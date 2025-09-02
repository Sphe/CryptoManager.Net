
window.chart = {
    show: function(symbol, container, includeDetails) {
    new TradingView.widget(
        {
            "autosize": true,
            "symbol": symbol,
            "interval": "H",
            "timezone": "Etc/UTC",
            "theme": "dark",
            "style": "1",
            "locale": "en",
            "backgroundColor": "#161a1c",
            "enable_publishing": false,
            "hide_top_toolbar": true,
            "allow_symbol_change": false,
            "save_image": false,
            "container_id": container,
            "hide_legend": includeDetails,
            "details": includeDetails
        }
    );
}
}