using KH;

/// <summary>
/// Base class for scene-scoped singleton managers.
/// <code>public class ExampleManager : KHSingleton&lt;ExampleManager&gt; { ... }</code>
/// Gives every subclass a static <c>Ins</c>, the duplicate-instance error, and automatic cleanup
/// when the instance is destroyed.
/// <para>If a subclass needs its own Awake/OnDestroy it must <c>override</c> them and call
/// <c>base.Awake()</c> / <c>base.OnDestroy()</c>, otherwise <c>Ins</c> is never set or cleared.</para>
/// <para>Because <c>Ins</c> becomes null once the object is destroyed, code that unsubscribes from a
/// singleton during teardown must null-check it (scene teardown order is undefined).</para>
/// </summary>
public abstract class KHSingleton<T> : KHManagedBehaviour where T : KHSingleton<T>
{
    public static T Ins { get; private set; }

    protected virtual void Awake()
    {
        if (Ins == null)
            Ins = (T)this;
        else
            KHDebug.LogError($"More Than One Instance of type {typeof(T).Name}");
    }

    protected virtual void OnDestroy()
    {
        // Only clear if WE are the registered instance: a rejected duplicate must not null the real one.
        if (Ins == this)
            Ins = null;
    }
}