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

        /// <summary>🔴 **2026-10-25（`E` 现核，C 域改动请求）**：原版 `DeckEnergyCostDrawer` 的
        /// `private const int NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`（`DeckEnergyCostDrawer.cs:43`）——
        /// **归一化的分母是【常数 10】，不是「本卡组最大值」**。
        /// <para>判据（一手，两条一起读）：</para>
        /// · 方法体 `d:/2/tools/decomp_full/DeckEnergyCostDrawer__Initialize.c:170-176`：
        ///   `fVar17 = (float)count / fVar5;` 然后 `if (fVar17 &lt; 0) 0; else if (fVar6 &lt; fVar17) fVar6;`
        ///   —— 即 **`count / 10` 再夹到 `[0, 1]`**；
        /// · 两个常量的**真值**（`GameAssembly.dll` 按 PE 节表现读，ImageBase `0x180000000`）：
        ///   `DAT_1834b2da8`（= `fVar5`）= **10.0f**、`DAT_1834b2bb8`（= `fVar6`）= **1.0f**
        ///   （脚本见 `工具/disasm_va.py` 那一族的读法；本行 2026-10-25 现读）。</summary>
        public const int NumberOfCardsForNormalization = 10;

        /// <summary>🔴 **2026-10-25（`E` 现核）**：原版 `DeckCostQuantityRowDrawer.maxCostToDraw` = **8**
        /// （ctor `:119` 的默认值，且 `assets_full` 全库 **64 个实例逐个实读都是 8**，0 例外）——
        /// 行内那个费用数字**在这一行要印 `8+`**。
        /// 判据：`DeckCostQuantityRowDrawer__Initialize` 末尾
        /// `text = cost.ToString(); if (cost == maxCostToDraw) text += "+";`
        /// （`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DeckCostQuantityRowDrawer.cs:99-106`）。</summary>
        public const int MaxCostToDraw = 8;
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

        // ============================================================ 原版那 9 颗 `Slider` 的逐颗读数
        // 🔴 **2026-10-19（U4 · 界面线批次 6）**：`Deck Editing Menu` 的 9 颗 `Slider` **逐颗读完**
        // （`bundle_menus_assets_all`，路径 `Content Area/Sidebar/Deck Details/Deck Information cost drawer/
        //  Content/Deck CostQuanityt Row Drawer` + 后缀 `"" / " (1)" .. " (8)"` + `/Slider`）——
        // **9 颗值完全一致**（不是抽样、不是「一个值当全部情况」）：
        //   · `m_Interactable = 0`（**不可交互**）
        //   · `m_Transition   = 0 (None)`（**连默认的 ColorTint 都不是**）
        //   · `m_TargetGraphic = null` · `m_HandleRect = null`（**没有手柄**）
        //   · `m_FillRect` → 子件 `Fill`（`40k_CardAmount_bar_fill` · `m_Color (1, 0.818, 0.486, 1)` ·
        //     `m_RaycastTarget = 0`）
        //   · `m_Direction = 0 (LeftToRight)` · `m_MinValue = 0` · `m_MaxValue = 1` · `m_WholeNumbers = 0`
        //   · 子件 `Background`（`40k_CardAmount_bar_bg` · 九宫 `3,0,15,0` · `m_RaycastTarget = 1`）
        //   · `m_Colors` 仍是 UGUI 默认四格（白 / 0.9608 / 0.7843 / (0.7843, α0.502)）——
        //     但 `m_Transition = None` ⇒ **那四格一个都不会被用上**。
        // ⇒ **原版这一族没有任何悬停/按下反馈**：它只是「画出来的槽」
        //   （`DeckCostQuantityRowDrawer.Initialize` 只写 `fillImage.fillAmount`）。
        // 🔴 **⛔ 别给这 9 颗接 `WfSlider` 反馈** —— UGUI `Slider` 全族默认是 `t=1 ColorTint`，
        //   **但这一族是显式 `t=0` + 不可交互**；「按全族默认值一刀切」会与原版相反。
        // ⚠️ `Background` 的 `m_RaycastTarget = 1` 是序列化事实，但父 `Slider` 不可交互 ⇒
        //   uGUI `Selectable.IsInteractable()` 早退，那一下**什么都不发生** ⇒ 我们的等价物 = 不建命中区。
        // 我们自己那两扇窗（`DeckRuntime.BuildCurve` / 本文件 `Build`）画的都是**静态图**（`ImageQuad`），
        // 没有 `Selectable` / `WindowButton` —— 与原版一致，⛔ 别「补交互」。

        /// <summary>自检用/宿主用：第 `count` 张卡那一行的填充比例 —— **判据只有上面那一条**
        /// （`count / NumberOfCardsForNormalization` 再夹 `[0,1]`）。⛔ 别在调用点另算一份。</summary>
        public static float NormalizedForTest(int count)
        {
            return Mathf.Clamp01(count / (float)NumberOfCardsForNormalization);
        }

        /// <summary>自检用：第 `row` 行费用数字的文本（第 `MaxCostToDraw` 行 = **`8+`**）。</summary>
        public static string CostLabelForTest(int row)
        {
            return (row == MaxCostToDraw) ? (row + "+") : row.ToString();
        }

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
        /// <paramref name="rowName"/> = 行的节点名前缀（两扇窗的原版名一样，都是 `Deck CostQuanityt Row Drawer`）· <paramref name="instant"/> = 原版那个 `instant` 形参（真 ⇒ 直赋、**不补间也不闪白**；判据见类尾「两段补间」一节）。</summary>
        public static void Build(Transform parent, float cx, float cy, float scale, int[] counts,
                                 int qRow, int qText, string rowName = "Deck CostQuanityt Row Drawer",
                                 bool instant = false)
        {
            if (parent == null || counts == null) return;

            float rowW = RowW * scale, rowH = RowH * scale;
            for (int i = 0; i < Rows; i++)
            {
                float rcy = cy + (Row0Dy + i * RowStep) * scale;
                float x1 = cx - rowW * 0.5f, x2 = cx + rowW * 0.5f;
                float y1 = rcy - rowH * 0.5f, y2 = rcy + rowH * 0.5f;
                var node = MenuDraw.Node(parent, rowName + (i == 0 ? "" : " (" + i + ")"),
                                         new PxRect(x1, y1, x2, y2));

                // 费用数字（绿）—— 🔴 最后一行（= `maxCostToDraw` 8）印 **`8+`**，见 `MaxCostToDraw`。
                float ncx = cx + CostDx * scale;
                string costLabel = CostLabelForTest(i);
                // 🆕 **2026-10-17（`A1024③`）**：原版 `…/Card Cost` = **Pragati**（`R_A1024_字体按用途普查.md` §Core/CostCurveDrawer）
                MenuDraw.Text(node, new PxRect(ncx - 23f * scale, y1, ncx + 23f * scale, y2),
                              costLabel, CostColor, "Card Cost", RowFontPx * scale, qText,
                              face: TmpFont.Face.LatinCardBody);

                // 滑块：底 + 填充（九宫格）
                float sx1 = cx - SliderW * scale * 0.5f, sx2 = cx + SliderW * scale * 0.5f;
                float sy1 = rcy - SliderH * scale * 0.5f, sy2 = rcy + SliderH * scale * 0.5f;
                var bgTex = CardArt.MenuUi(ArtBarBg);
                if (bgTex != null)
                    // 🔴 **2026-10-26（K-05）**：端帽**乘 scale** —— 文件头那条规矩（原版靠节点 `localScale`
                    //   放大、我们按放大后的像素直接画；字号 `25` 已经这么干了）对 `m_Border` 一样成立。
                    //   原来漏乘 ⇒ 1.2 / 1.8 那两档的端帽都偏小（1.2 档 3px→应 3.6px、1.8 档 15px→应 27px）。
                    MenuDraw.Nine(node, bgTex, new PxRect(sx1, sy1, sx2, sy2),
                                  new Vector4(3f * scale, 0f, 15f * scale, 0f),
                                  bgTex.width, bgTex.height, qRow, null, true, "Background");
                int n = i < counts.Length ? counts[i] : 0;
                // 🔴 **2026-10-25（`E` 现核）**：分母是常数 10、再夹 `[0,1]`（判据见 `NumberOfCardsForNormalization`）。
                // ⛔ 别改回「本卡组最大值」—— 那是我们 2026-10-03 自己挑的口径，原版没有。
                float frac = NormalizedForTest(n);
                // 🔴 **2026-10-26（K-05 · 账 `卡组:71` + `卡组:131`）：填充改成「按满槽宽建一次 + 逐帧改几何」。**
                //   原版是 uGUI `Slider.fillAmount`（`Image.Type = Sliced` ⇒ **端帽不缩放、中段拉伸**），
                //   进场那一刻 `Awake` 把值清 0 ⇒ 每行都从 0 长到目标值（值 0.2 s / 色 0.4 s 两段补间，
                //   判据全文在类尾「两段补间」那一节）。
                //   ⚠️ 九宫格**不能每帧重建**：`Object.Destroy` 在 player 里延到帧末 ⇒ 会叠出一圈残影
                //   （批处理下 `DestroyImmediate` 又是另一条路）⇒ 建一次、只改三块（左端帽 / 中段 / 右端帽）。
                var fillTex = CardArt.MenuUi(ArtBarFill);
                if (fillTex != null)
                {
                    var fillGo = MenuDraw.Nine(node, fillTex, new PxRect(sx1, sy1, sx2, sy2),
                                               new Vector4(3f * scale, 0f, 14f * scale, 0f),
                                               fillTex.width, fillTex.height, qRow, FillTint, true, "Fill");
                    var tw = MakeRowTween(fillGo, LayoutSpace.Px(SliderW * scale),
                                          LayoutSpace.Px(3f * scale), LayoutSpace.Px(14f * scale),
                                          LayoutSpace.Px(SliderH * scale));
                    if (instant) ApplyFrac(tw, frac);   // 原版 instant 支：直赋终值（不闪白、不改色）
                    else RegisterTween(tw, frac);       // 原版非 instant 支：几何清零 + 两段补间
                }

                // 张数（白）
                float ccx = cx + CountDx * scale;
                // 🆕 **2026-10-17（`A1024③`）**：原版 `…/Cards in deck` = **Pragati**（同上）
                MenuDraw.Text(node, new PxRect(ccx - 23f * scale, y1, ccx + 23f * scale, y2),
                              n.ToString(), Color.white, "Cards in deck", RowFontPx * scale, qText,
                              face: TmpFont.Face.LatinCardBody);
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

        // ============================================================ 两段补间（原版 `DeckCostQuantityRowDrawer.Initialize`）
        // 判据（🥇 `d:/2/tools/decomp_full/DeckCostQuantityRowDrawer__Initialize.c` 逐句；常量按 PE 节表现读，
        // ImageBase `0x180000000`，工具 `工具/read_literal.py`）：
        //   ① `instant == false` 支（`.c` 的 `param_5 == 0`）：
        //      · 先判差值：`max(max(|cur|,|tgt|)·1e-06, Mathf.Epsilon·8) <= |tgt−cur|` 才进补间
        //        （`DAT_1834b2db8` = **1e-06** · `DAT_1834b2e00` = **8.0f**）—— 这一串就是
        //        **`!Mathf.Approximately(cur, tgt)`**；差值可忽略时**整段跳过**（连白色闪都不发生）。
        //      · `DOTweenModuleUI.DOValue(slider, normalizedQuantity, **0.2f**)`（`DAT_1834b2bb0` 实读 0.2），
        //        **没有 `SetEase`** ⇒ 走 DOTween 全局默认缓动；默认值 = 原版 `DOTweenSettings` 资产实读
        //        **`defaultEaseType: 6`** = **`Ease.OutQuad`**（`d:/2/tools/assetripper_out/ExportedProject/Assets/
        //        Resources/DOTweenSettings.asset:31`；DOTween 枚举 = Unset 0 / Linear 1 / InSine 2 / OutSine 3 /
        //        InOutSine 4 / InQuad 5 / **OutQuad 6**）；同资产 `defaultTimeScaleIndependent: 0`
        //        ⇒ **scaled** 时间（= `Time.deltaTime`，不是 unscaled）。
        //      · `fillImage.color = colorWhileUpdating` **立刻**写 —— 那四段从 `0x1834b2e50` 起 16 字节读入，
        //        实读 **1, 1, 1, 1 = 白**（`DeckCostQuantityRowDrawer__.ctor.c` 的四句
        //        `*(undefined4 *)(param_1 + 0x40/0x44/0x48/0x4c) = …`）。
        //      · `DOColor(fillImage, originalColor.Value, **0.4f**).SetEase(**5 = InQuad**)`
        //        （`DAT_1834b3158` = 0.4；`SetEase` 第 2 实参 = `5`）。`originalColor` = **首次** Initialize 时
        //        从 `fillImage.color` 取一次（`originalColor == null` 才取）⇒ 我们的等价物 = 建行那一刻的
        //        `FillTint`（原版 prefab 的 `Fill.m_Color` = `(1, 0.818, 0.486, 1)`，本文件同一值）。
        //   ② `instant == true` 支：**只** `slider.value = normalizedQuantity` —— **不写颜色、不闪白**。
        //   ③ 两支都跑完之后才写两段文本（`Cards in deck` / `Card Cost`，后者 `cost == maxCostToDraw` 加 `+`）——
        //      文本**不参与补间**；本文件那两句本来就在 `Build` 里直写 ⇒ 已对。
        //   ④ 进场起点 = 0：原版 `Awake` 把 `slider.value` 清 0（`DeckCostQuantityRowDrawer__Awake.c`）
        //      ⇒ 每行都是「从 0 长到目标值」，不是「从上一档长过来」。
        // 🔶 自己设计（非原版）：下面这套「登记表 + 手工 `Tick(dt)`」是本仓的驱动方式 —— 原版靠 DOTween 插件，
        //    而本工程没有那个运行时（全仓无 Demigiant）。为什么必须自己定：没有插件可用，只能自建等价补间器；
        //    **时长与两条缓动曲线照抄原版常量与 `DOTweenSettings` 资产**（见上 ①，不是我们挑的）。
        //    调度台 2026-10-26 定（账 `卡组:71` / `卡组:131`）。

        /// <summary>值补间时长（秒）—— 原版 `DOValue(slider, target, **0.2f**)`（`DAT_1834b2bb0` 实读 0.2）。</summary>
        public const float ValueTweenSeconds = 0.2f;
        /// <summary>颜色补间时长（秒）—— 原版 `DOColor(fillImage, originalColor, **0.4f**).SetEase(InQuad)`。</summary>
        public const float ColorTweenSeconds = 0.4f;
        /// <summary>补间期间填充的临时色 —— 原版 `colorWhileUpdating`，`.ctor` 从 `0x1834b2e50` 读 16 字节 = **(1,1,1,1)**。
        /// ⚠️ 它是**回落值**（0.4 s 内渐变回 `FillTint`），⛔ 别当常色用、也别拿它当 `FillTint` 的期望值。</summary>
        public static readonly Color ColorWhileUpdating = new Color(1f, 1f, 1f, 1f);

        /// <summary>一行填充的三块（`m_Border` 上下都是 0 ⇒ 九宫格只剩**中间那一行**：左端帽 / 中段 / 右端帽）。
        /// <para>`slotW` / `capLW` / `capRW` / `height` 都是**世界单位**（调用点用 `LayoutSpace.Px` 换算）。
        /// 三块的位置一律相对**根**算，而根是按**满槽宽**建一次就不再动的 ⇒ 左沿恒在 `−slotW/2`（原版
        /// `Slider` 的 `m_Direction = 0 (LeftToRight)`，值长只往右推）。</para></summary>
        sealed class RowTween
        {
            public GameObject root;
            public ImageQuad capL, mid, capR;
            public float slotW, capLW, capRW, height;
            public float from, to, t;
            public Color baseColor;
        }

        static readonly List<RowTween> _tweens = new List<RowTween>();
        static bool _fillPartsWarned;

        /// <summary>把 `MenuDraw.Nine(..., name: "Fill")` 建出来的九宫格抓成补间句柄。
        /// ⚠️ 子块名 = `ImageQuad.CreateNineSlice` 的固定拼法 **`&lt;name&gt;_&lt;i&gt;&lt;j&gt;`**
        /// （这一族上下边框为 0 ⇒ 只有 `j = 1` 那一行 ⇒ `_10` / `_11` / `_12`）。
        /// 抓不到 ⇒ 返回一个三块全 null 的句柄（几何改不动 = 退化成直赋）并**出声一次**（不许静默）。</summary>
        static RowTween MakeRowTween(GameObject go, float slotW, float capLW, float capRW, float height)
        {
            var tw = new RowTween
            {
                root = go, slotW = slotW, capLW = capLW, capRW = capRW,
                height = height, baseColor = FillTint,
            };
            if (go == null) return tw;
            tw.capL = NinePart(go, go.name + "_10");
            tw.mid = NinePart(go, go.name + "_11");
            tw.capR = NinePart(go, go.name + "_12");
            if (tw.mid == null && !_fillPartsWarned)
            {
                _fillPartsWarned = true;
                Debug.LogError("[CostCurveDrawer] 填充九宫格抓不到三块（`" + go.name + "_10/_11/_12`）⇒ "
                             + "值补间只会改根节点显隐、**几何不会伸缩**（原版是 `Slider.fillAmount` 伸缩）。"
                             + "原因多半是 `ImageQuad.CreateNineSlice` 的子块命名变了，去看那一份。");
            }
            return tw;
        }

        static ImageQuad NinePart(GameObject root, string child)
        {
            var t = root.transform.Find(child);
            return t != null ? t.GetComponent<ImageQuad>() : null;
        }

        /// <summary>登记一条「0 → 目标值」的补间（原版非 instant 支）。差值可忽略
        /// （`Mathf.Approximately(0, 目标)`）⇒ **不登记**、当场到位（原版那一刻整段 `if` 都不进 ⇒ 连白闪都没有）。</summary>
        static void RegisterTween(RowTween tw, float to)
        {
            if (tw == null) return;
            tw.from = 0f;
            tw.to = Mathf.Clamp01(to);
            ApplyFrac(tw, 0f);                                  // 原版 `Awake`：值清 0
            if (Mathf.Approximately(tw.from, tw.to))
            {
                ApplyFrac(tw, tw.to);
                SetFillTint(tw, tw.baseColor);                   // 没闪白 ⇒ 颜色停在原色
                return;
            }
            tw.t = 0f;
            SetFillTint(tw, ColorWhileUpdating);                 // 原版：先把 `fillImage.color` 写成白色
            _tweens.Add(tw);
        }

        /// <summary>把某个值（0..1）落到三块几何上 —— 等价于 uGUI `Slider.fillAmount` 那一句
        /// （`Image.Type = Sliced` ⇒ 端帽不缩放、中段伸缩；两帽加起来比目标宽还宽时**按比例压帽** =
        /// `Image.GetAdjustedBorders`）。根按满槽宽建、左沿不动。</summary>
        static void ApplyFrac(RowTween tw, float frac)
        {
            if (tw == null || tw.root == null) return;
            float w = Mathf.Clamp01(frac) * tw.slotW;
            float l = tw.capLW, r = tw.capRW;
            if (l + r > w) { float s = w / Mathf.Max(1e-4f, l + r); l *= s; r *= s; }
            float m = Mathf.Max(0f, w - l - r);
            float left = -tw.slotW * 0.5f;
            bool on = w > 1e-4f;
            if (tw.root.activeSelf != on) tw.root.SetActive(on);
            PlacePart(tw.capL, left + l * 0.5f, l, tw.height);
            PlacePart(tw.mid, left + l + m * 0.5f, m, tw.height);
            PlacePart(tw.capR, left + w - r * 0.5f, r, tw.height);
        }

        static void PlacePart(ImageQuad q, float cxWorld, float wWorld, float hWorld)
        {
            if (q == null) return;
            float minW = LayoutSpace.Px(0.5f);                 // 半个设计像素以下当「没有」
            bool on = wWorld > minW;
            if (q.gameObject.activeSelf != on) q.gameObject.SetActive(on);
            if (!on) return;
            var lp = q.transform.localPosition;
            lp.x = cxWorld;
            q.transform.localPosition = lp;
            q.SetAspect(wWorld / Mathf.Max(1e-5f, hWorld));
        }

        static void SetFillTint(RowTween tw, Color c)
        {
            if (tw == null) return;
            if (tw.capL != null) tw.capL.SetTint(c);
            if (tw.mid != null) tw.mid.SetTint(c);
            if (tw.capR != null) tw.capR.SetTint(c);
        }

        /// <summary>推进所有在跑的补间（`dt` 秒），返回**还在跑**的条数。
        /// <para>生产侧由 <see cref="Pump"/> 每帧调（`Time.deltaTime` = 原版 DOTween 的 **scaled** 时间，
        /// 判据 = `DOTweenSettings.defaultTimeScaleIndependent = 0`）。**批处理 / 编辑模式不跑 PlayerLoop**
        /// ⇒ 自检要自己调（例：`Tick(0.1f)` 两次 ⇒ 值到位）—— 这也正是「补间已挂」能被断言的前提：
        /// 不推进 ⇒ 停在起点、<see cref="ActiveTweens"/> 仍是 N。</para>
        /// <para>宿主重建 / 关窗 ⇒ 句柄里的 `root` 变成 Unity 伪 null ⇒ 那一条**直接丢弃**（不会去写死对象）。</para></summary>
        public static int Tick(float dt)
        {
            if (dt < 0f) dt = 0f;
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                var tw = _tweens[i];
                if (tw == null || tw.root == null) { _tweens.RemoveAt(i); continue; }
                tw.t += dt;
                float tv = Mathf.Clamp01(tw.t / ValueTweenSeconds);
                ApplyFrac(tw, Mathf.LerpUnclamped(tw.from, tw.to, OutQuad(tv)));
                float tc = Mathf.Clamp01(tw.t / ColorTweenSeconds);
                SetFillTint(tw, Color.LerpUnclamped(ColorWhileUpdating, tw.baseColor, InQuad(tc)));
                if (tw.t >= ColorTweenSeconds)      // 0.4 > 0.2 ⇒ 以**长的那一条**收尾
                {
                    ApplyFrac(tw, tw.to);
                    SetFillTint(tw, tw.baseColor);
                    _tweens.RemoveAt(i);
                }
            }
            return _tweens.Count;
        }

        /// <summary>原版 DOTween 的 `Ease.OutQuad`（`1 − (1−t)²`）—— 值那一条用它（判据：DOTween 全局默认）。</summary>
        static float OutQuad(float t) { float u = 1f - t; return 1f - u * u; }
        /// <summary>原版 `Ease.InQuad`（`t²`）—— 颜色那一条用它（原版显式 `SetEase(5)`）。</summary>
        static float InQuad(float t) { return t * t; }

        /// <summary>「补间已挂」的读数：**正在跑**的条数（0 = 全部到位）。</summary>
        public static int ActiveTweens { get { return _tweens.Count; } }

        /// <summary>自检读数：每条一行 `0.00→0.30 @0.10s`（行序 = 登记序 = 费用 0..8 的顺序）。</summary>
        public static string DumpTweens()
        {
            if (_tweens.Count == 0) return "(无补间)";
            var sb = new StringBuilder();
            for (int i = 0; i < _tweens.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                var tw = _tweens[i];
                if (tw == null) { sb.Append("(空)"); continue; }
                sb.Append(tw.from.ToString("F2")).Append("→").Append(tw.to.ToString("F2"))
                  .Append(" @").Append(tw.t.ToString("F2")).Append('s');
            }
            return sb.ToString();
        }

        /// <summary>清空补间登记表（自检换夹具时用）。⚠️ 不清洗任何节点几何 —— 要归零就自己再 `Build`。</summary>
        public static void ResetTweensForTest() { _tweens.Clear(); }

        /// <summary>泵是否已插进 PlayerLoop（诊断读口）。</summary>
        public static bool PumpAttached { get { return Pump.Attached; } }

        // ---- 每帧泵（原版 = 每行组件的 `DOTween` 自己按 `DOTween.update` 走；我们没有 DOTween，挂 Unity 的 PlayerLoop）----
        /// <summary>进 Play / 启动时装一次泵。
        /// ⚠️ 这个入口**故意挂在外层类**（不是嵌套类 `Pump`）—— `RuntimeInitializeOnLoadMethod` 只扫静态方法，
        /// 嵌套类里的那条**不保证被扫到**（同 `Battle/BattleSfx.cs` 的先例）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallPump() { Pump.Attach(); }

        /// <summary>把 <see cref="Tick"/> 接到 Unity 自带每帧泵的 `Update` 阶段。
        /// <para>**为什么需要它**：补间登记完**没人推** = 柱子永远停在 0（静默的「动画不动」）；而批处理 /
        /// 编辑模式**不跑 PlayerLoop**（那是 `EditorApplication.update`）⇒ 自检走 <see cref="Tick"/> 手推。
        /// 同款先例 = `Shell/LiveOpsHandler.cs` 的 `LiveOpsPump`（含「为什么不是 `Application.update`」那条实测）。</para></summary>
        public static class Pump
        {
            static bool _installed;
            /// <summary>已插进 PlayerLoop 没有（诊断读口）。</summary>
            public static bool Attached { get { return _installed; } }

            /// <summary>幂等安装（域重载关掉时静态字段会跨 Play 存活 ⇒ 不查就会越插越多）。</summary>
            public static void Attach()
            {
                var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                if (Contains(loop)) { _installed = true; return; }
                if (!Insert(loop, typeof(UnityEngine.PlayerLoop.Update)))
                {
                    Debug.LogError("[CostCurveDrawer] 没找到 PlayerLoop 的 `Update` 阶段 ⇒ 费用曲线的柱子"
                                 + "**不会自己长起来**（会一直停在 0；自检手调 `Tick(dt)` 不受影响）");
                    return;
                }
                UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
                _installed = true;
            }

            /// <summary>摘掉（自检 / 手动接管时间时用）。重复调用安全。</summary>
            public static void Detach()
            {
                var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                if (Remove(loop)) UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
                _installed = false;
            }

            static void OnUpdate() { Tick(Time.deltaTime); }   // scaled：原版 `defaultTimeScaleIndependent = 0`

            static bool Contains(UnityEngine.LowLevel.PlayerLoopSystem sys)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == typeof(Pump)) return true;
                    if (Contains(subs[i])) return true;
                }
                return false;
            }

            static bool Insert(UnityEngine.LowLevel.PlayerLoopSystem sys, System.Type phase)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == phase)
                    {
                        var inner = subs[i].subSystemList;
                        var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner == null ? 0 : inner.Length);
                        if (inner != null) list.AddRange(inner);
                        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem
                        {
                            type = typeof(Pump),
                            updateDelegate = OnUpdate,
                        });
                        subs[i].subSystemList = list.ToArray();
                        return true;
                    }
                    if (Insert(subs[i], phase)) return true;
                }
                return false;
            }

            static bool Remove(UnityEngine.LowLevel.PlayerLoopSystem sys)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == typeof(Pump))
                    {
                        var inner = subs[i].subSystemList ?? new UnityEngine.LowLevel.PlayerLoopSystem[0];
                        var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner);
                        list.RemoveAll(s => s.type == typeof(Pump));
                        subs[i].subSystemList = list.ToArray();
                        return true;
                    }
                    if (Remove(subs[i])) return true;
                }
                return false;
            }
        }
    }
}
