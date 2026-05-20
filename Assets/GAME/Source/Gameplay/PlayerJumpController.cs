using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using JumpRing.Game.Theming;

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerJumpController : MonoBehaviour, IRunStartGate
    {
        private static readonly Vector3 OriginPosition = Vector3.zero;

        [SerializeField]
        private Rigidbody2D playerRigidbody;

        [SerializeField, Min(0.1f)]
        private float jumpImpulse = 13f;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private PlayerSkinSlot playerSkinSlot;

        private LinePathGenerator linePathGenerator;
        private DifficultyManager difficultyManager;
        private RiskRewardSystem riskRewardSystem;
        private BonusEffectManager bonusEffectManager;

        private Vector3 originalHitTopLocalPos;
        private Vector3 originalHitBottomLocalPos;

        [SerializeField]
        private Transform hitTop;

        [SerializeField]
        private Transform hitBottom;

        [SerializeField]
        private CircleCollider2D hitTopCollider;

        [SerializeField]
        private CircleCollider2D hitBottomCollider;

        [SerializeField, Min(0f)]
        private float lineBoundsPadding = 0.001f;

        [SerializeField, Min(0f)]
        private float lineTouchTolerance = 0.001f;

        [Header("Fall Feel")]
        [SerializeField, Min(1f), Tooltip("Gravity multiplier when falling — makes descent snappier")]
        private float fallGravityMultiplier = 2.2f;

        [SerializeField, Min(1f), Tooltip("Gravity multiplier at jump peak for faster turnaround")]
        private float peakGravityMultiplier = 1.6f;

        [SerializeField, Min(0f), Tooltip("Velocity threshold to consider 'at peak'")]
        private float peakVelocityThreshold = 1.5f;

        [Header("Squash & Stretch")]
        [SerializeField, Tooltip("Transform of the ring visual (skin slot or player sprite)")]
        private Transform ringVisual;

        [SerializeField, Range(0f, 0.4f)]
        private float stretchAmount = 0.18f;

        [SerializeField, Range(0f, 0.4f)]
        private float squashAmount = 0.12f;

        [SerializeField, Min(1f)]
        private float scaleResponseSpeed = 14f;

        private float defaultGravityScale;
        private Vector2 lastDeathPosition;
        private float currentSizeScale = 1f;
        private float permanentSizeBonus;

        /// <summary>
        /// Scales gravity independently. 1 = normal, lower = floaty, higher = snappy.
        /// </summary>
        public float GravityScale { get; set; } = 1f;

        /// <summary>
        /// Scales jump impulse independently. 1 = normal, lower = tiny hops.
        /// </summary>
        public float JumpScale { get; set; } = 1f;

        public Vector2 LastDeathPosition => lastDeathPosition;

        private void Awake()
        {
            linePathGenerator = Object.FindFirstObjectByType<LinePathGenerator>();
            difficultyManager = Object.FindFirstObjectByType<DifficultyManager>();
            riskRewardSystem = Object.FindFirstObjectByType<RiskRewardSystem>();
            bonusEffectManager = Object.FindFirstObjectByType<BonusEffectManager>();
            defaultGravityScale = playerRigidbody.gravityScale;
            originalHitTopLocalPos = hitTop.localPosition;
            originalHitBottomLocalPos = hitBottom.localPosition;
        }

        private void Start()
        {
            ResetPlayerToOrigin();
            SnapPlayerToLine();
        }

        private void OnEnable()
        {
            runSessionController.RegisterStartGate(this);
            runSessionController.RunFinished += OnRunFinished;
        }

        private void OnDisable()
        {
            runSessionController.UnregisterStartGate(this);
            runSessionController.RunFinished -= OnRunFinished;
        }

        private void Update()
        {
            if (!WasJumpPressed() || IsPointerOverUI())
            {
                return;
            }

            if (runSessionController.IsInReadyState)
            {
                runSessionController.BeginGameplay();
            }

            if (!runSessionController.CanControlPlayer)
            {
                if (!runSessionController.CanStartRun)
                {
                    return;
                }

                runSessionController.StartRun();
                return;
            }

            var currentScore = runSessionController.RegisterTap();

            if (difficultyManager != null)
            {
                difficultyManager.NotifyTap(currentScore);

                var playerY = playerRigidbody.position.y;
                var lineY = linePathGenerator.EvaluateHeightAtX(playerRigidbody.position.x);
                difficultyManager.NotifyTapDistance(Mathf.Abs(playerY - lineY));
            }

            if (riskRewardSystem != null)
            {
                riskRewardSystem.NotifyTap();
            }

            if (bonusEffectManager != null)
            {
                bonusEffectManager.NotifyTap();
            }

            var velocity = playerRigidbody.linearVelocity;
            velocity.y = jumpImpulse * JumpScale;
            playerRigidbody.linearVelocity = velocity;
            playerSkinSlot?.Skin?.OnJump();
        }

        private void FixedUpdate()
        {
            if (!runSessionController.CanControlPlayer)
            {
                playerRigidbody.gravityScale = 0f;
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.angularVelocity = 0f;

                // Safety net: ensure line is inside the hole when idle
                if (runSessionController.CanStartRun && !IsLineInsidePlayableWindow())
                {
                    ResetPlayerToOrigin();
                    SnapPlayerToLine();
                }
                else if (runSessionController.IsInReadyState && !IsLineInsidePlayableWindow())
                {
                    SnapPlayerToLine();
                }

                return;
            }

            // Snappier fall: boost gravity when descending or at jump peak
            var vy = playerRigidbody.linearVelocity.y;
            if (vy < -peakVelocityThreshold)
            {
                playerRigidbody.gravityScale = defaultGravityScale * fallGravityMultiplier * GravityScale;
            }
            else if (Mathf.Abs(vy) < peakVelocityThreshold)
            {
                playerRigidbody.gravityScale = defaultGravityScale * peakGravityMultiplier * GravityScale;
            }
            else
            {
                playerRigidbody.gravityScale = defaultGravityScale * GravityScale;
            }

            if (!linePathGenerator.IsTouchingLine(hitTopCollider, lineTouchTolerance) &&
                !linePathGenerator.IsTouchingLine(hitBottomCollider, lineTouchTolerance))
            {
                return;
            }

            // Skip death during invincibility
            if (bonusEffectManager != null && bonusEffectManager.IsInvincible)
            {
                return;
            }

            // Stop player and trigger death flow (panel will be shown)
            lastDeathPosition = playerRigidbody.position;
            playerRigidbody.gravityScale = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            runSessionController.FinishRun();
        }

        private void LateUpdate()
        {
            if (ringVisual == null)
            {
                return;
            }

            if (!runSessionController.CanControlPlayer)
            {
                ringVisual.localScale = Vector3.Lerp(ringVisual.localScale, Vector3.one * currentSizeScale, Time.deltaTime * scaleResponseSpeed);
                return;
            }

            var vy = playerRigidbody.linearVelocity.y;
            var normalizedVy = Mathf.Clamp(vy / jumpImpulse, -1f, 1f);

            float sx, sy;
            if (normalizedVy > 0.05f)
            {
                // Going up — stretch vertically
                sy = 1f + stretchAmount * normalizedVy;
                sx = 1f - stretchAmount * normalizedVy * 0.5f;
            }
            else if (normalizedVy < -0.05f)
            {
                // Falling — squash vertically
                var abs = -normalizedVy;
                sy = 1f - squashAmount * abs;
                sx = 1f + squashAmount * abs * 0.5f;
            }
            else
            {
                sx = 1f;
                sy = 1f;
            }

            var target = new Vector3(sx * currentSizeScale, sy * currentSizeScale, currentSizeScale);
            ringVisual.localScale = Vector3.Lerp(ringVisual.localScale, target, Time.deltaTime * scaleResponseSpeed);
        }

        public void SetPermanentSizeBonus(float bonus)
        {
            permanentSizeBonus = Mathf.Max(0f, bonus);
            ApplySizeModifier(0f);
        }

        /// <summary>
        /// Expands or resets the playable gap between hitTop and hitBottom.
        /// amount > 0 expands each side by that amount; 0 resets to original.
        /// </summary>
        public void ApplySizeModifier(float amount)
        {
            float baseScale = amount <= 0f ? 1f : 1f + amount;
            currentSizeScale = Mathf.Min(baseScale + permanentSizeBonus, 2f);
        }

        /// <summary>
        /// Revives the player at the given X position with line centered.
        /// Y is set to the center of the playable window automatically.
        /// </summary>
        public void RevivePlayer(float reviveX)
        {
            var localCenterOffset = (originalHitTopLocalPos.y + originalHitBottomLocalPos.y) * 0.5f;
            var lineY = linePathGenerator.EvaluateHeightAtX(reviveX);
            var targetY = lineY - localCenterOffset;

            var revivePos = new Vector2(reviveX, targetY);
            playerRigidbody.transform.SetPositionAndRotation(
                new Vector3(revivePos.x, revivePos.y, 0f), Quaternion.identity);
            playerRigidbody.position = revivePos;
            playerRigidbody.rotation = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.gravityScale = defaultGravityScale;

            Physics2D.SyncTransforms();
        }

        private void OnRunFinished()
        {
            ResetPlayerToOrigin();
            SnapPlayerToLine();
        }

        public bool CanStartRun()
        {
            ResetPlayerToOrigin();
            SnapPlayerToLine();

            if (!IsLineInsidePlayableWindow())
            {
                Debug.LogError("Run start blocked: line is outside HitTop/HitBottom bounds.");
                return false;
            }

            return true;
        }

        private void ResetPlayerToOrigin()
        {
            playerRigidbody.transform.SetPositionAndRotation(OriginPosition, Quaternion.identity);
            playerRigidbody.position = Vector2.zero;
            playerRigidbody.rotation = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        private void SnapPlayerToLine()
        {
            var px = playerRigidbody.position.x;
            var lineY = linePathGenerator.EvaluateHeightAtX(px);
            var localCenterOffset = (originalHitTopLocalPos.y + originalHitBottomLocalPos.y) * 0.5f;
            var targetY = lineY - localCenterOffset;

            playerRigidbody.transform.SetPositionAndRotation(
                new Vector3(px, targetY, 0f), Quaternion.identity);
            playerRigidbody.position = new Vector2(px, targetY);
        }

        private bool IsLineInsidePlayableWindow()
        {
            var lineYAtRing = linePathGenerator.EvaluateHeightAtX(playerRigidbody.position.x);
            var minAllowedY = Mathf.Min(hitBottom.position.y, hitTop.position.y) + lineBoundsPadding;
            var maxAllowedY = Mathf.Max(hitBottom.position.y, hitTop.position.y) - lineBoundsPadding;
            return lineYAtRing > minAllowedY && lineYAtRing < maxAllowedY;
        }

        private static bool IsPointerOverUI()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return eventSystem.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
            }
#endif

            return eventSystem.IsPointerOverGameObject();
        }

        private static bool WasJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }
}
