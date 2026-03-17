using System;
using UnityEngine;

public sealed class SmelterComponent : MonoBehaviour
{
    // Slot indices:
    // 0 = Ore, 1 = Fuel, 2 = Output
    public const int OreSlot = 0;
    public const int FuelSlot = 1;
    public const int OutputSlot = 2;

    [Header("Storage")]
    [SerializeField] private string containerId = "Smelter";

    // IMPORTANT:
    // - NOT serialized (so Unity won't overwrite it from Inspector/scene)
    // - Renamed field so old serialized data ("container"/"slots") won't apply.
    [NonSerialized] private ArrayItemContainer runtimeContainer;

    [Header("Runtime")]
    [SerializeField] private float fuelSecondsRemaining;
    [SerializeField] private float oreSecondsProgress;

    public event Action SmelterChanged;

    public IItemContainer Container => runtimeContainer;

    private void Awake()
    {
        EnsureContainer();
    }

    private void OnEnable()
    {
        EnsureContainer();
    }

    private void EnsureContainer()
    {
        // If anything is missing or wrong-sized, rebuild to exactly 3 slots.
        if (runtimeContainer == null || runtimeContainer.SlotCount != 3)
            runtimeContainer = new ArrayItemContainer(containerId, 3);
    }

    private void Update()
    {
        EnsureContainer();

        bool changed = TickSmelting(Time.deltaTime);
        if (changed) NotifyChanged();
    }

    public void NotifyChanged() => SmelterChanged?.Invoke();

    public float GetTimeToSmeltFullOreStackSeconds()
    {
        var ore = runtimeContainer.GetSlot(OreSlot);
        if (ore.IsEmpty || ore.Item == null || !ore.Item.IsOre || ore.Item.SmeltResult == null) return 0f;

        float per = Mathf.Max(0.01f, ore.Item.SmeltSecondsPerItem);
        return ore.Quantity * per;
    }

    public float GetTotalFuelTimeSeconds()
    {
        var fuel = runtimeContainer.GetSlot(FuelSlot);
        if (fuel.IsEmpty || fuel.Item == null || !fuel.Item.IsFuel) return Mathf.Max(0f, fuelSecondsRemaining);

        float per = Mathf.Max(0f, fuel.Item.FuelSeconds);
        return Mathf.Max(0f, fuelSecondsRemaining) + (fuel.Quantity * per);
    }

    private bool TickSmelting(float dt)
    {
        if (dt <= 0f) return false;

        bool changed = false;

        // Burn down current fuel.
        if (fuelSecondsRemaining > 0f)
        {
            fuelSecondsRemaining -= dt;
            if (fuelSecondsRemaining < 0f) fuelSecondsRemaining = 0f;
            changed = true;
        }

        // If no remaining fuel, consume 1 unit of fuel from the fuel slot.
        if (fuelSecondsRemaining <= 0f)
        {
            var fuel = runtimeContainer.GetSlot(FuelSlot);
            if (!fuel.IsEmpty && fuel.Item != null && fuel.Item.IsFuel && fuel.Item.FuelSeconds > 0f && fuel.Quantity > 0)
            {
                fuel.Quantity -= 1;
                runtimeContainer.SetSlot(FuelSlot, fuel);

                fuelSecondsRemaining += fuel.Item.FuelSeconds;
                changed = true;
            }
        }

        // Need fuel to smelt. If fuel is gone, reset partial smelt progress.
        if (fuelSecondsRemaining <= 0f)
        {
            if (oreSecondsProgress != 0f)
            {
                oreSecondsProgress = 0f;
                changed = true;
            }
            return changed;
        }

        var ore = runtimeContainer.GetSlot(OreSlot);
        if (ore.IsEmpty || ore.Item == null || !ore.Item.IsOre || ore.Item.SmeltResult == null || ore.Quantity <= 0)
        {
            if (oreSecondsProgress != 0f) { oreSecondsProgress = 0f; changed = true; }
            return changed;
        }

        // Check output capacity.
        ItemDefinition result = ore.Item.SmeltResult;
        var output = runtimeContainer.GetSlot(OutputSlot);
        int outMax = InventoryRules.GetMaxStack(result);

        bool outputCanAccept =
            output.IsEmpty ||
            (output.Item == result && output.Quantity < outMax);

        if (!outputCanAccept)
            return changed;

        // Progress smelt for one ore item at a time.
        float perOre = Mathf.Max(0.01f, ore.Item.SmeltSecondsPerItem);

        oreSecondsProgress += dt;
        changed = true;

        while (oreSecondsProgress >= perOre)
        {
            oreSecondsProgress -= perOre;

            // Consume 1 ore.
            ore.Quantity -= 1;
            if (ore.Quantity <= 0) ore.Clear();
            runtimeContainer.SetSlot(OreSlot, ore);

            // Produce 1 output.
            output = runtimeContainer.GetSlot(OutputSlot);
            if (output.IsEmpty)
                output = new ItemStack(result, 1);
            else
                output.Quantity += 1;

            runtimeContainer.SetSlot(OutputSlot, output);

            changed = true;

            // Stop if ore ended or output filled.
            ore = runtimeContainer.GetSlot(OreSlot);
            if (ore.IsEmpty) break;

            output = runtimeContainer.GetSlot(OutputSlot);
            if (!output.IsEmpty && output.Item == result && output.Quantity >= outMax) break;
        }

        return changed;
    }

    public float FuelSecondsRemaining => Mathf.Max(0f, fuelSecondsRemaining);

    public bool TryGetCurrentSmeltTimes(out float secondsPerItem, out float secondsRemainingThisItem)
    {
        secondsPerItem = 0f;
        secondsRemainingThisItem = 0f;

        if (runtimeContainer == null || runtimeContainer.SlotCount < 3)
            return false;

        var ore = runtimeContainer.GetSlot(OreSlot);
        if (ore.IsEmpty || ore.Item == null || !ore.Item.IsOre || ore.Item.SmeltResult == null || ore.Quantity <= 0)
            return false;

        secondsPerItem = Mathf.Max(0.01f, ore.Item.SmeltSecondsPerItem);
        secondsRemainingThisItem = Mathf.Max(0f, secondsPerItem - oreSecondsProgress);
        return true;
    }
}