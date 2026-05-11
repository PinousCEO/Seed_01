using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Player Status Profile", fileName = "PlayerStatusProfile")]
    public sealed class PlayerStatusProfile : ScriptableObject
    {
        [SerializeField] private int maxHealth = 500;
        [SerializeField] private int attackBonus = 8;
        [SerializeField] private int healPickupAmount = 20;

        public int MaxHealth => Mathf.Max(1, maxHealth);
        public int AttackBonus => Mathf.Max(0, attackBonus);
        public int HealPickupAmount => Mathf.Max(1, healPickupAmount);

        public void SetRuntimeDefaults()
        {
            maxHealth = 500;
            attackBonus = 8;
            healPickupAmount = 20;
            hideFlags = HideFlags.DontSave;
        }
    }
}
