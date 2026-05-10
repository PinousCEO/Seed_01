using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Monster Wave Profile", fileName = "MonsterWaveProfile")]
    public sealed class MonsterWaveProfile : ScriptableObject
    {
        [SerializeField] private int laneCount = 7;
        [SerializeField] private int maxConcurrentMonsters = 6;
        [SerializeField] private float spawnInterval = 2.25f;
        [SerializeField] private float fallSpeed = 0.55f;
        [SerializeField] private float fallSpeedVariance = 0.12f;
        [SerializeField] private int baseHealth = 16;
        [SerializeField] private int healthVariance = 3;
        [SerializeField] private float roundDuration = 60f;
        [SerializeField] private int roundHealthIncrease = 6;
        [SerializeField] private float roundSpeedIncrease = 0.05f;
        [SerializeField] private float spawnIntervalDecayPerRound = 0.08f;
        [SerializeField] private int maxConcurrentIncreasePerRound = 1;
        [SerializeField] private int battlefieldRows = 7;
        [SerializeField] private int spawnTurnsPerRound = 3;
        [SerializeField] private int spawnTurnsGrowthPerRound = 1;
        [SerializeField] private int spawnsPerTurn = 2;
        [SerializeField] private float lanePadding = 0f;
        [SerializeField] private float monsterWidth = 1.65f;
        [SerializeField] private float monsterHeight = 1.15f;
        [SerializeField] private float touchBoundaryPadding = 0.55f;
        [SerializeField] private float deathDuration = 0.2f;
        [SerializeField] private int baseCoinReward = 2;
        [SerializeField] private int coinVariance = 1;
        [SerializeField] private int coinRewardIncreasePerRound = 0;
        [SerializeField] private Color monsterTint = new Color(0.91f, 0.41f, 0.32f);

        public int LaneCount => Mathf.Max(1, laneCount);
        public int MaxConcurrentMonsters => Mathf.Max(1, maxConcurrentMonsters);
        public float SpawnInterval => Mathf.Max(0.3f, spawnInterval);
        public float FallSpeed => Mathf.Max(0.05f, fallSpeed);
        public float FallSpeedVariance => Mathf.Max(0f, fallSpeedVariance);
        public int BaseHealth => Mathf.Max(1, baseHealth);
        public int HealthVariance => Mathf.Max(0, healthVariance);
        public float RoundDuration => roundDuration <= 0f ? 60f : roundDuration;
        public int RoundHealthIncrease => Mathf.Max(0, roundHealthIncrease);
        public float RoundSpeedIncrease => Mathf.Max(0f, roundSpeedIncrease);
        public float SpawnIntervalDecayPerRound => Mathf.Clamp(spawnIntervalDecayPerRound, 0f, 0.85f);
        public int MaxConcurrentIncreasePerRound => Mathf.Max(0, maxConcurrentIncreasePerRound);
        public int BattlefieldRows => Mathf.Max(4, battlefieldRows);
        public int SpawnTurnsPerRound => Mathf.Max(1, spawnTurnsPerRound > 0 ? spawnTurnsPerRound : Mathf.RoundToInt(RoundDuration / 3f));
        public int SpawnTurnsGrowthPerRound => Mathf.Max(0, spawnTurnsGrowthPerRound);
        public int SpawnsPerTurn => Mathf.Max(1, spawnsPerTurn);
        public float LanePadding => Mathf.Max(0f, lanePadding);
        public float MonsterWidth => Mathf.Max(0.5f, monsterWidth);
        public float MonsterHeight => Mathf.Max(0.5f, monsterHeight);
        public float TouchBoundaryPadding => Mathf.Max(0f, touchBoundaryPadding);
        public float DeathDuration => Mathf.Max(0.01f, deathDuration);
        public int BaseCoinReward => Mathf.Max(1, baseCoinReward);
        public int CoinVariance => Mathf.Max(0, coinVariance);
        public int CoinRewardIncreasePerRound => Mathf.Max(0, coinRewardIncreasePerRound);
        public Color MonsterTint => monsterTint;

        public void SetAuthoringDefaults()
        {
            ApplyDefaults(HideFlags.None);
        }

        public void SetRuntimeDefaults()
        {
            ApplyDefaults(HideFlags.DontSave);
        }

        private void ApplyDefaults(HideFlags flags)
        {
            laneCount = 7;
            maxConcurrentMonsters = 6;
            spawnInterval = 2.25f;
            fallSpeed = 0.55f;
            fallSpeedVariance = 0.12f;
            baseHealth = 16;
            healthVariance = 3;
            roundDuration = 60f;
            roundHealthIncrease = 6;
            roundSpeedIncrease = 0.05f;
            spawnIntervalDecayPerRound = 0.08f;
            maxConcurrentIncreasePerRound = 1;
            battlefieldRows = 7;
            spawnTurnsPerRound = 3;
            spawnTurnsGrowthPerRound = 1;
            spawnsPerTurn = 2;
            lanePadding = 0f;
            monsterWidth = 1.65f;
            monsterHeight = 1.15f;
            touchBoundaryPadding = 0.55f;
            deathDuration = 0.2f;
            baseCoinReward = 2;
            coinVariance = 1;
            coinRewardIncreasePerRound = 0;
            monsterTint = new Color(0.91f, 0.41f, 0.32f);
            hideFlags = flags;
        }
    }
}
