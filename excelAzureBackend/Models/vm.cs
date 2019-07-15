using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace excelAzureBackend.Models

{

    [Serializable]
    public class vm
    {
        public string name;
        public int cores;
        public int ram;
        public int diskSize;
        public int ri;
        public bool isVcpu;
        public decimal? price;
        public decimal? pricemonth;
        public decimal? priceyear;
        public string currency;
        public string pricingTypes;
        public string displayName;
        public bool? isPreview;

    }

    [Serializable]
    public class price
    {
        public string region;
        public float value;
    }
    
    [Serializable]
    public class VmList
    {
        //public reserved;
        public string region ="";
        public string currency = "";

        public List<vm> vms;

        static HttpClient client = new HttpClient();

        private IConfiguration configuration;
        public VmList(IConfiguration iConfig)
        {
            configuration = iConfig;
            this.vms = new List<vm>();

        }

        /*public static string GetVMURL(int bindingPeriod, string currency)
        {
            switch (bindingPeriod)
            {
                case 0:
                    // 0 years no ahub
                    return "https://azure.microsoft.com/api/v2/pricing/virtual-machines-base/calculator/?culture=en-us&discount=mosp&currency=" + currency;
                case 1:
                    return "https://azure.microsoft.com/api/v2/pricing/virtual-machines-base-one-year/calculator/?culture=en-us&discount=mosp&currency=" + currency;
                case 3:
                    return "https://azure.microsoft.com/api/v2/pricing/virtual-machines-base-three-year/calculator/?culture=en-us&discount=mosp&currency=" + currency;
                default:
                    throw new KeyNotFoundException();
            }
        }*/
        private static string GetVMURLSoftware(int bindingPeriod, string currency)
        {
            string baseURL = "https://azure.microsoft.com/api/v2/pricing/";
            //string baseURL = "https://vmpricing.azureedge.net/";

            switch (bindingPeriod)
            {
                case 0:
                    // 0 years no ahub 
                    return "https://azure.microsoft.com/api/v2/pricing/virtual-machines-software/calculator/?culture=en-us&discount=mosp&currency=" + currency;
                case 1:
                    return "https://azure.microsoft.com/api/v2/pricing/virtual-machines-software-one-year/calculator/?culture=en-us&discount=mosp&currency=" + currency;
                case 3:
                    return "https://azure.microsoft.com/api/v2/pricing/virtual-machines-software-three-year/calculator/?culture=en-us&discount=mosp&currency=" + currency;
                default:
                    throw new KeyNotFoundException();
            }
        }
        private static string GetVMDispNameToken(int bindingPeriod)
        {
            switch (bindingPeriod)
            {
                case 0:
                    // 0 years no ahub
                    return "sizesPayGo";
                case 1:
                    return "sizesOneYear";
                case 3:
                    return "sizesThreeYear";
                default:
                    throw new KeyNotFoundException();
            }
        }
        public async Task<JArray> GetRegions()
        {
            excelAzureHelpers xhlp = new excelAzureHelpers(configuration);

            JArray cliRegions;
            // load local region list exported from CLI
            using (StreamReader r = new StreamReader("regionlist.json"))
            {
                string json = r.ReadToEnd();
                cliRegions = JArray.Parse(json.ToLower()); //JsonConvert.DeserializeObject<List<region>>(json);
            }
            //merge with calculator regions merge on displayname
            var response = await client.GetAsync(xhlp.GetVMURL(0, "usd",null));

            var result = JsonConvert.DeserializeObject<dynamic>((await response.Content.ReadAsStringAsync()).ToLower());
            var calcRegions = (JArray) result["regions"];
            foreach (JObject item in calcRegions)
            {
                // find corresponding displayName in cli regions
                var selected = cliRegions.SelectToken("$[?(@.displayName == '" + item["displayName"] + "')]");
                //var selected = (JObject) cliRegions.Where(cliRegion => cliRegion["displayName"].Equals(item["displayName"]));

                item.Merge(selected, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union
                });


            }
            return calcRegions;

        }
        public async Task<string> FindRegionSlug(string regionkw) {
            var regionkeyword = regionkw.ToLower();
            var regions = await GetRegions();
            String resultRegion;
                resultRegion = regions.SelectToken("$[?(@.displayname == '" + regionkeyword + "' || @.name == '" + regionkeyword + "' || @.slug == '" + regionkeyword + "')]")["slug"].Value<string>();


            return resultRegion;
        }


        public async Task<VmList> GetVmsAsync(int minCores = 0, int minMem = 0, int bindingPeriod = 0, string region = "europe-west", string currency = "usd", DateTime? date = null)
        {
            excelAzureHelpers xhlp = new excelAzureHelpers(configuration);

            var url = xhlp.GetVMURL(bindingPeriod, currency, date);

            var response = await client.GetAsync(xhlp.GetVMURL(bindingPeriod, currency, date));
            


            // #retries
            for (int i = 0; i < 3; i++)
            {
                if (!response.IsSuccessStatusCode)
                {
                    response = await client.GetAsync(xhlp.GetVMURL(bindingPeriod,currency,date));
                }
                else
                {
                    break;
                }
            }


            VmList vmlist = new VmList(configuration);
            vmlist.vms = new List<vm>();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());

                foreach (JProperty item in result["offers"])
                {
                    if (item.Name != "transactions") // transactions appearing in the offers list
                    {

                        var dispName = vmSearchDispName(item.Name, result[GetVMDispNameToken(bindingPeriod)]);
                        bool isPre = false;
                        if (dispName.ToLower().Contains("preview") || dispName.ToLower().Contains("promo") || dispName.ToLower().Contains("offer"))
                        {
                            isPre = true;
                        }
                        decimal? price;
                        decimal? pricemonth;
                        decimal? priceyear;
                        decimal pricemultiplier = 1;
                        if (isPre) { pricemultiplier = 2; }

                        try
                        {
                            price = (decimal?)item.Value.SelectToken("prices." + region + ".value") * pricemultiplier; // item.Value["prices"][region]["value"].Value<decimal>();
                            if (price != null)
                            {
                                pricemonth = (decimal?)price * 730 * pricemultiplier;
                                priceyear = (decimal?)pricemonth * 12 * pricemultiplier;

                            }
                            else
                            {
                                pricemonth = null;
                                priceyear = null;
                            }
                            vmlist.vms.Add(new vm() { name = item.Name, currency = currency, displayName = dispName, isPreview = isPre, ri = bindingPeriod, cores = item.Value["cores"].Value<int>(), ram = item.Value["ram"].Value<int>(), diskSize = item.Value["diskSize"].Value<int>(), isVcpu = item.Value["isVcpu"].Value<bool>(), price = price, pricemonth = pricemonth, priceyear = priceyear });
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
                System.Diagnostics.Trace.TraceError("endpoint unresponsive : " + xhlp.GetVMURL(bindingPeriod, currency,null));
            }
            return vmlist;
        }
        /*public async Task<VmList> GetVmsAsync(string date,  int minCores = 0, int minMem = 0, int bindingPeriod = 0, string region = "europe-west", string currency = "usd") {

                    // TODO implement historic pricing
                    
        }*/


        public async Task<VmList> GetVmsAsyncSoftware(int minCores = 0, int minMem = 0, int bindingPeriod = 0, string region = "europe-west", string currency = "usd", DateTime? date = null)
        {
            excelAzureHelpers xhlp = new excelAzureHelpers(configuration);

            var response = await client.GetAsync(GetVMURLSoftware(bindingPeriod, currency));


            // #retries
            for (int i = 0; i < 3; i++)
            {
                if (!response.IsSuccessStatusCode)
                {
                    response = await client.GetAsync(GetVMURLSoftware(bindingPeriod, currency));
                }
                else
                {
                    break;
                }
            }


            VmList vmlist = new VmList(configuration);
            vmlist.vms = new List<vm>();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());

                foreach (JProperty item in result["offers"])
                {
                    try
                    {
                        if (item.Name != "transactions" && item.Value["baseOfferSlug"] != null) // transactions appearing in the offers list
                    {
                        
                        var dispName = item.Name; //vmSearchDispName(item.Value["baseOfferSlug"].Value<string>(), result[GetVMDispNameToken(bindingPeriod)]);
                        bool isPre = false;
                        if (dispName.ToLower().Contains("preview") || dispName.ToLower().Contains("promo") || dispName.ToLower().Contains("offer"))
                        {
                            isPre = true;
                        }
                        decimal? price;
                        decimal? pricemonth;
                        decimal? priceyear;
                        decimal pricemultiplier = 1;
                        if (isPre) { pricemultiplier = 2;}


                            price = (decimal?)item.Value.SelectToken("prices." + region + ".value")*pricemultiplier; // item.Value["prices"][region]["value"].Value<decimal>();
                        if (price != null)
                        {
                            pricemonth = (decimal?)price * 730 * pricemultiplier;
                            priceyear = (decimal?)pricemonth * 12 * pricemultiplier;

                        }
                        else
                        {
                            pricemonth = null;
                            priceyear = null;
                        }

                            vmlist.vms.Add(new vm() { name = item.Name, currency = currency, displayName = "", isPreview= isPre, ri = bindingPeriod, cores = item.Value["cores"].Value<int>(), ram = item.Value["ram"].Value<int>(), diskSize = item.Value["diskSize"].Value<int>(), isVcpu = item.Value["isVcpu"].Value<bool>(), price = price, pricemonth = pricemonth, priceyear = priceyear });

                        }
                        
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Trace.TraceWarning("Issues with : " + item.ToString());

                        throw;
                    }
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceError("endpoint unresponsive : "+ xhlp.GetVMURL(bindingPeriod, currency, null));
            }
            return vmlist;
        }
        private static string vmSearchDispName(string name, JToken containerToken)
        {
            var productname = name.Split('-')[1].ToLower();

            //var result = containerToken.Where(i => i["sslug"].Value<string>().Contains<string>(productname));
            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    if (child["slug"].Value<string>().ToLower() == productname)
                    {
                        return child["displayName"].Value<string>();

                    }
                }


            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    if (child["slug"].Value<string>().ToLower() == productname)
                    {
                        return child["displayName"].Value<string>();

                    }
                    //FindTokens(child, name, matches);
                }
            }
            return "";
            /*else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    return child["displayName"];
                }
            }*/
        }
    }
    [Serializable]
    public class Dbo
    {
        public List<vm> vms;
        public DateTime date;
        public string region;
        public string currency;
    }

}
