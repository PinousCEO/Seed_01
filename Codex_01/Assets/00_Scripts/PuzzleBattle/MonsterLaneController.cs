using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleBattle
{
    public sealed class MonsterLaneController : MonoBehaviour
    {
        private enum HazardType
        {
            FrostWell,
            FlameCurtain,
            DelayedBlastMarker,
            TrapMine
        }

        private sealed class MonsterInstance
        {
            public MonsterView View;
            public int Lane;
            public int Row;
            public int WidthCells;
            public int HeightCells;
            public bool IsBoss;
            public bool IsRanged;
            public int RangedDamage;
            public int CoinReward;
            public float StepCharge;
            public bool IsDefeated;
            public int DotDamagePerTurn;
            public int DotTurnsRemaining;
            public int CharmTurnsRemaining;
            public float SlowMultiplier;
            public int SlowTurnsRemaining;
        }

        private sealed class HazardInstance
        {
            public HazardType Type;
            public Transform VisualRoot;
            public SpriteRenderer FallbackRenderer;
            public Vector3 VisualBaseScale;
            public float CenterLane;
            public int Row;
            public float RadiusCells;
            public float WidthCells;
            public int DamagePerTurn;
            public int BurstDamage;
            public float SlowMultiplier;
            public int RemainingTurns;
            public bool TickDownEachTurn;
            public bool TriggerOnContact;
            public float FallbackScale;
            public float EffectLifetime;
            public Color Tint;
            public GameObject EffectPrefab;
        }

        private sealed class ProjectileInstance
        {
            public Transform VisualRoot;
            public SpriteRenderer FallbackRenderer;
            public Vector3 VisualBaseScale;
            public MonsterInstance Target;
            public Vector3 Position;
            public float Speed;
            public int Damage;
            public int DotDamagePerTurn;
            public int DotTurns;
            public bool AdditiveDot;
            public float SlowMultiplier;
            public int SlowTurns;
            public int RetargetFrontCount;
            public Color Tint;
            public Vector3 CurveStart;
            public Vector3 CurveControl;
            public Vector3 CachedTargetPosition;
            public float TravelProgress;
            public float TravelDuration;
            public float CurveSide;
            public float CurveLift;
            public int ChainRemaining;
            public float ChainDamageFalloff;
            public float ChainSearchRadiusCells;
            public GameObject ImpactEffectPrefab;
            public float ImpactFallbackScale;
            public float ImpactEffectLifetime;
            public List<MonsterInstance> HitHistory;
        }

        private sealed class EnemyProjectileInstance
        {
            public Transform VisualRoot;
            public Vector3 Position;
            public Vector3 CurveStart;
            public Vector3 CurveControl;
            public Vector3 TargetPosition;
            public float TravelProgress;
            public float TravelDuration;
            public int Damage;
        }

        public event System.Action MonsterReachedPlayer;
        public event System.Action WaveCompleted;
        public event System.Action<Vector3, int, bool> MonsterDefeated;
        public event System.Action<int, Vector3> PlayerDamaged;

        private readonly List<MonsterInstance> _monsters = new List<MonsterInstance>();
        private readonly List<HazardInstance> _hazards = new List<HazardInstance>();
        private readonly List<ProjectileInstance> _projectiles = new List<ProjectileInstance>();
        private readonly List<EnemyProjectileInstance> _enemyProjectiles = new List<EnemyProjectileInstance>();
        private readonly List<SpriteRenderer> _gridLines = new List<SpriteRenderer>();
        private SimplePool<MonsterView> _monsterViewPool;
        private MonsterWaveProfile _profile;
        private Rect _region;
        private SpriteRenderer _backdrop;
        private SpriteRenderer _frame;
        private float _cellWidth;
        private float _cellHeight;
        private Vector2 _origin;
        private int _spawnSequence;
        private int _currentRound = 1;
        private int _spawnTurnsRemaining;
        private bool _battleActive;

        public int CurrentRound => _currentRound;
        public int ActiveMonsterCount => _monsters.Count;
        public int WaveTurnsRemaining => Mathf.Max(0, _spawnTurnsRemaining);
        public int RemainingMonsterCount => ActiveMonsterCount + (WaveTurnsRemaining * GetSpawnsPerTurnForCurrentRound());
        public bool IsBattleActive => _battleActive;
        public int ColumnCount => Mathf.Max(1, _profile != null ? _profile.LaneCount : 0);
        public int ActiveProjectileCount => _projectiles.Count + _enemyProjectiles.Count;
        public bool HasLivingBoss
        {
            get
            {
                for (int i = 0; i < _monsters.Count; i++)
                {
                    if (!_monsters[i].IsDefeated && _monsters[i].IsBoss)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private int Columns => Mathf.Max(1, _profile != null ? _profile.LaneCount : 0);
        private int Rows => _profile != null ? _profile.BattlefieldRows : 0;
        private float CellSize => Mathf.Min(_cellWidth, _cellHeight);

        public void Configure(MonsterWaveProfile profile, Rect region)
        {
            _profile = profile;
            _region = region;
            EnsureBattlefieldVisuals();
            UpdateLayout();
            RepositionMonsters();

            for (int i = 0; i < _hazards.Count; i++)
            {
                UpdateHazardVisual(_hazards[i]);
            }
        }

        public void StartRound(int round)
        {
            _currentRound = Mathf.Max(1, round);
            _battleActive = true;
            ClearAllEffects();
            ClearLiveMonsters();

            if (_currentRound == _profile.FinalRound - 1)
            {
                _spawnTurnsRemaining = 0;
                _battleActive = false;
                WaveCompleted?.Invoke();
                return;
            }

            if (_currentRound >= _profile.FinalRound)
            {
                _spawnTurnsRemaining = 0;
                SpawnBossMonster();
                return;
            }

            _spawnTurnsRemaining = GetSpawnTurnsForCurrentRound();

            if (_spawnTurnsRemaining > 0)
            {
                SpawnTurnMonsters();
            }
        }

        public void SetBattleActive(bool active)
        {
            _battleActive = active;
        }

        public void ClearTransientEffects()
        {
            ClearAllEffects();
        }

        public void AdvanceTurn()
        {
            if (!_battleActive || _profile == null)
            {
                return;
            }

            ApplyOngoingEffectsForCurrentTurn();

            if (!_battleActive)
            {
                return;
            }

            AdvanceMonstersTowardPlayer();

            if (!_battleActive)
            {
                return;
            }

            TriggerContactHazards();
            TickHazards();

            ProcessBossTurn();
            FireRangedMonsterAttacks();

            if (_spawnTurnsRemaining > 0)
            {
                SpawnTurnMonsters();
            }

            if (_spawnTurnsRemaining == 0 && _monsters.Count == 0)
            {
                _battleActive = false;
                WaveCompleted?.Invoke();
            }
        }

        public bool ApplyDamage(int damage, Color tint)
        {
            return ApplyDamageToMonster(GetFrontMostMonster(), damage, tint);
        }

        public bool SpawnOrbProjectile(OrbVisualDefinition definition, GameObject defaultEffectPrefab, Color tint, int damage, Vector3 origin, float speed, float fallbackScale)
        {
            GameObject effectPrefab = definition != null && definition.ProjectileEffectPrefab != null
                ? definition.ProjectileEffectPrefab
                : defaultEffectPrefab;
            Vector3 targetScale = Vector3.one * CellSize * Mathf.Max(0.05f, fallbackScale);
            return SpawnTargetedProjectile(
                "SkillProjectile",
                effectPrefab,
                ProceduralSpriteLibrary.GetOrbSprite(),
                tint,
                damage,
                0,
                0,
                false,
                1f,
                0,
                origin,
                speed,
                targetScale,
                95,
                3);
        }

        public bool SpawnBatProjectile(GameObject effectPrefab, Color tint, int damage, int dotDamagePerTurn, int dotTurns, Vector3 origin, float speed, float fallbackScale)
        {
            Vector3 targetScale = Vector3.one * CellSize * Mathf.Max(0.05f, fallbackScale);
            return SpawnTargetedProjectile(
                "BatProjectile",
                effectPrefab,
                ProceduralSpriteLibrary.GetOrbSprite(),
                tint,
                damage,
                dotDamagePerTurn,
                dotTurns,
                false,
                1f,
                0,
                origin,
                speed,
                targetScale,
                96,
                4);
        }

        public bool SpawnPoisonNeedleProjectile(GameObject effectPrefab, Color tint, int damage, int dotDamagePerTurn, int dotTurns, Vector3 origin, float speed, float fallbackScale)
        {
            float scale = Mathf.Max(0.05f, fallbackScale);
            Vector3 targetScale = new Vector3(CellSize * scale * 0.42f, CellSize * scale * 1.35f, 1f);
            return SpawnTargetedProjectile(
                "PoisonNeedle",
                effectPrefab,
                ProceduralSpriteLibrary.GetSquareSprite(),
                tint,
                damage,
                dotDamagePerTurn,
                dotTurns,
                true,
                1f,
                0,
                origin,
                speed,
                targetScale,
                97,
                5);
        }

        public bool SpawnIceProjectile(GameObject effectPrefab, Color tint, int damage, float slowMultiplier, int slowTurns, Vector3 origin, float speed, float fallbackScale)
        {
            Vector3 targetScale = Vector3.one * CellSize * Mathf.Max(0.05f, fallbackScale);
            return SpawnTargetedProjectile(
                "IceProjectile",
                effectPrefab,
                ProceduralSpriteLibrary.GetOrbSprite(),
                tint,
                damage,
                0,
                0,
                false,
                slowMultiplier,
                slowTurns,
                origin,
                speed,
                targetScale,
                97,
                4);
        }

        public bool SpawnLightningOrbProjectile(
            OrbVisualDefinition definition,
            GameObject impactEffectPrefab,
            Color tint,
            int damage,
            int chainCount,
            float chainDamageFalloff,
            float chainSearchRadiusCells,
            Vector3 origin,
            float speed,
            float fallbackScale,
            float effectLifetime)
        {
            GameObject projectileEffectPrefab = definition != null && definition.ProjectileEffectPrefab != null
                ? definition.ProjectileEffectPrefab
                : null;
            Vector3 targetScale = Vector3.one * CellSize * Mathf.Max(0.05f, fallbackScale);
            MonsterInstance target = GetRandomActiveMonster();

            if (target == null)
            {
                return false;
            }

            Transform visualRoot = CreateEffectVisual(
                "LightningOrb",
                transform,
                projectileEffectPrefab,
                92,
                ProceduralSpriteLibrary.GetOrbSprite(),
                tint,
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            visualRoot.position = origin;
            ApplyVisualScale(visualRoot, visualBaseScale, targetScale);

            ProjectileInstance projectile = new ProjectileInstance
            {
                VisualRoot = visualRoot,
                FallbackRenderer = fallbackRenderer,
                VisualBaseScale = visualBaseScale,
                Target = target,
                Position = origin,
                Speed = Mathf.Max(0.5f, speed),
                Damage = Mathf.Max(1, damage),
                DotDamagePerTurn = 0,
                DotTurns = 0,
                AdditiveDot = false,
                SlowMultiplier = 1f,
                SlowTurns = 0,
                RetargetFrontCount = 1,
                Tint = tint,
                CurveStart = origin,
                CachedTargetPosition = target.View.transform.position + (Vector3.up * 0.05f),
                TravelProgress = 0f,
                TravelDuration = 0f,
                CurveSide = Random.value < 0.5f ? -1f : 1f,
                CurveLift = Random.Range(CellSize * 0.12f, CellSize * 0.34f),
                ChainRemaining = Mathf.Max(0, chainCount),
                ChainDamageFalloff = Mathf.Clamp(chainDamageFalloff, 0.2f, 0.95f),
                ChainSearchRadiusCells = Mathf.Max(0.5f, chainSearchRadiusCells),
                ImpactEffectPrefab = impactEffectPrefab,
                ImpactFallbackScale = Mathf.Max(0.05f, fallbackScale),
                ImpactEffectLifetime = Mathf.Max(0.05f, effectLifetime),
                HitHistory = new List<MonsterInstance>()
            };

            _projectiles.Add(projectile);
            RebuildProjectileCurve(projectile, origin, projectile.CachedTargetPosition);
            return true;
        }

        public int StrikeRandomEnemies(int strikeCount, int damage, Color tint, GameObject effectPrefab, float fallbackScale, float effectLifetime)
        {
            List<MonsterInstance> targets = GetRandomActiveMonsters(strikeCount);
            int applied = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                MonsterInstance target = targets[i];

                if (target == null || target.View == null || target.IsDefeated)
                {
                    continue;
                }

                SpawnOneShotEffect(
                    "LightningStrike",
                    target.View.transform.position,
                    effectPrefab,
                    92,
                    ProceduralSpriteLibrary.GetSquareSprite(),
                    tint,
                    new Vector3(CellSize * 0.14f, CellSize * Mathf.Max(0.4f, fallbackScale), 1f),
                    effectLifetime);

                ApplyDamageToMonster(target, damage, tint);
                applied++;
            }

            return applied;
        }

        public int CharmRandomEnemies(int count, int charmTurns, GameObject effectPrefab, float fallbackScale)
        {
            List<MonsterInstance> targets = GetRandomActiveMonsters(count);
            int applied = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                if (ApplyCharmToMonster(targets[i], charmTurns, effectPrefab, fallbackScale))
                {
                    applied++;
                }
            }

            return applied;
        }

        public int CharmAllEnemies(int charmTurns, GameObject effectPrefab, float fallbackScale)
        {
            List<MonsterInstance> targets = GetActiveMonsters();
            int applied = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                if (ApplyCharmToMonster(targets[i], charmTurns, effectPrefab, fallbackScale))
                {
                    applied++;
                }
            }

            return applied;
        }

        public int ApplyRowDamageAtWorldPosition(Vector3 worldPosition, int damage, Color tint, GameObject effectPrefab, float fallbackScale, float effectLifetime)
        {
            if (_profile == null)
            {
                return 0;
            }

            int row = Mathf.Clamp(WorldToRow(worldPosition), 0, Mathf.Max(0, Rows - 1));
            float centerLane = Mathf.Max(0f, (Columns - 1) * 0.5f);
            Vector3 effectPosition = CellToWorld(centerLane, row);
            float width = Mathf.Max(CellSize, _cellWidth * Mathf.Max(1, Columns) * Mathf.Max(0.2f, fallbackScale));

            SpawnOneShotEffect(
                "DeathBeam",
                effectPosition,
                effectPrefab,
                89,
                ProceduralSpriteLibrary.GetSquareSprite(),
                tint,
                new Vector3(width, CellSize * Mathf.Max(0.16f, fallbackScale * 0.22f), 1f),
                effectLifetime);

            int hits = 0;

            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                MonsterInstance monster = _monsters[i];

                if (monster == null || monster.IsDefeated)
                {
                    continue;
                }

                int minRow = monster.Row;
                int maxRow = monster.Row + Mathf.Max(1, monster.HeightCells) - 1;

                if (row < minRow || row > maxRow)
                {
                    continue;
                }

                ApplyDamageToMonster(monster, damage, tint);
                hits++;
            }

            return hits;
        }

        public int ApplyAreaDamageAtWorldPosition(Vector3 worldPosition, float radiusCells, int damage, Color tint, GameObject effectPrefab, float fallbackScale, float effectLifetime)
        {
            if (_profile == null)
            {
                return 0;
            }

            float lane = WorldToLane(worldPosition);
            int row = Mathf.Clamp(WorldToRow(worldPosition), 0, Mathf.Max(0, Rows - 1));
            float diameter = CellSize * Mathf.Max(0.35f, radiusCells) * 2f * Mathf.Max(0.05f, fallbackScale);

            SpawnOneShotEffect(
                "DeathBomb",
                worldPosition,
                effectPrefab,
                90,
                ProceduralSpriteLibrary.GetSoftCircleSprite(),
                tint,
                Vector3.one * diameter,
                effectLifetime);

            return ApplyAreaDamage(lane, row, radiusCells, damage, tint);
        }

        public void SpawnFrostWell(float radiusCells, int damagePerTurn, float slowMultiplier, int durationTurns, GameObject effectPrefab)
        {
            if (_profile == null)
            {
                return;
            }

            TryGetEnemyAnchor(out float lane, out int row);
            Transform visualRoot = CreateEffectVisual(
                "FrostWell",
                transform,
                effectPrefab,
                18,
                ProceduralSpriteLibrary.GetSoftCircleSprite(),
                new Color(0.22f, 0.66f, 1f, 0.52f),
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            HazardInstance hazard = new HazardInstance
            {
                Type = HazardType.FrostWell,
                VisualRoot = visualRoot,
                FallbackRenderer = fallbackRenderer,
                VisualBaseScale = visualBaseScale,
                CenterLane = lane,
                Row = row,
                RadiusCells = Mathf.Max(0.25f, radiusCells),
                WidthCells = 0f,
                DamagePerTurn = Mathf.Max(1, damagePerTurn),
                BurstDamage = 0,
                SlowMultiplier = Mathf.Clamp(slowMultiplier, 0.15f, 1f),
                RemainingTurns = Mathf.Max(1, durationTurns),
                TickDownEachTurn = true,
                TriggerOnContact = false,
                FallbackScale = 1f,
                EffectLifetime = 0f,
                Tint = new Color(0.22f, 0.66f, 1f, 0.52f),
                EffectPrefab = effectPrefab
            };

            _hazards.Add(hazard);
            UpdateHazardVisual(hazard);
        }

        public void SpawnFlameCurtain(float widthCells, int damagePerTurn, int durationTurns, GameObject effectPrefab)
        {
            if (_profile == null)
            {
                return;
            }

            TryGetEnemyAnchor(out float lane, out int row);
            Transform visualRoot = CreateEffectVisual(
                "FlameCurtain",
                transform,
                effectPrefab,
                20,
                ProceduralSpriteLibrary.GetSquareSprite(),
                new Color(1f, 0.34f, 0.12f, 0.58f),
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            HazardInstance hazard = new HazardInstance
            {
                Type = HazardType.FlameCurtain,
                VisualRoot = visualRoot,
                FallbackRenderer = fallbackRenderer,
                VisualBaseScale = visualBaseScale,
                CenterLane = Mathf.Clamp(lane, 0f, Mathf.Max(0f, Columns - 1)),
                Row = row,
                RadiusCells = 0f,
                WidthCells = Mathf.Clamp(widthCells, 0.5f, Columns + 0.25f),
                DamagePerTurn = Mathf.Max(1, damagePerTurn),
                BurstDamage = 0,
                SlowMultiplier = 1f,
                RemainingTurns = Mathf.Max(1, durationTurns),
                TickDownEachTurn = true,
                TriggerOnContact = false,
                FallbackScale = 1f,
                EffectLifetime = 0f,
                Tint = new Color(1f, 0.34f, 0.12f, 0.58f),
                EffectPrefab = effectPrefab
            };

            _hazards.Add(hazard);
            UpdateHazardVisual(hazard);
        }

        public int TriggerEarthquake(float radiusCells, int damage, Color tint, GameObject effectPrefab, float fallbackScale, float effectLifetime)
        {
            if (_profile == null)
            {
                return 0;
            }

            TryGetEnemyAnchor(out float lane, out int row);
            Vector3 position = CellToWorld(lane, row);
            float diameter = CellSize * Mathf.Max(0.25f, radiusCells) * 2f * Mathf.Max(0.05f, fallbackScale);
            SpawnOneShotEffect(
                "Earthquake",
                position,
                effectPrefab,
                88,
                ProceduralSpriteLibrary.GetSoftCircleSprite(),
                tint,
                Vector3.one * diameter,
                effectLifetime);
            return ApplyAreaDamage(lane, row, radiusCells, damage, tint);
        }

        public bool PlaceSolarBeacon(float radiusCells, int damage, int delayTurns, Color tint, GameObject effectPrefab, float fallbackScale, float effectLifetime)
        {
            if (_profile == null)
            {
                return false;
            }

            TryGetEnemyAnchor(out float lane, out int row);
            Transform visualRoot = CreateEffectVisual(
                "SolarBeacon",
                transform,
                effectPrefab,
                22,
                ProceduralSpriteLibrary.GetSoftCircleSprite(),
                new Color(tint.r, tint.g, tint.b, 0.32f),
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            HazardInstance hazard = new HazardInstance
            {
                Type = HazardType.DelayedBlastMarker,
                VisualRoot = visualRoot,
                FallbackRenderer = fallbackRenderer,
                VisualBaseScale = visualBaseScale,
                CenterLane = lane,
                Row = row,
                RadiusCells = Mathf.Max(0.35f, radiusCells),
                WidthCells = 0f,
                DamagePerTurn = 0,
                BurstDamage = Mathf.Max(1, damage),
                SlowMultiplier = 1f,
                RemainingTurns = Mathf.Max(1, delayTurns),
                TickDownEachTurn = true,
                TriggerOnContact = false,
                FallbackScale = Mathf.Max(0.05f, fallbackScale),
                EffectLifetime = Mathf.Max(0.05f, effectLifetime),
                Tint = new Color(tint.r, tint.g, tint.b, 0.32f),
                EffectPrefab = effectPrefab
            };

            _hazards.Add(hazard);
            UpdateHazardVisual(hazard);
            return true;
        }

        public bool PlaceTrapMine(float radiusCells, int damage, bool empowered, Color tint, GameObject effectPrefab, float fallbackScale, float effectLifetime)
        {
            if (_profile == null || !TryGetTrapPlacement(out int lane, out int row))
            {
                return false;
            }

            if (HasTrapAt(lane, row))
            {
                return false;
            }

            Color visualTint = empowered
                ? new Color(Mathf.Min(1f, tint.r + 0.12f), Mathf.Min(1f, tint.g + 0.1f), Mathf.Min(1f, tint.b + 0.08f), 0.88f)
                : new Color(tint.r, tint.g, tint.b, 0.78f);

            Transform visualRoot = CreateEffectVisual(
                "TrapMine",
                transform,
                effectPrefab,
                24,
                ProceduralSpriteLibrary.GetSquareSprite(),
                visualTint,
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            HazardInstance hazard = new HazardInstance
            {
                Type = HazardType.TrapMine,
                VisualRoot = visualRoot,
                FallbackRenderer = fallbackRenderer,
                VisualBaseScale = visualBaseScale,
                CenterLane = lane,
                Row = row,
                RadiusCells = Mathf.Max(0.35f, radiusCells),
                WidthCells = 0f,
                DamagePerTurn = 0,
                BurstDamage = Mathf.Max(1, damage),
                SlowMultiplier = 1f,
                RemainingTurns = 0,
                TickDownEachTurn = false,
                TriggerOnContact = true,
                FallbackScale = Mathf.Max(0.05f, fallbackScale),
                EffectLifetime = Mathf.Max(0.05f, effectLifetime),
                Tint = visualTint,
                EffectPrefab = effectPrefab
            };

            _hazards.Add(hazard);
            UpdateHazardVisual(hazard);
            return true;
        }

        private void Update()
        {
            UpdateProjectiles();
            UpdateEnemyProjectiles();

            for (int i = 0; i < _hazards.Count; i++)
            {
                UpdateHazardVisual(_hazards[i]);
            }
        }

        private bool SpawnTargetedProjectile(
            string objectName,
            GameObject effectPrefab,
            Sprite fallbackSprite,
            Color tint,
            int damage,
            int dotDamagePerTurn,
            int dotTurns,
            bool additiveDot,
            float slowMultiplier,
            int slowTurns,
            Vector3 origin,
            float speed,
            Vector3 targetScale,
            int sortingOrder,
            int frontTargetCount)
        {
            MonsterInstance target = GetRandomFrontMonster(frontTargetCount);

            if (target == null)
            {
                return false;
            }

            Transform visualRoot = CreateEffectVisual(
                objectName,
                transform,
                effectPrefab,
                sortingOrder,
                fallbackSprite,
                tint,
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            visualRoot.position = origin;
            ApplyVisualScale(visualRoot, visualBaseScale, targetScale);

            _projectiles.Add(new ProjectileInstance
            {
                VisualRoot = visualRoot,
                FallbackRenderer = fallbackRenderer,
                VisualBaseScale = visualBaseScale,
                Target = target,
                Position = origin,
                Speed = Mathf.Max(0.5f, speed),
                Damage = Mathf.Max(1, damage),
                DotDamagePerTurn = Mathf.Max(0, dotDamagePerTurn),
                DotTurns = Mathf.Max(0, dotTurns),
                AdditiveDot = additiveDot,
                SlowMultiplier = Mathf.Clamp(slowMultiplier, 0.1f, 1f),
                SlowTurns = Mathf.Max(0, slowTurns),
                RetargetFrontCount = Mathf.Max(1, frontTargetCount),
                Tint = tint,
                CurveStart = origin,
                CachedTargetPosition = target.View.transform.position + (Vector3.up * 0.05f),
                TravelProgress = 0f,
                TravelDuration = 0f,
                CurveSide = Random.value < 0.5f ? -1f : 1f,
                CurveLift = Random.Range(CellSize * 0.12f, CellSize * 0.34f)
            });

            ProjectileInstance projectile = _projectiles[_projectiles.Count - 1];
            RebuildProjectileCurve(projectile, origin, projectile.CachedTargetPosition);

            return true;
        }

        private void ApplyOngoingEffectsForCurrentTurn()
        {
            ApplyDotDamage();

            if (!_battleActive)
            {
                return;
            }

            ApplyHazardDamageForCurrentCells();
        }

        private void ApplyDotDamage()
        {
            Color dotTint = new Color(0.42f, 0.9f, 0.42f, 1f);

            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                MonsterInstance monster = _monsters[i];

                if (monster == null || monster.IsDefeated || monster.DotTurnsRemaining <= 0)
                {
                    continue;
                }

                monster.DotTurnsRemaining--;

                if (monster.DotDamagePerTurn > 0)
                {
                    ApplyDamageToMonster(monster, monster.DotDamagePerTurn, dotTint);
                }

                if (monster.DotTurnsRemaining <= 0)
                {
                    monster.DotDamagePerTurn = 0;
                }
            }
        }

        private void ApplyHazardDamageForCurrentCells()
        {
            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                MonsterInstance monster = _monsters[i];

                if (monster == null || monster.IsDefeated)
                {
                    continue;
                }

                int damage = GetHazardDamage(monster, out Color tint);

                if (damage > 0)
                {
                    ApplyDamageToMonster(monster, damage, tint);
                }
            }
        }

        private void AdvanceMonstersTowardPlayer()
        {
            for (int lane = 0; lane < Columns; lane++)
            {
                List<MonsterInstance> laneMonsters = new List<MonsterInstance>();

                for (int i = 0; i < _monsters.Count; i++)
                {
                    MonsterInstance monster = _monsters[i];

                    if (!monster.IsDefeated && monster.Lane == lane)
                    {
                        laneMonsters.Add(monster);
                    }
                }

                laneMonsters.Sort((left, right) => left.Row.CompareTo(right.Row));
                int previousFinalRow = -1;

                for (int i = 0; i < laneMonsters.Count; i++)
                {
                    MonsterInstance monster = laneMonsters[i];
                    int stepCount = GetStepCountForTurn(monster);
                    int desiredRow = monster.Row - stepCount;

                    if (desiredRow < 0)
                    {
                        HandleMonsterEscape(monster);
                        continue;
                    }

                    int finalRow = desiredRow;

                    if (previousFinalRow >= 0)
                    {
                        finalRow = Mathf.Max(finalRow, previousFinalRow + 1);
                    }

                    monster.Row = Mathf.Min(finalRow, Rows - 1);
                    previousFinalRow = monster.Row;
                }
            }

            RepositionMonsters();
        }

        private int GetStepCountForTurn(MonsterInstance monster)
        {
            if (monster == null || monster.IsDefeated)
            {
                return 0;
            }

            if (monster.IsBoss)
            {
                return 0;
            }

            if (monster.CharmTurnsRemaining > 0)
            {
                monster.CharmTurnsRemaining--;
                return 0;
            }

            monster.StepCharge += GetStepAdvanceForTurn(monster);
            int stepCount = Mathf.FloorToInt(monster.StepCharge);
            monster.StepCharge -= stepCount;
            return stepCount;
        }

        private void TriggerContactHazards()
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                HazardInstance hazard = _hazards[i];

                if (hazard == null || !hazard.TriggerOnContact)
                {
                    continue;
                }

                bool triggered = false;

                for (int monsterIndex = 0; monsterIndex < _monsters.Count; monsterIndex++)
                {
                    MonsterInstance monster = _monsters[monsterIndex];

                    if (!monster.IsDefeated && IsMonsterInsideHazard(monster, hazard))
                    {
                        triggered = true;
                        break;
                    }
                }

                if (!triggered)
                {
                    continue;
                }

                ExecuteHazardBurst(hazard);
                DisposeHazard(hazard);
                _hazards.RemoveAt(i);
            }
        }

        private void TickHazards()
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                HazardInstance hazard = _hazards[i];

                if (hazard == null || !hazard.TickDownEachTurn)
                {
                    continue;
                }

                hazard.RemainingTurns--;

                if (hazard.RemainingTurns > 0)
                {
                    continue;
                }

                if (hazard.Type == HazardType.DelayedBlastMarker)
                {
                    ExecuteHazardBurst(hazard);
                }

                DisposeHazard(hazard);
                _hazards.RemoveAt(i);
            }
        }

        private void ExecuteHazardBurst(HazardInstance hazard)
        {
            if (hazard == null)
            {
                return;
            }

            Vector3 position = CellToWorld(hazard.CenterLane, hazard.Row);
            float diameter = CellSize * Mathf.Max(0.35f, hazard.RadiusCells) * 2f * Mathf.Max(0.05f, hazard.FallbackScale);
            Sprite fallbackSprite = hazard.Type == HazardType.TrapMine
                ? ProceduralSpriteLibrary.GetSquareSprite()
                : ProceduralSpriteLibrary.GetSoftCircleSprite();
            Color burstTint = new Color(hazard.Tint.r, hazard.Tint.g, hazard.Tint.b, 0.9f);

            SpawnOneShotEffect(
                hazard.Type == HazardType.TrapMine ? "TrapBurst" : "DelayedBurst",
                position,
                hazard.EffectPrefab,
                90,
                fallbackSprite,
                burstTint,
                Vector3.one * diameter,
                hazard.EffectLifetime);

            ApplyAreaDamage(hazard.CenterLane, hazard.Row, hazard.RadiusCells, hazard.BurstDamage, burstTint);
        }

        private int ApplyAreaDamage(float centerLane, int row, float radiusCells, int damage, Color tint)
        {
            int hits = 0;

            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                MonsterInstance monster = _monsters[i];

                if (monster == null || monster.IsDefeated)
                {
                    continue;
                }

                float monsterCenterLane = monster.Lane + ((Mathf.Max(1, monster.WidthCells) - 1) * 0.5f);
                float monsterCenterRow = monster.Row + ((Mathf.Max(1, monster.HeightCells) - 1) * 0.5f);
                float laneDistance = Mathf.Abs(monsterCenterLane - centerLane);
                float rowDistance = Mathf.Abs(monsterCenterRow - row);

                if (Mathf.Sqrt((laneDistance * laneDistance) + (rowDistance * rowDistance)) > radiusCells)
                {
                    continue;
                }

                ApplyDamageToMonster(monster, damage, tint);
                hits++;
            }

            return hits;
        }

        private void SpawnTurnMonsters()
        {
            int spawnCount = Mathf.Min(GetSpawnsPerTurnForCurrentRound(), GetCurrentMaxConcurrent() - _monsters.Count);
            spawnCount = Mathf.Max(0, spawnCount);

            for (int i = 0; i < spawnCount; i++)
            {
                int lane = FindSpawnLane();

                if (lane < 0)
                {
                    break;
                }

                SpawnMonsterInLane(lane);
            }

            _spawnTurnsRemaining = Mathf.Max(0, _spawnTurnsRemaining - 1);
        }

        private void SpawnMonsterInLane(int lane)
        {
            int health = _profile.BaseHealth + ((_currentRound - 1) * _profile.RoundHealthIncrease) + Random.Range(0, _profile.HealthVariance + 1);
            bool isRanged = Random.value < _profile.RangedMonsterChance;
            MonsterView view = GetMonsterViewFromPool($"Monster_{_spawnSequence:000}");
            view.Initialize(
                isRanged ? $"A{_currentRound} #{_spawnSequence + 1}" : $"R{_currentRound} #{_spawnSequence + 1}",
                health,
                _cellWidth * 0.76f,
                _cellHeight * 0.72f,
                isRanged ? _profile.RangedMonsterTint : _profile.MonsterTint);

            MonsterInstance instance = new MonsterInstance
            {
                View = view,
                Lane = lane,
                Row = Rows - 1,
                WidthCells = 1,
                HeightCells = 1,
                IsBoss = false,
                IsRanged = isRanged,
                RangedDamage = GetRangedDamageForCurrentRound(),
                CoinReward = GetCoinRewardForCurrentRound(),
                StepCharge = 0f,
                IsDefeated = false,
                DotDamagePerTurn = 0,
                DotTurnsRemaining = 0,
                CharmTurnsRemaining = 0,
                SlowMultiplier = 1f,
                SlowTurnsRemaining = 0
            };

            _monsters.Add(instance);
            Vector3 targetPosition = CellToWorld(lane, Rows - 1);
            Vector3 spawnPosition = new Vector3(
                targetPosition.x,
                _region.yMax + Mathf.Max(view.Height * 0.85f, _cellHeight * 0.5f),
                targetPosition.z);
            view.SetWorldPosition(spawnPosition);
            view.SetSortingOrder(70 + ((Rows - instance.Row) * 4));
            StartCoroutine(AnimateMonsterSpawn(instance, spawnPosition, targetPosition));
            _spawnSequence++;
        }

        private void SpawnBossMonster()
        {
            int maxLane = Mathf.Max(0, Columns - _profile.BossWidthCells);
            int maxRow = Mathf.Max(0, Rows - _profile.BossHeightCells);
            int lane = Random.Range(0, maxLane + 1);
            int row = Random.Range(Mathf.Max(0, Rows / 2), maxRow + 1);

            MonsterView view = GetMonsterViewFromPool($"Boss_{_spawnSequence:000}");
            view.Initialize(
                "BOSS",
                _profile.BossHealth,
                _cellWidth * _profile.BossWidthCells * 0.9f,
                _cellHeight * _profile.BossHeightCells * 0.9f,
                _profile.BossTint);

            MonsterInstance instance = new MonsterInstance
            {
                View = view,
                Lane = lane,
                Row = row,
                WidthCells = _profile.BossWidthCells,
                HeightCells = _profile.BossHeightCells,
                IsBoss = true,
                IsRanged = false,
                RangedDamage = 0,
                CoinReward = Mathf.Max(6, GetCoinRewardForCurrentRound() * 4),
                StepCharge = 0f,
                IsDefeated = false,
                DotDamagePerTurn = 0,
                DotTurnsRemaining = 0,
                CharmTurnsRemaining = 0,
                SlowMultiplier = 1f,
                SlowTurnsRemaining = 0
            };

            _monsters.Add(instance);
            view.SetWorldPosition(GetMonsterWorldPosition(instance));
            view.SetSortingOrder(72);
            _spawnSequence++;
        }

        private void FireRangedMonsterAttacks()
        {
            for (int i = 0; i < _monsters.Count; i++)
            {
                MonsterInstance monster = _monsters[i];

                if (monster == null || monster.IsDefeated || monster.View == null || !monster.IsRanged || monster.RangedDamage <= 0)
                {
                    continue;
                }

                SpawnEnemyProjectile(monster);
            }
        }

        private void ProcessBossTurn()
        {
            MonsterInstance boss = GetLivingBoss();

            if (boss == null)
            {
                return;
            }

            TryTeleportBoss(boss);
            TrySummonBossMinions(boss);
        }

        private IEnumerator AnimateMonsterSpawn(MonsterInstance monster, Vector3 startPosition, Vector3 targetPosition)
        {
            const float duration = 0.32f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (monster == null || monster.IsDefeated || monster.View == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                float settleOffset = Mathf.Sin(progress * Mathf.PI) * Mathf.Max(0.04f, CellSize * 0.05f);
                Vector3 position = Vector3.LerpUnclamped(startPosition, targetPosition, eased) + (Vector3.down * settleOffset);
                monster.View.SetWorldPosition(position);
                yield return null;
            }

            if (monster != null && !monster.IsDefeated && monster.View != null)
            {
                monster.View.SetWorldPosition(targetPosition);
            }
        }

        private int FindSpawnLane()
        {
            int bestLane = -1;
            int bestCount = int.MaxValue;

            for (int lane = 0; lane < Columns; lane++)
            {
                if (IsCellOccupied(lane, Rows - 1))
                {
                    continue;
                }

                int laneCount = 0;

                for (int i = 0; i < _monsters.Count; i++)
                {
                    if (!_monsters[i].IsDefeated && _monsters[i].Lane == lane)
                    {
                        laneCount++;
                    }
                }

                if (laneCount < bestCount)
                {
                    bestCount = laneCount;
                    bestLane = lane;
                }
            }

            return bestLane;
        }

        private bool ApplyDamageToMonster(MonsterInstance monster, int damage, Color tint)
        {
            if (monster == null || monster.IsDefeated || monster.View == null)
            {
                return false;
            }

            bool defeated = monster.View.ApplyDamage(Mathf.Max(0, damage), tint);

            if (defeated)
            {
                Vector3 coinDropPosition = monster.View.transform.position;
                int coinReward = Mathf.Max(1, monster.CoinReward);
                monster.IsDefeated = true;
                monster.View.PlayDeath(ReturnMonsterViewToPool);
                _monsters.Remove(monster);
                MonsterDefeated?.Invoke(coinDropPosition, coinReward, monster.IsRanged);
                TryCompleteWaveImmediately();
            }

            return defeated;
        }

        private void TryCompleteWaveImmediately()
        {
            if (!_battleActive)
            {
                return;
            }

            if (_spawnTurnsRemaining > 0 || _monsters.Count > 0)
            {
                return;
            }

            _battleActive = false;
            WaveCompleted?.Invoke();
        }

        private bool ApplyCharmToMonster(MonsterInstance monster, int charmTurns, GameObject effectPrefab, float fallbackScale)
        {
            if (monster == null || monster.IsDefeated || monster.View == null)
            {
                return false;
            }

            monster.CharmTurnsRemaining = Mathf.Max(monster.CharmTurnsRemaining, Mathf.Max(1, charmTurns));
            SpawnOneShotEffect(
                "CharmEffect",
                monster.View.transform.position,
                effectPrefab,
                91,
                ProceduralSpriteLibrary.GetSoftCircleSprite(),
                new Color(1f, 0.52f, 0.78f, 0.92f),
                Vector3.one * CellSize * Mathf.Max(0.05f, fallbackScale),
                0.45f);
            return true;
        }

        private void ApplyDotToMonster(MonsterInstance monster, int dotDamagePerTurn, int dotTurns, bool additive)
        {
            if (monster == null || monster.IsDefeated || dotDamagePerTurn <= 0 || dotTurns <= 0)
            {
                return;
            }

            monster.DotDamagePerTurn = additive
                ? monster.DotDamagePerTurn + dotDamagePerTurn
                : Mathf.Max(monster.DotDamagePerTurn, dotDamagePerTurn);
            monster.DotTurnsRemaining = Mathf.Max(monster.DotTurnsRemaining, dotTurns);
        }

        private void ApplySlowToMonster(MonsterInstance monster, float slowMultiplier, int slowTurns)
        {
            if (monster == null || monster.IsDefeated || slowTurns <= 0)
            {
                return;
            }

            monster.SlowMultiplier = monster.SlowTurnsRemaining > 0
                ? Mathf.Min(monster.SlowMultiplier, Mathf.Clamp(slowMultiplier, 0.1f, 1f))
                : Mathf.Clamp(slowMultiplier, 0.1f, 1f);
            monster.SlowTurnsRemaining = Mathf.Max(monster.SlowTurnsRemaining, slowTurns);
        }

        private void UpdateProjectiles()
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (i >= _projectiles.Count)
                {
                    continue;
                }

                ProjectileInstance projectile = _projectiles[i];
                MonsterInstance previousTarget = projectile.Target;
                MonsterInstance target = ResolveProjectileTarget(projectile);

                if (target == null || target.View == null)
                {
                    RemoveProjectileInstance(i, projectile);
                    continue;
                }

                Vector3 targetPosition = target.View.transform.position + (Vector3.up * 0.05f);
                if (previousTarget != target ||
                    projectile.TravelDuration <= 0f ||
                    Vector3.Distance(projectile.CachedTargetPosition, targetPosition) > (CellSize * 0.06f))
                {
                    RebuildProjectileCurve(projectile, projectile.Position, targetPosition);
                }

                projectile.TravelProgress += Time.deltaTime / Mathf.Max(0.05f, projectile.TravelDuration);
                float progress = Mathf.Clamp01(projectile.TravelProgress);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 2.35f);
                projectile.Position = EvaluateQuadraticBezier(projectile.CurveStart, projectile.CurveControl, targetPosition, easedProgress);
                projectile.VisualRoot.position = projectile.Position;

                if (progress < 1f && Vector3.Distance(projectile.Position, targetPosition) > 0.1f)
                {
                    continue;
                }

                Vector3 impactPosition = target.View.transform.position;
                SpawnProjectileImpactEffect(projectile, impactPosition);
                bool defeated = ApplyDamageToMonster(target, projectile.Damage, projectile.Tint);

                if (!defeated)
                {
                    if (projectile.DotTurns > 0)
                    {
                        ApplyDotToMonster(target, projectile.DotDamagePerTurn, projectile.DotTurns, projectile.AdditiveDot);
                    }

                    if (projectile.SlowTurns > 0)
                    {
                        ApplySlowToMonster(target, projectile.SlowMultiplier, projectile.SlowTurns);
                    }
                }

                if (TryContinueChainProjectile(projectile, target, impactPosition))
                {
                    continue;
                }

                RemoveProjectileInstance(i, projectile);
            }
        }

        private void UpdateEnemyProjectiles()
        {
            for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
            {
                EnemyProjectileInstance projectile = _enemyProjectiles[i];

                if (projectile == null || projectile.VisualRoot == null)
                {
                    RemoveEnemyProjectile(i, projectile);
                    continue;
                }

                projectile.TravelProgress += Time.deltaTime / Mathf.Max(0.05f, projectile.TravelDuration);
                float progress = Mathf.Clamp01(projectile.TravelProgress);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 2.2f);
                projectile.Position = EvaluateQuadraticBezier(projectile.CurveStart, projectile.CurveControl, projectile.TargetPosition, easedProgress);
                projectile.VisualRoot.position = projectile.Position;

                if (progress < 1f && Vector3.Distance(projectile.Position, projectile.TargetPosition) > 0.08f)
                {
                    continue;
                }

                PlayerDamaged?.Invoke(projectile.Damage, projectile.TargetPosition);
                RemoveEnemyProjectile(i, projectile);
            }
        }

        private MonsterInstance ResolveProjectileTarget(ProjectileInstance projectile)
        {
            if (projectile.Target != null && !projectile.Target.IsDefeated && projectile.Target.View != null)
            {
                return projectile.Target;
            }

            projectile.Target = GetRandomFrontMonster(projectile.RetargetFrontCount);
            return projectile.Target;
        }

        private void RebuildProjectileCurve(ProjectileInstance projectile, Vector3 startPosition, Vector3 targetPosition)
        {
            projectile.CurveStart = startPosition;
            projectile.CachedTargetPosition = targetPosition;
            projectile.TravelProgress = 0f;
            projectile.Position = startPosition;

            Vector3 delta = targetPosition - startPosition;
            float distance = Mathf.Max(0.001f, delta.magnitude);
            Vector3 direction = delta / distance;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            float lateralOffset = Mathf.Clamp(distance * 0.22f, CellSize * 0.18f, CellSize * 0.65f) * projectile.CurveSide;
            float verticalOffset = Mathf.Clamp(distance * 0.16f, CellSize * 0.12f, CellSize * 0.75f) + projectile.CurveLift;
            projectile.CurveControl = ((startPosition + targetPosition) * 0.5f) + (perpendicular * lateralOffset) + (Vector3.up * verticalOffset);
            projectile.TravelDuration = Mathf.Max(0.12f, distance / Mathf.Max(0.5f, projectile.Speed));
        }

        private static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float inverseT = 1f - t;
            return (inverseT * inverseT * start) + (2f * inverseT * t * control) + (t * t * end);
        }

        private void RemoveProjectileInstance(int index, ProjectileInstance projectile)
        {
            if (projectile == null)
            {
                return;
            }

            if (index >= 0 && index < _projectiles.Count && ReferenceEquals(_projectiles[index], projectile))
            {
                _projectiles.RemoveAt(index);
            }
            else
            {
                _projectiles.Remove(projectile);
            }

            DisposeProjectile(projectile);
        }

        private void RemoveEnemyProjectile(int index, EnemyProjectileInstance projectile)
        {
            if (index >= 0 && index < _enemyProjectiles.Count && ReferenceEquals(_enemyProjectiles[index], projectile))
            {
                _enemyProjectiles.RemoveAt(index);
            }
            else
            {
                _enemyProjectiles.Remove(projectile);
            }

            if (projectile != null && projectile.VisualRoot != null)
            {
                Destroy(projectile.VisualRoot.gameObject);
            }
        }

        private void SpawnProjectileImpactEffect(ProjectileInstance projectile, Vector3 impactPosition)
        {
            if (projectile == null)
            {
                return;
            }

            float width = Mathf.Max(CellSize * 0.14f, CellSize * projectile.ImpactFallbackScale * 0.18f);
            float height = Mathf.Max(CellSize * 0.4f, CellSize * projectile.ImpactFallbackScale);
            SpawnOneShotEffect(
                "LightningImpact",
                impactPosition,
                projectile.ImpactEffectPrefab,
                94,
                ProceduralSpriteLibrary.GetSquareSprite(),
                projectile.Tint,
                new Vector3(width, height, 1f),
                projectile.ImpactEffectLifetime);
        }

        private bool TryContinueChainProjectile(ProjectileInstance projectile, MonsterInstance hitTarget, Vector3 impactPosition)
        {
            if (projectile == null || projectile.ChainRemaining <= 0)
            {
                return false;
            }

            if (projectile.HitHistory == null)
            {
                projectile.HitHistory = new List<MonsterInstance>();
            }

            if (hitTarget != null && !projectile.HitHistory.Contains(hitTarget))
            {
                projectile.HitHistory.Add(hitTarget);
            }

            MonsterInstance nextTarget = GetClosestChainTarget(hitTarget, projectile.ChainSearchRadiusCells, projectile.HitHistory);

            if (nextTarget == null)
            {
                return false;
            }

            projectile.ChainRemaining--;
            projectile.Target = nextTarget;
            projectile.Position = impactPosition;
            projectile.VisualRoot.position = impactPosition;
            projectile.Damage = GetReducedChainDamage(projectile.Damage, projectile.ChainDamageFalloff);
            projectile.CurveSide *= -1f;
            projectile.CurveLift = Random.Range(CellSize * 0.08f, CellSize * 0.24f);
            RebuildProjectileCurve(projectile, impactPosition, nextTarget.View.transform.position + (Vector3.up * 0.05f));
            return true;
        }

        private static int GetReducedChainDamage(int currentDamage, float falloff)
        {
            if (currentDamage <= 1)
            {
                return 1;
            }

            int reducedDamage = Mathf.RoundToInt(currentDamage * Mathf.Clamp(falloff, 0.2f, 0.95f));
            return Mathf.Clamp(reducedDamage, 1, currentDamage - 1);
        }

        private MonsterInstance GetFrontMostMonster()
        {
            MonsterInstance best = null;

            for (int i = 0; i < _monsters.Count; i++)
            {
                MonsterInstance monster = _monsters[i];

                if (monster.IsDefeated)
                {
                    continue;
                }

                if (best == null || monster.Row < best.Row)
                {
                    best = monster;
                }
            }

            return best;
        }

        private MonsterInstance GetLivingBoss()
        {
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (!_monsters[i].IsDefeated && _monsters[i].IsBoss)
                {
                    return _monsters[i];
                }
            }

            return null;
        }

        private MonsterInstance GetRandomFrontMonster(int count)
        {
            List<MonsterInstance> sorted = GetActiveMonsters();

            if (sorted.Count == 0)
            {
                return null;
            }

            sorted.Sort((left, right) => left.Row.CompareTo(right.Row));
            return sorted[Random.Range(0, Mathf.Min(Mathf.Max(1, count), sorted.Count))];
        }

        private MonsterInstance GetRandomActiveMonster()
        {
            List<MonsterInstance> active = GetActiveMonsters();
            return active.Count == 0 ? null : active[Random.Range(0, active.Count)];
        }

        private MonsterInstance GetClosestChainTarget(MonsterInstance source, float radiusCells, List<MonsterInstance> excluded)
        {
            if (source == null)
            {
                return null;
            }

            MonsterInstance best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _monsters.Count; i++)
            {
                MonsterInstance candidate = _monsters[i];

                if (candidate == null ||
                    candidate.IsDefeated ||
                    candidate == source ||
                    (excluded != null && excluded.Contains(candidate)))
                {
                    continue;
                }

                float laneDistance = candidate.Lane - source.Lane;
                float rowDistance = candidate.Row - source.Row;
                float distance = Mathf.Sqrt((laneDistance * laneDistance) + (rowDistance * rowDistance));

                if (distance > radiusCells || distance >= bestDistance)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        private List<MonsterInstance> GetActiveMonsters()
        {
            List<MonsterInstance> active = new List<MonsterInstance>();

            for (int i = 0; i < _monsters.Count; i++)
            {
                if (!_monsters[i].IsDefeated)
                {
                    active.Add(_monsters[i]);
                }
            }

            return active;
        }

        private void TryTeleportBoss(MonsterInstance boss)
        {
            if (boss == null)
            {
                return;
            }

            int maxLane = Mathf.Max(0, Columns - boss.WidthCells);
            int maxRow = Mathf.Max(0, Rows - boss.HeightCells);

            for (int attempt = 0; attempt < 12; attempt++)
            {
                int lane = Random.Range(0, maxLane + 1);
                int row = Random.Range(Mathf.Max(0, Rows / 2), maxRow + 1);

                if (!CanPlaceMonsterAt(lane, row, boss.WidthCells, boss.HeightCells, boss))
                {
                    continue;
                }

                boss.Lane = lane;
                boss.Row = row;
                RepositionMonsters();
                return;
            }
        }

        private void TrySummonBossMinions(MonsterInstance boss)
        {
            if (boss == null || Random.value > _profile.BossSummonChancePerTurn)
            {
                return;
            }

            int summonCount = Random.Range(_profile.BossMinSummonCount, _profile.BossMaxSummonCount + 1);

            for (int i = 0; i < summonCount; i++)
            {
                if (!TryFindFreeCell(1, 1, boss, out int lane, out int row))
                {
                    break;
                }

                SpawnSummonedMinion(lane, row);
            }
        }

        private List<MonsterInstance> GetRandomActiveMonsters(int count)
        {
            List<MonsterInstance> active = GetActiveMonsters();
            int clampedCount = Mathf.Clamp(count, 0, active.Count);

            for (int i = 0; i < active.Count; i++)
            {
                int swapIndex = Random.Range(i, active.Count);
                MonsterInstance temp = active[i];
                active[i] = active[swapIndex];
                active[swapIndex] = temp;
            }

            if (clampedCount < active.Count)
            {
                active.RemoveRange(clampedCount, active.Count - clampedCount);
            }

            return active;
        }

        private int GetHazardDamage(MonsterInstance monster, out Color tint)
        {
            int damage = 0;
            tint = new Color(1f, 0.45f, 0.2f, 1f);

            for (int i = 0; i < _hazards.Count; i++)
            {
                HazardInstance hazard = _hazards[i];

                if (hazard == null || hazard.DamagePerTurn <= 0 || !IsMonsterInsideHazard(monster, hazard))
                {
                    continue;
                }

                damage += hazard.DamagePerTurn;
                tint = hazard.Tint;
            }

            return damage;
        }

        private float GetStepAdvanceForTurn(MonsterInstance monster)
        {
            float multiplier = 1f;

            if (monster.SlowTurnsRemaining > 0)
            {
                multiplier = Mathf.Min(multiplier, monster.SlowMultiplier);
                monster.SlowTurnsRemaining--;

                if (monster.SlowTurnsRemaining <= 0)
                {
                    monster.SlowMultiplier = 1f;
                }
            }

            for (int i = 0; i < _hazards.Count; i++)
            {
                HazardInstance hazard = _hazards[i];

                if (hazard.Type == HazardType.FrostWell && IsMonsterInsideHazard(monster, hazard))
                {
                    multiplier = Mathf.Min(multiplier, hazard.SlowMultiplier);
                }
            }

            return multiplier;
        }

        private bool IsMonsterInsideHazard(MonsterInstance monster, HazardInstance hazard)
        {
            if (monster == null || hazard == null)
            {
                return false;
            }

            float laneDistance = Mathf.Abs(monster.Lane - hazard.CenterLane);
            float rowDistance = Mathf.Abs(monster.Row - hazard.Row);

            switch (hazard.Type)
            {
                case HazardType.FrostWell:
                case HazardType.DelayedBlastMarker:
                case HazardType.TrapMine:
                    return Mathf.Sqrt((laneDistance * laneDistance) + (rowDistance * rowDistance)) <= hazard.RadiusCells;
                default:
                    return rowDistance <= 0.25f && laneDistance <= (hazard.WidthCells * 0.5f);
            }
        }

        private void TryGetEnemyAnchor(out float lane, out int row)
        {
            MonsterInstance anchor = GetRandomActiveMonster();

            if (anchor != null)
            {
                lane = anchor.Lane;
                row = anchor.Row;
                return;
            }

            lane = Random.Range(0, Mathf.Max(1, Columns));
            row = Random.Range(Mathf.Max(1, Rows / 2), Mathf.Max(2, Rows));
        }

        private bool TryGetTrapPlacement(out int lane, out int row)
        {
            List<MonsterInstance> active = GetActiveMonsters();
            List<Vector2Int> pathCells = new List<Vector2Int>();

            for (int i = 0; i < active.Count; i++)
            {
                MonsterInstance monster = active[i];

                for (int candidateRow = 0; candidateRow < monster.Row; candidateRow++)
                {
                    if (HasTrapAt(monster.Lane, candidateRow) || ContainsCell(pathCells, monster.Lane, candidateRow))
                    {
                        continue;
                    }

                    pathCells.Add(new Vector2Int(monster.Lane, candidateRow));
                }
            }

            if (pathCells.Count > 0)
            {
                Vector2Int selectedCell = pathCells[Random.Range(0, pathCells.Count)];
                lane = selectedCell.x;
                row = selectedCell.y;
                return true;
            }

            int attempts = Mathf.Max(1, Columns * Mathf.Max(1, Rows));

            for (int i = 0; i < attempts; i++)
            {
                lane = Random.Range(0, Columns);
                row = Random.Range(0, Mathf.Max(1, Rows));

                if (!HasTrapAt(lane, row))
                {
                    return true;
                }
            }

            lane = 0;
            row = 0;
            return false;
        }

        private static bool ContainsCell(List<Vector2Int> cells, int lane, int row)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].x == lane && cells[i].y == row)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasTrapAt(int lane, int row)
        {
            for (int i = 0; i < _hazards.Count; i++)
            {
                HazardInstance hazard = _hazards[i];

                if (hazard.Type == HazardType.TrapMine &&
                    Mathf.RoundToInt(hazard.CenterLane) == lane &&
                    hazard.Row == row)
                {
                    return true;
                }
            }

            return false;
        }

        private int WorldToRow(Vector3 worldPosition)
        {
            if (_cellHeight <= 0.0001f)
            {
                return 0;
            }

            float normalized = (worldPosition.y - _origin.y) / _cellHeight;
            return Mathf.RoundToInt(normalized);
        }

        private float WorldToLane(Vector3 worldPosition)
        {
            if (_cellWidth <= 0.0001f)
            {
                return 0f;
            }

            return (worldPosition.x - _origin.x) / _cellWidth;
        }

        private void SpawnOneShotEffect(string objectName, Vector3 position, GameObject effectPrefab, int sortingOrder, Sprite fallbackSprite, Color tint, Vector3 targetScale, float lifetime)
        {
            Transform visualRoot = CreateEffectVisual(
                objectName,
                transform,
                effectPrefab,
                sortingOrder,
                fallbackSprite,
                tint,
                out SpriteRenderer fallbackRenderer,
                out Vector3 visualBaseScale);

            visualRoot.position = position;
            ApplyVisualScale(visualRoot, visualBaseScale, targetScale);
            ApplyFallbackTint(fallbackRenderer, tint);
            Destroy(visualRoot.gameObject, Mathf.Max(0.05f, lifetime));
        }

        private static Transform CreateEffectVisual(
            string objectName,
            Transform parent,
            GameObject effectPrefab,
            int sortingOrder,
            Sprite fallbackSprite,
            Color fallbackTint,
            out SpriteRenderer fallbackRenderer,
            out Vector3 visualBaseScale)
        {
            GameObject visualObject;

            if (effectPrefab != null)
            {
                visualObject = Object.Instantiate(effectPrefab, parent);
                visualObject.name = objectName;
                fallbackRenderer = EnsureEffectRenderer(visualObject, fallbackSprite, fallbackTint);
            }
            else
            {
                visualObject = new GameObject(objectName);
                visualObject.transform.SetParent(parent, false);
                fallbackRenderer = visualObject.AddComponent<SpriteRenderer>();
                fallbackRenderer.sprite = fallbackSprite;
                fallbackRenderer.color = fallbackTint;
            }

            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualBaseScale = visualObject.transform.localScale;
            ApplySortingOrder(visualObject.transform, sortingOrder);
            return visualObject.transform;
        }

        private static SpriteRenderer EnsureEffectRenderer(GameObject visualObject, Sprite fallbackSprite, Color fallbackTint)
        {
            if (visualObject == null)
            {
                return null;
            }

            SpriteRenderer renderer = visualObject.GetComponentInChildren<SpriteRenderer>(true);

            if (renderer == null)
            {
                renderer = visualObject.AddComponent<SpriteRenderer>();
            }

            if (renderer.sprite == null)
            {
                renderer.sprite = fallbackSprite;
            }

            if (renderer.color.a <= 0f)
            {
                renderer.color = fallbackTint;
            }

            return renderer;
        }

        private static void ApplySortingOrder(Transform root, int sortingOrder)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = sortingOrder;
            }
        }

        private static void ApplyVisualScale(Transform root, Vector3 baseScale, Vector3 targetScale)
        {
            if (root == null)
            {
                return;
            }

            root.localScale = new Vector3(
                baseScale.x * targetScale.x,
                baseScale.y * targetScale.y,
                baseScale.z * targetScale.z);
        }

        private static void ApplyFallbackTint(SpriteRenderer fallbackRenderer, Color tint)
        {
            if (fallbackRenderer != null)
            {
                fallbackRenderer.color = tint;
            }
        }

        private void EnsureBattlefieldVisuals()
        {
            if (_backdrop == null)
            {
                GameObject backdropObject = new GameObject("MonsterFieldBackdrop");
                backdropObject.transform.SetParent(transform, false);
                _backdrop = backdropObject.AddComponent<SpriteRenderer>();
                _backdrop.sprite = ProceduralSpriteLibrary.GetSquareSprite();
                _backdrop.color = new Color(0.09f, 0.11f, 0.15f, 0.9f);
                _backdrop.sortingOrder = 1;
            }

            if (_frame == null)
            {
                GameObject frameObject = new GameObject("MonsterFieldFrame");
                frameObject.transform.SetParent(transform, false);
                _frame = frameObject.AddComponent<SpriteRenderer>();
                _frame.sprite = ProceduralSpriteLibrary.GetSquareSprite();
                _frame.color = new Color(0.98f, 0.94f, 0.75f, 0.12f);
                _frame.sortingOrder = 2;
            }

            int requiredLines = Columns + Rows + 2;

            while (_gridLines.Count < requiredLines)
            {
                GameObject lineObject = new GameObject($"FieldLine_{_gridLines.Count}");
                lineObject.transform.SetParent(transform, false);
                SpriteRenderer lineRenderer = lineObject.AddComponent<SpriteRenderer>();
                lineRenderer.sprite = ProceduralSpriteLibrary.GetSquareSprite();
                lineRenderer.color = new Color(1f, 1f, 1f, 0.08f);
                lineRenderer.sortingOrder = 3;
                _gridLines.Add(lineRenderer);
            }
        }

        private void UpdateLayout()
        {
            float usableWidth = Mathf.Max(0.1f, _region.width - (_profile.LanePadding * 2f));
            float usableHeight = Mathf.Max(0.1f, _region.height);
            float cellSize = Mathf.Min(usableWidth / Mathf.Max(1, Columns), usableHeight / Mathf.Max(1, Rows));

            _cellWidth = cellSize;
            _cellHeight = cellSize;

            Vector2 boardSize = new Vector2(cellSize * Columns, cellSize * Rows);
            Vector2 boardCenter = _region.center;
            _origin = boardCenter - (boardSize * 0.5f) + new Vector2(cellSize * 0.5f, cellSize * 0.5f);

            _backdrop.transform.position = boardCenter;
            _backdrop.transform.localScale = new Vector3(boardSize.x + 0.22f, boardSize.y + 0.22f, 1f);

            _frame.transform.position = boardCenter;
            _frame.transform.localScale = new Vector3(boardSize.x + 0.06f, boardSize.y + 0.06f, 1f);

            int lineIndex = 0;
            float left = _origin.x - (_cellWidth * 0.5f);
            float bottom = _origin.y - (_cellHeight * 0.5f);
            float right = left + boardSize.x;
            float top = bottom + boardSize.y;
            float lineThickness = Mathf.Max(0.02f, cellSize * 0.018f);

            for (int x = 0; x <= Columns; x++)
            {
                SpriteRenderer line = _gridLines[lineIndex++];
                line.transform.position = new Vector3(left + (x * _cellWidth), (top + bottom) * 0.5f, 0f);
                line.transform.localScale = new Vector3(lineThickness, boardSize.y, 1f);
            }

            for (int y = 0; y <= Rows; y++)
            {
                SpriteRenderer line = _gridLines[lineIndex++];
                line.transform.position = new Vector3((left + right) * 0.5f, bottom + (y * _cellHeight), 0f);
                line.transform.localScale = new Vector3(boardSize.x, lineThickness, 1f);
            }

            for (int i = lineIndex; i < _gridLines.Count; i++)
            {
                _gridLines[i].transform.localScale = Vector3.zero;
            }
        }

        private void RepositionMonsters()
        {
            for (int i = 0; i < _monsters.Count; i++)
            {
                MonsterInstance monster = _monsters[i];

                if (!monster.IsDefeated && monster.View != null)
                {
                    monster.View.SetWorldPosition(GetMonsterWorldPosition(monster));
                    monster.View.SetSortingOrder(70 + ((Rows - monster.Row) * 4));
                }
            }
        }

        private Vector3 CellToWorld(float lane, float row)
        {
            return new Vector3(_origin.x + (lane * _cellWidth), _origin.y + (row * _cellHeight), 0f);
        }

        private Vector3 GetMonsterWorldPosition(MonsterInstance monster)
        {
            if (monster == null)
            {
                return Vector3.zero;
            }

            float centerLane = monster.Lane + ((Mathf.Max(1, monster.WidthCells) - 1) * 0.5f);
            float centerRow = monster.Row + ((Mathf.Max(1, monster.HeightCells) - 1) * 0.5f);
            return CellToWorld(centerLane, centerRow);
        }

        private bool IsCellOccupied(int lane, int row)
        {
            return !CanPlaceMonsterAt(lane, row, 1, 1, null);
        }

        private void UpdateHazardVisual(HazardInstance hazard)
        {
            if (hazard == null || hazard.VisualRoot == null)
            {
                return;
            }

            float pulse = 1f + (Mathf.Sin((Time.time * 4.5f) + hazard.Row) * 0.04f);
            hazard.VisualRoot.position = CellToWorld(hazard.CenterLane, hazard.Row);

            switch (hazard.Type)
            {
                case HazardType.FrostWell:
                case HazardType.DelayedBlastMarker:
                    float diameter = CellSize * hazard.RadiusCells * 2f * pulse;
                    ApplyVisualScale(hazard.VisualRoot, hazard.VisualBaseScale, Vector3.one * diameter);
                    ApplyFallbackTint(hazard.FallbackRenderer, hazard.Tint);
                    break;
                case HazardType.TrapMine:
                    float size = CellSize * Mathf.Max(0.45f, hazard.RadiusCells * 0.78f) * pulse;
                    ApplyVisualScale(hazard.VisualRoot, hazard.VisualBaseScale, Vector3.one * size);
                    ApplyFallbackTint(hazard.FallbackRenderer, hazard.Tint);
                    break;
                default:
                    ApplyVisualScale(hazard.VisualRoot, hazard.VisualBaseScale, new Vector3(_cellWidth * hazard.WidthCells, _cellHeight * 0.72f * pulse, 1f));
                    ApplyFallbackTint(hazard.FallbackRenderer, hazard.Tint);
                    break;
            }
        }

        private int GetCurrentMaxConcurrent()
        {
            return _profile.MaxConcurrentMonsters + ((_currentRound - 1) * _profile.MaxConcurrentIncreasePerRound);
        }

        private int GetCoinRewardForCurrentRound()
        {
            return _profile.BaseCoinReward +
                ((_currentRound - 1) * _profile.CoinRewardIncreasePerRound) +
                Random.Range(0, _profile.CoinVariance + 1);
        }

        private int GetEscapeDamageForCurrentRound()
        {
            return _profile.EscapeDamage + ((_currentRound - 1) * _profile.EscapeDamageIncreasePerRound);
        }

        private int GetRangedDamageForCurrentRound()
        {
            return _profile.RangedAttackDamage + ((_currentRound - 1) * _profile.RangedAttackDamageIncreasePerRound);
        }

        private int GetSpawnTurnsForCurrentRound()
        {
            return _profile.SpawnTurnsPerRound + ((_currentRound - 1) * _profile.SpawnTurnsGrowthPerRound);
        }

        private int GetSpawnsPerTurnForCurrentRound()
        {
            return _profile.SpawnsPerTurn + ((_currentRound - 1) / 4);
        }

        private void ClearAllEffects()
        {
            for (int i = 0; i < _hazards.Count; i++)
            {
                DisposeHazard(_hazards[i]);
            }

            _hazards.Clear();

            for (int i = 0; i < _projectiles.Count; i++)
            {
                DisposeProjectile(_projectiles[i]);
            }

            _projectiles.Clear();

            for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
            {
                RemoveEnemyProjectile(i, _enemyProjectiles[i]);
            }
        }

        private void ClearLiveMonsters()
        {
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i].View != null)
                {
                    ReturnMonsterViewToPool(_monsters[i].View);
                }
            }

            _monsters.Clear();
        }

        private bool CanPlaceMonsterAt(int lane, int row, int widthCells, int heightCells, MonsterInstance ignore)
        {
            if (lane < 0 || row < 0 || lane + Mathf.Max(1, widthCells) > Columns || row + Mathf.Max(1, heightCells) > Rows)
            {
                return false;
            }

            for (int i = 0; i < _monsters.Count; i++)
            {
                MonsterInstance monster = _monsters[i];

                if (monster == null || monster.IsDefeated || monster == ignore)
                {
                    continue;
                }

                if (RectanglesOverlap(lane, row, widthCells, heightCells, monster.Lane, monster.Row, Mathf.Max(1, monster.WidthCells), Mathf.Max(1, monster.HeightCells)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RectanglesOverlap(int laneA, int rowA, int widthA, int heightA, int laneB, int rowB, int widthB, int heightB)
        {
            return laneA < laneB + widthB &&
                laneA + widthA > laneB &&
                rowA < rowB + heightB &&
                rowA + heightA > rowB;
        }

        private bool TryFindFreeCell(int widthCells, int heightCells, MonsterInstance ignore, out int lane, out int row)
        {
            int maxLane = Mathf.Max(0, Columns - widthCells);
            int maxRow = Mathf.Max(0, Rows - heightCells);

            for (int attempt = 0; attempt < 24; attempt++)
            {
                lane = Random.Range(0, maxLane + 1);
                row = Random.Range(Mathf.Max(0, Rows / 2), maxRow + 1);

                if (CanPlaceMonsterAt(lane, row, widthCells, heightCells, ignore))
                {
                    return true;
                }
            }

            lane = 0;
            row = 0;
            return false;
        }

        private void SpawnSummonedMinion(int lane, int row)
        {
            int health = Mathf.Max(1, _profile.BaseHealth + ((_currentRound - 1) * _profile.RoundHealthIncrease / 2));
            MonsterView view = GetMonsterViewFromPool($"Summon_{_spawnSequence:000}");
            view.Initialize($"S{_currentRound} #{_spawnSequence + 1}", health, _cellWidth * 0.7f, _cellHeight * 0.66f, _profile.MonsterTint);

            MonsterInstance instance = new MonsterInstance
            {
                View = view,
                Lane = lane,
                Row = row,
                WidthCells = 1,
                HeightCells = 1,
                IsBoss = false,
                IsRanged = false,
                RangedDamage = 0,
                CoinReward = Mathf.Max(1, GetCoinRewardForCurrentRound()),
                StepCharge = 0f,
                IsDefeated = false,
                DotDamagePerTurn = 0,
                DotTurnsRemaining = 0,
                CharmTurnsRemaining = 0,
                SlowMultiplier = 1f,
                SlowTurnsRemaining = 0
            };

            _monsters.Add(instance);
            view.SetWorldPosition(GetMonsterWorldPosition(instance));
            view.SetSortingOrder(70 + ((Rows - instance.Row) * 4));
            _spawnSequence++;
        }

        private static void DisposeHazard(HazardInstance hazard)
        {
            if (hazard != null && hazard.VisualRoot != null)
            {
                Object.Destroy(hazard.VisualRoot.gameObject);
            }
        }

        private static void DisposeProjectile(ProjectileInstance projectile)
        {
            if (projectile != null && projectile.VisualRoot != null)
            {
                Object.Destroy(projectile.VisualRoot.gameObject);
            }
        }

        private void HandleMonsterEscape(MonsterInstance monster)
        {
            if (monster == null || monster.IsDefeated)
            {
                return;
            }

            monster.IsDefeated = true;
            _monsters.Remove(monster);
            Vector3 damageOrigin = CellToWorld(monster.Lane, 0f);

            if (monster.View != null)
            {
                ReturnMonsterViewToPool(monster.View);
            }

            PlayerDamaged?.Invoke(GetEscapeDamageForCurrentRound(), damageOrigin);
            TryCompleteWaveImmediately();
        }

        private MonsterView GetMonsterViewFromPool(string objectName)
        {
            if (_monsterViewPool == null)
            {
                _monsterViewPool = new SimplePool<MonsterView>(
                    () =>
                    {
                        GameObject monsterObject = new GameObject("MonsterView");
                        monsterObject.transform.SetParent(transform, false);
                        return monsterObject.AddComponent<MonsterView>();
                    },
                    view =>
                    {
                        view.gameObject.SetActive(true);
                        view.transform.SetParent(transform, false);
                    },
                    view => view.DeactivateImmediate());
            }

            MonsterView monsterView = _monsterViewPool.Get();
            monsterView.name = objectName;
            return monsterView;
        }

        private void ReturnMonsterViewToPool(MonsterView view)
        {
            if (view == null)
            {
                return;
            }

            if (_monsterViewPool == null)
            {
                view.DeactivateImmediate();
                return;
            }

            _monsterViewPool.Release(view);
        }

        private void SpawnEnemyProjectile(MonsterInstance monster)
        {
            if (monster == null || monster.View == null)
            {
                return;
            }

            Transform visualRoot = CreateEffectVisual(
                "EnemyProjectile",
                transform,
                null,
                93,
                ProceduralSpriteLibrary.GetOrbSprite(),
                _profile.RangedMonsterTint,
                out SpriteRenderer unusedRenderer,
                out Vector3 visualBaseScale);

            Vector3 origin = monster.View.transform.position + new Vector3(0f, monster.View.Height * 0.05f, 0f);
            Vector3 target = GetPlayerImpactPosition();
            float distance = Mathf.Max(0.001f, Vector3.Distance(origin, target));
            float duration = Mathf.Max(0.14f, distance / Mathf.Max(0.5f, _profile.RangedProjectileSpeed));
            Vector3 direction = (target - origin).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            Vector3 control = ((origin + target) * 0.5f) + (perpendicular * Random.Range(-CellSize * 0.2f, CellSize * 0.2f)) + (Vector3.up * Mathf.Max(CellSize * 0.18f, distance * 0.14f));

            visualRoot.position = origin;
            ApplyVisualScale(visualRoot, visualBaseScale, Vector3.one * CellSize * 0.34f);

            _enemyProjectiles.Add(new EnemyProjectileInstance
            {
                VisualRoot = visualRoot,
                Position = origin,
                CurveStart = origin,
                CurveControl = control,
                TargetPosition = target,
                TravelProgress = 0f,
                TravelDuration = duration,
                Damage = monster.RangedDamage
            });
        }

        private Vector3 GetPlayerImpactPosition()
        {
            return new Vector3(_region.center.x, _region.yMin - Mathf.Max(_cellHeight * 0.32f, 0.18f), 0f);
        }
    }
}
