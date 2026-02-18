using System.Collections.Generic;
using UnityEngine;

public sealed class CraftingRecipeDatabase : MonoBehaviour
{
    [SerializeField] private List<CraftingRecipe> recipes = new();

    public IReadOnlyList<CraftingRecipe> Recipes => recipes;
}
