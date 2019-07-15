using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using excelAzureBackend.Models;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace excelAzureBackend.Controllers
{

    [Produces("text/plain")]
    [Route("api/v1/csv")]
    public class csvController : Controller
    {
        private IConfiguration configuration;
        public csvController(IConfiguration iConfig)
        {
            configuration = iConfig;
        }



        [HttpGet]
        public async Task<string> GetVM(int minCores = 0, int minRam = 0, string os = "linux", string tier = "standard", string region = "europe-west", int ri = 0, string format = "", string currency = "usd", bool store = false, string date = "")
        {
            DateTime? dt;
            Models.VmList vml = new Models.VmList(configuration);
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



            var taskList = new List<Task<VmList>>();

            taskList.Add(vml.GetVmsAsync(region: regionSlug, bindingPeriod: 0, currency: currency, date: dt));
            taskList.Add(vml.GetVmsAsync(region: regionSlug, bindingPeriod: 1, currency: currency, date: dt));
            taskList.Add(vml.GetVmsAsync(region: regionSlug, bindingPeriod: 3, currency: currency, date: dt));

            await Task.WhenAll(taskList.ToArray());

            List<vm> vms = new List<vm>();

            //VmList vms = new VmList();
            for (int i = 0; i < taskList.Count; i++)
            {
                vms.AddRange(taskList[i].Result.vms);
            }

            var result = vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.name.Contains(tier) && i.price.HasValue).OrderBy(x => x.price);


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
        [HttpGet("mdisks")]
        public async Task<string> GetStorageCSV(int minSize = 0, string region = "europe-west", string currency = "usd", string date = null)
        {
            DateTime? dt;
            Models.VmList vml = new Models.VmList(configuration);
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
            List<mdisk> mdiskList = await md.GetMdisks(region, currency, dt);
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
        [HttpGet("ussd")]
        public async Task<string> GetUSSDCSV(int minSize = 0, string region = "europe-west", string currency = "usd", string date = null)
        {
            DateTime? dt;
            Models.VmList vml = new Models.VmList(configuration);
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
            var result = mdiskList.Where(i => i.size >= minSize).OrderBy(x => x.displayName);
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
    }
}