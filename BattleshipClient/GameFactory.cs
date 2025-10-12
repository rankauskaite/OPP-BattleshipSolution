﻿using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient
{
    public interface AbstractGameFactory
    {
        int GetBoardSize();
        List<int> GetShipsLength();
        Dictionary<string, int> GetPowerups();
    }

    public class MiniGameFactory : AbstractGameFactory
    {
        public int GetBoardSize() => 6;

        public List<int> GetShipsLength() => new List<int> { 3, 2, 2, 2, 1 };

        public Dictionary<string, int> GetPowerups() => new Dictionary<string, int>
        {
            { "DoubleBomb",  2}
        };
    }

    public class StandartGameFactory : AbstractGameFactory
    {
        public int GetBoardSize() => 10;
        public List<int> GetShipsLength() => new List<int> { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        public Dictionary<string, int> GetPowerups() => new Dictionary<string, int>
        {
            { "DoubleBomb",  1}
        };
    }
}
