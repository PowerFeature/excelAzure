using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Data;
using System.Text;
using excelAzureBackend.Models;
using Microsoft.Azure.Documents.Client;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using excelAzureBackend;

namespace instance.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private IConfiguration configuration;
        public ValuesController(IConfiguration iConfig)
        {
            configuration = iConfig;
        }
         
        static HttpClient client = new HttpClient();
        // GET api/values
        [HttpGet]
        public async Task<IOrderedEnumerable<vm>> Get(string date = "",int minCores = 0, int minRam = 0, string os = "linux", string tier = "standard", string region = "europe-west", int ri =0, string format= "", string currency = "usd")
        {
            DateTime? dt;
            VmList vml = new VmList(configuration);
            String regionSlug;
            try
            {
                dt = excelAzureBackend.excelAzureHelpers.parseDTString(date);

                regionSlug = await vml.FindRegionSlug(region);
                var validCurrency = excelAzureHelpers.validateCurrency(currency);

            }
            catch (Exception)
            {
                this.Response.StatusCode = 400;

                return null;

            }
            var taskList = new List<Task<VmList>>();

            taskList.Add(vml.GetVmsAsync(region: region, bindingPeriod: 0, currency: currency, date: dt));
            taskList.Add(vml.GetVmsAsync(region: region, bindingPeriod: 1, currency: currency, date: dt));
            taskList.Add(vml.GetVmsAsync(region: region, bindingPeriod: 3, currency: currency, date: dt));

            await Task.WhenAll(taskList.ToArray());
            VmList vms = new VmList(configuration);
            for (int i = 0; i < taskList.Count; i++)
            {
                vms.vms.AddRange(taskList[i].Result.vms);
            }


            //var result = vms.vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.name.Contains(tier) && i.price.HasValue).OrderBy(x => x.price);
            var result = vms.vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.name.Contains(tier?.ToString() ?? "") && i.price.HasValue).OrderBy(x => x.price);

            //var responsetext = JsonConvert.SerializeObject(result);

            return result;
        }
        // GET api/values
        [HttpGet("mdisks")]
        public async Task<IOrderedEnumerable<mdisk>> GetStorage(string date = "", int minSize = 0, string region = "europe-west", string currency = "usd")
        {
            DateTime? dt;
            VmList vml = new VmList(configuration);
            String regionSlug;
            try
            {
                dt = excelAzureBackend.excelAzureHelpers.parseDTString(date);

                regionSlug = await vml.FindRegionSlug(region);
                var validCurrency = excelAzureHelpers.validateCurrency(currency);

            }
            catch (Exception)
            {
                this.Response.StatusCode = 400;

                return null;

            }

            mdisk md = new mdisk(configuration);
            List<mdisk> mdiskList = await md.GetMdisks(regionSlug, currency,dt);
            var result = mdiskList.Where(i => i.size >= minSize && i.pricemonth.HasValue).OrderBy(x => x.pricemonth);
            return result;

        }
        // GET api/values/csv
        [HttpGet("csv")]
        public async Task<string> GetVM(int minCores = 0, int minRam = 0, string os = "", string tier = "standard", string region = "europe-west", int ri = 0, string format = "", string currency = "usd", string date = "")
        {
            DateTime? dt;
            VmList vml = new VmList(configuration);
            String regionSlug;
            try
            {
                dt = excelAzureBackend.excelAzureHelpers.parseDTString(date);

                regionSlug = await vml.FindRegionSlug(region);
                var validCurrency = excelAzureHelpers.validateCurrency(currency);

            }
            catch (Exception)
            {
                this.Response.StatusCode = 400;

                return "";
                
            }
            // Search for region slug

            var taskList = new List<Task<VmList>>();

            taskList.Add(vml.GetVmsAsync(region: regionSlug, bindingPeriod: 0, currency: currency, date : dt));
            taskList.Add(vml.GetVmsAsync(region: regionSlug, bindingPeriod: 1, currency: currency, date: dt));
            taskList.Add(vml.GetVmsAsync(region: regionSlug, bindingPeriod: 3, currency: currency, date: dt));

            await Task.WhenAll(taskList.ToArray());

            VmList vms = new VmList(configuration);
            for (int i = 0; i < taskList.Count; i++)
            {
                vms.vms.AddRange(taskList[i].Result.vms);
            }



            var result = vms.vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.name.Contains(tier?.ToString() ?? "") && i.name.Contains(os?.ToString() ?? "")  && i.price.HasValue).OrderBy(x => x.price);


            
/*            if (store)
            {
                string EndpointUri = configuration.GetValue<string>("CosmosDBSettings:EndpointUri");
                string PrimaryKey = configuration.GetValue<string>("CosmosDBSettings:PrimaryKey");
                string databaseName = configuration.GetValue<string>("CosmosDBSettings:databaseName");

                DocumentClient Dbclient;

                Dbo dbo = new Dbo();
                dbo.vms = result.ToList<vm>();
                dbo.region = region;
                dbo.currency = currency;
                dbo.date = DateTime.Now;
                // Save result to DocumentDB
                Dbclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey);
                await Dbclient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, "vm"), dbo);



            }*/
            StringBuilder s = new StringBuilder();
            DataTable datbl = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(result));

                foreach (DataColumn column in datbl.Columns)
                {
                s.Append(column.ColumnName + ";");
                }
            s.Append(Environment.NewLine);

                foreach (DataRow row in datbl.Rows)
                {
                    for (var i = 0; i < datbl.Columns.Count; i++)
                    {
                        s.Append(row[i] + ";");
                    }
                s.Append(Environment.NewLine);
                }



            
            return s.ToString();
        }
        [HttpGet("csv/mdisks")]
        public async Task<string> GetStorageCSV(int minSize = 0, string region = "europe-west", string currency = "usd", bool store = false, string date = null)
        {
            DateTime? dt;
            VmList vml = new VmList(configuration);
            String regionSlug;
            try
            {
                dt = excelAzureBackend.excelAzureHelpers.parseDTString(date);

                regionSlug = await vml.FindRegionSlug(region);
                var validCurrency = excelAzureHelpers.validateCurrency(currency);

            }
            catch (Exception)
            {
                this.Response.StatusCode = 400;

                return "";

            }

            mdisk md = new mdisk(configuration);
            List<mdisk> mdiskList = await md.GetMdisks(regionSlug, currency, dt);
            var result = mdiskList.Where(i => i.size >= minSize && i.pricemonth.HasValue).OrderBy(x => x.pricemonth);

            if (store)
            {
                string EndpointUri = configuration.GetValue<string>("CosmosDBSettings:EndpointUri");
                string PrimaryKey = configuration.GetValue<string>("CosmosDBSettings:PrimaryKey");
                string databaseName = configuration.GetValue<string>("CosmosDBSettings:databaseName");

                DocumentClient Dbclient;
                DboMdisks dbomdisks = new DboMdisks();
                dbomdisks.mdisks = result.ToList<mdisk>();
                dbomdisks.region = region;
                dbomdisks.currency = currency;
                dbomdisks.date = DateTime.Now;

                // Save result to DocumentDB
                Dbclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey);
                await Dbclient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, "mdisks"), dbomdisks);



            }

            StringBuilder s = new StringBuilder();
            DataTable datbl = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(result));

            foreach (DataColumn column in datbl.Columns)
            {
                s.Append(column.ColumnName + ";");
            }
            s.Append(Environment.NewLine);

            foreach (DataRow row in datbl.Rows)
            {
                for (var i = 0; i < datbl.Columns.Count; i++)
                {
                    s.Append(row[i] + ";");
                }
                s.Append(Environment.NewLine);
            }
            return s.ToString();
        }
        [HttpGet("csv/mdisks2")]
        public async Task<string> GetStorageCSV2(int minSize = 0, string region = "europe-west", string currency = "usd", bool store = false, string date = null)
        {
            DateTime? dt;
            VmList vml = new VmList(configuration);
            String regionSlug;
            try
            {
                dt = excelAzureBackend.excelAzureHelpers.parseDTString(date);

                regionSlug = await vml.FindRegionSlug(region);
                var validCurrency = excelAzureHelpers.validateCurrency(currency);

            }
            catch (Exception)
            {
                this.Response.StatusCode = 400;

                return "";

            }
            UltraSSDs ussds = new UltraSSDs(configuration);
            var ussdisks = await ussds.GetUltraSSDs(region, currency, dt);

            mdisk md = new mdisk(configuration);
            List<mdisk> mdiskList = await md.GetMdisks(regionSlug, currency, dt);
            List<mdisk> ussdMdiskList = new List<mdisk>();


            foreach (var item in mdiskList)
            {
                    var compare_ussd =  ussds.findUSSD(ussdisks, item.size, item.iops, item.speed, item.pricemonth);
                if (compare_ussd.Count<UltraSSD>()>0) {
                    var ussdPriceMonth = ussds.calculatePriceMonth(compare_ussd[0], item.iops, item.speed);
                    // If the USSD is cheaper, replace the disk with USSD
                    string mdiskname = item.name.Split("-")[1];

                    item.name = "ultrassd-" + mdiskname; item.displayName = compare_ussd[0].displayName; item.isPreview = false; item.size = compare_ussd[0].size; item.pricemonth = ussdPriceMonth; item.priceyear = ussdPriceMonth * 12; item.snapshotPrice = null; item.transactionPrice = null; item.snapshotPriceLrs = null; item.snapshotPriceZrs = null;

                    //ussdMdiskList.Add(new mdisk(configuration) { name = compare_ussd[0].name, displayName = compare_ussd[0].displayName, isPreview = false, size = compare_ussd[0].size, iops = item.iops, speed = item.speed, pricemonth = ussdPriceMonth,  priceyear = ussdPriceMonth*12, snapshotPrice = null, transactionPrice = null });
                }
            }
            ussdMdiskList.ForEach(p => mdiskList.Add(p));


            var result = mdiskList.Where(i => i.size >= minSize && i.pricemonth.HasValue).OrderBy(x => x.pricemonth);

            StringBuilder s = new StringBuilder();
            DataTable datbl = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(result));

            foreach (DataColumn column in datbl.Columns)
            {
                s.Append(column.ColumnName + ";");
            }
            s.Append(Environment.NewLine);

            foreach (DataRow row in datbl.Rows)
            {
                for (var i = 0; i < datbl.Columns.Count; i++)
                {
                    s.Append(row[i] + ";");
                }
                s.Append(Environment.NewLine);
            }
            return s.ToString();
        }
        [HttpGet("csv/ussd")]
        public async Task<string> GetUSSDCSV(int minSize = 0, string region = "europe-west", string currency = "usd", bool store = false, string date = null)
        {
            DateTime? dt;
            VmList vml = new VmList(configuration);
            String regionSlug;
            try
            {
                dt = excelAzureBackend.excelAzureHelpers.parseDTString(date);

                regionSlug = await vml.FindRegionSlug(region);
                var validCurrency = excelAzureHelpers.validateCurrency(currency);

            }
            catch (Exception)
            {
                this.Response.StatusCode = 400;

                return "";

            }

            UltraSSDs md = new UltraSSDs(configuration);
            List<UltraSSD> mdiskList = await md.GetUltraSSDs(region, currency, dt);
            var result = mdiskList.Where(i => i.size >= minSize).OrderBy(x => x.size);
            StringBuilder s = new StringBuilder();
            DataTable datbl = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(result), new JsonSerializerSettings
            {
                FloatParseHandling = FloatParseHandling.Decimal
            });
                        
            foreach (DataColumn column in datbl.Columns)
            {
                s.Append(column.ColumnName + ";");
            }
            s.Append(Environment.NewLine);

            foreach (DataRow row in datbl.Rows)
            {
                for (var i = 0; i < datbl.Columns.Count; i++)
                {
                    s.Append(row[i] + ";");
                }
                s.Append(Environment.NewLine);
            }
            return s.ToString();
        }

        [HttpGet("regions")]
        public async Task<JArray> Regions() {
            VmList vml = new VmList(configuration);
            return await vml.GetRegions();
        }
        [HttpGet("csv/regions")]
        public async Task<String> RegionsCSV()
        {
            VmList vml = new VmList(configuration);
            var regions = await vml.GetRegions();
            StringBuilder s = new StringBuilder();    
            foreach (var item in regions)
            {
                s.Append(item.Value<string>("slug"));
                s.Append(";");
            }
            return s.ToString();
        }

    }

}
