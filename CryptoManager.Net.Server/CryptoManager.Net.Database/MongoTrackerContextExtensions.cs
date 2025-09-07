using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Database.Projections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CryptoManager.Net.Database
{
    public static class MongoTrackerContextExtensions
    {
        public static async Task<List<ExchangeUserValue>> AllUserExchangeBalancesAsync(this MongoTrackerContext context)
        {
            var pipeline = new[]
            {
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "assetStats" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "assetStats" }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "_id" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { "$total", 0 }) },
                        { "then", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$assetStats"), 0 }) },
                            { "then", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$eq", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.exchange", 0 }), "$exchange" }) },
                                { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.value", 0 }) }) },
                                { "else", new BsonDocument("$cond", new BsonDocument
                                {
                                    { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                    { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                    { "else", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.value", 0 }) }) }
                                }) }
                            }) },
                            { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                { "else", 0 }
                            }) }
                        }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$userId" },
                    { "usdValue", new BsonDocument("$sum", "$usdValue") }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "userId", "$_id" },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserBalances.Aggregate<ExchangeUserValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<List<ExchangeUserValue>> AllUserExternalBalancesAsync(this MongoTrackerContext context)
        {
            var pipeline = new[]
            {
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "assetStats" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "assetStats" }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "_id" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { "$total", 0 }) },
                        { "then", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$assetStats"), 0 }) },
                            { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$avg", "$assetStats.value") }) },
                            { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                { "else", 0 }
                            }) }
                        }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$userId" },
                    { "usdValue", new BsonDocument("$sum", "$usdValue") }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "userId", "$_id" },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserExternalBalances.Aggregate<ExchangeUserValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<List<ExchangeBalanceValue>> UserExchangeBalancesAsync(this MongoTrackerContext context, string userId, string? exchange = null)
        {
            var matchFilter = new BsonDocument
            {
                { "userId", userId },
                { "total", new BsonDocument("$gt", 0) }
            };

            if (!string.IsNullOrEmpty(exchange))
            {
                matchFilter.Add("exchange", exchange);
            }

            var pipeline = new[]
            {
                new BsonDocument("$match", matchFilter),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "assetStats" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "assetStats" }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "_id" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { "$total", 0 }) },
                        { "then", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$assetStats"), 0 }) },
                            { "then", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$eq", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.exchange", 0 }), "$exchange" }) },
                                { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.value", 0 }) }) },
                                { "else", new BsonDocument("$cond", new BsonDocument
                                {
                                    { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                    { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                    { "else", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.value", 0 }) }) }
                                }) }
                            }) },
                            { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                { "else", 0 }
                            }) }
                        }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$sort", new BsonDocument("usdValue", -1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "exchange", 1 },
                    { "asset", 1 },
                    { "available", 1 },
                    { "total", 1 },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserBalances.Aggregate<ExchangeBalanceValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<List<ExternalBalanceValue>> UserExternalBalancesAsync(this MongoTrackerContext context, string userId)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("userId", userId)),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "assetStats" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "assetStats" }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "_id" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { "$total", 0 }) },
                        { "then", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$assetStats"), 0 }) },
                            { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$avg", "$assetStats.value") }) },
                            { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                { "else", 0 }
                            }) }
                        }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$sort", new BsonDocument("usdValue", -1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "id", "$_id" },
                    { "asset", 1 },
                    { "total", 1 },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserExternalBalances.Aggregate<ExternalBalanceValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<List<ExchangeBalanceValue>> UserTotalExchangeAssetBalancesAsync(this MongoTrackerContext context, string userId)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "userId", userId },
                    { "total", new BsonDocument("$gt", 0) }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "assetStats" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "assetStats" }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "_id" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { "$total", 0 }) },
                        { "then", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$assetStats"), 0 }) },
                            { "then", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$eq", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.exchange", 0 }), "$exchange" }) },
                                { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.value", 0 }) }) },
                                { "else", new BsonDocument("$cond", new BsonDocument
                                {
                                    { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                    { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                    { "else", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$assetStats.value", 0 }) }) }
                                }) }
                            }) },
                            { "else", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                                { "then", new BsonDocument("$divide", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                                { "else", 0 }
                            }) }
                        }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$asset" },
                    { "total", new BsonDocument("$sum", "$total") },
                    { "usdValue", new BsonDocument("$sum", "$usdValue") }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "asset", "$_id" },
                    { "available", 0.1m },
                    { "exchange", "-" }
                }),
                new BsonDocument("$sort", new BsonDocument("usdValue", -1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "asset", 1 },
                    { "total", 1 },
                    { "usdValue", 1 },
                    { "available", 1 },
                    { "exchange", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserBalances.Aggregate<ExchangeBalanceValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<IEnumerable<ExchangeBalanceValue>> UserExchangeBalances(this MongoTrackerContext context, int userId, string? exchange = null)
        {
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", new BsonDocument("userId", userId)),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                        { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "exchange", 1 },
                    { "asset", 1 },
                    { "available", 1 },
                    { "total", 1 },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            if (!string.IsNullOrEmpty(exchange))
            {
                pipeline.Insert(1, new BsonDocument("$match", new BsonDocument("exchange", exchange)));
            }

            var result = await context.UserBalances.Aggregate<ExchangeBalanceValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<IEnumerable<ExternalBalanceValue>> UserExternalBalances(this MongoTrackerContext context, int userId)
        {
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", new BsonDocument("userId", userId)),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                        { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "id", "$_id" },
                    { "asset", 1 },
                    { "total", 1 },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserExternalBalances.Aggregate<ExternalBalanceValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task<IEnumerable<ExchangeBalanceValue>> UserTotalExchangeAssetBalances(this MongoTrackerContext context, int userId)
        {
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", new BsonDocument("userId", userId)),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "fiatPrices" },
                    { "localField", "asset" },
                    { "foreignField", "asset" },
                    { "as", "fiatPrices" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "usdValue", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$fiatPrices"), 0 }) },
                        { "then", new BsonDocument("$multiply", new BsonArray { "$total", new BsonDocument("$arrayElemAt", new BsonArray { "$fiatPrices.price", 0 }) }) },
                        { "else", 0 }
                    }) }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$asset" },
                    { "asset", new BsonDocument("$first", "$asset") },
                    { "total", new BsonDocument("$sum", "$total") },
                    { "usdValue", new BsonDocument("$sum", "$usdValue") }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "asset", 1 },
                    { "total", 1 },
                    { "usdValue", 1 },
                    { "_id", 0 }
                })
            };

            var result = await context.UserBalances.Aggregate<ExchangeBalanceValue>(pipeline).ToListAsync();
            return result;
        }

        public static async Task BulkInsertOrUpdateAsync<T>(this MongoTrackerContext context, IEnumerable<T> items, object? bulkConfig = null) where T : class
        {
            var collection = context.GetCollection<T>();
            var writeModels = new List<WriteModel<T>>();

            foreach (var item in items)
            {
                var idProperty = typeof(T).GetProperty("Id");
                if (idProperty != null)
                {
                    var idValue = idProperty.GetValue(item);
                    if (idValue != null)
                    {
                        var filter = Builders<T>.Filter.Eq("_id", idValue);
                        writeModels.Add(new ReplaceOneModel<T>(filter, item) { IsUpsert = true });
                    }
                }
            }

            if (writeModels.Any())
            {
                await collection.BulkWriteAsync(writeModels);
            }
        }

        private static IMongoCollection<T> GetCollection<T>(this MongoTrackerContext context) where T : class
        {
            var collectionName = typeof(T).Name.ToLowerInvariant();
            if (collectionName.EndsWith("s"))
                collectionName = collectionName.Substring(0, collectionName.Length - 1) + "s";
            
            // Use reflection to get the database from the private field
            var databaseField = typeof(MongoTrackerContext).GetField("_database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var database = (IMongoDatabase)databaseField!.GetValue(context)!;
            
            return database.GetCollection<T>(collectionName);
        }
    }
}
