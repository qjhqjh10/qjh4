# `§26` 第 11 条 VAT ①驱动 —— 交件（第六会话 · 动手写手）

> 本件 = `VAT①`（**驱动**）。②（自写 VAT shader）**不在本件**，但本件把它的判据也查齐了（见 §一·C）。
> **本次没跑 Unity**（用户本轮要求：待办没做完之前不跑自检）· **没动 git** · **没改正本** · 行尾逐个数过。

---

## 一、结论

### A. 🔴 开工前那一处「8 个位置分量的编码」——**查到了，而且是端到端验过的**

| 问题 | 答案 | 判据 |
|---|---|---|
| 位置怎么编进贴图 | **bounds 相对**：`p = _BoundsCenter + _BoundsExtents * (2 * texel.rgb - 1)` | DXBC VS：`mad r4.xyz, r4.xyzx, r5.xyzx, -cb0[132].xyzx` / `add r4.xyz, r4.xyzx, cb0[131].xyzx`（`vat_seg/06_vs.asm:75-77`，另 `00_vs.asm:49-51` 同形）；CPU 版旁证 `VATPlayer__GetPositions.c:52-81` 逐行同一个算式（`center + extents*(2v-1)`） |
| 「8 个分量」是什么 | **不是 8 个独立分量**。位置贴图宽 = **2 × PartsCount**，但**逐对完全相同**（6 件 VAT × 全部行/部件实测 **6199 对，0 对不同**）；shader 只读**偶数那个 texel**（`u = (k+0.5)/PartsCount`，在 2P 宽的贴图里正落在 texel `2k` 与 `2k+1` 的**边界**上，bilinear 取平均 ⇒ 仍得原值）。第 2 个 texel 是**重复填充** | 烘焙器字节码 `VATGenerator__WritePositionsTexture.c:106-133` —— 两个 `SetPixel`（`x=iVar9` 与 `x=iVar9+1`）**传的是同一个 Color**；实测脚本核 6 张贴图（`pos_pairs.py`） |
| 真·高精度走哪条 | **另一张贴图** `_PositionsTexB`（A 粗 + B `frac` 低位，`0x437f0000=255` / `0x3b808081=1/255` 参与位切分）。而 **`Matcap Full Options VAT` 根本没有这个槽位** —— 27 段 DXBC 全读，只有 `t0/t1` 两张贴图 ⇒ `_HighPrecisionMode` / `_PositionsTexB` 对本地这 6 件（`HighPrecisionPositionMode` **全为 0**）是 **no-op** | `VATGenerator__WriteHighPrecisionPositionsTextures.c:52-57,110-138`；`disasm_dxbc` 全 27 段的 `dcl_resource_texture2d` 只有 2 条 |
| 帧间插不插值 | **插**（线性）。`v = _State`（连续量、没有 floor），采样器 **Bilinear + Clamp、无 mip** | 贴图资产 `m_FilterMode: 1` / `m_WrapMode: 1` / `m_MipCount: 1`；DXBC `sample_l …, l(0.000000)` |
| 网格自带的「起始姿势」 | `start = _StartBoundsCenter + _StartBoundsExtents * (2 * 网格 COLOR.rgb - 1)`；最终顶点 = `start + R(q) * (p - start)` | `00_vs.asm:52-63`；网格实测 `COLOR.w == k/(PartsCount-1)`（14 个离散值）、`COLOR.xyz ∈ [0.094, 0.974]` |
| （亚 texel 抖动） | `v = _State + 网格 POSITION.x * 1e-4`（Vanguard 量级 ≤ **1.3e-4**，即一行的 0.5%）⇒ **可忽略** | `00_vs.asm:45` |

**端到端数值验收**（`final_check.py`）：把贴图**最后一行**（= `_state` 1.0 = 静止帧）解出来，与网格 `COLOR.xyz` 解出的起始姿势逐部件比 ——
**14/14 全部吻合，最大偏差 0.0085 世界单位**，而 `_BoundsExtents.y / 255 = 0.0077` ⇒ 残差**正好是 8bit 量化步长**。
⇒ 编码不是「看着对」，是**量出来的**。

### B. 驱动组件**已按原版建出来**（10 个方法体逐条对）

新增 `CardPresentation/Battle/WarpforgeVatDriver.cs`（namespace `WarpforgeVFX`，与兄弟件 `WarpforgeEffectBinder` 同）。
字段 / 时序 / 推送点逐条对应原版 `StoryProgramming.VATGPUPlayer`（**10 个方法体全可读**）：

| 原版 | 本组件 |
|---|---|
| `Awake`：`GetComponent<Renderer>()` → `GetMaterialArray()`（= `.materials`，**实例材质**）→ 11 × `Shader.PropertyToID` | 同（11 个名字用 `__Awake.c` 的字面量地址 × `stringliteral.json` 互校出来的那 11 个） |
| `Update`：`_vatAnimation == null` 直接 return；否则**每帧** `SendDataToRenderer()`；`_playing` 时才推进 `_state` | 同 |
| `PlayRecording()`：`_playing=1; startTime=Time.time` | 同 |
| `UpdateAnimation()`：`end = startTime + Duration/_animationSpeed`；`t=(now-start)/(end-start)`，`<0→0`、`>1→1`（`DAT_1834b2bb8` **实测 1.0f**）；`now>end ⇒ _playing=0` | 同 |
| `SendDataToRenderer()`：逐材质 `SetTexture`×2/3 · `SetInt(_HighPrecisionMode)` · `SetInt(_PartsIdsInUV3)` · **`SetFloat(_State, _state)`** · `SetInt(_PartsCount)` · 4 × `SetVector`（`.w = 0`） | 同 |
| `SetAnimation` / `IsThereAnimation` / `UpdateMaterials(bool shared)` | 同 |

**挂在哪、什么时候跑**
- 挂在 **和那个 `MeshRenderer` 同一个 GameObject** 上（原版 `VATGPUPlayer` 就在那儿）：
  `WarpforgeVFX/Prefabs/Vanguard Frame Animated VAT.prefab` 根（原版 4 个组件里的第 4 个 = 这个），
  以及 `WarpforgeVFX/Prefabs/VanguardIdleEffect.prefab` 里那个**同名子物件**。
- **何时跑**：`Awake` 取材质 + 属性 ID；之后**每帧** `Update → SendDataToRenderer()`（与原版同 —— 原版也是每帧都推，不只是播放中）。
- 谁实例化它：`CardPresentation/Core/TraitFrames.cs:73` 把 `KeywordTable.Vanguard` → 效果名 `Vanguard Frame Animated VAT`
  （断言在 `CardPresentation/Editor/BattleScene.cs:4888-4892`），效果库负责加载。

### C. `_state` 的真来源（这条决定了「像不像」）

🔴 **原版 `_state` 不是驱动器自己走出来的** —— 是父物件 `VanguardIdleEffect` 上那条 legacy `Animation`
（`m_PlayAutomatically: true` / `m_WrapMode: 0`）播 `Vanguard Frame Animation` 写进来的。
那条 clip 的**全部 6 条曲线**（逐条读完）：

| 曲线 | classID | 键 |
|---|---|---|
| **`_state`** | **114**（MonoBehaviour，`script` → 原版脚本） | **(0.0, 0.07) → (1.7916666, 1.0)** |
| `m_Enabled` | 23 Renderer | (0,0) → (0.0416667, 1) ← **这件 mesh 靠它才显示**（prefab 里写的是 `m_Enabled: 0`） |
| `material._EmissionColor.r` | 23 | 1.9767 → 1.9767 → **311.497**(t=1.4166666) → 1.9767 |
| `material._EmissionColor.g/b` | 23 | 1.0163 → 1.0163 → **160.157**(t=1.4166666) → 1.0163 |
| `material._EmissionColor.a` | 23 | 恒 1.0 |
| `m_IsActive` | 1 | 恒 1 |

⚠️ **`PlayRecording()` 全库 0 个调用者**（25096 份反编译里搜 `StoryProgramming_VATGPUPlayer__PlayRecording` 零命中）
⇒ 原版那条「自走计时」是**死代码**，`_state` 只有曲线一条来源。
⇒ 本组件因此：**能拿到 clip 就让位给 clip**（原版唯一的路）；拿不到（我们现在正是这种 —— 见下）时才用
`_stateCurveKeys`（就是上表那两个键）兜底 —— 判据文件 `Animator动画_导入路与判据.md:174-175` 明确要求
「把 **0.07→1.0 / 1.7917 s** 那条曲线搬过来」。

---

## 二、改动 / 新增清单

`⛔` 未动（白名单之外）：`Core/` · `RuleEngine/` · `Net/` · `Editor/BattleScene.cs` · `EffectExporter.cs` · 两张正本 · git。

### 新增
| 文件 | 说明 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Battle/WarpforgeVatDriver.cs` | **驱动组件本体**（365 行；`WarpforgeVatData` 数据类 + `WarpforgeVatDriver`）。文件头把「逐条对应原版 / 原版那条曲线 / 驱动推出去之后 shader 怎么用 / 实读常量 / 挂点与时机」全写进去了 —— **写 ② 的人看这一个文件就够** |
| `…/CardPresentation/Battle/WarpforgeVatDriver.cs.meta` | guid `21ee0ab585ea4007ab7810b8f6fcabae`（新造，与全工程 10769 条现有 guid 去重核过） |
| `…/WarpforgeVFX/Textures/Vanguard Frame Animation VAT_PositionTex.png` (+`.meta`) | **28×41**，与源包 **逐字节相同**（544 B）。guid `af7086ca0f754f149ffc4655277ab9e1` |
| `…/WarpforgeVFX/Textures/Vanguard Frame Animation VAT_RotationTex.png` (+`.meta`) | **14×41**，逐字节相同（584 B）。guid `3baf7cd065c844c2bd484a2a051bb5ce` |
| `…/WarpforgeVFX/Animations/Vanguard Frame Animation.anim` (+`.meta`) | **legacy clip 重建**（`m_Legacy: 1`、24 fps、`m_WrapMode: 1`、6 条曲线逐键照抄）。guid `067f477a302d4bfe86c5a04ddd081e6e` |

新建 `.meta`（贴图）的关键开关**按资产实测**设的，⛔ 不是默认值照抄：
`nPOTScale: 0`（**28×41 / 14×41 是 NPOT**，默认的 `1`(ToNearest) 会把它缩放、布局全毁）·
`enableMipMap: 0` · `sRGBTexture: 0`（原资产 `m_ColorSpace: 0` = **线性数据贴图**，彩色贴图是 1）·
平台 `textureFormat: 4`(RGBA32) + `textureCompression: 0`（**别让它压成 DXT/BC7**）· `filterMode: 1` · `wrapU/V: 1`。

### 改
| 文件 | 改动 |
|---|---|
| `WarpforgeVFX/Prefabs/Vanguard Frame Animated VAT.prefab` | 根 GameObject 组件表加 1 条 + 文件尾加 1 个 `MonoBehaviour`（fileID `5590582095431144137`），把原版那份数据（bounds / Frames 41 / PartsCount 14 / Duration 1.35 / 两张贴图）逐字写进去 |
| `WarpforgeVFX/Prefabs/VanguardIdleEffect.prefab` | ① 同名子物件（GO `5838308720746842456`）加同一个组件（fileID `7791699558725360724`）；② **修 `m_Animation` / `m_Animations` 的断链**（`guid: 00000000000000000000000000000000` → 新 clip 的 guid，`:59081` / `:59083`） |

> ⚠️ **没接线的那一项**：`_stateClip` 我**故意留空**（`{fileID: 0}`）—— 原版驱动器自己也**不认识** `Animation`，
> 是「谁播我我不管」。本组件在 `OnEnable` 里自己 `GetComponentInParent<Animation>()` 往上找
> ⇒ 两个 prefab 都不用改接线，也免得多一处会漂的硬引用。

---

## 三、证据（逐条出处）

**反编译**（`d:/2/tools/decomp_full/`，本目录下 `StoryProgramming.*` 共 **15 个** VAT 方法体全读）
- `VATGPUPlayer__Awake.c:33-66`（11 个属性 ID 的地址 → 名字）· `__Update.c:16-32` · `__UpdateAnimation.c:6-25` ·
  `__SendDataToRenderer.c:19-91` · `__PlayRecording.c:5-8` · `__SetAnimation.c:3-7` · `__UpdateMaterials.c` ·
  `__IsThereAnimation.c` · `__.ctor.c:3`（`_animationSpeed = 1.0f`）
- `VATGenerator__WritePositionsTexture.c:51-54`（`Texture2D((param_7+1)*param_3, param_4, …)`）`:106-133`（两个 texel 同值）
- `VATGenerator__WriteRotationsTexture.c:37-38,74-78`（`(q - (-1)) * 0.5` ⇒ `(q+1)/2`，正是 shader `2t-1` 的逆）
- `VATGenerator__WriteHighPrecisionPositionsTextures.c:52-57,110-138`（两张贴图 A/B）
- `VATPlayer__GetPositions.c:35-36,52-81`（CPU 版：`GetPixel(x=部件, y=帧)` + `center + extents*(2v-1)`）

**DXBC**（`d:/4/Unity/工具/disasm_dxbc.py`，本件**没改它**，只用）
- `Everguild/Matcap/Matcap Full Options VAT` = `bundle_battleprefabs_vfxandmisc_assets_all/Shader/Shader_6331056250094909814.json`
  的 **27 段** DXBC（4 个 VS 家族 × 3 变体 + 5 个 PS 家族 × 3），逐段反汇编后落盘在 `/d/4/_tmp_view/vat_seg/`：
  - `00_vs.asm:41-47`（uv 算式）`:49-63`（位置/旋转/起始姿势）`:74-100`（法线→matcap uv）
  - `06_vs.asm`（pass 1 的 VS，同形）· `12_vs.asm`（motion-vector 那条，`cb1[43]`）· `21_vs.asm`（`cb0[136]`）
  - **全部 27 段 `dcl_resource_texture2d` 只有 t0/t1** ⇒ 无第三张贴图

**资产**（`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`）
- `MonoBehaviour/MonoBehaviour_-6996853648599967635.json`（本件那个驱动器实例：`_vatAnimation` / `_state: 0.0` / `_animationSpeed: 1.0`）
- `MonoBehaviour/Vanguard Frame Animation VAT.json`（12 个字段，本件数据类的来源）
- `Animation/Animation_-8538308667419355027.json`（父件 `Animation`，`m_PlayAutomatically: true`）
- `AnimationClip/AnimationClip_8189764618720115800.json`（`m_Legacy: True`、6 条曲线、每个键的 time/value/slope/weight）
- `GameObject/Vanguard Frame Animated VAT.json`（4 个组件，第 4 个 = 驱动器）· `GameObject/VanguardIdleEffect.json`（6 个组件）
- `Texture2D/Vanguard Frame Animation VAT_{Position,Rotation}Tex.png`（28×41 / 14×41，`m_TextureFormat 4`、`m_ColorSpace 0`、`m_FilterMode 1`、`m_WrapMode 1`、`m_MipCount 1`、`m_IsReadable False`）
- **6 件 VAT 组件全读**：`HighPrecisionPositionMode` **全 0**、`PositionsTexB` **全 null**、位置贴图宽**全是 2×PartsCount**

**实读常量**（`d:/2/unity_run_ref/GameAssembly.dll`，按 RVA→节表→文件偏移直读 4 字节；`IMAGEBASE = 0x180000000`）

| 地址 | 值 | 用途 |
|---|---|---|
| `0x1834b2bb8` | **1.0f** | `_state` 上夹值（判据文件 §五 第 2 条那条「没验」的，**现已复读**） |
| `0x1834b2c80` | `-0.0f`（`0x80000000`） | 反编译里 `^ uVar3` 那个取负掩码 |
| `0x1834b2bb4` / `0x1834b2bc8` | 0.5f / -1.0f | 旋转贴图编码 `(q+1)*0.5` |
| `0x1834b2bc0` / `0x1834b2bac` | 255.0f / 1/255 | 高精度两贴图的位切分 |

**我们的工程（现状）**
- `WarpforgeArena1/Editor/EffectExporter.cs:878-880`（`StripMissingScripts` → `AttachPoolables`
  —— **现成的「补挂」钩子**，本件的根治改法照抄它）· `:1309-1316`（`RemoveMonoBehavioursWithMissingScript`）
- `CardPresentation/Core/TraitFrames.cs:73` · `CardPresentation/Editor/BattleScene.cs:4888-4892`
  （`Vanguard Frame Animated VAT` 在效果库里 —— 本件没改这条）
- `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:183`（`Everguild/Matcap/Matcap Full Options VAT` 在
  「未映射 → 掉原版 bundle 兜底」的 28 名单里 ⇒ **今天那件 mesh 还是占位材质**，② 的活）
- `.gitignore:26` `/Unity/MyGame/Assets/WarpforgeVFX/*`（只放行 `Runtime/` `Shaders/`）

---

## 四、验证

**秒级类型检查**（`TMPDIR=/tmp/wf_vat bash d:/4/Unity/工具/typecheck.sh`）
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（跑了**两次**：建完 `.cs` 一次、全部落盘后再跑一次，都是 0/0。⚠️ 本轮有别的写手在改 `Core/` 等文件，**0 条错**说明没撞上别人的半成品。）

**行尾**（二进制读，改前 → 改后；⛔ 没用 `sed -i`、⛔ 没用文本模式写 CRLF 文件）

| 文件 | 改前 CRLF/LF | 改后 CRLF/LF |
|---|---|---|
| `WarpforgeVatDriver.cs` | 新建 | 0 / 365 |
| `WarpforgeVatDriver.cs.meta` | 新建 | 0 / 1（**无尾随换行**，与全工程 `.cs.meta` 一致：`fileFormatVersion: 2\nguid: …`） |
| `Vanguard Frame Animation.anim` | 新建 | 0 / 326 |
| `…anim.meta` | 新建 | 0 / 8 |
| `…_PositionTex.png` / `.meta` | 新建 | 二进制（`\r\n` 是像素字节）/ 0 / 117 |
| `…_RotationTex.png` / `.meta` | 新建 | 二进制 / 0 / 117 |
| `Vanguard Frame Animated VAT.prefab` | 0 / 227 | **0 / 260** |
| `VanguardIdleEffect.prefab` | 0 / 124219 | **0 / 124252** |

**结构对账（新建的 clip）**：与工程里已知可用的 `Card Explosion.anim` 逐键比
—— **顶层 26 个键完全一致（无缺无多）**；与 `Booster Opening - Card Idle.anim` 比 curve 块
—— **19 个子键完全一致（无缺无多）**。

**资产逐字节**：两张 PNG 与源包 **同字节**（544 / 584 B）。

---

## 五、没查清 / 停手的（逐条：卡在哪、试过什么）

1. 🔴 **原 clip 的 `tangentMode`（键的切线模式）查不到** —— 解包 JSON 的 `m_Curve` 条目里**没有这个字段**
   （提取器的类型树没收它），我只拿到 `time/value/inSlope/outSlope/weightedMode/inWeight/outWeight`。
   - **有据的推论**：`m_Enabled` / `m_IsActive` 的 `inSlope = outSlope = **Infinity**` ⇒ 那是 Unity 写「**Constant(台阶)**」的方式
     ⇒ 这两条我写 `tangentMode: 3`（**推断，不是直读**）。
   - **没据的那条**：`_state` 两个键的斜率是 **0**，而 Unity 的 **Auto** 模式会把两键曲线的切线算成段斜率（0.519），**不会是 0**
     ⇒ 它更像 **Linear**。但那个 `tangentMode` 是**位域**（左右各自的模式 + 权重标志打包成一个 int），
     我**想不起也不敢写**它的确切值 ⇒ 写了 **`tangentMode: 0`（Auto，也是全工程现有 `.anim` 的统一值）**。
   - **影响**：若 Unity 按 Auto 重算 ⇒ `_state` 会变成「两端缓入缓出」而不是直线，**动画节奏略有出入**。
     ⛔ **没有拿猜测充数**：斜率我写的是**原件里的数**，只有这一个枚举值不确定。
   - **怎么收口**：跑一次 Unity 看导入后的曲线（或与原版抓帧比一次）⇒ 不对就把 `tangentMode` 改成 Linear 那一档；
     或干脆把 `m_FloatCurves` 里 `_state` 那条删掉，走本组件内置的（**精确线性**的）兜底曲线。
2. **`SendDataToRenderer` 里四个 vec4 的 `.w`** —— 写进材质的是 **0**（`local_98._12_4_ = 0`，四处都有），这条**是确定的**；
   但反编译里还有四个**没有消费者**的读取（`local_40 = *(SO+0x20)` 等 = 每个 `Vector3` 后面那个 padding 字）
   —— 它们的字段归属**没查清**（不影响推送结果）。
3. **其余 5 件 VAT（`Card Remnant_Remnant Anim {1,3,5,8,9}`）在我们工程里根本没有** ——
   没有它们的 prefab、也没有 10 张贴图（只有 `RemnantBody3D Aeldari/Necrons` 两件**别的**东西）。
   ⇒ 本件的驱动**数据驱动、按族可用**，但那 5 件要**先由导出器导出来**才接得上。
   **没查**：那 5 件在原版被谁引用（不在本件范围）。
4. **`PlayRecording()` 的调用者**：静态搜索（25096 份反编译里搜那个符号）**0 命中**。
   **没排除**的：反射 / 事件 / 别的程序集里的虚调用 ⇒ 我按「本内容里是死代码」处理（`_state` 由 clip 驱动），
   但保留了 `PlayRecording()` 的等价实现，不影响任何一种情况。
5. **没跑 Unity** ⇒ **本件的资产改动一条都没被引擎导入验证过**（`.anim` 能不能被认、贴图 meta 对不对、
   prefab 里那个 `m_EditorClassIdentifier` 解不解得开）。**这是本件最大的未验证面**，见 §七 的断言建议。

---

## 六、顺手发现的

1. 🔴 **`.gitignore:26` = `/Unity/MyGame/Assets/WarpforgeVFX/*`**（只放行 `Runtime/` `Shaders/`）
   ⇒ **本件改的 prefab / 贴图 / clip 一个都不入库**，`git status` 看不见。
   这正是 `CLAUDE.md` 里记的那族坑（`Resources/Art/**` 同理）：**「写手改了、git 一声不响」**。
   下个会话别拿 `git status` 当「本件有没有动过资产」的判据。
2. 🔴 **`VanguardIdleEffect.prefab:59081/59083` 的 `m_Animation` guid 全是 0** —— 与
   `Animator动画_导入路与判据.md:120-122` 记的那个症状**同形**（`guid: 000…0` 被写回资产）。
   全仓 `fileID: 8189764618720115800, guid: 00000000000000000000000000000000` **只有这一处**（已 grep 全 `Assets/`）。**本件已修**。
3. **`EffectExporter` 早就有一模一样的「补挂」钩子**（`AttachPoolables`，`:847-880`，A210 那轮建的，
   头注把「为什么必须在 `StripMissingScripts` 之后」写得很清楚）⇒ 本件的根治改法**照着抄**就行，落点不用再找。
4. **网格侧**：`Vanguard Frame Animated_Mesh` 的 `uv0` 只有 **2 个分量**（stride 48 = pos12 + normal12 + color16 + uv8），
   而 `COLOR.w` 正好是 `k/(PartsCount-1)` 的 14 个离散值 ⇒ **`PartsIdsInUV3 == 0` 时部件号在 `COLOR.w`**，
   与 pass 0 那条 VS 的 `v4.w` **对得上**。
   ⚠️ 但 motion-vector 那条 VS（`12_vs.asm`）用的是 `v3.w`（TEXCOORD0.w = 网格里不存在）——
   **那条 pass 在这件材质里是关的**（`Material.m_SavedProperties` 同级有 `disabledShaderPasses: ["MOTIONVECTORS"]`）
   ⇒ 用不上，但**别拿 `12_vs.asm` 当部件号的判据**。
5. **`Everguild/Matcap/Matcap Full Options VAT` 全库只有这一个 shader**（把 `assets_full/*/Shader/*.json`
   的 `m_ParsedForm.m_Name` 全扫一遍：含 `VAT` 的**只此一件**；顺带扫到 4 件 `Matcap` 族）。
   它有两份**副本**：原件在 `battleprefabs_vfxandmisc_assets_all.bundle`（pathID `6331056250094909814`），
   另一份在我们工程的 `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle`（**同 pathID**）。
   （`disasm_dxbc.py` 那次打「命中 3 个」是它把 `PROJECT_BUNDLE_DIR` 也扫了的原因，多的一份没定位 —— 不影响结论。）
6. `VanguardIdleEffect.prefab` 124k 行，但全文件只有 **1 处** `WarpforgeEffectBinder` 引用（在根上）——
   其余子件没有 binder。（**只是记录**，没查它是不是设计如此。）

---

## 七、建议的断言清单（宿主 = `CardPresentation/Editor/BattleScene.cs`，⛔ **本批一条都没加**）

> 走 `Check(...)` / `CheckTrue(...)`（与本文件既有风格一致）。**每条都盯原版判据**，不盯我们自己的常量。

**A. 静态（资产面）**
1. `WarpforgeVFX/Prefabs/Vanguard Frame Animated VAT.prefab` 根上**有** `WarpforgeVFX.WarpforgeVatDriver`，
   且**不是 missing script**（原版该 prefab 的 4 个组件里第 4 个就是它 —— 判据 `GameObject/Vanguard Frame Animated VAT.json`）。
   ⚠️ 这条断言要**同时**在 `VanguardIdleEffect.prefab` 的**同名子物件**上做一遍 —— 出牌与待机是**两个入口**
   （`CLAUDE.md` §十 第 5 条那种坑）。
2. `_vatAnimation` 的 12 个字段**逐条**对原资产 `MonoBehaviour/Vanguard Frame Animation VAT.json`：
   bounds 四组、`Frames == 41`、`PartsCount == 14`、`Duration == 1.35`、两个 bool 为 0、两张贴图非空、`PositionsTexB` 为空。
3. **贴图导入设置**（防「跑一次导入就毁掉布局」）：`nPOTScale == 0`（28×41 / 14×41 **是 NPOT**）、
   `enableMipMap == 0`、`sRGBTexture == 0`、平台 `textureFormat == RGBA32` 且 `textureCompression == Uncompressed`、
   `filterMode == Bilinear`、`wrapU/V == Clamp`。
4. `VanguardIdleEffect.prefab` 的 `Animation.m_Animation` **guid 不是全 0**，解析得到 `Vanguard Frame Animation`，
   且 `m_Legacy == true`、`m_StopTime ≈ 1.7916666`、`m_SampleRate == 24`、`m_WrapMode == 1`。
5. clip 的 `m_FloatCurves` 逐条核：6 条、`_state` 两点 (0, 0.07)/(1.7916666, 1.0)、
   `classID == 114` 且 `script` 指向**我们的** driver guid、`path == "Vanguard Frame Animated VAT"`；
   另有 `m_Enabled`（0→1 @ 0.0416667）、4 条 `material._EmissionColor.*`、`m_IsActive`。

**B. 结构闸（🔴 灭自证 —— 两边一起改也过不去）**
6. **尺寸互推闸**：`PositionsTex.width == 2 * PartsCount` **且** `PositionsTex.height == Frames`
   **且** `RotationsTex.width == PartsCount` **且** `RotationsTex.height == Frames`。
   这一条把「贴图被缩放/换掉」与「数据被写错」**同时**变成红 —— 只改一边永远绿不了
   （判据 = 6 件 VAT 实测的布局，见 §一·A）。
7. **唯一写入点闸**：`_state` 的**写入点只有** `UpdateAnimation()` 与 `EvalStateCurve()` 两处
   （`grep` 源文件数 `_state =` 的出现次数 / 位置），且 `SetFloat(_idState, _state)` 必须在 `SendDataToRenderer()` 里。
   ⇒ 谁把曲线改成常数、或把 `SendDataToRenderer` 里的推送删掉，这条红。

**C. 运行时探针（新起一个 `-executeMethod`，或并进 `BattleScene.Run`）**
8. 实例化 `VanguardIdleEffect.prefab`、把 `Animation` 推进到给定时间，读**每份材质**的 `_State`：
   `t = 0 → 0.07`、`t = 1.7916666 → 1.0`、中间**单调不减**、且**真的变了**（⛔ 不许「全程 0 也绿」）。
9. 同一次探针里断 `MeshRenderer.enabled` **由 0 → 1**（`t > 0.0416667` 之后）——
   这条验的是 clip 那条 `m_Enabled` 曲线，**也是「照原版」与「自己发明」的分界**：原版这件 mesh
   出厂是 `m_Enabled: 0`，**只有动画会开它**。
10. **「没做 ② 就不许绿」闸**：`WarpforgeShaderMap` 里 `Everguild/Matcap/Matcap Full Options VAT` 仍走
    「未映射 → 掉原版 bundle 兜底」（`:183`）⇒ 那件 mesh 现在还是**占位材质**。
    ⇒ ② 完成之前，断言只许断「**参数推到了材质上**」（拿到的 `Material` 上 `HasProperty("_State")` 为假时**出声**，
    ⛔ 不许静默当成通过）；**不许**断「画面正确」。

---

## 八、300 字摘要

**那一处编码查到了**：位置 = **bounds 相对** `p = _BoundsCenter + _BoundsExtents*(2*texel.rgb-1)`（DXBC 直读，
CPU 版旁证）；所谓「8 个分量」其实**不是 8 个** —— 位置贴图每部件占 2 texel 但**逐对全等**（6 件 VAT 共 **6199 对，0 对不同**，
烘焙器字节码也证实两次 `SetPixel` 写同一个值），shader 只读偶数 texel；真·高精度走**另一张贴图** `_PositionsTexB`，
而 `Matcap Full Options VAT` **只有 t0/t1 两个槽**（27 段 DXBC 全读）⇒ 对本地这 6 件（`HighPrecisionPositionMode` 全 0）是 no-op。
**端到端验过**：贴图末行解出的 14 个部件位置与网格起始姿势**全部吻合，最大偏差 0.0085 = 8bit 量化步长**。
帧间**插值**（Bilinear/Clamp 无 mip，`v = _State` 连续）。顺带复读了 `DAT_1834b2bb8 = 1.0f`。

**建了什么**：`WarpforgeVatDriver.cs`（逐条对应原版 10 个方法体：时序、11 个属性名、每帧推送），
挂到 `Vanguard Frame Animated VAT.prefab` 与 `VanguardIdleEffect.prefab` 的**同名子物件**上；
导进两张 VAT 贴图（**NPOT/线性/不压缩** 的导入设置）、重建 legacy clip `Vanguard Frame Animation`（6 条曲线逐键照抄），
并修掉 `m_Animation` 的全零 guid 断链。**类型检查 0 错**，行尾逐个核过。

**还欠**：① `EffectExporter` 要照 `AttachPoolables` 加 `AttachVatDrivers`（否则重导会把它删掉）；
② clip 的 `tangentMode` 提取器没收，写在「没查清」里；③ 其余 5 件 Remnant VAT 本地没有。
**本轮没跑 Unity**，断言建议 10 条见 §七。
