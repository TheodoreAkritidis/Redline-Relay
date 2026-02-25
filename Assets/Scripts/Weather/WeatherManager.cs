using UnityEngine;
using UnityEngine.VFX;

public class WeatherManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float rainIntensity;
    //[SerializeField, Range(0f, 1f)] float snowIntensity;
    //[SerializeField, Range(0f, 1f)] float fogIntensity;

    [SerializeField] VisualEffect rainVFX;
    //[SerializeField] VisualEffect snowVFX;

    float previousRainIntensity;
    //float previousSnowIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rainVFX.SetFloat("Intensity", rainIntensity);
    }

    // Update is called once per frame
    void Update()
    {
        if (rainIntensity != previousRainIntensity)
        {
            previousRainIntensity = rainIntensity;
            rainVFX.SetFloat("Intensity", rainIntensity);
        }

        //if (snowIntensity != previousSnowIntensity)
        //{
        //    previousSnowIntensity = snowIntensity;
        //    snowVFX.SetFloat("Intensity", snowIntensity);
        //}
    }
}
