using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    public static class SessionFeedbackData
    {
        static readonly HashSet<Objective> s_RegisteredObjectives = new HashSet<Objective>();
        static readonly HashSet<Objective> s_CompletedObjectives = new HashSet<Objective>();

        public static int EnemiesKilled { get; private set; }
        public static int ObjectivesCompleted => s_CompletedObjectives.Count;
        public static int ObjectivesTotal => s_RegisteredObjectives.Count;
        public static bool SessionFinished { get; private set; }
        public static bool DidWin { get; private set; }

        static float s_SessionStartTime;
        static float s_SessionEndTime;
        static bool s_IsTracking;

        public static void BeginSession()
        {
            s_RegisteredObjectives.Clear();
            s_CompletedObjectives.Clear();
            EnemiesKilled = 0;
            SessionFinished = false;
            DidWin = false;
            s_SessionStartTime = Time.time;
            s_SessionEndTime = s_SessionStartTime;
            s_IsTracking = true;
        }

        public static void RegisterEnemyKill()
        {
            if (!s_IsTracking)
                return;

            EnemiesKilled++;
        }

        public static void RegisterObjective(Objective objective)
        {
            if (!s_IsTracking || objective == null)
                return;

            s_RegisteredObjectives.Add(objective);
        }

        public static void RegisterObjectiveCompletion(Objective objective)
        {
            if (!s_IsTracking || objective == null)
                return;

            s_RegisteredObjectives.Add(objective);
            s_CompletedObjectives.Add(objective);
        }

        public static void CompleteSession(bool didWin)
        {
            if (!s_IsTracking || SessionFinished)
                return;

            DidWin = didWin;
            SessionFinished = true;
            s_SessionEndTime = Time.time;
            s_IsTracking = false;
        }

        public static float GetSessionDurationSeconds()
        {
            float endTime = SessionFinished ? s_SessionEndTime : Time.time;
            return Mathf.Max(0f, endTime - s_SessionStartTime);
        }

        public static string GetFormattedDuration()
        {
            int totalSeconds = Mathf.FloorToInt(GetSessionDurationSeconds());
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        public static string GetSummaryText()
        {
            string objectivesLine = ObjectivesTotal > 0
                ? $"Objectives completed: {ObjectivesCompleted}/{ObjectivesTotal}"
                : $"Objectives completed: {ObjectivesCompleted}";

            return $"Enemies killed: {EnemiesKilled}\n{objectivesLine}\nTime survived: {GetFormattedDuration()}";
        }
    }
}

