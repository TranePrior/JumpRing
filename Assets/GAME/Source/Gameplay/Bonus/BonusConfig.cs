using System;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    [CreateAssetMenu(fileName = "BonusConfig", menuName = "JumpRing/Bonus Config")]
    public sealed class BonusConfig : ScriptableObject
    {
        [Serializable]
        public struct BonusEntry
        {
            public BonusType type;
            public GameObject prefab;
            public float duration;
            public float weight;
            public Sprite icon;
        }

        [SerializeField]
        private BonusEntry[] entries =
        {
            new() { type = BonusType.SlowMotion, duration = 5f, weight = 25f },
            new() { type = BonusType.SecondChance, duration = 0f, weight = 15f },
            new() { type = BonusType.ScoreBoost, duration = 10f, weight = 20f },
            new() { type = BonusType.CalmLine, duration = 10f, weight = 20f },
            new() { type = BonusType.SizeUp, duration = 5f, weight = 20f },
        };

        public BonusEntry[] Entries => entries;

        public BonusEntry GetEntry(BonusType type)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].type == type)
                {
                    return entries[i];
                }
            }

            return default;
        }
    }
}
