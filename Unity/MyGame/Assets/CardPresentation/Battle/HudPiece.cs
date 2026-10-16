// HudPiece.cs — 战斗 HUD 里一个「件」的**适配层**（`T09` 整层换 uGUI 的过渡件）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它解决什么】T09 要的把那 ~52 处 `ImageQuad` 件换成 uGUI 件，但那些件在**别处**被用了 ~140 次
//   （`.Texture` 32 · `.gameObject` 32 · `.transform` 25 · `.WorldW/H` 28 · `.SetAspect` 8 · `.SetTint` 7 ·
//     `.Contains` 3 · `.Tint` 2 · `.SetTexture` 2 · `.SetAnchorPosition` 1）。
//   如果直接把这 39 个字段的类型从 `ImageQuad` 改成 `RectTransform`，那 140 处**每处都要改**，
//   而每一处都是**看不见的改动**（本工程那条血规：只改半边在 16:9 下逐位等价、全绿，坏只在别的宽高比上现形）。
//   ⇒ 用这一层**把差异挡住**：字段类型 `ImageQuad` → `HudPiece`，**140 处用法一行不用改**。
//
// 🔴 **一条铁律（本件存在的全部意义）**：一个 `HudPiece` **只能是两种之一** ——
//   **要么**旧的 `ImageQuad`（世界空间 quad），**要么**uGUI 的 `RectTransform + Image`。
//   ⛔ **绝不允许「两边都建」**（那就是同一件画两遍、而且两边会漂不同步）。
//   本件每个写口都先判 `IsUgui` 再走对应那一支，且**没有**任何一处会同时改两边。
//
// ⚠️ **它是【临时】的**：T09 第 7 步（收尾）要把旧那一支整个删掉、本件随之退场。
//   ⛔ 别在它上面长新功能（它不是抽象基类，是**迁移脚手架**）。
//
// 【判据：几个读回口在 uGUI 那一侧怎么算】
//   · `WorldW/WorldH` —— 🔑 **用 `RectTransform.GetWorldCorners()` 直接量世界尺寸**，
//     ⛔ **不是** `sizeDelta × 某个系数`：canvas 从「设计 px」到「世界单位」的换算随分辨率变
//     （1 单位 = `10/屏高` 世界单位），写死系数就是在**重犯 A2092 那个「两套尺」的错**。
//   · `Contains(world)` —— 同样用 `GetWorldCorners()` 得到的**世界四边形**判内点。
//     这样它与旧 quad 那条路**量的是同一件事**（画出来的那个矩形），且非 16:9 下也成立。
//   · `Texture` —— uGUI 那一侧取 `Image.sprite.texture`。
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>战斗 HUD 里的一个件：**旧 `ImageQuad` 世界空间 quad** 或 **uGUI `RectTransform + Image`**，
    /// 二选一。见文件头（含那条「⛔ 不许两边都建」的铁律）。</summary>
    public sealed class HudPiece
    {
        readonly ImageQuad _quad;          // 旧路（非 null ⇒ 走它）
        readonly RectTransform _rt;        // 新路
        readonly Image _img;

        HudPiece(ImageQuad q, RectTransform rt, Image img) { _quad = q; _rt = rt; _img = img; }

        /// <summary>这一件是不是已经迁到 uGUI 了（自检/诊断用；**判据只此一处**）。</summary>
        public bool IsUgui { get { return _quad == null; } }

        /// <summary>旧路那一支（没迁的件才有；没迁时才非 null）。</summary>
        public ImageQuad Quad { get { return _quad; } }
        /// <summary>新路那一支（迁过的件才有）。</summary>
        public RectTransform Rect { get { return _rt; } }

        // ── 建件（**只有 `HudCanvas` 与 `BattleDriver` 那 4 个建件助手调**）────────────
        /// <summary>包一个旧 quad（没迁的件走这条）。</summary>
        public static HudPiece Of(ImageQuad q) { return q == null ? null : new HudPiece(q, null, null); }
        /// <summary>包一个 uGUI 件（迁过的件走这条）。</summary>
        public static HudPiece Of(RectTransform rt, Image img)
        {
            return rt == null ? null : new HudPiece(null, rt, img);
        }

        // ── 那 140 处用法用到的口（逐条两种实现）──────────────────────────────────
        public GameObject gameObject { get { return _quad != null ? _quad.gameObject : _rt.gameObject; } }
        public Transform transform { get { return _quad != null ? _quad.transform : (Transform)_rt; } }

        /// <summary>这个件的**对象名**。⚠️ `HudPiece` **不是 `UnityEngine.Object`** ⇒ 那些
        /// `q.name` / `.gameObject` 的用法得由本件补出来（转发给底下那个真正的对象）。</summary>
        public string name
        {
            get { var g = gameObject; return g != null ? g.name : null; }
            set { var g = gameObject; if (g != null) g.name = value; }
        }

        /// <summary>当前贴图（诊断/自检用）。取不到 ⇒ `null`（⛔ 不返回假贴图）。
        /// ⚠️ 返回类型是 `Texture`（不是 `Texture2D`）—— 跟 `ImageQuad.Texture` 一致即可（用法只有 `.name`）。</summary>
        public Texture Texture
        {
            get
            {
                if (_quad != null) return _quad.Texture;
                var sp = _img != null ? _img.sprite : null;
                return sp != null ? sp.texture : null;
            }
        }

        /// <summary>画出来的**世界宽**。判据见文件头（uGUI 侧用 `GetWorldCorners`）。</summary>
        public float WorldW
        {
            get
            {
                if (_quad != null) return _quad.WorldW;
                var c = Corners(); return Mathf.Abs(c[3].x - c[0].x);
            }
        }

        /// <summary>画出来的**世界高**。判据同 <see cref="WorldW"/>。</summary>
        public float WorldH
        {
            get
            {
                if (_quad != null) return _quad.WorldH;
                var c = Corners(); return Mathf.Abs(c[1].y - c[0].y);
            }
        }

        /// <summary>当前染色（自检用）。</summary>
        public Color Tint { get { return _quad != null ? _quad.Tint : (_img != null ? _img.color : Color.white); } }

        /// <summary>换贴图。⚠️ uGUI 那一侧要**重建 `Sprite`**（`Image` 要的是 `Sprite`、不是 `Texture2D`）。</summary>
        public void SetTexture(Texture2D tex)
        {
            if (_quad != null) { _quad.SetTexture(tex); return; }
            if (_img == null || tex == null) return;
            _img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                        new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>染色。</summary>
        public void SetTint(Color c)
        {
            if (_quad != null) { _quad.SetTint(c); return; }
            if (_img != null) _img.color = c;
        }

        /// <summary>**盖掉贴图比例**。语义 = 「宽 = 高 × aspect」（与 `ImageQuad.SetAspect` 逐字一致）——
        /// ⚠️ uGUI 那一侧**只改 `sizeDelta.x`、高度不动**（`Image.preserveAspect` 是另一回事，见
        /// `HudCanvas.MakeImage` 里那条「原版 `PA=0` ⇒ 拉满」）。</summary>
        public void SetAspect(float aspect)
        {
            if (_quad != null) { _quad.SetAspect(aspect); return; }
            if (_rt == null || aspect <= 0f) return;
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.y * aspect, _rt.sizeDelta.y);
        }

        /// <summary>旧那套的「往后挪一点」（`localPosition.z += dz`）—— 修 z 的人一律走它。
        /// ⚠️ uGUI 下 z 没有意义，它改的是**兄弟序**（见 `HudCanvas.Reorder`）。</summary>
        public void NudgeZ(float dz)
        {
            if (_quad != null) { _quad.transform.localPosition += new Vector3(0f, 0f, dz); return; }
            if (_rt != null) HudCanvas.Reorder(_rt, HudCanvas.OrderZOf(_rt) + dz);
        }

        /// <summary>这个件占的 **z 档**（诊断/自检读回）。uGUI ⇒ 它在画布里的**排位 z**（⛔ 不是 Unity 的 z）。
        /// 认不出 ⇒ `NaN`。</summary>
        public float OrderZ
        {
            get { return _quad != null ? _quad.transform.localPosition.z : HudCanvas.OrderZOf(_rt); }
        }

        /// <summary>平移这个件（**世界单位**的增量）。两种模式各用各的口：
        /// quad ⇒ `transform.localPosition`；uGUI ⇒ `anchoredPosition`（+y 向上，与世界 +y 同向）。
        /// ⚠️ 别在调用点自己写 `transform.localPosition` —— uGUI 那一侧的单位与原点都不是那一套。</summary>
        public void NudgeWorld(float dxWorld, float dyWorld)
        {
            if (_quad != null)
            {
                _quad.transform.localPosition += new Vector3(dxWorld, dyWorld, 0f);
                return;
            }
            if (_rt != null) _rt.anchoredPosition += new Vector2(dxWorld * 108f, dyWorld * 108f);
        }

        /// <summary>这个件贴着的**归一化锚点**（x 从左、y 从**下**算）—— 自检读回用。
        /// ⚠️ 与 <see cref="SetAnchorPosition"/> 成对。⛔ **别用 `transform.localPosition` 代替**：
        /// uGUI 那一侧的单位是「设计 px」、且相对的是 canvas，与归一化锚点不是一回事。</summary>
        public Vector2 Anchor01()
        {
            if (_quad != null) return LayoutSpace.ToNormalized(_quad.transform.localPosition);
            return _rt != null ? _rt.anchorMin : new Vector2(-1f, -1f);
        }

        /// <summary>设**画心高**（世界单位），**保持当前宽高比**（⛔ 不动比例 —— 调用点接着会自己调
        /// `SetAspect` 把宽定下来，见 `FitDeckPile` 那三步）。判据同 <see cref="SetAspect"/>：
        /// 设计 px = 世界单位 × 108。</summary>
        public void SetWorldHeight(float worldH)
        {
            if (_quad != null) { _quad.SetWorldHeight(worldH); return; }
            if (_rt == null || worldH <= 0f) return;
            float h = worldH * 108f;
            float a = _rt.sizeDelta.y > 0f ? _rt.sizeDelta.x / _rt.sizeDelta.y : 1f;
            _rt.sizeDelta = new Vector2(h * a, h);
        }

        /// <summary>当前**材质**（自检比「牌堆那张 SDF 用的是原版 shader」时读它）。
        /// 两种模式各取各的：quad ⇒ 它自己的 `MeshRenderer.sharedMaterial`；uGUI ⇒ `Image.material`。</summary>
        public Material Material
        {
            get
            {
                if (_quad != null)
                {
                    var mr = _quad.GetComponent<MeshRenderer>();
                    return mr != null ? mr.sharedMaterial : null;
                }
                return _img != null ? _img.material : null;
            }
        }

        /// <summary>换材质（牌堆那张 SDF 用的）。⚠️ uGUI 那一侧 = `Image.material`。</summary>
        public void SetMaterial(Material m)
        {
            if (_quad != null) { _quad.SetMaterial(m); return; }
            if (_img != null) _img.material = m;
        }

        /// <summary>贴到某个**归一化锚点**（x 从左、y 从**下**算 —— 与 `LayoutSpace` 同口径）。</summary>
        public void SetAnchorPosition(float x01, float y01)
        {
            if (_quad != null) { _quad.SetAnchorPosition(x01, y01); return; }
            if (_rt == null) return;
            _rt.anchorMin = _rt.anchorMax = new Vector2(x01, y01);
            _rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>这个件的**世界四边形**是不是含住了 `world` 点（命中判定，**与旧 quad 量的是同一件事**）。</summary>
        public bool Contains(Vector3 world)
        {
            if (_quad != null) return _quad.Contains(world);
            var c = Corners();
            // 用四边形判内点（canvas 的件是轴对齐的矩形；这里按「两个三角」判，斜面也能覆盖）
            return InTri(world, c[0], c[1], c[2]) || InTri(world, c[0], c[2], c[3]);
        }

        /// <summary>`GetWorldCorners` 的四个角（顺序 = Unity 的约定：左下 → 左上 → 右上 → 右下）。</summary>
        Vector3[] Corners()
        {
            var c = new Vector3[4];
            _rt.GetWorldCorners(c);
            return c;
        }

        static bool InTri(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // 只判 xy 平面（HUD 是正对相机的平面）——与 `ImageQuad.Contains` 同口径
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool neg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool pos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(neg && pos);
        }

        static float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
