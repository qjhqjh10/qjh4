// BattleBackdrop.cs — 战场背景（原版 battlearena1 的实拍图）
//
// 整块 quad 贴在**所有东西后面**（相机在 -Z 看 +Z，所以 z 越大越远）。
// 图是 `ArtBaker` 从重建好的 `WarpforgeArena1/Scenes/BattleArena1.unity` 里渲出来的
// 1920×1080 —— **和原版同一个机位**，所以原版量出来的槽位坐标（y=708/466 px）
// 贴在这张图上位置正好对得上。
//
// 缩放策略是「铺满」（cover）：宽高比和图不一致时按大的那边铺，多出来的裁掉 ——
// 背景图不该被拉变形。换分辨率（4:3 / 超宽）时 `Refresh()` 会重算。
using UnityEngine;

namespace CardPresentation
{
    public class BattleBackdrop : MonoBehaviour
    {
        [Tooltip("放在多远（相机看 +Z，所以 z 越大越靠后）")]
        public float z = 6f;

        [Tooltip("压暗一点，让卡牌和 HUD 更跳出来")]
        public Color tint = new Color(0.82f, 0.82f, 0.86f, 1f);

        Texture2D _tex;
        MeshRenderer _mr;
        MeshFilter _mf;
        float _builtW = -1f, _builtH = -1f;

        public bool Ready { get { return _tex != null; } }
        public Texture2D Image { get { return _tex; } }

        /// <summary>
        /// 建出来（可重复调用）。
        /// ⚠️ **场景存过之后要能重新绑上**：`_mr`/`_tex` 这些私有字段**不进序列化**，
        ///    直接存场景的话运行时它们是 null，背景就不刷新尺寸了。所以这里先找一遍子节点。
        /// </summary>
        public void Build()
        {
            if (_mr == null)
            {
                var existing = transform.Find("Backdrop");
                if (existing != null)
                {
                    _mf = existing.GetComponent<MeshFilter>();
                    _mr = existing.GetComponent<MeshRenderer>();
                    if (_mr != null && _mr.sharedMaterial != null)
                        _tex = _mr.sharedMaterial.mainTexture as Texture2D;
                }
            }
            if (_mr != null) { _builtW = _builtH = -1f; Refresh(); return; }

            CardArt.Load();
            _tex = CardArt.Backdrop;
            if (_tex == null) return;

            var go = new GameObject("Backdrop");
            go.transform.SetParent(transform, false);
            _mf = go.AddComponent<MeshFilter>();
            _mr = go.AddComponent<MeshRenderer>();
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            _mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = tint };
            _mr.sharedMaterial.mainTexture = _tex;
            _mf.sharedMesh = Quad();
            _builtW = _builtH = -1f;
            Refresh();
        }

        /// <summary>按当前可见区域重算大小（切分辨率要调）</summary>
        public void Refresh()
        {
            if (_mr == null || _tex == null) return;
            float visW = LayoutSpace.VisibleWidth, visH = LayoutSpace.VisibleHeight;
            float imgAspect = _tex.width / (float)_tex.height;
            float w, h;
            if (visW / visH > imgAspect) { w = visW; h = visW / imgAspect; }   // 屏更宽 → 按宽铺，上下裁
            else                         { h = visH; w = visH * imgAspect; }   // 屏更高 → 按高铺，左右裁
            if (Mathf.Approximately(w, _builtW) && Mathf.Approximately(h, _builtH)) return;
            _builtW = w; _builtH = h;

            var t = _mr.transform;
            t.localPosition = new Vector3(0f, 0f, z);
            t.localScale = new Vector3(w, h, 1f);
        }

        static Mesh _quad;
        static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var m = new Mesh { name = "BackdropQuad" };
            m.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),   new Vector3(-0.5f, 0.5f, 0f),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _quad = m;
            return m;
        }

        /// <summary>背景图在屏幕上的位置（自检用：验证「铺满」算得对）</summary>
        public Vector4 ScreenRect()
        {
            if (_mr == null) return Vector4.zero;
            return new Vector4(_builtW, _builtH, LayoutSpace.VisibleWidth, LayoutSpace.VisibleHeight);
        }
    }
}
