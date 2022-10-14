using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;

namespace netCoreApiRedis.Services
{
    public class CacheService : ICacheService
    {
        StackExchange.Redis.IDatabase _cachedDb;
        public CacheService(StackExchange.Redis.IDatabase cachedDb)
        {
            // var redis = ConnectionMultiplexer.Connect("localhost:6379");
            // _cachedDb = redis.GetDatabase();
            this._cachedDb = cachedDb;
        }
        public async Task<T> GetDataAsync<T>(string key)
        {
            var value = await _cachedDb.StringGetAsync(key);
            if (!string.IsNullOrEmpty(value))
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            return default;
        }

        public async Task<object> RemoveDataAsync(string key)
        {
            var existKey = await _cachedDb.KeyExistsAsync(key);
            if (existKey)
            {
                return await _cachedDb.KeyDeleteAsync(key);
            }
            return false;
        }
        public async Task<bool> SetDataAsync<T>(string key, T value, DateTimeOffset expirationTime)
        {
            var expireTime = expirationTime.DateTime.Subtract(DateTime.Now);
            var isSet = await _cachedDb.StringSetAsync(key, JsonSerializer.Serialize<T>(value), expireTime);
            return isSet;
        }
    }
}