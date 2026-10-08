// ChoosePanel.cs — 原版「选牌 / 选效果」面板（`ChooseCardMenu`）
//
// **它和「换牌」是同一个组件吗**：不是同一个**组件**（`BattleManager` 里 `mulliganManager` 与
// `chooseCardManager` 是两个独立字段、运行时 dump 里也是并排两个节点），但**两棵节点树逐节点同构**
// —— 原版是**两份克隆**。⇒ 我们**合成一份壳**（`CardChoicePanel`），这里只放这个面板**自己**的数值。
// 出处：`资料/选牌_数据与规格.md` §甲·四之二 · 同文件 §丙（⚠️ 更正：原来指 `资料/选牌Choose_数据与设计.md` 与 `资料/选牌与选效果面板_原版数值.md`，2026-10-10 已并入）。
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
//     （`资料/选牌_数据与规格.md` §丙·三末；⚠️ 更正：原来指 `选牌与选效果面板_原版数值.md`，2026-10-10 已并入）。
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
        /// <summary>原版标题的兜底文案 = **原版词条** `Battle/ChooseCard/Instructions`
        /// （英文原文 `Choose one card`，出处 = `…battlearena1/MonoBehaviour_4871.json` 那颗 `Localize.mTerm`
        /// + 同族 TMP；全在 `Core/Loc.cs` 那一块里逐条记着，⛔ 别在这儿抄第二份）。
        /// 🔴 **2026-10-18（第十二轮 · W6）改**：以前这里是一个写死的中文 `const`（`"选择一张牌"`）——
        /// 那是「同一条语义两处写」（`Loc` 表里也有），而且**不跟语言走**。现在只留**一个来源**：
        /// `Loc.T(词条)`。⚠️ 它在 `Create()` 那一刻求值（面板是**每局新建**的，同战斗侧其余件）。</summary>
        public static string DefaultTitle { get { return Loc.T(TitleTerm); } }
        /// <summary>标题词条键 —— **只此一份**（`Create()` 与自检都取它）。</summary>
        public const string TitleTerm = "Battle/ChooseCard/Instructions";
        /// <summary>确认钮的字 = **原版词条** `Battle/Mulligan/ButtonDone`（选牌**复用了换牌那条**词条 ——
        /// 原版 `ChooseCardMenu/ButtonsGroup/Continue Button/ContinueText` 挂的就是它，TMP 原文 `Continue`）。
        /// 🔴 **2026-10-18 就地订正**：原来写「词条内容本地没有 ⇒ 用写死的中文」—— **键与英文原文都在本地**。</summary>
        public static string ConfirmLabel { get { return Loc.T("Battle/Mulligan/ButtonDone"); } }

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
        /// 🔴 **2026-10-18（第十二轮 · W6）就地订正（铁律 5）**：这里原来写
        /// 「**中文词条查不到**（I2 表本地没有）⇒ 留英文，如实标着」—— **不成立**。
        /// 键**就在本地**：`Battle/Prebattle/SelectButton`，挂在
        /// `bundle_battlesharedresources_assets_all/MonoBehaviour_-7067688171680277716.json`
        /// （父链 `CardChooseCardButtonFrame &lt; Generic Simplified UI Button_updated &lt; Button Text`），
        /// TMP 原文逐字 `Select`（全库 4 颗同键）。
        /// **错因**：上一轮只扫了 `bundle_menus_assets_all` **一个包**（正是「D 类：除 `menus` 之外的 bundle」
        /// 那一格）—— 同 `资料/已知的坑.md` #20「在一处找不到 ⇒ 说成『本地没有』」。
        /// ⇒ 现在走 `Loc.T(SelectTerm)`。</summary>
        public static string CardBtnWord { get { return Loc.T(SelectTerm); } }
        /// <summary>每张牌那颗钮的词条键 —— **只此一份**。</summary>
        public const string SelectTerm = "Battle/Prebattle/SelectButton";

        public static ChoosePanel Create(Transform parent)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `ChooseCardMenu` 实读：`RectTransform` · `anchor (0,0)-(1,1)` ·
            //    `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读）—— 与本文件头那句「根 `ChooseCardMenu` 铺满全屏」同源。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「选牌面板根 = 整屏矩形」红。
            var go = new GameObject("ChoosePanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
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

        /// <summary>本地词条表 —— **故意留空**。原版这条链查的是 I2.Loc 语言表，而**本地一张表都没有**
        /// （词条在远端 CCD；判据 → `资料/全量反编译复核_靠推断的清单.md` §2.1「词条正文在远端本地化表，本地没有」）。
        /// ⇒ **2026-10-18（第十二轮 · W6）就地订正**：无后缀那条现在**在 `Loc` 表里**（见 `TitleTerm`），
        ///    本地真正取不到的只有**带后缀**那一档（`…-&lt;uniqueId&gt;` 是运行期拼出来的，
        ///    任何 prefab 上都不可能存在 ⇒ 原版那套在远端 I2 表）。
        ///    现在的链 = **本表（覆盖 / 自检注入）→ `Loc` 表 → 调用方兜底**。
        /// 键 = 原版词条 key（`Battle/ChooseCard/Instructions` 与 `…-&lt;uniqueId>`）。</summary>
        public static readonly Dictionary<string, string> Terms = new Dictionary<string, string>();

        /// <summary>原版 `ChooseCardMenu.GetTittleText(string uniqueId)`（`dump.cs:37575`；`SetUpTitleText` 是它的内联版
        /// `:37572`）那条链：**带后缀那条 → 无后缀那条 → 调用方兜底**。
        /// 🔴 后两级都是**原版自己的回落**（不是我们加的），所以本地表空 = 出参就是 `fallback`/`DefaultTitle`。</summary>
        static string ResolveTitle(string uniqueId, string fallback)
        {
            string t = null;
            if (!string.IsNullOrEmpty(uniqueId)) Terms.TryGetValue("Battle/ChooseCard/Instructions-" + uniqueId, out t);
            if (string.IsNullOrEmpty(t)) Terms.TryGetValue("Battle/ChooseCard/Instructions", out t);
            if (!string.IsNullOrEmpty(t)) return t;
            return string.IsNullOrEmpty(fallback) ? DefaultTitle : fallback;
        }

        /// <summary>开面板。`title` 传 null 就用调用方/原版兜底文案 `选择一张牌`。
        /// ⚠️ 「选效果」复用同一个面板（原版靠 `isEnviromental` 分叉）——**我们靠标题区分**，
        ///    因为原版 `isEnviromental` 到底改了哪些视觉/文案**查不到**（那个方法体没有反编译产物）。
        /// `uniqueId` = 原版 `SetUpTitleText(string uniqueId)` 的入参：**卡定义 id**（`RawCardScript.uniqueId`
        /// 是 `string`、偏移 +0x38，`dump.cs:18813`；我们的 `CardDef.Id` 与原版同一 id 空间 —— 旁证
        /// `资料/全量反编译复核_靠推断的清单.md` §一 第 9 条：原版预组卡资产里逐字写着 `"AM3"`）。
        /// 三处调用点：进攻卡传 `"offensive"` · 防御卡传 `"defensive"` · 选牌/选效果传**那张正在结算的卡**的 id。</summary>
        public void Open(IReadOnlyList<CardView> cards, string title = null, string uniqueId = null)
        {
            LastUniqueId = uniqueId;
            base.Open(cards, ResolveTitle(uniqueId, title));
        }

        /// <summary>自检用：**最后一次 `Open` 收到的 `uniqueId`**（原版是拿它拼词条 key 的）。
        /// 三处调用点该分别给 `"offensive"` / `"defensive"` / 那张正在结算的卡的 id。</summary>
        public string LastUniqueId { get; private set; }
    }
}
