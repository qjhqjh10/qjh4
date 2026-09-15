// TmpFont.cs — 中文文字的字体入口（TextMeshPro 后端）
//
// 为什么会有这个文件：卡面和 HUD 的文字原来都走 `TextCanvas` 的自写 5×7 点阵字库 ——
// **只支持 ASCII**，所以卡名一直是英文。2026-09-12 查清原版**全量走 TMP**
// （战斗界面 TMP : legacy = 666 : 6），中文字体 `NotoSerifCJK-Regular.ttf` 就在本机解包资源里，
// 于是这条路通了（TMP 的资产由 `TmpSetup.ImportEssentials` 从命令行导，不用手点菜单）。
//
// 走的是 TMP 的**世界空间**组件 `TextMeshPro`，不是 UGUI 那版 `TextMeshProUGUI` ——
// 它本身就摆在 3D 世界里（`TextMeshPro.cs:17` 要 `MeshRenderer`），不需要 Canvas，
// 正好替掉「把字画进 Texture2D 再贴 quad」那套。
//
// 字体资产由 `TmpSetup.BuildCjkFontAsset` 生成，烘焙参数照抄原版那份 SDF（见那个文件）。
// **必须放在 Resources 下**，不然运行时（尤其是打包后）`Resources.Load` 不到。
//
// ⚠️ 拿不到字体资产时 `Available == false`，**所有调用方都要退回点阵字库** ——
//    和 `CardArt.Available` 一个路子：删掉字体资源游戏照样跑，只是没中文。
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    public static class TmpFont
    {
        /// <summary>字体资产在 Resources 下的路径（不带扩展名）。
        /// 和 `TmpSetup.FaPath` 是同一份文件，改一边要改另一边。</summary>
        public const string ResourcePath = "Fonts/NotoSerifCJK-Regular SDF";

        static TMP_FontAsset _font;
        static bool _tried;
        static float _worldPerPoint;      // 汉字字形高度 ÷ fontSize
        static float _capPerPoint;        // 拉丁大写高度 ÷ fontSize

        /// <summary>中文字体资产。没有返回 null（调用方用 <see cref="Available"/> 判）</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (!_tried)
                {
                    _tried = true;
                    _font = Resources.Load<TMP_FontAsset>(ResourcePath);
                    if (_font == null)
                        Debug.LogWarning("[TmpFont] 找不到字体资产 `Resources/" + ResourcePath +
                                         "` —— 中文出不来，退回自写点阵字库。" +
                                         "重建：-executeMethod TmpSetup.BuildCjkFontAsset");
                }
                return _font;
            }
        }

        /// <summary>能不能走 TMP。不能的话调用方自己退回点阵字库（**别静默变成空白**）</summary>
        public static bool Available { get { return Font != null; } }

        /// <summary>
        /// 「一个**汉字**在世界空间里有多高」÷「fontSize」。
        ///
        /// ⚠️ 量的必须是**字面高度**，不是行高。一开始量的是 `preferredHeight`（= 行高），
        ///    结果字小了一半：Noto Serif CJK 的行高是 **1.437 em**（`faceInfo.lineHeight 71.85 /
        ///    pointSize 50`，见解包那份 `NotoSerifCJK-Regular SDF.json`），而汉字只占 **1 em**，
        ///    中间那 0.437 是行距和降部。按行高定字号 = 汉字被压到应有大小的 70%。
        ///    所以这里拿一个汉字实测 `textBounds.size.y`。
        ///
        /// TMP 的字号→世界尺寸换算还跟字体资产的 `pointSize` / `scale` 绑在一起，
        /// **写死常数迟早对不上**，所以拿这个资产实测一次再缓存（一次性的，之后纯读字段）。
        /// </summary>
        public static float WorldGlyphPerFontSize
        {
            get
            {
                if (_worldPerPoint <= 0f) _worldPerPoint = MeasureGlyph('国', "汉字");
                return _worldPerPoint;
            }
        }

        /// <summary>
        /// 「一个**拉丁大写字母**有多高」÷「fontSize」。
        /// HUD 原来跑的是 5×7 点阵**大写 ASCII** —— 想让它看着和以前一样大，
        /// 就得按**大写高度**对齐（按汉字高度对的话，拉丁只有 0.72 em，会小 28%）。
        /// </summary>
        public static float WorldCapPerFontSize
        {
            get
            {
                if (_capPerPoint <= 0f) _capPerPoint = MeasureGlyph('M', "拉丁大写");
                return _capPerPoint;
            }
        }

        /// <summary>要让一个汉字占 `worldHeight` 个世界单位高，fontSize 该给多少。
        /// （拉丁字母会略矮 —— 拉丁大写高约 0.72 em，汉字 1 em。不是 bug。）</summary>
        public static float FontSizeForGlyphHeight(float worldHeight)
        {
            return worldHeight / WorldGlyphPerFontSize;
        }

        /// <summary>要让一个大写字母占 `worldHeight` 个世界单位高，fontSize 该给多少。HUD 用它</summary>
        public static float FontSizeForCapHeight(float worldHeight)
        {
            return worldHeight / WorldCapPerFontSize;
        }

        /// <summary>
        /// 造一个字。拿不到字体资产返回 null（调用方要判）。
        /// 参数只留最常用的几个 —— 位置/换行宽度/字号回缩由调用方按自己的版面调。
        /// </summary>
        public static TextMeshPro NewText(Transform parent, string name, string text, float fontSize, Color color)
        {
            if (!Available) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<TextMeshPro>();
            t.font = Font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            // 🔴 挂上卡面图标的 sprite asset —— **不设它的话 `<sprite name=…>` 会当字面量印出来**。
            //    挂在这里（全工程建 TMP 的唯一一处）而不是各个调用方，是因为效果文字会流到
            //    卡面 / 放大展示窗 / 探针好几处，分散着设迟早漏一处、变成「有的地方有图标有的地方没有」。
            //    取不到时 `CardIcons.SpriteAsset` 是 null —— TMP 保持自己的默认值，不静默变空白。
            if (CardIcons.SpriteAsset != null) t.spriteAsset = CardIcons.SpriteAsset;
            // 默认不换行：**一次要显示完的**（卡名、数值）不该被版面宽度切两行。
            // 需要折行的（关键词）调用方自己改成 `Normal` 并给 rect 宽度，见 `SetWrapWidth`。
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = text ?? "";

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            return t;
        }

        /// <summary>
        /// 给一个 TMP 设折行宽度（世界单位）。
        /// ⚠️ `ComputeMarginSize()` **只在 `OnEnable` / `GetTextInfo` / `OnValidate` 里跑**
        ///    （`TextMeshPro.cs:663/367/749`），而我们是 `AddComponent` **之后**才改
        ///    `sizeDelta` 的 —— 那会儿它已经按默认宽算过一次了。所以这里显式走一趟
        ///    `GetTextInfo` 逼它重算，不然换了宽度也不折行。
        /// </summary>
        public static void SetWrapWidth(TextMeshPro t, float width)
        {
            if (t == null) return;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = new Vector2(width, 0f);
            t.GetTextInfo(t.text);
        }

        /// <summary>
        /// 量一个字符的**字形高度**（世界单位）÷ 基准字号 100。
        ///
        /// ⚠️ 量的是**字形四边形的上下边**，不是 `textBounds.size.y`。
        ///    `textBounds` 给的是**行盒**（含行距和降部）—— 实测「国」在 fontSize=100 时
        ///    `textBounds.size.y = 14.37`，而它只是行高：Noto Serif CJK 的
        ///    `lineHeight/pointSize = 71.85/50 = 1.437`，汉字本身只占 1 em。
        ///    按行盒定字号，汉字会被压到应有大小的 ~70%。
        ///    实测同一个字：行盒 0.1437 / 字形 0.0948。
        /// </summary>
        static float MeasureGlyph(char probe, string what)
        {
            if (!Available) return 1f;

            var go = new GameObject("TmpFontMeasure") { hideFlags = HideFlags.HideAndDontSave };
            var t = go.AddComponent<TextMeshPro>();
            t.font = Font;
            t.fontSize = 100f;                    // 基准字号，量完按比例换算
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = probe.ToString();
            t.ForceMeshUpdate();

            float ratio = 0f;
            var info = t.textInfo;
            if (info != null && info.characterCount > 0)
            {
                var ci = info.characterInfo[0];
                ratio = Mathf.Abs(ci.topLeft.y - ci.bottomLeft.y) / 100f;
            }
            Kill(go);

            if (!(ratio > 0f))
            {
                // 量不出来也不能让字变成 0 号 —— 宁可字号偏，也别整段不显示
                Debug.LogWarning("[TmpFont] 量不出「" + what + "」的字形高度（characterInfo 为空），字号换算退回兜底值。");
                ratio = 0.01f;
            }
            return ratio;
        }

        /// <summary>批处理下没有帧循环，`Destroy` 不生效 —— 见 CLAUDE.md</summary>
        internal static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
