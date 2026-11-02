# B117_F13 · 三处 trait 读点（`B117-44` · `B117-45` · `B117-46`）

> 执行写手：`B117_F13`（2026-11-02）· 上一手 = `B117_F8`（`报告/B117_F8_oathdouble与剩余trait.md`）
> 一句话：**修掉会让下一轮 Unity 批跑直接变红的那条断言夹具（`B117-44`）**，把 `EffectResolverPartial_B88E2.cs`
> 剩下的两处 `Has(...)` 按原版判据落到 trait 读口（`B117-45`），并**只报告不改**地查清 `DoReloadDuty` 的判据（`B117-46`）。

---

## 一、`B117-44`：`B97_ER2_Assert.cs` 的誓约重放断言（**不改就红**）

### 病灶

`CardPresentation/Editor/B97_ER2_Assert.cs` 的 `B97ER2OathReplaysCountsOnlyOwnSide()` 给夹具卡设的是
**`CardDef.OathDouble` 这个正文 bool**，而 `RuleCore.OathExtraReplays` 自 `B117_F1` 起读的是
**`cardTraits`**（`HasOathDouble` = `u.HasCurrentTrait("oathdouble")`，`RuleCore.cs:18400-18404`）
⇒ 实得 **0**、期望 **1** ⇒ `RuleEngineTest.Run` 必红。

### 改了什么（零行为面，只有夹具）

| 行 | 原 | 现 |
|---|---|---|
| `:160` / `:161` / `:165` | `a.OathDouble = true;` / `e.OathDouble = true;` / `b.OathDouble = true;` | `a.AttachCardTraits(new[] { "oathDouble" });` / `e.…` / `b.…` |
| `:141-149` 注释块 | 「⚠️ 如实标着的残余：我们读 `Card.OathDouble`（卡面 trait）…那一半在 `RuleCore.cs`」 | **就地订正（铁律 5）**：那半句**已过期** —— `B117_F1`/`F8` 起本算子与 `IsThereDoubleOathEffect.c:33` **同口径**（`HasCurrentTrait("oathdouble")`）；夹具因此必须挂 `cardTraits`，设 `CardDef.OathDouble` 已不再能让本段变绿 |

- 为什么挂得上：`CardDef.AttachCardTraits` 走 `TraitKeyOf` → `KeywordTable.Normalize`，
  `"oathDouble"` 在 `Prefixes` 里有自己的条目（`B117_F8` · `B117-26` 补的三条，排在 `"oath"` 之前）⇒ 落成 `"oathdouble"`，与 `HasOathDouble` 的读口逐字对上。
- **不需要**把它们挪到 `Place()` 之前：`UnitState.HasCurrentTrait` 对卡面那批是**现读** `Card.HasDefaultTrait`（`UnitState.cs:4387`），不是构造时快照。

### 灭自证（还在，而且写进了断言文案）

- 把这两句 `AttachCardTraits` 拿掉 ⇒ trait 没挂上 ⇒ `OathExtraReplays` 实得 **0** ⇒ 首条断言（期望 1）红。
  这条已写进 `:164` 的文案尾（「把这两句 `AttachCardTraits` 拿掉（trait 没挂上）⇒ 实得 0、本行红」）。
- 原有两条仍然有效：改成「数双方」⇒ 实得 2 红；改成恒 0 ⇒ 首条红。

---

## 二、`B117-45`：`EffectResolverPartial_B88E2.cs` 两处 trait 读点

### ②-1 `:205`（`CanPlayWithOathNowInPlay` 的 jam 闸）→ `HasCurrentTrait`

- **判据**：`decomp_full/CardScript__CanPlayWithOathNowInPlay.c:22` = `EntityScript__HasCurrentTrait(param_1, 0x82 /*jam = 130*/)`
  —— 读的是**当前 trait 表 `+0x128`**，不是 `keywords` 单列。
- **改法**（同一物理行，`Has` → `HasCurrentTrait`）：
  ```csharp
  bool jammed = (thisCard != null && thisCard.HasCurrentTrait(KeywordTable.Jam))
             || (thisCard == null && card.HasDefaultTrait(KeywordTable.Jam, out _));
  ```
  `thisCard == null` 那半支**保持** `card.HasDefaultTrait`（`CardDef` 没有「当前表」，与 `B117_F8` 在
  `NeedsToChooseFromPool` 的处理同形）。

### ②-2 `:992`（`RunPlayChosenCardAbility` 的誓约闸）→ `HasDefaultTrait`（**不是** `HasCurrentTrait`）

- **判据**：`decomp_full/AbilityLogic__PlayAbility.c:2025` = `RawCardScript__HasDefaultTrait(*plVar2, 0x4fb /*oath*/)`
  —— 读的是**卡面默认 trait**（`defaultTraits(+0x230)`），**不是**当前 trait 表。
- ⚠️ **与简报口径的差异（如实记）**：简报写「`:992` 改成 `HasCurrentTrait(...)`」。
  回源后发现**两条都不成立**：
  ① 原版这一句不是 `HasCurrentTrait` 而是 `HasDefaultTrait`（上面那行 `.c` 逐字）；
  ② `cc` 的类型是 **`CardDef`**（`BattleContext.CardChosenInSelection`，`BattleContext.cs:2472`），
     全仓 **`CardDef` 上没有 `HasCurrentTrait`**（那是 `UnitState` 的口）—— 而且「选中的那张」**可能根本不在场**，
     「当前 trait」对它没有意义。
  ⇒ 按铁律「判据与施工单冲突时以 `.c` 为准、宁可拒做」，落成 **`cc.HasDefaultTrait(KeywordTable.Oath, out _)`**。
  行为面今天**零变化**（见 §二·3）。
- 同物理行注释也一起就地订正：写明「⛔ 别改成 `HasCurrentTrait`」，免得下一手又按错口径改回去。

### 注释订正（铁律 5）

| 位置 | 原文（已过期） | 现文 |
|---|---|---|
| `:1000` 行尾 | 「⚠️ 残余同 `EffectResolver.ResolveOathAbility`：**算子读卡面 trait，原版读 `HasCurrentTrait`** —— 那一半在 `RuleCore.cs`」 | ✅ 就地订正：该残余**已消除** —— `B117_F1`/`F8` 起 `RuleCore.OathExtraReplays` 就是 `HasCurrentTrait("oathdouble")`（`IsThereDoubleOathEffect.c:33` 同口径） |
| `:204` 行尾 | （无） | 追加 `B117-24 #22` 的判据与「别退回 `Has`」 |

⚠️ **简报点名的 `:969`**：现读那一行是「`:2043-2057` … `while (IsThereDoubleOathEffect(...) > i)` ⇒ 总次数 = 1 + 同方场上带 `oathDouble(1277)` 的牌数」，
**内容与 `.c` 一致、没有过期残余**（那个「算子读卡面 trait」的旧句在 `:1000`，不在 `:969`）。
⇒ 只订正了 `:1000`；`:969` **未动**（无错可改）。

### ②-3 会不会误伤那 28 张 `oath N`（如实核过）

`oathDouble` 与 `oath` 现在各归各名（`B117-26`），本批**没有触碰**归一化表，只换了读口。逐条核：

- `:205` 的 jam 闸：池内**带 `jam` 的卡 = 0 张**（`cards_engine.json` 现读）⇒ 任何读口都等价。
- `:992` 的 oath 闸：`cc` 是「选中的那张」——池内 `oath` 在 `keywords` 与 `cardTraits` **两列表现在完全一致**
  （现扫：`oath` 两表都有 **28 张** / 只在 `keywords` **0** / 只在 `cardTraits` **0**）⇒ `Has` 与 `HasDefaultTrait` 对全池**逐张同判**。
  另：`chosenAbility 360` 这一支在原版里本来就**进不去**（上游 plan 一格归 F1，见该方法 doc 的「📊 可达性」）。
- 同一扫法对 `duty` 也做了（23 张，两表一致）、`jam`（0 张）—— 供 `B117-46` 参考。

---

## 三、`B117-46`：`DoReloadDuty` 的 `Has(Duty)`（**只报告、未改一行**）

### 3·1 原版链（逐段现读）

| 环节 | 文件:行 | 内容 |
|---|---|---|
| 目标列表从哪来 | `decomp_full/AbilityLogic__PlayAbility.c:1934`（`case 0x1db`）+ `:1938/:1943` | 遍历能力自己的**目标表**（`param_1 + 0x78`，`:263` 处赋值 = `GetTargets` 的产物），**逐个**调下一跳 |
| 入队 | `AbilityLogic__PlayAbility.c:1949-1952` → `BattleManager__AddResetDuty.c:34` / `:98` | 组一条 `BattleAction`（类型 **`0x57`**）入队；**全程没有任何 trait 判断** |
| 结算 | `BattleManager__ResolveResetDuty.c:5-9` | `if (param_2 != 0) { *(undefined1 *)(param_2 + 0x4c) = 0; UpdateFigures; FinishResolvingAction(0x57); }` —— 对传进来的那一张**无条件**清 `+0x4c`（= `usedActiveAbility`），**也不看 trait** |

### 3·2 「谁选的目标」——池内三张卡的数据面

`chosenAbility == 475`（`0x1db`）的卡现读 **3 张**，其 reload 那一行 `targetCriteria`（`_tmp_view/allcards_export/cards_raw.json` 的 `abilityLogic`）：

| 卡 | 卡面 | `targetsAffected` | `friendlyUnits` | `includeHero` | `traitsFilter` | `keywordFilter` |
|---|---|---|---|---|---|---|
| `AM47 Press the Attack` | Reload the Duty abilities of **all your units** | 10 | 1 | 1 | **0** | **0** |
| Cadian Honour | …reload **their** Duty abilities | 10 | 1 | 0 | **0** | **0** |
| Lead by Example | …reload **its** Duty ability | 30 | 0 | 0 | **0** | **0** |

### 3·3 结论（照简报要求二选一）

**⇒ 「原版没有这条形状」**：
- 原版 `ResolveResetDuty` / `AddResetDuty` **都不按 trait 过滤**（上面 `.c` 两段逐句可核）；
- 目标的「带不带 duty」**根本不由 trait 闸决定**，而由 `AbilityLogic.targetCriteria` 决定，而池内三张卡的
  `traitsFilter = 0` / `keywordFilter = 0`（**没有按 duty 筛**；`AM47` 甚至是「己方全部单位 **含督军**」）。
- 所以 `EffectResolver.cs:15501/15507` 的 `u.Has(KeywordTable.Duty)` /
  「扫全部带 duty 的单位」是**我们自己挑的收窄**，原版**没有**委托体与之对应。

### 3·4 建议（**本批不动，留调度台裁定**）

两条路，都要在账里写清：

1. **照原版改成「按目标表清」**（推荐 · 复刻口径）：`all` 档 = 己方场上**全部单位（含督军）**逐张清
   `ActiveAbilityUsed`；单目标档 = 前一句那个目标。净行为对**有 duty 的单位**完全不变（`+0x4c` 本来就是假的再写假）；
   唯一可观测差别 = 今天那句「没有可供装填的单位 ⇒ 这条没生效 / 计入 `unresolved`」**要删掉**（原版没有这个失败档）。
2. **保留现状**：那就必须按铁律 18 在代码里打 `🔶 自己设计（非原版）` 标并写清「原版按 `targetCriteria`、`traitsFilter=0`」。

⚠️ 顺带记一条同族事实（**不是本批的活**）：`+0x4c` 是通用 `usedActiveAbility`，
「职责一局一次」那道闸在 `BattleManager__CanUseActiveAbility.c:92-97`（硬编码 trait `duty(1220)` ∧ `+0x4c`）——
`DutyUsed` 是我们自造的重复位（`B99_C1` 已记），本次未动。

---

## 四、静态核 `OathDouble`：还有没有别处拿它当判据

`grep -rn "OathDouble" --include=*.cs Unity/` 全量过了一遍，分三类：

| 类 | 落点 | 是不是「拿 bool 当判据」 |
|---|---|---|
| **`CardDef.OathDouble` 本体** | `Core/CardDef.cs:4339`（doc）· `:4341`（字段）· `:4401`（`case "誓约加倍": OathDouble = true;`） | ❌ 不是。doc 已写死「⛔ 别拿它当判据」；保留是因为**数据/展示**还认它 |
| **`EffectsToShow.OathDouble`** | `Core/EffectsToShow.cs:63`（`const int OathDouble = 1277`）· `:86` · `:550`；`CardPresentation/Editor/B97W16_Assert.cs:295` | ❌ 不是。这是**另一个符号**（int trait id 1277），与 `CardDef` 的 bool 无关 |
| **断言/注释** | `Editor/RuleEngineTest.cs:17959-17984`（**文本解析**那条路的回归：`Friendly [Oath] abilities apply an additional time` → `OathDouble == true`）· `Editor/RuleEngineTest_B116W19.cs:492-494`（前提断言 `!cassius.OathDouble`，正是为了证明旧判据恒假）· `Core/RuleCore.cs:11848-11849` / `:11857`（doc + `HasOathDouble`）· `Core/EffectResolver.cs:9392`（doc） | ❌ 不是「行为判据」：前者验的是**解析器**，后者验的是**旧判据为假** |
| 备份文件 | `Unity/_tmp_view/_a2_RuleCore_before_1028.cs:11650` | — 历史快照，不参与编译 |

**结论：`CardDef.OathDouble` 已经没有任何行为判据在用它**。唯一「还在读」的是数据层的文本解析测试，那是它的正当职责。

---

## 五、断言（接进**已接线**的 `RuleEngineTest_B116W19.cs` 的 `B117F1TraitConsumers()`，新开 ⑥ 段）

零新增接线（`CheckB116W19Jam()` 早在 `RuleEngineTest.cs` 的 `Run()` 里）。全部「先 `Check(前提)` 再 `Check(结论)`」，
前提不成立时显式红（`CheckTrue(false, "…")` 那种静默跳过一个都没有）。

| # | 断的是什么 | 🧨 灭自证（改哪儿会红） |
|---|---|---|
| ⑥-a | `CanPlayWithOathNowInPlay` 的 jam 闸：jam **只在 `cardTraits`** ⇒ 返回 **false**（该夹具的回合/oath/法力三道闸都为真） | 改回 `thisCard.Has(...)` ⇒ 返回 true ⇒ 红 |
| ⑥-b | 反证：同形但不带 jam ⇒ 返回 **true** | 把这个口写成恒 false ⇒ 红（证明 ⑥-a 不是「恒 false」） |
| ⑥-c | `RunPlayChosenCardAbility`：选中的那张 `oath` **只在 `cardTraits`** ⇒ 走誓约支、按 actingCard 的 `oath 1` **扣 1 点** | 改回 `cc.Has(...)` ⇒ 不扣钱 ⇒ 红 |
| ⑥-d | 反证：同一张卡的 `oath` **只在 `keywords`** ⇒ **不扣钱** | 改回 `cc.Has(...)` ⇒ 这里会扣 1 点 ⇒ 红 |

- ⑥-c/⑥-d 的读数口 = `ctx.Players[0].Energy`（`PayCardPlayedCostOath` 付的是 **actingCard 自己的 `oath` 值**，`.c:11`），
  外加一条「跑完 `CardChosenInSelection` 被清空」的前提断言（证明真的进了这一支，原版 `PlayAbility.c:2060-2068`）。
- 夹具：`W19TraitsCard(...)`（既有，只有 `cardTraits` 列）＋ `Unit(..., "Oath 1")`（既有，`keywords` 列）。

---

## 六、闸读数（本批实测）

| 闸 | 读数 | 判 |
|---|---|---|
| `bash 工具/typecheck.sh` | 运行时 **0** / 编辑器 **0** | ✅ |
| `check_ledger_points.py`（全库） | `战斗逻辑驱动` 漂移 2 / 失败 0 · `卡组编辑` 漂移 19 / 失败 2（后者的文件是 `DeckRules.cs` / `GameplayVariables.cs`，**此刻有别的写手在改**） | ✅ **本批三个文件 0 漂移 0 失败**（全表输出里 `EffectResolverPartial_B88E2` / `EffectResolver.cs` / `RuleEngineTest_B116W19` / `B97_ER2` **一条都没出现**） |
| `bash 工具/ruleprobe.sh check` | **rc=0** · 输出指纹 **`ebdac779…` 不变** · 全池 1121 张 · 解析差异 0 行 · 哨兵失败 0 | ✅ |
| `bash 工具/ruleprobe.sh play check` | **rc=1** · 169 张卡变 · 指纹 `d6c68354…` vs 基线 `a6ab8bcb…` | ⚠️ **不是本批造成的**（见下） |

**`play check` 那 169 张的成因（逐条核过）**：每一条的差异形状**只有一种** —— 督军血 `28 → 38` / `30 → 40`（一律 **+10**），
来源 = 另一写手的 `GameplayVariables.ClassicWarlordLifeChange`（与 `B117_F8` 当天报的是**同一个数、同一种形状、同样 169 张**）。
本批的两处改动对该池**结构上零影响**（`jam` = 0 张；`oath` 在 `keywords`/`cardTraits` 两表 28 张完全一致）⇒ **未重钉基线**。

---

## 七、没改的 / 查不到的 / 顺手发现

1. **`B117-46` 一律没改**（`EffectResolver.cs:15501/15507`）—— 判据结论与建议见 §三。
2. **`:969` 的注释没有过期**（简报疑似与 `:1000` 串行），未动。
3. **`RulesEngineTest.cs:17951-17990` 那族文本解析断言**仍然断言 `CardDef.OathDouble` —— 那是**解析器**的回归，不是行为判据，**不需要改**（也解释了为什么这个 bool 不能删）。
4. **`EffectResolverPartial_B88E2.cs` 的行数零变化**（205 / 992 / 1000 三处全是**同一物理行内**改写）——
   该文件被账按 `文件:行号` 引用 **23 处**（`:255` / `:785` / `:876` …），插行会让它们整体漂移（铁律 18·b）。

---

## 八、改动清单（3 个文件，全部在本批白名单内）

| 文件 | 改动 |
|---|---|
| `CardPresentation/Editor/B97_ER2_Assert.cs` | `:160/:161/:165` 三处 `OathDouble = true;` → `AttachCardTraits(new[] { "oathDouble" })` · `:141-149` 注释块就地订正（残余已消除）· 断言文案补「拿掉 `AttachCardTraits` ⇒ 红」 |
| `RuleEngine/Core/EffectResolverPartial_B88E2.cs` | `:205` `Has(Jam)` → `HasCurrentTrait(Jam)`（同行注释补判据）· `:992` `Has(Oath)` → `HasDefaultTrait(Oath)`（同行注释写清「⛔ 别改成 `HasCurrentTrait`」）· `:1000` 行尾过期残余就地订正。**净行数 0** |
| `RuleEngine/Editor/RuleEngineTest_B116W19.cs` | `B117F1TraitConsumers()` 新增 ⑥ 段 4 条断言（⑥-a/b = jam 闸，⑥-c/d = 誓约闸） |

（`RuleEngine/Core/EffectResolver.cs` 只读，**一个字节没改**。）
