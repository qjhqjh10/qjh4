# B141 · W128 · `EffectResolver` 独占包（`B139-V5b` + `ENG-24`）

> 执行写手 `W9` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账主表 / 活状态表 / 所有权表 / 正本 · ⛔ 未重钉基线。
> 判据全部**本次现读**（`d:/2/tools/decomp_full/AbilityLogic__GetValueFromCriteria.c` 全文 ·
> `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29` · `RuleEngine/Core/**`）。

---

## 〇 一句话

`B139-V5b` 已按「零行数变化、只改措辞」修掉同一次破坏事件的两行同文本日志；
`ENG-24` 已把原版 `GetValueFromCriteria` 的 **26 档 + `default => min`** 做成唯一分派表
（`EffectResolverPartial_B141VO.cs`），并把三处散点改成查表。
**`typecheck` 0/0 · `ruleprobe check` rc=0（解析差异 0 行）· `play check` rc=0、结算指纹 `a0e9c63d…` 与基线一致。**

---

## 一 改动清单（含行数变化）

| 文件 | 改什么 | 行数变化 |
|---|---|---|
| `RuleEngine/Core/EffectResolver.cs` | ① `:8107` 的日志措辞（**就地改写，单行换单行**）② `ApplyHealCriteria` 的非 0 档改走分派表（**6 行块换成 6 行块**） | **0**（21991 → 21991） |
| `RuleEngine/Core/EffectResolverPartial_B88E2.cs` | `ApplyRefCardValueOption`：100/110/120 改查表 + 新增 `opt 8/17` 显式警告 | +11（1114 → 1125） |
| `RuleEngine/Core/EnchantmentExecutor_B115.cs` | `TryResolveCriteriaValue` 由「自己的 switch」改成**纯转发**到分派表（签名末尾加 `int owner = -1`；两个调用点传 `owner`） | −23（1938 → 1915） |
| `RuleEngine/Core/EffectResolverPartial_B141VO.cs` | **新建**：26 档分派表 + `default => min` + 自检门 | 新文件 343 行 |
| `RuleEngine/Editor/B141_W9_Assert.cs` | **新建**：A 组 26 档逐档 / B 组 default / C 组 8-17；类名 == 文件名、全局命名空间，**未接聚合行**（由调度台同步点接） | 新文件 321 行 |

⚠️ `EffectResolver.cs` 两处都是**等行替换** ⇒ `:8107` 与全部下游账落点行号**一个都没漂**（现读 `:8107` 仍是那一行；文件总行数 21991 未变）。

---

## 二 `B139-V5b` —— 同一次破坏事件产生 2 行同文本「…的监听器」日志

### 1 两处打印点（现读）

| # | 位置 | 改前文本 |
|---|---|---|
| ① | `EffectResolver.cs:10452-10453` | `—— 事件「{kind}」触发：「{u.Name}」的监听器 → {names} ——` |
| ② | `EffectResolver.cs:8107` | `「{card.Name}」的监听器结算完毕 → 触发事件 \`{tn}\`（原版 AbilityTrigger）` |

两行的卡名是同一张（`EmitListenerTrigger(ctx, u.Card, owner, u, …)` ⇒ `card.Name == u.Name`），
而 `RuleEngineTest.WhenFired`（`:20825-20832`）的 needle = `「<卡名>」的监听器`
⇒ 一次结算被数成 **2 次**。

### 2 改法（只动 `:8107`，⛔ 没动 `:10452`）

```csharp
// 改前
ctx.Log($"「{card.Name}」的监听器结算完毕 → 触发事件 `{tn}`（原版 AbilityTrigger）"); return evt;
// 改后
ctx.Log($"事件 `{tn}` 已由监听者「{card.Name}」派发（原版 AbilityTrigger）"); return evt;
```

改后这一行**不含** `「<卡名>」的监听器`、也不含 `的监听器 →`。

### 3 为什么必须留 `:10452`

- `RuleEngineTest.cs:31390` 的 `log.LastIndexOf("的监听器 →")` 认的是 ①的格式；
- `RuleEngineTest.cs:20818` / `:35076` 的 doc 也指 ①；
- `BattleChatAssertE7.cs:145` 数 `"的监听器" >= 1`（①仍满足）。
⇒ **⛔ 别动 10452；⛔ 别改测试 needle（`RuleEngineTest.cs:20825-20832`）。**

### 4 验收（grep 现读）

- `grep -n "的监听器 →" RuleEngine/Core/EffectResolver.cs` ⇒ **只有 `:10452`**（其余命中都在 `RuleEngineTest.cs` 的注释/读口里）。
- 运行时 needle `「<卡名>」的监听器`：`EffectResolver.cs` 里**只有 `:10452`** 这一条日志会命中
  （`:8107` 的新措辞不再含该子串；文件内其余 `的监听器` 命中全是注释）。
⇒ **同一次事件只剩 1 行命中 needle。**

---

## 三 `ENG-24` —— `ChooseValueOptions` 26 档分派表

### 1 唯一实现

`RuleEngine/Core/EffectResolverPartial_B141VO.cs` 的

```csharp
internal static bool TryResolveValueOption(BattleContext ctx, int vo, int min, int max, int multFactor,
                                           UnitState thisCard, UnitState targetCard, UnitState refCard,
                                           int owner, out int value, out string gap)
```

形参 ↔ 原版 `.c`：`param_3=min` · `param_4=max` · `param_5=thisCard` · `param_6=targetCard` ·
`param_7=vo` · `param_8=multFactor` · `param_9=refCard`。
枚举成员逐值取自 `cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29`。
`gap` 非空 = 本刻读不出来（调用方「不结算 / 沿用正文」）；`false` ≠ 「原版这里没有值」。

### 2 26 档逐档（值 → 算式 → 判据行号）

| 值 | 枚举名 | 我们的算式 | 判据（`AbilityLogic__GetValueFromCriteria.c`） |
|---|---|---|---|
| 0 | Normal | `min == max ? min : GetRandomInt(min, max+1)` | `:36-46` |
| 10 | TargetMeleeAttack | `min == 0 ? t.Melee : min × t.Melee` | `:47-52` + 收尾 `:121-126` |
| 20 | TargetRangeAttack | 同上，`t.Range` | `:57-61` |
| 30 | TargetHealth | 同上，`t.Health` | `:62-79` |
| 40 | ThisCardAttack | 同上，`this.Melee` | `:82-86` |
| 41 | ThisCardAttackRanged | 同上，`this.Range` | `:87-92` |
| 50 | ThisCardHealth | 同上，`this.Health` | `:93-119` |
| 60 | TargetDamage | 同上，`t.CurrentMaxHealthProcessed − t.Health` | `:120-124` |
| 70 | ThisCardDamage | 同上，`this.CurrentMaxHealthProcessed − this.Health` | `:126-130`（`:112` 把 `param_6 = param_5`） |
| 80 | MinPlusMaxTimesMultiplier | `min + max × multFactor` | `:113-116` |
| 90 | RandomizedOnResolution | `min`（逐目标掷在 `StampRollPerTarget`） | `:130` |
| 100 | RefCardAttack | `ref.AttackBase`（**不乘 min**） | `:138-160` |
| 101 | BaseRefCardMAttack | `RefCardBaseAttack(ctx, min)`（现池恒 `min`） | `:157-177` |
| 110 | RefCardHealth | `ref.Health`（**不乘 min**） | `:172-190` |
| 120 | RefCardCost | `RefCardCostOf(ctx, ref)`（**不乘 min**） | `:195-207` |
| 130 | TargetCost | `min == 0 ? cost : min × cost` | `:214-220` |
| 140 | OwnWarlordAttack | `warlord.Melee × multFactor`（**不乘 min**） | `:243-252` |
| 150 | TargetBastion | `min == 0 ? v : min × v` | `:253-256` |
| 160 | SelfBastion | 同上 | `:258-270` |
| 170 | TargetCardHealthMinusAttack | `min == 0 ? v : min × v`，`v = t.Health − t.Melee` | `:278-283` |
| 200 | ThisCardMarksOfChaos | `min + this.MOC × max` | `case 200`（`:278-290`） |
| 201 | OtherCardMarksOfChaos | `min + target.MOC × max` | `case 0xc9`（`:283-288`） |
| 202 | AllFriendlyMarkOfChaos | `min + Σ(友方在场 MOC) × max` | `case 0xca`（`:288-320`） |
| 203 | ThisCardShuriken | `min + this.Shuriken × max`（转发 `TryResolveCriteriaValue203`） | `case 0xcb`（`:320-325`） |
| 204 | FaithValue | `Players[side].Faith`（**不乘 min**） | `case 0xcc`（`:325-333`） |
| 210 | TargetExcessDamage | `t.Health >= 0 ? 0 : −(t.Health × min)` | `case 0xd2`（`:333-366`） |

**兜底**：`default => value = min`（原版每一处 `return uVar6`）。
**两条显式分流**（都在 switch 之前）：
- `vo < 0` ⇒ `false` + 出声（**我们自己的「缺格」哨兵**，原版没有负数档；⛔ 不拿 `min` 顶）。
- `vo == 8 || vo == 17` ⇒ `false` + 出声（不在 26 档里，见 §五）。

### 3 三处散点：改前 / 改后

| 散点 | 改前 | 改后 |
|---|---|---|
| `EffectResolverPartial_B88E2.cs` `ApplyRefCardValueOption` | 只认 80/100/110/120；100/110/120 是**手写三元**取值；其余原样退回 | 80 仍走 `ApplyMinPlusMaxTimesMultiplier`；100/110/120 走 `TryResolveValueOption`；**新增 8/17 显式警告**；其余仍原样退回（正文路的 140/101/204/210/202 不在这里二次结算） |
| `EnchantmentExecutor_B115.cs` `TryResolveCriteriaValue` | **自己一份 switch**：0/10/20/30/40/41/50/80/90/203；`default` ⇒ 出声不结算 | **纯转发**到分派表（签名末尾加 `int owner = -1`；两个调用点传 `owner`）；8/17 仍 `false` |
| `EffectResolver.cs` `ApplyHealCriteria` | `vo != 0` ⇒ 一律「本仓没有那个求值档」+ 沿用正文 | `vo != 0` ⇒ 先问分派表（**本口在目标解析之前**，需要目标的档返回 `false` + `gap`）⇒ 拿得到就覆盖、拿不到沿用正文并出声。`210` 因此**走表**；今天目标未知 ⇒ 仍落到正文 `excess` 路（**净行为不变**） |
| `EffectResolver.cs` `StampRollPerTarget`（90） | 只决定「要不要逐目标掷」 | **不动** —— 它不是取值（表里 90 = `min`，与它互补） |

### 4 可达性 / 未接线（如实标）

- **`201` 消费者在 buff 路**（`BL43` 的 `meleeAttackValueOptions` / `BL47` 的 `armourValueOptions`），
  而**那条路今天没有任何读口**（全仓 `grep "\.ValueOptions"` 只在**数据定义**处命中）⇒
  分派表把 201 **实现好了**，但接线点在 `RuleCore.cs`（**本包白名单外**）⇒ 今天**可达 0**，报告里标着。
- **`default => min` 今天可达 0**：现池所有 `valueOptions` 里「不在 26 档」的只有 `8` 与 `17`，
  而这两个被显式分流（§五）⇒ 兜底只在「将来出现别的不认识值」时生效。
- **`opt 8 / 17` 的行为一点点都没变**（见 §五）。

---

## 四 引擎读数（⛔ 未重钉基线）

| 命令 | 结果 |
|---|---|
| `bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `bash 工具/ruleprobe.sh check` | `=== 全池 1134 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 ===` · 哨兵 0 条 · **rc=0** |
| `bash 工具/ruleprobe.sh play check` | 结算指纹 **`a0e9c63d7bfa33735f5dad8230a26edd`** · 与基线**一致** · ①-a/①-b 逐卡差异 0 · **rc=0** |

**指纹变化 = 无**（新指纹与基线逐字相同）。
**为什么纯重构没变**：三处散点/兜底在现池**没有可达行**——
`201` 的消费者在 buff 路（今天没有读口）、`default` 只对 8/17 以外的不认识值生效（现池没有）、
`100/110/120` 现池 0 行（R3 §三·3 现读）、附魔 `doDamage`/`doHeal` 也没有需要新档的行。
⇒ **不是因为「为了保指纹留错值」**：201 与 default 都按原版实现了，只是今天没有输入能把它们喂到。

---

## 五 `opt 8` / `healthValueOptions 17`（❓语义来源未查清）

- `ChooseValueOptions.cs:1-29` 现读 **26 个成员，无 8 / 无 17**；`dump.cs` 同值。
- `opt 8`：`abilityDamage` 里 **37 行 / 24 卡**，**全部 `damageType = 5`（`DamageType.Random`）**
  （`Sawbonez` 卡面「Deal 1-2 damage」）。
- `healthValueOptions 17`：**8 行 / 5 卡**（GOF30/31/32/36/43）。
- 原版 `.c` 对它们会落到 `return min`（`< 0xb` 的非 0/10 一律 `uVar6`）——
  但照做会把「Deal 1-2」压成恒 1 ⇒ **不接管**。
- 处理：分派表 `case 8/17` ⇒ `false` + `gap`（**出声**）；`ApplyRefCardValueOption` 里再加一条 `ctx.Log` 警告
  （`damageCriteria.valueOptions = 8/17 … 不在原版 26 档里 …`）。
  ⇒ 行为**逐字保持改前**（正文 / 卡表 `min/max` 照旧），**且不再静默**。
- **❓ 未查清**：这两个值是新版枚举还是残留列 —— 搜过 `ChooseValueOptions.cs` · `dump.cs:20865-20895` ·
  `Warpforge_code/.../AbilityEffect.cs` · `AbilityLogic__GetValueFromCriteria.c` 全文 · `cards_engine.json` 全卡扫描，
  **没有查到定义**。判据一旦查到 ⇒ 回到分派表补两条真正的 `case`。

---

## 六 断言（`RuleEngine/Editor/B141_W9_Assert.cs`，未接聚合行）

- **A 组（26 档逐档）**：每个值一条 `Eval`，前置硬检查（近战 / 远程 / 当前血 / 最大血 / 费用 / Shuriken /
  混沌印记 / Bastion / 信仰 / 督军近战）都写成 `Check(...)`（⛔ 不用 `if (前提) Check(...)` 静默跳过）。
  含三种收尾的鉴别：`min == 0 ⇒ 原值`（10/130/170）、`min × 原值`（10/30/40/50/130）、`min + 原值 × max`（200/201/202/203）、
  **不乘 min** 的六级（100/101/110/120/140/204）。
- **B 组**：`999 → min`、`37 → min`（不是 `min + max×f`）、`1000 + min=0 → 0`、负数哨兵 `-1 → false + gap`。
- **C 组**：`8` / `17` 返回 `false` 且 `gap` 点明「不在 26 档里」；**数据面**用 `AttachAbilityDamage` 造一条 `opt 8`
  的 `deal` op 喂 `ApplyRefCardValueOption`，断言 `Amount/AmountMax` **原样不动**且 `ctx.Events` 真多出一条警告。
- **灭自证**：把 `default` 改回「不结算」⇒ B1/B2 红；把 8/17 交给 `default => min` ⇒ C1/C3/C4 红；
  把 203 写成 `min × 值` ⇒ A24 红；把 80 写成 `(min+max)×f` ⇒ A10 红。

---

## 七 还差什么 / ❓

1. **`201` 的消费者（buff 路）** 在 `RuleCore.cs`，白名单外 ⇒ 本包只交实现，**未接线**。
2. **❓ `opt 8` / `17` 的语义来源**（§五）。
3. **断言未执行**（本包禁跑 Unity；聚合行接线由调度台同步点做）。
4. `202` 的 trait `0x14a` 与 `DefinedTrait` 的对应，本次沿用 `KeywordTable.MarkOfChaos`
   （`EntityScript__get_CurrentMarksOfChaos.c` 同址，属**直接判据**；「Dark Pact」是旁证）。
