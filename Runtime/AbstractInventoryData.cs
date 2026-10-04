using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inventory.Runtime
{
    public abstract class AbstractInventoryData<T> : ScriptableObject, IInventory<T> where T : IItem
    {
        public List<InventorySlot<T>> slots = new();
        public InventoryType inventoryType;
        [Min(0)]
        public int maxSize;
        public float maxWeight;

        public event Action<int> OnSlotUpdated;
        public event Action<int> OnSlotRemoved;
        public event Action<int> OnSlotAdded;
        
        // public bool allowAddingItemsWhileOverEncumbered = true;
        
        public float TotalWeight =>
            slots.Sum(slot =>
                slot is { item: IWeighted weighted }
                    ? weighted.Weight * slot.amount
                    : 0f);

        private void OnValidate()
        {
            if (inventoryType == InventoryType.Preallocated)
            {
                while (slots.Count > maxSize)
                {
                    slots.RemoveAt(slots.Count - 1);
                }

                while (slots.Count < maxSize)
                {
                    slots.Add(new InventorySlot<T>());
                }
            }
        }

        private bool CanAddWeight(T item, int amount)
        {
            if (inventoryType != InventoryType.Weighted)
                return true;

            if (item is not IWeighted weighted)
                return true;

            return TotalWeight + weighted.Weight * amount <= maxWeight;
        }

        public void RemoveItem(T item, int amount = 1)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            InventorySlot<T> existing = null;
            var slotIndex = -1;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null &&
                    !slot.IsEmpty() &&
                    EqualityComparer<T>.Default.Equals(slot.item, item))
                {
                    existing = slot;
                    slotIndex = i;
                    break;
                }
            }

            if (existing == null)
                throw new InvalidOperationException();
            
            existing.amount -= amount;

            if (existing.amount > 0)
                return;
            
            switch (inventoryType)
            {
                case InventoryType.Dynamic:
                case InventoryType.Weighted:
                    slots.Remove(existing);
                    OnSlotRemoved?.Invoke(slotIndex);
                    break;
                case InventoryType.Preallocated:
                    existing.item = default;
                    existing.amount = 0;
                    OnSlotUpdated?.Invoke(slotIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inventoryType));
            }
        } 
        
        public bool AddItem(T item, int amount = 1)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            InventorySlot<T> existing = null;
            var slotIndex = -1;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null &&
                    !slot.IsEmpty() &&
                    EqualityComparer<T>.Default.Equals(slot.item, item))
                {
                    existing = slot;
                    slotIndex = i;
                    break;
                }
            }

            if (existing != null)
            {
                if (!CanAddWeight(item, amount))
                {
                    return false;
                }
         
                existing.amount += amount;
                OnSlotUpdated?.Invoke(slotIndex);
                return true;
            }
            
            if (inventoryType is InventoryType.Dynamic or InventoryType.Weighted)
                slots.RemoveAll(s => !ValidateSlot(s));

            return inventoryType switch
            {
                InventoryType.Dynamic => AddNewItemDynamic(item, amount),
                InventoryType.Preallocated => AddNewItemPreallocated(item, amount),
                InventoryType.Weighted => AddNewItemWeighted(item, amount),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private bool AddNewItemDynamic(T item, int amount)
        {
            if (slots.Count >= maxSize)
                return false;
            var slotIndex = slots.Count;
            slots.Add(new InventorySlot<T>()
            {
                item = item,
                amount = amount,
            });
            OnSlotAdded?.Invoke(slotIndex);
            return true;
        }

        private bool AddNewItemWeighted(T item, int amount)
        {
            if (slots.Count >= maxSize)
                return false;
            if (!CanAddWeight(item, amount))
                return false;

            var slotIndex = slots.Count;
            slots.Add(new InventorySlot<T>()
            {
                item = item,
                amount = amount,
            });
            OnSlotAdded?.Invoke(slotIndex);
            return true;
        }

        private bool AddNewItemPreallocated(T item, int amount)
        {
            InventorySlot<T> empty = null;
            int slotIndex = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                var inventorySlot = slots[i];
                if (inventorySlot.IsEmpty())
                {
                    empty = inventorySlot;
                    slotIndex = i;
                    break;
                }
            }

            if (empty == null)
            {
                return false;
            }

            empty.item = item;
            empty.amount = amount;
            OnSlotUpdated?.Invoke(slotIndex);
            return true;
        }
        
        private void Awake()
        {
            if (inventoryType is InventoryType.Dynamic or InventoryType.Weighted)
                slots.RemoveAll(i => i == null || i.item == null);

            // if (!allowAddingItemsWhileOverEncumbered)
            // {
            //     if (inventoryType == InventoryType.Weighted)
            //     {
            //         while (TotalWeight > maxWeight && slots.Count > 0)
            //             slots.RemoveAt(slots.Count - 1);
            //     }
            // }
        }

        private static bool ValidateSlot(InventorySlot<T> s)
        {
            return s != null && !s.IsEmpty();
        }
        
    }

    public interface IInventory<T> where T : IItem
    {
        public float TotalWeight { get; }
        public bool AddItem(T item, int amount = 1);
    }

    public interface IItem
    {
        bool IsValid { get; }
    }
    
    public interface IWeighted
    {
        public float Weight { get; }
    }
    
    public enum InventoryType
    {
        Dynamic,
        Preallocated,
        Weighted
    }
    
    [Serializable]
    public class InventorySlot<T> where T : IItem
    {
        public T item;
        public int amount;

        public bool IsEmpty()
        {
            return item == null || !item.IsValid || amount <= 0;
        }
    }
}