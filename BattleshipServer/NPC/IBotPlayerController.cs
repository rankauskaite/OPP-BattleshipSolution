using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BattleshipServer.Npc
{
    public interface IBotPlayerController
    {
        Guid BotId { get; }

        Task MaybePlayAsync();
        
        void OnShotResolved(Guid shooterId, int x, int y, bool hit, bool sunk, List<(int x, int y)> sunkCells);
    }
}
