using System;

namespace qoooo.Parameters.Model
{
    // 別のstate ownerになってはいけないconsumer向けのread-only value view。
    public interface IReadOnlyValue<out T> { T Value { get; } }

    // application componentが所有するtyped Parameterによって実装されるmutable value boundary。
    public interface IValue<T> : IReadOnlyValue<T>
    {
        event Action<T> Changed;
        bool TrySetValue(T value);
    }
}
