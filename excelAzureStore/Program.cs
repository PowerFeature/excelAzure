using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Threading.Tasks;

namespace excelAzureStore
{
    class Program
    {
        static void Main(string[] args)
        {
           IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();


            calcStorage stor = new calcStorage(config);
            var output = "";
            switch (args[0])
            {
                case "vm":
                    Task<string> vmTask = Task.Run<string>(async () => await stor.StoreVMs(ri: int.Parse(args[1]), currency: args[2]));
                    while (!vmTask.IsCompleted)
                    {
                        vmTask.Wait();
                    }
                    
                    output=vmTask.Result;
                    break;
                case "mdisk":
                    Task<string> mDiskTask = Task.Run<string>(async () => await stor.StoreMdisk(currency: args[1]));
                    while (!mDiskTask.IsCompleted)
                    {
                        mDiskTask.Wait();
                    }
                    output=mDiskTask.Result;
                    break;


                default:
                    break;
            }
            Console.WriteLine(output);




        }
    }
}
