using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class PoolPairs
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;

        [BsonElement("asset")]
        public string Asset { get; set; } = string.Empty;

        [BsonElement("contractAddress")]
        public string ContractAddress { get; set; } = string.Empty;

        [BsonElement("routePlan")]
        public JupiterRoutePlan[] RoutePlan { get; set; } = Array.Empty<JupiterRoutePlan>();

        [BsonElement("inputMint")]
        public string InputMint { get; set; } = string.Empty;

        [BsonElement("outputMint")]
        public string OutputMint { get; set; } = string.Empty;

        [BsonElement("inAmount")]
        public string InAmount { get; set; } = string.Empty;

        [BsonElement("outAmount")]
        public string OutAmount { get; set; } = string.Empty;

        [BsonElement("priceImpactPct")]
        public string PriceImpactPct { get; set; } = string.Empty;

        [BsonElement("swapUsdValue")]
        public string SwapUsdValue { get; set; } = string.Empty;

        [BsonElement("routeCount")]
        public int RouteCount { get; set; }

        [BsonElement("updateTime")]
        public DateTime UpdateTime { get; set; }

        [BsonElement("lastProcessed")]
        public DateTime LastProcessed { get; set; }
    }

    public class JupiterRoutePlan
    {
        [BsonElement("swapInfo")]
        public JupiterSwapInfo SwapInfo { get; set; } = new();

        [BsonElement("percent")]
        public int Percent { get; set; }

        [BsonElement("bps")]
        public int Bps { get; set; }
    }

    public class JupiterSwapInfo
    {
        [BsonElement("ammKey")]
        public string AmmKey { get; set; } = string.Empty;

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("inputMint")]
        public string InputMint { get; set; } = string.Empty;

        [BsonElement("outputMint")]
        public string OutputMint { get; set; } = string.Empty;

        [BsonElement("inAmount")]
        public string InAmount { get; set; } = string.Empty;

        [BsonElement("outAmount")]
        public string OutAmount { get; set; } = string.Empty;

        [BsonElement("feeAmount")]
        public string FeeAmount { get; set; } = string.Empty;

        [BsonElement("feeMint")]
        public string FeeMint { get; set; } = string.Empty;
    }
}
