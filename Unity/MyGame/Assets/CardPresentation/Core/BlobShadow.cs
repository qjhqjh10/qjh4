// BlobShadow.cs — 场上卡底下那枚**软阴影**（原版 `BlobShadowController`；2026-09-25 补）
//
// 「原版有、我们完全没有」那一件的实现。**逐值实读**，出处写在每一段旁边。
//
// ---- 节点（谁是谁的孩子）----
// 路径 = `CardPrefab / Board Elements / 3DBody / **Card 3D** / <软阴影>`
//   —— 它是 `Card 3D` 的**子节点**，而 `target` 字段填的**也是** `Card 3D`。
//   出处：`bundle_battleprefabs_vfxandmisc_assets_all/Transform/Transform_6339896688388119488.json`
//         + `MonoBehaviour/MonoBehaviour_2235663227690458048.json`（`BlobShadowController`）
//   · localPosition **(0, 0, 0)** · localScale **2.0567584**（等比）
//   · localRotation q = **(0, 0.70710784, −0.70710576, ≈0)**
//     = 绕 **(0, 1, −1)/√2 转 180°** ⇒ 精灵的「右」→ +X、「上」→ **+Z**、法线 → **−Y**：
//     它是一张**平铺在地面、法线朝下**的片子（`Card 3D` 自己还带一个 180°@Y，两层合起来才是最终朝向）。
//     ⇒ 原版那个 shader 必然是**双面**的（从上看是背面）。
//
// ---- 精灵与材质 ----
// `SpriteRenderer_-8386903980694135872.json`：
//   · sprite **`Card blob shadow`**（`Sprite/Card blob shadow.json`：矩形 **128×128** ·
//     `m_PixelsToUnits = 100` · pivot (0.5,0.5)；内容 `textureRect` = 124.848² @ (2.076,1.076)）
//   · 材质 **`Everguild_Cards_BlobShadow`** —— shader `Everguild/Cards/BlobShadow`，**属性表只有 `_MainTex`**
//   · `m_Color` = **(0, 0, 0, 0.36078429)**
//   🔴 那张图是**白图 + alpha 掩码**（实测：中心 (255,255,255,255)、四角全透明、最大 alpha 255）——
//      所以「黑」**全靠 `SpriteRenderer.color` 染**出来，shader 必须吃顶点色（Unity 的 sprite 就是这条路）。
//
// ---- 参数（原版序列化值，逐字）----
//   `floorY = 0` · `maxYForAlpha0 = 0.15` · `yOffsetForSnap = 0.01` · `scaleMultiplierByHeight = 1.1`
//
// ---- 行为（`BlobShadowController__Start.c` / `__Update.c` 逐行读出来的）----
//   Start：记下 `sprite.color`；把它的 **alpha 归零**那份当「淡出色」；记下原来的 `localScale`
//          （另有一个 `targetYCenterOffset = target.y − self.y`，本工程里影子与 target 同点 ⇒ 恒 0）
//   Update：`t = clamp01((target.position.y − floorY) / maxYForAlpha0)`
//           · 颜色 = `Lerp(原色, 淡出色, t)`
//           · `localScale = Lerp(原 scale, 原 scale × scaleMultiplierByHeight, t)`
//           · **世界位置** = `(target.x, floorY + yOffsetForSnap, target.z)` —— **贴在地面上**，不随卡抬起
//           · `renderer.enabled = (color.a != 0)`
//
// 🔴 **2026-09-25 更正 `项目任务.md` §三 第 12 条 第 3 项的写法**：那里写「由 `BlobShadowController`
//    按离地高度调 alpha，**抬起变明显、贴地几乎透明**」—— **写反了**。代码里 `t=0`（**贴地**）取的才是
//    **原色 α0.361**，抬到 0.15 才淡成 0。这与常识也一致：卡一离地，影子就该淡。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>卡底那枚软阴影。**原版 `BlobShadowController` 的我们这版**（逐值照抄，见文件头）。</summary>
    public class BlobShadow : MonoBehaviour
    {
        /// <summary>阴影跟着谁走（原版 `target` = `Card 3D`）。</summary>
        public Transform target;

        // ---- 原版序列化参数（名字照抄，便于逐字段对账）----
        public float floorY = 0f;
        public float maxYForAlpha0 = 0.15f;
        public float yOffsetForSnap = 0.01f;
        public float scaleMultiplierByHeight = 1.1f;

        SpriteRenderer _sr;
        Color _originalColor, _fadedColor;
        Vector3 _originalScale;
        float _cardAlpha = 1f;          // 整卡淡出（消散/置灰）—— 原版没有这一维，是我们多出来的一层

        /// <summary>自检用：现在影子该用的颜色（含整卡淡出）</summary>
        public Color CurrentColor { get { return _sr != null ? _sr.color : Color.clear; } }
        /// <summary>自检用：t（0 = 贴地、1 = 抬到 `maxYForAlpha0`）</summary>
        public float HeightT { get; private set; }
        /// <summary>自检用：精灵的世界直径（世界单位）—— 断言比它，别比 `localScale`</summary>
        public float WorldDiameter
        {
            get
            {
                if (_sr == null) return 0f;
                var s = _sr.sprite;
                if (s == null) return 0f;
                var ls = transform.lossyScale;
                return s.bounds.size.x * Mathf.Abs(ls.x);
            }
        }

        /// <summary>建一枚软阴影，挂到 <paramref name="parent"/> 底下、跟着 <paramref name="target"/>。
        /// 取不到图或材质时**如实返回 null**（调用方那一层就不画，不静默画个错的）。</summary>
        public static BlobShadow Create(Transform parent, Transform target, string goName = "BlobShadow")
        {
            var sprite = CardView.BlobShadowSprite();
            var mat = CardView.BlobShadowMaterial();
            if (sprite == null || mat == null)
            {
                Debug.LogWarning($"[BlobShadow] 建不出来（sprite={(sprite != null)} 材质={(mat != null)}）"
                               + " ⇒ 卡底软阴影这一层不画。跑 `工具/import_original_3dcard.py` 补图；"
                               + "shader 走 `WarpforgeShaderMap.TryResolve(\"Everguild/Cards/BlobShadow\")`");
                return null;
            }

            // 🔴 **2026-10-11（A218）判「不改」**（这处**故意**保持**裸 `Transform`**，⛔ 别补 `RectTransform`）：
            //    判据 = **原版这一件本来就是裸 `Transform`** —— 它的来源文件就是
            //    `bundle_battleprefabs_vfxandmisc_assets_all/**Transform**/Transform_6339896688388119488.json`
            //    （父链 `CardPrefab/Board Elements/3DBody/Card 3D/<软阴影>`，见文件头那段字段实读）。
            //    它是张**平铺在地面、法线朝下**的片子（`localRotation` 绕 (0,1,−1)/√2 转 180°、
            //    `localScale 2.0567584`），靠 `localScale`/世界位置摆 —— **没有矩形语义**
            //    ⇒ 补 `RectTransform` + 编一个尺寸 = 造一个**与原版相反**的类型（铁律 3）。
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            // 🔴 **三个 transform 值都是原版序列化值**，见文件头。别「看着像」自己调。
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = new Quaternion(0f, 0.70710784f, -0.70710576f, 0f);
            go.transform.localScale = Vector3.one * 2.0567584f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.color = new Color(0f, 0f, 0f, 0.36078429f);      // 原版 `m_Color`
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            var b = go.AddComponent<BlobShadow>();
            b.target = target != null ? target : parent;
            b._sr = sr;
            b.Capture();
            b.Sync();
            return b;
        }

        /// <summary>原版 `Start`：记原色 / 淡出色 / 原 scale。</summary>
        public void Capture()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _originalColor = _sr.color;
                _fadedColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);
            }
            _originalScale = transform.localScale;
        }

        /// <summary>整卡淡出（消散 / 置灰）跟着走 —— 卡淡没了、影子还实着，一眼就假。</summary>
        public void SetCardAlpha(float a) { _cardAlpha = Mathf.Clamp01(a); Sync(); }

        /// <summary>原版 `Update` 那套（去掉 `hasChanged` 那个纯优化）。
        /// ⚠️ **批处理下没有帧循环** ⇒ 由 `CardView.SetPose` / `SetCardAlpha` 显式调；
        ///    真 Play 里 `Update()` 也会调（原版就是在 `Update` 里做的）。</summary>
        public void Sync()
        {
            if (_sr == null || target == null) return;

            // t = clamp01((target.y − floorY) / maxYForAlpha0) —— 原版逐字
            HeightT = maxYForAlpha0 > 0f
                    ? Mathf.Clamp01((target.position.y - floorY) / maxYForAlpha0)
                    : 0f;

            var c = Color.Lerp(_originalColor, _fadedColor, HeightT);
            c.a *= _cardAlpha;
            _sr.color = c;

            transform.localScale = Vector3.Lerp(_originalScale,
                                                _originalScale * scaleMultiplierByHeight, HeightT);

            // **贴在地上**（原版 `set_position`，注意是**世界**位置）
            var p = target.position;
            transform.position = new Vector3(p.x, floorY + yOffsetForSnap, p.z);

            _sr.enabled = c.a != 0f;
        }

        void Update() { Sync(); }
    }
}
