// TmpFont.cs — 中文文字的字体入口（TextMeshPro 后端）
//
// 为什么会有这个文件：卡面和 HUD 的文字原来都走 `TextCanvas` 的自写 5×7 点阵字库 ——
// **只支持 ASCII**，所以卡名一直是英文。2026-09-12 查清原版**全量走 TMP**
// （战斗界面 TMP : legacy = 666 : 6），中文字体 `NotoSerifCJK-Regular.ttf` 就在本机解包资源里，
// 于是这条路通了（TMP 的资产由 `TmpSetup.ImportEssentials` 从命令行导，不用手点菜单）。
//
// 🔴 **2026-10-18（`A1024`）补记 —— 这不是「我们换了字体」，是「全仓只有一个字体」**：
//   原账一度把「多处渲染值与原版不同（如左栏页签字号 `D19`）」写成「**根因 = 我们换了字体**」。
//   **那句话是错的**，现核（只读查证 → `资料/普查产出_第四会话/查证_探针覆盖率与字体口径.md` §3）：
//     · **我们【没有换过】字体** —— 是从「自写点阵（只支持 ASCII）」**新增**了 TMP 这条路（见上段）。
//       `NotoSerifCJK` 是**当时就有**的那一个，从来没有「原来用 A、后来换成 B」这回事
//       ⇒ ⛔ **别再把它写成「换字体」**（下个会话会以为是我们随手挑的，与事实不符）。
//     · 真正的根因 = **全仓只有一个字体** —— 现读给 TMP 赋字体的**只有 4 处**
//       （本文件的 `Font` 属性 · `MeasureGlyph` 里那句 `t.font = Font` ·
//        `Editor/{IconSetup,IconSizeProbe,Round1015Probe}` 三个探针），**全部 = `NotoSerifCJK`**
//       ⇒ **拉丁文本（页签、按钮、卡面英文）也用它渲染**；而原版拉丁走的是另外三种
//       （名字出处见 `ApplyOutline` 上方那段从 `bundle_fonts_assets_all/Material/` 读出的清单）：
//       `Asar-Regular`（正文/UI）· `Pragati-Regular`（卡面效果 / 兵种）· `RobotoCondensed-Regular`。
//
//   📌 **判据是可取的 —— 原版那三份 font asset 与烘好的图集都在本地**：
//      `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/` 的
//      `MonoBehaviour/{Asar-Regular SDF, Pragati-Regular SDF, RobotoCondensed-Regular SDF}.json`
//      + `Texture2D/* Atlas.png`（1024² · 1024×2048 · 512²）；三份都是 `m_AtlasPopulationMode = 0`
//      （**Static、字形已烘死**）、`m_SourceFontFile = null`
//      ⇒ **源 TTF 全库找不到，但不必找：JSON + 图集就够重建**（这正是原版的做法）。
//      ⚠️ 只有 `NotoSerifCJK-Regular SDF` 那份是 Dynamic（`= 1`）、图集是 **1×1 占位** ——
//         所以汉字只能自己从 TTF 生成（见 `TmpSetup` 头那段「像原版 = 同样的参数 + 同样的 Dynamic 模式」）。
//
//   🚧 **`A1024` 的三步：【①② 已做完】、③ 没做（⛔ 但别因此以为字体已经换过了 —— 见下面 ③）**：
//      ① ✅ **判据路径已记**（`资料/待办判据_第四会话.md` §A1024；上面这两段就是它的代码侧副本）；
//      ② ✅ **2026-10-14（第二十九会话）现核：这一条【其实早就做完了】，本行原来的「⛔ 没做」是过期记录。**
//         三份都在盘上、且**结构完整**（逐项现读）：`Resources/Fonts/` 下
//         **`Pragati-Regular SDF.asset`**（250 个字形 · `m_AtlasPopulationMode = 0` Static ·
//         `m_AtlasWidth/Height = 1024×2048` · **`m_PointSize = 95`（原版就是 95）** ·
//         `m_UsedGlyphRects` 非空）· **`Asar-Regular SDF.asset`**（250 · Static · 1024×1024 · **94**（原版 94））·
//         **`RobotoCondensed-Regular SDF.asset`**（304 · Static · 512×512 · 35）。
//         **三份的 `m_AtlasTextures` guid 全都解得开**（Pragati/Asar 的图集在 `WarpforgeVFX/Textures/`、
//         RobotoCondensed 的在 `Resources/Fonts/`，另有配套 `… Atlas Material.mat`）。
//         ⇒ 来路 = `git log --diff-filter=A` 现读 **`9998ebd`（第十三会话）**，此后没再动过。
//         ⚠️ **仍没验的是「`Resources.Load` 拿得到、且渲得对」** —— 那要跑一次 Unity（见 ③ 的验收办法）。
//         ⚠️ 原来这一行还挂着「`Asar Regular White` 等 58 个 Material 要不要一并复刻、
//            `ApplyOutline` 的 `_OutlineWidth/_Underlay*` 会不会被覆盖」—— **这两条仍未查**，挪到 ③ 一起办。
//      ③ 🟡 **2026-10-17：fallback 那一半已接（运行期，⛔ 不落盘）** —— `EnsureCjkFallback()` 在某一份
//         拉丁面第一次被取用时，把 CJK 那一份挂进它的 `fallbackFontAssetTable`（**内存里改**，
//         `Resources/Fonts/*.asset` 一个字节都不动）⇒ 混排文本里的汉字走 TMP 原生
//         `SearchFallbacksForCharacter`，不再画 `.notdef` 方框。
//         · **仍未做的是「按用途选字体」那一半**：`NewText` 里 `t.font = Font` 仍写死 CJK
//           （`Font` 属性 / `ResourcePath` 没改）⇒ 今天全仓仍然是「一个 Noto 打天下」，
//           `Face.LatinUi / LatinCardBody / LatinNarrow` **零消费者**（仓规：逐件换、逐件验收）。
//           🔴 **2026-10-18（`N55`）就地订正（铁律 5）**：这两句**已过期** —— 现读
//           `grep -rn "Face\.Latin" --include=*.cs` = **486 处 / 24 处在 `Editor/`**（生产侧 462 处、85 个文件）
//           ⇒ 「零消费者」「一个 Noto 打天下」**都不成立**。`NewText` 的默认仍是 CJK（逐位不变），
//           但调用点可以 `SetFace` / `MenuDraw.Text(..., face:)` 显式换面。
//         · `TMP Settings` 那条**全局** fallback 路没走：`TMP_Settings.fallbackFontAssets` 没有公开
//           setter（只有 getter 直通序列化字段），反射硬改会绕过「别弄脏资产」那条纪律。
//      ⇒ 今天全仓仍然是「**一个 Noto 打天下**」。
//      🔴 **2026-10-18（`N55`）**：本句同属**已过期**（现读 462 处生产站点显式换面）——
//         见上面 `③` 那条订正，⛔ 别按这句判断现状。
//      ⚠️ **换的时候必须一起重量**：下面「字形高度 ÷ fontSize」那套换算
//         （`WorldGlyphPerFontSize` / `WorldCapPerFontSize`，是**按字体标定**的）⇒ 换字体会**牵动全仓版面**
//         （页签、按钮、卡面都会位移）⇒ 已裁：**逐件换、逐件验收**，别一次全开。
//         🔴 **2026-10-18（`N55`）补**：`EmPerFontSize` 自本日起**按面分两支**
//         （`Face.Cjk` = 汉字墨高 / 拉丁面 = 原版真 em 0.1）——
//         换面的版面后果从「随面变 9–11%」收紧到「拉丁面按原版 em、CJK 基线不动」。
//         🔴 **2026-10-21（`A1262`）就地订正（铁律 5）**：上面那半句原来写
//            「`Face.Cjk` = 汉字墨高 **0.0948 逐位不变**」—— **0.0948 只是 `m_Scale = 1` 那一档的读数**。
//            原版那份 CJK 资产的 `m_FaceInfo.m_Scale` = **0.9**；我们这一份 2026-10-21 起也对齐成 0.9
//            ⇒ 这个**实测值**会跟着变（≈ **0.0853** = 0.0948 × 0.9）。
//            ✅ **不变的是口径**（汉字墨高 ÷ fontSize，运行时现测），⛔ 不是那个数字。
//            ⚠️ 关键推论：**一切「按 px 要一个字形高」的站点实渲逐位不变** —— px→fontSize 与
//            fontSize→px 用的是同一个实测比值，而 TMP 的行盒/字形**同时**乘 `m_Scale`
//            ⇒ 比值 `行盒 ÷ 字形` 与 `m_Scale` 无关（= 1.437 ÷ 0.948 = **1.516**，正是那四条 1.52 读数）。
//            变的只是内部那个 TMP `fontSize` 数字（× 1/0.9）。判据/影响面 →
//            `资料/普查产出_1021/W61b_界面_A1262_mScale.md`。
//         ⚠️ 中文那一侧**判据本来就空**（原版客户端没有中文表）⇒ **只有拉丁这一侧可逐字对**。
//            🔴 **2026-10-14 更正（铁律 5）**：这句的**前提不成立** —— 原版**有**官方中文表（9,440/9,447
//               条非空），由**远端 Addressables 包 `localization_assets_all`** 下发，**2026-10-14 已解开**
//               并落至 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`。**错因** = 把「不在离线包体里」
//               读成了「根本没有」。判据全文 → `CLAUDE.md` §一·7 的「2026-10-14 更正」段。
//               ⇒ **「中文那一侧判据本来就空」不再成立**，两半都要重看：
//                 · **文本层**：官方 zh-CN 词条已在手（`Card_Description/*` 还是 I2 占位符模板、
//                   要等 `Core/CardIcons` 的 `[[…]]`/`((…))` 记号层，见 `A1763`）；
//                 · **字形 / 版面层（本段真正在讲的那件事）**：原版**确实在运行期渲过中文** ——
//                   实据 = 2026-09-14 的运行期 dump（`资料/原版参照图/Unity参照管线_0825/data/
//                   panel_0914b/p3_mulligan_tree.tsv` 的 `:15`/`:13`，即 `A1103` 那一笔用的那份）
//                   ⇒ **中文侧不再是「无判据」**，可以拿原版实拍逐处对（原先那句「只有拉丁这一侧可逐字对」
//                   是**当时**的实情，现在**待重核**）。
//               ⚠️ **本段其余结论不变**（换字体牵动全仓版面、逐件换逐件验收）。
//               ⚠️ 订正只到措辞：**这两半各自怎么重核 = 另记的账**（本笔没动任何换算常量）。
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

        // ============================================================================================
        // 🆕 **2026-10-14（第二十九会话 · 账 `A1024` 的第 ③ 步）—— 原版的【按用途选字体】。**
        //
        // **为什么要有这一段**：原版给不同的文字用**不同的字体资产**（出处 =
        //   `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/Material/` 的材质名 —— 名字本身就写着用途：
        //   `Asar-Regular White-Card name` / `Pragati-Regular Card Content` / `Pragati-Regular Card Unit type`）：
        //     · **`Asar-Regular`**   —— 正文 / UI（卡名也用它）
        //     · **`Pragati-Regular`** —— 卡面效果文字 / 兵种 / **数字**
        //     · **`RobotoCondensed-Regular`** —— 另一档窄体
        //   而我们今天全仓**只有一个 `NotoSerifCJK`**（给 TMP 赋字体的只有 `Font` 这一个属性 + 三个探针）
        //   ⇒ **拉丁文本（页签、按钮、卡面英文、数字）也在用汉字字体渲**。
        //
        // 🔴 **它造成的真实后果（账 `A1927` 就是这么暴露的）**：三份拉丁字体的**字形高度其实差不多**
        //   （实测「拉丁大写高 ÷ fontSize」`Asar 0.0685` / `Pragati 0.0656` / `Noto 0.0779（2026-10-17 订正：原写 ≈0.0683 是 em 口径推算、非实测）`），
        //   **差得多的是【行盒】**：`Pragati 0.9474 em` ↔ `Noto 1.437 em`（+ 下面那条 2026-10-21 的补注）。
        //   🔴 **2026-10-21（`A1262`）就地补**：那个 `1.437` 是 Noto 的 **face 行盒**（`lineHeight ÷ pointSize`）；
        //   原版那份 Noto 资产带 `m_FaceInfo.m_Scale = 0.9` ⇒ **有效行盒 = 1.437 × 0.9 = 1.2933 em**
        //   （我们 2026-10-21 起也对齐了）。⚠️ 下面那句「`1.437×50 = 71.85px > 框 47.88px`」的**结论不变**：
        //   两边同时缩 10%（`71.85 × 0.9 = 64.7 > 47.88`），照样装不下。
        //   而 TMP 的**自适应是按行盒判「装不装得下」**的 ⇒ 卡组左栏那颗费用数字：
        //   原版（Pragati，`em≈50px` ⇒ 行盒 `47.4px`）**装得下、停在 `m_fontSizeMax = 50`**；
        //   我们（Noto ⇒ 行盒 `1.437×50 = 71.85px > 框 47.88px`）**必然被压** ⇒ 实测只到 **3.05**。
        //
        // 🔴 **口径：本步【零行为变化】** —— `Face.Cjk` 就是今天那条路（`ResourcePath` / `Font` / 现有那个
        //   `WorldGlyphPerFontSize` 缓存**一个字都没动**），新入口一律**显式传**才生效。
        //   仓规已裁：**逐件换、逐件验收**（换字体会牵动全仓版面）⇒ ⛔ 不许一把全开。
        //
        // ⚠️ **汉字的兜底：那三份拉丁资产【没有汉字字形】**（实测 `'国'` 量出来的是 `.notdef` 方框，
        //   `Asar`/`Pragati` 都是 `0.0622`）⇒ 本工程的做法是**按文本判**：
        //   一个 Label 的文本里**只要有汉字，就用 CJK 那面**（见 `Label.ActualFace`），
        //   ⛔ **不去改那三份资产的 `fallbackFontAssetTable`** —— 那会**静默弄脏 git 里跟踪的 `.asset`**
        //   （自检宿主会 `SaveAssets`），而且原版那三份本来就是「只有拉丁」的 Static 资产。
        //   ⇒ 「接 TMP 原生 fallback 链」这一半**仍未做**，如实记在账上。
        // ============================================================================================

        /// <summary>按用途选字体（见上面那一段）。**默认 `Cjk` = 今天的行为**。</summary>
        public enum Face
        {
            Cjk = 0,            // NotoSerifCJK —— 今天全仓走的那一份（汉字/拉丁都用它）
            LatinUi,            // Asar-Regular      —— 原版：正文 / UI / 卡名
            LatinCardBody,      // Pragati-Regular   —— 原版：卡面效果文字 / 兵种 / **数字**
            LatinNarrow,        // RobotoCondensed-Regular
        }

        static readonly string[] FaceResource =
        {
            "Fonts/NotoSerifCJK-Regular SDF",
            "Fonts/Asar-Regular SDF",
            "Fonts/Pragati-Regular SDF",
            "Fonts/RobotoCondensed-Regular SDF",
        };

        static readonly TMP_FontAsset[] _faceAsset = new TMP_FontAsset[4];

        /// <summary>取某一面的字体资产。**取不到就退回 CJK 那一份并出声**（⛔ 不返回 null、不静默变空白）。</summary>
        public static TMP_FontAsset FaceAsset(Face f)
        {
            int i = (int)f;
            if (_faceAsset[i] != null) return _faceAsset[i];
            var a = Resources.Load<TMP_FontAsset>(FaceResource[i]);
            if (a == null)
            {
                if (f == Face.Cjk) return null;                  // CJK 那份取不到 = 今天那条路的既有症状，由 `Font` 出声
                Debug.LogWarning("[TmpFont] 按用途选字体：`Resources/" + FaceResource[i] +
                                 "` 取不到 ⇒ **退回 CJK 那一份**（字形高度换算跟着 CJK 走，版面会与预期不同）");
                a = FaceAsset(Face.Cjk);
                if (a == null) return null;
            }
            _faceAsset[i] = a;
            if (f != Face.Cjk) EnsureCjkFallback();     // 🆕 2026-10-17（A1024③）：拉丁面挂 CJK 兜底
            return a;
        }

        static bool _cjkFallbackHooked;

        /// <summary>🆕 **2026-10-17（账 `A1024` ③）**：把 CJK 那一份挂成三份拉丁资产的**运行期** fallback。
        ///
        /// <para>原版做法 = 拉丁面渲拉丁、汉字由 CJK 兜底（TMP 原生 `fallbackFontAssetTable` +
        /// `SearchFallbacksForCharacter`）。此前「表内 fallback」与「`TMP Settings` 全局 fallback」
        /// **两条都是空的** ⇒ 混排文本（例 `Deck 1 张`）里的汉字会画成 `.notdef` 方框。</para>
        ///
        /// <para>🔴 **只改内存，⛔ 不落盘**：`Resources.Load` 拿到的是资产实例，往
        /// `fallbackFontAssetTable` 里加一份**不会**写回 `Resources/Fonts/*.asset`
        /// （不 `SetDirty` / 不 `SaveAssets`）⇒ 不会弄脏 git 里跟踪的资产 —— 这正是本文件头
        /// 「⛔ 不去改那三份资产的 fallback 表」那条纪律允许的那条路（它禁的是**改资产**，不是禁这条链）。</para>
        ///
        /// <para>⚠️ **懒挂 + 只挂一次**：只有真的取用某一份拉丁面（`FaceAsset(f != Cjk)`）时才执行；
        /// `Face.Cjk` 那条路（今天全仓唯一在用的）**逐位不变**。
        /// ⚠️ 不走 `TMP_Settings.fallbackFontAssets`：它**没有公开 setter**（只有 getter 直通序列化字段），
        /// 反射硬改会绕过上面那条「别弄脏资产」的纪律。</para></summary>
        public static void EnsureCjkFallback()
        {
            if (_cjkFallbackHooked) return;
            _cjkFallbackHooked = true;          // 🔴 先置位：下面会回调 `FaceAsset`，防重入
            var cjk = FaceAsset(Face.Cjk);
            if (cjk == null) return;            // 没中文资产 ⇒ 维持今天的行为（`Font` 那条会出声）
            for (int i = 1; i < FaceResource.Length; i++)   // 0 = Cjk 自己，跳过
            {
                var a = FaceAsset((Face)i);
                if (a == null || a == cjk) continue;        // 取不到时 `FaceAsset` 已退回 CJK 并出声
                var tbl = a.fallbackFontAssetTable;
                if (tbl == null)
                {
                    tbl = new System.Collections.Generic.List<TMP_FontAsset>();
                    a.fallbackFontAssetTable = tbl;
                }
                if (!tbl.Contains(cjk)) tbl.Add(cjk);
            }
        }

        /// <summary>「**1 em** 在世界空间里有多高」÷「fontSize」—— 这是本工程 px 口径的基准
        /// （`Label.FontSizeToPx` = `fontSize × 本值 × 108`）。
        ///
        /// 🔴🔴 **2026-10-18（账 `N55` · 用户 2026-10-17 拍板的方案 B）：按面分两支 ——
        ///   `Face.Cjk` 仍走汉字墨高（**逐位不变**），三个拉丁面走【原版真 em = 0.1 × fontSize】。**
        ///   · **为什么拉丁面是 0.1**：TMP 的 3D 换算 `fontScale = fontSize / pointSize × scale × 0.1`
        ///     （`TMP_Text.cs:7183`；本机那**三份拉丁**资产 `m_FaceInfo.m_Scale` **是 1**，2026-10-18 现读）
        ///     ⇒ **1 em = 0.1 × fontSize，与字体无关**。原来拿汉字墨高 0.0948 当拉丁面的 em
        ///     ⇒ 实渲 em = `0.1 ÷ 0.0948` = **1.052 × 名义 px**（实测 42.19 vs 原版 `m_fontSize 40`）。
        ///     🔴 **2026-10-21（`A1262`）就地订正**：原来这里写「**本机 4 份**资产全是 1」——
        ///       **对 CJK 那一份不再成立**：它的 `m_Scale` 已按原版对齐成 **0.9**（有效行盒 1.437 × 0.9）。
        ///       本常量 `TrueEmPerFontSize` **只服务那三个拉丁面**（`EmPerFontSize` 的 `f != Cjk` 那一支）
        ///       ⇒ 0.1 本身**仍然正确**；⛔ 别把 CJK 也接到它上面（那会少乘 0.9、行盒凭空高 11%）。
        ///   · **`Face.Cjk` 走的是实测墨高**（= `WorldGlyphPerFontSize`；`m_Scale = 1` 时读数 0.0948，
        ///     对齐成 0.9 后 ≈ **0.0853**）—— **口径**不变，⛔ 别把 0.0948 当常数抄走。
        ///   · ⚠️ `CapPerFontSize` **仍然按面**（它量的是拉丁大写：Asar 0.0685 / Pragati 0.0656 / Noto 0.0779）。
        ///   · 判据全文 → `资料/普查产出_1017/诊断_第四十八会话/R4_Rewards残差.md` §五·B ＋
        ///     `D1_桶A字号换算链.md` §五·2。
        ///
        /// 📌 **历史（⛔ 别照旧口径办事）**：2026-10-17 第四十八会话「桶 A」曾把这一支改成
        ///   **与面无关**（恒 0.0948），用来修 `ActualFace` 翻面引起的 ×0.912/×0.898 —— 那是**当时**的修法；
        ///   用户 2026-10-17 已改判「拉丁面按原版真 em」，2026-10-18 再次点名先做 `T07`。
        ///   ⚠️ 那次历史里量到的 `'M' ÷ capLine/pointSize`（Pragati 0.103906 / Asar 0.105507）是
        ///   **大写墨高 ÷ em**，**不是 em** ⇒ ⛔ 别再拿它当 em。
        /// ⚠️ 不能直接量 `'国'` 给拉丁面（那三份量出来是 `.notdef` 方框 0.0622）。</summary>
        public static float EmPerFontSize(Face f)
        {
            if (f == Face.Cjk) return WorldGlyphPerFontSize;   // 汉字墨高口径（全仓基线，逐位不变）
            return TrueEmPerFontSize;                          // 拉丁面：原版真 em
        }

        /// <summary>**原版真 em ÷ fontSize** = `0.1 × faceInfo.scale`。
        /// 本机**那三份拉丁**字体资产（Asar / Pragati / RobotoCondensed，`Resources/Fonts/*.asset`
        /// 的 `m_FaceInfo.m_Scale`）**是 1**（2026-10-18 现读）⇒ 恒 0.1。
        /// 🔴 **2026-10-21（`A1262`）就地订正**：原文写「**本机 4 份**……全是 1」——
        ///   **CJK 那一份已经不是 1 了**（按原版对齐成 **0.9**，有效行盒 1.437 × 0.9 = 1.2933 em）。
        ///   本常量只服务拉丁面 ⇒ 值不变；但 ⛔ 别再引用成「四份都是 1」。
        /// 判据 = TMP 的 `fontScale = fontSize / pointSize × scale × 0.1`（`TMP_Text.cs:7183`）。
        /// ⚠️ 只有 `Face.Cjk` 不走它（Cjk 那一支的「px」是**汉字墨高**，不是 em；读数随 `m_Scale` 变）。</summary>
        public const float TrueEmPerFontSize = 0.1f;

        /// <summary>要让 1 em 占 `worldHeight` 个世界单位高，fontSize 该给多少（按面折算）。</summary>
        public static float FontSizeForEm(Face f, float worldHeight)
        {
            float e = EmPerFontSize(f);
            return e > 0f ? worldHeight / e : 0f;
        }

        static readonly float[] _faceCap = new float[4];

        /// <summary>「一个**拉丁大写字母**有多高 ÷ fontSize」，**按面**。`Face.Cjk` 这一支返回既有缓存值
        /// （`WorldCapPerFontSize`）⇒ 零行为变化。
        /// ⚠️ 三份拉丁字体这一项实测**很接近**（`Pragati 0.0656` · `Asar 0.0685` · `Noto 0.0779（2026-10-17 订正：原写 ≈0.0683 是 em 口径推算、非实测）`）
        ///   ⇒ **换面本身几乎不改变字形大小**；真正会变的量在**行盒**。
        /// 🔴 **2026-10-18（`N55`）就地补注**：这句说的是**本函数（大写墨高）**，⛔ 别当成 `EmPerFontSize` ——
        ///   `EmPerFontSize` 从 2026-10-18 起**按面分两支**（Cjk 汉字墨高 0.0948 / 拉丁面原版真 em 0.1），
        ///   那一支**确实随面变**（判据 → `EmPerFontSize` 的 doc）。</summary>
        public static float CapPerFontSize(Face f)
        {
            if (f == Face.Cjk) return WorldCapPerFontSize;
            int i = (int)f;
            if (_faceCap[i] > 0f) return _faceCap[i];
            var a = FaceAsset(f);
            if (a == null) return WorldCapPerFontSize;
            _faceCap[i] = MeasureGlyph(a, 'M', "拉丁大写（" + f + "）");
            return _faceCap[i];
        }

        /// <summary>
        /// 「一个**汉字**在世界空间里有多高」÷「fontSize」。
        ///
        /// ⚠️ 量的必须是**字面高度**，不是行高。一开始量的是 `preferredHeight`（= 行高），
        ///    结果字小了一半：Noto Serif CJK 的 **face 行盒**是 **1.437 em**（`faceInfo.lineHeight 71.85 /
        ///    pointSize 50`，见解包那份 `NotoSerifCJK-Regular SDF.json`），而汉字只占 **1 em**，
        ///    中间那 0.437 是行距和降部。按行高定字号 = 汉字被压到应有大小的 70%。
        ///    所以这里拿一个汉字实测 `textBounds.size.y`。
        /// 🔴 **2026-10-21（`A1262`）就地订正（铁律 5）**：上面那个 1.437 是 **face 行盒**（`lineHeight ÷ pointSize`）。
        ///    原版那份资产的 `m_FaceInfo.m_Scale = 0.9`（我们 2026-10-21 起也对齐了）⇒
        ///    **有效行盒 = face 行盒 × `m_Scale` = 1.437 × 0.9 = 1.2933 em**。
        ///    ⚠️ 本属性量的是**字形高**（∝ `m_Scale`），所以**它自己的读数也会跟着变**
        ///    （`m_Scale = 1` 时 0.0948，对齐后 ≈ 0.0853）—— ✅ 口径不变、数字随资产走。
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
        /// 卡面文字的**描边/阴影** —— 🔴 原版这些**不在 TMP 组件上，在材质里**
        /// （组件 JSON 的键里没有 `m_outlineWidth`/`m_outlineColor`/underlay 任何一个，
        ///  所以只读组件的文档才会得出「原版卡名/desc 无描边」这个**错结论**）。
        ///
        /// **出处**：`d:/2/新解包资源/assets_full/bundle_fonts_assets_all/Material/`，
        /// 逐个材质读 `_OutlineWidth` / `_OutlineColor` / `_Underlay*` / `_FaceDilate`
        /// （shader `TextMeshPro/Distance Field`）：
        ///   · 卡名 `Asar-Regular White-Card name` —— `_OutlineWidth` **0.15** 黑、
        ///     `_FaceDilate` 0.35、`_UnderlayColor` 黑、`_UnderlayDilate` **1.0**、`_UnderlaySoftness` **0.578**
        ///     （= 粗黑描边 + 一团很软的暗影）
        ///   · 效果文字 `Pragati-Regular Card Content` —— `_OutlineWidth` **0.05** 黑
        ///   · 兵种/阵营 `Pragati-Regular Card Unit type` —— `_OutlineWidth` **0.05** 黑
        ///
        /// ⚠️ **必须拿实例材质**（`fontMaterial` 会给这一份 TMP 单独造一个）——
        ///    改共享材质会让**全工程所有卡**一起变（这个坑在 `AddLayer` 也踩过一次）。
        /// </summary>
        public enum TextOutline { None, Name, Body }

        public static void ApplyOutline(TMP_Text t, TextOutline kind)
        {
            if (t == null || kind == TextOutline.None) return;
            var mat = t.fontMaterial;                       // ⚠️ 实例材质，别碰共享的
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, kind == TextOutline.Name ? 0.15f : 0.05f);
            if (kind == TextOutline.Name)
            {
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, Color.black);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 1.0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.578f);
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.35f);
            }
            // 描边会把字形画到原 quad 之外 ⇒ 要重算网格留白，不然描边被裁掉
            t.UpdateMeshPadding();
        }

        /// <summary>
        /// 造一个字。拿不到字体资产返回 null（调用方要判）。
        /// 参数只留最常用的几个 —— 位置/换行宽度/字号回缩由调用方按自己的版面调。
        /// </summary>
        public static TextMeshPro NewText(Transform parent, string name, string text, float fontSize, Color color)
        {
            return NewText(parent, name, text, fontSize, color, Face.Cjk);
        }

        /// <summary>同上，但**指定字体面**（🆕 2026-10-14 · `A1024` ③）。
        /// ⚠️ 旧的那个五参重载**原样保留并转发 `Face.Cjk`** ⇒ 既有调用点**零行为变化**。</summary>
        public static TextMeshPro NewText(Transform parent, string name, string text, float fontSize,
                                          Color color, Face face)
        {
            // ⚠️ `Face.Cjk` 特意**走 `Font`**（不是 `FaceAsset`）—— 那份带 `_tried` 闩与那条既有告警，
            //   换成 `FaceAsset` 会把「取不到字体」的既有告警文案悄悄换掉。
            var fa = face == Face.Cjk ? Font : FaceAsset(face);
            if (fa == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<TextMeshPro>();
            t.font = fa;
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

        /// <summary>🔴 **2026-10-16（`T09` 面板族）：造一个【uGUI】的字**（`TextMeshProUGUI`，挂在画布下）。
        /// <para>与 <see cref="NewText"/> 的**唯一区别**是组件类型与节点自带 `RectTransform` ——
        /// 字体/精灵资产/不换行那些**逐句照抄**（⛔ 别在这里另立一套默认值，两份迟早不一致）。</para>
        /// <para>⚠️ **单位不同**：世界档的 `fontSize` 是**世界单位**，这一档是**设计 px**
        /// （canvas 单位就是设计 px，判据 → `T09` §五·补③ 一）—— 同一个数在两档里的**视觉大小差 108 倍**。</para>
        /// <para>⚠️ **`raycastTarget` 出厂给 `false`**（照 `HudCanvas.MakeImage` 那条「装饰件默认不吃射线」）。
        /// 要让这块字**自己**收点击的，调用点显式打开 —— ⛔ 否则它会**静默吃掉**底下的射线。</para>
        /// <para>⚠️ 拿不到字体资产时返回 `null`（调用方要判）—— **没有点阵兜底路**
        /// （点阵走 `MeshRenderer`，挂在画布下不归画布管）。</para></summary>
        public static TextMeshProUGUI NewTextUgui(Transform parent, string name, string text, float fontSize,
                                                  Color color, Face face)
        {
            var fa = face == Face.Cjk ? Font : FaceAsset(face);
            if (fa == null) return null;

            // 节点自带 `RectTransform`：普通 `Transform` 挂在画布下会把 RT 链断掉
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = fa;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            if (CardIcons.SpriteAsset != null) t.spriteAsset = CardIcons.SpriteAsset;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = text ?? "";
            t.raycastTarget = false;      // 见 doc：装饰件默认不吃射线（要收点击的调用点显式打开）
            return t;
        }

        /// <summary>
        /// 🆕 2026-09-21：世界坐标落在哪个 `&lt;link=…>` 上；没命中返回 null。
        ///
        /// 🔴 **判据是 TMP 自己的 `FindIntersectingLink`**（原版 `TextTooltipController` 也是这么做的，
        /// 见 `…GetTraitTooltip.c:30`）—— **不要自己按版面再算一份**：折行、`&lt;nobr>`、
        /// 行内 `&lt;sprite>` 的宽度都只有 TMP 自己知道，自己算第二份迟早和画出来的位置不一致。
        /// 🔴 **放在这里、不放 `Label`**：卡面的 TMP 是 `TmpFont.NewText` 直接建的，
        /// **不经过 `Label`**（那条是按钮/面板用的）⇒ 放在这里才能让两条路**共用一份实现**。
        /// ⚠️ 点阵后端没有 link 这个概念（`tmp == null`）⇒ 如实返回 null，**不猜一个位置出来**。
        /// </summary>
        /// <summary>🆕 2026-10-15（账 `A2089`）：`LinkAt` 被调了几次（= 跑了几次 `FindIntersectingLink`）。
        /// 自检用：**指针在空处时这个数不该涨**（原版 `TextTooltipController` 那道字节闸的等价物）。</summary>
        public static int LinkAtCalls { get; private set; }

        public static string LinkAt(TMP_Text tmp, Vector3 world, Camera cam)
        {
            if (tmp == null || cam == null) return null;
            // 🆕 2026-10-15（账 `A2089`）：**调用计数** —— `FindIntersectingLink` 是这条链上最贵的一步
            //   （逐字符做矩形判定），自检拿它断「指针在空处时一次都不该跑」。
            LinkAtCalls++;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return null;                       // 落在相机背后
            int idx = TMP_TextUtilities.FindIntersectingLink(tmp, sp, cam);
            if (idx < 0) return null;
            var info = tmp.textInfo != null ? tmp.textInfo.linkInfo : null;
            if (info == null || idx >= info.Length) return null;
            string id = info[idx].GetLinkID();
            return string.IsNullOrEmpty(id) ? null : id;
        }

        /// <summary>
        /// 给一个 TMP 设折行宽度（世界单位）。
        /// ⚠️ `ComputeMarginSize()` **只在 `OnEnable` / `GetTextInfo` / `OnValidate` 里跑**
        ///    （`TextMeshPro.cs:663/367/749`），而我们是 `AddComponent` **之后**才改
        ///    `sizeDelta` 的 —— 那会儿它已经按默认宽算过一次了。所以这里显式走一趟
        ///    `GetTextInfo` 逼它重算，不然换了宽度也不折行。
        ///
        /// <para>🔴 **2026-10-11（A266）本函数 = 下面两步**（<see cref="SetWrapWidthRect"/> +
        /// <see cref="GenerateLayout"/>），**行为与拆之前逐字一致**。
        /// ⚠️ **`Label` 那条路不再用它** —— `Battle/Label.cs` 的 `SetWrapWidth` 只立刻做第一步，
        /// 把「生成」延后到对象**激活**时（<c>TryApplyPendingWrap</c>），理由见那两个函数的注释。
        /// 本函数留给**直接持有 `TextMeshPro` 的调用方**：`Core/CardView.cs` 那两处
        /// （后面紧跟 `ForceMeshUpdate()`，`FitsBox` 就靠这一次生成量版面）—— 那条路**一字未改**。</para>
        /// </summary>
        public static void SetWrapWidth(TMP_Text t, float width)
        {
            SetWrapWidthRect(t, width);
            GenerateLayout(t);
        }

        /// <summary>把折行宽**写进**那个 TMP 的容器：`textWrappingMode = Normal` + `sizeDelta = (width, 0)`。
        /// 🔴 **它【不】生成字形** —— 生成那一步见 <see cref="GenerateLayout"/>。
        ///
        /// <para>**为什么这两件事要分开（A266）**：在**未激活**的对象上走 `GetTextInfo` 会造出
        /// 「**字模生成了、渲染网格却没有**」这个半成品状态 —— `TextMeshPro.Awake()` 没跑过 ⇒
        /// `m_mesh == null`、连 `MeshFilter` 组件都不存在（那一份是 `Awake` 里 `AddComponent` 挂的），
        /// 而字模却是满的。那正是 `RewardsScene.Run` 那个 `NullReferenceException` 的根因状态
        /// （全文 → `资料/普查产出_1008/X_RewardsScene崩溃修.md` §1·3/§1·4）。
        /// **写值本身任何时候都是安全的**：`textWrappingMode` 的 setter 只标脏
        /// （`TMP_Text.cs:747`），`sizeDelta` 是个普通字段。</para>
        /// <para>⚠️ **模式仍然在这里立刻设成 `Normal`**（旧版就是这样，调用方靠紧跟的
        /// `SetWrapping(false)` 还原自己那一档）—— 把模式也记成待办、等激活时再回写，
        /// 会把那些 `0` **静默改回 `1`**（`MainMenuRuntime` 的 `SetWrapWidth(...); SetWrapping(false);`
        /// 就是这种成对写法）。</para></summary>
        public static void SetWrapWidthRect(TMP_Text t, float width)
        {
            if (t == null) return;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = new Vector2(width, 0f);
        }

        /// <summary>**显式生成一整个版面**（折行宽 / 字号 / 对齐 / `fontSizeMin~Max` 都已经设好的前提下）。
        ///
        /// <para>🔴 **为什么不走 `ForceMeshUpdate()`**：那个在**未激活**对象上**早退** ——
        /// `TextMeshPro.cs:2119`：`if (!m_isAwake || (this.IsActive() == false &amp;&amp; m_ignoreActiveState == false)) return;`
        /// —— 而本函数存在的意义恰恰是「**未激活**时也要把版面数算出来」（`Label.Align*On` 要靠它量对齐，
        /// 见 `Label.HasMeasuredWidth`）。走的是 `GetTextInfo(string)`（`TextMeshPro.cs:360-374`）：
        /// `SetText` + `SetArraySizes` + `ComputeMarginSize` + `GenerateTextMesh`，**没有 `m_isAwake` 那道闸**
        /// —— 也正是 2026-10-11 之前 `SetWrapWidth` 一直在走的那一条：**语义逐字未变，变的只是调用时机**。</para>
        /// <para>⚠️ 它在 `TextRenderFlags.DontRender` 下跑 ⇒ Phase III（写渲染网格）**被跳过**
        /// （`:5024` 的闸 = `m_renderMode == Render &amp;&amp; IsActive()`）⇒ 它只保证**版面数**
        /// （`textBounds` / `characterCount` / 行数）是对的；**画面**由 TMP 自己在对象激活后重排
        /// （真 Play 有帧循环；批处理里那几帧靠 `Shoot` 的 `cam.Render()`）。
        /// 想让**未激活**的 TMP 也把网格推上去是**做不到**的（`m_mesh` 不存在），⛔ 别改成
        /// `ForceMeshUpdate(true)` 指望它替你把画面也做出来。</para></summary>
        public static void GenerateLayout(TMP_Text t)
        {
            if (t == null) return;
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
        ///    实测同一个字：行盒 0.1437 / 字形 0.0948（这两个数都是 **`m_Scale = 1`** 那一档的读数）；
        ///    🔴 **2026-10-21（`A1262`）**：资产对齐成 `m_Scale = 0.9` 后两者**同比例**缩成
        ///    0.12933 / 0.08532 —— **比值恒 1.516**，所以本函数的**用途与口径一个字没变**。
        /// </summary>
        static float MeasureGlyph(char probe, string what)
        {
            return MeasureGlyph(Font, probe, what);
        }

        /// <summary>同上，但**指定字体资产**（🆕 2026-10-14 · `A1024` ③ 的按用途换算要用）。
        /// ⚠️ 算法与上面那条**逐字相同**（`|topLeft.y − bottomLeft.y| ÷ fontSize`）—— 别改成量墨迹/量行盒，
        ///   那会让新老两套口径对不上（`TmpSetup.Verify` 的第 ⑨ 格打的就是这一条）。</summary>
        static float MeasureGlyph(TMP_FontAsset fa, char probe, string what)
        {
            if (fa == null) return 1f;

            var go = new GameObject("TmpFontMeasure") { hideFlags = HideFlags.HideAndDontSave };
            var t = go.AddComponent<TextMeshPro>();
            t.font = fa;
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
