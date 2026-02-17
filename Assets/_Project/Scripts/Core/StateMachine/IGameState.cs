using System.Threading.Tasks;

namespace MBHS.Core.StateMachine
{
    public interface IGameState
    {
        Task Enter();
        Task Exit();
        void Update();
    }
}
