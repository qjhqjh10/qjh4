# F1 · A1552 —— `battlearena1` `Barrels` 旁那团火（比原版暗一半、偏蓝）

> 2026-10-11 · 唯一改动人 = F1 写手。**这条只写本件**。
> 上游账：`资料/战场13场_逐场对账_0920.md` `:297-311`（第十八会话新开的那一笔）。

---

## 一、根因（坐实）

**原版每颗粒子的 `ParticleSystemRenderer.m_Pivot`，我们的生成器从来没抽、`ArenaBuilder` 从来没接 —— 一律吃 Unity 默认 `(0,0,0)`。**

- **判据（第一权威 = 原版 prefab）**：`ParticleSystemRenderer.m_Pivot`，
  `d:/2/解包整理/07_场景/<场>/` 按 PathID 解外链。
- **受影响的那颗**：`battlearena1` / `Scenario/Particles/TinyFlames`，
  原版 `m_Pivot = (-0.1, **0.48**, 0)`；`particles[19]`。
- **几何链（为什么它等于「暗一半 + 偏蓝」）**：
  1. 该粒子的世界位置 `y = 0.0098`（**正好贴在地面上**）；
  2. `renderAlignment = 0 (View)` ⇒ 公告板**以粒子位置为中心**画，不套 pivot 时
     贴片覆盖 `y ∈ [-0.18, +0.18]`（尺寸 `0.5~0.75 × 缩放 0.48734`）；
  3. 材质 `ZWrite=0` 但 **`ZTest` 仍是 `LEqual`** ⇒ **`y<0` 的那半张被不透明地板深度剔除**；
  4. `TinyFlame` 那张 11×11 翻页图里，**91.5% 的 alpha 质量落在 tile 的下半张**
     （实测 `frame 60`：上半张只占 9.1%；`frame 45` 4.8%）
     ⇒ 被吃掉的是**火的根部与亮核**，留下的是最淡的**火苗尖**。
     ⇒ 视觉：**又小、又暗、偏橙红**；区域均值**偏蓝** = 火苗没了、露出偏冷的地板。
  5. 原版 `pivot.y = +0.48` 把整张抬 `0.48 × 尺寸 ≈ 0.18` ⇒ 贴片覆盖 `y ∈ [-0.001, +0.37]`，
     **火苗正好「站」在地面上**。（`+Y` = 抬高：`RisingSteam` `+0.5` · `WildFire` `+0.4` ·
     `Fire` `+0.39` · `CandleFlame_*` `+0.2` —— 全是贴地效果，方向自洽。）

### 这不是一颗的事 —— 它是**整族**的

`工具/gen_arena_particlepivot.py` 逐颗读原版，13 场实测：

| | 粒子总数 | 原版 pivot 非零 | 其中 `Y>0`（抬高） | `Y<0` |
|---|---|---|---|---|
| 13 场合计 | **578** | **151** | **90** | 12 |

非零的**几乎全是「火 / 焰 / 贴地效果」**：`Fire (0,0.39,0)`（`battlearenasororitas` 一场就 11 颗）、
`Flames (0,0.1,0)`、`CandleFlame_{big,mid,small} (0,0.2,0)`、`TinyFlames (-0.1,0.48,0)`、
`WildFire (0,0.4,0)`、`RisingSteam (0,0.5,0)`、`Fume Burst (0,0.85,0)`、`Green Vapours (0,0.5,0)`、
`Floor Smoke 3 (0,0.5,0)`、`ToxicPoolSplash (0,0.51,0)` …
⇒ **`battlearena1` 的 `WildFire`（烟囱火）同样中招**，一份图就能看出来。

**实测读数（我们自己渲的两张图 vs 原版真渲图，1280 空间）**

| 度量 | 原版 | 我们 |
|---|---|---|
| 全图 `R-B>140` 像素 | **539** | **16** |
| 全图 `R-B>100` 像素 | **1290** | **519** |
| `Barrels` 火区 `R-B>60` 像素（藏掉两块底板后） | 1297 | 14 |
| `Barrels` 火区最强暖像素质量（同上） | 160 923 | 983 |
| 烟囱火区（470–520, 80–120）`R-B>100` 像素 | 39 | 2 |

并排图（左=原版 · 右=我们，对比拉伸）`_tmp_view/env52/_exp_hideFire_arena1.png` 已能直观看出来：
我们只剩一排**暗红小火苗尖**，原版是一整团**亮黄火**。

---

## 二、逐字段对照表（本条核心交付）

### 2.1 粒子系统本身 —— **全对**（45 字段 × 34 颗，0 处真差异）

用归一化比较（把两边都折成「表示无关」的规范形——`MinMaxCurve` 的
`Constant/TwoConstants/Curve/TwoCurves` 与清单的 `[min,max] / {isConst,c,cMin} / {mult,keys}` 互折），
`battlearena1` 逐颗逐字段比：

| 字段 | 原版 | 我们 | 差 |
|---|---|---|---|
| `startLifetime` / `startSpeed` / `startSize` / `startSizeY/Z` / `size3D` | — | — | **0**（仅表示法不同） |
| `startRotation` / `rotation3D` / `randomizeRotationDirection` | — | — | **0** |
| `startColor` | `MinMaxGradient`（含 min/max 两色） | 只取 max 色（near-white） | 近白，无可见差 |
| `maxNumParticles` / `gravityModifier` / `emission.rateOverTime` | — | — | **0** |
| `colorOverLifetime` / `sizeOverLifetime` / `sizeOL.separateAxes` | — | — | **0** |
| `uv.enabled/tilesX/tilesY/animationType/timeMode/fps/cycles/rowIndex` | — | — | **0**（`TinyFlames` 11×11 · fps 30 · cycles 1） |
| `shape.type/radius/angle/arc/pos/rot/scale/radiusThickness` | — | — | **0** |
| `duration/looping/prewarm/simulationSpeed/scalingMode` | — | — | **0** |

⇒ **账上给的那条「下一步：读 prefab 侧的 `startColor` / `size` / `rate`」—— 走完了，这三样全对，不是根因。**

### 2.2 材质 / 贴图 —— **全对**

| 项 | 原版 | 我们 | 差 |
|---|---|---|---|
| `TinyFlame` 材质 49 项属性 | `_SrcBlend=5 _DstBlend=10 _ZWrite=0 _Surface=1 _BaseColor=(1,1,1,1) …` | 逐项相同（0 处） | **0** |
| `m_ValidKeywords` | `_FADING_ON _FLIPBOOKBLENDING_ON _SOFTPARTICLES_ON _SURFACE_TYPE_TRANSPARENT` | 同上 | **0** |
| `m_CustomRenderQueue` | **3005** | `3000 + _QueueOffset(5)` = **3005** | **0** |
| `TinyFlame.png` | 2048² | 2048²，**逐像素 max diff = 0** | **0** |
| 贴图槽 `_BaseMap` | — | 已绑（`guid faf90161…`） | **0** |

### 2.3 `ParticleSystemRenderer` —— **这就是差异所在**

原版值 = 逐颗读原版 prefab；我们值 = 读**已建好的**`Assets/Resources/ArenaPrefabs/battlearena1.prefab`（34 颗全看）。

| 字段 | 原版（`battlearena1`） | 我们（34 颗全部） | 差 |
|---|---|---|---|
| **`m_Pivot`** | 6/34 非零：`TinyFlames (-0.1,0.48,0)` · `WildFire (0,0.4,0)` · `RisingSteam (0,0.5,0)` · `SmokeEffect ×3 (0,0,-0.28)` | **全部 `(0,0,0)`**（清单没这个键、C# 没这句） | 🔴 **本条根因** |
| `m_VertexStreams` | 5 种取值（`000304` / `00010304` / **`0003040508`** / `0001030405` / `000103040508`） | **全部 `00010304`**（Unity 默认） | 🔴 未接（见 §五） |
| `m_UseCustomVertexStreams` | 28/34 = `True` | 全部 `False` | 🔴 未接（同上） |
| `m_ApplyActiveColorSpace` | 28/34 = **`False`**、6/34 = `True` | 全部 `True`（Unity 默认） | 🔴 未接 |
| `m_MaxParticleSize` | `0.5`×17 · `5`×10 · `10`×3 · `20`×3 · `1`×1 | 全部 `0.5` | ⚪ **故意不设**（2026-09-21 实测：设了 arena3 亮度比 1.032→1.181）— **别当漏了** |
| `m_MinParticleSize` / `m_RenderMode` / `m_RenderAlignment` / `m_SortingFudge` / `m_LengthScale` / `m_EnableGPUInstancing` | — | — | **0** |

---

## 三、改动清单

| 文件:行 | 改了什么 |
|---|---|
| `Unity/工具/gen_arena_particlepivot.py`（新） | 逐颗读原版 `m_Pivot` → 灌进 13 场清单的 `particles[i].pivot`；名字/下标/行尾三重校验，幂等 |
| `.../WarpforgeArena1/arenas/*/**_manifest.json`（13 份） | 每颗粒子新增一行 `"pivot": [x, y, z],`（**+578 / −0，逐字节只加行**） |
| `.../WarpforgeArena1/Editor/ArenaBuilder.cs` `:366` | `ParticleData` 新增 `public float[] pivot;`（带判据说明） |
| `.../WarpforgeArena1/Editor/ArenaBuilder.cs` `:2459` | 设 `rend.pivot = ToVec3(p.pivot, Vector3.zero)`；**清单没这个键就出一句告警**（不许静默按 0 画） |
| `.../ArenaBuilder.cs` `:3658` | `static bool _warnedNoPivot`（上面那条告警只报一次） |

**只动了一个变量**（`pivot`）。`maxParticleSize` / `vertexStreams` / `applyActiveColorSpace` **一律没动**。

⚠️ **`ArenaBuilder` 是建场期跑的** ⇒ 要生效必须**重跑 `ArenaBuilder.BuildAll`**（或单场建场），
再 `RenderPreviewFromCLI`；`Resources/ArenaPrefabs/*.prefab` 与 `WarpforgeArena1/Scenes/*.unity`
是它的产物，**没重建就还是旧画面**。

⚠️ **回归风险**：这条会**改 13 场的亮度**（正是目的，方向 = 向原版靠）。
`BattleScene.Run` 里若有卡在 ±5% 边缘的亮度比断言，**可能被推动**（大概率是变好，但要看数）。

---

## 四、改前 / 改后读数

**我没跑 Unity（红线：同一工程只能一个实例、只有主对话能跑）** ⇒ 这一格**我出不了数**，
**需要主对话渲一张才能出读数**。建议的命令（与账里同一套）：

```bash
# 1) 重建（pivot 在 BuildContent 里生效）
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod ArenaBuilder.BuildAll -logFile -
# 2) 渲一张（固定种子，才能和原版逐像素比）
WF_ARENA=battlearena1 WF_PSFIXSEED=1 ... -executeMethod ArenaBuilder.RenderPreviewFromCLI
```
对照锚点（1280 空间，框 `(1020,450)-(1074,484)`）：
- 现读数（**改前**）：`mean (109.3, 85.6, 50.2)` · `max (152, 131, 97)`；全图 `R-B>140` = **16**。
- 原版：`mean (102.5, 78.8, 29.2)` · `max (244, 223, 76)`；全图 `R-B>140` = **539**。
- **改后该看**：该框 `R-B>60` 的像素数从 **14** 往 **1297** 靠；全图 `R-B>140` 从 16 往 539 靠。

---

## 五、没查清 / 没做的 · 顺手发现的

1. 🔴 **没查清（已记，别丢）：`ParticleSystemRenderer.m_VertexStreams` 我们也没接。**
   原版 `TinyFlames` 是 `m_UseCustomVertexStreams=1` + `[0,3,4,5,8]`，其中 **5 = `UV2BlendAndFrame`**；
   而它的材质开着 `_FLIPBOOKBLENDING_ON`，URP `Particles/Unlit` 在那个关键字下**确实读
   `texcoord0.zw` + `texcoord1.x`**（`Library/PackageCache/.../ShaderLibrary/Particles.hlsl:184-230`）。
   我们 34 颗全是 Unity 默认 `[0,1,3,4]`。
   **13 场相关性**：`battlearena1` 里「stream 5 存在 ⇔ 材质带 `_FLIPBOOKBLENDING_ON`」**30/34 命中**，
   4 个反例（`TinyExplosion_Far` / `ElectricalSparks` / `RisingSteam` / `TinyExplosion_Close`）是
   **只加不删**留下的残留 ⇒ 原版是**按需加**的。
   **⚠️ 但不能据此断定它是本条根因**：`WildFire`（材质 `LargeFlame02` 同样带该关键字、
   stream 同样缺）在我们这边**也**只有 2 个强暖像素 —— 症状同族，可**机制未坐实**（无 Unity 可验）。
   ⇒ **按铁律 11：要补**（生成器 + `ArenaBuilder` 两侧），**但按「一次只改一个变量」没并进本次**。
2. 🔴 **没做：`m_ApplyActiveColorSpace`**（原版 28/34 = `False`，我们全 `True`）。
   语义 = 「Linear 渲染下顶点色要不要做 sRGB→Linear」。注释里那句「`LinearToSRGB` 先过一道」
   （`资料/普查产出_0918/E组…:④`）说明这族颜色的空间处理很敏感。**属待办，未动。**
3. ⚠️ **顺手发现 · 生成器的坑（会咬下一次）**：`gen_unity_arena_manifest.py` **不在本仓**
   （真身在 `d:/2/Warpforge_tools/scripts/`，**不在白名单**）⇒ 我补的 `pivot` 键是
   **旁挂在清单上**的。**任何人重跑生成器都会把它冲掉**（冲掉后 `ArenaBuilder` 会打
   「清单没有 `pivot` 键」的告警，**不会静默**）。根治 = 在生成器的 `particles[]` 里补 `pivot`。
4. ⚠️ **顺手发现 · 本文件 `:310` 那句已过期**：「我们的旁挂清单**不带**这几个字段」——
   2026-10-11 现读 **manifest 全带** `startColor`/`startSize`/`emissionRate`，且
   `ArenaBuilder :2198 / :2173 / :2208` 三处都在消费。（主对话已在简报里订正过，此处留痕。）
5. ⚠️ **顺手发现 · `工具/arena_particle_audit.py` 的 `MAPPED` 表已过期**：
   它仍把 `simulationSpeed` · `ShapeModule.m_Position/m_Rotation/m_Scale` · `InitialModule.startSizeY/Z` ·
   `startRotation*` 报成 `[未接]`，而生成器**早就接了**。⇒ **别再按它的「缺口清单」派活**
   （它给 `battlearena1` 报「56 个缺口」，逐条核下来真缺口只有
   `emission.rateOverDistance` 一个 —— 而那条在 `moveWithTransform=World` 的静止发射体上**本来就不发射**）。
   **它只看 `MAPPED` 表、不看生成器实际写了什么**。
6. ⚠️ **没跑 `工具/typecheck.sh`**（简报红线：主对话统一跑）⇒ **`ArenaBuilder.cs` 只做了人工核对**
   （`ToVec3(float[], Vector3)` 签名在 `:3831` ✅ · CRLF 保持 ✅ · `git diff --numstat` = **41/0** ✅）。
   **编译这一关请主对话补**。
