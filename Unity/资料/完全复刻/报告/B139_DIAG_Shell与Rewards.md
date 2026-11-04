# B139 诊断 · ShellScene 18 + RewardsScene 16

只读诊断，未改任何代码。日志 = `_tmp_view/unity_b139_ShellScene.log`（合計 6224 / 18）· `_tmp_view/unity_b139_RewardsScene.log`（合計 3137 / 16）。

## §0 一句话 + 按根因分组

**34 条红里没有一条是本批（B139）引入的** —— 全部可归到 **三个根因**，三个都在 **上一会话 `81ea4fbb`（第一百二十五会话，2026-11-04 00:01）** 里：
那道提交改了「双写开关默认值」「连登奖励表」「存档根」，**同一批没同步改断言**。

| 根因 | 条数 | 分布 | 类 | 引入者 |
|---|---|---|---|---|
| **R1 双写开关默认关**（`LegacyMirrorOn` 由 `true` 改成环境变量驱动）⇒ 旧档不再写 | **21** | Shell B116W21 6 · Shell B117F4 10 · Rewards B116W16 5 | **α**（期望过期） | `81ea4fbb` |
| **R2 连登表重排**：卡包格从旧第 1/4 格挪到**第 7 格（index 6）** | **10** | Rewards C3 D2–D11 | **δ**（夹具前提不成立，D2 为根，其余级联） | `81ea4fbb` |
| **R3 连登数量表改值**：第 1 格与第 3 格现在都是 150 | **1** | Rewards B110M2F C4 | **α**（灭自证前提过期） | `81ea4fbb` |
| **R4 存档根迁到 `C:\warpforgesave`**：卡组库在新根是空表 ⇒ `Select(-1)` 不再「越界」 | **2** | Shell P26 P52a/P52b | **δ**（夹具前提不成立） | `81ea4fbb` |

（R1 的 21 条全是 **同一根因的连锁**：旧档没写出来 ⇒ A2 三条 + A3 + 两条「中央档没了回落旧档」逐条倒。）

## §1 ShellScene（18 条）

| 组 | 条数 | 类 | 根因 | 证据 | 本批引入 | 怎么修（方向） |
|---|---|---|---|---|---|---|
| B116W21 | 6 | α | 夹具没打开双写 ⇒ 旧档 `pass/pass.json` 根本没写 ⇒ A2 / A2' / A2'' / A3 全假；B 段撤登记后 C1 / C1' 靠「中央档删掉回落旧档」也读到出厂值 | `Shell/SaveCenter.cs:915`（现读 = 环境变量驱动，`WF_LEGACY_MIRROR` 空 ⇒ false）· `:354` 写口 `if (LegacyMirrorOn)` · `Editor/B116W21_Assert.cs:101,104,106,108,125,126` · 日志 `:1030910` 只打了「已登记双写」，`:1031006` 起连红 | 否（`81ea4fbb`） | 夹具显式 `SaveCenter.LegacyMirrorOn = true`（并 `finally` 还原）—— 与 `Editor/B102DECK_Assert.cs:132` 现成写法一致 |
| B117F4 | 10 | α | 同上，两条通道（卡牌等级 `_ours_CardProgress` / 轮抽 `ActiveEvent`）各 5 条 | `Editor/B117_F4_Assert.cs:174,175,177,187,194,233,234,235,246,254` · 日志 `:1032297 / :1032814 / :1033081` 登记成功但没写盘 | 否 | 同上 |
| P26 | 2 | δ | 夹具假设「`CollectionData.Select(-1)` **必然**越界」。现卡组库读到的是**新建的空库**（`current:-1`、`decks:[]`）⇒ `Lib.CurrentIndex == -1 == i` ⇒ 判成功、不报「越界」（P52a 红），并走到 `Lib.Save()` 成功后关窗（P52b 红） | `Editor/B104_P26_Assert.cs:222-235` · `Shell/CollectionData.cs:151,153`（`Lib.CurrentIndex != i` 才报错）· 盘上现读 `C:\warpforgesave\WarpforgeDecks.json` = `{"version":1,"current":-1,"decks":[]}`（mtime 12:32 = 本宿主运行时刻）；旧根 `%LocalLow%/QJHWarpforge/WarpforgeReplica/WarpforgeDecks.json` 是 `current 1 / decks 2` | 否（`81ea4fbb` 存档根迁移） | 夹具自备 2 套卡组（临时 `DeckStore.OverridePath`）再断越界；⛔ 别依赖环境里恰好有玩家卡组 |

## §2 RewardsScene（16 条）

| 组 | 条数 | 类 | 根因 | 证据 | 本批引入 | 怎么修（方向） |
|---|---|---|---|---|---|---|
| C3 | 10 | δ | 卡包格现在在**第 7 格（index 6）**。连登轨 7 格、视口 x∈[0,1920]，该格左沿 = **2443.83**（`2 + 5×391.779 + 482.935`；右沿 2899.61 = `TrackContentW` 对得上）⇒ 整格在视口外 ⇒ `MenuDraw.Hit` 的 `ClipRect` 判不建 ⇒ 命中件 null ⇒ D2 红，D3–D11 全部级联（含两条「灭自证」也读同一个空件） | `Shell/DailyData.cs:1109-1112`（`_streakIcon[6]` 才是 `40k_main_bt_rewards`）· `Shell/DailyStreakPopup.cs:1021,1024`（`AddHit` 返回 null 即不建）· `Shell/MenuDraw.cs:2235`（`if (!ClipRect(...)) return null;`）· `Shell/GameWindow.cs:326` · `FocusCurrentDay` 在 `StreakCollected=5` 时**早退不滚**（`DailyStreakPopup.cs:587`）· 日志 `:1205915` D1 ✓、`:1205932` D2 起连红 | 否（`81ea4fbb` 把卡包从旧第 1/4 格挪到第 7 格） | 夹具先 `_trackScroll` 滚到该格（或换一格**闸关且在视口内**的格子）再断；⛔ 不是相机问题 |
| B110M2F | 1 | α | C4 是「灭自证：两格数量**不同**」，现表第 1 格 150、第 3 格 150 ⇒ 前提不再成立（C1/C3 各自仍 ✓，说明取值链正常） | `Shell/DailyData.cs:1112` `_streakAmount = {150,75,150,...}`（`81ea4fbb` 由 `{1,150,20,...}` 改来）· `Editor/B110_M2F_Assert.cs:146` · 日志 `:1208103` 实得 `150 vs 150` | 否 | 换一对**数量确实不同**的格（如第 1 格 150 vs 第 2 格 75），或按表现值重写前提 |
| B116W16 | 5 | α | 同 R1：收件箱通道旧档 `inbox/inbox.json` 没写 ⇒ A2 / A2' / A2'' / A3 假；C1' 读旧档已读位拿不到 | `Editor/B116W16_Assert.cs:109,112,114,116,135` · 日志 `:1211211` 登记、`:1211308` 起连红 | 否 | 同 B116W21 |

## §3 两条假设的检验

**H1（B116W21 / B116W16 双写族是否过期）—— ✅ 成立（且不止这两族，B117F4 同病）。**
- 现读：`Shell/SaveCenter.cs:915` = `LegacyMirrorOn = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WF_LEGACY_MIRROR"))`；`81ea4fbb` 把 `= true` 改成了这一行（`git diff 81ea4fbb~1 81ea4fbb` 第 47/52 行）。用户 2026-11-03 裁定「默认关」，所以**实现是对的**，错的是断言。
- 三份夹具都**没有**显式打开开关（`B116W21_Assert.cs:60-101`、`B117_F4_Assert.cs:131-174`、`B116W16_Assert.cs:65-109` 全段搜索 `LegacyMirrorOn` = 0 命中），全靠旧默认值 true。
- 日志侧证：三处都打出了「已登记双写」但紧接着 A2 红 ⇒ 登记在、写口被开关挡住。
- 对照：`Editor/B102DECK_Assert.cs:132` 显式 `SaveCenter.LegacyMirrorOn = true`，所以它这一族不受影响。

**H2（C3 / B117F4 / P26 是否与相机有关）—— ❌ 不成立（三族都不是相机）。**
- `LayoutSpace.ResolveHudCamera()` 与 `LayoutSpace.Cam` 在三处都正常：两份日志里「取不到 HUD 相机」「切主菜单前取不到壳的 UI 相机」「场景里没有相机」**各 0 次**；Rewards 日志 `:1189733`、`:1195256` 两条前提断言都 ✓ 说 `LayoutSpace.Cam` 在。
- C3 是**几何**问题：`MenuDraw.Hit` 的 `ClipRect` 纯设计 px 比较，**不经过任何相机**（`Shell/MenuDraw.cs:2235`）；卡包格左沿 2443.83 远超视口右沿 1920。
- P26 是**存档库为空**（见 §1）。
- B117F4 是**旧档没写**（见 §1），与相机无关。
- ⚠️ 仍值得留意的次要面：`B139-D4` 的 26 处改造本身**没有在本批引入任何红**（没有任何「相机为 null」的出声行）。
- 计数侧证：Shell 本趟总数 **6,242**（= 6,224 + 18）比上一已知好读数 **6,232** 多 **10 条** ⇒ 那是 B139 新加的断言（相机那批），且**全绿**；Rewards 总数 **3,153**（= 3,137 + 16）与好读数**完全相同** ⇒ 16 条是「原本绿、现翻转」，**没有新增断言被写红**。

## §4 顺手发现

1. **自检宿主在往玩家真存档根写盘**：`C:\warpforgesave\WarpforgeDecks.json` 的 mtime = **12:32:20**，而 ShellScene 日志收尾 = 12:32:25（差 5 秒）⇒ 就是本趟 ShellScene 写的，内容是空库。`RuleEngine/Data/DeckStore.cs:293-306` 的写口**没有** `isBatchMode` 闸（与 `SaveCenter.StoreOn`（`SaveCenter.cs:92-95`）不同）⇒ 任何走到 `DeckLibrary.Save()` 的自检都会落到真根。真机玩家的卡组有被自检覆盖的风险，建议单独立账核（本次未改）。
2. **同一族断言还漏了一处**：`Editor/B117_F4_Assert.cs` 的 P 段只有 `A2` / `A2'` 两条，缺 `A2''`（「旧档不带 `_v`」），而 W21 / W16 两族都有三条 —— 修 R1 时顺手补齐，否则 P 段比 D 段少一条牙口。
3. **`81ea4fbb` 改数据表时没同步改断言的模式又出现了一次**（连登表改了名字/图标/数量、卡包换了格子，三份断言文件 0 改动）。这不是单点失误，是「改数据 → 断言跟着红」缺一道闸；建议把「改 `DailyData` 三张表 / `SaveCenter` 开关」列进派活简报的必查清单。
4. `C:\warpforgesave` 下同时有 `WarpforgeCardLevels.json`（80 KB，12:34）与 `WarpforgeRanked/`（12:33），也都是本批宿主跑出来的 —— 与第 1 条同源，一并核对是否该在批处理里被隔离到临时目录。
