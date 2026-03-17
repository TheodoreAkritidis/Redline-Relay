using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Dedicated Music Sources")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private AudioSource victoryMusicSource;

    [Header("Gameplay Music Sources")]
    [SerializeField] private AudioSource[] gameplayMusicSources;

    [Header("Music Settings")]
    [SerializeField] private float backgroundMusicVolume = 1.0f;

    private bool gameplayMusicActive = false;

    private int nextGameplayIndex = 0;
    private bool gameplayPaused;

    private void Awake()
    {
        if (gameplayMusicSources == null || gameplayMusicSources.Length == 0)
        {
            Debug.LogWarning($"{nameof(BackgroundMusic)}: No gameplay music sources assigned.", this);
        }

        if (menuMusicSource != null)
            menuMusicSource.ignoreListenerPause = true;

        if (victoryMusicSource != null)
            victoryMusicSource.ignoreListenerPause = true;

        if (gameplayMusicSources != null)
        {
            for (int i = 0; i < gameplayMusicSources.Length; i++)
            {
                if (gameplayMusicSources[i] == null) continue;
                gameplayMusicSources[i].ignoreListenerPause = false;
                gameplayMusicSources[i].volume = backgroundMusicVolume;
            }
        }

        if (menuMusicSource != null)
            menuMusicSource.volume = backgroundMusicVolume;

        if (victoryMusicSource != null)
            victoryMusicSource.volume = backgroundMusicVolume;

        nextGameplayIndex = GetRandomGameplayStartIndex();
        gameplayPaused = false;
    }

    private void Update()
    {
        if (!gameplayMusicActive) return;
        if (gameplayPaused) return;
        if (IsAnyGameplayTrackPlaying()) return;

        PlayNextGameplayTrack();
    }

    public void PlayMenuMusic()
    {
        gameplayMusicActive = false;
        gameplayPaused = false;

        StopVictoryMusic();
        StopGameplayMusic(resetClips: true);

        if (menuMusicSource == null) return;

        menuMusicSource.volume = backgroundMusicVolume;
        menuMusicSource.Stop();
        menuMusicSource.Play();
    }

    public void StartGameplayMusicFresh()
    {
        gameplayMusicActive = true;
        gameplayPaused = false;

        StopMenuMusic();
        StopVictoryMusic();
        StopGameplayMusic(resetClips: true);

        nextGameplayIndex = GetRandomGameplayStartIndex();
        PlayNextGameplayTrack();
    }

    public void PauseGameplayMusic()
    {
        if (gameplayPaused) return;

        AudioSource current = GetCurrentGameplayTrack();
        if (current != null && current.isPlaying)
            current.Pause();

        gameplayPaused = true;
    }

    public void ResumeGameplayMusic()
    {
        if (!gameplayPaused) return;

        AudioSource current = GetCurrentPausedGameplayTrack();
        if (current != null)
        {
            current.UnPause();
        }
        else
        {
            PlayNextGameplayTrack();
        }

        gameplayPaused = false;
    }

    public void PlayVictoryMusic()
    {
        gameplayMusicActive = false;
        gameplayPaused = false;

        StopMenuMusic();
        StopGameplayMusic(resetClips: true);

        if (victoryMusicSource == null) return;

        victoryMusicSource.volume = backgroundMusicVolume;
        victoryMusicSource.Stop();
        victoryMusicSource.Play();
    }

    public void StopAllMusic()
    {
        gameplayMusicActive = false;
        gameplayPaused = false;

        StopMenuMusic();
        StopVictoryMusic();
        StopGameplayMusic(resetClips: true);
    }

    private void PlayNextGameplayTrack()
    {
        if (gameplayMusicSources == null || gameplayMusicSources.Length == 0)
            return;

        AudioSource source = gameplayMusicSources[nextGameplayIndex];
        nextGameplayIndex++;
        if (nextGameplayIndex >= gameplayMusicSources.Length)
            nextGameplayIndex = 0;

        if (source == null || source.clip == null)
            return;

        source.volume = backgroundMusicVolume;
        source.Stop();
        source.Play();
    }

    private bool IsAnyGameplayTrackPlaying()
    {
        if (gameplayMusicSources == null) return false;

        for (int i = 0; i < gameplayMusicSources.Length; i++)
        {
            AudioSource source = gameplayMusicSources[i];
            if (source != null && source.isPlaying)
                return true;
        }

        return false;
    }

    private AudioSource GetCurrentGameplayTrack()
    {
        if (gameplayMusicSources == null) return null;

        for (int i = 0; i < gameplayMusicSources.Length; i++)
        {
            AudioSource source = gameplayMusicSources[i];
            if (source != null && source.isPlaying)
                return source;
        }

        return null;
    }

    private AudioSource GetCurrentPausedGameplayTrack()
    {
        if (gameplayMusicSources == null) return null;

        for (int i = 0; i < gameplayMusicSources.Length; i++)
        {
            AudioSource source = gameplayMusicSources[i];
            if (source != null && source.clip != null && source.time > 0f && !source.isPlaying)
                return source;
        }

        return null;
    }

    private void StopMenuMusic()
    {
        if (menuMusicSource != null)
            menuMusicSource.Stop();
    }

    private void StopVictoryMusic()
    {
        if (victoryMusicSource != null)
            victoryMusicSource.Stop();
    }

    private void StopGameplayMusic(bool resetClips)
    {
        if (gameplayMusicSources == null) return;

        for (int i = 0; i < gameplayMusicSources.Length; i++)
        {
            AudioSource source = gameplayMusicSources[i];
            if (source == null) continue;

            source.Stop();

            if (resetClips)
                source.time = 0f;
        }
    }

    private int GetRandomGameplayStartIndex()
    {
        if (gameplayMusicSources == null || gameplayMusicSources.Length == 0)
            return 0;

        return Random.Range(0, gameplayMusicSources.Length);
    }
}