using System;
using System.Collections.Generic;

/// <summary>
/// ドメインイベントを購読者へ同期的に配信するシンプルなイベントバス。
/// ドメイン層とアプリケーション層を疎結合にするために使用する。
/// </summary>
public static class DomainEventDispatcher
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>指定したドメインイベントのハンドラーを登録する。</summary>
    public static void Register<T>(Action<T> handler) where T : IDomainEvent
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }
        list.Add(handler);
    }

    /// <summary>指定したドメインイベントのハンドラーを解除する。</summary>
    public static void Unregister<T>(Action<T> handler) where T : IDomainEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var list))
        {
            list.Remove(handler);
        }
    }

    /// <summary>ドメインイベントを発行し、登録済みのハンドラーを同期的に呼び出す。</summary>
    public static void Dispatch<T>(T domainEvent) where T : IDomainEvent
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list)) return;

        foreach (var handler in list.ToArray())
        {
            ((Action<T>)handler)(domainEvent);
        }
    }

    /// <summary>全ハンドラーを一括解除する。シーン破棄時などに呼び出す。</summary>
    public static void Clear()
    {
        _handlers.Clear();
    }
}
