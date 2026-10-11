# F3 · A1557 第②项 —— `ParticleSystemRenderer.m_ApplyActiveColorSpace`

> 2026-10-11 · 唯一改动人 = F3 写手（A1557 第②项）。**只动了白名单里那几个文件。**
> 上游：`F2_粒子渲染器三项.md`（同一件事的①，它 §五·2 明确点名「② 才是会改静止帧亮/色的那一项，下一轮做它」）。
> **本轮只改这一个变量**（顶点流 / `pivot` / `maxParticleSize` / `enableGPUInstancing` 一律没动）。

---

## 一、逐字段对照表（**核心交付**）

### 1.1 一张表

| 字段 | 原版值（13 场 578 颗，本轮重读） | 我们值（13 件 prefab 的 469 个 PSR） | 差 |
|---|---|---|---|
| **`m_ApplyActiveColorSpace`** | **True 401 / False 177** | **`: 1` 469/469（全 True）** 🔴 | **469 颗全不一样，其中 161 颗该是 `False`** |

> ⚠️ 「清单 578 颗」≠「prefab 里 469 个 PSR」：`BuildContent` 会跳过**无贴图 / 原版关着 /
> `renderMode=None` / 画质档不建**的粒子（既有行为，不是本轮引入）。逐场跳过数见 §1.3。

### 1.2 `battlearena1` 逐颗（34 颗全列，「我们值」是**改前的实测产物文本**）

| # | 粒子 | 原版 | 我们（改前） | 差 |
|---|---|---|---|---|
| 0 | `Embers (2)` | **False** | True | 🔴 该改 |
| 1 | `Ship Burst` | **False** | True | 🔴 该改 |
| 2 | `Bullets Controller` | **False** | True | 🔴 该改 |
| 3 | `Generator glows` | True | True | ✅ |
| 4 | `Bullets Burst 1` | **False** | True | 🔴 该改 |
| 5 | `TinyExplosion_Far` | **False** | True | 🔴 该改 |
| 6 | `Droppods` | True | True | ✅ |
| 7 | `Battle` | **False** | True | 🔴 该改 |
| 8 | `Generator glows (1)` | True | True | ✅ |
| 9 | `Embers` | **False** | True | 🔴 该改 |
| 10 | `Bullets Controller (1)` | **False** | True | 🔴 该改 |
| 11 | `Generator glows` | True | True | ✅ |
| 12 | `Shockwave` | **False** | True | 🔴 该改 |
| 13 | `Glow` | True | True | ✅ |
| 14 | `Bullets Burst 1` | **False** | True | 🔴 该改 |
| 15 | `ElectricalSparks` | **False** | True | 🔴 该改 |
| 16 | `Steam (2)` | **False** | True | 🔴 该改 |
| 17 | `Steam (1)` | **False** | True | 🔴 该改 |
| 18 | `Steam` | **False** | True | 🔴 该改 |
| **19** | **`TinyFlames`** | **False** | True | 🔴 该改 |
| 20 | `SmokeEffect (1)` | **False** | True | 🔴 该改 |
| 21 | `SmokeEffect` | **False** | True | 🔴 该改 |
| 22 | `SmokeEffect` | **False** | True | 🔴 该改 |
| 23 | `RisingSteam` | **False** | True | 🔴 该改 |
| 24 | `Embers` | **False** | True | 🔴 该改 |
| **25** | **`WildFire`** | **False** | True | 🔴 该改 |
| 26 | `TinyExplosion_Close` | **False** | True | 🔴 该改 |
| 27 | `Shockwave` | **False** | True | 🔴 该改 |
| 28 | `Embers` | **False** | True | 🔴 该改 |
| 29 | `Dust Floor (2)` | **False** | True | 🔴 该改 |
| 30 | `Dust Floor (3)` | **False** | True | 🔴 该改 |
| 31 | `Dust Floor (1)` | **False** | True | 🔴 该改 |
| 32 | `Generator glows (2)` | True | True | ✅ |
| 33 | `Dust Floor` | **False** | True | 🔴 该改 |

⇒ `battlearena1`：原版 **False 28 / True 6**（与 `F2` §一吻合）。该场 34 颗**一颗没被跳过**，
⇒ **改后这一场该出现 `m_ApplyActiveColorSpace: 0` 28 处**。

### 1.3 逐场（原版清单数 → **改后该在 prefab 里看到的 `: 0` 处数**）

| 场 | 清单颗数 | 原版 `False` | 建出的 PSR | **`: 0` 预期** | `: 1` 预期 |
|---|---|---|---|---|---|
| battlearena1 | 34 | 28 | 34 | **28** | 6 |
| battlearena2 | 46 | 43 | 44 | **41** | 3 |
| battlearena3 | 46 | 11 | 42 | **7** | 35 |
| battlearenaaeldari | 23 | 11 | 21 | **11** | 10 |
| battlearenaastramilitarum | 20 | 4 | 20 | **4** | 16 |
| battlearenablacklegion | 39 | 19 | 35 | **15** | 20 |
| battlearenadarkangels | 34 | 0 | 24 | **0** | 24 |
| battlearenaemperorschildren | 13 | 0 | 13 | **0** | 13 |
| battlearenagenestealers | 35 | 6 | 35 | **6** | 29 |
| battlearenaleviathan | 45 | 34 | 44 | **33** | 11 |
| battlearenasororitas | 49 | 0 | 49 | **0** | 49 |
| battlearenaspacewolves | 31 | 0 | 25 | **0** | 25 |
| battlearenatauviorla | 163 | 21 | 83 | **16** | 67 |
| **合计** | **578** | **177** | **469** | **161** | **308** |

### 1.4 🔑 上面这张「预期」是怎么算出来的（**不跑 Unity，且已被实测校验过**）

`.prefab` 里**刻着** `WarpforgeVFX.ArenaParticleIndex.index`
（= 清单 `particles[]` 下标，`ArenaBuilder` 建场时 `go.AddComponent<...>().index = pi` 刻上去的）
⇒ 直接读 prefab YAML 就能把「清单第 i 颗」和「prefab 里那一个 PSR」逐一对上，
再把原版的逐颗值按**真正建出来的那一份**求和。

🔴 **校验（这条是本报告最硬的一处）**：拿**同一套反查**去数
`m_UseCustomVertexStreams: 1`，得到的逐场数
**27 / 29 / 24 / 13 / 4 / 10 / 2 / 0 / 8 / 29 / 2 / 0 / 19、合计 167** ——
与 `grep -c` 实测的 13 件 prefab **逐场全等**，也与上一轮 `F2` 跑 Unity 得到的
「**167 == 167 ✅**」**逐场全等** ⇒ **这套对齐法本身被独立实测验证过**，
§1.3 的预期值可以直接拿来判红绿（本题的 `_nColorSpaceFalseTotal` 应 = **161**）。

⚠️ **不要**把「清单里的 177」当期望值（那会假红 16 处）：清单 578 颗里有 **109 颗根本没建**
（逐场 0/2/4/2/0/4/10/0/0/1/0/6/**80**）。

---

## 二、语义：这个字段在 **Linear** 下到底做什么

### 2.0 先答「它在我们这儿是活的吗」——**是**

| | 色彩空间 | 出处 |
|---|---|---|
| 我们 | **Linear** | `MyGame/ProjectSettings/ProjectSettings.asset:50  m_ActiveColorSpace: 1` |
| 原版 | **Linear** | `资料/普查产出_0920/场景光照与后处理_原版规格.md:130-131` —— **实况 probe 反证**：arena3 `RenderSettings.ambientProbe` 的 DC 实测 `0.61508 / 0.89479 / 1`，正好是序列化 `ambientLight (0.8066, 0.9522, 1.0)` 做 **sRGB→linear** 的结果 ⇒ 原版就是 Linear |

⇒ 这个开关**在原版和在我们这里都真的参与计算**，不是死值（这一点它和
`m_EnableGPUInstancing` 那种「两边材质都关着」的死值**不一样**）。

### 2.1 Unity 自己的原话（**不是我推的**）

- 本机装的那一版 Unity（**6000.3.23f1**）随编辑器发的
  `Editor/Data/Managed/UnityEngine/UnityEngine.ParticleSystemModule.xml`，
  条目 `P:UnityEngine.ParticleSystemRenderer.applyActiveColorSpace`：

  > **"Set whether colors will be converted appropriately before being passed to the GPU when using Linear Rendering."**

- 同一版编辑器的**复选框 tooltip**（`Editor/Data/Managed/UnityEngine/UnityEditor.CoreModule.dll`
  里的 UTF-16 串，偏移 `@9285959`；`UnityEditor.dll` 里也有一份）：

  > **"Apply Active Color Space"** / **"When using Linear Rendering, colors will be converted appropriately before being passed to the GPU."**

  ⚠️ 同一句 tooltip **`TrailRenderer` / `LineRenderer` 共用**（同 dump 的 `P:…TrailRenderer.applyActiveColorSpace`
  / `P:…LineRenderer.applyActiveColorSpace` 是**逐字相同**的 summary）。

### 2.2 它作用在**链路的哪一环**（这条是本地实证，不是推测）

- 三个共用这条设置/文档的组件 —— `ParticleSystemRenderer` / `TrailRenderer` / `LineRenderer` ——
  **恰好都是「顶点色由 CPU 生成后塞进顶点缓冲」的那一类**。
- 🔴 **换算不在着色器里**：URP 本包
  `MyGame/Library/PackageCache/com.unity.render-pipelines.universal@7865b6b91f8a/ShaderLibrary/Particles.hlsl:172-182`
  的 `GetParticleColor(half4 color)` **只处理实例化**（`unity_ParticleUseMeshColors` / `data.color`），
  **原样返回 `input.color`**，**没有**任何 sRGB/Linear 转换；
  各 pass 都是 `output.color = GetParticleColor(input.color);`
  （`ParticlesUnlitForwardPass.hlsl:108` · `ParticlesLitForwardPass.hlsl:98` · …）。
- ⇒ 结论：**换算发生在「送 GPU 之前」**（CPU 侧填顶点色缓冲那一步），开关就在渲染器上。
  **这就是「它改的是静止帧的亮/色、而不是动画平滑度」的机制依据**
  （`F2` 那一项改的是帧间 `blendUv`，`Particles.hlsl:148,152` 的 `lerp(color, color2, blendUv.z)`）
  —— 两项**不重叠**。

### 2.3 ⚠️ 诚实范围：**方向（sRGB→Linear 还是反过来）本地查不到**

- Unity 只写「converted **appropriately**」，**实现在 native**（`UnityPlayer` 里），
  本地**没有**源码/文档说方向。
- 按 Unity 色彩空间的通行惯例，它应是「**把作者在检视器里挑的颜色换算到当前色彩空间**」
  （Linear 工程下即 gamma→linear），但**这一句我没有本地判据**，**标为未直接查实**。
- ⇒ **不要**把它当「一定会变亮」或「一定会变暗」写进别处。**验收看 §四的读数，不看预期方向。**

### 2.4 原版那个 True/False 分布像什么（**这是推断，不是判据**）

`True 401 / False 177` 且 **False 集中在「老内容」上**的形态（`darkangels` / `emperorschildren` /
`sororitas` / `spacewolves` 四场 **一颗 False 都没有**，而 `battlearena1` 28/34 是 False），
更像「**该字段出现之前的对象留着旧默认 `0`**」而不是「作者逐效果挑的」。
🔴 **这是形态推断，我没有独立判据**（Unity 6000.x 新建 `ParticleSystemRenderer` 的默认是 `True` ——
这一点倒是本机实测：我们 13 件 prefab 全是 `True`、且那些是在本机 `AddComponent` 出来的）。
**照抄值即可，不必解释成因。**

---

## 三、改动清单

| 文件:行 | 改了什么（≤40 字） |
|---|---|
| `Unity/工具/gen_arena_particle_colorspace.py`（**新**，166 行） | 逐颗读原版该字段 → 灌进 13 场清单；名字/下标/行尾校验 + 幂等 + `--dry` |
| `.../WarpforgeArena1/arenas/*/<场>_manifest.json`（13 份） | 每颗粒子 **+1 行** `"applyActiveColorSpace": true/false`（**+578 / −0**，逐字节只加行） |
| `.../ArenaBuilder.cs:397-429` | `ParticleEntry` 新增 `bool applyActiveColorSpace;`（带语义/出处/键缺失风险注释） |
| `.../ArenaBuilder.cs:2710-2719` | 建场时 `rend.applyActiveColorSpace = p.applyActiveColorSpace;`（**只在键在时**）+ 计数 |
| `.../ArenaBuilder.cs:2030` | 本场计数器清零 |
| `.../ArenaBuilder.cs:2880-2889` | 逐场报「设成 False 的粒子 **N** 颗」+ 累加 13 场合计 |
| `.../ArenaBuilder.cs:553-563` | `BuildArenaPrefabs` 清零合计 + 调 `VerifyPrefabColorSpace()` |
| `.../ArenaBuilder.cs:605-656` | **产物自检** `VerifyPrefabColorSpace()`：数 prefab 文本里的 `: 0` / `: 1` |
| `.../ArenaBuilder.cs:3969-3996` | 新增静态件：`_manifestHasApplyColorSpace` / `_warnedNoColorSpace` / 两个计数器 |
| `.../ArenaBuilder.cs:3722-3735` | `LoadManifest`：**数清单文本**判「键在不在」，不在就**一个字都不改 + 出声** |

### 3.1 🔴 一个必须说明的设计：**「键在不在」为什么只能数文本**

`JsonUtility` 对**缺键的 `bool`** 给 `false`，而 Unity 新建 `ParticleSystemRenderer` 的默认是
`true` ⇒ **反序列化后分不清「键不在」和「原版就是 false」**。不判的话，**老清单会被静默整片设成
`false`**（578 颗全变，而且「看着很有依据」）。
⇒ 做法 = `LoadManifest` 里 `CountSub(txt, "\"applyActiveColorSpace\"") >= 粒子数`；
不足就 **`_manifestHasApplyColorSpace = false` + 出声**，`BuildContent` 里**一个字段都不写**
（保持 Unity 默认 `true` = 与改前完全一致）。
这与本工程既有的 `p.vertexStreams == null`（`JsonUtility` 对**缺键的数组**给 `null`）
**是同一个路子 —— 只是 `bool` 没有 `null` 可用，只能落到文本层**。

### 3.2 ⚠️ 会怎样回退
- 数据侧是**旁挂**：`gen_unity_arena_manifest.py` 的真身**不在本仓**（`d:/2/Warpforge_tools/scripts/`，
  ⛔ 白名单外）⇒ **任何人重跑生成器都会把这个键冲掉**；冲掉后 `ArenaBuilder` 会打
  「清单没有 `applyActiveColorSpace` 键」的告警（**不会静默**）⇒ 再跑一次本脚本即可。
  根治 = 在生成器的 `particles[]` 里补这个键（与 `pivot` / `vertexStreams` 同一件欠账）。
- 代码侧回退 = 删掉 `ArenaBuilder.cs` 那 10 行 `if (_manifestHasApplyColorSpace) {...}` 块
  （清单纯数据，留着无害）。
- 🔴 **旁挂脚本之间的顺序依赖**：`gen_arena_particlepivot.py` → `gen_arena_particle_streams.py`
  → **`gen_arena_particle_colorspace.py`**（本脚本拿 `"vertexStreams"` 行当锚点；
  顺序反了会**报错停下**，不会写坏）。

---

## 四、改前 / 改后读数

### 4.1 我跑了什么 / 没跑什么（**如实**）
- ✅ **跑了**：13 份清单的旁挂注入（含幂等复跑 + 行尾复核 + `git diff --numstat`）。
- ⛔ **没跑 Unity、没跑 `typecheck.sh`**（简报红线：同一工程只能一个实例、编译与渲染由主对话统一跑）。
  ⇒ **渲染读数我出不了，本节不编数。**

### 4.2 改前基线（**已实测**，13 件 `.prefab` 的产物文本）

| 场 | PSR | `m_ApplyActiveColorSpace: 1` | `: 0` |
|---|---|---|---|
| 全部 13 件 | **469** | **469** | **0** |

（逐场 PSR：34 / 44 / 42 / 21 / 20 / 35 / 24 / 13 / 35 / 44 / 49 / 25 / 83。）

### 4.3 主对话该看到的**三条可复算读数**

```bash
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
# 1) 重建 13 份【场景】（RenderPreview 读的是它）
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod ArenaBuilder.BuildAll -logFile -
# 2) 重建 13 份【prefab】（顺手打两条产物自检）
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod ArenaBuilder.BuildArenaPrefabs -logFile -
# 3) 渲一张（固定种子）
unset ELECTRON_RUN_AS_NODE && WF_ARENA=battlearena1 WF_PSFIXSEED=1 "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.RenderPreviewFromCLI -logFile -
```

**读数①（逐场日志计数）**：`[Arena] 色彩空间：<场> 把 applyActiveColorSpace 设成 False 的粒子 **N** 颗`
—— `battlearena1` 应 = **28**；13 场 = §1.3 那张表的「`: 0` 预期」列。

**读数②（产物自检，一行）**：
`[Arena] 色彩空间自检（读 13 件 prefab 的产物文本）：建场时设成 false 的 **161** 颗 vs prefab 里
m_ApplyActiveColorSpace: 0 **161** 处 —— **相等 ✅**；另 : 1 **308** 处`
—— 🔴 **判绿看 161 == 161 且 161 + 308 = 469**（不是看退出码）。

**读数③（手工 `grep -c`，与读数①②同口径）**：
```
cd d:/4/Unity/MyGame/Assets/Resources/ArenaPrefabs
grep -c "m_ApplyActiveColorSpace: 0" battlearena1.prefab      # 28
grep -c "m_ApplyActiveColorSpace: 1" battlearena1.prefab      # 6
```
13 件逐场预期 = §1.3 的 `: 0` / `: 1` 两列；两列之和必须 = 该件 PSR 数。
⚠️ **数的时候直接数 `: 0`**，别拿总数减 `: 1`。
⚠️ 若出现「建场时设成 false 的 **N** 颗 vs prefab 里 `: 0` **0** 处」⇒ **没落盘**，按红处置。

### 4.4 渲染读数（**我没跑，给主对话的取证协议**）
`工具/arena_sbs.py battlearena1 --rect 0.74,0.54,0.92,0.74 --gain 6`（`Barrels` 旁那团火）
+ `工具/arena_lightcompare.py`（或既有那张「13 场亮度比」表）。
🔴 **预期方向我刻意不写死**（见 §2.3）：**先改动前存一份图，再改动后比**，
把「哪些场/哪些块动了、动了多少」记下来 —— 这一项 161/469 颗都要动，
**并排图大概率看得出差别**（与 `F2` 那项「静止帧不动」正好相反）。
⚠️ 这一项和 `maxParticleSize` 不同：它**没有**做过「设了会退步」的 A/B，所以**先照原版设上**，
真要退步再按铁律 4 拿实况说话。

---

## 五、没查清 / 没做的 · 顺手发现的

1. ⚠️ **没查清（本轮唯一的实质空缺）**：§2.3 那条 —— **换算方向**。
   本地能读到的只有 Unity 的「converted **appropriately**」，实现在 native。
   要么进原版实况量、要么做一次 A/B（linear 与 sRGB 两条读法结论相反），**本轮做不了（不能跑 Unity）**。
2. ⚠️ **没做（只报不改）**：`工具/arena_particle_audit.py` 的 `MAPPED` 表**还是不认**
   `m_UseCustomVertexStreams` / `m_VertexStreams` / **`m_ApplyActiveColorSpace`**
   （`F1` §五·5、`F2` §五·7 **已记过两次**）⇒ **别拿它的「缺口清单」派活**。
   这个脚本**在本仓白名单外**（本轮没动它）。**这是第三次记账了，建议主对话直接修掉。**
3. ⚠️ **顺手发现 · 原版 `PlayerSettings` 没被解出来**：
   `d:/2/新解包资源/assets_full/globalgamemanagers/PlayerSettings/` 目录**存在但是空的**
   （同目录的 `GraphicsSettings/` 却解出来了）⇒ **原版的色彩空间不能从那里读**。
   本报告的「原版 = Linear」是从 `资料/普查产出_0920/…:130-131` 的**实况 probe** 反证的。
   （顺带：原版 Unity 版本 = **6000.2.6f2**，从 `Warpforge_Data/globalgamemanagers` 里读到；
   我们 = 6000.3.23f1，同大版本 ⇒ 枚举/属性集可比。）
4. ✅ **顺手发现（可复用，值得落进工具）**：**`.prefab` 里刻着的 `ArenaParticleIndex.index`
   就是「清单下标 ↔ prefab 对象」的现成映射** ⇒ **不跑 Unity 也能算「改后该出现多少处某字段」**。
   本轮靠它算出了 §1.3 那 13 组数，并**用上一轮跑 Unity 得到的 `m_UseCustomVertexStreams: 1`
   逐场数（167 == 167）反向校验通过**。建议把它做进 `工具/`（本轮的探针是临时脚本，在白名单外没留）。
5. ⚠️ **顺手发现（会误导下一个人的坑）**：**`.prefab` YAML 里 `!u!199` 块的先后顺序 ≠ 建场顺序**
   （我第一版就是按文件顺序对齐的，13 场里 12 场当场对不上）。
   ⇒ **只能用 `ArenaParticleIndex.index` 对齐，别用文件顺序。**
6. ⚠️ **顺手发现（既有行为，不是本轮引入）**：清单 578 颗里 **109 颗根本没建**
   —— 逐场跳过数 `0/2/4/2/0/4/10/0/0/1/0/6/**80**`，
   🔴 **`battlearenatauviorla` 是 163 颗只建了 83 颗（跳过 80 颗，占 49%）**。
   跳过口径是「无贴图 / 原版关着 / `renderMode=None` / 画质档不建」，`BuildContent` **有那行日志**，
   但这 80 颗**是不是全部合法**我没逐颗核 —— **建议主对话拿日志那行对一次**（若其中混着「该建却没建」的，
   那是另一笔账）。**本轮只报不改。**
7. ✅ **技术复核（没跑 Unity，人工核的）**：
   · `rend.applyActiveColorSpace`（get/set 都在）**在 6000.3.23f1 的
     `UnityEngine.ParticleSystemModule.dll` 里确有**（`applyActiveColorSpace` ×4 / `set_` ×2 / `get_` ×2）
     ⇒ 与上一轮那两个被 Unity 6 删掉的属性不同，**这条不会编不过**（⚠️ 编译这一关仍请主对话补）。
   · `ArenaBuilder.cs` 行尾 **CRLF（5163/5163，无孤立 LF）** · 括号平衡
     `{}` 1058/1058 · `()` 2431/2431 · `[]` 531/531 · `git diff --numstat` **375 / 1**
     （含前两轮 `pivot` 41 行 + `vertexStreams` 实测的那批；本轮的 `−1` 是 `LoadManifest`
     里那行 `File.ReadAllText` 被提到变量里）。
   · 13 份清单：**CRLF 保持**（`\r\n` == `\n` 逐份核过），`git diff --numstat` 每份 **+N / −0**
     （N = 该场粒子数：34/46/46/23/20/39/34/13/35/45/49/31/163），**幂等复跑后数字不变**。
