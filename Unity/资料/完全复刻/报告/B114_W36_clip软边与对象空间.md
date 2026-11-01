# `B114_W36` · Border Highlight 的 `UNITY_UI_CLIP_RECT`：软边 + 对象空间

> 写手 `W-36` · 2026-11-01。任务 `B-273`。判据由 `B114_R8` 从原版 shader **自己的 DXBC 字节码**里坐实
> （`判据/B114_R8_BorderHighlight_clip.md`）。
> ⛔ 没跑 Unity（`-executeMethod` 全禁）· ⛔ 没动 git · 只改了一个 shader + 新建一个断言文件。
>
> **备份**：`d:/4/_tmp_view/b114_w36/WFUIBorderHighlight.shader.bak`（改前 270 行 · 20,382 B · 纯 LF）。

---

## 〇 一句话

把 `UNITY_UI_CLIP_RECT` 那一路从 **`UnityGet2DClipping`（世界空间 + `step` 硬边）** 换成
**原版那条 `saturate` 线性软边 + `positionOS` 对象空间**，并补上 `_UIMaskSoftnessX/Y` 两条属性与整条 `pixelSize` 链。
**惰性路径（今天零消费者）⇒ 零视觉影响**，但按铁律 11「复刻要照形状补对」做完了。

---

## 一 逐条对照（原版指令 → 我们原来 → 现在）

### 1·1 片元（PS idx 24 结尾 5 条 = 全部裁切指令）

| # | 原版 DXBC（`判据/B114_R8…` §2·1） | 我们**原来** | 我们**现在** |
|---|---|---|---|
| 1 | `add r0.xy, -cb0[131].xyxx, cb0[131].zwzz`（`_ClipRect.zw - _ClipRect.xy` = 宽高） | 没有（`step` 不需宽度） | `_ClipRect.zw - _ClipRect.xy` |
| 2 | `add r0.xy, r0.xyxx, -\|v3.xyxx\|`（`− \|o3.xy\|`） | `step(clipRect.xy, pos) * step(pos, clipRect.zw)` | `− abs(i.clipRect.xy)` |
| 3 | `mul_sat r0.xy, r0.xyxx, v3.zwzz`（`saturate(· × o3.zw)`） | **无**（`step` 是 0/1 突变） | `saturate( · * i.clipRect.zw)` |
| 4 | `mul r0.x, r0.y, r0.x`（`m.x * m.y`） | `inside.x * inside.y` | `mk.x * mk.y` |
| 5 | `mul o0.w, r0.x, r0.w`（`color.a *=`） | `o.a *= UnityGet2DClipping(i.worldXY, _ClipRect);` | `o.a *= mk.x * mk.y;` |

落点：`WFUIBorderHighlight.shader` 的 `frag` ⑨（原 `:258-260` → 现 `:321-322`）。

### 1·2 顶点（VS idx 0，`o3` **无条件**算）

| # | 原版 DXBC（§2·2） | 我们**原来** | 我们**现在** |
|---|---|---|---|
| a | `max/min r1.xyzw, cb0[131].xyzw, ±2e10`（`clamp(_ClipRect, -2e10, 2e10)`） | 无 | `clamp(_ClipRect, -2e10, 2e10)` |
| b | `mul r1.z, cb0[23].x, cb0[62].x` · `mul r1.w, cb0[23].y, cb0[63].y`（`P00·SP.x`, `P11·SP.y`） | 无 | `UNITY_MATRIX_P[0][0]*_ScreenParams.x` / `[1][1]*_ScreenParams.y` |
| c | `div r1.xy, l(1.0,1.0), \|r1.zwzz\|`（`pixelSize = 1/\|…\|`） | 无 | `1.0 / abs(float2(...))` |
| d | `mad r1.xy, cb0[132].xyxx, l(0.25), r1.xyxy`（`0.25·softness + \|pixelSize\|`） | 无 | `0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)` |
| e | `div o3.zw, l(0,0,0.25,0.25), r1.xxxy`（`0.25 / (…)`） | 无 | `0.25 / (…)` |
| f | `mad r1.xy, v0.xyxx, l(2.0,2.0), -r1.xyxx`（`2·positionOS − clampedRect.xy`） | `mul(unity_ObjectToWorld, v.vertex).xy`（**世界**） | `v.vertex.xy * 2.0 − clampedRect.xy` |
| g | `add o3.xy, -r1.zwzz, r1.xyxx`（`… − clampedRect.zw`） | 同上（没有这一步） | `− clampedRect.zw` |

落点：`vert` 内 `o.clipRect = …` 那一段（新增 · 现 `:233-237`）。
`v2f` 里 `float2 worldXY : TEXCOORD3` 换成 `float4 clipRect : TEXCOORD3`（插值器个数不变）。

### 1·3 属性表

| 项 | 原版 | 我们原来 | 现在 |
|---|---|---|---|
| `_UIMaskSoftnessX` | 有（`cb0[132].x`，推断） | **没有** | `[HideInInspector] … (Float) = 1` |
| `_UIMaskSoftnessY` | 有（`cb0[132].y`，推断） | **没有** | `[HideInInspector] … (Float) = 1` |
| `pixelSize` 链 | 有（VS） | **没有** | 已加在 `vert` |
| `_ClipRect` | `$Globals` 由 uGUI 喂 | 我方材质属性（`B-262`） | **不变**（默认仍是「不裁」档） |
| `#include "UnityUI.cginc"` | 原版没有 | 有（为了 `UnityGet2DClipping`） | **已删**（不再用那条硬边函数） |

---

## 二 哪些是**推断**（⛔ 别当已证事实）

代码注释里都写了 `// 推断（非判据）：…`。逐条：

1. **`cb0[131] = _ClipRect`** —— 这一格 R8 其实**坐实**了（`±2e10` 字面量出现在恰好是 `clamp(_ClipRect,…)` 的位置），
   所以本文件按**判据**处理，注释里没打「推断」。但 RDEF 被剥，**没有名字**直接作证 —— 属「强证据、缺署名」。
2. **`cb0[132].xy = _UIMaskSoftnessX/Y`** —— 🔴 **推断**。依据 = 它在 `0.25/(0.25·x+|p|)` 的**唯一**位置、
   与 `CanvasPass.hlsl:82` 逐字同形（R8 §五·3）。
3. **`cb0[23] = _ScreenParams`、`cb0[62].x/[63].y = 投影 2×2 对角线`** —— 🔴 **推断**（R8 §五·2；
   RDEF 被剥 + serde 里只有 `unity_ObjectToWorld` / `unity_WorldToObject` / `_StartOffset` 三个槽位有名字）。
4. **`pixelSize` 的首项**：本地 `CanvasPass.hlsl:77` 写 `pixelSize = output.positionCS.w`，
   而原版 DXBC 是**字面量 `1.0`**（`div r1.xy, l(1.0,1.0), |…|`）。正交 UI 下 `w ≡ 1` ⇒ 数值等价。
   本文件**按字节码写 `1.0`**（⛔ 没写 `o.pos.w`）。
5. **过渡带宽度 ≈ 3 个对象单位**（`S + 4|pixelSize|`，`S` 默认 1.0、`|pixelSize| = 0.5`）——
   这是「套源码 + 默认属性值」推的，**不是从原版实拍量的**。R8 也这么标的。

---

## 三 改动 + 净增净减

| 文件 | 改了什么 | 净增净减 |
|---|---|---|
| `Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` | ① `Properties` 加 `_UIMaskSoftnessX/Y`（2 条 + 注释）② 删 `#include "UnityUI.cginc"`（换注释）③ `v2f`：`worldXY` → `clipRect` ④ 变量声明加两条 softness ⑤ `vert`：新增 `o.clipRect` 那一段（含 `clamp` / `pixelSize`）⑥ `frag` ⑨：`UnityGet2DClipping` → `saturate` 软边 ⑦ 文件头加一节 `B-273`，并就地订正差异⑧（原来写的是「按 uGUI `UI/Default` 的约定补」） | **270 → 333 行（+77 / −14，净 +63）** |
| `Assets/CardPresentation/Editor/B114_W36_Assert.cs`（新建，+ `.meta`） | 20 条断言：A0–A5（运行时属性表/默认值）· B0–B12（源码级，含 3 条灭自证） | 新文件 |

**⛔ 没动**：`Shell/ItemDrawer*.cs` · `RuleEngine/**` · 其它 shader · `Editor/ShellScene.cs` · `Editor/BattleScene.cs` · 两份正本 · `账/*.tsv` · `Scenes/*.unity`。
`_DEBUG` / `_AUTOROTATION` / `_FLIPDIRECTION` 三条 `shader_feature_local` **原样**；`#pragma multi_compile_local` 两条**原样**；
alphaclip 顺序（先乘裁切再 `clip(a-0.001)`）**原样**；`_ClipRect` 默认值**原样**。

**行尾**：改前 LF 270 / 改后 LF 333，**无裸 CR**（`python -I -X utf8` 二进制读的计数）。

---

## 四 断言（`B114_W36_Assert.cs`）

| ID | 断什么 | 为什么要它 |
|---|---|---|
| A0 | `Shader.Find("WarpforgeVFX/UI/UI Border Highlight")` 取得到 | 前提 |
| A1/A2 | `_UIMaskSoftnessX` / `_UIMaskSoftnessY` **真在属性表里**（`Material.HasProperty`） | 🧨 「只在 CGPROGRAM 里写了 `float _UIMaskSoftnessX;` 却没进 `Properties`」只有这里抓得住；**同时冒烟「shader 编得过」**（编译失败 ⇒ error shader ⇒ 恒 false） |
| A3/A4 | 两条默认值 = 1.0 | = ShaderGraph `CanvasProperties.cs` 的默认值 |
| A5 | `_ClipRect` 默认仍是「不裁」档 `(-32767,-32767,32767,32767)` | 软边公式下这条更重要：改成 0 会在关键字打开那一刻**整块消失** |
| B0 | 读得到 shader 源 | 前提 |
| B1 | `saturate((_ClipRect.zw - _ClipRect.xy - abs(i.clipRect.xy)) * i.clipRect.zw)` == 1 | 软边公式在位 |
| B2 | `o.a *= mk.x * mk.y;` == 1 | 真的乘进 alpha（不是只算不用） |
| B3 | `clamp(_ClipRect, -2e10, 2e10)` == 1 | 原版 VS 的 `max/min` 两条 |
| **B4** | **`v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw` == 1** | ★ **对象空间**（原版 `mad r1.xy, v0.xyxx, l(2,2), …`） |
| B5 | `0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize))` == 1 | 斜坡标度 |
| **B6** | 代码段 `UnityGet2DClipping` 出现 **0** 次 | 🔴 **灭自证 ①**：改回硬边 ⇒ 红 |
| **B7** | 代码段 `unity_ObjectToWorld` 出现 **0** 次 | 🔴 **灭自证 ②**：改回世界空间 ⇒ 红（且 B4 同时红） |
| B8 | `Properties` 里两条 softness 声明 + 默认 1 | 源码级复核（A1/A2 是运行时的） |
| B9 | `mk` 乘进 alpha 在 `#ifdef UNITY_UI_ALPHACLIP` **之前** | `B-262` 的骨架别推翻 |
| B10 | `o.clipRect` 在 `vert` 里、唯一的 `#ifdef UNITY_UI_CLIP_RECT` 在 `frag` 之内 | 原版 **VS 不受 clip 关键字影响**（16 段 VS 只有 3 个不同体）|
| B11 | 两条 `multi_compile_local` 仍各 1 条 | 别推翻骨架 |
| B12 | 三条 `shader_feature_local` 仍是 feature（且没有 `multi_compile_local _DEBUG`） | 原版出包会裁变体 |

> ⚠️ **B6/B7 必须剥注释再数**（文件头和 frag 注释里逐字引用了这两个旧名字），
> 断言里用 `StripComments()` 先去掉 `//` 与 `/* */` —— 不剥就是**必然假红**。

**离线自验（⛔ 不跑 Unity 也能验「断言会不会假红」）**：用同一套 `StripComments` 逻辑写成 python
（`_tmp_view/b114_w36/verify_asserts.py`）对**当前** shader 源跑了一遍，**17 项全 OK**：

```
OK B1..B5 = 1 · B6 UnityGet2DClipping = 0 · B7 unity_ObjectToWorld = 0 · B8 = 1/1
OK B11 = 1/1 · B12 = 1/1/1 且 multi_compile_local _DEBUG = 0
OK B9 order (8256, 8315, 8361) · B10 unconditional (4262, 4946, 6206, 8101)
FAILED = 0
```

⇒ 断言字符串与源码**逐字对得上**（不是「写了但匹配不到」），剩下的只是「`Run()` 有没有被宿主接上」。

**接线（⛔ 调度台做，我不改宿主）**：挤进 `Editor/BattleScene.cs` / `Editor/ShellScene.cs` 里**某一条既有的聚合行**，
**同一物理行、第一个 `//` 之前**：

```
B114_W36_Assert.Run(); pass += B114_W36_Assert.Pass; fail += B114_W36_Assert.Fail;
```

（那两族用的是局部变量 `pass` / `fail`；若宿主用 `_sink`，照那一行的变量名写。）

---

## 五 需要 Unity 验什么（⚠️ `typecheck` 不覆盖 shader）

`bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_w36`）——
但那**只证明 `.cs` 编得过**，**不证明 shader 编得过**。要在 Unity 里验：

1. **shader 能不能编译**（最要紧）。本波新增了三样在 CG/built-in 里没被其他 shader 用过的写法，逐条盯 log：
   - `UNITY_MATRIX_P[0][0]` / `[1][1]` —— 若报 `undeclared identifier` 或矩阵下标不支持，
     退路 = `float2(unity_CameraProjection[0][0], unity_CameraProjection[1][1])`（同一格）。
   - `_ScreenParams` 在 **vertex** 里用（UnityCG 里有，但本工程此前没有 vertex 用它的先例）。
   - `half2 mk` + `saturate` 在 frag。
2. **变体数**：加了两条 `multi_compile_local` 后总变体 = 3 feature(`_DEBUG`/`_AUTOROTATION`/`_FLIPDIRECTION` 交互 4 种)
   × 2 × 2 = **16**（原版 PS 也是 16 段）—— 数一下 `Shader` 面板里的变体数，**应该和原版一致**。
3. **跑 `B114_W36_Assert.Run`**（串行、`-executeMethod`）：A1–A5 只有在 shader 真编出来时才会绿。
4. **画面零变化**的确认：这两路今天**惰性**（没人 `SetVector("_ClipRect")`、没人 `EnableKeyword`）⇒
   现有渲染件（`CardDisplayEffects` 的升级边框 / `UIBorderGlow`）应当**逐像素不变**。
   若变了 ⇒ 说明 `o.clipRect` 那段意外影响了别的插值器（那是真回归，不是设计）。

---

## 六 没查清的部分（⛔ 不填猜测）

1. 🔴 **`_ClipRect` 到底是「对象空间」还是「画布空间」** —— R8 §五·4 明确没定案。
   字节码**确定**的是：shader 侧拿它去比 **`positionOS`**（这条本轮照抄了）。
   但 `RectMask2D` 交给 `CanvasRenderer.EnableRectClipping()` 的是**画布空间**
   （`RectMask2D.cs:226,281` 的 `m_LastClipRectCanvasSpace`、`Clipping.cs:25` 取 `canvasRect`），
   中间那步 native 换算**不在本地源码里**。
   ⇒ **后果**：将来谁真要喂 `_ClipRect`，必须在**运行时实测**「喂什么空间的矩形能让边缘落在正确位置」，
   ⛔ 别按「世界坐标」或「屏幕坐标」直接塞。本文件已把这条写进 `_ClipRect` 的声明注释里。
2. **`_UIMaskSoftnessX/Y` 谁来写** —— 本地 `com.unity.ugui` 的 C# 里 `grep _UIMaskSoftness` = **0 命中**
   （只有 ShaderGraph 那侧引用）。它是「材质常量」还是「Unity native 逐帧喂」**没定案**（R8 §五·3）。
   本文件按**材质属性**声明（我们这一侧只有这条路），默认 1.0。
   ⇒ 若将来发现原版是**逐帧按 RectMask2D 的 softness 覆盖**的，那我们要复刻就得在喂 `_ClipRect` 时**一起喂 `SetFloat`**。
3. **`pixelSize` 的首项**：本地 `CanvasPass.hlsl` 是 `positionCS.w`、原版字节码是字面量 `1.0`，
   说明当年编译用的 URP 包与本地这份**不是同一版**（R8 §五·1）。本文件按字节码写 `1.0`。
   只影响**非正交相机**下的 `pixelSize`（UI 恒正交 ⇒ 无差）。
4. **过渡带具体几像素**：`S + 4|pixelSize|`（默认 ≈ 3 个对象单位）是「套源码+默认值」推的，
   没有原版实拍量过（且这两路今天惰性，也量不出来）。

---

## 七 顺手发现（⛔ 一条都没动）

### 7·0 🔴 **`B114_W31_Assert.cs` 会有 2 条断言变红（本波改动的直接后果，须调度台改）**

`B114_W31_Assert.cs:188` 把裁切算式**写死成旧写法**去搜：

```csharp
int iClip = IdxFrom(shSrc, "UnityGet2DClipping(i.worldXY, _ClipRect)", iIf + 1);   // ← 现在必然 -1
```

⇒ 它的 **B3**（`iClip > iIf`）与 **B5**（`iClip >= 0 && iAlpha > iClip`）**会红**。
这不是功能缺陷 —— 是「断言钉住了一个**具体实现写法**」，而那处写法按判据必须换掉。

**⚠️ 该文件不在本写手的白名单里（只许改 shader + `B114_W36_Assert.cs`）⇒ 我没动它。**
可粘贴补丁（`B114_W31_Assert.cs:188`，**只改这一行 + B3 的文案那一行**）：

```csharp
                // 🔴 `B-273`（W-36）：裁切算式已按原版字节码换成**软边**（`saturate` 斜坡 + `positionOS`），
                //    原锚点 `UnityGet2DClipping(i.worldXY, _ClipRect)` 已不存在 ⇒ 改锚到新算式。
                int iClip = IdxFrom(shSrc, "o.a *= mk.x * mk.y;", iIf + 1);
```

```csharp
                Check(iIf >= 0 && iClip > iIf,
                      "B3 裁切**真的乘进 alpha**（`o.a *= mk.x * mk.y;` 在 `#ifdef` 之内；算式见 `B-273`）"
                      + "（" + iIf + " / " + iClip + "）");
```

`B4` / `B5` 的判定式与 `iIfA` / `iAlpha` **不用改**（`clip (o.a - 0.001)` 从 `iIfA+1` 起搜，命中的仍是代码那一处 ——
文件头注释里那处更早，不在搜索区间内）。
**验证**：改完 `B114_W31_Assert.Run` 的 B3/B5 应回绿；`B114_W36_Assert` 的 B9 与它断的是**同一件事**（顺序）。

1. ⚪ **`ImageQuad` 的网格顶点本来就是「世界单位量级的局部坐标」**（`ImageQuad.cs:495`：
   `verts` 用 `x0 = -anchor.x * WorldW` 这类值，`localScale` 默认 1）⇒ 它**对象空间 = 世界空间**在这种常见配置下**恰好相等**。
   🔴 这正说明 `B-273` 不是纸上差异：**只要哪天谁给这个 quad 的父链加个缩放/位移（比如 `_nineOn` 那条九宫格态、
   或者 `CardDisplayEffects` 里被父级缩放），两者立刻分叉**，而且是**静默**分叉（画面只是裁切位置差一截）。
2. 🟠 **`ImageQuad` 是 `MeshRenderer`，没有 `CanvasRenderer`** —— 所以 uGUI 那条「`RectMask2D` 自动开关键字 + 自动喂
   `_ClipRect`」的链路**永远接不上**。要真用这条裁切，得**自己写一个喂参的 C# 组件**（`SetVector` + `EnableKeyword`）。
   今天没有这样的消费者 ⇒ 这条路仍然惰性，与 `B-262` 记的一致。
3. ⚪ **文件头差异⑧ 原文写的「按 uGUI `UI/Default` 的约定补」现在只对了一半** ——
   关键字声明/顺序/alphaclip 确实照 `UI/Default`，但**裁切算式不是**（`UI/Default` 是硬边）。
   已在文件头就地写明，⛔ 别再按「照 `UI/Default` 抄」去理解这一路。
4. 🟠 **`o.clipRect` 在非 clip 变体里是「写了但没人读」的插值器**（原版也这样：`o3` 无条件算）。
   它会让非 clip 变体也白算 `_ScreenParams` / `UNITY_MATRIX_P` 几条指令 —— 这是**照原版**，⛔ 别为了省指令
   把它包进 `#ifdef`（那条会与「VS 随关键字分叉」的原版事实冲突，断言 B10 会红）。
