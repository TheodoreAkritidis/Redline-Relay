using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe")]
public sealed class CraftingRecipe : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ItemDefinition Item;
        public int Amount;
    }

    [Header("UI")]
    public string DisplayName;

    [Header("Output")]
    public ItemDefinition OutputItem;
    public int OutputAmount = 1;

    [Header("Ingredients (max 4)")]
    public Ingredient[] Ingredients = new Ingredient[0];
}
