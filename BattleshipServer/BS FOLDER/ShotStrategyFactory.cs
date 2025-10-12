using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BattleshipServer.Npc
{
    /// <summary>
    /// Automatiškai užregistruoja visas INpcShotStrategy klases šiame Assembly.
    /// Pridėjai naują *.cs su [ShotStrategy("raktas")] – ji iškart pasiekiama per Create(raktas).
    /// </summary>
    public static class ShotStrategyFactory
    {
        private static readonly Dictionary<string, Type> _byKey;

        static ShotStrategyFactory()
        {
            var asm = typeof(INpcShotStrategy).Assembly;
            var types = asm.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(INpcShotStrategy).IsAssignableFrom(t));

            _byKey = new(StringComparer.OrdinalIgnoreCase);

            foreach (var t in types)
            {
                var key = t.GetCustomAttribute<ShotStrategyAttribute>()?.Key ?? ToKey(t.Name);
                if (_byKey.ContainsKey(key))
                    throw new InvalidOperationException(
                        $"Shot strategy key '{key}' already used by {_byKey[key].Name} and {t.Name}.");
                _byKey[key] = t;
            }
        }

        public static INpcShotStrategy Create(string key)
        {
            if (!_byKey.TryGetValue(key.Trim(), out var type))
                throw new KeyNotFoundException($"Strategy '{key}' not found. Available: {string.Join(", ", _byKey.Keys)}");
            return (INpcShotStrategy)Activator.CreateInstance(type)!;
        }

        public static IReadOnlyCollection<string> AvailableKeys() => _byKey.Keys.ToArray();

        private static string ToKey(string typeName)
        {
            // HumanLikeFrontierHeatStrategy -> "human-like-frontier-heat"
            var chars = new List<char>(typeName.Length + 8);
            for (int i = 0; i < typeName.Length; i++)
            {
                var c = typeName[i];
                if (char.IsUpper(c) && i > 0) chars.Add('-');
                chars.Add(char.ToLowerInvariant(c));
            }
            var s = new string(chars.ToArray());
            return s.EndsWith("-strategy", StringComparison.Ordinal) ? s[..^10] : s;
        }
    }
}
