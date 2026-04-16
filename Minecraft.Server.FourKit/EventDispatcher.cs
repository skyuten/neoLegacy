using System.Reflection;
using Minecraft.Server.FourKit.Event;

namespace Minecraft.Server.FourKit;
internal sealed class EventDispatcher
{
    private readonly struct RegisteredHandler : IComparable<RegisteredHandler>
    {
        public readonly Listener Instance;
        public readonly MethodInfo Method;
        public readonly EventPriority Priority;
        public readonly bool IgnoreCancelled;

        public RegisteredHandler(Listener instance, MethodInfo method, EventPriority priority, bool ignoreCancelled)
        {
            Instance = instance;
            Method = method;
            Priority = priority;
            IgnoreCancelled = ignoreCancelled;
        }

        public int CompareTo(RegisteredHandler other) => Priority.CompareTo(other.Priority);
    }

    private readonly Dictionary<Type, List<RegisteredHandler>> _handlers = new();
    private readonly object _lock = new();
    public void Register(Listener listener)
    {
        var methods = listener.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        lock (_lock)
        {
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<Event.EventHandlerAttribute>();
                if (attr == null)
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    Console.WriteLine($"[FourKit] Warning: @EventHandler method {method.Name} must have exactly 1 parameter, skipping.");
                    continue;
                }

                var eventType = parameters[0].ParameterType;
                if (!typeof(Event.Event).IsAssignableFrom(eventType))
                {
                    Console.WriteLine($"[FourKit] Warning: @EventHandler method {method.Name} parameter must extend Event, skipping.");
                    continue;
                }

                if (!_handlers.TryGetValue(eventType, out var list))
                {
                    list = new List<RegisteredHandler>();
                    _handlers[eventType] = list;
                }

                list.Add(new RegisteredHandler(listener, method, attr.Priority, attr.IgnoreCancelled));
                _handlers[eventType] = list.OrderBy(h => h.Priority).ToList();
            }
        }
    }
    public void Fire(Event.Event evt)
    {
        List<RegisteredHandler>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(evt.GetType(), out handlers))
                return;

            handlers = new List<RegisteredHandler>(handlers);
        }

        var cancellable = evt as Cancellable;

        foreach (var handler in handlers)
        {
            if (handler.IgnoreCancelled && cancellable != null && cancellable.isCancelled())
                continue;

            try
            {
                handler.Method.Invoke(handler.Instance, [evt]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FourKit] Error in handler {handler.Instance.GetType().Name}.{handler.Method.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
