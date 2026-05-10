using UnityEngine;

namespace PuzzleBattle
{
    public enum PuzzleBattleSkillId
    {
        OrbVolley,
        FrostWell,
        FlameCurtain,
        BatSwarm,
        LightningStrike,
        CharmingHeart,
        Earthquake,
        PoisonNeedles,
        IceOrb,
        SolarBeacon,
        TrapMine
    }

    public abstract class PuzzleBattleSkillDefinition : ScriptableObject
    {
        [SerializeField] private PuzzleBattleSkillId skillId = PuzzleBattleSkillId.OrbVolley;
        [SerializeField] private string displayName = "Skill";
        [SerializeField] private string shortName = "Skill";
        [SerializeField, TextArea(2, 4)] private string description = "Skill description.";
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField, Range(1, 5)] private int maxLevel = 5;

        public PuzzleBattleSkillId SkillId => skillId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? skillId.ToString() : displayName;
        public string ShortName => string.IsNullOrWhiteSpace(shortName) ? DisplayName : shortName;
        public string Description => description;
        public Color AccentColor => accentColor;
        public int MaxLevel => Mathf.Clamp(maxLevel, 1, 5);

        protected void SetCommonDefaults(
            PuzzleBattleSkillId id,
            string nameValue,
            string shortNameValue,
            string descriptionValue,
            Color accentValue,
            HideFlags flags)
        {
            skillId = id;
            displayName = nameValue;
            shortName = shortNameValue;
            description = descriptionValue;
            accentColor = accentValue;
            maxLevel = 5;
            hideFlags = flags;
        }

        protected int GetLevelOffset(int level)
        {
            return Mathf.Clamp(level, 1, MaxLevel) - 1;
        }
    }
}
