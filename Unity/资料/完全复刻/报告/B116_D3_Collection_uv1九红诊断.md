# B116_D3 · `CollectionScene.Run` 的 `B-326` uv1 九红诊断（只读）

- 会话：第一百一十六会话 · 只读诊断代理 `B116_D3` · 2026-11-02
- 输入日志：`D:\4\_tmp_view\collection.log`（行号如下，均为该文件行号）
- 被测：`Assets/CardPresentation/Editor/CollectionScene.cs:13417` `CheckB326Uv1`（调用点 `:13250` 聚合行**最末**）
- 实现：`Assets/CardPresentation/Battle/ImageQuad.cs` 类尾 `SyncShineUv1()`（`:1054`），调用 `:303`（`SetMaterial`）/ `:502`（`RebuildMesh`）/ `:975`（`RebuildNineSliceMesh`）

## 结论（一句话）

**9 条全部是 (α) 断言本身错 —— 不是实现缺陷、不是本批产品回归。** `CheckB326Uv1(win)` 排在
`B108_A4_Assert.RunCollectionCloseScrolls(win)`（该组**以 `win.Close()` 收尾**）之后，紧跟着
`Q2_Assert.Run()` 在没有传 `win` 的情况下**自建并打开一台新收藏窗**，`WindowsManager.ReapClosedWindows`
于是把宿主手上那台已关闭的 `Collection Menu Variant` **真销毁**了 ⇒ 传到 `CheckB326Uv1` 的 `win`
已是 Unity 假 null ⇒ `win.PageRoot(1)` 恒 null ⇒ 后面 8 条全是从这一条级联下来的。

处置分布：**α = 9 · β = 0 · γ = 0 · δ = 0**。

## 因果链（可逐跳复查）

1. `:13250` 聚合行上：`... B108A4_Assert.Run(); B108A4_Assert.RunCollectionCloseScrolls(win);`
2. `Editor/B108_A4_Aud02Card_Assert.cs:196-252`：进组时先断「窗开着」（日志 `:925588` ✓ B0-前提②），
   然后 3 轮 `win.Build()` → `win.Close()`，**循环最后一句就是 `win.Close()`**，
   日志 `:925616` 的 `[A4] B108A4(AUD03) 合计：32 通过 / 0 失败` 之后窗处于 `Closed`。
3. `GameWindow.Close()`（`Shell/GameWindow.cs:781`）尾句 `Manager.QueueDestroyOnClose(this)` —— 只记账、不当拍销毁。
4. 下一条语句 `Q2_Assert.Run()`（**不传参**，见 `:13250` 下一物理行）⇒ `Editor/Q2_Assert.cs:90-93`
   `CollectionWindow.Create(wm); wm.OpenWindow(win);` ⇒ `Shell/WindowsManager.cs:292` → `ReapClosedWindows`
5. 日志 `:925991`：`[Win] 关过的那一具 `Collection Menu Variant` ⇒ **销毁**`，
   栈 = `ReapClosedWindows (WindowsManager.cs:1670) ← OpenWindow (:292) ← Q2_Assert.cs:93` ——
   **被销毁的正是宿主的那台 `win`**（Q2 自己那台是 `keep`，`ReapClosedWindows` 明确跳过 `w == keep`）。
6. 之后 `B110_F1/F2`、`B111_S2`、`B112_G`、`B113_G3` 都不吃 `win`（所以全绿），
   到 `CheckB326Uv1(win)` 时 `win != null` 用的是 `UnityEngine.Object` 的重载 ⇒ **已销毁 ⇒ false**
   ⇒ `var page = win != null ? win.PageRoot(1) : null` 恒 null，**一句 `win.PageRoot(1)` 都没跑过**。

## 9 条逐条

| # | 行 | 断言原文（摘） | 期望 | 实得 | 判定 | 证据 |
|---|---|---|---|---|---|---|
| 1 | `:13424` | （前提·不静默）Cards 页取得到（`win.PageRoot(1)`） | True | **False** | α·**根因** | `win` 已销毁；日志 `:930243` |
| 2 | `:13428` | （前提）`Header Filters ▸ WIldcard Display ▸ Background` 取得到 | True | False | α·级联 | `page==null ⇒ hdr==null` |
| 3 | `:13431` | （前提）那颗 `Background` 是九宫格 **9 块** | 9 | **0** | α·级联 | `quads=null ⇒ n=0`；日志 `:930274` 段前 |
| 4 | `:13461` | ★ 9 块的材质全是 `WildcardShine`（写入门槛） | 9 | 0 | α·级联 | 循环体一次没进；日志 `:930274` |
| 5 | `:13463` | （前提）9 块的网格都是 4 顶点单 quad | 9 | 0 | α·级联 | 同上；日志 `:930289` |
| 6 | `:13464` | ★★ 9 块的 `Uv1VertexCount` 全是 4 | 9 | 0 | α·级联 | 同上；日志 `:930304` |
| 7 | `:13466` | ★★ 9 块 uv1 = 自己那块 uv0 的包围矩形 | 9 | 0 | α·级联 | 同上；日志 `:930319` |
| 8 | `:13485` | （前提·不静默）同页找到一具非 shine、有材质、4 顶点 quad | True | False | α·级联 | `page==null ⇒ all=null`；日志 `:930334` |
| 9 | `:13486` | ★★ 灭自证：非 shine 的 quad ⇒ `Uv1VertexCount` = 0 | 0 | **-1** | α·级联 | `plain==null ⇒ -1` 哨兵；日志 `:930350` |

> 第 9 条的 `-1` 是断言自己写的「没找到折算符」，不是读口返回的错值 —— 反过来证明它**确实**是级联。

## 为什么不是 β（实现没写 uv1）

- `CollectionScene.cs:13106-13140` 那一节（同一颗 `Background`、同一批 9 块、同一个 `win`）在本轮**全绿**：
  日志 `:919510`「★ 9 块的材质 shader 名**全是** `CardPresentation/WildcardShine`」——
  说明**窗还活着的时候**节点、9 块、材质都在。
- `Effects/UIEffectTogglerCoffeeEffects.cs:279-283` `Attach` 逐块调 `q.SetMaterial(mat)`；
  `ImageQuad.cs:303` 是 `_mr.sharedMaterial = m; SyncShineUv1();` —— **先赋材质、后写 uv1**，顺序正确。
- `SyncShineUv1`（`ImageQuad.cs:1054-1078`）先用 `UsesShineShader` 卡门槛，再取 `mesh.uv` 的包围矩形，
  四顶点写同一组 `(xMin,yMin,xMax,yMax)` —— 与断言第 7 条的独立算期望**同语义不同源**（满足灭自证）。

## 为什么不是 δ（`Background` 不是九宫格 9 块 / 材质不对）

Δ 的前提（`Header Filters ▸ WIldcard Display ▸ Background` 存在、9 块、材质 shine）**在窗活着时被 `:13106` 那一节证过**；
本轮失败时**节点查找根本没发生**（`page == null`）。⇒ 不是「前提不成立」，是「前提**没被求值**」。

## 是否本批回归（γ）

**否。** 产品代码路径一个字没在这个顺序上变过：`win.Close()` 收尾（`B108_A4`）与 `Q2_Assert.Run()` 自建窗
都是**既有行为**，`ReapClosedWindows` 的延迟销毁也早就在。本批新增的只是**把一条新的断言排在了它们之后**
（`:13250` 聚合行最末），于是第一次踩上「上游已经换了对象」这条坑。

## 最小修复方案（⛔ 本代理未动手）

1. **主修（一行级、零新增行风险最小的那个）**：把 `CheckB326Uv1(win);` 从 `:13250` 聚合行**最末**
   挪到**同一物理行、`B108A4_Assert.RunCollectionCloseScrolls(win);` 之前**（即 `B107_W14_Assert.RunCollection(win)` 之后）。
   该点窗仍是 `Open`，页与材质都在；`PageRoot(1)` 本身会懒建，与生产路径同形。
   ⚠️ 同一物理行末尾有 `//` 注释 —— 插入点必须在**第一个 `//` 之前**（本仓 2026-10-30 踩过 6 次）。
2. **配套（把「9 条级联」压成「1 条」）**：在 `CheckB326Uv1` 开头补一条**硬前提**
   `CheckTrue(win != null && win.gameObject.activeSelf, "（前提）宿主收藏窗还活着且开着 —— 本节必须排在 `B108_A4` 的关窗组之前")`，
   并把 `page` 那句改成 `win != null && win.gameObject.activeSelf ? win.PageRoot(1) : null`。
   （不是可选项：以后再被谁重排一次，至少要**读得懂**是「对象没了」，而不是看到 9 条红去怀疑实现。）
3. ⛔ **不要**改成「窗死了就自建一台」：那会把 `B-326` 悄悄变成「另起一扇窗的等价断言」，
   与 `:13106` 那节量的**不是同一棵树**，且建页是重活。

## 没核清的部分（如实记）

- 修复后 `CheckB326Uv1` 的 6/7/9 三条能否真绿，**必须跑一次 `CollectionScene.Run` 才作数** ——
  本代理按铁律只读、未跑 Unity。⚠️ 尤其第 8 条「同页另取一具非 shine、有材质、4 顶点 quad」是**扫全页碰运气**，
  若 Cards 页那一刻恰好没有合格件，会以「前提不成立」的形式单独红（那是断言设计问题，不是 uv1 的问题）。
- `B108A4` 的 A 组（`:75`，不吃 `win`）在关窗组之前跑，与本问题无关，未展开。
