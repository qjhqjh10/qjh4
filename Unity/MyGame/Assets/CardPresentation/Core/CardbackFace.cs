// CardbackFace.cs — 原版 uGUI `Image` 画一张**卡背 sprite** 的那套算法（本仓**唯一**一份口径）。
//
// 新建 2026-10-19（执行代理 B2）。起因：2026-10-18 那一轮把 `m_PreserveAspect = 1` 读成
// 「按**贴图自己**的比例内接」—— 那只做了 uGUI 的**第一段**，四处调用点（收藏窗卡背格 / 卡组编辑
// 卡背格 / 侧栏已装备 / 拖影预览）与战斗牌堆全都偏差（尺寸最大 +16.1%、位置最大 ~8px 且我们恒居中）。
// 裁定正本 = `Unity/资料/普查产出_第八会话/C1_卡背比例裁定.md`（只读查证，「两段」的结论出自它）。
//
// ==================================================================
//  一句话
// ==================================================================
// **原版是「两段」，不是「二选一」**：
//   ① **定框**：uGUI 只用 `activeSprite.rect`（= Sprite 的 **`m_Rect`**，卡背那 233 张 **恒 707×1020**、
//      比例 **0.693137**）去**内接**进节点框 —— ⛔ *不是* `textureRect`（那张逐张不同）。
//   ② **贴画**：把贴图贴到「框里**按 `padding` 内缩后**的那一块」上。
//      ⇒ 「画出来」= 框 × (`textureRect` / `m_Rect`)，**两轴系数相等 ⇒ 仍等比**；
//         画心的位置 = 框中心 **+ `((padL−padR)/2, (padB−padT)/2) × (框宽/707)`**。
//
//   对我们那批**已按 `textureRect` 裁好**的 PNG（`Resources/Art/cardbacks/<名>.png`）：
//     · **比例保持现状**（PNG 自己的比例 == 画心比例 ✔）；
//     · **尺寸要整体乘 `texRect/m_Rect`**（多数为「宽也缩、高也缩」，最坏 `Cardback_All_Early Backer`
//       只有 `608.85/707` = 宽缩 **13.9%**）；⛔ **不是「改比例」**。
//     · 还要按上面的 `offset` **挪位**（最大 水平 7.97px / 垂直 8.32px @250 宽的框；我们原来恒居中）。
//
// 🔴 **⛔ 别「单拿 0.6931 套到裁片上」**：那会把 `Cardback_AM_Shield of Humanity` 画成
//    `220×317.4`，而原版是 `220×305.3`（**竖着多 4%**）。两份数据（`m_Rect` + `padding`）**都要**。
//
// ==================================================================
//  判据（uGUI 源码，本机现读）
// ==================================================================
// `MyGame/Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Image.cs`
//   · `GetDrawingDimensions(bool)`（`:830-860`）：
//       `padding = DataUtility.GetPadding(activeSprite)`；`size = (rect.width, rect.height)`
//       `spriteW = RoundToInt(size.x)` · `spriteH = RoundToInt(size.y)`
//       ⇒ `v = (padL/spriteW, padB/spriteH, (spriteW−padR)/spriteW, (spriteH−padT)/spriteH)`
//       ⇒ 画出来 = `r.x + r.width·v.x` … `r.x + r.width·v.z`（y 同理）—— **贴到框里内缩后的那一块上**
//   · `PreserveSpriteAspectRatio(ref Rect, Vector2 spriteSize)`（`:810-827`）：
//       `spriteRatio > rectRatio ⇒ rect.height = rect.width/spriteRatio`（**宽定**），否则 `宽 = 高×spriteRatio`；
//       两支都绕 `rectTransform.pivot` 挪（我们建的都是 `.5/.5` ⇒ **绕中心**）。
//   · `Sprite.padding` = `rect − textureRect`（四条边各自的差）—— 所以
//       `texRectW = m_Rect.w − padL − padR`、`texRectH = m_Rect.h − padB − padT`。
//   ⚠️ `Sprites.DataUtility.GetOuterUV/GetPadding` 是 **native**（PackageCache 里只有调用点）——
//      「贴图画在按 padding 内缩的那块上」这一环是**推**的（依据见
//      `资料/普查产出_第八会话/C1_卡背比例裁定.md` §Q1 末）。若这一环反了，「定框」那一半不变，
//      只有 `w/h` 会换成「画满框」那一档。
//
//  数据（`m_Rect` + `padding`）→ `Resources/Cardbacks.json` 的 `items` 六列，
//  生成器 `工具/gen_cardbacks.py`（读原版 `bundle_cosmeticscardbacksimages_assets_all/Sprite/<名>_Main.json`）。
//
// ==================================================================
//  决定：**哪些贴图走这条**（以及为什么不走）
// ==================================================================
//  · 「原版节点本身就是 `m_Rect` 比例」的那几处（战斗牌堆 `Cardback` / `Cardback Shadow SDF`）
//    ⇒ `PreserveSpriteAspectRatio` 是**空操作**（PA=0 也一样），**只有「第二段」在起作用**。
//    本函数对它们**照样成立**（`box` 传节点自己的宽高即可）—— 这正是牌堆为什么要走它。
//  · 🔴 **`_SDF` 那批（`<名>_sdf`）【不】进表 ⇒ 走本函数的兜底（= 按贴图自身比例内接、不挪位）**。
//    理由：`_SDF` 全部 **233/233** 的 `m_Rect` = **(899.5,447,100,130.5)**、`textureRect` = **(900,447.25,100,130.5)**
//    ⇒「padding ≈ 0（只差图集取整的 0.5px）」，两段式与现行的「按贴图比例内接」**差 <0.4%**
//    （337.5×438.75 vs 337.5×440.26，**1.5px**）；而严格套公式反而会因那 0.5px 取整把它**横挪 1.7px**。
//    ⇒ 按 `C1` 的裁定「这层本来就是对的、不用动」**保持原样**（⛔ 那 1.7px 不是原版语义，是图集量化噪声）。
//  · **没登记的贴图**（不是卡背）⇒ 兜底 = 拿**贴图自己的宽高**当 `m_Rect`、`padding = 0`
//    ⇒ 算式**逐字退化**成本仓原来的「按贴图自身比例内接、居中」（`MenuDraw.Rect` / `CosmeticPreview.PreserveAspectSize`
//    老写法），**旧调用点一个像素都不变**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>卡背 sprite 的「两段式」画法 —— 判据与算式全文见文件头。**本仓唯一一份口径**，
    /// 别在调用处再抄一遍（两处写同一条规则 = 迟早不一致）。</summary>
    public static class CardbackFace
    {
        /// <summary>贴图名 → 原版那张 sprite 的 `m_Rect` 与 `padding`（都按**贴图像素**）。
        /// 未登记的（`_sdf` / 非卡背）返回 **false**（不是「返回 0」—— 调用方要能区分）。</summary>
        public static bool TryRect(string texName, out float rectW, out float rectH,
                                   out float padL, out float padR, out float padB, out float padT)
        {
            return CardbackTable.TrySpriteRect(texName, out rectW, out rectH,
                                               out padL, out padR, out padB, out padT);
        }

        /// <summary>在同一份口径下把一张 250×405 / 337.5×550.8 / 220×330 / 217×314 的**框**摆出来。
        ///
        /// <para><b>出参单位 = 入参那个框的单位</b>（画布像素 / 世界单位都行，只看调用方传的是哪种）：
        /// `w,h` = **画出来的**宽高；`dx,dyUp` = 画心相对**框中心**的偏移，
        /// **`dyUp` 向上为正**（uGUI 的方向）—— 本仓画布像素是**y 向下**
        /// （`PxRect` / `MenuDraw`）⇒ 那边要写成 `中心y − dyUp`；世界坐标（`LayoutSpace` / `DeckRuntime.Pos`）
        /// 是 **y 向上** ⇒ 直接 `+dyUp`。⛔ 别在调用处凭感觉翻符号，照这两个先例对。</para>
        ///
        /// <para>返回 `false` = **没有可用的几何**（贴图为 null / 框非正 / 登记表里的 `m_Rect` 是 0）
        /// ⇒ 调用方**不许改几何**（与 <c>CosmeticPreview.PreserveAspectSize</c> 的老口径一致）。</para></summary>
        public static bool Fit(Texture2D t, float boxW, float boxH,
                               out float w, out float h, out float dx, out float dyUp)
        {
            w = boxW; h = boxH; dx = 0f; dyUp = 0f;
            if (t == null || boxW <= 0f || boxH <= 0f) return false;

            float rw, rh, pl, pr, pb, pt;
            if (!TryRect(t.name, out rw, out rh, out pl, out pr, out pb, out pt))
            {
                // 兜底：把「贴图自己」当成 `m_Rect`（`padding = 0`）⇒ 下面整段退化成
                // 「按贴图自身比例内接、居中」—— 与改动前的老写法**逐字同值**。
                rw = t.width; rh = t.height; pl = pr = pb = pt = 0f;
            }
            if (rw <= 0f || rh <= 0f) return false;

            // ① 定框：uGUI `Image.PreserveSpriteAspectRatio`（比的是 `sprite.rect` 的比例）
            float spriteRatio = rw / rh, boxRatio = boxW / boxH;
            float fw, fh;
            if (spriteRatio > boxRatio) { fw = boxW; fh = boxW / spriteRatio; }   // 宽定、高缩
            else { fh = boxH; fw = boxH * spriteRatio; }                          // 高定、宽缩

            // ② 画心：框 × (textureRect / m_Rect)，再按 padding 挪 —— 两轴系数相等 ⇒ 等比
            float sw = (rw - pl - pr) / rw, sh = (rh - pb - pt) / rh;
            float k = fw / rw;                 // == fh / rh（框的比例就是 m_Rect 的比例）
            w = fw * sw;
            h = fh * sh;
            dx = (pl - pr) * 0.5f * k;
            dyUp = (pb - pt) * 0.5f * k;
            return true;
        }
    }
}
