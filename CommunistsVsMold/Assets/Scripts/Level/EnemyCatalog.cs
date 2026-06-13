using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Каталог врагов: строковый тип из макета уровня (zombie/runner/pistol/…) → префаб.
    /// Заполняется в инспекторе (сборщик сцены). Используется LevelBuilder при спавне.
    /// </summary>
    public class EnemyCatalog : MonoBehaviour
    {
        public static EnemyCatalog Instance { get; private set; }

        [System.Serializable]
        public class Entry
        {
            public string kind;
            public GameObject prefab;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Префаб по типу или null, если не задан.</summary>
        public GameObject Get(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].kind == kind) return entries[i].prefab;
            return null;
        }
    }
}
