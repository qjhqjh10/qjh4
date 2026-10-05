# D11 · `ShopScene.Run` 3 红 + `MainMenuScene.Run` 2 红 —— 只读诊断（2026-10-12）

> **角色**：只读诊断代理。⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git · ⛔ 没改任何生产代码 / 正本
> —— **本文件是本次唯一写过的文件**。
> **判据 = 日志原文 + 现读代码**：
> · `d:/4/_tmp_view/shop.log`（`[Shop] === 合计：1828 通过 / 3 失败 ===` 在 `:42756`；三条 ✗ 在 `:1624` / `:3245` / `:7048`，失败重列 `:42767` / `:42778` / `:42789`）
> · `d:/4/_tmp_view/menu.log`（`[Menu] === 合计：2008 通过 / 2 失败 ===` 在 `:34562`；两条 ✗ 在 `:21046` / `:21342`，失败重列 `:34573` / `:34584`）
> 📌 行号一律是**本次读到的坐标**（会漂）。
> ⚠️ **先说一句免得白跑**：**这 5 条红，没有一条是「实现没做到」** ——
> Shop 那 3 条是**一条断言量错了对象**（量的是商店格里**我们自加**的 `Name` 行，判据文案写的是 `OfferContainer` 的 `name`）；
> Menu 那 2 条是**同一个根因**：本轮新改的**清场那一行把被测的排位窗自己关掉了**（第 2 条是它的下游，不是 A94 老账复发）。

---

## 〇、结论（一表）

| 红 | 断言（`文件:行` · 日志行） | 根因 | 证据 | 置信度 | 最小改法 |
|---|---|---|---|---|---|
| **Shop ×3** | `Editor/ShopScene.cs:1370`（日志 `:1624` / `:3245` / `:7048`；三条文案逐字相同，因外层有**按页循环** ⇒ 3 页各一次） | 🔴 **断言量错了对象（假阴）**：它量的是商店栅格里 `CatalogItemShopContainer_0/Name` —— 那是 `Shell/ShopWindow.BuildCell` **我们自己加的那一行字**，上限写死 **30**（`Shell/ShopWindow.cs:662`）；而判据文案说的是 `OfferContainer` 的 `background/name-bg/name`（上限 **42**，`Shell/OfferContainer.cs:1082`）—— **两棵不相干的树** | 见 §一：`:1357-1374`（断言本体）· `:1307`/`:1312`（循环与取址）· `ShopWindow.cs:658/662`（那个 30）· `Label.cs:615`（`fontSizeMax = cur × maxPx/nomPx` ⇒ 反算回 px **恒等于实参**）· `OfferContainer.cs:1081-1082`（真正的 42）· `F1_字号线.md:194`（`ShopWindow` 那三处**是自加件、本来就查不到**） | **高** | **把这条挪到真正量 `OfferContainer` 的那一段**（`:2564` 已取到 `lbName`、`:2570` 就在断它的标称字号 ⇒ 紧跟其后加一条读 `lbName.FontSizeMax`；同段 `:2586-2592` 的 `Timer Text` 就是现成范式）。⛔ **不要**为了让它变绿去把 `ShopWindow.cs:662` 的 30 改成 42 |
| **Menu 1** | `Editor/MainMenuScene.cs:3957`（日志 `:21046`） | 🔴 **清场那一行关错了窗**：`:3866` 的 `popUpWindow.Close()` 里，`popUpWindow` 此刻**就是被测的排位窗**（排位窗是 `type=Popup`，开窗时 `OpenWindow` 把 `popUpWindow` 写成了它）⇒ 排位窗被 `Close()` 掉、摘出 `openWindows` ⇒ 后面没人能把它带回来 | 见 §二：`LiveOpsEventWindow.cs:322-324`（`type=Popup`）· `WindowsManager.cs:858-865`（弹窗支写 `popUpWindow`）· `:561-596` + `:961-972`（`Close` 摘表）· **日志链 `menu.log:20520 → :20533 → :20548`，紧接 `:20562` 那张 `05_排位窗.png`**（代码位置 = `:3870`，就在 `:3866` 之后） | **高** | 把 `:3866` 改回**按类型**清场、并把新宿主类型一起覆盖（见 §二·4）；⛔ **不要再拿 `popUpWindow` 当清场入口** —— 它此刻装的是被测那扇窗 |
| **Menu 2** | `Editor/MainMenuScene.cs:153`（由 `:3971` 调；日志 `:21342`） | ⬆ **同一个根因的下游**：窗被 `SetActive(false)` 后整棵子树的钮都进不了命中表（`HitQuad` 要求 `isActiveAndEnabled`）⇒ 面板矩形内**点哪儿都命不中吸收层** ⇒ `found=false` | 见 §二·3：`PointerLayer.cs:897-903`（`HitQuad` 的两道门槛）· `:1103-1131`（`CollectHits` 逐颗过它）· 旁证：同段**其它子项全绿**（节点在 / 是公共件建的 / 四沿 / 档 3115 / 无告警 / 有指针层，`menu.log:21199-21328`）、**兄弟窗同类断言全绿**（`:10525` / `:19566` / `:19837`） | **高** | 同红 1（修好它应自动转绿） |

**一句话总览**：**Shop 3 条 = 一条新落的断言量错了节点**（期望值 42 是对的，**量的对象**错了）；
**Menu 2 条 = 一条本轮的「清场」改动关掉了被测窗**（第 2 条是它的下游）。

---

## 一、Shop 3 条 —— 断言量的是**商店格里我们自加的那行字**，不是 `OfferContainer`

### 1.1 断言本体与它所在的循环（现读）

`Editor/ShopScene.cs:1357-1374`（**本轮工作区新增**，`git diff` 里是 `+` 块）：

```csharp
if (i == 0)
{
    var nmLb = LabelAt(cell, "Name");
    CheckTrue(nmLb != null, "★ 格 1 的 `Name` 上挂着 `Label`（上限那条才有对象可量）");
    float nmMaxPx = nmLb != null ? Label.FontSizeToPx(nmLb.FontSizeMax) : -1f;
    CheckNear(nmMaxPx, 42f, 0.35f, $"★ `OfferContainer` 的 `name` 上限 = **原版 `m_fontSizeMax` 42.0 px**（实得 {nmMaxPx:F2}）…");
}
```

它坐在**商店栅格的逐格循环**里：外层 `Editor/ShopScene.cs:1249` 是**按页**的 `for (int p = 0; p < ShopData.Pages.Length; p++)`（`:1248` 是它上面那句 `Section("页签切换…")`），
内层 `:1307` 是 `for (int i = 0; i < offers.Length; i++)`，而 `cell` 来自 `:1312`：

```
var cell = FindChild(content, "CatalogItemShopContainer_" + i);
```

⇒ 三页各命中一次 `i == 0` ⇒ **三条一模一样的失败**（日志 `:1624` / `:3245` / `:7048`，三条都印「实得 30.00」）。

### 1.2 被量的那个节点是谁建的（决定性）

`CatalogItemShopContainer_*` 由 `Shell/ShopWindow.BuildGrid`（`Shell/ShopWindow.cs:528`）→ **`BuildCell`**（`:565`）建。里面那行 `Name`：

| 行 | 原文 |
|---|---|
| `Shell/ShopWindow.cs:658` | `var nm = _win.Text(cell, o.Name, nmR.x1, nmR.x2, nmR.y1, nmR.y2, 5, Color.white, "Name", **30f**);` |
| `Shell/ShopWindow.cs:662` | `nm.SetAutoFitBox(LayoutSpace.Px(nmR.W), LayoutSpace.Px(nmR.H), 18f, **30f**);` |

而 `SetAutoFitBox`（`Battle/Label.cs:571-620`）把上限写成：

* `Battle/Label.cs:615`：`_tmp.fontSizeMax = cur * (maxPx / nomPx);`
* `Battle/Label.cs:649-650`：`NominalPx()` = `FontSizeToPx(TmpFontSize())` = `FontSizeToPx(cur)`
* `Battle/Label.cs:481-484`：`FontSizeToPx(f) = f × k`

⇒ `FontSizeToPx(fontSizeMax) = FontSizeToPx(cur) × (maxPx / nomPx) = nomPx × maxPx / nomPx = **maxPx**`（浮点上逐位）——
**断言量出来的数就是调用点传进去的那个 `maxPx`**。实测打印 **30.00**，与 `ShopWindow.cs:662` 的 `30f` **逐位相同**。
（该格里另一处 30 是 `_win.Text` 的 fontPx，同一族；`Type` = 26 / 占位名 = 24 / `Available` = 16，都不是 30。）

### 1.3 判据文案说的却是**另一棵树**

* `Shell/OfferContainer.cs:1081-1082`：
  `LabelFit(nb, R(g.Name…), c.Name, NameColor, NName, g.NameFs, 10f, qBase + QoText, true, false, **42f, 46f**);`
  —— 这才是「`OfferContainer` 的 `name` 上限 = 42（base 46）」那一处；判据 = 原版 21 颗 `'Legendary Wildcard Bundle'` 的 `m_fontSizeMax` 全是 42（`资料/普查产出_1012/F1_字号线.md:64` 的 #20 行）。
* **两棵树没有任何关系**：`Shell/ShopWindow.cs` 里**一处都没提** `OfferContainer`（`grep -rn "OfferContainer" Shell/` 排除自身后，命中的全是 `BoosterInfoPopup.cs` / `ItemDrawer.cs` 里的**注释**，无调用）；而 `OfferContainer.Build` 的调用点**全在 `Editor/ShopScene.cs`**：`:2491` / `:2948` / `:3588` —— 三处都是**自检脚手架**（`_offerScratch` / `shot`），生产路径**一处都不调它**。
* 反过来，`OfferContainer` 那棵树上**本来就有**一条量 `name` 的断言段：`:2564` `lbName = LabelAt(b.Root, "background/name-bg/name")` → `:2570` 断它的**标称**字号；同段 `:2586-2592` 断 `Timer Text` 的 **max/min**（就是这条 A333 断言该长的样子）。

### 1.4 为什么可以断定「是断言写错」而不是「实现没做到」（三条独立观察）

1. **这条断言结构上永远不可能绿**：被测量的实现在 `ShopWindow.cs:662` 写死 `30`，与施加对象无关。
2. **它自陈的「改坏法」对它自己无效**：文案写「把 `OfferContainer` 里 `LabelFit(…, 42f, 46f)` 的两个实参去掉 ⇒ 那一份 36.7 的格会量出 36.7 ⇒ 红」——
   去掉那两个实参只会改 `OfferContainer` 的 `name`，**改不到商店格那行字** ⇒ 去掉之后这里**照样读 30.00**（它已经红了，且不会因此变红或变绿）。**改坏法打不响 = 它量的不是它说的东西。**
3. **判据侧自己就写着这两者不是一回事**：`资料/普查产出_1012/F1_字号线.md:194` ——
   「（`Shell/ShopWindow.cs` 那三处则是**我们自加件**，本来就查不到）」⇒ `ShopWindow` 的 `Name`/`Type`/占位名那三行**没有原版值可抄**，把它们按 42 断是**无判据的**。

### 1.5 这条是怎么落错的（出处）

* 工作区 `git diff` 显示整块是**新增**（`Editor/ShopScene.cs:1357-1374`），HEAD 里没有这条（`git show HEAD:… | grep -n nmMaxPx` 零命中）。
* 落地报告 = `资料/普查产出_1012/F2_字号线收尾.md:188`（§三 #4），自己写的落点是
  「`Editor/ShopScene.cs`（**商店格循环里**，`i == 0` 时）｜ `OfferContainer` 的 `Name` 的 `FontSizeMax` 折回 = 42.0 px」
  ⇒ **把「商店格里那行 `Name`」当成了 `OfferContainer` 的 `name`**（两个都叫 `Name`，而判据（42/36.7）来自 `OfferContainer`）。
  F1 的原规格（`F1_字号线.md:133` 的 #4）只写了「期望值 42、改坏法去掉那两个实参」，**没给节点路径** —— 落点是放这一步自己挑的。

### 1.6 最小改法（两条路，二选一）

* **(a) 挪到真正的那棵树上（推荐）**：把这条断言从商店格循环里删掉，加到 `Editor/ShopScene.cs:2570` 那条之后
  （那里已经有 `lbName` 与「标称 = 原版 `m_fontSize`」的断言），形如
  `CheckNear(Label.FontSizeToPx(lbName != null ? lbName.FontSizeMax : 0f), 42f, 0.35f, …)`
  —— 同段 `:2586-2592` 断 `Timer Text` 的 max/min 就是现成范式（量的是**生产调用点上那个 label**）。
  ⚠️ 挪过去之后要**逐份看一眼 `VExpAll` 那 19 行的 `NameFs`**（这份表里 36.7 / 42 两支都在，见 `:2569` 的 `e.NameFs > 40f ? R42 : R367`）—— 我只按规格判「42 是期望」，**没有逐份复算这张表**（见 §没查清 3）。
* **(b) 若确实想连商店格那行自加字一起钉**：那它断的必须是**我们挑的值**（现为 30 = `ShopWindow.cs:658` 的 fontPx），**文案里「原版 `m_fontSizeMax` 42」那半句要去掉**（`F1:194`：那三处没有原版判据）。
* ⛔ **不要**为了让断言变绿去把 `ShopWindow.cs:662` 的 `30` 改成 `42` —— 那是**凭空编一个原版值**（铁律 3 / 铁律 11 的边界①：原版就没有）。

---

## 二、Menu 2 条 —— 清场那一行**关掉了被测的排位窗**（第 2 条是它的下游）

### 2.1 事故点：`Editor/MainMenuScene.cs:3865-3869`

```csharp
var wmFixB = rk.Manager != null ? rk.Manager : WindowsManager.Instance;
if (wmFixB != null && wmFixB.popUpWindow != null) wmFixB.popUpWindow.Close();     // ← 本轮的「类型无关清场」
if (wmFixB == null || wmFixB.popUpWindow != null)
    Debug.LogWarning("[MainMenu 自检] 排位窗这一段起手那扇弹窗**没被清掉**…");
Shoot("05_排位窗.png");                                                            // :3870
```

* **改前（HEAD）**：`foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None)) if (pp != null) pp.Close();`
  （`git show HEAD:… | grep -n "FindObjectsByType<PromptPopup>"` → HEAD `:1749` / `:3851` 两处）—— **按类型清**，**碰不到排位窗**。
* **改后（工作区，未提交）**：`git diff` 的 hunk **`@@ -3851,2 +3862,8 @@`** 就是这一处 —— **排位窗这一段今天只动了这一处**。
* 改动来源 = `资料/普查产出_1012/H17_ShowPopUp收编.md:55`（A416，「排位窗那处清场：同上改法（用 `rk.Manager`）」）；
  同一份报告 `:110` 明写 **「🔴 本轮按用户口径没跑自检」** ⇒ **这条改动从未被 `Run` 验过**。

### 2.2 为什么 `popUpWindow` 那一刻**就是排位窗**

| 环节 | 出处 |
|---|---|
| 排位窗是 **Popup** | `Shell/RankedEventWindow.cs:45-49` `Create` → `Shell/LiveOpsEventWindow.cs:322-324` `win.type = WindowType.Popup;`（原文注释「实证 type=1」） |
| 开弹窗时把 `popUpWindow` 写成它 | `Shell/WindowsManager.cs:858-865`（else 支）：`popUpWindow = win; currentWindow = win;` |
| 测试怎么开它的 | `Editor/MainMenuScene.cs:3800` `rwb.Click()` → `OpenWindow(rk)` —— 之后**再没有别的弹窗开过**（本段到 `:3866` 之间只有读断言 + `Shoot`） |
| 关它意味着什么 | `Shell/WindowsManager.cs:561-596`：`CurrentState = Closed` + `Data = null` + `NotifyClosed` + `SetActive(false)`；`NotifyClosed`（`:961-972`）**把它摘出 `openWindows`**（`:968`） |

### 2.3 日志链（决定性证据）

`d:/4/_tmp_view/menu.log`：

```
:20520  [Searching] 取消搜索（原版 `CancelSearchButtonOnClick → MatchMakerManager.CancelSearch`）
:20533  [Event] 取消匹配（原版 `SearchingOpponentWindow__CancelMatchMatchmaking → MatchMakerManager.CancelSearch`）
:20548  [Event] 排位：取消匹配 ⇒ 回到本窗（全屏那扇收掉）
:20562  [Menu]   截图 d:/4/_tmp_view/menu\05_排位窗.png
```

这三条**只能是**「排位窗自己被 `Close()`」：

* `:20520` 是 `Shell/SearchingMatchPopup.cs:255-262`（`Cancel()` 的第一句）；
* 而它的调用者正是 `LiveOpsEventWindow.Close()`（`Shell/LiveOpsEventWindow.cs:349-353`：`if (_search != null) _search.Cancel(); base.Close();`）——
  **只有排位窗/遭遇战窗这几扇 `LiveOpsEventWindow` 有自己的 `_search`**；
* `:20548` 是 `Shell/RankedEventWindow.cs:257-268` 的**末句**（`CancelSearch` 覆写）⇒ 打这行日志的**只能是排位窗**；
* 全屏 `SearchingOpponentWindow` 当时**还不存在**：它第一次被开在 `:20656`（`[Event] 排位：匹配那一步开**全屏** SearchingOpponentWindow`），
  ⇒ `RankedEventWindow.CancelSearch` 里那条 `if (_searchWin != null) _searchWin.Close();` **当时是空跑** ⇒ 这一串**只能来自 `Close()` 路径**；
* 而这个时刻**代码上只剩一条语句**：`:3866` 的 `popUpWindow.Close()`（`:3870` 就是那张截图，`104` 行之间没有任何别的调用）。

⇒ **`wmFixB.popUpWindow` 当时 == 排位窗** ⇒ 清场把被测的窗关掉了。
（副作用：`:3867` 那条守卫之所以**没出声**，是因为 `NotifyClosed` 顺手把 `popUpWindow` 清成了 `null`（`WindowsManager.cs:970`）—— 这正是「静默」的地方。）

### 2.4 红 1 的直接后果与「为什么不会自动回来」

`Close()` → `NotifyClosed`（`WindowsManager.cs:961-972`）：`wasTop` 为真 → `openWindows.Remove(rk)` → `ShowPreviousWindow()`。
`ShowPreviousWindow`（`:998-1057`）认的是**列表尾**，而排位窗**已经被摘出列表**（`:968`），
后面全屏搜索窗关掉时（Cancel ⇒ `so.Close()`）再跑一次 `ShowPreviousWindow`，列表尾**也不再是它** ⇒ 排位窗**停在 `Closed`**。
⇒ `Editor/MainMenuScene.cs:3957` 的 `CheckTrue(rk.CurrentState != WindowState.Closed, "**排位窗还在**…")` **红**（日志 `:21046`）。

### 2.5 红 2 = 同一个根因的下游（不是 A94 复发）

`Editor/MainMenuScene.cs:3971` 调 `CheckAbsorbRule("排位窗", rk.transform, …)`，失败在 `Editor/MainMenuScene.cs:153`：

```csharp
CheckTrue(found, $"{what}：**原版面板矩形以内找得到一个点、它的命中是吸收层**…");
```

`found` 来自 `PointerLayer.ButtonAt(tx,ty)` ⇒ `HitButton`（`Shell/PointerLayer.cs:1059-1071`）⇒ `CollectHits`（`:1103-1131`）逐颗过 `HitBoxPx` → `HitQuad`（`:897-903`）：

```csharp
static ImageQuad HitQuad(WindowButton b)
{
    if (b == null || !b.isActiveAndEnabled) return null;                       // ← 整棵子树被 SetActive(false) ⇒ 全部落选
    var q = b.GetComponentInChildren<ImageQuad>();
    if (q == null || !q.gameObject.activeInHierarchy) return null;
    return q;
}
```

排位窗 `Close()` 时 `gameObject.SetActive(false)` ⇒ 它里面所有 `WindowButton` 都 `isActiveAndEnabled == false` ⇒ **一个候选都不进表** ⇒ 面板矩形里点哪儿都命不中吸收层 ⇒ `found = false`。

**旁证（四条，排除「节点/档位/公共件坏了」）：**

* 排位窗那一段的**其它子项全绿**（`menu.log:21199-21328`）：节点 `General Red Background/AbsorbHit` 在 · 是公共件 `MenuDraw.Absorb` 建的 · 四沿 = (−960, 93.57, 2880, 986.43) · 档 3115 = 内容档 3116 − 1 · 无档位告警 · **场景里有指针层**（`CheckAbsorbRule` 内部 `:115`）—— 只有 `:153` 那一条红。
* **兄弟窗**的同类断言**全绿**：`menu.log:10525`（选卡组窗）· `:19566`（匹配弹窗）· `:19837`（遭遇战窗）—— 同一套公共件、同一条断言逻辑。
* 断言里的**兜底扫描**（`Editor/MainMenuScene.cs:146-152`，40px 步长走遍整个面板矩形）也没找到 ⇒ 不是「候选点没排好」，是**整扇窗不可命中**。
* 那一刻排位窗**确实还是 `Closed`**：`:3957` 已判过（实得 Closed），其间没有把它开回来的路径（它已不在 `openWindows` 里，见 §2.4；下一次 `TryOpen` 在 `:3975`，在这条断言**之后**）。

⇒ **红 2 不需要单独修** —— 红 1 修好（排位窗重新开着）之后它应当自动转绿。

### 2.6 最小改法

1. **把 `:3866` 改回「按类型清场」，并把新宿主类型一起覆盖**（A416 之后 `ShowPopUp` 的宿主是 `PopUpGameWindow`，见 `Shell/WindowsManager.cs:1180-1210`），例如：

   ```csharp
   foreach (var g in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None)) if (g != null) g.Close();
   foreach (var g in Object.FindObjectsByType<PopUpGameWindow>(FindObjectsSortMode.None)) if (g != null) g.Close();
   ```
   （`Editor/MainMenuScene.cs:11` 已有 `using CardPresentation;` ⇒ 两个类型直接可用。）
2. **或者**在打开排位窗**之前**（本段开头）把 `Manager.popUpWindow` 快照进一个局部变量，清场时关**那个快照** —— 语义等价但更脆（快照之后任何一次弹窗开窗都会让它指错）。
3. ⛔ **不要在这一点上用 `popUpWindow`** —— 它此刻装的就是被测的那扇窗（根因本身）。
4. ⚠️ **同一批的另一处一起看**：`:1749-1756`（练习窗那段）用的是**同一个改法**。那一处**这轮没红**（因为它要清的模态窗确实就是当时的 `popUpWindow`），但它**同样脆** —— 只要那扇模态窗被任何后来的弹窗顶掉，就会重演本条 ⇒ 建议一并改成按类型。

### 2.7 归属判定：**是今天改出来的**，不是 A94 那笔老账

* 断言本身是**老账已收口**的：`git show HEAD:… | grep -n` 命中 HEAD `:3940`（`排位窗还在`）与 HEAD `:3954`（`CheckAbsorbRule("排位窗"…)`）⇒ 两条**在 HEAD 就存在**，而 HEAD 那一轮（commit `9cb9aed`）自陈「全套自检 11 条全绿」。
* 今天动这一段的 hunk **只有** `@@ -3851,2 +3862,8 @@`（就是 §2.1 那一行）。
* 改它的报告 `H17_ShowPopUp收编.md:110` 明写**没跑自检** ⇒ 这条改动**没有被任何一次 `Run` 覆盖过**。
* ⇒ **与 A94（吸收层本身缺失）无关**：吸收层节点、档位、四沿、公共件全部正确（§2.5 第一条）。

---

## 三、没查清 / 没做（如实）

1. **A416 想在 `:3866` 关掉的那扇「模式不对」模态窗，这一轮日志里没有直接的开关记录**（`PopUpGameWindow.Close()` 不出声）
   ⇒ 它当时**是否还开着、是不是 `popUpWindow`**，我**没能从日志断定**。
   ⚠️ 但这不影响根因：**能断定被关掉的是排位窗**（§2.3 那条日志链）。
   （顺带的疑问：若那扇模态窗当时还开着而 `popUpWindow` 已指向排位窗，那 A416 这一行**既关错、又没关到**它 —— 这一条我**没有查证**。）
2. **`ShowPreviousWindow` 当时挑中了哪一扇**（列表尾是谁）**日志里没有**；§2.4 那句「列表尾不再是它」是**按 `:968` 摘表推的**，**没有直接证据**。
3. **Shop 侧我没有逐份复算** `VExpAll` 那 19 行 `OfferContainer` 变体的 `name` 上限是否**每一份都该断 42**（F1 表说 21 颗 TMP 全是 42，但那张表是按 `m_text` 扫出来的）。§一·6 给的改法（挪到 `:2570` 旁边）落地时**要现核那张表**。
4. 我**没有**跑 Unity、**没有**动 git、**没有**改任何生产代码 / 正本 / 别人的报告；**本文件是本次唯一写过的文件**。
