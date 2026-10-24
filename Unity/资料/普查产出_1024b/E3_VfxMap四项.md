# E3 · `VfxMap` 四项（D3 · D4 · UM37/UM84 · D10(C4)）

> 执行代理 **E3** · 2026-10-24 · 有写权限。
> **结论先说**：这四项**在我开工前就已经由 `B1` / `A2` / `A8` 落地**（都在提交 `955e7f9d` 里，我逐条现读核过、**不是转述**）。
> 本代理**没有重做**它们，做的是三件仍然缺的事：
> ① 给 **D3** 的「桶搬移」与 **D4** 的「按拍分件」补**全池逐卡**自检（原来只有 3~4 张夹具卡 + 没有 VfxMap 侧覆盖）；
> ② 给出**独立**的断言文件 `Editor/BattleVfxAssertE3.cs`（21 条，含 4 处只在本文件里断的靶子：UM84 的 `T`、EC57 的 `so` 归属、`Wait_1s` 无件、旧桶不许留）；
> ③ 用离线预演证明「分类键带 `Order`」这一条**已经不需要再做**（见 §五·1）。
> **改的文件**：`Core/VfxMap.cs`（+220 / −0）· 新建 `Editor/BattleVfxAssertE3.cs`(+`.meta`)。
> **没动**：`BattleDriver.cs` / `BattleScene.cs` / `RuleEngine/**` / `cards_engine.json` / 正本 / git；**没跑 Unity**。
> `TMPDIR=/tmp/wf_e3 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（最后一次，2026-10-24）。

---

## 一 · 四项的现读状态（原值 → 新值 · 桶 · 行）

| # | 桶 / 位置 | 原值 | 新值 | 谁落的 | 判据 |
|---|---|---|---|---|---|
| **D3-a** | `attack_melee` → `unit_attack_anim` | 逐卡 12 行当「近战攻击粒子」播在**受击方** | 载入时**搬**进 `unit_attack_anim`（`VfxMap.cs:1197` 附近 `m[UnitAttack] = picks;`，旧桶**不写回**） | `T1`(W63) / `B1` | `A21` §二·4 |
| **D3-b** | `hit` → `unit_damaged_anim` | 逐卡 1 行（`BL20`）当「受击特效」 | 搬进 `unit_damaged_anim`（`:1206` 附近） | 同上 | 同上 |
| **D3-c** | `BattleDriver.ScheduleAttackPresentation` 的 ③ | 攻方**无条件**播自己的 `UnitAttack`（近战/远程都播、不判 ability 是否结算） | **整段已删**（`:18622` 起那段注释），改由 `PlaySignal` 的 `EvtKind.Trigger` 支按 canonical 分流到 `PlayUnitSkillAnim`（`:19252-19278`） | `B1` | `CardAbility__CanTriggerAbility.c:126-140` |
| **D4** | `trigger` 桶 | 掉回整桶那两档（② 无 `animTimes` / ④ 关键词不在白名单）**整桶播** ⇒ `Start(0)` 那批在**每次触发**时重播 | `TriggerBeatRows` 只留 `Trigger(3)`/`TriggerOnPlay(6)` + 卡级行（`VfxMap.cs:2583-2590`，两处调用 `:2752` / `:2781`） | `A2` | `DisplayTriggerAnim.c:71` / `DisplayStatusAnim.c:86-119` |
| **UM37/UM84** | 分类键 `AddTriggerNamesOf` | 只取**第一条**同名行 ⇒ 第二条 occurrence 的 trigger 结构上取不到 | 返回**全部** trigger 名（命中任意即算）+ `TrimToLabelledRowCount` 按行数截断（`:2612-2690`） | `A2` | `cards_engine.json` 的 `animTimes[].o` |
| **D10(C4)** | `voice_lines.json` 的 `EC57` | **没有 `deploy` 行** ⇒ 登场静默 | 加 `deploy` 行 = `VO_UM_Primaris Intercessor - For the Imperium.ogg`（`src=cardSO` / `so=summonAudio`） | `A8` | 原版卡 SO 的 `summonAudio`（原版自己抄错，裁定「照原版播」） |

**本批新增（真·缺口）**：`VfxMap` 里这两件事**一条全池断言都没有** ——
`SelfCheckRoutedTriggers` 只覆盖 `MustRouteTriggerNames`（`UnitAttack` 走 `PlaySignal` 分流，不在那张表），
而且它走的是第 ③ 档（目标名定得出来）⇒ `TriggerBeatRows` 的 ②/④ 两档**没被任何自检碰过**。

---

## 二 · 改动清单

| 文件 | 改了什么 |
|---|---|
| `Core/VfxMap.cs:207-216` | 新增 `public const string UnitDamagedTriggerName = "UnitDamaged"`（只读消费者两处，注释写明 ⛔ 别让 `BattleDriver` 改用它） |
| `Core/VfxMap.cs:3089-3298` | 新增 **`SelfCheckUnitSkillAnims()`**（D3 全池逐卡：① 搬全了 ② 不多不少 ③ **「搬」不是「抄」**（旧桶不许留）④ 计数器 = 逐卡加总）与 **`SelfCheckTriggerBeatSplit()`**（D4 全池逐卡：②/④ **两档**都只回触发那一拍该播的；空放 / 恒 false 也报） |
| `Editor/BattleVfxAssertE3.cs`（新） | `public static class BattleVfxAssertE3 { public static void Run() }`，**21 条**断言（D3 6 · D4 5 · UM37/UM84 3 · D10 4 · 白名单回归 2 · 前提 1），结果写进 `Passed` / `Failed` |
| `Editor/BattleVfxAssertE3.cs.meta`（新） | `guid: 4d34530fcaf34efd84fad9175944b106` |

**挂载点（主对话做）**：`BattleScene.Run` 里，`W63/D3：UnitAttack(50) 归口到技能动画` 那一大块 `{ … }` **收尾之后**、
`// ---- 8. 原版卡牌接进对战` 之前（现读约 `BattleScene.cs:12056`）加一行：
```csharp
BattleVfxAssertE3.Run();   // E3：D3/D4/UM37·UM84/D10(C4) 的灭自证断言（Failed == 0 才算过）
```
> 位置理由：它**不碰驱动状态**（只读卡池 / VfxMap 表 / 语音表），放 `---- 8.` 之前不会污染下一节；
> 紧跟 D3 那一节读日志时能连着看。`VfxMap.ResetUnpinnedForTest()` 在 `Run()` 里已调（全池自检走生产解释路径，会给那个计数加数）。

---

## 三 · 断言清单（每条的「🧨 改回去会红」写进文案了）

**D3（6）** 全池自检 · `SW26`（新 1 / 旧 0）· `DA22`（2 件 + `T` 0.0/0.7）· `UM70`（**判据取远程**也有件）· `SAU31`（`Wait_1s` 无件 = 1 件）· `BL20`（`unit_damaged_anim` 1 / `hit` 0）。
**D4（5）** 全池自检（②+④ 两档）· `AM27`（② 档）· `GOF26`（④ 档）· `DA10`（**卡级行不许被切**）· `AM15`（正例）。
**UM37/UM84（3）** `UM37` codex 1 件 · `UM84` codex 2 件 **且 `T` = 0.0 / **0.5****（`Order 1.0.1`；钉回 `ThisCardPlayed` 那条是 **1.0** ⇒ 红）· `SW18` WhileInPlay 2 件（正例）。
**D10（4）** `deploy` 行存在（`ev == deploy`）· 指的就是 **UM 那句** · `SoRowCount ≥ 1`（**卡 SO 指名**，不是按名字猜的行）· 音频文件在。
**回归（2）** `SelfCheckRoutedTriggerNames()` / `SelfCheckRoutedTriggers()` 都空（= 上一轮的 D1/D6/D7/D11/D12/D14… 没被这次改动碰坏）。

---

## 四 · 证据（现读，不是转述）

| 项 | 读数 |
|---|---|
| `unit_attack_anim` | **11 张 / 12 行**（`SAU31` 的 `Wait_1s` 是纯计时拍无 prefab）· `CardTableUnitAttackEntries == 12` |
| `unit_damaged_anim` | **1 张 / 1 行**（`BL20`）· 现读 1 |
| 旧桶残留 | `attack_melee` / `hit` 在这 12 张卡上**各 0 行**（搬移是「搬」不是「抄」） |
| D4 覆盖面 | 「`trigger` 桶里有**任意**非触发拍行」现读 **237 张 / 258 行**（`Start(0)` 258 行）；`A2` 注释里的「46 张」是**「≥2 行且混 `Start(0)`」**那一档，**两个数别混用** |
| `TraitFrames.TriggerTypeOf` 反查表 | `Resources/trait_effect_bindings.json` 75 行（`triggerType`: 3→21 · 2→18 · 0→13 · 5→10 · 6→7 · 1→6）；`ArmourTraitStart`/`RallyTraitStart`/`FlankTraitStart` = 0、`TalentTraitTrigger`/`ConcussiveTraitTrigger` = 3、`EcstasyTraitTrigger` = 6 |
| UM84 的 `T` | `CodexTraitTrigger_warlord` 行 = `Order 1.0.0 / t=0.0`；`Buff_Purple_target` 行 = `Order 1.0.1 / t=0.5`；歧义那条 = `0.0.1 / t=1.0` |
| EC57 | `voice_lines.json` 现读 `EC57.lines` 有 `{ev:deploy, file:VO_UM_Primaris Intercessor - For the Imperium.ogg, src:cardSO, so:summonAudio}` |
| 离线预演 | 两条新自检按**同一段逻辑**（装载搬移 / 桶内容 / `TriggerTypeOf` / 两档回退）在真实数据上跑：**D3 0 违规**；**D4 237 张卡两档名字集合逐卡一致**（脚本 `_tmp_view/e3_sim/sim.py` · `sim2.py`） |

---

## 五 · 顺手发现（按铁律 13·4⑧ 报上来，⛔ 我没改正本）

1. 🔴 **「分类键带 `Order`」（战斗线总表 `:85` / `:90` 的「另账」）已经不需要做 —— 建议销账。**
   `A2` 的「一条候选可以带多个 trigger 名」+ 现有**贪心游标钉行**已经给出正确结果。我按「按 trigger 名再重钉一遍」写了一个
   后置修正（= 真正的 Order 感知钉行）在**全池逐卡 × 每个 routed trigger** 上预演：**0 处差异**（`_tmp_view/e3_sim/sim.py`）。
   ⇒ `UM37`/`UM84`/`SW18` 三行的件与**时刻**都已经对；⛔ 别再去加那层「按 Order 重钉」（加了只是多一条没人验的口径）。
2. 🔴 **战斗线 `任务总表.md` 的 D3 / D10 状态过期**（`:68` 仍写「仍开：D3 · D10(C4)」、`:91` 写 `✗ 未做`）：
   D3 已由 `B1` 落地（`BattleScene` 有 ①–⑤ 五条行为级断言）、D10 数据行已在 `voice_lines.json`、`AnimFXCheck` ⑤b 也有断言。
   ⇒ 建议主对话核销（正本我改不了）。
3. ⚠️ **两个「D4 张数」不是同一个口径**：`A2` 记的 **46 张 / 103 行**是「桶里 ≥2 行且混 `Start(0)`」；按「有任意非触发拍行」是 **237 张 / 258 行**。写账时带上口径，别混用。
4. ⚠️ 我的 `SelfCheckTriggerBeatSplit` 会在**首次跑**时给 `VfxMap.UnpinnedPicks` 加数、并可能打满 16 条 `[VfxMap] … 钉不上 AnimTimes` 警告（天赋件本来就不在 `animTimes` 里）。这是**既有行为**、不是新缺陷；断言前已 `ResetUnpinnedForTest()`。

## 六 · 没查清 / 如实记

1. **没跑 Unity**（本批规则）：两条新自检的「运行时读数」是我用 Python 按同一逻辑预演的（§四末行），**不是 Unity 实跑**。
   它们绿不绿要主对话跑 `BattleScene.Run` 才知道；我预演的结论是「应当 0 违规」。
2. **D3 的「谁攻击时播 / 几拍 / 载体」是行为级**，只在 `BattleScene.cs` 的 `W63/D3` 那一节（`B1` 落的）。本文件**不复刻**它 —— 两份会打架。
3. **D4 的 `TriggerTypeOf` 依赖 `Resources/trait_effect_bindings.json`**：若那份资源缺失/解析失败，`TriggerTypeOf` 恒 -1 ⇒ 我的自检会报
   「一行都没被挡掉」的**红**（这是**刻意**的：不让闸退化成装饰）。看到那条红先查资源，别去改自检。
4. **`SelfCheckTriggerBeatSplit` 只验「名字集合」**（哪几件播/不播），**不验顺序与 `T`** —— 那两格在 `D4` 的语义里没有原版判据（`Start(0)` 那批本来就不该出现在这一拍，顺序无意义）。
