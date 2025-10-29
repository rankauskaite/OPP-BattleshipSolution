using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient
{
    public class AbstractGameFactory
    {
        public GameTemplate CreateStandartGame()
        {
            StandartGameFactory factory = new StandartGameFactory();
            return new GameTemplate(factory.GetBoardSize(), factory.GetShipsLength(), factory.GetPowerups());
        }

        public GameTemplate CreateMiniGame()
        {
            MiniGameFactory factory = new MiniGameFactory();
            return new GameTemplate(factory.GetBoardSize(), factory.GetShipsLength(), factory.GetPowerups());
        }
    }

    public class MiniGameFactory
    {
        public int GetBoardSize() => 6;

        public List<int> GetShipsLength() => new List<int> { 3, 2, 2, 2, 1 };

        public Dictionary<string, int> GetPowerups() => new Dictionary<string, int>
        {
            { "DoubleBomb",  2}
        };
    }

    public class StandartGameFactory
    {
        public int GetBoardSize() => 10;
        public List<int> GetShipsLength() => new List<int> { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        public Dictionary<string, int> GetPowerups() => new Dictionary<string, int>
        {
            { "DoubleBomb",  1}
        };
    }
}
