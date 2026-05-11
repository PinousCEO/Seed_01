using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Monster Wave Profile", fileName = "MonsterWaveProfile")]
    public sealed class MonsterWaveProfile : ScriptableObject
    {
        [SerializeField] private int laneCount = 6;
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
        [SerializeField] private int playerMaxHealth = 500;
        [SerializeField] private int escapeDamage = 4;
        [SerializeField] private int escapeDamageIncreasePerRound = 1;
        [SerializeField] private float rangedMonsterChance = 0.1f;
        [SerializeField] private int rangedAttackDamage = 20;
        [SerializeField] private int rangedAttackDamageIncreasePerRound = 1;
        [SerializeField] private float rangedProjectileSpeed = 8.5f;
        [SerializeField] private Color rangedMonsterTint = new Color(0.42f, 0.72f, 0.96f, 1f);
        [SerializeField] private int finalRound = 20;
        [SerializeField] private int bossHealth = 220;
        [SerializeField] private int bossContactWidthCells = 2;
        [SerializeField] private int bossContactHeightCells = 2;
        [SerializeField] private float bossSummonChancePerTurn = 0.65f;
        [SerializeField] private int bossMinSummonCount = 1;
        [SerializeField] private int bossMaxSummonCount = 2;
        [SerializeField] private Color bossTint = new Color(0.7f, 0.24f, 0.95f, 1f);

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
        public int PlayerMaxHealth => Mathf.Max(1, playerMaxHealth);
        public int EscapeDamage => Mathf.Max(1, escapeDamage);
        public int EscapeDamageIncreasePerRound => Mathf.Max(0, escapeDamageIncreasePerRound);
        public float RangedMonsterChance => Mathf.Clamp(rangedMonsterChance <= 0f ? 0.1f : rangedMonsterChance, 0f, 1f);
        public int RangedAttackDamage => Mathf.Max(1, rangedAttackDamage <= 0 ? 20 : rangedAttackDamage);
        public int RangedAttackDamageIncreasePerRound => Mathf.Max(0, rangedAttackDamageIncreasePerRound);
        public float RangedProjectileSpeed => Mathf.Max(0.5f, rangedProjectileSpeed <= 0f ? 8.5f : rangedProjectileSpeed);
        public Color RangedMonsterTint => rangedMonsterTint.a <= 0f ? new Color(0.42f, 0.72f, 0.96f, 1f) : rangedMonsterTint;
        public int FinalRound => Mathf.Max(1, finalRound <= 0 ? 20 : finalRound);
        public int BossHealth => Mathf.Max(1, bossHealth <= 0 ? 220 : bossHealth);
        public int BossWidthCells => Mathf.Max(2, bossContactWidthCells <= 0 ? 2 : bossContactWidthCells);
        public int BossHeightCells => Mathf.Max(2, bossContactHeightCells <= 0 ? 2 : bossContactHeightCells);
        public float BossSummonChancePerTurn => Mathf.Clamp01(bossSummonChancePerTurn <= 0f ? 0.65f : bossSummonChancePerTurn);
        public int BossMinSummonCount => Mathf.Max(1, bossMinSummonCount <= 0 ? 1 : bossMinSummonCount);
        public int BossMaxSummonCount => Mathf.Max(BossMinSummonCount, bossMaxSummonCount <= 0 ? 2 : bossMaxSummonCount);
        public Color BossTint => bossTint.a <= 0f ? new Color(0.7f, 0.24f, 0.95f, 1f) : bossTint;

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
            laneCount = 6;
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
            playerMaxHealth = 500;
            escapeDamage = 4;
            escapeDamageIncreasePerRound = 1;
            rangedMonsterChance = 0.1f;
            rangedAttackDamage = 20;
            rangedAttackDamageIncreasePerRound = 1;
            rangedProjectileSpeed = 8.5f;
            rangedMonsterTint = new Color(0.42f, 0.72f, 0.96f, 1f);
            finalRound = 20;
            bossHealth = 220;
            bossContactWidthCells = 2;
            bossContactHeightCells = 2;
            bossSummonChancePerTurn = 0.65f;
            bossMinSummonCount = 1;
            bossMaxSummonCount = 2;
            bossTint = new Color(0.7f, 0.24f, 0.95f, 1f);
            hideFlags = flags;
        }
    }
}
