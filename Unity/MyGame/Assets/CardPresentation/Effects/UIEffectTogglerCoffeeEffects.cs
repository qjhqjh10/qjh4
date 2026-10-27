// UIEffectTogglerCoffeeEffects.cs — 原版 `UIEffectTogglerCoffeeEffects` 的 1:1 端口（账 455 / J-12）
//
// ============================ 判据（全是本机实读） ============================
// 原版类：`public class UIEffectTogglerCoffeeEffects : UIEffectTogglerBase`（`d:/2/tools/il2cpp_out/dump.cs:114051`），
//   **字段只有一个**：`[SerializeField] private UIEffect[] uiEffects; // 0x28`（`UIEffect` = **Coffee.UIEffect.dll**
//   里的 uGUI 材质滤镜组件，`Coffee.UIEffects.UIEffect`，TypeDefIndex **20378**）。
// 四个成员（`.c` + `dump.cs` 的 RVA 双证）：
//   · `Toggle(bool option)` —— RVA `0x86F2E0`，**真身**（Ghidra 反编译，非 ILSpy）：
//       `decomp_full/UIEffectTogglerCoffeeEffects__Toggle.c:20-45` 逐句 =
//         `*(char*)(this + 0x21) = option;`                      // = targetStateEnabled = option
//         `for (i = 0; i < uiEffects.Count; i++) {`
//           `if (UnityEngine.Object.op_Implicit(uiEffects[i], 0)) {`   // = `uiEffects[i] != null`（Unity 假 null）
//             `UnityEngine.Behaviour.set_enabled(uiEffects[i], option);`
//             `if (option) { tw = uiEffects[i].GetComponent<UIEffectTweener>();
//                            if (tw) Coffee_UIEffects_UIEffectTweener__Play(tw); }`
//       ⇒ **只有「开关 + 开时播一次」两句语义**。
//       ⚠️ **`duration` 一个字节都没用**（真身形参表只有 `(this, option)`，那个 0.3f 是 C# 默认值）。
//       ⚠️ **本类不碰 `deactivateOnDisable`**（真身里没有 `SetActive` 那一跳）。
//   · `DoUpdate(float, float)` —— RVA **`0x4B33B0`**（= 全局共享空方法桩，机器码 `c2 00 00` = `ret 0`）⇒ **空方法**；
//   · `AnimationFinished()`    —— RVA 同样是 **`0x4B33B0`** ⇒ **空方法**；
//   · `.ctor` —— 只写 `+0x20 = 1`（= 基类 `deactivateOnDisable` 的出厂值）+ `Behaviour..ctor()`。
//
// ============================ 🔴 这一版改了什么（2026-10-27 `B9_W16SHINE`） ============================
// **不再挂 Coffee 的组件**（`Coffee.UIEffects.UIEffect` / `UIEffectTweener` 只能挂 uGUI `Graphic`，我们的
//   UI 是世界空间 `ImageQuad` + `MeshRenderer` ⇒ 原版那 7 个实例在这里**没有可挂的载体**，上一版只做到
//   「类型接上、视觉为零」）。本版换成**我们自己的一层等价实现** = `WildcardShineLayer`（见文件尾）：
//   它把 `Resources/Shaders/WildcardShine.shader`（= 原版 `Hidden/UI/Default (UIEffect)` 的 `TRANSITION_SHINY` 变体的
//   忠实移植）挂到那颗 `ImageQuad` 的材质上，参数逐实例照抄（判据 = `报告/B9_W5COF_Coffee搬运.md` §2·5/§2·6）。
//   字段从 `List<UIEffect>` 换成 `List<WildcardShineLayer>` —— **同一个语义槽位**（本节点上要开关的那几件）。
//   ⚠️ `duration` 仍然不接（原版真身没用它；补间时长长在各自的 tweener 上，我们的 `Params.duration`）。
//   ⚠️ `d:/2` 里那份 `Coffee.UIEffect.dll` **不再被本类引用**（文件仍在 `Assets/ThirdParty/`，别删 —— 见报告）。
//
// ============================ 🔴 7 个实例 × 我们这边挂在哪个节点（现状，如实记） ============================
//   A/E `Ranked Army Selector Container V2/{Featured Icon, Army Icon}` —— 原版 `uiEffects` **空数组** ⇒ 什么都不做；
//   C `Campaign Army Item Button/Icon`   ✅ 已接（`Shell/CampaignTab.cs` 的 `BuildArmyItems` → `Icon`）；
//   G `Forge Army Item Button/Icon`      ✅ 已接（`Shell/ForgeTab.cs` 的 `BuildArmyItems` → `Icon`）；
//   F `Deck Editing Menu/…/Footer/Done`  ✅ 已接（`Deck/DeckRuntime.cs` 的 `foot_done`，Loop 那档）；
//   B `Army Item Button No Arrow Variant/Icon` · D `Army Item Button/Icon` ⇒ ⚠️ **宿主未定**：这两个 prefab
//     在我们工程里对应哪颗节点**没查到**（`Cell_fac_*` 是无箭头变体的候选，但原版收藏左抽屉那格的父链没读全
//     ⇒ ⛔ 不猜）。层本身已就绪，定位到节点后一行就能挂。
//   ⚠️ `UIEffectTogglerController` 那一层（`DeckEditingWindow.UpdateDoneButton` / `ForgeRewardSelector.ToggleCollectReward`
//     / `PrebuiltPackUI.*` 都调它）的 `Update` 只喂 `DoUpdate`，而本类 `DoUpdate` 是空桩 ⇒ 画面完全由 `Toggle` 里那次
//     `Play()` 决定；我们这几个宿主**不需要**那个 controller，直接调 `Toggle(bool)` 即同义。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `UIEffectTogglerCoffeeEffects` 的 1:1 端口：开/关一批「Shiny 扫光」层，
    /// 并在「开」时让它播一次。
    /// <para>⛔ **别给 `DoUpdate` / `AnimationFinished` 加补间** —— 原版那两个是 `RVA 0x4B33B0` 的空桩，
    /// 这一档不参与 `UIEffectTogglerController` 的补间（那是 `UIEffectTogglerImage` / `CanvasGroup` 那一族的事）。</para></summary>
    public class UIEffectTogglerCoffeeEffects : UIEffectTogglerBase
    {
        /// <summary>= 原版 `UIEffect[] uiEffects(+0x28)` 的等价槽位：**本节点上**那几层扫光（不递归子件）。
        /// <para>🔴 原版 7 个实例里 **2 个是空数组**（`Featured Icon` / `Army Icon`）⇒ **空 = 正常**，
        /// ⛔ 别把「空」当成「没复刻」去报警。</para>
        /// <para>⚠️ 载体差异（如实记）：原版这里是 Coffee 的 `UIEffect` 组件，我们换成自己那层等价物。</para></summary>
        public List<WildcardShineLayer> shineLayers = new List<WildcardShineLayer>();

        /// <summary>已经查过 shader 了（`Shader.Find` 只查一次，别每帧调）。</summary>
        static bool _shaderChecked;

        public override void Toggle(bool option, float duration = 0.3f)
        {
            // ⚠️ `duration` 原版**没用**（真身形参表里只有 this/option）⇒ 这里也不接成补间。
            targetStateEnabled = option;

            if (shineLayers != null)
            {
                for (int i = 0; i < shineLayers.Count; i++)
                {
                    var layer = shineLayers[i];
                    if (layer == null) continue;   // 原版：`UnityEngine.Object.op_Implicit(uiEffects[i], 0)`
                    layer.SetEnabled(option);      // 原版：`UnityEngine.Behaviour.set_enabled(uiEffects[i], option)`
                    if (!option) continue;
                    layer.Play();                  // 原版：`if (tw) Coffee_UIEffects_UIEffectTweener__Play(tw)`
                }
            }

            if (option) WarnIfVisualBodyMissing();
        }

        /// <summary>原版 = 空桩（`RVA 0x4B33B0`）⇒ 这里也留空。⛔ 别往这里加补间。
        /// （补间长在 `WildcardShineLayer` 自己的 `Tick` 上，由 `PlayerLoop` 泵推 —— 与原版 `UIEffectTweener` 同形。）</summary>
        public override void DoUpdate(float currentTime, float normalizedTime) { }

        /// <summary>原版 = 空桩（`RVA 0x4B33B0`）⇒ 这里也留空。</summary>
        public override void AnimationFinished() { }

        /// <summary>「我们自己那只 shader 不在工程里」**一次性出声**（`Shader.Find` 判据）。
        /// <para>🔴 这是**如实报告缺口**：`CardPresentation/WildcardShine` 不在 `Resources/` 下，player 包里要
        /// 靠 `ProjectSettings/GraphicsSettings.asset` 的 AlwaysIncluded —— 取不到时 `WildcardShineLayer.Attach`
        /// 会返回 null ⇒ 开关照跑、画面**看不出来**。不吭声就成了静默失败。</para></summary>
        void WarnIfVisualBodyMissing()
        {
            if (_shaderChecked) return;
            _shaderChecked = true;
            if (Shader.Find("CardPresentation/WildcardShine") != null) return;
            Debug.LogWarning("[UIEffectTogglerCoffeeEffects] 工程里取不到 shader `CardPresentation/WildcardShine`"
                           + "（它不在 `Resources/` ⇒ player 包里要靠 GraphicsSettings 的 AlwaysIncluded）"
                           + " ⇒ 扫光层建不出来，**滤镜视觉本体不出现**（开关与 `Play()` 语义仍然照原版执行）。"
                           + "缺什么 / 怎么补见 `资料/完全复刻/报告/B9_W16SHINE_扫光统一.md`。");
        }
    }

    // ======================================================================================
    //  WildcardShineLayer —— 原版 `Coffee.UIEffects.UIEffect`(Transition = Shiny) +
    //  `UIEffectTweener` 的**运行时等价物**（账 455 / 收藏:348 / 战役的 `shinyEffect`）
    //
    //  判据链（全部一手，⛔ 不是照截图猜）：
    //   ① shader = `Resources/Shaders/WildcardShine.shader`（= `TRANSITION_SHINY` 那个变体的移植，逐条式子带出处）；
    //   ② 参数 = `报告/B9_W5COF_Coffee搬运.md` §2·6 的**逐实例表**（5 个实例只有 `m_TransitionColor` 与
    //      `WrapMode` 两处不同）；收藏窗那颗 = `报告/B9_W7COLL_收藏与卡面剩余C.md` §二·K-13 的现读值；
    //   ③ 掩码坐标 = **本件框内的归一化坐标**（原版 `_RootViewMatrix`，见 `.shader` 文件头 ④）；
    //   ④ 播法 = `UIEffectTweener`：`m_Duration` 秒、**线性 0→1**、`m_WrapMode 0(Once)/1(Loop)`、
    //      `m_Delay 0`、`m_PlayOnEnable 0`（⇒ 由调用方显式 `Play()`，进场不自播）。
    //
    //  🔶 自己设计（非原版）：**θ 之后那一步的精确算式**（统一 `1/max(W,H)` 归一 + 绕框心旋转）·
    //     为什么必须自己定：Coffee 的 `UIEffectContext.UpdateViewMatrix` 方法体在 Cpp2IL 重建的 dll 里
    //     局部变量全被打散（`ilspycmd -il` 只剩 `IL_xxxx` 与 `NoteDecompilerIssue`）⇒ 逐句原文拿不到；
    //     能坐实的只有「坐标空间 = 自己的 Rect」（`transitionRoot` = `m_CustomRoot ?? 自己`，m_CustomRoot 全 null）、
    //     「绕 pivot 转」、「keepAspectRatio 会动那两个 1/W、1/H 比例」、「结果进贴图前 clamp(0,1)」四件。
    //     取统一 1/max 的理由：另两种取法会把框的一半 clamp 成死白（`|v'|>1` ⇒ alpha=1 一整片），
    //     与「Shiny 是一条扫过去的带」直接矛盾。· 调度台 2026-10-27 裁定（沿用 W7COLL 的 🔶 口径）。
    //
    //  ⚠️ **材质**：**每层一份**（不是每块一份）—— 掩码按世界位置算 ⇒ 九宫格 9 块共用一层的材质仍然连续
    //     （这正是原版「一颗 Image 一张 9 格网格」的形状）。材质**不缓存共享**：`ImageQuad.SetTexture`
    //     （悬停换图）会**直接改 `sharedMaterial.mainTexture`**，共享会把别人的图一起改掉（静默）。
    //     泄漏管理 = `StepAll` 里**剪掉 Quad 全没了的层并销毁它的材质**（页面重建 ⇒ 旧层下一帧自动回收）。
    // ======================================================================================
    /// <summary>一层「Shiny 扫光」。见上面那整段判据 / 自己设计的说明。</summary>
    public sealed class WildcardShineLayer
    {
        /// <summary>原版 `UIEffect` 的 Transition 族 + `UIEffectTweener` 的时长/循环那两格。</summary>
        public struct Params
        {
            public float rotation;      // `m_TransitionRotation`（度）
            public float width;         // `m_TransitionWidth`
            public float softness;      // `m_TransitionSoftness`
            public float duration;      // `UIEffectTweener.m_Duration`（秒）
            public Color color;         // `m_TransitionColor`
            public float colorFilter;   // `m_TransitionColorFilter`（6 = MultiplyAdditive · 0 = 不染）
            public float colorGlow;     // `m_TransitionColorGlow`
            public bool reverse;        // `m_TransitionReverse`
            public bool loop;           // `m_WrapMode`（1 = Loop）

            /// <summary>5 个「按钮/图标」实例（B/C/D/G，以及收藏窗那颗 0.75 s 的姐妹档）共用的那一组：
            /// 120° / 0.25 / 0.5 / 白 / filter 6 / Once —— 判据 = `B9_W5COF` §2·6 逐字段表。</summary>
            public static Params Icon { get { return Make(0.25f, 1f, Color.white, false); } }
            /// <summary>`Deck Editing Menu ▸ Footer ▸ Done`（F）：同一组参数，**只有两处不同** ——
            /// `m_TransitionColor = (3.4822,3.4822,3.4822,1)`、`m_WrapMode = 1 (Loop)`（一直扫）。</summary>
            public static Params DoneButton { get { return Make(0.25f, 1f, new Color(3.4822f, 3.4822f, 3.4822f, 1f), true); } }
            /// <summary>收藏窗 `WIldcard Display ▸ Background`：**0.75 s**（不是 1.0）、白、Once。
            /// ⚠️ 与 `Deck Editing Menu ▸ WIldcard Counter` 那颗**逐字段相同**（2026-10-27 现读；139/0.213/0.533 那套属 `Message Container ▸ Content`，见 `报告/B9_W36SHINEA_扫光断言与宿主.md`）。</summary>
            public static Params WildcardDisplay { get { return Make(0.25f, 0.75f, Color.white, false); } }

            static Params Make(float width, float duration, Color color, bool loop)
            {
                Params p;
                p.rotation = 120f;
                p.width = width;
                p.softness = 0.5f;
                p.duration = duration;
                p.color = color;
                p.colorFilter = 6f;
                p.colorGlow = 0f;
                p.reverse = false;
                p.loop = loop;
                return p;
            }
        }

        const string ShaderName = "CardPresentation/WildcardShine";
        static readonly List<WildcardShineLayer> _live = new List<WildcardShineLayer>();
        static bool _shaderWarned;

        readonly ImageQuad[] _quads;
        readonly Material _mat;
        readonly Params _p;
        Bounds _frame;
        Vector3 _refPos, _refScale;      // `Reframe` 那一刻第一块 quad 的世界位置 / lossyScale（`FollowFrame` 拿它比）
        float _time, _rate = 1f;
        bool _on, _playing;

        WildcardShineLayer(ImageQuad[] quads, Material mat, Params p)
        { _quads = quads; _mat = mat; _p = p; }

        // ---------------------------------------------------------------- 读口（自检用）
        /// <summary>这一层挂了几个 quad（九宫格 = 9；单件 = 1）。</summary>
        public int QuadCount { get { return _quads == null ? 0 : _quads.Length; } }
        /// <summary>当前 `_TransitionRate`（0 = 带在起点 · 1 = 播完/不亮）。</summary>
        public float Rate { get { return _rate; } }
        /// <summary>开着没有（原版 `Behaviour.enabled`）。</summary>
        public bool Enabled { get { return _on; } }
        /// <summary>还在播没有（Once 播完自动 false；Loop 恒 true，直到 `SetEnabled(false)`）。</summary>
        public bool Playing { get { return _playing; } }
        /// <summary>真的材质（自检要断「shader 名 == `CardPresentation/WildcardShine`」）。</summary>
        public Material ShaderMaterial { get { return _mat; } }
        /// <summary>材质**还挂在那几块 quad 上**吗。
        /// <para>🔴 为什么要有：`Shell/WindowButton.RefreshGray`（= 原版 `EverguildButton.SoftDisable` /
        /// `interactable=false` 那一跳）会把 quad 的材质**换走再换回**，而「换回」用的是它**变灰那一刻存下的**
        /// 那一份 ⇒ 若变灰发生在挂扫光**之前**，还原时会把扫光材质换成旧的 `Sprites/Default`（**静默**：
        /// 扫光从此不再出现）。宿主在「该亮」的那一拍查这个口，假就**重挂一次**。</para></summary>
        public bool StillAttached
        {
            get
            {
                if (_quads == null) return false;
                for (int i = 0; i < _quads.Length; i++)
                {
                    var q = _quads[i];
                    if (q == null) continue;
                    var mr = q.GetComponent<MeshRenderer>();
                    return mr != null && mr.sharedMaterial == _mat;
                }
                return false;
            }
        }
        /// <summary>框（世界坐标）—— 自检可比「等于那 9 块的世界并集」。</summary>
        public Bounds FrameBounds { get { return _frame; } }
        /// <summary>当前存活的层数（诊断 / 泄漏读口）。</summary>
        public static int LiveCount { get { return _live.Count; } }

        // ---------------------------------------------------------------- 建
        /// <summary>挂到**一颗** quad 上（图标 / 按钮那种单件）。</summary>
        public static WildcardShineLayer AttachQuad(ImageQuad q, Params p)
        {
            if (q == null) return null;
            return Attach(new[] { q }, p, q.name);
        }

        /// <summary>挂到一棵**九宫格**上（`MenuDraw.Nine` / `ImageQuad.CreateNineSlice` 返回的**根**）。
        /// 🔴 必须用 `GetComponentsInChildren`（根自己没有 `ImageQuad`，`GetComponent` 只会拿到 null）。</summary>
        public static WildcardShineLayer AttachNine(Transform root, Params p)
        {
            if (root == null) return null;
            var qs = root.GetComponentsInChildren<ImageQuad>(true);
            if (qs == null || qs.Length == 0) return null;
            return Attach(qs, p, root.name);
        }

        static WildcardShineLayer Attach(ImageQuad[] quads, Params p, string name)
        {
            var sh = Shader.Find(ShaderName);
            if (sh == null)
            {
                if (!_shaderWarned)
                {
                    _shaderWarned = true;
                    Debug.LogWarning("[WildcardShine] 取不到 shader `" + ShaderName + "`（不在 `Resources/` ⇒ player 包里"
                                   + "要靠 GraphicsSettings 的 AlwaysIncluded）⇒ 扫光层**不会出现**，且不报错。"
                                   + "见 `资料/完全复刻/报告/B9_W16SHINE_扫光统一.md`。");
                }
                return null;
            }

            var mat = new Material(sh);
            mat.name = "WildcardShine_" + (name ?? "layer");
            mat.SetFloat("_TransitionWidth", p.width);
            mat.SetFloat("_TransitionSoftness", p.softness);
            mat.SetColor("_TransitionColor", p.color);
            mat.SetFloat("_TransitionColorFilter", p.colorFilter);
            mat.SetFloat("_TransitionColorGlow", p.colorGlow);
            mat.SetFloat("_TransitionReverse", p.reverse ? 1f : 0f);
            mat.SetFloat("_TransitionAutoPlaySpeed", 0f);   // 原版 5 个实例全是 0（速度由 tweener 给）
            mat.SetFloat("_UseTransitionTex", 0f);          // 解析式等价，见 .shader 文件头 ③
            mat.SetFloat("_TransitionRate", 1f);            // 静止 = 不亮（rate=1 ⇒ 掩码 0，判据见 .shader 的 `band`）

            var layer = new WildcardShineLayer(quads, mat, p);
            Color tint = Color.white;
            bool tintTaken = false;
            for (int i = 0; i < quads.Length; i++)
            {
                var q = quads[i];
                if (q == null) continue;
                if (!tintTaken) { tint = q.Tint; tintTaken = true; }   // 原版整件只有一个 `Image.m_Color`
                q.SetMaterial(mat);      // 贴图 + 旧渲染队列带过去（分层靠队列，⛔ 不用 z）
                q.SetTint(tint);         // `_Color` = 原版 `Image.m_Color`
            }
            layer.Reframe();
            PruneDead();                 // 建新层时顺手回收（批处理里没有泵 ⇒ 只能靠这一下 + `StepAll`）
            _live.Add(layer);
            if (Application.isPlaying) Pump.Attach();
            return layer;
        }

        // ---------------------------------------------------------------- 播
        /// <summary>原版 `UIEffectTweener.Play(resetTime: true)`：从头播一次（Loop 那档一直循环）。</summary>
        public void Play(bool resetTime = true)
        {
            _on = true;
            _playing = true;
            if (resetTime) _time = 0f;
            _rate = Mathf.Clamp01(_time / Mathf.Max(1e-4f, _p.duration));
            PushRate();
        }

        /// <summary>原版 `Behaviour.enabled = option`：关掉 ⇒ 立刻回到「不亮」（rate = 1）。</summary>
        public void SetEnabled(bool on)
        {
            _on = on;
            if (!on) { _playing = false; _rate = 1f; PushRate(); }
        }

        /// <summary>推进 `dt` 秒。⚠️ 只在 `Enabled ∧ Playing` 时动（关掉/播完就停，省一次材质写）。</summary>
        public void Tick(float dt)
        {
            if (!_on || !_playing || _mat == null) return;
            FollowFrame();
            _time += Mathf.Max(0f, dt);
            float d = Mathf.Max(1e-4f, _p.duration);
            if (_p.loop) _rate = Mathf.Repeat(_time / d, 1f);
            else
            {
                _rate = Mathf.Clamp01(_time / d);
                if (_rate >= 1f) _playing = false;
            }
            PushRate();
        }

        /// <summary>按那几块 quad 的**世界并集**重算框（建时一次；分辨率/缩放变了要再调一次）。
        /// <para>⚠️ **不用 `Renderer.bounds`**：它要等渲染器同步（批处理里没有帧循环时读到的是旧值）。
        /// 这里直接拿 `MeshFilter.sharedMesh.bounds`（局部）逐角 `TransformPoint` —— 确定性、且与
        /// `ImageQuad.RebuildMesh` 写进去的几何逐位一致。</para></summary>
        public void Reframe()
        {
            bool any = false;
            Vector3 bmin = Vector3.zero, bmax = Vector3.zero;
            if (_quads != null)
                for (int i = 0; i < _quads.Length; i++)
                {
                    var q = _quads[i];
                    if (q == null) continue;
                    var mf = q.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    var lb = mf.sharedMesh.bounds;
                    for (int c = 0; c < 4; c++)
                    {
                        var p = q.transform.TransformPoint(new Vector3(
                            (c & 1) == 0 ? lb.min.x : lb.max.x,
                            (c & 2) == 0 ? lb.min.y : lb.max.y, lb.center.z));
                        if (!any) { bmin = p; bmax = p; any = true; }
                        else
                        {
                            bmin = Vector3.Min(bmin, p);
                            bmax = Vector3.Max(bmax, p);
                        }
                    }
                }
            if (!any) return;
            _frame = new Bounds((bmin + bmax) * 0.5f, bmax - bmin);
            if (_quads != null)
                for (int i = 0; i < _quads.Length; i++)
                    if (_quads[i] != null) { _refPos = _quads[i].transform.position; _refScale = _quads[i].transform.lossyScale; break; }
            if (_mat == null) return;
            var s = _frame.size;
            float mx = Mathf.Max(s.x, s.y);
            float rad = _p.rotation * Mathf.Deg2Rad;
            _mat.SetVector("_ShineFrame", new Vector4(_frame.center.x, _frame.center.y, s.x, s.y));
            _mat.SetVector("_ShineRot", new Vector4(Mathf.Cos(rad), Mathf.Sin(rad), mx > 0f ? 1f / mx : 1f, 0f));
        }

        void PushRate() { if (_mat != null) _mat.SetFloat("_TransitionRate", _rate); }

        /// <summary>框**跟着载体走**：窗口开场的缩放/滑入会让世界框变，而 `_ShineFrame/_ShineRot` 是
        /// CPU 侧一次算好的常量 ⇒ 变了就重算一次（判据 = 第一块 quad 的**世界位置与 lossyScale**，
        /// 每帧两次属性读，零分配）。⚠️ 不做这一下时，开场那 0.3 s 的缩放动画里扫光会**偏离原位**（静默）。</summary>
        void FollowFrame()
        {
            if (_quads == null) return;
            for (int i = 0; i < _quads.Length; i++)
            {
                var q = _quads[i];
                if (q == null) continue;
                var p = q.transform.position;
                var s = q.transform.lossyScale;
                if (p != _refPos || s != _refScale) Reframe();
                return;
            }
        }

        // ---------------------------------------------------------------- 全局泵 / 回收
        /// <summary>**确定性推进**全体（批处理自检用；每帧那条走 <see cref="Pump"/>）。
        /// 顺手**回收**（quad 全被销毁的层 ⇒ 销毁它的材质并从表里摘掉）。</summary>
        public static void StepAll(float dt)
        {
            PruneDead();
            for (int i = 0; i < _live.Count; i++)
                if (_live[i] != null) _live[i].Tick(dt);
        }

        /// <summary>把表清空（自检用例之间隔离用；⛔ 生产别调 —— 会把还活着的层一起丢掉）。</summary>
        public static void ResetForTest()
        {
            for (int i = 0; i < _live.Count; i++) if (_live[i] != null) _live[i].Release();
            _live.Clear();
        }

        /// <summary>把「quad 全被销毁」的层摘掉并销毁它的材质（泄漏管理：一页重建就换一层）。</summary>
        static void PruneDead()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                if (l == null) { _live.RemoveAt(i); continue; }
                if (l.AnyQuadAlive()) continue;
                l.Release();
                _live.RemoveAt(i);
            }
        }

        bool AnyQuadAlive()
        {
            if (_quads == null) return false;
            for (int i = 0; i < _quads.Length; i++) if (_quads[i] != null) return true;
            return false;
        }

        void Release()
        {
            if (_mat == null) return;
            if (Application.isPlaying) Object.Destroy(_mat);
            else Object.DestroyImmediate(_mat);      // 批处理没有帧循环 ⇒ `Destroy` 不生效（同 `MenuDraw.ClearChildren`）
        }

        /// <summary>每帧泵（Unity 的 `PlayerLoop`，形状照 `Battle/BattleSfx.cs` 的 `Pump` 抄 ——
        /// 批处理/编辑模式**不跑**它，那两条走 <see cref="StepAll"/>）。</summary>
        static class Pump
        {
            static bool _installed;
            /// <summary>已插进 PlayerLoop 没有（诊断读口）。</summary>
            public static bool Attached { get { return _installed; } }

            /// <summary>幂等安装（域重载关掉时静态字段会跨 Play 存活 ⇒ 不查就会越插越多）。</summary>
            public static void Attach()
            {
                var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                if (Contains(loop)) { _installed = true; return; }
                if (!Insert(loop, typeof(UnityEngine.PlayerLoop.Update)))
                {
                    Debug.LogWarning("[WildcardShine] 没找到 PlayerLoop 的 `Update` 阶段 ⇒ 扫光**不会自己动**"
                                   + "（自检的 `StepAll` 不受影响）");
                    return;
                }
                UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
                _installed = true;
            }

            static void OnUpdate() { StepAll(Time.deltaTime); }

            static bool Contains(UnityEngine.LowLevel.PlayerLoopSystem sys)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == typeof(Pump)) return true;
                    if (Contains(subs[i])) return true;
                }
                return false;
            }

            static bool Insert(UnityEngine.LowLevel.PlayerLoopSystem sys, System.Type phase)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == phase)
                    {
                        var inner = subs[i].subSystemList;
                        var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner == null ? 0 : inner.Length);
                        if (inner != null) list.AddRange(inner);
                        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem
                        {
                            type = typeof(Pump),
                            updateDelegate = OnUpdate,
                        });
                        subs[i].subSystemList = list.ToArray();
                        return true;
                    }
                    if (Insert(subs[i], phase)) return true;
                }
                return false;
            }
        }
    }
}
