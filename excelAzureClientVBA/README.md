

# excelAzureVmPricing
This VBA project allows you to find the cheapst VM size for a given Core/Ram configuration in a specific datacenter.
You can then pull an hour price for the VM. 

The solution relies on a custom backend, that pulls data from https://azure.microsoft.com/api/v2/pricing

NEW support for managed disks. See the example.xlsm.


![Demoimage](https://raw.githubusercontent.com/KillerFeature/excelAzure/master/excelAzureClientVBA/Capture3.PNG)

# Installation
1. Download the VM_Prices.bas (https://raw.githubusercontent.com/KillerFeature/excelAzure/master/excelAzureClientVBA/VM_Prices.bas?raw=true)
2. Open Excel
3. Press Alt-F11 to go to Macro Editor
4. Select "File" -> "Import Module"
5. Select the VM_Prices.bas file
6. Select "Tools" -> "References..."
7. Check "Microsoft XML, v6.0"
8. Press Alt-F11 to go back to Excel
9. Enter this in a cell =getVM(1;1;0;"europe-west";"EUR") the resulting VM should be linux-b1s-standard
10. Move to the next cell and type =getVMPriceHour("linux-b1s-standard";0;"europe-west";"EUR") the result should be something like 0,07929684

# Function syntax

=getVM([minimum cores];[minimum ram];[reserved instance years 0 or 1 or 3];[azure-region];[currency];[OPTIONAL exclude strings seperated by ;];[OPTIONAL must include strings seperated by ;])

Optionally you can exclude certain vm's by using semicolon seperated tags

=getVM([minimum cores];[minimum ram];[reserved instance years 0 or 1 or 3];[azure-region];[currency];"-b;-a")
This will exclude all burstable VM's, and all A series

=getVM([minimum cores];[minimum ram];[reserved instance years 0 or 1 or 3];[azure-region];[currency];"-b;-a";"windows")
This will exclude all burstable VM's, and all A series and select a windows VM.

=getVMPriceHour([VM Name (result from getVM)];[reserved instance];[azure region];[currency])

=getVMData([VM Name];[Region];[Currency];[Parameter you want returned])

Example:
=getVMData("linux-b2s-standard";"us-east";"USD";"isVcpu")

Supported parameters : isVCPU cores ram (DO NOT USE getVMData to get prices)

How to calculate monthly fee?
=[Hour price]*730


See demovideo here : [video](https://github.com/KillerFeature/excelAzure/raw/master/excelAzureClientVBA/comp.mp4?raw=true)



# Disclaimer
The is not for quotes. Use this tool at your own risk. Prices are cached for 48 hours and might be outdated.
This is an expert tool. A fool with a tool is still a fool. 

