using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using excelAzureBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace instance.Controllers
{
    [Produces("text/plain")]
    [Route("api/v1/csv/preview")]
    public class csvControllerPreview : Controller
    {
        private IConfiguration configuration;
        public csvControllerPreview(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        [HttpGet]
        public async Task<string> GetVM(int minCores = 0, int minRam = 0, string os = "linux", string tier = "standard", string region = "europe-west", int ri = 0, string format = "", string currency = "usd")
        {
            VmList vml = new VmList(configuration);
            var taskList = new List<Task<VmList>>();

            taskList.Add(vml.GetVmsAsync(region: region, bindingPeriod: 0, currency: currency));
            taskList.Add(vml.GetVmsAsync(region: region, bindingPeriod: 1, currency: currency));
            taskList.Add(vml.GetVmsAsync(region: region, bindingPeriod: 3, currency: currency));

            taskList.Add(vml.GetVmsAsyncSoftware(region: region, bindingPeriod: 0, currency: currency));
            taskList.Add(vml.GetVmsAsyncSoftware(region: region, bindingPeriod: 1, currency: currency));
            taskList.Add(vml.GetVmsAsyncSoftware(region: region, bindingPeriod: 3, currency: currency));

            await Task.WhenAll(taskList.ToArray());

            List<vm> vms = new List<vm>();

            //VmList vms = new VmList();
            for (int i = 0; i < taskList.Count; i++)
            {
                vms.AddRange(taskList[i].Result.vms);
            }
            var result = vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.name.Contains(tier) && i.price.HasValue).OrderBy(x => x.price);
            StringBuilder s = new StringBuilder();
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(result));

            foreach (DataColumn column in dt.Columns)
            {
                s.Append(column.ColumnName + ";");
            }
            s.Append(Environment.NewLine);

            foreach (DataRow row in dt.Rows)
            {
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    s.Append(row[i] + ";");
                }
                s.Append(Environment.NewLine);
            }




            return s.ToString();
        }
        [HttpGet("mdisks")]
        public async Task<string> GetStorageCSV(int minSize = 0, string region = "europe-west", string currency = "usd")
        {
            mdisk md = new mdisk(configuration);
            List<mdisk> mdiskList = await md.GetMdisks(region, currency);
            var result = mdiskList.Where(i => i.size >= minSize && i.pricemonth.HasValue).OrderBy(x => x.pricemonth);
            StringBuilder s = new StringBuilder();
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(result));

            foreach (DataColumn column in dt.Columns)
            {
                s.Append(column.ColumnName + ";");
            }
            s.Append(Environment.NewLine);

            foreach (DataRow row in dt.Rows)
            {
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    s.Append(row[i] + ";");
                }
                s.Append(Environment.NewLine);
            }
            return s.ToString();

        }

    }
}