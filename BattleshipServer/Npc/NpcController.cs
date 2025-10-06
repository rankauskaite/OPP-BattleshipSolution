using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public sealed class NpcController
    {
        private INpcShotStrategy _current;
        private readonly IStrategySelector _selector;

        public NpcController(IStrategySelector selector, INpcShotStrategy initial)
        {
            _selector = selector;
            _current  = initial;
        }

        // Patogus ctor, jei nori inicializuoti pagal key
        public NpcController(IStrategySelector selector, string initialKey)
            : this(selector, ShotStrategyFactory.Create(initialKey)) { }

        /// <summary>
        /// Kontekstas prieš kiekvieną šūvį parenka/permąsto strategiją pagal žaidimo būseną.
        /// </summary>
        public (int x, int y) Decide(BoardKnowledge knowledge)
        {
            var picked = _selector.Pick(knowledge, _current);

            // Persijungiam tik jei realiai keičiasi konkreti klasė (kad "netrūkčiotų")
            if (picked.GetType() != _current.GetType())
                _current = picked;

            return _current.ChooseTarget(knowledge);
        }
    }
}
