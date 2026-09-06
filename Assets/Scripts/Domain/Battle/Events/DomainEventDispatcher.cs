using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// ドメインイベントを購読者へ配信するシンプルなイベントバス。
    /// ドメイン層とアプリケーション層を疎結合にするために使用する。
    /// ハンドラーは <see cref="UniTask"/> を返し、<see cref="Dispatch{T}"/> は
    /// 登録順に各ハンドラーの完了を await する。これにより「敵を全滅させた」等の
    /// クリア条件成立時、後続のバトル処理を進める前にクリア演出・シナリオ再生の
    /// 完了までディスパッチ元を確実に待たせることができる。
    /// </summary>
    public static class DomainEventDispatcher
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        /// <summary>指定したドメインイベントのハンドラーを登録する。</summary>
        public static void Register<T>(Func<T, UniTask> handler) where T : IDomainEvent
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
        public static void Unregister<T>(Func<T, UniTask> handler) where T : IDomainEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>
        /// ドメインイベントを発行し、登録済みのハンドラーを登録順に await しながら呼び出す。
        /// 呼び出し側は戻り値の <see cref="UniTask"/> を await することで、
        /// 全ハンドラーの処理完了までバトルの進行を止められる。
        /// </summary>
        public static async UniTask Dispatch<T>(T domainEvent) where T : IDomainEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // ハンドラー内で Unregister されても列挙が壊れないようスナップショットを取る。
            foreach (var handler in list.ToArray())
            {
                await ((Func<T, UniTask>)handler)(domainEvent);
            }
        }

        /// <summary>全ハンドラーを一括解除する。シーン破棄時などに呼び出す。</summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
