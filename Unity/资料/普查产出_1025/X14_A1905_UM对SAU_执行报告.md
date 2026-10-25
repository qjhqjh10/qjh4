# X14 · A1905 后半（UM vs SAU 阵营局真跑）执行报告

## 做了什么
- `Editor/BattleScene.cs` 追加一段 **UM vs SAU 阵营局**：`Begin("Ultramarines","Sautekh",20261025, firstSeatOverride:0)`（真局、⛔ 不是合成事件），两刀真攻击 + 一次两连抽就收，**不跑完整对局**（不拖慢宿主）。落点 `BattleScene.cs:42602`，在 `DestroyImmediate(driver)` 之前。
- **最小重构（已按任务允许的口径做）**：A1730 那两处**逐字重复**的「解析出几件就播几件 + 灭自证」抽成 `RunCore` 内局部函数 `CheckAttackVfx(driver, faction, atkCardId, evt, tag)`（`:678`）；落点①(`:8950`) · 落点②(`:22653`) · UM/SAU 局(`:42682`/`:42706`) **三处共用**，⛔ 没有第二份判据。
- 既有两处**行为逐字不变**：仍传 `driver.FoeFaction`、仍只跑近战 ⇒ 两边解析都为空，读数与改动前一致。

## 新增断言（19 条 = 15 直接 + 共用函数 2 条 × 2 次调用）
- 前提：driver 还活着 · 阵营对真是 UM vs SAU · **SAU 卡组取得出来** · 三张真卡在池 · 近战/远程各自「有空格 + 两边视图都在」。
- 近战刀（`Doomstalker`/SAU31）：选择器弹出 · 有「近战」· 引擎码 OK · 共用判据（`NecronGauss_Large` 播了；该卡近战/远程同名 ⇒ 灭自证**按交集跳过**）。
- 远程刀（`Deathmark`/SAU15）：选择器弹出 · 有「远程」· 引擎码 OK · 共用判据（`NecronGauss_Deathmark` 播了；近战解析为空 ⇒ 0 次）。
- 抽卡：这一批**真发了 ≥ 2 条 `EvtKind.Draw`** · UM/SAU 真局**一件抽卡粒子都不播**。
- 两刀都沿用 `_fired.Clear()`（放在 `SimulateCommand` 之前）防假绿。

## SAU 卡组怎么取的
`Begin(foeFaction:"Sautekh", foeDeck:null)` ⇒ `ResolveDeck` 走 `DeckBuilder.StarterDeck(PoolFor(pool,"Sautekh"), "Sautekh", 30, …)`（`BattleDriver.cs:5151` / `:12056`）。前提断言现场核：对手手牌+牌库非空 · `Deck` 里每张 `Card.Faction == "Sautekh"`（别家 0 张）· 卡池里有 SAU。取不到 ⇒ **如实红**，⛔ 不静默跳过、不拿别的阵营冒充。

## 动了哪些既有行
`git diff --numstat` = **365 / 72**（其中 **163 / 6 是本代理接手前就有的未提交改动** ⇒ 本代理净 ≈ +202 / −66：helper +57 · 新段 +145 · 两处落点 −66/+4）。
行尾：改后 **CRLF 46,535 / LF 46,535**（全 CRLF，没翻）。`TMPDIR=/tmp/wf_a1905 bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。

## 🔴 两条口径纠正（铁律 5 / 5·b；台账归主对话，我按规矩没碰 `任务总表.md`）
1. **A1905 原文的判据已作废**：它要「`UM_CardDraw`/`SAU_CardDraw` 播 2 次、两落点 x 不等」，但 2026-10-22 `W64 §B5` 已判「原版抽卡链 **0 粒子**」、`ByFaction[draw]` 两行**已删** ⇒ `VfxMap.Resolve(Draw, 任何阵营)` 恒 null（静态那半已在 A1816 组里断过）。⇒ 真局里改断「真发 2 条 Draw + 一件粒子都不播」，落点数学由 1d·⑦ 的 `HudAnchor01` 纯函数覆盖。**`T05` 的「A1905 后半需真跑」应据此改写/核销。**
2. **「阵营按生产取 `FoeFaction`」是过期口径**：`BattleDriver.cs` 现在攻击支按**攻方卡自己的阵营**取件 —— `string atkFaction = VfxFactionOf(e.Player, e.CardId, e.InstanceId, e.Slot);`（判据 `_ResolveAttackRangedAnim…:146-176` 取攻方 clan）。⇒ 新段按生产传**攻方阵营**；既有两处仍传 `FoeFaction`（等价、未改语义）。

## 若 `BattleScene.Run` 报红，先查这三条
1. **前提链**：先看哪条 `（前提）A1905 …` 红 —— SAU 卡组取不到 / 视图没建出来 / 选择器没弹（`OpenCommand` 在 `_myUnits` 缺件时会早退）。
2. **`_fired` 空但解析非空** ⇒ `StepThrough` 没把时间线推完，或 `CardEffects.Play` 钩子被别段摘掉（§A388 那半）。
3. **远程那刀「期望 vs 实播」错位** ⇒ 多半有人把攻击阵营改回「目标那一方」：那是口径变更（本段按攻方），不是夹具错。

## 没做的
- **没跑 Unity**（归调度台）：本段只做**静态 0 错 + 逐条读码**，真跑读数待 `BattleScene.Run`。
