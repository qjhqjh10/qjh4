// UnitChatPanel.PartB118W3.cs — `B117-T09`（语音条卡图「画大再裁」）的**追加分部**。
//
// 🔴 为什么单开一个 partial 文件（⛔ 别把这些成员搬回 `UnitChatPanel.cs`）：
//   `UnitChatPanel.cs` 的 `CardChatIntro`（文件后半那个静态类）有 **4 条账**按 `文件:行号` 引用
//   （`:1669` ×2 `TryResolve` · `:1703` `PlayerTalksFirst`），而 `UnitChatPanel` 这个类的成员**结束在它们之前**
//   ⇒ 往里加任何成员都会把这 4 条整体推移（实测 +59 行 = 4 条漂移）。分部文件让主文件的**行号一个不动**。
//
// 判据（`bundle_scenes_scenes_battlearena1` 现读，三颗气泡逐值同）：
//   · `Mask` = **RectMask2D**，框 **144.457×157.465**（`RectTransform_2600/3347/3408`）；
//     `m_Padding (0,0,0,0)` · `m_Softness (0,0)`（`MonoBehaviour_4340/4724/4798`）= **硬边**；
//   · 子件 `CardImage` = **176×176**、`m_PreserveAspect = 0`、`m_Maskable = 1`、`m_RaycastTarget = 1`、
//     `m_AnchoredPosition = (−2.48, −5.00)`（`RectTransform_2844/3108/3169`）。
// ⇒ 可见内容 = 那张方图的 **~82%×89% 中心块**；改前我们是把整张图压到 0.9175 画满遮罩
//   ⇒ 多看到两边、且横向压 8%（判据 → `资料/完全复刻/判据/B117-G10-22_战斗内聊天与语音条.md` §二 第 5 行）。
// ✅ **2026-11-02（`B120-W3` · 账 `B-14`）世界档已补齐**：没有 `RectMask2D` 这种组件，改用**UV 裁剪**
//   （`PortraitCropUv` / `ApplyPortraitCrop`）表达同一块可见区域；生产仍走画布档（`BattleDriver.cs:29506`）。
// ⛔ 别把 176 改回 144.46（那就是「压满遮罩」那版）；也⛔ 别给子图调 `SetAspect`（原版 `PA = 0` 恒 176²）。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    public partial class UnitChatPanel
    {
        const float PortraitSpecPx = 176f;            // 原版 `CardImage` 的 `m_SizeDelta`（两轴同值）
        const float PortraitOffsetX = -2.48f;         // 原版 `m_AnchoredPosition.x`
        const float PortraitOffsetYTop = 5.00f;       // 原版 `m_AnchoredPosition.y = −5.00`（y 向上）⇒ 本仓 y 向下 = **+5.00**

        /// <summary>建卡图那一格：uGUI 档 = **遮罩容器**（`RectMask2D`，144.457×157.465）+ 子图 176×176；
        /// 世界档 = **UV 裁剪**（`PortraitCropUv`，账 `B-14` 补齐）。返回的 `HudPiece` 是**子图**（`SetTexture` / `SetRaycastTarget` 都吃它）。</summary>
        HudPiece MakePortrait(Slot at, float xPx, float yPxTop, string goName, float z)
        {
            var tex = CardArt.Solid();
            if (tex == null) return null;
            if (!_ugui) { var q0 = Quad(at, xPx, yPxTop, tex, PortraitH, goName, z); if (q0 != null) { q0.SetAspect(PortraitW / PortraitH); ApplyPortraitCrop(q0); } return q0; }   // 🆕 2026-11-02（`B120-W3` · 账 `B-14`）：世界档补 `RectMask2D` 的等价物 = **UV 裁剪**（见类尾 `PortraitCropUv`）
            var maskGo = new GameObject("chat_card_mask_" + goName, typeof(RectTransform));
            maskGo.transform.SetParent(transform, false);
            var mrt = (RectTransform)maskGo.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(PortraitW, PortraitH);
            maskGo.AddComponent<RectMask2D>();       // padding 0 / softness 0（出厂值）= 原版硬边
            HudCanvas.Add(mrt, z);
            HudCanvas.SetAnchorPx(mrt, at.ax, at.ay, xPx, yPxTop);
            return HudCanvas.MakeImageInParent(mrt, tex, goName,
                                               PortraitOffsetX, PortraitOffsetYTop,
                                               PortraitSpecPx, PortraitSpecPx, z, Color.white);
        }

        /// <summary>摆卡图那一格：**只挪遮罩容器**（子图相对容器固定）。世界档 / 没建遮罩 ⇒ 挪自己。</summary>
        void MovePortrait(Bubble b, float xPx, float yPxTop, float z, float ax, float ay)
        {
            var mrt = PortraitMaskOf(b);
            if (mrt != null) { HudCanvas.SetAnchorPx(mrt, ax, ay, xPx, yPxTop); HudCanvas.Reorder(mrt, z); return; }
            Move(b != null ? b.portrait : null, xPx, yPxTop, z, ax, ay);
        }

        /// <summary>这一颗的卡图是不是罩在 `RectMask2D` 下（是 ⇒ 子图恒 176²、⛔ 别再 `SetAspect`）。</summary>
        static bool HasPortraitMask(Bubble b) { return PortraitMaskOf(b) != null; }

        static RectTransform PortraitMaskOf(Bubble b)
        {
            if (b == null || b.portrait == null || b.portrait.Rect == null) return null;
            var p = b.portrait.Rect.parent as RectTransform;
            return p != null && p.GetComponent<RectMask2D>() != null ? p : null;
        }

        /// <summary>自检用：某一颗气泡的**卡图遮罩**（uGUI 档才有；世界档 / 没建 ⇒ `null`）。</summary>
        public RectTransform BubblePortraitMaskForTest(int side) { return PortraitMaskOf(BubbleFor(side)); }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-11-02（`B120-W3` · 账 `B-14`）：**世界档**补 `RectMask2D` 的等价物 = UV 裁剪
        //  ───────────────────────────────────────────────────────────────────────────────────────────
        //  原版（判据 = 上面文件头那三颗，`RT_2600/3347/3408` + `RT_2844/3108/3169`）：
        //    · 遮罩容器 `144.457 × 157.465`（`RectMask2D`，padding/softness 全 0 = 硬边）；
        //    · 子图 `CardImage` **176 × 176**、`anchoredPosition = (−2.48, −5.00)`（y 向上）、`PA = 0`。
        //  ⇒ 透过遮罩真正看到的，是那张 176² 方图里**中间一块 144.457×157.465** 的区域：
        //    把「遮罩矩形」反算回子图自己的 [0,1] UV 就得到下面这组数（y 轴同样按 Unity 的 y 向上）：
        //      · x: 视口相对子图中心 = `(−72.2285 + 2.48, +72.2285 + 2.48)` = `(−69.7485, 74.7085)`
        //           ⇒ 归一化 (÷176，加半宽 88) = `0.103693 … 0.924489`（宽 0.820796 = 144.46/176 ✓）
        //      · y: 视口相对子图中心 = `(−78.7325 + 5.00, +78.7325 + 5.00)` = `(−73.7325, 83.7325)`
        //           ⇒ 归一化 = `0.081080 … 0.975739`（高 0.894659 = 157.46/176 ✓）
        //  ⚠️ 这是**等价实现**而不是自设计：世界档没有 `RectMask2D` 这种组件，而「少看到哪一截」这个
        //     **结果**与原版逐像素相同。⛔ 别再退回「把整张图压满遮罩」（那会多看到两边、且横向压 8%）。
        //  ⚠️ 与生产档（uGUI）无关 —— 生产由 `BattleDriver.cs:29506` 传画布宿主 ⇒ `_ugui = true` 走遮罩。
        // ═══════════════════════════════════════════════════════════════════════════════════════════
        static readonly Rect PortraitCropUv = new Rect(0.103693f, 0.081080f, 0.820796f, 0.894659f);

        /// <summary>世界档：把卡图那张方图的 UV 收到「透过 144.46×157.46 硬边遮罩看得见的那一块」。
        /// 只对手里的 `ImageQuad`（`q.Quad != null`）有效；uGUI 档是空操作（它有自己的 `RectMask2D`）。</summary>
        static void ApplyPortraitCrop(HudPiece q)
        {
            if (q == null || q.Quad == null) return;
            q.Quad.SetUvRect(PortraitCropUv);
        }
    }
}
