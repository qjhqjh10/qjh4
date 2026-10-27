# B9 · `E7ENG` 报告 —— 账 532 的引擎侧两输入（`enchantTrait` / `EvtKind.EnchantmentRemoved`）＋ 成就触发口径对齐

> 执行代理 `E7ENG` · 2026-10-27 · 批次 9
> 白名单（简报）：`Enchantment.cs` · `CardDef.cs` · `BattleEvent.cs` · `EventTiming.cs` · `AchievementTally.cs` ·
> `工具/gen_cards_engine.py`（+ 产物 `cards_engine.json`）· `RuleEngineTest.cs`（**只改计数那一条断言**）
> ⚠️ **实际多改了一个文件：`RuleEngine/Data/CardDatabase.cs`**（理由见 §④·0 —— 不改它，新列在运行期**读不到**，
> 整件功能就是死数据；该文件本波没有分给任何人）。

---

## ① 结论（三件全部做完，typecheck 0/0）

| # | 任务 | 结果 |
|---|---|---|
| 1 | **`532` 的引擎侧输入 1a：`CardEffect.trait`** | ✅ **数据链全通**：`gen_cards_engine.py` 新列 `enchantTrait`（表版本 **32 → 33**）→ `CardDef.EnchantTrait`（构造后挂载）→ `ActiveCardEffect.Trait`（`Attach(..., trait:)` 写入）。池内落值 **24 张**：`pray` × 23（SOR）＋ `vulnerable` × 1（`BL74`）。 |
| 2 | **`532` 的引擎侧输入 1b：`EvtKind.EnchantmentRemoved`** | ✅ 枚举**末尾**加第 21 档（🔶 自己设计标记已打）· 发射点 = `Enchantments.Remove` **成功分支**（四个生产落点唯一汇合处）· `EventTiming.ZeroDurationKinds` 已登记（0 s）· `RuleEngineTest` 的 `EvtKind` 计数 **20 → 21**。**消费点未接线**（`BattleDriver.PlaySignal`，归 `E4BDR`）⇒ 精确改法见 §④·4。 |
| 3 | **`AchievementTally.AddTrigger` 不过滤 20/240/630** | ✅ 已按原版 `ChallengeLogMgr.LogTriggerAbility` 加过滤（判据见 §②·3）。**这是「成就口径 = 挑战口径」的对齐，不是发明。** |

**关键读数**（都是本批实跑，不是引用）：
- `TMPDIR=/tmp/wf_E7ENG bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改了 8 个 `.cs` 之后共跑 3 次，最后一次仍 0/0）。
- `play all dump` **改前 / 改后逐卡逐列比：1130 行、0 行不同** ⇒ 本批对**结算层零影响**（预期：`enchantTrait` 只有附魔层读、成就计数不进探针）。
- `ruleprobe check`：rc=0（解析差异 0 行）· **但多了一条「卡池 md5 与钉住的不同」的哨兵**（我重生成过卡池）⇒ **需要重钉 `PinnedPoolMd5`**（见 §④·3）。
- `ruleprobe play check`：**rc=1（红）**，但 🔴 **不是本批造成的**（归因见 §②·4）。

---

## ② 证据

### 2·1 `CardEffect.trait(+0x3C)` 的语义与原版唯一消费点

- **原版唯一消费者** = `BattleManager__RemoveDestroyableAnims.c`（内联进 `ResolveAction` case 0x10）：
  第一句 `(buffType - 15) > 1 ⇒ return`（只认 **15 singleUse / 16 choice**），随后拿 `cardEffect.trait` 当键去
  `BattleCardUI.TraitFrameEffectsPlay(+0x2d0)` / `TraitFrameEffectsTrigger(+0x2d8)` 两本字典找那件状态框、
  `DoDestroy()` 并摘字典。**不参与任何数值结算。**
- **数据在哪（本批现读 `cards_raw.json` 1,351 张全表扫）**：`cardAbilities[].abilityLogic[].buffEffect` 里
  `buffType ∈ {12,15,16}` 且带 `trait` 键的行 **132 条**，其中 **`trait != 0` 的 28 条**
  = `1185 pray` 24 条（23 张 SOR，`SOR29` 两条）＋ `480 vulnerable` 4 条；
  `activeAbility` / `alternativeAbility` 那两条支 **0 条**。
  ⇒ 落成卡列后**池内 24 张**（`480` 那 4 条里 3 条属于**不在池**的 `Spreading Corruption`（`875eedc9…`），
  只有 `BL74` 在池）。
- **数字 → 名字只认 `DefinedTrait.cs`**（`vulnerable = 480` 在 `:48`、`pray = 1185` 在 `:117`）——
  ⛔ 不用真值 `CardTraits[].name` 兜底：`480` 在 1,351 张 `CardTraits` 里 **0 命中**（判据文件 §1·2 已写明）。
  生成器里同 `load_ability_trigger_names()` 的写法新增 `load_defined_trait_names()`（读枚举桩，不在脚本里抄表）。

### 2·2 🔴 判据文件 §1a 的一处**就地订正**（铁律 5）

判据 `判据/T4_532与PremiumCampaign_判据.md` §1a 的施工建议写「`Enchantments.Attach` 读 **`enchantmentCard`**.`EnchantTrait`」。
**按此实现会永远读不到值**：`Pray Enchantment` 模板自己 `cardTraits = []`、它的 buff 里没有 `trait`；
`trait 1185` 写在**挂它的那张卡**（23 张 SOR）的 `buffEffect` 上 —— 判据文件 §1·2 自己也这么写。
⇒ 本批落法：**列落在「载体卡」身上**，`Attach` 取值次序 = ① 调用方显式传的 `trait` →
② **载体卡**的 `EnchantTrait` → ③ 附魔模板的 `EnchantTrait`（将来真有「模板自带 trait」的附魔时兜底）。
判据理据不变（数据源仍是「附魔效果自带的 trait」），只是「从哪张卡读」这一句按数据实际形状订正。

### 2·3 原版**没有**「摘效果」广播（所以第 21 档是 🔶 自己设计）

`BattleManager.ResolveRemoveEffect` 是 `ResolveAction` **case 0x10 的内联体**（`BattleManager__ResolveAction.c:1640-1738`）：
`CardScript.RemoveEffect` → `CancelAnimFromUnitFromQueue` ×4 → `RemoveDestroyableAnims` → `FinishResolvingAction`。
**全程没有任何 `Broadcast*`**。⇒ 我们这条 `EvtKind.EnchantmentRemoved` 是**为表现层新开的窄口**，
已在枚举成员上打 `// 🔶 自己设计（非原版）：… · 调度台 2026-10-27 裁定`。

### 2·4 🔴 `play check` 我为**什么**判「红不归本批」

事实链：
1. **改前** 07:00:02 `play check` = **rc=0**（指纹 `b671acdb…` 与基线一致）。
2. 改后 07:06 / 07:07 两次 = **rc=1**，当次指纹稳定在 `517be27d…`；①-a 主档 **1 张**（`BL6`）、①-b 电池 **13 张**变了。
3. **同窗口里别的写手在改引擎**（`ls --time-style=full-iso` 实测）：
   `Aura.cs` **07:01:26** · `EffectResolver.cs` **07:03:13** · `RuleCore.cs` **07:05:56**
   （我的改动起点 ≈ 07:02，`play all dump` 两次都在同一批文件上跑）。
4. `git diff --numstat`（只读）：`Aura.cs` **1/1** —— 那一行是
   **`if (t.IsRemnant) return;`（残骸整条不施加 aura）**，而红掉的电池差异**正是**
   `FIX_WAY … {armour1, waystone1}` → `FIX_WAY 0/0/0/1`（残骸上的关键词没了）。
5. **本批的两个引擎改动结构上不可能影响探针**：`grep` 全 `工具/ruleprobe/` ⇒ `AchievementTally` / `Tally` **0 命中**；
   `ctx.Signals` 只被 `A24Probe` / `G2Probe` 读（且都 `Kind != Trigger` 过滤），`PlayProbe` 不读。
6. **`play all dump`（1128 张 × 23 列）改前改后 0 行不同**（1130 行逐行比）。

⇒ **结论**：`play check` 的红 = `Aura.cs` 那行「残骸不施加」造成的，**不要为它重钉 `PinnedPlayMd5`**；
本批不改它（`Aura.cs` 归 `E6ENG`）。**如果**调度台确认那行要保留，则 `PinnedPlayMd5` 需要在同步点按新指纹重钉（那是另一件事）。

---

## ③ 没查清 / 没做到的（如实）

1. **`EnchantmentRemoved` 没有运行时断言** —— 本批白名单只许改 `RuleEngineTest.cs` 的**计数**那一条，
   且 ⛔ 不跑 Unity ⇒ 发射口只有**静态核对 + typecheck**。建议断言（同步点补，放 `RuleEngineTest_EngineX13`）：
   摘成功后 `ctx.Signals` 里有一条 `EvtKind.EnchantmentRemoved`、`CardId == "pray"`、`Amount == 15`、`(Player,Slot)` = 载体格。
   ⚠️ 已核对：`RuleEngineTest_EngineX13.cs` **不读 `Signals`**（`grep Signals` 0 命中）⇒ 本批加的那条事件**不会**让现有 pray 断言变红。
2. **`vulnerable(480)` 今天挂不上** —— 那 4 条引用的附魔模板没进池、也没有执行点
   （`ENCHANTMENT_EXECUTORS` 只收 `Pray Enchantment`）⇒ 列已落，但**能命中的只有 `pray` 那 23 张**。
3. **`traitValue` 不用** —— `RemoveDestroyableAnims` 只拿 trait 当键（判据 §1·④）⇒ 没塞进别处。
4. **`RuleEngineTest.Run` / `BattleScene.Run` 都没跑**（简报禁止 Unity 批处理）⇒ 「第 21 档在 `BattleScene`
   的 `unsourced == 2` 断言下不报警」是**静态推断**（`IsRegisteredEventKind` 走 `ZeroDurationKinds` ⇒ `IsSourced` 回 true），
   建议同步点跑 `BattleScene.Run` 证实。

---

## ④ 改动清单

### ④·0 ⚠️ 白名单外的第 8 个文件：`RuleEngine/Data/CardDatabase.cs`（25 行，纯加）

**为什么必须动**：`CardDef` 只由 `CardDatabase.Parse` 造，`cards_engine.json` 的列**只有它读**。
不加它，`enchantTrait` 永远不会进 `CardDef` ⇒ 整件功能是死数据（而报告也会写成「已接」= 静默失效）。
改动三处、全是**加**：
1. `CardDto` 末尾加字段 `public string enchantTrait;`（`null` = 未知 · `""` = 明确没有 · 名字 = 有）；
2. `Parse` 里加 `def.AttachEnchantTrait(c.enchantTrait);`（紧跟 `AttachKeywordId` —— **同一个「构造后挂载」口**，
   理由与 `B8_AH` 那两条逐字相同：`CardDef` 构造函数下方有 40 处账落点，⛔ 不能往构造函数加参数）；
3. 版本守卫加 `< 33`（缺列 ⇒ 出声，说明「`null` = 未知、不是 `""` = 没有」）。

### ④·1 逐文件

| 文件 | 行数 | 改了什么 |
|---|---|---|
| `工具/gen_cards_engine.py` | +135 / −6 | ① 新常量 `DEFINED_TRAIT_STUB`；② `load_defined_trait_names()`；③ `ENCHANT_TRAIT_BUFFTYPES` + `enchant_trait_of()`（**同一张卡两个不同 trait ⇒ `sys.exit`**，现读 0 张；未知 trait 数值 ⇒ `sys.exit`，不猜名字）；④ 主循环 + 附魔模板那两层各落一列；⑤ 读数行「附魔 trait …」；⑥ **version 32 → 33**。 |
| `RuleEngine/Resources/cards_engine.json` | 1/1（整行） | 重生成产物：**1128 张全带 `enchantTrait` 键**（24 张非空）、`version` 33，**其余 1128 张的其它键一字未变**（逐卡逐键比过）。 |
| `RuleEngine/Core/CardDef.cs` | +32 | **类尾**（`AttachKeywordId` 之后）加 `_enchantTrait` / `EnchantTrait` / `AttachEnchantTrait` —— 零漂移（该类最后一个账落点是 `:6552`）。 |
| `RuleEngine/Data/CardDatabase.cs` | +25 | 见 §④·0。 |
| `RuleEngine/Core/Enchantment.cs` | +61 / −5 | `ActiveCardEffect.Trait`（含三态 doc）· `Attach(..., string trait = null)` + 取值次序 + 未知时**出声** · `Remove` 成功分支 `ctx.Emit(EvtKind.EnchantmentRemoved, …)` · 文件头「0 张卡会挂附魔」那段**就地订正**（v30 起池里有 `Pray Enchantment`、v33 起 23 张 SOR 真会挂）。 |
| `RuleEngine/Core/BattleEvent.cs` | +19 / −2 | 第 21 档 `EnchantmentRemoved`（成员 doc + `🔶 自己设计` 标记）· 文件尾「`EvtKind`（20 个成员）」**就地订正成 21**（并注明条数以计数断言为准）。 |
| `CardPresentation/Core/EventTiming.cs` | +1 / −1 | `ZeroDurationKinds` 加 `EvtKind.EnchantmentRemoved` —— **刻意写在同一行 = 零行数变化**（本文件下游有账按行号引）。 |
| `RuleEngine/Core/AchievementTally.cs` | +30 | `AddTrigger` 里加 20/240/630 过滤 + 类尾三个常量（`TriggerWhileInPlay` / `TriggerWhileInPlayVariable` / `TriggeredSynapse`）。 |
| `RuleEngine/Editor/RuleEngineTest.cs` | +8 / −4 | **只改那一条断言**：`EvtKind` 计数 20 → 21，`Contains` 列表补 `EnchantmentRemoved`。 |

### ④·2 生成前后逐卡差异（`cards_engine.json`）

- `version` 32 → 33 · `count` 1128 → 1128 · **id 集合完全相同**。
- **逐卡逐键比：只有 `enchantTrait` 这一个键变了**（1128 张都新增了该键）。
- **落值 24 张**（其余 1104 张 = `""`）；**共 25 条 buff 行**（`SOR29 Ephrael Stern` 一人两条）：
  `BL74` = `vulnerable`；`SOR1 SOR2 SOR3 SOR4 SOR5 SOR6 SOR13 SOR15 SOR17 SOR18 SOR20 SOR24 SOR25 SOR26 SOR29 SOR30 SOR34 SOR35 SOR37 SOR65 SOR67 SOR68 SOR69` = `pray`。
  生成器读数行现读：「`enchantTrait` 落值 **24/1128** 张 · 25 条 · 分布 `pray` 24 · `vulnerable` 1」。
- **行为差异：0** —— `play all dump` 1130 行 0 行不同（见 §②·4·6）。

### ④·3 🔴 **需要重钉哪几颗指纹**

| 指纹 | 现状 | 结论 |
|---|---|---|
| `Program.PinnedPoolMd5` | 现值 **`cc603636fab13c241014eef53d6109fc`**（基线里钉的是 `ce3faf91f9634053e00f0c97e602bb54`） | **需要重钉这一颗**（我重生成过 `cards_engine.json`） |
| `Program.PinnedParseMd5` | ⑦ 输出指纹 **`c9a64798…` 一致**（解析输出没变） | **不动** |
| `Program.PinnedBaselineMd5` | 基线文件 `基线/out_baseline.txt` 没重生成过 | **不动** |
| `Program.PinnedPlayMd5` | 当次 `517be27d…` vs 钉的 `b671acdb…` | **本批不动**（差额 = `Aura.cs` 残骸那行，见 §②·4）；调度台确认那行要保留时再在同步点重钉 |

### ④·4 待接线清单（交给 `E4BDR` / 同步点）

1. **`CardPresentation/Battle/BattleDriver.cs` 的 `PlaySignal`**（现读 `:22617` 起；`StatusApplied` 那一支**结束于 `:22664`**）
   —— 在它**之后**插一段（原版 ②③ 两段，逐字对照判据文件 §1b）：
   ```csharp
   // 🆕 2026-10-27（账 532 的 1b）：一条附魔效果被摘掉 —— 原版 ResolveAction case 0x10 内联做的两件事
   if (e.Kind == EvtKind.EnchantmentRemoved)
   {
       // ② 原版 CancelAnimFromUnitFromQueue 连走四条动作道 —— **无条件**（不按 buffType 闸）
       CancelPendingAnimsOf(e.Player, e.Slot);
       // ③ 原版 RemoveDestroyableAnims：闸 (buffType - 15) > 1 ⇒ 只认 15/16
       if (e.Amount == 15 || e.Amount == 16) RemoveDestroyableAnims(e.Player, e.Slot, e.CardId);
       return;
   }
   ```
   两个入口都已就位：`CancelPendingAnimsOf(int side, int slot)` = `BattleDriver.cs:34205` ·
   `RemoveDestroyableAnims(int player, int slot, string trait)` = `:33940`。
   ⚠️ **`e.CardId` 里装的是 trait 名**（引擎约定：关键词/名字塞 `CardId`，⛔ 不是 `Keyword`）—— 与 `StatusApplied` 同一条。
2. **断言**（同步点补到 `RuleEngineTest_EngineX13`）：摘成功后 `Signals` 有一条 `EnchantmentRemoved`、`CardId == "pray"`、
   `Amount == 15`、`(Player,Slot)` = 载体格；再一条「`permanentBuff` 拒绝摘 ⇒ **不发**」。
3. **`BattleScene.Run`** 跑一次，确认第 21 档没让 `unsourced == 2` / 未登记计数变红（静态推断不会，但要有读数）。

### ④·5 账补丁

`账/_parts/B9_E7ENG.tsv`（4 行 · 11 列 · `状态` 一律**未动** · `轮次` = `B9_E7ENG`）：

| 主表 | 原版类.方法 | 状态（不动） | 改的是 |
|---|---|---|---|
| 战斗逻辑驱动.tsv:532 | `BattleManager.ResolveRemoveEffect(...)` | C | 差异 + 备注：引擎侧两输入已落地；仍判 C 的唯一原因 = ②③ 消费点未接线 |
| 战斗逻辑驱动.tsv:480 | `BattleManager.RemoveDestroyableAnims(...)` | B | 差异 + 备注：两个输入已补齐；只差 `PlaySignal` 一行 |
| 战斗逻辑驱动.tsv:238 | `BattleManager.CancelAnimFromUnitFromQueue(...)` | B | 差异 + 备注：信号已有，阻塞条件改为「等 `PlaySignal` 接线」 |
| 战斗逻辑驱动.tsv:440 | `BattleManager.LogActivatedTrigger(...)` | B | 差异 + 备注：引擎侧 20/240/630 过滤已补 ⇒ 与挑战口径一致 |

`python -I -X utf8 merge_ledger_parts.py --dry --only B9_E7ENG` ⇒ **更新 4 · 新增 0 · 无变化 0**，无报错；
`check_patch_conflicts.py` ⇒ **没有跨补丁重复键**。

### ④·6 🔴 本批造成的账**行号漂移**（同步点用 `check_ledger_points --all-A --fix` 收）

`check_ledger_points.py`（只读）现读 **漂移 473 条**（**大部分是别人在飞的改动**），其中**归本批的 18 条**：

| 主表 | 方法 | 账里 | 实际 |
|---|---|---|---|
| 战斗逻辑驱动 | `BattleManager.AddEnchantment(CardEffect newEffect)` | 208 | **228** |
| 战斗逻辑驱动 | `EntityScript.HasEnchantment(RawCardScript)` | 329 | **385** |
| 战斗逻辑驱动 | `EntityScript.HasSecretOrders()` | 527 | **583** |
| 战斗逻辑驱动 | `EntityScript.HasQuests()` | 540 | **596** |
| 战斗逻辑驱动 | `EntityScript.HasSecretOrder(RawCardScript)` | 582 | **638** |
| 战斗逻辑驱动 | `EntityScript.GetQuestTimes()` | 794 | **850** |
| 战斗逻辑驱动 | `BattleManager.RemoveEnchantment(...)` | 922 | **978** |
| 战斗逻辑驱动 | `BattleManager.SpendEnchantment(CardScript actingCard…)` | 362 | **418** |
| 战斗逻辑驱动 | `EntityScript.SpendEnchantment(RawCardScript)` | 362 | **418** |
| 战斗逻辑驱动 | `BattleManager.UsedEnchantment()` | 384 | **440** |
| 战斗逻辑驱动 | `EntityScript.IsSpentEnchantment(RawCardScript)` | 384 | **440** |
| 日常_奖励 | `ChallengeLogMgr.LogPlayTactic(...)` | 195 | **210** |
| 日常_奖励 | `ChallengeLogMgr.LogPlayTroop(...)` | 195 | **210** |
| 未分类_其他 | `CardPlayedCount.OnCardPlayed(...)` | 195 | **210** |
| 未分类_其他 | `AddSabotageCard.OnAddCard(...)` | 216 | **231** |
| 未分类_其他 | `GiveHuntsMark.OnAddCardEffect(...)` | 222 | **237** |
| 未分类_其他 | `GiveMarkOfChaos.OnAddCardEffect(...)` | 228 | **243** |
| 未分类_其他 | `UnitReanimated.OnUnitReanimated(...)` | 234 | **249** |

（`CardDef.cs` / `CardDatabase.cs` / `EventTiming.cs` / `BattleEvent.cs` **0 条漂移** —— 前两个的加行全在类尾 / 零行数变化，后两个的落点不在 A 档。）

### ④·7 顺手发现（⛔ 未改，只报）

1. **`SOR24` 卡名与真值 SO 不一致**：`cards_engine.json` = **`Simulacrum Celestian`（天界模拟体）**，
   而 `cards_raw.json` 的 `cardName` = **`Simulacrum Imperialis`**（`m_Name = 23 Sororitas-Simulacrum Imperialis`）。
   按铁律 7 该拿 PnP 成品图核（这是一张会挂 `pray` 的卡）⇒ 若不是我们有意改的名，就是卡表错一条。
2. **`Enchantment.cs` 文件头那段「今天卡池 0 张卡会挂附魔」是过期陈述**（v30 起池里有 `Pray Enchantment`）——
   本批已**就地订正**（不是顺手发现，是同一条铁律 5 的动作，记在这里备查）。
3. **`BattleEvent.cs` 末尾长注释里「`EvtKind`（20 个成员）」已同步改成 21**（同一条铁律 5）。

---

## ⑤ 复跑命令（给同步点照抄）

```bash
TMPDIR=/tmp/wf_E7ENG bash d:/4/Unity/工具/typecheck.sh          # 期望 0/0
bash d:/4/Unity/工具/ruleprobe.sh check                          # 期望 rc=0（解析差异 0 行）
bash d:/4/Unity/工具/ruleprobe.sh play check                     # 本批不改变它（红/绿看 Aura 那行的裁定）
bash d:/4/Unity/工具/ruleprobe.sh play all dump /tmp/after.tsv    # 与 _tmp_view/e7eng/play_all_before.tsv 应是 0 行差异
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py          # 看漂移 473 → …（本批 18 条会被 --fix 收掉）
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B9_E7ENG   # 合并本批账补丁（⚠️ 合并后把本补丁归档）
```

本批中间产物（可复查）：`_tmp_view/e7eng/play_all_before.tsv` · `play_all_after.tsv` · `cards_engine_before.json` ·
`diff_engine.py`（逐卡逐键比生成前后）· `e7eng_probe_trait.py`（真值全表扫 trait）· `ledger_points.txt`。

---

# 第二批 · 订正与立账（2026-10-27 · 协调台追派的三小件）

## ⓪ 先解「全池都变」那个谜 —— 结论：**不是我的卡池，是 `E5ENG` 给探针加了两列**

问的那句先答实：

| 项 | 值 | 判据 |
|---|---|---|
| `Program.PinnedPoolMd5`（钉住的旧值） | **`8aa37475268b685929c113e925f613ad`** | `工具/ruleprobe/Program.cs:251` |
| 现池 `cards_engine.json` 的 md5 | **`cc603636fab13c241014eef53d6109fc`** | 直接算文件字节 |
| 我重生成**之前**那一份的副本（`_tmp_view/e7eng/cards_engine_before.json`） | **`8aa3747…`**（与钉住值**逐字符相同**） | 直接算文件字节 |

⇒ **卡池指纹的变化确实是我**（v32→v33 加 `enchantTrait` 列）。

🔴 **但「全池 1128+586 张都变」不是它造成的**，直接证据两条：

1. **我自己的改前/改后 `play all dump`（23 列那版）= 1130 行、0 行不同**（`_tmp_view/e7eng/play_all_before.tsv` ↔ `play_all_after.tsv`）。
   如果新列真的动了每张卡的结算，这 1128 行不可能逐字节相同。
2. **现在再跑一次 `play all dump`，表头多了两列**：`atkCount` · `exhausted` ——
   `工具/ruleprobe/PlayProbe.cs` 的 mtime = **2026-10-27 07:11:56**（E6ENG 报「07:11 全池都变」就在这一分钟）。
   逐行验证：**新表前 23 列与旧表逐字相同的行 = 1129 / 1129**（非空行全部），差异**只有**末尾那两列。

⇒ **结论**：`E6ENG` 看到的「全池都变」= **`PlayProbe` 新增 `atkCount` / `exhausted` 两列**（列集合变了 ⇒ 逐卡对拍全错位），
与我的卡池重生成**无关**、与任何结算行为**无关**。
⇒ 同步点仍要重钉 `PinnedPoolMd5 → cc603636…`（这是真的卡池变化），**但重生成 play 基线时要意识到**：
基线文件若按旧 23 列生成，重生成前必须先确认 `PlayProbe` 的列集合已定版（否则基线里会缺/多两列）。

## ① 判据文件就地订正（已改）

`资料/完全复刻/判据/T4_532与PremiumCampaign_判据.md` §③·1a：
- 在方案 A/B 表后加了 **「⚠️ 2026-10-27 现核订正（执行代理 `E7ENG`）」** 引用块 —— 保留原表原文，写明
  「读 `enchantmentCard.EnchantTrait` 按字面会永远读到空」的原因（模板 `cardTraits=[]`、`trait` 在挂它的那张卡上，
  本文件 §1·2 自己也这么写），以及**现行落法三条**（列落载体卡 / 取值次序 显式实参→载体卡→模板 / `null` 与 `""` 的区别）。
- 同时把「建议的具体落点」第 4 条那句错的（`走 enchantmentCard.EnchantTrait`）**就地改成**「走**载体卡**的 `CardDef.EnchantTrait`（⚠️ 不是 `enchantmentCard`，见订正）」
  —— 避免文档里两套说法打架（铁律 5）。

## ② `SOR24` 卡名核对 —— **我们的卡表是对的，真值 SO 的 `cardName` 是孤例**

**做法（铁律 7）**：直接看 PnP 成品图 `d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png`（900×1200，亲读）。

**卡面印的**：`Simulacrum Celestian` · 费用 **5** · 近战 **5** · 远程 **4** · 生命 **6** · `Infantry` ·
`Pray: Give +1 [近战图标] and +1 [远程图标] to all friendly units`。

**逐项对我们的 `cards_engine.json`（SOR24）**：

| 项 | 卡面 | 我们的表 | |
|---|---|---|---|
| 名 | `Simulacrum Celestian` | `Simulacrum Celestian` | ✅ |
| cost / attack / ranged / health | 5 / 5 / 4 / 6 | 5 / 5 / 4 / 6 | ✅ 全对 |
| subtype | `Infantry` | `Infantry` | ✅ |
| desc | `Pray: Give +1 [Attack] and +1 [Ranged] to all friendly units` | 同（方括号 token 是我们的写法） | ✅ |
| `enchantTrait` | —（本批新增） | `pray` | ✅（这张卡就是「挂祈祷附魔」的那一族） |

**结论**：`cards_raw.json` 的 `cardName = "Simulacrum Imperialis"`（`m_Name = 23 Sororitas-Simulacrum Imperialis`）
与**印刷品**不符 ⇒ **那是 SO 里的旧名/错名，不是我们抄错了**。
⛔ **卡表不改**（协调台已明确：改卡表走正规通道）。建议：把这条记成「真值 SO 的 `cardName` 不可当卡名判据」的一个实例
（`cards_raw` 的 `cardName` 本来就不是卡名权威 —— 铁律 7 的判据是 PnP 成品图）。

## ③ 新账 · `OnTrapDrawn`（trigger 250 `ThisCardDrawnAsTrap`）

> 🔴 **先说一件会改变处置方式的事**：主表里**已经有这两行**，⛔ 别新开重复行（铁律 6）：
> - `战斗逻辑驱动.tsv:1272` `RawCardScript.OnTrapDrawn(BattleManager manager, CardScript thisCard)` —— **状态 D**，备注写着「本批没有为它建 Kind」；
> - `战斗逻辑驱动.tsv:703` `CardScript.CardDrawn(CardScript actingCard)` —— **状态 B**，差异里就写着「`trap+armed ⇒ OnTrapDrawn(250)` 那一支没接」。
>
> ⇒ 本节的建议 = **把下面的增量并进 703 / 1272**（703 的 B 不必变；1272 的 D 依据要补一条「`isTrapCard` 的定义」），
> 而不是新开一行。本批**没有**动主表（白名单禁止）。

### ③·1 原版做法（逐句 + 出处，全部本批亲读）

**(a) 250 的唯一入口 = 「抽牌广播」的第 1 跳（预调）**
`BattleManagerSupport__BroadcastCardDrawn.c` 第 1 件事：
```c
CardScript__CardDrawn(param_2, param_2, 0);      // param_2 = 被抽到的那张牌 —— 「自己调自己」
```

**(b) `CardScript.CardDrawn(self, drawer)` 的三条分支**（`decomp_full/CardScript__CardDrawn.c`，全文 36 行）
```c
cVar2 = UnityEngine_Object__op_Equality(param_2 /*drawer*/, param_1 /*self*/, 0);
if (cVar2 == 0) {                       // ① drawer ≠ self（别人抽到的）
    否则 rawCard.OnCardDrawn(bm, self, drawer)     ⇒ OnTrigger(0x104 = 260 OtherCardDrawn)
} else {                                // ② drawer == self（自己抽到的）
    if (self.isTrapCard && *(char*)(self + 0x54) /*armedTrap*/ != 0) {
        self.rawCard.OnTrapDrawn(bm, self)          ⇒ OnTrigger(0xfa = 250 ThisCardDrawnAsTrap)   ← **:23**
        return;
    }
    rawCard.OnCardDrawn(bm, self, self /*drawer 被改写成 self*/) ⇒ 260
}
```
- 判据行号：**`CardScript__CardDrawn.c:23`**（协调台给的出处，本批现读核对无误）。
- ⚠️ **Ghidra 的 `op_Equality` 陷阱**：`UnityEngine_Object__op_Equality(param_2, param_1, 0)` 这里**两个实参都在**
  （同族坑见 CLAUDE.md：Ghidra 常把 `op_*` 的第二实参吞掉，看到「只有一个参数」不能判「它没用」）。
- `rawCard == null` 的支路是 **NRE 桩**（`FUN_1803f47a0`）⇒ 我们不复制崩溃语义。

**(c) `isTrapCard` 的定义（🔴 21 条账里**没有**一条写过它 —— 本批新增的硬知识）**
`decomp_full/EntityScript__get_isTrapCard.c`（全文 11 行）：
```csharp
RawCardScript raw = self.rawCard(+0x28);
return raw.cardType(+0x94) == 0x14 /*20*/ && raw.spellType(+0x90) == 0x46 /*70 = SpellType.Trap*/;
```
⇒ **`isTrapCard` = `cardType == 20` ∧ `spellType == 70`**（`SpellType.cs` 现读：`Trap = 70`）。
⚠️ **2026-10-27 订正（第三批现核，铁律 5）**：本节初稿写「`cardType 20` = 隐藏 Whispers/附魔模板那一档、与陷阱共用 cardType 20」—— **错了**。
`CardTypeOptions`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs` 现读）=
**`Minion = 0` · `Hero = 10` · `Tactic = 20` · `Whispers = 40`**
⇒ **`cardType 20` = `Tactic`（战术卡），不是 Whispers**。所以 `isTrapCard` 的真实语义 = **「战术卡里 `spellType == 70 (Trap)` 的那些」**。
两个连带事实：① 生成器里「`cardType == 20`」被用来筛附魔模板，是**宽松筛**（模板本来就是战术型卡）—— `gen_cards_engine.py:3954` 自己写着「`cardType 20` 不是附魔专用型，全表 680 张都是 20」；
② 我们卡表的 `type` 字符串（`unit/tactic/hero/defence/enchantment`）**不是** cardType 的忠实表示（`Pray Enchantment` 真值 `cardType = 20` 而我们的 `type = "enchantment"`）⇒ `IsTrapCard` **必须**靠真值 `cardType` 列算，⛔ 不能拿 `Type` 字符串反推。

**(d) `armedTrap(+0x54)` 的唯一写点**
`CardScript__SetAsActiveTrap.c:16` = `*(undefined1 *)(param_1 + 0x54) = 1;`（外加一句 Log），
唯一调用者 = **`BattleCardManager__CreateCard`** ⇒ **陷阱卡一被造出来就是「已布设」**，原版没有单独的「布设动作」。
（与账 883 一致，本批复核。）

**(e) 250 与 270 是**并行**的两条后续，不是替代关系**
`BattleManager._ResolveDrawCard_d__432__MoveNext.c:283-295`：
```
:285  BroadcastCardDrawn(bm, drawnCard)          ← 里面第 1 跳发 250（若陷阱）
:288  if (drawn.armedTrap != 0) {
:289      BroadcastTrapResolved(bm, drawnCard)  ← 另一族：270（账 217 / 941）
:291      CemeteryManager.AddDrawTrapAction(...)  ← 战斗日志那一行
:295  SetCardTurnDrawn(drawnCard)
```
⇒ 抽到一张已布设的陷阱：**250 一次 ＋ 270 一族 ＋ 日志一行**，三件都发生。

### ③·2 我们现状（逐条核过）

| 段 | 原版 | 我们 | 差什么 |
|---|---|---|---|
| 250 名字表 | `AbilityTrigger.ThisCardDrawnAsTrap = 250` | ✅ `CardDef.cs:1928` `{"ThisCardDrawnAsTrap", 250}` | — |
| 抽牌广播三趟 | 预调 / 当前手牌两遍 / 棋盘（含原版那个漏 `!` 的 bug） | ✅ 已按原版落（`EffectResolver.DrawHandHopTakes` + `DrawnNow`，账 627 · `E6ENG`） | — |
| **250 的发射点** | `CardScript.CardDrawn` 的 ②-b 支 | 🔴 **0 个** —— 全仓 `grep ThisCardDrawnAsTrap` 只命中 `CardDef.cs` 的名字表；`WhenEventKind.Draw` 恒映射到 `OtherCardDrawn(260)`（`EffectResolver.cs:7383`） | **就是本条** |
| `CardDef.IsTrapCard` | `EntityScript.get_isTrapCard` | 🔴 **没有这一格**（`CardDef` 只有 `SpellType`；`cardType` 只在生成期用来分流） | 缺数据位 |
| `armedTrap` | `+0x54`（`CreateCard` 时置 1） | 🔴 **没有这一位**（`ConditionEval.cs:188-189` 已如实标） | 缺数据位 |
| 陷阱机制本身（布设 / 造牌 / 结算） | `SetAsActiveTrap` / `AddActiveTrapToHand` / `TrapResolved` | 🔴 **整条没有**（账 217 / 883 / 941 全判 D） | 不在本条范围 |

**可达性（本批独立复核，两个源都扫）**：
- `cards_raw.json`（1,351 张真值快照）**`spellType == 70` = 0 张**（`cardType==20 ∧ spellType==70` 也是 **0**）；
- `cards_engine.json`（池 1,128 张）`spellType` 现读分布里**没有 70**（`0/90/150/180/190/210/220/230/240/260/265/270` 与 `defence` 的 210）。
⇒ **`isTrapCard` 对全池恒 `false` ⇒ 250 今天恒不可达**（这是**数据事实**，⛔ 不是「我们没做所以不可达」）。

### ③·3 施工建议（**本批只写判据，⛔ 不写代码**）

1. **数据位两格**（先做，否则后面全是死代码）：
   - `CardDef.IsTrapCard`（三态？不需要 —— 原版是纯函数 `cardType==20 ∧ spellType==70`，可由已有的
     `SpellType` + 生成期的 `cardType` 直接算；但要**把 `cardType` 也落进卡表**或**在生成期算好落一列 `isTrapCard`**，二选一，
     ⛔ 别在运行期拿 `Type == "enchantment"` 猜 —— 那是我们自己的映射）。
   - 实例位 `CardInstance.ArmedTrap`（0/1，唯一写点 = **造牌那一刻**，等价于原版 `CreateCard → SetAsActiveTrap`）；
     我们今天的「造牌」有多个入口（`RuleCore.PlayCard` / `DeployFree` / `EffectResolver.DoCreate` / 起手发牌 …），
     **每一个造牌入口都要显式置位**（与 §10 第 5 条同一个坑：多入口的状态要在每个入口显式设置，并各配一条断言）。
2. **250 的发射点**：`RuleCore.Draw` 的广播链里，**只对被抽的那一份**（现成的 `DrawnNow(ctx, kind)` / hop 0）
   判 `IsTrapCard && ArmedTrap` ⇒ 发 250 **而不是** 260；其余所有跳（棋盘 / 手牌两遍）仍发 260。
   ⚠️ **顺序**：原版 250 在 `BroadcastCardDrawn` 的**第 1 跳**、棋盘跳之前
   （`EffectResolver.cs:16450` 已如实标「我们这一跳排在棋盘之后」的已知次序偏差 ⇒ 接 250 时**顺手把预调提前**）。
3. **并行的第二族（270）不在这里做**：`BroadcastTrapResolved` + `AddDrawTrapAction` 的整条施工单已在
   `账 217` 的备注里，⛔ 别在这条里重复实现（两处写同一条规则 = 迟早不一致）。
4. **断言（人造夹具，池内 0 张 ⇒ 只能人造）**：
   - 前提断言先写：夹具卡 `SpellType == 70` ∧ `ArmedTrap == 1`（**硬检查**，⛔ 别写成 `if (前提) Check(结论)` —— 那会空转绿）；
   - 抽它 ⇒ `Signals` 里 **恰一条** `250`、**零条** `260`（灭自证：把发射点改回 260 ⇒ 红）；
   - 反面：同一张卡 `ArmedTrap = 0` ⇒ 走 **260**、零条 250；
   - 再一面：非陷阱卡抽到 ⇒ 260（防止「所有抽牌都变成 250」）。
5. **可达性分级**：这是**休眠机制**（铁律 11：休眠也要做），但**不是「原版本身没有」**（原版有活调用点
   `BroadcastCardDrawn` → `CardDrawn`）⇒ 排在「陷阱数据/机制出现之后」，⛔ 不许以「池内 0 张」为由判「不做」。
   ⚠️ 与 `账 1272` 现有 D 的关系：那条 D 的**时间限定**（「数据出现需重跑」）正是这条 5；本批新增的只是
   **`isTrapCard` 的定义** 与 **`armedTrap` 唯一写点** 两条硬知识 —— 建议并进 1272 的「判据要点/备注」。

---

# 第三批 · 两格数据位（`CardDef.IsTrapCard` ＋ `CardInstance.ArmedTrap`）· 2026-10-27

## ① 结论

两格数据位都落地了，**250 的发射 / 270 那一族 / 预调次序一律没动**（按协调台划定）。

| 格 | 落点 | 契约 |
|---|---|---|
| `CardDef.CardType` | `CardDef.cs` **类尾**（`AttachCardType` 构造后挂载） | `int`，原版 `RawCardScript.cardType`（`+0x94`）= `CardTypeOptions`：`Minion 0 / Hero 10 / Tactic 20 / Whispers 40`。**`-1` = 未知**（卡表 < v34 / 这张卡不在真值快照里），⛔ 不是 `Minion(0)` |
| `CardDef.IsTrapCard(out string why)` | 同上 | `bool?` **三态**：`true` / `false`（判得出）/ `null`（判不了，`why` 说明缺哪一格）。判据 = 原版 `EntityScript__get_isTrapCard.c`：**`cardType == 20 (Tactic)` ∧ `spellType == 70 (Trap)`** —— 现算，不缓存 |
| `CardInstance.ArmedTrap` | `CardInstance.cs` 类尾 + **构造函数一行** | `int` 三态：`1` 已布设 / `0` 判得出不是 / `-1` 判不了（`ArmedTrapUnknown`）。唯一写点 = **实例出生那一刻** |

**读数（本批实跑）**：
- `typecheck` **0/0**（改完 `CardDef` / `CardInstance` / `CardDatabase` / 生成器后跑）。
- 生成器 v33 → **v34**；`cards_engine.json` 重生成；**新池 md5 = `c2ee753d96ad11ea548843c861fd8890`**（上一版 = `cc603636fab13c241014eef53d6109fc`，同步点按新值重钉 `PinnedPoolMd5`）。
- **逐卡逐键比（v32 备份 ↔ v34）：只有 `enchantTrait` / `cardType` 两键 + 顶层 `version` 变了**，`id` 集合相同、`count` 1128 不变。
- **`play all dump` 改前 / 改后 = 1130 行、0 行不同**（`_tmp_view/e7eng/play_all_now.tsv` ↔ `play_all_v34.tsv`）⇒ 新列对结算层零影响。
- `ruleprobe check`：rc=0、解析差异 0 行、⑦ 输出指纹 `c9a64798…` **一致**；唯一哨兵告警仍只是「卡池 md5 与钉住的不同」（就是上面那个新值）。

## ② 🔴 判据订正（本批现核，铁律 5 · 已在 §③ 就地改掉）

上一节我写「`cardType 20` = 隐藏 Whispers/附魔模板那一档」—— **错**。
`CardTypeOptions.cs` 现读：**`Minion = 0` · `Hero = 10` · `Tactic = 20` · `Whispers = 40`**
⇒ **`cardType 20` = `Tactic`（战术卡）**，`Whispers` 是 **40**。
所以 `isTrapCard` 的真实语义 = **「战术卡里 `spellType == 70 (Trap)` 的那些」**。
（生成器里「`cardType == 20`」被用来筛附魔模板是**宽松筛** —— `gen_cards_engine.py:3954` 自己写着「`cardType 20` 不是附魔专用型，全表 680 张都是 20」。）

## ③ 造牌入口普查（协调台要求「先列出全部，再逐个置位」）

**结论先说**：`armedTrap` 的等价写点**只有一处**，而且**不该**在「搬牌」的入口重复置位。

**(a) 实例出生点 = 1 个构造函数（2 个工厂都走它）**
```
new CardInstance(                       ← 全仓只有 2 处（grep 现读）
  ├─ BattleContext.cs:702               // NewInstance：真对局，正号 id（_nextInstanceId++）
  └─ CardInstance.cs:420                // Detached：无对局的场合（测试/演示/预览），负号 id
```
两处都 `new CardInstance(card, id)` ⇒ **构造函数 `CardInstance(CardDef, int)` 是唯一的出生点**。

**(b) `ctx.NewInstance(...)` 的生产调用点 = 20 处**（现读；它们都经 (a) 的构造函数）：
- `RuleCore.cs` **10 处**：`574`（抽牌）· `622`（初始牌库）· `631`（督军）· `750` ·
  `1136`（给牌）· `4284`（复制品）· `4509` · `4966`（`DeployFree` 凭空造）· `13504` / `13543`（天赋生成）
- `EffectResolver.cs` **8 处**：`3415`（造到手上）· `3717`（造到场上）· `3846` · `4022` ·
  `4721` · `4970`（复制）· `6537`（再造）· `13130`（第 7 行第 2 步「加进去的是新造的一份」）
- `TutorialScript.cs` **2 处**：`1477`（教程摆单位）· `1538`（教程塞牌库）

**(c) 为什么不在这些入口各自置位**
原版唯一写点 = `CardScript__SetAsActiveTrap.c:16`，**唯一调用者 = `BattleCardManager__CreateCard`**（VA 直调）
⇒ 原版语义是「**造出来那一刻**算已布设」，**不是**「每次进场/每次抽到都置一次」。
而上面 20 处全都是**造一张新牌**（都会经构造函数）；`PlayCard` / `DeployFree(fromHand)` / `Draw` /
`Reanimate` 那些则是**搬同一份已有实例**（不新建）—— 在那里置位会在「同一张陷阱被造一次、抽多次」
或「基地里反复进出」时与原版不一致（原版只在造牌那一次置，且**没有任何清零点**：
`EntityScript__set_armedTrap` 在 `decomp_full/` 全树无按名调用点）。
⇒ **一个写点覆盖全部造牌入口**，这就是「每个入口都置位」的忠实形态；我又加了一层静态保护：
`ArmedTrap` 的 setter 是 `private`、全仓 `grep "ArmedTrap ="` 只命中构造函数那一行。

## ④ 改了哪些文件

| 文件 | 行数 | 内容 |
|---|---|---|
| `RuleEngine/Core/CardDef.cs` | +70 | 类尾：`CardTypeMinion/Hero/Tactic/Whispers` 常量、`SpellTypeTrap = 70`、`_cardType` + `CardType` + `AttachCardType`、`IsTrapCard(out why)`（三态 + 判据出处） |
| `RuleEngine/Core/CardInstance.cs` | +48 / −0 | 构造函数加一行 `ArmedTrap = TrapArmedOf(card);`（**该行在 ctor 声明之后**，`账` 引用的 `CardInstance.cs:395` **没漂**）；类尾：`ArmedTrapUnknown`、`ArmedTrap`（三态 doc，含「唯一写点 / 无清零点 / 别在搬牌入口再置」）、`TrapArmedOf` |
| `工具/gen_cards_engine.py` | +38 / −2 | `cardType` 列（主循环 + 附魔模板各一处，`truth_int(trow/r, "cardType", -1)`）；**version 33 → 34**；读数行「`cardType` 落值 … · `IsTrapCard` 池内命中 N 张」 |
| `RuleEngine/Resources/cards_engine.json` | 1/1（整行） | v34 重生成（新 md5 见 §①） |
| ⚠️ `RuleEngine/Data/CardDatabase.cs` | +30 | **白名单外，理由同上批**：`CardDto.cardType = -1` · `def.AttachCardType(c.cardType);`（紧跟 `AttachEnchantTrait`）· `< 34` 版本守卫。不接这一跳，新列永远读不到 ⇒ 两格数据位全是死数据 |

**数据读数（生成器现打）**：
```
卡牌类型     `cardType` 落值 1128/1128 张 · 未知 0 张（分布 0:586 · 10:56 · 20:486）
             `IsTrapCard`（= cardType 20 ∧ spellType 70）池内命中 0 张（⇒ 陷阱机制今天恒不可达，CardInstance.ArmedTrap 全池为 0）
```
- 池内 **没有 `cardType == 40 (Whispers)`**（Whispers 卡不进可打卡池）；
- **附魔模板 `Pray Enchantment`**：`cardType = 20` ∧ `spellType = 0` ⇒ `IsTrapCard` 判得出 **`false`**
  （**不是未知** —— 这点重要：说明「战术型但不带 Trap」这一档走的是 `false` 而非 `null`）；
- **陷阱命中 0 张** ⇒ 全池 `ArmedTrap` 恒 **`0`**（判得出，不是 `-1`）。

## ⑤ 断言（⚠️ 未加：本波 `RuleEngineTest.cs` 归 `E5ENG`）— 建议照抄

```csharp
// 1) 数据位本身（不需要对局）
Check(pool.First(c => c.Id == "SOR24").CardType, 20, "★ `cardType` 列已落（SOR24 = Tactic 20）");
Check(pool.First(c => c.Id == "SOR24").IsTrapCard(out _), false, "★ 战术卡但 spellType=0 ⇒ IsTrapCard 判得出 false（不是 null）");

// 2) 造牌那一刻置位（灭自证：把 ctor 那行删掉 ⇒ 红）
var trap = new CardDef("FixtureTrap", "FixtureTrap", "tactic", "", "common", "Test", 1, 0, 0, 0, null, subtype: "Infantry");
// ⚠️ `CardDef` 的 `cardType` 只能走 `AttachCardType`（构造后挂载）——夹具必须显式挂，否则 IsTrapCard 是 null
trap.AttachCardType(CardDef.CardTypeTactic);
// `spellType` 走构造参数 `spellType: 70`
var ctx = ProbeBattle(...);
var inst = ctx.NewInstance(trap);
Check(inst.ArmedTrap, 1, "★ 造出来即「已布设」（= 原版 CreateCard → SetAsActiveTrap）｜🧨 删掉 ctor 那一行 ⇒ 本行红");

// 3) 未知不许当 0（三态契约）
var bare = new CardDef("FixtureNoCardType", "FixtureNoCardType", "tactic", "", "common", "Test", 1, 0, 0, 0, null, spellType: 70);
Check(ctx.NewInstance(bare).ArmedTrap, CardInstance.ArmedTrapUnknown, "★ 卡表没 `cardType` ⇒ ArmedTrap = -1（⛔ 不是 0）");
Check(bare.IsTrapCard(out var why) == null && !string.IsNullOrEmpty(why), "★ 判不了时必须给出 why");
```

## ⑥ 顺手发现（⛔ 未改，报给 `E6ENG` / 协调台）

1. **`E6ENG` 的 250 发射口已经按同一套 API 写好了**：`EffectResolver.cs` 的
   `EmitTrapDrawnInsteadOfCardDrawn`（现读 `:16519` 起）读的正是 `inst.ArmedTrap` /
   `CardInstance.ArmedTrapUnknown` / `inst.Card.IsTrapCard(out why)` —— 与本批实现**逐字对上**
   （`typecheck 0/0` 已验证；这也说明两条线用的是同一份接口约定，没撞车）。
2. ⚠️ **那条 `ArmedTrap == -1` 的 `LogWarning` 是「每次抽到都喊」**（`EffectResolver.cs:16524-16532`）。
   夹具卡（`new CardDef(...)` 不挂 `cardType`）全是 `-1` ⇒ **测试里每抽一张牌就刷一条警告**。
   建议改成「每张卡一次」的闩（或只在 `drawMode` 首次时喊）。这不影响判定（本仓 `grep LogAssert` = 0 命中，
   警告不会让断言红），只是噪音。
3. `Pray Enchantment` 的 `cardType = 20` 会让「按 `cardType == 20` 数附魔模板」的**任何统计**把普通战术卡一起数进去
   （生成器那条注释早就写了「全表 680 张都是 20」）⇒ 以后要数附魔模板，判据仍是 `cardType 20 ∧ 名字含 `_enchantment``
   或 `type == "enchantment"`，⛔ 不能只用 cardType。

## ⑦ 复跑命令

```bash
TMPDIR=/tmp/wf_E7ENG bash d:/4/Unity/工具/typecheck.sh          # 期望 0/0
bash d:/4/Unity/工具/ruleprobe.sh check                          # 期望 rc=0；卡池 md5 = c2ee753d…
bash d:/4/Unity/工具/ruleprobe.sh play all dump /tmp/after.tsv    # 与 _tmp_view/e7eng/play_all_now.tsv 应是 0 行差异
python -I -X utf8 d:/4/Unity/工具/gen_cards_engine.py --check     # 期望「一致 ✅」（v34）
```

---

# 第四批 · `cardType` 判据点普查（只读）· 2026-10-27

**产出** → `资料/完全复刻/判据/cardType判据点普查.md`（逐条 `文件:行` + 判定 + 准确修法）。
**范围**：全仓 `Assets/**/*.cs`（grep `cardType|CardType` **409 处**，其中**代码行 96**）＋ `工具/gen_*.py`（**21 处**）；⛔ 本批**只查不改**。

## ① 决定性事实（新分布不会让既有判断变错）
`CardTypeOptions` = **`Minion 0 / Hero 10 / Tactic 20 / Whispers 40`**；池内（v34 现读）`type × cardType` 是**完全双射**：

| `type` | `cardType` | 张数 |
|---|---|---|
| `unit` | 0 | 586 |
| `hero` | 10 | 56 |
| `tactic` | 20 | 446 |
| `defence` | 20 | 39 |
| `enchantment` | 20 | 1 |
| `whispers` | 40 | **0** |

⇒ 两条：
1. **拿 `Type` 字符串当代理的写法今天逐值等价**（不会因新列突然判错）；
2. 🔴 **误筛方向永远是「多筛」**：`cardType == 20` 会**同时命中 tactic/defence/enchantment（486 张）** —— 用 20 代表某一种 `type` 会多筛 **40 张**。

## ② 判定结果汇总
- **✅ 14 处判得对、不需要改**（含本批新增的两处）：`TypeGatePasses`（唯一把 `tactic|defence|enchantment` 三档都写进 `IncludeSpells` 的地方）· `WhenEvent.ActingCardTypeMatches` 主体 · `AiTargets.CardTypeOf`（真值优先 + `Type` 兜底）· `CardView.IsUnit` / `CardCriteria` / `DeckRules.IsWarlordCardType` / `BattleDriver` 的 `!IsUnit` / `CanUseVOs` · `CardFaceRules.CardTypeWhispers` / `CardFaceAssertE24` 的 defence=20 断言 · 各处 hero 断言。详见普查 §2。
- **⚠️ 6 处有风险/可改进**（普查 §3，逐条给了修法）：
  1. **`CampaignScriptRunner.CardTypeOf`（`:377-380`）= 桩，一律返回 0** —— 「值 + 分支」**两件都没接**（`ShowRadioMessage` 的第四参我们只进日志）。**今天行为影响 = 0，日志值是错的**；接线时两件一起补，修法 = `card.CardType >= 0 ? card.CardType : (0 + warning)`。
  2. **`WhenEvent.ActingCardTypeMatches` 漏 `enchantment`**（`ActingTactic` 只认 `tactic|defence`），而附魔模板真值 `cardType = 20` ⇒ 口径差；**可达性 0**（附魔卡从不作为 acting card）。
  3. **`AiTargets.CardTypeOf` 的 `else → 20`** 会把将来的 `whispers(40)` 判成 Tactic（今天 0 张）；修法 = 优先 `card.CardType`。
  4. `EffectResolver.cs:2017` `tacticCaster = !IsUnit` —— 今天等价、语义偏；可选改成 `CardType == 20` 优先。
  5. `DeckRuntime.cs:2457-2458` 卡组计数只有 unit/tactic 两格 —— `defence`/`enchantment` 落不进；**可达性 0**（`defence` 全 39 张 `inventoryOptions=5 CantAddToDeck`、`enchantment` `=10 NotInInventory` ⇒ 进不了 `CardIds`）。
  6. **生成器把 `cardType == 20` 当「附魔模板」的宽松筛**（`:4142/:4145/:4166/:5194-5195`）—— 逻辑**没错**（实测 **120 张名字含 `enchant`**：`cardType 20` **118** ＋ `cardType 0` 2 张兄弟卡；`cardType 20` 里 **562 张**非 enchant 名被名字挡掉），**但注释文案会误导**（我自己就先误读成「20 = 附魔型」）⇒ 建议文案改成「`Tactic(20)` ∧ 名字含 `enchant`」，并给 `:3954` 那句「全表 680 张都是 20」补上「**池内 486**」。

## ③ 顺带（已转给 `E6ENG`）
`ArmedTrap == -1` 的 `LogWarning` 每次抽牌都喊（夹具卡全是 -1 ⇒ 测试刷屏）—— 协调台已转 `E6ENG` 改成「每张卡一次」的闩。本批未动该文件。

## ④ 查不到 / 边界
- 只查「**我们代码里按 cardType 分支**」的地方；**没有**逐条回原版复核「每处该不该用 cardType」（那是逐方法账）。
- `ai_scoring.json`（1,351 行，`20:680 / 0:605 / 10:66`）与本批新列**同源不同载体**；本批只核分布一致，**没**逐卡 join。
- `defence` 不能进卡组只核了**数据位**，没核 `DeckRules.CannotAddToDeck` 的运行时路径。
- `d:/2` 侧只核了 `CardTypeOptions.cs` 与 `EntityScript__get_isTrapCard.c`。

---

# 第五批 · cardType 六条里归我的 4 条（2026-10-27）

> 协调台把我第四批普查的 6 条拆了：#2（`WhenEvent`）与 #4（`EffectResolver`）归 `E6ENG`，⛔ 本批未碰。
> 本批做 **#1 / #3 / #5 / #6**（对应普查 §3·1 / §3·3 / §3·5 / §3·6）。**typecheck 0/0**（最终一次；中途有 3 条红**全在 `EffectResolver.cs` 的 `NoteTrapDrawnOnce`**，是 `E6ENG` 正在写的半成品，等它落地后复跑即 0/0 —— 按铁律 13·3「错误全在别人文件里 = 不是你的问题」处理，未去动别人的文件）。

## ① #1 `CampaignScriptRunner.CardTypeOf` —— 桩接线了（值对了，分支仍待接）

`CardPresentation/Battle/CampaignScriptRunner.cs`（+14 行）：

```csharp
public int CardTypeOf(CardDef card)
{
    if (card == null) { LogWarning("`CardTypeOf(null)` ⇒ 传 0（没有卡可判）"); return 0; }
    if (card.CardType >= 0) return card.CardType;          // ← 真的接上了（原版 RawCardScript.cardType @+0x94）
    LogWarning($"…cardType **判不了**（{card.Id}：卡表 < v34 没有 cardType 列，或这张卡不在真值快照里）⇒ 传 0（⛔ 不是「它真的是 Minion」）");
    return 0;
}
```
- **改前**：无条件 `LogWarning("没有接线") + return 0` —— 每调一次喊一次、值恒 0。
- **改后**：真值（`CardDef.CardType`，卡表 v34）优先；**只有 `-1`（未知）才出声并退回 0**（= 改前行为）。
- ⚠️ **`ShowRadioMessage` 的第 4 参仍然「只进日志、没有分支」** —— 我按协调台口径**只标了待接线注释、没顺手接分支**：
  原版 `VoiceLinesController.ShowRadioMessage` 按 `CardTypeOptions` 分档（`CanUseVOs` 一族），
  接线时「值 + 分支」两件要一起做，且判据要从 `ShowRadioMessage` 的反编译体取，⛔ 别按 `CanUseVOs` 猜。

## ② #3 `AiTargets.CardTypeOf` —— 改成「真值优先」，兜底才出声

`RuleEngine/Data/AiTargets.cs`（+20/−3）：取值次序改成 **① `card.CardType`（v34 卡表）→ ② `ai_scoring.json` 真值 → ③ `Type` 字符串兜底**。
- 改前只有 ②③，且 ③ 的 `else → 20` 会把将来的 `Whispers(40)` 判成 Tactic（今天 0 张，但那是**猜**）。
- ③ 现在**出声一次**（静态闩 `_cardTypeFallbackWarned`，同一次装载只喊一次），并在注释里写明
  「池内 `type ↔ cardType` 是双射 ⇒ 兜底不改任何现有行为」。
- ⚠️ 这条兜底只在「卡表 < v34 **且** ai_scoring 里没这张卡」时才会走到（现读：ai_scoring 有 1,351 行 = 真值全表）⇒ 今天**一次都不会响**。

## ③ #5 `DeckRuntime` 的两格计数 —— **确认进不去，只补注释（零行数变化）**

先判「`defence`/`enchantment` 会不会进 `CardIds`」—— **不会**，三条独立证据：
1. **结构**：`PlayerDeck` = `{ Name, WarlordId, **DefensiveId**, CardIds }`（`RuleEngine/Core/DeckRules.cs`）—— 防御卡走**独立字段**；
2. **校验**：`DeckRules.cs:299` `if (c.Type == "hero" || c.Type == "defence") return false;`、`:493` 同一句（防御卡一旦出现在 `cards` 里就报错）；
3. **数据 + 既有记录**：`defence` 39 张全 `inventoryOptions = 5 (CantAddToDeck)`、`enchantment` 1 张 `= 10 (NotInInventory)`；
   且 `Shell/DeckInfoPopup.cs:1885` 早就写着「防御卡不在 `CardIds` 里 ⇒ 恒 1 张」。

⇒ 按协调台口径 **只补注释、⛔ 没加死格子**。做法 = **就地把那两行行尾注释写长**（`git diff --numstat` = **2/2**，行数零变化 ⇒ `账 卡组编辑.tsv row 234` 引的 `DeckRuntime.cs:2464` **没漂**）：
```csharp
if (d.Type == "unit") troops++;          // …⚠️ 两格就够，⛔ 别补 defence/enchantment 格子 —— 真值里那两种 type 也是 cardType 20，
                                         //   但它们结构上进不了 CardIds（DefensiveId 独立字段 / DeckRules:299 / inventoryOptions=10）⇒ 补格子 = 死分支
else if (d.Type == "tactic") stratagems++;   // …为什么不算 defence/enchantment 见上一行
```

## ④ #6 生成器文案（**只改注释/日志，⛔ 没动筛选逻辑**）

`工具/gen_cards_engine.py`：
1. `:3954` 那句「`cardType 20` **不是附魔专用型** —— 全表 680 张都是 20」→ 改写成
   「**`cardType 20` 是 `Tactic`（`Minion 0 / Hero 10 / Tactic 20 / Whispers 40`），⛔ 不是「附魔专用型」**
   —— 真值全表 **680** 张、**池内 486 张**（= `tactic` 446 + `defence` 39 + `enchantment` 1；2026-10-27 现读）
   ⇒ 判附魔模板永远是 **`cardType == 20` ∧ 名字含 `enchant`** 两条合取，单用 cardType 会多筛 486 张」。
2. 统计段 ② 的头注释：把「`cardType 20` 模板」改成「**`Tactic(20)`（= 模板那一档）**」，并写明
   「20 是 Tactic 不是附魔型；真正的判据是两条合取」＋现读数字（120 = 20 的 118 + 0 的 2）。
3. 入池闸（`:4166`）上方加了 4 行注释：「入池闸 = 两条合取；**单用 cardType 会多筛 486 张 ⇒ 名字那一条不能去掉**；
   **本批只改注释、没动判定**（已实测 0 漏 / 0 误收）」。
4. 读数行改成：`其中 Tactic(20) **118** 张（= 附魔模板那一档） · 非 20 的兄弟卡 **2** 张（Wurrboy(0) · Sergeant Gadriel(0)）`。

**验证**：`gen_cards_engine.py --check` ⇒ **一致 ✅**（只动了注释/日志 ⇒ 产物**一字节未变** ⇒ 池 md5 仍是 **`c2ee753d96ad11ea548843c861fd8890`**）。

## ⑤ 本批改动清单（4 个文件，全部纯 LF/CRLF 保持原样、无裸 CR）

| 文件 | 行数 | 内容 |
|---|---|---|
| `CardPresentation/Battle/CampaignScriptRunner.cs` | +14 / −2（**纯 LF**） | `CardTypeOf` 接线；`ShowRadioMessage` 加「分支待接线」注释（⛔ 没改行为） |
| `RuleEngine/Data/AiTargets.cs` | +20 / −3（**纯 LF**） | `CardTypeOf` 三级次序 + 兜底出声一次（`_cardTypeFallbackWarned`） |
| `CardPresentation/Deck/DeckRuntime.cs` | **2 / 2**（**CRLF 保持**，行数零变化） | 两行行尾注释写长（为什么两格就够 / ⛔ 别补死格子） |
| `工具/gen_cards_engine.py` | 文案（**CRLF 保持**） | §④ 四处文案；判定逻辑一字未动 ⇒ `--check` 一致 ✅ |

## ⑥ 验证读数
- `TMPDIR=/tmp/wf_E7ENG bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（中途那 3 条红全在 `EffectResolver.cs` 的 `NoteTrapDrawnOnce` = `E6ENG` 在写，落地后复跑即 0/0）。
- `python -I -X utf8 d:/4/Unity/工具/gen_cards_engine.py --check` ⇒ **一致 ✅**；池 md5 = **`c2ee753d96ad11ea548843c861fd8890`**（未变）。
- `账` 漂移面：`CampaignScriptRunner.cs` / `AiTargets.cs` **0 条账引用**；`DeckRuntime.cs` 的引用全部在别处、两条改动行**零行数变化** ⇒ **本批 0 条新漂移**。
