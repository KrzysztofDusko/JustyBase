namespace JustyBase.Editor;
public class CommonPropertyChangedArgs<T>
{
    public T OldValue { get; }

    public T NewValue { get; }

    public CommonPropertyChangedArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}