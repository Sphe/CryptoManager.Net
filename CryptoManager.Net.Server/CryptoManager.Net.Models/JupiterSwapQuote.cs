using System.Text.Json.Serialization;

namespace CryptoManager.Net.Models
{
    public class JupiterSwapQuote
    {
        [JsonPropertyName("inputMint")]
        public string InputMint { get; set; } = string.Empty;

        [JsonPropertyName("inAmount")]
        public string InAmount { get; set; } = string.Empty;

        [JsonPropertyName("outputMint")]
        public string OutputMint { get; set; } = string.Empty;

        [JsonPropertyName("outAmount")]
        public string OutAmount { get; set; } = string.Empty;

        [JsonPropertyName("otherAmountThreshold")]
        public string OtherAmountThreshold { get; set; } = string.Empty;

        [JsonPropertyName("swapMode")]
        public string SwapMode { get; set; } = string.Empty;

        [JsonPropertyName("slippageBps")]
        public int SlippageBps { get; set; }

        [JsonPropertyName("platformFee")]
        public object? PlatformFee { get; set; }

        [JsonPropertyName("priceImpactPct")]
        public string PriceImpactPct { get; set; } = string.Empty;

        [JsonPropertyName("routePlan")]
        public JupiterRoutePlan[] RoutePlan { get; set; } = Array.Empty<JupiterRoutePlan>();

        [JsonPropertyName("contextSlot")]
        public long ContextSlot { get; set; }

        [JsonPropertyName("timeTaken")]
        public double TimeTaken { get; set; }

        [JsonPropertyName("swapUsdValue")]
        public string SwapUsdValue { get; set; } = string.Empty;

        [JsonPropertyName("simplerRouteUsed")]
        public bool SimplerRouteUsed { get; set; }

        [JsonPropertyName("mostReliableAmmsQuoteReport")]
        public JupiterQuoteReport? MostReliableAmmsQuoteReport { get; set; }

        [JsonPropertyName("useIncurredSlippageForQuoting")]
        public object? UseIncurredSlippageForQuoting { get; set; }

        [JsonPropertyName("otherRoutePlans")]
        public object? OtherRoutePlans { get; set; }

        [JsonPropertyName("aggregatorVersion")]
        public object? AggregatorVersion { get; set; }

        [JsonPropertyName("loadedLongtailToken")]
        public bool LoadedLongtailToken { get; set; }
    }

    public class JupiterRoutePlan
    {
        [JsonPropertyName("swapInfo")]
        public JupiterSwapInfo SwapInfo { get; set; } = new();

        [JsonPropertyName("percent")]
        public int Percent { get; set; }

        [JsonPropertyName("bps")]
        public int Bps { get; set; }
    }

    public class JupiterSwapInfo
    {
        [JsonPropertyName("ammKey")]
        public string AmmKey { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("inputMint")]
        public string InputMint { get; set; } = string.Empty;

        [JsonPropertyName("outputMint")]
        public string OutputMint { get; set; } = string.Empty;

        [JsonPropertyName("inAmount")]
        public string InAmount { get; set; } = string.Empty;

        [JsonPropertyName("outAmount")]
        public string OutAmount { get; set; } = string.Empty;

        [JsonPropertyName("feeAmount")]
        public string FeeAmount { get; set; } = string.Empty;

        [JsonPropertyName("feeMint")]
        public string FeeMint { get; set; } = string.Empty;
    }

    public class JupiterQuoteReport
    {
        [JsonPropertyName("info")]
        public Dictionary<string, string> Info { get; set; } = new();
    }
}
