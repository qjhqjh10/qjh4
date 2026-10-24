# G5 · 裸 `Deal N damage` 的 `ta=30` 目标语义 ＋ `ta=200/201` 的 `referencedCards`

> 角色：**引擎执行代理 G5**（2026-10-24）。白名单内改代码/加测试分册；本文件是报告。
> ⛔ 未跑 Unity 批处理 / 未动 git / 未改正本 / 未越白名单。
> ✅ 已跑：`TMPDIR=/tmp/wf_g5 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**；
> 另跑了纯 dotnet 的 `工具/ruleprobe.sh`（`card` / `play` / `play check` / `check`），见 §三。
> 判据第一权威 = `d:/2/tools/decomp_full/`；C4 / C5 报告只作第一入口。

---

## 件1 · 裸 `Deal N damage` 的 `ta=240` 假设（**判据 → 已按 `ta=30` 改**）

### 1·1 判据（现读，逐句）

| 问题 | 结论 | 出处 |
|---|---|---|
| 裸 `Deal <n> damage`（无目标短语）在原版是哪一档？ | **`ta = 30 (target)`** —— 目标 = **调用方传进来的 `targetCard`** | 数据：`资料/普查产出_1024/C4_ta240是否含督军.md` §六·2（当前 build 全池 **33/33** 裸 Deal 卡 = `ta=30`；`ta=240` 全池 2 张都不是裸 Deal） |
| `ta=30` 在目标解析里做什么？ | **不做任何筛选**：`param_5`（`targetCard`）非空就把它塞进结果表，然后直接出函数；为空 ⇒ 空表 | `AbilityLogic__GetTargets.c:192-207`（`if ((iVar6 == 0x1e) \|\| (iVar6 == 0xdc))` → `FUN_180002430(list, param_5)` → `goto` 出口） |
| 「生命最低」是哪一档？ | `ta = 240 lowestHealth`（`BattleManager__GetLowestHealthUnit`），**只有卡面明写 `with lowest Health` 的卡走它** | 同上 + C4 §一/§二；全池 2 张（`SAU70` / `SAU73`） |
| 「AI 怎么挑」 | **每个合法目标各算一条动作的评分、挑最高**：`AI.GetAvailableActions` + `GetActionScore`；评分内部的候选表/排序才走 `targetSelectMethod` | 反编译 `AI__ScoreFromPlayingCard.c` / `AI__SelectBestAction.c`；我们对应 `SimpleAI.EnumerateActions` → `ScoreFromPlayingCard` → `AiScoring.ScoreFromPlayingNow` → `AiTargets.Trim`（`G3` 已按八档重写） |
| 「没人选」时原版呢？ | `targetCard == null` ⇒ **空表**（`:192-207` 的 `param_5 != 0` 那一闸） | 同上 |

### 1·2 改动（`Core/EffectText.cs` · `TryDeal` 的 `tok.Length == 0` 支）

改前 → 改后：

```csharp
// 改前（= B7 那一版；依据「裸 Deal 走 ta=240」—— 数据证伪）
op.Target = new EffectTargetSpec { Raw="(未写目标：…生命最低…)", Side="enemy", Kind="any",
                                   Count = 1, Auto = true, PickMost = "-health" };
// 改后
op.Target = new EffectTargetSpec { Raw="(未写目标短语：原版 `ta=30 target` ⇒ 由施放方选一个敌方单位)",
                                   Side="enemy", Kind="any", Count = 1 };
```

`Auto = false` + `Count == 1` ⇒ `EffectText.PickTarget` 会选中它。**玩家 / AI 各自怎么挑**：

* **玩家**：`CanPlayTactic` 要求一个合法 `targetSlot`（高亮判据 = `IsLegalPick`，与结算同一份）；
  `PlayTactic` 用 `PickSide` 决定点的那一格属于哪一侧。单位卡的 `Duty/Pray/Agenda/Oath` 走
  `RuleCore.UseAlternativeCore`（`AlternativeTargetSide` / `CanPickAlternativeTarget` 都读 `PickTarget`）。
* **AI**：`SimpleAI.EnumerateActions` 对**每个合法目标各发一条动作**（战术卡 = `CanPlayCard(…, slot)`；
  替代行动 = `CanPickAlternativeTarget`），再按 `ScoreFromPlayingCard` → `AiScoring.ScoreFromPlayingNow`
  打分挑最高；候选表/排序走 `AiTargets.GetTargets` / `Trim`（`targetSelectMethod`，`G3` 已实现）。
* **没有选择方的场合**（触发式正文：`Rally: Deal 1 damage` 等，`chosen == null`）：原版 `ta=30` 给空表；
  我们**没有**「为触发式能力临时问玩家」的通道 ⇒ 落 `ResolveTargets` 末段那条**对所有「明写了目标却没人点」
  的正文共用的既有近似**（池序前 N = 格号序）。**如实标：这一格与原版不等价**（见 §四）。
* **生产入口核对**（不改代码，只确认「不会因为多了选目标而打不出去」）：`BattleDriver` 的出牌/技能入口
  的 slot 来自玩家点击或 `SimpleAI.NextPlay / NextAttack / SelectBestAction`（逐目标各一条动作）；
  `-wfdrive` 的 `BattleAutoDrive.NextUiAction` 也走 `SimpleAI.NextPlay` ⇒ 都会给一个真实 slot。

同批就地订正（铁律 5）的**过期记录**（都在白名单内的文件里）：
`EffectText.cs` 的 `TryDeal` 头注 + `EffectTargetSpec.Auto` 的 doc + `FillInsteadTarget` 那段「`Auto = true`」的理由；
`EffectResolver.cs` 的 `PickMost` 支那段「裸 Deal 走的正是 240」；
`RuleCore.cs` 的 `ActiveAbilityDamageEntries` 那句「裸 `Deal N damage` …**不计入**」。
⛔ **`Data/SimpleAI.cs:902` 还有一句同族的过期话**（「用户 2026-09-18 拍的『裸伤害走 lowestHealth』在 AI 出牌这条路上没生效」）—— `RuleEngine/Data/**` 不在白名单，**未改**，需主对话收口。

### 1·3 连带修的一处（否则 `play` 基线会多坏一张）

改完第一版后 `play check` 只差 **1 张：`GOF103 Da Hunt is On`**（`Deal 2 damage.` + `The next Beast you play gains Flank.`）——
伤害没变（本来就打 `FIX_E1`），变的是**第二句**：它的 `gain flank` 是 `Subjectless`（`subjTa = 0 any`），
而 `ResolveTargets` 的 Subjectless 支有一条「有 `chosen` 就用 `chosen`」的既有近似 ⇒ **Flank 加到了玩家点中的那个敌人身上**。

⇒ 在 `Core/EffectResolver.cs` 的 Subjectless 支加了一道 **`chosen` 只属于 `ta=30 / 220`** 的闸：

```csharp
else if (chosen != null && chosen.IsAlive
         && (op == null || op.SubjectTargetsAffected < 0
             || op.SubjectTargetsAffected == 30 || op.SubjectTargetsAffected == 220))
    list.Add(chosen);
```

判据 = `AbilityLogic__GetTargets.c:192-207`：整张函数里**只有** `ta=30 target` 与 `0xdc (220)` 两支读
`param_5`（`targetCard`），其余每一档都忽略它 ⇒ 一条无主语 op 若不是这两档，玩家点的目标**不是它的目标**；
`ta < 0`（读不到）仍按旧行为放行（宁可不改也不猜）。

### 1·4 **是否改 `play` 基线**（实测）

* `bash 工具/ruleprobe.sh play check` → **①-a 主档 1127 张逐字节一致 · ①-b 事件电池 586 张逐字节一致**
  （加闸**之后**实测；加闸**之前**只有 `GOF103` 那张不同）。
  ⚠️ 这只说明**探针夹具分辨不出**「点谁」的差别（探针给的 slot 恰好等于原来最低血那个），
  ⛔ **不是**「结算没变」的证明 —— 判别力在本批新断言里（`RuleEngineTest_G5.cs` ①②）。
* 🔴 **`bash 工具/ruleprobe.sh check`（解析基线）红了 40 行**：`全池 1121 张 · 解析差异 40 行`，
  另加哨兵 ⑦ `[输出指纹]` 红。这 40 行 = 那批卡的 `Target` 块三列（`Raw` / `Auto` / `PickMost`）变了。
  **需主对话重钉** `工具/ruleprobe/基线/out_baseline.txt` 与 `Program.PinnedParseMd5`
  （`工具/**` 不在本批白名单；⛔ 我**没有**自己改指纹常量）。
  逐张名单（前 20）：`Empyric Ambush · Witchfire · ASH21 · ASH30 · AM5 · AM39 · Efficiency and Excellence ·
  AM38 · AM35 · AM1 · Rites of Possession · DA43 · DA33 · DA14 · EC40 · Euphoric Strike · EC50 · GSC40 ·
  GSC46 · SAU38 …`
  ⚠️ 注意其中 **`ASH21` / `DA33` / `GOF95` 这类「`to an enemy` 明写了目标」的卡也在名单里** ——
  因为它们**尾句**的 `deal N damage instead` 是**裸 Deal**（`PeelInstead` 拆出来的第二条 op），也换了规格。

### 1·5 🔴 **与新判据冲突的现存断言**（文件不在白名单，未改，**需主对话更新**）

| 位置 | 现在期望什么 | 改后实际 | 处置 |
|---|---|---|---|
| `Editor/RuleEngineTest.cs:11866` | 裸 `Deal 1 damage` 打**生命最低**的 `B`（槽 5，2 血） | 打**池序第一**的 `A`（槽 3，9 血）⇒ `Board(1,5).Health` 实得 2 | 期望改成「打池序第一 / `A` 8」，或改成显式 `saved` 目标 |
| `Editor/RuleEngineTest.cs:11867` | 槽 3 的 `A` **没挨打**（9） | 实得 8 | 同上 |
| `Editor/RuleEngineTest.cs:11886/11887` | 平手取进场序先者（C 3→2、D 3） | **仍然绿**（平手时「池序第一」与「生命最低+`PlayOrder`」同一个人）—— 但**理由已变**，注释（`:11870-11877`）读起来会误导 | 只改注释 |
| `Editor/RuleEngineTest.cs:27204-27205` | `Oath 1: Deal 3 damage` 的主动技能伤害预览 = **0** 条（理由写「裸 Deal 是 `lowestHealth(240)`，只认 `target(30)`」） | 实得 **1** 条（裸 Deal 现在**就是** `target(30)` ⇒ **该计入**，与原版同向） | 期望 `1`、值 `3`；注释就地订正 |
| `Editor/RuleEngineTest_EngineB7.cs:154-162` | ① `Parse("Deal 2 damage")` 的 `PickMost == "-health"`；② 端到端打 4 血的 `live`、残骸不挨 | ① 实得空串；② 池序第一 = 残骸（槽 3）⇒ 残骸挨打、`live` 不挨 | 该分册整段（⑥）改口径；①~⑤ 用的手搓 `B7Lowest(...)` 规格**不受影响** |

---

## 件2 · `ta = 200 / 201` 的 `referencedCards`（**结构件，今天不改变任何一局**）

### 2·1 结构（三处，全在白名单）

| 件 | 落点 | 内容 |
|---|---|---|
| **载体** | `Core/UnitState.cs`（`PlayOrder` 之后） | `public readonly List<UnitState> ReferencedCards` —— 原版 `CardScript.referencedCards // +0x278`。**不落卡表、不序列化**，每局从空开始 |
| **写点（能力 380）** | `Core/EffectResolver.cs` 新增 `RuleCore.SetReferenceCards(ctx, owner, acting, spec, source, chosen)` | 照 `AbilityLogic__PlayAbility.c:2128-2160`：**先换新 List**（我们用 `Clear()` 等价表达，字段是 `readonly`）再把**该条 logic 自己的目标表**写进去；目标表直接复用 `ResolveTargets`（与读侧同一份解析）。副作用：`ResolveTargets` 会覆写 `ctx.LastTargets/LastTarget` ⇒ **先存后还原**（原版 380 不碰这两个代词槽） |
| **读点（`ta=200/201`）** | `Core/EffectResolver.cs` · `ResolveTargets` 的**最前面**（`source` 回落之后、`prev` 之前） | 按 `op.SubjectTargetsAffected` 分派：`200` = **整表**（不过筛、不 Trim、不看 criteria）；`201` = 只留 `IsUnitInPlay`（原版 `CardScript.IsInPlay` 的等价物）。写 `ctx.LastTargets/LastTarget` 后返回 |
| 顺带 | `TargetsAffectedName` | 补 `200 referencedCards` / `201 referencedCardsInPlay`（日志用） |

**为什么「按 op 分派」而不是给 `EffectTargetSpec` 加字段**：`EffectTargetSpec` 的字段表被
`工具/ruleprobe/Program.cs` 的 `TargetCols` 哨兵**逐字段对账**（`TargetColsDrift`），加列会让 `check` 直接红，
而 `工具/**` 不在白名单 ⇒ 用本仓既有的 op↔`targetsAffected` 镜像 `SubjectTargetsAffected`（`B-17` 那一列）。

### 2·2 断言（`Editor/RuleEngineTest_G5.cs`，新建 + `.meta`）

见 §三逐条。**灭自证那一对**：不调用 `SetReferenceCards`（= 不消费 380）⇒ 引用表空 ⇒ `ta=200` **得空表**；
若读侧在空表时「回落到现筛目标池」⇒ 那一格会拿到己方督军/施放者 ⇒ 红。

### 2·3 **补完后这两张的行为会不会变：不会**（实测 + 断言）

* 引擎**不跑 SO `abilityLogic`**，`cards_engine.json` 也没有 `chosenAbility` 这一列
  （同 `FollowUpSchedulerOf` / `ShowCardsWindowOf` 那两处已记的限制）⇒ **380 没有自然触发口**，
  本件把语义落成**可被调用的一个口**（自检直接调），生产调用点**留空**。
  判据：`AbilityLogic__PlayAbility.c:2128-2160`（`iVar8 == 0x17c`）· `资料/普查产出_1024/C5_剩余判据普查.md` §3。
* 两张真卡的**文字管线今天一条 op 都读不到 200/201**（`play`/`card` 现读：`AM54`、`GOF93` 全部 `subjTa = -1`）
  ⇒ 读侧分支今天**不可达**；断言 `CountTaOps == 0` 把它钉住。
* 它们的现有行为仍由卡面文字覆盖（`AM54`：抽 3 张 + 战将 ≤10 血则降费 4；`GOF93`：`choosecard` + `forceattack`）。

---

## 三 · 逐条断言（`RuleEngineTest_G5.cs`，全部带 `🧨`）

入口 `TestG5BareDealAndReferencedCards`；`RuleEngineTest.cs` **只加两行**：
`Section("🆕 引擎线 G5：裸 Deal N damage 的 ta=30 目标语义 · ta=200/201 的 referencedCards")`
+ `Step(TestG5BareDealAndReferencedCards)`。

**件1（解析层）** `G5BareDealSpec`
1. `Parse("Deal 2 damage")` 出 1 条 op；
2. `Target.Auto == false`（🧨 改回 `Auto=true` ⇒ 红，玩家无从选）；
3. `Target.PickMost` 为空（🧨 改回 `-health` ⇒ 红）；
4. `Side == "enemy"`（🧨 反侧 ⇒ 红）；5. `Kind == "any"`（🧨 收窄 troop ⇒ 督军被静默排除）；
6. `Count == 1`；7. `PickTarget(ops) != null`（🧨 `Auto=true` ⇒ null ⇒ 红）；8. `PickSide == "enemy"`。

**件1（结算层）** `G5BareDealWhoGetsHit`
9. 夹具让三种口径指向**不同的人**（槽 3 = 池序第一 ∩ 生命最低 1 血；槽 5 = 玩家点的 6 血）：
   **点槽 5** ⇒ 槽 5 `6 → 4`（🧨 忽略 `chosen` ⇒ 打槽 3 那个 ⇒ 实得 6）；
10. 槽 3 仍 1（🧨 同上 ⇒ -1）；
11. **没人点** ⇒ 打**池序第一**（槽 3，6 → 4）（🧨 回到 `PickMost` ⇒ 打 1 血那个 ⇒ 实得 6）；
12. 槽 5（1 血）不挨（🧨 同上）。

**件1（灭自证的另一半）** `G5LowestHealthStillHitsLowest`
13. `Parse("Deal 1 damage to the enemy with lowest Health.")` 的 `PickMost == "-health"`（🧨 顺手删了 lowest/highest 支 ⇒ 红）；
14. 没人点时打 3 血的（3 → 2）（🧨 删 `PickMost` 支 ⇒ 退池序 ⇒ 打 9 血那个）；
15. 9 血那个不挨（🧨 同上）。

**件1（真卡）** `G5BareDealRealCards`
16-18. `AM5 Shock Trooper` / `TAU44 Pulse Onslaught` / `SAU38 Extermination Protocol`：每条 `deal` op 都是
「敌方 · 一个 · 非 Auto · 无 `PickMost`」（🧨 任一条回到旧口径 ⇒ 该格少 1）；
19. `SAU73 Annihilation Barge` 的 `…lowest Health` 仍带 `PickMost = "-health"`（🧨 把两档合并 ⇒ 红）。

**件2** `G5ReferencedCards`
20. `SetReferenceCards(…, 敌方部队全表)` ⇒ 返回 **2**、引用表就是那两张（按引用认）（🧨 不写 ⇒ 0）；
21. `ta=200` ⇒ **2 张**，且**不是**文本 spec（`Side=own`）现筛的池（🧨 读侧没接 ⇒ 拿到己方督军 + 施放者）；
22. 让 `G5RefB` 离场后：`ta=201` ⇒ **1 张**（留 `G5RefA`）（🧨 201 不筛在场 ⇒ 2）；
23. 同一张表：`ta=200` ⇒ **仍然 2 张**（🧨 200 也按 201 筛 ⇒ 1；与 22 互为灭自证）；
24. **不消费 380** 的新牌：`ta=200` ⇒ **空表**（🧨 空表回落目标池 ⇒ 红；与 20/21 互为灭自证）；
25. 再写一次 ⇒ **覆盖**成 1 张（🧨 写成追加 ⇒ 3）；
26. 数据钉：`AM54` 有 `ta=200`、`GOF93` 有 `ta=201`，且**不串档**（🧨 卡表重生成丢列 ⇒ 红）；
27-28. **今天的行为不变**：两张卡的**文字管线**各有 **0** 条 op 带 200/201
（🧨 谁给文字管线硬塞 200/201 ⇒ 红，且必须同时改本报告 §2·3）。

---

## 四 · 没查清 / 没做（如实列）

1. **`ta=30` 的「无人选」场合**：原版 `targetCard == null` ⇒ **空表**；我们落「池序前 N」的既有近似。
   要不要为触发式正文补一条「代选」通道（并定清按什么挑）**没查清、没做** ——
   那会影响全池所有 `to an enemy` 的触发正文，不是本件能定口径的范围。
2. **380 的生产调用点**：引擎不跑 SO `abilityLogic`、`cards_engine.json` 无 `chosenAbility` 列
   ⇒ 本件只落结构 + 自检直调，**未接生产**（⛔ 没在文字管线里硬塞一个猜测的调用点）。
3. **手牌战术卡作 `actingCard` 的引用表没有载体**：`AM54 At All Costs` 那条 380 属于「手牌打出的战术卡」，
   而本批白名单不含 `Core/CardInstance.cs` ⇒ 挂在 `UnitState` 上，**手牌卡这一半未做**
   （读侧遇到 `source == null` 会**出声**并返回空表，不静默）。
4. **`FilterMethods.CheckIfMeetsCriteria` 里 200/201 的另一半语义**（候选必须**在**引用表里，
   `FilterMethods__CheckIfMeetsCriteria.c:92-127`）**未接** —— 那是「筛选条件」不是「目标表」，
   本件只做了 `GetTargets` 的两档。`ApplyTargetFilters` 没有对应位。
5. **新断言只过了编译（`typecheck` 0 错），没跑过 Unity**（红线：子代理不跑批处理）。
   `run()` 的用例表变了 ⇒ **需主对话跑 `工具/gen_diag_runner.py` 重生成**（工具不在白名单）。
6. `RuleEngineTest.Run` 的**现存红**见 §1·5（4 处，其中 `11886/11887` 只是注释要改）。

---

## 五 · 顺手发现（只报不改）

1. 🔴 **`PickTarget` 不排除 `PickMost` 规格 ⇒ `ta=240` 的卡仍然要玩家点，而且点了之后覆盖「生命最低」。**
   `EffectText.PickTarget` 只跳过 `Auto / Random / Count != 1 / prev / warlord / AttackedBySelf`，
   而 `Deal 3 damage to the enemy with lowest Health` 的 spec 是 `Count=1, Auto=false, PickMost=-health`
   ⇒ 它会被当成「要玩家选目标」；`ResolveTargets` 末段
   （`chosen != null && spec.Count == 1 && list.Count > 0 && !list.Contains(chosen)` ⇒ `list.Clear(); list.Add(chosen)`）
   **用玩家点的那个覆盖掉算出来的最低血那个**。
   ⇒ 真机上 `SAU73 Annihilation Barge` / `SAU70 Damaged Hexmark` 的实际目标 = **玩家点的那个**，不是最低血。
   判据上 `ta=240` **忽略 `targetCard`**（`AbilityLogic__GetTargets.c:835-844` 直接 `goto` 出口）⇒ 应跳过。
   **未改**（属 `B7` 那条判据线的收口，且会改 UI 交互；需调度台定口径后再动）。
2. 🔴 **裸 Deal 现在计入主动技能伤害预览**（`RuleCore.DealsToPickedTarget`）：`Duty: Deal 8 damage` 那族
   以前预览里是 **0**，现在有数 —— 与原版同向（`GetActiveAbilityDamage` 只认 `ta=30`）。
   它让 `RuleEngineTest.cs:27204` 那条反例红了（见 §1·5）。
3. 🟡 **`Data/SimpleAI.cs:902` 的注释已过期**（「裸伤害走 lowestHealth 在 AI 出牌这条路没生效」）——
   `Data/**` 不在白名单，未改。
4. 🟡 **`AM5` 这类卡的 `subjTa = -1`**：卡 SO 的 `ta=30` **没有**落到 op 上（`AssignAbilityTargets` 的 G1/G2 闸没过）。
   本次改动不依赖它（走文本），但**任何想靠 `op.SubjectTargetsAffected == 30` 判断「这条是 target 档」的代码，
   在 33 张裸 Deal 卡里会读不到**。同理 200/201 的读侧今天对 `AM54`/`GOF93` 也读不到（= 结构不可达的实证）。
5. 🟢 **`play check`（结算层）在加闸后逐字节一致**，是个可复用的对拍结论：
   「改 `chosen` / 选目标语义」这类改动，探针夹具**分辨不出来**（它给的 slot 恰好等于旧口径挑中的人）
   ⇒ 判别力必须写在 `RuleEngineTest` 的显式 `chosen` 断言里（本批 9/10 那两条就是为此写的）。
