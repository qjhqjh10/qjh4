// PragatiDigits.cs — 卡面**数字**的字形（原版 `Pragati-Regular`）
//
// 为什么会有这个文件（出处：`资料/PnP卡图_逐张对账_0915.md` §六末 **A3**）：
//   费用与四个数值一直是自写的 **5×7 点阵**（`Core/TextCanvas.cs` 的 `Glyphs`），
//   而原版实测**没有任何点阵字形** —— 那几个数字是 `TextMeshProUGUI` + **`Pragati-Regular SDF`**
//   （逐字段读 `d:/2/解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_{3782,3743,…}.json`
//    的 `m_fontAsset` / `m_fontSize`；`PerfectDOSVGA437.ttf` 是 **Unity 内置调试字体**，原版从没用过）。
//   本文件消费的字形就是**从原版那张 SDF 图集里抠出来的**，见 `PragatiDigits.Data.cs`。
//
// ⚠️ **故意不和 `TextCanvas` 合并**：`TextCanvas` 还被 HUD 用（`Battle/Label.cs`），
//    动它等于全 HUD 一起换字形。这边只服务卡面的数字。
using System;
using UnityEngine;

namespace CardPresentation
{
    public static partial class PragatiDigits
    {
        public struct Glyph
        {
            public string variant;   // "thin" / "thick"（原版两档描边）
            public char ch;
            public int x, y, w, h;   // 在表里的位置与尺寸（表内像素）
            public float adv;        // 步进
            public float bearingX;   // 左轴承
        }

        /// <summary>逐像素：`rgb` = 字面色（白），`a` = **描边**覆盖。表里不存 RGB，见生成器注释</summary>
        static Color32[] _sheet;

        public static bool Available { get { return Load(); } }

        static bool Load()
        {
            if (_sheet != null) return true;
            byte[] raw;
            try { raw = Convert.FromBase64String(SheetB64); }
            catch (Exception e)
            {
                Debug.LogError("[PragatiDigits] 表解不开（生成物坏了？重跑 `工具/gen_pragati_digits.py`）：" + e.Message);
                return false;
            }
            int need = SheetW * SheetH * 2;
            if (raw.Length < need)
            {
                Debug.LogError($"[PragatiDigits] 表长 {raw.Length} < 需要 {need} —— 重跑 `工具/gen_pragati_digits.py`");
                return false;
            }
            var px = new Color32[SheetW * SheetH];
            for (int i = 0; i < px.Length; i++)
            {
                byte face = raw[i * 2], outline = raw[i * 2 + 1];
                byte a = outline > face ? outline : face;   // 描边至少把字面盖住
                // 字面白、描边黑 ⇒ 直通 alpha 的 `rgb` 就是「白到什么程度」= 字面覆盖
                px[i] = new Color32(face, face, face, a);
            }
            _sheet = px;
            return true;
        }

        /// <summary>一行数字有多宽（表内像素）。表里没有的字符按半个字宽跳过（**不静默画成别的字**）</summary>
        public static int Measure(string s, bool thick)
        {
            if (string.IsNullOrEmpty(s) || !Load()) return 0;
            int w = 0;
            foreach (var c in s)
            {
                var g = Find(c, thick);
                w += g.h == 0 ? DigitH / 2 : Mathf.RoundToInt(g.adv);
            }
            return w;
        }

        /// <summary>
        /// 把一行数字画进卡面贴图的 CPU 缓冲（`CardView.InfoTexture` 那张 `Color32[]`）。
        /// `cx` / `cy` 是这行字的**中心**，坐标是 `px` 数组里的像素（**y 从顶部数**，和 `TextCanvas` 一致）。
        /// 颜色固定：原版数值/费用就是**白字 + 黑描边**（`m_fontColor=(1,1,1,1)` + 材质 `_OutlineWidth`）。
        /// </summary>
        /// <param name="maxW">
        /// 这行字**最多允许多宽**（表内像素；0 = 不限）。超了就**整体等比缩号**。
        ///
        /// 🔴 **为什么必须有它**（2026-09-16 修）：`DigitH = 28` 是按原版**一位数**的字号
        /// （0.34 卡单位）折算的，而 `Draw` 原来**只居中、不缩号** ⇒ 两位数的总宽
        /// `Measure("10")` = 36.6 面像素 > 数值圆净宽 ≈ 31 ⇒ **必然溢出**。
        /// **实据**：`EC41 Chaos Land Raider` 的紫圈里那个 `10`，左边空 ~15 px、右边缘正好压在圈的右内沿上，
        /// 而原版（`Emperors Children/3部队/Warpforge_41_Chaos-Land-Raider.png`）同一个圈里的 `10`
        /// **四周都有余量**。（子代理在 700 px 渲染图上量圆为 x 113→203 = 90 px
        /// ⇒ 折算到我们 `FaceW=256` 的卡面 ≈ 33 px，减 2 px 边距 ⇒ 调用方传 31。）
        /// </param>
        public static void Draw(Color32[] px, int W, int H, string s, int cx, int cy, bool thick, int maxW = 0)
        {
            if (string.IsNullOrEmpty(s) || !Load() || px == null) return;
            float scale = 1f;
            if (maxW > 0)
            {
                int measured = Measure(s, thick);
                if (measured > maxW) scale = (float)maxW / measured;
            }
            int x = cx - Mathf.RoundToInt(Measure(s, thick) * scale * 0.5f);
            int top = cy - Mathf.RoundToInt(DigitH * scale * 0.5f);
            foreach (var c in s)
            {
                var g = Find(c, thick);
                if (g.h == 0) { x += Mathf.RoundToInt(DigitH * scale * 0.5f); continue; }
                Blit(px, W, H, g, x, top, scale);
                x += Mathf.RoundToInt(g.adv * scale);
            }
        }

        static void Blit(Color32[] px, int W, int H, Glyph g, int x0, int y0, float scale)
        {
            // 缩号：**目的像素反查源像素**（最近邻）。`scale >= 1` 时退化成原来的 1:1 逐像素。
            int dw = Mathf.Max(1, Mathf.RoundToInt(g.w * scale));
            int dh = Mathf.Max(1, Mathf.RoundToInt(g.h * scale));
            for (int oy = 0; oy < dh; oy++)
            {
                // 🔴 卡面缓冲 **row 0 在底部**，而传进来的 `y0` / `oy` 是**从顶部数**的
                //    （`Draw` 的注释就是这么写的）—— 这里必须翻一次。
                //    2026-09-16 修：这文件 2026-09-15 23:34 新加时**漏了这一步**，
                //    于是整批数字上下镜像：底部三个数值格的数字跑到**卡顶外沿**、
                //    费用的跑到中右。`CardView.FillHexagon` / `TextCanvas` 都翻了，只有这里没翻。
                //    ⚠️ 四条自检**一条都测不出来** —— 断言看不出「数字画在哪」，只能靠并排看渲染图。
                int y = (H - 1) - (y0 + oy);
                if (y < 0 || y >= H) continue;
                int sy = Mathf.Min(g.h - 1, Mathf.FloorToInt(oy / scale));
                int row = (g.y + sy) * SheetW + g.x;
                for (int ox = 0; ox < dw; ox++)
                {
                    int x = x0 + ox;
                    if (x < 0 || x >= W) continue;
                    int sx = Mathf.Min(g.w - 1, Mathf.FloorToInt(ox / scale));
                    var s = _sheet[row + sx];
                    if (s.a == 0) continue;

                    int i = y * W + x;
                    var d = px[i];
                    float sa = s.a / 255f, da = d.a / 255f;
                    float oa = sa + da * (1f - sa);            // 直通 alpha 的 over
                    if (oa <= 0f) continue;
                    px[i] = new Color32(
                        (byte)Mathf.Clamp((s.r * sa + d.r * da * (1f - sa)) / oa, 0f, 255f),
                        (byte)Mathf.Clamp((s.g * sa + d.g * da * (1f - sa)) / oa, 0f, 255f),
                        (byte)Mathf.Clamp((s.b * sa + d.b * da * (1f - sa)) / oa, 0f, 255f),
                        (byte)Mathf.Clamp(oa * 255f, 0f, 255f));
                }
            }
        }

        static Glyph Find(char c, bool thick)
        {
            string want = thick ? "thick" : "thin";
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i].ch == c && Rows[i].variant == want) return Rows[i];
            return default(Glyph);
        }
    }
}
