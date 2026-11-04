# B139_V5 · 破坏广播 `OnSabotageCreated` 的 subject 侧（4 条红 ← 真产品缺陷）

> 工作包 V5 · 2026-11-04 · 诊断输入 = `报告/B139_DIAG_OnSabotageCreated.md`
> 一句话：**破坏（Sabotage）广播的 `subject` 取了施放方督军，原版取的是收牌方（敌方）督军** —— 已按原版修正，
> 池内 `GSC68 Atalan Jackal` 的 701 监听从「静默失效」恢复。

---

## §1 原版判据（五跳，逐句现读）

| # | 出处 | 读到了什么 |
|---|---|---|
| ① | `d:/2/tools/decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:95-98` | `BattleManager__GetHero(mgr, *(createdCardScript + 0x40 /*isPlayer*/))` → `BroadcastSabotageCreated` |
| ② | 卡面 + 数据 | `Create a random Sabotage in the enemy hand`；`cards_truth.json` GSC7 `summon.isForEnemy = 1` ⇒ 破坏卡进**敌方手牌** |
| ③ | `BattleManager__AddNewCardToHand.c:52-57` → `BattleCardManager__CreateCard.c:90` → `CardScript__CardSetup.c:42` | 手牌侧由创牌目的侧 `param_4` 决定，并写成 `isPlayer = param_4` ⇒ `isPlayer` = **收牌方** |
| ④ | `BattleManagerSupport__BroadcastSabotageCreated.c:2-15` | 该督军 = `param_2`；先 `ReactToSabotageCreated(param_2, param_2)` 预调，再棋盘循环排除它 |
| ⑤ | `CardScript__ReactToSabotageCreated.c:5-8` | `OnTrigger(0x2bd = 701, …, actingCard = param_2, targetCard = param_2)` ⇒ **acting = target = 收牌方督军** |

⇒ 原版 `targetCard` = **敌方（收牌方）督军**。对照：隐秘那一支（`_ResolveCreateDeckCard_d__513__MoveNext.c:113-119`）插进
**自己的牌库** ⇒ 那一支主体才是施放方 —— 两条路**不是同一个答案**，这正是此前记错的那一处。

**缺陷后果**（现读）：`TargetCriteriaEval.cs:222-237` 同侧查 `friendlyUnits`；`GSC68` 的 701 criteria 是
`{enemyUnits:1, friendlyUnits:0}` ⇒ 同侧 ⇒ 0 ⇒ 闸判死 ⇒ 池内**唯一**监听 701 的卡静默失效
（原版给它留了真动画行 `Atk_BulletImpact_autogun_2shot` 0.75s）。

---

## §2 改了什么（4 处代码 + 3 处文案订正 + 1 处夹具解耦 + 1 个新断言文件）

### 2·1 `RuleEngine/Core/EffectResolver.cs`（**10 行改 / 10 行删 = 行数 21,939 守恒**）

| 行 | 改法 |
|---|---|
| `:18471` | `BroadcastCreatedCardWithWarlordPreCall(..., string by, int subjectSide = -1)` —— 新增「**主体所在那一侧**」形参，缺省 `-1` = 同 `owner`（隐秘两支逐字不变） |
| `:18474` | `int side = (subjectSide >= 0 && subjectSide <= 1) ? subjectSide : owner; var ps = (side …) ? ctx.Players[side] : null;` —— 两条语句**同一物理行**（本文件下游有账的 `文件:行号` 落点，⛔ 不许加行，铁律 18·b） |
| `:18492` | `PreCallBoardListener(..., targetWho: side)`（原传 `owner`）—— 预调那一跳的宾语归属 = 主体自己那一侧 |
| `:18494` | `BroadcastWhen(kind, who: owner, …, subject/actor/target: warlord_of(side))` —— **`who` 仍是 `owner`**（`When you …` 归属 / 极性匹配） |
| `:4301` | **生产调用点**：`…(ctx, WhenEventKind.CreatesSabotage, owner, c, by, **who**)` —— `who` 在 `:4269` = `op.Dest == "hand" ? owner : 1 - owner`，即**收牌方** |
| `:4298` `:7823` `:18451/18457` `:18480` `:18492/18494` | **铁律 5 就地订正**：原文案写「预调主体 = **造牌那一方**的督军」是错的 ⇒ 改成「造出来那张卡所在那一侧」，并写明破坏 = 收牌方、隐秘 = 施放方（各带「⚠️ 2026-11-04 更正（`B139_V5`）」） |

⛔ **没有动的两处**（诊断 §2·3 明确禁止）：
- `EffectResolver.cs:7823` 的 `case`（`CreatesSabotage` 仍在 ability 级 `targetCriteria` 闸白名单里）—— 原版那条 `&&` 链对所有 trigger 都跑。
- 任何夹具的 criteria（没有为了变绿把 `E6Criteria(1,0)` 换成 `(0,1)`）。

### 2·2 `CardPresentation/Editor/B108_A1_TriggerEmit_Assert.cs`（+13 / −5）

`B108A1ListenerBeat(...)` 新增 `int subjectSide = -1`，把「**`subject` 放哪一侧**」与「**`who` 是谁**」解耦
（`subjectOn = subjectSide >= 0 ? subjectSide : (eventWho == 0 ? 0 : 1)`，三处 `Place` 统一用它）；
701 那一族改传 `subjectSide: 1`（= 施放方 0 的敌方）；其余五族逐字不变。
末尾 `CheckB108A1TriggerEmits()` 多一行 `CheckB139V5SabotageSubject();`（与 701 那个族同宿，⛔ 不改 `RuleEngineTest.cs`）。

### 2·3 新建 `RuleEngine/Editor/B139_V5_Assert.cs`（+ `.meta`）

`CheckB139V5SabotageSubject()` —— 真生产链（`PlayTactic` + 真卡池 `CardDatabase.Load()`）两条断言组，见 §3。

---

## §3 断言与灭自证（新增 **11 条**）

**A 组（正例 · criteria = `GSC68` 的形状 `enemyUnits:1 / friendlyUnits:0`）**

- A①（前提）双方督军都在场 —— 否则会走「只跑棋盘那一跳」的退回分支，断言空转。
- A②/A③（前提）**敌方棋盘上只有督军**一个单位、且就是 `ctx.Players[1].Warlord` ⇒ 「在敌方侧」≡「是敌方督军」。
- A④ 打出 `Create a random Sabotage in the enemy hand` 的战术卡。
- A⑤（前提）破坏卡真的进了**敌方**手牌（手牌 +1）。
- **A⑥ `OnSabotageCreated` 恰好 1 条 `EvtKind.Trigger`** —— 监听者过了阵营闸。
- A⑦ 同一件事的日志口径（`WhenFired` = 1）。

**B 组（灭自证 · 同一份夹具，criteria 翻成 `friendlyUnits:1 / enemyUnits:0`）**

- **B④ 必须 0 条**。候选（`subject`）只可能在**一侧** ⇒ **A 与 B 结构上不可能同时满足**：
  把 `subjectSide` 改回 `owner` ⇒ A 变 0 条、B 变 1 条 ⇒ **两条同时红**。
  ⛔ 这不是「再跑一遍同方向」，它挡的正是「subject 又传回施放方」那种改法。

铁律：前提三条写成**硬检查**（`CheckTrue(前提)` + `Check(结论)`），⛔ 不用 `if (前提) Check(...)`
（那会在前提塌掉时静默跳过 —— CLAUDE.md 已把这条列为踩过的坑）。

---

## §4 `play check` / `RuleEngineTest.Run` —— ⛔ 本代理没跑，请调度台跑

### 4·1 `play check`：**预测不变**（rc=0，`PinnedPlayMd5` 指纹相同）

逐条理由（现读）：
1. `play all dump` 的夹具里，两侧督军是**合成的** `FIXWarlord_*`（`PlayProbe.cs:301/333/1788`，正文为空 ⇒ 没有 `WhenTriggers`）⇒ 预调/棋盘跳都叫不醒任何监听者。
2. 棋盘上除被测卡外只有夹具扣血用的普通单位（无正文）⇒ 没有别的监听者。
3. **能造破坏的 4 张卡**（`Cult Propaganda` / `Improvised Barricade` / `Jammed Communications` / `Poisoned Supplies`）打出去时，701 的唯一真监听卡是 **`GSC68`**，而 `play all` 是**每次只测一张卡** ⇒ 两者不会同场。
4. 探针那条 `preset event=createssabotage` 是**直调** `RuleCore.BroadcastWhen(..., subject: null)`（`PlayProbe.cs:1465-1472`）⇒ 根本不过我改的这个入口。
5. 我**没有新增任何 `ctx.Emit`**（`:4303` 那条 `EvtKind.SabotageCreated` 是 `B102_506` 早就有的）⇒ 事件条数不变。

⇒ 若调度台跑出来**变了**，那是上面某条前提被推翻（最可能是夹具里出现了真督军或 `GSC68` 与造破坏卡同场），届时看差异落在哪几张卡即可。

### 4·2 `RuleEngineTest.Run`：**预测 4 条红全绿**，且总数 **+11**

- 原 2 条唯一失败（`OnSabotageCreated` 恰好一条 / `CardId = 监听者`）应转绿（夹具那一侧已与生产形状同形）。
- 断言合计比本轮 14,990 **多 11 条**（A①–A⑦ + B①–B④）⇒ 期望 `RE === 结束：15001/15001 通过，0 条失败`（数字按实跑为准）。

---

## §5 账

- 补丁：`资料/完全复刻/账/_parts/B139_V5.tsv`（11 列 · 2 行 · **都是更新已有行**，`--dry` 报「更新 2 · 新增 0」）
  ⚠️ **合并命令要用 `--only B139_V5`（⛔ 不要写成 `--only B139_V5_`）** —— 文件名是 `B139_V5.tsv`，
  带下划线后缀时 `startswith` 匹配不到，脚本会**静默报「没有补丁」**：
  `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B139_V5`（先 dry）→ 去掉 `--dry` 真合并。
  - `战斗逻辑驱动 / BattleManagerSupport / BroadcastSabotageCreated(BattleManager manager, CardScript unitCollected)` —— 判据出处、落点、差异、轮次全部订正；
  - `战斗逻辑驱动 / BattleManager / BroadcastSabotageCreated(CardScript unitCollected)` —— 同族另一行（纯转调），把「预调被造者」那句错文案同批订正（铁律 6：两份说法别打架）。
- 沙箱验证（**没有**动主表）：把 20 份主表拷到 `/tmp/b139ledger/`，在那里跑 `--only B139_V5` 真合并
  ⇒ `check_ledger_points` = **A 1156 条 · 通过 1156 · 漂移 0 · 失败 0**（告警 375→376，多出的那条是**次要落点**候选 `SECOND`，⛔ 不算失败）。
- 主表闸（现行）：`check_ledger_points.py --all-A` ⇒ **A 5970 条 · 通过 5970 · 漂移 0 · 失败 0**。

---

## §6 纪律核对

- `typecheck.sh`（`TMPDIR=/tmp/wf_v5`）：**运行时 0 / 编辑器 0**。
- `EffectResolver.cs`：**21,939 行 = HEAD 同行数**（`git diff --numstat` = 10/10），行尾全 CRLF、裸 CR = 0。
- `B108_A1_TriggerEmit_Assert.cs`：13/5（**故意加行** —— 该文件在账里**没有任何 `文件:行号` 落点**，已 grep 全部 20 份主表确认）。
- 新成员追加：`B139_V5_Assert.cs` 是**新文件**；`EffectResolver.cs` 的改动**全部落在既有行上**，没有插行。
- 没有碰：`RuleEngine/Data/**`、`TargetCriteriaEval.cs`（**不需要改** —— 闸本身是对的，错的是喂给它的 `subject`）、
  `C3_Assert` / `B110_M2F_Assert` / `B104_P26_Assert` / `B116*` / `B117_F4*`、`RuleEngineTest.cs`、活状态表、正本、git。

---

## §7 顺手发现 / 边界（⛔ 本代理未改）

1. **同一类「主体侧」错误在隐秘那一支不成立**（已核对）：`_ResolveCreateDeckCard_d__513:113-119` 插的是**自己牌库** ⇒ 主体 = 施放方，缺省 `-1` 语义正确，⛔ 别一起改。
2. `cards_engine.json` 的 `abilityLevelCriteria` 里 **701 只有 1 行**（`GSC68`）⇒ 生产影响面 = 1 张卡；但不排除**别的 trigger 族**存在同类「subject 取错侧」的缺陷 —— 建议按 `AbilityLevelGateTarget` 返回 `subject` 的那七族**逐族核一次 targetCard 是哪一侧**（本代理白名单外，未扫）。
3. `PreCallBoardListener` 的 `targetWho` 形参在其它调用点**只传过 `owner` 或 `-1`（= 「判不了」哨兵）**
   （现读：`EffectResolver.cs:11288` / `:13243` / `:13258` / `:13327` / `RuleCore.cs:1657` / `:4242`）——
   本笔是**第一次**传「另一个真实座位」。⇒ 那些族的 `targetWho` 是否也该跟主体侧走，属于同一笔待核。
