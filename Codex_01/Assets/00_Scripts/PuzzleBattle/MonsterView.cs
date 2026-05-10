using System.Collections;
using UnityEngine;

namespace PuzzleBattle
{
    public sealed class MonsterView : MonoBehaviour
    {
        private sealed class DamagePopupAnimator : MonoBehaviour
        {
            private Transform _popupTransform;
            private TextMesh _popupText;
            private MeshRenderer _popupRenderer;
            private Vector3 _startPosition;
            private Vector3 _endPosition;
            private Vector3 _startScale;
            private Vector3 _endScale;
            private Color _startColor;
            private float _arcHeight;
            private float _duration;
            private float _elapsed;
            private int _sortingOrder;

            public void Initialize(Transform popupTransform, TextMesh popupText, MeshRenderer popupRenderer, float width, float height, int sortingOrder)
            {
                _popupTransform = popupTransform;
                _popupText = popupText;
                _popupRenderer = popupRenderer;
                _sortingOrder = sortingOrder;
                _startPosition = popupTransform.position;

                float horizontalDrift = Random.Range(-width * 0.18f, width * 0.18f);
                float riseHeight = Mathf.Max(height * 0.42f, 0.32f);
                _endPosition = _startPosition + new Vector3(horizontalDrift, riseHeight, 0f);
                _arcHeight = Mathf.Max(height * 0.18f, 0.14f);
                _startScale = Vector3.one * 0.7f;
                _endScale = Vector3.one * 1.02f;
                _startColor = popupText.color;
                _duration = 0.72f;
                _elapsed = 0f;
                popupTransform.localScale = _startScale;
            }

            private void Update()
            {
                if (_popupTransform == null || _popupText == null)
                {
                    Destroy(gameObject);
                    return;
                }

                _elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(_elapsed / _duration);
                Vector3 linearPosition = Vector3.Lerp(_startPosition, _endPosition, progress);
                float arcOffset = _arcHeight * 4f * progress * (1f - progress);
                _popupTransform.position = linearPosition + new Vector3(0f, arcOffset, 0f);

                float scaleT = progress < 0.2f
                    ? Mathf.SmoothStep(0f, 1f, progress / 0.2f)
                    : Mathf.Lerp(1f, 0.92f, Mathf.InverseLerp(0.2f, 1f, progress));
                _popupTransform.localScale = Vector3.LerpUnclamped(_startScale, _endScale, scaleT);

                float fadeProgress = Mathf.InverseLerp(0.18f, 1f, progress);
                _popupText.color = new Color(_startColor.r, _startColor.g, _startColor.b, 1f - fadeProgress);

                if (_popupRenderer != null)
                {
                    _popupRenderer.sortingOrder = _sortingOrder;
                }

                if (progress >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _shadowRenderer;
        private SpriteRenderer _barBackgroundRenderer;
        private SpriteRenderer _barFillRenderer;
        private TextMesh _label;
        private MeshRenderer _labelRenderer;
        private Coroutine _flashRoutine;
        private Color _baseTint;
        private int _maxHealth;
        private int _currentHealth;
        private int _sortingOrderBase = 10;

        public float Width { get; private set; }
        public float Height { get; private set; }

        public void Initialize(string label, int maxHealth, float width, float height, Color tint)
        {
            _maxHealth = Mathf.Max(1, maxHealth);
            _currentHealth = _maxHealth;
            _baseTint = tint;
            Width = width;
            Height = height;

            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            _shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite = ProceduralSpriteLibrary.GetMonsterSprite();
            _shadowRenderer.color = new Color(0f, 0f, 0f, 0.18f);
            _shadowRenderer.sortingOrder = 5;
            shadow.transform.localScale = new Vector3(width * 1.05f, height * 0.38f, 1f);
            shadow.transform.localPosition = new Vector3(0f, -height * 0.52f, 0f);

            GameObject body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            _bodyRenderer = body.AddComponent<SpriteRenderer>();
            _bodyRenderer.sprite = ProceduralSpriteLibrary.GetMonsterSprite();
            _bodyRenderer.color = tint;
            _bodyRenderer.sortingOrder = 10;
            body.transform.localScale = new Vector3(width, height, 1f);

            GameObject barBackground = new GameObject("HpBackground");
            barBackground.transform.SetParent(transform, false);
            _barBackgroundRenderer = barBackground.AddComponent<SpriteRenderer>();
            _barBackgroundRenderer.sprite = ProceduralSpriteLibrary.GetSquareSprite();
            _barBackgroundRenderer.color = new Color(0f, 0f, 0f, 0.35f);
            _barBackgroundRenderer.sortingOrder = 11;
            barBackground.transform.localScale = new Vector3(width * 0.82f, 0.08f, 1f);
            barBackground.transform.localPosition = new Vector3(0f, -(height * 0.64f), 0f);

            GameObject barFill = new GameObject("HpFill");
            barFill.transform.SetParent(barBackground.transform, false);
            _barFillRenderer = barFill.AddComponent<SpriteRenderer>();
            _barFillRenderer.sprite = ProceduralSpriteLibrary.GetSquareSprite();
            _barFillRenderer.color = new Color(0.33f, 0.95f, 0.48f, 1f);
            _barFillRenderer.sortingOrder = 12;
            barFill.transform.localScale = new Vector3(1f, 0.8f, 1f);
            barFill.transform.localPosition = new Vector3(0f, 0f, 0f);

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            _label = labelObject.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.characterSize = 0.095f;
            _label.fontSize = 36;
            _label.color = Color.white;
            _label.text = maxHealth.ToString();
            labelObject.transform.localPosition = new Vector3(0f, -(height * 0.46f), 0f);
            _labelRenderer = labelObject.GetComponent<MeshRenderer>();

            UpdateHealthVisuals();
        }

        public void SetWorldPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetSortingOrder(int order)
        {
            _sortingOrderBase = order;
            _shadowRenderer.sortingOrder = order;
            _bodyRenderer.sortingOrder = order + 5;
            _barBackgroundRenderer.sortingOrder = order + 6;
            _barFillRenderer.sortingOrder = order + 7;

            if (_labelRenderer != null)
            {
                _labelRenderer.sortingOrder = order + 8;
            }
        }

        public bool ApplyDamage(int damage, Color damageTint)
        {
            int clampedDamage = Mathf.Max(0, damage);

            if (clampedDamage <= 0)
            {
                return _currentHealth <= 0;
            }

            SpawnDamagePopup(clampedDamage, damageTint);
            _currentHealth = Mathf.Max(0, _currentHealth - clampedDamage);
            UpdateHealthVisuals();

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine(damageTint));
            return _currentHealth <= 0;
        }

        public void PlayDeath()
        {
            StartCoroutine(DeathRoutine());
        }

        private void UpdateHealthVisuals()
        {
            float normalizedHealth = Mathf.Clamp01(_currentHealth / (float)_maxHealth);
            _barFillRenderer.transform.localScale = new Vector3(normalizedHealth, 0.8f, 1f);
            _barFillRenderer.transform.localPosition = new Vector3((normalizedHealth - 1f) * 0.5f, 0f, 0f);
            _label.text = _currentHealth.ToString();
        }

        private void SpawnDamagePopup(int damage, Color tint)
        {
            Transform parent = transform.parent != null ? transform.parent : transform;
            GameObject popupObject = new GameObject("DamagePopup");
            popupObject.transform.SetParent(parent, false);
            popupObject.transform.position = transform.position + new Vector3(Random.Range(-Width * 0.08f, Width * 0.08f), Height * 0.22f, 0f);

            TextMesh popup = popupObject.AddComponent<TextMesh>();
            popup.anchor = TextAnchor.MiddleCenter;
            popup.alignment = TextAlignment.Center;
            popup.characterSize = 0.11f;
            popup.fontSize = 44;
            popup.color = new Color(Mathf.Clamp01(tint.r + 0.12f), Mathf.Clamp01(tint.g + 0.12f), Mathf.Clamp01(tint.b + 0.12f), 1f);
            popup.text = damage.ToString();

            MeshRenderer popupRenderer = popupObject.GetComponent<MeshRenderer>();

            if (popupRenderer != null)
            {
                popupRenderer.sortingOrder = _sortingOrderBase + 12;
            }

            DamagePopupAnimator animator = popupObject.AddComponent<DamagePopupAnimator>();
            animator.Initialize(popupObject.transform, popup, popupRenderer, Width, Height, _sortingOrderBase + 12);
        }

        private IEnumerator FlashRoutine(Color flashTint)
        {
            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _bodyRenderer.color = Color.Lerp(Color.white, _baseTint, progress);
                _barFillRenderer.color = Color.Lerp(flashTint, new Color(0.33f, 0.95f, 0.48f, 1f), progress);
                yield return null;
            }

            _bodyRenderer.color = _baseTint;
            _barFillRenderer.color = new Color(0.33f, 0.95f, 0.48f, 1f);
            _flashRoutine = null;
        }

        private IEnumerator DeathRoutine()
        {
            Vector3 startScale = transform.localScale;
            Color bodyColor = _bodyRenderer.color;
            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float scale = 1f - progress;
                transform.localScale = startScale * scale;
                _bodyRenderer.color = new Color(bodyColor.r, bodyColor.g, bodyColor.b, 1f - progress);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
