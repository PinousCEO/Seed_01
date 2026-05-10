using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Poison Needles", fileName = "PoisonNeedleSkillDefinition")]
    public sealed class PoisonNeedleSkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int baseDamage = 2;
        [SerializeField] private int damagePerLevel = 1;
        [SerializeField] private int dotDamagePerTurn = 2;
        [SerializeField] private int dotDamagePerLevel = 1;
        [SerializeField] private int dotTurns = 3;
        [SerializeField] private float projectileSpeed = 8.5f;
        [SerializeField] private float projectileSpeedPerLevel = 0.4f;
        [SerializeField] private float fallbackScale = 0.38f;

        public GameObject EffectPrefab => effectPrefab;
        public int DotTurns => Mathf.Max(1, dotTurns);
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);

        public int GetDamage(int level)
        {
            return Mathf.Max(1, baseDamage + (GetLevelOffset(level) * damagePerLevel));
        }

        public int GetDotDamage(int level)
        {
            return Mathf.Max(1, dotDamagePerTurn + (GetLevelOffset(level) * dotDamagePerLevel));
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
                PuzzleBattleSkillId.PoisonNeedles,
                "독침",
                "독침",
                "파괴된 검은 구슬 수만큼 독침을 발사해 3턴 동안 지속 피해를 입힙니다.",
                new Color(0.42f, 0.9f, 0.42f, 1f),
                flags);
            effectPrefab = null;
            baseDamage = 2;
            damagePerLevel = 1;
            dotDamagePerTurn = 2;
            dotDamagePerLevel = 1;
            dotTurns = 3;
            projectileSpeed = 8.5f;
            projectileSpeedPerLevel = 0.4f;
            fallbackScale = 0.38f;
        }
    }
}
