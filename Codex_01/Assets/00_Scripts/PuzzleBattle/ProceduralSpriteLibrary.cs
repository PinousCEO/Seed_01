using UnityEngine;

namespace PuzzleBattle
{
    public static class ProceduralSpriteLibrary
    {
        private static Sprite _squareSprite;
        private static Sprite _orbSprite;
        private static Sprite _monsterSprite;
        private static Sprite _softCircleSprite;

        public static Sprite GetSquareSprite()
        {
            if (_squareSprite == null)
            {
                _squareSprite = CreateSprite("PuzzleBattleSquare", 16, (x, y) => Color.white);
            }

            return _squareSprite;
        }

        public static Sprite GetOrbSprite()
        {
            if (_orbSprite == null)
            {
                _orbSprite = CreateSprite("PuzzleBattleOrb", 128, (x, y) =>
                {
                    float distance = Mathf.Sqrt((x * x) + (y * y));
                    float alpha = 1f - Mathf.InverseLerp(0.92f, 1f, distance);
                    alpha = Mathf.Clamp01(alpha);
                    float highlight = Mathf.Clamp01(1f - Mathf.InverseLerp(0f, 0.9f, Vector2.Distance(new Vector2(x, y), new Vector2(-0.35f, 0.4f))));
                    return new Color(1f, 1f, 1f, alpha) * (0.85f + (0.15f * highlight));
                });
            }

            return _orbSprite;
        }

        public static Sprite GetMonsterSprite()
        {
            if (_monsterSprite == null)
            {
                _monsterSprite = CreateSprite("PuzzleBattleMonster", 128, (x, y) =>
                {
                    float radius = 0.28f;
                    float innerX = Mathf.Abs(x) - (0.82f - radius);
                    float innerY = Mathf.Abs(y) - (0.62f - radius);
                    float dx = Mathf.Max(innerX, 0f);
                    float dy = Mathf.Max(innerY, 0f);
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)) - radius;
                    float alpha = 1f - Mathf.InverseLerp(0f, 0.08f, distance);
                    alpha = Mathf.Clamp01(alpha);
                    return new Color(1f, 1f, 1f, alpha);
                });
            }

            return _monsterSprite;
        }

        public static Sprite GetSoftCircleSprite()
        {
            if (_softCircleSprite == null)
            {
                _softCircleSprite = CreateSprite("PuzzleBattleSoftCircle", 128, (x, y) =>
                {
                    float distance = Mathf.Sqrt((x * x) + (y * y));
                    float alpha = 1f - Mathf.InverseLerp(0.78f, 1f, distance);
                    alpha = Mathf.Clamp01(alpha);
                    return new Color(1f, 1f, 1f, alpha);
                });
            }

            return _softCircleSprite;
        }

        private static Sprite CreateSprite(string name, int size, System.Func<float, float, Color> colorAt)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            int index = 0;

            for (int y = 0; y < size; y++)
            {
                float normalizedY = ((y + 0.5f) / size * 2f) - 1f;

                for (int x = 0; x < size; x++)
                {
                    float normalizedX = ((x + 0.5f) / size * 2f) - 1f;
                    pixels[index++] = colorAt(normalizedX, normalizedY);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
