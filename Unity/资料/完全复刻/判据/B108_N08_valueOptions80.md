# N08 · `valueOptions == 80` 回源判据与施工单

> 代理：第一百零八会话 · 查判据 `B108_R3`（只读 · 只写本文件）· 2026-10-31
> 上游：`判据/B107_未完成任务总表.md` §7·1（N08：`EffectResolver.cs:18062` 用 `🔶 自己设计` 的形状等价替原版 80 位）· ✅ 2026-10-31 已转执行（第二步 + 断言已落，见 `报告/B108_R3_exec.md`）
> 前置判据：`判据/B88_R2_80档_VA判定.md`（VA 级已把 80 档公式定死）
> 行号 = 2026-10-31 现读（`EffectResolver.cs` 共 21,024 行；B107 写的 `:18062` 已漂移到 `:18081`）

---

## 〇 一句话结论

**数据位早就在，N08 可只改 `EffectResolver.cs` 一处（+ 一段可选的值公式），不需要动 `CardDef.cs`、不需要改生成器。**
`CardDef.AbilityDamageSpec.ValueOptions` 自 v36（2026-10-28，与 90 档同批）起就在，`CardDatabase.Parse:1749` 每卡挂载，
数据源 = `cards_engine.json` 的 `abilityDamage[].opt`（现 v45）。`EffectResolver.cs:18081-18083` 那句
「我们**没有** `valueOptions` 位……补它要动 `CardDef.cs` ⇒ 白名单外」**已过期**（铁律 5）。
现池 `opt == 80` 恰好 **2 行 / 2 张卡**（`GSC35 Benefictus` / `GSC59 Day of Ascension`），与今天形状判据命中的是**同一批**
⇒ **今天可观察差 = 0，但判据性质（18·0 违规）必须换掉**。

---

## 一 原版调用链（逐跳）

**① 数据位**：卡 SO `cardAbilities[].abilityLogic[].damageCriteria` `{ minDamage, maxDamage, valueOptions, damageType }`
（`dump.cs:19585` `DamageCriteria`；偏移实测 `+0x10`=valueOptions · `+0x14`=minDamage · `+0x18`=maxDamage · `+0x1c`=damageType，
见 `AbilityLogic__PlayAbility.c:1471-1476` 的实参装填）。

**② `PlayAbility` 先算 `multiplierFactor`（= `local_178`）**：`abilityLogic.multiplier`（`+0x30`）→ `GetTargets` 的**表长**（`+0x18`）
= `AbilityLogic__PlayAbility.c:232`；`any(0)` ⇒ **1**（`:190`）；`210/220` ⇒ 累加 `traitsFilter`（`:198-221`）；
再 `if (0 < furtherMultiplier(+0x38)) local_178 *= fm`（`:235-236`）。

**③ 伤害支（`case 10 / 0xd`）**：`AbilityLogic__PlayAbility.c:1466-1508` —— 调 `GetValueFromCriteria(…, **valueOptions**, **multiplierFactor**, …)`
（`:1474-1477`），拿一个整数后 `new DamageInfo` → `DealMultiDamage`（`:1504`）**调一次**；该支没有用 `local_178` 做循环（循环只在 `0x46-0x4d` 召唤 / stun 等分支）。

**④ `GetValueFromCriteria` 的 80 档**：
`AbilityLogic__GetValueFromCriteria.c:113-116`
```c
if (param_7 != 0x50) { return uVar6; }
return (ulonglong)(param_3 + param_4 * param_8);   // minValue + maxValue × multiplierFactor
```
VA 复核：`0x18081b71f cmp eax,0x50` / `0x18081b728 imul r14d,[rsp+0xf8]` / `0x18081b731 lea eax,[rdi+r14]`。
⚠️ **不是 `(min+max)×mult`**，枚举名 `MinPlusMaxTimesMultiplier` 有歧义，以机器码为准。

**⑤ AI 估值同式**：`AbilityLogic__GetAverageValue.c:136-137`（`DamageCriteria.GetAverageDamage` 用）。

**⇒ 原版对玩家的可见结果**：这两张卡的伤害 = `minDamage + maxDamage × (敌方手牌里被 multiplier 筛中的张数)`，
**确定值、不掷骰、整条 ability 只结算一次**。

---

## 二 我们现状（差在哪）

**决策点（就是 N08 指的那一处；行号已漂移）**：
- `EffectResolver.cs:14314-14329` —— 注释块 `🔶 自己设计（非原版）`，意思是「80 档的乘数已折进数值 ⇒ 只结算 1 遍」；
- 它读的是 `IsMultiplierFoldedIntoValue(op, ta)`：`EffectResolver.cs:18088-18093`，
  条件是 **`ta == 50` ∧ `op.AmountRef == "enemyhand"`**（**形状**，不是数据位）。

**差在哪（三条，按严重度）**：

1. **判据性质错（= N08 / 18·0 违规）**：原版这一位是数据字段 `damageCriteria.valueOptions == 80`；
   我们按「乘数池是手牌 + 数值来源文本是 enemyhand」这个**形状**去认。
   形状是**文本解析的副产物**，与原版字段没有必然映射；任何未来同字段的卡都会漏判/误判。
2. **注释里的「拿不到」已不成立**：`EffectResolver.cs:18082-18083` 写「我们**没有**这一位……补它要动 `CardDef.cs`」。
   现读 `CardDef.cs:6797-6811`（`AbilityDamageSpec`，`ValueOptions` 字段，doc 里已写 `0 固定 / 80 min+max×mult / 90 区间`）、
   `:6819-6825`（`AttachAbilityDamage`）、`:6830-6839`（`AbilityDamageAt(ab,lg)`）、
   `CardDatabase.cs:1749`（每卡挂载）、`:2157-2184`（`opt → ValueOptions`）。
   同文件 `:18108-18117` 的 `StampRollPerTarget` **已经在用这条数据位判 90 档** —— 80 档却还在猜形状。
3. **值的来源不同（形状对、数值今天相同）**：我们的值来自卡面文本 `EffectText.cs:3488-3491`（`equal to the number of cards in the
   enemy hand` ⇒ `op.AmountRef = "enemyhand"`）→ `EffectResolver.cs:303` `CountEnemyHandUnitsAndSpells`（只数 `Type == unit/tactic`）；
   原版 = `min + max × multiplierFactor`，`multiplierFactor` 来自 `multiplier` 的 TargetCriteria（`includeMinions=1 · includeSpells=1`，
   按 `cardType` 20 判）—— 敌方手牌里有防御/附魔卡时两者**分叉**（B101_R2 报告 §五·1：39 张 defence + 2 张 enchantment 的 `cardType`
   也是 20，原版会数、我们按 `Type` 不数）。这两张卡 `min=0 / max=1`、夹具也没有 defence/enchantment ⇒ **可观察差 = 0**。

**为什么 18·0（只有服务器相关才许自设计）会判到它身上**：上面第 1 条 —— 决策依据是**我们发明的形状**，
不是原版字段；而原版字段本机**拿得到**（不是服务器数据）。⇒ 必须换回数据位。

⚠️ **判别力（说明这条 clamp 今天是活的）**：`ta=50` 的乘数计数自 `B99_E3` 起**已实现**
（`CountEval.cs:650-661` / `CountEval.cs:1127` 列的 4 行 `mult`：GSC35/37/59/75）。
所以 `TryStructuredMultiplier`（`:14285-14312`）会真的返回 `n = 敌方手牌(单位+战术)数`；
若 80 档不折成 1 遍，GSC35/GSC59 会变成 `n × n`（= 平方）。`TestB88PlayerHandMultiplier` 就是按这个判的。

---

## 三 数据位统计（现读）

| 来源 | 字段路径 | `opt/valueOptions == 80` 条数 | 卡 |
|---|---|---:|---|
| `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（v45 · count 1129） | `cards[].abilityDamage[].opt` | **2 行 / 2 卡** | `GSC35 Benefictus`（unit, `ab0 lg0 min0 max1 opt80 type0`）· `GSC59 Day of Ascension`（tactic, 同四格） |
| `MyGame/Assets/RuleEngine/Resources/cards_truth.json`（1,351 张 SO 全量） | `cards[].abilities[].logic[].damage.valueOptions` | **2 行 / 2 卡**（同上） | 同上；**heal 侧 80 = 0**（`heal.valueOptions` 现读 `0×96 · 210×2`） |
| 池内 `valueOptions` 全分布（cards_engine） | `abilityDamage[].opt` | `0×1504 · 8×37 · 74×90 · 80×2 · 101×1 · 140×1 · 202×1 · 204×1`（共 1621 行） | —— |

**口径说明（防翻倍）**：`cards_raw.json` 不在这两个目录（现读在 `资料/普查产出_1018/allcards_1350/` 与 `_tmp_view/allcards_export/`），每卡有 `cardAbilities` 与 `full_raw.cardAbilities` **两份同构树** ⇒ 递归扫会**翻倍**。本表两条都**没有**用它：
`cards_engine` 的 `abilityDamage` 是生成器按单份 `cardAbilities` 展开的（`gen_cards_engine.py:1770-1784`），`cards_truth` 按 `abilities` 单份递归（命中路径只有 `abilities[].logic[].damage`）。

**同批卡的两条配套数据**（决定 80 档乘数怎么数）：
`unitConditions` 里各 1 行 `role="mult"`：`targetsAffected=50 · enemyUnits=1 · includeMinions=1 · includeSpells=1 · includeHero=0`；
`abilityMultipliers`：`{"trigger":0,"fm":[0]}`（`furtherMultiplier = 0` ⇒ 不乘）。

---

## 四 精确补法

> ⚠️ **落点文件本波归 `B108_A1` 独占**（`文件所有权_当前.md`：`RuleEngine/Core/{RuleCore,EffectResolver,WhenEvent,BattleEvent}.cs`）。
> 本单按调度台铁律 13·3 交回，**由调度台排到 A1 交回之后**（或并入 A1 合并）。`EffectResolverPartial_B88E2.cs` 本波无主，可直接派。

### 4·1 第一步（必做 —— 消掉 18·0，用数据位替形状）

**A. 改调用点** `EffectResolver.cs:14314-14329`：把整段 `🔶 自己设计` 注释与 `IsMultiplierFoldedIntoValue(op, ta)` 换成：

```csharp
// 原版 80 档（`ChooseValueOptions.MinPlusMaxTimesMultiplier`）：乘数是【值】不是【遍数】——
//   公式 `minValue + maxValue × multiplierFactor`，伤害只结算 1 次。
//   判据 = 卡表数据位 `damageCriteria.valueOptions == 80`
//   （`AbilityLogic__GetValueFromCriteria.c:113-116`；VA 0x18081b71f/728/731 → 判据 `B88_R2_80档_VA判定.md` §一）
//   ⇒ 折成 1 遍，⛔ 不再当循环遍数（否则 = 平方）。
if (IsMinPlusMaxTimesMultiplier(card, op))
{
    ctx.Log($"（{op.Source}：这条 `multiplier` 走的是原版 **80 档**（`min + max × mult`）"
          + "—— 乘数**已折进数值** ⇒ 只结算 **1** 遍（⛔ 不再当循环遍数，否则 = 平方）"
          + $"；计数 = {n}）");
    n = 1;
}
```

（日志**保留**「80 档」「计数 = {n}」两个子串 —— `TestB88PlayerHandMultiplier` 现读按它们断言。）

**B. 换掉判定函数** `EffectResolver.cs:18078-18093`（`IsMultiplierFoldedIntoValue`，唯一调用点就是上面那处）：

```csharp
/// <summary>原版 `damageCriteria.valueOptions == 80`（`ChooseValueOptions.MinPlusMaxTimesMultiplier`）。
/// 判据 = 卡表数据位（`CardDatabase.Parse:1749` ← `cards_engine.json.abilityDamage[].opt`），
/// 与同族 `StampRollPerTarget`（本文件 `:18108-18117`，判 90 档）**同一条取数路**。
/// ⚠️ 旧写法是形状等价（`ta==50 ∧ AmountRef=="enemyhand"`），已按 18·0 撤掉（B108_R3 2026-10-31）。</summary>
static bool IsMinPlusMaxTimesMultiplier(CardDef card, EffectOp op)
{
    if (card == null || op == null) return false;
    if (string.Equals(op.AbilityMap, "attached", System.StringComparison.Ordinal)) return false;
    if (op.AbilityIndex >= 0 && op.LogicIndex >= 0)
        return card.AbilityDamageAt(op.AbilityIndex, op.LogicIndex)?.ValueOptions == 80;
    // 兜底（与 `FindSoleMultiplierRow` 同一纪律）：op 没归位，且全卡**恰好一条** abilityDamage 行 ⇒ 用那一行。
    return card.SoleAbilityDamageIs80();
}
```

`SoleAbilityDamageIs80()` 要新增到 `CardDef.cs:6839` 之后（`CardDef.cs` 不在本波所有权表的任何一格 ⇒ 需调度台先认领/派单）。

```csharp
/// <summary>全卡**恰好一行** `abilityDamage` 且其 `valueOptions == 80`（op 未归位时的唯一可用判据）。
/// 理由同 `EffectResolver.FindSoleMultiplierRow`：多于一行就分不清哪行属这条 op ⇒ 不猜。</summary>
public bool SoleAbilityDamageIs80()
{
    if (_abilityDamage == null || _abilityDamage.Length != 1) return false;
    var s = _abilityDamage[0];
    return s != null && s.ValueOptions == 80;
}
```

> 若调度台不愿动 `CardDef.cs`：只留主判据，`spec == null` 时 `ctx.Log` 出声（现池两张卡都归位到 (0,0)，主判据够用）。
> ⚠️ **`spec == null` 时不许退回形状**（那就是把 18·0 又写回来）；改成**出声一次**。

### 4·2 第二步（完整复刻值公式 —— 值也走数据，不再靠文本从句）

原版 = `min + max × multiplierFactor`。今天值由 `AmountRef="enemyhand"` 给，**数值相等纯属 `min=0/max=1`**；完整复刻要把值也接到数据上（顺带修掉 §二·3 的分叉）。

**改** `EffectResolverPartial_B88E2.cs:467` 的 `ApplyRefCardValueOption`（唯一调用点 `EffectResolver.cs:840`）：

```csharp
public static EffectOp ApplyRefCardValueOption(BattleContext ctx, EffectOp op, CardDef card,
                                               int owner = -1, UnitState actor = null)
{
    ...（原有 100/110/120 三段不动）
    // ---- 80 档：minValue + maxValue × multiplierFactor（确定值、不掷骰、只结算一次）----
    if (vo == 80)
    {
        if (spec.Min < 0 || spec.Max < 0) { ctx.Log(...数据缺格...); return op; }
        if (owner < 0) return op;                       // 非出牌链路不猜阵营
        if (!card.TryGetCondition(op.AbilityIndex, op.LogicIndex, "mult", out var ms) || ms?.MultiplierSet == null)
        { ctx.Log($"「{op.Source}」是 80 档，但取不到 `multiplier` 行 ⇒ 保留文本数值"); return op; }
        var cnt = CountMultiplierTargets(ctx, owner, ms.MultiplierSet, ms.Condition, actor, out var gap);
        if (cnt == null) { ctx.Log($"「{op.Source}」80 档的 `multiplier` 数不出来（{gap}）⇒ 保留文本数值"); return op; }
        int fm = op.AbilityFurtherMultiplier > 0 ? op.AbilityFurtherMultiplier : 1;   // PlayAbility.c:235-236
        int v = spec.Min + spec.Max * (cnt.Value * fm);
        var c = op.Clone();
        c.Amount = v; c.AmountMax = v;                   // 原版直接返回值：不是区间、不掷骰
        ctx.Log($"「{op.Source}」的数值 = `min + max × 目标数` = {spec.Min} + {spec.Max} × {cnt.Value}"
              + (fm > 1 ? $" × fm {fm}" : "") + $" = **{v}**（`ChooseValueOptions` 80 档）");
        return c;
    }
    ...
}
```

**调用点** `EffectResolver.cs:840` 改成 `ApplyRefCardValueOption(ctx, op, card, owner, chosen)`。

顺序（现读链路，正确）：`ResolveOps:581`（`AmountRef` 先算文本值）→ `:618`（`TryComputeRepeats` 折成 1 遍）→ `ResolveOneCore:840`（80 档数据值**覆盖**文本值）
→ `DoDeal:3055`。⇒ 文本值退化为**兜底**（数据缺格 / 数不出时仍能跑），⛔ 不要删 `AmountRef="enemyhand"` 那条链。

---

## 五 生成器改动

**无。** 现读证据：
- `工具/gen_cards_engine.py:1755-1784` `ability_damage_of()`：四格 `min/max/opt/type`，**v36 已落**；
- `:5233-5235` / `:5978-5982`：`entry["abilityDamage"] = _admg`（两条卡池路径都已接）；
- `:6732`：列版本注释 `v36 = 加 abilityDamage`；现读 `cards_engine.json` `version = 45`。
⇒ **调度台不需要跑生成器**；`cards_engine.json` 里 `opt==80` 的 2 行现成可读（§三）。

---

## 六 验收断言

**6·1 真实卡（已有，保留原样）**：`RuleEngineTest.TestB88PlayerHandMultiplier`（`RuleEngineTest.cs:7974`）
—— 断 `foe.Health` 掉 `expect` 点、且日志含「80 档 … 计数 = expect」。改完两步后**必须仍绿**。

**6·2 新增「灭自证」断言（防形状回归）** —— 建议落点 `CardPresentation/Editor/B108_W_ValueOptions80_Assert.cs`
（新文件；由调度台接进 `RuleEngineTest.Run` 的 step 列表，⚠️ 参考 2026-10-30 的教训：**别追加在被 `//` 注释吞掉的行尾**）：

- **判别式 A（数据说 80、形状不是）**：夹具卡 `desc = "Deal 2 damage to an enemy troop"`（⇒ `Amount=2/AmountMax=2`，**`AmountRef` 为空**）
  ⇒ 挂 `AttachAbilityDamage({ab0,lg0,min=1,max=2,opt=80,type=0})` + 1 行 `mult ta=50`（`enemyUnits=1 includeMinions=1 includeSpells=1`），
  敌方手牌 3 张合规卡。期望 `1 + 2×3 = 7` 点、**一次**。旧形状判据（`AmountRef` 不成立 ⇒ 不折）给 `2×3 = 6`；只折不接值给 `2`。
- **判别式 B（形状像 80、数据不是）**：同一张夹具但 `opt = 0`（或挂 `[]`）、`desc` 含「equal to the number of cards in the enemy hand」
  ⇒ 旧代码折成 1 遍（=3 点），新代码不折（repeats=3 × 每下 3 点 = 9）。断 `9` ⇒ 谁把形状判据写回来必红。
  ⚠️ 这条是**判别式不是原版语义**（原版不会有这种数据组合），注释里写清。

**6·3 数据不变量**（防生成器漂移，`RuleEngineTest` 里一条轻量断言）：
`cards_engine.json` 里 `abilityDamage[].opt == 80` 的行数 **现读 2**；断「`≥2` 且每行的卡都有 `mult ta=50` 行」，
**数字用现读值并在注释里标明日期**（⛔ 不写死成永久语义）。

**6·4 判别力提醒**：6·2 的两条必须**同时存在** —— 只有 A，会漏「用别的方式折 1 遍」的实现；
只有 B，会漏「值公式没接」。两条的伤害数**互不相同**（7 / 9），不会同时被一个错误实现满足。

---

## 七 没查清的部分（缺哪一行）

1. **没跑到运行时实况**：子代理不能跑 Unity 批处理 ⇒ `TestB88PlayerHandMultiplier` 改后是否绿**本单没实测**，需调度台在同步点跑 `RuleEngineTest.Run`。
2. **`fm > 0 ∧ opt == 80`**：全池 0 行（两张卡 `fm=[0]`）。第二步 `× fm` 那句是按 `PlayAbility.c:235-236` **预防性写的**，无真实卡可验。
3. **主动能力支（`activeAbilityLogic[]`）的 80 档**：`AbilityDamageSpec` 只装 `cardAbilities`；现读 `activeAbilityLogic.dmg.opt == 80` **0 行** ⇒ 本单不写修法（将来照 `StampActiveRollPerTarget`（`CardDef.cs:6849-6863`）先标位再折值）。
4. **`includeSpells` 卡种口径**：第二步依赖 `CountEval` 把 `cardType 20` 的 defence/enchantment 也数进去；原版 `FilterMethods__CheckIfMeetsCriteria.c:286-300` 本轮**没有逐行读** ⇒ 第二步的数值面须回源核一次。
5. **AI 估值侧**：`SimpleAI` 对这两张卡的估值是否用 `Hand.Count`（B88_R2 §五·3 遗留），本单未核。

---

## 八 顺手发现

1. **落点撞文件所有权**：`EffectResolver.cs` 本波归 `B108_A1` 独占 ⇒ 第一步必须排其后（或并入它）；`EffectResolverPartial_B88E2.cs` 本波无主可直接派。
2. **过期注释（铁律 5）**：`EffectResolver.cs:18081-18083`「我们没有 `valueOptions` 这一位」与 `B88_R2_80档_VA判定.md:118`「现在**完全没带**」**都已过期**（同批 `A1c` 当天就加了列）⇒ 交调度台就地订正。
3. **B88_R2 行数口径差**：该文件 `:169` 写「3 行 / 2 卡」（含 1 行 `nonTargetScoringCriteria` 的 AI 打分口径），我们 `abilityDamage` 现读 **2 行**（生成器只遍历 `cardAbilities`，`gen_cards_engine.py:1770`）⇒ 第三行不在我们数据里，只影响 AI 估值。
4. **`CardDef.AbilityDamageSpec` 的 doc 早就预写了 80 档**（`CardDef.cs:6807`）⇒ 数据层当初就按「80 是真档位」建的，只有消费端没跟上。
