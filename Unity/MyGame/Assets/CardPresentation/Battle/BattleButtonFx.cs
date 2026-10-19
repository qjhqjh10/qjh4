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
        public static readonly Color Disabled = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.502f);
        /// <summary>原版 `m_Colors.m_FadeDuration`。</summary>
        public const float FadeSeconds = 0.1f;

        // 「设置窗 / 战斗设置窗 4 行开关」那一族（`EverguildToggle`，NOR 绿）——逐值实读，
        // 判据 = `资料/普查产出_1018/R_按钮交互反馈对账.md` §1·2 #14。
        public static readonly Color ToggleNormal = new Color(0.2863f, 0.9647f, 0.6863f, 1f);
        public static readonly Color ToggleHighlighted = new Color(0.2921f, 0.9528f, 0.6817f, 1f);
        public static readonly Color TogglePressed = new Color(0.3058f, 0.8208f, 0.6105f, 1f);
        public static readonly Color ToggleDisabled = new Color(0.3058f, 0.8208f, 0.6105f, 0.502f);

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

        /// <summary>`ColorTint` 族。默认四格 = 原版默认（白 / 0.9608 / 0.7843 / 0.7843-α0.502）。
        /// 绿开关那一族传 `ToggleNormal/ToggleHighlighted/TogglePressed/ToggleDisabled`。</summary>
        public static BattleButtonFx Tint(HudPiece target, string name = null,
                                          Color? normal = null, Color? highlighted = null,
                                          Color? pressed = null, Color? disabled = null)
        {
            if (target == null) return null;
            return new BattleButtonFx(target, name ?? (target.name ?? "fx"), Mode.Tint,
                                      normal ?? Normal, highlighted ?? Highlighted,
                                      pressed ?? Pressed, disabled ?? Disabled,
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
            if (_mode == Mode.Swap)
            {
                var tex = _disabled ? _nt : _pressed ? (_pt ?? _nt) : _hover ? (_ht ?? _nt) : _nt;
                if (tex != null) _target.SetTexture(tex);
                return;
            }
            // 原版 `CrossFadeColor` 是**乘**在图形自己的颜色上
            _target.SetTint(new Color(_base.r * _key.r, _base.g * _key.g,
                                      _base.b * _key.b, _base.a * _key.a));
        }
    }
}
