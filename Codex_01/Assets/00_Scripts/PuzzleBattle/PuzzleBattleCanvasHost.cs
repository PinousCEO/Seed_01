using UnityEngine;
namespace PuzzleBattle
{
    public sealed class PuzzleBattleCanvasHost : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private RectTransform topUiRoot;
        [SerializeField] private RectTransform cardAreaRoot;
        [SerializeField] private bool createMissingRoots = true;

        public Canvas Canvas => canvas;
        public RectTransform UiRoot => uiRoot;
        public RectTransform TopUiRoot => topUiRoot;
        public RectTransform CardAreaRoot => cardAreaRoot;
        public bool CreateMissingRoots => createMissingRoots;

        private void Reset()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvas != null && uiRoot == null)
            {
                uiRoot = canvas.GetComponent<RectTransform>();
            }
        }
    }
}
