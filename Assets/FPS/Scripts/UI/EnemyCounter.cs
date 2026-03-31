using TMPro;
using Unity.FPS.AI;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class EnemyCounter : MonoBehaviour
    {
        [Header("Enemies")] [Tooltip("Optional text component for displaying enemy objective progress")]
        public TMP_Text EnemiesText;

        EnemyManager m_EnemyManager;

        void Awake()
        {
            m_EnemyManager = FindFirstObjectByType<EnemyManager>();
            DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyCounter>(m_EnemyManager, this);

            EnsureCounterText();
            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
        }

        void Start()
        {
            UpdateCounterText(m_EnemyManager != null ? m_EnemyManager.NumberOfEnemiesRemaining : 0);
        }

        void OnEnemyKilled(EnemyKillEvent evt)
        {
            UpdateCounterText(evt.RemainingEnemyCount);
        }

        void UpdateCounterText(int enemiesRemaining)
        {
            EnsureCounterText();

            if (EnemiesText != null)
            {
                EnemiesText.text = $"Enemies left: {Mathf.Max(0, enemiesRemaining)}";
            }
        }

        void EnsureCounterText()
        {
            if (EnemiesText != null)
                return;

            RectTransform parentRect = GetCounterParent();
            if (parentRect == null)
                return;

            TextMeshProUGUI referenceText = FindFirstObjectByType<TextMeshProUGUI>();

            GameObject counterObject = new GameObject("EnemyCounterText", typeof(RectTransform));
            counterObject.transform.SetParent(parentRect, false);

            RectTransform counterRect = counterObject.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0.5f, 1f);
            counterRect.anchorMax = new Vector2(0.5f, 1f);
            counterRect.pivot = new Vector2(0.5f, 1f);
            counterRect.anchoredPosition = new Vector2(0f, -50f);
            counterRect.sizeDelta = new Vector2(520f, 32f);

            var counterText = counterObject.AddComponent<TextMeshProUGUI>();
            counterText.font = referenceText != null ? referenceText.font : TMP_Settings.defaultFontAsset;
            counterText.fontSharedMaterial = referenceText != null ? referenceText.fontSharedMaterial : null;
            counterText.fontSize = 20f;
            counterText.enableWordWrapping = false;
            counterText.alignment = TextAlignmentOptions.Center;
            counterText.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            counterText.text = string.Empty;

            EnemiesText = counterText;
        }

        RectTransform GetCounterParent()
        {
            ObjectiveHUDManager objectiveHudManager = GetComponent<ObjectiveHUDManager>() ?? FindFirstObjectByType<ObjectiveHUDManager>();
            if (objectiveHudManager != null && objectiveHudManager.ObjectivePanel != null && objectiveHudManager.ObjectivePanel.parent is RectTransform rectTransform)
                return rectTransform;

            return FindFirstObjectByType<Canvas>()?.transform as RectTransform;
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        }
    }
}