using System.Collections.Generic;
using BattleshipClient.Models;

namespace BattleshipClient.TemplateMethod
{
    public enum PlacementMode
    {
        Random,
        Edge,
        SpreadSafe
    }

    /// <summary>
    /// Template Method bazinė klasė laivų išdėstymui.
    /// </summary>
    public abstract class ShipPlacementTemplate
    {
        /// <summary>
        /// Template Method – bendra laivų išdėstymo eiga.
        /// </summary>
        public (List<ShipDto> ships, CellState[,] map) PlaceShips(int size, List<int> shipLengths)
        {
            var map = CreateEmptyMap(size);
            var ships = new List<ShipDto>();

            Initialize(size, shipLengths, map, ships);

            foreach (var length in shipLengths)
            {
                PlaceSingleShip(size, length, map, ships);
            }

            Finalize(size, shipLengths, map, ships);

            return (ships, map);
        }

        protected virtual CellState[,] CreateEmptyMap(int size)
        {
            // numatytasis – tuščia lenta (CellState.Empty)
            return new CellState[size, size];
        }

        private void PlaceSingleShip(int size, int length, CellState[,] map, List<ShipDto> ships)
        {
            int tries = 0;
            bool placed = false;

            while (!placed && tries < MaxPlacementTries)
            {
                tries++;
                var (x, y, horiz) = ChoosePosition(size, length, map, ships);

                if (CanPlace(length, x, y, horiz, map))
                {
                    ApplyShip(length, x, y, horiz, map, ships);
                    placed = true;
                }
            }

            if (!placed)
            {
                HandlePlacementFailure(length);
            }
        }

        /// <summary>Maksimalus bandymų skaičius vienam laivui.</summary>
        protected virtual int MaxPlacementTries => 200;

        /// <summary>Hook – galima atlikti pasiruošimą prieš išdėstant laivus.</summary>
        protected virtual void Initialize(int size, List<int> shipLengths, CellState[,] map, List<ShipDto> ships) { }

        /// <summary>
        /// Abstraktus žingsnis – konkreti klasė nusprendžia, kur bandyti dėti laivą.
        /// </summary>
        protected abstract (int x, int y, bool horiz) ChoosePosition(
            int size,
            int length,
            CellState[,] map,
            List<ShipDto> ships);

        /// <summary>
        /// Bendras validavimo žingsnis – bendra logika visoms strategijoms.
        /// </summary>
        protected virtual bool CanPlace(int length, int x, int y, bool horiz, CellState[,] map)
        {
            if (horiz && x + length > map.GetLength(1)) return false;
            if (!horiz && y + length > map.GetLength(0)) return false;

            for (int i = 0; i < length; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                if (map[cy, cx] != CellState.Empty) return false;
            }

            return true;
        }

        /// <summary>
        /// Bendras žingsnis – pažymime laivo langelius ir pridedame į ships sąrašą.
        /// </summary>
        protected virtual void ApplyShip(int length, int x, int y, bool horiz, CellState[,] map, List<ShipDto> ships)
        {
            for (int i = 0; i < length; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                map[cy, cx] = CellState.Ship;
            }

            ships.Add(new ShipDto
            {
                x = x,
                y = y,
                len = length,
                dir = horiz ? "H" : "V"
            });
        }

        /// <summary>
        /// Hook – kas nutinka, jei nepavyko išdėstyti laivo.
        /// </summary>
        protected virtual void HandlePlacementFailure(int shipLength)
        {
            // galima loginti arba vėliau pridėti išimtį, bet dabar tyli
        }

        /// <summary>
        /// Hook – pabaigos žingsnis, jei reikia.
        /// </summary>
        protected virtual void Finalize(int size, List<int> shipLengths, CellState[,] map, List<ShipDto> ships)
        {
        }
    }
}