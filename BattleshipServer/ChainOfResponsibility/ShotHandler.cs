using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipServer.ChainOfResponsibility
{
    public abstract class ShotHandler
    {
        protected ShotHandler? _next;
        public ShotHandler SetNext(ShotHandler next)
        {
            _next = next;
            return this;
        }

        public async Task HandleAsync(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            bool handled = await ProcessAsync(manager, player, dto);
            if (!handled && _next != null)
            {
                await _next.HandleAsync(manager, player, dto);
            }
        }

        protected abstract Task<bool> ProcessAsync(GameManager manager, PlayerConnection player, MessageDto dto);
    }
}
