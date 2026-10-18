# W — `Editor/CollectionScene.cs`：6 条过期断言对齐 `DeckInfoPopup` 新行为（执行代理 · 2026-10-18）

> 只改了 **`Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`** 一个文件（+102 / −25 行）。
> 判据 = `W_DeckInfoPopup_修复.md`（§三·5 逐条点名的 6 处）· `R_DeckInfoPopup_对账.md`（原版真值表）·
> `Shell/DeckInfoPopup.cs:1168-1201`（`showGmIcon` / `ddShift`）。
> ⛔ 没跑 Unity / 没跑 `*.Run` / 没动 git / 没碰别的文件。
> ⚠️ 本文件在我开工前**已有别人未提交的改动**（`N55`：`FontSizeToPx` → `FontSizeToPxNow` 那一族），
> 我一处都没碰；下面的行号是**改后现读**的。

---

## 一、改了哪 6 处（`Editor/CollectionScene.cs`）

| # | 位置（改前 → 改后行号） | 改前 | 改后 |
|---|---|---|---|
| 1 | `:2422` → **`:2437`** `Army Icon` | `CheckAt(… 767.9, 867.9, 106.7, 216.7)` | `CheckAt(… **659.03, 759.03**, 106.7, 216.7)` —— classic（隐藏态） |
| 2 | `:2424` → **`:2441-2443`** `Game Mode Separator` | `CheckAt(… 759.0, 767.9, …)`（期望存在） | `CheckTrue(FindChild(pr, "Game Mode Separator") == null, …)` —— classic 下**整颗不建** |
| 3 | `:2435` → **`:2459`** `Deck Name` 左缘 | `872.9f` | **`764.03f`**（= 872.9 − 108.87） |
| 4 | `:2440` → **`:2466`** `Warlord Name` 左缘 | `872.9f` | **`764.03f`** |
| 5 | `:2444-2450` → **`:2472-2474`** `Game Mode Icon` | `CheckTrue(… Texture.name == "40k_gamemode_icon_classic")` + `CheckAt(659,759,…)` | `CheckTrue(FindChild(pr, "Game Mode Icon") == null, …)` —— classic 下**不建**（那颗图的断言搬到 skirmish 态，见 §二 ②′） |
| 6 | `:1917-1918` → **`:1923-1926`** `Warlord Name` 文本 | `wlc != null ? wlc.Name : Loc.T(…)`（英文） | `wlc != null ? **CardText.Name(wlc)** : Loc.T(…)`（本地化卡名，`Zh` 语档闸在里面） |

每处都带注释：**「2026-10-18：`Game Mode Icon` 按原版机制隐藏（判据 → `R_DeckInfoPopup_对账.md`）；位置随之左移 108.87」**（或同一句的等价展开，逐条都写明判据出处）。

**顺带就地订正的一处过期注释**（铁律 5）：`:1913` 原来引「`Shell/DeckInfoPopup.cs:846` 的 `wl != null ? wl.Name : …`」——
本轮改成 `:1228` 的 `CardText.Name(wl)`，行号与句子一起换掉了，并写明「原写 `:846`、那一半写的是 `wl.Name`」。

---

## 二、新增的两态断言（都是**新增**，一条旧的都没删）

### classic（夹具 `CollectionData.DeckAt(0).GameMode == 0`）—— 断在原来那扇窗 `pop` 上

- `Army Icon` 矩形 = **659.03 → 759.03**（左移 108.87）；
- `Game Mode Separator` **`== null`**（整颗不建，⛔ 不是「建了但关着」）；
- `Game Mode Icon` **`== null`**；
- `Deck Name` 渲染左缘 = **764.03 ±2px**、`Warlord Name` 渲染左缘 = **764.03 ±2px**（与原来同一套 `LabelRenderedPx` 量法）；
- 🆕 **补了两条「前提」**：`Deck Name` / `Warlord Name` 底下**必须量得到 `Label`** ——
  原来那两句是 `if (dnl != null) { … }` **没有 `else`**，取不到时整条**静默空转**（同 `A52-F10` 那个坑）；
  现在取不到**当场红**，⛔ 不是放宽。

### skirmish（`GameMode == 13`）—— ②′ 段（`:2476-2521`）

做法：**只改内存**（`CollectionData.Raw(0).GameMode = (int)GameMode.Skirmish`）→ **原地重建**（`pop.Open()` = `Build()`）
→ 断 → **改回原值 + 再 `Open()` 一次**（把树建回 classic 那一版给后面几小节用）。照本文件既有写法
（`Practice Deck` 那段 `flipSaved/flipIdx` 的「只改内存、验完还原」）。

断的内容：

- `Game Mode Icon` **建出来了**，且 `Texture.name == "40k_gamemode_icon_skirmish"`
  （实得串会写进断言文案；取不到图会打「**没有图 / 没建出来**」而不是安静过去）；
- `Game Mode Icon` 矩形 = **659.0 → 759.0**（跑后真值）；
- `Game Mode Separator` **建出来了**且矩形 = **759.0 → 767.9**（显隐随图标）；
- `Army Icon` 矩形 = **767.9 → 867.9**（有图标态**不**左移）；
- `Deck Name` / `Warlord Name` 渲染左缘 = **872.9 ±2px**（不左移），各配一条「量得到 `Label`」前提。

### 🔴 为什么用「原地重建」而不是 `win.OpenDeckInfo(0)` 另开一扇（如实记，这是个设计决定）

`WindowsManager.OpenWindow` 对弹窗会 `currentWindow.ToBackground()`，而 `ToBackground()` 落到
**`SetActive(false)`**（`Shell/GameWindow.cs:566-569` 那张表）。这扇窗一旦被压到背景，
后面几小节（③ 抽屉 / ④ `Switch Deck Info` / ⑥ 立绘命中区）用的还是**这棵树**，
而它们量的 `GetComponentInChildren<Label>()`（默认**跳过**没激活的对象）会**量不到 ⇒ 静默空转**。
⇒ 改用 `pop.Open()`（生产同一条 `Open() → Build()`；先例 = `Editor/MainMenuScene.cs:4067` 的「原地重建」）。
⚠️ 重建是同步干净的：`Build()` 用 `RewardsWindow.DestroySafe`，**批处理下走 `DestroyImmediate`**
（`Shell/MainMenuSubmenuWindow.cs:207-213`）；`CostRowCounts` 也会 `Clear()`（`DeckInfoPopup.cs:1799`），不会累加。
⚠️ `pop.Rows` 在本段之前（`:2275-2365`）就用完了，重建清表对后面没有影响。

---

## 三、没改动的 / 判不准的

1. **队列那几条没动**（`:1967-1990` 一带「8 颗钮的每一张 quad 都画在红底之上」）——
   修复报告已核过：红底 `QDI`(3120) / 钮 `QDIRow`(3124) · `QDIRowArt`(3125) 仍在其上，不受本轮影响。
2. **`Red Bg` / `Info Panel` / `Info Panel` 矩形、`Deck Name` 文本、按钮几何、`state 0` 显隐、
   立绘命中区、抽屉开关**等断言**一个字没动**（它们不因本轮改动而变）。
3. **没有新增「其它 PlayMode」的态**（Draft / Ranked / Campaign…）：`GameModesIconCollection` 这个 SO
   本地不存在，`R_DeckInfoPopup_对账.md` 已把它列进「查不到」；实现今天把「非 skirmish」**一律当 classic 隐藏**
   ⇒ 自检也只覆盖 classic / skirmish 两态。（这一条**不是**判定「不用做」，是**判据只到这两态**。）
4. **`Game Mode Separator` 的旋转（−10.60°）没单独断言** —— 旋转绕枢轴（中心）、不改 `t.position`，
   `CheckAt` 量的是中心 ⇒ 加了旋转也断不出来。原代码也没有这一条，我**没有新增**（本轮任务只对齐过期断言）。
5. **`Army Icon` 的 659.03 / 759.03 是「常量 − 108.87」算出来的**，不是原版 dump 的实读值
   （原版 HLG 跑出来的就是 659 / 759 —— `R_DeckInfoPopup_对账.md`「Army Icon」行写的是
   「Game Mode 隐藏时原版 HLG 会把它移到 659」，即 659→759）。⇒ 有图标态仍按原版 dump 的 **767.9→867.9**。
6. ⚠️ **本件夹具里 `Warlord(0)` 非空**：`Warlord Name` 的「空战将位」那一支今天**走不到**，那条 `Loc.T(键)` 兜底仍只是为将来准备（原注释保留）。

---

## 四、typecheck 原文

命令（必须带这个 `TMPDIR`）：

```
TMPDIR=/tmp/wf_f2 bash d:/4/Unity/工具/typecheck.sh
```

输出：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾：改后 `CRLF 0 / LF 10212`（原文件就是纯 LF，没翻）；`git diff --numstat` = **102 / 25**（只动了这几个断言块）。
