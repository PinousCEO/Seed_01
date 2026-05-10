using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Earthquake", fileName = "EarthquakeSkillDefinition")]
    public sealed class EarthquakeSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 9;
        [SerializeField] private int damagePerLevel = 3;
        [SerializeField] private float damagePerWood = 1.75f;
        [SerializeField] private float baseRadiusCells = 1.1f;
        [SerializeField] private float radiusPerLevel = 0.08f;
        [SerializeField] private float radiusPerWood = 0.12f;
        [SerializeField] private float fallbackScale = 1f;
        [SerializeField] private float effectLifetime = 0.4f;

        public GameObject EffectPrefab => effectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level, int woodCount)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel) + Mathf.RoundToInt(woodCount * damagePerWood));
        }

        public float GetRadius(int level, int woodCount)
        {
            return Mathf.Max(0.25f, baseRadiusCells + (GetLevelOffset(level) * radiusPerLevel) + (woodCount * radiusPerWood));
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
                PuzzleBattleSkillId.Earthquake,
                "지진",
                "지진",
                "나무 구슬이 터지면 적 필드에 지진을 일으켜 원형 범위 피해를 줍니다.",
                new Color(0.48f, 0.82f, 0.38f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 9;
            damagePerLevel = 3;
            damagePerWood = 1.75f;
            baseRadiusCells = 1.1f;
            radiusPerLevel = 0.08f;
            radiusPerWood = 0.12f;
            fallbackScale = 1f;
            effectLifetime = 0.4f;
        }
    }
}
