using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float rainIntensity;
    [SerializeField, Range(0f, 1f)] float snowIntensity;
    [SerializeField, Range(0f, 1f)] float fogIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
