# B141 · W48 · `B141-ENG11B-LISTENER410` —— 监听者路补 410：**槽位已裁定，但接线是空操作（未落）**

> 执行写手 `W48` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 白名单：`RuleEngine/Core/EffectResolver.cs` · 新建 `RuleEngine/Editor/B141_W48_Assert.cs` · 本文件。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账主表 / 活状态表 / 所有权表。
> 前置：`报告/B141_W128_ExtraTriggerName.md`（W39 §六·1）· `报告/B141_W128_ENG11.md`（W36）。

---

## 〇 一句话结论

**施工单的两槽裁定做完了（`actingCard` = 监听者本卡 · `targetCard` = 监听者那条 ability 收到的 `targetCard`），但那行代码【没有落】—— 回源核出它今天跑不出任何效果，只会在日志里喊一句「本行不结算」，还给日后留一个双发地雷。**

净生产代码改动：**0 行**（`EffectResolver.cs` 22095 → 22095，逐字节回到改前）。
本件的产出 = **槽位判据** ＋ **「为什么不能接」的四条反证** ＋ **一份钉住这条判断链的断言文件**。

---

## 一 现读判据（全部本轮现读，⛔ 不是照抄简报）

| 项 | 现读值 | 出处 |
| --- | --- | --- |
| 监听者路的 410 缺口位置 | `if (!ctx.IsOver) { string sched = …; if (sched != null && sched == TriggerNameOfWhenKind(kind, selfOnly)) EmitFollowUpResolution(…); }` | `RuleEngine/Core/EffectResolver.cs:10657-10666` |
| 410 的正文入口 | `ops = TriggerOpsByNumber(triggerNumber)` ⇒ 空则 `FireTriggerRowsByChosenAbility` | `EffectResolver.cs:21067` / `:21078` |
| `ca` → 支别 | `320 ⇒ FollowUp` · `325/340/380/480 ⇒ OwnLane` · **其余（含 30/40）⇒ BodyOp** | `EffectResolver.cs:21368-21374` |
| BodyOp 的处置 | `default:` 分支**只有 `ca == 50` 有执行器**，其余走 `NoteTriggerRowKindGap` = **只喊不跑** | `EffectResolver.cs:21396-21407` |
| `ASH81` 的 410 行 | `ab1/lg0 ca 40` · `ab1/lg1 ca 40` | `cards_engine.json` ＋ `ruleprobe triggerops ASH81` |
| `GSC14` 的 410 行 | `ab1/lg0 ca 30` | 同上 |
| 两张卡的监听者行 | `ASH81` = 455/`ca 320`；`GSC14` = 10/`ca 320` | 同上 |

---

## 二 槽位裁定（施工单点名要的那一格）

### 判据链（原版 `.c`，逐跳）

| 跳 | 文件 | 现读 |
| --- | --- | --- |
| ① | `AbilityLogic__PlayAbility.c:2474` | `BattleManager__AddFollowAfterResolution(manager, abilityData+0x10, abilityData+0x20, priority, false)` |
| ② | `AbilityData__SetNewData.c` | 偏移表：`+0x10 = thisCard` · `+0x18 = actingCard` · `+0x20 = targetCard` |
| ③ | `BattleManager__AddFollowAfterResolution.c:26` | `BattleAction(type = 0x3a, …, actingCard = param_2, …)`，动作里存两份卡引用（`uStack_200 = param_2` / `local_1f8[0] = param_3`） |
| ④ | `BattleManager__ResolveFollowAfterResolution.c:379-380` | `CardScript__FollowUpResolutionPlayed(lStack_200 /* = param_2 */, local_f8, 0)` |
| ⑤ | `CardScript__FollowUpResolutionPlayed.c:5-9` | `RawCardScript__OnTrigger(this+0x28, 0x19a = 410, manager, **thisCard = 本卡**, **actingCard = 本卡**, **targetCard = param_2**, 0)` |

⇒ **结论**：

- **`actingCard` = 监听者本卡**（就是我们的 `u`）。**不是**关键词路那套 `chosen ?? subject`
  （`RuleCore.cs:13035` 那一处是关键词路的形状，⛔ 别照抄）。
- **`targetCard` = 监听者那条 ability 收到的 `targetCard`**（= `AbilityData.targetCard`）。
  按两条广播器分别落地：

| 卡 | 事件 | 原版 `targetCard` | 判据 |
| --- | --- | --- | --- |
| `ASH81` | 455 `OtherCardSpiritStone` | **恒 `null`** | `CardScript__TriggerOtherCardSpiritStone.c:21`：`OnTrigger(0x1c7, …, this, **default, default**)` |
| `GSC14` | 10 `OtherCardPlayed` | 被打出那张牌的 `targetCard` | `RawCardScript__OnCardPlayedWithTarget.c:96`：`TriggerAbilities(this, 10, mgr, thisCard = param_3, actingCard = param_4, targetCard = param_5)`，`param_5` 来自 `CardScript__ResolveCardPlayed` ← `BattleManagerSupport__BroadcastCardPlayed` 的第 3 实参 |

若真要接线，形状就是：

```csharp
FireTriggerByNumber(ctx, u, 410, owner, fuSlot, actingCard: u, targetCard: target, triggerEvent: fuEvt);
```

（`target` = `FireBoardListenerDeferred` 的事件宾语 —— 我们的等价物。⚠️ 见 §六·2 的边界：`Play` 广播两处调用点都传 `null`。）

---

## 三 🔴 为什么**没有**落这一行（四条反证，逐条可复核）

### A 410 的正文取不到（`TriggerOpsByNumber(410)` 恒 null）

`FireTriggerByNumberWithSink`（`EffectResolver.cs:21078`）第一句取的就是 `u.Card.TriggerOpsByNumber(410)`。
`TriggerOpsByNumber`（`CardDefPartial_B96CA.cs:102-116`）是**两级口径**：① 数据表 `_triggerOpsByNumber`
（`null` = 没挂表 ⇒ 走 ②）；② 退回 `TriggerOps(trigger 名小写)` = `TriggerOps("followupresolution")`。

现读探针（**走真实装载链 `CardDatabase.Parse`**，不是手读 JSON）：

```text
=== GSC14 ===  HasTriggerOpsByNumber = False 🔴（未挂表 ⇒ 会落进「卡面关键词」退回档）
               TriggerOps("mob") = null（0 条）
=== ASH81 ===  HasTriggerOpsByNumber = False 🔴   同样
```

而 `TriggerOps("followupresolution")` 也不是这两张卡上的关键词 ⇒ **410 的正文那一格是空的**。

### B 就算往下走，`ca` 分派也**不执行**这两行

`FireTriggerRowsByChosenAbility`（`:21385`）的 `switch (TriggerRowKindOf(r.Ca))`：
`320 ⇒ RunTriggerRowFollowUp` · `OwnLane/Unknown ⇒ continue` · **`default`（BodyOp）里只有 `ca == 50` 有执行器**，
其余一律 `NoteTriggerRowKindGap(...)`（`:21405`）—— **只打一条 `Debug.LogWarning`，`fired` 不加**。

两张卡的 410 行是 `ca 40` / `ca 30` ⇒ 全部落进 `default` ⇒ **`FireTriggerByNumber(410)` 会返回 `false`、一行效果都不执行**。

### C 这两张卡的**效果今天已经在跑**（跑在监听者的正文路）

`FireBoardWhenListener` 取的是 `u.Card.FireWhen(kind, …)`（`CardDef.cs:5933`），而 `WhenTrigger.Ops`
来自 `CollectWhenTriggers → TryParseWhenSentence → EffectText.Parse(正文)`。现读 `seg` 探针（两条正文都解析得出真 op）：

```text
SEG = [give +1 Attack and +1 Health to your troops]
  op: verb=give  payload=[+1 attack and +1 health]  target=[your troops]
SEG = [gain Sniper and +1 Ranged]
  op: verb=gain  payload=[sniper and +1 ranged]
```

⇒ 「410 没派」这件事**不等于「效果没跑」**：我们的等价实现就是这条正文路（与 W36 给另外 7 张卡的结论同一形状）。

### D 现在就接 = **给以后埋一个双发地雷**

监听者路**没有** `AbilityLayerOwnsRow` 那种「正文已覆盖 ⇒ 本层让路」的闸（那张表只在战术/防御路
`EffectResolver.cs:2917` 用过一次）。一旦将来有人给 `FireTriggerRowsByChosenAbility` 的 `default` 补上
BodyOp 执行器，410 就会**真跑**，而监听者正文路**仍在跑同一条效果** ⇒ `GSC14` 的 `+1/+1`、`ASH81` 的
`Sniper + +1 Ranged` **各打两遍**，且**不会报错**。

---

## 四 我实际做了什么（含一次「按令改完又回退」的完整记录）

1. **按施工单改了**：在 `EffectResolver.cs:10662-10665` 把 `EmitFollowUpResolution` 那一跳改成
   `{ int fuSlot = …; var fuEvt = EmitFollowUpResolution(…); FireTriggerByNumber(ctx, u, 410, owner, fuSlot, actingCard: u, targetCard: target, triggerEvent: fuEvt); }`
   —— 第一次改**少了一行**（把原 4 行写成 3 行）⇒ `check_ledger_points` 立刻把 `EffectResolver.cs` 的
   **30+ 条落点整体报 −1 漂移**（实测：`战斗逻辑驱动` 漂移 13→93）。补回一行后漂移归零（这也是
   「本文件按行号被大量引用 ⇒ 必须行数守恒」的一次现场实证）。
2. **回源核到 §三 A–D** 之后**整段回退**到原文（4 行 → 4 行，**22095 行不变**），并复核：
   - `grep -n "B141-W48" EffectResolver.cs` = **0 命中**（我那一行没留残迹）；
   - `check_ledger_points`：**`EffectResolver` 相关诊断 = 0 条**（与改前同）。
3. 新建 `RuleEngine/Editor/B141_W48_Assert.cs`（**未接线**；`RuleEngineTest.cs` 不在本批白名单）。
4. 新建本报告。
5. （清理）`ruleprobe scan` 一次误用产生了 `d:/4/Unity/Acolyte Leader`（把参数当输出路径）—— **已删除**，无其它残留。

---

## 五 验证读数（本机实测 2026-11-04 19:47–20:11）

| 命令 | rc | 读数 |
| --- | --- | --- |
| `TMPDIR=/tmp/wf_b141_w48 bash 工具/typecheck.sh` | **0** | `运行时错误数: 0` · `编辑器错误数: 0`（含新增的 `B141_W48_Assert.cs`） |
| `bash 工具/ruleprobe.sh check` | **0** | `=== 全池 1134 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 ===` · `哨兵合计：失败 0 条` · 输出指纹 **`77a3c2bc…`**（钉住值） |
| `bash 工具/ruleprobe.sh play check` | **0** | ①-a 主档逐卡全等 · ①-b 电池逐卡全等 · ② 哨兵基线 **`a0e9c63d…`**（钉住值） |
| `bash 工具/ruleprobe.sh play all dump`（改前 / 改后各一次） | 0 / 0 | **`diff` = 0 行**（`play all dump` 逐字节相同） |
| `python -I -X utf8 工具/check_ledger_points.py` | — | **`EffectResolver.cs` 相关：0 条**（漂移/失败/告警全 0） |

⚠️ **`play all dump` 逐字节相同是预期的**（与 W36 §六·1 的边界同一条）：探针的电池覆盖
`unit/tactic/defence/enchantment` 的「打出去那一拍」，**不含**「对手打出战术 / 用灵魂石能力」这两个事件
⇒ 这两张卡的监听者路在离线探针里**结构上到不了**。⇒ 本件**不能**靠探针判「接上有没有用」，
必须靠 §三 的调用链穷举（那也是本次判「空操作」的唯一依据，**没有跑宿主**）。

---

## 六 断言文件（`RuleEngine/Editor/B141_W48_Assert.cs`，**未接线**）

只钉「这条判断链的每一格」，⛔ 不钉「已修」：

- **A 行形状**：`ASH81` 455=`ca 320` / 410=`ca 40`；`GSC14` 10=`ca 320` / 410=`ca 30`。
- **B `TriggerRowKindOf`**：`40/30 ⇒ BodyOp` · `320 ⇒ FollowUp` · `50 ⇒ BodyOp`（唯一带执行器的 BodyOp）。
- **C `TriggerOpsByNumber(410)`**：两张卡都为空，且 `HasTriggerOpsByNumber = false`（退回档，不是数据）。
- **D `AbilityLayerOwnsRow`**：两卡 410 行**全为 false**（B6「真效果种类 ⇒ 卡面正文负责」）⇒ 接执行器前必须先补让路闸。
- **E `FollowUpSchedulerOf`**：`GSC14 = OtherCardPlayed` · `ASH81 = OtherCardSpiritStone`（监听者路命中的名字判据）。
- **🧨 灭自证**：给 `FireTriggerRowsByChosenAbility` 的 `default` 补 BodyOp 执行器 ⇒ **B 组红**（逼回读 §三·D 的双发风险）；
  把 `TriggerOpsByNumber` 的退回档改成「返回空表 = 有正文」⇒ **C 组红**。

接线（**调度台做**）：`RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加
`Guard("B141W48_Assert", B141_W48_Assert.Run); _pass += B141_W48_Assert.Pass; _fail += B141_W48_Assert.Fail;`

---

## 七 还差什么 / 给调度台的处置建议

1. 🔴 **ENG-11b 的正确定性**：这两张卡的**效果今天已经跑到**（监听者正文路），缺的是**结构**（410 那一跳没有消费者）。
   ⇒ 建议把 ENG-11b 从「补一行 `FireTriggerByNumber(410)`」改写成：
   **「监听者路的 410 需要两半：(a) 让路闸（正文层对 `ca 320` 载体让路）＋ (b) BodyOp 执行器（`ca 30/40` 的通用执行器）**；
   **只做 (b) = 双发，只做 (a) = 效果消失，两半必须同批落。**」
2. **本件不要核销成「410 已派」** —— 生产代码 0 行，且 `play all dump` 逐字节不变。
3. 若要临时「出声」（铁律：不许静默），**不要**用 `FireTriggerByNumber` 那条路（它喊的话是
   「本行不结算」，会让下一个人以为这两张卡是坏的）；更合适的是在 §七·1 的施工单里写清「文本层已覆盖、结构缺口在 BodyOp 执行器」。
4. `B141_W36_Assert` 的 **C6/C7**（`othercardplayed` / `othercardspiritstone` 必须为 null）已被 **W39** 补上映射
   ⇒ 按 W39 报告 §六·2 处理（改成「必须非 null」或删掉）；与本件无关，仅作提醒。

---

## 八 ❓ 没查清 / 边界（如实标着）

1. **`GSC14` 的 `targetCard` 我们拿不到**：原版 = 被打出那张战术的 `targetCard`；我们的
   `BroadcastWhen(WhenEventKind.Play, …)` 两处调用点（`EffectResolver.cs:2724` 战术路 / `RuleCore.cs:4204` 单位路）
   **都传 `target = null`** ⇒ 即使将来接 410，这一格也只能给 `null`（`GSC14` 的 410 行 `ta = 10` 不吃这一格，
   所以今天无影响；要修得改那两个广播调用点，跨文件）。
2. **没跑宿主**：监听者路在真局里是否真的走到 `:10662`（`GSC14` 的 criteria = 「对手打出战术」、`ASH81` = 「你触发灵魂石能力」），
   本件是**调用链穷举**得出的，**没有实跑**。要坐实需要一条宿主断言（打一张 `ASH81` + 让对手打张战术，数 410 的入队/结算）。
3. **`EffectResolver.cs` 有并发写手**：本次两次 `Edit` 之间该文件被**别的代理改过**（工具报
   "the file had been modified on disk"）。我已复核：本件只碰 `:10662-10665` 那 4 行、已逐字回退；
   `EffectResolver` 的账落点诊断 = **0 条**。⚠️ 若同一时刻有第二个人也在改这一段，请以本报告的行内容为准再核一次。
