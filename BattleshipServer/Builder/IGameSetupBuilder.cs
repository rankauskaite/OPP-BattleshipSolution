// Builders/IGameSetupBuilder.cs
using System;
using System.Collections.Generic;
using BattleshipServer.Npc;

namespace BattleshipServer.Builders
{
    public interface IGameSetupBuilder
    {
        // 1) žaidimo „korpusas“: kas žais ir kur
        IGameSetupBuilder CreateShell(PlayerConnection p1, PlayerConnection p2, GameManager manager, BattleshipServer.Data.Database db);

        // 2) lenta/režimas
        IGameSetupBuilder ConfigureBoard();

        // 3) flotilės išdėstymas (p1 – iš kliento; p2 – random jei botui)
        IGameSetupBuilder ConfigureFleets(List<BattleshipServer.Models.ShipDto> humanShips, bool opponentRandom);

        // 4) NPC (nebūtina): leist prijungti BotOrchestrator
        IGameSetupBuilder ConfigureNpc(Func<Game, BotOrchestrator?>? npcFactory);

        // 5) pagaminti produktą
        Game Build();

        // grąžinam „pagamintą“ orkestratorių (jei ConfigureNpc buvo paduotas)
        BotOrchestrator? Orchestrator { get; }
    }
}
