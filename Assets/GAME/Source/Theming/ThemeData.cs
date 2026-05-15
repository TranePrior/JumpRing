using UnityEngine;

namespace JumpRing.Game.Theming
{
    [CreateAssetMenu(fileName = "ThemeData", menuName = "JumpRing/Theme Data")]
    public sealed class ThemeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string themeName = "Default";

        [Header("Player")]
        [SerializeField]
        private GameObject playerSkinPrefab;

        [Header("Line")]
        [SerializeField]
        private Material lineMaterial;

        [Header("Line Dots")]
        [SerializeField, Tooltip("If set, LineRenderer is hidden and dots are spawned along the path")]
        private Sprite lineDotSprite;

        [SerializeField, Min(0.1f)]
        private float lineDotSpacing = 1.5f;

        [SerializeField, Min(0.01f)]
        private float lineDotSize = 0.5f;

        [Header("Coins")]
        [SerializeField]
        private GameObject coinPrefab;

        public string ThemeName => themeName;
        public GameObject PlayerSkinPrefab => playerSkinPrefab;
        public Material LineMaterial => lineMaterial;
        public Sprite LineDotSprite => lineDotSprite;
        public float LineDotSpacing => lineDotSpacing;
        public float LineDotSize => lineDotSize;
        public bool UseLineDots => lineDotSprite != null;
        public GameObject CoinPrefab => coinPrefab;
    }
}
