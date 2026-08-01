using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public interface IReviveService
    {
        /// <summary>True while the player still has a heart to spend on a revive.</summary>
        bool CanReviveWithHeart { get; }

        /// <summary>Spends one heart and puts the run back on its feet.</summary>
        void ReviveWithHeart();

        /// <summary>Puts the run back on its feet without touching the heart stock.</summary>
        void ReviveWithAd();
    }

    /// <summary>
    /// Owns the revive sequence: where the player comes back, what happens to the coin field and
    /// the camera, and which session state the run lands in.
    /// </summary>
    /// <remarks>
    /// This used to live inside <c>SecondChancePresenter</c>, which meant a UI class held five
    /// gameplay references and decided gameplay policy — and the sequence could only be exercised
    /// in play mode, with a full scene behind it. The window now only decides whether a revive was
    /// earned; how one is performed is here, behind interfaces, and covered by edit mode tests.
    /// </remarks>
    public sealed class ReviveService : MonoBehaviour, IReviveService
    {
        [SerializeField]
        private PlayerJumpController playerJumpController;

        [SerializeField]
        private CoinStepSpawner coinStepSpawner;

        [SerializeField]
        private CameraFollowTarget cameraFollowTarget;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        // How far behind the death position the player comes back. Landing exactly where the run
        // ended drops the player back onto whatever killed them.
        [SerializeField, Min(0.1f)]
        private float reviveOffset = 2f;

        private IRevivablePlayer player;
        private ICoinSpawner coinSpawner;
        private ICameraFollow gameplayCamera;
        private IRunSessionController runSession;
        private ISecondChanceStock secondChanceStock;

        public bool CanReviveWithHeart => secondChanceStock.SecondChanceCount > 0;

        private void Awake()
        {
            Construct(
                playerJumpController,
                coinStepSpawner,
                cameraFollowTarget,
                runSessionController,
                bonusEffectManager);
        }

        public void Construct(
            IRevivablePlayer revivablePlayer,
            ICoinSpawner coins,
            ICameraFollow followCamera,
            IRunSessionController session,
            ISecondChanceStock stock)
        {
            player = revivablePlayer;
            coinSpawner = coins;
            gameplayCamera = followCamera;
            runSession = session;
            secondChanceStock = stock;
        }

        public void ReviveWithHeart()
        {
            secondChanceStock.ConsumeSecondChance();
            Revive();
        }

        public void ReviveWithAd()
        {
            secondChanceStock.StartInvincibility();
            Revive();
        }

        private void Revive()
        {
            var deathPosition = player.LastDeathPosition;
            player.RevivePlayer(deathPosition.x - reviveOffset);

            // Order matters: the coin field lays itself out around the player's current position,
            // and the camera cuts to it. Both read the position the teleport above just wrote.
            coinSpawner.RespawnFromCurrentPosition();
            gameplayCamera.SnapImmediate();

            runSession.ReviveToReady();
        }
    }
}
