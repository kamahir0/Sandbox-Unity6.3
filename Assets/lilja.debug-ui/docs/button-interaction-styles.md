# ボタンのホバー・クリック外観カスタマイズ

## 背景と問題

Unity UI Toolkit のデフォルトテーマ (`unity-theme://default`) は、ボタンのホバー・クリック時に `background-color` を強制上書きする。
このため、以下のアプローチはいずれも機能しない。

| 試みたアプローチ | 結果 | 理由 |
|---|---|---|
| USS `:hover` に `background-color` | 効かない | デフォルトテーマが後段で上書き |
| USS `!important` | 効かない | 同上（Unity の実装上 `!important` が無効） |
| `PointerDownEvent` コールバックで `style.backgroundColor` | ホバーは効く、クリックは効かない | `Clickable` が `StopImmediatePropagation()` を呼ぶため PointerDownEvent が届かない |

ただし `color` や `border-color` は USS `:hover` で変更できる（background-color だけが特殊）。

---

## 解決策

### ホバー色（PointerEnter / PointerLeave）

`PointerEnterEvent` / `PointerLeaveEvent` に登録したコールバックで `style.backgroundColor` を直接セットする。
`Clickable` は `PointerDownEvent` のみを止め、Enter/Leave は止めないため、これらのイベントは届く。

```csharp
button.RegisterCallback<PointerEnterEvent>(_ => button.style.backgroundColor = hoverColor);
button.RegisterCallback<PointerLeaveEvent>(_ => button.style.backgroundColor = StyleKeyword.Null);
```

### クリック色（PointerDown / PointerUp）

`Button` の `Clickable` マニピュレータは `PointerDownEvent` 内で `StopImmediatePropagation()` を呼ぶ。
そのため後から登録した `PointerDownEvent` コールバックは一切届かない。

解決策は `Clickable` のサブクラスを作り、`ProcessDownEvent` / `ProcessUpEvent` をオーバーライドしてフックを挿入すること。

```csharp
internal class InteractiveClickable : Clickable
{
    public event Action OnPressed;
    public event Action OnReleased;

    public InteractiveClickable() : base((Action)null) { }

    protected override void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
    {
        OnPressed?.Invoke();               // base より先に呼ぶ
        base.ProcessDownEvent(evt, localPosition, pointerId); // ここで StopImmediatePropagation が呼ばれる
    }

    protected override void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
    {
        base.ProcessUpEvent(evt, localPosition, pointerId);
        OnReleased?.Invoke();
    }
}
```

このクラスで `button.clickable` を置き換えてから、`OnPressed` / `OnReleased` に色変更ロジックを登録する。

```csharp
var clickable = new InteractiveClickable();
button.clickable = clickable;
clickable.OnPressed  += () => button.style.backgroundColor = activeColor;
clickable.OnReleased += () => button.style.backgroundColor = isOver ? (StyleColor)hoverColor : StyleKeyword.Null;
```

> **重要**: `button.clickable` を置き換えると元の `clickable.clicked` イベントの購読が失われる。
> `button.clicked += handler` は **Register より後に** 呼ぶこと。

---

## CSS カスタムプロパティによるテーマ対応

ホバー色・アクティブ色は USS のカスタムプロパティで定義し、C# 側で `ICustomStyle.TryGetValue` を使って読み取る。

### 注意: `:root` では読み取れない

`ICustomStyle` は **その要素に直接適用されたプロパティ** しか読み取れない。
`:root` に定義した変数は USS の `var()` では参照できるが、C# の `TryGetValue` では取得できない。

```css
/* NG: :root に書いても ICustomStyle から読めない */
:root { --hover-color: #0062CC; }

/* OK: 要素のクラスに直接書く */
.c-button--primary { --hover-color: #0062CC; --active-color: #004C9E; }
```

### .tss のインポート順と上書き

`.tss` のインポート順は以下の通り。

```
@import url("unity-theme://default");
@import url("DebugMenuXxxTheme.uss");   ← テーマ（後から上書きされる側）
@import url("DebugMenu.uss");            ← ベーススタイル（最後なので同セレクタでは勝つ）
```

`DebugMenu.uss` の同セレクタ（例: `.c-button--primary`）に `--hover-color` を書くと
テーマの定義を上書きしてしまう。**ホバー色・アクティブ色は `DebugMenu.uss` には書かず、テーマ側のみで定義すること。**

### 読み取り方（C#）

```csharp
static readonly CustomStyleProperty<Color> s_HoverColor  = new("--hover-color");
static readonly CustomStyleProperty<Color> s_ActiveColor = new("--active-color");

button.RegisterCallback<CustomStyleResolvedEvent>(e =>
{
    e.customStyle.TryGetValue(s_HoverColor,  out hoverColor);
    e.customStyle.TryGetValue(s_ActiveColor, out activeColor);
});
```

---

## 実装場所

| ファイル | 役割 |
|---|---|
| `src/Runtime/DebugMenu/Scripts/DebugControls/DebugControls.cs` | `InteractiveClickable` と `ButtonInteractionHelper` の定義 |
| `src/Runtime/DebugMenu/StyleSheets/DebugMenuDefaultTheme.uss` | `--hover-color` / `--active-color` の Default テーマ値 |
| `src/Runtime/DebugMenu/StyleSheets/DebugMenuDarkTheme.uss` | `--hover-color` / `--active-color` の Dark テーマ値 |

### テーマファイルの記述例

```css
/* DefaultTheme.uss */
.c-button--primary   { --hover-color: #0062CC; --active-color: #004C9E; }
.c-button--secondary { --hover-color: #E8E8E8; --active-color: #D0D0D0; }
.c-button--danger    { --hover-color: #C62828; --active-color: #9A0007; }
.c-nav-button        { --hover-color: #EEEEEE; --active-color: #D5D5D5; }
.c-menu-window__back-button,
.c-menu-window__back-to-root-button { --hover-color: #F0F0F0; --active-color: #E0E0E0; }
```

新しいテーマを作る場合は、上記クラスセレクタに `--hover-color` と `--active-color` を定義するだけでよい。
