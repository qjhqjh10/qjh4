// BoosterPackVisualItem.cs — 原版 `BoosterPackVisualItem`（开包窗中心那件**独立 3D 卡包实物**）的复刻宿主
//   （2026-10-29 · 第九十八会话 · 执行代理 `B98_A5` · 账 `锻造:23`）
//
// ============================ 它是什么（不是 UI 节点） ============================
// 原版 `BoosterPackOpenWindow.Initialize` 里 **`Instantiate(BoosterPackVisualItem prefab, boosterPackAnchor)`**
// 把一件**独立的 3D 物件**塞进锚点，紧接着调它的 `Initialize(null, this)`：
//   判据 = `d:/2/tools/decomp_full/BoosterPackOpenWindow__Initialize.c`
//          → `UnityEngine_Object__Instantiate<object>(param_3 /*prefab*/, +0xa8 /*boosterPackAnchor*/, …)`
//          → 存进 `+0xd0` → `BoosterPackVisualItem__Initialize(inst, **0**, this, 0)`
//   `boosterPackAnchor` 是谁：窗口 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_9012570135841684515.json`
//   的 `boosterPackAnchor.m_PathID = -3486917825874871261` = RectTransform
//   `RectTransform_-3486917825874871261.json` = GameObject **`Booster Animation Parent`**
//   （`lp.z = −202` · `m_LocalScale = 0.87626` · 100×100）⇒ 我们那棵同名节点就是它（`Shell/BoosterPackOpenWindow.cs`）。
//   它**不在这扇窗的 prefab 树里**（`menu_dump --depth 6` 现读：`Booster Animation Parent ▸ Cards ▸ 5 张卡`，没有别的）。
//
// ============================ 判据（全部可复查） ============================
//  ① **类桩（字段表）** → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BoosterPackVisualItem.cs`
//      `Renderer myRenderer` · `TextMeshPro[] armyTexts` · `AudioCue openBoosterPack` ·
//      `[TermsPopup] string boosterName` · `OverwriteCameraShakePreset[] shakePresets` · `BoosterPackOpenWindow boosterPackOpenWindow`
//  ② **方法体（第一权威）** → `d:/2/tools/decomp_full/BoosterPackVisualItem__Initialize.c`（71 行，逐句对照见 <see cref="Initialize"/>）
//      · `__StartCardAnimationEvent.c`（24 行：转调宿主窗的 `BoosterPackOpen()`）
//      · `__.ctor.c`（14 行：只给 `boosterName` 赋 I2 默认值 —— 没有别的初始化）
//  ③ **实例数据** → `d:/2/新解包资源/assets_full/bundle_boosterpacks_assets_all/MonoBehaviour/*`，
//      `m_Script.m_PathID = -1105334708447906859` 的**共 17 份**（2026-10-29 现数）。
//     17 份里 `shakePresets` **逐字节相同**（`json.dumps(sort_keys=True)` 只有 1 种），
//     `boosterName` = `Armies/<阵营>_short` 那一族（另有 `MenuShop/Alpha/AllArmies` 一份）；逐条见 §表。
//  ④ **prefab 本体** → `StreamingAssets/WarpforgeVFX/wf_boosters_extra.bundle` 的 17 棵
//     （取件口 = `CardPresentation/BoosterPackAssets.cs`，它同时是 shader / 材质 / 贴图那条链的唯一入口）。
//  ⑤ **它自己那条 2 s clip（关键）** → 同一个包里 `AnimationClip_7302051762796698856`（`Booster Open Standard`）/
//     `AnimationClip_5156805757413551419`（`Booster Open Sororitas`），两者都是 **legacy**、
//     prefab 根上的 `Animation` 组件 `m_PlayAutomatically = true`：
//       · `material._ExplodeRange` 1.0 → −0.1（0.67 s 起）、`material._InflateDistance` 0 → 0.356（1.05 s 峰值）
//       · `Army Text` / `Army Text 2` 的 `m_fontColor.a` → 0（0.6–0.75 s 淡出）
//       · `Booster Pack Particles` 0.67 s 激活、`ParticleExplosion` 1.98 s 激活、`Force` 2.0 s 一帧
//       · **t = 2.0 的 AnimationEvent = `StartCardAnimationEvent`**（= 宿主窗的开包动画那一拍）
//     ⇒ **炸开 / 粒子 / 阵营字淡出 / 2 s 那一拍，全在这条 clip 里** —— 我们只要**把原版 prefab 实例化**就都有，
//       ⛔ 不要自己再写一遍（重写一遍 = 两处写同一条规则）。
//
// ============================ 🔴 为什么这个类必须是【全局命名空间】 ============================
// bundle 里那 17 份 MB 的 `m_Script` 指着一份 `MonoScript`，Unity 按「程序集 + 命名空间 + 类名」回填。
// 原版类是**全局命名空间**里的 `BoosterPackVisualItem`（类桩第一行就是 `public class BoosterPackVisualItem`，
// 没有 `namespace`），而本工程 `Assets/CardPresentation/` 下**没有任何 `.asmdef`**（2026-10-29 现查：0 份）
// ⇒ 本类同样落在 `Assembly-CSharp`。**名字对得上，bundle 里那份 MB 就能回填到本类**
// （序列化字段按名字对：`myRenderer` / `armyTexts` / `boosterName` 会直接带上 prefab 里的值）。
// ⛔ **别把它挪进 `namespace CardPresentation`** —— 挪了就对不上，那条 AnimationEvent 会打
// 「`StartCardAnimationEvent` has no receiver」，炸包动画**照样播**（clip 在包里）但**开包动画永不开始**。
//
// ============================ ⚠️ 与「不在本类」的那条（账 `锻造:24`） ============================
// 原版 `StartCardAnimationEvent()` 的体 = **转发给宿主窗**（`AnimationEvent` 打的就是它）：
//   `BoosterPackVisualItem__StartCardAnimationEvent.c` → `this.boosterPackOpenWindow(+0x48).BoosterPackOpen()`
// 而 `BoosterPackOpen()` = `windowAnimation.Play()` + `backgroundAnimation.Play("Booster Window - Background Shake On Open")`
// ⇒ 本类**只转调** `BoosterPackOpenWindow.StartOpenAnimations()`（那一份实现在窗口里，⛔ 这里不写第二份）。
using System.Text;
using TMPro;
using UnityEngine;
using CardPresentation;

/// <summary>原版 `BoosterPackVisualItem` —— 开包窗中心那件独立的 3D 卡包实物。见文件头。</summary>
public class BoosterPackVisualItem : MonoBehaviour
{
    // ---- 原版序列化字段（**名字逐字照类桩** —— bundle 里那份 MB 靠名字回填，⛔ 别改名）----
    [SerializeField] Renderer myRenderer;
    [SerializeField] TextMeshPro[] armyTexts;
    /// <summary>原版是 `AudioCue` 引用；本工程的 cue 是**名字**（唯一一张表 = `Shell/WindowSounds.cs`）。
    /// 值由 <see cref="Bind"/> 用 `WindowSounds.BoosterOpenCue` 填（那张映射表的判据：prefab 上
    /// `boosterName ↔ openBoosterPack` 的配对，逐颗实读过 17 份 MB）。</summary>
    [SerializeField] string openBoosterPack;
    /// <summary>原版 `[TermsPopup] string`：I2 词条键（`Armies/<阵营>_short` / `MenuShop/Alpha/AllArmies`）。</summary>
    [SerializeField] string boosterName;
    [SerializeField] BoosterShakePreset[] shakePresets;

    BoosterPackOpenWindow boosterPackOpenWindow;

    // ---- 自检读口 ----
    /// <summary>`Initialize` 里最终写进 `armyTexts[]` 的那串（`localizedArmyText` 空则 = `Loc.T(boosterName)`）。</summary>
    public string ArmyText { get; private set; }
    /// <summary>最近一次 `Initialize` 拿到的宿主窗。</summary>
    public BoosterPackOpenWindow Window { get { return boosterPackOpenWindow; } }
    /// <summary>`Initialize` 被调过几次（自检：一扇窗只该初始化它一次）。</summary>
    public int InitializeCount { get; private set; }
    /// <summary>真正交给 `WindowSounds` 的 cue 名（`null` = 没播 —— 例如没有音频系统那一档）。</summary>
    public string PlayedCue { get; private set; }
    /// <summary>真正递交给宿主窗的抖动请求（自检读）。</summary>
    public readonly System.Collections.Generic.List<BoosterShakePreset> PlayedShakes =
        new System.Collections.Generic.List<BoosterShakePreset>();

    /// <summary>原版 `myRenderer`（自检读口）。</summary>
    public Renderer Renderer { get { return myRenderer; } }
    /// <summary>原版 `armyTexts`（自检读口；`null` = 没绑上）。</summary>
    public TextMeshPro[] ArmyTexts { get { return armyTexts; } }
    /// <summary>原版 `boosterName`（自检读口）。</summary>
    public string BoosterName { get { return boosterName; } }
    /// <summary>原版 `openBoosterPack`（自检读口）。</summary>
    public string OpenBoosterPack { get { return openBoosterPack; } }
    /// <summary>原版 `shakePresets`（自检读口）。</summary>
    public BoosterShakePreset[] ShakePresets { get { return shakePresets; } }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  原版 `BoosterPackVisualItem.Initialize(string localizedArmyText, BoosterPackOpenWindow boosterPackOpenWindow)`
    //  逐句对照（`d:/2/tools/decomp_full/BoosterPackVisualItem__Initialize.c`，71 行；行号是该 .c 的行）：
    //
    //  ┌ .c:20   `if (String.IsNullOrWhiteSpace(param_2))`                        → 本文件 ①
    //  │ .c:22-27 `param_2 = LocalizationManager.GetTranslation(this.boosterName)` → 本文件 ①（`Loc.T`）
    //  │ .c:29-30 `this.boosterPackOpenWindow(+0x48) = param_3`（带写屏障）        → 本文件 ②
    //  │ .c:31-42 `for (i < armyTexts.Length(+0x18)) armyTexts[i].text = param_2`  → 本文件 ③
    //  │          （`+0x28` = armyTexts · `+0x20` = 元素区 · 虚表 `+0x558` = `TMP_Text.set_text`）
    //  │ .c:43-48 `if (SoundManager.Instance != null)`                            → 本文件 ④（见那里的注释）
    //  │ .c:49-53 `SoundManager.Play2D(openBoosterPack(+0x30), **3** = MixerType.Jingles)` → 本文件 ④
    //  │ .c:54-66 `for (j < shakePresets.Length(+0x18)) OverwriteCameraShakePreset.Play(preset[j], 0)` → 本文件 ⑤
    //  └ 空数组的两处 NRE 兜底（`.c` 的 LAB_…126 / …120 = `NullReferenceException` / `IndexOutOfRange`）
    //    —— 那是 C# 数组访问的天然语义，⛔ 我们不**伪造**抛异常；改成**出声**（红线：不许静默失败）。
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    public void Initialize(string localizedArmyText, BoosterPackOpenWindow boosterPackOpenWindow)
    {
        InitializeCount++;

        // ① 空白（含 null / "" / 纯空格）⇒ 取 `boosterName` 的 I18 译文。
        //    ⚠️ 原版调的是 `LocalizationManager.GetTranslation(boosterName)`（**一个实参**那版），
        //    本工程的对应口 = `Loc.T`（三档兜底：手写 Table → bulk → 官方 13 语全表）。
        if (string.IsNullOrWhiteSpace(localizedArmyText))
            localizedArmyText = Loc.T(boosterName);
        ArmyText = localizedArmyText;

        // ② 存宿主窗（原版 `this.boosterPackOpenWindow = 入参`，带写屏障）。
        this.boosterPackOpenWindow = boosterPackOpenWindow;

        // ③ 两颗世界空间 TMP 都写上同一串字（原版是 `for` 全数组写，不是「只写第一颗」）。
        if (armyTexts == null || armyTexts.Length == 0)
        {
            Debug.LogWarning("[BoosterPack] 卡包实物上没有 `armyTexts`（原版 = 2 颗**世界空间** `TextMeshPro`："
                             + "`Army Text` / `Army Text 2`）⇒ 包面上**不会**出现阵营字。"
                             + " 判据 → `bundles_boosterpacks_assets_all` 的 17 份 MB（`armyTexts[2]`）。");
        }
        else
        {
            for (int i = 0; i < armyTexts.Length; i++)
            {
                var t = armyTexts[i];
                if (t == null)
                {
                    // 原版这一档会 NRE（数组元素是 null）⇒ 我们不抛，出声就够
                    Debug.LogWarning("[BoosterPack] `armyTexts[" + i + "]` 是 null ⇒ 这一颗不写字（原版这里会抛 NRE）");
                    continue;
                }
                t.text = localizedArmyText;
            }
        }

        // ④ 原版这里裹着一层 `if (SoundManager.Instance != null)`。
        //    🔴 **本工程没有这个单例**（音频是静态门面 `WarpforgeAudio`，永远是「在」的）
        //    ⇒ 该闸**没有对应物**；⛔ 不去伪造一个（伪造 = 自己设计一个原版没有的开关）。
        //    结构照原版留（声音在前、抖在后），只是少了那一层 null 闸。
        PlayedCue = openBoosterPack;
        WindowSounds.PlayJingles(openBoosterPack,
            "卡包实物 `BoosterPackVisualItem.Initialize`（原版 `SoundManager.Play2D(openBoosterPack, MixerType.Jingles)`）");

        // ⑤ 逐条 `OverwriteCameraShakePreset.Play()`。
        //    ⚠️ **等价物**：菜单层**没有可抖的相机** ⇒ 抖的是卡容器（本工程既有口径，见窗口的 `_bursts` /
        //    `StepBgShake` 两处注释）；⛔ 这里不另写一套公式，只把「幅度 / 延迟 / 衰减」三个**原版值**递过去。
        if (shakePresets == null) return;
        for (int i = 0; i < shakePresets.Length; i++)
        {
            var p = shakePresets[i];
            PlayedShakes.Add(p);
            if (boosterPackOpenWindow != null) boosterPackOpenWindow.PlayCameraShake(p.amplitude, p.delay, p.decayTime);
        }
    }

    /// <summary>原版 `StartCardAnimationEvent()`（`bundle` 里那条 2 s clip 在 **t=2.0** 打的 AnimationEvent）。
    /// 体 = 转发给宿主窗（`BoosterPackVisualItem__StartCardAnimationEvent.c`：`window.BoosterPackOpen()`）。
    /// ⛔ 这里**不写第二份**动画逻辑 —— 实现在 <see cref="BoosterPackOpenWindow.StartOpenAnimations"/>。</summary>
    public void StartCardAnimationEvent()
    {
        if (boosterPackOpenWindow == null)
        {
            Debug.LogWarning("[BoosterPack] `StartCardAnimationEvent` 打进来时 `boosterPackOpenWindow` 是 null"
                             + "（原版这里会 NRE）⇒ 开包动画不会开始");
            return;
        }
        boosterPackOpenWindow.StartOpenAnimations();
    }

    /// <summary>把原版 prefab 上**已经序列化**的字段补进本类（原版这些值就存 prefab 里，没有「绑定」这一步）。
    /// 取件 / 回填都在 <see cref="BoosterPackOpenWindow"/> 的 `BuildVisualItemLayer` 里；
    /// 这里只做「**不覆盖已经有值的那几格**」—— 万一 bundle 里的 MB 真的回填到了本类（见文件头那条命名空间说明），
    /// `myRenderer` / `armyTexts` / `boosterName` 就已经是原版值了，不该被我们的查找结果盖掉。
    /// ⚠️ `openBoosterPack` / `shakePresets` **一定覆盖**：原版那两格是 `AudioCue` 引用与
    /// `OverwriteCameraShakePreset[]`，与本类的 `string` / `BoosterShakePreset[]` 类型不同、回填不了。</summary>
    public void Bind(Renderer renderer, TextMeshPro[] texts, string cue, string termKey, BoosterShakePreset[] shakes)
    {
        if (myRenderer == null) myRenderer = renderer;
        if (armyTexts == null || armyTexts.Length == 0) armyTexts = texts;
        if (string.IsNullOrEmpty(boosterName)) boosterName = termKey;
        openBoosterPack = cue;
        shakePresets = shakes;
    }

    /// <summary>自检 / 日志用的一行摘要。</summary>
    public string Dump()
    {
        var sb = new StringBuilder();
        sb.Append("BoosterPackVisualItem[").Append(boosterName).Append("] cue=").Append(openBoosterPack)
          .Append(" · armyText=").Append(ArmyText)
          .Append(" · texts=").Append(armyTexts == null ? 0 : armyTexts.Length)
          .Append(" · renderer=").Append(myRenderer != null ? myRenderer.name : "<null>")
          .Append(" · shakes=").Append(shakePresets == null ? 0 : shakePresets.Length)
          .Append(" · init=").Append(InitializeCount);
        return sb.ToString();
    }
}

/// <summary>一条震屏参数 —— **逐字段照原版 `OverwriteCameraShakePreset` 的序列化形状**（17 份 MB 里那份，
/// `bundle_boosterpacks_assets_all/MonoBehaviour/MonoBehaviour_-1083784694647810962.json` 的 `shakePresets[]`）。
/// <para>⚠️ 原版还有一格 `presetSO`（指向 `CameraShakePreset` SO，pid −4069090446645055156，在**别的包**里、
/// 本机取不到）—— `overwrite*` 为 1 的那几格由 MB 里的值覆盖，没覆盖的那几格（`attackTime` · `direction`）
/// 用的就是 SO 的缺省值 ⇒ **那两格查不到**，本结构里保留原值（`attackTime` 两条都是 0、`direction` 两条都没覆盖），
/// ⛔ 不猜一个数填进去。</para></summary>
[System.Serializable]
public struct BoosterShakePreset
{
    public float delay;
    public bool overwriteSustainTime;
    public float sustainTime;
    public bool overwriteAmplitude;
    public float amplitude;
    public bool overwriteFrequency;
    public float frequency;
    public bool overwriteDirection;
    public Vector3 direction;
    public bool overwriteAttackTime;
    public float attackTime;
    public bool overwriteDecayTime;
    public float decayTime;
}
