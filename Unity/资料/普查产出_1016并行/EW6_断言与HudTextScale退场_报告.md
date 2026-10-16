# EW6 · 断言对齐 + `HudTextScale` 退场（T09 收尾）

> 白名单 = `Editor/BattleScene.cs` + `Core/LayoutSpace.cs` + 本报告。⛔ 未跑 Unity（一条 `*.Run` 都没跑）、未动 git、未改白名单外任何文件。
> 判据来源 = `资料/普查产出_1016并行/EW4_HUD字族迁uGUI_报告.md`（**唯一判据来源**）+ 两处源码**现读**。
> 最后一次 `TMPDIR=/tmp/wf_ew6 bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（在**删 `HudTextScale` 之后**跑的；删之前另跑过一次也是 0/0）。

---

## ① 结论

### 1·a 四处必改：**行号全部对不上（已漂移），四处都按内容找到并改完** ✅

EW4 给的行号是它写作时的；本会话里 `BattleScene.cs` 又被别的写手改过（本件开工时该文件已是 `M`），
逐处**按标识符现核**（`HudTextScale` / `HintWidth` / `PileLabelWorldW` / `HandLabelWorldW`）—— 四处**都没被别的写手动过**，语义与 EW4 描述**逐条吻合**，改法与它建议的**一致**。

| EW4 说的处 | 现核（改后行号） | 它原来的形态 | 改成了什么 | 结论 |
|---|---|---|---|---|
| `:1903-1910`（两条） | **`BattleScene.cs:1903-1975`**（旧块 = 旧 `:1903-1911` 九行） | ① 断 `LayoutSpace.HudTextScale == 1` ② 断 `HudLabelScaleForTest(0) ≈ HudTextScale` | **两条一起删**，换成**逐颗**那一组（**44 条**：21 父链 + 21 锚点 + 颗数 + 鉴别力前提） | ✅ 见 1·b |
| `:9350`（提示条宽度） | **`BattleScene.cs:9424-9429`**（旧 `:9350-9351`） | 右项 `LayoutSpace.VisibleWidth * 0.9f`（= **16.0 世界单位**） | 右项 `1920f * 0.9f`（= **1728 设计 px**，同一个门槛换把尺） | ✅ |
| `:2028`（牌库张数） | **`BattleScene.cs:2092-2104`**（旧 `:2028-2030`） | `PileLabelWorldW < MyDeckSizePlateWorldH * (442f/112f) - 0.05f`（**混两把尺**：左边 px / 右边世界单位 ⇒ 右边只有 2.16） | 右项 = **原版底板宽 `238.5f`**（同一个 `MyDeckSizeW` 那个数、写**字面量**）；下限 `0.01` 世界单位 → **`1f` px**（等值换算） | ✅ 取 EW4 给的**改法②**（改法①要在 `BattleDriver` 加读口 ⇒ 越白名单） |
| `:20173`（手牌数框） | **`BattleScene.cs:20251-20261`**（旧 `:20173-20178`） | `<= LayoutSpace.Px(234f)` / `Px(64f)`（÷108 = 2.1667 / 0.5926 世界） | `<= 234f` / `<= 64f`（**设计 px 原样**） | ✅ |

🔴 **四处共同的口径**：**画布档的读回一律读真 px** —— 本件改完的这几处**一个 `* 108f` / `LayoutSpace.Px(...)` 都不剩**
（`PileLabelWorldW` · `HandLabelWorldW/H` · `HintWidth` 现在全是**设计 px**，判据 = 它们各自的 doc，EW4 `②·a` 那张表）。

### 1·b 新增那一组（44 条）：**放在原 `:1903-1911` 的位置**，与 EW4 建议的形状一致

`BattleScene.cs:1920-1975`（一个 `{ … }` 作用域块）：

1. `Check(drv.HudLabelCount == 21, …)` —— 颗数；
2. `Check(drv.hudRoot != null && HudCanvas.RootRect != null && drv.hudRoot != HudCanvas.RootRect.transform && HudCanvas.RootRect.parent != drv.hudRoot, …)` —— **鉴别力前提 / 灭自证**；
3. 21 颗 **逐颗两条**：`Check(drv.HudLabelOnCanvasForTest(i), …)`（**父链**）+ `Check(Mathf.Abs(a.x-wx) < 1e-6f && Mathf.Abs(a.y-wy) < 1e-6f, …)`（**锚点**，`wx/wy` 来自本文件**自带的** `hudWantX/Y` 两张表）。

**期望值表**（本文件自带的**第二份**，⛔ 不读 `BattleDriver` 的同名常量 —— 那是自证）：
x = `hudWantX` · y = `hudWantY` · px 那两列是按 `x01×1920` / `(1−y01)×1080` 折出来的**屏坐标（y 向下）**，只为消息里好读：

| # | 标识符（`_hudLabels` 顺序） | 节点名 | x01 | y01 | 屏幕 px |
|---|---|---|---|---|---|
| 0 | `_turnLabel` | `TurnLabel` | 0.500000 | 0.810000 | (960.00, 205.20) |
| 1 | `_enemyText` | `EnemyPlateText` | 228.17147/1920 = 0.118839 | 1−66.88264/1080 = 0.938072 | (228.17, 66.88) |
| 2 | `_myText` | `PlayerPlateText` | 225.74343/1920 = 0.117575 | 1−1002.47582/1080 = 0.071782 | (225.74, 1002.48) |
| 3 | `_skullScore` | `MatchSkullsScore` | 0.117400 | 0.111340 | (225.41, 959.75) |
| 4 | `_myFaithText` | `PlayerFaithText` | 0.971770 | 0.397040 | (1865.80, 651.20) |
| 5 | `_foeFaithText` | `EnemyFaithText` | 0.971770 | 0.821670 | (1865.80, 192.60) |
| 6 | `_myStoneText` | `PlayerSpiritStoneText` | 0.974010 | 0.403430 | (1870.10, 644.30) |
| 7 | `_foeStoneText` | `EnemySpiritStoneText` | 0.973230 | 0.815000 | (1868.60, 199.80) |
| 8 | `_energyLabel` | `EnergyLabel` | 0.97180+1.8/1920−1.5/1920 = 0.971956 | 0.48556−0.5/1080+0.594/1080 = 0.485647 | (1866.16, 555.50) |
| 9 | `_foeEnergyLabel` | `FoeEnergyLabel` | 0.97060+3.284/1920−1.5/1920 = 0.971529 | 0.73278+0.15/1080+0.594/1080 = 0.733469 | (1865.34, 287.85) |
| 10 | `_handLabel` | `HandLabel` | 0.075000 | 0.158000 | (144.00, 909.36) |
| 11 | `_endTurnLabel` | `EndTurnButton` | 0.962630 | 0.578190 | (1848.25, 455.55) |
| 12 | `_clockLabel` | `TurnClock` | 0.962630 | 0.57819+0.021 = 0.599190 | (1848.25, 432.87) |
| 13 | `_pileLabel` | `MyPileLabel` | 0.90104−20.75/1920 = 0.890233 | 0.10648+165/1080 = 0.259258 | (1709.25, 800.00) |
| 14 | `_foePileLabel` | `FoePileLabel` | 0.90104−5/1920 = 0.898436 | 0.90741−160/1080 = 0.759262 | (1725.00, 260.00) |
| 15 | `_tipText` | `TipText_Text` | (960+1.0600586)/1920 = 0.500552 | 1−(540−91.78)/1080 = 0.584981 | (961.06, 448.22) |
| 16 | `_resultLabel` | `ResultLabel` | 0.500000 | 0.500000 | (960.00, 540.00) |
| 17 | `_titleMe` | `TitleText_Me` | 217.62721/1920 = 0.113347 | 1−1038.15523/1080 = 0.038745 | (217.63, 1038.16) |
| 18 | `_titleFoe` | `TitleText_Foe` | 217.92701/1920 = 0.113504 | 1−102.402645/1080 = 0.905183 | (217.93, 102.40) |
| 19 | `_qpTextMe` | `QPText_Me` | 1865.8381719470285/1920 = 0.971791 | 1−643.9898812899978/1080 = 0.403713 | (1865.84, 643.99) |
| 20 | `_qpTextFoe` | `QPText_Foe` | 1864.9734582742763/1920 = 0.971340 | 1−198.7184759276015/1080 = 0.816001 | (1864.97, 198.72) |

⚠️ 上面这些 px 与 EW4 报告 ④·b 那张「逐颗现算」表**逐行吻合**（我这边是脚本按 float32 独立算的、不是抄它的）—— 两处互为旁证。

### 1·c `HudTextScale`：**删了** ✅（消费者归零之后才删）

- 删前现核：`grep -rn "HudTextScale" --include=*.cs Unity/MyGame/Assets` ⇒ **只剩注释**
  （`BattleDriver.cs` 5 处留痕 · `HudPiece.cs` 3 处说明）—— **一条代码引用都没有**（本件把 `BattleScene.cs` 最后两条删掉之后）。
- 删的是 `Core/LayoutSpace.cs` 的**本体 + 它的整段 doc**（旧 `:48-75`，28 行），原位留了一小段**留痕注释**
  （「原来是什么 / 为什么能删 / ⛔ 别建回来」，判据 = 铁律 5「保留更正痕迹」＋ `T09` §五 第 5 步「同批退掉」）。
  ⛔ **没有静默留着 API** —— 留痕里没有 `public static float HudTextScale` 这句声明（断言过）。
- 删之前/之后各跑一次 `typecheck`：**0/0 · 0/0**。

### 1·d 🔴🔴 一条**必须报**的（未被点名 ⇒ 按硬要求**没改**，见 ③/⑤）

**EW4 把字族迁到画布，把 `BattleScene.cs` 里 [靠 `hudRoot.Find` 找字] 的两组断言整组打断了 —— 20 条前提**必红**、另有 148 条**静默跳过**：**

| 机制 | 位点 | 后果 |
|---|---|---|
| `CheckHudBox`（助手，`:473` 那句 `hd.hudRoot.Find(name)`） | 16 个调用点：`:14436` `TitleText_Me` · `:14438` `TitleText_Foe` · `:14466`…`:14492` 那 14 条（`EnemyPlateText`…`QPText_Foe`） | **16 条前提红**（`lb == null`）；`if (lb == null \|\| tmp == null) return;` ⇒ 每颗**后面那 9 条整组不跑** = **144 条静默消失** |
| `hudNoBox` 那圈（`:14507` 名单 · `:14552` 那句 `drv.hudRoot.Find(n)`） | 4 个名字：`TurnLabel` `TurnClock` `ResultLabel` `HandLabel` | **4 条前提红** + **4 条静默消失** |

**机制（本件现核，不猜）**：`Hud()` 现在把字建在**画布**上（`BattleDriver.cs:16320-16333`：`cv = root as RectTransform`，
传进来的是裸 `Transform` 的 `hudRoot` ⇒ `cv = HudCanvas.Ensure(cam)`），而根画布是 `HudCanvas.cs` `Ensure()` 里
`new GameObject("BattleHud Canvas")` **没有任何 `SetParent`** ⇒ **根对象**；`HudCanvas.cs` 文件头第 2 条也明文写着
「⛔ 根 canvas 不能挂在 HUD 根下 ⇒ 建成根对象」。
⇒ `hudRoot.Find(name)` 对**这 20 颗字**恒 `null`（`Transform.Find` **不递归**，原来能中是因为旧路 `Label.Create` 直接挂 `hudRoot`）。
🔴 这不是「可能红」，是**必然红**；而且它**不是本件改出来的**，是 EW4 那一批的连带（EW4 不跑 Unity ⇒ 看不见）。
🔴 **本件为什么没顺手修**：它**没被 EW4 点名**，而任务书硬要求「发现某条必须改但没被点名 ⇒ 写进报告 ③/⑤」（详见 ③·a 与 ⑤·1 给了逐行改法）。

---

## ② 改动清单（逐处 `文件:行号` · 改前 → 改后 · 行尾核对 · numstat）

### 2·a `Editor/BattleScene.cs`（**只有 4 个 hunk 是本件的**）

| # | 改后行号 | 改前 → 改后（一句话） | 本件 hunk（`git diff -U0` 头） |
|---|---|---|---|
| A | `:1903-1975` | 删 9 行（两条 `HudTextScale` 断言 + 那个 `{}`）→ 加 73 行（口径注释 + 颗数 + 鉴别力前提 + 两张期望表 + 逐颗两 Check 的循环） | `@@ -1857,4 +1903,17 @@` + `@@ -1862,3 +1921,54 @@`（净 **+64**） |
| B | `:9424-9429` | `driver.HintWidth < LayoutSpace.VisibleWidth * 0.9f` → `< 1920f * 0.9f`（消息同步换 px）；+4 行口径注释 | `@@ -9193,2 +9424,6 @@`（净 **+4**） |
| C | `:2092-2104` | `drv.PileLabelWorldW < drv.MyDeckSizePlateWorldH * (442f/112f) - 0.05f` → `< 238.5f - 0.5f`；下限 `> 0.01f` → `> 1f`；+10 行口径注释 | `@@ -1982,3 +2092,13 @@`（净 **+10**） |
| D | `:20251-20261` | `<= LayoutSpace.Px(234f)` / `Px(64f)` → `<= 234f` / `<= 64f`（含消息里那两处）；+5 行口径注释 | `@@ -19917,0 +20251,5 @@` + `@@ -19919,4 +20257,4 @@`（净 **+5**） |

**本件对 `BattleScene.cs` 的净增行 = +83**（= 64 + 4 + 10 + 5）—— 与**行尾计数**独立对得上（见 2·c）。

### 2·b `Core/LayoutSpace.cs`

| 改后行号 | 改前 → 改后 |
|---|---|
| `:48-71` | 旧 `:48-75`：`HudTextScale` 的**整段 doc（19 行）** + **属性体（9 行）** → **24 行留痕注释**（删了什么 / 为什么能删 / 为什么别建回来）；**属性声明与 getter 整个不存在了** |

### 2·c 行尾核对（**二进制读**，`io.open(p,'rb')`，改完**逐文件**对）

```
MyGame/Assets/CardPresentation/Editor/BattleScene.cs   CRLF=25245  LF=25245  loneLF=0   ⇒ 纯 CRLF，未翻 ✓
MyGame/Assets/CardPresentation/Core/LayoutSpace.cs     CRLF=0      LF=304    loneLF=304 ⇒ 纯 LF，未翻 ✓
```
（改前：`BattleScene.cs` CRLF=25162 / `LayoutSpace.cs` LF=307。`BattleScene.cs` 的 +83 与 2·a 那张表逐条相加吻合 ⇒ **没有别的东西混进本件的改动**。）

### 2·d 逐文件 `git diff --numstat`

```
25	28	Unity/MyGame/Assets/CardPresentation/Core/LayoutSpace.cs
677	76	Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
```
🔴 **`BattleScene.cs` 那个 677/76 里只有 4 个 hunk 是本件的** —— 该文件在本件**开工之前就已经是 `M`**（会话开头的 `git status` 里挂着，工作区里压着**别的写手**的未提交改动）。
本件改完 A 那一处时测得 `--numstat` = **649/67**，而 A 那一处本身 = **+71 / −7**（见 2·a 的两个 hunk 头）⇒ **开工前的基数是 ≈ 578 / 60**，与 `LayoutSpace.cs` 的 25/28 无关。
本件对 `BattleScene.cs` 的总贡献 = 2·a 那 4 个 hunk = **+101 / −18**（净 **+83**）。
`LayoutSpace.cs` 那 25/28 **全是本件的**（该文件本件开工前不是 `M`）。

---

## ③ 接线清单（**白名单外**谁该改哪一行）

### 3·a 🔴🔴 `Editor/BattleScene.cs` 里 **EW4 迁移打断的那两组**（**本件没改**，见 ⑤·1）

> 这**仍然在 `BattleScene.cs` 里**，但**不在本件的授权范围**（任务书：只改 EW4 点名的那几处 + 它建议新增的那组）。
> 逐行改法（三条，都在同一处）：

1. **`:473`（`CheckHudBox` 的查找）**：`hd.hudRoot.Find(name)` → **改查画布**。
   现场可用的读法：`HudCanvas.RootRect.Find(name)`（**这 20 颗字都是根画布或它的子画布的直系子件 —— 见 ④ 的最后一条「已验证的结构事实」**）；
   ⚠️ `_hudLabels` 是 `private`，`BattleDriver` 没给「按名字取标签」的口 ⇒ 要么走 `RootRect.Find`，要么让 `BattleDriver` 补一个口（**那是它的白名单**）。
2. **`:485` / `:487`（`CheckHudBox` 的框宽/框高）**：`LayoutSpace.Px(bw)` / `LayoutSpace.Px(bh)` → **`bw` / `bh` 原样（设计 px）**
   —— 判据：`Hud()` 现在把 `boxWpx/boxHpx` **原样**喂给 `SetAutoFitBox`（EW4 `②·a`），而 TMP 子节点的 `sizeDelta` 在画布档**单位就是设计 px**。
3. **`:494` / `:496` / `:502`（`CheckHudBox` 的三个字号）**：静态 `Label.FontSizeToPx(...)`（**世界档口径**）→ **实例口 `lb.FontSizeToPxNow(face)`**
   —— 判据：EW4 `③·a` 最后一行（用静态那条会「拿世界尺量 px 值」**恒差 108 倍**；`EW4` 已把实例口在画布档补齐 ×10）。
   ⚠️ 那条 `MeasureBoundLimit` 的修正是它的前提（EW4 `②·b` 已修）。
4. **`:14552`（`hudNoBox` 那圈）**：同 1（`drv.hudRoot.Find(n)` → `HudCanvas.RootRect.Find(n)`）。
   ⚠️ 那 4 个名字（`TurnLabel` / `TurnClock` / `ResultLabel` / `HandLabel`）里**只有 `ResultLabel` 的期望锚点是 (0.5,0.5)** ——
   假如只按「锚点」判迁移，这一颗会**单独蒙对**；所以 1·b 那 21 条**必须**同时有**父链**那一条（本件已按这个写）。

### 3·b `Battle/BattleDriver.cs`（**不在本件白名单**）

| 处 | 该改什么 | 判据 |
|---|---|---|
| `:16186-16190` `HudLabelScaleForTest(int)` | **现在是死口**（本件把 `BattleScene.cs` 那最后一个调用点删了）：`grep -rn "HudLabelScaleForTest" --include=*.cs` ⇒ 只剩**它自己的声明 + 两处注释**。⇒ 连它那段 doc 一起删（它自己也写着「只为编得过」） | EW4 `②·a` 那张表最后一行 + 本件 `①·c` 的现核 |
| `:16141-16146` `_hudLabels` 的 doc | ⚠️ **别跟着一起删** —— 另外两个新口（`HudLabelCount` / `HudLabelAnchorForTest`）还在用它 | 同上 |

### 3·c 图族那一半（**只报不改**，见 ⑥·1）

| 处 | 该改什么 | 判据 |
|---|---|---|
| `Editor/BattleScene.cs:15100-15103`（`Quad()` 助手，被 `:15129` / `:15151` / `:15167` / `:15181` 四处用） | 它 `hudRoot.Find(nm).GetComponent<ImageQuad>()` 找 `AvatarImage_Me/Foe` · `AvatarItemSmall_Me/Foe` —— 这四件是 `HudAbs`/`HudAbsTex` 建的 ⇒ 已在**画布**上、而且那份件是 `HudPiece(=_rt + _img)`、**GO 上根本没有 `ImageQuad` 组件** ⇒ `Quad()` **恒 null** ⇒ 该节约 8 条红 | `HudAbsTex → HudImageTex`（`BattleDriver.cs:16454`）+ `HudPiece` 是**普通类**（`Battle/HudPiece.cs` 类声明，非 `MonoBehaviour`） |
| `Editor/BattleScene.cs:14686-14690`（`HudQuad(...)`） | ✅ **这一处是好的**（不用改）：`Aspect Ratio Filler{Top,Bottom}` 走 `ImageQuad.Create(root=hudRoot, …)`，**没迁**（`BattleDriver.cs:15698` 那句注释「这一件**没迁**」） | 现读 `BuildOneAspectFiller` |

### 3·d 别处（EW4 已记、仍然成立，本件**复核过）**

| 处 | 状态 |
|---|---|
| `Battle/WaitBanner.cs:280` | **仍是 `Label.Create`**（世界档）—— 属**面板族**、整扇还没迁；HUD 上仍有这 1 处世界档的字。本件**没动**（现核，与 EW4 `③·c` 一致） |
| `Battle/AttackSelector.cs:642/646/664` · `Battle/ChatPopupPanel.cs:378` · `Battle/ErrorMessageBanner.cs:547/636` | 同样仍是世界档 `Label.Create`（各面板自己的口，不是 `Hud()`）—— **不是**本件范围，如实登记 |

---

## ④ 判据与出处（**每条断言：期望值的来源 + 在什么条件下会红**）

### 4·a `BattleScene.cs:1903-1975`（本件新增的那 44 条）

| 断言 | 期望值来源 | **在什么条件下会红** |
|---|---|---|
| `HudLabelCount == 21` | **原版那一族的件数**：16 颗带框（`CheckHudBox` 那 16 个名）+ 4 颗只有 `scale`（`hudNoBox` 名单）+ 提示条那颗 `TipText_Text`。**判据不是「我们建了几颗」，是「原版 HUD 上非卡非面板的静态字件」那份清单** —— 清单出处 = `BattleDriver.BuildHud` 的 21 个 `Hud(...)` 调用点（每一处都在注释里写着原版节点路径），逐条与 EW4 报告 ①·a 的表核过 | **双向**红的：少建一颗（某处 `Hud()` 早退：`cam == null` / `Label.CreateUgui` 拿不到 TMP 字体 / `root` 与 `cam` 都取不到画布）⇒ 红；**多调用一次 `Hud(...)`（22 颗）也红**（`==` 不是 `>=`）。⚠️ `TipText` 是 `SetTipVisible(false)` 出厂的 ⇒ **不参与可见性**，只参与建件（那一格别误以为「没显示 = 没建」） |
| 鉴别力前提（`RootRect.parent != hudRoot` 等 3 项） | **结构事实**：`HudCanvas.Ensure()` 里 `new GameObject("BattleHud Canvas")` 无 `SetParent` ⇒ 根对象；`HudCanvas.cs` 文件头第 2 条明文「⛔ 根 canvas 不能挂在 HUD 根下」 | 有人把根画布塞进 `hudRoot` 下（或把 `RootRect` 改成别的对象）⇒ 红。**它红的含义不是「回退了」，是「下面那 21 条失去了鉴别力」** —— 那时要重挑一条判据 |
| `HudLabelOnCanvasForTest(i)` ×21 | **不取期望值，断的是结构**：父件 = 根画布**或**根画布的子画布（`HudCanvas.RootRect` / `p.parent == RootRect`） | 把 `Hud()` 改回世界档 `Label.Create(root=hudRoot, …)` ⇒ **21 条全红**；单独把某一颗挪出去 ⇒ 那一颗红 |
| `HudLabelAnchorForTest(i)` ×21 | **本文件自带的 `hudWantX/Y`（第二份原版字面量）**，出处 = `BattleDriver.BuildHud` 各调用点的**实参原文**（那些实参本身 = 原版 prefab 绝对屏幕 rect 折归一化；绝对矩形与出处写在那边注释里）。⛔ **不读** `BattleDriver.SkullScoreX01` / `MyFaithX01` / `EndTurnX01` / `mySizeX` … 那几条常量（读它 = 自证） | ① 任何一颗的 `x01/y01` 漂了（> **1e-6** 归一化 ≈ 0.002 px）⇒ 那一颗红；② `Hud()` 不再写 `anchorMin/anchorMax`（改回写 `localPosition`）⇒ **20 颗红、只有 `ResultLabel` (0.5,0.5) 会蒙对**（这正是要配父链那一条的原因） |

**容差取 1e-6 的理由**（不是拍脑袋）：两次独立算法（C# 编译期常量折叠 vs 我这边 float32）在**同一组字面量**上可能有
最后一位的差异；1e-6 归一化 = **0.0019 px**，比任何真实判据差都小两个数量级以上
（这张表里最接近的一对 = `EndTurnButton` 0.578190 vs `TurnClock` 0.599190，差 **0.021**；不同格子差 ≥ 1e-4）。

### 4·b `BattleScene.cs:9428`（提示条宽度）

- **期望值来源**：**原版门槛 = 屏宽的 90%**，屏（画布）宽 = `CanvasScaler` 的参考宽 **1920**（判据 → `HudCanvas.ReferenceResolution`，原版字段）。`1920 × 0.9 = 1728`。
  旧那条写的 `LayoutSpace.VisibleWidth(17.7778 世界单位) × 0.9 = 16.0` **换算到 px 就是 1728**（16 × 108 = 1728）⇒ **同一个门槛，只是换把尺**。
- **在什么条件下会红**：① `HintWidth ≤ 0`（提示条那一格没量到 —— 它读的是 `TickTipBanner` 里**趁激活时**缓存的那一量，判据 → `_tipW` 的 doc）；② 量到的宽 **≥ 1728 px**（提示条那行字横着长出屏 —— `Label` 是 **NoWrap**，卡组名是玩家自起的、长度不可控，这条就是挡它）。
- ⚠️ 它**不会**因为「提示条那颗字迁到画布」而红（迁前后都成立，只是量纲换了）—— 换口径是**必须**的，否则左边是几百 px、右边 16.0。

### 4·c `BattleScene.cs:2102`（牌库张数文字 vs 底板）

- **期望值来源**：**原版那块底板自己的宽 = `MyDeckSizeW` = 238.5 px**（原版那格 `m_PreserveAspect = 0`、**拉伸填满** `238.5 × 59.11`；出处 = `BattleDriver.MyDeckSizeW/MyDeckSizePx` 的声明的注释 + `Check` 上面那段注释）。本件写**字面量** `238.5f`（⛔ 不读那条 `const`）。
- **在什么条件下会红**：① `PileLabelWorldW ≤ 1f`（**没量到**：TMP 在对象没激活时量不出尺寸 ⇒ `RefreshBounds` 把它夹到 `1e-4`；原门槛 `0.01` 世界单位 ≈ **1 px**，等值换算）；② **≥ 238.0 px**（文字宽过底板 —— 框/字号改坏时就是这一条）。
- ✅ **它现在两边同尺**（都是设计 px）；⛔ 右边**不许**再出现 `MyDeckSizePlateWorldH`（那个走 `HudPiece.WorldH` = `GetWorldCorners`，**仍是世界单位**，≈0.5473）。
  🔴 **这条是本件四处里唯一「改法有分叉」的一处**：EW4 给了两个改法，本件选了**改右边**（写 `238.5f` 字面量），因为**改左边要在 `BattleDriver` 加读口**（越白名单）。若调度台更想要「左边也钉住底板 px」，那要**另起一笔**给 `BattleDriver` 加口。

### 4·d `BattleScene.cs:20256-20261`（手牌数框）

- **期望值来源**：**原版 `CardsInHandText` 的框 = 2.34 × 0.64 局域单位**，经那条**单位桥**（父链 `HandArea` 是纯 `Transform` + `m_LocalScale = 108`、自己 `0.925926` ⇒ 1 局域 = **100 px**）换算 ⇒ **234 × 64 px**。
  本件写的是 **`234f`/`64f`**（与 `BattleDriver.HandLabelBoxW/H` 那两个常量**同值、不同份** —— 那是有意保留的第二份，判据 → 铁律「⛔ 别拿我们自己的常量当期望值」）。
- **在什么条件下会红**：`HandLabelWorldW > 234` 或 `HandLabelWorldH > 64`（**框没接住**：把 `Hud()` 里那句 `SetAutoFitBox` 删掉 ⇒ 落 `Label` 默认档、字号不受框约束 ⇒ 渲染尺寸出框）。
  ⚠️ 断的是「**渲染尺寸 ≤ 框**」而不是「字号 == X」—— 字号对而溢出时自检照样全绿（`AutoFitBox` 教训）。
  ⚠️ 读的前提 = 那一格**显出来之后**（TMP 在对象没激活时量不出尺寸，本仓已知的坑）—— 本件没动这个前提，它在**下面那条 `SetCardHoldForTest(true)`** 之后。

### 4·e 本件**核过、判定「不用改」**的（省下下一轮的重查）

| 处 | 核查结论（判据） |
|---|---|
| `Label.Contains(Vector3 world)`（END TURN 的命中区，`BattleDriver.cs:8100`） | ✅ **两档都自洽**：`transform.InverseTransformPoint(world)` 给的是**本节点局部坐标**（画布档 = 画布单位 = 设计 px；世界档 = 世界单位），而 `WorldW/WorldH` = `_tmp.textBounds` 也是**同一个局部空间**的量 ⇒ 两边同一把尺，**迁移不改语义**。相机是正交（`LayoutSpace.Apply` 写死 `orthographic = true`）⇒ 画布挪到 `z = 0.3` 也不改变世界指针的 x/y |
| `HudCanvas.Add`（`BattleDriver.Hud()` 里那句） | ✅ **不动锚点**：`Group.Add` 只 `SetSiblingIndex`（`HudCanvas.cs:395-403`）；`Label.cs` 写 `anchorMin/Max` **只有一处**（`:170` 的默认 `(0.5,0.5)`）在 `CreateCore` 里，之后没人再写 ⇒ 本件那 21 条锚点断言量的是**稳定值** |
| `EnsureSubCanvas(null, "AboveShader Canvas", …)` | ✅ 子画布挂在 `RootRect` **直下**（`HudCanvas.cs:511` `SetParent(parent != null ? parent : RootRect)`）⇒ `_tipText` 那颗（父件 = 子画布）在 `HudLabelOnCanvasForTest` 的 `p.parent == RootRect` 那一支里**成立**（不是漏判） |
| `BattleScene.cs:16724` 那句 `LayoutSpace.Px(wPx)` | ✅ **只在「非画布件」那一支**（同一助手 `:16709` 先判 `GetComponentInParent<Canvas>() != null`，画布件走另一支、直接比 px）⇒ **不是**漏改 |

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **没改「EW4 迁移打断的那两组断言」**（`CheckHudBox` 16 条 + `hudNoBox` 4 条，见 ①·d / ③·a）。
   **理由 = 任务书硬要求**（「只改 EW4 点名的那几处 + 它建议新增的那组；发现某条必须改但没被点名 ⇒ 写进报告 ③/⑤」，
   并明确「⛔ 别顺手全量换口径（本文件 2600+ 条 `Check(`）」）。逐行改法已写在 ③·a（4 条），**判据都齐、只差动手**。
   ⇒ 🔴 **建议调度台把它排成独立一笔**（同一文件、同一批口径，但它是**另一组断言**）。
   ⚠️ 本波不跑 `*.Run` ⇒ 我没法告诉你它们今天是不是「全绿」（**结构上不可能是绿的**，见 ①·d 的机制）。
2. **没跑任何 `*.Run`**（现行口径 + 本件授权）：⇒ **本件新增的那 44 条一条都没被执行过**。
   我能保证的只有：**typecheck 0/0**（编得过）+ **静态推导**（期望值与实现同式、float32 逐位核过）。
   **必须真跑一次才算数**（`BattleScene.Run`）。
3. **没进真机 / 没截图**：⇒「那 21 颗字画出来在不在**对的位置**」只有静态推导（EW4 的 16:9 逐位自证 + 本件的锚点表）。
   ⚠️ 本件**没有**回答「4:3 / 21:9 下图与字是不是真的同尺」—— 那是 EW4 ④·b 末段的承诺，**要靠真机三档验**。
4. **`HudLabelScaleForTest` 还留着**（死口）：`BattleDriver.cs` **不在本件白名单** ⇒ 没删（接线 → ③·b）。
5. **`WaitBanner` 那颗字仍在世界档**：**如实登记，本件不动**（面板族、整扇还没迁；判据 → EW4 `③·c`）。
6. **图族那一半的 `hudRoot.Find` 我**只核了机制与位点**、**没有逐条跑**（③·c / ⑥·1）——
   「该节约 8 条红」这个**数字是估的**（按 4 个 `foreach` × 2 件数的），**机制是核过的**、**逐条红绿没验**。
7. **`LayoutSpace.Px` 在别处还有 13 处**（`BattleScene.cs` 现核）：我逐处看过，只有 `:485/:487`（→ ③·a）与 `:20174/:20175/:20177`（本件已改）是**指向已迁件的**；其余 11 处都是**世界档探针 / 面板 / 已分支处理**（4·e 那张表）。⚠️ 我**只看了 `BattleScene.cs`**，**别的文件没扫**（例如 `Editor/` 下另外几个宿主）。
8. ⚠️ **收尾时类型检查里那 1 个运行时错误不是本件的**（如实记，免得被误归因）：本件的两次关键检查（`BattleScene.cs` 四处改完 · 删 `HudTextScale` 之后）都是 **0/0**；
   但**收尾复跑**时出现 `Assets\CardPresentation\Battle\WaitBanner.cs` 的编译错，而且**两次报的错还不一样**
   （先 `(247,22) CS0029 ImageQuad → HudPiece`，75 秒后又变成 `(598,22) CS0102 类型 WaitBanner 已经包含 FillPpuMul 的定义`）
   ⇒ 那是**另一个写手正在改 `WaitBanner.cs`**（面板族那一扇，本件白名单外、任务书也点名「不许碰」）的**半成品状态**。
   ✅ 与本件无关的判据：① 报错文件是 `WaitBanner.cs`（本件一个字没碰）；② **编辑器程序集 0**（本件改的 `BattleScene.cs` 就在编辑器程序集里，编得过）；③ 全仓 `grep HudTextScale --include=*.cs` 零代码引用（删它没留下任何悬空引用）。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **图族那一半有同一个病**（**不是本件造成**，是 `T09` 批 3 的连带）：`BattleScene.cs:15100` 的 `Quad()` 助手
   `hudRoot.Find(nm)` + `GetComponent<ImageQuad>()`，而 `AvatarImage_Me/Foe` · `AvatarItemSmall_Me/Foe` 这四件
   走 `HudAbs`/`HudAbsTex` → `HudImageTex` → `HudCanvas.MakeAnchored`（**画布上**），且那份件是 `HudPiece`
   （**普通类**，GO 上是 `Image + RectTransform`）⇒ `Quad()` **恒 null**。
   ⚠️ 后果三样（`:15109` 那句 `"节点或 ImageQuad 不在"` / `QH()` 返 **0f** ⇒ `bh > 1e-3f` 挡下 / `QZ()` 返 **NaN** ⇒ 比较恒假）
   ⇒ **该节直接红**。🔴 **`G2_BattleScene收口`（22:13，晚于批 3）与 `_收口台账` 里都没有它**（我 grep 过 `hudRoot.Find` / `CheckHudBox` / `A1502`，零命中）⇒ **这是一条还没入账的账**。
2. ⚠️ **同一个坑在 BattleScene.cs 里一共 4 处 `hudRoot.Find`**：`:473`（字族 · 见 ①·d）· `:14552`（字族 · 同）·
   `:14686`（**好的**，图族那两枚没迁的黑边 quad）· `:15102`（图族 · 见上）。⇒ 下次迁任何一族，**`grep hudRoot.Find` 是最好的抓手**。
3. ⚠️ **过期注释一处**（不影响行为，未改）：`BattleScene.cs:15098-15099` 那句「这两个 quad 挂在 `hudRoot` 的**直接子件**上
   （`HudAbsTex` → `HudImageTex` → `ImageQuad.Create(root, …)`）」—— **`HudImageTex` 早就不建 `ImageQuad` 了**（现在是 `HudCanvas.MakeAnchored`）。
   同一族：`BattleDriver.cs:6652`（EW4 已记）、`:15549`（EW4 已记）。
4. ✅ **`Label.Contains` 那条读法在迁到画布之后仍然对**（见 4·e 第一行）—— **记一笔免得下一轮有人「顺手修」它**。
   ⚠️ 它的**名字**（`Contains(Vector3 world)` / `WorldW` / `WorldH`）现在**两档都叫 World**、而画布档量的是**设计 px**
   —— 这是本会话「0.75 倍 / 两把尺」困惑的**总源头**（任务书原话）。本件**没有**改名（改名 = 动共用件 + 全线调用点，**不在白名单**）。
5. ⚠️ **`BattleScene.cs` 里别的写手的未提交改动**（本件开工前 = 649/67）：本件**只碰了那 4 个 hunk**；
   若调度台要按 diff 归因，用 2·a 那张 hunk 表切。

---

## 可直接粘进总表的一行（≤3 行）

| **`EW6`** | **HUD 字族迁 uGUI · 断言收尾 + `HudTextScale` 退场**（`T09` §五 第 5 步收尾） | ✅ **EW4 点名的四处全改**（`:1903-1975` 换口径 · `:9428` `HintWidth` 换 px · `:2102` 张数文字换 px · `:20256` 手牌框换 px）· ✅ **新增 44 条**（21 父链 + 21 自备期望锚点 + 颗数 + 鉴别力前提；⛔ 不读实现常量）· ✅ **`LayoutSpace.HudTextScale` 已删**（消费者归零后删的，原位留痕说明「为什么能删 / 别建回来」）· `typecheck` **0/0 ×2** · ⛔ 未跑 Unity / 未真机 |
| | 🔴 **必须跟一笔**：EW4 的迁移把 `CheckHudBox`（16 条）+ `hudNoBox`（4 条）**打红** —— 它们靠 `hudRoot.Find` 找字，而字已在**根画布**下（根画布是根对象、⛔ 不在 `hudRoot` 下）⇒ **20 条前提必红 + 148 条静默不跑**；**本件按授权未改**，逐行改法（4 条）在 `EW6` 报告 ③·a。另：图族同病（`BattleScene.cs:15100` 的 `Quad()`，批 3 连带、**尚未入账**）见 ⑥·1 |
