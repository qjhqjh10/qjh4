# B140_WN — AI 评分参数 2 件（option 5 的 `Exhausted` 闸 · `ValueOfEnchantments` 跳 extrinsic）

> 执行写手 `WN` · 2026-11-04 · 依据 = `报告/B134_审计_AI决策参数.md` §八 #1/#2 + `判据/B140_对账_G6_审计指针.md:129-136`（行号均现读重核）。
> 改动范围：`Assets/RuleEngine/Data/AiScoring.cs`（**零行数变化**，1279 行 → 1279 行，`git diff --numstat` = `10/10`）· 新建 `Assets/RuleEngine/Editor/B140_WN_Assert.cs` · 本报告 · `账/_parts/B140_WN.tsv`。
> ⛔ 未动：`SimpleAI.cs`（不需要）· `play check` 基线 · 账主表 · `RuleEngineTest.cs`（接线归调度台）。

---

## 一、逐条「原版值 vs 我们值」

### #1 option 5 多了一道 `Exhausted` 闸

| | 内容 |
|---|---|
| **原版** | `AI.ScoreFromCriteria` 的 `case 5` 循环体**只有一道** `CardScript__get_mightAct` 闸（`decomp_full/AI__ScoreFromCriteria.c` 的 `:0x18093A934-0x18093A936`），随后两次 `AI.ScoreFromBestAttack`（带 buff / 不带）；`BattleManager__MightAttackBuffedCard.c:10-27` 三闸 = `mightAct` ∧（`近战+Δ≥1` 或 `远程+Δ≥1`）∧ `cardState(+0x228)==2`。 |
| **`mightAct` 到底读什么** | `CardScript__get_mightAct.c:1-30` 全文：① `attacksThisTurn(+0x48) != 0` ⇒ 必须 `bloodThirst(0xdc)` 且 `== 1`；② `IsUnit`；③ `!displaySummonSickness`；④ `!stun(100)` / `!cantAttack(150)` / `!nonCombatant(880)`。**一处都不读 `Exhausted` / `canAct`**。 |
| **我们（改前）** | `AiScoring.cs:546`：`if (u.Exhausted` 或 `u.IsStunned) return 0f;` ⇒ 「本回合已行动、未眩晕」（用过主动技等）的单位 `option 5` 恒取 0。注释原文写「待引擎线裁决」。 |
| **我们（改后）** | `AiScoring.cs:545-546` 两行改为注释（就地改、行数不变），闸只留 `:547` 的 `SimpleAI.MightAttackBuffedCard`；眩晕由 `case 5` 外层的 `AiTargets.MightAct`（`:233`）按 `stun(100)` 挡掉 ⇒ 行为与原版同形。 |
| **影响面** | 池内 `option 5` = **10 条**（`B134` 审计现数）。 |

### #2 `ValueOfEnchantments` 没跳 extrinsic

| | 内容 |
|---|---|
| **原版** | `AI__ValueOfEnchantments.c:25-30`：`if (*(char *)(effect + 0x30) == '\0') { … }` —— `extrinsic != 0` 的 `CardEffect` **整条跳过**；对 `buffType == 1 addStats` 累加 `(meleeAttackChange + rangeAttackChange + healthChange) × 0.5`（不读 `armourChange(+0x58)`）。🥈 交叉：`cpp2il/out_cpp2il_dev_ilspy/AI.cs:2416-2529` 逐字同形（`if (!cardEffect.extrinsic)` → addStats 分支）。 |
| **我们（改前）** | `AiScoring.cs:482-485`：`dm = u.Attack - card.Attack` 等三项 ⇒ 把**光环 / whileInPlay**（extrinsic）的加值、以及 `setMeleeAttack 70 / setRangeAttack 85 / setHealth 9` 的覆盖值也一并算进附魔价值（每 1 点属性多 0.5 分，污染 option 2/3/4/5 以及 `GetValueInPlay`）。 |
| **我们（改后）** | `AiScoring.cs:480-485` 改为扫 `UnitState.StatMods`：只取 `BuffType == 1 ∧ !IsSet ∧ !Extrinsic` 的 `attack` / `ranged` / `health` 行求和 ×0.5。 |
| **extrinsic 的等价字段（映射依据）** | `UnitState.StatMod.Extrinsic`（`Core/UnitState.cs:1849`，doc 明写「原版 `CardEffect.extrinsic(+0x30)`」）。三个生产点：<br>① **数据行** `ApplyAbilityBuff` 的 `case 1 / 70 / 85` 传 `b.Extrinsic != 0`（`Core/EffectResolverPartial_B97W2.cs:235 / 241 / 247 / 449`）；<br>② **光环层** `RecordGrant(…, Auras.GrantTag, …)` → `IsExtrinsicSource(source)`（`Core/Aura.cs:896` + `Core/UnitState.cs:1387-1390`）；<br>③ **作用域量** `ctx.ResolvingExtrinsic`（`Core/EffectResolver.cs:12409/12412/12423`）。 |
| **顺带更近原版的一格** | set 档（70/85/9）在原版**本来就不计分**（不是 `buffType==1`），旧写法经由「运行时 − 卡面」把它们算了进去；新写法一并排除（`B4` 断言单列）。 |

---

## 二、断言（`Assets/RuleEngine/Editor/B140_WN_Assert.cs`，类名 == 文件名、全局命名空间、`-executeMethod B140_WN_Assert.Run` 可直跑）

- **A 组**（option 5）：先断 8 条前提（三个同形单位只差 `Exhausted` / `stun` 两位；`mightAct` 在疲劳单位上为**真**、在眩晕单位上为**假**；`ta=30` 目标表 = 那一个单位），再断：
  - `A1` 对照（未行动）分 > 0；
  - `A2` **疲劳组也 > 0**；
  - `A3` 疲劳组 ≡ 对照组（同一局面、只差 `Exhausted` 一位）；
  - `A4` 眩晕 ⇒ 0；`A5` 不疲劳但眩晕 ⇒ 仍 0。
- **B 组**（ValueOfEnchantments）：先断前提（`extrinsic +2` **真的进了运行时属性** `Attack 2→4`、`Attack 2→6`、`setMeleeAttack` 覆盖成 9），再断：
  - `B1` 只有 extrinsic ⇒ **0**（旧写法 = 1.0）；
  - `B2` extrinsic +2 再叠非 extrinsic +2 ⇒ **1.0**（旧写法 = 2.0）；
  - `B3` 三围非 extrinsic（2/2/4）⇒ **4.0**；
  - `B4` `setMeleeAttack(9)` ⇒ **0**（旧写法 = 3.5）。
- **灭自证**
  - 把 `if (u.Exhausted || u.IsStunned) return 0f;` 加回 `BestAttackScore` ⇒ `A2/A3` 红，`A4/A5` 仍绿（说明删的是 `Exhausted` 那一格、不是把候选表整个放开）。
  - 把 `m.Extrinsic` 的跳过删掉 / 改回「运行时 − 卡面」⇒ `B1`（及 `B2`）红，`B3` 仍绿。
  - 把 `m.IsSet || m.BuffType != 1` 删掉 ⇒ `B4` 红。
- ⚠️ **本轮没跑 Unity**（用户 2026-10-25 起暂停自检、且施工单禁跑）⇒ 断言**只保证 `typecheck 0/0`**，没有实跑读数。

---

## 三、验证

```
TMPDIR=/tmp/wf_b140wn bash d:/4/Unity/工具/typecheck.sh
  --- 运行时程序集 ---   运行时错误数: 0
  --- 编辑器程序集 ---   编辑器错误数: 0        ← 含新建的 B140_WN_Assert.cs

bash d:/4/Unity/工具/ruleprobe.sh play check
  === 13:48:50 结束（rc=0）===                  ← 与改前同一份基线（AI 不进结算层）

bash d:/4/Unity/工具/ruleprobe.sh ai "Nexus of Devotion"   （option 5 读数旁证，rc=0）
  nonTarget[0] option=5 … mightAct=真1/假0 ⇒ 1.2   ← 与改前同值（该夹具没有疲劳单位 ⇒ 本改动在它上面零可观测）
```

- 行尾：`AiScoring.cs` 二进制复核 = `CRLF 0 / LF 1279 / 裸 CR 0`；`git diff --numstat` = `10 / 10`（两处都是**等行数改写**，账户 `AiScoring.cs:711 / :936 / :1146` 等落点不漂）。

---

## 四、没做的部分 / 残差（如实标）

1. **`RuleEngineTest.cs:38628-38630` 那段 doc 已过期** —— 它写「判别器为什么不用已疲劳：`BestAttackScore` 自己第一句就是 `if (u.Exhausted || u.IsStunned) return 0f`」。断言本身用的是 `cantAttack`，**删掉闸之后仍然绿**；但这段理由句要删/改，`RuleEngineTest.cs` 不在本代理白名单 ⇒ 归调度台。
2. **extrinsic 的一个残差**：文字 op（`EffectResolver.DoGive`，`Core/EffectResolver.cs:12409/12412/12423`）那条路传的是 `extrinsic: ctx.ResolvingExtrinsic`，而 `ResolvingExtrinsic` 今天**恒 false**（唯一写点 `RuleCore.ResolveHandBuffOps`，`HandEffect.Extrinsic` 无生产者）⇒ 「靠卡面文本解析出来的 whileInPlay 属性增益」**还没标 extrinsic**，新写法仍会把它们计入。数据行（`ApplyAbilityBuff`）与光环层**都已覆盖**。⇒ 待引擎线补 `ResolvingExtrinsic` 的生产点（本单不动 `RuleEngine/Core/**`）。
3. **`_statMods` 里非 `CardEffect` 来源的行**（虫群合并 `Core/RuleCore.cs:4733-4736`、`doubleAttack` 的等价改写 `EffectResolver.cs:15244-15246`）仍按 `BuffType==1` 计入：`doubleAttack` 原版本来就会被改写成 `addStats`（`.c:198-207`）⇒ 逐字等价；**虫群合并**原版不是 `CardEffect` ⇒ 我们这里仍多算（旧写法也多算，非本单引入）。
4. **AI 随机流 / `PlayTurn` 兼容层 / 结算**：按施工单**不做**（随机流会动 `play check` 基线，属同步点单独裁定）。
