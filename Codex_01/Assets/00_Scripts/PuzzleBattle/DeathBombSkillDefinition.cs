using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Death Bomb", fileName = "DeathBombSkillDefinition")]
    public sealed class DeathBombSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 9;
        [SerializeField] private int damagePerLevel = 3;
        [SerializeField] private float baseRadiusCells = 1.15f;
        [SerializeField] private float radiusPerLevel = 0.2f;
        [SerializeField] private float fallbackScale = 0.95f;
        [SerializeField] private float effectLifetime = 0.32f;

        public GameObject EffectPrefab => effectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public float GetRadius(int level)
        {
            return Mathf.Max(0.35f, baseRadiusCells + (GetLevelOffset(level) * radiusPerLevel));
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
            effectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.DeathBomb,
                "사망 폭탄",
                "폭탄",
                "몬스터가 죽으면 주변 적에게 폭발 피해를 줍니다.",
                new Color(1f, 0.78f, 0.2f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 9;
            damagePerLevel = 3;
            baseRadiusCells = 1.15f;
            radiusPerLevel = 0.2f;
            fallbackScale = 0.95f;
            effectLifetime = 0.32f;
        }
    }
}
