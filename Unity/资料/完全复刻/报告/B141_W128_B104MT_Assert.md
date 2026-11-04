# B141 · W128 · `B104_MT_Assert` A10-a/b 陈旧断言改写（`targetSelectMethod 2/3`）

- 批次 `B141` · 会话 128 · 写手 `B141-W12`
- 独占文件：`Unity/MyGame/Assets/CardPresentation/Editor/B104_MT_Assert.cs`（**注意路径**：简报里写的是
  `RuleEngine/Editor/B104_MT_Assert.cs`，**现读不存在** —— 真身在 `CardPresentation/Editor/`，
  同目录树上唯一一份，行号与简报 `:281-296` 对得上）
- 判据：`资料/完全复刻/判据/B141_W128_对账_引擎RuleCore.md` §2（SAN-27 残余 ①）＋ §5 顺手发现 #1
- 现读实现：`RuleEngine/Core/RuleCore.cs:18102-18108`（`case 2/3` → `KeepByValueInPlay`）· `:18517`（键 = `SimpleAI.ValueInPlay`）
- 未做：未跑 Unity · 未动 git · 未碰 `d:/2` · 未动账/活状态表/所有权表/`RuleCore.cs`

---

## 一、为什么旧断言必红

| 项 | 旧断言（改前） | 现实现（现读） |
|---|---|---|
| `targetSelectMethod = 2` | `⇒ null`（`B104_MT_Assert.cs:285-288` 改前） | `RuleCore.cs:18102-18108` → `KeepByValueInPlay(pool, 2, max)` = `OrderByDescending(ValueInPlay)` 后取前 `max` |
| `targetSelectMethod = 3` | `⇒ null`（`:289-290` 改前） | 同上，`OrderBy(ValueInPlay)` 升序 |
| 旧理由原文 | 「要 `CardScript.GetValueInPlay()`，本版**没有**等价字段」 | 等价物 = `SimpleAI.ValueInPlay`（`RuleCore.cs:18501-18503` 的 doc）；`B140_WA` 2026-11-04 已落 |

⇒ 旧 `A10-a/A10-b` 每条都会返回 1 张（不是 `null`）⇒ **两条红**（`RuleEngineTest.cs:1294` 已接线）。

键链判据（🥇）= `out_il2csharp/Assembly-CSharp/AbilityLogic.cs` 的 `switch (targetSelectMethod - 1)`：
`case 2` ⇒ `OrderByDescending<CardScript,float>(pool, __9__26_3)`、`case 3` ⇒ `OrderBy<...>(__9__26_4)`，
两个闭包体都是 `unit.GetValueInPlay()`，非 `default` 六档末尾统一 `Take(maxTargets)`。
（⛔ `decomp_full` 的 C 伪码会把返回值吞掉，判不出「筛还是排」—— 这条已写进 `RuleCore.cs:18499`。）

---

## 二、改成什么（保留原意图：验证多目标候选的**排序 + 截断**）

### 2.0 新夹具（为什么不能再用 `MakeCtx`）

旧 `A10` 复用 `MakeCtx()` 的 4 个单位**费用全一样**（`PlainUnit` 恒 `cost = 1`、`Hero` 恒 `0`），
按价值排与按池序取前 N **分不开**。新夹具 `CostedUnit` / `CostedHero`（`B104_MT_Assert.cs:63-67`）
让费用可控：`SimpleAI.ValueInPlay = 2 × 卡面费 + 1`（`Data/SimpleAI.cs:786-800`；满血 · 无附魔 · 未眩晕时只走那一句）。

| 槽位 | 卡 | 卡面费 | `ValueInPlay` | 池序（`AddSide` 按槽号升序） |
|---|---|---|---|---|
| 2 | `CostedUnit` | 4 | **9** | ① |
| 3 | `CostedUnit`（施放者） | 6 | **13** | ② |
| 4 | `CostedHero`（督军） | 1 | **3** | ③ |
| 5 | `CostedUnit` | 3 | **7** | ④ |

⇒ **最大值（13）在池序第 2 位、最小值（3）在第 3 位** ⇒ 「按价值排」与「取池序前 N」必然给出不同答案。

### 2.1 改前 → 改后

| # | 改前断言 | 改后断言（`B104_MT_Assert.cs` 现行号） |
|---|---|---|
| A10-a | `method 2` + `maxTargets 1` ⇒ **`null`** | ⇒ **1 张、3 号格（价值 13）**（`:332-344`） |
| A10-b | `method 3` + `maxTargets 1` ⇒ **`null`** | ⇒ **1 张、4 号督军（价值 3）**（`:346-358`） |
| A10-c（新） | ——（旧 `A10-c` 是另一件事，见下） | `method 2` + `maxTargets 3` ⇒ 截断后 **`0:3,0:2,0:5`**（13 → 9 → 7，4 号格的 3 被丢）（`:360-372`） |
| A10-d（新） | —— | `method 3` + `maxTargets 3` ⇒ **`0:4,0:5,0:2`**（3 → 7 → 9，13 被丢）（`:374-385`） |
| A10-e（旧 A10-c 保留并加强） | `maxTargets 0` ⇒ 4 张（只断张数） | `maxTargets 0` ⇒ 4 张 **且序仍是池序 `0:2,0:3,0:4,0:5`**（`:387-400`） |

覆盖对照简报要求：① **排序** = A10-a/A10-b；② **截断（>上限取前 N）** = A10-c/A10-d；③ **不截断不受影响** = A10-e。

### 2.2 灭自证（照简报"去掉排序改回原顺序必须红"逐条写进断言文案）

| 反向改动 | 会红在哪条 | 实得（预期） |
|---|---|---|
| `KeepByValueInPlay` 去掉排序、改回池序取前 N | A10-a | 2 号格（9），不是 3 号格（13） |
| 同上 | A10-b | 2 号格（9），不是 4 号督军（3） |
| 同上 | A10-c | `0:2,0:3,0:4`，不是 `0:3,0:2,0:5` |
| `method 2` 写成升序 / `method 3` 与 `method 2` 共用降序支 | A10-a / A10-b / A10-d | 4 号督军 / 3 号格 / `0:3,0:2,0:5` |
| 只排不截（漏 `Take`） | A10-c / A10-d | 4 张 |
| 排序挪到截断闸**之前**（无条件排） | A10-e | `0:3,0:2,0:5,0:4` |
| 退回旧口径「2/3 档一律 `null`」 | A10-a/b/c/d/e | `(null)` |

⚠️ 这三条**不是**自证：期望值来自**独立的卡面费表**（4/6/1/3 → 9/13/3/7），
不是拿 `KeepByValueInPlay` 的输出反过来当期望。诊断口 `WithValues()`（`:125-140`）
把每一格的 `SimpleAI.ValueInPlay` 打进断言文案 —— 红了直接看是「排错」还是「取错」。

---

## 三、文件改动清单

| 位置 | 改动 |
|---|---|
| `:20-22`（文件头「灭自证」清单） | 旧 A8 行写「2/3 valueInPlay ⇒ 会返回 1 个而不是 `null`」，**前提已不成立**（`B140_WA` 已实现）⇒ 就地改写成 A10 的新反向判据（铁律 5） |
| `:59-67` | 新增 `CostedUnit` / `CostedHero`（A10 专用费用可控卡；理由 + `ValueInPlay` 算式出处写在注释里） |
| `:123-140` | 新增 `WithValues()` 诊断 helper |
| `:311-401` | A10 整段重写（旧 16 行 → 新 91 行） |

行数：**444 → 548**（`git diff --numstat` = `119 / 15`）。**行尾仍是纯 LF**（二进制复核：CRLF 0 / LF 548 / 裸 CR 0）。
本文件**没有**账 `文件:行号` 落点（`grep -rn "B104_MT_Assert" 账/` = 0 命中）⇒ 增行不影响账闸。

改动处已打标记：`✅ 已复刻（2026-11-04 · B141 · out_il2csharp/Assembly-CSharp/AbilityLogic.cs 的 switch (targetSelectMethod - 1) + RuleCore.cs:18102-18108 / :18517）`（`:316-317`）。

---

## 四、验证

### 4.1 类型检查 —— ✅ **0 / 0**

`cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w12 bash 工具/typecheck.sh`（2026-11-04 18:2x）：

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ **过程留痕（不是本文件的错，但会误导下一个人）**：前两次跑都停在
`Assets\CardPresentation\Battle\BattleDriver.cs(42211→42225,65)` 的 `CS1061「CardInteraction」未包含「HasLastDropWorld」`
—— 那是**另一个写手当时正在改 `BattleDriver.cs`**（两次调用之间行号 42211→42225、mtime 就在检查前 9 秒），
编辑器那一半因此拿不到 `WFCheck.dll`（`CS0006`）。第三次（写手收工后）**直接 0/0**。
⇒ 与 `CLAUDE.md` §三「错误全集中在不是你负责的文件上 ⇒ 那不是你的问题」一致；
中途我还用 2026-11-03 23:59 那份 `WFCheck.dll` 单独编过编辑器 rsp，`grep -c "B104_MT_Assert"` = **0**（我的文件从头到尾没红）。

### 4.2 断言没跑（约定如此）

本写手不跑 Unity（铁律 13·3）。A10 五条的**实得值**要靠 `RuleEngineTest.Run` 印出来；
若 `B140_WA` 的实现与判据不符，A10-a/b/c/d 会当场红，且文案里带着每格的实际价值。

---

## 五、还差什么 / 提示调度台

1. **`B140_WA_Assert` 仍未接线**（R4 顺手发现 #2）：`RuleEngineTest.cs:1294` 那段聚合行里**没有** `B140_WA_Assert`
   ⇒ `KeepByValueInPlay` 此前**零断言覆盖**。本批 A10 五条**已经覆盖** method 2/3 的排序 + 截断，
   但仍建议调度台按 `W132` 的安排把它接上（本写手白名单外，未动）。
2. **`RuleCore.cs:17923-17927` 的过期注释**（R4 顺手发现 #3）仍在（写「`ta=150` 手牌半不产 / 2/3 要 `null`」）——
   本写手白名单外，未动，建议派给 `RuleCore.cs` 的写手按铁律 5 就地订正。
3. 本文件里其它断言（A1-A9、B1-B6）**未改**，只做了 A10 与文件头两处。若 `RuleEngineTest.Run` 报出本文件别的红，
   那是另一笔，别混进这批。
