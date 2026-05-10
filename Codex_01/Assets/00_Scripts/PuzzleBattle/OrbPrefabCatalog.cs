using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Orb Prefab Catalog", fileName = "OrbPrefabCatalog")]
    public sealed class OrbPrefabCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private OrbVisualDefinition definition;
            [SerializeField] private BoardPieceView prefab;

            public Entry(OrbVisualDefinition definition, BoardPieceView prefab)
            {
                this.definition = definition;
                this.prefab = prefab;
            }

            public OrbVisualDefinition Definition => definition;
            public BoardPieceView Prefab => prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public Entry[] Entries => entries ?? Array.Empty<Entry>();

        public OrbVisualDefinition[] Definitions
        {
            get
            {
                Entry[] sourceEntries = Entries;
                List<OrbVisualDefinition> definitions = new List<OrbVisualDefinition>(sourceEntries.Length);

                for (int i = 0; i < sourceEntries.Length; i++)
                {
                    if (sourceEntries[i].Definition != null)
                    {
                        definitions.Add(sourceEntries[i].Definition);
                    }
                }

                return definitions.ToArray();
            }
        }

        public BoardPieceView GetPrefab(OrbVisualDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            Entry[] sourceEntries = Entries;
            string targetId = definition.OrbId;

            for (int i = 0; i < sourceEntries.Length; i++)
            {
                if (sourceEntries[i].Definition == definition && sourceEntries[i].Prefab != null)
                {
                    return sourceEntries[i].Prefab;
                }
            }

            for (int i = 0; i < sourceEntries.Length; i++)
            {
                OrbVisualDefinition entryDefinition = sourceEntries[i].Definition;

                if (entryDefinition != null &&
                    sourceEntries[i].Prefab != null &&
                    string.Equals(entryDefinition.OrbId, targetId, StringComparison.Ordinal))
                {
                    return sourceEntries[i].Prefab;
                }
            }

            return null;
        }

        public void SetAuthoringEntries(Entry[] newEntries)
        {
            entries = newEntries ?? Array.Empty<Entry>();
            hideFlags = HideFlags.None;
        }

        public void SetRuntimeEntries(Entry[] newEntries)
        {
            entries = newEntries ?? Array.Empty<Entry>();
            hideFlags = HideFlags.DontSave;
        }
    }
}
