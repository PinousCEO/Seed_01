using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleBattle
{
    public sealed class PuzzleBattleRuntimeBootstrap : MonoBehaviour
    {
        private sealed class SkillChoiceCard
        {
            public PuzzleBattleSkillDefinition Skill;
            public int Index;
            public RectTransform Root;
            public Image Background;
            public Image Accent;
            public Text Title;
            public Text Description;
            public Text ActionLabel;
            public Button Button;
            public Rect Bounds;
        }

        private sealed class AcquiredSkillIcon
        {
            public RectTransform Root;
            public Image Frame;
            public Image Icon;
            public Text LevelLabel;
        }

        private sealed class HudButton
        {
            public string Id;
            public RectTransform Root;
            public Image Background;
            public Text Label;
            public Button Button;
            public Rect Bounds;
        }

        private enum CoinPickupState
        {
            Dropping,
            Grounded,
            Collecting
        }

        private sealed class CoinPickupVisual
        {
            public RectTransform Root;
            public Image Icon;
            public int Value;
            public bool IsHealthPickup;
            public CoinPickupState State;
            public Vector2 StartLocal;
            public Vector2 ControlLocal;
            public Vector2 EndLocal;
            public float Progress;
            public float Duration;
            public float ReadyAt;
        }

        private Camera _camera;
        private PuzzleBattleBoardProfile _boardProfile;
        private MonsterWaveProfile _monsterWaveProfile;
        private PlayerStatusProfile _playerStatusProfile;
        private readonly List<PuzzleBattleSkillDefinition> _skillDefinitions = new List<PuzzleBattleSkillDefinition>();
        private readonly Dictionary<PuzzleBattleSkillId, int> _skillLevels = new Dictionary<PuzzleBattleSkillId, int>();
        private SpriteRenderer _topBackground;
        private SpriteRenderer _topBattlefieldArt;
        private SpriteRenderer _wallDeco;
        private SpriteRenderer _bottomBackground;
        private SpriteRenderer _divider;
        private PuzzleBattleCanvasHost _canvasHost;
        private PuzzleBattleUiDocument _uiDocument;
        private Canvas _uiCanvas;
        private RectTransform _uiRoot;
        private RectTransform _topUiRoot;
        private RectTransform _cardAreaRoot;
        private RectTransform _coinHudRoot;
        private RectTransform _turnTimerBarRoot;
        private Image _turnTimerBarBackground;
        private Image _turnTimerBarFill;
        private RectTransform _playerHealthRoot;
        private Image _playerHealthBarBackground;
        private Image _playerHealthBarFill;
        private Text _playerHealthLabel;
        private Font _uiFont;
        private Match3BoardController _boardController;
        private MonsterLaneController _monsterLaneController;
        private Image _coinHudIcon;
        private Text _coinLabel;
        private Text _roundLabel;
        private Text _statusLabel;
        private Text _timerLabel;
        private Text _skillsLabel;
        private Text _comboLabel;
        private readonly List<AcquiredSkillIcon> _skillIcons = new List<AcquiredSkillIcon>();
        private readonly List<HudButton> _hudButtons = new List<HudButton>();
        private readonly List<SkillChoiceCard> _skillCards = new List<SkillChoiceCard>();
        private readonly List<PuzzleBattleSkillDefinition> _presentedChoices = new List<PuzzleBattleSkillDefinition>();
        private readonly List<CoinPickupVisual> _coinPickups = new List<CoinPickupVisual>();
        private SimplePool<CoinPickupVisual> _coinPickupPool;
        private Vector2Int _lastScreenSize;
        private Rect _monsterRegion;
        private Coroutine _comboRoutine;
        private Coroutine _coinHudPulseRoutine;
        private Coroutine _turnAdvanceRoutine;
        private int _currentRound = 1;
        private int _coins;
        private int _playerAttackBonus;
        private int _playerMaxHealth;
        private int _playerCurrentHealth;
        private int _blueOrbsClearedThisRound;
        private float _turnTimeRemaining;
        private bool _turnTimerActive;
        private bool _battleActive;
        private bool _gameOver;
        private bool _skillSelectionActive;
        private bool _settingsOpen;
        private string _statusText = "Pick a skill to begin.";
        private string _defaultComboText = "Select a skill to start the round.";
        private const float PlayerTurnDurationSeconds = 20f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PuzzleBattleRuntimeBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("PuzzleBattleBootstrap");
            bootstrapObject.AddComponent<PuzzleBattleRuntimeBootstrap>();
        }

        private void Awake()
        {
            CleanupLegacySceneObjects();
            _camera = EnsureCamera();
            _canvasHost = FindFirstObjectByType<PuzzleBattleCanvasHost>();
            _boardProfile = LoadBoardProfile();
            _monsterWaveProfile = LoadMonsterWaveProfile();
            _playerStatusProfile = LoadPlayerStatusProfile();
            LoadSkillDefinitions();
            _playerMaxHealth = _playerStatusProfile != null ? _playerStatusProfile.MaxHealth : 500;
            _playerAttackBonus = _playerStatusProfile != null ? _playerStatusProfile.AttackBonus : 8;
            _playerCurrentHealth = _playerMaxHealth;
            _turnTimeRemaining = PlayerTurnDurationSeconds;

            if (!CreateSceneVisuals())
            {
                enabled = false;
                return;
            }

            CreateControllers();
            LayoutScene();

            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        private void Start()
        {
            StartCoroutine(ShowInitialSkillSelectionRoutine());
        }

        private void Update()
        {
            if (_lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
            {
                LayoutScene();
                _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            }

            UpdateCoinPickups();
            UpdateTurnTimer();
            UpdateHud();
        }

        private void OnMonsterReachedPlayer()
        {
            StopPlayerTurnTimer();
            _battleActive = false;
            _gameOver = true;
            _skillSelectionActive = false;
            _boardController.SetInputEnabled(false);
            _monsterLaneController.SetBattleActive(false);
            _monsterLaneController.ClearTransientEffects();
            SetSkillCardsVisible(false);
            _statusText = "The monsters reached the player.";
            _defaultComboText = "Run ended.";
        }

        private void OnWaveCompleted()
        {
            if (_gameOver)
            {
                return;
            }

            StopPlayerTurnTimer();
            _battleActive = false;
            _boardController.SetInputEnabled(false);
            _monsterLaneController.SetBattleActive(false);
            _monsterLaneController.ClearTransientEffects();

            if (_currentRound >= (_monsterWaveProfile != null ? _monsterWaveProfile.FinalRound : 20))
            {
                _gameOver = true;
                _statusText = "Final boss defeated.";
                _defaultComboText = "Victory.";
                SetSkillCardsVisible(false);
                return;
            }

            _currentRound++;
            _statusText = $"Round cleared. Choose a skill for Round {_currentRound}.";
            _defaultComboText = "All cascades from one move count as one attack.";
            ShowSkillSelectionForCurrentRound();
        }

        private void OnMonsterDefeated(Vector3 worldPosition, int coinReward, bool droppedHealthPickup)
        {
            SpawnCoinDrops(worldPosition, coinReward);

            if (droppedHealthPickup)
            {
                SpawnHealthPickup(worldPosition, _playerStatusProfile != null ? _playerStatusProfile.HealPickupAmount : 20);
            }

            ApplyDeathTriggeredSkills(worldPosition);
        }

        private void OnTurnResolved(Match3BoardController.AttackResult attack)
        {
            if (!_battleActive || _gameOver || _skillSelectionActive)
            {
                return;
            }

            StopPlayerTurnTimer();

            if (attack != null && attack.TotalOrbsCleared == 0)
            {
                _statusText = $"Round {_currentRound}: no match, monsters advanced.";
            }

            if (_turnAdvanceRoutine != null)
            {
                StopCoroutine(_turnAdvanceRoutine);
            }

            _boardController.SetInputEnabled(false);
            _turnAdvanceRoutine = StartCoroutine(AdvanceMonsterTurnRoutine());
        }

        private void OnAttackResolved(Match3BoardController.AttackResult attack)
        {
            if (!_battleActive || _gameOver || attack == null)
            {
                return;
            }

            int totalDamage = 0;
            OrbVolleySkillDefinition orbVolley = GetSkillDefinition<OrbVolleySkillDefinition>(PuzzleBattleSkillId.OrbVolley);
            int orbVolleyLevel = GetSkillLevel(PuzzleBattleSkillId.OrbVolley);
            bool deliversBaseDamageViaProjectiles = orbVolley != null && orbVolleyLevel > 0;

            for (int i = 0; i < attack.Matches.Count; i++)
            {
                Match3BoardController.MatchResult match = attack.Matches[i];
                int damage;

                if (deliversBaseDamageViaProjectiles)
                {
                    int projectileDamage = Mathf.Max(1, Mathf.RoundToInt((match.Definition.DamagePerOrb + _playerAttackBonus) * orbVolley.GetDamageMultiplier(orbVolleyLevel)));
                    damage = projectileDamage * match.Size;
                }
                else
                {
                    float cascadeMultiplier = 1f + ((match.CascadeIndex - 1) * 0.25f);
                    damage = Mathf.RoundToInt((match.Definition.DamagePerOrb + _playerAttackBonus) * match.Size * cascadeMultiplier);
                    _monsterLaneController.ApplyDamage(damage, match.DisplayColor);
                }

                totalDamage += damage;
            }

            List<string> triggeredSkills = ApplySkillEffects(attack);
            _statusText = $"Round {_currentRound}: {attack.TotalCombos} combo, {totalDamage} damage";

            if (triggeredSkills.Count > 0)
            {
                _statusText += $" / {string.Join(", ", triggeredSkills)}";
            }

            if (_comboRoutine != null)
            {
                StopCoroutine(_comboRoutine);
            }

            _comboRoutine = StartCoroutine(ShowComboRoutine(attack.TotalCombos, totalDamage));
        }

        private List<string> ApplySkillEffects(Match3BoardController.AttackResult attack)
        {
            List<string> triggeredSkills = new List<string>();

            OrbVolleySkillDefinition orbVolley = GetSkillDefinition<OrbVolleySkillDefinition>(PuzzleBattleSkillId.OrbVolley);
            int orbVolleyLevel = GetSkillLevel(PuzzleBattleSkillId.OrbVolley);

            if (orbVolley != null && orbVolleyLevel > 0)
            {
                int volleyCount = 0;

                for (int i = 0; i < attack.Matches.Count; i++)
                {
                    Match3BoardController.MatchResult match = attack.Matches[i];
                    int projectileDamage = Mathf.Max(1, Mathf.RoundToInt((match.Definition.DamagePerOrb + _playerAttackBonus) * orbVolley.GetDamageMultiplier(orbVolleyLevel)));

                    for (int orbIndex = 0; orbIndex < match.Size; orbIndex++)
                    {
                        Vector3 origin = GetProjectileOrigin(volleyCount);

                        if (_monsterLaneController.SpawnOrbProjectile(
                            match.Definition,
                            orbVolley.ProjectileEffectPrefab,
                            match.DisplayColor,
                            projectileDamage,
                            origin,
                            orbVolley.GetProjectileSpeed(orbVolleyLevel),
                            orbVolley.FallbackScale))
                        {
                            volleyCount++;
                        }
                    }
                }

                if (volleyCount > 0)
                {
                    triggeredSkills.Add($"{orbVolley.DisplayName} Lv.{orbVolleyLevel}");
                }
            }

            EarthquakeSkillDefinition earthquake = GetSkillDefinition<EarthquakeSkillDefinition>(PuzzleBattleSkillId.Earthquake);
            int earthquakeLevel = GetSkillLevel(PuzzleBattleSkillId.Earthquake);

            if (earthquake != null && earthquakeLevel > 0)
            {
                int quakeHits = 0;

                for (int i = 0; i < attack.Matches.Count; i++)
                {
                    Match3BoardController.MatchResult match = attack.Matches[i];

                    if (match.Definition == null || match.Definition.OrbId != "wood")
                    {
                        continue;
                    }

                    quakeHits += _monsterLaneController.TriggerEarthquake(
                        earthquake.GetRadius(earthquakeLevel, match.Size),
                        earthquake.GetDamage(earthquakeLevel, match.Size),
                        earthquake.AccentColor,
                        earthquake.EffectPrefab,
                        earthquake.FallbackScale,
                        earthquake.EffectLifetime);
                }

                if (quakeHits > 0)
                {
                    triggeredSkills.Add($"{earthquake.DisplayName} Lv.{earthquakeLevel}");
                }
            }

            int blueCount = attack.GetClearedCount("water");
            FrostWellSkillDefinition frostWell = GetSkillDefinition<FrostWellSkillDefinition>(PuzzleBattleSkillId.FrostWell);
            int frostWellLevel = GetSkillLevel(PuzzleBattleSkillId.FrostWell);

            if (frostWell != null && frostWellLevel > 0 && blueCount > 0)
            {
                _blueOrbsClearedThisRound += blueCount;
                float powerScale = Mathf.Clamp01(_blueOrbsClearedThisRound / 18f);
                float radiusCells = Mathf.Lerp(frostWell.GetMinRadius(frostWellLevel), frostWell.GetMaxRadius(frostWellLevel), powerScale);
                int damagePerTurn = frostWell.GetDamagePerTurn(frostWellLevel, _blueOrbsClearedThisRound);
                float slowMultiplier = Mathf.Lerp(frostWell.GetStartSlow(frostWellLevel), frostWell.GetMaxSlow(frostWellLevel), powerScale);
                int lifetimeTurns = frostWell.GetDurationTurns(frostWellLevel, _blueOrbsClearedThisRound);
                _monsterLaneController.SpawnFrostWell(radiusCells, damagePerTurn, slowMultiplier, lifetimeTurns, frostWell.WellEffectPrefab);
                triggeredSkills.Add($"{frostWell.DisplayName} Lv.{frostWellLevel}");
            }

            IceOrbSkillDefinition iceOrb = GetSkillDefinition<IceOrbSkillDefinition>(PuzzleBattleSkillId.IceOrb);
            int iceOrbLevel = GetSkillLevel(PuzzleBattleSkillId.IceOrb);

            if (iceOrb != null && iceOrbLevel > 0 && blueCount > 0)
            {
                int launchedOrbs = 0;

                for (int i = 0; i < blueCount; i++)
                {
                    if (_monsterLaneController.SpawnIceProjectile(
                        iceOrb.EffectPrefab,
                        iceOrb.AccentColor,
                        iceOrb.GetDamage(iceOrbLevel),
                        iceOrb.GetSlowMultiplier(iceOrbLevel),
                        iceOrb.GetSlowTurns(iceOrbLevel),
                        GetProjectileOrigin(i + 67),
                        iceOrb.GetProjectileSpeed(iceOrbLevel),
                        iceOrb.FallbackScale))
                    {
                        launchedOrbs++;
                    }
                }

                if (launchedOrbs > 0)
                {
                    triggeredSkills.Add($"{iceOrb.DisplayName} Lv.{iceOrbLevel}");
                }
            }

            int redCount = attack.GetClearedCount("fire");
            FlameCurtainSkillDefinition flameCurtain = GetSkillDefinition<FlameCurtainSkillDefinition>(PuzzleBattleSkillId.FlameCurtain);
            int flameCurtainLevel = GetSkillLevel(PuzzleBattleSkillId.FlameCurtain);

            if (flameCurtain != null && flameCurtainLevel > 0 && redCount > 0)
            {
                int fullSpanThreshold = flameCurtain.GetFullSpanThreshold(flameCurtainLevel);
                float progress = Mathf.Clamp01(redCount / (float)fullSpanThreshold);
                float activeColumns = Mathf.Max(1, _monsterLaneController.ColumnCount);
                float widthCells = Mathf.Lerp(
                    Mathf.Max(1f, flameCurtain.GetStartWidthNormalized(flameCurtainLevel) * activeColumns),
                    activeColumns,
                    progress);
                int damagePerTurn = flameCurtain.GetDamagePerTurn(flameCurtainLevel, redCount);
                int lifetimeTurns = flameCurtain.GetDurationTurns(flameCurtainLevel, redCount);
                _monsterLaneController.SpawnFlameCurtain(widthCells, damagePerTurn, lifetimeTurns, flameCurtain.CurtainEffectPrefab);
                triggeredSkills.Add($"{flameCurtain.DisplayName} Lv.{flameCurtainLevel}");
            }

            TrapMineSkillDefinition trapMine = GetSkillDefinition<TrapMineSkillDefinition>(PuzzleBattleSkillId.TrapMine);
            int trapMineLevel = GetSkillLevel(PuzzleBattleSkillId.TrapMine);

            if (trapMine != null && trapMineLevel > 0)
            {
                int placedTraps = 0;

                for (int i = 0; i < attack.Matches.Count; i++)
                {
                    Match3BoardController.MatchResult match = attack.Matches[i];

                    if (match.Definition == null || match.Definition.OrbId != "fire")
                    {
                        continue;
                    }

                    bool empowered = match.Size >= 5;

                    if (_monsterLaneController.PlaceTrapMine(
                        trapMine.GetRadius(trapMineLevel, empowered),
                        trapMine.GetDamage(trapMineLevel, empowered),
                        empowered,
                        trapMine.AccentColor,
                        trapMine.EffectPrefab,
                        trapMine.FallbackScale,
                        trapMine.EffectLifetime))
                    {
                        placedTraps++;
                    }
                }

                if (placedTraps > 0)
                {
                    triggeredSkills.Add($"{trapMine.DisplayName} Lv.{trapMineLevel}");
                }
            }

            int darkCount = attack.GetClearedCount("dark");
            BatSwarmSkillDefinition batSwarm = GetSkillDefinition<BatSwarmSkillDefinition>(PuzzleBattleSkillId.BatSwarm);
            int batSwarmLevel = GetSkillLevel(PuzzleBattleSkillId.BatSwarm);

            if (batSwarm != null && batSwarmLevel > 0 && darkCount > 0)
            {
                int launchedBats = 0;
                Color batColor = GetOrbColor("dark");

                for (int i = 0; i < darkCount; i++)
                {
                    Vector3 origin = GetProjectileOrigin(i);

                    if (_monsterLaneController.SpawnBatProjectile(
                        batSwarm.BatEffectPrefab,
                        batColor,
                        batSwarm.GetDamage(batSwarmLevel),
                        batSwarm.GetDotDamage(batSwarmLevel),
                        batSwarm.GetDotTurns(batSwarmLevel),
                        origin,
                        batSwarm.GetProjectileSpeed(batSwarmLevel),
                        batSwarm.FallbackScale))
                    {
                        launchedBats++;
                    }
                }

                if (launchedBats > 0)
                {
                    triggeredSkills.Add($"{batSwarm.DisplayName} Lv.{batSwarmLevel}");
                }
            }

            PoisonNeedleSkillDefinition poisonNeedles = GetSkillDefinition<PoisonNeedleSkillDefinition>(PuzzleBattleSkillId.PoisonNeedles);
            int poisonLevel = GetSkillLevel(PuzzleBattleSkillId.PoisonNeedles);

            if (poisonNeedles != null && poisonLevel > 0 && darkCount > 0)
            {
                int launchedNeedles = 0;

                for (int i = 0; i < darkCount; i++)
                {
                    if (_monsterLaneController.SpawnPoisonNeedleProjectile(
                        poisonNeedles.EffectPrefab,
                        poisonNeedles.AccentColor,
                        poisonNeedles.GetDamage(poisonLevel),
                        poisonNeedles.GetDotDamage(poisonLevel),
                        poisonNeedles.DotTurns,
                        GetProjectileOrigin(i + 31),
                        poisonNeedles.GetProjectileSpeed(poisonLevel),
                        poisonNeedles.FallbackScale))
                    {
                        launchedNeedles++;
                    }
                }

                if (launchedNeedles > 0)
                {
                    triggeredSkills.Add($"{poisonNeedles.DisplayName} Lv.{poisonLevel}");
                }
            }

            int lightCount = attack.GetClearedCount("light");
            LightningStrikeSkillDefinition lightning = GetSkillDefinition<LightningStrikeSkillDefinition>(PuzzleBattleSkillId.LightningStrike);
            int lightningLevel = GetSkillLevel(PuzzleBattleSkillId.LightningStrike);

            if (lightning != null && lightningLevel > 0 && lightCount > 0)
            {
                int strikes = lightCount + lightning.GetBonusTargets(lightningLevel);
                int applied = _monsterLaneController.StrikeRandomEnemies(
                    strikes,
                    lightning.GetDamage(lightningLevel),
                    GetOrbColor("light"),
                    lightning.LightningEffectPrefab,
                    lightning.FallbackScale,
                    lightning.EffectLifetime);

                if (applied > 0)
                {
                    triggeredSkills.Add($"{lightning.DisplayName} Lv.{lightningLevel}");
                }
            }

            ChainLightningSkillDefinition chainLightning = GetSkillDefinition<ChainLightningSkillDefinition>(PuzzleBattleSkillId.ChainLightning);
            int chainLightningLevel = GetSkillLevel(PuzzleBattleSkillId.ChainLightning);

            if (chainLightning != null && chainLightningLevel > 0 && lightCount > 0)
            {
                int launchedOrbs = 0;
                int chainCount = chainLightning.GetChainCount(chainLightningLevel);

                if (lightCount >= 5)
                {
                    chainCount++;
                }

                for (int i = 0; i < attack.Matches.Count; i++)
                {
                    Match3BoardController.MatchResult match = attack.Matches[i];

                    if (match.Definition == null || match.Definition.OrbId != "light")
                    {
                        continue;
                    }

                    for (int orbIndex = 0; orbIndex < match.Size; orbIndex++)
                    {
                        if (_monsterLaneController.SpawnLightningOrbProjectile(
                            match.Definition,
                            chainLightning.LightningEffectPrefab,
                            match.DisplayColor,
                            chainLightning.GetDamage(chainLightningLevel),
                            chainCount,
                            chainLightning.ChainDamageFalloff,
                            chainLightning.ChainSearchRadiusCells,
                            GetProjectileOrigin(orbIndex + (i * 17)),
                            chainLightning.ProjectileSpeed,
                            chainLightning.FallbackScale,
                            chainLightning.EffectLifetime))
                        {
                            launchedOrbs++;
                        }
                    }
                }

                if (launchedOrbs > 0)
                {
                    triggeredSkills.Add($"{chainLightning.DisplayName} Lv.{chainLightningLevel}");
                }
            }

            SolarBeaconSkillDefinition solarBeacon = GetSkillDefinition<SolarBeaconSkillDefinition>(PuzzleBattleSkillId.SolarBeacon);
            int solarBeaconLevel = GetSkillLevel(PuzzleBattleSkillId.SolarBeacon);

            if (solarBeacon != null && solarBeaconLevel > 0)
            {
                int placedBeacons = 0;

                for (int i = 0; i < attack.Matches.Count; i++)
                {
                    Match3BoardController.MatchResult match = attack.Matches[i];

                    if (match.Definition == null || match.Definition.OrbId != "light")
                    {
                        continue;
                    }

                    if (_monsterLaneController.PlaceSolarBeacon(
                        solarBeacon.GetRadius(solarBeaconLevel, match.Size),
                        solarBeacon.GetDamage(solarBeaconLevel),
                        solarBeacon.DelayTurns,
                        solarBeacon.AccentColor,
                        solarBeacon.EffectPrefab,
                        solarBeacon.FallbackScale,
                        solarBeacon.EffectLifetime))
                    {
                        placedBeacons++;
                    }
                }

                if (placedBeacons > 0)
                {
                    triggeredSkills.Add($"{solarBeacon.DisplayName} Lv.{solarBeaconLevel}");
                }
            }

            int heartCount = attack.GetClearedCount("heart");
            CharmingHeartSkillDefinition charm = GetSkillDefinition<CharmingHeartSkillDefinition>(PuzzleBattleSkillId.CharmingHeart);
            int charmLevel = GetSkillLevel(PuzzleBattleSkillId.CharmingHeart);

            if (charm != null && charmLevel > 0 && heartCount > 0)
            {
                int fullCharmThreshold = charm.GetFullCharmThreshold(charmLevel);
                int charmTurns = charm.GetCharmTurns(charmLevel);
                int charmedCount;

                if (heartCount >= fullCharmThreshold)
                {
                    charmedCount = _monsterLaneController.CharmAllEnemies(charmTurns, charm.CharmEffectPrefab, charm.FallbackScale);
                }
                else
                {
                    int targetCount = (heartCount / charm.HeartsPerCharm) + charm.GetBonusTargets(charmLevel);
                    charmedCount = targetCount > 0
                        ? _monsterLaneController.CharmRandomEnemies(targetCount, charmTurns, charm.CharmEffectPrefab, charm.FallbackScale)
                        : 0;
                }

                if (charmedCount > 0)
                {
                    triggeredSkills.Add($"{charm.DisplayName} Lv.{charmLevel}");
                }
            }

            return triggeredSkills;
        }

        private IEnumerator AdvanceMonsterTurnRoutine()
        {
            yield return new WaitForSeconds(0.12f);
            _monsterLaneController.AdvanceTurn();

            while (!_gameOver && _monsterLaneController != null && _monsterLaneController.ActiveProjectileCount > 0)
            {
                yield return null;
            }

            _turnAdvanceRoutine = null;

            if (_battleActive && !_gameOver && !_skillSelectionActive)
            {
                _boardController.SetInputEnabled(true);
                StartPlayerTurnTimer();
            }
        }

        private IEnumerator ShowComboRoutine(int combos, int damage)
        {
            _comboLabel.text = $"{combos} COMBO  {damage} DMG";
            _comboLabel.color = new Color(1f, 0.95f, 0.68f, 1f);
            yield return new WaitForSeconds(1f);
            _comboLabel.color = new Color(1f, 1f, 1f, 0.82f);
            _comboRoutine = null;
        }

        private bool CreateSceneVisuals()
        {
            ResolveBattlefieldSceneRenderers();
            _topBackground = CreatePanel("TopBackground", new Color(0.09f, 0.13f, 0.18f, 1f), -20);

            if (_topBattlefieldArt == null)
            {
                _topBattlefieldArt = CreateSpriteLayer("TopBattlefieldArt", null, 0);
            }

            if (_wallDeco == null)
            {
                _wallDeco = CreateSpriteLayer("WallDeco", null, 4);
            }

            _bottomBackground = CreatePanel("BottomBackground", new Color(0.13f, 0.11f, 0.18f, 1f), -20);
            _divider = CreatePanel("Divider", new Color(0.98f, 0.92f, 0.6f, 0.9f), -10);

            if (!EnsureUiCanvas())
            {
                Debug.LogError("PuzzleBattle UI is not configured. Add a Canvas with PuzzleBattleUiDocument and connect it through PuzzleBattleCanvasHost or place the document in the scene.", this);
                return false;
            }

            SetSkillCardsVisible(false);
            return true;
        }

        private void CreateControllers()
        {
            GameObject boardObject = new GameObject("Match3Board");
            boardObject.transform.SetParent(transform, false);
            _boardController = boardObject.AddComponent<Match3BoardController>();
            _boardController.AttackResolved += OnAttackResolved;
            _boardController.TurnResolved += OnTurnResolved;

            GameObject monsterLaneObject = new GameObject("MonsterLane");
            monsterLaneObject.transform.SetParent(transform, false);
            _monsterLaneController = monsterLaneObject.AddComponent<MonsterLaneController>();
            _monsterLaneController.MonsterReachedPlayer += OnMonsterReachedPlayer;
            _monsterLaneController.MonsterDefeated += OnMonsterDefeated;
            _monsterLaneController.WaveCompleted += OnWaveCompleted;
            _monsterLaneController.PlayerDamaged += OnPlayerDamaged;
        }

        private void LayoutScene()
        {
            float orthoSize = _camera.orthographicSize;
            float halfWidth = orthoSize * _camera.aspect;
            float dividerY = 0f;
            float headerHeight = 1.22f;
            float footerHeight = 0.96f;
            float sidePadding = 0.18f;

            Rect topRect = Rect.MinMaxRect(-halfWidth, dividerY, halfWidth, orthoSize);
            Rect bottomRect = Rect.MinMaxRect(-halfWidth, -orthoSize, halfWidth, dividerY);

            LayoutPanel(_topBackground, topRect);
            LayoutPanel(_bottomBackground, bottomRect);

            _divider.transform.position = Vector3.zero;
            _divider.transform.localScale = new Vector3(topRect.width, 0.08f, 1f);

            ResolveBattlefieldRect(topRect);
            float castleBoundaryY = ResolveCastleBoundaryY(topRect, footerHeight);

            _monsterRegion = Rect.MinMaxRect(
                topRect.xMin + sidePadding,
                castleBoundaryY,
                topRect.xMax - sidePadding,
                topRect.yMax - headerHeight);

            LayoutCanvasUi();
            LayoutHudButtons(topRect);
            LayoutAcquiredSkillIcons(topRect);
            LayoutSkillCards(_monsterRegion);

            Rect boardRect = Rect.MinMaxRect(bottomRect.xMin, bottomRect.yMin, bottomRect.xMax, bottomRect.yMax);
            _boardController.Configure(_boardProfile, boardRect, _camera);
            _monsterLaneController.Configure(_monsterWaveProfile, _monsterRegion);
        }

        private void ResolveBattlefieldSceneRenderers()
        {
            _topBattlefieldArt = null;
            _wallDeco = null;
            SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            SpriteRenderer backgroundSource = FindNamedRenderer(renderers, "BackGround");
            SpriteRenderer castleSource = FindNamedRenderer(renderers, "CastleArt");

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                string rendererName = renderer.name.ToLowerInvariant();

                if (backgroundSource == null &&
                    (rendererName == "background" ||
                     rendererName == "bg" ||
                     rendererName == "topbattlefieldart" ||
                     rendererName == "top bg" ||
                     rendererName.Contains("background")))
                {
                    backgroundSource = renderer;
                    continue;
                }

                if (castleSource == null &&
                    (rendererName == "castleart" ||
                     rendererName == "walldeco" ||
                     rendererName.Contains("castle") ||
                     rendererName.Contains("wall")))
                {
                    castleSource = renderer;
                }
            }

            if (backgroundSource != null && backgroundSource.sprite != null)
            {
                _topBattlefieldArt = backgroundSource;
            }

            if (castleSource != null && castleSource.sprite != null)
            {
                _wallDeco = castleSource;
            }
        }

        private static SpriteRenderer FindNamedRenderer(SpriteRenderer[] renderers, string targetName)
        {
            if (renderers == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer != null &&
                    renderer.sprite != null &&
                    string.Equals(renderer.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return renderer;
                }
            }

            return null;
        }

        private void ShowSkillSelectionForCurrentRound()
        {
            _presentedChoices.Clear();

            List<PuzzleBattleSkillDefinition> pool = new List<PuzzleBattleSkillDefinition>();

            for (int i = 0; i < _skillDefinitions.Count; i++)
            {
                PuzzleBattleSkillDefinition skill = _skillDefinitions[i];

                if (skill != null && GetSkillLevel(skill.SkillId) < skill.MaxLevel)
                {
                    pool.Add(skill);
                }
            }

            Shuffle(pool);

            for (int i = 0; i < Mathf.Min(3, pool.Count); i++)
            {
                _presentedChoices.Add(pool[i]);
            }

            if (_presentedChoices.Count == 0)
            {
                _skillSelectionActive = false;
                _defaultComboText = "All skills are maxed. Continue the run.";
                BeginRound();
                return;
            }

            _skillSelectionActive = true;
            _battleActive = false;
            _boardController.SetInputEnabled(false);
            _monsterLaneController.SetBattleActive(false);
            LayoutCanvasUi();
            Canvas.ForceUpdateCanvases();
            LayoutSkillCards(_monsterRegion);
            SetSkillCardsVisible(true);
            _defaultComboText = "Pick one card. Existing skills level up to Lv.5.";
        }

        private IEnumerator ShowInitialSkillSelectionRoutine()
        {
            yield return null;
            LayoutScene();
            ShowSkillSelectionForCurrentRound();
        }

        private void BeginRound()
        {
            if (_gameOver)
            {
                return;
            }

            _skillSelectionActive = false;
            _battleActive = true;
            _blueOrbsClearedThisRound = 0;
            SetSkillCardsVisible(false);
            _boardController.SetInputEnabled(true);
            _monsterLaneController.StartRound(_currentRound);

            if (!_monsterLaneController.IsBattleActive || _skillSelectionActive || _gameOver)
            {
                return;
            }

            StartPlayerTurnTimer();
            _statusText = $"Round {_currentRound} started.";
            _defaultComboText = "Each player move advances monsters by one tile.";
        }

        private void HandleSkillSelectionClick()
        {
        }

        private void OnSkillCardPressed(int index)
        {
            if (!_skillSelectionActive || index < 0 || index >= _skillCards.Count)
            {
                return;
            }

            SkillChoiceCard card = _skillCards[index];

            if (card.Skill != null)
            {
                SelectSkill(card.Skill.SkillId);
            }
        }

        private void SelectSkill(PuzzleBattleSkillId skillId)
        {
            int level = GetSkillLevel(skillId);
            PuzzleBattleSkillDefinition skill = GetSkillDefinition(skillId);

            if (skill == null || level >= skill.MaxLevel)
            {
                return;
            }

            _skillLevels[skillId] = level + 1;
            _statusText = $"Selected {skill.DisplayName} Lv.{level + 1}.";
            BeginRound();
        }

        private int GetSkillLevel(PuzzleBattleSkillId skillId)
        {
            return _skillLevels.TryGetValue(skillId, out int level) ? level : 0;
        }

        private PuzzleBattleSkillDefinition GetSkillDefinition(PuzzleBattleSkillId skillId)
        {
            return GetSkillDefinition<PuzzleBattleSkillDefinition>(skillId);
        }

        private T GetSkillDefinition<T>(PuzzleBattleSkillId skillId) where T : PuzzleBattleSkillDefinition
        {
            for (int i = 0; i < _skillDefinitions.Count; i++)
            {
                if (_skillDefinitions[i] != null && _skillDefinitions[i].SkillId == skillId)
                {
                    return _skillDefinitions[i] as T;
                }
            }

            return null;
        }

        private void UpdateHud()
        {
            _roundLabel.text = _skillSelectionActive
                ? $"Wave {_currentRound}  |  Skill Select"
                : $"Wave {_currentRound}";

            _timerLabel.text = _turnTimerActive
                ? $"Time {Mathf.Max(0f, _turnTimeRemaining):0.0}s"
                : _skillSelectionActive
                    ? "Time Ready"
                    : _settingsOpen
                        ? "Time Paused"
                        : _gameOver
                            ? "Time Ended"
                            : "Time Waiting";

            _statusLabel.text = $"{_statusText}  |  Remain {_monsterLaneController.RemainingMonsterCount}  |  Active {_monsterLaneController.ActiveMonsterCount}  |  Spawn {_monsterLaneController.WaveTurnsRemaining}";
            _coinLabel.text = $"\uCF54\uC778 {_coins}";
            _skillsLabel.text = $"Status  ATK +{_playerAttackBonus}  |  HP {_playerCurrentHealth}/{_playerMaxHealth}";
            _playerHealthLabel.text = $"HP {_playerCurrentHealth}/{_playerMaxHealth}";
            UpdateBarFill(_turnTimerBarFill.rectTransform, _turnTimerActive ? _turnTimeRemaining / PlayerTurnDurationSeconds : 0f);
            UpdateBarFill(_playerHealthBarFill.rectTransform, _playerCurrentHealth / (float)Mathf.Max(1, _playerMaxHealth));
            UpdateAcquiredSkillIcons();
            UpdateHudButtons();

            if (_comboRoutine == null)
            {
                _comboLabel.text = _defaultComboText;
            }
        }

        private void UpdateTurnTimer()
        {
            if (!_turnTimerActive || !_battleActive || _gameOver || _skillSelectionActive || _settingsOpen)
            {
                return;
            }

            _turnTimeRemaining = Mathf.Max(0f, _turnTimeRemaining - Time.deltaTime);

            if (_turnTimeRemaining > 0f)
            {
                return;
            }

            HandleTurnTimeout();
        }

        private void StartPlayerTurnTimer()
        {
            _turnTimeRemaining = PlayerTurnDurationSeconds;
            _turnTimerActive = true;
        }

        private void StopPlayerTurnTimer()
        {
            _turnTimerActive = false;
        }

        private void HandleTurnTimeout()
        {
            if (!_battleActive || _gameOver || _skillSelectionActive || _settingsOpen || _turnAdvanceRoutine != null)
            {
                return;
            }

            StopPlayerTurnTimer();
            _statusText = $"Round {_currentRound}: time over, monsters advanced.";
            _defaultComboText = "Turn skipped.";
            _boardController.SetInputEnabled(false);
            _turnAdvanceRoutine = StartCoroutine(AdvanceMonsterTurnRoutine());
        }

        private void OnPlayerDamaged(int damage, Vector3 worldPosition)
        {
            if (_gameOver)
            {
                return;
            }

            _playerCurrentHealth = Mathf.Max(0, _playerCurrentHealth - Mathf.Max(0, damage));
            _statusText = $"Player took {damage} damage.";

            if (_playerCurrentHealth > 0)
            {
                return;
            }

            StopPlayerTurnTimer();
            _battleActive = false;
            _gameOver = true;
            _skillSelectionActive = false;
            _boardController.SetInputEnabled(false);
            _monsterLaneController.SetBattleActive(false);
            _monsterLaneController.ClearTransientEffects();
            SetSkillCardsVisible(false);
            _statusText = "Player HP reached 0.";
            _defaultComboText = "Run ended.";
        }

        private void SpawnCoinDrops(Vector3 worldPosition, int totalValue)
        {
            if (_uiRoot == null || totalValue <= 0)
            {
                _coins += Mathf.Max(0, totalValue);
                return;
            }

            int visualCount = Mathf.Clamp(totalValue, 1, 8);
            int remainingValue = totalValue;

            for (int i = 0; i < visualCount; i++)
            {
                int slotsLeft = visualCount - i;
                int coinValue = Mathf.Max(1, Mathf.CeilToInt(remainingValue / (float)slotsLeft));
                remainingValue -= coinValue;

                CoinPickupVisual coinVisual = GetCoinPickupVisualFromPool();
                RectTransform root = coinVisual.Root;
                Image icon = coinVisual.Icon;
                root.name = $"CoinPickup_{_coinPickups.Count}";
                icon.color = new Color(1f, 0.83f, 0.18f, 0.96f);
                SetRectTransform(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

                Vector2 startLocal = WorldToCanvasLocal(worldPosition + new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0.02f, 0.08f), 0f));
                Vector2 endLocal = startLocal + new Vector2(Random.Range(-68f, 68f), Random.Range(-96f, -48f));
                Vector2 controlLocal = ((startLocal + endLocal) * 0.5f) + new Vector2(Random.Range(-24f, 24f), Random.Range(54f, 102f));

                root.anchoredPosition = startLocal;
                root.localScale = Vector3.one;
                coinVisual.Value = coinValue;
                coinVisual.IsHealthPickup = false;
                coinVisual.State = CoinPickupState.Dropping;
                coinVisual.StartLocal = startLocal;
                coinVisual.ControlLocal = controlLocal;
                coinVisual.EndLocal = endLocal;
                coinVisual.Progress = 0f;
                coinVisual.Duration = Random.Range(0.32f, 0.46f);
                coinVisual.ReadyAt = 0f;
                _coinPickups.Add(coinVisual);
            }
        }

        private void SpawnHealthPickup(Vector3 worldPosition, int healAmount)
        {
            if (_uiRoot == null || healAmount <= 0)
            {
                _playerCurrentHealth = Mathf.Min(_playerMaxHealth, _playerCurrentHealth + Mathf.Max(0, healAmount));
                return;
            }

            CoinPickupVisual coinVisual = GetCoinPickupVisualFromPool();
            RectTransform root = coinVisual.Root;
            Image icon = coinVisual.Icon;
            root.name = $"HealthPickup_{_coinPickups.Count}";
            icon.color = new Color(0.32f, 0.96f, 0.52f, 0.98f);
            SetRectTransform(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));

            Vector2 startLocal = WorldToCanvasLocal(worldPosition + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(0.04f, 0.12f), 0f));
            Vector2 endLocal = startLocal + new Vector2(Random.Range(-48f, 48f), Random.Range(-82f, -42f));
            Vector2 controlLocal = ((startLocal + endLocal) * 0.5f) + new Vector2(Random.Range(-16f, 16f), Random.Range(48f, 90f));

            root.anchoredPosition = startLocal;
            root.localScale = Vector3.one;
            coinVisual.Value = healAmount;
            coinVisual.IsHealthPickup = true;
            coinVisual.State = CoinPickupState.Dropping;
            coinVisual.StartLocal = startLocal;
            coinVisual.ControlLocal = controlLocal;
            coinVisual.EndLocal = endLocal;
            coinVisual.Progress = 0f;
            coinVisual.Duration = Random.Range(0.32f, 0.46f);
            coinVisual.ReadyAt = 0f;
            _coinPickups.Add(coinVisual);
        }

        private void UpdateCoinPickups()
        {
            for (int i = _coinPickups.Count - 1; i >= 0; i--)
            {
                CoinPickupVisual coin = _coinPickups[i];

                if (coin == null || coin.Root == null)
                {
                    _coinPickups.RemoveAt(i);
                    continue;
                }

                switch (coin.State)
                {
                    case CoinPickupState.Dropping:
                        coin.Progress += Time.deltaTime / Mathf.Max(0.05f, coin.Duration);
                        float dropT = Mathf.Clamp01(coin.Progress);
                        float easedDrop = 1f - Mathf.Pow(1f - dropT, 2.2f);
                        coin.Root.anchoredPosition = EvaluateQuadraticBezier(coin.StartLocal, coin.ControlLocal, coin.EndLocal, easedDrop);

                        if (dropT >= 1f)
                        {
                            coin.State = CoinPickupState.Grounded;
                            coin.Progress = 0f;
                            coin.ReadyAt = Time.time + Random.Range(0.12f, 0.28f);
                            coin.Root.anchoredPosition = coin.EndLocal;
                        }
                        break;

                    case CoinPickupState.Grounded:
                        coin.Root.anchoredPosition = coin.EndLocal;

                        if (Time.time >= coin.ReadyAt && CanCollectDroppedCoins())
                        {
                            coin.State = CoinPickupState.Collecting;
                            coin.Progress = 0f;
                            coin.Duration = Random.Range(0.34f, 0.48f);
                            coin.StartLocal = coin.Root.anchoredPosition;
                            coin.EndLocal = coin.IsHealthPickup ? GetHealthHudTargetLocal() : GetCoinHudTargetLocal();
                            coin.ControlLocal = ((coin.StartLocal + coin.EndLocal) * 0.5f) + new Vector2(Random.Range(-56f, 56f), Random.Range(78f, 132f));
                        }
                        break;

                    case CoinPickupState.Collecting:
                        coin.Progress += Time.deltaTime / Mathf.Max(0.05f, coin.Duration);
                        float collectT = Mathf.Clamp01(coin.Progress);
                        float easedCollect = 1f - Mathf.Pow(1f - collectT, 3f);
                        coin.Root.anchoredPosition = EvaluateQuadraticBezier(coin.StartLocal, coin.ControlLocal, coin.EndLocal, easedCollect);
                        float scale = Mathf.Lerp(1f, 0.62f, collectT);
                        coin.Root.localScale = Vector3.one * scale;

                        if (collectT >= 1f)
                        {
                            if (coin.IsHealthPickup)
                            {
                                _playerCurrentHealth = Mathf.Min(_playerMaxHealth, _playerCurrentHealth + coin.Value);
                            }
                            else
                            {
                                _coins += coin.Value;
                                PulseCoinHud();
                            }

                            ReleaseCoinPickupVisual(coin);
                            _coinPickups.RemoveAt(i);
                        }
                        break;
                }
            }
        }

        private CoinPickupVisual GetCoinPickupVisualFromPool()
        {
            if (_coinPickupPool == null)
            {
                _coinPickupPool = new SimplePool<CoinPickupVisual>(
                    () =>
                    {
                        RectTransform root = CreateUiRect(_uiRoot, "CoinPickup");
                        Image icon = CreateUiImage(root, "Icon", ProceduralSpriteLibrary.GetSoftCircleSprite(), Color.white);
                        StretchRect(icon.rectTransform);
                        root.gameObject.SetActive(false);
                        return new CoinPickupVisual
                        {
                            Root = root,
                            Icon = icon
                        };
                    },
                    visual =>
                    {
                        visual.Root.SetParent(_uiRoot, false);
                        visual.Root.gameObject.SetActive(true);
                        visual.Root.localScale = Vector3.one;
                    },
                    visual =>
                    {
                        visual.Root.gameObject.SetActive(false);
                    });
            }

            return _coinPickupPool.Get();
        }

        private void ReleaseCoinPickupVisual(CoinPickupVisual coin)
        {
            if (coin == null)
            {
                return;
            }

            if (_coinPickupPool == null)
            {
                coin.Root.gameObject.SetActive(false);
                return;
            }

            _coinPickupPool.Release(coin);
        }

        private bool CanCollectDroppedCoins()
        {
            return _monsterLaneController == null || _monsterLaneController.ActiveProjectileCount == 0;
        }

        private Vector2 WorldToCanvasLocal(Vector3 worldPosition)
        {
            if (_uiRoot == null || _camera == null)
            {
                return Vector2.zero;
            }

            Vector2 screenPoint = _camera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRoot, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private Vector2 GetCoinHudTargetLocal()
        {
            if (_uiRoot == null || _coinHudIcon == null)
            {
                return Vector2.zero;
            }

            Vector3 worldCenter = _coinHudIcon.rectTransform.TransformPoint(_coinHudIcon.rectTransform.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRoot, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private Vector2 GetHealthHudTargetLocal()
        {
            if (_uiRoot == null || _playerHealthRoot == null)
            {
                return Vector2.zero;
            }

            Vector3 worldCenter = _playerHealthRoot.TransformPoint(_playerHealthRoot.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRoot, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void PulseCoinHud()
        {
            if (_coinHudRoot == null)
            {
                return;
            }

            if (_coinHudPulseRoutine != null)
            {
                StopCoroutine(_coinHudPulseRoutine);
            }

            _coinHudPulseRoutine = StartCoroutine(PulseCoinHudRoutine());
        }

        private IEnumerator PulseCoinHudRoutine()
        {
            float elapsed = 0f;
            float duration = 0.18f;
            Vector3 startScale = Vector3.one;
            Vector3 peakScale = Vector3.one * 1.12f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress < 0.5f
                    ? Mathf.SmoothStep(0f, 1f, progress / 0.5f)
                    : Mathf.SmoothStep(1f, 0f, (progress - 0.5f) / 0.5f);
                _coinHudRoot.localScale = Vector3.LerpUnclamped(startScale, peakScale, eased);
                yield return null;
            }

            _coinHudRoot.localScale = Vector3.one;
            _coinHudPulseRoutine = null;
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            float inverseT = 1f - t;
            return (inverseT * inverseT * start) + (2f * inverseT * t * control) + (t * t * end);
        }

        private string BuildSkillsLabel()
        {
            List<string> parts = new List<string>();

            for (int i = 0; i < _skillDefinitions.Count; i++)
            {
                PuzzleBattleSkillDefinition skill = _skillDefinitions[i];

                if (skill == null)
                {
                    continue;
                }

                int level = GetSkillLevel(skill.SkillId);

                if (level > 0)
                {
                    parts.Add($"{skill.ShortName} Lv.{level}");
                }
            }

            return parts.Count == 0
                ? "Skills: None selected"
                : $"Skills: {string.Join("  |  ", parts)}";
        }

        private bool HandleHudButtonClick()
        {
            return false;
        }

        private void OnHudButtonPressed(string id)
        {
            if (id == "settings")
            {
                ToggleSettings();
            }
            else if (id == "quit")
            {
                ExitRun();
            }
        }

        private void ToggleSettings()
        {
            _settingsOpen = !_settingsOpen;

            if (_settingsOpen)
            {
                _boardController.SetInputEnabled(false);
                _monsterLaneController.SetBattleActive(false);
                _statusText = "Settings opened.";
                return;
            }

            _statusText = _battleActive ? $"Wave {_currentRound} resumed." : _statusText;

            if (_battleActive && !_skillSelectionActive && !_gameOver)
            {
                _monsterLaneController.SetBattleActive(true);
                _boardController.SetInputEnabled(true);
            }
        }

        private void ExitRun()
        {
            _battleActive = false;
            _gameOver = true;
            _skillSelectionActive = false;
            _settingsOpen = false;
            _boardController.SetInputEnabled(false);
            _monsterLaneController.SetBattleActive(false);
            _statusText = "Run ended by player.";
            _defaultComboText = "Run ended.";

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResolveSkillIcon(PuzzleBattleSkillDefinition skill, out Sprite sprite, out Color tint)
        {
            GameObject effectPrefab = GetSkillEffectPrefab(skill);

            if (effectPrefab != null)
            {
                SpriteRenderer renderer = effectPrefab.GetComponentInChildren<SpriteRenderer>(true);

                if (renderer != null && renderer.sprite != null)
                {
                    sprite = renderer.sprite;
                    tint = renderer.color;
                    return;
                }
            }

            GetSkillFallbackVisual(skill, out sprite, out tint);
        }

        private GameObject GetSkillEffectPrefab(PuzzleBattleSkillDefinition skill)
        {
            switch (skill)
            {
                case OrbVolleySkillDefinition orbVolley:
                    return orbVolley.ProjectileEffectPrefab;
                case EarthquakeSkillDefinition earthquake:
                    return earthquake.EffectPrefab;
                case FrostWellSkillDefinition frostWell:
                    return frostWell.WellEffectPrefab;
                case IceOrbSkillDefinition iceOrb:
                    return iceOrb.EffectPrefab;
                case FlameCurtainSkillDefinition flameCurtain:
                    return flameCurtain.CurtainEffectPrefab;
                case TrapMineSkillDefinition trapMine:
                    return trapMine.EffectPrefab;
                case BatSwarmSkillDefinition batSwarm:
                    return batSwarm.BatEffectPrefab;
                case PoisonNeedleSkillDefinition poisonNeedles:
                    return poisonNeedles.EffectPrefab;
                case LightningStrikeSkillDefinition lightning:
                    return lightning.LightningEffectPrefab;
                case ChainLightningSkillDefinition chainLightning:
                    return chainLightning.LightningEffectPrefab;
                case SolarBeaconSkillDefinition solarBeacon:
                    return solarBeacon.EffectPrefab;
                case CharmingHeartSkillDefinition charm:
                    return charm.CharmEffectPrefab;
                case DeathBeamSkillDefinition deathBeam:
                    return deathBeam.EffectPrefab;
                case DeathBombSkillDefinition deathBomb:
                    return deathBomb.EffectPrefab;
                default:
                    return null;
            }
        }

        private void GetSkillFallbackVisual(PuzzleBattleSkillDefinition skill, out Sprite sprite, out Color tint)
        {
            switch (skill)
            {
                case OrbVolleySkillDefinition orbVolley:
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    tint = orbVolley.AccentColor;
                    break;
                case EarthquakeSkillDefinition earthquake:
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    tint = earthquake.AccentColor;
                    break;
                case FrostWellSkillDefinition frostWell:
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    tint = frostWell.AccentColor;
                    break;
                case IceOrbSkillDefinition iceOrb:
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    tint = iceOrb.AccentColor;
                    break;
                case FlameCurtainSkillDefinition flameCurtain:
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    tint = flameCurtain.AccentColor;
                    break;
                case TrapMineSkillDefinition trapMine:
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    tint = trapMine.AccentColor;
                    break;
                case BatSwarmSkillDefinition batSwarm:
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    tint = batSwarm.AccentColor;
                    break;
                case PoisonNeedleSkillDefinition poisonNeedles:
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    tint = poisonNeedles.AccentColor;
                    break;
                case LightningStrikeSkillDefinition lightning:
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    tint = lightning.AccentColor;
                    break;
                case ChainLightningSkillDefinition chainLightning:
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    tint = chainLightning.AccentColor;
                    break;
                case SolarBeaconSkillDefinition solarBeacon:
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    tint = solarBeacon.AccentColor;
                    break;
                case CharmingHeartSkillDefinition charm:
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    tint = charm.AccentColor;
                    break;
                case DeathBeamSkillDefinition deathBeam:
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    tint = deathBeam.AccentColor;
                    break;
                case DeathBombSkillDefinition deathBomb:
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    tint = deathBomb.AccentColor;
                    break;
                default:
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    tint = skill != null ? skill.AccentColor : Color.white;
                    break;
            }
        }

        private Color GetOrbColor(string orbId)
        {
            OrbVisualDefinition[] definitions = _boardProfile.OrbDefinitions;

            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].OrbId == orbId)
                {
                    return definitions[i].Tint;
                }
            }

            return Color.white;
        }

        private Vector3 GetProjectileOrigin(int projectileIndex)
        {
            float left = _monsterRegion.xMin + 0.8f;
            float right = _monsterRegion.xMax - 0.8f;
            float t = Mathf.Repeat((projectileIndex * 0.173f) + 0.1f, 1f);
            float x = Mathf.Lerp(left, right, t);
            float y = Mathf.Max(0.35f, _monsterRegion.yMin - 0.22f);
            return new Vector3(x, y, 0f);
        }

        private SkillChoiceCard CreateSkillChoiceCard(int index)
        {
            RectTransform root = CreateUiRect(_cardAreaRoot, $"SkillChoice_{index}");
            Image background = CreateUiImage(root, "Background", ProceduralSpriteLibrary.GetSquareSprite(), new Color(0.14f, 0.16f, 0.22f, 0.96f));
            StretchRect(background.rectTransform);

            Image accent = CreateUiImage(root, "Accent", ProceduralSpriteLibrary.GetSquareSprite(), Color.white);
            Text title = CreateUiLabel(root, "Title", 26, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
            Text description = CreateUiLabel(root, "Description", 18, FontStyle.Normal, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.82f));
            Text action = CreateUiLabel(root, "Action", 18, FontStyle.Bold, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.72f, 1f));
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => OnSkillCardPressed(index));

            return new SkillChoiceCard
            {
                Index = index,
                Root = root,
                Background = background,
                Accent = accent,
                Title = title,
                Description = description,
                ActionLabel = action,
                Button = button,
                Bounds = new Rect()
            };
        }

        private AcquiredSkillIcon CreateAcquiredSkillIcon(int index)
        {
            RectTransform root = CreateUiRect(_topUiRoot, $"AcquiredSkill_{index}");
            Image frame = CreateUiImage(root, "Frame", ProceduralSpriteLibrary.GetSquareSprite(), new Color(0.16f, 0.18f, 0.24f, 0.94f));
            StretchRect(frame.rectTransform);

            Image icon = CreateUiImage(root, "Icon", ProceduralSpriteLibrary.GetOrbSprite(), Color.white);
            Text level = CreateUiLabel(root, "Level", 15, FontStyle.Bold, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.76f, 1f));
            root.gameObject.SetActive(false);

            return new AcquiredSkillIcon
            {
                Root = root,
                Frame = frame,
                Icon = icon,
                LevelLabel = level
            };
        }

        private HudButton CreateHudButton(string id, string labelText)
        {
            RectTransform root = CreateUiRect(_topUiRoot, $"{id}_Button");
            Image background = CreateUiImage(root, "Background", ProceduralSpriteLibrary.GetSquareSprite(), new Color(0.15f, 0.18f, 0.24f, 0.96f));
            StretchRect(background.rectTransform);
            Text label = CreateUiLabel(root, "Label", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            label.text = labelText;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => OnHudButtonPressed(id));
            root.gameObject.SetActive(true);

            return new HudButton
            {
                Id = id,
                Root = root,
                Background = background,
                Label = label,
                Button = button,
                Bounds = new Rect()
            };
        }

        private void LayoutSkillCards(Rect area)
        {
            SetSkillCardsVisible(_skillSelectionActive);
            Canvas.ForceUpdateCanvases();
            bool applyRuntimeLayout = _uiDocument == null || _uiDocument.ApplyRuntimeLayout;

            int visibleCount = _skillSelectionActive
                ? Mathf.Min(_presentedChoices.Count, _skillCards.Count)
                : 0;
            float areaWidth = Mathf.Max(320f, _cardAreaRoot.rect.width);
            float areaHeight = Mathf.Max(220f, _cardAreaRoot.rect.height);
            float layoutPadding = 14f;
            float cardSpacing = 16f;

            for (int i = 0; i < _skillCards.Count; i++)
            {
                bool visible = i < visibleCount;
                _skillCards[i].Root.gameObject.SetActive(visible);

                if (!visible)
                {
                    continue;
                }

                SkillChoiceCard card = _skillCards[i];
                PuzzleBattleSkillDefinition skill = _presentedChoices[i];
                int currentLevel = GetSkillLevel(skill.SkillId);
                int nextLevel = Mathf.Min(skill.MaxLevel, currentLevel + 1);

                card.Skill = skill;
                card.Accent.color = skill.AccentColor;
                card.Title.text = skill.DisplayName;
                card.Description.text = skill.Description;
                card.ActionLabel.text = currentLevel > 0 ? $"Lv.{currentLevel} -> Lv.{nextLevel}" : "획득";

                if (!applyRuntimeLayout)
                {
                    continue;
                }

                float spacing = visibleCount > 1 ? i / (float)(visibleCount - 1) : 0.5f;
                float cardWidth = Mathf.Min(420f, (areaWidth - (layoutPadding * 2f) - (cardSpacing * Mathf.Max(0, visibleCount - 1))) / Mathf.Max(1, visibleCount));
                float cardHeight = Mathf.Min(340f, areaHeight - 18f);
                float minX = -(areaWidth * 0.5f) + layoutPadding + (cardWidth * 0.5f);
                float maxX = (areaWidth * 0.5f) - layoutPadding - (cardWidth * 0.5f);
                float centerX = visibleCount == 1 ? 0f : Mathf.Lerp(minX, maxX, spacing);
                float centerY = 0f;

                SetRectTransform(card.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(centerX, centerY), new Vector2(cardWidth, cardHeight));
                SetRectTransform(card.Accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 16f));
                SetRectTransform(card.Title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(-36f, 60f));
                SetRectTransform(card.Description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(-42f, -118f));
                SetRectTransform(card.ActionLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-36f, 34f));
            }
        }

        private void LayoutHudButtons(Rect topRect)
        {
            if (_uiDocument != null && !_uiDocument.ApplyRuntimeLayout)
            {
                return;
            }

            float buttonWidth = 128f;
            float buttonHeight = 38f;
            float spacing = 12f;
            float rightPadding = 26f;
            float topPadding = 20f;

            for (int i = 0; i < _hudButtons.Count; i++)
            {
                HudButton button = _hudButtons[i];
                float offsetX = rightPadding + (i * (buttonWidth + spacing));
                SetRectTransform(button.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-offsetX, -topPadding), new Vector2(buttonWidth, buttonHeight));
                SetRectTransform(button.Label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
        }

        private void LayoutAcquiredSkillIcons(Rect topRect)
        {
            if (_uiDocument != null && !_uiDocument.ApplyRuntimeLayout)
            {
                return;
            }

            int totalIcons = Mathf.Max(1, _skillIcons.Count);
            float availableWidth = Mathf.Max(280f, _topUiRoot.rect.width - 160f);
            float spacing = totalIcons > 1 ? Mathf.Clamp(availableWidth / (totalIcons * 10f), 8f, 14f) : 0f;
            float iconSize = Mathf.Clamp((availableWidth - (spacing * Mathf.Max(0, totalIcons - 1))) / totalIcons, 44f, 70f);
            float startX = 28f + (iconSize * 0.5f);
            float centerY = 56f;

            for (int i = 0; i < _skillIcons.Count; i++)
            {
                AcquiredSkillIcon icon = _skillIcons[i];
                float centerX = startX + (i * (iconSize + spacing));
                SetRectTransform(icon.Root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(centerX, centerY), new Vector2(iconSize, iconSize));
                SetRectTransform(icon.Icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(iconSize * 0.58f, iconSize * 0.58f));
                SetRectTransform(icon.LevelLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(0f, 18f));
            }
        }

        private void SetSkillCardsVisible(bool visible)
        {
            for (int i = 0; i < _skillCards.Count; i++)
            {
                _skillCards[i].Root.gameObject.SetActive(visible && i < _presentedChoices.Count);
            }
        }

        private void UpdateAcquiredSkillIcons()
        {
            List<PuzzleBattleSkillDefinition> ownedSkills = new List<PuzzleBattleSkillDefinition>();

            for (int i = 0; i < _skillDefinitions.Count; i++)
            {
                PuzzleBattleSkillDefinition skill = _skillDefinitions[i];

                if (skill != null && GetSkillLevel(skill.SkillId) > 0)
                {
                    ownedSkills.Add(skill);
                }
            }

            for (int i = 0; i < _skillIcons.Count; i++)
            {
                bool visible = i < ownedSkills.Count;
                AcquiredSkillIcon icon = _skillIcons[i];
                icon.Root.gameObject.SetActive(visible);

                if (!visible)
                {
                    continue;
                }

                PuzzleBattleSkillDefinition skill = ownedSkills[i];
                ResolveSkillIcon(skill, out Sprite sprite, out Color tint);
                icon.Icon.sprite = sprite;
                icon.Icon.color = tint;
                icon.Frame.color = new Color(skill.AccentColor.r * 0.24f, skill.AccentColor.g * 0.24f, skill.AccentColor.b * 0.24f, 0.96f);
                icon.LevelLabel.text = $"Lv.{GetSkillLevel(skill.SkillId)}";
            }
        }

        private void UpdateHudButtons()
        {
            for (int i = 0; i < _hudButtons.Count; i++)
            {
                HudButton button = _hudButtons[i];

                if (button.Id == "settings")
                {
                    if (button.Background != null)
                    {
                        button.Background.color = _settingsOpen
                            ? new Color(0.32f, 0.45f, 0.62f, 0.96f)
                            : new Color(0.15f, 0.18f, 0.24f, 0.96f);
                    }

                    button.Label.text = _settingsOpen ? "계속" : "설정";
                }
                else
                {
                    if (button.Background != null)
                    {
                        button.Background.color = new Color(0.34f, 0.18f, 0.18f, 0.96f);
                    }

                    button.Label.text = "종료";
                }
            }
        }

        private void LoadSkillDefinitions()
        {
            _skillDefinitions.Clear();
            PuzzleBattleSkillDefinition[] loaded = Resources.LoadAll<PuzzleBattleSkillDefinition>("PuzzleBattle/SkillDefinitions");
            HashSet<PuzzleBattleSkillId> loadedIds = new HashSet<PuzzleBattleSkillId>();

            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && IsSkillAvailable(loaded[i].SkillId))
                {
                    _skillDefinitions.Add(loaded[i]);
                    loadedIds.Add(loaded[i].SkillId);
                }
            }

            PuzzleBattleSkillId[] allSkillIds =
            {
                PuzzleBattleSkillId.OrbVolley,
                PuzzleBattleSkillId.Earthquake,
                PuzzleBattleSkillId.FrostWell,
                PuzzleBattleSkillId.IceOrb,
                PuzzleBattleSkillId.FlameCurtain,
                PuzzleBattleSkillId.TrapMine,
                PuzzleBattleSkillId.BatSwarm,
                PuzzleBattleSkillId.PoisonNeedles,
                PuzzleBattleSkillId.LightningStrike,
                PuzzleBattleSkillId.ChainLightning,
                PuzzleBattleSkillId.SolarBeacon,
                PuzzleBattleSkillId.CharmingHeart,
                PuzzleBattleSkillId.DeathBeam,
                PuzzleBattleSkillId.DeathBomb
            };

            for (int i = 0; i < allSkillIds.Length; i++)
            {
                if (IsSkillAvailable(allSkillIds[i]) && !loadedIds.Contains(allSkillIds[i]))
                {
                    _skillDefinitions.Add(CreateRuntimeSkillDefinition(allSkillIds[i]));
                }
            }

            _skillDefinitions.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
        }

        private bool IsSkillAvailable(PuzzleBattleSkillId skillId)
        {
            switch (skillId)
            {
                case PuzzleBattleSkillId.CharmingHeart:
                    return BoardHasOrb("heart");
                default:
                    return true;
            }
        }

        private bool BoardHasOrb(string orbId)
        {
            if (_boardProfile == null || string.IsNullOrWhiteSpace(orbId))
            {
                return false;
            }

            OrbVisualDefinition[] definitions = _boardProfile.OrbDefinitions;

            if (definitions == null)
            {
                return false;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null &&
                    string.Equals(definitions[i].OrbId, orbId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private PuzzleBattleSkillDefinition CreateRuntimeSkillDefinition(PuzzleBattleSkillId skillId)
        {
            switch (skillId)
            {
                case PuzzleBattleSkillId.OrbVolley:
                    OrbVolleySkillDefinition orbVolley = ScriptableObject.CreateInstance<OrbVolleySkillDefinition>();
                    orbVolley.SetRuntimeDefaults();
                    return orbVolley;
                case PuzzleBattleSkillId.Earthquake:
                    EarthquakeSkillDefinition earthquake = ScriptableObject.CreateInstance<EarthquakeSkillDefinition>();
                    earthquake.SetRuntimeDefaults();
                    return earthquake;
                case PuzzleBattleSkillId.FrostWell:
                    FrostWellSkillDefinition frostWell = ScriptableObject.CreateInstance<FrostWellSkillDefinition>();
                    frostWell.SetRuntimeDefaults();
                    return frostWell;
                case PuzzleBattleSkillId.IceOrb:
                    IceOrbSkillDefinition iceOrb = ScriptableObject.CreateInstance<IceOrbSkillDefinition>();
                    iceOrb.SetRuntimeDefaults();
                    return iceOrb;
                case PuzzleBattleSkillId.FlameCurtain:
                    FlameCurtainSkillDefinition flameCurtain = ScriptableObject.CreateInstance<FlameCurtainSkillDefinition>();
                    flameCurtain.SetRuntimeDefaults();
                    return flameCurtain;
                case PuzzleBattleSkillId.TrapMine:
                    TrapMineSkillDefinition trapMine = ScriptableObject.CreateInstance<TrapMineSkillDefinition>();
                    trapMine.SetRuntimeDefaults();
                    return trapMine;
                case PuzzleBattleSkillId.BatSwarm:
                    BatSwarmSkillDefinition batSwarm = ScriptableObject.CreateInstance<BatSwarmSkillDefinition>();
                    batSwarm.SetRuntimeDefaults();
                    return batSwarm;
                case PuzzleBattleSkillId.PoisonNeedles:
                    PoisonNeedleSkillDefinition poisonNeedles = ScriptableObject.CreateInstance<PoisonNeedleSkillDefinition>();
                    poisonNeedles.SetRuntimeDefaults();
                    return poisonNeedles;
                case PuzzleBattleSkillId.LightningStrike:
                    LightningStrikeSkillDefinition lightning = ScriptableObject.CreateInstance<LightningStrikeSkillDefinition>();
                    lightning.SetRuntimeDefaults();
                    return lightning;
                case PuzzleBattleSkillId.ChainLightning:
                    ChainLightningSkillDefinition chainLightning = ScriptableObject.CreateInstance<ChainLightningSkillDefinition>();
                    chainLightning.SetRuntimeDefaults();
                    return chainLightning;
                case PuzzleBattleSkillId.SolarBeacon:
                    SolarBeaconSkillDefinition solarBeacon = ScriptableObject.CreateInstance<SolarBeaconSkillDefinition>();
                    solarBeacon.SetRuntimeDefaults();
                    return solarBeacon;
                case PuzzleBattleSkillId.CharmingHeart:
                    CharmingHeartSkillDefinition charm = ScriptableObject.CreateInstance<CharmingHeartSkillDefinition>();
                    charm.SetRuntimeDefaults();
                    return charm;
                case PuzzleBattleSkillId.DeathBeam:
                    DeathBeamSkillDefinition deathBeam = ScriptableObject.CreateInstance<DeathBeamSkillDefinition>();
                    deathBeam.SetRuntimeDefaults();
                    return deathBeam;
                case PuzzleBattleSkillId.DeathBomb:
                    DeathBombSkillDefinition deathBomb = ScriptableObject.CreateInstance<DeathBombSkillDefinition>();
                    deathBomb.SetRuntimeDefaults();
                    return deathBomb;
                default:
                    return null;
            }
        }

        private void ApplyDeathTriggeredSkills(Vector3 worldPosition)
        {
            if (_monsterLaneController == null)
            {
                return;
            }

            DeathBeamSkillDefinition deathBeam = GetSkillDefinition<DeathBeamSkillDefinition>(PuzzleBattleSkillId.DeathBeam);
            int deathBeamLevel = GetSkillLevel(PuzzleBattleSkillId.DeathBeam);

            if (deathBeam != null && deathBeamLevel > 0)
            {
                _monsterLaneController.ApplyRowDamageAtWorldPosition(
                    worldPosition,
                    deathBeam.GetDamage(deathBeamLevel),
                    deathBeam.AccentColor,
                    deathBeam.EffectPrefab,
                    deathBeam.FallbackScale,
                    deathBeam.EffectLifetime);
            }

            DeathBombSkillDefinition deathBomb = GetSkillDefinition<DeathBombSkillDefinition>(PuzzleBattleSkillId.DeathBomb);
            int deathBombLevel = GetSkillLevel(PuzzleBattleSkillId.DeathBomb);

            if (deathBomb != null && deathBombLevel > 0)
            {
                _monsterLaneController.ApplyAreaDamageAtWorldPosition(
                    worldPosition,
                    deathBomb.GetRadius(deathBombLevel),
                    deathBomb.GetDamage(deathBombLevel),
                    deathBomb.AccentColor,
                    deathBomb.EffectPrefab,
                    deathBomb.FallbackScale,
                    deathBomb.EffectLifetime);
            }
        }

        private Camera EnsureCamera()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.06f, 0.06f, 0.09f);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 9f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            return mainCamera;
        }

        private PuzzleBattleBoardProfile LoadBoardProfile()
        {
            PuzzleBattleBoardProfile profile = Resources.Load<PuzzleBattleBoardProfile>("PuzzleBattle/BoardProfile");

            if (profile != null)
            {
                return profile;
            }

            OrbMotionProfile motionProfile = Resources.Load<OrbMotionProfile>("PuzzleBattle/OrbMotionProfile");

            if (motionProfile == null)
            {
                motionProfile = ScriptableObject.CreateInstance<OrbMotionProfile>();
                motionProfile.SetRuntimeDefaults();
            }

            OrbVisualDefinition[] definitions = CreateRuntimeOrbDefinitions();
            profile = ScriptableObject.CreateInstance<PuzzleBattleBoardProfile>();
            profile.SetRuntimeDefaults(6, 5, 0.6f, definitions, motionProfile);
            return profile;
        }

        private MonsterWaveProfile LoadMonsterWaveProfile()
        {
            MonsterWaveProfile profile = Resources.Load<MonsterWaveProfile>("PuzzleBattle/MonsterWaveProfile");

            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<MonsterWaveProfile>();
            profile.SetRuntimeDefaults();
            return profile;
        }

        private PlayerStatusProfile LoadPlayerStatusProfile()
        {
            PlayerStatusProfile profile = Resources.Load<PlayerStatusProfile>("PuzzleBattle/PlayerStatusProfile");

            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<PlayerStatusProfile>();
            profile.SetRuntimeDefaults();
            return profile;
        }

        private OrbVisualDefinition[] CreateRuntimeOrbDefinitions()
        {
            PuzzleBattleOrbDefaults.OrbSeed[] seeds = PuzzleBattleOrbDefaults.Seeds;
            OrbVisualDefinition[] definitions = new OrbVisualDefinition[seeds.Length];

            for (int i = 0; i < seeds.Length; i++)
            {
                definitions[i] = CreateRuntimeOrb(seeds[i].Id, seeds[i].Tint, seeds[i].Damage);
            }

            return definitions;
        }

        private OrbVisualDefinition CreateRuntimeOrb(string id, Color tint, int damage)
        {
            OrbVisualDefinition definition = ScriptableObject.CreateInstance<OrbVisualDefinition>();
            definition.SetRuntimeDefaults(id, tint, damage);
            return definition;
        }

        private SpriteRenderer CreatePanel(string objectName, Color color, int sortingOrder)
        {
            GameObject panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = panelObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralSpriteLibrary.GetSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private SpriteRenderer CreateSpriteLayer(string objectName, Sprite sprite, int sortingOrder)
        {
            GameObject layerObject = new GameObject(objectName);
            layerObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = sprite != null;
            return renderer;
        }

        private void LayoutPanel(SpriteRenderer renderer, Rect rect)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = rect.center;
            SetRendererSizeExact(renderer, rect.width, rect.height);
        }

        private Rect ResolveBattlefieldRect(Rect fallbackRect)
        {
            if (_topBattlefieldArt == null)
            {
                return fallbackRect;
            }

            _topBattlefieldArt.enabled = _topBattlefieldArt.sprite != null;

            if (!_topBattlefieldArt.enabled)
            {
                return fallbackRect;
            }

            _topBattlefieldArt.color = Color.white;
            _topBattlefieldArt.sortingOrder = 0;
            _topBattlefieldArt.transform.position = fallbackRect.center;
            SetRendererCoverRect(_topBattlefieldArt, fallbackRect.width, fallbackRect.height);
            return fallbackRect;
        }

        private float ResolveCastleBoundaryY(Rect battlefieldRect, float fallbackInset)
        {
            float defaultBoundaryY = battlefieldRect.yMin + fallbackInset;

            if (_wallDeco == null)
            {
                return defaultBoundaryY;
            }

            _wallDeco.enabled = _wallDeco.sprite != null;

            if (!_wallDeco.enabled)
            {
                return defaultBoundaryY;
            }

            float renderedWallHeight = SetRendererWidthPreserveAspect(_wallDeco, battlefieldRect.width);
            float wallCenterY = battlefieldRect.yMin + (renderedWallHeight * 0.5f);
            _wallDeco.color = Color.white;
            _wallDeco.sortingOrder = 4;
            _wallDeco.transform.position = new Vector3(battlefieldRect.center.x, wallCenterY, 0f);
            return defaultBoundaryY;
        }

        private void CleanupLegacySceneObjects()
        {
            string[] legacyObjectNames =
            {
                "TopBackground",
                "TopBattlefieldArt",
                "WallDeco",
                "BottomBackground",
                "Divider",
                "PuzzleBattleCanvas",
                "Match3Board",
                "MonsterLane"
            };

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform sceneTransform = transforms[i];

                if (sceneTransform == null || sceneTransform == transform)
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < legacyObjectNames.Length; nameIndex++)
                {
                    if (!string.Equals(sceneTransform.name, legacyObjectNames[nameIndex], System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    sceneTransform.gameObject.SetActive(false);
                    Destroy(sceneTransform.gameObject);
                    break;
                }
            }
        }

        private static void SetRendererSizeExact(SpriteRenderer renderer, float width, float height)
        {
            if (renderer == null)
            {
                return;
            }

            Vector2 spriteSize = renderer.sprite != null
                ? new Vector2(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y)
                : Vector2.one;

            float safeWidth = Mathf.Max(0.0001f, spriteSize.x);
            float safeHeight = Mathf.Max(0.0001f, spriteSize.y);
            renderer.transform.localScale = new Vector3(width / safeWidth, height / safeHeight, 1f);
        }

        private static void SetRendererCoverRect(SpriteRenderer renderer, float width, float height)
        {
            if (renderer == null)
            {
                return;
            }

            Vector2 spriteSize = renderer.sprite != null
                ? new Vector2(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y)
                : Vector2.one;

            float safeWidth = Mathf.Max(0.0001f, spriteSize.x);
            float safeHeight = Mathf.Max(0.0001f, spriteSize.y);
            float scale = Mathf.Max(width / safeWidth, height / safeHeight);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static float SetRendererWidthPreserveAspect(SpriteRenderer renderer, float width)
        {
            if (renderer == null)
            {
                return 0f;
            }

            Vector2 spriteSize = renderer.sprite != null
                ? new Vector2(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y)
                : Vector2.one;

            float safeWidth = Mathf.Max(0.0001f, spriteSize.x);
            float safeHeight = Mathf.Max(0.0001f, spriteSize.y);
            float scale = width / safeWidth;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return safeHeight * scale;
        }

        private bool EnsureUiCanvas()
        {
            if (_uiCanvas != null)
            {
                return true;
            }

            EnsureEventSystem();
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (TryUseExternalCanvasHost())
            {
                return true;
            }

            PuzzleBattleUiDocument document = FindFirstObjectByType<PuzzleBattleUiDocument>();
            return TryBindUiDocument(document);
        }

        private bool TryUseExternalCanvasHost()
        {
            if (_canvasHost == null)
            {
                return false;
            }

            PuzzleBattleUiDocument document = _canvasHost.UiDocument != null
                ? _canvasHost.UiDocument
                : _canvasHost.GetComponent<PuzzleBattleUiDocument>();

            if (document == null && _canvasHost.Canvas != null)
            {
                document = _canvasHost.Canvas.GetComponent<PuzzleBattleUiDocument>();
            }

            return TryBindUiDocument(document);
        }

        private bool TryBindUiDocument(PuzzleBattleUiDocument document)
        {
            if (document == null)
            {
                return false;
            }

            Canvas canvas = document.Canvas;
            RectTransform uiRoot = document.UiRoot;
            RectTransform topUiRoot = document.TopUiRoot;
            RectTransform cardAreaRoot = document.CardAreaRoot;
            PuzzleBattleUiDocument.TurnTimerBarSlot turnTimerBar = document.TurnTimerBar;
            PuzzleBattleUiDocument.CoinHudSlot coinHud = document.CoinHud;
            PuzzleBattleUiDocument.PlayerHealthBarSlot playerHealthBar = document.PlayerHealthBar;

            if (canvas == null ||
                uiRoot == null ||
                topUiRoot == null ||
                cardAreaRoot == null ||
                document.RoundLabel == null ||
                document.StatusLabel == null ||
                document.TimerLabel == null ||
                document.SkillsLabel == null ||
                document.ComboLabel == null ||
                turnTimerBar == null ||
                turnTimerBar.Root == null ||
                turnTimerBar.Fill == null ||
                coinHud == null ||
                coinHud.Root == null ||
                coinHud.Icon == null ||
                coinHud.Label == null ||
                playerHealthBar == null ||
                playerHealthBar.Root == null ||
                playerHealthBar.Fill == null ||
                playerHealthBar.Label == null)
            {
                return false;
            }

            _uiDocument = document;
            _uiCanvas = canvas;
            _uiRoot = uiRoot;
            _topUiRoot = topUiRoot;
            _cardAreaRoot = cardAreaRoot;
            _roundLabel = document.RoundLabel;
            _statusLabel = document.StatusLabel;
            _timerLabel = document.TimerLabel;
            _skillsLabel = document.SkillsLabel;
            _comboLabel = document.ComboLabel;
            _turnTimerBarRoot = turnTimerBar.Root;
            _turnTimerBarBackground = turnTimerBar.Background;
            _turnTimerBarFill = turnTimerBar.Fill;
            _coinHudRoot = coinHud.Root;
            _coinHudIcon = coinHud.Icon;
            _coinLabel = coinHud.Label;
            _playerHealthRoot = playerHealthBar.Root;
            _playerHealthBarBackground = playerHealthBar.Background;
            _playerHealthBarFill = playerHealthBar.Fill;
            _playerHealthLabel = playerHealthBar.Label;

            _skillCards.Clear();
            PuzzleBattleUiDocument.SkillCardSlot[] cardSlots = document.SkillCards;

            if (cardSlots != null)
            {
                for (int i = 0; i < cardSlots.Length; i++)
                {
                    PuzzleBattleUiDocument.SkillCardSlot slot = cardSlots[i];

                    if (slot == null || slot.Root == null || slot.Accent == null || slot.Title == null || slot.Description == null || slot.ActionLabel == null || slot.Button == null)
                    {
                        continue;
                    }

                    if (slot.Background != null && slot.Button.targetGraphic == null)
                    {
                        slot.Button.targetGraphic = slot.Background;
                    }

                    int index = _skillCards.Count;
                    slot.Button.onClick.RemoveAllListeners();
                    slot.Button.onClick.AddListener(() => OnSkillCardPressed(index));

                    _skillCards.Add(new SkillChoiceCard
                    {
                        Index = index,
                        Root = slot.Root,
                        Background = slot.Background,
                        Accent = slot.Accent,
                        Title = slot.Title,
                        Description = slot.Description,
                        ActionLabel = slot.ActionLabel,
                        Button = slot.Button,
                        Bounds = new Rect()
                    });
                }
            }

            _skillIcons.Clear();
            PuzzleBattleUiDocument.SkillIconSlot[] iconSlots = document.SkillIcons;

            if (iconSlots != null)
            {
                for (int i = 0; i < iconSlots.Length; i++)
                {
                    PuzzleBattleUiDocument.SkillIconSlot slot = iconSlots[i];

                    if (slot == null || slot.Root == null || slot.Frame == null || slot.Icon == null || slot.LevelLabel == null)
                    {
                        continue;
                    }

                    _skillIcons.Add(new AcquiredSkillIcon
                    {
                        Root = slot.Root,
                        Frame = slot.Frame,
                        Icon = slot.Icon,
                        LevelLabel = slot.LevelLabel
                    });
                }
            }

            _hudButtons.Clear();
            PuzzleBattleUiDocument.HudButtonSlot[] hudButtonSlots = document.HudButtons;

            if (hudButtonSlots != null)
            {
                for (int i = 0; i < hudButtonSlots.Length; i++)
                {
                    PuzzleBattleUiDocument.HudButtonSlot slot = hudButtonSlots[i];

                    if (slot == null || string.IsNullOrWhiteSpace(slot.Id) || slot.Root == null || slot.Label == null || slot.Button == null)
                    {
                        continue;
                    }

                    if (slot.Background != null && slot.Button.targetGraphic == null)
                    {
                        slot.Button.targetGraphic = slot.Background;
                    }

                    string id = slot.Id;
                    slot.Button.onClick.RemoveAllListeners();
                    slot.Button.onClick.AddListener(() => OnHudButtonPressed(id));

                    _hudButtons.Add(new HudButton
                    {
                        Id = id,
                        Root = slot.Root,
                        Background = slot.Background,
                        Label = slot.Label,
                        Button = slot.Button,
                        Bounds = new Rect()
                    });
                }
            }

            if (_skillCards.Count == 0)
            {
                return false;
            }

            return true;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void LayoutCanvasUi()
        {
            if (_uiRoot == null)
            {
                return;
            }

            if (_uiDocument != null && !_uiDocument.ApplyRuntimeLayout)
            {
                Canvas.ForceUpdateCanvases();
                return;
            }

            SetAnchorStretch(_topUiRoot, new Vector2(0f, 0.5f), Vector2.one);
            SetRectTransform(_roundLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(640f, 42f));
            SetRectTransform(_timerLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -56f), new Vector2(220f, 30f));
            SetRectTransform(_turnTimerBarRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -82f), new Vector2(360f, 18f));
            SetRectTransform(_statusLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28f, -108f), new Vector2(-268f, 34f));
            SetRectTransform(_coinHudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(482f, -18f), new Vector2(220f, 42f));
            SetRectTransform(_coinHudIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(30f, 30f));
            SetRectTransform(_coinLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(170f, 36f));
            SetRectTransform(_skillsLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 44f), new Vector2(240f, 24f));
            SetRectTransform(_playerHealthRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-52f, 24f));
            SetRectTransform(_playerHealthLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetRectTransform(_comboLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(760f, 36f));

            Rect viewportRect = WorldRectToViewportRect(_monsterRegion);
            SetAnchorStretch(_cardAreaRoot, viewportRect.min, viewportRect.max);
            Canvas.ForceUpdateCanvases();
        }

        private RectTransform CreateUiRect(Transform parent, string objectName)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private Text CreateUiLabel(Transform parent, string objectName, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateUiRect(parent, objectName);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _uiFont;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateUiImage(Transform parent, string objectName, Sprite sprite, Color color)
        {
            RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = false;
            return image;
        }

        private static void StretchRect(RectTransform rectTransform)
        {
            SetAnchorStretch(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void SetAnchorStretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRectTransform(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void UpdateBarFill(RectTransform fillRect, float normalized)
        {
            if (fillRect == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalized);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(clamped, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private Rect WorldRectToViewportRect(Rect worldRect)
        {
            Vector3 bottomLeft = _camera.WorldToViewportPoint(new Vector3(worldRect.xMin, worldRect.yMin, 0f));
            Vector3 topRight = _camera.WorldToViewportPoint(new Vector3(worldRect.xMax, worldRect.yMax, 0f));
            return Rect.MinMaxRect(
                Mathf.Clamp01(bottomLeft.x),
                Mathf.Clamp01(bottomLeft.y),
                Mathf.Clamp01(topRight.x),
                Mathf.Clamp01(topRight.y));
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
