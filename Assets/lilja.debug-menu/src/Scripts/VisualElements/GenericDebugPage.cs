using System;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// ラムダ式でUIを構成できる汎用DebugPage。
    /// 型として同じ GenericDebugPage でも、name が異なれば別ページとして履歴管理される。
    /// </summary>
    public sealed class GenericDebugPage : DebugPage
    {
        private readonly Action<IDebugPageBuilder> _configure;

        public GenericDebugPage(string name, Action<IDebugPageBuilder> configure)
        {
            this.name = name;
            _configure = configure;
        }

        public override void Configure(IDebugPageBuilder builder) => _configure?.Invoke(builder);
    }
}
