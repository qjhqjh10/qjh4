# B89_RXb · 自设计标注修正（B 档全改 + C 档可改的改）

执行者：`RX`（原审查岗，本轮转岗动手）· 日期 2026-10-28 · 授命 = 调度台 B89 第 1 波转岗指令
配套：只读审计 `报告/B89_RX_自设计标注审计.md` · 账补丁 `账/_parts/B89_RXb.tsv`（**6 行** · 轮次 `B89-RXb` · 已过 `--dry`）

---

## §0 摘要

| 项 | 结果 |
|---|---|
| **B 档（不该打）7 行** | **改掉 7 行（100%）** —— 全部就地改写理由 + 该改值的改值 |
| **C 档（标错）10 行** | **改掉 6 行**（`GameModesHandler:428` → TODO · `ItemDrawerCurrency` 2 行 → TODO · `WindowHeaderWithBackButton` 3 行 → 保留标记、理由写准）· **未改 4 行**（`LoadableRef` 3 行 / `PlayerProfileState` 2 行 / `RuleCore` 1 行 —— **不在本轮文件所有权内**，见 §3） |
| **顺手补的真缺口** | **1 条做掉** —— 原版横幅第二行 `Status: <ClientState>`（原来缺） |
| **变体标记归一** | **10 行**由 `🔶 我们挑的` 一类改成规范串 `🔶 自己设计（非原版）`（NetRuntime 4 · WindowHeader 4 · ItemDrawerCurrency 2 转 TODO） |
| **验证** | `TMPDIR=/tmp/wf_B89RXb bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0** |

---

## §1 逐条改动

### 1·1 `Core/CardView.cs` —— B-1 / B-2（`showStats` 的容器规则）

- **值**：`:15267` `StatContainerRuleForCell` 由 `NoSimplifiedView(15)` ⇒ **`Normal(0)`**。
  - 判据：`assets_full` 按 MonoScript pathID **`-1746985122272567298`** 反查出 **760 份** `CardTextCountersController` 实例，`activationRules` 明文 —— **608 份 `[[0]]`** · **152 份 `[[0,15],[5,10]]`**
    （`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/` · `bundle_menus_assets_all/MonoBehaviour/`）。
  - 行为影响：`ContainerOpens` 里 `Normal` 与 `NoSimplifiedView` **都是「开」** ⇒ **运行期行为零变化**，改的是「取值是否忠实于原版实例」。
- **说明行**：`:15245`（注释块）与 `:15265`（doc）就地改写 —— 删掉「本机没有那张 prefab 的 JSON 可读」，换成上面这组实数；并写明**152 份那族的第二容器（`SmallScreen|SimplifiedView`）我们塌掉了 = 已知惰性差异**（无小屏档 ⇒ 恒关），**不是判据缺失**。
- 标记**保留**（理由：原版的「容器」是 GameObject 列表，我们**一个数值 = 一个图层** —— 这一层载体确实是我们挑的）。

### 1·2 `Net/NetRuntime.cs` —— B-3 / B-4 / B-5（`Connecting` 横幅）

- **`:262` 段（B-3）**：删掉「基串判据拿不到」。基串是**复刻**：`cpp2il/out_cpp2il_dev_ilspy/ShowStatusWhenConnecting.cs:48` 的 `string text3 = "Connecting" + text;`。`🔶` 只剩**字号 / 颜色 / 对齐**（IMGUI 默认 skin ⇒ 确无像素判据）。
- **`:298`**：出处由 `:32` 订正为实读的 **`:48`**（原文那句"R4b 取 `Loc.T(...)` 是猜的"保留为历史痕迹）。
- **`:282`**：「基串是 🔶」⇒「基串是**复刻**」。
- **`:313` / `:314`（B-4 / B-5）**：显示条件**改代码**。判据 = 原版 `OnGUI` 尾部 **`if (PhotonNetwork.inRoom) enabled = false;`**（dev_ilspy `:60-63`，`out_il2csharp` 同句；`decomp_full` 里这个类只有 3 个成员、别处无启用点）。
  - 旧：`return st == NetState.Connecting;`
  - 新：`Connecting / Handshaking / Lobby / Listening` 亮；`Off / InBattle / WaitingReconnect / Closed` 不亮（= 原版「未进房一直亮、进房即灭」的 `NetState` 映射，映射本身是 🔶，已在注释里标明）。
- **`:268` 段**：改成「显示条件与第二行」的自设计说明（含上面那条判据）。
- **🆕 第二行（顺手补的真缺口）**：`:321` 新增字段 `_statusLabel`、`:384` 在建横幅时建第二条 `Label`（正文下方 24 设计 px）、`:357` 每拍写 `"Status: " + st`（`:347` 关横幅时一并藏）。
  - 判据：dev_ilspy `:56-58` `GUILayout.Label("Status: " + <ClientState>)`。
  - ⚠️ **没有用 `\n`** —— 本仓 `Label` 的点阵后端（`TextCanvas`，5×7 ASCII）**不认换行**，`SetText` 会把它当未知字符；所以照原版做**两条标签**。
  - 枚举无 Photon ⇒ 打印我们的 `NetState`（映射是 🔶）。
- `:301` / `:365` / `:370`：变体前缀归一为 `🔶 自己设计（非原版）`。

### 1·3 `Shell/GameModesHandler.cs` —— B-6（默认值理由）· C-5（接口欠账）

- **`:455` 段（B-6）**：理由从「本机**没有** `demoData` 这一层（全仓 `canEnterDraft` 0 命中）」改成「**判据可读、我们没实现**」——
  `DemoData.canEnterDraftMode` 在 `dump.cs:79362`、解锁路径在 `cpp2il/…/PracticeEvent.cs:97-101`（原「0 命中」只对 `decomp_full` 成立：我实测 `decomp_full` 0 / `cpp2il` 4 文件 / `dump.cs` 1）。
  - **默认值 `true` 仍是 🔶**（照原版 `false` 接会变「谁都进不去且不可恢复」的死状态）；「首胜落地后翻回 `false`」的待办保留。
- **`:428` 段（C-5）**：`🔶 自己设计` ⇒ **`TODO（欠账，⛔ 不是「自己设计」）`**。`IRankedEvent` 在**原版确凿存在**（`dump.cs:57719` / `:62773`）⇒ 这是我们没建接口，不是设计选择。

### 1·4 `Core/PlayerDataMath.cs` —— B-7（`ExtractDeckInfo` 三格）

- **`:295`**：口径收窄 —— 只有 `deckId(+0x18)` **真来自服务器**；`lastEditTime(+0x68)` / `warcryId(+0x30)` 都是 `CardDeck` 的**序列化字段**，是**我们 `PlayerDeck` 少了列**（补列后就不用填空）。
- 代码不变（仍填 `null / default / null`，那是我们挑的 ⇒ 标记保留）。

### 1·5 `Shell/ItemDrawerCurrency.cs` —— C-7（ARF 兜底）

- **`:27`（文件头 ②）** 与 **`:85`（`ar` 上方）**：`🔶 自己设计` ⇒ **`TODO（还没查清…）`**。
  - 原文自己写着「原版 ARF 在这种容器下的行为**没有判据（未读）**」—— 按铁律 18 ⛔③，未查清**不许**用 `🔶` 蒙。

### 1·6 `Shell/WindowHeaderWithBackButton.cs` —— C-6（保留标记，理由写准）

- **`:24` / `:32` / `:68` / `:242`**：区分开「**行为**照原版」（取不到标签 ⇒ 空串，判据 `Shell/BaseEventPopup.cs:42`）与「**接线方式**是我们挑的（`TitleTermResolver` 注入口，因 `LiveOpsLocalCatalog` 正在重构）」；`null` 默认值的语义由「我们挑的」改成 **「待接」**。前缀统一成规范串。

---

## §2 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_B89RXb bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0** |
| 行尾 | 6 个文件全 **LF**、**0 个裸 CR**（二进制计数） |
| 行号稳定性 | `CardView` 锚点未动（`StatContainerRuleForCell` 仍 `:15267`、`ApplyStatContainersOnHeroLand` 仍 `:15275`）· `NetRuntime` 的账引用（`:181` / `:136` / `:81`）**全部在改动点之前**，未漂 · `GameModesHandler`（`:433` / `:462`）、`PlayerDataMath`（`:304`）、`WindowHeader`（`:69`）锚点未动 |
| 新增行 | 只有一条真缺口（第二行 Status）：`NetRuntime.cs` +13 行，**全部在 `:262` 之后** ⇒ 账里该文件的落点（最大 `:181`）**零影响** |
| 自检 | ⛔ 按本波口径**未跑** Unity（调度台在跑） |

---

## §3 未改 / 待办（⛔ 不在本轮文件所有权内，留给对应代理）

| 位置 | 为什么没改 | 建议 |
|---|---|---|
| `Core/LoadableRef.cs:198` / `:220` / `:236` | `Core/` 不在授命清单 | 按 C 档处理：`object` 替 `JToken` 是**载体替代**（语义逐句照抄），改成普通 ⚠️ 说明、去掉 `🔶` |
| `Shell/PlayerProfileState.cs:131` / `:241` | `Shell/**` 归 E22/E25/E29/E16d | 两个**测试钩子**（`ResetForTest` / `ReloadCollectorXpFromSave`）⇒ 改普通注释 |
| `RuleEngine/Core/RuleCore.cs:15995` | `RuleEngine/**` 归 E26/E27 | `EphemeralMarked` 是 `CardDef` 共享带来的**结构替身**，不是我们挑的语义 ⇒ 改「结构替身」说明 |
| `Battle/BattleDriver.cs:34667` | 归 E24 | 那行写「判据与 🔶 标记见 CardView 那个方法」—— CardView 已改（`15`→`Normal(0)`），**这处引用现在会读到一个已经订正过的说明**，建议它顺手把括号里的指向补一句「现已按 608/760 实值取 Normal(0)」 |

---

## §4 顺手发现

1. 🔴 **「连接中横幅」的显示条件**全仓 **0 条自检断言**（`grep -rn ShouldShowConnectingBanner|ConnectingBannerVisibleForTest|ConnectingBannerTextForTest` 在 `NetRuntime.cs` 之外 **0 命中**）⇒ 账 HUD **805/806** 当初判 A 时没有覆盖面。本轮把语义从「只 `Connecting`」改成「未进房都亮」，**没有任何断言会因此变绿/变红** —— 建议给 HUD 805/806 补一条（批处理里可用 `TickConnectingBannerForTest` 推两个状态读 `ConnectingBannerVisibleForTest`）。
2. **标记写法不统一**：本轮新增里 `🔶 我们挑的` 一类有 10 行，`grep "自己设计"` **扫不到**（`selfdesign_census.py` 因此漏计）。本轮已把这 10 行归一（或转 TODO）；建议以后只认 `🔶 自己设计`。
3. **原版横幅其实是「两行」**（正文 + `Status:`），账 HUD 805/806 的判据要点只写了「点 + 节奏」，**漏了第二行**。本轮已补编码，账补丁里也记了一行。
4. `CardView` 的 `statContainerRules` 那族 **760 份实例**是一个可以直接拿来做「按卡型分档」判据的富矿（608 单容器 / 152 双容器）—— 将来若补「小屏 / 简化视图」这两档，不用再挖。

---

## §5 仍没查清（如实记）

1. **`PlayerDataMath.warcry(+0x30)` 是否玩家可选**：`dump.cs` 里我只读到 `CardDeck` 的内部类（`<>c` / `DisplayClass`），没定位到 `CardDeck` 主类字段区 ⇒ 「warcry 属装饰项」**未证实**。
2. **`NetRuntime` 横幅的字体/颜色**：全 `assets_full` **0 个 `GUISkin` 资产**、按 `ShowStatusWhenConnecting` 的 MonoScript（pathID `927`）反查 MB **0 命中** ⇒ 原版多半走 Unity 自带 skin；自带 skin 的 `customStyles[0]` 具体值没核（要跑编辑器）⇒「无像素判据」方向可信、**未闭环**。
3. **未解 `D:/2/关服前存档/…xapk`**（红线：不动 `d:/2` 一个字节）。若还要坐实远端有没有别的说法，解到 `_tmp_view/apk135/` 再去查。

---

## §6 账补丁的返工（第一版被合并工具整份拒绝）

第一版 8 行**没进主表**（`merge_ledger_parts.py` 一条不合规就整份不写）。三处原因与修法：

| 原因 | 详情 | 修法 |
|---|---|---|
| 域值不存在 | 用了 `活动/窗口` / `活动/轮抽` / `商店/货币` —— 主表现读只有 `战斗界面/HUD` · `联机` · `日常/奖励` · `其他/基础设施` · `轮抽` · `商店` …（**域用斜杠、文件名用下划线**） | 按 `(原版类, 方法)` 在主表里找到**真身行**，域跟着主表走 |
| `方法` 不是签名 | `货币抽屉的 ARF 缩放`（没有 `(...)`，是注记） | **该条移出补丁、只留在本报告 §1·5**（`AspectRatioFitter` 是 Unity 内置组件、不是原版方法） |
| 列数/折行 | 一条字符串里带了换行符 ⇒ 撑成 3 个物理行、列数变 7 / 3 | 整份重建：**一行一条、字段内禁止换行/Tab**，写完逐行断言 11 列 |

修完后的 `--dry` 读数：**更新 6 · 新增 0 · 无变化 0 · 无报错**（4 份主表：`战斗逻辑驱动` / `战斗界面_HUD` / `其他_基础设施` / `日常_奖励`）。
6 行分别落在：`CardScript.HeroLandIntoField` · `ShowStatusWhenConnecting.OnGUI()` · `PracticeEvent.OnMatchWon()` · `GameModes.GetActiveEvent(Type)`（**A→B 降档**）· `PlayerDataManager.ExtractDeckInfo(CardDeck)` · `LiveOpsAssetUtility.GetLabel(ILiveOps, …)`。
