using Integration.Common;
using Integration.Backend;
using System.Collections.Concurrent;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.

    //Buğra Gündoğan - 09/28/2024
    //Single Server Solution, creating a concurrent dictionary to save items inside before calling the ItemIntegrationBackend.SaveItem method
    private static readonly ConcurrentDictionary<string, object> _processingItems = new ConcurrentDictionary<string, object>();

    public Result SaveItem(string itemContent)
    {
        // Check the backend to see if the content is already saved.
        if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        //Buğra Gündoğan - 09/28/2024
        //Single Server Solution
        var itemAdded = _processingItems.TryAdd(itemContent, itemContent);
        if(!itemAdded)
            return new Result(false, $"Item with content {itemContent} was not saved at {DateTime.Now} because it was being processed by another thread.");

        var item = ItemIntegrationBackend.SaveItem(itemContent);

        return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}