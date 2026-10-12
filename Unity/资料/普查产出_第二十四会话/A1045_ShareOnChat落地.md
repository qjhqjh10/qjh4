# `A1045` 落地记录（`Share On Chat` 照原版做）

> **2026-10-12 · 第二十四会话 · 写手 `A1045` 产出，主对话照录**
> ✅ 类型检查通过（`TMPDIR=/tmp/wf_a1045` ⇒ 运行时 0 / 编辑器 0，跑了 3 次）· 四个文件**纯 LF、无翻行尾**。
> ⛔ **没跑 Unity**（红线）· 未动 git · 未改正本。

---

## 一、现读到的形状（账文行号已全部漂掉）

| 位置 | 原状 |
|---|---|
| `Shell/DeckInfoPopup.cs:1751 ShareOnChat()` | 原 `NotBuilt()` + `Manager.ShowPopUp`（已整体替换）；新增 `ShareDeckToChat`（≈`:1806`）· 新常量 `:1862-1871` · `SetChatState`/`IsBannedFromChat`/`ChatRoom` 在 `:1700-1745` |
| `Shell/GenericOptionsPanel.cs:158 OptButton` | **原本没有回调字段** |
| `Shell/GenericOptionsPanel.cs:503 OnButton` | **原本只打一行日志** |
| `Shell/GenericOptionsPanel.cs:431 Apply` | `LocalizeText` 那条远端 I2 警告 |
| `Shell/GenericOptionsPanel.cs:64` 类文档 | 写「本地没有任何调用方」—— **过期**（→ 新账 `A1718`） |
| `Core/Loc.cs:491` | 是 `MenuDeck/Share/ExportSuccesful`，同族另三条**只有注释提到、没有词条** |
| `Editor/CollectionScene.cs:2643` 起 | A1051(3)，原本断 `[DeckInfo] … 还没实现`；`:2354-2370` 注释也写「面板还没建」 |

**判据补充（现读反编译）**：原版签名是 `ShareDeckOnChat(Transform optionsMountPoint)`；
两颗钮的回调是**两个不同的 lambda** —— `…_b__30_1.c`（群组 → `CurrentGroupId`, room=1）/
`…_b__30_2.c`（全局 → 常量串, room=0），两者都落到 `…_g__HandleShare_30_0.c`。
`Context___ctor(this, buttons, Title, AnchorPosition, LocalizeText=1, mi)`
⇒ **原版确实把那个 `Transform` 塞进 `AnchorPosition`**。

---

## 二、改了什么

### 1. `Shell/GenericOptionsPanel.cs`
- `OptButton` 新增 `public System.Action OnClick`（= 原版 `Button.OnClick`）。
- `OnButton` 补**两道闸**：`Interactable == false` ⇒ **动作照进来立刻返回 + 出声**
  （原版 uGUI 连派发都没有；口径同 `DeckInfoPopup.Blocked`）；`OnClick == null` ⇒ **出声**。
  🔴 **原来这里只打日志 ⇒ 调用方挂的动作根本没有地方跑**（这正是本趟的前提缺陷 = 新账 `A1715`）。
- 类文档 / `Open()` 日志 / `Apply()` 的 LocalizeText 警告**就地订正**（铁律 5）：
  **调用方一直在**（`DeckInfoPopup__ShareDeckOnChat.c`），当年是按**文件名** grep `GenericOptionsPanel` 漏的
  ⇒ **按文件名 grep「有没有调用方」会漏**（→ `A1718`）。警告**照旧出声**，只补一句
  「要中文请把 `Loc.T(键)` 喂进 `Context`」。

### 2. `Core/Loc.cs`
- 补 3 条（**键名 = 原版常量表实读、非自拟**）：`MenuDeck/Share/{Title, ShareOnAlliance, ShareOnGlobal}`
  （`0x42D0AE0 / 0x42D08E0 / 0x42D09E0`，与 `ExportSuccesful` `0x42D07E0` 连号）。
- ⚠️ **两列文案都是我们自拟** —— 原版值在远端 I2；且 prefab 里标题是占位串 `Pepito el de siempre`、
  钮字是 `Template` ⇒ **英文列拿不到原版 TMP 原文**（已如实标注）。

### 3. `Shell/DeckInfoPopup.cs`
- 新增 `enum ChatRoom { Global = 0, Alliance = 1 }`（**逐值照抄**）+
  **显式状态口** `SetChatState(inGroup, bannedGlobal, bannedAlliance)`（出厂 `false/false/false`，
  形状同 `AllianceEventScorePanel.SetInAlliance` / `AlliancesTab` 的「本地恒走未入盟支」；
  注释写明本仓无 `AlliancesManager`/服务器）+ `IsBannedFromChat(room)`。
- `ShareOnChat()` 改为**真开面板**：`Title=Loc.T(Title键)` · `LocalizeText=true`（原版字面量 1，**不压**警告）·
  钮 0 = 群组（`_inChatGroup && !_bannedAlliance`）· 钮 1 = 全局（`!_bannedGlobal`）·
  两颗 `AutoClose=true` · 各挂 `OnClick` · `AnchorPosition=null` ⇒ `WindowsManager.OpenGenericOptionsPanel(ctx, null)`。
- `ShareDeckToChat(room)` = 原版四跳：① 再查 `IsBannedFromChat(room)` ⇒ 直接 return（+ 一行出声）
  ② `ExportString`（**故意**不调 `CopyDeckToClipboard`）③ **本仓没有 `ChatGlobalManager` 对等物** ⇒
  `Debug.LogWarning` + 一条 `MessageToast` ④ 关窗交给 `AutoClose`。
  ⛔ **全程不写剪贴板**；**删掉了**「把卡组串印到弹窗里给玩家自己抄」那套权宜兜底（改前是 `Manager.ShowPopUp` 模态）。

---

## 三、断言（`Editor/CollectionScene.cs`，A1051(3) 改写 + 新 A1045 一节，共 ~35 条）

- **(3)**：剪贴板仍是哨兵 + **真开出了 `Generic Options Panel`**；删掉「`还没实现` 告警」那半。
- **A1045①** 内容/兄弟序（**0 = 群组、1 = 全局，对调必红**）/ 标题 / `LocalizeText` / 无锚点档 `Name` 646.70-683.30；
  **②** 两颗 `AutoClose` + **都挂了回调**；**③ 三道门拨 4 档**（`false/true` × banned 组合），
  期望值**按原版两个表达式现算**；**⑤** 点完面板自己关了、剪贴板仍没动；
  **⑥** 门关着那颗点了 ⇒ **没有**「发送口不在」+ 抓到 `Interactable = false` 那条出声 + 面板**没关**。
- 🔴 **灭自证（④）**：**真点两颗**，抓各自那条出声，断第 0 颗 = `ChatRoom.Alliance`、第 1 颗 = `ChatRoom.Global`
  （且互不含对方的词）⇒ **两颗对调必红、两颗都留空也必红**（后者会落到「没有挂回调」那支，一条都抓不到）。
  另断两条出声**不是同一条**。
- **前提与结论分条**，破了会点名（「面板里正好 2 颗钮」破了则整节跳过，不拿 0 颗索引把后面几百条一起掐掉）。

---

## 四、顺手发现（只报不改；已立账的标明）

1. 🔴 `Shell/WindowsManager.cs:1401` 仍写 `GenericOptionsPanel` 的「**本地没有任何调用方**」——
   **已过期**（同 `:1543` 附近那句）。该文件当时在**黑名单**（另一写手占着）⇒ **主对话派人改**。→ **`A1718`**
2. 🔴 **`WindowButton.GrayTargets()` 对九宫格只灰到 `qs[0]`（1/9）**：
   `GrayTargets` = 「命中区子树 + 那**一颗** `target`」，而 `MenuDraw.Nine` 的 9 块是 `target` 的**兄弟/后代**；
   且 `WindowButton` 挂在**透明命中区**上、可见底图是**兄弟**（原版是「按钮子树里所有 `Graphic` 都灰」）。
   ⇒ 本扇**故意不设** `wb.Interactable`（半灰比不变更糟），改走第二道闸；要完全照原版得先修 `Shell/WindowButton.cs`。
   → **`A1719`**
3. `MenuDraw.Nine` 那 9 块里只有 `qs[0]` 会被 `GrayTargets` 看见（与 2 同根因），
   而 `BindNine` 的悬停换图**换的是全部 9 张** ⇒ **灰/亮两条路不对称**。
4. 🔴 `OptButton.Interactable` 的**旧 doc** 写着「置 false 时**并出声**」，
   **实测既没有闸也没有出声**（置 false 的钮照样触发 `OnClick`）—— 真缺陷，本轮已补（行为那半）。
5. ⚠️ **裁定 2 的口径要微调表述**：不是「原版收 `Transform` ⇒ 判据空」，而是
   「**它确实转发给 `Context.AnchorPosition`**（`Context___ctor` 第 3 个实参，**可读**）；
   **空的是那个 mount point 的值** —— **谁调 `ShareDeckOnChat(Transform)` 跨 CAB 查不到**」
   ⇒ 按裁定取 `null`，并把这句如实写进代码注释。

---

## 五、要跑哪几个自检宿主（写手没跑 Unity，红线）

- **必跑 `CollectionScene.Run`**（新加的 A1045 + 改写的 A1051(3) 在这）。
- **必跑 `ShopScene.Run`**（改了 `Shell/GenericOptionsPanel.cs`，它的断言宿主在 `Editor/ShopScene.cs:4222-4346`）。
- `Shell/DeckInfoPopup.cs` 还被 `ShellScene` / `MainMenuScene` / `DeckScene` / `RewardsScene` / `BattleScene`
  的断言盯着，且 `Core/Loc.cs` 是**共用件** ⇒ 这一批属**跨面 + 动共用件**，
  按铁律 12 ⇒ **建议直接跑全套 12 条**（由主对话在同步点跑）。

---

## 六、没查清的

1. 原版 `b__30_2.c` 里那个常量频道串（`DAT_18423a130`）的**字面值没解**
   （要按 `CLAUDE.md` 那条 RVA → PE 节表的路读 `GameAssembly.dll`）；
   本趟用 `ChatPanel.Channels[(int)room]`（= `Global`/`Alliance`，全仓现成那一份），
   并在注释标明「**不是原版那份 id**」。
2. **谁调 `ShareDeckOnChat(Transform optionsMountPoint)`**（那颗钮的 `UnityEvent` 挂点）**没查到**
   ⇒ 锚点走原版「无锚点」那一支。
3. **没跑 Unity** ⇒ 「`ClickForTest()` 是否真跑到 `OnButton`」「批次里那两条出声是否真被抓到」
   **未经实跑验证**（判据链与既有 A1051 同一手法，先例是绿的）。
