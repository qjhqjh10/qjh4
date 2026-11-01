# `B115_W1` · 全仓 shader 关键字冲突闸（`B-298`）+ `WFUIFxShine` 档位结论（`B-286`）

> 写手 `W-115W1` · 2026-11-01。
> ⛔ 没跑 Unity 批处理 · ⛔ 没动 git · ⛔ 没碰任何 `.cs` / `账/*.tsv` / `活状态表.md` / `CLAUDE.md` / `项目任务.md`。
> 只改了 **2 个 `.shader`**（都在白名单内）＋ **新建 1 个只读工具** ＋ 本报告。

---

## 〇 一句话

新建只读闸 **`Unity/工具/check_shader_keyword_collision.py`**，对全仓 **65 份 shader** 求
「关键字集 ∩ 同名 HLSL 变量声明集」⇒ **真冲突 1 处**（`WFParticlePremultiply.shader:80`
的 `float _ENABLEVERTEXSTREAMS;` vs `:42` 的 `[Toggle(_ENABLEVERTEXSTREAMS)]`）**已修**；
另 22 处是**合法**的「属性名与关键字同名」（WARN，不判失败，有自己的理由，见 §二·3）。
`B-286` 的结论：`WFUIFxShine` 的 **b1 / b3 / b4 不需要补** —— 它们与 b2 的**反编译产物逐字节相同**（只差头两行注释），
我们那条 `vert` 就是那唯一一份体的复刻；注释已按这条**反证**改写（⛔ 不是「影响小」）。

---

## 一 交付物 1 · 工具 `check_shader_keyword_collision.py`

### 1·1 判据（工具**做什么**）

对同一个 `.shader` 文件求两个集合的交集：

| 集合 | 收哪些东西 |
|---|---|
| **关键字集** | `[Toggle(NAME)]` · `[ToggleOff(NAME)]` · `[KeywordEnum(A,B)] _PROP`（展开成 `_PROP_A` / `_PROP_B`，**大写**）· `#pragma multi_compile[_local][_fragment\|_vertex] …` · `#pragma shader_feature[_local][_fragment\|_vertex] …`（`_` / `__` 是空档占位，跳过） |
| **变量声明集** | `float\|half\|fixed\|real\|double\|int\|uint\|bool\|dword`（含 `float2/3/4`、`float4x4`）＋ `sampler2D` 一族 ＋ `Texture2D/texture2D` 一族；覆盖 `CBUFFER_START/END` 内、struct 成员、多声明符（`float a, b, c;`） |

**交集非空 = 真冲突（rc=1）** —— 因为关键字一开 Unity 就给整份 shader `#define NAME`，
再声明同名变量就是 `redefinition of 'NAME'`、**整份编不过**（本项目 2026-10-24 踩过两次，
`_tmp_view/20261024_playerbuild.log:4967` / `:5661`）。这个东西 **`工具/typecheck.sh` 完全看不见**（它只编 C#）。

### 1·2 两类刻意**不**判失败的东西

1. **`Properties` 里的属性名与关键字同名**（`[Toggle(_X)] _X ("…", Float) = 0`）——
   **合法**：ShaderLab 属性名**不会**在 HLSL 里被 `#define`，属性与关键字是两个命名空间。
   本项目 3 个文件的正解都是「**只删 HLSL 声明、`Properties` 里那条 `[Toggle]` 原样保留**」
   （`WFEclipseTau.shader:96-101` · `WFParticlePremultiplyGreyscale.shader:109-111` · `WFUIBorderHighlight.shader:214-219`）。
   ⇒ 工具报 `⚠ WARN`，**rc 不受影响**（现读 22 条，见 §二·3）。
   ⚠️ 这也是与简报里「Properties 块里的属性名也算变量声明集」那条**有意收窄的一处**：
   若把属性名当变量名，`[Toggle(_X)] _X` 这种**到处都在用**的合法写法会全部假红。
   工具的 `--no-warn` 可以完全关掉这一类输出。
2. **`[Toggle(...)]` 那一行本身**：变量集只认「HLSL 类型 + 名字」，而 ShaderLab 的属性类型写的是 `Float`（大写，
   不在类型表里）⇒ 天然不会自撞，**不需要特判**。

### 1·3 用法 / 退出码（工具文件头也写了）

```bash
# 全仓扫（不给路径时默认 <工具目录>/../MyGame/Assets，现读 65 份）
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py
# 只看真冲突，不看 WARN
python -I -X utf8 …/check_shader_keyword_collision.py --quiet --no-warn
# 单文件 / 目录（递归）/ 机器可读
python -I -X utf8 …/check_shader_keyword_collision.py <路径> [--json]
# 自测（6 个正负例，用子进程验 rc 与命中数）
python -I -X utf8 …/check_shader_keyword_collision.py --self-test
```

退出码：`0` 无真冲突 · `1` 有真冲突 · `2` 用法/读文件错误（`--self-test` 失败也是 2）。
**行为只读**：不写盘、不改文件。

### 1·4 自测输出（现读，`--self-test`）

```text
✓ neg_toggle_float.shader:    rc=1 冲突=1（期望 rc=1 冲突=1）          ← [Toggle] + 同名 float
✓ neg_pragma_cbuffer.shader:  rc=1 冲突=1（期望 rc=1 冲突=1）          ← pragma + CBUFFER 内 half
✓ neg_keywordenum.shader:     rc=1 冲突=1（期望 rc=1 冲突=1）          ← KeywordEnum 展开名写在多声明符第 2 个
✓ pos_toggle_property.shader: rc=0 冲突=0（期望 rc=0 冲突=0）          ← 属性名同名但无 HLSL 变量 ⇒ 合法
✓ pos_commented.shader:       rc=0 冲突=0（期望 rc=0 冲突=0）          ← 注释里的 [Toggle]/float 不算（剥注释）
✓ pos_function.shader:        rc=0 冲突=0（期望 rc=0 冲突=0）          ← 函数声明/形参不当变量
自测结论：PASS（rc=0）
```

### 1·5 已知局限（**刻意收窄，宁可漏报也不误报**）

- 不是 HLSL 解析器：`UNITY_DEFINE_INSTANCED_PROP(TYPE, NAME)` / `TEXTURE2D(NAME)` 这类**宏里**的名字**不采集**
  （现读全仓 `.shader` 里 `UNITY_DEFINE_INSTANCED_PROP` = 0 处，见 §五·4）。⇒ 它是**兜底闸**，不是「编过了」的证明。
- 函数形参不采集（`float3 f (float3 c)` 的 `c`）：形参与关键字同名的概率极低，而「把返回类型+函数名误当声明」的假阳性代价更高。
- 行号是**现读**的：别的写手改文件后行号会变，工具每次重读、不缓存。

---

## 二 扫描结果（现读）

命令：`python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py`
规模：**文件 65 · 关键字声明 143 · 变量声明 3,695 · 真冲突 0（修后）· WARN 22 ⇒ PASS（rc=0）**
（修前：变量声明 3,696、真冲突 **1**、rc=1）

### 2·1 真冲突（1 处，**已修**）

| 文件:行 | 关键字那一头 | 变量那一头 | 判定 |
|---|---|---|---|
| `WarpforgeVFX/Shaders/WFParticlePremultiply.shader:80`（修前） | `:42` `[Toggle(_ENABLEVERTEXSTREAMS)] _ENABLEVERTEXSTREAMS("EnableVertexStreams", Float) = 0` | `float  _ENABLEVERTEXSTREAMS;`（`CBUFFER_START(UnityPerMaterial)` 内） | **真**（修法见 §三·1） |

> ⚠️ **置信度如实标**：本文件**没有**该关键字的 `#pragma`。而「`[Toggle]` **单独**（无 pragma）是否也会产生 `#define`」
> 本机**没有 Unity 编译可验**，且 **219 份 Unity 官方 shader 里找不到「`[Toggle]` 不给 pragma」的同形反例**
> （扫过 `Editor/Data/Resources/PackageManager/BuiltInPackages` + `MyGame/Library/PackageCache` 共 123 份，命中 0）
> ⇒ **这一条在「编译期是否真会炸」上存疑**。但**改动是安全的**：那个变量在本文件**从不被读**
> （正文只有 `#ifdef _SOFTPARTICLES`），删声明**零行为变化**；留着则一旦成立就是整份编不过 ⇒ 风险不对称，删。

### 2·2 这一处的**独立复核**（不只信自己的工具）

```bash
grep -rnE "^\s*(float|half|fixed|real|int|uint|bool|dword)[1-4]?(x[1-4])?\s+_(SOFTPARTICLES|USEMASK|ANIMUVS|APPLYAMBIENTCOLOR|AUTOROTATION|FLIPDIRECTION|DEBUG|ROTATEUVS|ENABLEMATCAP|USEDISTORT|RECEIVESHADOWS|SOFT|USEBORDERFADE|ENABLEVERTEXSTREAMS)\b" --include="*.shader" .
```
⇒ 只有两处命中：`WFParticlePremultiply.shader:80`（**真冲突**）与
`WFParticlePremultiplyGreyscale.shader:108`（**假阳性/安全**：那颗的 `_ENABLEVERTEXSTREAMS` 在 `Properties:52`
是**普通 Float、没有 `[Toggle]`** ⇒ 不是关键字）。
**工具与 grep 在这一格上一致，而且工具没有像 grep 那样把第二处也报成冲突** —— 这是「不是按名字一刀切」的反证。

### 2·3 WARN 22 条（**合法，⛔ 不要改**）

判据见 §1·2①：属性名与关键字同名本身合法；本项目 3 个已有先例都是「删 HLSL 声明、保留 `[Toggle]`」。

| 文件 | 行 | 名字 | 关键字来源 |
|---|---|---|---|
| `CardPresentation/Resources/Shaders/WFUIFxShine.shader` | :69 | `_USESCREENSPACE` | Toggle |
| `CardPresentation/Resources/Shaders/WFUIFxShine.shader` | :70 | `_USETILEMASK` | Toggle |
| `WarpforgeVFX/Shaders/WFDistortion.shader` | :67 | `_SOFTPARTICLES` | Toggle |
| `WarpforgeVFX/Shaders/WFDistortion.shader` | :68 | `_USEMASK` | Toggle |
| `WarpforgeVFX/Shaders/WFDistortion.shader` | :72 | `_ANIMUVS` | Toggle |
| `WarpforgeVFX/Shaders/WFEclipseTau.shader` | :53 | `_APPLYAMBIENTCOLOR` | Toggle |
| `WarpforgeVFX/Shaders/WFMatcap.shader` | :78 | `_APPLYAMBIENTCOLOR` | Toggle |
| `WarpforgeVFX/Shaders/WFMultiRay.shader` | :123 | `_USEBORDERFADE` | `#pragma shader_feature_local` |
| `WarpforgeVFX/Shaders/WFParticlePremultiply.shader` | :42 | `_ENABLEVERTEXSTREAMS` | Toggle |
| `WarpforgeVFX/Shaders/WFParticlePremultiply.shader` | :43 | `_SOFTPARTICLES` | Toggle |
| `WarpforgeVFX/Shaders/WFParticlePremultiplyGreyscale.shader` | :53 | `_SOFTPARTICLES` | Toggle |
| `WarpforgeVFX/Shaders/WFParticlesExtraColor.shader` | :43 | `_SOFTPARTICLES` | Toggle |
| `WarpforgeVFX/Shaders/WFParticlesExtraColorEC.shader` | :50 | `_SOFTPARTICLES` | Toggle |
| `WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` | :116 | `_DEBUG` | Toggle |
| `WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` | :119 | `_FLIPDIRECTION` | Toggle |
| `WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` | :125 | `_AUTOROTATION` | Toggle |
| `WarpforgeVFX/Shaders/WFUnlitAmbient.shader` | :141 | `_ROTATEUVS` | Toggle |
| `WarpforgeVFX/Shaders/WFUnlitAmbient.shader` | :149 | `_ENABLEMATCAP` | Toggle |
| `WarpforgeVFX/Shaders/WFUnlitAmbient.shader` | :153 | `_USEDISTORT` | Toggle |
| `WarpforgeVFX/Shaders/WFUnlitAmbient.shader` | :192 | `_RECEIVESHADOWS` | Toggle |
| `WarpforgeVFX/Shaders/WFUnlitAmbient.shader` | :198 | `_APPLYAMBIENTCOLOR` | Toggle |
| `WarpforgeVFX/Shaders/WFUnlitUVScroll.shader` | :55 | `_SOFT` | Toggle |

⚠️ `WFParticlePremultiply.shader` 那两条 WARN 里，`_ENABLEVERTEXSTREAMS` 是**本次删声明之后**才落到 WARN 档的
（删之前它在「变量集」里、走的是冲突判定）。

---

## 三 改动清单（逐文件逐行）

### 3·1 `WarpforgeVFX/Shaders/WFParticlePremultiply.shader`（`B-298` 的修复）

| 位置（改后） | 改了什么 | 为什么 |
|---|---|---|
| `:78-101`（`CBUFFER_START(UnityPerMaterial)` 内） | **删掉 `float  _ENABLEVERTEXSTREAMS;` 这一行**，并把原来那句「`_ENABLEVERTEXSTREAMS` 不算这一类……永远不会被 `#define`，留着无害」**就地改写成反证**（含「本机未实证」的如实标注 + 同族三处先例 + 闸的入口） | 它是 `[Toggle(_ENABLEVERTEXSTREAMS)]` 的关键字名；本轮工具报真冲突。变量**从未被读** ⇒ 删了零行为变化 |

- 保留：`Properties:42` 那条 `[Toggle(_ENABLEVERTEXSTREAMS)] _ENABLEVERTEXSTREAMS(...)` **原样不动**
  （材质面板 / `SetFloat` 照旧能用；这是本族三处先例的统一做法）。
- 行尾：**LF**（改前 164 行 / 8,983 B ⇒ 改后 175 行 / 10,490 B，`\r\n`=0、裸 CR=0，`io.open(...,'rb')` 二进制计数）。
- 账：`grep -rn "WFParticlePremultiply" 账/*.tsv` = **0 命中** ⇒ 行号变化不会造成账漂移。

### 3·2 `CardPresentation/Resources/Shaders/WFUIFxShine.shader`（`B-286`）

| 位置（改后） | 改了什么 |
|---|---|
| `:22-38`（文件头「档位判据」那一节） | 把「另外三档（b1/b3/b4）没有任何消费者，**本轮不复刻**（与 `WFUIAdditive` 对 b9/b10 的处置同一条口径）」**整段换成结论 + 反证**：<br>① b1/b3/b4 **不需要补** —— 四份 `Sub0_Pass{0,1}_Vertex_b{1..4}.hlsl` **两两 diff 只差第 1-2 行注释**，函数体一字不差；两个 Pass 之间那 6 份文件也逐字节相同；<br>② 明确写「原句是『惰性 ⇒ 不做』的已作废说法，换掉它是因为**有逐字节反证**，⛔ 不是因为『影响小』」；<br>③ 记下**另一件真的缺**：片元 **b7（catch-all）没实现**（差异只在两处：`Fragment_b7.hlsl:87-95` 用 `uv-0.5` 当中心、`:100-114` 没有 tile mask），按 `B-262` 口径是**真缺口**（留给单独一条活，见 §五·1） |

**结论与判据（`B-286` 要的「二选一」：选『不补』，但不是判据为空，而是「已经等价覆盖」）**：

| 档 | 原版入口条件 | 原版顶点产物 | 结论 |
|---|---|---|---|
| b2 | `_USESCREENSPACE && _USETILEMASK && !SKINNED_SPRITE` | `Sub0_Pass0_Vertex_b2.hlsl`（`diff b1 b2` = 只差头两行注释） | 已实现（我们那条 `vert`） |
| b1 | catch-all | `…_b1.hlsl` | **体与 b2 相同** ⇒ 已覆盖 |
| b3 | `SKINNED_SPRITE`（且两个空间关键字都没有） | `…_b3.hlsl` | **体与 b2 相同** ⇒ 已覆盖；且我们走 `MeshRenderer`+`ImageQuad`，`SKINNED_SPRITE` 不会被定义 |
| b4 | `SKINNED_SPRITE + _USESCREENSPACE + _USETILEMASK` | `…_b4.hlsl` | 同上 |

原样命令（可复核）：

```bash
cd "d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_FX_FX Shine For Animation Normal"
diff Sub0_Pass0_Vertex_b1.hlsl Sub0_Pass0_Vertex_b2.hlsl   # 1,2c1,2（只有 // Blob: 与 // Keywords: 两行）
diff Sub0_Pass0_Vertex_b1.hlsl Sub0_Pass0_Vertex_b3.hlsl   # 同上
diff Sub0_Pass0_Vertex_b1.hlsl Sub0_Pass0_Vertex_b4.hlsl   # 同上
diff Sub0_Pass1_Vertex_b1.hlsl Sub0_Pass1_Vertex_b{2,3,4}.hlsl   # 同上
for f in Vertex_b1 Vertex_b2 Vertex_b3 Vertex_b4 Fragment_b7 Fragment_b8; do diff -q Sub0_Pass0_$f.hlsl Sub0_Pass1_$f.hlsl; done  # 6 份全 same
```

- 行尾：**LF**（改前 197 行 / 12,826 B ⇒ 改后 212 行 / 14,456 B，`\r\n`=0、裸 CR=0）。
- 账：该 shader 在 `账/个人资料_成就_排行.tsv:64` 被引用，但**只写路径、不写行号**（`shader = Resources/Shaders/WFUIFxShine.shader`）⇒ 本次加 15 行注释**不产生账漂移**。
- ⚠️ 只动注释，**一行代码未改**。

---

## 四 需要 Unity 编译验证的清单（⚠️ `typecheck` 不覆盖 shader）

1. 🔴 **`WFParticlePremultiply.shader` 仍编得过、画面不变**（最要紧）。
   改了 `UnityPerMaterial` 的成员构成 ⇒ 请确认：Shader Inspector **无 error**、且
   `WarpforgeVFX/FX/ParticlePremultiply` 的 `Shader.Find` 拿得到。
   ⚠️ 这颗在 `UseOriginal` 白名单里 ⇒ **正常态走原件**，本文件只在「原件包不在场」的兜底路生效
   ⇒ 画面侧很可能「看不出差别」，**别用「画面没变」当「编过了」的证据**，要看 Shader 面板/编译日志。
2. ⚠️ **「`[Toggle]` 单独（无 pragma）是否也产生 `#define`」** —— 本机**未实证**（§2·1）。
   若能顺手验：在 Unity 里造一颗最小 shader（`[Toggle(_X)] _X` + `float _X;`、**无 pragma**），
   看是否 `redefinition`。结果无论哪边，**都不影响本次改动的安全性**（那个变量本来就没被读）。
3. `WFUIFxShine.shader`：**只改注释**，理论上零风险；仍建议扫一眼 Shader 面板无 error。
4. 顺带：`tools/check_shader_keyword_collision.py` 的判据在 Unity 里**不需要**验证（纯静态、只读）。

---

## 五 顺手发现（⛔ 我只报、没越界改）

### 5·1 🟠 `WFUIFxShine.shader` 的**片元 b7（catch-all）没实现** —— 真缺口，请调度台另立一条账

- 原版片元只有两档：**b8**（`_USESCREENSPACE && _USETILEMASK`）与 **b7**（catch-all）。
  我们 `frag` **恒走 b8 的算式**，而两条 `#pragma shader_feature_local`（`:104-105` 改后）**不改任何代码**
  ⇒ 今天实际是「4 个一模一样的变体」。
- 差异只有两处（已逐行核过 `Sub0_Pass0_Fragment_b7.hlsl`）：
  1. `:87-95` 中心 = **`uv - 0.5`**（b8 用屏幕坐标 `gl_FragCoord / _ScreenParams` + `_ProjectionParams.x` 镜像补偿）；
  2. `:100-114` **没有 tile mask**（b8 才有 `_Mask_Size` + `ddx/ddy` 软边）；
  其余（法线差分、旋转、sRGB 编码、`(shine*enc + tx.rgb) * color` 的尾巴）**逐句相同**。
- 未做的原因：`B-286` 的授权范围是「判 b1/b3/b4」，且加分支会改动**在产品里正常工作的 b8 路径**
  （唯一材质带齐两个关键字）⇒ 按「改动保守」留给人另开一条（需配断言）。
  ⚠️ 现有断言 `CardPresentation/Editor/B110_S1_Assert.cs:206-215` 断的都是 `Contains(...)`，
  加 `#if` 分支**不会**让它们变红（我核过），但新分支本身需要新断言。

### 5·2 ⚪ `WFParticlePremultiplyGreyscale.shader:108` 的 `float _ENABLEVERTEXSTREAMS;` 是**安全**的 —— ⛔ 别顺手删错

同一行的名字看起来与 5·1 同形，但那颗的 `Properties:52` 是**普通 Float（没有 `[Toggle]`）**
⇒ `_ENABLEVERTEXSTREAMS` 在那颗里**不是关键字**。两者**不要一起处理**。
（`WFParticlePremultiply.shader` 才是带 `[Toggle]` 的那颗。）

### 5·3 ⚪ 全仓**没有**「`#ifdef` 一个没有 `pragma` 的 `[Toggle]` 关键字」的写法

扫过全部 65 份（脚本判据：`[Toggle]` 关键字 − 任意 pragma 关键字，再搜 `#ifdef/#if defined`）= **0 处**
⇒ 本波不需要追加这一类检查。若将来有人写出这种，`#ifdef` 恒假、是**静默死分支**，那时再把这条判据并进工具。

### 5·4 ⚪ 工具**不覆盖**的两类声明（写下来免得后人误以为「绿 = 一定编得过」）

`UNITY_DEFINE_INSTANCED_PROP(TYPE, NAME)` 与 `TEXTURE2D(NAME)` / `SAMPLER(NAME)` 这类**宏**声明（名字在括号里）
本工具不采集。现读全仓 `.shader`：`UNITY_DEFINE_INSTANCED_PROP` = **0 处**；
`TEXTURE2D(...)` 的实参全是 `_MainTex` 一类**非关键字**名 ⇒ 今天没有漏网。将来要收，得给工具加一条宏形态的正则。

---

## 六 复核材料（可复跑）

```bash
# ① 闸本身（现读：文件 65 · 关键字 143 · 变量 3,695 · 真冲突 0 · WARN 22 ⇒ PASS）
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet --no-warn
# ② 自测
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --self-test
# ③ 行尾（必须 `io.open(...,'rb')`；文本模式会把 \r\n 折成 \n，两个数恒相等）
python -I -X utf8 -c "import io;b=io.open(r'd:/4/Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFParticlePremultiply.shader','rb').read();print(b.count(b'\r\n'), b.count(b'\n'), b.count(b'\r')-b.count(b'\r\n'))"
```

**没有改任何 `.cs`** ⇒ 按简报不需要跑 `typecheck.sh`（也就没有占用它的固定 `$TMPDIR`、不会与别的写手互相覆盖）。
