# B106_D5 · 判据（BattleScene 1 红 / ShopScene 3 红）

> 只读诊断代理 `B106_D5`，2026-10-31。第二轮全套自检：`BattleScene.Run` 7347/1 · `ShopScene.Run` 3197/3。
> 日志 = `d:/4/_tmp_view/battle.log` / `d:/4/_tmp_view/shop.log`。行号全部现读。

## 结论速览

| # | 位置（日志行） | 断言落点（现读） | 定性 | 一句话根因 |
|---|---|---|---|---|
| 1 | `battle.log:267408` | `Editor/BattleScene.cs:49950`（调用点 `:46873`） | **γ 本批接线回归** | 新接的 `CheckManaBuffHud` 落在 `DestroyImmediate(driver)`（`:45243`）**之后** ⇒ 拿到 Unity 假 null ⇒ 首句前提红、余 10 条一条没跑 |
| 2 | `shop.log:86381` | `Editor/ShopScene.cs:1114`（调用点 `:3196`） | **α 编排写法不完整 + δ 前提未覆盖 `Background` 档** | 商店停在 `Background`（不是 `Closed`）⇒ `ClosePackAndReopenShop` 不重开；`bp` 又因页被 `BuildPages()` 重建而恒 null ⇒ 开包窗也没关 |
| 3 | `shop.log:131964` | `Editor/B103F4_Assert.cs:106`（`ClickByQuad` premise） | **β 场景卫生（陈旧 `ShopTabPage`）** | 同一像素上有两颗 `InfoHit`，命中的是「宿主窗已销毁、页仍激活」的陈旧那颗 |
| 4 | `shop.log:132014` | `Editor/B103F4_Assert.cs:362` | **级联于 #3**（不是独立问题） | #3 那一颗 `OpenBoosterInfo` 走 `_win == null` 早退 ⇒ `LastBoosterInfo` 恒 null |

**根因数 = 3（BattleScene 1 + Shop 2），级联 = 1（Shop #4）。** `shop.log:86381` 与 `131964/132014` **不是同一条**（不同窗、不同时刻、不同机制）。

---

## #1 BattleScene · `（前提·B106_W47）\`BattleDriver\` 在`

**失败原文（battle.log:267408）**
```
BT    ✗ （前提·B106_W47）`BattleDriver` 在
  at BattleScene/<>c__DisplayClass32_0:<RunCore>g__Check|3 (bool,string) (BattleScene.cs:660)
  at BattleScene:CheckManaBuffHud (CardPresentation.BattleDriver,System.Action`2<bool,string>) (BattleScene.cs:49950)
  at BattleScene:RunCore () (BattleScene.cs:46873)
```

**实得 vs 期望**：`driver != null` 期望 `true`，实得 **`false`** —— `driver` 是 Unity **假 null**（组件已被销毁）。

**判据链（全部现读）**
- `:45243`（一整条聚合行）末尾有 `UnityEngine.Object.DestroyImmediate(driver);`。
- `:45274` 的 A388 断言 `Check(driver == null && hookedAfterB == 0, …)` **本轮是绿的**（本轮唯一红不在它）⇒ 那一拍之后 `driver` 确实是假 null。
- `:46873` 在 `:45243` 之后 **4,630 行**：`… B106_W23_Assert.Run(); … CheckManaBuffHud(driver, Check); Debug.Log(P + "=== 合计 …")`。
- 同族先例（本文件已经踩过两回并搬家，注释都在）：`RunE7Fixture();`（`:45093`，2026-10-24 从末尾搬回销毁之前）、`RunR13();` / `B102C506_Assert.RunDriver(driver)`（2026-10-30 搬进 `:45243` 同一行）。**本件是第三次**。

**连带损失（不是"通过"是"没跑"）**：`CheckManaBuffHud` 共 **11** 条 check ——
`:49950 / :49966 / :49969 / :49981 / :49992 / :49996 / :50001 / :50009 / :50012 / :50015 / :50020`。
首句早退 ⇒ **另外 10 条一条没执行**。修好后合计最多从 **7347 → 7357**。

**修法（方案）**
1. 把 `CheckManaBuffHud(driver, Check);` 从 `:46873` 挪进 `:45243` 那一行内，位置 = `RunR13();` **之后**、`driver.DetachStaticHooks();` **之前**。
   - 必须在 `DetachStaticHooks()` **之前**：W47 会 `driver.Begin(...)` 重开一局；若它重新挂静态钩子，紧随其后的 `Check(BattleDriver.StaticHookCount == 0, …)` 会跟着红。
   - 必须在 `RunR13()` **之后**：W47 会 `Begin` 一局新的（会改局面），⛔ 不能污染前面任何依赖当前局面的断言。
2. 纪律：**同一物理行追加 ⇒ 行数零变化**（本文件被账按 `文件:行号` 钉着）。

**还缺什么**：无。⚠️ 修好后再看 W47 那 10 条自身是否绿（`ctx.Active == me` / `SimulatePlay` 依赖 `mulliganEnabled=false` + `offensivePhaseEnabled=false` + `firstSeatOverride`，本轮**从未跑到**，属未知）。

---

## #2 ShopScene · `（前提）商店重开之后真的开着`

**失败原文（shop.log:86381）**
```
[Shop]    ✗ （前提）商店重开之后真的开着 —— 下面几段都靠它 —— 期望 [True]，实得 [False]
  at ShopScene:ClosePackAndReopenShop (CardPresentation.ShopWindow) (ShopScene.cs:1114)
  at ShopScene:RunCore () (ShopScene.cs:3196)
```

**被测代码（现读 `ShopScene.cs:1104-1115`）**
```csharp
static void ClosePackAndReopenShop(ShopWindow win)
{
    var pg = win.PageOf(0);
    var bp = pg != null ? pg.LastBoosterPack : null;
    if (bp != null && bp.CurrentState != WindowState.Closed) { … bp.Close(); }
    if (win.CurrentState == WindowState.Closed && win.Manager != null) win.Manager.OpenWindow(win);
    CheckTrue(win.CurrentState == WindowState.Open, "（前提）商店重开之后真的开着 —— 下面几段都靠它");
}
```
**只处理 `Closed` 一档，`Background` 无人管。**

**日志证据（shop.log 现读）**
- 同一句共 10 次：`80041 / 81384 / 82439 / 82961 / 86381(红) / 91268 / 92111 / 110189` 绿，`133350` 是失败重列。
- `（编排）买完弹出的开包窗先关掉…` **只出现 3 次**：`79867 / 81210 / 82265` ⇒ **从 `82961` 那一次起 `bp` 恒 null**。
- 为什么恒 null：`82961` 那一次走的是「商店 `Closed` ⇒ `win.Manager.OpenWindow(win)`」这半边 —— 那会 **`Open()` → `Build() → BuildPages()`**（`Shell/ShopWindow.cs:150-168` / `:249-261`），**重建出一批新的 `ShopTabPage`** ⇒ 页上的 `LastBoosterPack` 跟着归零。此后任何 `ClosePackAndReopenShop` 都关不掉开包窗。
- 失败那一刻的窗口栈：`:85811` 附近有一条
  `[Win] Booster Pack Open Window：TryOpen 命中 **Background** 支 —— 只提回前台，不重建内容`
  （来源 = `CheckAbsorbRule` 点面板外关掉 `popA` 后 `ShowPreviousWindow()` 把**开包窗**带回 `Open`，`WindowsManager.cs:664-700`）⇒ **开包窗在商店之上、商店停在 `Background`**。
- ⇒ 那一刻：`bp == null`（关不掉上面那扇）+ `win.CurrentState == Background`（≠ `Closed`，不重开）⇒ 红。

**定性**：**α（自检编排写法不完整）+ δ（前提未覆盖 `Background` 档）**。**不是产品缺陷** —— `ShowPreviousWindow` 把列表尾那扇开回 `Open`、其余留 `Background`，是照原版 `WindowsManager__ShowPreviousWindow.c` 逐句抄的。

**修法（方案）**
```csharp
static void ClosePackAndReopenShop(ShopWindow win)
{
    // ① 从管理器**现取**：⛔ 别读页上的 `LastBoosterPack` —— 页会被 `BuildPages()` 重建，那个字段跟着归零
    var mgr = win.Manager;
    if (mgr != null)
    {
        var pop = mgr.GetOpenWindow<BoosterInfoPopup>();      if (pop != null) pop.Close();
        var bp  = mgr.GetOpenWindow<BoosterPackOpenWindow>(); if (bp  != null) bp.Close();
    }
    // ② 三档都收：Background ⇒ 无参 `TryOpen()`（= 原版那一跳，只回前台不重建）；Closed ⇒ `OpenWindow`
    if (win.CurrentState == WindowState.Background) win.TryOpen();
    else if (win.CurrentState == WindowState.Closed && mgr != null) mgr.OpenWindow(win);
    CheckTrue(win.CurrentState == WindowState.Open, "（前提）商店重开之后真的开着 —— 下面几段都靠它");
}
```
（`GameWindow.TryOpen()` 无参重载 = `GameWindow.cs:507`，public；`WindowsManager.GetOpenWindow<T>` = `:383`。）

**还缺什么**：`CheckTrue` 只印 `[False]`、**不印实得状态** ⇒ 建议文案加 `$"（实得 {win.CurrentState}）"`，下次一眼分得出 `Background` / `Closed`。

---

## #3 / #4 ShopScene · B103F4 ② 段 B（`InfoHit` 前提 + 点主图开详情窗）

**失败原文**
```
[Shop]    ✗ 第 1 页第 1 件的主图 `InfoHit`：（前提）它**渲出来那一块的中心**命中的就是这一颗（引用相等 ⇒ 真鼠标点得到） —— 中心 (498.1, 302.6) —— 期望 [True]，实得 [False]      (shop.log:131964)
[Shop]    ✗ ★★ 点**主图** ⇒ 开出卡包详情窗（…）—— 期望 [True]，实得 [False]                                                                                                (shop.log:132014)
```

**断言源码（现读 `Editor/B103F4_Assert.cs`）**
- `:100-112` `ClickByQuad`：`:106` 是 premise（`pl.ButtonAt(c.x,c.y) == wb` 引用相等），`:110` 是 `pl.ClickAt(c.x, c.y)`。
- `:342-372` 调用链：`w2 = ShopWindow.Create(mgr)` → `mgr.OpenWindow(w2)` → `page0 = w2.PageOf(0)` → `cell0` → `infoHit` / `infoWb` → `:359 ClickByQuad(...)` → `:360 ST(ok,…)` → `:362 ST(pop != null, …)`。

**铁证（这两条同源）**：日志紧接着出现
```
[Shop] 没有 `WindowsManager` ⇒ 开不了 `Booster Info Popup`
  at CardPresentation.ShopTabPage:OpenBoosterInfo (int)      (ShopTabPage.cs:951)
  at ShopTabPage/<>c__DisplayClass67_0:<BuildCell>b__0 ()    (ShopTabPage.cs:612)
  at CardPresentation.WindowButton:Click ()                  (WindowButton.cs:1089)
  at CardPresentation.PointerLayer:ClickAt (single,single)   (PointerLayer.cs:1365)
  at B103F4_Assert:ClickByQuad (…)                           (B103F4_Assert.cs:110)
```
读 `ShopTabPage.cs:947-955`：`if (_win == null || _win.Manager == null) { Debug.LogWarning("没有 `WindowsManager` …"); return null; }`。

⇒ 两条硬结论：
1. **`ClickAt` 那一下确实落在一颗 `InfoHit` 上、也确实进了 `OpenBoosterInfo`** —— 所以 `ButtonAt` 返回的是非 null（否则 `ClickAt` 会返回 false、`:360` 那条也会红，而它是绿的）。
2. 但**那不是 `w2` 自己那颗**（`w2` 活着、`Manager` 非空，走不到那个早退）⇒ 它所属页的 `_win` 是 Unity **假 null**（宿主窗已销毁），**页却仍在激活链上**。

⇒ 同一像素 (498.1, 302.6) 上**至少有两颗 `InfoHit`**：w2 的（`infoWb`）+ 一具陈旧页的；`HitButton` 的平局判据（同队列 `QCellInfoHit = 3031` → 同 z → `IsAboveInHierarchy`，`PointerLayer.cs:1586-1599` / `:1646-1667`）判给了陈旧那颗 ⇒ premise 红；点击落到陈旧那颗 ⇒ 走 `_win == null` 早退 ⇒ `page0.LastBoosterInfo` 保持 null ⇒ **#4 = #3 的级联**。

**定性**：**(β) 场景卫生缺陷** —— 一具「宿主窗已销毁、页仍激活」的 `ShopTabPage` 残留，且它的命中区与 w2 的完全重叠。

**已排除的来源（本轮现读核过）**
- `wOff` / `wOn` / `wSm`（`Editor/ShopScene.cs:6350 / :6366 / :6408`）：都 `DestroyImmediate(…gameObject)`。
- `w15Host`（`ShopScene.cs:6515`）：`Object.DestroyImmediate(w15Host.gameObject)`。
- `B94S1` / `B97_SHOP2` 的 `SetHost(null, null, _)` 夹具（`ShopFamilyAssertB94S1.cs:108-116` / `B97_SHOP2_Assert.cs:130`）：**只建组件、不建树**，且都销毁了。
- 页是**子件**：`BuildPages`（`ShopWindow.cs:249-261`）把页建在 `tabHolder = Content Area/Tabs`（`MainMenuSubmenuWindow.cs:578`）下 ⇒ 销毁窗 GameObject 时会连带销毁 —— **所以「页活着 / 窗死了」在静态上解释不通**。

**两个仍在的候选（需要一条运行期判据来分）**
- **A**：`ShopScene.cs:6161` 的 `shop435` —— 只有 `shop435.Close();`（`:6306`）、**全文件没有任何销毁点**。它的销毁只能靠 `QueueDestroyOnClose` + `ReapClosedWindows`（`WindowsManager.cs:1663-1675`），而那里有一条**静默豁免**：`w.CurrentState != Closed`（被 `ShowPreviousWindow`/`OpenWindow` 带回来过）⇒ 只从待收表摘掉、**不销毁**（`:1668` / `:1673`）。
- **B**：`ShopWindow.BuildPages()`（`:249-261`）只 `tabs.Clear()`、**不销毁旧页** —— 若 `Build()` 之外还有人再跑一次 `BuildPages()`，旧页整棵子树（含 `InfoHit`）会留在 `tabHolder` 下并保持激活。⚠️ 但这条路**不能**解释 `_win == null`（旧页的 `_win` 仍是同一扇活窗）⇒ 单独不够。

**修法（方案，按顺序）**
1. **先让它可指名**（没有这一步查不动）：
   - `B103F4_Assert.ClickByQuad` 的 premise 文案补：`got?.name` · `got.GetComponentInParent<GameWindow>()` 的名字 · `got.GetComponentInParent<ShopTabPage>()` 的宿主状态；
   - `Shell/PointerLayer.cs` 加只读口 `public static WindowButton[] ButtonsAtForTest(float px, float py)`（**一跳**转发 `CollectHits`，⛔ 不加工，同 A964 那三个口的纪律），把**所有**候选打出来。**追加到类尾**。
2. **卫生断言**（放 `B103F4_Assert.RunShop()` ② 段 B 开头）：
   扫 `FindObjectsByType<ShopTabPage>(FindObjectsInactive.Include)`，凡 `OwnerWin == null`（Unity 假 null）**且** `gameObject.activeInHierarchy` ⇒ 红并逐个点名。
   需要给 `Shell/ShopTabPage.cs` 加只读口 `public MainMenuSubmenuWindow OwnerWin { get { return _win; } }`（同 `GridScroll` 的既有做法），**追加到类尾**（⛔ 别插进成员之间 —— 本文件被账按 `文件:行号` 钉着，插一行整段下游漂）。
3. **指名之后按来源修**：若是 A ⇒ `shop435` 收尾补 `DestroyImmediate(shop435.gameObject)`（或把 `ReapClosedWindows` 的静默豁免改成「`!= Closed` 且**不在 `openWindows`** 才豁免」）；若是 B ⇒ `BuildPages()` 重建前先销毁旧 `tabs`。

**还缺什么（明确）**
① `got` 到底是谁 —— `ClickByQuad` 不印；
② 陈旧页的**父链**（`Transform.parent` 链名）—— 一眼能看出它挂在谁下面；
③ `ShopTabPage._win` 没有公开读口 ⇒ 断言里判不了「宿主还活着」。

---

## 根因 vs 级联（最终）
- **BattleScene**：**1 根因、0 级联**（另 10 条是「没执行」，不是级联红）。
- **ShopScene**：**2 根因 + 1 级联**
  - 根因① = `:86381`（`ClosePackAndReopenShop` 只认 `Closed`、`bp` 恒 null）—— 独立，与 B103F4 无关；
  - 根因② = `:131964`（命中一颗宿主已销毁的陈旧 `InfoHit`）；
  - 级联 = `:132014`（根因②导致弹窗没开）。

## 按文件归并的修法清单
| 文件 | 改什么 | 备注 |
|---|---|---|
| `Editor/BattleScene.cs` | `CheckManaBuffHud(driver, Check);` 从 `:46873` 挪进 `:45243` 行内（`RunR13();` 之后 / `DetachStaticHooks()` 之前） | 零行数变化 |
| `Editor/ShopScene.cs` | `ClosePackAndReopenShop`：三档收口 + `bp` 改从 `Manager.GetOpenWindow<BoosterPackOpenWindow>()` 现取；`CheckTrue` 文案补 `实得 {CurrentState}` | 影响 8 个调用点，全是自检编排 |
| `Editor/B103F4_Assert.cs` | `ClickByQuad` premise 文案补 `got` 身份 / 祖先窗 / 全候选 | 只为可诊断 |
| `Shell/PointerLayer.cs` | 新增只读口 `ButtonsAtForTest(px,py)` | **追加到类尾** |
| `Shell/ShopTabPage.cs` | 新增只读口 `OwnerWin` | **追加到类尾** |
| `Editor/B103F4_Assert.cs` 或 `Editor/ShopScene.cs` | 加「无陈旧 `ShopTabPage`」卫生断言（B103F4 之前） | 需要上一行的读口 |
| `Shell/ShopWindow.cs` | （待证据）`BuildPages()` 重建前销毁旧 `tabs` | 只有候选 B 成立才做 |
