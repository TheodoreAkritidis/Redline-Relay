// File: ItemDefinition.cs
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Survival/Items/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string ItemId;

    [Header("UI")]
    public Sprite Icon;

    [Header("Stacking")]
    [Min(1)] public int MaxStack = 1;
}