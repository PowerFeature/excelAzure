using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace excelAzureBackend.Models
{
    [Serializable]
    public class mdisk
    {
        public string name;
        public int size;
        public int? iops;
        public int? speed;
        public decimal? pricemonth;
        public decimal? priceyear;
        public string currency;
        public decimal? transactionPrice;
        public decimal? snapshotPrice;
        public decimal? snapshotPriceLrs;
        public decimal? snapshotPriceZrs;
        public string displayName;
        public bool? isPreview;
        static HttpClient client = new HttpClient();
        public const string mdiskUrl = "https://azure.microsoft.com/api/v2/pricing/managed-disks/calculator/?culture=en-us&discount=mosp&currency=";
        private IConfiguration configuration;
        public mdisk(IConfiguration iConfig)
        {
            configuration = iConfig;
        }



        public async Task<List<mdisk>> GetMdisks(string region = "europe-west", string currency = "usd", DateTime? date = null)
        {
            List<mdisk> mds = new List<mdisk>();
            excelAzureHelpers xhlp = new excelAzureHelpers(configuration);
            
            var response = await client.GetAsync(xhlp.GetMdiskURL(currency,date));

            if (response.IsSuccessStatusCode)
            {

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(content);

                decimal? snapshotPriceStandardHHD_LRS = (decimal?)result.SelectToken("offers.standardhdd-snapshot-lrs.prices." + region + ".value");
                decimal? snapshotPriceStandardHDD_ZRS = (decimal?)result.SelectToken("offers.standardhdd-snapshot-zrs.prices." + region + ".value");

                decimal? snapshotPriceStandardSSD = (decimal?)result.SelectToken("offers.standardssd-snapshot.prices." + region + ".value");
                decimal? snapshotPricePremium = (decimal?)result.SelectToken("offers.premiumssd-snapshot.prices." + region + ".value");
                decimal? transactionPriceHDD = (decimal?)result.SelectToken("offers.transactions-hdd.prices." + region + ".value");
                decimal? transactionPriceSSD = (decimal?)result.SelectToken("offers.transactions-ssd.prices." + region + ".value");

                foreach (JProperty item in result["offers"])
                {
                    if (!item.Name.Contains("snapshot") && item.Name != "standardssd-snapshot" && item.Name != "premiumssd-snapshot" && !item.Name.Contains("transactions"))
                    {


                        // switch to take out non disk elements
                        var productname = item.Name.Split('-').Last().ToLower();
                        var seriesname = item.Name.Split('-').First().ToLower();
                        var dispName = mdiskSearchDispName(seriesname, result["tiers"])  + " " + mdiskSearchDispName(productname, result["sizes"]);
                        bool isPre = false;
                        decimal pricemultiplier = 1;

                        if (dispName.ToLower().Contains("preview") || dispName.ToLower().Contains("promo") || dispName.ToLower().Contains("offer"))
                        {
                            isPre = true;
                            pricemultiplier = 2;
                        }


                        decimal? pricemonth;
                        decimal? priceyear;

                        try
                        {

                            pricemonth = (decimal?)item.Value.SelectToken("prices." + region + ".value") * pricemultiplier; // Value["prices"][region]["value"].Value<decimal>();
                            if (pricemonth != null)
                            {
                                priceyear = (decimal?)pricemonth * 12 * pricemultiplier;

                            }
                            else
                            {
                                priceyear = null;

                            }

                            if (item.Name.Contains("premiumssd"))
                            {
                                mds.Add(new mdisk(configuration) { name = item.Name, displayName = dispName, isPreview=isPre, size = item.Value["size"].Value<int>(), iops = item.Value["iops"].Value<int>(), speed = item.Value["speed"].Value<int>(), pricemonth = pricemonth, priceyear = priceyear, snapshotPrice = snapshotPricePremium, transactionPrice = transactionPriceSSD });



                            }
                            else if (item.Name.Contains("standardssd"))
                            {
                                mds.Add(new mdisk(configuration) { name = item.Name, displayName = dispName, isPreview = isPre, size = item.Value["size"].Value<int>(), iops = item.Value["iops"].Value<int>(), speed = speed = item.Value["speed"].Value<int>(), pricemonth = pricemonth, priceyear = priceyear, snapshotPrice = snapshotPriceStandardSSD, transactionPrice = transactionPriceSSD });


                            }

                            else if (item.Name.Contains("standardhdd"))
                            {
                                mds.Add(new mdisk(configuration) { name = item.Name, displayName = dispName, isPreview = isPre, size = item.Value["size"].Value<int>(), iops = item.Value["iops"].Value<int>(), speed = speed = item.Value["speed"].Value<int>(), pricemonth = pricemonth, priceyear = priceyear, snapshotPrice = null, snapshotPriceLrs = snapshotPriceStandardHHD_LRS, snapshotPriceZrs = snapshotPriceStandardHDD_ZRS, transactionPrice = transactionPriceHDD });
                            }
                        }
                        catch (Exception)
                        {
                            System.Diagnostics.Trace.TraceWarning("Issues with : " + item.ToString());
                            throw;
                        }
                    }

                }


            }
            else
            {
                System.Diagnostics.Trace.TraceError("endpoint unresponsive : " + mdiskUrl);
            }


            return mds;

        }

        private static string mdiskSearchDispName(string name, JToken containerToken)
        {
            
            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    if (child["slug"].Value<string>().ToLower() == name)
                    {
                        return child["displayName"].Value<string>();
                        
                    }
                }


            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    if (child["slug"].Value<string>().ToLower() == name)
                    {
                        return child["displayName"].Value<string>();

                    }
                }
            }
            return "";
        }

        private decimal? ParseOrDefault(JToken inputToken, decimal? defaultIfInvalidString = null)
        {
            decimal result;
            try
            {
                if (decimal.TryParse(inputToken.Value<string>().Replace(".", ","), out result))
                    return result;
                return defaultIfInvalidString;
            }
            catch (NullReferenceException)
            {
                return null;
            }



        }
    }
    public class DboMdisks
    {
        public List<mdisk> mdisks;
        public DateTime date;
        public string region;
        public string currency;

    }

}
