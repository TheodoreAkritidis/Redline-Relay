using UnityEngine;

public sealed class Canteen : MonoBehaviour
{
    [SerializeField] private float DrinkValue;
    [SerializeField] private float FillValue;

    public CanteenModel CanteenModel { get; private set; }

    private void Awake()
    {
        CanteenModel = new CanteenModel(0);
    }

    public void DrinkFromCanteen()
    {
        if (CanteenModel.CanteenLevel <= 0)
        {
            return;
        }

        CanteenModel.CanteenLevel -= 25;
    }

    public void FillCanteen(float fillValue)
    {
        if (CanteenModel.CanteenLevel >= 100)
        {
            return;
        }

        CanteenModel.CanteenLevel += fillValue;
    }

    public float GetCurrentLevel()
    {
        return CanteenModel.CanteenLevel;
    }
}
