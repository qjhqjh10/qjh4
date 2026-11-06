// BattleButtonFx.cs — 战斗内按钮的悬停 / 按下 / 禁用反馈（`N70` P0-2）
//
// 为什么单开一件：战斗侧**不走** `Shell/PointerLayer + WindowButton`（那套是外壳的指针层），
// 各面板自己判命中（`HitEndTurn` / `HitResign` / `MulliganPanel.HitDone` …）。原来它们
// **一条反馈都没有** —— 原版 `battlearena1` 的 89 颗 `Selectable` 里 **59 颗 `ColorTint` /
// 27 颗 `SpriteSwap`**（普查 → `资料/普查产出_1018/R_按钮交互反馈对账.md` §1·2 #15）。
//
// 判据（第一权威 = 原版 prefab 的 MB 字段，逐族实读）：
//   · **`ColorTint`**：`m_NormalColor` 白 · `m_HighlightedColor 0.9607843` · `m_PressedColor 0.7843137` ·
//     `m_SelectedColor 0.9607843` · `m_DisabledColor 0.7843137 / α0.502` · `m_FadeDuration 0.1 s`
//     （`TurnBtn`/`SettingsBtn`/`OffensiveButton`/`CenterCameraButton` 那一族）。
//     🔴 **原版 `ColorTint` 只打 `m_TargetGraphic` 那一颗**（不是整棵子树）。
//   · **`SpriteSwap`**：`m_HighlightedSprite` / `m_PressedSprite`（27 颗；例 `TurnBtn` =
//     `UI_Button_End Turn_Hover_wide` / `_Pressed_wide`；`Generic Close Button` = `40k_bt_close_hover/_pressed`；
//     `Resign` / `SkipTutorial` = `40K_button_hover/_pressed`；`Voice Over Button` =
//     `40k_UI_bt_voicelines_hover/_pressed`；`HideMulligan` = `40k_UI_bt_eye_hover/_pressed`）。
//     换图是**瞬时**的（UGUI `SpriteSwap` 没有补间）。
//   · 原版**没有** `Animation`/Animator/缩放反馈（全库 0 颗；同普查 §1·1）⇒ ⛔ 别加缩放。
//
// 🔴🔴 **2026-11-02（`B117-G10-03`）新取的三条判据**（逐颗现读 `Battle Arena 1.unity` 的 39 颗 `EverguildButton`）：
//   ① **禁用档有【两族 α】**：`colorTintGreyOnDisable = 1`（33/39）那族运行时 `m_DisabledColor.a = 1`
//      （19/19 颗 ColorTint 逐颗核过，见 `DisabledOpaque` 的 doc）；`grey = 0` 的 4 颗 ColorTint 才是 0.502
//      （见 `Disabled` 的 doc）。⇒ **`Tint(...)` 的缺省已改成 α 1.0**，0.502 那一档要显式传。
//   ② **禁用 ⇒ 还要换灰化材质**（`EverguildButtonHelper.DoMaterialRefresh` → `Everguild/UI/Greyscale`），
//      与 colorTint **同时**生效 ⇒ 见类尾「变灰」那一节。这一条原来是**完全没做**的。
//   ③ **点击音**：`EverguildButton.OnPointerClick` 里 `m_onClickSound` 有 clip 就播它、否则
//      `useDefaultAudioClipIfNull`（37/39 = 1）播 `SoundManager.defaultButtonSound`。**逐颗表**：
//        · 用自己的 cue 4 颗：`Generic Close Button` / `Auto Zoom Toggle` / `ChatToggle` / `Resign Button`；
//        · **静音 2 颗**（`useDefaultAudioClipIfNull = 0` 且无 cue）：**`Voice Over Button` · `Continue Button`**；
//        · 其余 33 颗 = 默认按钮音（我们 `WindowSounds.DefaultButtonCue` = cue `ButtonClick`）。
//      ⚠️ 我们的点击口在 `Battle/BattleHudButton.cs`（本文件不管点击）—— 那一侧的接口见它的 `clickCue`。
//
// 🔴 **2026-10-19（B4）**：本件只是**状态机 + 写色/换图**；命中判定仍在各面板自己那一份
// （`HitPaddedRect` / `WfSlider.InUnion` / `MulliganPanel.HitDone` …）—— ⛔ 别在这里另写一套命中。
// 宿主每帧喂 `SetHover/SetPressed`（按下 = 悬停 且 指针按着），并在 Play 里每帧 `Tick()`。

using UnityEngine;

namespace CardPresentation
{
    /// <summary>战斗内一颗按钮的视觉反馈（原版 `Selectable` 的等价物，只做「看得见」那一半）。</summary>
    public sealed class BattleButtonFx
    {
        // ================= 原版默认 `m_Colors`（`battlearena1` 那 59 颗 t=1，实读） =================
        public static readonly Color Normal = Color.white;
        public static readonly Color Highlighted = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        public static readonly Color Pressed = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
        public static readonly Color Selected = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        /// <summary>uGUI **默认** `m_DisabledColor`（α 0.502）—— 只有 `colorTintGreyOnDisable = 0` 那一族用它。
        /// 判据（2026-11-02 `B117-G10-03` 现读 `Battle Arena 1.unity`，39 颗 `EverguildButton` 逐颗）：
        /// **`grey = 0` 的 4 颗 ColorTint 全是 α 0.502**（`Auto Zoom Toggle` · `Select Active Skill Button` ·
        /// `Select Melee Button` · `Select Range Button`）⇒ 这一档**对**，⛔ 别把它当成所有钮的值。
        /// ⚠️ **今天 `Tint(...)` 的缺省不走它**，走 <see cref="DisabledOpaque"/>（见那条 doc）。
        /// ✅ 已复刻（2026-11-05 · `B150` 账 #35 · 判据 = `MonoBehaviour_4262.json` 的 `m_DisabledColor.a`
        /// 序列化实读 **0.5019608140** = 128/255）：原来写 `0.502f` 差 3.9e-5 ⇒ 断言 A6 红；⛔ 别写成 `0.502f`。</summary>
        public static readonly Color Disabled = new Color(0.7843137f, 0.7843137f, 0.7843137f, 128f / 255f);
        /// <summary>`colorTintGreyOnDisable = 1`（**变灰**）那一族的禁用色 = 同样的 RGB、**α 1.0（不透明）**。
        /// <para>🔴 **判据（2026-11-02 `B117-G10-03` 现读，两处互相印证）**：
        /// ① **场景表**：`Battle Arena 1.unity` 的 39 颗 `EverguildButton` 里 `grey = 1 & m_Transition = 1(ColorTint)`
        /// 共 **19 颗**，序列化 `m_DisabledColor.a` **18 颗 = 1**、1 颗（`Views` pid 4630）序列化写 0.502 但
        /// `colorTintGreyOnDisableAlpha = 1` ⇒ **19/19 运行时 α = 1**；
        /// ② **机制**：`decomp_full/EverguildButton__Awake.c` 在 `grey != 0 && m_Transition == 1` 时把
        /// `colorBlock.disabledColor.a` 改写成 **`+0x17C` = `colorTintGreyOnDisableAlpha`**，再 `set_colors`；
        /// 而 `EverguildButton__.ctor.c` 该字段出厂 = **1.0f**，本场景 39 颗逐颗现读**全是 1**（1 颗写 0）。
        /// ⇒ 变灰那一族的禁用色是**不透明**的，`α 0.502` 只是 uGUI 出厂值（原版被上面那一跳改掉了）。</para>
        /// <para>⚠️ `SpriteSwap` 档（`m_Transition = 2`）**根本不看 `m_Colors`** ⇒ 这一档对它们无意义
        /// （那 14 颗的禁用观感 = `m_DisabledSprite`（全 null ⇒ 回落 Normal）+ 灰化材质）。</para></summary>
        public static readonly Color DisabledOpaque = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
        /// <summary>原版 `m_Colors.m_FadeDuration`。</summary>
        public const float FadeSeconds = 0.1f;

        // 「设置窗 / 战斗设置窗 4 行开关」那一族（`EverguildToggle`，NOR 绿）——逐值实读，
        // 判据 = `资料/普查产出_1018/R_按钮交互反馈对账.md` §1·2 #14。
        public static readonly Color ToggleNormal = new Color(0.2863f, 0.9647f, 0.6863f, 1f);
        public static readonly Color ToggleHighlighted = new Color(0.2921f, 0.9528f, 0.6817f, 1f);
        public static readonly Color TogglePressed = new Color(0.3058f, 0.8208f, 0.6105f, 1f);
        public static readonly Color ToggleDisabled = new Color(0.3058f, 0.8208f, 0.6105f, 0.502f);

        // ================= 「选卡面板 Continue Button」绿族（`B141-W35` · 2026-11-04） =================
        //  判据（逐字段现读，`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1`）：
        //   · `MonoBehaviour_4262.json` = **`UnityEngine.UI.Button`**（`bundle_Waprforge_monoscripts/MonoScript_-4448497653027179337.json`
        //     的 `m_ClassName` 现读 = `Button`，⛔ **不是** `EverguildButton` ⇒ 没有 `colorTintGreyOnDisable` 那一跳 α 改写，
        //     禁用 α 保持序列化的 **0.5019608**）；`m_GameObject` → GO **779**，
        //     `m_Transition = 1` ColorTint、`m_Interactable = 1`，`m_Colors` 五格 + `m_ColorMultiplier = 2`
        //     + `m_FadeDuration = 0.1`（与本类 `FadeSeconds` 同值）；
        //   · 它的 `m_TargetGraphic` → `MonoBehaviour_4210.json` = **`BG`**（`m_GameObject` → GO **1153**）
        //     那颗 `Image`，`m_Color` = **与 Normal 同一个绿**。
        //  🔴 **两处都是绿** ⇒ 屏上 = 贴图 × `Image.m_Color` × `m_NormalColor` × `ColorMultiplier`
        //     （UGUI 那一跳的判据 = `CanvasRenderer.SetColor` 的 doc 原话「Will be multiplied with the
        //     UIVertex color and the Canvas color」，本机 `UnityEngine.UIModule.xml:442`）
        //     —— ⛔ **别把其中一处当白**：只留一处会把 R 从 0.2717 抬到 0.7370（`B141-W35` 的灭自证断言钉的就是这条）。
        //  ⚠️ 本类状态机**没有** Selected 态（同 `Selected` 常量那条：原版只有 EventSystem 选中才进，
        //     本面板点完就关）⇒ `ContinueSelected` 只承载原值，不参与 `Retarget`。
        //  ✅ 已复刻（2026-11-04 · B141-W35 · 判据 = `Battle Arena 1.unity` GO779 MB4262 + Image MB4210 现读）
        public static readonly Color ContinueNormal = new Color(0.36862749f, 0.89411765f, 0.58743727f, 1f);
        public static readonly Color ContinueHighlighted = new Color(0.46666670f, 0.91372555f, 0.40000004f, 1f);
        public static readonly Color ContinuePressed = new Color(0.47843140f, 0.49411768f, 0.48235297f, 1f);
        /// <summary>原版 `m_SelectedColor`（与 Normal **只差 B**：0.5882353 vs 0.5874373）。只承载原值（见上）。</summary>
        public static readonly Color ContinueSelected = new Color(0.36862746f, 0.89411771f, 0.58823532f, 1f);
        /// <summary>原版 `m_Colors.m_ColorMultiplier`（本族 = **2**；`B131` 色彩 A14 普查：全库只有 15 颗是 2、其余 1,252 颗 = 1）。
        /// ⚠️ UGUI 是 `tintColor * colorMultiplier` 之后再 `CrossFadeColor` ⇒ **四态与 α 一起乘**。</summary>
        public const float ContinueColorMultiplier = 2f;

        enum Mode { Tint, Swap }

        readonly Mode _mode;
        readonly HudPiece _target;
        readonly string _name;

        // ColorTint 四格（缺省 = 原版默认）
        readonly Color _n, _h, _p, _d;
        Color _base;                       // 目标图形自己的基准色（`m_Color` / `Image.color`）
        Color _key = Color.white, _targetKey = Color.white;

        // SpriteSwap 三张
        Texture2D _nt, _ht, _pt;

        bool _hover, _pressed, _disabled;

        BattleButtonFx(HudPiece target, string name, Mode mode,
                       Color n, Color h, Color p, Color d,
                       Texture2D nt, Texture2D ht, Texture2D pt)
        {
            _target = target;
            _name = name;
            _mode = mode;
            _n = n; _h = h; _p = p; _d = d;
            _nt = nt; _ht = ht; _pt = pt;
            _base = target != null ? target.Tint : Color.white;
        }

        /// <summary>`ColorTint` 族。默认四格 = 原版默认（白 / 0.9608 / 0.7843 / 0.7843-α**1.0**）。
        /// 绿开关那一族传 `ToggleNormal/ToggleHighlighted/TogglePressed/ToggleDisabled`。
        /// <para>🔴 **2026-11-02（`B117-G10-03`）改：禁用档的缺省从 `Disabled`（α 0.502）换成
        /// <see cref="DisabledOpaque"/>（α 1.0）**。判据 = 本类 `DisabledOpaque` 那条 doc（19/19 颗
        /// `grey=1` 的 ColorTint 钮运行时 α 都是 1；`Awake` 里那次 `Awake` 改写还用 `+0x17C` 覆盖一次）。
        /// ⚠️ **只动缺省值，常量一个没删**：需要 `grey = 0` 那一档的调用点显式传 `BattleButtonFx.Disabled`。</para>
        /// <para>🔴 **2026-11-04（`B141-W35`）加 `colorMultiplier`**：原版 `Selectable.DoStateTransition` 是
        /// `StartColorTween(tintColor * m_Colors.colorMultiplier, instant)`（本机 `com.unity.ugui/…/Selectable.cs:702`）
        /// ⇒ **乘发生在四态色上**（α 一起乘，⛔ 不是只乘 RGB）。缺省 1 = 所有既有调用点零变化。
        /// 选卡面板那颗 Continue Button 传 `ContinueColorMultiplier`（2）。</para></summary>
        public static BattleButtonFx Tint(HudPiece target, string name = null,
                                          Color? normal = null, Color? highlighted = null,
                                          Color? pressed = null, Color? disabled = null, float colorMultiplier = 1f)
        {
            if (target == null) return null;
            // 原版 `tintColor * m_Colors.colorMultiplier`：**四通道一起乘**（含 α）—— ⛔ 别只乘 RGB。
            float m = colorMultiplier;
            return new BattleButtonFx(target, name ?? (target.name ?? "fx"), Mode.Tint,
                                      (normal ?? Normal) * m, (highlighted ?? Highlighted) * m,
                                      (pressed ?? Pressed) * m, (disabled ?? DisabledOpaque) * m,
                                      null, null, null);
        }

        /// <summary>`SpriteSwap` 族（原版 27 颗）。悬停/按下图取不到 ⇒ **保持原图并出声一次**
        /// （⛔ 不静默：那会表现成「这颗钮没有反馈」）。</summary>
        public static BattleButtonFx Swap(HudPiece target, string name,
                                          Texture2D normal, Texture2D hover, Texture2D pressed)
        {
            if (target == null) return null;
            var fx = new BattleButtonFx(target, name, Mode.Swap,
                                        Normal, Highlighted, Pressed, Disabled,
                                        normal, hover, pressed);
            if (hover == null || pressed == null)
                Debug.LogWarning($"[BattleButtonFx] `{name}` 的悬停/按下图缺了"
                               + $"（hover={(hover != null)} pressed={(pressed != null)}）"
                               + " —— 这一颗**没有完整换图反馈**（不静默；补齐图后自动生效）");
            fx.Apply();
            return fx;
        }

        /// <summary>换掉 Normal 图（`SpriteSwap` 族用）。为什么要它：Mulligan 那几颗的 Normal
        /// **随「已标记」变**（`UI_Button_Mulligan` ↔ `_Pressed`）—— 标记态若还拿旧 Normal，
        /// 指针一离开就把「按下图」换回未标记那张（静默错）。</summary>
        public void SetNormal(Texture2D normal)
        {
            if (_mode != Mode.Swap) return;
            _nt = normal;
            Apply();
        }

        /// <summary>悬停态（宿主每帧喂）。</summary>
        public void SetHover(bool on) { if (_hover == on) return; _hover = on; Retarget(); }
        /// <summary>按下态（原版 `Selectable.Pressed` = 指针在件上并按着）。</summary>
        public void SetPressed(bool on) { if (_pressed == on) return; _pressed = on; Retarget(); }
        /// <summary>禁用态（原版 `DisabledColor`；默认 false、现有调用点零变化）。</summary>
        public void SetDisabled(bool on) { if (_disabled == on) return; _disabled = on; Retarget(); }

        /// <summary>自检读**目标**色键（批处理没有帧循环；Play 里当前值是渐变中的 `_key`）。</summary>
        public Color KeyForTest { get { return _targetKey; } }
        /// <summary>自检读**当前实际**色键。</summary>
        public Color CurrentKeyForTest { get { return _key; } }
        /// <summary>自检读当前贴图名（`SpriteSwap` 族；`Tint` 族返回原图名）。</summary>
        public string TextureNameForTest
        {
            get { var t = _target != null ? _target.Texture : null; return t != null ? t.name : null; }
        }

        void Retarget()
        {
            if (_mode == Mode.Swap) { Apply(); return; }
            _targetKey = _disabled ? _d : _pressed ? _p : _hover ? _h : _n;
            if (!Application.isPlaying) { _key = _targetKey; Apply(); }
            // 🔴 2026-11-02（`B117-G10-03`）：Play 里 `Apply()` 走的是**逐帧 lerp**（颜色要淡），但**灰化材质不淡** ——
            //   原版 `DoStateTransition` 是**当场**换材质（`DoMaterialRefresh` 没有补间）⇒ 这里即时落一次，
            //   ⛔ 别等 `Tick()`（那会晚一帧）。`ApplyGray` 幂等（状态没变直接 return）。
            else ApplyGray(WantGray);
        }

        /// <summary>Play 里每帧调（0.1s 逐通道 lerp，与 `WindowButton` 同形）。批处理里幂等。</summary>
        public void Tick()
        {
            if (_mode == Mode.Swap || !Application.isPlaying || _key == _targetKey) return;
            float k = Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.0001f, FadeSeconds));
            _key = Color.Lerp(_key, _targetKey, k);
            if (Mathf.Abs(_key.r - _targetKey.r) < 0.001f && Mathf.Abs(_key.g - _targetKey.g) < 0.001f
                && Mathf.Abs(_key.b - _targetKey.b) < 0.001f && Mathf.Abs(_key.a - _targetKey.a) < 0.001f)
                _key = _targetKey;
            Apply();
        }

        /// <summary>建完立刻写一次 Normal（原版 `Selectable.OnEnable → DoStateTransition(Normal, instant)`；
        /// 对 `NormalColor` 不是白的族 —— 绿开关 —— **不立刻写就是错色**）。</summary>
        public void ApplyInstant()
        {
            if (_mode == Mode.Swap) { Apply(); return; }
            _targetKey = _key = _disabled ? _d : _pressed ? _p : _hover ? _h : _n;
            Apply();
        }

        void Apply()
        {
            if (_target == null) return;
            // 🔴 2026-11-02（`B117-G10-03`）：禁用 ⇒ **换灰化材质**（原版 `EverguildButton` 那条，见类尾 `ColorTintGreyOnDisable`）。
            //    两支（Tint / Swap）都要做 —— 原版那是 `DoStateTransition` 的公共后半段，与 `m_Transition` 无关。
            ApplyGray(WantGray);
            if (_mode == Mode.Swap)
            {
                var tex = _disabled ? _nt : _pressed ? (_pt ?? _nt) : _hover ? (_ht ?? _nt) : _nt;
                if (tex != null) _target.SetTexture(tex);
                return;
            }
            // ✅ 已复刻（2026-11-04 · B141 · `B141-W57` · 判据 = UGUI `Selectable.cs:702` + `Graphic.CrossFadeColor`
            //   + `CanvasRenderer.SetColor` 的 multiply 语义）：原版 `tintColor * colorMultiplier` **先不夹**，
            //   夹在最后一次**字节顶点色**写入 ⇒ 算式 = `Clamp01(底座 × 色键 × m)`。
            //   ⚠️ 乘数已在 ctor 乘进四态（`KeyForTest` 要读到它）⇒ 这里 multiplier 传 1；
            //   实现与外壳侧 `WindowButton.ApplyTint` **共用同一个静态口**（⛔ 别在这里另写一份乘法）。
            _target.SetTint(WindowButton.ComposeVertexColor(_base, _key, 1f));
        }

        // ============================================================================================
        //  🔴 2026-11-02（`B117-G10-03`）：**变灰**（原版 `EverguildButton.colorTintGreyOnDisable` 那一半）
        //
        //  判据链（逐跳实读）：
        //   ① `decomp_full/EverguildButton__DoStateTransition.c` 末段：
        //      `flag1 = (state != Disabled) && (softDisabled == 0)` ⇒ `SetToStateActiveOrDisabled(flag1)`
        //      ⇒ `SwitchMaterial(interactable)` ⇒ `EverguildButtonHelper.DoMaterialRefresh(list, interactable)`；
        //   ② `EverguildButtonHelper__DoMaterialRefresh.c` 末句 `*(byte*)(modifier + 0x20) = interactable ^ 1;`
        //      ⇒ 每颗图形件上那颗 `EverguildButtonMaterialModifier.ToogleGreyScale = !interactable`；
        //   ③ `EverguildButtonMaterialModifier__GetModifiedMaterial.c`：`ToogleGreyScale` 为真时返回
        //      `EverguildButtonHelper.DisabledMaterial` = **一份静态缓存**的
        //      `new Material(Shader.Find("Everguild/UI/Greyscale"))`（`EverguildButtonHelper.cs` 属性体）。
        //   ⇒ **一句话**：禁用 ⇒ 按钮那一组图形件换成同一份灰化材质（**只有一份**，原版就是静态共享的）。
        //  ⚠️ 与 `colorTint` **同时**生效（那是 `DoStateTransition` 的前半段），⛔ 不是二选一。
        //  ⚠️ uGUI `Image` 的贴图与颜色由 `CanvasRenderer` 逐件给 ⇒ **一份共享材质在这里也成立**
        //     （与 `Shell/WindowButton` 给 `ImageQuad` 每颗造一份不同 —— 那一支贴图存在材质上，见那边的 doc）。
        //  ⛔ 取不到 shader ⇒ **不变灰 + 出声**（同 `WindowButton.RefreshGray`：不拿别的灰顶替）。
        // ============================================================================================

        /// <summary>原版 `EverguildButton.colorTintGreyOnDisable`（`+0x17A`）—— 「这颗钮到底会不会变灰」的**前置闸**。
        /// 默认 **true**：`Battle Arena 1.unity` 的 39 颗 `EverguildButton` 里 **33 颗 = 1**（`grey = 0` 只有 6 颗）；
        /// 本类唯一现役的禁用站点（选牌卡钮 `Generic Simplified UI Button_updated`，
        /// `bundle_battlesharedresources_assets_all/MonoBehaviour/MonoBehaviour_-5173003275250738388.json`）
        /// 现读 `colorTintGreyOnDisable = 1` ⇒ 它**该灰**。
        /// <para>需要不开灰的站点**由调用点显式置 `false`**（判据 = 那一颗自己的 prefab 字段，⛔ 别按族推）。</para></summary>
        public bool ColorTintGreyOnDisable = true;

        /// <summary>该灰了吗（原版那两条路合成的那一个布尔：`interactable == false` ⇒ 灰）。</summary>
        bool WantGray { get { return ColorTintGreyOnDisable && _disabled; } }

        /// <summary>现在**真的**灰着吗（取不到 shader 时仍是 `false` —— 那是「想灰没灰成」，见 `GrayShaderMissingForTest`）。</summary>
        public bool GrayedForTest { get { return _grayOn; } }

        /// <summary>这一颗**想要灰但取不到 shader**（自检要能把它抓出来：静默不变灰是最坏的一档）。</summary>
        public bool GrayShaderMissingForTest { get { return _grayAskedButUnavailable; } }

        bool _grayOn;
        bool _grayAskedButUnavailable;
        Material _savedMat;

        static Material _grayMat;                       // 原版 `EverguildButtonHelper.disabledMaterial`：**全局一份**
        static bool _saidNoGrayShader;

        /// <summary>原版那张灰化 shader 的内部名 —— **不另写一份字面量**，取 `Shell/WindowButton` 那处唯一出处
        /// （它自己是从 `EverguildButtonStateFollower.EVERGUILD_UI_GREYSCALE` 实读的；⛔ 别在这里抄第三个副本）。</summary>
        const string GrayShaderName = WindowButton.GrayShaderName;

        void ApplyGray(bool on)
        {
            if (on == _grayOn) return;
            if (_target == null || !_target.IsUgui)
            {
                // 世界空间件（`ImageQuad`）：本类**不换**它的材质 —— 那一族的灰化归 `Shell/WindowButton`
                // （每颗一份材质、贴图存在材质上，见 `ImageQuadMaterialModifier` 文件头）。如实出声，不假装做了。
                if (on) Debug.LogWarning("[BattleButtonFx] `" + _name + "` 是世界空间件（`ImageQuad`）⇒ "
                                       + "**本类不换它的灰化材质**（那一支归 `WindowButton`）；这一颗禁用时只会变色、不会变灰");
                return;
            }
            if (on)
            {
                if (!EnsureGrayMaterial()) { _grayAskedButUnavailable = true; return; }
                _savedMat = _target.Material;                // uGUI：没设过时这里拿到的是**默认 UI 材质**（不是 null）
                _target.SetMaterial(_grayMat);
                _grayOn = true;
            }
            else
            {
                _target.SetMaterial(_savedMat);              // 原样还回去（与 `WindowButton.RefreshGray` 同一语义）
                _grayOn = false;
            }
            ApplyGrayExtras(on);                             // 🆕 2026-11-04（`B142-E7`）：同一按钮上**另外几颗**图形件（见类尾那一节）
        }

        static bool EnsureGrayMaterial()
        {
            if (_grayMat != null) return true;
            Shader sh;
            if (!WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(GrayShaderName, out sh) || sh == null)
            {
                if (!_saidNoGrayShader)
                {
                    _saidNoGrayShader = true;
                    Debug.LogWarning("[BattleButtonFx] 取不到原版灰化 shader `" + GrayShaderName + "`"
                                   + "（随包 shader bundle 在不在？见 `Shell/WindowButton.GrayShaderAvailable`）"
                                   + " ⇒ **禁用态只变色、不变灰**（⛔ 不拿别的灰顶替 —— 铁律 3）");
                }
                return false;
            }
            // 原版那一份**一个属性都没写**（shader 自带 `_GreyScale = 1.0`）⇒ 我们也只 new 出来。
            _grayMat = new Material(sh);
            _grayMat.name = "UI Greyscale (BattleButtonFx)";
            return true;
        }

        // ============================================================================================
        //  🆕 2026-11-04（`B142-E7`）：**一颗按钮上可以有多颗灰化件**
        //
        //  判据（逐件现读 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1`，选卡面板
        //  `Continue Button` = `Battle Arena 1.unity` GO **779**）：
        //   ① `MonoBehaviour_4262.json` = `UnityEngine.UI.Button`（该 GO 自己），`m_TargetGraphic` → `BG`（GO 1153）的 `Image`；
        //   ② 子树里 **4 颗** `EverguildButtonMaterialModifier`（`m_Script` pathID `-9096086197743689236`，
        //      `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-9096086197743689236.json` 的 `m_Name` 现读
        //      = `EverguildButtonMaterialModifier`）：根透明图 MB **5159**（GO 779）· `BG` MB **5186**（GO 1153）·
        //      `ContinueText` MB **4762**（GO 444）· `ContinueButton` MB **4996**（GO 926）；
        //   ③ 🔴 **第四颗（`ContinueText`）实际不灰**：`EverguildButtonMaterialModifier__GetModifiedMaterial.c`
        //      开头两跳 `is TextMeshProUGUI` / `is TMP_SubMeshUI` ⇒ **原样返回 `baseMaterial`**
        //      （ILSpy 同形：`out_cpp2il_dev_ilspy/EverguildButtonMaterialModifier.cs` 里两个 `typeof(…)` 早退）
        //      ⇒ **有效灰化 = 3 颗**（根透明图 / `BG` / `ContinueButton`）。
        //      ⛔ 别给 TMP 挂这一件：灰化材质没有字体图集，换上会把那行字画坏 —— 原版正是因此跳过 TMP 一族。
        //   ⇒ 本类的 `_target` 只承载**色键**（原版 `ColorTint` 只打 `m_TargetGraphic` 那一颗）；
        //     追加进来的件**只跟着灰、不参与色键** —— 与原版 `DoMaterialRefresh(list, …)` 逐颗写 `ToogleGreyScale` 同形。
        // ============================================================================================

        readonly System.Collections.Generic.List<HudPiece> _grayExtras = new System.Collections.Generic.List<HudPiece>();
        readonly System.Collections.Generic.List<Material> _grayExtraSaved = new System.Collections.Generic.List<Material>();

        /// <summary>把**另一颗图形件**也纳入「禁用 ⇒ 换灰化材质」这一组（原版 `EverguildButtonHelper.DoMaterialRefresh(list, …)`
        /// 就是按 list 逐颗写 `ToogleGreyScale` 的，判据见本类尾那一节）。
        /// <para>⚠️ **只影响灰化那一半**：色键（`m_Colors` / `m_TargetGraphic`）仍只打 <see cref="_target"/>（原版 `ColorTint` 只取那一颗）。</para>
        /// <para>幂等：同一件重复传入只记一次；**已经灰着**时当场给这一颗补上灰化材质（原版改的是同一个 bool）。</para>
        /// <para>⚠️ 只接受 **uGUI 件**（原版 `MaskableGraphic`）；世界空间 `ImageQuad` 那一支归 `Shell/WindowButton`，这里出声、不动它。</para></summary>
        public void AddGrayTarget(HudPiece piece)
        {
            if (piece == null || piece == _target) return;
            if (_grayExtras.Contains(piece)) return;
            _grayExtras.Add(piece);
            _grayExtraSaved.Add(null);
            if (!_grayOn) return;
            if (!piece.IsUgui)
            {
                Debug.LogWarning("[BattleButtonFx] `" + _name + "` 追加的灰化件 `" + piece.name + "` 是世界空间件（`ImageQuad`）"
                               + " ⇒ **本类不换它的材质**（那一支归 `WindowButton`）");
                return;
            }
            _grayExtraSaved[_grayExtras.Count - 1] = piece.Material;
            piece.SetMaterial(_grayMat);
        }

        /// <summary>本类当前带的灰化件个数（含 `_target`）—— 自检断「三颗都挂上了」用。</summary>
        public int GrayTargetCountForTest { get { return _grayExtras.Count + (_target != null ? 1 : 0); } }

        /// <summary>自检：**这一件**现在是不是真的换着灰化材质。
        /// （原来只有 <see cref="GrayedForTest"/> 一份，断不出「三颗都换了吗」⇒ `B142-E7` 补这个口。）</summary>
        public bool IsGrayedForTest(HudPiece piece)
        {
            if (piece == null) return false;
            if (piece == _target) return _grayOn;
            if (!_grayOn || !piece.IsUgui) return false;
            return _grayExtras.Contains(piece) && piece.Material == _grayMat;
        }

        void ApplyGrayExtras(bool on)
        {
            for (int i = 0; i < _grayExtras.Count; i++)
            {
                var p = _grayExtras[i];
                if (p == null) continue;
                if (!p.IsUgui)
                {
                    if (on) Debug.LogWarning("[BattleButtonFx] `" + _name + "` 的附带灰化件 `" + p.name + "` 是世界空间件"
                                           + "（`ImageQuad`）⇒ 不换它的材质（那一支归 `WindowButton`）");
                    continue;
                }
                if (on) { _grayExtraSaved[i] = p.Material; p.SetMaterial(_grayMat); }
                else p.SetMaterial(_grayExtraSaved[i]);
            }
        }
    }
}
