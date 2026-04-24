using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class HeartsHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [SerializeField]
        private Image[] heartImages;

        [SerializeField]
        private Color activeColor = new Color(1f, 0.1f, 0.2f, 1f);

        [SerializeField]
        private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        private void OnEnable()
        {
            bonusEffectManager.SecondChanceCountChanged += UpdateHearts;
            UpdateHearts(bonusEffectManager.SecondChanceCount);
        }

        private void OnDisable()
        {
            bonusEffectManager.SecondChanceCountChanged -= UpdateHearts;
        }

        private void UpdateHearts(int count)
        {
            for (var i = 0; i < heartImages.Length; i++)
            {
                heartImages[i].color = i < count ? activeColor : inactiveColor;
            }
        }
    }
}
