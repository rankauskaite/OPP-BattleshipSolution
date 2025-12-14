using System.Threading.Tasks;

namespace BattleshipClient.Mediator
{
    public interface IGameMediator
    {
        Task RequestShotAsync(int x, int y);
    }
}
