using System;
using BattleshipServer.Domain;

namespace BattleshipServer.State
{
    /// <summary>
    /// State pattern interfeisas, aprašantis laivo būseną pagal jo sąveiką su šūviais.
    /// Būsenos: padėtas, pašautas, nušautas, išgelbėtas.
    /// </summary>
    public interface IShipState
    {
        Ship Ship { get; }

        /// <summary>
        /// Patogumui – dabartinės būsenos pavadinimas (naudinga debugging / testams).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Kviesk kiekvieną kartą, kai laivas pereina į šią būseną.
        /// </summary>
        void Enter();

        /// <summary>
        /// Įvyksta šūvis į šį konkretų laivą (į vieną iš jo langelių).
        /// Board masyvas čia reikalingas tam, kad prireikus būtų galima atnaujinti langelių būsenas.
        /// </summary>
        void Hit(CellState[,] board, int x, int y);

        /// <summary>
        /// Bandom išgelbėti laivą. Pagal užduotį – galima tik kai laivas pašautas, bet dar nenušautas.
        /// </summary>
        void Save(CellState[,] board);
    }
}