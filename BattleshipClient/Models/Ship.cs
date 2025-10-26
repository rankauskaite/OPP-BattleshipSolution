using BattleshipClient.Observers;
using System.Collections.Generic;
using System.Linq;

namespace BattleshipClient.Models
{
    public class Ship
    {
        private readonly GameEventManager _eventManager;
        public readonly string _shooterName;
        public readonly int _length;
        public readonly List<bool> _hits;
        public readonly string _direction;

        public int X { get; }
        public int Y { get; }
        public bool IsSunk => _hits.All(h => h);

        public Ship(GameEventManager eventManager, ShipDto dto, string shooterName)
        {
            _eventManager = eventManager;
            _shooterName = shooterName;
            X = dto.x;
            Y = dto.y;
            _length = dto.len;
            _direction = dto.dir;
            _hits = new List<bool>(new bool[_length]);
        }

        public string RegisterShot(int shotX, int shotY)
        {
            int relativePos = GetRelativePosition(shotX, shotY);
            if (relativePos >= 0 && relativePos < _length)
            {
                _hits[relativePos] = true;
                if (IsSunk)
                {
                    _eventManager.Notify("EXPLOSION", _shooterName);
                    return "whole_ship_down";
                }
                else
                {
                    _eventManager.Notify("HIT", _shooterName);
                    return "hit";
                }
            }

            _eventManager.Notify("MISS", _shooterName);
            return "miss";
        }


        private int GetRelativePosition(int shotX, int shotY)
        {
            if (_direction == "H")
            {
                if (shotY == Y && shotX >= X && shotX < X + _length)
                    return shotX - X;
            }
            else if (_direction == "V")
            {
                if (shotX == X && shotY >= Y && shotY < Y + _length)
                    return shotY - Y;
            }
            return -1;
        }
    }
}
