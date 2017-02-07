namespace Core
{
    public class DbSettings
    {
        public string LogsConnString { get; set; }
    }

    public class CacheSettings
    {
        public string FinanceDataCacheInstance { get; set; }
        public string RedisConfiguration { get; set; }

        public string OrderBooksCacheKeyPattern { get; set; }

        public int RedisPort { get; set; }
        public string RedisInternalHost { get; set; }
    }

    public static class CacheSettingsExt
    {
        public static string GetOrderBookKey(this CacheSettings settings, string assetPairId, bool isBuy)
        {
            return string.Format(settings.OrderBooksCacheKeyPattern, assetPairId, isBuy);
        }
    }

    public class MatchingOrdersSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
        public RabbitMqSettings RabbitMq { get; set; }
    }

    public class RabbitMqSettings
    {
        public string Host { get; set; }
        public string ExternalHost { get; set; }
        public string ExchangeOrderbook { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }
    }

    public static class RabbitMqSettingsExt
    {
        public static string GetConnectionString (this RabbitMqSettings settings)
        {
            return $"amqp://{settings.Username}:{settings.Password}@{settings.Host}:{settings.Port}";
        }
    }

    public class IpEndpointSettings
    {
        public string InternalHost { get; set; }
    }

    public class BaseSettings
    {
        public DbSettings Db { get; set; }
        public MatchingOrdersSettings MatchingEngine { get; set; }
        public CacheSettings CacheSettings { get; set; }
    }
}
