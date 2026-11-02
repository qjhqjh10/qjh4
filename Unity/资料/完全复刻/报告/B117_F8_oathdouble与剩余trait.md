# B117_F8 · `B117-26`（`oathDouble` 归一化撞车根治）+ `B117-24` 剩余 11 处

> 执行写手：`B117_F8`（2026-11-02）· 上一手 = `B117_F1`（`报告/B117_F1_trait续修.md`）
> 一句话：**`KeywordTable.Prefixes` 补上 `oath` 三个变体（排在 `"oath"` 之前）**，`oathDouble` 从此有自己的归一化名；
> 撞车消失后，`B117_F1` 留下「等 `B117-26` 再动」的那 11 处 `Has` 全部按原版判据改成 `HasCurrentTrait`，
> `RuleCore.HasOathDouble` 从「`Raw` 逐字精确比」简化成一句 `HasCurrentTrait("oathdouble")`。

---

## 一、`B117-26` 根治：`Prefixes` 补 `oath` 变体（`CardDef.cs:8342`，**同物理行追加、行数零变化**）

### 改了什么

```
原 8342: new[] { "shuriken"… }, new[] { "sniper"… }, new[] { "waystone"… },
原 8343: new[] { "oath", "oath" }, …
```

在 **8342 行尾**追加三条（⛔ 不能加到 8343 之后 —— `Normalize` 是 `StartsWith` + **先到先得**，排在 `"oath"` 后面等于没写）：

```
new[] { "oathinallturns", "oathinallturns" }, new[] { "oathdouble", "oathdouble" }, new[] { "oathtripleactivation", "oathtripleactivation" },
```

对应原版 `DefinedTrait`：`oathInAllTurns = 1276`（`0x4fc`）· `oathDouble = 1277`（`0x4fd`）· `oathTripleActivation = 1278`（`0x4fe`）。

- **⛔ 不进 `Implemented`**（同 `massive` / `transient` / `firststrike` 先例）：这三个词在官方 `Card_Trait/*` 里**没有名字**（隐藏 trait），进 `Implemented` 会让 `BattleDriver` 的外观过滤多显示一枚原版没有的徽标。
- 行数**零变化**（`CardDef.cs` 现读仍 9616 行）—— 本文件有账按 `文件:行号` 引用，插行会让下游整体漂移（铁律 18·b）。

### 全池扫一遍（改前 vs 改后，逐条比 `TraitKeyOf`/`Normalize` 结果）

脚本按源码把 `Prefixes` 整表（119 条，含新三条）解析出来，对 `cards_engine.json` **1129 张**的
`cardTraits` 与 `keywords` 两列逐条做「改前 / 改后」归一化对比（`TraitKeyOf` 的 `Normalize` 失败回退也照抄）：

| 结果 | 数量 | 明细 |
|---|---|---|
| 归一化**变了**的条目 | **1** | `UM100 Chaplain Cassius` 的 `cardTraits=["oathDouble"]`：`"oath"` → **`"oathdouble"`** |
| 归一化未变 | 全部其余 | 28 张 `cardTraits=["oath 1..8"]` 仍 → `"oath"`；`keywords` 那 28 条 `Oath` / `Oath N` 仍 → `"oath"` |

交叉核对（同一件事的第二个证据面）：

- `keywords` 列全池**没有任何**以 `oathdouble` / `oathtripleactivation` / `oathinallturns` 开头的串 ⇒ **关键词层零变化**。
- `cards_engine.json` 的 `desc` / `descZh` 逐句扫：以这三个词开头的句子 **0 条** ⇒ `EffectText.IsKeywordOnly`（要求吃满整句）不会误判。
- `cards_raw.json`（1,351 张 SO 真值）的 `CardTraits`：`oath` 38 条 · **`oathDouble` 1 条（`UM100`）**，没有第三种变体。
- 拼写核对：`oathInAllTurns` / `oathDouble` / `oathTripleActivation` → 小写连写，与 `cards_raw.json` 的 `name` 逐字一致。

### 连带就地订正的注释（铁律 5）

| 位置 | 原文 | 现文 |
|---|---|---|
| `CardDef.cs:8662-8663` | 45 词「不登记」清单里含 `oathInAllTurns` / `oathDouble` / `oathTripleActivation` | 划掉这三个词，写明「**归一化撞车会污染别的词**」这条新理由 + 仍在 `Prefixes`、仍**不进** `Implemented` |
| `CardDef.cs:4339`（`OathDouble` 字段 doc，保持两行） | 「次数 = 同方场上带本标记的牌数，见 `OathExtraReplays`」 | 「`OathExtraReplays` **不再读本字段**，改走 `HasOathDouble`；本字段保留（数据/展示还认它），⛔ 别拿它当判据」 |
| `RuleCore.cs:11848`（`OathExtraReplays` doc） | 「为什么不能直接问 `HasCurrentTrait("oathdouble")` 见类尾」 | 「撞车已根治 ⇒ `HasOathDouble` 现在就是一句 `HasCurrentTrait("oathdouble")`」 |
| `EffectResolver.cs:9392` | 「残余：算子读 `u.Card.OathDouble`…池内 `oathDouble` 只 `UM84` 一张」 | 「残余已消除…池内带 `oathDouble` 的**只有 `UM100`**（`UM84` 是 `LegacyCardIds` 的旧编号）」 |

### `HasOathDouble` 简化（`RuleCore.cs:18403`，类尾）

```csharp
static bool HasOathDouble(UnitState u)
{
    if (u == null || u.Card == null) return false;
    return u.HasCurrentTrait("oathdouble");   // 原版 `EntityScript__HasCurrentTrait(card, 0x4fd)`
}
```

- 原实现（`B117_F1`）= 遍历 `CardTraitEntry.Raw` 精确比 + 借 `HasCurrentTrait("oath")` 拿残骸/已摘除两道闸。
  **撞车消失后借来的那道闸会反向漏判**（`oath N` 卡也带 `oath`，而 `UM100` 不带）⇒ 必须换。
- 新实现顺带覆盖了原来判不到的**运行时 `addTrait` 授予**（那条走 `_keywords`，授予名同样过 `Normalize`）。
- ⚠️ 本段位于**类尾**（最大 A 档落点 `:18247` 之后），所以本文件净 **−7 行**也不产生任何账漂移（已用闸复核）。

---

## 二、`B117-24` 剩余 11 处：逐处判定 + 落地

口径照 `报告/B117_F1_trait续修.md` §四：原版 `MeetsTraitCriteria` / `HasCurrentTrait` 读的都是**当前 trait 表 `+0x128`**
（`CardScript__SetBaseCardTraits.c:37-42` 把 `cardTraits(+0x230)` 整表灌进去）⇒ **trait 语义**，⛔ 不是 `keywords` 单列。

| # | 落点（现读行号） | 原版判据（`d:/2/tools/decomp_full/`） | 改动 |
|---|---|---|---|
| 15 | `RuleCore.cs:3657` `RecomputeActAndAttack` | `CardScript__ActivateMinion.c:43-69`：`:43` `HasCurrentTrait(0x4f1 ferocity)` / `:45-47` `HasCurrentTrait(0x4fb oath)` | 两个 → `HasCurrentTrait`；**同一行尾**的 `Has(Stun)` 也一并改（`:55` 是 `HasCurrentTrait(100)`） |
| 16 | `RuleCore.cs:3718` / `:3721` `ActivateTraitsOnSummonOrEnchantment` | `CardScript__ActivateTraitsOnSummonOrEnchantment.c:44-49` 枚举的正是 **`+0x128` 当前 trait 表**，`:76-99` 按 `iVar1`（trait id）分支（`:86` 是 `0x4f1` / `0x4fb`） | `fast`/`flank` 与 `ferocity`/`oath` 四个 → `HasCurrentTrait` |
| 17 | `RuleCore.cs:5759` `IsValidTarget` 的召唤病那支 | `BattleManager__IsValidAttackTarget.c:201`：`!HasCurrentTrait(attacker, 0x28 fast)` | `!Has("fast")` → `!HasCurrentTrait("fast")` |
| 18 | `RuleCore.cs:6028` `RecomputeAttackTypeAfterActiveAbility` | `CardScript__UsedActiveAbility.c`：`HasCurrentTrait(0x4c4 duty)` / `(0x4f1 ferocity)` / `(0x4fb oath)` | 三个 → `HasCurrentTrait` |
| 18 | `RuleCore.cs:6126` `ForceAttackTypeOnDeploy` | `CardScript__ActivateMinion.c:43-47,70-72`（同上 ferocity/oath） | 两个 → `HasCurrentTrait` |
| 19 | `RuleCore.cs:2862` 回合末「带 `oath` 重挑攻击型」 | `CardScript__OnTurnEnd.c:214-218`：`HasCurrentTrait(param_1, 0x4fb)` | → `HasCurrentTrait` |
| 20 | `RuleCore.cs:17129` `PayCardPlayedCostOath` | `BattleManager__PayCardPlayedCostOath.c:10`：`EntityScript__HasCurrentTrait(param_2, 0x4fb)` | 这一处是 **`CardDef`**（手牌，没有 `UnitState`）⇒ 照 `BattleContext.cs:1546` / `RuleCore.cs:3986` 的既有形状并集：`Has(oath) \|\| HasTrait(oath)`。**原来注释写 `HasCurrentTrait`、代码写 `Has` 的自相矛盾就此消解** |
| 21 | `EffectResolver.cs:11867` `AddEffectCasterNotJammed` | `CardScript__AddEffect.c:86-89`：`EntityScript__HasCurrentTrait(caster, 0x82 jam)` | → `HasCurrentTrait` |
| 21 | `EffectResolver.cs:17949` `NeedsToChooseFromPool` 的 jam 闸 | `CardScript__NeedsToChooseFromPool.c:133`：`HasCurrentTrait(param_1, 0x82)` | 实例那半支 → `HasCurrentTrait`（`thisCard == null` 时仍退回 `card.HasDefaultTrait`，行为不变） |
| 21 | `EffectResolver.cs:18595` `TriggerAbilitiesPassGates` ① | `RawCardScript__TriggerAbilities.c:43`：`HasCurrentTrait(param_4, 0x82)` | → `HasCurrentTrait` |

**顺带同族一格（不在 11 条清单里，但是同一个前提）**

| 落点 | 判据 | 改动 |
|---|---|---|
| `RuleCore.cs:3554` `HasDeployExemption` 的 `oath` 半格 | `CardScript__ActivateTraitsOnSummonOrEnchantment.c:34-99`（四个词全 `HasCurrentTrait` 语义） | `B117_F1` 当时因撞车**故意保留 `Has`** 并在行尾写明理由；前提被 `B117-26` 消掉 ⇒ 现改成 `HasCurrentTrait(Oath)`，行尾注释同步换掉 |

### 今天有没有可观测变化（如实记）

**引擎侧：零。** 逐条查过：全池 28 张 `oath N` 卡的 `keywords` 里都另有 `Oath`（`Has` 本来就为真），
而 `UM100` 的 `keywords` 只有 `Talent: Catechism of Death`（`Has` 本来就为假、改后 `HasCurrentTrait` 也为假）；
`jam` / `fast` / `flank` / `duty` 的 cardTraits-only 卡池内各 0 张。
⇒ 这一批是**口径对齐**（把「原版读 trait 表」落实到代码形状），不是行为改动。
**唯一真实变化 = `UM100` 的 trait 身份**（`oath` → `oathdouble`），这正是 `B117-26` 要的。

**表现层一处连带**（如实记，且方向与原版一致）：`VfxMap.PreloadTraitEffectsOfCard` 按 `CardTraitEntry.Name` 预热，
`UM100` 现在查 `trait_effect_bindings.json` 的 `oathdouble` 行 —— 该表**只有 `oath`、没有 `oathDouble`**（现读 0 命中）
⇒ `UM100` 不再预热 `oath` 那族 trait 特效件。原版对 `UM100` 传的也是 `oathDouble`（同表同缺）⇒ **与原版同形**；
且预热只是 warm-up，不影响播放。

---

## 三、断言（接进**已接线**的 `RuleEngineTest_B116W19.cs` 的 `B117F1TraitConsumers()`，新开 ⑤ 段）

零新增接线（`CheckB116W19Jam()` 已在 `RuleEngineTest.cs` 的 `Run()` 里）。全部「先 `Check(前提)` 再 `Check(结论)`」，
前提不成立时**显式红**（`CheckTrue(false, "…无法判定…")`），⛔ 没有 `if (前提) Check(结论)` 那种静默跳过。

| # | 断的是什么 | 🧨 灭自证（改哪儿会红） |
|---|---|---|
| ⑤-a | `Normalize("oathDouble") == "oathdouble"` | 把那条从 `Prefixes` 删掉 / 挪到 `"oath"` 之后 ⇒ 红 |
| ⑤-b | 反证：`Normalize("oath 2") == "oath"`（老词不许改坏） | 写成 `"oath 2"` 也变别的名 ⇒ 红 |
| ⑤-c | `UM100`：`HasTrait("oathdouble")` 真 / `HasTrait("oath")` **假** | 前缀表回退 ⇒ 两者同时为真 ⇒ 红 |
| ⑤-d | 反证：`UM101`（`oath 2`）`HasTrait("oath")` 真 / `HasTrait("oathdouble")` **假** | 反向污染 ⇒ 红 |
| ⑤-e | 场上单位：`HasCurrentTrait("oathdouble")` 真 / `HasCurrentTrait("oath")` 假 | 同上 |
| ⑤-f | 反证：`UM101` 反向（`oath` 真 / `oathdouble` 假） | 同上 |
| ⑤-g | 消费端：`HasDeployExemption(UM101)` 真、`HasDeployExemption(UM100)` **假** | 前缀表回退 ⇒ `UM100` 也变有豁免 ⇒ 红（本批 `:3554` 那条的直接消费者） |
| ⑤-h | `HasOathDouble` 简化后：同侧摆 `UM100`+`UM101` ⇒ `OathExtraReplays == 1` | `UM101` 被算进来 ⇒ 2 ⇒ 红 |

⚠️ 断言**只写不跑**（简报要求），真跑由调度台用 `RuleEngineTest.Run` 统一做。
原有的 ③-a / ③-d 两条**断言文案**已按新事实就地订正（它们原来的理由写着「`Prefixes` 没有 `oathdouble`」）。

---

## 四、闸读数（本批实测）

| 闸 | 读数 | 判 |
|---|---|---|
| `bash 工具/typecheck.sh` | 运行时 **0** / 编辑器 **0** | ✅ |
| `bash 工具/ruleprobe.sh check` | 输出指纹 **`ebdac779…` 不变** · 全池 1121 张 · 解析差异 **0 行** · 哨兵失败 0 | ✅ 解析层零回归（**未重钉**） |
| `check_ledger_points.py`（全库） | A 5831 · 通过 5801 · 漂移 **28** · 失败 **2** | ⚠️ 见下 |
| `check_ledger_points.py`（只筛本批 4 个文件） | **0 漂移 0 失败**（全部 `OK`） | ✅ |

**那 28 漂移 + 2 失败全部落在 `RuleEngine/Core/DeckRules.cs` 与 `RuleEngine/Core/GameplayVariables.cs`** ——
这两个文件此刻**有别的代理在改**（`git status` 现读均 `M`，`GameplayVariables.cs` 对 HEAD 是 `36+/14-`），
与我的改动无关（判据 = CLAUDE.md 12 节那条：错误全集中在不是自己负责的文件上）。

**`ruleprobe.sh play check` = rc 1，⚠️ 不是本批造成的**：169 张卡的差异**逐条只有一种形状** —— 督军血 `28 → 38`（`30 → 40` 等，一律 **+10**），
来源 = 另一代理 `B117_F6` 的 `GameplayVariables.ClassicWarlordLifeChange` **`0 → +10`**（`git diff` 现读确认）；
oath / `oathdouble` 相关差异 **0 条**（全文 grep `oath` = 0 命中）。**⛔ 按简报未重钉基线。**

---

## 五、没改的 / 查不到的 / 顺手发现

1. **`EffectResolverPartial_B88E2.cs:205`（`Has(Jam)`）/ `:992`（`Has(Oath)`）** —— `B117-24` #22，**不在本批白名单**，
   判据同 #21（`ReactToUnitJammed` / 誓约那头都读 `HasCurrentTrait`）⇒ 建议下一批照本批形状改。
   同文件 `:969` / `:1000` 两处注释还写着「算子读卡面 trait」的旧残余（已过期）⇒ 一并订正。
2. **`EffectResolver.cs:15501/15507` `DoReloadDuty` 的 `Has(KeywordTable.Duty)`** —— 顺手发现。
   ⚠️ **判据未定，只报告不改**：`BattleManager__ResolveResetDuty.c` / `AddResetDuty.c` **都不按 trait 过滤**
   （前者对传入的那一张牌直接复位 `+0x4c`），我们的「扫全部带 duty 的单位」这一形状**找不到对应委托体**
   ⇒ 要么是另一条链路，要么是我们自设计。建议下一批派只读代理查 `reloadduty` 这个 op 的判据再动。
3. **`ActivateTraitsOnSummonOrEnchantment` 原版还有一支 `0xe6`（dropPod）**（`.c:70-78`：`if (iVar1 == 0xe6) cVar7 = param_2;`
   走 else 分支写全局那两格）。我们这一支**没实现**。⚠️ 池内带 `dropPod` 的卡 **现读 0 张** ⇒ 休眠；
   按铁律 11 仍要补，建议下一批做（本批只改 trait 读法，不扩行为面）。
4. **`CardPresentation/Editor/B97_ER2_Assert.cs` 会变红（不是本批造成，但会被 `RuleEngineTest.Run` 跑到）**：
   `B97ER2OathReplaysCountsOnlyOwnSide()`（`:148-176`）给夹具卡设的是 **`CardDef.OathDouble` 这个 bool**
   （`a.OathDouble = true;`），而 `B117_F1` 起 `OathExtraReplays` 读的是 **`cardTraits`** ⇒ 实得 0、期望 1 ⇒ 红。
   **该文件不在本批白名单**，给出可粘贴的补丁（照本批 ⑤ 段的形状）：
   ```csharp
   // 原：a.OathDouble = true;  e.OathDouble = true;  b.OathDouble = true;
   // 改为（`Unit(...)` 不带 cardTraits ⇒ 必须显式挂）：
   a.AttachCardTraits(new[] { "oathDouble" });
   e.AttachCardTraits(new[] { "oathDouble" });   // 对面那张：⛔ 不该被数进来
   b.AttachCardTraits(new[] { "oathDouble" });
   ```
   同一段 `:142-145` 的注释（「我们读 `Card.OathDouble`（卡面 trait）」）也要一起订正。
5. **`CardPresentation/Core/VfxMap.cs` 的预热差**见 §二末（已核实与原版同形，不需要改）。

---

## 六、改动清单（4 个文件，全部在本批白名单内）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/CardDef.cs` | `:8342` 追加三条 `Prefixes`（零行数变化）· `:8662-8663` 45 词清单就地订正 · `:4339` `OathDouble` doc 就地订正（保持两行） |
| `RuleEngine/Core/RuleCore.cs` | `:3554` / `:3657-3658` / `:3718` / `:3721` / `:5759` / `:6028` / `:6126` / `:2862` / `:17129` 九处 `Has` → `HasCurrentTrait`（或并上 `HasTrait`）· `:11848` doc · 类尾 `HasOathDouble` 简化（净 −7 行，全在最大落点之后） |
| `RuleEngine/Core/EffectResolver.cs` | `:11867` / `:17949` / `:18595` 三处 jam 闸 → `HasCurrentTrait` · `:9392` doc 就地订正（零行数变化） |
| `RuleEngine/Editor/RuleEngineTest_B116W19.cs` | `B117F1TraitConsumers()` 新增 ⑤ 段 8 条断言 · ③-a / ③-d 文案订正 |
