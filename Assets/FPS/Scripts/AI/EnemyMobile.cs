using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyMobile : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Follow,
            Attack,
            Search,
        }

        public Animator Animator;

        [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
        [Range(0f, 1f)]
        public float AttackStopDistanceRatio = 0.5f;

        [Tooltip("The random hit damage effects")]
        public ParticleSystem[] RandomHitSparks;

        public ParticleSystem[] OnDetectVfx;
        public AudioClip OnDetectSfx;

        [Header("Search")]
        [Tooltip("Minimum time spent searching at the last known target position before returning to patrol")]
        public float SearchDurationMin = 3f;

        [Tooltip("Maximum time spent searching at the last known target position before returning to patrol")]
        public float SearchDurationMax = 5f;

        [Tooltip("Rotation speed while searching the last known target position")]
        public float SearchRotationSpeed = 60f;

        [Header("Debug")]
        [Tooltip("Draw the last known target position while selected")]
        public Color SearchPositionGizmoColor = Color.cyan;

        [Tooltip("Enable logs when the mobile enemy changes AI state")]
        public bool LogStateChanges;

        [Header("Sound")] public AudioClip MovementSound;
        public MinMaxFloat PitchDistortionMovementSpeed;

        public AIState AiState { get; private set; }
        EnemyController m_EnemyController;
        AudioSource m_AudioSource;
        bool m_WasSeeingTarget;
        bool m_IsAlerted;
        bool m_HasLastKnownTargetPosition;
        bool m_SearchReachedLastKnownPosition;
        Vector3 m_LastKnownTargetPosition;
        float m_SearchDuration;
        float m_SearchStartTime = Mathf.NegativeInfinity;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        void Start()
        {
            m_EnemyController = GetComponent<EnemyController>();
            DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyMobile>(m_EnemyController, this,
                gameObject);

            m_EnemyController.onAttack += OnAttack;
            m_EnemyController.onDetectedTarget += OnDetectedTarget;
            m_EnemyController.onLostTarget += OnLostTarget;
            m_EnemyController.SetPathDestinationToClosestNode();
            m_EnemyController.onDamaged += OnDamaged;

            // Start patrolling
            AiState = AIState.Patrol;

            // adding a audio source to play the movement sound on it
            m_AudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
            m_AudioSource.clip = MovementSound;
            m_AudioSource.Play();
        }

        void Update()
        {
            UpdateAiStateTransitions();
            UpdateCurrentAiState();

            float moveSpeed = m_EnemyController.NavMeshAgent.velocity.magnitude;

            // Update animator speed parameter
            Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

            // changing the pitch of the movement sound depending on the movement speed
            m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max,
                moveSpeed / m_EnemyController.NavMeshAgent.speed);
        }

        void UpdateAiStateTransitions()
        {
            bool hasKnownTarget = m_EnemyController.KnownDetectedTarget != null;
            bool isSeeingTarget = m_EnemyController.IsSeeingTarget;

            if (hasKnownTarget && isSeeingTarget)
            {
                RememberTargetPosition(m_EnemyController.KnownDetectedTarget.transform.position);
                SetAlertVisuals(true, false);
                SetAiState(m_EnemyController.IsTargetInAttackRange ? AIState.Attack : AIState.Follow);
                m_WasSeeingTarget = true;
                return;
            }

            if (m_WasSeeingTarget && !isSeeingTarget && hasKnownTarget)
            {
                StartSearch();
            }

            m_WasSeeingTarget = isSeeingTarget;

            // Handle transitions 
            switch (AiState)
            {
                case AIState.Follow:
                    if (!hasKnownTarget)
                    {
                        if (m_HasLastKnownTargetPosition)
                        {
                            StartSearch();
                        }
                        else
                        {
                            ResumePatrol();
                        }
                    }

                    break;
                case AIState.Attack:
                    if (!hasKnownTarget)
                    {
                        if (m_HasLastKnownTargetPosition)
                        {
                            StartSearch();
                        }
                        else
                        {
                            ResumePatrol();
                        }
                    }

                    break;
                case AIState.Search:
                    if (!m_HasLastKnownTargetPosition)
                    {
                        ResumePatrol();
                    }
                    else if (m_SearchReachedLastKnownPosition && Time.time >= m_SearchStartTime + m_SearchDuration)
                    {
                        ResumePatrol();
                    }

                    break;
            }
        }

        void UpdateCurrentAiState()
        {
            // Handle logic 
            switch (AiState)
            {
                case AIState.Patrol:
                    m_EnemyController.UpdatePathDestination();
                    m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                    break;
                case AIState.Follow:
                    if (m_EnemyController.KnownDetectedTarget == null)
                    {
                        break;
                    }

                    m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.OrientWeaponsTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    break;
                case AIState.Attack:
                    if (m_EnemyController.KnownDetectedTarget == null)
                    {
                        break;
                    }

                    if (Vector3.Distance(m_EnemyController.KnownDetectedTarget.transform.position,
                            m_EnemyController.DetectionModule.DetectionSourcePoint.position)
                        >= (AttackStopDistanceRatio * m_EnemyController.DetectionModule.AttackRange))
                    {
                        m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                    }
                    else
                    {
                        m_EnemyController.SetNavDestination(transform.position);
                    }

                    m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                    m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
                    break;
                case AIState.Search:
                    m_EnemyController.SetNavDestination(m_LastKnownTargetPosition);

                    if ((transform.position - m_LastKnownTargetPosition).magnitude <= m_EnemyController.PathReachingRadius)
                    {
                        if (!m_SearchReachedLastKnownPosition)
                        {
                            m_SearchReachedLastKnownPosition = true;
                            m_SearchStartTime = Time.time;
                        }

                        m_EnemyController.SetNavDestination(transform.position);
                        transform.Rotate(Vector3.up, SearchRotationSpeed * Time.deltaTime, Space.Self);
                    }
                    else
                    {
                        m_SearchReachedLastKnownPosition = false;
                    }

                    m_EnemyController.OrientWeaponsTowards(m_LastKnownTargetPosition);
                    break;
            }
        }

        void OnAttack()
        {
            Animator.SetTrigger(k_AnimAttackParameter);
        }

        void OnDetectedTarget()
        {
            if (m_EnemyController.KnownDetectedTarget != null)
            {
                RememberTargetPosition(m_EnemyController.KnownDetectedTarget.transform.position);
            }

            SetAiState(m_EnemyController.IsTargetInAttackRange ? AIState.Attack : AIState.Follow);
            SetAlertVisuals(true, true);
        }

        void OnLostTarget()
        {
            if ((AiState == AIState.Follow || AiState == AIState.Attack || AiState == AIState.Search) &&
                m_HasLastKnownTargetPosition)
            {
                StartSearch();
                return;
            }

            ResumePatrol();
        }

        void OnDamaged()
        {
            if (RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length - 1);
                RandomHitSparks[n].Play();
            }

            Animator.SetTrigger(k_AnimOnDamagedParameter);

            if (m_EnemyController.KnownDetectedTarget != null)
            {
                RememberTargetPosition(m_EnemyController.KnownDetectedTarget.transform.position);
                SetAlertVisuals(true, false);
                SetAiState(IsTargetWithinAttackRange(m_EnemyController.KnownDetectedTarget.transform.position)
                    ? AIState.Attack
                    : AIState.Follow);
            }
        }

        void StartSearch()
        {
            if (!m_HasLastKnownTargetPosition)
            {
                ResumePatrol();
                return;
            }

            m_SearchDuration = Random.Range(Mathf.Min(SearchDurationMin, SearchDurationMax),
                Mathf.Max(SearchDurationMin, SearchDurationMax));
            m_SearchReachedLastKnownPosition = false;
            m_SearchStartTime = Mathf.NegativeInfinity;
            SetAiState(AIState.Search);
        }

        void ResumePatrol()
        {
            SetAiState(AIState.Patrol);
            m_SearchReachedLastKnownPosition = false;
            m_SearchStartTime = Mathf.NegativeInfinity;
            m_EnemyController.SetPathDestinationToClosestNode();
            SetAlertVisuals(false, false);
        }

        void RememberTargetPosition(Vector3 targetPosition)
        {
            m_LastKnownTargetPosition = targetPosition;
            m_HasLastKnownTargetPosition = true;
        }

        bool IsTargetWithinAttackRange(Vector3 targetPosition)
        {
            return Vector3.Distance(targetPosition, m_EnemyController.DetectionModule.DetectionSourcePoint.position) <=
                   m_EnemyController.DetectionModule.AttackRange;
        }

        void SetAiState(AIState newState)
        {
            if (AiState == newState)
            {
                return;
            }

            AiState = newState;

            if (LogStateChanges)
            {
                Debug.Log($"{name} AI state -> {AiState}", this);
            }
        }

        void SetAlertVisuals(bool alerted, bool playDetectFeedback)
        {
            if (!m_IsAlerted && alerted && playDetectFeedback)
            {
                for (int i = 0; i < OnDetectVfx.Length; i++)
                {
                    OnDetectVfx[i].Play();
                }

                if (OnDetectSfx)
                {
                    AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection,
                        1f);
                }
            }
            else if (m_IsAlerted && !alerted)
            {
                for (int i = 0; i < OnDetectVfx.Length; i++)
                {
                    OnDetectVfx[i].Stop();
                }
            }

            m_IsAlerted = alerted;
            Animator.SetBool(k_AnimAlertedParameter, alerted);
        }

        void OnDrawGizmosSelected()
        {
            if (!m_HasLastKnownTargetPosition)
            {
                return;
            }

            Gizmos.color = SearchPositionGizmoColor;
            Gizmos.DrawWireSphere(m_LastKnownTargetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, m_LastKnownTargetPosition);
        }
    }
}