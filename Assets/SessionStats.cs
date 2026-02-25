using System;
using UnityEngine;

public static class SessionStats
{
    public enum RunResult { Success, Premature, Failed }

    public static int SuccessfulExtractions { get; private set; }
    public static int PrematureExtractions { get; private set; }
    public static int FailedExtractions { get; private set; }
    public static int TotalScore { get; private set; }

    public static event Action StatsChanged;

    public static void RecordRun(RunResult result, int runScore)
    {
        runScore = Mathf.Max(0, runScore);
        TotalScore += runScore;

        switch (result)
        {
            case RunResult.Success: SuccessfulExtractions++; break;
            case RunResult.Premature: PrematureExtractions++; break;
            case RunResult.Failed: FailedExtractions++; break;
        }

        StatsChanged?.Invoke();
    }

    public static void ResetSession()
    {
        SuccessfulExtractions = 0;
        PrematureExtractions = 0;
        FailedExtractions = 0;
        TotalScore = 0;
        StatsChanged?.Invoke();
    }
}
