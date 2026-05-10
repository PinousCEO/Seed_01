using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Charming Heart", fileName = "CharmingHeartSkillDefinition")]
    public sealed class CharmingHeartSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject charmEffectPrefab;
        [SerializeField] private int heartsPerCharm = 2;
        [SerializeField] private int baseCharmTurns = 1;
        [SerializeField] private int charmTurnsPerLevel = 1;
        [SerializeField] private int bonusTargetsPerLevel = 1;
        [SerializeField] private int fullCharmThreshold = 5;
        [SerializeField] private int fullCharmThresholdReductionPerLevel = 0;
        [SerializeField] private float fallbackScale = 0.72f;

        public GameObject CharmEffectPrefab => charmEffectPrefab;
        public int HeartsPerCharm => Mathf.Max(1, heartsPerCharm);
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);

        public int GetCharmTurns(int level)
        {
            return Mathf.Max(1, baseCharmTurns + (GetLevelOffset(level) * charmTurnsPerLevel));
        }

        public int GetBonusTargets(int level)
        {
            return Mathf.Max(0, GetLevelOffset(level) * bonusTargetsPerLevel);
        }

        public int GetFullCharmThreshold(int level)
        {
            return Mathf.Max(1, fullCharmThreshold - (GetLevelOffset(level) * fullCharmThresholdReductionPerLevel));
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
            charmEffectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.CharmingHeart,
                "매혹의 하트",
                "매혹",
                "분홍 구슬이 터지면 적을 매혹해 움직임을 막고 뒤의 적도 함께 가로막습니다.",
                new Color(1f, 0.45f, 0.72f, 1f),
                flags);
            charmEffectPrefab = null;
            heartsPerCharm = 2;
            baseCharmTurns = 1;
            charmTurnsPerLevel = 1;
            bonusTargetsPerLevel = 1;
            fullCharmThreshold = 5;
            fullCharmThresholdReductionPerLevel = 0;
            fallbackScale = 0.72f;
        }
    }
}
