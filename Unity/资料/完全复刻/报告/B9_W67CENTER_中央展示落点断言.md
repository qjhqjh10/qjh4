# B9_W67CENTER —— 敌方打出卡「中央展示」落点的两条结构断言

> 代理：**B9_W67CENTER** · 2026-10-27（`B9` 波）
> 上游：`资料/完全复刻/报告/B9_W57BDR4_敌方展示位与技能面板.md` §五·5 的第 1 条（「断言缺口」）。
> 文件所有权：`CardPresentation/Editor/BattleScene.cs`（**独占**）· 本报告 · `账/_parts/B9_W67CENTER.tsv`。
> ⛔ 未动：`*.unity` / `ProjectSettings` / `BattleDriver.cs` / `SkillPanel.cs` / 本波别线的任何文件。

---

## 一 结论

### ① 落点上现在**有两条结构断言**（此前零条）

`Editor/BattleScene.cs` 里此前只有 `:15390` 的「载体建出来了」，**没有任何断言钉落点** ⇒
把终点改回 `LayoutSpace.ToWorld(0.5f, 0.5f)`（屏幕正中 960,540）**不会红**。现补两条：

| # | 断言（写在哪） | 断什么 | 改坏哪处会红 / 为什么结构上互斥 |
|---|---|---|---|
| **A1** | `BattleScene.cs:48940` | 桌面档落点距屏幕正中 **(960,540) > 100 px** | 🧨 终点改回 `ToWorld(0.5f,0.5f)` ⇒ 距离 0 ⇒ 红 |
| **A2** | `:48945` | 桌面档 `x < 860`（明确在**中线左侧**） | 🧨 X 偏移写成 `+26` 或整项漏掉 ⇒ 落回 960 / 跑右侧 ⇒ 红 |
| **A3** | `:48954` | 两维 = 「原版锚点 (960,50) + 原版偏移(−26,+24)×14.835」±2 px | 🧨 只挪 X 不挪 Y（落点变 (574,540)）⇒ **A1 照样绿、A3 红**（A1 单独不够，所以这一条必须有） |
| **B1** | `:48977` | **X 反号判别式**：`x(小屏) > 960 > x(桌面)`（两态各取一次落点） | 🧨 改回正中（两档都 960，两个不等号都不成立）· 或两档读同一字段（同号 ⇒ 同侧）⇒ 红。**一个错误值不可能同时落在 960 的左边和右边** —— 这就是「结构上互斥」的来源 |
| **B2** | `:48985` | 小屏档 = `960 + 25×14.835 = 1330.9`（±2）**且** `\|Δx\| = 756.6 > 500` | 🧨 小屏档忘了换字段（仍读 −26）⇒ 两档同值、差 0 ⇒ 红 |

实测期望（**由原版判据算出，⛔ 不从被测实现读回**）：桌面档 **(574, 406)** · 小屏档 **(1331, 406)**。

### ② 量法：走**真演出**读落点（不新开生产口子、不改 `BattleDriver`）

`W67CenterAssert.Measure(...)`：调 `driver.DisplayEnemyCardInCenter(...)` 建出那张临时卡 →
`CardTween.Advance(0.2f)` 推过 `DOMove(0.15 s)` → 读 `v.transform.position` →
`LayoutSpace.ToDesignPixel(...)`（`FromPixel` 的逆）读回**设计 px**。

- 读的是**卡本体真的停在哪**（端到端），不是把公式再算一遍 ⇒ 改坏接线必红。
- 🔴 **只断 px 语义**：⛔ 不用世界版的 `VarsGlobal.PlayedCardDisplayPosition`（它的偏移单位是原版 UI 画布世界单位
  = 14.835 px，而 `LayoutSpace` 的 1 世界单位 = 108 px ⇒ 直接用会差 **7.28 倍**）。
- ⚠️ `FromPixel` / `ToDesignPixel` 是**严格互逆**（x 用 `1920/VisibleWidth`、y 用 108 那条，`VisibleHeight` 恒 10）
  ⇒ 这两条断言**与宽高比无关**，16:9 之外也成立（不要被「设计 px」三个字骗成只在 1080p 有效）。

### ③ 前提全部**硬检查**（红），⛔ 没有 `if (前提) Check(结论)`

**共 10 条前提**（`Check` 里 4 条 + `Measure` 里 6 条）（`:48922` 驱动/卡池 · `:48927` 非单位卡 · `:48930` 第一态=桌面档 ·
`:48967` 第二态=小屏档 · `:48996` 载体建出来 · `:49000/:49004` 推过 DOMove 后载体还在 ·
`:49009` **真的移动过** · `:49012` **两次采样一致 = 读的是停稳的落点** · `:49014` 演完已收摊）。

其中两条是「读数口非退化」的关键：

- `Distance(p1, p0) > 50` —— 不成立就说明补间没推进、读到的是**起点**；那种情况下「不在正中」会**假绿**。
- `Distance(p2, p1) < 0.5` —— 不成立就说明 `DOMove` 被改长、读到的是**半路**（那时也可能离中心 > 100）。

失败时 `Measure` 返回 **NaN**（不是 0）⇒ 调用点每一条比较都变红，⛔ 不会伪装成通过。

### ④ 零账漂移

调用点是**行内 1:1 替换**（`:15410` 原为 `}`，现为 `W67CenterAssert.Check(driver, Check); }`），
断言体追加在**文件尾**的 `static class W67CenterAssert`（`:48908`）⇒
**本文件既有的每一行的行号一个都没动**（48,882 → 49,018，+136 全在文件尾）。
现核：`账/**/*.tsv` 里**没有任何一条**落点指向 `Editor/BattleScene.cs` 的 `:15410` 或更后面（35 条落点最大 24,468）。

---

## 二 证据（文件:行号）

| 事项 | 出处 |
|---|---|
| 落点公式（`enemyHand + right·X − up·Y`；桌面 X `+0x54 = −26` · 小屏 X `+0x58 = +25` · Y `+0x50 = 24`） | `decomp_full/CardScript__GetEnemyCardPlayedDisplayPosition.c:60-75` |
| 展示位 state0 用它做 `DOMove(…, 0.15)` | `decomp_full/CardScript._DisplayEnemyCardInCenter_d__263__MoveNext.c:28-46` |
| `enemyHand` 屏幕锚点 **(960,50) @1080p** | `资料/完全复刻/判据/enemyHand屏幕锚点_判定.md` §一·1 |
| 六个偏移的原版字段值（常量表） | `RuleEngine/Core/GameplayVariables.cs:606-624` |
| px 换算 **14.835 px/世界单位 @1080p** + `y 向下为正` | `RuleEngine/Core/GameplayVariables.cs:663-693`（`PlayedCardDisplayOffsetPx` · `:682`） |
| 被测实现：落点 = `FromPixel(960+offX, 50+offY)` | `CardPresentation/Battle/BattleDriver.cs:35555-35559`（`EnemyCardPlayedDisplayPointWorld`）· 调用点 `:25911` |
| 演出：起点 = 手牌末位、`DOLocalMove(centerLocal, 0.15)`、`tailAt = 0.1 + 0.66·hold + 2.6·hold ≈ 0.4912` | `BattleDriver.cs:25793-25800` · `:25909-25913` · `:25940` · `:25975-25991` |
| 临时卡的父 = `hudRoot`（`SetParent(parent,false)`）⇒ `InverseTransformPoint`/`TransformPoint` 严格互逆 | `Core/CardView.cs:2149-2155` · `BattleDriver.cs:25887` |
| 批处理下补间手动推（`UpdateType.Manual`） | `Editor/BattleScene.cs:526` · `Core/CardTween.cs:85-88` |
| 契约：`Check(bool,string)` 是 `Run` 的局部函数、要靠**委托外传** | `Editor/BattleScene.cs:637` · 先例 `:3596`（`CheckCardViewState` / `K4HoldAssert`）· `:24175`（`W11ShakeAssert`） |
| 本次调用点 / 断言体 | `Editor/BattleScene.cs:15410` · `:48908`（类）· `:48940/:48945/:48954`（A）· `:48977/:48985`（B）· `:48996-49014`（前提） |

---

## 三 没查清的部分

1. **非 1080p 档的像素落点仍然没算** —— `PlayedCardDisplayOffsetPx` 的 `screenHeightPx` 传死 1080
   （与 `RevealEndPx` 同口径，W57 报告 §三·1 已记）。两条断言里 A3/B2 的期望值同样是 1080p 口径。
   ⚠️ **但 A1/A2/B1 是结构量（>100 / <860 / 左右两侧），与分辨率无关** ⇒ 换分辨率也不会假绿。
2. **断言覆盖的是「落点计算 + 演出终点」，不是「事件 → 中央展示」的整条链**：
   后者（`EvtKind.Play` → `MaybeDisplayPlayedCardInCenter`）有它自己的计数断言
   （`PlayedCenterDisplaysForSeatForTest` 的 delta，`Editor/BattleScene.cs:31929-31940`）。
   两段合起来才覆盖全程，⛔ 别把这一段当成「整条链都验了」。
3. **`Measure` 每次调用会让全局补间时间前进约 1.15 s**（0.2 + 0.15 + 0.8），两次 ≈ 2.3 s。
   本文件在 `:15406` 本来就有一次 `Advance(1.5f)`（同一位置已「推到头」），所以**影响面很小**，
   但**没有逐条核**「后面有没有断言依赖某个补间正飞到一半」。⇒ 若 `BattleScene.Run` 出现
   与本件无关的动画类红，**先看是不是这里多推了 2.3 s**。
4. **`W67CenterAssert` 是嵌套静态类**（照 `W11ShakeAssert` / `K4HoldAssert` 的先例）。
   不是 `MonoBehaviour` ⇒ 不涉及「类名 == 文件名」那条（`!u!115` 存根）纪律。
5. **本波禁跑 Unity ⇒ 这两条断言一条都还没真跑过**（`typecheck 0/0` 只证明编得过）。
   由调度台在同步点跑 `BattleScene.Run` 验。

---

## 四 改动清单

| # | 文件 | 行 | 改了什么 | 行数变化 |
|---|---|---|---|---|
| 1 | `CardPresentation/Editor/BattleScene.cs` | `:15410` | 原来那一行只有组块的右括号 `}` ⇒ 改成「语句 + 同一行右括号」：`W67CenterAssert.Check(driver, Check); }`（**行内 1:1 替换**，语句放在右括号前） | **0**（本行仍是 1 行，下游行号零漂移） |
| 2 | 同上 | 文件尾（`:48908` 起） | 新增 `static class W67CenterAssert`（5 条 ★ 结论断言 + `Measure` + 10 条硬前提；带判据头注、为什么不用世界版偏移注） | **+136**（全在文件尾，既有行号一个不动） |
| 3 | `资料/完全复刻/报告/B9_W67CENTER_中央展示落点断言.md` | — | 本报告 | 新建 |
| 4 | `资料/完全复刻/账/_parts/B9_W67CENTER.tsv` | — | **表头 + 1 行**（`战斗界面_HUD.tsv:756` `GetEnemyCardPlayedDisplayPosition()` 的整行副本；改 `我们的落点`/`差异`/`轮次`/`备注` 四列，**状态 C / 等级 P2 原样带走**） | — |
| 5 | `资料/完全复刻/账/_parts/_gen_B9_W67CENTER.py` | — | 上面那份补丁的**生成器**（只读主表、只写 `_parts/`；**不带 `--apply` 什么都不做**） | 新建 |

**校验（全部跑过）**

- `TMPDIR=/tmp/wf_w67 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- 行尾/行数（二进制现读）：`BattleScene.cs` **全 CRLF**（CRLF 49,018 / LF 49,018 / 裸 CR **0**），48,882 → **49,018**（+136 全在文件尾）。
- 账补丁：现读 **11 列 × 2 行**（表头 + 1）；`工具/check_patch_conflicts.py` ⇒ **没有跨补丁重复键（2 份补丁）**。
- 漂移：`账/**/*.tsv` 里指向 `Editor/BattleScene.cs` 的 35 条落点，**行号全部未变**（最大 24,468，均在我插入点之前）。

---

## 五 顺手发现（只报，未越界）

1. **账 `战斗界面_HUD.tsv:756`（`GetEnemyCardPlayedDisplayPosition()`）在 W57 之后就是错的**：
   它的 `我们的落点` 仍写「落点仍是我们挑的 (960,540)」、`差异` 仍写「**仍把卡移到屏幕正中**」
   —— 那是 `B9_W44HUD` 时的现状，W57 已把终点改成原版落点。**铁律 5**（记错就地改）⇒
   我在这份补丁里**追加了带日期的订正段**（不改旧段，保留更正链），并把 `我们的落点` 列更新到
   `BattleDriver.cs:35555（EnemyCardPlayedDisplayPointWorld）` + 断言宿主。
   ⚠️ **状态列仍是 C、等级仍是 P2，我没有单方面改级** —— 建议调度台在 `BattleScene.Run` 跑绿后判 C→A。
2. **`EnemyCardPlayedDisplayPointWorld()` 是 private**，`Editor/` 侧拿不到那个点 ⇒ 本件只能走「真演出 + 读卡位置」量。
   这**反而是更强的验法**（端到端），但如果将来有别的自检只想拿那个点，得先给它一个 `*ForTest` 转发口
   （⛔ 别在测试里再算一遍公式 —— 那就是自证）。本件**没加**那个口（改 `BattleDriver.cs` 不在我的白名单）。
3. **`SmallScreenUI.Set(true)` 会顺带把 `ChosenManually` 置 1**（`Shell/TransformScalerBySmallScreenUI.cs:82-89`）。
   我在 `finally` 里只恢复了 `Enabled` + `PersistOverride`（与本文件 `:6170-6186` 那段两态写法同一先例），
   `ChosenManually` 由 `Run` 收尾那句 `if (!ssWasChosen) SmallScreenUI.ResetForTest();`（`:45674`）兜底。
   ⇒ **顺序上安全**（我这一段在收尾之前），但「两态自检都要记得别把 ChosenManually 留成 true」这条值得记一笔。
4. **`HandRow01` 的兜底档会改变「起点」**：敌方手牌为空时它返回
   `(0.5, HandLayout.EnemyBaselineY)`（`BattleDriver.cs:24542`）。本件的「真的移动过 > 50 px」前提在
   那一档下同样成立（起点与落点相距约 500 px）⇒ 不会因卡池/手牌状态假红；但**这不是我算过的保证**，
   如实记在这里。
5. `BattleScene.cs` 里那条「同族旧话」顺带核过：`:25904` 的注释（W57 改的）与
   `:15350` / `:15378` 那两段「中央展示」注释**都不矛盾**，无需订正。
