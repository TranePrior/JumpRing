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

        [Header("Coins")]
        [SerializeField]
        private GameObject coinPrefab;

        public string ThemeName => themeName;
        public GameObject PlayerSkinPrefab => playerSkinPrefab;
        public Material LineMaterial => lineMaterial;
        public GameObject CoinPrefab => coinPrefab;
    }
}
