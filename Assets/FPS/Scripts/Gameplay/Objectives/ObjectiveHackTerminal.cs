using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectiveHackTerminal : Objective
    {
        [Tooltip("Terminal that must be hacked to complete this objective")]
        public HackTerminal TerminalToHack;

        protected override void Start()
        {
            if (TerminalToHack == null)
                TerminalToHack = GetComponent<HackTerminal>();

            if (string.IsNullOrEmpty(Title))
                Title = "Hack terminal";

            if (string.IsNullOrEmpty(Description))
                Description = "Get close to the terminal, press E, and stay nearby until the hack finishes.";

            DebugUtility.HandleErrorIfNullGetComponent<HackTerminal, ObjectiveHackTerminal>(TerminalToHack, this,
                gameObject);

            base.Start();

            UpdateObjective(Description, string.Empty, string.Empty);

            if (TerminalToHack == null)
                return;

            TerminalToHack.OnHackStarted += OnHackStarted;
            TerminalToHack.OnHackCancelled += OnHackCancelled;
            TerminalToHack.OnHackCompleted += OnHackCompleted;

            if (TerminalToHack.IsHacked && !IsCompleted)
                CompleteObjective(string.Empty, string.Empty, "Objective complete : " + Title);
        }

        void OnHackStarted(HackTerminal terminal)
        {
            if (IsCompleted || terminal != TerminalToHack)
                return;

            UpdateObjective("Stay near the terminal until the hack finishes.", "Hacking...", "Hack started");
        }

        void OnHackCancelled(HackTerminal terminal)
        {
            if (IsCompleted || terminal != TerminalToHack)
                return;

            UpdateObjective("Hack interrupted. Return to the terminal and press E again.", string.Empty,
                "Hack interrupted");
        }

        void OnHackCompleted(HackTerminal terminal)
        {
            if (IsCompleted || terminal != TerminalToHack)
                return;

            CompleteObjective(string.Empty, string.Empty, "Objective complete : " + Title);
        }

        void OnDestroy()
        {
            if (TerminalToHack == null)
                return;

            TerminalToHack.OnHackStarted -= OnHackStarted;
            TerminalToHack.OnHackCancelled -= OnHackCancelled;
            TerminalToHack.OnHackCompleted -= OnHackCompleted;
        }
    }
}


