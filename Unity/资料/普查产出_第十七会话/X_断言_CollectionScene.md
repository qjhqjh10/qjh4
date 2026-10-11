# X · 断言补课 —— `Editor/CollectionScene.cs`（第十七会话）

**只改了一个文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`
（**LF** 核过：`\n` 8681 / `\r\n` 0 · 改前 8502 → 改后 8681 = **+179 行，纯插入、零删除**）
**类型检查**：`TMPDIR=/tmp/wf_x bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（两读数都 0）。

---

## 一、加了哪几条（本文件哪一行 + 一句话）

三块新断言，插在 `A1503⑤` 那一块收尾（原 `:5413`）之后、`Clear filters` 那一块之前。

| 行 | 块 | 一句话 |
|---|---|---|
| `:5414-5513` | **A1525⑤** | 卡组格两枚徽章（`Faction` / `Game Mode Icon`）的**队列夹层 + 档位 + 逐格 `activeSelf` + 存在性** |
| `:5514-5553` | **A1525⑥** | 收藏窗卡组格**逐格**没有 `Highlight Rounded Square`（+ 单点「当前那一格也没有」） |
| `:5554-5592` | **A1525⑥ 的灭自证对** | 自开一扇选卡组弹窗，断**选中那一格【有】**金框（+ 色 = 金） |

`A1525⑤` 块里 9 条：
`Check(CollectionWindow.QPageOverlay, 3034)` / `QPageText == 3033` / `QPageRow == 3032` /
梯子单调 / 逐格 `Frame.RenderQueue < 两枚徽章` / 徽章队列 `== QPageOverlay`（两半各一条）/
`Faction.activeSelf == true` / `Game Mode Icon.activeSelf == true` / 该有阵营徽的格子**都真有那颗**。

## 二、每条「改回什么 ⇒ 红」

| 断言 | 改回什么 ⇒ 红 |
|---|---|
| `Frame.RenderQueue < 徽章.RenderQueue`（`:5500`） | `Shell/MenuDraw.cs` `DeckCell` 里两枚徽章那两个 `qOverlay` **改回 `q`**（= 与 `Frame` 同队列、改前那版）⇒ 106 vs 3139/3139 式，本格必红 |
| 徽章队列 `== QPageOverlay`（`:5504`/`:5505`） | 徽章队列改成别的档（如只抬到 `QPageText`）⇒ 红；**两枚同时改回 `q`** ⇒ 上一条 + 这两条一起红 |
| `QPageOverlay==3034` 等三条（`:5432-5435`） | 把 `Shell/CollectionWindow.cs:128` 那条梯子**整体抬/降一档**（Δ 一致时夹层那条抓不住）⇒ 红。⚠️ **这是变更探测器、不是原版参数**（梯子 3030..3034 是我们自建的），作用 = 挡住「两枚徽章与 `Frame` 一起漂」。若嫌吵可删，夹层那条独立成立 |
| 逐格 `activeSelf == true`（`:5506`/`:5509`） | 给徽章加 `SetActive(false)`（或改成建完就关）⇒ 红。**⚠️ 为什么单开**：`FindChild` = `GetComponentsInChildren<Transform>(true)` = **含未激活件** ⇒ 只断「找得到」测不出「关着」 |
| 存在性 `Check(miss5, 0)`（`:5510`） | `DeckCell` 里那句 `if (!string.IsNullOrEmpty(info.Faction))` 去掉（整层不画）⇒ 红 |
| `收藏窗逐格无 `Highlight Rounded Square``（`:5538`） | `Shell/CollectionWindow.cs` `BuildDeckCell` 那个实参 **`false` → `i == CollectionData.CurrentIndex()`** ⇒ 红 |
| `…当前那一格也没有`（`:5545`） | 同上；**这一条是逐格那条的判别式**（只逐格断「都没金框」时，两边一起改成恒 false 也全绿 —— 这一条配合下面那条把两个方向都钉住） |
| **灭自证对**：弹窗选中格**有**金框（`:5578`） | ① `Shell/DeckSelectionPopup.cs:731` 把 `sel` 也改成 `false`（顺手抄错树）⇒ 红；② `MenuDraw.DeckCell` 里 `if (selected)` 那一支被删 ⇒ 红；③ 弹窗那份 `sel` 判据改成 `false` ⇒ 红。**与「收藏窗没有金框」结构上不可能同时满足** |
| 金框色 `== (1,0.773,0)`（`:5584`） | 把 `Highlight_Rounded_Square` 的 tint 改掉 ⇒ 红（`CheckTint` 容差 2/255） |

## 三、对上了 / 没做到的

- ✅ **判据没重挖**，两条都照简报走；`QPageOverlay` 等四个常量**现读** `Shell/CollectionWindow.cs:128` = `3030/3031/3032/3033/3034`（不是抄的）。
- ✅ **本文件原来【没有】任何「徽章 rect」断言** —— `grep "DcFacX\|DcModeX\|DcFacY\|DcModeY"` 全仓只命中 `Shell/MenuDraw.cs`；
  本文件里 `Faction` 一次都没出现过、`Game Mode Icon` 只有 `:5327/:5341/:5348`（`A1503①` 的**存在性**）与 `:2055`（**另一棵树** = `Deck info Popup`）。
  ⇒ 简报第三项「核一遍还成不成立」= **无既有断言可核，队列改动不可能撞到位置断言**。
- ⚠️ **没做**：**没给徽章位置补 rect 断言**（简报没要求，且 `preserveAspect` 开着时实绘矩形取决于贴图比例 —— 补它得再查两张贴图的尺寸比）。判据**已经现读好了，谁要补直接用**（见下）。
- ⚠️ **「当前那一格也没有金框」那条有额度**：`FindChild(PageRoot(0),"CollectionDeck_"+CurrentIndex())` 若**不在视口内**（不建节点），走 `Debug.LogWarning`「**限额·不是通过**」而不是红 —— 逐格那条照样成立（`:5549-5552` 就地写明了）。
- ⚠️ **弹窗那条依赖「自开窗后 `TryOpen` 确实 `Build()`」**（WindowsManager 的 `AttachToAnchor` 不调 `Open()`，`CurrentState` 默认 `Closed` ⇒ `TryOpen` 走 `Open()` ⇒ 建树）。与 `A1035①`（`:2596-2632`）**逐句同形**，那一段现在是绿的 ⇒ 前提成立。
- 🔴 **一律没跑 Unity**（`CollectionScene.Run` 一条都没跑）⇒ **本批断言是不是真绿，我没验**，只验了「编得过」。

## 四、顺手发现（**只报，没改**）

1. 🟢 **两枚徽章原版就是 `preserveAspect`** —— 我们 `DeckCell` 传的第 7 个实参 `true` **对得上**（本来担心是抄错模板）。
   现读（`python -I 工具/menu_dump.py bundle_menus_assets_all -2024334647938937812 --depth 4`）：
   ```
   Faction Icon      -10.5  273.7   74.0  359.4   84.50  85.75  Image  <无图> Simple (1,1,1,1) preserveAspect
   Game Mode Icon    170.5  273.7  255.0  359.4   84.50  85.75  Image  <无图> Simple (1,1,1,1) preserveAspect
   Frame               2.0   17.0  248.0  385.0  245.99 368.01  Image  40K_bt_deck 244×366 | Simple (1,1,1,1)   ← 无 preserveAspect（我们传 false ✓）
   DificultyLevel    159.2   15.7  244.1   99.7   84.93  84.01  INACT  preserveAspect（我们传 true ✓）
   Highlight         -19.8  -10.9  270.0  416.3  289.75 427.27  INACT  Sliced 九宫73,75,73,75 (1,0.773,0,1)
   ```
   ⇒ **徽章 rect 的现成判据**（谁补位置断言直接用这一行；我们的常量是 `DcFac/…=(-10.5,273.7,84.5,85.7)` · `DcMode…=(170.5,273.68,84.5,85.75)`，
   原版两枚 y 都是 **273.7**、高都是 **85.75** ⇒ 我们的 `85.7` / `273.68` 各差 0.05 / 0.02px，无声）。
2. 🔴 **`Deck Image` 原版【没有】`preserveAspect`**（上表第 5 行）—— 而我们在 `MenuDraw.cs:3010-3017` 传的是 `keepAspect: true`
   （注释里自知「画宽 200 vs 227，差 12% · `A1507` 的尾巴」）。
   **这条现在可以精确复现**：原版就是**拉伸画在 347.70×346.35**（= `DcImgW/DcImgH`）再被 `Deck Image(mask)`（`11,26.1→239,332.1`，`RectMask2D`）裁 ⇒
   要的是「先按 `DcImgW/H` 的方框拉满、再取 uv」，**不是** `keepAspect`。账在 `A1507`，⛔ 不是本轮范围。
3. 🟡 **节点名偏离**：原版叫 **`Faction Icon`**，我们建的是 **`Faction`**（`MenuDraw.cs:3074`）。原版 `Game Mode Icon` / `DificultyLevel` 我们**照抄了**
   （`:3085` 的注释还专门写「拼错了，照抄别改」）⇒ 只有这一颗被改短。改名会牵动 `MainMenuScene.cs:3205` 与 `:3353` 的读数，**这事留给调度台**。
4. 🟡 **`MainMenuScene.cs:3354` 那条「阵型图标在左下」是**粗判**（`fx < 500 && fy > 450`）**，不是 rect 断言：原版中心 = `(31.75, 316.55)`、
   我们画在 `(31.75, 316.55)`（`-10.5+84.5/2`, `273.7+85.75/2`）⇒ 判据已经能收紧到 ±2px。⛔ 不是本文件，只报。
5. 🟡 **`Highlight Rounded Square` 的矩形仍是错的**（原版 `-19.8,-10.9→270.0,416.3`；我们 `DcHiOff=-1.4` x/y 同值）——
   `MenuDraw.cs:2938-2939` 已如实记着（「两轴都不对，留给那一笔」）。我这轮**没碰它**；新断言只断**队列/存在/颜色**，⛔ 不断它的大小，
   ⇒ **将来把矩形改对不会让本轮任何一条变红**。
6. ⚪ 本文件另有「**恒绿**」味的写法一处（**没改，只报**）：`:5305-5350`（`A1503①`）那条 `Check(miss, 0)` 的**鉴别力依赖「该有图标」的格子数 `want > 0`**，
   而 `want` 的闸是「`Deck Name` 建出来」（视口裁切）。**它有 `CheckTrue(want > 0)` 前提兜着**，不是假绿 —— 我照同一个形状写了自己的闸。

## 五、类型检查读数

```
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```
命令：`TMPDIR=/tmp/wf_x bash d:/4/Unity/工具/typecheck.sh`（一次通过，没有「错在别人文件上」的情形）。
行尾核过：`b.count(b'\n')=8681` / `b.count(b'\r\n')=0` ⇒ **没翻行尾**；`git diff --numstat` = `240 insertions / 4 deletions`
（那 **4 条删除不是我删的**：`左栏四个键` 那一段，是本会话开跑前就躺在工作区里的改动）。
