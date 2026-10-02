// CostCurveDrawer.cs — 「费用曲线」那一套（原版 `DeckEnergyCostDrawer` + `DeckCostQuantityRowDrawer`）。
//
// ============================ 为什么要这个文件 ============================
// 同一套东西在**两扇窗**里各出现一次：
//   · `Deck info Popup` 的 `Deck Info` 抽屉 —— `Deck Information cost drawer`（`localScale` **1.8**）
//   · 练习窗 `Practice Mode Menu` 的 `General container` —— `Deck Information cost drawer`（`localScale` **1.2**）
// 🔴 CLAUDE.md §三：「两处写同一条规则 = 迟早不一致」⇒ **画法与数数的规则只此一份**，
//    两扇窗各传自己的「中心 + 缩放 + 队列」进来。
//
// ============================ 判据（几何逐值） ============================
// 出处：`python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`（2026-10-03 实测）
//   抽屉     `Deck Information cost drawer` rect **160×200** · **`localScale 1.8`**（练习窗那份是 1.2）
//   `Content` `VerticalLayoutGroup` spacing **3.43** · **9 行**（费用 0..8）
//   行       每行 **223.59 × 18.91** · 步进 **22.295**（= 18.91 + 3.43）· 第 0 行中心在 `Content` 顶 +9.455
//   行内     `Card Cost` 文本（居中 · 绿 `(0.296,0.774,0.496,1)`）· `Cards in deck`（居中 · 白）·
//            `Slider` **151.34 × 20** · 底 `40k_CardAmount_bar_bg`（九宫 3,0,15,0）·
//            填充 `40k_CardAmount_bar_fill`（九宫 3,0,14,0 · 色 `(1,0.818,0.486,1)`）
//   相对抽屉中心（**未乘 scale**）：费用数字 x **−95.65** · 数量 x **+95.2** · 第 0 行 y **−90.045**
// ⚠️ 字号 `25`（原版的 `m_fontSize`）也要**乘 scale** —— 原版是靠节点缩放放大的，我们按放大后的像素直接画。
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>费用曲线（原版 `DeckEnergyCostDrawer`）。**画法与数数只此一份** —— 见文件头。</summary>
    public static class CostCurveDrawer
    {
        /// <summary>行数 = **9**（费用 0..8；原版 `DeckEnergyCostDrawer` 序列化了 9 行）。</summary>
        public const int Rows = 9;
        public const float RowW = 223.59f, RowH = 18.91f, RowStep = 22.295f;
        /// <summary>第 0 行**行心**相对抽屉中心的 y（未乘 scale）。</summary>
        public const float Row0Dy = -90.045f;
        /// <summary>行内三件的相对位置（未乘 scale）。</summary>
        public const float CostDx = -95.65f, CountDx = 95.2f, SliderW = 151.34f, SliderH = 20f;
        /// <summary>行内字号（原版 `m_fontSize` = 25；**乘 scale 之后才是画布像素**）。</summary>
        public const float RowFontPx = 25f;
        public static readonly Color CostColor = new Color(0.296f, 0.774f, 0.496f, 1f);
        public static readonly Color FillTint = new Color(1f, 0.818f, 0.486f, 1f);
        const string ArtBarBg = "40k_CardAmount_bar_bg", ArtBarFill = "40k_CardAmount_bar_fill";

        /// <summary>按费用数一遍（费用 0..8，越界夹到两端）。`find` = id → 卡（两边都传 `CollectionData.Card`）。</summary>
        public static int[] Counts(PlayerDeck deck, Func<string, CardDef> find)
        {
            return Counts(deck != null ? deck.CardIds : null, find);
        }

        public static int[] Counts(IEnumerable<string> cardIds, Func<string, CardDef> find)
        {
            var by = new int[Rows];
            if (cardIds == null || find == null) return by;
            foreach (var id in cardIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var c = find(id);
                if (c == null) continue;
                by[Mathf.Clamp(c.Cost, 0, Rows - 1)]++;
            }
            return by;
        }

        /// <summary>把 9 行画出来。
        /// <paramref name="cx"/><paramref name="cy"/> = 抽屉**中心**（绝对画布像素）·
        /// <paramref name="scale"/> = 原版那一档 `localScale`（`Deck Info Popup` **1.8** / 练习窗 **1.2**）·
        /// <paramref name="rowName"/> = 行的节点名前缀（两扇窗的原版名一样，都是 `Deck CostQuanityt Row Drawer`）。</summary>
        public static void Build(Transform parent, float cx, float cy, float scale, int[] counts,
                                 int qRow, int qText, string rowName = "Deck CostQuanityt Row Drawer")
        {
            if (parent == null || counts == null) return;
            int max = 1;
            for (int i = 0; i < counts.Length && i < Rows; i++) if (counts[i] > max) max = counts[i];

            float rowW = RowW * scale, rowH = RowH * scale;
            for (int i = 0; i < Rows; i++)
            {
                float rcy = cy + (Row0Dy + i * RowStep) * scale;
                float x1 = cx - rowW * 0.5f, x2 = cx + rowW * 0.5f;
                float y1 = rcy - rowH * 0.5f, y2 = rcy + rowH * 0.5f;
                var node = MenuDraw.Node(parent, rowName + (i == 0 ? "" : " (" + i + ")"),
                                         new PxRect(x1, y1, x2, y2));

                // 费用数字（绿）
                float ncx = cx + CostDx * scale;
                MenuDraw.Text(node, new PxRect(ncx - 23f * scale, y1, ncx + 23f * scale, y2),
                              i.ToString(), CostColor, "Card Cost", RowFontPx * scale, qText);

                // 滑块：底 + 填充（九宫格）
                float sx1 = cx - SliderW * scale * 0.5f, sx2 = cx + SliderW * scale * 0.5f;
                float sy1 = rcy - SliderH * scale * 0.5f, sy2 = rcy + SliderH * scale * 0.5f;
                var bgTex = CardArt.MenuUi(ArtBarBg);
                if (bgTex != null)
                    MenuDraw.Nine(node, bgTex, new PxRect(sx1, sy1, sx2, sy2), new Vector4(3f, 0f, 15f, 0f),
                                  bgTex.width, bgTex.height, qRow, null, true, "Background");
                int n = i < counts.Length ? counts[i] : 0;
                float frac = n / (float)max;
                if (frac > 0f)
                {
                    var fillTex = CardArt.MenuUi(ArtBarFill);
                    if (fillTex != null)
                        MenuDraw.Nine(node, fillTex, new PxRect(sx1, sy1, sx1 + (sx2 - sx1) * frac, sy2),
                                      new Vector4(3f, 0f, 14f, 0f), fillTex.width, fillTex.height, qRow, FillTint,
                                      true, "Fill");
                }

                // 张数（白）
                float ccx = cx + CountDx * scale;
                MenuDraw.Text(node, new PxRect(ccx - 23f * scale, y1, ccx + 23f * scale, y2),
                              n.ToString(), Color.white, "Cards in deck", RowFontPx * scale, qText);
            }
        }

        /// <summary>自检用的读数：`0:3 1:8 …` 那种一行。</summary>
        public static string Dump(int[] counts)
        {
            if (counts == null) return "(空)";
            var sb = new StringBuilder();
            for (int i = 0; i < counts.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(i).Append(':').Append(counts[i]);
            }
            return sb.ToString();
        }
    }
}
