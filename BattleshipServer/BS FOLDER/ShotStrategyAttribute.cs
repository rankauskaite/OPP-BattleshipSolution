using System;

namespace BattleshipServer.Npc
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ShotStrategyAttribute : Attribute
    {
        public string Key { get; }
        public ShotStrategyAttribute(string key) => Key = key?.Trim().ToLowerInvariant();
    }
}
