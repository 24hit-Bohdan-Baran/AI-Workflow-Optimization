using System;
using TMPro;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class HackTerminal : MonoBehaviour
    {
        [Tooltip("How long the player must stay near the terminal for the hack to complete")]
        public float HackDuration = 2.5f;

        [Header("World Prompt")]
        [Tooltip("Whether to show a floating world-space prompt when the player is in range")]
        public bool ShowWorldSpacePrompt = true;

        [Tooltip("Optional anchor for the prompt. Defaults to this transform")]
        public Transform PromptAnchor;

        [Tooltip("Offset applied when the prompt is auto-created")]
        public Vector3 PromptOffset = new Vector3(0f, 2f, 0f);

        [Tooltip("Optional pre-made TextMeshPro object used for the prompt")]
        public TextMeshPro PromptText;

        [Tooltip("Font size used when the prompt is auto-created")]
        public float PromptFontSize = 3f;

        [Tooltip("Prompt color used when the prompt is auto-created")]
        public Color PromptColor = Color.cyan;

        [Tooltip("Message shown when the player gets close enough to interact")]
        public string PromptMessage = "Press E to hack terminal";

        [Tooltip("Message shown when the hack starts")]
        public string HackStartedMessage = "Hack started";

        [Tooltip("Message shown if the player leaves before the hack is complete")]
        public string HackCancelledMessage = "Hack cancelled";

        [Tooltip("Message shown after a successful hack")]
        public string HackCompletedMessage = "Terminal hacked";

        public bool IsHacked { get; private set; }
        public bool IsPlayerInRange => m_Player != null;
        public bool IsHacking => m_IsHacking;
        public float CurrentHackProgress => HackDuration <= 0f ? 1f : Mathf.Clamp01(m_CurrentHackTime / HackDuration);

        public event Action<HackTerminal> OnHackStarted;
        public event Action<HackTerminal> OnHackCancelled;
        public event Action<HackTerminal> OnHackCompleted;

        Collider m_TriggerCollider;
        PlayerCharacterController m_Player;
        PlayerInputHandler m_PlayerInputHandler;
        bool m_IsHacking;
        float m_CurrentHackTime;
        Transform m_PromptTransform;

        void Awake()
        {
            m_TriggerCollider = GetComponent<Collider>();
            DebugUtility.HandleErrorIfNullGetComponent<Collider, HackTerminal>(m_TriggerCollider, this, gameObject);

            if (m_TriggerCollider != null)
                m_TriggerCollider.isTrigger = true;

            InitializePrompt();
            RefreshPrompt();
        }

        void Update()
        {
            UpdatePromptFacing();

            if (IsHacked || m_Player == null || m_PlayerInputHandler == null)
            {
                RefreshPrompt();
                return;
            }

            if (!m_IsHacking)
            {
                if (m_PlayerInputHandler.GetInteractInputDown())
                    StartHack();

                RefreshPrompt();
                return;
            }

            m_CurrentHackTime += Time.deltaTime;
            RefreshPrompt();

            if (m_CurrentHackTime >= Mathf.Max(0f, HackDuration))
                CompleteHack();
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsHacked)
                return;

            PlayerCharacterController player = other.GetComponentInParent<PlayerCharacterController>();
            if (player == null)
                return;

            m_Player = player;
            m_PlayerInputHandler = player.GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, HackTerminal>(m_PlayerInputHandler, this,
                player.gameObject);

            RefreshPrompt();
        }

        void OnTriggerExit(Collider other)
        {
            PlayerCharacterController player = other.GetComponentInParent<PlayerCharacterController>();
            if (player == null || player != m_Player)
                return;

            if (m_IsHacking)
                CancelHack();

            m_Player = null;
            m_PlayerInputHandler = null;
            RefreshPrompt();
        }

        void StartHack()
        {
            if (m_IsHacking || IsHacked)
                return;

            m_IsHacking = true;
            m_CurrentHackTime = 0f;

            ShowMessage(HackStartedMessage);
            RefreshPrompt();
            OnHackStarted?.Invoke(this);
        }

        void CancelHack()
        {
            m_IsHacking = false;
            m_CurrentHackTime = 0f;

            ShowMessage(HackCancelledMessage);
            RefreshPrompt();
            OnHackCancelled?.Invoke(this);
        }

        void CompleteHack()
        {
            m_IsHacking = false;
            m_CurrentHackTime = HackDuration;
            IsHacked = true;

            ShowMessage(HackCompletedMessage);
            RefreshPrompt();
            OnHackCompleted?.Invoke(this);
        }

        void InitializePrompt()
        {
            if (!ShowWorldSpacePrompt)
                return;

            if (PromptAnchor == null)
                PromptAnchor = transform;

            if (PromptText == null)
            {
                GameObject promptObject = new GameObject("HackTerminalPrompt");
                promptObject.transform.SetParent(PromptAnchor, false);
                promptObject.transform.localPosition = PromptOffset;
                promptObject.transform.localRotation = Quaternion.identity;

                PromptText = promptObject.AddComponent<TextMeshPro>();
                PromptText.alignment = TextAlignmentOptions.Center;
                PromptText.fontSize = PromptFontSize;
                PromptText.color = PromptColor;
                PromptText.text = string.Empty;
            }

            m_PromptTransform = PromptText != null ? PromptText.transform : null;
        }

        void UpdatePromptFacing()
        {
            if (!ShowWorldSpacePrompt || PromptText == null || !PromptText.gameObject.activeSelf || Camera.main == null)
                return;

            Vector3 directionToCamera = m_PromptTransform.position - Camera.main.transform.position;
            if (directionToCamera.sqrMagnitude <= 0f)
                return;

            m_PromptTransform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
        }

        void RefreshPrompt()
        {
            if (!ShowWorldSpacePrompt || PromptText == null)
                return;

            bool shouldShowPrompt = !IsHacked && m_Player != null;
            PromptText.gameObject.SetActive(shouldShowPrompt);

            if (!shouldShowPrompt)
                return;

            if (m_IsHacking)
            {
                int progressPercent = Mathf.RoundToInt(CurrentHackProgress * 100f);
                PromptText.text = $"Hacking... {progressPercent}%";
            }
            else
            {
                PromptText.text = PromptMessage;
            }
        }

        void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
            displayMessage.Message = message;
            displayMessage.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(displayMessage);
        }
    }
}


