using System;
using System.Collections.Generic;

namespace Lilja.DebugUI
{
    /// <summary>
    /// ページ名をキーとして、ページインスタンスを1つキャッシュする。
    /// 同一ページ型のインスタンスは常にただ1つ。
    /// </summary>
    internal sealed class DebugPageCache
    {
        private readonly Dictionary<string, DebugPage> _cache = new();

        internal bool Contains(string pageName) => _cache.ContainsKey(pageName);

        /// <summary>
        /// ページを登録する。インスタンスを1つ生成してキャッシュする。
        /// 既に登録済みの場合は何もしない。
        /// </summary>
        internal void Register(string pageName, Func<DebugPage> factory)
        {
            if (Contains(pageName)) return;

            // インスタンス生成前にスロットを確保し、Configure() 内の再帰的な Register 呼び出しをガードする
            _cache[pageName] = null;
            var page = factory();
            page.name = pageName;
            PreparePage(page);
            _cache[pageName] = page;
        }

        /// <summary>
        /// 外部から渡された既存ページを初期化する。
        /// ルートページや一時ページなど、キャッシュ経由でない場合にも統一的な初期化を行う。
        /// </summary>
        internal void PreparePage(DebugPage page)
        {
            page.Configure(new DebugUIBuilder(page, this));
        }

        /// <summary>
        /// キャッシュされたインスタンスを返す。未登録なら null を返す。
        /// </summary>
        internal DebugPage Get(string pageName)
        {
            return _cache.TryGetValue(pageName, out var page) ? page : null;
        }

        /// <summary>
        /// キャッシュされている全ページを列挙する。
        /// </summary>
        internal IEnumerable<DebugPage> GetAllCachedPages() => _cache.Values;

        /// <summary>
        /// ページ使用完了時の処理。スクロール位置をリセットする。
        /// </summary>
        internal void Return(DebugPage page)
        {
            page.ResetScrollPosition();
        }
    }
}
