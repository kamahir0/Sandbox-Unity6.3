using System;
using System.Collections.Generic;

namespace Lilja.DebugUI
{
    /// <summary>
    /// ページ名をキーとし、同一ページのインスタンスを最大 MaxPerType 個プールする。
    /// </summary>
    public sealed class DebugPagePool
    {
        private readonly Dictionary<string, Queue<DebugPage>> _pool = new();
        private readonly Dictionary<string, Func<DebugPage>> _factories = new();

        public const int MaxPerType = 2;

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
            PreparePage(page);
            if (_pool[pageName].Count < MaxPerType)
            {
                _pool[pageName].Enqueue(page);
            }
        }

        /// <summary>
        /// 外部から渡された既存ページを初期化する。
        /// ルートページや一時ページなど、プール経由でない場合にも統一的な初期化を行う。
        /// </summary>
        internal void PreparePage(DebugPage page)
        {
            page.Configure(new DebugPageBuilder(page, this));
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
            PreparePage(page);
            return page;
        }

        /// <summary>プールに格納されている全ページを列挙する（事前アタッチ用）</summary>
        public IEnumerable<DebugPage> GetAllPooledPages()
        {
            foreach (var queue in _pool.Values)
            {
                foreach (var page in queue)
                {
                    yield return page;
                }
            }
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
    }
}
