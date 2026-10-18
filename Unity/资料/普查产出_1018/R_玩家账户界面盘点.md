# R · 玩家账户（左上角头像 → 玩家档案窗）界面盘点 · 2026-10-18

> 只读代理产出（无写文件工具 ⇒ 主对话原样落盘）。

## 1. 一句话结论

**做了一部分**：六页 UI 骨架、原版控件与本地清单（**469 头像 / 462 称号 / 102 成就**）都建齐且自检覆盖；
真正「生效」的只有**会话内**的改名、换头像、选称号三件 —— **没有存档**；两个 `Select` 钮是空壳，
资料页的称号行是死字，成就/段位/统计没有进度，多页是「只有 UI」。

## 2. 入口

- 顶栏 `Upper bar/Player Profile/Avatar Item Small/Border` → `TopBar.Parts.ProfileHit`（`Shell/TopBar.cs:260`）
  → `MainMenuRuntime.OpenProfile()`（`Shell/MainMenuRuntime.cs:1556`）→ `Shell/PlayerProfileWindow.cs`。
- 原版对应：**`Player Profile Window`**（`bundle_menus_assets_all`，类 `PlayerProfileMenu : GameWindowWithTabs`，
  `type=1 Popup / placement=15 / closeOnESC=1`）。
- 出厂落 **Title 页（第 3 键）**；六页在开窗时一次性全建（`BuildPages` :409-431）。

## 3. 六页表

| 页 | 建了没 | 主要控件 | 功能是否真生效 | 出处 |
|---|---|---|---|---|
| 1 `Profile`（资料） | 已建 | PlayerId、头像、改名钮、Player Level、Current/Highest Rank 两卡、Legendary（关）、Events 三格、内嵌 `ChooseNameWindow` | **部分**：改名真生效（会话内）；头像跟随选择刷新；等级/评分/段位/联盟/事件全是空态；`Player Title` 是硬编码占位；头像与 PlayerId 点击只打日志 | `Shell/ProfileTab.cs:376-384, 508, 563, 1050-1063, 1078-1089` |
| 2 `Avatar`（头像） | 已建 | Item Display Panel + 5 列 **469 格**、Selected Item Panel、`Select Avatar Button`、`Toggle borde` | **会话内生效 / 部分**：点格子真换（写 `ProfileData.AvatarIndex`，顶栏每帧跟着换）；`Select Avatar Button` **只打日志**；`Toggle borde` 只切网格格边框、不存档 | `Shell/AvatarTab.cs:322, 354-377, 405-410`；`TopBar.cs:459-467` |
| 3 `Title`（称号） | 已建 | 3 列 **462 格**、Selected Item Panel、`Select` 钮 | **会话内生效 / 部分**：点格子真选（战斗 HUD 名牌读它）；`Select` 钮**只打日志**；资料页 `Player Title` **不读它** | `Shell/TitleTab.cs:263, 296-331`；`Battle/BattleDriver.cs:6888-6891` |
| 4 `Battle Log`（对局记录） | 已建 | Matches 滚动区、行模板、自建「对局历史弹窗」入口 | **是（本地）**：结算处真写记录，打过的局会列出来；默认空、无空态文案 | `Shell/BattleLogTab.cs:80, 137-216`；`BattleLogData.cs` |
| 5 `Trophies`/成就 | 已建 | 4 分类页签、2 列网格、成就格、Counter | **部分**：**102 条全齐**、可筛可滚可列；**进度恒 0**（进度条不画）、计数条恒 0、**无领取交互**、格内无点击区 | `Shell/AchievementsMenu.cs:157-407`；数据 `ProfileData.cs:260-300` |
| 6 `Ranking`（段位） | 已建（骨架） | Top4 四格、Global Rating、Profile Player Info、AllFactions、封印 | **只有壳**：分数全 `------`、段位图/名次图不画；头像/名字/等级是真的 | `Shell/RankedTab.cs:149, 285-300, 361, 488-502` |

## 4. 用户点名的三项

### 4.1 切换头像
- 能选：点格子 → `ProfileData.AvatarIndex`（唯一一份）→ 大图/名/高亮立即刷新。
- 真换到哪：主菜单顶栏每帧比对换图；Profile / Ranking 页切页时重读。
- **数据写去哪 / 存档：只写内存，无存档** ⇒ 重开游戏回默认第 0 张。
- 缺口：`Select Avatar Button` 只打日志；`Toggle borde` 不存档。

### 4.2 称号（Title）
- 能选/显示：点 462 格之一 → `ProfileData.TitleIndex`；**战斗 HUD 名牌**读它（有自检）。
- 来源：`Resources/profile_cosmetics.json` 的 `titles` 462 条（官方 zh-CN 名命中 368/462）。
- 缺口：`Select` 钮只打日志；**资料页 `Player Title` 是硬编码字面量**，永远不显示所选称号；无存档。

### 4.3 成就记录
- 数据：**102 条齐**，自检逐条断；名字/描述官方词条命中 95/102。
- 界面：4 分类页签可切、2 列网格可滚、一屏只建可见格。
- **进度恒 0**：`CurrentValue` 是服务器存档，本地没有；格子按第 1 档渲染、进度条 Fill 不画、Counter 恒 0。
  **没有领取按钮** —— 原版 `AchievementContainer` 的字段也只有 title/description/reward/image/sliderBar/counter，
  无按钮 ⇒ **领取不是本仓漏做**。

## 5. 「只有 UI、点了不生效」清单

1. `Avatar` 页 `Select Avatar Button`：只 `Debug.Log`。
2. `Title` 页 `Select` 钮：只 `Debug.Log`。
3. `Profile` 页头像点击：只 `Debug.Log`，不切页（Ranking 页头像点击**会**切）。
4. `Profile` 页 `PlayerId` 点击：无 ID、无复制。
5. `Profile` 页 `Player Title`：死字面量。
6. **改完名后顶栏名字不刷新**（`TopBar` 只在建树时读一次）。
7. `Toggle borde`：出窗即丢，不存档。
8. `Player Level` 恒 `-`、两处 Rank 评分恒 `------`、Events 三格空、Legendary 整卡关。
9. Ranking 页分数全 `------`、段位图不画、Top4 不填。
10. 成就进度条/计数条恒 0、无领取、格不可点。
11. `Consecutive login days` / `Invite to alliance`：建了恒关。
12. **头像/称号/改名三者都只在本次会话有效，没有玩家存档。**

## 6. 没查清

- 原版两个 `Select` 钮的真实效果：`AvatarTab.OnChoseAvatar` / `TitleTab.OnSelect` 在本地反编译桩里**方法体为空**
  ⇒ 只能确认「原版有这两个入口」，具体是提交还是仅预览**查不到**。
- 原版成就是否有领取：桩里 `AchievementsMenu` / `AchievementContainer` 无 claim 字段，预制体未逐节点 dump
  ⇒ 倾向「原版也没有」，未 100% 证实。
- 顶栏名字改名后是否有别的重建路径会顺带刷新：未跑 Unity 实测。
- 头像框 Toggle 的原版语义（`FindActiveAvatarBorder` 桩返回 null）**查不到**。
- `StrangerProfilePage.cs`（61 行）本次只确认存在（整页占位），未逐项核。
