// ImageQuad.cs — 世界空间的一张图（HUD 用）
//
// 和 `Label` 是一对：Label 画字，这里贴图。没有 uGUI —— 卡牌/粒子全是世界空间的，
// HUD 也走同一套坐标，省得在 Canvas 和世界坐标之间来回换算。
//
// 原版复刻用的图从 `CardArt.Ui(...)` 取（`Resources/Art/ui/`）；没有图时 `Create` 返回 null，
// 调用处要判空 —— 这样删掉美术目录 HUD 也不会炸，只是变成纯文字。
using UnityEngine;

namespace CardPresentation
{
    public class ImageQuad : MonoBehaviour
    {
        /// <summary>**画布像素 → 世界单位**的换算：1 px = 1/108 世界单位。
        ///
        /// 🔴 **2026-10-03 订正（原来写的是 `100f`）**：这里的 px 一律指**画布像素**（1920×1080 设计像素，
        /// 左上原点 —— `menu_dump` / 场景 JSON 里那些数），而本工程 **1 世界单位 = 108 画布像素**
        /// （判据 = `Core/LayoutSpace.cs:110` 那条：「可见高度固定 10 个世界单位 = 1080 px」
        ///  ⇒ `LayoutSpace.Px()` 就是 `px ÷ 108`）。写成 `100` 会让**每一条九宫格/平铺整体大 8%**，
        /// 实测：左栏 `Highlight` 的角块 `30 ÷ 0.92 = 32.61` 画布像素，渲出来是 **35.22**（= 32.61 × 1.08）
        /// —— `Editor/RewardsScene.cs` 的「§三·b4-b」那条断言量到了、且当时是红的。
        ///
        /// ⚠️ **不是「和 `Label` 同口径」**（原来这么写是错的）：`Battle/Label.cs:22` 另有一份**私有**的
        /// `PixelsPerUnit = 100f`，管的是**点阵后端**的字块尺寸与 HUD 的整数 scale 档，**与本类这个常量无关**
        /// （改这里不会动它）。本常量**只**被下面 `CreateNineSlice` / `CreateTiled` 两处用来把 px 折成世界尺寸。
        ///
        /// 📌 值写成 `LayoutSpace` 的推导式（而不是字面量 `108f`）—— 让「换算只有一处」是**结构性**的：
        /// `DesignPxH / DesignHeight` 一变，这里跟着变，不会再各写一个数（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        public const float PixelsPerUnit = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

        public Vector2 anchor = new Vector2(0.5f, 0.5f);

        MeshRenderer _mr;
        MeshFilter _mf;
        Texture _tex;
        float _worldH = 1f;
        float _aspect = 1f;

        public float WorldH { get { return _worldH; } }
        public float WorldW { get { return _worldH * _aspect; } }

        /// <summary>建一张图。`worldHeight` 是**屏幕上的高度**（世界单位），宽度按原图比例走。
        /// 贴图为空则返回 null（调用处判空）。
        /// ⚠️ `tex` 收 `Texture` 而不是 `Texture2D` —— 结算视频那层要贴 `RenderTexture`。</summary>
        public static ImageQuad Create(Transform parent, Texture tex, Vector3 pos, float worldHeight,
                                       Vector2 anchor, string name = null)
        {
            if (tex == null) return null;

            var go = new GameObject(name ?? ("img_" + tex.name));
            go.transform.SetParent(parent, false);

            var q = go.AddComponent<ImageQuad>();
            q.anchor = anchor;
            q._tex = tex;
            q._aspect = tex.height > 0 ? tex.width / (float)tex.height : 1f;
            q._worldH = worldHeight;

            q._mf = go.AddComponent<MeshFilter>();
            q._mr = go.AddComponent<MeshRenderer>();
            q._mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            q._mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q._mr.receiveShadows = false;
            q.SetTexture(tex);
            q.RebuildMesh();
            go.transform.localPosition = pos;
            return q;
        }

        public Texture Texture { get { return _tex; } }

        /// <summary>把贴图**内接**进一个框，返回内接后的**高**（世界单位）—— 这就是原版
        /// `Image.m_PreserveAspect` 的语义：**等比放进框、居中**（宽由 `WorldW` 自然得出）。
        ///
        /// 🔴 **为什么非要有这个入口**：`Create` 只吃「高」，宽一律 = 高 × 贴图比例 ⇒ **可以超出框**。
        ///   牌堆底板就是这个坑：原版那一格 `preserveAspect`、框 **230×229.85**（方），
        ///   而图 `UI_Deck_Background` 是 **364×346**（横）⇒ 原版**按宽定**、实绘 **230×218.63**；
        ///   我们原来只给高 230 ⇒ 实绘 **241.96×230**（**宽出框 11.96px、高出 11.4px = +5.2%**）。
        ///
        /// 判据 = uGUI `Image.GetDrawingDimensions(preserveAspect)`：**图比框宽 ⇒ 按宽定；否则按高定**。
        /// 与菜单侧那条**同一条算法**（`MenuDraw.Rect:68-73` / `MenuWindowBase.Rect` 只是先内缩矩形再建）。</summary>
        public static float FitHeight(float boxW, float boxH, float sprAspect)
        {
            if (boxW <= 0f || boxH <= 0f || sprAspect <= 0f) return boxH;
            return sprAspect > (boxW / boxH) ? boxW / sprAspect : boxH;
        }

        public void SetTexture(Texture t)
        {
            _tex = t;
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.mainTexture = t;
            if (t != null && t.height > 0) _aspect = t.width / (float)t.height;
        }

        /// <summary>换掉材质（结算视频要自建的「左右拼 alpha」合成 shader，
        /// 默认的 `Sprites/Default` 不会拆左右半）。**贴图会跟着带过去**，不然换完是空白。</summary>
        public void SetMaterial(Material m)
        {
            if (_mr == null || m == null) return;
            m.mainTexture = _tex;
            _mr.sharedMaterial = m;
        }

        public void SetTint(Color c)
        {
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.color = c;
        }

        /// <summary>当前染色（含 alpha）。**自检用它验「半透明底板没被画成实心」** ——
        /// 一张 α0.694 的板子画成 α1 在截图上很容易看漏（尤其底下本来就有图案的时候）。</summary>
        public Color Tint
        {
            get { return (_mr != null && _mr.sharedMaterial != null) ? _mr.sharedMaterial.color : Color.white; }
        }

        /// <summary>
        /// 改**渲染队列**。给「要压住一切」的面板用（日志面板、结算面板那种）。
        ///
        /// 为什么需要它：`Sprites/Default` 和粒子都在**透明队列 3000**，同队列下谁压谁由
        /// **距离排序**决定 —— 而粒子系统的排序用的是它自己的**包围盒中心**，粒子一散开包围盒就变大，
        /// 排序结果和肉眼看到的对不上（2026-09-13 实测：把面板一路推到 z = −4，烟照样穿在面板上面）。
        /// **4000 = Overlay：脱离排序，最后画。**
        /// </summary>
        public void SetRenderQueue(int q)
        {
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.renderQueue = q;
        }

        /// <summary>当前渲染队列（**分层靠它，不靠 z** —— 见 `DeckRuntime` 那条注释：
        /// 透明队列按「到相机的 3D 距离」排序，铺满屏的图会互相盖错）。</summary>
        public int RenderQueue
        {
            get { return (_mr != null && _mr.sharedMaterial != null) ? _mr.sharedMaterial.renderQueue : 0; }
        }

        /// <summary>强制宽高比，**盖掉从贴图推出来的那个**。
        /// 结算视频要用：RT 是左右拼的 3840×1080（比例 3.56），显示区却是 1920×1080。</summary>
        public void SetAspect(float aspect)
        {
            if (aspect <= 0f || Mathf.Approximately(_aspect, aspect)) return;
            _aspect = aspect;
            RebuildMesh();
        }

        public void SetWorldHeight(float h)        {
            if (Mathf.Approximately(h, _worldH)) return;
            _worldH = h;
            RebuildMesh();
        }

        /// <summary>重新贴到某个归一化锚点（切分辨率要调）。**保留 z** —— HUD 靠它分层</summary>
        public void SetAnchorPosition(float x01, float y01)
        {
            var p = LayoutSpace.ToWorld(x01, y01);
            p.z = transform.localPosition.z;
            transform.localPosition = p;
        }

        void RebuildMesh()
        {
            float w = WorldW, h = WorldH;
            float x0 = -anchor.x * w, y0 = -anchor.y * h;

            var m = new Mesh { name = "ImageQuad" };
            m.vertices = new[]
            {
                new Vector3(x0, y0, 0f), new Vector3(x0 + w, y0, 0f),
                new Vector3(x0 + w, y0 + h, 0f), new Vector3(x0, y0 + h, 0f),
            };
            // UV 默认是整张图；九宫格/平铺靠 `SetUvRect` 只取一块
            m.uv = new[]
            {
                new Vector2(_uv.xMin, _uv.yMin), new Vector2(_uv.xMax, _uv.yMin),
                new Vector2(_uv.xMax, _uv.yMax), new Vector2(_uv.xMin, _uv.yMax),
            };
            // 顶点色：默认白（= 不染色）。`Sprites/Default` 会 `tex × 顶点色`，所以四角给了不同颜色就是渐变。
            m.colors = _cornerColors ?? WhiteVerts;
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _mf.sharedMesh = m;
        }

        static readonly Color[] WhiteVerts = { Color.white, Color.white, Color.white, Color.white };
        Color[] _cornerColors;

        /// <summary>
        /// **四角顶点色**（顺序 = 左下 · 右下 · 右上 · 左上，与网格顶点同序）。
        /// 原版好几块「底板」的渐变就是靠这个，**不是靠 `Image.m_Color`**：
        /// `Navigation Panel/Background`（8×8 的 `White Square`，`m_Color` 是**白的**）
        /// 之所以是一块**深酒红板**，是因为**同一个 GameObject 上还挂着一个四角顶点色组件**
        /// （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1941.json`，
        /// TL `(0.066,0.027,0.003)` · TR `(0.189,0.005,0)` · BR `(0.226,0.001,0.001)` · BL `(0.019,0.007,0.007)`）。
        /// ⇒ 只补 `m_Color` **补不出那块板**（2026-09-22 实测：渲染出来是一整块白）。
        /// </summary>
        public void SetCornerColors(Color bl, Color br, Color tr, Color tl)
        {
            _cornerColors = new[] { bl, br, tr, tl };
            RebuildMesh();
        }

        /// <summary>当前的四角顶点色（顺序 = 左下 · 右下 · 右上 · 左上）；**没设过时是 `null`**（= 四角全白）。
        /// 🆕 2026-10-04 加：给**软边遮罩**用 —— 它要在「已有的顶点色」上再乘一道 alpha 斜坡，
        /// 而 `Tint` 是**材质色**、量不到顶点色（`SetCornerColors` 一直只写不读）。
        /// ⚠️ 返回的是内部数组本身（**别改它**）—— 自检也只读它。</summary>
        public Color[] CornerColors { get { return _cornerColors; } }

        Rect _uv = new Rect(0f, 0f, 1f, 1f);

        /// <summary>只用贴图的一块（**归一化** uv 矩形）。九宫格/平铺的子块用它。
        /// ⚠️ 贴图的导入设置若是 Clamp，uv 超出 [0,1] 会拉边而不是重复 ——
        ///    所以**平铺走「多块 quad」**（`CreateTiled`），不靠 uv 越界。</summary>
        public void SetUvRect(Rect r) { _uv = r; RebuildMesh(); }

        /// <summary>当前那张 uv 矩形（**只读**）。给自检用 —— 镜像就是「宽为负」，
        /// 断言要量**真值**、不能只量「调过 SetUvRect」（判据 → `SearchingOpponentWindow.ShowOpponent`）。</summary>
        public Rect UvRect { get { return _uv; } }

        // ==================================================================
        //  两种原版 `Image.Type` 的替身（`Sliced` / `Tiled`）
        //  —— 为什么要有：原版弹窗底板是 `40k_popup`（**九宫格**：`m_Border=(169,160,169,160)`、
        //     `m_Rect=359×336`，中间只剩 21×16），我们过去**因为「没有九宫格」把它改成了自建实底**
        //     （`WaitBanner` / `SettingsPanel` 两处都这么写的）。这里补上，就能照原版画。
        // ==================================================================

        /// <summary>原版 `Image.Type = Sliced`：按 sprite 的 border 切九块。
        /// `borderPx` = (左, 下, 右, 上)（Unity `m_Border` 的 x/y/z/w，**贴图 px**）；
        /// `texW/texH` = 整张图的 px 尺寸；`worldW/H` = 目标世界尺寸。
        /// 角块保持原 px 尺寸，边与心拉伸。
        /// 🔴 **「目标太小怎么办」的判据 = 原版 uGUI `Image.GetAdjustedBorders`**（**逐轴**：只在
        /// `border.x + border.z > rect.width` 时才把那两边的角块按 `rect.width ÷ (bL+bR)` 缩）——
        /// 2026-10-04 之前这里两轴共用一个比例、且会为「端帽铺满整张图」的**合法**形状误报（见下面的注释）。</summary>
        /// <param name="borderOutPx">**绘制时的角块像素长**（不传 = 与 `borderPx` 相同）。
        /// 🔴 为什么要分成两个量：原版的 `Image` 有 **`m_PixelsPerUnitMultiplier`**（`40k_square_border` 是 **5**）
        /// —— UV 切分按**图里的真实边宽**（`m_Border` = 13px / 64² 图），而**画出来的角块 = 13 × 5 = 65px**。
        /// 只传一个量的话，13 会让角块只有 13px（太细）、65 又会让 UV 越界（`65/64 > 1` ⇒ 报警退化成单块）。</param>
        /// <param name="fillCenter">**画不画中间那块**。原版的 `Image.m_FillCenter = 0`（`40k_square_border` 就是这样）
        /// ⇒ 中间**留空**，只画那 8 块边角。</param>
        public static GameObject CreateNineSlice(Transform parent, Texture tex, Vector4 borderPx,
                                                 float texW, float texH, Vector3 center,
                                                 float worldW, float worldH, string name,
                                                 Vector4? borderOutPx = null, bool fillCenter = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;
            if (tex == null) { Debug.LogWarning($"[ImageQuad] {name}: 贴图为空，九宫格没建"); return root; }

            float l = borderPx.x, b = borderPx.y, r = borderPx.z, t = borderPx.w;
            // 归一化 uv 分界
            float uL = l / texW, uR = 1f - r / texW, vB = b / texH, vT = 1f - t / texH;
            // 🔴 **2026-10-04 修（真 bug：误报 + 中段被吃掉）**。
            //   旧写法 `if (uR <= uL || vT <= vB)` 把「**端帽正好铺满整张图**」这种**合法**形状也当成了越界：
            //   判据 = 原版 uGUI `Image.GetAdjustedBorders`（`Runtime/UGUI/UI/Core/Image.cs:1479-1506`（关键那一条在 `:1501`：`adjustedRect.size[axis] < combinedBorders`））——
            //   它只在 **`border.x + border.z > rect.width`**（两边边宽之和 **>** 矩形宽）时才按比例缩，
            //   **`uR == uL`（= 边宽之和 == 贴图宽）是合法形状**，中段宽就是 0（`GenerateSlicedSprite`
            //   对「宽 ≤ 0 的那一格」是 `continue` 跳过，`Image.cs:1194-1195`）。
            //   实测受害例：`WF_Special offer_Value` 324×87 · `m_Border = (162,0,162,0)`（L+R **正好** = 324）
            //   ⇒ `uL == uR == 0.5` ⇒ 旧代码每次都打一条「border 比图还大，退回单块」的**假警告**
            //   （`BoosterPackOpenWindow` 每个卡位一枚 `New Card Badge`、`OfferContainer` 19 个变体各一枚 —— 任务书记的实测是 **21 次**），而它根本没有退回单块。
            //   ⇒ 只有**真正的越界**（`uR < uL` / `vT < vB`）才出声。
            //   另：真的越界时把分界**夹成不反向**（下面两行）——旧代码会拿着一个反过来的 uv 去切图
            //   （画出来是垃圾且**静默**）；夹完之后退化成「中段取同一列纹素」，与原版同形。
            if (uR < uL || vT < vB)
                Debug.LogWarning($"[ImageQuad] {name}: border 比图还大（{l}+{r} > {texW} 或 {b}+{t} > {texH}）—— "
                               + "`m_Border` 与贴图尺寸不自洽，中段退回同一列纹素");
            uR = Mathf.Max(uR, uL); vT = Mathf.Max(vT, vB);

            // 目标里三段的长（角块**不缩放**，按 108 px = 1 世界单位）
            float ol = borderOutPx.HasValue ? borderOutPx.Value.x : l;
            float ob = borderOutPx.HasValue ? borderOutPx.Value.y : b;
            float orr = borderOutPx.HasValue ? borderOutPx.Value.z : r;
            float ot = borderOutPx.HasValue ? borderOutPx.Value.w : t;
            float wl = ol / PixelsPerUnit, wr = orr / PixelsPerUnit;
            float hb = ob / PixelsPerUnit, ht = ot / PixelsPerUnit;
            // ⚠️ **目标比「两边角加起来」还小时，按比例把角缩下来** —— 原版 uGUI 也是这么退化的
            //    （`GetAdjustedBorders`：把角块压扁），不这么做的话角块会**互相重叠**画出去。
            // 🔴 **2026-10-04：按【轴】缩，不是两轴共用一个比例**（判据同上 `GetAdjustedBorders` ——
            //    它是 `for (axis = 0; axis <= 1; axis++)` **逐轴**判 `rect.size[axis] < border[axis]+border[axis+2]`）。
            //    旧写法 `sc = min(1, W/(wl+wr), H/(hb+ht))` 两轴共用：**一个轴挤了，另一个轴的角块也跟着缩**。
            //    实测受害例：提示条用 `40k_popup`（边 169/160）塞进 1323×90 —— 只有**竖**着挤，
            //    原版横边的两个端帽仍是 **169px**（中段 985），旧写法把它们缩成 **47.5px**（中段 1228）。
            //    ⚠️ 受影响的是「某一轴被挤」的那些件（横竖两轴都够的、或被挤的那个轴本来就该缩的，值不变）；
            //    块数不变（被挤那一轴的中段该归零还是归零）。已记在交接报告里。
            float scX = (wl + wr) > worldW ? worldW / Mathf.Max(1e-4f, wl + wr) : 1f;
            float scY = (hb + ht) > worldH ? worldH / Mathf.Max(1e-4f, hb + ht) : 1f;
            wl *= scX; wr *= scX; hb *= scY; ht *= scY;
            float wm = worldW - wl - wr, hm = worldH - hb - ht;
            if (wm < 0f) { wm = 0f; }
            if (hm < 0f) { hm = 0f; }

            float x0 = -worldW * 0.5f, y0 = -worldH * 0.5f;
            float[] xs = { x0, x0 + wl, x0 + wl + wm, x0 + worldW };
            float[] ys = { y0, y0 + hb, y0 + hb + hm, y0 + worldH };
            float[] us = { 0f, uL, uR, 1f };
            float[] vs = { 0f, vB, vT, 1f };

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if (!fillCenter && i == 1 && j == 1) continue;   // 原版 `m_FillCenter = 0` ⇒ 中间那块不画
                    float w = xs[i + 1] - xs[i], h = ys[j + 1] - ys[j];
                    if (w <= 0f || h <= 0f) continue;
                    var q = Create(root.transform, tex,
                                   new Vector3((xs[i] + xs[i + 1]) * 0.5f, (ys[j] + ys[j + 1]) * 0.5f, 0f),
                                   h, new Vector2(0.5f, 0.5f), $"{name}_{i}{j}");
                    if (q == null) continue;
                    q.SetAspect(w / h);
                    q.SetUvRect(new Rect(us[i], vs[j], us[i + 1] - us[i], vs[j + 1] - vs[j]));
                }
            return root;
        }

        /// <summary>原版 `Image.Type = Tiled`：按**贴图原始尺寸**重复铺（不是拉伸）。
        /// 用「多块 quad」实现（不依赖贴图的 wrapMode）。</summary>
        public static GameObject CreateTiled(Transform parent, Texture tex, float tilePxW, float tilePxH,
                                             Vector3 center, float worldW, float worldH, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;
            if (tex == null) { Debug.LogWarning($"[ImageQuad] {name}: 贴图为空，平铺没建"); return root; }

            float tw = tilePxW / PixelsPerUnit, th = tilePxH / PixelsPerUnit;
            int nx = Mathf.Max(1, Mathf.CeilToInt(worldW / tw));
            int ny = Mathf.Max(1, Mathf.CeilToInt(worldH / th));
            float x0 = -worldW * 0.5f, y0 = -worldH * 0.5f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                {
                    float px = x0 + i * tw, py = y0 + j * th;
                    float w = Mathf.Min(tw, x0 + worldW - px), h = Mathf.Min(th, y0 + worldH - py);
                    if (w <= 0f || h <= 0f) continue;
                    var q = Create(root.transform, tex, new Vector3(px + w * 0.5f, py + h * 0.5f, 0f),
                                   h, new Vector2(0.5f, 0.5f), $"{name}_{i}{j}");
                    if (q == null) continue;
                    q.SetAspect(w / h);
                    // 最后一块只画一部分 ⇒ uv 也跟着截（否则会被压扁）
                    q.SetUvRect(new Rect(0f, 0f, w / tw, h / th));
                }
            return root;
        }

        /// <summary>世界坐标是不是点在这张图上（按钮命中测试用）</summary>
        public bool Contains(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return l.x >= -anchor.x * WorldW && l.x <= (1 - anchor.x) * WorldW
                && l.y >= -anchor.y * WorldH && l.y <= (1 - anchor.y) * WorldH;
        }
    }
}
