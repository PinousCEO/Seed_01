using UnityEngine;
using UnityEngine.UI;

namespace PuzzleBattle
{
    public sealed class PuzzleBattleUiDocument : MonoBehaviour
    {
        [System.Serializable]
        public sealed class TurnTimerBarSlot
        {
            public RectTransform Root;
            public Image Background;
            public Image Fill;
        }

        [System.Serializable]
        public sealed class CoinHudSlot
        {
            public RectTransform Root;
            public Image Icon;
            public Text Label;
        }

        [System.Serializable]
        public sealed class PlayerHealthBarSlot
        {
            public RectTransform Root;
            public Image Background;
            public Image Fill;
            public Text Label;
        }

        [System.Serializable]
        public sealed class SkillCardSlot
        {
            public RectTransform Root;
            public Image Background;
            public Image Accent;
            public Text Title;
            public Text Description;
            public Text ActionLabel;
            public Button Button;
        }

        [System.Serializable]
        public sealed class SkillIconSlot
        {
            public RectTransform Root;
            public Image Frame;
            public Image Icon;
            public Text LevelLabel;
        }

        [System.Serializable]
        public sealed class HudButtonSlot
        {
            public string Id;
            public RectTransform Root;
            public Image Background;
            public Text Label;
            public Button Button;
        }

        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private RectTransform topUiRoot;
        [SerializeField] private RectTransform cardAreaRoot;
        [SerializeField] private bool applyRuntimeLayout;
        [SerializeField] private Text roundLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField] private Text skillsLabel;
        [SerializeField] private Text comboLabel;
        [SerializeField] private TurnTimerBarSlot turnTimerBar;
        [SerializeField] private CoinHudSlot coinHud;
        [SerializeField] private PlayerHealthBarSlot playerHealthBar;
        [SerializeField] private SkillCardSlot[] skillCards;
        [SerializeField] private SkillIconSlot[] skillIcons;
        [SerializeField] private HudButtonSlot[] hudButtons;

        public Canvas Canvas => canvas != null ? canvas : GetComponent<Canvas>();
        public RectTransform UiRoot => uiRoot != null ? uiRoot : GetComponent<RectTransform>();
        public RectTransform TopUiRoot => topUiRoot;
        public RectTransform CardAreaRoot => cardAreaRoot;
        public bool ApplyRuntimeLayout => applyRuntimeLayout;
        public Text RoundLabel => roundLabel;
        public Text StatusLabel => statusLabel;
        public Text TimerLabel => timerLabel;
        public Text SkillsLabel => skillsLabel;
        public Text ComboLabel => comboLabel;
        public TurnTimerBarSlot TurnTimerBar => turnTimerBar;
        public CoinHudSlot CoinHud => coinHud;
        public PlayerHealthBarSlot PlayerHealthBar => playerHealthBar;
        public SkillCardSlot[] SkillCards => skillCards;
        public SkillIconSlot[] SkillIcons => skillIcons;
        public HudButtonSlot[] HudButtons => hudButtons;

        public void SetAuthoringReferences(
            Canvas canvasValue,
            RectTransform uiRootValue,
            RectTransform topUiRootValue,
            RectTransform cardAreaRootValue,
            bool applyRuntimeLayoutValue,
            Text roundLabelValue,
            Text statusLabelValue,
            Text timerLabelValue,
            Text skillsLabelValue,
            Text comboLabelValue,
            TurnTimerBarSlot turnTimerBarValue,
            CoinHudSlot coinHudValue,
            PlayerHealthBarSlot playerHealthBarValue,
            SkillCardSlot[] skillCardValues,
            SkillIconSlot[] skillIconValues,
            HudButtonSlot[] hudButtonValues)
        {
            canvas = canvasValue;
            uiRoot = uiRootValue;
            topUiRoot = topUiRootValue;
            cardAreaRoot = cardAreaRootValue;
            applyRuntimeLayout = applyRuntimeLayoutValue;
            roundLabel = roundLabelValue;
            statusLabel = statusLabelValue;
            timerLabel = timerLabelValue;
            skillsLabel = skillsLabelValue;
            comboLabel = comboLabelValue;
            turnTimerBar = turnTimerBarValue;
            coinHud = coinHudValue;
            playerHealthBar = playerHealthBarValue;
            skillCards = skillCardValues;
            skillIcons = skillIconValues;
            hudButtons = hudButtonValues;
        }
    }
}
