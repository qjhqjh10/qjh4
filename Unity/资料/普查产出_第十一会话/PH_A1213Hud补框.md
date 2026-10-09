# P-H · `A1213①` —— `BattleDriver.Hud` 那 17 颗 HUD 文字补框 + 接四格

> 执行代理 **P-H** · 2026-10-19 · 白名单 = 只有 `Battle/BattleDriver.cs`（**别的文件一个都没碰**）。
> 红线：⛔ 没跑 Unity / `_run_8_checks.sh` · ⛔ 没动 git · ⛔ 没碰两张正本 · `d:/2/**` 只读（只读、没写）。
> ✅ 跑了**秒级类型检查**两次（改完立刻一次、最后又一次）—— 读数在 §六。
> 📌 判据来源 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §2·A / §4（`R6`）**+ 本件自己现读的原版解包资源**
> （`bundle_scenes_scenes_battlearena1` 的 `RectTransform/` + `MonoBehaviour/`；13 场同构，挑一场核，**逐颗现读**）。
> ⚠️ **`R6` 的行号与本报告的行号都是快照**；本报告的行号一律是 **2026-10-19 改完之后的现读**。

---

## §一 一句话结论

**17 颗里做了 16 颗、故意没做 1 颗**（`HandLabel` / 原版 `CardsInHandText` —— 它的四格在**「缩放假」**里，
照同一套口径会把字号**静默压到 3 px**，而换算要的单位桥本仓没有 ⇒ 按红线「不许自己发明口径」**只报不动**，见 §四·1）。

- **落地形状**：`Hud()` 后面加了 6 个**带默认值**的形参（`boxWpx/boxHpx/autoMinPx/autoMaxPx/autoBasePx/wrapMode`），
  **只在这 6 个都在场时**才调 `Label.SetAutoFitBox` + `SetWrappingMode` ⇒
  **另外 5 个调用点（`TurnLabel` / `TurnClock` / `HintLabel` / `ResultLabel` / `HandLabel`）逐位不变**。
- 🔴 **摆位一个字都没动**：没改任何 `x01/y01`、没动任何 `anchor`、没新增/改动 `SetVAlign` 与 `Align*On`。
  判据是**结构性的**（不是「我量了看起来没变」）——见 §三。
- ⚠️ **会变的是字号**（这正是接 autosize 的目的）：`SetAutoFitBox` 开的是真自适应，
  TMP 会在 `[min,max]` 里收敛。**收敛结果本波量不到**（不跑 Unity）⇒ 我按我们的字体度量给了一张**估算**表（§二·B），
  真正的读数交给后面的断言波 + 真 Play。

---

## §二 逐颗表

### A. 原版真值（**本件现读**，不是转述）

读法（可复现）：
`python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 --rt 2684 --depth 9 --no-sprite`
（`2684` = `Canvas/BackCanvas`）+ 自写只读脚本（**在 `D:/tmp/`，没进工程**）。
**框 = 那颗 `RectTransform` 解算到画布的屏幕矩形（1920×1080 px）**；
**四格 = 那颗 TMP 自己的 `m_fontSizeMin` / `m_fontSizeMax` / `m_fontSizeBase` / `m_TextWrappingMode` 原文**
（与 `R6` §2·A 逐值吻合 ✓ —— 我逐颗复读过一遍）。

| # | 我们这处（`BattleDriver.cs`） | 原版节点（`battlearena1 ▸ Canvas/BackCanvas/Safe area BackCanvas/` 之下） | `RectTransform` pid | 框（px） | min | max | base | 折行 |
|---|---|---|---|---|---|---|---|---|
| 1 | `:11745` `EnemyPlateText` | `LeftArea/EnemyInfo/EnemyName/EnemyNameText` | 3568 | 290.94 × 36.84 | 2 | 35 | 36 | 0 |
| 2 | `:11752` `PlayerPlateText` | `LeftArea/PlayerInfo/PlayerName/PlayerNameText` | 3016 | 199.38 × 35.93 | 2 | 35 | 36 | 0 |
| 3 | `:11774` `MatchSkullsScore` | `LeftArea/PlayerInfo/Milestones/MatchSkulls Score` | 3467 | 94.46 × 47.31 | 18 | 35 | 36 | 1 |
| 4 | `:11858` `PlayerFaithText` | `…/Energy And turn holder/PlayerMana/FaithHolder/FaithText` | 3246 | 57.43 × 94.19 | 8 | 40 | 36 | 1 |
| 5 | `:11861` `EnemyFaithText` | `…/Energy And turn holder/EnemyMana/FaithHolder/FaithText` | 3090 | 57.43 × 88.40 | 8 | 40 | 36 | 1 |
| 6 | `:11877` `PlayerSpiritStoneText` | `…/PlayerMana/SpiritStoneHolder/SpiritStoneText` | 2975 | 52.80 × 63.00 | 8 | 40 | 36 | 1 |
| 7 | `:11880` `EnemySpiritStoneText` | `…/EnemyMana/SpiritStoneHolder/SpiritStoneText` | 2906 | 52.80 × 136.40 | 8 | 40 | 36 | 1 |
| 8 | `:11908` `EnergyLabel` | `…/PlayerMana/ManaHolder/ManaText` | 2994 | 73.90 × 82.48 | 8 | 40 | 36 | 1 |
| 9 | `:11916` `FoeEnergyLabel` | `…/EnemyMana/ManaHolder/ManaText` | 2780 | 77.34 × 80.95 | 8 | 40 | 36 | 1 |
| 10 | `:11963` `EndTurnButton` | `…/Energy And turn holder/Clock/TurnBtn/TurnText` | 3420 | 119.08 × 80.43 | 8 | 31 | 36 | 1 |
| 11 | `:12071` `MyPileLabel` | `RightArea/PlayerDeck/Player Deck Size Container/Player Deck Size Tex` | 2604 | 219.42 × 44.93 | 10 | 42 | **49.63** | 1 |
| 12 | `:12074` `FoePileLabel` | `RightArea/EnemyDeck/Player Deck Size Container/Player Deck Size Tex` | 3406 | 193.20 × 39.56 | 10 | 42 | **49.63** | 1 |
| 13 | `:12265` `TitleText_Me` | `LeftArea/PlayerInfo/PlayerName/TitleBackground/EnemyTitle` | 2893 | 187.40 × 36.84 | 2 | 35 | 36 | 0 |
| 14 | `:12268` `TitleText_Foe` | `LeftArea/EnemyInfo/EnemyName/TitleBackground/EnemyTitle` | 3134 | 187.40 × 36.84 | 2 | 35 | 36 | 0 |
| 15 | `:12495` `QPText_Me` | `…/PlayerMana/QuestPointsHolder/QPText` | 3483 | 48.17 × 45.26 | 15.79 | 40.5 | 36 | 1 |
| 16 | `:12498` `QPText_Foe` | `…/EnemyMana/QuestPointsHolder/QPText` | 2607 | 48.17 × 45.26 | 15.79 | 40.5 | 36 | 1 |
| 17 | `:11936` **`HandLabel`** | `scenes_battlearena1 ▸ CardsInHandText` | 3400 | （2.34 × 0.64 局域单位） | 0.5 | 3.0 | 36 | 1 |

**框不是「直读 `m_SizeDelta`」得到的** —— 这 17 颗里**只有 2 颗两个轴都固定**
（`MatchSkulls Score` `anchorMin = anchorMax = (0.5,0.5)`、`CardsInHandText` 同），
`FaithText` 两颗是「**x 固定 / y 拉伸**」（`anchorMin.x = anchorMax.x = 0.5`），
**其余 13 颗全是双轴拉伸锚点** ⇒ `m_SizeDelta` 在那 13 颗上是**内缩量**、当框寸用是错的。
解算口径（uGUI 语义，逐级）：`rect.size(局域) = sizeDelta + anchorDiff ⊙ 父局域尺寸` ·
`父局域 = 父屏幕 ÷ lossyScale(父)` · **`屏幕尺寸 = 局域尺寸 × lossyScale(自己)`** ·
`枢轴(屏幕) = 锚参考点(父屏幕) + anchoredPosition × lossyScale(父)`。

**两处必须解释的「看着像抄错」**（都核过、都是原版长的样子）：

1. **敌我两颗的高度不一样**（#4/#5 `94.19 vs 88.40`、#6/#7 `63.00 vs 136.40`、#8/#9 `82.48 vs 80.95`）
   —— 不是抄错：敌侧那两颗的父 holder 带 `m_LocalScale = (1,-1,1)`（**镜像**），而它们的 `anchorMin.y = 0`、
   `anchorMax.y = 0.519/0.855`，`sizeDelta.y` 那个**负内缩**被镜像翻到另一侧 ⇒ 解算出来的高不同。
   `ManaText` 那一对则是 holder 自己尺寸不同：`RectTransform_2790`(我 `PlayerMana`) `m_SizeDelta = (-1.8120, 1.9700)`
   vs `RectTransform_3479`(敌 `EnemyMana`) `(0.2520, -0.8300)`。
2. **#11/#12 的框高不是序列化字段**：那颗 TMP 的 `m_SizeDelta.y = **0**`，高度由父容器
   `Player Deck Size Container` 的 **`AspectRatioFitter`** 撑出来 ——
   原版实读 `MonoBehaviour_4275.json` / `_5165.json`：`m_AspectMode = 1`（`WidthControlsHeight`）、
   `m_AspectRatio = 4.0346479415893555`。
   ⇒ 容器 238.50×59.11（我）/ 210.00×52.05（敌），TMP 取锚区比 (0.92, 0.76) ⇒ **219.42×44.93 / 193.20×39.56**。
   ⛔ **拿 `sizeDelta.y = 0` 当框高会把字号压到 `m_fontSizeMin`（10）** —— 这一格是本波最容易踩的坑。
   （复现：`python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 --rt 3292 --depth 4 --no-sprite`，
   表尾会打 `⚙ARF 宽控高(4.03465) ⇒ 1844.00×457.04` —— 那个 `1844` 是**根节点算错**的产物，
   要**从 `PlayerDeck`(3292) 起根**才拿得到 238.50/210.00。）

### B. 我们这一侧：**摆位变没变 / 字号会去哪（估算，不是实测）**

**「渲染位置逐位相同」这一格怎么证** —— 见 §三，结论是**结构性的、不需要量**。
下表这一列给的是**机制**，不是实测读数：

| # | 落地后的框（世界单位，`Px(px)` = px/108） | 锚点 | **块锚点位置** | 接上自适应之后**会变的** |
|---|---|---|---|---|
| 1 | 2.694 × 0.341 | (0.5,0.5) | 中心不动 | 字号 |
| 2 | 1.846 × 0.333 | (0.5,0.5) | 中心不动 | 字号 |
| 3 | 0.875 × 0.438 | **(0,0.5)** | **左缘不动** | 字号 + 右缘 |
| 4–9 | 见上表 | (0.5,0.5) | 中心不动 | 字号 |
| 10 | 1.103 × 0.745 | (0.5,0.5) | 中心不动 | 字号（**命中区**另见 §三·3） |
| 11–12 | 见上表 | (0.5,0.5) | 中心不动 | 字号 |
| 13–16 | 见上表 | (0.5,0.5) | 中心不动 | 字号 |

**字号会去哪（⚠️ 估算，依据写在下面，⛔ 不是实测）**：
判据两条 —— ① TMP 的自适应是**竖向驱动**的（`TextMeshPro.GenerateTextMesh()` 的
`#region Text Auto-Sizing (Text greater than vertical bounds)` 与
`#region Check Auto-Sizing (Upper Font Size Bounds)`「increase font size to fill text container」）；
② 我们字体的**行盒/em = 0.1437 / 0.0948 = 1.5157**（`TmpFont.MeasureGlyph` 的 doc：`fontSize=100` 时
`textBounds.size.y = 14.37`、字形 9.48）⇒ `行盒(px) ≈ 1.5157 × em(px)`。
⇒ 收敛点 ≈ `min(maxPx, 框高 ÷ 1.5157)`。

| # | 现在的 em（px） | 估算收敛后 | 差 |
|---|---|---|---|
| 1 `EnemyPlateText`（scale 3 ⇒ 27.6） | 27.6 | ≈ 24.3 | −12% |
| 2 `PlayerPlateText` | 27.6 | ≈ 23.7 | −14% |
| 3 `MatchSkullsScore` | 27.6 | ≈ 31.2 | +13% |
| 4/5 `FaithText` 两颗（scale 4 ⇒ 36.85） | 36.85 | 40（= max） | +9% |
| 6/7 `SpiritStoneText` 两颗 | 36.85 | 40 | +9% |
| 8/9 `ManaText` 两颗 | 36.85 | 40 | +9% |
| 10 `EndTurnButton`（scale 3） | 27.6 | 31（= max） | +12% |
| 11 `MyPileLabel`（scale 3） | 27.6 | ≈ 29.6 | +7% |
| 12 `FoePileLabel`（scale 3） | 27.6 | ≈ 26.1 | −5% |
| 13/14 `TitleText` 两颗（`SetGlyphHeight(30.55/108)`） | 30.55 | ≈ 24.3 | −20% |
| 15/16 `QPText` 两颗（scale 4） | 36.85 | ≈ 29.9 | −19% |

⚠️ 三条会让估算偏掉、**只有真 Play / 断言量得到**：① 我没量过我们字体**实际**的 `ascender−descender`（用的是 doc 里那条
`textBounds` 读数）；② 折行开着的几颗（15/16 的 `0/3` 塞在 48.17 px 宽的框里、8/9 的 `10/10` 塞在 74 px 里）
**可能会折成两行**，而两行的行盒高会让收敛点再降一档；③ `Label.SetVAlign`（A712）的**字墨校正**与字号成正比
⇒ 字号一变，那个位移也跟着变一点点（同一档位内部的自洽，不算偏离）。

---

## §三 🔴 摆位不变是怎么保证的（本件最大的风险点）

### 1. 结构性保证：**框从不进入最终摆位那条算式**

加框这件事**只写一个地方**：TMP 子节点的 `sizeDelta`
（`TmpFont.SetWrapWidthRect` = `sizeDelta = (w, 0)` · `Label.SetAutoFitBox` 再补 `sizeDelta.y`）。
而**最终摆位**由 `Label.RefreshBounds()` 一锤定音（`Battle/Label.cs`）：

```text
_tmp.rectTransform.localPosition =
    new Vector3(-anchor.x * _tmpW - b.min.x, -anchor.y * _tmpH - b.min.y + _vOffset, 0f);
```

—— `_tmpW/_tmpH` 是 `textBounds` 的**尺寸**、`b.min` 是它的**左下角**，**框的 pivot / `anchoredPosition` /
`sizeDelta` 一个都不在式子里**。`b.min` 那一项正好**抵消掉**「框把文字挪到框内某处」这件事
（框宽了 `b.min.x` 就更负，减掉之后回到同一处）。
⇒ **文字块的锚点逐位停在 `Label` 节点的 `localPosition` 上，与框多大、轴心在哪、pivot 取多少无关。**

这同时回答了简报里那句「**框的锚点/轴心要选得让文字渲染位置逐位不变**」：
**本仓这条路根本不需要选轴心** —— 等效的「轴心」由 `anchor` 决定，而 `anchor` 我们一个字没动。
本仓已有的先例 `MenuDraw.Text` 那条 `Local(parent, 矩形)` = 「矩形中心 − 父件位置」也是同一个约定
（矩形中心落在调用方要的那个点上）；`Label` 这里是**自动**做到的。

⇒ 所以本件**没有**「某一颗做不到『加框而位置不变』」的情况；**16 颗都做到了**，
而第 17 颗（`HandLabel`）停手的原因**不是摆位**、是四格的**单位**（§四·1）。

### 2. 我**实际**动过的、可能影响摆位的东西 —— 逐条核过，**一条都没动**

| 会牵动摆位的东西 | 本件做了什么 |
|---|---|
| `x01/y01`（→ `LayoutSpace.ToWorld` → `localPosition`） | ⛔ **一个字没改**（16 处全是原地加实参） |
| `anchor` | ⛔ **一个字没改** |
| `SetVAlign`（A712 垂直档，会调 `RefreshBounds`） | ⛔ **没新增、没改档**；原有的 3 处（`_skullScore` Midline / 两张 `TitleText` Midline / 两张 `QPText` Capline）**位置与实参都没动** |
| `AlignLeftOn` / `AlignRightOn` / `SetAlignLeft` | ⛔ 这 17 颗**一处都没用**（没有「先对齐、后加框」的次序问题） |
| `ReanchorHud()`（换分辨率重贴） | ✅ 它只写 `localPosition`（`:13246`），**不碰 `sizeDelta`** ⇒ 框在分辨率切换后活着 |
| `_hudSpots` / `_hudLabels` | ✅ 记账口径没变（新参数不影响这两个 list）；`HandLabel` 仍在里面 |

### 3. R6 点名的另两处随之而来的影响 —— 都核过

- **命中区**：`Label.Contains` 读 `WorldW/WorldH`（= 字块尺寸）⇒ 字变大命中区就跟着变大。
  **但这 17 颗里只有 `EndTurnButton` 用 `Contains`**，而那一处（`BattleDriver.cs:6857`）**先判 `_endTurnBg != null` 就 return**
  ⇒ `_endTurnLabel.Contains` 只是**取不到按钮图时的兜底**（正常有美术的构建根本走不到）。
  ⇒ **本件对命中区的实际影响 = 0**。其余 16 颗没有任何 `Contains` 调用点。
- **`SetVAlign`**：`VOffsetWorldNow()` 里两处都不依赖框 —— `Middle` / `Capline` / `Midline` 都**不吃框高**
  （只有 `Bottom`/`Top` 吃 `_boxHFromAutoFit`）⇒ 这 17 颗的档位**一个都不受影响**，
  也不会因为本件新写进去的 `sizeDelta.y` 而改道（`_boxHFromAutoFit` 只被 `Bottom/Top` 读）。
  ⚠️ 唯一间接影响：`OurInkCenterWorld()` 与 `OrigInkCenterPx()` 两项**都与字号成正比**⇒ 字号变了位移跟着变，
  但那是「同一套算式在新字号下重算」，**不是本件引入的偏离**。

---

## §四 没做的逐条

### 1. `HandLabel`（原版 `scenes_battlearena1 ▸ CardsInHandText`）—— **只报不动**（唯一的 1 颗）

- **判据齐**：`RectTransform/RectTransform_3400.json` = `m_SizeDelta (2.34, 0.64)`、`anchorMin = anchorMax = (0.5,0.5)`；
  TMP（`MonoBehaviour_3712.json`）= `m_fontSize 3.0` · `m_fontSizeMin 0.5` · `m_fontSizeMax 3.0` ·
  `m_fontSizeBase 36.0` · `m_TextWrappingMode 1` · `m_text 'Cards left: XX'`。
  （另：GO 出厂 `m_IsActive = 0`；拖到运行时 dump `:344` 也是 `False/False`；祖先链是**纯 `Transform`**、
  算不出绝对位置 —— 这条 `BattleDriver.cs:11889` 早就记着，**不是新发现**。）
- ⛔ **不能照其余 16 颗那套口径直接传**：这四个数在**「缩放假」**里 ——
  父链 `HandArea`（`Transform/Transform_1401.json`）`m_LocalScale = 108` → `CardsInHandText m_LocalScale 0.925926`
  ⇒ **1 局域单位 = 100 px**。而 `SetAutoFitBox` 的 px 口径是**我们这一侧**的
  （`Label.FontSizeToPx(F) = F × 0.0948 × 108`，即 **`maxPx` 直接就是渲染出来的 em px**）
  ⇒ 照抄 `maxPx = 3.0` 会把我们这颗的字**压到 3 px**（≈ 看不见）。**这一条是死路，不是「先凑合」能解决的。**
- ⛔ **换算要的那条单位桥本仓没有，而且两条候选读法互相打架**：
  - 读法 ①`1 fontSize = 1 局域单位` ⇒ em = `3.0 × 100 = 300 px`，**塞不进 64 px 的框**（不自洽）；
  - 读法 ②`按我们字体那条 0.0948 的比值` ⇒ em = `3.0 × 0.0948 × 100 = 28.4 px` ✅（与 234×64 px 的框自洽）
    —— 但**其余 16 颗要的是读法 ① 才自洽**（例如 `QPText` 的 `40.5` 若按 ② 就只剩 3.8 px）⇒ **两条不能同时成立**。
  - 我判这是「**同一批 TMP 的 `m_fontSize` 口径本身就不是一套**」（原版这个包里的字体资产/烘焙档不同），
    **不是我能裁的** ⇒ 交调度台。
- ⚠️ **为什么不「只补框不补四格」**：框的唯一目的就是给自适应用（见 `Hud` 的 doc 与简报）；
  只补 `SetWrapWidth` 不接自适应 = 加了一个**没有任何可观测效果**的东西（`HandLabel` 文案短、永不折行），
  **不是复刻**。⇒ 整颗挂起。
- 📌 **顺带一条旁证（⛔ 不当判据用）**：若按 **234 × 64 px** 读那一颗（= 2.34/0.64 × 100），
  它与读法 ② 的 28.4 px 自洽；而**我们现有的 `HandPlatePx = 65.7f`（`:1381`，那张 `Bg (1)` 底板）正是用同一条链
  （108 × 0.925926 × 0.009 = 0.9）算出来的** ⇒ 两条独立结论指向「这条链在**底板**那一级是对的」，
  但**证不了它在这一级（TMP 自身）也对**。如实登记。

### 2. 另外 4 颗 `Hud` 建的（`TurnLabel` / `TurnClock` / `HintLabel` / `ResultLabel`）—— **按普查不是缺口，故意没动**

`R6` §2·A #1/#13/#16/#17：`TurnLabel` / `HintLabel` / `ResultLabel` **原版根本没有此件**
（13 场 GO 名扫描 + 运行时 dump 双查）；`TurnClock` 的四格**原版读不到**
（`ClockManager` 34 个方法无一含 `set_text`）⇒ ⛔ 给它们补框 = **主动制造偏离**。
它们走 `Hud()` 的新默认值（`boxWpx = 0`）⇒ **行为逐位不变**。

### 3. 读不到 / 判不了的

- 本件 17 颗里**没有**「四格读不到」的（`HandLabel` 的四格读到了，卡的是**单位**，见上）。
- ⚠️ **没量**：接上自适应之后**实际**收敛到几号字（不跑 Unity 量不到）—— 见 §二·B 那张估算表的三条前提。

---

## §五 该断什么（给下一波的「断言宿主」清单）

> 宿主 = `Editor/BattleScene.cs`（**不在本件白名单**）⇒ 本件只列，⛔ 没动。
> 通用读法：从 `BattleDriver` 的 HUD 根按**节点名**（`EnemyPlateText` 那一列）找 `Label`，读
> `Label.WorldW/WorldH`、`AutoSizing`、`FontSizeMin/FontSizeMax/FontSizeBase`、`WrappingMode`、
> 以及 TMP 子节点的 `rectTransform.sizeDelta`。

**① 每颗一条「框真的写进去了」**（16 条）：断 TMP 子节点 `sizeDelta ≈ (Px(框宽), Px(框高))`，
容差 1e-4（**别精确比浮点** —— 本仓已有先例）。
例：`EnemyPlateText` ⇒ `sizeDelta ≈ (290.94/108, 36.84/108) = (2.69389, 0.34111)`；
`MyPileLabel` ⇒ `(219.42/108, 44.93/108) = (2.03167, 0.41602)`。
**改坏法**：把 `boxHpx` 传成 `0`（或漏掉那 6 个实参）= 这一条立刻红。

**② 每颗一条「四格真的接上了」**（16 条，两态）：

- `AutoSizing == true`；
- `Label.FontSizeToPx(FontSizeMax)` ≈ **`maxPx` 原文**、`FontSizeToPx(FontSizeMin)` ≈ `minPx` 原文
  （容差用「≤ 0.05 fontSize 单位」那一档 —— TMP 自适应按 1/20 取整）；
- `FontSizeBase`（反射读的**真字段**）≈ `basePx` 原文 ——
  ⚠️ **这一格特别值得断**：`Label` 的 doc 明写 `basePx == nomPx` 时 `m_fontSizeBase` 会**停在 TMP 的序列化默认 36**
  （A536 那个洞）；本件的 `TitleText` 两颗、`EndTurnButton` 都落在「`basePx`(36) > 标称」这一档，
  正好是**有鉴别力**的一格。
  **改坏法**：把 `autoBasePx` 传成 `0` ⇒ `base` 变成调用方那一档（≠ 36）⇒ 红。

**③ 折行档**（4 条 · 只给 `wrap == 0` 的那 4 颗）：`WrappingMode == 0`。
`EnemyPlateText` / `PlayerPlateText` / `TitleText_Me` / `TitleText_Foe`。
**改坏法**：删掉那句 `l.SetWrappingMode(wrapMode)` ⇒ 这 4 颗读成 `1`（`SetAutoFitBox` 无条件开的）⇒ 红。

**④ 摆位不变**（**2 条**，一条正向一条「灭自证」）：

- 正向：16 颗的 `Label.transform.localPosition` ≈ `LayoutSpace.ToWorld(x01, y01)`（**逐位**，容差 1e-6）
  - **块锚点**满足 `TmpChildPos.x + anchor.x*WorldW + textBounds.min.x ≈ 0`（y 那一项多一个 `VOffsetWorld`）。
- 🔴 **灭自证（结构上不可能同时满足的那一对）**：**同一颗调两次** ——
  第一次不给框、第二次给框（或反过来），断 `Label.transform.localPosition` **两次逐位相同**。
  只断「新写法对」是不够的：若被测实现与它的检测器用同一个口，**把两边一起改回去**依然全绿。
  ⇒ 这一对的意义是：**只要有人把框的 pivot/anchoredPosition 也写进 `RefreshBounds` 的算式**，它就红。

**⑤ 反向（挡「顺手给不该给的也补框」）**：5 颗**必须没有框** ——
`TurnLabel` / `TurnClock` / `HintLabel` / `ResultLabel` / **`HandLabel`**：
断 `AutoSizing == false` **且** TMP 子节点的 `sizeDelta.x` 仍等于「建标签时的默认值」。
**改坏法**：给 `Hud` 那 6 个形参填上非零默认值 ⇒ 红。

**⑥（建议）一条「HUD 之外没被误伤」**：`Hud` 的形参默认值改动只影响本文件内的调用点；
断「`Battle/` 下其它建 `Label` 的路（`UnitChatPanel` / `SettingsPanel` …）的 `AutoSizing` 读数与本件之前一致」——
本件没碰它们，这条是**成本极低的保险**。

---

## §六 验证

| 项 | 读数 |
|---|---|
| **秒级类型检查**（`TMPDIR=/tmp/wf_ph bash d:/4/Unity/工具/typecheck.sh`） | **运行时错误数: 0 · 编辑器错误数: 0** —— 跑了 **2 次**（改完 16 处后一次、补完 `HandLabel` 注释 + `Hud` doc 后一次），两次都是 0/0（**没有别人的半成品干扰**） |
| **`git diff --numstat`**（`Battle/BattleDriver.cs`） | **172 / 17** —— 172 行新增几乎全是**注释**（16 处调用点各带判据注 + `Hud` 那份 doc + `HandLabel` 那一段）；17 行删除 = 被替换的旧行 |
| **行尾**（`io.open(p,'rb')`） | **CRLF 14431 / LF 14431** ✅ **没有翻**（改前 14276/14276；增量 = 155 行） |
| 改动文件数 | **1**（`Battle/BattleDriver.cs`）—— 白名单之外**零**改动 |
| ⛔ 没跑的 | Unity 批处理 / `_run_8_checks.sh` / `RuleEngineTest.Run` / `BattleScene.Run` …（按简报：本波不跑，由调度台在同步点按覆盖面决定） |

**自检该跑哪几条（建议，⚠️ 由调度台定）**：本件只动 `Battle/BattleDriver.cs` 一个宿主 ⇒
按铁律 12 的判据「**只动一个宿主就只跑那一条**」= **`BattleScene.Run`**。
⚠️ 但它**不会**覆盖本件新增的任何一格（§五那些断言**还没写**）⇒ 它只能证明「没把对战弄红」。
真正的验收在**断言宿主那一波** + 真 Play（字号是视觉量，断言管行为、管不了「字号对不对」）。

---

## §七 没查清 / 停手的部分

1. 🔴 **`HandLabel` 的四格单位** —— 停手（§四·1）。**还差什么**：一条能判「原版
   `bundle_scenes_scenes_battlearena1` 里 `CardsInHandText` 那颗 TMP 的 `m_fontSize` 一个单位
   = 多少**我们这一侧的 px**」的桥。候选判据（⛔ 我没查）：① 同包里**另一个落在缩放假**、
   但**有独立像素旁证**的 TMP（例如手牌卡上的文字，卡宽 165 px 是已知的）⇒ 用比值反推；
   ② 把 `CardsInHandText` 与其父 `HandArea` 的**字体资产 pid** 与其余 16 颗对一下
   （若同资产、则两条读法必有一错，能一次性裁掉一半）。
2. 🔴 **`menu_dump.py` 的父链解算在「父件自带 `m_LocalScale ≠ 1`」时偏高 `1/localScale`**
   —— **顺手发现，见 §八·1**，⛔ 我没改工具（不在白名单）。
   本件在 #1/#2（两张名牌）上**没有采信工具那个数**、改用 uGUI 语义自算 ⇒
   若调度台裁「改回工具的口径」，那两格要改成 **352.47×50.61** / **238.02×47.77**（一行的事，见 §八·1）。
3. ⚠️ **没量**收敛后的实际字号 / 有没有哪几颗会折成两行（§二·B 那三条前提）。
4. ⚠️ **`R6` 简报里那句「§2·A 表里逐颗抄好了原版 `m_SizeDelta`」不成立** ——
   `R6` 那张表**没有 `m_SizeDelta` 列**，只在表前正文里给了 4 个例子（`MatchSkulls Score` / `TurnText` /
   `ManaText` / `SpiritStoneText`）。本件那 17 个框**全部是现读解算出来的**（§二·A）。

---

## §八 🔑 顺手发现（⛔ 一个都没改，交调度台分流）

### 1. 🔴🔴 `工具/menu_dump.py` / `menu_rect.py` 的父链解算在「父件自己带 `m_LocalScale ≠ 1`」时偏高 `1/localScale`

**症状**：`parent_rect_of()` 把「父的 `rect_of` 返回值」直接当成「**父在屏幕上的框**」传给下一级
（`menu_rect.py` 的 `rect_of(rt, parent_rect, scale)` 收到的是这个）。而 `rect_of` 的返回值按它**自己的 docstring**
是「**布局框 = anchorDiff ⊙ 父屏幕 + sizeDelta ⊙ scale**」—— 也就是**还没乘本件自己 `m_LocalScale`** 的那一档
（doc 写得很清楚：「画出来的是 **布局框 × 这一件自己的 `m_LocalScale`**」）。
⇒ 父件 `localScale ≠ 1` 时，子件拿到的 `pw` 不是父的屏幕宽，子件的矩形就**偏高 `1/localScale`**。

**实据（本件现场，两把尺并排）**：`battlearena1` 的 `LeftArea/EnemyInfo/EnemyName`（`RectTransform_3083.json`）
`m_LocalScale = 0.8` ⇒

| 节点 | `menu_dump.py --rt 2684 --depth 9` | uGUI 语义自算（本件 §二·A） | 倍率 |
|---|---|---|---|
| `EnemyNameText` | 352.47 × 50.61 | **290.94 × 36.84** | 恰好 0.8 |
| `PlayerNameText` | 238.02 × 47.77 | **199.38 × 35.93** | 恰好 0.8 |

**同一棵树里「父 `localScale == 1`」的 13 颗两把尺逐位相同**（`MatchSkulls Score` 94.46×47.31 ·
`TurnText` 119.08×80.43 · `QPText` 48.17×45.26 · `ManaText` 73.90×82.48 · `FaithText` 57.43×94.19 …
**且 `QPText` 的绝对矩形与 `资料/战斗规格/战斗重建_0827/…` 里手算的那份逐位吻合**、`TurnText` 的 y 区间
`[415.33,495.76]` 与权威表 `[415.3,495.8]` 吻合）⇒ **不是我的脚本算错，是工具的父链那一跳漏了一次换算**。
⚠️ **两张 `TitleText` 之所以两把尺也吻合**（187.40×36.84），是因为那条链上的缩放**正好相乘为 1**
（`TitleBackground` 1.25 × `EnemyTitle` 0.8）—— **巧合，不是反证**。

**为什么值得单独记**：`menu_dump.py` 是本仓的**官方尺子**（`CLAUDE.md` 直接点名），
⇒ 凡「父链上有 `m_LocalScale ≠ 1`」的节点，**所有从它抄下来的矩形/常量都偏 `1/localScale`**。
本件**已经撞到两处真后果**（都在 `BattleDriver.cs`，**行号 = 本件改完之后的现读**；**⛔ 我没动**）：

- `:11739` `_enemyPlate = HudImage(…, 126.3f / 108f, …)` —— `126.3` 是 `NameBackground` 的**局域**高，
  真屏幕高 = `126.3 × 0.8 = 101.04`（**我们比原版大 ≈ 25%**）；
- `:11728` 那段注释自己写着「`NameBackground` 435.7×126.3、PA=1 → **实绘 382.3×126.3**」
  —— 同样漏了那个 ×0.8（382.3×101.0）。
⚠️ **这一条我只报到「证据」为止**：⛔ 没改工具、⛔ 没改 `_enemyPlate`（**不在本件范围**，且要改得先裁口径）。

### 2. `R6` §2·A 的 `#11 HandLabel` 那一行的四格**是缩放假里的值**，与其余 16 颗**不同量纲**

`R6` 把它与其余 16 颗并列成同一张表（`min 0.5 / max 3.0 / base 36 / 折行 1`），
但**它的 `0.5~3.0` 与 `QPText` 的 `15.79~40.5` 不是同一个单位**（父链 `HandArea` 带 `m_LocalScale = 108`）。
⇒ 建议在 `R6` / 正本那一行加一句量纲警告（⛔ 正本不在我白名单，没动）。

### 3. 原版 `EverguildTextController` 上另有一对 `minFontSize`/`maxFontSize`，与 TMP 的 `m_fontSizeMin/Max` **不是同一组数**

顺手扫了 `battlearena1` 全包的 `MonoBehaviour`：带 `minFontSize`/`maxFontSize` 字段的那些
（`EverguildTextController`）取值分成几族 —— `(0.0, 35.0)` · `(4.0, 31.0)` · `(10.0, 46.0)` · `(12.0, 33.0)` ·
`(0.025, 0.34)` · `(0.0, 0.4)` …，**与同一节点上 TMP 自己的 `m_fontSizeMin/Max` 对不齐**
（例：`TurnText` 的 TMP 是 `auto[8~31]`，而包里有 `(4.0, 31.0)`；`CardsInHandText` 的 TMP 是 `0.5/3.0`、
它的 controller 是 `0.5/4.0`）。
**本件照 `R6` 与全仓口径取 TMP 自身那三个字段**（`Label.SetAutoFitBox` 的 doc：「传原版那两个字段的原文」）。
⚠️ **没查清**：那份 controller 到底在运行时**覆写** TMP 的 min/max 还是**只是另一套用途**（小屏档？）——
可能是**另一笔账**（⛔ 我没往下查，交调度台）。

### 4. `EndTurnButton` 的 `Contains` 只是兜底（顺带核清 R6 那条「命中区」担忧）

`BattleDriver.cs:6857`：`if (_endTurnBg != null) return _endTurnBg.Contains(world);`
⇒ 有按钮图时**根本读不到** `_endTurnLabel.Contains`。⇒ `R6` 点名的「给框会牵动命中区」
在这 17 颗上**实际影响 = 0**（见 §三·3）。
