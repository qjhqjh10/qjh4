# D2 · A364（模态消息窗）+ A328（①/②a/②c）（写手 · 2026-10-12）

> 白名单内改动：新建 `Shell/PopUpGameWindow.cs`（+ `.meta`）· `Shell/WindowsManager.cs`（只加两条接线）·
> `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` · `Editor/RewardsScene.cs`（只改**理由**那一段）·
> 两份资料的行号订正。⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没越白名单。

---

## ① 结论（逐件）

| 件 | 结论 |
|---|---|
| **A364** | ✅ **做完了**。原版那扇模态消息窗 `PopUpGameWindow` **建了**（两扇 prefab 都建：`MessagePopupWindow` / `MessagePopupWindow2Buttons`，全库只有这 2 个实例）；`DeckRuntime.SaveAndSay()` 不合法那一支**改弹它**（原版 `__TrySaveDeck.c:94`），合法那一支 `HidePopUp`（原版 `:103`）；两颗钮的回调**逐句照原版那两颗 lambda**；窗开着时本窗**不吃点击**（模态）。 |
| **A328①** | ✅ **查清了 + 改了**。那两个调用点**不在**被 `TransformScalerBySmallScreenUI` 缩放的窗根下（本窗根是 `new GameObject("DeckEditor")`、无父、`DeckRuntime` **不是 `GameWindow`**；全仓该组件的**生产** `AddComponent` 只有两处，都不在本场景）⇒ 「假定 `localScale = 1`」**今天成立**。仍按 **A298 口径**把那份副本的**位置项**收到 `MenuDraw.PosInDesignSpace`（**尺寸项不动**），并配了**两态断言**（`k == 1` vs 根 ×1.2）。 |
| **A328②a** | ✅ **只改理由**（`Editor/RewardsScene.cs` 那三行）：`BuildArmyItems` 现在**三件套一起拿捏**（W4 的 C 件），原来那句「只设 `Clip`」已过期。**那条断言的做法照旧成立**（仍只数 `CampaignNode_*` 子树）。 |
| **A328②c** | ✅ 两份资料的旧行号 `CampaignTab.cs:342` → **`:358`**（**三处**：A297 报告 `:74` · 共用件报告 `:107` **与** `:118` —— 第三处是同文件里同一句的第二次出现，铁律 5 要求一起改）。 |

---

## ② 「层 × 参数」表（铁律 10② —— 动手前从**两扇 prefab** 实读）

**出处**：`d:/2/新解包资源/assets_full/bundle_generalgamewindows_assets_all/`
`python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "MessagePopupWindow" --depth 8 --md`
（`"MessagePopupWindow2Buttons"` 同）· 另**逐字段复核了原始 JSON**（`GameObject/MessagePopupWindow*.json`
+ `MonoBehaviour_3669898094100658758.json` / `MonoBehaviour_2996096449700776559.json` 窗参 +
`GameObject/Button Text*.json` 上的 TMP/ARF/Localize）。

| 层（原版节点） | 1 按钮版 `MessagePopupWindow` | 2 按钮版 `MessagePopupWindow2Buttons` | 其它真值 |
|---|---|---|---|
| 根（窗参 MB） | `type=1` · `windowsPlacement=15` · **`closeOnESC=0`** · `extraScaleSmallScreen=1.0` · `openSound`/`closeSound` 空 + `useDefaultCloseSoundIfNull=1` | **逐字段同值** | `Button`/`LiveButtons`/`Message` 三个 PPtr 各自指到下面那几颗 |
| 根矩形 | 锚 `(.5,.5)` · `sz 1919×1079` | 锚 `(0,0)-(1,1)` · `sz 0×0` | 两者都铺满屏 |
| `Menu Dark Background` | 4574.6×2572.36 · **无 sprite** · `Simple` · 色 **(0,0,0,0.772549)** · `raycastTarget=1` · 挂 `BackgroundCloseButton`（**`window` = 空引用**）+ `BackgroundOverDrawController` | **同** | 全库 82 颗 `BackgroundCloseButton` 里**只有 28 颗**非空 |
| `Window` | 中心 = 画布中心 + `apos(0,80)` ⇒ **[585,250]–[1335,670]** · **750×420** · **无组件**（没有 LG/CSF） | `apos(0,80)` ⇒ **[535,245]–[1385,675]** · **850×430** | 尺寸是**成品值** |
| `Generic Popup Background` | = `Window` 矩形 · `Image` `40k_popup` 359×336 · 九宫 **(169,160,169,160)** · `Sliced` · `ppuMul 1.0` | **同** | |
| `Mask` | [595.40,259.44]–[1325.13,660.20]（`sd(-20.268,-19.245)` + `apos(0.261993,0.178986)`）· `Image,Mask` **`showGraphic=0`** ⇒ **自己不画** | [545.40,254.44]–[1375.13,665.20] | |
| `Background fill` | = `Mask` 矩形 · `40k_popup_texture` 128×128 · `Tiled` · **`ppuMul 2.0`** ⇒ **64 一格** | **同** | |
| `MessageText` | [625,275]–[1295,545]（`sd(-80,270)`+`apos(0,50)`）· TMP **fs 40** · `auto[4,40]` · `Center/Middle` · **折行=1** · 白 | [575,273.80]–[1345,546.20] | 出厂字面量 `'Text goes here'` |
| `Buttons` | [593.05,555]–[1326.95,645] · **`VerticalLayoutGroup`** `pad 0` · `align=4` · `spacing 22.24` · `expandW=1` `ctrlW/H=0` | [572.30,560]–[1347.70,650] · **`HorizontalLayoutGroup`** `pad 0` · `align=4` · `spacing 0` · `expandW/H=1` `ctrlW/H=0` | 两处都在 `apos(0,70)`（贴面板下沿往上 70） |
| 按钮 | `Generic UI Button` **[760,562.5]–[1160,637.5]（400×75）** · `40K_button` 489×107 · 九宫 (234,46,234,46) · `Simple` · **`preserveAspect = 0`** · 色 **(0.36862749,0.89411765,0.58743727,1)** · `trans=2`（HL `40K_button_hover` / P `40K_button_pressed`） | `ButtonLeft` **[591.15,567]–[941.15,643]** · `ButtonRight` **[978.85,567]–[1328.85,643]**（各 350×76）· 同图同色 · **`preserveAspect = 1`** | 子件宽按 uGUI `childSize = sizeDelta + surplus/totalFlexible` 算（350 + 75.4/2 = 387.7 的「格」，子件自己仍 350、格里居中 ⇒ 左沿 591.15） |
| `Button Text` | 锚 stretch + `sd.x = −26` + **`AspectRatioFitter`(mode 1 宽控高, ratio 5.140573)** ⇒ 374×72.75 · TMP **fs 40** `auto[12,40]` · `Center/Midline` · **折行=0** · `apos(-0.303955,0.608994)` | 324×63.03 · TMP **fs 38** `auto[12,38]` · 同对齐/折行 · `apos(0,0)` · **多一颗 `Localize.mTerm = "Battle/Mulligan/ButtonDone"`** | 出厂字面量 **`'Continue'`**（两扇都是）—— **模板默认、运行期被 `ConfigurePopUp` 覆盖** |
| **按钮的字（真值）** | 调用方给的 `GameWindowButton.Text`。`TrySaveDeck` 那一支 = 左 **`MainMenu/General/Discard`** · 右 **`MainMenu/General/Cancel`** | | 判据 = `stringliteral.json` 地址表：`0x42BE418` / `0x42BE120`（与 `TrySaveDeck.c` 里那两个 `DAT_18…` **逐个地址对上**） |
| 两颗钮的**回调体** | 左 = `HidePopUp()` + 虚槽 `0x1b8`(= `GameWindow.Close()`) · 右 = **只** `HidePopUp()` | | `DeckEditingWindow___TrySaveDeck_b__42_1.c` / `DeckEditingWindow.__c___TrySaveDeck_b__42_0.c`（`script.json` 的 `ScriptMetadataMethod` 定名） |
| 正文（真值） | `DeckUtility.ToRawLocalizationString(err)` = 键 `MenuDeck/Error/{0}`；**查不到词条 ⇒ 兜底仍是另一个键** `MenuDeck/Error/InvalidDeck` | | 本地**没有** I2 词条表（远端 CCD）⇒ 今天画面上**显示的是键** |

**我们这一侧对上的**（`Shell/PopUpGameWindow.cs`）：
队列 **3560–3565**（`QShade` 3560 < `QShadeHit` 3561 < `QPanel` 3562 < `QFill` 3563 < `QContent` 3564 < `QText` 3565）——
取这一档的理由：**在最高的窗带之上**（排行榜 `3500–3520` 是最高的一族）、**在 `Core/Tooltip.cs` 的 `3605–3607` 之下**
（「tooltip 全壳最高」是原版兄弟序，判据 → `Core/Tooltip.cs:75-84`）⇒ 层带不重叠。
压暗层那颗**吃射线**的命中区走 `MenuDraw.Absorb`（**不是** `ShadeHit`——本窗 `BackgroundCloseButton.window` 是空引用）。

---

## ③ 改动清单（文件:行号 · 每处一句为什么）

**新建 `Unity/MyGame/Assets/CardPresentation/Shell/PopUpGameWindow.cs`（389 行）+ `.cs.meta`**（`guid 7ab8c81efe6e41a891dc963cd39006f7`，全工程 10124 个 guid 里查过不撞）
- `:81` `class PopUpGameWindow : GameWindow`；`:90` 队列档 3560–3565；`:96-160` 两扇 prefab 的**全部字面量**（每块 `PxRect` 上都写了算式）；
  `:170` `Term(key)`（**表空 ⇒ 返回键本身**）；`:180` 两颗钮的键字面量；`:219` `Create(...)`；`:242` `Configure(...)`；
  `:253` `Open()`（出声：表在远端 / 音效没做）；`:265` `Build()`；`:341` `MakeButton(...)`。
- 🔴 **两版 prefab 的分家点全在常量里**（`Win1R/Win2R` · 字号 40/38 · **`preserveAspect` 0/1** · VLG vs HLG 的成品矩形）——
  `preserveAspect` 那一档**逐扇实读**，⛔ 别统一。
- `Mask` 那颗 `Image` **不画**（`showGraphic = 0`，判据见文件头 ④）——补一块九宫格 = 把边框**错位重画一遍**。

**`Shell/WindowsManager.cs`（+60 / −0，只在 `ShowPopUp` 之后插入）**
- `:1033` `ShowMessagePopUp(...)` —— **接线**：原版那一族 `ShowPopUp` 的**真身**（宿主是 `PopUpGameWindow`）；
  「**还开着就复用同一扇**、重配 + 重开」照原版 `*(this+0x60)`。
- `:1063` `HidePopUp(bool force = false)` —— **接线**：逐句照 `WindowsManager__HidePopUp.c`；
  `TrySaveDeck` 成功那一支（`:103`）调的就是它。
- ⛔ 上面那个 `PromptPopup` 版的 `ShowPopUp` **一个字没动**（**11 个生产调用点 + 4 个自检站点**各自传的是明文）——收编是另一件（见 §六·1）。
  🔴 **2026-10-12 就地订正（铁律 5 · A455②）**：本行原来写「**8 个**既有调用点」，**那个数字是错的**；
  实测 = **11 个生产 + 4 个自检**（逐处清单 → `Shell/WindowsManager.cs:1229-1249` ·
  `资料/普查产出_1012/H5_DeckScene红与ShowPopUp收编.md` §⑦·1）。
  🔴 **2026-10-12 二次订正（A483 · 铁律 5）**：本行在 A455② 那一版里写的是「11 个生产 + **2 个**自检」，
  **「2」也是错的**（那是**上游口径**的错，⛔ 不是 A455② 自己数错的）—— 真值 = **4 处**，
  现行位置 `Editor/ShellScene.cs` **`:862` · `:903` · `:918` · `:930`**
  （⚠️ 行号**仍在漂**，判据是 `grep -c '\.ShowPopUp('` 那一句 = 4，别照抄行号）。
  **错因** = 「2 处」是 **A416 收编【前】** 的数（`git show HEAD:…/Editor/ShellScene.cs` 那一版 = `:808` · `:829`），
  A416 同一天为「两钮」「没给 `onCancel`」两档各补了一处 ⇒ 2 → 4。同口径的订正 → `Shell/WindowsManager.cs:1229-1249`。

**`Deck/DeckRuntime.cs`（+242 / −22）**
- `:2217` `SaveAndSay()`：不合法支加 `ShowInvalidDeckPopUp(err)`；合法支加 `HideDeckPopUp()`（= 原版 `:103`）。
  同时**就地订正**了原来那段「我们退一档：页脚 `_verdict` + `Say(...)`」（**那条已不成立**）。
- `:2272` `ShowInvalidDeckPopUp(err)`：`WindowsManager.EnsureHost()`（幂等；**窗建在场景根**，⛔ 不挂 `_root` 底下
  —— 否则那块 4574×2572 的压暗层会进「所有可见图都在可见区内」那条扫描）+ 两颗钮的回调
  （左 = `HideDeckPopUp()` + `BackToMenu()`、右 = `HideDeckPopUp()`，**照原版那两颗 lambda**）。
- `:2284` `HideDeckPopUp()`；`:2317` `MenuDeckErrorNumber(DeckError)`（我们的枚举 → **原版号**）；
  `:2332` `MenuDeckErrorKey(DeckError)`（= `ToRawLocalizationString`：表空 ⇒ 兜底**键**）。
- `:1999` `HandlePointer` 里那句 `if (ModalPopupOpen) return;`：**模态**——原版靠压暗层那颗 `raycastTarget=1`
  的整屏图吞射线，我们这套没有 UGUI ⇒ 等价物（形状与 `_importOpen` 那条**逐字同款**）。
- `:2791` `UiClickPx` **同一条闸**（自检走鼠标那条路，两处必须一致）。
- `BackToMenu()` 加 `LeaveCount++`（自检口：Discard 那颗钮唯一可观测的副作用 —— 批处理不切场景）。
- **订正三处过期话**（铁律 5）：「本窗没有 `PointerLayer`」—— 从 A364 起**只在弹窗弹过之前成立**
  （`EnsureHost` 会连指针层一起建），已在 `Build()` 末尾与新加的那段里写清**后果**（见 §六·2）。

**`Editor/DeckScene.cs`（+570 / −9）**
- `:246` `QuadRectPx` 的**位置项**改成 `LayoutSpace.ToPixel(MenuDraw.PosInDesignSpace(q.transform))`（A328①）。
- `:1452` **A328① 两态段**（14 条）：滚到抽屉底部取一格压在带口上的 `flt_cell`，态一量一次；
  给 `_root` 挂一颗 `menuScale = 1.2` 的 `TransformScalerBySmallScreenUI` + `Tick()` 再量一次；
  断四条边**相等**（0.05px）+ **夹具非退化**（旧式读数与设计 px 的**中心差 > 10px**）+ **还原**（根缩放回 1、开关回出厂关）。
- `:300` `DestroyPopupScaffold()` ＋ `Run()` 末尾 `:545` 那句调用 —— 🔴 **必须加的一条**：`Run()` **末尾**
  （`:544-546`）会 `Shoot` + **`SaveScene()` 重写 `DeckEditor.unity`**，而 Unity **连非激活的 GameObject 一起存**
  ⇒ A364 那条链建出来的 `Window Anchors` / `WindowsManager` / `Pointer Layer` / 两扇窗**会落进场景**
  （运行期拼的窗、贴图与材质都指不回原始资产 —— 同族事故：`BattleScene` 那条「34 个粒子的材质没贴图」）。
  已按本文件既有的 `DestroyImmediate(sgo)` 口径改成「**存场景之前先拆**」，并出声报出拆了几件。
- `:260/:283/:324` 三个量测 / 驱动小工具（`UnionQuadsPx` / `FirstQuad` / `ClickPopupButton`）。
- `:2766` **A364 段**（84 条）：文案映射 → 开窗 → 版面（量渲染矩形）→ 模态 → 复用 → Discard → 合法保存收窗 → 1 按钮版。

**`Editor/RewardsScene.cs`（只改 `:7215-7223` 那一段的**理由**）** —— 结论（只数 `CampaignNode_*` 子树）不变，
理由从「`BuildArmyItems` 只设 `Clip`」改成「它现在三件套一起拿捏 ⇒ 仍可能带着自己那一趟的值」。

**两份资料（只订正行号）**：`资料/普查产出_1010/A297_MenuWindowBase副本.md:74` ·
`共用件_A294_A292.md:107` **与 `:118`**（第三处是同文件里同一句的第二次出现）。

---

## ④ 新增 / 改动断言（**改坏了会不会红**）

### A364 段（84 条 · 宿主 `DeckScene.Run`）

| # | 断言什么 | 改坏法（怎么改会红） |
|---|---|---|
| 文案映射（6 条） | `NoWarlord→2` · `TooFew/TooMany→5` · `WrongFaction/CopyLimitExceeded/WarlordNotHero→4` | 把映射压成 0/1、或者拿**我们的枚举下标**当原版号 ⇒ 红 |
| 表空 ⇒ 印键（4 条 + 反证 2 条） | `MenuDeckErrorKey(TooFewCards) == "MenuDeck/Error/InvalidDeck"` · `Term("MenuDeck/Error/5") == 它自己` | 自己编一句文案 ⇒ 红；**反证**：往 `Terms` 塞一条假词条 ⇒ 键与文字**都要跟着变**（一个「根本没用表」的实现蒙不过去） |
| 开窗 + 窗参（9 条） | 不合法 ⇒ 弹出窗 · 名 = `MessagePopupWindow2Buttons` · `type/placement/closeOnESC/extra` · 挂 `3 - PopUp Holder` · 三处文字 = 三个键 | 删 `ShowInvalidDeckPopUp` ⇒ 第 1 条红；建 1 按钮版 / 参数抄错 ⇒ 各自红 |
| 版面（13 条） | 面板 [535,245]–[1385,675]（九块**并集**）· 压暗层 4574.6×2572.36 · 左钮中心 (766.15,605) 宽 = 76×图长宽比 · 右钮中心 (1153.85,605) · 上下沿 567/643 | 常量抄错 / 九宫格只取一块 / 把 `keepAspect` 关掉（宽会撑到 350）⇒ 红 |
| 层带（3 条） | 压暗 < 面板 < 填充 < 按钮 < 文字（**严格递增**）· 整条 ≥ 3103 | 把两层并到同一档 ⇒ 红 |
| 模态（6 条） | 压暗层命中区 = `MenuDraw.Absorb` 建的 · 档合法 · `absorbOnly` · **点它不关窗** · 开着时点 `Filters` **没人吃**且抽屉没翻 · 不合法时 ESC 不落盘 | 删 `HandlePointer`/`UiClickPx` 那句闸 ⇒ 「没人吃」红；把 `Absorb` 换成 `ShadeHit(()=>Close())` ⇒ 「点它不关窗」红 |
| 复用 / 关过不复用（4 条） | 还开着时再弹 ⇒ **同一实例**；`Cancel` 关了之后再弹 ⇒ **新实例** | `ShowMessagePopUp` 每次新建 ⇒ 第 1 条红；不判 `StillOpen`（对已关的实例再配一遍）⇒ 第 2 条红 |
| 两颗钮的接线（4 条） | 点右钮 ⇒ 窗关 + **没离场** + 盘上没变；点左钮 ⇒ 窗关 + **离场调了一次** | 两颗钮的 `onClick` 接反 / 接成同一个 ⇒ 红 |
| 合法保存收窗（3 条） | 不合法开窗 → 补回一张 → ESC ⇒ **窗被收掉** + 真的落盘 | 删成功支那句 `HideDeckPopUp()` ⇒ 第 1 条红 |
| 1 按钮版（7 条） | 只给一颗 ⇒ 名 = `MessagePopupWindow` · 钮 [760,562.5]–[1160,637.5]（**撑满 400** ⇒ 那一版 `preserveAspect=0`）· 没有第二颗 | 拿 2 按钮版顶替 ⇒ 红（那一版宽是 347.3 不是 400） |
| 收尾（3 条） | 牌还回去 ⇒ 合法 + 落盘 + **没有窗留在场上** | ——（这是给后面几节用的） |

⚠️ **A364 段顺带把 D1 那 13 条 A330 断言压过了一遍**（不改它们）：A330 里那两次「不合法 ⇒ ESC/Done」
现在**会开窗**，最后一次合法 ESC 会把它收掉 ⇒ **D1 那一段逐条照旧成立**（已按此推演，见 §五·1）。

### A328① 两态段（14 条）

| 断言什么 | 改坏法（怎么改会红） |
|---|---|
| 态一：中心与 `PxOfWorld(世界)` **逐位同值**（`k == 1`） | 这一步只是「出厂态零可观测差异」的凭据 |
| 夹具非退化：态二下旧式读数与设计 px **中心差 > 10px** | 夹具摆到画布中心附近 ⇒ 红（那时两式恒等、后面四条等于没查） |
| ★ 态二四条边 = 态一四条边（0.05px） | **把位置项换回裸 `PxOfWorld(q.transform.position)`** ⇒ 偏 `0.2 × 到画布中心的距离`（这一格 ≈160px/48px）⇒ 红 |
| 还原：根缩放回 1 · 与态一逐值一致 · 开关回出厂关 | 不还原 ⇒ 后面几节的截图/断言全歪（这条会先红） |

---

## ⑤ 没查清 / 判不了的（⛔ 不猜）

1. **`MenuDeck/Error/<n>` 与 `MainMenu/General/{Discard,Cancel}` 的【文字】**：**本地没有 I2 词条表**
   （远端 CCD）⇒ 今天画面上**显示的是键**。已在 `PopUpGameWindow.Terms` 留了空表 + 一句 `Debug.Log` 出声，
   拿到表**只往表里填、不改任何调用点**（同 `Battle/ChoosePanel.cs` 的先例）。⛔ **没编任何一句人话**。
2. **`UnknownCard` 该落哪个原版号**：原版那一路会**先**过 `ValidateDeckOwnership`（`:16-17`），
   而一个不在库里的 id 在那里**大概是**「持有数 0 ⇒ err=1」；但 `InventoryManager.GetOwnedCount` 对未知 id
   的返回值**本件没查到**（方法体是那一大坨泛型/LINQ 展开）⇒ **按原版 catch-all（4）落账，不猜 1**。
   （真正能返回它的路径今天也走不到：卡组是**从卡池建的**。）
3. **1 / 3 两个号我们【永远不回】**：`CardsNotOwned`（持有数）单机全解锁 ⇒ 恒真；`InvalidDeckBannedCards`
   （事件禁卡表）是 LiveOps 远端下发、本地没有。⇒ 这不是「漏做」，是**没有那个数据源**
   （同 `资料/预组卡组_原版规格.md` §五之五）。
4. **`PopUpGameWindow.ConfigurePopUp` 那个 3 参数重载的【方法体】在 `decomp_full` 里没有产物**
   （只有 4 参数那个）⇒ 「一颗钮都不给时原版画成什么」**读不到**。我们的 1 按钮版按**只给一颗**建
   （生产上 `ShowPopUp` 至少给主按钮）⇒ 这一档**没有原版对照**。
5. **ESC 那一处偏离**（如实标在 A364 段里）：原版那一刻 ESC 落在 `closeOnESC = 0` 的弹窗上 =
   「什么都不做」且**到不了** `DeckEditingWindow.ESCPressed`；我们这边 `DeckRuntime` **不走**
   `WindowsManager.Update` 那条 ESC 路由（它不是 `GameWindow`）⇒ ESC 仍会走 `SaveAndSay()`，
   结果是**把同一扇窗又配了一遍**（同一实例、看不出来）。⛔ 没照原版「把 ESC 挡在窗外」：
   那样这扇窗在本模型里就**只能靠点钮**才消得掉（自检与真玩家都只剩一条路）。
6. **断言没实跑**：本件按红线**没跑 Unity** ⇒ §四那张「会红/不会红」的表是**推演**（依据＝代码路径 +
   `menu_dump` 实读值 + uGUI 源码的布局算式），**不是跑出来的结论**。宿主 = `Editor/DeckScene.cs` ⇒ `DeckScene.Run`。

---

## ⑥ 顺手发现（⛔ 一个都没在本件里改，只报）

1. 🔴🔴 **`Shell/WindowsManager.cs` 的 `ShowPopUp`（`PromptPopup` 版）那句理由已过期**（`:999-1001`）：
   它写着「原版那两个 `popupWindowOneButton/TwoButtons` **仍然本地没有**（正本 §五 第 2 条）」——
   **本件现核推翻了这个前提**（那两扇 prefab 就在 `bundle_generalgamewindows_assets_all`，全库就这 2 个实例，
   判据见 §二）。⇒ 原版的 `WindowsManager.ShowPopUp` **指的就是本件新建的那扇**；那 **11 个生产调用点 + 4 个自检站点**
   各自传**明文**（不是术语键 —— 🔴 **2026-10-12 就地订正（铁律 5 · A455②）**：原文两处都写「8 个」，**是错的**；
   🔴 **2026-10-12 二次订正（A483）**：「自检站点」那一半也从 **2** 改成 **4** —— 判据与错因见 §三 那条同口径的更正块，
   ⛔ 别在这儿抄第二份）。
   **要做**（铁律 11）：把 `ShowPopUp` 收编到 `PopUpGameWindow`（调用点**不用改传键** —— 它们传的是明文、
   `Term()` 对明文恒等；`ShowPopUp` 转调 `ShowMessagePopUp`；`PromptPopup` 还给「`GenericPromptWindow` 自己的用途」）。
   ✅ **2026-10-12（A416）已做完**：清单与判据 → `资料/普查产出_1012/H17_ShowPopUp收编.md`（§④ 逐处列了「11 生产 + 自检」）。
   本件**只接线、没动它**（改既有调用点会越出白名单给的边界）。
2. 🔴 **本场景从 A364 起会多出一台 `PointerLayer`**（`EnsureHost` 第一句就是它，而且**不会自己消失**）——
   那几颗挂了 `WindowButton` 的件（换图 5 + 色偏 3）**可能被派发两次悬停**：`Enter()` 有 `Hovered` 守卫
   ⇒ 同向幂等，但两者**命中判据不同**（本类是 `_btns` 区域表、它是 quad 矩形）⇒ 边界上**理论上可能来回抖**（视觉）。
   **点击不会重复**（本类 0 处给它们设过 `onClick` ⇒ `Click()` 空转）；**弹窗开着时也不会**（`PointerLayer` 按
   渲染队列取最高那件 ⇒ 只会命中弹窗自己）。已就地订正 `Build()` 末尾那段「本窗没有指针层」并写清后果
   ⇒ **要不要进一步收口（例如让本类自己那套派发权威化）请调度台排**。
3. 🔴🔴 **D1 那一件（A363）在 `:3123-3124` 会红 —— 与 A364 无关，是「在飞改动里的既有红」**：
   「拖出侧栏 = 删一张」那一节把牌删到 **29 张**（`:3114-3115`），紧接着 `EscPressed()`（`:3122`）想让
   「Done 把删除落盘」，可 **29/30 的经典牌组 `Validate()` 恒为 `TooFewCards`** ⇒ A330/WB1 那道闸**不放行**
   ⇒ `DeckDirty` **清不掉**、盘上也不会变成 29 张 ⇒ `:3123`（`!DeckDirty`）与 `:3124`（盘上 = n2−1）**两条红**。
   **判据**：`DeckCount = Deck.CardIds.Count`（`Deck/DeckEditorState.cs:190`）· 闸在 `SaveAndSay()` 里（A330）。
   **两条候选修法**（本件**没动**，⛔ 属于 D1/A363 那一件）：① 断言改成「**不完整 ⇒ Done 也不落盘**」（= A330 的反向）；
   ② 夹具改成一个**仍然合法**的突变（例如「删一张 + 立刻补一张别的」）再验「Done 才落盘」。
   ⚠️ 本件改动对这一条的**红的形状**没有影响（改前改后都不落盘），只多了「那一下还会弹出一扇模态窗」——
   而那扇窗已由 `DestroyPopupScaffold()` 在存场景之前拆掉。
4. ⚠️ **`DeckScene.Run` 是**在方法末尾**（`:511-512`）才 `Shoot` + `SaveScene()`** —— ⛔ 不是开头
   （本件一开始按行号误判成「开头」，当场订正）：所以**运行期拼出来的物件会被写进 `DeckEditor.unity`**，
   A364 那一段建的三样 + 两扇窗必须在存场景前拆掉 ⇒ 这就是新增 `DestroyPopupScaffold()` 的理由（见 §三）。
   🔑 **可复核**：`Run()` 起于 `:367`、`SaveScene()` 在 `:512`，而各 `Test*` 段都在 `:1200+` ⇒ 保存**在断言之后**。
5. ⚠️ **`Editor/ShellScene.cs` 那条小屏缩放器注释过期**（`:864-866`）：写着「`extraScaleSmallScreen` 在我们这套里
   **没有消费者**……小屏缩放器**没实现**」—— A165（2026-10-06）就做完了（`WindowsManager.ApplySmallScreenScale`）。
   不在白名单 ⇒ 没动。
   ✅ **2026-10-12 补记（A430）**：这一条**已就地订正**（`Editor/ShellScene.cs` 行号现为 `:944-955`）——
   结论（本窗那棵树缩放恒为 1）照旧成立，只是理由改成「`extra = 1.0` = 不覆盖语义 + 本窗根上没有烤 `menuScale`」。
   上面那句「没动」只描述**当时**。
6. ⚠️ **`A330` 段里那两条「不合法 ⇒ Done/ESC 不落盘」现在会**顺带**开出模态窗**（第 3 扇那一次由 A330 收尾
   的合法 ESC 收掉）⇒ **断言照旧全绿**（A364 段开头就断「起手没有模态窗」，把这一条钉住）；
   那一段结束时场上会多一台 `WindowsManager` + 指针层（同 §六·2），由 `DestroyPopupScaffold()` 一并拆掉。
7. ⚠️ **另一处「跑完会留东西」的入口**：`:3122` 那次 ESC（= 上面 1 那条红的那一下）**也会开出一扇窗**，
   而那之后到 `Run()` 结束**再没有任何 `UiClickPx`**（本件 grep 过）⇒ 它不会被后面的断言翻动，
   只在存场景之前被拆掉。⚠️ **将来谁在后面加一节用 `UiClickPx` 的，会撞上那扇窗的模态闸**（记在这里）。
8. ⚠️ **`CheckHoverSwap` 只审「有 `_hoverTex` 且 `target != null`」的钮** ⇒ 压暗层那颗吸收层（`target == null`）
   天然不计入 ⇒ A364 段那条「悬停换图」审的是**两颗真钮**（与本仓既有口径一致，记一笔免得下次误以为漏审）。

---

## ⑦ 跑过的检查

- **秒级类型检查**（`TMPDIR=/tmp/wf_d2 bash d:/4/Unity/工具/typecheck.sh`）：**跑了 7 遍**，最终
  ```
  --- 运行时程序集 ---
  运行时错误数: 0
  --- 编辑器程序集 ---
  编辑器错误数: 0
  ```
  其中**第 5 遍**报 `Battle/CombatAutoZoom.cs(381,31): CS0102` + `Editor/BattleScene.cs(10040,20): CS0234`
  —— **两条都在别人的文件上**（`BattleScene.cs` 是写手 G1 在写的那个）⇒ 按铁律 13·3 第 2 条**不当成自己的错**、
  **没去改**；隔 100 秒重跑**自行消失**（= 那位写手的半成品）。
- **行尾二进制复核**（`io.open(...,'rb')`，⛔ 不是文本模式的假读数）：
  `Deck/DeckRuntime.cs` 4336/4336（**纯 CRLF**，改前改后一致）· `Editor/DeckScene.cs` 3505/3505（**纯 CRLF**）·
  `Shell/WindowsManager.cs` 1081/1081（纯 LF）· `Editor/RewardsScene.cs` 7568/7568（纯 LF）·
  新文件与 `.meta` 都是 LF（与 `Shell/` 其它文件一致）。⇒ **一行都没被翻**。
- **`git diff --numstat` 对眼**（最后一次读数）：`DeckRuntime 242/22` · `DeckScene 590/9` · `RewardsScene 474/11` ·
  `WindowsManager 60/0` —— 删除数远小于各文件行数 ⇒ **无整篇重写**（数字大头是**本仓此前批次留下的在飞改动**）；
  本件自己那部分的量级：`DeckRuntime` 净 **+220/−0** 上下 · `DeckScene` 净 **+360/−0** 上下 ·
  `WindowsManager` **+60/−0** · 新文件 389 行。
  新文件 `Shell/PopUpGameWindow.cs` + `.meta` 是 **untracked**（`git status` 已确认）。
- ⛔ **没跑 Unity / 自检**（红线）· ⛔ **没动 git 写命令** · ⛔ 没碰白名单外的 `.cs`（`Battle/*` 一个字没动）。
- 📌 **覆盖面（交调度台定复跑时机）**：本件动了 `Deck/DeckRuntime.cs`（`DeckScene.Run` 的宿主）·
  `Editor/DeckScene.cs`（该条自检本身）· **`Shell/WindowsManager.cs`（共用件）** · 新建 `Shell/*.cs` · `Editor/RewardsScene.cs`。
  ⇒ 按铁律 12 判据②（动共用件要 grep 谁在用）：`WindowsManager` 被全壳引用，但**本件只加了两个新方法**
  （`ShowPopUp` / `OpenWindow` / `Close` 那一族**一行未动**）⇒ 建议 **`DeckScene.Run`（必跑）+ `ShellScene.Run` + `RewardsScene.Run`**；
  若想一次收干净就 `bash d:/4/Unity/工具/_run_8_checks.sh`（11 条，串行）。
