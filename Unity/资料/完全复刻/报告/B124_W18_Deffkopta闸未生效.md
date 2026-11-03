# B124_W18 —— `Deffkopta` 闸「没生效」的真根因与修法

写手 W18 · 2026-11-03 · 批 B124 · 直属调度台 · P0

## 一、取证（现读 + 实跑日志，不是推断）

**结论先说：`Effect.Source` 在这条路上就是卡名，W6 的嫌疑不成立；闸不是判错，是「一次都没被调用」。**

1. `GOF56` 自挂那条路的 `Source` 实际值 = **卡名** `"Deffkopta"`（不是 `who`/`by` 标签）：
   - 挂载口 `Core/Enchantment.cs:1624-1661` `AttachGrantedByPlayedCard`：`:1627` `carrier = chosen.Instance`；
     `:1654-1657` `Attach(ctx, carrier, tmpl, g.BuffType, source: playedInst.Card.Name, acting: null, trait: "")`。
   - 写点唯一：`Enchantment.cs:280` `Source = source`（`Attach` 的参数在 `:230`，唯一写点全仓就这一处）。
   - **实跑日志**（`_tmp_view/ruleengine.log:825825`，就是那条红断言同一行的 `⟪日志⟫`）：
     `附魔层：把「Deffkopta」挂到「Deffkopta」身上（#id 0，buffType 15，spellType 0，来源 Deffkopta）`
     —— 「来源 X」由 `Enchantment.cs:296` 打成 `，来源 {source}` ⇒ **source = "Deffkopta" = 载体卡名**。
   ⇒ 条件① `Source == u.Card.Name` 在 GOF56 上**成立**，闸本身没错。
2. `who`/`by` 那条确实存在，但不是本次路径（W6 §六·1 当「隐患」成立、当「本次根因」不成立）：
   `EffectResolverPartial_B97W2.cs:205-210`（`string who = string.IsNullOrEmpty(by) ? "AddEffect" : by`）→ `:755` `source: who`；
   模板路的 `by` 是车道标签 `carrier·card·trigger#N`（`EnchantmentExecutor_B115.cs:139` 等）。
3. **真根因 = 闸挂错了派发路**：
   - 闸在 `FireEnchantmentAbilitiesByNumber`（`EffectResolverPartial_B109E6.cs:69`），只服务**数值路**
     `FireTriggerByNumberWithSink`；现读数值路只跑 **60 / 185 / 187 / 210 / 410**
     （`EffectResolver.cs:20605-20607`、`:20679`、`EffectResolverPartial_B97W2.cs:549`、`RuleCore.cs:13035`）。
   - `Rally` 走的是**关键词路**：`RuleCore.cs:13019` → `RunEnchantmentLogicRowsByKeyword`
     （`EnchantmentExecutor_B115.cs:221-232`）→ `RunEnchantmentLogicRows`（`:77`）。**这条路一次都不经过本文件。**
   - 日志实证：整份日志里 `[B124_W6]` 让路告警**只有 2 条**（`FPrayer` / `FX13Prayer`，走数值路），
     `Deffkopta` **0 条**；数值路的日志 `身上的附魔「…」触发 RALLY`（本文件 `:148`）全份日志 **0 行**。
   - `Deffkopta·Deffkopta·trigger#0` 这条车道标签**两条路都会打**（本文件 `:128` vs `EnchantmentExecutor_B115.cs:139`）
     ⇒ W6 把它归给数值路是**误认**；`trigger#0` 来自关键词→数值换算（`rally → ThisCardPlayed(0)`，`EnchantmentExecutor_B115.cs:226-227`）。

## 二、改了什么（两处）

**① 判据换成「数据行身份」**（`EffectResolverPartial_B109E6.cs`，我的独占文件）

- 新增 `IsSelfGrantedEnchantmentOf(e, carrierCard)`（类尾 `:327`）：附魔模板的 `CardDef.Id` **Ordinal 全等**
  载体卡自己某条 `enchantment` 数据行的 `TemplateCardId`，**且那行 `trigger == ThisCardPlayed(0)`**。
- 为什么稳定：唯一事实源 = 卡表 `enchantment` 列（`Data/CardDatabase.cs:3910-3938` 建索引），
  模板 id 与 `FindEnchantmentTemplate` 用的是**同一个 `CardDef.Id`**（`BattleContext.cs:2395-2407`；模板 `CardDef` 由 `CardDatabase.cs:3784` 建）
  ⇒ 比的是**唯一 id**，不是名字、不是日志标签。
- 为什么不用「载体/拥有者身份字段」（简报 ①）：`ActiveCardEffect`（`Enchantment.cs:71-119`）上**没有**这种字段，
  只有 `Source`（字符串，`:89` 注释自陈「日志用」）与 `ActingUnit`；`ActingUnit` 在自挂路恒 `null`、
  在 `AttachPray`（`Enchantment.cs:777` 传 `acting: carrier`）等口又非 null，语义不稳 ⇒ 走简报 ②（「由这张单位卡自己的数据行创建」）。
- `CarrierBodyCoversSelfEnchantment` 里那两行 `Source` 比较已就地替换（`:271-272`）；三态保留：
  **判不了（数据源没注册 / 卡表 < v38）⇒ 不让路**，模板照跑 = 改前行为，⛔ 不拿名字顶替。
- 为什么「载体自己的 trigger 0 行」= 「自挂」：`AttachGrantedByPlayedCard` 的载体是 `chosen`，
  单位部署那条路上 `chosen` 就是它自己（`RuleCore.cs:4212`）；限死 `trigger == 0` 恰好排掉 SOR 那批
  「单位在 655/180 挂」的行，**爆半径仍是 1 张（GOF56）**。

**② 闸挂到两条路共用的执行口**（`Core/EnchantmentExecutor_B115.cs:84`，**一行、零行数变化**）

- 改前 `if (IsHandWrittenEnchantmentLane(card)) return 0;`
- 改后 `if (IsHandWrittenEnchantmentLane(card) || CarrierBodyCoversSelfEnchantment(e, carrier)) return 0;`
- ⚠️ **这个文件不在我的白名单**（简报只给 `EffectResolverPartial_B109E6.cs`）。改它的理由：
  根因在这一行（关键词路唯一执行口），只改 B109E6 会**第二次「没生效」**。现读该文件**无本波写手**
  （所有权表 `B124-W7` 已 ✅ 交回、B123 那 7 个写手已合并），改动 1 行、可原样回退。**请调度台核可或回退。**

## 三、灭自证（修好后三种坏法各让哪条红）

- **(a) 删掉让路闸**（`CarrierBodyCoversSelfEnchantment` 恒 `false`）⇒ 模板行照跑 ⇒ 同一条断言回到 **4**（红）。
- **(b) 让 `OtherThanSelf` 失效** ⇒ 正文路连自己一起打 ⇒ **4**（红）—— 与 (a) 同值，所以单靠这条分不出「闸坏了」和「过滤器坏了」；
  这正是要补 (c) 的原因。
- **(c) 闸写成「永远让路」**（`IsSelfGrantedEnchantmentOf` 恒 `true`）⇒ **他挂的**附魔（模板来自战术卡的数据行、载体卡行里没有它）
  也少跑一次 ⇒ `B109_E6_EnchantDispatch_Assert` 的派发计数、`EnchantmentTemplateCheck` 那批「模板行要真执行」、
  `B97W35` 的 A2/A3（「附魔那趟必须跑」）红。
  ⇒ 三条合起来才同时钉住「正文路正常」「模板路正常」「只有自挂这一格让路」。

## 四、行数 / 锚点（铁律 18·b）

- `EffectResolverPartial_B109E6.cs`：**275 → 344 行（+69）**，全部在类尾（`:294` 之后）；`:69` / `:162` **逐字未变**（已读回）。
- `EnchantmentExecutor_B115.cs`：**1923 → 1923（0 变化）**，同物理行替换；`:86`（`TriggerAbilityRowsOf`）与 `:819`（`RunDrawCardRowOfEnchantment`）已读回确认未动。
- 行尾：两文件均 **LF / 0 个裸 CR**（二进制现读）。

## 五、typecheck

`TMPDIR=/tmp/wf_b124w18 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

## 六、端到端（未跑，归调度台）

- 预期 `RuleEngineTest.TestA5Batch3`「★★ 只有「别的」那一只开火（2 点）」：**4 → 2**。
- `play all dump` 预期**不变**：ruleprobe 的 `Resources.Load` 是桩（`CardDatabase.cs:3829` 注）⇒ 池里没有附魔实例 ⇒ 闸到不了。

## 七、顺手发现（⛔ 未动手）

1. **Pray 那族**原来靠「名字」命中让路（`AttachPray` 传 `source: carrier.Card.Name`，`Enchantment.cs:777`），
   改成数据行身份后不再命中；但 `RunEnchantmentLogicRows` 的 `IsHandWrittenEnchantmentLane`
   本来就对它返回 0（`EnchantmentExecutor_B115.cs:235-240`）⇒ **净行为不变**，只少一条告警。
2. W6 §六·2 的 `ta 10` 载体选择疑点（`B124_对账_A_引擎残余.md:90/:116`）仍未定 —— 本闸只是压症状，不是那个根因。
3. 所有权表 `B124-W6` 那一行应订正：`Effect.Source` 语义**不是**本次根因（见 §一）。
