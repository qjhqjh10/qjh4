# B124 断言债分诊 1（RuleEngineTest / CardBaseDemo / ShopScene）· 2026-11-03

> 只读分诊。判据优先级：① 原版反编译 `d:/2/tools/decomp_full/` ② prefab/字段 ③ 现读源码。
> 日志（现读）：`_tmp_view/ruleengine_b123c.log`（93 MB, mtime 11-03 07:41）· `_tmp_view/CardBaseDemo_b123.log`（11-03 07:57）· `_tmp_view/ShopScene_b123.log`（11-03 07:56）。
> ⚠️ **判失败必须按「行首前缀 + 归属断言类」分组**（`✗` 会出现在断言文案里，`grep -c ✗` 会数多）：
> RE 日志 22 行含 `✗` − 4 条非失败（1 条通过文案 + 3 条 `合计` 行）− 1 条**末段复述** = **17 条真失败**（与日志自述 `14802/14819` 一致）；
> CBD 20 行 = 6 条宿主 `Check` + `B99G1`(1) + `cardface-chk`(13)；SHOP 4 行 = 1 条真失败 + 3 条 `小结`。

## 〇 结论摘要

- **RuleEngineTest：17 红**（日志自述 17；`交接_第一百二十三会话` 里写的「12 红」是旧读数）→ **α 7 · β 2（1 条实锤 + 1 条待裁）· δ 8 · γ 0**；其中 **3 条已在 07:42 被修掉、只差复跑**（`B123_E1.E0` · `EngineE1` · `B121_D1B.C1`）。
- **CardBaseDemo：20 条 `✗` / 6 个宿主计数**（拖拽 4 + 倾摆 2）+ `B99G1` 1 + `cardface-chk` 13 → **α 18 · γ(带 δ 夹具) 2 · β 0**；**唯一真实现缺陷在 RE 侧**。
- **ShopScene：1 红 → α（卡池 1129→1142 旧常量），07:57 已改源码，只差复跑**。

## 一 按断言类分组的总表

| 组（断言类） | 宿主 | 条数 | 四类 | 证据（文件:行号） | 建议 |
|---|---|---|---|---|---|
| `RuleEngineTest.TestA5Batch3`（Deffkopta 双发） | RE | 1 | **β**（本批引入 = γ 性质） | 断言 `RuleEngine/Editor/RuleEngineTest.cs:8774`；日志 `ruleengine_b123c.log:825690` | **修实现**（§二·1） |
| `B118W11_Assert` C3 | RE | 1 | α | `RuleEngine/Editor/B118W11_Assert.cs:186`（闸在 `RuleEngine/Core/RuleCore.cs:18464`） | 改断言（`ta=0` 已合法） |
| `B118W11_Assert` C5 | RE | 1 | α | `B118W11_Assert.cs:175-176`（同上） | 改断言（`ta=0` 不看 `maxTargets`） |
| `B118W20_Assert` A3 | RE | 1 | α | `RuleEngine/Editor/B118W20_Assert.cs:102`（与 `:101` 逻辑互斥） | 改断言 |
| `B123_E1_Assert` E0 | RE | 1 | α（**已修**） | `RuleEngine/Editor/B123_E1_Assert.cs:330`（mtime 07:42:09） | 复跑确认 |
| `EngineE1Assert` E1（前提） | RE | 1 | α（**已修**） | `CardPresentation/Editor/RuleEngineTest_EngineE1.cs:80`（mtime 07:42:14） | 复跑确认 |
| `B121_D1_Assert` C3 | RE | 1 | **δ**（+β 次级） | 夹具 `CardPresentation/Editor/B121_D1_Assert.cs:108/99`；实现 `RuleEngine/Core/EffectResolver.cs:6844` | 改夹具（§二·7） |
| `B121_D1B_Assert` C1 | RE | 1 | α（**已修**） | `CardPresentation/Editor/B121_D1B_Assert.cs:140` | 复跑确认 |
| `B121_D1B_Assert` F185.3 / F236.3 / F238.3 | RE | 3 | **δ** | 夹具 `B121_D1B_Assert.cs:202`；实现 `RuleEngine/Core/EnchantmentExecutor_B115.cs:86-87` | 改夹具（trigger 0→`Trig`） |
| `B121_D3_Assert` A6 | RE | 1 | α | `CardPresentation/Editor/B121_D3_Assert.cs:203-205`；判据 `RuleEngine/Core/TargetCriteriaEval.cs:252` | 改断言 |
| `B121_D5_Assert` B1 | RE | 1 | **δ** | `CardPresentation/Editor/B121_D5_Assert.cs:104`；夹具 `:336`（`IncludeSelf=0`） | 改夹具 |
| `B121_D5_Assert` E3 | RE | 1 | **β（待裁）** | `B121_D5_Assert.cs:221`；实现 `RuleEngine/Core/RuleCore.cs:6599` | 先裁原版口径（§二·11） |
| `B121_D5_Assert` E6 | RE | 1 | **δ** | `B121_D5_Assert.cs:235`；夹具 `:336` | 改夹具 |
| `B121_D5_Assert` G70.2 / G100.2 | RE | 2 | **δ** | 夹具 `B121_D5_Assert.cs:282`（trigger 0） | 改夹具（trigger 0→`Trig`） |
| `CardBaseDemo` 拖拽·落位（2 条） | CBD | 2 | **γ（+δ 夹具）** | `CardPresentation/Editor/CardBaseDemo.cs:229-231`；闸 `CardPresentation/Hand/CardInteraction.cs:2349-2353` | 改夹具（§二·15） |
| `CardBaseDemo` 非法落点 / 回弹（2 条） | CBD | 2 | α | `CardBaseDemo.cs:246/251`；`CardInteraction.cs:1393`（`allowNearest: true`） | 改断言 |
| `CardBaseDemo` 倾摆角上界（1 条） | CBD | 1 | α | `CardBaseDemo.cs:580`；实现 `CardPresentation/Core/AutoCardRotation.cs:145-150` | 改断言（≤14.15°） |
| `CardBaseDemo` 倾摆绕 Z（1 条） | CBD | 1 | α | `CardBaseDemo.cs:584`；同一处 + `AutoCardRotation__GetRotation.c` | 删/改断言 |
| `B99G1_Assert` G2c | CBD | 1 | α | `CardPresentation/Editor/B99G1_Assert.cs:333`；带值 `CardView.cs:5577` | 改断言（战术档 2.015） |
| `CardFaceProbe` A1661 ⑥（7 位） | CBD | 7 | α | `CardPresentation/Editor/CardFaceProbe.cs:640`；徽标父节点 `CardView.cs:3915/15865` | 改断言（`Body3DBonesForTest`） |
| `CardFaceProbe` CF-6（5 条） | CBD | 5 | α | `CardFaceProbe.cs:1588/1615/1627/1644/1653`；带值 `CardView.cs:5571-5591` | 改期望（shipped 档） |
| `CardFaceProbe` CF-8（1 条） | CBD | 1 | α | `CardFaceProbe.cs:1756`；`CardView.cs:5573` | 改期望（0.1235） |
| `ShopScene` ①（前提）卡池张数 | SHOP | 1 | α（**已修**） | `CardPresentation/Editor/ShopScene.cs:6698`（mtime 07:57:52） | 复跑确认 |

**合计 38 条 `✗`**：α **26**（其中 4 条已修待复跑）· β **2**（1 实锤 + 1 待裁）· δ **8**（其中 2 条同时由 γ 触发）· 纯 γ **0**。
**真实现缺陷只有 1 条实锤（Deffkopta 双发）+ 1 条候选（`ca 236` 行动额度），且都在引擎侧。**

## 二 逐组详述

### 1（β · 最重要）Deffkopta 的 RALLY 伤害被结算两次：正文 op + 附魔数据行

- 现象（日志原文）：`RALLY：「Other friendly Deffkopta deal 2 damage to a random enemy」1 个单位各 2 点开火（合计 2 点）` + `Deffkopta·Deffkopta·trigger#0：chosenAbility 10（doDamage）⇒ 对 1/1 张各 2 点伤害` ⇒ 实得 **4**（期望 2）。
- 根因链（现读）：
  1. `GOF56 Deffkopta` 的卡数据 `abilityBuffs[1].buffType = 15`（singleUseEnchantment）⇒ 部署时 `EffectResolverPartial_B97W2.cs:755` 挂上一件同名附魔（日志「附魔层：把「Deffkopta」挂到「Deffkopta」身上 … buffType 15」来自 `Core/Enchantment.cs:293`）；
  2. 同一张卡的**正文**也被我们的文本层解析成 `eachunitdeal` op 并结算（`RuleEngine/Editor/RuleEngineTest.cs` 断言本身就在验这条解析）；
  3. 触发时 `EffectResolver.cs:21078` **无条件**先跑 `FireEnchantmentAbilitiesByNumber`（附魔数据行），再跑正文 op；
  4. `FireEnchantmentAbilitiesByNumber`（`EffectResolverPartial_B109E6.cs:129-131`）的双发闸只比 **附魔卡自己的** `TriggerOpsByNumber`（`e.Card`），而附魔模板卡的正文**恒空** ⇒ 闸放行 ⇒ `ca 10` 数据行也打 2 点。
- 判定：**β（真缺陷）**，且是**本批引入的回归**（`RunDoDamageRowOfEnchantment` 于 `995da8d5` 2026-11-03 02:59 才落地；2026-11-02 15:24 全绿基线时附魔 `ca 10` 还只「出声不结算」⇒ 当时不会双发）。玩家可见后果：**带自挂附魔的卡，其触发效果打两遍**。
- 改法（最小、可断）：
  - 位置 `RuleEngine/Core/EffectResolverPartial_B109E6.cs:129`，把闸从「只看附魔卡正文」扩成「附魔卡正文 ∨ **载体自己那张卡的正文**」：
    ```csharp
    var eops = e.Card.TriggerOpsByNumber(triggerNumber);
    var uops = u.Card != null ? u.Card.TriggerOpsByNumber(triggerNumber) : null;
    bool carrierTextRan = uops != null && uops.Count > 0
                       && e.Effect != null && e.Effect.Source == (u.Card.Name ?? "");
    if (eops == null || eops.Count == 0) { if (!carrierTextRan) RunEnchantmentLogicRows(...); continue; }
    ```
  - 断言的灭自证已经在了（`RuleEngineTest.cs:8774`：漏 `OtherThanSelf` 会是 4、双发也是 4）⇒ 改完必须复跑 `RuleEngineTest.Run` 看这一条转绿、且 `B118W11` / `B121_*` 那批不因此变化。
  - ⚠️ **这是口径级改动**（「正文 op 已跑 ⇒ 数据行让开」的适用范围）：落手前请在账里写清「来源 = 载体自己那张卡的 abilityBuff」这一限定，别一刀切成「只要有正文就不跑附魔」。

### 2 `B118W11_Assert` C3 / C5（α，2 条）

- 现象：`C3 接住的那些全是多目标档（ta ∈ {10,110,140,150}）；越界 7 处` / `C5 只有截断行的 trigger（实测 176 处）一条都没接（实得 2 处）`。
- 根因：`RuleCore.cs:18464` 已在 **2026-11-03（`B121_D3` · 账 `b121-d3-ta0`）** 把 `ta = 0 any` 加进「可接」集合，并且对 `ta=0` **不看 `maxTargets`**（原版 `AbilityLogic__GetTargets.c:884-891` 建完池直接 `goto` 返回）。断言仍钉「只有 10/110/140/150」的旧闸 ⇒ 7 条 `ta=0` 被算成越界、2 条 `ta=0 ∧ maxTargets>=1` 被算成「截断档被接」。
- 改法（**改断言，⛔ 别改实现**）：
  - `B118W11_Assert.cs:175` → `if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) continue;`
  - `B118W11_Assert.cs:176` → `if (ta != 0 && set.MaxTargets >= 1) hasTrunc = true; else hasMultiNoTrunc = true;`（`ta=0` 恒算「多目标不截断」）
  - `:186` `badTa` 判据同样加 `&& gta != 0`；`:187` `badMax` 加 `gta != 0`（否则 `ta=0` 的 `maxTargets` 会被判违规）。
  - 文案里「`ta ∈ {10,110,140,150}`」改成「`ta ∈ {0,10,110,140,150}`，且 `ta=0` 不看 `maxTargets`」，并注明判据 `.c:884-891`。
  - 附带：`B118W11_Assert.cs:20` 的头注释同步（铁律 5）。

### 3 `B118W20_Assert` A3（α，1 条）

- 现象：`A3 非永久那条照旧被摘` 红，而同类 **A2 绿**。
- 根因：夹具有两条 `TempBuff`（permanent / 非永久），`RemoveAllAppliedEffectsForJam()` 之后只剩 1 条。
  `:101` 断言「剩的那条是 permanent」，`:102` 断言「剩的那条**不是** permanent」——**两条在只有 1 条存活时逻辑互斥，必有一红**。
- 改法：把 `:102` 改成「被摘掉的确实是**非永久**那条」，即加一条独立读口（例如在移除前记下非永久那条的对象引用，断言 `!u.TempBuffs.Contains(nonPermanent)`），或直接删掉 `:102`（`:101` + `A3b` 的数值断言已覆盖「摘了对的那条」）。⛔ 不要做成「Count==0」。

### 4 `B123_E1_Assert` E0（α · **已修**）

- 会话 123 首跑前，现池 18 张 `ca 50` 里**没有一张**把 `ca 50` 行与正文 op 挂在同一 trigger ⇒ 「双发结构上不可达」，旧断言「池里应当有一张」前提不成立。
- 现读 `B123_E1_Assert.cs:330` 已改成护栏式 `CheckTrue(dfCard == null, "E0 前提：池里**没有**…")`（mtime 07:42:09，提交 `ced4ebce`）。**复跑确认即可**。

### 5 `EngineE1Assert` E1 前提（α · **已修**）

- 旧常量 1129；现池 1142（v49 加 13 张 `TRANSFORM_*` 变身战术卡）。`RuleEngineTest_EngineE1.cs:80` 已改 1142（mtime 07:42:14）。**复跑确认**；⚠️ 其余「1129」口径请一并核（见 §九·3）。

### 6 `B121_D1B_Assert` C1（α · **已修**）

- `ca 10` 返回的是**实际结算目标数**，夹具敌方 3 个 ⇒ 实得 3；旧断言钉死 `== 1`。现已改 `CheckTrue(n >= 1, … "夹具里敌方 3 个 ⇒ 现读 " + n + ")`（`:140`）。**复跑确认**。
- ⚠️ 这条改弱了「灭自证」的强度（≥1 仍能挡住「执行器整跳删除」），可接受；若要更强，可钉 `n == 3`（= 夹具的敌方单位数）。

### 7 `B121_D1_Assert` C3（δ 夹具 · 附带 β 次级发现）

- 现象：`ca 140 recallToOwnerHand` 的 `C1`（返回 1）、`C2`（手牌 +1）、`C4`（载体没动）都绿，只有 `C3 它已离开棋盘` 红。
- 根因（现读）：夹具 `PlaceUnit(ctx, 0, 4, buddyCard)`（`:99`，`PlaceUnit` 见 `:221`）把「旁边那张」放在 **slot 4 = `BoardSpec.WarlordSlot`**（`RuleEngine/Core/BoardSpec.cs:30`，`IsValid` 明确排除它）。回手执行体走 `DoReturn` → `BoardSlots.RemoveAt(ps, 4)`（`Core/BoardSlots.cs:362-365`）**遇督军格直接 `return null`、不清格**，而调用方（`EffectResolver.cs:6844-6850`）**照样**把卡加进手牌 ⇒ 棋盘与手牌各一份。
- 判定：**δ（夹具摆错格）** ⇒ 把 buddy 放到 **slot 5**（右贴督军格）或 2 即可。
- **β 次级发现（必须记账）**：`DoReturn` 在「`FindUnit` 找到、但 `RemoveAt` 没真删掉」时会**静默**制造重复卡。建议在 `EffectResolver.cs:6844`（`BoardSlots.RemoveAt`）与 `:6850`（`AddCardToHandWithinLimit`）之间加一句「删没删掉」的硬检查（`RemoveAt` 返回 null ⇒ 出声 + `continue`，⛔ 不 AddCardToHand）。今天只有违规棋盘（督军格/有洞）能触发，但这是「静默错打」的同一族形状。

### 8 `B121_D1B_Assert` F185.3 / F236.3 / F238.3（δ，3 条）

- 现象：F 组三条 `.1`（返回 0）与 `.2`（目标生命没动）**都绿**，只有 `.3`（「不静默：出声计数涨了」）红 —— 典型的「这一行根本没被执行」。
- 根因：`B121_D1B_Assert.cs:202` 构造夹具时第二个实参（trigger）传的是 **`0`**，而 `RunEnchantmentLogicRowsForTest(..., Trig = 640, ...)` 按 640 取行 ⇒ `TriggerAbilityRowsOf(640)` 返回空数组 ⇒ `EnchantmentExecutor_B115.cs:86-87` 直接 `return 0`，**根本不进 `IsKnownChosenAbilityWithoutPayload` 那条出声支**。
- 证据：同一文件的 A/B/C/D/E 组在本批已各自把第二个实参从 `0` 改成 `Trig`（提交 `ced4ebce` 的 diff，`:93/:108/:129/:155/:174`），**唯独 F 组漏改** ⇒ 这不是判据问题，是改漏。
- 改法：`B121_D1B_Assert.cs:202` → `Template("B121D1BTplGap" + ca, Trig, ca: ca, …)`。改完这三条 `.3` 应该转绿，`.1/.2` 仍绿（`ca 185/236/238` 没有执行体 ⇒ 返回 0）。

### 9 `B121_D3_Assert` A6（α，1 条）

- 现象：`反证：ta=10 allFiltered 仍按 friendlyUnits/enemyUnits 建池（只己方 ⇒ 4 张，实得 0:2,0:4,0:5）` —— 实得 3（缺 `0:3` = `src`）。
- 根因：夹具 `Set(10, 1, 0, 1, 1, 0)` 的 `IncludeSelf = 0`（`B121_D3_Assert.cs:100` 的 `Set()` 把 `IncludeSelf` 写 0），而被排除的正是**施放者自己**。判据链：`FilterMethods__CheckIfMeetsCriteria.c:232-238` 的 `if (candidate == thisCard && *(byte*)(param_4 + 0x1d) == 0) return 0;` ⇔ `TargetCriteriaEval.cs:252` `if (self && set.IncludeSelf == 0) return false;` ⇒ **原版也会把 src 剔掉**。⇒ 期望 4 是错的。
- 改法（二选一，推荐前者）：① 断言改成 `C(…, 3, "只己方部队 2/5 + 督军 4 = 3（施放者自己被 includeSelf=0 剔掉）…")`；② 或夹具加 `s.IncludeSelf = 1` 后再期望 4（但那样就不再覆盖「默认 36 格」这一档）。⛔ 别改 `TargetCriteriaEval`。

### 10 `B121_D5_Assert` B1（δ，1 条）

- 现象：`ca 410（ta=10）登记了 1 条 codex 触发`，实得 0。B2（没走缺口出声）绿。
- 根因：夹具 `B121_D5_Assert.cs:86-88` 的 criteria = `FriendlyUnits=1 ∧ IncludeMinions=1`，而我方**只有载体一个单位**；`Criteria()`（`:336`）把 `IncludeSelf` 写 **0** ⇒ 唯一候选被 `TargetCriteriaEval.cs:252` 剔掉 ⇒ `targets.Count == 0` ⇒ `RunTriggerCodexRowOfEnchantment` 在 `:748` 就 `return 0`。
- 改法：两处任选 —— ① 在 `:88` 后加 `crit.IncludeSelf = 1;`（对应原版真卡 `Duty's End` 的 `includeSelf` 取值需现核，别想当然）；② 或再摆一个己方单位（`PlaceUnit(ctx, 0, 2, …)`）让池里有两个候选。

### 11 `B121_D5_Assert` E3（β 候选，1 条 · **待裁**）

- 现象：`ca 236 不碰行动额度（原版 AddRedirectedAttack 里 RemoveAttackThisTurn 0 命中）`，期望 0，实得 1。
- 现读：`DeclareScriptedAttack236`（`EnchantmentExecutor_B115.cs:1445`）走 `DeclareAttackCore(..., forced: true)`；`RuleCore.cs:6599` 的 `attacker.AttacksThisTurn++` 是**无条件**的（只有「被取消」那两条早退例外）⇒ 实得 1 是这条实现的必然结果。
- 判据现状：断言引的 `RemoveAttackThisTurn` **0 命中**只能证明「没有退还」，**证明不了「不计数」**。原版 `_ResolveForceAttack_d__504__MoveNext.c` 全文 122 行、**没有 `+0x48` 写点**（`grep 0x48` 0 命中），但它把活派给 `AddScriptedAttack` → 排一条 `BattleAction(0x34)`，真正落账在哪要追 `ResolveAction.c:690` 的 `case 0x34` —— 本波**未追到底**，如实标「待裁」。
- 二裁办法（可离线）：用 `ruleprobe` 摆一张 `Duty's End`（或任何带 `ca 236` 附魔的卡），打一记后读 `UnitState.AttacksThisTurn` / `Exhausted`，与「同一单位正常攻击一次」对照；或以原版 `.c` 追到 `case 0x34` 的落账跳。
- 若判「原版不计数」⇒ **β**：给 `DeclareAttackCore` 加 `countsAsAction: bool = true` 形参，`ca 236` 传 false（238 那档继续用预扣+退还）。
- 若判「原版计数」⇒ **α**：`:221` 改成 `Check(opp.AttacksThisTurn, 1, …)`，并把文案改成「只打一次、计一次（无退还）」。

### 12 `B121_D5_Assert` E6（δ，1 条）

- 同 §10 的夹具原因：`:216` 的 criteria `FriendlyUnits=1 ∧ IncludeMinions=1` + `IncludeSelf=0` ⇒ 候选 0 个 ⇒ `RunScriptedAttackRowOfEnchantment` 在 `:1396` 就 `return 0`，**走不到** `:1399-1403`「攻方为 null ⇒ 出声」那一支。
- 改法同 §10（`crit.IncludeSelf = 1` 或补一个己方单位）。改完 E5 仍应为 0、E6 应转绿。

### 13 `B121_D5_Assert` G70.2 / G100.2（δ，2 条）

- 同 §8：`:282` 的 `Template("B121D5TplGap" + ca, **0**, …)` 传 trigger 0，而入口传 `Trig = 640` ⇒ 行取不到 ⇒ 出声计数不涨。两条 `.1`（返回 0）因此也是「空转绿」。
- 改法：`:282` 第二个实参 `0` → `Trig`。

### 14 `ShopScene` ① 前提（α · **已修**）

- 旧常量 1129，现池 1142。`ShopScene.cs:6698` 已改（mtime 07:57:52）。**复跑 `ShopScene.Run` 预期 0 红**；⚠️ 同一条链上 `★①（前提）cantBeRandomlyCreated = 168` 是**独立**常量，这次没动 —— 复跑时盯一眼它是否仍绿。

### 15 `CardBaseDemo` 拖拽·落位（γ + δ 夹具，2 条）

- 现象：`松手后落在槽 3 上（到槽位距离 3.527）` + `Placed[3] 登记的是 <空>`；日志 `CardBaseDemo_b123.log:616282` 的 `Release：指针世界 (-1.38,-0.89) … 落点判定=**MissedSlot**`。
- 根因链（现读）：
  1. `CardInteraction.PlayCommitGate`（`CardInteraction.cs:2349-2353`）是 **2026-11-03 00:34（`60c211b3` · `B120-W16`）** 新加的：松手必须满足「指针比**按下点**抬高 ≥ `PlayLiftWorld`（0.3 原版世界单位）」；
  2. 夹具在 `SimulatePress(hoverCard.transform.position)`（`CardBaseDemo.cs:201`）时喂的是**悬停抬起后**的卡位（y ≈ −0.88，日志「抬起来了：y −3.79 → −0.88」），而目标槽位 y ≈ −0.89 ⇒ 抬高 ≈ 0 ⇒ 闸不过；
  3. 悬停抬起量本身在 **2026-11-02 20:53（`47a4ccee`，会话 119）** 被放大过（提交自述「悬停抬起量单位错 14.835×」）⇒ 旧夹具的「从手牌高度起拖」这个隐含前提失效。
- 判定：**δ（夹具把「卡的位置」当成「指针位置」）**，由 γ（新 commit 闸 + 放大后的抬起量）触发。
- 改法（改夹具，⛔ 别为了让断言绿去动 `PlayCommitGate`）：把 press 的世界点换成**手牌行上那个点**（例如 `handLayout.SlotPosition(...)` 或 `hoverCard` 的未抬起 home），并注释「press 的是**指针**，不是卡；卡已被悬停抬起 2.91 单位」。
- ⚠️ **同时必须记一条真机风险**（见 §九·1）：若真机上「悬停抬起」也把指针抬到棋盘线附近，玩家会**拖不出去牌** —— `wf_click` 进不了拖拽态，这一条**只能真人验**。

### 16 `CardBaseDemo` 非法落点 / 回弹（α，2 条）

- 现象：`非法落点没被算成合法（台面上的牌 0 → 1）` + `弹回手牌第 0 位（距离 4.612）`；日志 `:616343` 反而打印 `提交（还按着就 commit）… 落点判定=**Ok**`，指针在 (−6.76, 3.80)。
- 根因：`allowNearest: true`（`CardInteraction.cs:1393`，同样 `60c211b3` / `B120-W24`）实现的是原版 `GetClosestAvailableSlot`：「指针在棋盘上方任意处 ⇒ 取最近可用格」，**不再要求压在某一格上**。夹具里那个「离任何格位都远」的点（`CardBaseDemo.cs:241`）因此**不再是非法落点**。
- 改法：把「非法」改成真正会被拒的一档 —— ① 指针**留在手牌限位线以下**（不越 `BoardLayout.HandLimitY`）⇒ `PlayCommitGate` 第二道会挡；或 ② 两侧全满（`IsAvailableSlot == false`）⇒ 候选集空。断言文案里写清是哪一档（原版判据 `BattleManager__Update.c:249-288`）。

### 17 `CardBaseDemo` 倾摆角上界（α，1 条）

- 现象：`快拖 20 帧后卡面真的歪了：13.76°（上限 10°）`。
- 根因：10° 是**每轴**上限（`AutoCardRotation.MaxXAngle/MaxYAngle = 10`，153/153 prefab 实例），而原版/我们的组合是 `GetRotation(dx,10,down) * GetRotation(dy,10,right)`（`AutoCardRotation.cs:145-150`，判据 `AutoCardRotation__GetRotation.c`）—— **两个 10° 正交轴旋转的合成角可达 2·acos(cos5°·cos5°) ≈ 14.12°**。断言拿「单轴上限」当「合成角上限」⇒ 必红。
- 改法：`CardBaseDemo.cs:580` 上界改成 `<= 14.15f`（并在文案里写清「10° 是每轴上限、合成可达 14.12°」），或直接拿 `TargetRotationFor(dx,dy,10,10)` 的解析角做期望。⛔ 别把 `maxX/maxY` 改小。

### 18 `CardBaseDemo` 倾摆绕 Z（α，1 条）

- 现象：`倾摆没有绕 Z 转` 红。根因同 §17：`AngleAxis(a, -Y) * AngleAxis(b, X)` 的四元数 `z = sin(a/2)·sin(b/2) ≠ 0`（a、b 都非零时），欧拉分解必然出 Z 分量 —— **原版那两行相乘本身就是这个结果**。
- 改法：删掉这条（或改成「Z 残角 = 解析合成的 z 分量 ±0.01」）。原注释「原版那两个轴只出 X/Y 的倾斜」是**推断、不是判据**，按 `.c` 订正（铁律 5）。

### 19 `B99G1_Assert` G2c（α，1 条）

- 现象：`战术卡卡名 fs 2.0150 ≤ 1.5950` 红。实得 2.0150 **正好等于** `CardView.NameTacticBandMax = 2.015f`（`CardView.cs:5577`）。
- 根因：`:333` 用的是 `v.NameBandMaxForTest`，而它的实现是 `NameUnitBandMaxForUse`（= **单位卡**档 `NameUnitBandMax = 1.595`，`CardView.cs:15729/5575`）。夹具是**战术卡** ⇒ 用错了档。
- 改法：给 `CardView` 加一个战术档读口（`NameTacticBandMaxForTest` → `NameTacticBandMax`）并在这里用它；文案里的「24 ÷10 × 0.75 ⇒ 1.80」也是旧口径，改成 2.015（`B117_F14` 的 shipped 步进见 `CardView.cs:5571-5591`）。

### 20 `CardFaceProbe` A1661 ⑥（α，7 条）

- 现象：`第 {i} 位的层没建出来` ×7；同时 `A1661 前提：7 枚徽标都建出来了` 与 ①②③④⑤ **全绿**。
- 根因：`:640` 用 `bv.FaceLayerRoot.Find("badgePlate" + i)` / `badgeIcon`，而徽标自 **2026-11-02 09:32（`fd26e3f3` · `B117-R22`）** 起挂在 **`Board Elements/3DBody`**（`CardView.cs:3915` 底板 `parent: Board3DBones()`、`:3987` 图标 `SetParent(Board3DBones())`、`:15859-15868` 建节点；自检读口 = `Body3DBonesForTest`）。且 `Transform.Find` **只找直接子件** ⇒ 恒 null。
- 改法：把 `FaceLayerRoot.Find` 换成 `bv.Body3DBonesForTest` 查找（先判 null 并出声），或让 `CardView` 暴露 `BadgeRootForTest`。文案同步「徽标在 `3DBody/TraitIcons`，⛔ 不在 `2DCard`」（`CardView.cs:3930-3935` 已有该口径）。

### 21 `CardFaceProbe` CF-6 / CF-8（α，6 条）

- 现象（实测 vs 期望，全部是 em = `fontSize × 0.1`）：option0 卡名 0.1595(期望0.19)、效果 0.1560(0.2355)；态5 0.1690(0.26)；态6 0.2015(0.24)；态7 0.2015(0.24)；态8 0.1690(0.26)；CF-8 0.1235(0.19)。
- 根因：**每一个实测值都精确等于当前的 shipped 上界常量**：`NameUnitBandMax 1.595`→0.1595、`DescUnitBandMax 1.560`→0.1560、`DescUnitBigBandMax 1.690`→0.1690、`NameUnitBigBandMax/NameTacticBigBandMax 2.015`→0.2015、`DescTacticBandMax 1.235`→0.1235（`CardView.cs:5571-5591`）。这些上界是 **`B117_F14`/`UI-21`（shipped 实拍 ×0.78）** 与 **`B117_WCARD`（再 ×0.8333）** 两次裁定后的新值；CF-6/CF-8 写于 2026-10-25，钉的仍是 prefab 名义值。
- 改法：把期望改成 shipped 档的**字面量**（0.1595 / 0.1560 / 0.1690 / 0.2015 / 0.1235），文案写清「prefab 名义 em = 0.19/0.2355/0.24/0.26，shipped 两刀 = ×0.78 ×0.8333 ⇒ 上界 X；判据 `CardView.cs:5571-5591` + `B117_F14`/`B117_WCARD`」。⛔ **别从 `CardView` 的常量读期望**（那是自证），要写字面量。
- ⚠️ 这 6 条是本批最容易被误判成 β 的一组：它**不是**字号实现错，是期望值停在旧口径。

## 九 顺手发现

1. 🟠 **`PlayCommitGate` × 悬停抬起的真机风险（未验）**：悬停抬起 2.91 世界单位（≈314 px）后，玩家按下那一刻的指针 y 已经贴近棋盘线；而 commit 闸要求「比按下点再抬高 0.3 原版世界单位 + 越过 `HandLimitY`」（`CardInteraction.cs:2349-2353`）。CBD 夹具已经因此被挡（`落点判定=MissedSlot`）。**`wf_click` 进不了拖拽态**（`交接_第一百二十三会话:95`）⇒ 这一条目前**只能真人验**，建议进 `真Play待验清单`，优先级高（挡出牌）。
2. 🔴 **`B121_D1B` / `B121_D5` 的「gap 组」夹具换 trigger 时改漏了两处**（`B121_D1B_Assert.cs:202`、`B121_D5_Assert.cs:282`）。同族风险：这两个文件里若还有别处 `Template(..., 0, …)`，一并核（本波 grep 只余这两处）。这类错**不会报错**，只会让 `.1/.2` 空转绿。
3. 🟡 **「1129」这个卡池常量在全仓可能还有别的副本**：本波已修 `RuleEngineTest_EngineE1.cs:80`、`ShopScene.cs:6698`；请再 `grep -rn "1129" Assets/**/Editor/*Assert*.cs`，凡是「(前提) 卡池张数」口径的一起升到 1142（⛔ 别改「某类卡 1129 张」那类语义不同的行）。
4. 🟡 **`DoReturn` 的「删没删掉」无自检**（`EffectResolver.cs:6850`）：`BoardSlots.RemoveAt` 在**督军格 / 有洞棋盘**上是 no-op，但调用方照样 `AddCardToHandWithinLimit` ⇒ 静默造出「手里一份、场上一份」。建议加硬检查 + 出声（与 §二·7 同一条）。
5. 🟢 **`B121_D1_Assert` 夹具把单位摆进督军格**（`:99`，`Board[4]`）—— 这会**覆盖掉玩家 0 的督军对象**，本组的其它断言今天恰好没读到督军才没出问题。建议这一族夹具统一用 `PlaceUnit(..., 5, ...)`，并在 `PlaceUnit` 里加一句 `if (slot == BoardSpec.WarlordSlot) 出声`。
6. 🟢 **`B118W11` 的 C3/C5 与 `B121_D3` 的 ta=0 接线存在「断言滞后一周」的固定模式**：引擎侧改了 `ta` 闸，只读类断言没跟着改。建议这类「闸表」在断言里改成**从一次集中定义的清单校验**（例如把「本版接哪些 ta」放进一个 `RuleCore.AcceptedTaForTest` 并让断言与实现共用同一份 —— 注意这属于「同源」风险，需要在两条断言上加反证条）。
7. 🟢 **RE 日志把失败行在末尾**又复述了一遍**（`RuleEngineTest.Run` 的收尾段），所以 `✗` 的原始行数 ≠ 失败数。后续统计请按「断言类 + 唯一文案」去重。
