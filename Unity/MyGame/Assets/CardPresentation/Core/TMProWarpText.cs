// TMProWarpText.cs — 沿 `AnimationCurve` 把 TMP 的字形顶点「弯」一条弧（原版 `TMProWarpText` 的等价实现）
//
// ============================ 判据（全部现读，逐条标出处） ============================
// 类：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TMProWarpText.cs`（**`MonoBehaviour`**，不是 TMP 的子类 ——
//     ⚠️ 与派单简报里「TMP 的 `Text` 子类（`TMP_Text` 派生）」**不一致**；以反编译为准，见报告 §3）。
//     字段只有 5 个：`text`(`TMP_Text` · +0x20) · `axis`(enum Y=0 / Z=1 · +0x28) · `vertexCurve`(+0x30)
//     · `yCurveScaling`(+0x38，出厂 100f) · `isForceUpdatingMesh`(+0x3c)。
// 方法体（`d:/2/tools/decomp_full/`，逐句）：
//   · `TMProWarpText__Awake.c`      —— `text` 空就 `GetComponent<TMP_Text>()` 回填
//   · `TMProWarpText__OnEnable.c`   —— 先 `WarpText()` 一次，再 `TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged)`
//   · `TMProWarpText__OnDisable.c`  —— 先退订，再 `ForceMeshUpdate()`
//   · `TMProWarpText__ReactToTextChanged.c` —— `obj` 不是 `TMP_Text` 就返回；是自己且 `isForceUpdatingMesh` 为假才重弯
//   · `TMProWarpText__Reset.c`      —— 缓存 TMP + 建那条 3 键曲线 + 立刻弯一次
//   · `TMProWarpText__WarpText.c`   —— **364 行，本族主场**，见下面 `WarpText()` 的逐句注
// 曲线默认值（`Reset`）：`(0,0,切线 30) → (0.5,0.25) → (1,0,切线 −30)`，`pre/postWrapMode = WrapMode.Once`。
// 🔴 **出货值不是 `Reset` 的默认值**（本轮现读：34 个实例全在 `bundle_boosterpacks_assets_all` 的
//    `Booster Pack Standard *.prefab` 的 `Army Text` / `Army Text 2` 上）：
//    `axis = Z` · `yCurveScaling = −0.09` · 两种曲线各 17 个
//    ⇒ 与 `Reset()` 默认（`axis = Y` / `100` / 单峰 0.25）**差 1000 倍**，⛔ 别用默认值建实例。
//
// 数值常量（本轮用 `工具/read_literal.py` 从 `GameAssembly.dll` 现读，⛔ 不再猜）：
//   `0x1834b2f48` = **0.0001f**（`x1 = x0 + 0.0001`）· `0x1834b2bb4` = **0.5f**（取两顶点的中点 x）
//   `0x1834b2f6c` = **−0.0f**（平移时给 z 加的那个常量 ⇒ 等于 0）
//   `0x1834b2dc0` = **0.0174533f**（度→弧度，`Internal_FromEulerRad` 之前乘的）· `0x1834b2e98` = **57.2958f**（弧度→度）
//   `0x1834b2bc4` = **360f**（切向 y ≤ 0 时 `angle = 360 − angle`）
//
// ============================ 🔶 我们挑的 ============================
//   ① ⚠️ 2026-11-05 订正（B145-E1 · 铁律 5）：原来这两行写「订阅 `OnPreRenderText`、用 `Update` 里文本变了才重弯兜底（见 `Tick()`）」
//      —— **全是错的**：原版订阅的是 **`TMPro_EventManager.TEXT_CHANGED_EVENT`**（`FastAction<UnityEngine.Object>`），本文件也照它订/退；本文件**没有** `Tick()` / `Update()`。
//   ② 没有 `isForceUpdatingMesh` 的对外暴露（原版是 private）—— 我们留了只读口给自检。
using System;
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `TMProWarpText`（判据见文件头）。
    /// <para>用法：`var w = go.AddComponent&lt;TMProWarpText&gt;(); w.Configure(txt, Axis.Z, curve, -0.09f);`</para>
    /// <para>⚠️ 编辑模式下 `AddComponent` **不会**触发 `Awake/OnEnable/Update`（工程已知坑）⇒
    /// 必须先 `Configure(...)`（它自己会弯一次），⛔ 别指望 `OnEnable` 那一跳。</para></summary>
    public class TMProWarpText : MonoBehaviour
    {
        /// <summary>原版的 2 档（`Y` = 绕 Z 轴转、`Z` = 绕 X 轴转并在 Z 上偏移）。</summary>
        public enum Axis { Y = 0, Z = 1 }

        /// <summary>被弯的那颗 TMP（原版 `text` · `+0x20`）。</summary>
        public TMP_Text text;
        /// <summary>弯的方向（原版 `axis` · `+0x28`）。</summary>
        public Axis axis = Axis.Y;
        /// <summary>那条曲线（原版 `vertexCurve` · `+0x30`）。</summary>
        public AnimationCurve vertexCurve;
        /// <summary>曲线的 y 缩放（原版 `yCurveScaling` · `+0x38`，出厂 **100f**；出货实例是 **−0.09f**）。</summary>
        public float yCurveScaling = 100f;

        bool _warping;

        /// <summary>原版 `isForceUpdatingMesh`（只读口，自检用）。</summary>
        public bool IsForceUpdatingMesh { get { return _warping; } }

        /// <summary>`Reset()` 那条 3 键曲线（判据见文件头；⛔ 它只是默认值，出货实例另有一套）。</summary>
        public static AnimationCurve DefaultCurve()
        {
            var c = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 30f),
                new Keyframe(0.5f, 0.25f),
                new Keyframe(1f, 0f, -30f, 0f));
            c.preWrapMode = WrapMode.Once;
            c.postWrapMode = WrapMode.Once;
            return c;
        }

        /// <summary>显式接线（编辑模式/自检都走它，见类注释）。</summary>
        public void Configure(TMP_Text t, Axis a, AnimationCurve curve, float scaling)
        {
            text = t;
            axis = a;
            vertexCurve = curve;
            yCurveScaling = scaling;
            WarpText();
        }

        void Awake()
        {
            // 原版 `Awake`：`text` 空就 `GetComponent<TMP_Text>()` 回填
            if (text == null) text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            WarpText();                                                    // 原版：先弯一次（`text` 空时由它自己 return）
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged); // ✅ 已复刻（2026-11-05 · B145-E1 · decomp_full/TMProWarpText__OnEnable.c:15-24：先弯 → `TEXT_CHANGED_EVENT.Add`）
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged); // ✅ 已复刻（2026-11-05 · B145-E1 · decomp_full/TMProWarpText__OnDisable.c:19-23：先退订）
            if (text != null) text.ForceMeshUpdate();      // 原版：退订后强制重算（⇒ 网格回直）；🔶 自己设计（非原版）：`text` 空就跳过 —— 原版这里直接调、会 NRE（调度台 2026-11-05 判）
        }

        /// <summary>原版 `ReactToTextChanged(UnityEngine.Object obj)`：不是 `TMP_Text` 就返回；
        /// 是自己、且当前不在弯的过程中，才重弯一次。</summary>
        public void ReactToTextChanged(UnityEngine.Object obj)
        {
            var t = obj as TMP_Text;
            if (t == null) return;
            if (t != text) return;
            if (_warping) return;
            WarpText();
        }

        /// <summary>原版 `Reset()`（`[ContextMenu/Reset]`）：缓存 TMP + 建默认曲线 + 立刻弯一次。</summary>
        public void Reset()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            vertexCurve = DefaultCurve();
            WarpText();
        }

        /// <summary>原版 `WarpText()`（`decomp_full/TMProWarpText__WarpText.c` 全 364 行）。
        /// <para>逐句对应（行号见那份 `.c`）：</para>
        /// <para>① `text` 空 ⇒ return；置 `isForceUpdatingMesh = true`；`havePropertiesChanged = true`；`ForceMeshUpdate()`。</para>
        /// <para>② `textInfo == null` / `characterCount == 0 || lineCount == 0` ⇒ return，且**不复位** `isForceUpdatingMesh`（原版 bug 照抄：`.c:120/123/126` 三条早退都不复位，只有走完循环才在 `.c:352` 置 0 ⇒ 空文本跑过一次后 `ReactToTextChanged` 的闸恒假、再也不重弯。⛔ 别修）。</para>
        /// <para>③ `minX = bounds.min.x` · `maxX = bounds.max.x`（`.c:139/145`：`center − extent` / `center + extent`）。</para>
        /// <para>④ 逐字符：`!isVisible` 跳过；`vi = vertexIndex` · `mi = materialReferenceIndex`；</para>
        /// <para>⑤ `mid = ((v[vi].x + v[vi+2].x) × 0.5, characterInfo.baseLine, 0)` —— 4 个顶点先**减去** `mid`；</para>
        /// <para>⑥ `x0 = (mid.x − minX) / (maxX − minX)`；`x1 = x0 + 0.0001f`；
        ///         `y0 = curve(x0) × yCurveScaling`；`y1 = curve(x1) × yCurveScaling`；</para>
        /// <para>⑦ 切向 `tangent = (x1·(maxX−minX)+minX − mid.x, y1 − y0, 0)`；
        ///         `angle = Acos(dot(→x, normalize(tangent))) × 57.2958`，**`tangent.y ≤ 0` 时 `angle = 360 − angle`**；</para>
        /// <para>⑧ 矩阵：`axis == Y` ⇒ `TRS((0, y0, 0), Euler(0, 0, angle))`；
        ///         `axis == Z` ⇒ `TRS((0, 0, y0), Euler(−angle, 0, 0))`（`.c` 的 axis 分支逐句核过）；</para>
        /// <para>⑨ 4 个顶点过矩阵，再**加回** `mid`；每字符结束后 `UpdateVertexData(Vertices)`（原版就在循环里，照抄）。</para>
        /// <para>⑩ 循环结束 `isForceUpdatingMesh = false`（**唯一复位点**，判据 `.c:352`）。</para></summary>
        public void WarpText()
        {
            var tmp = text;
            if (tmp == null) return;
            _warping = true;
            tmp.havePropertiesChanged = true;
            tmp.ForceMeshUpdate();

            var ti = tmp.textInfo;
            if (ti == null) { return; }                                                      // 原版这里早退不复位 isForceUpdatingMesh ⇒ 空文本跑过一次后 ReactToTextChanged 的闸恒假、再也不重弯（原版 bug，照抄，⛔ 别修；判据 `.c:120` 早退 / `.c:352` 唯一复位点）
            if (ti.characterCount == 0 || ti.lineCount == 0) { return; }                     // 同上（`.c:123` / `.c:126` 两条早退都不复位；只有走完循环才在 `.c:352` 置 0）

            var b = tmp.bounds;
            float minX = b.center.x - b.extents.x;
            float maxX = b.center.x + b.extents.x;
            float span = maxX - minX;

            for (int i = 0; i < ti.characterCount; i++)
            {
                var ci = ti.characterInfo[i];
                if (!ci.isVisible) continue;
                int vi = ci.vertexIndex;
                int mi = ci.materialReferenceIndex;
                if (mi >= ti.meshInfo.Length) continue;
                var verts = ti.meshInfo[mi].vertices;
                if (vi + 3 >= verts.Length) continue;

                float midX = (verts[vi].x + verts[vi + 2].x) * 0.5f;
                float baseLine = ci.baseLine;
                for (int j = 0; j < 4; j++)
                {
                    var v = verts[vi + j];
                    v.x -= midX;
                    v.y -= baseLine;
                    verts[vi + j] = v;
                }

                float x0 = span == 0f ? 0f : (midX - minX) / span;
                float x1 = x0 + 0.0001f;
                float y0 = (vertexCurve != null ? vertexCurve.Evaluate(x0) : 0f) * yCurveScaling;
                float y1 = (vertexCurve != null ? vertexCurve.Evaluate(x1) : 0f) * yCurveScaling;

                var tangent = new Vector3(x1 * span + minX - midX, y1 - y0, 0f);
                var n = tangent.normalized;
                float dot = Mathf.Acos(Vector3.Dot(Vector3.right, n)) * 57.29578f;
                float angle = tangent.y > 0f ? dot : 360f - dot;

                Matrix4x4 m = axis == Axis.Y
                    ? Matrix4x4.TRS(new Vector3(0f, y0, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one)
                    : Matrix4x4.TRS(new Vector3(0f, 0f, y0), Quaternion.Euler(-angle, 0f, 0f), Vector3.one);

                for (int j = 0; j < 4; j++)
                {
                    var v = m.MultiplyPoint3x4(verts[vi + j]);
                    v.x += midX;
                    v.y += baseLine;
                    verts[vi + j] = v;
                }
                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
            _warping = false;
        }
    }
}
