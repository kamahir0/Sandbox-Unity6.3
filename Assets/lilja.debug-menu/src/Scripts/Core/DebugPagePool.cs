using System;
using System.Collections.Generic;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// ページ名をキーとし、同一ページのインスタンスを最大 MaxPerType 個プールする。
    /// </summary>
    public sealed class DebugPagePool
    {
        public const int MaxPerType = 2;

        private readonly Dictionary<string, Queue<DebugPage>> _pool = new();
        private readonly Dictionary<string, Func<DebugPage>> _factories = new();

        /// <summary>
        /// プールに pageName が登録済みか返す。循環防止チェックにも使う。
        /// </summary>
        public bool Contains(string pageName) => _pool.ContainsKey(pageName);

        /// <summary>
        /// ページを登録する。循環防止マーカー設置・ファクトリ登録・初回インスタンス生成を一括実行する。
        /// 既に登録済みの場合は何もしない。
        /// </summary>
        public void Register(string pageName, Func<DebugPage> factory)
        {
            if (Contains(pageName)) return;

            _pool[pageName] = new Queue<DebugPage>();

            Func<DebugPage> wrappedFactory = () =>
            {
                var p = factory();
                p.name = pageName;
                return p;
            };
            _factories[pageName] = wrappedFactory;

            var page = wrappedFactory();
            page.Configure(new DebugPageBuilder(page, this));
            if (_pool[pageName].Count < MaxPerType)
            {
                _pool[pageName].Enqueue(page);
            }
        }

        /// <summary>
        /// キーだけを登録して空のキューを確保する（ルートページ等の循環防止マーカー用）。
        /// </summary>
        internal void Reserve(string pageName)
        {
            if (!_pool.ContainsKey(pageName))
            {
                _pool[pageName] = new Queue<DebugPage>();
            }
        }

        /// <summary>
        /// プールからインスタンスを1つ借りる。キューが空なら新規生成して返す。ファクトリ未登録なら null を返す。
        /// </summary>
        public DebugPage Rent(string pageName)
        {
            if (_pool.TryGetValue(pageName, out var queue) && queue.Count > 0)
            {
                return queue.Dequeue();
            }
            return CreateNew(pageName);
        }

        private DebugPage CreateNew(string pageName)
        {
            if (!_factories.TryGetValue(pageName, out var factory)) return null;

            var page = factory();
            page.Configure(new DebugPageBuilder(page, this));
            return page;
        }

        /// <summary>
        /// ページをプールへ返却する。MaxPerType を超えた分は DOM から除去して破棄する。
        /// </summary>
        public void Return(DebugPage page)
        {
            var pageName = page.name;
            page.ResetScrollPosition();

            if (!_pool.TryGetValue(pageName, out var queue))
            {
                queue = new Queue<DebugPage>();
                _pool[pageName] = queue;
            }

            if (queue.Count < MaxPerType)
            {
                queue.Enqueue(page);
            }
            else
            {
                page.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// 他プールの内容をマージする（Foldout のネスト用）。
        /// </summary>
        public void Merge(DebugPagePool other)
        {
            foreach (var (key, otherQueue) in other._pool)
            {
                if (!_pool.TryGetValue(key, out var queue))
                {
                    queue = new Queue<DebugPage>();
                    _pool[key] = queue;
                }

                while (otherQueue.Count > 0 && queue.Count < MaxPerType)
                {
                    queue.Enqueue(otherQueue.Dequeue());
                }
            }

            foreach (var (key, factory) in other._factories)
            {
                if (!_factories.ContainsKey(key))
                {
                    _factories[key] = factory;
                }
            }
        }
    }
}
