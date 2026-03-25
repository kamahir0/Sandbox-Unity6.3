using System;
using System.Collections.Generic;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// ページ型名をキーとし、同一型のインスタンスを最大 MaxPerType 個プールする。
    /// </summary>
    public sealed class DebugPagePool
    {
        public const int MaxPerType = 2;

        private readonly Dictionary<string, Queue<DebugPage>> _pool = new();
        private readonly Dictionary<string, Func<DebugPage>> _factories = new();

        /// <summary>
        /// プールに pageName が登録済み（空でもキーが存在する）か返す。
        /// NavigationButton の循環防止マーカーとして使う。
        /// </summary>
        public bool Contains(string pageName) => _pool.ContainsKey(pageName);

        /// <summary>
        /// キーだけを登録して空のキューを確保する（循環防止マーカー）。
        /// </summary>
        public void Reserve(string pageName)
        {
            if (!_pool.ContainsKey(pageName))
            {
                _pool[pageName] = new Queue<DebugPage>();
            }
        }

        /// <summary>
        /// ファクトリを登録する。
        /// </summary>
        public void RegisterFactory(string pageName, Func<DebugPage> factory)
        {
            _factories[pageName] = factory;
        }

        /// <summary>
        /// インスタンスをプールに追加する。
        /// </summary>
        public void Add(string pageName, DebugPage page)
        {
            if (!_pool.TryGetValue(pageName, out var queue))
            {
                queue = new Queue<DebugPage>();
                _pool[pageName] = queue;
            }

            if (queue.Count < MaxPerType)
            {
                queue.Enqueue(page);
            }
        }

        /// <summary>
        /// プールからインスタンスを1つ借りる。
        /// キューが空なら false を返す。
        /// </summary>
        public bool TryRent(string pageName, out DebugPage page)
        {
            if (_pool.TryGetValue(pageName, out var queue) && queue.Count > 0)
            {
                page = queue.Dequeue();
                return true;
            }

            page = null;
            return false;
        }

        /// <summary>
        /// ページをプールへ返却する。page.name をキーとして使用する。
        /// MaxPerType を超えた分は DOM から除去して破棄する。
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
        /// ファクトリから新規インスタンスを生成し、Configure を実行して返す。
        /// ファクトリ未登録なら null を返す。
        /// </summary>
        public DebugPage CreateNew(string pageName)
        {
            if (!_factories.TryGetValue(pageName, out var factory))
            {
                return null;
            }

            var page = factory();
            page.Configure(new DebugPageBuilder(page, this));
            return page;
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
