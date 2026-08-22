using System.Collections.Generic;
using KH;
using UnityEngine;

public interface IKHSubsystem
{
    void IOnEnable() { }
    void IOnDisable() { }
    void IAwake() { }
    void IStart() { }
    void IUpdate() { }
    void IFixedUpdate() { }
    void IOnDrawGizmosSelected() { }
    void IOnTriggerEnter2D(Collider2D collision) { }
}

public static class KHHelper
{
    public static void OnEnableAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IOnEnable());
    }

    public static void OnDisableAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IOnDisable());
    }

    public static void AwakeAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IAwake());
    }

    public static void StartAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IStart());
    }

    public static void UpdateAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IUpdate());
    }

    public static void FixedUpdateAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IFixedUpdate());
    }

    public static void OnTriggerEnter2DAll(this List<IKHSubsystem> systems, Collider2D collision)
    {
        systems.KHForEach(p => p.IOnTriggerEnter2D(collision));
    }

    public static void OnDrawGizmosSelectedAll(this List<IKHSubsystem> systems)
    {
        systems.KHForEach(p => p.IOnDrawGizmosSelected());
    }
}