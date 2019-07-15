using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace excelInstance
{

    [Serializable]
    public class Vm
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
        public Vm() { }

    }
    [Serializable]
    public class VmList
    {
        public string region = "";
        public string currency = "";

        public List<Vm> vms;

        public VmList() { }

    }
    [Serializable]
    public class Mdisk
    {
        public string name;
        public int size;
        public int? iops;
        public int? speed;
        public decimal? pricemonth;
        public decimal? priceyear;
        public string currency;
        public decimal? transactionPrice;
        public decimal? snapshotPrice;
        public decimal? snapshotPriceLrs;
        public decimal? snapshotPriceZrs;
        public string displayName;
        public bool? isPreview;
    }
    [Serializable]
    public class DboMdisks
    {
        public List<Mdisk> mdisks;
        public DateTime date;
        public string region;
        public string currency;

    }


}
