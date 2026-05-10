using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Flame Curtain", fileName = "FlameCurtainSkillDefinition")]
    public sealed class FlameCurtainSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject curtainEffectPrefab;
        [SerializeField] private int baseDamagePerTurn = 7;
        [SerializeField] private int damagePerLevel = 2;
        [SerializeField] private float damagePerRed = 2.5f;
        [SerializeField] private int baseDurationTurns = 3;
        [SerializeField] private int durationPerLevel = 1;
        [SerializeField] private float extraTurnsPerRed = 0.5f;
        [SerializeField] private float startWidthNormalized = 0.35f;
        [SerializeField] private float widthBonusPerLevel = 0.08f;
        [SerializeField] private int fullSpanThreshold = 5;
        [SerializeField] private int thresholdReductionPerLevel = 0;

        public GameObject CurtainEffectPrefab => curtainEffectPrefab;

        public int GetDamagePerTurn(int level, int redCount)
        {
            return Mathf.Max(1, baseDamagePerTurn + (GetLevelOffset(level) * damagePerLevel) + Mathf.RoundToInt(redCount * damagePerRed));
        }

        public int GetDurationTurns(int level, int redCount)
        {
            return Mathf.Max(1, baseDurationTurns + (GetLevelOffset(level) * durationPerLevel) + Mathf.FloorToInt(redCount * extraTurnsPerRed));
        }

        public float GetStartWidthNormalized(int level)
        {
            return Mathf.Clamp01(startWidthNormalized + (GetLevelOffset(level) * widthBonusPerLevel));
        }

        public int GetFullSpanThreshold(int level)
        {
            return Mathf.Max(1, fullSpanThreshold - (GetLevelOffset(level) * thresholdReductionPerLevel));
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
            curtainEffectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.FlameCurtain,
                "화염 장막",
                "장막",
                "붉은 구슬이 터지면 적 가로줄에 화염 장막을 생성해 지나가는 적에게 피해를 줍니다.",
                new Color(1f, 0.38f, 0.19f, 1f),
                flags);
            curtainEffectPrefab = null;
            baseDamagePerTurn = 7;
            damagePerLevel = 2;
            damagePerRed = 2.5f;
            baseDurationTurns = 3;
            durationPerLevel = 1;
            extraTurnsPerRed = 0.5f;
            startWidthNormalized = 0.35f;
            widthBonusPerLevel = 0.08f;
            fullSpanThreshold = 5;
            thresholdReductionPerLevel = 0;
        }
    }
}
