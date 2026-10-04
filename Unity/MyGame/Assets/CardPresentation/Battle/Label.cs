// Label.cs — 世界空间文字（HUD 用）
//
// **两条后端**：
//   · **TMP**（`TextMeshPro` 世界空间组件）—— 拿得到中文字体资产时走这条，中文才画得出来。
//   · **自写 5×7 点阵 ASCII**（`TextCanvas` 画进贴图再贴 quad）—— 字体资产不在时的兜底。
//     和 `CardArt.Available` 一个路子：删掉字体资源游戏照样跑，只是没中文。
//
// ⚠️ 两条后端的 `WorldW` / `WorldH` 语义**必须一致**：都是「整块文字占的世界尺寸」，
//    摆位和 `Contains`（END TURN 的命中测试）全靠它。TMP 那条量的是 `textBounds`
//    —— 也就是**行盒**，和点阵那条的「行高 × 缩放」是同一回事，所以换后端不会让命中区变形。
//
// ⚠️ 字号按**拉丁大写高度**对齐（`TmpFont.FontSizeForCapHeight`），不是按汉字高度 ——
//    点阵字库只有大写 ASCII，按汉字对的话 HUD 的英文会小 28%。
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    public class Label : MonoBehaviour
    {
        /// <summary>1 像素 = 1/100 世界单位（点阵后端用）</summary>
        const float PixelsPerUnit = 100f;

        public int scale = 3;
        public Color color = Color.white;

        /// <summary>锚点：(0,0) 左下、(0.5,0.5) 中心、(1,1) 右上</summary>
        public Vector2 anchor = new Vector2(0.5f, 0.5f);

        // ---- 点阵后端 ----
        MeshRenderer _mr;
        MeshFilter _mf;
        int _texW = 1, _texH = 1;

        // ---- TMP 后端 ----
        TextMeshPro _tmp;
        float _tmpW = 1e-4f, _tmpH = 1e-4f;

        string _text;

        /// <summary>贴图/文字块背后的世界尺寸（HUD 摆位/命中测试要用）</summary>
        public float WorldW { get { return _tmp != null ? _tmpW : _texW / PixelsPerUnit; } }
        public float WorldH { get { return _tmp != null ? _tmpH : _texH / PixelsPerUnit; } }

        /// <summary>能不能出汉字。拿不到字体资产就是 false（那条路只画得出 ASCII）</summary>
        public bool CanRenderChinese { get { return _tmp != null; } }

        public static Label Create(Transform parent, string text, Vector3 pos, int scale,
                                   Color color, Vector2 anchor, string name = null)
        {
            var go = new GameObject(name ?? ("label_" + text));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var l = go.AddComponent<Label>();
            l.scale = Mathf.Max(1, scale);
            l.color = color;
            l.anchor = anchor;

            if (TmpFont.Available) l.BuildTmp();
            if (l._tmp == null) l.BuildDot();        // 字体资产不在 / TMP 没建起来 → 退回点阵

            l.SetText(text);
            return l;
        }

        public string Text { get { return _text; } }

        /// <summary>当前渲染队列（**自检 / 诊断用**，2026-09-28 加：分层断言需要读回来）。
        /// ⚠️ 点阵侧读的是**实例材质** `_mr.material` —— `SetRenderQueue` 走的就是它；
        /// 读 `sharedMaterial` 会拿到**改之前**的值（那份是共享的、我们没动）。</summary>
        public int RenderQueue
        {
            get
            {
                if (_tmp != null && _tmp.fontMaterial != null) return _tmp.fontMaterial.renderQueue;
                if (_mr != null && _mr.material != null) return _mr.material.renderQueue;
                return -1;
            }
        }

        /// <summary>改**渲染队列**（见 `ImageQuad.SetRenderQueue` 的注释）。
        /// 面板上的文字要跟着面板一起压住一切时用它 —— `fontMaterial` 会**实例化**一份，
        /// 所以改这个不会波及别处的文字。</summary>
        public void SetRenderQueue(int q)
        {
            if (_tmp != null)
            {
                // 🔴 **必须先实例化一份**（2026-09-27 实测补的）——
                //    TMP 的 `fontMaterial` 在**还没实例过**的时候返回的**就是字体资产里那份共享材质**，
                //    直接写它 = 改**工程资产**（而且是**落盘**的）：实测把
                //    `Resources/Fonts/Warpforge Trait TextSprites.asset` 的 `m_CustomRenderQueue`
                //    写成了 `3009`，`git status` 里挂出来。下面这条注释原来写「会实例化一份」—— **不成立**。
                if (_tmp.fontMaterial == _tmp.fontSharedMaterial)
                    _tmp.fontMaterial = new Material(_tmp.fontSharedMaterial);
                if (_tmp.fontMaterial != null) _tmp.fontMaterial.renderQueue = q;
                return;
            }
            // 🔴 2026-09-21 补：**点阵后端原来什么都不做** ⇒ 用点阵渲染的文字
            //    **永远留在默认队列 3000**，被队列更大的面板盖住（tooltip 就是这么「面板在、字不在」的）。
            //    ⚠️ 走 `_mr.material`（**Unity 的 `.material` getter 一定会实例化**），
            //    别用 `sharedMaterial` —— 那同样是改共享材质（同上面 TMP 那条）。
            if (_mr != null && _mr.sharedMaterial != null) _mr.material.renderQueue = q;
        }

        public void SetText(string text)
        {
            if (text == null) text = "";
            if (text == _text) return;
            _text = text;
            // 字数变了就可能从「没有图标」变成「有图标」—— 每次重算，别缓存（很便宜）
            _iconBoost = CardIcons.FontScaleFor(text);

            if (_tmp != null) SetTextTmp(text);
            // ⚠️ 点阵那条路不认识 `<sprite>` —— 原样喂进去会画出一串空格/乱码（见 `CardIcons.StripTags`）
            else SetTextDot(CardIcons.StripTags(text));
        }

        public void SetColor(Color c)
        {
            color = c;
            if (_tmp != null) { _tmp.color = c; return; }

            _mr.sharedMaterial.color = Color.white;     // 点阵那条：颜色已经烘进贴图了
            string t = _text;
            _text = null;
            SetText(t);
        }

        /// <summary>世界坐标是不是点在这块字上（结束回合按钮的命中测试用）</summary>
        public bool Contains(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return l.x >= -anchor.x * WorldW && l.x <= (1 - anchor.x) * WorldW
                && l.y >= -anchor.y * WorldH && l.y <= (1 - anchor.y) * WorldH;
        }

        /// <summary>
        /// 🆕 2026-09-21：这个世界坐标落在哪个 `<link=…>` 上；没命中返回 null。
        /// **实现只有一份** —— 转发给 `TmpFont.LinkAt`（卡面的 TMP 不经过 `Label`，
        /// 两边必须共用同一份判据，见那里的注释）。点阵后端没有 link 概念 ⇒ 返回 null。
        /// </summary>
        public string LinkAt(Vector3 world, Camera cam)
        {
            return TmpFont.LinkAt(_tmp, world, cam);
        }

        // ==================================================================
        //  TMP 后端
        // ==================================================================

        /// <summary>点阵那条的字号：7×scale 像素高的大写，换算成世界单位再问 TMP 要字号</summary>
        const float CapHeightWorldOfScale = 7f / PixelsPerUnit;   // 每个 scale 档 = 0.07 世界单位

        /// <summary>非整数档的大写高度（世界单位）。> 0 时**盖过** `scale` —— 面板那种
        /// 字号是从原版的 px 值换来的，不是整数档。点阵后端只能就近取整。</summary>
        float _capHeight;

        /// <summary>非整数档的**汉字**高度（世界单位）。优先级最高。
        /// 中文文案按它定档（一个汉字 ≈ 1 em，而拉丁大写只有 0.72 em —— 差很多，别混用）</summary>
        float _glyphHeight;

        float CapWorld { get { return _capHeight > 0f ? _capHeight : CapHeightWorldOfScale * Mathf.Max(1, scale); } }

        /// <summary>现在的大写高度（世界单位）。面板要按版面宽度回缩字号时读它 ——
        /// 字号正比于大写高度，所以「缩多少比例」能直接作用回去</summary>
        public float CapHeightWorld { get { return CapWorld; } }

        /// <summary>现在的**汉字**高度（世界单位）。`SetGlyphHeight` 定过就是它；
        /// 只按大写定过的（HUD）就用**实测的两个比例**换算，别写死 0.72</summary>
        public float GlyphHeightWorld
        {
            get
            {
                if (_glyphHeight > 0f) return _glyphHeight;
                float k = TmpFont.WorldCapPerFontSize / TmpFont.WorldGlyphPerFontSize;
                return k > 0f ? CapWorld / k : CapWorld;
            }
        }

        /// <summary>
        /// 按「大写字母占 `capWorld` 个世界单位高」定字号。
        /// ⚠️ 点阵后端**只支持整数档**，这里会就近取整（没有字体资产时才走那条路）。
        /// </summary>
        public void SetCapHeight(float capWorld)
        {
            SetSizes(capWorld, 0f);
        }

        /// <summary>
        /// 按「一个**汉字**占 `worldHeight` 个世界单位高」定字号。
        /// 中文文案用它 —— 汉字约占 1 em，而拉丁大写只有约 0.72 em，
        /// 拿 `SetCapHeight` 喂中文会大 39%。
        /// </summary>
        public void SetGlyphHeight(float worldHeight)
        {
            SetSizes(0f, worldHeight);
        }

        /// <summary>
        /// **让文字折行**，折行宽度 = `worldWidth` 个世界单位。
        /// 🔴 为什么需要它：`BuildTmp()` 里给 HUD 单行标签**硬写了 `NoWrap`**，
        ///    所以面板里的长文案（弹窗正文 / 语音台词）**不显式调它就会画出框外** ——
        ///    这正是 `项目任务.md` §三 12.2「语音台词溢出台词框」的根因那一半
        ///    （另一半是 `UnitChatPanel` 建文本时**只给了一个点、从没传框宽**）。
        /// ⚠️ **TMP 在对象没激活时量不出尺寸** —— 必须在 `SetActive(true)` **之后**调（`CLAUDE.md` §三 那条坑）。
        /// </summary>
        public void SetWrapWidth(float worldWidth) { TmpFont.SetWrapWidth(_tmp, worldWidth); }

        /// <summary>
        /// 逐行**左对齐**（TMP 的 `m_HorizontalAlignment = 1`）。
        /// 🔴 **为什么需要它**：`TmpFont.NewText` 把所有 TMP 统一建成 `Center`，而原版这批文字
        ///    **多数是 Left**（`ChatText` = 1 · `Mission Header` / 卡名 / 每日行全是 1）。
        ///    `AlignLeftOn` 挪的是**整块**的位置，**管不了折行之后每一行在块内怎么排** ——
        ///    多行时短的那些行会居中，与原版不一致（这个差别只有真折行时才看得见）。
        /// `TextAlignmentOptions.Left` = H=Left + V=Middle，正好是原版 `m_HorizontalAlignment=1`
        /// + `m_VerticalAlignment=512` 那一对。
        /// ⚠️ 要在**量尺寸之前**调；调完 `textBounds` 会变，所以紧跟着要有一次
        ///    `ForceMeshUpdate` + `RefreshBounds`（走 `SetAutoFitBox` 就会顺带做掉）。
        /// </summary>
        public void SetAlignLeft()
        {
            if (_tmp == null) return;                       // 点阵后端没有「对齐」这回事
            _tmp.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>自检用：现在是不是**折行**模式（原版 `m_TextWrappingMode = 1`）。
        /// 点阵后端没有这个概念 ⇒ 恒 false（如实报，不猜）。
        /// ⚠️ 第三档 `3`（`PreserveWhitespaceNoWrap`）在这里**答 false** —— 但**不许拿这个 false 当「= 0」**：
        /// 那一档与 `0` 只在「折不折行」这一件事上同档，见 <see cref="WrappingMode"/>。要断那一档读 `WrappingMode`。</summary>
        public bool Wrapping
        {
            get { return _tmp != null && _tmp.textWrappingMode == TextWrappingModes.Normal; }
        }

        /// <summary>🔴 **2026-10-07（A77①）**：当前换行模式**按原版 `m_TextWrappingMode` 的原文**报出来
        /// —— `0` `NoWrap` · `1` `Normal` · `2` `PreserveWhitespace` · `3` `PreserveWhitespaceNoWrap`
        /// （枚举值出处 = `TMP_Text.cs:100`：`{ NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 }`）。
        /// **点阵后端返 `-1`**（那后端没有「折行」这回事，如实报、别猜 0）。
        ///
        /// <para>🔴 **为什么要开这个口（原版真有第三档）**：实测 2 处原版件是 `折行=3` ——
        /// `Shell/ProfileTab.cs` 改名窗输入框里的 `Text` · `Deck/DeckRuntime.cs` 筛选栏搜索框的 `Text`
        /// （判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 14 --md`
        /// 的 `折行=` 列）。旧写法（`SetWrapping(bool)` + `Wrapping` 只判 `== Normal`）**表达不了它**
        /// ⇒ 拿 `false` 顶替 = 把 `3` **静默降级成 `0`**。</para>
        ///
        /// <para>🔴 **`3` 与 `0` 的关系（有判据的那一半 / 没有判据的那一半）**：
        /// · **有判据**：在「**折不折行**」这一件事上它们**同档** —— `TMP_Text.cs:4485`
        ///   （`if (textWrapMode != NoWrap && textWrapMode != PreserveWhitespaceNoWrap && …)` 才断行）与
        ///   `:4731`（保存换行状态那处）都把这两个值并列。
        /// · **没有判据**：**空白保留**那一半**不同**（`:4461` 把 `PreserveWhitespace`/`PreserveWhitespaceNoWrap`
        ///   单列一支，`0` 不在其中）⇒ 「`3` 与 `0` 在我们这套排版下等不等价」**至今没有人给过判据**
        ///   （普查报告原文如实留白）⇒ ⛔ **不许当等价用**，要 `3` 就写 `3`。
        /// · 📌 顺带一条旁证（为什么输入框会是 `3`）：**这是 TMP 自己给单行输入框写的档** ——
        ///   `TMP_InputField.SetTextComponentWrapMode()`（`TMP_InputField.cs:4618-4627`）：
        ///   `multiLine ? Normal : PreserveWhitespaceNoWrap`。原版那两处都是 `TMP_InputField` 的 `Text`。</para></summary>
        public int WrappingMode
        {
            get { return _tmp != null ? (int)_tmp.textWrappingMode : -1; }
        }

        /// <summary>自检用：现在开没开**自动缩放**（原版 `m_enableAutoSizing`）。点阵后端恒 false（如实报）。
        /// 🔴 **判「原版这一处 `auto` 是关的」只能读它** —— 读 `FontSizeMin/Max` 分不出来
        /// （TMP 出厂就带一对默认值 `m_fontSizeMin/Max`，没开自适应时它们只是**死值**，见 `FilterPanelModel`
        /// 那条「`min18/max72` 是不生效的残留值」）。</summary>
        public bool AutoSizing { get { return _tmp != null && _tmp.enableAutoSizing; } }

        /// <summary>🆕 **2026-10-04**：显式设换行模式（两档：折行 / 不折行）。
        /// 🔴 **为什么需要它**：`SetAutoFitBox` 内部会调 `SetWrapWidth`，而那个**无条件**把
        /// `textWrappingMode` 设成 `Normal` ⇒ 「要自适应、但原版**不折行**」的件（`name`/`type`/`Available Counter`/
        /// `Price…/text` 都是 `折行=0`）会被**悄悄打开折行**（A34-F4 新加的断言当场报出来，19 份 ×2）。
        /// ⇒ 调用方在 `SetAutoFitBox` **之后**用它把模式还原成自己那一档。
        /// ⚠️ 表达不了原版第三档 `3` ⇒ 那一档走 <see cref="SetWrappingMode"/>（⛔ 别用 `false` 顶替）。
        /// 🔴 **2026-10-07（A205）**：改模式**顺带把版面推下去**（内部调 <see cref="ForceRelayout"/>）——
        /// 不推的话字段说了、画面没变（批处理没有帧循环），见那个函数的注释。</summary>
        public void SetWrapping(bool on)
        {
            SetWrappingMode((int)(on ? TextWrappingModes.Normal : TextWrappingModes.NoWrap));
        }

        /// <summary>🆕 **2026-10-07（A77①）**：**按原版 `m_TextWrappingMode` 的原文**设模式（`0`/`1`/`2`/`3`）。
        /// 传别的值 ⇒ **出声**（`Debug.LogWarning`）并**什么都不设**（不静默降级到 0）。
        /// 模式**真的变了**才重排（<see cref="ForceRelayout"/>）；点阵后端直接返回。
        /// ⚠️ 它会挪 TMP 子节点 ⇒ **要在对齐/量宽之前调**（调用顺序：`SetAutoFitBox` → 本函数 → 对齐 → 量）。</summary>
        public void SetWrappingMode(int originalMode)
        {
            if (_tmp == null) return;
            TextWrappingModes m;
            switch (originalMode)
            {
                case 0: m = TextWrappingModes.NoWrap; break;
                case 1: m = TextWrappingModes.Normal; break;
                case 2: m = TextWrappingModes.PreserveWhitespace; break;
                case 3: m = TextWrappingModes.PreserveWhitespaceNoWrap; break;
                default:
                    Debug.LogWarning("[Label] ⚠️ 原版 `m_TextWrappingMode = " + originalMode
                                     + "` 不在 0/1/2/3 里 ⇒ 这一处**没设**（出声，不静默降级）");
                    return;
            }
            if (_tmp.textWrappingMode == m) return;      // 没变 ⇒ 别白重排一次（既有调用点行为不变）
            _tmp.textWrappingMode = m;
            ForceRelayout();
        }

        /// <summary>🆕 **2026-10-07（A205）**：把一次「改完折行模式 / 改完字号相关参数之后」的版面**真正推下去**。
        ///
        /// <para>🔴 **为什么必须有它**：`textWrappingMode` 的 setter 里只有 `SetVerticesDirty()` + `SetLayoutDirty()`
        /// （`TMP_Text.cs:747`）—— **批处理没有帧循环**（`CLAUDE.md` §三 那条）⇒ 那张 mesh 还停在**上一版**：
        /// **字段说「不折行」、画面却还是折行的**（静默不一致）。A62 那次「补一行 `SetWrapping(false)`」的修法
        /// 全落在这一口上（判据 → `Battle/Label.cs` 的 `SetWrapping` 头 + `资料/普查产出_1007/波8_Label折行族.md`）。</para>
        ///
        /// <para>实现 = **`SetFontSize(传当前值)`** + `RefreshBounds()`：
        /// · 传的是**当前值本身** ⇒ TMP 的 `fontSize` setter 逐位相等 ⇒ **早退**
        ///   （`TMP_Text.cs:465`：`if (m_fontSize == value) return;`）⇒ `m_fontSizeBase` 一个字节不动，
        ///   只有紧跟的那次 `ForceMeshUpdate()` 生效：按**当前**的折行模式 + 当前的 `[fontSizeMin,fontSizeMax]`
        ///   重新收敛一次（收敛结果与上一次相同 ⇒ **幂等**）。
        ///   ⛔ **别传 `px/108`** —— 那会大 2.7 倍（见 `SetFontSize` 头那条）。
        /// · 重排后的尺寸/摆位要落回字段（`_tmpW/_tmpH`），否则 `WorldW` 还是旧版面的值。</para>
        ///
        /// <para>⚠️ **它会挪 TMP 子节点**（`RefreshBounds` 按新宽度重新居中）⇒ 调用方若在这之前调过
        /// `AlignLeftOn`/`AlignRightOn`（两个都按 `WorldW` 算），**重排之后要再对齐一次**。</para>
        /// <para>⚠️ 点阵后端（`_tmp == null`）没有折行/mesh 这回事 ⇒ 什么都不做（如实，不假装）。</para>
        /// <para>📌 用法先例（本函数就是从它收上来的）：`Shell/PracticeModePopup.cs` 的 `RelayoutNow` —— 那边
        /// 当时`Battle/Label.cs` 不在白名单里，只能绕；现在公共件里有了，那一处**可以**退休（不在本件范围）。</para></summary>
        public void ForceRelayout()
        {
            if (_tmp == null) return;
            SetFontSize(_tmp.fontSize);     // 值相同 ⇒ setter 早退，只要那一次 `ForceMeshUpdate`
            RefreshBounds();                // 重排后的 `_tmpW/_tmpH` 要落回字段（否则 WorldW 还是旧版面的值）
        }

        /// <summary>自检用：TMP 现在排出来**几行**（判「折行真的生效了」，不是只把字缩小了）。
        /// ⚠️ 要在 `ForceMeshUpdate` 之后读；`textInfo` 还没建时返回 0。</summary>
        public int LineCount
        {
            get { return _tmp != null && _tmp.textInfo != null ? _tmp.textInfo.lineCount : 0; }
        }

        /// <summary>
        /// **直接定 TMP 的 `fontSize`（世界单位）**。
        /// 用它是为了**逐字照抄原版的字号**：原版 TMP 的 `m_fontSize` 是 **UI 画布像素**
        /// （例如导航钮标签 `33` · `Player Name` `32` · 聊天两行 `18`），
        /// 我们的设计空间是 **1080px = 10 世界单位** ⇒ 传 **`原版px / 108f`** 就等价。
        /// ⚠️ 调它之前 `_tmp` 必须已经建好（`Create` 之后、且 `TmpFont.Available`）；
        /// 没字体资产时会静默无效（那本来就走点阵后端，点阵只有整数档 —— 见 `SetSizes`）。
        /// </summary>
        public void SetFontSize(float worldSize)
        {
            if (_tmp == null || worldSize <= 0f) return;
            _tmp.fontSize = worldSize;
            _tmp.ForceMeshUpdate();
        }

        /// <summary>当前的 TMP `fontSize`（**不是世界单位**，见 <see cref="SetAutoFitBox"/>）。</summary>
        public float FontSize { get { return _tmp != null ? _tmp.fontSize : 0f; } }

        /// <summary>🆕 2026-10-03：**字距**（原版 TMP 的 `m_characterSpacing`，**原样传**、不换算）。
        /// 🔴 原来三处带字距的原版文字都**没照做**，理由写的是「`Label` 没有字距接口」—— 那只是**没加**，不是加不了：
        ///   遭遇/排位窗 `Window Title` **5** · `DivisionText` **−2.6** · `ChangeRankedToggle` **−4**
        ///   （正本 `资料/阶段二_战斗入口_原版规格.md` §二 那几张表里逐条标着）。
        /// ⚠️ **点阵后端（`_tmp == null`）没有「字距」这回事** ⇒ 什么都不做并**出声**（红线：不许静默失败）。</summary>
        public void SetCharSpacing(float v)
        {
            if (_tmp == null)
            {
                Debug.Log("[Label] ⚠️ 这一处要 `characterSpacing = " + v + "`，但**点阵后端没有字距** ⇒ 没生效（出声，不静默）");
                return;
            }
            _tmp.characterSpacing = v;
            _tmp.ForceMeshUpdate();
        }

        /// <summary>当前字距（自检用）。</summary>
        public float CharSpacing { get { return _tmp != null ? _tmp.characterSpacing : 0f; } }

        /// <summary>🔴 **「TMP 的 `fontSize`」↔「画布像素」只此一份换算**：`fontSize × 本式 = 画布 px`。
        /// 判据 = `TmpFont` 实测的「字形世界高 ÷ fontSize」（≈0.0948）× **108 px / 世界单位**
        /// （`LayoutSpace.DesignPxH / DesignHeight`）。
        /// ⚠️ 与本文件顶部那个**点阵后端**的私有 `PixelsPerUnit = 100f` **无关**（那个管字块贴图的整数档）。</summary>
        public static float FontSizeToPx(float fontSize)
        {
            return fontSize * TmpFont.WorldGlyphPerFontSize * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
        }

        /// <summary>当前**实际生效**的字号换算成「像素」口径（= `fontSize × WorldGlyphPerFontSize × 108`）。
        /// **自检拿它跟原版的 `m_fontSize` 比**（原版那也是画布像素）。
        /// ⚠️ 别用 `CapHeightWorld` / `GlyphHeightWorld` 去量 —— 那两个是**回读传入值**的伪测量（见 `已知的坑.md`）。</summary>
        public float FontPxNow { get { return _tmp != null ? FontSizeToPx(_tmp.fontSize) : 0f; } }

        /// <summary>🆕 2026-10-04（A50①）：TMP 现在的**自适应下限/上限**（`fontSize` 单位，**不是**世界单位、
        /// 也不是 px —— 倍率见 <see cref="FontSizeToPx"/>）。自检要能把它读出来**断死**：
        /// 原版 `Timer Text` 是 `m_fontSizeMin/Max = 10/32`、而它的 `m_fontSize` 是 **30.6**
        /// —— **上限 ≠ 字号**（判据 → `Shell/OfferContainer.cs` 的 `TimerTextFit` / `资料/待办判据_审查发现_1004.md` §A34-F2）。
        /// `_tmp == null`（点阵后端）⇒ 恒 0：那后端没有「自适应」这回事，如实报、别猜。</summary>
        public float FontSizeMin { get { return _tmp != null ? _tmp.fontSizeMin : 0f; } }
        public float FontSizeMax { get { return _tmp != null ? _tmp.fontSizeMax : 0f; } }

        /// <summary>
        /// **给文字一个框**（宽 × 高，世界单位），并开**自动缩放**（TMP 的 `enableAutoSizing`）。
        ///
        /// 为什么需要：原版主菜单那几处文字都带 `m_enableAutoSizing=1`
        /// （导航标签 **18→33** · `Player Name` **10→32** · 模式卡标题 **18→72**）——
        /// 也就是说**字号是自适应出来的**，卡在框里；我们只按最大值摆会溢出
        /// （实测：`COLLECTION` 在 146.9px 宽的条里、em 33px 会**画出框外被裁**）。
        ///
        /// <para>🔴 **`minPx` / `maxPx` 的口径 = 「画布像素」，就是原版的 `m_fontSizeMin` / `m_fontSizeMax`**
        /// —— 传**原版那两个字段的原文**即可，本函数自己折成 TMP 的 `fontSize` 单位
        /// （倍率见 <see cref="FontSizeToPx"/>：1 个 fontSize ≈ 10.24 画布 px）。</para>
        ///
        /// <para>🔴 **上限是「绝对天花板」，不是「调用方那个字号」**（2026-10-04 · A50① 修）：
        /// TMP 的自适应是**在 `[fontSizeMin, fontSizeMax]` 里二分**（`TextMeshPro.cs:4139-4149`
        /// 「increase font size to fill text container」那一支，只涨到 `m_fontSizeMax` 为止；
        /// 起点 = `Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`，见 `TextMeshPro.cs:2149`）
        /// ⇒ 文案短的时候**会一直涨到 `fontSizeMax`**。
        /// 而原版这两个数**可以不等**：`Timer Text`（商店 19 变体里 `TypeFs=34` 的那 9 份）是
        /// `m_fontSize = **30.6**` 而 `m_fontSizeMax = **32**`。旧写法把 `fontSizeMax` 设成「调用方字号」
        /// ⇒ 天花板矮 **1.4px**，短文案永远画小。
        /// 现在按比例折：`cur`（= 调用方要的那个字号）对应 `NominalPx()`，`minPx`/`maxPx` 各自按同一比例
        /// 换成 fontSize 单位。**`maxPx == nominalPx` 时与旧写法逐位相同**（绝大多数调用点都是这样）。</para>
        ///
        /// ⚠️ TMP 在对象没激活时量不出尺寸 —— 要在 `SetActive(true)` **之后**调（`CLAUDE.md` §三 那条坑）。
        /// </summary>
        public void SetAutoFitBox(float worldW, float worldH, float minPx, float maxPx)
        {
            if (_tmp == null) return;
            SetWrapWidth(worldW);                       // 顺带把折行宽度也设上（同一份 sizeDelta）
            if (worldH > 0f)
            {
                var d = _tmp.rectTransform.sizeDelta;
                _tmp.rectTransform.sizeDelta = new Vector2(d.x, worldH);
            }
            // 🔴 **`minPx`/`maxPx` 是「像素」，不是 TMP 的 `fontSize`** —— TMP 的 fontSize **不是世界单位**
            //    （实测：字形世界高 ÷ fontSize ≈ **0.0948**；`33/108` 的世界高对应 fontSize **3.22**，差 10.55 倍）。
            //    第一版把 `33/108` 直接填进 `fontSizeMax` ⇒ TMP 被压到 **0.3px**、整条标签直接看不见。
            // 🔴 **上限不能取 `_tmp.fontSize`**（2026-09-25 修）：TMP 的自适应**会把结果写回 `m_fontSize`**
            //    ⇒ 同一个 `Label` **被调第二次**时读到的是**上一轮缩过**的值，于是
            //    `fontSizeMax` 一档一档往下走、`fontSizeMin` 跟着降。
            //    实测（`Editor/ChatBoxProbe.cs`，台词框 440.9×81.7/10~33 那套）：连说三句
            //    **22.01 → 21.50 → 13.82 px**，全量自检里跑到第 5、6 句时字号只剩 **0.5 px**
            //    （台词渲染成 17.3×0.8 px，几乎看不见）。**换新 label 的地方看不出来，只坑复用同一个 label 的**。
            //    `TmpFontSize()` = 调用方通过 `SetGlyphHeight`/`SetCapHeight` 要的那个字号，**不含自适应结果**。
            float cur = TmpFontSize();
            if (cur <= 0f || minPx <= 0f || maxPx <= 0f) return;
            float nomPx = NominalPx();                  // 「cur 折算成画布 px」= 本工程唯一那条 px 口径（见它的注释）
            if (nomPx <= 0f) return;
            // 🔴 **2026-10-04（A57③）：先关自适应、再写 `fontSize`** —— TMP 只在 `!m_enableAutoSizing` 时
            //    才回写 `m_fontSizeBase`（`TMP_Text.cs:467`），而**起点就是 base**
            //    （`TextMeshPro.cs:2149`：`m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`）。
            //    旧写法没有这一行：同一个 label 被**第二次**调时自适应还开着 ⇒ base 停在**第一次**那个值
            //    （起点是旧值；二分最后仍收敛到「装得下的最大号」，所以**影响小**，但字段是错的）。
            //    ⚠️ **第一次调用逐位不变**：那一次 `m_enableAutoSizing` 本来就是 `false`（TMP 出厂值），
            //    这一行 setter 走 `m_enableAutoSizing == value` 早退 ⇒ 等于什么都没做。
            //    ⚠️ **残留一个洞（如实说）**：`fontSize` 的 setter 还有一条 `m_fontSize == value` 早退
            //    ⇒ 第二次调用时若 TMP 正好已经停在 `cur`，base 还是刷不到。TMP 没有公开的 `fontSizeBase` 口
            //    （`m_fontSizeBase` 是 `protected`），要根治得先把 `fontSize` 拨到别的值 —— 那会造成
            //    一次多余的重排，代价大于收益，**不做**；这里只保证「顺序对」。
            _tmp.enableAutoSizing = false;
            _tmp.fontSize = cur;                        // 复位到「没缩过」的大小（这一下顺带回写 `m_fontSizeBase`）
            _tmp.fontSizeMax = cur * (maxPx / nomPx);   // 🔴 原版 `m_fontSizeMax`（**不是** cur，见方法头 A50①）
            _tmp.fontSizeMin = cur * (minPx / nomPx);   // 🔴 原版 `m_fontSizeMin`（旧写法 = cur·minPx/maxPx，两者只在 maxPx==nomPx 时相等）
            _tmp.enableAutoSizing = true;
            _tmp.ForceMeshUpdate();
            RefreshBounds();     // 🔴 字号变了 ⇒ 尺寸/摆位都要重算（不然 `WorldW` 还是缩之前的值）
        }

        /// <summary>`cur`（= <see cref="TmpFontSize"/> 那一档）折算成**画布像素** —— 也就是调用方要的那个字号，
        /// 在**本工程唯一那条 px 口径**里是多少：`px = fontSize × WorldGlyphPerFontSize × 108`
        /// （<see cref="FontSizeToPx"/>；自检拿它跟原版 `m_fontSize` 比的就是这一条）。
        /// ⚠️ 与 `TmpFontSize()` 一样**不含自适应结果**（否则第二次调用会拿缩过的值当基准，同 `SetAutoFitBox` 那条）。
        /// <para>🔴 **2026-10-05 就地订正（第 3 条：两条 `Set*` 路必须落到同一个 px）**：
        /// 本函数原来直接 `world × 108`（`world = _glyphHeight &gt; 0 ? _glyphHeight : CapWorld`）——
        /// 这对 `SetGlyphHeight(px/108)` 那一路**恰好**等于上面那个式子
        /// （`cur = _glyphHeight ÷ Wglyph` ⇒ `FontSizeToPx(cur) = _glyphHeight × 108`），
        /// 但走 `SetCapHeight(px/108)` 的件算出来的却是**大写高**：而本工程「px」的标称口径是**汉字墨高**
        /// ⇒ 比它小 `Wcap ÷ Wglyph`（实测 0.0779/0.0948 = 0.822 ⇒ **差 1.2169 倍**）
        /// ⇒ 同一对 `minPx/maxPx` 会因为调用方用哪个 `Set*` 而量出**两个意思**
        /// （`fontSizeMax = cur × maxPx/nomPx` 偏大 1.2169 倍 ⇒ 自适应出来的字大一圈）。
        /// 现在改成**从 `cur` 反算**，两条路自动一致。</para>
        /// <para>📌 **当天全量核过调用点**（`grep -rn "SetAutoFitBox("`，不含注释与定义：**本轮之前全工程 41 处**，
        /// 逐处追到它那个 label 的建法；本轮 §4.6 那条新断言自己再加 1 处）：
        /// **没有任何一处**同时用 `SetCapHeight` + `SetAutoFitBox` —— `grep -rn "SetCapHeight"` 全工程只有
        /// **9 个真实调用点**、分布在 **6 个文件**里（`Battle/BattleDriver.cs:7470` ·
        /// `Battle/MulliganPanel.cs:158/187/270` · `Battle/SettingsPanel.cs:206/250` · `Core/CardFeel.cs:1025` ·
        /// `Core/Tooltip.cs:130` · `Shell/ShellRuntime.cs:268`），
        /// **那 6 个文件一个都不调 `SetAutoFitBox`**（逐文件数过），且那几个 label 不逃逸到别处；
        /// 而那 41 处**全部走 `SetGlyphHeight` 那一路**（各自经由 `MenuDraw.Text/TextBox` 或本窗同形的
        /// `Text` 助手，逐个追过）⇒ **本次改动对现有画面是恒等的**，它修的是**潜伏**那一档
        /// （哪天有人给 Cap 那一路接上自适应，就会静默大 1.22 倍）。
        /// 判据 → `Editor/BattleScene.cs` §4.6「两条路落到同一个 10/32px」那条。
        /// （`_iconBoost` 恒 1 —— `CardIcons.FontScaleFor` 就是 `return 1f`，两版逐位相同。）</para></summary>
        float NominalPx()
        {
            // ⚠️ 走 `FontSizeToPx`（**唯一那份 px 口径**）—— 别自己再乘一次 108，那等于把口径写成两份
            return FontSizeToPx(TmpFontSize());
        }

        void SetSizes(float capWorld, float glyphWorld)
        {
            _sizeCalls++;
            if (capWorld <= 0f && glyphWorld <= 0f) return;
            _capHeight = capWorld;
            _glyphHeight = glyphWorld;

            // 点阵后端只有整数档：拿「这一档大约多高」换算，别让它退化成 1 档
            float approx = glyphWorld > 0f ? glyphWorld : capWorld;
            scale = Mathf.Max(1, Mathf.RoundToInt(approx / CapHeightWorldOfScale));

            if (_tmp != null)
            {
                string t = _text;                 // 强制重排（`SetText` 同串会早退）
                _text = null;
                SetText(t);
            }
        }

        float TmpFontSize()
        {
            float k = _iconBoost;                 // 含图标时 > 1：图标按世界单位摆、不随字号缩，见 `_iconBoost`
            if (_glyphHeight > 0f) return TmpFont.FontSizeForGlyphHeight(_glyphHeight) * k;
            return TmpFont.FontSizeForCapHeight(CapWorld) * k;
        }

        /// <summary>
        /// 含**行内图标**（`<sprite name="…">`）时字号要乘的系数 —— 现在 `CardIcons.FontScaleFor`
        /// **恒返回 1**（试过放大，反而把字号缩死，原因写在那个函数的注释里）。
        /// 留着这个乘法是为了：万一以后真要补偿，只改 `FontScaleFor` 一处、这里自动跟上。
        /// </summary>
        float _iconBoost = 1f;

        int _sizeCalls;

        /// <summary>自检用：把内部的 sizing 状态原样吐出来（截图和 `GlyphHeightWorld` 都看不出「到底谁把它清成 0 了」）。
        /// 🆕 2026-10-04（A50①）：补上 `fontSizeMin`/`fontSizeMax`（`SetAutoFitBox` 真的写进去的那两个数）。</summary>
        public string DumpSizes()
        {
            return "cap=" + _capHeight.ToString("R") + " glyph=" + _glyphHeight.ToString("R")
                 + " scale=" + scale + " calls=" + _sizeCalls
                 + " tmp=" + (_tmp != null) + " fontSize=" + TmpFontSize().ToString("R")
                 + " fontSizeMin=" + FontSizeMin.ToString("R") + " fontSizeMax=" + FontSizeMax.ToString("R")
                 + " tmpW=" + _tmpW.ToString("R") + " tmpH=" + _tmpH.ToString("R");
        }

        /// <summary>`scale` 档对应的 TMP 字号。自检报数用 —— 实例走 <see cref="TmpFontSize"/></summary>
        public static float FontSizeFor(int scale)
        {
            return TmpFont.FontSizeForCapHeight(CapHeightWorldOfScale * Mathf.Max(1, scale));
        }

        void BuildTmp()
        {
            // 点阵那套的 MeshFilter / MeshRenderer **一个都不要挂** —— 两套同时画就是重影
            _tmp = TmpFont.NewText(transform, "text", "", TmpFontSize(), color);
            if (_tmp != null) _tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void SetTextTmp(string text)
        {
            _tmp.text = text;
            _tmp.fontSize = TmpFontSize();
            _tmp.ForceMeshUpdate();                  // 批处理没有帧循环，尺寸得手动推
            RefreshBounds();
        }

        /// <summary>量一次当前渲染出来的尺寸（`_tmpW/_tmpH`）并**把整块摆进锚点里**。
        /// 🔴 **改了字号之后必须重跑它** —— 否则 `WorldW/WorldH` 还是旧值
        /// （`SetAutoFitBox` 开 autosize 之后字号会变，第一版就是漏了这一步 ⇒ 量出来的宽度是缩之前的）。</summary>
        public void RefreshBounds()
        {
            if (_tmp == null) return;
            var b = _tmp.textBounds;
            _tmpW = Mathf.Max(Mathf.Abs(b.size.x), 1e-4f);
            _tmpH = Mathf.Max(Mathf.Abs(b.size.y), 1e-4f);

            // 把整块摆进 [-anchor.x*W, (1-anchor.x)*W] × [-anchor.y*H, (1-anchor.y)*H] ——
            // 和点阵那条 `RebuildMesh` 里的 x0/y0 是**同一套规矩**，所以两条后端可以互换
            _tmp.rectTransform.localPosition =
                new Vector3(-anchor.x * _tmpW - b.min.x, -anchor.y * _tmpH - b.min.y, 0f);
        }

        /// <summary>
        /// 把这段文字**右对齐到给定的世界 x**（右边缘落在 `worldRightX`）。
        /// 原版靠 TMP 的 `alignment`；我们这块 `Label` 是「以 `anchor` 定位的居中网格」，
        /// 所以等效做法是**渲完之后量一次宽度再挪** —— 判据用 `WorldW`（TMP 的 `textBounds`，**真测量**），
        /// 不用 `CapHeightWorld`/`GlyphHeightWorld` 那两个「回读传入值」的伪测量。
        /// 出处：`资料/日常_原版规格.md` §三·2 的 `timer`（原版右对齐）与 §二 的 `Refill Counter`。
        ///
        /// 🔴 **2026-09-23 修：参数是世界 x，但写进去的是 `localPosition`（相对父节点）——
        /// 原来没减父节点的世界 x ⇒ 父链一有偏移就整块**飞到屏幕外**。
        /// 实测代价：每日任务三行的 `timer` 与 `Mission Header` 的 `Refill Counter` **两处文字一个字都看不见**，
        /// 而 61 条断言全绿（它们量的是矩形，量不到「字飘走了」）。本工程已记过同一条坑
        /// （`RewardsWindow.Local` 的注释：`ImageQuad.Create`/`Label.Create` 的 `pos` 是 **localPosition**）。
        /// ⚠️ **只减位移、不除缩放** —— 本工程 Shell 这一线的父链**不带 `transform` 缩放**
        /// （原版的 `localScale` 是用「按矩形显式缩放」实现的，见 `MissionsTab.R`）。
        /// </summary>
        /// <summary>
        /// 把这段文字**左对齐到给定的世界 x**（左边缘落在 `worldLeftX`）。
        /// 与 <see cref="AlignRightOn"/> 对称；同样是「渲完之后量一次宽度再挪」。
        ///
        /// 🔴 **为什么需要它**：`Label` 的 <see cref="RefreshBounds"/> 把**文字块居中**放在
        /// 锚点上 —— 而原版这批 TMP 的 `m_HorizontalAlignment` 实测是 **Left**
        /// （`Mission Header` / 三张卡的 `name` / 每日行的 `description`·`timer`·`progress` 全是 `H=1 (Left)`；
        /// 只有卡片上的 `Timer` 是 `H=2 (Center)`）。
        /// 不对齐的代价实测：`Mission Header` 的 `name`（居中）与右对齐的 `Refill Counter` **叠在一起**
        /// （渲染图上是 `Daily Mis0Disponible`）。
        /// </summary>
        public void AlignLeftOn(float worldLeftX)
        {
            if (_tmp == null) return;
            RefreshBounds();
            if (!HasMeasuredWidth()) return;      // 🔴 见 `HasMeasuredWidth` —— 量不出宽度时**不动位置**
            float parentX = transform.parent != null ? transform.parent.position.x : 0f;
            var p = transform.localPosition;
            transform.localPosition = new Vector3(worldLeftX - parentX + WorldW * 0.5f, p.y, p.z);
        }

        public void AlignRightOn(float worldRightX)
        {
            if (_tmp == null) return;
            RefreshBounds();
            if (!HasMeasuredWidth()) return;      // 🔴 同 `AlignLeftOn`
            float parentX = transform.parent != null ? transform.parent.position.x : 0f;
            var p = transform.localPosition;
            transform.localPosition = new Vector3(worldRightX - parentX - WorldW * 0.5f, p.y, p.z);
        }

        /// <summary>🔴 **2026-10-04（Y5 · A70 修红）：`WorldW` 这一趟能不能用来算对齐。**
        ///
        /// <para>**为什么必须有这道守卫**：两个 Align 方法都是「渲完之后量一次宽度、再把节点挪过去」
        /// （`worldLeftX − parentX + WorldW × 0.5f` 直接写进 `localPosition.x`）——
        /// 而 **`WorldW` 在「量不出来」的时候是 TMP 的未定义值**：实测**空串**读到的是 **`4.29e9`**
        /// （同 `Core/Tooltip.cs` 里记的那条同族现象：对象没激活时也是这个天文数字）
        /// ⇒ `× 0.5` 把整颗节点扔到 **2.1e9 世界单位**之外，而**画面上什么都没有、也不报错**
        /// （同步点实跑就是这么红的：`TrophyInfoPopup` 的 `Title` / `Descripton` 零值态报
        /// 「差 **2147484000** 世界单位」—— 天文数字 = 垃圾坐标，不是「节点不在」）。</para>
        ///
        /// <para>判据（两条，都只覆盖「测不出来」这一档）：① **空文本** —— 没有东西可对齐，挪了也没有意义；
        /// ② **宽度不是画布尺度的量** —— `> 100` 世界单位 = **10800 画布像素**，而整块画布只有 1920 宽
        /// ⇒ 一段文字比整块画布宽五倍还多，那不是测量值。（顺带挡住 `NaN` / `Inf` / 非正值。）</para>
        ///
        /// <para>⛔ **正常路径一字不变**：有字、宽度正常时行为与旧版**逐位相同**（所以既有断言一条都不受影响）。
        /// ⚠️ 代价如实说：**空文本时不再对齐** ⇒ 那颗节点停在 `Label.Create` 给的框心，
        /// 等有字了由调用方**再对齐一次**（`TrophyInfoPopup.Apply` 就是这么做的）。</para></summary>
        bool HasMeasuredWidth()
        {
            if (string.IsNullOrEmpty(_text)) return false;
            float w = WorldW;
            if (float.IsNaN(w) || float.IsInfinity(w) || w <= 0f) return false;
            return w <= 100f;                     // 100 世界单位 = 10800px（画布宽 1920px 的五倍多）
        }

        // ==================================================================
        //  点阵后端（兜底）
        // ==================================================================

        void BuildDot()
        {
            _mf = gameObject.AddComponent<MeshFilter>();
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void SetTextDot(string text)
        {
            int w = Mathf.Max(1, TextCanvas.Measure(text, scale));
            int h = Mathf.Max(1, TextCanvas.LineHeight(scale));

            var px = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            TextCanvas.Draw(px, w, h, text, scale, 0, 0, (Color32)color);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;          // 点阵字，别插值糊掉
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply();

            var old = _mr.sharedMaterial.mainTexture;
            _mr.sharedMaterial.mainTexture = tex;
            if (old != null) DestroyImmediate(old);

            _texW = w; _texH = h;
            RebuildMesh();
        }

        void RebuildMesh()
        {
            float w = WorldW, h = WorldH;
            float x0 = -anchor.x * w, y0 = -anchor.y * h;

            var m = new Mesh { name = "LabelQuad" };
            m.vertices = new[]
            {
                new Vector3(x0, y0, 0f), new Vector3(x0 + w, y0, 0f),
                new Vector3(x0 + w, y0 + h, 0f), new Vector3(x0, y0 + h, 0f),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _mf.sharedMesh = m;
        }
    }
}
