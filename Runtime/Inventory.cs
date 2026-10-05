using System;
using System.Collections;
using System.Collections.Generic;
using MelodySuite.Core.Runtime;
using UnityEngine;

namespace MelodySuite.Inventory.Runtime
{
    public abstract class Inventory<T, TI> : MonoBehaviour, IInventory<TI>
        where T : InventoryData<TI>
        where TI : IItem
    {
        [SerializeField]
        private T m_inventoryData;
        
        public float TotalWeight => m_inventoryData.TotalWeight;

        public UEvent<int> onSlotUpdated;
        public UEvent<int> onSlotRemoved;
        public UEvent<int> onSlotAdded;
        
        private void UnsubscribeFromEvents(T inventoryData)
        {
            if (inventoryData == null)
                throw new ArgumentNullException(nameof(inventoryData));
            
            inventoryData.OnSlotUpdated -= onSlotUpdated.Invoke;
            inventoryData.OnSlotRemoved -= onSlotRemoved.Invoke;
            inventoryData.OnSlotAdded -= onSlotAdded.Invoke;
        }

        private void SubscribeToEvents(T inventoryData)
        {
            if (inventoryData == null)
                throw new ArgumentNullException(nameof(inventoryData));
            
            inventoryData.OnSlotUpdated += onSlotUpdated.Invoke;
            inventoryData.OnSlotRemoved += onSlotRemoved.Invoke;
            inventoryData.OnSlotAdded += onSlotAdded.Invoke;
        }

        private void OnEnable()
        {
            if (m_inventoryData == null)
                return;
            SubscribeToEvents(m_inventoryData);
        }

        private void OnDisable()
        {
            if (m_inventoryData == null)
                return;
            UnsubscribeFromEvents(m_inventoryData);
        }
        
        public T InventoryData
        {
            get => m_inventoryData;
            set => SetInventory(value);
        }

        private void SetInventory(T inventoryData)
        {
            if (inventoryData == null)
                throw new ArgumentNullException(nameof(inventoryData));

            if (m_inventoryData != null)
            {
                UnsubscribeFromEvents(m_inventoryData);
            }
            
            m_inventoryData = inventoryData;
            SubscribeToEvents(inventoryData);
        }
        
        public bool AddItem(TI item, int amount)
        {
            return m_inventoryData.AddItem(item, amount);
        }

        public void RemoveItem(TI item, int amount)
        {
            m_inventoryData.RemoveItem(item, amount);
        }

        public IEnumerator<IReadOnlyInventorySlot<TI>> GetEnumerator()
        {
            return m_inventoryData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}