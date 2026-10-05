# W-A · A314 + A318 + A331 + A322（写手 · 2026-10-11）

> **白名单三文件**：`Shell/CampaignRewardWindow.cs` · `Shell/BoosterInfoPopup.cs` · `资料/普查产出_1008/A220_A221_常量收窄与四条.md`
> **性质**：纯注释 / 纯文档 / 只读扫描 —— 三个文件里改的**全是注释与文档**，⛔ **没有一行可执行代码被动过**；
> ⛔ 没跑 Unity、没跑自检；⛔ 没动 git 写命令（只用了 `status` / `diff` / `show` / `log` 这几个只读的）。

---

## 一、结论（四件各一句）

| 件 | 结论 |
|---|---|
| **A314** | ✅ **改完** —— `Shell/CampaignRewardWindow.cs` 里那句「**仍不吃裁切的**：抽屉里那三层文字」**就地订正**（那一件 = **A302，2026-10-11 已做完**），带更正痕迹。**这是第三轮报它**（戊2、W1 各报过一次没人动）。 |
| **A318** | ✅ **改完** —— `Shell/BoosterInfoPopup.cs` 那句「`BoosterPackOpenWindow:95` 用 `QBase−1`」订正为 **`QShade`(3170)**；⚠️ **同句还有 3 处同类过期**（`QDsHit−1` / `QImpHit−1` / `QShade+1`，全是 2026-10-04 `A47`/`A25⑥` 那一批订掉的）⇒ **一并就地订正**（**超出 A318 字面范围**，理由与回滚点见 §三·2 末）。 |
| **A331** | ✅ **改完** —— §三·1 两处：① `ChatPanel` 那一行「= 0 处」的 6 个里**只有 `QBg` 可收**（后 5 个被同文件的 `ChatMessageRow` 用限定名引用 ⇒ 收了**当场 `CS0122`**）；② 「**8 个窗**」**点名到个**（顺带解掉 `A表核对_块2.md` §五·2 那个悬案）。 |
| **A322** | ✅ **扫完，隐患 = 0** —— **87 个 `M` 状态 `.cs` 逐个核过：HEAD 行尾与工作区行尾【全部一致】**。🔴 **但简报里那个前提现读不成立**：`Battle/Label.cs` **现在两边都是纯 CRLF**（见 §二·4）。 |

---

## 二、判据（逐条带 文件:行号 / 现值 vs 新值 / 出处）

### 2·1 A314 —— 「仍不吃裁切的」为什么过期

- **改前那一句**（`Shell/CampaignRewardWindow.cs:621-622`，改前行号）：
  「⚠️ **仍不吃裁切的**：抽屉里那三层**文字**（`MenuDraw.Text` 没有裁切形参，本库也没走 `MenuDraw.ClipText`）—— 那是**另一件欠账**（记在报告里，⛔ 本件没顺手加）。」
- 🔴 **判据（现读，A302 已落地）**：`Shell/ItemDrawer.cs` 里**新增了唯一一份 `ClippedText(...)`**（`static Label ClippedText(Transform node, PxRect r, string s, Color color, string name, …)`），
  它内部三步 = `MenuDraw.Visible` → `MenuDraw.Text` → `MenuDraw.ClipText(lb, st.Clip.Value, st.ClipSoftness)`；
  **三层文字的调用点全改走它**（阵营名条 `NameStrip` / 物品短名 / 数量 `"x"+qty`，数量那处还顺带把「先裁再挪」的次序倒过来了）。
- 该文件自己有一段 doc 把这件事写死了：`Shell/ItemDrawer.cs` 里 `ClipSoftness` 那段 —— 「**那一件已经做完了** …… ⇒ 文字与图现在吃**同一份** `Clip` / `ClipSoftness`。⛔ 别再照上面那句旧话写。」
- **现值 → 新值**：旧句「不吃裁切（另一件欠账）」 = ❌ 过期；新句「**A302 已做完**，三层文字与图吃同一份 `Clip`/`ClipSoftness`」 = ✅。
- **落地件**：`资料/普查产出_1010/戊2_RewardWindow_A302.md`（§五·1 就是「请调度台订正 `CampaignRewardWindow.cs:621` 那一句」）。
- ⚠️ **同段的相邻事实仍然为真**（没被我改掉）：`MenuDraw.Text` **确实**没有裁切形参 —— 收口靠的是新增的 `ClippedText`，不是给 `Text` 加参。

### 2·2 A318 —— `QBase−1` 那个号怎么没了

| 项 | 值 | 出处（现读） |
|---|---|---|
| 旧写法（**已删**） | `QCloseSurface = QBase − 1` = **3169** | `Shell/BoosterPackOpenWindow.cs:103-105`「🔴 **2026-10-04（A47 接线批）删掉了 `QCloseSurface = QBase − 1`（3169）**：「内容档 − 1」这个写法按规矩是错的……」 |
| 现写法 | **`QShade` = `QBase` = 3170** | `Shell/BoosterPackOpenWindow.cs:99` `public const int QBase = 3170;` · `:102` `public const int QShade = QBase;` |
| 调用点 | `MenuDraw.ShadeHit(CloseNode, CloseSurfaceR, QShade, QCard, Close, "Collider")` | `Shell/BoosterPackOpenWindow.cs:317` |
| 为什么仍安全 | `QShade`(3170) **严格低于** `QCard`(3171) ⇒ 卡照样能点 | `:106` `QCard = QBase + 1` · `:105`「仍**严格低于** `QCard` ⇒ 卡照样能点」 |

- 原句还带一个**行号** `:95` —— 那也早漂了（现读 `:99`/`:102` 是那两个 `const`、`:317` 是调用点）⇒ 本件**改成按符号认**。

### 2·3 A331 —— `ChatMessageRow` 为什么让那 5 个常量收不得

- **文件**：`Shell/ChatPanel.cs`（**同文件里有两个类型**：`class ChatPanel : GameWindowWithTabs` 与 `public static class ChatMessageRow`）。
- **限定名引用实测**（`grep -n "ChatPanel\." Shell/ChatPanel.cs` ⇒ **11 命中 = 7 处代码 + 4 处文字**）：

| 常量 | 代码引用处数 | 用在哪（按内容认） |
|---|---|---|
| `ChatPanel.QContent` | 1 | 建 `RowBackground` 那一行 |
| `ChatPanel.QHead` | **2** | `Sender` / `Time` 两行 |
| `ChatPanel.QFrame` | 1 | 建 `Border`（`Player_Profile_Border`） |
| `ChatPanel.QAvatar` | 1 | 建 `Profile content` |
| `ChatPanel.QText` | 1 | 建正文 `Message` |
| `ChatPanel.QHit` | 1 | `MenuDraw.Hit(pb, "Hit", …)` |

（另 4 处命中 = 文件头 `:1` · `:41` 的注释 · `:224` 的注释 · `:443` 的 doc 文字 —— **都不是编译依赖**。）
- **错因（口径洞）**：A220 的 0 引用扫描**排除了声明文件自己** ⇒ **「同文件里的第二个类」是唯一会漏的形状**（`const` 的可见性只在编译期存在、跨类引用**只能**写限定名）。
  它 §1·1 对**那 5 扇窗**逐个核过这一形状（`LeaderboardWindow.TabEntry` / `MissionRerollPopup.MissionRerollContext`），**§三·1 这张表没把同一条检查带上**。
- **可收的只有 `QBg`**：`ChatPanel.cs` 里它**本类内也只用非限定名**、外部也 0 引用 ⇒ **唯一一个两边都是 0 的**。
  ✅ **A252 已落地**（工作区现读：`const int QBg = QBase + 1;`，上面那段注释把原因写全了）—— 与本件口径一致。
- **「8 个窗」的点名（可复跑）**：`grep -rl "A47 接线批"` = **19 个 `.cs`**（复量仍是 19），其中 `Shell/` 侧 **15 个**；
  剔掉 **本件已处理的 5 个**（`BattleLogPopup`/`BoosterInfoPopup`/`DuelPopupWindow`/`LeaderboardWindow`/`MissionRerollPopup`）、
  **当模型的 `BoosterPackOpenWindow`**、**不是窗的 `MenuDraw`** ⇒ **剩 8 个**：
  `CardDetailPopup` · `DeckSelectionPopup` · `LiveOpsEventWindow` · `PlayerProfileWindow` · `PracticeModePopup` · **`RankedEventWindow`** · `SearchingMatchPopup` · **`SkirmishEventWindow`**。
  它们在表里**只占 6 行**（`LiveOpsEventWindow` 那行同时覆盖它的两个子类 —— `class RankedEventWindow : LiveOpsEventWindow` / `class SkirmishEventWindow : LiveOpsEventWindow`，各在该文件 `:18`，两文件**也都带那条注释**）；
  第 7 行 `Shell/ChatPanel.cs` **不在这 8 个里**（`grep -rl "A47 接线批"` **命中不到它**）⇒ 是表外的同族候选。**∴「8」与「7 行」本不矛盾。**

### 2·4 A322 —— 行尾扫描的判法 + 那两条**前提核对**

- **判法（照简报）**：HEAD 那版 = `git show HEAD:<路径>` 取字节；工作区那版 = **二进制读**（`io.open(p,'rb')`）数 `\r\n` 与 `\n`（**文本模式会把 `\r\n` 折成 `\n`、两个数恒相等 ⇒ 看着像纯 LF**，这条简报点名了，我照做）。
  **交叉验过一次**（防我这支口径自己错）：拿 `file -b -` 对 `Label.cs` / `ChatPanel.cs` / `BattleDoors.cs` 各比一次 HEAD 与工作区 —— 与字节计数的结论**逐条一致**。
- 🔴 **先证明了「这个隐患真的存在」**：`git config core.autocrlf` = **`false`**、`core.eol` 空、**`.gitattributes` 不存在**（`git check-attr -a` 对样本文件零输出）
  ⇒ **没有 clean/smudge 归一化** ⇒ 行尾翻转**确实会**让 `git diff` 变成整篇重写（CLAUDE.md 里记的那三次踩坑就是这么来的）。
- 🔴 **然后结果 = 0 个**：**87 个 `M` 状态 `.cs`，HEAD 行尾与工作区行尾【全部一致】**（表见 §四）。
- 🔴 **简报里那条前提现读不成立**（**这是本件最该带回去的一条**）：
  简报写「`Battle/Label.cs` 的 HEAD 是 CRLF（717/717）、而**工作区那版开工时已是纯 LF**」——
  **现读**：`HEAD` = CRLF **(717/717)**、**工作区 = CRLF (904/904)**（`file -b` 与字节计数两法一致），`git diff --numstat` = **`+199/-12`**（**干净**，若真是 LF↔CRLF 之差这里会是 ~`904/717`）。
  ⇒ 工作区**已经不是**纯 LF 了；W3 当时报的 `141/10` 也已被后续改动刷新成 `199/12`。
  ⚠️ **是哪一件把它写回 CRLF 的 —— 没查清**（工作区改动不入 `git log`），⛔ **不猜**（见 §五·1）。

---

## 三、改动清单

| # | 文件 | `git diff --numstat` | 改了什么 |
|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Shell/CampaignRewardWindow.cs` | **+33/−26**（其中本件 **+7/−2**） | **A314**：`BuildItem` 里那句「仍不吃裁切的：抽屉里那三层文字」→ **整段换成订正块**（原话逐字引在正文里 + 日期 + 错因 + 指回 A302）。⛔ 同一行的 `st.Clip = RenderClip;` / `st.ClipSoftness = ClipSoftness;` **一个字没动**。 |
| 2 | `Unity/MyGame/Assets/CardPresentation/Shell/BoosterInfoPopup.cs` | **+39/−13**（其中本件 **+12/−5**） | **A318**：`QShadeHit` 那段 doc 里那张「同族既有做法」清单 → 4 处值订正（见 3·2）+ 订正块（原话逐字保留）。⛔ `const int QShadeHit = QShade;` **没动**。 |
| 3 | `Unity/资料/普查产出_1008/A220_A221_常量收窄与四条.md` | **+26/−3** | **A331**：§三·1 的 ①「8 个窗」那句 → 点名到个 + ② `ChatPanel` 那一行 → 加「只有 `QBg`」的订正 + ③ 表后那条 ⚠️ 块 → 追加 A331 订正块（含复跑判据）。 |

### 3·1 行尾与列数（本仓最容易在这儿翻车）

- **三个文件改完复量（二进制读）**：`CampaignRewardWindow.cs` = **crlf 0 / lone_lf 715** · `BoosterInfoPopup.cs` = **crlf 0 / lone_lf 573** · 该 `.md` = **crlf 0 / lone_lf 447** ⇒ **都仍是纯 LF，一个都没被翻**（全程只用 Edit 工具，⛔ 没用 `sed -i`、⛔ 没用 python 写文件）。
- **`git diff --numstat` 三行都远小于文件行数** ⇒ 没有整篇重写（判据：数字接近行数就是翻了）。
- **表格列数复核**：`ChatPanel` 那一行 **4 条竖线 = 3 列** ✅（防 10-03 那个「行首当锚点 ⇒ 列数变 5」的坑）。

### 3·2 ⚠️ 一处**超出 A318 字面范围**的改动（**请调度台复核 / 可单独回滚**）

- A318 只点名了 `BoosterPackOpenWindow:95 用 QBase−1` 这一格。但**同一句话**里还有 **3 处同类过期**，且**错因完全相同**（都是 2026-10-04 `A47`/`A25⑥` 那批把「内容档 − 1 / 压暗档 + 1」改成「压暗层自己那一档」时留下的），**判据就在各自文件的订正注释里**：

| 窗 | 句子原写 | 现读真值 | 判据（现读） |
|---|---|---|---|
| `DeckSelectionPopup` | `QDsHit−1`（3127） | **`QDs`（3125）** | `DeckSelectionPopup.cs` 里「🔴 **2026-10-04（A47 接线批）订正档号：`QDsHit − 1`(3127) → `QDs`(3125)**」+ 调用点 `MenuDraw.ShadeHit(root, …, QDs, QDsHit, () => Close(), "BackgroundHit")` |
| `ImportDeckPopup` | `QImpHit−1`（3132） | **`QImp`（3130）** | `ImportDeckPopup.cs`「🔴 **2026-10-04（A25⑥）**：档从 `QImpHit − 1`（= 3132…）」+ `MenuDraw.ShadeHit(root, …, QImp, QImpHit, …)` |
| `PlayerProfileWindow` | `QShade+1`（3151） | **`QShade`（3150）** | `PlayerProfileWindow.cs`「订正档号：`QShade + 1`(3151) → `QShade`(3150)」+ `MenuDraw.ShadeHit(root, …, QShade, QHit, …)` |

- **为什么做了**（三条，都可核）：① **同一句话、同一个文件、同一个错因** ⇒ 铁律 5 明写「**就地改掉那条记录本身**……顺手 grep 那句话的关键词」；
  ② 只改一格会**留下三句已知为假的话紧挨着一条订正**（正是铁律 5 说的「两份说法打架比没有更糟」）；
  ③ 这段注释**是给后来人照抄的**（它的标题就是「同族既有做法」）—— 而 `QDsHit−1` 这一写法**恰恰是当年被判为错的那种**（「内容档 − 1」），照抄 = **把 A47 修掉的 bug 抄回来**。
- ⛔ **没做的**：没碰**行号**（七个行号全漂，改成**按符号认**并在正文里写明「⛔ 别把行号抄回来；全仓『行号已漂』是另一件账 **A338**」）；没碰别的文件、没碰 A315、没碰任何可执行代码。
- **回滚点**：若要回到「只改 A318 那一格」，把 `BoosterInfoPopup.cs` 第 2 行那格改回 `QBase−1` 即可 —— 但**三处同类过期会留在句子里**，请一并裁决。

---

## 四、A322 行尾隐患清单（文件 / HEAD 行尾 / 工作区行尾 / 判决）

> **判法**：HEAD = `git show HEAD:<路径>` 的字节；工作区 = `io.open(路径,'rb')` 的字节。`CRLF/总行` 两数**相等 = 纯 CRLF**、`0/总行` = **纯 LF**、**都不是** = MIXED（本批 **一个 MIXED 都没有**）。
> **配置前提**：`core.autocrlf = false` · `core.eol` 空 · **无 `.gitattributes`** ⇒ **没有归一化**，行尾翻转会**真的**变成整篇重写 ⇒ 这张表的「✅ 同」是有意义的结论，不是默认值。
> **判决列**：`✅ 同` = HEAD 行尾 = 工作区行尾（**不会**整篇重写）· `🔴 不同` = **会**（本批 **0 个**）。
> ⏱ **本表是「扫描那一刻」的快照** —— 扫描期间**别的写手仍在动文件**（实测 `Editor/BattleScene.cs` 在两次扫描之间从 10324 行长到 10329、`ArenaBuilder.cs` 从 4500 长到 4506）。
> ⇒ **两次扫描的「行尾不一致」行数都是 0**（变的只有 `+N/-N` 与行数），但**提交前建议由调度台再复跑一次本表**：
> 判法脚本与口径见 §2·4（`git show HEAD:<路径>` + 二进制读，两个数报出来即可）。

| # | 文件 | HEAD 行尾 (CRLF/总行) | 工作区行尾 (CRLF/总行) | `git diff --numstat` | 判决 |
|---|---|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Battle/BattleBackdrop.cs` | LF (0/108) | LF (0/115) | +7/-0 | ✅ 同 |
| 2 | `Unity/MyGame/Assets/CardPresentation/Battle/BattleDoors.cs` | CRLF (308/308) | CRLF (328/328) | +22/-2 | ✅ 同 |
| 3 | `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | CRLF (8547/8547) | CRLF (8572/8572) | +26/-1 | ✅ 同 |
| 4 | `Unity/MyGame/Assets/CardPresentation/Battle/BattleLogPanel.cs` | CRLF (700/700) | CRLF (713/713) | +15/-2 | ✅ 同 |
| 5 | `Unity/MyGame/Assets/CardPresentation/Battle/BattlePostFx.cs` | LF (0/258) | LF (0/356) | +116/-18 | ✅ 同 |
| 6 | `Unity/MyGame/Assets/CardPresentation/Battle/CardDisplayWindow.cs` | CRLF (731/731) | CRLF (737/737) | +7/-1 | ✅ 同 |
| 7 | `Unity/MyGame/Assets/CardPresentation/Battle/ChatPopupPanel.cs` | LF (0/386) | LF (0/393) | +8/-1 | ✅ 同 |
| 8 | `Unity/MyGame/Assets/CardPresentation/Battle/ChoosePanel.cs` | LF (0/125) | LF (0/131) | +7/-1 | ✅ 同 |
| 9 | `Unity/MyGame/Assets/CardPresentation/Battle/EndPanel.cs` | CRLF (333/333) | CRLF (351/351) | +21/-3 | ✅ 同 |
| 10 | `Unity/MyGame/Assets/CardPresentation/Battle/EnvironmentApplier.cs` | LF (0/912) | LF (0/930) | +22/-4 | ✅ 同 |
| 11 | `Unity/MyGame/Assets/CardPresentation/Battle/ImageQuad.cs` | LF (0/446) | LF (0/535) | +91/-2 | ✅ 同 |
| 12 | `Unity/MyGame/Assets/CardPresentation/Battle/Label.cs` | CRLF (717/717) | CRLF (904/904) | +199/-12 | ✅ 同 |
| 13 | `Unity/MyGame/Assets/CardPresentation/Battle/MulliganPanel.cs` | LF (0/434) | LF (0/468) | +42/-8 | ✅ 同 |
| 14 | `Unity/MyGame/Assets/CardPresentation/Battle/MultiCardDisplay.cs` | LF (0/240) | LF (0/250) | +11/-1 | ✅ 同 |
| 15 | `Unity/MyGame/Assets/CardPresentation/Battle/RemnantSfx.cs` | LF (0/207) | LF (0/213) | +6/-0 | ✅ 同 |
| 16 | `Unity/MyGame/Assets/CardPresentation/Battle/ReplayBar.cs` | LF (0/236) | LF (0/250) | +16/-2 | ✅ 同 |
| 17 | `Unity/MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs` | LF (0/1685) | LF (0/1824) | +187/-48 | ✅ 同 |
| 18 | `Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs` | LF (0/415) | LF (0/431) | +19/-3 | ✅ 同 |
| 19 | `Unity/MyGame/Assets/CardPresentation/Battle/TargetReticle.cs` | LF (0/662) | LF (0/669) | +7/-0 | ✅ 同 |
| 20 | `Unity/MyGame/Assets/CardPresentation/Battle/UnitChatPanel.cs` | LF (0/345) | LF (0/356) | +13/-2 | ✅ 同 |
| 21 | `Unity/MyGame/Assets/CardPresentation/Battle/WaitBanner.cs` | LF (0/253) | LF (0/286) | +37/-4 | ✅ 同 |
| 22 | `Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs` | LF (0/464) | LF (0/473) | +10/-1 | ✅ 同 |
| 23 | `Unity/MyGame/Assets/CardPresentation/Board/BoardLayout.cs` | LF (0/435) | LF (0/441) | +6/-0 | ✅ 同 |
| 24 | `Unity/MyGame/Assets/CardPresentation/Core/BlobShadow.cs` | LF (0/158) | LF (0/165) | +7/-0 | ✅ 同 |
| 25 | `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs` | CRLF (4981/4981) | CRLF (4932/4932) | +22/-71 | ✅ 同 |
| 26 | `Unity/MyGame/Assets/CardPresentation/Core/EnvBlendables.cs` | LF (0/196) | LF (0/197) | +3/-2 | ✅ 同 |
| 27 | `Unity/MyGame/Assets/CardPresentation/Core/EnvironmentConditions.cs` | LF (0/142) | LF (0/145) | +8/-5 | ✅ 同 |
| 28 | `Unity/MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs` | LF (0/538) | LF (0/588) | +59/-9 | ✅ 同 |
| 29 | `Unity/MyGame/Assets/CardPresentation/Core/TmpFont.cs` | LF (0/264) | LF (0/310) | +46/-0 | ✅ 同 |
| 30 | `Unity/MyGame/Assets/CardPresentation/Core/Tooltip.cs` | LF (0/477) | LF (0/483) | +11/-5 | ✅ 同 |
| 31 | `Unity/MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs` | CRLF (416/416) | CRLF (416/416) | +1/-1 | ✅ 同 |
| 32 | `Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | CRLF (3859/3859) | CRLF (4069/4069) | +228/-18 | ✅ 同 |
| 33 | `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | CRLF (9507/9507) | CRLF (10324/10324) | +835/-18 | ✅ 同 |
| 34 | `Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs` | LF (0/4035) | LF (0/4909) | +915/-41 | ✅ 同 |
| 35 | `Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` | CRLF (2432/2432) | CRLF (2854/2854) | +423/-1 | ✅ 同 |
| 36 | `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs` | LF (0/7003) | LF (0/7692) | +734/-45 | ✅ 同 |
| 37 | `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` | LF (0/4757) | LF (0/6477) | +1773/-53 | ✅ 同 |
| 38 | `Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs` | LF (0/1680) | LF (0/2325) | +696/-51 | ✅ 同 |
| 39 | `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` | LF (0/2249) | LF (0/2792) | +563/-20 | ✅ 同 |
| 40 | `Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs` | LF (0/2708) | LF (0/3338) | +632/-2 | ✅ 同 |
| 41 | `Unity/MyGame/Assets/CardPresentation/Shell/AllianceMemberTab.cs` | LF (0/1239) | LF (0/1245) | +13/-7 | ✅ 同 |
| 42 | `Unity/MyGame/Assets/CardPresentation/Shell/AlliancesTab.cs` | LF (0/637) | LF (0/670) | +49/-16 | ✅ 同 |
| 43 | `Unity/MyGame/Assets/CardPresentation/Shell/AvatarTab.cs` | LF (0/333) | LF (0/333) | +4/-4 | ✅ 同 |
| 44 | `Unity/MyGame/Assets/CardPresentation/Shell/BattleLogTab.cs` | LF (0/207) | LF (0/203) | +5/-9 | ✅ 同 |
| 45 | `Unity/MyGame/Assets/CardPresentation/Shell/BoosterInfoPopup.cs` | LF (0/547) | LF (0/573) | +39/-13 | ✅ 同 |
| 46 | `Unity/MyGame/Assets/CardPresentation/Shell/BoosterPackOpenWindow.cs` | LF (0/701) | LF (0/734) | +42/-9 | ✅ 同 |
| 47 | `Unity/MyGame/Assets/CardPresentation/Shell/CampaignRewardWindow.cs` | LF (0/708) | LF (0/715) | +33/-26 | ✅ 同 |
| 48 | `Unity/MyGame/Assets/CardPresentation/Shell/CampaignTab.cs` | LF (0/783) | LF (0/888) | +112/-7 | ✅ 同 |
| 49 | `Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs` | LF (0/801) | LF (0/810) | +11/-2 | ✅ 同 |
| 50 | `Unity/MyGame/Assets/CardPresentation/Shell/ChatPanel.cs` | LF (0/650) | LF (0/663) | +17/-4 | ✅ 同 |
| 51 | `Unity/MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | LF (0/2612) | LF (0/2722) | +167/-57 | ✅ 同 |
| 52 | `Unity/MyGame/Assets/CardPresentation/Shell/DailyData.cs` | LF (0/837) | LF (0/907) | +79/-9 | ✅ 同 |
| 53 | `Unity/MyGame/Assets/CardPresentation/Shell/DailyRewardPopup.cs` | LF (0/367) | LF (0/370) | +3/-0 | ✅ 同 |
| 54 | `Unity/MyGame/Assets/CardPresentation/Shell/DailyStreakPopup.cs` | LF (0/437) | LF (0/458) | +44/-23 | ✅ 同 |
| 55 | `Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs` | LF (0/1251) | LF (0/1453) | +210/-8 | ✅ 同 |
| 56 | `Unity/MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.cs` | LF (0/565) | LF (0/581) | +19/-3 | ✅ 同 |
| 57 | `Unity/MyGame/Assets/CardPresentation/Shell/FriendsTab.cs` | LF (0/349) | LF (0/352) | +7/-4 | ✅ 同 |
| 58 | `Unity/MyGame/Assets/CardPresentation/Shell/ImportDeckPopup.cs` | LF (0/336) | LF (0/357) | +24/-3 | ✅ 同 |
| 59 | `Unity/MyGame/Assets/CardPresentation/Shell/InboxWindow.cs` | LF (0/382) | LF (0/424) | +67/-25 | ✅ 同 |
| 60 | `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs` | LF (0/943) | LF (0/990) | +56/-9 | ✅ 同 |
| 61 | `Unity/MyGame/Assets/CardPresentation/Shell/LeaderboardRow.cs` | LF (0/241) | LF (0/242) | +11/-10 | ✅ 同 |
| 62 | `Unity/MyGame/Assets/CardPresentation/Shell/LeaderboardWindow.cs` | LF (0/651) | LF (0/659) | +9/-1 | ✅ 同 |
| 63 | `Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEventWindow.cs` | LF (0/1034) | LF (0/1057) | +40/-17 | ✅ 同 |
| 64 | `Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs` | LF (0/1137) | LF (0/1225) | +192/-104 | ✅ 同 |
| 65 | `Unity/MyGame/Assets/CardPresentation/Shell/MatchLogRow.cs` | LF (0/313) | LF (0/320) | +8/-1 | ✅ 同 |
| 66 | `Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs` | LF (0/1978) | LF (0/2269) | +344/-53 | ✅ 同 |
| 67 | `Unity/MyGame/Assets/CardPresentation/Shell/MenuScroll.cs` | LF (0/364) | LF (0/383) | +30/-11 | ✅ 同 |
| 68 | `Unity/MyGame/Assets/CardPresentation/Shell/MenuWindowBase.cs` | LF (0/473) | LF (0/499) | +45/-19 | ✅ 同 |
| 69 | `Unity/MyGame/Assets/CardPresentation/Shell/MissionRerollPopup.cs` | LF (0/487) | LF (0/493) | +7/-1 | ✅ 同 |
| 70 | `Unity/MyGame/Assets/CardPresentation/Shell/PlayerProfileWindow.cs` | LF (0/704) | LF (0/743) | +45/-6 | ✅ 同 |
| 71 | `Unity/MyGame/Assets/CardPresentation/Shell/PointerLayer.cs` | LF (0/1066) | LF (0/1154) | +94/-6 | ✅ 同 |
| 72 | `Unity/MyGame/Assets/CardPresentation/Shell/PracticeModePopup.cs` | LF (0/1582) | LF (0/1783) | +219/-18 | ✅ 同 |
| 73 | `Unity/MyGame/Assets/CardPresentation/Shell/ProfileTab.cs` | LF (0/942) | LF (0/952) | +31/-21 | ✅ 同 |
| 74 | `Unity/MyGame/Assets/CardPresentation/Shell/PromptPopup.cs` | LF (0/1042) | LF (0/1107) | +86/-21 | ✅ 同 |
| 75 | `Unity/MyGame/Assets/CardPresentation/Shell/RankedEventWindow.cs` | LF (0/282) | LF (0/280) | +6/-8 | ✅ 同 |
| 76 | `Unity/MyGame/Assets/CardPresentation/Shell/RankedTab.cs` | LF (0/448) | LF (0/448) | +6/-6 | ✅ 同 |
| 77 | `Unity/MyGame/Assets/CardPresentation/Shell/RewardsWindow.cs` | LF (0/337) | LF (0/345) | +9/-1 | ✅ 同 |
| 78 | `Unity/MyGame/Assets/CardPresentation/Shell/SearchingMatchPopup.cs` | LF (0/317) | LF (0/325) | +9/-1 | ✅ 同 |
| 79 | `Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs` | LF (0/1708) | LF (0/2080) | +449/-77 | ✅ 同 |
| 80 | `Unity/MyGame/Assets/CardPresentation/Shell/ShopWindow.cs` | LF (0/780) | LF (0/804) | +26/-2 | ✅ 同 |
| 81 | `Unity/MyGame/Assets/CardPresentation/Shell/SocialWindow.cs` | LF (0/452) | LF (0/489) | +61/-24 | ✅ 同 |
| 82 | `Unity/MyGame/Assets/CardPresentation/Shell/TitleTab.cs` | LF (0/262) | LF (0/262) | +3/-3 | ✅ 同 |
| 83 | `Unity/MyGame/Assets/CardPresentation/Shell/TrophyInfoPopup.cs` | LF (0/531) | LF (0/535) | +8/-4 | ✅ 同 |
| 84 | `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | LF (0/774) | LF (0/1021) | +263/-16 | ✅ 同 |
| 85 | `Unity/MyGame/Assets/RuleEngine/Data/DeckLibrary.cs` | CRLF (268/268) | CRLF (268/268) | +1/-1 | ✅ 同 |
| 86 | `Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` | CRLF (4268/4268) | CRLF (4506/4506) | +238/-0 | ✅ 同 |
| 87 | `Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs` | CRLF (1995/1995) | CRLF (2146/2146) | +151/-0 | ✅ 同 |

**非 `.cs` 的 M 文件（任意扩展名）里，行尾不一致的：**

- （无：非 `.cs` 的 M 文件也是一个都没有）

---

## 五、没查清 / 判不了的（⛔ 不猜）

1. 🔴 **`Battle/Label.cs` 是怎么从「工作区纯 LF」变回「CRLF 904/904」的 —— 没查清。**
   能确证的只有**现读状态**（两法一致：`file -b` + 字节计数）+ `numstat +199/-12`（干净）。**工作区改动不进 `git log`**，我看不到是谁写的。
   ⚠️ 简报说「W3 那一件改完是 `141/10`」，现读是 `199/12` ⇒ 之后**有写手动过这个文件** —— 但「是同一个写手继续改」「还是另有人还原了行尾」，**材料不够，不猜**。
2. **简报那条前提的来源没查**（它在简报里、不在任何文件里 ⇒ **无可就地订正的对象**，只能在这里报）。
3. **A331 那条订正的「原意」无法证实** —— 我**没有**去猜原报告作者当时怎么数出「8」的；我给的是**可复跑的点名**（剔三类 ⇒ 剩 8 个），并把「8 个窗 / 6 行 + 表外 1 个 = 7 行」的算式写进文档。**若调度台另有口径，这一段可整块换掉。**
4. **非 `.cs` 的 `M` 文件**我也顺带扫了（任意扩展名，只报不一致）；结果**也是 0 个**。⚠️ 但 A322 的验收口径**只点名 `.cs`** ⇒ 那一段只当**附赠**，别当成「已按 A322 交付」。

---

## 六、顺手发现（⛔ **只报不改**）

1. **A315**（`Shell/BoosterInfoPopup.cs:24` 说商品主图挂着 `BlinkGraphic`、我们没有那个组件）—— 按简报**没动**，只登记一句：**它在本文件里，与 A318 那处相隔很远**（`ShadeHit` 那条 doc 在文件中段），两者**没有耦合**。
2. 🔴 **本件改了 `BoosterInfoPopup.cs`（净 +7 行）⇒ 该文件在【别的文档】里的行号引用又漂了一格**：
   - 本报告 §二·3 那张表里那句「`Shell/BoosterInfoPopup.cs:79`（**现 `:93`**）」—— 现读应在 **`:100` 附近**（我这次是在 `:86` 那一带插入的 ⇒ `:86` 之后全部 +7）。
   - `资料/普查产出_1008/A220_A221_常量收窄与四条.md` §三·2 里那句 `BoosterInfoPopup:52` **未受影响**（在 `:86` 之前）。
   - ⇒ 这属于 **A338 那一族（「`<文件>:<行号>` 引用已漂」）**，按简报**没动**。
3. 🔴 **`资料/待办判据_阶段二与联机.md` 里 A25·补(一) 那张分派表仍留着「改前值」而【没有一行标「已落地」」**：
   `:442`（三种编码全工程 7 处）· `:448`（`PlayerProfileWindow:245`：`QShade + 1`）· `:449`（`BoosterPackOpenWindow:95`：`QBase − 1`）· `:481`/`:485`/`:486`（三行「🔴 档要改」表）。
   现读**这 4 处全已改完**（本件 §2·2 / §3·2 逐条核过）⇒ 那份文档**照现状读会以为还没改**。⛔ 不在白名单（且是正本级），只报。
4. ⚪ **`Shell/BoosterPackOpenWindow.cs:312`** 的注释里也还写着「档用的是 `QCloseSurface = QBase − 1`(3169)」——
   看上下文它是在**复述改前**（紧接着下一行就写「按规矩收口到公共件 `MenuDraw.ShadeHit`，档改成 `QShade`(3170)」）⇒ **不算错**，
   但**同文件里 `:98` / `:103` 已订正过同一件事** ⇒ 三处口径并存，读的人容易只看到 `:312` 那句。⛔ 不在白名单，只报。
5. ⚪ **`Shell/ChatPanel.cs` 的 `QBase` 仍是 `public`** —— A220 报告 §1·5·1 那条悬案（「该不该留 `public`」，4 个窗的 `QBase` 都是 0 引用）**仍然开着**，本件没动（不在 A331 的点名里）。
6. ⚪ **`file` 命令对「混行尾」不报警**（CLAUDE.md 记过）—— 本批我**改用「数出来」**（`crlf` vs `lone_lf` 两个数），
   87 个文件里**没有一个 MIXED**（即没有「CRLF 文件里某几行是 LF」这种 `file` 抓不到的形态）。这条口径建议**继续用「数」的**。

---

## 七、跑过的检查

### 7·1 秒级类型检查（**唯一必跑的那一步**）

```
$ TMPDIR=/tmp/wf_wa bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- **运行时 0 错 / 编辑器 0 错** ✅ —— **也不存在「别处的半成品污染」**（本次读数干净，无需按 13·3 那条「错误集中在别人的文件上」做甄别）。
- ⚠️ **按铁律 12，本件不跑任何 Unity 自检**（改的全是注释与文档；Unity 实例全程串行留给调度台）⇒ 覆盖面 **0 条宿主**，同步点可**不追加**自检。

### 7·2 本件自己做的核对（都不是「自检」，是**静态核对**）

| 核对 | 手段 | 结果 |
|---|---|---|
| 行尾没被翻 | 三个文件**二进制读**数 `\r\n` / `\n` | 纯 LF ×3 ✅（见 §3·1） |
| 没整篇重写 | `git diff --numstat` 三行 | `+33/−26` · `+39/−13` · `+26/−3` ✅ |
| 表格没被改坏 | 数 `ChatPanel` 那一行的竖线 | 4 条 = 3 列 ✅ |
| A322 的判法自身可信 | `file -b -` 对 3 个样本 × HEAD/工作区 | 与字节计数**逐条一致** ✅ |
| 「隐患真的会有后果」这个前提 | `core.autocrlf` / `core.eol` / `.gitattributes` | `false` / 空 / 不存在 ⇒ **会** ✅ |
| A331 的 5 个常量真被跨类用 | `grep -n "ChatPanel\."` | 7 处代码引用，全在 `ChatMessageRow` ✅ |
| 「8 个窗」点名可复跑 | `grep -rl "A47 接线批"` + 子类声明 `grep` | 19 / Shell 侧 15 / 剔三类剩 8 ✅ |

### 7·3 ⛔ 本件**没做**的（如实记）

- ⛔ 没跑 Unity（`-executeMethod` 一次都没有）· ⛔ 没跑那 11 条自检 · ⛔ 没动 git 写命令（无 `add`/`commit`/`checkout`/`stash`/`reset`）。
- ⛔ 没改正本（`项目任务.md` / `CLAUDE.md` 一个字没碰）· ⛔ 没越白名单（改动只有 §三 那三个文件）。
- ⛔ 扫描脚本落在临时目录（`/tmp/wf_wa/`：`linescan.py` · `gen_table.py` · `a322_table.md`），**没往 `工具/` 里放**（不在白名单）。
