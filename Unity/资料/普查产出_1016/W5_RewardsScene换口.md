# W5 · `Editor/RewardsScene.cs`（A796′ 换口 · A796 三处判断 · A799 夹具 1 处）

> 「清空 A 表」第七轮 · **2026-10-16** · 执行写手 W5
> 白名单 = **只** `Editor/RewardsScene.cs`。⛔ 本件：没跑 Unity · 没动 git · 没改正本 · 没碰任何别的文件。
> 验证：`TMPDIR=/tmp/wf_w5 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
> 行尾：改前 `CRLF 0 / LF 10366` → 改后 `CRLF 0 / LF 10439`（**纯 LF，没翻**）；`git diff --numstat` = `288 36`
> （其中 A815 与 A826 的**既有**段 = `208 29`，本件净增 `80 7`）。
> ⚠️ 行号都会漂：**动手前先 `git diff` 看现状**（本文件刚被 A815/A826 改过，那两段的区域**一个字没动**）。

---

## ① 结论（三笔）

| # | 账 | 结论 |
|---|---|---|
| 1 | **A796′ 换口** | ✅ **做完**（`RectOf` label 支 + `TextLeftPx`/`TextRightPx`）。 |
| 2 | **A796 剩 3 处** | 领奖窗 = **接**（已落）· 重摇任务窗 = **接**（已落）· 每日连登窗 = **不接**（理由 §③·2）。 |
| 3 | **A799 夹具 1 处** | ⛔ **本文件里没有这一处**（证据 §③·3）—— 主判据的 15 处里，`Editor/` 侧 6 处**全在 `MainMenuScene.cs`**。 |

---

## ② 改动清单（逐处 `文件:行号` —— 行号 = **改后**工作区）

### 2·1 A796′（三处，都在**定义**上；42 + 17 个调用点由定义一处覆盖）

| 处 | 位置 | 改法 |
|---|---|---|
| 新增 helper | `RewardsScene.cs:337-355`（`LabelRenderedWH`，doc 在 `:337-346`） | 宽/高 = **TMP 自己渲出来那块 `textBounds`**（复用本文件既有的 `TmpRenderedRect`，`:217`）；量不到（点阵后端 / 底下没有 TMP 网格）⇒ **退回旧口** `Label.WorldW/H` 并返回 `false`（**那条路本来就没有 `textBounds`** —— 点阵支读 `_texW`，不是「静默吞掉新口」）。 |
| `RectOf` **label 支** | `:463`（定义）· 改的那一句 = `:471` | `w = lb.WorldW*108f; h = lb.WorldH*108f;` → `LabelRenderedWH(lb, out w, out h)`。**图那一支（`q != null`）一字未动**；`node = lb.transform`、`cx/cy = PxOf/PxYOf(node.position)` **一字未动**。 |
| `TextLeftPx` / `TextRightPx` | `:363-375`（定义） | `∓ lb.WorldW*108f*0.5f` → `∓ w*0.5f`，`w` 来自 `LabelRenderedWH`。**中心仍是节点位置**。 |

- 🔴 **只换「从哪个口读那个数」**（照 `Editor/ShopScene.cs` 的同族先例）：两处都**没有**改成直接用 `textBounds` 那块矩形的两条边 —— 那会把父链缩放 + TMP 子节点上 `_vOffset` / `(0.5 − anchor)` 两项一起算进去，期望值会整体漂（W4 的落地注释写了同一条，`Shell/` 侧先例 `MenuDraw.CheckShadeClickRule` 上面那段也一样）。
- ⛔ **没有换成「TMP 网格顶点」那条口**（`TmpVertPx`）—— 判据 = RO §一·3②（首字左边距 2.22px）。
- ✅ **覆盖数逐处现数**：`RectOf(` 引用 **42** 行（41 调用 + `RectOfUnion` 内部转发 1，`:520`）· `TextLeftPx|TextRightPx` 调用**出现 17 次** ⇒ 与 `RO_缓存口径与输入三件.md` §一 的 **42 / 17 逐位吻合**。

### 2·2 A796 两处新接（每处 3 行：现场重开 + 公共口）

| 处 | 位置 | 内容 |
|---|---|---|
| 领奖窗 `rw` | `:6490-6510`（`Check(rw.CurrentState, Closed)` 之后、`⛔ 不许再写 CloseAllWindows` 那段注释之前） | `rw.TryOpen()`（**无参**）+ `MenuDraw.CheckShadeClickRule(CheckTrue, "领奖窗", rw.transform, FindChild(rw.transform,"BackgroundHit"), () => rw.CurrentState)`；注释里写全了「W7 §四·1 那两个未查实项**已查实**」与终态不变。 |
| 重摇任务窗 `popA` | `:9713-9733`（§⑨ `Check(rwA.CurrentTab, …Missions…)` 之后、① 之前） | `popA.TryOpen()`（无参）+ 同一个口（`FindChild(popA.transform,"CloseHit")`）；注释写明「`Close()` 那条副作用链在本落点**是零副作用**」的三条判据。 |

---

## ③ A796 三处：逐处判断

### 3·1 领奖窗 `rw` —— **接**（已落 `:6508-6510`）

W7 §四·1 点名的两条「本件没查实」，**逐条读码查实**（不是推的）：

1. **`_ctx` 不会被清** —— 这里走的是**无参** `TryOpen()`（`Shell/WindowsManager.cs:496-499`：只 `OpenByState()`，**一次都不调 `SetupData`**）⇒ `Build()` 拿到的仍是 ⑨ 段灌进去那个 `_ctx`。
2. **即使 `_ctx == null` 也不炸** —— `RewardWindow.Build()` 第一句就是 `var ctx = _ctx ?? new RewardWindowContext { Rewards = new RewardSpec[0] }`（`Shell/RewardWindow.cs:1044`）。
3. **「重发 `OnClose`」这条**：`Close()` 是「**每关一次发一次**」（`Shell/RewardWindow.cs:864-869` 头一句 `cb = _ctx?.OnClose`）⇒ 语义自洽，不是「一次关窗发两次」；那条 `Check(closeCalls, 1, …)`（`:6499`）在**本块之前** ⇒ 照旧绿；本块之后 `closeCalls` 会是 2，而**它是本段 `{ }` 里的局部量、后面零引用**（`grep` 核过：`rw` 在 `:6530` 之后**零引用**）。
4. **终态不变** ⇒ 后面 `CollectDaily(0)` / `OpenRewardWindowCount() == 0`（`:6545-6551`）吃的前提一个字没动。
5. **站立点**：本窗原有一条**等价**站立点（`:6507` 那条 `CheckAbsorbRule` 的末步 = 真指针点击 + 上一步断「命中的就是本窗压暗层那一颗」）；本块的**增量** = 「点之前必须是 `Open`」（负向态对照）+ 「那颗不是吸收层」（结构上不可能与行为断言同时满足，= 灭自证那一半）。

### 3·2 每日连登窗 `ds` —— **不接**（一句没动）

本窗压暗层的动作**不是裸 `Close()`**：`DailyData.StreakAutoCollect(); Close();`（`Shell/DailyStreakPopup.cs:273-274`）。
而 `StreakAutoCollect()` 是**真收**（`Shell/DailyData.cs:1050-1059`）⇒ 转调 `CollectStreak`（`:990-998`）：

- **置 `_streakClaimed[SI(i)] = true`**（改「已领」态）· **`Wallet.Grant` 真发奖**（`Wallet` 是不可逆的累计读数）· **`ShowCollectedWindow(...)` 弹一扇 `Reward Window`**（⇒ 还会通过它的 `OnClose = RefreshOpenDailyWindows` 去重建还开着的日常窗）。

⇒ **会改被测状态**，而且**发出去的奖收不回来**（其余两样能还原、这一样不能）⇒ 照 W4 的判法（开包窗那处）与派单的硬约束：**不接**。
要真接，唯一诚实的形状 = **另起一块**（走生产入口另开一扇 → 跑口 → 收尾还原），且**必须先跑一次 Unity** ⇒ **这是新账、不是本件**。
⚠️ **顺手订正 W7 §四·2 的一处措辞**（不影响结论）：`_streakClaimed[i] = true` **不会移动** `DailyData.StreakClaimableDay` —— 那个读口 = `StreakCollected`（另一条轨，`:515`），收一格**不改它**；真正的代价是**钱包 + 弹窗**这两样，加上「旗标被置真」。

### 3·3 重摇任务窗 `popA` —— **接**（已落 `:9731-9733`）

W7 §四·3 点名的「`Close()` 副作用链 / 要重新核它跟后面那几段」，**逐条查实后落点选在 §⑨ 之后**：

1. **那一刻页签本来就是 `Missions`**（§⑨ `:9704-9706` 刚断过）⇒ `Close()` 里那句 `FindOpenTabbedWindow()` → `tabButtons.Click(0)`（`Shell/MissionRerollPopup.cs:430-441`）是**恒等操作**；
2. `ChangeTab` **只切 `activeSelf`、不重建**（`Shell/RewardsWindow.cs:179-188`：`SetActive` + `OnOpen()` + `RefreshHighlights()`），而它调的 `MissionsTab.OnOpen()` 是**空实现**（`Shell/MissionsTab.cs:245`）⇒ **零副作用**；
3. ✅ **不打扰后面那几拍**：`popA` 是上面 `CheckAbsorbRule` 点关的那一扇 ⇒ 无参 `TryOpen()` 走 `Closed` 支重建（`MissionRerollPopup.Open()` = `LastOpened = this` + `Build()`）；`popA` 在本块之后**零引用**，① 那一大段用的是 `mtComp` / **现取**的 `rowFull`…（`:9744+`），与它无关；`:9749` 那条对照取的是**本块之后**的 `LastOpened`。
4. **站立点**：本窗原有一条**更强**的站立点（真路径 `pl.ClickAt(100,1000)` → `pop2 Closed`，`:9631-9632`）⇒ 本块**不替代**它，只补「点之前必须是 `Open`」+「非吸收层」两条。

### 3·4 A799（夹具 1 处）—— **本文件里没有这一处**（如实，附搜过的词）

- 主判据全文读了：`资料/普查产出_1015/R2_A799全量表.md` §一 + **全量表 199 行**。**判「会新裁」的 15 行逐条列出来** = 生产 9（`LeaderboardRow`×4 · `PurchasePremiumWindow`×2 · `MatchLogRow`×1 · `RankedRewardEventWindow`×1 · `SocialWindow`×1）+ **`Editor/MainMenuScene.cs` 6 行**（`:8712/8713/8715/8770/8777/8782`）⇒ **`Editor/` 侧 6 处全在那一个文件**；`Editor/{BattleScene,RewardsScene,CollectionScene,ShellScene,ShopScene}.cs` 在这一列里**一行都没有**。
- 本文件侧现核：`grep -n "MenuDraw\.\(Text\|TextBox\)(" Editor/RewardsScene.cs` ⇒ **代码调用点 0**（12 处命中全是**注释**或 `MenuDraw.TextClipUnavailable/…UploadSkipped` 那两个计数器）。
- 搜过的词：`MenuDraw.Text` · `MenuDraw.TextBox` · `MenuDraw.TextClip*` · 七个包装器（`MenuWindowBase.Text/TextBox` · `WindowsManager.Text` · `ItemDrawer.TextCentered/ClippedText` · `ChatPanel.Text` · `SettingsWindow.Text` · `PlayerProfileWindow.Text` · `AllianceMemberTab.TextSoft`）的实参形式；本文件只有 6 处 `win.Text/win.TextBox`，**全是自带控制组的裁切探针**（`:4404/4420/4459/4463` 是 `§三·b4-d-3` 那组「控制组 vs 实验组」，`:9982` 是 A266 折行探针且挂在**无父**的探针根上、`win.Clip = null`）—— 它们的新裁是**目的本身**且注释已写明「为什么可以」，**不属于** A799 那 15 处（那 15 处是 R2 逐条解父链解出来的**意外**新裁点）。
- ⇒ **本件在 A799 这一笔上零改动**。🔴 **记账要订正一处**：`资料/普查产出_1016/盘点_GH段剩余小账.md:22` 写「夹具 6 在 `Editor/{BattleScene,MainMenuScene,ShellScene,CollectionScene,RewardsScene,ShopScene}.cs`」—— 与 R2 自己的全量表**冲突**（那 6 处全在 `MainMenuScene.cs`，且 W3 已补完那一段注释：`普查产出_1016/W3_MainMenuScene四笔.md` §2·4「1 处（纯注释，覆盖 6 个调用点）」）。

---

## ④ 没查清的 / 顺手发现（⛔ 越界的一处都没改）

1. ⚠️ **换口「当天逐位同值」是【静态推理】，不是跑出来的**（本件禁跑 Unity）。三条**已核过**的、和 W4 §④·1 同族的两个可能分叉点：
   - **父链缩放**：`TmpRenderedRect` 走 `localToWorldMatrix`（含父链缩放），旧口 `Label.WorldW` 是**纯局部值** ⇒ **父链 `lossyScale ≠ 1` 的标签会差一个倍数**。本文件侧**已核**：两处「原版 `localScale 1.15`」（`Special Missions`/`Daily Missions`）在我们的实现里是**烘进矩形与字号**的（`Shell/MissionsTab.cs:274-295` 的 `R()`/`FS()`，节点本身 `scale = 1`）；唯一真的写 `localScale` 的那类节点（`Shell/CampaignTab.cs` 的 `Premium Mark`，`localScale = 2`）**底下没有 `Label` 子件**（只挂一颗 quad）；`CheckScaleTwo` 那套夹具**用完就还原**（`scaleRoot.transform.localScale = one`）且它量的是**节点坐标**、不经过本件的两个口。⇒ **本文件今天同值**，但这不是全局保证（别的宿主仍按 W4 那条如实标）。
   - **A266 的「待办兑现」副作用**：旧口 `WorldW` 会经 `EnsureMeasured()` 兑现「未激活时欠下的折行待办」，新口**不做这一下**。本文件侧**已核**：那段探针（`:9897-10050`）只被 `WorldW`/`LineCount` 读，**没有任何 `RectOf`/`TextLeftPx` 落在它上面** ⇒ 无影响。
   - **空串标签**：`TmpRenderedRect` 对空串 TMP 会给 TMP 的哨兵值（`4.29e9`，助手自己的 doc 已记）—— 与旧口是否同值**没逐处核**（同 W4 §④·1 的如实标）。
2. 🔴 **同族还有一处直读没换**（**不在本件定义里 ⇒ 没改**，列给调度台）：`RewardsScene.cs:10034`
   `float a266W = a266Lb.WorldW * 108f;` —— 它是 A266 探针里的**单点直读**（不是 `RectOf`/`TextLeftPx` 那两族口），语义是「折行宽真的落到了框内」。要收它得先判「换成 `textBounds` 之后那两条断言还成不成立」（它在 `ForceRelayout()` 之后读，缓存**应该是新的**）⇒ 单列一小笔更稳。
3. ⚠️ **本文件里 `CheckAbsorbRule` 的调用点现数 = 5**（`:5760 / 6488 / 7149 / 9337 / 9696`；第 6 个命中 `:178` 是本文件那个**转调包装**自己那句，不是调用点）—— 与 `Shell/MenuDraw.cs` 里那句「26 个调用点」的记账**对不上**（W3 §③·1 已报过「实测共 23」）—— `Shell/` 不在本件白名单，**只记不动**。
4. ⚠️ **本件的两处新块【没有经过 Unity 执行】**（本轮口径：A 表清零前不中途跑）⇒ 红绿以收口那次 `RewardsScene.Run` 为准；两处的前置（`darkHit != null` / `点之前 == Open`）在公共口里都是**显式报红**、不会静默早退。
