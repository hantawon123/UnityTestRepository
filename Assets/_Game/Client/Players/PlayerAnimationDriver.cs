using Game.Client.Combat;
using Game.Client.Interactions;
using Game.Core.Players;
using UnityEngine;

namespace Game.Client.Players
{
    /// <summary>
    /// 이동·전투 상태를 읽어 애니메이터 상태를 직접 지시한다.
    /// 전환 조건을 애니메이터 그래프가 아니라 코드가 소유한다. (루트 모션 미사용)
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const string PunchState = "Punch";
        private const string StunnedState = "Stunned";
        private const string AirborneState = "Fall_Flutter";
        private const string LandState = "Land_Matched";
        private const float SpeedDampTime = 0.1f;
        private const float CrossFadeSeconds = 0.15f;
        private const float DirectionDeadZone = 0.2f;
        private const float LandSeconds = 20f / 30f;

        [SerializeField, Min(0.1f), Tooltip("펀치 모션 유지 시간(초). CombatConfig가 있으면 그 값을 우선한다")]
        private float punchDurationSeconds = 0.5f;

        private float PunchDuration =>
            combatant != null && combatant.Config != null
                ? combatant.Config.PunchMotionSeconds
                : punchDurationSeconds;

        private PlayerMovement movement;
        private PlayerCombatant combatant;
        private PlayerInteractor interactor;
        private Animator animator;
        private string currentState;
        private float punchUntilTime;
        private float oneShotUntilTime;
        private string oneShotState;
        private bool wasGrounded = true;
        private PlayerPosture lastPosture = PlayerPosture.Standing;
        private bool usesNetworkState;
        private float networkSpeed;
        private bool networkGrounded;
        private int networkAttackSequence;
        private Vector2 networkMoveLocal;
        private bool networkCarrying;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            combatant = GetComponent<PlayerCombatant>();
            interactor = GetComponent<PlayerInteractor>();
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogWarning("PlayerAnimationDriver: 자식에서 Animator를 찾지 못해 비활성화합니다.", this);
                enabled = false;
                return;
            }

            animator.applyRootMotion = false;
            lastPosture = movement.Posture;
        }

        private void OnEnable()
        {
            if (combatant != null)
            {
                combatant.AttackPerformed += OnAttackPerformed;
            }
        }

        private void OnDisable()
        {
            if (combatant != null)
            {
                combatant.AttackPerformed -= OnAttackPerformed;
            }
        }

        private void OnAttackPerformed()
        {
            if (usesNetworkState)
            {
                return;
            }

            PlayPunch();
        }

        private void PlayPunch()
        {
            punchUntilTime = Time.time + PunchDuration;
            oneShotUntilTime = 0f;
            currentState = PunchState;
            animator.CrossFadeInFixedTime(PunchState, 0.05f, 0, 0f);
        }

        public void PlayPickup() => PlayOneShot("Pickup_Low", 60f / 30f);

        public void PlayPutDown() => PlayOneShot("PutDown_Low", 60f / 30f);

        public void PlayThrow() => PlayOneShot("Throw", 41f / 30f);

        public void ApplyNetworkState(
            float planarSpeed,
            bool grounded,
            int attackSequence)
        {
            ApplyNetworkState(planarSpeed, grounded, attackSequence, Vector2.zero, false);
        }

        public void ApplyNetworkState(
            float planarSpeed,
            bool grounded,
            int attackSequence,
            Vector2 planarDirectionLocal,
            bool carrying)
        {
            if (!usesNetworkState)
            {
                usesNetworkState = true;
                networkAttackSequence = attackSequence;
            }
            else if (networkAttackSequence != attackSequence)
            {
                networkAttackSequence = attackSequence;
                PlayPunch();
            }

            networkSpeed = Mathf.Max(0f, planarSpeed);
            networkGrounded = grounded;
            networkMoveLocal = planarDirectionLocal;
            networkCarrying = carrying;
        }

        private void Update()
        {
            if (animator.runtimeAnimatorController != null &&
                HasParameter(animator, "Speed"))
            {
                animator.SetFloat(
                    "Speed",
                    usesNetworkState ? networkSpeed : movement.PlanarSpeed,
                    SpeedDampTime,
                    Time.deltaTime);
            }

            var desiredState = ResolveDesiredState();
            if (desiredState != currentState)
            {
                currentState = desiredState;
                animator.CrossFadeInFixedTime(desiredState, CrossFadeSeconds);
            }
        }

        private string ResolveDesiredState()
        {
            if (combatant != null && combatant.IsStunned)
            {
                oneShotUntilTime = 0f;
                lastPosture = movement.Posture;
                wasGrounded = usesNetworkState ? networkGrounded : movement.IsGrounded;
                return StunnedState;
            }

            if (Time.time < punchUntilTime)
            {
                return PunchState;
            }

            var grounded = usesNetworkState ? networkGrounded : movement.IsGrounded;
            if (!grounded)
            {
                oneShotUntilTime = 0f;
                wasGrounded = false;
                lastPosture = movement.Posture;
                return AirborneState;
            }

            if (!wasGrounded)
            {
                wasGrounded = true;
                PlayOneShot(LandState, LandSeconds);
            }

            var posture = movement.Posture;
            if (posture != lastPosture)
            {
                var transition = TransitionClip(lastPosture, posture);
                lastPosture = posture;
                if (transition != null)
                {
                    PlayOneShot(transition, TransitionSeconds(transition));
                }
            }

            if (Time.time < oneShotUntilTime && !string.IsNullOrEmpty(oneShotState))
            {
                return oneShotState;
            }

            var settings = movement.MovementSettings;
            var speed = usesNetworkState ? networkSpeed : movement.PlanarSpeed;
            var move = usesNetworkState ? networkMoveLocal : movement.PlanarVelocityLocal;
            var carrying = usesNetworkState
                ? networkCarrying
                : interactor != null && interactor.CarriedItem != null;
            return ResolveLocomotionClip(
                posture,
                carrying,
                speed,
                move,
                settings.WalkSpeed,
                settings.SprintSpeed);
        }

        private void PlayOneShot(string state, float seconds)
        {
            oneShotState = state;
            oneShotUntilTime = Time.time + seconds;
        }

        internal static string ResolveLocomotionClip(
            PlayerPosture posture,
            bool carrying,
            float speed,
            Vector2 localMove,
            float walkSpeed,
            float sprintSpeed)
        {
            var direction = ResolveDirection(localMove);
            var moving = direction != MoveDirection.Neutral && speed >= 0.35f;

            if (posture == PlayerPosture.Prone)
            {
                if (!moving)
                {
                    return carrying ? "Carry_Prone" : "Prone_Idle";
                }

                return carrying
                    ? DirectionClip("Carry_Crawl_Forward", "Carry_Crawl_Back", "Carry_Crawl_Left", "Carry_Crawl_Right", direction)
                    : DirectionClip("Crawl_Forward", "Crawl_Back", "Crawl_Left", "Crawl_Right", direction);
            }

            if (posture == PlayerPosture.Crouching)
            {
                if (!moving)
                {
                    return carrying ? "Carry_Crouch" : "Crouch_Idle_KneesUp";
                }

                return carrying
                    ? DirectionClip(
                        "Carry_Crouch_Walk_Forward",
                        "Carry_Crouch_Walk_Back",
                        "Carry_Crouch_Walk_Left",
                        "Carry_Crouch_Walk_Right",
                        direction)
                    : DirectionClip(
                        "Crouch_Walk_Forward_KneesUp",
                        "Crouch_Walk_Back_KneesUp",
                        "Crouch_Walk_Left_KneesUp",
                        "Crouch_Walk_Right_KneesUp",
                        direction);
            }

            if (!moving)
            {
                return carrying ? "Carry_Idle" : "Idle_Breathing";
            }

            if (speed >= (walkSpeed + sprintSpeed) * 0.5f)
            {
                return carrying
                    ? DirectionClip("Carry_Run", "Carry_Run_Back", "Carry_Run_Left", "Carry_Run_Right", direction)
                    : DirectionClip("Run_SideArms", "Run_Back", "Run_Left", "Run_Right", direction);
            }

            return carrying
                ? DirectionClip("Carry_Walk", "Carry_Walk_Back", "Carry_Walk_Left", "Carry_Walk_Right", direction)
                : DirectionClip("Walk_Wide_Clean", "Walk_Back", "Walk_Left", "Walk_Right", direction);
        }

        internal static MoveDirection ResolveDirection(Vector2 localMove)
        {
            if (localMove.sqrMagnitude < DirectionDeadZone * DirectionDeadZone)
            {
                return MoveDirection.Neutral;
            }

            if (Mathf.Abs(localMove.x) > Mathf.Abs(localMove.y))
            {
                return localMove.x > 0f ? MoveDirection.Left : MoveDirection.Right;
            }

            return localMove.y > 0f ? MoveDirection.Forward : MoveDirection.Back;
        }

        private static string DirectionClip(
            string forward,
            string back,
            string left,
            string right,
            MoveDirection direction) => direction switch
        {
            MoveDirection.Back => back,
            MoveDirection.Left => left,
            MoveDirection.Right => right,
            _ => forward
        };

        private static string TransitionClip(PlayerPosture from, PlayerPosture to) => (from, to) switch
        {
            (PlayerPosture.Standing, PlayerPosture.Crouching) => "Stand_To_Crouch_KneesUp",
            (PlayerPosture.Crouching, PlayerPosture.Standing) => "Crouch_To_Stand_KneesUp",
            (PlayerPosture.Standing, PlayerPosture.Prone) => "Prone_Start",
            (PlayerPosture.Crouching, PlayerPosture.Prone) => "Crouch_To_Prone",
            (PlayerPosture.Prone, PlayerPosture.Standing) => "Prone_End",
            (PlayerPosture.Prone, PlayerPosture.Crouching) => "Prone_To_Crouch",
            _ => null
        };

        private static float TransitionSeconds(string clip) => clip switch
        {
            "Crouch_To_Prone" or "Prone_To_Crouch" => 36f / 30f,
            _ => 24f / 30f
        };

        private static bool HasParameter(Animator target, string name)
        {
            foreach (var parameter in target.parameters)
            {
                if (parameter.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        internal enum MoveDirection
        {
            Neutral,
            Forward,
            Back,
            Left,
            Right
        }
    }
}
