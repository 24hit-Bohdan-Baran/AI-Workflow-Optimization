using TMPro;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.FPS.UI
{
    public class EndGameSummaryDisplay : MonoBehaviour
    {
        TextMeshProUGUI m_SummaryText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateForEndScene()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName != "WinScene" && activeSceneName != "LoseScene")
                return;

            if (FindFirstObjectByType<EndGameSummaryDisplay>() != null)
                return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            GameObject summaryRoot = new GameObject("EndGameSummaryDisplay", typeof(RectTransform));
            summaryRoot.transform.SetParent(canvas.transform, false);
            summaryRoot.AddComponent<EndGameSummaryDisplay>();
        }

        void Awake()
        {
            CreateSummaryText();
            RefreshSummary();
        }

        void CreateSummaryText()
        {
            if (m_SummaryText != null)
                return;

            RectTransform summaryRect = gameObject.GetComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.5f, 0.5f);
            summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
            summaryRect.pivot = new Vector2(0.5f, 0.5f);
            summaryRect.anchoredPosition = new Vector2(0f, -55f);
            summaryRect.sizeDelta = new Vector2(720f, 180f);

            TextMeshProUGUI referenceText = FindFirstObjectByType<TextMeshProUGUI>();

            m_SummaryText = gameObject.AddComponent<TextMeshProUGUI>();

            m_SummaryText.font = referenceText != null ? referenceText.font : TMP_Settings.defaultFontAsset;
            m_SummaryText.fontSharedMaterial = referenceText != null ? referenceText.fontSharedMaterial : null;
            m_SummaryText.fontSize = 26f;
            m_SummaryText.enableWordWrapping = false;
            m_SummaryText.alignment = TextAlignmentOptions.Center;
            m_SummaryText.color = Color.white;
        }

        void RefreshSummary()
        {
            if (m_SummaryText == null)
                return;

            m_SummaryText.text = SessionFeedbackData.GetSummaryText();
        }
    }
}


