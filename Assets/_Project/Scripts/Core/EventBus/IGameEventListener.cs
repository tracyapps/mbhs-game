namespace MBHS.Core.Events
{
    public interface IGameEventListener
    {
        void OnEventRaised();
    }

    public interface IGameEventListener<in T>
    {
        void OnEventRaised(T value);
    }
}
