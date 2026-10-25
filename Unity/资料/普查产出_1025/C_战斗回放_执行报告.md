# 执行代理 C · 战斗线（回放 / 每帧开销 / 卡面族 / 血账探针 / trait 阵营）

> 日期：2026-10-25 · 角色：执行代理 `C` · 白名单：`Battle/**` · `Core/VfxMap.cs` · `Core/BlobShadow.cs` · `Editor/BattleScene.cs`
> 判据顺序：反编译 `d:/2/tools/decomp_full/` → 解包资源 → 本仓既有判据文件（`RE1` / `R2` / `C5` / `C6` / `AN1` / `S1`）。
> 运行：**typecheck 0/0**（`TMPDIR=/tmp/wf_c1 bash 工具/typecheck.sh`，运行时 + 编辑器两个程序集全 0）；⛔ 没跑 Unity 批处理（红线）。
> 行尾：`BattleDriver.cs` / `BattleScene.cs` **纯 CRLF 未翻**（30296/30296 · 44366/44366）；`VfxMap.cs` / `BlobShadow.cs` / `ReplayStore.cs` **纯 LF 未翻**。

---

## C1 · `RE1` 回放三件

### ① 录像头记「双方实际卡 id」（`RE1` §1.4-A2）—— ✅ 已做

**结论**：录像头新增两列 `hostCardIds` / `clientCardIds`（`CardDef.Id`，含督军卡与防御卡、顺序原样），
值取 `Begin` 里**真正喂给 `RuleCore.NewBattle` 的那两份 `List<CardDef>`**；回放时按 id 展开、走
`Begin(exactMine:, exactFoe:)` **原样重建**，不再在回放时按阵营重算。老录像（两列为空）**逐字退回老路**。

**判据**：原版 `BattleRecordData` **双方各存一份牌组**（`playerData +0x40` / `oppData +0x38`，`DUMP:77673`；
映射 → `资料/普查产出_0927/回放_入口与数据链.md` §C·①）—— 原版从来不是「回放时再算一遍对手的牌」。
本地判据文件：`资料/普查产出_1024c/RE1_战斗回放完整审查.md` §1.4-A2 / §1.5。

**改动清单**

| 文件 | 位置（按符号） | 改动 |
|---|---|---|
| `Battle/ReplayStore.cs` | `ReplayRecord`（`MulliganEnabledForReplay` 之后） | 新增 `public List<string> hostCardIds; clientCardIds;`（`null`/空 = 「没记」哨兵；**`version` 不 +1**，纯新增字段；⛔ 不进 `MsgStart`） |
| `Battle/BattleDriver.cs` | `CardIdsOf(...)`（`RecBegin` 上方，新静态助手） | `List<CardDef>` → id 列表；null/空 ⇒ `null` |
| 同上 | `RecBegin(...)` 形参 + 对象初始化 | 新增 `resolved0/resolved1`；写 `hostCardIds/clientCardIds` |
| 同上 | `Begin` 的 `RecBegin(...)` 调用点 | 传 `resolved0: myCards, resolved1: foeCards`（⛔ 不是 `myDeck/foeDeck`） |
| 同上 | `TryExpandRecordedDecks` / `TryExpandCardIds`（`PlayReplay` 上方） | 录像 id → `CardDef`；**一张都查不到**就出声 + 返回 false（退回老路） |
| 同上 | `PlayReplay` 的 `BeginFromPendingCore(...)` 调用点 | 先展开；有就传 `exactMine/exactFoe` + 日志 |
| 同上 | `BeginFromPendingCore(...)` 签名 / 转调 `Begin` | 新增 `exactMine/exactFoe` 两个形参；转调时 `exactDecksFromReplay: 两者都非空` |
| 同上 | `Begin(...)` 签名 + 「exact 只对教程局生效」那道闸 | 新增 `bool exactDecksFromReplay = false`；**只有它为真时**才允许 `tutorial == null` 走 exact（缺一边 ⇒ 出声 + 退回老路） |
| 同上 | `Begin` 的「教程局原样上」那一支的日志 | 分清「教程关卡」/「录像重建」两种来路（原来只会说教程局） |
| `Editor/BattleScene.cs` | 录像节**末尾**（`15z` 之前）新增一节 | 产品形状用例 + 多重集对账 + 正/反两例（见下） |
| 同上 | 文件底部 `UsedCardIds(...)`（`MakeDeck` 之后） | 某座位所有实例的 `Card.Id` 多重集（Deck/Hand/Discard/Board/Warlord） |

**断言**（`BattleScene.Run` 的录像节末尾，走**产品开局形状**：`Begin(seed:, myDeck:)`，⛔ 不传 factions / foeDeck）：
1. ★★ `rec.hostCardIds` / `clientCardIds` **非空** —— 🧨 只记 `deckJson`（入口参数）⇒ 引擎自动凑的那副一张没有 ⇒ 红。
2. ★ 多重集对账：**录像里记的牌都在引擎实际用的那副里**（`used ⊇ rec`），差集 **≤1 且必须是 `defence`**
   （先手方卡组里那张按原版 `BuildPlayer` 只分流、不入库）。⚠️ 只断「录的都在」，⛔ 不比「两边相等」——
   引擎**造出来的**实例（召唤物/生成卡）本来就不在牌表里，比相等会假红。
3. ★★ 按录像头重建 ⇒ `PlayReplay` 返回 true（指纹相等）+ `ReplayDivergenceForTest < 0`。
4. ★★ **灭自证**：把 `clientCardIds` 删掉一张再放 ⇒ **必须不是同一局**（证明回放真的读这一列）。

> ⚠️ 这一节插在录像节的**最后一刻**（`15z` 之前、`driver != null` 守卫内）：它自己 `Begin` 开新局，
> 不能插在中段 —— 中段那批断言依赖 `driver.Ctx` 是「刚放完 `playRec`」的那一刻。

### ② 列表与文件 —— 部分做（**列表那半边不在白名单**）

| 子项 | 结论 | 证据 / 落点 |
|---|---|---|
| **文件名防覆盖**（`RE1` §3-E3） | ✅ **已做** | `ReplayStore.Save` 改走 `UniqueNameFor`：同名加 `_2`/`_3`…（**且出声**）；`ReplayStore.ClockForTest`（自检钉「现在」用，生产恒 null）。断言：同一秒两局、双方督军名相同 ⇒ 两个不同的名字 + **两个文件都真在盘上** |
| **列表改读盘（含「盘上有、表无」兜底行）** | ⛔ **没做（不在白名单）** | 落点全在 `Shell/BattleLogData.cs` · `Shell/BattleLogTab.cs` · `Shell/MatchLogRow.cs`（黑名单）。`RE1` §3-D4 指的是 `BattleLogTab.RefreshRows` **零调用点**；属于界面线 |
| **量 `R2` 覆盖率** | ✅ **量了（只读）** | `R2_回放表现判据.md` §7 共 **6 条**：①一条一条推 ②`WaitingForBlockingAction` 恒 false ③回放出口 ④四颗回放钮+解冻结点 ⑤终局分叉诊断 ⑥过期注释订正。<br>**现状 = 5/6**：①–④ 由 `W1` 落地（`PlayReplay` 拆 `ApplyReplayActionAt` + `TickReplayPump`、`_replayBar.Setup(true)` + `Begin` 复位、`AdvanceTimeline` 挪到 `HandleReplayBar` 之前、七处交互闸、`WaitingForBlockingAction` 回放恒 false；**§1.1「部队卡牌消失」的根因随之闭合**）；⑥ 已订正（`grep "从不进回放局"` → 只剩`W1` 的订正注释，5 处全部标了「不成立」）。<br>**⑤ 未做**：`RecFinish` 的 `finalBreakdown/finalBrief`（诊断用，`grep` 零命中）；属「诊断不是修」，未计入本代理任务 |
| **`R6` 时点** | ❓ **判据出处查不到** | 在 `资料/**` 里搜 `R6` + `回放|时点|早`：只有任务总表那两行自己（`§一` 第 108/236 行）；`RE1`/`R2`/`R3`/`C5`/`C6`/`AN1` **均无 R6 条目**。⇒ **没查清**：需要主对话给 `R6` 的原始出处（哪份文件哪一节），或确认它是笔误 |

### ③ 无目标战术卡：**我方**的「中央展示卡」—— ✅ 已做

**结论**：`MaybeDisplayPlayedCardInCenter` 原来一句 `if (e.Player == _me) return;` 把我方整条掐掉。
判据 = 原版 `CardScript._PlayPlayerCard.c:36` 与 `_PlayEnemyCard` **是同一个调用**（`cardType != 0` ⇒ `DisplayEnemyCardInCenter`）⇒ 两侧都演。
现在两侧都走（我方 `showFront: true`），卡的**取法**改为 `PlayedDefOf(e)`（先回查 `Ctx.PlayedCards` 拿真正的卡对象，
查不到才按卡名 + **卡自己的阵营**查池）—— 顺带把「跨阵营同名卡认错」这条封住。

**改动**：`Battle/BattleDriver.cs`
- `MaybeDisplayPlayedCardInCenter`：去掉我方早退；`def` 改 `PlayedDefOf(e)`；`showFront: mine`；`creator: mine ? null : PlayedCreatorOf(e)`
  （原版 `DisplayCreatedByText` 对 state 10 只点亮 `isPlayer == 0` 那张）。
- 新增 `PlayedDefOf(BattleEvent)`（`PlayedCreatorOf` 的同款回查，返回 `CardDef`）。
- `DisplayEnemyCardInCenter`：新增 `PlayedCenterDisplaysForTest` / `LastPlayedCenterOwnerForTest` /
  `PlayedCenterDisplaysForSeatForTest(seat)` / `PlayedCenterViewForTest` 四个只读口（卡片本体演完就被 DOTween `Kill` ⇒ 事后读视图恒 null，只能靠计数）。
- `Editor/BattleScene.cs`：在既有 `R1`（`EvtKind.Play` + `Slot=-1`）那一节加 **★★** 断言 ——
  `animateFeel = true` + 我方座位投一条无目标 `Play` 事件 ⇒ **我这一侧的中央展示计数 +1**（按座位计数，避免同趟另一侧一张把「最近一次」挤掉）。
  🧨 把 `if (e.Player == _me) return;` 加回去 ⇒ 红。

**如实标的两处「我们挑的」**（⛔ 不是原版）：
1. **起点仍取「那一侧手牌末位」**（原版搬的是卡本体、不复制；我方手牌每张不同，从末位飞出与那张牌真实位置不符）。
2. **我方那张 `showFront: true`（开场正面、不翻面）**：原版 `_PlayPlayerCard` 传的是 `showFront = 0`（会翻），
   但我们建的是**副本**（本体此刻已离手）⇒ 翻一次会露出卡背，取 `showFront` 那一档。
   两处都要**真机并排**再定（`RE1` §8-1 · `W1` 报告 §三同一条）。

---

## C2 · `T08` 每帧开销（`C5 Q6` A1/A3/A4/A5）

**结论**：A1/A3/A5 **真做了**（A3 补了「一帧只换算一次指针」的缓存）；**A4 只做了「命中就停」那半，
「指针与上一帧相同 ⇒ 0」没做**（要事件驱动重写，见下）。typecheck 0/0。

**判据**：`资料/普查产出_1015战斗判据/T08_每帧开销判据.md` §三 · `资料/普查产出_1024c/C5_引擎战斗剩余判据.md` §Q6。

### 改动

| 文件 | 改动 |
|---|---|
| `Core/BlobShadow.cs` | `Sync()` 记 `_lastTargetPos`；新增 `Tick()`（**位置没变 ⇒ 整帧跳过**）+ `TickForTest()`；`Update` 改调 `Tick()`。计数器 `SyncRecomputeCountForTest / SyncSkipCountForTest` |
| `Battle/BattleDriver.cs` | ① `UpdateHud()` 首行 `UpdateHudCallsForTest++`<br>② `WorldPointer()` 加**帧缓存**（键 = `Time.frameCount` + `cam`；`PointerWorldForTest` 走最前一句、不进缓存）+ `WorldPointerCallsForTest` / `ScreenToWorldCallsForTest`<br>③ `TickTooltipAt` 入口清 `TooltipLastCandidateEvalsForTest`；`TickTraitTip` / `TickCardTip` 各计一次候选评估<br>④ `TickFrameForTest()` = **直接调生产的 `Update()`**（⛔ 不是第二套实现） |
| `Editor/BattleScene.cs` | B5 节内新增 T08 断言块（A5 → A1/A3 → A4；`TickFrameForTest` 前存/还原 `PointerWorldForTest`） |

### 断言（落 `BattleScene.Run`；`TickFrameForTest` 跑**生产那一份** `Update()` 一帧，前后取差）

- **A5**（`BlobShadow`）：`TickForTest()` 两次，第二次**必须 false**（位置没变 ⇒ 不重算）；
  再把**卡根**（`target.parent`）平移一点 ⇒ 必须 **true**。
  🧨 照抄成「只看 `target.hasChanged`」（父节点动不会置子节点的位）⇒ 影子冻住 ⇒ 红。
  ⚠️ **为什么不能照抄原版**：原版 `target` 就是被移动的那个节点；我们 `BlobShadow.Create(go.transform, go.transform)`
  的 `target` 是**卡体子节点**，运动写在**卡根** ⇒ 只能比「上一帧世界位置」。也⛔ 不能去读写卡根的
  `hasChanged`（`AutoCardRotation` 用同一个 flag 做 2 秒计时器并帧末清零，抢 flag ⇒ 旋转收尾静默变形）。
- **A1**：一帧里 `UpdateHud()` **≤1 次**（原版 `BattleManager.Update` 全文 0 次 HUD 刷新）。
- **A3**：一帧里「指针→世界」**只换算 1 次**（`ScreenToWorld` 计数差 ≤1；同帧 `WorldPointer()` 会被调 5～8 次）。
  🧨 去掉缓存 ⇒ 每个调用点各换算一次 ⇒ 红。
- **A4（做了一半）**：指针压在生命数值圈上 ⇒ `TickTooltipAt` 返回 true 且**评估数 < 空处那一趟**
  （= 命中即停，`return true` 链有效）。**⚠️ 原版口径「指针与上一帧相同 ⇒ 0 次评估」没做**：
  原版那是**事件驱动**（`TextTooltipController.Update` 只在 `OnPointerEnter` 置过位后才比一次字节），
  要复刻得把 hover 改成 uGUI 的 enter/exit 事件（`EventSystem` 每帧自己也会 raycast）——
  那是**表现层输入模型重写**，不在本件范围；已在此如实记，⛔ 别把「命中就停」当成那一条。

⚠️ **A1/A3 的覆盖条件**：`TickFrameForTest` 只在「我方回合 / 不在换牌 / 无模态窗」时跑（否则 `DriveAiTurn` 会改局面）；
不满足时**出声说「没验」**（⛔ 不冒充通过）。若 `BattleScene.Run` 日志里出现
`⚠️ T08·A1/A3：这一档跑不了一帧`，说明这条没验到 —— 需要主对话换一个我方回合的时点再跑。

---

## C3 · `C6·Q7`：`_revealCard` 用错族 —— ✅ 已做

**结论**：揭示卡原来走 `CardView.Create` 默认 `CardFace.Full`（= 菜单/HUD 的 `CardUI` 族，立绘中心 `−0.044`）；
原版正主 = `BattleManager.DisplayRevealedCard` → `BattleCardManager.CreateCard`（`cardPrefab` 逐值落到 `CardPrefab`）
⇒ **战斗卡族**、立绘中心 `−0.065`。已改传 `CardFace.Hand`（= 战斗卡族，且正好不印阵营行）。

**判据**：`资料/普查产出_1024c/C6_表现界面轮抽剩余判据.md` §七 · `Core/CardView.cs` 的 `ArtUnitYFight` 那一节。

**改动**：`Battle/BattleDriver.cs` `BeginOffensiveRevealOrApply` 的 `CardView.Create(hudRoot, d, "OffensiveReveal", **CardFace.Hand**)`
（注释里写明「⛔ 别统一改另外两处：`_choosePanel` 的 `Full` / `CenterCard_*` 的 `Hand` 本来就对」）。

**断言**（`Editor/BattleScene.cs`，既有 reveal 起手那一条之后）：
1. ★ `drv.RevealCardForTest.Face == CardFace.Hand`。
2. ★ **立绘网格**中心差：同一份 `CardData` 建一张 `Full` 参照卡，两块 `ArtLayerMeshForTest.bounds.center.y` 之差
   **必须 = −0.021 卡单位**（= `ArtUnitYFight − ArtUnitY`）。⚠️ 断的是**网格顶点**（族差烘在顶点里，`transform.localPosition` 量不到）。
3. 量不到网格时**出声说「没验」**（⛔ 不读成通过）。

---

## C4 · `_pendingHp` 探针（`S1` §2·6 的次候选）—— ✅ 已做（探针 + 断言）

**结论**：「加攻/加血时数值变红」的主因已定案（整卡着色 `_tint`，`W14` 已修）；`S1` §2·6 的**第二候选**
= 「数值真的先掉了」（`ToBoardCardData` 的 `d.health += pend.amount` 那条路若符号/清账出错）。
今天**静态看两侧符号成对**，但**没有证据**说它错 ⇒ 按判据要求补**探针 + 断言**（不改行为）。

**改动**：`Battle/BattleDriver.cs`
- 新增 `PendingHpBySlotCountForTest`（**第二本账** `_pendingHpBySlot` 的**条目总数**；不是键数）。
- 新增 `WF_PENDINGHP_PROBE=1` **探针**（默认关 ⇒ 日志一字不变）：`NotePendingHp` / `CompletePendingHpBeat` /
  `DropPendingHpForSlot`（作废 / 挪格）/ **三处成对清** 各打一行
  `[PENDINGHP] op=… seat=… slot=… id=… amt=… engineHp=… bookA=… bookB=…`。
  一局真 Play 就能看出是「值掉了」（`engineHp` 不变而账面变、显示跟着变）还是「被染红」。
- **清账口一字未改**（三处 `_pendingHp.Clear(); _pendingHpBySlot.Clear();` 仍是成对的，只在后面跟了一行探针）。

**断言**（`BattleScene.Run` 的 B5 节）：
1. ★（前提）引擎真扣了血。
2. ★ **加算**：同一单位同一批挨两下 ⇒ 账 = **两下之和**（🧨 后一下覆盖前一下 ⇒ 红）。
3. ★ 卡面**仍显示旧血**（账没演到之前不许提前掉）—— 🧨 `ToBoardCardData` 不再加回 `_pendingHp` ⇒ 红（**这正是 `S1` 次候选的病形**）。
4. ★ 第二本账**≥2 笔**（🧨 只记一本 ⇒ 红）。
5. ★ 受击拍演完 ⇒ 卡面 = 引擎值；**两本账成对清零**（🧨 只清一本 ⇒ 下一次挨打把陈账一起扣掉 ⇒ 红）。

---

## C5 · trait / 特效阵营取**拥有者**（原版 `cardArmy`）—— ✅ 已做

**结论**：表现层十几处把 `FactionOf(seat)`（= **这一局这个座位**的阵营）当 `faction` 传给
`VfxMap.Resolve/ResolveAllTimed` / `CardAnimClips.Resolve` / `AnimTimesOf`。原版这些表按 **`cardArmy`**（卡自己 SO 的阵营）索引。
整副牌同阵营时两者恒等（零可观测差异）；跨阵营的卡（效果生成的卡 / 教程关卡牌 / 将来混编）落在这个座位上时会取到
**另一个阵营的同名变体**，或撞上 `VfxMap.ResolveCardIdOf` 的「重名钉不住 ⇒ 逐卡特效整档失效」。

**改动**

| 文件 | 改动 |
|---|---|
| `Battle/BattleDriver.cs` | 新增 `VfxFactionOf(seat, cardId, instanceId, slot)`（+ `VfxFactionOfForTest`）：<br>① 事件带实例号 → 那一份的 `Card.Faction`；② 棋盘 (座位,格号) 上那个单位——**带名字守卫**（`EvtKind.Play` 的 Slot 是**目标格**、Player 是施法者，不加守卫会取到目标单位的阵营）；③ 卡池按卡名 + 座位阵营消歧；④ 查不到 ⇒ **退回座位阵营 + 出声一次**（dedup） |
| 同上 | 全部 **10 处** VFX/动画阵营取值改走它：`SkillAnimLagOf` · `PlaySignal`（`hudFaction` / `pFaction` / 主 `faction` / `atkFaction`）· `RangedVfxDurationBefore` · `PlayAbilityBeat` 系 · `PlayDeathAbilityAnims` · `PlayUnitSkillAnim` · `PlayAiDeployLandingVfx` |
| `Core/VfxMap.cs` | `Resolve` / `ResolveAllTimed` / `WarnNoFactionVariant` 的 doc 写明 **`faction` = 卡自己的 `cardArmy`**（⛔ 不是座位阵营）+ 生产侧唯一取值口 = `BattleDriver.VfxFactionOf`；新增只读口 `ResolveCardIdForTest`（自检用）。**逻辑零改动**（本类不替调用方猜） |

**断言**（`Editor/BattleScene.cs`）
1. **VfxMap 层**（既有「`Terminator` 重名靠阵营钉」那条之后）：
   `ResolveCardIdForTest("Terminator", 帝皇之子) == "EC25"` · `…("Terminator", 极限战士) == "UM103"`；
   **负控**：`…("Terminator", "Goff") == null`（证明量的是阵营那一格，不是「名字总能解出一个」）。
2. **驱动层**（B5 节）：把我方场上一格换成一张**跨阵营**的部队卡 ⇒ `VfxFactionOfForTest` 取回的是**卡自己的阵营**
   （≠ 座位阵营）；实例号缺省（旧事件）时按格位取仍是它；**负控**：查不到的卡名 ⇒ 退回座位阵营（⛔ 不拿遍历到的第一张充数）。

⚠️ **`VfxMap` 的「留白/不复刻」注释是判据，一字未动**（⛔ 没有把 `Draw` 塞进「原版无此槽」那张表）。

---

## 需要主对话跑的自检（**串行**，同一工程只能一个 Unity 实例）

| 条目 | 为什么 |
|---|---|
| **`BattleScene.Run`** | **必跑**（主宿主）：C1 的 A2/中央展示断言、C2 的 A1/A3/A4/A5、C3 的族断言、C4 的血账断言、C5 的跨阵营断言全在这一条里 |
| `CardBaseDemo.Run` | `Core/BlobShadow.cs` 的 `Update` 行为变了（每帧闸）、`VfxMap` doc/只读口 —— 四档分辨率那一套是卡面/卡体的既有闸 |
| `MainMenuScene.Run` | `ReplayStore.Save` 的文件名策略变了；这条宿主里挂 `ProfileDataAssertE13` / `ReplayStoreGuardAssertE11`（都在动 ReplayStore） |

> 纯文档/纯注释的改动不单独跑。`RuleEngine/**` 一个字节没碰 ⇒ 不必跑 `RuleEngineTest.Run`。
> ⚠️ `BattleScene.Run` 比原来多一次**整局**（A2 产品形状用例）+ 两三次 `PlayReplay`，预计**多 10～20 秒**。

---

## 没查清 / 没做（如实）

1. 🔴 **`RE1·列表与文件` 的「列表改读盘（含盘上有、表无兜底行）」没做** —— 落点 `Shell/BattleLogData.cs` ·
   `Shell/BattleLogTab.cs` · `Shell/MatchLogRow.cs` 全在**黑名单**（界面线）。
2. ❓ **`R6` 时点**：判据出处**查不到**。已搜：`资料/**` 的 `R6`（只命中任务总表自己两行）、
   `RE1`/`R2`/`R3`/`C5`/`C6`/`AN1`/`S1` 全文、`普查产出_0927/回放_入口与数据链.md`。⇒ 需主对话给原始出处或确认笔误。
3. ⚠️ **T08·A4 的「指针与上一帧相同 ⇒ 0 次评估」没做**（只做了「命中就停」）。理由见 C2 §A4：
   原版是事件驱动，复刻要改 hover 输入模型。
4. ⚠️ **T08·A1/A3 可能没验到**：`TickFrameForTest` 要求「我方回合 / 非换牌 / 无模态窗」。若 `BattleScene.Run` 日志里
   出现 `⚠️ T08·A1/A3：这一档跑不了一帧` ⇒ 这两条**没验**，需要换时点。
5. ⚠️ C1-③ 的两处「我们挑的」需要**真机并排**（起点取手牌末位 / 我方 `showFront`）—— 见 C1 ③ 末。
6. ⚠️ `R2` §7-⑤（`RecFinish` 的 `finalBreakdown/finalBrief` 诊断）**没做**（本代理任务不含；它是「诊断不是修」）。

## 顺手发现（照实报，⛔ 没越界改）

1. 🔴 **`RE1` §6-2 的「过期注释」已由 `W1` 全部订正**（`grep "从不进回放局"` → 5 处都带「不成立」的订正注）⇒ 那条可以销账。
2. ⚠️ **`PlayedDefOf` 依赖 `Ctx.PlayedCards` 回查**（`EvtKind.Play` **不带** `InstanceId`）：
   同名两张连续打出的极端情形会指向后打的那张（对「取哪张卡面」无差别，但如果将来要用它取**实例**，必须给 `Play` 事件补 `InstanceId` —— 引擎线）。
3. ⚠️ **`VfxFactionOf` 对 Ember/Tide（自设计阵营）会走「④ 退回座位阵营 + 出声一次」**：那两套牌由 `StarterCards` 造、
   不在 `CardDatabase.Load()` 的池里 ⇒ `CardDatabase.Find` 查不到。行为等价（整副同阵营），只是日志里会出现
   「取不到自己的阵营」的去重警告；要消掉得给 `VfxFactionOf` 一个「StarterCards 表」的旁路（未做）。
4. ⚠️ `_vfxFactionMissWarned` 是**跨局保留**的去重表（键 `seat|cardId`，规模受卡池限制）。逻辑上无碍，若要「每局清零」可在 `Begin` 加一句（未做）。
5. ⚠️ **`C6` §7-3 的「reveal 卡相机/量纲」那一格仍未核**（原版 reveal 卡由**透视**战场相机画，我们建在 `hudRoot` 由**正交** HUD 画）——
   Q7 只定「族」，屏幕尺寸可能还差一个「该处透视 px/单位 ÷ 108」。C3 只改了族，**没有**动量纲。
