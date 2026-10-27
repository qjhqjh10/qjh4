# B9_S0EFFECT · `GetCardEffectsToShow` 项 · **总闸 S0 两件**（报告）

> 执行代理 `B9_S0EFFECT` · 2026-10-27 · 只做 **S0-①（`ApplyOneGain` 切记录原语 + 限时档 `timingBits`）** 与 **S0-②（`ApplyMarkBody` 按神分行）**。
> 判据 = `资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md` §三 **S0 / S3-a / S3-b** · `资料/完全复刻/报告/B9_R3CEFF.md`。
> ⛔ 本波**不做** S1/S2/S4/S5/S6/S7（投影、Seq、表现侧、断言、探针）。

---

## 一 结论

1. **S0-① 落地**：`ApplyOneGain` 的关键词支**两档都**走 `UnitState.ApplyTraitEffect`（原来只有无限期那一档走，限时档仍是裸 `AddKeyword`）。
   限时档现在**一起传 `timingBits`**（`""→0` · `"turn"→bit0` · `"nextturn"→bit1`）⇒ 限时记录**不会**被并进永久那条。
2. **限时记录的到期清理**（不做就是一本只增不减的记录账）：`UnitState.TempBuff` 新增 **`TraitRec`**（记录引用）+
   类尾新增 **`ExpireTraitEffect(rec, value)`**（按值扣、扣到 ≤0 摘记录，**不碰 `_keywords`**）；
   **三处撤法全部接上** —— `RevertBuffs`（回合到期）· `RemoveBuffsFromCard`（按卡名撤）· `RemoveAllAppliedEffectsForJam`（jam 整份收）。
3. **S0-② 落地**：`ApplyMarkBody` 的 `markofchaos` 从裸 `AddKeyword(…, 1)` 改成
   `ApplyTraitEffect("markofchaos", 1, markOfChaosType: GodOf(k))`，四神映射 `blood 10 / resilience 20 / excess 30 / fate 40`
   按简报照抄。`darkpact` 那一半（我们自己的层数代理）**保留**。
4. **净行为不变**（这一条有闸有证）：`ruleprobe check` **rc=0**（解析差异 0 行 / 哨兵 0 条）·
   `ruleprobe play check` **rc=0**（①-a 主档 + ①-b 电池逐卡全等、②基线哨兵过）⇒ **一个指纹都没动**。
   机制上可解释：`markofchaos` 是 `count(20)` 档、`AddKeyword` 内部就把值夹成 +1，
   而 `ApplyTraitEffect` 的 `keywordDelta` 对 `add(10)` 档恒等于 `val` ⇒ 两条路的 `_keywords` 增量**逐字相同**。
5. **灭自证**（闸绿不足以证明「改动真的生效」）：在仓库外写了一个 13 条的夹具（`/tmp/wf_s0h`，只引用 `wfprobe.dll`），
   **全过**。其中 4 条是**真正带鉴别力**的（把 `timingBits` 改回 0 / 把 `markOfChaosType` 改回 0 就会红）：
   - `Give Vanguard this turn` ⇒ 记录 `vanguard=1 bits=1`，且 `TempBuff.TraitRec` **是同一个对象**；`RevertBuffs` 后记录一并消失、`KwValue` 归 0；
   - 永久 `Give Vanguard` + 限时 `Give Vanguard this turn` ⇒ **2 条**（值 2），**不是**并成 1 条（并了就会变成 1 条值 2 —— 条数可判）；
   - `until your next turn` ⇒ `bits=2`（与 this-turn 的 `bits=1` 分开）；
   - `GrantDarkPact(blood)` + `GrantDarkPact(resilience)` ⇒ **2 条 mark 记录（god=10 / god=20）**，`markofchaos=2 · darkpact=2`。
   两条「撤法」也单独验了：`RemoveBuffsFromCard` / `RemoveAllAppliedEffectsForJam` 之后记录同样归零。
6. **可达性**（避免「改了但池内到不了」）：池内带「give + 时长」的卡 **85 张**（`cards_engine.json` 现读），
   其中关键词型的典型 = `ASH70`（`Give Vanguard … until your next turn`）· `ASH47`（`Give Shuriken 2 … this turn`）·
   `BL66`（`Give Vulnerable 1 to all enemies this turn`）⇒ **限时那一档真能走到**。

---

## 二 证据（文件:行号）

**判据（原版）**
- `decomp_full/CardScript__AddEffect.c:469-568`（`case 3` addTrait 的合并循环：合并键含 `markOfChaosType(+0x78)` / `daemonHostType(+0x7c)` / `SameTimingAsEffect` / 两边 `extrinsic(+0x30)==0`；`count` 档新建强置 1 `:496`、合并 `+1` `:554`；`add` 档 `+=` `:544`）
- `decomp_full/CardScript__AddEffect.c:776-792`（`case 8` markOfChaos：`GetMarkOfChaos(+0x78)` → `TriggerEnchantment` → `lastMarkOfChaos` → 广播）
- `decomp_full/EntityScript__GetCardEffectsToShow.c:47-130`（mark 按四神聚合、`traitValue` 累加）+ `EntityScript__CreateMarkCardEffect.c:1-21`（`buffType=8` + `markOfChaosType=god`）
- 施工细则 `资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md` §三 **S0**（前置）、**S3-a**、**S3-b**（四神映射「三重已证」）

**我方代码（改后现读）**
- `RuleEngine/Core/EffectResolver.cs`（**类名是 `RuleCore`，文件名≠类名** —— 见 §五·1）
  - `:11841` `UnitState.TraitEffectRecord appliedTrait = null;`（本次新增的局部，限时档要用它建链）
  - `:11877-11885` 关键词支：`if (val > 0) added = (appliedTrait = u.ApplyTraitEffect(… timingBits: TimingBitsOfDuration(duration))) != null;`
  - `:12118-12120` `TempBuff{ …, Mod = appliedMod, TraitRec = appliedTrait }`
  - `:12391-12394` `ApplyMarkBody`：`u.ApplyTraitEffect(KeywordTable.MarkOfChaos, 1, buffType: BuffTypeAddTrait, stackType: -1, source: by, extrinsic: false, markOfChaosType: MarkOfChaosTypeOf(k));` · `:12395` `u.AddKeyword(KeywordTable.DarkPact, 1);` **保留**
  - `:16610` `TimingBitsOfDuration(string)` · `:16628` `MarkOfChaosTypeOf(string)`（**追加在类尾**，避免下游行号漂移）
- `RuleEngine/Core/UnitState.cs`
  - `:1670` `TempBuff.TraitRec`（字段，追加在 `TempBuff` 类尾）
  - `RevertBuffs` `:1704` / `RemoveBuffsFromCard` `:1761` / `RemoveAllAppliedEffectsForJam` `:2239` 三处关键词支改成
    `{ RemoveKeyword(b.Name, b.Value); ExpireTraitEffect(b.TraitRec, b.Value); }`
  - `:3154` 类尾新增 `public void ExpireTraitEffect(TraitEffectRecord rec, int value)`（`count` 档扣 1，其余按 `value` 扣；≤0 摘记录）
- 被复用的既有实现：`UnitState.ApplyTraitEffect`（`:2998` 起）· `FindTraitEffect`（`:3059`，合并键**本来就有** `MarkOfChaosType`）· `UnitState.TraitEffectRecord`（`:2916`）

**闸（读数带时间与前提，别当成「此刻可复现」）**
- `bash 工具/typecheck.sh`（`TMPDIR=/tmp/wf_s0`）⇒ **运行时 0 / 编辑器 0**，两次独立复现（撞到别人半成品的两次不算，见 §六·2）
- `bash 工具/ruleprobe.sh check` ⇒ `解析差异 0 行 · 哨兵合计：失败 0 条` · **rc=0**（`18:23:03`）
- `bash 工具/ruleprobe.sh play check` ⇒ ①-a ①-b 逐卡全等 · ② 基线哨兵 ✓ · **rc=0**（`18:23:25`）
- 🔴 **此刻这两个 ruleprobe 读数已不可复现** —— 不是我的改动，是 `RuleEngine/Core/GameplayVariables.cs` 被别的写手引入了
  `UnityEngine.Vector3/Vector2`、离屏桩没有这两个类型 ⇒ 探针**编不过**（详见 §六·2）。
  我这两条绿灯取自它坏掉之前的构建，且那份 `wfprobe.dll` **确实含我的改动**（仓库外夹具从同一份 dll 读到了只有本次改动才会产生的 `bits=1` 记录）。
  **建议调度台在同步点重跑一次 `ruleprobe check / play check` 再写进定案读数。**

---

## 三 指纹影响表（`play check` **没有变红**）

| 项 | 读数 | 判「真行为变化」还是「等价重构」 |
|---|---|---|
| `play check` ①-a 主档逐卡 | **全等**（rc=0） | 等价重构 —— `_keywords` 增量与改前**逐字相同**（见 §一·4 的机制解释） |
| `play check` ①-b 事件电池 | **全等** | 同上（`markofchaos` 走 `count` 档，`AddKeyword` 与 `ApplyTraitEffect` 都给 +1） |
| `play check` ② 基线文件内容 vs 源码常量 | ✓ `0766fbe1…` 一致 | 我没有、也不允许改基线/ `PinnedPlayMd5`（红线） |
| `check` 解析差异 / 哨兵 | 0 行 / 0 条 | 本次改动**不在解析层** |

> **为什么「没变红」是可信的、不是我改漏了**：记录账（`_traitEffects`）今天**没有任何结算读点**
> （`grep` 全仓：`TraitEffects` / `TraitEffectValue` 只在本文件里定义，**零消费者**）⇒ 新增的记录**结构上不可能**改结算。
> 而「改动确实生效」由 §一·5 的仓库外夹具正面证明（不是靠闸绿推断）。
> ⚠️ 顺带一条判据边界：**闸绿 ≠ 记录对**。「记录对」由夹具的 4 条鉴别性断言兜住。

---

## 四 改动清单

| # | 文件 | 改动 | 行数变化 |
|---|---|---|---|
| 1 | `Assets/RuleEngine/Core/EffectResolver.cs` | 关键词支：`val > 0` 两档统一走 `ApplyTraitEffect`；新增局部 `appliedTrait`；`TempBuff` 填 `TraitRec`；就地改写「只切无限期那一档」那段过期注释（铁律 5） | **+20**（`:11858` 之后整体 +20） |
| 2 | 同上 | `ApplyMarkBody`：`addTrait(330)` 切记录原语 + 按神分行（`MarkOfChaosTypeOf`）；`darkpact` 计数保留 | 含在上面 +20 里 |
| 3 | 同上 | 类尾追加 `TimingBitsOfDuration` / `MarkOfChaosTypeOf`（两个小件，**类尾**，不影响既有行号） | +52（文件尾） |
| 4 | `Assets/RuleEngine/Core/UnitState.cs` | `TempBuff.TraitRec` 字段 + 三处关键词撤法接 `ExpireTraitEffect` + 类尾 `ExpireTraitEffect` | **+11 ~ +14**（按位置；`:1640` 之后 +11、`:2240` 之后 +14） |
| 5 | 报告（本文件）· 账补丁 `账/_parts/B9_S0EFFECT.tsv` | 见 §六 | —— |

**账漂移（给调度台）**：`check_ledger_points.py`（**只读**跑了一次）现读 **A 3,756 · 漂移 305 · 失败 95**；
其中**因本次两个文件插行造成的 = 34 条**（`EffectResolver.cs` 的落点**整段 +20**、`UnitState.cs` 的按位置 +11/+13/+14），
**全部是「漂移」（符号名仍能唯一命中）**，没有一条变成「失败」 ⇒ 同步点 `check_ledger_points --all-A --fix` 可一次收回。
⚠️ 我**没有**跑 `--fix`（红线：重钉只有调度台能做）。

---

## 五 没查清的部分（如实）

1. **`duration == "nextturn"` 到底对应原版哪一位**（`+0x33 untilPlayerTurnStart` 还是 `+0x35 untilFollowingPlayerTurnStart`）
   **没核到逐句**：施工细则 §六 已把它列为缺口，本轮沿用「取 bit1」。这**只影响记录侧的合并键口径**
   （同一档要能并、不同档不能并），**不影响关键词数值**。要真钉死，得读 `CardEffect__SameTimingAsEffect.c` 的逐位比较 + 解析层是否该拆成两档（**还差**：解析层要不要把 `nextturn` 拆成两档的裁定）。
2. **`ApplyTraitEffect` 的一个既有缺陷（不是本次引入，也没有修）**：它在**合并分支里先改 `rec.TraitValue`、再调 `AddKeyword`**，
   而 `AddKeyword` 可能拒收（毒 × 抗性）⇒ 那一次会**凭空给记录 +1**。
   池内 **0 入口**（`poisoned(200)` / `resistant(600)` 的 `buffType` 条目一条都没有，`cards_truth.json` 现读），
   且**永久档从上一批起就已经是这个形状** ⇒ 本轮只记不修（改它要动 `ApplyTraitEffect` 方法体中部、会带来新的账漂移）。
   ⚠️ **要修的话落点是 `UnitState.cs` 的 `ApplyTraitEffect`（合并分支）**，不是本次新增的任何一处。
3. **`RemoveAll(poisoned)` 不清记录**（同族：`AddKeyword` 加抗性时会 `RemoveAll(Poisoned)`）⇒ 那一格记录会留一条「影子」。
   同上，池内 0 入口。
4. **`Auras.Recompose` 的隔离实验没有做，因为本次没有新增任何 Recompose 交互**：
   光环给关键词走 `Auras.AddAuraKeyword` / `RecordGrant`，**不经过 `ApplyTraitEffect`** ⇒ 不产生记录、也就不会被重算重折。
   `fast`/`flank` 经 `ApplyTraitEffect → AddKeyword` 触发的 `RuleCore.RecomputeActAndAttack` 在**改前就存在**（裸 `AddKeyword` 同样触发）⇒ **零新增**。
   ⚠️ 如实标：**我没有做 ON/OFF 隔离实验**（那需要跑 `play all dump` 前后比，`play all dump` 属调度台口）—— 这次不需要，理由就是上面两条。
5. **`play all dump` 的前后逐字节对比没做**：`play check` 已经 rc=0（= 逐卡全等），比「只看变化」更强；
   若调度台仍要逐列归因，那是 `play all dump` 的事，本轮**没有**跑（也不在验收三件里）。
6. **`RuleCore.ResolveOps` 的端到端夹具只覆盖了 `give <keyword> [duration]` 这一形状**：
   `give +N Melee Attack this turn` 那种**属性**形状走的是 `case "attack"` 那一路（不进记录账、原版是 `addStats` 记录 —— 属 S3 之外的族），夹具里**没有**覆盖。

---

## 六 顺手发现

1. 🔴 **`EffectResolver.cs` 里的类名不是 `EffectResolver`，而是 `RuleCore`（partial）** —— 文件 `:25-27` 自证
   （文件里 `:8219` 自己还写着「全仓**没有**叫 `EffectResolver` 的类」）。
   我第一次写仓库外夹具时按文件名找类，直接 `CS0103`。**判据/改代码时一律按 `RuleCore.` 找**，
   别按文件名 grep 类成员（本项目已知「文件名≠类名」那一族）。
2. 🔴 **`RuleEngine/Core/GameplayVariables.cs` 被别的写手引入了 `UnityEngine.Vector3/Vector2`**（`:648-682` 的 `PlayedCardDisplayPosition`），
   **Unity 侧编得过（typecheck 0/0），但 `ruleprobe` 编不过**（离屏桩只提供 `Debug/Random/Resources/JsonUtility/TextAsset`）：
   ```
   GameplayVariables.cs(651,25): error CS0234: 命名空间"UnityEngine"中不存在类型或命名空间名"Vector3"
   ```
   ⇒ **全批次所有人的 `ruleprobe check / play` 读数此刻都不可用**（我 5 分钟内重试 3 次，一直是这个错）。
   ⚠️ 我的绿灯读数取自**它坏掉之前**的那次构建（`18:23:03` / `18:23:25`，且那份 `wfprobe.dll` 里确实含我的改动 ——
   仓库外夹具读到了只有我这次改动才会产生的 `bits=1` 记录，可反证）。
   **处置建议（调度台）**：要么让那个写手改用桩里已有的类型/加值类型桩（`Stub.cs` 是 `工具/ruleprobe/` 的白名单文件），
   要么在同步点重跑一次 `ruleprobe check` 才写进定案读数。
3. **`GetCardEffectsToShow` 这条主线在账里 `EntityScript` 段没有行**（现读：`战斗逻辑驱动.tsv` 只有
   `GetCurrentTraitValue*` / `HasActiveEffect` / `HasCurrentTrait*` 等，**没有 `GetCardEffectsToShow` / `CreateMarkCardEffect`**）。
   ⇒ 已在本波补丁里按 **C（未实现）** 记两条（见 `账/_parts/B9_S0EFFECT.tsv`），
   ⛔ 别把「账里没有」当成「原版没有这条链」。
4. **`ApplyOneGain` 之外还有两处裸 `AddKeyword` 会挂 trait 而没有记录**（本波**不属**我，属 S3-f）：
   `EffectResolver.cs` 的 `give <关键词>: <正文>` 那条路（`GrantEmbeddedOps` 一带，`u.AddKeyword(norm, 1)` + `AddTempBuff`）
   · `RuleCore.cs:7251`（`stun`）/ `:8600`/`:8659`（`droppod`）。
   ⇒ 投影落地后，**这三处挂的 trait 不会出现在「受到以下影响」**（与我们自己的 `RecordGrant` 光环一样是「无记录」族）。
   ⚠️ `droppod` 是我们自己的表示（原版无对应），按铁律 18 应判「不建记录」；`stun` 按施工细则 §三 S3-f「应走 `ApplyTraitEffect`」。
5. **`TempBuff` 现在有两个「外挂引用」字段**（`Mod` / `TraitRec`），它们的生命周期规则**相同**（到期 = 从各自账上摘那一格）。
   本轮把三处撤法都补齐了；**将来若再加第四处「撤限时增益」的入口**，`ExpireTraitEffect` 必须一起调（漏了静默）。
   已在 `TempBuff.TraitRec` 与 `ExpireTraitEffect` 的 doc 里写死。

---

## 七 交接给下一个代理（S1 / S2 / S4 的接手点）

- **可以直接依赖的东西**：`u.TraitEffects` 现在**真有内容**了 —— 永久 `buffType=3/90` × 限时（`TimingBits != 0`）× mark（按神分行）。
  `TraitEffectRecord` 各列（`Trait / BuffType / TraitValue / MarkOfChaosType / TimingBits / Source`）都已被真路径写过一遍。
- **S1 投影**：`Timed` 请读 **`(r.TimingBits & 0x1F) != 0`**（别自己再判时长）；**mark 分组读 `MarkOfChaosType`**（`0` 跳过）。
- **S2（`Seq`）**：`TraitEffectRecord` 加 `Seq` 的位置就在类尾（`Source` 后面），加了以后 `ApplyTraitEffect` 建记录那一刻写戳；
  ⚠️ `ExpireTraitEffect` 摘记录时**不需要**动戳（记录本来就要没）。
- **S4 表现**：本波**没动** `CardDisplayWindow` / `BattleDriver`（项源仍是 `RowsOf(TempBuff)`）。
- **本波已知的「不完整」**：`setHealth(9)` / `setCurrentHealth(100)` 仍不留记录；`RemovedTraits` 仍是 `HashSet`；手牌族仍未接 —— 三件都在施工细则 §六。
