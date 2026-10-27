# B9 / C2T7m —— 特效施工单（把核账的「差在哪」汇总成可派修）

> 查判据型子代理（只读 + 出表，不改代码）。把本会话 `特效` 域 4 份核账表（C2T7h/续/续3/续4）里**已写清「差在哪」的真差异（B）**汇总成施工单。
> 产出：`判据/特效_施工单_20261027.tsv`（9 列 · 16 行）· 本报告。

---

## 一、施工单统计（按阻塞分组）

**共 16 条：无阻塞 6 · 有阻塞 10。**

| 阻塞 | 该改哪里（文件） | 条数 | 方法 |
|---|---|---|---|
| **无阻塞（先派）** | `AnimAnchors.cs` | 1 | GetAnimTransform |
| | `AnimFXController.cs` | 1 | PlayTriggerAnim |
| | `Badges.cs` | 1 | GetCardEffectsToShow |
| | `CardInteraction.cs` | 1 | StopHandAnim |
| | `TraitFrames.cs` | 2 | ActivateIconAnim · ActivateTriggerTraitAnim |
| **🔴 有阻塞（CardFeel.cs / BattleDriver.cs 有代理在写）** | `CardFeel.cs` | 3 | ResolveUnitShake · ResolveAttackAnimationEffects · ReturnToDeckAnim |
| | `BattleDriver.cs` | 7 | PlayAnim · ResolveBloodThirstAnim · ResolveAnimInfoRangeAttack · ResolveAttackRangedAnim · MoveAndDisappearAnim · VoiceLinesPopUpShowedUp · ShowPlayerActionAnim |

> **排序**：TSV 里已按「无阻塞在前（同文件相邻）→ 有阻塞在后」排好，序号 1–16 即建议派修顺序。
> **未收**（按你口径）：`判据缺 / 无落点` 的**不算真差异**，故 `NestedFadeGroup` 18 条（无落点·结构缺，"做 per-group alpha"是另一本账）· `GetAnimTransform` 之外的无落点行 · `ReturnToCard` 类未实现但判据缺的，**均不在本单**。

---

## 二、TOP10 最值得先做（影响面 × 成本）

> 原则：**无阻塞优先**（能立刻派）；同为无阻塞里挑「可见/影响大、改法明确」的。

1. **`ResolveAttackAnimationEffects`（震屏幅度表）** —— 原版 `GetUnitSize→OverwriteCameraShakePreset.SetAmplitude` 三档，我方 `ShakeAmpUnitSize` 0/2/3→2/4/8 未对。**每场攻击都可见**，改法 = 读原版三档 SetAmplitude 值替换常量。成本低。⚠️ 阻塞 CardFeel.cs（等代理释放）。
2. **`ResolveAttackRangedAnim`（clan 弹道表）** —— 原版按 `cardArmy` 查 `GetRangedClanParticles`；**每张远程卡可见**。我方远程链是否按 cardArmy 查未证。成本中（要先确认我方表）。⚠️ 阻塞 BattleDriver.cs。
3. **`ActivateIconAnim`（亮图标半）** —— 触发类图标缺「亮」这一半，**战斗里词条触发可见**。无阻塞（TraitFrames.cs），改法明确。**无阻塞里最该先做**。
4. **`GetAnimTransform`（锚点方法可能整体错）** —— 若我方用 GetPos 代替导致某些 PositionOption 锚点不同，影响面广。无阻塞（AnimAnchors.cs），但要逐档证（成本中）。
5. **`ResolveUnitShake`（shake 参数）** —— 受击抖动手感。⚠️ 阻塞 CardFeel.cs。
6. **`GetCardEffectsToShow`（卡面效果列表项与序）** —— 卡面「受到以下影响」行。无阻塞（Badges.cs）。
7. **`ActivateTriggerTraitAnim`（高亮半）** —— 同 ActivateIconAnim 的亮图标半。无阻塞（TraitFrames.cs）。
8. **`MoveAndDisappearAnim`（margin/末态收体）** —— 单位消失演出。⚠️ 阻塞 BattleDriver.cs。
9. **`ResolveAnimInfoRangeAttack` / `PlayAnim`** —— 远程/多目标 anim 细节。⚠️ 阻塞 BattleDriver.cs。
10. **`ReturnToDeckAnim`（换牌回牌库）** —— 换牌一条可见演出缺失。⚠️ 阻塞 CardFeel.cs。

> TOP10 里 **3 条无阻塞**（#3 ActivateIconAnim / #4 GetAnimTransform / #6 GetCardEffectsToShow / #7 ActivateTriggerTraitAnim 共 4 条可立刻派）其余等 CardFeel/BattleDriver 代理释放。

---

## 三、方法学 & 纪律
- 数据来源：本会话 `特效` 域 4 份核账（`B行逐条核_特效.tsv` / `_续` / `_续2` / `_续3` / `_续4`）+ 各报告。每条 `判据出处` 都用具体的 `.c` / ILSpy `文件:行`（抽查：`decomp_full/CardScript__ResolveAttackAnimationEffects.c:1-123` · `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:4080 _d__360` · `decomp_full/EntityScript__GetCardEffectsToShow.c:1-196` 等）。
- `判据/特效_施工单_20261027.tsv`：9 列 × 16 行，按「无阻塞→阻塞」+ 同文件相邻排序。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。

---

## 四、无阻塞四条施工（2026-10-27 · C2T7m 续）

> ⚠️ **结论：4 条里没有一条需要改代码** —— 2 条**早已实现**、2 条**落点可疑/不同方法**。这正是「派活前先 grep 落点确认是不是早做完了」那条纪律的命中（本会话第 9、10 例）。**未写任何 `.cs`**（避开了 2 处会改错的地方）。

### ① `ActivateIconAnim` 亮图标半 → **已实现，无缺口**
- 原版 `HighlightTraitIcon(trait) → BoardTraitIcon.HighlightIcon()` = `DOScale(原尺寸×1.6, 0.5s).SetLoops(2,Yoyo).SetEase(OutCubic)`。
- 我方 **`CardView.PulseBadge`**：doc 逐句写「原版 `BoardTraitIcon.HighlightIcon()` 逐句照写」（1.6x / 0.5s / 2 Yoyo / OutCubic，`Core/CardView.cs:4052-4084`）；`PulseBadgeByKeyword` 按关键词找那一位再脉冲。
- **已接线**：`BattleDriver.PlayTraitPulse` 第 `:24590` 句 `if (v.PulseBadgeByKeyword(kw)) return;` ⇒ 触发路径上「亮图标」这一半真在跑。
- ⇒ 「亮图标半未接」**是误判**。**无需改**。（唯一未逐条核的 = 「非触发路径（能力直接赋予 trait）是否也调」——那是接线覆盖面问题，不是缺方法。）

### ② `ActivateTriggerTraitAnim` 高亮半 → **已实现，无缺口**
- 同上（高亮半 = `PulseBadgeByKeyword` = HighlightIcon），已由 `PlayTraitPulse:24590` 接线；早退半本就一致（`DisplayTrigger` 查不到帧就返回 false）。
- ⇒ **无需改**。

### ③ `GetCardEffectsToShow` 项与序 → **落点可疑！勿改 `Badges.For`**
- **`Badges.For` 是 `BattleCardUI.UpdateTraitIcons`（场上 7 枚徽标/图标行）的复刻**（doc：「从单位当前关键词表挑出要显示的那几个，最多 7 个」，`Core/Badges.cs:For`）—— **不是 `GetCardEffectsToShow`**。
- 原版 `EntityScript.GetCardEffectsToShow`（`decomp_full/EntityScript__GetCardEffectsToShow.c:1-196`）迭代 `abilities(+0x108)` 过滤→建 **`CardEffect`**（mark-of-chaos 等 `CreateMarkCardEffect`）= **卡面「受到以下影响」的效果列表**，喂 `CardEffectsService.EnableEffects`（账里 row 20）。
- ⇒ **落点应改到 `CardEffectsService`/`CardEffectItem`**，⛔ 不是 `Badges.cs`。**本轮不改**（改错文件比不改更糟）；项与序的逐条核移到效果列表那一路（非本文件）。

### ④ `GetAnimTransform` 逐档 → **不同原版方法，证不出等价 ⇒ 只写结论，不改**
- 原版 `BattleManager.GetAnimTransform` 按 BM 字段选（§一已列全档），我方 `AnimAnchors.Resolve` 是 `AbilityLogic.GetPos` 复刻 —— 两条不同方法（`Core/AnimAnchors.cs:154`）。**证不出「同 PositionOption 取同一节点」** ⇒ 结论 = **未实现独立 GetAnimTransform**；⛔ 不硬改 `AnimAnchors`（改错会连带 `GetPos` 那 36 档）。

### 施工单已同步
`判据/特效_施工单_20261027.tsv` 这 4 行已改注：`ActivateIconAnim`/`ActivateTriggerTraitAnim` → 「无需（已实现）」；`GetCardEffectsToShow` → 「落点修正（非 Badges）」；`GetAnimTransform` → 「需独立复刻（先证或单列）」。

### 纪律
- 本轮**无 `.cs` 改动 ⇒ 无需 typecheck**；未动 git/主表/正本；未碰 `CardFeel.cs`/`BattleDriver.cs`（只读它们的既有事实）。

---

## 五、`GetCardEffectsToShow` 真落点（2026-10-27 · 查证 + 停手）

### 真落点 = `Battle/CardDisplayWindow.cs:851 RowsOf(...)`（**不是** `CardDisplayEffects.cs`，也不是 `Badges.cs`）
- `GetCardEffectsToShow` 喂 `CardEffectsService`/`CardEffectItem` = 卡面「受到以下影响」效果行 ⇒ 我方在 **`CardDisplayWindow.RowsOf(IReadOnlyList<UnitState.TempBuff> buffs)`（`Battle/CardDisplayWindow.cs:851`）** + 同文件的 `AttrText`（属性名→原版键 `Battle/Effect/Change*OneTurn`）、`SetEffectRows`(:915，5 槽、有 buff 才露、槽用完截断 = 原版 `CardEffectsService.cs:91-132`）。
- `Shell/CardDisplayEffects.cs` 是**展示窗粒子/图件**建件器（另一件事，其文件头自己写「`CardEffectItem ×5` 不做」）；`Badges.cs` 是 `UpdateTraitIcons`（图标行）。都不是 `GetCardEffectsToShow`。

### 项与序的**真差异**（读 `CardDisplayWindow.RowsOf` :851-875 + 原版 `EntityScript__GetCardEffectsToShow.c:1-196`）
- **原版项源 = 迭代 `abilities(+0x108)`**（这张卡的**能力列表**，按能力顺序；含 mark-of-chaos 等建 `CreateMarkCardEffect`）。
- **我方项源 = 迭代 `UnitState.TempBuff`（当前生效的限时 buff）**（`RowsOf(buffs)` :851）。
- ⇒ **项与序都不同**：① 原版列「卡 abilities 里的 effect」（含永久/限时/关键词/属性那几族）；我方只列**已施加的 TempBuff**（限时增益）；② 原版序 = abilities 序，我方序 = buffs 施加序。**这是真差异**（不是「判据缺」）。

### ⛔ 停手（按纪律）：`CardDisplayWindow.cs` 正被别的代理在写
- `git status` 现读：**`M CardPresentation/Battle/CardDisplayWindow.cs`**（另一代理在改）。
- 按你定的纪律「发现有人动就停手写报告」⇒ **本轮未改**。
- **改法建议（待该文件释放后，由它的写手或下一批做）**：把 `RowsOf` 的项源从「仅 `TempBuff`」扩成「**迭代整卡 abilities（含永久 effect）**」，并**按 abilities 序**输出（原版序）；保留现有 5 槽/空行占位/`Battle/Effect/*` 键那套（那套已对）。风险：`abilities` 的 effect 与 `TempBuff` 是两套模型，需在 `RuleEngine` 侧对齐「这条 ability 当前是否该显示」，**不是纯表现层能拍**⇒ 建议连同 RuleEngine 一起核后再改。

### 交付
- 真落点 + 项源差异 + 停手原因 + 改法建议 = 本节；**未改 `CardDisplayWindow.cs`**（M，被占）。施工单里 `GetCardEffectsToShow` 那行「落点修正」现可精确写成 **`CardDisplayWindow.cs:851 RowsOf`**。

### 纪律
- 本轮**无 `.cs` 改动**（目标文件被占）⇒ 无需 typecheck；未动 git/主表/正本；未碰被占的 `CardDisplayWindow.cs`/`BattleDriver.cs`/`CardFeel.cs`。

---

## 六、CardEffectsToShow 判据（2026-10-27 · 只出判据不改码）

> 完整版 → `判据/CardEffectsToShow_判据.md`。答你 5 问，主据 `decomp_full/EntityScript__GetCardEffectsToShow.c`（迭代 `+0x108`）+ `CardEffectsService__EnableEffects.c` 等。

1. **哪几类进表**：按 `+0x108` 下标遍历。**mark-of-chaos**（`+0x3c==0x14a` + trigger∈{10,0x14,0x1e,0x28}，`.c:47/53/72/92/111`）**按神聚合一条 + 计数累加**；**普通 effect**（非 mark 分支，`.c:161`）**每条一行**；**排除**：`+0x38==2 && +0x68==0`（`:135`）、`+0x38==3 && +0x3c∈{0x4fd,0x4fc,0x4fe}`（`:146-161`）。文字渲染在 `CardEffectItem__LoadEffect`（属性 `Battle/Effect/*` / 关键词 `Card_Trait/*`）。
2. **该不该显示**：**本方法无时间窗/isActive 闸**，只有上面的 type/trait 排除 + mark 聚合。`CardEffectItem.IsActive` = `gameObject.activeSelf`（渲染态）。「该不该」= `+0x108` 列表里有没有（时间窗/来源在场在**列表内容**那一层 = 引擎增删）。
3. **序**：`+0x108` **索引序 = 添加序**（非优先级/非字典序）。
4. **RuleEngine 要提供什么**：项源要换成 `+0x108` 合成列表 —— 我方引擎**已有两半** `CardInstance.ActiveEffects` + `HandEffects`（`RuleEngine/Core/Enchantment.cs:48-49` 明写「两条都对应原版 `+0x108`」）⇒ **不用造数据，缺一个按 `+0x108` 序合并+带标记字段的读口**。建议接口形状：`CardInstance.GetEffectsToShow(bool isPlayer) → IReadOnlyList<CardEffectShown>`，`CardEffectShown{ Type(+0x38) / Trait(+0x3c) / Trigger(+0x78) / Value(+0x40) / FromPlayer(+0x90/+0x91) / SourceName }`；过滤+mark 聚合放读口里做（一次对）。⚠️ 合成序需在 RuleEngine 侧确认「`ActiveEffects`+`HandEffects` 是否真是原 `+0x108` 一条 list 拆的」（我们拆了，合成要还原原序）。
5. **5 槽/空行占位/截断**：**全在渲染层 `CardEffectsService.EnableEffects`（`CardEffectsService__EnableEffects.c`），项源不截断**：逐项 `LoadEffect`；`IsTextEmpty` 真 ⇒ 隐藏那格但**照样 `slotIndex++`（空行占位）**；`slotIndex` 达 `slots.Length`(恒 5) ⇒ **break 截断**；`DisableLeftoverSlots` 关剩余槽。⇒ **我方现况（`CardDisplayWindow.cs:913` doc 自称「按 buff 铸行、空的不铸 ⇒ 不留空洞」）与原版不同**，要还原「空项占槽/留空洞/槽满截断」。

**施工口径**（文件释放后一次做）：项源换 `+0x108` 合成（`ActiveEffects`+`HandEffects`）→ 过滤(type2&flag0 / type3&trait∈{4fc,4fd,4fe}) + mark 按神聚合 → 序=添加序 → 渲染层保留 5 槽/空项占槽/槽满截断。⚠️ 落地前拿 `dump.cs` 的 `CardEffect` 字段表交叉一次（0x14a=mark trait、10/14/1e/28=四神、4fc/4fd/4fe=三排除 是按 .c 用法反推的）。

> 本批**未改任何 `.cs`**（你说只出判据）。


---

## 七、施工（A RuleEngine 读口 / B 渲染层）—— 结果：**卡在模型不统一，未硬做**

> 先谢纠正「`git status` 的 M = 累积差异 ≠ 此刻有人在写」。但读了我方模型后，A/B 都触及一个**比文件占用更硬**的阻塞，按你「确认不了就写清差什么、别硬拼」处理。全本 → `判据/CardEffectsToShow_判据.md` §七。

### A. 字段表已交叉 `dump.cs`（订正了我按 .c 反推的两处）
`CardEffect`（`dump.cs:TypeDefIndex 3199`）真字段：`+0x38=buffType` · `+0x3c=trait` · `+0x40=traitValue` · `+0x68=costChange` · **`+0x78=markOfChaosType`（我原写成 trigger，错）** · **`+0x90/+0x91=secretBuff/questBuff`（我原写成 isPlayer，错；真 isPlayer=`originalIsPlayer+0xA0`）**。⇒ mark 分支 = 「`trait==markOfChaos(0x14a)` 且 `markOfChaosType∈{10,20,30,40}` ⇒ 按神聚合 + traitValue 累加」。判据 doc 已订正。

### 🔴 A 为什么不能硬做（模型不匹配）
- 原版 `+0x108` = **一条** `CardEffect` 列表（带 `buffType/trait/markOfChaosType/traitValue`）。
- **我方没有这条统一列表**：`+0x108` 被拆成 **4~5 套异构模型** —— `CardInstance.HandEffects`（**`HandEffect` = `EffectOp` 载荷**，无 buffType/trait/markOfChaosType）· `CardInstance.ActiveEffects`（`ActiveCardEffect`，**只附魔 12/15/16**）· `UnitState.TempBuff`（限时）· `Aura`/`Persistent`（计算式，非可枚举列表）（`Enchantment.cs:48-49` 自己写「按消费者拆开」）。
- 要 `GetEffectsToShow` 返回 §四那个 `CardEffectShown{buffType/trait/markOfChaosType/traitValue/...}`，必须 **union 这几套 + 把 `EffectOp` 反推成 `buffType/trait`** —— 而 **`Aura`/`EffectResolver`/`CardDef`/`Enchantment` 全被 E6ENG/E7ENG 占着**，我拿不到贡献枚举、也没有 EffectOp→原版字段的映射。**硬拼 = 半对的项源 → 所有对局所有卡「受到以下影响」静默显示错**（blast radius 极大）。

### B 为什么现在也≈no-op
- 现 `RowsOf` 源 = `TempBuff`，**每条都有文本、根本不出空项** ⇒ 「空项占槽/留空洞」这半**改了也没有可见效果**；真正的可见差异要等项源换成原版 `+0x108`（= A）。所以单独做 B 是死功夫。

### 建议（等 E6ENG/E7ENG 释放 / 或并入它们）
1. RuleEngine 侧先**统一只读投影**：`CardInstance.GetEffectsToShow(bool isPlayer)` union `ActiveEffects+HandEffects+(Aura/Persistent 贡献)`，每条归一成原版 `CardEffect` 形状（需 EffectResolver 提供 `EffectOp→buffType/trait` 映射）。
2. 归一层落定后，`CardDisplayWindow.SetEffectRows` 再接它 + 上 `5 槽/空项占槽/截断/关剩余`。
3. 判据已铺平（`判据/CardEffectsToShow_判据.md` §一–§七）。

### 纪律
- **本轮无 `.cs` 改动**（A/B 均阻塞在模型不统一，且触被占系统；按「别硬拼」）。未跑 Unity/动 git/改正本。产出：`判据/CardEffectsToShow_判据.md`（含 §七 字段订正+模型阻塞）+ 本报告 §七。

---

## 八、CardEffect 三格置值点（2026-10-27 · 只读）

> 全本 → `判据/CardEffect三格_置值点.md`。**结论：三格全部 LIVE（非死），置值点都找到。**

- **`secretBuff`(+0x90)**：`CardScript__AddEffect.c:853-854` —— 当**这条效果的【附魔卡】`spellType == SpellType.SecretOrder(60)`** 时置 true（`+0x88`=enchantment，`+0x90`=其 spellType）；`EntityScript.RevealedSecret.c:59` 揭示时置 false。**消费** = `CardEffectItem.cs:31`（`secretBuff & isPlayer` ⇒ 效果行只给拥有者看）。
- **`questBuff`(+0x91)**：`CardScript__AddEffect.c:856-887` —— 附魔卡 `spellType == SpellType.Quest(120)` 时置 true，且**先 RemoveEffect 旧 quest 效果 + 播 DisplayStatusAnim(0x2ee) + UpdateTraitIcons**。**消费** = `CardEffectItem.cs:41/43`。
- **`originalIsPlayer`(+0xA0)**：`CardEffect.cs:106` + `CardScript.cs:14294/14593/14661` = **施放方 isPlayer**。**消费（重度）** = 回合到期（`CardScript.cs:7157/7180/7194`、`PlayerHand.cs:2759`）+ mark 归属（`GiveHuntsMark.cs:24`、`GiveMarkOfChaos.cs:26` 只 originalIsPlayer 的 mark 才算）。

**对我们**：三格都能**派生**（secret/quest ← `CardDef.SpellType`(60/120 都有)；originalIsPlayer ← `HandEffect.ActingUnit` 阵营），但**当前没落成 per-effect 字段** ⇒ 做 `GetEffectsToShow` 时应在**统一项源/挂载时把这三格固化**（否则「施放者离场后取不到 originalIsPlayer」「按 TempBuff 读不到 spellType」）。

**⚠️ 又一次「别只看一份」**：`questBuff`/`secretBuff` 的 `= true` 写点 **ILSpy 里没有（只显示 `=false`）、`.c` 里才有**（`AddEffect.c:854/887`）—— 和本会话前面 `.c` 丢体/ILSpy 丢常量是同一族。⛔ 不能因为 ILSpy 没写点就判「死格」。

**纪律**：只读，未写 `.cs`，未跑 Unity/动 git/改正本。产出 `判据/CardEffect三格_置值点.md` + 本 §八。
