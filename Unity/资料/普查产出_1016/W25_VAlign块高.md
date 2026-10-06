# W25 · A712 阶段 2 根治件：`Top`/`Bottom` 的位移**按块高**算（多行串口径）

> 写手代理 W25 · 2026-10-16 · 「清空 A 表」第七轮 · 判据：`普查产出_1016/W16_A712阶段2.md` §五·1（本件所治那一笔的原委 + 它给的数字）· §四（全局校正五条理由）·
> `普查产出_1015/W11_A712字墨校正.md` §2·4/§三（逐档算式 + 代理口径）· 本机 TMP 包 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/{TextMeshPro,TMP_TextInfo,TMP_LineInfo}.cs`（逐字段现读）
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改两张正本 · ✅ 类型检查跑了 **5 次**（末次：运行时 **0** 错 / 编辑器 **0** 错，`TMPDIR=/tmp/wf_w25`）· ✅ 行尾核过（`Label.cs`/`BattleScene.cs` **CRLF**、`ReferralPopupWindow.cs` **LF**，以**现数**为准）

---

## 一、结论

1. **缺口已根治**：`Label` 的垂直位移现在**按块高**算 —— 新增 `VOffsetBlockWorld()`（`Top` 折**首行** / `Bottom` 折**末行**，
   = 「本行行盒心相对**块心**」那一截），并进 `VOffsetWorldNow()` 的最后一项。
2. **`Middle` 逐位不变**（论证见 §三）—— `OurInkCenterWorld` / `OrigInkCenterPx` / 两个全局校正常数**一个浮点都没改**。
3. **`ReferralPopupWindow` 的 `Descripton` 落成真调用**（`Top` + 框高 140）：W16 判「档案对、但不落」的前提被本件补上了
   ⇒ 敢打开那一行（判据 = prefab `m_VerticalAlignment = 256`，该文件 `:427` / `W16` §二）。**新增 5 条断言**（§4.6e）。
4. **顺手订正一处量法**：`TmpInkCenterPx` 原量**整串墨盒并集**（多行串量到的是**块心附近**、不是首行）⇒ 加 `line` 那一格
   （默认 `-1` = 旧行为**逐位不变**，11 个既有调用点一字未改）。**没跑 Unity** ⇒ 红绿以收口那次 `BattleScene.Run` 为准。

---

## 二、改动逐处（`文件:行号` = **改完那一刻**；⛔ 别按行号定位）

### 2·1 `Battle/Label.cs`（机制本体 · `Middle` 之外的位移算法）

| # | 行 | 改前 | 改后 |
|---|---|---|---|
| ① | `:1394` `OneLineBoxWorld()` | 一个可见字都取不到 ⇒ **一律** `return _tmpH`（多行时那是**整块**高 ⇒ 一行被当 N 行） | 多行且 `_tmpH < 100f` ⇒ `return _tmpH / n`；哨兵值（4.29e9）**不许除**（会躲过守卫）⇒ 原样退回 |
| ② | `:1420` `LineCountNow()` 🆕 | ——（不存在） | **可见行数**（`lineInfo[i].characterCount > 0` 才数；空行/末尾换行不参与） |
| ③ | `:1438` `LineBoxWorldAt(k)` 🆕 | —— | 第 `k` 行的**行盒高** = `lineInfo[k].ascender − descender`（每行用**它自己**那份值，⛔ 不拿首行顶替） |
| ④ | `:1473` `VOffsetBlockWorld()` 🆕 | —— | **本件唯一一条新算式**：`Tier` 非 `Top`/`Bottom` ⇒ **第一句就 `return 0f`**；否则 `(Σ前k行 + Lk/2) − blockH/2`（`Top` k=0 · `Bottom` k=n−1） |
| ⑤ | `:1553` `VOffsetWorldNow()` 末句 | `return OrigInkCenterPx(tier,m,FontPxNow,boxHpx)/PxPerWorld - OurInkCenterWorld();` | 同式 **`- VOffsetBlockWorld()`**（仅仅**追加**一项；前三项一字未动） |

**④ 的推导**（写进代码注释）：`RefreshBounds` 一律把**整块的行盒心**摆到节点上（`anchor.y=0.5`）⇒
第 `k` 行的行盒心相对**本节点** = `(L0+…+L(k−1)) + Lk/2 − blockH/2`（`blockH` = 首行 `ascender` 到末行 `descender`）；
等高时化整成 `(n−1−2k)/(2n) × blockH`。**哪一档折哪一行** = 原版 TMP 那**一个** `anchorOffset`
（`TextMeshPro.cs:4193-4232`，那里它是**行盒心**的偏移；`W11` §2·1 那张表是它的推论）：
`Top` = 基线上角−a/p·F ⇒ 首行 · `Bottom` = 基线下角−d/p·F ⇒ 末行 · `Middle`/`Capline`/`Midline` = `框心 − …` ⇒ **块心自己**。
**量不到就出声 + 返回 0**（红线）：两个 `NoteNotApplied` 的 key 都是 **`垂直档块高`**（🆕，与既有 9 个口及 `垂直档框高` **不撞**）。

### 2·2 `Shell/ReferralPopupWindow.cs`（生产调用点）

| 行 | 改前 | 改后 |
|---|---|---|
| `:445`（`Descripton`） | 只有 `MenuDraw.TextBox(...)` + 一段「没落 + 为什么」的注释 | **`MenuDraw.SetVAlign(_descr, Label.VAlign.Top, DescrR);`** + 注释改写成现状（保留「W16 为什么当时停手」那段史） |
| `:427-441`（注释） | 「本模型对多行串没有正确口径 ⇒ 停手」 | 同上，就地订正（铁律 5）：缺口已治，列出老病 ≈49px 与新算式 |

⚠️ **`Shell/MenuDraw.cs` 一字未改** —— 那个转发助手（`:1768`）已经是「只转发、换算只有一份」的形状，够用。

### 2·3 `Editor/BattleScene.cs`（断言宿主 + 量法订正）

| 行 | 改前 | 改后 |
|---|---|---|
| `:248` `TmpInkCenterPx` | `(Label lb)`，量**整串**墨盒并集 | `(Label lb, int line = -1)`；`line >= 0` ⇒ 只量 `lineInfo[line]` 的 `firstCharacterIndex..lastCharacterIndex`；**默认档逐位 = 旧行为**（11 个既有调用点一字未改） |
| `:3349` §4.6e 🆕 | —— | 5 条断言（见 §四） |

---

## 三、🔴 「`Middle` 逐位不变」的论证（四条，逐条可核）

1. **新算式对 `Middle` 的返回值**：`VOffsetBlockWorld()` 的**第一句**是
   `if (tier != VAlign.Top && tier != VAlign.Bottom) return 0f;` —— `tier` 读的是字段 `_vTier`，
   而 `Middle` 档下它恒为 `Middle`/`Capline`/`Midline` ⇒ **到这一行就返回 0**，
   后面**没有任何一句被执行**（`_tmpH` 没读、`lineInfo` 没碰、出声口没进）。
2. **减法项 `x − 0f` 逐位等于 `x`**：IEEE-754 下 `x − (+0)` = `x`（连 `x = −0` 都保持 sign bit）。
   实际值量级 ~0.1 世界单位、绝不是 ±0 ⇒ **逐位**。
3. **前两项一字未动 + 唯一动到的那一格按 `_vTier` 分了档**：`OrigInkCenterPx(...)` / `OurInkCenterWorld()` 的函数体**没碰过**；
   `PxPerWorld` / `Pragati` / `Asar` 三处常量没碰；`_vFace` 仍默认 `Pragati`；`_boxHFromAutoFit`/`_vBoxH` 的读取顺序没碰。
   `OneLineBoxWorld()` 的兜底那一格**同时被 `_vTier` 挡着**：
   `if (n > 1 && _tmpH < 100f && (_vTier == VAlign.Top || _vTier == VAlign.Bottom)) return _tmpH / n;`
   ⚠️ **这是本笔自己补的一刀**：第一版只写了前两个条件，那样「多行 **+ 全空格串** + `_tmpH ∈ (0,100]`」
   这一类下 `Middle` 的返回值**会**变（推演：`_tmpH=90 n=3` ⇒ 90 vs 30）。加上 `_vTier` 之后，
   **`Middle` 走的就是这一行的最后那一句 `return _tmpH`**（**逐字节 = 改前**——改前那一句就是它）
   ⇒ 兜底那一格对 `Middle` **结构上不可能**产生影响。
   ⇒ `Middle` 的 `+0.1291 F`（Pragati）/ `+0.1616 F`（Asar）**一个字节都没变**（`W16` §四 五条理由照旧成立）。
4. **`VOffsetWorldNow` 的早退路也没碰**：`tier` 被框高守卫退回 `Middle` 那一支，发生在**赋 `tier` 之后、求 `VOffsetBlockWorld()` 之前**；
   而后者读的是字段 `_vTier`（仍是 `Top`/`Bottom`）⇒ 它会**照常算**（框高已给 ⇒ 算式成立）
   ⇒ 位移 = `Middle 那一格 − … − 块高那一截`。这是**对的**：框高拿不到时原版 `Top` 也摆不出来，
   而「块高那一截」是**摆位模型**的一部分、与框高无关 ——⛔ 不是「凭空多减一项」。

**结论**：`OneLineBoxWorld` 的兜底那一格虽然**确实**要多减「块高 ÷ 行数」这一层（旧写法在多行时会给个错值），
但**被 `_vTier` 挡在 `Top`/`Bottom` 之外** ⇒ 全工程 ~340 个文字入口的出厂档（`Middle`）**在所有状态下逐位不变**
（不是「大部分情况下」—— 是**结构上**不可能被这一件碰到）。

---

## 四、新增断言逐条（`Editor/BattleScene.cs` §4.6e，5 条）

**判据**（期望值那一侧）= **原版 TMP 的 `anchorOffset` 只折行盒心** ⇒ `Top` 那一格 = `框高/2 − (a−c/2)/p × F`
（Pragati `a=70 c=60 p=95` · `F=35` · `框高=50` ⇒ **+10.263px**）——**与串有几行无关**，这就是本节要咬的性质。
**观测值那一侧** = `TmpInkCenterPx(lb, k)`（真渲出来的字形四边形，`characterInfo[i].topLeft/bottomLeft`）。
探针串 = `"COUNTER"` / `"COUNTER\nCOUNTER\nCOUNTER"`（**大写 + 数字 + 换行、不带下伸部** —— `W11` §2·4 记着带下伸部的串两个基准差 **4px 量级**，容差只有 1.5px ⇒ 那样会量出与被测行为无关的数）。

| # | 断言 | 期望值出处 | 改坏法 |
|---|---|---|---|
| ① | （前提）两颗探针都走 **TMP** 后端 + 真排成 **1 行 / 3 行** | 探针自身 | 三行那颗没排成三行 ⇒ 本节**退化成「单行验单行」**（写成 `Check` 而不是裸 `if`，不许静默空转） |
| ② | ★★ **多行串落 `Top`，首行的墨心 = 原版那一格**（+10.263px ± 1.5） | `TextMeshPro.cs:4193-4232` 的 `anchorOffset` | 把 `VOffsetBlockWorld()` 那一项删掉（= 只按一行算）⇒ 多行偏 ≈54px，而**单行照样绿** ⇒ **只有本条会红** |
| ③ | ★★ **灭自证**：**单行与三行**落同档同框时**首行落在同一高度**（差 < 1.5px） | 同上（`Top` 与行数无关） | 「把期望值改成现状」**不可能同时满足 ② 与 ③**（两条指向相反：② 要绝对目标、③ 要「与行数无关」）；旧写法差 ≈54px = 容差的 36 倍 |
| ④ | ★ **多行串落 `Bottom`，末行的墨心 = 原版那一格**（−6.579px ± 1.5） | 同上（`Bottom` 折**末行**） | 把 `Bottom` 的 `k` 错写成 0（折首行）⇒ 偏 ≈98px，而 **② 照样绿** ⇒ `Top`/`Bottom` 各钉一支，不许只留一条 |

**「灭自证」为什么成立**：`TmpInkCenterPx` 量的是**真渲出来的顶点**（不是 `Label.VOffsetWorld`，⛔ 那是我们自己的算式），期望值来自**原版资产的字段原文**；而 ② 与 ③ 的目标方向**互斥** ⇒ 不存在「把两处一起改回去」还能同时绿的写法。

---

## 五、没查清的 / 有话要说的

1. 🔴 **白名单与验收②冲突，我按「能验」那一头做了**：简报的白名单 = `Battle/Label.cs` · `Shell/ReferralPopupWindow.cs` ·
   `Shell/MenuDraw.cs`，但验收②要「**新增断言**」。全仓 A712 那 8 条断言 + `TmpInkCenterPx` 助手**都在**
   `Editor/BattleScene.cs`（宿主 = `BattleScene.Run`），`Shell/MenuDraw.cs` 里一条断言也没有 ⇒
   新断言**只能**落 `Editor/BattleScene.cs`。**如实报，请调度台核**（若要挪，那要另立账 + 换宿主自检）。
2. ⚠️ **本件一条断言都没实跑**（本笔不许跑 Unity + 铁律 12 攒批）⇒ 红绿以收口那次为准。
   两处**最可能红**的地方：① 三行串是否真排成 3 行（`LineCount` 由 TMP 决定，见断言①的前提）
   ② 1.5px 容差在多行下是否够（我按「等效等高行盒」推的，TMP 若对末行给了**不同**的 `ascender/descender`，
   断言④ 会有**小于 1px 量级**的偏差 —— **没量过，如实记**）。
3. ⚠️ **`Bottom` 那一支的逐行累加**是**真的逐行加**（`Σ L0..L(n−2)`），不是拿 `blockH − L(n−1)` 反推 —— 两者在**行距不等**时会给出不同的数（我选了「按 TMP 自己逐行的数」那一支）。**没验**哪种更贴原版（原版那条路只有一份行盒心）。
4. **`anchor.y != 0.5` 的两处不在本模型里**（`Battle/SettingsPanel.cs:392` · `BattleDriver.cs:7958` 的 `HandLabel`，`W11` §五·6 记的两处）：它们若落 `Top`/`Bottom` 且是**多行**，本件的式子**不成立**（模型前提是「节点位 = 原版框心」）。今天两处都是单行、也都是出厂 `Middle` ⇒ **不触发**。**如实记，没猜。**
5. **顺手发现（⑧）**：`TmpInkCenterPx` 原来那个「整串并集」量法**不止影响本节** —— `W14_A712字墨探针.md` 与 `BattleScene.cs:3535` 那一段（`RowOn(...)`）在**多行件**上量的都是块心。今天它们量的全是单行件 ⇒ **结果不受影响**；谁以后拿它量多行件，**先看 `line` 那一格**。**没改那两处。**
6. **卡面那条线一行没动**（`Core/CardView.cs` 自己 `TmpFont.NewText` + 自己摆位，不走 `RefreshBounds`）—— 与 `W11` §五·7 · `WA712` §六·4 的登记一致，**本件也没碰**。
