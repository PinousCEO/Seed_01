using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Bat Swarm", fileName = "BatSwarmSkillDefinition")]
    public sealed class BatSwarmSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject batEffectPrefab;
        [SerializeField] private int baseDamage = 5;
        [SerializeField] private int damagePerLevel = 2;
        [SerializeField] private int dotDamagePerTurn = 3;
        [SerializeField] private int dotDamagePerLevel = 1;
        [SerializeField] private int dotDurationTurns = 2;
        [SerializeField] private int dotDurationPerLevel = 1;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileSpeedPerLevel = 0.35f;
        [SerializeField] private float fallbackScale = 0.42f;

        public GameObject BatEffectPrefab => batEffectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public int GetDotDamage(int level)
        {
            return Mathf.Max(1, dotDamagePerTurn + (GetLevelOffset(level) * dotDamagePerLevel));
        }

        public int GetDotTurns(int level)
        {
            return Mathf.Max(1, dotDurationTurns + (GetLevelOffset(level) * dotDurationPerLevel));
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
            batEffectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.BatSwarm,
                "박쥐 떼",
                "박쥐",
                "검은 구슬이 터지면 박쥐를 소환해 적을 공격하고 지속 피해를 남깁니다.",
                new Color(0.38f, 0.26f, 0.56f, 1f),
                flags);
            batEffectPrefab = null;
            baseDamage = 5;
            damagePerLevel = 2;
            dotDamagePerTurn = 3;
            dotDamagePerLevel = 1;
            dotDurationTurns = 2;
            dotDurationPerLevel = 1;
            projectileSpeed = 8f;
            projectileSpeedPerLevel = 0.35f;
            fallbackScale = 0.42f;
        }
    }
}
