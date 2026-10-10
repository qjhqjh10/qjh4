# F4 · `BattleScene` 称号那条红（改成窗口形态）+ HUD 16 颗的断言覆盖

> 执行代理 **F4** · 白名单 = `Editor/BattleScene.cs` + `Battle/BattleDriver.cs`（**只动了前一个**）。
> 红线：⛔ 没跑 Unity / `_run_8_checks.sh` · ⛔ 没动 git · ⛔ 没碰两张正本 · `d:/2/**` 只读、一个字节没写。
> ✅ 秒级类型检查跑了 **3 次**（每次改完立刻一次），三次都是 **0/0**（读数见 §五）。
> 📌 判据来源 = `D1_九条红诊断.md`（#1 + 根因族 ① 子机制 ② + X1）· `PH_A1213Hud补框.md` §二·A/§四/§五
> **+ 本件自己现读的原版解包资源与 `menu_dump.py` 现跑的表**（⚠️ 工具 `81941b5` 那个「父的屏幕框」缺陷**已在 HEAD 修好**，本件跑的是修好之后那一版）。

---

## §一 一句话结论

**那条红是 (α) 断言错**：旧形态拿「TMP 的**收敛字号**」比「原版 prefab 里的**序列化** `m_fontSize` 30.55」，
而这一颗原版**本来就开着自适应**、我们这一侧又**在文本为空的那一刻**去量它
⇒ 量到的是「空文本被夹到窗口上限 35」的产物。**改成窗口 + 关系式**（断 `FontSizeMin/Max/Base` 三个**真字段** + `min ≤ 现 ≤ max`），
并**顺手把 `A1213①` 新接上自适应的那 16 颗 HUD 文字补上断言覆盖**（那一族此前**一条都没有**）。

- 改动文件 = **1 个**（`Editor/BattleScene.cs`，**+254 / −4**）；`Battle/BattleDriver.cs` **一个字节没动**（`14431/14431` 未变）。
- 新增断言 = **175 条**（16 颗 × 10 + 反向 12 + 灭自证 3）。
- ✅ **没为变绿动过自适应**（那四颗原版本来就 `m_enableAutoSizing = 1`）；动的只有**断言形态**。

---

## §二 那条红的改法（改前 → 改后 · 为什么旧形态是 (α)）

### 改前（`Editor/BattleScene.cs` 旧 `:11725-11728`）

```csharp
// 字号与位置（**别拿常量自证**：这里比的是 TMP 渲出来的实际字号 `FontPxNow`）
Check(Mathf.Abs(drv.TitleFontPxNow(true) - BattleDriver.TitleFontPx) < 0.6f
      && Mathf.Abs(drv.TitleFontPxNow(false) - BattleDriver.TitleFontPx) < 0.6f,
      $"称号字号实测 … ≈ 原版 m_fontSize {BattleDriver.TitleFontPx}");
```

**判档 (α) 的三条理由**（都在 §四 里逐位核过）：

1. **前提过期**：这条断言成立的前提是「这两颗字的字号是**定死**的」。原版那一颗**本来就开着自适应**
   （`m_enableAutoSizing = 1`、`auto[2~35] base36`，本件现读，见 §四）⇒ `_tmp.fontSize` 是
   「**当前文本 + 当前框**」的函数；`30.55` 只是 prefab 里的**序列化**字段。
2. 🔴 **它量的那一态根本不是「称号显示着」**：上一句（旧 `:11722`）刚 `SetTitle(null, null)` 把称号清掉，
   所以**文本是空的**。空文本 ⇒ `TextMeshPro.cs:2149` 把它夹成 `Clamp(base 36, min 2, max 35) = 35`，
   而 `:4164` 的 `m_characterCount == 0` 让「按竖界缩字」那一支**根本不跑** ⇒ **35.00 = 窗口上限**，
   和「称号多大」无关（X1 的完整核算 → §四）。
3. **改法不是「把 30.55 改成 35」**：那只是把 35 写死成第二个字面量，换个文案/字体照样错（D1 明写）。

### 改后（现读行号 → 见 §五 的 diff）

```csharp
drv.SetTitle("测试称号", "测试称号");      // 🔴 量字号要**在称号真的显示着**的时候量
CheckHudBox("TitleText_Me",  "…/PlayerInfo/PlayerName/TitleBackground/EnemyTitle",
            187.40f, 36.84f, 2f, 35f, 36f, 0);
CheckHudBox("TitleText_Foe", "…/EnemyInfo/EnemyName/TitleBackground/EnemyTitle",
            187.40f, 36.84f, 2f, 35f, 36f, 0);
drv.SetTitle(null, null);                  // 复位（后面的截图要的是原版实况那副样子）
```

`CheckHudBox`（新助手，声明在 `Run()` 顶层块、紧跟 `Check`）一次给一颗字断 **9 条**：

| 组 | 断什么 | 期望值从哪来 |
|---|---|---|
| 前提 | `Label` 与 TMP 后端都在 | ——（**当场红**，⛔ 不静默跳过） |
| ① | TMP 子节点 `sizeDelta ≈ (bw/108, bh/108)` | 原版那颗 `RectTransform` 的**屏幕矩形** |
| ② | `AutoSizing == true` | 原版 `m_enableAutoSizing = 1` |
| ② | `FontSizeToPx(FontSizeMin/Max/Base)` ≈ `mn/mx/bs`（容差 0.05） | 原版那三个字段**原文** |
| ③ | `WrappingMode == wrap` | 原版 `m_TextWrappingMode` 原文 |
| ④ | 文字块按 `anchor` 摆回**节点原点**（残差 < 1e-4/轴） | 结构性恒等式（见下） |
| ⑤ | `mn ≤ FontPxNow ≤ mx`（**关系式**，⛔ 不写字面量） | ——|

**为什么 ④ 是「结构性恒等式」而不是自证**：`Label.RefreshBounds()` 的摆位式是
`localPosition = (-anchor.x·W − b.min.x, -anchor.y·H − b.min.y + vOffset)`
⇒ 我断的 `cp.x + b.min.x + anchor.x·|size.x| ≈ 0` 与它**恒等**，而**框的 pivot / `anchoredPosition` / `sizeDelta` 一个都不在式子里**。
它是「**加框不挪字**」这件事的可断言形式，且**有鉴别力**：谁把框的参数加进那条算式，它当场红。
（⚠️ 断 `Label.transform.localPosition` **抓不到**——那个位置从头到尾没人写过、恒不变；会变的是 **TMP 子节点**的。）

**为什么 ⑤ 只能断关系**：开了自适应之后收敛值是「当前文本 + 当前框」的函数（空文本时还会被夹到上限）
⇒ 任何「等于某个数」的写法都是**拿实现证明实现**。窗口那三个字段才是**不受框/收敛影响**的量
（`SetAutoFitBox` 写的是 `cur × 原版字段 / nomPx`，`nomPx = FontSizeToPx(cur)` ⇒
`FontSizeToPx(写进去的那个)` **恒等于原版字段原文**，逐位、且与 `cur` 走哪条 `Set*` 路无关）。

---

## §三 🔑 HUD 16 颗的断言覆盖表

**表里的「框」全部是 `menu_dump.py` 现跑出来的屏幕矩形**（`--rt 2684 --depth 9 --no-sprite`），
**四格**是同一行里 TMP 自己的 `m_fontSizeMin/Max/Base` / `m_TextWrappingMode` 原文 —— 本件**逐颗复读过一遍**。
⛔ 期望值一律写原版字面量，**没读** `BattleDriver.Hud` 那一份注释表（读它 = 自证）。

| # | 我们这颗 | 原版节点 | 框（px） | min | max | base | 折行 | 补了哪几类 | 改坏法（**红了先看这一列**） |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `EnemyPlateText` | `…/EnemyInfo/EnemyName/EnemyNameText` | 290.94×36.84 | 2 | 35 | 36 | 0 | ①②③④⑤ | `Hud` 那 6 个实参传 `0` ⇒ ①红；删 `SetWrappingMode` ⇒ ③红（这颗原版是 0） |
| 2 | `PlayerPlateText` | `…/PlayerInfo/PlayerName/PlayerNameText` | 199.38×35.93 | 2 | 35 | 36 | 0 | 同上 | 同上 |
| 3 | `MatchSkullsScore` | `…/Milestones/MatchSkulls Score` | 94.46×47.31 | 18 | 35 | 36 | 1 | 同上 | 锚是 `(0,0.5)` ⇒ ④ 只吃 x 那一项，改 `Hud` 的 anchor 会红 |
| 4 | `PlayerFaithText` | `…/PlayerMana/FaithHolder/FaithText` | 57.43×94.19 | 8 | 40 | 36 | 1 | 同上 | 敌我两核对调（94.19 ↔ 88.40）⇒ ①红 |
| 5 | `EnemyFaithText` | `…/EnemyMana/FaithHolder/FaithText` | 57.43×88.40 | 8 | 40 | 36 | 1 | 同上 | 同上 |
| 6 | `PlayerSpiritStoneText` | `…/PlayerMana/SpiritStoneHolder/SpiritStoneText` | 52.80×63.00 | 8 | 40 | 36 | 1 | 同上 | 敌我两核对调（63.00 ↔ 136.40）⇒ ①红 |
| 7 | `EnemySpiritStoneText` | `…/EnemyMana/SpiritStoneHolder/SpiritStoneText` | 52.80×136.40 | 8 | 40 | 36 | 1 | 同上 | 同上 |
| 8 | `EnergyLabel` | `…/PlayerMana/ManaHolder/ManaText` | 73.90×82.48 | 8 | 40 | 36 | 1 | 同上 | 与 #9 对调 ⇒ ①红 |
| 9 | `FoeEnergyLabel` | `…/EnemyMana/ManaHolder/ManaText` | 77.34×80.95 | 8 | 40 | 36 | 1 | 同上 | 同上 |
| 10 | `EndTurnButton` | `…/Clock/TurnBtn/TurnText` | 119.08×80.43 | 8 | **31** | 36 | 1 | 同上 | 上限抄成 35 ⇒ ②红（原版这颗是 31，**≠ 它的字号 31**） |
| 11 | `MyPileLabel` | `…/PlayerDeck/…/Player Deck Size Tex` | 219.42×44.93 | 10 | 42 | **49.63** | 1 | 同上 | 🔴 **框高拿 `m_SizeDelta.y`（=0）当框寸** ⇒ 字号被压到 min ⇒ ⑤红（那两颗的高来自 `AspectRatioFitter`） |
| 12 | `FoePileLabel` | `…/EnemyDeck/…/Player Deck Size Tex` | 193.20×39.56 | 10 | 42 | **49.63** | 1 | 同上 | 同上 |
| 13 | `QPText_Me` | `…/PlayerMana/QuestPointsHolder/QPText` | 48.17×45.26 | **15.79** | **40.5** | 36 | 1 | 同上 | 下界/上界抄成整数 ⇒ ②红（原版这两个**不是整数**） |
| 14 | `QPText_Foe` | `…/EnemyMana/QuestPointsHolder/QPText` | 48.17×45.26 | 15.79 | 40.5 | 36 | 1 | 同上 | 同上 |
| 15 | `TitleText_Me` | `…/PlayerInfo/PlayerName/TitleBackground/EnemyTitle` | 187.40×36.84 | 2 | 35 | 36 | 0 | ①②③④⑤（**在上一个现场断**） | 旧写法（比 30.55）⇒ 当场红，这正是本件要修的那条 |
| 16 | `TitleText_Foe` | `…/EnemyInfo/EnemyName/TitleBackground/EnemyTitle` | 187.40×36.84 | 2 | 35 | 36 | 0 | 同上 | 同上 |

**另外两组（PH §五 的 ⑤⑥）**：

| 组 | 断什么 | 期望值从哪来 | 改坏法 |
|---|---|---|---|
| ⑤ 反向（5 颗） | `TurnLabel` / `TurnClock` / `HintLabel` / `ResultLabel` / `HandLabel` **必须没有框**：`!AutoSizing` ∧ `FontSizeMin==0 ∧ FontSizeMax==0`（TMP 序列化默认）∧ `WrappingMode==0` ∧ `sizeDelta.x == 新建一条 Label 的读数` | 原版判据 = `PH` §四（三颗**原版没有此件** · `TurnClock` 的四格**读不到** · `HandLabel` 的四格在**「缩放假」**里） | 🔑 **给 `Hud` 的 6 个形参填非零默认值** ⇒ 5 条一起红 |
| ⑥ 保险 | `Label.Create` 那条路（`Hud` 之外）建出来的字：`!AutoSizing ∧ min/max == 0` | 同上（「不给框」那一态） | `A1213①` 只动 `Hud` 的形参 ⇒ 这条守「没碰共用件」 |
| ④-2 灭自证 | **同一颗** Label：**不给框** / **给框** 两次调，两次都满足 ④，且两次残差**逐位相同**（< 1e-6） | —— | 谁把框的 pivot / `anchoredPosition` / `sizeDelta` 加进 `RefreshBounds` 的摆位式 ⇒ 红（⛔ 只断「新写法对」是不够的：**两边一起改回去**依然全绿） |

**⚠️ 三处如实登记的「不需要做的」**：`HandLabel` **整颗挂起**（`A1239`，单位桥缺失）⇒ 只断了「没有框」、**没补框也没补四格**；
`TurnLabel`/`TurnClock`/`HintLabel`/`ResultLabel` 同理（原版没有/读不到 ⇒ 补框 = 主动制造偏离）。

---

## §四 X1 的核算结果（`36.84` 的框高渲在 `35 px` 是怎么来的）

### ① 实参现读（`Battle/BattleDriver.cs`，本件现核）

| 项 | 值 | 出处 |
|---|---|---|
| 框高实参 | **36.84**（画布 px；=`0.3411` 世界单位 = `36.84/108`） | `:12267` / `:12270` 那两处 `Hud(…, 187.40f, 36.84f, 2f, 35f, 36f, 0)` |
| 字号路 | 🔴 **`SetGlyphHeight(TitleFontPx / 108f)`** —— **GlyphHeight 路**，⛔ **不是** `CapHeight` 路 | `:12271-12272` |
| 原版那颗的四格 | `m_fontSizeMin/Max/Base = 2 / 35 / 36`、`m_TextWrappingMode = 0` | 本件现跑 `menu_dump.py`（`EnemyTitle` 行：`基准=36.0 auto[2.0~35.0] … 折行=0`） |

### ② `35.00` 的来源 —— **文本是空的**（逐位吻合，不是「竖界没生效」）

`SetAutoFitBox` 写进去的是 `cur × 原版字段 / nomPx`：

| 量 | 算式 | 值 |
|---|---|---|
| `cur`（调用 `Hud()` 那一刻的标称） | `FontSizeForCapHeight(0.07 × 3)` | 2.6958 |
| `nomPx` | `FontSizeToPx(cur)` | 27.60 px（⚠️ 与 PH §二·B 的「现在的 em = 27.6」**逐位吻合**） |
| `m_fontSizeMax` | `2.6958 × 35/27.60` | **3.4185** |
| `m_fontSizeMin` | `2.6958 × 2/27.60` | 0.1953 |
| `m_fontSizeBase` | `2.6958 × 36/27.60` | 3.5162 |

**渲染那一步**（判据 = 包源码 `Runtime/TMP/TextMeshPro.cs`）：

1. `ForceMeshUpdate()` → `OnPreRenderObject()`（`:349`）——
   `:2149` `if (m_enableAutoSizing) m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`
   ⇒ `Clamp(3.5162, 0.1953, 3.4185)` = **3.4185**；
2. 进 `while (m_IsAutoSizePointSizeSet == false) GenerateTextMesh()` —— 而 `:4158` 先置
   `m_IsAutoSizePointSizeSet = true`，紧接着 `:4164` `if (m_characterCount == 0 …)` → `ClearMesh(true); … return;`
   ⇒ **空文本当场早退**，「按竖界缩字」那一支（`:3053` 的 `if (m_enableAutoSizing)` 底下 `:3074` 的 `#region Text Auto-Sizing (Text greater than vertical bounds)`）**一次都没进**；
3. 于是 `m_fontSize` 停在 **3.4185** ⇒ `FontPxNow = 3.4185 × 10.2384 =` **`35.00`** ✅ **与红的那一行逐位吻合**。

**为什么那一刻文本是空的**：旧 `Editor/BattleScene.cs:11722` 的 `drv.SetTitle(null, null)` 刚把称号清掉；
而单机恒无玩家资料 ⇒ 这一格**在真实对局里也永远是空的**（`SetTitle` 只在自检里被调过）。

⇒ **结论：这不是「竖界没生效」，是「没有文本可界」**。与「另外四条（`Timer` / `Victories` / 两张 SM 卡）都是竖界缩字」并不矛盾 —— 那四条**读的时候都有文本**。

### ③ 框对不对 —— **没查出缺陷**（所以 `BattleDriver.cs` 一个字节没动）

- `187.40 × 36.84` 就是**官方尺子**（`menu_dump.py`，HEAD 已修好「父的屏幕框」那一跳）现跑出来的
  `EnemyTitle` **屏幕矩形**（`x 123.9→311.3 / y 1019.7→1056.6`），⛔ 不是直读 `m_SizeDelta`（那颗是**拉伸锚**）。
  同一张表里邻居 `QPText 48.17×45.26` / `TurnText 119.08×80.43` 与 0827 那份手算表**逐位吻合** ⇒ 尺子可信。
- 用**我们这套字体度量**算它该收敛到哪：行盒 `= 0.1437 × fontSize`（`TmpFont.MeasureGlyph` 的 doc：`fontSize=100` 时 `textBounds.size.y = 14.37`）
  ⇒ 上限 `3.4185` 的行盒 = `0.4912` 世界 = **53.05 px > 36.84 px** ⇒ **只要文本非空**就会被缩到 `0.3411/0.1437 ≈ 2.374 字号单位` ≈ **24.3 px**。
- 原版那一侧的**同一条无量纲比值**：`em(屏) = 框高(屏) ÷ 行盒比`。原版框高（屏）也是 36.84 ⇒
  按 D1 §证据 A 推出的**原版行盒 ≈ 1.47 em** ⇒ 原版渲染 ≈ **25.06 px**；按它 prefab 里序列化的
  `m_fontSize = 30.55`（**局域/点**单位）× 那一级的 `lossyScale 0.8` ⇒ ≈ **24.4 px**。三条独立读数落在 **24.3 ~ 25.1 px**。
  ⇒ **我们的框换算是对的**，剩的 ~3% 差是 **D1 #2/#3 记的那条系统性字体差异**（我们行盒 1.5157 em vs 原版 ≤1.47 em），
  ⛔ **不是框的问题**，也**不在本件范围**。

### ④ 顺带核清的两件事（都属 X1 问的）

- **字号路 = `SetGlyphHeight`（GlyphHeight 路）**，⛔ 不是 `CapHeight` 路 ⇒ 与 `A1235`（`SetCapHeight` 路 + 自适应）**不同族**。
- **`A1235` 不波及这 16 颗**（本件现核）：`BattleDriver.cs` 里 `SetCapHeight` / `SetScriptHeight` 全文件**只有一处**
  （`:12630` 加时 splash 那颗，**不在 16 颗里**）；而其余 14 颗走的是「没设过 `_glyphHeight` ⇒ 用 `scale` 推的 `CapWorld`」那条**默认路**，
  它的 `NominalPx()` 已被 2026-10-05 那次修（`Label.cs:919-927`：**从 `cur` 反算**）统一到 px 口径 ⇒
  `FontSizeToPx(写进 min/max/base 的那个数)` **恒等于原版字段原文**（与走哪条 `Set*` 路无关，见 §二 末段那个恒等式）
  ⇒ 四条格**吃不到 1.14 倍差**。⚠️ 「别处还有没有踩到」仍归 `A1235` 那笔账，本件不动。

---

## §五 验证

| 项 | 读数 |
|---|---|
| **秒级类型检查**（`TMPDIR=/tmp/wf_f4 bash 工具/typecheck.sh`） | **运行时错误数 0 · 编辑器错误数 0** —— 跑了 **3 次**（第一版助手 → 修 `drv` 作用域后 → 最后两处文案订正后），三次都是 0/0 |
| **`git diff --numstat`** | `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` = **254 / 4**（254 行新增里约 150 行是注释/判据/doc，其余是 16 处 `CheckHudBox` 调用 + 反向组 + 灭自证探针） |
| **`Battle/BattleDriver.cs`** | `git diff --numstat` **没有它这一行** = **零改动**；行尾 `CRLF 14431 / LF 14431`（与改前逐位相同） |
| **行尾**（`io.open(p,'rb')` 数出来的） | `BattleScene.cs` **CRLF 20730 / LF 20730** ✅ **没翻**（进本件前 20480/20480 ⇒ 增量 = 250 行 × 1，两边同步） |
| 改动文件数 | **1**（`Editor/BattleScene.cs`）—— 白名单之外**零**改动 |
| ⛔ 没跑的 | Unity 批处理 / `_run_8_checks.sh` / `BattleScene.Run`（按简报：本波不跑，由调度台在同步点按覆盖面决定） |

**本件只动一个自检宿主 ⇒ 按铁律 12 的判据，覆盖面 = `BattleScene.Run` 这一条。**

---

## §六 没查清 / 停手的部分

1. ⚠️ **原版那颗标题的渲染值没有实测**（关服 + 本件不跑 Unity）⇒ §四·③ 里「原版 ≈ 24.4 ~ 25.1 px」是**两条独立换算**给的区间，
   **不是实测读数**。要坐实得真 Play 或一条只读探针（跑不起 Unity 就到此为止）。
   其中「原版行盒 ≤ 1.47 em」是 **D1 的推断**（我没有回原版字体资产亲读 `Pango`/`Pragati` 的 `faceInfo`）。
2. ⚠️ **`BattleDriver.TitleFontPx`（30.55）的语义现在只对了一半**：它作为 `SetGlyphHeight` 的**标称**起点是**对的**，
   但它的 doc（`BattleDriver.cs:13276-13277`）还写着「称号那行字的**字号**」—— 那句话现在会让下一个会话
   以为它就是渲染值。🔴 **我按白名单的红线没有动它**（简报：只有查实「框高是错的」才动 `BattleDriver.cs`，
   而 §四 的结论是**框没错**）⇒ **留给调度台裁**：要不要把那半句改成「**标称**字号（原版序列化 `m_fontSize`）」。
3. ⚠️ **`drv.TitleFontPxNow(bool)` 现在没有调用点了**（旧断言是唯一一处）。它是 `public` 诊断口、
   ⛔ 删它要动 `BattleDriver.cs`（不在本件范围）⇒ **如实登记**，请调度台裁。
4. ⚠️ **没实测**：这 16 颗接上自适应之后**实际**收敛到几号字（不跑 Unity 量不到）——
   `CheckHudBox` 的 ⑤ 只断「落在窗口内」，这正是它**故意**的形态（收敛值不是判据）。
5. ⚠️ **反向组那 5 颗的 `sizeDelta.x` 对照值**用的是「同一建造路径新建一条 Label 的读数」，
   不是某个原版字面量（那个默认值来自 `TMP_Settings.defaultTextMeshProTextContainerSize`，
   见 `TMP_Text.cs:5980` —— ⛔ 我不去猜它是 `20` 还是 `100`）⇒ 它断的是「**与同路新建的一条逐位相同**」。
   若这一条**真的红了**，先怀疑「那 5 颗的 TMP `Awake` 没跑过 / TMP_Settings 载入时机不同」，别当实现缺陷。
6. ⚠️ **没查**：`TurnLabel` / `HintLabel` / `ResultLabel` 在别处有没有被 `SetWrapWidth`（本件 `grep` 过 `BattleDriver.cs`：
   `SetWrapWidth` / `SetWrapping` 的**写入点只有 `Hud()` 里那一句**；假定它们不在别的文件里被单独摸过）。

**停止点**：没有卡住、没有发明口径；未跑 Unity、未动 git、未碰正本、`d:/2/**` 只读。

---

## §七 🔑 顺手发现（⛔ 一个都没改，交调度台分流）

1. 🔴 **`Hud()` 的 doc 里那句「本表 = 本件唯一的数字正本」正在被三处引用**：
   `Battle/BattleDriver.cs:13211` 写着「逐颗判据（本表 = 本件唯一的数字正本，⛔ 别在调用点另抄一份）」。
   本件按「期望值必须来自**原版**、不许读实现」的口径，在 `Editor/BattleScene.cs` **另抄了一份**（16 行表）。
   ⚠️ 这不是违反它 —— **断言的期望值本来就不能从被测件里读**，但这意味着**同一个数字现在有两份**（一份在 doc、一份在断言），
   而 `CLAUDE.md` 明写「两处写同一条规则 = 迟早不一致」⇒ 建议调度台裁一句「哪一份是正本、另一份怎么写指针」。
2. 🔴 **`menu_dump.py` 修好之后，`BattleDriver` 里两张名牌的常量比「原版」大了 25%**（PH §八·1 的同一条）——
   本件现跑确认：`EnemyNameText` 屏幕框 **290.94×36.84**，而 `Hud()` 传的**正是**这两个数 ✅（名牌的 `Hud` 实参**已经**是修好之后的数）。
   但 PH 点名的**另一处**（`_enemyPlate = HudImage(…, 126.3f/108f, …)`，`NameBackground` 的**局域**高，真屏幕高 = `126.3 × 0.8 = 101.04`）
   **本件没动、也没复核**（不属于「Assertion/HUD 文字」这一族）⇒ **那笔仍开着**。
3. ⚠️ **`BattleScene.cs` 里 `Check` 只是 `Run()` 的局部函数**，而 `drv` 是**每个块各自** `FindObjectOfType` 出来的
   （顶层块里没有 `drv`）⇒ 新助手只能自己再取一份（本件在 `CheckHudBox` 里取）。**下一个往顶层加助手的会话会踩同一个坑**
   （我第一次就因为直接写 `drv.hudRoot` 编译不过；错是 `CS0103`，报在助手的声明行上，不是调用行）。
4. ⚠️ **`UpdateHud()` 会给 HUD 文字重设字号**（`SetTextTmp` 里 `_tmp.fontSize = TmpFontSize()`），
   而**对象没激活时 `ForceMeshUpdate` 早退** ⇒ `m_fontSize` 会**停在标称值**（不夹窗口、不收敛）。
   本件这 16 颗的标称都**恰好落在各自窗口内**（27.6 / 36.8 / 30.55 分别落在 `[2,35]`、`[8,40]`、`[10,42]`、`[15.79,40.5]`、`[8,31]` 里）
   ⇒ ⑤ 那条关系式**两种态都成立**。⚠️ **将来若有人把某颗的标称调到窗口外**，那条会**报红在一个看不出原因的地方** —— 记在这里备查。
5. ⚠️ **`Label.FontSizeBase` 在 `basePx == nomPx` 时会早退**（A536）。本件 16 颗**都不在那一格**
   （各自的标称是 27.60 或 36.80，而 basePx 是 36 / 49.63 / 36 …，**没有一对相等**）⇒ ② 的 `base` 那一条**有鉴别力**。
   🔴 若将来有人把某颗改成 `basePx == 标称`，**那条断言会变成空转**（不是红）—— 留一句给下一个会话。
