// UICornersGradient.cs — 原版 uGUI 件 `UICornersGradient`（四角渐变：按顶点位置双线性插值四角颜色）
// 🆕 2026-10-28 · 执行代理 `B93_N6` · 账 `未分类_其他:667`（`UICornersGradient.ModifyMesh(VertexHelper)`）
//
// ── 为什么单开一个文件 ──────────────────────────────────────────────────────────
//  原版它是一个 `BaseMeshEffect`（MonoBehaviour 派生）。**类名 == 文件名**在本仓是硬约束：
//  类名与所在文件不同 ⇒ Unity 建不出脚本资产 ⇒ 存盘时烘成内联 `MonoScript`（`!u!115`）存根、**player 加载即崩**
//  （`CLAUDE.md` §二「跑任何会存盘的自检宿主之前」那一大段，2026-10-13 查实）。所以衍生物类**必须**单独成文件。
//
// ── 判据（三源同读，2026-10-28）──────────────────────────────────────────────────
//  · 🥇 方法体：`decomp_full/UICornersGradient__ModifyMesh.c`（**131 行**，本代理亲读全文）
//  · 🥈 同体：`cpp2il/out_cpp2il_dev_ilspy/UICornersGradient.cs`（`ModifyMesh` 全文，ILSpy 把 Color 运算打成 `object`，
//      但**四角字段名与插值次序**在那份里是清楚的）
//  · 字段表：同 ILSpy 类头 —— `m_topLeftColor` / `m_topRightColor` / `m_bottomRightColor` / `m_bottomLeftColor`
//      （四个 `Color`）。`.c` 侧的偏移互证：`+0x28` = TL（16B）· `+0x38` = TR · `+0x48` = BR · `+0x58` = BL
//      （`m_Script` 之后 0x10/0x18/0x20 是 Unity 的 MonoBehaviour 头，`Button` 那一族同形）。
//
// ── 它到底算什么（逐句对 `.c`）────────────────────────────────────────────────────
//  `.c` 的骨架 = ① `if (!enabled) return;`（`:76-79`）② 取 `graphic.rectTransform.rect`（`:81-83`）
//  ③ `for (i = 0; i < vh.currentVertCount; i++)`（`:118-121`）：`PopulateUIVertex(&uivertex, i)` →
//  用**这一颗顶点的位置**在 rect 里算出归一化 (u,v) → **双线性**插四角颜色（四个通道各一次 `FUN_1800066e0`，
//  即四次 `Mathf.Lerp`）→ **乘上顶点原色**（`.c:...` 那四段 `(nint)0 >> 8/16/24` 是把原 `Color32` 拆成
//  0..255 再 `/255f`）→ **夹到 [0,1]**（`.c` 里那四段 `if (!(0f > x)) { if (x > 1f) x = 1f; } else x = 0f;`）
//  → `* 255f` 再 `cvttss2si`（截断成字节）→ `SetUIVertex`。
//
//  ⚠️ **两处如实标注**：
//   ① 那四次 `FUN_1800066e0` 在 `.c` 里没有名字（ILSpy 写 `NoteDecompilerIssue("Method not found @1800066E0")`）。
//      从它**恰好在每个颜色通道各调一次**、前后是「乘原色 → 夹 [0,1]」来看，那是 `Mathf.Clamp01`
//      （`Color` 没有别的逐通道一元函数会出现在这个位置）。本实现直接写 `Mathf.Clamp01`。
//   ② `.c` 的 (u,v) 是**先建一个 2×2 变换再套每个顶点**（`num4 = xMin/width + 0.5`、`num5 = … + 0.5`、
//      再用 `right`/`up` 拼矩阵、末尾 `^ 0x80000000` 取负）。那是「把 rect 的局部坐标折成 [0,1]」的
//      手写展开式，净效果就是 `u = (pos.x − rect.xMin)/rect.width`、`v = (pos.y − rect.yMin)/rect.height`
//      （ILSpy 那份把它写回了 `(vertexPos - rect.min) / rect.size` 的形状）。本实现用后者 —— **同一个式子，
//      少三次浮点往返**；⛔ 别按「必须逐句照抄矩阵」改回去。
//
// ── 消费者（如实记：**今天一个都没有**）──────────────────────────────────────────
//  原版资产侧 6 实例 / 5 文件挂的就是它（`MainMenuScene` 队伍 A/B 底 · `DailyStreakPopup.bg` ·
//  `GachaEventWindow.BackgroundTop` …）。**我们这些位置走的是「建贴图时把四角渐变烘进像素」**
//  （`Core/CardArt.cs` 的 `Gradient` / `GradientKeys` 一族）⇒ 运行时不需要回调 ⇒ **本类今天零挂载点**。
//  它仍然要建：判据 = 调度台 2026-10-28 `B92_N5b` 裁定③(a)「保持 B ⇒ 具体缺口 = 缺一个 `BaseMeshEffect`
//  派生类 + 调用点」。本文件补上的是**类那一半**，调用点那一半仍是空的（见账的「差异」列）。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>原版 `UICornersGradient : BaseMeshEffect` —— 按顶点在 rect 里的归一化位置，把四个角颜色
    /// 双线性插值后**乘到**顶点色上（`ModifyMesh`）。
    /// <para>⚠️ 它是**乘**不是**替换**（`.c` 那四段 `/255f` 之后做乘）⇒ 顶点原本的 alpha（例如淡出）不会被抹掉。</para>
    /// <para>⚠️ 原版 `.c` 在 `graphic` / `rectTransform` 为空时走 `FUN_1803f47a0`（= **抛 NRE**）。
    /// 我们**不崩**：`BaseMeshEffect` 的契约保证 `graphic` 非空，真取不到 ⇒ 出声一次 + 本次不改网格。</para></summary>
    [AddComponentMenu("UI/Effects/UICornersGradient")]
    public class UICornersGradient : BaseMeshEffect
    {
        /// <summary>左上角颜色（原版 `m_topLeftColor`，字段偏移 `+0x28`）。</summary>
        public Color m_topLeftColor = Color.white;
        /// <summary>右上角颜色（原版 `m_topRightColor`，`+0x38`）。</summary>
        public Color m_topRightColor = Color.white;
        /// <summary>右下角颜色（原版 `m_bottomRightColor`，`+0x48`）。</summary>
        public Color m_bottomRightColor = Color.white;
        /// <summary>左下角颜色（原版 `m_bottomLeftColor`，`+0x58`）。</summary>
        public Color m_bottomLeftColor = Color.white;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!enabled) return;                                  // `.c:76-79`
            if (vh == null) return;
            var g = graphic;
            if (g == null)
            {
                Debug.LogWarning("[UICornersGradient] `graphic` 是空的（原版这一跳 NRE）⇒ 本次不改网格");
                return;
            }
            var rt = g.rectTransform;
            if (rt == null)
            {
                Debug.LogWarning("[UICornersGradient] `graphic.rectTransform` 是空的（原版这一跳 NRE）⇒ 本次不改网格");
                return;
            }

            Rect rect = rt.rect;                                   // `.c:83`
            float w = rect.width, h = rect.height;
            if (w <= 0f || h <= 0f) return;                        // 退化矩形：除零会得到 NaN ⇒ 整片顶点色变黑

            UIVertex v = default;
            int n = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                Vector3 pos = v.position;

                // ① 归一化位置（`.c` 的那套 2×2 变换展开式 = 这一对式子；见文件头「两处如实标注」②）
                float u = (pos.x - rect.xMin) / w;
                float t = (pos.y - rect.yMin) / h;

                // ② 双线性：先横向各插一次（底边 / 顶边），再纵向插一次 —— ⚠️ 用 `LerpUnclamped`：原版 `.c` 是裸算术 `a+(b−a)t`（**不外推夹**），夹只在最后按通道 `Clamp01` ⇒ 用会夹 t 的 `Color.Lerp` 在「顶点越出 rect」时会与原版不同
                Color bottom = Color.LerpUnclamped(m_bottomLeftColor, m_bottomRightColor, u);
                Color top = Color.LerpUnclamped(m_topLeftColor, m_topRightColor, u);
                Color col = Color.LerpUnclamped(bottom, top, t);

                // ③ **乘**上顶点原色（`.c` 把原 `Color32` 拆成 0..255 再 /255）
                Color32 src = v.color;
                float r = Mathf.Clamp01(col.r * (src.r / 255f));
                float gg = Mathf.Clamp01(col.g * (src.g / 255f));
                float b = Mathf.Clamp01(col.b * (src.b / 255f));
                float a = Mathf.Clamp01(col.a * (src.a / 255f));

                // ④ `* 255f` + `cvttss2si`（**截断**，不是四舍五入）—— `(byte)(x * 255f)` 就是截断
                v.color = new Color32((byte)(r * 255f), (byte)(gg * 255f), (byte)(b * 255f), (byte)(a * 255f));
                vh.SetUIVertex(v, i);
            }
        }

        // ============================================================ 自检口（只读；⛔ 生产路径别调）
        /// <summary>把一个顶点位置 → 本件会写进去的顶点色（**纯函数**，不碰任何网格）。
        /// <para>为什么要这一口：`ModifyMesh` 只在 uGUI 重建网格时才被调，而自检宿主**没有 uGUI 运行时**
        /// 那一套完整重建 ⇒ 直接比对「输入四角 + 顶点位置 → 输出色」这一层，才是**可断言**的那一半
        /// （与 `Label.DumpSizes` 同一思路：给断言一个能读的量）。</para></summary>
        public Color32 ColorAt(float localX, float localY, Color32 vertexColor, Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f) return vertexColor;
            float u = (localX - rect.xMin) / rect.width;
            float t = (localY - rect.yMin) / rect.height;
            Color bottom = Color.LerpUnclamped(m_bottomLeftColor, m_bottomRightColor, u);
            Color top = Color.LerpUnclamped(m_topLeftColor, m_topRightColor, u);
            Color col = Color.LerpUnclamped(bottom, top, t);
            return new Color32((byte)(Mathf.Clamp01(col.r * (vertexColor.r / 255f)) * 255f),
                               (byte)(Mathf.Clamp01(col.g * (vertexColor.g / 255f)) * 255f),
                               (byte)(Mathf.Clamp01(col.b * (vertexColor.b / 255f)) * 255f),
                               (byte)(Mathf.Clamp01(col.a * (vertexColor.a / 255f)) * 255f));
        }
    }
}
