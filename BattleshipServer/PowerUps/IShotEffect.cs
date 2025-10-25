// PowerUps/IShotEffect.cs
namespace BattleshipServer.PowerUps
{
    public interface IShotEffect
    {
        // Grąžina true, jei (x,y) laive pritaikytas poveikis privertė laivą būti nuskendusį
        bool AfterCellHit(int x, int y, int[,] targetBoard, List<Game.Ship> targetShips);
    }

    public sealed class NoopEffect : IShotEffect
    {
        public bool AfterCellHit(int x, int y, int[,] b, List<Game.Ship> s) => false;
    }

    // SuperDamage: pataikius į laivą – pažymi visą laivą kaip nuskendusį (būseną)
    public sealed class SuperDamageEffect : IShotEffect
    {
        public bool AfterCellHit(int x, int y, int[,] board, List<Game.Ship> ships)
        {
            var ship = ships.FirstOrDefault(s =>
                (s.Horizontal && y == s.Y && x >= s.X && x < s.X + s.Len) ||
                (!s.Horizontal && x == s.X && y >= s.Y && y < s.Y + s.Len));
            if (ship == null) return false;

            // Pažymim visą laivą kaip nuskendusį (keičiam lentą)
            ship.setAsSunk(board);
            return true;
        }
    }
}
