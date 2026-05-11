using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleBattle.Editor
{
    [InitializeOnLoad]
    public static class SampleSceneCanvasSetupUtility
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MainCanvasName = "#MainCanvas";
        private const string SessionKey = "PuzzleBattle.SampleSceneCanvasSetup";

        static SampleSceneCanvasSetupUtility()
        {
            EditorApplication.delayCall += TrySetupOpenSampleSceneOnce;
        }

        [MenuItem("Tools/Puzzle Battle/Setup SampleScene Canvas")]
        private static void SetupSampleSceneCanvasMenu()
        {
            SetupSampleSceneCanvas(forceOpenScene: true);
        }

        private static void TrySetupOpenSampleSceneOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == SampleScenePath)
            {
                SetupScene(activeScene);
            }
        }

        private static void SetupSampleSceneCanvas(bool forceOpenScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == SampleScenePath)
            {
                SetupScene(activeScene);
                return;
            }

            if (!forceOpenScene)
            {
                return;
            }

            if (activeScene.isDirty)
            {
                Debug.LogWarning("Active scene has unsaved changes. Save it before running SampleScene canvas setup.");
                return;
            }

            Scene openedScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            SetupScene(openedScene);
        }

        private static void SetupScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject canvasObject = GameObject.Find(MainCanvasName);
            if (canvasObject == null)
            {
                Debug.LogWarning($"Could not find {MainCanvasName} in {scene.path}.");
                return;
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            RectTransform uiRoot = canvasObject.GetComponent<RectTransform>();
            if (canvas == null || uiRoot == null)
            {
                Debug.LogWarning($"{MainCanvasName} is missing Canvas or RectTransform.");
                return;
            }

            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform topUiRoot = FindOrCreateRect(uiRoot, "@TOP_UIRoot");
            SetRect(topUiRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -480f), new Vector2(0f, 960f));

            RectTransform cardAreaRoot = FindOrCreateRect(uiRoot, "SkillCardArea");
            StretchRect(cardAreaRoot);

            TextMeshProUGUI roundLabel = FindOrCreateText(topUiRoot, "RoundLabel", font, 34, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.97f, 0.88f, 1f), "Round 1");
            TextMeshProUGUI timerLabel = FindOrCreateText(topUiRoot, "TimerLabel", font, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f, 0.92f), "Timer 20.0s");
            TextMeshProUGUI statusLabel = FindOrCreateText(topUiRoot, "StatusLabel", font, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.9f), "Pick a skill to begin.");
            TextMeshProUGUI skillsLabel = FindOrCreateText(topUiRoot, "SkillsLabel", font, 20, FontStyle.Bold, TextAnchor.LowerLeft, new Color(1f, 0.95f, 0.8f, 0.95f), "Status");
            TextMeshProUGUI comboLabel = FindOrCreateText(uiRoot, "ComboLabel", font, 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.82f), "Select a skill to start the round.");

            SetRect(roundLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(640f, 42f));
            SetRect(timerLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -56f), new Vector2(220f, 30f));
            SetRect(statusLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28f, -108f), new Vector2(-268f, 34f));
            SetRect(skillsLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 44f), new Vector2(240f, 24f));
            SetRect(comboLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(760f, 36f));

            RectTransform turnTimerBarRoot = FindOrCreateRect(topUiRoot, "TurnTimerBar");
            Image turnTimerBackground = FindOrCreateImage(turnTimerBarRoot, "Background", new Color(0.14f, 0.18f, 0.24f, 0.92f), false);
            Image turnTimerFill = FindOrCreateImage(turnTimerBarRoot, "Fill", new Color(0.38f, 0.82f, 1f, 0.96f), false);
            SetRect(turnTimerBarRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -82f), new Vector2(360f, 18f));
            StretchRect(turnTimerBackground.rectTransform);
            StretchRect(turnTimerFill.rectTransform);

            RectTransform coinHudRoot = FindOrCreateRect(topUiRoot, "CoinHud");
            Image coinHudIcon = FindOrCreateImage(coinHudRoot, "CoinIcon", new Color(1f, 0.84f, 0.22f, 0.96f), false);
            TextMeshProUGUI coinLabel = FindOrCreateText(coinHudRoot, "CoinLabel", font, 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.94f, 0.72f, 1f), "코인 0");
            SetRect(coinHudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(482f, -18f), new Vector2(220f, 42f));
            SetRect(coinHudIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            SetRect(coinLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(170f, 36f));

            RectTransform playerHealthRoot = FindOrCreateRect(topUiRoot, "PlayerHealthBar");
            Image playerHealthBackground = FindOrCreateImage(playerHealthRoot, "Background", new Color(0.18f, 0.09f, 0.1f, 0.94f), false);
            Image playerHealthFill = FindOrCreateImage(playerHealthRoot, "Fill", new Color(0.92f, 0.28f, 0.24f, 0.96f), false);
            TextMeshProUGUI playerHealthLabel = FindOrCreateText(playerHealthRoot, "Label", font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.92f, 1f), "HP 500/500");
            SetRect(playerHealthRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-52f, 24f));
            StretchRect(playerHealthBackground.rectTransform);
            StretchRect(playerHealthFill.rectTransform);
            StretchRect(playerHealthLabel.rectTransform);

            PuzzleBattleUiDocument.SkillCardSlot[] skillCards = new PuzzleBattleUiDocument.SkillCardSlot[3];
            for (int i = 0; i < skillCards.Length; i++)
            {
                RectTransform root = FindOrCreateRect(cardAreaRoot, $"SkillChoice_{i}");
                Image background = GetOrAddComponent<Image>(root.gameObject);
                background.color = new Color(0.14f, 0.16f, 0.22f, 0.96f);
                background.raycastTarget = true;
                GetOrAddComponent<CanvasRenderer>(root.gameObject);

                Button button = GetOrAddComponent<Button>(root.gameObject);
                button.targetGraphic = background;

                Image accent = FindOrCreateImage(root, "Accent", Color.white, false);
                TextMeshProUGUI title = FindOrCreateText(root, "Title", font, 26, FontStyle.Bold, TextAnchor.UpperCenter, Color.white, "Skill Title");
                TextMeshProUGUI description = FindOrCreateText(root, "Description", font, 18, FontStyle.Normal, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.82f), "Skill description");
                TextMeshProUGUI action = FindOrCreateText(root, "Action", font, 18, FontStyle.Bold, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.72f, 1f), "획득");

                SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 390f, 0f), new Vector2(360f, 320f));
                SetRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 16f));
                SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(-36f, 60f));
                SetRect(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(-42f, -118f));
                SetRect(action.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-36f, 34f));

                skillCards[i] = new PuzzleBattleUiDocument.SkillCardSlot
                {
                    Root = root,
                    Background = background,
                    Accent = accent,
                    Title = title,
                    Description = description,
                    ActionLabel = action,
                    Button = button
                };
            }

            PuzzleBattleUiDocument.HudButtonSlot[] hudButtons =
            {
                CreateHudButton(topUiRoot, font, "settings", "설정", new Vector2(-26f, -20f), new Color(0.15f, 0.18f, 0.24f, 0.96f)),
                CreateHudButton(topUiRoot, font, "quit", "종료", new Vector2(-166f, -20f), new Color(0.34f, 0.18f, 0.18f, 0.96f))
            };

            PuzzleBattleUiDocument document = GetOrAddComponent<PuzzleBattleUiDocument>(canvasObject);
            document.SetAuthoringReferences(
                canvas,
                uiRoot,
                topUiRoot,
                cardAreaRoot,
                false,
                roundLabel,
                statusLabel,
                timerLabel,
                skillsLabel,
                comboLabel,
                new PuzzleBattleUiDocument.TurnTimerBarSlot
                {
                    Root = turnTimerBarRoot,
                    Background = turnTimerBackground,
                    Fill = turnTimerFill
                },
                new PuzzleBattleUiDocument.CoinHudSlot
                {
                    Root = coinHudRoot,
                    Icon = coinHudIcon,
                    Label = coinLabel
                },
                new PuzzleBattleUiDocument.PlayerHealthBarSlot
                {
                    Root = playerHealthRoot,
                    Background = playerHealthBackground,
                    Fill = playerHealthFill,
                    Label = playerHealthLabel
                },
                skillCards,
                System.Array.Empty<PuzzleBattleUiDocument.SkillIconSlot>(),
                hudButtons);

            PuzzleBattleCanvasHost canvasHost = GetOrAddComponent<PuzzleBattleCanvasHost>(canvasObject);
            SerializedObject serializedHost = new SerializedObject(canvasHost);
            serializedHost.FindProperty("canvas").objectReferenceValue = canvas;
            serializedHost.FindProperty("uiDocument").objectReferenceValue = document;
            serializedHost.FindProperty("uiRoot").objectReferenceValue = uiRoot;
            serializedHost.FindProperty("topUiRoot").objectReferenceValue = topUiRoot;
            serializedHost.FindProperty("cardAreaRoot").objectReferenceValue = cardAreaRoot;
            serializedHost.FindProperty("createMissingRoots").boolValue = false;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static PuzzleBattleUiDocument.HudButtonSlot CreateHudButton(Transform parent, Font font, string id, string labelText, Vector2 anchoredPosition, Color backgroundColor)
        {
            RectTransform root = FindOrCreateRect(parent, $"{id}_Button");
            Image background = GetOrAddComponent<Image>(root.gameObject);
            background.color = backgroundColor;
            background.raycastTarget = true;
            GetOrAddComponent<CanvasRenderer>(root.gameObject);

            Button button = GetOrAddComponent<Button>(root.gameObject);
            button.targetGraphic = background;

            TextMeshProUGUI label = FindOrCreateText(root, "Label", font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, labelText);
            SetRect(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), anchoredPosition, new Vector2(128f, 38f));
            StretchRect(label.rectTransform);

            return new PuzzleBattleUiDocument.HudButtonSlot
            {
                Id = id,
                Root = root,
                Background = background,
                Label = label,
                Button = button
            };
        }

        private static RectTransform FindOrCreateRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<RectTransform>();
            }

            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = 5;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI FindOrCreateText(Transform parent, string name, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, string textValue)
        {
            RectTransform rect = FindOrCreateRect(parent, name);
            GetOrAddComponent<CanvasRenderer>(rect.gameObject);
            TextMeshProUGUI text = GetOrAddTmpText(rect.gameObject);
            ApplyTmpTextStyle(text, fontSize, fontStyle, alignment, color);
            text.text = textValue;
            return text;
        }

        private static Image FindOrCreateImage(Transform parent, string name, Color color, bool raycastTarget)
        {
            RectTransform rect = FindOrCreateRect(parent, name);
            GetOrAddComponent<CanvasRenderer>(rect.gameObject);
            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void StretchRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }


        private static TextMeshProUGUI GetOrAddTmpText(GameObject gameObject)
        {
            TextMeshProUGUI tmp = gameObject.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                return tmp;
            }

            Text legacyText = gameObject.GetComponent<Text>();

            if (legacyText != null)
            {
                Object.DestroyImmediate(legacyText);
            }

            return gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static void ApplyTmpTextStyle(TextMeshProUGUI text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            text.fontSize = fontSize;
            text.fontStyle = ToTmpFontStyle(fontStyle);
            text.alignment = ToTmpAlignment(alignment);
            text.color = color;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }

        private static FontStyles ToTmpFontStyle(FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }
    }
}
