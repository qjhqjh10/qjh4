# `B115_W22` · `B-304` —— `WFUIFxShine` 片元 **b7（catch-all）补实现**

> 写手 `B115_W22` · 2026-11-01。
> ⛔ 没跑 Unity（按简报：shader 编不编得过 `typecheck` 看不见，清单见 §六）· ⛔ 没动 git / 账 / 正本 / 其它 `.shader`。
> 只改 **1 个文件**：`Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WFUIFxShine.shader` ＋ 本报告。

---

## 〇 一句话

`frag` 里原来**恒走 b8**（两条 `#pragma shader_feature_local` 只声明关键字、不改代码，实质是「一个体 × 4 个变体」）。
现在在 `frag` 顶部加了一条 **`#if defined(_USESCREENSPACE) && defined(_USETILEMASK)`** —— **它不是我们自己挑的条件，是原版 shaderlab `:61-66` 那条判据逐字照搬**，
`#else` 分支 = 原版 **b7 catch-all** 的逐句复刻（旋转中心 **`uv - 0.5`**、**没有 tile mask**）。
**b8 那条正在工作的路径语义一字未变**（§四 用预处理 diff 证到「只有声明/赋值拆成两句」这一处）。

---

## 一 判据（原版出处 · 现读核对）

| 项 | 值 |
|---|---|
| 原版 shaderlab | `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Everguild_FX_FX Shine For Animation Normal.shader`（119 行） |
| 变体判据（**唯一**） | `:61` `#if defined(_USESCREENSPACE) && defined(_USETILEMASK)` → `:62` 引 `Sub0_Pass0_Fragment_b8.hlsl`；`:63-66` `#else` / `// Catch-all.` → `Sub0_Pass0_Fragment_b7.hlsl` |
| `#pragma` | `:41` `multi_compile_local _ _USESCREENSPACE` · `:42` `… _USETILEMASK` · `:43` `… _ SKINNED_SPRITE`（**片元只分两档**：两个都开 → b8，其余 → b7） |
| b7 体 | `…/Everguild_FX_FX Shine For Animation Normal/Sub0_Pass0_Fragment_b7.hlsl`（119 行，`// Keywords: <none> (default / catch-all variant)`） |
| b8 体 | 同目录 `Sub0_Pass0_Fragment_b8.hlsl`（144 行，`// Keywords: _USESCREENSPACE _USETILEMASK`） |

**三份导出目录里那颗 b7 逐字节相同**（`B115_W1` 只说「在 rootshaders」，我现读三个目录都有一份，md5 全等）：

```text
e3bc16f6ea20f284fd094c787e34c47f  rootshaders/…Normal/Sub0_Pass0_Fragment_b7.hlsl
e3bc16f6ea20f284fd094c787e34c47f  rootshaders/…Normal_0/Sub0_Pass0_Fragment_b7.hlsl
e3bc16f6ea20f284fd094c787e34c47f  allshaders/…Normal/Sub0_Pass0_Fragment_b7.hlsl
e3bc16f6ea20f284fd094c787e34c47f  rootshaders/…Normal/Sub0_Pass1_Fragment_b7.hlsl      ← Pass0 / Pass1 同体
101291ab2ae7f36cd88a918c7667d30b  …Sub0_Pass0_Fragment_b8.hlsl = Pass1_b8（b8 也同体）
```

**b7 与 b8 的差异，我自己逐句复算过，确认只有 `B115_W1` 说的那两处**：

1. 旋转中心 —— b7 `:66-67` = `INTERP.x - 0.5` / `INTERP.y - 0.5`；
   b8 `:72-76` = `gl_FragCoord.x / _ScaledScreenParams.x`，y 走 `_ProjectionParams.x < 0` 镜像补偿后再 `0.5 - y/H`（**取反过**）。
   连 **cbuffer 都不一样**：b7 的 `type_Globals`（`:5-9`）**没有** `_ScaledScreenParams` / `_ProjectionParams`（它压根不需要 `gl_FragCoord`）。
2. tile mask —— b7 `:91` 取完扫光图，`:92` 直接进 `_ShineColor` 的 sRGB 编码；b7 的 `UnityPerMaterial`（`:11-22`）里**没有 `_Mask_Size`**。
   b8 `:103-114` 才有 `mx/my`（`abs(uv*2-1) - _Mask_Size`）+ `ddx/ddy` 软边 + `mask * sh.rgb`。
   其余（底图早退 / `off³×0.1` 亮度差分造法线 / 旋转 + `_Shine_Movement` / `+0.5`·`-0.5` / sRGB 编码 / `mad(sh, enc, tx) * INTERP_1` / alpha）**逐句相同**，只差 SSA 编号（`_83/_85/_88` ↔ `_89/_91/_94` 等）。

---

## 二 改动（逐处）

文件行尾：**LF**（改前 `CRLF 0 · LF 212 · 裸 CR 0` ⇒ 改后 `CRLF 0 · LF 245 · 裸 CR 0`，`io.open(...,'rb')` 二进制计数；**没被翻**）。
大小：`14,456 B` → `18,460 B`（第 2 波换成 `multi_compile_local` + 补 `🔶` 标记后；**行数两次都没变，仍是 245**）。用 **Edit 工具**改，⛔ 没用 `sed -i` / python 整篇写。

| 位置（改后） | 改了什么 | 为什么 |
|---|---|---|
| `:35-50`（文件头「b7 没实现」那 5 行 → 改成 16 行） | **整段改写成「两档都已实现」+ 判据**，并把原文里**写错的行号**（`:87-95` / `:100-114` —— 那是 **b8 自己的**行号）**就地订正**为 b7 的 `:66-67` / `:91` | 铁律 5：文档写错就地改；旧句子会让人去 b8 的行号找 b7 的差异 |
| `:177` | 新增 `float2 ctr;`（旋转中心的**声明**提到分派之前） | 两档共用一个变量 |
| `:178-190` | `#if defined(_USESCREENSPACE) && defined(_USETILEMASK)` → b8 屏幕坐标；`#else` → **b7 `uv - 0.5`** | 判据 = 原版 `:61-66`（逐字） |
| `:212-214` | 扫光图采样从「b8 段里」提出来（两档同一条 sample、同一位置） | 原版两档都在 sRGB 编码**之前**采，改前这条在 `:100-114` 段里 |
| `:216-230` | `#if` 同上 → b8 的 tile mask（原 6 行**原样**）；`#else` → `float3 shine = sh.rgb;`（**整段没有 mask**） | 判据 = b7 `:91`→`:92` 之间**没有** `_Mask_Size`/`ddx`/`ddy` 任何一段 |
| `:192 / :200 / :232 / :236`（4 条注释行） | 「`Fragment_b8.hlsl:xx-yy`」→「**b7 `:aa-bb` / b8 `:xx-yy`，两档逐句相同**」 | 这四段本来就是两档共用的，只标 b8 会让人以为 b7 没有它们。**同一行内改写，零行数变化** |
| `:112-115`（第 2 波） | `#pragma shader_feature_local _ _USESCREENSPACE` / `… _USETILEMASK` → **`#pragma multi_compile_local …`**（＝原版 `shaderlab:41-42` **逐字**），注释同步改写 | 原版就是 `multi_compile_local`；而 `shader_feature` 按材质用量剥离 ⇒ **b7 这条分支永远进不了编译器**（详见 §十） |
| `:85`（第 2 波） | 给 `_Color` 那行补 `🔶 自己设计（非原版）：…` 标记（**就地改写，零行数变化**） | 铁律 18：`_Color` 是「机制不同但视觉等价」那一格，必须能 `grep 自己设计` 列出来 |

⚠️ **一处如实记的取舍**：`float4 _Mask_Size;` 这条**uniform 声明保留着**（原版 b7 的 cbuffer 里没有它）。
原因：它同时服务 b8 变体、且材质 `WFUI_FxShine.mat` 真有这条属性值 ⇒ 删了 b8 会坏。
b7 变体里它只是**声明了没人读**（HLSL 不报错、只是占一个 uniform 槽）——**不是**「mask 恒等于 1」那种语义。

---

## 三 b7 逐句对照（原版 `Sub0_Pass0_Fragment_b7.hlsl` → 我们的 `frag`）

> 我们这一侧的行号 = 当前文件（`WFUIFxShine.shader`）。

| 原版 b7 | 原版在算什么 | 我们（行） | 备注 |
|---|---|---|---|
| `:59` | `_MainTex.SampleBias(…, INTERP)` | `:169` `tex2D(_MainTex, i.uv)` | ⚠️ 原版带 `_GlobalMipBias`（见 §七·2） |
| `:60` `:61` | `_85 = x` · `_88 = w` | `:170` `:171` | 两档同 |
| `:62-65` | `if (_88 == 0) discard;` | `:172` | 两档同 |
| **`:66-67`** | **`_93 = INTERP.x - 0.5` · `_95 = INTERP.y - 0.5`** | **`:189` `ctr = float2(i.uv.x - 0.5, i.uv.y - 0.5);`** | **差异①**：b8 这里换成屏幕坐标且 **y 取反** |
| `:68-70` | `_100 = UvRotation` · `sin` · `cos` | `:193-194` | 两档同 |
| `:71` | `_104 = float2(_93,_95)` | 由 `ctr` 承担 | — |
| `:72` | `_119 = dot2(_104, (cos,sin)) + mov.x` | `:196` `u = wfDot2(ctr, rot1) + _Shine_Movement.x` | `rot1 = (cs, sn)` |
| `:73` | `_120 = dot2(_104, (-sin,cos)) + mov.y` | `:197` `v = …` | `rot2 = (-sn, cs)` |
| `:74-75` | `_121 = _119 + 0.5` · `_122 = _120 - 0.5` | `:198` `sm = float2(u + 0.5, v - 0.5)` | — |
| `:76-78` | `_126 = offset.x` · `_127 = _126²` · `_128 = _127·_126` | `:201-202` `off` / `off3` | — |
| `:79` | `Sample(_128*0.1 + uv.x, uv.y).x - _85` | `:203-204` `e` / `dx` | `mad(a,b,c)` = `a*b+c`，同 |
| `:80` | `_154 = offset.y` | `:206` `str` | — |
| `:81-85` | `_155 = dx·str`；`_158 = 0 - _155`；`_159 = 0 - _157` | `:207` `float3 n = float3(-dx * str, -dy * str, 1.0)` | 负号折叠，逐位等价（`(-a)*b ≡ -(a*b)`） |
| `:86-87` | `_160 = (-0.0)+1.0` · `_163 = float3(_158,_159,_160)` | 同上 | — |
| `:88-90` | `_165 = rsqrt(dot3(_163,_163))` · `_166 = _165·_158` · `_167 = _165·_159` | `:208` `inv = rsqrt(dot(n,n))`；`n.x*inv` / `n.y*inv` | — |
| `:91` | `SampleBias(shine, float2(mad(_121, scale.x, _166), mad(_122, scale.y, _167)))` | `:209-210` `shineUV` · `:214` `sh = tex2D(…)` | 两档**同一条** |
| **`:92`→`:103`** | **直接进 sRGB 编码，中间没有 mask** | **`:229` `float3 shine = sh.rgb;`（`:225-228` 注释说明整段不存在）** | **差异②** |
| `:92-94` | `_196.._198 = ShineColor.rgb` | `:233` `sc` | 两档同 |
| `:95-100` | 低段 `c*12.923210144…` / 高段 `exp2(log2(abs(c))*0.41666666…)*1.055-0.055` | `:154-164` `wfSrgbEncode` | 两条同一条函数（两档共用） |
| `:101-103` | `mad(sh.x, enc(_196), _85) * INTERP_1.x/y/z` | `:237` `(shine * enc + tx.rgb) * i.color.rgb` | 见 §六·3 的 `mad` 提示 |
| `:104` | `_242 = _88 * INTERP_1.w` | `:238` `a = baseA * i.color.w` | — |
| `:105-108` | `SV_TARGET = (x,y,z,w)` | `:239` `return fixed4(rgb, a)` | — |

**分派用的那条 `#if`**：`:178` / `:184` / `:190`、`:216` / `:224` / `:230` ——
条件文本与原版 `:61` **逐字相同**（`defined(_USESCREENSPACE) && defined(_USETILEMASK)`，**没有** `||`、**没有** `SKINNED_SPRITE`），
⚠️ 记一句口径：**只开一个关键字也走 b7**（原版 `:63-66` 就是 `#else` + `// Catch-all.`，不是「各开各的」）。

---

## 四 怎么保证 b8 那条正常路径没被动

三条证据，都可复跑：

**① 结构性证据 —— 预处理后逐行 diff（`HEAD` 的 b8 vs 我们开两关键字预处理出来的 b8）**

```text
old lines 81 / new lines 82
@@ -49,5 +49,6 @@
 if (baseA == 0.0) discard;
+float2 ctr;                                            ← 只多了这一句声明
 float2 screenPx = _ScreenParams.xy;
 float py = (_ProjectionParams.x < 0.0) ? (screenPx.y - i.pos.y) : i.pos.y;
-float2 ctr = float2(i.pos.x / screenPx.x - 0.5, 0.5 - py / screenPx.y);
+ctr = float2(i.pos.x / screenPx.x - 0.5, 0.5 - py / screenPx.y);
 float rot = Vector1_c76b369469204ddeb1ee5d707098307c;
```

⇒ **b8 分支的算式一行未改**，唯一变化是 `float2 ctr = X;` 拆成 `float2 ctr;` + `ctr = X;`（值表达式逐字相同）。
脚本 = `…/Temp/w22/b8diff.py`（只读，`git show HEAD:` 作基线）。

**② 今天的实际用法仍然走 b8 —— 素材侧现读**

`Assets/CardPresentation/Resources/Art/vfx/WFUI_FxShine.mat`：

```yaml
  m_ValidKeywords:
  - _USESCREENSPACE
  - _USETILEMASK
```

⇒ 两个关键字都开 ⇒ 落进 `#if` 的真分支（= b8）。**全工程只有这 1 份材质用这颗 shader**
（`grep -rn "_USESCREENSPACE\|_USETILEMASK\|FX Shine For Animation" Assets` 除本文件外只命中这颗 `.mat`、
两条断言、和 `WarpforgeVFX/Runtime/*` 里**原版 shader 名**的映射表 —— 后者与我们的 `WarpforgeVFX/UI/…` 无关）。

**③ 断言侧 —— 我把 `B110_S1_Assert` 的判据逐条在改后的文件上重跑了一遍**

它断的全是 `src.Contains(...)`（`StripLineComments` 之后）：`Shader "…"` · `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha` ·
`ZWrite Off` · `Cull Off` · `ZClip On` · **`!Contains("LightMode")`** · `_ScreenParams` + `i.pos` ·
`_ProjectionParams.x < 0.0` · `ddx(` + `ddy(` · 三个 sRGB 常量 · `if (baseA == 0.0) discard` · 三个 GUID 派生属性名。
**逐条现读 = 全 OK、`LightMode` = False**（脚本见 §八·③）。
⇒ **一条都不会变红**（这几条我都留在 b8 分支/共用段里，没有删、没有藏进注释）。

---

## 五 关键字冲突闸读数

```bash
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py
```

| 读数 | 值 |
|---|---|
| 全仓 | **文件 65 · 关键字 150 · 变量声明 3,755 · 真冲突 0 · WARN 22 ⇒ PASS（rc=0）** |
| 本文件 | **关键字 4 · 变量声明 49 · 真冲突 0 · WARN 2**（`:79 _USESCREENSPACE` / `:80 _USETILEMASK` —— 都是「属性名与关键字同名」的**合法** WARN，判据见 `B115_W1` §1·2①） |

**我这次没有新增任何关键字、也没有新增任何变量名**，证据：

- 关键字 **4 → 4**（改前 `git show HEAD:` 那份也是 4：2 个 `[Toggle]` + 2 条 pragma；`#if defined(…)` 是 `#` 开头的行，闸按设计整行跳过）。
- 变量 **48 → 49** —— 那 +1 是闸把**两档里各一条 `float3 shine = …`** 都数上了（它不评估 `#if`），**不是真多了一个变量**
  （同一份文件按变体预处理后，每档里 `float3 shine` 都恰好 1 条、`float2 ctr` 都恰好 1 条，见 §八·②）。
- 全仓 `143 / 3,695 → 150 / 3,755` 的**增量不是我的**：同一波里 `B115_W12` 改的 `WildcardShine.shader`（现读 关键字 7 · 变量 101）
  还在工作树里未提交（`git diff --numstat -- "*.shader"` 现读：`WildcardShine.shader 294/34`）。

---

## 六 需要 Unity 编译验证的清单（⚠️ `typecheck` 完全看不见 shader）

> 我这轮**只能做静态核对**（括号/预处理平衡、逐句对照、闸），**「编得过」必须靠 Unity**。

| # | 要验什么 | 怎么验 | 期望 |
|---|---|---|---|
| 1 | **b8（今天在用的那一档）仍编得过、画面不变** | 开工程让 `WFUIFxShine` 重编；`Shell/AvatarTab.cs:649 DoShine` 那条扫光跑一次 | Console 无 `Shader error`；扫光**照旧 4 秒扫过头像**（b8 分支算式未改，理应零差异，但**别拿「看不出差别」当编过了的证据**，要看 Console/Shader 面板） |
| 2 | 🔴 **b7（catch-all）到底能不能编** —— 这是本次最需要验的一条 | **已不需要造材质（§十 已把 pragma 换成 `multi_compile_local`）** ⇒ 正常开工程/重 import 这颗 shader，4 档会一起编 | Console 无 `Shader error`；Shader Inspector 里 `WarpforgeVFX/UI/FX Shine For Animation Normal` 的变体数 = **4**（原来只有 1）。**⚠️ 这一条必须真的看一眼 Console** —— b7 是这一波**第一次**进编译器 |
| 3 | `mad(a,b,c)` vs `a*b+c` 的末位差 | 无需单独验（现写法与 `B115_W1` 之前那版一致，属既有口径） | 只影响 ±1 LSB，不判失败 |
| 4 | b7 里「`_Mask_Size` 声明了但没人读」不会告警成错 | 上面第 2 条编过即可顺带确认 | 至多一条 unused 提示，不是 error |
| 5 | 预处理分支在 Unity 的 Cg/HLSL 预处理器下等价 | 同第 2 条（`#if defined(…)` 是标准写法，且原版 shaderlab 就这么写） | —— |

**静态自查（已做，可复跑）**：两档预处理后 `{} 6/6` · `()（b8 49/49 · b7 34/34）` · `[] 0/0` ·
每档 `float2 ctr` 1 条声明 + 1 次赋值 · 每档 `float3 shine` 1 条 · 每档 1 条 `return fixed4(rgb, a);` · `#if/#endif` 2/2 配平。

---

## 七 顺手发现（⛔ 我只报、没越界改）

### 7·1 ✅ **【已完成，见 §十】pragma 是 `shader_feature_local`，而原版是 `multi_compile_local` —— 后果是「b7 今天压根不进编译」**

- 原版 `shaderlab:41-42`：`#pragma multi_compile_local _ _USESCREENSPACE` / `… _USETILEMASK`（**无条件编全部 4 档**）。
  我们 `WFUIFxShine.shader:114-115`（现读行号）：`#pragma shader_feature_local _ _USESCREENSPACE` / `… _USETILEMASK`。
- 现文件那条注释写的理由是「只有真被材质用到的组合才会编」——**理由本身成立**，但它带来两个后果：
  1. **今天唯一材质的组合是 `[1,1]`** ⇒ 只编 b8 一档 ⇒ **我们刚写的 b7 从来没有被编译过**（潜在语法错会一直藏着，等哪天有人建个没开关键字的材质才炸）；
  2. 与**原版语义不同**：原版 4 档一定存在；`shader_feature` 在**打包时**会把没用的档剥掉（编辑器里按需编）。
- **按 `CLAUDE.md` 铁律 18·0（「需要拍板的地方默认复刻原版」）**，正解是换成 `multi_compile_local`（＝原版 `:41-42`）。
  代价很小（这颗 shader 很小，4 档），收益是「b7 进编译、可验、与原版同构」。
- ⛔ **我没改** —— 简报只授权 b7 的体、且换 pragma 会改**变体面**（打包体积/编译时间），属另一条决定。
  **建议调度台直接接一句**：把 `:111-112` 两条 `shader_feature_local` 改成 `multi_compile_local`（注释同步改），
  然后第 2 条验证就变成「正常开工程即可看见 4 档都编」。

### 7·2 ⚪ **`SampleBias(_GlobalMipBias.x)` vs 我们的 `tex2D`**（既有差异，不是本轮引入）

原版两档都用 `_MainTex.SampleBias(sampler, uv, _GlobalMipBias.x)` / `Texture2D_… .SampleBias(…, _GlobalMipBias.x)`（b7 `:59/:79/:82/:91`，b8 `:65/:88/:91/:102`）。
我们用 `tex2D`（无 mip bias）。URP 只在开了 mip-bias 特性时才把它设成非 0 ⇒ **今天等价**，
但若将来开渲染缩放/mip bias，这里会差一档 mip。**这条 b8 一直如此**，与 b7 无关，只是核到这里顺手记下。

### 7·3 ⚪ **`_Shine_Scale` 在原版 cbuffer 里只有 `float2`（`packoffset(c3.y)`），但材质属性是 `Vector`（4 分量）**

原版 b7 `:16` / b8 `:21` 都是 `float2 _Shine_Scale : packoffset(c3.y)`，而 `Properties:7` 是 `Vector`。
我们声明 `float4 _Shine_Scale`、只用 `.x/.y` ⇒ **取值一致**（材质现值 `{r:0, g:20}`）。
记一笔免得以后有人看到 `float4` 以为「我们多读了两个分量」。

### 7·4 ✅ **【已完成，见 §二 表末行】本文件原来没有 `🔶 自己设计` 标记**，但它确实有一处「我们自己挑的」

`:86` 那条 `_Color` 属性（原版没有，用来承载 uGUI 顶点色的那个乘数 —— 文件头 §「顶点色的口径」已解释这是**等价物**而非新语义）。
按**铁律 18**「自己设计处必须能 grep 出来」，它属于「机制不同但视觉等价」那一格，**应该**有一条 `🔶 自己设计` 标记。
⛔ 我没加：本轮只授权 b7，且加标记要**就地改写已有注释行**（18·b 零行数变化），属于另一条独立活。建议另开一条（成本极低、一次就能做完）。

### 7·5 🟠 **账里那一行已经过期了（我只报，⛔ 没动账）**

`Unity/资料/完全复刻/账/个人资料_成就_排行.tsv:64`（`个人资料/成就/排行 · AvatarDisplay.DoShine`）的「差异」列现读写着：

> 「③ 该 shader 另有 b1/b3/b4/**b7** 三档变体（b7 = catch-all，无屏幕空间/tile mask）——全库用这颗 shader 的材质**只有 1 份**且带 `[USE|SCREENSPACE, USETILEMASK]` ⇒ 其余档**没有消费者**，**未复刻**（与 `WFUIAdditive` 对 b9/b10 同一处置）」

**这句话的后半句今天就错了**：b7 **已实现**（本轮）。建议账写手把那半句改成：

> 「③ 该 shader 另有 b1/b3/b4 三档顶点变体（**体与 b2 逐字节相同** ⇒ 已由同一条 `vert` 覆盖，不需要另写，判据 `B-286`）＋ **b7 catch-all 片元档已实现**（`B-304`/`B115_W22`：`frag` 里 `#if defined(_USESCREENSPACE) && defined(_USETILEMASK)` 分派，b7 = `uv-0.5` 中心 + 无 tile mask，逐句对照见 `报告/B115_W22_WFUIFxShine_b7.md` §三）；⚠️ b7 目前**无消费者**（唯一材质两个关键字都开）且 pragma 是 `shader_feature_local` ⇒ 它不进编译（要验见本报告 §六·2）」

⚠️ 顺带一句口径差异（不是错，但两个说法打架）：`B-286` 的结论是「b1/b3/b4 体相同 ⇒ **不需要补**」，而账里写的是「**没有消费者，未复刻**」。
前者是「已等价覆盖」，后者是「偷懒不做」——`CLAUDE.md` 明确要求前者那种写法。改账时请用 `B-286` 的口径。

---

## 八 复核材料（可复跑）

```bash
# ① 闸（现读：文件 65 · 关键字 150 · 变量 3,755 · 真冲突 0 · WARN 22 ⇒ PASS；单文件：关键字 4 · 变量 49 · 冲突 0）
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet --no-warn
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py \
  "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WFUIFxShine.shader"

# ② 行尾（必须二进制读；文本模式会把 \r\n 折成 \n，两个数恒相等）
python -I -X utf8 -c "import io;p=r'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WFUIFxShine.shader';b=io.open(p,'rb').read();print(b.count(b'\r\n'),b.count(b'\n'),b.count(b'\r')-b.count(b'\r\n'),len(b))"
# 现读：0 245 0 17958

# ③ 两档预处理 + 结构自查（括号平衡 / 每档 ctr·shine 各 1 条 / #if 配平）
python -I -X utf8 "C:/Users/qjh36/AppData/Local/Temp/w22/pp.py"
# ④ b8 与 HEAD 的预处理 diff（应只有 float2 ctr 声明那一处变化）
python -I -X utf8 "C:/Users/qjh36/AppData/Local/Temp/w22/b8diff.py"
```

⚠️ 上面 ③④ 两个脚本在 `%TEMP%/w22/`（**不进仓库**，是本轮现写的只读核对脚本）；逻辑很短，报告 §四·① 已贴出全部输出，
不影响复核（要重跑就按报告里的判据重写 20 行即可）。

**没有改任何 `.cs`** ⇒ 按简报不跑 `typecheck.sh`（也就不会占用它的固定 `$TMPDIR`、不会与别的写手互相覆盖）。

---

## 九 改动边界（自证没越界）

- 只碰 1 个文件：`Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WFUIFxShine.shader`（hunk 只在**文件头 `:24` 区**与 **`frag` 区**；
  `git diff -U0` 的 hunk 头现读：`@@ -24 +24,26 @@` / `@@ -143..-182 @@` 那 7 个），＋本报告。
- ⛔ 没动 git（没 commit/stash/checkout）· ⛔ 没动 `账/*.tsv` · ⛔ 没动 `活状态表.md` / `CLAUDE.md` / `项目任务.md` · ⛔ 没动其它 shader / `.cs`。
- 账影响：本文件在 `账/个人资料_成就_排行.tsv:64` 只被**按路径**引用（`shader = Resources/Shaders/WFUIFxShine.shader`，无行号，`B115_W1` 已核）
  ⇒ 本次 `+33 行` 不产生 `check_ledger_points` 漂移。

---

## pragma 换成 multi_compile_local

> 第 2 波（调度台 2026-11-01 指派：「你那条发现① 直接做掉」）。**行数一次没变（245 行），只用 Edit 工具改了 3 处注释/指令行。**

### 一 改了什么

| 位置（改后） | 改前 | 改后 |
|---|---|---|
| `:114` | `#pragma shader_feature_local _ _USESCREENSPACE` | **`#pragma multi_compile_local _ _USESCREENSPACE`** |
| `:115` | `#pragma shader_feature_local _ _USETILEMASK` | **`#pragma multi_compile_local _ _USETILEMASK`** |
| `:112-113`（2 条注释行） | 「原版是 multi_compile_local —— 这里用 shader_feature，只有真被材质用到的组合才会编」 | 「原版 `shaderlab:41-42` **逐字**（引用原文两条 pragma）⇒ **4 档无条件全编**；不改了，因为 `shader_feature` 会按材质用量剥离 ⇒ 今天唯一材质 `[1,1]`，b7 永远进不了编译器」 |
| `:45-48`（文件头 4 行） | 「pragma 仍是 `shader_feature_local` ⇒ **它今天不会进编译**…（换 pragma 是另一条决定，本文件**未改**）」 | 「**变体面（`B115_W22` 2026-11-01 改）**：已换成原版 `multi_compile_local` ⇒ 4 档全编 ⇒ **从今天起 b7 真的会被编译器看到**；⚠️ 运行时选哪档不变（材质两个关键字都开 ⇒ 仍 b8）」 |
| `:85` | `// 🆕 我们这一项：原版没有 _Color，其乘数来自顶点色（…）` | **`// 🔶 自己设计（非原版）：_Color 这个载体 …· 为什么必须自定：原版那三个乘数在 uGUI 侧 …（乘数语义不变）· B110_S1 2026-10-31 落地，B115_W22 2026-11-01 补标`**（就地改写，**零行数变化**；`grep -n 自己设计` 现读 = `:85` 一条） |

**判据（逐字）**：原版 `Everguild_FX_FX Shine For Animation Normal.shader`：

```hlsl
#pragma multi_compile_local _ _USESCREENSPACE      // :41
#pragma multi_compile_local _ _USETILEMASK         // :42
#pragma multi_compile_local _ SKINNED_SPRITE       // :43
```

🔴 **`SKINNED_SPRITE` 那条我们仍然不加**（第 3 条 pragma）：它的两个顶点档（b3 / b4）与 b2 **体逐字节相同**（`B-286` 已用 `diff` 证过），
加了等于把变体数从 4 抬到 8、换 0 行为。**这是「等价覆盖」，不是「懒得做」**；依据与 `B-286` 同。
（另有一条环境事实支持这个判断：我们走 `MeshRenderer + ImageQuad`，`SKINNED_SPRITE` 根本不会被定义。）

### 二 变体数：**1 → 4**

| 口径 | 改前（`shader_feature_local` × 2） | 改后（`multi_compile_local` × 2，＝原版） |
|---|---|---|
| 关键字组合（理论） | 4 | 4 |
| **实际会编几档** | **1** —— `shader_feature` 按**材质用量**剥离，全工程唯一材质 `WFUI_FxShine.mat` 是 `[1, 1]` ⇒ **只编 b8** | **4** —— `multi_compile` 无条件全编：`(∅,∅)`、`(∅,_USETILEMASK)`、`(_USESCREENSPACE,∅)` → **b7**；`(_USESCREENSPACE,_USETILEMASK)` → **b8** |
| 其中体相同的 | — | 3 档落 b7（**同一份体**，Unity 不跨变体去重 ⇒ 3 份字节码内容一致）+ 1 档 b8 |
| 结论 | **b7 从没被编译过** | ✅ **从今天起 b7 会被编译** —— 这一波第一次有人看见它 |

### 三 影响估计（⚠️ 只能说估计：本轮 ⛔ 不跑 Unity）

| 项 | 估计 | 依据 |
|---|---|---|
| **打包体积** | **+3 份 subprogram，约 +5–10 KB 量级（上限 < 20 KB）** | 原版导出里 b7 / b8 的**反汇编文本**分别是 **5,058 B / 6,297 B**（文本形态，真实字节码更小）；这颗片元约 120 行、无循环无分支，单份 DXBC 通常 1–4 KB ⇒ 3 份 ≈ 5–10 KB。⚠️ 全工程 65 份 shader，这是**一颗 UI 小 shader** 的量级，对大包可忽略 |
| **编译时长（编辑器 import / 构建）** | **亚秒级增量** | 同一份片元的第 2 个变体本来就已经在编（b8）；现在多 3 档**同体**变体。参照：`B115_W1` 那次全仓 65 份 shader 关键字扫描是秒级，本颗的编译远小于此。⚠️ 精确值必须在 Unity 里量 |
| **运行时行为** | **零变化** | 选哪一档由材质关键字决定；`WFUI_FxShine.mat` 的 `m_ValidKeywords` 仍是 `[_USESCREENSPACE, _USETILEMASK]` ⇒ 仍选 b8 那份。`multi_compile` 与 `shader_feature` 在**选变体**上没有差别（差别只在「编不编/留不留」） |
| **打包会不会被裁** | 4 档都留（原版同构） | `multi_compile` 默认不按用量裁；这正是不用 `shader_feature` 的原因（否则 b7 在包里也不存在，等于白写） |

> 佐证「pragma 种类要跟原版走」不是我们独有口径：同一波里 `B114_W31` / `B114_W36` 的断言就**反向**钉着另一颗 shader
> 「`_DEBUG/_AUTOROTATION/_FLIPDIRECTION` 仍是 `shader_feature_local`，⛔ 别改成 `multi_compile_local`」（`B114_W31_Assert.cs:209-212`、`B114_W36_Assert.cs:217-222`）
> —— 因为**那颗原版写的就是 `shader_feature`**。⇒ 结论：**pragma 种类逐 shader 按原版，不统一**。

### 四 冲突闸读数（改完重跑）

```text
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet --no-warn
合计：文件 65 · 关键字 150 · 变量声明 3755 · 真冲突 0  ⇒ PASS      （rc=0）
单文件：文件 1 · 关键字 4 · 变量声明 49 · 真冲突 0 · WARN 2 ⇒ PASS   （2 条 = 属性名与关键字同名的合法 WARN）
```

换 pragma 种类**不改变**任何计数（闸把 `multi_compile*` 与 `shader_feature*` 采进同一个「关键字集」）⇒ **仍 4 个关键字、真冲突 0**。
行尾复核：`lines 245 · CRLF 0 · 裸CR 0 · 18,460 B` ⇒ **没被翻**。

### 五 b8 的算式：仍然没动（第 2 波再证一次）

预处理 diff（`git show HEAD:` 的 b8 vs 现在开两关键字预处理出来的 b8）现读全文就是下面这些行 —— **`frag` 的算式一行未改**：

```text
@@ -3,4 +3,4 @@
 #pragma target 3.0
-#pragma shader_feature_local _ _USESCREENSPACE      ← 本波**有意**改的两行（不在 frag 里）
-#pragma shader_feature_local _ _USETILEMASK
+#pragma multi_compile_local _ _USESCREENSPACE
+#pragma multi_compile_local _ _USETILEMASK
 #include "UnityCG.cginc"
@@ -49,5 +49,6 @@
 if (baseA == 0.0) discard;
+float2 ctr;                                          ← 第 1 波那处（声明/赋值拆两句）
 float2 screenPx = _ScreenParams.xy;
 float py = (_ProjectionParams.x < 0.0) ? (screenPx.y - i.pos.y) : i.pos.y;
-float2 ctr = float2(i.pos.x / screenPx.x - 0.5, 0.5 - py / screenPx.y);
+ctr = float2(i.pos.x / screenPx.x - 0.5, 0.5 - py / screenPx.y);
```

断言侧也重跑了一遍（`B110_S1_Assert` 的全部 `Contains` 判据）：**missing = none · `LightMode` = False · 四条 `HasCodeLine` 全 True**（换 pragma 只动 `#pragma` 行，`StripLineComments` 之后它们不进任何 `Contains` 判据）。

### 六 必须靠 Unity 编译验证的清单（**现在 b7 会真的编了，重点看它有没有语法错**）

| # | 要验什么 | 怎么验 | 期望 / 判失败长什么样 |
|---|---|---|---|
| 1 | 🔴🔴 **b7 首次进编译器不报错** —— 本轮第一优先 | 开工程（或右键 Reimport 这颗 shader），看 Console | 无 `Shader error in 'WarpforgeVFX/UI/FX Shine on …': …`。若 b7 有语法错，会以**变体 `_ _`**（空关键字那档）报出来 ⇒ **那就是本轮的真信号，别当成噪声** |
| 2 | **变体数 = 4** | Shader Inspector 的「Compile and show code」/ 变体列表（或 `ShaderUtil.GetShaderVariantCount`） | 4（**改前 = 1**）。若还是 1 ⇒ pragma 没生效（Unity 没重 import） |
| 3 | 🔴 **b8 仍走、扫光照旧** | `Shell/AvatarTab.cs:649 DoShine` 跑一次（选头像 → 4 秒扫光） | 扫光如常；Console 无 warning `keyword not found` |
| 4 | **打包侧变体不被误裁** | 出一次 player build（可选） | 4 档都在（`multi_compile` 默认不裁）；若将来有人想裁，⛔ 别用 `shader_feature` 换回来，改 `ShaderVariantCollection` |
| 5 | 编译时长 | 记一次 import 耗时（可选） | 与改前同量级（亚秒级差） |

> **一句话交接**：第 2 波之后，**b7 第一次真的会被编译器看见**（变体数 1 → 4），`frag` 的 b8 算式**一行未改**（§五 有预处理 diff 全文）；
> 唯一还没被证明的就是「b7 这份体没有语法错」—— 那一条只能进 Unity 看 Console（§六 第 1 条，判据都写死了）。
