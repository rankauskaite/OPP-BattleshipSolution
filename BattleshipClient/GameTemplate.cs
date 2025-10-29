using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient
{
    public class GameTemplate
    {
        public GameBoard GameBoard { get; private set; }
        public GameBoard EnemyBoard { get; private set; }

        public List<int> Ships { get; private set; }
        public Dictionary<string, int> Powerups { get; private set; }

        public GameTemplate(int n)
        {
            this.GameBoard = new GameBoard(n);
            this.EnemyBoard = new GameBoard(n);
            this.Ships = new List<int>();
            this.Powerups = new Dictionary<string, int>();
        }

        public GameTemplate(int n, List<int> ships, Dictionary<string, int> powerups)
        {
            this.GameBoard = new GameBoard(n);
            this.EnemyBoard = new GameBoard(n);
            this.Ships = ships;
            this.Powerups = powerups;
        }
    }
}
