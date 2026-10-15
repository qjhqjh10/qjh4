# AssetRipper 能力核实（重点：材质）（2026-10-16 · 只读普查）

> 判据分级：**【源】** = 我亲手读的开源仓文件（`github.com/AssetRipper/AssetRipper`，`master`，`pushed_at 2026-10-10`）；
> **【issue】** = GitHub issue 原文/维护者原话；**【本地】** = `d:/2` 我只读扫出来的数；**【中文】** = CSDN/gitcode 页面。
> 源码取法：`api.github.com/.../contents/<path>` 取 base64（`raw.githubusercontent.com` 当机时可用）。
> 本地快照：`d:/2/tools/toolsurvey3/r3/`（up* 目录 = 我下载的源文件）。
> ⚠️ **2026-10-16 订正**：`d:/2/tools/toolsurvey3/AssetRipper` 那份 `git clone --depth 1` **最终失败了**（不是「慢」）——
> `error: RPC failed; curl 56 Recv failure: Connection was reset / fatal: early EOF / fetch-pack: invalid index-pack output`，
> git 把半成品目录**自己清掉了**（所以现在 `d:/2/tools/toolsurvey3/` 下**没有** `AssetRipper/`）。
> ⇒ 本轮**所有源码判据都来自按文件取的 contents API 副本**（`r3/up*/`），**不依赖那份 clone**；这条也再次印证 §七/§八 的网络不稳。
> ⚠️ 另注：`d:/2/tools/toolsurvey3/` 根下的 `ae.json`/`am.cs`/`asc.cs`/`c2.json`… 等文件**不是我写的**，是**同期另一个子代理**在跑（时间戳 00:26–00:37 与本轮重叠）；我的产出一律在 `r3/` 子目录里，**两份没有互相覆盖**。

## 结论（5 句以内，直答「材质能不能提取、提到什么程度」）

1. **能提到「值完整、渲染不对」**：材质导出成真正的 `Assets/**/*.mat`（YAML `!u!21`）+ `.meta`，`m_SavedProperties`（`m_TexEnvs`/`m_Floats`/`m_Colors`）**照原资产字段原样写出、不是空壳**（【源】无材质专用导出器 ⇒ 走通用 YAML walker；【issue】#1136 贴的实际输出有值）。
2. **它的 `m_Shader` 指向占位 shader**，但占位 shader 会**从原 shader 的 `m_ParsedForm.m_PropInfo` 重建 `Properties{}` 块**（名字/类型/默认值都抄）⇒ **材质属性名能对上，值因此得以保留**；**shader 的行为**才是占位的（一行 `#pragma surface surf Lambert` 或 4 个写死的 HLSL 模板）。
3. **反编译 shader 是付费**，免费版 GUI 明写「免费版不可用」（【源】`ShaderExportModeDropDownSetting`：`Decompile => GameFileLoader.Premium ? … : Localization.NotAvailableInTheFreeEdition`）。
4. **【本地】我们这 234 份原版 shader 全部带 `m_ParsedForm`（0 份缺）**，其中 **184 份有属性表（均 14 条）**；没属性的 50 份**几乎全是 `Hidden/*`**（URP 内部/VFX 隐藏 Pass）⇒ 对「材质属性保留」**风险很小**。
5. **MonoBehaviour 不是「全 missing」**：`IL2CPPManager` **内置 Cpp2IL**，只要把 `GameAssembly.dll` + `global-metadata.dat` 一起喂进去 —— 而这**两个文件我们本机都有**（`d:/2/unity_run_ref/`）⇒ 这是本轮相对前两轮**最大的新信息**。

## 一、材质（Material）—— 逐项给判据 + 原文引文

**1.1 导出成什么文件、走哪条代码路**
- 【源】`Source/AssetRipper.Export.UnityProjects/ExportCollection.cs` → `GetExportExtension()`：
  `IShader => "shader", IMaterial => "mat", …` ⇒ **材质落 `.mat`**；同文件 `ExportAsset/GetUniqueFileName` 会给每个导出件配一个 **`.meta`**（`AssetExportCollection.CreateImporter` → `NativeFormatImporter`）。
- 【源】**全仓没有材质专用导出器**：我按 `(?i)(Material|…)` 过滤 1641 条路径，`Export.UnityProjects/` 下**没有 `MaterialExporter.cs`**，只有 `SourceGenerated.Extensions/MaterialExtensions.cs`（只读工具：`TryGetTextureProperty` / `GetTextureProperties`）。
- 【源】`ProjectExporter.Overrides.cs` 的兜底是第一行 `OverrideExporter<IUnityObjectBase>(new DefaultYamlExporter(), true);` ⇒ **材质把全部字段交给通用 YAML walker**（`YamlExporterBase.Export` → `ProjectYamlWalker.ExportYamlDocument` → `asset.WalkEditor(this)`）。
  （唯一例外：`OverrideExporter<IMaterial>(fontAssetExporter)` —— 字体材质随字体走，见 `ProjectExporter.Overrides.cs`。）

**1.2 `m_SavedProperties` 是空还是有值？值从哪来？**
- **有值，且是「原资产字段直写」，不是从 shader 属性表推出来的**。
- 【issue】`#1136 [Bug]: Material properties data are exported incorrectly`（2023-12-19 closed）报的是**格式**问题，正文贴的实际导出片段本身就证明有值：
  ```
  m_SavedProperties:
    serializedVersion: 2
    m_TexEnvs:
    - first: { name: _MainTex }
      second: …
    m_Floats: {}
    m_Colors:
    - first: { name: _Color }
      second: …
  ```
  （ds5678 回「Duplicate of #1045」；#1045 标题 `AssetDictionary<FastPropertyName, UnityTexEnv> Material.TexEnvs has bad Yaml formatting on Unity 4`，2023-10-12 closed。）⇒ **值来自原材质资产**（`material.SavedProperties_C21`，见 `MaterialExtensions.cs`），walker 只是把它序列化出来。

**1.3 `m_Shader` 指向什么？（这是关键，且**不是**前两轮说的「整片丢」）**
- 【源】`Shaders/DummyShaderTextExporter.cs`（免费版默认，`ExportSettings.ShaderExportMode` 默认 `Dummy`）：
  - `if (shader.Has_ParsedForm())` ⇒ `writer.Write($"Shader \"{shader.ParsedForm.Name}\" {{")` 之后 **`Export(shader.ParsedForm.PropInfo, writer)`** —— 即**把原 shader 的属性表逐条重建成 `Properties { … }`**（`Name`/`Description`/类型/默认值全抄，含 `[HideInInspector] [PerRendererData] [NoScaleOffset] [Normal] [HDR] [Gamma]` 这些 flag），再接模板 SubShader。
  - 模板只有 4 个（`Templates.json`）：`Default` / `SingleTexture` / `SingleColor` / `TransparentDiffuse`，按「有没有 `_MainTex`/`_Color`」选；**都不匹配时**用写死的 `FallbackDummyShader`（`#pragma surface surf Lambert`）。
  - ⚠️ **兜底分支才是「属性表整片丢」**：`else`（没有 ParsedForm）走 `shader.Script.String` 的 SubShader 之前那段 + `FallbackDummyShader` —— **那个 fallback 里一条属性都不声明**。**【本地】我们 0/234 走这条**（见结论 4）。
- ⇒ **材质属性在 Unity 导入时能不能留住，取决于占位 shader 有没有声明同名属性** —— 我们**184/234 有**，且没属性的 44 个名字几乎全是 `Hidden/*`。
  ⚠️ **推断（未实测）**：Unity 导入 `.mat` 时只保留 shader 声明过的属性名 ⇒ 那 44 个 `Hidden/*` 若被材质引用，属性会被丢。**没跑过，标为推断。**
- 【issue】`#1815`（2025-06-17 报，2025-10-19 关）用户实测：「`.mat` material files, but the shaders in them are all set to `Hidden/InternalErrorShader`」⇒ 存在**连不上 shader 时材质退化成 InternalErrorShader** 的实况；ds5678 只回了「The current shader decompilation implementation is unmaintained and provided as-is. **Use dummy shaders** if you want shaders to compile in the editor.」

**1.4 免费 vs 付费（材质相关）**
- 【源】`ShaderExportMode` 枚举有 `Dummy / Yaml / Decompile`；但开源 `ProjectExporter.Overrides.cs` 的 switch **只有两个分支**：`ShaderExportMode.Yaml => new YamlShaderExporter(), _ => new DummyShaderTextExporter()` ⇒ **`Decompile` 分支不在开源仓里**。
- 【源】`ShaderExportModeDropDownSetting.cs` 原文：`ShaderExportMode.Decompile => GameFileLoader.Premium ? Localization.ShaderAssetFormatDecompileDescription : Localization.NotAvailableInTheFreeEdition`。
- 【源】`docs/articles/PremiumFeatures.md` 原文：「This is an experimental shader decompiler … it's not yet polished, so **expect it to throw errors while ripping some shaders and for there to be compilation errors in Unity Editor**.」平台限制：「DirectX shaders can only be decompiled on Windows computers.」

## 二、网格 / 贴图 / Sprite / 音视频（【源】为主）

| 类别 | 导出成什么 | 判据 |
|---|---|---|
| **Mesh** | **`*.asset`（YAML）+ 顶点数据内联**，不是 OBJ/FBX。`YamlStreamedAssetExportCollection.ExportMesh`：`mesh.VertexData.Data = mesh.StreamData.GetContent(collection)` 再直写，写完把 StreamData 还原 | 【源】`Project/YamlStreamedAssetExportCollection.cs` |
| **Texture2D / Cubemap** | PNG（默认，`ImageExportFormat = Png`）；另有 TGA/JPG…；`PreferOriginalTextureExtension = true` 默认优先原扩展名 | 【源】`ExportSettings.cs` |
| **Sprite** | **默认 `SpriteExportMode.Yaml`** ⇒ 出 YAML sprite 资产（源码注释：「This is **the only mode that ensures a precise recovery of all metadata** of sprites」）；另两档 `Native` / `Texture2D` 都被源码标「Using this is inadvisable」 | 【源】`SpriteExportMode.cs` |
| **Audio** | `AudioExportFormat`（默认 `Default`），先 `YamlAudioExporter` 打底，按档再加 Native / 转码器 | 【源】`ProjectExporter.Overrides.cs` |
| **AnimationClip / AnimatorController** | `.anim` / `.controller`，**有专门的处理器**（`Processing/AnimationClips/AnimationClipConverter.cs`、`AnimatorControllerProcessor.cs`） | 【源】`ExportCollection.GetExportExtension` |
| **FBX** | **在 2.0.0 的代码树里找不到**。我按 `(?i)(PrimaryContent\|Fbx\|SharpGLTF\|Glb\|ObjMesh\|Stl)` 过滤 1641 条路径，模型导出器**只有 `Glb*`**（在 `Export.PrimaryContent/Models/`，是「raw 提取」模式，不是工程模式） | 【源】tree 过滤结果 |

⚠️ **对「Unity 导入器会静默改坏原版资产」的意义**：Mesh 走的是**把浮点顶点直接写进 YAML 资产**，**不经过 OBJ/模型导入器** ⇒ 理论上**绕开**我们那条坑（OBJ 不切子网格 / 贴图砍到 2048）。**没实测**。

## 三、场景 / Prefab（【源】）

- 场景：`SceneYamlExporter` 按 `asset.MainAsset` 分流 —— `SceneHierarchyObject → SceneExportCollection`、`PrefabHierarchyObject → PrefabExportCollection`，都出 YAML `.unity` / `.prefab`（`GetExportExtension` 对应 `SceneAssetExporter` / `PrefabExportCollection.GetExportExtension => "prefab"`）。
- **免费版对「游戏里本来就存成 PrefabInstance 资产的 prefab」是出 `.prefab` 的**：`Processing/Prefabs/PrefabProcessor.cs` 遍历 `gameData.GameBundle.FetchAssets().OfType<IPrefabInstance>()` 逐条建 `PrefabHierarchyObject`；`PrefabExportCollection` **就在开源树里**（`Flags |= SerializeForPrefabSystem`）。
- **付费的是「把场景里被内联（instantiated）的 GameObject 还原回 prefab」**：`PremiumFeatures.md` 原文「When a game is compiled, **all prefabs in a scene are inlined** (instantiated), so any information about the original prefab is lost. This feature attempts to reverse that process… default false.」⇒ 与第二轮报告「内联 GameObject 平铺、Outline 是付费」**一致，复核成立**。

## 四、粒子（ParticleSystem）

- **「没有 particle 专项目录/文档」—— 复核【成立】**。理由（可复查）：
  - 【源】`Export.UnityProjects/` 的全部子目录只有 `AnimatorControllers / Audio / AudioMixers / DeletedAssets / EngineAssets / Miscellaneous / PathIdMapping / Project / RawAssets / Scripts / Shaders / Terrains / Textures` —— **无 particle/vfx**。
  - 【源】全仓与粒子有关的代码**只有 4 个强类型访问器**：`SourceGenerated.Extensions/ParticleSystem{,EmissionBurst,ForceFieldParameters,Renderer}Extensions.cs`（给生成类加读字段的便捷方法），**没有任何 `ParticleSystemExporter`**。
- 粒子走**通用 native class 导出**（随场景/prefab 的 YAML walker 出去）。
- 【issue】已知缺陷：`#1815 [Bug]: **Particle prefab file lose it's material**, and .mat file lose it's shader`（2025-06-17 报，2025-10-19 关）。用户追问「particle effect prefab has lost the information about which material it was using. Is there any way to restore this?」，另有第三人 `@EMBlin1022` 回「I also encountered the same problem」—— **维护者没给解法就关了**。⇒ **粒子 prefab 的材质引用丢失是未解决/被忽略的**。
- 【本地】原版数据在：`assets_full` 里 **18 个 bundle 有 `ParticleSystem/` 目录**（另有 `ParticleSystemRenderer/`）⇒ 问题不在「提不出来」，在「**引用落不回工程**」——与第一轮那条「78 个 prefab 被遮罩」同族。

## 五、MonoBehaviour（为什么这条对我们最要命）

- **免费版不是「全 missing」**，两条路：
  - **Mono**：【源】`docs/articles/CommonIssues.md` 原文「Except in extremely rare and special cases, **there are no mono scripts stored in asset bundles**. Mono scripts are stored in C# assemblies, ie `.dll` files… Place all the assemblies in a folder. Select that folder and the assetbundle(s) … Drag those into AssetRipper at the same time.」成功标志 = 日志出 `Files use the 'Mono' scripting backend.`，失败 = `Unknown`。
  - 🔴 **IL2CPP：2.0.0 把 Cpp2IL 直接内置了**。【源】`Import/Structure/Assembly/Managers/IL2CppManager.cs`：`Cpp2IlApi.InitializeLibCpp2Il(GameAssemblyPath!, MetaDataPath!, UnityVersion, false)`，静态构造里 `LibCpp2IlBinaryRegistry.RegisterBuiltInBinarySupport()`；【源】`Import/Platforms/PlatformGameStructure.cs`：`DefaultGameAssemblyName = "GameAssembly.dll"`、`DefaultGlobalMetadataName = "global-metadata.dat"`、`HasIl2CppFiles()` 两个文件存在即走 IL2CPP ⇒ **不必自己先跑 Cpp2IL**（与文档里那句「For IL2Cpp games, use Cpp2IL to generate assemblies」相比，**2.0.0 已内建**）。
  - 🔴 **【本地】我们两个文件都在**：`d:/2/unity_run_ref/GameAssembly.dll`（76,765,184 B）+ `d:/2/unity_run_ref/Warpforge_Data/il2cpp_data/Metadata/global-metadata.dat` ⇒ **把 `d:/2/unity_run_ref/` 整个目录喂给它就能反序列化 MonoBehaviour 字段**。（【源】`Requirements.md`：跑 binary 还需要装 **Unity Editor**，版本「at least as recent as the game version」—— 我们 6000.3.23f1 ≥ 原版 6000.2.6f2，满足。）
- 组件型 MonoBehaviour（挂在 GameObject 上的）走的是 `SceneYamlExporter`（`OverrideExporter<IComponent>`），只有**非组件**的才走 `ScriptableObjectExporter`（`ScriptableObjectExporter.CreateCollection`: `monoBehaviour.IsComponentOnGameObject() → EmptyExportCollection`）。
- ⚠️ **风险实例**【issue】`#2320 [Bug]: Properties on scripts are lost`（2026-08-02 报 / 2026-08-04 关，**Free edition，IL2CPP 游戏**）：`UnityEngine.UI.Image` 等脚本只剩那几个基类字段（`m_Script`/`m_Name` 之后就没了），而 `1.3.14` 是正常的；ds5678 定位到 **Cpp2IL 侧**并提交了 `SamboyCoding/Cpp2IL#596` ⇒ **要拿到修复后的版本（≥2.0.0 / nightly）**。

## 六、中文来源核对（CSDN 那篇 + gitcode 镜像）

**(a) 用户点名那篇 —— 正文【取到了】**（`curl -A <浏览器 UA>` 直接命中服务端渲染的 HTML，我落到 `d:/2/tools/toolsurvey3/r3/csdn.html`，54,418 B）。
标题《AssetRipper 完整教程：快速提取 Unity 游戏资源》，发布 2026-10-13，387 阅读。要点与**出入**：
- 顶部与底部都挂「【免费下载链接】… 项目地址: `https://gitcode.com/GitHub_Trending/as/AssetRipper`」【中文】。
- ① 版本：「AssetRipper 支持 Unity 3.5 到 **6000.2** 的几乎所有版本」—— **与上游 README 不一致**（原文「AssetRipper supports Unity versions from `3.5.0` to `6000.4.X`」）。
- ② 格式：「能把游戏文件里的 3D 模型、纹理、音频等资源导出为 **FBX、PNG、WAV** 等可编辑格式」—— **FBX 在 2.0.0 源码里找不到实现**（见 §二表末行）。
- ③ 🔴 「提取时还会保留资源间的引用关系：**模型仍指向材质，材质仍指向纹理**」—— **方向对，但通篇没提「shader 是占位、材质渲染不对」**，而这恰恰是最要命的一条（英文 docs + issue 说得明明白白）。**中文这篇会让人以为材质提到手就能用。**
- ④ 「Mesh Export Format = Native / Image Export Format = PNG / Script Content Level = 1」三条建议值，与源码默认值**大体一致**（【源】`ExportSettings` 默认 Png / Hybrid / Dummy；「Native」对应 mesh 的 YAML `.asset`）。
- ⑤ 尾部署名原文：**「创作声明：本文部分内容由AI辅助生成（AIGC），仅供参考」** ⇒ **不是一手实测，不能当判据**（与铁律 2「别猜」同一条）。

**(b) gitcode 镜像（中文站）**【中文】：
- `https://gitcode.com/GitHub_Trending/as/AssetRipper` HTTP 200（202,023 B），页面标题是「**AssetRipper:从Unity序列化文件和资源包中提取并转换资产的工具 - AtomGit**」，**Star 19 / Fork 9 / 5 名成员**（上游 8,632★）；README 是机器简介「图形用户界面应用程序，用于处理引擎资源…【此简介由AI生成】」。
- `…/releases` HTTP 200 但正文 **5,792 B**、**无任何 `.zip`/`.exe` 链接**（我把页面里所有 href 过了一遍）⇒ **镜像上没有可执行文件**（详见 §七）。

**(c) 其他中文源**：CSDN 站内搜 `AssetRipper 材质` 命中 **30 篇**（如《AssetRipper实战指南：Unity游戏资源逆向提取与**着色器修复**》《Unity 资产提取指南…》），搜 `AssetRipper 材质丢失` 6 篇、`AssetRipper shader 粉色 修复` 6 篇 —— **但后续这些页一律返回 521（WAF 拦截），正文取不到**（如实报，未读 = 不写）。另有 Bing（`setlang=zh-CN`）命中的中文标题若干（《AssetRipper 资源提取完整指南》《Unity资产提取神器AssetRipper：从零基础到精通的完整教程》），**同样只拿到标题**。

## 七、🔴 可执行文件的可达下载路线（逐条给 http 码 + 确切 URL；⛔ 我没下载/没运行）

| # | URL | 实测（`curl`） | 结论 |
|---|---|---|---|
| 1 | `https://github.com/AssetRipper/AssetRipper/releases/download/2.0.0/AssetRipper_win_x64.zip`（44,439,367 B） | **HEAD 200**；`-L -r 0-1023` **连测 5 次全部 `http=206 size=1024`**（default ×3、`-4` ×2，落到 `release-assets.githubusercontent.com`）；**`-6`（IPv6）= `http=000 size=0`** | ✅ **唯一可用**。🔴 **主对话那次 000 可解释**：这条链**间歇 + IPv6 不通**，**加 `-4` 就稳**（我第一次 HEAD 200 之后紧接着一次 GET 也曾 000，隔 2 秒重试即 206 ⇒ 偶发，**失败重试 1~2 次**） |
| 2 | `https://gitcode.com/GitHub_Trending/as/AssetRipper/releases` | **200**（5,792 B，正文只有 AtomGit 落地页文案，**0 个资产链接**） | ❌ |
| 3 | `https://gitcode.com/GitHub_Trending/as/AssetRipper/releases/download/2.0.0/AssetRipper_win_x64.zip`（照 github 路径猜的） | **401** | ❌ |
| 4 | `https://api.nuget.org/v3-flatcontainer/assetripper.export.unityprojects/index.json` · `…/assetripper/index.json` | **404** · **404**；NuGet 搜索 `AssetRipper` = **42 个包，全是库**（`.Primitives` / `.SourceGenerated` / `.TextureDecoder` / `.GUI.Web.Dependencies` / `.Tpk` / …），**没有主程序包** | ❌ 拿不到 App（但**侧证源码可编**） |
| 5 | 本机 `dotnet --list-sdks` | **只有 `8.0.425`**；【源】`Requirements.md`「To build AssetRipper from source, you'll need: **.NET 10 SDK**」 | ⚠️ 走源码自建**要先装 .NET 10 SDK**（走 1 的预编译包则不需要） |
| 6 | `https://archive.org/details/AssetRipper-AssetRipper-20251004212129` · `https://web.archive.org/web/2026/https://github.com/…/releases` | **000** · **000** | ❌ 本机不可达 |

**给主对话的建议**（一行）：`curl -4 -L -o AssetRipper_win_x64.zip <#1 的 URL>`，失败重试 1–2 次；拿不到再考虑装 .NET 10 SDK 走源码（`git clone` 也行，但注意本轮 `--depth 1` 那次的 checkout 很慢）。

## 八、没查清的（写「没查到 + 我搜过哪些词 / 哪些 URL」）

1. **没实测**：AssetRipper 跑在**我们这个原版包**上的导出结果（.mat 实际长什么样、50 个 `Hidden/*` 材质的属性会不会真丢）—— 本报告只读、不许下载运行。**「Unity 会丢掉 shader 未声明的属性」这一句是推断，不是实据。**
2. **粒子字段完整度**：没查到任何文档/issue 说明 `ParticleSystem` 各字段（曲线/`GravityModifier`/`SizeOverLifetime`…）导出是否完整。**搜过**：GitHub issues `repo:AssetRipper/AssetRipper+particle`（12 条，逐条看了前 5 条标题，只有 #1815 相关）、以及本机 1641 条路径过滤 `particle`（只 4 个扩展器文件）。
3. **CSDN 其余 29 篇正文取不到（521）**。**搜过**：CSDN 站内 API `so.csdn.net/api/v3/search` 的 `AssetRipper 材质` / `AssetRipper 材质丢失` / `AssetRipper shader 粉色 修复`；**读过的 URL 只有**：用户点名那篇 `blog.csdn.net/gitblog_00975/article/details/154221414`（成功）与 `weixin_30603633/98020437`、`weixin_30693683/97542576`、`weixin_30787531/96518987`（**三篇均 521，正文空**）。
4. **`AssetRipper.MeshSharp.FBX` 这个 NuGet 依赖到底有没有被用**：取 `Export.Modules.Models.csproj` 时网络中断（`http=000`），**没拿到**。所以我给的是「**代码树里没有 Fbx 导出器**」，**不是**「依赖里没有 FBX 库」。
5. **`Prefab Outlining` 的具体实现**：免费树里只有 `PrefabProcessor`（对已有 `IPrefabInstance` 建 prefab），**「场景内联还原」那段代码不在开源仓**（只有 `PremiumFeatures.md` 的描述 + `PremiumFeaturesPage` 的状态显示）—— 与 shader 反编译同一种形态（**实现被剥到付费构建**）。
6. **`api.github.com` 可用、`github.com` / `raw.githubusercontent.com` / `cdn.jsdelivr.net` 时通时不通**（本轮后半段 `github.com`=000、`raw.*`=000、`cdn.jsdelivr.net`=000，而 `api.github.com`=200、`baidu.com`=200）⇒ 后半段我改用 **contents API** 取源码；**这条网络现象本身**建议记一笔，主对话下载时优先 `-4` + 重试。
