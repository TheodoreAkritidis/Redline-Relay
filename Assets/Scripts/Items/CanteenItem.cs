using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Items/Canteen Item")]
public class CanteenItem : ItemDefinition
{
    [Header("Canteen")]
    [SerializeField] public float MaxCapacity = 100f;
    [SerializeField] public float ConsumeAmount = 25f;
    [SerializeField] public float FillAmount = 100f;
}