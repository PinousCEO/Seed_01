using UnityEngine;

namespace PuzzleBattle
{
    public static class PuzzleBattleOrbDefaults
    {
        public readonly struct OrbSeed
        {
            public OrbSeed(string id, string displayName, Color tint, int damage)
            {
                Id = id;
                DisplayName = displayName;
                Tint = tint;
                Damage = damage;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public Color Tint { get; }
            public int Damage { get; }
        }

        public static readonly OrbSeed[] Seeds =
        {
            new OrbSeed("fire", "Fire", new Color(0.95f, 0.39f, 0.32f), 14),
            new OrbSeed("water", "Water", new Color(0.34f, 0.62f, 0.98f), 12),
            new OrbSeed("wood", "Wood", new Color(0.39f, 0.82f, 0.42f), 12),
            new OrbSeed("light", "Light", new Color(0.98f, 0.9f, 0.48f), 13),
            new OrbSeed("dark", "Dark", new Color(0.62f, 0.46f, 0.92f), 15)
        };
    }
}
