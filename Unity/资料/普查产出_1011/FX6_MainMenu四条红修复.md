# FX6 · MainMenu 四条红修复（写手 · 2026-10-11）

> 判据 = `资料/普查产出_1011/D7_MainMenu四条红.md`（第一判据，逐份读完）⇒ 归属 **(β) 实现真缺陷**，照 D7 §四最小改法执行。
> 只碰白名单那两个文件。⛔ 未跑 Unity / 未跑任何 `*.Run` · ⛔ 未动 git（只跑了只读 `git diff/--numstat`）· ⛔ 未改断言 · ⛔ 未改 `DesignWidth/DesignHeight` 定义。

## 一、结论

- **已修**：`Shell/MainMenuRuntime.cs` **3 行 / 4 个实参** —— 原来把**世界单位**传进了收**设计像素**的口 ⇒ 1/108 做了两遍。
- 改后 4 个轴 = `LayoutSpace.Px(1920f)` / `LayoutSpace.Px(1080f)`，与断言那一侧**是同一个式子**（残差 0）。
- `Core/LayoutSpace.cs`：给两组只差 `Px` 两字母、**量纲差 108 倍**的同族常量加了显式警告注释；**⛔ 未改名**（`ImageQuad`/`Camera`/`DeckScene`/`ShellScene` 一大片在用）。
- ⛔ **未动**：断言（`Editor/MainMenuScene.cs` 全程只读）· 同文件 `:276`/`:279` 的 `LayoutSpace.DesignHeight`（`ImageQuad.Create`/`SetAspect` 要的就是世界单位）· 另外 16 个调用点。

## 二、判据（逐处：原写法 / 新写法 / 为什么量纲对）

**共同根因**：`New(parent, name, float wPx, float hPx)`（`Shell/MainMenuRuntime.cs:147`）的两个口收**设计像素** ——
它内部 `MenuDraw.SetPxSize`（`Shell/MenuDraw.cs:155-164`）写的是 `rt.sizeDelta = LayoutSpace.Px(wPx)`，
而 `LayoutSpace.Px(px) = px / (DesignPxH / DesignHeight)` = **px ÷ 108**（`Core/LayoutSpace.cs:128`）⇒ `sizeDelta` 是**世界单位**。

| # | 处（改后行号） | 原写法 | 新写法 | 为什么量纲对 | 差多少 |
|---|---|---|---|---|---|
| 1 | `MainMenuRuntime.cs:274` | `New(root, "Background", LayoutSpace.DesignWidth, LayoutSpace.DesignHeight)` | `New(root, "Background", 1920f, 1080f)` | `DesignWidth` = `DesignHeight×16/9` = **17.7778 世界单位** → 被当 px 再 ÷108 ⇒ `sizeDelta.x = 0.1646`，期望 `Px(1920) = 17.7778` | 17.6131 世界单位（≈1902px） |
| 2 | 同上（`hPx`） | `LayoutSpace.DesignHeight` = **10 世界单位** | `1080f` | `sizeDelta.y = 10/108 = 0.0926`，期望 `Px(1080) = 10.0` | 9.9074 |
| 3 | `MainMenuRuntime.cs:314` | `New(root, "Navigation Panel", 191f, LayoutSpace.DesignHeight)` | `New(root, "Navigation Panel", 191f, 1080f)` | 同一行的 `191f` 是 px ⇒ **宽那条本来就过**（干净正对照）；只有高传了世界单位 | 9.9074 |
| 4 | `MainMenuRuntime.cs:832` | `New(root, "Upper bar", LayoutSpace.DesignWidth, 100f)` | `New(root, "Upper bar", 1920f, 100f)` | 同一行的 `100f` 是 px ⇒ **高那条本来就过**；只有宽传了世界单位 | 17.6131 |

- **为什么选 px 字面量、不选 `LayoutSpace.DesignPxW/DesignPxH`**（简报让我写进注释）：两者**等值**，但同文件另外 **16 个调用点全是 px 字面量**（`:331 164.379f,931.66f` · `:836 87.78f,61.73f` · `:1091 74f,945.5651f` …，两个常量件 `NavBtnW/ModesW` 的定义处也是 px 字面量）⇒ **字面量与同族同风格**，且每行两个实参一眼可读作「191 × 1080」。两条理由已写进 `:268-273` 的注释。
- **判据出处**：`Shell/MenuDraw.cs:162`（换算只有这一份）· `Core/LayoutSpace.cs:14`（「可见宽度 = `DesignHeight` × 宽高比（16:9 → **17.78**）」= 日志里印出来的那个 `17.78px`，铁证）· 期望值第二来源 `资料/主菜单_原版规格.md:258/304/117`。

## 三、改动清单

- `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs`
  - `:274` `New(root, "Background", 1920f, 1080f)`（原 `LayoutSpace.DesignWidth, LayoutSpace.DesignHeight`）
  - `:314` `New(root, "Navigation Panel", 191f, 1080f)`（原第 2 实参 `LayoutSpace.DesignHeight`）
  - `:832` `New(root, "Upper bar", 1920f, 100f)`（原第 1 实参 `LayoutSpace.DesignWidth`）
  - 三处各加 2~3 行注释（记「修了什么 / 为什么量纲对 / 本行正对照」），共 **+10 行**；⛔ 逻辑只有这 3 行。
- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/LayoutSpace.cs`
  - `:25-35` `DesignHeight` 的 `<summary>` 扩成量纲警告（世界单位 vs px · 差 108 倍 · ⛔ 别传进 `*Px*` 形参 · ⛔ 别改名）；`:41-42` 给 `DesignWidth` 补一句同款指回警告。**只加注释，一个字符的代码没动**；共 **+12 行**。
- ⛔ 其它文件一律未碰。

## 四、改后预计

- **预计 `MainMenuScene.Run` 4 条 ✗ 全转绿** ⇒ 合计从 `1978 通过 / 4 失败` 变 **`1982 通过 / 0 失败`**；`§A332 主菜单` 节 `108/112` → **`112/112`**。
- `=== 合计 ===` 那 4 行**失败重列**（`menu3.log:34094/34105/34116/34127`）一并消失 ⇒ 日志里 ✗ 行 **8 → 0**；`§A92 节点类型` 桶那 4 条假 fail 也随之清零。
- 依据：4 条断言的期望值是**独立来源**（`资料/主菜单_原版规格.md` 三处），不是照我们实现写的 ⇒ 值改对就该绿。
- 🔑 **同一行那条「本来就过」的轴就是本式闭环的实测证据**：`Navigation Panel` 宽传 `191f` ⇒ 日志印 **`191.00px`**、`Upper bar` 高传 `100f` ⇒ **`100.00px`**（`*108f` 与 `÷108` 逐位还原）⇒ 改后的 `Px(1920)/Px(1080)` 与断言期望**是同一个函数调用**（残差 0、与容差无关）。
- ⚠️ **这是「预计」不是「已验证」** —— 我**没跑 Unity**（简报③明令）；以主对话同步点那次重跑为准。

## 五、没查清 / 判不了的（⛔ 不猜）

- `new GameObject(name, typeof(RectTransform))` 的**原生出厂 `sizeDelta`** 仍未实测（A92 留的坑，D7 §六·1）。我**没测**（无 Unity），也**没有在注释里写**「删掉 `SetPxSize` 会变 0」这类说法 —— 三处注释只写「被除两次 108 ⇒ `rect` 只剩 0.165/0.093」，那是**日志实测值**、不是对出厂默认的断言。
- 三条原版矩形（`1920×1080` / `191×1080` / `1920×100`）我**没有**回头重读 bundle 核第一来源（按简报⑤「别重复查」）；第二来源三处 D7 §三已逐位核过 ⇒ 我不新增也不推翻。
- `LayoutSpace.DesignWidth` 是 **property 而非 `const`**：不影响任何调用点（三处全是运行时求值），仅记录。

## 六、顺手发现（⛔ 只报不改）

1. **D7 §六·2「谁在用 `MainMenuRuntime`」—— 已查清（补全它留的那格）**：全工程**非注释引用共 3 处** —— ① `Editor/MainMenuScene.cs`（宿主：`:303 AddComponent<MainMenuRuntime>()`，`Run` 走它）② `Editor/ShellScene.cs:2348`（`mmGo.AddComponent<MainMenuRuntime>()`，同行注释明写「**只挂组件、不建界面**（`Start` 在编辑模式下不跑）」）③ `Shell/PointerLayer.cs:648`（`MainMenuRuntime.EscapePressed()`，静态、与本件无关）。⇒ **真正会跑 `New(...)` 的只有 `MainMenuScene.Run`**；`ShellScene.Run` 只编译到它。**跑几条由调度台按铁律 12 定**，我只交这张表。
2. **日志文案可读性陷阱**（D7 §七·2 复述，⛔ **该处在 `Editor/MainMenuScene.cs` = 我的黑名单**）：`:705/:707` 把实测值 `×108f` 印成 px ⇒ 「世界单位当 px 用」会印出 `**17.78px**`，肉眼像「差一点点」，实差 **108 倍**。这次是 `CheckNear` 括号里的 `（0.165 ≈ 17.778±0.010）`救了命。建议将来给这两个数标「实测 / 期望」。
3. **本件只影响「矩形元数据」，不改画面** —— 复述 D7 §二·6 的结论（子件全走绝对 `Center(...)`、`SetPxSize` 写死 `anchor=pivot=(0.5,0.5)`、运行时无代码读这三颗节点的 `rect`）⇒ 这 4 条红**不是可见回归**，修它是为了「原版每一件都有自己的矩形」这条口径。
4. **`git diff --numstat` 的数字别读错**：`MainMenuRuntime.cs` 报 **291/127**，那是 **A332（WB2）本波未提交的存量改动**（`New` 的签名与整段注释在 diff 里都是 `+` 行），**不是 FX6 改的**；FX6 的增量只有 **3 行代码 + 10 行注释**（`LayoutSpace.cs` 报 13/1，其中 12 行是新增注释、1 行是 `DesignHeight` 那句 summary 的重写）。

## 七、跑过的检查

- **秒级类型检查**（`TMPDIR=/tmp/wf_fx6 bash d:/4/Unity/工具/typecheck.sh`，**改完第二次（注释措辞）后又跑了一遍**）原文：
  - `--- 运行时程序集 ---` / `运行时错误数: 0`
  - `--- 编辑器程序集 ---` / `编辑器错误数: 0`
- **行尾复量**（二进制读，改后终态）：`Shell/MainMenuRuntime.cs` `crlf=0 / lf=1301`（改前 `1291`，+10 = 三处注释净增，**未翻行尾**）；`Core/LayoutSpace.cs` `crlf=0 / lf=155`（改前 `143`，+12，**未翻行尾**）。
- 只读复核：`grep -n "LayoutSpace.DesignWidth\|DesignHeight\|= New("` ⇒ 剩余两处世界单位用法（`:276`/`:279`）确认为**正确用法、未动**；`grep -rn "MainMenuRuntime"`（§六·1）。
- ⛔ 未跑 Unity / 未跑自检 / 未动 git 写命令 / 未改 `项目任务.md`·`CLAUDE.md`·`资料/**` 既有文件。
