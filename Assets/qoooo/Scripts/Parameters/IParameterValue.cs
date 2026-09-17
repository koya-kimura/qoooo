using System;

namespace qoooo.Parameters
{
    public interface IReadOnlyValue<out T> { T Value { get; } }

    public interface IValue<T> : IReadOnlyValue<T>
    {
        event Action<T> Changed;
        bool TrySetValue(T value);
    }
}
