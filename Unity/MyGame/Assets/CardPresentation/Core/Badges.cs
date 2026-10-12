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
//     15+ 个卡事件都会触发重算）。守卫：非单位直接 return；**伏击（ambush 且未翻面）不显示**；
//     每次重算先把 7 个位全部 `Toggle(false)`。图标取 `CardTrait._traitIcon`（运行时按
//     卡当前拥有的 trait 查表），角标数值取 `GetCurrentTraitValueWithModifiers()`，
//     激活态取 `CardTrait.IsActive(CardScript)`。
//  ③ 几何：`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/TraitIconContainer*.json`
//     （7 份）—— 父链 `TraitIconContainer ← TraitIcons ← 3DBody ← Board Elements ← CardPrefab`。
//
// 🔴 **本地查不到、因而是「我们挑的」**（按项目红线必须写明）：
//  · **哪些关键词进这 7 个位** —— 我们的判据见 `For(...)`（= 「凡是查得到图标的都显示」，
//    **再减掉黑名单 `NotInBoardBadges`** —— 那一条的判据是**用户 2026-10-12 口径**、⛔ 不是原版读数）。
//    🔴 **2026-10-11（`A1362`）就地订正（铁律 5）：本行原来写「原版的筛选器
//    `CardTraitCollection.GetTraitInPlayListForIcons` 的**方法体没被反编译**」—— 那句【不成立】。**
//    现读：`d:/2/tools/decomp_full/CardTraitCollection__GetTraitInPlayListForIcons.c`（**202 行，全的**），
//    调用点也核了（`BattleCardUI__UpdateTraitIcons.c:59` 调它，链是
//    `CardScript.UpdateTraitIcons` → `BattleCardUI.UpdateTraitIcons`）。
//    **原版判据（从那份方法体读出来的）** = 一个 trait 进这 7 个位要**同时**满足：
//      ① `CardTrait._hasIcon`（偏移 `0x21`）—— getter `CardTrait__get_HasIcon.c` 实测就是读它；
//      ② `CardTrait._isInPlayIcon`（偏移 `0x22`）—— `CardTrait__get_IsInPlayIcon.c` 同；
//      ③ `CardTrait._traitIcon != null`（偏移 `0x18`，`UnityEngine_Object__op_Inequality` 那一步）；
//      ④ （第一支循环另加）**这张卡的 `CardTraitData._hide == false`**（偏移 `0x20` = `CardTraitData._hide`，
//         见 `CardTraitData.cs`；`CardTrait.GetNewTrait(`, bool hideTrait = true`)` 那个参数）。
//    另有一条**同族但不同用途**的筛选器：`CardTraitCollection__GetTraitListForCardIcons.c`（100 行）
//    —— 那条**只要求 ① + ③**（不要求 `_isInPlayIcon`）⇒ 两者差的那一族 = 「有图标、但不算**场上**图标」。
//    🔴 **仍然取不到的是那两个开关的【逐 trait 取值】**：`CardTrait` 的**资产本体本地没有**。
//    实据（2026-10-11 现搜，⛔ 不是「没找到」而是「确实不在」）：`可读的 exports = 24.7 万文件`，
//    按字段名全盘搜 `_traitIcon` / `_hasIcon` / `_isInPlayIcon` / `_traitEffects` / `customObject`
//    **全 0 命中**（同一条 grep 对 `m_Name` 命中 12.8 万 ⇒ 仪器有牙）；`CardTrait` 的 MonoScript
//    **在**（`assets_full/globalgamemanagers/MonoScript/MonoScript_1795.json`，`m_ClassName: "CardTrait"`），
//    但**一个实例都没有**；`assets_full/resources/MonoBehaviour/` 是**空目录**
//    （`resources.assets` 2039 个对象里 **616 个导出失败**）⇒ 它多半在没解出来的那一桶 / 远端包里。
//    ⇒ **`For(...)` 今天只能拿「查得到图标」当那两条开关的**替身**，这条如实标着。**
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
        //        ⚠️ 要画它得先定口径：`Trait Not Active` 这个独立渲染器与 `disabledMaterial`（灰化）
        //        **是两条并存的通道**（见上面那条更正）—— **另立一条账**，别在这儿顺手补。
        //   ⚠️ 所以 `IconSizeInactive` **已无用处**（原来也是死代码，全工程无引用）—— 留个 0 值的占位会误导，直接删。
        /// <summary>原版那层「未激活」用的材质名 —— `BoardTraitIcon.disabledMaterial` 指向的资源
        /// （`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1344813161239158161.json`，
        /// `m_Name = "Sprite Greyscale"`，`_GreyScale = 1.0`）。我们的等价物是自建 shader
        /// `CardPresentation/TraitDisabled` 的 `_GreyScale` 属性（同一个语义、同一个属性名）。</summary>
        public const string DisabledMaterialName = "Sprite Greyscale";

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

        // 🔴 **2026-09-20：图标与底板各自还要往「卡外」偏一截**（左右镜像）。原来只记了底板的 0.30，
        //    而且把图标的偏移**漏了** ⇒ 7 个徽标整体偏**内** 0.215 卡单位（≈卡宽的 10%）。
        //    实据（`TraitIconContainer*` 的子节点，`Transform/` 逐份实读）：
        //      · `Container`（**图标本体**）localPosition x = **∓0.287**，y = 0
        //      · `IconBackground`（**底板**）localPosition = (**∓0.300**, **−0.020**)
        //    ⚠️ 这两个数活在**容器自己的空间**里，容器 scale = **0.750**，所以乘 0.750 才是卡单位。
        //    ⚠️ 原注释写「底板偏 0.30，我们摆 0（正后方）—— 因为我们是平面 2D 卡」：**那条前提已经作废**
        //       （徽标只在**场上**显示，而场上是 3D 卡体）⇒ 按原版摆回去。
        public const float IconOutward  = 0.287f * 0.750f;   // = 0.21525
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
        /// 🔴 **不进【场上徽标】的那几个关键词**（黑名单）。**判据 = 用户 2026-10-12 真机口径**，
        /// ⛔ **不是「原版读数」** —— 原版那条判据是 `HasIcon && IsInPlayIcon && TraitIcon != null`
        /// （`CardTraitCollection__GetTraitInPlayListForIcons.c`，202 行，全文见文件头），而
        /// **那两个逐 trait 开关的取值本地取不到**（实据见文件头）⇒ 这条**只能按用户口径**。
        ///
        /// · **`talent`（天赋）** —— 用户原话：「战将和部队卡面上有 7 格图标位，什么词条放什么图标。
        ///   **但是天赋这个图标不要放。你现在放了**」。
        ///   我们原来画它的**原因**是「它有图」（`Resources/Art/traits/talent.png` 9070 B、
        ///   `Resources/Fonts/Warpforge Trait TextSprites.asset` 里有字形 `talent`、
        ///   `资料/关键词图标_现状与总表.md:146` 标 ✓）—— ⚠️ **「有图」不等于「该进徽标」**，两件事。
        ///   原版那边它大概率是 **`IsInPlayIcon == false`**（同族的 `GetTraitListForCardIcons` 只要
        /// `HasIcon && TraitIcon != null` ⇒ 两者差的那一族就是「有图标、但不算**场上**图标」），
        /// 但那**是推断，不是读数** ⇒ 本条**如实标成用户口径**。
        ///
        /// ⚠️ **只管【场上徽标】这一处**：卡面效果文字里的**行内图标**走的是另一条路
        /// （`CardIcons`/`CardText.KeywordSegment` 的 `&lt;sprite name="talent"&gt;` —— 原版卡面
        /// 确实印 `〔纸卷〕Talent: …`，见 `工具/_fix_a7_talent.py` 与 `Editor/IconSetup.cs:480`）
        /// ⇒ ⛔ **两处别一起关**（关掉那一条会让 `BattleScene` 里 `SpriteOf("Talent: …")` 那族断言与卡面一起红）。
        /// </summary>
        static readonly HashSet<string> NotInBoardBadges = new HashSet<string> { "talent" };

        /// <summary>
        /// 从**单位当前的关键词表**挑出要显示的那几个（最多 7 个，顺序稳定 ⇒ 对局可复现）。
        ///
        /// 🔴 **判据**：**凡是查得到图标的都显示**（= 我们的**替身**，见下面那条更正与文件头）。
        /// 也就是说「引擎当前认为这个单位身上有什么，卡上就画什么」—— 加/减益、光环给的、
        /// 限时增益到期收回，全都会跟着变，因为喂进来的是 <see cref="UnitState"/> 的**当前**表。
        /// 认不出图标的**跳过并计数**，由自检盯着，不静默吞掉。
        /// <para>🔴 **2026-10-11（`A1362`）**：**原版那条判据已经读到了**（文件头有全文）——
        /// 它要 `HasIcon && IsInPlayIcon && TraitIcon != null`（外加 `CardTraitData._hide == false`），
        /// 而**那两条开关的逐 trait 取值本地取不到**（文件头写了实据）⇒ 本方法**今天做不到逐条对齐**。
        /// ⛔ **别把「取不到开关」当成「不用管」**：能取到的那一天，要按 `HasIcon && IsInPlayIcon`
        /// 收窄这里（**先做哪些**：先补这两个开关的读取口，再谈收窄）。</para>
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
                                      System.Func<string, bool> isActive = null)
        {
            var list = new List<Badge>();
            if (keywords == null) return list;

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
            var ordered = new List<KeyValuePair<string, int>>();
            foreach (var kv in keywords) ordered.Add(kv);
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
                // 🔴 **黑名单：认得出图也不上场的那些**（`talent`；判据 = 用户 2026-10-12 口径，
                //    全文与「为什么不是原版读数」见 `NotInBoardBadges`）。放在 `SpriteOf` 之后：
                //    这位仍然算「认得出图」⇒ **不会**被当成「认不出图标」计进那条诊断。
                if (NotInBoardBadges.Contains(Key(kv.Key))) continue;
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
