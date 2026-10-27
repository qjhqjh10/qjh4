# B88_F4 — `RuleEngineTest.Run` 3 条 Pack 红修复

- 日期：2026-10-28 · 代理：F4（重同步点修复）
- 结论：**判定 = (α) 断言过期**（实现是对的，断言拿粉丝规则书当判据）⇒ 改断言，未改实现
- 复跑：`RE === 结束：9458/9458 全过 ✅ ===` · 行首 `✗` = **0** · `typecheck` 运行时 0 / 编辑器 0

---

## 一、红是什么（改前）

```
RE    ✗ 场上 1 个友方部队 → 近战 2+1=3 —— 期望 [3]，实得 [2]
RE    ✗ 3 个友方部队 → 2+3=5 —— 期望 [5]，实得 [4]
RE    ✗ 远程也 +3（基础 0） —— 期望 [3]，实得 [2]
```

被测 = `RuleEngineTest.cs` 的「② 兽群」（`TestBattleKeywords`），走 `RuleCore.FieldAttack` 的 `u.Has("pack")` 分支。

## 二、判定 (α)：断言过期，实现（B88-E1 那笔）是对的

### 判据链（全部是原版二进制，两份反编译互证）

| # | 判据 | 内容 |
|---|---|---|
| ① | `decomp_full/CardScript__CheckAndApplyCurrentPack.c:15-38` | `EntityScript__get_cardType()`：**`== 10` ⇒ `*(unitList + 0x18)`（= `Count`）；否则 ⇒ `Count + -1`** |
| ② | `decomp_full/CardScript__UpdateWhileInPlay.c:36-52` | 同一段逻辑的**内联体**，逐字同形（`iVar2 == 10` ⇒ `*(lVar4+0x18)`；否则 `+ -1`） |
| ③ | `out_cpp2il_dev_ilspy/CardScript.cs:16078-16118`（`CheckAndApplyCurrentPack`） | `cardTypeOptions == CardTypeOptions.Hero` ⇒ `unitList._size`；否则 `unitList._size - 1`；再把 `num` 同时写进 **`meleeAttackChange`（`:16107`）与 `rangeAttackChange`（`:16108`）** ⇒ 近战/远程同一个数 |
| ④ | `out_cpp2il_dev_ilspy/CardTypeOptions.cs:1-7` | `Minion = 0` · **`Hero = 10`** · `Tactic = 20` · `Whispers = 40` ⇒ ①的「10」= **督军**，其余 = 部队 |
| ⑤ | `decomp_full/MinionManager__GetUnitList.c`（ILSpy `MinionManager.cs:3397-3497`，`.c` 里是 `+0x60`/`+0x68` 两条 List） | 内容 = **`leftMinions + rightMinions`**，逐条过 `CardScript.IsInPlay` ⇒ **督军不在里面、源自己【在】里面** |
| ⑥ | `decomp_full/MinionManager__InsertMinion.c`（ILSpy `MinionManager.cs:673-745`） | 唯一写这两条 List 的入口，只由 `PlayMinionFromHand` / `SummonMinion` / `MoveMinionIntoSlot` 调用 ⇒ 督军不从手牌打出 ⇒ **永远不进这两条 List** |
| ⑦ | `decomp_full/CardScript__IsInPlay.c:1-16` | in-play 态 = 2 / 15 / 3 / 17 —— 带 Pack 的源自己当然在这个集合里 |

⇒ **原版部队的 Pack = `GetUnitList().Count - 1` = 「其他友方部队」的个数（自己不算）**；
带 Pack 的**督军**才取 `Count`（= 全部友方部队；督军本来就不在 List 里，所以「不减 1」不是「算自己」）。

两者在「**每个其他友方部队**」这一读法下**正好自洽**（督军不是部队 ⇒ 不必减 1），这本身反证了⑤⑥的读法正确；
反过来若照旧断言（含自己），督军分支就会得到「部队数 + 自己」这种没人写过的语义。

### 为什么改前是绿的、这波变红

实现侧这笔 = `RuleCore.cs:5034`（**2026-10-27 `B88-E1`**）新加的 `if (!u.IsWarlord) n--;` —— 那是**正确的复刻修正**
（改前对带 pack 的部队**多算 1**，池内 10 张带 pack）。断言侧仍停在旧口径（出处 = **粉丝实体规则书** `Warbook:195`
「场上每有 1 个友方部队 +1 近战 +1 远程」＋ 我们上一版 Godot `rule_core.gd:4172` —— 那份规则书自己第 5 行写着
`Not official. Fan project.`，按 `CLAUDE.md` 铁律 2 只能当**旁证**）。

⇒ 本波把**正确实现**落地后，旧断言才露出来。**不是实现回归**：实得 `[2]/[4]/[2]` 与 `.c` 逐位吻合。

## 三、改了什么

只改 `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的「② 兽群」一段（`git diff --numstat` = **29 增 / 5 删**，
CRLF 38849 / LF 38849 / 裸 CR 0 —— 行尾未被翻）：

1. `Check(..., 3, "…近战 2+1=3")` → **`2`**，消息改成「只有自己 1 个部队 → 近战 2+0=2（原版部队取 Count−1、排掉自己）」
2. `Check(..., 5, "3 个友方部队 → 2+3=5")` → **`4`**，「3 个部队（自己 + 2 个其他）→ 近战 2+2=4」
3. `Check(..., 3, "远程也 +3")` → **`2`**，「远程同样 +2（基础 0）」
4. 整段头部加一段**带判据出处的订正注释**（①～⑦ 的路径行号 + 「⛔ 别改回 `[3]/[5]/[3]`」）
5. 顺手补**督军分支**覆盖（原来只有一句注释说「督军自己带 Pack 时…」，但断言测的是**不带** Pack）：
   `wl.AddKeyword("pack", 1)` 后断 `FieldAttack(近战) = wl.Attack + 3`、`(远程) = wl.RangedAttack + 3`
   —— 这条把 `RuleCore.cs:5034` 的 `if (!u.IsWarlord)` 那一半也钉住了。
6. 断言文案 `"督军不参与计数（原版过滤条件）"` → `"督军默认不带 Pack"`（原文案把「督军不进 List」写成了「不参与计数」，易误导）

**未改**：任何 `.cs` 实现文件。`RuleCore.cs` 全程只读（在本次黑名单内）。

## 四、复跑读数

```
RuleEngineTest.Run → RE === 结束：9458/9458 全过 ✅ ===
行首 "^RE *✗" 计数 = 0
typecheck.sh（TMPDIR=/tmp/wf_s88f4）：运行时错误数 0 · 编辑器错误数 0
```
（总数 9456 → 9458 = 新增的 2 条督军分支断言）

日志：`d:/4/_tmp_view/ruleengine2.log`（结束行 `:273308`；兽群段 `:148451`–`:148550`）

## 五、顺手发现（未做，留给调度台分流）

1. **`RuleCore.cs:4999-5008` 的 `FieldAttack` doc 注释已与实现相反**：那里写「`rule_core.gd:4172` … **包含自己**」，
   而现在实现是**排自己**。该注释描述的是 Godot 旁证文件，但读者会当成规格 ⇒ 建议按铁律 5 就地订正
   （本轮 `RuleCore.cs` 在黑名单，没动）。
2. **`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md:195`** 的「兽群（Pack）」词条照原样留着
   （「场上每有 1 个友方部队 +1 近战 +1 远程」）—— 它是粉丝实体版，与数字版差在「**自己算不算**」。
   建议在该行加一句「⚠️ 数字版：部队排自己（`CardScript__CheckAndApplyCurrentPack.c`），督军不减 1」，
   否则下个会话还会照它写断言。
3. `MinionManager.GetUnitList` 在**全量反编译里只有 2 个调用点**（`CheckAndApplyCurrentPack` + `UpdateWhileInPlay`）
   ⇒ 这方法**只为 Pack 而存在**，是「Pack 计数口径只有一处」的强证据，值得写进判据（避免以后有人另找一处）。
