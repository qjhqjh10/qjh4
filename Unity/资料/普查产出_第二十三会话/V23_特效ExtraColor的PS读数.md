# V23 · 特效线 `Extra Color` 的 `_Color` 那一跳 —— **PS 逐条读完，原问题结清**

> 会话：第二十三会话 · 2026-10-12 · 主对话（调度台）自己下场做的。
> 入口 = `项目任务.md` §三 第 4 条 ③「`Extra Color` 的 `_Color` 那一跳还没读通 …**要坐实得把它的 PS 也逐条读出来**」。
> ⛔ 本笔**没有跑 Unity**（五个写手在飞、批处理全局串行）—— 全部是**静态实读**。

---

## 一、结论（三条，都是读出来的、不是推的）

1. 🔴 **`cb2[1]` 就是 `_Color`，而且它【确实】喂给最终颜色。**
   原版 `Everguild/FX/Extra Color` 的**全部 32 个 VS 变体**都算：
   `o2.xyz = LinearToSRGB(_Color.rgb) × vcol.rgb`，`o2.w = vcol.w × _Color.w`；
   而 **Universal Forward 的 12 个 PS 变体**（去重后 3 种形状）**全部**把 `v2`（= VS 的 `o2`）乘进最终颜色：
   · `mul o0.xyz, r0.xyzx, v2.xyzx`（只乘 rgb，`o0.w = 1.0`）
   · `mul o0.xyzw, r0.xyzw, v2.xyzw`（rgba 都乘）
   · 软粒子那支：`r1 = tex * v2` 之后 `o0.w = fade * r1.w`、`o0.xyz = r1.xyz`
   ⇒ **最终色 = `tex × LinearToSRGB(_Color) × vcol`**。
2. 🔴 **上一轮那句「`cb2[1]` 一定不是喂最终颜色的那一份 / PS 乘的不是 o2」—— 两条都相反。**
   它是**同一份**，PS 乘的**就是** `o2` 那个插值器。**原问题在这里结清。**
3. ⚠️ **但「乘回去 ⇒ 退步」这条实测也不假** —— 它测的是**另一个东西**：
   我们自建那份 `WarpforgeVFX/Particles/Extra Color` **同时顶了 8 个原版 shader**
   （`Everguild/FX/Extra Color` 741 · `Mobile/Particles/{Additive,Alpha Blended}` 582 ·
   `Particles/Standard Unlit` 17 · `Everguild/FX/Particle Premultiply` · `…/TrailShader_Fading` ·
   `Shader Graphs/{Doomweaver effect,Eclipse Tau}` 等）。**`_Color` 只对其中一族有意义** ——
   对内置族（`Mobile/Particles/*` / `Particles/*`）它是**死值**（那些 shader 认的是 `_TintColor`）。
   ⇒ 加上 `_Color` = **修好一族、弄坏另一族**，净效果看谁的影响面大。

**所以「`_Color` 要不要乘」这个问题问错了** —— 正确的问题是
**「一份自建 shader 能不能同时顶 8 个原版 shader」**（答案：不能）。

---

## 二、判据（逐条可复查）

### 2·1 槽位是谁 —— Unity 序列化反射（`工具/dump_shader_blob.py` 那条路的加强版）
```bash
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
  D:/4/_tmp_view/vfx23/slot_names.py "Everguild/FX/Extra Color" "Universal Forward"
```
实读（`shaders_assets_all.bundle`，与 `wf_shaders.bundle` 逐字相同）：

| 程序 | cb 名 | `m_Size` | 用到的槽 |
|---|---|---|---|
| **VS** | `UnityPerDraw` | 720 | `slot[0]` `unity_ObjectToWorld` · `slot[4]` `unity_WorldToObject` |
| **VS** | `UnityPerMaterial` | 96 | **`slot[1]` off=16 → `_Color`（vec4）** |
| **PS** | `$Globals` | 2080 | `slot[4]` off=64 → `_GlobalMipBias`（vec2） |
| **PS** | 纹理 | — | `t0` = `_MainTex` |

**名字表**（`pass.m_NameIndices`，逐条实读）：
`('$Globals',6) ('UnityPerDraw',0) ('UnityPerMaterial',1) ('_Color',4) ('_GlobalMipBias',7) ('_MainTex',5) ('unity_ObjectToWorld',2) ('unity_WorldToObject',3)`
⇒ 索引 4 = `_Color`，`UnityPerMaterial` 里**只有这一个** vector 被该程序用到（`m_IsPartialCB=True`）。

**DXBC 寄存器号 ↔ 反射名的对应**（这一跳上一轮没做）：
字节码里 `dcl_constantbuffer CB0/CB1/CB2`，而三只 cb 的实读大小与用到的槽逐一对上 ——
`CB0` 用 `cb0[78..81]`（= `unity_MatrixVP` 四列）⇒ `$Globals`；
`CB1` 用 `cb1[0..3]`（= `unity_ObjectToWorld`）⇒ `UnityPerDraw`；
**`CB2` 声明 `CB2[2]`、只用到 `cb2[1]`（off 16）⇒ `UnityPerMaterial` 的 `_Color`。**
⇒ **`cb2[1]` = `_Color`**（上一轮那条判断是对的，别推翻）。

### 2·2 PS 说了什么
```bash
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
  D:/4/Unity/工具/disasm_dxbc.py "Extra Color" --stage ps --out D:/4/_tmp_view/dxbc/extra_color_ps.txt
```
48 个 PS 变体，其中 **Universal Forward 那 12 个**（其余是 `DepthOnly` / `MotionVectors` /
`DepthNormalsOnly` / `ShadowCaster` 四个 pass 的）去重后只有 **3 种形状**，三种都乘 `v2`：

| 形状 | 指令（逐条） | INTERP1 用量 |
|---|---|---|
| A（×4） | `sample_b r0, v1.xy, t0, s0, cb0[4].x` → `mul o0.xyz, r0.xyz, v2.xyz` → `mov o0.w, 1.0` | `xyz` |
| B（×4） | `sample_b r0, v1.xy, t0` → `mul o0.xyzw, r0.xyzw, v2.xyzw` | `xyzw` |
| C（×4） | `mul_sat r0.x = v3.y*4` → `mad/mul/min` 成 `fade` → `r1 = tex*v2` → `o0.w = fade*r1.w` · `o0.xyz = r1.xyz` | `xyzw` + `v3.y`（软粒子） |

**`v2` 是哪一个插值器**：VS 的 `dcl_output o2.xyzw` 与 PS 的 `INTERP 1 → register 2` 对应
（PS 侧签名 `INTERP 0 → v1` 用 `xy` = UV、`INTERP 1 → v2` 用 `xyz/xyzw` = 颜色）
⇒ **PS 乘的 `v2` 就是 VS 写的 `o2`**。

### 2·3 VS 说了什么（32 个变体逐个查过）
`v4` = 输入签名里的 `COLOR`（`dcl_input v4.xyzw`），`cb2[1]` = `_Color`：
```
182 log r0.xyz, |cb2[1].xyzx|          ; r0 = log2(|_Color|)
183 mul r0.xyz, r0.xyzx, l(0.416667)   ;  × 1/2.4
184 exp r0.xyz, r0.xyzx                ;  _Color^(1/2.4)
185 mad r0.xyz, r0.xyzx, l(1.055), l(-0.055)
186 ge  r1.xyz, l(0.003131), cb2[1].xyzx
187 mul r2.xyz, cb2[1].xyzx, l(12.923210)
188 movc r0.xyz, r1.xyzx, r2.xyzx, r0.xyzx   ; 这就是 Unity 的 LinearToSRGB
189 mul o2.xyz, r0.xyzx, v4.xyzx             ; × 顶点色
190 mul o2.w,  v4.w, cb2[1].w
```
`1/2.4 = 0.416667` · `1.055 / -0.055` · `12.9232 = 1/0.0774` · 分段点 `0.003131`
—— 与 Unity `Color.hlsl` 的 `LinearToSRGB` **逐常数吻合**。
**32/32 个 Universal Forward 变体都是这两行**（`CB1[43]` 那些是深度/MotionVectors pass，`o2` 是裁剪坐标）。

### 2·4 那份自建 shader 到底顶了几个原版 shader
`MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` 的 `Replacements` 里，
**指向 `WarpforgeVFX/Particles/Extra Color` 的原版名**（逐行实读 `:208 / :262 / :264 / :265 / :280 / :290 / :291 / :297 / :298`）：
`Everguild/FX/Extra Color` · `Everguild/FX/Particle Premultiply` · `Everguild/FX/TrailShader_Fading` ·
`Shader Graphs/Doomweaver effect` · `Shader Graphs/Eclipse Tau` ·
`Mobile/Particles/Additive` · `Mobile/Particles/Alpha Blended` ·
`Particles/Standard Unlit` · `Particles/Additive`（死条目）。
🔴 **而 `Everguild/FX/Extra Color` 不在 `UseOriginal` 白名单里** ——
`WarpforgeShaderMap.cs:200-201` 白纸黑字写着「⏸ **暂缓的两个大头**（改它们会一次性动 ~974 条效果，
要单独一批 + 一次全量 sweep 量过再动）：`Everguild/FX/Extra Color`（741）· `Everguild/FX/Particle Distortion Affect Transparents`（233）」。
⇒ 它今天**走 `Replacements` → 我们那份 shader**。

---

## 三、⚠️ 为什么 2026-10-02 那次 A/B 不能用来判这一条（**这条比结论本身重要**）

那一轮的判据是 `|ln(自建 ÷ 白名单)|`，出处 `资料/特效还原_进度与交接.md` §十四 与
`资料/比对基线/兜底路审计_1002j.tsv` 的表头（「**白名单 = 正常态（走原件）**」）。
⇒ 这个比值**只对「白名单里那些名字」有分辨力** —— 白名单那一侧才是**原版编译件**。
对 `Everguild/FX/Extra Color` 这种**不在白名单**的名字，**两侧都落到我们自建那份**：
正常态走 `Replacements`、`WFBIND_FORCE_BUILTIN=1` 也走 `Replacements`
⇒ 除非两侧抓取的时间点不同（= 比的是**我们 shader 的今天和昨天**），否则它恒等于 1，**看不见对错**。
🔴 结论：**「>0.10 15 → 126」量的是「我们那份 shader 变了多少」，不是「离原版近了多少」**
（对内置族是真回归 —— 那一族 `_Color` 是死值；对 `Everguild/FX/Extra Color` 那一族则测不出方向）。

**有效的尺子是另外两把**（都拿**原版侧**做基准）：
· `EffectCompare`（原版 | 导出 并排，`WFCMP_ISO=all` / `MATDUMP`）
· `EffectSweepBatch` 的 `sweep_orig.tsv` ↔ `sweep_exp.tsv`（即 `analyze_sweep.py` 那份台账）

⚠️ **而且仓库里现存的 `sweep_*.tsv` 已经不可用于本笔** ——
`资料/比对基线/README.md` 自己写着「**仓库里现在这份 TSV 是旧定义 —— 两趟都要重扫**」
（`sum` 从「Σ像素」改成「Σ|像素−底图|」、并加了棋盘底图两帧读取）。
我本笔**离线试算过**（`sweep_orig.tsv` vs `sweep_exp.tsv`，按「该效果只用了 EC」分组）：
`EC-only` 组均值 `ln(exp/orig) = −0.0564`、中位 `−0.0088`；`no-EC` 组均值 `−0.0160`、中位 `0.0000`
⇒ **方向对（EC 那族偏暗）但幅度被旧口径稀释到判不了**，所以**不拿它下结论**（记在这里只是留个线索）。

---

## 四、⏭ 该做的事（按判据强弱排；都还没做）

| # | 做法 | 为什么 | 成本 |
|---|---|---|---|
| **①** | 🔴 **把 `Everguild/FX/Extra Color` 加进 `UseOriginal` 白名单**（= `WarpforgeShaderMap.cs:200-201` 那件「暂缓的大头」） | 原件**就在随包 `wf_shaders.bundle` 里**（本笔实读：`Everguild/FX/Extra Color` **带字节码**）⇒ 直接绑原版编译件 = **零猜测、精确复刻 `LinearToSRGB(_Color)` 那一跳**，741 处引用一次到位；且与 `:119-139` 那批内置 shader 的成功先例同形 | 要一次**全量 sweep A/B**（那正是它被「暂缓」的理由）；⚠️ 先要证**不品红**（`TryResolve:434-441` 记着「bundle 里的 URP shader 在编辑器下渲不出来」那条） |
| **②** | 退一步：给 `Everguild/FX/Extra Color` **单开一份自建 shader**（算式 `tex × LinearToSRGB(_Color) × vcol`），别与内置族共用一份 | 与 §十四 D「`Multiply` 单开一份」、第三批「Legacy 两个单开」**同一个形状**；代价是仍属自建、仍要 A/B | 新 shader + 两处映射表 + 一次小批 `EffectCompare` |
| **③** | 两族共用那份 shader 的 `_Color` **保持不动**（现状） | 在 ①/② 落地之前，这是**当前唯一的**状态；⛔ 别再单独乘一次（那正是把内置族弄坏的那一手） | — |

**验证脚本（本笔写好的，下轮直接跑）**：
· 槽位/名字：`D:/4/_tmp_view/vfx23/slot_names.py "<shader 名片段>" [pass 名片段]`
· 对比原版材质 `_Color` 与我们导出的 `.mat`：`D:/4/_tmp_view/vfx23/ec_mats.py`
· PS 反汇编：`D:/4/Unity/工具/disasm_dxbc.py "<名>" --stage ps --out …`

---

## 五、没查清
- **`_Color` 在运行时到底以什么值到达 shader**：我们那份 `.mat` 里 `_Color` 有 **157/658** 非白
  （`(4,4,4,1)` ×18 · `(2,√2…)` ×9 · `(766.9961,…,0.6588)` ×4 …），但**原版材质上的原始值**本笔
  **没读到**（`battleprefabs_vfxandmisc_assets_all.bundle` 里按 `m_Shader.read()` 筛 `Extra Color`
  命中 **0** 个 ⇒ 材质不在那个包里；`ec_mats.py` 留着，下轮换包再跑）。
  ⇒ **「`_Color` 是 HDR 还是非 HDR 属性」也没读**（`m_PropInfo.m_Props[0].m_Flags` 的第 16/32 位）
  —— 它决定上传给 shader 时**做不做 sRGB→线性转换**，是 ①/② 落地前该补的一格。
- ①那条要证的「**不品红**」没跑（`ShaderFallbackProbe` 那条路能证）。
- `LinearToSRGB(_Color.a)` 参与 `o2.w` —— 我们的 `col.a` 路径**完全没管 `_Color.a`**；
  ①/② 落地时要连这一条一起判（今天不可观测）。
