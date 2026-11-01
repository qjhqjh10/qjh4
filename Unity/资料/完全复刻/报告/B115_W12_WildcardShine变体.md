# `B115_W12` · `B-274` —— `WildcardShine` 的原版变体清单 与 变体补齐

> 写手 `B115_W12` · 2026-11-01。
> ⛔ 没跑 Unity 批处理 · ⛔ 没动 git · ⛔ 没碰任何 `.cs` / 其它 `.shader` / `账/*.tsv` / `活状态表.md` / 正本。
> 只改了 **1 个 `.shader`**：`Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WildcardShine.shader`（本件所有权内）。

---

## 〇 一句话

原版这颗 shader 的变体不是「9+ 个」，而是 **6 个 `multi_compile_local` 关键字 / 20 个 fragment blob（= 5 个效果体 × 4 个裁剪修饰）+ 4 个 vertex blob（**体完全相同**）**。
其中 **TONE_GRAYSCALE / TRANSITION_DISSOLVE / EDGE_SHINY / UNITY_UI_ALPHACLIP / UNITY_UI_CLIP_RECT 五路本波补齐**（TRANSITION_SHINY 原来就有，逐项保留）；
文件头同时**就地订正**了「该实例 keywords 全 None ⇒ 变体不会被编进去」那句错话（对 `multi_compile` 不成立）。

---

## 一 判据定位（**搜过哪些目录、哪些词**，便于复核 / 避免重复搜）

| 搜的东西 | 结果 |
|---|---|
| `find d:/2/tools/ruri_out_1020_allshaders -iname "*UIEffect*" / "*Hidden_UI*"` | **0 命中** —— ⚠️ **简报指的那个目录里没有这颗 shader**（那里只有 99 份 `.shader`，全是 Everguild 的） |
| `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/` | ✅ **有**（这里 304 份）—— `Hidden_UI_Default (UIEffect).shader`（YAML，114 行）+ 同名目录下 **`Sub0_Pass0_Fragment_b13..b32.hlsl`（20 份）+ `Sub0_Pass0_Vertex_b1..b4.hlsl`（4 份）** |
| `d:/2/tools/assetripper_out/ExportedProject/Assets/Shader/Hidden_UI_Default (UIEffect).shader` | 只有 `//DummyShaderTextExporter` 生成的假体（64 行）⇒ **不能当判据** |
| `d:/2/tools/decomp_full` 里 `Coffee.UIEffects.UIEffect__*` | **0 个**（该类的方法体没被导出）⇒ 拿不到 C# 侧口径，只能以**编译产物字节**为准 |
| `d:/2/tools/il2cpp_out/dump.cs` 的 `Coffee.UIEffects.UIEffect` / `UIEffectBase` | ✅ 字段表在（`m_Transition*` / `m_Tone*` / `m_Edge*` / `m_Shadow*` 全有）；**`UIEffectBase`（TypeDefIndex 20380，`:1068820`）声明 `IMeshModifier`，`public virtual void ModifyMesh(VertexHelper vh)` 在 `:1068886` 附近** —— 见 §六·7 |

**唯一判据 = `rootshaders` 里那份 shader YAML（6 条 `#pragma` 与 20 条 `#if/#elif` 的精确条件）+ 20 份 fragment / 4 份 vertex 的编译产物。**
每份产物**第 2 行就写着自己的 `// Keywords:`**，这是「哪个 blob 属于哪一档」的硬证据。

---

## 二 原版变体表（关键字组合 / 体是否相同 / 判据出处）

### 2·1 YAML 里的 6 条关键字（逐字）

判据 = `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Hidden_UI_Default (UIEffect).shader:44-49`

```
#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
#pragma multi_compile_local _ TONE_GRAYSCALE
#pragma multi_compile_local _ TRANSITION_DISSOLVE
#pragma multi_compile_local _ TRANSITION_SHINY
#pragma multi_compile_local _ EDGE_SHINY
```

⚠️ **这就是「`multi_compile` 一定会编」的直接证据** —— 旧注释把「该实例 keywords 全 None」当成了「不会被编进去」，**对 `multi_compile` 不成立**（只有 `shader_feature` 才会按材质用量剥离）。

### 2·2 构建产物只有 20 + 4 份 ⇒ 变体选择是「**精确匹配**」

同一份 YAML 的 `:67-108` 是一串 `#if defined(A) && !defined(B) && …` 的 `#elif` 链，**末尾是 `#else` → `b13`（catch-all = 基础档）**。
⇒ 名义 6 个二态关键字 = 64 组合，但**真正有独立体的只有「4 个效果里恰有 1 个」×「2 个 UI 关键字任意」= 5 × 4 = 20**，其余全部落 `b13`。

| blob | 关键字（`// Keywords:` 现读） | 与同效果其它裁剪档比 | 判据 |
|---|---|---|---|
| **b13** | `<none>`（catch-all） | 基础档 | `Sub0_Pass0_Fragment_b13.hlsl:2` |
| b14 / b19 / b24 / b29 | `TONE_GRAYSCALE` ＋ 无 / `ALPHACLIP` / `CLIP_RECT` / 两者 | **同一个效果体**（行数 532 → 536 → 547 → 551，增量恒 **+4 / +15 / +19**） | b14:2 · b19:2 · b24:2 · b29:2 |
| b15 / b20 / b25 / b30 | `TRANSITION_DISSOLVE` ＋ 同上 | 同上（735 → 739 → 750 → 754，**同一组 +4/+15/+19**） | b15:2 … b30:2 |
| b16 / b21 / b26 / b31 | `TRANSITION_SHINY` ＋ 同上 | 同上（745 → 749 → 760 → 764） | b16:2 … b31:2 |
| b17 / b22 / b27 / b32 | `EDGE_SHINY` ＋ 同上 | 同上（870 → 874 → 885 → 889） | b17:2 … b32:2 |
| b18 / b23 / b28 | `ALPHACLIP` / `CLIP_RECT` / 两者（**无效果关键字**） | = **b13 基础档 + 裁剪修饰**（83 → 87 → 98 → 102，**同一组 +4/+15/+19**） | b18:2 · b23:2 · b28:2 |
| b1 / b2 / b3 / b4（vertex） | 无 / `ALPHACLIP` / `CLIP_RECT` / 两者 | ✅ **体完全相同** | 见 2·3 |

🔑 **「同一组 +4 / +15 / +19」本身就是判据**：四个效果行的裁剪增量与基础档**逐值相同**
⇒ 裁剪档 = 「同体 + 一段固定附加代码」，**不是另一个效果体**。
（注：原始行数用的是「剥掉 `//` 注释后」的行数，避免注释行干扰。）

### 2·3 「只差注释 = 同一个体」的**反证**（vertex 四份）

```
$ diff Sub0_Pass0_Vertex_b1.hlsl Sub0_Pass0_Vertex_b2.hlsl
1,2c1,2
< // Stage: Vertex  Blob: 1  ParamBlob: 0  Language: hlsl     > // Stage: Vertex  Blob: 2  ParamBlob: 0  Language: hlsl
< // Keywords: <none> (default / catch-all variant)            > // Keywords: UNITY_UI_ALPHACLIP
```
b1↔b3、b1↔b4 同形（**只有第 1–2 行不同**）。
⇒ **vertex 的 4 档是同一个体**，⛔ **不需要按关键字去实现 4 份 vertex**；本文件仍然只有一份 `vert`。

### 2·4 ⚠️ 一个**不能**用的判据：整份 `diff` 的行数

同一段算式在不同变体里 **SSA 变量会被重新编号**（`_433` → `_411` → `_397` …），
所以 `diff b16 b21` 会报 **+251 −247 行**，而**实际只多了一句 `discard`**。
⇒ 判「体是否相同」只看 ① 算式与常量 ②（本表用的）**剥注释后的行数增量**，⛔ 别看 diff 行数。

---

## 三 实现了哪几条 / 哪几条判「不需要」及反证

### 3·1 ✅ 本波实现（全部写进 `WildcardShine.shader`，每条都标了原版变量名可回核）

| 档 | 原版出处 | 我们写的式子 | 对应原版 |
|---|---|---|---|
| **`TRANSITION_SHINY`** | `b16.hlsl:99-745` | 原样保留（只重排了变量名） | `_433/_444/_447/_500/_501/_510.._513` |
| **`TRANSITION_DISSOLVE`** | `b15.hlsl` | 与 SHINY 共用骨架，但 `_448 = saturate(2·band/softW)` **额外乘进 rgb 与 a**（`_513.._516 = _448 · _509.._512`），且带内掩码用 `_500 = _448 · clamp(_444)`（**不平方**） | `_442.._448` · `_500` · `_509.._516` |
| **`TONE_GRAYSCALE`** | `b14.hlsl:99-99/171-174` | `luma = dot(s, (0.22,0.707,0.071))`；`s = s + _ToneIntensity·(luma − s)` | `_155`、`_172.._174`（`mad(_171, mad(−_146,_137,_155), _150)`） |
| **`EDGE_SHINY`** | `b17.hlsl` | 12 向 30° 采样取 α 最小 → `edge = 1 − saturate(max(aMin−0.15,0)·6.6667)`；边缘色；`atan2` 扫角 `frac(angle/π + t·speed + rate) < _EdgeShinyWidth`；`lerp(base, edge, shiny·edge)` | `_394.._630`（边缘）· `_910/_911`（α）· `_913.._941`（atan2）· `_974`（扫）· `_781.._787`（合成） |
| **`UNITY_UI_ALPHACLIP`** | `b18.hlsl:71` | `if (col.a < 0.001) discard;` | `mad(_111,_118,−0.001) < 0` |
| **`UNITY_UI_CLIP_RECT`** | `b23.hlsl:137-161` + `Vertex_b1.hlsl:99-102` | 顶点算 `2·pos − clamp(_ClipRect.xy) − clamp(_ClipRect.zw)` 与 `0.25/(softness·0.25+|pixelSize|)`；像素侧 `clamp((w−|x|)·kx)·clamp((h−|y|)·ky)` 乘进 rgba | `_297/_298/_271/_272`（顶点）· `_137.._161`（像素） |
| **变体选择本身** | YAML `:67-108` | `#if` 严格「四个效果内**恰有一个**」才点亮；两个以上同时开 ⇒ 不回退到叠加，而是**落回什么都不做**（= 原版 `b13` catch-all） | — |

### 3·2 ⛔ 判「不需要」的（附反证）

| 项 | 反证 |
|---|---|
| **vertex 的另外 3 档**（b2/b3/b4） | §2·3 的 `diff`：**只有第 1–2 行注释不同**，其余逐字节相同 ⇒ 一份 `vert` 就够 |
| **把 20 个 fragment blob 各写一份** | §2·2：四个效果行的裁剪增量恒等于基础档的 **+4/+15/+19** ⇒ 裁剪只是**附加一段代码**，不是新体；写 20 份会得到 5 份重复体 |
| **单独实现「b13 基础档」** | 基础档 = 「什么效果都不做」。本文件用**兼容档**（见 §3·3）把「无关键字」映射到 SHINY —— 因为本工程 C# 现在不开关键字，照原版语义会**静默丢扫光**。⚠️ 这是**已知偏离**，不是「不需要」 |
| **`b18/b23/b28` 的三个「无效果 + 裁剪」组合** | 已通过把「裁剪修饰」写成**正交的一段**（`#if UNITY_UI_CLIP_RECT` / `ALPHACLIP`）覆盖到**全部**效果档 ⇒ 5×4=20 档全覆盖，不需要单独写 |

### 3·3 ~~🔶 兼容档~~ → ✅ **已于同会话收口删除**（见文末「## 兼容档收口」）

> 下面这段是**收口前**的状态，留作留痕；**现行代码里没有这段**。

```hlsl
// 🔶 自己设计（非原版）：只在「四个效果关键字全未定义」时成立 ⇒ 现状继续等于 TRANSITION_SHINY
#if !defined(TONE_GRAYSCALE) && !defined(TRANSITION_DISSOLVE) && !defined(TRANSITION_SHINY) && !defined(EDGE_SHINY)
    #define WF_SHINY 1
#endif
```
**为什么必须**：`WildcardShineLayer.Attach`（`Effects/UIEffectTogglerCoffeeEffects.cs:258-268`）只设材质属性、**不开任何关键字**；
而原版「四个关键字全不开」= 基础档 = **什么都不做** ⇒ 不加这段就会**静默丢掉收藏窗那道扫光**（本波不许改 `.cs`）。
**下一波正解**（一行）：`mat.EnableKeyword("TRANSITION_SHINY");`，然后删掉这 4 行 ⇒ 恢复原版语义且**行为逐位不变**。

---

## 四 关键字冲突闸读数

```bash
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py \
  d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WildcardShine.shader --no-warn
# PASS …：关键字 7 · 变量 101 · 冲突 0        （rc=0）
```
**全仓复跑**（回归）：
```bash
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet
# 合计：文件 65 · 关键字 150 · 变量声明 3754 · 真冲突 0 · WARN 22  ⇒ PASS
```
（22 条 WARN 全是既有的「属性名与关键字同名」合法写法，本波**一条没增**。）

**结构自检**（手工，纯静态）：`#if/#endif` 深度回 0 · `{}` 16/16 · `()` 188/188 · `[]` 9/9 · 行尾 **LF 0 CRLF / 0 裸 CR**（原文件就是 LF，没被翻）。

---

## 五 需 Unity 编译验证清单（shader 编不过 `typecheck` 看不见，**必须**跑一次 Unity）

| # | 验什么 | 怎么算过 |
|---|---|---|
| 1 | **整份 shader 编得过**（64 变体全编） | Console 无 `Shader error`；`Shader.Find("CardPresentation/WildcardShine")` 非 null |
| 2 | **回归：现状（无关键字）观感不变** | 收藏窗 `WIldcard Display ▸ Background` 的扫光与改前逐像素一致（走 `WF_SHINY` 兼容档） |
| 3 | `UNITY_UI_ALPHACLIP` 档 | 材质量 `_UseUIAlphaClip=1` ⇒ α<0.001 的像素被丢弃（品红/黑边消失） |
| 4 | `UNITY_UI_CLIP_RECT` 档 | 放进 `RectMask2D` 下 ⇒ 出框部分被裁掉；`_UIMaskSoftness` 为 0 时不除零 |
| 5 | `_MainTex_TexelSize` 自动填值 | EDGE 档有反应（若 Unity 没填 = 全 0 ⇒ 12 个采样同一 texel ⇒ 恒 `edge=0`），**这一条最容易静默失败** |
| 6 | `UNITY_MATRIX_P` 在 `CGPROGRAM` 里可用 | 上一条 4 跑通即证明 |
| 7 | 变体数量 / 编译时间 | 64 变体是否可接受（原版同样 6 条 `multi_compile_local`，但构建里只保留 20 档） |

⚠️ **本波没有跑以上任何一条**（简报红线：⛔ 不跑 Unity）。

---

## 六 顺手发现（**没改，只记账**）

1. 🔴 **`活状态表.md` 的 `B-274` 行「证据锚」路径写错了**：写的是 `WarpforgeVFX/Shaders/WildcardShine.shader`，
   实际全仓只有 **`Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WildcardShine.shader`**（`find d:/4/Unity -iname "WildcardShine*"` 只 2 命中 = 该文件 + 它的 `.meta`）。
   ⚠️ 同类「按目录猜路径」的坑（铁律 5 同族），建议调度台就地订正。
2. 🔴 **简报指的原版产物目录不对**：`d:/2/tools/ruri_out_1020_allshaders/` 里**没有**这颗 shader；
   它在 **`ruri_out_1020_rootshaders/`**（304 份）。以后查这颗 shader 直接去 rootshaders。
3. ⛔ **`_TextureSampleAdd` 未实现**：原版**基础档 b13 就有** `tex + _TextureSampleAdd`（`b13.hlsl:35-38`），我们的 `frag` 直接 `tex2D(...) * color`。
   对 Image（非字体）来说它恒 (0,0,0,0)，所以观感无差；但**这是与老底档的差异**，已记账。
4. ⛔ **`_ColorFilter` / `_TransitionColorFilter` / `_EdgeColorFilter` 只做了 `0`（不染）与 `6` 两档**；
   原版各有 **1/2/3/4/5/7/8** 七档（`b14.hlsl:305-590` · `b16.hlsl:190-330` · `b17.hlsl:645-830`）。现役实例只用 6，但**七档仍缺**。
5. ⛔ **Shadow 族整块未实现**（`_ShadowColorFilter/_ShadowColor/_ShadowColorGlow/_ShadowIteration/_ShadowFade/_ShadowMirrorScale/_ShadowBlurIntensity`）——
   每个效果变体里都有一大段（`b16.hlsl` 的 `if (_287)` 那一支）。
6. ⛔ **Gradation / Target / Detail / Sampling 四族未实现**（`b14` 的 cbuffer 里能看到 `_SamplingScale` 之外的同族槽位）。
7. ⚠️ **原版每个变体都拿 `TEXCOORD1`（mesh uv1）当「sprite 的 uv 矩形」做盒测，而那条通道是 `UIEffectBase.ModifyMesh` 写进去的**
   （`dump.cs` 里 `UIEffectBase.ModifyMesh(VertexHelper)` 存在；`decomp_full` 没有它的体 ⇒ 读不到逐句）。
   **本工程没有这条通道** ⇒ 本文件按 **`uv1 = (0,0,1,1)`** 那一档写（此时 `_382 = uv.x>=0 = 1`、`_393 = 1>=uv.x = 1`、`_287 = 0<0 = false`）——
   **逐项等价**，但「原版真的会传非平凡 uv 矩形」这件事**没复刻**。
   🆕 **同一格还藏着一个机制**：`uv1.x < −3` 时 `+4` 重映射、`uv1.x < 0` 时进 **Shadow 支**（`b14.hlsl:68-71` · `b16.hlsl:115-117`）
   ⇒ **uv1.x 同时是「Shadow 开没开」的开关**。这条以前没人记过，将来要做 Shadow 必须先解 `ModifyMesh`。
8. 🆕 **`_EdgeColorFilter` 的 6 档与 `_TransitionColorFilter` 的 6 档【不是一个算式】**：
   - `b16.hlsl:745`（Transition 6）：`_745 = _407 · (C.x + 1)` —— **不乘 alpha**
   - `b17.hlsl:746`（Edge 6）：`_1452 = _747 · mad(_1442, _1448, 1.0)`，`_1448 = _EdgeColor.w` ⇒ **乘了 alpha**
   - （`b14.hlsl:655` 的 `_ColorFilter` 6 也乘 alpha：`mad(_645, _651, 1.0)`，`_651 = _ColorValue.w`）
   ⇒ 已按各自字节分开实现。**别把三者合并成一个函数**（会静默错一档）。
9. ⚠️ 原版 shader 是 `#pragma target 5.0` + `#pragma use_dxc` + `HLSLPROGRAM`；我们用**默认 2.5 档 + CGPROGRAM**（沿用旧实现），
   所以 `round()` 仍写成 `floor(x+0.5)`（`round` 要 SM4）。
10. ⚠️ 原版 `Blend [_SrcBlend] [_DstBlend]` 是**材质属性驱动**；我们写死 `Blend One OneMinusSrcAlpha`。
    对这一颗实例（`m_SrcBlendMode 1 / m_DstBlendMode 10`）成立，但**换实例就不对**了。
11. 🔶 **`_SamplingScale` 的默认值取 1**（`_SamplingScale ("Sampling Scale", Float) = 1`）：Coffee 包默认应为 1，但**本机没有 EDGE 实例可核** ⇒ 标记为「我们挑的默认值」。
    同理 `_ToneIntensity = 0`（= 不改变颜色）、`_Edge*` 全 0（= 不出边缘）。
12. ⚠️ 原版 EDGE 的中心点会算出 `1/0 = inf ⇒ NaN`（`b17.hlsl:919`）；`frac(NaN) < w` 恒 false ⇒ 结果同为「不亮」。
    我们加了 `max(amax, 1e-8)` 把 NaN 挡掉（数值保护，**不改变任何可见结果**）。

---

## 兼容档收口（2026-11-01 · 调度台补发指令后完成）

### 7·1 改了什么

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` | `WildcardShineLayer.Attach` 里 `mat.name = …`（新 `:259`）之后**新增 7 行**，核心是 **`:266` `mat.EnableKeyword("TRANSITION_SHINY");`** ＋ 6 行判据注释（原版 6 条 `multi_compile_local` + 「四个效果关键字必须恰有一个开」＋ 5 个实例 `m_TransitionFilter` 全是 4 = Shiny） |
| `Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WildcardShine.shader` | **删掉**兼容档那 7 行（4 行 🔶 注释 + `#if/#define/#endif`）；文件头 §② 改写为「关键字由材质显式点亮，本文件没有任何兜底」 |

`git diff --numstat`：`.cs` = **+7 / −0**（489 → 496 行，**纯新增**）；`HEAD` 版就是 LF（`CRLF 0`），改后仍 **CRLF 0 / 裸 CR 0** ⇒ **没翻行尾**。

### 7·2 🔴 静态等价性证明（改前 vs 改后，逐分支比）

| 预处理 / 执行步骤 | **改前**：材质关键字集 = `{}` ＋ 兼容档 | **改后**：材质关键字集 = `{TRANSITION_SHINY}`，无兼容档 | 等价？ |
|---|---|---|---|
| 选中哪一份 SVC 变体 | 空关键字那一份（`multi_compile` 保证它**存在**） | `TRANSITION_SHINY` 那一份（同样**始终存在**，因为 `multi_compile_local` 无条件编全部档） | 只影响「选哪份」，不影响内容 ↓ |
| `WF_TONE` | 未定义（`TONE_GRAYSCALE` 未开） | 未定义（同） | ✅ |
| `WF_DISSOLVE` | 未定义（`TRANSITION_DISSOLVE` 未开） | 未定义（同） | ✅ |
| `WF_SHINY` | **由兼容档 `#define`**（`!TONE && !DISSOLVE && !SHINY && !EDGE` = 真） | **由关键字档 `#define`**（`!TONE && !DISSOLVE && defined(SHINY) && !EDGE` = 真） | ✅ **同一个宏、同一个值 = 1** |
| `WF_EDGE` | 未定义 | 未定义（`EDGE_SHINY` 未开） | ✅ |
| Tone 段 `#if defined(WF_TONE)` | 不编进 | 不编进 | ✅ |
| `uvT` 段 `#if WF_DISSOLVE \|\| WF_SHINY \|\| WF_EDGE` | **编进** | **编进** | ✅ |
| 过渡段 `#if WF_DISSOLVE \|\| WF_SHINY` | 编进，内层 `#if WF_DISSOLVE` 为假 ⇒ **走 `#else`（SHINY）** | 编进，内层同样为假 ⇒ **走 `#else`（SHINY）** | ✅ |
| Edge 段 `#if defined(WF_EDGE)` | 不编进 | 不编进 | ✅ |
| `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP` 段 | 不编进（两个关键字都没开） | 不编进（C# 仍只开 `TRANSITION_SHINY`） | ✅ |
| 材质属性 | 8 条 `SetFloat/SetColor`（width/softness/color/colorFilter/colorGlow/reverse/autoPlaySpeed/useTransitionTex/rate） | **同样那 8 条，一条没动**；`EnableKeyword` 只写关键字、**不写任何属性** | ✅ |
| SubShader 渲染状态（Blend/Cull/ZWrite/Tags） | SubShader 级常量 | 同（未改） | ✅ |
| **预处理后的翻译单元** | `T_A` | `T_B` | ✅ **同一串 token**（唯一的差别是「哪一行 `#define WF_SHINY`」——预处理指令本身不进源码） |

**结论：两条路径编译出的像素着色器是同一份代码、读同一组 uniform、同一组属性值 ⇒ 逐位相同。**
补充两条旁证：
- `EnableKeyword` 是 **`Material` 级**（`multi_compile_local` 就该走这个），不是 `Shader.EnableKeyword`（那是全局）⇒ 不会传染别的材质。
- 这颗 shader **只有** `WildcardShineLayer.Attach` 一个材质生产点（`grep -rn "CardPresentation/WildcardShine" --include=*.cs` 的其余命中全是 `Debug` 文案与 Editor 断言）；断言只比 `mm.shader.name` / `Shader.Find != null`，**没有任何断言读 `shaderKeywords`**（`grep shaderKeywords` 在扫光相关代码里 **0 命中**）⇒ 加关键字不会让任何自检变红或空转。

⚠️ **一处必须承认的差别（不是行为差别）**：改前「空关键字」那份变体编译进去的是 `WF_SHINY=1` 的代码；改后它编译进去的是 **原版基础档 = 什么都不做**。
这正是本次想要的（恢复原版语义）；只要**没有别的材质**在用这个 shader 的空关键字档 —— 实测**没有**（见上一条）。

### 7·3 三闸读数（本波实跑）

```
$ TMPDIR=/tmp/wf_w12b bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0            ← 0/0

$ python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py       （⛔ 没加 --fix）
合计：A 5831 条 · 通过 5831（其中告警 864 条）· 漂移 0 · 失败 0
B 档：B 4 条 · 落点问题 2 条（点在注释行 0 条）  —— 与改动前**逐字相同**

$ python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet
合计：文件 65 · 关键字 150 · 变量声明 3754 · 真冲突 0 · WARN 22 ⇒ PASS   （22 条 WARN 与改动前一致）
```

📌 **typecheck 第一次跑（20:42:4x）报了 6 条红，全部在 `Shell/SettingsWindow.cs`（`HitCovers` / `DescribeHit` 未定义）**——
当时那个文件的 mtime 恰好是**同一秒**、且 1 分钟后再 `grep` 那两个名字已 **0 命中** ⇒ 是**别的写手正在写它**（铁律 13·3 那条「错误全在不是你负责的文件上」）。**隔一分钟重跑即 0/0**，与本改动无关。

### 7·4 还要验什么（本波 ⛔ 没跑 Unity）

| # | 验什么 | 怎么算过 |
|---|---|---|
| 1 | **shader 重编**（64 变体） | Console 无 `Shader error`；`Shader.Find` 非 null |
| 2 | **回归：扫光还在** | 从收藏窗 `CardsTab` 与卡组编辑窗 `WIldcard Counter` 各看一次，扫光**照常扫**（若没了 = 关键字没生效/变体没编进去） |
| 3 | **基础档确实是「无效果」**（= 新语义生效） | 临时建一个**不开任何关键字**的材质贴到同一块底板：应当**只剩底板原图、没有扫光** —— 这一条同时反证 `EnableKeyword` 真的切到了另一份变体 |
| 4 | `mat.EnableKeyword` 无 warning | 本地关键字已声明（`#pragma multi_compile_local _ TRANSITION_SHINY`）⇒ Console 不应出现 `keyword not found` |
| 5 | 材质/批处理 | 9 块九宫仍是**一份材质**（关键字不改变这一点） |
