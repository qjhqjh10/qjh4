# W · 特效线 —— T01 `Extra Color` 改走原件 · T04 `UnlitAmbient` 顶点色分流 · T03 判据不足

> 会话：2026-10-19 · 特效线写手
> 🔴 **本笔没起 Unity**（不跑 `*.Run` / player）—— 只跑了秒级类型检查（`TMPDIR=/tmp/wf_vfx bash d:/4/Unity/工具/typecheck.sh`，**0 / 0**）。
> 只改了 `MyGame/Assets/WarpforgeVFX/**`（本线白名单），**没碰** `RuleEngine/**` / `CardPresentation/**` / `Arena1/Editor/**` / 数据 JSON / 任何正本。

---

## 一、逐条表

| T号 | 改了什么 | 文件:行 | 判据 | 验证 |
|---|---|---|---|---|
| **T01** | `Everguild/FX/Extra Color` **加进 `UseOriginal` 白名单**（741 处引用 ⇒ 正常态直接绑原版编译件）。`Replacements` 里那条**故意保留**（导出期占位材质 + 兜底路还要用），只把注释改成现状；「暂缓的两个大头」注释改成**只剩 `Particle Distortion Affect Transparents`（233）** | `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:216`（白名单新条目）· `:233`（`Replacements` 保留理由） | `资料/普查产出_第二十三会话/V23_特效ExtraColor的PS读数.md` §一/§二：原版最终色 = `tex × LinearToSRGB(_Color) × vcol`（`cb2[1]` = `_Color`，Unity 序列化反射 `off=16 ⇒ slot 1`；32/32 VS 变体 + 12/12 Universal Forward PS 变体）；原件在随包 `wf_shaders.bundle`（836,914 B，与 `shaders_assets_all.bundle` 逐字相同）且带编译字节码 ⇒ 走原件 = 零猜测复刻那一跳 | 类型检查 0/0 · 静态核：`ShaderResolveProbe.Run` 是**逐条遍历** `UseOriginal` 的真断言、**没有写死条数**（`ShaderResolveProbe.cs:80-89`）⇒ 新条目会被自动盯住（期望 `N 个走原件 / 0 个没走成`）· ⚠️ 不品红 / A/B **未跑**（命令 → §三） |
| **T04** | 基础 `Everguild/UnlitAmbient` 的**顶点色分流**：① shader 新增浮点闸 `_VertexColorLerpOn`，`frag` 里 `>0.5` 走 `lerp(tex.rgb, IN.color.rgb, IN.color.w) * _Color.rgb`、否则维持 `tex.rgb * _Color.rgb`；② 两个 pass 的 CBUFFER **同序**各加一行；③ `WarpforgeEffectBinder.Apply` **逐渲染器**判 `Mesh.HasVertexAttribute(VertexAttribute.Color)`（只对基础 `Everguild/UnlitAmbient` 开闸，Flickker 不开），另开一档材质缓存；④ 新增 `WFBIND_NO_VERTEXCOLOR=1` 诊断开关（A/B 用，默认关） | `WarpforgeVFX/Shaders/WFUnlitAmbient.shader:134`（属性）· `:217` / `:425`（两份 CBUFFER，同序）· `:350-354`（分流）；`WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs:94`（A/B 开关）· `:158`（逐渲染器取闸）· `:195/209/222`（`IsBaseUnlitAmbient` / `RendererHasVertexColor` / `MeshHasColor`）· `:275/278/367`（`Build` 第三档 + 缓存键 + 灌闸） | `资料/历史/项目任务_第二十五会话_搬出段.md` ⑨（判据全文）：原版基础 PS = `lerp(tex.rgb, v2.rgb, v2.w) * _Color.rgb`；`v2` 是不是真顶点色**取决于网格有没有 COLOR 流**（VS 段 0/1/2 `mov o2.xyzw, v4.xyzw` vs 段 3 `mov o2.xyz, cb0[61].xyzx` + `o2.w=0`）⇒ 必须按渲染器分流；一刀切走 lerp 会因 Unity 补 `(1,1,1,1)` 把**整片渲成纯白**（2026-10-02 实测 `1.766`） | 类型检查 0/0 · **逐渲染器静态普查（下面 §一·补）**：71 个用这 13 个 UnlitAmbient 材质的渲染器里，**38 个是基础 shader、其中 6 个有颜色通道**（`Environmental Condition Dark Angels Void Combat` 的 `Ship 1..6`，`ch3 dimension=4`）⇒ 闸会真打开、不是空操作；Flickker 的 33 个渲染器**不开闸**（判据：其 PS 用 `v3`、COLOR 支写 `(…, 0)` ⇒ lerp 恒等）· ⚠️ **A/B 与 HLSL 编译未跑**（命令 → §三） |
| **T03** | **未改任何资产 —— 判据不足**（按任务文件 §四 第 1 步只做了「分类 + 静态筛」，没有动手） | —— | 任务文件 `任务/T03_C组导出整个丢了.md` §二 第 3/4 条 + 正本 §P1-a0 `:628-631`（一半是「尺子的假象」：空场景没有内容可扭曲） | 见下面 §一·补② 的静态筛：**没有发现任何可脚本化的缺陷**；要定性必须做渲染侧两趟（命令 → §三） |

### §一·补① T04 的逐渲染器普查（静态、可复查）

做法：把 `WFUnlitAmbient.shader` 的 13 个材质 guid 反查 → 扫 `WarpforgeVFX/Prefabs/*.prefab` 的 `MeshRenderer` / `SkinnedMeshRenderer` → 每个渲染器的材质槽命中这 13 个之一 → 取同 GameObject 的 `MeshFilter.m_Mesh`（或 `SkinnedMeshRenderer.m_Mesh`）→ 读网格 `.asset` 的 `m_VertexData.m_Channels`，看**第 4 项（ch3 = COLOR）的 `dimension`**。

| prefab | 基础 / Flickker | 有颜色通道的渲染器 | 无颜色通道 |
|---|---|---|---|
| `Environmental Condition Dark Angels Void Combat` | 基础 | **6**（`Ship 1..6`，ch3 dim=4） | 0 |
| `EnvironmentalCondition Sororitas Disrupted Ceremony` | **Flickker** | 31（烛光网格，**不开闸**） | 2 |
| `Environmental Condition Space Wolves Full Moon` | 基础 | 0 | 23 |
| `Environmental Condition Space Wolves First Light` | 基础 | 0 | 2 |
| `EnvironmentalCondition Leviathan Blazing Biomatter` | 基础 | 0 | 2 |
| `EnvironmentalCondition Leviathan Sweeping Infestation` | 基础 | 0 | 2 |
| `Environmental Condition Astra Militarum Dawn Attack` | 基础 | 0 | 1 |
| `Environmental Condition Dark Angels Orbiting` | 基础 | 0 | 1 |
| `EnvironmentalCondition Ultramarines Aerial Clash` | 基础 | 0 | 1 |
| **合计** | —— | **37**（其中 6 个会开闸） | **34** |

⇒ **实际会改变取值的 = 1 个 prefab / 6 个渲染器**（`… Dark Angels Void Combat` 的 6 条 Ship）。⚠️ 该效果正是 T02 里「现在最差两条」之一（台账口径 `0.226`）—— **不要提前归因**，等 §三 的 A/B 读数。

### §一·补② T03 的静态筛（只证明「不是这几类可脚本化缺陷」）

对台账里 8 条 `分组 = C` 的效果逐个查（`资料/特效还原台账.tsv`，列名现读）：

| 检查项 | 结果 |
|---|---|
| prefab 是否存在 | **8/8 存在**（`WarpforgeVFX/Prefabs/<名>.prefab`） |
| 渲染器 / 粒子系统 | 13/7/16/19/55/23/37/19 个 `ParticleSystemRenderer`，全部有材质 |
| `WFMatDef.shader` 是否有未映射名 | 否 —— 所有名字要么在 `UseOriginal` / `Replacements`，要么是工程内 shader / 引擎内建 |
| Mesh 模式的粒子渲染器有没有空网格 | 否 —— 8 个 prefab 里 `m_RenderMode = 4(Mesh)` 的渲染器**都有** `m_Mesh` guid |
| `rendererSlots` / `trailSlots` 与渲染器材质槽对不对齐 | 对齐 —— 注意 `SpriteMask` 也是 `Renderer`（`!u!331`），`AcidSpraySweepAttack` / `Tidewall Gunrig` 各有 1 个，算上它之后槽数完全相等 |
| 粒子系统是否根本没开播 | 否 —— `playOnAwake = 0` **0 个**；`startDelay` 最大 2.0s（`Webway Rift`），未超出各自 `lengthInSec` |
| 技术构成里的「抓屏扭曲」 | 2 条（`Relentless Fusillade sniper` · `Tidewall Gunrig`）⇒ 仍是**尺子假象候选**，先不动 |

⇒ **结论：T03 的判据仍然不足**（关键假设是「原版那张屏幕贴图在我们测量环境里没绑上」，只有渲染侧能证）。**本轮未改资产，是对的。**

---

## 二、关键 diff

### 2·1 T01 —— `WarpforgeShaderMap.cs`

```diff
-            // ⏸ **暂缓的两个大头**（改它们会一次性动 ~974 条效果，要单独一批 + 一次全量 sweep 量过再动）：
-            //   `Everguild/FX/Extra Color`（741）· `Everguild/FX/Particle Distortion Affect Transparents`（233）
+            // ---- 🆕 2026-10-19：**`Everguild/FX/Extra Color`（741 处引用）整条改走原件** ----
+            // 🔴 原版这一族多一跳：`tex × LinearToSRGB(_Color) × vcol`（判据 → V23 §一/§二）
+            // 🔴 原件就在随包 `wf_shaders.bundle` 里、带编译字节码 ⇒ 零猜测、741 处一次到位
+            // ⚠️ `Replacements` 里那一条【保留不动】（导出期占位 + 兜底路）
+            "Everguild/FX/Extra Color",                                // 741
+            // ⏸ **暂缓的大头（仅剩这一个）**：`Everguild/FX/Particle Distortion Affect Transparents`（233）
         };
```

`Replacements` 那条**没有删**，只补了「为什么保留」的注释（`:233`）：① `EffectExporter.ShaderMap` 与它必须同步，占位材质不能是 bundle 里的 shader；② 兜底路要落它。

### 2·2 T04 —— `WFUnlitAmbient.shader` 的 `frag`

```hlsl
half3 col;
if (_VertexColorLerpOn > 0.5)
    col = lerp(tex.rgb, IN.color.rgb, IN.color.w) * _Color.rgb;
else
    col = tex.rgb * _Color.rgb;
```

两份 CBUFFER（`UniversalForward` 与 `ShadowCaster`）**同位置**各加了一行 `float _VertexColorLerpOn;`（SRP Batcher 要求逐字段同序；`:217` / `:425`）。

### 2·3 T04 —— `WarpforgeEffectBinder.Apply`

```csharp
bool vc = !SkipVertexColorLerp && IsBaseUnlitAmbient(materials[idx]) && RendererHasVertexColor(r);
var m = vc
    ? (em ? (builtVcEm[idx] ?? (builtVcEm[idx] = Build(materials[idx], true,  true)))
          : (builtVc[idx]   ?? (builtVc[idx]   = Build(materials[idx], false, true))))
    : (em ? (builtEm[idx]   ?? (builtEm[idx]   = Build(materials[idx], true)))
          : (built[idx]     ?? (built[idx]     = Build(materials[idx], false))));
```

`RendererHasVertexColor`：`SkinnedMeshRenderer.sharedMesh` / `MeshRenderer + MeshFilter.sharedMesh` → `Mesh.HasVertexAttribute(VertexAttribute.Color)`；**粒子渲染器一律 false**（理由写在方法注释里：粒子颜色由 `ParticleSystem` 每帧供，网格通道推不出 `IN.color` 的补值，宁可走旧路径也不冒「整片渲白」的风险）。

### 2·4 为什么用浮点闸、不是任务文件里写的「keyword 闸」

任务文件写的是「用一个 keyword 闸」，**这里按同一文件的既有先例改成了浮点闸**（`_EmissiveFlickerOn` 就是这么做的）：材质是运行时 `new Material(shader)` 出来的，**没有任何材质资产替这个 keyword 保活** ⇒ `shader_feature` 变体会在 player 构建里被剥掉、且**不报错**（`WFUnlitAmbient.shader:121-122` 已记这条）。浮点闸同样能单变量 A/B（`WFBIND_NO_VERTEXCOLOR=1`），且不增变体。**如果主对话坚持 keyword，需要先解决「变体保活」这一格。**

---

## 三、需要主对话跑的渲染对拍命令（**本笔不执行**）

> ⚠️ Unity 批处理**全局串行**、只有主对话能跑；下面全部是**待跑**。
> `UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"`，跑前 `unset ELECTRON_RUN_AS_NODE`。

### 3·1 T01 —— 解析链断言 + 不品红 + A/B

```bash
# ① 白名单解析（新条目会被 ShaderResolveProbe 逐条断言；期望 "… N 个走原件 / 0 个没走成"，N 比上次 +1）
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod ShaderResolveProbe.Run -logFile "d:/4/_tmp_view/shaderresolve_1019.log"

# ② 不品红 + A/B（EffectCompare 的 NameFilter 是源码里写死的、当前为空 = 全量 958 条）
#    主对话先临时把 EffectCompare.cs 的 NameFilter 改成一小批 EC 族效果，跑完还原。
#    建议目标（覆盖 0.3~3.0s 的内容时段，配合 WFCMP_SIMT）：
#      AcidSpraySweepAttack / Buff_SW_Runes 1 / BulletImpact_Lascannon_2shots / MegaBlasta
unset ELECTRON_RUN_AS_NODE && WFCMP_ISO=all WFCMP_SIMT=0.5 "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectCompare.Run -logFile "d:/4/_tmp_view/cmp_ec_1019.log"
#    判：_tmp_view/cmp/*_exp.png 出现品红 = 编译/加载坏（必须回滚白名单那一条）；
#        否则看 orig/exp 亮度 —— EC 族应整体变亮（旧口径 `EC-only 组均值 ln(exp/orig) = −0.0564` 是线索、不是判据）。
# ③ 属性级（可选，逐属性看 _Color 有没有正确到达原件）
unset ELECTRON_RUN_AS_NODE && WFCMP_MATDUMP=1 "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectCompare.Run -logFile "d:/4/_tmp_view/cmp_ec_matdump.log"
```

### 3·2 T04 —— 三点 A/B（单变量）

```bash
# 目标 = Environmental Condition Dark Angels Void Combat（唯一会开闸的 prefab，6 个 Ship）
# A = 新行为（我们的 shader + 闸开）
unset ELECTRON_RUN_AS_NODE && WFBIND_FORCE_BUILTIN="UnlitAmbient" WFCMP_ISO=all WFCMP_SIMT=1.2 \
  "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectCompare.Run -logFile "d:/4/_tmp_view/cmp_ua_new.log"

# B = 旧行为（我们的 shader + 闸关）—— 唯一变量
unset ELECTRON_RUN_AS_NODE && WFBIND_FORCE_BUILTIN="UnlitAmbient" WFBIND_NO_VERTEXCOLOR=1 \
  WFCMP_ISO=all WFCMP_SIMT=1.2 \
  "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectCompare.Run -logFile "d:/4/_tmp_view/cmp_ua_old.log"

# 对照 = 原版（不设 FORCE_BUILTIN；正常态本来就该是这一份）
unset ELECTRON_RUN_AS_NODE && WFCMP_ISO=all WFCMP_SIMT=1.2 "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectCompare.Run -logFile "d:/4/_tmp_view/cmp_ua_orig.log"
```

⚠️ `EffectCompare` / `EffectIso` 的 `NameFilter` / `Targets` 都是**源码里写死的数组**（本线白名单外，我不能改）⇒ 主对话按上面挑目标；`EffectIso` 还有两趟口径（`WFISO_SIDE=orig` 先、不设第二趟，见 `EffectIso.cs` 文件头 + `工具/run_iso.sh`）。

### 3·3 T03 —— 只做「判据确认」，别先修

```bash
# 先证「尺子」：对两条抓屏扭曲的（Relentless Fusillade sniper / Tidewall Gunrig）
# 用 EffectIso 两趟跑，看原版侧那张屏幕贴图到底有没有绑上（正本 §P1-a0 那条假象）。
WFISO_SIDE=orig ... -executeMethod EffectIso.Run -logFile "d:/4/_tmp_view/iso_t03_orig.log"
... -executeMethod EffectIso.Run -logFile "d:/4/_tmp_view/iso_t03_exp.log"
# 其余 6 条：EffectCompare 小批（WFCMP_ISO=all）看导出侧渲出的是 空 / 灰方块 / 品红，再判。
```

---

## 四、没查到 / 风险

1. **T01 的「不品红」与 A/B 没跑**（本笔不允许起 Unity）。`ShaderFallbackProbe` 的 28 条**不含** `Everguild/FX/Extra Color` ⇒ 这一格**没有现成的逐条实测**。
   缓解证据：① 原件在 `wf_shaders.bundle`、带字节码（V23 实读）；② 同 bundle 的 21+ 条 Everguild shader 走 `UseOriginal` 已成常态，`ShaderResolveProbe` 每次都会断言「全部走原版bundle」；③ `TryResolve` 里「bundle 里的 URP shader 在编辑器下渲不出来」那条**讲的是工程自带的 URP 内建 shader**，与 Everguild 自定义 shader 不是同一件事（`ShaderFallbackProbe.cs` 文件头已记这个区分）。
   ⇒ **仍需一次真跑确认**（§三 3·1）。
2. **T01：兜底路仍然偏暗**。原件包不在场时 `TryResolve` 会落回 `Replacements` = 我们那份（同时顶 8 个原版 shader、按内置族算式写）⇒ **EC 族在兜底路上少 `LinearToSRGB(_Color)` 那一跳**。这不是本次能修的（要单开一份自建 = V23 候选 ②），**记在这里别忘**。
3. **T01：`_Color` 运行时到底以什么值到达 shader —— 仍没读到**（V23 §五：`battleprefabs_vfxandmisc_assets_all.bundle` 里按 `m_Shader.read()` 筛 `Extra Color` 命中 0）。走原件后这一跳由原件负责，**但「材质属性表有没有正确灌进原件」仍需 `WFCMP_MATDUMP` 看一眼**。
4. **T01：`EffectExporter.ShaderMap` 没改、也不该改** —— 它管导出期占位材质，占位不能是 bundle 里的 shader；「两份表必须同步」这条在这里的正确读法是「`Replacements` 与它保持同一条，白名单单独加」。**别把白名单条目抄进 `EffectExporter.ShaderMap`。**
5. **T04：HLSL 没编译过**（只有 Unity 能编）。C# 0/0 只保证 binder 编得过；shader 侧的 CBUFFER 同序是逐字段人工核过的（两份都加在 `_EmissiveFlickerOn` 之后）。
6. **T04：`_VertexColorLerpOn` 是浮点闸不是 keyword** —— 与任务文件字面不同，理由见 §二·2·4。若主对话要求 keyword，先解决 player 变体保活。
7. **T04：粒子渲染器一律不开闸**（`RendererHasVertexColor` 返回 false）。若将来有 `UnlitAmbient` 材质挂到 `ParticleSystemRenderer(Mesh)` 且网格有颜色，会走旧路径 —— **保持现状、不会变坏**，但那一种情况没有复刻。
8. **T04：`Apply()` 是一次性的**（Awake）。运行时若有人换 `sharedMesh` 或改材质，闸不会重算 —— 与现有 `Apply` 的既有行为一致。
9. **T04 / Flickker 的顶点色乘白仍然存在**：`col += k * … * IN.color.rgb`，而战场 OBJ 链丢颜色通道 ⇒ `IN.color = (1,1,1,1)`。**修它在 `工具/gen_arena_*.py`（战场/资产线）**，本线不动。
10. **T03：判据不足，未动任何资产。** 静态筛只证明「不是 prefab 缺失 / 未映射 shader / 空网格 / 槽位错位 / 没开播」这五类；「抓屏扭曲的尺子假象」仍需渲染侧两趟。
11. **T02 提醒**：`… Dark Angels Void Combat`（T04 唯一会改到取值的 prefab）正是 T02 的「最差两条」之一（台账 `0.226`），且 T02 自己的口径是**先证不是抖动带**。**不要把 T04 的改动静默当成 T02 的答案**。
