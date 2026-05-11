using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Death Beam", fileName = "DeathBeamSkillDefinition")]
    public sealed class DeathBeamSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private int damagePerLevel = 4;
        [SerializeField] private float fallbackScale = 0.88f;
        [SerializeField] private float effectLifetime = 0.28f;

        public GameObject EffectPrefab => effectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
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
                PuzzleBattleSkillId.DeathBeam,
                "데스 빔",
                "빔",
                "몬스터가 죽으면 사망한 가로열 전체에 빔을 쏴 모든 적에게 피해를 줍니다.",
                new Color(1f, 0.42f, 0.2f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 10;
            damagePerLevel = 4;
            fallbackScale = 0.88f;
            effectLifetime = 0.28f;
        }
    }
}
