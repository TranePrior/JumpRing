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

        [SerializeField]
        private LineDotsRenderer lineDotsRenderer;

        [SerializeField]
        private LineCornerRenderer lineCornerRenderer;

        [SerializeField]
        private BackgroundTiler backgroundTiler;

        [SerializeField]
        private Camera gameplayCamera;


        public ThemeData ActiveTheme => activeTheme;

        public void Initialize()
        {
            if (activeTheme != null)
            {
                ApplyTheme(activeTheme, null);
            }
        }

        public void ApplyTheme(ThemeData theme, SkinPack pack)
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

            if (lineDotsRenderer != null)
            {
                if (theme.UseLineDots)
                {
                    lineDotsRenderer.Configure(theme.LineDotSprite, theme.LineDotSpacing, theme.LineDotSize);
                    lineDotsRenderer.Activate();
                    linePathGenerator.SetLineVisible(false);
                }
                else
                {
                    lineDotsRenderer.Deactivate();
                    linePathGenerator.SetLineVisible(true);
                }
            }

            if (lineCornerRenderer != null)
            {
                if (theme.UseCorners)
                {
                    lineCornerRenderer.Configure(theme.CornerSprite);
                    lineCornerRenderer.Activate();
                }
                else
                {
                    lineCornerRenderer.Deactivate();
                }
            }

            if (coinStepSpawner != null && theme.CoinPrefab != null)
            {
                coinStepSpawner.SetCoinPrefab(theme.CoinPrefab);
            }

            ApplyBackground(theme);
        }

        public void ApplyBackground(ThemeData theme)
        {
            if (backgroundTiler != null && theme.BackgroundTexture != null)
            {
                backgroundTiler.SetBackground(theme.BackgroundTexture, theme.TileColor);
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.backgroundColor = theme.BackgroundColor;
            }
        }
    }
}
