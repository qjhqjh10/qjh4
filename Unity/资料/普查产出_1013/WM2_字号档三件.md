# WM2 · 字号档三件（A579 + A574 + A575）

> 写手：**执行写手 WM2**（第四轮「清空 A 表」）· 时刻 **2026-10-13**（本机日期 2026-10-06 10:4x）
> 白名单内改了 **3 个文件**：`Shell/MissionsTab.cs`（A579）· `Shell/CardDetailPopup.cs`（A574+A575）·
> `Editor/CollectionScene.cs`（**只加自己那一段断言**，`+57` 行纯插入 / `0` 删）
> ⛔ 没跑 Unity / `-executeMethod` / `_run_8_checks.sh` · ⛔ 没动 git（只跑过只读的 `git diff --numstat`）
> · ⛔ 没改两张正本 · ⛔ 没越白名单 · 没碰 `Battle/*` / `Deck/*` / `工具/*`
> ✅ 跑过 **3 次** `TMPDIR=/tmp/wf_wm2 bash d:/4/Unity/工具/typecheck.sh`，最后一次 **运行时 0 / 编辑器 0**

---

## 一、结论

1. **三件都做了**（A579 / A574 / A575）—— 都是「字段级」的小笔，**今天零可观测差异或近零**，
   按铁律 11 照样做（判据正确性那一类）。
2. 🔴 **A371 那条断言的期望值必须跟着改，但它在我白名单外 ⇒ 本件只报不改** ——
   确切位置 = **`Editor/RewardsScene.cs:2198-2201`**（`CheckFontWindow(wkR37(), "Step Text", 10f, 50f, …)`）。
   **不改的话，下一次 `RewardsScene.Run` 那一条会红**（`15` vs `10`）—— 那是**预期红**（期望值过期），
   不是新缺陷。**详情见 §二·3**。
3. **补了 9 条断言**（3 颗 × `base` / `落位` / `前提`），落在 `Editor/CollectionScene.cs:4091-4147`
   （A404 那段**之后**、`// 三块面板的动作` **之前**，**纯插入、一行都没碰别人的**）。
4. ⚠️ **断言一次没跑过**（子代理不许跑 Unity）⇒ 归**主对话的同步点**跑 `CollectionScene.Run`
   （本件只碰了 `CardDetailPopup.cs` + `CollectionScene.cs` + `MissionsTab.cs`；
   ⚠️ **`MissionsTab` 那半要 `RewardsScene.Run` 才覆盖得到**，而那条今天**必定红一处**，理由见 §二·3）。

---

## 二、A579 —— 周常 `Step Text` 的自适应下界 `10 → 15`

### 2·1 改了什么（一处实参）

`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MissionsTab.cs:1310-1311`（`BuildMilestone` 尾句）：

```csharp
            Txt(cell, tr, stepText, Color.white, "Step Text", small ? 42.2f : 50f,
                small ? 10f : 15f, 50f, 36f);     // 下界两卡不同（每日 10 / 周常 15）—— 判据见上面那段（A579）
```

改前那一行是 `…, small ? 42.2f : 50f, 10f, 50f, 36f)` —— **只动了 `autoMinPx` 那一个实参**，
而且**只对周常那支生效**（下面 §2·2 说清为什么能这么写）。
上面 `:1285-1309` 的注释块**重写了**（原文写「`10f/50f` = 原版两个 `m_fontSizeMin/Max` 字段」——
那句对周常是**错的**，现在分两卡写明出处）。

### 2·2 判据（本件**亲跑** `menu_dump.py`，不是转引）

| 读的是哪份 | 命令 | `…/holder/text` 的 auto | 谁在用 |
|---|---|---|---|
| **`Weekly Mission Milestone T1`** | `python 工具/menu_dump.py bundle_menus_assets_all "Weekly Mission Milestone T1" --depth 4 --relative --no-layout` | **`auto[15.0~50.0]`**（`字号=50.0 基准=36.0 折行=1`） | ✅ **运行期**：`DF:MissionMilestonesDisplay__Setup.c` 头一句 `DestroyAllChildren`、随后逐格 `Instantiate(stepPrefab[Math.Min(i, 长度−1)])`，而真包里**三份 5 元 `stepPrefab` 同值 = T1..T5**（`资料/普查产出_1013/WM1_任务页三笔.md` §九·1） |
| `Weekly Mission Milestones Step (3)` | `… "Weekly Mission Milestones Step (3)" --depth 3 …` | `auto[10.0~50.0]` | ❌ **作者预览**，会被上面那句 `DestroyAllChildren` 删掉 —— **我们原来照的是它** |
| `Daily Skulls Mission Container Small` | `… "Daily Skulls Mission Container Small" --depth 7 …` | 5 格**全** `auto[10.0~50.0]` | ✅ 每日那支**不动**（`small = true` 仍走 `10f`） |

- **`small` 这个开关覆盖完整**：`grep -n "BuildMilestone(" Shell/MissionsTab.cs` ⇒ **只有 2 个调用点**
  （`:797` 每日 = `true` · `:974` 周常 = `false`）⇒ 两态就是**全部**，没有第三种情况会拿到错的下界。
- ⚠️ **今天零可观测差异**：这一格印的都是 1–3 字符（`3`/`10`/`100`），fs50 装得下 ⇒ 收缩一次都不会触发。
  属**判据正确性**那一类（铁律 11 + 铁律 5·c：别拿一个值顶两种情况）。

### 2·3 🔴 A371 那条断言（**只报不改** —— 文件在我的 ⛔ 名单里）

**位置**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:2198-2201`

```csharp
                // 字号窗口：原版 `Weekly Mission Milestones Step (3)/holder/text` = fs **50** · auto[10~50]；
                // 周常**不在** `Special Missions` 子树里（`_s = 1`）⇒ **不乘 1.15**，就是 [10, 50]。
                CheckFontWindow(wkR37(), "Step Text", 10f, 50f,
                                "★ 周常格里的数字：自适应窗口 = 原版 `[10,50]`（这一卡无 `localScale`）");
```

**要改两处**：① `10f` → **`15f`**；② 上面那两行注释 —— 它把判据源写成了**页内那份作者预览**
（`Weekly Mission Milestones Step (3)`），**那正是 A579 的病根**（注释与实现一起站错了源）。
正确的源 = `Weekly Mission Milestone T1..T5`（**运行期 `Instantiate` 的那份**），auto = `[15, 50]`。

- ⚠️ **`CheckFontWindow` 断的是 `fontSizeMin/Max` 两个字段**（`:9354-9365`）⇒ 本件改完实参之后，
  `FontSizeMin` 那半会读到 **15**、与写死的 `10f` 差 **5px**（容差 `0.5f`）⇒ **必红**。
- ✅ **同节每日那条不用动**：`Editor/RewardsScene.cs:2179` 断的是 `11.5f, 57.5f`（= `10×1.15` / `50×1.15`），
  每日那支的 `10f` 本件**没碰** ⇒ 那条仍然绿。
- ⛔ 本件**没改** `Editor/RewardsScene.cs`（它在 ⛔ 名单里）—— 请调度台代为改，或指派给 RewardsScene 的写手。

---

## 三、A574 —— 三颗 `autoBasePx`（补第 10 参）

### 3·1 表

| 节点 | 原版 `m_fontSizeBase`（本件**现读**原版 MB） | 改前（= 不传第 10 参 ⇒ 落到标称档） | 改后（传进去的实参） |
|---|---|---|---|
| `…/Counters/Counter` | **36.0**（pid `1892`） | 52.6（= `CcX1Px`） | `CcBaseX1 = 36f` |
| `…/Counters/Slash` | **36.0**（pid `1848`） | 57（= `CcSlPx`） | `CcBaseSl = 36f` |
| `…/Slash/Duplicates text` | **35.0**（pid `1823`） | 52.6（= `CcX1Px`） | `CcBaseX2 = 35f` |

**现读复现**（本件亲跑，纯 python / 只读）：
```
cd d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour
python -c "…json.load('MonoBehaviour_1892.json')…"   ⇒ m_fontSizeBase = 36.0
                                              _1848 ⇒ 36.0 · _1823 ⇒ 35.0
```
（同一次现读还复核了 `m_HorizontalAlignment` = **4 / 2 / 1**、`m_TextWrappingMode` = **0/0/0**、
`m_fontSizeMin/Max` = `25/52.6` · `25/57` · `18/52.6` —— 与 A404 那节断的一致。）

⚠️ **三颗不是同一个数**（36 / 36 / 35）—— 铁律 5·c，⛔ 别一个值顶三颗。

### 3·2 落点

- **新常量**：`Shell/CardDetailPopup.cs:147-155`（`CcBaseX1/CcBaseSl/CcBaseX2`，紧跟 `CcWrap*` 那一族）
- **调用点**：`Shell/CardDetailPopup.cs:715-736` 里的三处 `MenuDraw.Text(...)`
- **写法**：`…, CcWrapX1, 25f, 0f, CcBaseX1` —— `autoMaxPx` **照旧传 `0f`**
  （⇒ `MenuDraw.Text` 内部退回 `fontPx` 当上限，**A404 断的 52.6/57/52.6 一个字没变**）。
- **量纲核对**（本件读 `Battle/Label.cs:687-694` 确认）：`baseCur = cur × (basePx / nomPx)`，
  而 `FontSizeToPx` 就是 `nomPx` 那条口径 ⇒ **`FontSizeToPx(FontSizeBase)` 逐位等于传进去的 `basePx`**（36/36/35）✓

---

## 四、A575 —— 三颗的水平对齐档

### 4·1 表（原版档 → 我们走哪个助手）

| 节点 | 原版 `m_HorizontalAlignment` | 含义 | 我们的做法 |
|---|---|---|---|
| `…/Counters/Counter` | **4** | **`Right`** | `MenuDraw.AlignRight(lbC, rC)` |
| `…/Counters/Slash` | **2** | `Center` | **不调**（`TmpFont.NewText` 的出厂档就是 `Center`，调了反而偏） |
| `…/Slash/Duplicates text` | **1** | **`Left`** | `MenuDraw.AlignLeft(lbX2, rX2)` |

- **枚举判据**：`Runtime/TMP/TMP_Text.cs:74-77` ⇒ `Left=1, Center=2, Right=4, Justified=8, Flush=0x10, Geometry=0x20`
  ⇒ **`4` 是 `Right`、⛔ 不是 `Flush`**。✅ 正本 `资料/阶段二_卡片详情窗_原版规格.md:90` **已被调度台就地订正**
  （现文 = `⚠️ **4(`Right`)** —— ~~4(Flush)~~`），本件**无需再改**那个文件。
- **走的助手**：`Shell/MenuDraw.cs:1598/1604` 的 `AlignLeft` / `AlignRight` →
  `Battle/Label.cs:862/872` 的 `AlignLeftOn` / `AlignRightOn`（**挪整块**：`localPosition.x = 边缘 ∓ WorldW/2`）。
  ⛔ **不是**去改 TMP 的 `alignment` —— `Label.AlignLeftOn` 的注释写着：改 `alignment` **管不了折行之后
  每一行在块内怎么排**（本仓惯例 = `Shell/AllianceMemberTab.cs` 那一族）。
- 🔴 **顺序**：**先 `SetWrapping(false)`、后 `Align*`** —— `Battle/Label.cs:397` 逐字写着
  「它会挪 TMP 子节点 ⇒ **要在对齐/量宽之前调**（调用顺序：`SetAutoFitBox` → 本函数 → 对齐 → 量）」。
  本件按这个顺序写（`SetAutoFitBox` 在 `MenuDraw.Text` 里 → `SetWrapping(false)` → `Align*`）。
- **幂等**：`AlignRightOn` 写的是**绝对位置**（不是相对位移）⇒ `ShowCard` 首开那次 `Build()` 跑两遍也不会累积漂移。

---

## 五、断言清单（9 条 = 3 颗 × 3 条）

**落点**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs:4091-4147`
（A404 那段**之后**、`// 三块面板的动作` **之前**；**纯插入 `+57` / `-0`**，W-E2 / W-E4 / WSmall3 / WSmall4
那几段**一行都没碰**）。夹具沿用上一段那个已开着的卡片详情窗（`cnt = cd.Counter`，**该卡 `spares > 0`** ⇒
走 `Duplicate Counter` 那一支，三颗都在）。

**期望值来源**：**全部是原版字面量** —— `m_fontSizeBase` = 36/36/35（pid 1892/1848/1823，本件现读）·
框边/框心 = `869.41→942.69` · `942.69→970.71` · `970.71→1014.80`（正本 §三 `:90-92`）。
⛔ **不读** `CardDetailPopup.CcBaseX1` / `CcX1R` / `CcWrap*`（那与被测实参同源 = 同义反复）。

| # | 断什么 | 期望值来源 | 🧨 改坏法（怎么改就红） | 量具 |
|---|---|---|---|---|
| ① | `FontSizeToPx(FontSizeBase)` ≈ **36 / 36 / 35** | 原版 `m_fontSizeBase`（现读 MB） | 三处 `autoBasePx` 实参去掉（回到不传第 10 参）⇒ 落到标称档 52.6 / 57 / 52.6 | `Label.FontSizeBase`（反射读 TMP 那个 `protected` 字段，A305 ①） |
| ② | `Counter` 的**渲染块右缘** = **942.69px** | 原版 rect 右缘 + 原版 `align = 4 (Right)` | 去掉 `MenuDraw.AlignRight(lbC, rC)` ⇒ 退回 `Center`，右缘落到「rect 心 906.05 + 块宽/2」（差 `36.64 − 块宽/2`） | `Label.WorldW`（TMP `textBounds` 真测量）+ `PxOf(transform.position.x)` |
| ③ | `Slash` 的**渲染块块心** = **956.70px** | 原版 rect `942.69→970.71` 的心 + 原版 `align = 2 (Center)` | **给 `Slash` 也接上 `Align*`** ⇒ 块心挪半个块宽 | 同上 |
| ④ | `Duplicates text` 的**渲染块左缘** = **970.71px** | 原版 rect 左缘 + 原版 `align = 1 (Left)` | 去掉 `MenuDraw.AlignLeft(lbX2, rX2)` ⇒ 退回 `Center`，左缘落到「rect 心 992.755 − 块宽/2」 | 同上 |
| ⑤ | （前提，×3）渲染块宽 **> 5px** | —— | 量不出宽度（`WorldW` 退化）⇒ 这条红，而 ②③④ 会**跟着一起退化**（口径 = `MenuDraw.Align*` 内部那句 `HasMeasuredWidth()` 早退） | `Label.WorldW` |

**容差**：`base` 那三条 `0.5px`；`落位` 那三条 **`0.5px`**（`AlignRightOn` 与断言用的是**同一个** `WorldW`，
位置本身是**算出来的**、不是量出来的 ⇒ 只有浮点余差）。

### 5·1 ⚠️ 如实说清判别力（两笔各断各的）

- **① 咬 A574**：差值 = **16.6 / 21 / 17.6px**（标称档 − 原版 base）⇒ 远大于容差。
- **②③④ 咬 A575**：差值 = **半个渲染块宽**（因为「不调 `Align*`」时整块居中、`Center` 与 `Left/Right` 之间
  正好差 `块宽/2`）。
  - ⚠️ 这依赖「**渲染块比容器窄**」：本夹具三颗的块宽都远小于容器宽
    （`x2` < 73.28 · `/ ` < 28.02 · 一位数 < 44.09）⇒ 差值 ≈ 15~25px 量级。
    **⚠️ 没在实机上量过块宽**（这一句是推的）；但 ⑤ 那条前提断言会在量不出宽度时**先红**，不会静默放过。
  - ⚠️ **收敛结果不受 base 影响**（base 只改二分起点）⇒ **②③④ 对 A574 没有判别力**，
    ① 对 A575 也没有 ⇒ **两笔各有各的判别式，别互相顶**（这条已写进代码注释）。
- ⛔ **没有**「`!RectOfUnion` / 『一个 quad 都没有』」式断言；量的全是**渲出来的数**（`textBounds`）。

### 5·2 与上一段（A404）的关系

A404 那 21 条**一条都不用改**：本件只**加**参数（第 10 参）与**挪块**，`wrapPx` / `autoMin/Max` /
`SetWrapping(false)` / 容器宽**一个字没动** ⇒ A404 的判别式（`AutoSizing` · `fontSizeMin/Max` · 容器宽 · 折行档）
全部照旧。本件那 9 条**插在它后面**，与它零重叠。

---

## 六、没查清 / 没做的

1. ⛔ **断言一次没跑过**（子代理不许跑 Unity）—— 归**主对话同步点**：`CollectionScene.Run`
   （那 9 条）· **`RewardsScene.Run`（周常那条会红，见 §二·3 与下面第 2 条）**。
2. 🔴 **`Editor/RewardsScene.cs:2200` 的期望值 `10f → 15f`（+ 那两行注释）本件没改**（不在白名单）。
   **不改的话 `RewardsScene.Run` 必红一处** —— 请调度台改，或指派给那条线的写手。
3. ⚠️ **对齐前后「差多少 px」没实测**：§5·1 那两个差值是**按公式写的**（`块宽/2`），不是量出来的。
   三颗的渲染块宽（`x2` / `/ ` / 一位数）**本件没跑 Unity，量不到**。
4. ⚠️ **`Slash` 那条（③）在今天的实现下是「不动就绿」** —— 它的判别力来自「**别人给它接 `Align*`**」
   这个反向世界（本仓「对齐档」这一族最常见的错法 = 一个档顶三颗）。**如实标：它不是本轮修复的判别式。**
5. **`Slash` 的 `WorldW` 含不含末尾那个空格**：没查（`'/ '` 带一个尾空格；TMP 的 `textBounds`
   按 `SetWrapping` 之外的设置可能把它算进去/不算进去）—— **两种情形下块心都还是 956.70**
   （块心 = 节点位置，与块宽无关），所以**不影响 ③**；只影响 ⑤ 的 `> 5px`（远够）。
6. **`m_fontSizeBase` 的「写入路径」在第二次调用同一个 label 时有个已知残留洞**（`Battle/Label.cs:676-679`
   自陈）—— 本夹具是**新 label**（`Build()` 先销毁再重建）⇒ 不触发；**不属本笔，不改**。

---

## 七、顺手发现（⛔ 本件一条都没改）

1. 🔴 **`Single Counter` 那一支我们根本没照原版另建**（**真偏离，要记一条**）
   - 原版（正本 `资料/阶段二_卡片详情窗_原版规格.md` §三 **末行**，本件**转引、没现读那份 MB**）：
     `Card Counter/Single Counter`（**出厂 `INACT`**，`spares == 0` 时用）= 框 **875,844.50 → 1045,894.50**、
     里面的 `Counter` = fs **35** · auto[**25~35**] · align **2(Center)**，且**没有** `Slash` / `Duplicates text`。
   - 我们：`spares == 0` 时**只把 `Duplicate Counter` 改个名**（`box` 那一句的 `dup ? … : "Single Counter"`），
     几何 / 字号 / 对齐全部沿用 Duplicate 那一套。
   - 本笔的 A574/A575 取的是 **`Duplicate Counter` 那棵树**的三个 pid（1892/1848/1823）——
     这一条**不影响 A574/A575**（本笔改的就是那一棵树），但**是一笔独立的欠账**。
   - 📌 已在 `Shell/CardDetailPopup.cs` 的注释里**如实标注**（铁律 3），**没动代码**。
2. 🟡 **`Editor/RewardsScene.cs` 的 A371 那节「判据源站错了」**（= §二·3）：
   它照的是**页内作者预览** `Weekly Mission Milestones Step (3)`，而运行期被 `Instantiate` 的是
   `Weekly Mission Milestone T1..T5`。**同一份报告 §七·2 已经记过这件事**（WM1 写的），
   只是那一条断言当时没跟着改。
3. ✅ **正本 `:90` 的 `4(Flush)` → `4(Right)` 已被调度台订正**（本件现读 = 4，复核通过，**不必再改**）。
4. 🟡 **`menu_dump.py` 不印 `m_fontSizeBase`**（`Shell/MissionsTab.cs:370-373` 也记过）——
   本件那三个 base 值是**直接读 MB JSON** 拿的；若以后要批量核 base，得写脚本扫 `MonoBehaviour/*.json`。
5. ⚠️ **并发**：本件改 `Editor/CollectionScene.cs` 期间，**该文件被另一个写手动过两次**
   （Edit 工具当场警告 "the file had been modified on disk"；`git diff` 显示第二个 hunk（`:5189+`）
   从我看到的 `548` 行变成 `550` 行 ⇒ **别人在那一段加了 2 行**）。
   **本件那一段是纯插入（`+57 / -0`）**，两次都**没有撞车、没有覆盖别人的内容**（§八 有对账数字）。

---

## 八、类型检查结果

```
$ TMPDIR=/tmp/wf_wm2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- **跑了 3 次**。第 1 次报 **7 条**、第 2 次报 **2 条**、第 3 次 **0/0** ——
  三次的错**全部集中在 `Battle/AttackSelector.cs` 与 `Battle/BattleDriver.cs`**
  （`EnterAnimSeconds` / `ChatPopupPanel.PointerDownAt` / `HoverPickReady` / `TickMultiCards` …），
  **一个字都不在我的文件里** ⇒ 按铁律 13·3 = 「别的写手正在写的半成品」，**我一条都没去改别人的文件**，
  隔 100～120 秒重跑就没了。
- **行尾**（改完按二进制数）：三个文件**全是纯 LF、CRLF 计数一直是 0**（没被翻）：
  `Shell/CardDetailPopup.cs` `CRLF=0 / LF=908` · `Shell/MissionsTab.cs` `CRLF=0 / LF=1515` ·
  `Editor/CollectionScene.cs` `CRLF=0 / LF=5759`。
- `git diff --numstat`（给调度台对账）：

| 文件 | 本件之前 | 本件之后 | 本件的净增 |
|---|---|---|---|
| `Shell/MissionsTab.cs` | 250 / 31 | **267 / 32** | **+17 / +1**（注释块 12 行 + 调用点 2 行换 1 行） |
| `Shell/CardDetailPopup.cs` | 24 / 5（A404 的） | **61 / 5** | **+37 / +0**（常量 9 行 + `BuildCounter` 那段替换） |
| `Editor/CollectionScene.cs` | 602 / 0 | **661 / 0** | **+59 / +0**（**我的 57 行纯插入** + 别人在 `:5189+` 的 2 行） |

- **本件改了哪些行**（改后行号）：`Shell/MissionsTab.cs:1285-1311`（注释 + 实参）·
  `Shell/CardDetailPopup.cs:147-155`（新常量）· `:698-714`（注释）· `:715-736`（三处调用点）·
  `Editor/CollectionScene.cs:4091-4147`（9 条断言）· 新建本文件 `资料/普查产出_1013/WM2_字号档三件.md`。
- ⛔ **没碰**：`项目任务.md` · `CLAUDE.md` · `Editor/RewardsScene.cs` · `Shell/MenuDraw.cs` ·
  `Battle/Label.cs` · `工具/*` · git。
