# DIAG · `BattleScene.Run` 六条红（2026-10-14 收口自检 · 只读诊断 · 未改任何文件）

- 现场：`d:/4/_tmp_view/battle.log`（285,997 行）—— `=== 合计：2862 通过 / 6 失败 ===`（log:287898）
- 基线：上一趟 `d:/4/_tmp_view/BattleScene_Run.log`（Oct-13 19:25，`2807 通过 / 1 失败`）—— 用来分「新红 / 老红」

## 逐条判定

### 红 1+2：相机 / HUD 根「放回原位」（log:165028 · log:165040）
- 判定：**β 本会话回归**（两条同一根因，只差一处）。
- 证据：`Editor/BattleScene.cs:12358` 是 `CardTween.Advance(0.25f);   // 0.18 + 0.25 > 0.4 ⇒ 序列跑完`，
  而它依赖的那句 `CardTween.Advance(0.18f);` 本会话**被删掉了**（`git diff BattleScene.cs` 里的 `-` 行）：
  W11 把「t=0.18 落到谷底」那条**正弦**断言换成纯数学采样（`BattleScene.cs:12338-12353`，不再推进时间）。
  ⇒ 实际只推进 0.25 s < 序列 0.4 s（`ShakeAttackTime 0 + Sustain 0.1 + Decay 0.3`，`Core/CardFeel.cs:183-187`；
  `CardTween.Advance` = `DOTween.ManualUpdate`，`Core/CardTween.cs:76-79`）⇒ tween 未跑完、`OnKill` 没触发
  ⇒ 相机/HUD 停在偏移位（复位那半 = `CardFeel.cs:863-868` 的 `OnKill`）。
- 上一趟这两条都是 **✓**（`BattleScene_Run.log:125444` / `:125456`）⇒ 不是老红。
- 该改成：`CardTween.Advance(0.45f)`，并改掉「0.18 + 0.25」那句注释。谁改坏的：W11（`W11_动画时长与震屏.md:86` 自述「改掉两条旧断言」）。

### 红 3：灭自证 `drawFromEnd`（log:10856 · 断言在 `BattleScene.cs:921-922`）
- 判定：**α 断言把比较对象写错**（结构上恒红，任何实现都过不了）。
- 证据：`Battle/BattleDriver.cs:11232-11233` —— `int idx = count - 1;` / `if (drawFromEnd >= 1) idx = count - drawFromEnd;`
  ⇒ `drawFromEnd = -1` 与 `= 1` **都**解析成 `count-1`；断言却拿 `p2`（fromEnd=1）与默认(-1)比 ⇒ 恒等 ⇒ 恒红。
  实测印证：`p2.x` 0.6223 == `dMe.x` 0.622（log:10411，那一条就是默认锚点、✓）。
- 该改成：把 `p2` 换成 `p1`（fromEnd=2 ⇒ `count-2`）：
  `Mathf.Abs(p1.x - driver.HudAnchor01(EvtKind.Draw, true, -1).x) > 1e-3f`
  —— 这才对上它自己的文案「改回 `count - 1` ⇒ 本条与上一条**同时**红」（现算 0.5408 vs 0.6223 = 0.0815 > 1e-3 ⇒ 绿）。

### 红 4：同批 2 张的落点「y 相等、x 不等」（log:10844 · 断言在 `BattleScene.cs:919-920`）
- 判定：**α 断言期望值过期** —— 「y 相等」只在手牌 `count == 2` 时成立，而那一刻 **count = 4**。
- 证据：落点 = `HandRow01 → HandLayout.SlotPosition`，`y = baselineY + arc`，
  `arc = ArcHeightFull · Arc01(t) · hMod(count)`，`t = idx / (count-1)`（`Hand/HandLayout.cs:488-496`）。
  · **count = 4**：`p1.x 0.5408 / p2.x 0.6223`（log:10844）反推 `SpacingFor(4) = 0.0815`，
    且 `0.6223` 正是 `count-1` 槽（`dMe.x`，log:10411）；`0.0815 × 17.778`（= 1920/108）= **1.450 世界**，
    与 log:12477「手牌 4 张：中心距 **1.450** 世界」逐位吻合 ⇒ idx = 2 / 3 ⇒ `t = 2/3` 与 `1`。
  · 弧只在**中间**抬、两端为 0 ⇒ 只有 `count == 2`（idx 0/1 = 两端）时 y 才相等。
  · 复算：`Arc01(1) = 0`（由 log:10387「y 0.1204 == `DefaultBaselineY`」✓ 反证），
    `Arc01(2/3) ≈ 0.886`（`HandLayout.cs:124-131` 那 5 个关键帧做 Hermite）
    ⇒ Δy ≈ 0.0293（log:12477「满弧 2.93 归一化×100」）× 0.886 × hMod(≈0.2) ≈ **5.7e-3 = 容差 `1e-4` 的 ~57 倍**。
- 该改成（判据 = 「同一行、不同格」，弧高是这一行的正常形状）：
  `Mathf.Abs(p1.x - p2.x) > 1e-3f && |p1.y − HandLayout.DefaultBaselineY| < 0.05f && |p2.y − DefaultBaselineY| < 0.05f`
  ⛔ 别再断 `p1.y == p2.y`（铁律 5·c：一个值 ≠ 全部情况）。
- 备注：原文由 `W12_…md:163` 提出、`W14_…md:109-113` 逐字抄进代码，**两位都没跑过**（本会话自检暂停）。

### 红 5：抽卡占 0.3s（log:10423 · 断言在 `BattleScene.cs:788-789`）
- 判定：**β 本会话回归** —— `case` 落到了错误的函数里。
- 证据：`Core/EventTiming.cs:86-133` 的 `DurationOf(EvtKind)` **没有 `Draw` 分支** ⇒ 落到 `default: return 0f`；
  本会话把 `case EvtKind.Draw: return DrawDeal;` 加进了 **`DelayBetween`**（`EventTiming.cs:69`，那个 switch 在 `:40-82`，
  语义是「上一条到这一条的**间隔**」，不是「这条**占**多久」）。三处自述都指向 `DurationOf`：
  `DrawDeal` 的 doc「**抽卡**这一条事件占多久 = **0.3 s**」（`:274-287`）· `SourceOf(Draw)` 仍写 0.3（`:313`）·
  断言原文（`W8_三个三层全无的事件.md:35`）。原版也是「先入队、再 `WaitForSeconds`」
  （`PlayerHand._AddDrawnCardToHand:78-80`）⇒「占」= `DurationOf`。
  ⚠️ 落在 `DelayBetween` 还多一个副作用：`DelayBetween(null, e)` 对非 Attack 恒返回 0（`EventTiming.cs:38`）
  ⇒ 抽卡若是这一段的**第一条**，这 0.3 会被整段跳过（顺序相关）。
- 该改成：把该 case **移进 `DurationOf`**（⛔ 不是两处都留 —— 两处都留会多排 0.3）。
  ⚠️ 两种写法下「下一事件的时刻」相同，差的只是这 0.3 排在抽卡**之前**还是**之后**（移过去 = 抽卡提前 0.3）。

### 红 6：`Master of the Fleet` 到手且付得起（log:133998 · 断言在 `BattleScene.cs:9488`）
- 判定：**β 本会话回归**（夹具前提被本会话的新规则推翻）。
- 证据：本会话 `Battle/BattleDriver.cs:7205` 新增 `DeckRules.RemoveInvalidCards(pruned, …)`（账 `A1866` · 写手 `W10_引擎线.md`），
  移除判据 = `DeckRules.cs:394` 的 `CannotAddToDeck(c)`（原版 `inv != 0 || show != 5`，判据只此一处 `DeckRules.cs:263`）。
  而夹具钉死的那张卡 `UM_Master_of_the_Fleet` 的卡表真值 = `inventoryOptions 10 / showInCollectionOptions 0`
  （`RuleEngine/Resources/cards_engine.json`）⇒ **开局就被摘掉**。
  日志实据：`battle.log:93047 / 97980 / 102590 / 108581 / 112671 / 117930 / 122994 / 128250`（= 8 次 attempt 各一条）
  `[Battle] 我的卡组「自检·带战术卡」按原版摘掉一张 UM_Master_of_the_Fleet（原版判据 MenuDeck/Error/EffectOnlyCard）`
  —— 上一趟 `BattleScene_Run.log` 里这句 **0 条**（那时还没有这段代码）。
  隔离旁证：同族的另两处 `DriveTacticAndCheck` **没红** —— `EC52 Mercurial Host`（inv 0 / show 5）、
  `ASH_Skyborne_Deployment`（inv -1 ⇒ 本条不拦）⇒ 全池只有这一张被摘。
- 该改成：`BattleScene.cs:9448-9458` 那个挑卡循环补 `if (DeckRules.CannotAddToDeck(c)) continue;`
  （与 `Data/DeckBuilder.cs:48` 同一条判据，⛔ 别另写一份）；或把夹具改成钉一张**合法**的战术卡。
  是否还有候选**要现跑**才确定（desc 粗筛：`TAU44 Pulse Onslaught` cost 2「Deal 3 damage」·
  `GOF32 Grot Bomb` cost 1「Deal 3 damage to a random enemy」都是 inv 0 / show 5，但过不过
  `EffectText.PickSide == "enemy"` 判据未验）。

### 红 7：[诊断入口过期]（log:261）
- 按简报**跳过**（五个宿主同一根因，重跑 `工具/gen_diag_runner.py`）。

## 共性
1. **没有一条是「查找 bug」**（本仓那条教训再次成立）：3 条是「改了实现、没回头改别处」（红 1/2 逼出的陈旧 `Advance` 数 · 红 5 的落点函数 · 红 6 的夹具挑卡），3 条是「新写的断言从没执行过」（红 3/4/5）。
2. **三条老断言在新代码下变红**（红 1/2/6）—— 它们在 `BattleScene_Run.log` 里都是 ✓（`:125444` / `:125456` / `:95693`）⇒ 全部由本会话引入，不是老红。
3. **三条新断言首跑即红**（红 3/4/5），而且都是「两处写同一条规则」的变体：断言抄了实现的一个**函数名 / 一个分量 / 一个前提**，抄错了。⇒ 这是「自检暂停 + 断言由写手给原文、没人执行」这个做法的直接代价。
4. 红 5 / 6 形状相同：**新规则只落到一处**（`DurationOf` 漏 case；`RemoveInvalidCards` 落了、夹具没跟上）。

## 分不清的
- 红 4：`p1.y / p2.y` 两个真值**没印**（文案只印 x）。5.7e-3 是**复算**（关键帧取 `HandLayout.cs` 字段默认值 + log 的「满弧 2.93」）。要坐实：让那条 `Check` 把两个 y 也印出来跑一次。
- 红 1/2：只知「tween 没跑完」，`Advance(0.25f)` 那一刻的实际相机偏移量没印（不影响结论）。
- 红 5：两种改法都能变绿；我按三处自述 + 原版「入队之后才等」判「该移 case」——⚠️ **这一步是裁断**，请调度台定。
- 红 6：替换候选过不过 `EffectText.Parse / PickSide / TacticPlayable` **没验**（要跑）。

## 顺手发现
1. **`Loc.T` 缺键告警的文案说重了**：本轮 14 条 `[Loc] 语言表里没有这个词条`（12 条 `Card_Name/*` + 2 条 `Card_Trait/*`），
   但这条路的调用方是 `CardText.TermOr`（`Core/CardText.cs:550-556`）—— 它**会返回英文兜底**（界面不会印键名），
   只有**直接**调 `Loc.T` 的才印键名 ⇒ 那 12 张卡的卡面**没坏**，是告警文案（`Core/Loc.cs:2692-2695`）说了重话。
   实据：log:58204 的调用链 `ToCardData → CardText.Name → TermOr → Loc.T`（英文档下 `nameZh` 被语言闸挡掉才走到这条）。
2. **官方本地化表的 `Card_Name/*` 是按 id 建键**（1065 条：`Card_Name/ASH1`、`Card_Name/5c4d9e78…`），
   里面**既没有** `Card_Name/Master of the Fleet`、**也没有** `Card_Name/UM_Master_of_the_Fleet`
   （`Unity/数据/游戏数据/官方本地化_en+zh-CN.json`）⇒ 这张卡的 id 与名字**两边都对不上官方键**，
   与已记的「`card_ids.json` 的 id 不是游戏卡 id」（`A970`）同源。**只报，不改。**
