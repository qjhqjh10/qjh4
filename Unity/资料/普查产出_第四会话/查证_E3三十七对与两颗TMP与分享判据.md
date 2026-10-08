# 查证：E3 三十七对 · 两颗原版 TMP · 分享判据

> 只读查证（⛔ 未改任何生产代码 / 正本 · 未跑 Unity / typecheck · 未碰 git）。权威顺序照本项目。结论以**本文件写作时刻的现读**为准（此刻有多个写手在改 `Core/Loc.cs` / `Shell/MenuDraw.cs` / `Editor/*.cs`）。

## 第一节 · `A964` 的 `E3` 三十七对

### 1·1 先纠一个数：**37 条 ≠ 37 对，去重后是 12 对**

- `shell.log` 里 `A964·E3：` 行 = **37**；`hitprobe/shell_hits.tsv` 里 `LeaderboardWindow` 行/去重路径 = **51/17 = 恰好 3 倍**；12 条 `Army Content/*/Hit` 路径**每条恰好 3 行**；`ChatPanel` 六条路径**每条恰好 4 行** ⇒ 去重后 = **11 对（army）+ 1 对（聊天）= 12 对**。
- army 那 3 次读数**hit 矩形逐位相同**（`SaimHann x1=493.71` · `Goff 371.35` · `Genestealers 860.79` 各 3 次）⇒ 是**同一版面被重扫**，不是三种状态。
- army 只有 **11 对**：可见 `i=0..11` 这 12 颗（`i=12` 被 `RebuildArmyButtons` 的 `MenuScroll.Intersects` 裁掉，`r.x1=1717.31 > ArmySelR.x2=1671.01`）⇒ 相邻对 = 12−1 = **11**；日志里像有 12 个不同串，是**同名对两种写法**（`TauEmpire⇄Genestealers` / `Genestealers⇄TauEmpire`）造成的假象。
- ⚠️ **×3 / ×4 的根因没查清**：「场景里 3/4 个同类实例」与「同一实例被 `ScanScope` 扫 3/4 次」都给出逐位相同的 TSV 行，本轮**判据不足**（不能跑 Unity）。**不影响下面结论**，但**别把 `37` 当缺陷数写进正本**（写「12 对，army 11 / 聊天 1」），否则白名单断言会被「扫了几次」左右（flaky 源）。

### 1·2 逐对判（12 对）

`Army Content` 的 `armies[i]` 序号→名字（由 TSV 的 `x1` 反算 `i=(x1+68.18−248.99)/122.36` 实读）：`0 Ultramarines · 1 Goff · 2 SaimHann · 3 BlackLegion · 4 DarkAngels · 5 Genestealers · 6 TauEmpire · 7 Sautekh · 8 AstraMilitarum · 9 Leviathan · 10 Sororitas · 11 EmperorsChildren`（第 12 颗被裁）。

| # | 队列 | 两颗节点 | 交叠 | 判定 |
|---|---|---|---|---|
| 1–11 | 3520 | `…/Army Content/<A>/Hit` ⇄ `…/Army Content/<B>/Hit`（(A,B) = 上述序号的**相邻 11 对** `(0,1)…(10,11)`） | **14.00 × 110.95 px**（各报 3 次） | **真重叠 ⇒ 我们的缺陷**（1·3） |
| 12 | 3308 | `…/Enter Text/InputField (TMP)/InputHit` ⇄ `…/Enter Text/Button/Hit` | **23.50 × 26.53 px**（各报 4 次） | **原版就这样 ⇒ 无害**（1·4） |

### 1·3 army 那 11 对 = 真重叠（**我们的**），不是「原版就这样」

要拆成两件事，原版只占前一件：

1. **版面确实叠 14px —— 原版实据**：`menu_dump.py bundle_menus_assets_all "RankedSkirmishLeaderboardPopup" --depth 6` ⇒ `Army Content … HorizontalLayoutGroup,ToggleGroup,ContentSizeFitter **spacing=-14.0** align=4`；子件 `Army Item Button` 136.36×121.59 ⇒ 步进 122.36 ⇒ 相邻恒叠 14.00px。我们的 `ArmyBtnSpacing = -14f`（`Shell/LeaderboardWindow.cs:176`）**是对的** ⇒ **画出来**的叠法是原版。
2. 🔴 **但「可点区域」原版不叠 —— 这 14px 是我们 `Hit` 放大出来的**。逐颗实读原版可射线图形：
   - `Army Item Button` **根上没有任何 Graphic**（`GameObject/Army Item Button.json` 组件只有 `RectTransform · CanvasRenderer · EverguildToggle(-6020787132085273659) · ArmyItemContainer(-1291534667684480059)`）。
   - toggle 的 `m_TargetGraphic` = pid `3767153502029970373`，落在 `Icon` 这个 GO 上，**`m_RaycastTarget=1`**、`m_AnchorMin=(0.08067,0.08222)`/`m_AnchorMax=(0.92667,0.92600)`/`m_SizeDelta=0` ⇒ **115.36 × 102.59**（与 `menu_dump` 的 `Icon` 那行逐位相同）。其余可射线件：`HighlightBG`（136×122，**只在选中时可见**）· `Badge Highlight`+`OneText`（35×35，x∈[−61.7,−26.7]）· `Arrow`（挂 `HighlightBG` 下）。
   - **算式（把徽记也算进去，结论不变）**：第 i 颗最右 = `cx+58.19`；第 i+1 颗最左 = `cx+122.36−61.7 = cx+60.66` ⇒ **不相交（差 2.47px）**。
   - 我们的 `MenuDraw.Hit(node,"Hit", r, QHit, …)`（`Shell/LeaderboardWindow.cs:545`）用的是**整颗根矩形** ⇒ 宽出 **21.00px**、高 **19.00px** ⇒ 相邻叠 14.00px。**这 14px 是我们的 `Hit` 造的。**
   - ⚠️ 唯一没坐实的一环 = `Badge Highlight` 出厂是否就活（`menu_dump` 那行没有 `INACT` ⇒ 判它活）；**算进/不算进都不相交** ⇒ 不影响结论。

### 1·4 聊天那一对 = 原版就这样 ⇒ 无害

- 原版 prefab（`menu_dump … "ChatPanel" --depth 8`）：两颗是 `Enter Text` 的**兄弟** `Background(0) → InputField (TMP)(1) → Button(2)`；`Enter Text` 的 VLG `pad=40,40,20,20` ⇒ `InputField` 653.9→**1773.9** · `Button` **1750.4→1790.4**（40×40）。
- 命中的就是我们那两个常数（`Shell/ChatPanel.cs:89-90`：`InputR 653.88→1773.88` · `SendBtnR 1750.38→1790.38`）⇒ **x 交 23.50**；两者**垂直同心**（中心都 992.735）⇒ y 交 = 输入框整高 **26.53**。**与 E3 报的逐位相同**。
- **歧义确定**：`Button` 是最后一个子件 ⇒ 同队列同 Canvas 下它压住 `InputHit`，重叠带上点的是**发送键**；我们建树顺序一致（`ChatPanel.cs:228-290`：`InputField (TMP)` 在前、`Button` 在后）⇒ **行为逐位相同**。

### 1·5 处置表 + 断言该怎么落

| 对象 | 处置 | 依据 |
|---|---|---|
| 聊天那一对 | **加白名单**（附「原版 VLG pad + 兄弟序」出处） | 1·4 |
| army 那 11 对 | **要修（铁律 11：要做，不是不做）** —— 把 `RebuildArmyButtons` 里 `MenuDraw.Hit` 的矩形从整颗 `r` 换成 **`Rel(r, ArmyIconR)`**（= 原版那颗唯一常驻可射线的 `Icon`，115.36×102.59）。⛔ `ArmyBtnSpacing` 别动（−14 是原版版面） | 1·3 |
| 探针本身 | 报数先按 `(队列, 路径A, 路径B)` **去重**再落白名单（否则 ×3 是 flaky 源） | 1·1 |

**要落断言 —— 四条（都分得出「真重叠」与「同族并列」，无一条恒真）：**

- **A · 白名单封闭**：`E3` 去重后的集合 **⊆** 白名单（今天应恰好 = `{队列 3308 聊天那一对}`）。**多一条就红** = 回归守卫。
- **B · 白名单不腐烂**（与 A 配成「**恰好相等**」，这就是它不恒真的原因）：白名单每一对**必须仍在报**；哪天它不再重叠 ⇒ 版面被改过、白名单该删 ⇒ 红。
- **C · 灭自证**：不看「有没有重叠」而看**结构式** —— 断 `army 那颗 Hit 的宽 == 115.36`（原版 `Icon` 宽·出处进注释）**且** `ArmyBtnSpacing == -14f`。⚠️ 只断「A 不再多报」不够：把 `Hit` 改回整颗根矩形 **+ 同时**把那一对塞进白名单，A、B 会**一起变绿**；C 断的是「命中区按 `Icon` 摆」，两边一起改回去就裂开。
- **D · 判别式夹具两态**（照 `A964` 现有两条判别式的形制，**一报一不报**）：态一「真重叠」= 同队列、兄弟、两颗命中区各 100×100 **完全重合** ⇒ **必须报**；态二「同族并列」= 同队列、兄弟、两颗宽 **115.36**、步进 **122.36**（照原版 `Icon`） ⇒ **必须不报**。⇒ 这一对的差就是「探针真的在比几何」的证据。

## 第二节 · `A1043` 的两颗原版 TMP（`Instructions` / `label`）

**原版实读**（`menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 4`）：

| 路径 | 文本 | 字号/对齐 | 出厂 |
|---|---|---|---|
| `… > Content Area > Header > Instructions` | `'\n'` | 36/36 · Right/Middle | **INACT** |
| `… > Content Area > Header > label` | `'Edit your deck'`（带 `Localize`） | 38/38 · auto[30~38] · Right/Midline | **INACT** |

**结论（逐颗）：我们【两颗都没建】⇒ 与原版一致。**

- `Instructions` **没建**：`Deck/DeckRuntime.cs` 全文 `instruction`（不分大小写）**0 命中**。
- `label` **没建**：全仓 `grep -rn "Edit your deck"` **0 命中**；`Deck/DeckRuntime.cs` 对 `"label"` **0 命中**。
- 我们 `BuildHeader()`（`Deck/DeckRuntime.cs:913` 起）建的是 `hdr_sep · hdr_back · hdr_back_t · hdr_fltbtn · hdr_flticon · hdr_fltlbl · hdr_clear · hdr_clear_t · hdr_wcbg` + 野卡四图标/计数 —— **没有标题那一颗，也没有提示语那一颗**。
- **旁证**：用户实拍 `资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png` 页头只有「返回 / 过滤器 / 野卡计数(6·5·10·3)」，**`Edit your deck` 与提示语那行都不在画面上** —— 与 `act=F` 自洽。

**⚠️ 两条必须一起记（否则下个会话会误判成「永远不显示」）：**

1. `DeckEditingHeader.SetInstructions(string)` 存在（`decomp_full/DeckEditingHeader__SetInstructions.c`；签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DeckEditingHeader.cs`），但方法体是**纯 setter** —— 只对字段 `+0x28` 那颗 TMP 调一次 `set_text`，**没有任何 `SetActive`** ⇒ 就算有人喂文本也点不亮它。**且查不到调用点**：`grep -rln "SetInstructions"` / `"DeckEditingHeader"` 在 `decomp_full/` 只命中它自己那个文件（`DeckEditingWindow` 的字段表里也**没有** header 引用）；`assets_full/bundle_menus_assets_all` 全目录 grep `SetInstructions` **0 命中** ⇒ **不是 UnityEvent 挂的**。⚠️ 那个 `.c` 的函数名印成了 `ChatMessageHeader__UpdateTimer`（反编译器认错符号）—— **文件名才是权威**，别被它带跑。
2. `label`（1320→1820）与我们**已有的** `hdr_clear`（`DeckRuntime.cs:73` = 1218.6→1468.6）**矩形相交**；原版靠 `act=F` 躲开 ⇒ **将来要把 `label` 照原版建出来，必须一并处理这个相交**（不能只加一颗字不管压谁）。

## 第三节 · `A1040` 「分享卡组」两处不一致 —— 判据已读通

**反编译第一权威 `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeck.c`（产物完整，不是缺失）**，逐句：① `uVar1 = DeckInfoPopup__MakeDeckString(param_1, 0);` —— 先造卡组串；② **`UnityEngine_GUIUtility__set_systemCopyBuffer(uVar1, 0);`** —— **写系统剪贴板**；③ `UIMessageController__ShowMessage(…, DAT_1842d07e0, 1, 0);` —— 再**弹一条消息（toast）**。

⇒ **原版 = 「写剪贴板 + 弹一条提示消息」**：没有确认弹窗、没有平台分享、没有「先问再写」。API = **`UnityEngine.GUIUtility.systemCopyBuffer`**（不是 `TextEditor`、不是插件）。旁证：全库 `systemCopyBuffer` 只三处 —— 本件 · `ProfileTab…b__0.c` · `Reporter__drawToolBar.c`（Unity Reporter 调试工具，与玩法无关）。那条消息的**文案没解出**（`DAT_1842d07e0` 是元数据指针、不是字符串字面量，要读它得走 Il2Cpp metadata），**不影响结论**。

**同族另外两件（要分清，别混成一个动作）：**

- `DeckInfoPopup__ShareDeckOnChat.c` = `Share On Chat`：开一扇 **`GenericOptionsPanel`**（两键：群组 / 全局，由 `AlliancesManager.IsInGroup` 与 `PlayerDataManager.IsBannedFromChat` 决定可用），选中后走 `g__HandleShare_30_0` → `CardDeck.Serialize()` → **`ChatGlobalManager.ShareDeck(串, 目标)`**（`ChatGlobalManager__ShareDeck.c` = `CreateChatEvent(0x96)` + `SendChatMessage`）⇒ **发聊天消息**。⇒ **`Share On Chat` 不写剪贴板**，与 `Share` 是**两个不同的动作**。
- 全库**没有** `get_systemCopyBuffer` 的读法 ⇒ 原版没有「从剪贴板导入」。

**哪一份是正解 / 另一份怎么改：**

| 实现 | 判定 |
|---|---|
| `Deck/DeckRuntime.cs:3887` `ShareDeckString()`（剪贴板 `:3889` + `Say(…)` `:3890`） | ✅ **正解**，与原版逐句对齐（剪贴板 = 写法；`Say` = 那条消息）。⚠️ 它的 doc（`:3877`）写「原版…**就一句**」——**少了第 3 句 `ShowMessage`**，就地订正措辞（行为不用改）。 |
| `Shell/DeckInfoPopup.cs:1363` `ShareDeck(key)`（弹窗、**不写剪贴板**） | 🔴 **偏离**，要改：① `key=="Share"` 那一路**改成写剪贴板**（照 `DeckRuntime.ShareDeckString`）；② 两处**收口成一份**（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」—— 抽 `static void CopyDeckToClipboard(deck)` 供两边调，⛔ 别再抄一份）；③ ⛔ **`"Share On Chat"` 那一路不许跟着写剪贴板**（原版不写）。 |

**改 `Share On Chat` 时要一并做的（铁律 11：要做，只有先后）：** 现在它只弹一句「发不出去」（`Shell/DeckInfoPopup.cs:1374`）。原版那是**一扇两键的目标选择面板** —— **面板本身可复刻**（`GenericOptionsPanel` 两键 + 两条禁用条件 `IsInGroup` / `IsBannedFromChat`，实据 = 那两个 `cVar4 != '\0' && cVar3 == '\0'` / `cVar2 == '\0'`），**只有最后一步发消息不可复刻**（我们无 `ChatGlobalManager` 对等物）⇒ 应记成「**面板要建 + 最后一步出声**」两笔，别一句话带过。另：它那句「原版是**平台分享**」（`:1375`）**与事实不符**，要改。

**⚠️ 还没查清（不拿推断顶替）：** `GUIUtility.systemCopyBuffer` 在 `-batchmode -nographics` 下**能否真写进系统剪贴板** —— 本轮红线禁止跑 Unity，**判不了**。⚠️ **这不构成「不能做」的理由**（原版就是调它，照调即可）；而且 `DeckInfoPopup.cs:1361` 那句「**批处理与桌面都没法替用户按剪贴板**」**已被同仓 `DeckRuntime.cs:3889` 直接反证** ⇒ 那句理由**本身是错的**，无论如何都要删。

## 顺手发现（⛔ 都没自己改）

1. **`E3` 报的 `37` 会误导记账**（1·1）：真值 **12 对**；`×3`/`×4` 重扫根因**没查清**，落白名单前**必须先去重**。
2. **`DeckRuntime.cs:3877` 那句「原版就一句」少了 `ShowMessage`**（第三节）—— 订正措辞即可，行为无差。
3. **`A1032` 交件 §⑥ 的尾巴可销**：它写「我们有没有建这两颗，未在本件范围内核（留给后续）」—— 本件已结（**都没建 ⇒ 一致**）。
4. `label`(1320→1820) 与 `hdr_clear`(1218.6→1468.6) **在 Header 里矩形相交**（原版靠 `act=F` 躲开），已写进第二节当「将来建 `label`」的前置条件。
5. 全库 `systemCopyBuffer` 只有三处，其中一处是 Unity `Reporter` 工具的「复制」钮 —— 与玩法无关，别当功能。
