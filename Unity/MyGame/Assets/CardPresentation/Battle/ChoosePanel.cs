// ChoosePanel.cs — 原版「选牌 / 选效果」面板（`ChooseCardMenu`）
//
// **它和「换牌」是同一个组件吗**：不是同一个**组件**（`BattleManager` 里 `mulliganManager` 与
// `chooseCardManager` 是两个独立字段、运行时 dump 里也是并排两个节点），但**两棵节点树逐节点同构**
// —— 原版是**两份克隆**。⇒ 我们**合成一份壳**（`CardChoicePanel`），这里只放这个面板**自己**的数值。
// 出处：`资料/选牌Choose_数据与设计.md` §四之二 · `资料/选牌与选效果面板_原版数值.md`。
//
// ---- 数值出处（**实况**，铁律 4）----
// `资料/原版参照图/Unity参照管线_0825/data/panel_0914b/报告.md` §③（`p4_choose_tree.tsv` 的逐字段真值）
//   · 根 `ChooseCardMenu` 铺满全屏；`ChooseCardMenuAnchor` **childCount=0**（卡运行时生成）
//   · `HideChooseButton`  `ap=143.40,133.17  sd=78.44²  a=(0,0)`  sprite `40k_UI_bt_eye`
//        → 锚点是**左下** ⇒ 屏上中心 = (143.40, 1080−133.17 = **946.83**)
//   · `Continue Button`  `ap=−396.40,105.00  sd=548.02×75.90  a=(1,0)`（**底右**锚）
//        → 容器中心 = (1920−396.40, 105.00) = (**1523.60**, 从下 105.00)
//        ├ `BG`              `ap=65.20,−5.75  sd=577.50×63.84`  sprite `40k_bt_underbutton`
//        │      → (**1588.80**, 从**上** 980.75)
//        ├ `ContinueText`    `ap=−4.60,−5.26  sd=372.86×43.62`  TMP **`继续` fs=33.7** 白 左对齐
//        │      → (**1519.00**, 从**上** 980.26)
//        └ `ContinueButton`  `ap=240.31,−3.39  sd=80.47×79.64`  sprite `40k_UI_bt_play`
//               → (**1763.91**, 从**上** 978.39)
//   · `ChooseText`       `ap=7.00,−106.50  sd=1344×79.44  a=(.5,1)`  TMP **`选择一张牌` fs=55** 白 居中
//        → 屏上中心 = (**967**, 从**上** **106.5**)（和换牌面板那个文字框**完全相同**）
//   ⚠️ `ChooseText` 的 Image 组件是 **DISABLED** 的（原版那块没有底板）⇒ 我们也不画底板。
//
// ---- ⚠️ 哪些是「我们挑的」----
//   · **卡行的纵向位置**（`CardRowCy`）：✅ **2026-09-18 起有据**（原来标「我们挑的 / 无据可查」）。
//     卡行的挂点 = `ChooseCardMenuAnchor`（**只有 Transform、没有 RectTransform**），
//     `Transform_1256.json:12-16` 的 `m_LocalPosition = (0, 29, 0)`；
//     父链逐级核过（`Transform_1256.m_Father = 3277` → `RectTransform_3273` = `ChooseCardMenu` 自己，
//     `anchorMin(0,0)/anchorMax(1,1)`、`sizeDelta 0` ⇒ 铺满父级、原点即画布中心）
//     ⇒ **行中心 = 画布中心 +29 px = 距顶 511 px**（1920×1080）。
//     13 个战场的这份实例**逐份核过**都是 `(0, 29, 0)`；自洽校验：`29 × lossyScale 0.0674 = 1.955`
//     与原版运行时实测的行世界 y = **1.95** 吻合。
//     出处与推导全文：`资料/普查产出_0918/第18行_手感与选牌_规格.md` §三。
//     （原来那个 526 是「标题下沿到眼睛上沿取中点」挑出来的，已作废。）
//   · **卡上那颗 `Select` 按钮的纵向偏移**：原版 prefab `CardChooseCardButtonFrame` 是运行时实例化的，
//     静态 dump 里没有它；**但 prefab 本体在解包里**，实测按钮在卡中心下方 **≈323 px**
//     （`选牌与选效果面板_原版数值.md` §三末）。
//     ✅ **已经照原版改了**（`CardChoicePanel.cs:249` 用 `U(323f)`）—— 原来这里写「用的是卡高×0.30
//     ≈183 px，是我们排的」，**那句已过期**（2026-09-17 复核）。
//   · **压暗层**、**眼睛是开关还是按住**：见共享壳文件头的说明。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ChooseCardMenu`：**选牌**与**选效果**共用（靠标题区分）。**单选**。</summary>
    public class ChoosePanel : CardChoicePanel
    {
        /// <summary>原版运行时实测的标题兜底文案（词条 key `Battle/ChooseCard/Instructions` 的中文）。
        /// 出处：实况报告 §③ / `选牌与选效果面板_原版数值.md` §四（2026-09-14 晚更正：**运行时是中文**）。</summary>
        public const string DefaultTitle = "选择一张牌";
        /// <summary>原版 `Battle/Mulligan/ButtonDone` 的中文 —— 选牌**复用了换牌那条**词条。</summary>
        public const string ConfirmLabel = "继续";

        // ---- 🔴 2026-09-18：原来这里还有 `ChooseOneTitle`("选择一项") / `ChooseEffectTitle`("选择一个效果")，**已删** ----
        //   **为什么删**：原版**只有一个标题对象** —— `ChooseCardMenu` 的子物体只有 3 个
        //   （`ChooseCardMenuAnchor` / `ButtonsGroup` / `ChooseText`），标题就是 `ChooseText`
        //   （TMP 文本 `Choose one card` fs55、Image 组件 disabled）。
        //   它的运行时机制是 `ChooseCardMenu__SetUpTitleText.c`：
        //     `key = "Battle/ChooseCard/Instructions-" + actingCardId` → `GetTermTranslation` →
        //     **空则回落到无后缀那条** → `Localize.Term = key`。
        //   ⇒ 原版是**同一个标题换词条**，不是三个标题 ⇒ 那两句是我们的自我设计。
        //   出处：`资料/普查产出_0918/第18行_UI三小条_规格.md` §④（含 13 个战场逐份核过）。
        //   ⚠️ 「哪条文案何时用」的**调用点**在 `decomp_full` 里 grep 不到（只命中它自己）
        //      ⇒ 那部分仍**没闭合**，别当已定案。

        /// <summary>原版每张牌下面那颗按钮上的字（`CardChooseCardButtonFrame` 里的 `Select`）。
        /// ⚠️ 中文词条查不到（I2 表本地没有）⇒ **留英文**，如实标着。</summary>
        public const string CardBtnWord = "Select";

        public static ChoosePanel Create(Transform parent)
        {
            var go = new GameObject("ChoosePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ChoosePanel>();
            p.Build(new Layout
            {
                TitleCx = 967f, TitleCy = 106.5f, TitleW = 1344f, TitleH = 79.44f, TitlePx = 55f,
                BarCx = 1588.80f, BarCy = 980.75f,
                PlayCx = 1763.91f, PlayCy = 978.39f,
                EyeCx = 143.40f, EyeCy = 946.83f,
                ConfirmCx = 1519.00f, ConfirmCy = 980.26f,
                CardRowCy = 511f,                    // 原版实测值，见文件头
                CardBtnText = CardBtnWord,
            }, PickMode.Single, DefaultTitle, ConfirmLabel);
            return p;
        }

        /// <summary>开面板。`title` 传 null 就用原版兜底文案 `选择一张牌`。
        /// ⚠️ 「选效果」复用同一个面板（原版靠 `isEnviromental` 分叉）——**我们靠标题区分**，
        ///    因为原版 `isEnviromental` 到底改了哪些视觉/文案**查不到**（那个方法体没有反编译产物）。</summary>
        public void Open(IReadOnlyList<CardView> cards, string title = null)
        {
            base.Open(cards, title ?? DefaultTitle);
        }
    }
}
