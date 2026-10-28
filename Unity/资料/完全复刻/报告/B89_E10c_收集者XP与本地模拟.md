# B89_E10c · 账 340 增量口 + 本地模拟那半条链 + `ShowCollectionGainXP` 收口 + `dontUseAltPortSetting` 归位

> 执行代理 `B89-E10c` · 2026-10-28 · 判据 = `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:15335-15374`（`AddCollectorXp` 全文）
> ＋ `decomp_full/PlayerDataManager__SetAltPorts.c` ＋ 上一波（E10b）的 XP 链反证。
> 账补丁 = `账/_parts/B89_E10c.tsv`（**2 行** · 11 列 · LF）。

---

## §1 做了什么（四件）

| # | 事 | 落点 |
|---:|---|---|
| ① | **账 340 `AddCollectorXp` 的增量口** | `Shell/PlayerProfileState.cs:211 AddCollectorXp` · `:188 CollectorXpGranted` · `:192 TotalCollectorXp` |
| ② | **本地模拟那条链的客户端半边** | `Shell/PlayerProfileState.cs:239 GrantMatchXp`（`ShowGrantedXpToast` 与 E10b 已有的 `:1486` 那一个口连起来） |
| ③ | **`ShowCollectionGainXP` 那条过期注释**（改成一行转调，**行数不变**） | `Shell/ToastNotifications.cs:812`（**签名仍在 812**，见 §4） |
| ④ | **`dontUseAltPortSetting` 归位到 `GameStaticData`** | `Shell/GameStaticData.cs:224` ＋ `Net/NetConfig.cs:388 SetAltPorts` |

---

## §2 🔴 原版语义 / 我们的本地模拟 / 差在哪（调度台点名的表）

| 环节 | 原版语义（判据） | 我们的本地模拟 | 差在哪 |
|---|---|---|---|
| **一局给多少 XP** | **服务器回包**里的 `RewardInfo.grantedXp`（`RewardService.cs:363` 求和）；`GetXPFromMatch` **0 调用点** | 🔶 `PlayerDataMath.GetXPFromMatch(MatchType, 赢?1f:0f)` —— **借用原版那个客户端版公式**当作「服务器会给的数」 | **值来源不同**（服务器 vs 本地公式）。这是本波**唯一**的自设计，代码里已打 `🔶 自己设计（非原版）` |
| **加 XP** | `CurrentXp += grantedXp`（一个**可写整数**） | `CollectorXpGranted += grantedXp`，总 XP = `CardTierConfig.GetTotalPlayerXPFromInventory()`（库存反推，原版同口径）**＋** 增量 | 原版一个数；我们**两层相加**（因为我们的那份总 XP 是派生值、不可写） |
| **过阈升级** | `CurrentXp >= GetAccumulatedXPForLevel(previousLevel+1)` ⇒ raise 等级信号 + 升级 toast | 同一条比较（同一份 `CardTierConfig` 曲线）⇒ 直接调 `ToastNotifications.ShowCollectionLevelUp(newLevel)` | 原版走**信号层**（`CollectorLevelChangeSignal`），本仓**没有信号层** ⇒ 改成直接调（顶栏/档案窗各自的 `Refresh*` 口负责刷新） |
| **升级 toast 文案** | `Loc("MenuCollection/Toast/CollectorLevel")`（= **`{0}`**，就是等级数字）+ prefab 自带的标题 `Collector Level Up!` | 复用既有 `ShowCollectionLevelUp`（账 510，**调度台已裁 A**）：正文 = `MainMenu/ToastNotifications/PlayerCollectorLevelUp`（`收藏者升级！`），**不印等级数字** | 这是**既有**的已裁定差异（铁律 18「机制不同意图等价」），本波**没有推翻**它（⛔ 不新增第二个升级 toast 口） |
| **`showToast` 的门** | 只管「+N 收藏点数」那条；**升级 toast 不受它管** | 同（`AddCollectorXp` 里升级分支在 `if (showToast)` 之外） | 无 |
| **谁调它** | `RewardService.Collect`：`AddCollectorXp(x, showToast:false)` → 再单独 `ShowGrantedXpToast(x)` | `GrantMatchXp` 按同一顺序（`AddCollectorXp(x,false)` → `ShowGrantedXpToast(x)`） | **调用点还没接**（`Battle/BattleDriver.cs:32444 RecordBattleLog()` 之后调一次；E5c 在写，见 §5） |
| **增量落盘** | 服务器每次登录下发 | **不落盘**（随进程存活） | 未接存档槽（`Shell/SaveKeys.cs` 已有 `CollectorLevel` 键可参照）。出口 = `PlayerProfileState.CollectorXpGranted` |

---

## §3 验证

- **秒级类型检查**：`TMPDIR=/tmp/wf_B89E10c bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
  ⚠️ 中途被别人的在飞改动顶红两次（`RuleEngine/Core/BattleContext.cs` 的 `CS0708 ResolvingExtrinsic` ·
  `Core/TMProWarpText.cs` 的 `CS1061 Bounds.extent` · `Shell/BaseEventPopup.cs` 的 `CS0117 GetAssetReference` ·
  `Shell/AchievementMilestoneToast.cs` 的 `CS0103 LastIconArt`）—— **全都不在本代理的白名单内**；
  等它们编干净后复跑即 **0/0**。
- ⛔ 本波**没有跑 Unity**（红线）⇒ 新增断言**一条都没执行过**。
- 新增断言（`Editor/PlayerDataMathCheck.cs` 的 `CheckCollectorXp()`，**13 条**）：
  增量清零 · 空增量时总 XP == 库存那份 · `AddCollectorXp(10,false)` ⇒ 增量 10 / 总 XP 加 10 /
  返回值 == `GetLevelForXP(总 XP)` · 负值照抄 · `GrantMatchXp(Practice,赢)` == 0 且**不动**增量 ·
  `GrantMatchXp(Ranked,赢)` == 100 且增量记了 100 · `(Ranked,输)` == 30 · `(Campaign,赢)` == 130 ·
  `ResetForTest` 清回。
  ⚠️ 全部传 `showToast: false`：批处理里没有 toast 控制器，弹那条只会刷 `LogError`（不是断言失败）。

---

## §4 一处差点踩到的坑（记下来，下次同类）

`ShowCollectionGainXP` 我第一次改完，**签名从 `:812` 挪到了 `:815`** —— 块总行数没变（后面的 `InternalShowCount`
仍在 `:1461`，**没有任何行号漂移**），但**账的那条落点指的是「签名所在行」**，`:812` 变成了注释行 ⇒ 那一条会被判漂移。
修法：把 5 行 doc 压回 **2 行**、多余的解释**挪到方法之后**当注释 ⇒ 签名回到 `:812`，
`:828 ShowCollectionLevelUp` / `:922 Fill` / `:1002` / `:1063 ToastRootHeight` 全部原位（已逐行核过）。
⇒ **纪律**：这类「就地改写」不只要求**总行数不变**，还要求**被引用的那个符号仍在原来那一行**。

---

## §5 待接点（本波一个字没动）

| 事 | 落点（归属） | 还差什么 |
|---|---|---|
| **XP 的生产调用点** | `Battle/BattleDriver.cs:32444 RecordBattleLog()`（E5c） | 在该方法之后加一句 `PlayerProfileState.GrantMatchXp(Ctx.MatchType, Ctx.Winner == _me + 1);`（⚠️ 赢的判据要用那一处已有的 winner 口径，别另算） |
| **等级显示吃到增量** | `Shell/CardTierConfig.cs:203 PlayerLevel` / `:331 GetTotalPlayerXPFromInventory()` | 加一句 `+ PlayerProfileState.CollectorXpGranted`（**一行**）。⚠️ 那文件不归本代理 ⇒ 这是账 340 升 A 的唯一前置 |
| **增量落盘** | `Shell/SaveKeys.cs` 那一族 + 一个存档槽 | 把 `CollectorXpGranted` 落盘（参照 `NetConfig.OverridePath` 那套：`JsonUtility` + 自检可改道的路径） |

---

## §6 顺手发现（⛔ 没改）

1. **`CardTierConfig.PlayerLevel` 与「总 XP」今天会分叉**：前者只看库存那份、后者已含增量 ⇒
   在 §5 那一行接上之前，`AddCollectorXp` 返回的等级会**大于**界面显示的等级。这不是新缺陷
   （原来是恒等价，因为根本没人加 XP），但**接上之前别拿界面等级去断言 XP 是对的**。
2. **`GetTotalPlayerXPFromInventory()` 每次调用都遍历整个收藏**（`PlayerLevel` 每帧读的地方要小心）——
   本波 `TotalCollectorXp` 会调它，`AddCollectorXp` 一局只调一次，量级没问题；但**别把它塞进每帧刷新的路径**。
3. **升级 toast 的两处文案口径仍不一致**（§2 那张表最后一行）：原版正文是 `{0}` = 等级数字，我们现在印的是
   `收藏者升级！`。这属于账 510 的既有裁定（A），本波**没有推翻**；真要补「等级数字」得改那个已 A 的行，
   建议调度台单独裁。

---

## §7 交接

- **账补丁** `账/_parts/B89_E10c.tsv`：2 行 —— **340 建议 `B`**（增量口 + 升级/经验两条 toast 都照原版；
  但等级显示还差**一行**别处的接线、增量未落盘）、**486 归位后重发**（取代 `B89_E10b.tsv` 里那一行的落点：
  `NetConfig.cs:385/:392` → 现在 `GameStaticData.cs:224` + `NetConfig.cs:388`）。
  `check_patch_conflicts.py` 确认 `B89_E10c.tsv` 在 486 上**赢过** `B89_E10b.tsv`（字典序 `E10c > E10b`）⇒ 合并顺序天然正确。
- **自检入口不用改**：断言都在既有的 `PlayerDataMathCheck.Run` 里（本波 +13 条）。
- **本波没碰的**：`Battle/BattleDriver.cs` · `RuleEngine/**` · `Shell/CardTierConfig.cs` · `Shell/SaveKeys.cs` ·
  `Shell/MainMenuRuntime.cs` · `Shell/BattleLogData.cs` · `Editor/GameStaticDataCheck.cs` · 账主表 · 两张正本 · git。
