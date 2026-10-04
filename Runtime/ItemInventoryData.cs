using System;
using UnityEngine;

namespace Inventory.Runtime
{
    [CreateAssetMenu(fileName = "InventoryData", menuName = "Inventory/Item Inventory Data")]
    public class ItemInventoryData : AbstractInventoryData<ItemInstance>
    {
    }
}
