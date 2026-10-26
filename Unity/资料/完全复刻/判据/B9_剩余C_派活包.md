# B9 · 剩余 C 114 条 · 派活包（按目标文件切片）

> 判据代理 `C2`（第八十三会话「完全复刻」）· 2026-10-26 · **只读核**（未改 `.cs` / 主账表 / 正本；未跑 Unity；未动 git）
> 配套：逐条施工单 = `判据/B9_剩余C_施工单.tsv`（12 列 × 114 行）· 总表与判读 = `报告/B9_C2_剩余C施工单.md`
> 🔴 **稳定性**：下面的「账行号」是**快照**（主表正被多个代理并行改，行号会漂）；**写手一律先按「原版类 + 方法名」在 `账/战斗逻辑驱动.tsv` 里定位，再对照本包的行号**。

---

## §0 用法（三句话）

1. **一个包 = 一个目标文件（或一组同族小文件）= 一个写手**；包内按「先 P0 / 再 P1 / 最后 P2」的顺序做，做完一条就在施工单里勾掉。
2. **独占文件**（`RuleCore.cs` / `EffectResolver.cs` / `BattleDriver.cs`）**同一时刻只能一个写手**，其余包不得在它们未落地时改同一文件。
3. 每包做完先跑 **`bash 工具/typecheck.sh`**，再按「本包验收口」跑 `ruleprobe` / 对应宿主；⛔ 写手不跑全量 Unity 自检（主对话在同步点统一跑）。

---

## §1 包总表

| 包名 | 文件 | 条数 | 写手独占? | 前置依赖（哪些包必须先做） | 判据行区间（施工单账行号） | 本包验收口（摘要） |
|---|---|---|---|---|---|---|
| **P-01** | `RuleEngine/Core/RuleCore.cs` | **18**（含 2 条已改判待落：724→B、738→B；840→B 待并） | ✅ **独占** | 无 | 157·203·217·354·440·590·619·637·716·724·738·805·818·840·841·845·850·1373 | `ruleprobe play check` 逐条解释；事件流 210/270 各一次（场上+双方手牌）；血量写点后三档骷髅；洗回后费用回卡面 |
| **P-02** | `RuleEngine/Core/EffectResolver.cs` | **10** | ✅ **独占** | **P-01**（广播骨架 + 动作队列口） | 64·74·627·631·690·718·719·721·835·1287 | UM118 从 UnparsedEffects 移出；Draw 事件三跳集合；`ruleprobe ai` option 13 分解 |
| **P-03** | `RuleEngine/Core/Enchantment.cs` | **7** | ✅ 独占（与 P-02 有 2 条同链） | **P-02**（481/532 与 EffectResolver 摘附魔同链） | 481·532·687·763·770·1044·1047 | 摘附魔后该卡 anim cue=0；`HasEnchantmentWhileInPlay` 读数；`FindSecretOrder` 早退 null |
| **P-04** | `RuleEngine/Core/BattleContext.cs` | **5** | ✅ 独占（小） | 无（但**必须一次改完**随机源三条） | 195·375·1058·1072·1360 | 随机数组长 100 / 游标单调 / 同局两次一致；洗牌·随机部署·RandomizedOnResolution 逐族比 |
| **P-05** | `RuleEngine/Core/UnitState.cs` | **3** | ✅ 独占（改 `Has` 语义） | **P-01**（残骸化写点） | 853·938·1040 | 残骸 `Has(卡面 trait)`=false（改前 true）；buffType 90 授予=true；🔴 **翻回后 buff/附魔/标记被【清】**（`ResetToValuesInHand.c:45-70`；2026-10-26 订正，原文写"不变"方向反了） |
| **P-06** | `RuleEngine/Core/ActionPump.cs` + `CountEval.cs` | **2** | ✅ 独占 | **P-04**（676 的 ③ RandomChance）；669 无前置 | 669·676 | 676 四条灭自证断言 + maxTargets 截断 + playerTurn 出声；669 接线后 1128 主档逐字节等基线 |
| **P-07** | `RuleEngine/Core/CardDef.cs` + 数据生成器 | **2** | ✅ 独占（动数据列） | 无 | 993·1254 | SOR66 `Has(pray)`=true；`cards_truth` 新列全 0 |
| **P-08** | `RuleEngine/Data/AiScoring.cs` | **4** | ❌ 任意写手 | 无（四条都「只加函数不接线」） | 12·29·38·39 | `ruleprobe ai` 分解出现新项；`check` 指纹**不变** |
| **P-09** | `RuleEngine/Data/SimpleAI.cs` | **3** | ❌ 任意写手 | 无（只标 🔶 + 注释） | 1062·1063·1064 | `ruleprobe ai` 旋钮读数 = 配置值；`check` 不变 |
| **P-10** | `RuleEngine/Core/BoardSlots.cs` | **1** | ❌（与 P-09 同写手最佳） | **P-09**（同写手，避免两处改 AI 部署） | 1120 | AI 把新兵贴到「相邻效果」单位旁；有符号↔绝对格号断言 |
| **P-11** | `CardPresentation/Battle/BattleDriver.cs` | **28** | ✅ **独占（第二瓶颈）** | 逐条不同：596/914 无；669/676→P-06；481/532→P-03；461/464→P-12；375→P-04 | 47·46·53·54·88·91·94·101·102·115·236·238·239·259·304·401·450·483·596·782·819·884·914·936·940·973·1172·1181 | 真机逐项（见 §2·P-11）+ `ruleprobe play check` |
| **P-12** | `Battle/BattleBigChat.cs`＋`AbilityIconAnim.cs`＋`BattleSfx.cs`（3 个新/小件） | **6** | ✅ 独占（三文件同写手） | **P-06**（ActionPump 的 kind 21） | 294·295·460·461·464·489 | 大框出现/图标动画/音效串行，各一条断言 |
| **P-13** | `Core/CardFeel.cs`＋`Core/CardView.cs`＋`UnitTweenTable.cs`＋`Board/DeploySequence.cs` | **10**（含 856 待并 D） | ✅ 独占（视觉链） | **P-11**（回手/墓地链在 BattleDriver 侧） | 730·732·804·806·896·908·921·930·972·974 | `DissolveAmount` 1↔0；CreatedBy 行；秘密面；攻击档高亮族；抖位置 |
| **P-14** | `Hand/CardInteraction.cs`＋`Core/TutorialScript.cs` | **4** | ✅ 独占 | **P-11**（教程点击链在 BattleDriver） | 89·98·1157·1170 | 拖拽首帧无洞布局；换位不重复 SendCardReorder；教程指针跟手 |
| **P-15** | 战役线执行器（待建） | **4** | ✅ 独占 | **战役线整体**（未建） | 100·105·109·111 | 战役关卡逐条播完；游标单调；与教程互不干扰 |
| **P-16** | `Core/ScreenUtilities.cs`＋`Shell/FTUESelectDeckWindow.cs` | **2** | ❌ 任意 | P-16 的 FTUE 那条依赖教程线 | 48·443 | 移动档 y/横偏移；FTUE bot 池 |
| **P-17** | —（**不派活**） | **5** | — | — | 780·849·856·893·953 | 已改判 A/B/D（见报告 §7）；856 随 `_parts/B9_C2.tsv` 并入后生效 |

**合计**：16 个可派包 + 1 个不派活包 = 114 条（其中 5 条不派活 ⇒ **实际派活 109 条**）。

---

## §2 各包明细

### P-01 `RuleCore.cs` · 18 条 · ✅ 独占（引擎第一瓶颈）
- **序**：① 203/217（P0，先搭「广播口」两跳/三跳骨架，其余 5 条广播照抄）→ ② 716 CheckForSkullEarned / 850 RemoveCostEffects（P1，动数值）→ ③ 841 RecordPendingHealing（保持 C，按「粘性累计预测表」实现）→ ④ 其余 P2（157/354/440/590/619/637/805/818/845/1373）
- **跳过**：724 / 738（已 B）、840（待 B，补丁 `_parts/B9_C2.tsv` 合并后跳过）
- **验收口**：`bash 工具/ruleprobe.sh play check`（指纹变化**逐条**解释）；`play all dump` 只在该包相关局变化；210/270 两跳各断言「场上 1 次 + 双方手牌各 1 次（270 排除陷阱自身）」；`EarnSkull` 在 20/10/0 三档各一次；洗回后费用 = 卡面值。
- **风险**：动作队列顺序（157）与血量写点（716）会改指纹；⛔ 别顺手改 `Auras.Recompose` 的 9 个挂点。

### P-02 `EffectResolver.cs` · 10 条 · ✅ 独占
- **序**：① 64 AdjustCardEffect（动所有多目标 buff ⇒ 最后做、全池对拍）→ ② 627/631 广播 → ③ 74 GetRefCard + UM118 → ④ 690/718/719/721/835/1287
- **前置**：P-01 的广播骨架先落地（两处共用「预调 + 排除」写法）。
- **验收口**：`ruleprobe play` 里 UM118 从 `UnparsedEffects` 快照消失；Draw 事件「当前手牌 2 遍 / 对手 0 遍」；多目标 + addTrait/stackable 只剩 1 条实例且 `traitValue = change×N`。
- **风险**：🔴 64 动共用结算量 ⇒ 四颗指纹会变；⛔ 别碰已折好的 `PerCount` / buffMana 两条路。原版 `BroadcastCardDrawn` 第 4 跳漏 `!` 的 bug **照抄不修**。

### P-03 `Enchantment.cs` · 7 条 · ✅ 独占
- **序**：① 1047 HasSecretOrder（一行早退，最快）→ ② 1044 HasEnchantmentWhileInPlay（需数据列 ⇒ 与 P-07 对齐）→ ③ 481/532（摘附魔入队链，与 P-02 同批）→ ④ 687/763/770
- **验收口**：摘附魔后该卡待播 anim cue = 0；buffType 15/16 的 trait 帧特效消失；`CountSecretOrders==0` 时 `FindSecretOrder` 返回 null（改前返回同卡 effect）。

### P-04 `BattleContext.cs` · 5 条 · ✅ 独占（小但关键）
- **序**：① 375 + 1058 + 1072 **一次改完**（预烧 100 + 下标取 + 空数组返 0 + `max<=min+1` 返回 min）→ ② 195 TroopsPlayedThisTurn → ③ 1360 killer 字段
- **验收口**：数组长 100、游标单调、同局两次跑一致；洗牌/随机部署/RandomizedOnResolution 三族逐条比；`play check` 指纹变化**全部**能解释。
- **风险**：🔴 **全局随机源** ⇒ 所有对局指纹变、**旧录像全部失配**（要用户知情）；⛔ 别只改一处。

### P-05 `UnitState.cs` · 3 条 · ✅ 独占
- **序**：① 1040 HasCurrentTrait（残骸闸 + 与 `:1068` 同批）→ ② 938 TransformFromRemnant（原地翻回、保运行时状态）→ ③ 853 RemoveEffect（按记录摘 + 5/45/40 三档）
- **验收口**：残骸 `Has(卡面 trait)`=false（改前 true）、90 档授予=true；🔴 **翻回后 buff / 附魔 / 标记会被【清掉】**（判据 = `ResetToValuesInHand.c:45-70` 两次 `List.Clear()`：`activeEffects +0x108` / `activeEnchantments +0x110`；「变残骸」那一跳也调同一个复位，`…_ResolveTransformIntoRemnant_d__554__MoveNext.c:43`）—— **只保留** `companionCounter` / `tideCounter` / `orderInPlay` / `isPraying`；5/45/40 三档摘除后数值正确。
  🔴 **2026-10-26 就地订正（第八十四会话 · `E10TB` 实读推翻本节原验收口）**：原写「翻回后 buff/标记/教练数**与翻回前一致**（改前清零）」—— **方向反了**。原版是**清**，不是"保留"；按原来那句做会得到与原文相反的行为。
- **风险**：🔴 改 `Has` 语义 ⇒ AI/目标/光环全部读点受影响，必须全池对拍。

### P-06 `ActionPump.cs` + `CountEval.cs` · 2 条 · ✅ 独占
- **676 MeetsCriteria（P0）**：④ `maxTargets` 截断 → ⑤ `playerTurn` 缺 ctx/owner 时出声放行 → ⑥ **卡级** `conditionsFilter` 落进 `BoardGateSpec`；③ RandomChance(8) **等 P-04**。
- **669 BroadcastWhileInPlay**：把 `Aura.RefreshInPlayNumericAndKeywords` 挂到 `ActionPump.cs:118` 旁（接线口已备）；挂上后 `BL18`/`UM18` 事件电池各 +4 ⇒ **重钉基线必须调度台做**。
- **验收口**：676 的四条灭自证读数（board 条件 false/true/去掉 count 行 true/`cond=10` 区分）；`ignoreRandom:true` ⇒ true、false ⇒ null+gap；截断断言（3 目标 + maxTargets=1 ⇒ 只数 1）。

### P-07 `CardDef.cs` + 数据生成器 · 2 条 · ✅ 独占（动数据列）
- 993 GetTraitForActiveAbilityRawCard（六档表）+ 1254 IsCumulativeMutation（需先扩 `cards_truth` 的 `mutationType` 列）。
- **验收口**：SOR66 `Has(pray)`=true（其余 76 张不变）；`mutationType` 列全 0；`check` 指纹变化逐条解释（重生成卡表会动全池）。

### P-08 `AiScoring.cs` · 4 条 · ❌ 任意写手（最轻）
- 四条都**只加函数不接线**（原版 0 调用点）⇒ 指纹**必须不变**；每条配一条单测。
- **验收口**：`ruleprobe ai` 分解里出现 `defensive=` / hero+minion 两项；`bash 工具/ruleprobe.sh check` 指纹不变。

### P-09 `SimpleAI.cs` · 3 条 · ❌ 任意
- 只做「标 `🔶 自己设计` + 注释写清值无本地判据」；⛔ 别把自定值写成原版值。
- **验收口**：`ruleprobe ai` 三旋钮读数不变；`check` 不变。

### P-10 `BoardSlots.cs` · 1 条 · ❌（建议与 P-09 同写手）
- 1120 GetNextSlotCheckingAdjacents：改成**有符号格号** + 在 AI 部署处接线。
- **验收口**：有「相邻效果」单位在场时 AI 贴它放；`check` 指纹变化可解释。

### P-11 `BattleDriver.cs` · 28 条 · ✅ 独占（第二瓶颈）
- **先做无依赖的**：47 BotDecks（本地 103 副预组）· 450 OnApplicationQuit（联机）· 259 ClickExitBattle（回放退出）· 914 ShouldShowTargetsAvailable · 88 CheckIfWaitingToChangeDesiredAttack · 782 HeroLandIntoField · 401 HideHeroAtStart（需 P-13 的 UnDissolve）· 46/53/54 AI 三段延迟（🔶 自定值）
- **再做链上的**：238/239/483（按卡撤队列）· 236/304（执行顺序，与 28 成对）· 940/973/819/884/936 · 1172/1181 · 101/102/115/91/94（教程节奏，与 P-14 对齐）· 596（**只打 🔶 标记、别删**）
- **验收口**：真机逐项（AI 节奏抖动 / 回放退出不判负 / 督军溶解入场 / 墓地叠放顺序 / 手牌隐藏 / sentinel 三件）+ `ruleprobe play check`。

### P-12 三个表现小件 · 6 条 · ✅ 独占
- 294/295 DisplayBigChat（⚠️ **显示细节判据拿不到** ⇒ 版面参数按场景树，取不到标 🔶）· 460/489 图标动画（同层，参数在 `_ResolveAbilityIconAnim` MoveNext）· 461/464 音效 FIFO（入队 + 出队）。
- **验收口**：大框+头像出现；图标动画在结算前后各一次；**音效出队 = 每帧取一条**（`BattleManager__Update.c:152` 实读；≈16 ms 间隔、`PlayOneShot` 之间**仍可叠着响**），队列归 0。
  🔴 **2026-10-26 就地订正（第八十四会话）**：本行原写「3 条**串行**、间隔 ≈ **clip 时长**」—— **那句是我们自己编的**，原版就是 `Update` 每帧 `PlayNextSoundInQueue` 一行（`BattleManager__Update.c:152`）。⇒ 实现默认取 `GapPolicy.OnePerFrame`，串行档只留作可选项；验收口按「每帧一条」断。
  ⚠️ 另：`PlayIconAnim` / `ResolveAbilityIconAnim` 在原版**零调用点**（`decomp_full` + ILSpy 两棵树只命中自身定义）⇒ 图标动画这一路**原版从不触发**，只做形状与参数，⛔ **别发明触发源**。

### P-13 视觉链（CardFeel/CardView/UnitTweenTable/DeploySequence）· 10 条 · ✅ 独占
- **一条链**：730 Dissolve 复原支 → 972 UnDissolveWholeCard → 806 MoveBackToHand → 930 StopAnimsInPlay → 921 ShowCardBeingDrawn（弃牌/偷牌各一次，⚠️ 别统一方向）→ 974 UpdateAttackText（高亮族）→ 896 秘密面 → 908 CreatedBy 行 → 732 抖位置 → 804 orderInPlay 语义。
- **验收口**：`DissolveAmount` 1→0 且不停在半溶；CreatedBy 行出现/隐藏各一条；秘密面 ≠ 卡背；两档图标按规则高亮；`transform.localPosition` 抖动而 `localScale` 不变。
- **跳过**：856（待 D）。

### P-14 手牌与教程 · 4 条 · ✅ 独占
- 1170 HandCardMovedToNewIndex（早退 + **教程指针重算**）→ 1157 DisplayHandMinusCard（首帧无洞布局）→ 89/98（谓词无消费者 ⇒ 只写注释；98 需先裁口径）。
- **验收口**：同位置换位 `SendCardReorder` = 0 次（改前 1）；拖拽首帧 = 排除被拖卡的连续序；教程箭头跟到新位置。

### P-15 战役线执行器 · 4 条 · ✅ 独占（**依赖战役线整体**）
- 100 GetCurrentWaitTime / 105 HasPendingActions / 109 PlayNextScriptedAction / 111 PlayScriptedTurn。
- ⚠️ **同名 tutorial 重载（`TurnScriptedData`）已是 A，本条是 campaign 重载（`List<ScriptedActionCampaignData>`），仍要做**。
- **验收口**：战役关卡逐条播完、游标单调 +1、返回 false 即收尾；与教程回合互不干扰。

### P-16 环境件 · 2 条 · ❌ 任意（低优先）
- 443 MobileAdjPos（`VarsDevice` 等价件，与 102 横偏移同数据件）· 48 FTUE bot 池（依赖教程线）。
- **验收口**：移动档 y 偏移 = targetYOffset；FTUE 局对手牌组来自 FTUE 池。

### P-17 不派活 · 5 条
`HasWhileInPlayAdjacentEffect`(A) · `RemoveCardFromCemetery`(D) · `RemoveInHandEffects`(D，待并) · `SetCardState`(B) · `TriggerOtherCardSpiritStone`(A)

---

## §3 最小可交付包（MVP · 1 个写手串行，不必等整包）

> 目标：**P0 三条 + P1 里可达且不依赖别包**，一个写手 1.5–2 人日做完，调度台可**立刻**放一路人进去。
> 排除项：`55` 的 ③ RandomChance（依赖 P-04 随机源）· `99 HasCurrentTrait`（改 `Has` 语义、要全池对拍）· `54 BroadcastWhileInPlay`（要基线裁定）· `10 AdjustCardEffect`（全池影响）· `51 BroadcastCardDrawn`（动 Draw 消费面，留给 P-02 整包）。

| 序 | 包内序 | 账行号 | 方法 | 施工要点（一句话） | 验（最小） | 估时 |
|---|---|---|---|---|---|---|
| 1 | P-01 | 217 | `BroadcastTrapResolved` (P0) | 两跳：场上 `IsInPlay` + 双方手牌**排除陷阱自身** | `play` 出现 1 次 TrapResolved，陷阱卡自己不收、场上+对手手牌收；`check` 指纹（池内 0 监听）不变 | 45m |
| 2 | P-01 | 203 | `BroadcastCardCreated` (P0) | 两跳：场上 `IsInPlay` + 双方手牌**六开关全 1**（无闸） | 同上，事件流 1 次、场上+双方手牌各 1 次 | 60m |
| 3 | P-03 | 1047 | `HasSecretOrder` (P1) | 开头加 `if (CountSecretOrders(carrier) <= 0) return null;` | 夹具：activeEnchantments 空 + activeEffects 有同卡附魔型效果 ⇒ 返回 null | 20m |
| 4 | P-10 | 1120 | `GetNextSlotCheckingAdjacents` (P1) | 返回**有符号格号** + AI 部署处接线 | 有相邻效果单位在场 ⇒ 新兵贴它一格；`check` 变化可解释 | 3h |
| 5 | P-11 | 914 | `ShouldShowTargetsAvailable` (P1) | `ShowSkillPanel` 里把 `targetsAny` 真正算出来传进 `SetTargetsSuffix` | 两条反例（inPlay 真/假）读数不同；不传时该行不出现 | 2h |
| 6 | P-14 | 1170 | `HandCardMovedToNewIndex` (P1) | 早退两个分支 + 换位后调教程指针重算 | 同位置换位 `SendCardReorder`=0；教程箭头跟手 | 2h |
| 7 | P-11 | 450 | `OnApplicationQuit` (P1) | 联机局发 `SendForfeit`（幂等） | 两台机联机关窗口 ⇒ 对面立刻收场（改前等掉线超时） | 1h |
| 8 | P-06 | 676 | `MeetsCriteria` 的 ④⑤⑥ (P0 部分) | `maxTargets` 截断 · `playerTurn` 出声放行 · **卡级** `conditionsFilter` 落进 `BoardGateSpec` | 截断断言（3 目标 + max=1 ⇒ 数 1）；`cond=10` 疲劳/未疲劳读数不同 | 2.5h |

**MVP 验收口（合并）**：`bash 工具/typecheck.sh`（0/0）→ `bash 工具/ruleprobe.sh check`（指纹变化逐条解释）→ 事件流三条断言 → 真机：教程第 5 关点击链不卡死 + 联机退出立刻收场。

---

## §4 并发波次建议（并发上限 10，别贴满）

| 波次 | 可同时开工的包 | 说明 |
|---|---|---|
| **第 0 波（先放 1 路）** | **§3 MVP** | 不等任何人；1.5–2 人日 |
| **第 1 波（9 路）** | P-01（RuleCore）· P-11（BattleDriver）· P-13（视觉链）· P-04（随机源）· P-08（AiScoring）· P-09+P-10（同写手）· P-07（CardDef/数据）· P-12（表现小件）· P-14（手牌/教程） | 九路**文件互不相交**；P-12/P-14 的部分行要等 P-06/P-11，先把无依赖的做掉 |
| **第 2 波（3 路）** | P-02（EffectResolver，等 P-01）· P-06（ActionPump/CountEval，等 P-04）· P-05（UnitState，等 P-01） | P-02 与 P-01 **建议同一写手串行**（共享广播口/结算口） |
| **第 3 波（1 路）** | P-03（Enchantment，等 P-02）· P-15（战役线，等战役线整体）· P-16（低优先） | — |

**硬约束复述**：`RuleCore.cs`（18）· `EffectResolver.cs`（10）· `BattleDriver.cs`（28）三个文件**同一时刻各只能一个写手**；`RuleCore` 与 `EffectResolver` 共享广播/结算口 ⇒ **同写手串行**最稳（否则两处写同一条规则 ⇒ 迟早不一致）。

---

## §5 待并的账改动（本代理只写补丁，不碰主表）

`账/_parts/B9_C2.tsv`（11 列 × 4 行）：
- `ShouldUseEnviromentalEffects`（596）—— 保持 **C/P1**，**差异列改写**成「我们多做了一扇原版永不出现的选卡面板（默认关闭、仅自检用）⇒ 不是『我们缺』」；备注写明裁定与 `🔶 自己设计` 标记任务（归表现侧写手，本代理不改 `.cs`）。
- `RemoveInHandEffects`（856）—— **C→D**（`reflectedPrefab` 空 PPtr、原版从不实例化；时间限定 = 本 build）。
- `RecordPendingDamage`（840）—— **C→B**（5 个消费者全是内部战斗/AI 决策，无卡能力可观察）。
- `RecordPendingHealing`（841）—— **保持 C**，差异列改写（`damaged` 过滤器 27 行 + 原版「粘性累计预测表」语义；⛔ 施工要照抄「永不清理」）。
