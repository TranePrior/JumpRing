using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class TapHandAnimator : MonoBehaviour
    {
        [SerializeField]
        private Sprite tapGestureSprite;

        [SerializeField]
        private Sprite handReleasedSprite;

        [Header("Sprite Switch")]
        [SerializeField, Min(0.1f)]
        private float switchInterval = 0.6f;

        [Header("Scale Pulse")]
        [SerializeField]
        private float scaleMin = 0.9f;

        [SerializeField]
        private float scaleMax = 1.1f;

        private Image image;
        private float switchTimer;
        private bool showingTapGesture;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            showingTapGesture = false;
            switchTimer = 0f;

            if (image != null && handReleasedSprite != null)
            {
                image.sprite = handReleasedSprite;
            }

            transform.localScale = Vector3.one * scaleMax;
        }

        private void OnDisable()
        {
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            switchTimer += Time.deltaTime;
            float t = switchTimer / switchInterval;

            if (showingTapGesture)
            {
                float scale = Mathf.Lerp(scaleMin, scaleMax, t);
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                float scale = Mathf.Lerp(scaleMax, scaleMin, t);
                transform.localScale = new Vector3(scale, scale, 1f);
            }

            if (switchTimer >= switchInterval)
            {
                switchTimer = 0f;
                SwitchSprite();
            }
        }

        private void SwitchSprite()
        {
            showingTapGesture = !showingTapGesture;
            image.sprite = showingTapGesture ? tapGestureSprite : handReleasedSprite;
        }
    }
}
