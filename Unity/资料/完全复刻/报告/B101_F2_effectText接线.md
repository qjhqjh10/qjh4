# B101_F2 · `effectText` 链喂到展示层（执行报告）

> 执行代理 `B101_F2` · 2026-10-30 · 第一百零一会话 · 波 2
> 活 = `B101_Q2` 定性里**唯一一条「B · 真没做」**（`报告/B101_Q2_特效卡牌定性.md` §四①）。
> ⛔ 本轮**没跑** Unity / `ruleprobe` / git；**没改**账 / 正本 / 宿主 / `BattleDriver.cs`。

## 〇 一句话结论

原版 `CardEffect.effectText(+0x28)` 那条链**接上了**：投影侧给每条 `ShownEffect` 补「来源卡 + `(ab, lg, isHand)`」四元组，
展示侧在 `RowsOfEffects` 每个分支之前先试 `Card_Abilities/<原文>`（缺键印原文）。
**31 张有 `abilityEffectTexts` 的卡里，26 张的原文行能真的上屏**（30/38 行）；剩下 5 张 / 8 行的原因逐条在 §二，**没有一条是「不想做」**。

---

## 一 改动清单（逐文件 · 改前 / 改后）

### 1. `RuleEngine/Core/EffectsToShow.cs`（4 处，**全部行数零变化或追加在没人引用的行之后**）

| 行 | 改前 | 改后 |
|---|---|---|
| `:30` | `public static class EffectsToShow` | `public static partial class EffectsToShow`（**同一物理行**改写，为了让新实现在文件尾另开一段 —— 主类被 `账/特效动画.tsv:521` 按行号引用 `:193/:201/:478/:502/:559/:578/:599`，往主类体里插方法会整体推走那些落点） |
| `:473` | `SortShown(outp);` | `SortShown(outp); AttachEffectTexts(outp, ctx);`（**同一物理行**追加） |
| `:631-646`（`ShownEffect` 结构体**尾部**，`IsSet` 之后） | —— | 新增 4 格：`CardDef EffectTextCard` · `int EffectAb` · `int EffectLg` · `bool EffectIsHand`（结构体起点 `:599` 不动 ⇒ 账落点不漂） |
| `:648-775`（**文件尾**） | —— | 新建 `namespace RuleEngine { public static partial class EffectsToShow { AttachEffectTexts(...) · BuildSourceCardIndex(...) } }` |

**`AttachEffectTexts` 做什么**（`:693`）：逐条 `ShownEffect`，只要
`Source` 是个**卡名** → `CardDatabase.Load()` 建「卡名 → `CardDef`」索引（同名多张 ⇒ 该名置 `null`，判不了）
→ `CardDatabase.AbilityBuffsOf(def)` 里挑 **`BuffType` 相同**的行 → `def.EffectTextOf(b.Ab, b.Lg, b.IsHandBuff)`
→ 非空就写进那四格（两个候选给出**不同**原文 ⇒ 判不了、不出原文、出声）。
**纯读**：只写自己那条 `ShownEffect`（值类型、就在调用方给的 List 里），不碰 `UnitState` / `CardInstance` / Rng / `ctx.Log`（生产 `ctx` 传 null，与 `Of()` 既有口径一致）。

> 为什么不能「直接读记录上的 `effectText`」：我们的模型里**根本没有这个字段** ——
> `StatMod` / `TraitEffectRecord` / `TempBuff` / `ActiveCardEffect` 都不带（判据 → §六·1）。
> 原版它是**逐条记录**的字段（`AbilityLogic__PlayAbility.c:1189-1205` 从 `abilityData.buffEffect` 克隆时连 `+0x28` 一起拷），
> 我们这套模型丢掉了「这条记录来自哪条 ability」⇒ 只能用**数据面反解**（来源卡名 + `buffType`）。

### 2. `CardPresentation/Battle/CardDisplayWindow.cs`（2 处）

| 行 | 改前 | 改后 |
|---|---|---|
| `:2229` | `var e = items[i];` | `var e = items[i]; if (TryAddEffectTextRow(rows, e)) continue;`（**同一物理行**追加，在 `switch (e.Family)` 之前） |
| `:2650-2686`（**类尾**） | —— | 新方法 `TryAddEffectTextRow`（`:2671`）：`EffectTextOf` 非空 ⇒ 键 `Card_Abilities/<原文>` → `Loc.T` → **缺键（`Loc.T(key) == key`）就印原文** → `CardIcons.RewriteMarks`（= 原版 `ModifyLocalization` 那一跳）→ `TextRow(e.Source, 那段)` 并返回 `true` |

> 位置选在 `switch` **之前**：原版 `GameStaticData.GetEffectDesc.c:8-25` 的 `effectText` 判据
> **排在 `switch(buffType)` 之前**，它**替换的是右列**、与「这条记录属于哪一族」无关
> ⇒ 属性行 / 关键词行 / 附魔行**共用同一条短路**，⛔ 不是给每族各写一份。

---

## 二 31 张的覆盖边界（逐卡 · 现读 `cards_engine.json` v45）

判据 = 这张卡的 `abilityEffectTexts` 行**落在哪个 `buffType` 的 buff 行上**：
只有 **12 / 15 / 16**（附魔族）那一档，`ActiveCardEffect.Source` 才是**真正的卡名**
（`Enchantments.AttachGrantedByPlayedCard` 传 `source: playedInst.Card.Name`）⇒ 才反解得出来源卡。

**完全覆盖（25 张 / 28 行）**：

| 卡 | ET 行 | buffType |
|---|---|---|
| Graceful Avoidance | (0,0,buffEffect) | 15 |
| Cadia Stands | (0,0) | 12 |
| Helspear Assault | (0,1) | 15 |
| Idolatrous Despoilers | (0,0) | 12 |
| Litany of Despair | (0,1) | 15 |
| Wages of Retribution | (0,0) | 12 |
| Hunters of Heretics | (0,1) | 12 |
| Coterie of the Conceited | (0,0) | 12 |
| Pledge to the Dark Prince | (0,0) | 12 |
| Enhanced Aggression | (0,1) | 12 |
| Living Icon | (0,0) + (0,1) | 12 · 12 |
| Master Outrider | (0,1) | 15 |
| Beastboss on Squigosaur | (0,0) | 15 |
| Ferocious Rage | (0,1) | 15 |
| Power of the Waaagh! | (0,0) | 12 |
| Krump da Gitz | (0,0) | 12 |
| ’Uge Choppa | (0,1) | 15 |
| Hallowed Martyrs | (0,0) | 12 |
| Raid Tactics | (0,0) | 15 |
| Experimental Drones | (0,0) | 12 |
| Hive Fleet Leviathan | (0,0)+(0,1)+(0,2) | 12 ×3 |
| Armoured Support | (0,0) | 12 |
| Duty's End | (0,0) | 12 |
| Oath of Moment | (0,1) | 12 |
| Undying Legions | (0,0) | 12 |

> ⚠️ `Living Icon` / `Hive Fleet Leviathan` / `Saviour Protocols` 这些**同卡多行**的，多个候选行给出的原文**相同**
> ⇒ 匹配不会二义（`AttachEffectTexts` 的「两条不同原文 ⇒ 判不了」闸对它们不触发）。

**部分覆盖（1 张 / 5 行里 2 行）**：`Saviour Protocols` —— `(0,0)/(0,1)` 的 `buffEffect`（12）覆盖；
`(0,0)/(0,1)/(0,2)` 的 **`handBuff`（`buffType 2` = `changeCost`）**不覆盖（理由见下「不覆盖 · ①」）。

**不覆盖（5 张 / 8 行）**：

| 卡 | ET 行 | buffType | 为什么没走 |
|---|---|---|---|
| Eve of the Uprising | (0,2,buffEffect) | **2 `changeCost`** | ① **这一格今天根本不成一行** —— `EffectsToShow.Of` 只投影 `addStats`/`addTrait`/`mark`/`removeTrait`/附魔五族，**改费用的记录不是「受到以下影响」表的一行**（`IsNonStatAttr` 把 `cost` 挡在 Stat 族外）。⇒ 不是「原文没取到」，是**那一行还没有生产者**（属账 `特效动画:521` 的既有残差，不在本件） |
| Underground Network | (0,1,handBuff) | **2 `changeCost`** | 同上 ① |
| Immortal Pride | (0,2,handBuff) | **2 `changeCost`** | 同上 ① |
| Bjorn's Shrine | (0,1,buffEffect) | **3 `addTrait`** | ② **归属卡名拿不到**：战术卡那条路 `EffectResolver.ResolveOps:355` 把 `by` 写成常量 `"战术卡"`，`ApplyTraitEffect(source: by)` 记的就是它（`EffectResolver.cs` 的 `addTrait` 正支）⇒ `ShownEffect.Source == "战术卡"` ⇒ 对不上任何卡名 ⇒ **不出原文**（⛔ 不猜） |
| Bjorn the Fell-Handed | (0,0,buffEffect) | **3 `addTrait`** | 同上 ②（单位自己的 while-in-play 光环那条走 `Auras.GrantTag`，同样不是卡名） |

⇒ **合计：26 / 31 张卡有原文行上屏；行级 30 / 38**。
**② 是真残差**（要补得让 `TraitEffectRecord` / `StatMod` 记住「哪条 ability 给的」⇒ 动 `UnitState.cs` + `EffectResolver.cs`，**不在本代理白名单**，已写进 §六·1）。

---

## 三 验收（本代理自跑）

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_b101_f2 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 账落点闸 | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A` | **A 5729 条 · 漂移 0 · 失败 0**（告警 722 条与改前同档；B 档只报告） |
| 行尾 | 二进制计数（`io.open(...,'rb')`） | 两个文件 `CRLF 0 / bareCR 0`；行数 633→775、2649→2686（**全部落在没人引用的行之后**） |

⚠️ **没跑** `BattleScene.Run`（新断言未接宿主 —— 见 §五）与 `ruleprobe`（简报明令）。

---

## 四 判别式（灭自证 · 哪一处改动会让哪一条红）

| 断言 | 怎么才会红 |
|---|---|
| A1..A8（8 张真卡原文上屏） | ① `TryAddEffectTextRow` 第一句改 `return false;`（不短路）⇒ **全红**；② `CardDef.EffectTextOf` 短路成 `return null;` ⇒ `AttachEffectTexts` 拿不到原文 ⇒ **全红**（而 B1/B2 **仍绿** —— 它们断的正是「没有原文时保持原样」，两条**结构上不可能同时满足**） |
| A*-id（右列 ≠ 原文 id） | 键拼错（如 `"Card_Abilitie/"`）⇒ 缺键回原文 id ⇒ 该行只剩 `EffectXxx` ⇒ 红 |
| A*-lit（字面量） | 取错键 / 张冠李戴 ⇒ 那一行不含原版那句话（英/中任一）⇒ 红。期望值**不从被测实现反算**（8 张各写死一句英/中文） |
| B1（无 ET 的卡：附魔行仍是洞） | 把 `AttachEffectTexts` 改成「有 `BuffType` 就套一个」/ 无条件短路 ⇒ 红 |
| **B2（`buffType` 对不上 ⇒ 不猜）** | 把匹配键从 `buffType` 放宽成「只看来源卡有 ET」⇒ `Helspear Assault`（原文在 `(0,1)` 的 `buffType 15` 上，用 `12` 去问）会被套上原文 ⇒ **红** |
| B3（属性行走原合成键） | 让 `TryAddEffectTextRow` 对**所有**行先试一次 ⇒ 属性行绕开 `AddStatRows` ⇒ 红 |

---

## 五 宿主接线（交给调度台的那一行）

`CardPresentation/Editor/BattleScene.cs` 的**超长聚合行**（现读 `:46701`）里，
在 `B101R5_Assert.Run(); pass += B101R5_Assert.Pass; fail += B101R5_Assert.Fail;` **之后**、
`Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");` **之前**，**同一物理行**追加：

```csharp
B101F2_Assert.Run(); pass += B101F2_Assert.Pass; fail += B101F2_Assert.Fail;
```

（照 `B100F9_Assert` 的先例；**同一行追加 ⇒ 行数零变化**。本件不建 GameObject、不用 `CardDisplayWindow` 实例，任何宿主都能跑。）

⚠️ 新建的 `B101F2_Assert.cs` 目前**没有 `.meta`** —— 与本仓其它新 `.cs` 一样，由下一次 Unity 批处理自动生成（`资料/完全复刻/文件所有权_当前.md` 末句的口径），**不算写手**。

---

## 六 没查清 / 边界（如实，⛔ 不拿猜测填空）

1. **「这条记录来自哪条 ability」在我们模型里没有落点**（本件的根）：
   原版 `CardEffect.effectText` 是**逐条记录**的字段，我们把它拆成了 6 本账
   （`_statMods` / `_tempBuffs` / `_traitEffects` / `_auraKw` / `ActiveEffects` / `HandEffects`），
   **每一本都不带 `(ab, lg)`**；`EffectOp.AbilityIndex/LogicIndex` 只在「正文解析出了结构化块」时非 `-1`
   （`EffectText.AbilityIndex` 的 doc：`-1` = 读不到）。
   ⇒ 本件用「**来源卡名 + `buffType`**」在 `abilityBuffs` 列上反解，**这是等价物、不是原版机制**；
   要逐字一致得给记录加 `(ab, lg)` 生产点 ⇒ **`UnitState.cs` + `EffectResolver.cs` + `Enchantment.cs`（本代理白名单外）**。
   池内影响 = §二 的「不覆盖 ②」那 2 张卡。
2. **`Source` 的语义在不同族里不一致**（本件只吃「正好是卡名」的那几档）：
   附魔 = 卡名；限时 `TempBuff.SourceCard` = 卡名；**永久属性 / 永久关键词 = `"战术卡"` 或 `null` 或 `Auras.GrantTag`**。
   ⇒ 后两族今天**结构上**取不到原文（不是本件漏做）。
3. **`changeCost(2)` 那一族没有「受到以下影响」行**：`Eve of the Uprising` / `Underground Network` / `Immortal Pride`
   的原文挂在费用变更上，而我们**不投影费用变更**。原版这一行怎么印（`GetEffectDesc` 的 `case 2`）**本轮没核** ——
   它属账 `特效动画:521` 的既有残差，⛔ 别拿本件当「已闭合」。
4. **`Loc.T` 三层兜底的取值**：`Card_Abilities/*` 全在**官方全表**那一层（手写 `Table` 里没有）。
   本件按「`Loc.T(key) == key` ⇒ 缺键 ⇒ 印原文」判，**没核**原版缺键时到底是印原文还是印空串
   （`GetEffectDesc.c:21-25` 的 `IsNullOrEmpty(GetTranslation)` 之后 `return effectText` **看着是印原文**，
   但 `I2 GetTranslation` 与 `GetTermTranslation` 两个口的缺键行为不同 —— 与 `Card_Trait` 那三条同一族问题，未取证）。
5. **`effectText` 里带 `[[token]]` / `<link>` / `<sprite>` 的渲染**：走 `CardIcons.RewriteMarks`（= `ModifyLocalization` 的等价物）。
   实读 `Card_Abilities/*` 里有 `[[backlash]]` / `[[vulnerable]]` / `[[flank]]` / `<link=sabotage><sprite …>` 四种形状，
   断言 A*-lit 只用**不含记号**的那半句做字面量 —— **记号渲染本身**只有并排比（真机）能验。
6. **同名卡（池内 4 组：`Terminator` / `Terminator Champion` / `Maulerfiend` / `Reconnaissance Mission`）**：
   本件对这种名字**一律判不了**（索引里置 `null`）⇒ 若将来某张同名卡带 `effectText`，它的原文**不会**上屏（会静默不取）。
   现读这 4 组**都没有** `abilityEffectTexts` ⇒ 今天零影响；⛔ 但别把「按名字找卡」当长期方案。
7. **性能**：`AttachEffectTexts` 每次 `Of()` 会 `CardDatabase.Load()`（进程级缓存，返回浅拷贝）并在需要时扫全池建索引。
   开窗外加一次 1129 项的建表，**没实测**在真机上的开销（本代理不跑 Unity）。

---

## 七 顺手发现（只报不改）

1. `EffectsToShow.cs:24` 的文件头写着「本文件属 `Core/` —— **不允许依赖 UnityEngine**」，
   而本件为了拿卡表从 `Core` 调了 `Data/CardDatabase`（`Load()` 内部走 `Resources`）。
   **先例已经在**（`Core/EffectResolverPartial_B97W2.cs:180` 调 `CardDatabase.AbilityBuffsOf`）⇒ 编译无碍，
   但**将来真要给 `Core/` 单独加 asmdef 时这两处会一起断** —— 已如实标在 §六·1。
2. `CardDisplayWindow.cs` 里 `EffectRow` 是**嵌套**在 `CardDisplayWindow` 里的（`:829`），
   而它同时被 `RowsOf` / `RowsOfEffects` / `SetEffectRows` 用 —— 没毛病，但**编辑器程序集里写 `EffectRow` 裸名会 CS0246**
   （本件第一版就撞了一次：必须写 `CardDisplayWindow.EffectRow`）。同类文件以后写断言时注意。
