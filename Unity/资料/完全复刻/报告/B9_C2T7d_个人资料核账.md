# B9 / C2T7d —— `个人资料_成就_排行.tsv` B 行逐条核（**本轮部分批**，已核 32/218）

> 查判据型子代理（只读 + 落账补丁）。文件 `账/个人资料_成就_排行.tsv` 状态=B 共 **218 条**（单一域 `个人资料/成就/排行`）。
> **本轮的核到范围（按方法名钉）**：`AccountTab` + `Achievement` + `AchievementsManager` + `AchievementsMenu`（B 行序 1–23，L2–L51）+ **`差异` 原文写「一致/等价」的那 10 条**（其中 18/19 已在 1–23 内；散点 50/81/86/87/95/161/166/167/194 已核）。**共 32 条**。
> **下一批从这里继续**：B 行序第 24 条 = `ArmySelectorRanked.FocusOnArmy(RankedArmySelectorContainer)`（L51 之后），往后到第 218 条。
> 产出：`判据/B行逐条核_个人资料.tsv`（8 列 · 32 行）· `账/_parts/B9_C2T7d.tsv`（11 列 · 32 行）· 本报告。

---

## 一、结论速览（32 条已核）

- **建议 A（等价）= 6 · 建议 B（保持）= 26。**
- **6 条升 A**：
  | # | 方法 | 等价依据 |
  |---|---|---|
  | 15 | `AchievementsManager.GetValue(Achievement)` | = `ProfileData.AchievementValue(id)`（读 `_achProgress` 当前值，逐字等价） |
  | 50 | `AvatarTab.OnChoseAvatar()` | = AvatarTab 装备预览那一跳（`AvatarTab.cs:576` 注释明写=原版 OnChoseAvatar/SetAvatar；本地保存等价、上传=服务器 P3） |
  | 95 | `PlayerProfileUIController.OnCollectorLevelUpdate(...)` | = `TopBar.RefreshPlayerLevelIfChanged(:966)` + `ProfileTab(:1624)` 刷新玩家等级/身份 |
  | 166 | `RankedFastMode.GetCurrentRankedDivision()` | = `RankedData`「当前段位」= `DivisionOf(CurrentScore)`（`:289` 注释逐字） |
  | 167 | `RankedFastMode.GetDivisionByPoints(long)` | = `RankedData.DivisionOf`（`:228`「逐步对应」注释，方法体可读） |
  | 194 | `RankedV3Event.GetArmyState(CardArmy,IRankedScoreBoost...)` | = `RankedData:469 GetArmyState`（注释=原版 GetArmyState(army, extraBoosts)） |
- **26 条保持 B** 的成因（同前域）：
  - **落点文件级/方法未定位**（`AccountTab.*` 2/3、`Achievement.Initialize/OnDestroy/OnReachMilestone` 6/8/9、`AchievementsManager.GetAchievement/GetSave/OnDestroy/TryUpdateSave` 11/14/16/17、`AchievementsMenu.*` 20–23、`SetCurrentItem` 81、`ClearCache` 86/87、`SetDivision` 161）—— 我方对应实现**换名了**（见 §三），需逐方法映射。
  - **无独立方法**：`GetIconByTier`(13，图名规则内联在 `AchievementsMenu.cs:438`)、`GetID`(5，等价值在 `Achievement.Id` 字段，按 §4.1 记 B)。
  - **无落点/无对应方法**：`AchievementContainer.OnInitialize`(10，—)、`Achievement.CleanUp`(4，ProfileData 无 CleanUp)。
  - **真偏离（本地等价、缺服务器那半）= P3 边界**：`UpdateSave(ach)`(18)、`UpdateSave(string)`(19) —— 本地值/档位/奖励/点数/存档都在（`SetAchievementValue`/`AddAchievementValue`+`SaveIfAuto`），**缺「上传服务端 + Steam 同步」**（原版这条走 AchievementsManager 批量上传）。→ 写全证据 + 时间限定后属 **P3（服务器/Steam），判 D 需调度台裁**。
  - **语义不同**：`GetHighestValue`(12) —— 我方 `AchievementTierOf` 给「当前第几档」，不是「历史最高值」。
  - **账户删除**：`DisplayDeleteAccountMessage`(1) —— 账户删除走 PlayFab 账户服务（**P3**），落点未定位到方法。

---

## 二、方法学 & 证据

1. **机械分类（全 218 条）**：`落点` 里**真正方法级（（）内是方法名）只有约 1 条（#95）**；**187 条是文件级（类级命中，`Shell/XXX.cs`）**；**9 条无落点（—）**；其余是描述性括注（`字段表实读`/`装备清单`/`Top4支` 等）。⇒ 这正是**「类级命中 ⇒ 只能 B」**（方案 §4.1）的典型：这个域的账**几乎全靠类级落点在撑**。
2. **读过的落点实体**：`ProfileData.cs` 的成就一组（`AchievementValue/SetAchievementValue/AddAchievementValue/AchievementTierOf/TargetOf/RewardOf/PointsOf` `:1168-1260` · `GainAchievement` `:502-560`）· `AchievementsMenu.cs`（勋章图内联 `:438`）· `AvatarTab.cs`（OnChoseAvatar 注释 `:576`）· `RankedData.cs`（`DivisionOf` `:228`、当前段位 `:289`、`GetArmyState` `:469`）· `TopBar.cs:966`。
3. **升 A 判据**：只对「找到真实方法 + 逐字/注释逐句等价」的升 A；未定位的一律留 B。

---

## 三、🔑 顺手发现（对本域后续批次很有用）

- **本域的 B 绝大多数是「我方换了方法名」而非「没做」**。例：
  - `AchievementsManager.GetValue` → `ProfileData.AchievementValue`
  - `GetDivisionByPoints / GetCurrentRankedDivision` → `RankedData.DivisionOf / 当前段位`
  - `GetArmyState` → `RankedData.GetArmyState`
  这些行为**已等价**（本轮已把能找到的 6 条升 A）。**下一批的高效打法**：先一次性把「原版方法名 → 我方改名方法」建一张映射表（在各 `Shell/*.cs` 里 grep 语义相近的名字），再逐条核，能批量把「类级命中未核」升 A。
- **P3 边界**（写全证据 + 时间限定，供判 D）：① 账户删除 `AccountTab`（PlayFab 账户服务）② 成就**上传服务端 + Steam 同步**（`UpdateSave` 的服务器那半）。⚠️ 但 **UI 部分不判 D**（成就页/排行页界面已在）。
- **`Achievement.GetID` 的等价值在字段 `Achievement.Id`**（非方法）—— 按 §4.1「字段落点记 B」保留。
- **`GetIconByTier` 没有独立方法**：图名规则 `40k_Achievements_icon_medal{tier}` **内联**在 `AchievementsMenu.cs:438`（这是「无独立取值口」，不是缺口）。

---

## 四、产物 & 覆盖边界

- `判据/B行逐条核_个人资料.tsv`：8 列 × **32 行**（已核批；A=6/B=26）。
- `账/_parts/B9_C2T7d.tsv`：11 列 × 32 行（状态列全保持原值；只改 差异（A 行改写「等价（C2T7d 核）：…」/ B 行原差异后追加 `｜[C2T7d 2026-10-27] …`）/ 轮次=`B9_C2T7d` / 备注前缀 `[C2T7d 2026-10-27]`，原备注保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7d` → **更新 32 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- **🔴 覆盖边界（重要）**：本轮**只核了 218 条里的前 23 条（AccountTab/成就簇）+ 散点「一致/等价」9 条 = 32 条**；**剩余 186 条（B 行序 24–218，`ArmySelectorRanked` 往后）本轮未核，全部维持 B 原状（未进 patch）**。下一批：**从第 24 条 `ArmySelectorRanked.FocusOnArmy(RankedArmySelectorContainer)` 继续**，建议先建「原版名→我方改名」映射表（见 §三）再逐条核。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。
