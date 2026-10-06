# Block8 · `Shell/` 杂项 6 条（A525 · A526 · A527 · A549 · A612 · A810③）（2026-10-14）

> 写手代理产出。**独占的四个文件 = `Shell/{ChatPanel,CollectionWindow,LiveOpsEventWindow,InboxWindow}.cs`**（本批只改了这四个）。
> 判据来源：`资料/普查产出_1013/批次计划_1013.md` **§六·B** 的对应行（A525/A526/A527 `:212-214` · A549 `:236` · A612 `:299`）
> + §六·B 末那段「并入 A810」的第 7 项 `:641`；逐条细节回到来源报告
> `W403_A414_对齐与字号.md` §七·1–4 · `WE4_卡组库静默失败.md` §六·3/4 · `WSmall3_选中断言与两条尾巴.md` §六·1 · `D1013_诊断_块4_Shop与Rewards.md` §六·2/§31。
> ⛔ 没跑 Unity · 没动 git · 没改两张正本 · 没碰白名单外的文件 · ⛔ 没动 `ChatPanel.cs` 那行 `QBase` 收窄（主对话刚落的）。
> ⚠️ **本文件里「改前 `:N`」= `git show HEAD:` 那一版的坐标**（本批开工时工作区与 HEAD 只差 `ChatPanel.cs` 的 `QBase` 那 4 行注释）；
> 「改后 `:N`」= 本批落地后的坐标。**`ChatPanel.cs:698` / `CollectionWindow.cs:2694-2701` / `LiveOpsEventWindow.cs:962-975` 这三个账上写的行号都是立项时的**，现读各不相同（见逐条）。

---

## 一、逐条

| A 编号 | 改了什么（文件:行） | 判据 | 做完没有 |
|---|---|---|---|
| **A525** | `Shell/ChatPanel.cs:749`（改前 HEAD `:721-722`、A 表立项时 `:698`）：`ChatMessageRow.Build` 里那颗 `Message` 的调用**补 `alignLeft: true`**；注释块 `:742-748` 如实记「注释对、代码不对」+ 判据命令与那一行 dump 原文 | 走的是**静态** `Text`（定义 `:659`，缺省 `alignLeft = false, alignRight = false`）⇒ 原来两侧都没传 ⇒ 标签留在 `MenuDraw.Text` 摆的**矩形中心**（画成居中）。原版 = **`Left/Top`**：`python d:/4/Unity/工具/menu_dump.py bundle_mainmenualwaysloaded_assets_all "ChatMessageRow" --depth 8 --md` ⇒ `\| ·1 \| Message \| … \| 'This is the message' 字号=22.0 基准=22.0 对齐=Left/Top 折行=1 色=(1,1,1,1) \|`（W403 亲跑，`W403_A414_对齐与字号.md:238-241`）。**同批顺手订正** `:246-249` 那句已经过期的「**不在本批三条账上，只报不改**」（改成「✅ A525 已补」+ 保留更正痕迹） | ✅ |
| **A526** | `Shell/ChatPanel.cs:251-264`（改前 HEAD `:246-247`）：输入框占位那颗的返回值**接出来**（`var ph =`），随后 `ph.SetWrappingMode(3)` + `MenuDraw.AlignLeft(ph, InputR)` | 原版这颗 **`折行 = 3`（`PreserveWhitespaceNoWrap`）**，我们走 `MenuDraw.TextBox` ⇒ 恒落 `Normal(1)`。判据 = A493#10 那一行 dump 原文（逐字，本批重读，与 `W403_A414_对齐与字号.md:257-259` 一致）：`\| ······6 \| Placeholder \| … \| 'Type message' 字号=28.0 基准=28.0 对齐=Left/Middle 折行=3 色=(1,1,1,0.439) \|`。⛔ 没用 `SetWrapping(false)` 顶替（那是 `0` 档，见 `Battle/Label.cs` `SetWrapping` 头那条）。**顺序按那个口的文档**：`SetWrappingMode` 内部会 `ForceRelayout()`（A205）⇒ 对齐压在它**之后**（同形先例 `Deck/DeckRuntime.cs:3450-3456` 的 `SetWrappingMode(3)` → `AlignLeftOn`） | ✅ |
| **A527** | `Shell/ChatPanel.cs:670-677`（改前 HEAD `:649`）：把「它的**四个**调用点」订正成「**3 处**」并写清是哪三处、以及 A406 立项→A435 阶段 2 的行号漂移；**结论那半句保持原样**（三处都是 `ChatMessageRow` 的件 ⇒ 不上 `autoMax/base` 两格） | 现读 `ChatMessageRow` 内那个静态 `Text` 的调用点 = **3**（`Sender :722` / `Time :724` / `Message :749`）；全仓 `grep "ChatMessageRow.Text("` **零命中**（没有类外调用）。纯注释订正，⛔ 零行为变化 | ✅ |
| **A549①** | `Shell/CollectionWindow.cs:2762-2780`（改前 HEAD `:2767` 那一行；A 表立项时 `:2694-2701`）：`CreateDeck()` 拿到空串时**改打一条 `LogWarning`**（「新建卡组**没写进存档**…重启就没了 —— 原因见上一条 `[CollectionData]` 警告」），拿到名字才打原来那句；**补一段 doc 说明为什么只改措辞** | 账上原文：「拿到空串时日志会打「新建卡组**「」**」… **功能没坏**（那一套确实在内存里，列表也该重建），**只有那半句日志**」（`批次计划_1013.md:236` + `WE4_卡组库静默失败.md:188`）。⇒ **重建照旧无条件跑、不加 `return`**（同 A609 的裁定：只出声、不改玩家可见流程）；原因**不另写一套**（`CollectionData.CreateDeck` 自己已经 `LogWarning` 过一遍，见 `Shell/CollectionData.cs:245-250`） | ✅ |
| **A549②** | `Shell/LiveOpsEventWindow.cs:996-1000`（doc 段）+ `:1018-1027`：`CreateDeckInMode` 的「新建失败 ⇒ `PendingEditDeck = -1`」那一支**在注释里如实记**（含「行为仍良性」的判据与出处），并按 A612 拆支（见下行） | 账上原文：「拿到空串 ⇒ `PendingEditDeck = -1`，**静态核过是良性的**（`DeckRuntime:461` 不吃交接 ⇒ 落 `Library.Current` = 刚建那套，同一副）⇒ **只需在报告/注释里如实记**」（`批次计划_1013.md:236`）⇒ 本件**没把它当「要改的」**，只落注释 + 保留原行为 | ✅ |
| **A612** | `Shell/LiveOpsEventWindow.cs:1008-1034`：**把「新建失败」与「选中失败」两支拆开** —— `bool created = !string.IsNullOrEmpty(name);`；`idx = created ? IndexOf(name) : -1`；`created` 那一支才 `if (!CollectionData.Select(idx)) LogWarning(… LastSelectError)` + `PendingEditDeck = idx`；`!created` 那一支**显式 `PendingEditDeck = -1`** + 一条 `LogWarning`；尾部那句 `Debug.Log` 也改成不打印 `「」` | 账上原文：`idx` **可能是 −1**（`CreateDeck` 失败 ⇒ `IndexOf("")`）… **行为仍良性**，但 **返回值一旦有人读，就必须同时处理「新建失败」那一支** —— **⛔ 别只把 `if (!Select(idx))` 抄过去**（`批次计划_1013.md:299` + `WSmall3_选中断言与两条尾巴.md:208-211`）。⇒ 本件的形状：**先判 `created`，再判 `Select`**（把「压根没建」与「建了没选中」分开出声，⛔ 没把两者都塞进 `Select` 的返回值）。⚠️ **「要不要拦下来」不在本件裁**（A613/A609 一族，照旧进编辑器、不 `return`）。⚠️ 失败支里 `PendingEditDeck = -1` **照旧显式写**（⛔ 不能省：留着上一轮的旧下标会跳进**别的**卡组） | ✅ |
| **A810③** | `Shell/InboxWindow.cs:352`（改前 `:339`，**账上的行号就是它**）：`hit.Bind(baseQ, null, "40k_general_bt_yellow_hover")` → **第一格传真名字** `ArtCloseBg`（= `UI_Button_Round_background`）；`:339-351` 写清后果、判据命令与两处如实标注 | 账上原文：「`InboxWindow.cs:339` 把常态图名传成 `null` ⇒ 按下图审计日志里记下一条**认不出是谁**的 `" → "`」（`批次计划_1013.md:641` + `D1013_诊断_块4_Shop与Rewards.md:287-288`）。判据 = **本件亲跑**：`python 工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6` ⇒ `Generic Close Button Orange … **UI_Button_Round_background** 237×237 … \| trans=2 target=5609434692533257010 interactable=1 \| **HL=40k_general_bt_yellow_hover P=40k_general_bt_yellow_pressed**` ⇒ 被换的就是这颗圆底、它的常态图名 = `UI_Button_Round_background`。⚠️ 两处**如实标注**（都在注释里）：① 本档没有对应按下图（`PressedNames` 表里也没这一条）⇒ 表里仍留一条**有名字**的记录，属「如实出声」那一档；② 原版 `P=40k_general_bt_yellow_pressed`（**本地有这张图**）我们**没接**（与同族 5 颗关闭钮一致，走 `Press()` 的「退回高亮图」），不在 A810③ 口径内 | ✅ |

**合计：点名的 6 条全部落实（A549 含①②两半）= 6 处改动 + 2 处过期注释订正，零遗留。**
⚠️ **A810 的①②不在本件**（`Editor/ShopScene.cs` 三套缺图口径 / `MaxQuadBottomPx` vs `GUnion`）—— 归别的写手，本件**一个字没碰** `Editor/`。

---

## 二、顺手发现（⛔ 只报不改）

1. 🔴 **A810③ 那个形状不止 `InboxWindow` 一处，§六·B 第 7 项只点了它一个。** 同形的
   `MenuDraw.Hit(<钮>, …, <目标 quad>, **null**, "40k_general_bt_yellow_hover")`（`art` 传 `null` + 显式 `hoverArt`）
   **现读还有 5 处**，它们同样会在 `WindowButton.MissingPressedArt` 里记 `" → "`：
   · `Shell/ChatPanel.cs:281`（聊天窗关闭钮 —— **我这个文件的**，但不在这 6 条账上，⛔ 未动）
   · `Shell/BoosterInfoPopup.cs:335` · `Shell/DeckSelectionPopup.cs:397` · `Shell/LeaderboardWindow.cs:641` · `Shell/PlayerProfileWindow.cs:372`
   ⇒ **推论（对账用）**：`MissingPressedArt` 那张表**去重**（`PromptPopup.cs:620` 的 `Contains`）⇒ 这几处**共用同一条 key**
   `" → "` ⇒ **光改 `InboxWindow` 一处，审计日志里那条 `" → "` 仍可能由这 5 处里的任意一处产出**（谁先跑到谁写进去）。
   要真清干净：**逐处补常态图名**（一行一处，同本件），或者**动共用件的记录口径**（`Bind` 跳过空 `art` 那条）——
   后者 `D1013_诊断_块4_Shop与Rewards.md:294` 自己写着「**不在本批白名单，另立账**」。**建议：另开一条账，把这 5 处一起收。**
2. 🟡 **同族 5 颗关闭钮的「按下图」我们是落着的，原版有。** 上一条那 5 处（以及本件改的 `InboxWindow`）都**没传 `pressedArt`**，
   而原版那一格逐颗都有值（本件 dump：`P=40k_general_bt_yellow_pressed`；`PurchasePremiumWindow.cs:569` / `MissionsTab.cs:615` /
   `AllianceMemberTab.cs:1177` 那三颗**是传了的** ⇒ 同族两种口径并存）。**本地有这张图**（`Resources/Art/ui_menu/40k_general_bt_yellow_pressed.png`）
   ⇒ 传上去之后：① 按下时画面换的是原版那张图（今天退回高亮图）② `MissingPressedArt` 那条**直接消失**。
   ⛔ 不是 A810③ 的口径，本件没动手；**建议另立一条账「同族 6 颗关闭钮补 `pressedArt`」**。
3. ℹ️ **`CollectionData.Select(-1)` 原来会在 `CreateDeck` 失败时多打一条误导告警**：`Select(-1)` 走越界支 ⇒
   `LastSelectError = "**没选中** —— 下标 -1 越界（库里现在 N 套）"` + `Debug.LogWarning("[CollectionData] 选中卡组失败：…")`
   （`Shell/CollectionData.cs:94-99`）—— 那句读起来像「选不中已有的一套」，其实是「压根没建出来」。
   A612 的改法顺带把它消掉了（失败支不再调 `Select`）。**记在这儿**：`Select` 的越界文案本身没错，是**调用时机**误导。
4. ℹ️ **`LiveOpsEventWindow.CreateDeckInMode` 的失败支**在自检里**零覆盖**（全仓 `grep CreateDeckInMode` 只有
   `Editor/MainMenuScene.cs:4211` 一处，走的是**成功**支；`PendingEditDeck` 那条断言 `:4217` 也只在成功支成立）。
   要造失败支得先有个「写盘必失败」的夹具 —— 与本件无关，**只报不改**。
5. ℹ️ **`ChatPanel.cs` 里那句自称「四个调用点」的注释是 A406 留下的**（`:670-677`，本件已订正成 3）。
   同类的「数字注释」在同一个口上还有一层风险：**行号会漂、个数会变**（A403 那条注释自己就写着「按内容找，别按行号」）
   ⇒ 建议以后这类注释写**节点名/文本内容**，别写数。

---

## 三、没做完的（+为什么）

**无。** 点名的 6 条全部落地。

⛔ 明确**不在本件范围**、也没动的（都在 §二 里报了）：
- **A810①②**（`Editor/ShopScene.cs` 那三套缺图口径 / `MaxQuadBottomPx` vs `GUnion`）—— 文件归别的写手。
- §二 第 1/2 条那 5 处**同形**（含我自己白名单里的 `ChatPanel.cs:281`）—— 不在这 6 条账上，**只报不改**。

---

## 四、类型检查结果

```
（一）ChatPanel.cs 改完 · （二）另外三个文件改完 · （三）最后一次复跑：
$ TMPDIR=/tmp/wf_b8 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（前三次都是 **0/0**。）

⚠️ **中途有一次不是 0**，如实记下来（简报里点名的那种情况）：
```
Assets\CardPresentation\Battle\Label.cs(723,13): error CS0103: 当前上下文中不存在名称"EnsureFontSizeBase"
运行时错误数: 1
```
—— **全部集中在 `Battle/Label.cs`**，**不是我负责的四个文件里的任何一个**（那个文件归调度台/别的写手，本批在飞）
⇒ 按简报「报错全集中在别人的文件上 ⇒ 不是你的问题，重跑一次写进报告」处置：**隔 20 秒复跑 ⇒ 0/0**
（那一处是别人写到一半的半成品，已闭合）。⛔ 我没去碰那个文件。

---

## 五、行尾与规模（铁律那三条核对）

| 文件 | HEAD 版行尾 | 改后行尾 | `git diff --numstat`（+/-） | 改后行数 |
|---|---|---|---|---|
| `Shell/ChatPanel.cs` | LF（`CRLF=0 / LF=734`） | **LF**（`CRLF=0`） | **35 / 7** | 762 |
| `Shell/CollectionWindow.cs` | LF（`CRLF=0 / LF=2826`） | **LF** | **10 / 1** | 2835 |
| `Shell/LiveOpsEventWindow.cs` | LF（`CRLF=0 / LF=1102`） | **LF** | **32 / 5** | 1129 |
| `Shell/InboxWindow.cs` | LF（`CRLF=0 / LF=572`） | **LF** | **14 / 1** | 585 |

⚠️ 判据是**数出来**的（`io.open(p,'rb')`，`b.count(b'\r\n')` vs `b.count(b'\n')`），不是 `file -b -`（它对混行尾不报警）。
四个文件**改前改后都是纯 LF**，`numstat` 的数字与改动量相称（没有「整篇翻行尾」那种数字）。
全部改动都用 Edit 工具落的，⛔ 没使用 `sed -i` / python 文本模式写。
