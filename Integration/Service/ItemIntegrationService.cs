using Integration.Common;
using Integration.Backend;
using System.Collections.Concurrent;
using StackExchange.Redis;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.

    public Result SaveItem(string itemContent)
    {
        // Check the backend to see if the content is already saved.
        if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        //Buğra Gündoğan - 09/28/2024
        //Distributed System Solution - This approach involves Redis.
        //Create key to acquire lock with:
        var key = Guid.NewGuid().ToString();
        var lockAcquired = RedisLock.AcquireLock(itemContent, key);
        
        if(!lockAcquired)
            return new Result(false, $"Duplicate item is being processed with content {itemContent} at {DateTime.Now}.");

        var item = ItemIntegrationBackend.SaveItem(itemContent);

        RedisLock.ReleaseLock(key);

        return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }


    //Buğra Gündoğan - 09/28/2024
    //Distributed System Solution, created a class that has methods for acquiring and return a Redis lock
    //Making this class static should be fine, since no shared instance value will be used.
    public static class RedisLock
    {
        private static string YOUR_REDIS_SERVER_AND_PORT = "localhost:6379";

        private static readonly Lazy<ConnectionMultiplexer> LazyConnection =
        new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(YOUR_REDIS_SERVER_AND_PORT));
        private static IDatabase _redisCache => LazyConnection.Value.GetDatabase();
        public static bool AcquireLock(string itemContent, string key)
        {
            bool lockAcquired = _redisCache.StringSet(key, itemContent, TimeSpan.FromSeconds(120), When.NotExists);
            return lockAcquired;
        }

        public static void ReleaseLock(string key)
        {
            _redisCache.KeyDelete(key);
        }
    }
}