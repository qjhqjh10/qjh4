# W0 · `Shell/PointerLayer.cs`：给 A964 探针开三个只读口

> 立项 = `资料/普查产出_1018第三会话/RECON_A964前置.md` §1（本件**不重复**它的侦察，只落地）。
> ⛔ 没跑 Unity · 没动 git · 没改正本 · **只改了 `Shell/PointerLayer.cs` 一个文件**。
> 📌 本件**只开只读口、没加任何断言** —— 断言宿主（`Editor/ShellScene.cs` / `Editor/BattleScene.cs`）
> 不在本写手白名单里 ⇒ **下一条自动断言由下一批的探针自己带**。

## 一、四个私有物的签名与语义（读到的；行号 = **改前**）

| 私有物 | 签名 | 语义 |
|---|---|---|
| `HitBoxPx` `:831/:870` | `static bool HitBoxPx(WindowButton b, out Vector2 center, out Vector2 half)` / `…, out ImageQuad q)` | 一颗钮的**命中区**：中心 + 半宽半高。口径 = **画布 px、`y` 向下**（原版 `m_AnchoredPosition` 那套）。`false` = 不可命中。**唯一实现 = 四参那个**（两参那个转发它）。 |
| `HitQuad` `:897` | `static ImageQuad HitQuad(WindowButton b)` | 一颗钮的**命中用 quad**（`null` = 不可命中/不可导航）。用 `GetComponentInChildren` —— `AddHit` 是 `ImageQuad.Create(hit,…)` 把 quad 建成**子物体**，不在按钮自己那层。三处 `null` 来源见 §三。 |
| `Navigable` `:944` | `static bool Navigable(WindowButton b)` | `b != null && !b.absorbOnly && HitQuad(b) != null`。类注释自称「**全类唯一判据**」（`FindInDirection` 候选集 / `ButtonCountForTest` / `ButtonCountUnder` 三处都走它）。 |
| `AllButtons` `:908` | `static WindowButton[] AllButtons()` | `Object.FindObjectsByType<WindowButton>` —— **全场景现扫**（只在真有输入时走一次，不是每帧）。 |

`HitBoxPx` 里两个探针会用到的细节：`center = ToPixel(q.transform.position)`（视觉世界坐标，**已含父链缩放**）、
`half = q.WorldW/H × ScaleAbs(lossyScale) × k × 0.5`（`k = DesignPxH / DesignHeight`；父链缩放因子是 **A167** 补的）。
另有 `HitBox`（三参、y **向上**）**只给键盘导航用**，不在本次开的口里。

## 二、开的三个口（新行号）

| 口 | 签名 | 函数体（**全部内容**） |
|---|---|---|
| `HitBoxForTest` `:951` | `public static bool HitBoxForTest(WindowButton b, out Vector2 center, out Vector2 half, out ImageQuad quad)` | `=> HitBoxPx(b, out center, out half, out quad);` |
| `HitQuadForTest` `:958` | `public static ImageQuad HitQuadForTest(WindowButton b)` | `=> HitQuad(b);` |
| `AllButtonsForTest` `:961` | `public static WindowButton[] AllButtonsForTest()` | `=> AllButtons();` |

- **为什么 `public`**：本仓 **0 个 `.asmdef`**（现核 `find MyGame/Assets -name "*.asmdef"` ⇒ 空）
  ⇒ 自检宿主在 `Assembly-CSharp-Editor.dll`、生产在 `Assembly-CSharp.dll` ⇒ **`internal` 跨不过去**。
  判据 = `资料/已知的坑.md:2927`（**本件现读该文件核过**；节标题原文「`internal` 在**自检里用不了** ——
  自检在编辑器程序集，跨程序集看不见」）。命名沿用本类既有 `*ForTest`（`ButtonCountForTest` /
  `ButtonCountUnder` / `HoveredForTest`）。
- **「转发不加工」怎么保证的**：三处函数体**各自只有一个 `=>` 表达式** —— 无局部变量、无分支、
  无换算、无夹取、无默认值；签名里连 `out` 都是原样传下去的。想「加工」就必须先写第二行语句。
- ⚠️ **本件【没有】开 `Navigable` 的口**（简报的「建议三个口」里也没有）：探针要这个判据时用
  `b.absorbOnly`（`Shell/PromptPopup.cs:537`，**public 字段**）+ `HitQuadForTest(b)` 自组即可。
  **若调度台认为该收口成第四个口，加一行即可**（本件没自作主张加，避免开没人要的公共 API）。
- 三个口**全仓 0 个调用点**（本次新加，还没接线）⇒ 见 §四「没查清」。

## 三、注释里写进去的陷阱（落在 `:933-944`，下一批写手必须读到）

1. **先【逐扇窗开一次】再扫**：`WindowsManager.CloseAllWindows()` → 逐扇 `OpenWindow` → 扫 → 关，
   **一次只开一扇**（被压到 `Background` 的窗按 `PointerReachable` 本来就不参与指针命中）。
2. **`HitQuad` 恒 `null` 的三个原因**（三条都在 `HitQuad` 那六行里，按**符号**认、⛔ 别抄行号）：
   ① 那一颗**不 `activeAndEnabled`**；② 子树里**根本没有 `ImageQuad`**（**这才是 A8 那一族真缺陷**）；
   ③ 有 quad、但 **quad 不在激活链上**（**多半是那扇窗没开** / 页签切走了）。
   ⇒ `HitQuadForTest(b) == null` **只说「不可命中」、不说是哪一条** ⇒
   ⛔ **把 ③（窗没开）报成 ②（缺 quad）= 把「窗没开」写成「缺陷」= 一片假红**。
3. 另注：`Background` 态窗里的钮不参与指针命中是**原版行为**，别算进「不可命中」的账。
4. 段头另写了三条**为什么**（为什么要有这个口 / 为什么必须 `public` / 「转发不加工」是全部纪律，
   含**改坏法**：往口里塞算式 ⇒ 生产侧改坏时探针**照样全绿**）。

## 四、类型检查 · 行尾 · 没查清

- **类型检查**：`TMPDIR=/tmp/wf_pl bash d:/4/Unity/工具/typecheck.sh`
  ⇒ **运行时错误数 0 / 编辑器错误数 0**（改了两次、跑了两次，两次都是 0）。**没跑任何 Unity 批处理。**
- **行尾**：改前二进制实测 = `CRLF 0 / LF 1154`（**纯 LF**）；改后 = `CRLF 0 / LF 1209`
  ⇒ **保持纯 LF、没翻**（⛔ 没用 `sed -i`，全程 Edit 工具）。
- **`git diff --numstat` = `56 1`**：56 行新增是本节三个口 + 注释；**那 1 行删除【不是本件改的】** ——
  `:12` 那句注释（`Deck/DeckRuntime.cs:732 HandlePointer()` → 改成「按【符号】认，别抄行号」）
  **我开工第一次 `Read` 时就已经是新措辞**，属**接手前就存在的未提交改动**。我没碰它、也没动 git。
- **没查清 / 未验**：① 三个口**没有任何调用点** ⇒ 编译过 ≠ 探针调得对，**下一批探针要先跑一次**；
  ② 探针实际能开出几扇窗、`AllButtonsForTest` 真跑时会收进哪些夹具 —— **没实测**（本件不许跑 Unity）；
  ③ `HitBoxForTest` 的 `center/half` 与「**实绘矩形**」的口径差（A964 要断的正是这个）**本件没量**，留给探针；
  ④ `HitBoxForTest` 的 `quad` 出参**与 `HitQuadForTest` 是否恒同一颗**——按符号看是（都调 `HitQuad`），**没跑过**。
