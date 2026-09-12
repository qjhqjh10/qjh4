// TextCanvas.cs — 把文字描进 CPU 画布（自带的 5×7 点阵 ASCII 字库）
//
// 卡面和 HUD 都要文字。先后试过三条路，结论如下：
//
//   ❌ **TextMeshPro** —— 工程没导 Essentials，一个字都不显示，而导它要手动点菜单（踩过）
//   ❌ **TextMesh + 动态字体** —— 批处理下整张图是空的（图集建不起来）
//   ❌ **自己读系统字体图集描像素** —— Latin 完全正确，**汉字花成一团**
//        已经查清的：矩形映射（`min/max(uv.y)*ah` 取自下而上的区间）是对的、
//        行序（`atlasY = rowLo + gy`）是对的、图集可以直接 `GetPixels32` 读。
//        但汉字抠出来仍是**转置的**（实测『星』`glyphWidth=64, glyphHeight=59`，
//        而 UV 矩形是 59×64 宽高互换 —— 图集里那个字形本身是躺着的）。
//        未解，见 `资料/玩法线_进度与交接.md` 的「汉字渲染」一节。
//   ✅ **自写 5×7 点阵 ASCII 字库** —— 确定、无依赖、批处理里也照样出图。**现在走这条。**
//
// 所以当前**只支持 ASCII**：卡名/HUD 都用英文。要上中文得先把上面那条路修通，
// 或者换一条（把系统字体的字形渲染成图集资产、走 `Font` 的 `m_CharacterRects` 等）。
//
// ⚠️ 画布数组按 `Texture2D.SetPixels32` 的顺序理解：**row 0 在底部，往上递增**。
//    对外接口的 `yTop` 一律是「从画布顶部往下」，内部翻过来。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class TextCanvas
    {
        /// <summary>字库的一个字：5 列 × 7 行，'1' 是墨</summary>
        const int GlyphW = 5, GlyphH = 7;

        /// <summary>点阵字库。只做 ASCII —— 见文件头「汉字渲染」那条。</summary>
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            { ' ', "00000 00000 00000 00000 00000 00000 00000" },
            { '0', "01110 10001 10011 10101 11001 10001 01110" },
            { '1', "00100 01100 00100 00100 00100 00100 01110" },
            { '2', "01110 10001 00001 00010 00100 01000 11111" },
            { '3', "11111 00010 00100 00010 00001 10001 01110" },
            { '4', "00010 00110 01010 10010 11111 00010 00010" },
            { '5', "11111 10000 11110 00001 00001 10001 01110" },
            { '6', "00110 01000 10000 11110 10001 10001 01110" },
            { '7', "11111 00001 00010 00100 01000 01000 01000" },
            { '8', "01110 10001 10001 01110 10001 10001 01110" },
            { '9', "01110 10001 10001 01111 00001 00010 01100" },
            { 'A', "01110 10001 10001 11111 10001 10001 10001" },
            { 'B', "11110 10001 10001 11110 10001 10001 11110" },
            { 'C', "01110 10001 10000 10000 10000 10001 01110" },
            { 'D', "11110 10001 10001 10001 10001 10001 11110" },
            { 'E', "11111 10000 10000 11110 10000 10000 11111" },
            { 'F', "11111 10000 10000 11110 10000 10000 10000" },
            { 'G', "01110 10001 10000 10111 10001 10001 01111" },
            { 'H', "10001 10001 10001 11111 10001 10001 10001" },
            { 'I', "01110 00100 00100 00100 00100 00100 01110" },
            { 'J', "00111 00010 00010 00010 00010 10010 01100" },
            { 'K', "10001 10010 10100 11000 10100 10010 10001" },
            { 'L', "10000 10000 10000 10000 10000 10000 11111" },
            { 'M', "10001 11011 10101 10101 10001 10001 10001" },
            { 'N', "10001 11001 10101 10011 10001 10001 10001" },
            { 'O', "01110 10001 10001 10001 10001 10001 01110" },
            { 'P', "11110 10001 10001 11110 10000 10000 10000" },
            { 'Q', "01110 10001 10001 10001 10101 10010 01101" },
            { 'R', "11110 10001 10001 11110 10100 10010 10001" },
            { 'S', "01111 10000 10000 01110 00001 00001 11110" },
            { 'T', "11111 00100 00100 00100 00100 00100 00100" },
            { 'U', "10001 10001 10001 10001 10001 10001 01110" },
            { 'V', "10001 10001 10001 10001 10001 01010 00100" },
            { 'W', "10001 10001 10001 10101 10101 11011 10001" },
            { 'X', "10001 10001 01010 00100 01010 10001 10001" },
            { 'Y', "10001 10001 01010 00100 00100 00100 00100" },
            { 'Z', "11111 00001 00010 00100 01000 10000 11111" },
            { '-', "00000 00000 00000 11111 00000 00000 00000" },
            { '_', "00000 00000 00000 00000 00000 00000 11111" },
            { '+', "00000 00100 00100 11111 00100 00100 00000" },
            { '/', "00001 00010 00010 00100 01000 01000 10000" },
            { '\\',"10000 01000 01000 00100 00010 00010 00001" },
            { ':', "00000 00100 00000 00000 00000 00100 00000" },
            { '.', "00000 00000 00000 00000 00000 01100 01100" },
            { ',', "00000 00000 00000 00000 01100 00100 01000" },
            { '!', "00100 00100 00100 00100 00100 00000 00100" },
            { '?', "01110 10001 00001 00010 00100 00000 00100" },
            { '(', "00010 00100 01000 01000 01000 00100 00010" },
            { ')', "01000 00100 00010 00010 00010 00100 01000" },
            { '%', "11001 11010 00010 00100 01000 01011 10011" },
            { '*', "00000 10101 01110 11111 01110 10101 00000" },
            { '=', "00000 00000 11111 00000 11111 00000 00000" },
            { '<', "00010 00100 01000 10000 01000 00100 00010" },
            { '>', "01000 00100 00010 00001 00010 00100 01000" },
        };

        const int Tracking = 1;      // 字间距（点阵格）

        /// <summary>画布像素宽 / 点阵像素的放大倍数</summary>
        public const int DefaultScale = 2;

        /// <summary>画一行文字要多宽（像素）</summary>
        public static int Measure(string text, int scale = DefaultScale)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.Length * (GlyphW + Tracking) * scale - Tracking * scale;
        }

        /// <summary>一行文字有多高（像素）</summary>
        public static int LineHeight(int scale = DefaultScale) { return GlyphH * scale; }

        /// <summary>
        /// 画一行 ASCII 文字。`x` 是左边界，`yTop` 是**从画布顶部往下**数的那一行。
        /// 返回画完之后的 x。
        /// </summary>
        public static int Draw(Color32[] canvas, int W, int H, string text, int scale,
                               int x, int yTop, Color32 color)
        {
            if (string.IsNullOrEmpty(canvas == null ? null : text)) return x;

            int pen = x;
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);
                string rows;
                if (!Glyphs.TryGetValue(c, out rows)) { pen += (GlyphW + Tracking) * scale; continue; }

                var parts = rows.Split(' ');
                for (int gy = 0; gy < GlyphH; gy++)              // gy = 0 是字形**顶**行
                {
                    int canvasY = H - 1 - (yTop + gy * scale);   // 画布 row 0 在底部
                    var line = parts[gy];
                    for (int gx = 0; gx < GlyphW; gx++)
                    {
                        if (line[gx] != '1') continue;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            int cy = canvasY - dy;
                            if (cy < 0 || cy >= H) continue;
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int cx = pen + gx * scale + dx;
                                if (cx < 0 || cx >= W) continue;
                                canvas[cy * W + cx] = color;
                            }
                        }
                    }
                }
                pen += (GlyphW + Tracking) * scale;
            }
            return pen;
        }

        /// <summary>水平居中画一行（以 `x01 * W` 为中心）</summary>
        public static void DrawCentered(Color32[] canvas, int W, int H, string text, int scale,
                                        float x01, int yTop, Color32 color)
        {
            int w = Measure(text, scale);
            Draw(canvas, W, H, text, scale, Mathf.RoundToInt(x01 * W - w * 0.5f), yTop, color);
        }

        /// <summary>
        /// 按宽度自动折行。返回画完之后下一行的 y。
        /// 卡面上的关键词说明用它。
        /// </summary>
        public static int DrawWrapped(Color32[] canvas, int W, int H, string text, int scale,
                                      int x, int yTop, int maxWidth, int lineGap, Color32 color)
        {
            if (string.IsNullOrEmpty(text)) return yTop;

            int y = yTop;
            int i = 0;
            while (i < text.Length && y < H)
            {
                // 贪心地取尽量多的词（英文按空格断；没有空格的硬断）
                int take = 0;
                while (i + take < text.Length)
                {
                    int probe = System.Math.Min(take + 1, text.Length - i);
                    if (Measure(text.Substring(i, probe), scale) > maxWidth && take > 0) break;
                    take = probe;
                }
                if (take == 0) take = 1;

                int cut = take;
                if (i + take < text.Length)
                {
                    int sp = text.LastIndexOf(' ', i + take - 1, take);
                    if (sp >= i) cut = sp - i + 1;
                }

                string line = text.Substring(i, cut).Trim();
                if (line.Length > 0)
                {
                    Draw(canvas, W, H, line, scale, x, y, color);
                    y += GlyphH * scale + lineGap;
                }
                i += cut;
                while (i < text.Length && text[i] == ' ') i++;
            }
            return y;
        }
    }
}
