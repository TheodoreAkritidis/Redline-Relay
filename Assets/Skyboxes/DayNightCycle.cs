using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    // skybox textures for each time of day
    [SerializeField] private Texture2D skyboxNight;
    [SerializeField] private Texture2D skyboxSunrise;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxSunset;

    // gradients used for the transition between skyboxes
    [SerializeField] private Gradient graddientNightToSunrise;
    [SerializeField] private Gradient graddientSunriseToDay;
    [SerializeField] private Gradient graddientDayToSunset;
    [SerializeField] private Gradient graddientSunsetToNight;

    [SerializeField] private Light globalLight; // represents light from the sun (daytime) and moon (nighttime)
    [SerializeField] float speed;

    // helps to track the day and time for when a certain texture should be used and when the light should change
    [SerializeField] private int minutes;
    [SerializeField] private int hours = 5;
    [SerializeField] private int days;
    [SerializeField] private float tempSecond;

    // getters and setters for the time variables, will call the appropriate functions when the value changes
    public int Minutes
    { 
        get { return minutes; } set { minutes = value; OnMinutesChange(value); } 
    }

    public int Hours
    { 
        get { return hours; } set { hours = value; OnHoursChange(value); } 
    }
    

    public int Days
    { 
        get { return days; } set { days = value; } 
    }

    // starts with time at 0 seconds, so the matching visual will be day/sunset
    private void Start()
    {
        // set based on starting hours
        SetSkybox(skyboxDay, skyboxSunset);
        SetLight(graddientDayToSunset);
    }

    // calculates the seconds and holds that in a temporary variable and updates the minutes to be accurate to the game
    public void Update()
    {
        tempSecond += Time.deltaTime * speed;

        if (tempSecond >= 1)
        {
            Minutes += 1;
            tempSecond = 0;
        }
    }

    private void OnMinutesChange(int value)
    {
        globalLight.transform.Rotate(Vector3.up, (1f / (1440f / 4f)) * 360f, Space.World);
        if (value >= 60)
        {
            Hours++;
            minutes = 0;
        }
        if (Hours >= 24)
        {
            Hours = 0;
            Days++;
        }
    }

    private void OnHoursChange(int value)
    {
        if (value == 6)
        {
            StartCoroutine(LerpSkybox(skyboxNight, skyboxSunrise, 10f));
            StartCoroutine(LerpLight(graddientNightToSunrise, 10f));
        }
        else if (value == 8)
        {
            StartCoroutine(LerpSkybox(skyboxSunrise, skyboxDay, 10f));
            StartCoroutine(LerpLight(graddientSunriseToDay, 10f));
        }
        else if (value == 18)
        {
            StartCoroutine(LerpSkybox(skyboxDay, skyboxSunset, 10f));
            StartCoroutine(LerpLight(graddientDayToSunset, 10f));
        }
        else if (value == 22)
        {
            StartCoroutine(LerpSkybox(skyboxSunset, skyboxNight, 10f));
            StartCoroutine(LerpLight(graddientSunsetToNight, 10f));
        }
    }

    // will be called on run to set the skybox to morning
    private void SetSkybox(Texture2D a, Texture2D b)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);
    }

    private IEnumerator LerpSkybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);
        for (float i = 0; i < time; i += Time.deltaTime)
        {
            RenderSettings.skybox.SetFloat("_Blend", i / time);
            yield return null;
        }
        RenderSettings.skybox.SetTexture("_Texture1", b);
    }

    private void SetLight(Gradient lightGradient)
    {
        globalLight.color = lightGradient.Evaluate(0);
        RenderSettings.fogColor = globalLight.color;
    }

    private IEnumerator LerpLight(Gradient lightGradient, float time)
    {
        for (float i = 0; i < time; i += Time.deltaTime)
        {
            globalLight.color = lightGradient.Evaluate(i / time);
            RenderSettings.fogColor = globalLight.color;
            yield return null;
        }
    }
}


// Reference https://pastebin.com/DBvfk1PK