using System.Collections.Generic;
using UnityEngine;

public interface IKHSubsystem
{
    void IOnEnable() { }
    void IOnDisable() { }
    void IAwake() { }
    void IStart() { }
    void IUpdate() { }
    void IFixedUpdate() { }
    void IOnTriggerEnter2D(Collider2D collision) { }
    void IReset() { }
}

public static class KHHelper
{
    public static void OnEnableAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IOnEnable();
    }

    public static void OnDisableAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IOnDisable();
    }

    public static void AwakeAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IAwake();
    }

    public static void StartAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IStart();
    }

    public static void UpdateAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IUpdate();
    }

    public static void FixedUpdateAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IFixedUpdate();
    }

    public static void OnTriggerEnter2DAll(this List<IKHSubsystem> systems, Collider2D collision)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IOnTriggerEnter2D(collision);
    }

    public static void ResetAll(this List<IKHSubsystem> systems)
    {
        if (systems == null)
            return;

        foreach (var system in systems)
            system.IReset();
    }
}