// Badges.cs —— 棋盘**单位卡身上的 buff / debuff 徽标**（2026-09-15）
//
// **原版出处**（三条独立证据，缺一不可）：
//  ① 组件：`BattleCardUI.boardTraitIcons[7]` —— 7 个 `BoardTraitIcon`
//     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleCardUI.cs:226`）。
//     闭环：`BoardTraitIcon` 的 MonoScript PathID = `-1269599923680593461`，带
//     `traitIcons[2]/traitNotActiveIcons[2]/counterText` 的实例全库**只有 7 个**，
//     与 7 个容器**逐个对上**（2026-09-15 全库扫）。
//  ② 驱动链：`CardScript.UpdateTraitIcons()` → `BattleCardUI.UpdateTraitIcons()`
//     （反编译 `CardScript__UpdateTraitIcons.c:8`；**加/移除效果、眩晕、伏击、回合结束**等
//     15+ 个卡事件都会触发重算）。守卫：非单位直接 return；每次重算先把 7 个位全部 `Toggle(false)`。
//     🔴 **2026-10-24（`W10`）就地订正（铁律 5）**：本行原来写「**伏击（ambush 且未翻面）不显示**」——
//        只对一半。真值（`BattleCardUI__UpdateTraitIcons.c:40-46`，**逐句现读**）是**两条**：
//          ㈠ `ambush` 的 `hasIcon == false` ⇒ **它自己永远进不了这 7 格**（`InPlayIconKeys` 里没有它）；
//          ㈡ 先把 7 格全 `Toggle(false)`，然后 `if (HasCurrentTrait(card, 0x4b0 /*ambush*/) &&
//             *(card + 0x40 /*isPlayer*/) == 0) return;` ⇒ **对面那张伏击单位整排一枚都不显示**，
//             跟它身上还有没有别的词条无关。
//          ⚠️ `Core/CardView.cs` 的 `ApplyAmbush` 里那句「徽标不藏（原版那一支没碰 boardTraitIcons）」
//             **只对 `SetAmbush` 那条支成立**、对 `UpdateTraitIcons` 不成立 ⇒ 两处说法打架。
//             🔴 **`CardView.cs` 本批不归 `W10`（文件所有权）⇒ 那句注释【还没改】**，
//             已写进 `资料/普查产出_1024d/W10_选择器与词条_执行报告.md` 的「没做到」一节
//             （下一批改 `CardView` 的代理请顺手把那句改成「`SetAmbush` 只换面朝下图；
//             整排徽标的显隐由 `Badges.For(..., isPlayer)` 那道闸管」）。
//          ⇒ ㈡ 的落地口 = `For(...)` 的 `isPlayer` 形参（调用方传「是不是我方的」）。
//     图标取 `CardTrait._traitIcon`（运行时按
//     卡当前拥有的 trait 查表），角标数值取 `GetCurrentTraitValueWithModifiers()`，
//     激活态取 `CardTrait.IsActive(CardScript)`。
//  ③ 几何：`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/TraitIconContainer*.json`
//     （7 份）—— 父链 `TraitIconContainer ← TraitIcons ← 3DBody ← Board Elements ← CardPrefab`。
//
// 🔴 **哪些关键词进这 7 个位 —— 现在按【原版真值表】筛，不再是「有图就画」**（2026-10-24 · `W10`）
//
//  · **原版判据**（`d:/2/tools/decomp_full/CardTraitCollection__GetTraitInPlayListForIcons.c`，202 行，
//    调用点 `BattleCardUI__UpdateTraitIcons.c:59`）—— 一个 trait 进这 7 个位要**同时**满足：
//      ① `CardTrait._hasIcon`（偏移 `0x21`；getter `CardTrait__get_HasIcon.c` 实测就是读它）
//      ② `CardTrait._isInPlayIcon`（偏移 `0x22`；`CardTrait__get_IsInPlayIcon.c` 同）
//      ③ `CardTrait._traitIcon != null`（偏移 `0x18`，`UnityEngine_Object__op_Inequality` 那一步）
//      ④ （第一支循环另加）**这张卡的 `CardTraitData._hide == false`**（偏移 `0x20` = 我们这边没有的
//         那一层「单卡隐藏」，`CardDef` 里无对应字段 ⇒ **这一条今天恒真**，如实记着）
//    另有一条**同族但不同用途**的筛选器：`CardTraitCollection__GetTraitListForCardIcons.c`（100 行）
//    —— 那条**只要求 ① + ③**（不要求 `_isInPlayIcon`）⇒ 两者差的那一族 = 「有图标、但不算**场上**图标」。
//
//  🔴 **2026-10-24（`W10`）就地订正（铁律 5）——下面这一段原来写着「那两列的逐 trait 取值本地取不到」，
//     那句【已作废】。** 原文留痕：
//       「`CardTrait` 的资产本体本地没有 → 全盘按字段名搜 0 命中 → `resources/MonoBehaviour/` 空目录
//        ⇒ `For(...)` 只能拿『查得到图标』当那两条开关的替身」。
//     **为什么作废**：2026-10-23「Trait 全量权威化」已把 73 个 `CardTrait` 逐字节解进
//     `Unity/数据/游戏数据/trait_collection.json`（**77 条全带** `hasIcon` / `isInPlayIcon` / `traitIcon`），
//     生成器 = `工具/export_trait_collection.py`（安卓 1.35.0 与 PC 关服前 build 逐字段 0 差异）。
//     错因是「当时确实没有」→ **后来有了、但没有回来改这一行**（不是当初查错）。
//    ⇒ 现行判据 = 下面那张 `InPlayIconKeys`（**由那两列 + `traitIcon` 现算出来的 47 个键**，每条都能
//      在 `trait_collection.json` 里逐条复查）；`For(...)` 用它筛。
//    ⚠️ 那份 JSON **只在 `Unity/数据/游戏数据/`（不在 `Resources/`）** ⇒ 运行期读不到 ⇒ 这里是
//      **照数据抄下来的静态表**（不是猜的）。数据哪天变了，重跑：
//        `python -I -X utf8 -c "import json;d=json.load(open(r'd:/4/Unity/数据/游戏数据/trait_collection.json',encoding='utf-8'));
//          print([(t['name'],t['hasIcon'],t['isInPlayIcon'],bool(t['traitIcon'])) for t in d['traits']])"`
//      并把差异落到 `InPlayIconKeys`。
//    ⛔ **别再把「查不到」写成「没反编译」**（那是两种不同的缺口：一个缺数据、一个缺口径）。
//  · **底板怎么画** —— 🔴 **2026-10-12 就地订正（铁律 5，`A1661`）**：本行原来写
//    「原版那个 sprite（PathID 2729514481590049735）在解包里**找不到本体**，我们按工程里现成的
//    `Art/ui/Base3d_Trait_Background.png` 画（按自身比例 fit 进格子）」—— **两条都不成立**：
//    ① **找得到**：它 = `Base3d Trait Background`（`bundle_atlasindividual_assets_battleatlasui/`
//       `Sprite/Base3d Trait Background_2729514481590049735.json`），而且**我们画的就是它**
//       （`Resources/Art/ui/Base3d_Trait_Background.png` 与原版 **逐字节相同**，md5 `75a79401371f`）。
//       **错因**：按**对象名** `IconBackground` 去找 **Sprite**（全库没有这个名的 sprite）⇒ 判成「没有」。
//       **名字对不上 ≠ 资源不存在**（本仓栽在这上面好几次了）。
//    ② **不是「按自身比例 fit 进格子」**。原版 `IconBackground` 是个 **Sliced** 的 `SpriteRenderer`：
//       **`m_Pivot = (0, 0.5)`（左边缘 ⇒ 从外缘往卡内铺）** · `m_Border = (0, 0, 96, 0)` ·
//       `m_Color = (0.32298, 0.34305, 0.35849)`（深灰）· `m_DrawMode = 1`。
//       ⇒ **画法只有一处**：`CardView.BadgePlateQuad`（含那条竖切线、可见板宽 0.3312）；
//       判据全文 = `资料/普查产出_第二十二会话/DIAG_徽标底板图.md`。
//  · **手牌上显不显示** —— 原版手牌与棋盘**是同一个 prefab**（7 个位物理上都在手牌上），
//    但有没有被隐藏**没找到判据**（`ToggleTraitIcons`/`FadeAllTraitsIcons` 在已反编译集合里无调用者）
//    ⇒ 我们**只在棋盘（场上）显示**，手牌不显示。
//  · 🔴 **7 个位的【次序】**（2026-10-12 · `A1613②` 现读反编译坐实；此前一直没查、只当「我们挑的」）：
//    **原版没有任何排序** —— `CardTraitCollection__GetTraitInPlayListForIcons.c`（202 行）是
//    **按 `List` 的索引序**走两层（外层 `param_1+0x110`、内层 `*(elem+0x10)+0x290`）、逐条 `Add`；
//    唯一的「顺序规则」是**去重**：`System_Predicate<b__0>` 比较的是 **trait id**（`(a+0x10)+0x18 == b+0x18`），
//    `List.Find` 命中就 `continue` ⇒ **先出现者赢**。消费侧 `BattleCardUI__UpdateTraitIcons.c`（132 行）
//    同样是**按索引序**把列表逐个写进那 7 个槽（没有 sort、没有倒序）。
//    ⇒ **原版次序 = 该卡 trait 列表的【数据作者序】**（`CardTraitCollection.TraitCollection`
//      这份 ScriptableObject 资产里的排列），**既不是字母序、也不是「效果文字里首现序」**。
//    ⚠️ **那份资产本地取不到**（`A1613②` 现搜：`assets_full` 全盘按名搜 `TraitCollection` / `CardTrait`
//      **只有 1 个 MonoScript 命中、零资产**；与上面那条「`CardTrait` 零实例」同一件事）
//      ⇒ **逐卡的原版次序今天判不了**（这是「判据不可及」，不是「没找」）。
//    ⇒ 我们 `For(...)` 用的「`orderHint` 里首现序 → 规范键字典序」是**替身**，并且**与上面两条都不同**；
//      **判据可取到的那一天要按资产列表序替换它**（先取哪一份资产，见 `For(...)` 上方那段）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一个徽标位要画的东西。`sprite` 为 null = 这个位空着。</summary>
    public struct Badge
    {
        public string sprite;   // `Art/traits/` 里的图名（图集文件名就是关键词名）
        /// <summary>引擎侧的**规范关键词键**（`UnitState.Keywords` 的键，例 `"huntmark"`）。
        /// 只拿来配对 —— 脉冲高亮要按「刚触发的是哪个词条」（`BattleEvent.Keyword`）找到**这一位**。</summary>
        public string key;
        public int counter;     // 角标数字；0 = 不带角标（原版 `ToggleCounter(false)`）
        /// <summary>原版 `BoardTraitIcon.Initialize(icon, counter, traitEnabled)` 的**第 3 个（bool）参数**：
        /// true ⇒ 用 `originalMaterial`；false ⇒ 换 `disabledMaterial`（**只灰化，位置和大小都不变**）。
        /// 原版只有 `CardTraitDuty` / `CardTraitOath` 覆写了 `IsActive`，其余恒 true。</summary>
        public bool active;
    }

    public static class Badges
    {
        /// <summary>位子个数。原版左 3 + 右 4 = 7（**第 8 个关键词不再显示**）。</summary>
        public const int MaxSlots = 7;

        // ── 几何（**卡单位**：卡本体 2.0927 × 3.3313，和 `CardView` 同一套坐标）──────
        //
        // 🔴 **2026-09-20 重算 y（原来那套是错的）**。原版出处（逐份 JSON 实读）：
        //   · `TraitIcons` 挂在 `3DBody` 下，localPosition = **(0.296, 1.533, −0.014)**
        //   · 7 个 `TraitIconContainer*` 是 `TraitIcons` 的子节点，localScale 全 **0.750**：
        //       左列   x = **−0.8590**，y = 0.8260 / 0.4010 / −0.0390
        //       右列   x = **+0.2670**，y = 0.8260 / 0.4010 / −0.0390 / −0.4790
        //     ⇒ `TraitIcons.x + 容器x` = **∓0.563 两列**（与我们原来的 `ColX` **逐位吻合**）；
        //       `TraitIcons.y + 容器y` = **2.359 / 1.934 / 1.494 / 1.054**（**3DBody 空间**）。
        //
        //   两处关键认识：
        //   ① **3DBody 空间的 y=0 是「卡的底边」**（网格 `Card 3D WH40k` 实测 y∈[0.012, 2.973]，
        //      原点就在卡底）⇒ 换算到我们的**卡中心原点**要减半个卡高。
        //   ② 🔴 **不能再乘 0.88586**。0.88586 是 `Card 3D`（**那个网格**）自己的 localScale，
        //      而 `TraitIcons` 是 `3DBody` **的**孩子、和 `Card 3D` **平级** ⇒ 徽标活在
        //      **未缩放的 3DBody 空间**里。原注释写的「ourY = (origY/2.96 − 0.5) × 3.3313」
        //      既乘了 1.125（= 1/0.88586）又按 3.3313 拉伸，**两处都错**（这正是场卡
        //      「数值/徽标浮在卡体下面」那条 bug 的一半；另一半是 3D 体自己没下移）。
        //   判据链与实测过程见 `资料/3DBody_原版场上卡体规格.md` §四。
        const float TraitIconsY = 1.533f;                       // 3DBody 空间
        // 容器那一列的四个 y（实测，**非等距**：间距 0.425 / 0.440 / 0.440）—— 照抄，别线性化
        static readonly float[] RowYBody = { 0.826f, 0.401f, -0.039f, -0.479f };
        const float CardHalfH = CardView.CardUnitH * 0.5f;      // 3DBody 的 y=0 对齐到「卡底」

        static readonly float[] ColX = { -0.563f, 0.563f };
        static readonly float[] ColYLeft  = { BodyY(0), BodyY(1), BodyY(2) };
        static readonly float[] ColYRight = { BodyY(0), BodyY(1), BodyY(2), BodyY(3) };

        /// <summary>原版 3DBody 空间的 y → 我们的卡单位 y（**不乘 0.88586**，见上）。</summary>
        static float BodyY(int row) { return TraitIconsY + RowYBody[row] - CardHalfH; }

        /// <summary>第 i 个位（0..6）的卡单位坐标。顺序 = 原版子节点顺序（左 1/2/3、右 1/2/3/4）。</summary>
        public static Vector2 SlotAt(int i)
        {
            if (i < 3) return new Vector2(ColX[0], ColYLeft[i]);
            return new Vector2(ColX[1], ColYRight[i - 3]);
        }

        // 图标：原版 `TraitIcon` 的 SpriteRenderer size 0.8 × localScale 0.7593 × 容器 0.7502 = **0.456**
        public const float IconSize = 0.456f;
        // 🔴 **2026-09-29 更正（原来这条写错了）**：这里原来写
        //   「未激活的那张：localScale 0.49 ⇒ 0.294（原版激活/未激活**大小就差这一档**）」——
        //   错在**把两个不同的渲染器当成了同一个的两种状态**。实据（逐份 JSON 实读）：
        //   · **未激活不是「缩小版」** —— `BoardTraitIcon.Initialize` 在 `traitEnabled == false` 时
        //     只是给**同一批** `traitIcons` 换上 `disabledMaterial`（`BoardTraitIcon__Initialize.c`：
        //     `SetMaterial(渲染器, param_4 == 0 ? +0x60 /*disabledMaterial*/ : +0x80 /*originalMaterial*/)`）
        //     ⇒ **位置、大小都不变，只灰化**。
        //   · `localScale 0.49` 那一个是**另一个独立渲染器** `Trait Not Active`
        //     （`traitNotActiveIcons[]`，`BoardTraitIcon.cs` 桩里 `SpriteRenderer[] traitNotActiveIcons // 0x28`），
        //     它的贴图是**预制体里写死的**、四个实例共用同一个 sprite PathID。
        //     🔴 **2026-10-12 就地订正（铁律 5，`A1661`）**：这里原来写「**那张贴图在解包全库
        //        （24 万个文件）里找不到**（既不在 `bundle_atlasindividual_assets_40ktraiticonatlas`，
        //        也不在那份预制体包里）⇒ 我们不知道它长什么样」—— **「找不到」不成立**：
        //        它 = **`40k_Combat_Icon_Cross`**（PathID `4627540309930320646`，
        //        出在 `bundle_atlasindividual_assets_battleatlasui/Sprite/`），
        //        `素材/Warpforge原版/UI图集/图集/battleatlasui/sliced/`、`d:/2/Warpforge_tools/data/ui_extract/`
        //        与**我们工程的 `Resources/Art/ui/`** 三处都有；工程那份与原版逐字节相同。
        //        ⇒ **「今天不画」的理由从「找不到」换成「还没做」**（⛔ 别再拿「找不到」当理由）。
        //     ✅ **2026-10-18（`A1676`）已画**（`CardView.SetBadgeCross`）—— 口径见下面「未激活态第二条通道」。
        //  ⚠️ 所以 `IconSizeInactive` **已无用处**（原来也是死代码，全工程无引用）—— 留个 0 值的占位会误导，直接删。
        /// <summary>原版那层「未激活」用的材质名 —— `BoardTraitIcon.disabledMaterial` 指向的资源
        /// （`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1344813161239158161.json`，
        /// `m_Name = "Sprite Greyscale"`，`_GreyScale = 1.0`）。我们的等价物是自建 shader
        /// `CardPresentation/TraitDisabled` 的 `_GreyScale` 属性（同一个语义、同一个属性名）。</summary>
        public const string DisabledMaterialName = "Sprite Greyscale";

        // ── 未激活态的**第二条通道**：那枚 ✕（`Trait Not Active` / `40k_Combat_Icon_Cross`）──
        //  账 `A1676`（2026-10-18 落地）。
        //
        //  🔴 **与灰化是【叠加】，⛔ 不是二选一** —— 判据 = 反编译**同一个方法体里两条一起做**：
        //    `d:/2/tools/decomp_full/BoardTraitIcon__Initialize.c`
        //      · `traitIcons` 逐个：`SetMaterial(r, traitEnabled ? originalMaterial(+0x80) : disabledMaterial(+0x60))`
        //      · `traitNotActiveIcons` 逐个：`set_enabled(r, traitEnabled ^ 1)`
        //    两条在**同一次 `Initialize`** 里、用**同一个 `param_4`** 判 ⇒ 未激活时**灰化 + ✕ 同时出现**。
        //    实据（预制体 7 份逐个同形，例 `bundle_battleprefabs_vfxandmisc_assets_all/`：
        //      `MonoBehaviour_-2012836666520331328.json`）：`traitIcons` 2 个、`traitNotActiveIcons` 2 个、
        //      `disabledMaterial` → `Material_-1344813161239158161.json`（`m_Name = "Sprite Greyscale"`、`_GreyScale = 1`）。
        //
        //  **几何**（`Transform/Transform_6666491460811267008.json` = 图标 · `Transform_5341559741513440192.json` = ✕）：
        //    · 图标 `TraitIcon`：local **(0,0,0)**、scale **0.7593**
        //    · ✕ `Trait Not Active`：local **(0,0,−0.004)**、scale **0.49**（两者同父 = `Without counter`）
        //    · 两者 SpriteRenderer 的 `m_Size` 都是 **0.8 × 0.8**（`SpriteRenderer_-1467968542656586816.json`）
        //    ⇒ ✕ 边长 = 图标边长 × (0.49 / 0.7593) = **0.64533**；
        //      z 比图标**更靠前** 0.004 × 容器 scale 0.7502 ≈ **0.0030** 卡单位（卡内 z 越负越靠前）。
        //    ⚠️ 贴图 `40k_Combat_Icon_Cross.png` 是 **128×128**（工程 `Resources/Art/ui/` 那份与原版逐字节同），
        //      所以按 `CenteredQuad` 的比例内接后仍是正方形。
        /// <summary>✕ 相对图标边长的比例（原版 scale 0.49 ÷ 0.7593）。</summary>
        public const float NotActiveScale = 0.49f / 0.7593f;                 // ≈ 0.64533
        /// <summary>✕ 的边长（卡单位）—— 与图标共用同一个中心。</summary>
        public const float NotActiveCrossSize = IconSize * NotActiveScale;    // ≈ 0.29427
        /// <summary>✕ 比图标**更靠前**多少（卡单位；原版 local z −0.004 × 容器 0.7502）。</summary>
        public const float NotActiveCrossZOffset = 0.004f * 0.7502f;          // ≈ 0.0030

        // ── 脉冲高亮（原版 `BoardTraitIcon.HighlightIcon`）──────────────────────────
        //
        // 原文（`BoardTraitIcon__HighlightIcon.c`）：先把上一次的补间 `Kill` 掉、把 `contentTransform`
        // 的 `localScale` **归位**到 `originalIconSize`（`Awake` 里cache 的那份），再
        //   `DOScale(contentTransform, originalIconSize × highlightAnimScale, highlightAnimTime)`
        //   `.SetLoops(2, Yoyo).SetEase(9 /* OutCubic */)`
        // 两个字段值来自预制体（`MonoBehaviour_-2012836666520331328.json`，7 份一样）：
        //   `highlightAnimScale = 1.6` · `highlightAnimTime = 0.5`
        // ⚠️ 归位那一步别省 —— 连点两下时没有它，第二次是从「已经放大 1.6 倍」的基础上再乘，越点越大。
        /// <summary>原版 `BoardTraitIcon.highlightAnimScale`（预制体字段）</summary>
        public const float HighlightAnimScale = 1.6f;
        /// <summary>原版 `BoardTraitIcon.highlightAnimTime`（预制体字段，秒）</summary>
        public const float HighlightAnimTime = 0.5f;
        /// <summary>原版 `SetLoops(2, Yoyo)` 的那个 2</summary>
        public const int HighlightAnimLoops = 2;
        // 底板：size (0.85, 0.874) × localScale (0.866, 1.069) × 容器 0.7502
        public const float PlateW = 0.552f, PlateH = 0.701f;

        // 🔴 **2026-10-20 就地订正（铁律 5 · `W58`）—— 图标**不再**往卡外偏。**
        //    本段原来写「`Container`（图标本体）localPosition x = ∓0.287 ⇒ 图标要再往外偏 0.21525」——
        //    **那是误读**：原版那一条 `Container`（x=∓0.287）下面**还有一层** `Without counter` /
        //    `With counter`（x=±0.287，逐份实读 `Transform`），两者**互相抵消** ⇒
        //    **图标中心 = `TraitIconContainer` 容器中心 = ±0.563**（`Badges.SlotAt(i)`）。
        //    多偏那一档的后果（用户报的「词条图标超出卡面」）：图标外缘被画到 **−1.006**，
        //    超出**可见 3D 卡体**半宽 **0.9286**（`Card 3D` scale 0.88586 × 网格半宽 1.0482）**0.078**；
        //    用户实拍交叉验证：徽标中心归一化 x=**0.192**（原版容器位预测 0.197），我们旧实现预测 **0.081**
        //    ⇒ 实拍否决了这条外偏。判据全文 = `资料/普查产出_1020/W58_战斗线_场上词条判据.md` §二 / §四 #1。
        //    保留这个符号（值 = 0）而不是删掉：`Editor/BattleScene.cs` 还有一处引用，删除会编不过；
        //    那一处的断言由 `W58` 报告给出**可粘贴替换块**（⛔ 本批不能改那个文件）。
        //    ⚠️ **底板仍要偏**：`IconBackground` 的 ∓0.300 没有「抵消它的下一级」，所以下面两条照旧。
        public const float IconOutward  = 0f;               // 原版：Container(∓0.287) + Without counter(±0.287) 抵消
        public const float PlateOutward = 0.300f * 0.750f;   // = 0.225
        public const float PlateDy      = -0.020f * 0.750f;  // = −0.015（底板比图标略低）

        // ── 关键词 → 图名 ─────────────────────────────────────────────────
        //
        // 图集文件名**就是关键词名**（`Art/traits/`，78 张，见 `资料/关键词图标/`），
        // 但**大小写不统一**（`blast` / `bloodThirst` / `SpiritStone_1`），所以我们按
        // 「只留小写字母数字」归一后查表 —— 这样 `Blood Thirst` 能查到 `bloodThirst`。
        static Dictionary<string, string> _byKey;

        /// <summary>
        /// 卡面上/关键词表里出现、但**图集里没有同名图**的那几个 —— 逐条有理由，**不是兜底猜测**。
        /// 出处：`资料/卡面图标_现状与缺口.md` §二之二（逐张卡图核过）。
        /// </summary>
        static readonly Dictionary<string, string> Alias = new Dictionary<string, string>
        {
            { "destroyer", "frenzied" },    // 卡面给「毁灭者」用的图就叫 `frenzied`（`Skorpekh Destroyer` 核过）
            { "penitence", "rage" },        // `Death Cult Assassin` 卡面写 Penitence、画的是 `rage`
            { "questpoint", "questPoints" },
            { "questpoints", "questPoints" },
            { "armor", "armour" },
            { "darkpact", "markOfChaos" },  // 黑暗契约 = 混沌印记那一枚（规则书 :179「黑暗契约」）
            // ⚠️ 引擎里的关键词是 `Concussion`（规则书中文「震荡」），而图集那张图叫 `concussive.png`
            //    —— **同一个词的两种拼法**（`资料/卡面图标_现状与缺口.md` §二之二·补 第 3 条逐张核过）。
            { "concussion", "concussive" },
        };

        /// <summary>🆕 **2026-10-17（`N44`）**：规范键 → **官方枚举名**（只走上面那张 `Alias`，⛔ 不另开一张表）。
        /// 供关键词 tooltip 推官方 `CardTraitDescription/&lt;枚举名&gt;` 用
        /// （`Tooltip.LoadOfficialTraitMap` 的最后一级兜底）。查不到返回 `null`。
        /// ⚠️ **别拿它当显示名** —— 它只说明「我们家这个词在原版叫什么」，显示仍走 `CardText`。</summary>
        public static string OfficialTraitName(string canonKey)
        {
            if (string.IsNullOrEmpty(canonKey)) return null;
            string k = canonKey.Trim().ToLowerInvariant();
            string a;
            if (Alias.TryGetValue(k, out a)) return a;
            // 大小写不同的键（例 `questPoints`）→ 归一化后扫一遍（键只有十几条，开销可忽略）
            foreach (var kv in Alias)
                if (Norm(kv.Key) == Norm(k)) return kv.Value;
            // 🆕 **2026-10-17（`N44`）**：`quest` 是**阵营资源**（任务点），不是关键词图 ——
            //   ⛔ 不能进 `Alias`（那会把「任务点」的图标换成 `questPoints` 那枚、改变卡面）。
            //   但它的 **tooltip 官方枚举名**就是 `questPoints`（`CardTraitDescription/questPoints`），
            //   所以只在本口给一条：**只影响 tooltip 取官方正文，不影响图标**。
            if (Norm(k) == Norm("quest")) return "questPoints";
            return null;
            // ⚠️ `Norm` 在本类下面（`Badges.KeyOf` 用的同一个归一化）。
        }

        /// <summary>
        /// 🆕 2026-09-21：**图名 → 规范键**（卡面那条 `&lt;link>` 用它）。只列「图名 ≠ 规范键」的那几个，
        /// 其余**图名就是规范键**（`rally` / `armour` / `flying` … 小写化即可）。
        /// ⚠️ **必须和上面那张 `Alias` 成对维护** —— 正向表加了条目、这里不跟上，
        ///    卡面那条 `&lt;link>` 就会指到一个查不到的键（tooltip 只剩名字、没有解释）。
        /// ⚠️ **不能从 `Alias` 反推**，两个反例：`{"armor","armour"}` 反推会得到 `armour`→`armor`
        ///    （**错**，规范键就是 `armour`）；`questpoint` / `questpoints` 两条都指向 `questPoints`，
        ///    反推还有歧义。**手写这一张更短也更安全。**
        /// </summary>
        static readonly Dictionary<string, string> SpriteKey = new Dictionary<string, string>
        {
            { "frenzied", "destroyer" },     // 反向：图 `frenzied` = 关键词 `Destroyer`
            { "rage", "penitence" },         // 反向：图 `rage` = 关键词 `Penitence`
            { "markOfChaos", "darkpact" },   // 反向：图 `markOfChaos` = 关键词 `Dark Pacts`
            { "concussive", "concussion" },  // 反向：图 `concussive` = 关键词 `Concussion`（规则书拼法）
            // 🔴 **2026-10-11（`A1379`）补**：另外四张 `markOf*` 切片**只有这一张会出现在计划表里**
            //    （`BL19` 的 `❄ Dark Pact of Excess` → `markOfSlaanesh`；另三张 `markOfKhorne`/
            //     `markOfNurgle`/`markOfTzeentch` 全池**计划表一条都没用**）。
            //    不登记它 ⇒ `KeyOf` 落到「小写化图名」= `markofslaanesh`，而
            //    `Loc` 里**没有**这个键 ⇒ 那枚图标的 tooltip 只剩名字、没有解释
            //    （**规则书上「黑暗契约」那一条是 `darkpact`**，判据 → `Core/Loc.cs` 的 `Card_Trait/darkpact`）。
            //    ⚠️ 这一条**只改 `<link>` 的 id**，不改画哪一枚图（画哪一枚是 `Alias` 那一侧的事，
            //    而 `A1420` 那笔「BL19 该画哪一枚」的裁定与本条无关、别并起来看）。
            { "markOfSlaanesh", "darkpact" },
        };

        /// <summary>🆕 2026-09-21：图名 → 规范键（见 `SpriteKey`）。认不出就返回**小写化的图名**
        /// ——至少还能当个名字显示（`TipText.Trait` 那边会如实说「规则书里没有这个词的条目」）。
        /// <para>🔴 **2026-10-11（`A1379`）查证收口**：那条「认不出就返回小写图名」的兜底
        /// **对绝大多数图是正确的**（图集文件名就是关键词名 ⇒ 小写化恰好等于规范键），
        /// 实测样本 `"stun"`：`KeyOf("stun") = "stun"`，而 `stun` **就是**规范键
        /// （`CardDef.cs` 的 `Prefixes` 里 `new[] { "stun", "stun" }`）⇒ 运行时那枚图标外套的
        /// `&lt;link=stun&gt;` **查得到东西**，不是「指向一个空键」：
        /// `Loc` 有 `Card_Trait/stun`（`{"眩晕","Stun"}`）⇒ tooltip 有图标 + 标题；
        /// `Resources/trait_tips.json` 里 `眩晕` **在**那 61 条里 ⇒ 连规则书正文都有。
        /// ⇒ **本条不是缺陷**（原来只有「没核」这一句挂着，现在核完了）。
        /// ⚠️ 需要登记进 `SpriteKey` 的只有「**图名 ≠ 规范键**」的那几个（含 `A1379` 新补的
        /// `markOfSlaanesh`）—— ⛔ 别把 78 张图逐个登记一遍，那只会多出一张要同步维护的表。</para></summary>
        public static string KeyOf(string sprite)
        {
            if (string.IsNullOrEmpty(sprite)) return null;
            // **档位数字烘在图名里**，两种写法都有：`SpiritStone_1..5`（**带下划线**）、
            // `questPoints1..3`（**不带**）。⇒ 判据要**两种都剥**，只按 `_` 剥会得到
            // `questpoints2` 这种查不到的键（2026-09-21 自检抓到的）。
            // ⚠️ 安全前提：73 张关键词图的名字里**没有以数字结尾的**（带数字的只有上面那 8 张）。
            string s = System.Text.RegularExpressions.Regex.Replace(sprite, @"_?\d+$", "");
            if (s.Length == 0) return null;
            string k;
            if (SpriteKey.TryGetValue(s, out k)) return k;
            return s.ToLowerInvariant();
        }

        static void EnsureTable()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, string>();
            // 路径和 `CardArt.Trait` **同一份**（`Root + "traits"`）—— 两处写死迟早不一致。
            foreach (var t in Resources.LoadAll<Texture2D>(CardArt.Root + "traits"))
            {
                if (t == null) continue;
                _byKey[Key(t.name)] = t.name;      // 值用**真名**（大小写照文件）
            }
        }

        static string Key(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        /// <summary>和查表用的归一化**同一份**（只留小写字母数字）—— 给「按关键词找徽标位」那类配对用
        /// （`BattleEvent.Keyword` 写 `huntmark`、卡面写 `Hunt Mark`，得能配上）。
        /// ⚠️ 别在调用方另写一份归一化 —— 两份迟早不一致。</summary>
        public static string Norm(string s) { return Key(s); }

        /// <summary>
        /// 关键词 → `Art/traits/` 里的图名；**认不出就返回 null**（调用方必须判 —— 红线：宁可少画，不给错图）。
        /// 「Armour 2」「Blast 2.」这类**带数值**的先剥掉数值；「Rally: …」这种**带正文**的只取冒号前。
        /// </summary>
        public static string SpriteOf(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            string k = keyword.Trim();
            int colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            k = System.Text.RegularExpressions.Regex.Replace(k, @"[\s\.]*\d+[\s\.]*$", "");
            string key = Key(k);
            if (key.Length == 0) return null;

            EnsureTable();
            string name;
            if (_byKey.TryGetValue(key, out name)) return name;
            if (Alias.TryGetValue(key, out name) && _byKey.ContainsKey(Key(name))) return name;
            return null;
        }

        /// <summary>
        /// 🔴 **能进【场上徽标】的那 47 个关键词** —— 原版 `hasIcon && isInPlayIcon && traitIcon != null`
        /// 在**本 build 数据**上的逐条结果（判据链见文件头；数据 = `数据/游戏数据/trait_collection.json`
        /// 的 77 条 `CardTrait`，2026-10-24 现算）。键 = <see cref="Key(string)"/> 归一化后的 trait 名。
        ///
        /// 🔴 **2026-10-24（`W10`）本表取代了原来的黑名单 `NotInBoardBadges = {"talent"}`**：
        /// 那条黑名单的判据是**用户 2026-10-12 真机口径**（⛔ 不是原版读数），只能挡住 1 条；
        /// 现在按原版那三列筛，而它挡的那一条（`talent`）**被原版数据自己证明是对的**
        /// （`talent` 的 `isInPlayIcon == false`）—— 用户口径与原版读数在这一条上**一致**。
        ///
        /// **被挡在门外的 17 个（有图、但原版不上场上徽标）**，逐键备查：
        ///   · `hasIcon == false`（5）：`ambush` `berzerk` `flying` `huntMark` `markerlight`
        ///   · `hasIcon == true` 但 `isInPlayIcon == false`（12）：
        ///     `armour` `companion` `ephemeral` `fast` `flank` `pack` `rally` `talent` `teleport` `tide`
        ///     `uprising` `vanguard`
        ///   （另有 13 条两列里有 true、但 `traitIcon == null` ⇒ 本来就没图：`combo` `daemonicAllegiance`
        ///    `dodge` `dontReturnFerocity` `firstStrike` `large` `mekaniak` `notDestroyRemnant` `oathDouble`
        ///    `oathInAllTurns` `oathTripleActivation` `piercing` `unstoppable` —— 它们进不进本表都画不出来。
        ///    77 = 47 + 17 + 13。）
        ///
        /// ⚠️ **用户点名的「护卫」**：官方中文表里没有「护卫」这个词条（`vanguard = 先锋`、
        /// `shield = 盾牌`、`frontLine = 前线`）⇒ 他说的多半是 `vanguard`。**`vanguard` 原版压根不上
        /// 场上徽标**（它靠「登场 idle 特效」那条通道表现）⇒ 本条会把它挡掉，正是用户要的效果。
        ///
        /// ⚠️ **只管【场上徽标】这一处**：卡面效果文字里的**行内图标**走的是另一条路
        /// （`CardIcons`/`CardText.KeywordSegment` 的 `&lt;sprite name="talent"&gt;` —— 原版卡面
        /// 确实印 `〔纸卷〕Talent: …`，见 `工具/_fix_a7_talent.py` 与 `Editor/IconSetup.cs:480`）
        /// ⇒ ⛔ **两处别一起关**（关掉那一条会让 `BattleScene` 里 `SpriteOf("Talent: …")` 那族断言与卡面一起红）。
        /// </summary>
        static readonly HashSet<string> InPlayIconKeys = new HashSet<string>
        {
            "agenda", "artifice", "backlash", "blast", "blind", "bloodthirst", "camouflage",
            "cantattack", "codex", "concussive", "cruelty", "duty", "ecstasy", "ferocity",
            "frenzied", "invulnerable", "longrange", "markofchaos", "markofkhorne", "markofnurgle",
            "markofslaanesh", "markoftzeentch", "mob", "oath", "pindown", "pray", "rage",
            "regeneration", "regiment", "relentless", "remnant", "resolution", "sentry", "shield",
            "shuriken", "slay", "sniper", "stealth", "stimulation", "stomp", "strike", "stun",
            "swarm", "synapse", "unstable", "vulnerable", "waystone",
        };

        /// <summary>那 47 个键（自检读口，已排序；改表时同步改
        /// `资料/普查产出_1024d/W10_选择器与词条_执行报告.md` 的名单）。</summary>
        public static string[] InPlayIconKeysForTest
        {
            get { var a = new string[InPlayIconKeys.Count]; InPlayIconKeys.CopyTo(a); System.Array.Sort(a); return a; }
        }

        // ── 🆕 2026-10-25（第 4 轮 · G 语义复核判定的真缺口）：**较松的那一族** ──────────────────
        //  判据 = `CardTraitCollection__GetTraitListForCardIcons.c`（100 行）—— 它**只要求**
        //  `_hasIcon(0x21) + _traitIcon != null(0x18)`，**不要求** `_isInPlayIcon(0x22)`；
        //  与 `GetTraitInPlayListForIcons`（= 上面那 47 条）**差 12 个键**：
        //  `armour` · `companion` · `ephemeral` · `fast` · `flank` · `pack` · `rally` · `talent` ·
        //  `teleport` · `tide` · `uprising` · `vanguard`（有图标、但原版**不算场上图标**）。
        //  ⚠️ 两个筛选器**用途不同、都要留着**：场上那 7 枚徽标走 `PlaysInPlayIcon`（47 条），
        //     卡面/收藏/详情窗那种「这卡有哪些图标」走 `PlaysCardIcon`（59 条）。
        //  ⚠️ 表由 `数据/游戏数据/trait_collection.json`（77 条，`hasIcon && traitIcon`）**现算**抄来，
        //     数据变了重跑同一段脚本；⛔ 别凭印象加减。
        static readonly HashSet<string> CardIconKeys = new HashSet<string>
        {
            "agenda", "armour", "artifice", "backlash", "blast", "blind", "bloodthirst", "camouflage",
            "cantattack", "codex", "companion", "concussive", "cruelty", "duty", "ecstasy", "ephemeral",
            "fast", "ferocity", "flank", "frenzied", "invulnerable", "longrange", "markofchaos",
            "markofkhorne", "markofnurgle", "markofslaanesh", "markoftzeentch", "mob", "oath", "pack",
            "pindown", "pray", "rage", "rally", "regeneration", "regiment", "relentless", "remnant",
            "resolution", "sentry", "shield", "shuriken", "slay", "sniper", "stealth", "stimulation", "stomp",
            "strike", "stun", "swarm", "synapse", "talent", "teleport", "tide", "unstable", "uprising",
            "vanguard", "vulnerable", "waystone",
        };

        /// <summary>那 59 个键（自检读口，已排序）。</summary>
        public static string[] CardIconKeysForTest
        {
            get { var a = new string[CardIconKeys.Count]; CardIconKeys.CopyTo(a); System.Array.Sort(a); return a; }
        }

        /// <summary>这个**图名**能不能进「卡面图标」那一族 —— 原版 `GetTraitListForCardIcons` 的等价物：
        /// **只要求 `hasIcon && traitIcon != null`**（59 条），**不要求 `isInPlayIcon`**。
        /// <para>⚠️ 与 <see cref="PlaysInPlayIcon"/> 的区别就是那 12 个键（见 `CardIconKeys` 的注释）——
        /// 场上那 7 枚徽标**仍走严的那个**，⛔ 别把这里换过去。</para></summary>
        public static bool PlaysCardIcon(string sprite)
        {
            if (string.IsNullOrEmpty(sprite)) return false;
            return CardIconKeys.Contains(Key(sprite));
        }

        // ── 「去重告警」与「本轮观测」是**两个集合** ──────────────────────────────────
        //  ⚠️ **2026-10-25 更正（执行代理 `S` · 任务线 `T07` · 铁律 5 留痕）**：原来**只有一个**
        //  `HashSet`（当时叫 `_notInDataWarned`），它同时是 ①「每个图名只告警一次」的**去重集**
        //  和 ②`UnknownIconSpritesForTest`（自检读「这一趟见过哪些」）的**数据源** ⇒
        //  `ResetUnknownForTest()` 一 `Clear()`，**告警也一起重新武装** ⇒ 自检
        //  （`Editor/BadgeFilterAssert.cs` 每轮开扫前先 Reset）**每轮把同样那 17 个图名再打 17 行
        //  `[Badges]` 日志**，而那 17 条在断言里被当成「已知/预期」解释掉 ⇒ **噪声被当成正常**。
        //  现在拆开，各管一件事（⛔ 别再把两件事塞回同一个集合）：
        /// <summary>去重告警集：**一辈子**每个图名只放行一次 `Debug.LogWarning`。
        /// <para>⛔ `ResetUnknownForTest()` **不清它** —— 「只告警一次」的语义不该被自检的读口左右。</para></summary>
        static readonly HashSet<string> _notInDataWarned = new HashSet<string>();
        /// <summary>本轮观测集：`PlaysInPlayIcon` 每遇到一个**不在白名单里**的图名就记一笔。
        /// <para>`ResetUnknownForTest()` **只清它** ⇒ 自检读到的是「这一趟见过的完整名单」。</para></summary>
        static readonly HashSet<string> _notInDataSeen = new HashSet<string>();
        /// <summary>真打出过多少行 `[Badges]` 告警（**只增不减**，不随 `ResetUnknownForTest` 归零）。
        /// 自检用**差值**断「Reset 之后重跑同一批 ⇒ 新增 0 行」。见
        /// <see cref="UnknownIconWarnCountForTest"/>。</summary>
        static int _notInDataWarnCount;

        /// <summary>自检用：自进程启动以来 `[Badges]` 未知图名告警**真打出的行数**（单调递增）。
        /// <para>用法：判断「这一批有没有再告警」时取**差值**（`after - before`），⛔ 别比绝对值
        /// —— 它不是「这一趟」的计数器（那一趟的名单看 <see cref="UnknownIconSpritesForTest"/>）。</para></summary>
        public static int UnknownIconWarnCountForTest { get { return _notInDataWarnCount; } }

        /// <summary>自检用：**这一趟**（自上次 <see cref="ResetUnknownForTest"/> 起）遇到过的、
        /// **不在那 47 条白名单里**的图名（去重、已排序）。
        /// <para>⚠️ 数据源是 <see cref="_notInDataSeen"/>（**本轮观测集**），**不是**告警去重集
        /// —— 两者 2026-10-25 起是分开的（见上）。</para>
        /// <para>🔴 **2026-10-24（执行代理 `W24` · `D2_BattleScene11红诊断.md` §11）就地订正（铁律 5）**：
        /// 本段原来写「正常卡池里**应当是空的**（78 张图里除那 17 张外都能对上）」—— **自相矛盾**：
        /// 这个集合记的正是 <see cref="PlaysInPlayIcon"/> 里**被原版三列（`hasIcon` / `isInPlayIcon` /
        /// `traitIcon`）挡掉**的那些图名，而其中 **17 条本来就该在里面**
        /// （`Editor/BadgeFilterAssert.cs` 里 `HasIconFalse` 5 条 + `NotInPlayIcon` 12 条）。
        /// ⇒ 正常卡池里它**应当恰好是那 17 条**，⛔ **不是空**。</para>
        /// <para>⚠️ 真正那一格「图集里有、但 `trait_collection.json` 的 77 条里没有」**本集合从来没记过**
        /// （判它需要一份 77 条的完整键集，运行期读不到那份 JSON）。</para>
        /// <para>⚠️ 它是**这一趟累积**的（自上次 <see cref="ResetUnknownForTest"/> 起，任何 `Badges.For`
        /// 都会加数）⇒ 要「只看这一趟」先调 <see cref="ResetUnknownForTest"/>。</para></summary>
        public static string[] UnknownIconSpritesForTest
        {
            get { var a = new string[_notInDataSeen.Count]; _notInDataSeen.CopyTo(a); System.Array.Sort(a); return a; }
        }

        /// <summary>自检用：清空 <see cref="UnknownIconSpritesForTest"/>（= **本轮观测集**）
        /// （全池扫那一趟开始前调一次，否则数里混着更早调用的残留 —— `D2` §11）。
        /// <para>⚠️ **2026-10-25 更正（执行代理 `S` · `T07` · 铁律 5）**：本段原来写着
        /// 「这个集合同时也是『每个新键只告警一次』的去重集 ⇒ 清空之后同样的图名**会再告警一次**（有意的）」
        /// —— **那句已不成立**：两个集合拆开了，本方法**只清观测集**，
        /// 去重告警集 `_notInDataWarned` **不清** ⇒ Reset 之后同样的图名**不会再告警**
        /// （自检因此不再每轮重打那 17 行日志），而「这一趟的名单」仍然完整
        /// （观测集从空开始、本轮遇到就记）。旧写法见 `W24` / `W26` 的报告。</para></summary>
        public static void ResetUnknownForTest() { _notInDataSeen.Clear(); }

        /// <summary>这个**图名**（`Art/traits/` 的文件名，= `SpriteOf` 的返回值）能不能进场上徽标。
        /// 判据 = 归一化后是否在 <see cref="InPlayIconKeys"/> 里（= 原版那三列在本 build 数据上的等价值）。
        /// <para>⚠️ **认不出的一律 false**（原版：不在 `TraitCollection` 里 ⇒ 永远进不了那张列表），
        /// 且**每个新键只告警一次**（⛔ 不静默：真出现这种词说明数据/图集对不上，要人看一眼）。
        /// 🔴 **2026-10-25（`T07`）**：这里记两个集合 —— <see cref="_notInDataSeen"/>（**每遇到就记**，
        /// Reset 会清，供自检读「这一趟」）与 <see cref="_notInDataWarned"/>（**一辈子只放行一次告警**，
        /// Reset 不清）。⛔ 别把两句合成一句「`Add` 成功才记」—— 那正是旧的「一集两用」写法。</para></summary>
        public static bool PlaysInPlayIcon(string sprite)
        {
            if (string.IsNullOrEmpty(sprite)) return false;
            if (InPlayIconKeys.Contains(Key(sprite))) return true;
            _notInDataSeen.Add(sprite);                 // 本轮观测集：每遇到就记（Reset 只清这个）
            if (_notInDataWarned.Add(sprite))           // 去重告警集：一辈子只放行一次
            {
                _notInDataWarnCount++;
                Debug.LogWarning("[Badges] 图 `" + sprite + "` 不在「场上图标」那 47 条里（判据 = `trait_collection.json`"
                               + " 的 `hasIcon && isInPlayIcon && traitIcon != null`）⇒ 按原版口径**不画**这枚徽标。"
                               + " 若它确实该画：先重跑 `工具/export_trait_collection.py` 核数据，再补进 `Badges.InPlayIconKeys`。");
            }
            return false;
        }

        /// <summary>
        /// 从**单位当前的关键词表**挑出要显示的那几个（最多 7 个，顺序稳定 ⇒ 对局可复现）。
        ///
        /// 🔴 **判据（2026-10-24 起 = 原版真值表）**：一个关键词要**同时**满足
        /// ① `hasIcon` ② `isInPlayIcon` ③ 有图标 —— 落在 <see cref="InPlayIconKeys"/> 那 47 条里
        /// （判据链与数据来源见文件头）。**不再是**「凡是查得到图标的都显示」那种替身口径。
        /// 也就是说「引擎当前认为这个单位身上有什么」**且**「原版把它算作场上图标」，卡上才画。
        /// 认不出图标的**跳过并计数**，由自检盯着，不静默吞掉。
        /// <para>🔴 **2026-10-24（`W10`）就地订正（铁律 5）**：本段原来写「那两条开关的逐 trait 取值
        /// 本地取不到 ⇒ 本方法今天做不到逐条对齐」+「能取到的那一天，要按 `HasIcon && IsInPlayIcon` 收窄」——
        /// **两句都已落地**：数据 2026-10-23 解出来了（`trait_collection.json` 77 条全带），
        /// 本方法今天就按那两列收窄（`PlaysInPlayIcon`）。留痕保留，免得下一个会话以为还没做。</para>
        /// <para>🔴 **2026-10-24 新增 `isPlayer`（原版那一支别漏）**：`BattleCardUI__UpdateTraitIcons.c:40-46`
        /// 在筛之前还有一道 —— **敌方的 `ambush` 单位（`!isPlayer`）整排一枚都不画**。
        /// 调用方传「这张卡是不是我方的」（默认 `true` = 我方/不判断途）。⚠️ 不传 ⇒ 那一条**不会生效**
        /// 且**静默**（默认值没法自己知道阵营）⇒ 接的时候别忘（现状：`BattleDriver.ToCardData` 还是默认值，
        /// 见 `资料/普查产出_1024d/W10_选择器与词条_执行报告.md` 的「没做到」一节）。</para>
        /// 🔴 **2026-10-21 更正（`A1361` · 铁律 5）**：这一句原来举的例子是「（`Talent` / `Secret` /
        ///   `Lord Commander` 这类**没有图的**）」—— **`Talent` 那半个例子不成立**：
        ///   `Resources/Art/traits/talent.png` 在（9070 B）、`Resources/Fonts/Warpforge Trait TextSprites.asset`
        ///   里有字形 `m_Name: talent`，`资料/关键词图标_现状与总表.md:146`（第 54 行 `Talent`）标的就是 ✓
        ///   ⇒ 它**不是**「没有图的」。⚠️ 同句剩下的 `Secret` / `Lord Commander` **另论、本批没动**：
        ///   `Secret` 在卡池里是 **`subtype`、不是关键词**（5 张）；`Lord Commander` 是卡名
        ///   （`AM_Lord_Commander`）＋ `AM3` 的一条 `desc` 文本 ⇒ 「它们算不算关键词」是**另一笔账**。
        ///   （本句只改文案 —— 判据、逻辑、去重规则一个字节没动。）
        /// </summary>
        public static List<Badge> For(IEnumerable<KeyValuePair<string, int>> keywords, string orderHint = null,
                                      IReadOnlyCollection<string> numericKeys = null,
                                      System.Func<string, bool> isActive = null,
                                      bool isPlayer = true, bool cardIcons = false)
        {
            var list = new List<Badge>();
            if (keywords == null) return list;

            // 🔴 **2026-10-24（`W10`）· 原版那道「敌方伏击整排不画」的闸**（`BattleCardUI__UpdateTraitIcons.c:40-46`）：
            //    先把 7 格全 `Toggle(false)`，然后 `if (HasCurrentTrait(card, 0x4b0 /*ambush*/) && !isPlayer) return;`
            //    ⇒ 注意它**不看有没有翻面**、也不看别的词条 —— 只要「对面 + 带 ambush」就整排空。
            //    ⚠️ 先 `Copy` 再判：`keywords` 可能是**只能走一遍**的惰性序列，判两次就空了。
            var ordered = new List<KeyValuePair<string, int>>();
            foreach (var kv in keywords) ordered.Add(kv);
            if (!isPlayer && HasKeyword(ordered, "ambush")) return list;

            // 顺序：**卡面效果文字里先出现的排前面**（和玩家在卡上读到的词序一致），
            // 文字里没有的（后面加上来的增益）按名字排 —— 全程确定 ⇒ 对局可复现。
            //
            // 🔴 **这是替身，不是原版次序**（2026-10-12 · `A1613②` 现读反编译坐实，全文见文件头第 4 条）：
            //    原版 `CardTraitCollection.GetTraitInPlayListForIcons` **一条排序都没有** —— 它按
            //    `TraitCollection` 那个 `List` 的**索引序**逐条 `Add`（唯一的「次序规则」是**按 trait id 去重**、
            //    先出现者赢），`BattleCardUI.UpdateTraitIcons` 也按索引序把它写进那 7 个槽
            //    ⇒ **原版次序 = 卡的数据作者序**，不是文本序、也不是字典序。
            //    ⚠️ 那份资产（`CardTraitCollection.TraitCollection`）**本地没有**（只有 MonoScript、零实例）
            //    ⇒ 今天**判不出**逐卡原版次序；这段排序是**在那之前最接近玩家阅读顺序的替身**。
            //    ⚠️ `A1406` 那次只动了**卡面关键词段**（`CardText.KeywordSegment`）、**没动这里**。
            string hint = (orderHint ?? "").ToLowerInvariant();
            ordered.Sort((a, b) =>
            {
                int ia = hint.IndexOf(Strip(a.Key).ToLowerInvariant(), System.StringComparison.Ordinal);
                int ib = hint.IndexOf(Strip(b.Key).ToLowerInvariant(), System.StringComparison.Ordinal);
                if (ia < 0) ia = int.MaxValue;
                if (ib < 0) ib = int.MaxValue;
                if (ia != ib) return ia.CompareTo(ib);
                return string.CompareOrdinal(a.Key, b.Key);
            });

            foreach (var kv in ordered)
            {
                if (list.Count >= MaxSlots) break;
                string spr = SpriteOf(kv.Key);
                if (spr == null) continue;
                // 🔴 **原版那三列（`hasIcon && isInPlayIcon && traitIcon != null`）**——
                //    判据与逐键名单见 `InPlayIconKeys`。**按【图名】判、不按关键词判**：
                //    别名（`Destroyer`→`frenzied` / `Armor`→`armour` / `Dark Pact`→`markOfChaos` …）
                //    在这里天然归一到同一条 trait 上，⛔ 不用再维护第二张表。
                //    ⚠️ 放在 `SpriteOf` 之后：这位仍然算「认得出图」⇒ **不会**被当成
                //    「认不出图标」计进那条诊断（两者是不同的缺口）。
                if (!(cardIcons ? PlaysCardIcon(spr) : PlaysInPlayIcon(spr))) continue;
                // ↑ 🔴 两个筛选器**用途不同**（见 `CardIconKeys` 的注释）：`cardIcons:false`（缺省）= 场上那 7 枚徽标，
                //   走严的 47 条（要求 `isInPlayIcon`）；`cardIcons:true` = 「这张卡有哪些图标」那一族，
                //   走松的 59 条（只要求 `hasIcon + traitIcon`）。⛔ 别把缺省值改成松的。
                // 角标（2026-09-29 改口径，见 `CarriesValue`）；
                // 激活态（原版 `BoardTraitIcon.Initialize` 的第 4 个参数）由调用方给 ——
                // 原版只有 `Duty` / `Oath` 两个子类覆写了 `IsActive`，其余恒 true。
                bool act = isActive == null || isActive(kv.Key);
                list.Add(new Badge
                {
                    sprite = spr,
                    key = kv.Key,
                    counter = CarriesValue(kv.Key, kv.Value, numericKeys) ? Mathf.Max(1, kv.Value) : 0,
                    active = act
                });
            }
            return list;
        }

        /// <summary>
        /// 这个关键词**画不画角标数字**。
        ///
        /// 🔴 **原版判据（2026-09-29 读反编译坐实）**：`BoardTraitIcon.Initialize(icon, counter, enabled)`
        ///    —— `counter &lt; 1` ⇒ 换成 `Without counter` 那个子物体、否则 `With counter`
        ///    （`d:/2/tools/decomp_full/BoardTraitIcon__Initialize.c` 尾部两分支）。
        ///    那个 `counter` = `EntityScript.GetCurrentTraitValueWithModifiers(entity, traitId)`
        ///    = `traitData.值 + Σ 同 id 的 modifier`，**钳 ≥ 0**（`EntityScript__GetCurrentTraitValueWithModifiers.c`）。
        ///    而词条的基值来自 `CardTrait.GetNewTrait(..., int defaultValue = **0**)`
        ///    —— 卡面没写数字的词条基值就是 **0** ⇒ **不画角标**。
        ///
        /// ⚠️ **不能直接照抄 `值 ≥ 1`**：我们的引擎对「没有数字」的关键词**兜底给 1**
        ///    （`KeywordTable.FirstNumber`），照抄会把 `Flying` / `Rally` 这些**全都画上一个 1**。
        ///    ⇒ 判据按下面三条的**并集**，第 ① 条就是原版那条判据在我们数据里的等价物：
        ///
        /// ① **卡面原文里写了数字**（`CardDef.NumericKeywords`，由 `KeywordTable.HasNumber` 抽出来）
        ///    —— 实测卡池 1126 张里带数字的关键词只有
        ///    `Armour / Blast / Tide / Regeneration / Shuriken / Companion / Ecstasy / Oath / Sentry`
        ///    这几个（`cards_engine.json` 全量扫描），**正是我们原来那张手写表的内容**。
        /// ② **引擎当前值 ≥ 2** —— 已经被 modifier 抬上去的（原版这时也会画）。
        /// ③ **规则书「带数值?」那一列**（下面那张表）—— 留给**运行时授予**、卡面上本来不印数字的
        ///    （`Hunt Mark` / `Markerlight` / `Vulnerable` 这一族，出现在效果文字而不是关键词行）。
        ///
        /// 🔴 **两个调用点的实参不同 —— 就写在这里**（2026-10-11 · `A1332` 收口）：
        ///   · **徽标那一档**（`Badges.For:288`）传 `(kv.Key, kv.Value, numericKeys)` ⇒ **①②③ 三档全接**
        ///     （喂进来的是单位**当前**的关键词表，光环/授予抬起来的词走 ②）；
        ///   · **卡面关键词行那一档**（`CardText.KeywordSegment`，现读签名 `:230-232`、实参 `:253-261`）——
        ///     🔴 **`A1341` 起【也接 ① 了】**（2026-10-10）：`KeywordSegment` 加了**带默认值的可选形参**
        ///     `IReadOnlyCollection<string> numericKeys = null`，`BattleDriver.FaceTextFull:11653` 传
        ///     `c.NumericKeywords` ⇒ **两处现在都是 ①②③ 三档全接**。
        ///     ⚠️ **原来这里写「传 `(key, kv.Value, null)` ⇒ 只接 ②③」—— 那句从 `A1341` 起不成立**（铁律 5 订正痕）。
        ///   ✅ 两处**共用这一个函数体**，**改前改后逐张等价**（全池 1126 张离线复算：过门槛的 (卡,键)
        ///     **改前 203 / 改后 203 · 差异 0 处**）—— 因为今天全池「带数字的原文关键词头」**31 种全是
        ///     `Word N` 形态、词干一种不落全落进 ③ 那 12 个键里** ⇒ **接上 ① 不会多印一个数字**。
        ///     ⚠️ 这条等价**只是今天的读数**：真会分家的情形 = 「卡池出现带数字、又不在 ③ 表里的词」（`A1332`）。
        ///
        /// ⚠️ **原来这条注释里举的反例（`SpiritStone_1..5` / `questPoints1..3`「原版画角标、我们不画」）
        ///    2026-09-29 查实是【误记】**：那两个是 **HUD 的阵营资源计数**，不是场上徽标 ——
        ///    全卡池 1126 张的关键词行里**一个都没有**（`spiritstone` 是 `KeywordTable` 的常量、
        ///    `questPoints` 是 `PlayerState` 的资源，都进不了 `UnitState.Keywords`）。
        /// </summary>
        public static bool CarriesValue(string keyword, int value = 1, IReadOnlyCollection<string> numericKeys = null)
        {
            string key = Key(Strip(keyword));
            // ① 卡面原文里写了数字（数据侧 = 原版那条判据的等价物）
            if (numericKeys != null && Contains(numericKeys, key)) return true;
            // ② 引擎当前值已经比兜底的 1 大
            if (value >= 2) return true;
            // ③ 规则书那张表（旁证 / 运行时授予的那一族）
            switch (key)
            {
                case "armour": case "blast": case "companion": case "ecstasy":
                case "markerlight": case "oath": case "regeneration": case "sentry":
                case "shuriken": case "tide": case "vulnerable": case "huntmark":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>关键词表里有没有这一条（**归一化比较**：`Ambush` / `ambush` / `Ambush 1` 都算）。
        /// 原版 `HasCurrentTrait(card, 0x4b0)` 的等价物（我们这侧按词名判，不按 trait id）。</summary>
        static bool HasKeyword(List<KeyValuePair<string, int>> keys, string want)
        {
            if (keys == null || string.IsNullOrEmpty(want)) return false;
            string w = Key(Strip(want));
            for (int i = 0; i < keys.Count; i++)
                if (Key(Strip(keys[i].Key)) == w) return true;
            return false;
        }

        /// <summary>`IReadOnlyCollection` 上没有 `Contains`（那是 `ICollection` 的）——
        /// 集合只有两三个元素，线性扫就好，不值得为此引 `System.Linq`。</summary>
        static bool Contains(IReadOnlyCollection<string> set, string key)
        {
            foreach (var s in set) if (s == key) return true;
            return false;
        }

        /// <summary>去掉数值后缀与冒号正文（`"Armour 2"` → `"Armour"`）—— 只在排序时用，判据仍是 <see cref="SpriteOf"/>。</summary>
        static string Strip(string keyword)        {
            string k = (keyword ?? "").Trim();
            int colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            return System.Text.RegularExpressions.Regex.Replace(k, @"[\s\.]*\d+[\s\.]*$", "").Trim();
        }
    }
}
