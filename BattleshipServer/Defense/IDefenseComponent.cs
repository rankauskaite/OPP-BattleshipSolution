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
    /// GoF Composite "Component".
    ///
    /// UML'e dažniausiai matysi Operation(), Add(), Remove(), GetChild().
    /// Mūsų domenui Operation turi (x,y), nes gynyba priklauso nuo langelio.
    /// </summary>
    public interface IDefenseComponent
    {
        // Domeninis "Operation" – pagal koordinatę pasako, koks gynybos režimas galioja langeliui.
        DefenseMode GetMode(int x, int y);

        // GoF Operation(x,y) – paliekam kaip alias, kad UML atitiktų "Operation()".
        DefenseMode Operation(int x, int y) => GetMode(x, y);

        // GoF Composite valdymo metodai.
        // Leaf pagal nutylėjimą jų nepalaiko (mėto išimtį) – kaip klasikinėje Composite interpretacijoje.
        void Add(IDefenseComponent item) => throw new NotSupportedException("Leaf komponentas neturi vaikų.");
        void Remove(IDefenseComponent item) => throw new NotSupportedException("Leaf komponentas neturi vaikų.");
        IDefenseComponent GetChild(int index) => throw new NotSupportedException("Leaf komponentas neturi vaikų.");
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

    /// <summary>
    /// GoF Composite "Composite" – gali turėti daug vaikų (CellShield, AreaShieldComposite, ...).
    /// </summary>
    public class DefenseComposite : IDefenseComponent
    {
        protected readonly List<IDefenseComponent> Children = new();

        public virtual void Add(IDefenseComponent item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            Children.Add(item);
        }

        public virtual void Remove(IDefenseComponent item)
        {
            Children.Remove(item);
        }

        public virtual IDefenseComponent GetChild(int index) => Children[index];

        public virtual DefenseMode GetMode(int x, int y)
        {
            foreach (var child in Children)
            {
                // Naudojam GoF "Operation" alias (interface default metodą), kad atitiktų UML.
                // Realiai jis kviečia child.GetMode(x,y).
                var mode = child.Operation(x, y);
                if (mode != DefenseMode.None)
                    return mode;
            }
            return DefenseMode.None;
        }
    }

    /// <summary>
    /// 3x3 (ar bendrai stačiakampio) zona, sudaryta iš kitų "Leaf" (CellShield) objektų.
    ///
    /// Tai yra tai, ko tau trūko: vietoje koordinatinių skaičiavimų (x1..x2, y1..y2)
    /// mes realiai sukonstruojam 9 vaikinius langelius ir Composite tik per juos deleguoja Operation().
    /// </summary>
    public sealed class AreaShieldComposite : DefenseComposite
    {
        public int X1 { get; }
        public int Y1 { get; }
        public int X2 { get; }
        public int Y2 { get; }
        public DefenseMode Mode { get; }

        public AreaShieldComposite(int x1, int y1, int x2, int y2, DefenseMode mode, int boardSize = 10)
        {
            // sutvarkom ribas ir suklampinam į lentą
            var minX = Math.Min(x1, x2);
            var minY = Math.Min(y1, y2);
            var maxX = Math.Max(x1, x2);
            var maxY = Math.Max(y1, y2);

            minX = Math.Clamp(minX, 0, boardSize - 1);
            minY = Math.Clamp(minY, 0, boardSize - 1);
            maxX = Math.Clamp(maxX, 0, boardSize - 1);
            maxY = Math.Clamp(maxY, 0, boardSize - 1);

            X1 = minX; Y1 = minY; X2 = maxX; Y2 = maxY;
            Mode = mode;

            for (int y = Y1; y <= Y2; y++)
                for (int x = X1; x <= X2; x++)
                    Add(new CellShield(x, y, Mode));
        }
    }
}
