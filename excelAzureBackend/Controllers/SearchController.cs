using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using excelAzureBackend.Models;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace instance.Controllers
{
    [Produces("application/json")]
    [Route("api/v1")]
    public class SearchController : Controller
    {
        private IConfiguration configuration;
        public SearchController(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        static HttpClient client = new HttpClient();
        // GET api/values
        [HttpGet]
        public async Task<IOrderedEnumerable<vm>> Get(int minCores = 0, int minRam = 0, string os = "linux", string tier = "standard", string region = "europe-west", int ri = 0, string format = "")
        {
            VmList vml = new VmList(configuration);
            var vmlist = await vml.GetVmsAsync(region: region, bindingPeriod: ri);
            var result = vmlist.vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.name.IndexOf(os) > -1 && i.name.Contains(tier) && i.price.HasValue).OrderBy(x => x.price);

            return result;
        }
        [HttpGet("regions")]
        public async Task<dynamic> Get()
        {
            VmList vml = new VmList(configuration);
            
            return await vml.GetRegions();
        }
        [HttpGet("mdisk")]
        public async Task<IOrderedEnumerable<mdisk>> GetStorage(int minSize = 0, string region = "europe-west", string currency = "usd")
        {
            mdisk md = new mdisk(configuration);
            List<mdisk> mdiskList = await md.GetMdisks(region, currency);
            var result = mdiskList.Where(i => i.size >= minSize && i.pricemonth.HasValue).OrderBy(x => x.pricemonth);
            return result;

        }
    }



}