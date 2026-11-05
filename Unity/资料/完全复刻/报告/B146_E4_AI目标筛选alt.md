# B146-E4 · AI 目标筛选 alt 收口（2026-11-05）

## 一、改了什么

| 文件:行 | 改动 |
|---|---|
| `Core/TargetCriteriaEval.cs:458` | `static bool? MeetsStatCriteria(...)` → **`internal static`**（同一行改关键字，**行数不变**：1223 → 1223） |
| `Core/TargetCriteriaEval.cs:621` | `static int? RarityOf(...)` → **`internal static`**（同上；四组里的第 4 组需要它） |
| `Data/AiTargets.cs:672-689` | 四组不再各自算 alt，**全部转调唯一求值器**；新增 `thisBastion`（`:679`）；攻组 alt = `t.RangedAttack`（`:680`）· 血组 = `thisBastion`（`:681`）· 费 = 0（`:682`）· 稀有度组**真筛**（`:683-689`，`RarityOf` + 同一求值器） |
| `Data/AiTargets.cs:732-739` | 删掉本类那份 17 行 `switch`（原 `MeetsStat`），换成**一行转发** `StatPasses`：`TargetCriteriaEval.MeetsStatCriteria(...) ?? false` |
| `Data/AiTargets.cs:675` | 加标记 `// ✅ 已复刻（2026-11-05 · B146-E4 · 判据 CheckIfMeetsCriteria.c:326-328 / :349-352；alt 口径收口到 TargetCriteriaEval.MeetsStatCriteria）` |

`git diff --numstat`：AiTargets **18+ / 23−**（1128 → 1123 行）· TargetCriteriaEval **2+ / 2−**。
🔴 **账 `账/*.tsv` 对本文件的全部落点只有 3 处，且都在 `:80-86 / :113 / :399`（改动点之前）⇒ 零漂移**；
⚠️ **证据 md（`判据/B143*`）里那些行号会整体 −5**：`:866-877 → :861-872`、`:962 → :957`（该文件是「随机流」那段，内容未动）。

## 二、为什么可达性 0（证据）

`Resources/ai_scoring.json` 全文件正则现扫（236 次出现 = 118 条 `targetCriteria` + 118 条 `multiplierCriteria`）：

| 字段 | 出现过的值（条数） |
|---|---|
| `attackFilterOption` | **只有 0**（236） |
| `currentHealthFilterOption` | **0（236）· 2（4）** |
| `manaCostFilterOption` | **只有 0**（236） |
| `rarityFilterOption` | **只有 0**（236） |

⇒ 原版**只有 option 6/9/10/11 读第 5 实参**，而这四档在池内 **0 条** ⇒ alt 传对传错都改变不了任何评分结果
（这就是「改完看不到差异」的全部原因，不是修复没生效）。
唯一非零的线上档 = 血组 **option 2**（EqualOrHigher，不读 alt）—— 行为必须原样，断言 H5/H6 钉住它。
另一处（比 alt 更小）的语义变化：`option == -1` 以前落 `default: true`，现在按唯一求值器返回 `null` ⇒ AI 侧 `?? false`
（「数据缺失不许冒充『这一组不筛』」）。池内该字段同样**没有 −1** ⇒ 今天也 0 可观测。

## 三、断言与接线状态

- 新建 **`Assets/RuleEngine/Editor/B146_E4_Assert.cs`**（全局命名空间、类名 == 文件名、`Run`/`Pass`/`Fail`；13.5 KB）。
  18 条：**S0-S5**（源码层结构：无 `MeetsStat(` / 必须调 `TargetCriteriaEval.MeetsStatCriteria` / 攻组 alt = `t.RangedAttack` / 血组 alt = `thisBastion`）·
  **A0-A2**（option 9 走远程）· **H0-H6**（option 6 的 bastion · option 9 的 alt · 线上 option 2 不回归）· **R1-R2**（稀有度真筛）· **D0-D4**（数据面机检「alt 档 0 条」）。
  每条的「怎么才能红」写在文件头。
- **⚠️ 未接宿主**（`RuleEngineTest.cs` 不在本代理白名单）。调度台请在该文件聚合行**同一物理行、第一个 `//` 之前**追加：
  `Guard("B146E4_Assert", B146_E4_Assert.Run); _pass += B146_E4_Assert.Pass; _fail += B146_E4_Assert.Fail;`
- **没跑过这些断言**（本代理不许跑 Unity）。离线冒烟：`bash 工具/ruleprobe.sh check` → **rc=0**（全池 1134 张 · 解析差异 0 · 哨兵 0）；
  `bash 工具/ruleprobe.sh ai actions "Embrace the Pain"`（池内 4 条 option 2 的卡之一）→ 动作表 13 条、**rc=0、无异常**。

## 四、typecheck

`TMPDIR=/tmp/wf_e4{,b,c,d} bash d:/4/Unity/工具/typecheck.sh` —— **运行时 0 / 编辑器 0**（每次改完各跑一次，共 4 次全绿）。
⚠️ 工作树里 `Data/AiScoring.cs` · `Data/SimpleAI.cs` 的 `M` **不是本代理改的**（别的写手在飞），本次编译把它们一起编过、无误。

## 五、顺手发现（都没动，不在白名单）

1. `Core/CountEval.cs:870` 的注释还写「口径与 `Data/AiTargets.MeetsStat` 同一张表」—— **该方法已删**，应改成指向 `TargetCriteriaEval.MeetsStatCriteria`。
2. 费组实参口径**仍是两份**：`AiTargets` 传 `t.Cost`（`CardDef.Cost` 基础费），而原版 ③ 组读 `get_CurrentCost`，我们唯一求值器的 `StatGroupsPass` 自己用的是 `RuleCore.CostOf(ctx, side, inst)`。池内 `manaCostFilterOption` 全 0 ⇒ 今天休眠，但这是「同一规则两处实现」的残留。
3. `thisAtk = thisCard.Attack`（`CardDef` 基础近战）与原版 `get_CurrentMeleeAttack(thisCard)`（当前值）在手牌出牌那一档可能不同；只影响 option 4/5/7（池内 0 行）。
4. **更大的收口没做**：`AiTargets.Match` / `ApplyFilters` 整体仍是 `CheckIfMeetsCriteria` 的第二份（子集）实现（本批只并了 stat 四组）。
5. 账/证据行号：见 §一末尾的 −5 位移表；`活状态表`/`B143*` 若按旧行号引用需同步。
