// using System;
// using System.Linq;
// using System.Reflection;
// using System.Collections.Generic;

// namespace BattleshipServer.Npc
// {
//     [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
//     public sealed class ShotStrategyAttribute : Attribute
//     {
//         public string Key { get; }
//         public ShotStrategyAttribute(string key) => Key = key;
//     }

//     public static class ShotStrategyFactory
//     {
//         private static readonly Dictionary<string, Type> _byKey;

//         static ShotStrategyFactory()
//         {
//             _byKey = Assembly.GetExecutingAssembly()
//                 .GetTypes()
//                 .Where(t => typeof(INpcShotStrategy).IsAssignableFrom(t) && !t.IsAbstract && t.GetCustomAttribute<ShotStrategyAttribute>() != null)
//                 .ToDictionary(t => t.GetCustomAttribute<ShotStrategyAttribute>()!.Key, t => t, StringComparer.OrdinalIgnoreCase);
//         }

//         public static INpcShotStrategy Create(string key)
//         {
//             if (_byKey.TryGetValue(key, out var t)) return (INpcShotStrategy)Activator.CreateInstance(t)!;
//             // fallback
//             return new RandomShotStrategy();
//         }
//     }
// }
