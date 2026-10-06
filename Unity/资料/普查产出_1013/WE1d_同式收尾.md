# WE1d · 同式收尾（A684 CheckHAlign 14 处 + A685 + A686）

> 写手代理 · 2026-10-13 · **只动一个文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（+ 本报告）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读的 `git diff` / `git diff --numstat`）· ⛔ 没改正本
> ⛔ 没越白名单（`Battle/Label.cs` · `Shell/*` · `Core/*` **全部只读**；`Battle/Label.cs` 一个字没动）
> 📌 本报告的「现读」= **本件收工那一刻**的行号（改完：文件 **8981 → 9051** 行）。

---

## 一、结论

| 账 | 结果 | 一句话 |
|---|---|---|
| **A684**（`CheckHAlign` 自己就是手抄同一式） | ✅ **换口做完**（14 个调用点**一处也没漏**） | 量法从「节点世界 x ∓ 缓存 `WorldW`/2」（**手抄的 `RenderedRect` 式子**）换成 **`TmpRenderedRect`**（TMP 自己的网格）；**期望值、容差（`2f` px）、三档可分辨的语义一字未动**（§二表逐处给证） |
| **A685**（卡名断言引用了实现常量） | ✅ **改完**（2 行式子/文案 + 1 段 15 行注释） | `PracticeModePopup.DlCellW` → **字面量 `231f`**（原版 `GridLayoutGroup` 的格宽）——**与同节 §A218 那处（`:2067` 现读 `:2090`）写法统一** |
| **A686**（`WorldW` 没乘 `lossyScale`） | ✅ **如实记 · 一行代码没改** | 今天外壳父链单位缩放 ⇒ 两条量法**等价**；改了今天反而错 ⇒ 只写报告 + 给出要落进 `资料/已知的坑.md` 的那句话（§五） |
| 类型检查 | ✅ **0 / 0**（跑 3 趟：开工基线 + 改完 + 收尾注释改完） | `TMPDIR=/tmp/wf_we1d bash d:/4/Unity/工具/typecheck.sh` |
| 行尾 | ✅ **没翻** | 二进制读：改前 `0 CRLF / 8981 LF` ⇒ 改后 `0 CRLF / **9051** LF`（全程只用 **Edit** 工具，⛔ 无 `sed -i`） |
| 净增行 | **+78 / −8**（净 **+70**，8981 → 9051） | 与 `git diff --numstat` 对得上（852−774 = 78 · 58−50 = 8，基线 = W-E1 `550/16` + WE1b `646/27` + WE1c `774/50`） |

**三件都办到，没有一件跳过**；判不了的、没做的全部落在 §七（没改期望值、没硬做）。

---

## 二、A684 逐处表（**14 处**，一处不漏）

> 📌 **调用点行号**：本条简报给的 `:6001–6069` 是 **A685 改之前**的行号；本件在 `:1939` 那段加了 15 行注释 ⇒ 现读整体 **+15**（见下表「现读」列）。
> ⚠️ **先说口径（简报点名的那个坑）**：本函数**从头到尾都是【画布 px】**——`w = lb.WorldW * 108f`（px）·
> `cx = LayoutSpace.PxX(t.position.x)`（px）· 容差 `2f`（**px**）· 期望值 `x1/x2` 也是**原版 prefab 的 px 矩形**。
> **不是** WE1c 那个「世界单位口径」的情形 ⇒ **不需要折回世界**，两边同量纲直接比。**逐处复核过下表 14 行的期望值**，
> 全部落在 `0..1920` px 量纲内（最大 `1862.35`）。

| # | **现读**调用点 | 断的是 | `hAlign` | 期望 `x1` | 期望 `x2` | **改前期望值**（式子） | **改后期望值**（式子） | 容差 | **改前实测**（旧量法） | **改后实测**（新量法） | 实现侧那一句（判据） |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `:6016` | **中心**（cx） | `2` Center | 369.63 | 610.31 | `(x1+x2)*0.5f` = **489.97** | **同左（逐字未动）** | `2f` px | `PxX(node.x)` | 网格 `(px1+px2)/2` | `Shell/AllianceMemberTab.cs:362-363`（`Toggle()` 内 `alignLeft: false` ⇒ **不挪节点**，停在 `MenuDraw.Local` 摆的框心）；调用点 `:289` |
| 2 | `:6019` | 中心（cx） | `2` | 642.08 | 882.76 | = **762.42** | 同上 | `2f` | 同上 | 同上 | 同 `:362-363`；调用点 `:290` |
| 3 | `:6023` | **左缘**（x1） | `1` Left | 598.41 | 1493.50 | `x1` = **598.41** | 同上 | `2f` | `PxX(node.x) − w/2` | 网格 `px1` | `:426-427`（`alignLeft: true`）→ `Shell/SocialWindow.cs:389` |
| 4 | `:6026` | 左缘 | `1` | 597.94 | 1504.44 | = **597.94** | 同上 | `2f` | 同上 | 同上 | `:430-431` → `SocialWindow.cs:389` |
| 5 | `:6030` | 左缘 | `1` | 1494.50 | 1851.80 | = **1494.50** | 同上 | `2f` | 同上 | 同上 | `:801-802`（`MsgRow`）→ `SocialWindow.cs:389` |
| 6 | `:6034` | 左缘 | `1` | 369.42 | 964.25 | = **369.42** | 同上 | `2f` | 同上 | 同上 | `:1089-1090` → `SocialWindow.cs:389` |
| 7 | `:6038` | **右缘**（x2） | `4` Right | 1408.66 | 1862.35 | `x2` = **1862.35** | 同上 | `2f` | `PxX(node.x) + w/2` | 网格 `px2` | `:1018-1019`（`alignLeft: false`）+ **`:1020`** `MenuDraw.AlignRight(lb, ei)` |
| 8 | `:6055` | 左缘 | `1` | 673.38 | 1099.96 | = **673.38** | 同上 | `2f` | 同上 | 同上 | `:1182-1183`（`RatingRow`）→ `SocialWindow.cs:389`；调用点 `:1001` |
| 9 | `:6062` | 左缘 | `1` | 673.38 | 1099.96 | = **673.38** | 同上 | `2f` | 同上 | 同上 | 同 `:1183`；调用点 `:1002` |
| 10 | `:6071` | **中心**（cx） | `2` | 372.19 | 416.71 | = **394.45** | 同上 | `2f` | `PxX(node.x)` | 网格中心 | `:1336-1337`（`alignLeft: false`） |
| 11 | `:6074` | 左缘 | `1` | 519.11 | 1064.42 | = **519.11** | 同上 | `2f` | 同上 | 同上 | `:1354-1355` → `SocialWindow.cs:389` |
| 12 | `:6077` | 左缘 | `1` | 519.11 | 865.19 | = **519.11** | 同上 | `2f` | 同上 | 同上 | `:1357-1358` → `SocialWindow.cs:389` |
| 13 | `:6081` | 右缘 | `4` | 979.85 | 1109.85 | = **1109.85** | 同上 | `2f` | 同上 | 同上 | `:1412-1413` + **`:1414`** `MenuDraw.AlignRight(lb, tr)`；调用点 `:1362` |
| 14 | `:6084` | 右缘 | `4` | 979.85 | 1109.85 | = **1109.85** | 同上 | `2f` | 同上 | 同上 | 同 `:1413-1414`；调用点 `:1363` |

**分类小计（14 = 8 + 3 + 3）**：`Left`(1) **8 处**（#3·4·5·6·8·9·11·12）· `Right`(4) **3 处**（#7·13·14）· `Center`(2) **3 处**（#1·2·10）。
**「一处不漏」的判据** = `grep -c 'CheckHAlign("'` ⇒ **14**（收工那一刻再数了一遍）。

### 2·1 「期望值与容差一位未动」的**证据**（不是嘴说）

`git diff -U0` 那三个 hunk 就是全部改动（`@@ -7633 +8356 @@` · `@@ -7634,0 +8358,10 @@` · `@@ -7661,3 +8407,37 @@` · `@@ -7672 +8452,3 @@`）：

```
-        // 「渲出来的边」= 节点世界 x（换成画布 px）∓ 字宽/2 —— 与 `RenderedRect` 同一式（这里只用得上 x）。
-        float cx = LayoutSpace.PxX(t.position.x);
-        float px1 = cx - w * 0.5f, px2 = cx + w * 0.5f;
+        …（注释 34 行）…
+        float px1, py1, px2, py2;
+        if (!TmpRenderedRect(t, out px1, out py1, out px2, out py2))
+        { CheckTrue(false, what + "（前提）这一颗底下没有 TMP 网格 ⇒ 「渲出来的边」量不到"); return; }
```

🔴 **这三行之外，`got` / `want` / `CheckTrue(...) <= 2f` 三句是【上下文行】（diff 里没被 +/− 碰过）**：

```csharp
float got  = hAlign == 1 ? px1 : (hAlign == 4 ? px2 : (px1 + px2) * 0.5f);   // ← 未动
float want = hAlign == 1 ?  x1 : (hAlign == 4 ?  x2 : ( x1 +  x2) * 0.5f);   // ← 未动（14 处共用的期望值算式）
CheckTrue(Mathf.Abs(got - want) <= 2f, …);                                   // ← 未动（容差 2f px）
```
⇒ **14 处的 `want` 全部由同一句生成**，那句**一个字没改**；`x1/x2` 那 28 个实参（= 原版 prefab 字面量）**也一并没改**
（它们不在 diff 里）。**唯一动过的一行文案**是文末那条**诊断**（§九·补注：`字宽` 后面加了「渲出来那块宽」，
**判绿红的那半句 `<= 2f` 与 `want` 都没动**）。

---

## 三、A684 换口说明（**为什么这就是「灭自证」**）

**一句话**：旧写法与实现**共用一个口**（`Label.WorldW` 那个缓存 —— `_tmpW`），新写法读的是 **TMP 自己的网格**（`textBounds`）。

|  | 旧 | 新 |
|---|---|---|
| 读的什么 | `Label.WorldW`（= `Label._tmpW` 缓存）+ **Label 节点**的 `transform.position.x` | `TMPro.TextMeshPro.textBounds`（`m_textInfo.characterInfo[]` 的字形布局）过 `localToWorldMatrix` |
| 这个口谁在用 | **实现**：`Battle/Label.cs:862-880` `AlignLeftOn/AlignRightOn` 正是拿 `WorldW` 把左/右缘钉到框边（`:869` `localPosition.x = worldLeftX − 父x + WorldW*0.5f`） | **实现一个字都没写进那里** |
| 中间那层 | **看不见** —— 位置项与宽度项在算式上互相抵消 ⇒ 恒等于喂进去的 `PxRect.x1/x2`（= 断「我们把左缘钉在哪」） | **看得见** —— `RefreshBounds`（`Battle/Label.cs:799-809`）那一下**摆位**（`_tmp.localPosition.x = −anchor.x*W − b.min.x`） |
| 「两边一起改回旧写法」 | **有落点**（两边都用 `_tmpW`） | **没有落点**：把实现改回去 ⇒ 网格跟着动 ⇒ 新量法照样报出来 |

**链条（静态推的，逐环有出处；为的是证明「今天逐位同值」）**：
`TmpFont.NewText`（`Core/TmpFont.cs:158`）把 TMP 建出来是 **Center 对齐**；`RefreshBounds` 令 `_tmpW = |b.size.x|` 并把
TMP 子节点摆到 `−anchor.x*_tmpW − b.min.x` ⇒ **网格那一块在 Label 节点局部空间恒为 `[−W/2, +W/2]`**
（与 TMP 内部对齐档无关 —— `− b.min.x` 那一下就是补偿）；而 `AlignLeftOn` 把节点摆到
`worldLeftX + W/2` ⇒ **网格左缘 ≡ `worldLeftX` ≡ `PxRect.x1`**。取 `anchor = (0.5, 0.5)`：
**Shell 侧建标签只有一条路**（`Shell/MenuDraw.cs:1557-1558` `Label.Create(..., new Vector2(0.5f, 0.5f), ...)`；
`MainMenuRuntime` / `MenuWindowBase` / `PracticeModePopup` / `ImportDeckPopup` / `ShellRuntime` 全传 `(0.5, 0.5)`）。
⇒ **新量法 ≡ 旧量法**（今天）**≡ 原版那条边**（14 处逐处如上表）。

**⛔ 没有**另立一条「两边一起改回去也红」的断言 —— 判断与 A490/A617/WE1c 那三处**一致**：
**换口本身就是灭自证**（两边不再共用一个口 ⇒ 没有「一起改回去」这个动作）。
**⛔ 也没写**「渲染宽 = 原版多少」那一格 —— 本地**没有**「原版这一段渲出来多宽」的判据
（`menu_dump` 给的是**布局框**），同 A490 那条如实标注。

---

## 四、A685 改动（`PracticeModePopup.DlCellW` → **`231f`**）

**位置**（现读）：式子 `:1960` · 文案 `:1962` · 新加注释块 `:1939-1953`（**改前 = `:1939-1949`**）。

```csharp
// 改前（:1946 / :1948）
if (wpx > PracticeModePopup.DlCellW + 0.5f) over++;
Check(over, 0, $"卡列表**每一行的卡名都放得进 {PracticeModePopup.DlCellW}px 的格**（最宽 {widest:F1}px；"
               + "判据是**原版格宽**，不是我们自己的常量 —— 第一版撞列就是这条没断）");
// 改后（:1960 / :1962-1964）
if (wpx > 231f + 0.5f) over++;
Check(over, 0, $"卡列表**每一行的卡名都放得进 231px 的格**（最宽 {widest:F1}px；"
               + "判据是**原版格宽** 231（**原版字面量**，⛔ 不读 `PracticeModePopup.DlCellW`），"
               + "不是我们自己的常量 —— 第一版撞列就是这条没断）");
```

- **为什么改**（写进注释了）：与本文件那条自订纪律打架（节头现读 **`:8542`**「断言用的**原版行几何**（判据源，独立于实现）」：
  「🔴 断言里⛔ 不许再引用实现常量 … ✅ 做法 = 把**原版值**在断言处**重抄一遍并注明出处**」；
  A667 那处 `:2030-2041` **同一天刚这么改过**，`:2033` 就是它引这条纪律的那一句）；
  而**这一条自己的文案就写着**「判据是**原版格宽**，不是我们自己的常量」——**同一个文件里两种写法并存**
  （同节 §A218 那处**早就是字面量**：现读 `:2082` —— `box(…, "卡格 CardRow_0", 231f, 27.88f, …)`）。
- **值本身是原版字面量**（不是我们挑的）：`231` = 原版卡列表那个 `GridLayoutGroup` 的格宽
  （出处 = `Shell/PracticeModePopup.cs:396` 的头注释：「格：**231×27.88**、spacing (22, 3.6)、pad (22, 0, 4, 0)
  （原版 `GridLayoutGroup` 字段，逐条抄）」）；同节 §A218 那处**早就是字面量**（现读 `:2090` `box(…, "卡格 `CardRow_0`", 231f, 27.88f, …)`）。
- **改坏法（有牙）**：把 `Shell/PracticeModePopup.cs` 的 `DlCellW` 改成别的数（例如 `300`）⇒ 卡名渲出来的宽**没变**
  ⇒ **这一条不再跟着它漂**（旧写法会跟着那个常量一起动、**永远绿**）。另一半（把 `BuildCardRows` 的起排 y 或
  格宽推坏 ⇒ `wpx` 超 `231.5` ⇒ 红）**新旧都红**，不算本件的增量。
- ⚠️ **量法一行没动**（`nl.WorldW * 108f`）：它读的是这一颗 `Label` 自己量出来的宽，与「期望值从哪来」是两件事。
  （该量法**是**缓存读 —— 见 §八·3 那条顺手发现，本件**没碰**。）
- ⚠️ **`DlCellW` 在本文件只有这两处引用**（`grep` 复核过：`:1946`/`:1948` 改前，其余命中全在**注释里**）
  ⇒ **没有第三处漏网**。

---

## 五、A686（**没改代码** · 为什么 · 建议落进坑库的那句话）

### 5·1 事实（本件现读复核过）

- `Battle/Label.cs:53`：`public float WorldW { get { if (_tmp == null) return _texW / PixelsPerUnit; EnsureMeasured(); return _tmpW; } }`
  —— `_tmpW` 是 **`Label` 节点【局部空间】**的量：`RefreshBounds`（`:803`/`:808-809`）拿它写 **TMP 子节点**的
  `localPosition`，**那两处必须同量纲**（同一个量既当「宽度」又当「摆位偏移」）。
- 而断言侧（`RenderedRect` 现读 `:285-302`，以及本件改之前的 `CheckHAlign`）把它当 **【世界】宽**用：
  `节点世界 x ∓ WorldW/2`。**两者只在父链 `lossyScale == 1` 时等价。**
- 今天为什么等价：外壳父链**单位缩放**（`WindowsManager.AttachToAnchor` 把窗根写成 `localScale = one`；
  小屏缩放器 `TransformScalerBySmallScreenUI` **出厂关**）—— 同文件 **`:2054`**（现读）那节注释已经点过这件事。
- **今天改它反而错**：新量法（`TmpRenderedRect`，走 `localToWorldMatrix`）**本来就把缩放算进去了** ⇒
  在「父链 = 1」的今天，给它乘 `lossyScale` 是**乘 1**（看不出差别），可一旦有人真的加了缩放，
  「乘 `lossyScale` 的旧路子」与「网格路子」**会给出两个不同的数**，而**对的那个是网格**。
  ⇒ 动 `WorldW` 的定义会**连带改掉** `AlignLeftOn`/`AlignRightOn`（它们与 `RefreshBounds` 共量纲，见上）
  ⇒ **本件一行没改**（也不在白名单里）。

### 5·2 建议落进 `资料/已知的坑.md` 的那句话（**请调度台落，我不写别人的文档**）

> **`Label.WorldW` 是「Label 节点局部空间」的量，不是世界长度。** `RefreshBounds`（`Battle/Label.cs:799-809`）
> 拿它写 **TMP 子节点**的 `localPosition`（宽度与摆位偏移**共用一个量纲**）；而断言侧的
> `节点世界 x ∓ WorldW/2`（`Editor/MainMenuScene.cs` 的 `RenderedRect:285-302`、以及 **A684 改之前**的 `CheckHAlign`）
> 把它当**世界**宽用 ⇒ **两条量法只在父链 `lossyScale == 1` 时等价**。
> 今天外壳父链是单位缩放（`WindowsManager.AttachToAnchor` 把窗根写 `localScale = one`；小屏缩放器出厂关）
> ⇒ 等价、看不见；**一旦有人给菜单/外壳某棵树加缩放**（A165 的 `TransformScalerBySmallScreenUI` 真开时，
> **窗根那一级**就是真 `localScale`，见 `Battle/Label.cs:843-849` 那条订正），
> **这两条量法就会分家 —— 那时只有「读 TMP 网格」那条（`TmpRenderedRect`，走 `localToWorldMatrix`）是对的**。
> 📌 同族的一条现成判据：`Label.ParentXInDesignSpace`（`Battle/Label.cs:942-975`）就是为了这个缩放
> 给「父节点位置项」补的（`worldX − P/k`）；**宽度项 `WorldW × 0.5` 没有除 k**（它是**局部**长度，本来就不该除）
> —— 所以「对齐」那条算式在 k≠1 时**是不是仍然把网格左缘钉在 `worldLeftX` 上，本地没有判据**（要真跑一档缩放才知道）。

---

## 六、改坏法总表（逐处点名「实现侧那一句」）

> 🔴 **先说一条会影响结论、本件现读查实的事**：**我们的 TMP 出厂是 `Center` 对齐**
> （`Core/TmpFont.cs:158` `t.alignment = TextAlignmentOptions.Center`），而这 **14 颗没有一处**调
> `Label.SetAlignLeft()`（全仓只有 `Battle/CardDisplayWindow.cs:228/263/272` · `Battle/UnitChatPanel.cs:309` ·
> `Editor/ChatBoxProbe.cs` 调）⇒ **重排之后那一块字自己重新居中**（`RefreshBounds` 的 `− b.min.x` 补偿
> **只在那一次**做过）⇒ **渲出来的左缘左移、右缘右移，各 `Δ/2`**（`Δ` = 新加进去的字距）。
> ⇒ 这一族改坏法**对 `Left`/`Right` 那 11 处有效**；**对 `Center` 那 3 处【不增检出】**（见下表末行，**如实标**）。

| # | 站 | **实现侧那一句**（在它**之后**插一句重排调用） | 新量法红？ | 旧量法 |
|---|---|---|---|---|
| 1·2 | 二级页签 `Button Text`（Center） | `Shell/AllianceMemberTab.cs:362-363`（`Toggle()`，`alignLeft: false`）—— 该处**没有对齐调用**，插在建标签之后即可 | ⚠️ **不增检出**（中心没动） | 同样不增 |
| 3·4 | `CurrentActiveBadge {Name,Count}>Text` | `Shell/AllianceMemberTab.cs:427` / `:431`（`alignLeft: true`）→ 共同口 **`Shell/SocialWindow.cs:389`** | ✅（左缘左移 `Δ/2`） | 照旧报原值 |
| 5 | `ChatPreview/…/Message Preview>text` | `Shell/AllianceMemberTab.cs:802` → `SocialWindow.cs:389` | ✅ | 同上 |
| 6 | `MemberList/members label>Text` | `Shell/AllianceMemberTab.cs:1090` → `SocialWindow.cs:389` | ✅ | 同上 |
| 7 | `Config fields/extra_info`（Right） | `Shell/AllianceMemberTab.cs:1020` `MenuDraw.AlignRight(lb, ei)` | ✅（右缘右移 `Δ/2`） | 同上 |
| 8·9 | `{Alliance,Draft} Rating Display/Individual rating value` | `Shell/AllianceMemberTab.cs:1183`（`RatingRow`）→ `SocialWindow.cs:389` | ✅ | 同上 |
| 10 | 成员行 `member index`（Center） | `Shell/AllianceMemberTab.cs:1337`（`alignLeft: false`） | ⚠️ **不增检出** | 同样不增 |
| 11 | 成员行 `member name` | `Shell/AllianceMemberTab.cs:1355` → `SocialWindow.cs:389` | ✅ | 同上 |
| 12 | 成员行 `member role` | `Shell/AllianceMemberTab.cs:1358` → `SocialWindow.cs:389` | ✅ | 同上 |
| 13·14 | 成员行 `{Draft,Ranked} Rating/…`（Right） | `Shell/AllianceMemberTab.cs:1414` `MenuDraw.AlignRight(lb, tr)` | ✅ | 同上 |

**🔴 三个「改坏法」的现成落点（都在 `Shell/`，⛔ 我不改，只点名）**：

1. **把 `Shell/SocialWindow.cs:388` 与 `:389` 对调**（把 `lb.SetWrapping(false)` 挪到 `MenuDraw.AlignLeft` **之后**
   —— 该文件 `:386-388` 那条注释正是禁止这件事：「必须在 `TextBox` **之后**、`AlignLeft` **之前**：
   `SetWrapping` 会重排并挪 TMP 子节点（`ForceRelayout`，**A205**）」）⇒ 这正是「重排排在 AlignLeft 之后」的**真身**。
2. **在 `AlignLeft/AlignRight` 之后加 `lb.SetCharSpacing(4f);`** —— `Battle/Label.cs:524-533` 只
   `_tmp.characterSpacing = v; ForceMeshUpdate();`、**不刷 `_tmpW`**（A475 在 `Shell/LiveOpsEventWindow.cs`
   就是这么躲过同款断言的）。
3. **`Center` 那 3 处**：把实现的 `alignLeft:` 翻过来（`false` → `true`）⇒ 节点横移 `|矩形宽 − 字宽|/2`
   ⇒ **新旧量法都红**（这条**不是**本换代的增量，如实标）。

**⚠️ 如实标（同 WE1b §4·4 / WE1c §五）：`Δ/2` 与容差 `2f` 谁大 —— 本地量不出来**
（`Δ` = 字距 × 字符数在该档字号下的实际 px；要跑起来或按 TMP 的 `characterSpacing → m_cSpacing` 换算链算才知道）
⇒ 若 `Δ/2 < 2px`，上面第 1/2 条改坏之后**仍可能是绿的**。

---

## 七、没查清 / 没做的（⛔ 不许猜）

1. ⚠️ **这 14 条断言从没在运行时跑过**（本件按简报**一次 Unity 都没跑**）⇒ **首跑就是它的第一次**。
   静态侧我把「改前改后逐位同值」的链条重新推了一遍（§三：`anchor=(0.5,0.5)` + `RefreshBounds` 的居中补偿
   ⇒ 网格边缘 ≡ 节点 ∓ `W/2`；`AlignLeftOn/AlignRightOn` 喂的正是同一条边）⇒ **预期不会红**。
   **红了先量实得值**，判「实现没对齐」还是「量法没生效（天文数字 / 全 0）」——⛔ **别直接改期望值**（代码注释里也写了）。
2. ⚠️ **`Δ/2` 与容差 `2f` 谁大 —— 判不了**（§六 末）。
3. ⚠️ **`TrophiesWindow` 那一棵在断言时是关着的**，新量法**依赖「它建的时候是激活的」**：
   本件现读查实 —— `AllianceMemberTab.Build()`（`Shell/AllianceMemberTab.cs:267-279`）**先**建 `TrophiesWindow`
   与里面的标签（`:275` `Node(...)` ⇒ `MenuDraw.Node` 建出来默认**激活**）、**最后**一句才 `ShowGeneral()`
   （`:388` `SetActive(false)`）⇒ **建成时激活过** ⇒ `textBounds` 有效（判据链同 WE1c §六·3：
   `OnDisable` 不碰 `m_textInfo`、`ForceMeshUpdate` 只在「当时就没激活」时早退）。
   ✅ **不是「没查清」，是查实了**；残余风险只剩一种（**某个 TMP 激活时从没渲过、之后才 `SetText`**），
   本件涉及的两个「关着的」现场（`TrophiesWindow` / act F 的 `GeneralDetails`）**都不是这一种**
   （act F 那棵是「建完才 `SetActive(false)`」，出处 = `Shell/AlliancesTab.cs:356-363` 的
   `BuildGeneralDetails()`：建完 `_detail` 之后最后一句才 `SetActive(false)`）。
4. ⚠️ **原版这 14 颗 TMP 的 `m_VerticalAlignment` 我没查**（同 WE1b §五·4）：本函数**只断横向**，
   纵向那一族**不是本账**；`TmpRenderedRect` 会**顺带**量出 `py1/py2`，但本件**没有**拿纵向去比任何东西
   （⛔ 不新增判据 —— 那要另立期望值）。
5. ⚠️ **`Center` 那 3 处「网格中心 = 节点 x」这一条，我是按 Center 对齐的几何推的**
   （单行/多行都成立：并集盒的左端由**最宽那行**决定 ⇒ 恒对称）；**没有实跑复核**。
   ⇒ 若将来发现它们在运行时红了，先怀疑这条静态推理（而不是先改期望值）。
6. ⚠️ **没有扫「别的形状」的手抄式**（同 WE1b §六·4 / WE1c §六·6）：本件只处理 `CheckHAlign` 这一个口
   （简报点名的），**没有**再全文扫一遍「先算 `RectCenter` 再拿它比实参」那一族。

---

## 八、顺手发现（⛔ **一个都没改**）

### 8·1 🔴 `CheckHAlign` 的「空串守卫」**门槛可能永远不成立**（静态推的，写进代码注释了）

`Editor/MainMenuScene.cs:8400` `if (Mathf.Abs(w) < 0.01f)`（`w = lb.WorldW * 108f`）自称抓「**空串**」，
但 **`Label.WorldW` 在空串上是 `4.29e9` 而不是 0**：空串时 TMP 的 `textBounds` 是「没有可见字符」的哨兵
（`PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:4864-4891` `GetTextBounds()`：
`extent` 停在 `k_LargePositiveVector2 / k_LargeNegativeVector2` ⇒ `size = −4.2949673e9`），
而 `RefreshBounds` 是 `_tmpW = Mathf.Max(Mathf.Abs(b.size.x), 1e-4f)`（`Battle/Label.cs:803`），
`Battle/Label.cs:977-985` 自己也写着「实测**空串**读到的是 **`4.29e9`**」⇒ **这一支进不去**，
「空串」会走下面那条、以**天文数字**判红（**也是红**，只是说不出「这一段断不了」那句话）。

- **影响**：**不影响本件的换口**（新量法在空串上同样给天文数字 ⇒ 同样红）；影响的是**文案/可诊断性**。
- **补丁形状**（一行，**不用动 `Battle/Label.cs`** —— `Label` 有公共读口 `Battle/Label.cs:79` `public string Text`）：
  把守卫改成 `if (string.IsNullOrEmpty(lb.Text)) { …原文案… }`（`< 0.01f` 那条保留为第二道兜底亦可）。
- ⚠️ **同族的两处旧话要一起复核**（都在**本文件**、都是别人的账）：`:8368-8372`（`CheckHAlign` 的 doc
  「本函数遇到空串【自己判红并说明】」）与 `:6050`/`:6054`（A485 那段块注释引同一句话）。
- ⛔ **本件没改**（不在 A684 的账上；且这是「两档都是红、只是文案不同」的事，应由调度台裁）。

### 8·2 ⚠️ `CheckHAlign` 的 doc 里那个「**3 处**」是**过期的数字**（同一文件里已经订正过）

`Editor/MainMenuScene.cs:8372`（现读）：「这正是 `Shell/AllianceMemberTab.cs` 对齐表里那 **3 处**
「写不出断言」的情形」。
而**同一个文件** `:5989` **白纸黑字订正过**：「🔴 **2026-10-12（A485）就地订正：
上面原来写的是「3 处」（铁律 5，保留更正痕迹）**。摘掉的那一处 = `{Alliance,Draft} Rating Display/
Individual rating value`（A392 之后那两格有 `'-------'` ⇒ 断得出东西）」⇒ **现在是 2 处**。
⇒ **一行注释的订正**（⛔ 本件没动：简报红线「顺手发现别自己改」）。

### 8·3 ℹ️ `CheckFits`（`Editor/MainMenuScene.cs:8534-8541`）读的也是**缓存**

```csharp
static void CheckFits(Transform t, float boxPx, string what)
{ … float w = lb.WorldW * 108f; CheckTrue(w <= boxPx + 1f, $"{what}（渲出 {w:F1}px ≤ 框 {boxPx:F1}px）"); }
```
它自称断的是「**渲染宽度**必须放得进框」—— 但 `WorldW` 是**缓存**（同 A617 #13 那条 `_tmpH` 风险），
**重排而没刷缓存时它报的是旧宽**。⛔ **本件没碰**（不在账上；且 `TmpRenderedRect` 现在就在同一个文件里，
真要收口是一行的事）。⚠️ 与 §8·1 是**同一族**（都是「缓存 ≠ 活值」），别当两件事。

### 8·4 ℹ️ `TmpRenderedRect` 会顺带量到纵向（`py1/py2`），本件**故意没用**

`CheckHAlign` 现在 `out px1, out py1, out px2, out py2` 里 `py1/py2` **没被用**（编译**无警告**，本地实测过）。
⛔ **不许**顺手把它们拿去比纵向（那要另立期望值 = 原版矩形中线，是另一笔账 —— 见 WE1b §五·4）。

---

## 九、类型检查结果

```
# ① 开工基线（改之前，同一 TMPDIR）
$ TMPDIR=/tmp/wf_we1d bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0

# ② A684 + A685 改完
运行时错误数: 0 · 编辑器错误数: 0

# ③ 收尾（注释再改两处之后）
运行时错误数: 0 · 编辑器错误数: 0
```

- **三趟全 `0 / 0`**；⛔ **全程没有出现**「错误集中在某个不是我负责的文件上」那种情形
  （不需要「隔一会儿重跑 + 如实记」那条处置 —— 但本批有别的写手在飞，**同步点那一刻要重跑一次**才算数）。
- **行尾**：二进制读 `0 CRLF / **9051** LF`（改前 `0 / 8981`）⇒ **纯 LF 未翻**；全程只用 **Edit** 工具，⛔ 没有 `sed -i`。
- **`git diff --numstat`**（收工那一刻）：`852  58  Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`
  （**相对 HEAD** —— 里面含 W-E1 的 `550/16`、WE1b 的 `646/27`、WE1c 的 `774/50`，⛔ **别当本件的改动量**）。
  ⇒ **本件真正加的是 +78 行 / 删 8 行（净 +70 行，8981 → 9051）** —— 两边的账对得上。
- **补注（诊断行）**：唯一动过的断言文案是文末诊断那一句 —— `字宽 {w:F2}` 后面**加**了
  「· **渲出来那块宽 {px2 - px1:F2}**」（并把原 `字宽` 标成「实现缓存 `Label.WorldW`，= 它对齐时用的那一档」）。
  **判绿红的那三句（`got` / `want` / `<= 2f`）逐字未动**（判据 = §2·1 的 `git diff -U0`）。
- ⛔ **没跑 Unity**（简报禁）· ⛔ **没动 git**（只跑了只读的 `git diff` / `git diff --numstat`）· ⛔ **没改正本** ·
  只碰白名单里那一个 `.cs` + 本报告（**`Battle/Label.cs` / `Shell/*` 一律只读**）。

---

*（报告完 · 写手代理 WE1d · 只写了本文件 + `Editor/MainMenuScene.cs`）*
