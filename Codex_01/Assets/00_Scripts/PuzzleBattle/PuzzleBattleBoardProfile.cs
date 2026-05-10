using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Board Profile", fileName = "PuzzleBattleBoardProfile")]
    public sealed class PuzzleBattleBoardProfile : ScriptableObject
    {
        [SerializeField] private int columns = 6;
        [SerializeField] private int rows = 5;
        [SerializeField] private float boardInset = 0.45f;
        [SerializeField] private int minimumMatchLength = 3;
        [SerializeField] private bool avoidStartingMatches = true;
        [SerializeField] private OrbPrefabCatalog orbPrefabCatalog;
        [SerializeField] private OrbVisualDefinition[] orbDefinitions;
        [SerializeField] private OrbMotionProfile motionProfile;

        public int Columns => Mathf.Max(3, columns);
        public int Rows => Mathf.Max(3, rows);
        public float BoardInset => Mathf.Max(0f, boardInset);
        public int MinimumMatchLength => Mathf.Max(3, minimumMatchLength);
        public bool AvoidStartingMatches => avoidStartingMatches;
        public OrbPrefabCatalog OrbPrefabCatalog => orbPrefabCatalog;
        public OrbVisualDefinition[] OrbDefinitions
        {
            get
            {
                OrbVisualDefinition[] catalogDefinitions = orbPrefabCatalog != null ? orbPrefabCatalog.Definitions : null;
                return catalogDefinitions != null && catalogDefinitions.Length > 0 ? catalogDefinitions : orbDefinitions;
            }
        }
        public OrbMotionProfile MotionProfile => motionProfile;

        public BoardPieceView GetOrbPrefab(OrbVisualDefinition definition)
        {
            return orbPrefabCatalog != null ? orbPrefabCatalog.GetPrefab(definition) : null;
        }

        public void SetAuthoringDefaults(
            int boardColumns,
            int boardRows,
            float inset,
            OrbPrefabCatalog catalog,
            OrbMotionProfile profile)
        {
            ApplyDefaults(boardColumns, boardRows, inset, catalog != null ? catalog.Definitions : orbDefinitions, catalog, profile, HideFlags.None);
        }

        public void SetRuntimeDefaults(
            int boardColumns,
            int boardRows,
            float inset,
            OrbVisualDefinition[] definitions,
            OrbMotionProfile profile)
        {
            ApplyDefaults(boardColumns, boardRows, inset, definitions, null, profile, HideFlags.DontSave);
        }

        public void SetOrbCatalog(OrbPrefabCatalog catalog)
        {
            orbPrefabCatalog = catalog;

            if (catalog != null && (orbDefinitions == null || orbDefinitions.Length == 0))
            {
                orbDefinitions = catalog.Definitions;
            }
        }

        public void SetMotionProfile(OrbMotionProfile profile)
        {
            motionProfile = profile;
        }

        private void ApplyDefaults(
            int boardColumns,
            int boardRows,
            float inset,
            OrbVisualDefinition[] definitions,
            OrbPrefabCatalog catalog,
            OrbMotionProfile profile,
            HideFlags flags)
        {
            columns = boardColumns;
            rows = boardRows;
            boardInset = inset;
            minimumMatchLength = 3;
            avoidStartingMatches = true;
            orbPrefabCatalog = catalog;
            orbDefinitions = definitions;
            motionProfile = profile;
            hideFlags = flags;
        }
    }
}
