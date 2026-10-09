// WarpforgeVatDriver.cs — 原版 `StoryProgramming.VATGPUPlayer` 的等价替身（`§26` 第 11 条 **VAT ①驱动**）
//
// ============================ 为什么需要它 ============================
// 原版 `Vanguard Frame Animated VAT.prefab` 上挂着 4 个组件，其中第 4 个是
// `StoryProgramming.VATGPUPlayer`（`m_Script` → `3946243950355606049`，在
// `bundle_Waprforge_monoscripts` 里）。我们工程里没有那个脚本 ⇒ 导出器
// `EffectExporter.StripMissingScripts`（`WarpforgeArena1/Editor/EffectExporter.cs`，
// 调用点 `:1053`）把**整条 MonoBehaviour 删掉** ⇒ 结果：（行号 2026-10-08 现读刷新 —— 原来写的是 `:1309` / `:878`，都已漂）
//   · `_State` 永远停在 prefab 手写值（`_state: 0.0`）上；
//   · 那件 mesh 的顶点位置永远采样贴图的第 0 行 ⇒ **一点都不动**。
// 本文件把那个组件按原版重建回来（字段 / 时序 / 推送点逐条对应）。
//
// ============================ 🔴 版本库边界：本组件依赖的资产**不在版本库里**（2026-10-08 · A1096①） ============================
// **本文件 + `.meta` 进仓库；它要挂回去的那 8 个资产一个都不进** —— 全部命中
// `.gitignore:26` 的 `/Unity/MyGame/Assets/WarpforgeVFX/*`（那一段只放行 `Runtime/` `Shaders/`；
// 顶层原则写在 `.gitignore:4-8`：「原版资产的直接衍生物留在本地，随时可以用导出器重新生成」）。
// ⇒ **换一台机器 clone 下来只有组件、没有挂点**：本组件会退化成**一个空跑的驱动**
//    （取不到 `WarpforgeVatData` / 顶点贴图，`Update` 每帧推的还是贴图第 0 行），而且**不会报错**。
// ⚠️ 连带一条操作纪律：**那 8 个文件在 `git status` 里根本看不见** ⇒ 「我这支改了没有」**不能拿
//    `git status` / `git diff --numstat` 判**，要直接看 mtime / 字节 / md5（下表即实读值）。
//
// 被挡住的 8 个（2026-10-08 实读 · `git check-ignore -v` 逐个核过 · 路径前缀 `Assets/WarpforgeVFX/`）：
//
//   | 资产                                        | 字节    | md5(前 12)     | mtime            |
//   |---------------------------------------------|---------|----------------|------------------|
//   | `Prefabs/Vanguard Frame Animated VAT.prefab`| 6714    | `aa0309ab6450` | 2026-10-08 23:34 |
//   | `Prefabs/VanguardIdleEffect.prefab`         | 3008659 | `a82e0a059df8` | 2026-10-08 23:34 |
//   | `Animations/Vanguard Frame Animation.anim`  | 7845    | `88388c3792f3` | 2026-10-08 23:34 |
//   | `Animations/Vanguard Frame Animation.anim.meta` | 188 | `0e5391661550` | 2026-10-08 23:34 |
//   | `Textures/…VAT_PositionTex.png`             | 544     | `0e2481439826` | 2026-10-08 23:33 |
//   | `Textures/…VAT_PositionTex.png.meta`        | 2650    | `0d908f143dbf` | 2026-10-08 23:33 |
//   | `Textures/…VAT_RotationTex.png`             | 584     | `339d21b4448b` | 2026-10-08 23:33 |
//   | `Textures/…VAT_RotationTex.png.meta`        | 2650    | `601b3fe32421` | 2026-10-08 23:33 |
//
// **clone 下来怎么补**（按依赖顺序；出处 → `资料/普查产出_第六会话/W_VAT收口_地雷与另5件.md` §④）：
//   ① 两个 prefab + 那个 `.anim`：跑 `-executeMethod EffectExporter.RunListed`
//      —— `EffectExporter.AttachVatDrivers` 会把**本组件连 12 个字段**一起挂回去，
//      `.anim` 由同一条路上的 `ImportClip` 落出来。**这一步之后引用会自己重新接上**（按路径取）。
//   ② 两张贴图：**没有任何导出器步骤会写它们的导入设置**（是手配的）⇒ 要照同目录 `.meta` 逐字段配回来：
//      `nPOTScale: 0`（28×41 是 **NPOT**）· `enableMipMap: 0` · `sRGBTexture: 0`（顶点数据贴图、线性）·
//      `textureFormat: 4`(RGBA32) + `textureCompression: 0` · `filterMode: 1` · `wrapU/V: 1` · `maxTextureSize: 2048`。
//      🔴 **⛔ 别拿 `EffectExporter.ImportTexture` 重导这两张** —— 它写死 `ti.sRGBTexture = true`
//      （`:1987`）且**不动 `nPOTScale`**（默认 `ToNearest`）⇒ 一导就把 28×41 缩成 32×64、**贴图布局全毁**。
//
// ⚠️ **为什么不干脆把这 8 个放行**（A1096① 的另一条路）：**放行这 8 个也不够** ——
//    实测这两个 prefab 分别引用 **10** 与 **26** 个 guid，其中 **8 / 24 个同样被 ignore**
//    （材质 / 网格 / 贴图都在 `WarpforgeVFX/{Materials,Meshes,Textures}` 下）
//    ⇒ 只放行 8 个 = 往仓库里塞一份**引用悬空的半个 prefab**，比一份都不放更糟。
//    要让它们真能用，就得整棵 `WarpforgeVFX/*`（**1.8 GB**）进库 —— 那正是 `.gitignore:24-30` 要避免的。
//
// ⚠️ **本组件不会**去开 `MeshRenderer.m_Enabled` —— 那是原版那条 legacy clip 干的
//    （见下面「原版那条曲线」）。本组件的职责边界与原版 `VATGPUPlayer` 完全一致。
//
// ============================ 与原版的逐条对应 ============================
// 出处 = `d:/2/tools/decomp_full/StoryProgramming.VATGPUPlayer__*.c`（**10 个方法体全可读**）
//        + `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
//          MonoBehaviour_-6996853648599967635.json`（本件那个实例的序列化值）
//        + 数据资产 `.../MonoBehaviour/Vanguard Frame Animation VAT.json`
//
//   原版（反编译）                                     本组件
//   ---------------------------------------------------------------------------
//   `Awake`：`GetComponent<Renderer>()`                `Awake`：同
//     → `Renderer.GetMaterialArray()`                    → `renderer.materials`
//     → 11 × `Shader.PropertyToID(名字)`                 → 同 11 个名字
//   `Update`：`_vatAnimation != null` 才干活            `Update`：同
//     → 每帧都 `SendDataToRenderer()`                    → 同（**每帧都推**，不只是播放中）
//     → `_playing` 时才推进 `_state`                     → 同
//   `PlayRecording()`：`_playing = 1; startTime = now`   `PlayRecording()`：同
//   `SetAnimation(VATAnimation)`：写 `+0x20`             `SetAnimation(WarpforgeVatData)`：同
//   `IsThereAnimation()`：`_vatAnimation != null`        `IsThereAnimation()`：同
//   `UpdateMaterials(bool shared)`：取 materials /        `UpdateMaterials(bool shared)`：同
//     sharedMaterials 并缓存
//   `UpdateAnimation()`：                                 `UpdateAnimation()`：
//     `end  = startTime + VAT.Duration / _animationSpeed`  同
//     `t    = (Time.time - startTime) / (end - startTime)` 同
//     `t < 0 ⇒ 0`（`fVar4 < 0.0`）；`t > 1 ⇒ 1`            同（`DAT_1834b2bb8` **实测 = 1.0f**，
//       （`DAT_1834b2bb8`）                                    见本文件末「实读常量」）
//     写回 `_state`（`+0x28`）                              同
//     `Time.time > end ⇒ _playing = 0`                     同
//   `SendDataToRenderer()` 逐材质：                        `SendDataToRenderer()`：同
//     `SetTexture(_PositionsTex,  VAT.PositionsTex)`       `SetTexture(PositionsTex,   ...)`
//     `SetTexture(_RotationsTex,  VAT.RotationsTex)`       `SetTexture(RotationsTex,   ...)`
//     `if (HighPrecision) SetTexture(_PositionsTexB, …)`   `if (HighPrecisionPositionMode) ...`
//     `SetInt(_HighPrecisionMode, HighPrecision ? 1 : 0)`  同
//     `SetInt(_PartsIdsInUV3,     PartsIdsInUV3 ? 1 : 0)`  同
//     `SetFloat(_State, _state)`                           **`SetFloat("_State", _state)`**
//     `SetInt(_PartsCount, VAT.PartsCount)`                同
//     `SetVector(_BoundsCenter,      (c.x, c.y, c.z, 0))`  同
//     `SetVector(_BoundsExtents,     (e.x, e.y, e.z, 0))`  同
//     `SetVector(_StartBoundsCenter, (c.x, c.y, c.z, 0))`  同
//     `SetVector(_StartBoundsExtents,(e.x, e.y, e.z, 0))`  同
//
//   11 个属性名 = `__Awake.c` 的 `Shader.PropertyToID` 字面量 ×
//   `d:/2/tools/il2cpp_out/stringliteral.json` 互校（地址→名字，逐一命中）：
//     `_PositionsTex` @0x4239E90 · `_PositionsTexB` @0x4239F90 · `_RotationsTex` @0x423C880 ·
//     `_BoundsCenter` @0x42C1D18 · `_BoundsExtents` @0x42C1E18 ·
//     `_StartBoundsCenter` @0x4245478 · `_StartBoundsExtents` @0x4245578 ·
//     `_PartsCount` @0x4239390 · `_PartsIdsInUV3` @0x4239490 ·
//     `_State` @0x4245678 · `_HighPrecisionMode` @0x42D5CE8
//
// ============================ 原版那条曲线（`_state` 的真来源） ============================
// 🔴 `_state` 在原版**不是**本组件自己走出来的 —— 是父物件 `VanguardIdleEffect` 上的
//    legacy `Animation`（`m_PlayAutomatically: true`，`m_WrapMode: 0`(=Default)）
//    播 `Vanguard Frame Animation`（`AnimationClip_8189764618720115800.json`，
//    **`m_Legacy: True`**、`m_SampleRate: 24`、`m_WrapMode: 1`(=Once)、`m_LoopTime: False`）
//    写进来的。那条 clip 的**全部** `m_FloatCurves`（逐条读完，非肌肉格式）：
//      · `_state`                     path=`Vanguard Frame Animated VAT` classID=**114**
//        script=3946243950355606049   keys = **(0.0, 0.07) → (1.7916666, 1.0)**（两点、线性）
//      · `m_Enabled`                  classID=23(Renderer)  keys = (0.0,0) → (0.0416667,1)
//      · `material._EmissionColor.r`   classID=23  keys = 1.9767 → 1.9767 → **311.497**(t=1.4166666) → 1.9767
//      · `material._EmissionColor.g/b` classID=23  keys = 1.0163 → 1.0163 → **160.157**(t=1.4166666) → 1.0163
//      · `material._EmissionColor.a`   classID=23  恒 1.0
//      · `m_IsActive`                  classID=1  恒 1
//    ⚠️ `PlayRecording()` **全库 0 个调用者**（25096 份反编译里搜
//      `StoryProgramming_VATGPUPlayer__PlayRecording` 零命中）⇒ 原版的自走那一段是**死代码**，
//      `_state` 只有曲线这一条来源。本组件因此把曲线的两个键也搬进来当**兜底**
//      （判据文件 `Animator动画_导入路与判据.md:174-175` 明确要求「把 0.07→1.0 / 1.7917s 那条曲线搬过来」）：
//        `_stateClip` 能拿到 clip ⇒ **让位给 clip**（原版唯一的路）；
//        拿不到（我们现在就是这种：`VanguardIdleEffect.prefab` 里那条引用是
//        `guid: 00000000000000000000000000000000` 的**断链**）⇒ 用 `_stateCurveKeys` 复现同一条键值。
//
// ============================ 驱动推出去之后 shader 怎么用 ============================
// （2026-10-08 逐条反汇编 `Everguild/Matcap/Matcap Full Options VAT`
//   —— `Shader_6331056250094909814.json`，4 个 VS 家族 / 27 段 DXBC 全读，
//   脚本 `工具/disasm_dxbc.py`。写 VAT shader（`§26` 第 11 条 ②）时照这个来：）
//
//   u（部件横坐标） = `COLOR.w * (1 - 1/PartsCount) + 0.5/PartsCount`
//                     （网格 `COLOR.w` 实测 = `k/(PartsCount-1)`，k = 部件号 ⇒ `u = (k+0.5)/PartsCount`）
//   v（帧纵坐标）   = **`_State`**（+ 网格 `POSITION.x * 1e-4` 的亚 texel 抖动，量级 ≤1.3e-4 ⇒ 可忽略）
//   `sample_l` 两张贴图，**同一个 uv**：
//     位置贴图（`t0`）→ `p = _BoundsCenter + _BoundsExtents * (2 * T.rgb - 1)`
//     旋转贴图（`t1`）→ `q = 2 * T.rgba - 1`（单位四元数）
//   网格自带的「起始姿势」= `_StartBoundsCenter + _StartBoundsExtents * (2 * COLOR.rgb - 1)`
//   最终顶点 = `起始姿势 + R(q) * (p - 起始姿势)`
//   ⚠️ **采样器是 Bilinear + Clamp、无 mip**（原资产 `m_FilterMode: 1 / m_WrapMode: 1 / m_MipCount: 1`）
//      ⇒ **帧间是线性插值**（`_State` 是连续量，没有 floor）；`u` 正好落在两个**内容相同**的
//      texel 边界上（见下），所以部件方向**不会**串色。
//   ⚠️ 贴图规格：位置贴图 **2 × PartsCount 宽**、旋转贴图 **PartsCount 宽**、高 = `Frames`；
//      逐对逐 texel 核过 **6199 对全等**（6 件 VAT × 全部行/部件）⇒ 位置贴图的**第 2 个 texel
//      是重复的填充**，shader 只读偶数那个（`u = (k+0.5)/PartsCount` 在 2P 宽贴图里落在
//      texel 2k 与 2k+1 的**边界**上，bilinear 取平均 ⇒ 仍得原值）。**不是 8 个独立分量。**
//      真·高精度走的是**另一张贴图** `_PositionsTexB`（`WriteHighPrecisionPositionsTextures`：
//      两贴图同尺寸、A=粗、B=`frac` 低位），而 `_PositionsTexB` / `_HighPrecisionMode`
//      **在这件 shader 里根本没有槽位**（全文只有 `t0/t1` 两张贴图）⇒ 对本地这 6 件
//      （`HighPrecisionPositionMode` **全为 0**）是 no-op。
//
// ============================ 实读常量（`d:/2/unity_run_ref/GameAssembly.dll` 文件偏移直读 4 字节） ============================
//   `0x1834b2bb8` = **1.0f**（`_state` 上夹值 —— 已复读，不再是「按 `[Range(0,1)]` 猜」）
//   `0x1834b2c80` = `-0.0f`（`0x80000000`，反编译里 `^ uVar3` 那个取负掩码）
//   `0x1834b2bb4` = 0.5f · `0x1834b2bc8` = -1.0f（旋转贴图编码 `(q + 1) * 0.5`）
//   `0x1834b2bc0` = 255.0f · `0x1834b2bac` = 1/255（高精度两贴图的位切分用）
//
// ============================ 落地位置 ============================
// 挂点 = **与那个 `MeshRenderer` 同一个 GameObject**（原版 `VATGPUPlayer` 就在那儿）
//        —— `WarpforgeVFX/Prefabs/Vanguard Frame Animated VAT.prefab` 的根，
//        以及 `WarpforgeVFX/Prefabs/VanguardIdleEffect.prefab` 里那个同名子物件。
// 何时跑 = `Awake` 取材质、之后**每帧** `Update` → `SendDataToRenderer()`（与原版同）。
// ⚠️ **导出器还没教过它**：`EffectExporter` 现在只在 `StripMissingScripts`（`:878`）之后补
//    `AttachPoolables`（`:880`），没有 VAT 这一类 ⇒ **下次重导会把本组件连带删掉**。
//    要根治得照 `AttachPoolables` 的形状加一个 `AttachVatDrivers(inst, src.name)`
//    （落点 = `EffectExporter.StripMissingScripts(inst)` 之后、`PrefabUtility.SaveAsPrefabAsset` 之前）。
//    本文件所在的那次改动**没有**动 `EffectExporter.cs`（在白名单之外）⇒ 见交接报告。

using System;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `StoryProgramming.VATAnimation`（`m_Script = -974199562677010219`）的等价数据。
    ///
    /// **字段顺序与名字逐条照抄原资产**（`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
    /// Vanguard Frame Animation VAT.json`）—— 顺序也是原版序列化的顺序（反编译里偏移可互证：
    /// BoundsCenter@+0x18 · BoundsExtents@+0x24 · StartBoundsCenter@+0x30 · StartBoundsExtents@+0x3c ·
    /// Frames@+0x48 · PartsCount@+0x4c · Duration@+0x50 · HighPrecisionPositionMode@+0x54 ·
    /// PartsIdsInUV3@+0x55 · PositionsTex@+0x58 · PositionsTexB@+0x60 · RotationsTex@+0x68）。
    /// 全库 6 件：`Card Remnant_Remnant Anim {1,3,5,8,9}` + `Vanguard Frame Animation VAT`。</summary>
    [Serializable]
    public class WarpforgeVatData
    {
        public Vector3 BoundsCenter;
        public Vector3 BoundsExtents;
        public Vector3 StartBoundsCenter;
        public Vector3 StartBoundsExtents;
        /// <summary>帧数 = 贴图**高**（Vanguard 41 / Remnant 15）。</summary>
        public float Frames;
        /// <summary>部件数 = **旋转贴图宽** = 位置贴图宽的一半（Vanguard 14 / Remnant 75）。</summary>
        public int PartsCount;
        /// <summary>一段动画的时长（秒）。Vanguard = 1.35。</summary>
        public float Duration;
        /// <summary>原版 `HighPrecisionPositionMode`。本地 6 件**全是 0**。</summary>
        public bool HighPrecisionPositionMode;
        /// <summary>原版 `PartsIdsInUV3`。本地 6 件**全是 0**（部件号在 `COLOR.w`）。</summary>
        public bool PartsIdsInUV3;
        public Texture PositionsTex;
        public Texture PositionsTexB;
        public Texture RotationsTex;
    }

    [DisallowMultipleComponent]
    public class WarpforgeVatDriver : MonoBehaviour
    {
        [Tooltip("原版 `_vatAnimation`（PPtr→VATAnimation）。为空则整条 Update 直接 return —— 与原版同。")]
        public WarpforgeVatData _vatAnimation;

        [Tooltip("原版 `_state`。范围 [0,1]（`DAT_1834b2bb8` 实测 1.0f）= 采样贴图的**纵坐标 v**。\n"
               + "原版由父物件 `VanguardIdleEffect` 的 legacy clip 写；clip 拿不到时由 `_stateCurveKeys` 兜底写。")]
        [Range(0f, 1f)] public float _state;

        [Tooltip("原版 `_animationSpeed`（ctor 里写 1.0f）。只影响 `PlayRecording()` 那条自走路。")]
        public float _animationSpeed = 1f;

        [Tooltip("原版那条 legacy clip 的键值（`Vanguard Frame Animation`：t=0→0.07、t=1.7916666→1.0）。\n"
               + "**只在拿不到 `_stateClip` 时**用 —— 原版没有这条兜底，是判据文件要求补的。")]
        public Vector2[] _stateCurveKeys = new Vector2[] { new Vector2(0f, 0.07f), new Vector2(1.7916666f, 1f) };

        [Tooltip("父物件上那条 legacy `Animation`（原版播 `Vanguard Frame Animation`）。\n"
               + "留空则 `Awake` 自动往上找 —— 原版的驱动器自己也**没有**这个引用，是去找父件的。")]
        public Animation _stateClip;

        [Tooltip("没挂到 clip 时，自己按 `_stateCurveKeys` 走 `_state`（兜底）。挂到 clip 时**让位**。")]
        public bool _useBuiltInCurveWhenNoClip = true;

        // ---- 运行时状态（原版对应 `+0x30`/`+0x38`…`+0x60`/`+0x64`/`+0x68`）----
        Material[] _mats;
        bool _playing;          // 原版 +0x64
        float _startTime;       // 原版 +0x68
        float _enableTime;      // 本组件自己那条兜底曲线的起点
        bool _warned;

        // 原版在 `Awake` 里逐实例取；这里同样逐实例取（故意不用 static，免得与 shader 变体脱节）
        int _idPositionsTex, _idPositionsTexB, _idRotationsTex;
        int _idBoundsCenter, _idBoundsExtents, _idStartBoundsCenter, _idStartBoundsExtents;
        int _idPartsCount, _idPartsIdsInUV3, _idState, _idHighPrecisionMode;

        void Awake()
        {
            var r = GetComponent<Renderer>();
            if (r == null)
            {
                // 原版这里直接抛（`FUN_1803f47a0` = 空引用 throw）。我们出声，不静默。
                Debug.LogError($"[VatDriver] `{name}` 上没有 Renderer —— 原版 VATGPUPlayer 在这里会直接抛异常；"
                             + "组件已自动关掉（没有它就没有任何材质可推）。");
                enabled = false;
                return;
            }

            _mats = r.materials;   // 原版 `Renderer.GetMaterialArray()` = `.materials`（**实例材质**，不是 shared）

            _idPositionsTex        = Shader.PropertyToID("_PositionsTex");
            _idPositionsTexB       = Shader.PropertyToID("_PositionsTexB");
            _idRotationsTex        = Shader.PropertyToID("_RotationsTex");
            _idBoundsCenter        = Shader.PropertyToID("_BoundsCenter");
            _idBoundsExtents       = Shader.PropertyToID("_BoundsExtents");
            _idStartBoundsCenter   = Shader.PropertyToID("_StartBoundsCenter");
            _idStartBoundsExtents  = Shader.PropertyToID("_StartBoundsExtents");
            _idPartsCount          = Shader.PropertyToID("_PartsCount");
            _idPartsIdsInUV3       = Shader.PropertyToID("_PartsIdsInUV3");
            _idState               = Shader.PropertyToID("_State");
            _idHighPrecisionMode   = Shader.PropertyToID("_HighPrecisionMode");
        }

        void OnEnable()
        {
            _enableTime = Time.time;
            if (_stateClip == null)
            {
                // 原版没有这个引用（驱动器不认识 Animation）—— 我们是去找**父链**上那条。
                _stateClip = GetComponentInParent<Animation>();
            }
            // 原版 `PlayRecording()` 在本地内容里 0 调用者（见文件头）⇒ 默认不自动开自走路；
            // 只有「没有 clip 可用、且允许兜底」时才走 `_stateCurveKeys`。
            if (_useBuiltInCurveWhenNoClip && !ClipIsUsable())
            {
                if (!_warned)
                {
                    _warned = true;
                    // 出声，不静默：这件 mesh 还不会显示 —— 开 `MeshRenderer.m_Enabled` 的是原版那条 clip。
                    Debug.LogWarning($"[VatDriver] `{name}` 拿不到 legacy clip（原版这里指 "
                                   + "`Vanguard Frame Animation`，父物件 `VanguardIdleEffect` 的 Animation 上）"
                                   + "⇒ `_state` 改由**内置曲线** 0.07→1.0 / 1.7916666s 驱动。"
                                   + "⚠️ 内置曲线**不管** `MeshRenderer.m_Enabled`（那是 clip 的 `m_Enabled` 曲线，"
                                   + "t=0.0416667 由 0→1）⇒ 在它修好之前这件 mesh 仍然不显示。");
                }
            }
        }

        void OnDisable()
        {
            _playing = false;
        }

        /// <summary>原版 `PlayRecording()`（`+0x64 = 1`、`+0x68 = Time.time`）。
        /// 本地内容里没有调用者（死代码），保留只为等价。</summary>
        public void PlayRecording()
        {
            _playing = true;
            _startTime = Time.time;
        }

        /// <summary>原版 `SetAnimation(VATAnimation)`（写 `+0x20`）。</summary>
        public void SetAnimation(WarpforgeVatData a) { _vatAnimation = a; }

        /// <summary>原版 `IsThereAnimation()`。</summary>
        public bool IsThereAnimation() { return _vatAnimation != null; }

        /// <summary>原版 `UpdateMaterials(bool shared)`。`shared = true` 时取 `sharedMaterials`
        /// （原版那两个分支的差别只在这儿）。</summary>
        public void UpdateMaterials(bool shared)
        {
            var r = GetComponent<Renderer>();
            if (r == null) { Debug.LogError($"[VatDriver] `{name}` UpdateMaterials：没有 Renderer。"); return; }
            _mats = shared ? r.sharedMaterials : r.materials;
        }

        /// <summary>原版 `UpdateAnimation()` —— 逐行照抄反编译（`StoryProgramming.VATGPUPlayer__UpdateAnimation.c:6-25`）。</summary>
        void UpdateAnimation()
        {
            float start = _startTime;
            float end   = _vatAnimation.Duration / _animationSpeed + start;
            float t     = (end == start) ? 0f : (Time.time - start) / (end - start);
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;           // 原版夹到 `DAT_1834b2bb8` = 1.0f（已实读）
            _state = t;
            if (Time.time > end) _playing = false;
        }

        /// <summary>**原版没有这一条** —— 它那条 legacy clip 在我们这儿是断链
        /// （`VanguardIdleEffect.prefab:59081` 的 `guid: 000…0`）时的等价兜底：
        /// 用 clip 的两个键（`t=0→0.07`、`t=1.7916666→1.0`）线性求值。判据见文件头。</summary>
        float EvalStateCurve(float time)
        {
            var k = _stateCurveKeys;
            if (k == null || k.Length == 0) return 1f;
            if (time <= k[0].x) return k[0].y;
            if (time >= k[k.Length - 1].x) return k[k.Length - 1].y;
            for (int i = 0; i + 1 < k.Length; i++)
            {
                if (time <= k[i + 1].x)
                {
                    float span = k[i + 1].x - k[i].x;
                    float f = (span <= 0f) ? 0f : (time - k[i].x) / span;
                    return Mathf.Lerp(k[i].y, k[i + 1].y, f);
                }
            }
            return k[k.Length - 1].y;
        }

        bool ClipIsUsable()
        {
            try { return _stateClip != null && _stateClip.GetClipCount() > 0; }
            catch { return false; }
        }

        /// <summary>原版 `Update`（`__Update.c:16-32`）：`_vatAnimation == null` 直接 return、
        /// 否则**每帧**都 `SendDataToRenderer()`。</summary>
        void Update()
        {
            if (_vatAnimation == null) return;

            if (_useBuiltInCurveWhenNoClip && !ClipIsUsable())
            {
                // 兜底路（clip 断链时）：别的曲面都让给 clip。
                _state = Mathf.Clamp01(EvalStateCurve(Time.time - _enableTime));
            }
            else if (_playing)
            {
                UpdateAnimation();   // 原版那条自走路（本地内容里没调用者，保留）
            }

            SendDataToRenderer();
        }

        /// <summary>原版 `SendDataToRenderer()`（`__SendDataToRenderer.c:19-91`）：**逐份材质各推一遍**。</summary>
        public void SendDataToRenderer()
        {
            if (_mats == null || _mats.Length == 0 || _vatAnimation == null) return;
            var v = _vatAnimation;
            var bc = v.BoundsCenter;  var be = v.BoundsExtents;
            var sc = v.StartBoundsCenter; var se = v.StartBoundsExtents;
            for (int i = 0; i < _mats.Length; i++)
            {
                var m = _mats[i];
                if (m == null) continue;

                if (v.PositionsTex  != null) m.SetTexture(_idPositionsTex,  v.PositionsTex);
                if (v.RotationsTex  != null) m.SetTexture(_idRotationsTex,  v.RotationsTex);
                if (v.HighPrecisionPositionMode && v.PositionsTexB != null)
                    m.SetTexture(_idPositionsTexB, v.PositionsTexB);

                m.SetInt(_idHighPrecisionMode, v.HighPrecisionPositionMode ? 1 : 0);
                m.SetInt(_idPartsIdsInUV3,     v.PartsIdsInUV3 ? 1 : 0);
                m.SetFloat(_idState, _state);                       // ← 原版 `:53-54`：Material.SetFloat(_stateId, _state)
                m.SetInt(_idPartsCount, v.PartsCount);

                // 原版四个 vec4 的 `.w` 都是 0（`local_98._12_4_ = 0`），不是 1
                m.SetVector(_idBoundsCenter,       new Vector4(bc.x, bc.y, bc.z, 0f));
                m.SetVector(_idBoundsExtents,      new Vector4(be.x, be.y, be.z, 0f));
                m.SetVector(_idStartBoundsCenter,  new Vector4(sc.x, sc.y, sc.z, 0f));
                m.SetVector(_idStartBoundsExtents, new Vector4(se.x, se.y, se.z, 0f));
            }
        }
    }
}
