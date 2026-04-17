using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerJumpController : MonoBehaviour, IRunStartGate
    {
        private static readonly Vector3 OriginPosition = Vector3.zero;

        [SerializeField]
        private Rigidbody2D playerRigidbody;

        [SerializeField, Min(0.1f)]
        private float jumpImpulse = 8f;

        [SerializeField]
        private RunSessionController runSessionController;

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
            if (!WasJumpPressed())
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
            velocity.y = 0f;
            playerRigidbody.linearVelocity = velocity;
            playerRigidbody.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
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

            playerRigidbody.gravityScale = defaultGravityScale;

            if (!linePathGenerator.IsTouchingLine(hitTopCollider, lineTouchTolerance) &&
                !linePathGenerator.IsTouchingLine(hitBottomCollider, lineTouchTolerance))
            {
                return;
            }

            runSessionController.FinishRun();
            playerRigidbody.gravityScale = 0f;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
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
