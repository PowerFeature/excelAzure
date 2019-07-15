$ProgressPreference = "SilentlyContinue"
#$currencies = "USD"

$currencies = "USD","EUR","CHF","ARS","AUD","DKK","CAD","IDR","JPY","KRW","NZD","NOK","RUB","SAR","ZAR","SEK","TWD","TRY","GBP","MXN","MYR","INR","HKD","BRL"
$regions = "asia-pacific-east","asia-pacific-southeast","australia-central","australia-central-2","australia-east","australia-southeast","brazil-south","canada-central","canada-east","central-india","south-india","west-india","europe-north","europe-west","france-central","france-south","germany-central","germany-northeast","japan-east","japan-west","korea-central","korea-south","united-kingdom-south","united-kingdom-west","us-central","us-east","us-east-2","us-north-central","us-south-central","us-west-central","us-west","us-west-2","usgov-arizona","usgov-texas","usgov-virginia"
#$regionsreduced = "europe-north","europe-west","france-central","germany-central","us-central","us-east","us-east-2","us-north-central","us-south-central","us-west-central","us-west","us-west-2","usgov-virginia"

    foreach ($cur in $currencies) {
        dotnet excelAzureStore.dll vm 0 $cur
        dotnet excelAzureStore.dll vm 1 $cur
        dotnet excelAzureStore.dll vm 3 $cur
        dotnet excelAzureStore.dll mdisk $cur
    }