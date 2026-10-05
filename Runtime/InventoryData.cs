using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MelodySuite.Inventory.Runtime
{
    public abstract class InventoryData<T> : ScriptableObject, IInventory<T> where T : IItem
    {
        public List<InventorySlot<T>> slots = new();
        public InventoryType inventoryType;
        [Min(0)]
        public int maxSize;
        public float maxWeight;

        public event Action<int> OnSlotUpdated;
        public event Action<int> OnSlotRemoved;
        public event Action<int> OnSlotAdded;

        public IReadOnlyInventorySlot<T> GetSlotAt(int i)
        {
            return slots[i];
        }
        
        public bool HasItem(T item, int amount = 1)
        {
            var total = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && EqualityComparer<T>.Default.Equals(slot.Item, item))
                {
                    total += slot.Amount;
                }
            }
            return total >= amount;
        }
        
        public void Set(InventoryType type, List<InventorySlot<T>> s)
        {
            
            inventoryType = type;
            slots = s;
            
            switch (inventoryType)
            {
                case InventoryType.Preallocated:
                {
                    while (slots.Count > maxSize)
                    {
                        slots.RemoveAt(slots.Count - 1);
                    }

                    while (slots.Count < maxSize)
                    {
                        slots.Add(new InventorySlot<T>());
                    }

                    break;
                }
                case InventoryType.Dynamic or InventoryType.Weighted:
                    slots.RemoveAll(i => i == null || i.IsEmpty);
                    break;
            }
        }

        public List<IReadOnlyInventorySlot<T>> GetSlots(T item)
        {
            List<IReadOnlyInventorySlot<T>> result = new List<IReadOnlyInventorySlot<T>>();
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && EqualityComparer<T>.Default.Equals(slot.Item, item))
                {
                    result.Add(slot);
                }
            }
            return result;
        }
        
        public IReadOnlyInventorySlot<T> GetFirstSlot(T item)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && EqualityComparer<T>.Default.Equals(slot.Item, item))
                {
                    return slot;
                }
            }

            return null;
        }
        
        // public bool allowAddingItemsWhileOverEncumbered = true;
        
        public float TotalWeight =>
            slots.Sum(slot =>
                slot is { Item: IWeighted weighted }
                    ? weighted.Weight * slot.Amount
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
                    !slot.IsEmpty &&
                    EqualityComparer<T>.Default.Equals(slot.Item, item))
                {
                    existing = slot;
                    slotIndex = i;
                    break;
                }
            }

            if (existing == null)
                throw new InvalidOperationException();
            
            existing.Remove(amount);

            if (existing.Amount > 0)
                return;
            
            switch (inventoryType)
            {
                case InventoryType.Dynamic:
                case InventoryType.Weighted:
                    slots.Remove(existing);
                    OnSlotRemoved?.Invoke(slotIndex);
                    break;
                case InventoryType.Preallocated:
                    existing.Clear();
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
                    !slot.IsEmpty &&
                    EqualityComparer<T>.Default.Equals(slot.Item, item))
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
         
                existing.Add(amount);
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
            var slot = new InventorySlot<T>();
            slot.Set(item, amount);
            slots.Add(slot);
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
            var slot = new InventorySlot<T>();
            slot.Set(item, amount);
            slots.Add(slot);
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
                if (inventorySlot.IsEmpty)
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

            empty.Set(item, amount);
            OnSlotUpdated?.Invoke(slotIndex);
            return true;
        }
        
        private void Awake()
        {
            if (inventoryType is InventoryType.Dynamic or InventoryType.Weighted)
                slots.RemoveAll(i => i == null || i.IsEmpty);

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
            return s != null && !s.IsEmpty;
        }

        public IEnumerator<IReadOnlyInventorySlot<T>> GetEnumerator()
        {
            return slots.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    public interface IInventory<T> : IEnumerable<IReadOnlyInventorySlot<T>> where T : IItem
    {
        public float TotalWeight { get; }
        public bool AddItem(T item, int amount = 1);
    }

    public interface IReadOnlyInventorySlot<out T> where T : IItem
    {
        T Item { get; }
        int Amount { get; }
        bool IsEmpty { get; }
    }
    
    [Serializable]
    public class InventorySlot<T> : IReadOnlyInventorySlot<T> where T : IItem 
    {
        [SerializeField]
        private T item;

        [SerializeField]
        private int amount;
        
        internal void Set(T item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        internal void Clear()
        {
            item = default;
            amount = 0;
        }

        internal void Add(int amount)
        {
            this.amount += amount;
        }

        internal void Remove(int amount)
        {
            this.amount -= amount;
        }
        
        public T Item => item;
        public int Amount => amount;

        public bool IsEmpty => item == null || !item.IsValid || amount <= 0;
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
    

}