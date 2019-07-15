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
    public class UltraSSD
    {
        public string name;
        public string displayName;
        public int size;
        public int maxIops;
        public int minIops;
        public int maxThroughput;
        public int minThroughput;
        public decimal? priceIops; // IOPS /HOUR
        public decimal? priceSize; // GB/Hour
        public decimal? priceThroughput; //MB/S/HOUR
        public decimal? priceVCpu;
        public string currency;
    }
    public class UltraSSDs { 
        static HttpClient client = new HttpClient();
        public const string mdiskUrl = "https://azure.microsoft.com/api/v2/pricing/managed-disks/calculator/?culture=en-us&discount=mosp&currency=";
        private IConfiguration configuration;
        public UltraSSDs(IConfiguration iConfig)
        {
            configuration = iConfig;
        }
        public async Task<List<UltraSSD>> GetUltraSSDs(string region = "us-east", string currency = "usd", DateTime? date = null) {
            List<UltraSSD> usds = new List<UltraSSD>();
            excelAzureHelpers xhlp = new excelAzureHelpers(configuration);
            var response = await client.GetAsync(xhlp.GetMdiskURL(currency, date));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(content);
                JObject maxIopsPerGb = (JObject) result["maxIopsPerGb"];
                JObject maxThroughputPerGb = (JObject)result["maxThroughputPerGb"];
                int minIopsPerGb = result["minIops"].Value<int>();
                int minThroughput = result["minThroughput"].Value<int>();

                try
                {
                    foreach (JObject item in result["ultraSizes"])
                    {
                        UltraSSD disk = new UltraSSD();
                        disk.name = "ultrassd-u" + item["slug"].Value<string>();
                        disk.currency = currency;
                        disk.size = item["slug"].Value<int>();
                        disk.displayName = "Ultra SSD " + item["displayName"].Value<string>();
                        disk.minIops = minIopsPerGb;
                        disk.minThroughput = minThroughput;

                        disk.maxIops = maxIopsPerGb.SelectToken(disk.size.ToString()).Value<int>();
                        disk.maxThroughput = maxThroughputPerGb.SelectToken(disk.size.ToString()).Value<int>();
                        disk.priceIops = result["offers"]["ultrassd-iops"]["prices"][region]["value"].Value<decimal>();

                        disk.priceThroughput = result["offers"]["ultrassd-throughput"]["prices"][region]["value"].Value<decimal>();
                        disk.priceVCpu = result["offers"]["ultrassd-vcpu"]["prices"][region]["value"].Value<decimal>();
                        disk.priceSize = result["offers"]["ultrassd-stored"]["prices"][region]["value"].Value<decimal>();

                        usds.Add(disk);
                    }
                }
                catch (Exception)
                {
                    // Ignore Exceptions
                    //throw;
                }


                
            }
            return usds;


    }
        public decimal? calculatePriceMonth(UltraSSD ussd, int? iops, int? throoughput, int? vCPU=0) {

            const int HoursMonth = 730;
            decimal? outputPrice = (ussd.priceSize * HoursMonth * ussd.size) + (ussd.priceIops * iops * HoursMonth) + (ussd.priceThroughput * throoughput * HoursMonth) + (ussd.priceVCpu * vCPU * HoursMonth);

            return outputPrice;
;
        }


        public List<UltraSSD> findUSSD(List<UltraSSD> Ussds, int? size, int? iops, int? throoughput, decimal? priceToBeat) {

            List<UltraSSD> newlist = new List<UltraSSD>();
            foreach (var item in Ussds)
            {
                decimal? ussdPriceMonth = calculatePriceMonth(item, iops, throoughput);
                if (item.size >= size && item.minIops <= iops && item.maxIops >= iops && item.minThroughput <= throoughput && item.maxThroughput >= throoughput && ussdPriceMonth < priceToBeat)
                {
                    newlist.Add(item);
                }
            }
            //List<Order> SortedList = objListOrder
            return newlist.OrderBy(o => o.size).Take<UltraSSD>(1).ToList();
        }
    }
}
