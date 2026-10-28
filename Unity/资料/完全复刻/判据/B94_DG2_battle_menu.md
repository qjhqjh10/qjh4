# B94_DG2 · `BattleScene.Run` 8 红 + `MainMenuScene.Run` 13 红 —— 逐条定性

> **只读诊断代理 DG2 · 2026-10-29**｜输入：`d:/4/_tmp_view/battle.log`（5824 通过 / 8 失败）· `d:/4/_tmp_view/menu.log`（5607 通过 / 13 失败）
> ⛔ 本代理**没改源码 / 账 / 正本**，**没跑 Unity**，**没动 git**。
> 计数口径：按**行首标记**数（`grep -c ✗` 不可用 —— `✗` 也出现在 ✓ 的文案里）。battle 日志 10 行含 `✗`，其中 2 行是「合计行 / ✓ 文案」，真红 **8**；menu 日志 13 行含 `✗`，真红 **13**。

---

## §一 汇总

| 宿主 | 总红 | α 断言写错 | β 实现缺陷 | γ 本批回归 | δ 夹具/宿主前提不成立 |
|---|---|---|---|---|---|
| `BattleScene.Run` | 8 | 2 | 0 | **2** | 4 |
| `MainMenuScene.Run` | 13 | 5 | **4** | 0 | 4 |
| **合计** | **21** | **7** | **4** | **2** | **8** |

**根因只有 8 个**（21 条红里 13 条是别的条的连带）：

| # | 根因 | 档 | 覆盖的红 | 落在哪 |
|---|---|---|---|---|
| R1 | 本会话 `B94_F2` 在战术卡出牌处**无条件**补了一次 `PayCardPlayedCostOath` ⇒ 带 oath 的战术卡**永远多扣 1 点**（平版多扣 1、誓约版多扣 1 = 双扣） | **γ** | B88-8a · B88-8b（2） | `RuleEngine/Core/EffectResolver.cs:2587` |
| R2 | `LocalPlayerDataStore.SaveKey = "LocalPlayerData"` **不在 `SaveKeys` 键表、也没有 `_ours_` 前缀** ⇒ `SaveCenter` 拒绝一切读写 ⇒ 这条存档链**在生产里也从来没落盘过** | **β** | B7 ×4（4） | `Shell/LocalPlayerData.cs:142` |
| R3 | X2 的「双平面都打空」夹具其实**打中了**敌兵平面（`Quaternion.Euler(-90,0,0)` 的 forward 带 ~1e-8 的 z 残差 ⇒ `Plane.Raycast` 在 4.7e7 处命中） | **δ** | X2 B1 · B2 · B3 · C4（4） | `Editor/B94X2_Assert.cs:101-107`（B）· `:147-153`（C） |
| R4 | Y5 的「拖拽走完」取样点**跨过了循环回绕点**（相位 2 的 `_ptrPhaseT` 已走到 0.09，再推 0.72 ⇒ 0.81 ≥ `moveDuration+0.1`=0.8 ⇒ 回首 `ShowStartingPointer` 把位置写回起点） | **α** | Y5 D14 · D15（2） | `Editor/B94Y5_Assert.cs:151-157` |
| R5 | S4-B8 收尾期望「拨盘还原 ⇒ `MaxOf` = 0」，而 `campaignPoints` 的**生产值本来就是 2000** | **α** | S4 B8（1） | `Editor/B93S4Assert.cs:103` |
| R6 | S4-E6 的夹具把 `DA1` 放在**非最高那一组**里，而原版只在该组 `victoriesById` 里找 | **α** | S4 E6（1） | `Editor/B93S4Assert.cs:226-230`（map 定义） |
| R7 | T1 两条夹具前提错：②f 以为「`CampaignBadge` 定义数不出东西」，其实**战役根节点真可领**；②g 改完数据**没调 `Refresh()`** ⇒ 读的还是旧快照 | **α** | T1 ②f · ②g（2） | `Editor/B94T1_Assert.cs:193-203` |
| R8 | B18-C0 用 `MainMenuRuntime.Instance` 判「有一棵主菜单」，而 `Instance` **只在 `Awake` 赋值、编辑模式下恒 null**（本类自己就写了这条坑并给了 `InstanceOrFind()`） | **α** | B18 C0（1） | `Editor/B94B18_Assert.cs:99` |
| R9 | X1-F 想用反射调 `Update()` 驱动 `Time.deltaTime` 状态机 —— 编辑模式批处理里那不是可用的正帧长（实测 0° ⇒ 累加器被推成负/异常值，闸门永不开），F4 因此也是「空转绿」 | **δ** | X1 F1 · F2 · F3（3） | `Editor/B94X1_Assert.cs:259-271` ／ 需在 `WarpforgeArena1/Runtime/ArenaIconRotation.cs` 加 dt 注入缝 |
| R10 | B10-G2 想「换下标 ⇒ 画名跟着变」，而下标 1..4 的四条都是**限时边框、父活动已过期** ⇒ 读口 `EquippedBorder` 一进就 `RefreshExpiredBorders` 把它**卸回 `Default`**，于是 n1 == n0 | **δ** | B10 G2（1） | `Editor/B94B10_Assert.cs:306-311` |

---

## §二 逐条（21 行）

### `BattleScene.Run`（8）

| # | 断言文案（截断） | 期望 / 实得 | 归档 | 证据（文件:行号） | 该怎么修 |
|---|---|---|---|---|---|
| 1 | `B88-8a`：点誓约版 ⇒ 扣卡费 1 + 誓约 1（9 → 6） | 期望 7 / 实得 **6**（多扣 1） | **γ** | `EffectResolver.cs:2587` 本会话新增的 `if (card.Has(KeywordTable.Oath)) PayCardPlayedCostOath(...)`；`Adaptive Strategy` 卡费=1（`cards_engine.json` `UM109`）⇒ 1+1+1 | 删掉那句内联调用，恢复成 `RuleCore.ConsumeOnceCostMods(ctx, p, inst);`（见 §三·R1） |
| 2 | `B88-8b`：点平版 ⇒ 只扣卡费 1（9 → 7） | 期望 8 / 实得 **7**（多扣 1） | **γ** | 同上 | 同上（同一句） |
| 3 | `X2 B1`（前提）这条射线确实两个平面都打空（`AimMisses` +1） | 期望 1 / 实得 **0** | **δ** | `B94X2_Assert.cs:99-107`：`Quaternion.Euler(-90,0,0)` 的 forward 实际是 `(0, 0.99999996, ±2e-8)` ⇒ 敌兵平面 `vdot≈8e-8 ≠ 0` ⇒ `Plane.Raycast` 在 4.7e7 处**命中** | 把 HUD 相机改成「稳打空」的朝向（见 §三·R3） |
| 4 | `X2 B2` ★ 双打空 ⇒ 落点 = 世界原点 | 期望 (0,0,0) / 实得 **(0, 47171730, -4)** | **δ** | 同上；实得值的 `z = -4 = enemyRowZ` 正是「命中敌兵平面」的铁证 | 同上 |
| 5 | `X2 B3` …`LastAimOnPlane = false` | 期望 True / 实得 **False** | **δ** | 同上（点落在平面上 ⇒ `LastAimOnPlane = true`） | 同上 |
| 6 | `X2 C4` 弧线终点 = 求交结果（此夹具双打空 ⇒ 原点） | 期望 (0,0,0) / 实得 **(0, 47171730, -4)** | **δ** | `B94X2_Assert.cs:147-153` 复制了 B 的坏夹具 | 同上 |
| 7 | `Y5 D14` ★★ 拖拽走完 `moveDuration(0.7)` ⇒ 落在 `GetAdjustedPos(to)` | 期望 (3.000,-1.500) / 实得 **(2.000,-2.000)** = 起点 | **α** | `B94Y5_Assert.cs:151` 的 `Advance(0.72)`；`TutorialOverlay.cs:1413-1415`（相位 2 在 `_ptrPhaseT ≥ 0.7` 时 t=1 ⇒ 终点，`≥ 0.8` 时回首）；D13 实测「离起点 0.144」⇒ 进 D14 前 `_ptrPhaseT ≈ 0.09` ⇒ 0.09+0.72=0.81 **跨过 0.8** | 改成「在相位 2 内逐拍采样、取离终点最近的一刻」（见 §三·R4） |
| 8 | `Y5 D15` ★★ 灭自证：终点确实离开起点（x 差 > 0.5） | 期望 True / 实得 **False** | **α** | 同上（位置已被回首写回起点，差 = 0） | 同上 |

> ⚠️ D14 的文案把自己的失败原因写成了「单点口那版没有终点 ⇒ 还停在起点」—— **不是这个原因**（`SetPointer` 的双点口是好的：D4/D6/D7 都绿）。**别按文案去改实现**。

### `MainMenuScene.Run`（13）

| # | 断言文案（截断） | 期望 / 实得 | 归档 | 证据（文件:行号） | 该怎么修 |
|---|---|---|---|---|---|
| 9 | `S4 B8` 拨盘已还原（收尾不许漏到别的段） | 期望 0 / 实得 **2000** | **α** | `B93S4Assert.cs:103`；`MainMenuRuntime.cs:2110` `MaxAmount = 2000`（生产值，判据是实拍 `300/2000`）；`ClearCounterForTest()`（`:2160`）只清拨盘 ⇒ 回落生产值 | 段首先记 `var savedMax = MainMenuRuntime.MaxOf(saved);`，段尾比 `MaxOf(saved) == savedMax`（⛔ 别写死 0） |
| 10 | `S4 E6` ★ 战报优先：`ZZ9` 不在桶、`DA1` 在 ⇒ 取 `DA1` | 期望 DA1 / 实得 **UM47** | **α** | 夹具 `B93S4Assert.cs:226-230`：`DA1` 在 `SW5` 组的 `victoriesById`，而最高组是 `UM82`（`{UM47:4}`）。原版判据 `decomp_full/WarlordMasteryContainer__OnInitialize.c:70-84`：谓词比对的是 `highestGroup.Value.victoriesById`、`HighestCardId` 也取同一组 | 把 `DA1:2` 塞进 **`UM82`** 那一组：`{ { "UM47", 4 }, { "DA1", 2 } }`（E4 仍取 UM47，两态照样可分） |
| 11 | `B18 C0`（前提）宿主里有一棵常驻主菜单（`MainMenuRuntime.Instance` ≠ null） | 期望 True / 实得 **False** | **α**（边界 δ） | `MainMenuRuntime.cs:55` `Awake` 里才赋值；本文件 `:874-877` 自己记过「编辑模式恒 null ⇒ 改走 `InstanceOrFind()`」；`:1703` 就是那个口 | 改成 `MainMenuRuntime.InstanceOrFind() != null`（⚠️ 改完 C1–C6 才第一次真正执行，可能还有新红 —— 见 §四） |
| 12 | `T1 ②f` 🧨 灭自证：拿 `CampaignBadge` 定义去数 ⇒ 0 | 期望 0 / 实得 **1** | **α** | `BadgeNotifications.cs:339-347` 的 `CampaignNotificationProvider` 在「战役根节点可领」时**真的发一条** `CampaignBadge`（`:344` `CampaignData.Claimable`）；`Validate`（`:278-284`）是真类型过滤，所以这 1 条是**真命中** | 换一个**没有 provider** 的私有类型：`sealed class B94T1NoBadge : BadgeNotification {}` 当定义类型 ⇒ 必 0（⛔ 别改用 `ForgeBadge`：锻造也有 provider） |
| 13 | `T1 ②g` 四类源全不可领 ⇒ Missions 页签计数 = 0 | 期望 0 / 实得 **1** | **α** | `B94T1_Assert.cs:198-201` 改完 `DailyData` 状态**直接** `GetCount`；而 `BadgeNotifications.cs:206-224` 的 `GetCount` 只读 `_notifications` **缓存快照**，不刷新；`DailyData.cs:2261/2266` 那几个 `Force*ForTest` 都不调 `Refresh` | 每个状态变更后补一句 `UiBadgeNotificationManager.Refresh()`（`notify:false` 那档，见 `BadgeNotifications.cs:178` doc「读时刷新」）再 `GetCount` |
| 14 | `X1 F1` ★ `rate = 0` ⇒ 每帧都转（实测 0°） | 期望 True / 实得 **False** | **δ** | `B94X1_Assert.cs:259-263` 反射调 `Update`；`ArenaIconRotation.cs:54-62` 的闸门是 `updateRate <= accumulator`，accumulator 只加 `Time.deltaTime`。实测 0° ⇒ 累加器为负/异常 ⇒ 这个宿主里 `Time.deltaTime` 不是可用的正帧长 | 给组件加 **dt 注入缝**（`TickForTest(float dt)`，`Update` 只转发 `TickForTest(Time.deltaTime)`），断言改成喂 dt（先例：`B93S3Assert.cs:584-608` 的 `SimpleRotation.Update(euler, speed, dt)` / `LoadingController.Tick(dt)`） |
| 15 | `X1 F2` ★ 转动量 = \|ySpeed\| = 0.02° | 0.00 ≈ 0.02 | **δ** | 同上 | 同上（喂 `dt = 0f` ⇒ 一次 0.02°） |
| 16 | `X1 F3` ★ 两次 Update ⇒ 0.04°（证明是 `<=` 含等号） | 0.00 ≈ 0.04 | **δ** | 同上 | 同上（喂两次 `dt = 0f`） |
| 17 | `B10 G2` ★ 两态：下标 1 的画名 ≠ 下标 0 | 期望 True / 实得 **False**（两边都是 `Player_Profile_Border`） | **δ** | `ProfileData.cs:1453-1467`（含 `:1463`） 只卸「已装备」那一条 ⇒ 读口 `EquippedBorder`（`:1344`）每次都 `RefreshExpiredBorders(NowUtc)`；日志已印 `限时边框 EC Release 已不可用 ⇒ 卸下、回到 Default`；本机今天可用边框 = **1 条**（`UsableBorderCountForTest` doc `:1469-1478`） | 给读口加**可注入的 now**（`ProfileData.NowUtcForTest`），在活动期内（如 `ExpansionOrks1Scoring` 的 2025-04-15）装备下标 1 ⇒ 画名必须变；收尾还原。⛔ 别改成「跳过 usability 检查」—— 那是原版行为 |
| 18 | `B7` 把一份**逐格非零**的总档写进临时档 | 期望 True / 实得 **False** | **β** | 日志三连：`[SaveCenter] Set：key 'LocalPlayerData' **不在键表** ⇒ 拒绝`。`LocalPlayerData.cs:142` `SaveKey = "LocalPlayerData"`；`SaveCenter.cs:519` 只放行 `SaveKeys.IsKnown`；`SaveKeys.cs:339-346` = 键表 51 条 **或 `_ours_` 前缀**（`:307` `OursPrefix`） | `LocalPlayerDataStore.SaveKey` 改成 `SaveKeys.OursPrefix + "LocalPlayerData"`（或字面量 `"_ours_LocalPlayerData"`），并同步改 `:141` 的注释（那条注释还写着「⛔ 别把它并进那 51 条」—— 它确实没并进，但**必须带前缀**） |
| 19 | `B7` `ReadDataFromFile` 读回来了 | 期望 True / 实得 **False** | **β** | 同 R2（`Get` 也被拒） | 同 #18 |
| 20 | `B7` 结果档 = BadVersion | 期望 BadVersion / 实得 **Missing** | **β** | 同 R2（档根本没写进去 ⇒ 读侧只能报 `Missing`，永远走不到版本闸） | 同 #18 |
| 21 | `B7` 拒绝时有**人话**（⛔ 不静默） | 期望 True / 实得 **False** | **β** | 同 R2（`Missing` 那支不写 `LastError`，只有 `BadVersion` 才写） | 同 #18。⚠️ 这条**值得表扬**：它是唯一把 R2 从「静默失效」变成「一条红」的断言 |

---

## §三 β / γ 展开（要改代码的最小改动）

### R1（γ，2 条）· `EffectResolver.cs:2587` —— 战术卡的誓约费被**无条件**再扣一次

**现状（本会话 `B94_F2` 加的）**

```csharp
RuleCore.ConsumeOnceCostMods(ctx, p, inst); if (card.Has(KeywordTable.Oath)) PayCardPlayedCostOath(ctx, p, inst);   // 🆕 2026-10-29（B94_F2 …）
```

**为什么错（三条独立证据）**

1. 原版里 `PayCardPlayedCostOath` 的**唯一调用点**在 `AbilityLogic__PlayAbility`：`grep -rl PayCardPlayedCostOath decomp_full/` 只命中 `AbilityLogic__PlayAbility.c` + 它自己。
2. 那一处**有闸**：`AbilityLogic__PlayAbility.c:2015-2033` —— 先读 `*(param_2 + 0x2e8)`（= `cardChosenInSelection`），非 null **且** `RawCardScript__HasDefaultTrait(chosen, 0x4fb) == true` 才 `PayCardPlayedCostOath`。⇒ **只有「选中了誓约版」那一条才付**；平版一次都不付。
3. `B88_A1` 的取证（`资料/完全复刻/报告/B88_A1_oath二选一.md` §三·3）本来就是把誓约费收在**被选中变体的正文**里 ⇒ 我们这边由 `ChargePaidGroups` 按 `CostKindOf == "oath"` 那一组收。

**⇒ 现读行为：平版 1+1=2（应 1）、誓约版 1+1+1=3（应 2）。** 与实测完全吻合（9→6 / 9→7）。

**最小改动**

```csharp
RuleCore.ConsumeOnceCostMods(ctx, p, inst);   // ← 把同一行末尾那句 if 删掉（行数不变 = 零账漂移）
```

⛔ **不要**改成「只在誓约组也在场时才扣」—— 那就与 `ChargePaidGroups` 双扣。⛔ 也**别动** `RuleCore.cs:3945`（单位那条）—— 那条不在这 8 条红里，且它有独立的取证链（`PayActiveAbilityCostOath`），改它要另开证据。

**连带提醒（本报告范围外但要排）**：同一句会让 `RuleEngineTest.TestTacticOathChoice` 也红 —— 那里 `RuleEngineTest.cs:7788` 断言 `2 → 0`、`:7804` 断言 `2 → 1`。修完 R1 后**必须单独复跑 `RuleEngineTest.Run`**（本次没给我它的日志）。

### R2（β，4 条）· `LocalPlayerData.cs:142` —— 存档 key 过不了 `SaveCenter` 键表闸

**现状**

```csharp
public const string SaveKey = "LocalPlayerData";     // LocalPlayerData.cs:142
```

`SaveCenter.ValidateKey`（`SaveCenter.cs:519`）→ `SaveKeys.IsKnown(key)`（`SaveKeys.cs:339-346`）= **键表 51 条 ∪ `_ours_` 前缀**。`"LocalPlayerData"` 两者都不是 ⇒ `Set` / `Get` / `Delete` **全部拒绝**。

**影响面 = 生产**：`LocalPlayerDataStore.SaveDataToFile / ReadDataFromFile / Delete` 三条链一条都不通（`LocalPlayerData.cs:328 / 373 / 384`），`GameStaticData` 里那 5 格（`:226-252`）因此从来没被持久化过。**这不是测试问题**。

**最小改动（落盘判据：`SaveKeys.cs:307` `OursPrefix` + `:337` 的 doc）**

```csharp
public const string SaveKey = SaveKeys.OursPrefix + "LocalPlayerData";   // = "_ours_LocalPlayerData"
```

- 顺带改 `LocalPlayerData.cs:141` 那句注释（它讲的是「别并进原版 51 条键表」—— 仍然正确，但要补一句「**必须带 `_ours_` 前缀**，否则 `SaveCenter` 拒绝」）。
- 影响面：文件名从 `LocalPlayerData.json` 变成 `_ours_LocalPlayerData.json`。**没有迁移负担** —— 旧 key 从来没写成功过，盘上不会有那份文件（`SaveCenter.cs:100` `key + ".json"`）。

### R3（δ，4 条）· X2 夹具：「平行射线」在浮点下会命中 4700 万单位外

**机理**：`Quaternion.Euler(-90,0,0)` 的 forward 是 `(0, 0.99999996, ±2e-8)`（float 精度）。敌兵平面法线 `(0,0,-1)` ⇒ `vdot = −dir.z ≈ ±8e-8`。Unity `Plane.Raycast` 的闸是 `vdot == 0`（精确零），**不是** Approximately ⇒ `enter = 4 / 8e-8 = 4.7e7 > 0` ⇒ 判命中。实得 `(0, 47171730, −4)` 里 `z = −4 = enemyRowZ` 正是这次命中的指纹。

**夹具改法**（B/C 两段都改，两段是复制关系）

```csharp
// 原：
//   hc.transform.position = new Vector3(0f, 5f, 0f);
//   hc.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // forward = +Y
// 改：forward = (0, +1, +1)/√2 —— 两条都**结构性**打空，不靠浮点巧合
hc.transform.position = new Vector3(0f, 5f, 0f);
hc.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 1f).normalized);
```

- 地面平面：origin `y=5` 在地面之上、`dir.y = 0.707 > 0` ⇒ `enter < 0` ⇒ `Raycast` 返 false（**不需要**靠精度）。
- 敌兵平面：`dir.z = +0.707` ⇒ `vdot = −0.707 < 0` ⇒ `enter = 4 / (−0.707) < 0` ⇒ false。
- 改完 B1（`AimMisses +1`）/ B2（落点 `Vector3.zero`）/ B3（`LastAimOnPlane = false`）/ C4 四条应一起转绿；C3/C5 不受影响（C5 现在靠「相距 47171730」过关、改完是「相距 ~5」也过关）。
- ⚠️ 顺便：B 段的注释「射线与法线垂直 ⇒ 平行 ⇒ 打空」要改成上面这条，否则同一个坑会被下一个人再踩。
- ⚠️ 被测实现 `TargetReticle.ResolveAim`（`Battle/TargetReticle.cs:511-547`）**是对的，别动** —— `best == float.MaxValue` 那条支就是原版的 `fVar9 <= fVar10` 双哨兵。

### R4（α，2 条）· Y5 D14/D15 的取样点跨过了循环回绕点

**时序（全部可在源码核）**

| 时刻 | `_ptrPhase` / `_ptrPhaseT` | 位置 | 出处 |
|---|---|---|---|
| `SetPointer(true, from, to)` | 0 / 0 | `GetAdjustedPos(from)` | `TutorialOverlay.cs:1339-1340` |
| `Advance(0.55)` | 1 / ≈0.05 | 起点 | `:1379` 需 `≥ 0.5` |
| `Advance(0.30)` | **2 / ≈0.09** | `lerp(t≈0.13)`（D13 实测离起点 0.144） | `:1404-1406` 需 `≥ 0.25` |
| `Advance(0.72)` | 0.09 + 0.72 = **0.81 ≥ 0.8** ⇒ **回首** | 被 `ShowStartingPointer` 写回**起点** | `:1413-1415` |

**夹具改法（推荐：在相位 2 内采样，别赌一个固定步长）**

```csharp
Vector3 best = ov.PointerWorldPosForTest;
float bestD = float.MaxValue;
for (int i = 0; i < 80 && ov.PointerPhaseForTest == 2; i++)
{
    ov.TickPointerForTest(0.01f);
    var p = ov.PointerWorldPosForTest;
    float d = Vector2.Distance(new Vector2(p.x, p.y), new Vector2(adjTo.x, adjTo.y));
    if (d < bestD) { bestD = d; best = p; }
}
NearXY(best, adjTo.x, adjTo.y, "D14 …拖拽过程中指针**到达过** GetAdjustedPos(to)");
Tr(Mathf.Abs(best.x - adjFrom.x) > 0.5f, "D15 灭自证：终点确实离开起点");
```

（若坚持固定步长：从 `_ptrPhaseT ≈ 0.09` 起，安全窗口是 `advance ∈ (0.61, 0.70)`。⛔ 别用 `0.72`。）

**⛔ 别按 D14 的文案去改实现** —— 「单点口那版没有终点」这个自诊断是错的（D4/D6/D7 证明双点口是好的）。

**顺带**：`TutorialOverlay` 没有暴露 `PointerPhaseT`，所以「赌步长」这条路既脆又难查；建议一并加一个 `PointerPhaseTForTest` 只读口（类尾追加，零行号漂移）。

---

## §四 判不了的（差什么）

1. **`X1 F1/F2/F3` 的确切机理：`Time.deltaTime` 在编辑模式批处理里到底给什么值 —— 判不了（⛔ 不跑 Unity）。**
   我把它判 δ 的推理链是：`ArenaIconRotation.Update` 只有一条路能让 `transform.Rotate` 不执行 —— `updateRate(0) <= accumulator` 为假 ⇒ `accumulator < 0` ⇒ `Time.deltaTime < 0`（若 deltaTime 恰为 0，`0 <= 0` 反而**会**转，F1 就该绿）。**这条推理没法在不跑 Unity 的前提下证死**（差一个 `Debug.Log(Time.deltaTime)` 探针）。
   ⇒ 修法不受影响（注入 dt 后与宿主无关），但**如果**注入后 F1 仍红，那就说明 `Update` 体压根没被执行到 —— 那是另一条路（届时先查 `AddComponent` 后的 `enabled` / 场景归属）。

2. **`B18 C0` 修好之后 C1–C6 会怎样 —— 判不了。**
   C0 是**硬早退**（`if (!hasMenu) return;`），所以 C1–C6 **从来没有执行过**。它们断的是「左竖导航三颗 + 排位模式卡已被 `MenuUniqueIds` 登记」，取决于 `MainMenuScene.RunCore` 那一刻是否已经建出导航栏与模式卡 —— 判据在 `MainMenuRuntime.BuildNavigationPanel` / `BuildModeCard` 的调用时机，而这要看宿主当时的建树进度（`MainMenuScene.cs:18447` 前后）。**建议单独安排**：改完 C0 先只跑 `MainMenuScene.Run` 看这 6 条的真身，别把 C0 的修复和 C1–C6 的返工当同一轮。
   ⚠️ 另注：`AllUniqueIds` 里有一条 `CardCollectionReferenceCardPointer` 是**注释里就写了「⛔ 本仓没有这颗节点」**的（`B94B18_Assert.cs:74`）—— 若它也在生产注册点的断言里被要求非空，注定红。

3. **`B7` 修好 key 之后是否还有第二层问题 —— 只能判断到「现在全部红都是同一个 key 闸」这一层。**
   4 条红的**全部** `SaveCenter` 输出都是同一条 `不在键表 ⇒ 拒绝`，改完 key 后要重跑才能看 `BadVersion` 闸与「人话」是否真的成立。

4. **`B88` 与 `RuleEngineTest.TestTacticOathChoice` 的交叉影响 —— 本会话没给我 `RuleEngineTest` 的日志**，我只能按代码断定它会红（`RuleEngineTest.cs:7788/7804`）。**复跑清单里务必加上它**。

---

## 附：两条「顺手发现」（⛔ 本代理没改，只报）

1. **`B93S4Assert.cs` 的文件名与 sink 前缀不一致**：文件叫 `B93S4`，`_sink` 打的是 `[B94S4]`（`:36`），而调度台清单里写的是 `B93S4Assert`。不影响正确性，但按前缀 grep 日志会对不上名 —— 建议统一（**改 sink 字符串会动行数不影响账**，但要先确认没人按 `[B94S4]` grep 台账）。
2. **`BadgeWiring.RegisterDefaults()` 是**幂等**且**全局**的**（`BadgeNotifications.cs:150` 一族）：任何一条断言只要调过它，`_notifications` 里就会同时有 Missions / Campaign / Inbox / Forge 四家的条目。⇒ 以后写「灭自证：拿**别的**定义去数 ⇒ 0」这类断言，**必须先确认那个「别的类型」没有 provider**（R7 就是这么踩的）。**唯一安全写法 = 自造一个没有 provider 的私有类型。**
