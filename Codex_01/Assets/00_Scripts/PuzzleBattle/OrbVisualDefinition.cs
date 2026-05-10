using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Orb Visual Definition", fileName = "OrbVisualDefinition")]
    public sealed class OrbVisualDefinition : ScriptableObject
    {
        [SerializeField] private string orbId = "fire";
        [SerializeField] private Color tint = Color.red;
        [SerializeField] private Sprite spriteOverride;
        [SerializeField] private GameObject projectileEffectPrefab;
        [SerializeField] private int damagePerOrb = 10;

        public string OrbId => string.IsNullOrWhiteSpace(orbId) ? name : orbId;
        public Color Tint => tint;
        public Sprite SpriteOverride => spriteOverride;
        public GameObject ProjectileEffectPrefab => projectileEffectPrefab;
        public int DamagePerOrb => Mathf.Max(1, damagePerOrb);

        public void SetProjectileEffectPrefab(GameObject prefab)
        {
            projectileEffectPrefab = prefab;
        }

        public void SetAuthoringDefaults(string id, Color color, int damage)
        {
            ApplyDefaults(id, color, damage, HideFlags.None);
        }

        public void SetRuntimeDefaults(string id, Color color, int damage)
        {
            ApplyDefaults(id, color, damage, HideFlags.DontSave);
        }

        private void ApplyDefaults(string id, Color color, int damage, HideFlags flags)
        {
            orbId = id;
            tint = color;
            spriteOverride = null;
            projectileEffectPrefab = null;
            damagePerOrb = damage;
            hideFlags = flags;
        }
    }
}
