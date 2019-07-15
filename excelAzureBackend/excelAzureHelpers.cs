using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace excelAzureBackend
{
    public class excelAzureHelpers

    {
        public excelAzureHelpers(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        private IConfiguration configuration;

        public string GetVMURL(int bindingPeriod, string currency, DateTime? date)
        {
            if (date.HasValue && !(date.Value < (DateTime.Now.AddDays(-7))))
            {
                date = null;
            }


            if (!date.HasValue)
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
            }
            else
            {
                string blobUri = configuration.GetValue<string>("BlobStorageSettings:blobUri");
                string containerName = configuration.GetValue<string>("BlobStorageSettings:containerName");

                return blobUri + getBlobPath(date.Value, currency, "vm", bindingPeriod);

            }

        }
        public string GetMdiskURL(string currency, DateTime? date = null) {
            if (date.HasValue && !(date.Value < (DateTime.Now.AddDays(-7))))
            {
                date = null;
            }
            if (date.HasValue)
            {
                string blobUri = configuration.GetValue<string>("BlobStorageSettings:blobUri");
                return blobUri + getBlobPath(date.Value, currency, "mdisk");

            }
            else
            {
                return configuration.GetValue<string>("CalculatorUrls:mdisk") + currency;

            }

        }

        private static int GetIso8601WeekOfYear(DateTime time)
        {

            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
        public string getBlobPath(DateTime date, string currency, string type = "vm", int? ri = null, bool container = true)
        {
            string containerName = configuration.GetValue<string>("BlobStorageSettings:containerName");
            string week = GetIso8601WeekOfYear(date).ToString();
            // year + "/" + week + "/" + currency + "/" + filename
            var filename = "";
            if (type.Equals("vm"))
            {
                filename = "vm_ri" + ri.ToString();
            }
            else if (type.Equals("mdisk"))
            {
                filename = "mdisk";

            }


            if (container)
            {
                return ("/" + containerName + "/" + date.Year.ToString() + "/" + week + "/" + currency + "/" + filename).ToLower();

            }
            else
            {
                return ("" + date.Year.ToString() + "/" + week + "/" + currency + "/" + filename).ToLower();

            }

        }
        public static DateTime? parseDTString(string input) {
            try
            {
                return DateTime.ParseExact(input, "yyyyMMdd", CultureInfo.InvariantCulture);

            }
            catch (Exception)
            {

                return null;
            }


        }
        public static Boolean validateCurrency(string currency) {
            var currencies = new string[] { "USD", "EUR", "CHF", "ARS", "AUD", "DKK", "CAD", "IDR", "JPY", "KRW", "NZD", "NOK", "RUB", "SAR", "ZAR", "SEK", "TWD", "TRY", "GBP", "MXN", "MYR", "INR", "HKD", "BRL" };
            bool has = currencies.Contains(currency.ToUpper());
            if (has != true)
            {
                throw new ArgumentException("Currency not supported");
            }
            return has;
        }
    }
}
