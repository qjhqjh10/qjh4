# B89 · E4 —— `UnitState` / `Enchantment`（批次 7：记录与到期原语）

- 写手：B89 第 1 波执行代理 `E4` · 2026-10-28
- 白名单（只动了这两个）：`MyGame/Assets/RuleEngine/Core/UnitState.cs` · `MyGame/Assets/RuleEngine/Core/Enchantment.cs`
- 验证：`TMPDIR=/tmp/wf_B89E4 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**
- 行数：`UnitState.cs` 3253 → **3397**（新增 144 行**全部在类尾**；账对它的最高引用 = 3177 ⇒ **零漂移**）·
  `Enchantment.cs` 1375 → **1505**（新增 130 行**全部在类尾**；账最高引用 = 1324 ⇒ **零漂移**）
- 行尾：两个文件都是纯 `LF`（`CRLF 0 / bareCR 0`，`io.open(…,'rb')` 数出来的）

---

## §1 逐条结论

| 账行 | 原版判据（现读） | 改动（文件 : 符号 @ 现读行号） | 状态建议 |
|---|---|---|---|
| **L875**<br>`CardScript.ResolveUnitAttacked(…)` | `untilNextAttack(+0x36)` 的到期档：两处扫点形状相同 —— `CardScript__UsedActiveAbility.c:66-84`（收尾，`:20` 先 `attacksThisTurn += 1`）与 `CardScript__ResolveUnitAttacked.c:79-94`（`actingCard == this` 支）。扫 `activeEffects(+0x108)`，`+0x36` 为真 ⇒ `SendRemoveEffect(card, 效果, 1)`（= 入队一条 `battleActionType 0x10`）。ILSpy 对应 `CardScript.cs:14113-14129` | `UnitState.cs:2781`（`EffectRecord.UntilNextAttack` 列）· `:3351`（`TimingBitUntilNextAttack = 0x10`）· `:3354` / `:3363`（`IsUntilNextAttack` 两个重载）· `:3383`（`ExpireUntilNextAttack()`） | **B**（原语全建、接线未做，见下） |
| **L1042**<br>`EntityScript.HasEnchantment(AbilityTrigger)` | `EntityScript.cs:1806-1841`：逐件 `ActiveEnchantment` 的 `enchantmentCard.HasAbilityType(trigger)` = 该卡 `cardAbilities` 里有没有这个 trigger（只看 trigger）。三个调用点：`CardScript.cs:6827`（`HasWhileInPlayAbility()`，`WhileInPlay ∨ WhileInPlayVariable ∨ pack-trait`）· `:14072`（`mob-trait ∨ Mob`）· `:14298`（`rage-卡面/当前 ∨ Rage`） | `Enchantment.cs:1448`（`HasEnchantmentForTrigger(CardInstance,string)`）· `:1464`（`UnitState` 重载）· `:1479`（`HasWhileInPlayGate`）· `:1490`（`HasMobGate`）· `:1500`（`HasRageGate`）· `:1422/:1425/:1428/:1431`（四个 trigger 名字常量） | **B**（三个闸建成、调用点未接） |
| **L1376**<br>`EntityScript.get_displaySummonSickness()` | `.c:1-16` 全方法体 = `+0x58 && !fast(0x28) && !flank(0x1cc)`。**加** trait 走 `AddEffect` `case 3`，`.c:570-572` 对 fast/flank **一定**调 `UpdateSummonSicknessAnims`；**摘**走 `RemoveEffect → RemoveTraitSilently` 时**不调**（`RemoveTraitSilently.c` 全文 55 行，`grep -c UpdateSummonSicknessAnims` = **0**） | `UnitState.cs:1548`（`AddAuraKeyword` 尾部那一句）· `:3295`（`RecomputeActAfterAuraKeyword`） | **B**（账里那条「未走重挂重算」已闭环；新记一条「收回侧比原版严」，见 §3·1） |

### 逐条「还差什么」

**L875 —— 已补的是「原语」，不是「接线」**
- 记录列：`EffectRecord.UntilNextAttack` = 原版 `CardEffect.untilNextAttack(+0x36)`。
  ⚠️ **零行数变化**的落法：它**加在 `Permanent` 的同一行**。理由 —— `EffectRecord` 在文件中部（2781），另起一行会让下游 6 处账落点整体 +1 漂移，其中 **5 条状态是 B，`check_ledger_points --fix` 修不了**（只有 A 能自动修）⇒ 按「零行数变化优先」处理。
- 判定原语：`IsUntilNextAttack(int timingBits)`（`TraitEffectRecord.TimingBits` 的 **bit4**，位序判据在 `TraitEffectRecord.TimingBits` 的 doc）+ `IsUntilNextAttack(EffectRecord)`。
- 到期口：`ExpireUntilNextAttack()` —— 今天真正带列表的记录账只有 `_traitEffects`（`EffectRecord` 那个类只是「调用方把相关列投进来」的容器，没列表），所以扫描落在它上面；每条走 `RemoveTraitEffect`（= 原版 `RemoveEffect` 的 `case 3`）。
- **仍差三项**：
  1. **触发器接线**（本批明令不动 `RuleCore`）—— 见 §5 的两个落点；
  2. 原版**入队**再摘（`SendRemoveEffect → AddAutoActionToQueue`），我们**同步**摘；
  3. `permanentBuff(+0x74) ∧ untilNextAttack` 的组合原版第一句就 `return`；`TraitEffectRecord` **没有 `Permanent` 列**。等价性：每条记录都来自 `ApplyTraitEffect`，它没有 `Permanent` 形参 ⇒ 恒 false ⇒ **今天等价**。

**L1042 —— 三个闸的「附魔那一半」建好了，另一半是 trait（不是本层）**
- 通用口 `HasEnchantmentForTrigger(carrier, triggerName)`，**三值口径**：`AbilityTriggerPresence` = `1` 真 / `0` 跳过 / **`-1`（旧表缺列）退回关键词口径**（`TriggerOps` / `Effect`，与 `HasAboutToAttackAbility` 同一套兜底）。⛔ 不把「判不了」读成「没有」。
- `enum → 关键词` 映射只用一处数字表：`CardDef.AbilityTriggerNumberOfName`（现读 `WhileInPlay 20` / `Rage 130` / `WhileInPlayVariable 240` / `Mob 300`），⛔ 没抄第三份。
- 🔴 **`pack` / `mob` / `rage` 在原版是「trait」析取项，不是附魔** —— 三个 gate 的 doc 里都写死了「调用方自己 OR 上 `u.Has(…)`」。`HasWhileInPlayGate` 只等于 `HasEnchantment(WhileInPlay) || HasEnchantment(WhileInPlayVariable)`。
- **仍差**：调用点未接（归批次 4 的 `E3`）。

**L1376 —— 账里那条判据悬案结案了**
- 悬案原文（`RuleCore.RecomputeActAndAttack` 的 doc 末段）：「原版光环给的关键词走的是哪条路**还没查清**（`AddTraitSilently` 不重算、`UpdateWhileInPlay` 那一支没读到实据）」。
- 实读结论：**两半都对，只是问的是不同的方法** —— `AddTraitSilently` 自己不重算（`.c` 里确实没有），但**它的调用方 `AddEffect` 在 `.c:570-572` 补了这一跳**，而光环/附魔挂 trait 的**唯一**入口就是 `AddEffect`。（另注：`.c:570` 那个 `if` 在 `if (!bVar2) goto LAB_1805e59f9;` **之后**，但 `LAB_1805e59f9` 是「新建那条记录」的分支，它执行完会**落到同一个尾巴** ⇒ **新建与合并两条路都经过 `.c:570`**。这一点专门核过，否则会误判「只有合并才重算」。）
- ⇒ 我们的落法：`AddAuraKeyword` 在 `fast` / `flank` 上立刻 `RuleCore.RecomputeActAndAttack(this)`。**只在光环挂词那一路生效**（`AddKeyword` 那一路仍由它自己尾巴上的 1194 行负责，⛔ 没有把判断搬进 `SyncKeywordState` —— 那会变成同一条判据的第二份写法）。

---

## §2 验证读数

**已跑（我这一侧）**
```
$ TMPDIR=/tmp/wf_B89E4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

**要在同步点跑（我跑不了 `ruleprobe` / Unity）**

| # | 命令 | 预期 | 若不符怎么办 |
|---|---|---|---|
| 1 | `bash 工具/ruleprobe.sh play check` | **指纹逐字节不变**（理由：L875 / L1042 都是 **0 调用点**的原语；L1376 只影响带 `fast`/`flank` 光环的 4 张卡） | 见下「L1376 的隔离与定位」 |
| 2 | `bash 工具/ruleprobe.sh play all dump`（可选） | 同上不变 | 同上 |
| 3 | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | `UnitState.cs` / `Enchantment.cs` **零新增漂移** | 若报，先看是不是别的写手动了同文件（见 §3·4） |

**L1376 的隔离验证（按要求写清做法）**

三条改动**互相独立**，而且 L875 / L1042 都是 0 调用点 ⇒ **本批天然只有一个可变量 = L1376**，不需要做「关掉哪一条」的开关。

1. **正向**：`play check` 前后比指纹。若**变**，只应出现在 `AM73 Vitus Gryf` / `DA30 Ravenwing Talonmaster` / `EC8 Alluress` / `TAU31 Devilfish` 这 4 张光环卡的对局里。
2. **方向判据**：变化必须是「**该能动的变能动了**」（`Exhausted` / `CannotAttackThisTurn` 由 `true` 变 `false`），⛔ 不允许出现反方向。
3. **定位**：`bash 工具/ruleprobe.sh play <卡id>` 看那两张 bool。
4. **反向对照（证明改动真生效，只在同步点做）**：把 `UnitState.cs:1548` 那句 `RecomputeActAfterAuraKeyword(keyword);` 注掉重编，同一局应回到旧行为；两边指纹**必须不同**，否则说明这一跳没被走到（那就是我在判据上读错了）。
5. ⛔ **别用 `play all dump` 判「对不对」** —— 它看不见 AI 决策列（CLAUDE.md 已有这条），只适合判「有没有意外变化」。

---

## §3 顺手发现（⛔ 我没改，留给调度台分流）

### 3·1 🔴 新差异：`ClearAuraGrants` 的「收回就重算」**比原版严**
- 原版光环收回走 `CardScript__RemoveExtrinsicEffectsFrom.c:44` → `CardScript__RemoveEffect(card, effect, 0)` → `case 3` → `RemoveTraitSilently`。
  `CardScript__RemoveTraitSilently.c` 全文 55 行，`grep -c UpdateSummonSicknessAnims` = **0** ⇒ **原版在光环收回时不重算**，两个 bool 停在旧值（那一回合仍然能动），要到下一次 `ActivateMinion` / 新回合才改写。
- 我们：`UnitState.ClearAuraGrants` 的 `needActRecompute`（`:1575`）+ `EndAuraRecompute` 的 `_auraExemptBefore && !现在是豁免`（`:2123`）会在**豁免真丢**时立刻重算 ⇒ **比原版严**。
- 为什么本批**只记账不改**：改它等于翻转 `W18AR`（2026-10-27）那次已 `play check` 验证过的行为面，而本批禁止跑 `ruleprobe` ⇒ 无法做两态对照。**建议**：由调度台在同步点用「注掉 `:2123` 那一句 / 不注」两态各跑一次 `play check` 再定。
- 落点（将来要改时）：`UnitState.cs:2123`（`EndAuraRecompute` 尾句）。

### 3·2 `CardScript.UsedActiveAbility()` 的名字会误导接线人
它在 `CardScript.cs:12171` 第一句就是 `attacksThisTurn(+0x48) += 1`，是「**这张牌用掉一次行动**」的收尾（主动能力 + 攻击共用），不只是「主动能力」。
⇒ 接 `untilNextAttack` 时，`BattleManager.cs:9431` 与 `ResolveUnitAttacked` 的 `actingCard == this` 支是「**两种行动各一处**」，⛔ 别以为漏了一个、也别以为重复了一个。

### 3·3 `untilNextAttack` 这一档现在有**两套表示**（将来统一 `activeEffects` 表时会踩）
`EffectRecord` 上是 `bool UntilNextAttack`；`TraitEffectRecord` 上是 `TimingBits` 的 bit4。两者今天覆盖的记录**不重叠**（`EffectRecord` 是「调用方投列」，`TraitEffectRecord` 是真列表），所以不冲突；但将来谁做「统一记录表」时必须**两边一起搬**，否则静默丢一半。

### 3·4 账里 `Enchantment.cs` 的一处**既有**漂移（不是本批引入）
`L1042` 的落点写 `Enchantment.cs:384（Matching）`，现读 **384 是 `Has`**、`Matching` 在 **467**。
本批只在类尾追加（账对它的最高引用 1324 未受影响）⇒ 这条漂移在我开工前就在，属于「落点名与行号不符」，`check_ledger_points --fix` 大概率修不好（它会按名字找）。

### 3·5 `EffectResolver` 的「时长 → `TimingBits`」只有三档，缺 `untilNextAttack`
`EffectResolver.TimingBitsOfDuration`（`:16757`）只产出 `""`(0) / `"turn"`(bit0) / `"nextturn"`(bit1)。
⇒ 即使 `EffectBjornShrine` 进了池，也没有任何一层会把 bit4 置上。**接 `L875` 时必须同时补这一层**（属 `EffectResolver`，本批白名单外）。

---

## §4 没查清（如实写，⛔ 没拿猜测填空）

1. **`AbilityTriggersVersioned` 对「附魔卡」是否为真** —— 即 `cards_engine.json` 里附魔卡有没有 `abilityTriggers` 列。
   `Enchantment.cs:883` 的老注释写「附魔卡一般**不在** `cards_engine.json` 里」。若确实不在，`HasEnchantmentForTrigger` 会**总是**走 `-1` 兜底（= 退化成今天的 `Matching` 口径，不静默、但不等价）。
   查法：拿一张附魔卡 id 去 `cards_engine.json` 找 `abilityTriggers`。
   → **本批不阻塞**：池内 0 张附魔 ⇒ 零可观察。
2. **`ReactToUnitJammed.c:122` 那个 `RemoveTrait` 循环作用在哪批 trait 上**（`lVar10` 的来历只读到循环体，没追它的构造）。
   它决定「`RemoveAllAppliedEffectsForJam → ClearAuraGrants` 该不该重算」—— §3·1 那条差异的**另一半判据**。
3. **原版 `SendRemoveEffect` 入队的动作（`battleActionType 0x10`）的结算时机** —— 相对当前动作是「同一拍尾」还是「下一拍头」没读。
   今天无关（0 调用点），接 `untilNextAttack` 时必须补，否则「摘的时机」可能与原版差一拍。

---

## §5 交接（这些谓词/原语现在可以被接走）

### 给 `RuleCore`（`L875` 接线，2 处）
```csharp
// ① 主动能力用掉之后（对应原版 BattleManager.cs:9431 → UsedActiveAbility 尾）
u.ExpireUntilNextAttack();
// ② 攻击结算里「这一记是它自己打出去的」那一支（对应原版 actingCard == this）
u.ExpireUntilNextAttack();
```
- 谓词：`UnitState.IsUntilNextAttack(EffectRecord rec)` · `UnitState.IsUntilNextAttack(int timingBits)`
- 字段：`UnitState.EffectRecord.UntilNextAttack`
- ⚠️ 接之前先补 §3·5（`EffectResolver` 那一层得会产出 bit4），否则 `ExpireUntilNextAttack()` 永远返回 0。

### 给 `RuleCore` / `EffectResolver`（`L1042` 接线，3 处）
```csharp
// 闸① HasWhileInPlayAbility（原版 CardScript.cs:6827）
Enchantments.HasWhileInPlayGate(u) || u.Has("pack")
// 闸② ResolveUnitAttacked 近战支（原版 :14072）
u.Has(KeywordTable.Mob /* "mob" */) || Enchantments.HasMobGate(u)
// 闸③ 伤害结算（原版 :14298）
卡面 rage ∨ u.Has("rage") || Enchantments.HasRageGate(u)
```
- 通用口：`Enchantments.HasEnchantmentForTrigger(u, triggerName)`，四个名字常量已给：
  `Enchantments.TriggerNameWhileInPlay` / `TriggerNameWhileInPlayVariable` / `TriggerNameMob` / `TriggerNameRage`。

### 不需要再接线（`L1376`）
`UnitState.AddAuraKeyword` 自己已重算 —— 光环那一路不需要调用方做任何事。

---

## 附：改动清单（两个文件、四处）

| 文件 | 位置 | 类型 | 说明 |
|---|---|---|---|
| `UnitState.cs` | `:2781` | 就地改写（**+0 行**） | `EffectRecord` 加 `UntilNextAttack` 列，与 `Permanent` 同行 |
| `UnitState.cs` | `:1548` | 就地改写（**+0 行**） | `AddAuraKeyword` 尾部加一句 `RecomputeActAfterAuraKeyword(keyword)` |
| `UnitState.cs` | `:3253-3397` | 类尾追加（**+144 行**） | `L1376` 窄口 + `L875` 到期档（常量 / 两个谓词 / `ExpireUntilNextAttack`）+ 两段判据头注 |
| `Enchantment.cs` | `:1376-1505` | 类尾追加（**+130 行**） | `L1042` 的四个名字常量 + `HasEnchantmentForTrigger`（两个重载）+ 三个 gate + 判据头注 |
