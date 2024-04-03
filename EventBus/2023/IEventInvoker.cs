using Type = System.Type;

namespace Game.EventSystem
{
    public interface IEventInvoker
    {
        void InvokeEvent(Type eventType, IEventParameter eventParam = null);
    }
}