# B141 · W128 · 对账 · 引擎 `EffectResolver` 四条

> 只读对账代理 `R3` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 唯一产出 = 本文件。⛔ 未改任何源码/数据/账/正本；⛔ 未跑 Unity / `ruleprobe`；⛔ 未动 git。
> 口径：行号**全部本次现读**（`RuleEngine/Core/**` · `RuleEngine/Resources/*.json` · `d:/2/tools/decomp_full/` · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`）。
> ⚠️ 账上 `EffectResolver.cs:836-843` / `:12409/12412/12423` 这两组行号**已漂**（现读分别在 `:840` 与 `DoGive` 体内），本文一律给现读值。

---

## 〇 一句话结论

| ID | 判定 | 一句话 |
|---|---|---|
| **ENG-11** | `⛔判据本身错`（施工建议错）＋真残差 12 行 | 原版确实有通用解释器，但我们的等价物是**正文 op**（不是第二张 `ca` 表）；照账上「按 ca 走同一张分派表」做会把 1,400+ 已由正文结算的被动行**打成双发**。真缺口 = 被动触发上 12 行 meta-op（9×320 · 2×340 · 1×480）：340 已有等价物、320 里 2 张已跑 / 7 张没跑、480 缺 1 行 |
| **ENG-12** | 功能 `✅已做完待核销`；账上改法 `⛔判据本身错` | TL20 三选一**已通**（正文 `chooseone` + `choice_option_cards.json` 旁表 + 结构化执行器，B116_W32 A5 读数 `Carnifex 4→6`）。`OathChoiceNeeded` 的 `Count != 2` **只服务誓约卡**（池内 10 张誓约卡全是 2 项），TL20 没有 `oath` trait，**根本没经过这道闸** |
| **ENG-24** | `🟠真没做`（判据这次查全了） | 26 档 = `ChooseValueOptions` 枚举 26 个值；我们**没有**成表分派，散在 4 处 + 正文 AmountRef；可达缺口 = `201`（2 卡 buff 路）· `17`（5 卡）· 以及原版**「未命中档 ⇒ 返回 min」**这条兜底 |
| **B140-WN-DOGIVE-EXTRINSIC** | `🟠真没做`（今天可达 0） | `ResolvingExtrinsic` 唯一写点 `RuleCore.cs:16155` 由 `HandEffect.Extrinsic` 驱动，而后者**无生产者**；原版真身 = `PlayAbility.c:1197` `= abilityData.whileInPlay(+0x40) ‖ whileInPlayVariable(+0x41)`；但 20/240 的正文 op 今天**没有 `FxOps` 键**（效果由 Aura 层承载并已标 extrinsic）⇒ 净可观测 = 0 |

---

## 一 ENG-11 · 通用 `chosenAbility` 执行器

**判定：`⛔判据本身错`（「按 ca 走 `RunActiveRowOpsOrGrantedTrigger` 同一张分派表」会双发）＋真残差 12 行**

### 1 现读证据（我们的分派面，三处）

| # | 文件:行号 | 摘录 / 事实 |
|---|---|---|
| O1 | `EffectResolver.cs:21368-21374` | `if (ca < 0) return Unknown; if (ca == 320) return FollowUp; if (ca == 325 \|\| ca == 340 \|\| ca == 380 \|\| ca == 480) return OwnLane; return BodyOp;` |
| O2 | `EffectResolver.cs:21396-21407` | `switch (TriggerRowKindOf(r.Ca))`：`Unknown/OwnLane ⇒ continue`；`FollowUp ⇒ RunTriggerRowFollowUp(...)`；**default ⇒ `if (r.Ca == 50) { RunHandBuffRowOfEnchantment(...) }` 否则 `NoteTriggerRowKindGap(...)`** |
| O3 | `EffectResolver.cs:21078` | 唯一调用点：`if (ops == null \|\| ops.Count == 0) { … FireTriggerRowsByChosenAbility(...) }` ⇒ **整条 trigger 只要有一条正文 op，本口就一次都不进** |
| O4 | `EffectResolver.cs:21326-21329`（文件尾头注释）| 已写死口径：「正文 op 非空时一个字节都不许动；把 `chosenAbility` 再跑一遍 = 打两次」 |
| O5 | `CardAbilityDispatch_B116.cs:70-91` / `:136-158` | **B116_W27 已建的卡级派发层**（`trigger 0` 专用）：`AbilityLayerOwnsRow` 四档；`ca 380`→`DispatchCardAbility380`、`320`→`DispatchCardAbility320`、325/360/480→`NoteCardAbilityRowPending` |
| O6 | `RuleCore.cs:4221` · `EffectResolver.cs:2805` | O5 的两个接线点（单位路 / 战术路），已接 |
| O7 | `EffectResolver.cs:18776-18783` | 主动能力那张 `ca` 分派（`if (ca == 85) …` 一行 23 个码：85/350/370/400/160/165/360/320/390/380/480/220/280/290/330/375/135/340/200/345/80/162/310 → 各 `Run*Row`，末档 `RunActiveRowOps`）**只服务 `UseActiveAbilityCore` 的计划表（`RuleCore.cs:15588/:15657`）** |

### 2 原版值 vs 我们值

- **原版**：`AbilityLogic__PlayAbility.c:286` `iVar8 = *(param_1 + 0x10)`（= `chosenAbility`）之后是巨型 switch；被动路入口 `RawCardScript__TriggerAbilities.c:201-215` = 逐条 `CanTriggerAbility` → 逐条 `PlayAbility`（一行 ability 一份结算）。
- **我们**：`trigger 0` 已有「一行一份」层（O5）；**其余 trigger 的等价物 = 该 trigger 的正文 op（`u.FxOps(keyword)`）**，即「整条 trigger 的 desc 一次结算」。
- **可达性（本次现扫 `cards_engine.json`，现读 **1,142** 张 · `triggerAbilityIndices` 1,622 行）**：
  - `trigger != 0` 的 meta-op 行（取主动表那 23 个码 ∪ `{320,325,340,380,480}`）**合计 12 行 / 11 卡**：`320`×9（ASH81/AM8/AM21/GSC14/GSC17/SAU11/GOF59/TL3/UM64）· `340`×2（`9218576f…` = Pray Enchantment 模板）· `480`×1（**SW34 Murderfang**，`trigger 120` 第 3 行）。
  - `320` 那 9 行：`RunTriggerRowFollowUp` 已实现，但**要到 O3 那一格（`TriggerOpsByNumber` 为 null）才会被调到**。而 `TriggerOpsByNumber`（`CardDefPartial_B96CA.cs:102-115`）在「没有 44 张挂表」时**退化成 `TriggerOps(trigger 名的小写)`** ⇒ 进不进这一格**取决于该 trigger 名在不在我们的关键词表**：
    · **会进**（名字不在关键词表 ⇒ op 为 null）：`ASH81`（455 `OtherCardSpiritStone`）· `GSC14`（10 `OtherCardPlayed`）—— 这两张的 320 今天**已经跑**（`RunTriggerRowFollowUp`）。
    · **不会进**（trigger 名就是关键词 ⇒ 正文 op 非空）：`AM8`/`AM21`（301 = `regiment`）· `GSC17`/`TL3`（580 = `strike`）· `GOF59`（300 = `mob`）· `SAU11`（560 = `artifice`）· `UM64`（145 = `codex`）—— 这 7 张的 320 **今天一次都没跑**（净差 = 少那一拍 `FollowUpResolution(410)`；效果本身由正文在主触发上直接结算）。
    ⚠️ 上面这一格是**静态推断**（`TriggerOps` 的键由 `EffectText.Parse` 产，本代理禁跑宿主）⇒ 施工前必须现读一次 `TriggerOps(<名>)` 是否为 null。
  - `340` 那 2 行：**已有等价物**（`EnchantmentExecutor_B115.cs:21-32` 与 `RuleCorePartial_B103W5.RunRemoveThisEnchantmentRow`），不是缺口。
  - `480` 那 1 行：`RunHighlightBloodThirstRow`（`EffectResolver.cs:20930`）只在主动表里，被动路**没有入口** ⇒ 唯一「纯表现」缺口。
- **为什么不能照账上做**：`trigger != 0` 的 1,610 行里 1,598 行是**真效果种类**（40×149 · 30×147 · 100×119 · 10×113 …），正文 op 已经在跑它们 ⇒ 再按 `ca` 派一次 = 同一效果两份。

### 3 该改哪里（判据已足的走法）

1. **不要**把真效果种类接进 `RunActiveRowOpsOrGrantedTrigger`（O4 的理由成立）。
2. 新建 `Core/CardAbilityDispatch_B141Passive.cs`（`partial RuleCore`，类名==文件名）：
   - `internal static int DispatchPassiveMetaRows(BattleContext ctx, UnitState u, int triggerNumber, int owner, int slot, UnitState acting, UnitState target)`：`AbilityLayerOwnsRow`（O5）**复用**，只放行 `FollowUp / OwnLane` 两类；`320` → `RunTriggerRowFollowUp`；`400/380/…` 走主动表里**同名的** `Run*Row`（抽一个 `TryDispatchCaRow(ctx, p, u, row, ca, ta, chosen)` 窄口，由 `EffectResolver.cs:18776-18783` 与这里**共用同一份 if 链** —— 铁律「两处写同一条规则迟早不一致」）。
   - 挂点 = `EffectResolver.FireTriggerByNumberWithSink`（`EffectResolver.cs:21078`）**改成无条件先调一次**（而不是只在 `ops == null` 时），并在同一处把 `ca == 50` 那个特判（O2）搬进表里。
3. 想让 `ca 480` 一并在被动路生效 ⇒ 把 `RunHighlightBloodThirstRow` 加进该窄口；验收 = SW34 打出后多一条 `EvtKind.BloodThirst`（宿主 `BattleScene`）。

### 4 还差什么

- **逐 ability `AbilityIndex` 归位**（被动 trigger 的正文 op 没有 `AbilityIndex`）：`CardDatabasePartial_B98H7.MapTriggerOpsByNumber` 现池只 44 张挂表；`B116_R6` §还差 5/6 已标「未查清」⇒ 不做这一步，`B7 双保险` 在被动路恒不命中。
- 上面那 7 张 `320` 卡「正文已在主触发上结算」是否与原版 410 时机等价 —— **未实测**（需 `play` 读数或宿主跑）。
- 9 张 `320` 卡的「进不进 O3」是**静态推断**（§2），施工前要现读一次 `TriggerOps(<trigger 名>)`。

> 🔴 **2026-11-04 就地订正（`B141-W36` 现读，铁律 5）—— §3 的「挂点 `:21078` 改成无条件先跑」作废**：
> `FireTriggerByNumberWithSink` 的唯一生产调用方 `RuleCore.FireTriggerByNumber`（`:17665→:17673`）全仓**只有 6 处**调用点，全是字面 trigger：`60`（`:20605`）· `185`（`:20606`）· `187`（`:20607`）· `210`（`_B97W2.cs:549`）· `410`（`:13035` / `:20679`）。
> 12 行所在的 `10/120/145/300/301/455/560/580` **一条都到不了该口** ⇒ 改成无条件先跑对 12 行**净覆盖 = 0**（还会破坏 `战斗逻辑驱动.tsv:1151` 记的「只在正文 op 为空时进 ⇒ 与文本路天然互斥」不变式）。
> 现读后的真结论：9×320 里 **7 行已由关键词路在跑**（`FollowUpSchedulerOf` 硬编码表 + `TriggerNameOfKeyword`），2 行（ASH81/GSC14）缺的是 `ActionPump.ExtraTriggerName` 两条映射；340×2 已有等价物；480×1（SW34）需 `RuleCore.FireTriggerDeferred` 尾部加一跳，且**尚未定**「480 早于正文 ⇒ `Has(bloodthirst)` 闸」的顺序。
> ⇒ 本轮**生产代码 0 行**（不建 `CardAbilityDispatch_B141Passive.cs`：唯一挂点不可达 = 死代码）。证据全文 → `报告/B141_W128_ENG11.md`。
> 📌 **账侧待补（`B141-ENG11-DOC`，由调度台改 · 本条只登记）**：`账/战斗逻辑驱动.tsv:80`（`AbilityLogic.PlayAbility(BattleManager, AbilityData, AbilityTrigger)`）与 `:1151`（`PlayerHand.AddHandEffect(HandEffect)`）都引用了 `EffectResolver.cs:21078` 那道闸，但**都没写服务范围** ⇒ 宜各在 `备注` 列补一句「**本口只服务 trigger `60` / `185` / `187` / `210` / `410`；对其余被动行（含 12 行 meta-op：`10/120/145/300/301/455/560/580`）净覆盖 0**」，否则后人会把它当成覆盖全部被动的入口、再去接一遍 `ca`。只改 `备注` 列 ⇒ `check_ledger_points` 零漂移。

---

## 二 ENG-12 · 3 选 1 闸

**判定：功能 `✅已做完待核销`；账上「被 `Count != 2` 拦掉 ⇒ 改成 `>= 2`」= `⛔判据本身错`**

### 1 现读证据

| # | 文件:行号 | 摘录 / 事实 |
|---|---|---|
| E1 | `EffectResolver.cs:18043-18049` | `OathChoiceNeeded`：`IsUnit` 否 · `ChoicesTargetsAffected != 180` 否 · **`:18047` `if (card.ChoicesSpecificCardIds == null \|\| …Count != 2) return false;`** · `return HasOathTrait(card, out _);` |
| E2 | `CardDef.cs:790-797` | `HasSpecificCardChoice` = `targetsAffected == 180 && maxTargets > 0 && ids.Count > 0` —— **没有 `== 2` 的要求**（旧判据说的「`CardDef.cs:786` 要求 2」**不成立**） |
| E3 | `EffectText.cs:3891-3928` | `Choose and gain a bonus (A, B or C)` **归一成三选一**（正则锚 `^choose and gain a bonus (...)$`，出处注释就写着 Carnifex） |
| E4 | `EffectText.cs:10927-10945` | `TryAttachChoiceTableToTextChoice`：**任意 N 张、原序**挂到那条 `chooseone` 上，闸 = `op.Amount == ids.Count` |
| E5 | `EffectResolver.cs:21629-21679` | `TryRunStructuredOptionOrText`：`ctx.FindChoiceOptionCard(id)`（池 → 旁表）命中 ⇒ `RunAbilityOfAnotherCard(...)`；无正文 ⇒ `TryRunChoiceOptionRows`（`ca 40 buffSelf`）；都失败 ⇒ 卡面文本兜底 |
| E6 | `Resources/choice_option_cards.json`（现读） | `count 57`；TL20 三枚 id **全在**：`aa3b6aa4…`=Crushing Claws · `51e88ac3…`=Devourer Cannon · `340fad54…`=Armoured Exoskeleton，每张 `logic = [(ca 40, ta 20)]` |
| E7 | 数据（`cards_engine.json` 现读） | `TL20 Carnifex`：`choicesTargetsAffected 180` · `choicesMaxTargets 3` · 3 个 id · `cardTraits ["Rally"]`（**无 oath**）；池内带 `oath` trait 的 **10 张**卡 `Count` **全 = 2** |
| E8 | `Editor/B116W32_Assert.cs`（接线在 `RuleEngineTest.cs:1306` 同一物理行） | A2（TL20 的 chooseone 挂着 3 个 id）· A5/A5b/A5c 真跑（`Carnifex 4→6`，`FIX_A/FIX_B` 不变）· A6 灭自证（旁表空 ⇒ 回落文本 ⇒ 两友军都 +2） |

### 2 原版值 vs 我们值

- **原版**：`BattleManager__GetChoiceFullPool.c:36-55`（`ta == 0xb4(180)` ∧ `filterSpecificCards` 非空 ⇒ 候选池就是那张表，`Where(uniqueId != null)` + `OrderBy(uniqueId)`）；`BattleManager__GetChoiceOptions.c:40-70`：`iVar1 = maxTargets(+0x40)`，**`if (pool.Count <= iVar1) iVar1 = pool.Count`**，然后循环抽 `iVar1` 个**不重复**候选 ⇒ **N 选 1，N = min(maxTargets, 池大小)**，`3 选 1` 是常态、与誓约无关。
- **我们**：`3 选 1` 走**正文 `chooseone`**（E3）+ **选项 SO 旁表**（E5/E6），与 E8 的读数一致；`OathChoiceNeeded`（E1）只服务「平/誓二选一」，对 TL20 返回 false **是正确的**（它没有 oath），**不是拦路的那道闸**。
- **差异 = 0**（就 ENG-12 这条任务的范围：TL20 三选一能选出、且选中的那一项生效）。

### 3 该改哪里

- **无行为改动**。建议（零风险）：在 `OathChoiceNeeded` 的 doc 里补一句「本闸**只认**誓约二变体；N 选 1 走 `EffectText` 的 `chooseone` + 旁表（ENG-12 已结）」，并把「`Count >= 2`」这条账上建议标作作废（否则后人会照它改，把 3 项的 oath 卡（今天 0 张）送进只按 2 项算下标的 `OathPoolIndexIsOathVariant`（`EffectResolverPartial_B111E1B.cs:39-47`））。
- 若要加断言：`OathChoiceNeeded(TL20) == false` ＋ `TL20 的 chooseone.Amount == 3` ＋ 结构化支命中（E8 已有，可只补第一条）。

### 4 还差什么

- 无判据缺口。**未在宿主实跑**（本代理禁跑 Unity）；E8 的读数是 B116_W32 当时的离线复跑。

---

## 三 ENG-24 · `ChooseValueOptions` 分派表

**判定：`🟠真没做`（26 档清单已查全；可达缺口 = `201` · `17` · 「未命中 ⇒ min」这条兜底）**

### 1 现读证据（我们：散在 4 处 + 正文路，成表的分派**不存在**）

| # | 文件:行号 | 覆盖的档 |
|---|---|---|
| V1 | `EffectResolverPartial_B88E2.cs:467-503`（`ApplyRefCardValueOption`，判据块 `:445-466`） | `:475` `if (vo == 80) return ApplyMinPlusMaxTimesMultiplier(...)`；`:476` `if (vo != 100 && vo != 110 && vo != 120) return op;` ⇒ **只认 80/100/110/120**，其余原样退回正文 |
| V2 | `EffectResolverPartial_B88E2.cs:1061` | `ApplyMinPlusMaxTimesMultiplier`（80 档：`min + max × 目标数`） |
| V3 | `EffectResolverPartial_B88E2.cs:293-330` | `101 BaseRefCardMAttack`：**按原版照抄为「恒空 ⇒ 不生效」**（原版读 `actingCard.referencedBaseCard+0xa4`，我们的 `referencedBaseCard` 池内全空） |
| V4 | `EnchantmentExecutor_B115.cs:1044-1081`（`TryResolveCriteriaValue`） | `case 0/10/20/30/40/41/50/80/90/203`；`default` ⇒ `gap = "…不在本版读得出来的档里"` 且**不结算** |
| V5 | `EffectResolver.cs:18182-18189`（`StampRollPerTarget`） | `spec.ValueOptions == 90` ∧ `Type ∈ {5,10,12}` ⇒ 逐目标掷（`RandomizedOnResolution`） |
| V6 | `EffectResolver.cs:21728-21751`（`ApplyHealCriteria`） | **只认 `valueOptions == 0`**；其余（池内只有 `210`）出声 + 沿用正文数值 |
| V7 | `EffectResolver.cs:292-331`（`TryApplyAmountRef`） | 正文路的数值来源：`faith` / `spirit` / `enemyhand` / `ownwarlordmelee`(140) / `refcardbaseattack`(101)；`excess`(210) 由 `EffectText.cs:5297-5306` 产 |
| V8 | `EffectResolver.cs:840`（`ResolveOneCore` 入口） | `op = ApplyRefCardValueOption(...)`；同一行还有 `ApplyHealCriteria` / `ApplyManaBuff` —— **账上写的 `:836-843` 就是这一带**（现读是注释 + 这一个调用） |

### 2 原版 26 档（`🥇 cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29`，与会话早期 `dump.cs:20865-20895` 逐值一致）

`0 Normal · 10 TargetMeleeAttack · 20 TargetRangeAttack · 30 TargetHealth · 40 ThisCardAttack · 41 ThisCardAttackRanged · 50 ThisCardHealth · 60 TargetDamage · 70 ThisCardDamage · 80 MinPlusMaxTimesMultiplier · 90 RandomizedOnResolution · 100 RefCardAttack · 101 BaseRefCardMAttack · 110 RefCardHealth · 120 RefCardCost · 130 TargetCost · 140 OwnWarlordAttack · 150 TargetBastion · 160 SelfBastion · 170 TargetCardHealthMinusAttack · 200 ThisCardMarksOfChaos · 201 OtherCardMarksOfChaos · 202 AllFriendlyMarkOfChaos · 203 ThisCardShuriken · 204 FaithValue · 210 TargetExcessDamage`

分档语义（`decomp_full/AbilityLogic__GetValueFromCriteria.c` 全文 366 行，逐支）：
`:36-46` 0（`min==max ? min : GetRandomInt(min,max+1)`）· `:47-52` 10 · `:57-61` 20 · `:62-79` 30（4×`ObscuredInt` 拼当前血）· `:82-86` 40 · `:87-92` 41 · `:93-119` 50 · `:120-124` 60（`CardScript.GetCurrentDamage(target)`）· `:126-130` 70（同上，`param_6 = param_5`）· `:131-137` 80（`min + max × multiplierFactor`）· `:130` **90 ⇒ `return min`**（逐目标掷在别处）· `:138-160` 100（`refCard +0x7c` = `currentBaseMeleeAttack`）· `:157-177` 101（`thisCard+0x280` → `+0xa4`）· `:172-190` 110（`refCard+0x68`）· `:195-207` 120（`refCard.CurrentCost`）· `:214-220` 130（`target.CurrentCost`）· `:243-252` 140（`GetHero(bm, thisCard.isPlayer).CurrentMeleeAttack × multiplierFactor`）· `:253-256` 150（`target.CurrentBastion`）· `:258-270` 160（`thisCard.CurrentBastion`）· `:278-283` 170（`target health − target melee`）· `:278-290` 200（`min + thisCard MOC × max`）· `:283-288` 201（`min + target MOC × max`）· `:288-320` 202（`min + Σ 友方单位 trait 0x14a 值 × max`）· `:320-325` 203（`min + thisCard shuriken × max`）· `:325-333` 204（`PlayerManager.GetCurrentFaith`）· `:333-366` 210（`target` 若 `currentHealth >= 0` ⇒ 0，否则 `-(health × min)`）。
**兜底**：所有未命中的 `param_7` 都走 `return uVar6`（= **min**），包括 `1..9`、`11..19` 这些非枚举值。

### 3 我们 vs 原版（现读 `cards_engine.json` 1,142 张的 `opt` 分布）

- `abilityDamage` 1,621 行：`0×1504 · 90×74 · 8×37 · 80×2 · 202/140/204/101 各 1`
- `abilityHeal` 60 行：`0×59 · 210×1`
- buff 列（`*ValueOptions`）：`0×4748 · 10×1 · 17×8 · 30×1 · 40×1 · 41×1 · 50×1 · 80×6 · 90×1 · 201×2`
- **可达但没实现**：`201`（BL43 `meleeAttackValueOptions` / BL47 `armourValueOptions`；引擎里 `grep "MarksOfChaos"` 只命中 `AchievementTally` ⇒ **buff 路 0 实现**）· `17`（GOF30/31/32/36/43 的 `healthValueOptions`，8 行；**不是枚举成员**，原版走「⇒ min」）· **「未命中 ⇒ min」这条兜底整体缺失**（我们的 `default` 是「出声 + 不结算」或「沿用正文」，不是 min）。
- **0 行、只做形态**：60 · 70 · 130 · 150 · 160 · 170 · 200（现扫 0 行）。
- **已由正文路承担、⛔ 别再补第二份**：140 · 101 · 204 · 210 · 202（V7）。

### 4 该改哪里

1. 新建 `Core/EffectResolverPartial_B141VO.cs`（`partial RuleCore`）：
   `static bool TryApplyValueOption(BattleContext ctx, int vo, int min, int max, int multFactor, UnitState thisCard, UnitState targetCard, UnitState refCard, int owner, out int value, out string gap)` —— **一张 switch 覆盖 26 档 + `default ⇒ value = min`**（判据逐支照 §2 的行号抄进注释）。
2. 把 V1 `:475-476` 与 V4 `:1049-1079` 改成**查这张表**（两处都只保留「取 spec / 取目标 / 出声」的壳）；`V6` 的 `210` 走表；`V5` 保持（它管「要不要逐目标掷」，不是取值）。
3. 补 `201`：`min + target.CurrentMarksOfChaos × max`（`UnitState` 已有 `KwValue`/trait 取值口可复用；先在 `UnitState` 找等价读法，⛔ 别新造计数）。
4. 验收：纯重构部分（V1/V4 → 表）**`play all dump` 指纹应不变**；`default ⇒ min` 与 `201` **可能变**（`opt 8`/`17` 共 45 行、BL43/BL47）⇒ 若真变，按「原版就是 min」重钉基线，⛔ 不要为了「指纹不变」保留错值。

### 5 还差什么

- `opt 8`（37 行 / 24 卡，**全是 `DamageType.Random(5)`**）与 `healthValueOptions 17`（8 行 / 5 卡）**都不在 26 档里** —— 是「新版枚举值」还是「残留列」**查不到**（见 §六）。补 `default ⇒ min` 之**前**必须先定这一格，否则可能把 `Sawbonez` 的「Deal 1-2 damage」压成恒 1。
- `202` 的 `trait 0x14a` 是哪条 trait（我们读作 Dark Pact 依据是 `BL31` 卡面）——本次未回源核对 `DefinedTrait 0x14a`，标注为「旁证」。

---

## 四 B140-WN-DOGIVE-EXTRINSIC · 文字路 whileInPlay 属性未标 extrinsic

**判定：`🟠真没做`（判据齐，改动点明确；但**今天可达 0**）**

### 1 现读证据

| # | 文件:行号 | 摘录 / 事实 |
|---|---|---|
| X1 | `EffectResolver.cs:11916` | `static bool DoGive(...)`（`give` / `gain` / `lose`；现读入口，⛔ 不是账上写的 `:12409`） |
| X2 | `EffectResolver.cs:11937` / `:12396` / `:12409` / `:12412` / `:12423` / `:12537` | 六处把 `extrinsic: ctx.ResolvingExtrinsic` 往下传（`AddEffectCasterGates` / `RecordGrant` / `AddStatMod`×3 / `AddTempBuff`） |
| X3 | `BattleContext.cs:2099-2106` | `public bool ResolvingExtrinsic;`（doc 明写「写点全部 save/restore」） |
| X4 | `RuleCore.cs:16143-16160`（`ResolveHandBuffOps`） | 唯一写点 **`:16155` `ctx.ResolvingExtrinsic = x;`**（`:16157` finally 还原）；`x` 来自 `extrinsics[]` |
| X5 | `RuleCore.cs:10739` | 唯一调用：`ResolveHandBuffOps(ctx, owner, unit, ops, ext)` —— `ext` 由 `HandEffect.Extrinsic` 生成，而**该字段无生产者**（现读全仓 `grep "HandEffect.*Extrinsic"` 无写入点） |
| X6 | `UnitState.cs:1354-1361` / `:1387` | `Extrinsic = extrinsic \|\| IsExtrinsicSource(source)`；doc 已写判据 `PlayAbility.c:1197` ⇒ `trigger ∈ {20,240}` |
| X7 | `EffectResolverPartial_B97W2.cs:235/241/247/449` | 数据行那半**已覆盖**（`case 1/70/85` 传 `b.Extrinsic != 0`） |
| X8 | 现读 `grep -rn '"whileinplay"' Core/ Data/` | **0 命中**；`TriggerNameOfKeyword`（`ActionPump.cs:1081`，映射表 `:1099-1122` + `ExtraTriggerName` `:1894-1912`）**没有 whileinplay / whileinplayvariable** ⇒ `u.FxOps("whileinplay")` 永远拿不到正文 op |

### 2 原版 extrinsic 的真身（本次现读，`.c` 逐行）

`decomp_full/AbilityLogic__PlayAbility.c:1190-1200`：

```
cVar4 = '\x01';
if (*(char *)(param_3 + 0x40) == '\0') cVar4 = *(char *)(param_3 + 0x41);
*(bool *)(local_res8 + 0x30) = cVar4 != '\0';      // CardEffect.extrinsic(+0x30)
```

`param_3` = `AbilityData`（`dump.cs:19556-19569`：`whileInPlay // 0x40` · `whileInPlayVariable // 0x41`）⇒ **`extrinsic = (ability.trigger ∈ {20 WhileInPlay, 240 WhileInPlayVariable})`**；另一条同形写点 `:1354`（`*(bool*)(uVar23 + 0x30) = cVar4 != '\0'`），读取点 `:1208` `if (*(char*)(uVar23 + 0x30) == '\0') { … }`。🥈 交叉：`cpp2il/out_cpp2il_dev_ilspy/CardEffect.cs:14` `public bool extrinsic;` + `:112` `cardEffect.extrinsic = extrinsic;`（`GetNewInstanceOfThisEffect` 逐格拷贝）。

### 3 我们 vs 原版 / 可达性

- 数据行（X7）与光环层（X6：`Auras.GrantTag` ⇒ `IsExtrinsicSource`）**都已标**；文字 op 路（X2）**没标**，因为 X4/X5 恒 false。
- **可达 = 0**：20/240 的正文 op 今天没有 `FxOps` 键（X8），这些卡的常驻效果全部由 `Aura.Recompose` 承载 ⇒ `DoGive` 根本不会在这两族上被调用。所以本残差是**结构性欠账**（将来谁把 20/240 正文 op 挂上，就会静默少标 extrinsic）。
- 池内规模：`abilityTriggers` 含 20 或 240 的 **46 张**（现扫；20：13 张左右，240：33 张，有交集）。

### 4 该改哪里

1. `RuleCore.FireTriggerDeferred`（`RuleCore.cs:12913`）里把 `:12932` 那次 `ResolveOps` 包起来：
   `bool prev = ctx.ResolvingExtrinsic; if (keyword == "whileinplay" \|\| keyword == "whileinplayvariable") ctx.ResolvingExtrinsic = true; try { … } finally { ctx.ResolvingExtrinsic = prev; }`
   ⚠️ **别找 `KeywordTable.WhileInPlay` 常量 —— 它不存在**（现读 `grep "WhileInPlay\s*="` 只命中 `AchievementTally.TriggerWhileInPlay = 20` / `:270 = 240`，那两个是成就计数表的私有语义，⛔ 别跨用）；数值口 = `CardDef.AbilityTriggerNumber("WhileInPlay")`（`CardDef.cs:1951-1956`，公开，名字表在 `:1918/:1927`）。另：`keyword` 传进来是**小写**（`u.FxOps(keyword)` 的键），所以这里直接比小写串，或先做第 2 条映射再反查数值。
2. `ActionPump.ExtraTriggerName`（`ActionPump.cs:1912` 的 `return null;` 之前）补两条映射：`whileinplay → "WhileInPlay"` · `whileinplayvariable → "WhileInPlayVariable"`（判据 = `CardDef.cs:1918/1927` 的名字表）—— 不补就永远拿不到名字（X8），宿主断言与 `abilityTrigger` 那道闸也无从下口。
3. 手牌那一半：`GrantHandBuffForTargets` / `AttachHandAbilityBuff` 建 `HandEffect` 时同样按「来源 trigger ∈ {20,240}」置 `HandEffect.Extrinsic`（这是 X5 缺的那个生产者）；接线后 `ResolveHandBuffOps` 会自动分段。

### 5 还差什么

- 20/240 的正文 op **该不该**由文本路产生（现在由 Aura 层承载）—— 属「结构性决策未定」，⛔ 本代理不裁；不接这一步，上面三处改动**净可观测 = 0**（可先落 ①②，附一条纯函数断言：`FireTriggerDeferred("whileinplay")` 期间 `ResolvingExtrinsic == true`）。
- 「Aura 层已覆盖」的覆盖度：46 张里有多少张有 `AuraSpecs` **未逐张核**（只核了主链 5 张：AM28/AM38/GSC4/GOF11/SAU27 的描述形态）。

---

## 五 顺手发现（⛔ 未改，只报）

1. **`cards_engine.json` 现读 1,142 张**（`count` 字段与 `len(cards)` 都是 1,142）—— 大量文档仍写 1,129 / 1,127。凡引用张数一律现读。
2. **B116 对账 G2 的一条施工前提不成立**：它写「让 `HasSpecificCardChoice`（`CardDef.cs:786`）不再要求 2」——现读 `CardDef.cs:790-797` **从来就没有** `Count == 2`（只要求 `> 0`）。同族：G2 说「`EffectResolver.cs:18047` 让 TL20 走不进 `choosecard`」，实际 `choosecard` 的注入闸在 `EffectText.cs:2537-2553`（**不读** `OathChoiceNeeded`）。
3. **`opt 8` 与 `opt 17` 可能不是 `ChooseValueOptions` 的成员**（26 档表里没有；`GetValueFromCriteria.c` 对 `< 0xb` 的非 0/10 一律 `return min`）。但 `opt 8` 的 37 行全部是 `DamageType.Random(5)`、且 `Sawbonez` 卡面写的是「Deal 1-2 damage」⇒ 原版若真按 min 走就恒 1，与卡面不符 ⇒ **先核 SO 再动**，⛔ 别急着补「default ⇒ min」。
4. **`EffectResolver.cs` 的账行号已普遍漂移**：账里 ENG-24 用 `:836-843`（现读是注释 + `:840` 的调用）、B140_WN 用 `:12409/12412/12423`（现读是 `DoGive` 体内五行）。凡按账行号施工前必须现读。
5. **`ApplyHealCriteria`（`EffectResolver.cs:21728-21751`）只认 `valueOptions == 0`**，`210 TargetExcessDamage` 靠正文 `excess` 路（`EffectText.cs:5297-5306`）—— 这两条写在不同文件、没有交叉引用，后人很容易只改一处。

---

## 六 ❓查不到（搜过什么）

1. **`opt 8` / `healthValueOptions 17` 的语义**：搜过 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs`（26 值，无 8/17）· `d:/2/tools/il2cpp_out/dump.cs:20865-20895`（同 26 值）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs` · `decomp_full/AbilityLogic__GetValueFromCriteria.c` 全文（`< 0xb` 只认 0/10，其余 `return min`）· `Cards_engine.json` 全卡扫描。**结论 = 原版代码对 8/17 走 `return min`；数据里为何有这两个值（新版枚举？残留列？）没查到**。
2. **20/240 触发正文 op 的 `FxOps` 键**：搜过 `Core/*.cs`（`"whileinplay"` / `WhileInPlay"` / `TriggerNameOfKeyword` / `ExtraTriggerName`）· `Data/*.cs` · `cards_engine.json` 的 `abilityTriggers`。**结论 = 没有任何地方给 `_triggerOps` 挂 `whileinplay` 键**；但「原版这一族里哪一半由正文 op 承载、哪一半是纯光环」没有查到（`CardDef.AuraSpecs` 的产出面只核了 5 张样本）。
3. **SW34 的 `ca 480` 是否需要表现（高亮）**：只读到主动路执行器 `RunHighlightBloodThirstRow`（`EffectResolver.cs:20930`）；被动路有没有等价位、原版在 `Slay` 上是否真的触发高亮 —— 需要宿主实跑或再查 `CardScript__HighlightBloodThirst`，本次**没查到**。
4. **`trait 0x14a`（202 档）与 `DefinedTrait` 的对应**：本次未回源核 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`，文中「Dark Pact」是旁证（`BL31` 卡面）。
