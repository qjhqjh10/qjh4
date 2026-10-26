# X2 — `ForgeTab` 光效 `Rescan` + `MainMenuScene` banned 夹具（2026-10-26 · 第八十三会话）

## ① 两件各改了什么

### A. ① `RewardsScene` 那 5 条 —— `ForgeTab` 建完立刻显式 `Rescan()`

- 文件：`Unity/MyGame/Assets/CardPresentation/Shell/ForgeTab.cs:426`
- 改动：`_readyToggler.Toggle(false, 0f);` → `_readyToggler.Rescan(); _readyToggler.Toggle(false, 0f);`（＋就地注释）
- 根因（`D2` 已核，本轮复核成立）：`UIEffectTogglerController.togglers` 只由 `Awake → Rescan()` 填
  （`Effects/UIEffectTogglerController.cs:42`；全仓 `AddComponent<UIEffectTogglerController>` **只有 `ForgeTab.cs:425` 一处**），
  而**编辑/批处理下 `AddComponent` 不触发 `Awake`** ⇒ 列表恒空 ⇒ 每次 `Toggle` 都在空列表上跑 ⇒
  `UIEffectCanvasGroupToggler` 从未被 `Toggle(false,…)` 过 ⇒ `Ready for level up` 从建出来就一直 active ⇒
  `ForgeTab.Tick` 那道「关着就冻时钟」的闸成了死闸。运行期 play 会跑 `Awake`，所以**真机看不出来**，只有自检暴露。

### B. ② `MainMenuScene` 那 1 条 —— δ 夹具补一次通知

- 文件：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:9166`
- 改动：`CollectionData.ResetForTest();` → `CollectionData.ResetForTest(); (sk as SkirmishEventWindow)?.RefreshBannedLine();`（＋就地注释）
- 根因：`sk` 在 `:8724` 就已 `Open()`（`Open()` 里刷过一次 `RefreshBannedLine`，`Shell/SkirmishEventWindow.cs:799-803`），
  而夹具到 `:9166` 才把卡组改回经典/清督军 ⇒ **没有通知窗** ⇒ 后面那条「`Banned card in deck/Text` 显隐」读的是**旧卡组**算出的状态。
- ⚠️ 编译细节（易踩）：`var sk = SkirmishEventWindow.LastOpened;` 的**声明类型是基类 `LiveOpsEventWindow`**
  （`Shell/LiveOpsEventWindow.cs:238`）⇒ 直接 `sk.RefreshBannedLine()` 是 `CS1061`（本轮第一版就红在这一行）。
  用 `as SkirmishEventWindow` 下行，再 `?.` 兜 null。
- ⛔ 断言**没动**（它读的是 `Banned card in deck/Text` 的 `activeSelf`，并自己现算期望值）。

## ② 为什么选 `ForgeTab` 侧显式 `Rescan()`，而不是 `UIEffectTogglerController.Toggle` 里懒加载

1. **与「显式初始化」的既有口径一致**：本类原版语义就是 `Awake` 里收一次列表（`FindEffectTogglersInChildren`），
   我们的 `Rescan()` 是公开的、注释里已写明「运行期新增的用 `Rescan`」（`UIEffectTogglerController.cs:29`）。
   调用方建完组件后自己扫一次 = 沿用这条既有入口，不改类的行为。
2. **懒加载会把「编辑期特例」藏进运行时组件**：`Toggle` 里加 `if (togglers.Count == 0) Rescan();` 会让
   **任何**未来调用点都静默拿到「空列表 ⇒ 自动重扫」的语义，与原版「列一次、之后靠 `Rescan` 显式刷」不同；
   将来若真有「togglers 本来就该为空」的实例，这条懒加载就是**静默改变行为**。
3. **改动面最小**：只动一处调用点（全仓唯一 `AddComponent` 点），`Effects/` 那个共用件**零改动**（不牵动其它宿主）。
- 代价：`ForgeTab` 侧多一次 `GetComponentsInChildren`（编辑/批处理每建一次走一遍；运行期每开一次锻造页走一遍）—— 可忽略。

## ③ `Rescan` 在生产 play 路径是否幂等（证据）

- `Rescan()` 体（`Effects/UIEffectTogglerController.cs:45-51`）：`found = GetComponentsInChildren<UIEffectTogglerBase>(true)`
  → `togglers.Clear()` → 逐条 `Add`。**先清后填、无累加**。
- play 路径顺序：`AddComponent<UIEffectTogglerController>()`（`:425`）**当场触发 `Awake`** → `Rescan()`（填一次）
  → 我们这句 `Rescan()`（`:426`）**清掉刚填的、按同一批子件重填** ⇒ **列表内容完全相同，无重复注册**。
- 扫描范围无外溢：`_readyRoot`（`Ready for level up`）下面只有 `BuildReadyBlobs` 建的那两团光效宿主 + 根本身挂的
  `UIEffectCanvasGroupToggler`，**没有**别的窗/页的 toggler 会被卷进来。
- 时序也无碍：`BuildReadyBlobs(readyR)`（`:413`）在 `:425` 之前就建完子件；且扫描用 `includeInactive=true`，
  即使节点此刻是关的也扫得到。

## ④ 没查清 / 留给调度台

- ⛔ **没跑 Unity**（按简报）：`RewardsScene.Run` 的 5 条与 `MainMenuScene.Run` 的 1 条**是否真转绿，未验** —— 由调度台统一跑。
- 未验项（只做了静态推演）：`ForgeTab.Tick` 的「关着冻时钟」闸在**编辑/批处理**下现在应当真的冻住了
  （`_readyRoot` 出厂 `SetActive(false)`）；若 `RewardsScene` 那 5 条里有一条断的是「时钟走没走」，跑一次见分晓。
- **账**：两处改动都**零行数变化**（`ForgeTab.cs` 1272 行、`MainMenuScene.cs` 20090 行，改前改后同值；
  两文件原本都是纯 LF、改后仍纯 LF、无裸 CR）⇒ `check_ledger_points` 的落点（`ForgeTab.cs:402/720/742/764/788/798/801/834/891/895/949/988/1227` ·
  `MainMenuScene.cs:15834/17410` 等）**全部不受影响**，故**没有**写 `账/_parts/B9_X2.tsv` 补丁。
- 做法说明（纪律）：为了「零行数变化」，新增语句是**接在同一行**（`; ` 分隔）而不是新起一行 ——
  按 `CLAUDE.md` §18·b 的口径（插一行注释都会让 `check_ledger_points` 报漂移），这里同样适用。
