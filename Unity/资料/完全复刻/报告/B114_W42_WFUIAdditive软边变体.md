# `B114_W42` · `WFUIAdditive` 补齐 b9 / b10 两个软边 clip 变体

> 写手 `W-42` · 2026-11-01 · 任务 `B-272`。
> ⛔ 没跑 Unity（`-executeMethod` 全禁）· ⛔ 没动 git · 只改了一个 shader + 新建一个断言类。
>
> **备份**：`d:/4/_tmp_view/b114_w42/WFUIAdditive.shader.bak`（改前 **144 行 / 7,943 B / 纯 LF / 裸 CR 0**）
> **自验脚本**：`d:/4/_tmp_view/b114_w42/verify_asserts.py`（离线跑断言串）· `verify_blob.py`（解原版 blob 扫名字）

---

## 〇 一句话

`WFUIAdditive.shader` 原来只有 **b7（catch-all）+ b8（`UNITY_UI_ALPHACLIP`）**，缺的 **b9（只有 `UNITY_UI_CLIP_RECT`）
与 b10（两条都要）** 已按 **`Additive` 自己**的四个编译产物逐指令补齐：软边 `clamp` mask、`positionOS` 对象空间、
`gl_Position.w` 那一条 `pixelSize`、以及**反过来的 pragma 顺序**（ALPHACLIP 在前）。b7 那一档**算式一字未动**。

⚠️ **路径订正（铁律 5）**：任务简报写 `Assets/WarpforgeVFX/Shaders/WFUIAdditive.shader`，**该文件不存在**；
真身 = **`Assets/CardPresentation/Resources/Shaders/WFUIAdditive.shader`**（`B110_W5_ShaderSupport_Assert.cs:94` 也是这么记的）。
另一个 `Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` 是**另一颗**（本次一行未动）。

---

## 一 原版 4 档逐档还原（判据 → 我们的落点）

### 1·1 判据（三条，都是本机实读）

| 什么 | 在哪 |
|---|---|
| 原版资产本体 | `d:/2/新解包资源/assets_full/bundle_menus_assets_all/Shader/Shader_4921318991789134610.json` |
| ① 名字/关键字 | `m_ParsedForm.m_Name = "Everguild/UI/Additive"`；`m_KeywordNames` 末两条 = `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`，`m_KeywordFlags` 都是 **1 = local** |
| ② shaderlab 四分支 | `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Additive.shader`：`:44` ALPHACLIP pragma、`:45` CLIP_RECT pragma、`:48-58` VS 四分支、`:62-73` 片元四分支 |
| ③ 逐变体真源码 | 同目录 `Everguild_UI_Additive/`：`Sub0_Pass0_Vertex_b1..b4.hlsl` + `Sub0_Pass0_Fragment_b7..b10.hlsl` |
| ④ 原版材质 | `…/bundle_menus_assets_all/Material/Material_-8464978790139598553.json` · `m_ValidKeywords = []` |

> 🔴 **四份 VS 逐字节相同**（`diff b1 b2` / `b1 b3` / `b1 b4` 只差首行 `Keywords:` 注释）⇒ 裁切插值器
> **无条件算**、与 clip 关键字无关。这一条直接决定「`o.clipRect` 不许包进 `#ifdef`」。

### 1·2 四档对照（原版指令 → 我们的代码）

| 档 | 关键字 | 原版片元 | 原版关键指令 | 我们 |
|---|---|---|---|---|
| **b7** | （无） | `Fragment_b7.hlsl:29-49` | `_34=round(a255)/255`；`_82=_34*tex.a`；`out=_82*COLOR*tex` | **一字未动**（原就有） |
| **b8** | `UNITY_UI_ALPHACLIP` | `Fragment_b8.hlsl:28-54` | `if (mad(_34,_71,-0.001) < 0) discard;` | 原就有，**位置没动** |
| **b9** | `UNITY_UI_CLIP_RECT` | `Fragment_b9.hlsl:32-60` | 见 1·3 | 🆕 本轮新增 |
| **b10** | 两条都要 | `Fragment_b10.hlsl:32-64` | b9 的 mask + b8 的 discard，**discard 判 `_83*_115`（乘完 mask）** | 🆕 由两段自然合成 |

### 1·3 软边 mask（b9 `:34-40` / b10 `:45-51`）逐条对上

原版（`precise float` 逐行）：

```
_42 = _ClipRect.z - _ClipRect.x          // w
_43 = _ClipRect.w - _ClipRect.y          // h
_53 = _42 - abs(TEXCOORD_2.x)            // w - |t.x|
_54 = _43 - abs(TEXCOORD_2.y)            // h - |t.y|
_61 = _53 * TEXCOORD_2.z
_62 = _54 * TEXCOORD_2.w
_67 = clamp(_62,0,1) * clamp(_61,0,1)    // ★ 先 Y 后 X
...
_115 = _73 * _105                        // a255 * tex.a
_116 = _67 * _115                        // ★ alpha *= mask
SV_Target = (_116*COLOR.rgb*tex.rgb, _116)
// b10 才有：if (mad(_83, _115, -0.001) < 0) discard;   ← 判的是**乘完 mask** 的 alpha
```

我们（`frag`，`#ifdef UNITY_UI_CLIP_RECT` 之内）：

```hlsl
float mW = clamp(((_ClipRect.z - _ClipRect.x) - abs(i.clipRect.x)) * i.clipRect.z, 0.0, 1.0);
float mH = clamp(((_ClipRect.w - _ClipRect.y) - abs(i.clipRect.y)) * i.clipRect.w, 0.0, 1.0);
outA *= mH * mW;
```

`mH * mW` 与 `_67` 的乘序一致；`outA` 就是 `_115`；discard 排在它后面 ⇒ b10 语义自动成立（b8 里 mask 恒 1，同值）。

### 1·4 顶点裁切插值器（b1 `:94-107`，四档共用）

```
_255 = _158 / abs(mad(P[0u].x, SP.x, SP.y * P[1u].x))      // pixelSize.x
_256 = _158 / abs(mad(P[0u].y, SP.x, SP.y * P[1u].y))      // pixelSize.y   （_158 = gl_Position.w）
_269 = 0.25 / mad(_UIMaskSoftnessX, 0.25, abs(_255))
_270 = 0.25 / mad(_UIMaskSoftnessY, 0.25, abs(_256))
TEXCOORD_2.zw = (_269, _270)
TEXCOORD_2.x  = mad(POSITION.x, 2.0, -clamp(_ClipRect.x,±2e10)) - clamp(_ClipRect.z,±2e10)
TEXCOORD_2.y  = mad(POSITION.y, 2.0, -clamp(_ClipRect.y,±2e10)) - clamp(_ClipRect.w,±2e10)
```

我们：`o.clipRect = float4(v.vertex.xy*2 - clampedRect.xy - clampedRect.zw, 0.25/(0.25*float2(sx,sy) + abs(pixelSize)))`，
`pixelSize = o.pos.w / abs(proj2x2)`，`proj2x2 = (P00*SP.x + SP.y*P10, P01*SP.x + SP.y*P11)`。

> `P` = `glstate_matrix_projection` = `UNITY_MATRIX_P`（`UnityShaderVariables.cginc:27` 是 `#define`，**逐字同义**），
> 而 `o.pos` 就是 `UnityObjectToClipPos` 的结果 ⇒ `o.pos.w` 就是原版 `_158`。**不是推断**。

---

## 二 我们原来缺什么（逐条）

| # | 缺的东西 | 原版在哪 | 后果 |
|---|---|---|---|
| 1 | `#pragma multi_compile_local _ UNITY_UI_CLIP_RECT` | `:45` | b9/b10 **根本编不出来** |
| 2 | `_ClipRect` / `_UIMaskSoftnessX` / `_UIMaskSoftnessY` 三条属性 | `type_Globals` c4 / c6.x / c6.y（`b1:10-13`） | 片元无从裁切、顶点无从算斜坡 |
| 3 | 顶点裁切插值器（整个 `TEXCOORD_2`） | `b1:94-107`（四档 VS 都算） | 软边公式没有输入 |
| 4 | 片元软边 mask + `alpha *= mask` | `b9:34-40` / `b10:45-51` | b9/b10 的**本体** |
| 5 | pragma **顺序**（ALPHACLIP 在前） | `:44` → `:45` | 顺序错 = 与原版不符（且是「照抄 BorderHighlight」的指纹） |

原来文件末那段「**b9 / b10 本轮没做**」的理由（材质 keywords 为空、我们走 `MenuDraw.Clip*` 不是 `RectMask2D`，
「⇒ 这条路上没有消费者」）**已按 `B-262` 口径作废**：铁律 11 / 18·0 —— 「惰性」只决定**先后**，不决定**做不做**。
那段已就地改写成「消费者现状（如实记）」，**保留了「今天恒不生效」这个事实**。

---

## 三 改动 + 净增净减

| 文件 | 改了什么 | 量 |
|---|---|---|
| `Assets/CardPresentation/Resources/Shaders/WFUIAdditive.shader` | ① 文件头：加「四档变体」「与 BorderHighlight 的三处不同」「`_ClipRect` 来历（名字有证据 / 默认值是推断）」三节，并把「未复刻的变体」改写成「消费者现状」② `Properties` 加 `_ClipRect` + `_UIMaskSoftnessX/Y`（全 `[HideInInspector]`）③ pragma 加 `UNITY_UI_CLIP_RECT`（**排在 ALPHACLIP 之后**）④ `v2f` 加 `float4 clipRect : TEXCOORD1` ⑤ 变量声明加三条 ⑥ `vert` 末尾加裁切插值器（**无条件**）⑦ `frag` 加 `#ifdef UNITY_UI_CLIP_RECT` 软边 mask，并把它排在 alphaclip 之前 | **144 → 242 行（+114 / −16，净 +98）** |
| `Assets/CardPresentation/Editor/B114_W42_Assert.cs`（新建 353 行）+ `.cs.meta` | 35 条断言：A0–A7（运行时属性表 / 默认值）· B0–B17（源码级，含 3 条灭自证 + 顺序 + 对象空间）· C0–C6（C# 镜像，含 2 条灭自证） | 新文件 |

**⛔ 一行没动**：`WFUIBorderHighlight.shader` · `WildcardShine.shader` · `WFUIFxShine.shader` · `Shell/ItemDrawer*.cs` · `RuleEngine/**` ·
`Editor/BattleScene.cs` · `Editor/ShellScene.cs`（**宿主接线留给调度台**）· 两份正本 · `账/*.tsv` · `Scenes/*.unity`。

**行尾**：改前 LF 144 / 改后 LF 242，**裸 CR 0**（`io.open(...,'rb')` 二进制计数）。
**账**：`grep -rn "WFUIAdditive" 账/` = **0 命中** ⇒ 行号变化不会造成账漂移。

### 3·1 断言清单（`B114_W42_Assert.cs`，共 35 条）

| ID | 断什么 | 为什么 |
|---|---|---|
| A0 | `Shader.Find("WarpforgeVFX/UI/Additive")` 取得到 | 前提 |
| A1–A3 | `_ClipRect` / `_UIMaskSoftnessX` / `_UIMaskSoftnessY` **真在属性表里** | 🧨 「只在 CGPROGRAM 里写了 `float4 _ClipRect;` 却没进 `Properties`」只有这里抓得住；**同时冒烟「shader 编得过」**（编译失败 ⇒ error shader ⇒ 恒 false） |
| A4 | `_ClipRect` 默认 = `(-32767,-32767,32767,32767)` | 🧨 是 0 ⇒ 关键字一打开**整块消失** |
| A5/A6 | 两条 softness 默认 = 1.0 | = ShaderGraph Canvas 默认值 |
| A7 | b7 骨架五条属性都还在 | 别把既有东西改没 |
| B0 | 读得到 shader 源 | 前提 |
| B1/B2 | 两条 `multi_compile_local` **各恰 1 条** | 原版 flag=1=local 且是 multi_compile |
| **B3** | ★ **pragma 顺序：ALPHACLIP 在前、CLIP_RECT 在后** | 🔴 反过来 = 照抄了 BorderHighlight（那族原版是反的） |
| **B4** | ★ 插值器由 **`positionOS`** 算 | 对象空间 |
| B5 | `clamp(_ClipRect, -2e10, 2e10)` 在 | 原版 `max/min` 两条 |
| B6 | 斜坡标度 `0.25/(0.25*S + abs(pixelSize))` | 原版 `mad`+`div` |
| **B7** | ★ `pixelSize` 分子是 **`o.pos.w`**，且**没有** `1.0 / abs(float2(UNITY_MATRIX_P…))` | 🔴 **灭自证**：照抄 BorderHighlight 的「字面量 1.0」⇒ 红 |
| B8 | 投影项形状 = `P00*SP.x + SP.y*P10`（不是单乘对角线） | 原版 `mad(P[0].x, SP.x, SP.y*P[1].x)` |
| B9 | 软边 mask 两条 `clamp(...)` | 本体 |
| B10 | `outA *= mH * mW;` | 🧨 算了不乘 ⇒ 红 |
| B11 | 顺序：mask → alphaclip → 输出 | b10 的 discard 判「乘完 mask」的 alpha |
| **B12** | 代码段 `UnityGet2DClipping` = **0** | 🔴 **灭自证**：改回硬边 ⇒ 红 |
| **B13** | 代码段 `unity_ObjectToWorld` = **0** | 🔴 **灭自证**：改回世界空间 ⇒ 红 |
| B14 | `o.clipRect` 在 `vert` 里**无条件**赋值（唯一的 `#ifdef` 在 `frag`） | 四档 VS 逐字节相同 ⇒ VS 不许随关键字分叉 |
| B15 | `Properties` 三条声明 + 默认值 | A1–A6 的源码侧复核 |
| B16 | `UNITY_UI_CLIP_RECT` **不是** `shader_feature*` | 原版四档产物全在包里 |
| B17 | b7 五条锚点（量化 / 预乘 / `_TextureSampleAdd` / `v.color*_Color`） | 别为了加 b9/b10 把 b7 改坏 |
| C0 | 斜坡标度 = 1/3（S=1、\|p\|=0.5） | 过渡带 ≈ 3 对象单位 |
| C1/C1b | 矩形正中 mask=1、正好在边界 mask=0 | 公式方向对 |
| **C2/C2b** | ★ 过渡带内（x=9.25）mask ∈ **(0,1) 开区间**，且与硬边（=1）**可分** | 🔴 **灭自证**：把 `clamp` 改成 `step` ⇒ 这里恒 0/1 ⇒ 红 |
| C3 | 越过边界（tx=11）⇒ 0 | 另一侧 |
| C4 | 从中心往外 **单调不增** | 挡「h/w 两半乘反」 |
| C5/C6 | mask 1→0.5 ⇒ outA 恰好减半；mask=0 ⇒ rgb=0（与「非预乘 = 1」可分） | 🔴 **灭自证**：漏乘 / 非预乘 |

> ⚠️ **B12/B13 必须剥注释再数**（文件头和 `frag` 注释里逐字引用了反例名 `UnityGet2DClipping`）——
> 断言里用 `StripComments()` 先去掉 `//` 与 `/* */`；不剥就是**必然假红**。

**离线自验（⛔ 不跑 Unity 也能验「断言会不会假红」）**：同一套 `StripComments` 写成 python
（`_tmp_view/b114_w42/verify_asserts.py`）对**当前** shader 源跑了一遍 —— **21 项全 OK，`FAILED = 0`**：

```
OK B1 =1 · B2 =1 · B3 order (2115, 2176) · B4 =1 · B5 =1 · B6 =1 · B7 (1, 0) · B8 (1,1)
OK B9 (1,1) · B10 =1 · B11 order (4598/4709/4755/4846) · B12 =0 · B13 =0
OK B14 (2968/3736/.../4324) · B15 (1,1,1) · B16 (0,0) · B17 (1,1,1,1,1)
OK M3-blend / M3-nolightmode / M3-stencil / M3-shadername   ← 既有 B110_M3 的锚点没被本波破坏
FAILED = 0
```

（第一遍 **B14 曾红**：我按 BorderHighlight 的 `float4 frag (v2f i)` 写锚点，而本 shader 是 **`fixed4 frag (v2f i)`** ⇒ 已订正。
这正是「离线自验」存在的意义 —— 写在 C# 里但匹配不到 = 永假绿。）

**接线（⛔ 调度台做，我不改宿主）**：挤进 `Editor/BattleScene.cs` / `Editor/ShellScene.cs` 里**某一条既有的聚合行**，
**同一物理行、第一个 `//` 之前**：

```
B114_W42_Assert.Run(); pass += B114_W42_Assert.Pass; fail += B114_W42_Assert.Fail;
```

（`BattleScene.cs` 那族用局部变量 `pass` / `fail`；`ShellScene.cs` 那族用 `_sink.Pass` / `_sink.Fail` —— 照那行的变量名写。）

---

## 四 哪些是**推断**（⛔ 别当已证事实）

代码里都写了 `// 推断（非判据）：…`。本轮只有**三条**，而且都在「值」不在「算式」：

1. **`_ClipRect` 的默认值 `(-32767,-32767,32767,32767)`** —— 取自 uGUI 的「不裁」档（`UI/Default` 的惯例）。
   原版由 native 喂，**字节码里读不到默认值**。
2. **`_UIMaskSoftnessX/Y` 的默认值 `1.0`** —— 取自 ShaderGraph Canvas 目标 `CanvasProperties.cs` 那条属性。
3. **过渡带绝对宽度 ≈ 3 个对象单位**（`S + 4|pixelSize|`，S=1、|p|=0.5）—— 「套源码 + 默认值」推的，**没有从原版实拍量过**。

### ✅ 反过来：这三条**不是**推断（与 BorderHighlight 那一族不同）

| 项 | BorderHighlight 那边 | 本条（Additive） |
|---|---|---|
| `_ClipRect` 的名字 | RDEF 被剥 ⇒ **推断** | ✅ **有名字作证**（见下） |
| `_UIMaskSoftnessX/Y` 的名字 | **推断** | ✅ **有名字作证** |
| `_ScreenParams` / 投影矩阵 | **推断**（cb0[23]/cb0[62]/[63]） | ✅ 原版 cbuffer 声明里逐字就是 `_ScreenParams` / `glstate_matrix_projection` |

**取证**：`_tmp_view/b114_w42/verify_blob.py`（用 `dump_shader_blob.py` 同一套 LZ4 切块逻辑）解 `Shader_4921318991789134610.json`
的 `compressedBlob`（2 平台 / 5,186 B → 9,984 + 12,504 B），名字表里逐字含：

```
_ClipRect · _UIMaskSoftnessX · _UIMaskSoftnessY · _TextureSampleAdd · _MainTex_ST · _Color
_ScreenParams · glstate_matrix_projection · unity_MatrixVP · unity_ObjectToWorld
```

⇒ 本条**没有「RDEF 被剥」这个问题**，所以不比 BorderHighlight 需要更多「推断」标注。

---

## 五 要 Unity 验什么（⚠️ `typecheck` 不覆盖 shader）

`TMPDIR=/tmp/wf_w42 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**（跑了**两次**：改 shader 后、改断言锚点后）
—— 但那只证明 **`.cs` 编得过**，**不证明 shader 编得过**。要在 Unity 里验：

1. **shader 能不能编译**（最要紧）。本波用到三个「本工程此前没有先例」的写法，逐条盯 log：
   - `UNITY_MATRIX_P[0][0]` / `[0][1]` / `[1][0]` / `[1][1]`（矩阵双下标）。退路 = `glstate_matrix_projection[i][j]`（同一格，`UNITY_MATRIX_P` 就 `#define` 到它）。
     ⚠️ `WFUIBorderHighlight.shader` 也用了 `UNITY_MATRIX_P[0][0]/[1][1]`（`B-273`，**同样没在 Unity 里验过**）⇒ 这两颗可以**一起**看。
   - `_ScreenParams` 在 **vertex** 里用（`CBUFFER_START(UnityPerCamera)`，理论上两阶段都绑）。
   - `clamp(float4, scalar, scalar)`（标量广播）—— `WFUIBorderHighlight` 已有同形先例。
2. **变体数**：加了一条 `multi_compile_local` 后总变体 = **2 × 2 = 4**（原版也正好 4 档 b7/b8/b9/b10）。
   数一下 Shader 面板的变体数，**应该 4**；多于 4 = 有人顺手加了别的关键字。
3. **跑 `B114_W42_Assert.Run`**（串行、`-executeMethod`）：A1–A7 只有在 shader 真编出来时才会绿。
4. **b7 画面零变化**：这颗 shader 的**唯一生产消费者**是 `Shell/ParentEventWindow.cs:2589` 的 `Shader.Find("WarpforgeVFX/UI/Additive")`
   （材质 `Art/vfx/WFUI_Additive.mat`，`m_ValidKeywords = []` ⇒ 走 b7）。b7 那段算式**一字未动**，
   `vert` 只是**多算了两条没人读的插值**⇒ 正常应**逐像素不变**。若变了 ⇒ `o.clipRect` 那段意外影响了别的插值器，那是真回归。
5. **b9/b10 无法用画面验**（今天没有任何数据源）—— 只有 A/B 断言能证明「编得出来、算式在」。

---

## 六 没查清的部分（⛔ 不填猜测）

1. 🔴 **`_ClipRect` 到底是「对象空间」还是「画布空间」** —— 与本条 `B-273`（BorderHighlight）同一个未决问题。
   字节码**确定**的是：shader 侧拿它比 **`positionOS`**（`b1:104-107` 逐字 `POSITION.x*2 - clamp(rect.x) - clamp(rect.z)`），
   这条本轮**照抄**了。但 `RectMask2D` 交给 `CanvasRenderer.EnableRectClipping()` 的是**画布空间**，
   中间那步 native 换算**不在本地源码里** ⇒ 将来谁真要喂 `_ClipRect`，必须**运行时实测**「喂什么空间的矩形能让边缘落在正确位置」。
2. **`_UIMaskSoftnessX/Y` 谁来写** —— 本机 `com.unity.ugui` 的 C# 里 `grep _UIMaskSoftness` = 0 命中（只有 ShaderGraph 那侧引用）。
   「材质常量」还是「native 逐帧喂」**没定案**。本轮按**材质属性**声明（我们这一侧只有这条路），默认 1.0。
3. **`TEXCOORD_1_1`（原版 VS 的 `POSITION` 原样透传，`:90-93`）没有任何片元读它** ——
   四条片元的 `SPIRV_Cross_Input` 里都没有它（只有 COLOR / TEXCOORD / TEXCOORD_2）⇒ 本轮**不建**这个插值器
   （建了就是一条没人读的 dead interpolant）。**没查到**它在原版里到底被谁消费（如果真有人消费，那是别的 pass / 别的 shader 的事）。
4. **`platforms = [4, 18]`（DX11 + 另一个）两份 blob 的差异没逐段 diff** —— 本轮只用了 AssetRipper 反编译产物（与
   `m_KeywordNames`/`m_PropInfo` 交叉自洽：名字、关键字 flag、四档产物差异全对上）。若将来怀疑「两个平台编译出的算式不同」，
   得把两份 blob 分别反汇编对一眼（本轮**没做**）。

---

## 七 顺手发现（⛔ 一条都没动）

### 7·0 🟠 `WFUIFxShine.shader:24` 有一句**因本波而变旧**的交叉引用（建议调度台改）

原文（现读）：

```
//   ⇒ 另外三档（b1 / b3 / b4）**没有任何消费者**，本轮不复刻（与 `WFUIAdditive.shader` 对 b9/b10 的处置同一条口径）。
```

`WFUIAdditive` 的 b9/b10 **本轮已经补了** ⇒ 这句括号里的类比**不再成立**（而且它本身就是「惰性 ⇒ 不做」那套已作废口径的残留）。
**该文件不在本写手白名单里（只许改 `WFUIAdditive.shader` + 新建 `B114_W42_Assert.cs`）⇒ 我没动它。**
可粘贴改法：把括号里那句换成「（⛔ 不是『惰性 ⇒ 不做』：`WFUIAdditive` 的 b9/b10 已于 2026-11-01 `B-272` 补齐）」。
⚠️ 改之前请**先确认它自己那三档**（b1/b3/b4）到底该不该补 —— 那是另一条账，本波不判。

### 7·1 ⚪ `WildcardShine.shader:51-52` 那处措辞仍未改（`B-274` 已记）

「该实例上全是 None ⇒ 这两个关键字变体里**也不会被编进去**」—— 对 `#pragma multi_compile` **不成立**（一定会编、只是运行时不选）。
`B114_W33 §3·3(b)` 已记，`B-274` 仍是「整批变体没做」⇒ 本波**不单摘**，只重申一次「别只补 clip」。

### 7·2 ⚪ `B114_W31_Assert.cs:188` 的过期锚点**已经被别人修好了**

`B114_W36` 报告 §7·0 提过它把 `UnityGet2DClipping(i.worldXY, _ClipRect)` 写死成锚点、会因 `B-273` 变红。
本波现读：`:188` 已经是 `IdxFrom(shSrc, "o.a *= mk.x * mk.y;", iIf + 1)` ⇒ **无需再改**（省一条待办）。

### 7·3 🔴 一个**越界**的隐患：`B110_M3_MenuMat_Assert` 与本次改动**不冲突**（已逐条核过，写下来免得后人重查）

它 `CheckShaderSource()` 里断的五条锚点（`Blend One One, One One` / `round(i.color.w * 255.0)` /
`outA * i.color.rgb * tx.rgb` / `tx.rgb, outA)` / `v.color * _Color` / 无 `OneMinusSrcAlpha` / 无 `LightMode`）
本波**全部保留**，且我把它写进了 `B114_W42_Assert` 的 **B17 + 离线自验的 `M3-*` 四项**（双保险）。

### 7·4 ⚪ 「四档 VS 逐字节相同」这条**以后能省事**

它意味着「本 shader 的 VS 永远不随 clip/alphaclip 分叉」。下次谁要改 `vert`，**一次改就是四档全改**，
不需要（也不该）为某一档单独加分支 —— `B14` 就是钉这一条。
