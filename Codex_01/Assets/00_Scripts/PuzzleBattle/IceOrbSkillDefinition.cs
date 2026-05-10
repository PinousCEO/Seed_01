using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Ice Orb", fileName = "IceOrbSkillDefinition")]
    public sealed class IceOrbSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 4;
        [SerializeField] private int damagePerLevel = 1;
        [SerializeField] private float slowMultiplier = 0.58f;
        [SerializeField] private float extraSlowPerLevel = 0.05f;
        [SerializeField] private int baseSlowTurns = 1;
        [SerializeField] private int slowTurnsPerLevel = 1;
        [SerializeField] private float projectileSpeed = 7.8f;
        [SerializeField] private float projectileSpeedPerLevel = 0.35f;
        [SerializeField] private float fallbackScale = 0.42f;

        public GameObject EffectPrefab => effectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public float GetSlowMultiplier(int level)
        {
            return Mathf.Clamp(slowMultiplier - (GetLevelOffset(level) * extraSlowPerLevel), 0.1f, 1f);
        }

        public int GetSlowTurns(int level)
        {
            return Mathf.Max(1, baseSlowTurns + (GetLevelOffset(level) * slowTurnsPerLevel));
        }

        public float GetProjectileSpeed(int level)
        {
            return Mathf.Max(0.5f, projectileSpeed + (GetLevelOffset(level) * projectileSpeedPerLevel));
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
                PuzzleBattleSkillId.IceOrb,
                "빙결 구체",
                "빙결",
                "파란 구슬이 터지면 얼음 구체를 발사해 피해를 주고 이동 속도를 감소시킵니다.",
                new Color(0.54f, 0.9f, 1f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 4;
            damagePerLevel = 1;
            slowMultiplier = 0.58f;
            extraSlowPerLevel = 0.05f;
            baseSlowTurns = 1;
            slowTurnsPerLevel = 1;
            projectileSpeed = 7.8f;
            projectileSpeedPerLevel = 0.35f;
            fallbackScale = 0.42f;
        }
    }
}
