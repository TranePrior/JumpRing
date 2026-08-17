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
            int index = IndexOf(key);

            return index >= 0 ? entries[index].Value : key.ToString();
        }

        /// <summary>
        /// Whether this language actually defines the key. <see cref="GetText"/> answers with the key
        /// name when it does not, which is indistinguishable from a translation that happens to equal
        /// the key ("Select"), so coverage has to be asked about directly.
        /// </summary>
        public bool HasText(LocalizationKey key)
        {
            return IndexOf(key) >= 0;
        }

        private int IndexOf(LocalizationKey key)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Key == key)
                    return i;
            }

            return -1;
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
