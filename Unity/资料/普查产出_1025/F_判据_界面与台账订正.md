# F · 判据：界面线与台账订正（只读代理产出，主对话落盘）

> 代理 F 为只读模式（Explore，无写工具）⇒ 报告由主对话代存，内容一字未改。
> 日期：2026-10-25 · 第七十五会话

---

## 任务 1 · 界面线两处判据缺口

### A1520「壳卡死」
- **结论**：**仍在**（缺陷本体未验）。但「无判据、无复现」这半**已过期**——C3 已给出唯一可疑机制 + 3 条假设；台账自己两处互相矛盾。
- **证据**：
  - 台账自相矛盾：`资料/任务线/界面线/任务总表.md:138`（「A1520 … C3 给出唯一可疑机制 + 3 条复现步骤」）vs `:208`（T02 行「只剩壳卡死无判据、无复现」）。
  - 机制：`资料/普查产出_1024/C3_界面卡牌活动判据总表.md:237-251`（5·3 唯一可疑机制）+ `:246-251`（H1/H2/H3 三条真机步骤）。
  - 代码链：`Shell/MainMenuRuntime.cs:1911-1922`（`OpenHome` → `OpenWindow(BaseMenu, closeAll:true)`）→ `Shell/WindowsManager.cs:605-630`（`CloseAllWindows`，632-660 收尾只摘 `Closed`）→ `Shell/GameWindow.cs:711-718`（首句守卫：`!activeSelf` 直接 return，**一旦脏态永久**）→ `:776`（`BaseMenu == this` ⇒ `Hide(); return;`，A1714）。
  - 冻结判据链：`Shell/PointerLayer.cs:765-782`（`TopWindow` 非 `Open` ⇒ ESC 直接 return false）、`Shell/MainMenuRuntime.cs:923-934`（`top.BlocksLeftNav` ⇒ 左栏命中区降到 `QNavHitBlocked`=3001）、`Shell/GameWindow.cs:78-81`（`BlocksLeftNav`）。
  - ⚠️ 反证：`Shell/GameWindow.cs:700-710` 自陈「今天生产路径零个站」能造出「`state=Open` + inactive」⇒ 历史冻结可能来自旧 build/沙箱/连点。
- **可能的复现路径**：
  1. **残影**：左栏`社交`→（不关）左栏`游玩`→模式卡`练习`（T12 已把第一跳接成 `OpenHome(closeAll)`，H1 只作回归确认）。
  2. **冻结（ESC）**：停在商店/收藏 → ESC 两次 → 再点左栏（H2）。抓 `TopWindow.name / CurrentState / BlocksLeftNav`。
  3. **冻结（baseMenu 支）**：收藏→ESC→商店→ESC，反复 3 轮（H3）。
- **该断的不变量（建议加进 `Editor/ShellScene.cs`）**：
  - I1 状态对偶：∀win，`activeSelf == (CurrentState != Closed)`（抓「绕过 Close/Hide 直接置灰」与「Hide 后状态没写」）。
  - I2 `TopWindow == null || TopWindow.IsOpen() || TopWindow == BaseMenu`（ESC 死的第一判据）。
  - I3 导航闸对偶：`NavHitQueueForTest == (top!=null && top.BlocksLeftNav ? QNavHitBlockedForTest : QNavHitForTest)`，**两个方向都断**（读口已存在：`MainMenuRuntime.cs:968-971`）。
  - I4 `CloseAllWindows()` 后：无 `activeSelf==true` 的窗；`openWindows` 里无「active 却不在册」的孤儿。
  - I5 `OpenHome()` 后：恰好 1 扇 active+Open 的窗（= `BaseMenu`），且其余全 inactive/Closed。
  - I6 在 `closeOnEsc` 的 Open 窗上 `KeyCancel()` 必须返回 true 并让它进 `Closed`（不许静默 false）。
- **真机 vs 批处理**：I1–I6 = **批处理**（ShellScene，秒级，驱 `OpenWindow/Close/OpenHome` 同一批入口即可）；**「冻结态是否可达」= 真机**——批处理的直调 `Click()` 绕过命中队列（S0「查得到点不到」同族），复现不到真点击。建议真机先加一行诊断日志（TopWindow/CurrentState/BlocksLeftNav/NavHitQueue）。
- **该做什么**：T02 行改口径（判据已有、缺的是不变量断言 + 真机复现）；补 I1–I6 六条断言；真机按 H2/H3 跑一次。

### 轮抽 Card List 的 `Scroll View` 步长判据
- **结论**：**仍在**（代码没做、台账 §一 **0 命中**、交接**没记错**）；且「判据缺」**现在可以闭合**——代理已读到原版值。
- **证据（我们侧）**：`Shell/DraftModeWindow.cs:510-511`（`DeckListR` 1542..1858 × 330..800、`DeckRowH=38`，注释自陈「我们挑的」）· `:2335-2383`（Card List 按行排）· `:2380-2383`（日志：「原版是 `Scroll View`，**我们这一版没有滚动**」）。台账：`任务线/界面线/任务总表.md` §一（46-252 行）grep `Scroll View|步长` = 0；`资料/普查产出_1024d/W7_轮抽与连带_执行报告.md:138` 明写「没有判据说原版滚一格多少像素 ⇒ 建议单开一条账」。
- **证据（原版，本次现读）**：`bundle_menus_assets_all` 扫 `m_ScrollSensitivity/m_Spacing` 并按父链认领：
  - `MonoBehaviour_-1440327974928745787.json:22`（Demo 实例）/ `MonoBehaviour_8659355001888467690.json`（Timed 实例）：`m_ScrollSensitivity = **50.0**` · `m_Horizontal=0 · m_Vertical=1` · `m_MovementType=1(Elastic)` · `m_Elasticity=0.1` · `m_Inertia=1` · `m_DecelerationRate=0.135`，父链 `Scroll View > Cards in deck panel > content > Draft Mode Deck Info Panel`。
  - `MonoBehaviour_6258000673127561925.json:19` / `_762676418022788530.json`：Content 的 VLG `m_Spacing = **2.5**`。
  - `说明书/04_界面UI/菜单全树.md:4059-4062`：`Scroll View 331×589` · `Content` 子件 `Deck Selector Card Info button 331×45` ⇒ **行步距 = 45 + 2.5 = 47.5 px**（我们 38、且无滚动）。`S3_轮抽窗原版规格.md:363-364` 同值。
- **该做什么**：界面上补**一行账**：行高 45 / spacing 2.5 / 步距 47.5 / `m_ScrollSensitivity=50` / 竖滚 Elastic .1 / 减速 .135 / 视口 330.97×588.88；实现挂 `MenuScroll`（竖）并补断言；顺带订正 `DeckListR` 落点（我们挑的框比原版大）。

---

## 任务 2 · 交接 §2·2/§2·3 七条逐条判定

### 1. `Alliance Info` 页提示（要服务端）
- **结论**：**已做完**（本地可做的「提示」这一半已落地；整页等外部）。
- **证据**：`Shell/DraftModeWindow.cs:2385-2393`（`_deckTab==2` ⇒ 自拟中英提示 `Alliance Hint` + `Debug.Log` 出声）· 原版判据 `S3_轮抽窗原版规格.md:376`（`UpdateData` 调 `LeaderboardManager.GetCachedLeaderboard`，本地 0）。**无任何断言**（`Editor/DraftAssertD3.cs` grep `Alliance` = 0）。
- **该做什么**：核销「提示」；台账降格为 ⏸ 等外部（服务端）；补 1 条断言（切第 3 页 ⇒ `Alliance Hint` 在 + 出声一次）。

### 2. `PackName/*` 推广到锻造厂/商店
- **结论**：**记错 / 不适用**（那两个界面没有可推广的「包名」对象）。
- **证据（搜过哪些词/文件）**：`PackName` → `ShopData.cs` **0**、`ShopWindow.cs` **0**、`ShopTabPage.cs` **0**、`ForgeData.cs` **0**、`ForgeTab.cs` **0**。`official_loc_13lang.json` 的 `PackName/*` 共 **856** 键，grep `booster` = **0**。商店卡包是 4 条**自编商品名**且**无 `packId`**（`ShopData.cs:150-157`：Ultramarines/Sautekh/Space Wolves/Leviathan Booster）；锻造厂奖励是**野牌**不是包（`ForgeData.cs:73-78`）。出处原文也只是「顺手接上会更统一（另开账）」（`D2_轮抽各界面原版规格.md:197`）。
- **该做什么**：从交接删掉（或改成「等将来商店走真 `packId` 再谈」）；⛔ 别为凑统一去编映射。

### 3. `Badges._notInDataWarned` 拆两个集合
- **结论**：**仍在**（代码仍是**一个** `HashSet`）。
- **证据**：`Core/Badges.cs:431`（唯一集合）、`:445`（读口）、`:452`（`ResetUnknownForTest` 直接 `Clear()`）、`:462`（`PlaysInPlayIcon` 里 `Add`）· `:442-452` 的 doc 自陈「清空之后同样的图名**会再告警一次**（有意）」· 自检 `Editor/BadgeFilterAssert.cs:88-94` 每轮先 Reset ⇒ 17 行重复日志（`:155-166` 把 17 条当「已知」解释）。
- **该做什么**：在**自检断言线**补一行：拆「去重告警集」与「本轮观测集」，`Reset` 只清后者；补一条断言钉「Reset 不重打日志、但本轮名单仍完整」。

### 4. 回放采样条数直接读口（`ReplayTraceCountForTest`）
- **结论**：**已做完**（该条目**已过时**；且交接写的属性名**不存在**，但等价读口早就有了）。
- **证据**：`grep -rn "ReplayTraceCountForTest" Assets/` → **0 命中**（另搜 `TraceCount` / `ReplayTrace` / `SampleCount`）。实事：`Battle/BattleDriver.cs:1247`（`Finish(_replayFt.ObservedCount)` —— 已改成「真演了几条」）、`:16485`（读口 `ReplayFullTraceForTest`）；`RuleEngine/Core/ReplayFingerprintTrace.cs:85`（`SampleCount`）、`:114`（`ObservedCount`）；断言已在 `Editor/BattleScene.cs:28153-28157`（`ActionCountMismatch==0`，打印 ObservedCount）与 `:32358-32382`（**少演一条的灭自证**：`traceFull` 多一条 ⇒ 必须报 + 出声）。
- **该做什么**：交接**删掉此条**（改索引到 W9）；若坚持要「直接读口」这个名字，只是改名、无功能缺口。

### 5. `A462` 抬起沿的源级断言
- **结论**：**仍在**（只有「按下沿」的源级断言）。
- **证据**：`Editor/BattleScene.cs:30918-30964` 只扫 `ClickedThisFrame()` 并断「**只剩 4 处**」；全文件搜 `upCallSites/releasedCallSites/IndexOf("ReleasedThisFrame` → **0**（唯一命中 `:24697` 是另一条无关白名单）。`BattleDriver.cs` 里**代码**调用 `ReleasedThisFrame()` 实际有 **7 处**：`5448 / 7276 / 7590 / 9384 / 12918 / 13221 / 15383`（定义 `:29312`、测试口 `:29373`）。
- **该做什么**：加一条对称源级断言（扫 `ReleasedThisFrame()`，白名单这 7 处、逐处一句话判据），并与现有「按下沿 4 处」互斥（同一行不许两条沿）。

### 6. `Avatar Name` 视口外不建节点（`V5` 的 (c) 支）
- **结论**：**仍在（拍板项）**，且现状是**我们偏离原版**、断言还把偏离钉成了「正确」。
- **证据**：我们 `Shell/AvatarTab.cs:411` `if (!_scroll.Intersects(r)) continue;`（视口外整格不建；`:396-397` 注释自陈）；原版 `d:/2/tools/decomp_full/AvatarTab__InitializeGameObjects.c:140-160` 对**过滤后的全表**逐个 `Instantiate`（`:152`），**无任何视口判据** ⇒ 原版逐格建。断言 `Editor/ProfileAssertU2b.cs:288-311` 用「原版视口字面量」算 `expectNames`，并断 `nameNodes == expectNames`（`:299`），文案写「按既定规矩本来就不建（A184/A435）」——**钉的是我们的裁剪，不是原版**。台账 `界面线:247` 的 V5 行只覆盖「多印名字（显隐）」，不含建不建。
- **该做什么**：拍板二选一——① 照原版建全部格（删 `Intersects` 早退、`U2b` 期望改 `cells==15`）；② 保留裁剪但记成**已知偏离** + 改断言文案/台账（⛔ 现在这样 = 自证）。必须先进任务总表 §一，不能只留在交接 §2·3。

### 7. 轮抽 `Scroll View` 步长
- **结论**：**仍在**（同任务 1·2）。**证据**同上。
- **该做什么**：界面线 §一 补行（附已读到的原版值）。

---

## 任务 3 · 三个 ✓ 项是否「只差真机」

### `DiedLastTurn`
- **结论**：**不是「只差真机」——它是纯引擎逻辑，批处理即可，且台账已核销。**
- **证据**：`引擎线/任务总表.md:130`（`✓` 已核销；写点 `RuleCore.cs:1561`、读点只有自检）· `Editor/RuleEngineTest_EngineE6.cs:30-84`（`E6T1`：写脏 999 读数不变 + 清 `DeadUnits` 归 0，**已把自证断言换掉**）。无需真机。

### `C5 Q3`
- **结论**：**不是「只差真机」——差的是跑批处理 `RuleEngineTest.Run` + 订正 C5 文档**（交接 §2·3 说它「已核销」不准确）。
- **证据**：`引擎线/任务总表.md:144`（`✓ 已修待验收`，明写「⏳ 待跑 `RuleEngineTest.Run`」）· `:32-33`（`C5 Q3` 的「建议落点 2」与主结论相反，下一轮订正 doc）。无真机项。

### `M2 立绘中心 0.536`
- **结论**：**不是「只差真机」——0.536 这一行已核销（不是独立任务），但并入的「整卡面 +0.08」**代码没改**。**
- **证据**：`卡牌线/任务总表.md:93`（`✓` 已核销，并入整卡 +0.08；`X1_三处判据补查.md:90-122` 现读 `0.456→0.536`，152 实例）· `:92`（「整卡面偏低 0.08」= `✗` 未做；修法一处但移动每一层 ⇒ 需单独一盘 + 全场景复核 + **并排比 PnP**）。⇒ 先做代码/几何断言（批处理），再真机并排。

---

## 主对话合并建议（只建议，未改）

**交接 §2·2/§2·3 该删/改的**
- 删：`PackName/* 推广到锻造厂/商店`（记错，无对象）。
- 删：`ReplayTraceCountForTest`（已完成；改索引 `W9` / `BattleScene.cs:28153`）。
- 改：`Alliance Info 页提示` ⇒ 核销「提示」、降格 ⏸ 等外部 + 补断言。
- 改：`Avatar Name 视口外不建节点` ⇒ 从「要不要照原版」改成**已知偏离待拍板**（附原版逐格建证据）。
- 留：`Badges 拆两集合`、`A462 抬起沿源级断言`、`轮抽 Scroll View`（三条都真开着）。

**各线 `任务总表.md` 该补/该改的行**
- `界面线 §一`：**补行**「轮抽 Card List 无 Scroll View（步距 47.5 / sensitivity 50 / 视口 330.97×588.88）」；**补行**「Alliance Info 页：提示已落地、整页等外部」；**改** `T02/A1520` 那一格（去掉「无判据、无复现」，改成「机制+3 假设已有，缺 I1–I6 断言 + 真机复现」）；**改** `:247 V5` 行加一条「(c) 建不建 = 待拍板」。
- `自检断言线 §一`：**补两行**——① Badges 拆集合并断言 Reset 不重打日志；② A462 抬起沿源级断言（7 处白名单）。
- `引擎线`：`C5 Q3` 行状态文案保持 `✓ 已修待验收`，但交接 §2·3 的「已核销」要改回「待跑批处理」。
- `卡牌线`：`M2·战术卡立绘中心 y` 已核销不动；`M2·整卡面偏低 0.08` 保持 §一（`✗`）。

---

## 摘要（原报告 ≤25 行摘要）

- **A1520**：仍在；判据其实已有（C3 机制+3 假设），缺 I1–I6 状态不变量断言（批处理）+ 真机复现；台账 `:138` 与 `:208` 自相矛盾。
- **轮抽 Card List Scroll View**：仍在、台账 §一漏登；判据已可闭合——原版 sensitivity **50**、竖滚 Elastic .1 / 减速度 .135、格子 45 + spacing 2.5 ⇒ 步距 **47.5**、视口 330.97×588.88。
- ① Alliance Info 提示：**已做完**（`DraftModeWindow.cs:2385-2393`），核销+降格等外部+补断言。
- ② PackName 推广商店/锻造厂：**记错**（5 个文件 grep 均 0、官方 856 键无 booster、商店无 packId）。
- ③ Badges 拆集合：**仍在**（`Badges.cs:431/452/462` 单集合，Reset 会重打 17 行）。
- ④ 回放采样读口：**已做完**（`BattleDriver.cs:1247/16485`、`BattleScene.cs:28153/32358`）；`ReplayTraceCountForTest` 搜 0 次、名字是记错。
- ⑤ A462 抬起沿源级断言：**仍在**（`BattleScene.cs:30943` 只扫 `ClickedThisFrame()`=4 处；7 处 `ReleasedThisFrame()` 未钉）。
- ⑥ Avatar Name 视口外不建：**仍在/拍板**；原版 `AvatarTab__InitializeGameObjects.c:152` 逐格建，我们 `AvatarTab.cs:411` 裁掉，`U2b:299` 把偏离钉成「正确」。
- ⑦ 轮抽 Scroll View：同第 2 条（仍在，判据可闭合）。
- `DiedLastTurn`：不是只差真机——纯引擎、批处理可验、已核销。
- `C5 Q3`：不是只差真机——差跑 `RuleEngineTest.Run` + 订正 C5 doc（交接说「已核销」不准）。
- `M2 0.536`：0.536 已核销；真正剩下的是「整卡面 +0.08」代码未改 ⇒ 批处理断言 + 真机 PnP 并排。
