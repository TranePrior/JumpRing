using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class BonusHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [SerializeField]
        private BonusConfig bonusConfig;

        [Header("UI Elements")]
        [SerializeField]
        private GameObject bonusPanel;

        [SerializeField]
        private Image bonusIcon;

        [SerializeField]
        private Image timerFill;

        private float totalDuration;

        private void OnEnable()
        {
            if (bonusEffectManager != null)
            {
                bonusEffectManager.BonusActivated += OnBonusActivated;
                bonusEffectManager.BonusDeactivated += OnBonusDeactivated;
            }

            if (bonusPanel != null)
            {
                bonusPanel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (bonusEffectManager != null)
            {
                bonusEffectManager.BonusActivated -= OnBonusActivated;
                bonusEffectManager.BonusDeactivated -= OnBonusDeactivated;
            }
        }

        private void Update()
        {
            if (bonusEffectManager == null || !bonusEffectManager.HasActiveBonus)
            {
                return;
            }

            if (timerFill != null && totalDuration > 0f)
            {
                timerFill.fillAmount = bonusEffectManager.RemainingTime / totalDuration;
            }
        }

        private void OnBonusActivated(BonusType type)
        {
            if (bonusPanel != null)
            {
                bonusPanel.SetActive(true);
            }

            if (bonusConfig != null && bonusIcon != null)
            {
                var entry = bonusConfig.GetEntry(type);

                if (entry.icon != null)
                {
                    bonusIcon.sprite = entry.icon;
                    bonusIcon.enabled = true;
                }
            }

            if (bonusConfig != null)
            {
                var entry = bonusConfig.GetEntry(type);
                totalDuration = entry.duration;
            }

            if (timerFill != null)
            {
                timerFill.fillAmount = totalDuration > 0f ? 1f : 0f;
            }
        }

        private void OnBonusDeactivated(BonusType type)
        {
            if (bonusPanel != null)
            {
                bonusPanel.SetActive(false);
            }

            totalDuration = 0f;
        }
    }
}
