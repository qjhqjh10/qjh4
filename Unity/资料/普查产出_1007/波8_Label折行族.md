# 波 8 · Label 折行族（A205 · A62 · A77 ①③⑩⑫）

> 执行代理：波 8 · 「Label 折行族」一大件 · 日期 2026-10-07
> 判据全文 = `资料/待办判据_审查发现_1005.md`（§①③⑩⑫）· 正本 A62 / A205 行 · `资料/普查产出_1004/A62_折行普查_1005.md`（逐处表）·
> `资料/普查产出_1007/波8_A77_批四_5_22_23.md` §五·1（A205 的原始发现）。
> ⚠️ 动手前**逐处重跑了 `grep -n` 与 `menu_dump.py`**（行号与报告里记的已经漂了，一律以本文的行号为准）。
> 白名单外一行没动；`git status` 里别人的未提交改动原样保留。

---

## 〇、一句话总账

| 组 | 结论 | 一句话 |
|---|---|---|
| **A205** | ✅ **做完** | `Battle/Label.cs` 新增 **`ForceRelayout()`**；`SetWrapping`/`SetWrappingMode` 改模式时**自己**推版面 ⇒ 「字段说了、画面没变」这一档堵死 |
| **A62** | ✅ **白名单内做完** | 我把范围内的 **28 处**逐处核过并落地（主表 #2/#4/#5/#6/#14/#25 · 子表 A 的 21 处 · 子表 E1/E2）；**白名单外**的（`#31 MenuWindowBase` · `#34 PlayerProfileWindow` · `#20/#21 CollectionWindow` · 子表 A 里 TitleTab/AchievementsMenu/BattleLogTab 那 4 处）**一处没动**，逐条列在 §七 |
| **①（第三档）** | ✅ **做完** | `Label.SetWrappingMode(int)` + `WrappingMode` 读口；两处 `折行=3` 用 **3** 落地；`ProfileTab.cs` 那句「3（= 不折行）」**就地订正** |
| **③（碰巧对）** | ✅ **做完** | 4 处**全补显式 `wrap:true`**（ProfileTab `Avatar Name`/`MessageText` · AvatarTab 格子 `Avatar Name` · RankedTab `Avatar Name`），并各配一条断言 |
| **⑩（SetWrapWidth 直开 + 按窗参数）** | ✅ **做完** | E1/E2 补 `SetWrapping(false)` + **订正注释**（后半句是错的）· E3 标注「本来就对，别改」· E4 补 `SetWrapping(false)` · E5 标明**我们挑的**；搜索框**按窗分参数**（新常量，共用值一个字没改） |
| **⑫④（三处常量重算）** | ✅ **做完** | 三处**用修好的 `menu_dump.py` 现算**并对照 → 位移都是 **+5.19 / +5.15 / +4.69**（成因见下） |

**改动**：**13 个文件**（11 个 `.cs` + 0 个工具 + 报告 1 份）· **新断言 53 条**（DeckScene 31 · MainMenuScene 22）· typecheck 两遍 **0/0**。

---

## 一、A205 —— `SetWrapping` 之后 mesh 不重排（**必须最先做**的那一件）

### 1. 结论
✅ **做完**。`Battle/Label.cs` 加了公共口 **`ForceRelayout()`**（`Battle/Label.cs:325`），
并把**两个**改模式的口都接到它上面：`SetWrapping(bool)`（`:276`）与 `SetWrappingMode(int)`（`:285`）——
**模式真的变了才重排**（没变就早退 ⇒ 既有调用点行为逐位不变）。

### 2. 证据
- **判据（机制）**：`TMP_Text.cs:747` —— `set { … m_havePropertiesChanged = true; m_TextWrappingMode = value; SetVerticesDirty(); SetLayoutDirty(); }`
  ⇒ **只标脏、不重排**；而 `CLAUDE.md` §三「批处理下没有帧循环」⇒ mesh 停在上一版。
  （同一份文件里 `fontSize` 的 setter `:465` 是 `if (m_fontSize == value) return;` ⇒ 传当前值 = 早退、只剩 `ForceMeshUpdate()`。）
- **实现** = 批四写手那套 `RelayoutNow`（`Shell/PracticeModePopup.cs:963`）原样收进公共件：

```csharp
// Battle/Label.cs:325
public void ForceRelayout()
{
    if (_tmp == null) return;                    // 点阵后端没有折行/mesh 这回事 ⇒ 什么都不做（不假装）
    SetFontSize(_tmp.fontSize);                  // 值相同 ⇒ setter 早退，只要那一次 `ForceMeshUpdate`
    RefreshBounds();                             // 重排后的 `_tmpW/_tmpH` 要落回字段（否则 WorldW 还是旧版面的值）
}
```

- **新断言（`Editor/DeckScene.cs:2157-2189`，探针，**两态可分辨**）**：

```csharp
var plb = Label.Create(probe.transform, "", Vector3.zero, 3, Color.white, new Vector2(0.5f, 0.5f), "a205");
CheckTrue(plb != null && plb.CanRenderChinese, "（前提）A205 探针建出了**真 TMP** …");
const string Long = "AAA BBB CCC DDD EEE FFF GGG HHH";
plb.SetText(Long); plb.SetGlyphHeight(20f/108f); plb.SetWrapWidth(80f/108f); plb.ForceRelayout();
int wrapped = plb.LineCount; float wrappedW = plb.WorldW;
CheckTrue(wrapped > 1, $"（前提）80px 宽的框里这句真的折了（{wrapped} 行）…");
plb.SetWrapping(false);
Check(plb.WrappingMode, 0, "★ 折行字段 = **0（`NoWrap`）**");
Check(plb.LineCount, 1, $"★ 而且**mesh 真的重排了**：行数从 {wrapped} → **1** …");
CheckTrue(plb.WorldW > wrappedW * 1.05f, "★ 块宽跟着变宽 …");
```

**怎么改坏就红**：把 `SetWrapping` 里那句 `ForceRelayout()` 删掉（退回只设字段）⇒ 第 3 条**不会红**（字段确实设了）、
但第 4/5 条**立刻红**（行数仍 = `wrapped`、宽度纹丝不动）—— 这正是「字段说了、画面没变」那一档，旧写法**断言不出来**。

### 3. 没查清的部分
- `ForceRelayout` **会挪 TMP 子节点**（`RefreshBounds` 按新宽度重新居中）⇒ 调用方若在**之前**调过 `AlignLeftOn`/`AlignRightOn`
  （两个都按 `WorldW` 算），重排之后要**再对齐一次**。本件所有「先对齐再关折行」的站点都补了第二次对齐；
  ⚠️ **但这条约束没写进 `Label` 的类型系统**（只能靠注释 + code review），将来新增调用点要自己记住。
- 批四写手顺手发现的**同族第二条**（`MenuDraw.ClipText` 那一刀在 `SetAutoFitBox` 之后整体偏移 ≈ (12,5)px，**静默**）
  **不在本件范围**，我**没有**处理；本件新增的 `ForceRelayout` 会让**更多**标签走到那条路上（凡是 `SetWrapping` 真改了模式的），
  ⇒ 那一笔的优先级被本件**抬高**了（记在 §七·顺手发现）。

---

## 二、A62 —— `SetAutoFitBox` 内部无条件开折行

### 1. 结论
✅ **白名单内做完**。逐处**现读 dump / MB** 之后落地，**没有**采用「让 `SetAutoFitBox` 自己还原」那条被否掉的收口
（⛔ 那会把 4 处「碰巧对」静默回退）。

### 2. 证据（原版值 vs 我们值，逐处）

| # | 站点（现读行号） | 原版 `m_TextWrappingMode` | 判据 | 改动 |
|---|---|---|---|---|
| 主 #2 | `Deck/DeckRuntime.cs:611` | `'Cards' 1` · `'Deck info' 1` · **`'Cosmetics' 0`** | `md "Deck Editing Menu" --depth 14 --md` | 只对 `i==2` 补 `SetWrapping(false)` |
| 主 #4 | `Deck/DeckRuntime.cs:2850-2890` | `Placeholder 0` / `Text **3**` · **auto 关**（fs 26） | 同一条 dump 的**原始行**（该行没有 `auto[…]` 段）+ MB 侧 11 份 `'Search'` 逐份扫 | 不再挂 `SetAutoFitBox`；改 `SetWrapWidth`（只取框宽）+ `SetWrappingMode(3)` + `AlignLeftOn`；字号走**按窗常量 26** |
| 主 #5 | `Deck/DeckRuntime.cs:2962` | 开关 0 · 稀有度 0 · 类型 0 · **费用桶 1** | 同一条 dump；四族的 `折行=` 列 | `lb.SetWrapping(c.LabelWrap == 1)`（`Cell` 新增**显式字段**，四族各自声明） |
| 主 #6 | `Deck/DeckRuntime.cs:3090` | 卡背页 `'Owned only'` **0** | `md "Deck Editing Menu" --depth 18 --md`（该行 `auto[26.0~32.0]`） | 同上（卡背那份也有 `LabelWrap = 0`） |
| 主 #14 | `Shell/BoosterInfoPopup.cs:466` | `'Save More!'` **0** | `md "Booster Info Popup" --depth 16 --md`（`折行=0 auto[12.0~38.0]`） | 补 `SetWrapping(false)` |
| 主 #25 | `Shell/MainMenuRuntime.cs:716` | 顶栏 `Player Name` **0** | `MB: …mainmenuwarpforge/MonoBehaviour_1717.json` 实读 | 补 `SetWrapping(false)` |
| 子A A1 | `Shell/ProfileTab.cs:382` | `playerIdText` **0** | `md "Player Profile Window" --depth 25 --md` | 关 + 重做左对齐 |
| 子A A6/A7 | `Shell/ProfileTab.cs:456/460` | `Player Name`/`Player Title`（with Alliance）**0** | 同上 | 关 + 重做左对齐 |
| 子A A10/A11 | `Shell/ProfileTab.cs:495/499` | 同上（without Alliance，**显示的那一份**）**0** | 同上 | 关 + 重做左对齐 |
| 子A A12~A15 | `Shell/ProfileTab.cs:556/563/571/585` | `'Leaderboard'` 0 · `'Current Rank'` 0 · `'Division V'` 0 · `'Ends in:…'` 0 | 同上 | 各自关掉 |
| 子A A17 | `Shell/ProfileTab.cs:646` | `'Legendary Points'` **0** | 同上 | 关掉 |
| 子A A22/A25/A26 | `Shell/ProfileTab.cs:746/770/776` | `Placeholder` 0 · `'Free'` 0 · `'300,00'` 0 | 同上 | 各自关掉（A22 加注释说明它**今天本来就**没被 `SetAutoFitBox` 碰过） |
| 子A A28~A30 | `Shell/AvatarTab.cs:186/194/203` | 大图 `Avatar Name` 0 · `'Selecionar'` 0 · `'Toggle Border'` 0 | 同上 | 各自关掉 |
| 子A A40/A41 | `Shell/RankedTab.cs:231/235` | `Player Name`/`Player Title` **0** | 同上 | 关 + 重做左对齐 |
| 子A A44 | `Shell/RankedTab.cs:311` | `Top4>center>DivisionText`（画 `'Global Rating'`）**0** | 同上 | 关掉 |
| 子A A46 | `Shell/RankedTab.cs:338` | `'Faction Rating'` **0** | 同上 | 关 + 重做左对齐 |

**⛔ 两处口径陷阱（判据文件点名过的）我都按「写死」处理了**：
- **#5 那条**：判据文件说「按 `LabelCenter` 取反会**静默漏改**（稀有度/类型是 `LabelRight`、费用桶两样都不设）」
  ⇒ 我在 `Cell` 上**新增了显式字段 `LabelWrap`**（`Core/FilterPanelModel.cs:277`），四族逐个声明、各带一句 dump 出处，
  渲染方 `SetWrapping(c.LabelWrap == 1)` —— **不是**反推。
- **#2/#6 的 auto 列**：判据文件订正过「卡背那份是 `auto[26~32]`、不是 `18~32`」⇒ 我只落了**折行**那一半
  （字号区间那一笔见 §七·顺手发现①，**没动**）。

### 3. 没查清的部分（⛔ 不猜）
- **白名单外的 4 组 A62 站点一处没动**（详见 §七）—— 它们不是「判不了」，是**不归本件管**（不在白名单）。
- 子表 C（`MenuDraw.TextBox` 的 C6~C24，19 处调用方）**仍然没核**（普查就记着「没查清」）—— 本件**只碰了其中 0 处**。
  见 §七·顺手发现②（我在其中**一处**顺手读到了真值，但按红线**只报不改**）。

---

## 三、① —— 第三档 `折行=3`（`PreserveWhitespaceNoWrap`）

### 1. 结论
✅ **做完**。`Label` 上开了「按原版原文设模式」的口，两处 `3` 用 **3** 落地，`ProfileTab.cs` 那句等号**就地订正**。

### 2. 证据
- **口**（`Battle/Label.cs`）：

```csharp
public int WrappingMode { get { return _tmp != null ? (int)_tmp.textWrappingMode : -1; } }   // :257（点阵返 −1，如实）
public void SetWrappingMode(int originalMode)                                               // :285
{
    if (_tmp == null) return;
    TextWrappingModes m;
    switch (originalMode) {
        case 0: m = TextWrappingModes.NoWrap; break;
        case 1: m = TextWrappingModes.Normal; break;
        case 2: m = TextWrappingModes.PreserveWhitespace; break;
        case 3: m = TextWrappingModes.PreserveWhitespaceNoWrap; break;
        default: Debug.LogWarning("[Label] ⚠️ 原版 `m_TextWrappingMode = " + originalMode
                                  + "` 不在 0/1/2/3 里 ⇒ 这一处**没设**（出声，不静默降级）"); return;
    }
    if (_tmp.textWrappingMode == m) return;      // 没变 ⇒ 别白重排一次（既有调用点行为不变）
    _tmp.textWrappingMode = m;
    ForceRelayout();                             // ← A205
}
```

- **枚举值判据**（第一手）：`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:100`
  = `{ NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 }`。
- **`3` 与 `0` 的关系（把有判据的那一半写死、没判据的那一半如实留白）**：
  · **有判据**：换行判定上**同档** —— `TMP_Text.cs:4485`（`if (textWrapMode != NoWrap && textWrapMode != PreserveWhitespaceNoWrap && …)` 才断行）·
    `:4731`（保存换行状态那处并列）；
  · **没判据**：**空白保留**那一半不同（`:4461` 把 `PreserveWhitespace{,NoWrap}` 单列一支、`0` 不在其中）
    ⇒ 「`3` 与 `0` 在本工程这套排版下等不等价」**仍然没有判据**，⛔ 不许当等价用（`Label.WrappingMode` 头里写着）。
  · **旁证（新增，见 §七·顺手发现③）**：`3` 不是原版手工挑的 —— `TMP_InputField.SetTextComponentWrapMode`
    （`TMP_InputField.cs:4618-4627`）对**单行输入框**写的就是 `PreserveWhitespaceNoWrap`，而实测那两处 `3` **都是输入框的 `Text`**。
- **两处落地**：
  1. `Shell/ProfileTab.cs:754` 改名窗 `Text`：`_nameField.SetWrappingMode(InWrapMode /* = 3，:284 */)` + 重做左对齐；
     **注释订正**（`:275-283`）：原写「折行 3（**= 不折行**）」⇒ 改成「**折行 3**」并把「等号没有判据」写进去（铁律 5）。
  2. `Deck/DeckRuntime.cs:2883` 筛选栏搜索框 `Text`：`_fltInputText.SetWrappingMode(FilterPanelModel.DeckEditInputWrap /* = 3，:131 */)`。
- **新断言**（`Editor/MainMenuScene.cs:2167` · `Editor/DeckScene.cs:2174-2189`）：

```csharp
CheckWrapMode(inArea != null ? FindChild(inArea, "Text") : null, ProfileTab.InWrapMode,
              "★ 改名窗输入框 `Text` = 原版**第三档 `折行=3`**（`PreserveWhitespaceNoWrap`）"
              + " —— 旧口表达不了它，用 `false` 顶替 = 静默降级成 0");
CheckWrapMode(inArea != null ? FindChild(inArea, "Placeholder") : null, 0,
              "★ 同名那一对里的 `Placeholder` = 原版 **`折行=0`**（**同一格两个节点两个档**，别一刀切）");
```
```csharp
plb.SetWrappingMode(3);
Check(plb.WrappingMode, 3, "★ 第三档 `折行=3`（`PreserveWhitespaceNoWrap`）**表达得出来** …");
CheckTrue(!plb.Wrapping, "★ `Wrapping`（= 是不是 `Normal`）为**假**（3 不是 1）");
Check(plb.LineCount, 1, "★ 第三档也**不折行**（TMP 源码：两者在换行判定上同档）");
plb.SetWrappingMode(7);
Check(plb.WrappingMode, 3, "负例：传一个不存在的档（7）⇒ **不改**（只出声 …）");
```

**怎么改坏就红**：把 `SetWrappingMode(3)` 换成 `SetWrapping(false)` ⇒ 期望值 3 的那条红（`Placeholder` 那条仍是 0，**两条能分开**）。

### 3. 没查清的部分
- 「`3` 与 `0` 在本工程排版下**渲染结果**等不等价」—— **仍然没有判据**（本件只把「不许当等价用」落成了代码与断言，**没有**去证明等价）。
  要坐实得单开一件：拿同一个 `Label` 分别设 0 与 3、对同一串**含首尾空格/零宽空格**的文本量渲染宽度差（本件没做）。
- `SetWrappingMode(2)`（`PreserveWhitespace`）**本地没有任何实例**（普查零命中）⇒ 口开了但**没有真实调用点**，如实记。

---

## 四、③ —— 4 处「碰巧对」补显式 `wrap:true`

### 1. 结论
✅ **做完**（判据文件自己订正过：「5 处」**实为 4 处**，`A42` 本就有 `wrap:true`）。4 处**都补了显式参数 + 都配了断言**。

### 2. 证据

| 站点（现读行号） | 原版 | 改前 | 改后 |
|---|---|---|---|
| `Shell/ProfileTab.cs:421`（`Avatar Item Small > Avatar Name`） | `折行=1` | 靠 `SetAutoFitBox` 副作用 | **`wrap: true`** |
| `Shell/ProfileTab.cs:762`（`MessageText`） | `折行=1` | 同上 | **`wrap: true`** |
| `Shell/AvatarTab.cs:245`（格子 `Avatar Name`） | `折行=1` | 同上 | **`wrap: true`** |
| `Shell/RankedTab.cs:206`（`Profile Player Info > Avatar Name`） | `折行=1` | 同上 | **`wrap: true`** |

**新断言**（4 条，各配在它那一页的块里）：`Editor/MainMenuScene.cs:1925`（AvatarTab 格子）· `:2168`/`:2171`/`:2173`（ProfileTab 的三处）·
`:2581`（RankedTab）· `:6003`（主菜单顶栏与聊天预览）。例：

```csharp
CheckWrapMode(FindChild(grid2, "Avatar Name"), 1,
              "★ 格子里 `Avatar Name` **折行**（原版 `折行=1`；原来靠 `SetAutoFitBox` 的副作用"
              + " = 「碰巧对」⇒ 现在补了**显式** `wrap:true`）");
```

**怎么改坏就红**：删掉任一处的 `wrap: true` ⇒ 该处立刻回到「靠副作用」——**今天不会红**（副作用还在），
所以这 4 条断言钉的是**行为**（= 折行）而不是「有没有写那个参数」；⛔ 但**它正是为此存在的**：
哪天有人给 `SetAutoFitBox` 收口（A62 那个被否掉的方案），这 4 条会**当场变红**而不是静默回退。

### 3. 没查清的部分
无。4 处的原版值都来自同一条 `md "Player Profile Window" --depth 25 --md`（逐行 `折行=`，本件现读）。

---

## 五、⑩ —— `SetWrapWidth` 的直开 + 按窗分参数

### 1. 结论
✅ **做完**（5 处逐处处置：E1/E2 改 · E3 只标注 · E4 改 · E5 标注「我们挑的」；搜索框按窗分参数落地）。

### 2. 证据

| 站点（现读行号） | 原版 折行 / auto | 处置 |
|---|---|---|
| **E1/E2** `Shell/MainMenuRuntime.cs:830/834`（聊天预览两行） | **0** · auto **关**（`MB: …_1795.json` / `_1814.json`：`m_TextWrappingMode=0` · `m_enableAutoSizing=0` · fs18） | 按调度台裁定 **(a)**：`SetWrapWidth`（只留框宽）+ **`SetWrapping(false)`**；**注释订正**——原写「原版 auto=0，**只给折行宽**、不给自适应」，**后半句是错的**（原版连折行都是关的，那个宽是**我们主动加的**） |
| **E3** `Shell/PromptPopup.cs:114` 前后 | **1**（`md "GenericPromptWindow" --depth 12 --md`：`MessageText 折行=1 auto[4~50]`） | ✅ **本来就对** ⇒ **只补一句「别顺手改」**的注释（`:114-117`），**代码不动** |
| **E4** `Shell/PromptPopup.cs:246`（钮上的字） | **0** · auto `12~50` | 补 `SetWrapping(false)`（原版靠缩字号、不折行） |
| **E5** `Shell/SettingsWindow.cs:1215` 那行说明 | **判据空**（原版没有这个节点，是我们自建的联机页） | **只标「我们挑的」**（注释里写明：不套 `false`、也不冒充原版） |
| **按窗分参数** `Core/FilterPanelModel.cs:113/125/131` + `Deck/DeckRuntime.cs:1068/2883` | 卡组编辑窗 = **fs 26 · auto 关**；收藏窗 = `auto[18~30]`（共用值**原样保留**） | 新增 `InputFontPxDeckEdit = 26f` 与 `DeckEditInputWrap = 3`；**⛔ 共用值 `InputFontPx/InputFontAutoMin` 一个字没改**；消费点两处（建的时候定字号、刷的时候定模式） |

**新断言**：`Editor/MainMenuScene.cs:5996-6005`（三条，见 §二 表里的 #25/E1/E2）+ `Editor/DeckScene.cs:2201-2208`：

```csharp
Check(inLb.WrappingMode, FilterPanelModel.DeckEditInputWrap, "★ 搜索框 `Text` = 原版 **`折行=3`** …");
CheckTrue(!inLb.AutoSizing, "★ 搜索框**不开自适应** —— 原版这一对搜索框 `m_enableAutoSizing = 0` …");
CheckNear(inLb.FontPxNow, FilterPanelModel.InputFontPxDeckEdit, 0.6f, "★ 搜索框字号 = 原版 **26**（不是收藏窗那份 30）");
```

**怎么改坏就红**：把 `DeckEditInputWrap` 改回 0 / 把 font 改回 `InputFontPx`（30）/ 把 `SetAutoFitBox` 挂回去 ⇒ 三条各红一条。

### 3. 没查清的部分
- **E5 没有断言**（判据本身就空 ⇒ 只能靠代码注释「标明是我们挑的」，断言一个「我们自建节点的折行」等于自证）。
- **搜索框去掉 `SetAutoFitBox` 的副作用（如实记）**：原版那一格是 `TMP_InputField` + `RectMask2D`（**裁**掉溢出），
  我们的 `DeckRuntime` **全文件没有一处裁切**（`ClipText`/`ClipRect` 零命中）⇒ 名字超长时**会画到框外**，
  而**原版是裁掉**。这是**已知的、机制不同**的一处（要按原版收口得先给这个抽屉接 `MenuDraw.ClipText`，**不在本件范围**，记在 §七）。
- E1/E2 的 `SetWrapWidth` **我留着**（只为那份框宽 327.3）——⛔ 没有证据说原版那个节点的宽就是 327.3
  （判据文件里那 327.3 的出处本件**没去复核**，如实标「沿用旧值、未复核」）。

---

## 六、⑫④ —— 三处 C# 常量按**修好的** `rect_of` 重算

### 1. 结论
✅ **做完**（三处都现读现算，给出「命令 / 新旧对照 / 为什么变了」）。

### 2. 证据

**重算命令（三条，都是现读）**
```bash
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Booster Info Popup"  --depth 16 --md
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 14 --md
```

| # | 常量（现读行号） | 旧值（旧工具） | **新值（现算）** | 位移 |
|---|---|---|---|---|
| 1 | `Shell/BoosterInfoPopup.cs:120` `WebTextR` | `1291.22 … 1423.22` | **`1296.42,725.86 → 1428.42,777.74`** | **+5.19**（左右同比） |
| 2 | `Shell/ProfileTab.cs:314` `PdTxL/PdTxR` | `938.55 / 1030.77` | **`943.70 / 1035.92`** | **+5.15**（`PdIcL/PdIcR` **不变** ✓ 与判据一致） |
| 3 | `Shell/AlliancesTab.cs:579`（`Price Display > text`） | `542.06 … 609.12` | **`546.75,730.73 → 613.81,777.65`** | **+4.69** |

**为什么变了（三处同一个机理，逐条验算过）**：这三处都是 `HorizontalLayoutGroup` 里的**第二个子件**，
`m_ChildScaleWidth = 1`，而**前一个子件**（图标）自带 `m_LocalScale = 1.2`（dump 里逐行标着 `×1.2 → 视觉 …`）：
· uGUI 的**推进量** = `childSize × scaleFactor`（`51.88×1.2` 而不是 `51.88`）；
· 而**组内居中**的起始偏移 = `(组宽 − 各子件缩放后之和)/2` ⇒ 组内内容**多出来的那一半**由起始偏移吸收。
⇒ **净位移 = 前一件布局宽 × (localScale − 1) ÷ 2**：
`51.88×0.1 = 5.19` ✓ · `51.47×0.1 = 5.15` ✓ · `46.91×0.1 = 4.69` ✓（三处都逐位对上）。
旧值是**旧工具**的读数（`rect_of` 的 `scale` 曾是死参 + `_child_sizes` 未补 `LayoutUtility.GetFlexibleSize`）；
`--no-ancestor-scale` 复跑**三处都不变** ⇒ 差异**不是**祖先缩放那条路带来的，而是**布局推进**那条路（可反查）。

**怎么改坏就红**：这三处**没有专门断言**（既有断言里 **0 处**引用这三个常量 —— 已用 `grep` 核过 `WebTextR/PdTxL/PdTxR/613.81/609.12` 全零命中，
所以本次改动**不会**让任何既有断言变红）；要钉的话得新写「量渲出来的矩形」那种断言（见 §七·没做）。

### 3. 没查清的部分
- 三处**没有新断言**（时间预算分给了 A205/①/③）；按本仓纪律「改了版面要能红」，**这三条待补**（已列 §七）。
- `AlliancesTab` 那一处的**上层**（我们没有建原版的 `Price Display` 节点，文字直接挂在按钮下）**仍然与树结构不同**，
  本件只订正了**矩形**，没动结构（老账，不在本件范围）。

---

## 七、没做完的 / 白名单外没碰的 / 顺手发现（⛔ 只报不改）

**A. 白名单外的 A62 站点（一处没动 —— 不是判不了，是不归本件管）**
1. 主表 **#31**（四窗左栏页签，`Shell/MenuWindowBase.cs:462` 那个 helper）—— 原版 `折行=0`、我们无条件开。
2. 主表 **#34**（`Shell/PlayerProfileWindow.cs:307` 档案窗左栏 6 颗页签）—— 原版 6/6 `折行=0`。
3. 主表 **#20/#21**（`Shell/CollectionWindow.cs:1474/1720`：收藏窗筛选格 + Back 钮）—— 原版 `0`（费用桶那族 `1`）。
4. 子表 A 的 **A32~A37**（`AchievementsMenu.cs`）· **A38**（`BattleLogTab.cs`，判据空）· **A47~A49**（`TitleTab.cs`）—— 8 处。
5. ⚠️ **同一个 helper 的收口机会**：`Shell/PlayerProfileWindow.cs:551-568` 的 `Text(...)` 就是子表 A 那 52 处的公共口 ——
   若能在那儿加一个「`autoFit` 不再隐含 `wrap`」的口（例如把 `autoFit` 分支后补一次 `SetWrapping(wrap)`），
   **上面 1~4 与 TitleTab/AchievementsMenu 那 8 处会一起收口**；本件**没动它**（不在白名单，且那是一次**跨 6 文件**的收口，得单独派）。

**B. `Cell.LabelWrap` 已经就绪，收藏窗那一半可以直接接**：`Core/FilterPanelModel.cs:277` 的显式字段是**收藏窗与卡组编辑窗共用**的模型
⇒ `Shell/CollectionWindow.cs`（主表 #20 那一半）只要照 `DeckRuntime` 那一行写 `lb.SetWrapping(c.LabelWrap == 1)` 即可（**本件没动那个文件**）。

**C. 顺手发现（⛔ 只报不改）**
1. **卡背页那颗 `'Owned only'` 的 auto 下限可能是偏离**：原版 `auto[26~32]`，而我们共用常量 `ToggleFontAutoMin = 18f`
   （`Core/FilterPanelModel.cs:151`）⇒ 若采纳，要**按窗分参数**（同 ⑩ 那条裁定的做法）。**本件没改**（判据文件只说折行那一半）。
2. **子表 C 有一处我顺手读到了真值**：`Shell/AlliancesTab.cs:579`（建盟页 `Price Display > text`，`'1000'`）原版
   **`折行=0 auto[13.46~40] 对齐=Center/Capline`**，而它走的是 `SocialWindow.Text → MenuDraw.TextBox`（**恒折行**）
   ⇒ 是一处**真偏离**；按红线**只报不改**（判据文件把 C6~C24 记作「没查清」，这一条连同 dump 命令可以省下一轮查）。
   ⚠️ 同一族的 `SocialWindow.Text`（`Shell/SocialWindow.cs:292`）**所有调用方**都吃这一口 ⇒ 建议单开一件收口。
3. **`3` 为什么出现在输入框上——查到判据了**（对「普查没查清的①」是个补充）：
   `TMP_InputField.SetTextComponentWrapMode()`（`TMP_InputField.cs:4618-4627`）：
   `multiLine ? Normal : PreserveWhitespaceNoWrap` ⇒ 那两处 `3` 都是**单行输入框**（原版行为，不是随手设的）。
4. **`Shell/PracticeModePopup.cs` 的 `RelayoutNow` 现在与 `Label.ForceRelayout` 重复**（该文件不在本件白名单）
   ⇒ 可退化成直调公共件（**行为等价**，但两份实现迟早不一致，属「两处写同一条规则」那族）。
5. **`MenuDraw.ClipText` 重裁偏移（批四写手记的那条）被本件放大了**：A205 上线后，凡是「`SetWrapping` 真改了模式」的标签
   都会多一次重排 ⇒ 落在 `ClipText` 覆盖面上的（如 `PlayerProfileWindow.Text` 在 `Clip != null` 时、`PracticeModePopup` 的卡组格）
   会多走一次「重裁 + 之后 `RefreshBounds` 再挪位」那一步。**未实测**，建议在 A205 相关首跑时重点看那几处（判据同批四报告 §五·2）。

**D. 本件**没做**的、明确要补的两笔**：
1. **⑫④ 三处常量没有断言**（见 §六·3）。
2. **`3` 与 `0` 的渲染等价性没有判据**（见 §三·3）。

---

## 八、改动清单（`git diff --numstat`，2026-10-07 收尾现读）

```
   87    4   Unity/MyGame/Assets/CardPresentation/Battle/Label.cs
  148    0   Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs
  486   13   Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs     ← 含批四等别的写手的未提交改动
   11    2   Unity/MyGame/Assets/CardPresentation/Shell/AlliancesTab.cs
   15    5   Unity/MyGame/Assets/CardPresentation/Shell/AvatarTab.cs
   15    1   Unity/MyGame/Assets/CardPresentation/Shell/BoosterInfoPopup.cs
   13    2   Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs
   99   30   Unity/MyGame/Assets/CardPresentation/Shell/ProfileTab.cs
    9    0   Unity/MyGame/Assets/CardPresentation/Shell/PromptPopup.cs
   23    8   Unity/MyGame/Assets/CardPresentation/Shell/RankedTab.cs
  156   28   Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs    ← 含 A171 的未提交改动（本件只 +~9 行注释）
   37    0   Unity/MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs
  108   10   Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs        ← 含 ⑭ 的未提交改动（本件 ≈ +95）
```

- **行尾**：改前 CRLF 的（`Label.cs` / `DeckRuntime.cs` / `DeckScene.cs`）改后仍**全 CRLF**；LF 的仍全 LF（逐文件数过 `\r\n` == `\n`）。
  ⛔ 全程用 Edit 工具 / python `wb`，**没有** `sed -i`。
- **typecheck**：本件每批改完各跑一次，**两遍都 0/0**（`TMPDIR=/tmp/wf_w8m bash d:/4/Unity/工具/typecheck.sh`）。
  ⚠️ **没有跑任何 Unity 自检**（铁律 12 / 红线：代理不跑 Unity）。
- **跑哪几条（给同步点）**：本件动了 **`Battle/Label.cs`（共用件）** + 12 个文件 ⇒ 按铁律 12 的覆盖面判据，**只少要看**
  `DeckScene.Run` + `MainMenuScene.Run`；`Shell/SettingsWindow.cs` 只有注释、`Shell/PromptPopup.cs`/`Shell/AlliancesTab.cs`/`Shell/BoosterInfoPopup.cs`
  属别的宿主（`SettingsScene` / `ShellScene` / `ShopScene`）⇒ 若 **`Label.cs` 的 `ForceRelayout`** 被视为跨面共用件改动，则按口径 **跑全套**。
  🔴 **首跑重点观察项**（我无法静态证明、可能第一次就红的 5 条）：
  ① `DeckScene` 的 A205 探针「80px 框里真的折了 > 1 行」（依赖字体度量）；
  ② 探针里 `LineCount == 1`（依赖 `ForceRelayout` 确实重排）；
  ③ `DeckScene` 的搜索框 `WrappingMode == 3`；
  ④ `MainMenuScene` 的 `CheckWrapMode` 系列（尤其**出厂关着**那几处：改名窗、`Info Section with Alliance`、`Current Rank/Timer`）——
     用的是「直接取该节点自己的组件」的读口，**不**依赖激活态；
  ⑤ `DeckScene` 页签三颗 `1/1/0`。
  ⛔ 若其中哪条红，**先看是不是我上面写的口径错**（尤其 ①，那是探针的构造问题不是实现问题），别直接改实现去迁就断言。

### 每组「怎么改坏就红」
| 组 | 改坏法 | 期望 |
|---|---|---|
| A205 | 删 `SetWrapping` 里的 `ForceRelayout()` | `DeckScene` 探针第 4/5 条红（行数、宽度） |
| A62（#5） | 把 `SetWrapping(c.LabelWrap == 1)` 换成 `SetWrapping(false)` | 费用桶那条红（原版 1） |
| A62（#5） | 按 `LabelCenter` 反推（改回旧写法） | 稀有度/类型那两条红 |
| A62（#2） | 改成三颗一起 `false` | 前两颗页签那条红 |
| ① | `SetWrappingMode(3)` → `SetWrapping(false)` | 改名窗 `Text` 那条红（期望 3，实得 0） |
| ① | 把 `SetWrappingMode` 的默认分支改成「退回 0」 | 负例（7）那条红 |
| ③ | 删任一处 `wrap: true` | 今天**不红**（副作用还在）—— 它的价值在「别人动 `SetAutoFitBox` 时当场红」 |
| ⑩（E1/E2） | 删 `SetWrapping(false)` | 聊天预览两条红 |
| ⑩（按窗） | 字号改回 `InputFontPx` / 模式改回 `SetAutoFitBox` | `DeckScene` 那三条各红 |
| ⑫④ | 三个常量改回旧值 | **今天不红**（无断言，见 §六·3） |

---

## 九、跨组总账

- **改了 12 个源码文件**（`Label.cs` · `ProfileTab` · `AvatarTab` · `RankedTab` · `BoosterInfoPopup` · `MainMenuRuntime` · `PromptPopup` ·
  `SettingsWindow` · `AlliancesTab` · `FilterPanelModel` · `DeckRuntime` · `MainMenuScene`(断言) · `DeckScene`(断言)）+ 本报告。
  **一行都没越白名单**；**没有**动两张正本、**没有**动 git、**没有**跑 Unity。
- **补了 53 条断言**（`DeckScene` 31 · `MainMenuScene` 22）；其中能**真分两态**的 = A205 那 5 条 + 模式档位那批（0/1/3 三档互不混淆）。
- **没做完的**：§七·A（白名单外的 5 组，属于**别的写手/后续件**，不是本件缺工）· §七·D（两笔明确要补的小账：三处常量断言 · `3`↔`0` 等价性判据）。
- **要请调度台分流的三笔**（都在 §七·C）：卡背 auto 下限 26 vs 18 · `SocialWindow.Text` 那族的 `TextBox` 恒折行 ·
  `PracticeModePopup.RelayoutNow` 与 `Label.ForceRelayout` 的重复。
