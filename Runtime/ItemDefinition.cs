using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Inventory.Runtime;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public float weight;

    public string ID
    {
        get
        {
            var id = name.ToLower().Replace(" ", "_");
            if (!Regex.IsMatch(id, @"^[A-Za-z0-9_]+$"))
                throw new ArgumentException();
            return id;
        }
    }
    private void OnValidate()
    {
        var id = name.ToLower().Replace(" ", "_");
        if (!Regex.IsMatch(id, @"^[A-Za-z0-9_]+$"))
            Debug.LogWarning("Invalid ID");
    }
}

[Serializable]
public class ItemInstance : IItem, IWeighted
{
    [SerializeField]
    private ItemDefinition item;

    public ItemInstance(ItemDefinition item)
    {
        this.item = item;
    }

    public float Weight => item.weight;
    
    public bool IsValid => item;

    public override bool Equals(object obj)
    {
        if (obj is ItemInstance other)
        {
            return item.ID == other.item.ID;
        }

        return false;
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return item.ID.GetHashCode();
    }
}