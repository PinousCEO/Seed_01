using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleBattle.Editor
{
    [InitializeOnLoad]
    public static class OrbAssetAuthoringUtility
    {
        private const string ResourcesRoot = "Assets/Resources/PuzzleBattle";
        private const string OrbDefinitionsFolder = ResourcesRoot + "/OrbDefinitions";
        private const string SkillDefinitionsFolder = ResourcesRoot + "/SkillDefinitions";
        private const string PrefabsRoot = "Assets/01_Prefabs/PuzzleBattle";
        private const string OrbPrefabsFolder = PrefabsRoot + "/Orbs";
        private const string ProjectilePrefabsFolder = PrefabsRoot + "/Projectiles";
        private const string SkillEffectPrefabsFolder = PrefabsRoot + "/SkillEffects";
        private const string CatalogPath = ResourcesRoot + "/OrbPrefabCatalog.asset";
        private const string BoardProfilePath = ResourcesRoot + "/BoardProfile.asset";
        private const string MotionProfilePath = ResourcesRoot + "/OrbMotionProfile.asset";
        private const string MonsterWavePath = ResourcesRoot + "/MonsterWaveProfile.asset";
        private const string UiPrefabPath = ResourcesRoot + "/PuzzleBattleUI.prefab";
        private static bool _isSynchronizing;

        static OrbAssetAuthoringUtility()
        {
            EditorApplication.delayCall += EnsureDefaultAssets;
            EditorApplication.projectChanged += EnsureDefaultAssets;
        }

        [MenuItem("Tools/Puzzle Battle/Ensure Orb Assets")]
        private static void EnsureDefaultAssetsMenu()
        {
            EnsureDefaultAssets();
        }

        [MenuItem("Tools/Puzzle Battle/Rebuild Orb Assets")]
        public static void RebuildOrbAssetsMenu()
        {
            RebuildOrbAssets();
        }

        public static void RebuildOrbAssets()
        {
            SynchronizeAssets(true);
        }

        private static void EnsureDefaultAssets()
        {
            SynchronizeAssets(false);
        }

        private static void SynchronizeAssets(bool rebuildGeneratedPrefabs)
        {
            if (_isSynchronizing)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += rebuildGeneratedPrefabs ? RebuildOrbAssets : EnsureDefaultAssets;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                return;
            }

            _isSynchronizing = true;

            try
            {
                EnsureFolder("Assets/Resources");
                EnsureFolder(ResourcesRoot);
                EnsureFolder(OrbDefinitionsFolder);
                EnsureFolder(SkillDefinitionsFolder);
                EnsureFolder("Assets/01_Prefabs");
                EnsureFolder(PrefabsRoot);
                EnsureFolder(OrbPrefabsFolder);
                EnsureFolder(ProjectilePrefabsFolder);
                EnsureFolder(SkillEffectPrefabsFolder);

                OrbMotionProfile motionProfile = LoadOrCreateMotionProfile();
                LoadOrCreateMonsterWaveProfile();

                List<OrbPrefabCatalog.Entry> orbEntries = new List<OrbPrefabCatalog.Entry>(PuzzleBattleOrbDefaults.Seeds.Length);

                for (int i = 0; i < PuzzleBattleOrbDefaults.Seeds.Length; i++)
                {
                    PuzzleBattleOrbDefaults.OrbSeed seed = PuzzleBattleOrbDefaults.Seeds[i];
                    OrbVisualDefinition definition = LoadOrCreateDefinition(seed);
                    GameObject projectilePrefab = rebuildGeneratedPrefabs
                        ? RebuildOrbProjectilePrefab(seed, definition)
                        : LoadOrCreateOrbProjectilePrefab(seed, definition);
                    AssignProjectilePrefab(definition, projectilePrefab);

                    BoardPieceView orbPrefab = rebuildGeneratedPrefabs
                        ? RebuildOrbPrefab(seed, definition)
                        : LoadOrCreateOrbPrefab(seed, definition);
                    orbEntries.Add(new OrbPrefabCatalog.Entry(definition, orbPrefab));
                }

                LoadOrCreateSkillDefinitions(rebuildGeneratedPrefabs);

                OrbPrefabCatalog catalog = rebuildGeneratedPrefabs
                    ? ForceWriteCatalog(orbEntries)
                    : LoadOrCreateCatalog(orbEntries);

                if (rebuildGeneratedPrefabs)
                {
                    ForceWriteBoardProfile(catalog, motionProfile);
                }
                else
                {
                    LoadOrCreateBoardProfile(catalog, motionProfile);
                }

                AssetDatabase.SaveAssets();

                if (rebuildGeneratedPrefabs)
                {
                    AssetDatabase.Refresh();
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private static OrbMotionProfile LoadOrCreateMotionProfile()
        {
            OrbMotionProfile profile = AssetDatabase.LoadAssetAtPath<OrbMotionProfile>(MotionProfilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<OrbMotionProfile>();
                profile.SetAuthoringDefaults();
                AssetDatabase.CreateAsset(profile, MotionProfilePath);
            }

            return profile;
        }

        private static MonsterWaveProfile LoadOrCreateMonsterWaveProfile()
        {
            MonsterWaveProfile profile = AssetDatabase.LoadAssetAtPath<MonsterWaveProfile>(MonsterWavePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MonsterWaveProfile>();
                profile.SetAuthoringDefaults();
                AssetDatabase.CreateAsset(profile, MonsterWavePath);
            }

            return profile;
        }

        private static OrbVisualDefinition LoadOrCreateDefinition(PuzzleBattleOrbDefaults.OrbSeed seed)
        {
            string path = $"{OrbDefinitionsFolder}/{seed.Id}.asset";
            OrbVisualDefinition definition = AssetDatabase.LoadAssetAtPath<OrbVisualDefinition>(path);

            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<OrbVisualDefinition>();
                definition.SetAuthoringDefaults(seed.Id, seed.Tint, seed.Damage);
                AssetDatabase.CreateAsset(definition, path);
            }

            return definition;
        }

        private static GameObject LoadOrCreateOrbProjectilePrefab(PuzzleBattleOrbDefaults.OrbSeed seed, OrbVisualDefinition definition)
        {
            string path = $"{ProjectilePrefabsFolder}/{seed.Id}Projectile.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ?? CreateOrbProjectilePrefab(path, seed, definition);
        }

        private static GameObject RebuildOrbProjectilePrefab(PuzzleBattleOrbDefaults.OrbSeed seed, OrbVisualDefinition definition)
        {
            string path = $"{ProjectilePrefabsFolder}/{seed.Id}Projectile.prefab";
            return CreateOrbProjectilePrefab(path, seed, definition);
        }

        private static GameObject CreateOrbProjectilePrefab(string path, PuzzleBattleOrbDefaults.OrbSeed seed, OrbVisualDefinition definition)
        {
            return CreateEffectPrefab(path, $"{seed.DisplayName}Projectile", ProceduralSpriteLibrary.GetOrbSprite(), definition != null ? definition.Tint : seed.Tint, Vector3.one * 0.38f);
        }

        private static void AssignProjectilePrefab(OrbVisualDefinition definition, GameObject projectilePrefab)
        {
            if (definition == null || projectilePrefab == null || definition.ProjectileEffectPrefab != null)
            {
                return;
            }

            definition.SetProjectileEffectPrefab(projectilePrefab);
            EditorUtility.SetDirty(definition);
        }

        private static BoardPieceView LoadOrCreateOrbPrefab(PuzzleBattleOrbDefaults.OrbSeed seed, OrbVisualDefinition definition)
        {
            string path = $"{OrbPrefabsFolder}/{seed.Id}.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefabAsset == null)
            {
                return RebuildOrbPrefab(seed, definition);
            }

            EnsureOrbPrefabComponent(path);
            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefabAsset != null ? prefabAsset.GetComponent<BoardPieceView>() : null;
        }

        private static BoardPieceView RebuildOrbPrefab(PuzzleBattleOrbDefaults.OrbSeed seed, OrbVisualDefinition definition)
        {
            string path = $"{OrbPrefabsFolder}/{seed.Id}.prefab";
            GameObject root = new GameObject($"{seed.DisplayName}Orb");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralSpriteLibrary.GetOrbSprite();
            renderer.color = definition != null ? definition.Tint : seed.Tint;
            root.AddComponent<BoardPieceView>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefabAsset != null ? prefabAsset.GetComponent<BoardPieceView>() : null;
        }

        private static void EnsureOrbPrefabComponent(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = false;

            if (root.GetComponent<BoardPieceView>() == null)
            {
                root.AddComponent<BoardPieceView>();
                dirty = true;
            }

            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void LoadOrCreateSkillDefinitions(bool rebuildGeneratedPrefabs)
        {
            GameObject orbVolleyPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.OrbVolley)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.OrbVolley);
            GameObject earthquakePrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.Earthquake)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.Earthquake);
            GameObject frostWellPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.FrostWell)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.FrostWell);
            GameObject iceOrbPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.IceOrb)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.IceOrb);
            GameObject flameCurtainPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.FlameCurtain)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.FlameCurtain);
            GameObject trapMinePrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.TrapMine)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.TrapMine);
            GameObject batSwarmPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.BatSwarm)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.BatSwarm);
            GameObject poisonNeedlePrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.PoisonNeedles)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.PoisonNeedles);
            GameObject lightningPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.LightningStrike)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.LightningStrike);
            GameObject solarBeaconPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.SolarBeacon)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.SolarBeacon);
            GameObject charmingHeartPrefab = rebuildGeneratedPrefabs
                ? RebuildSkillEffectPrefab(PuzzleBattleSkillId.CharmingHeart)
                : LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId.CharmingHeart);

            LoadOrCreateSkillAsset<OrbVolleySkillDefinition>(
                $"{SkillDefinitionsFolder}/OrbVolley.asset",
                orbVolleyPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(orbVolleyPrefab);
                },
                skill =>
                {
                    if (skill.ProjectileEffectPrefab == null && orbVolleyPrefab != null)
                    {
                        skill.SetEffectPrefab(orbVolleyPrefab);
                    }
                });

            LoadOrCreateSkillAsset<EarthquakeSkillDefinition>(
                $"{SkillDefinitionsFolder}/Earthquake.asset",
                earthquakePrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(earthquakePrefab);
                },
                skill =>
                {
                    if (skill.EffectPrefab == null && earthquakePrefab != null)
                    {
                        skill.SetEffectPrefab(earthquakePrefab);
                    }
                });

            LoadOrCreateSkillAsset<FrostWellSkillDefinition>(
                $"{SkillDefinitionsFolder}/FrostWell.asset",
                frostWellPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(frostWellPrefab);
                },
                skill =>
                {
                    if (skill.WellEffectPrefab == null && frostWellPrefab != null)
                    {
                        skill.SetEffectPrefab(frostWellPrefab);
                    }
                });

            LoadOrCreateSkillAsset<IceOrbSkillDefinition>(
                $"{SkillDefinitionsFolder}/IceOrb.asset",
                iceOrbPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(iceOrbPrefab);
                },
                skill =>
                {
                    if (skill.EffectPrefab == null && iceOrbPrefab != null)
                    {
                        skill.SetEffectPrefab(iceOrbPrefab);
                    }
                });

            LoadOrCreateSkillAsset<FlameCurtainSkillDefinition>(
                $"{SkillDefinitionsFolder}/FlameCurtain.asset",
                flameCurtainPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(flameCurtainPrefab);
                },
                skill =>
                {
                    if (skill.CurtainEffectPrefab == null && flameCurtainPrefab != null)
                    {
                        skill.SetEffectPrefab(flameCurtainPrefab);
                    }
                });

            LoadOrCreateSkillAsset<TrapMineSkillDefinition>(
                $"{SkillDefinitionsFolder}/TrapMine.asset",
                trapMinePrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(trapMinePrefab);
                },
                skill =>
                {
                    if (skill.EffectPrefab == null && trapMinePrefab != null)
                    {
                        skill.SetEffectPrefab(trapMinePrefab);
                    }
                });

            LoadOrCreateSkillAsset<BatSwarmSkillDefinition>(
                $"{SkillDefinitionsFolder}/BatSwarm.asset",
                batSwarmPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(batSwarmPrefab);
                },
                skill =>
                {
                    if (skill.BatEffectPrefab == null && batSwarmPrefab != null)
                    {
                        skill.SetEffectPrefab(batSwarmPrefab);
                    }
                });

            LoadOrCreateSkillAsset<PoisonNeedleSkillDefinition>(
                $"{SkillDefinitionsFolder}/PoisonNeedles.asset",
                poisonNeedlePrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(poisonNeedlePrefab);
                },
                skill =>
                {
                    if (skill.EffectPrefab == null && poisonNeedlePrefab != null)
                    {
                        skill.SetEffectPrefab(poisonNeedlePrefab);
                    }
                });

            LoadOrCreateSkillAsset<LightningStrikeSkillDefinition>(
                $"{SkillDefinitionsFolder}/LightningStrike.asset",
                lightningPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(lightningPrefab);
                },
                skill =>
                {
                    if (skill.LightningEffectPrefab == null && lightningPrefab != null)
                    {
                        skill.SetEffectPrefab(lightningPrefab);
                    }
                });

            LoadOrCreateSkillAsset<SolarBeaconSkillDefinition>(
                $"{SkillDefinitionsFolder}/SolarBeacon.asset",
                solarBeaconPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(solarBeaconPrefab);
                },
                skill =>
                {
                    if (skill.EffectPrefab == null && solarBeaconPrefab != null)
                    {
                        skill.SetEffectPrefab(solarBeaconPrefab);
                    }
                });

            LoadOrCreateSkillAsset<CharmingHeartSkillDefinition>(
                $"{SkillDefinitionsFolder}/CharmingHeart.asset",
                charmingHeartPrefab,
                skill =>
                {
                    skill.SetAuthoringDefaults();
                    skill.SetEffectPrefab(charmingHeartPrefab);
                },
                skill =>
                {
                    if (skill.CharmEffectPrefab == null && charmingHeartPrefab != null)
                    {
                        skill.SetEffectPrefab(charmingHeartPrefab);
                    }
                });
        }

        private static void LoadOrCreateSkillAsset<T>(string path, GameObject effectPrefab, System.Action<T> initializeNewAsset, System.Action<T> patchExistingAsset)
            where T : PuzzleBattleSkillDefinition
        {
            PuzzleBattleSkillDefinition anyAsset = AssetDatabase.LoadAssetAtPath<PuzzleBattleSkillDefinition>(path);

            if (anyAsset != null && !(anyAsset is T))
            {
                AssetDatabase.DeleteAsset(path);
                anyAsset = null;
            }

            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                initializeNewAsset(asset);
                AssetDatabase.CreateAsset(asset, path);
                return;
            }

            patchExistingAsset(asset);
            EditorUtility.SetDirty(asset);
        }

        private static GameObject LoadOrCreateSkillEffectPrefab(PuzzleBattleSkillId skillId)
        {
            string path = GetSkillEffectPrefabPath(skillId);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ?? RebuildSkillEffectPrefab(skillId);
        }

        private static GameObject RebuildSkillEffectPrefab(PuzzleBattleSkillId skillId)
        {
            string path = GetSkillEffectPrefabPath(skillId);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            GetSkillEffectVisual(skillId, out string prefabName, out Sprite sprite, out Color color, out Vector3 scale);
            return CreateEffectPrefab(path, prefabName, sprite, color, scale);
        }

        private static string GetSkillEffectPrefabPath(PuzzleBattleSkillId skillId)
        {
            return skillId switch
            {
                PuzzleBattleSkillId.OrbVolley => $"{SkillEffectPrefabsFolder}/OrbVolley.prefab",
                PuzzleBattleSkillId.Earthquake => $"{SkillEffectPrefabsFolder}/Earthquake.prefab",
                PuzzleBattleSkillId.FrostWell => $"{SkillEffectPrefabsFolder}/FrostWell.prefab",
                PuzzleBattleSkillId.IceOrb => $"{SkillEffectPrefabsFolder}/IceOrb.prefab",
                PuzzleBattleSkillId.FlameCurtain => $"{SkillEffectPrefabsFolder}/FlameCurtain.prefab",
                PuzzleBattleSkillId.TrapMine => $"{SkillEffectPrefabsFolder}/TrapMine.prefab",
                PuzzleBattleSkillId.BatSwarm => $"{SkillEffectPrefabsFolder}/BatSwarm.prefab",
                PuzzleBattleSkillId.PoisonNeedles => $"{SkillEffectPrefabsFolder}/PoisonNeedles.prefab",
                PuzzleBattleSkillId.LightningStrike => $"{SkillEffectPrefabsFolder}/LightningStrike.prefab",
                PuzzleBattleSkillId.SolarBeacon => $"{SkillEffectPrefabsFolder}/SolarBeacon.prefab",
                PuzzleBattleSkillId.CharmingHeart => $"{SkillEffectPrefabsFolder}/CharmingHeart.prefab",
                _ => null
            };
        }

        private static void GetSkillEffectVisual(PuzzleBattleSkillId skillId, out string prefabName, out Sprite sprite, out Color color, out Vector3 scale)
        {
            switch (skillId)
            {
                case PuzzleBattleSkillId.OrbVolley:
                    prefabName = "OrbVolleyEffect";
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    color = new Color(1f, 0.82f, 0.42f, 1f);
                    scale = Vector3.one * 0.42f;
                    break;
                case PuzzleBattleSkillId.Earthquake:
                    prefabName = "EarthquakeEffect";
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    color = new Color(0.48f, 0.82f, 0.38f, 0.78f);
                    scale = Vector3.one * 0.96f;
                    break;
                case PuzzleBattleSkillId.FrostWell:
                    prefabName = "FrostWellEffect";
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    color = new Color(0.22f, 0.66f, 1f, 0.52f);
                    scale = Vector3.one;
                    break;
                case PuzzleBattleSkillId.IceOrb:
                    prefabName = "IceOrbEffect";
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    color = new Color(0.58f, 0.92f, 1f, 0.96f);
                    scale = Vector3.one * 0.46f;
                    break;
                case PuzzleBattleSkillId.FlameCurtain:
                    prefabName = "FlameCurtainEffect";
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    color = new Color(1f, 0.34f, 0.12f, 0.58f);
                    scale = new Vector3(1f, 0.28f, 1f);
                    break;
                case PuzzleBattleSkillId.TrapMine:
                    prefabName = "TrapMineEffect";
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    color = new Color(1f, 0.48f, 0.2f, 0.88f);
                    scale = Vector3.one * 0.56f;
                    break;
                case PuzzleBattleSkillId.BatSwarm:
                    prefabName = "BatSwarmEffect";
                    sprite = ProceduralSpriteLibrary.GetOrbSprite();
                    color = new Color(0.28f, 0.18f, 0.42f, 0.95f);
                    scale = Vector3.one * 0.46f;
                    break;
                case PuzzleBattleSkillId.PoisonNeedles:
                    prefabName = "PoisonNeedlesEffect";
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    color = new Color(0.42f, 0.9f, 0.42f, 0.95f);
                    scale = new Vector3(0.14f, 0.72f, 1f);
                    break;
                case PuzzleBattleSkillId.LightningStrike:
                    prefabName = "LightningStrikeEffect";
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    color = new Color(1f, 0.9f, 0.24f, 0.96f);
                    scale = new Vector3(0.18f, 1.1f, 1f);
                    break;
                case PuzzleBattleSkillId.SolarBeacon:
                    prefabName = "SolarBeaconEffect";
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    color = new Color(1f, 0.9f, 0.42f, 0.72f);
                    scale = Vector3.one * 0.82f;
                    break;
                case PuzzleBattleSkillId.CharmingHeart:
                    prefabName = "CharmingHeartEffect";
                    sprite = ProceduralSpriteLibrary.GetSoftCircleSprite();
                    color = new Color(1f, 0.5f, 0.76f, 0.92f);
                    scale = Vector3.one * 0.7f;
                    break;
                default:
                    prefabName = "SkillEffect";
                    sprite = ProceduralSpriteLibrary.GetSquareSprite();
                    color = Color.white;
                    scale = Vector3.one;
                    break;
            }
        }

        private static GameObject CreateEffectPrefab(string path, string prefabName, Sprite sprite, Color color, Vector3 scale)
        {
            GameObject root = new GameObject(prefabName);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            root.transform.localScale = scale;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject LoadOrCreateUiPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            return prefab ?? RebuildUiPrefab();
        }

        private static GameObject RebuildUiPrefab()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = new GameObject("PuzzleBattleUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PuzzleBattleUiDocument));
            RectTransform uiRoot = root.GetComponent<RectTransform>();
            StretchRect(uiRoot);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform topUiRoot = CreateUiRect(uiRoot, "TopUIRoot");
            StretchRect(topUiRoot, new Vector2(0f, 0.5f), Vector2.one);

            RectTransform cardAreaRoot = CreateUiRect(uiRoot, "SkillCardArea");
            StretchRect(cardAreaRoot);

            Text roundLabel = CreateUiText(topUiRoot, "RoundLabel", font, 34, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            Text timerLabel = CreateUiText(topUiRoot, "TimerLabel", font, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f, 0.92f));
            Text statusLabel = CreateUiText(topUiRoot, "StatusLabel", font, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.9f));
            Text skillsLabel = CreateUiText(topUiRoot, "SkillsLabel", font, 20, FontStyle.Bold, TextAnchor.LowerLeft, new Color(1f, 0.95f, 0.8f, 0.95f));
            Text comboLabel = CreateUiText(uiRoot, "ComboLabel", font, 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.82f));
            RectTransform turnTimerBarRoot = CreateUiRect(topUiRoot, "TurnTimerBar");
            Image turnTimerBarBackground = CreateUiImage(turnTimerBarRoot, "Background", new Color(0.14f, 0.18f, 0.24f, 0.92f));
            StretchRect(turnTimerBarBackground.rectTransform);
            Image turnTimerBarFill = CreateUiImage(turnTimerBarRoot, "Fill", new Color(0.38f, 0.82f, 1f, 0.96f));
            RectTransform coinHudRoot = CreateUiRect(topUiRoot, "CoinHud");
            Image coinHudIcon = CreateUiImage(coinHudRoot, "CoinIcon", new Color(1f, 0.84f, 0.22f, 0.96f));
            Text coinHudLabel = CreateUiText(coinHudRoot, "CoinLabel", font, 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.94f, 0.72f, 1f));
            RectTransform playerHealthRoot = CreateUiRect(topUiRoot, "PlayerHealthBar");
            Image playerHealthBarBackground = CreateUiImage(playerHealthRoot, "Background", new Color(0.18f, 0.09f, 0.1f, 0.94f));
            StretchRect(playerHealthBarBackground.rectTransform);
            Image playerHealthBarFill = CreateUiImage(playerHealthRoot, "Fill", new Color(0.92f, 0.28f, 0.24f, 0.96f));
            Text playerHealthLabel = CreateUiText(playerHealthRoot, "Label", font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.92f, 1f));
            SetRectTransform(roundLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(640f, 42f));
            SetRectTransform(timerLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -56f), new Vector2(840f, 30f));
            SetRectTransform(turnTimerBarRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -82f), new Vector2(360f, 18f));
            SetRectTransform(statusLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -92f), new Vector2(980f, 30f));
            SetRectTransform(coinHudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(482f, -18f), new Vector2(220f, 42f));
            SetRectTransform(coinHudIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            SetRectTransform(coinHudLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(170f, 36f));
            SetRectTransform(skillsLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 12f), new Vector2(240f, 24f));
            SetRectTransform(playerHealthRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-52f, 24f));
            SetRectTransform(playerHealthLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetRectTransform(comboLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(760f, 36f));

            PuzzleBattleUiDocument.SkillCardSlot[] cardSlots = new PuzzleBattleUiDocument.SkillCardSlot[3];

            for (int i = 0; i < cardSlots.Length; i++)
            {
                RectTransform cardRoot = CreateUiRect(cardAreaRoot, $"SkillChoice_{i}");
                Image background = CreateUiImage(cardRoot, "Background", new Color(0.14f, 0.16f, 0.22f, 0.96f));
                StretchRect(background.rectTransform);
                Image accent = CreateUiImage(cardRoot, "Accent", Color.white);
                Text title = CreateUiText(cardRoot, "Title", font, 26, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
                Text description = CreateUiText(cardRoot, "Description", font, 18, FontStyle.Normal, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.82f));
                Text action = CreateUiText(cardRoot, "Action", font, 18, FontStyle.Bold, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.72f, 1f));
                Button button = cardRoot.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                SetRectTransform(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 390f, 0f), new Vector2(360f, 320f));
                StretchRect(background.rectTransform);
                SetRectTransform(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 16f));
                SetRectTransform(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(-36f, 60f));
                SetRectTransform(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(-42f, -118f));
                SetRectTransform(action.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-36f, 34f));

                cardSlots[i] = new PuzzleBattleUiDocument.SkillCardSlot
                {
                    Root = cardRoot,
                    Background = background,
                    Accent = accent,
                    Title = title,
                    Description = description,
                    ActionLabel = action,
                    Button = button
                };
            }

            int iconCount = System.Enum.GetValues(typeof(PuzzleBattleSkillId)).Length;
            PuzzleBattleUiDocument.SkillIconSlot[] iconSlots = new PuzzleBattleUiDocument.SkillIconSlot[Mathf.Max(6, iconCount)];

            for (int i = 0; i < iconSlots.Length; i++)
            {
                RectTransform iconRoot = CreateUiRect(topUiRoot, $"AcquiredSkill_{i}");
                Image frame = CreateUiImage(iconRoot, "Frame", new Color(0.16f, 0.18f, 0.24f, 0.94f));
                StretchRect(frame.rectTransform);
                Image icon = CreateUiImage(iconRoot, "Icon", Color.white);
                Text level = CreateUiText(iconRoot, "Level", font, 15, FontStyle.Bold, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.76f, 1f));
                float iconSize = 60f;
                float spacing = 10f;
                float centerX = 28f + (iconSize * 0.5f) + (i * (iconSize + spacing));
                SetRectTransform(iconRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(centerX, 42f), new Vector2(iconSize, iconSize));
                StretchRect(frame.rectTransform);
                SetRectTransform(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(iconSize * 0.58f, iconSize * 0.58f));
                SetRectTransform(level.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(0f, 18f));

                iconSlots[i] = new PuzzleBattleUiDocument.SkillIconSlot
                {
                    Root = iconRoot,
                    Frame = frame,
                    Icon = icon,
                    LevelLabel = level
                };
            }

            PuzzleBattleUiDocument.HudButtonSlot[] hudButtons =
            {
                CreateHudButtonSlot(topUiRoot, font, "settings", "설정"),
                CreateHudButtonSlot(topUiRoot, font, "quit", "종료")
            };

            PuzzleBattleUiDocument.TurnTimerBarSlot turnTimerBar = new PuzzleBattleUiDocument.TurnTimerBarSlot
            {
                Root = turnTimerBarRoot,
                Background = turnTimerBarBackground,
                Fill = turnTimerBarFill
            };

            PuzzleBattleUiDocument.CoinHudSlot coinHud = new PuzzleBattleUiDocument.CoinHudSlot
            {
                Root = coinHudRoot,
                Icon = coinHudIcon,
                Label = coinHudLabel
            };

            PuzzleBattleUiDocument.PlayerHealthBarSlot playerHealthBar = new PuzzleBattleUiDocument.PlayerHealthBarSlot
            {
                Root = playerHealthRoot,
                Background = playerHealthBarBackground,
                Fill = playerHealthBarFill,
                Label = playerHealthLabel
            };

            PuzzleBattleUiDocument document = root.GetComponent<PuzzleBattleUiDocument>();
            document.SetAuthoringReferences(canvas, uiRoot, topUiRoot, cardAreaRoot, true, roundLabel, statusLabel, timerLabel, skillsLabel, comboLabel, turnTimerBar, coinHud, playerHealthBar, cardSlots, iconSlots, hudButtons);

            PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
        }

        private static PuzzleBattleUiDocument.HudButtonSlot CreateHudButtonSlot(Transform parent, Font font, string id, string labelText)
        {
            RectTransform root = CreateUiRect(parent, $"{id}_Button");
            Image background = CreateUiImage(root, "Background", new Color(0.15f, 0.18f, 0.24f, 0.96f));
            StretchRect(background.rectTransform);
            Text label = CreateUiText(root, "Label", font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            label.text = labelText;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            float offsetX = id == "settings" ? 26f : 166f;
            SetRectTransform(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-offsetX, -20f), new Vector2(128f, 38f));
            StretchRect(background.rectTransform);
            SetRectTransform(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            return new PuzzleBattleUiDocument.HudButtonSlot
            {
                Id = id,
                Root = root,
                Background = background,
                Label = label,
                Button = button
            };
        }

        private static RectTransform CreateUiRect(Transform parent, string objectName)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static Text CreateUiText(Transform parent, string objectName, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            RectTransform rectTransform = CreateUiRect(parent, objectName);
            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateUiImage(Transform parent, string objectName, Color color)
        {
            RectTransform rectTransform = CreateUiRect(parent, objectName);
            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.sprite = ProceduralSpriteLibrary.GetSquareSprite();
            image.color = color;
            return image;
        }

        private static void StretchRect(RectTransform rectTransform)
        {
            StretchRect(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void StretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
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

        private static OrbPrefabCatalog LoadOrCreateCatalog(List<OrbPrefabCatalog.Entry> defaultEntries)
        {
            OrbPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<OrbPrefabCatalog>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<OrbPrefabCatalog>();
                catalog.SetAuthoringEntries(defaultEntries.ToArray());
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                return catalog;
            }

            List<OrbPrefabCatalog.Entry> mergedEntries = new List<OrbPrefabCatalog.Entry>(catalog.Entries);
            bool dirty = false;

            for (int i = 0; i < defaultEntries.Count; i++)
            {
                OrbPrefabCatalog.Entry defaultEntry = defaultEntries[i];
                int existingIndex = FindEntryIndex(mergedEntries, defaultEntry.Definition);

                if (existingIndex < 0)
                {
                    mergedEntries.Add(defaultEntry);
                    dirty = true;
                    continue;
                }

                OrbPrefabCatalog.Entry existingEntry = mergedEntries[existingIndex];
                OrbVisualDefinition definition = existingEntry.Definition != null ? existingEntry.Definition : defaultEntry.Definition;
                BoardPieceView prefab = existingEntry.Prefab != null ? existingEntry.Prefab : defaultEntry.Prefab;

                if (definition != existingEntry.Definition || prefab != existingEntry.Prefab)
                {
                    mergedEntries[existingIndex] = new OrbPrefabCatalog.Entry(definition, prefab);
                    dirty = true;
                }
            }

            if (dirty)
            {
                catalog.SetAuthoringEntries(mergedEntries.ToArray());
                EditorUtility.SetDirty(catalog);
            }

            return catalog;
        }

        private static OrbPrefabCatalog ForceWriteCatalog(List<OrbPrefabCatalog.Entry> entries)
        {
            OrbPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<OrbPrefabCatalog>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<OrbPrefabCatalog>();
                catalog.SetAuthoringEntries(entries.ToArray());
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                catalog.SetAuthoringEntries(entries.ToArray());
                EditorUtility.SetDirty(catalog);
            }

            return catalog;
        }

        private static PuzzleBattleBoardProfile ForceWriteBoardProfile(OrbPrefabCatalog catalog, OrbMotionProfile motionProfile)
        {
            PuzzleBattleBoardProfile profile = AssetDatabase.LoadAssetAtPath<PuzzleBattleBoardProfile>(BoardProfilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PuzzleBattleBoardProfile>();
                profile.SetAuthoringDefaults(6, 5, 0.6f, catalog, motionProfile);
                AssetDatabase.CreateAsset(profile, BoardProfilePath);
            }
            else
            {
                profile.SetOrbCatalog(catalog);
                profile.SetMotionProfile(motionProfile);
                EditorUtility.SetDirty(profile);
            }

            return profile;
        }

        private static PuzzleBattleBoardProfile LoadOrCreateBoardProfile(OrbPrefabCatalog catalog, OrbMotionProfile motionProfile)
        {
            PuzzleBattleBoardProfile profile = AssetDatabase.LoadAssetAtPath<PuzzleBattleBoardProfile>(BoardProfilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PuzzleBattleBoardProfile>();
                profile.SetAuthoringDefaults(6, 5, 0.6f, catalog, motionProfile);
                AssetDatabase.CreateAsset(profile, BoardProfilePath);
                return profile;
            }

            bool dirty = false;

            if (profile.OrbPrefabCatalog == null)
            {
                profile.SetOrbCatalog(catalog);
                dirty = true;
            }

            if (profile.MotionProfile == null)
            {
                profile.SetMotionProfile(motionProfile);
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(profile);
            }

            return profile;
        }

        private static int FindEntryIndex(List<OrbPrefabCatalog.Entry> entries, OrbVisualDefinition definition)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                OrbVisualDefinition existingDefinition = entries[i].Definition;

                if (existingDefinition == definition)
                {
                    return i;
                }

                if (existingDefinition != null && definition != null && existingDefinition.OrbId == definition.OrbId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
