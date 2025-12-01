using System;
using System.Collections.Generic;

namespace BattleshipServer.Defense
{
    public enum DefenseMode
    {
        None = 0,
        Safetiness = 1,
        Visibility = 2
    }

    public interface IDefenseComponent
    {
        // Pagal koordinatę pasako, koks gynybos režimas galioja šitam langeliui
        DefenseMode GetMode(int x, int y);
    }

    // Vieno langelio skydas
    public sealed class CellShield : IDefenseComponent
    {
        public int X { get; }
        public int Y { get; }
        public DefenseMode Mode { get; private set; }

        public CellShield(int x, int y, DefenseMode mode)
        {
            X = x;
            Y = y;
            Mode = mode;
        }

        public DefenseMode GetMode(int x, int y)
        {
            if (x == X && y == Y && Mode != DefenseMode.None)
            {
                var current = Mode;

                // SAFETINESS skydas suveikia tik vieną kartą:
                // po šito šūvio jis išsijungia, kad kitą kartą šūvis jau pataikytų į laivą.
                if (current == DefenseMode.Safetiness)
                {
                    Mode = DefenseMode.None;
                }

                return current;
            }

            return DefenseMode.None;
        }

        public void ChangeMode(DefenseMode newMode) => Mode = newMode;
    }

    // Zonos skydas (pvz. 3x3)
    public sealed class AreaShield : IDefenseComponent
    {
        public int X1 { get; }
        public int Y1 { get; }
        public int X2 { get; }
        public int Y2 { get; }
        public DefenseMode Mode { get; private set; }

        public AreaShield(int x1, int y1, int x2, int y2, DefenseMode mode)
        {
            X1 = Math.Min(x1, x2);
            Y1 = Math.Min(y1, y2);
            X2 = Math.Max(x1, x2);
            Y2 = Math.Max(y1, y2);
            Mode = mode;
        }

        public DefenseMode GetMode(int x, int y)
        {
            if (Mode != DefenseMode.None &&
                x >= X1 && x <= X2 &&
                y >= Y1 && y <= Y2)
            {
                var current = Mode;

                // Tas pats principas: jei zona turi SAFETINESS,
                // ji "sudega" po pirmo panaudojimo.
                if (current == DefenseMode.Safetiness)
                {
                    Mode = DefenseMode.None;
                }

                return current;
            }

            return DefenseMode.None;
        }

        public void ChangeMode(DefenseMode newMode) => Mode = newMode;
    }

    // Composite – turi daug vaikų (CellShield, AreaShield, ir t.t.)
    public sealed class DefenseComposite : IDefenseComponent
    {
        private readonly List<IDefenseComponent> _children = new();

        public void Add(IDefenseComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            _children.Add(component);
        }

        public void Remove(IDefenseComponent component)
        {
            _children.Remove(component);
        }

        public DefenseMode GetMode(int x, int y)
        {
            foreach (var child in _children)
            {
                var mode = child.GetMode(x, y);
                if (mode != DefenseMode.None)
                    return mode;
            }
            return DefenseMode.None;
        }
    }
}
