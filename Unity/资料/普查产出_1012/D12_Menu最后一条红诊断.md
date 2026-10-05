# D12 —— `MainMenuScene.Run` 最后一条红：只读诊断

> **角色**：只读诊断代理。⛔ 没改任何生产代码 / 正本 / 别人的报告，⛔ 没动 git，⛔ 没跑 Unity（日志已在盘上）。
> 本文件是本次唯一的写入。
> **日期**：2026-10-12

---

## 结论

**根因 = 自检那个 `hb` 句柄跨了「窗体重建」，成了 Unity 假 null ⇒ 第二次 `Battle!` 点击被【静默跳过】**，
不是开战链的实现错。属今天第 ③ 族（**夹具前提被「照原版改对了的改动」推翻**）。

**一句话链条**：`so.Close()`（`:3990`）把被全屏窗藏起来的排位窗**带回来**（`ShowPreviousWindow` → `TryOpen` 的 `Closed` 支 = **重建内容**）
⇒ 整棵子树被 `DestroyImmediate` 重建 ⇒ `:3919` 抓的 `hb` 成了**死引用** ⇒ `:4005` 的 `if (hb != null)` 判假、**一下都没点**
⇒ `:4006` `rk.TickSearch(12f)` 推的是**新建的那个** `_search`（`Searching == false`，从没 `BeginSearch` 过）⇒ 什么都不会发生
⇒ `:4007` `rk.StartedBattle` 仍是 `false` ⇒ ✗。

**证据（最硬的两条）**

1. **新旧两跑逐行对账：那一下点击的日志从「有一条完整链」变成「一行都没有」。**
   - 旧跑（`d:/4/_tmp_view/menu.log`，01:18，`=== 合计：2008 通过 / 2 失败 ===` @ `:34562`）：这一条是 **✓**（`:21186`），
     它前面有 `[Searching] 匹配开始`（`:21074`）+ `排位：匹配那一步开**全屏**`（`:21128`）——**第二次点击真的派发了**。
   - 新跑（`C:/Users/qjh36/AppData/Local/Temp/wf_final_MainMenuScene.Run.log`，01:45，`=== 合计：2015 通过 / 1 失败 ===` @ `:34579`）：
     `✓ 排位窗还在`（`:21055`）与 `✗`（`:21068`）之间 **`:21056–21067` 只有那两条栈、一条 `[` 开头的日志都没有**。
     `[Searching] 匹配开始` 全跑只有 3 次（`:7875 / :18996 / :20550`），而旧跑有 4 次（第 4 次 = `:21074`，就是这一次点击）。
2. **新跑在那条红之前多出一次「排位窗被重建」，落在 `so.Close()` 里面。**
   `LiveOpsEventWindow.Build()` 的三句指纹（`段位块 Content 建完` / `` DEBUG`` 那一层没建` / `Few players online message 没画`）在
   新跑 `:20902 / :20923 / :20943`，夹在 `✓ 到时（1.1s / 1s）⇒ 放行切战场`（`:20889`，自检 `:3984`）与
   `✓ 🔴 关窗时把那一口气放掉`（`:20963`，自检 `:3991`）之间 —— 中间只隔着自检 `:3987/:3989/:3990` 三行，`:3990` 就是 `so.Close()`。
   旧跑那一段**没有**这组三连（旧跑的同名三连在 `:21356/:21373/:21389`，位置在 `（A94 收尾）把排位窗开回来` 那一句，是另一回事）。

**置信度**：**高（~0.9）**。唯一没被日志直接印出来的那一跳是「Unity 对已销毁对象 `== null` 为真」——那是引擎语义，且
`WindowButton.Click()`（`Shell/PromptPopup.cs:1095-1109`）**只有三个出口**，另两个都被日志/事实排除了（见下「排除项」）。

**最小改法（夹具侧，一处 4 行）**：把 `:4005` 的**陈旧句柄**换成**重新抓一次节点**，并让「抓不到」出声：

```csharp
// 再走一遍：这回不取消，等满 12 秒 ⇒ 开战
// 🔴 2026-10-12（D12）：so.Close() 会把排位窗从「被全屏窗藏起来」**带回来** ——
//    WindowsManager.ShowPreviousWindow（Shell/WindowsManager.cs:998-1056）→ 列表尾 TryOpen()（无参）
//    → OpenByState 的 Closed 支（Shell/WindowsManager.cs:459-468）= SetActive(true) + state=Open + Open()
//    → LiveOpsEventWindow.Build() 把**整棵子树** DestroyImmediate 重建（LiveOpsEventWindow.cs:356-361
//      + MenuWindowBase.cs:170-176）⇒ 上面 :3919 抓的 hb 已经是**死引用**（Unity 假 null）
//    ⇒ 照着旧句柄点 = **静默不点**（WindowButton.Click 只有 onClick==null / absorbOnly 两个不出声的出口）。
//    ⇒ 这是「关一扇全屏窗 ⇒ 底下那扇重建」的正常行为，**不是缺陷** ⇒ 夹具重新抓节点。
var hit2 = FindChild(rk.transform, "BattleHit");
var hb2 = hit2 != null ? hit2.GetComponent<WindowButton>() : null;
CheckTrue(hb2 != null, "（前提）重建之后 `Battle!` 仍有点击区 —— 抓不到 ⇒ 下面那条等于空断");
if (hb2 != null) hb2.Click();
rk.TickSearch(12f);
CheckTrue(rk.StartedBattle, "排位窗的 `Battle!` 走**同一条**开战链（等满 12 秒 ⇒ 开战）");
```

按上面的改法，重抓到的 `BattleHit` 是 `BuildToBattle`（`Shell/LiveOpsEventWindow.cs:812-814`）新建的、`onClick = () => StartMatch()` 已接好；
点下去会走 `LiveOpsEventWindow.StartMatch`（`:831-875`）→ `BeginSearch`（日志「匹配开始」）→ `RankedEventWindow.StartMatch` 的 `OpenSearchWindow(false)`（`Shell/RankedEventWindow.cs:219-247`）
→ `rk.TickSearch(12f)` 推满 12 秒 → `OnSearchDone` → `StartBotBattle()`（`Shell/LiveOpsEventWindow.cs:929-951`，`:938` 置 `StartedBattle = true`）⇒ **这一条应当转绿**。

---

## 展开

### 1. 这条断言到底在断什么（先把「量什么」看清）

`Editor/MainMenuScene.cs:4004-4007`：

```
4004  // 再走一遍：这回不取消，等满 12 秒 ⇒ 开战
4005  if (hb != null) hb.Click();
4006  rk.TickSearch(12f);
4007  CheckTrue(rk.StartedBattle, "排位窗的 `Battle!` 走**同一条**开战链（等满 12 秒 ⇒ 开战）");
```

它断的是**结果**（`StartedBattle`），不是「12 秒计时走满」也不是「走的是哪条链」——
「同一条链」是**做**出来的（`Battle!` 走的就是 `StartMatch`），断的是**走完之后真的开了战**。
`hb` 是 `:3918-3919` 抓的 `BattleHit` 的 `WindowButton`（第一次点击 `:3921` 也是它）。

⚠️ 简报里给的栈 `MainMenuScene.cs:7351` 是**旧那一跑**的行号（旧文件里那是 `EditorApplication.Exit`）；
新跑同一句在 `:7360`（H46 往这文件里加了 9 行）。**真正红的那一条断言在 `:4007`**。

### 2. 完整因果链（每一步都带出处）

| # | 发生了什么 | 判据（文件:行号 / 日志行） |
|---|---|---|
| ① | 自检抓 `BattleHit` 的 `WindowButton` 存进局部 `hb`，点一下 ⇒ `StartMatch` ⇒ 排位窗开了**全屏** `SearchingOpponentWindow` | `Editor/MainMenuScene.cs:3918-3921`；新跑日志 `:20550`（`[Searching] 匹配开始`）、`:20604`（开全屏）、`:20619`（✓） |
| ② | 全屏窗一开，**场上其余窗被 `HideAllWindows()` 藏起来**（`state=Closed`，**仍留在 `openWindows` 表里**） | `Shell/WindowsManager.cs:852-857`（Fullscreen 支）；`:555-559`（`Hide()` = `state=Closed` + 物体关掉，**不摘表**）；`:910-921`（文档注释明写「不摘表 … 等上面那扇关掉时由 `ShowPreviousWindow` 认列表尾把它们带回来」） |
| ③ | 自检 `so.Close()` ⇒ `NotifyClosed` ⇒ 关的是顶窗 ⇒ **`ShowPreviousWindow()`** ⇒ 列表尾（= 排位窗）⇒ **`prev.TryOpen()`（无参那条）** | 自检 `:3990`；`Shell/WindowsManager.cs:961-972`（`wasTop` → `ShowPreviousWindow()`）、`:1013-1031`（取列表尾）、`:1056`（`prev.TryOpen();`） |
| ④ | 排位窗此刻是 `Closed` ⇒ 走 **`Closed` 支 ⇒ `SetActive(true)` + `state=Open` + `Open()`** ⇒ `Build()` **把根下所有子件 `DestroyImmediate` 掉再重建**，并且**新建一个 `_search`**（`Searching=false`、`SecondsLeft=0`） | `Shell/WindowsManager.cs:459-468`（`OpenByState` 的 `Closed` 支，`:465` 就是 `Open()`）；`Shell/LiveOpsEventWindow.cs:333-344`（`Open()` → `Build()` + `_search = SearchingMatchPopup.Attach(...)`）、`:356-361`（`Build()` 先清空根下子件）；`Shell/MenuWindowBase.cs:170-176`（`DestroySafe` 在编辑器非播放态 = **`DestroyImmediate`**） |
| ⑤ | 于是 `:3919` 抓的 `hb` 指向**已销毁**的物体 ⇒ `:4005` 的 `if (hb != null)` 判**假**（Unity 假 null）⇒ **点击一下都没发生** | 自检 `:4005`；新跑日志 `:21056-21067` **零输出**；`Shell/PromptPopup.cs:1095-1109`（`Click()` 只有三个出口，见下） |
| ⑥ | `rk.TickSearch(12f)` 推的是**新建的那个** `_search`：`Tick` 里 `if (!Searching) return;` ⇒ 早退，`OnSearchDone` 永不触发 ⇒ `StartBotBattle()` 从没被调 ⇒ `StartedBattle` 仍是 `false` | `Shell/LiveOpsEventWindow.cs:902`（`TickSearch` → `_search.Tick`）；`Shell/SearchingMatchPopup.cs:297-320`（`:311` 那一句 `if (!Searching) return;`）、`:270-277`（`BeginSearch` 才会置 `Searching=true`）；`:929-951`（`StartBotBattle`，`:938` 置 `StartedBattle`） |
| ⑦ | ⇒ ✗ | 新跑 `:21068` |

### 3. 排除项（为什么不是别的）

- **不是实现侧**：`StartMatch` 从头到尾**一行日志都没打**。它通路上每一步都出声或必被记：
  `SelectedDeckFitsMode` 拦下 → `Debug.LogWarning("[Event] 开战被挡：…")`（`Shell/LiveOpsEventWindow.cs:839`）；
  没督军 → `:846` `LogWarning`；联机接管 → `:860` `Debug.Log`；没接管 → `:866` `Debug.Log`；`BeginSearch` → `SearchingMatchPopup.cs:275` `Debug.Log`。
  这五句**一句都没出现** ⇒ `StartMatch` 根本没被调用（不是「调了但早退」）。
- **不是 `interactable=false` 那条路**：那条会打 `[Button] `xxx` **点了不生效** …`（`Shell/PromptPopup.cs:1103-1106`）——
  这个警告今天在别处确实出现过 3 次（新跑 `:8480 / :8900` 等），说明它**会**印；本节一处都没有。
- **不是 `onClick == null` / `absorbOnly == true`**：同一个对象在 `:3921` 点得动（那次派发成功了，且当时的 ✓ 就在 `:20619`）。
  ⇒ 唯一剩下的出口就是 `:4005` 那个 `if` 本身就为假。
- **不是「12 秒推不满」**：`TickSearch(12f)` 推的对象是**重建时新建的** `_search`，它连倒计时都没起过（`Searching=false`），
  `BeginSearch` 那句日志同样一次都没有 ⇒ 这一支是「第 ⑤ 步的**下游**」，不是成因。
- **不是异常**：`21056–21067` 无任何异常输出，且后面 8 条（吸收层那组）照常跑完、合计 2015/1。

### 4. 为什么是**今天改出来的**（新旧两跑对账）

两次跑之间（01:18 → 01:45），`CardPresentation` 下与本路径相关的源文件**只有 `Editor/MainMenuScene.cs` 变过**（mtime 01:35）：
`Shell/LiveOpsEventWindow.cs`(00:06) / `Shell/WindowsManager.cs`(00:14) / `Shell/MenuDraw.cs`(00:19) / `Shell/PromptPopup.cs`(00:05) /
`Shell/RankedEventWindow.cs`(10-05 07:11) / `Shell/SearchingMatchPopup.cs`(10-05 13:02) / `Shell/SearchingOpponentWindow.cs`(10-02) 全在两跑之前。
（01:00–01:36 还改过 `DeckRuntime.cs` / `DeckScene.cs` / `RewardsScene.cs` / `BattleDriver.cs` / `BattleScene.cs` / `ShopScene.cs` / `ScenarioBlendables.cs`，都与这条链无关。）

变化点就是 H46 那一处（`git diff` 打出来的 hunks `@@ -3845,14 +3897,24 @@` / `@@ -1743,14 +1778,27 @@`）：
`Editor/MainMenuScene.cs:3911` 从 **A416 的 `wmFixB.popUpWindow.Close()`** 换成了 **`CloseModalPopups()`**（`:217-223`，按类型清 `PopUpGameWindow` + `PromptPopup`）。

**它怎么把这条红「露出来」的**：

- **A416 那一版**：那一刻 `popUpWindow` 里装的正是**被测的排位窗** ⇒ 清场把它自己关掉 ⇒ `NotifyClosed` 还会把它**摘出 `openWindows`**
  ⇒ 第 ③ 步 `ShowPreviousWindow()` 时**表里已经没有排位窗可带回来** ⇒ **不发生重建** ⇒ `hb` 一直活着（虽然物体被关、`Click()` 也照旧派发，它不查窗态）
  ⇒ 第二次点击照常开战 ⇒ 这条**在旧跑里是 ✓**（旧跑那两条红是「排位窗还在」+ 它的下游「吸收层」）。
- **H46 那一版（正确的那一版）**：排位窗**合法地一直开着 / 一直留在表里** ⇒ 第 ③ 步真的把它带回来了 ⇒ **重建** ⇒ `hb` 变死引用。

⇒ 这正是今天第 ③ 族：**夹具的前提（「这棵树不会在两次点击之间被重建」）被一个「照原版改对了的改动」推翻**。
**0 条实现错**——与今天 27 条红的统计口径一致。

### 5. 复发面 / 给调度台的两条建议（我没改任何东西，只报）

1. **同类陈旧句柄**：这个形状 =「抓节点 → 某处关/开一扇窗 → 再拿旧句柄点」。
   本节里**只有 `:3918` 这一处**跨了关窗（练习窗 `:1806-1809` 与遭遇战 `:3726-3729` 都是「抓完立刻点，中间不关窗」；
   本节 `:4016` 的 `CheckAbsorbRule("排位窗", rk.transform, …)` 用的是**现取**的 `rk.transform`，不受影响）。
   ⚠️ 全文件 500+ 处 `GetComponent<WindowButton>()` **没有逐处核**（见 §没查清）。
2. **这类「静默不点」值得进 `资料/已知的坑.md`**：`if (node != null) node.Click();` 在**窗体重建**之后会**安静地什么都不做**
   —— 对着 `StartedBattle == false` 的红，你会以为实现坏了。判据 = 「关一扇全屏窗之后，底下那扇的状态是 `Closed` ⇒ 会被重建」
   （`WindowsManager.cs:459-468` + `:961-972`）。

### 6. 顺带记录（不算这条红的成因，与这条红同源，供调度台分流）

- 新跑比旧跑**总断言数 +6**（旧 2008+2=2010 → 新 2015+1=2016），而 H46 报告自陈「条数不变（只动清场与夹具）/ 预期 2010 通过 0 失败」。
  多出来的 6 条是哪一段落的，我没归因（不在本件范围）。
- `so.Close()` 会把底窗带回 `Open` ⇒ 本节 `:4002` 那条 `✓ 排位窗还在` 现在**依然成立但语义变宽了一点**
  （从「没被那扇全屏窗带走」变成「被带走又被带回来了」）。这不是缺陷，但**它已经分辨不出这两种情况**了 ——
  如果哪天真要钉「没被带走」，判据得另找（例如查 `openWindows` 里它有没有被摘过表）。

---

## §没查清

1. **「`hb != null` 判假」这一步没有直接打印**（我按红线没跑 Unity）。
   我是反推的：`Click()` 只有三个出口（`PromptPopup.cs:1095-1109`），另两个都被日志/事实排除（见 §3）⇒ 只剩「自检那个 `if` 为假」。
   **要钉死只需在 `:4005` 前后各加一行 `Debug.Log("hb==" + (hb == null))`**，一次跑（约 24s）就能定案 —— 那是改自检代码，不在本次白名单内。
2. **旧跑 `so.Close()` 那一刻到底有没有触发 `ShowPreviousWindow` / 它带回了哪一扇**，我没逐条记账；
   我是按「旧跑那一段**没有**重建三连 ⇒ 没带回顾」反推的（与 A416 把排位窗摘出表自洽）。
3. **同类陈旧句柄的全量面**：只核了「抓完就点、中间不关窗」这个形状在本节的分布，**没扫全文件**。
4. **新跑多出的 6 条断言出自哪一段**没查（可能与本次这条红无关）。

---

## 出处清单（复制用）

**代码**
- `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`：`:217-223`（`CloseModalPopups`）、`:229-234`（`AnyModalPopupLeft`）、
  `:3911`（H46 那一句）、`:3918-3921`（抓 `hb` + 第一次点击）、`:3990`（`so.Close()`）、`:3993`、`:4002`、`:4004-4007`（**红的那一条**）、`:4016`、`:7360`
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs`：`:446-449` / `:455-468`（`TryOpen` / `OpenByState`，`Closed` 支重建）、
  `:555-559`（`Hide()`）、`:846-882`（`OpenWindow`，Fullscreen ⇒ `HideAllWindows`）、`:910-921`（`HideAllWindows` 文档）、
  `:961-972`（`NotifyClosed` → `ShowPreviousWindow`）、`:998-1057`（`ShowPreviousWindow`，`:1056` `prev.TryOpen()`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEventWindow.cs`：`:333-344`（`Open`）、`:356-361`（`Build` 清子件）、
  `:831-875`（`StartMatch`）、`:902`（`TickSearch`）、`:929-951`（`StartBotBattle`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/RankedEventWindow.cs`：`:219-227`（`StartMatch` 覆写）、`:236-247`（`OpenSearchWindow`）、`:252-268`（`CancelSearch`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/SearchingMatchPopup.cs`：`:255-262`（`Cancel`）、`:270-277`（`BeginSearch`，日志在 `:275`）、`:297-320`（`Tick`，`:311` 早退）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/SearchingOpponentWindow.cs`：`:261-266`（`Cancel`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MenuWindowBase.cs`：`:170-176`（`DestroySafe` = `DestroyImmediate`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/PromptPopup.cs`：`:1095-1109`（`WindowButton.Click` 的三个出口）

**日志**
- 新（01:45，**这条红在这一次**）：`C:/Users/qjh36/AppData/Local/Temp/wf_final_MainMenuScene.Run.log`
  `:34579` 合计 2015/1 · `:20550` 第一次「匹配开始」 · `:20604` 开全屏 · `:20619` ✓ · `:20889` ✓ 到时 ·
  **`:20902 / :20923 / :20943` 重建三连（在 `so.Close()` 里）** · `:21029` 取消匹配 · `:21055` ✓ 排位窗还在 ·
  **`:21056-21067` 空档** · `:21068` ✗（栈 `MainMenuScene.cs:4007`） · `:21373 / :21390 / :21406` 第二组重建三连（A94 收尾）
- 旧（01:18，同一条是 **✓**）：`d:/4/_tmp_view/menu.log`
  `:34562` 合计 2008/2 · `:21046` ✗「排位窗还在」 · **`:21074` 第二次「匹配开始」** · `:21128` 开全屏 · **`:21186` ✓ 同一条开战链** · `:21356 / :21373 / :21389` 重建三连
