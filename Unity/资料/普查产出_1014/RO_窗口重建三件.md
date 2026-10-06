# RO · 窗口重建三件（A675 / A569 / A571）现状与判据

> ⚠️ **本文件由主对话代录**：跑这一趟的是**只读代理**（无 Write 工具）⇒ 它把全文写在交件消息里，**由主对话原样落盘**（内容未改，只做了排版）。
> **读到的是 2026-10-06 13:50 的工作区**（当时 `git status` 有 21 个 `.cs` 是 `M`，含全部 7 个窗口宿主）⇒ **每个行号都配锚点，引用前按锚点 grep 一次**。
> 快照 md5：`ShellScene 30d0fc55…` · `MainMenuScene 75525280…` · `RewardsScene a24dcf76…` · `CollectionScene 12842bd6…` · `SettingsScene 2d324cbb…` · `ShopScene 2d361bf6…` · `DeckScene 59a0bea0…`
> 判据源（现读）：`批次计划_1013.md` §六·B §A675/A569/A571 · `WA505_同实例重开普查.md` · `WA569_跨窗重建普查.md` · `已知的坑.md:4109-4113` · `D:/tmp/wf569/scan8.py` 全文。

---

## §A675 ·「窗内子树重建」族

### ① 现状
**(a) 定义侧**（`Shell/*.cs`，剥注释正则 `(public|protected|internal|private) [static] [override] void (Build|Refresh*|Rebuild*)(`）= **85 个**。
最密：`CollectionWindow.cs` 7 · `ChatPanel.cs` 6 · `AllianceMemberTab.cs` 5 · `CampaignTab.cs` 3 · `ProfileTab.cs` 3 · `RankedTab.cs` 3 · `RewardsWindow.cs` 3 · `SocialWindow.cs` 3 · 其余 20 个文件各 1–2。

**(b) 触发面** = `Open()`/`OnOpen()` 体内直接调本族 = **39 个类**。
⚠️ 这批里有一部分与「跨窗重建」在 `OpenByState` 的 `Closed` 支上叠；**本族还有独立的一半**：**不经 `OpenByState`**、由**窗内逻辑/生产回调/夹具**主动调 —— **这一半才是 A675 要查的**。

**(c) 生产侧「谁会调它」跨对象调用全族只有 4 条**：
| 调用点 | 触发链 |
|---|---|
| `Shell/DailyData.cs` `dw.Build()` / `sw.Build()`（在 `RefreshOpenDailyWindows` 里） | ← `RewardWindow.ShowCollected(rewards, onClose)`（关窗先发 `OnClose`）⇒ 🔴 **这正是 A671 的机制**（WA569 记在跨窗栏，其实是本族） |
| `Shell/CardDetailPopup.cs` `Build()`（`ShowCard(card)` 内，自带注释「换卡靠这一句，⛔ 不是靠『再开一次会重建』」） | 唯一「点一下就整窗重建」的生产入口；夹具在 `Editor/CollectionScene.cs` 有 7 处 `cd.ShowCard(...)` |
| `Shell/AlliancesTab.cs` `Search.Build()` / `Member.Build()`（`Setup()` 建窗时一次） | 建完不再重建 |
| `Shell/SearchingMatchPopup.cs` `p.Build()`（`Attach()` 建时一次） | 同上 |

⇒ **生产侧「别人把你整窗重建」= 2 条链**：`RefreshOpenDailyWindows`、`CardDetailPopup.ShowCard`。

**(d) 夹具侧**（`Editor/` 7 个窗口宿主，剥注释）= **129 处**：`RewardsScene` **59** · `MainMenuScene` **40** · `ShellScene` **11** · `CollectionScene` **7** · `DeckScene` **6** · `SettingsScene` **3** · `ShopScene` **3**。
⚠️ **别把战斗侧混进来**：`Editor/BattleScene.cs` 的 `driver.RefreshAll()` **91 处** + `hand.Refresh(...)` 属**战斗视图**族，与窗口无关。

**(e) 本族真踩过的已知实例（8 条）**：`RewardsScene` 的 `boxA` 被 `mt389.Build()` 冲掉（**曾恒红**，#52 已修）· `RewardsScene` 明写「本夹具不重建 MissionsTab」以保住 `rows[0]` · `RewardsScene` 注释「重建后旧 `Transform` 是已销毁对象」· `MainMenuScene` 的 `LiveOpsEventWindow.Build()` 整棵 `DestroyImmediate` · `MainMenuScene` 「主菜单重建 ⇒ 下面一律重新找」· `SettingsScene` 的失败文案专门区分「没建」vs「重建后的假 null」· `ShellScene` 断言点名「第二次 `Build()` 之后指向已销毁的组件」· `RewardsScene` 的**正面样板**（`Build()` 之后按名重收 `rows`）。
⇒ **1 条曾恒红（#52）+ 1 条曾静默假绿（A671）** —— 这一族**不是假想**。

**(f) 机械扫描（只读、未落盘）**：7 个宿主 × 「本族调用点 × 此前抓/此后用的节点句柄」⇒ **20 个候选**（RewardsScene 9 · MainMenuScene 6 · CollectionScene 2 · SettingsScene 2 · ShellScene 1）。**逐条读了其中 9 条，全是假阳/结构性免疫**（`RebuildDeckCells` 显式保留 `Viewport` · `RebuildGfxRows` 只清子件 · `ClearTrackContent` 清的是子件 · `RefreshTopAvatarIfChanged` 只 `SetTexture` …）。
⚠️ **扫描能查出一批，但会漏掉 A671 那一类** —— 它的 `Build()` 在**生产回调**里，夹具侧看不见。**这是「必须单独普查」的最强理由。** ⚠️ **20 个候选只逐条读了 9 条**，其余按「同名/已重取」归为假阳、**未逐条读**（如实记）。

### ② 最小改法（**本件结论 = 先别改代码**）
若普查确认某处，只有两种形状：**(a) 现取**（重建之后 `FindChild` 重取；样板 `RewardsScene` 三处）· **(b) 前提出声**（`CheckTrue(x != null, "（前提）…")`；样板 `SettingsScene` 的文案分档 + A671 的改法）。
⛔ **不许**动生产侧让 `Build()` 幂等/延迟（`RefreshOpenDailyWindows` 与 `CardDetailPopup.ShowCard` 都是**照原版的实现**）。
⛔ 6 处「故意」家族（`ShellScene 2015/2035/3316/3363` · `CollectionScene 5054/5103`）**别去「修」** —— 它们的断言就靠「重建前后不是同一个节点」证伪实现。

### ③ 判据齐不齐 —— **不齐**，还缺 4 样
① 「哪些算真风险」的标准没写死；② 扫描口径没定（**且必须并上生产侧那 2 条链**）；③ 计数口径要对齐 A676（**别把 129 当 129 个 bug**）：分类至少要有「真缺陷 / 故意 / 结构性免疫 / 假阳 / 今天安全但脆」；④ 「重建后现取」没有统一写法（`RewardsScene` 里就有 3 种）。
⇒ **建议单开 1 件只读**（判据补齐后再派写手）。

### ④ 派活
**先 0 个写手**；派 **1 个只读**。若非要一次到位：最大头 `Editor/RewardsScene.cs`（59 处）**必须独占**，次之 `Editor/MainMenuScene.cs`（40 处）。

---

## §A569 ·「跨窗重建」普查 —— **已做过，且比「WA505 覆盖」更彻底**

### ① 现状
**`资料/普查产出_1013/WA569_跨窗重建普查.md`**（219 行）才是覆盖它的那份：
- 扫描范围 = `Editor/*.cs` **全部**；**触发集**（脚本 `D:/tmp/wf569/scan8.py`，**不在工程里**）= `CloseAllWindows()` · `HideAllWindows()` · `OpenWindow(v)` · `v.TryOpen(...)` · `v.Close()` + 同文件内「体内含上述操作的助手」按方法名一层内联；**`.Build()` 族明确排除**。
- 结果：**264 条窗口级操作行 / 1900 个句柄捕获 / 24 个跨窗站点** —— **1 真缺陷**（= **A671**）· 6「故意」· 5 结构性免疫/假阳 · 1 已修 · 9「今天安全但脆」· 2 不受影响。
- **触发集口径已由 A674 订正**：不是那 8 类全屏窗，是「**任何** `OpenWindow/TryOpen/.Close()/CloseAllWindows`」；**真正的杀句柄那一拍 = 「某扇 `Closed` 的窗被 `TryOpen`」**。
- ⚠️ **`WA505` 只报了 1 例**并自陈「本件只报不算」⇒ **单看 WA505 会误判成「A569 没做」**（`清单_A表全量.md` 那句「疑似已由 WA505 覆盖」就踩在这个误读上 —— **真正覆盖它的是 `WA569`**）。
- ⚠️ **行号已漂**：`WA569` 表内 `MainMenuScene.cs:5211`（R9）现读 **`:5455`**；`ShellScene.cs:3184`（R3）未漂。

### ②③ 剩余清单 = **4 条待查（S1–S4）+ 1 条可选加固（S5）**
| # | 缺什么 | 站点（行号 + 锚点） | 现核到哪一步 |
|---|---|---|---|
| **S1** | **`ShowPopUp` / `ShowMessagePopUp` 这条开窗路不在扫描触发集里**（`scan8.py` 的 hot-helper 是同文件内建的 ⇒ **Shell 侧助手永远进不来**）；而 `ShowPopUp` → `ShowMessagePopUp` → `OpenWindow` → `PopUpGameWindow.Open()` → **`Build()`** 是**真的重建** | 夹具 5 处（锚点 `shell.Windows.ShowPopUp(` / `wm.ShowMessagePopUp(`） | ✅ **现读核过 5 处：今天安全**（都从 `popUpWindow` 重取 / 用的是窗对象 / 按名现找）⚠️ **但没有断言守着「必须重取」** |
| **S2** | **R3 下游未查清**：`WindowButton.onClick` 是**裸字段** ⇒ 解引用不抛、委托照旧被调；但「**委托体碰到已销毁对象会怎样**」（抛/静默/居然还能开出来）没读 | `Editor/ShellScene.cs`（锚点 `var logBtn = logHitN != null ? logHitN.GetComponent<WindowButton>() : null;`） | ❌ 要读 `Shell/BattleLogTab.cs` 的 `OpenPopup` 捕获了哪些对象 |
| **S3** | **R9 下游未查清**：`FindChild(nmv, "List View")` 之后那条链（`FindChild(null,…)` 之后是**红还是静默**）没逐条追 | `Editor/MainMenuScene.cs`（锚点 `var lv = FindChild(nmv, "List View");`） | ❌ 未查 |
| **S4** | **`ShopScene` 的 6 个调用点没人工复核**：助手 `ClosePackAndReopenShop`（锚点 `ClosePackAndReopenShop(win);`）⇒ 扫描在那 6 点**零命中** ⇒ 「真安全」与「句柄判据在这一族不灵」**两者没分辨** | `Editor/ShopScene.cs`（定义 1 处 + 6 个调用点） | ❌ 未复核 |
| **S5** | （**可选加固，不是缺陷**）WA569 那 9 处「今天安全但脆」**都没有出声**：谁在那两句之间插一条断言，就会**红或静默** | 同上 | 加固形状 = 「①重抓句柄 ②抓不到**出声**」 |

### ④ 派活
**S2 / S3 = 只读判定**（各 1 人）· **S4 = 只读复核** · **S1 若要补出声 = 写手**（独占 `Editor/ShellScene.cs` + `Editor/DeckScene.cs`）⇒ **建议 2 只读 + 1 写手**。⚠️ `Editor/ShellScene.cs` 与 A571、A675 都撞 ⇒ **排一条串行队列**。

---

## §A571 · `ShopWindow` 重开隐含前提的断言

### ① 现状
- **目标行** = 助手 `ClosePackAndReopenShop(ShopWindow win)` 体内最后一句：`if (win.CurrentState == WindowState.Closed && win.Manager != null) win.Manager.OpenWindow(win);`（⚠️ **WA505 记的 `:1039` 已漂**；调用点 6 个）。
- **前提现核（比 WA505 更进一步）**：`Shell/ShopWindow.cs` 对 `Data` 的引用 = **0**；也**不覆写** `SetupData`（全库只有 3 处覆写：`CampaignRewardWindow` · `MissionRerollPopup` · `RewardWindow`）。`Open()` = `Build(); BuildPages(); tabButtons.Click(0); RefreshHighlights();` ⇒ **「`ShopWindow.Open()` 不读 `Data`」成立**。
- 🔴 **「同族 12 处」复算不出来**。可复算的是 `Editor/*.cs` 的 `TryOpen(null)` = **27 处** = **已有断言的 9 处**（全 `CheckTrue(x.TryOpen(null), "（A94 收尾）…")`）+ **裸调（静默）的 18 处**（`CollectionScene` 4 · `SettingsScene` 7 · `ShellScene` 4 · `ShopScene` 3）。
- **另一层前提（WA505 没写）**：**带参** `TryOpen(null)` 会**先** `SetupData(null)` ⇒ 对**每一扇**窗都把 `Data` 清空，3 个覆写窗还会连带清 `_ctx`/`Index`。27 处里落在「真读 `Data`/覆写」的窗上**只有 1 处**（`RewardsScene` 的 `cw`）⇒ **已由 A570 立账**。

### ② 最小改法（3 选 1，全零行为变化）
**(a)** 那 1 处助手 → 补一行 `CheckTrue(win.CurrentState == WindowState.Open, "（前提）商店重开之后真的开着");`
**(b)** 18 处静默 `x.TryOpen(null);` → 逐处 `CheckTrue(x.TryOpen(null), "（前提）重开之后窗真的开着");` —— ⚠️ **写法照 A94 全族那 9 处**，别发明新句式。
**(c)** 可选、**建议不做**（那是**新判据**不是前提守护，越界）。
⛔ **别**把 `TryOpen(null)` 改成无参 `TryOpen()`。

### ③ 判据齐不齐 —— **齐**（唯一要订正的是「12 处」→「**27 处 = 9 已断 + 18 裸调**」）。

### ④ 派活
**1 个写手**足够（19 行断言）。**独占**：`Editor/ShopScene.cs` · `Editor/SettingsScene.cs` · `Editor/ShellScene.cs` · `Editor/CollectionScene.cs`。`Editor/MainMenuScene.cs` 那 6 处**已断，不用派**。
⚠️ 与 A569·S1、A675 **撞 `ShellScene.cs`** ⇒ **排串行**；另三个文件互不重叠。

---

## 附 · 诚实边界（原报告自陈）
- **搜过哪儿**：`Editor/*.cs` 全量（grep + 剥注释/字符串的两轮扫描）· `Shell/*.cs` 的 `Build/Refresh*/Rebuild*` 定义与调用点、`SetupData`/`Open()` 覆写全表 · `scan8.py` 全文 · `WA505`/`WA569` 全文 · §六·B 相关行 · `已知的坑.md:4109-4113` · `清单_A表全量.md` 里那三行。
- **没查 / 查不到**：Unity 没跑 ⇒ 全部是**静态判定** · S2 的 `BattleLogTab.OpenPopup` 没读 · S3 的下游没逐条追 · S4 的 6 个调用点没人工复核 · **A675 的 20 个候选只逐条读了 9 条** · `WA569` 表内 24 行的行号没逐行重取（只重取了 R3/R9）。
