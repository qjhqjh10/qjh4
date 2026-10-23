# 诊断：`MainMenuScene.Run` 唯一失败「（前提）首格有点击区」

- 会话：2026-10-23 · 只读诊断代理
- 日志：`d:/4/_tmp_view/t67_sync/menu.log`（合计 3966 通过 / 1 失败，`:83675`；失败重列 `:83687`）
- 唯一失败原文（`menu.log:15875`）：
  `[Menu]    ✗ （前提）首格有点击区 —— 期望 [True]，实得 [False]`
- 断言位置：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:3080`
  （`CheckTrue(owb != null, "（前提）首格有点击区");`）

## 一、分类结论

**α（断言写错 —— 查的对象是**点击前抓的、点击后已被重建销毁**的 Transform）**。

不是 δ（首格建出来了：`MainMenuScene.cs:3050` 的「左列首格 = 你的卡组」本条通过，`menu.log:15751`），
也不是 β（生产侧首格的点击区和其余 78 对**同一份** `BuildDeckCell` 里建的，没有「只有首格没有」）。

## 二、根因（一句话）

`MainMenuScene.cs:3049` 把首格 Transform 存进局部变量 `own`，随后 `MainMenuScene.cs:3074` 的
`preWb.Click()` 触发 `PickPair → PickPrebuilt → RebuildDeckRows`，把那批格节点**当场 `DestroyImmediate` 销毁重建**；
而 `MainMenuScene.cs:3078` 仍拿**旧的 `own`** 去找 `Hit` ⇒ 命中 Unity 的「假 null」⇒ 恒 `null` ⇒ 断言假。

## 三、证据链（逐条带出处）

### 1. 断言用的对象 = 点击**之前**抓的

- `MainMenuScene.cs:3049`：`var own = FindChild(pw.transform, PracticeModePopup.YourDeckCellName);`
- 这之后到失败那一行之间只有**读**操作（`3054/3057/3059/3064`），唯一的状态改变是 `3074` 的点击。
- `MainMenuScene.cs:3078`：`var oh = FindChild(own, "Hit");`（**没有重新找 `own`**）
- 对照：同文件后面几段**每次点完都重新按名字找** —— `3089`（`backCell`）、`3161`、`3180`、`3195`。
  ⇒ 本条是本文件里**唯一**一处跨越点击复用旧 Transform 的写法。

### 2. 那次点击真的会重建（销毁）整批格

`menu.log:15820-15860` 的调用栈原文：

```
CardPresentation.PracticeModePopup:PickPrebuilt (...PracticeModePopup.cs:2580)
CardPresentation.PracticeModePopup:PickPair (...PracticeModePopup.cs:2531)
CardPresentation.PracticeModePopup/<>c__DisplayClass317_1:<RebuildDeckRows>b__1 () (...PracticeModePopup.cs:1940)
CardPresentation.WindowButton:Click () (...WindowButton.cs:887)
```

- `PracticeModePopup.cs:2519` `PickPair` → `:2531` `PickPrebuilt(deck, pair.loreKey, i)`
- `PracticeModePopup.cs:2574`（PickPrebuilt 内）`if (_deckHolder != null) RebuildDeckRows(_deckHolder);`
- `PracticeModePopup.cs:1866-1870`：
  `RebuildDeckRows` 开头 `for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);`
- `RewardsWindow : MainMenuSubmenuWindow`（`Shell/RewardsWindow.cs:94`）⇒
  `Shell/MainMenuSubmenuWindow.cs:217-223`：
  `#if UNITY_EDITOR if (!Application.isPlaying) { DestroyImmediate(go); return; } #endif`
  ⇒ 批处理（非 playing）下**立即销毁**，旧 `own` 当场变假 null。

### 3. 假 null 之后 `FindChild` 静默返回 null（所以日志里没有异常）

- `Editor/MenuCheck.cs:141-147`：`if (parent == null) return null;` —— Unity 对已销毁对象重载的 `==` 判真
  ⇒ 不进 `GetComponentsInChildren` ⇒ 直接 `null`，**不抛 `MissingReferenceException`**。
- 佐证：`menu.log` 里 `MissingReference|NullReference|Exception` **零命中**。

### 4. 生产侧：首格的 `Hit` 确实会建（与 78 对**共用一份**）

- `PracticeModePopup.cs:1912-1919`（首格）与 `:1935-1942`（78 对）调的是**同一个** `BuildDeckCell(...)`。
- `PracticeModePopup.cs:2135`（`BuildDeckCell` 尾部，**无条件**）：
  `var rowHit = HitOn(cell.transform, cell.transform, "Hit", rr, onClick, QPrHit, null);`
- `PracticeModePopup.cs:3196-3242` `HitOn`：`ClipRectAbove` 通过后就 `new GameObject(name, RectTransform)` +
  `ImageQuad` + `hit.AddComponent<WindowButton>()`（`:3237`）并返回；**唯一的提前 `null`** 是 `:3215`
  `if (!MenuDraw.ClipRectAbove(parent, r, clip, out hr)) return null;`（= 整块在视口外）。
- 首格矩形 = `(270.08, 262.64) – (450.08, 442.64)`（`PracticeModePopup.cs:2019-2024` + 常量推导；
  与 `MainMenuScene.cs:3544` 那条 `Main image` 期望值一致），**完整落在**
  `Decks Scroll view/Viewport = (261.28, 262.64) – (634.88, 803.43)`（`:1821`）里 ⇒ `ClipRectAbove` 必然通过
  ⇒ 首格的 `Hit` + `WindowButton` **建得出来**。
- 经验佐证：点完重建之后，同批的**兄弟格**用**新找的引用**立刻取到了 `Hit`+`WindowButton` ——
  `menu.log:15812`（`:3073` 「第 1 对那格有点击区」通过）、`menu.log:15890`（`:3092` 「复位用」通过）；
  再往后多次重建之后，首格本身用**新找的引用**也拿得到 —— `menu.log:17174`（`:3528-3529`
  「（前提）`DeckRow_YourDeck`（格位 0）在」通过）。

### 5. 顺带后果：不是「1 红」，是「1 红 + 1 条断言没跑」

- `owb == null` ⇒ `MainMenuScene.cs:3081-3087` 的 `★ 点首格 ⇒ 选中我的卡组` **整块被跳过**。
- 证据：`grep -c "预组与那一对作废" menu.log` = **0**（该断言的文案一次都没出现在日志里）。
- 所以本轮**真正该跑的断言数少 1**，`3966 通过 / 1 失败` 里既丢了这条正测、又把它前面的前提判红。

## 四、最小修法（**只改测试**，不动生产代码）

在 `MainMenuScene.cs:3078` 之前**重新按名字找一次首格**（与本文件 `3089/3161/3180/3195` 同一写法）：

```csharp
// 🔴 2026-10-23 诊断修法：上一句 `preWb.Click()` 走 `PickPair → PickPrebuilt → RebuildDeckRows`
//   （PracticeModePopup.cs:2531 → :2574 → :1866-1870，编辑器下 DestroyImmediate）
//   ⇒ 第 3049 行抓的 `own` 已被销毁（Unity 假 null）⇒ 直接 FindChild 恒 null。
//   必须**重新按名字找** —— 同本文件 3089 / 3161 / 3180 / 3195 那几处的写法。
own = FindChild(pw.transform, PracticeModePopup.YourDeckCellName);
var oh = own != null ? FindChild(own, "Hit") : null;
var owb = oh != null ? oh.GetComponent<WindowButton>() : null;
CheckTrue(owb != null, "（前提）首格有点击区");
```

- 改动量：`1` 行替换 `own` 的来源 + 把 `FindChild` 加一道 null 守卫（`own` 是 `var` 局部，可直接重赋值）。
- 需要改生产代码吗？**不需要**。`PracticeModePopup.BuildDeckCell` / `HitOn` 对首格与 78 对走同一份，无缺陷。
- 改完预期：`3968 通过 / 0 失败`（红的那条转绿 `+1`，被跳过的 `★ 点首格…` 补跑 `+1`）。
  若顺手再加一条「重建之后首格还在」的前提断言，则 `3969 通过 / 0 失败`（属可选，不影响结论）。

### 为什么重找之后这条断言仍然有鉴别力（不是自证）

- 重找拿到的是**新格**，它的 `Hit` 回调是 `PracticeModePopup.cs:1917` `() => PickDeck(yiForHit)`，
  `yiForHit` = 建格那一刻的 `YourDeckIndex` =（点完第 1 对后 `DeckIndex = -1`）`CollectionData.CurrentIndex()`。
- 点它之后 `MainMenuScene.cs:3084` 断的是 `DeckIndex >= 0 && PickedPrebuilt == null && PairIndex < 0`
  —— 对应 `PracticeModePopup.cs:2469-2481` `PickDeck` 的实际写入，仍能照出「回调写成空 / 写成 `PickPair`」那一档改坏法。

## 五、风险

| 项 | 评估 |
|---|---|
| 生产行为 | 零改动（只动 `Editor/MainMenuScene.cs` 的自检块），不影响任何窗口/点击/数据 |
| 断言强度 | 不变甚至更强（被跳过的 `★ 点首格 ⇒ 选中我的卡组` 恢复执行） |
| 复发 | 同一族写法（**跨 `Click()` 复用旧 Transform**）在本文件其它 40 处 `Click()` 附近均已按「点完重找」写（`3089/3161/3180/3195` 等）；本次是新写的 W65·1c 块里唯一一处漏掉 |
| 未验证项 | 本条结论是**静态阅读 + 日志**得出，未跑 Unity（红线）。「重建后首格带 `Hit`」是由「共用 `BuildDeckCell` + 首格矩形全在视口内 + 兄弟格/首格后续新引用均通过」推得，**没有直接在新格上 observed 过 `Hit`**；若修完仍红，下一个怀疑点应是 `ShowYourDeckCell()` 在点完第 1 对后返回 `false`（但 `YourDeckIndex` 回落 `CurrentIndex()`，当前夹具下应仍为真，见 `PracticeModePopup.cs:1980-2004`） |

## 六、附带发现（不改，只记）

- `MainMenuScene.cs:3089-3095` 的复位块**在 `if (owb != null)` 之外**（`if` 在 `:3087` 结束），
  所以本次虽然 `owb == null`，复位两条仍跑到了（`menu.log:15890 / 15938`）—— 这是**对**的结构，
  别在修 `3080` 时顺手把它挪进 `if` 里。
- 同文件 `3826` 那句 `awb.Click()` 复用旧 `awb` 是**安全**的：`PickArmy` 同阵营**早返回**
  （`PracticeModePopup.cs:2454` `if (ArmyIndex == i) return;`），不会重建阵营格。
