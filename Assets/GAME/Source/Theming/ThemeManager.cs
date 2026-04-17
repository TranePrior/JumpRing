using UnityEngine;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.Theming
{
    public sealed class ThemeManager : MonoBehaviour
    {
        [SerializeField]
        private ThemeData activeTheme;

        [Header("References")]
        [SerializeField]
        private PlayerSkinSlot playerSkinSlot;

        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private CoinStepSpawner coinStepSpawner;

        public ThemeData ActiveTheme => activeTheme;

        public void Initialize()
        {
            if (activeTheme != null)
            {
                ApplyTheme(activeTheme);
            }
        }

        public void ApplyTheme(ThemeData theme)
        {
            activeTheme = theme;

            if (playerSkinSlot != null && theme.PlayerSkinPrefab != null)
            {
                playerSkinSlot.ApplySkin(theme.PlayerSkinPrefab);
            }

            if (linePathGenerator != null && theme.LineMaterial != null)
            {
                linePathGenerator.SetLineMaterial(theme.LineMaterial);
            }

            if (coinStepSpawner != null && theme.CoinPrefab != null)
            {
                coinStepSpawner.SetCoinPrefab(theme.CoinPrefab);
            }
        }
    }
}
