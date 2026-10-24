# B5 · 补上原版 `GrabScreenFeature` 的 **event 501 重画**（W64 · 兜底审计 B14）

> 代理 B5 · 2026-10-24 · 红线：**没跑 Unity / 没动 git / 没改正本台账 / 没越白名单**。
> 判据优先级：反编译方法体 > 解包资源字段 > 本仓文档。

---

## 〇、结论（先看这一节）

| # | 事 | 结果 |
|---|---|---|
| 1 | **501 重画 pass** | ✅ **已写进** `Assets/WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs`（新类 `GrabRedrawPass`），逐项照反编译（event / 三条 ShaderTagId / FilteringSettings / RenderStateBlock / SortingCriteria / 画到 activeColor + 读 activeDepth） |
| 2 | **500 那一次** | ✅ **一个字节没动**（`GrabPassPass` 原样；`Create/AddRenderPasses` 里对它的两行也没改） |
| 3 | **开关** | ✅ `WF_GRAB_501=0/1`（环境变量优先）+ 序列化 `redrawAt501`（默认 **false**）+ `redrawLayerMask`（默认 **0**）+ A/B 用 `WF_GRAB_501_LAYER=<层号>`。**关掉时只 Enqueue 500 一个 pass，代码路径与今天逐字相同** |
| 4 | **能不能现在就打开** | ⛔ **不能**。打开前必须先做**两步白名单外的配置**（图层分流：`PC_Renderer.asset` 的 `m_TransparentLayerMask` 清位 + 把 233 个扭曲物挪到那一层）。不做 ⇒ **同一帧画两遍**（扭曲叠两遍） |
| 5 | **原记录那个「Built-in GrabPass / per-object 抓屏」** | ⛔ **确实是错的**（本轮再次证伪）。真机制 = **500 整帧拷一次 + 501 按【图层】重画**；**没有** GrabPass、**没有**自定义 LightMode |
| 6 | **自检** | `TMPDIR=/tmp/wf_b5 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**。⚠️ **这不等于「画对了」** —— 没跑 Unity，见 §五 |

---

## 一、判据（原版两次 event 的出处，逐条可复查）

全部在 `d:/2/tools/decomp_full/`（方法体反编译），类定义在 `d:/2/tools/il2cpp_out/dump.cs:92193-92340`。

### 1·1 两个 pass 都在 `Create()` 建、都在 `AddRenderPasses` 入队

| 依据文件 | 关键指令 |
|---|---|
| `GrabScreenFeatureRenderGraphAPI__Create.c:25-38` | 先建 `GrabPass`（`:28` `*(lVar3 + 0x10) = **500**;`），再建 `RenderPass`（`:36` 调 `RenderPass___ctor`） |
| `GrabScreenFeatureRenderGraphAPI__AddRenderPasses.c` | **两个都** `ScriptableRenderer.EnqueuePass`（`+0x28` = grabPass、`+0x30` = renderPass） |

### 1·2 501 那一步的设置（`GrabScreenFeatureRenderGraphAPI.RenderPass__.ctor.c`）

| 反编译原文 | 含义 |
|---|---|
| `:130` `*(undefined4 *)(param_1 + 0x10) = 0x1f5;` | **renderPassEvent = 501**（`0x1f5`） |
| `:64-122` 三次 `ShaderTagId___ctor` + `List.Add` | **三条** ShaderTagId |
| `:134` `RenderQueueRange__get_all()` | `RenderQueueRange.**all**`（0..5000） |
| `:147` `FilteringSettings___ctor(&local_38, &local_b8, uVar4, 0xffffffff, 0, 0)` | layerMask = **`Settings.LayerMask`**（`param_2 + 0x18`，见 `:139-140`）· renderingLayerMask = `0xffffffff` |
| `:156` `RenderStateBlock___ctor(&local_a8, 4)` | mask = **`RenderStateMask.Depth`**（= 4） |
| `:158` `DepthState___ctor(local_res20, 0, 4)` | `writeEnabled = **false**`、`compareFunction = **4 = LessEqual**` |
| `…RenderPass__RecordRenderGraph.c:172` `local_1e8 = 0x17;` | **SortingCriteria = 23 = `CommonTransparent`** |

### 1·3 那三条 ShaderTagId 的**名字**（这次解出来了，判据级）

反编译里是三个 il2cpp 字符串字面量槽 `DAT_1842d2738` / `DAT_184260608` / `DAT_1842aceb8`。
**解法**：槽地址 − ImageBase(`0x180000000`) = RVA，RVA 就是 `d:/2/tools/all_strings.txt` 的**行号键**
（那张表是「元数据槽地址 → 字符串」）。结果：

| 槽 | RVA | 字符串 |
|---|---|---|
| `DAT_1842d2738` | `0x42d2738` | **`SRPDefaultUnlit`** |
| `DAT_184260608` | `0x4260608` | **`UniversalForward`** |
| `DAT_1842aceb8` | `0x42aceb8` | **`LightweightForward`** |

自证：同一个 `Settings.__ctor` 用的 `DAT_1842d38e8` → `0x42d38e8` → **`_GrabPassTransparent`**，
正是 `Settings.TextureName` ⇒ 这套解法在同一个方法里同时对上 4 条。

> 📌 这也解释了为什么「换自定义 LightMode」是错的：原版就是用**现成的 forward 三件套**，
> 靠 **LayerMask** 把重画范围缩到「抓屏那一层」，不是靠新标签。

### 1·4 501 那一步的执行（`…RenderPass__RecordRenderGraph.c`）

`Get activeDepthTexture` → `CreateDrawingSettings(三条 tag, …, 0x17)` →
`RendererListDesc(cullResults, drawSettings, FilteringSettings)`（带 `stateBlock`）→
`CreateRendererList` → `AddRasterRenderPass` → 颜色挂 `activeColorTexture`、深度挂 `activeDepthTexture` →
`UseRendererList` → `DrawRendererList`。

### 1·5 `Settings` 的字段（`dump.cs:92218-92229`）

`public string TextureName;`（`0x10`）· `public LayerMask LayerMask;`（`0x18`）·
`[Range(0,1)] public float textureSizeMultiplier;`（`0x1C`，ctor 里写 `0x3e800000` = **0.25f**）。

**⇒ 为什么「必须按层分流」**：501 只是把「forward 标签 + 那一层」的物体**再画一遍**；
它不会把物体从 URP 默认透明 pass 里拿走。若 `LayerMask` 是全开，501 会把**整屏透明物**重画一遍
（显然不是原版）。原版靠的是 `UniversalRenderer.cs:430` 把 `data.transparentLayerMask` 传进透明 pass
⇒ 抓屏那一层**本来就不在**默认透明 pass 的绘制集里。

---

## 二、改动清单

**唯一改动的文件**：`D:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs`
（`git diff --numstat` = **+209 / −16**，行尾仍是 LF：`CRLF 0 / LF ~305`，与 HEAD 一致）。

| 位置 | 改了什么 |
|---|---|
| 文件头 `:32-65` | 把旧「❌ 缺的那一步 + 两步改法（① 换自定义 LightMode ② 加 pass 画它）」**就地订正**为「✅ 已落地」，并写清判据出处（§一 的全部文件/行 + 三条 tag 的解码法）；旧方案标为**猜测、已作废**（保留痕迹） |
| `:66` | `using System.Collections.Generic;`（`List<ShaderTagId>`） |
| `:87-96` | 新增两个序列化字段：`redrawAt501`（默认 `false`）、`redrawLayerMask`（默认 `0`） |
| `:101-108` `Create()` | 额外建 `_redraw = new GrabRedrawPass()`；**`_pass` 那两行没动** |
| `:110-148` `AddRenderPasses` | **第一行仍是** `renderer.EnqueuePass(_pass);`（500 原样）；之后才判开关/层掩码决定是否 Enqueue 501 |
| `:127-162` | 开关解析（`WF_GRAB_501` 优先 → 序列化 bool）· 层掩码（`WF_GRAB_501_LAYER` 覆盖）· `WarnOnce`（**不许静默失败**：开了但掩码=0 会告警一次） |
| `:184-281` 新类 `GrabRedrawPass` | 501 pass 本体（下表逐项对应 §一） |
| `:285-307` 文件尾 §开关与前置 | 开启前置、顺序、批处理安全性 |

`GrabRedrawPass` 与原版的逐项对照：

| 原版 | 我们的实现 |
|---|---|
| `renderPassEvent = 0x1f5` | `public const RenderPassEvent Event501 = (RenderPassEvent)501;`（枚举里没有这个名字，必须强转） |
| 三条 `ShaderTagId` | `static readonly List<ShaderTagId> TagIds = { SRPDefaultUnlit, UniversalForward, LightweightForward }` |
| `RenderQueueRange.all()` + `Settings.LayerMask` | `new FilteringSettings(RenderQueueRange.all, layerMask)` |
| `RenderStateBlock(mask=Depth) + DepthState(false, LEqual)` | `new RenderStateBlock(RenderStateMask.Depth){ depthState = new DepthState(false, CompareFunction.LessEqual) }` |
| `SortingCriteria 0x17` | `SortingCriteria.CommonTransparent` |
| `CreateDrawingSettings(3 tags)` | `RenderingUtils.CreateDrawingSettings(TagIds, renderingData, cameraData, lightData, sort)` |
| `CreateRendererList(desc with stateBlock)` | **URP 的 `RenderingUtils.CreateRendererListWithRenderStateBlock` 是 `internal`（`RenderingUtils.cs:404`）本程序集调不到** ⇒ 照它的实现自己拼 `RendererListParams`（`tagValues = {ShaderTagId.none}`、`stateBlocks = {state}`、`isPassTagName = false`）再 `graph.CreateRendererList` |
| 挂 color `activeColorTexture` / depth `activeDepthTexture` | `SetRenderAttachment(color, 0, Write)` + `SetRenderAttachmentDepth(depth, **Read**)`（ZWrite off ⇒ 只读） |
| `DrawRendererList` | `ctx.cmd.DrawRendererList(d.List)` |
| —（RenderGraph 特有） | `builder.UseAllGlobalTextures(true)` —— **重画时必须能读上一步挂上来的 `_GrabPassTransparent`**；漏了它 RG 可能不给绑全局纹理 |

---

## 三、开关怎么关（三种方式，任一即可）

```csharp
// 1) 环境变量（A/B 用，优先于序列化字段）
WF_GRAB_501=0        // 强制关（也接受 false）
WF_GRAB_501=1        // 强制开

// 2) 序列化（正常玩法用）—— PC_Renderer.asset 里 GrabPassTransparent 那个子资产
redrawAt501: 0
redrawLayerMask: 0

// 3) 重画哪一层（只影响「开」的那一支）
WF_GRAB_501_LAYER=8      // 层号 0..31，内部按 1<<n
WF_GRAB_501_LAYER=256    // >31 时直接当位掩码用
```

**关掉后的行为与今天逐字相同**：`AddRenderPasses` 在 `!RedrawEnabled()` 时**直接 return**，
只 Enqueue 500 那一个 pass（`_pass` 的构造、`requiresIntermediateTexture`、`passEvent` 都没改）。
两条保险再叠一层：`redrawLayerMask == 0` 时也不 Enqueue；`GrabRedrawPass.RecordRenderGraph` 第一行也是 `if (layerMask == 0) return;`。

**批处理 / 无帧循环**：本 Feature 用的是标准 `ScriptableRenderPass`，**没有挂 `RenderPipelineManager` 回调**
⇒ 没有帧循环时它只是不被调用，不会崩、也不会留下全局状态。501 打开后也只在相机真渲染时执行。

---

## 四、A/B 怎么跑 + 判绿红

> ⚠️ **先说清楚：这个 A/B 现在跑不了** —— 差两步**白名单外**的配置（§五-2）。下面把前置、命令、判据一次写全，
> 主对话做完那两步就能直接跑。

### 4·1 前置（各一处，**都不在 B5 白名单**）

1. **关掉透明 pass 对那一层的绘制**（`Assets/Settings/PC_Renderer.asset`，必要时 `Mobile_Renderer.asset`）
   ```yaml
   m_TransparentLayerMask:
     serializedVersion: 2
     m_Bits: 4294967295          # ← 改成 4294967295 & ~(1<<N)，N = 你挑的层
   ```
   （`m_OpaqueLayerMask` / `m_PrepasseLayerMask` **不用动**。相机 culling mask 要**包含** N —— 默认就是全含。）
2. **把 233 个扭曲物放到层 N**：`Everguild/FX/Particle Distortion Affect Transparents`（现读 233 个 prefab 引用它）。
   - 正式玩法：在 prefab 数据里的粒子 System GameObject 上设 `.layer = N`（`WarpforgeVFX/Prefabs/` 是**生成物**，
     手改会被下次导出覆盖 ⇒ 正确落点是导出/导入那一段，或运行时实例化后统一设层）。
   - **只为 A/B**：在 `EffectSweepBatch` 里给两侧生成出来的实例统一设层（见下面 4·2 的补丁）。

### 4·2 A/B 补丁（`Assets/WarpforgeArena1/Editor/EffectSweepBatch.cs`，白名单外，主对话自己做）

扫描的 `orig` 侧用的是 bundle 里的原版 prefab、`exp` 侧用我们导出的 prefab —— **两侧图层不同**。
所以 A/B 必须在两侧都设层，否则比的是两件不同的事：

```csharp
// 放在实例化之后、渲染之前（两侧都要走）
static void SetGrabLayerRec(GameObject go, int layer)
{
    go.layer = layer;
    foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
}
// 用法：SetGrabLayerRec(instance, 8);   // 8 = 你挑的层 N
```

### 4·3 命令（照 `工具/run_grab_ab.sh` 的骨架，含备份/还原 —— 全量 sweep 会**覆盖**基线三件套）

```bash
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"; PROJ="D:\4\Unity\MyGame"
BASE="d:/4/Unity/资料/比对基线"; TMP="d:/4/_tmp_view"; unset ELECTRON_RUN_AS_NODE

# 0) 备份（run_grab_ab.sh 已有这一步，别省）
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$BASE/$f" "$TMP/bak_$f"; done

# 1) 组 A = 501 关（= 现状基线）
export WF_GRAB_501=0
"$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod EffectSweepBatch.Run -logFile "$TMP/sweep_A.log"
cp "$BASE/sweep_orig.tsv" "$TMP/ab_A_orig.tsv"; cp "$BASE/sweep_exp.tsv" "$TMP/ab_A_exp.tsv"

# 2) 组 B = 501 开（层掩码走环境变量，免改资产）
export WF_GRAB_501=1 WF_GRAB_501_LAYER=8
"$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod EffectSweepBatch.Run -logFile "$TMP/sweep_B.log"
cp "$BASE/sweep_orig.tsv" "$TMP/ab_B_orig.tsv"; cp "$BASE/sweep_exp.tsv" "$TMP/ab_B_exp.tsv"

# 3) 还原基线 + 比较
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$TMP/bak_$f" "$BASE/$f"; done
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/compare_sweep.py \
    "$TMP/ab_A_exp.tsv" "$TMP/ab_B_exp.tsv" --orig "$TMP/ab_A_orig.tsv"
```

- 想**一次进程两阶段**（消跨进程漂移，推荐）：加 `WFSWEEP_SIDE=both`（同一次 Run 里先 orig 再 exp）。
- **底图尺子必须开**（`WFSWEEP_BACKDROP` 默认就是开）—— 抓屏族在纯色背景上是**零像素变化**，那是旧尺子的假象。
- 每时刻「渲两帧读第二帧」的 workaround 两组都要保持一致（它就是现在 sweep 的固定口径）。
- 233 条目标清单：`d:/4/_tmp_view/grab_effects.txt`（我生成的，**故意没放成** `sweep_targets.txt`，见 §六-2）。

### 4·4 判绿红（沿用本仓既有尺子，别自创）

- **指标**：两侧 `lit ≥ 60` 的帧上 `sum` 比值 → `|ln(比值)|` 的**中位/均值** + **0.7–1.4 带内比例**
  （`工具/analyze_sweep.py` 的同一口径；⛔ 不要用「Z 计数」判，边界挪 0.05 就能摆 22–54 条）。
- **判绿**（B 比 A 更贴原版）：233 条抓屏族里
  ① `|ln|` **中位不升**、**带内比例不降**；② 逐条**没有**从 `Z`(|ln|≤0.336) 翻成 `E`(|ln|>0.336) 的**新**条目；
  ③ 不出现新的 `D`（只有导出有）/`C`（只有原版有）。
- **判红**：① 中位上升 > 0.05；或 ② 任一条从 Z 翻 E；或 ③ 出现成片「两边都空」变「一边有一边无」。
- **噪声底（别误判）**：同一实例连跑两次，`|ln|` 中位/90 分位都是 0.0000、最大 ~0.41，**E 组计数每次 ±1**；
  **`|ln| ≲ 0.6` 的差异在这把尺子上分不出真假**（`特效还原_进度与交接.md` §14.4⑬ + `analyze_sweep.py` 自带提示）。
- **先看一组「负例」**：501 开但**层分流没做**（掩码=0）时应当与 A **逐位相同**（只多一条 warning）
  —— 这条能证明「开关关得干净」。

---

## 五、没查清 / 没做

1. **原版 `Settings.LayerMask` 取的是哪一层：查不到。** 原版的 `UniversalRendererData` 资产本地**没解出来**：
   `assets_full` 里没有任何带 `m_TransparentLayerMask` 的对象；84 个 bundle 里也**没有**这段 ASCII（`grep -a` 实跑）。
   ⇒ 层号由我们自己挑（推荐 8，Ubuntu 默认未命名层；本工程 `TagManager.asset` 的 6..31 全未命名）。
2. **「原版靠层分流」这条是【推断】，不是实证**（文件头也这么标）。推理链：原版 `Settings` 有 `LayerMask` 字段
   且**只**用于 501 的 `FilteringSettings`；若它是全开，501 会把所有 forward 标签的透明物重画一遍（显然不是原版）。
   **要实证**：拿到原版 renderer 资产（远端包）或跑原版实况看那一层。
3. **没跑 Unity** ⇒ **不声称「编过了/跑过了/画对了」**。唯一跑过的是 `工具/typecheck.sh`（运行时 0 / 编辑器 0，
   只证明**编得过**，不证明 RenderGraph 里那条 `RendererListParams` 在运行时行为正确）。
   第一件该验的事 = **501 开但层掩码=0** 时画面逐位不变（§4·4 的负例）+ 日志里没有 RG 报错。
4. **A/B 跑不了**：差 §4·1 的两步（`PC_Renderer.asset` + 233 个物体设层）与 §4·2 的 sweep 补丁 —— 都不在 B5 白名单。
5. **500 那一次与我们**还有两处**已存在**的差异（本轮**故意不动** —— 简报要求「500 一个字节别动」，另开账）：
   - 原版 RT 尺寸 = `cameraTargetDescriptor × **textureSizeMultiplier(0.25)**`（`GrabPass___OnCameraSetup.c`），
     我们是**全分辨率**拷贝（采样更清、不是错，但与原版不同）。
   - 原版用 `RenderingUtils.ReAllocateIfNeeded` + `ConfigureClear(None)` + `OnCameraCleanup` 释放；
     我们用 RenderGraph `CreateTexture`（RG 管生命周期）。
6. **没有新增 Editor 探针**：A/B 用现成的 `EffectSweepBatch` 就够；再写一个探针只会多一份没跑过的代码。

---

## 六、顺手发现（都不在 B5 白名单 / 或属于订正建议，**我没动**）

1. 🔴 **简报说「`WarpforgeVFX/Editor/` 在 gitignore」—— 不对**：`.gitignore:29` 是
   `!/Unity/MyGame/Assets/WarpforgeVFX/Editor/`（**反向忽略 = 进仓库**）。该目录现有
   `ArenaOriginalShaderRebindProbe.cs`、`WarpforgeUiFxProbe.cs` 两件，都是**跟踪中**的。
2. 🔴 **`_tmp_view/sweep_targets.txt` 原来是【故意删掉】的**（台账原文：「免得将来全量 sweep 只扫 103 条」）。
   ⇒ 我把 233 条清单放在 **`d:/4/_tmp_view/grab_effects.txt`**，**没有**直接写成 `sweep_targets.txt`。
   跑小批前再 `cp` 成 `sweep_targets.txt`，**跑完删掉**（否则下一次全量 sweep 只会扫 233 条，且**不报错**）。
3. 🔴 **B14 说的「41 条抓屏扭曲效果」与现读不符**：现读 = **233 个效果 / 233 个 prefab**
   （判据：`grep -rl "Particle Distortion Affect Transparents" Assets/WarpforgeVFX/Prefabs | wc -l` = 233；
   用 `效果库报告.tsv` 的 `prefab` 列反查 = 233 个效果名）。清单已落 `_tmp_view/grab_effects.txt`。
   台账里那个 41 要么是旧效果库口径、要么是「扫描里量得出来」的条数 —— 主对话可顺手订正。
4. 🔴 **旧方案①（改 `WFDistortion.shader` 换 LightMode）在正常态【完全无效】**：2026-10-22 的 M6 已把
   `Everguild/FX/Particle Distortion Affect Transparents` 放进 `WarpforgeShaderMap.UseOriginal`
   （`WarpforgeShaderMap.cs:245`）⇒ 运行时材质走**bundle 原件**，我们那份 shader 只在「原件包不在场」的兜底路生效。
   ⇒ 这也说明「换 shader 打标」这条路**顺带**是错的（不只是不必要）。
5. ⚠️ **B14 那一行台账仍写着「原版 Built-in GrabPass 是每个物体画到那一刻抓…」** —— 与 2026-10-22 的文件头订正、
   以及本轮的反编译判据**互相打架**（`普查产出_1022/W64_兜底审计_特效.md:38`）。台账是正本，**我没改**，请主对话订正。
6. 📌 本工程 `ProjectSettings/TagManager.asset` 里 **6..31 全未命名**（只有 0..5 有名字）；原版那些 prefab 带
   `m_Layer: 6 / 5 / 1 / 10 / 20` 是**原版数据**带进来的，不是我们设的 —— 挑「抓屏层」时**别挑 0/5/6/10/20**
   （会误伤现有效果的相机归属；`WarpforgeEffectHook.cs:36-53` 那套「实例跟随父节点层」的注释里有解释）。
