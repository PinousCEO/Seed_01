using System.Collections;
using UnityEngine;

namespace PuzzleBattle
{
    public sealed class BoardPieceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private bool preservePrefabVisuals = true;
        private OrbVisualDefinition _definition;
        private OrbMotionProfile _motionProfile;
        private Sprite _prefabSprite;
        private Coroutine _moveRoutine;
        private Coroutine _popRoutine;
        private float _baseScale;
        private float _animationScale = 1f;
        private float _selectedSeed;
        private bool _isSelected;
        private bool _isPopped;

        public OrbVisualDefinition Definition => _definition;
        public Color DisplayColor => _definition != null ? _definition.Tint : (_renderer != null ? _renderer.color : Color.white);

        private void Reset()
        {
            EnsureRendererReference();
        }

        private void OnValidate()
        {
            EnsureRendererReference();
        }

        public void Initialize(OrbVisualDefinition definition, OrbMotionProfile motionProfile, float baseScale)
        {
            StopAllCoroutines();
            _moveRoutine = null;
            _popRoutine = null;
            _isSelected = false;
            _definition = definition;
            _motionProfile = motionProfile;
            _baseScale = baseScale;
            _selectedSeed = Random.Range(0f, 100f);
            _isPopped = false;
            _animationScale = 1f;
            gameObject.SetActive(true);

            EnsureRendererReference();
            bool hasPrefabRenderer = _renderer != null;

            if (!hasPrefabRenderer)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (hasPrefabRenderer && preservePrefabVisuals)
            {
                _prefabSprite = _renderer.sprite;
            }

            ApplyDefinitionVisuals();
            _renderer.sortingOrder = 20;
            UpdateScale();
        }

        public void DeactivateForPool()
        {
            StopAllCoroutines();
            _moveRoutine = null;
            _popRoutine = null;
            _isSelected = false;
            _isPopped = false;
            _animationScale = 1f;

            if (_renderer != null)
            {
                Color color = _renderer.color;
                _renderer.color = new Color(color.r, color.g, color.b, 1f);
            }

            gameObject.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateScale();
        }

        public void SetBaseScale(float baseScale)
        {
            _baseScale = baseScale;
            UpdateScale();
        }

        public void SetSortingOrder(int order)
        {
            if (_renderer != null)
            {
                _renderer.sortingOrder = order;
            }
        }

        public void SnapTo(Vector3 position)
        {
            transform.position = position;
        }

        public void AnimateTo(Vector3 targetPosition, float duration)
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
            }

            _moveRoutine = StartCoroutine(MoveRoutine(targetPosition, duration));
        }

        public void AnimatePop()
        {
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
            }

            _popRoutine = StartCoroutine(PopRoutine());
        }

        private void Update()
        {
            if (_popRoutine != null || _isPopped)
            {
                return;
            }

            ApplyDefinitionVisuals();
            UpdateScale();
        }

        private IEnumerator MoveRoutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = _motionProfile.MoveCurve.Evaluate(Mathf.Clamp01(elapsed / safeDuration));
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
                yield return null;
            }

            transform.position = targetPosition;
            _moveRoutine = null;
        }

        private IEnumerator PopRoutine()
        {
            float elapsed = 0f;
            Color baseColor = _renderer != null ? _renderer.color : DisplayColor;
            float duration = _motionProfile.PopDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float scaleFactor = _motionProfile.PopCurve.Evaluate(progress);
                _animationScale = scaleFactor;
                float flash = 1f - progress;
                _renderer.color = Color.Lerp(baseColor, Color.white, flash * 0.65f);
                UpdateScale();
                yield return null;
            }

            _animationScale = 0f;
            _isPopped = true;
            UpdateScale();
            _renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            _popRoutine = null;
        }

        private void UpdateScale()
        {
            float selectionScale = 1f;

            if (_isSelected)
            {
                selectionScale = _motionProfile.SelectedScale + (Mathf.Sin((Time.time + _selectedSeed) * _motionProfile.SelectedPulseSpeed) * _motionProfile.SelectedPulseAmplitude);
            }

            float spriteUnitSize = GetSpriteUnitSize();
            transform.localScale = Vector3.one * (_baseScale / spriteUnitSize) * selectionScale * _animationScale;
        }

        private void EnsureRendererReference()
        {
            if (_renderer == null)
            {
                TryGetComponent(out _renderer);
            }

            if (_renderer != null && preservePrefabVisuals && _prefabSprite == null)
            {
                _prefabSprite = _renderer.sprite;
            }
        }

        private void ApplyDefinitionVisuals()
        {
            if (_renderer == null || _definition == null)
            {
                return;
            }

            Sprite proceduralOrbSprite = ProceduralSpriteLibrary.GetOrbSprite();
            Sprite desiredSprite = _definition.SpriteOverride;
            bool usesProceduralTint = false;

            if (desiredSprite == null)
            {
                desiredSprite = preservePrefabVisuals && _prefabSprite != null
                    ? _prefabSprite
                    : proceduralOrbSprite;
                usesProceduralTint = desiredSprite == proceduralOrbSprite;
            }

            if (_renderer.sprite != desiredSprite)
            {
                _renderer.sprite = desiredSprite;
            }

            Color desiredColor = usesProceduralTint ? _definition.Tint : Color.white;

            if (_renderer.color != desiredColor)
            {
                _renderer.color = desiredColor;
            }
        }

        private float GetSpriteUnitSize()
        {
            if (_renderer == null || _renderer.sprite == null)
            {
                return 1f;
            }

            Vector3 spriteSize = _renderer.sprite.bounds.size;
            float maxDimension = Mathf.Max(spriteSize.x, spriteSize.y);
            return maxDimension > 0.0001f ? maxDimension : 1f;
        }
    }
}
