# `F2` — `BattleScene.cs` 4 条 + `DeckScene.cs` 2 条红的修复（第三十二会话 · 写手）

> 判据来源 = `资料/普查产出_第三十二会话/D2_18条红诊断.md`（**整篇读过**）。本文件只报 `BT-1..BT-4` + `DK-1`/`DK-2`。
> 只改了两个文件：`CardPresentation/Editor/BattleScene.cs` · `CardPresentation/Editor/DeckScene.cs`。

---

## 一、逐条（6 条；按文件分两节）

### `BattleScene.cs`

#### `BT-1`. `Editor/BattleScene.cs:9586`（旧） —— ② 免费部署：「Skyborne Deployment」8 个种子都没到手/付不起
类别：**(δ) 夹具前提不成立**
改了什么：
- 换了一张**真能进卡组**的同形战术：`Skyborne Deployment` → **`Spyder Nest`**（Sautekh 3 费，卡面
  `Deploy 2 Canoptek Scarab`）。判据：池里 `SAU46` 的 `inventoryOptions=0` / `showInCollectionOptions=5`
  ⇒ 过 `DeckRules.CannotAddToDeck`（`Core/DeckRules.cs:263-275`）；解析基线
  `工具/ruleprobe/基线/out_baseline.txt:537` = `deploy/pool///2/canoptek scarab`（**完整解析**）。
- 断言本体随之对齐：`u.Name == "Storm Guardian"` → `"Canoptek Scarab"`；`now == 2` / `occupied == before.OnBoard + 2` / 画面==引擎手牌 **三条判别力一条不少**（仍是「免费部署两张具名单位」同一形状）。
- 截图名 `15_部署_两个风暴守卫` → `15_部署_两个圣甲虫`（全仓只有这一处引用，已 grep）。
- 夹具**补了一条前置**（诊断点名要的）：在 `DriveTacticAndCheck` 第一次 `Begin` 之后断言
  **牌库 ∪ 手牌里找得到那张战术**（`BattleScene.cs` 新 `if (attempt == 0) { … check(ours, …) }`）
  —— 判据 = `BattleDriver.ResolveDeck` 那三段 `[Battle]` 出声。牌被静默换掉时这条**当场红**，
  而原来只会报出一句指向错方向的话。
🧨 那句跟着改了吗：**本条原本没有 🧨 句**；新增前置自带「找不到 = 静默换成自动凑的一副」的正向判据，② 那三条断言的旧文案未动（它们没写 🧨）。

#### `BT-2`. `Editor/BattleScene.cs:11279`（旧） —— ★ 灭自证②：只有精确绑定的卡（实得 0 条）
类别：**(α) 断言期望值过期**
改了什么：**按诊断给的【第一方案】办 —— 降为「如实报读数」**：
`Check(exOnly > 0, "★ 灭自证②…")` → **删掉该 Check**，`exOnly` 改由同段那句
`Debug.Log(P + " 精确档口径：{exHit} 条命中 · {exDiff} 条与兜底不同 · {exOnly} 条兜底给不出 · …")` 报出
（那行**本来就在报这个数**，本笔把它改成唯一出口）。原地留了 12 行「2026-10-15 就地改口」注释写清：
`voice_lines.json` + `card_vfx_by_card.json` 在 `ee784b5` 重生成 ⇒ 那一族今天 **0 条、没有见证者**，
`exOnly > 0` 已从「在查东西」变成「恒红」。
**我的选择与理由**：诊断的两条路里，**② 走不通**（它要的那张「只有精确绑定的卡」在全表里**不存在**
—— `exOnly == 0` 的就是「一个都没有」，不是「没找到」）；① 是唯一实事求是的出路。
**判别力没全丢**：灭自证那一角由 **①（`exDiff > 0`，今天 5 条）** 独担，而①盯的正是本条原本要防的
**同一个改法**（把精确档改回照抄后缀兜底 ∥ `exDiff` 与 `exOnly` 会一起塌）。
🧨 那句跟着改了吗：**是** —— 块注释那句「这两条…两条同时变 0」改成「这一条…`exDiff` 当场变 0」，
并在 `Check(exDiff > 0, …)` 上方补了一句 🧨：「把精确档改回『照抄后缀兜底』 ⇒ `exDiff` 变 0 ⇒ 上面那条红」。
⚠️ **提请调度台知悉**：这是本批**唯一一条「断言数减一」**的改动（诊断的第一方案就是它）。

#### `BT-3`. `Editor/BattleScene.cs:12546`（旧） —— 挪到**费用**上 → 换成费用那条
类别：**(δ) 夹具前提不成立**
改了什么：**按诊断给的【第一方案】办 —— 换【手牌】视图探，⛔ 不删这条、⛔ 也不改 `!board` 那道门**。
- `drv.TickTooltipAt(cv.StatWorld(CardView.StatCost))` → 新增 `var cvCost = drv.HandViewAt(0);`
  + `drv.TickTooltipAt(cvCost.StatWorld(CardView.StatCost))`；原来那条 `Check(ShownBody == TipText.Cost, …)`
  **逐字保留**（只附加说明「探的是手牌视图」）。
- 另补一条**前置**：`Check(cvCost != null, "（前提）手牌里有一张卡可以探费用…")`（手牌空时下面那条是空跑）。
- 根因写进注释：`Core/CardView.cs:4299-4306` 是 `if (!board && ShowsCost(Data) && HitAt(l, CostAt))`，
  出生/换形态两个入口同口径（`CardView.cs:901`）⇒ **场上那套照原版不印费用**，而夹具取的是
  `drv.BoardViewAt(s, true)` ⇒ 原断言**恒红**。`TickTooltipAt` 先扫 `_handViews` 再扫场上
  （`BattleDriver.cs:15346-15353`）⇒ 手牌视图这条路与鼠标那条**同一个函数**。
- 手牌视图存在的依据：本节上方 ⑥「手牌 ≥ 2 张」+ ⑥c 收尾「手牌还原成 baseCount 张」都在同一局里跑。
🧨 那句跟着改了吗：**本条原本没有 🧨 句**；未新增恒真/恒假的措辞。

#### `BT-4`. `Editor/BattleScene.cs:19717`（旧） —— ★ A964/E2：战斗日志面板（0 行）
类别：**(δ) 夹具前提不成立**
改了什么：**只在夹具侧补动作**，`Check(rowsN964 > 0 && rowsBad964 == 0, …)` **一个字没动**。
- 在 row 3 块开头加：**试到 `Ctx.ActionLog` 里出现一条非 `Draw` 的记录为止**地驱动回合，
  上限 6 回合 —— `drv964.SimulateEndTurn(); drv964.SimulateAiTurn(); Step(0.3f);`
  （与 `DriveTacticAndCheck` 同一个驱动形状；驱动之后仍是**我方回合**）。
- 新增一个局部工具 `HasNonDrawEvt964(BattleContext)`（与外层那几个局部函数同一层，紧挨 `CloseAll964`）。
- 判据写进注释：`RefreshBattleLog` 的 `case EvtKind.Draw:` = `skipRow = true`（主对话 2026-10-18 裁定），
  而夹具从 `Begin` 到这儿**一个动作都没做过** ⇒ `ActionLog` 全是回合开始那一抽的 `Draw` ⇒ `FilledRows == 0`。
- ⛔ **不写死「走几个回合」**（那会随种子/牌序漂）；⛔ **没有往面板里手塞一行**（那是恒真的假绿）。
🧨 那句跟着改了吗：**保留原文**，未改（「把 `RowAt` 的 `WorldW/WorldH` 换成写死的常量 ⇒ 红」在 0 行变 >0 行之后**才真正成立**）。

### `DeckScene.cs`

#### `DK-1`. `Editor/DeckScene.cs:2697`（旧） —— ★ N14 ①：有督军时费用那一格显示
#### `DK-2`. `Editor/DeckScene.cs:2722`（旧） —— ★ N14 ③：督军放回去 ⇒ 显示回来
类别：两条同判 **(δ) 夹具前提不成立**（`N14` 实现本身没坏）
改了什么：**只改探针的滚动位**，两条断言的期望值一个字没动：
- `_rt.UiScrollFilters(-1e6f);` → **`_rt.UiScrollFilters(-1e6f);  _rt.UiScrollFilters(220f);`**
  （`UiScrollFilters` 是**增量**，所以先归 0 再 +220）。
- 220 是**按原版字面量算出来的**（算式已写进注释）：`$cost:1` 滚到 0 时绝对 y =
  `FltY 156 + CostTop 1009.02 + CostContentTop 65` = **1230.02**（65 高 ⇒ `[1230.02, 1295.02]`，整行在带口
  `[156, 1080.1]` 之下 ⇒ `ClipCellToBand` 把它 `SetActive(false)`）；减 220 ⇒ **`[1010.02, 1075.02]`，整格落进带口**。
- ⛔ **没有采纳「滚到底」（诊断举例的 `1e6f` ⇒ 464.92）**：那样 `$fac:<第一阵营>`（Army 首格，
  滚到 0 时 `[385.02, 485.02]`）被推成 `[-79.9, 20.1]` = 整块出带口 ⇒ 下面 ② 那条「`Army` 那一族
  **照旧活着**」的反向控制会**假红**。⇒ 选了一个**两格同时在带口内**的滚动位（220）。
- 本节收尾补一句 `_rt.UiScrollFilters(-1e6f);`（**还原滚动位**）—— 下面那些节都在滚动位 0 上读抽屉
  （同文件「滚到顶时抽屉里的格子节点数」那条），不还回去会把红挪到别人身上。
- **顺手订正了诊断 §顺手发现 #4 那两处互相打架的注解**：`:2683-2684` 原写「滚动位 0 时**第 2 排**
  （档 5~8）整块在带口外」→ 改成「**Cost 整行**（两排 8 档）都在带口之下」（判据 = 上面那个 1230.02；
  这与同文件另一处早就写着的「Cost/Type 两族全在带口外」一致）。⚠️ 两处引用的行号 `:2939` 已按改后位置订正为 `:2962`。
🧨 那句跟着改了吗：**保留原文**（本节 🧨 那四条判别式未动；它们现在**才真正生效** —— 原来 ② 那三条在
「探针本来就不显示」时不具判别力，诊断已点名）。

---

## 二、机器复核

- `typecheck.sh`（`TMPDIR=/tmp/wf_a2`，避免与别的写手互相覆盖）：**运行时错误数 0 / 编辑器错误数 0**
  （改前跑过一次 0/0、改完又跑过一次 0/0，两次都贴在上面那句同一格式下）。
- `git diff --numstat`（只列我这两个文件）：
  ```
  99	10	Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
  26	3	Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs
  ```
- 行尾（**二进制读**，`CRLF == LF` 即「纯 CRLF」）：
  ```
  BattleScene.cs  改前 CRLF 23457 / LF 23457  →  改后 CRLF 23546 / LF 23546
  DeckScene.cs    改前 CRLF  8735 / LF  8735  →  改后 CRLF  8758 / LF  8758
  ```
  两处都是 `CRLF == LF` ⇒ **行尾一个字没翻**；`numstat` 也印证（99/10 与 26/3，不是整篇重写）。
- 副本：`d:/4/_tmp_view/BattleScene.cs.bak_a2` · `d:/4/_tmp_view/DeckScene.cs.bak_a2`（改前）。
- ⚠️ **本批会动的断言总数**：`-1`（`BT-2` 那条降为读数）`+2`（`BT-1` / `BT-3` 各补一条**前置**）
  ⇒ 合计 **+1**。看 `battle.log` / `deck.log` 的合计行时**不要拿旧总数当基线**。

---

## 三、没查清 / 没做的

1. 🔴 **六条红都没有真机复核**（⛔ 本批不许跑 Unity）—— 全部是**静态推理 + 诊断的读数**。
   最有把握的三条 = `BT-1`（成因有运行日志三段实锤）· `DK-1/DK-2`（算术可逐项核，见 §一）· `BT-3`（判据一行代码）。
   最需要下一次自检确认的两条 = **`BT-4`**（驱动回合能不能真产出非 `Draw` 事件 —— 诊断 §三·1 自己也没读到
   `ActionLog` 的 `Kind` 直方图）· **`BT-1` 的前置**（`Spyder Nest` 会不会被 `DeckBuilder.FromDeck` 当
   「引擎结算不了」丢掉 —— 我用解析基线 `out_baseline.txt:537` 旁证，**没有跑过引擎**）。
2. ⚠️ **`BT-1` 换卡是「换一张同形的」而不是「修那张卡」** —— `Skyborne Deployment` 在池里
   `type: "tactic"` 而 PnP 成品图在 `Aeldari/2天赋/`（诊断 §顺手发现 2）⇒ 它**大概率根本不是一张能进卡组的卡**。
   那笔账属 `A970`／`card_ids.json` 一族，**不在我的白名单内**（卡池数据我不能动）。**这条只是绕开，没修**。
3. ⚠️ **`BT-2` 那条是「断言减一」**（见 §一）—— 若调度台认为「宁可留一条钉数据态」，
   可以反过来把 `exOnly` 钉成 `Check(exOnly == 0, …)`；我**没有**那么做（那会让一次良性的数据变更变红，且
   等于断言「我们希望它没有的那个东西不存在」，语义颠倒）。
4. `DK-1/DK-2` 的 220 依赖 `Factions() == 13`（Army 行 550 高）。**阵营数一变这儿要重算**（注释里写了）。
   `Factions()` = `_pool` 里去重后的阵营集合（`Deck/DeckEditorState.cs:518-533`），与 `WarlordGatedPool` 无关 ⇒ 今天恒 13。
5. 诊断 §三·3 那个 `1230.02 vs 1074.02` 的分歧**已就地结清**：**`1230.02` 对**，
   `1074.02` 是漏了 `+ FltY(156)`（`DeckRuntime.FltAbs` = `FltY + y − _fltScroll`，`DeckRuntime.cs:7145`）。

---

## 顺手发现（超出本任务范围，供调度台分流）

1. ⚠️ **`DeckScene.cs` 里同一个量（当前行号）被别处按旧行号引用**：本笔把 `:2939` 改成 `:2962`，
   是因为我自己的插入把它推下去了。**这类「文件内 `:行号` 引用」在本仓很密、而且没有任何守卫** ——
   谁往中间插一行，引用就静默指错（本笔就是撞上的）。可考虑改成「引函数名/断言原文」而不是行号。
2. ⚠️ **`BattleScene.cs` §15b 的 tooltip 探针全程拿**场上**卡当靶**，而「费用」是**唯一**一格
   场上形态不印的（照原版）。⇒ 这一节的其它数值（近战/远程/生命）在场上形态下能过，
   **只是因为 `BoardMeleeAt` 那组位置恰好也有命中区**；将来若有人把场上那套数值改成「不响应」，
   同族的另外三条会**一起**红，根因（「场上不印」）和症状会混在一起。建议在这节头注里点一句。
3. ⚠️ **`DeckWithTactic` / `MakeDeck` 凑牌时既不筛「能不能进卡组」、也不看 id 形状** ——
   `DeckWithTactic`（`BattleScene.cs`）挑单位只看 `Type`/`Faction`，战术卡是**调用方给的名字**。
   而 `BattleDriver.ResolveDeck` 事后会**静默降级**（摘牌 → `TooFewCards` → 换自动凑的一副）。
   ⇒ 任何用这两个夹具的用例都可能踩同一个坑，而症状永远是「拿不到 / 数字对不上」而不是「牌被换了」。
   本笔只给 `DriveTacticAndCheck` 补了前置（`BT-1`），**别的夹具没查**。
   （相关：`Skyborne Deployment` 的 id 是 32 位 GUID `e59ac9e4…`，即诊断 §顺手发现 1 那 113 张的一族。）
