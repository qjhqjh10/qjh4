# B9 / C2T7e —— `个人资料_成就_排行.tsv` B 行逐条核（续：第 24 → 218）

> 查判据型子代理（只读 + 落账补丁）。承接 C2T7d（前 32 条）。本批处理「C2T7d 之后剩余的 B 行」。
> 产出：`判据/改名映射_个人资料.tsv`（4 列 · rename map）· `判据/B行逐条核_个人资料_续.tsv`（8 列 · 186 行）· `账/_parts/B9_C2T7e.tsv`（11 列 · 32 行）· 本报告。

---

## 〇、🔴 先纠一个坑：B 行**序号已漂移**（以后一律按方法名，别用序号）

协调台把 C2T7d 的 6 条升了 A ⇒ 那 6 条**退出 B 集合** ⇒ 「第 24 条」**不再是** `ArmySelectorRanked.FocusOnArmy`（现读**第 24 条 = `ArmySelectorRanked.Initialize`**）。C2T7d 我按序号取的那 32 条（其实方法名对得上、patch 也是按方法名匹的，没出错），但**本批起全部按方法名处理**，序号只作参考。

---

## 一、结论（剩余 186 条已分级）

- **建议 A = 4 · 建议 B = 182。**
- **建议升 A 的 4 条（数据层改名映射，双侧方法级等价）**：
  | 原版方法 | 我方方法 | 文件:行 |
  |---|---|---|
  | `GetCurrentRankedDivision()` (RankedV3Event) | `RankedData.CurrentDivision` | RankedData.cs:290（= DivisionOf(Points)） |
  | `GetArmyState(CardArmy, IReadOnlyList<RankedScoreBoostEvent>)` (RankedFastMode) | `RankedData.ArmyState` | RankedData.cs:474 |
  | `GetWins(CardArmy)` (RankedScoreBoostSave) | `RankedBoostEvent.WinsFor` | RankedData.cs:99 |
  | `IsArmyAffectedAndHasBoostsLeft(CardArmy)` (RankedScoreBoostEvent) | `RankedBoostEvent.Affects + BoostsLeftFor` | RankedData.cs:97 / :100 |

---

## 二、🔑 关键结论：剩下的 182 条 B **不是「改名能解」，是「架构差异」**

这是本批最重要的判断：**剩余 182 条里，绝大多数是 UI prefab 的 MonoBehaviour 回调方法** —— `Awake / OnEnable / OnDisable / Start / OnDestroy / Draw / Click / OnClick / HandleOnClick / Toggle / ToggleHighlight / ToggleLock / Initialize(组件) / Refresh / SetXxx / Close` 等。

我方架构**根本没有同名方法**：这些行为由壳里的 **`Build()` / `Refresh()` / `Select()` / `OnOpen()`** 统一承接（例：`AvatarTab.cs` 只有 `Build/Select/ConfirmSelect/ToggleBorder/Refresh/OnOpen/ButtonLabel`；`RankedTab.cs` 只有 `Build/OnOpen/RebuildTop4/RebuildRows/RefreshIdentity`；`LeaderboardRow.cs` 只有 `Build/OnRowClicked/RebuildBackgroundForTest`）。

⇒ **这不是「我方换了方法名」那种可一表升 A 的情况**，而是「原版每个 prefab 组件一个 MonoBehaviour 回调」 vs 「我方一个页面 Builder 统建」的**结构性差异**。要判 A 只能**逐条比「那一条回调的可见行为」是否被 Builder 复现**（视觉/交互级），⛔ 不能靠改名映射批量升 A。**这正是该域 B 一直压着的根因**（187/218 是文件级落点）。

**数据层（RankedData / LeaderboardData / ProfileData / BattleContext）例外**：那里是我方**真方法**，能靠改名映射升 A —— 本批找到 4 条（见 §一）；`ProfileData`/`RankedData` 里另有一批「数据层 B」见 §三。

---

## 三、数据层「改名映射的下一层」——下一批的入口

剩余 28 条数据层 B（已进 patch 更新说明）多是**LiveOps 事件回调 → 我方数据方法**，语义近但**未逐条核**：
- `RankedV2Event / RankedV3Event` 的 `OnMatchFinished / OnMatchProcessSuccess / OnSeasonCollected / RecordMatchFinished / StartMatch / Expire / MatchFoundCallback / OpenSeasonChangePopup` → 我方 `RankedData.ApplyMatchResult / MarkSeasonCollected / AdvanceSeason / ToggleRankedMode / NoteOpponent`（近名、事件语义需逐条对）。
- `RankedEventBase.GetBaseData / RankedFastMode.GetBaseData` → 我方 `LiveOpsEvent.GetBaseData` 等价物（未定位）。
- `PlayerAvatarDataManager.GetAllItems/GetItemWithId/GetItemImageWithId/IsDefaultBorderEquipped/SetCurrentItem/SetCurrentItemNoSave` → `ProfileData` 头像/边框那一组（换名，未逐条定位）。
- `RankedMatchResult.GetWinner/SetBattleResult` → `RuleEngine/Core/BattleContext.cs` 的战果字段（未定位）。

**下一批建议**：把上面这条「事件回调 → 数据方法」的映射补全（比 UI 回调那批可映射度高得多），能再收一批 A。

---

## 四、方法学 & 证据

1. 机械分类：剩余 186 条里，`落点` 真正方法级极少；绝大多数是文件级（`Shell/XXX.cs`）+ 8 条无落点。→ 与 C2T7d 同型。
2. 读过的落点：`RankedData.cs` 全方法表（`DivisionOf :245 / DivisionOfV2 :506 / CurrentDivision :290 / ArmyState :474` · `RankedBoostEvent.Affects :97 / WinsFor :99 / BoostsLeftFor :100`）· `RankedTab.cs`（`Build/OnOpen/RebuildTop4/RebuildRows/RefreshIdentity`）· `LeaderboardWindow.cs`（`SelectTab/SelectArmy/Open`）· `LeaderboardRow.cs`（`Build/OnRowClicked`）· `AvatarTab.cs`（`Build/Select/ConfirmSelect/ToggleBorder/Refresh`）。
3. A 判据：仅数据层、找到真实方法且逐句/注释等价的 4 条升 A；UI 回调类一律 B（架构差异，未逐条比行为）。

---

## 五、产物 & 覆盖边界

- `判据/改名映射_个人资料.tsv`：4 列（`原版方法 / 我方方法 / 文件:行 / 依据`）· **4 条**（本轮验证过的）。⚠️ 表**不能**继续靠「改名」扩张到 UI 回调那 182 条（架构差异，见 §二）。
- `判据/B行逐条核_个人资料_续.tsv`：8 列 × **186 行**（覆盖剩余全部 B 行；A=4/B=182，每行带一句结论+还差）。
- `账/_parts/B9_C2T7e.tsv`：11 列 × **32 行**（**4 条 A + 28 条数据层 B**；UI 回调那 154 条本轮不 patch，保持 B 原状且原备注已准确=「类级命中；方法级未核」）。表头逐字抄主表、状态列不动、轮次=`B9_C2T7e`。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7e` → **更新 32 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- **覆盖边界**：剩余 B 行（序号 24→218，因升 A 已漂移，按方法名）**全部机械分类完毕**；A 只 4 条（数据层可映射的），其余 182 条是 UI 回调类**架构差异**，需逐条比可见行为（非批量升 A）。下一批入口 = §三 的「事件回调→数据方法」映射。

- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。
