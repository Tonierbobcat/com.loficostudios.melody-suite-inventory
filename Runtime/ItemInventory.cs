namespace Inventory.Runtime
{
    public class ItemInventory : Inventory<ItemInventoryData, ItemInstance>
    {
        public void AddItem(ItemDefinition item, int amount = 1)
        {
            AddItem(new ItemInstance(item), amount);
        }

        public void RemoveItem(ItemDefinition item, int amount = 1)
        {
            RemoveItem(new ItemInstance(item), amount);
        }
    }
}