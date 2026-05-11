using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Chain Lightning", fileName = "ChainLightningSkillDefinition")]
    public sealed class ChainLightningSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject lightningEffectPrefab;
        [SerializeField] private int baseDamage = 8;
        [SerializeField] private int damagePerLevel = 3;
        [SerializeField] private int baseChainCount = 1;
        [SerializeField] private int chainCountPerLevel = 1;
        [SerializeField] private float chainDamageFalloff = 0.72f;
        [SerializeField] private float chainSearchRadiusCells = 2.6f;
        [SerializeField] private float projectileSpeed = 11.5f;
        [SerializeField] private float fallbackScale = 0.82f;
        [SerializeField] private float effectLifetime = 0.35f;

        public GameObject LightningEffectPrefab => lightningEffectPrefab;
        public float ChainDamageFalloff => Mathf.Clamp(chainDamageFalloff, 0.2f, 0.95f);
        public float ChainSearchRadiusCells => Mathf.Max(0.5f, chainSearchRadiusCells);
        public float ProjectileSpeed => Mathf.Max(0.5f, projectileSpeed);
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public int GetChainCount(int level)
        {
            return Mathf.Max(0, baseChainCount + (GetLevelOffset(level) * chainCountPerLevel));
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
                PuzzleBattleSkillId.ChainLightning,
                "연쇄 번개",
                "전이",
                "노란 구슬이 터지면 번개 구체를 발사하고, 적에게 맞으면 주변 적에게 전이 피해를 줍니다.",
                new Color(1f, 0.94f, 0.42f, 1f),
                flags);
            lightningEffectPrefab = null;
            baseDamage = 8;
            damagePerLevel = 3;
            baseChainCount = 1;
            chainCountPerLevel = 1;
            chainDamageFalloff = 0.72f;
            chainSearchRadiusCells = 2.6f;
            projectileSpeed = 11.5f;
            fallbackScale = 0.82f;
            effectLifetime = 0.35f;
        }
    }
}
