# WSmall5 · 五个一行级修正（A635 · A641 · A642 · A646 · A647）

> 写手 **WSmall5**（第四轮「清空 A 表」· 五个一行级）· 账 = **A635 + A641 + A642 + A646 + A647**
> 白名单：`Shell/CampaignRewardWindow.cs`（①）· `Shell/DailyStreakPopup.cs`（②③）· `Shell/DeckInfoPopup.cs`（④）·
> `RuleEngine/Data/DeckLibrary.cs`（⑤，只改注释）· 断言宿主 `Editor/CollectionScene.cs`（④）· 本报告。
> ⛔ 没跑 Unity · 没动 git · 没碰白名单外任何文件 · 没改两张正本。
> 行号 = **本件改完那一刻的现读**。

---

## 一、结论

| 账 | 状态 | 一句话 |
| --- | --- | --- |
| **A635** `Warning` 被右对齐 | ✅ **做完（两处 —— 第二处如实标，见 §二·4）** | ①`Warning` 那句 `Text(…)` 的 `align` **实参 `2` 删掉**（`2` 在本仓 = **右对齐**，而原版是 **Center**）；②`Point Count` 补 **`align: 1`**（原版 **Left**），原来没传 ⇒ 居中 |
| **A641** `Header Background (1)` 该走 `Nine` | ✅ **做完** | `MenuDraw.Rect` → **`MenuDraw.Nine(…, HeaderBorder, HeaderTexW, HeaderTexH, QPanel, null, true, "Header Background (1)")** |
| **A642** `Fill Line` 色值 | ✅ **做完** | `FillLineTint` `(0.43, 0, 0.06, 0.62)` → **`(0.434, 0, 0.0567, 0.624)`** |
| **A646** `Select Deck` 失败用普通级 | ✅ **做完** | `Debug.Log` → **`Debug.LogWarning`**（正文一字不动） |
| **A647** `DeckLibrary` 注释过期 | ✅ **做完（两段 —— 第二段如实标）** | 按铁律 5 重写那一 `<para>`（保留更正痕迹）；**并且**同一段 doc 里**紧邻的另一句也一样过期**（「它们现在是『调完再自己 `Lib.Save()` 一次』的写法」）⇒ 一并更正 |
| **断言** | ④ **已落盘**（`Editor/CollectionScene.cs`，2 条 + 2 处抓取）· ①②③ **只报不改**（宿主 `Editor/RewardsScene.cs` **那一刻正在被另一位写手写** —— 硬证据见 §七·0） | ①②③ 的断言已写成**可直接粘贴的代码**（§七·1～七·3） |

- **改动量**（`git diff --numstat`，含本件开工前别人已落的部分）：

| 文件 | 本件开工时基线 | 现在 | 本件净增 |
| --- | --- | --- | --- |
| `Shell/CampaignRewardWindow.cs`（①） | 159 / 66 | **180 / 68** | +21 / −2 |
| `Shell/DailyStreakPopup.cs`（②③） | 140 / 7 | **167 / 9** | +27 / −2 |
| `Shell/DeckInfoPopup.cs`（④） | 34 / 5 | **40 / 5** | +6 |
| `RuleEngine/Data/DeckLibrary.cs`（⑤） | 0 / 0（干净） | **15 / 6** | +15 / −6 |
| `Editor/CollectionScene.cs`（断言） | 574 / 0 | **604 / 0** | +30 |

- **行尾**：改前改后**逐文件核对**（二进制读 `count(b'\r\n')` vs `count(b'\n')`）——
  `CampaignRewardWindow` / `DailyStreakPopup` / `DeckInfoPopup` / `CollectionScene` 是**纯 LF**（CRLF 0）·
  `DeckLibrary.cs` 是**纯 CRLF**（327 / 327，**没有被翻**）。**一行都没翻**。
- **类型检查**：最后两次读数是 **运行时 4 错 / 编辑器 0 错**，**4 条全在 `Battle/BattleDriver.cs`**（别人的在飞文件，见 §十）。

---

## 二、A635 —— `Warning` 的文字被右对齐（+ `Point Count`）

### 2·1 判据（本件**亲读**，⛔ 不是转述 WA537）

```bash
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 12
```

| 原版节点 | 那一行的「对齐」格 | TMP `m_HorizontalAlignment` |
| --- | --- | --- |
| `Warning`（**只有高级列**有，`'Warning or Tip'` 字号 47.5） | **`Center/Middle`** | **2** |
| `Point Count`（两列各一颗，`'100'` 字号 32.3） | **`Left/Midline`** | **1** |
| `Claimed Text`（两列各一颗，`'Change Deck'` 字号 35.0） | `Center/Middle` | 2 |

### 2·2 两套枚举**不是一套**（本件要写进报告的那一格）

| | 值 → 含义 |
| --- | --- |
| **原版 TMP** `HorizontalAlignmentOptions` | `Left = 1` · **`Center = 2`** · `Right = 4` · `Justified = 8` · `Flush = 16` |
| **本仓** `GameWindow.Text(…, int align, …)`（`Shell/WindowsManager.cs:346-358`） | **`0 = 居中`（`Label` 默认，什么都不做）** · `1 = 左`（`MenuDraw.AlignLeft`）· `2 = 右`（`MenuDraw.AlignRight`） |

⇒ **只有 `1` 两边同义**；`2` 恰好是「**原版的居中 / 我们的右**」。那个实参 `2` 当年就是**照着原版那个 `2` 写下来的**
（`Shell/WindowsManager.cs:318` 的 doc 原文还写着「那颗 `Warning` 原版就是 `Right`」——**那句是错的**）。

### 2·3 改法（两行）

`Shell/CampaignRewardWindow.cs`

| 行（改后现读） | 改前 | 改后 |
| --- | --- | --- |
| `:645` | `wr.W, WarnAutoMin, **2**, autoMaxPx: …, autoBasePx: …` | `wr.W, WarnAutoMin, autoMaxPx: …, autoBasePx: …`（`align` 退回缺省 `0` = 居中） |
| `:702` | `autoMaxPx: CostAutoMax, autoBasePx: CostAutoBase` | `autoMaxPx: CostAutoMax, autoBasePx: CostAutoBase, **align: 1**` |

- ⚠️ **第二处用命名实参 `align:`**（⛔ 不是位置实参）—— `WindowsManager.Text` 的 doc 明写：`align` 后面跟着两个
  `float` 形参，`int` 字面量能隐式转 `float`，**位置一错就静默绑到 `autoMaxPx`**。
- 两处都写了更正留档（原文怎么错的 / 错因 / 判据出处 / A537 让偏差从「看不出来」变成「看得出来」）。

### 2·4 🔴 如实标：**第二处（`Point Count`）越出了简报的字面「改法一行」**

- 简报 §① 的**判据行**里把两颗都点了名（「`Warning` 的 TMP `m_HorizontalAlignment = 2(Center)` ·
  `Point Count` = `1(Left)`」），末句又写着「**⛔ 别只改一处就完**」；
  WA537 报告 §九·2 也写着「**建议与 九·1 并成一笔**」⇒ 本件按**一笔**做。
- **如果调度台认为该拆成单独一笔**：回退 `:702` 的 `align: 1` 即可（一行的改动，`Warning` 那处不受影响）。
- ⚠️ 两处**都在本件白名单文件里**，没有越文件。

---

## 三、A641 —— `Header Background (1)` 走 `MenuDraw.Nine`

### 3·1 判据（**两处互证**，都是本件亲读）

① `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10`：

```
2  Header Background (1)   -462.1  21.7  87.9  137.0  550.00×115.36  Image,LayoutElement
     WF_Campaign_Info_Background 740×167 九宫335,0,395,0 | **Sliced** (1,1,1,1)
```

② **全量清点**（防「只有这一颗是这样」）：`bundle_menus_assets_all/MonoBehaviour/` 里**用这张 sprite
（`m_PathID 6473405944757030420`）的 20 个 `Image` 实例，`m_Type` 全是 `1`**（`grep -l` 出文件名再逐个读 `m_Type`）
—— 那颗底图**在整个包里没有一处走 `Simple`**。其中
`MonoBehaviour_-3814993926740998224.json`（简报点的那一份）亲读 =
`m_Type: 1` · `m_PixelsPerUnitMultiplier: 1.0` · `m_FillCenter: 1` · `m_Color: (1,1,1,1)` ✓

### 3·2 改法

`Shell/DailyStreakPopup.cs:528-529`（`BuildHeader`）

```csharp
// 改前（1 行）
MenuDraw.Rect(h, Art(ArtHeaderBg), H_Bg, "Header Background (1)", QPanel);
// 改后（2 行）
MenuDraw.Nine(h, Art(ArtHeaderBg), H_Bg, HeaderBorder, HeaderTexW, HeaderTexH, QPanel,
              null, true, "Header Background (1)");
```

- **调用形状照同族先例**：`Shell/LiveOpsEventWindow.cs:766-768` 对**同一个原版节点**走的就是
  `MenuDraw.Nine(…, HeaderBorder, HeaderTexW, HeaderTexH, q)`（常量在同文件 `:185-188`）。
- 🔴 **一处与先例【有意不同】的实参**：**把名字显式传进 `Nine`**（`Nine` 的缺省名是 `"Nine"`）。
  理由：这一颗**原来节点自己就是那个 quad**（名字 = `Header Background (1)`），而 `Nine` 返回的是**根节点**
  ⇒ 不传名就**改名成 `Nine`**，按名字找它的断言/工具会全部落空。
  ⚠️ 同文件那颗 `Header Background`（A519 建的）**形状本来就不同**：它**外面还有一层具名节点**
  （`MenuDraw.Node(h, "Header Background", H_Plate)` + 内层缺省名 `"Nine"`）—— 那一颗不动。
- ⚠️ **视觉差很小、如实标**：`scX` 从 0.7433（`Simple` 无此概念，这里指另一颗）落到 **0.7534**，
  而**中段本来就被压成 0**（`border.L + border.R = 730 > 550`）⇒ 角块只被按比例压一点点。
  **但铁律 11 仍然要做**（与原版不符 ⇒ 完全复刻）。

### 3·3 顺带的**行为面提示**（给调度台 / 后面写断言的人）

🔴 这一改**把那一颗从「`ImageQuad` 节点」变成「`Nine` 根节点（两个 `ImageQuad` 子件）」**：

- 任何**按名字量它**的既有/新断言，⛔ **不能再用 `RectOf`**（`RectOf` 先找子件里的 `Label`、再找**直接** `ImageQuad`
  —— 见 `Editor/RewardsScene.cs:519-533` 的实现），要用 **`RectOfUnion`**（`:562`，它扫整棵子树的**激活** quad 取并集）。
- 我 grep 过全仓 `.cs`：**今天没有任何 C# 代码按名字找 `Header Background (1)`**（只有注释与文档提到它）⇒ 本次改名零破坏。
- ⚠️ **但 `Editor/RewardsScene.cs` 那一刻正在被别人写**（见 §七·0）⇒ 若他新加的断言刚好量这一颗，**用 `RectOf` 会假红**。

---

## 四、A642 —— `Fill Line` 的色值

- **判据**（本件亲读）：`python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10`
  那一行的行末 = `… 40k_generial_bar_fill 12×12 九宫4,4,4,4 | Sliced **(0.434,0,0.0567,0.624)** ppuMul=2.0`。
  ⚠️ 那个三元组的**来源**是本件现读 `工具/menu_dump.py:573` = `f'{IMG_TYPE[m_Type]} {_c(mb["m_Color"])} '`
  ⇒ 它印的就是 **`Image.m_Color` 本体**（不是工具另算的）✓。
- **改法**：`Shell/DailyStreakPopup.cs:202`
  `new Color(0.43f, 0f, 0.06f, 0.62f)` → **`new Color(0.434f, 0f, 0.0567f, 0.624f)`**，
  并把原值、差值（**0.004 / 0 / 0.0033 / 0.004**）、判据出处写进 doc。
- 同时把**调用点**那段「色值仍用旧的…那一笔不属于本账（另立账）」的注释就地改成「A642 就是那条另立的账，已做」
  （否则同一份文件里两处说法打架 —— 铁律 5）。

---

## 五、A646 —— `DeckInfoPopup` 的 `Select Deck` 失败仍用普通级

- **改法**（`Shell/DeckInfoPopup.cs:1197`，正文一字不动）：

```
-                else Debug.Log("[DeckInfo] 选中卡组失败：" + CollectionData.LastSelectError);
+                else Debug.LogWarning("[DeckInfo] 选中卡组失败：" + CollectionData.LastSelectError);
```

- 上面补了 5 行更正留档：**为什么当年用 `Log`**（与 A548 那两句同通道）· **为什么那条理由已失效**
  （A610 已把 `DeleteDeck` / `DuplicateDeck` 的失败支统一成 `LogWarning`）· **为什么现在它成了本窗唯一**
  （本文件里 `Blocked()` / `没有登记过这颗钮` / 取不到图 一律 `LogWarning`）。
- **顺带核过**：本文件里其余 `Debug.Log` 全是**信息级**的正常路径（`已选中` / `已删除` / `已复制` / `state = …`），
  **只有这一处是「失败走普通级」** ⇒ 改完口径齐了（✅ 成功那两句按 A610 的口径**不动**）。

---

## 六、A647 —— `DeckLibrary` 里那段过期注释

`RuleEngine/Data/DeckLibrary.cs`（**纯 CRLF，改完仍是 327/327**）

### 6·1 主目标（`:179-190`，原来是 `:176-179`）

原文（一句「如实标注」）：

> ⚠️ **如实标注（本笔没做的那一半）**：`Shell/CollectionData.cs` 的 `CreateDeck` / `DuplicateDeck` / `ImportDeck`
> **既不读返回值、也不读 `LastError`** ⇒ 写盘失败时玩家看到的是「操作成功」，而盘上没变。**那是另一笔账**…

**按铁律 5 重写为**（保留更正痕迹：原来写 X / 实际是 Y / 错因 Z / 出处）：

- **实际是（A503 / A611 起，那三处全读了）**：
  `CreateDeck`（`Shell/CollectionData.cs:240`，返回卡组名、落盘失败回空串）·
  `DuplicateDeck`（`:190`，同上，另有出口 `LastDuplicateError`）·
  `ImportDeck`（`:215`，返回名 + `out string why`）—— **三处都读 `Lib.LastError` 并 `Debug.LogWarning`**；
  调用点也读返回值（例：`Shell/CollectionWindow.cs:2701` 的 `string name = CollectionData.CreateDeck();`）。
- **错因**：写这条注释时（A398）那几处确实还没读，**A503/A611 补上之后没有回头改这里** ⇒ 留下的是一条
  「其实已经做完的欠账」。
- ⛔ 明确写上 **「别照这段旧话去『补做』那三处」**。

（本件**逐条亲读**过上面四个行号，不是转述。）

### 6·2 🔴 如实标：**同一段 doc 里紧邻的另一句也一样过期，本件一并改了**

`:174-176` 原来是：

> ⛔ **别为了回传 `bool` 去改那三个方法的签名** —— `Shell/CollectionData.cs` 那几处调用点会跟着断
> **（它们现在是「调完再自己 `Lib.Save()` 一次」的写法）**。…

**那半句也是假的** —— `Shell/CollectionData.cs:194 / :221 / :243` 三处**现在各挂着一句**
「⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `SaveOrWarn`）」。本件按**铁律 5 末段**
（「顺手 `grep` 那句话的关键词，把同一句话被复制到别处的地方一起改」）把它一并更正，
并保留更正痕迹。⇒ **本件在 `DeckLibrary.cs` 里改了【两段注释、零行代码】**
（`git diff -U0` 那边只有 `///` 行）。

---

## 七、断言清单 / 或「为什么不需要断言」

### 7·0 🔴 为什么 ①②③ **只报不改**（硬证据，不是偷懒）

简报写着：「若那一刻有人在动它，就只报不改、把断言规格写进报告」。**那一刻确实有人在动**：

| 时刻 | `Editor/RewardsScene.cs` | `Editor/CollectionScene.cs` |
| --- | --- | --- |
| 10:26:58 | mtime 10:26:58 · 812,294 B · `diff 716/41` | — |
| 10:33:59 | **mtime 仍是 10:26:58**（看着像停了 7 分钟） | mtime 10:29:46 · `diff 574/0` |
| 10:38:44 | 🔴 **mtime 10:38:41 · 833,565 B**（刚写完） | mtime 10:29:46（**10 分钟没动**） |
| 10:39:12 | 🔴 **mtime 10:39:10 · 835,162 B · `diff 957/41`**（30 秒内**又**写了一次） | mtime 10:29:46 |
| 10:39:41 | mtime 10:39:10 | **= 本件自己那三笔**（`diff 604/0`） |
| **10:42:25** | mtime 10:39:10 | 🔴 **被第三方写了**：`diff 604/0 → 661/0` · 494,017 → **500,572 B** · 5702 → **5759 行** |

⇒ `RewardsScene.cs` **30 秒内被写了两次** = 一位写手**正在流里** ⇒ **本件一个字节都没碰它**。
⇒ `CollectionScene.cs` 在**本件动它的那一刻**已经 **10 分钟**没有第三方写入 ⇒ 本件在它里面落了 ④。

🔴 **后续（如实记，给同步点）**：本件收工前 3 分钟，**`CollectionScene.cs` 被别人写了**（`+57 行`）。
本件**逐条 grep 复核过**：④ 那两处抓取（`okLv` / `selLv`）与两条断言**都还在、位置也还有意义**
（现读 `:5365-5374` · `:5383-5386` · `:5403-5418`），并且**合起来仍然编得过**（§十 第 ④ 次读数：编辑器 0 错）
⇒ **两边没有互相覆盖**（各自走 Edit 的字符串替换、不是整篇 `Write`）。
⚠️ 但**这说明 `CollectionScene.cs` 那一刻也有写手** ⇒ 同步点做 `git diff` 时请把这一段的**归属**看清：
`:5365-5374` / `:5383-5386` / `:5403-5418` 是**本件**的，其余 `+57` 行不是。

### 7·1 ① A635 —— **只报不改**（宿主 `Editor/RewardsScene.cs` 正被写）· 待落代码

**落点**：`Editor/RewardsScene.cs` 的 **A537⑦ 那一段之后**（现读 `:5091-5103` 那个 `if` 里，`px1/py1/px2/py2`
与 `warnLk` 都已在作用域里）。**贴着 A537⑦ 的 `}` 后面**插：

```csharp
                // 🆕 **2026-10-13（A635）**：`Warning` 的**文字在框里怎么摆** —— A537⑦ 只钉了**节点**（框心），
                //   钉不住那颗字（那是 `align` 管的）。原版 `Warning` 的 TMP `对齐=Center/Middle`
                //   （`m_HorizontalAlignment = 2` = `HorizontalAlignmentOptions.Center`），而本仓
                //   `GameWindow.Text` 的 `align` 是**自己的枚举**（`0=居中 · 1=左 · 2=右`，只有 `1` 与 TMP 同义）
                //   —— 那个 `2` 当年就是被当成「原版的 2」传进去的 ⇒ **右对齐**（A635 已删）。
                //   ⛔ 量的是**文字块的真渲染矩形**（`RectOf` 走 `Label.WorldW` + `AlignLeft/Right` **挪过的节点位置**），
                //      不是节点框 —— 框心在三种对齐下**恒等于** A537⑦ 量到的那个数（那一条分不出两种状态）。
                //   **改坏法**：把 `Shell/CampaignRewardWindow.cs` 那句 `Text(…)` 的 `align` 再加回 `2` ⇒ 红。
                if (warnLk != null && RectOf(warnLk, out float tx1, out float ty1, out float tx2, out float ty2))
                {
                    float frameCx = (px1 + px2) * 0.5f;   // 锚 (0.1,0)-(0.9,0) + pivot .5 ⇒ 框心 = holder 心
                    CheckTrue(tx1 >= px1 - 0.5f && tx2 <= px2 + 0.5f,
                              "★ A635：（前提）那颗字**整个在 holder 里**（否则下面那条的中心相等可能是巧合）");
                    CheckNear((tx1 + tx2) * 0.5f, frameCx, 1.5f,
                              "★★ A635：锁支的 `Warning` **文字块居中在框里**（原版 `对齐=Center/Middle`）"
                            + $"—— 文字中心 {(tx1 + tx2) * 0.5f:F1} · 框心 {frameCx:F1} · 文字宽 {tx2 - tx1:F1}"
                            + "；对齐改回 `2`（右对齐）⇒ 文字中心右移 (框宽 − 文字宽)/2 ⇒ 红");
                }
```

⚠️ **鉴别力如实标**：它的判别量 = `(框宽 − 文字块宽) / 2`。这一档夹具（`cwA402`，UM0 **1 个物品**）
⇒ holder 内容宽 **260** ⇒ `Warning` 框宽 = `0.8 × 260 + 30` = **238**。
**只要文字块宽不是恰好 238，这条就分得开**；文字**铺满整框**时中心/右对齐在**视觉上也等价**（原版同样如此）。
⇒ 想要**更大刻度**的话：另建一扇 `Rewards = CampaignData.RewardsOf(2)`（**2 个物品** ⇒ 框宽 **418**）+
`IsPremiumLocked = true` 的夹具，判别量按同式放大（**这是可选项，不是必要条件**）。

### 7·2 ①′ A635 第二处（`Point Count` 左对齐）—— **只报不改** · 待落代码

**落点**：`Editor/RewardsScene.cs` 的 **`:4964` 那个 `}` 之后**（那里 `costLb`(`:4948`) 与 `ubtn`(`:4954`) 都已声明；
⚠️ **别插在 `:4948` 那一带** —— `ubtn` 是后面 6 行才声明的）。

```csharp
            // 🆕 **2026-10-13（A635）**：`Point Count` 的**左对齐** —— 原版那一颗 TMP `对齐=Left/Midline`
            //   （`m_HorizontalAlignment = 1`）。框 = 按钮右半格减右边 5：原版 `Point Count 870.0 857.2 987.5 887.8`
            //   （**框宽 117.50**）、按钮 `742.5 850 987.5 895`（宽 245、中心 **865**）⇒ **框左沿 = 按钮中心 + 5**。
            //   ⛔ 不从 `UguiRect` 的算式反推框（那是**我们**算的）；用「按钮中心 + 5」这个**原版读数**。
            //   **改坏法**：删掉 `Shell/CampaignRewardWindow.cs:702` 的 `align: 1`（退回居中）
            //   ⇒ 左缘右移 (117.5 − 文字宽)/2 ≈ **30px**（"100" 在字号 32.32 下约 55～60 宽）⇒ 红。
            if (costLb != null && ubtn != null && RectOf(costLb, out float kx1, out float ky1, out float kx2, out float ky2))
            {
                CheckNear(kx1, PxOf(ubtn.position.x) + 5f, 2f,
                          "★★ A635②：`Point Count` 的**真渲染左缘 = 按钮中心 + 5**（原版 `对齐=Left/Midline`）"
                        + $"—— 实测左缘 {kx1:F1} · 期望 {PxOf(ubtn.position.x) + 5f:F1} · 文字宽 {kx2 - kx1:F1}"
                        + "；不传 `align: 1`（居中）⇒ 左缘右移约 30px ⇒ 红");
            }
```

### 7·3 ② A641 / ③ A642 —— **只报不改**（同一个宿主）· 待落代码

**落点**：`Editor/RewardsScene.cs` 的 `§五 Daily Streak Popup` 段内、**A493 那两条（现读 `:6491` 一带）之后**。
（`ds` / `succ` 两个变量都在 `:6396` 前后已声明。）

```csharp
            // ============================================================ 🆕 **2026-10-13（A641 + A642）**
            //  A641：顶栏左边那颗 `Header Background (1)` —— 原版是 **`Sliced`**（`m_Type = 1`），
            //    我们原来走 `MenuDraw.Rect`（`Simple`：把整张 740×167 拉到 550×115.36）。
            //    判据（⛔ 全是**原版读数**）：sprite `WF_Campaign_Info_Background` **740×167 · border (335,0,395,0)** ·
            //    `m_PixelsPerUnitMultiplier = 1` · 框 **550 × 115.36** ⇒ 按 uGUI `Image.GetAdjustedBorders`
            //    （`border.x + border.z = 730 > 550` ⇒ **逐轴**按比例压）算出来：
            //      `scX = 550 / 730 = 0.753425` ⇒ 左角块 `335 × 0.753425 = 252.40px` · 右角块 `395 × 0.753425 = 297.60px`
            //      （两者之和 = 550 ⇒ 中段宽 0）；竖向 border 是 0 ⇒ **只有一行**
            //      ⇒ **一共 2 块**（`Simple` 是 1 块、且宽 550）。
            //  A642：`Fill Line` 的 `m_Color` 原版实读 `(0.434, 0, 0.0567, 0.624)`，我们原来 `(0.43, 0, 0.06, 0.62)`
            //    ⇒ 容差 0.001 分得开（四个分量差 0.004 / 0 / 0.0033 / 0.004）。
            {
                var bg1 = FindPath(ds.transform, "Header With Back Button/Header Background (1)");
                CheckTrue(bg1 != null, "★ A641：（前提）`Header Background (1)` 按**名字**找得到"
                                     + "（`Nine` 的缺省名是 `Nine` ⇒ 实现里把名字**显式**传进去了）");
                if (bg1 != null)
                {
                    var q1s = bg1.GetComponentsInChildren<ImageQuad>(true);
                    Check(q1s.Length, 2, "★★ A641：`Header Background (1)` 是**九宫格**（原版 `m_Type = 1`）"
                                       + "—— 竖边 border 为 0 ⇒ 只有一行、横边两角块 ⇒ **2 块**"
                                       + "；退回 `MenuDraw.Rect`（`Simple`）⇒ **1 块** ⇒ 红");
                    if (q1s.Length == 2)
                    {
                        float wa = q1s[0].WorldW * 108f, wb2 = q1s[1].WorldW * 108f;
                        CheckNear(Mathf.Min(wa, wb2), 252.40f, 1.5f,
                                  "★★ A641：左角块宽 = `335 × scX(0.753425)` = **252.40**"
                                + "（scX 由原版 border 335/395 + 贴图 740 + 框 550 三样算出）");
                        CheckNear(Mathf.Max(wa, wb2), 297.60f, 1.5f,
                                  "★★ A641：右角块宽 = `395 × scX` = **297.60**（两块之和恒 = 550）");
                    }
                    float ux1, uy1, ux2, uy2;
                    if (RectOfUnion(bg1, out ux1, out uy1, out ux2, out uy2))
                        CheckNear(ux2 - ux1, 550f, 1f,
                                  "★ A641：（前提）并集宽仍是 **550**（= 原版那一格的 `m_SizeDelta`/锚算出来的宽；"
                                + "钉住「换画法不改框」）");
                }

                var fl = FindChild(succ.transform, "Fill Line");
                var flQ = fl != null ? fl.GetComponentInChildren<ImageQuad>(true) : null;
                CheckTrue(flQ != null, "★ A642：（前提）`Fill Line` 那颗九宫格建出来了");
                if (flQ != null)
                {
                    CheckNear(flQ.Tint.r, 0.434f,  0.001f, "★★ A642：`Fill Line` 的 `tint.r` = **0.434**（原版 `m_Color.r`；退回 0.43 ⇒ 红）");
                    CheckNear(flQ.Tint.g, 0.0f,    0.001f, "★★ A642：…`tint.g` = **0**（原版）");
                    CheckNear(flQ.Tint.b, 0.0567f, 0.001f, "★★ A642：…`tint.b` = **0.0567**（退回 0.06 ⇒ 红）");
                    CheckNear(flQ.Tint.a, 0.624f,  0.001f, "★★ A642：…`tint.a` = **0.624**（退回 0.62 ⇒ 红）");
                }
            }
```

- ⚠️ **`RectOf` 那条为什么不能用**（A641 专用纪律）：`RectOf`（`:519-533`）先找 `Label`、再找**直接** `ImageQuad`
  ⇒ 对 `Nine` 根节点**量不到**（它自己不是 quad、子件才是）⇒ 必须 **`RectOfUnion`**（`:562`）。
- ⚠️ `Check(int, int, string)` / `CheckTrue(bool, string)` / `CheckNear(float, float, float, string)` /
  `FindChild` / `FindPath` / `RectOf` / `RectOfUnion` **都是 `RewardsScene.cs` 的现成静态件**（本件逐个读过签名）✓
- ⚠️ 标识符 `bg1` / `q1s` / `wa` / `wb2` / `ux1…` / `fl` / `flQ` / `tx1…` / `kx1…` / `frameCx`
  —— 落笔时请**再 grep 一遍**（`RewardsScene.cs` 那一刻正被别人改，命名可能撞）。

### 7·4 ④ A646 —— **已落盘**（`Editor/CollectionScene.cs`）

**落点 = 现成那段 A600 夹具里**（`Run()` 尾部「①-d 调用点」那一节），**不另建夹具**（那一节已经会
`OverridePath` 拐到写不进去的路径、又用 `DeckIndex = n0 + 7` 触发失败支 ⇒ 恰好是 A646 要的那一下）：

| 落点（**改后现读**；⚠️ 那一刻另一位写手又往本文件后面加了 57 行，**行号可能再漂**） | 加了什么 |
| --- | --- |
| `:5365-5374` | 好路径那一下**外面**再挂一份**带级别**的抓取（`okLv`：`LogType.ToString()`），`ClickHit` 照旧跑（`grabbed` 语义一字不动） |
| `:5383-5386` | 失败那一下同样挂一份（`selLv`） |
| `:5403-5418`（**在 `①-e 收尾` 之后** · 刻意编成 `①-f`） | 两条断言 |

```csharp
CheckTrue(okLv.Count == 1 && okLv[0] == "Log",     "★★ A646（对照档）：**成功**那一句仍是 `Log` 级…");
CheckTrue(selLv.Count == 1 && selLv[0] == "Warning","★★ A646：`Select Deck` **失败走警告级**…");
```

**为什么这两条不是自证 / 不是弱断言**（派活必查行三条自查）：

1. **两态都抓**：成功档期望 `Log`、失败档期望 `Warning` ⇒ **只抓一边**分不出「抓取器根本不认级别」与
   「级别真的对」（弱断言）；两条一起 ⇒ **一条恒绿的假观测救不了另一条**。
   **改坏法**：把 `Shell/DeckInfoPopup.cs:1197` 改回 `Debug.Log` ⇒ ② 的实得变「Log」≠「Warning」⇒ **红**（① 仍绿）。
2. **期望值不是我们的常量**：期望值是**引擎枚举的名字** —— `LogType.Log` / `LogType.Warning` 的 `ToString()`
   （`UnityEngine.LogType`），**不是本仓任何 `const`、也不是我们写死的一句文案**。
3. **不是同义反复**：断言读的是 `Application.logMessageReceived` **实际投递**给订阅者的那一条
   （不是回读我们传进去的字符串），而 `Count == 1` 与既有那条 `Check(HitCount("[DeckInfo] 选中卡组失败："), 1, …)`
   **各自独立算一遍**（一个数字符串出现次数、一个数级别表长度）。
4. ⛔ **不是「`!RectOfUnion` / 一个 quad 都没有」那一式**（本批点名禁掉的那一类）——这里**没有**矩形断言。
5. ⛔ **没有改** `ClickHit`（`A601/A610/A611` 那几节共用它）——只是**在它外面又挂了一份** handler，
   所以那几节的断言**逐字不变**。

### 7·5 ⑤ A647 —— **不需要断言**（说清为什么）

- 改的**是注释**（`git diff` 那边只有 `///` 行，零行代码）⇒ **运行期没有任何可断言的量**。
- ⛔ 也**不该**为它写「注释里含某句话」式的断言：那种断言咬的是**文案**、不是**行为**，
  下次改注释就会红，而它与「实现对不对」无关（= 本工程反复踩的「断言盯着我们自己的常量」那一族）。
- **它的「验」= 类型检查 + 本报告 §6 的逐条亲读**（四个行号都现读过）。

---

## 八、没查清 / 没做的（⛔ 不猜、不静默）

1. ⛔ **没跑任何 Unity 自检**（简报红线 + 本批口径「A 表清零前不跑中途自检」）⇒
   **本件落盘的那 2 条 A646 断言只过了类型检查，没有一条被执行过**。红绿要等收口那次跑。
2. ⛔ **①②③ 的断言没落盘**（宿主 `Editor/RewardsScene.cs` 那一刻正被别人写，见 §7·0）——
   规格已写成**可直接粘贴**的代码（§7·1 / §7·2 / §7·3），但**它们一次都没编译过**
   （`Check(q1s.Length, 2, …)` 那类 `Check<T>` 重载、`Mathf` / `ImageQuad` 的可见性都只是**照现成用法抄的**）
   ⇒ 落笔时请顺手跑一次类型检查。
3. ⚠️ **A635 那条「文字块居中」断言的鉴别力**（§7·1）**依赖文字块宽 < 框宽 238**：
   这是**几何上不可避免**的（文字铺满框时，中心与右对齐在**视觉上也等价**）。
   本件**没有实拍/没跑 Unity**，**量不出那颗字在 238 框里的实际宽**（`SetAutoFitBox(238, 45, 18, 30)` 的收敛点
   要靠 TMP 真跑一次）⇒ **如实标：这条的刻度待收口那次跑起来才看得到**；
   要更大刻度就照 §7·1 末尾那段另建 **2 个物品**的夹具（框宽 418）。
4. ⚠️ **A641 的「视觉差很小」没有实拍**：`scX = 0.753425` 是**照 uGUI `GetAdjustedBorders` 手算**的
   （`ImageQuad.CreateNineSlice:459-470` 逐条照抄了那条公式），**没跑起来对过**。
   §7·3 那两条断言（252.40 / 297.60）就是这个手算式的**落地判据** —— 它们红了要么是我们错、要么是断言错。
5. ⚠️ **`ItemDrawer.Quantity` 的对齐（`Shell/ItemDrawer.cs:1218` 的 `align: 2`）没查证**（见 §九·2）——
   不在本件账里，也没找到本地那把尺子。
6. ⛔ 没碰：`Shell/CampaignData.cs` · `Editor/RewardsScene.cs` · `Shell/WindowsManager.cs` ·
   `Battle/*`（含只读的 `ImageQuad.cs` / `Label.cs`）· `工具/*` · `项目任务.md` · `CLAUDE.md` · git。
   （`Shell/WindowsManager.cs:316-319` 那段 doc 里「`Warning` 原版就是 `Right`」**是错的**，
   但那个文件**不在本件白名单** ⇒ 只报不改，见 §九·1。）

---

## 九、顺手发现（⛔ 本件只报不改，请调度台分流）

### 九·1 🔴 `Shell/WindowsManager.cs:316-319` 里那句判据是**错的**（本件**没改**，不在白名单）

原文：

> `align`（**0 = 居中（`Label` 默认）· 1 = 左 · 2 = 右**；**原版 TMP 的 `m_HorizontalAlignment`**）
> … `ClipText` 夹的是世界坐标的顶点，先裁再挪会把裁好的块挪出框（**`CampaignRewardWindow` 那颗 `Warning`
> 原版就是 `Right`**，原来写成「建完再 `MenuDraw.AlignRight`」⇒ 已改）。

- **两处都对不上事实**：① 括注把「我们自己的 0/1/2」说成「原版 TMP 的 `m_HorizontalAlignment`」
  —— 两套枚举**只有 `1` 同义**（原版 `Center = 2 / Right = 4`）；② 「那颗 `Warning` 原版就是 `Right`」**是错的**
  —— 原版实读 **`Center/Middle`**（§2·1）。
- **它正是 A635 这个缺陷的源头**（写实现的人照着这句给实参填了 `2`）⇒ 建议**下一个写手**：
  把括注改成「`0=居中 · 1=左 · 2=右`，**是我们自己的枚举**（TMP 是 `Left=1 / Center=2 / Right=4`，
  **只有 1 同义**）」，第二处改成「原版是 **Center**，A635 已订正」。
- ⛔ **本件一个字节都没碰它**（不是我的文件）。

### 九·2 `Shell/ItemDrawer.cs:1218` 的 `align: 2` —— **没查证**，同族风险

- `Quantity(...)` 里那句 `ClippedText(node, qr, "x" + qty, …, st.QuantityPx, st.QText, qr.W, st, **2**)`
  走的是**同一套 0/1/2 语义**（`ItemDrawer.ClippedText` 的 doc 里也写着同一句
  「**原版 TMP 的 `m_HorizontalAlignment`**」的括注 —— 同 §九·1 的口径问题）。
- 那一颗原版叫 `ItemDrawerComponents.Quantity`，**它的 TMP 对齐本件没读**（抽屉 prefab 在
  `bundle_menus_assets_all` 里 dump 得出来，但**本件没查**）。
- ⚠️ 那一格的位置**本来就是「我们挑的」**（`Shell/ItemDrawer.cs:1208-1213` 的更正留档自己写着
  「排版值**我们还没照它改**（待做的活）⇒ 这一套位置**仍是我们的**」）⇒ 对齐很可能也一起是「我们的」。
- ⇒ **建议**：核 `ItemDrawerComponents.Quantity` 的 `对齐=` 那一格，与 `align: 2` 对一遍；
  顺带把那份 doc 的括注按 §九·1 改口径。

### 九·3 `GameWindow.Text` 的 **align 参数全表**（本件普查结果，供调度台留档）

**扫法**：python 平衡括号扫描全部 `.cs`，取**裸 `Text(` 调用里位置实参 ≥ 10 个或有 `align:`** 的
（= 真在传这个形参的）。**结论：全仓只有 6 个站点真传了 `align`**，逐条核过如下：

| # | 站点 | 传的 `align` | 我们这边的含义 | 原版那一颗的 `对齐=`（本件 dump 实读） | 判定 |
| --- | --- | --- | --- | --- | --- |
| 1 | `Shell/CampaignRewardWindow.cs:645`（`Warning`） | ~~`2`~~ → 删 | ~~右~~ → 居中 | `Warning` = **Center/Middle** | 🔴 **原来是错的** ⇒ **A635 已改** |
| 2 | `Shell/CampaignRewardWindow.cs:702`（`Point Count`） | （原来没传）→ **`1`** | ~~居中~~ → 左 | `Point Count` = **Left/Midline** | 🔴 **原来是错的** ⇒ **A635 已改** |
| 3 | `Shell/CampaignRewardWindow.cs:706`（`Claimed Text`） | 没传 = `0` | 居中 | `Claimed Text` = **Center/Middle** | ✅ 对 |
| 4 | `Shell/RewardWindow.cs:1075`（`Button Text` / `'Collect'`） | `0` | 居中 | `Reward Window` 的 `Button Text` = **Center/Capline** | ✅ 对 |
| 5 | `Shell/RewardWindow.cs:1104`（`Disclaimer Text`） | `2` | **右** | `Premium Disclaimer` = **Right/Middle** | ✅ 对（这一处 `2` **正好**是「右」，**别顺手改成 0**） |
| 6 | `Shell/RewardWindow.cs:1111`（`Tap Text`） | `0` | 居中 | `Tap To Continue` = **Center/Bottom** | ✅ 对（**Bottom** 那一半由本文件另按行高贴底，不归 `align`） |
| 7 | `Shell/InboxWindow.cs:538`（`RowText`） | `0`（**有意**） | 「不动对齐」 | —— | ✅ 对（它自己排三步：`GameWindow.Text(align:0)` 之后自己调 `MenuDraw.AlignLeft/Right`，判据在 `Shell/InboxWindow.cs:531-536`） |
| 8 | `Shell/ItemDrawer.cs:1218`（数量） | `2` | 右 | **没读**（见 §九·2） | ⚠️ **未查证** |

- ⚠️ **第 5 行特别注意**：`RewardWindow.cs:1104` 传的也是 `2`，但它**是对的** ——
  说明「`2` 一律错」**不成立**，逐处都要读原版那一颗的 `对齐=` 格。
- ⚠️ **其余那些「没传 align」的站点不在本表里**：它们拿的是 `0`（居中），而「居中」是原版最常见的值；
  **但 `Point Count`（第 2 行）正是「没传却错了」的反例** ⇒ 「没传 = 一定对」**不成立**，
  要全量核就得逐窗逐颗读原版的 `对齐=` 格（**那是另一笔账**，本件只做了**真传了 `align` 的 6 处** +
  本窗（`CampaignRewardWindow`）的**全部 3 颗**）。

### 九·4 本件观察到的**并发事实**（给调度台的同步点信息）

- `Editor/RewardsScene.cs` 在 **10:38:41 / 10:39:10 两次被写**（812,294 → 833,565 → 835,162 B；
  `git diff` 从 `716/41` → `957/41`）⇒ 一位写手**正在流里**。
- `Editor/CollectionScene.cs` 在**本件动它时**连续 10 分钟没有第三方写入（⇒ 本件在里面落了 ④），
  **但本件收工前 3 分钟（10:42:25）也被第三方写了**（`604/0 → 661/0`，`+57 行`）。
  ✅ **本件 grep 复核过：④ 那三处都还在、且合起来仍编得过**（§十 第 ④ 次读数）。
  ⇒ 同步点做 `git diff` 时请把 `:5365-5374` / `:5383-5386` / `:5403-5418` 归**本件**，其余 `+57` 行不是。
- ⚠️ **A641 会把 `Header Background (1)` 从「自带 quad 的节点」改成「`Nine` 根节点（2 个 quad 子件）」**
  ⇒ 若 RewardsScene 那位写手新加的断言**按名字量这一颗**，**必须用 `RectOfUnion` 而不是 `RectOf`**
  （`RectOf` 只找**直接**子件里的 quad，见 `Editor/RewardsScene.cs:519-533`）—— 否则会**假红**。

---

## 十、类型检查结果

```
$ cd d:/4/Unity && TMPDIR=/tmp/wf_wsmall5 bash 工具/typecheck.sh      # ① 五处代码改完（断言还没落）
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

$ TMPDIR=/tmp/wf_wsmall5 bash 工具/typecheck.sh                       # ② ④ 的三笔全落盘 + 挪到 ①-e 收尾 之后
--- 运行时程序集 ---
Assets\CardPresentation\Battle\BattleDriver.cs(2632,54): error CS1061 … `PointerDownAt`
Assets\CardPresentation\Battle\BattleDriver.cs(2634,44): error CS1061 … `PointerUpAt`
Assets\CardPresentation\Battle\BattleDriver.cs(2667,28): error CS1061 … `PointerDownAt`
Assets\CardPresentation\Battle\BattleDriver.cs(2668,36): error CS1061 … `PointerUpAt`
运行时错误数: 4
--- 编辑器程序集 ---
编辑器错误数: 0

$ TMPDIR=/tmp/wf_wsmall5 bash 工具/typecheck.sh                       # ③ 重跑一次（同样 4 条、同一个文件）
Assets\CardPresentation\Battle\BattleDriver.cs(2632,54) …（同上四条）
运行时错误数: 4  ·  编辑器错误数: 0

$ TMPDIR=/tmp/wf_wsmall5 bash 工具/typecheck.sh                       # ④ 收工前（**另一位写手刚往
                                                                      #    Editor/CollectionScene.cs 加了 57 行**）
Assets\CardPresentation\Battle\BattleDriver.cs(2632,54) …（同样那四条，行号文件都没变）
运行时错误数: 4  ·  编辑器错误数: **0**

$ TMPDIR=/tmp/wf_wsmall5 bash 工具/typecheck.sh                       # ⑤ **收工那一刻（最后一次）**
运行时错误数: **0**  ·  编辑器错误数: **0**
```

- 🔴 **第 ②③④ 次那 4 条全部集中在 `Assets\CardPresentation\Battle\BattleDriver.cs` 一个文件上**
  （`ChatPopupPanel` 少 `PointerDownAt` / `PointerUpAt`）⇒ 按简报口径：**那是别人的在飞文件**
  （`Battle/BattleDriver.cs` 在本会话开工时就已经是 `M`），**连跑三次数字与文件都没变** ⇒ **如实记、一个字节没碰**；
  **第 ⑤ 次（10:43:26）那 4 条已被它自己的作者修掉** ⇒ **全仓 0 / 0**。
- ✅ **本件碰过的 5 个文件全程 0 错**（`Shell/CampaignRewardWindow.cs` · `Shell/DailyStreakPopup.cs` ·
  `Shell/DeckInfoPopup.cs` · `RuleEngine/Data/DeckLibrary.cs` · `Editor/CollectionScene.cs`）
  —— 第 ① 次读数（还没人写 `BattleDriver.cs` 时）是 **0 / 0**，那一次就含编辑器程序集里的
  `Editor/CollectionScene.cs`（⇒ **本件那两条 A646 断言真的编得过**，这是它们唯一拿到的验证）。
  第 ④ 次读数说明**本件的改动与那位写手新加在 `CollectionScene.cs` 里的 57 行合起来也编得过**（编辑器 0 错）。
- **行尾**（**二进制**读，`count(b'\r\n')` vs `count(b'\n')`；⚠️ 文本模式会把 `\r\n` 折成 `\n`、两数恒相等）：

| 文件 | 行尾 | 改前 → 改后 |
| --- | --- | --- |
| `Shell/CampaignRewardWindow.cs` | 纯 LF | 946 → **967** 行（CRLF 恒 0） |
| `Shell/DailyStreakPopup.cs` | 纯 LF | 591 → **618** 行（CRLF 恒 0） |
| `Shell/DeckInfoPopup.cs` | 纯 LF | 1506 → **1512** 行（CRLF 恒 0） |
| `RuleEngine/Data/DeckLibrary.cs` | **纯 CRLF** | 318 → **327** 行，**`count(b'\r\n') = 327` / `count(b'\n') = 327`**（**没翻**） |
| `Editor/CollectionScene.cs` | 纯 LF | 5672 → **5759** 行（CRLF 恒 0；其中**本件 +30**、另一位写手 +57） |

- **白名单核对**：本件只出现在 `git status` 的 `Shell/{CampaignRewardWindow,DailyStreakPopup,DeckInfoPopup}.cs` ·
  `RuleEngine/Data/DeckLibrary.cs` · `Editor/CollectionScene.cs` **五个文件**上（外加本报告）；
  另建的那个一次性扫描脚本 `d:/4/Unity/_tmp_scan_text_align.py` **已删**。
