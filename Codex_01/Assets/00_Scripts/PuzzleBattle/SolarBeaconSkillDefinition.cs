using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Solar Beacon", fileName = "SolarBeaconSkillDefinition")]
    public sealed class SolarBeaconSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 16;
        [SerializeField] private int damagePerLevel = 5;
        [SerializeField] private float baseRadiusCells = 0.55f;
        [SerializeField] private float radiusPerMatchedOrb = 0.24f;
        [SerializeField] private float radiusPerLevel = 0.05f;
        [SerializeField] private int delayTurns = 1;
        [SerializeField] private float fallbackScale = 0.9f;
        [SerializeField] private float effectLifetime = 0.45f;

        public GameObject EffectPrefab => effectPrefab;
        public int DelayTurns => Mathf.Max(1, delayTurns);
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public float GetRadius(int level, int matchedOrbCount)
        {
            return Mathf.Max(0.35f, baseRadiusCells + (matchedOrbCount * radiusPerMatchedOrb) + (GetLevelOffset(level) * radiusPerLevel));
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
                PuzzleBattleSkillId.SolarBeacon,
                "태양 표식",
                "표식",
                "노란 구슬이 터지면 지역을 비추고 1턴 뒤 강력한 범위 공격을 가합니다. 범위는 매치 크기에 따라 커집니다.",
                new Color(1f, 0.9f, 0.42f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 16;
            damagePerLevel = 5;
            baseRadiusCells = 0.55f;
            radiusPerMatchedOrb = 0.24f;
            radiusPerLevel = 0.05f;
            delayTurns = 1;
            fallbackScale = 0.9f;
            effectLifetime = 0.45f;
        }
    }
}
