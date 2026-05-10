using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Frost Well", fileName = "FrostWellSkillDefinition")]
    public sealed class FrostWellSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject wellEffectPrefab;
        [SerializeField] private int baseDamagePerTurn = 3;
        [SerializeField] private int damagePerLevel = 1;
        [SerializeField] private float damagePerBlue = 0.65f;
        [SerializeField] private int baseDurationTurns = 3;
        [SerializeField] private int durationPerLevel = 1;
        [SerializeField] private float extraTurnsPerBlue = 0.33f;
        [SerializeField] private float minRadiusCells = 0.75f;
        [SerializeField] private float maxRadiusCells = 1.65f;
        [SerializeField] private float radiusBonusPerLevel = 0.08f;
        [SerializeField] private float startSlowMultiplier = 0.72f;
        [SerializeField] private float maxSlowMultiplier = 0.45f;
        [SerializeField] private float extraSlowPerLevel = 0.04f;

        public GameObject WellEffectPrefab => wellEffectPrefab;

        public int GetDamagePerTurn(int level, int blueCount)
        {
            return Mathf.Max(1, baseDamagePerTurn + (GetLevelOffset(level) * damagePerLevel) + Mathf.RoundToInt(blueCount * damagePerBlue));
        }

        public int GetDurationTurns(int level, int blueCount)
        {
            return Mathf.Max(1, baseDurationTurns + (GetLevelOffset(level) * durationPerLevel) + Mathf.FloorToInt(blueCount * extraTurnsPerBlue));
        }

        public float GetMinRadius(int level)
        {
            return Mathf.Max(0.1f, minRadiusCells + (GetLevelOffset(level) * radiusBonusPerLevel));
        }

        public float GetMaxRadius(int level)
        {
            return Mathf.Max(GetMinRadius(level), maxRadiusCells + (GetLevelOffset(level) * radiusBonusPerLevel));
        }

        public float GetStartSlow(int level)
        {
            return Mathf.Clamp(startSlowMultiplier - (GetLevelOffset(level) * extraSlowPerLevel * 0.5f), 0.1f, 1f);
        }

        public float GetMaxSlow(int level)
        {
            return Mathf.Clamp(maxSlowMultiplier - (GetLevelOffset(level) * extraSlowPerLevel), 0.1f, GetStartSlow(level));
        }

        public void SetAuthoringDefaults()
        {
            ApplyDefaults(HideFlags.None);
        }

        public void SetRuntimeDefaults()
        {
            ApplyDefaults(HideFlags.DontSave);
        }

        public void SetEffectPrefab(GameObject prefab)
        {
            wellEffectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.FrostWell,
                "서리 우물",
                "우물",
                "파란 구슬이 터지면 적 위치에 원형 우물을 설치해 지속 피해와 감속을 부여합니다.",
                new Color(0.36f, 0.77f, 1f, 1f),
                flags);
            wellEffectPrefab = null;
            baseDamagePerTurn = 3;
            damagePerLevel = 1;
            damagePerBlue = 0.65f;
            baseDurationTurns = 3;
            durationPerLevel = 1;
            extraTurnsPerBlue = 0.33f;
            minRadiusCells = 0.75f;
            maxRadiusCells = 1.65f;
            radiusBonusPerLevel = 0.08f;
            startSlowMultiplier = 0.72f;
            maxSlowMultiplier = 0.45f;
            extraSlowPerLevel = 0.04f;
        }
    }
}
