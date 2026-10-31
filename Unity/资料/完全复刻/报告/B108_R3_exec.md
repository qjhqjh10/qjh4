# B108_R3 执行报告 —— N08 `valueOptions == 80`（第二步求值 + 灭自证断言）

> 代理：第一百零八会话 · `B108_R3`（查判据转执行）· 2026-10-31
> 判据单：`资料/完全复刻/判据/B108_N08_valueOptions80.md` · 前置判据：`判据/B88_R2_80档_VA判定.md`
> 本波所有权：本代理独占 `RuleEngine/Core/EffectResolverPartial_B88E2.cs` + 新建 `CardPresentation/Editor/B108_N08_Assert.cs`；
> `EffectResolver.cs` 归 `B108_A1`（本代理**未碰**）· 断言宿主 / 账主表 / 生成器 / git / Unity 批处理 均未碰。

---

## 〇 一句话

80 档的**两半**现在都在：折 1 遍 = `B108_A1` 已落（`EffectResolver.cs:14323` `IsMinPlusMaxTimesMultiplier`，数据位判据）；
**值 = `min + max × 目标数`** = 本代理落（`EffectResolverPartial_B88E2.cs` 新增 `ApplyMinPlusMaxTimesMultiplier`，分流点 `:475`）。
**还差一跳接线**：调用点 `EffectResolver.cs:840` 要传 `owner, chosen`（精确补丁在 §二，请转 `B108_A1`）。

---

## 一 改动清单（只有两个文件）

### 1·1 `RuleEngine/Core/EffectResolverPartial_B88E2.cs`（本代理独占）

| 位置 | 改法 | 说明 |
|---|---|---|
| `:467` 签名 | `ApplyRefCardValueOption(BattleContext ctx, EffectOp op, CardDef card)` → `(..., int owner = -1, UnitState actor = null)` | 可选形参 ⇒ 现有唯一调用点（3 实参）照旧编得过；未接时 80 支**出声并保留文本值**，不静默生效 |
| `:475` | 在 `int vo = spec.ValueOptions;` **同一物理行**后追 `if (vo == 80) return ApplyMinPlusMaxTimesMultiplier(ctx, op, card, spec, owner, actor);` | **零行数变化**（铁律 18·b）——该文件 `:568…:1023` 有 20 条账落点 |
| `:1061` 类尾追加 | 新方法 `public static EffectOp ApplyMinPlusMaxTimesMultiplier(...)`（约 80 行，含判据 doc） | 追加在**最后一个成员之后**（原文件 1032 行，全部账落点 ≤ `:1023` ⇒ 零漂移） |

新方法逐句（与原版对齐）：
1. `spec.Min/Max` 缺格（`< 0`）⇒ 出声 + 保留文本值；
2. `owner < 0`（调用点未接线）⇒ 出声 + 保留文本值（日志里写明接线方式）；
3. 取 `mult` 行（`card.TryGetCondition(ab,lg,"mult")`）；`MultiplierSet == null` / `Exclude` ⇒ 出声 + 保留；
4. `targetsAffected == 0 (any)` ⇒ `multiplierFactor = 1`（`AbilityLogic__PlayAbility.c:190`）；`Unknown` ⇒ 出声 + 保留；
   其余走 `CountMultiplierTargets`（= `CountEval` 的 `ta=50` 手牌支），数不出 ⇒ 出声 + 保留；
5. `fm = op.AbilityFurtherMultiplier > 0 ? … : 1`（`PlayAbility.c:235-236`）；
6. `v = spec.Min + spec.Max × (cnt × fm)`；`c.Amount = c.AmountMax = v`（**不是区间、不掷骰**）；出声一行。

判据三源：VA `0x18081b71f/728/731` · `decomp_full/AbilityLogic__GetValueFromCriteria.c:113-116` · `AbilityLogic__PlayAbility.c:181-236`（factor 来源）+ `:1504`（只结算一次）。
逻辑里 `AmountRef` 那条链**保留**当兜底（数据缺格 / 数不出时仍按原口径跑）。

### 1·2 `CardPresentation/Editor/B108_N08_Assert.cs`（新建）

`public static partial class RuleEngineTest`（与宿主同 partial ⇒ 复用 `Section / Check / CheckTrue / CheckCode / LogTail /
Place / Unit / HandIdx / ToP1Turn / GaugeCard / B101R2_B88Battle` 等私有夹具，**不新建第二套**）。入口：`CheckB108N08ValueOptions80()`。

夹具 = 一张 `Rally` 单位 + 一条 `mult` 行（`ta=50 · enemyUnits=1 · includeMinions=1 · includeSpells=1`，其余 30+ 格显式 0，
因为 `CountSetSpec` 默认是 `Unknown(-1)`，不显式落 0 会被判「未知 ⇒ 数不出来」）+ `abilityDamage` 一行。
敌方起手 = 2 单位 + 1 战术 + 1 督军（督军被 `includeHero=0` 排除）⇒ 计数 = 3。

| 用例 | 夹具 | 期望 | 能红在哪 |
|---|---|---|---|
| **A**（数据说 80、形状不是） | 正文 `Deal 2 damage to an enemy troop`（`Amount=2`、**无 `AmountRef`**）+ `damageCriteria{min 1,max 2,opt 80}` | **7 点、一次**（`1 + 2×3`） | 值那半没接 ⇒ **2**；折 1 遍没接 ⇒ **21**；判据退回形状 ⇒ **21**；调用点未接线 ⇒ **2** |
| **B**（形状像 80、数据不是） | 正文 `… equal to the number of cards in the enemy hand`（`AmountRef=enemyhand`）+ `damageCriteria{opt 0}` + 同一条 `mult` | **9 点**（3 遍 × 每下 3） | 把形状判据写回来（`ta==50 ∧ AmountRef=="enemyhand"` ⇒ 折成 1 遍）⇒ **3** |

外加：A 断日志里有「`min + max × 目标数`」（= 值那半真的走了数据位，而不是兜底）；B 断**没有**这条日志（反例侧）。

**接线片段（调度台改宿主 `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()`）**：紧接
`        Step(TestB88PerTargetRoll);` 之后（同一物理行追加、插在该行第一个 `//` 之前）：
```csharp
            { CheckB108N08ValueOptions80(); }
```

---

## 二 依赖交回：`EffectResolver.cs:840` 的调用点补丁（不动该文件）

文件 = `RuleEngine/Core/EffectResolver.cs`（`B108_A1` 独占）。**现读原句**（`:840`，`ResolveOneCore` 内，`owner` / `chosen` 都在作用域）：

```csharp
            if (!payOnly && card != null) op = ApplyRefCardValueOption(ctx, op, card);
```

**改成**（其余一字不动）：

```csharp
            if (!payOnly && card != null) op = ApplyRefCardValueOption(ctx, op, card, owner, chosen);   // 🆕 B108_N08：80 档求值要 owner/actor（`min + max × 目标数`）；缺了会走「没递 owner ⇒ 保留文本数值」出声支
```

⇒ 这一步不落，**A 会红（实得 2）**，B 不受影响。建议与 A1 的第一步同批合并。

---

## 三 §没查清·4 的回源（`includeSpells` 是否数 `cardType 20` 的 defence/enchantment）

**结论：原版会数它们；第二步用 `CountMultiplierTargets`（`CountEval` 的 `ta=50`）与原版一致；今天走的 `AmountRef` 路（只数 `Type == unit/tactic`）反而**少数**。** 证据链三条：

1. `dump.cs` `TargetCriteria`（TypeDefIndex 463）字段偏移：`includeSpells` = **`+0x18`**（`includeHero +0x16 · includeMinions +0x17 · includeWhispers +0x19`）。
2. `FilterMethods__CheckIfMeetsCriteria.c:286-300`：候选 `cardType`（`+0x28 → +0x94`）= `0x14 (20)` 时，**要求 `param_4 + 0x18 != 0`** —— 即 `cardType 20` ⇔ `includeSpells`，无额外卡种细分。
3. 我们的数据（来自卡 SO）：`cards_engine.json` 现读 `cardType` 分布 = `0 unit ×586 · 10 hero ×56 · 20 {tactic 446 · defence 39 · enchantment 2}`；
   实现侧 `TargetCriteriaEval.TypeGatePasses`（`:1011-1013`）同样把 `tactic|defence|enchantment → IncludeSpells`。
   ⇒ 原版**会把**敌手里的 defence/enchantment 数进 `multiplierFactor`，而我们的 `EffectResolver.CountEnemyHandUnitsAndSpells`（`Type` 只看 unit/tactic）**不会**。
   第二步改用 `CountMultiplierTargets` 后，计数口径与原版一致（顺带修掉 `B88_R2` §四·3 的潜在边差）。

⚠️ 只核到「字段偏移 + 单一分支 + 数据分布」三处，**没有**跑到原版实况（原版已关服、本代理不能起 Unity）⇒ 仍是「代码字段级」结论。

---

## 四 验证

- **类型检查**：`TMPDIR=/tmp/wf_b108r3 bash d:/4/Unity/工具/typecheck.sh`
  第 1 次 = 运行时 6 条错（全在 `CardPresentation/Deck/DeckRuntime.cs`，**`B108_A3` 在飞的文件**，与本代理无关）⇒ 按纪律等了一会儿重跑；
  第 2 次 = **运行时 0 / 编辑器 0**，且 `$TMPDIR/wfcheck/WFCheck.dll` 的 mtime（15:47:42）**晚于**本代理两个文件（15:45:20 / 15:46:40）⇒ 不是旧 DLL 的假绿。
- **未跑**：`RuleEngineTest.Run`（Unity 批处理归调度台）⇒ A/B 两条断言的**红绿未实测**，需在调用点补丁 + A1 第一步都在位后跑。

---

## 五 账补丁

`资料/完全复刻/账/_parts/B108_N08.tsv`（11 列表头 + 1 行，LF、字段内无制表/换行）。
- 键 = `战斗逻辑驱动 / AbilityLogic / GetValueFromCriteria(…完整签名…)`（与现主表第 77 行同键）。
- **⚠️ 与 `B108_A1.tsv` 同键**：本补丁是**在 A1 那行基础上**叠的（保留了 A1 的备注 / 第一步落点 / 「第二步归 B108_R3」那句已改成「已接」）。
  （脚本按**文件名**读 A1 补丁 + 主表当时值生成；若 A1 之后又改了它的补丁，以本文件为准即可 —— 本行已含两半。）
- **合并顺序：`--only B108_A1` 先、`--only B108_N08` 后**（字典序本来就 N08 在后）。⛔ 别整体 `--only B108_` 之后再手工改这两行。
- 落点行号（`EffectResolver.cs:14323/:18087/:840`、`EffectResolverPartial_B88E2.cs:475/:1061`、`CardDef.cs:6839`）都是**现在**读数；
  调用点一旦接线、A1 若再动文件，按 `check_ledger_points --fix` 重钉。

---

## 六 没查清 / 边界（如实）

1. **A/B 未真跑**（见 §四）。
2. `furtherMultiplier > 0 ∧ opt == 80` 全池 **0 行** ⇒ `× fm` 那半是按 `PlayAbility.c:235-236` 预防性写的，**无真实卡可验**。
3. 主动能力支（`activeAbilityLogic[].dmg`）的 80 档现池 **0 行**、未接（`AbilityDamageSpec` 只装 `cardAbilities`）；将来出现照 `StampActiveRollPerTarget`（`CardDef.cs:6849-6863`）同法。
4. 原版 `heal.valueOptions == 80`：现读 **0 行**（`heal` 只有 `0×96 · 210×2`），且本仓求值入口只处理 `Verb == "deal"` ⇒ 未接。
5. `includeSpells` 只到「字段级」；未跑原版实况（§三末）。

---

## 七 顺手发现（供调度台分流）

1. **`CountEnemyHandUnitsAndSpells`（`EffectResolver.cs:18135-18148`）与 `CountMultiplierTargets` 两条计数口径并存**：前者按 `CardDef.Type`（unit/tactic），后者按 `cardType` 20（含 defence/enchantment）。第二步接线后，**`AmountRef="enemyhand"` 的价值只剩「数据缺格时的兜底」**；若将来要收口到一处，删前者前先确认 `AmountRef` 仍有人用（faith/spirit/ownwarlordmelee/refcardbaseattack 同表）。
2. `B88_R2` §三·1 的过期结论已按铁律 5 就地订正（保留更正痕迹，见该文件 `:118`）。
3. `RuleEngine/Editor/RuleEngineTest_B101R2.cs` 的 `GaugeCard` 注释里有一句「`enchantment` 的 `cardType` 也是 **20**、原版会数它」—— 本代理 §三 的三条证据支持这句（原版确实会数），可以把它从「未定」升级为「已核（字段级）」。
