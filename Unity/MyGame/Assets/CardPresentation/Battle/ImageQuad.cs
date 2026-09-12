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
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _mf.sharedMesh = m;
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
