using Lilja.DebugMenu;
using UnityEngine;
using UnityEngine.UIElements;

public class MockBuilder : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private void OnEnable()
    {
        DebugMenuManager.Initialize(_uiDocument, new RootPage());
    }

    public class RootPage : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.Button("次へ2");
            builder.Foldout("折り畳み", b =>
            {
                b.Button("次へ3");
            });
            builder.Button("次へ2");
            builder.NavigationButton<MockBuilder.Page1>();
        }
    }

    public class Page1 : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.Button("次へ1");
            builder.Button("次へ2");
            builder.Foldout("折り畳み", b =>
            {
                b.Button("次へ3-1");
                b.Button("次へ3-2");
            });
            builder.NavigationButton<Page1>();
            builder.NavigationButton<Page2>();
        }
    }

    public class Page2 : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.Button("次へ2");
            builder.Button("次へ2");
            builder.NavigationButton<Page2>();
        }
    }
}
