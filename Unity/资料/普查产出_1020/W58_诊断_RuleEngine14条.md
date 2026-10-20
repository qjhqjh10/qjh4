# W58 诊断：`RuleEngineTest.Run` 14 条失败（只读诊断 · 不改代码）

> 诊断代理 **W58-DIAG-RE** · 2026-10-20
> 输入：`d:/4/_tmp_view/ruleengine.log`（6.6 MB，`✗` 共 28 处 = 14 条 × 2，后 14 处是末尾汇总）
> 判据：`d:/2/tools/decomp_full/`、`d:/4/Unity/资料/普查产出_1020/W58_*.md`、`ruleprobe seg` 离线解析
> 红线遵守：未改任何代码 / 未跑 Unity / 未动 git。

## 〇、结论摘要（按严重度）

**14 条里只有 1 组是真实现缺陷（3 条断言，同一根因），而且它在 HEAD（第五十七会话 `FACTION-DOUBLE605`）里就已经存在，不是本批 W58 引入的。**
其余 11 条全部是**测试侧**问题：断言过期（α/γ）或夹具前提不成立（δ）；被点名优先的五条（D8 双响 / 哨戒监听缺失 / Slay Draw / 能量回 2 / 抽牌伤害缺失）**没有一条是引擎实现缺陷**。

| # | 断言（简） | 分类 | 阻塞？ | 改哪儿 |
|---|---|---|---|---|
| 11a/11b/11c | `FACTION-DOUBLE605` 的 T 偏移 = 2/3、2.8、1 | **γ 实现缺陷（HEAD 已有）** | **是**（新功能验收） | `CardPresentation/Core/VfxMap.cs` 钉行/筛能力序号的先后 |
| 3 | 「载荷已生效、场上无需挂目标」日志 | δ 夹具前提不成立 | 否 | 测试夹具（棋盘不是空的） |
| 7/8 | D8 被打者监听 2 次 / `UnitDamaged` 共 2 条 | δ 夹具（没传 `ranged: true` ⇒ 吃了反击） | 否 | `RuleEngineTest.cs:25653` |
| 9/10 | 哨戒日志找不到（-1）、监听器不在哨戒之后 | δ 断言取词写错（`"的 Sentry"` 不是日志行） | 否 | `RuleEngineTest.cs:25812` |
| 6 | `Slay: Draw a card` 灭自证手牌数 2 → 1 | α 断言算错（战术卡打出会离开手牌） | 否 | `RuleEngineTest.cs:24663` 期望改 `hand0 - 1` |
| 5 | 抽 2 张部队 → 回 2 能（当前能量 3） | α 断言过期（`W58-TURN-A` 改的是**上限**） | 否 | `RuleEngineTest.cs:6390` |
| 2 | `EvtKind` 15 种 | α 断言过期（本批新增 `Heal`） | 否 | `RuleEngineTest.cs:15151` 改 16 |
| 4 | 抽牌后 `turn_start` 的 DS8 造成 2 伤 | α 断言过期（`E10` 2026-10-19：单位卡的 at-turn 只在**在场**时跑） | 否 | `RuleEngineTest.cs:23537` 夹具/判据 |
| 1 | Spanner「random bonus」三项随机取一 | α 断言过期（`E10` 改成 SO 的 5 项，无 armour） | 否 | `RuleEngineTest.cs:11928` |
| 12 | AI「打能打死的那只分更高」0 > 0 | γ/α 断言过期（`T06` 起合成卡无评分数据） | 否 | `RuleEngineTest.cs:27051` 或 `SimpleAI.ScoreFromPlayingCard` |

---

## 一、逐条诊断

### 1. `Give a random bonus to a friendly Vehicle` 解成「三项随机取一」——🔴**α 断言过期（HEAD 已红）**

- **失败行**：`d:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:11928`（`TestFormerTerminalGaps`）
- **根因**：实现在 `E10`（2026-10-19 · 第五十五会话）已按**解包资源字段**（第二权威优先于卡面）改成 `GOF12` 的 5 条 `abilityLogic`：
  `+2 melee` / `+2 ranged` / `+2 health` / `+2 melee +1 health` / `+2 ranged +1 health`，**没有 armour**。
  断言仍要求 `Payload.Contains("melee") && Contains("ranged") && Contains("armour")` ⇒ armour 恒假。
- **证据**：
  - `d:/4/Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs:2818-2852`（5 项构造；`2822-2837` 的 E10 订正注释写明「SO 里根本没有 armour 这一项」）。
  - `ruleprobe seg "Give a random bonus to a friendly Vehicle"` 实读：`verb=chooseone randomPick=1`，payload **5 段**，无 `armour`。
  - `git show HEAD:.../EffectText.cs` 已有同一份 5 项 ⇒ **HEAD 里这条断言就是红的**，非本批引入。
- **修法**：改断言（**不改实现**）：接受 5 选 1，检查 `chooseone && RandomPick && Payload.Contains("health")`，删掉 `armour`；文案「三项」改为「五项（E10 · `GOF12` SO）」。

### 2. `EvtKind` 期望 15、实得 16 —— **α 断言过期（本批新增 `Heal`）**

- **失败行**：`RuleEngineTest.cs:15151`（`TestCemeteryEvents`）
- **根因**：本批「治疗表现」线在 `EvtKind` 末尾加了 `Heal`（枚举 16 个）。断言本身就是「事件表护栏」，注释也写着「加新事件时同步改这一条」，只是没改。
- **证据**：`d:/4/Unity/MyGame/Assets/RuleEngine/Core/BattleEvent.cs:241`（`Heal,`）在 `git diff` 中（工作树新增）；`RuleEngineTest.cs:15151`。
- **修法**：改断言：期望 **16**，文案补 `` `Heal`（W58 治疗） ``；同时把 `kinds.Contains("Heal")` 一起验。

### 3. P3-①「载荷已生效、场上无需挂目标」期望 1、实得 0 —— **δ 夹具前提不成立**

- **失败行**：`RuleEngineTest.cs:3959`（`TestW58GainEnergy` ①）
- **根因**：夹具注释写「两边棋盘都空」，**实际不空** —— `NewBattle` 一定把督军摆在 `Board[WarlordSlot]`。
  `Gain 2 Energy` 解析成 `verb=gain · payload=[2 energy] · target=(未写主语⇒己方全体 unit)`，
  `targets.Count >= 1`（督军）⇒ 走的是 `EffectResolver.cs:8064-8068` 的
  `「只含能量 ⇒ 已按玩家自己结算一次」` 分支，**不是** `targets.Count == 0` 的
  `EffectResolver.cs:7939-7944`（`上面那一句已经结算了`）。
- **证据**：
  - `RuleCore.cs:549`：`p.Board[BoardSpec.WarlordSlot] = p.Warlord;`（每局必在）。
  - `EffectResolver.cs:7935-7946`（0 目标分支） vs `8064-8068`（只含能量分支）。
  - `ruleprobe seg "Gain 2 Energy"` ⇒ `verb=gain payload=[2 energy] target=own/unit/Auto`。
  - 日志 `26175`（✓「没有合法目标，空过」=0，其实是**根本没走到那一支**）、`26189`（✗ 期望消息缺失）。
  - 旁证：`TestW58GainEnergy` 前两条（上限 +2、当前不动）都 ✓。
- **修法（改断言/夹具，不改实现）**：两个选项
  1. 想要 7939 那一支：打出前 `pctx.Players[0].Board[BoardSpec.WarlordSlot] = null;`（夹具里可行，很多用例都这么把棋盘清空），再断言 `LogCount("上面那一句已经结算了") == 1`；
  2. 保持真实棋盘：断言 `LogCount("只含能量 ⇒ 已按**玩家自己**结算一次") == 1`（真实路径的日志），并在注释里写明「0 目标分支在真实对局里不可达（督军恒在场）」。
  ⚠️ 顺带记录：审查 `P3-1` 说的「两句日志会同时出现」**不成立** —— `targets.Count == 0` 时会先 `return`，到不了 8067；B2 新加的分支在实践中基本是死支。

### 4. 抽牌之后 `turn_start` 的 `deal 2 damage to all enemies` 没结算（30 → 30）—— **α 断言过期（E10 · 2026-10-19）**

- **失败行**：`RuleEngineTest.cs:23537`（`TestBeginTurn` 相位钉子）
- **根因**：`E10`（第五十五会话）按原版把「回合开始/结束」的**手牌通道**限定为**手牌陷阱**：
  `EffectResolver.cs:6039` `if (!EffectText.IsHandTrap(c)) continue;`，而
  `EffectText.cs:4533-4544` 明确「**单位卡一律不算手牌陷阱**」（注释点名 `DS8 Support Turret`）。
  单位卡的 `At the start of your turn, …` 只在**它站在场上**时由 `ResolveAtTurn` 的棋盘那趟（`EffectResolver.cs:6060+`）结算。
  本测试把 DS8 压在牌库顶、靠「抽进手牌」触发 ⇒ E10 之后不再成立。
- **证据**：`EffectResolver.cs:6026-6039`、`EffectText.cs:4533-4544`（注释第 4536-4541 行列的 11 张里就有 `DS8 Support Turret`）；
  `git show HEAD:.../EffectResolver.cs` 已有 `IsHandTrap(c)` ⇒ **HEAD 已红**，非本批。
- **修法**：把夹具改成「DS8 已**在场**」再开始回合（Place 到本方棋盘），或另找一张**陷阱卡**承载「抽牌后可见」这条相位判据；
  ⛔ 不要放宽 `IsHandTrap`（那会把 11 张单位卡重新误判成手牌陷阱）。

### 5. 抽 2 张部队 → 回 2 能（期望 3、实得 1）—— **α 断言过期（本批 `W58-TURN-A` 改的就是语义）**

- **失败行**：`RuleEngineTest.cs:6390`（`TestBattleKeywords` ④）
- **根因**：本批 `W58-TURN-A` 按原版把 `Gain N Energy` 从「补当前能量」改成「**永久抬上限、当前能量不动**」
  （`EffectResolver.DoEnergy` `:8554-8580`、`DoGive` 能量支 `:7907-7929`）。
  测试仍按旧语义：`energy0 - 1 + 2`（当前能量回 2）。实际：`2 - 1 = 1` 当前，`MaxEnergy 2 → 4`。
- **证据**：
  - `d:/4/Unity/资料/普查产出_1020/W58_回合流程_加时能量手牌上限.md` ③ 节（`ManaBuff.createMana → AddNewMana`，DA61 的 `refillMana=0`）。
  - 同文件 `:136` 说明本批已把**另一条**老断言（`RuleEngineTest.cs:3571` 附近）改成「上限 +1、当前不动」——
    **这一条（6390）漏了**。
  - 日志 `36115`：期望 3、实得 1（正好 = `2 - 1`）。
- **修法**：改断言（不改实现）：
  `Check(Energy, energy0 - 1)` + `Check(MaxEnergy, max0 + 2)`；文案「回 2 能」改「上限 +2、当前不动」。
  ⚠️ 若主对话对 `W58-TURN-A` 的判据有异议，应先在 `DoEnergy` 一处改，**不要**两边各留一套。

### 6. `Slay: Draw a card` 灭自证：期望 2、实得 1 —— **α 断言算错（打出战术卡本身会离开手牌）**

- **失败行**：`RuleEngineTest.cs:24663`（`TestW58BlastStompSlay` ⑥）
- **根因**：`hand0` 在 `PlayTactic` **之前**取；`PlayTactic` 会把打出的那张从手牌移除
  （`EffectResolver.cs:1682` `ps.Hand.RemoveAt(handIdx)`）⇒ 无论 Slay 响不响，手牌都先少 1。
  本用例的 `CountSlaySignals == 0` 已 ✓，说明 Slay **没有**响，实现是对的。
- **证据**：`RuleEngineTest.cs:24646-24665`；`EffectResolver.cs:1640/1682`；日志 `37726`（期望 2、实得 1，同段 `37699` 的「战术卡伤害不算击杀」✓）。
- **修法**：期望改 `hand0 - 1`（并补一条注释：战术卡打出本身 −1）。同组 ⑦/⑧ 用 `DeclareAttack`、不消耗手牌，`hand0` 不变，**不要一起改**。

### 7/8. D8：被打者监听 2 次 / 整场 `UnitDamaged` 2 条 —— **δ 夹具 bug（没传 `ranged: true`）**

- **失败行**：`RuleEngineTest.cs:25677`、`RuleEngineTest.cs:25681`（`TestW58DamagedBroadcastOrderD8`）
- **根因**：夹具用 `Ranged(..., 3, "Long Range")` 造了远程攻击者，但调用
  `RuleCore.DeclareAttack(ctx, 0, 3, 1, 5)` **没有传 `ranged: true`**（形参默认 `false`，
  `RuleCore.cs:5195`）⇒ 这一记按**近战**结算（日志原文就是「`W58D8Shooter 近战攻击 …`」）。
  被打者 `Child of Torment` 有 4 点反击 ⇒ 攻击者挨反击 → **第二次 `damaged` 广播** →
  `Child of Torment` 的 `When a unit receives damage` 再响一次。
  触发器事件的 `InstanceId` 是**监听者自己**（`EffectResolver.cs:7085` `EmitListenerTrigger(ctx, u.Card, owner, u, kind)`），
  所以两次都算在 `victim.InstanceId` 头上 ⇒ 2；旁观者合成卡没有 `UnitDamaged` 动画行 ⇒ 不发 Trigger。
  测试自己的 doc 已经点名「否则…计数断言会被搅成 2（那是夹具 bug，不是 D8）」。
- **证据**：
  - 日志 `39598/39612`，以及同段 `⟪日志⟫` 里的「`Child of Torment 反击 W58D8Shooter：4 攻 → 实际 4 伤`」（反击确实发生）。
  - 预调/排除本身是对的：同段「被打者先于所有其它监听者（10 < 13）」✓；`BroadcastWhen` 的排除在 `EffectResolver.cs:6936`。
  - 对照用例 `TestW58AttackBroadcastOrder`（`RuleEngineTest.cs:25730`）同样没传 `ranged`，但被打者攻击力 0 ⇒ 没反击 ⇒ 全绿，反证根因。
- **修法**：测试调用改 `DeclareAttack(ctx, 0, 3, 1, 5, ranged: true)`。**不要动 `EmitHit` / `BroadcastWhen`。**

### 9/10. 哨戒日志找不到（-1），连带「监听器排在哨戒之后」失败 —— **δ 断言取词写错**

- **失败行**：`RuleEngineTest.cs:25814`、`RuleEngineTest.cs:25817`（`AssertAttackBroadcastAfterSentry`）
- **根因**：`LogIdx(ctx, "的 Sentry")` 找的是 `"<目标名> 的 Sentry"`，但引擎真正打的日志是
  `RuleCore.cs:5483`：`Sentry {se}：{target.Name} 反击了正在攻击它的 …`。
  `"… 的 Sentry"` 只是 `Hurt` 的 `source` 实参，**只有**在盾/闪避/无敌挡下时才会被打进日志（`ApplyDamage`），本夹具不会。
  ⇒ `iSentryLog = -1`，前提条与次序条一起红。
- **证据**：`RuleCore.cs:5482-5483`；日志 `39931`（`-1 / 11`）与 `⟪日志⟫` 里的 `Sentry 5：W58SSentry 反击了正在攻击它的 W58SAtk 5 伤（剩 -3）`；
  该行**排在**「事件「attack」触发…」之前 ⇒ 换成正确取词后次序断言应绿。
- **修法**：改取词：`LogIdx(ctx, "Sentry 5：")` 或 `"反击了正在攻击它的"`。**实现不用改。**

### 11a/11b/11c. 三条 T 偏移全红 —— 🔴 **γ 实现缺陷（`VfxMap` 钉行与能力序号筛选的先后反了，HEAD 已有）**

- **失败行**：`RuleEngineTest.cs:20715`（SOR56 `#2` T=2/3）· `RuleEngineTest.cs:20733`（SOR23 `#1` T=2.8）· `RuleEngineTest.cs:20748`（SOR67 `#2` T=1）
- **根因**：`ResolveAllTimed` 的流程是：
  1. `TryResolveTriggerPicks` 先按 trigger 名+能力序号把候选缩到「这一条能力」的那几行
     （`CardPresentation/Core/VfxMap.cs:1330` → `FilterPicksByAbilityIndex` `:1335-1358`）；
  2. **然后**才做 `pin[i] = FindAnimTimeIndex(times, cursor, picks[i])`，而这里 `cursor` **从 0 重新开始**
     （`VfxMap.cs:854-864`）。
  `CardPick` 只带 `N`/`A`（prefab / CardAnim 名），**不带行号**；同一张卡里 `Faith_trigger_*` 同名重复出现两次
  （SOR56 的 4 条 UseFaith 行、SOR23/SOR67 各 2 条）⇒ 第 2 条能力的候选在第 2 步被**重新钉到第 1 次出现的行**上，
  拿到的是**第 1 条能力的 `Offset`**（全 0/1），于是：
  - SOR56 `#2` 实得 T=0/1（期望 2/3）；
  - SOR23 `#1` 实得 T=0（期望 2.8）；
  - SOR67 `#2` 实得 T=0（期望 1）。
  这也解释了为什么同组 `#1` 全绿（`#1` 的行恰好是第一次出现的行，重钉结果不变）。
- **证据**：
  - `VfxMap.cs:854-864`（pin 的 cursor 从 0 起）vs `:1319-1331`（先 Filter）。
  - `times` 数据（`cards_engine.json`）：SOR56 `[1.0.0 t=0, 1.0.1 t=1, 2.0.0 t=2, 2.0.1 t=3]`；
    SOR23 `[0.0.0 t=0, 0.0.1 t=1, 1.0.0 t=2.8]`；SOR67 `[1.0.0 t=0, 2.0.0 t=1]`。
    按上面流程重钉后拿到的正是 t=0/1、0、0。
  - 日志 `61523/61580/61638`；同组长度断言（`61480`/`61566`/`61609`）与 `#1` 的 T 断言（`61508`/`61595`）全绿 —— 只有「第 2 条能力的绝对偏移」错。
  - `git show HEAD:.../VfxMap.cs` 已有同一份代码（`keepOffset` / `FilterPicksByAbilityIndex` 都在 HEAD 的第五十七会话里）⇒ **不是本批引入**。
- **修法（改实现，1 处）**：让「钉行」发生在「筛能力序号」**之前**并复用同一组 `pin[]`。最小改法：
  - `TryResolveTriggerPicks` 只按 trigger 名合并，**不做** `FilterPicksByAbilityIndex`；
  - `ResolveAllTimed` 先对完整 `merged` 跑一遍 `pin`（cursor 顺序消费），**再**按 `times[pin[i]].Order` 的第一段
    （`CardDef.TryParseAbilityIndexOfOrder`）筛 `evtAbility`，筛完重算 `n`/`onlyOne`。
  或给 `CardPick` 增加一个 `Idx` 字段，由 `FilterPicksByAbilityIndex` 回填、`pin` 直接采用。
  ⚠️ 不要靠「按名字取第一条/最后一条」绕过 —— 同名重复正是本缺陷的土壤。

### 12. AI「打能打死的那只分更高」0 > 0 —— **γ/α 断言过期（`T06` 起合成卡无评分数据）**

- **失败行**：`RuleEngineTest.cs:27051`（`TestAiOriginal` ⑪）
- **根因**：本用例用合成战术 `Tactic("Zap", 1, "Deal 2 damage to an enemy troop")`。
  出牌打分自 `T06/A1797`（2026-10-19 · 第五十五会话）起走**原版逐卡评分表** `AiScoring`：
  `SimpleAI.ScoreFromPlayingCard`（`Data/SimpleAI.cs:500-517`）→ `AiScoring.ScoreFromPlayingNow`
  （`Data/AiScoring.cs:57-62`）：`AiScoringData.Get(card) == null ⇒ return 0f`。
  合成卡不在真值表里 ⇒ 两个目标同分 0。测试测的是 `ScoreOps` 老口径（已不再用于出牌打分）。
- **证据**：`SimpleAI.cs:511-516`、`AiScoring.cs:61-62`；`git status` 显示 `SimpleAI.cs` / `AiScoring.cs`
  **本批未改**、测试函数体也未改 ⇒ **HEAD 起即红**（`T06` 落地时这条夹具没跟着改）。
  同文件 `ScoreAbility`（`SimpleAI.cs:570-590`）**保留了**夹具用的 `ScoreOps` 兜底，出牌这条没有。
- **修法（二选一）**：
  1. **改实现（推荐，风险低）**：`ScoreFromPlayingCard` 在 `AiScoringData.Get(card) == null` 时回落到 `ScoreOps`
     （与 `ScoreAbility` 的夹具兜底同一条口径）。池内 1,127 张都有 `ai_scoring` ⇒ 产品路径零变化；
  2. **改测试**：改用池内**真有 `cardTargetScoringCriteria` 的战术卡**（或给合成卡注入 DTO），保持「目标参与打分」这条判据。
  ⛔ 别把 `AiScoring` 整体退回 `ScoreOps` —— 那是 `T06` 刚按原版换掉的。

---

## 二、复现命令（只读）

```bash
# 第 1 / 5 条的解析形态（能直接看见 payload / countRef）
bash d:/4/Unity/工具/ruleprobe.sh seg "Give a random bonus to a friendly Vehicle"
bash d:/4/Unity/工具/ruleprobe.sh seg "Draw 2 cards. For each troop drawn, gain 1 Energy"
bash d:/4/Unity/工具/ruleprobe.sh seg "Gain 2 Energy"
```

## 三、给主对话的分流建议

1. **先修 11a/11b/11c**（唯一实现缺陷，3 条断言，改动集中在 `VfxMap.ResolveAllTimed` / `TryResolveTriggerPicks`）。
2. **3 / 6 / 7 / 8 / 9 / 10 是测试自身**，可一次改完（都在 `RuleEngineTest.cs`，且都不动引擎）；
   改完这 6 条后，「D8 双响 / 哨戒监听 / Slay Draw」三条优先项即可转绿，**不需要改引擎**。
3. **5 / 8 条之外的两条语义确认**：`W58-TURN-A`（Gain N Energy = 上限）与 `E10`（单位卡 at-turn 只在场）
   是 HEAD/本批已定的判据；如果认可，改断言即可；若不认可，必须回引擎一处改，**别两边各留一套**。
4. 建议顺手把 `W58_审查修复_B2.md` 里 `P3-1` 的表述订正为「该分支只覆盖 0 目标，真实对局不可达」，
   并把本条测试的期望改成实际日志（铁律 5）。
