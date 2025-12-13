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

    /// <summary>
    /// GoF Composite: Component
    ///
    /// UML "Operation()" tavo domeine yra "GetMode(x,y)".
    /// Kad UML atitiktų skaidrę, Component taip pat deklaruoja Add/Remove/GetChild.
    /// Leaf šiuos metodus laiko nepalaikomais (NotSupportedException).
    /// </summary>
    public interface IDefenseComponent
    {
        // Operation(x,y)
        DefenseMode GetMode(int x, int y);

        // Composite operations
        void Add(IDefenseComponent item);
        void Remove(IDefenseComponent item);
        IDefenseComponent GetChild(int index);
    }

    /// <summary>
    /// GoF Composite: Leaf (vienas langelis)
    /// </summary>
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

                // SAFETINESS: suveikia 1 kartą konkrečiam langeliui, po to išsijungia
                if (current == DefenseMode.Safetiness)
                {
                    Mode = DefenseMode.None;
                }

                return current;
            }

            return DefenseMode.None;
        }

        // Leaf neturi vaikų (kaip GoF: Leaf nepalaiko Add/Remove/GetChild)
        public void Add(IDefenseComponent item) =>
            throw new NotSupportedException("Leaf negali turėti vaikų (Add nepalaikomas)");

        public void Remove(IDefenseComponent item) =>
            throw new NotSupportedException("Leaf neturi vaikų (Remove nepalaikomas)");

        public IDefenseComponent GetChild(int index) =>
            throw new NotSupportedException("Leaf neturi vaikų (GetChild nepalaikomas)");

        public void ChangeMode(DefenseMode newMode) => Mode = newMode;
    }

    /// <summary>
    /// GoF Composite: Composite (turi daug vaikų)
    /// </summary>
    public sealed class DefenseComposite : IDefenseComponent
    {
        // <-- ČIA ir yra "children" (GoF Composite ryšys Composite -> daug Component)
        private readonly List<IDefenseComponent> _children = new();

        public void Add(IDefenseComponent item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _children.Add(item);
        }

        public void Remove(IDefenseComponent item)
        {
            _children.Remove(item);
        }

        public IDefenseComponent GetChild(int index)
        {
            return _children[index];
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
