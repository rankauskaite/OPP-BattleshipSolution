using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    /// <summary>
    /// Bazinė laivo būsenų klasė – daugumai veiksmų pagal nutylėjimą meta klaidą,
    /// o konkretūs state'ai perrašo tik tai, kas jiems leidžiama.
    /// </summary>
    public abstract class ShipStateBase : IShipState
    {
        public Ship Ship { get; }
        public abstract string Name { get; }

        protected ShipStateBase(Ship ship)
        {
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
        }

        public virtual void Enter()
        {
            // Pagal nutylėjimą – nieko.
        }

        public virtual void Hit(CellState[,] board, int x, int y)
        {
            throw new InvalidOperationException($"Negalima šaudyti į laivą būsenos {Name} metu.");
        }

        public virtual void Save(CellState[,] board)
        {
            throw new InvalidOperationException($"Negalima išgelbėti laivo būsenos {Name} metu.");
        }
    }
}