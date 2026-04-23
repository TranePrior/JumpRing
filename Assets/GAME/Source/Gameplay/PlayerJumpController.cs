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

        [SerializeField]
        private Transform hitTop;

        [SerializeField]
        private Transform hitBottom;

        [SerializeField]
        private CircleCollider2D hitTopCollider;

        [SerializeField]
        private CircleCollider2D hitBottomCollider;

        [SerializeField, Min(0.0001f)]
        private float alignmentTolerance = 0.001f;

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

        private void Awake()
        {
            linePathGenerator = Object.FindFirstObjectByType<LinePathGenerator>();
            difficultyManager = Object.FindFirstObjectByType<DifficultyManager>();
            riskRewardSystem = Object.FindFirstObjectByType<RiskRewardSystem>();
            defaultGravityScale = playerRigidbody.gravityScale;
        }

        private void Start()
        {
            ResetPlayerToOrigin();
            AlignLineToPlayableWindow();
        }

        private void OnEnable()
        {
            runSessionController.RegisterStartGate(this);
        }

        private void OnDisable()
        {
            runSessionController.UnregisterStartGate(this);
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
            }

            if (riskRewardSystem != null)
            {
                riskRewardSystem.NotifyTap();
            }

            var velocity = playerRigidbody.linearVelocity;
            velocity.y = jumpImpulse;
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
                return;
            }

            // Snappier fall: boost gravity when descending or at jump peak
            var vy = playerRigidbody.linearVelocity.y;
            if (vy < -peakVelocityThreshold)
            {
                playerRigidbody.gravityScale = defaultGravityScale * fallGravityMultiplier;
            }
            else if (Mathf.Abs(vy) < peakVelocityThreshold)
            {
                playerRigidbody.gravityScale = defaultGravityScale * peakGravityMultiplier;
            }
            else
            {
                playerRigidbody.gravityScale = defaultGravityScale;
            }

            if (!linePathGenerator.IsTouchingLine(hitTopCollider, lineTouchTolerance) &&
                !linePathGenerator.IsTouchingLine(hitBottomCollider, lineTouchTolerance))
            {
                return;
            }

            runSessionController.FinishRun();
            playerSkinSlot?.Skin?.OnDie();
            playerRigidbody.gravityScale = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        private void LateUpdate()
        {
            if (ringVisual == null)
            {
                return;
            }

            if (!runSessionController.CanControlPlayer)
            {
                ringVisual.localScale = Vector3.Lerp(ringVisual.localScale, Vector3.one, Time.deltaTime * scaleResponseSpeed);
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

            var target = new Vector3(sx, sy, 1f);
            ringVisual.localScale = Vector3.Lerp(ringVisual.localScale, target, Time.deltaTime * scaleResponseSpeed);
        }

        public bool CanStartRun()
        {
            ResetPlayerToOrigin();
            AlignLineToPlayableWindow();

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

        private void AlignLineToPlayableWindow()
        {
            var lineAnchorPoint = new Vector2(playerRigidbody.position.x, GetPlayableWindowCenterY());
            linePathGenerator.AlignAndRebuildToPoint(lineAnchorPoint);
        }

        private bool IsLineInsidePlayableWindow()
        {
            var lineAnchorPoint = new Vector2(playerRigidbody.position.x, GetPlayableWindowCenterY());

            if (!linePathGenerator.IsAlignedWithPoint(lineAnchorPoint, alignmentTolerance))
            {
                return false;
            }

            var lineYAtRing = linePathGenerator.EvaluateHeightAtX(playerRigidbody.position.x);
            var minAllowedY = Mathf.Min(hitBottom.position.y, hitTop.position.y) + lineBoundsPadding;
            var maxAllowedY = Mathf.Max(hitBottom.position.y, hitTop.position.y) - lineBoundsPadding;
            return lineYAtRing > minAllowedY && lineYAtRing < maxAllowedY;
        }

        private float GetPlayableWindowCenterY()
        {
            return (hitBottom.position.y + hitTop.position.y) * 0.5f;
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
