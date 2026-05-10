using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Skills/Orb Volley", fileName = "OrbVolleySkillDefinition")]
    public sealed class OrbVolleySkillDefinition : PuzzleBattleSkillDefinition
    {
        [SerializeField] private GameObject projectileEffectPrefab;
        [SerializeField] private float damageMultiplier = 0.45f;
        [SerializeField] private float damageMultiplierPerLevel = 0.08f;
        [SerializeField] private float projectileSpeed = 7.5f;
        [SerializeField] private float projectileSpeedPerLevel = 0.35f;
        [SerializeField] private float fallbackScale = 0.42f;

        public GameObject ProjectileEffectPrefab => projectileEffectPrefab;
        public float FallbackScale => Mathf.Max(0.05f, fallbackScale);

        public float GetDamageMultiplier(int level)
        {
            return Mathf.Max(0.05f, damageMultiplier + (GetLevelOffset(level) * damageMultiplierPerLevel));
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
            projectileEffectPrefab = prefab;
        }

        private void ApplyDefaults(HideFlags flags)
        {
            SetCommonDefaults(
                PuzzleBattleSkillId.OrbVolley,
                "속성 구체",
                "구체",
                "터진 구슬의 속성에 맞는 구체를 발사해 전방의 적을 공격합니다.",
                new Color(1f, 0.63f, 0.24f, 1f),
                flags);
            projectileEffectPrefab = null;
            damageMultiplier = 0.45f;
            damageMultiplierPerLevel = 0.08f;
            projectileSpeed = 7.5f;
            projectileSpeedPerLevel = 0.35f;
            fallbackScale = 0.42f;
        }
    }
}
