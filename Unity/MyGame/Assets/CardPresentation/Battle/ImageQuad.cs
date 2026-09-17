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
        /// <summary>1 像素 = 1/100 世界单位（和 Label 同口径）</summary>
        public const float PixelsPerUnit = 100f;

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
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _mf.sharedMesh = m;
        }

        Rect _uv = new Rect(0f, 0f, 1f, 1f);

        /// <summary>只用贴图的一块（**归一化** uv 矩形）。九宫格/平铺的子块用它。
        /// ⚠️ 贴图的导入设置若是 Clamp，uv 超出 [0,1] 会拉边而不是重复 ——
        ///    所以**平铺走「多块 quad」**（`CreateTiled`），不靠 uv 越界。</summary>
        public void SetUvRect(Rect r) { _uv = r; RebuildMesh(); }

        // ==================================================================
        //  两种原版 `Image.Type` 的替身（`Sliced` / `Tiled`）
        //  —— 为什么要有：原版弹窗底板是 `40k_popup`（**九宫格**：`m_Border=(169,160,169,160)`、
        //     `m_Rect=359×336`，中间只剩 21×16），我们过去**因为「没有九宫格」把它改成了自建实底**
        //     （`WaitBanner` / `SettingsPanel` 两处都这么写的）。这里补上，就能照原版画。
        // ==================================================================

        /// <summary>原版 `Image.Type = Sliced`：按 sprite 的 border 切九块。
        /// `borderPx` = (左, 下, 右, 上)（Unity `m_Border` 的 x/y/z/w，**贴图 px**）；
        /// `texW/texH` = 整张图的 px 尺寸；`worldW/H` = 目标世界尺寸。
        /// 角块保持原 px 尺寸，边与心拉伸。</summary>
        public static GameObject CreateNineSlice(Transform parent, Texture tex, Vector4 borderPx,
                                                 float texW, float texH, Vector3 center,
                                                 float worldW, float worldH, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;
            if (tex == null) { Debug.LogWarning($"[ImageQuad] {name}: 贴图为空，九宫格没建"); return root; }

            float l = borderPx.x, b = borderPx.y, r = borderPx.z, t = borderPx.w;
            // 归一化 uv 分界
            float uL = l / texW, uR = 1f - r / texW, vB = b / texH, vT = 1f - t / texH;
            if (uR <= uL || vT <= vB) { Debug.LogWarning($"[ImageQuad] {name}: border 比图还大，退回单块"); }

            // 目标里三段的长（角块**不缩放**，按 108 px = 1 世界单位）
            float wl = l / PixelsPerUnit, wr = r / PixelsPerUnit;
            float hb = b / PixelsPerUnit, ht = t / PixelsPerUnit;
            // ⚠️ **目标比「两边角加起来」还小时，按比例把角缩下来** —— 原版 `40k_popup` 的角是 169/160 px，
            //    而提示条只有 1323×90 ⇒ 竖着 160+160 > 90。Unity 的 Sliced 也是这么退化的（把角压扁），
            //    不这么做的话角块会**互相重叠**画出去。
            float sc = Mathf.Min(1f, worldW / Mathf.Max(1e-4f, wl + wr), worldH / Mathf.Max(1e-4f, hb + ht));
            wl *= sc; wr *= sc; hb *= sc; ht *= sc;
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
