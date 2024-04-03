namespace Game.EventSystem
{
    public interface IEventListener
    {
        void SubscribeToEvents();

        void UnsubscribeFromEvents();
    }
}