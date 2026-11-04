# B140_WJ · Mobile 档全局后处理（G6）

- 日期：2026-11-04 · 写手：`B140_WJ` · 上游：`审计-B136-光照后处理` G6（`判据/B140_对账_G6_审计指针.md:157`）
- 结论：**🟠 → ✅ 已修**（`m_VolumeProfile` 置 `{fileID: 0}`）· typecheck **0 / 0** · 断言 **4 组 26 条**（+2 条失败分支兜底）

## 一、键名订正（先纠一个会让人改错文件的口径）

任务简报与 `B136` 报告把这一条写成 **`postProcessData`** —— **现读确认是笔误**：

| 键 | 落在哪 | 现读值 | 是不是差异点 |
|---|---|---|---|
| `postProcessData` | **Renderer** 资产（`Mobile_Renderer.asset:31`、`PC_Renderer.asset:50`） | 两档都是 `{fileID: 11400000, guid: 41439944d30ece34e96484bdb6645b55}` = **URP 包自带 `PostProcessData.asset`**（`Library/PackageCache/…/Runtime/Data/PostProcessData.asset.meta`） | ❌ 不是。这是**后处理 shader 资源包**，置 0 会让整条后处理链拿不到 shader。原版主 Renderer 同样非 0（`…_Renderer.asset:31`） |
| **`m_VolumeProfile`** | **UniversalRenderPipelineAsset**（RP 资产） | Mobile 改前 = `SampleSceneProfile`（Bloom 0.25/1.0/0.5 + Vignette 0.2） | ✅ **是**。这才是「全局后处理面板」 |

⇒ 本次改的是 `m_VolumeProfile`（与 `B136` 报告 `:203` 的「改法」一致，行号 `:94/:95` 也对得上 —— 只是字段名写错了）。

## 二、改前 / 改后

| | 文件:行号 | 值 |
|---|---|---|
| 原版判据 | `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/UniversalRenderPipelineAsset Medium.asset:95` | `m_VolumeProfile: {fileID: 0}` |
| 原版旁证 | 同目录 `High.asset:95` · `Low.asset:95` · `Ultra.asset:95` | **4/4 全部 `{fileID: 0}`** |
| 我们改前 | `Assets/Settings/Mobile_RPAsset.asset:94` | `{fileID: 11400000, guid: 10fc4df2da32a41aaa32d77bc913491c, type: 2}` = `SampleSceneProfile.asset` |
| **我们改后** | `Assets/Settings/Mobile_RPAsset.asset:94` | **`{fileID: 0}`** |
| 同工程对照 | `Assets/Settings/PC_RPAsset.asset:95` | `{fileID: 0}`（本来就对） |

- `git diff --numstat` = `1 / 1`（**只动这一行**）· 行尾：改前 LF 135 行 / 4345 B → 改后 LF 135 行 / 4289 B，**bareCR 0、CRLF 0（未翻）**。
- 为什么用 `{fileID: 0}` 而不用别的写法：`m_VolumeProfile` 是 `VolumeProfile` 的 PPtr，**空引用在原版 4 份 RPAsset 里就是这个写法**（不是「关掉某组件」，没有第二种规范表达）。置 0 后全局 volume 由 `UniversalRenderPipelineGlobalSettings.asset:363` 的 `URPDefaultVolumeProfileSettings.m_VolumeProfile → DefaultVolumeProfile.asset`（guid `ab09877e…`，19 个全中性组件）兜住 ⇒ **与原版同形**，不是「把后处理链拆了」。

## 三、断言（`Assets/CardPresentation/Editor/B140_G6_Assert.cs` · 全局命名空间 · 类名==文件名 · `.meta` 已建）

```
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod B140_G6_Assert.Run -logFile -
```
判绿红：按**行首** `B140G6 ✓` / `B140G6 ✗` 数。

| 组 | 条 | 内容 |
|---|---|---|
| **A 前提（硬断言）** | A1–A6 | 文件存在 · 行数 >100 · `m_Name: Mobile_RPAsset` 恰 1 处 · `k_AssetVersion:` 恰 1 处 · `m_VolumeProfile:` 恰 1 处 · **能解析出 fileID**。⛔ 没有 `if (前提) Check(结论)` 写法，前提不成立就报红 |
| **B 值** | B1–B6 | fileID == **0**（原版 4/4）· 全文件不含 SampleSceneProfile 的 guid `10fc4df2…` · PC 档同键可解析且两档一致 |
| **C 🧨 灭自证** | C1a–C4c | C1 同一解析器读本文件**已知非 0** 的 `blueNoise64LTex` → **2800000**；C2 同一解析器读**合成的改前行** → **11400000**；**C3 用同一条判定函数 `IsOriginalVolumeProfile` 去判 11400000 ⇒ 必须返回 false**（判成恒真这里必红）；C3b 对 0 必须返回 true（正向不恒假）；C4 同一解析器读**另一份真资产** `PC_Renderer.asset` 的 `postProcessData` → **11400000（非 0）** |
| **D 结构** | D1–D4 | 置 0 后全局 `DefaultVolumeProfile` 仍接着（guid `ab09877e…` 命中）· `m_ObsoleteDefaultVolumeProfile` == 0 |

## 四、typecheck（`TMPDIR=/tmp/wf_b140wj bash d:/4/Unity/工具/typecheck.sh`，全文仅 4 行）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

## 五、账：**不写账补丁**（并说明为什么）

这是**资源/资产参数**（`.asset` YAML 的字段值），**没有「原版类/方法」**可指 —— 账的三源分档（`A/B/C/D`）判的是「原版某个类的某个方法在不在我们的实现里」，套不到这里。硬凑一条只会得到一个指向 `Mobile_RPAsset.asset` 的假 `A` 行。⇒ **不写 `账/_parts/` 补丁**，落点证据只有本报告 + 断言文件。

## 六、顺手发现（⛔ 未改，均在我的文件白名单之外，交调度台）

1. 🔴 **原版 Medium 档的 renderer 也把 `postProcessData` 置 0**：原版 `Medium.asset:20` 的 `m_RendererDataList[0]` = guid `11e1fde537ef8cf4e8507ab4d39f3be2`，落到 **`…_Renderer LOW.asset`**（`:30  postProcessData: {fileID: 0}`）。我们 `Mobile_RPAsset.asset:20` 挂的是 `Mobile_Renderer.asset`，其 `postProcessData` 非 0 ⇒ **原版 Medium 等于后处理链整体不跑，我们只做到「没有 bloom/暗角」**。⚠️ 要改得动 `Mobile_Renderer.asset`（**不在我白名单**），且要先判「原版是不是故意关掉整条链（含 tonemap）」。
2. 🔴 **原版 6 个画质档 `customRenderPipeline` 全是 `{fileID: 0}`**（`ProjectSettings/QualitySettings.asset:51/104/157/210/263/316`）⇒ 原版**永远用 `GraphicsSettings` 的默认管线 `High.asset`**（`:73` → guid `6ab4f96e…`），**切画质档根本不换管线**。我们 `QualitySettings.asset:51/:105` 给 Mobile/PC **各挂一份管线** ⇒「Mobile 档」这个差异是我们自己造的。G6 修完仍不等于「原版切档行为」，这条要调度台定口径。
3. ✅ **`m_VolumeFrameworkUpdateMode`**：原版 `Medium.asset:94` = **1**、我们 PC 档 `PC_RPAsset.asset:94` = **1**，**我们 Mobile 档 `Mobile_RPAsset.asset:93` 改前 = 0**。⇒ **2026-11-04 调度台追加为第 1 件，已改（见 §七·7.1）**，此条不再是待办。

---

## 七、追加三件（调度台 2026-11-04 追加）

### 7.1 第 1 件 · `m_VolumeFrameworkUpdateMode` —— ✅ 已改（0 → 1）

| | 文件:行号 | 值 |
|---|---|---|
| 原版判据 | `d:/2/tools/assetripper_out/…/UniversalRenderPipelineAsset Medium.asset:94` | `1`（`VolumeFrameworkUpdateMode.ViaScripting`） |
| 原版旁证 | 同目录 `High.asset:94` · `Low.asset:94` · `Ultra.asset:94` | **4/4 全是 `1`** |
| 我们改前 | `Assets/Settings/Mobile_RPAsset.asset:93` | `0`（`EveryFrame`，URP 新建资产的默认值） |
| **我们改后** | 同上 | **`1`** |
| 同工程对照 | `PC_RPAsset.asset:94` | `1`（本来就对） |

- 累计 `git diff --numstat` = **2 / 2**（本单两行）· 行尾 LF 135 行 / **bareCR 0**（未翻）。
- 断言（补进 `B140_G6_Assert`）：**A7 / A8** 前提（键恰 1 处 + 能解析整数）· **B7 / B8** 值 == 1 · **C5a–C5c** 灭自证（同一标量解析器读本文件**已知为 0** 的 `m_RequireDepthTexture`，判定函数必须返 false）· **C5d / C5e** 用合成的改前行 `m_VolumeFrameworkUpdateMode: 0` 再证一次（有人改回 0 ⇒ B8 与 C5e 一起红）。

### 7.2 第 2 件 · 原版 LOW renderer vs 我们 Mobile_Renderer —— ⛔ **不是同一份，不改**

**判别结论：我们 `Mobile_Renderer` ≈ 原版【HIGH】去掉 SSAO，不是原版【LOW】。** 决定性判别器三项全不一致：

| 字段 | 原版 `…_Renderer LOW.asset` | 原版 `…_Renderer.asset`（HIGH 用） | 我们 `Mobile_Renderer.asset` | 我们 `PC_Renderer.asset` |
|---|---|---|---|---|
| `m_RendererFeatures` | **[]（0 个）** | 1 个 = **`GrabScreenFeatureU6`**（抓屏，`TextureName: _GrabPassTransparent`，`LayerMask: 64` = 层 6） | 1 个 = `GrabPassTransparent`（**我们复刻的同一功能**） | 2 个 = SSAO（`m_Active: 0`）+ GrabPass |
| `postProcessData` | **`{fileID: 0}`** | `7decaff8…`（非 0） | `41439944…`（URP 包默认，非 0） | 同左 |
| `m_PrepassLayerMask` / `m_OpaqueLayerMask` / `m_TransparentLayerMask` | `1207963447` | `1207963447` | **`4294967295`（全 32 层）** | **`4294967295`** |
| `m_AssetVersion` | 3 | 3 | 3 | 3 |
| `m_UseNativeRenderPass` · `m_ShadowTransparentReceive` · `m_RenderingMode` · `m_DepthPrimingMode` · `m_CopyDepthMode` · `m_DepthAttachmentFormat` · `m_DepthTextureFormat` · `m_AccurateGbufferNormals` · `m_IntermediateTextureMode` | 全 0 | 全 0 | 全 0 | 全 0 |
| `m_DefaultStencilState` | 全同 | 全同 | 全同 | 全同 |
| `xrSystemData` | `{fileID: 0}` | `{fileID: 0}` | **字段不存在**（URP 17 已移除） | 同左 |
| `debugShaders` | 2 项 | 2 项 | 3 项（多 `probeVolumeSamplingDebugComputeShader`） | 3 项 |
| `probeVolumeResources` | 全 0 | 全 0 | 全 0 | 全非 0（未启用） |

**为什么不改（三条）：**

1. **形态对不上**：原版 LOW 是「**没有抓屏 feature + 没有后处理 shader**」的廉价档；我们的 Mobile 档**必须**留着 GrabPass（它是原版 `GrabScreenFeatureU6` 的复刻，扭曲特效要用）⇒ 我们这份天生更接近 HIGH。
2. **原版运行时根本不读 Medium**：`ProjectSettings/QualitySettings.asset` 六档 `customRenderPipeline` 全 `{fileID: 0}` ⇒ 一律落到 `GraphicsSettings.asset:73` 的 **`High.asset`**（其 renderer 的 `postProcessData` **非 0**）。⇒ 「原版 Medium 的 renderer = 0」是一条**从没在运行时生效过的休眠配置**；照它改 = 给一个原版都没跑过的组合造行为。
3. **代价大且不可逆**：URP 17 `Runtime/UniversalRenderer.cs:456` 明写 `//No postProcessData means that post processes are disabled` ⇒ 置 0 = **整条后处理链（含 tonemap / color grading）停摆**，不是「少个 bloom」。本单没有 Unity 复验窗口，做不了视觉确认。

- ⚠️ 补充确认（排除「改了也白改」）：`postProcessData` **没有 `[Reload]` 属性**（`Runtime/UniversalRendererData.cs:134`），`OnEnable → ReloadAllNullProperties` 只重灌带 `[Reload]` 的资源 ⇒ 置 0 **不会**被编辑器偷偷改回来。

**建议（交调度台）**：若目标是「原版 Medium 资产数据」逐字一致，正确的一单是 **把 `Mobile_Renderer` 换成 LOW 等价件（清空 features + `postProcessData` 0 + 掩码 `1207963447`）并决定 GrabPass 怎么办** —— 那会**丢掉移动档的抓屏特效**，是设计决定，需用户/调度台拍板 + Unity 复验。**本单不改。**

**顺带发现（同域、两份 renderer 都有，⛔ 未改）**：三个 layerMask 我们两份都是 `4294967295`（全 32 层），原版两份都是 **`1207963447`** = 位 **{0,1,2,4,5,8,9,10,11,27,30}**。这些位落在我们 `TagManager.asset` 的层名上恰好是 `Default / TransparentFX / Ignore Raycast / Water / UI / CustomPostEffectIgnore / 2DElements / BoardElements / BoardCardLimit / Blocking / GroundCollider` —— **层名是照原版复刻的，掩码却放宽到全层** ⇒ 我们多渲染 **层 6 `AfterTransparents`** 与 **层 12 `ParticleTapCollider`**（原版这两个都不进主渲染）。建议单开一单（影响 PC 与 Mobile 两份 renderer，需 Unity 复验）。

### 7.3 第 3 件 · 画质档 `customRenderPipeline` —— ❓ 只调查，⛔ 未改

| | 原版 | 我们 |
|---|---|---|
| 画质档数 | **6**：Very Low · Low · Medium · High · Very High · Ultra | **2**：Mobile(0) · PC(1) |
| 每档 `customRenderPipeline` | **6/6 全 `{fileID: 0}`**（`QualitySettings.asset:51/104/157/210/263/316`） | Mobile → `Mobile_RPAsset`（`:51`）· PC → `PC_RPAsset`（`:105`） |
| 默认管线 | `GraphicsSettings.asset:73` = **`High.asset`**（guid `6ab4f96e…`） | `GraphicsSettings.asset:91` = **`PC_RPAsset`**（guid `4b83569d…`） |
| `m_CurrentQuality` | 0（Very Low） | 1（PC） |
| 每平台默认档 | Android 2 · iPhone 2 · Standalone 5(Ultra) · WebGL 3 | Android 0(Mobile) · iPhone 0 · Standalone 1(PC) · WebGL 0 |
| 档间差异靠什么 | **全靠 QualitySettings 档位字段**（pixelLightCount / shadows / shadowResolution / shadowDistance / lodBias / antiAliasing / skinWeights / softParticles） | 除档位字段外，**还靠换管线** |

**若把两档都改 `{fileID: 0}`** ⇒ 落到 `GraphicsSettings` 默认 = **`PC_RPAsset`**：

- 移动档会拿到 PC 管线：`m_RenderScale` **0.8 → 1**（像素数 ×1.56）· `m_MainLightShadowmapResolution` **1024 → 2048** · `m_AdditionalLightsPerObjectLimit` **4 → 8** · `m_RequireDepthTexture` **0 → 1** · `m_EnableLODCrossFade` **1 → 0** · `m_SupportsTerrainHoles` **1 → 0** · `m_AdditionalLightsShadowmapResolution` **2048 → 512**。
- **`Mobile_RPAsset` 变成零引用孤儿** ⇒ 本单 G6 的两处修复（`m_VolumeProfile` / `m_VolumeFrameworkUpdateMode`）**再也不会被读到**。
- 与「Mobile 档」现有差异的关系：**G6 那条差异就此消失，但不是因为对齐了原版，而是因为整份资产被旁路。**

**结论**：我们的 **`PC_RPAsset` 数值上很接近原版 `High.asset`**（HDR 1 / MSAA 1 / RenderScale 1 / 主光阴影图 2048 / 附加光阴影 512 / shadowDistance **15** / soft shadows 1 / SRP batcher 1 —— 逐项相同），所以「两档全 0」在**行为上其实能复刻原版「切档不换管线」**；差的不是这一步，而是 **档数（6 vs 2）与档位字段**。

**建议（⛔ 本单不做，需 Unity / 真机复验）**：把这条当**一整单**做 —— ① 重建原版 6 档并照抄档位字段；② 6 档 `customRenderPipeline` 全置 `{fileID: 0}`；③ 保留 `PC_RPAsset` 作默认管线；④ 处理 `Mobile_RPAsset` 的去留（若删，G6 两条修复随之作废，须在账里写清）。**只填两个 0 会让移动档吃 PC 的渲染开销，却不解决档位差异。**

### 7.4 追加后的验收状态

- 第 1 件：✅ 已改 + 已加断言（A7/A8 · B7/B8 · C5a–C5e）。
- 第 2 件：⛔ 按判据**不改**（不是原版 LOW）；已给逐字段表 + 建议 + 一条新发现的 layerMask 差异。
- 第 3 件：⛔ **不改**（渲染路径整体变化）；已给「原版 vs 我们」表 + 若改的落点 + 建议。
- typecheck：**0 / 0**（最终一次 `TMPDIR=/tmp/wf_b140wj bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 0 · 编辑器 0）。
  ⚠️ 中途两次读到红，**都不是本单文件**：① `RuleEngine/Editor/B140_WA_Assert.cs`（4 条 `CS1061`，该文件 mtime 13:41:56 正在被别的代理写）② `CardPresentation/Battle/BattleDriver.cs`（3 条 `CS0103`，mtime 13:43:38 同上）。等对方停手后复跑即 **0 / 0** —— 按 §12 的判据，错误全集中在非本单文件时不算本单的错。
