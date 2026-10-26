// UIViewportUvWriter.cs — 原版 `UIBorderGlow.ModifyMesh` 的 uv 那一半（`uv0.zw = 视口坐标`）
//
// ════════════════════════════════════════════════════════════════════════════════════
//  为什么需要它（判据链，2026-10-26 B8_AS 逐句现读）
//
//  ① 原版 shader `Everguild/UI/UI Border Highlight Appear` 的像素着色器**直接读 `uv0.zw`**
//     （顶点里没有第二个 uv 通道的语义可用；`OriginalShaderBlendTable.cs:388` 现读它的属性表里也没有
//      「屏幕坐标」这类 uniform ⇒ 只能走 uv0.zw）。
//  ② 往里写值的是 `UIBorderGlow`（`UIBehaviour, IMeshModifier`）的 `ModifyMesh(VertexHelper)`：
//     🥇 `d:/2/tools/decomp_full/UIBorderGlow__ModifyMesh.c:59-110`：
//         `for (i = 0; i < vh.currentVertCount; i++) { vh.PopulateUIVertex(ref v, i); … vh.SetUIVertex(v, i); }`
//         循环体里只改 uv0 的 **z/w 两个分量**（反编译里是 `CONCAT44` 两句：`uStack_64 = viewPos.x`
//         → uv0.z、`uStack_60 = viewPos.y` → uv0.w；uv0.xy 原样带过去 —— 按 `UIVertex` 108 字节布局逐字节数过）。
//     而且**每个顶点写的是同一个值**（循环外算一次）。
//  ③ 那个值来自 `UIBorderGlow.CalculateViewPosition()`：
//     🥇 `d:/2/tools/decomp_full/UIBorderGlow__CalculateViewPosition.c:15-48`：
//         `if (camera == null) camera = Camera.main;`（缓存进 `+0x48`）
//         → `result = camera.WorldToViewportPoint(transform.position)`（**取 xy**；没相机时返回 **(0.5, 0.5)**，
//           见那两句 `0x3f000000`）。
//  ④ 刷新时机（原版）：`OnEnable` · `OnRectTransformDimensionsChange` · `OnTransformParentChanged` ·
//     `Update`（四个入口都调 `ModifyMesh` 那一套 —— 见同目录 `UIBorderGlow__{OnEnable,Update,
//     OnRectTransformDimensionsChange,OnTransformParentChanged}.c`）。
//
//  本组件 = 把 ②③④ 搬到**我们自己这套渲染模型**上：
//    原版靠 uGUI 的 `IMeshModifier`（Canvas 重建时 Unity 回调 `ModifyMesh`）；
//    我们**没有 uGUI Canvas**，图块是 `CardPresentation.ImageQuad`（`MeshRenderer` + 运行时 `new Mesh()`），
//    而 `ImageQuad.RebuildMesh()` 只写 **2 分量** 的 `mesh.uv` ⇒ **uv0.zw 永远是 0**。
//    ⇒ 本组件在 `LateUpdate` 里把 uv0.zw 补上（`Mesh.SetUVs(0, Vector4[])`，等价原版 `VertexHelper.SetUIVertex`
//      改 uv0.zw），并在 `ImageQuad` 重建网格（**它每次都 `new Mesh()`**，见 `ImageQuad.cs:RebuildMesh`）
//      之后**自动重写一遍** —— 这一条是原版不需要、我们必须有的（否则布局一变 uv0.zw 就悄悄回到 0）。
//
//  ⚠️ 为什么不是「改 `ImageQuad` 让它直接写 Vector4 uv」（那是更干净的做法，原话见报告）：
//     `ImageQuad` 在 `CardPresentation/Battle/**`，不在本代理白名单。这里做成**旁挂组件**，
//     好处是零侵入、`ImageQuad` 任何既有调用点都不受影响；代价是每块图多一个 `LateUpdate`（4 个顶点）。
//     等 `ImageQuad` 有了原生 `SetUVs(0, Vector4)` 口，本组件可以整体删掉（⛔ 别两处都写）。
//
//  ── 🔶 自己设计（非原版）──────────────────────────────────────────────────────────
//   ① **刷新时机**：原版是「uGUI 重建网格时回调」+ 那四个入口；我们是**每帧 `LateUpdate` 比对**
//      （相机/位置/网格实例三者任一变了才写）。为什么必须自己定：没有 uGUI 的重建回调可用。
//      谁定的 + 日期：调度台 2026-10-26。
//   ② **没相机时出声**：原版静默返回 `(0.5, 0.5)`；我们**照它的值**（`FallbackViewPosition`），
//      但**打一次 `Debug.LogWarning`**（本工程红线：不静默失败）。谁定的 + 日期：调度台 2026-10-26。
//  ────────────────────────────────────────────────────────────────────────────────
//
//  ⚠️ 它自己**不产生任何画面**：材质得先换成带 `_Reveal` 的那只原版 shader
//     （`WarpforgeShaderMap.TryResolve("Everguild/UI/UI Border Highlight Appear", …)`，`Shell/ForgeTab.cs`
//     已有先例），否则 uv0.zw 没有消费者（本组件对「材质用不用」不作判断 —— 它只负责把值写进网格）。
//     接线方在 `Shell/UIBorderGlow` 那一侧，不在本代理白名单。

using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>把「本件在屏上的视口坐标」写进网格 `uv0.zw` —— 原版 `UIBorderGlow.ModifyMesh` 的那一半。</summary>
    [DisallowMultipleComponent]
    public class UIViewportUvWriter : MonoBehaviour
    {
        /// <summary>投影用的相机。**留空 ⇒ `Camera.main`**（与原版 `CalculateViewPosition` 一致，取到就缓存）。</summary>
        public Camera cam;

        /// <summary>取哪个点的视口坐标。**留空 ⇒ 本组件自己的 `transform`**（原版就是 `transform.position`）。</summary>
        public Transform anchor;

        /// <summary>原版 `CalculateViewPosition` 里「没有相机」时返回的那个常量 —— `(0.5, 0.5)`
        /// （`.c:44-45` 两句 `0x3f000000`）。</summary>
        public static readonly Vector2 FallbackViewPosition = new Vector2(0.5f, 0.5f);

        /// <summary>当前写进 `uv0.zw` 的值（只读 —— 给探针/自检看）。</summary>
        public Vector2 ViewPosition { get; private set; }

        /// <summary>共写过几次（>0 = 真的写进去了；给探针用）。</summary>
        public int Writes { get; private set; }

        /// <summary>已经写过的那份网格（`ImageQuad` 每次重建都 `new Mesh()` ⇒ 实例变了要重写）。</summary>
        Mesh _mesh;
        MeshFilter _mf;
        Vector2 _lastView = new Vector2(float.NaN, float.NaN);
        int _lastVertCount = -1;
        bool _warnedNoCamera;
        bool _warnedNoRenderer;
        readonly List<Vector4> _uv = new List<Vector4>(8);

        /// <summary>原版 `UIBorderGlow.CalculateViewPosition()`（静态形态，方便探针直接调）。</summary>
        public static Vector2 CalculateViewPosition(Camera camera, Vector3 worldPosition)
        {
            if (camera == null) return FallbackViewPosition;
            Vector3 v = camera.WorldToViewportPoint(worldPosition);
            return new Vector2(v.x, v.y);
        }

        void LateUpdate()
        {
            ApplyNow();
        }

        /// <summary>写一次。返回是否真的写了（探针在批处理里直接调它 —— `LateUpdate` 不跑也能验）。</summary>
        public bool ApplyNow()
        {
            var mf = _mf != null ? _mf : (_mf = GetComponent<MeshFilter>());
            if (mf == null || mf.sharedMesh == null)
            {
                if (!_warnedNoRenderer)
                {
                    _warnedNoRenderer = true;
                    Debug.LogWarning($"[UIViewportUvWriter] 《{name}》上没有 `MeshFilter.sharedMesh` ⇒ 没东西可写。" +
                                     "它要挂在有 `ImageQuad`（MeshRenderer + MeshFilter）的那个 GameObject 上。");
                }
                return false;
            }

            var mesh = mf.sharedMesh;
            int vc = mesh.vertexCount;
            if (vc <= 0) return false;

            var camera = ResolveCamera();
            Vector2 view = CalculateViewPosition(camera, (anchor != null ? anchor : transform).position);

            if (mesh == _mesh && vc == _lastVertCount && view == _lastView) return false;   // 没变化

            // 保留 uv0.xy，只换 zw（原版 `ModifyMesh` 就只改这两个分量）。
            // ⚠️ 读用 `mesh.uv`（Vector2）而不是 `GetUVs(0, List<Vector4>)` —— `ImageQuad.RebuildMesh` 写的是
            //    **2 分量**通道，走 Vector2 读法拿到的 xy 是确定的（`GetUVs` 对 2 分量通道填 zw=0 也行，
            //    但这条路径不依赖那个行为）。写回必须是 4 分量 ⇒ `SetUVs(0, Vector4[])`。
            var src = mesh.uv;
            if (src == null || src.Length != vc) return false;
            _uv.Clear();
            for (int i = 0; i < vc; i++) _uv.Add(new Vector4(src[i].x, src[i].y, view.x, view.y));
            mesh.SetUVs(0, _uv);

            _mesh = mesh;
            _lastVertCount = vc;
            _lastView = view;
            ViewPosition = view;
            Writes++;
            return true;
        }

        Camera ResolveCamera()
        {
            if (cam == null) cam = Camera.main;      // 原版同上（取到就缓存）
            if (cam == null && !_warnedNoCamera)
            {
                _warnedNoCamera = true;
                Debug.LogWarning($"[UIViewportUvWriter] 《{name}》取不到相机（`cam` 为空且 `Camera.main` 为空）⇒ " +
                                 $"`uv0.zw` 恒为 {FallbackViewPosition}（原版同值，但原版是静默的 —— 我们出声）。");
            }
            return cam;
        }
    }
}
