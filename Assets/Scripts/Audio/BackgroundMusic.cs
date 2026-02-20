using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Background Music Sources")]
    [SerializeField] private AudioSource[] musicSources;

    [Header("Music Settings")]
    [SerializeField] private float timeBetweenMusicChecks = 5f;
    [Header("volume should be between 0.0 to 1.0")]
    [SerializeField] private float backgroundMusicVolume = 1.0f;

    private int songIndex = 0;
    private bool anyMusicIsPlaying = false;

    private void Awake()
    {
        //if no music sources are assigned, log a warning and disable this script to prevent errors
        if (musicSources == null || musicSources.Length == 0)
        {
            Debug.LogWarning($"{nameof(BackgroundMusic)}: No music sources assigned.", this);
            enabled = false; // disables this script
            return;
        }

        songIndex = Random.Range(0, musicSources.Length);
    }

    private void Start()
    {
        InvokeRepeating(nameof(PlayMusic), 0f, timeBetweenMusicChecks);
    }

    private void PlayMusic()
    {
        anyMusicIsPlaying = false;
        for (int i = 0; i < musicSources.Length; i++)
        {
            if(musicSources[i].isPlaying)
            {
                anyMusicIsPlaying = true;
                break;
            }
        }

        if (!anyMusicIsPlaying)
        {
            musicSources[songIndex].volume = backgroundMusicVolume;
            musicSources[songIndex].Play();
            songIndex++;
            if(songIndex >= musicSources.Length)
            {
                songIndex = 0;
            }
        }
    }
}
