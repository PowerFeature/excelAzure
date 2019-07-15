using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
 

namespace excelInstance
{

    public static class instanceFunctions
    {
        private static int test = 2;
        private static List<VmList> DC = new List<VmList>();
        [ExcelFunction(Description = "Get VM")]
        public static string getVMc(int minCores, int minRam, int ri, string region, string currency)
        {
            if (currency.Equals("") || region.Equals("") || minCores.Equals(0) || minRam.Equals(0))
            {
                return "";
            }
            else
            {
            Vm result = new Vm();
            //Search 
            var Search = DC.Where(v => v.region.ToLower() == region.ToLower() && v.currency.ToLower() == currency.ToLower());
            if (Search.Count<VmList>() > 1) { throw new Exception(); }
            if (Search.Count<VmList>() == 1)
            {
                    if (Search.First<VmList>().vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.price.HasValue).OrderBy(x => x.price).Count<Vm>() > 0)
                    {
                        result = (Vm)Search.First<VmList>().vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.price.HasValue).OrderBy(x => x.price).First();
                        return result.name;
                    }
                    else
                    {
                        return "Not Found!";
                    }

            }
            else
            {
                Fetcher ft = new Fetcher();
                    try
                    {
                        DC.Add(ft.GetDC(currency, region));
                    }
                    catch (Exception)
                    {

                        return "Invalid Input!";
                    }
                
                Search = DC.Where(v => v.region == region.ToLower() && v.currency == currency.ToLower());

                    if (Search.First<VmList>().vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.price.HasValue).OrderBy(x => x.price).Count<Vm>() > 0)
                    {
                        result = (Vm)Search.First<VmList>().vms.Where(i => i.cores >= minCores && i.ram >= minRam && i.price.HasValue).OrderBy(x => x.price).First();
                        return result.name;
                    }
                    else
                    {
                        return "Not Found!";
                    }
                }
            }
        }
        [ExcelFunction(Description = "Get Price Hour")]
        public static double getPriceHourc(string name, int ri, string region, string currency)
        {

            if (name.Equals("") || region.Equals("") || currency.Equals("") || ri.Equals(""))
            {
                throw new ArgumentOutOfRangeException();
            }
            else
            {
                Vm result = new Vm();
                //Search 
                var Search = DC.Where(v => v.region.ToLower() == region.ToLower() && v.currency.ToLower() == currency.ToLower());
                if (Search.Count<VmList>() > 1) { throw new Exception(); }
                if (Search.Count<VmList>() == 1)
                {
                    if (Search.First<VmList>().vms.Where(i => i.name.Equals(name) && i.price.HasValue && i.ri == ri).OrderBy(x => x.price).Count<Vm>() > 0)
                    {
                        result = (Vm)Search.First<VmList>().vms.Where(i => i.name.Equals(name) && i.price.HasValue && i.ri == ri).OrderBy(x => x.price).First();
                        return (double) result.price;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();

                    }

                }
                else
                {
                    Fetcher ft = new Fetcher();
                    try
                    {
                        DC.Add(ft.GetDC(currency, region));
                    }
                    catch (Exception)
                    {

                        throw new ArgumentOutOfRangeException();
                    }

                    Search = DC.Where(v => v.region == region.ToLower() && v.currency == currency.ToLower());

                    if (Search.First<VmList>().vms.Where(i => i.name.Equals(name) && i.price.HasValue && i.ri == ri).OrderBy(x => x.price).Count<Vm>() > 0)
                    {
                        result = (Vm)Search.First<VmList>().vms.Where(i => i.name.Equals(name) && i.price.HasValue && i.ri == ri).OrderBy(x => x.price).First();
                        return (double) result.price;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }

            }
        }
    }

    class Fetcher
    {
        private HttpClient client = new HttpClient();
        public VmList GetDC(string currency, string region)
        {
            var ResponseTask = client.GetAsync("https://vmsizecdnv.azureedge.net/api/values/?seed=123&region=" + region + "&currency="+currency);
            ResponseTask.Wait();
            var response = ResponseTask.Result;
            var ContentTask = response.Content.ReadAsStringAsync();
            ContentTask.Wait();

            var JsonContent = JsonConvert.DeserializeObject<List<Vm>>(ContentTask.Result);

            return new VmList() { currency = currency.ToLower(), region = region.ToLower(), vms = JsonContent };
            
        }

    }


}
