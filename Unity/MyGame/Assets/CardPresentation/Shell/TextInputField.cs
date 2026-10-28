// TextInputField.cs — 外壳层的【文字输入框】（原版 = `TMP_InputField` 子类 + prefab `GenericPromptWindow/Window/inputs/template`）
//
// ============================ 这是什么 / 为什么自己写 ============================
// 原版 `PromptPopup.InstantiateInput(PromptContext.InputData)` 干的事（判据见
// `PromptPopup__InstantiateInput.c` / `out_cpp2il_dev_ilspy/PromptPopup.cs`）：
//   `Instantiate(inputPrefab, inputHolder)` → `contentType = data.ContentType` · `text = data.DefaultValue`
//   → 占位字过词条 → 回调。
// 🔴 **本仓没有 uGUI `TMP_InputField`**（整条渲染链是 `ImageQuad + Label`，见 `Shell/PromptPopup.cs` 文件头 ③）
//   ⇒ 这里按**同一个版面**自建一个：九宫底 + 占位字 + 正文 + 命中区 + 键盘。
//
// ============================ 判据（全部来自 prefab，逐条标出处） ============================
// prefab = `d:/2/tools/assetripper_out/ExportedProject/Assets/GenericPromptWindow.prefab`
// （`bundle_menus_assets_all` 的同一份；下列字段是本轮**直读 YAML**，⛔ 不是从截图量的）：
//   · `Window/inputs`  `m_SizeDelta = (900, 0)` —— 宽吃满面板（VLG `childControlWidth`），高由子件定
//   · `inputs/template` `m_SizeDelta = (0, 90)` ⇒ **行高 90**、宽拉伸到 900
//   · `template/Text Area` `m_SizeDelta = (−20, −13)` ⇒ 四边各内缩 **10 / 6.5** ⇒ 文本区 **880 × 77**
//   · `Text Area/Placeholder` / `Text Area/Text` 都是 `(0,0)`（吃满 Text Area）
//   · `m_ContentType: 0`（Standard）· `m_LineType: 0`（SingleLine）· `m_CharacterValidation: 0`（None）
//     · **`m_CharacterLimit: 0` = 不限长**（两处 TMP 都是 0）⇒ 我们默认也不截断
//   · 底 = `40K_dropdown_bg` **九宫 (23,20,23,20)**、源图 119×102、tint **(0.286, 0.965, 0.686, 1)**
//     （与 `Shell/GachaEventWindow.cs:222` 的 `ArtDropdownBg` 同一张图；那处的 tint 是它自己的暗色，
//       ⛔ 别把两处混用）
//   · 占位字 TMP = 字号 **40** · 基准 **14** · auto **[18~40]** · 色 **(0.972, 0.972, 0.972, 0.5)**
//   · 正文 TMP   = 字号 **45** · 基准 **14** · auto **[18~45]** · 折行 **3**（= 开折行）
//     （以上四格来自账 541 差异列的 `menu_dump.py` 实读，本条本轮用 YAML 复核了尺寸、字号仍用那份读数）
//
// ============================ 🔶 我们挑的（原版拿不到 / 本仓没有对应物） ============================
//   ① **光标** = 编辑时在正文末尾补一个 `_`（同 `Deck/DeckRuntime.cs:7176` 那套键盘输入的既有做法）。
//      原版是 `TMP_InputField` 自己的闪烁 caret（协程 + 选区），本仓无该项 ⇒ 等价物。
//   ② **选区 / 粘贴 / 输入法候选窗**：不做（同 ①）。`onTextInput` 收的是**已按输入法处理**的字符，
//      中文能进来（判据同 `DeckRuntime.cs:7190` 那条注释）。
//   ③ **`Password` 档的掩码字符** = `*`（TMP 默认是 `*`，但本仓没有那份资产可读 ⇒ 按 TMP 默认值取）。
//   ④ **正文颜色** = 白：prefab 里那颗 TMP 的 `m_Color` 本轮**没读**，按同一扇窗其它正文（白）取。
//   —— 以上四条都是「我们能观测到的等价物」，⛔ 不冒充原版写法。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardPresentation
{
    /// <summary>原版 `PromptContext`（`out_cpp2il_dev_ilspy/PromptContext.cs`）的等价物。
    /// <para>原版字段表（逐条现读）：`InputData { Name · Label · DefaultValue · ContentType }`，
    /// 构造重载 `(name, label, inputType, defaultValue)` 与 `(name, inputType, defaultValue)`。</para>
    /// <para>⚠️ 原版还有一个 `DropdownData : InputData`（带 `Options`）—— 那是**下拉**那条活，
    /// 本类**故意不建**（没有消费者 ⇒ 死代码红线）。</para></summary>
    public class PromptContext
    {
        /// <summary>原版 `PromptContext.InputData`。</summary>
        public class InputData
        {
            public readonly string Name;
            public readonly string Label;
            public readonly string DefaultValue;
            public readonly TMP_InputField.ContentType ContentType;

            public InputData(string name, string label,
                             TMP_InputField.ContentType inputType = TMP_InputField.ContentType.Standard,
                             string defaultValue = null)
            {
                Name = name; Label = label; ContentType = inputType; DefaultValue = defaultValue;
            }

            public InputData(string name,
                             TMP_InputField.ContentType inputType = TMP_InputField.ContentType.Standard,
                             string defaultValue = null)
            {
                Name = name; Label = name; ContentType = inputType; DefaultValue = defaultValue;
            }
        }
    }

    /// <summary>外壳层的一格文字输入框（版面/字段判据见文件头）。
    /// <para>⚠️ **生命周期**：本件靠 `Tick()` 泵，**不依赖 `Awake/OnEnable/Update`** ——
    /// 编辑模式下 `AddComponent` 不会触发那三个回调（工程已知坑），所以键盘订阅在
    /// <see cref="BeginEdit"/> 里**惰性挂**、<see cref="EndEdit"/> 里退。</para></summary>
    public sealed class TextInputField : MonoBehaviour
    {
        // ---- prefab 实值（出处见文件头）----
        public const string ArtBg = "40K_dropdown_bg";      // 119×102 · 九宫 (23,20,23,20)
        public static readonly Vector4 BgBorder = new Vector4(23f, 20f, 23f, 20f);
        public const float BgTexW = 119f, BgTexH = 102f;
        public static readonly Color BgTint = new Color(0.286f, 0.965f, 0.686f, 1f);
        public const float RowH = 90f;                      // `inputs/template` `m_SizeDelta.y`
        public const float AreaInsetX = 10f, AreaInsetY = 6.5f;   // `Text Area (−20,−13)` 的一半
        public const float PhFontPx = 40f, PhAutoMinPx = 18f, PhAutoBasePx = 14f;
        public const float TxFontPx = 45f, TxAutoMinPx = 18f, TxAutoBasePx = 14f;
        public static readonly Color PhColor = new Color(0.972f, 0.972f, 0.972f, 0.5f);
        public static readonly Color TxColor = Color.white;

        PromptContext.InputData _data;
        Label _text, _ph;
        WindowButton _hit;
        string _value = "";
        bool _editing, _hooked;
        int _maxLen;
        readonly List<char> _typed = new List<char>();

        /// <summary>当前输入内容（未编辑时也是最后一次的值）。</summary>
        public string Value { get { return _value; } }
        /// <summary>是不是正在编辑。</summary>
        public bool Editing { get { return _editing; } }
        /// <summary>正文那颗 `Label`（自检读口）。</summary>
        public Label TextLabel { get { return _text; } }
        /// <summary>占位字那颗 `Label`（自检读口）。</summary>
        public Label PlaceholderLabel { get { return _ph; } }
        /// <summary>命中区（自检读口）。</summary>
        public WindowButton Hit { get { return _hit; } }

        /// <summary>建一格输入框。
        /// <param name="r">整行矩形（原版 `inputs` 那一行：宽 = 面板宽 900，高 = 90）。</param>
        /// <param name="maxLen">0 = 不限长（prefab `m_CharacterLimit: 0` 就是这个意思）。</param>
        /// <param name="qBase">渲染队列基准：底 = qBase、字 = qBase+1（与 `PromptPopup` 的 `QFill/QText` 同序）。</param>
        /// <param name="onSubmit">回车提交时回调（值 = <see cref="Value"/>）；可以不传。</param>
        public static TextInputField Create(Transform parent, PxRect r, PromptContext.InputData data,
                                            int maxLen = 0, int qBase = 3142, Action<string> onSubmit = null)
        {
            if (data == null) return null;
            var go = MenuDraw.Node(parent, "template", r);
            if (go == null) return null;
            var f = go.gameObject.AddComponent<TextInputField>();
            f._data = data;
            f._maxLen = Mathf.Max(0, maxLen);
            f._value = data.DefaultValue ?? "";
            f._onSubmitCb = onSubmit;

            // 底：`40K_dropdown_bg` 九宫（整行 900×90）
            var tex = CardArt.MenuUi(ArtBg);
            if (tex != null)
            {
                MenuDraw.Nine(go, tex, r, BgBorder, BgTexW, BgTexH, qBase, BgTint, true, "Background");
            }
            else Debug.LogWarning("[Input] 取不到 `" + ArtBg + "`（输入框底没画）—— 导入器：`工具/import_original_art.py`");

            // `Text Area` = 整行四边内缩 10 / 6.5 ⇒ 880 × 77
            var area = new PxRect(r.x1 + AreaInsetX, r.y1 + AreaInsetY, r.x2 - AreaInsetX, r.y2 - AreaInsetY);
            var areaNode = MenuDraw.Node(go, "Text Area", area);

            // `Placeholder`：字号 40 / 基准 14 / auto[18~40] / 色 (0.972,0.972,0.972,0.5)
            f._ph = Label.Create(areaNode, "", Vector3.zero, 6, PhColor,
                                 new Vector2(0.5f, 0.5f), "Placeholder");
            if (f._ph != null)
            {
                f._ph.SetFace(TmpFont.Face.LatinCardBody);
                f._ph.SetRenderQueue(qBase + 1);
                f._ph.SetGlyphHeight(LayoutSpace.Px(PhFontPx));
                f._ph.SetAutoFitBox(LayoutSpace.Px(area.W), LayoutSpace.Px(area.H),
                                    PhAutoMinPx, PhFontPx, PhAutoBasePx);
            }

            // `Text`：字号 45 / 基准 14 / auto[18~45] / 折行开
            f._text = Label.Create(areaNode, "", Vector3.zero, 6, TxColor,
                                   new Vector2(0.5f, 0.5f), "Text");
            if (f._text != null)
            {
                f._text.SetFace(TmpFont.Face.LatinCardBody);
                f._text.SetRenderQueue(qBase + 1);
                f._text.SetGlyphHeight(LayoutSpace.Px(TxFontPx));
                f._text.SetAutoFitBox(LayoutSpace.Px(area.W), LayoutSpace.Px(area.H),
                                      TxAutoMinPx, TxFontPx, TxAutoBasePx);
            }

            // 命中区：点一下开始编辑（原版是 `TMP_InputField` 自己吃 `IPointerClickHandler`）
            var hitTr = MenuDraw.Hit(go, "Hit", r, qBase + 2, () => f.BeginEdit());
            if (hitTr != null) f._hit = hitTr.GetComponent<WindowButton>();

            f.Refresh();
            return f;
        }

        Action<string> _onSubmitCb;

        /// <summary>开始编辑（点命中区 ⇒ 这里）。幂等。</summary>
        public void BeginEdit()
        {
            if (_editing) return;
            _editing = true;
            if (!_hooked)
            {
                var kb = Keyboard.current;
                if (kb != null) { kb.onTextInput += OnText; _hooked = true; }
                else Debug.LogWarning("[Input] 没有 `Keyboard.current` ⇒ 这一格**打不了字**（如实说，不假装能输）");
            }
            _typed.Clear();
            Refresh();
        }

        /// <summary>结束编辑。<paramref name="keep"/> = 保留当前值（`false` = 丢弃、回到默认值）。</summary>
        public void EndEdit(bool keep)
        {
            if (!_editing) return;
            _editing = false;
            if (!keep) _value = _data != null ? (_data.DefaultValue ?? "") : "";
            Unhook();
            _typed.Clear();
            Refresh();
        }

        void Unhook()
        {
            if (!_hooked) return;
            var kb = Keyboard.current;
            if (kb != null) kb.onTextInput -= OnText;
            _hooked = false;
        }

        void OnText(char c)
        {
            if (!_editing || char.IsControl(c)) return;
            _typed.Add(c);
        }

        /// <summary>自检/无键盘环境的口子：直接喂一串字符（等价于 `onTextInput` 收进来的那些字）。</summary>
        public void FeedText(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            for (int i = 0; i < s.Length; i++) _typed.Add(s[i]);
        }

        /// <summary>每帧泵（play 模式由 `Update` 调；批处理/自检里可手调）。</summary>
        public void Tick()
        {
            if (!_editing) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { EndEdit(false); return; }
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                {
                    EndEdit(true);
                    var cb = _onSubmitCb;
                    if (cb != null) cb(_value);
                    return;
                }
                if (kb.backspaceKey.wasPressedThisFrame && _value.Length > 0)
                    _value = _value.Substring(0, _value.Length - 1);
            }
            if (_typed.Count == 0) return;
            for (int i = 0; i < _typed.Count; i++)
            {
                char c = _typed[i];
                if (!Accepts(_data != null ? _data.ContentType : TMP_InputField.ContentType.Standard, c)) continue;
                if (_maxLen > 0 && _value.Length >= _maxLen) break;
                _value += c;
            }
            _typed.Clear();
            Refresh();
        }

        void Update() { Tick(); }

        /// <summary>原版 `TMP_InputField.characterValidation` 的等价（prefab 实测 `None`，其余档按 TMP 的语义取）。</summary>
        static bool Accepts(TMP_InputField.ContentType t, char c)
        {
            switch (t)
            {
                case TMP_InputField.ContentType.IntegerNumber:
                case TMP_InputField.ContentType.Pin:
                    return c >= '0' && c <= '9';
                case TMP_InputField.ContentType.DecimalNumber:
                    return (c >= '0' && c <= '9') || c == '.' || c == ',' || c == '-';
                case TMP_InputField.ContentType.Alphanumeric:
                    return char.IsLetterOrDigit(c);
                case TMP_InputField.ContentType.Name:
                    return char.IsLetterOrDigit(c) || c == ' ';
                case TMP_InputField.ContentType.EmailAddress:
                    return !char.IsWhiteSpace(c);
                default:                     // Standard（prefab 实测值）/ Password / Custom
                    return true;
            }
        }

        /// <summary>按当前值/编辑态重画两格字（占位字只在空值时显示）。</summary>
        void Refresh()
        {
            bool empty = string.IsNullOrEmpty(_value);
            if (_ph != null)
            {
                _ph.gameObject.SetActive(empty);
                var data = _data;
                string label = data != null ? data.Label : null;
                if (string.IsNullOrEmpty(label) && data != null) label = data.Name;
                // 占位字过词条（原版 `InstantiateInput` 里那句）；`Loc.T` 缺键时原样返回 ⇒ 人写的字不会被吃掉
                _ph.SetText(string.IsNullOrEmpty(label) ? "" : Loc.T(label));
            }
            if (_text != null)
            {
                string shown = _value;
                if (_data != null && _data.ContentType == TMP_InputField.ContentType.Password && !string.IsNullOrEmpty(shown))
                    shown = new string('*', shown.Length);          // 🔶 我们挑的（掩码字符，见文件头 ③）
                if (_editing) shown = (shown ?? "") + "_";          // 🔶 我们挑的（光标，见文件头 ①）
                _text.SetText(shown ?? "");
            }
        }
    }
}
