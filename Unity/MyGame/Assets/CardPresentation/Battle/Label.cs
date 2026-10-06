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
//
// 🔴 **2026-10-15（A712）：摆位现在是「按原版**字墨**定位」，不再是「把行盒居中到节点位」。**
//    为什么必须改：我们的字体（`NotoSerifCJK`）的**行盒比原版高 1.6 倍、且不对称**
//    （行盒心→基线 0.4325 em，而原版 `Pragati` 只有 0.2632 em）⇒ 两边都做「`Middle`」时，
//    **我们的字墨比原版低 ≈0.13 × fontPx**（fs35 → 4.5px）。判据与实测：
//    `资料/普查产出_1013/WA712_垂直对齐普查.md` §4·4 · `资料/普查产出_1015/主对话_Unity腿与裁定.md` §一
//    （★ 大写字墨心 − 行盒心：我们 **−0.0725 em**、Pragati **+0.0526 em**、Asar **+0.0851 em**）。
//    ⚠️ **实测钉死**：TMP 自带的 `m_VerticalAlignment` 在这条路上是**空转**（`RefreshBounds` 每次都按
//    `textBounds.min` 把行盒重新摆正）⇒ 必须是**显式位移**，不是把那个枚举设一设。
//    口径、逐档算式、以及「为什么用【大写墨盒】当代理」→ `SetVAlign` 的注释（⛔ 别在别处再写一份）。
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

        /// <summary>🔴 **2026-10-11（A266）**：`SetWrapWidth` 欠下的「生成版面」待办（`&lt; 0` = 没有）。
        /// 值 = 那一次传进来的折行宽（**只为诊断/断言可读**；兑现时用的是 `sizeDelta` 里那份 —— 见
        /// <see cref="SetWrapWidth"/> 的第 ② 条理由）。</summary>
        float _pendWrapW = -1f;

        /// <summary>🔴 **2026-10-15（A546②）**：**上一次**影响本标签版面的动作是不是「定版面」那三条之一
        /// （<see cref="SetAutoFitBox"/> / <see cref="SetWrapWidth"/> / <see cref="ForceRelayout"/>）。
        /// <para>**为什么要有它**：<see cref="SetAlignLeft"/> 的 doc 写着「要在**量尺寸之前**调」，而那个
        /// 「量尺寸」的承载者就是那三条 —— 过了它们再调对齐，那一次对齐**不会被自己推下去**
        /// （TMP 的 setter 只置脏，而**批处理没有帧循环**，见 <see cref="ForceRelayout"/> 的文件头）。
        /// ⚠️ **它只记账、不改行为**：条件成立时既不改版面、也不打日志 —— 唯一后果是
        /// <see cref="AlignAfterLayoutCount"/> +1。为什么不做成硬守卫/出声，见 <see cref="SetAlignLeft"/> 的 doc。</para>
        /// <para>置 true = 那三条各自在**自己的重排之后**；置 false = <see cref="RefreshBounds"/>（= 每一条
        /// 定版面的路的末句，含建标签/换字/自适应/两个 `Align*On`）。⚠️ 所以「两个 `Align*On` 之后再调
        /// `SetAlignLeft`」这一档**不计入** —— 那两个方法**自己就会重排一次**（先 `RefreshBounds()` 再挪）。</para></summary>
        bool _defLayoutPushed;

        /// <summary>🔴 **2026-10-15（A546②）**：<see cref="SetAlignLeft"/> 是在**某条「定版面」的路之后**被调用的
        /// **次数**（只增不减；**只记账、不改行为** —— 与 <see cref="PendingWrapAppliedCount"/> 同形）。
        /// <para>**判据**：调用那一刻 <see cref="_defLayoutPushed"/> 为真 = `SetAutoFitBox` / `SetWrapWidth` /
        /// `ForceRelayout` 自上一次 `RefreshBounds` 之后跑过。</para>
        /// <para>**今天的读数（7 处调用点逐处现核过，⛔ 别当成 0）**：生产 4 = `Battle/CardDisplayWindow.cs` 3 +
        /// `Battle/UnitChatPanel.cs` 1 · 探针 3 = `Editor/ChatBoxProbe.cs`。（本次另在
        /// `Editor/SettingsScene.cs` 的 A546② 那一节加了 3 处**故意两态**的探针，不在这个普查数里。）
        /// · **不计的 4 处**：`Battle/UnitChatPanel.cs` 与那 3 处探针都是「先 `SetAlignLeft()`、后 `SetAutoFitBox`」；
        /// · **会计 3 笔**：`Battle/CardDisplayWindow.cs` 那 3 处走 `MenuDraw.Text`，而它的内层 `TextCore`
        ///   在多行那一档**自己就调** `SetWrapWidth` + `SetAutoFitBox`（`Shell/MenuDraw.cs`）⇒ 那一刻本标记已是真。
        ///   ⚠️ **那 3 笔不是缺陷**：① `_fxWho`/`_fxWhat` 当时还是空串；② `_fxTitle` 虽有字，但它是**单行**
        ///   —— 对齐只影响「折行之后每一行在块内怎么排」，而 `RefreshBounds` 是按 `textBounds` 把整块摆正的
        ///   ⇒ 单行时两种对齐渲在同一处（同 `AlignLeftOn` doc 那句「这个差别只有真折行时才看得见」）。</para>
        /// <para>它的用处 = 把「过晚调用」变成**可断言的**（两态断言 → `Editor/SettingsScene.cs` 的 A546② 那一节）。</para></summary>
        public int AlignAfterLayoutCount { get; private set; }

        string _text;

        /// <summary>贴图/文字块背后的世界尺寸（HUD 摆位/命中测试要用）。
        /// <para>🔴 **2026-10-11（A266）：这不再是纯读** —— 若这个标签身上还压着一条「折行宽已写、
        /// 字形还没生成」的待办（**未激活**的父链里建的标签就是这种，见 <see cref="SetWrapWidth"/>），
        /// 读它会**当场兑现**那条待办（生成一整个版面 + 重新量一次）。理由：这个数正是「摆位 / 对齐 /
        /// 命中」要用的**真值**，而没兑现时它是**垃圾**（`textBounds` 对没生成过的 TMP 给 0 或天文数字，
        /// 见 `HasMeasuredWidth` 的文件头）—— 静默用一个垃圾宽度去摆位比慢一拍坏得多。
        /// ⚠️ 兑现**只发生一次**（兑现之后就是纯读）；⛔ 别把它当纯读塞进热循环。</para></summary>
        public float WorldW { get { if (_tmp == null) return _texW / PixelsPerUnit; EnsureMeasured(); return _tmpW; } }
        /// <summary>同 <see cref="WorldW"/>（同一个兑现口）。</summary>
        public float WorldH { get { if (_tmp == null) return _texH / PixelsPerUnit; EnsureMeasured(); return _tmpH; } }

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

            // 🆕 2026-10-11（A266）：定版面的路**尾巴上都再试一次**待办（同 `RefreshBounds`）——
            //    这样「激活那一刻」万一没轮上，下一次摸到这个标签时也会把欠的那一刀补掉。
            TryApplyPendingWrap();
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
        ///
        /// <para>🔴 **2026-10-11（A266）：写值照旧立刻做，只有「生成字形」那一步在【未激活】时延后**，
        /// 由 <see cref="TryApplyPendingWrap"/> 在激活那一刻补做（或由 <see cref="EnsureMeasured"/>
        /// 在有人要真数时补做）。动手前试过「整条延后」，**那条被否掉了**（下面第 ①② 条就是原因）。</para>
        ///
        /// <para>**为什么要延后**：未激活的对象上跑 `GetTextInfo` 会造出「字模有了、渲染网格没有」的
        /// 半成品状态（`Awake` 没跑过 ⇒ `m_mesh == null`）—— 那正是 `RewardsScene.Run` 那个 NRE 的
        /// 根因状态（判据全文 → `资料/普查产出_1008/X_RewardsScene崩溃修.md` §1·3/§1·4）。
        /// 而**只加一道 `isActiveAndEnabled` 闸**（把整句吞掉）会让「未激活页里建的标签」永远拿不到
        /// 折行宽度 ⇒ 静默视觉回归（A266 的正本记着这一条）。</para>
        ///
        /// <para>**为什么只推迟「生成」这一件事**（模式与 `sizeDelta` 留在原地立刻写）：
        /// ① 调用方常紧跟一句 `SetWrapping(false)`（`MainMenuRuntime` 那两处就是
        /// `SetWrapWidth(...); SetWrapping(false);`）—— 把模式也记进待办、等激活再回写，
        /// 会把那个 `0` **静默改回 `1`**；② `SetAutoFitBox` 紧接着还会写 `sizeDelta.y`，
        /// 待办里那份宽度在激活时回写会把**高度**盖掉。⇒ 兑现那一刀只做「生成 + 重新量」，
        /// 一个值都不回写。</para>
        ///
        /// <para>🔴 **2026-10-13（A545）**：点阵后端下「折行」这一档**根本不存在** ⇒ **必须出声**
        /// （`CLAUDE.md` §三：不许静默失败）—— 原来那一句 `return` **一个字都不留**，而调用方
        /// （`MenuDraw` / `MatchLogRow` / `PromptPopup` …）都以为框宽已经生效。
        /// 出声走同族的 <c>NoteDotBackendLacks</c>，**口名 / key 头段 = `折行宽`**
        /// （⛔ **别改成别的口的名字**：`_dotAlignNoted` 是**进程内静态** HashSet、同一个 key 只响一次
        /// ⇒ 撞了就是「那一处先响过之后，这一口**再也不出声**」，静默复发、只在同一个进程里现形）。
        /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节（`SettingsScene.Run`）。</para>
        /// </summary>
        public void SetWrapWidth(float worldWidth)
        {
            if (_tmp == null)
            {   // 点阵后端不会折行 —— **要出声**（红线：不许静默失败），见方法头 A545
                NoteDotBackendLacks("折行宽", "不会折行", "折行宽度 = " + worldWidth,
                                    "没生效、这段文字仍然不折行");
                return;
            }
            TmpFont.SetWrapWidthRect(_tmp, worldWidth);      // 模式 + 宽度：**立刻写**（与旧版逐字相同）
            if (_tmp.isActiveAndEnabled)
            {
                _pendWrapW = -1f;
                TmpFont.GenerateLayout(_tmp);                // 旧版就是在这一刻生成的 ⇒ 这一档行为不变
                _defLayoutPushed = true;                     // 🆕 A546②：本条是「定版面」那三条之一
                return;
            }
            _pendWrapW = worldWidth;                         // 未激活 ⇒ 只把「生成」记成待办
            _defLayoutPushed = true;                         // 🆕 A546②：同上（⚠️ 真正落定要等激活时那次 `RefreshBounds`）
        }

        /// <summary>🔴 **2026-10-11（A266）**：对象被激活时把 <see cref="SetWrapWidth"/> 欠下的那一刀补做掉。
        /// <para>**判据（为什么敢在这里补 —— 三步，缺一不可）**：
        /// ① **`AddComponent` 在批处理（编辑模式）下不跑 `Awake`** —— 两处**独立**实测：
        ///    `Editor/BattleScene.cs:214-216` 的注释（为此显式补 `Build()`）与 `WindowsManager.EnsureHost`
        ///    那条已修缺陷（`Instance` 只在 `Awake` 里赋 ⇒ 自检里恒 null）；
        /// ② 而页面上的字**确实渲出来了**（`_tmp_view/rewards/02_战役.png`）—— 渲出来就要有
        ///    `MeshRenderer`/`MeshFilter`/网格，那三样**只在 `TextMeshPro.Awake()` 里建**
        ///    （`m_mesh` 是 `:584`）；
        /// ③ ⇒ ①排除「建的时候」⇒ 唯一触发点是**激活**（页签出厂 active、被 `ChangeTab` 关掉、再由 `Click(n)` 打开）
        ///    ⇒ **激活会跑 `Awake`，`OnEnable` 紧随其后**。
        /// ⚠️ 与 `Shell/PointerLayer.cs:47` · `Shell/PromptPopup.cs:858` · `资料/已知的坑.md:704`
        /// 那三条**不矛盾**：它们说的是「**建的时候就是活的**那些脚本，编辑模式下 `OnEnable` 不会再触发」——
        /// `OnEnable` 只在**激活那一刻**跑。
        /// ⚠️ 拿不到字体资产（点阵后端）时 `_tmp == null` ⇒ 待办恒空。</para></summary>
        void OnEnable() { TryApplyPendingWrap(); }

        /// <summary>有待办就兑现：**生成**一整个版面 + **重新量一次**（新版面要落回 `_tmpW/_tmpH`，
        /// 否则 `WorldW` 还停在旧值）。⛔ 只做这两件 —— **不回写**模式 / 宽度（理由见 `SetWrapWidth`）。
        /// <para>幂等：**先销账再干活**（下面那次生成会回调进 `RefreshBounds`，它的尾巴还会调回本函数）。</para>
        /// <para>未激活时**什么都不做**（待办留着，等激活或等有人要真数）—— 「生成」在未激活时正是
        /// 那个半成品状态的来源。</para></summary>
        void TryApplyPendingWrap()
        {
            if (_pendWrapW < 0f || _tmp == null) return;
            if (!_tmp.isActiveAndEnabled) return;
            _pendWrapW = -1f;
            PendingWrapAppliedCount++;          // 🔴 2026-10-11（FX4）：只记账、不改行为（见下面那个只读口）
            TmpFont.GenerateLayout(_tmp);
            RefreshBounds();
        }

        /// <summary>测量口（`WorldW/WorldH`）的兑现：**未激活也兑现** —— 见 `WorldW` 的注释。
        /// 与 <see cref="TryApplyPendingWrap"/> 的差别只有「不要求已激活」这一条，其余（先销账、
        /// 只生成+重量、不回写）逐字相同。</summary>
        void EnsureMeasured()
        {
            if (_pendWrapW < 0f || _tmp == null) return;
            _pendWrapW = -1f;
            TmpFont.GenerateLayout(_tmp);
            RefreshBounds();
        }

        /// <summary>
        /// 逐行**左对齐**（TMP 的 `m_HorizontalAlignment = 1`）。
        /// 🔴 **为什么需要它**：`TmpFont.NewText` 把所有 TMP 统一建成 `Center`，而原版这批文字
        ///    **多数是 Left**（`ChatText` = 1 · `Mission Header` / 卡名 / 每日行全是 1）。
        ///    `AlignLeftOn` 挪的是**整块**的位置，**管不了折行之后每一行在块内怎么排** ——
        ///    多行时短的那些行会居中，与原版不一致（这个差别只有真折行时才看得见）。
        /// `TextAlignmentOptions.Left` = H=Left + V=Middle，正好是原版 `m_HorizontalAlignment=1`
        /// + `m_VerticalAlignment=512` 那一对。
        /// ⚠️ **要在「定版面」之前调**（`SetAutoFitBox` / `SetWrapWidth` / `ForceRelayout` 那三条 ——
        ///    调完 `textBounds` 会变，所以它们各自的重排会**顺带**把这一次对齐落到画面上）。
        /// <para>🔴 **2026-10-15（A546②）就地订正（铁律 5）**：本行原文只写「要在**量尺寸之前**调」，
        /// 读起来像「代码里有守卫」—— **没有**。现在把它写成**调用方责任**，并说清越界之后的实况与处置：
        /// · **越界了会怎样**：这一句**自己不重排**（TMP 的 `alignment` setter 只置脏，而批处理没有帧循环
        ///   ⇒ 见 <see cref="ForceRelayout"/> 那份「字段说了、画面没变」的实测）⇒ **画面停在旧对齐上**。
        ///   ⚠️ 只有**真折行**时看得出来（`AlignLeftOn` 挪的是**整块**，逐行那半边归本方法）—— 单行时
        ///   对齐不影响渲出来的形状 ⇒ 本仓那几处「定版面之后才调」**今天都是无害的**（实据见下条）。
        /// · **为什么不做成硬守卫 / 不在这里出声**（判据两条，都是现读）：
        ///   ① **会误报**：`Shell/MenuDraw.Text` 的内层 `TextCore` 在多行那一档**自己就调**
        ///      `SetWrapWidth` + `SetAutoFitBox`（`Shell/MenuDraw.cs` 的 `TextCore`）⇒ 而
        ///      `Battle/CardDisplayWindow.cs` 的 3 处**正是**「`MenuDraw.Text(...)` 之后紧跟 `SetAlignLeft()`」
        ///      —— 那是**合法**形状（后面 `SetEffectRows` 的 `SetText` 会再推一次），日志却会在那儿响。
        ///   ② **不许自愈**（改行为 + 踩已知的坑）：要「补一次重排」就得在这一刻生成版面，而这些标签
        ///      **多半建在未激活的父链里**（`SetAlignLeft` 就在各窗的 `Build()` 里调）—— 未激活时 TMP 量出来的
        ///      `textBounds` 是**天文数字**（<see cref="HasMeasuredWidth"/> 文件头记的 4.29e9 → 把节点扔到
        ///      2.1e9 世界单位之外）⇒ 自愈会把「画面停在旧对齐」换成更坏的坐标错。
        ///      ⚠️ 而且「后面还会不会再推一次」**在调用点看不出来** ⇒ 那一刻没有一句**准确**的话可说。
        /// · **所以本账的落地 = 订正 doc + 一个可断言的记账口** <see cref="AlignAfterLayoutCount"/>
        ///   （越界调用 +1、正常形状不动）—— 断言（**两态**）→ `Editor/SettingsScene.cs` 的 A546② 那一节。</para>
        /// <para>🔴 **2026-10-13（A491）**：点阵后端下「对齐这回事**根本不存在**」**必须出声**
        /// （`CLAUDE.md` §三：不许静默失败）—— 同族的**第三处**（前两处 = 两个 `Align*On`，账 **A476**
        /// 2026-10-12 已做出声；同族的 `SetCharSpacing` 更早就出声 —— 🔴 **A596 之后它也走同一只口**
        /// （`字距`），见 `NoteDotBackendLacks` 那段 doc）。
        /// 原来这一句 `return` **一个字都不留** ⇒ 一旦字体资产缺失（`TmpFont.Available == false`），
        /// **整批逐行左对齐静默退回居中**（多行时短的那些行居中 —— 正是本函数要修的那个差别），日志里空。
        /// ⚠️ 出声走**同一个** `NoteDotBackendLacks`，口名/key 头段 = **`逐行左对齐`** ——
        /// **⛔ 别改成两个 `Align*On` 用的 `左对齐` / `右对齐`**：`_dotAlignNoted` 是**进程内静态** HashSet、
        /// 同一个 key **只出声一次** ⇒ 撞了就是「那一处先响过一次之后，这一口再也不出声」（静默复发）。
        /// 断言（含「key 不撞」那一条）→ `Editor/SettingsScene.cs` 的 A491 那节（`SettingsScene.Run`）。</para>
        /// </summary>
        public void SetAlignLeft()
        {
            if (_tmp == null)
            {   // 点阵后端没有「对齐」这回事 —— **要出声**（红线：不许静默失败）
                NoteDotBackendLacks("逐行左对齐", "没有对齐这回事", "逐行左对齐（每行在块内贴左）",
                                    "没生效、每一行仍在块内居中");
                return;
            }
            // 🆕 **A546②**：过晚调用（「定版面」那三条之后才调）**只记账、不改行为** —— 不做硬守卫 / 不出声的
            //   两条理由（会误报 · 不许自愈）→ 本方法的 doc。断言 → `Editor/SettingsScene.cs` 的 A546② 那一节。
            if (_defLayoutPushed) AlignAfterLayoutCount++;
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
        /// ⚠️ 它会挪 TMP 子节点 ⇒ **要在对齐/量宽之前调**（调用顺序：`SetAutoFitBox` → 本函数 → 对齐 → 量）。
        ///
        /// <para>🔴 **2026-10-13（A545）就地订正（铁律 5）**：上面那句「点阵后端**直接返回**」只描述了行为、
        /// 没说出不出声，而实现是**一个字都不留**的 `return` ⇒ 与 `CLAUDE.md` §三「不许静默失败」打架
        /// ⇒ 现在**出声**后返回。**口名 / key 头段 = `换行模式`** —— ⛔ 别与 `SetWrapWidth` 的 `折行宽` 合并
        /// （两者是**两个口**，`SetAutoFitBox` 内部会调 `SetWrapWidth` ⇒ 合并会让后响的那一个永远静默）。
        /// ⚠️ **传非法档（不在 0/1/2/3）那一支【照旧】走 `Debug.LogWarning`**（那条与后端无关，早就出声）。
        /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para></summary>
        public void SetWrappingMode(int originalMode)
        {
            if (_tmp == null)
            {   // 点阵后端只有「单行」这一档 —— **要出声**（红线：不许静默失败），见方法头 A545
                NoteDotBackendLacks("换行模式", "只有「单行」这一档", "换行模式 = " + originalMode,
                                    "没生效、这段文字仍然单行不折");
                return;
            }
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
        /// <para>🔴 **2026-10-13（A545）就地订正（铁律 5）**：本行原文写「点阵后端（`_tmp == null`）没有
        /// 折行/mesh 这回事 ⇒ **什么都不做（如实，不假装）**」—— 前半句是事实，**后半句是自陈静默**：
        /// 调用方以为「版面已经推下去了」，日志里却一个字都没有 ⇒ 与 `CLAUDE.md` §三「不许静默失败」打架。
        /// 现在**出声**后返回，**口名 / key 头段 = `重排`**（⛔ 别改成别的口的名字，理由见 `NoteDotBackendLacks`）。
        /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para>
        /// <para>📌 用法先例（本函数就是从它收上来的）：`Shell/PracticeModePopup.cs` 的 `RelayoutNow` —— 那边
        /// 当时`Battle/Label.cs` 不在白名单里，只能绕；现在公共件里有了，那一处**可以**退休（不在本件范围）。</para></summary>
        public void ForceRelayout()
        {
            if (_tmp == null)
            {   // 点阵后端不经过 TMP 排版 —— **要出声**（红线：不许静默失败），见方法头 A545
                NoteDotBackendLacks("重排", "不经过 TMP 排版", "重排一次（把版面推下去）",
                                    "没生效（这个后端没有可推的那一版）");
                return;
            }
            SetFontSize(_tmp.fontSize);     // 值相同 ⇒ setter 早退，只要那一次 `ForceMeshUpdate`
            RefreshBounds();                // 重排后的 `_tmpW/_tmpH` 要落回字段（否则 WorldW 还是旧版面的值）
            _defLayoutPushed = true;        // 🆕 A546②：本条是「定版面」那三条之一
        }

        /// <summary>自检用：TMP 现在排出来**几行**（判「折行真的生效了」，不是只把字缩小了）。
        /// ⚠️ 要在 `ForceMeshUpdate` 之后读；`textInfo` 还没建时返回 0。</summary>
        public int LineCount
        {
            get { return _tmp != null && _tmp.textInfo != null ? _tmp.textInfo.lineCount : 0; }
        }
        
        /// <summary>🔴 **2026-10-11（FX4 / DIAG-B §三·#11(b)）新增的【只读口】**：`TryApplyPendingWrap` 真的
        /// 兑现过一次的次数（**只增不减**）。**只记账、不改任何行为**。
        /// <para>**为什么要它**：判「A266 那一跳（未激活时欠下的「生成版面」被补做）到底做了没有」**不能读
        /// `LineCount`** —— 那读的是 `TextMeshPro.textInfo.lineCount`，而 `TextMeshPro.Awake()`（**第一次激活时**跑）
        /// 里那句 `m_textInfo = new TMP_TextInfo(this)` 会把它**清零**（`TextMeshPro.cs:582-593`，只在 `m_mesh == null` 时）
        /// ⇒ **只要版面是在激活前生成的，`lineCount` 就必然被抹掉** ⇒ 两种世界读数一模一样（都是 0）。
        /// 本计数器数的是**我们这条待办路自己**兑现了几次，与 `m_textInfo` 无关。</para>
        /// <para>⚠️ **只在这个函数里自增** —— `EnsureMeasured`（`WorldW/WorldH` 那条兜底兑现）**不算**：
        /// 那是「有人要真数」时的兑现，与「激活时补做」是两件事（要分开数才判得清）。</para>
        /// <para>⚠️ **批处理（编辑模式）下 `OnEnable` 不跑**（`Awake/OnEnable` 只对 `[ExecuteAlways]` 的脚本跑 ——
        /// `资料/已知的坑.md:704` · `Shell/PromptPopup.cs:868` · `Shell/MainMenuRuntime.cs:533` ·
        /// `Shell/PointerLayer.cs:47-48` 四条独立记录），而本类**没有那个特性**）⇒ 自检里兑现点落在
        /// `RefreshBounds` 尾句 / `EnsureMeasured`；计数器读到的仍是「这一刀补过了」，只是补的人不是 `OnEnable`。</para></summary>
        public int PendingWrapAppliedCount { get; private set; }

        /// <summary>🆕 **2026-10-15（A821）：`MenuDraw` 那两个「文字裁切落空」诊断计数「只记**唯一标签**」的凭据。**
        /// 某一档在**这个标签**上**第一次**被取 ⇒ `true`（调用方据此 `++` 并出声）；之后同一档恒 `false`。
        /// <para>**为什么要它**：`MenuDraw.Text` / `TextBox` 末尾会裁一刀（A781 起），而 7 个包装器
        /// （`ItemDrawer.TextCentered`/`ClippedText` · `ChatPanel.Text` · `AllianceMemberTab.TextSoft` ·
        /// `SettingsWindow.Text` · `PlayerProfileWindow.Text` · `WindowsManager.Text` · `MenuWindowBase.Text`/`TextBox`）
        /// **接着自己再裁一刀** ⇒ 同一颗标签被数两遍（`MenuDraw.TextCore` 的注释点名的就是这个病，
        /// 只是病长在包装器那一层）。调度台 2026-10-15 裁：**不删那 7 句、不改行为**（同框幂等 ⇒ 没有复刻缺口），
        /// 只让这两个数诚实。判据全文 → `资料/已知的坑.md` §「2026-10-15 新增」1。</para>
        /// <para>**只记账、不改任何行为**（同族先例 = 上面的 `PendingWrapAppliedCount`）。
        /// ⚠️ **别改成在 `MenuDraw` 里 `AddComponent` 一颗标记组件**：本类 `OnEnable` → `TryApplyPendingWrap`
        /// → `ForceRelayout` → `RefreshBounds` 尾句 → `ClippedTextGuard.Reclip` → `ClipTextNow` 这条路上
        /// **真会走到计数**，而那是「往一个正在 `OnEnable` 的 `GameObject` 上 `AddComponent`」——
        /// 本件没有 Unity 可跑、判据查不到 ⇒ **选了不需要建任何组件的这一种**。本类字段本来就在，
        /// 两档各自一个 `bool`、零生命周期风险。</para>
        /// <para>⚠️ **上游那个 `Label` 是`tmp.GetComponentInParent&lt;Label&gt;(true)` 取回来的**（`ClipTmpMesh` 手上
        /// 只有 TMP 子节点）⇒ 取不到时调用方**退回旧行为「照数」**（那一族没有「同一颗裁两刀」的调用方）。</para></summary>
        /// <param name="uploadSkipped">`true` = 「没上传到会被画出来的那份网格」那一档
        /// （`MenuDraw.TextClipUploadSkipped`）；`false` = 「压根没有可裁的网格」那一档
        /// （`MenuDraw.TextClipUnavailable`）。</param>
        public bool TakeClipFailMark(bool uploadSkipped)
        {
            if (uploadSkipped)
            {
                if (_clipFailUpload) return false;
                _clipFailUpload = true;
                return true;
            }
            if (_clipFailNoMesh) return false;
            _clipFailNoMesh = true;
            return true;
        }

        /// <summary>`TakeClipFailMark` 的两档凭据（各一颗标签一次）。**只记账、不改任何行为**。</summary>
        bool _clipFailNoMesh, _clipFailUpload;

        /// <summary>
        /// **直接定 TMP 的 `fontSize`（世界单位）**。
        /// 用它是为了**逐字照抄原版的字号**：原版 TMP 的 `m_fontSize` 是 **UI 画布像素**
        /// （例如导航钮标签 `33` · `Player Name` `32` · 聊天两行 `18`），
        /// 我们的设计空间是 **1080px = 10 世界单位** ⇒ 传 **`原版px / 108f`** 就等价。
        /// ⚠️ 调它之前 `_tmp` 必须已经建好（`Create` 之后、且 `TmpFont.Available`）。
        /// <para>🔴 **2026-10-13（A545）就地订正（铁律 5）**：本行原文写「没字体资产时会**静默无效**」
        /// （那本来就走点阵后端，点阵只有整数档 —— 见 `SetSizes`）—— 「静默无效」**是自陈静默**，
        /// 与 `CLAUDE.md` §三「不许静默失败」打架 ⇒ 现在**出声**后返回（**口名 / key 头段 = `字号`**，
        /// ⛔ 别改成别的口的名字，理由见 `NoteDotBackendLacks`）。
        /// ⚠️ **传非正数那一支【照旧】不出声**：那是**无效入参**（没有字号可设），不是「这一档功能不存在」
        /// —— 为了这句话**严格成立**，无效入参那道闸**必须排在 `_tmp == null` 之前**
        /// （反过来写的话，点阵后端下传 `0` 会被报成「这个后端没有字号」，归因就错了）。
        /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para>
        /// </summary>
        public void SetFontSize(float worldSize)
        {
            if (worldSize <= 0f) return;     // 无效入参：不是「后端没有这一档」⇒ 照旧不出声（A545 的边界）
            if (_tmp == null)
            {   // 点阵后端没有 TMP 的 fontSize（它只有整数档 scale）—— **要出声**，见方法头 A545
                NoteDotBackendLacks("字号", "没有 TMP 的 fontSize（只有整数档 scale）", "fontSize = " + worldSize,
                                    "没生效（点阵那条路只认整数档，见 SetSizes）");
                return;
            }
            _tmp.fontSize = worldSize;
            _tmp.ForceMeshUpdate();
        }

        /// <summary>当前的 TMP `fontSize`（**不是世界单位**，见 <see cref="SetAutoFitBox"/>）。</summary>
        public float FontSize { get { return _tmp != null ? _tmp.fontSize : 0f; } }

        /// <summary>🆕 2026-10-03：**字距**（原版 TMP 的 `m_characterSpacing`，**原样传**、不换算）。
        /// 🔴 原来三处带字距的原版文字都**没照做**，理由写的是「`Label` 没有字距接口」—— 那只是**没加**，不是加不了：
        ///   遭遇/排位窗 `Window Title` **5** · `DivisionText` **−2.6** · `ChangeRankedToggle` **−4**
        ///   （正本 `资料/阶段二_战斗入口_原版规格.md` §二 那几张表里逐条标着）。
        /// ⚠️ **点阵后端（`_tmp == null`）没有「字距」这回事** ⇒ 什么都不做并**出声**（红线：不许静默失败）。
        /// <para>🔴 **2026-10-13（A596）收编（铁律 5 留痕）**：本行原文写「…并**出声**」—— 那个出声走的是
        /// **它自己的 `Debug.Log`**（消息模板第二份、且**每次调用都打一行、不去重**）⇒ 同族**第 9 个出声口**，
        /// 「key 的拼法只有一份」这句话因此不完全成立。现在改走同族的 <see cref="NoteDotBackendLacks"/>
        /// （口名 / key 头段 = **`字距`**）⇒ **消息文案与刷屏行为都变了**（逐次一行 ⇒ 每处一次），这是本账要的。
        /// ⛔ **别把出声删掉了事**、也别改回私有 `Debug.Log`（= 回到两套消息模板 + 不去重那个原始状态）。
        /// ⚠️ **正常路径一字未动**（`characterSpacing = v` + 那一句 `ForceMeshUpdate`）—— 那一下**必要**：
        /// `characterSpacing` 的 setter 只置脏，而 `ForceMeshUpdate` 会把 `m_fontSize` 复位成
        /// `Clamp(m_fontSizeBase, min, max)`、**带着新字距重跑一次自适应**
        /// （`TextMeshProUGUI.cs:556-566` + `TextMeshPro.cs:2148-2149`，静态判据）⇒ ⛔ 别删它。</para></summary>
        public void SetCharSpacing(float v)
        {
            if (_tmp == null)
            {   // 点阵后端没有字距 —— **要出声**（红线：不许静默失败）；A596：收编到同族的统一口
                NoteDotBackendLacks("字距", "没有「字距」这回事", "`characterSpacing = " + v + "`",
                                    "没生效（这一档字距没加上）");
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
            return fontSize * TmpFont.WorldGlyphPerFontSize * PxPerWorld;
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

        /// <summary>🔴 **2026-10-11（A305 ①）**：TMP 真正的 `m_fontSizeBase`（**fontSize 单位**，不是 px、
        /// 也不是世界单位；换算见 <see cref="FontSizeToPx"/>）。自检要能把它读回来**断死** ——
        /// 它是「自适应二分的起点」，**没有公开访问器**（`TMP_Text.cs:473` 是 `protected`），
        /// 而 `fontSize` getter 读到的是**收敛结果**（`TMP_Text.cs:466`）⇒ **只能反射读**。
        /// ⚠️ 与 `FontSizeMin/Max` 同族：`_tmp == null`（点阵后端）⇒ 恒 **−1**（那后端没有自适应，
        /// 如实报、别猜 0 —— 0 是个合法的 base）。
        /// <para>📌 反射读非公开字段在本工程有先例：`Editor/BattleScene.cs:2287`（A80①）、
        /// `RuleEngineTest.cs:7823`。</para>
        /// <para>🔴 **2026-10-13（A536）**：句柄的取法收进 <see cref="BaseFld"/>（**读写共用那一份**）——
        /// 本件要**写**这个字段（见 `EnsureFontSizeBase`），⛔ 别在别处再 `GetField` 一次。</para></summary>
        public float FontSizeBase
        {
            get
            {
                if (_tmp == null) return -1f;
                var f = BaseFld;
                if (f == null) return -1f;
                return (float)f.GetValue(_tmp);
            }
        }

        /// <summary>🔴 **2026-10-13（A536）**：`m_fontSizeBase` 的反射句柄 —— **读（<see cref="FontSizeBase"/>）
        /// 与写（<see cref="EnsureFontSizeBase"/>）共用这一份**。
        /// ⚠️ 找不到 / 类型不是 `float` ⇒ `null`（调用方各自如实处置：读那边报 −1、写那边出声一次）。
        /// 找不着时**每次调用会重试一遍**（与原来长在 getter 里那段的语义逐字相同：`_baseFld == null` 就再找）。</summary>
        static System.Reflection.FieldInfo BaseFld
        {
            get
            {
                if (_baseFld == null)
                {
                    _baseFld = typeof(TMP_Text).GetField("m_fontSizeBase",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (_baseFld != null && _baseFld.FieldType != typeof(float)) _baseFld = null;   // 版本换了就如实报 −1，别硬转
                }
                return _baseFld;
            }
        }
        static System.Reflection.FieldInfo _baseFld;

        /// <summary>🔴 **2026-10-13（A536）**：把 `m_fontSizeBase` **真的**写成 `baseCur`
        /// （`SetAutoFitBox` 里那句 `_tmp.fontSize = baseCur` 的**补丁**）。
        ///
        /// <para>**病灶**：`fontSize` 的 setter 第一条是 `if (m_fontSize == value) return;`
        /// （`TMP_Text.cs:465`）⇒ `baseCur` 正好等于现值时**整句什么都不做**，而 `m_fontSizeBase`
        /// **只有它一条写口**（同文件那句 `if (!m_enableAutoSizing) m_fontSizeBase = m_fontSize;`）
        /// ⇒ 字段停在旧值上（`Label.Create` 之后 = TMP 的序列化默认 **36.0**）。
        /// **现场**：`basePx == nomPx` 的站（`LabelAutoMax = 32 = 标称` · `LabelBase = 32`）**第一次调用**
        /// 就落在这一格（`Label.Create` 里 `TmpFont.NewText(…, TmpFontSize(), …)` ⇒ `m_fontSize` 就是 `cur`
        /// ⇒ `baseCur == cur == _tmp.fontSize`）⇒ W-D1 交件时「读不出差别、编不出有鉴别力的断言」（A407 第二处）。</para>
        ///
        /// <para>**为什么反射写、而不是「先把 `fontSize` 拨到别的值再拨回来」**：后者要多两下
        /// `SetVerticesDirty`/`SetLayoutDirty`（旧注释写「代价大于收益」指的就是它，字段值倒是能写进去）；
        /// 而本函数**只在 setter 确实早退时**才被调到（调用点那句 `if (fontBefore == baseCur)` ——
        /// 早退 = 那一下什么都没改）⇒ 补写字段不改任何别的状态，
        /// 紧接着的 `ForceMeshUpdate`（`SetAutoFitBox` 尾部那一句）就会拿新 base 当二分起点
        /// （`TextMeshPro.cs:2148-2149`：`if (m_enableAutoSizing) m_fontSize = Mathf.Clamp(m_fontSizeBase, min, max);`）。
        /// ⛔ **别把它改成无条件调用** —— 那会在 setter 已经写好时再写一遍同一个值（同值，但多一层
        /// 「两处写同一个字段」的债：读回 `/` 断言都分不清是谁写的）。</para>
        ///
        /// <para>⚠️ 拿不到字段（TMP 换版）⇒ **出声一次**（红线：不许静默失败），字段停在旧值。</para></summary>
        void EnsureFontSizeBase(float baseCur)
        {
            if (_tmp == null) return;
            var f = BaseFld;
            if (f == null)
            {
                if (!_baseWriteWarned)
                {
                    _baseWriteWarned = true;
                    Debug.LogWarning("[Label] ⚠️ 找不到 TMP 的 `m_fontSizeBase` 字段 ⇒ `SetAutoFitBox(…, basePx: …)` "
                        + "**写不进自适应二分的起点**（这一格停在旧值上）。改法：核 TMP 版本 / 字段名（`TMP_Text.cs:473`）。"
                        + "（出声，不静默；只报一次）节点 = " + name);
                }
                return;
            }
            f.SetValue(_tmp, baseCur);
        }
        static bool _baseWriteWarned;

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
        ///
        /// <para>🔴 **2026-10-11（A305 ①）：`basePx` = 原版那一颗 TMP 的 `m_fontSizeBase` 原文**（画布 px，
        /// 与 `minPx`/`maxPx` **同一个量纲**；`&lt;= 0` = **不指定**，维持 A57③ 的旧行为「base = 调用方那一档」）。
        /// 它**只影响自适应二分的起点**（`TextMeshPro.cs:2148-2149`
        /// `m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`），
        /// 终点两侧都收敛到「装得下的最大号」⇒ **渲染无差**（差 ≤ 0.05 fontSize 单位 —— `TMP_Text.cs:4814` 涨 /
        /// `:4537` 缩，两个方向都按 **1/20** 取整且被 min/max 夹住）。</para>
        ///
        /// <para>🔴 **为什么必须逐站现读、不套通则**（铁律 5·c「一个值 ≠ 全部情况」的正例）：
        /// 全库 8,680 个「开了自适应」的原版 TMP 里 `m_fontSizeBase` 有 **200+ 种取值** ——
        /// `36.0` 出现 **3,045** 次（= **TMP 的序列化默认值**，`TMP_Text.cs:473`
        /// `protected float m_fontSizeBase = 36;` ⇒ 原版**大多数站根本没显式设过** ——
        /// 作者开着 `m_enableAutoSizing` 时 setter **不回写 base**，见 `TMP_Text.cs:467`）·
        /// `12.0` 只有 **559** 次 · 其余散在 `45.2`（卡包详情 `Title`）· `39` / `26` / `24` / `23` / `35` /
        /// `17.95` / `27.69` / `36.89` / `30` / `32` …
        /// 🔴 **2026-10-11 就地订正（铁律 5）**：旧转述「原版那一档是 12px」**只对【一个站】成立**
        /// （`Rewards Base Submenu Variant` 的 `Continue` / `WebShop Button` / `Back` 那一族按钮文案），
        /// **不是通则** —— 照它抄会把几十个站一次改错。逐站实读表 →
        /// `资料/普查产出_1011/V7_A305_A304_普查.md` §二·3（19 个站，逐个亲读原版 MB）。</para>
        ///
        /// <para>⛔ **本参数不碰「上限」那一族**（把原版 `m_fontSize` 当成 `m_fontSizeMax` 传的那 42 处）
        /// —— 那是**另一条**（A333）。`maxPx` 的语义一个字没动。</para>
        ///
        /// <para>🔴 **2026-10-13（A545）**：点阵后端下这一档**整个不存在**（框、折行、字号自适应三样都没有）
        /// ⇒ **必须出声**（`CLAUDE.md` §三：不许静默失败）—— 原来那一句 `return` **一个字都不留**，
        /// 而它是**全仓 39 个生产调用点**（另 10 处自检探针）的入口（`MenuDraw` / `MenuWindowBase` /
        /// `CollectionWindow` / `MainMenuRuntime` …），每一处都以为「字号被夹进框里了」。
        /// 出声走同族的 <c>NoteDotBackendLacks</c>，
        /// **口名 / key 头段 = `自适应框`**（⛔ 别与它内部调的 `SetWrapWidth` 的 `折行宽` 合并 —— 两个口，
        /// 合并会让后响的那一个永远静默）。断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para>
        ///
        /// <para>🔴 **2026-10-13（A597）**：内部那**两条入参闸**（`cur`/`minPx`/`maxPx` 里有非正数、
        /// `nomPx` 折算不出来）原来也是**静默 `return`** ⇒ 现在**出声**，口名 / key 头段 =
        /// `自适应框参数` / `自适应框折算`（走 <see cref="NoteArgInvalid"/> —— **入参**的错，不是「后端缺这一档」，
        /// 归因别混）。⚠️ 两格今天都**够不到**（现核见那个方法的 doc）⇒ 它们是给将来兜底的，
        /// 有 TMP 的站**行为逐位不变**（A545 的 ★⑥ 反面对照照样成立：那 5 个口一行都不出）。</para>
        /// </summary>
        public void SetAutoFitBox(float worldW, float worldH, float minPx, float maxPx, float basePx = 0f)
        {
            if (_tmp == null)
            {   // 点阵后端既不会折行、也没有自适应 —— **要出声**（红线：不许静默失败），见方法头 A545
                NoteDotBackendLacks("自适应框", "既不会折行、也没有自适应",
                                    "一个 " + worldW + "×" + worldH + " 的框 + 自适应 " + minPx + "~" + maxPx + "px",
                                    "没生效（框、折行、字号自适应三样都没有）");
                return;
            }
            SetWrapWidth(worldW);                       // 顺带把折行宽度也设上（同一份 sizeDelta）
            if (worldH > 0f)
            {
                var d = _tmp.rectTransform.sizeDelta;
                _tmp.rectTransform.sizeDelta = new Vector2(d.x, worldH);
                // 🆕 **2026-10-15（A712）**：记下这个框高，留给**垂直档**当 `Bottom`/`Top` 的框高用
                // （调用方没显式传第 2 实参时）。⚠️ **只认这一条路**：不读 `sizeDelta.y` 本身 ——
                // `SetWrapWidth` 会把 `sizeDelta` 写成 `(width, 0)`，而**从没设过框**的标签身上那份是
                // Unity 给新 `RectTransform` 的默认值（不是原版框高）⇒ 拿它当框高会把 Bottom/Top
                // 摆到「半框之外」，而且**一个字都不报**。
                _boxHFromAutoFit = worldH;
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
            if (cur <= 0f || minPx <= 0f || maxPx <= 0f)
            {   // 🔴 **2026-10-13（A597）**：入参不成立 ⇒ 原来**静默 return**（一个字都不留）⇒ 调用方以为
                //    「字号已经夹进框里了」。现在出声（口名 / key 头段 = `自适应框参数`，见 `NoteArgInvalid`）。
                //    ⚠️ `cur <= 0f` 这一格今天够不到（`CapWorld` 有常数兜底），`minPx/maxPx <= 0` 那一格是
                //    「将来有人忘了给窗口」的兜底 —— 两条都在报告 §A597 里现核过，如实登记。
                NoteArgInvalid("自适应框参数",
                               "标称字号 cur = " + cur + " · minPx = " + minPx + " · maxPx = " + maxPx,
                               "一个 " + worldW + "×" + worldH + " 的框 + 自适应 " + minPx + "~" + maxPx + "px",
                               "没生效（框没设、字号也没夹）");
                return;
            }
            float nomPx = NominalPx();                  // 「cur 折算成画布 px」= 本工程唯一那条 px 口径（见它的注释）
            if (nomPx <= 0f)
            {   // 🔴 **2026-10-13（A597）**：折算不出画布 px ⇒ 原来也**静默 return**。今天同样够不到
                //    （`TmpFont.MeasureGlyph` 永不返回 ≤ 0，`Core/TmpFont.cs:272-300`）—— 出声是为将来兜底。
                NoteArgInvalid("自适应框折算",
                               "`nomPx` = " + nomPx + "（`WorldGlyphPerFontSize` = " + TmpFont.WorldGlyphPerFontSize + "）",
                               "一个 " + worldW + "×" + worldH + " 的框 + 自适应 " + minPx + "~" + maxPx + "px",
                               "没生效（min/max 折不成 fontSize 单位 ⇒ TMP 的自适应根本没开）");
                return;
            }
            // 🔴 **2026-10-04（A57③）：先关自适应、再写 `fontSize`** —— TMP 只在 `!m_enableAutoSizing` 时
            //    才回写 `m_fontSizeBase`（`TMP_Text.cs:467`），而**起点就是 base**
            //    （`TextMeshPro.cs:2149`：`m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`）。
            //    旧写法没有这一行：同一个 label 被**第二次**调时自适应还开着 ⇒ base 停在**第一次**那个值
            //    （起点是旧值；二分最后仍收敛到「装得下的最大号」，所以**影响小**，但字段是错的）。
            //    ⚠️ **第一次调用逐位不变**：那一次 `m_enableAutoSizing` 本来就是 `false`（TMP 出厂值），
            //    这一行 setter 走 `m_enableAutoSizing == value` 早退 ⇒ 等于什么都没做。
            //    🔴 **2026-10-13（A536）洞已堵（铁律 5 留痕）**：本行原文写「`fontSize` 的 setter 还有一条
            //    `m_fontSize == value` 早退 ⇒ 第二次调用时若 TMP 正好已经停在 `cur`，base 还是刷不到 …
            //    代价大于收益，**不做**；这里只保证「顺序对」」。**那个洞是真的、而且不止第二次**：
            //    `basePx == nomPx` 的站（`LabelAutoMax = 32 = 标称`、`LabelBase = 32`）**第一次调用**就落在
            //    那一格（`Label.Create` 里 `TmpFont.NewText(…, TmpFontSize(), …)` ⇒ `m_fontSize` 就是 `cur`）
            //    ⇒ `SetAutoFitBox(…, basePx: 32)` 那一格**一个字节都没写进 base**（字段停在 TMP 的
            //    序列化默认 **36**），W-D1 交件时因此**编不出有鉴别力的断言**（A407 第二处 = 卡背页 `$owned`）。
            //    ⇒ 现在由 `EnsureFontSizeBase(baseCur)` **只在 setter 确实早退时**补写字段（见它自己的 doc）。
            // 🔴 **2026-10-11（A305 ①）**：写进 `m_fontSizeBase` 的那个值 = **原版那一站显式设过的 base**
            //    （`basePx > 0` 时按 `maxPx`/`minPx` 同一套比例折成 fontSize 单位）；**没指定就照旧 = `cur`**。
            //    ⚠️ 写法只能是「先关自适应、再把 `fontSize` 拨到 base」—— TMP **没有公开的 base 口**
            //    （`m_fontSizeBase` 是 `protected`，`TMP_Text.cs:473`），只能借 setter 回写（`TMP_Text.cs:467`），
            //    早退那一格再由上面那句反射补写（A536）。
            //    ⚠️ 这一下**顺带把 `m_fontSize` 也挪到了 base**，但紧接着的 `ForceMeshUpdate` 会把它
            //    重新 `Clamp(base, min, max)`（`TextMeshPro.cs:2148-2149`）⇒ 只是**二分起点**变了。
            //    （`FontPxNow`/`WorldW` 那些读数读的是**收敛结果**，不受影响 —— 见方法头。）
            float baseCur = basePx > 0f ? cur * (basePx / nomPx) : cur;
            _tmp.enableAutoSizing = false;
            float fontBefore = _tmp.fontSize;           // 🔴 A536：setter 有一条 `m_fontSize == value` 早退 ⇒ 先记下现值
            _tmp.fontSize = baseCur;                    // 复位/照抄原版 base（这一下顺带回写 `m_fontSizeBase`）
            if (fontBefore == baseCur) EnsureFontSizeBase(baseCur);   // 🔴 A536：早退那一格由这里补写（否则 base 停在旧值）
            _tmp.fontSizeMax = cur * (maxPx / nomPx);   // 🔴 原版 `m_fontSizeMax`（**不是** cur，见方法头 A50①）
            _tmp.fontSizeMin = cur * (minPx / nomPx);   // 🔴 原版 `m_fontSizeMin`（旧写法 = cur·minPx/maxPx，两者只在 maxPx==nomPx 时相等）
            _tmp.enableAutoSizing = true;
            _tmp.ForceMeshUpdate();
            RefreshBounds();     // 🔴 字号变了 ⇒ 尺寸/摆位都要重算（不然 `WorldW` 还是缩之前的值）
            _defLayoutPushed = true;    // 🆕 A546②：本条是「定版面」那三条之一（见 `AlignAfterLayoutCount`）
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
                 + " base=" + FontSizeBase.ToString("R")          // 🆕 A305①：自适应**起点**（反射读的真字段）
                 + " tmpW=" + _tmpW.ToString("R") + " tmpH=" + _tmpH.ToString("R")
                 // 🆕 A712：垂直档 + 本次施加的字墨位移（世界单位）。⚠️ **只能追加在末尾** ——
                 //    `Editor/MainMenuScene.cs` / `Editor/ShopScene.cs` 的 `NominalFontPx` 会**解析本串**，
                 //    它们读的是 `fontSize=` 后面到**第一个空格**为止（见那两处的注释）⇒ 前面那些字段一个都不能动。
                 + " vAlign=" + _vTier + " vFace=" + _vFace
                 + " vOffset=" + _vOffset.ToString("R");
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
        /// （`SetAutoFitBox` 开 autosize 之后字号会变，第一版就是漏了这一步 ⇒ 量出来的宽度是缩之前的）。
        ///
        /// <para>🔴 **2026-10-15（A712）**：y 那一项多了 <see cref="VOffsetWorldNow"/> 的**字墨校正**——
        /// 原来只有「把行盒摆进锚点」（`-anchor.y * _tmpH - b.min.y`），而原版的摆位是「行盒按档位对齐到框里」，
        /// 两边字体的行盒不对称度不同 ⇒ 即使都是 `Middle`，我们的**字墨**也低 ≈0.13 × fontPx。
        /// ⚠️ 位移只加在 **TMP 子节点**上 ⇒ `Label.transform` 的 `localPosition`、`WorldW/WorldH`
        /// （= `textBounds` 的**尺寸**）**一个都不动** —— 命中区（`Contains`）与两个 `Align*On` 的口径不变。
        /// ⚠️ **每一次都从零重算**（字号会被自适应改掉）⇒ 本函数**幂等**，重排几次都不会累积漂移
        /// （`Editor/BattleScene.cs` 的 A712 那一节有一条专门咬这个的断言）。</para></summary>
        public void RefreshBounds()
        {
            if (_tmp == null) return;
            var b = _tmp.textBounds;
            _tmpW = Mathf.Max(Mathf.Abs(b.size.x), 1e-4f);
            _tmpH = Mathf.Max(Mathf.Abs(b.size.y), 1e-4f);

            // 把整块摆进 [-anchor.x*W, (1-anchor.x)*W] × [-anchor.y*H, (1-anchor.y)*H] ——
            // 和点阵那条 `RebuildMesh` 里的 x0/y0 是**同一套规矩**，所以两条后端可以互换
            // 🔴 y 上再叠一项**字墨校正**（A712；口径与算式只写在 `SetVAlign` / `OurInkCenterWorld` 那两处）
            _vOffset = VOffsetWorldNow();
            _tmp.rectTransform.localPosition =
                new Vector3(-anchor.x * _tmpW - b.min.x, -anchor.y * _tmpH - b.min.y + _vOffset, 0f);

            // 🆕 **2026-10-08（A225-②）**：**挪完再裁那一刀**。
            // 🔴 为什么必须有这一句：文字裁切（`MenuDraw.ClipText`，原版 `RectMask2D` 的等效物）是
            //    **按当时的顶点位置**逐字算的，而「重排」与「摆到位」是两件事 —— TMP 的
            //    `ON_TEXT_CHANGED`（`ClippedTextGuard` 订的就是它）只在 `ForceMeshUpdate()` **里面**发，
            //    那一刻 TMP 子节点还停在**上一次** `RefreshBounds` 摆的位置上（差多少见 A206）。
            //    本函数是**每一条定版面的路的末句**（`SetAutoFitBox` / `ForceRelayout` / 建标签…）
            //    ⇒ 在这里再裁一刀，那一刀才落在文字**真正被画**的地方。
            //    ⚠️ 幂等：`ClipTmpMesh` 写的是**绝对值**（基准 = 该字自己的顶点色，见 `BaseCornerAlpha`）
            //    ⇒ 多裁几刀结果一样（⛔ 别改回「在现值上乘」，那会越裁越暗）。
            //    ⚠️ 没被裁过的标签身上**没有** `ClippedTextGuard` ⇒ 这一句什么都不做（一条 if）。
            ReclipNow();

            // 🆕 2026-10-11（A266）：本函数是**每一条定版面的路的末句**（`SetAutoFitBox` /
            //    `ForceRelayout` / 建标签…）⇒ 也是「未激活时欠下的那一刀」最自然的补做点。
            //    ⚠️ 它**只在对象已激活时**才干活（未激活时生成会造出那个半成品状态）；
            //    有人要真数时另一条路是 `WorldW/WorldH` 的 `EnsureMeasured`。
            // 🔴 **2026-10-15（A546②）**：本函数 = 「版面对齐这一刻已经落定」那一拍 ⇒ 把
            //    `_defLayoutPushed` 归零（那三条「定版面」的方法各自在**自己那次 `RefreshBounds()` 之后**
            //    再把它置 true —— 见 `AlignAfterLayoutCount` 的 doc）。
            _defLayoutPushed = false;
            TryApplyPendingWrap();
        }

        /// <summary>🔴 **2026-10-13（A804）**：**重裁那一刀的唯一一口** —— 被裁过的标签身上挂着
        /// `ClippedTextGuard`（`MenuDraw.ClipText` 挂的），它按**当时的顶点世界位置**逐字算 alpha。
        /// <para>**为什么单开一个方法**：裁切的判据是「节点在哪」，所以**凡是在裁切存在时挪动本节点**的路，
        /// 挪完都得重裁一次 —— `RefreshBounds`（重排/摆位）与 `AlignLeftOn`/`AlignRightOn`（左/右对齐）
        /// **都是这样的路**。原来只有前者调，后者漏了 ⇒ 对齐把整块**连同裁切框一起推走**
        /// （`ShellScene` A743 实测：两次差值**逐位相同 = +123.16px**，即纯平移、不是缩放型的 `PosInDesignSpace` 病）。</para>
        /// <para>⚠️ 幂等：`ClipTmpMesh` 写的是**绝对值**（基准 = 该字自己的顶点色，见 `BaseCornerAlpha`）
        /// ⇒ 多裁几刀结果一样（⛔ 别改回「在现值上乘」，那会越裁越暗）。
        /// ⚠️ 没被裁过的标签身上**没有** `ClippedTextGuard` ⇒ 这一句什么都不做（一条 if）。</para></summary>
        void ReclipNow()
        {
            var guard = GetComponent<ClippedTextGuard>();
            if (guard != null) guard.Reclip();
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
        /// 🔴 **2026-10-11（A228）就地订正（铁律 5）**：本行原文写「**只减位移、不除缩放** —— 本工程 Shell
        /// 这一线的父链**不带 `transform` 缩放**（原版 `localScale` 是用「按矩形显式缩放」实现的，见 `MissionsTab.R`）」
        /// —— **已过期**：A165（2026-10-06）落地小屏缩放器之后，`TransformScalerBySmallScreenUI` 会给
        /// **窗口根**乘 `menuScale`（开关开且 `extraScaleSmallScreen ≠ 1` 时），**父链从此带缩放**。
        /// 那句「按矩形显式缩放」在**窗内子件**这一层仍然成立（`ForgeTab` / `CampaignRewardWindow` 那条纪律），
        /// 但**窗根那一级**是**真的 `Transform.localScale`**（判据 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头 ①）。
        /// ⇒ 位移那一项**必须**除父链缩放，见下面 <see cref="ParentXInDesignSpace"/>。
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
            if (_tmp == null) { NoteDotBackendLacks("左对齐", worldLeftX); return; }
            RefreshBounds();
            if (!HasMeasuredWidth()) return;      // 🔴 见 `HasMeasuredWidth` —— 量不出宽度时**不动位置**
            var p = transform.localPosition;
            transform.localPosition =
                new Vector3(worldLeftX - ParentXInDesignSpace() + WorldW * 0.5f, p.y, p.z);
            ReclipNow();                          // 🔴 A804：平移会**把裁切框一起推走** ⇒ 挪完必须重裁（见 `ReclipNow`）
        }

        public void AlignRightOn(float worldRightX)
        {
            if (_tmp == null) { NoteDotBackendLacks("右对齐", worldRightX); return; }
            RefreshBounds();
            if (!HasMeasuredWidth()) return;      // 🔴 同 `AlignLeftOn`
            var p = transform.localPosition;
            transform.localPosition =
                new Vector3(worldRightX - ParentXInDesignSpace() - WorldW * 0.5f, p.y, p.z);
            ReclipNow();                          // 🔴 A804：同 `AlignLeftOn`
        }

        /// <summary>🔴 **2026-10-12（A476）**：点阵后端下「对齐这回事**根本不存在**」**必须出声**
        /// （`CLAUDE.md` §三：不许静默失败）—— 与同族的 <see cref="SetCharSpacing"/> 同一个口径。
        ///
        /// <para>**为什么要有它**：两个 `Align*On` 原来在 `_tmp == null` 时**直接 return、一个字都不留**
        /// ⇒ 一旦字体资产缺失（`TmpFont.Available == false`，`Core/TmpFont.cs:52`），**整批左/右对齐静默退回居中**
        /// —— 画面错（`RefreshBounds` 把整块摆在框心，`Battle/Label.cs:733-734`）、日志里什么都没有。
        /// 同一族的 `SetCharSpacing` 早就出声（🔴 **A596 之后它走同一只口**，见 `NoteDotBackendLacks` 那段 doc）
        /// ⇒ 当年「两个口口径不一致」本身就是缺陷，A476/A596 已各自收口。</para>
        ///
        /// <para>🔴 **为什么不是「每次调用打一行」**：两个口全仓约 **20 个生产调用点**，且**每个窗每重建一次就跑一遍**
        /// （主入口是 `Shell/MenuDraw.cs:1585-1594` 的 `AlignLeft`/`AlignRight`；`Deck/DeckRuntime.cs:3391` /
        /// `Shell/CollectionWindow.cs:1677` 还会**逐格**调它）—— 逐次刷屏会把别的告警淹掉，而信息量为零。
        /// 本仓先例 = 「同一件事故只出声一次」（`Shell/ItemDrawer.cs:727-737` 的 `Note` ·
        /// `Core/CardIcons.cs:91` 的 `_warned` · `Core/Tooltip.cs:392` 的 `_warnedNoEntry`）
        /// ⇒ 这里 key 取 **「方法 + 节点全路径」**，**每处一次**（路径能认出是哪一窗哪一颗，"Window Title" 这种重名不会互相吞）。</para>
        ///
        /// <para>⛔ **`HasMeasuredWidth()` 那条早退【不】出声**（下一行那句）：那是**有意的守卫**
        /// —— 空串 / 量不出宽时不动位置，理由见 `HasMeasuredWidth` 的文件头（否则会把节点扔到 2.1e9 之外）；
        /// 而且**空文案是正常状态**（零值态面板、等数据的格子），调用方灌进文案后会**再对齐一次**
        /// （例 `Shell/TrophyInfoPopup.cs:461`）。与「这个后端根本没有对齐功能」是两件事，别混。</para>
        ///
        /// <para>⚠️ **进程内静态**：一次 Unity 批处理里就是「每处一次」；批处理之间会重置（同 `Note` 那条）。</para></summary>
        void NoteDotBackendLacks(string which, float worldX)
        {
            NoteDotBackendLacks(which, "没有对齐这回事", which + "（x=" + worldX + "）", "没生效、整块停在框心");
        }

        /// <summary>🔴 **2026-10-13（A491）**：点阵后端那一族出声的**唯一一口** —— **全部**调用点
        /// （`AlignLeftOn` / `AlignRightOn` / <see cref="SetAlignLeft"/> / A545 那 5 个口 / A596 的 `SetCharSpacing`）
        /// **共用它**，它自己只把那句「但……」转给 <see cref="NoteNotApplied"/>
        /// （**key 的拼法 / 去重 / 落日志只有那一份**）。
        /// `which` 既是**口名**（人话）又是 **key 的头一段** ⇒ 不同的口**天然不撞 key**；
        /// key = `which + "|" + 节点全路径` ⇒ 每处一次、且认得出是哪一窗哪一颗。
        /// <para>🔴 **2026-10-13（A598）改名（铁律 5 留痕）**：本函数原名 **`NoteDotAlign`** ——
        /// 那是个**历史名**（它 2026-10-12 出生时只服务对齐那两个口；到 A545 已经管 8 个口，其中只有 3 个是对齐）。
        /// 名字里的 `Align` 会让人以为「它只报对齐」，而它真正管的是「**点阵后端缺这一档功能**」⇒ 改名成
        /// `NoteDotBackendLacks`。**牵动面已核**：本文件全部调用点 + `Editor/SettingsScene.cs` 的 10 处
        /// 注释/断言文案（本件一并改了）；⛔ **那两个文件之外没有任何代码引用它** —— 它是本类**私有**方法
        /// （无访问修饰符 ⇒ `private`），全仓 `grep -rn "NoteDotAlign"` 只剩 `Battle/ScenarioBlendables.cs:2089`
        /// 的一处**注释**（不在本件白名单，⛔ 没动，如实登记；⚠️ 行号是 2026-10-13 现读，会漂）。
        /// ⚠️ 它旁边那只静态集仍叫 `_dotAlignNoted`（**同一个历史名**）—— 本件**没改**它：`Battle/ScenarioBlendables.cs:2094,2099`
        /// 的注释按名字引用了它（`Battle/Label.cs:935` 这样的行号引用也在别处），改名字会留下说不清的引用 ⇒ 另立账。</para>
        /// <para>🔴 **⛔ 别把新的口并进现有的 `which`**：`_dotAlignNoted` 是**进程内静态** HashSet、
        /// **同一个 key 只出声一次** ⇒ 撞了就是「那一处先响过一次之后，这一口**再也不出声**」（静默复发，
        /// 而且只在同一个进程里现形）。断言（含「key 不撞」那一条）→ `Editor/SettingsScene.cs` 的 A491 那节。</para>
        /// <para>出声与否 / 为什么每处只报一次 / 为什么 `HasMeasuredWidth()` 那条早退**不**出声
        /// ⇒ 见上面那一大段 doc，这里不抄第二份。</para>
        /// <para>🆕 **2026-10-13（A545）**：`why` 那一格是为了让它**不止服务对齐那一族** ——
        /// 同族又收了 5 个口（`折行宽` / `换行模式` / `重排` / `字号` / `自适应框`），
        /// 它们「点阵后端缺什么」**各不相同** ⇒ 把原来写死的那句「没有对齐这回事」**参数化**
        /// （A476 / A491 两条消息**逐字节没变**：它们传的仍是 `"没有对齐这回事"`）。</para>
        /// <para>🆕 **2026-10-13（A596）收编第 9 个口**：`SetCharSpacing` 原来走**它自己的** `Debug.Log`
        /// （每次调用打一行、不去重）—— 同族**第 9 个出声口**，所以「key 的拼法只有一份」这句话当时**并不完全成立**。
        /// 现在它也走本口（口名 = `字距`）⇒ **消息文案与刷屏行为都变了**（逐次一行 ⇒ 每处一次），
        /// 这正是本账要的（⛔ 别把它改回去，更别把出声删掉了事）。</para>
        /// <para>📌 **现有 `which`（= key 头段）全表**：`左对齐` · `右对齐`（A476）· `逐行左对齐`（A491）·
        /// `折行宽` · `换行模式` · `重排` · `字号` · `自适应框`（A545）· `字距`（A596）—— 共 **9** 个口走**本函数**；
        /// 另有 `自适应框参数` / `自适应框折算`（A597）走 <see cref="NoteArgInvalid"/>，**同一个 key 集**。
        /// 新增口请挑一个**上表没出现过**的名字；断言 → `Editor/SettingsScene.cs` 的 A545 那一节
        /// （它用「**同一个节点**上依次调 6 个口、每个都要响」来咬「两两不撞 key」）。</para></summary>
        void NoteDotBackendLacks(string which, string why, string wanted, string symptom)
        {
            NoteNotApplied(which, "但**点阵后端" + why + "**", wanted, symptom);
        }

        /// <summary>🔴 **2026-10-13（A597）**：入参不成立那一族（**不是**「这个后端缺这一档」）——
        /// `SetAutoFitBox` 内部原来**两条静默 `return`**（`cur` / `minPx` / `maxPx` 里有非正数、`nomPx` 折算不出来），
        /// 调用方以为「字号已经夹进框里了」，日志里一个字都没有 ⇒ 与 `CLAUDE.md` §三「不许静默失败」打架。
        /// <para>🔴 **为什么另开一只、而不是直接用上面那只**：那一只的消息模板写死「**但\*\*点阵后端…\*\***」——
        /// 而这两条是**入参**的错（同一个 label 完全可能**有** TMP）：把入参非法报成「点阵后端缺这一档」
        /// 就是**归因错**（A545 在 `SetFontSize` 上刻意躲开过同一个错，见那里的方法头）。
        /// ⇒ 两只口**只差那句「但……」**，key 的拼法 / 去重 / 落日志**仍然只有一份**（都在 `NoteNotApplied`）。
        /// ⛔ **别再往下加第三只**：要加就加一句「但……」 clause。</para>
        /// <para>📌 **今天够不到（2026-10-13 现核）**：`cur = TmpFontSize()` 恒 > 0（`CapWorld` 有常数兜底
        /// `7f/100 × max(1, scale)`）；`nomPx = FontSizeToPx(cur)` 也恒 > 0（`TmpFont.MeasureGlyph` 永不返回 ≤ 0：
        /// 无字体资产 → `1f`、量不出 → `0.01f`，`Core/TmpFont.cs:272-300`）；`minPx/maxPx` 那边
        /// **全仓 `SetAutoFitBox(` 的调用点逐个现核过**（生产 39 + 自检探针 10）：每一处要么传正字面量，
        /// 要么上游就有 `autoMinPx > 0f` / `fontPx > autoMinPx` / `AutoMax > AutoMin` 这样的闸
        /// （`Shell/MenuDraw.cs:1602,1635` · `Shell/OfferContainer.cs:1347` · `Shell/CollectionWindow.cs:1704` ·
        /// `Shell/MatchLogRow.cs:163` · `Shell/PlayerProfileWindow.cs:329,637` · `Shell/MissionsTab.cs:350`）；
        /// 表驱动的两处（`Shell/MenuWindowBase.cs:489` 的 `TabBtnSpec` · `Shell/PlayerProfileWindow.cs:330` 的
        /// `PpTabSpec`）**逐行核过**，各表 min 全是 5~18。
        /// **⇒ 它是「将来某一天有人传 0」的兜底，不是今天的病灶** —— 但按铁律 11（复刻有缺漏就要补），
        /// 静默那一格必须堵上。</para>
        /// <para>⛔ **别把它并进 `自适应框` 那一格**（撞 key = 静默复发，理由见上面那只口）。
        /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para></summary>
        void NoteArgInvalid(string which, string why, string wanted, string symptom)
        {
            NoteNotApplied(which, "但**入参不成立（" + why + "）**", wanted, symptom);
        }

        /// <summary>🔴 **2026-10-13（A596 / A597 / A598）**：**这一族唯一的落笔处** ——
        /// 节点全路径的拼法 / key 的拼法（`which + "|" + path`）/ 去重集 / `Debug.Log` 的模板**只此一份**。
        /// 上面两只口（<see cref="NoteDotBackendLacks"/> · <see cref="NoteArgInvalid"/>）只负责挑那句
        /// 「但**……**」（`butClause`）⇒「同族不许各写一套消息」这条规矩仍然成立，只是**族的范围**从
        /// 「点阵后端缺这一档功能」扩成「这一处要 X，但因为 Y，没生效」。
        /// ⚠️ A476 / A491 / A545 那 8 条消息**逐字节没变**（`butClause` 传的仍是 `"但**点阵后端" + why + "**"`）。</summary>
        void NoteNotApplied(string which, string butClause, string wanted, string symptom)
        {
            string path = name;
            for (var t = transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            if (!_dotAlignNoted.Add(which + "|" + path)) return;
            Debug.Log("[Label] ⚠️ 这一处要" + wanted + "，" + butClause + " ⇒ " + symptom
                      + "（出声，不静默；同一处只报一次）。节点 = " + path);
        }
        static readonly System.Collections.Generic.HashSet<string> _dotAlignNoted =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>🆕 **A228**：父节点的**世界 x → 设计空间**（= 除掉父链的 `lossyScale.x`）。
        ///
        /// <para>**为什么必须有这一步**：`Align*On` 收的实参是**设计空间**的 x（调用侧一律
        /// `LayoutSpace.FromPixel(...)` / 裸世界 x —— 与 `Label.Create` 的 `pos`、`MenuDraw.Local`
        /// 算出来的那一套**同一量纲**），而 `transform.parent.position.x` 是**已缩放的视觉世界 x**。
        /// 两者只在「父链 `lossyScale == 1`」时才同量纲。</para>
        ///
        /// <para>**精确式**（设父的世界位置 `P`、父链缩放 `k`；窗口根在世界原点 —— 全壳都满足：
        /// `ShellRuntime.Build` 的根 = `new GameObject("Shell")`（无父 ⇒ 世界原点），三个 `WindowHolder`
        /// 也是 `SetParent(root,false)` ⇒ 窗口根 `AttachToAnchor` 后停在原点）：
        /// `<c>localPosition.x = worldX − P/k ± W/2</c>`。`W`（`WorldW`）本来就是**局部**长度 ⇒ 那一项不用动。
        /// 不除 `k` 时的偏差 = `(1−k)·(P/k)`（局部单位；世界单位是 `(1−k)·P`）——
        /// `P` 越靠窗根这一项越小，所以**窗根附近看不出来、离得越远偏得越多**。</para>
        ///
        /// <para>⚠️ **只在「窗口根在世界原点」时精确** —— 根一旦有偏移 `Rx`，正确式是
        /// `worldX − (P − Rx)/k`，而本函数**拿不到 `Rx`**（只有父的绝对世界位置）⇒ 会多出 `Rx/k`。
        /// 全仓的 UI 根（Shell 的 `Shell` 根、战斗 HUD 那一棵）实测都在原点；**将来哪条路把 UI 根挪走，
        /// 这里要改成 `parent.InverseTransformPoint`（等价于 A228 的选项 (c)）**。</para>
        ///
        /// <para>🔴 **默认关着 ⇒ 今天零可观测差异**：小屏开关出厂 `PlayerPrefs` = 0（原版 `GameStaticData.cctor`
        /// 也是 0）⇒ 窗根本没接缩放器 ⇒ `k == 1` ⇒ 本函数**逐位等于**旧写法。
        /// 断言必须**两态**（关 / 开），判据 → `Editor/SettingsScene.cs` 的 A228 那两组。</para>
        ///
        /// <para>⚠️ **退化档**（`k` 非有限 / ≈0）：那意味着父链被压平，字本来也画不出来 ⇒ 照旧走 `P`（不除），
        /// 只在这一档与旧写法一致 —— 不加别的兜底，也不出声（与 `HasMeasuredWidth` 那种「量不出来就什么都不做」
        /// 是同一类处置，且这一档下改与不改都看不见）。</para></summary>
        float ParentXInDesignSpace()
        {
            var pt = transform.parent;
            if (pt == null) return 0f;
            float k = pt.lossyScale.x;
            if (float.IsNaN(k) || float.IsInfinity(k) || Mathf.Abs(k) < 1e-6f) return pt.position.x;
            return pt.position.x / k;
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
        //  垂直档（A712：把「字墨」摆到原版的位置上）
        // ==================================================================

        /// <summary>垂直档 = 原版 TMP 的 `m_VerticalAlignment`（枚举值**逐值照抄那一格**，便于与原版存档互查）。
        /// 判据 = 本机 TMP `TMP_Text.cs:24-85`：`Top = 0x100` · `Middle = 0x200` · `Bottom = 0x400` ·
        /// `Geometry`（别名 `Midline`）= `0x1000` · `Capline = 0x2000`。
        /// 原版逐档占比（主菜单 214 / 战斗 333 颗 TMP）→ `资料/普查产出_1013/WA712_垂直对齐普查.md` §二：
        /// `Middle` 64.0/67.3% · `Midline` 23.4/25.8% · `Capline` 9.3/3.0% · `Bottom` 2.8/2.4% · `Top` 0.5/1.5%。</summary>
        public enum VAlign
        {
            Top = 0x100, Middle = 0x200, Bottom = 0x400, Midline = 0x1000, Capline = 0x2000,
        }

        /// <summary>原版那两份字体度量选哪一份（<see cref="SetVAlign"/> 的第三实参）。
        /// 🔴 **两份给的数不一样**（`Middle` 的墨心目标 +0.0526 F vs +0.0851 F）⇒ 逐处要按原版那一颗
        /// **实际用的字体**选；哪一档用哪份 → `WA712` §2·4（主菜单：`Capline` 19 颗 Pragati / 1 颗 Asar ·
        /// `Midline` 20 / 30；战斗：`Capline` 2 / 8 · `Midline` 33 / 53）。</summary>
        public enum OrigFace { Pragati = 0, Asar = 1 }

        /// <summary>原版字体度量（单位 = 字体自己的 `pointSize`）。
        /// <para>🔴 **判据 = 解包资产字段的原文**（不是我们量出来的数）：<br/>
        /// `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/MonoBehaviour/Pragati-Regular SDF.json` ——
        /// `m_FaceInfo`：`m_PointSize` 95 · `m_AscentLine` 70 · `m_CapLine` 60 · `m_DescentLine` −20<br/>
        /// 同目录 `Asar-Regular SDF.json` —— 94 / 80 / 61 / −35<br/>
        /// （两个 pid：`3485036404935369831` / `-8244042478085975641`，`WA712` §2·4 现读）。
        /// 本工程另有一条**独立实测**只覆盖了中间的比值：`Editor/Round1015Probe.cs:181-186`（`CmpOriginal`）。</para>
        /// <para>⚠️ **参数名对齐 TMP 的 `FaceInfo`**：`p` = pointSize · `a` = ascentLine · `c` = capLine ·
        /// `d` = descentLine（`descentLine` 原版是**负**的，别丢掉那个负号）。</para></summary>
        struct OrigMetrics { public float p, a, c, d; }

        static readonly OrigMetrics Pragati = new OrigMetrics { p = 95f, a = 70f, c = 60f, d = -20f };
        static readonly OrigMetrics Asar = new OrigMetrics { p = 94f, a = 80f, c = 61f, d = -35f };

        /// <summary>画布 px ÷ 世界单位（= `LayoutSpace.DesignPxH / DesignHeight` = 108）。
        /// 与 <see cref="FontSizeToPx"/> 那条口径**同源**（这里只把它写成一处，别在两处各写一遍算式）。</summary>
        static float PxPerWorld { get { return LayoutSpace.DesignPxH / LayoutSpace.DesignHeight; } }

        VAlign _vTier = VAlign.Middle;      // 出厂 = `Middle`（原版 64~67% 的件是它；本阶段全工程都用这一档）
        OrigFace _vFace = OrigFace.Pragati; // 默认取**主力**那一份（`WA712` §2·4：Pragati 是主力）
        float _vBoxH;                       // 原版那一颗的**框高**（世界单位）；<= 0 = 调用方没给
        /// <summary>`SetAutoFitBox(…, worldH, …)` 写进去的那个框高（世界单位）—— <see cref="VOffsetWorldNow"/>
        /// 在调用方没显式给框高时的**唯一**兜底来源（见 `SetAutoFitBox` 里那一行）。
        /// ⛔ **别改成现读 `_tmp.rectTransform.sizeDelta.y`**：那是「最后一次谁写过它」，不是「原版框高」。</summary>
        float _boxHFromAutoFit;

        /// <summary>最近一次 <see cref="RefreshBounds"/> 真正施加的垂直位移（世界单位，向上为正）。</summary>
        float _vOffset;

        /// <summary>读回**当前生效**的垂直档（**自检 / 诊断用**）。
        /// ⚠️ 点阵后端下永远报 `Middle` —— 那里 <see cref="SetVAlign"/> **出声后直接返回、一个字段都不写**
        /// （如实报「实际在生效的那一档」，不报调用方想要的那一档）。</summary>
        public VAlign VAlignTier { get { return _vTier; } }

        /// <summary>读回当前施加的垂直位移（**世界单位**，向上为正）—— **自检 / 诊断用**。
        /// 🔴 ⛔ **不许拿它当断言的期望值**（那是拿我们自己的算式证明我们自己 —— 自证）。
        /// 期望值要写成**原版的档位目标**（<see cref="OrigInkCenterPx"/> 那张表的数），实测那一侧要
        /// **量渲染出来的字墨**（`characterInfo[i].topLeft/bottomLeft`），见 `Editor/BattleScene.cs` 的 A712 那一节。</summary>
        public float VOffsetWorld { get { return _vOffset; } }

        /// <summary>原版那一档下「**大写墨盒中心**」相对**框心**的位置（画布 px，向上为正）——
        /// 这就是「档 → 墨心相对框心」那张表（逐格算式与出处 → `资料/普查产出_1015/W11_A712字墨校正.md` §3）。
        ///
        /// <para>**算式来自 TMP 自己的那一支 `switch`**（`TextMeshPro.cs:4193-4232` 的 `anchorOffset` = **基线**的位置）：
        /// `Top = 上角 − ascent` · `Middle = 心中 − (ascent+descent)/2` · `Bottom = 下角 − descent` ·
        /// `Capline = 心中 − capLine/2` · `Geometry/Midline = 心中 − (墨水顶+墨水底)/2`；
        /// 再把基线折成**大写墨盒中心**（= 基线 + capLine/2，大写墨盒 = 基线到 capLine 那一格）。</para>
        ///
        /// <para>🔴 **为什么用「大写墨盒」而不是「这一串字真实渲出来的那块」当代理**：原版那一档的位移
        /// **只跟字体度量走、跟串里有几个下伸部无关**（`Middle`/`Capline`/`Bottom`/`Top` 四条都不看字形）。
        /// 我们若改用「逐串量出来的字形四边形」当基准，同一档下 `PLAY` 与 `Points` 会被摆到**两个不同高度**
        /// —— 那是原版没有的自由度。（实测两个基准差多远：fs35 下 `Counter` 差 **0.19px**、`PLAY` 差 **0.30px**、
        /// `Points` 差 **0.95px` —— 判据 `Editor/Round1015Probe.cs` 的输出 `d:/4/_tmp_view/valign_probe.txt`。）</para>
        /// <para>⚠️ **`Midline` 只对「全大写 / 数字串」给 0** —— `Geometry` 那一档 TMP 用的是**这一串的墨水盒**，
        /// 带下伸部的串（`Single Counter` 那种 `g`/`p`）要**逐串算**（`WA712` §4·5 / §六·2 明说是范围、不是定值）
        /// ⇒ 那是**第二阶段**的事，本阶段不许给别处填这个坑。</para>
        /// </summary>
        /// <param name="tier">原版那一颗的档（<see cref="VAlign"/>）。</param>
        /// <param name="m">那一颗的**字体**度量（<see cref="Pragati"/> / <see cref="Asar"/>）。</param>
        /// <param name="F">原版那一颗的 `m_fontSize`（**画布 px**）—— 喂**实际渲染**那一档（<see cref="FontPxNow"/>）。</param>
        /// <param name="boxH">原版那一颗的**框高**（画布 px）；只有 `Bottom` / `Top` 用得到它。</param>
        static float OrigInkCenterPx(VAlign tier, OrigMetrics m, float F, float boxH)
        {
            switch (tier)
            {
                case VAlign.Middle:  return (m.c * 0.5f - (m.a + m.d) * 0.5f) / m.p * F;
                case VAlign.Capline: return 0f;    // 基线 = 心中 − c/2 ⇒ 大写盒心**正好**落在框心
                case VAlign.Midline: return 0f;    // 同上（全大写/数字串）；带下伸部的串见上面那条 ⚠️
                case VAlign.Bottom:  return -boxH * 0.5f + (-m.d + m.c * 0.5f) / m.p * F;
                case VAlign.Top:     return boxH * 0.5f - (m.a - m.c * 0.5f) / m.p * F;
            }
            return 0f;
        }

        /// <summary>「**一行**行盒」的高度（世界单位）—— 取 TMP **自己算出来的那一份**：
        /// `textInfo.characterInfo[i].ascender − descender`（`textBounds` 就是逐字取这两个值的并集，
        /// `TMP_Text.cs:4875-4879`）⇒ **多行也拿得到「一行」的高度**。
        /// ⚠️ 为什么不直接用 `_tmpH`（`textBounds` 的高）：多行时它是**整块**的高 ⇒ 当作一行用会大 N 倍。
        /// ⚠️ 一个字都量不到（空串 / 版面没生成）时退回 `_tmpH` —— 单行时两者同值；
        /// 而空串那种退化状态下它是**哨兵天文数字**（4.29e9，见 <see cref="HasMeasuredWidth"/>）⇒
        /// 由 <see cref="OurInkCenterWorld"/> 的守卫挡掉（不拿垃圾数去摆位）。</summary>
        float OneLineBoxWorld()
        {
            var ti = _tmp.textInfo;
            if (ti != null && ti.characterInfo != null)
                for (int i = 0; i < ti.characterCount && i < ti.characterInfo.Length; i++)
                {
                    var ci = ti.characterInfo[i];
                    if (!ci.isVisible) continue;
                    float h = ci.ascender - ci.descender;
                    if (h > 0f) return h;
                }
            return _tmpH;
        }

        /// <summary>🔴 **A712 的核心算式（只有这一份）**：我们的**大写墨盒中心**现在落在哪
        /// （相对本节点，世界单位，向上为正）。
        ///
        /// <para>**推导**：当前摆位是「行盒中心落在节点上」（见 <see cref="RefreshBounds"/>）。设行盒
        /// 上沿 = `a`、下沿 = `d`、基线在 0、大写线 = `c`（都是**字体单位**，除 `p` = `pointSize` 归一）⇒
        /// · 基线相对行盒中心 = `−(a+d)/(2p)`；
        /// · 大写盒心相对基线 = `c/(2p)`；
        /// ⇒ **墨盒心相对行盒中心 = `(c/2 − (a+d)/2)/p` × 一个 em 的世界高**。</para>
        ///
        /// <para>**三个比值现读我们字体资产的 `faceInfo`**，乘的那个「一个 em 有多高」用
        /// **TMP 自己排出来的行盒**（<see cref="OneLineBoxWorld"/>）反除 `(a−d)/p` 得到
        /// —— ⛔ **别写死 `0.1` / `1.437` / `0.0725` 这类从我们这份资产量出来的常数**
        /// （换字体资产/换烘焙档就全错，而且是静默错）。
        /// 实测对照（探针 `d:/4/_tmp_view/valign_probe.txt`）：理论 **−0.0725 em** ·
        /// 真渲出来 `PLAY` −0.0645 / `Counter` −0.0675 / `Points` −0.047 —— 同向、量级一致。</para></summary>
        float OurInkCenterWorld()
        {
            float lineBox = OneLineBoxWorld();
            // 守卫：量不出来（空串 ⇒ 哨兵 4.29e9）或不是画布尺度的量 ⇒ 不位移（同 `HasMeasuredWidth` 那条口径）。
            if (!(lineBox > 0f) || lineBox > 100f) return 0f;
            var fi = _tmp.font != null ? _tmp.font.faceInfo : default(UnityEngine.TextCore.FaceInfo);
            float p = fi.pointSize;
            float span = fi.ascentLine - fi.descentLine;
            if (!(p > 0f) || !(span > 0f)) return 0f;         // 字体度量拿不到 ⇒ 后面那条会让它出声
            return lineBox * (fi.capLine * 0.5f - (fi.ascentLine + fi.descentLine) * 0.5f) / span;
        }

        /// <summary>这一次 <see cref="RefreshBounds"/> 要加在 **TMP 子节点 y** 上的位移
        /// （世界单位，向上为正）—— **档位换算只此一份**（`Shell/MenuDraw.cs` 那层只转发）。
        /// <para>= 「原版那一档的墨心目标位置」−「我们现在墨心在哪」（两者都量到**本节点**上）。
        /// ⚠️ 每一次都**从零重算**（字号会被自适应改掉、框高可能后给）—— `RefreshBounds` 是幂等的，
        /// ⛔ 别改成「在现位上再挪一点」（那会随重排次数累积漂移）。</para>
        /// <para>`Bottom` / `Top` 要**框高**：调用方给（<see cref="SetVAlign"/> 第 2 实参）或吃
        /// <see cref="SetAutoFitBox"/> 记下的那一份（`_boxHFromAutoFit`）；**两处都拿不到 ⇒ 出声**并退回
        /// `Middle` 那一档（不许静默拿 0 当框高 —— 那会让目标位置悄悄错半框）。</para></summary>
        float VOffsetWorldNow()
        {
            var m = _vFace == OrigFace.Asar ? Asar : Pragati;
            float boxHpx = 0f;
            if (_vBoxH > 0f) boxHpx = _vBoxH * PxPerWorld;
            else if (_boxHFromAutoFit > 0f) boxHpx = _boxHFromAutoFit * PxPerWorld;
            VAlign tier = _vTier;
            if ((tier == VAlign.Bottom || tier == VAlign.Top) && !(boxHpx > 0f))
            {
                // 🔴 不许静默失败（CLAUDE.md §三）：这一档算不出目标位置 ⇒ 出声，并按 `Middle` 走。
                NoteNotApplied("垂直档框高", "但**这一档要框高、而它两处都没给**",
                               "一个 `" + tier + "` 档（要框高）",
                               "退回 `Middle` 那一档（框高拿不到 ⇒ Bottom/Top 的目标位置算不出来）");
                tier = VAlign.Middle;
            }
            return OrigInkCenterPx(tier, m, FontPxNow, boxHpx) / PxPerWorld - OurInkCenterWorld();
        }

        /// <summary>设**垂直档**（= 原版那一颗的 `m_VerticalAlignment`），并把**字墨**摆到原版那一档的位置上。
        ///
        /// <para>**为什么是这个做法（而不是把 TMP 的 `alignment` 设一设）**：实测（2026-10-15）TMP 自带的
        /// 纵向档位枚举在本类这条路上**是空转** —— 档位只挪行盒，而 <see cref="RefreshBounds"/> 每次
        /// 都按 `textBounds.min` 把行盒重新摆正 ⇒ 五档下「行盒 vs 字墨」的关系逐位相同
        /// （判据 `资料/普查产出_1015/主对话_Unity腿与裁定.md` §1·2 + `valign_probe.txt` 全表）。
        /// ⇒ **必须是显式位移**。</para>
        ///
        /// <para>**出厂那一档 = `Middle`**（原版 64~67% 的件是它）⇒ **本阶段全工程 340 个文字创建入口
        /// 一个都不用改**：不调本函数就是 `Middle`，而 `Middle` 的校正照常生效。
        /// 那 1/3 非 `Middle` 的**逐处档位**是第二阶段（`WA712` §三·1 那张「80 行注释 / 24 个文件」清单
        /// 就是现成的逐处判据）。</para>
        ///
        /// <para>⚠️ **前提**：本模型假设「**节点位 = 原版那一颗的框心**」（`RefreshBounds` 按
        /// `anchor` 摆，全仓 61 个 `Label.Create` 调用点的**纵向 anchor 100% 是 0.5** ⇒
        /// 行盒心就落在节点上）。`anchor.y != 0.5` 的**只有两处**（2026-10-15 现读：
        /// `Battle/SettingsPanel.cs:392` 的音量滑条标签 · `Battle/BattleDriver.cs:7958` 的 `HandLabel`
        /// —— 都是 `(0,0)` = 「文字块**左下角**落在节点上」）**不在这个前提里**；它们照样吃 `Middle` 的
        /// 全局校正（本阶段口径 = 全工程统一），但「框心」对它们**没有定义** ⇒ 要按档精确摆得先给定框心，
        /// **如实登记、没猜**（见报告 §5）。</para>
        /// </summary>
        /// <param name="tier">原版那一颗的档。</param>
        /// <param name="boxHWorld">原版那一颗的**框高**（世界单位；`&lt;= 0` = 不给，改用 `SetAutoFitBox` 记下的那一份）。
        /// 世界单位 = `LayoutSpace.Px(原版矩形高 px)` —— Shell 那一侧走 `MenuDraw.SetVAlign` 转发。</param>
        /// <param name="face">原版那一颗用的**字体**（默认主力 `Pragati`；逐处判据 → `WA712` §2·4）。</param>
        public void SetVAlign(VAlign tier, float boxHWorld = 0f, OrigFace face = OrigFace.Pragati)
        {
            if (_tmp == null)
            {   // 点阵后端没有「行盒 / 字墨」这回事 —— **要出声**（红线：不许静默失败），且**不写字段**
                //（读回要如实报「实际在生效的那一档」= 出厂 `Middle`）。
                NoteDotBackendLacks("垂直档", "没有行盒/字墨这回事",
                                    "一个 `" + tier + "` 垂直档",
                                    "没生效（这一档的文字仍按点阵那块矩形居中）");
                return;
            }
            _vTier = tier;
            _vFace = face;
            _vBoxH = boxHWorld;
            RefreshBounds();          // 立刻按新档重摆（本类是「每一条定版面的路的末句」那一套）
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
