# B8_X · BattleDriver 族（`BattleDriver` / `BattleContext` / `ActionPump` / `PlayerState`）执行报告

> 执行代理 `B8_X` · 2026-10-26 · 批次 8
> 白名单：`CardPresentation/Battle/BattleDriver.cs` · `RuleEngine/Core/BattleContext.cs` · `RuleEngine/Core/ActionPump.cs` · `RuleEngine/Core/PlayerState.cs`
> 账补丁：`资料/完全复刻/账/_parts/B8_X.tsv`（10 行：**7 条升 A** · 2 条 B 补实① · 1 条**转 C**）

---

## 一、结论

| 项 | 数 |
|---|---|
| **真升 A** | **7**（`CardScript.SendToCemetery` · `CardScript.SetAsUnitDestroyed` · `CardScript.TriggerAmbush` · `CardScript.SetAsDeckDrawing` · `CardScript.SetAsHero` · `BattleManager.IsPlayerTurn` · `BattleManager.ResolveActionFromQueue`） |
| 两处一行接线 | **2/2 已落**（`ShowsQuestPoints` 转发 · `ShowSkillPanel` 的 `targetsAny`） |
| B 补实、仍保持 B | 2（`ResolveAction` 的 500 行 switch 分支表还没逐型对账 · `SetAmbush` 的 HUD 能量显隐跳归 HUD 域 + 盖上那一支需真机） |
| **B → C**（明确缺口） | 1（`AlreadyPlayedTroopThisTurn`：原版 per-turn bool，我们只有**本局**累计计数） |
| 改了 `.cs` | 2 个文件（`BattleDriver.cs` **0 行增减**；`PlayerState.cs` 类尾 **+14 行**） |
| 机检 | `typecheck` **0 / 0** · `ruleprobe.sh check` **rc=0**（解析差异 0 · 哨兵 0）· `check_a_vs_diff` 补丁预览 **0 命中** · `verify_annot` **缺注解 0** · `check_ledger_points`（预览）**失败 0**，我这 7 行一条都不在漂移/失败列表里 |

**没达成 ≥10 的原因（如实报，不凑数）**：这四份文件剩下的 B 行**绝大多数是表现层**（`CardView` / `CardFeel` / 动画 / 特效），
`ruleprobe` 结构上读不到（它只编 `RuleEngine/Core`），而真机截图是本代理的红线外的动作（⛔ 不跑 Unity）。
逐类阻断原因见 §五。**宁可 7 条真 A，不写 10 条假 A。**

---

## 二、逐条证据（G8 格式）

```
块 B8_X-1 | CardScript.SendToCemetery() | decomp_full/CardScript__SendToCemetery.c:7-19 + CardScript__GoToCemetery.c:8-27
          | 落点 RuleCore.cs:9909-9935（MinionDeathBookkeepingDeferred）+ 14039-14045（AddToCemetery）+ BattleDriver.cs:24313-24360（PlayDeathFeel）
          | 观测量 = `play DA58` 的 `弃`：P1 (空) → [FIX_E1]；棋盘 P1[3] 由 FIX_E1 → 空；日志「P2 的 FIX_E1 阵亡，进弃牌堆」
          | 差异 = 表达差异（数据半在引擎、演出半在表现层；ephemeral 分支逐句照抄）
          | 还剩 = 无（ephemeral 那 105 张是天赋卡、不是单位，`play` 夹具走不到，判据侧已核）

块 B8_X-2 | CardScript.SetAsUnitDestroyed() | decomp_full/CardScript__SetAsUnitDestroyed.c:19-33
          | 落点 RuleCore.cs:9909-9935 + BattleContext.cs:1764（DyingThisAction）+ BattleDriver.cs:24313-24360
          | 观测量 = 同一读数：`☠ 离场 P1[3] FIX_E1 2/0/0/6 → .` + `🗑 弃牌 P1 +「FIX_E1」×1`
          | 差异 = 原版每卡一份状态枚举（+0x228）+ 一道 LogWarning 守卫；我们由「格子有没有人」+ DyingThisAction 派生，守卫由 refiled 复核承担
          | 还剩 = 无

块 B8_X-3 | CardScript.TriggerAmbush() | decomp_full/CardScript__TriggerAmbush.c:11-18 · 闸 SetAmbush.c:117-128 · 摘 trait SetAmbush.c:74
          | 落点 RuleCore.cs:6207-6268（DeclareAttackCore 伏击支：`:6258` FaceDown=false → `:6260` RemoveAll(Ambush) → `:6267` FireTriggerAt(Ambush)）+ ActionPump.cs:1056（670 名表）
          | 观测量 = `play GSC16` 电池段 `✎ 改动 P0[2] GSC16 5/2/0/5 {ambush1, strike1} → GSC16 7/2/0/3 {strike1}`（7 = 印 5 + 伏击 +2 Attack）
          | 差异 = 闸的表示：原版 `isPlayer == IsPlayerTurn()`，我们 `p == ctx.Active`
          | 还剩 = 无

块 B8_X-4 | CardScript.SetAsDeckDrawing() | decomp_full/CardScript__SetAsDeckDrawing.c:5（全文 7 行）
          | 落点 RuleCore.cs:3042-3110（MoveDeckTopToHand）+ 3128-3142（AddCardToHandWithinLimit）
          | 观测量 = `play DA58`：`回合 2 开始：P1 能量 2，抽 1 张`；`手=[DA58,filler29] 库=29` → 打出后 `手=[filler29] 弃=[DA58] 库=29`（抽到的那张只在一处）
          | 差异 = 原版有一个可观察中间态 16（抽出中仍算「在库里」），我们一步移出 Deck ⇒ 没有那一帧
          | 还剩 = 无（「抽牌动画期间查 IsInDeck」那一档由视图自己持有，不回头查 Zone）

块 B8_X-5 | CardScript.SetAsHero(Vector3 pos, Vector3 newScale, bool isUIHero) | decomp_full/CardScript__SetAsHero.c:5-61
          | 落点 BattleDriver.cs:25302-25330（SyncBoard）+ 25285-25300（格子遍历）
          | 观测量 = `play DA58`：`P0 棋盘 [.][.][.][FIX_A 2/0/0/2][WARLORD 2/0/0/28][FIX_B …][FIX_C …][.][.]` ⇒ 督军在 4 号格（= BoardSpec.WarlordSlot）、且不是刚打出的卡；对照 `play ASH1` 那一局督军就是 ASH1 本身
          | 差异 = 原版有专属英雄摆放函数（pos/scale 入参 + 原始位缓存），我们把督军并进 SyncBoard 一条路
          | 还剩 = 排序层那一跳（`-(isPlayer!=0) & 10`）我们由 renderQueue 那一族承担，未逐值对

块 B8_X-6 | BattleManager.IsPlayerTurn() | decomp_full/BattleManager__IsPlayerTurn.c:10-22 · 消费者 `_NextTurn_d__395:380-383`
          | 落点 RuleCore.cs:5636-5700（CanAttackNow，回合闸 `:5663`）+ BattleContext.cs:846-852（ActivePlayer）
          | 观测量 = `play DA58` 日志 `回合 0 结束，轮到 P2` → `回合 1 开始：P2 能量 2，抽 1 张` → `回合 1 结束，轮到 P1` → `回合 2 开始：P1 能量 2`；电池 `end0=0` / `atkM=0`
          | 差异 = 原版存一位 ObscuredBool（+0x274）再派生 `+0x244/+0x246` 两个闸；我们存座位号、闸在 CanAttackNow
          | 还剩 = 非行动方的**反例**（`play` 电池结构上只会让行动方宣告攻击）

块 B8_X-7 | BattleManager.ResolveActionFromQueue(ref List<BattleAction> queue) | decomp_full/BattleManager__NextActionInQueue.c:11-29（四道取序）+ ResolveActionFromQueue.c:31-44（Resolving 置真 + get_Item(0) + RemoveAt(0)）+ AddAutoActionToQueue.c（分道 0→+0x350 / 1→+0x358 / 2→+0x368 / 3→+0x360）
          | 落点 ActionPump.cs:1436-1460（PumpActionQueues）+ ActionQueue.cs:277-292（TryDequeue）+ ActionQueue.cs:7-28（判据出处）+ ActionPump.cs:75-95（ActionScope）
          | 观测量 = ① 全池 `play check`（池 1128 张 / 主档 1128 行 + 电池 586 行）；② 单卡 `play DA58`：打前 弃=(空) → 打后 弃=[FIX_E1]，日志序 = 入队序
          | 差异 = **队列原语逐条一致**（四道 + 取序 Interrupt→Instant→DelayedInstant→Player + 同道 FIFO），差异只在「分派点在入队 vs 出队」与「每帧一条 vs 一次取空」
          | 还剩 = 无（本行的判据是「出队 + 取道」那一层；`ResolveAction` 的大 switch 属另一行，保持 B）
```

---

## 三、改了哪些文件、哪些行、新增行数

### 3.1 两处一行接线（判据已查清，均**零行数变化**）

| # | 文件 | 行 | 改法 |
|---|---|---|---|
| 1 | `CardPresentation/Battle/BattleDriver.cs` | `:151` / `:152` | `QuestPointsFaction` 注释补一句 + 常量改成**转发** `PlayerState.QuestPointsFaction`（同一个 `"DarkAngels"` 不再各写一份） |
| 1 | 同上 | `:187-190` | `ShowsQuestPoints(string)` 方法体 → `return PlayerState.UsesQuestPointsFor(faction);`（删掉重复判定） |
| 1 | `RuleEngine/Core/PlayerState.cs` | `:330-331` | `UsesQuestPoints()` 方法体改调 `UsesQuestPointsFor(Warlord.Card.Faction)`（判据收在类尾一处） |
| 1 | `RuleEngine/Core/PlayerState.cs` | 类尾 `:334-346` | **新增** `public static bool UsesQuestPointsFor(string faction)` + `public const string QuestPointsFaction = "DarkAngels"`（**+14 行**，全部在类尾 ⇒ 账里 `PlayerState.cs` 的最大引用行 `:332` 不动） |
| 2 | `CardPresentation/Battle/BattleDriver.cs` | `:15070` | `skillPanel.Show(name, ability, targets, 0, null, !CardFaceRules.ShouldShowTargetsAvailable(u.Card, false))` —— ⚠️ 形参语义是**反的**（`targetsAny = true` ⇒ **不拼**后缀），按 `!ShouldShowTargetsAvailable` 传 |

> ⚠️ 三处 `BattleDriver.cs` 的改动**行数 0 增减**（就地改写）。已核：`:152/:187/:189/:15065/:15070` 全部在原行号上；
> `wc -l` 前后一致（32,880）；`b.count(b'\r\n') == b.count(b'\n')`、裸 CR = 0。
> `PlayerState.cs` 是纯 LF（348 行），插入后仍是纯 LF。

### 3.2 引擎文件

- `RuleEngine/Core/BattleContext.cs`：**未改**（读过，本轮没有需要动的落点；`#206` 要加的那一列**故意没加** —— 写点在 `RuleCore.BeginTurnCore` / `PlayCard`，不在白名单，单加一个没人写的字段 = 死状态）。
- `RuleEngine/Core/ActionPump.cs`：**未改**（读透 `PumpActionQueues` / `ActionScope` / `ActionQueue.TryDequeue`，产出是账不是代码）。

### 3.3 账

- 新文件 `资料/完全复刻/账/_parts/B8_X.tsv`：表头 + 10 行，LF，每行 11 字段（生成器里带 assert）。
- 键 = `(原版类, 方法)` 全表唯一（已核：10/10 唯一，合并不会撞 `>1 处命中`）。
- `check_a_vs_diff.py` / `verify_annot.py` / `check_ledger_points.py` 我用**副本**指到「主表 + 本补丁」的预览目录跑：
  **0 强 / 0 弱 · 缺注解 0 · 失败 0**，我这 7 行一条都不在漂移列表里。

---

## 四、G7 门禁读数（含「红是预期」的那一条）

```
TMPDIR=/tmp/wf_b8x bash d:/4/Unity/工具/typecheck.sh      → 运行时 0 / 编辑器 0
bash d:/4/Unity/工具/ruleprobe.sh check                  → rc=0 · 解析差异 0 行 · 哨兵 0 · 全池 1121 行
bash d:/4/Unity/工具/ruleprobe.sh play check             → rc=1
   结算指纹：当次 = 350c16b2761e9a662b1f29ebaf69500e · 基线 = a93ffb8f344517a9ac611ef06de17a3a
   ①-a 主档变 2 张：GOF97 / TL27   （stun / vulnerable 一类正文）
   ①-b 电池变 5 张：ASH16 / GOF67 / GOF97 等（invulnerable / mob / collect）
```

🔴 **`play check` 的红不是本代理造成的**：本轮我只改了
①`BattleDriver` 的两处表现层接线（`ruleprobe` 根本不编 `CardPresentation`）、
②`PlayerState` 类尾一个纯谓词（`string.Equals`，行为等同改前的字面量比较）。
变化的那几张卡全是 stun / vulnerable / invulnerable / collect 一类正文，属**别的写手当轮在改**的域。
⛔ **我没有重钉基线**（铁律：改行为要调度台在同步点统一重钉）。

---

## 五、没查清的部分（按行列，含「为什么到不了 A」）

| 行 | 现状 | 阻断点（具体到哪一段没核 / 哪个读数口没有） |
|---|---|---|
| `BattleManager.ResolveAction` | **B**（本轮补实） | 队列原语那一半已判 A；剩 = `out_cpp2il_dev_ilspy/BattleManager.cs:21939-22450` 那 500 余行 **switch 分支表**没有逐 `actionType` 对「我们哪一处入队口承担它」 |
| `CardScript.SetAmbush` | **B**（本轮补实） | 引擎那一半读数已过（见 B8_X-3 同一条）；剩 ①`AreMinionsWithAmbush ⇒ PlayerManager.ToggleCurrentMana`（`.c:98-104`）是 **HUD 能量数显隐**，本仓 HUD 恒显 ⇒ 归 `战斗界面_HUD` 域；②盖上那一支（`.c:19-67`：卡背 / 隐藏数值 / 3D 体）需**真机截图** |
| `BattleManager.AlreadyPlayedTroopThisTurn` | **B → C（P2）** | 原版按方各一位 **per-turn** bool（`+0x242`/`+0x243`）；我们 `TroopsPlayed[2]` 是**本局**累计（写点 `RuleCore.cs:3963` / `EffectResolver.cs:3807`，全树无清零）⇒ 判不出「本回合」。补法写在账里：`BattleContext` 加 `TroopsPlayedThisTurn[2]`（本批可加），清零/写点在 `RuleCore`（**不在白名单**） |
| `BattleManager.AlreadyPlayedCardThisTurn` | B（未动） | 同族（原版 per-side bool / 我们全局 per-turn 计数）；**对行动方等价**，且两侧都 0 调用点 ⇒ 保持 B 即可 |
| `CardScript.HasDiedSinceLastTurn` | B（未动） | `ctx.DiedLastTurn` **全仓 0 消费者**（写点唯一 = `RuleCore.BeginTurnCore` 的滚动）⇒ 没有可观察下游；原版唯一消费者也是 `FilterMethods.MeetsConditionCriteria` |
| `CardScript.HasDiedThisTurn` | B（未动） | 正向读数做不到：`EC58 Vengeful Surge`（`If a friendly troop died this turn, give them Flank`）在 `play` 夹具里**本回合没有友军阵亡** ⇒ 只拿得到反例（实测部署 EC8/EC5 未获 Flank，与 DiedThisTurn=0 一致） |
| `CardScript.RemoveFromGameCard` | B（未动） | 落点仍是 `BattleContext.cs:1513`（`Removed` 表）= **字段级**；且带 ephemeral 的 105 张全是**天赋卡**（池内 ephemeral 单位 = 0）⇒ `play` 走不到、拿不到「不进墓地」的正向读数 |
| `MinionManager.GetMinionList` / `BattleManager.SetUnitOrderInPlay` | B（未动） | `PlayOrder` 不在口径 §三 的观察量表里，`play` / `ai` / `layers` 三个探针都不打印它 ⇒ **要工具线加一列**（`工具/ruleprobe/` 不在本代理白名单） |
| `MatchData.GenerateRandomNumbers` / `BattleManager.GetRandomInt` | B（未动） | 机制差异（预烧 100 + 按下标取 vs 流式 `Next`）**是全局随机源的活**：改它会翻掉整池结算指纹、且今天没有玩家可见读数口 ⇒ 必须调度台先决定「什么时候换随机源 + 谁重钉基线」 |
| `ActionPump` 的 `CanPlayAbility` / `MeetsCriteria` / `CanTriggerAbility` / `IsRightContext` / `ExecuteNextAction` / `FinishResolvingAction` / `TriggerAbilities` | B（未动） | 分别是：触发前闸无独立读数口 · 池内 `triggerIf == 8` 命中 0 行 · 同上 · 40 档只在「单位死后还能触发」那一瞬可见（`GOF10 Grot Tank` 在电池里**没死**，净差 0，读不到）· 按日志下标重放那条支我们走 `ReplayStore` · 教程指针那半只有 `tutorial run` 的条数汇总 · `TriggerAbilities` 272 行只读了签名 |
| `TriggerRegiment` / `TriggerTeleport` / `TriggerUprising` / `TriggerCodex` | B（未动） | 本轮逐个试了 `AM9`（Regiment）/ `DA32`（Teleport）/ `GSC23`（Uprising）在 `play` 电池里**都不触发**（电池只打「这一张」、没有「第二张入场 / 被传送 / 能量归零」那几档）⇒ 现有探针结构上量不到。**其中 `DA32` 的 Teleport 正文在电池里没生效**这一条我只当「量具走不到」，**没判成缺陷**（判据是 `RuleCore.cs:4132-4138` 的 `ShouldTriggerTeleport` 闸） |

---

## 六、顺手发现（都没改，按规矩报上来）

1. 🔴 **`RuleCore.cs` / `EffectResolver.cs` 正在被别的写手改，行号在飞**：同一段代码 07:26 读到 `CanAttackNow@5631`、07:35 已变 `5636`（`MoveDeckTopToHand` 3037→3042、`MinionDeathBookkeepingDeferred` 9904→9909、`AddToCemetery` 14024→14039）。
   本补丁里所有 `RuleCore.cs` 的行号都按 **07:35 快照**重钉，且括号里带**符号名** ⇒ 同步点那次 `check_ledger_points --fix` 能按名字重定位。
   预览跑 `check_ledger_points` 时 `战斗逻辑驱动.tsv` 已有 **118 条漂移 / 0 失败**（几乎全是这轮并发改动带出来的 +5 / +29 偏移）—— 提醒调度台在同步点统一 `--fix` 一次。
2. `账/战斗逻辑驱动.tsv` 里 `SetAsDeckDrawing` 原落点写 `BattleContext.cs:1990-2010（抽牌链）` —— **现已指向 E1 在 10-26 新加的注释块**（不是抽牌链；抽牌链在 `RuleCore.Draw`）。本补丁那条已改成真落点。
3. `CardScript.SetAmbush` 的 `.c:98-104`（`AreMinionsWithAmbush` 为假才 `ToggleCurrentMana(true)`）说明原版**场上有伏击单位时会隐藏能量数**；本仓 HUD 恒显能量。这属 `战斗界面_HUD` 域，建议那边记一条。
4. `play check` 当前 rc=1（指纹 `350c16b2…` vs 基线 `a93ffb8f…`），变化卡集中在 GOF97 / TL27 / ASH16 / GOF67 —— 调度台重钉基线时请先确认那是哪一批的成果（不是本批）。

---

## 七、给调度台的请求

1. **重钉 `play check` 基线**（本批未动行为；红来自别的写手的 stun / vulnerable / collect 修）。本代理按铁律不碰基线。
2. **工具线**：给 `工具/ruleprobe/` 加两个读数口 —— ① 棋盘/单位一列 **`PlayOrder`**（解 `GetMinionList` / `SetUnitOrderInPlay` 两条 B）② 抽牌/牌库序一行 dump（解 `CreatePlayerDeck` 那条 B，G18 已改完代码、只缺读数）。
3. **`RuleCore.cs` 的写手**：补 `TroopsPlayedThisTurn[2]` 的清零（`BeginTurnCore`）与写点（`PlayCard` 落位那一步），把 `#206` 从 C 结掉。
4. **`战斗界面_HUD` 域的写手**：`SetAmbush` 的「场上有伏击单位时隐藏能量数」那一跳（`.c:98-104`）。
5. **下一轮接 `ResolveAction`**：按 `BattleActionType` 逐值列一张「原版 case → 我们的入队口」表（约 500 行 switch，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleActionType.cs` 是键表）。
