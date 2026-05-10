using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Lightning Strike", fileName = "LightningStrikeSkillDefinition")]
    public sealed class LightningStrikeSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject lightningEffectPrefab;
        [SerializeField] private int baseDamage = 8;
        [SerializeField] private int damagePerLevel = 3;
        [SerializeField] private int bonusTargetsPerLevel = 1;
        [SerializeField] private float fallbackScale = 0.82f;
        [SerializeField] private float effectLifetime = 0.35f;

        public GameObject LightningEffectPrefab => lightningEffectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public int GetBonusTargets(int level)
        {
            return Mathf.Max(0, GetLevelOffset(level) * bonusTargetsPerLevel);
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
            lightningEffectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.LightningStrike,
                "낙뢰",
                "번개",
                "노란 구슬이 터지면 랜덤한 적 위치에 번개를 떨어뜨립니다.",
                new Color(1f, 0.88f, 0.26f, 1f),
                flags);
            lightningEffectPrefab = null;
            baseDamage = 8;
            damagePerLevel = 3;
            bonusTargetsPerLevel = 1;
            fallbackScale = 0.82f;
            effectLifetime = 0.35f;
        }
    }
}
