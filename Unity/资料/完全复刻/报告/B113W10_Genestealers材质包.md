# W10 · B-42 —— Genestealers 水漫材质打进随包

> 日期 2026-11-01 · 第一百一十三会话 · 第 1 波 · 写手 `W10`
> 纯 python 离线（**未跑 Unity / 未动 git / 未改账 / 未改活状态表 / 未碰 `d:/2` 一个字节**）
> 判据来源 → `判据/B113W_对账_D_资源导出.md` 的 **B-42** 行

---

## 一、源包核查（`scenes_scenes_battlearenagenestealers.bundle`，23,702,832 B）

UnityPy 1.25.3 实读。源包是 **2 份内层 CAB**：
`CAB-dfc5d1957f6f8ec53aad732ef391cf25.sharedAssets`（`AssetBundle` 对象在**这一份**，pid 2）+ 主 CAB。
包内 `m_Container` **只有 1 条**（`Assets/Data/Scenes/Battle Scenarios/Battle Arena Genestealers.unity`）
⇒ 与 `scenes_scenes_mainmenuwarpforge` 同形：**地址表里没有这三件**，必须重打包。

| 要的东西 | 在不在 | 真身（名字 · pid · CAB） | 备注 |
|---|---|---|---|
| Shader `Everguild/FX/Genestealers Flood` | ✅ 在 | pid **36** @ `…sharedAssets` | `m_ParsedForm.m_Name` 读得到；**31 个 `m_PropInfo`** |
| Material `Battle Arena Genestealers Water Floor` | ✅ 在（**名字逐字相符**） | pid **18** @ `…sharedAssets` | `m_Shader` = `m_FileID 0` / pid 36（**同份引用**） |
| 贴图 ① `_MainTex` → `Genestealers Ground reflections` | ✅ 在 | pid **27** @ 同份 | 4096×4096，**流式**（`…sharedAssets.resS`） |
| 贴图 ② `_SampleTexture2D_4b2b807f0f904784b4448ef0c2b579e2_Texture_1_Texture2D` → `rain_ripples` | ✅ 在 | pid **28** @ 同份 | 256×256，**流式** |
| 贴图 ③ `_Noise` → `Noise Combined` | ⚠️ **不在本源包里** | pid **-8214795764591836323** @ `CAB-6e07b2d16fe55a89121640e51fae0bb9`（= **`duplicateassetisolation_assets_all.bundle`**） | 128×128，流式；材质里写的是 `m_FileID=10` |

**「3 张贴图」这句是对的，但它们不在一只包里** —— 前两张同份、走依赖树自动带上；
第三张（`_Noise`）在**另一个源包**，必须单独搬 + 改指针。
`_Noise` 不是死值：`Shader.m_PropInfo` 里 `_Noise` 类型 **4 = Texture**，材质 `_NoiseIntensity=0.005` 在用。

材质只有这 **3 个非空 `m_TexEnvs`**，其余 3 个（`unity_Lightmaps` / `unity_LightmapsInd` / `unity_ShadowMasks`）是空槽。

---

## 二、产物

| 文件 | 字节 | 内层 CAB | 对象 |
|---|---|---|---|
| `Assets/StreamingAssets/WarpforgeVFX/wf_arena_genestealers.bundle` | **5,160,078** | `CAB-wfarenags`（+ `CAB-wfarenags.resS`） | `Material`×1 · `Shader`×1 · `Texture2D`×2 · `AssetBundle`×1 |
| `…/wf_arena_genestealers_noise.bundle` | **81,616** | `CAB-wfarenagsnoise`（+ `.resS`） | `Texture2D`×1 · `AssetBundle`×1 |
| （对照）`…/wf_arena_shaders.bundle` | 143,037（**未动**） | `CAB-68cc81d3…` | `Shader`×7 · `AssetBundle`×1 —— 一个字节没变 |

主产物 `AssetBundle` 关键字段：`m_Container = 4` · `m_PreloadTable = 2` ·
`m_IsStreamedSceneAssetBundle = False`（源包带出来的，已清，否则 `LoadAllAssets` 抛异常）·
`m_Dependencies` 含 `cab-wfarenagsnoise`（21 条）。

**容器表（全 4 条，每条都给「裸名 + 小写路径别名」）**

| 容器键 | 目标 |
|---|---|
| `Battle Arena Genestealers Water Floor` | `fid=0` pid 18（Material） |
| `assets/battle arena genestealers water floor.mat` | `fid=0` pid 18 |
| `Everguild/FX/Genestealers Flood` | `fid=0` pid 36（Shader） |
| `assets/everguild/fx/genestealers flood.shader` | `fid=0` pid 36 |

流的去向：`CAB-wfarenags.resS` = 56,709 KB → **22,187 KB**（只切 2 个对象用到的区间）；
`CAB-wfarenagsnoise.resS` = 16,648 KB → **64 KB**。

---

## 三、复扫读数（UnityPy 重开产物，按运行时那两条路逐项试）

| 检查项 | 读数 |
|---|---|
| `WarpforgeShaderLoader` 那条（`LoadAllAssets<Shader>()`） | ✅ 1 个：`Everguild/FX/Genestealers Flood`（pid 36） |
| 容器里按**裸名** `Contains(name)` | ✅ 命中（`Everguild/FX/Genestealers Flood`） |
| 运行时 ① —— `GetAllAssetNames()` 里按**文件基名**找材质 | ✅ `assets/battle arena genestealers water floor.mat` |
| 运行时 ② —— `LoadAsset<Material>("Battle Arena Genestealers Water Floor")` | ✅ 命中（容器键就是原版材质名） |
| 材质 `m_Shader` | ✅ `fid=0` pid 36 → **包内** |
| `_MainTex` | ✅ `fid=0` pid 27 → 包内 `Genestealers Ground reflections` |
| `_SampleTexture2D_…` | ✅ `fid=0` pid 28 → 包内 `rain_ripples` |
| `_Noise` | ✅ **跨包但有主**：`fid=22` → `archive:/CAB-wfarenagsnoise/CAB-wfarenagsnoise`，且该 CAB 已写进主产物 `m_Dependencies`；对面包里确有 `Texture2D Noise Combined`（128×128，`size=65536`） |
| 悬空引用 | **0**（3 个非空贴图槽全部有主：2 个包内 + 1 个已发出去的依赖包） |

> 另有一条**不是我们造成的**外部引用，已核实能解析：`Shader.m_Dependencies[0]` 指向
> `Hidden/Shader Graph/FallbackError`（pid 3819691914921817934）@ `CAB-d47690319398b604c3bb5a35a8ed2499`
> （= `battleprefabs_vfxandmisc_assets_all.bundle`）。该 shader **已经在随包**
> `wf_shaders_extra.bundle` 里，且**内层 CAB 名与 pid 都相同**（实扫命中）⇒ 运行时靠
> `WarpforgeShaderLoader` 常驻加载的那只包解析得到。

---

## 四、改动清单

| 文件 | 净行数 | 改了什么 |
|---|---|---|
| `Unity/工具/extract_missing_shaders.py` | **+138 / −2**（2,898 → 3,034 行，**纯 LF、0 裸 CR**） | ① 新常量段 `GENESTEALERS_SRC` / `DEST_ARENA_GS` / `ARENA_FLOOD_*` / `ARENA_FLOOD_IMPORTS`（含三件 pid 与判据出处）；② 新入口 `run_arena_flood()`；③ `argparse` 加 `--arena-flood`；④ `main()` 分发；⑤ `repack_tree()` 放行「**只有贴图根**」的包（`if not found` → `if not found and not tex_roots`）；⑥ `_build_import_parts()` 加 `kind="texture"`；⑦ 文件头补本模式一节 + A523 模式表加一行 |
| `Assets/StreamingAssets/WarpforgeVFX/wf_arena_genestealers.bundle` | 新增 5,160,078 B | 主产物 |
| `Assets/StreamingAssets/WarpforgeVFX/wf_arena_genestealers_noise.bundle` | 新增 81,616 B | `Noise Combined` 依赖包 |

**没有新开重打包口**：主产物走 `repack_tree()`、外部贴图走 `merge_imports_into_bundle()`（`B102_D10` 那条分包路），
写盘仍只经 `_write_bundle()`。⑤⑥ 是**两条只加分支**的扩展（原调用方一个都不受影响）。
回归：`--booster-mats --check`（会走 `_build_import_parts` 的 material + shader 两类）实跑 **rc=0**，
6 只依赖包全部照原样打出来。

**`.cs` 一个都没改** ⇒ 不需要 typecheck。⚠️ 运行时那条仍要 Unity 验（本波不跑 Unity）。

---

## 五、顺手发现 / 尚需确认

1. 🔴 **`wf_arena_shaders.bundle` 是个「现在没有任何模式能重出」的遗留产物，而且它名字具有误导性。**
   现读它装着 **7 个战场 shader**（`Everguild/FX/Tyranids/Pulsating Mesh` / `Tyranid Tentacle` +
   5 个 `Hidden/VFX/Aeldari …`），内层 CAB = `CAB-68cc81d35f88d5ad277f2717d6e1ac25`
   （= `battlesharedresources_assets_all`），mtime 2026-09-21。
   但今天 `run_arenas()` 的产物名是 **`wf_arena_<slug>.bundle`** —— `battlesharedresources` 那一路会写成
   **`wf_arena_shared.bundle`**（盘上**不存在**），`DEST_ARENA` 常量只剩「`args.out` 的默认值」这一个用途。
   ⇒ **这就是我没把本件写进它的原因**（本工具一次只读一个 `src`，重写会静默删掉那 7 个，
   症状是利维坦的肉不搏动、触手变冻棍）。
   📌 建议调度台定夺：要么把 `run_arenas()` 的 slug 规则改成「`shared` 那份仍写 `wf_arena_shaders`」
   （否则以后谁跑一次 `--arenas` 就会多出一只 `wf_arena_shared.bundle`、与前一份内容重复），
   要么把现有文件改名成 `wf_arena_shared.bundle`。**两条都不在本次白名单内，我没动。**
2. **产物命名与任务单不一致（这是唯一一处偏离，特此点名）**：任务单写的产物是 `wf_arena_shaders.bundle`，
   我改成**新增** `wf_arena_genestealers.bundle`。运行时等价、无需改任何 `.cs`：
   `WarpforgeShaderLoader.ArenaBundlePattern = "wf_arena_*.bundle"`（通配命中）、
   `ScenarioBlendables.ShippedBundles()` 是**枚举整个目录的 `*.bundle`**（材质也命中）。
3. **`StreamingAssets/WarpforgeVFX/` 整个目录被 `.gitignore:41` 忽略** ⇒ 这两只产物**不进 git**，
   和目录里其它 30 多只 bundle 一样是「本地生成的资产」。（也因此没有 `.meta` 需要我补。）
4. **真机「看到水」的验收条件**（本波不跑 Unity，留给调度台/用户）：
   · 前置：跑一次 `python 工具/extract_missing_shaders.py --arena-flood`（已跑过，产物在盘上）；
   · 进 **Genestealers 战场**，触发 `SumpOverspill` 那条环境混合；
   · 期望：**不再出现** `[EnvBlend] 原版材质 `Battle Arena Genestealers Water Floor` 在随包 … 里取不到`
     这条 `Debug.LogWarning`（`ScenarioBlendables.cs:1543`）；
   · 期望：水面材质真的换了（`_Water_level 0.86` / `_Rain_Intensity 0.156` / `_Water_Transition 1.84` 那组）；
   · 还可以顺带看有没有 `[WarpforgeVFX]` 侧的 shader 缺失告警。
5. **未验的一条**：跨 bundle 依赖（`_Noise` → `CAB-wfarenagsnoise`）在 Unity 6 上认不认，
   与 `B102_D10` 那条分包路是**同一个未验点**（`BoosterPackMaterialProbe` 那类探针）。本机没有额外证据。
6. **`_Noise` 只影响细节**（`_NoiseIntensity = 0.005`，`_NOISECHANNEL = 0`）：即使运行时那条跨包引用解析不到，
   水漫主体（`_MainTex` 反射 + `rain_ripples`）**仍然是对的** ⇒ 不会整块失效，只是噪声调制缺席。

---

## 六、复现命令

```bash
# 只体检（两步都 dry-run，不写 StreamingAssets）
python -I -X utf8 d:/4/Unity/工具/extract_missing_shaders.py --arena-flood --check
# 真写两个包
python -I -X utf8 d:/4/Unity/工具/extract_missing_shaders.py --arena-flood
```
