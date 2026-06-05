using System;
using UnityEngine;

namespace JumpRing.Game.Core.Localization
{
    [CreateAssetMenu(fileName = "LocalizationData", menuName = "JumpRing/Localization Data")]
    public sealed class LocalizationData : ScriptableObject
    {
        [SerializeField]
        private LocalizationEntry[] entries;

        public string GetText(LocalizationKey key)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Key == key)
                    return entries[i].Value;
            }

            return key.ToString();
        }

        [Serializable]
        public struct LocalizationEntry
        {
            [SerializeField]
            private LocalizationKey key;

            [SerializeField]
            private string value;

            public LocalizationKey Key => key;
            public string Value => value;

            public LocalizationEntry(LocalizationKey key, string value)
            {
                this.key = key;
                this.value = value;
            }
        }
    }
}
