# Block14 · A469（只剩「生产接线」那一小块）—— 写手代理报告

> 日期：2026-10-14 · 独占文件 = `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`（**只加不改**）
> 账：`项目任务.md` §三 **A469** ——「⏭ **只剩「生产接线」那一小块**（`CampaignTab.BuildContext` → 真数据领到）」
> 简报指定的判据原文：`资料/普查产出_1013/批次计划_1013.md` §六·B（`:149`）+ `资料/普查产出_1013/A表现核_块6.md` §A469（`:146-158`）+ `资料/普查产出_1012/H33_断言落地波三.md` §三·1（`:177-186`）

---

## 一、做了没有：**做了（能做的都做了）—— 但账上那三步【第 3 步原样落不下去】，见 §三**

| # | 账上写的 | 结果 |
|---|---|---|
| 口存在吗 | `ClaimForTest` / `ClickNodeForTest` / `BaseClaimed` | ✅ **三个口都在**（grep 现核：`Shell/CampaignTab.cs:854` `ClaimForTest` · `:789` `ClickNodeForTest` · `Shell/CampaignData.cs:186` `BaseClaimed`）⇒ 照账上写，**没造新 API** |
| ① | 先 `ClaimForTest(0, TierBasic)` 领掉 UM0 | ✅ **已经有人做过**（`:4756` 那句既有断言就是它）⇒ 本块**不重复领**（再领返回 false ⇒ 会假红），改成**断前提** |
| ① 落点 | 「节点染色那几条断言之后、`CampaignData.ResetForTest()` 之前」 | ✅ 落点一致（新块 `:4883-4981`，紧贴 `:4983` 的 `CampaignData.ResetForTest()`） |
| ② | `ClickNodeForTest(1)` → **真点窗里那颗 `Unlock`** → 断 `BaseClaimed(1)` 且窗 `Closed` | 🔴 **前半句成立（真开出了窗）· 后半句【点不下去】** —— 那扇窗是**空的**（缺陷见 §三）⇒ 本块补一句 `Reopen(BuildContext(1))` 才点得动 |
| ③ | 收尾 `RewardWindowFixture.DismissRewardWindows()` | ✅ 做了（并断「恰好 1 扇」+ 关完 0 扇） |
| 判据（灭自证） | 删 `ClaimForTest` 里那句 `ShowCollected`、或把 `BuildContext` 的 `OnCollect` 改成不转发 ⇒ 应红 | ✅ 两条★都满足：删 `OnCollect` 那句 ⇒ `:4934` 红；不转发 ⇒ `:4966` + `:4970` 红 |

## 二、改了什么（文件:行）

**只加不改**：`Editor/RewardsScene.cs` 新增一块 `:4883-4981`（**纯插入**，既有段落一行没动、没重排）。
`git diff --numstat` = `189/17` —— 其中 `17` 是**今天别的写手**（#52–#54 · A797′ · A570 …）留下的，**不是本件删的**；本件是 0 删除。

新增的断言（共 **15 条**，`Check`/`CheckTrue` 计）：

| 行 | 断什么 | 判别力（怎么让它红） |
|---|---|---|
| `:4921` | （前提）本块之前**没有**遗留领奖窗 | 上面 `:4775` 那次没收干净 ⇒ 红 |
| `:4922-4925` | （前提）收掉遗留的 `Campaign Reward Window` 之后场上 **0 扇** | 两扇同族窗并存 ⇒ `PointerLayer` 赢家退化成枚举顺序（静默）⇒ 这条把它逼成显式 |
| `:4928` | （前提）UM0 已领 · UM1 **未领** | 顺序被人挪过 / 提前领过 ⇒ 红 |
| `:4930` | （前提）UM1 `Claimable` 为真 | 前驱链变了 ⇒ 红 |
| `:4934` | ★ **`BuildContext(1).OnCollect != null`** | 删掉 `Shell/CampaignTab.cs:822` 那句 ⇒ 红 |
| `:4937-4939` | ★ context 的 `BaseCollected` / `Claimable` **等于真进度** | `BuildContext` 里写死任一个 ⇒ 红 |
| `:4942` | ★ 点 UM1 节点 ⇒ **开出了窗**（`ClickNodeForTest(1)` 返回真） | `Claimable` 判据被改 ⇒ 红 |
| `:4944` | ★ …真开出了一扇 `Campaign Reward Window` | `ClickNodeForTest` 不再 `OpenWindow` ⇒ 红 |
| `:4961` | ★（前提）窗里 `…/Base Rewards/Rewards/Unlock Button/Hit` 取得到 | 取不到 ⇒ 下面三条等于没查（出声） |
| `:4963-4964` | ★ **真点**那颗 `Unlock`（`PointerLayer` 真路径） | 命中区没了 / 队列错了 / 被别的窗抢走 ⇒ 红（`ClickButtonByQuad` 自己那条前提带实得赢家） |
| `:4966-4968` | ★★ **`CampaignData.BaseClaimed(1)` 翻真** = **生产接线通了** | `BuildContext` 的 `OnCollect` 不转发 ⇒ 红 |
| `:4970-4971` | ★ …再组一次 context：`BaseCollected` 翻真、`Claimable` 翻假 | 同上（读真进度 vs 写死） |
| `:4973-4974` | ★★ **领到 ⇒ 窗自动关**（A448，走**生产那份 context**） | 删 `Shell/CampaignRewardWindow.cs:836` 的 `if (_ctx.OnCollect(tier)) Close();` ⇒ 红 |
| `:4975-4977` | ★ 收掉 A438 弹的领奖窗 = **恰好 1 扇** | `ShowCollected` 不弹 / 弹两扇 ⇒ 红 |
| `:4978-4979` | ★ …关完 **0 扇遗留**（下面那张实拍才干净） | 收不干净 ⇒ 红 |

**期望值来源**（⛔ 全不是我们自己的常量）：原版 `CampaignRewardsWindow` 基础列那颗 `Unlock` → `UnlockClicked` → `TryCollect`；「领到 ⇒ 关窗」= `…g__Refresh_0` 末尾那一跳（判据链已在 `Shell/CampaignRewardWindow.cs` 的 `OnUnlock` 方法头里逐句记着）。

## 三、🔴🔴 顺手发现（**只报不改** · 这是本件最大的一件）：`ClickNodeForTest` 开出来的窗**是空的**

**症状**：玩家/自检点了战役轨道上任意一个节点，弹出的 `Campaign Reward Window` **两列全关、一颗 `Unlock Button` 都没有**。

**证据链（逐句现读，⛔ 不是推测）**：

| # | 出处 | 那一句 |
|---|---|---|
| 1 | `Shell/CampaignTab.cs:803` | `win.Reopen(BuildContext(i));` —— 置 `_ctx` **并** `Build()`（`CampaignRewardWindow.cs:910`） |
| 2 | `Shell/CampaignTab.cs:804` | `_win.Manager.OpenWindow(win);` —— ⚠️ `OpenWindow(GameWindow, object data = null)` ⇒ **`data` 是 `null`** |
| 3 | `Shell/WindowsManager.cs:892` | `win.TryOpen(data);` —— 调的是**带参**那条重载 |
| 4 | `Shell/WindowsManager.cs:451` | `TryOpen(object data)` 第一句 = `SetupData(data)`（无参那条 `TryOpen()` 才是**不调** `SetupData` 的，见 `:475`） |
| 5 | `Shell/CampaignRewardWindow.cs:316` | `SetupData` = `base.SetupData(data); _ctx = data as CampaignRewardsContext;` ⇒ **`data == null` ⇒ `_ctx = null`** |
| 6 | `Shell/WindowsManager.cs:484-494` | `OpenByState()`：新窗 `state == Closed` ⇒ 走那一支：`SetActive(true)` → `CurrentState = Open` → **`Open()`** |
| 7 | `Shell/CampaignRewardWindow.cs:318` | `public override void Open() { Build(); }` |
| 8 | `Shell/CampaignRewardWindow.cs:329` | `var ctx = _ctx ?? new CampaignRewardsContext { Rewards = new RewardSpec[0] };` ⇒ **整棵按空 context 重建成空窗**（`Build()` 首句就把 `:803` 那次建的全销毁了） |

⇒ **1→2 的次序**是关键：`Reopen`（带 context 建好）**之后** `OpenWindow` 又把 `_ctx` 清空并**重建** ⇒ 结果是空窗。
同类先例已经记过一次（**A570**，`批次计划_1013.md:257`）—— 但那一处是**自检夹具**里那句 `TryOpen(null)`，**今天无后果**；
**这一处是生产路径**（`OnNodeClicked` = 节点那颗钮的 `onClick`），后果是**玩家点节点看到空窗**。

**为什么没就地修**：修点在 `Shell/CampaignTab.cs`（`win.Reopen(...)` 与 `OpenWindow(win)` 的次序 / 或改传 context）
—— **不在本件白名单**（本件只许改 `Editor/RewardsScene.cs`）。⛔ 我没有动它。
**建议的修法（留给出账的人判）**：把 `:803/:804` 两句换个写法 —— 要么 `OpenWindow(win, BuildContext(i))`（一次性喂给 `SetupData`），
要么 `OpenWindow(win)` **之后**再 `Reopen(BuildContext(i))`（同 `H12_同窗再开不重建.md:55` 记的那条「`Build()` 必须排在 `OpenWindow` 之后」）。
⚠️ 两者我**都没验过**（跑不了 Unity）⇒ 别当成结论。

**本件在自检里的处置**：在真点之前补了一句 `prodWin.Reopen(cTab.BuildContext(1))`（把 `_ctx` 装回去），
**并在代码注释里逐句写明它是「绕开上面那个缺陷的夹具补丁、⛔ 不是生产路径」**。
判别力不受影响：接线本身错了（`OnCollect` 不转发 / `ShowCollected` 被删）那两条★★照样红。
⚠️ **如实标注**：这一句**会把上面那个缺陷盖住**（它让「窗是空的」这件事在自检里不声不响）——
所以**它必须与本节这条账一起看**；本件**没有**加「断它在空窗状态」的断言（那等于把一个缺陷钉成期望值）。

## 四、没做完的（+为什么）

1. 🔴 **上面那个缺陷没修** —— 修点在 `Shell/CampaignTab.cs`，**不在本件白名单**（简报：只许改 `Editor/RewardsScene.cs`）。
2. ⚠️ **真点那一段是「半根夹具」** —— 走的是 `ClickNodeForTest` 真开出来的那扇窗 + `PointerLayer` 真路径，
   但 `_ctx` 是夹具补回去的（见 §三）。**缺陷修好之后**这一段就是**纯生产路径**，一个字都不用改。
3. ⚠️ **没跑 Unity**（简报硬纪律：写手不跑自检）⇒ 本块**一条都还没真跑过**。
   按覆盖面，本件只动 `Editor/RewardsScene.cs` 一个宿主 ⇒ **同步点跑 `RewardsScene.Run` 一条即可**（`RewardsScene.cs` 宿主）。
   ⚠️ **预期风险两条**（跑之前先看）：
   - `:4963` 那次真点依赖「场上只剩一扇战役奖励窗」—— 本块自己收干净了，但若**别人**在 `:4747`~本块之间又开了一扇，会红；
   - `:4975` 断「领奖窗恰好 1 扇」—— 若 A438 的 `ShowCollected` 哪天改成不弹，这条会红（那正是它要盯的）。
4. ⚠️ **`wm2` 的记账会变**（H33 §三·1 风险③）：本块开头收掉了遗留窗、结尾两扇都关掉了。
   现读核过**没有副作用**：`ShowPreviousWindow`（`Shell/WindowsManager.cs:1059-1060`）把 `popUpWindow`
   填成 `prev.type == Popup ? prev : null`，而列尾那扇（壳 `RewardsWindow`）是 **Fullscreen**（`Shell/RewardsWindow.cs:222`）
   ⇒ 填 `null`；且 `prev.TryOpen()` 走「`Background` ⇒ 只提前台」那一支（**不重建内容** ⇒ `cTab` 这个引用不受影响）。
   ⇒ 本节末尾那句 `if (wm2.popUpWindow != null) wm2.popUpWindow.Close();` **变成空转**（它原本关的就是那一扇遗留窗）。
   ⛔ 那一句**没动**（不是本件的账）。

## 五、核过的东西（免得下一个人重查）

- **落点用符号定位、没抄行号**：锚点 = `CampaignData.ResetForTest();      // 复位，后面的截图要用起手态`（全文件唯一）。
- **行尾**：本文件是**纯 LF**（`CRLF 0 / LF 9958`，二进制读现核），用 Edit 工具改、改完复核**仍是纯 LF**。
- **类型检查**：`TMPDIR=/tmp/wf_b14 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完跑了两次，第二次是订正注释里的行号之后）。
- **别人的活**：B5（`Block5_RewardsScene.md`，13:42 收工）今天也改过**同一个文件**（净 89/17）⇒ 本件开工时先读了它的 `git diff`；
  本块与它那八笔（#52–#54 · A671 · A797′ · A570 · A615 · A539/A738 现核）**零交集**（本块在 `§三·c` 末尾，那些分别在
  `MissionsTab` 段 / 锻造段 / 文本量尺段 / 日常重建段 / `§三·d` 末尾）。⚠️ 但**同一文件先后两个写手**这件事本身要如实记：
  B5 的报告里也写着「**只改了一个文件**：`Editor/RewardsScene.cs`」⇒ **同一文件先后两个写手**：本件靠
  **纯插入（0 删除）+ 锚点（不抄行号）** 来避开撞车；B5 的 8 笔与本块零交集（不同段）。
