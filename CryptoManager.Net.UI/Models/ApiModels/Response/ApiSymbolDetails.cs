using CryptoExchange.Net.SharedApis;

namespace CryptoManager.Net.UI.Models.ApiModels.Response
{
    public class ApiSymbolDetails : ApiSymbol
    {
        public decimal? MinTradeQuantity { get; set; }
        public decimal? MinNotionalValue { get; set; }
        public decimal? QuantityStep { get; set; }
        public decimal? PriceStep { get; set; }
        public int? QuantityDecimals { get; set; }
        public int? PriceDecimals { get; set; }
        public int? PriceSignificantFigures { get; set; }
        public bool SupportPlacement { get; set; }
        public SharedFeeAssetType? FeeAssetType { get; set; }
        public SharedFeeDeductionType? FeeDeductionType { get; set; }
        public SharedTimeInForce[]? SupportedTimeInForces { get; set; }
        public SharedOrderType[]? SupportedOrderTypes { get; set; }
        public SharedQuantitySupport? SupportedQuantities { get; set; }
    }
}
