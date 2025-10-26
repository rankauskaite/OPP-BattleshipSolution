using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public sealed class BotOrchestrator : IBotPlayerController
    {
        private readonly Game _game;
        private readonly Guid _botId;
        private readonly BoardKnowledge _k;
        private readonly NpcController _ctrl;
        private const int W = 10, H = 10;
        public Guid BotId => _botId;


        public BotOrchestrator(Game game, Guid botId, IStrategySelector selector, string initialKey = "checkerboard")
        {
            _game = game;
            _botId = botId;
            _k = new BoardKnowledge(W, H);
            _ctrl = new NpcController(selector, initialKey);

            _game.ShotResolved += OnShotResolved;
        }

        public void OnShotResolved(Guid shooterId, int x, int y, bool hit, bool sunk, List<(int x,int y)> sunkCells)
        {
            // Mus domina BOTO šūviai į žmogų -> atnaujinam žinias
            if (shooterId == _botId)
            {
                if (sunk && sunkCells != null && sunkCells.Count > 0)
                {
                    _k.MarkSunk(sunkCells.Select(c => new Cell(c.x, c.y)));
                }
                else
                {
                    // hit/miss (jei ne "whole_down")
                    _k.MarkShot(new Cell(x, y), hit);
                }
            }
        }

        public async Task MaybePlayAsync()
        {
            while (_game.CurrentPlayerId == _botId)
            {
                var (tx, ty) = _ctrl.Decide(_k);
                await _game.ProcessShot(_botId, tx, ty, isDoubleBomb: false);
            }
        }
    }
}
