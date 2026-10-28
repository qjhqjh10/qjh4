# B94_R-G · 画质档位映射（`EverguildQualitySetting` ↔ Unity `QualitySettings`）

> 只读查证代理 **R-G** · 第九十四会话 · 2026-10-29
> 任务：`MaterialChangerByQuality`（账 `未分类_其他:423` / `:424`）的卡点 —— 「默认 Ultra 下 5 个实例全不命中 ⇒ no-op；组件拿不到输入」。
> **产出物仅此一份文件，未改任何源码 / 账 / 正本。**

## §〇 结论速览（三句）

1. 🔴 **「组件拿不到输入」不成立。** `MaterialChangerByQuality` 读的**不是** Unity 的 `QualitySettings.GetQualityLevel()`，而是静态字段 **`GameStaticData.graphicsQuality`（`+0x120`，类型 `EverguildQualitySetting`）**。
   我们工程**已经有这一格**（`CardPresentation/Shell/GameStaticData.cs:106`，默认 `Ultra`）⇒ 输入本来就通，**不需要任何「画质档映射」来喂它**。
2. ✅ **默认档 Ultra(4) 下 5 个实例全不命中 = 原版行为**，照抄即可。5 个实例的 `presets` 只有 `quality 0/1/2` 三行（`§四.1` 表），`4` 一定不命中 ⇒ `Initialize()` 循环走完直接 `return`。这不是缺陷，是**原版就是这样**。
3. 🔧 真要复刻的那张表在**别处**：`QualitySettingsManager.QualityDefinition[5]`（`EverguildQualitySetting → (URP 资产, qualityNumber, clamp, AA, SS)`），判据 = **原版 `Intro` 场景 `MonoBehaviour &346`**（`§三.2` 逐行）。而 Unity 侧**原版是 6 档**（Very Low/Low/Medium/High/Very High/Ultra），枚举只有 5 档 —— **两者索引不一一对应**，靠 `qualityNumber` 显式映射。

---

## §一 `EverguildQualitySetting` 全枚举

| 名字 | 数值 | 出处 |
|---|---|---|
| `VeryLow` | 0 | `D:/2/Warpforge_code/Scripts/Assembly-CSharp/EverguildQualitySetting.cs`（签名桩全文只有这 5 行） |
| `Low` | 1 | 同上 |
| `Medium` | 2 | 同上 |
| `High` | 3 | 同上 |
| `Ultra` | 4 | 同上 |

交叉源（三处一致）：
- `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GameStaticData.cs:218` `public static EverguildQualitySetting graphicsQuality;`
- `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LocalPlayerData.cs:59` 同名字段（存档面）
- 我们自己的副本：`Unity/MyGame/Assets/CardPresentation/Shell/GameStaticData.cs:64`（逐字一致，无偏移）

相关静态量（**偏移是反编译 `.c` 里读出来的，不是猜**）：

| 静态格 | 偏移 | 类型 | 默认（`.cctor`） | 出处 |
|---|---|---|---|---|
| `graphicsQuality` | `+0x120` | `EverguildQualitySetting` | **High(3)**（⚠️ 会被 `LoadPlayerData` 覆盖成 Ultra，见 §二） | ILSpy `GameStaticData.cs:2360`；`.c` `MaterialChangerByQuality__Initialize.c` 与 `GraphicsTab__GraphicsQualityDropdownChange.c` 都读 `+0x120` |
| `graphicsQualityChosenManually` | `+0x12C` | bool | false | ILSpy `GameStaticData.cs:2363`；`GraphicsQualityDropdownChange.c` 写 `+300 (=0x12C)` |
| `useSuperSampling` | `+0x124` | bool | 由 `LoadPlayerData` 定（见 §二） | `GraphicsTab__OnSetup.c`（toggle+0x40 读 `+0x124`）／`QualityDefinition__TryEnableQuality.c` 写 |
| `vsync` | `+0x127` | bool | true | `.cctor` `vsync = true`；`ApplyGraphicsQuality.c` 读 `+0x127` |
| `FPSLimit` | `+0x128` | int | 2 | `PlayerDataManager__ApplyGraphicsQuality.c` 读 `+0x128`（0→30fps / 1→60fps / 2→不限） |
| `DefaultQualitySetting`（属性） | — | `EverguildQualitySetting` | **`return 4` = Ultra** | `.c` `GameStaticData__get_DefaultQualitySetting.c`（全文就一句 `return 4;`）；ILSpy `GameStaticData.cs:484` |

> ⚠️ 别被 `.cctor` 的 `High` 骗了：**`DefaultQualitySetting` 是 Ultra，且 `LoadPlayerData` 在「玩家没手动选过」时用它覆盖**（§二 第 1 跳）。

---

## §二 映射链（谁 → 什么时候 → 把哪档 → 写到哪）

### 第 0 跳 · 启动顺序（关键，决定了「默认 Ultra」真的生效）

`PlayerDataManager.Awake()`（ILSpy `PlayerDataManager.cs:7768`）**显式串行**：

```
LoadPlayerData();                                   // :7807  —— 把 graphicsQuality 灌成 Ultra
SetTargetFramerate(DEFAULT_TARGET_FRAMERATE);
QualitySettingsManager.Instance.Initialize();       // :7809-7810 —— 之后才 ApplyGraphicsQuality
loginManager.Initialize();
```

⇒ **数据先于画质**。所以启动时 `ApplyGraphicsQuality` 拿到的是 **Ultra(4)**，不是 `.cctor` 的 High(3)。

### 第 1 跳 · `LoadPlayerData()`：决定「这局用哪档」

`PlayerDataManager.cs:10268`（`private unsafe void LoadPlayerData()`）：

- `GameStaticData.graphicsQualityChosenManually = localPlayerData.graphicsQualityChosenManually;`（`:10324`）
- **没手动选过**（`:10326`）：
  - `GameStaticData.graphicsQuality = GameStaticData.DefaultQualitySetting`（`:10329-10330`；ILSpy 把调用打成 `default`，真身 = `get_DefaultQualitySetting()` @VA `0x18087E560`，`.c` 里 `return 4`）
  - `useSuperSampling = !Application.isMobilePlatform && Screen.width >= 1920`（`:10331-10355`）
  - 日志 `"Graphics quality loaded to default " + <档名>`
- **手动选过**（`:10357` 起）：`graphicsQuality = localPlayerData.graphicsQuality; useSuperSampling = localPlayerData.useSuperSampling;`（`:10359-10360`）

> 出厂存档里 `LocalPlayerData.graphicsQuality` 的 ctor 默认是 **High**（`LocalPlayerData.cs:98`），但 `chosenManually=false` ⇒ 它不被采信，**实际生效 = Ultra**。

`.c` 佐证：`PlayerDataManager__LoadPlayerData.c`（`LoadSharedDefaultValues()` 在 `LoadStarterData` 之前，两者都把 `graphicsQuality = default()` 写成 `get_DefaultQualitySetting()`）。

### 第 2 跳 · 玩家改档（设置页）

`GraphicsTab.GraphicsQualityDropdownChange(int chosenQuality)`（ILSpy `GraphicsTab.cs:186-199`；`.c` 全文 `GraphicsTab__GraphicsQualityDropdownChange.c`）：

```
Type.GetTypeFromHandle(typeof(EverguildQualitySetting));
Enum.Parse(type, chosenQuality.ToString())          // 下拉索引 == 枚举值（选项由 OnSetup 按 Enum.GetValues 顺序建）
if (GameStaticData.graphicsQuality != 新值)
    SettingsMenu.ApplyGraphicsChangesOnClose = true;   // 反编译里的 `[this+0x28]+0xC0` = SettingsMenu 的 public bool
GameStaticData.graphicsQuality = 新值;                 // +0x120
GameStaticData.graphicsQualityChosenManually = true;   // +0x12C
CustomDebug.LogWarning("Graphics quality changed in settings to " + <档名>);
ConfigureSuperSamplingVisibility();
```

- 下拉选项怎么来的：`GraphicsTab.OnSetup`（ILSpy `:480-500` 区）用 `Enum.GetValues(typeof(EverguildQualitySetting))` 逐项加，文案取本地化键 **`"Settings/Graphics/" + 枚举名`**（我们的本地化表里 5 个键全在：`Settings/Graphics/{VeryLow,Low,Medium,High,Ultra}`）。
- 回填当前值：`qualityDropdown.SetValueWithoutNotify((int)GameStaticData.graphicsQuality)`（ILSpy `:490`）。
- ⚠️ `ConfigureSuperSamplingVisibility` 读的是**下拉框 `m_Value`**（`GraphicsTab.cs:88`）再 `GetQualityDefinition(...)`，不是读 `GameStaticData`。
- `SettingsMenu` 那个脏位字段名 = **`ApplyGraphicsChangesOnClose`**（桩 `D:/2/Warpforge_code/.../SettingsMenu.cs`）。

### 第 3 跳 · 关窗时真生效

`SettingsMenu.Close()`（`.c` `SettingsMenu__Close.c`；ILSpy `SettingsMenu.cs:180`）：

```
PlayerDataManager.SavePlayerData();                 // 先把档位写进存档
GameWindowWithTabs.Close(...);
if (ApplyGraphicsChangesOnClose) {
    QualitySettingsManager.Instance.ApplyGraphicsQuality();
    ApplyGraphicsChangesOnClose = false;
}
```

`SavePlayerData()`（`PlayerDataManager.cs:10170`）里 `localPlayerData.graphicsQuality = GameStaticData.graphicsQuality`（`:10237`）、`…ChosenManually`（`:10244`）。

### 第 4 跳 · `ApplyGraphicsQuality()` —— 唯一的写入点

`QualitySettingsManager.ApplyGraphicsQuality()`（ILSpy `QualitySettingsManager.cs:283-320`；`.c` `QualitySettingsManager__ApplyGraphicsQuality.c` 全文）：

```
if (!initialized) { OriginalScreenWidth/Height = Screen.width/height; initialized = true; }   // 只捕一次
foreach (var d in qualityDefinitions)                              // 数组在 Intro 场景里序列化（§三.2）
  if (GameStaticData.graphicsQuality == d.quality) {               // ★ 精确相等，无区间
      GraphicsSettings.defaultRenderPipeline = d.universalRenderPipelineAsset;  // +0x18
      QualitySettings.SetQualityLevel(d.qualityNumber);                        // +0x28
      d.ChangeResolution();                                                     // 改 URP renderScale
      Shader.EnableKeyword/DisableKeyword("SUPERSAMPLING_ON");                  // 依 d.enableCardsAntiAliasing (+0x2C)
      if (!d.allowSuperSampling) GameStaticData.useSuperSampling = false;       // +0x2D → 写 +0x124
      QualitySettings.vSyncCount = GameStaticData.vsync ? 1 : 0;                // 读 +0x127
      PlayerDataManager.singletonManager.ApplyGraphicsQuality();                // 帧率：30/60/不限
  }
```

- ⚠️ **`QualitySettings.antiAliasing` 全程没被写过**（MSAA 来自 URP 资产 `m_MSAA`）。
- `ChangeResolution()`（ILSpy `:136-180`；`.c` 同名）：
  `clamp = InCombatScene ? clampInCombatResolution : clampInMenuResolution`；
  移动平台且 `clamp>0 && Screen.width>clamp` ⇒ `renderScale = clamp/Screen.width`；
  否则 `renderScale = (非移动 && allowSuperSampling && GameStaticData.useSuperSampling) ? 2 : 1`。
  ⇒ **Ultra 在 PC 上：clamp 全 0、allowSuperSampling=1、默认 1920+ 屏 ⇒ renderScale = 2.0**（这就是「超采样」）。
- 调用时机两个：① `QualitySettingsManager.Initialize()`（ILSpy `:262-271`，由 `PlayerDataManager.Awake` 点名调用，并额外 `Signal.Register<ScreenResolutionChangeSignal>` + `EverguildSceneManager.SceneTransitionEnded`）；② `SettingsMenu.Close()`（有脏位时）。

### 第 5 跳 · `GameStaticData.graphicsQuality` 的全部读者（= 我们复刻必须覆盖的面）

| 读者 | 判读方式 | 出处 |
|---|---|---|
| `QualitySettingsManager.ApplyGraphicsQuality` | **精确 ==** | ILSpy `QualitySettingsManager.cs:290` |
| `MaterialChangerByQuality.Initialize` | **精确 ==** | ILSpy `MaterialChangerByQuality.cs:86`（`.c` 同，读 `+0x120`） |
| `ObjectTogglerByQuality.OnEnable` | **区间**：`cond ? q>=th : q<=th` | ILSpy `ObjectTogglerByQuality.cs:13-36` |
| `kTools.Mirrors.Mirror.SelectQuality / OnEnable` | **精确 ==**（不命中就取 `qualityPresets[0]`） | ILSpy `kTools.Mirrors/Mirror.cs:242`、`:865` |
| `GraphicsTab.ConfigureSuperSamplingVisibility` | 用下拉框值 → `GetQualityDefinition` | `GraphicsTab.cs:88` |

### 附带 · 版本迁移（会改档）

`LoginManager.DoNewVersionOperations(oldVersion)`（ILSpy `LoginManager.cs:1443-1475`）：旧版本 `<= 1.20.0` 时**把画质升一档**（`min(q+1, 枚举末位)`，若已是 `VeryLow` 则不动）；`<= 1.31.1` 时 `vsync=true; FPSLimit=2`；之后 `SavePlayerData()`。

---

## §三 Unity 侧 `QualitySettings` 的档位序

### 三.1 原版工程 = **6 档**

| index | 名字 | 出处 |
|---|---|---|
| 0 | Very Low | ① `D:/2/unity_run_ref/Warpforge_Data/globalgamemanagers` 偏移 2080-2165 的连续串（**二进制亲读**：`\x06 count=6` 后依次 `"Very Low"(8)` `"Low"(3)` `"Medium"(6)` `"High"(4)` `"Very High"(9)` `"Ultra"(5)`）；② `D:/2/tools/assetripper_out/ExportedProject/ProjectSettings/QualitySettings.asset:10` |
| 1 | Low | 同上（asset `:63`） |
| 2 | Medium | 同上（asset `:116`） |
| 3 | High | 同上（asset `:169`） |
| 4 | Very High | 同上（asset `:222`） |
| 5 | Ultra | 同上（asset `:275`） |

- `m_CurrentQuality: 0`（asset `:7`）是**编辑器序列化值**，运行期由 `ApplyGraphicsQuality` 覆盖。
- 6 档的 `customRenderPipeline` **全是 `{fileID: 0}`**（asset `:51/104/157/210/263/316`）⇒ 渲染管线**不走** Unity quality level，而是运行期 `GraphicsSettings.defaultRenderPipeline = d.universalRenderPipelineAsset`。
- 各档原始设置（供对账用）：Very Low→Low 都 `shadows:0`；Medium `shadows:1 vSync:1`；High `shadows:2`；Very High/Ultra `antiAliasing:2`。

### 三.2 `EverguildQualitySetting` → Unity 档号（`qualityNumber`）**不是索引平移**

判据 = 原版 `Intro` 场景 `D:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity`，`GameObject &50 "QualitySettingsManager"` → `MonoBehaviour &346`（`sed -n '6740,6795p'` 现读）：

| `quality`（枚举） | URP 资产（guid → 名字） | `qualityNumber`（= Unity 档号） | `clampInCombat` | `clampInMenu` | `enableCardsAntiAliasing`（控 `SUPERSAMPLING_ON`） | `allowSuperSampling` |
|---|---|---|---|---|---|---|
| 0 VeryLow | `6b6efd2b…` = **UniversalRenderPipelineAsset Low** | **1**（Low） | 1280 | 1280 | 0 | 0 |
| 1 Low | `53b0634d…` = **… Medium** | **2**（Medium） | 1920 | 1920 | 0 | 0 |
| 2 Medium | `53b0634d…` = **… Medium** | **2**（Medium） | 1920 | 0 | 0 | 0 |
| 3 High | `6ab4f96e…` = **… High** | **2**（Medium） | 1920 | 0 | 1 | 0 |
| 4 Ultra | `0a3397ff…` = **… Ultra** | **2**（Medium） | 0 | 0 | 1 | 1 |

⇒ **除 VeryLow 用 Unity 档 1 外，其余四档的 `qualityNumber` 全是 2**（= Unity 的 "Medium" 档）。**Unity 档位在这套系统里几乎只是容器**，真正的差别在 URP 资产 + renderScale + 关键字 + 分辨率夹取。

4 个 URP 资产的关键差异（`Assets/MonoBehaviour/UniversalRenderPipelineAsset {Low,Medium,High,Ultra}.asset` 现读）：

| 资产 | `m_RenderScale` | `m_MSAA` | `m_SupportsHDR` | `m_RequireDepthTexture` | `m_MainLightShadowsSupported` | `m_MainLightShadowmapResolution` | `m_ShadowDistance` |
|---|---|---|---|---|---|---|---|
| Low | 1 | 1 | 0 | 0 | 0 | 1024 | 15 |
| Medium | 1 | 1 | 0 | 0 | 0 | 512 | 15 |
| High | 1 | 1 | 1 | 1 | 1 | 2048 | 15 |
| Ultra | 1 | 1 | 1 | 1 | 1 | 4096 | 15 |

renderer 资产：Low/Medium 用 `renderer 11e1fde5…`，High/Ultra 用 `renderer 3b341e1b…`；两者都追加 `0d67daec…`（第二项，疑 UI renderer）。

### 三.3 我们工程（对照，不是判据）

`Unity/MyGame/ProjectSettings/QualitySettings.asset`：**只有 2 档** `Mobile`(0) / `PC`(1)，`m_CurrentQuality: 1`；两档的 `customRenderPipeline` 都指向我们自己的 URP 资产（`5e6cbd92…` / `4b83569d…`），`GraphicsSettings.asset:88` 的 `m_CustomRenderPipeline` = PC 那份。
⇒ **与原版 6 档 / 5 档枚举没有任何对应关系**（这一点 `SettingsWindow.cs:3887` 的注释已经如实记了）。

---

## §四 给施工的结论（`MaterialChangerByQuality`）

### 4.1 五个实例的实测数据（判据，不是转述）

脚本/反查方式：`bundle_Waprforge_monoscripts/MonoScript/MonoScript_-6903873682265095532.json`（= 本类的脚本 pathID）→ 全 `assets_full` 里 `m_Script.m_PathID = -6903873682265095532` 的 `MonoBehaviour/*.json` ⇒ **共 5 个，全部在战场侧**：

| # | 所在包 | 挂在哪个对象 | presets（quality → shader） |
|---|---|---|---|
| 1 | `bundle_battlesharedresources_assets_all` | **`Ground`**（MB `-3128930142240273445`） | 0 → `Everguild/Misc/Unlit shadows receiver`（`7745480916046788`）<br>1 → 同上<br>2 → `Everguild/FX/Floor Planar Reflections Grainny`（`-8049749197193168683`） |
| 2 | `bundle_battlesharedresources_assets_all` | **`Floor (1)`**（MB `-3393742555676221536`） | 同上 |
| 3 | `bundle_scenes_scenes_battlearena3` | `Ground`（MB `4523`） | 同上 |
| 4 | `bundle_scenes_scenes_battlearenaaeldari` | `Floor (1)`（MB `4902`） | 同上 |
| 5 | `bundle_scenes_scenes_battlearenatauviorla` | `Floor`（MB `4753`） | 0/1 → `Everguild/Misc/Unlit shadows receiver Vertex Color Shadow Mask`（`Shader_29`）<br>2 → `Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask`（`Shader_30`） |

- **全部 `material` 为 null，只填 `shader`** ⇒ 走的是「换 `sharedMaterial.shader`」那条支，不是 `SetMaterial`。
- 出厂材质：`Ground` 的 MeshRenderer（`MeshRenderer_3223950338352319451.json`）→ `Material "Ground"`（`-1558128744687014823`）→ `m_Shader = -8049749197193168683` = **Grainny**。
  ⇒ **在 Ultra/High（不命中）以及 Medium（命中 2）时，视觉上是同一个 shader**；**只有 VeryLow/Low 才真的换成 `Unlit shadows receiver`**。

### 4.2 复刻口径（逐句照原版）

```
输入：CardPresentation.GameStaticData.graphicsQuality   // ⛔ 不是 QualitySettings.GetQualityLevel()
时机：InitializableBehaviour.Start() → SceneInitializer.QueueForInitialization → Initialize()
      （原版序列化 priority = 2 = UI；对我们的宿主就是「场景/战场建好之后、开画之前」跑一次）
算法：
  mr = GetComponent<MeshRenderer>();
  if (!mr) { CustomDebug.LogError("[ERROR] Trying to change the material to an object without mesh"); return; }
  foreach (p in presets)
      if (GameStaticData.graphicsQuality == p.quality) {
          if (p.material) { mr.SetMaterial(p.material); }               // 用 .material（实例）还是 .sharedMaterial
          else if (p.shader) { mr.sharedMaterial.shader = p.shader; }   // 取决于「是否运行期」那一支
          return;                                                       // ★ 命中第一行就停
      }
  return;                                                               // ★ 不命中 = 什么都不做（no-op）
```

- **判「命中/不命中」只有一条规则：`==` 精确相等，取 `presets` 里第一个匹配项，然后 `return`。** 没有任何区间/回退/默认项。
- **入口只有一个**：`Initialize()`。没有 `OnEnable`、没有监听器 ⇒ **档位在运行中被改后它不会自己重跑**（这点照原版：改档是「关设置窗 → `ApplyGraphicsQuality`」，游戏内不重建战场）。
- ⚠️ 反编译里那个 `UnityEngine_UIElements_BaseVerticalCollectionView__HandleItemNavigation(0)` 是**符号错配**（ICF 折叠），语义是「现在是不是运行期」——两支的差别只有 `renderer.material`（运行时实例） vs `renderer.sharedMaterial`。对 `material == null` 的这 5 个实例**完全无影响**（都走 shader 支）。
- ⚠️ `GetComponent<MeshRenderer>()` **只取自身**，不查子节点。

### 4.3 于是「Ultra 下 no-op」怎么判

- ✅ **不需要动**：`graphicsQuality` 默认 Ultra ⇒ 5 个实例必然 no-op ⇒ **出厂画面就是原版出厂画面**（Ground 保留 `Material "Ground"` 的 Grainny）。
- 🔎 **要验证「接线真的通了」只能靠临时改档**：把 `GameStaticData.graphicsQuality` 临时置 `VeryLow`，`Ground` 的 `sharedMaterial.shader` 应变成 `Everguild/Misc/Unlit shadows receiver`；置 `Medium` 应仍为 Grainny（且材质实例被赋成 Grainny 那一份）。断言建议只断「shader 名字」，不要断材质引用相等。

### 4.4 顺带查清的两条（都影响施工口径，别按旧假设写）

1. **`ObjectTogglerByQuality` 也读 `GameStaticData.graphicsQuality`**（`+0x120`，区间判断，`conditionIsHigherOrEqual` 真 = `q >= 阈值`）。
   - 全 13 场 + 全包实测：**只有 2 个实例，都在 `battlearenaaeldari`**（MB `3983` = `toggleBellowIncluding 1 / cond 0 / state 1`；MB `3991` = `2 / 1 / 1`）。
   - 按 `q=2` 与 `q=4` 逐实例求值：**两档结果完全相同**（`Portal` 那一对在 2..4 都是 High on / Low off）。
   ⇒ 我们 `ArenaBuilder` 用 `工具/gen_arena_quality_toggle.py`（`QUALITY = 2`，`gen_arena_quality_toggle.py:49`）烘焙出来的数据，**在真实的 Ultra(4) 下依然正确** —— **这不是缺陷**，别去「修」它。
2. **我们设置窗的画质下拉目前是自设计**：`SettingsWindow.cs:3885-3915` 直接列 Unity 的 `QualitySettings.names`（我们工程 = `Mobile`/`PC` 两档），并在选档时改 `QualitySettings`。
   按用户 2026-10-28 口径（能照原版一律照原版）⇒ 应改成：5 项 `EverguildQualitySetting`（文案 `Settings/Graphics/<名>`，键已存在）+ 写 `GameStaticData.graphicsQuality` + 脏位 + 关窗 `ApplyGraphicsQuality`（§二 第 2/3/4 跳）。
   ⚠️ 这一步的**前置依赖**是 §五 第 1 条（我们还没有原版那 6 档 + 4 个 URP 资产）。

---

## §五 还差什么（查不到的写「查不到」）

1. **原版 6 档 `QualitySettings` + 4 个 URP 资产，我们工程没有**（我们是自设计的 2 档 + 2 份 URP 资产）。
   判据已经记在 §三.1 / §三.2（含 4 份 URP 资产的 8 个关键字段），但**要把它们做成工程资产是另一件事**（改 `ProjectSettings/QualitySettings.asset` 属于全局设置，按项目规矩要先确认）。
   ⛔ 在做到这一步之前，**不要**让 `ApplyGraphicsQuality` 去 `SetQualityLevel(2)` —— 我们工程只有 0/1 两档，Unity 会夹取，行为与原版不同且**静默**。
2. **`QualityDefinition.TryEnableQuality` 全树 0 调用点**（ILSpy `QualitySettingsManager.cs:47` 只有定义；`out_il2csharp/Assembly-CSharp/QualitySettingsManager.cs:205` 同样只有定义；`decomp_full` 只有它自己那份 `.c`；`script.json:55224` 只登记名字）。
   - 已按项目规矩排除「内联假象」：它**不属于** `BattleManager.ResolveAction` 那条 91 路 switch 的路子（那套只吞 `BattleManager` 的方法）。
   - ⇒ 判定 = **原版死代码**。复刻口径：**照抄保留、不接线**（铁律 11「原版自己的死支也照抄」），并在注释里写「原版 0 调用点，我们也不接线」。
3. **没跑实况**：本单全部为静态判据（反编译 `.c` / ILSpy / 场景 YAML / 包内 JSON）＋ 一处**二进制字节亲读**（`globalgamemanagers`）。原版已关服，未起游戏验证 `SetQualityLevel(2)` 的实际画面差。
4. **那个错配符号没最终定名**：`BaseVerticalCollectionView__HandleItemNavigation(0)` 在 `MaterialChangerByQuality` 里当布尔用。强烈疑似 `Application.isPlaying`（语义自洽：运行期取 `.material`、编辑器取 `.sharedMaterial`），但**没有拿到反证**（`decomp_full` 里同名产物是错的），按铁律记「疑似，未定名」。
5. **`graphicsQualityChosenManually` 的持久化落点没展开**：它进 `LocalPlayerData`（`LocalPlayerData.cs:71`）经 `SavePlayerData` 落盘，但**本地存档文件格式 / 我们 `SaveCenter` 的对应格**不在本单范围，未查。
6. **`buildin` 那侧还有没有别的画质消费者**：本单只核了 5 类读者（§二 第 5 跳）。**没做全量枚举**（比如 `QualitySettings` 相关反射入口、`m_MethodName` 里点名 `ApplyGraphicsQuality` 的 prefab 事件）—— 若要「完全覆盖」还需一次全树扫。**本条如实写「没查完」**。
