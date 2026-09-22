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

        /// <summary>改**渲染队列**（见 `ImageQuad.SetRenderQueue` 的注释）。
        /// 面板上的文字要跟着面板一起压住一切时用它 —— `fontMaterial` 会**实例化**一份，
        /// 所以改这个不会波及别处的文字。</summary>
        public void SetRenderQueue(int q)
        {
            if (_tmp != null) { _tmp.fontMaterial.renderQueue = q; return; }
            // 🔴 2026-09-21 补：**点阵后端原来什么都不做** ⇒ 用点阵渲染的文字
            //    **永远留在默认队列 3000**，被队列更大的面板盖住（tooltip 就是这么「面板在、字不在」的）。
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.renderQueue = q;
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

        /// <summary>当前**实际生效**的字号换算成「像素」口径（= `fontSize × WorldGlyphPerFontSize × 108`）。
        /// **自检拿它跟原版的 `m_fontSize` 比**（原版那也是画布像素）。
        /// ⚠️ 别用 `CapHeightWorld` / `GlyphHeightWorld` 去量 —— 那两个是**回读传入值**的伪测量（见 `已知的坑.md`）。</summary>
        public float FontPxNow { get { return _tmp != null ? _tmp.fontSize * TmpFont.WorldGlyphPerFontSize * 108f : 0f; } }

        /// <summary>
        /// **给文字一个框**（宽 × 高，世界单位），并开**自动缩放**（TMP 的 `enableAutoSizing`）。
        ///
        /// 为什么需要：原版主菜单那几处文字都带 `m_enableAutoSizing=1`
        /// （导航标签 **18→33** · `Player Name` **10→32** · 模式卡标题 **18→72**）——
        /// 也就是说**字号是自适应出来的**，卡在框里；我们只按最大值摆会溢出
        /// （实测：`COLLECTION` 在 146.9px 宽的条里、em 33px 会**画出框外被裁**）。
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
            //    这里改成**按比例**算，不猜单位：当前 `fontSize`（已由 `SetGlyphHeight(px/108)` 设成「em = maxPx」）当 max，
            //    min 按原版的 `minPx/maxPx` 比例给。
            float cur = _tmp.fontSize;
            if (cur <= 0f || minPx <= 0f || maxPx <= 0f) return;
            _tmp.fontSizeMax = cur;
            _tmp.fontSizeMin = cur * (minPx / maxPx);
            _tmp.enableAutoSizing = true;
            _tmp.ForceMeshUpdate();
            RefreshBounds();     // 🔴 字号变了 ⇒ 尺寸/摆位都要重算（不然 `WorldW` 还是缩之前的值）
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

        /// <summary>自检用：把内部的 sizing 状态原样吐出来（截图和 `GlyphHeightWorld` 都看不出「到底谁把它清成 0 了」）</summary>
        public string DumpSizes()
        {
            return "cap=" + _capHeight.ToString("R") + " glyph=" + _glyphHeight.ToString("R")
                 + " scale=" + scale + " calls=" + _sizeCalls
                 + " tmp=" + (_tmp != null) + " fontSize=" + TmpFontSize().ToString("R")
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
