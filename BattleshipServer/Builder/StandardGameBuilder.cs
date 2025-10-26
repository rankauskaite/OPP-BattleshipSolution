// Builders/StandardGameBuilder.cs
using System;
using System.Collections.Generic;
using BattleshipServer.Npc;
using BattleshipServer.Models;

namespace BattleshipServer.Builders
{
    public sealed class StandardGameBuilder : IGameSetupBuilder
    {
        private PlayerConnection? _p1, _p2;
        private GameManager? _mgr;
        private BattleshipServer.Data.Database? _db;
        private Game? _game;

        private List<ShipDto>? _humanShips;
        private bool _opponentRandom;
        private Func<Game, IBotPlayerController?>? _npcFactory;
        public IBotPlayerController? Orchestrator { get; private set; }

        public IGameSetupBuilder CreateShell(PlayerConnection p1, PlayerConnection p2, GameManager manager, BattleshipServer.Data.Database db)
        { _p1 = p1; _p2 = p2; _mgr = manager; _db = db; return this; }

        public IGameSetupBuilder ConfigureBoard()
        {
            _game = new Game(_p1!, _p2!, _mgr!, _db!);
            _game.SetGameMode(_p1!.Id, isStandartGameVal: true);
            _game.SetGameMode(_p2!.Id, isStandartGameVal: true);
            return this;
        }

        public IGameSetupBuilder ConfigureFleets(List<ShipDto> humanShips, bool opponentRandom)
        { _humanShips = humanShips; _opponentRandom = opponentRandom; return this; }

        public IGameSetupBuilder ConfigureNpc(Func<Game, IBotPlayerController?>? npcFactory)
        { _npcFactory = npcFactory; return this; }

        public Game Build()
        {
            // P1 – žmogus (iš payload)
            _game!.PlaceShips(_p1!.Id, _humanShips!);

            // P2 – botas (random)
            if (_opponentRandom)
            {
                var botShips = RandomFleetStandart();
                _game.PlaceShips(_p2!.Id, botShips);
            }

            // NPC (jei reikia)
            Orchestrator = _npcFactory?.Invoke(_game);

            return _game;
        }

        // ta pati „standart“ flotilė kaip GameManager.RandomFleet
        private static List<ShipDto> RandomFleetStandart()
        {
            var lens = new[] {4, 3, 3, 2, 2, 2, 1, 1, 1, 1};
            var rnd = new Random();
            var used = new int[10,10];
            var list = new List<ShipDto>();

            foreach (var L in lens)
            {
                bool placed = false;
                for (int tries=0; tries<500 && !placed; tries++)
                {
                    bool horiz = rnd.Next(2)==0;
                    int x = rnd.Next(0, 10 - (horiz ? L : 0));
                    int y = rnd.Next(0, 10 - (horiz ? 0 : L));
                    if (CanPlace(used, x, y, L, horiz))
                    {
                        for (int i=0;i<L;i++)
                        {
                            int cx = x + (horiz? i:0);
                            int cy = y + (horiz? 0:i);
                            used[cy, cx] = 1;
                        }
                        list.Add(new ShipDto { X=x, Y=y, Len=L, Dir=horiz?"H":"V" });
                        placed = true;
                    }
                }
            }
            return list;

            static bool CanPlace(int[,] b, int x, int y, int len, bool h)
            {
                for (int i=0;i<len;i++)
                {
                    int cx = x + (h? i:0);
                    int cy = y + (h? 0:i);
                    if (cx<0||cx>=10||cy<0||cy>=10) return false;
                    if (b[cy,cx]!=0) return false;
                }
                return true;
            }
        }
    }
}
