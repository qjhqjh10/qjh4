# D7 · MainMenuScene 4 条红诊断（只读 · 2026-10-11）

> 只读诊断代理。**未改任何文件**（本报告除外）· ⛔ 未动 git（只跑只读 `diff/log`）· ⛔ 未跑 Unity / 自检。
> 判据一律给 `文件:行号`。日志：`d:/4/_tmp_view/menu3.log`（本轮）· `d:/4/_tmp_view/menu.log`（上一轮）。

---

## 一、结论（4 条各归 (α|β|γ)）

| # | 日志行 | 节点 · 断的量 | 归属 |
|---|---|---|---|
| 1 | `menu3.log:2293`（重列 `:34094`） | `Background` · `rect.width` | **(β) 实现真缺陷** |
| 2 | `menu3.log:2308`（重列 `:34105`） | `Background` · `rect.height` | **(β) 实现真缺陷** |
| 3 | `menu3.log:2367`（重列 `:34116`） | **`Navigation Panel`** · `rect.height`（← 简报里要现读定位的那一条） | **(β) 实现真缺陷** |
| 4 | `menu3.log:3060`（重列 `:34127`） | `Upper bar` · `rect.width` | **(β) 实现真缺陷** |

**一句话根因**：A332 新收口的私有工厂 `Shell/MainMenuRuntime.cs:153` `New(parent, name, wPx, hPx)` 收的是**设计像素**（内部 `MenuDraw.SetPxSize` 再 ÷108 —— `Shell/MenuDraw.cs:155-162`），而 **3 个调用点把「世界单位」当 px 传了进去**：

| 文件:行 | 实际传的实参 | 它的真身 | 应为 |
|---|---|---|---|
| `Shell/MainMenuRuntime.cs:268` | `LayoutSpace.DesignWidth` | **10 × 16/9 = 17.7778 世界单位** | `1920f`（或 `LayoutSpace.DesignPxW`） |
| 同上 | `LayoutSpace.DesignHeight` | **10 世界单位** | `1080f`（或 `LayoutSpace.DesignPxH`） |
| `Shell/MainMenuRuntime.cs:306` | `LayoutSpace.DesignHeight` | 10 世界单位 | `1080f` |
| `Shell/MainMenuRuntime.cs:822` | `LayoutSpace.DesignWidth` | 17.7778 世界单位 | `1920f` |

⇒ 等于**把 1/108 那次换算做了两遍**。其余 16 个调用点全传 px 字面量（`:324 :374 :419 :827 :847 :849 :882 :899 :923 :955 :1040 :1075 :1077 :1082 :1152 :1242`），所以只有这 4 个轴红。

⛔ **不是 (α)**：断言表达式与它的期望值都对（见 §二·4 与 §六·3）。
⛔ **不是 (γ)（严格意义）**：这 3 处**没有「曾经对过的值被改坏」** —— A332 之前旧签名是 `New(parent, name)`（`git diff` 有 `-var go = New(root, "Background");`），那时这 3 颗节点**一个 `sizeDelta` 都没写**。这是**本批新写的代码在 3/19 处写错了值**；新断言**第一次**就把自家实现抓出来了（断言尽职，不是被带出来的回归）。

---

## 二、逐条（# / 日志行 / 断言在源码哪一行 / 表达式原文 / 两边的数 / 差在哪一步）

### 0. 断言长什么样（简报①的答案）

**宿主** = `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`，**WB2（A332）本批新开的那一节**：

- 节头：`Editor/MainMenuScene.cs:694` `Section("§A332 主菜单：空节点工厂的 `sizeDelta`（`rect` 的宽高 = 原版矩形）")`
- 探针 lambda：`:696`（`.Action<Transform, string, float, float, string> mbox`）
  - `:700` 前言「是 `RectTransform`」· `:702` 前言「`lossyScale.x == 1`」
  - **`:704` 宽** · **`:706` 高**
- 三次调用：**`:709`**（`Background`, 1920f, 1080f）· **`:711`**（`Navigation Panel`, 191f, 1080f）· **`:728`**（`Upper bar`, 1920f, 100f）

**表达式原文（`:704` / `:706`，两行同款）**：

```csharp
CheckNear(rt.rect.width,  LayoutSpace.Px(wPx), 0.01f,
          $"★ {what} 的 `rect.width` = **{rt.rect.width * 108f:F2}px**（原版 {wPx}px —— {src}）");
CheckNear(rt.rect.height, LayoutSpace.Px(hPx), 0.01f,
          $"★ …`rect.height` = **{rt.rect.height * 108f:F2}px**（原版 {hPx}px）");
```

⇒ **不是 `CheckRel`**，是 `CheckNear`（`Editor/MainMenuScene.cs:210`）：

```csharp
static void CheckNear(float got, float want, float tol, string msg)
    => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F3} ≈ {want:F3}±{tol:F3}）");
```

🔴 **公差是【绝对】误差、不是相对误差**：`Mathf.Abs(got − want) ≤ 0.01`（0.01 **世界单位 ≈ 1.08 px**）。日志括号里的 `0.165 ≈ 17.778±0.010` 是 `（实测 got ≈ 期望 want ± 容差）`。

**`0.165` / `0.093` / `10.000` / `17.778` 各是谁**（日志格式逐字对得上）：

| 数 | 身份 | 算式 |
|---|---|---|
| `17.778` | **期望值**（世界单位）= `LayoutSpace.Px(1920)` | `1920 ÷ (DesignPxH/DesignHeight)` = `1920 ÷ 108` |
| `0.165` | **实测值**（世界单位）= `rt.rect.width` | `= 17.7778 ÷ 108`（**被除了两次 108**） |
| `10.000` | 期望值 = `LayoutSpace.Px(1080)` | `1080 ÷ 108` |
| `0.093` | 实测值 = `rt.rect.height` | `= 10 ÷ 108`（同样两遍） |

`LayoutSpace.Px` 与两个常量（`Core/LayoutSpace.cs:26,29,31,113,116`）：

```csharp
public const float DesignHeight = 10f;             // 世界单位（可见高度）
public static float DesignWidth { get { return DesignHeight * DesignAspect; } }  // = 10 × 16/9 = 17.7778 世界
public const float DesignPxW = 1920f, DesignPxH = 1080f;   // ← 这两个才是「设计像素」
public static float Px(float px) { return px / (DesignPxH / DesignHeight); }     // = px / 108
```

🔴 **一句话就能看出真凶**：`Core/LayoutSpace.cs:14` 自己写着
「可见宽度 = `DesignHeight` × 宽高比（**16:9 → 17.78**，21:9 → 23.3，4:3 → 13.3）」
—— 红名单里打印出来的那个 `**17.78px**` **就是 `DesignWidth` 的字面值**。

### 1. `Background` · `rect.width`（`menu3.log:2293` / 重列 `:34094`）

- 断言：`Editor/MainMenuScene.cs:709-710`，`mbox(menu.Find("Background"), "整屏背景 \`Background\`", 1920f, 1080f, …)`
- 产生它的语句：`:704`（宽）→ 栈 `MainMenuScene.cs:704` ✓ 与日志栈逐字对上
- 实际值：`Shell/MainMenuRuntime.cs:268` `New(root, "Background", LayoutSpace.DesignWidth, LayoutSpace.DesignHeight)`
  ⇒ `SetPxSize` 收 `17.7778 世界` 当 px ⇒ `sizeDelta.x = 17.7778/108 = 0.164609`
- `Mathf.Abs(0.164609 − 17.77778) = 17.6131` > `0.01` ⇒ 红。**差在哪一步：第 268 行的实参量纲（应为 1920 px，实给 17.7778 世界）**

### 2. `Background` · `rect.height`（`menu3.log:2308` / 重列 `:34105`）

- `:706`（高）→ 栈 `MainMenuScene.cs:706` ✓
- `Shell/MainMenuRuntime.cs:268` 的第二个实参 `LayoutSpace.DesignHeight` = **10 世界** ⇒ `sizeDelta.y = 10/108 = 0.0925926`
- `|0.0925926 − 10.0| = 9.9074` > `0.01` ⇒ 红。**应为 `1080f`**

### 3. `Navigation Panel` · `rect.height`（`menu3.log:2367` / 重列 `:34116`）—— 简报要现读定位的那一条

- **节点 = 左竖导航 `Navigation Panel`**（日志紧邻上文是 `✓ （前提）左竖导航 Navigation Panel 是 RectTransform` · `✓ （前提·父链缩放）… lossyScale.x = 1` · `✓ ★ 左竖导航 Navigation Panel 的 rect.width = **191.00px**`，紧接着就是这条 ✗）
- 断言：`Editor/MainMenuScene.cs:711-712`，`mbox(nav, "左竖导航 \`Navigation Panel\`", 191f, 1080f, …)`
- 实际值：`Shell/MainMenuRuntime.cs:306` `New(root, "Navigation Panel", 191f, LayoutSpace.DesignHeight)`
  - **宽 191 px ⇒ `1.768519` ✓ 过**（这条是本报告最干净的正对照：同一行、同一个工厂、只差量纲）
  - **高 `DesignHeight`(10 世界) ⇒ `0.0925926` ✗ 红**（应 `1080f`）

### 4. `Upper bar` · `rect.width`（`menu3.log:3060` / 重列 `:34127`）

- 断言：`Editor/MainMenuScene.cs:728-729`，`mbox(bar, "顶栏 \`Upper bar\`", 1920f, 100f, …)`
- 实际值：`Shell/MainMenuRuntime.cs:822` `New(root, "Upper bar", LayoutSpace.DesignWidth, 100f)`
  - **宽 `DesignWidth`(17.7778 世界) ⇒ `0.164609` ✗ 红**（应 `1920f`）
  - **高 100 px ⇒ `0.925926` ✓ 过**（`menu3.log` 紧接着那行 `✓ ★ …rect.height = **100.00px**`）

### 5. 为什么这 4 条**恰好**是 4 条

`§A332 主菜单` 节共 **28 个 `mbox` = 112 条断言**（本报告 §五 有拆解），逐轴扫一遍，**量纲错的只有那 4 个轴**（`LayoutSpace.DesignWidth/DesignHeight` 在 `New(...)` 实参位只出现 4 次：`:268` 两次 · `:306` 一次 · `:822` 一次）。
全工程 grep 复核（`grep -rn "DesignWidth\|DesignHeight" …/CardPresentation` 去 `LayoutSpace.cs`）：这两个常量在**别的 `*Px*` 形参位**上一次都没出现 ⇒ **缺陷面到此为止**，没有第二条漏的。
（同文件 `:271` `ImageQuad.Create(…, LayoutSpace.DesignHeight, …)` 与 `:274` `SetAspect(DesignWidth/DesignHeight)` 是**世界单位**的正确用法，⛔ 不是缺陷、别顺手改。）

### 6. 有没有可见影响 —— **没有**（4 条全是「元数据不忠实」，不是画面）

- `Shell/MainMenuRuntime.cs` 的不变式（WB2 写进 `:145` `New` 的注释、A332 复核过）：**节点全部停在原点，子件一律用绝对 `Center(...)` 当 `localPosition`** ⇒ 子件不读父矩形。
- 三颗节点的子件走 `Rect(...)`（`MainMenuRuntime.cs:59-70` → `RectTex` `:74-88`）：位置取 `Center(x1,x2,y1,y2)`、尺寸取 `H(y1,y2)`，**一个字都没引用父 `rect`**。
- `MenuDraw.SetPxSize`（`Shell/MenuDraw.cs:155-162`）**显式写死 `anchorMin=anchorMax=pivot=(0.5,0.5)`** ⇒ 子件 `rect` 与父矩形无关（A218 的裁定，简报②已记）。
- 全工程 `grep "\.rect\.width\|\.rect\.height"`（去 `Editor/`）**只命中两条注释** + DOTween 插件两行 ⇒ 运行时**没有任何代码**读这三颗节点的 `rect`。
- 本轮**别的节一条红都没有**（`§五 A 整屏背景` 那条「背景铺满可见高度」照旧绿，`menu3.log:443` 那一节 0 红）⇒ 画面没变。

> 结论：这是「A332 声称『19 处都写上了原版 `sizeDelta`』，实际有 3 处写的是别的数」——
> 正是 A332 自己要修的那一类缺陷（原版每一件都有自己的矩形，我们表达不出来），**该修，但不是画面上看得见的问题**。

---

## 三、若是 (α)：逐条给【可执行的正确判据文字】

**不适用（不是 (α)）。** 为免下一轮重开这一页，把「为什么排除 (α)」写死：

| 会被误判成 (α) 的点 | 实测 |
|---|---|
| 「拿 `rect` 比 px」 | ✅ 断言两边**同量纲**：`rt.rect.width`（世界）比 `LayoutSpace.Px(px)`（世界）= `px/108`。`SetPxSize` 写的就是 `sizeDelta = Px(wPx)` |
| 「分母选错」 | ✅ `LayoutSpace.Px` 是**全工程唯一一份**换算（`Core/LayoutSpace.cs:116`，`MenuDraw` 已转发）。**34/38 个轴过**（19 节点 × 2 轴 − 4 条红）⇒ 分母不可能错 |
| 「期望值取错（拿了 WB2 那份『退化读法』）」 | ✅ **排除，有第二来源**：`资料/主菜单_原版规格.md`（**A332 之前的旧表**）独立写着 —— `:258` `MainMenu(内)/Background` RT/1089 = **0..1920, 0..1080 拉伸** · `:304` `Upper bar` RT/1092 = **1920×100** · `:117` `Navigation Panel` RT/1103 = `a(0,0)-(0,1) sz(191,0)`。**三处逐位吻合** |
| 「容差太紧」 | ✅ tol `0.01` 世界单位 ≈ **1.08 px**。这 4 条差的是 **17.6 / 9.91 / 9.91 / 17.6 世界单位**（≈ 1900px / 1070px）⇒ **差 108 倍**，与容差无关 |

---

## 四、若是 (β)/(γ)：最小改法 + 归属哪一件

**归属**：`Shell/MainMenuRuntime.cs` 的 A332 收口（本批 WB2）· **新账**（本批新引入）。

**最小改法 = 3 行、4 个实参**（其余 16 个调用点、`MenuDraw.SetPxSize`、断言、`PracticeModePopup` 那半边**一个字都不用动**）：

```csharp
// Shell/MainMenuRuntime.cs:268
- var go = New(root, "Background", LayoutSpace.DesignWidth, LayoutSpace.DesignHeight);
+ var go = New(root, "Background", LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);   // 1920 × 1080

// Shell/MainMenuRuntime.cs:306
- var p = New(root, "Navigation Panel", 191f, LayoutSpace.DesignHeight);
+ var p = New(root, "Navigation Panel", 191f, LayoutSpace.DesignPxH);               // 191 × 1080

// Shell/MainMenuRuntime.cs:822
- var bar = New(root, "Upper bar", LayoutSpace.DesignWidth, 100f);
+ var bar = New(root, "Upper bar", LayoutSpace.DesignPxW, 100f);                    // 1920 × 100
```

- `LayoutSpace.DesignPxW/DesignPxH` 就是 `1920f/1080f` 的 `const`（`Core/LayoutSpace.cs:113`）⇒ **语义正、不需要新常量**。
  ⚠️ 也可写成字面量 `1920f/1080f`（与另外 16 个调用点的风格一致）；两种都能让这 4 条转绿，**选哪种由调度台定**。
- ⛔ **别去改 `LayoutSpace.DesignWidth/DesignHeight` 的定义** —— 它们是「可见设计矩形（世界单位）」，`ImageQuad.Create`/`SetAspect`/`Camera.orthographicSize` 一大片在用，改了会炸别处。
- ⛔ **别去改断言**：`editor` 那 4 条断的正是原版值，改它就是「拿我们的实现证明我们的实现」。
- 改完**只影响 `MainMenuScene.Run` 这一条宿主**（`Shell/MainMenuRuntime.cs` 只被 `Editor/MainMenuScene.cs` 用？→ ⚠️ **未核实**，见 §六·2；稳妥起见按「碰了 Shell 共用件」跑到覆盖面该跑的那几条）。
- 顺带**必看**：`Shell/MainMenuRuntime.cs:268` 那一行上方 A332 自己留的注释（`:266-267`）写「绝对矩形 = 整屏 1920×1080」—— 注释是对的，**实参写错了**；改的时候把注释一起看住。

---

## 五、`+144` 条断言的来源拆解（⛔ 别把它当成「WB2 加了两百条」）

| 来源 | 条数 | 说明 |
|---|---|---|
| `§A332 主菜单` 节（`Editor/MainMenuScene.cs:694-…`） | **+112**（108 ✓ / 4 ✗） | **28 个 `mbox` × 4 条**（`t==null`/`rt==null` 的兜底 1 条路**一次都没走到** ⇒ 28×4 满额）。28 = `Background`·`Navigation Panel`·`Buttons Container`·5 颗导航钮 + 5 个 `Hit` + `Upper bar`·`SettingsBtn`·`TopBarButtons`·`InboxBtn`·`Resources Bar`·`Player Profile`·`Avatar Item Small`·`Player Level`·`ChatPreview`·`GameModes`·`Viewport`·`Content`·`Tutorial` 卡·`Practice` 卡·`Practice/Hit` |
| `§A332 练习窗` 节（`:1692`） | **+36 新增** | **9 个 `pbox` × 4 条**（9/9 都非 null） |
| 同上这一节**「搬家」进来的** | **+13**（**不是新增**） | `§A332 练习窗` 的 `Section(...)` 被插在 `§A218 练习窗` 与后面「换一套卡组…」那一段**之间**（`git diff` 里那 13 条断言是**上下文行**、不是 `+` 行）⇒ 那 13 条既有断言在**日志里换了「节」的归属** |
| `§A218 练习窗` 桶 | **−13**（另有 1 行 `截图 …` 也被划走，那不计数） | 就是上一条搬走的那 13 条 —— **没删**（`menu.log` 42 行 → `menu3.log` 28 行，差 14 = 13 ✓ + 1 截图行） |

**对账**：`112 + 36 = 148` 条新断言；上一轮 `1834 ✓ / 0 ✗` ⇒ 本轮应 `1834 + 148 − 4 = 1978 ✓`、`✗ = 4` ⇒ **与 `menu3.log:34083` 的 `=== 合计：1978 通过 / 4 失败 ===` 逐位吻合**。
分节桶的差（+108 / +4 / +49 / −13 = **+148 通过**）也自洽。

**另**：`menu3.log` 里 **✗ 行共 8 条**（`:2293 :2308 :2367 :3060` + `:34094 :34105 :34116 :34127`），后 4 条是**失败重列**、与前 4 条**逐字节相同**（已用 python 逐行 `==` 比对，`identical=True`）。这 4 条重列出现在 `=== 合计 ===`（`:34083`）**之后**，落在 `§A92 节点类型` 那个桶里 ⇒ 分节脚本会把它算成「§A92 +4 fail」，**那是假的**（判定口径：`✗` 一律按行首计数、然后**去重**）。

---

## 六、判不了的（缺什么才能判）

1. **`new GameObject(name, typeof(RectTransform))` 的原生默认 `sizeDelta` 仍未实测**（A92 §三·1 留的坑，WB2 报告 §五·2 原样复述）。
   这**不影响**这 4 条红的判定（它们是「**写了、值错**」；「没写时是什么」是另一件事）。
   影响的是 **WB2 报告 §四「改坏法①」那句话的成立性**（详见 §七·4）。
2. **`Shell/MainMenuRuntime.cs` 被谁用** —— 我只确认了 `Editor/MainMenuScene.cs` 是宿主；**没查**是否还有别的窗口/宿主引用它（`MainMenuRuntime.QBarPanel` 之类被 `Editor/ShellScene.cs` 等引用过）。
   ⚠️ 这决定改完要跑**几条**自检（铁律 12 的「共用件先 grep 谁在用」）⇒ **改之前先 grep 一遍**，我没做。
3. **原版 `Upper bar` 宽 = 1920 / `Background` = 1920×1080 的「第一来源」我这次没去重读 bundle**（按简报②「别重复查」）。
   但**第二来源已核**（`资料/主菜单_原版规格.md:258,304,117`，见 §三）⇒ **足够排除 (α)**。

---

## 七、顺手发现（⛔ 只报不改）

1. 🔴 **命名陷阱（本批 4 条红的唯一根因）**：`LayoutSpace` 里有**两组**只差 `Px` 两个字母的常量 ——
   `DesignWidth/DesignHeight`（**世界单位** 17.7778 / 10）与 `DesignPxW/DesignPxH`（**设计像素** 1920 / 1080），**量纲差 108 倍**。
   A332 的 3 个调用点就是照前者写的。⇒ 建议在 `Core/LayoutSpace.cs:26,31` 那两个世界常量上加一句显式警告
   （「⚠️ 这是**世界单位**；⛔ 别传进任何 `*Px*` 形参（`SetPxSize` / `Node` / `ApplyPxRect`）」）。
   ⚠️ **别改名字**（`ImageQuad` / `Camera` / `DeckScene` / `ShellScene` 一大片在用，见 `grep` 输出）。
2. 📌 **日志可读性陷阱**：`Editor/MainMenuScene.cs:705,707` 的文案把实测值**换算成 px** 打印
   （`{rt.rect.width * 108f:F2}px`）。当实测值恰好是「本应 px 的数被当世界单位用」时，
   它打印出的就是 `**17.78px**` —— **与期望的 1920px 差 108 倍，肉眼却像「差一点点」**。
   （`CheckNear` 括号里的 `（0.165 ≈ 17.778±0.010）` 是对的、够判案；建议文案里把这两个数也标上「实测 / 期望」。）
3. 📌 **13 条断言「换节」这件事必须说清**：否则下一轮做通过数审计时，会把 `§A332 练习窗 +49` 读成「49 条新断言」（真值 **36 新 + 13 搬**）。
   （同理 `§A218 −13` 不是「删了 13 条」，是划走了。）
4. 📌 **WB2 报告 §四「改坏法①」有一句未证实**：原文「删掉 `MenuDraw.SetPxSize(...)`（或把工厂改回只建节点）⇒ 下面**每一条**的 `rect.width/height` 都变 **0** ⇒ 全红」。
   实质是「变回 `RectTransform` 的**原生出厂默认**」，**未必是 0**（同一份报告 §五·2 自己说这个默认「仍未实测」）。
   ⇒ 建议要么补一次实测，要么把措辞改成「其值等于 `RectTransform` 出厂默认（未实测）」。
5. 📌 **缺陷面清爽**：`MenuDraw.SetPxSize` 本身、`PracticeModePopup` 那 9 处（`§A218` +28 / `§A332 练习窗` +36 全绿）**一条都没红** ⇒ 本批的红**只在 `MainMenuRuntime` 3 个调用点的实参**，收口范围可控。
6. 📌 简报②给的「强线索」（怀疑 WB2 把原版那份『退化读法』当期望值写进断言）**这条线索不成立**：
   - 断言的期望值是 **1920/1080/100 字面量**，不是 0×0 也不是负数；
   - 红的**全在「实测」那一侧**，且实测值能用一个干净的 108× 算式解释（§二）；
   - `资料/已知的坑.md:3991`（WB2 自己记的「三颗全退化 RT」）说的是**读原版 bundle 时**的口径，与本次实现侧无关。
   ⇒ 这个怀疑方向可以关掉，别在下一轮重开。

---

## 八、我读过的文件与命令

**文件**
- `d:/4/_tmp_view/menu3.log`（本轮 1978/4）· `d:/4/_tmp_view/menu.log`（上一轮 1834/0）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（`:1-60` · `:180-230` · `:685-735` · `:1626-1700`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs`（`:60-112` · `:110-200` · `:262-290` · `:818-832` · `:1221`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs`（`:112-185`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/LayoutSpace.cs`（`:100-142`）
- `d:/4/Unity/资料/主菜单_原版规格.md`（`:246` · `:258` · `:304` · `:117`）
- `d:/4/Unity/资料/普查产出_1011/WB2_A332.md`（逐份读完）
- `d:/4/Unity/资料/已知的坑.md`（`:2045-2049` · `:3908-3910` · `:3991-3996`）
- `d:/4/Unity/资料/待办判据_1007.md`（`:160-162` A218 判据原文）

**命令**（全部只读）
- `wc -l` / `sed -n` 读日志与源码 · `grep -n "= New("` · `grep -n "^\[Menu\] *✗"`（+ `cat -A` 核中文字节）· `grep -rn "DesignWidth\|DesignHeight"` · `grep -rn "\.rect\.width|\.rect\.height"`（去 `Editor/`）· `grep -n "§A332\|§A218"` · `grep -n "mbox("`
- `git diff` / `git diff -U2` / `git diff --numstat` / `git log --oneline -3 -- <path>`（**只读**）
- python（只读）：① 按 `[Menu] --- … ---` 切桶、逐节数 ✓/✗ 并两轮对差；② 对 `§A218` 桶做 `difflib` 逐行 diff；③ 校验 8 条 ✗ 行的**失败重列与前 4 条逐字节相同**

**没做**：⛔ 未跑 Unity / 未跑任何 `*.Run` · ⛔ 未动 git 写命令 · ⛔ 除本报告外**未写任何文件** · ⛔ 未改 `项目任务.md` / `CLAUDE.md` / `资料/` 既有文件。
