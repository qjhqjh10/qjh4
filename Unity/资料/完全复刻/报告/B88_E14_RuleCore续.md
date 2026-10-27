# B88 · E14 · `RuleCore` 续（Ability priority 发侧 + 783 写点 + VarsGlobal 撞名）

> 执行代理 `E14` · 2026-10-27 · 白名单 = `RuleEngine/Core/RuleCore.cs` / `RuleCorePartial_B88E1.cs` /
> 新建 `BattleContextPartial_B88E14.cs` / `RuleCorePartial_B88E14.cs`（＋调度台当场授权的一处：
> `CardPresentation/Core/VarsGlobal.cs` 改名）。
> ⛔ 没跑 Unity · 没动 git · 没改账主表 / 正本。

---

## 一、结论（四句话）

1. **`priority` 发侧接完了**，但**不是**用「尾部 `int priority = 0`」那种重载 —— 那个写法产生 **20 条 `CS0121`**；
   改成 **`priority` 挂第 5 位且必填**（C# 不许「必填排在可选之后」`CS1737`，第 5 位是唯一可行位置），
   **既有 15 实参调用点一个都不用改**。
2. **四处发点传值**：`:11626` → `1` · `:11791` → `1` · `:12054` → `AlternativeAbilityTriggerPriority(ctx, u.Card, keyword)` · `:15432` → `1`。
3. **783 的写点：不做**（判据 = 原版**没有调用点**，五源 + 二进制全 0；造一个就是发明原版没有的行为）。
4. **`typecheck` 运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_s88e14`，改完连跑两次都是 0/0）。

---

## 二、逐条证据

### 2·1 重载形状（为什么不是尾部默认值）

| 写法 | 结果 |
|---|---|
| `…, int abilityIndex = -1, int priority = 0)`（第一版） | **20 条 `CS0121`**：既有 15 实参调用同时匹配两个重载（调度台当场报的，波及 `TutorialScript.cs:1496` / `Enchantment.cs:360` / `EffectResolver.cs` 多处 / `RuleCore.cs:2947`） |
| `…, int abilityIndex = -1, int priority)` | **CS1737**：必填形参不能排在可选形参之后 |
| **`Emit(kind, player, slot, cardId, int priority, 其余全可选)`（采用）** | 既有调用**少一个位置实参** ⇒ 本重载不适用 ⇒ 只匹配主文件那一版；新调用写 `priority: 1`（具名）⇒ 只有本重载适用。两条**互相排斥** ⇒ 零二义 |

实现（`RuleEngine/Core/BattleContextPartial_B88E14.cs:48-70`）**不复制字段清单**：先转调主文件那条唯一实现，
再把 `Priority` 补到**刚发出去的那一条**上 —— `Emit(BattleEvent)` 把**同一个对象**同时放进
`Signals` 与 `ActionLog`（`BattleContext.cs:1880-1881` 现读：`Signals.Add(e); AppendLog(e);`）⇒ 改一处两边都对。
内部那条调用是 15 个位置实参（第 5 个是 `string keyword`）⇒ 只会匹配主文件那一版（⛔ 不递归）。

### 2·2 四处传值（`RuleCore.cs`，全部零净行数）

| # | 行 | 方法 | 传值 | 判据 |
|---|---|---|---|---|
| ① | `11626` | `UseAbilityCore` | `priority: 1` | 施工单 §二 ①；`decomp_full/RawCardScript__ResolveActiveAbility.c:47` 固定 1 |
| ② | `11791` | `UseOathAbilityCore` | `priority: 1` | 誓约同走 `ResolveActiveAbility` 那一支（施工单 §二 ②） |
| ③ | `12054` | `UseAlternativeCore` | `AlternativeAbilityTriggerPriority(ctx, u.Card, keyword)` | 施工单 §二 ③：查这条替代能力**原版 trigger 名**的 `CardAbility.priority` |
| ④ | `15432` | `UseActiveAbilityCore` | `priority: 1` | 形状判定见下 |

**④ 的判定（施工单要求「先判它属于哪一支」）**：这一支是 `ResolveActiveAbilityRows(...)` 那条路
（`effect: "activeAbility"`、`activeAbilityLogic` 的行、`ChosenAbility/TargetsAffected` 逐行跑），
与 ① 的 `Ability:` 关键词是**同一族主动能力**（`RawCardScript.ResolveActiveAbility`），
原版那一支的 priority **硬编码 1**（`RawCardScript__ResolveActiveAbility.c:47` 的 `SetNewData(…, 1, …)`）
⇒ **不是** `Ability:` 那条「按 `CardAbility.priority` 查表」。故传 **1**，⛔ 没有照抄 ① 就算完
（① 的 1 有独立判据）。

**③ 的触发名映射**（`RuleCorePartial_B88E14.cs:46/64`）：`duty→Duty` · `pray→Pray` · `ferocity→Ferocity` · `agenda→Agenda`
（原版 `AbilityTrigger` 枚举名，转写表 `CardDef.AbilityTriggerNumberOfName`；
⛔ 不用 `TriggeredPray/TriggeredDuty/TriggeredFerocity` —— 那是「被触发」那一族）。
**现读数据**：`cards_engine.json` 的 `abilityTriggerPriority` 列只有 **7 张卡 / 6 个键**
（`Artifice` / `UseSpiritStone` / `UnitReceivesMarkOfChaos` / `OnCollectQuestPoints` / `FollowUpResolution` / `Codex`）
⇒ 四族今天**全部落回缺省 1**（`CardDef.DefaultTriggerPriority`，判据 `CardAbility__.ctor.c:5`）
⇒ 与「写死 1」行为相同，但走查表这条路，将来数据补上自动生效。
⚠️ 认不出时 `ctx.Log` 出声 + 回缺省 1（**理论上不可达**：上游 `CanUseAlternative` 已用 `IsAlternativeAction` 拦过）。

### 2·3 零回归 / 指纹

- `BattleEvent` 只多一个字段、**条数不变**；`NetProtocol.Fingerprint` 现读只读
  `Turn/Active/Winner/**Events.Count**/PlayerHash×2`（`NetProtocol.cs:555-568`）⇒ **指纹稳定**。
- 那四处以外一处未接 ⇒ 其余事件仍 `Priority = 0` ⇒ `AnimLaneRank(0) = 3`、并列仍「先来先播」（`BattleDriver.cs:36351-36356`）。
- `BattleDriver.AnimLaneRanksForTest()` 现读 = `AnimLaneRank` 的 4 档原样映射（`:36351-36363`）：
  `p2→0 · p1→1 · p3→2 · p0→3` ⇒ 验收口在，子代理不跑 Unity，未执行。
- `RuleCore.cs` **仍是 15,587 行**（改动全部是「已有行行尾 + 实参 + 同行注释」）⇒ 账落点漂移 **0**。
- 全仓 `EvtKind.Ability` 的**发射点只有那四处**（`grep` 现读：其它命中全是注释 / 消费端 / 自检）⇒ 施工单「步骤 3（E2）」确认**不用做**。

### 2·4 783 `HideWarlordActiveAbilityForTutorial` —— **原版无调用点**

| 源 | 读数 |
|---|---|
| ① `decomp_full` 全树 | 只有它自己的定义文件 `.c:1-8`，**任何方法体都没有调用**（对照：`Show` 的在 `AiScripted__ShowPlayerActionAnim.c:606`） |
| ② `out_cpp2il_dev_ilspy` | `CardScript.cs:20120` 只有定义 |
| ③ `out_il2csharp`（**含异步状态机体**） | `Assembly-CSharp/CardScript.cs:12981` 只有定义 |
| ④ `assets_full` 全树字符串 | **0**（含 prefab `m_MethodName` / SO 引用） |
| ⑤ **二进制**（新增，最硬） | `GameAssembly.dll` 的 `il2cpp` 节（VA `0x1804B3000` + 长 `0x2FFE15C`）**全节**线性扫三种引用 → **0**（`E8 rel32` 调用 / `E9 rel32` 跳转 / `48 8D [rip+rel32]` 取址） |

**扫描法自身可信度**（关键）：同一套扫描对 `ShowWarlordActiveAbilityForTutorial`（VA `0x1805FEBB0`）
**精确命中 1 处 = `0x18094474D`**，正是 `AiScripted__ShowPlayerActionAnim.c:606` / ILSpy `AiScripted.cs:1483`
那处 `TapCard` 支的调用 ⇒ 不是「扫法失效」。

**语义补全**：
- `Show` = **从卡数据恢复**：`+0x220 = RawCardScript(+0x28)[+0x2a9]`（`CardScript__ShowWarlordActiveAbilityForTutorial.c:5-6`）——
  不是「打开」，是「还原成数据值」。
- `CardScript.set_hasActiveAbility`（公开 setter）同样 **0 调用点**（decomp 层）。
- 现 build 里 `+0x220` 的写点只有四个内部处：`SetBaseCardTraits`（从数据灌）· `ReactToUnitJammed`（= 我们的 `ActiveAbilitySuppressed`）·
  `AddEffect`（`0x101` 一次写两格）· **本方法（0 调用）**。
- `tutorial_stages.json`（6 关 SO）**没有这一档**：只有 `hideCemetery` / `hideCardsLeftInDeck` / `hideLargeCardDisplay` / `hideChat`
  四个 hide 开关；23 个 `ScriptedActionType`（`0=None`…`170=ActivateHandCards`）**无一相关**。
  ⇒ 「哪一关、对哪张督军隐藏」这个判据**在原版里不存在**。

⇒ **处置**：⛔ 不造写点（铁律 11 例外①「原版本身就没有」）；我们的 `UnitState.ActiveAbilityHidden`
保留（默认 false = 不隐藏 = 与原版等价），读点 `RuleCore.cs:15304` 不动。
账里**保持 C**、并把证据与「建议判 D」写进备注（等级由调度台裁定）。

---

## 三、改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| **新建** `RuleEngine/Core/BattleContextPartial_B88E14.cs` | 带 `priority` 的 `Emit` 重载（第 5 位必填，转调主文件实现后补 `Priority`） | 71 行（新文件） |
| **新建** `RuleEngine/Core/RuleCorePartial_B88E14.cs` | `TriggerNameOfAlternativeKeyword` + `AlternativeAbilityTriggerPriority` | 79 行（新文件） |
| `RuleEngine/Core/RuleCore.cs` | 四处 `ctx.Emit(EvtKind.Ability, …)` 尾行加 `priority:`（含同行注释） | **净 0 行**（仍 15,587） |
| `CardPresentation/Core/VarsGlobal.cs` → **`LevelCurve.cs`** | 类 `VarsGlobal`→`LevelCurve`（修 `CS0104`；旧文件已删，未进过 Unity、无 `.meta`、全仓 0 调用点）；⛔ **判据里的 `VarsGlobal__*.c` 一律保留原名** | 149 行（原 146 + 3 行改名说明） |
| **新建** `账/_parts/B88_E14.tsv` | 2 行（`特效动画:486` 的 `AddCardAnim` · `战斗逻辑驱动:783` 的 `HideWarlordActiveAbilityForTutorial`） | 11 列表头 + 2 行 |

**typecheck**：`TMPDIR=/tmp/wf_s88e14 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（改完连跑两次）。

**⚠️ 合并顺序（必须先输后赢）**：`check_patch_conflicts.py` 现读 ——
`AddCardAnim(…)` 同时出现在 `B88_E5b:6` / `B88_E5c:2` / `B88_E5d:2` / `B88_E14:2`，
`HideWarlordActiveAbilityForTutorial()` 同时出现在 `B88_E4c:2` / `B88_E14:3`。
按字典序 **`B88_E14` 会输给 `B88_E5d` / `B88_E4c`**（`"B88_E14" < "B88_E5d"`）⇒
**请按 `E5b → E5c → E5d → E14`、`E4c → E14` 的顺序逐份 `--only <完整文件名>` 合并**，否则我这两行会被旧版盖回去。

---

## 四、没查清的部分

1. **替代行动四族的 trigger 名是「按枚举名推的」，不是「从卡数据读的」** —— 原版 SO 里
   `hasAlternativeAbility` 在 `cards_truth.json` 全池只有 **1 张**（`SW21 Wulfen Pack Leader`，且它 `abilities` 为空），
   duty/pray/ferocity/agenda 那 68 张卡**没有对应的 `CardAbility` 行** ⇒ 它们的 priority 在原版数据里
   **无从直接读**。今天两种口径都得 1（数据无该键），⇒ 结论不受影响；**若将来数据补上这四键，我这套映射就生效**。
2. **783 的 `Show` 这一半我们没接**：`AiScripted.ShowPlayerActionAnim` 的 `TapCard` 支会调 `Show`（原版唯一调用点）。
   由于我们**从不隐藏**，接上去是**恒等操作**（`ActiveAbilityHidden` 已是 false）⇒ 本批按「零行为」不做；
   若要逐句对齐，可在 `TutorialScript` 的 `TapCard` 支置 `u.ActiveAbilityHidden = false`（**恒等，无断言价值**）。
3. **`Show` 为什么存在**（谁先隐藏的）在原版里查不到：`Hide` 0 调用、setter 0 调用、关卡数据无此档
   ⇒ 推测是**旧版本残留 / 已被裁掉的教程流程**。**未证**，如实记在这里，⛔ 别当结论用。

---

## 五、顺手发现（⛔ 未越界改）

1. **`cards_truth.json` 顶层是「对象」不是「数组」**：`{格式/note/source/… 共 14 个元数据键} + cards: [1351 张]`。
   直接 `for c in json.load(...)` 会静默扫到 **0 张卡**（我这轮踩了一次，差点把「全池只有 1 张 hasAlternativeAbility」
   读成「0 张」）。⇒ 工具里一律 `d["cards"]`（`cards_engine.json` 同理，但它有 `d.get("cards") or d` 的兼容写法）。
2. **`hasAlternativeAbility` 几乎没用**：全池 1,351 张里**只有 1 张 = 1**（`SW21`），其余全 0；
   我们的替代行动靠**卡面文本**（`HasAlternativeAbility` → `HasFx(keyword)`），与原版数据一致 ⇒ 别拿这一列当判据。
3. **`ShowWarlordActiveAbilityForTutorial` 的名字会骗人**：它的语义是**从卡数据恢复**（`+0x220 = 数据[+0x2a9]`），
   不是「显示」。配套地，`Hide` 才是「强制 0」。⇒ 将来谁读这两个方法名，别按字面猜。
4. **`set_hasActiveAbility` 这个公开 setter 全仓 0 调用点** —— 又一个「公开但没人用」的例子
   （与 `check_ledger_points` 那批「同名重载歧义」无关，纯记录）。
5. **账 `特效动画:486` 的 `状态` 我保持 C**：本次只接了 **`EvtKind.Ability` 这一族**的发侧；
   攻击/其它动画族的 priority 生产者仍未接（`ScheduleAttackPresentation` 恒 lane 0）。
   ⇒ 谁要把 486 判 A，请先核那一族（⛔ 别把这半刀当整条）。
