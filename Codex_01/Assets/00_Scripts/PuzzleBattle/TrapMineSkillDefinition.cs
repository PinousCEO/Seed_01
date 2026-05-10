using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Trap Mine", fileName = "TrapMineSkillDefinition")]
    public sealed class TrapMineSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private int damagePerLevel = 3;
        [SerializeField] private float baseRadiusCells = 0.8f;
        [SerializeField] private float radiusPerLevel = 0.05f;
        [SerializeField] private float empoweredDamageMultiplier = 1.8f;
        [SerializeField] private float empoweredRadiusBonus = 0.5f;
        [SerializeField] private float fallbackScale = 0.65f;
        [SerializeField] private float effectLifetime = 0.4f;

        public GameObject EffectPrefab => effectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level, bool empowered)
        {
            int value = Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
            return empowered ? Mathf.Max(1, Mathf.RoundToInt(value * empoweredDamageMultiplier)) : value;
        }

        public float GetRadius(int level, bool empowered)
        {
            float value = Mathf.Max(0.3f, baseRadiusCells + (GetLevelOffset(level) * radiusPerLevel));
            return empowered ? value + empoweredRadiusBonus : value;
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
                PuzzleBattleSkillId.TrapMine,
                "폭발 함정",
                "함정",
                "붉은 구슬이 터지면 적 경로에 함정을 설치합니다. 5개 매치면 더 강한 함정이 설치됩니다.",
                new Color(1f, 0.48f, 0.2f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 10;
            damagePerLevel = 3;
            baseRadiusCells = 0.8f;
            radiusPerLevel = 0.05f;
            empoweredDamageMultiplier = 1.8f;
            empoweredRadiusBonus = 0.5f;
            fallbackScale = 0.65f;
            effectLifetime = 0.4f;
        }
    }
}
