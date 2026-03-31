using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using TMPro;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class ObjectiveHUDManager : MonoBehaviour
    {
        [Tooltip("UI panel containing the layoutGroup for displaying objectives")]
        public RectTransform ObjectivePanel;

        [Tooltip("Prefab for the primary objectives")]
        public GameObject PrimaryObjectivePrefab;

        [Tooltip("Prefab for the primary objectives")]
        public GameObject SecondaryObjectivePrefab;

        Dictionary<Objective, ObjectiveToast> m_ObjectivesDictionnary;
        Dictionary<Objective, ObjectiveStatus> m_ObjectiveStatuses;
        List<Objective> m_ObjectiveDisplayOrder;
        TextMeshProUGUI m_CurrentObjectiveText;

        class ObjectiveStatus
        {
            public string DescriptionText;
            public string CounterText;
        }

        void Awake()
        {
            m_ObjectivesDictionnary = new Dictionary<Objective, ObjectiveToast>();
            m_ObjectiveStatuses = new Dictionary<Objective, ObjectiveStatus>();
            m_ObjectiveDisplayOrder = new List<Objective>();

            EnsureMissionTracker();

            EventManager.AddListener<ObjectiveUpdateEvent>(OnUpdateObjective);

            Objective.OnObjectiveCreated += RegisterObjective;
            Objective.OnObjectiveCompleted += UnregisterObjective;

            if (GetComponent<EnemyCounter>() == null)
            {
                gameObject.AddComponent<EnemyCounter>();
            }
        }

        void Start()
        {
            RefreshMissionTracker();
        }

        public void RegisterObjective(Objective objective)
        {
            // instanciate the Ui element for the new objective
            GameObject objectiveUIInstance =
                Instantiate(objective.IsOptional ? SecondaryObjectivePrefab : PrimaryObjectivePrefab, ObjectivePanel);

            if (!objective.IsOptional)
                objectiveUIInstance.transform.SetSiblingIndex(0);

            ObjectiveToast toast = objectiveUIInstance.GetComponent<ObjectiveToast>();
            DebugUtility.HandleErrorIfNullGetComponent<ObjectiveToast, ObjectiveHUDManager>(toast, this,
                objectiveUIInstance.gameObject);

            // initialize the element and give it the objective description
            toast.Initialize(objective.Title, objective.Description, "", objective.IsOptional, objective.DelayVisible);

            m_ObjectivesDictionnary.Add(objective, toast);

            m_ObjectiveStatuses[objective] = new ObjectiveStatus
            {
                DescriptionText = objective.Description,
                CounterText = string.Empty,
            };

            m_ObjectiveDisplayOrder.Remove(objective);
            if (objective.IsOptional)
                m_ObjectiveDisplayOrder.Add(objective);
            else
                m_ObjectiveDisplayOrder.Insert(0, objective);

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(ObjectivePanel);
            RefreshMissionTracker();
        }

        public void UnregisterObjective(Objective objective)
        {
            // if the objective if in the list, make it fade out, and remove it from the list
            if (m_ObjectivesDictionnary.TryGetValue(objective, out ObjectiveToast toast) && toast != null)
            {
                toast.Complete();
            }

            m_ObjectivesDictionnary.Remove(objective);
            m_ObjectiveStatuses.Remove(objective);
            m_ObjectiveDisplayOrder.Remove(objective);

            RefreshMissionTracker();
        }

        void OnUpdateObjective(ObjectiveUpdateEvent evt)
        {
            if (m_ObjectivesDictionnary.TryGetValue(evt.Objective, out ObjectiveToast toast) && toast != null)
            {
                // set the new updated description for the objective, and forces the content size fitter to be recalculated
                Canvas.ForceUpdateCanvases();
                if (!string.IsNullOrEmpty(evt.DescriptionText))
                    toast.DescriptionTextContent.text = evt.DescriptionText;

                if (!string.IsNullOrEmpty(evt.CounterText))
                    toast.CounterTextContent.text = evt.CounterText;

                if (toast.GetComponent<RectTransform>())
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(toast.GetComponent<RectTransform>());
                }
            }

            if (!m_ObjectiveStatuses.TryGetValue(evt.Objective, out ObjectiveStatus objectiveStatus))
            {
                objectiveStatus = new ObjectiveStatus();
                m_ObjectiveStatuses[evt.Objective] = objectiveStatus;
            }

            if (!string.IsNullOrEmpty(evt.DescriptionText))
                objectiveStatus.DescriptionText = evt.DescriptionText;

            if (!string.IsNullOrEmpty(evt.CounterText))
                objectiveStatus.CounterText = evt.CounterText;

            RefreshMissionTracker();
        }

        void RefreshMissionTracker()
        {
            EnsureMissionTracker();

            if (m_CurrentObjectiveText == null)
                return;

            Objective currentObjective = GetCurrentObjective();
            if (currentObjective == null)
            {
                m_CurrentObjectiveText.text = string.Empty;
                return;
            }

            if (currentObjective is ObjectiveKillEnemies &&
                m_ObjectiveStatuses.TryGetValue(currentObjective, out ObjectiveStatus objectiveStatus) &&
                TryGetEnemiesRemaining(objectiveStatus.CounterText, out int remainingEnemies))
            {
                m_CurrentObjectiveText.text = $"Enemies remaining: {remainingEnemies}";
                return;
            }

            if (currentObjective is ObjectiveKillEnemies)
            {
                var enemyManager = FindFirstObjectByType<Unity.FPS.AI.EnemyManager>();
                if (enemyManager != null)
                {
                    m_CurrentObjectiveText.text = $"Enemies remaining: {enemyManager.NumberOfEnemiesRemaining}";
                    return;
                }
            }

            string objectiveLabel = currentObjective.Title;
            if (string.IsNullOrEmpty(objectiveLabel) &&
                m_ObjectiveStatuses.TryGetValue(currentObjective, out ObjectiveStatus statusWithDescription))
            {
                objectiveLabel = statusWithDescription.DescriptionText;
            }

            if (string.IsNullOrEmpty(objectiveLabel))
                objectiveLabel = currentObjective.Description;

            m_CurrentObjectiveText.text = string.IsNullOrEmpty(objectiveLabel)
                ? string.Empty
                : $"Objective: {objectiveLabel}";
        }

        Objective GetCurrentObjective()
        {
            for (int i = 0; i < m_ObjectiveDisplayOrder.Count; i++)
            {
                Objective objective = m_ObjectiveDisplayOrder[i];
                if (objective != null && !objective.IsCompleted)
                    return objective;
            }

            return null;
        }

        void EnsureMissionTracker()
        {
            if (m_CurrentObjectiveText != null)
                return;

            RectTransform parentRect = ObjectivePanel != null && ObjectivePanel.parent is RectTransform rectTransform
                ? rectTransform
                : FindFirstObjectByType<Canvas>()?.transform as RectTransform;

            if (parentRect == null)
                return;

            TextMeshProUGUI referenceText = FindFirstObjectByType<TextMeshProUGUI>();

            GameObject trackerObject = new GameObject("CurrentObjectiveText", typeof(RectTransform));
            trackerObject.transform.SetParent(parentRect, false);

            RectTransform trackerRect = trackerObject.GetComponent<RectTransform>();
            trackerRect.anchorMin = new Vector2(0.5f, 1f);
            trackerRect.anchorMax = new Vector2(0.5f, 1f);
            trackerRect.pivot = new Vector2(0.5f, 1f);
            trackerRect.anchoredPosition = new Vector2(0f, -18f);
            trackerRect.sizeDelta = new Vector2(640f, 36f);

            m_CurrentObjectiveText = trackerObject.AddComponent<TextMeshProUGUI>();
            m_CurrentObjectiveText.font = referenceText != null ? referenceText.font : TMP_Settings.defaultFontAsset;
            m_CurrentObjectiveText.fontSharedMaterial = referenceText != null ? referenceText.fontSharedMaterial : null;
            m_CurrentObjectiveText.fontSize = 24f;
            m_CurrentObjectiveText.enableWordWrapping = false;
            m_CurrentObjectiveText.alignment = TextAlignmentOptions.Center;
            m_CurrentObjectiveText.color = Color.white;
            m_CurrentObjectiveText.fontStyle = FontStyles.Bold;
            m_CurrentObjectiveText.text = string.Empty;
        }

        bool TryGetEnemiesRemaining(string counterText, out int remainingEnemies)
        {
            remainingEnemies = 0;
            if (string.IsNullOrEmpty(counterText))
                return false;

            string[] values = counterText.Split('/');
            if (values.Length != 2)
                return false;

            if (!int.TryParse(values[0].Trim(), out int currentKills) || !int.TryParse(values[1].Trim(), out int totalKills))
                return false;

            remainingEnemies = Mathf.Max(0, totalKills - currentKills);
            return true;
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<ObjectiveUpdateEvent>(OnUpdateObjective);

            Objective.OnObjectiveCreated -= RegisterObjective;
            Objective.OnObjectiveCompleted -= UnregisterObjective;
        }
    }
}