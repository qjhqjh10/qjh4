# B124_W11 · `buffType 25 ephemeralCopy` 去重闸 + `silent` 档

判据 = `判据/B124_W20c_RemoveTrait可达性.md` §三（原版 `CardScript__AddTraitSilently.c`：先 `Enumerable.Any` ⇒ 命中即 `return`）。
`RuleCore.cs:16192` 的 `RemoveAll` **未动**（判据已证：trait 列表里最多一条 ⇒ `RemoveAll` ≡ `Find+Remove`）。

## 一 改动（两个文件，引擎侧**零行数变化**）

### 1. `RuleEngine/Core/EffectResolverPartial_B97W2.cs:385-386`（`case 25`）

改前（`:385-386`）：块开 `{` 单独一行，下一行 `bool added = target.AddKeyword("ephemeral", 1);`

改后：

- `:385`（块开括号，该行无 `//`）行末追加去重闸：

```
{ if (target.Has("ephemeral")) { UnityEngine.Debug.LogWarning($"{who}：「{target.Name}」的 `ephemeralCopy(25)` **已带 `ephemeral` ⇒ 去重**（原版 `CardScript__AddTraitSilently.c`：`Any` 命中即 return；⛔ 不是 `AddKeyword` 的同键累加）"); return false; }
```

- `:386` 换成 `bool added = target.AddKeyword("ephemeral", 1, silent: true);` + 行末注释（代码在注释**之前**）。
- 语义：去重 = 原版 `Any ⇒ return`；`silent: true` = 原版 `AddTraitSilently` 的「不置图标脏」（`UnitState.AddKeyword` 头注 / 账 `D118-80`）。
- 出声口 = **`UnityEngine.Debug.LogWarning`**（本仓既定：诊断出声不进 `ctx.Log`，见 `EffectResolver.cs:1830` / `:8404`）⇒ **不碰 `ctx.Events` ⇒ 不动 `Fingerprint`**。
- 行数守恒 **773 → 773**，CRLF 0 / 裸 CR 0（改前改后一致）。

### 2. `CardPresentation/Editor/B97W2_Assert.cs:183` ＋ `:188-198`（C1 块内，**+12 行**，305 → 317）

- `:183` `int rev0 = u.TraitRev;`（第一次调用**之前**取基线）。
- `:191-195` 第二次 `ApplyAbilityBuff(BuffType=25)` ⇒ `Check(again, false)` ＋ `Check(u.KwValue("ephemeral"), 1)`（⛔ 不是 2），附 3 行灭自证注释。
- `:196-198` `Check(u.TraitRev, rev0)` —— 两次调用后**不动**。
  ⚠️ 简报写「只 +1」，但 `silent: true` 下第一次调用就是 **+0**（`UnitState.cs:1195` `if (!silent) TraitRev++`），第二次被闸拒收也是 +0 ⇒ 正确期望值是 `rev0`，写 `+1` 会假红。
- `:42` 文件头「灭自证」清单那条同句补上「漏 `silent: true` ⇒ `TraitRev` 那条红」（同一行就地改）。

## 二 typecheck

- 第一遍（加去重闸后）`TMPDIR=/tmp/wf_b124w11` ⇒ 运行时 0 / 编辑器 0。
- 第二遍（加 `silent` 与 `TraitRev` 断言后）`TMPDIR=/tmp/wf_b124w11b` ⇒ **运行时 0 / 编辑器 0**。

## 三 灭自证

- 删去重闸 ⇒ 第二次回 `true` 且值累加成 2 ⇒ `again` / `KwValue` 两句同时红。
- 删 `silent: true` ⇒ 第一次调用 `TraitRev` 就变 `rev0+1` ⇒ 第三条红。三条**红在行为值上**，不是文案。
- 「值 == 1 / `TraitRev` 不动」为什么排除别的路径：同一个 `u` 只被本块两次调用碰过；棋盘只它一个单位、无光环/附魔；卡面 `ephemeral` 由前置**硬**断言 `Check(..., false)` 排除（不是 `if` 跳过）；全仓 `case 25` 生产调用点 = 0（判据 §二）；且去重/静默两条路都不写 `ctx.Log`。

## 四 顺手发现（**未改**）

1. 账 `战斗逻辑驱动.tsv` 行 853 落点栏写 `EffectResolverPartial_B97W2.cs:411-332`（反向区间，`332 < 411`），实际 `case 45` 块现读 `:409-423` —— 账面笔误，本代理未动账。
2. `B120_W20_Assert` 已覆盖 `AddKeyword` 的 `silent` 语义，本件的 `TraitRev` 断言与它同口径、不重复（前者测原语、这里测 `case 25` 的**用法**）。
