using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using excelAzureBackend;

using excelAzureBackend.Models;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Blob;

namespace excelAzureStore
{
    class calcStorage
    {
            private static HttpClient client;
            private IConfiguration configuration;
            public calcStorage(IConfiguration iConfig)
            {
                configuration = iConfig;
            }
            public async Task<string> StoreVMs(int ri = 0, string format = "", string currency = "usd")
            {
                client = new HttpClient();
                VmList vml = new VmList(configuration);
                excelAzureHelpers xhlp = new excelAzureHelpers(configuration);
                //var taskList = new List<Task<HttpResponseMessage>>();
                var url = xhlp.GetVMURL(ri, currency.ToLower(), null);
                var response = await client.GetAsync(url);

                var result = await response.Content.ReadAsStringAsync();
                var uri = await storeBlob(result, DateTime.UtcNow, "vm", currency, ri);

                return "Stored VM's at " + uri.AbsoluteUri;
        }
            public async Task<string> StoreMdisk(string region = "europe-west", string currency = "usd")
            {
            client = new HttpClient();

            mdisk mdk = new mdisk(configuration);
            excelAzureHelpers xhlp = new excelAzureHelpers(configuration);
            var url = mdisk.mdiskUrl + currency;
            var response = await client.GetAsync(url);

            var result = await response.Content.ReadAsStringAsync();
            var uri = await storeBlob(result, DateTime.UtcNow, "mdisk", currency, null);

            return "Stored mdisks at " + uri.AbsoluteUri;
        }

        public string Get(int id)
            {
                return "value";
            }

            private async Task<Uri> storeBlob(string blob, DateTime? input_date, string type, string currency, int? ri = null)
            {
                excelAzureHelpers xhlp = new excelAzureHelpers(configuration);

                DateTime date;
                if (!input_date.HasValue)
                {
                    date = DateTime.Now;
                }
                else
                {
                    date = (DateTime)input_date;
                }
                int year = date.Year;
                int month = date.Month;


                string storageConnectionString = configuration.GetValue<string>("BlobStorageSettings:blobConnectionString");
                string containerName = configuration.GetValue<string>("BlobStorageSettings:containerName");


                CloudStorageAccount storageAccount;
                if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
                {
                    // If the connection string is valid, proceed with operations against Blob storage here.
                }
                else
                {
                    // Otherwise, let the user know that they need to define the environment variable.
                    Debug.WriteLine(
                        "A connection string has not been defined in the system environment variables. " +
                        "Add a environment variable named 'storageconnectionstring' with your storage " +
                        "connection string as a value.");
                }

                // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                // Set the permissions so the blobs are public. 
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(xhlp.getBlobPath(date, currency, type, ri, false));

                if (!await cloudBlockBlob.ExistsAsync())
                {
                    cloudBlockBlob.Properties.ContentType = "application/json";

                    await cloudBlockBlob.UploadTextAsync(blob);
                }
                //           CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(xhlp.getBlobPath(date,currency,type,ri,false));

                return cloudBlockBlob.Uri;
            }


        }
    }
