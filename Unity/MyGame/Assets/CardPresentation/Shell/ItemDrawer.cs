// ItemDrawer.cs — 「抽屉库」：原版 `ItemDrawer` 那套「**按物品类型选一个抽屉、把物品画进 holder**」的**共用入口**
//
// ============================ 为什么要单开一份 ============================
// 原版的奖励/商品格**不是**一格一格的贴图，而是一棵**运行时装配的抽屉**：
//   `ItemDrawer.Draw(holder, item, quantity, drawerOverride)`：
//     ① `GetDrawerConfig(item, override)` —— 从 **`ItemDrawerConfig` SO** 里按**物品类型**找一条
//        `ItemDrawerReference{ TypeReference@0x10, drawerReference@0x18, customDrawerOverrides@0x20, options@0x28 }`
//     ② `drawer == null` ⇒ **返回 null（不画）**
//     ③ `Instantiate(prefab, parent)`，把 `drawerOverride` 写进对象 `+0x20`
//     ④ 调抽屉自己的虚表 `+0x188` = `Draw(item, quantity, options)`
//   （`d:/2/tools/decomp_full/ItemDrawer__Draw.c` 逐步读出来的；选择那两步在
//     `ItemDrawerConfig__GetReference.c` + `…__GetReference_b__0/b__1.c` 里，见本文件 `PickDrawer`）
//
// 阶段二有**四个**消费点要同一件事：战役奖励窗 · 战役页节点上的首奖励 · 锻造厂奖励格（A12-P1）· 商店那 19 个变体（A8）。
// 按 `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」⇒ **判据只此一份**，调用点别再各写一套。
//
// ============================ 出处（唯一正本） ============================
//  · 普查：`资料/普查产出_1003/ItemDrawer_抽屉系统.md` §一（选抽屉 4 步）· §二（23 个抽屉）· §三（6 个 prefab）
//    · §四（**画法**：`WildcardDrawer` / `WildcardIconDrawer` / `GenericArmyItemDrawer`）· §六（还缺什么）
//  · 正本：`资料/阶段二_锻造厂与战役页_原版规格.md` `:233`（`RewardTransform` 出厂空、奖励由 `ItemDrawer.Draw` 灌）·
//    `:598`（`Draw` 之后 `SetAsFirstSibling`）· `:629`（`rewardTier 0/10` 决定左列/右列）· `:672-673`（节点首奖励没建）
//  · 反编译（`d:/2/tools/decomp_full/`）：`ItemDrawer__Draw.c` · `ItemDrawer__GetDrawerConfig.c` ·
//    `ItemDrawerConfig__GetReference.c` · `ItemDrawerConfig.ItemDrawerReference__GetDrawer.c` ·
//    `WildcardDrawer__Draw.c` · `WildcardIconDrawer__Draw.c` · `CampaignNode__Setup.c:109` ·
//    `CampaignRewardsWindow__Open.c:134`
//    🔴 **全库 22 处 `ItemDrawer.Draw` 调用（18 个文件），每一处的 `quantity`/`drawerOverride` 都读过**：
//       `CampaignNode.Setup`（战役节点） = **1 / 10(Icon)** · `CampaignRewardsWindow.Open`（战役奖励窗） = **该奖励的
//       quantity / 0(Default)** · `CatalogItemContainer.OnInitialize`（商店格） = 1 / **20(Shop)** ·
//       `TitleTab.Initialize` = 1 / **15(Horizontal)** · `EndBattleRewardHolder.SetReward`（结算格）·
//       `EventBadge.SetNotificationData`（通知红点）· `EventProgressBarController.ScoreObject`（进度格）·
//       `MissionRewardItem.Setup`（任务格） = 1 / **10(Icon)** —— `Icon(10)` **一共 5 处，全是「小徽记」场合**
//       （战役节点 + 上面那四格），这是 `PickDrawer` 里「`Icon` 档 = 缩略那一个变体」的判据之一。
//       其余：`DailyRewardDrawerController` / `DailyStreakItemContainer` / `GenericArmyItemDrawer` 三处的
//       覆盖档是**变量**（各自的上游字段，本地读不出取值）；剩下那些全是 `0(Default)`。
//  · 类型/字段：`d:/2/tools/il2cpp_out/dump.cs:93280-93841`（`ItemDrawer` · `ItemDrawer<T>` · `ItemDrawerComponents`
//    `{background/image/label/quantity/premiumHighlight/premiumBadge/claimedWarning}` · `ItemDrawerOptions{stackable, showName, typeString}`
//    · `CardArmy` 枚举在 `:45637-45650`）
//  · 物品 SO（**本地有**，`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`）：
//    `WildcardUltramarines1..4.json` = `{ cardRarity: 1/2/3/4, cardArmy: 10 }` ·
//    `Booster Pack Ultramarines.json` 的 `containerPreviewImage.m_SubObjectName = "40K_shop_offer_booster_UM"`
//
// ============ 🔴 原来写「本地【读不到】的两样」—— **2026-10-04 两条都作废：两样都读得到** ============
//  🔴 **订正痕迹**（铁律 5）：这一节原来写「**两样都读不到 ⇒ 别再查**」—— **两半现在都不成立**。
//     （触发：审查代理 R-X1 的 F11 查出①是假的、本批 FX-3 复核出②**同样假**；旧文本已删，只留结论与判据 —— 铁律 6。）
//  ① **「类型 → 抽屉 prefab」那张映射表（= `ItemDrawerConfig` SO）——**✅ **已解出**：
//     判据 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md`（脚本 `工具/read_itemdrawerconfig.py` 可复现、
//     7 条自检全绿）：**20 个类型 + 19 档 `customDrawerOverrides` + 36 个 GUID，零条靠猜**。
//     ⚠️ 当年那句「`grep -rl customDrawerOverrides assets_full` **0 命中** ⇒ 读不出来」**是假结论** ——
//     SO 本体在 `sharedassets0.assets`（**没有 type tree** 的原始文件），**grep 字段名对它无效**
//     （这类文件要按签名桩的字段序手工解 —— `工具/read_itemdrawerconfig.py` 就是这么解的），
//     **不是「本地没有」**。
//     ⇒ 但**本文件 `PickDrawer` 那张表仍然是我们推的**（它的键是 `ItemKind`，不是原版那套 .NET 类型）
//     —— 「**表已解出**」与「**我们还没接过去**」是两件事；接过去是**一件待做的活**（铁律 11：要做，判据齐）。
//     🆕 **2026-10-05（A68②）**：那张**原版表本身**（`ItemTypeSet` / `ItemTypeSets`，20 条 · `OfferPopups`(30)
//     那一档）**已从 `Shell/OfferContainer.cs` 搬进本类**（见下面「⓪」那一节）—— 「表住在哪」与
//     「`PickDrawer` 还没按它选」是**两件事**：搬的是**住址**，`PickDrawer` 那一步**照旧是我们推的**。
//     ✅ **2026-10-05（A79）：接过去了** —— `PickDrawer` 现在**先走真表那一跳**
//     （`ItemTypeOf` 求原版类型 → `GetDrawerClass` 查 `ItemTypeSets` → `DrawerImpls` 换成我们的实现），
//     **真表答不出才走兜底**，两条路都**出声**（同一个 key 只报一次，免得 47 个节点刷屏）。
//     `ResolveDrawerClass` / `ItemParents` / `ParentItem` 也**一并搬进本类**（理由见 `GetDrawerClass`）；
//     `OfferContainer.ResolveDrawerClass` / `TypeOfKind` 现在只剩**一行转调**（原签名、原行为不变）。
//     ✅ **2026-10-03（A79②）：四档也抄齐了** —— `ItemTypeSets` 现在有 `Main` + `Icon`(10) +
//     `Horizontal`(15) + `Shop`(20) + `OfferPopups`(30) 五列，20 行**逐格对着映射表 §②**填
//     （`Icon` 13 条 · `Horizontal` 1 条 · `Shop` 2 条 · `OfferPopups` 3 条；其余格 `null` = 这条没写）。
//     🔴 **同批把 `GetDrawerClass` 那条守卫换掉了**：原来是「本表没抄这一档 ⇒ 返回 `null` + 出声、⛔ 不拿主档顶」，
//     现在照**原版**——`GetDrawer.c:23-33` / `GetReference.c:62-72`「**按键找不到 ⇒ 回落主档**」。
//     ⚠️ **旧守卫其实是个偏离**：原版本来就回落主档，「返回 `null`」= 我们**比原版少画**；
//     而真正要防的「拿主档顶替一个该用变体的档」，现在由**表本身**排除（写了变体的档一定取到变体）。
//     ⚠️ 那条旧守卫的**唯一读者** = `Editor/ShopScene.cs` 一条断言（`…("CosmeticItemTitle", Icon) == null`），
//     **同批翻成正値断言**（`== "TitleIconDrawer"`）；⛔ 只改表不改它 ⇒ 那条会红。
//  ② **抽屉 prefab 本身 ——**✅ **也读得到**：`python 工具/menu_dump.py bundle_menus_assets_all "Deck Drawer"`
//     （`"Wildcard Drawer"` 同）能把**整棵子树**摊开 —— 矩形 / `act` / 组件类名 / sprite 名一列一列都在
//     （`Deck Drawer` 那一份连 `Collection Deck` / `Converted Drawer` / `Premium Highlight/Highlight` /
//     `Blackout` / `Badge` 都建出来了）⇒ 当年那句「抽屉**内部每一层的矩形/字号**是我们挑的**因为读不到**」
//     的**前提**倒了（「**我们还在挑**」这个**事实**没倒）⇒ **照 dump 出来的抽屉几何改版式**是**待做的活**。
//     📌 **一条真的几何旁证**（不是猜 · 保留）：`Daily Reward Popup Item Drawer` 里那个抽屉实例的 `Content/Image`
//     **与抽屉根同矩形**（286.60×293.27，`scl 1.1`、`preserveAspect`）⇒「主图层铺满抽屉框」这一条有依据，
//     **其余（图标占框的比例、文字的条位）没有** —— 那几项要按 ② 的 dump 一件一件定。
using UnityEngine;
using System;

namespace CardPresentation
{
    /// <summary>原版 `DrawerOverride`（`Assembly-CSharp/DrawerOverride.cs`，普查 §一 实读）—— **值照抄**。
    /// 语义（`ItemDrawerConfig.ItemDrawerReference.GetDrawer`）：`Default` ⇒ 用该类型的**主抽屉**；
    /// 非 `Default` ⇒ 在 `customDrawerOverrides` 里按这个键找**该档变体**，**找不到回落主抽屉**。</summary>
    public enum DrawerOverride
    {
        Default = 0,
        /// <summary>小徽记档：战役节点 / 结算格 / 通知红点 / 进度格 / 任务格（22 处调用点里**这 5 处**用这个值）。</summary>
        Icon = 10,
        /// <summary>横条档（`TitleTab.Initialize` 用这个值）。</summary>
        Horizontal = 15,
        /// <summary>商店格档（`CatalogItemContainer.OnInitialize` 用这个值）。</summary>
        Shop = 20,
        /// <summary>礼包弹窗档。🔴 **2026-10-04 订正（R-X1 的 F11）**：原来写「**22 处调用点里一处都没用到它**
        /// （表里有这个值而已）」—— **前半句对、后半句假**：
        /// · **对的那半句**只在它自己的限定域里成立 = **`ItemDrawer.Draw` 那 22 处调用点**里没人传 30；
        /// · **假的那半句**：原版**在 `GetDrawerConfig` 那条路上正在用它** ——
        ///   `GeneralOfferPopupDrawer__DrawRewards.c:319` 把 **`0x1e`(=30)** 传给了 `GetDrawerConfig`
        ///   （那一处**不是** `Draw` 调用点 ⇒ 当年那张「22 处」的普查**扫不到它**），
        ///   而且 `ItemDrawerConfig` 表里有 **3 条**专门写了 30 档（映射表 §⑤.2）⇒ 这一档**在用**。</summary>
        OfferPopups = 30,
    }

    /// <summary>物品种类。**原版是按 .NET 类型分的**（配置表第一轮 `Type == item.GetType()` 精确相等、
    /// 第二轮 `ReflectionHelper.Is` 走 is-a）；我们没有那套服务端类型体系 ⇒ 用这个枚举当判据，
    /// **由 `ItemDrawer.Spec(...)` 从 id 推出来** —— **这是我们挑的**（依据逐条写在 `Spec` 上）。</summary>
    public enum ItemKind
    {
        /// <summary>**明确没有抽屉**（空 id / 空物品）—— 照 `ItemDrawer__Draw.c` 第 ②步：**不画**、返回空结果。</summary>
        None = 0,
        /// <summary>野牌 `Wildcard` —— 唯一一个**画法有判据**的种类（§四 那两个 `Draw` 逐字段照抄）。</summary>
        Wildcard = 1,
        /// <summary>有图、但**不知道原版是哪个抽屉**（今天的卡包 / 商店格都落这里）。</summary>
        Generic = 2,
        /// <summary>**判据空**：id 认不出、也没有图 ⇒ 走**占位板**抽屉（**不是**「不画」；调用方要**逐条出声**）。</summary>
        Unknown = 3,
    }

    /// <summary>一个物品。**原版 `ObtainableItem` 的替身**（那个类只在服务端模型里）。
    /// 每个字段后面标了它**有没有判据**。</summary>
    public partial struct ItemSpec
    {
        /// <summary>服务端 id（= SO 的 `m_Name` / `RewardInfo.targetId`）。</summary>
        public string Id;
        public ItemKind Kind;
        /// <summary>🆕 **2026-10-05（A79）**：这一项在**原版里是什么 `ObtainableItem` 子类型**
        /// （= 原版 `item.GetType()` 的替身 = **`ItemTypeSets` 那张真表的键**；逐字，含命名空间）。
        /// `null` = **判据空**（本地没有它的 SO ⇒ 推不出类型）。
        /// <para>🔴 **由 <see cref="Spec"/> 从 id 填**（实据逐条在 <see cref="IdTypes"/> 与 `Spec` 的注释里）；
        /// **它只管「查真表时用哪个键」** —— 抽屉里面画什么仍由 <see cref="Kind"/> 决定。
        /// ⛔ **不许按前缀推广**（`DT Ultramarines R3` 长得像 `DT Ultramarines All`，但本地没有它的 SO ⇒ 只能留 `null`）。</para></summary>
        public string Type;
        /// <summary>野牌专用。**判据** = 该物品 SO 的 `cardRarity`（1..4）。</summary>
        public int Rarity;
        /// <summary>野牌专用。**判据** = 该物品 SO 的 `cardArmy`（`CardArmy` 枚举值，10 = Ultramarines）。</summary>
        public int CardArmy;
        /// <summary>数据层那张「id → 图」表给的图名（战役这边 = `CampaignData.ItemIcon`）。`null` = 判据空。</summary>
        public string Art;
        /// <summary>没有图时占位板上写的短名（`CampaignData.ItemShortName`）。</summary>
        public string Label;
    }

    /// <summary>抽屉的版式参数 —— **全是调用方给的**。
    /// 🔴 **2026-10-04 订正（铁律 5）**：本行原来写「（原版这些值在抽屉 prefab 里，**本地没有**）」——
    /// 后半句**是假的**（抽屉 prefab **dump 得出来**，见文件头 ②）；**但本库的版式还没照它改**
    /// ⇒ 这些量现在**仍然是调用方给的**（「照 dump 出来的抽屉几何改版式」= **待做的活**，不是「本地没有」）。
    /// 用 <see cref="Default"/> 拿战役奖励窗那一套（值 = 它原来写死在 `BuildItem` 里的量，**一字未改**）。</summary>
    public struct ItemDrawerStyle
    {
        /// <summary>抽屉根节点名（原版是 prefab 名；`CampaignRewardWindow` 的自检按 `Item_` 前缀数格子）。</summary>
        public string NodeName;
        /// <summary>占位板底色的队列。</summary>
        public int QBoard;
        /// <summary>图层的队列。</summary>
        public int QArt;
        /// <summary>文字的队列。</summary>
        public int QText;
        /// <summary>主图边长 = `min(box.W, box.H) × IconFill`（**我们挑的**）。</summary>
        public float IconFill;
        /// <summary>阵营徽记边长 = `min(box.W, box.H) × ArmyFill`（**我们挑的**，野牌全抽屉那一层用）。</summary>
        public float ArmyFill;
        /// <summary>数量的字号（画布像素）；**0 = 不画数量**（原版 `Icon` 档那个抽屉就只画一张图）。
        /// ⚠️ 原版「这一格该不该显数量/显名字」是由**每条 `ItemDrawerReference.options`** 的
        /// `ItemDrawerOptions.stackable` / `showName` 决定的。🔴 **2026-10-04 订正（铁律 5）**：本行原来补了
        /// 「**而那张配置表本地没有**」—— **假的**：映射表 §② 把**每条记录的 `options` 三字段
        /// （`stackable` / `showName` / `typeString`）逐类型解出来了**（判据齐）。**我们还没接**（待做的活）
        /// ⇒ 现在**仍**用调用方给的这两个字号当替身（**我们挑的**：谁调用谁定，逐处有注释）。</summary>
        public float QuantityPx;
        /// <summary>名字的字号（画布像素）；**0 = 不画名字**（占位板短名 / 野牌阵营名）。同 `QuantityPx`：替 `options.showName`。</summary>
        public float NamePx;
        /// <summary>裁切边界 —— 🔴 **2026-10-13（A435 甲 · A198② 阶段 2）起它只是【显式覆盖】的那个口子**
        /// （同 `MenuDraw.Rect` 的 `clip` 形参）：**非空 ⇒ 它赢、连父链都不走**；留 `null`（= 缺省）
        /// ⇒ 那几条画路沿父链找最近的 `ViewportClip` 节点（原文「滚动区画内容前给一次」那套
        /// **派生写法已废**——`CampaignRewardWindow` / `RewardWindow` / `CampaignTab` 三处派生写入都删了）。</summary>
        public PxRect? Clip;
        /// <summary>🆕 **2026-10-11（A238）**：软边（= 原版那个视口 `RectMask2D.m_Softness`，画布像素；
        /// x 管左右、y 管上下），与 `Clip` **成对**给（⛔ 别只给一个）。`(0,0)` = 硬边 = 本库原来的行为。
        /// <para>🔴 **为什么要它**：原来只有 `Clip` ⇒ 战役奖励窗里**列底 / 按钮 / 徽标是 `(200,0)` 渐隐**，
        /// 而同一个视口下的**物品抽屉四层是硬边截** —— 同一条带里两种观感
        /// （判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §四·1；A238 那一行）。
        /// 逐层透传的四个落点 = `MenuDraw.Rect` 的 `clipSoftness` 形参（本文件那四条画路）。</para>
        /// <para>⚠️ **`MenuDraw.Rect` 只在 `Clip` 非空且 softness 有一个分量 &gt; 0 时才加工软边** ——
        /// 不给 `Clip` 的调用方（商店 / 战役节点那几个）保持 `(0,0)` ⇒ **一个字节都不动**。</para>
        /// <para>🔴 **2026-10-11 就地订正（铁律 5 · A302）**：本行原来写「**文字那三层（阵营名 / 短名 / 数量）
        /// 仍不吃裁切** —— `MenuDraw.Text` 没有裁切形参，而本库没走 `MenuDraw.ClipText`（那是**另一件**欠账）」。
        /// **那一件已经做完了**：本库新增了 <see cref="ClippedText"/>（= `Visible` → `Text` → `align`
        /// → `ClipText`，三段与 `GameWindow.Text` 逐字相同），三层文字的调用点**全部改走它**
        /// ⇒ 文字与图现在吃**同一份** `Clip` / `ClipSoftness`。⛔ 别再照上面那句旧话写。</para></summary>
        public Vector2 ClipSoftness;

        // ---- 🆕 **2026-10-11（A311）**：原版 `RewardWindow.Open` 在 `ItemDrawer.Draw` **之后**对每个抽屉
        //      调的**另外三跳**（判据逐条见 `SetPremium` / `SetEphemeral` / `SetConverted`）。
        //      ⛔ 这三格**不是**「抽屉自己画什么」，而是**调用方按奖励状态决定叠哪一层**
        //      ⇒ 默认全 `false`：不置位 = 原版那个「压根没调那个方法」的状态（那三层在 prefab 里出厂就是关的）。

        /// <summary>第 1 跳：原版 `drawer.TogglePremiumHighlight(rewardTier == 10)`。</summary>
        public bool Premium;

        /// <summary>第 2 跳：原版 `drawer.SetEphemeralDisplay(reward.ephemeralState)`
        /// （调用方只在 `convertedInto == null &amp;&amp; IsEphemeral` 时才调）。</summary>
        public bool Ephemeral;
        /// <summary>第 2 跳画的那段字的**源量** = 原版 `EphemeralEndTime − EphemeralStartTime`（**毫秒**）。</summary>
        public long EphemeralMs;

        /// <summary>第 3 跳：原版 `drawer.SetConvertedItem(reward.convertedInto)`。</summary>
        public bool Converted;
        /// <summary>第 3 跳那串数字 = `convertedTo.Quantity.ToString()`。</summary>
        public int ConvertedQuantity;
        /// <summary>第 3 跳那个图标（原版 = `((ICurrency)convertedTo.Item).GetIcon(IconSize.Small)`）
        /// —— 我们走数据层那张「id → 图」表的等价物。`null` = 取不到（那就只画数字，不静默）。</summary>
        public string ConvertedArt;

        /// <summary>三个装饰层（premium 的 Highlight/Blackout/Badge、converted/ephemeral 的条与字）的起始队列。
        /// 原版靠 **sibling 序**（`Premium Highlight` 是抽屉**最后**一个子节点 ⇒ 画在所有内容之上）；
        /// 我们这边 `CLAUDE.md` §三 定了「分层要用渲染队列」⇒ 按原版那三件的**出现次序**依次 +1。
        /// `null`/0 = 由 <see cref="Default"/> 给 `qText + 1`。</summary>
        public int QDecor;

        /// <summary>默认档。`IconFill 0.7` / `QuantityPx 34` / `NamePx 26` 就是战役奖励窗格子（200×300）
        /// 原来那三个量（图标 140 = 200×0.7），**为了不动既有的量渲染断言**。</summary>
        public static ItemDrawerStyle Default(int qBoard, int qArt, int qText)
        {
            return new ItemDrawerStyle
            {
                NodeName = "Item Drawer", QBoard = qBoard, QArt = qArt, QText = qText,
                IconFill = 0.7f, ArmyFill = 0.34f, QuantityPx = 34f, NamePx = 26f, Clip = null,
                ClipSoftness = Vector2.zero, QDecor = qText + 1,
            };
        }
    }

    /// <summary>`ItemDrawer.Draw` 的返回 —— 调用方靠它**出声**（判据空 / 取不到图 / 用了退档图）。</summary>
    public partial struct ItemDrawResult
    {
        /// <summary>建出来的抽屉根节点。`null` = **没画**（原版第 ②步：`drawer == null`）。</summary>
        public Transform Node;
        /// <summary>物品 id（回填，便于调用方记账）。</summary>
        public string Id;
        public ItemKind Kind;
        /// <summary>实际用的抽屉（`DrawerWildcard` / `DrawerWildcardIcon` / `DrawerIcon` / `DrawerPlaceholder`）。</summary>
        public string Drawer;
        /// <summary>实际用的图名（`null` = 没图）。</summary>
        public string Art;
        /// <summary>**走了占位板** = 这个物品的图**判据空**（或在本地取不到）⇒ 调用方**必须逐条出声**。</summary>
        public bool Placeholder;
        /// <summary>抽屉里要的**某一张图本地取不到**（主图取不到时**同时**置 `Placeholder`；
        /// 只有阵营徽记那一层取不到时 `Placeholder` 仍是 false —— 那种是「少画一层」）。</summary>
        public bool MissingArt;
        /// <summary>用了**退档图**（原版那张取不到、退成 `_small` 档 —— 见 `WildcardTex`）。</summary>
        public bool FallbackArt;
    }

    /// <summary>抽屉库。**只此一份**（A8 / A12 都读它）。</summary>
    public static partial class ItemDrawer
    {
        // ============================================================ 抽屉名（诊断 + 自检按它认）

        /// <summary>野牌全抽屉（原版 `WildcardDrawer : ItemDrawer&lt;Wildcard&gt;`）。</summary>
        public const string DrawerWildcard = "WildcardDrawer";
        /// <summary>野牌图标档（原版 `WildcardIconDrawer : ItemDrawer&lt;Wildcard&gt;`，`Override = Icon` 那档）。</summary>
        public const string DrawerWildcardIcon = "WildcardIconDrawer";
        /// <summary>🔶 自己设计（非原版）：抽屉名 `"IconDrawer"` —— 原版**没有这个类**（`dump.cs` 全库 0 个 `class IconDrawer`；只有 `XxxIconDrawer` 那几族）· 必须自己定：`DrawerOverride.Icon` 那档的真身类名只能反查 `ItemDrawerConfig`（该资产本地没有）⇒ 只能起个名字占位 · 我们定的（`A79` 那批 2026-10-05；2026-10-26 G8 复核「原版无此类」）。</summary>
        public const string DrawerIcon = "IconDrawer";
        /// <summary>🔶 自己设计（非原版）：抽屉名 `"PlaceholderDrawer"` —— 原版没有「占位板抽屉」这个东西（判据空的物品在原版走「真抽屉 + prefab 自带图」）· 必须自己定：我们拿不到那条判据时**不许静默画成别的样子**（红线）⇒ 只能显式画一块会出声的占位板 · 我们定的（`A79` 那批 2026-10-05）。</summary>
        public const string DrawerPlaceholder = "PlaceholderDrawer";

        // 抽屉内部的节点名（`Icon`/`IconPlaceholder`/`ItemName`/`Quantity` 是战役奖励窗既有自检找的名字，
        // 原版这些层是 `ItemDrawerComponents` 的 `background/image/label/quantity` 字段）。
        // 🔴 **2026-10-04 订正（铁律 5）**：这里原来补了「**节点名在 prefab 里、本地读不到**」—— **假的**：
        // 那些抽屉 prefab 的**整棵子树 dump 得出来**（见文件头 ②，含每一层的节点名）。
        // ⚠️ **但这几个名字现在**仍是我们定的 —— 战役奖励窗的既有自检按它们数格子，改名 = **把断言改软**
        // ⇒ 「照原版改节点名 + 版式」是**一件待做的活**（与文件头 ② 同一件）。
        public const string NodeIcon = "Icon";
        public const string NodePlaceholder = "IconPlaceholder";
        public const string NodeItemName = "ItemName";
        public const string NodeQuantity = "Quantity";
        public const string NodeArmyIcon = "Army Icon";
        public const string NodeArmyName = "Army Name";

        // ---- 🆕 **2026-10-11（A311）**：奖励状态那三层（第 1/2/3 跳）的节点名 —— **照 prefab 逐字**
        //      （判据 = `工具/menu_dump.py bundle_menus_assets_all "Container Drawer"` 的整棵子树；
        //       `Wildcard Drawer` / `Random Card Drawer` / `Premium Drawer` / `Campaign Points Drawer` / `Deck Drawer`
        //       六份**逐个核过，名字与锚点完全一致**）。⛔ 别改名：自检按这些名字取件。
        /// <summary>第 1 跳的容器（原版是抽屉**根**的子节点、排在 `Content` 之后 ⇒ 画在所有内容之上）。</summary>
        public const string NodePremiumHighlight = "Premium Highlight";
        /// <summary>第 1 跳里的高亮框（sprite `OctagonUI Filled Fade SDF`，Sliced，`m_Color = (1,1,1,0.8)`）。</summary>
        public const string NodeHighlight = "Highlight";
        /// <summary>第 1 跳里的压暗层（**没有 sprite**，`m_Color = (0,0,0,0.5529412031173706)`）。</summary>
        public const string NodeBlackout = "Blackout";
        /// <summary>第 1 跳里的角标（sprite `40k_campaign_Premium-icon`、`m_PreserveAspect = 1`）。</summary>
        public const string NodePremiumBadge = "Badge";
        /// <summary>第 3 跳的容器（原版是抽屉 `Content` 的子节点）。</summary>
        public const string NodeConvertedDrawer = "Converted Drawer";
        /// <summary>第 2 跳的容器（原版是抽屉 `Content` 的子节点，与 `Converted Drawer` **同锚点**）。</summary>
        public const string NodeEphemeralDrawer = "Ephemeral Drawer";
        /// <summary>两个容器里那一行「图标 + 文字」（原版 = 挂 `PriceDisplay` 组件的那个节点）。</summary>
        public const string NodePriceDisplay = "Price Display";
        /// <summary>`Price Display` 里那两件（原版节点名逐字 = `icon` / `text`）。</summary>
        public const string NodePriceIcon = "icon", NodePriceText = "text";
        /// <summary>第 3 跳那行下面的说明字（原版 TMP 的 `m_text` = `Already Owned`）。</summary>
        public const string NodeAlreadyOwned = "AlreadyOwned";
        /// <summary>抽屉里的条所处的**参照高度**（画布像素）—— 那些装饰的绝对量（字号 65/72/75、图标 93.55、
        /// 高亮外扩 50、说明字下移 108）都是相对抽屉 `Content` 这个 **712×1080** 的框定的
        /// （六个抽屉 prefab 逐个实读，都是这一个数）⇒ 按 `box.H / 1080` 折算。
        /// ⚠️ **这是我们的映射**：我们的 `box` 是调用方给的格子（战役奖励窗 200×300），
        /// 而原版那个 `Content` 是**固定尺寸**的子节点 —— 照 prefab 改内版式是**另一件待做的活**
        /// （文件头 ② 那条），别把这里的折算当成原版的版面。</summary>
        public const float DecorRefH = 1080f;

        /// <summary>占位板底色（**我们挑的**：中性深灰 —— 不拿别的图冒充，也不与真图混淆）。</summary>
        public static readonly Color BoardColor = new Color(0.16f, 0.16f, 0.18f, 1f);

        /// <summary>🆕 **2026-10-17（界面线 `T07` · `A1753` #8/#9）**：抽屉里两段字的**屏上颜色**。
        /// 口径 = **顶点色 × 材质 `_FaceColor`**（判据 = `资料/普查产出_1017/UI/R_T07_剩余四项.md` §4·0/§4·2）：
        /// <list type="bullet">
        /// <item><b>`AlreadyOwned`</b>：材质 `Pragati-Regular Off White Warm` (0.972, 0.937, 0.866)
        ///   × 顶点 **(1, 1, 0.737)** = **(0.972, 0.937, 0.638)** —— ⚠️ 蓝通道被顶点色压过，别抄成材质本色。</item>
        /// <item><b>`Price Display/text`</b>：材质 `Pragati-Regular Light Cream` (0.992, 0.868, 0.733)、顶点白。</item>
        /// </list></summary>
        public static readonly Color AlreadyOwnedInk = new Color(0.972f, 0.937f, 0.638f, 1f);
        /// <inheritdoc cref="AlreadyOwnedInk"/>
        public static readonly Color PriceInk = new Color(0.992f, 0.868f, 0.733f, 1f);

        // ============================================================ ⓪ 原版 `ItemDrawerConfig` 那张表（🆕 2026-10-05 A68② 搬来）

        /// <summary>`ItemDrawerConfig` 里的一条：**物品类型 → 抽屉类**。
        /// 出处 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md`（**整张表从 SO 原始字节解出**：
        /// 20 个类型 + 19 档 override，36 个 GUID，**零条靠猜**；脚本 `工具/read_itemdrawerconfig.py`）。
        /// <para>🔴 **`OfferPopups`(30) 这一档全表只有 3 条**（映射表 §⑤.2）⇒ 其余 17 个类型在这一档
        /// **回落主档**（`…GetDrawer.c:24-33`：按键找不到 `useDefault` 那条路）。</para>
        /// <para>🆕 **2026-10-05（A68②）：这个类型 + 下面那张表原来长在 `Shell/OfferContainer.cs` 里**
        /// （A43 那批临时放在那儿），现在搬进本类 —— 它复刻的**就是原版 `ItemDrawerConfig`**（`ItemDrawer`
        /// 族自己的配置），正统归处是这里；`OfferContainer` 那边只剩**读**它的一方
        /// （`OfferContainer.ResolveDrawerClass`，A43 的填槽路）。
        /// ⚠️ **搬迁不是改写：20 行的值与注释一字未改**（只有这行「住哪」的说明是本轮加的）。
        /// 🆕 **2026-10-05（A79）**：**读它的那一方也搬过来了** = <see cref="GetDrawerClass"/>
        /// （原名 `OfferContainer.ResolveDrawerClass`，那边只剩一行转调）。
        /// 🔴 **订正痕迹（铁律 5 · A90 · 2026-10-05）**：本行原来接着写「**行为一字未改**」—— **不成立**
        /// （判据同 A79②，与 `Shell/OfferContainer.cs` 里那条「三件已搬进 `ItemDrawer`」订正注 那道门面上的同名订正配对）：本体早在
        /// A79②（2026-10-03）就已照**原版**把「本表没抄这一档 ⇒ 返回 `null` + 出声」换成
        /// 「**按键找不到 ⇒ 回落主档**」（`GetDrawer.c:23-33` / `GetReference.c:62-72`）
        /// ⇒ 没写变体的那几档**返回值变了**；搬家保住的只有**签名与转调关系**。
        /// **错因**：这句是照**搬迁**写的（搬的只有住址），却把「住址没变」写成了「行为没变」；
        /// 而同一个文件 `:545-547`（`&lt;/remarks>` 里那条「`noov` 随守卫一起删掉」）/ `本文件`（下面 `pick`
        /// 那段回落主档的订正痕迹）本来就写着「那条守卫已删」⇒ 前后自相矛盾。
        /// 逐条订正与断言那半边见 <see cref="GetDrawerClass"/> 的同名订正。</para></summary>
        public struct ItemTypeSet
        {
            /// <summary>原版 `TypeReference` 的名字（**逐字**，含命名空间；= 查表的键）。</summary>
            public string Type;
            /// <summary>`drawerReference`（主档 · `override == 0`）指向的 prefab 上的**抽屉类**。</summary>
            public string Main;
            /// <summary>`customDrawerOverrides` 里**键 = `Icon`(10)** 那条的抽屉类；`null` = 这条**没写 10** ⇒ 回落主档。</summary>
            public string Icon;
            /// <summary>…**键 = `Horizontal`(15)**；`null` = 这条没写 ⇒ 回落主档。</summary>
            public string Horizontal;
            /// <summary>…**键 = `Shop`(20)**；`null` = 这条没写 ⇒ 回落主档。</summary>
            public string Shop;
            /// <summary>…**键 = `OfferPopups`(30)**；`null` = 这条没写 ⇒ 回落主档。</summary>
            public string Popup;
            public ItemTypeSet(string t, string m, string icon, string horizontal, string shop, string popup)
            { Type = t; Main = m; Icon = icon; Horizontal = horizontal; Shop = shop; Popup = popup; }

            /// <summary>`customDrawerOverrides` **按键**取那一档的抽屉类；`null` = **这条记录没写这一档**。
            /// <para>🔴 **「按键取值、找不到回落主档」这一步照原版两处**：
            /// `ItemDrawerConfig.ItemDrawerReference.GetDrawer.c:23-33`（`override != 0` ⇒ 在 `customDrawerOverrides`
            /// 里 `Find(键 == override)`，**找不到就保持 `drawerReference`（主档）**）与
            /// `ItemDrawerConfig__GetReference.c:62-72`（同形：`param_4 == 0` 直接取主档，否则 `Find` 替换）。
            /// ⛔ 所以 `null` **不是**「判据空」、更**不是**「不许画」—— 它是「**这条记录没写这一档**」，
            /// 调用方**必须回落主档**（<see cref="GetDrawerClass"/> 就是这么用的）。</para></summary>
            public string Variant(DrawerOverride ov)
            {
                switch (ov)
                {
                    case DrawerOverride.Icon:        return Icon;
                    case DrawerOverride.Horizontal:  return Horizontal;
                    case DrawerOverride.Shop:        return Shop;
                    case DrawerOverride.OfferPopups: return Popup;
                    default: return null;      // `Default`(0) 走主档（= 原版那个 `if (override != 0)` 的分支）
                }
            }
        }

        /// <summary>**`ItemDrawerConfig` 的 20 条**，四档 override 各一列
        /// （`Main` = 主档 · `Icon`(10) · `Horizontal`(15) · `Shop`(20) · `Popup` = `OfferPopups`(30)），
        /// 每格 = **那条记录指向的 prefab 上的抽屉类**；`null` = **这条记录没写这一档** ⇒ **回落主档**。
        /// 🔴 **2026-10-03（A79②）：四档抄齐了** —— 判据 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §②
        /// 那 39 行（20 类型 + 19 档 override；每格都有 prefab 名 + 抽屉类 + GUID，零条靠猜）。
        /// **逐档条数**：`Icon`(10) **13 条** · `Horizontal`(15) **1 条** · `Shop`(20) **2 条** ·
        /// `OfferPopups`(30) **3 条**；其余类型在这几档**没写 ⇒ 回落主档**（每一格 `null` 都对着映射表核过）。
        /// <para>🔴 **订正痕迹（铁律 5）**：本行原来写「别的档**没有判据**」，2026-10-04 订正成「本表没抄」
        /// —— **现在已抄齐**；`GetDrawerClass` 里那条「没抄的档 ⇒ 返回 `null` + 出声」的守卫
        /// **同批换成照原版的「按键找不到 ⇒ 回落主档」**（`GetDrawer.c:23-33` / `GetReference.c:62-72`）。</para>
        /// <para>⚠️ **表里的类 ≠ 我们实现的抽屉**：`DrawerImpls` **现读覆盖 24 档**（2026-10-30 `B101_E3` 补齐后：
        /// 野牌两档 + `Deck`/`DeckAndCardback`/`CampaignPoints`/`Currency` + 17 个新类）；**仍缺** `CardDrawer` /
        /// `CardAlternateArtDrawer` / `GenericArmyItemDrawer` ⇒ 那三个类 ⇒ `PickDrawer` **出声 + 兜底**（⛔ 不静默画成别的样子）。</para>
        /// <para>⚠️ **一个值 ≠ 全部情况（铁律 5·c）**：同一条记录的**不同 override 档可以是不同的类** ——
        /// 表里现成就有 4 处：`ForgePoints` 主档 `ForgePointDrawer` / `Icon` 档 `ForgePointIconDrawer` ·
        /// `VIPPremiumItem` 主档 `PremiumDrawer` / `Icon` 档 `PremiumIconDrawer` ·
        /// `CosmeticItemTitle` 主档 `TitleDrawer` / `Icon` 档 `TitleIconDrawer` / `Horizontal` 档 `TitleDrawerHorizontal`
        /// ⇒ **拿主档顶替任何一档都会被这些行抓出来**（`Editor/ShopScene.cs` 里有断言盯着）。</para></summary>
        public static readonly ItemTypeSet[] ItemTypeSets =
        {
            //         类型名（原版 `TypeReference`，逐字）                 主档                         Icon(10)                        Horizontal(15)          Shop(20)                 OfferPopups(30)
            new ItemTypeSet("Currency", "CurrencyDrawer", "CurrencyDrawer", null, null, null),
            new ItemTypeSet("PlayerAvatar", "AvatarDrawer", "AvatarDrawer", null, "AvatarDrawer", null),
            new ItemTypeSet("Everguild.LiveOps.ShopContainer", "ContainerDrawer", "ContainerDrawer", null, null, null),
            new ItemTypeSet("RawCardScript", "CardDrawer", null, null, null, null),
            new ItemTypeSet("DropTableItem", "RandomCardDrawer", "RandomCardDrawer", null, null, null),
            new ItemTypeSet("CosmeticItemCardback", "CardbackDrawer", null, null, null, null),
            new ItemTypeSet("Wildcard", "WildcardDrawer", "WildcardIconDrawer", null, null, null),
            new ItemTypeSet("CampaignPoints", "CampaignPointDrawer", "CampaignPointDrawer", null, null, null),
            new ItemTypeSet("ForgePoints", "ForgePointDrawer", "ForgePointIconDrawer", null, null, null),
            new ItemTypeSet("Everguild.LiveOps.ExpansionPassPoints", "ExpansionPassPointDrawer", "ExpansionPassPointDrawer", null, null, null),
            new ItemTypeSet("PrebuiltDeck", "DeckDrawer", null, null, null, null),
            // 下面这三条就是**全表写着 30 的那三条**（`Popup` 列）
            new ItemTypeSet("CosmeticItemTitle", "TitleDrawer", "TitleIconDrawer", "TitleDrawerHorizontal", null, "TitleDrawerHorizontal"),
            new ItemTypeSet("ExpansionPremiumItem", "ExpansionPassPremiumDrawer", "ExpansionPassPremiumDrawer", null, null, "ExpansionPassPremiumDrawer"),
            new ItemTypeSet("CosmeticItemAvatarBorder", "AvatarBorderDrawer", null, null, "AvatarBorderDrawer", "AvatarBorderDrawer"),
            new ItemTypeSet("PremiumItem", "PremiumDrawer", "PremiumDrawer", null, null, null),
            new ItemTypeSet("VIPPremiumItem", "PremiumDrawer", "PremiumIconDrawer", null, null, null),
            new ItemTypeSet("AllianceTrophyData", "AllianceBadgeDrawer", "AllianceBadgeDrawer", null, null, null),
            new ItemTypeSet("XSollaBundleItem", "XSollaOfferDrawer", null, null, null, null),
            new ItemTypeSet("AlternateArtCard", "CardAlternateArtDrawer", null, null, null, null),
            new ItemTypeSet("GenericArmyItem", "GenericArmyItemDrawer", null, null, null, null),
        };

        // ============================================================ ① 选抽屉

        /// <summary>原版类型名表里那一条野牌 —— 它的 SO 类就是 `Wildcard`（4 条 SO 实读，见 <see cref="Spec"/>）。</summary>
        public const string TypeWildcard = "Wildcard";

        /// <summary>`ItemKind` → **原版类型名**（只有一档**有判据**）：
        /// `Wildcard` ⇒ `"Wildcard"`（`ItemDrawer.Spec` 认出的野牌，其 SO 就是 `Wildcard` 这个类 ——
        /// 4 条 `Wildcard&lt;阵营&gt;&lt;档&gt;` SO 实读 + `ItemTypeSets` 表里有 `Wildcard` 这一条）。
        /// 其余 ⇒ `null`（**判据空**，不许猜：`Generic`/`Unknown` 是**我们**的枚举，不是原版的类型）。
        /// <para>🆕 **2026-10-05（A79）**：本函数原来长在 `Shell/OfferContainer.cs`（叫 `OfferContainer.TypeOfKind`），
        /// 现在搬进本类 —— 它是 `ItemKind`（住在本文件）的伴生映射；那边只剩一行转调。</para></summary>
        public static string TypeOfKind(ItemKind kind)
        {
            return kind == ItemKind.Wildcard ? TypeWildcard : null;
        }

        /// <summary>这一项**在原版里是什么类型** = 查 `ItemTypeSets` 真表用的键。
        /// 取 <see cref="ItemSpec.Type"/>（<see cref="Spec"/> 从 id 填的实据）；
        /// 它空时退回 <see cref="TypeOfKind"/>（**手搓的 `ItemSpec`** —— 不走 `Spec`、只给了 `Kind` 的那些
        /// —— 仍认得出野牌）。两条都没有 ⇒ `null` = **判据空**（调用方出声 + 兜底，不猜）。</summary>
        public static string ItemTypeOf(ItemSpec item)
        {
            if (!string.IsNullOrEmpty(item.Type)) return item.Type;
            return TypeOfKind(item.Kind);
        }

        /// <summary>**照 `ItemDrawerConfig.ItemDrawerReference.GetDrawer` + `ItemDrawerConfig.GetReference`**：
        /// 第一轮按**精确类型**找、第二轮按 **is-a** 兜底（那两条谓词 = `…_GetReference_b__0.c` 的 `Equals`
        /// 与 `b__1.c` 的 `ReflectionHelper.Is`）；找到之后再按 `drawerOverride` 取该档变体，**找不到回落主抽屉**。
        /// <para>🔴 **本表仍然是我们推的** —— 但**理由变了**（2026-10-04 订正，铁律 5）：本行原来写
        /// 「（原版那张 `ItemDrawerConfig` SO **本地没有**，见文件头）」—— **那是假的**：那张 SO **当天已解出**
        /// （映射表 doc：20 条 + 19 档 override 全在）。真正的原因是**本表的键是我们这套 `ItemKind`**、
        /// 不是原版那套 .NET 类型 ⇒ **接过去是一件待做的活**（判据齐，铁律 11）。
        /// 现在的推断依据照旧：① `*IconDrawer` 这一族确实存在（`WildcardIconDrawer` / `ForgePointIconDrawer` /
        /// `RandomCardIconDrawer` / `TitleIconDrawer` —— 普查 §二）；② 22 处调用点里用 `Icon(10)` 的 **5 处**
        /// **全是「小徽记」场合**（战役节点 / 结算格 / 通知红点 / 进度格 / 任务格）。
        /// 其余档（`Shop` / `Horizontal` / `OfferPopups`）我们**没有**对应变体 ⇒ 照原版「找不到就回落主抽屉」。</para>
        /// <para>✅ **2026-10-05（A79）：这段「我们推的」现在是【兜底】，不再是第一条路** ——
        /// 见下面的 <see cref="PickDrawer(ItemSpec, DrawerOverride)"/>：**先查真表**（`GetDrawerClass` + `DrawerImpls`），
        /// 查不到才落到这里，并且**出声**。</para>
        /// <returns>`null` = 原版第 ②步的「不画」。</returns></summary>
        public static string PickDrawer(ItemSpec item, DrawerOverride drawerOverride)
        {
            string via;
            return PickDrawer(item, drawerOverride, out via);
        }

        /// <summary><see cref="PickDrawer(ItemSpec, DrawerOverride)"/> 的带路由说明版（`via` = 这一票**为什么**这么选：
        /// 走的真表哪一档 / 还是兜底）。两条路**都出声**（`Note`，同一 key 只报一次）。
        /// <para>**顺序（2026-10-05 A79）**：① `None` ⇒ 不画（原版第 ②步）；② **真表那一跳** ——
        /// `ItemTypeOf` 求原版类型 → `GetDrawerClass` 查 `ItemTypeSets` → `DrawerImpls` 换成我们的实现；
        /// ③ 上面任何一步答不出 ⇒ **兜底**（<see cref="OverrideDrawer"/> + `ItemKind` 那条 switch），**并出声**。</para></summary>
        public static string PickDrawer(ItemSpec item, DrawerOverride drawerOverride, out string via)
        {
            if (item.Kind == ItemKind.None)                       // ② 原版：drawer == null ⇒ 不画
            {
                via = "`ItemKind.None`（空 id / 空物品）⇒ 照原版第 ②步**不画**（`ItemDrawer__Draw.c`）";
                return null;
            }

            // ---- ② 真表那一跳（原版 `ItemDrawer.GetDrawerConfig(item.GetType(), ov)`，2026-10-05 A79）----
            string type = ItemTypeOf(item);
            if (type != null)
            {
                string how;
                string cls = GetDrawerClass(type, drawerOverride, out how);
                if (cls != null)
                {
                    string impl = ImplOfDrawerClass(cls);
                    if (impl != null)
                    {
                        via = "走**真表**：" + how + " ⇒ 我们的实现 **`" + impl + "`**";
                        return impl;
                    }
                    Note("impl|" + cls,
                         "原版类型 `" + type + "` ⇒ 抽屉类 **`" + cls + "`** —— 但这个抽屉**我们没实现**"
                         + "（`DrawerImpls` 里**没有这个类** —— 现读覆盖 **24 档**；缺的只有 `CardDrawer` / `CardAlternateArtDrawer` / `GenericArmyItemDrawer`）⇒ 兜底，**不静默画成别的样子**");
                }
                else
                {
                    // ⚠️ **不把 `how` 再打一遍** —— `GetDrawerClass` 自己已经为这件事出过声（同一个 key 只报一次）；
                    //    这里只补**它的后果**：这一票退回兜底了（不然「真表没答出」这件事看不出结果）。
                    Note("fb|" + type + "|" + (int)drawerOverride,
                         "物品类型 `" + type + "` 的真表这一跳**没答出**（原因见 `GetDrawerClass` 那条 `[ItemDrawer]`）"
                         + "⇒ 这一票退回**兜底**那一档（**我们挑的**），⛔ 不是静默顶替");
                }
            }
            else
            {
                Note("type|" + (item.Id ?? "<空>"),
                     "物品 `" + (item.Id ?? "<空>") + "` 的**原版类型判据空**（本地没有它的 SO ⇒ 推不出"
                     + "`ItemTypeSets` 的键）⇒ 走**兜底**那一档（**我们挑的**，见 `PickDrawer` 的注释）");
            }

            // ---- ③ 兜底（**我们挑的**）：老口径原样保留，只是从「第一条路」降级成「真表答不出时的路」----
            string main;
            switch (item.Kind)
            {
                case ItemKind.Wildcard: main = DrawerWildcard; break;
                case ItemKind.Generic: main = DrawerIcon; break;
                default: main = DrawerPlaceholder; break;         // 判据空 ⇒ 占位板（**我们挑的**）
            }
            string alt = OverrideDrawer(item.Kind, drawerOverride);
            string d = alt ?? main;                               // 变体找不到 ⇒ 回落主抽屉
            via = (type != null ? "走**兜底**（真表这一跳没答出，上面已出声）：" : "走**兜底**（类型判据空，上面已出声）：")
                  + "`ItemKind." + item.Kind + "` + `" + drawerOverride + "` ⇒ **`" + d + "`**";
            return d;
        }

        /// <summary>`customDrawerOverrides[override]` 的替身（**我们挑的**，依据见 `PickDrawer`）——
        /// ✅ 2026-10-05（A79）起它只服务**兜底**那一档。⛔ **`Icon` 之外一律 `null`**。
        /// <para>✅ **2026-10-03（A79②）**：当初写「加档要照映射表 §② 逐条核，那是**另一件活**」—— **那件活做完了**：
        /// 四档（`Icon`/`Horizontal`/`Shop`/`OfferPopups`）现在都在 <see cref="ItemTypeSets"/> 上，
        /// **真表那一跳**先把它接走了。⇒ 就**今天**的调用方而言这一格是**兜底的兜底**：
        /// 野牌的 `ItemTypeOf` 给出 `Wildcard`、真表也一定有它（`WildcardDrawer` / `Icon` 档 `WildcardIconDrawer`）
        /// ⇒ **走不到这里**；它只在「类型链那一跳整体答不出」时兜住（那时 `Kind == Wildcard` 这一支才有意义）。
        /// ⚠️ **别往这里补档来「修」别的物品** —— 那些物品缺的是**原版类型**（`ItemSpec.Type`），不是它。</para></summary>
        static string OverrideDrawer(ItemKind kind, DrawerOverride ov)
        {
            if (ov == DrawerOverride.Icon && kind == ItemKind.Wildcard) return DrawerWildcardIcon;
            return null;
        }

        // ============================================================ ①·二 真表那一跳（原版 `GetDrawerConfig`）

        /// <summary>**物品类型的基类链**（`GetReference` 第二轮 `Is(itemType, entryType)` 要沿它上溯）。
        /// 出处 = 签名桩（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/&lt;类&gt;.cs` 的 `class X : Y`）。
        /// <para>⚠️ 表里**只出现真正存在的父子对**；这些中间基类**自己不是** config 条目
        /// （`CosmeticItem` / `PlayerItem` / `Points&lt;T&gt;` / `ShopContainerBase` / `ObtainableItem`）。</para>
        /// <para>🔴 **最后三条 = 「表里没有、但它的祖宗在表里」那种类型的实据**（各自的下层已核过**没有更多子类**）：
        /// `PrebuiltSortedDeck : PrebuiltDeck`（`PrebuiltSortedDeck.cs:1`）· `Energy : Currency`（`Energy.cs:1`）·
        /// `DlcBundle : ShopContainer`（`DlcBundle.cs:4`）。**去掉任何一条，对应的那一行就会变成「判据空」**。</para>
        /// <para>⛔ 这张表**不完整就出声**（查不到 ⇒ <see cref="GetDrawerClass"/> 返回 `null` + 出声），
        /// **不猜**一个最近的条目顶上（红线：不许静默失败）。</para>
        /// <para>🆕 **2026-10-05（A79）：原在 `Shell/OfferContainer.cs`** —— 连同 <see cref="ParentItem"/> 与
        /// <see cref="GetDrawerClass"/> 一起搬进本类，**值一字未改**。搬的理由见 `GetDrawerClass`。</para></summary>
        static readonly string[] ItemParents =
        {
            "CosmeticItemTitle:CosmeticItem", "CosmeticItemAvatarBorder:CosmeticItem",
            "CosmeticItemCardback:CosmeticItem", "PlayerAvatar:CosmeticItem",
            "CosmeticItem:PlayerItem", "RawCardScript:PlayerItem", "AlternateArtCard:PlayerItem",
            "PrebuiltDeck:PlayerItem", "XSollaBundleItem:PlayerItem", "PlayerItem:ObtainableItem",
            "VIPPremiumItem:ExpansionPremiumItem", "ExpansionPremiumItem:PremiumItem",
            "PremiumItem:ObtainableItem", "Everguild.LiveOps.ShopContainer:ShopContainerBase",
            "CampaignPoints:Points<T>", "ForgePoints:Points<T>",
            "Everguild.LiveOps.ExpansionPassPoints:Points<T>", "Points<T>:Points",
            // 下面三条：**类型本身不在表里**，靠它们才上溯得到条目（每条都有一条断言在盯着）
            "PrebuiltSortedDeck:PrebuiltDeck", "Energy:Currency",
            "DlcBundle:Everguild.LiveOps.ShopContainer",
        };

        /// <summary>`type` 的基类（表里查不到 ⇒ `null` = 链到头了）。
        /// ⚠️ **分隔符是 `:`，不是 `&gt;`** —— 类名里有 `Points&lt;T&gt;` 这种**带尖括号**的
        /// （用 `&gt;` 当分隔符会被 `IndexOf('&gt;')` 在 `&lt;T&gt;` 那里切断，静默解出 `"Points&lt;T"` 这种坏键）。</summary>
        static string ParentItem(string type)
        {
            for (int i = 0; i < ItemParents.Length; i++)
            {
                int gt = ItemParents[i].IndexOf(':');
                if (ItemParents[i].Substring(0, gt) == type) return ItemParents[i].Substring(gt + 1);
            }
            return null;
        }

        /// <summary>沿**本文件那张 `ItemParents`** 把这个类型名上溯到 `ItemTypeSets` 里的那一条
        /// （= `GetDrawerClass` 原来**内联**的那个循环，2026-10-11（A311）抽出来给 <see cref="IsStackable"/> 共用 ——
        /// `CLAUDE.md` §三：两处写同一条规则迟早不一致）。
        /// ⛔ **行为一字未改**：`via` 的语义照旧 = 「最后一次上溯到的那个父类型名」（没上溯过就是 `null`）。
        /// <returns>`false` = 表里没有这一条（`e` 无意义）。</returns></summary>
        static bool FindEntry(string itemType, out ItemTypeSet e, out string via)
        {
            e = default(ItemTypeSet);
            via = null;
            string t = itemType;
            for (int guard = 0; t != null && guard < 12; guard++)
            {
                for (int i = 0; i < ItemTypeSets.Length; i++)
                    if (ItemTypeSets[i].Type == t) { e = ItemTypeSets[i]; return true; }
                t = ParentItem(t);
                if (t != null) via = t;
            }
            return false;
        }

        /// <summary>物品类型名 → 它该用的**抽屉类**（= 原版 `ItemDrawer.GetDrawerConfig(item.GetType(), ov)` 的第三跳）。
        /// <para>两级匹配**照 `ItemDrawerConfig__GetReference.c`**：① **精确相等**（`b__0` · `:34` the `Equals` 谓词）；
        /// ② 没有 ⇒ **沿物品类型的基类链上溯**找一条（`b__1` = `Is(itemType, entryType)` · `:35-45`）；
        /// 都没有 ⇒ **空**（`:40-45` 返回 `(0,0)`）。找到之后**照 `GetDrawer`**：`ov == Default` 用主档，
        /// 否则按键找 `customDrawerOverrides`、**找不到回落主档**。</para>
        /// <para>🔴 **第二轮是【近似】不是【等价】（2026-10-04 收 R-X1 的 F4）** —— 差别在**两条**上：
        /// ① 原版是 `FirstOrDefault(条目 =&gt; entryType.IsAssignableFrom(itemType))`，取的是**配置列表里第一条**
        ///    祖先；我们是沿物品自己的链上溯、取**最近**的那个表内祖先。**一个类型有两个表内祖先时会分叉**
        ///    （表里现成就有这种结构：`VIPPremiumItem : ExpansionPremiumItem : PremiumItem`，三个都有条目 ——
        ///    只因 `VIPPremiumItem` 自己也在表里、走了第一轮精确匹配，才没暴露）。
        /// ② 上溯用的链是**本文件那张 `ItemParents`（21 条）**，**不是真 `.NET` 基类链** ⇒ 表漏一条时
        ///    原版会命中、我们会返回 `null`（同一个坑的另一面见下面那条告警的措辞）。
        /// ⚠️ **现有会走第二轮的类型逐个核过（3 个：`PrebuiltSortedDeck` / `Energy` / `DlcBundle`）各自表内祖先只有一个
        /// ⇒ 与 `b__1` 同结果**（2026-10-04 审查实读）。要**完全**照原版，得把 `ItemTypeSets` 排成
        /// **SO 列表序**（映射表 §② 那张表**就是按字节偏移 = 串流序排的**，可以照抄那个序）再取「第一条」——
        /// **要做**（判据齐），先做哪个 → 排在「补 `ItemParents` 全链」之后（②不修的话，①修了还是近似）。</para>
        /// <para>🆕 **2026-10-05（A79）：原在 `Shell/OfferContainer.cs`（叫 `ResolveDrawerClass`），搬进本类** ——
        /// 搬的只是**住址**。🔴 **订正痕迹（铁律 5 · A90 · 2026-10-05）**：这里原来接着写「**行为一字未改**
        /// （含对没抄的档返回 `null` 的那条守卫）」—— **不成立**（判据同 A79②；`Shell/OfferContainer.cs` 里那条「三件已搬进 `ItemDrawer`」订正注
        /// 有一份**逐条配对的同名订正**）：① **那条守卫早就不存在了** —— `Icon`(10)/`Horizontal`(15)/`Shop`(20)
        /// 三档 2026-10-03 全抄进 `ItemTypeSets`（判据 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §②），
        /// 「本表还没抄这一档」这个状态不再出现；② **「行为一字未改」对【搬】成立、对【本体】不成立** ——
        /// 本体同批已照**原版**换成「按键找不到 ⇒ **回落主档**」（就是下面 `pick` 的那个分支），
        /// 没写变体的那几档**返回值变了**（原本是 `null` + 出声）；③ **它保的那四条断言也不是「一个字都不用改」**
        /// ⚠️（那是 A79② 换守卫带来的、与【搬】无关 —— 下面 ④ 说的「不受影响」只对搬本身成立）——
        /// `Editor/ShopScene.cs` 的 `Run` 里那条 `TitleIconDrawer` 断言 那条「`…(Icon) == null`」已**翻成正値**（期望 `TitleIconDrawer`），
        /// 并加了 **16 条**逐格覆盖（`Editor/ShopScene.cs` 的 `Run` 里那 16 条逐格覆盖（`DrawerAtOv`），`DrawerAtOv.Length == 16`）。
        /// **错因**：这句抄的是 A43/A68② 那时的口径，A79② 换掉守卫之后**没人回来改它**
        /// （同一个文件 `CardPresentation/Editor/ShopScene.cs` / `CardPresentation/Editor/ShopScene.cs` 自己已经写着「守卫已删」）。搬的四条理由：
        /// ① 它复刻的 `ItemDrawerConfig` 是 **`ItemDrawer` 族自己的配置**，而那张表（`ItemTypeSets`）
        ///    A68② 就已经住在本类 ⇒ 表与读表的人分居两个文件 = 「两处写同一条规则」的温床（`CLAUDE.md` §三）；
        /// ② <see cref="PickDrawer"/>（住本类）现在**必须**调它，留在 `OfferContainer` 会**反向依赖**；
        /// ③ `ItemParents` / `ParentItem` 是它私有的伴生表，跟着走才算一个整体；
        /// ④ 唯一的外部消费方（`Editor/ShopScene.cs`）走的是 `OfferContainer.ResolveDrawerClass` 这个**转调门面**，
        ///    先 grep 过全仓反向引用（只有 `ShopScene.cs` 与 `OfferContainer.cs`），签名与语义都没动 ⇒ 它不受影响。</para></summary>
        /// <returns>`null` = **判据空**（原版这一项**什么都不画**）；`how` 里写明是哪一条（一律出声，不静默）。</returns>
        /// <remarks>🆕 **2026-10-05（A79）**：这里原来有一处 `Debug.LogWarning`，改成走 <see cref="Note"/>（**同一个 key 只报一次**）——
        /// 因为 `PickDrawer` 会带着 `Icon` 档反复进来（战役页 47 个节点 + 普查各扫一遍），
        /// 逐次刷屏会把别的告警淹掉。**出声这件事没减**（同一件事故只报一次），`how` 也照旧带全文。
        /// 🔴 **2026-10-03（A79②）**：另一处 `Note("noov|…")`（「本表还没抄这一档」）**随那条守卫一起删掉了** ——
        /// 四档抄齐 + 照原版回落主档之后，「没抄的档」这个状态**不存在了**，而「回落主档」是**原版行为**、
        /// 不是事故（原本就不该出声）。这一档到底走了哪条路，`how` 里照旧写得清清楚楚。</remarks>
        public static string GetDrawerClass(string itemType, DrawerOverride ov, out string how)
        {
            if (string.IsNullOrEmpty(itemType))
            {
                how = "物品类型**空**（`Content.ItemType` 没给）⇒ 照原版「没有配置 ⇒ 这一项什么都不画」";
                return null;
            }
            ItemTypeSet e = default(ItemTypeSet);
            string via;
            bool found = FindEntry(itemType, out e, out via);
            if (!found)
            {
                // 🔴 **这条消息必须说真话**（2026-10-04 收 R-X1 的 F5）：代码查的是**本文件那张 `ItemParents`（21 条）**，
                //    **不是真 `.NET` 基类链** ⇒ 原来的措辞「它的**基类链**上也没有」是 **overclaim**
                //    （任何「真链上有、只是本表漏抄了」的类型都会打出同一句，而那种情况下**原版会命中、我们会空手**）。
                how = "`ItemTypeSets` 那张 `ItemDrawerConfig` 表（" + ItemTypeSets.Length + " 条）里**没有** `" + itemType
                      + "`，沿**本文件那张 `ItemParents`（" + ItemParents.Length + " 条）**上溯**也没命中** —— "
                      + "⚠️ **这不等于原版判空**：原版第二轮用的是真 `Type.IsAssignableFrom`"
                      + "（`ItemDrawerConfig__GetReference.c:35-45`），**表漏一条祖先时原版会命中、我们会空手** ⇒ "
                      + "照原版「没有配置 ⇒ 这一项什么都不画」+ **在此出声**";
                Note("noentry|" + itemType, how + "（→ `资料/普查产出_1004/ItemDrawerConfig_映射表.md` 的 20 条）");
                return null;
            }

            string cls = e.Main;
            string pick;
            if (ov == DrawerOverride.Default)
            {
                pick = "主档（`override == 0`）";
            }
            else
            {
                string variant = e.Variant(ov);
                if (variant != null)
                {
                    cls = variant;
                    pick = "`customDrawerOverrides` 里键 = `" + ov + "`(" + (int)ov + ") 那一档";
                }
                else
                {
                    // 🔴 **按键找不到 ⇒ 回落主档** —— 照原版：`ItemDrawerConfig.ItemDrawerReference.GetDrawer.c:23-33`
                    //    （`override != 0` ⇒ 在 `customDrawerOverrides` 里 `Find(键 == override)`，**找不到保持主档**）
                    //    与 `ItemDrawerConfig__GetReference.c:62-72`（同形、`param_4 == 0` 直接取主档）。
                    //    ⛔ 这**不是**「判据空」、也**不是**「不许画」：是**这条记录没写这一档** ⇒ 用主档。
                    //    🔴 **订正痕迹（2026-10-03 · A79②）**：这里原来是一条守卫 ——
                    //    「本表还没抄 `Icon`(10)/`Horizontal`(15)/`Shop`(20) 这一档 ⇒ 返回 `null` + 出声，
                    //    ⛔ 不拿主档顶」。**那条守卫的前提（本表没抄）已经不成立**：三档 2026-10-03 全抄进
                    //    `ItemTypeSets`（判据 = 映射表 §②），守卫**同批换成照原版的回落**。
                    //    ⚠️ **原来那个「不拿主档顶」的取舍错在哪**：原版本来就会回落主档（两处反编译同形），
                    //    所以「返回 `null`」等于**我们比原版少画**；真正的风险（拿主档顶替**一个该用变体的档**）
                    //    现在由**表本身**排除 —— 写了变体的档一定取到变体，`null` 只可能是真没写。
                    pick = "**回落主档**（这一条**没写** `" + ov + "`(" + (int)ov + ") 那一档 ⇒ 照 `GetDrawer.c:23-33`"
                         + " / `GetReference.c:62-72`「按键找不到 ⇒ 用主档」）";
                }
            }
            how = "`" + itemType + "`" + (via != null ? "（基类链上溯到 `" + e.Type + "`）" : "")
                  + " + " + pick + " ⇒ 抽屉类 **`" + cls + "`**";
            return cls;
        }

        /// <summary>**原版抽屉类 → 我们的实现**（`cls:impl`）—— 真表那一跳的最后一跳。
        /// <para>⚠️ **现读覆盖 24 档**（2026-10-30 `B101_E3`：野牌两档 + `Deck`/`DeckAndCardback`/`CampaignPoints`/`Currency`
        /// + 17 个新类，各档实现在 `Shell/ItemDrawer&lt;名&gt;.cs`，判据见各自头注）；**仍缺 3 个原版类**：
        /// `CardDrawer` / `CardAlternateArtDrawer`（卡在「本机没有 `BasicCardUI` 等价件」）· `GenericArmyItemDrawer`（本工程零消费点）。
        /// ⇒ 表里列不到的类返回 `null`，<see cref="PickDrawer"/> **出声 + 兜底**，⛔ 不静默画成别的样子。</para>
        /// <para>🔴 `"WildcardDrawer:WildcardDrawer"` 这种「左右同名」**是巧合，不是恒等**：左边是**原版**
        /// 那个类的名字，右边是**我们**给实现的常量名（<see cref="DrawerWildcard"/>）。
        /// 我们的常量名当年就是照原版类名起的 ⇒ 今天撞在一起。**别据此把这张表当成「名字相同就算实现」**。</para></summary>
        public static readonly string[] DrawerImpls =
        {
            "WildcardDrawer:" + DrawerWildcard,          // 原版 `WildcardDrawer : ItemDrawer<Wildcard>`
            "WildcardIconDrawer:" + DrawerWildcardIcon, "DeckDrawer:" + DrawerDeck, "DeckAndCardbackDrawer:" + DrawerDeckAndCardback, "CampaignPointDrawer:" + DrawerCampaignPoint, "CurrencyDrawer:" + DrawerCurrency, "AvatarDrawer:" + DrawerAvatar, "AvatarBorderDrawer:" + DrawerAvatarBorder, "CardbackDrawer:" + DrawerCardback, "CosmeticDrawer:" + DrawerCosmetic, "ContainerDrawer:" + DrawerContainer, "RandomCardDrawer:" + DrawerRandomCard, "TitleDrawer:" + DrawerTitle, "TitleIconDrawer:" + DrawerTitleIcon, "TitleDrawerHorizontal:" + DrawerTitleHorizontal, "PremiumDrawer:" + DrawerPremium, "PremiumIconDrawer:" + DrawerPremiumIcon, "ExpansionPassPremiumDrawer:" + DrawerExpansionPremium, "ForgePointDrawer:" + DrawerForgePoint, "ForgePointIconDrawer:" + DrawerForgePointIcon, "ExpansionPassPointDrawer:" + DrawerExpansionPassPoint, "AllianceBadgeDrawer:" + DrawerAllianceBadge, "XSollaOfferDrawer:" + DrawerXSolla, "CardDrawer:" + DrawerCard, "CardAlternateArtDrawer:" + DrawerCardAltArt,  // 原版 `WildcardIconDrawer : ItemDrawer<Wildcard>`；后两条 = 2026-10-26（批次 3 · Q1）新建（判据 = `DeckDrawer.cs:7` `DeckAndCardbackDrawer : DeckDrawer`）；`CampaignPointDrawer` = 2026-10-28（B90-N6，账 战役:53/55）；`CurrencyDrawer` + 其后 18 个类 = 2026-10-30（B101_E3 抽屉补齐，判据 = `判据/B100_D1_抽屉18档_施工单.md` 表 B）
        };

        /// <summary>原版抽屉类 → 我们的实现（表里查不到 ⇒ `null` = **这个抽屉我们没实现**）。</summary>
        public static string ImplOfDrawerClass(string cls)
        {
            if (string.IsNullOrEmpty(cls)) return null;
            for (int i = 0; i < DrawerImpls.Length; i++)
            {
                int c = DrawerImpls[i].IndexOf(':');
                if (DrawerImpls[i].Substring(0, c) == cls) return DrawerImpls[i].Substring(c + 1);
            }
            return null;
        }

        /// <summary>同一件事故**只出声一次**（`CLAUDE.md` §三：不许静默失败 —— 但也不该刷屏）。
        /// key 不变就不再报；key 变了照报。本工程既有同款（`Core/CardIcons.cs` 的 `_warned`、
        /// `Core/Tooltip.cs` 的 `_warnedNoEntry`）。
        /// ⚠️ **进程内静态**：一次 Unity 批处理里那就是「每个 key 一次」；批处理之间会重置。</summary>
        static readonly System.Collections.Generic.HashSet<string> _noted =
            new System.Collections.Generic.HashSet<string>();
        static void Note(string key, string msg)
        {
            if (!_noted.Add(key)) return;
            Debug.LogWarning("[ItemDrawer] " + msg);
        }


        // ============================================================ 从 id 推物品（**我们挑的**）

        /// <summary>🆕 **2026-10-05（A79）**：**id → 原版类型**的实据表（`id|原版类型名`）。
        /// <para>🔴 **逐条都是「本地有那个 SO、且它的 `m_Script` 解出的类就是这个」**，链同 `工具/read_itemdrawerconfig.py`：
        /// `MonoBehaviour.m_Script` 的 pathID → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_&lt;pathID&gt;.json`
        /// 的 `m_Namespace` + `m_ClassName`（两者拼起来才是 `ItemTypeSets` 的键）。**9 条 SO 逐个解过，零条靠猜。**</para>
        /// <list type="bullet">
        /// <item>`WildcardUltramarines1..4` → `Wildcard` —— `bundle_menus_assets_all/MonoBehaviour/WildcardUltramarines1..4.json`
        ///   （`m_Script` pathID `3072300046260315001` ⇒ `m_Namespace` 空 + `m_ClassName` `Wildcard`）。
        ///   ⚠️ 这一档**不写在本表里**（它是 `Wildcard&lt;阵营&gt;&lt;档&gt;` 那条**模式**认出来的，见 <see cref="Spec"/>）。</item>
        /// <item>`Booster Pack Ultramarines` · `Booster Pack Legendary Ultramarines` → `Everguild.LiveOps.ShopContainer`
        ///   （两份 SO 同一条 `m_Script`，pathID `-5965435178007098505` ⇒ `m_Namespace` `Everguild.LiveOps`
        ///   + `m_ClassName` `ShopContainer`）。</item>
        /// <item>`DT Ultramarines All` · `DT Ultramarines All R2+` → `DropTableItem`（pathID `-2313241570638646064`，
        ///   命名空间空）。</item>
        /// </list>
        /// <para>⛔ **表里没有的 id 一律判据空**（`Spec` 给 `Type = null` ⇒ `PickDrawer` 出声 + 兜底）——
        /// **不许按前缀 / 名字像就推广**：`DT Ultramarines R3` / `R4` 与 `DT Ultramarines All` 只差一个后缀，
        /// 而**本地没有它们的 SO**（`DT Ultramarines All.json` 的 `dropTableItems` 里那两条 `reference` 是 `{0,0}`）
        /// ⇒ 推不出 `DropTableItem`，**只能留 `null`**（铁律 2：查不到就说查不到）。
        /// 同理 `C2` / `UMxx` / 32 位 hex 那批**连 SO 都没导出**。</para>
        /// <para>⚠️ 分隔符用 **`|`** 不用 `:`：左边是 id、右边是**含命名空间**的类型名（`Everguild.LiveOps.…`），
        /// 用 `:` 会在第一个命名空间点处切断（同 `ItemParents` 那条注释的坑）。id 本身不含 `|`。</para></summary>
        public static readonly string[] IdTypes =
        {
            "Booster Pack Ultramarines|Everguild.LiveOps.ShopContainer",
            "Booster Pack Legendary Ultramarines|Everguild.LiveOps.ShopContainer",
            "DT Ultramarines All|DropTableItem",
            "DT Ultramarines All R2+|DropTableItem",
        };

        /// <summary>从 id + 数据层给的图/短名组一个 <see cref="ItemSpec"/>。
        /// 🔴 **种类是从 id 推的**（原版按 .NET 类型分，我们读不到那套）—— 三条依据：
        /// ① `Wildcard&lt;阵营&gt;&lt;档&gt;`：本地 4 条 SO 实测 `cardRarity` = 后缀、`cardArmy` = 10，
        ///    `物品 SO` 就在 `bundle_menus_assets_all/MonoBehaviour/WildcardUltramarines1..4.json`；
        ///    ⚠️ **「阵营名写进 id ⇒ cardArmy 就是它」这一步是【推广】**（只有 UM 那 4 条可证）——
        ///    认不出阵营名时 `CardArmy = 0`，那一层就不画（不猜）。
        /// ② `Booster Pack Ultramarines`：SO 的 `containerPreviewImage` 实读到图名 ⇒ 有图、但**用哪个抽屉不知道** ⇒ `Generic`。
        /// ③ 其余：`art` 有 ⇒ `Generic`；`art` 没有 ⇒ `Unknown`（**判据空**，走占位板 + 出声）。
        /// <para>🆕 **2026-10-05（A79）**：本函数现在还填 <see cref="ItemSpec.Type"/>（= 查真表用的键）——
        /// 两个来源：<see cref="IdTypes"/> 那张实据表（逐 id）+ `Wildcard&lt;阵营&gt;&lt;档&gt;` 那条**模式**
        /// （4 条 SO 实读，`m_ClassName` 就是 `Wildcard`）。**两者都只覆盖「本地有 SO 且解过」的 id**，
        /// 其余一律 `null` ⇒ 上层出声 + 兜底。</para></summary>
        public static ItemSpec Spec(string targetId, string art, string label)
        {
            var s = new ItemSpec { Id = targetId, Kind = ItemKind.Unknown, Art = art ?? CurrencyNames.Icon(targetId), Label = CurrencyNames.Of(targetId) ?? label };   // 2026-10-28（B89_E17/E17b）：货币 code（`C2/C3/C5`）的名字走原版 `Currency.DisplayName` 那条词条、主图走 `Currency.GetIcon(Large)` = SO 的 `bigIcon`（三段 guid 链逐条实读）；判据 / 边界 → `Shell/CurrencyNames.cs` 文件头。⚠️ 零行数变化（本行只换内容）
            if (string.IsNullOrEmpty(targetId)) { s.Kind = ItemKind.None; return s; }

            // 原版类型：先查实据表（`IdTypes`），命中即定；再走野牌那条模式（下面 `StartsWith("Wildcard")`）。
            for (int i = 0; i < IdTypes.Length; i++)
            {
                int bar = IdTypes[i].IndexOf('|');
                if (IdTypes[i].Substring(0, bar) == targetId) { s.Type = IdTypes[i].Substring(bar + 1); break; }
            }

            if (targetId.StartsWith("Wildcard"))
            {
                string rest = targetId.Substring("Wildcard".Length);
                for (int i = 0; i < ArmyNames.Length; i++)
                {
                    if (i == 0 || !rest.StartsWith(ArmyNames[i])) continue;      // 跳过 Neutral
                    string tail = rest.Substring(ArmyNames[i].Length);
                    if (tail.Length == 1 && tail[0] >= '1' && tail[0] <= '4')
                    {
                        s.Kind = ItemKind.Wildcard;
                        s.Type = TypeWildcard;      // 🆕 A79：`WildcardUltramarines1..4` 的 SO 类实读 = `Wildcard`
                        s.Rarity = tail[0] - '0';
                        s.CardArmy = ArmyValues[i];
                    }
                    break;
                }
            }
            if (s.Kind == ItemKind.Unknown && !string.IsNullOrEmpty(s.Art)) s.Kind = ItemKind.Generic;   // 🔴 2026-10-28（B89_RD 修正）：原来判的是**入参** `art`，而货币 code（`C2/C3/C5`）的主图是上面那行 `art ?? CurrencyNames.Icon(targetId)` 解出来的 ⇒ 入参为 null 时 `Kind` 停在 `Unknown` ⇒ `PickDrawer` 落到 `DrawerPlaceholder`、**走不到** `Icon` 里那句货币转交（`ItemDrawer.cs:1297`）⇒ 战役奖励的 `C2` 画的是占位板。判据 = 本行上一句自己：能画出图就应该走 `Icon` 那一档（零行数变化：同一行只换内容）
            return s;
        }

        // ============================================================ ③④ 建抽屉 + 画

        /// <summary>原版 `ItemDrawer.Draw(Transform parent, ObtainableItem item, int quantity = 1, DrawerOverride drawerOverride = 0)`
        /// —— **参数顺序与默认值逐个对照**，只把 `ObtainableItem` 换成 `ItemSpec`，并多要两样：
        /// 一个 `box`（原版抽屉的尺寸在 prefab 里，我们没有 ⇒ 框由调用方给）与一个 `style`（分层队列等）。
        /// <para>**四步照 `ItemDrawer__Draw.c`**：① `PickDrawer` 选抽屉 ② `null` ⇒ **不画**（空结果）
        /// ③「Instantiate」= 在 `parent` 下建抽屉根节点 ④ 调抽屉自己的 `Draw(item, quantity, options)`。</para>
        /// <para>⚠️ **`SetAsFirstSibling` 不在这里** —— 原版是**调用方**在 `Draw` 之后自己调的
        /// （`CampaignRewardsWindow__Open.c` 那句、正本 `:598`）⇒ 谁要「挤到列首」谁自己调。</para></summary>
        public static ItemDrawResult Draw(Transform parent, PxRect box, ItemSpec item, int quantity,
                                          DrawerOverride drawerOverride, ItemDrawerStyle style)
        {
            var res = new ItemDrawResult { Id = item.Id, Kind = item.Kind };
            if (parent == null || box.W <= 0.01f || box.H <= 0.01f) return res;

            string drawer = PickDrawer(item, drawerOverride);
            if (drawer == null) return res;                                 // ② 不画
            res.Drawer = drawer;
            res.Node = MenuDraw.Node(parent,                                       // ③「Instantiate」
                string.IsNullOrEmpty(style.NodeName) ? "Item Drawer" : style.NodeName, box); ResetDecorationLayers(res.Node);   // B0（施工单 §一-2 / §四-3）：原版基类 `Draw` 进门先复位 converted/ephemeral/claimedWarning/premiumHighlight 四个装饰状态（四句虚表调用）；同一物理行追加（铁律 18·b · 账 A 落点行号不动）

            switch (drawer)                                                        // ④ 抽屉自己的 Draw（实现只此一份 = `Dispatch`，`Refresh()` 也走它）
            {
                case DrawerWildcard: case DrawerWildcardIcon: case DrawerIcon: case DrawerDeck: case DrawerDeckAndCardback: case DrawerCampaignPoint: case DrawerCurrency: case DrawerAvatar: case DrawerAvatarBorder: case DrawerCardback: case DrawerCosmetic: case DrawerContainer: case DrawerRandomCard: case DrawerTitle: case DrawerTitleIcon: case DrawerTitleHorizontal: case DrawerPremium: case DrawerPremiumIcon: case DrawerExpansionPremium: case DrawerForgePoint: case DrawerForgePointIcon: case DrawerExpansionPassPoint: case DrawerAllianceBadge: case DrawerXSolla: case DrawerCard: case DrawerCardAltArt:
                    Dispatch(drawer, res.Node, box, item, quantity, style, ref res); break;
                default: Placeholder(res.Node, box, item, quantity, style, ref res); break;
            }
            res = Bind(res, box, item, quantity, style);
            return res;
        }

        // ============================================================ ⑤ 奖励状态那三跳（🆕 2026-10-11 · A311）
        //
        //  这三跳**不是**抽屉自己画的 —— 原版是 `RewardWindow.Open` 在 `ItemDrawer.Draw` **之后**，
        //  对着刚建出来的那个抽屉逐个调的（`RewardWindow__Open.c:111-147`，四跳里的前三跳）：
        //    ① `drawer.<虚表 0x1b8>((reward.rewardTier(+0x30) == 10))`   = `TogglePremiumHighlight(bool)`
        //    ② `reward.convertedInto(+0x38) == null && reward.IsEphemeral` ⇒ `drawer.<0x1e8>(reward.ephemeralState(+0x28))`
        //    ③ `reward.convertedInto != null`                             ⇒ `drawer.<0x1d8>(reward.convertedInto)`
        //  🔴 **槽位↔方法名是算出来的**（不是猜）：
        //    `ItemDrawer` 的虚表基址 = `0x138`、每档 16 字节 ⇒ `0x1b8`→档 8、`0x1e8`→档 11、`0x1d8`→档 10；
        //    `d:/2/tools/il2cpp_out/dump.cs:93330/93339/93336` 的 `Slot: 8/11/10` 正是
        //    `TogglePremiumHighlight` / `SetEphemeralDisplay` / `SetConvertedItem`。
        //    `0x1d8` 那一处**反编译里看着没传参**（`(**(code **)(*plVar8 + 0x1d8))(plVar8);`）——
        //    **是 Hex-Rays 丢了实参**：现读指令流，`1807BD53F` 先把 `reward.convertedInto` 装进 `rdx`
        //    （`mov rdx,[r14+0x38]`）、`1807BD546 jne` 跳到 `1807BD570`，那里只补 `rcx` 就 `call rax`
        //    ⇒ **`rdx` 一路带过去**，实参就是 `convertedInto`（⛔ 不是 `reward` 自己）。
        //
        //  🔴 **三个方法体是【现读】的**：`ItemDrawer<T>` 的泛型实现在 `decomp_full/` 里**没有 `.c`**
        //    （`d:/2/tools/wanted_full.txt` 里没有这批名字 ⇒ 当年没请求反编译，所以按类名 grep 一定空）。
        //    出路 = **按 VA 反汇编**（`工具/disasm_va.py`，`d:/2/unity_run_ref/GameAssembly.dll`）：
        //      · `TogglePremiumHighlight`  RVA `0x1AEAA30`（`18224048`）
        //      · `SetEphemeralDisplay`     RVA `0x1AEA690`
        //      · `SetConvertedItem`        RVA `0x1AEA3B0`
        //    三个方法体都**只碰 `ItemDrawerComponents` 的字段**（字段表 `dump.cs` 的
        //    `background 0x20 / image 0x28 / label 0x30 / quantity 0x38 / premiumHighlight 0x40 /
        //     premiumBadge 0x48 / claimedWarning 0x50 / conversionDisplay 0x58 / convertedItem 0x60 /
        //     convertedLabel 0x68 / ephemeralDisplay 0x70 / ephemeralText 0x78`）——
        //    这一点是**互证**：三个反汇编里读的 `+0x40 / +0x58 / +0x60 / +0x68 / +0x70 / +0x78`
        //    与字段表逐个对上（⛔ 不是我按名字猜的）。

        /// <summary>**第 1 跳**：原版 `TogglePremiumHighlight(bool)` —— 方法体**全篇只有一句**
        /// `Components.premiumHighlight.SetActive(toggle)`（`[+0x40]`，另有 `.op_Implicit` 那道 Unity 空判）。
        /// <para>那三件**画什么**照 prefab：`Highlight` 铺满抽屉框、`sizeDelta (50,50)`（拉伸锚 ⇒ **每边外扩 50px**）、
        /// sprite `OctagonUI Filled Fade SDF`、`m_Type 1(Sliced)`、`m_Color (1,1,1,0.8)`、材质 `Border Highlight Appear Green`；
        /// `Blackout` 铺满抽屉框、**无 sprite**、`m_Color (0,0,0,0.5529412031173706)`；
        /// `Badge` 锚 `(0,0.7)-(0.3,1.0)` = **左上 30%×30%**、`m_PreserveAspect 1`、sprite `40k_campaign_Premium-icon`。
        /// 三者的**兄弟序** = `Highlight` → `Blackout` → `Badge`（后者压前者）⇒ 队列按同序 +1。</para>
        /// <remarks>**改坏法**：`Badge` 那四个数里把 `0.3` 改成 `0.5` ⇒ 自检「角标 = 左上 30%×30%」那条红；
        /// 把 `Blackout` 的 `0.5529412` 改成 `1` ⇒ 「压暗层 alpha」那条红；少画 `Highlight` ⇒ 「premium 三件齐」那条红。</remarks></summary>
        public static void SetPremium(Transform drawer, PxRect box, ItemDrawerStyle st)
        {
            if (drawer == null) return;
            float k = box.H / DecorRefH;
            var root = MenuDraw.Node(drawer, NodePremiumHighlight, box);
            float pad = 50f * k;                       // = prefab 的 `sizeDelta (50,50)`（每边外扩）
            MenuDraw.Rect(root, Tex(ArtPremiumHighlight), new PxRect(box.x1 - pad, box.y1 - pad,
                          box.x2 + pad, box.y2 + pad), NodeHighlight, st.QDecor, HighlightColor, false,
                          st.Clip, st.ClipSoftness);
            MenuDraw.Rect(root, CardArt.Solid(), box, NodeBlackout, st.QDecor + 1, BlackoutColor, false,
                          st.Clip, st.ClipSoftness);
            MenuDraw.Rect(root, Tex(ArtPremiumBadge), new PxRect(box.x1, box.y1,
                          box.x1 + box.W * BadgeFrac, box.y1 + box.H * BadgeFrac), NodePremiumBadge,
                          st.QDecor + 2, null, true, st.Clip, st.ClipSoftness);
        }

        /// <summary>**第 2 跳**：原版 `SetEphemeralDisplay(EphemeralState)`。方法体实读（`0x1AEA690`）共四句：
        /// ① `if (Components.ephemeralDisplay == null) return;`（`[+0x70]`，Unity 空判）
        /// ② `if (state == null || !state.HasExpiration) { ephemeralDisplay.SetActive(false); return; }`
        /// ③ `ephemeralDisplay.SetActive(true)`
        /// ④ `Components.ephemeralText`（`[+0x78]`）**`.Setup(串, null)`** ——
        ///    串 = `SupportMethods.GetTimeString(state.EphemeralEndTime − state.EphemeralStartTime,
        ///    eventTimer: true, clampTo0: false, hide0Values: false)`
        ///    **`.Replace("0m","").Replace("0h","")`**（两个 `String.Replace` 的实参是**字符串字面量**
        ///    `0m` / `0h` 与空串，按 `d:/2/tools/il2cpp_out/script.json` 的 `ScriptString` 表
        ///    逐地址解出来：`0x42B8338 = "0h"` · `0x42B8438 = "0m"` · `0x42B80E0 = ""`）。
        ///    🔴 **`iconSprite` 传的是 `null`** ⇒ 照 `PriceDisplay.Setup(string, Sprite)` 的实读行为
        ///    （`0x180794DC0`：`icon.gameObject.SetActive(iconSprite != null)`）**那一行整个不画图标**
        ///    ⇒ 我们这里**只画字**（⛔ 别照 prefab 里那个 `icon` 节点补一个图）。
        /// <para>⚠️ 原版画的是 **`EndTime − StartTime` = 总时长**、**不是剩余时间**（那一句就是 `DateTime.op_Subtraction`，
        /// 两个操作数都在参数上）—— prefab 的占位串 `24 hours` 也对得上。</para>
        /// <remarks>**改坏法**：把 <see cref="EphemeralTextOf"/> 的 `Replace("0h","")` 删掉 ⇒
        /// 自检「`24h` 那串里没有 `0h`」那条红；给这一行补一个图标 ⇒ 「ephemeral 行只有一段字」那条红。</remarks></summary>
        public static void SetEphemeral(Transform drawer, PxRect box, ItemDrawerStyle st)
        {
            if (drawer == null) return;
            var strip = Strip(box);
            var root = MenuDraw.Node(drawer, NodeEphemeralDrawer, strip);
            TextCentered(root, strip, EphemeralTextOf(st.EphemeralMs), st.QDecor + 4, st);
        }

        /// <summary>**第 3 跳**：原版 `SetConvertedItem(RewardInfo convertedTo)`。方法体实读（`0x1AEA3B0`）：
        /// ① `if (Components.conversionDisplay == null) return;`（`[+0x58]`）
        /// ② `if (convertedTo == null) { conversionDisplay.SetActive(false); return; }`
        /// ③ `conversionDisplay.SetActive(true)`
        /// ④ `Components.convertedItem`（`[+0x60]`，`PriceDisplay`）**`.Setup(convertedTo.Quantity.ToString(),
        ///    图)`** —— 图 = `((ICurrency)convertedTo.Item).GetIcon(IconSize.Small)`：
        ///    反汇编里先取 `convertedTo` 的 **档 13 = `get_Item`**（`0x208/0x210`），
        ///    再走 `il2cpp` 的接口查找（`0x181AEA52E` 那个 `r9` 槽 = `0x429E6F0`，
        ///    在 `script.json` 的 `ScriptMetadata` 里明写 `ICurrency_TypeInfo`）到 `ICurrency` 的**档 2**
        ///    = `GetIcon(IconSize small)`；实参 `edx = 0` = **`IconSize.Small`**（`dump.cs`：`Small = 0`）。
        /// ⑤ `Components.convertedLabel`（`[+0x68]`，TMP）`.text = I2_Loc.LocalizationManager.GetTranslation(
        ///    "MainMenu/RewardWindow/AlreadyOwned", …)` —— 词条 key 是字面量（同一张 `ScriptString` 表，
        ///    地址 `0x42C1318` 实读）；**词条正文在远端 CCD、本地没有** ⇒ 照本仓既有口径
        ///    （`RewardWindow.TxtPremium` 那条）画 prefab 里的那串英文 `Already Owned`。
        /// <para>⚠️ 第 ④ 步那两处**不是**「数字 + 图标」摆两个位置就完了 —— 原版 `PriceDisplay` 内部是
        /// **一个「图标 + 文字」的横排，整组在 `PriceDisplay` 盒里居中**（两个实测点：
        /// converted 的 `icon` 左沿 `127.2207` = `(456.9613 − (93.5475 + 108.97)) / 2` ·
        /// ephemeral 的 `86.4112` = `(456.9613 − (93.5475 + 190.61)) / 2`）⇒ 我们按「量出文字宽再整组居中」实现。</para>
        /// <remarks>**改坏法**：把 `ConvertedQuantity` 换成 `1` ⇒ 自检「那一行写的是 converted 的数量」那条红；
        /// 不传 `ConvertedArt` ⇒ 「图标那格没建」那条红；把 `Already Owned` 那串删掉 ⇒ 「说明字」那条红。</remarks></summary>
        public static void SetConverted(Transform drawer, PxRect box, ItemDrawerStyle st)
        {
            if (drawer == null) return;
            var strip = Strip(box);
            var root = MenuDraw.Node(drawer, NodeConvertedDrawer, strip);
            TextCentered(root, strip, st.ConvertedQuantity.ToString(), st.QDecor + 4, st,
                         Tex(st.ConvertedArt));
            // `AlreadyOwned`：锚 `(0.5,0)-(0.5,0)`（条**下沿**）、`pos (0,−108)`、`size (506.96,108)`
            //   ⇒ 条宽 × 108·k 高、**整条落在条的下方**（中心在条下沿再往下 108·k）。
            //   ⚠️ 「下」在 uGUI 里是 y 减小，而本工程 `PxRect.y1` 是**上沿** ⇒ 落到 `strip.y2 + 54·k … strip.y2 + 162·k`。
            float k = box.H / DecorRefH;
            var lb = new PxRect(strip.x1, strip.y2 + 54f * k, strip.x2, strip.y2 + 162f * k);
            // 🔴 **2026-10-19（A1187）**：这四格 = 原版 `AlreadyOwned` 那颗 TMP 的**原文**，
            //   出处 = `工具/menu_dump.py bundle_menus_assets_all "Deck Drawer"|"Wildcard Drawer"|…`
            //   （六个抽屉 prefab 逐个现读，**逐值相同**）：
            //     `AlreadyOwned` = `'Already Owned' 字号=75.0 基准=36.0 auto[12.0~75.0] 对齐=Center/Middle 折行=0`
            //   ⇒ `min 12` · `max 75`（= `m_fontSize` 本身）· `base 36` · **折行 0**。
            //   ⚠️ **三格一起乘 `k`**（`k = box.H / 1080`）—— 本函数传的 `fontPx = 75f * k` 就是
            //   原版 `m_fontSize × k`（同文件 `TextCentered` 的 `65f * k` 是同一条换算）
            //   ⇒ `min/max/base` 与 `nominal` 必须**同一把尺**（先例 = `Shell/MissionsTab.cs` 的 `FS()`，
            //   本仓 P4 报告 §3 已把这条口径逐处核过）。
            // 🆕 **2026-10-17（`A1753` #8）**：屏上颜色 = `Off White Warm` × 顶点 (1,1,0.737) = (0.972, 0.937, 0.638)
            //   （改前传纯白 ⇒ 整条说明字偏冷）。材质/顶点逐颗实读，判据见 `AlreadyOwnedInk` 的 summary。
            ClippedText(root, lb, AlreadyOwnedText, AlreadyOwnedInk, NodeAlreadyOwned, 75f * k, st.QDecor + 5,
                        lb.W, st, 0, 12f * k, 75f * k, 36f * k, true, TmpFont.Face.LatinCardBody);
            // 🆕 **2026-10-17（第四十八会话 · A1212 块 11 #69）**：原版 `Converted Drawer` 里**也有**
            //   `Content/Label/Quantity`（四格 `12/75/36/0`）—— 我们原来只建了 `Price Display` +
            //   `AlreadyOwned` ⇒ 自检「Quantity 建出来了」红。走同一份 `Quantity()`（⛔ 不另写一套排版）。
            Quantity(root, box, st.ConvertedQuantity, st);
        }

        /// <summary>第 2/3 跳那两个条（`Converted Drawer` / `Ephemeral Drawer`）**同锚点**：
        /// `(0.05,0.15)-(0.95,0.25)`、`pivot (0.5,0.5)`（六个抽屉 prefab 逐个实读，一模一样）
        /// ⇒ 横向 5%..95%、纵向 15%..25%（**相对抽屉框**，与框的大小无关 ⇒ 这一条是原版值、不是折的）。
        /// ⚠️ 原版那两件是挂在抽屉 `Content` 下的；我们这一层没有单独的 `Content` 节点（`Draw` 直接把层建在抽屉根下）
        /// ⇒ 挂在抽屉根下，比例照旧。
        /// <para>⛔ **这两个条自己的底板（那张 `Image`）没画** —— 它的 `m_Sprite` 是**外链**（`m_FileID 3`、
        /// `m_PathID 1838051009033412331`），那个 pid **不在 `bundle_menus_assets_all` 里**（实查：
        /// `UnityPy` 读整包，`Sprite` 一共 22 张，没有它），**我没查出它叫什么**（⛔ 不猜一张图顶上）⇒
        /// 今天这两个条只有「图标 + 文字」，**没有底**。⚠️ 这条链在我们数据上**今天走不到**
        /// （89 条战役奖励 `IsEphemeral` 全假、`convertedInto` 全空），但**要做**、别当已实现。</para></summary>
        static PxRect Strip(PxRect box)
        {
            // ⚠️ **锚是 uGUI 的 y（自下往上）**，而本工程的 `PxRect.y1` 是**上沿**（同文件 `NameStrip`「顶边 = y1」·
            //    `Quantity`「底边 = y2」那两条口径）⇒ **竖向要翻**：锚 0.15..0.25 落到 `box.y2 − 0.25H … box.y2 − 0.15H`。
            return new PxRect(box.x1 + box.W * 0.05f, box.y2 - box.H * 0.25f,
                              box.x1 + box.W * 0.95f, box.y2 - box.H * 0.15f);
        }

        /// <summary>`PriceDisplay` = 「图标（可选）+ 一段字」的横排、**整组居中在条里**（见 `SetConverted` 的实测点）。
        /// 图标边长 = `93.5475 × k`（prefab 的 `icon` 节点 `93.5475²`）；字高 = `65 × k`（prefab 的 `text` 字号）；
        /// 图标与文字之间**没有间距**（prefab 实测：图标右沿 `220.9904`、文字左沿 `220.7705` ⇒ 相接）。
        /// ⚠️ 我们量的是**字形宽**（`Label.WorldW`），原版排的是 TMP 的**矩形宽** ⇒ 居中会差一点点（如实记）。</summary>
        static void TextCentered(Transform parent, PxRect strip, string s, int q, ItemDrawerStyle st,
                                 Texture2D icon = null)
        {
            if (string.IsNullOrEmpty(s)) return;                     // 原版 `Setup(串, …)` 的串不会是空的
            float k = strip.H / (DecorRefH * 0.10f);                 // 条高 = 0.10 × box.H ⇒ 反推 box.H/1080
            float iconSide = 93.5475f * k, textPx = 65f * k;
            // 🔴 **2026-10-17（界面线 `T07` · `A1243`）：价签那段字【自己】的框高 = 原版 `…/Price Display/text`
            //   的 **93.55**（`ContentSizeFitter h:PreferredSize`；`'2000'@65` 复算 `GetPreferredWidth = 108.96`
            //   ↔ 序列化 `108.97`）。判据 = `资料/普查产出_1017/UI/R_T07_剩余四项.md` §一·1 #4。
            //   ⚠️ **别把父件 `Price Display` 的 `108` 当字框** —— 108 是**父节点**的 `m_SizeDelta.y`（= `strip.H`，
            //   0.10 × box.H）；字框是它里面那颗 `text`（CSF 只撑**宽**，高由 TMP 行盒给）。
            //   改前这一句写的是 `iconSide`（= 93.5475 × k，与 93.55 只差 0.0025）—— 数值上本来就对，
            //   这里写成**显式字面量**是为了让「框高 = 93.55」在代码里可读、不被后人按 108 改回去。
            float priceTextH = 93.55f * k;
            var row = new PxRect(strip.x1, strip.CY - priceTextH * 0.5f, strip.x2, strip.CY + priceTextH * 0.5f);
            // 🔴 2026-10-13（A435 甲）：`Visible` → **`VisibleAbove`**（要把 `parent` 交进去才解析得到节点；
            //   原来那个纯矩形重载在 `st.Clip == null` 时**恒真** ⇒ 节点态下这一问是空转）。
            if (!MenuDraw.VisibleAbove(parent, row, st.Clip)) return;
            // 原版这一层的节点名逐字是 `Price Display`（那一行），里面才是 `icon` / `text` 两件 ⇒ 照建。
            var pd = MenuDraw.Node(parent, NodePriceDisplay, row);
            // 🔴 **2026-10-19（A1187②）**：这四格 = 原版 `Price Display/text` 那颗 TMP 的**原文**，
            //   出处 = `工具/menu_dump.py bundle_menus_assets_all "<抽屉名>"`（**六个抽屉 prefab 逐个现读，
            //   逐值相同**）：`text` = `'2000'/'24 hours' 字号=65.0 基准=39.0 auto[13.460000038146973~65.0]
            //   对齐=Center/Capline 折行=0` ⇒ `min 13.46` · `max 65`（= `m_fontSize` 本身）· `base 39` · **折行 0**。
            //   ⚠️ **三格一起乘 `k`**（`k = box.H / 1080`，本函数上面那行算的）—— 本函数传的
            //   `textPx = 65f * k` 就是原版 `m_fontSize × k` ⇒ 上下限必须同一把尺（先例同 `SetConverted`）。
            //   ⚠️ **不是** `R2` 报告 §2#44 说的「67 颗 `m_text='300,00'` 的 `Price Display/text`」——
            //   那一族是**商店/礼包按钮**里的同名节点（`min/max` 分 `13.46~40` / `12~54` 两族）；
            //   本处这一颗是**抽屉自己**那一颗（`max = 65`），判据见上面那次实读。
            //   ⚠️ 原版这一颗的父件 `Price Display` 带 `HorizontalLayoutGroup`、它自己带 `ContentSizeFitter`
            //   （**框宽跟文字走**）⇒ 原版**没有**一条固定的「框宽」；这里传的 `row.W` = 我们这一层的条宽
            //   （= 原版 `Price Display` 那个节点的矩形，640.8×108 设计值 ⇒ 与六份 prefab 逐位相同；
            //     ⚠️ 我们传给**字**的框是 `strip.W × 93.55` —— 折行 0 ⇒ 宽不咬字，高才是原版 `text` 那一档），
            //   `折行` 照原版关回 0 ⇒ 这一格是**下界守卫**、不吃掉任何排版自由度。
            // 🆕 **2026-10-17（`A1753` #9）**：原版 `Price Display/text` 挂 `Pragati-Regular Light Cream`
            //   （`_FaceColor = (0.992, 0.868, 0.733)`、顶点白）⇒ 屏上是暖白，不是纯白。
            var lb = MenuDraw.Text(pd, row, s, PriceInk, NodePriceText, textPx, q, row.W,
                                   13.46f * k, 65f * k, 39f * k, face: TmpFont.Face.LatinCardBody);
            // ⓪ 原版折行=0（`SetAutoFitBox` 会把模式开成 `Normal(1)`）—— ⚠️ 必须在下面那次
            //    `MenuDraw.ClipText(lb, …)`（本方法的最后一步）**之前**：`SetWrapping` 会 `ForceRelayout` 推版面。
            if (lb != null) lb.SetWrapping(false);
            float tw = lb != null ? lb.WorldW * 108f : 0f;           // `WorldW` 是 Unity 单位 ⇒ ×108 回画布像素
            float groupW = (icon != null ? iconSide : 0f) + tw;
            float gx = row.CX - groupW * 0.5f;                       // 整组居中
            if (icon != null)
                MenuDraw.Rect(pd, icon, new PxRect(gx, row.CY - iconSide * 0.5f,
                              gx + iconSide, row.CY + iconSide * 0.5f), NodePriceIcon, q - 1, null, true,
                              st.Clip, st.ClipSoftness);
            if (lb != null)
            {
                float tx = gx + (icon != null ? iconSide : 0f);
                lb.transform.localPosition = MenuDraw.Local(pd, tx, row.CY - textPx * 0.5f,
                                                            tx + tw, row.CY + textPx * 0.5f);
                // 🔴 **2026-10-13（A435 辛 · A773，甲块漏的那一处）**：形参**原样**交给 `ClipText`
                //   （它自己从 `lb.transform` 沿父链解析）—— 改法与同文件 `ClippedText` 那一句**逐字相同**。
                //   原来那句 `if (st.Clip.HasValue)` 守卫在生产上**恒假**：`ItemDrawerStyle.Clip` 缺省 `null`
                //   （见 `Default`），三处派生写入（`CampaignRewardWindow` / `RewardWindow` / `CampaignTab`）
                //   已在甲块 B8/B9/B10 删掉 ⇒ **没有任何生产调用点会设它** ⇒ 这一段字**一个顶点都不裁**、
                //   压在视口边上整段画出去（**静默**）。同一函数里 `本文件` 的 `VisibleAbove`（粗筛）与
                //   `SetConverted` 里 `Already Owned` 那条（走 `ClippedText`）**都已改** ⇒ 只剩这一处。
                //   ⚠️ 受影响的是这两段字：`SetEphemeral` 的 `24 hours` 那串 · `SetConverted` 的数量数字。
                //   🔴 **可见影响面（哪一格会露出视口、露多少）没跑过 Unity 量过** ——
                //      判据/边界见 `资料/普查产出_1013/WA435辛_第7种载体收口.md` §二。
                MenuDraw.ClipText(lb, st.Clip, st.ClipSoftness);
            }
        }

        /// <summary>第 3 跳说明字那串（原版是 I2 词条 `MainMenu/RewardWindow/AlreadyOwned`）。
        /// ⚠️ **词条正文在远端 CCD、本地没有** ⇒ 我们写死 prefab 的 TMP 占位串（与 `RewardWindow.TxtPremium` 同一口径）。</summary>
        public const string AlreadyOwnedText = "Already Owned";

        /// <summary>`Highlight` 的图名（原版 sprite 名 `OctagonUI Filled Fade SDF`；工程里是 `ui_menu/` 那张同名下划线版）。
        /// ⚠️ 判据 = `bundle_menus_assets_all` 里 `Highlight` 的 `m_Sprite` PPtr 实解出的 `m_Name`。</summary>
        public const string ArtPremiumHighlight = "OctagonUI_Filled_Fade_SDF";
        /// <summary>`Badge` 的图名（原版 sprite 名逐字就是它）。</summary>
        public const string ArtPremiumBadge = "40k_campaign_Premium-icon";
        /// <summary>`Highlight` 的 `m_Color` 原文 `(1,1,1,0.8)`。</summary>
        public static readonly Color HighlightColor = new Color(1f, 1f, 1f, 0.8f);
        /// <summary>`Blackout` 的 `m_Color` 原文 `(0,0,0,0.5529412031173706)`。</summary>
        public static readonly Color BlackoutColor = new Color(0f, 0f, 0f, 0.5529412f);
        /// <summary>`Badge` 的锚跨 = `0.3 − 0.0`（横竖各 30%）。</summary>
        public const float BadgeFrac = 0.3f;

        /// <summary>第 2 跳那段时间串 —— **原版 `SupportMethods.GetTimeString` 那一条链**
        /// （`decomp_full/SupportMethods__GetTimeString.c` 170 行全文读；调用点的三个开关
        /// `eventTimer: true, clampTo0: false, hide0Values: false` 是 `SetEphemeralDisplay` 里**立即数**读到的）。
        /// <para>🔴 **2026-10-17（N4）**：格式化**已收口**到 `TimerDisplay.FormatTimeString`
        /// （全工程唯一一份实现）—— 本文件原来那第二份拷贝已删（铁律：两处写同一条规则 = 迟早不一致）。
        /// 分支与边界（`TotalDays &gt; 1` ⇒ `DH` · `TotalHours &gt; 1` ⇒ `H_Only` / `HM` · 否则 `MS`）
        /// 现在只在 `TimerDisplay.cs` 里维护。</para>
        /// <para>本方法只留**调用点自己的后处理**：`.Replace("0m","").Replace("0h","")`
        /// （两个实参是字符串字面量 `0m` / `0h` 与空串，按 `d:/2/tools/il2cpp_out/script.json` 的
        /// `ScriptString` 表逐地址解出：`0x42B8338 = "0h"` · `0x42B8438 = "0m"` · `0x42B80E0 = ""`），
        /// **顺序也照原版**。`clampTo0: true` 对应原来那句 `if (ms < 0) ms = 0;`
        /// （两端都来自本地数据，负数只会是脏数据）。</para>
        /// <remarks>**改坏法**：把 `"0h"` 那一句 `Replace` 删掉 ⇒ `2d 0h` 那种串会原样冒出来（自检那条红）。</remarks></summary>
        public static string EphemeralTextOf(long ms)
        {
            string t = TimerDisplay.FormatTimeString(System.TimeSpan.FromMilliseconds(ms),
                                                     eventTimer: true, clampTo0: true);
            return t.Replace("0m", string.Empty).Replace("0h", string.Empty);   // **两个 Replace 的顺序也照原版**
        }

        // ============================================================ ⑥ 第 4 跳：`!Options.Stackable` 时按数量展开

        /// <summary>`ItemDrawerOptions.stackable = 0` 的三个类型（**逐字**）。
        /// <para>🔴 **判据 = 整张 `ItemDrawerConfig` 的 `options` 表**（判据正本
        /// `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §② 那张 20 行的表，从 SO 原始字节解出）——
        /// **20 条里只有这 3 条是 `0`**，其余 17 条全是 `1`（`Currency` `PlayerAvatar`
        /// `Everguild.LiveOps.ShopContainer` `DropTableItem` `CosmeticItemCardback` `Wildcard` `CampaignPoints`
        /// `ForgePoints` `Everguild.LiveOps.ExpansionPassPoints` `PrebuiltDeck` `CosmeticItemTitle`
        /// `ExpansionPremiumItem` `AllianceTrophyData` `CosmeticItemAvatarBorder` `XSollaBundleItem`
        /// `AlternateArtCard` `GenericArmyItem`）。写正表还是写反表：**只列这 3 个**（其余一律「可堆叠」），
        /// 理由 = 表里 17:3，写反表能把「哪 3 个特殊」一眼看全；⚠️ 但**改这张表前必须先回那张 20 行的正表核**。</para></summary>
        static readonly string[] NonStackableTypes = { "RawCardScript", "PremiumItem", "VIPPremiumItem" };

        /// <summary>第 4 跳的判据：这一项的抽屉是不是**可堆叠**（= 原版 `drawer.Options.Stackable`，
        /// `ItemDrawerOptions + 0x10`，字段表 `dump.cs:93813` 实读）。
        /// <para>**不可堆叠 ⇒ 调用方要再画 `quantity − 1` 个抽屉、每个 `quantity = 1`**
        /// （`RewardWindow__Open.c:133-147`：`if (plVar8[5] == 0) throw;` —— `plVar8[5]` = `+0x28` = 那个
        /// `ItemDrawer.options`；`if ((*(char *)(options + 0x10) == '\0') &amp;&amp; (reward + 0x38 == 0))`
        /// ⇒ 循环上界 `reward.Quantity − 1`；循环体里 `ItemDrawer.Draw(parent, item, **1**, Default)` +
        /// **每个都照样 `TogglePremiumHighlight(tier == 10)`**、**但不再调第 2/3 跳**）。</para>
        /// <para>⚠️ **类型判据空时返回 `true` 并出声**：原版那种情况根本走不到这里（`GetDrawerConfig` 返回
        /// 空抽屉 ⇒ `Draw` 返回 null ⇒ 整格都不画），而我们**会**画一个兜底抽屉
        /// ⇒ 猜「不可堆叠」会把格子数乘上 quantity（错得更大），猜「可堆叠」只少画几个空格
        /// ⇒ 取后者 + 出声（⛔ 不静默）。</para>
        /// <remarks>**改坏法**：把 `"PremiumItem"` 从表里删掉 ⇒ 自检「`PremiumItem` ⇒ `IsStackable == false`」那条红；
        /// 把判据空那一支改成返回 `false` ⇒ 「类型判据空时出声且返回 true」那条红。</remarks></summary>
        public static bool IsStackable(ItemSpec item, DrawerOverride ov)
        {
            string type = ItemTypeOf(item);
            if (type != null)
            {
                ItemTypeSet e;
                string via;
                if (FindEntry(type, out e, out via)) return !IsNonStackable(e.Type);
            }
            Note("stack|" + (item.Id ?? "<空>"),
                 "物品 `" + (item.Id ?? "<空>") + "` 的**抽屉配置判据空**（推不出它的 `ObtainableItem` 子类型）"
                 + "⇒ 「可堆叠」这一格**拿不到原版值**，按**可堆叠**处理（= 不按数量展开成多格）并在此出声");
            return true;
        }

        /// <summary>该配置条目的 `options.stackable` 是不是 0（表 = <see cref="NonStackableTypes"/>）。</summary>
        static bool IsNonStackable(string type)
        {
            for (int i = 0; i < NonStackableTypes.Length; i++)
                if (NonStackableTypes[i] == type) return true;
            return false;
        }

        /// <summary>原版 `drawer.Options` 那句的等价物 —— 但**我们的 `ItemDrawerStyle` 不表达 `options`**
        /// （`QuantityPx`/`NamePx` 是调用方给的替身，见 `ItemDrawerStyle` 的注释）⇒ 展不展开**只**由
        /// <see cref="IsStackable"/> 决定。放在这里是为了让「第 4 跳」在库里有一个**可被自检直调**的落点。</summary>
        public static bool ExpandsByQuantity(ItemSpec item, DrawerOverride ov) { return !IsStackable(item, ov); }

        /// <summary>**照 `WildcardDrawer__Draw.c` 三层**：
        /// `background` = `wildcardBackgrounds[cardRarity−1]`（卡面底图）·
        /// `image` = `ArmyUtilities.GetArmyIcon(cardArmy)`（阵营徽记 —— 我们走 `DeckRuntime.FactionIcon`，
        /// 全工程唯一一份阵营图标映射，正本 §六 要求别抄第二份）·
        /// `armyText` = `GameStaticData.CardArmyToString(cardArmy)`。
        /// ⚠️ **三层的矩形都是我们挑的**（见 `Square` / `ArmyName` 的注释）。</summary>
        static void Wildcard(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            WildcardIcon(node, box, item, qty, st, ref res);       // 底图那一层（与 Icon 档同一张图）
            if (res.MissingArt) return;                            // 没图 ⇒ 已经画了占位板，别再叠两层

            // `image`：阵营徽记（图取不到就**少画一层 + 出声**，不退占位板 —— 底图那张还在）
            string fac = ArmyName(item.CardArmy);
            if (fac != null)
            {
                var fatex = Tex(DeckRuntime.FactionIcon(fac));
                if (fatex == null) res.MissingArt = true;
                else MenuDraw.Rect(node, fatex, Square(box, st.ArmyFill),
                                   NodeArmyIcon, st.QArt, null, true, st.Clip, st.ClipSoftness);
            }
            // `armyText`：原版 = `GameStaticData.CardArmyToString(cardArmy)`
            //   = `I2_Loc.GetTranslation("Armies/" + CardArmy枚举名)`（判据：`GameStaticData__CardArmyToString.c`
            //   现读 —— 前缀字面量 = `stringliteral.json` 的 `0x426cfa8 = "Armies/"`，`Concat(前缀, Enum.ToString)`）。
            //   🔴 **2026-10-27（B88-E8 · 收藏:269）就地订正（铁律 5）**：本行原来写
            //   「**词条表在远端 CCD、本地没有** ⇒ 我们画的是枚举名」—— **已过期**：官方 I2 表 2026-10-18 已并入
            //   （`Resources/official_loc_13lang.json` / `数据/游戏数据/官方本地化_en+zh-CN.json`），
            //   `Armies/*` 13 个阵营逐个现读都有（`Ultramarines→极限战士` / `Goff→欧克蛮人` …）
            //   ⇒ 现在走 `Loc.T`（≈ 原版 `GetTranslation`，按当前语言取）。⚠️ 字体随之换回 `Cjk`
            //   （`LatinCardBody` 渲不出汉字），这是「画对了原文」的必要一步。
            float k = box.H / DecorRefH;      // 原版字号三格的刻度（`box.H / 1080`）—— 阵营名与 `Name` 两条共用
            if (st.NamePx > 0f && fac != null)
            {
                // 🆕 **2026-10-10（A1212 块 11）接上自适应**：原版那一颗
                //   `… Drawer ▸ Content/Army` 四格 = `12 / 75 / 36 / 1`（折行 **1** ⇒ 不用还原）。
                //   出处 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §2·H #67
                //   （六份抽屉逐个现读，`Wildcard Drawer` 那份实测同值）；归组 =
                //   `资料/普查产出_第十一会话/RA1212_切块表.md` 块 11。
                //   ⚠️ **三格一起乘 `k`** —— 本文件 `ClippedText` 的 doc 写着「三者都是**原版设计空间的原文**，
                //   调用方自己按该族的**刻度**换算（见 `AlreadyOwned` 那一处）」，而同族已接的那一站
                //   （`本文件`）用的正是 `k = box.H / DecorRefH`（`DecorRefH = 1080` = 抽屉的参考高）
                //   ⇒ 照抄同族写法，⛔ 不自己发明系数。
                // 🆕 **2026-10-10（`A1242③`）：`wrapPx = box.W − 20f` 的【量纲】已查实 —— 它是「我们这一档的
                //   **框 px**」，⛔ 不是设计 px、**不需要乘 `k`**，本处与 `:1280` 用的是【同一把尺】**不存在混用**：
                //   · `wrapPx` 必须与本行的 `r` 同量纲 —— `ClippedText` 把 `r` 与 `wrapPx` **原样**交给
                //     `MenuDraw.Text`，而后者对两者都走 `LayoutSpace.Px(...)`（`MenuDraw.cs` 的 `MenuDraw.Text`）
                //     ⇒ `wrapPx` = 那个 TMP 矩形的**宽**，量纲只能是框 px。
                //   · 数值上也**正好**是矩形宽：`NameStrip(box, st.NamePx)` 的左右各内缩 `side = 10f`
                //     （`NameStrip` 里那两个是**未乘 `k` 的字面量**）⇒ 它的 `W` ≡ `box.W − 20f`；
                //     `本文件` 的 `Quantity` 更直接 —— 传的是 `qr.W` 本身（`qr` 同样 `side = 10f`）。
                //   · 乘 `k` 的只有 `12f/75f/36f` 这三个**原版 `m_fontSizeMin/Max/Base`**（设计 px ⇒ 框 px），
                //     而 `fontPx = st.NamePx` 本来就是框 px（`Label.SetAutoFitBox` 的 `minPx/maxPx/basePx`
                //     与调用方的 nominal 同量纲）⇒ 两边**同尺**。
                //   ⚠️ **另一个（真的）不对齐处，⛔ 本件没动**：`st.NamePx`（`Default()` 给 **26**）是**我们挑的
                //     旧值**，而原版那一颗 `Army Name` 是 `75 / base 36 / auto[12~75]`（设计帧 712×1080）
                //     ⇒ 折到 200×300 的格子应当是 `75k = 20.83` ⇒ 现在 `fontPx(26) > autoMaxPx(20.83)`，
                //     自适应会把它压到 20.83（画面大致对，但 nominal 是死数）。要改得连 `QuantityPx = 34`
                //     与那两个调用方（`CampaignRewardWindow` / `RewardWindow`）一起定，属另一笔账。
                ClippedText(node, NameStrip(box, st.NamePx), Loc.T("Armies/" + fac), Color.white, NodeArmyName,
                            st.NamePx, st.QText, box.W - 20f, st, 0, 12f * k, 75f * k, 36f * k);
            }
            // 🆕 **2026-10-27（B88-E8 · 收藏:269）：补回原版那格 `Name`（稀有度档名）**。
            //   判据三处：① `WildcardDrawer.GetItemName` = `string.Format(wildcardKey, CardRarityToString(rarity))`
            //   （`判据/ItemDrawer泛型族_判据.md` §二 #14 + `decomp_full/WildcardDrawer__GetItemName.c`）；
            //   ② `wildcardKey` = prefab 上那条 I2 字段，`mTerm = "MenuShop/RarityWildcard"`（字面 `{0} Wildcard` / `{0}通用牌`）；
            //   ③ `CardRarityToString` = `I2("Card_Rarity/" + 1..4→Common/Rare/Epic/Legendary · 其余→Special)`
            //   （`GameStaticData__CardRarityToString.c` + `stringliteral.json` 的 `0x423d4c8/0x42a75e8/0x429dd78/0x42a7e50/0x425a9b0`
            //   + 前缀 `0x42ce140 = "Card_Rarity/"`），→ prefab `Content/Label/Name` 那颗 TMP 的出厂文本正好是
            //   **`"Legendary Wildcard"`**（= 该式 rarity=4 的结果）⇒ 三条互证。
            //   ⚠️ **字号/对齐照 prefab**：`Name` 那颗 = `m_fontSizeMax 75 / base 36 / auto[12..75]`、
            //   `m_HorizontalAlignment = 1`（左）、`m_VerticalAlignment = 512`（中）。折行 = **1**（不还原）。
            //   🔶 自己设计（非原版）：**这一格的矩形**（`NameStripAt` 给的条位 = 阵营名那一条**下面**再起一条）
            //   —— 原版那颗由 `Content/Label` 的 `HorizontalLayoutGroup`（padding L/R 25 · spacing 10 · 两项各占一半）
            //   与 `Quantity` 排在**同一行**；我们的 `Quantity` 沿用战役奖励窗的底边位、没有那条 layout group
            //   ⇒ 只能另给一条；必须自己定：prefab 的 0×0 rect 是 VLG 跑之前的模板位、照抄会压在一起。
            //   执行代理 E8 2026-10-27 定（本批简报授权的范围内）。
            if (st.NamePx > 0f)
                ClippedText(node, NameStripAt(box, st.NamePx, 1),
                            WildcardName(item), Color.white, "Name",   // 节点名照 prefab 逐字（`Content/Label/Name` = 原版 `label` 那一格）；⛔ 别与 `NodeItemName`（我们起的名字）合并
                            st.NamePx, st.QText, box.W - 20f, st, 1, 12f * k, 75f * k, 36f * k);
            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item)) Quantity(node, box, qty, st);   // 2026-10-28（B89_E17c）：原版 `ItemDrawerOptions.stackable`（+ prefab 的 `quantity` 槽是否为 null）—— 判据/边界 → `Shell/ItemDrawerOptions.cs` 文件头。⚠️ 零行数变化（本行只换内容）
        }

        /// <summary>野牌那格 `Name` 的**文本**：原版 `WildcardDrawer.GetItemName(item)`
        /// = `string.Format(I2("MenuShop/RarityWildcard"), I2("Card_Rarity/" + CardRarityToString(rarity)))`。
        /// 判据逐条见 `Wildcard` 里那一段注释（三条互证）。
        /// ⚠️ 缺词条时 `Loc.T` 会退回键名并**出声**（不静默）；`string.Format` 对不含 `{0}` 的串是恒等 ⇒ 不会抛。</summary>
        static string WildcardName(ItemSpec item)
        {
            return string.Format(Loc.T("MenuShop/RarityWildcard"), Loc.T("Card_Rarity/" + RarityName(item.Rarity)));
        }

        /// <summary>`cardRarity` → 原版 `CardRarityToString` 里那个名字（`GameStaticData__CardRarityToString.c` 现读：
        /// `1→Common · 2→Rare · 3→Epic · 4→Legendary`，**其余一律 `Special`**）。
        /// ⚠️ 与本文件 `WildcardArtName` 的四档（`common/rare/epic/legendary`，**没有 special**）**不是同一张表**：
        /// 那张只服务图名，rarity 越界时它返 `null`（⇒ 取不到图、走兜底），这里照原版落 `Special`。</summary>
        static string RarityName(int rarity)
        {
            switch (rarity)
            {
                case 1: return "Common";
                case 2: return "Rare";
                case 3: return "Epic";
                case 4: return "Legendary";
                default: return "Special";
            }
        }

        /// <summary>**照 `WildcardIconDrawer__Draw.c`**：只写 `image` = `iconsByRarity[cardRarity−1]`
        /// （`Count &lt;= i` 时 `FirstOrDefault` = 那张图取不到就不设，我们照「取不到 ⇒ 不画」处理）。
        /// **无阵营、无名字、无数量** —— 原版这个方法就只碰一张图。</summary>
        static void WildcardIcon(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            var tex = WildcardTex(item, ref res);
            if (tex == null)
            {
                Placeholder(node, box, item, qty, st, ref res);
                res.MissingArt = true;
                return;
            }
            MenuDraw.Rect(node, tex, Square(box, st.IconFill), NodeIcon, st.QArt, null, true, st.Clip,
                          st.ClipSoftness);
        }

        /// <summary>通用图标抽屉（**我们建的**，见 `DrawerIcon` 的注释）：`item.Art` 那张图 + 数量。</summary>
        static void Icon(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            var tex = Tex(item.Art); if (CurrencyNames.Of(item.Id) != null) { DrawCurrency(node, box, item, qty, st, ref res); return; }   // 2026-10-28（B89_E17d）：货币转交 `CurrencyDrawer` 整件（主图+名条行+小图标+数量四件套）—— 判据/逐项几何 → `Shell/ItemDrawerCurrency.cs` 文件头。⚠️ 零行数变化（本行只换内容）
            if (tex == null)
            {
                Placeholder(node, box, item, qty, st, ref res);
                res.MissingArt = true;
                return;
            }
            res.Art = item.Art;
            MenuDraw.Rect(node, tex, Square(box, st.IconFill), NodeIcon, st.QArt, null, true, st.Clip,
                          st.ClipSoftness);
            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item)) Quantity(node, box, qty, st);   // 2026-10-28（B89_E17c）：原版 `ItemDrawerOptions.stackable`（+ prefab 的 `quantity` 槽是否为 null）—— 判据/边界 → `Shell/ItemDrawerOptions.cs` 文件头。⚠️ 零行数变化（本行只换内容）
        }

        /// <summary>占位板（**我们建的**）：中性底色 + 短名 + 数量，并把 `Placeholder` 记上 ——
        /// 调用方**必须**把 `Placeholder` 为真的那些 id **逐条出声**（`CLAUDE.md` §三：不许静默失败）。
        /// 原版没有这一档：它那边每个物品都有真抽屉（图在 prefab 里），我们这边**判据空**才落到这儿。</summary>
        static void Placeholder(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            res.Placeholder = true;
            var br = Square(box, st.IconFill);
            MenuDraw.Rect(node, CardArt.Solid(), br, NodePlaceholder, st.QBoard, BoardColor, false, st.Clip,
                          st.ClipSoftness);
            if (st.NamePx > 0f && !string.IsNullOrEmpty(item.Label))
                ClippedText(node, br, item.Label, Color.white, NodeItemName, st.NamePx, st.QText, br.W, st,
                            face: TmpFont.Face.LatinCardBody);
            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item)) Quantity(node, box, qty, st);   // 2026-10-28（B89_E17c）：原版 `ItemDrawerOptions.stackable`（+ prefab 的 `quantity` 槽是否为 null）—— 判据/边界 → `Shell/ItemDrawerOptions.cs` 文件头。⚠️ 零行数变化（本行只换内容）
        }

        // ============================================================ 小工具（版式全是我们挑的）

        /// <summary>居中方块。**我们挑的**：`IconFill = 0.7` 只有一条依据 —— 战役奖励窗原来那个
        /// `ItemIconPx = 140` 正好是格子（200×300）**短边的 0.7**（既有的量渲染断言按 140 写；
        /// 那个常量是**唯一出处**，调用方现算 `IconFill` 传进来）。
        /// 🔴 **必须跟着框的中心**（`box.CX/CY`）—— 2026-09-23 那一版把它贴到格子上边缘（`r.y1 + 40`），
        /// 而并排的 `Unlock Button` 是垂直居中的 ⇒ 两者中心差 **40px**、实拍才看出来，**当时所有断言全绿**。</summary>
        static PxRect Square(PxRect box, float k)
        {
            float s = Mathf.Min(box.W, box.H) * k;
            return new PxRect(box.CX - s * 0.5f, box.CY - s * 0.5f, box.CX + s * 0.5f, box.CY + s * 0.5f);
        }

        /// <summary>野牌阵营名那一条（在抽屉框**顶边**的横条里）。**我们挑的** ——
        /// 放顶部是因为底边已被数量占着（`Quantity` 沿用战役奖励窗原来的位置）。</summary>
        static PxRect NameStrip(PxRect box, float px)
        {
            return NameStripAt(box, px, 0);
        }

        /// <summary>同 <see cref="NameStrip"/>，但可以落在**第 `row` 条**（0 = 顶边第一条）。
        /// 🔶 自己设计（非原版）：**行距 `2px` 与「往下堆」这个排法是我们的** —— 原版 `Content/Label`
        /// 用一个 `HorizontalLayoutGroup`（padding `L/R 25` · spacing `10` · `forceExpandWidth=1`）
        /// 把 `Name` 与 `Quantity` 摊在**同一行**；我们的 `Quantity` 走战役奖励窗的底边位（自己的版式），
        /// ⇒ 只能另起一条。必须自己定：prefab 里 `Name`/`Quantity` 的 rect 是 `0×0`（VLG 跑之前的模板位），
        /// 照抄会把两段字压在同一个点上。执行代理 E8 2026-10-27 定（本批简报授权的范围内）。</summary>
        static PxRect NameStripAt(PxRect box, float px, int row)
        {
            const float side = 10f, top = 10f, gap = 2f;
            float band = px * 1.4f;
            float y1 = box.y1 + top + row * (band + gap);
            return new PxRect(box.x1 + side, y1, box.x2 - side, y1 + band);
        }

        /// <summary>数量：钉在抽屉框**底边**上、右对齐（值沿用战役奖励窗原来那一套）。
        /// 原版这一格是 `ItemDrawerComponents.Quantity`。🔴 **2026-10-04 订正（铁律 5）**：原来补的
        /// 「**它到底怎么排版在 prefab 里、本地读不到**」是**假的** —— 那些抽屉 prefab dump 得出来（文件头 ②）；
        /// 排版值**我们还没照它改**（待做的活）⇒ 这一套位置**仍是我们的**。</summary>
        static void Quantity(Transform node, PxRect box, int qty, ItemDrawerStyle st)
        {
            const float side = 10f, top = 60f, bottom = 15f;
            var qr = new PxRect(box.x1 + side, box.y2 - top, box.x2 - side, box.y2 - bottom);
            // 🔴 **2026-10-11（A302）**：原来这里是「`MenuDraw.Text` 建完再 `MenuDraw.AlignRight`」——
            //    顺序反了：`ClipText` 夹的是**世界坐标**的顶点，先裁再挪 = 把裁好的块挪出框
            //    （同 `CampaignRewardWindow.cs` 那颗 `Warning` 踩过的那次）。⇒ 改走 `ClippedText(align: 2)`，
            //    **对齐在裁之前**（这是它签名的第 3 步）。
            // 🆕 **2026-10-10（A1212 块 11）接上自适应**：原版那一颗
            //   `… Drawer ▸ Content/Label/Quantity` 四格 = `12 / 75 / 36 / 0`，出处 =
            //   `R6_包装层战斗与其余.md` §2·H #69（六份抽屉现读；⚠️ 原报告同时记着
            //   `Currency Drawer` 是 `auto[12~100]`、`Title Drawer` 是 `fs40` —— 那是**别的抽屉**，
            //   本函数只有这一个调用点、按**多数那一档**接，差额如实记在报告里）。
            //   ⚠️ 折行 = **0** ⇒ `wrapOff: true`（`SetAutoFitBox` 内部那句 `SetWrapWidth` 会无条件开折行，
            //   而这句还原在 `ClippedText` 体内、`ClipText` **之前**做 —— 调用方拿到返回值再关就晚了，
            //   见那个形参自己的 doc）。⚠️ 三格乘 `k` 的理由同 `Army Name` 那一处。
            float k = box.H / DecorRefH;
            // 🔴 **2026-10-27（B88-E8 · 收藏:265）去掉 `x` 前缀**：原版基类 `Draw` 第 ③ 步是
            //   `label.setText(amount.ToString())`（**裸数字**）—— 判据两处互证：
            //   ① Ghidra 泛型产物 `decomp_generic_trial/out/ItemDrawer_object___Draw__1ae9cc0.c`（155 行 · 0 NoteDecompilerIssue）；
            //   ② `Wildcard Drawer.prefab` 的 `Content/Label/Quantity` 那颗 TMP 的 `m_text` 出厂值 = **`"2"`**（没有 `x`）。
            //   ⚠️ `"x"+n` 是**别的**组件（`UICardInfoItem.SetCount` / 卡组列表份数 / 骷髅数）的写法，⛔ 别往抽屉上套。
            ClippedText(node, qr, qty.ToString(), Color.white, NodeQuantity, st.QuantityPx, st.QText, qr.W, st, 2,
                        12f * k, 75f * k, 36f * k, true, TmpFont.Face.LatinCardBody);
        }

        /// <summary>抽屉里一段**吃裁切**的文字（🆕 **2026-10-11（A302）**）—— 全库唯一一份。
        /// 四步顺序与同族 `GameWindow.Text`（`Shell/WindowsManager.cs`，`GameWindow` 直系窗口那一支）
        /// **逐字相同**：① 整块在视口外 ⇒ **不建**（`MenuDraw.Visible` —— 与 `MenuDraw.Rect` 那条 `ClipRect`
        /// 是同一份判据）；② `MenuDraw.Text`；③ **对齐**（⛔ 必须在裁之前，见 `Quantity` 那条注释）；
        /// ④ `MenuDraw.ClipText(lb, clip, soft)` —— 逐字夹顶点 + 按同一剖面削 alpha。
        /// <para>🔴 **为什么要有它**：本库原来**只给图吃裁切**（四条画路各传 `st.Clip` / `st.ClipSoftness`），
        /// 三层文字（阵营名 / 短名 / 数量）走的是裸 `MenuDraw.Text` ⇒ **图被裁、字照画**
        /// —— A182 那段「抽屉里那四层凡是整块落在视口外的不建、压在边上的**截**」**对文字不成立**
        /// （戊1 已在 `Shell/CampaignRewardWindow.cs` 的 `BuildItem` 注释里就地写明）。
        /// 判据 = 原版 `RectMask2D` 对**文字与图一视同仁**（掩码在 shader 里按像素裁，不分是 quad 还是字形）。</para>
        /// <para>⚠️ **不给 `Clip` 的调用方一个字节都不动**：`Clip == null` ⇒ 只走 ②（`Visible` 恒真、
        /// 不调 `ClipText`）——商店格 / 战役节点那几处就是这一档。</para>
        /// 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：两处守卫改成【沿父链解析】**（`VisibleAbove` /
        /// 交给 `ClipText` 自己解析）—— 判据 = `ViewportClip.Resolve` 的三段优先级：
        /// **显式形参非空 ⇒ 形参赢**（`st.Clip` 现在是纯「显式覆盖」的口子，生产代码一个都不传）；
        /// 否则**沿父链找最近的 `ViewportClip` 节点**（抽屉挂在滚动区的 `Viewport` 之下 ⇒ 命中）。
        /// ⇒ 调用方**不必再派生**状态（`CampaignRewardWindow` / `RewardWindow` / `CampaignTab` 那三处
        /// 派生写入已删，见各自注释），本库自己取 = 从前的 `st.Clip` 那一档逐位等价。
        /// <param name="align">0 = 居中（`Label` 默认）· 1 = 左 · 2 = 右（原版 TMP 的 `m_HorizontalAlignment`）。
        /// 与 `GameWindow.Text` 的同名形参同义。</param></summary>
        /// <param name="autoMinPx">🔴 **2026-10-19（A1187）新加**：原版那一颗的 `m_fontSizeMin`（**画布 px**）。
        /// **`0` = 这一档不接自适应**（= 旧行为，逐位不变）。要过 `MenuDraw.TextCore` 那道闸，
        /// `wrapPx &gt; 0 ∧ autoMinPx &gt; 0 ∧ fontPx &gt; autoMinPx` **三条得全真** ⇒ 只传这个 = **死实参**。</param>
        /// <param name="autoMaxPx">原版那一颗的 `m_fontSizeMax`（画布 px；`≤ 0` ⇒ 上限退回 `fontPx`）。</param>
        /// <param name="autoBasePx">原版那一颗的 `m_fontSizeBase`（画布 px；`≤ 0` ⇒ base 退回调用方那一档）。
        /// ⚠️ 三者都是**原版设计空间的原文**，调用方自己按该族的刻度换算（见下面 `AlreadyOwned` 那一处）。</param>
        /// <param name="wrapOff">🔴 原版那一颗 `m_TextWrappingMode = 0`（不折行）时传 `true` ——
        /// `SetAutoFitBox` 内部那句 `SetWrapWidth` 会**无条件**把模式开成 `Normal(1)`，
        /// ⇒「关折行」要做在 `ClipText` **之前**（在这里做最省事、一次做对）。
        /// 🔴 **2026-10-17（`A1228`）就地订正（铁律 5）**：本行原来写「**调用方拿到返回值再关就晚了**」
        /// —— **不成立**：`SetWrappingMode → ForceRelayout → RefreshBounds`，而 `RefreshBounds` 的末句
        /// 就是 `ReclipNow()`（`Battle/Label.cs` 的 `ReclipNow()`）⇒ **调用方那条路也会重裁**、结果一样。
        /// （判据 = 本仓现读；⛔ 别据此再给调用方加顺序限制。）</param>
        static Label ClippedText(Transform node, PxRect r, string s, Color color, string name,
                                 float fontPx, int q, float wrapPx, ItemDrawerStyle st, int align = 0,
                                 float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f,
                                 bool wrapOff = false, TmpFont.Face face = TmpFont.Face.Cjk)
        {
            // 🔴 2026-10-13（A435）：`Visible` → **`VisibleAbove`**（纯矩形函数手上没有 `Transform`，
            //   解析不了节点 ⇒ 必须走带 `parent` 的那个重载，判据 → `MenuDraw.VisibleAbove` 的注释）。
            if (!MenuDraw.VisibleAbove(node, r, st.Clip)) return null;
            var lb = MenuDraw.Text(node, r, s, color, name, fontPx, q, wrapPx,
                                   autoMinPx, autoMaxPx, autoBasePx, face: face);
            if (lb == null) return null;
            // ⓪ 原版 `折行=0` 的那几颗：`SetAutoFitBox` 刚把模式开成 `Normal(1)` ⇒ 在这里还原
            //    （⚠️ 必须在 ①② 之前，且必须在 `ClipText` 之前 —— `SetWrapping` 自己会 `ForceRelayout` 推版面）
            if (wrapOff) lb.SetWrapping(false);
            if (align == 1) MenuDraw.AlignLeft(lb, r);
            else if (align == 2) MenuDraw.AlignRight(lb, r);
            // 🔴 2026-10-13（A435）：形参**原样**交给 `ClipText`（它自己会从 `lb.transform` 解析）——
            //   原来那句 `if (st.Clip.HasValue)` 守卫在节点态下**恒假** ⇒ 整段字不吃裁切（静默）。
            MenuDraw.ClipText(lb, st.Clip, st.ClipSoftness);
            return lb;
        }

        /// <summary>只查不画：这个物品在本地**画得出来吗**（判据空 / 图取不到 ⇒ `false`）。
        /// 给「先普查一遍再出声」用 —— 节点那 47 条要在**没建视图**的时候也能数出来
        /// （`CampaignTab.AuditNodeRewards`，这样数字与视口滚到哪无关）。</summary>
        public static bool HasArt(ItemSpec item, DrawerOverride drawerOverride)
        {
            string d = PickDrawer(item, drawerOverride);
            if (d == null || d == DrawerPlaceholder) return false;
            if (d == DrawerWildcard || d == DrawerWildcardIcon) return WildcardTexName(item) != null;
            return Tex(item.Art) != null;                     // `IconDrawer`：就吃数据层那张图
        }

        /// <summary>野牌那张图。**照 §四**：`iconsByRarity` / `wildcardBackgrounds` = **同一组 4 张**
        /// `40k_general_wildcard_{common,rare,epic,legendary}`（**无 `_small` 后缀**），顺序 = `cardRarity − 1`。
        /// ✅ **2026-10-04 已导进工程**（原来的缺口：那 4 张**只在 `Art/原版/0_mainmenu/` 里、不在 `Resources/` 下**，
        /// 运行时 `CardArt.MenuUi` 取不到，只能退到 `_small`）——
        /// 现在 `Resources/Art/ui_deck/` 里已有这 4 张（**328×497 的平铺卡面**）+ 各自的 `.meta`，
        /// 而 `CardArt.MenuUi` 走 `ui_menu/ → ui_deck/ → ui/` 三级兜底 ⇒ **判据名直接取得到、不再退档**。
        /// 🔑 **重建路 = `工具/sync_battle_ui_art.py` 的 `NAMES_DECK`**（⚠️ **不是** `import_original_art.py` —— 原记录写错了；
        /// 且 `Resources/Art/` 整棵在 `.gitignore:75` 里，新克隆必须跑那个脚本才有图）。
        /// 下面这条「取不到退 `_small` 并记 `FallbackArt`」的兜底**保留**（图上真缺时仍要出声，不静默）。</summary>
        static string WildcardTexName(ItemSpec item)
        {
            string main = WildcardArtName(item.Rarity);
            if (Tex(main) != null) return main;
            if (main != null && Tex(main + "_small") != null) return main + "_small";
            return Tex(item.Art) != null ? item.Art : null;    // 数据层那张表（今天与 `_small` 同一个名字）
        }

        static Texture2D WildcardTex(ItemSpec item, ref ItemDrawResult res)
        {
            string name = WildcardTexName(item);
            if (name == null) { res.FallbackArt = true; return null; }   // 判据那张与退档都取不到
            res.Art = name;
            res.FallbackArt = name != WildcardArtName(item.Rarity);
            return Tex(name);
        }

        /// <summary>稀有度 → 图名。`cardRarity 1..4` ⇒ `common/rare/epic/legendary`（§四 实读的顺序）。</summary>
        public static string WildcardArtName(int rarity)
        {
            if (rarity < 1 || rarity > WildcardRarity.Length) return null;
            return "40k_general_wildcard_" + WildcardRarity[rarity - 1];
        }
        static readonly string[] WildcardRarity = { "common", "rare", "epic", "legendary" };

        /// <summary>`CardArmy` 枚举 → **枚举名**。**判据** = `d:/2/tools/il2cpp_out/dump.cs:45637-45650` 的枚举原文。
        /// ⚠️ 原版 `GameStaticData.CardArmyToString` 后面还要过一道 I2 本地化（见 `Wildcard` 里那条注释）
        /// ⇒ 我们画的就是**枚举名**（`Ultramarines` 这种），**不是**原版屏幕上那串。
        /// 返回 `null` = 这一档没有阵营（`Neutral`）⇒ 徽记与名字两层都不画（**不拿默认图顶**）。</summary>
        public static string ArmyName(int cardArmy)
        {
            for (int i = 1; i < ArmyValues.Length; i++)        // 跳过 Neutral
                if (ArmyValues[i] == cardArmy) return ArmyNames[i];
            return null;
        }

        /// <summary>13 个阵营。**顺序与值照 `CardArmy` 枚举**（`dump.cs:45637-45650`）；
        /// 名字与 `DeckRuntime.FactionIcon` / `ForgeData.Armies` 逐字相同（那是全工程唯一一份阵营名表）。</summary>
        static readonly string[] ArmyNames =
        {
            "Neutral", "Ultramarines", "Goff", "SaimHann", "Sautekh", "BlackLegion", "Leviathan",
            "TauEmpire", "Sororitas", "Genestealers", "AstraMilitarum", "DarkAngels", "EmperorsChildren",
            "SpaceWolves",
        };
        static readonly int[] ArmyValues =
        {
            0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130,
        };

        /// <summary>取图（`CardArt.MenuUi` 自己走 `ui_menu/ → ui_deck/ → ui/` 三级兜底）。</summary>
        static Texture2D Tex(string name)
        {
            return string.IsNullOrEmpty(name) ? null : CardArt.MenuUi(name);
        }

        // ============================================================ ⑦ 抽屉【作为可点格子】的生命周期（🆕 2026-10-25 · 代理 H）
        //
        //  判据（第一权威 = 反编译 `ItemDrawer.cs` / `ItemDrawer<T>`）：
        //    · `ToggleClaimedWarning(bool)`（:790）＝ `claimedWarning`（**+0x50**）那颗件的**显示态**
        //    · `TogglePremiumHighlight(bool)`（:846）＝ `premiumHighlight`（+0x40）`SetActive(toggle)`
        //    · `ToggleHighlight(bool)`（:907）＝ **转调 `TogglePremiumHighlight`**（同一个落点，不是第二套高亮）
        //    · `ToggleLock(bool)`（:912）＝ **转调 `ToggleClaimedWarning`**（同一个落点）
        //    · `OverridePremiumBadge(Sprite)`（:808）＝ `premiumBadge.sprite = sprite`（**非空才改**）
        //    · `ToggleShowInfoOnClick(bool)`（:876）＝ **写那道点击闸** `this+0x44 = option`（🔴 2026-10-29 R-K 用 VA + 374 份 prefab 全量复扫证明：ILSpy 把它读成 `{ }` = **丢赋值**，同族坑；`OnPointerClick.c:13` 读的正是这一格）
        //    · `OverrideOnClickAction(Action)`（:880）＝ 存进 `+0x50` 那一格，点击时优先于默认
        //    · `OnPointerClick(eventData)`（:917）＝
        //        item 为空 ⇒ `LogError("[ERROR] We are trying to open a item display window without an item.
        //                            Mat check this out, Probably we are abusing the use of ItemDrawer")`
        //        否则 **有 `onClick` 就跑它、没有就 `HandlePointerClick()`**；末尾放一次 `inspectSound`
        //    · `HandlePointerClick()`（:955）＝ 开 `ItemDisplayWindow`（`ItemDisplayWindowContext(item, 0)`）
        //
        //  🔴 **`claimedWarning` 那件在原版 prefab 里是【空节点】**：亲扫 `bundle_menus_assets_all` 的
        //     **80 份 `GameObject/Collected Badge*.json` = 80/80 只有 `RectTransform + CanvasRenderer`**
        //     （没有 Image、没有 TMP、没有子件）⇒ 它的「警告」视觉**不在这个节点上**。
        //     ⇒ 我们照这个事实：节点**照判据几何建**（`aMin(0.1,0)` / `aMax(0.9,0.1)` / `ap(0,20)` /
        //     `pivot(0.5,0)`，出厂隐藏），**不贴任何图**（⛔ 别自己画一条「已领取」缎带冒充原版）。
        //     可观测的那一半（显示态）照样有，自检读 `ClaimedVisible`。

        /// <summary>取一张装饰图给 `ItemDrawerView.OverridePremiumBadge` 用（原版那个口收 `Sprite`，我们按图名收）。</summary>
        public static Texture2D ArtTexture(string artName) { return Tex(artName); }

        /// <summary>`Draw` 的**句柄版**：返回 <see cref="ItemDrawerView"/>（画不出来时 `null`）。
        /// <para>⚠️ 与 <see cref="Draw"/> 同一条实现（它内部就是调 `Draw`）⇒ ⛔ 别在别处再画一份。</para></summary>
        public static ItemDrawerView DrawView(Transform parent, PxRect box, ItemSpec item, int quantity,
                                              DrawerOverride drawerOverride, ItemDrawerStyle style)
        {
            var r = Draw(parent, box, item, quantity, drawerOverride, style);
            if (r.Node == null) return null;
            var v = r.View != null ? r.View : new ItemDrawerView(r, box, style);
            v.RememberFace(item.Label, item.Art);      // 句柄自己留一份（`ItemSpec` 出了这句就没了）
            return v;
        }
    }

    /// <summary>
    /// 一个画出来的抽屉的**可变句柄**（原版 `ItemDrawer&lt;T&gt;` 上那些「画完之后再调」的方法的等价物）。
    /// 判据与「哪些是空方法」见 `ItemDrawer` 文件里 §⑦ 那一段。
    /// </summary>
    public sealed partial class ItemDrawerView
    {
        public Transform Node { get; private set; }
        public string Id { get; private set; }
        public ItemKind Kind { get; private set; }
        public string Drawer { get; private set; }

        readonly PxRect _box;
        readonly ItemDrawerStyle _style;
        Action _onClick;

        /// <summary>原版 `claimedWarning` 那件（我们的「Collected Badge」节点）。见 `ItemDrawer` §⑦。
        /// ⚠️ **懒建**（原版 prefab 里出厂就有；我们第一次 `ToggleClaimedWarning(true)` 才建）——
        /// 观测行为相同（出厂 = 隐藏 = 不存在），但**少给每一格加一个空节点**。</summary>
        Transform _claimed;

        internal ItemDrawerView(ItemDrawResult r, PxRect box, ItemDrawerStyle style)
        {
            Node = r.Node; Id = r.Id; Kind = r.Kind; Drawer = r.Drawer;
            _box = box; _style = style;
        }

        /// <summary>「已领取」警示件现在显示着吗（原版 `claimedWarning.activeSelf`）。</summary>
        public bool ClaimedVisible { get { return _claimed != null && _claimed.gameObject.activeSelf; } }
        /// <summary>premium 高亮组现在显示着吗（原版 `premiumHighlight.activeSelf`）。</summary>
        public bool PremiumHighlightVisible
        {
            get
            {
                var t = Node != null ? Node.Find(ItemDrawer.NodePremiumHighlight) : null;
                return t != null && t.gameObject.activeSelf;
            }
        }

        /// <summary>原版 `ToggleClaimedWarning(bool)`（+0x50）。</summary>
        public void ToggleClaimedWarning(bool toggle)
        {
            if (Node == null) return;
            if (_claimed == null && toggle)
            {
                // 几何逐字段照原版 `GameObject/Collected Badge.json` 的 RectTransform：
                //   aMin (0.1, 0) · aMax (0.9, 0.1) · ap (0, 20) · pivot (0.5, 0) · sd (0,0)
                // ⇒ 抽屉**下沿内侧**一条 80% 宽、10% 高的带子，底边离抽屉底 20px。
                // ⚠️ 本壳**没有 uGUI 运行时**（`MenuDraw.Node` 的注释：锚点一律写死成重合、矩形只由
                //    `sizeDelta` + 中心位置决定）⇒ 这里把上面那四个字段**换算成矩形**记下来，⛔ 不写 anchor
                //    （写了也不生效，反而会让「矩形 = 这四个数」这条判据变含糊）。
                float w = _box.W, h = _box.H;
                float x1 = _box.x1 + 0.1f * w, x2 = _box.x1 + 0.9f * w;
                float yBottom = _box.y2 - 20f;            // y 向下：底边 = y2 − ap.y
                float yTop = yBottom - 0.1f * h;          // 高 = 10% × 抽屉高
                _claimed = MenuDraw.Node(Node, "Collected Badge", new PxRect(x1, yTop, x2, yBottom));
            }
            if (_claimed != null) _claimed.gameObject.SetActive(toggle);
        }

        /// <summary>原版 `TogglePremiumHighlight(bool)`（+0x40，`SetActive`）。</summary>
        public void TogglePremiumHighlight(bool toggle)
        {
            var t = Node != null ? Node.Find(ItemDrawer.NodePremiumHighlight) : null;
            if (t != null) t.gameObject.SetActive(toggle);
            else if (toggle)
                Debug.LogWarning("[ItemDrawer] 这一格没有 `" + ItemDrawer.NodePremiumHighlight + "` 节点"
                               + "（画的时候 `style.Premium == false`）⇒ `TogglePremiumHighlight(true)` 落空（出声，不静默）");
        }

        /// <summary>原版 `ToggleHighlight(bool)` = **转调** `TogglePremiumHighlight`（同落点）。</summary>
        public void ToggleHighlight(bool toggle) { TogglePremiumHighlight(toggle); }

        /// <summary>原版 `ToggleLock(bool)` = **转调** `ToggleClaimedWarning`（同落点）。</summary>
        public void ToggleLock(bool toggle) { ToggleClaimedWarning(toggle); }

        /// <summary>原版 `OverridePremiumBadge(Sprite)`：只换角标那张图（**空的图不换**，
        /// 原版 `if ((bool)sprite)` 闸）。我们按图名收（`ItemDrawer.ArtTexture`）。</summary>
        public void OverridePremiumBadge(string artName)
        {
            var tex = ItemDrawer.ArtTexture(artName);
            if (tex == null) return;                       // 原版空 sprite ⇒ 不改
            var root = Node != null ? Node.Find(ItemDrawer.NodePremiumHighlight) : null;
            var badge = root != null ? root.Find(ItemDrawer.NodePremiumBadge) : null;
            if (badge == null)
            {
                Debug.LogWarning("[ItemDrawer] `OverridePremiumBadge(\"" + artName + "\")` 落空：这一格没有 `"
                               + ItemDrawer.NodePremiumBadge + "` 角标节点（出声，不静默）");
                return;
            }
            var q = badge.GetComponent<ImageQuad>();
            if (q != null) q.SetTexture(tex);
        }

        /// <summary>原版 `ToggleShowInfoOnClick(bool option)` = **写那道点击闸**（`this+0x44 = option`）—— 🔴 2026-10-29 `R-K` 用 VA + 374 份 prefab 全量复扫证明 **ILSpy 把它读成 `{ }` 是丢了赋值**（`OnPointerClick.c:13` 读的正是这一格；374 份里 `0=296 / 1=78`）。转调 <see cref="SetShowInfoOnClick"/>（同一个闸 + 同步命中区）。</summary>
        public void ToggleShowInfoOnClick(bool option) { SetShowInfoOnClick(option); }

        /// <summary>原版 `OverrideOnClickAction(Action)`：覆写点击回调（优先于默认的「开物品详情窗」）。</summary>
        public void OverrideOnClickAction(Action onClick) { _onClick = onClick; }

        /// <summary>原版 `OnPointerClick(PointerEventData)`：item 为空 ⇒ LogError；否则
        /// **有 `onClick` 就跑它、没有就 `HandlePointerClick()`**。</summary>
        public void OnPointerClick()
        {
            if (!ShowInfoOnClick) return; if (string.IsNullOrEmpty(Id))
            {
                Debug.LogError("[ERROR] We are trying to open a item display window without an item. "
                             + "Mat check this out, Probably we are abusing the use of ItemDrawer");
                return;
            }
            if (_onClick != null) { try { _onClick(); } catch (Exception e) { Debug.LogError("[ItemDrawer] onClick 抛异常：" + e); } PlayInspectSound(); return; }
            HandlePointerClick(); PlayInspectSound();
        }

        /// <summary>原版 `HandlePointerClick()`：开物品详情窗（`ItemDisplayWindowContext(item, 0)`）。</summary>
        public void HandlePointerClick()
        {
            if (DeckClick()) return; if (string.IsNullOrEmpty(Id))
            {
                Debug.LogError("[ERROR] We are trying to open a item display window without an item. "
                             + "Mat check this out, Probably we are abusing the use of ItemDrawer");
                return;
            }
            var mgr = WindowsManager.Get();
            var ctx = new ItemDisplayWindow.Context
            {
                Title = !string.IsNullOrEmpty(_styleLabel) ? _styleLabel : Id,
                Description = null,
                Type = null,
                Army = null,
                Lore = null,
                Art = ItemDrawer.ArtTexture(_styleArt),
                ArtName = _styleArt,
            };
            ItemDisplayWindow.Create(mgr, ctx);
        }

        // 画的时候用的「名字 / 图名」——`ItemSpec` 在 `Draw` 之后就没了，句柄要自己留一份。
        string _styleLabel, _styleArt;
        internal void RememberFace(string label, string art) { _styleLabel = label; _styleArt = art; }
    }

    // ============================================================ 追加区 · 2026-10-26（批次 3 · Q1）
    //
    // 【这一段补的是什么】账 `账/卡组编辑.tsv` 那 5 条 C（`DeckDrawer` 的 4 个方法 + `DeckAndCardbackDrawer.Draw`）：
    //   原版 `DeckDrawer : ItemDrawer<PrebuiltDeck>` 与 `DeckAndCardbackDrawer : DeckDrawer` 这两个抽屉我们**没有**
    //   ⇒ 「奖励/商店里发一整副卡组」那一格今天落到 `default: Placeholder`（画成占位板 + 出声）。
    //
    // 【判据】（逐条都是从 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` 的**真方法体**读的，
    //         并用 `d:/2/tools/decomp_full/DeckDrawer__*.c` 的 Ghidra 版交叉核对）
    //   · `DeckDrawer.cs:35-55` `Draw`：① `armyIcon.sprite = ArmyUtilities.GetArmyIcon(item.DeckHero.cardArmy)`
    //     ② `base.image.sprite = item.DeckHero.cardImageReference.Load()` ③ `gameModeIcon.sprite =
    //     ScriptableObjectsCollectionsUtilities.GetGameModeIcon(item.gameMode)` ④ `gameModeIcon.enabled = 图标非空`
    //   · `DeckDrawer.cs:57-73` `GetItemName`：词条 **`"PrebuiltDecks/" + item.deckId`**（不是 SO 名，也不是 deckName）
    //   · `DeckDrawer.cs:17-28` `HandlePointerClick`：`new CardDeck(item)` → `DeckInfoContext(deck, View, selectButton=null,
    //     shouldCheckOwnership, editButtonAction=null)` → `WindowsManager.OpenWindow<DeckInfoPopup>(ctx)`
    //   · `DeckDrawer.cs:30-33` `SetShowOwnership(bool)`：只写 `shouldCheckOwnership`（**反编译全库 0 调用点**）
    //   · `DeckAndCardbackDrawer.cs:7-17` `Draw`：`base.Draw(item,q)`；**若 `cardBackDrawer` 非空**再 `cardBackDrawer.Draw(...)`
    //     + `cardBackDrawer.gameObject.SetActive(true)`
    //
    // 【几何】= `Deck Drawer` prefab（`bundle_menus_assets_all`，GameObject `Deck Drawer_-3438027711880944002`）
    //   逐节点的 `RectTransform.m_AnchorMin/m_AnchorMax` 现读（2026-10-26 侦察代理现核）：
    //     `Deck Image(mask)` aMin(.044,.180) aMax(.956,.93547) · `Frame` aMin(0,.04935) aMax(1,.958)
    //     · `Deck Name` aMin(.08,.052) aMax(.92,.15)（TMP fs **32**，白）
    //     · `Faction Icon` aMin(-.042,.11253) aMax(.296,.32425) · `Game Mode Icon` aMin(.682,.11253) aMax(1.02,.32425)
    //     · `Deck Image`（mask 的子件）aMin(-.265,-.067) aMax(1.26,1.065) —— 那是**过扫**给 `RectMask2D` 裁的；
    //       我们这一层没有 mask ⇒ 直接画在 **mask 那个矩形**里（可见范围相同，见 `Deck` 的注释）
    //     · 参考高度 **496.97** = prefab 里 `Collection Deck` 那一格的高度（字号按 `box.H / 496.97` 线性换算）
    //   ⚠️ **这套几何没有做视觉验收**：当前 `数据/游戏数据/*.json` 里 **0 条 `PrebuiltDeck` 发放物**
    //      ⇒ 没有可比对的实例（报告里已如实记）。节点矩形本身是资产现读，不是猜的。
    //
    // 【⛔ 不画的两件，各有出处】
    //   · `Highlight`（`Highlight Rounded Square`，金）：原版那是**选中高亮**、由 `CollectionDeck.SetSortMode`/`selected`
    //     控制，抽屉里出厂不显示 ⇒ 不建。
    //   · `Ban Icon` / `Converted Drawer` / `Ephemeral Drawer` / `Collected Badge` / `Premium Highlight`：
    //     原版是**调用方**按奖励状态三跳（`TogglePremiumHighlight` / `SetEphemeralDisplay` / `SetConvertedItem`）
    //     才开的那几层，本抽屉不负责（本库同一个口径，见 `ItemDrawerStyle.Premium/Ephemeral/Converted`）。
    /// <summary>`ItemSpec` 的 partial 第二部分：**一副卡组**这条数据（原版 `DeckDrawer.Draw(PrebuiltDeck item,…)` 读的那些位）。
    /// <para>为什么追加在这里而不就近加进结构体：`账/` 有 10 条 A 行的落点钉在 `Shell/ItemDrawer.cs:427…1605`
    /// ⇒ 往中间插字段会让它们整体漂移。⛔ 别把这条理由删掉。</para></summary>
    public partial struct ItemSpec
    {
        /// <summary>整副预组卡组（`DeckDrawer` / `DeckAndCardbackDrawer` 专用）。`null` = 不是卡组类物品。</summary>
        public PrebuiltDecks.Deck Deck;
        /// <summary>画的时候要不要叠卡背（原版 `DeckAndCardbackDrawer` 那一档）。</summary>
        public bool WithCardback;
    }

    public static partial class ItemDrawer
    {
        /// <summary>原版 `DeckDrawer : ItemDrawer&lt;PrebuiltDeck&gt;`。</summary>
        public const string DrawerDeck = "DeckDrawer";
        /// <summary>原版 `DeckAndCardbackDrawer : DeckDrawer`。</summary>
        public const string DrawerDeckAndCardback = "DeckAndCardbackDrawer";

        /// <summary>`Deck Drawer` prefab 里 `Collection Deck` 那一格的高度（**设计 px**）——
        /// 本抽屉那几颗字号（`Deck Name` fs32）按它折算。出处见上面的「几何」段。</summary>
        const float DeckDrawerRefH = 496.97f;

        /// <summary>按 prefab 的 `anchorMin/anchorMax` 换算成我们的绝对矩形。
        /// ⚠️ Unity 的 `anchorMin.y = 0` 是**底边**，我们的 `PxRect` 是**左上原点** ⇒ y 要翻。</summary>
        static PxRect DeckAnchor(PxRect box, float ax1, float ay1, float ax2, float ay2)
        {
            return new PxRect(box.x1 + ax1 * box.W, box.y2 - ay2 * box.H,
                              box.x1 + ax2 * box.W, box.y2 - ay1 * box.H);
        }

        /// <summary>原版 `DeckDrawer.Draw(PrebuiltDeck item, int quantity = 1)` 的四步。
        /// <para>⚠️ 两处**如实标**的偏离：① 原版第 ②步给的是**过扫**的 `Deck Image`（靠 `RectMask2D` 裁），
        /// 我们这一层没有 mask ⇒ 直接按 mask 矩形画（可见范围相同）；② 原版 `armyIcon` /
        /// `gameModeIcon` 是 prefab 里的 `Image`，我们按同一个矩形**建**。</para></summary>
        static void Deck(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            var deck = item.Deck;
            if (deck == null)
            {
                // 判据空：`ItemTypeSets` 说它是 `PrebuiltDeck`，但调用方没把 `Deck` 填进来 ⇒ 照旧占位并出声。
                Placeholder(node, box, item, qty, st, ref res);
                res.MissingArt = true;
                InventoryManager.Note("deck-spec:" + item.Id,
                    "物品 `" + item.Id + "` 走的是 `DeckDrawer`，但 `ItemSpec.Deck` 是 null ⇒ 摊到占位板（调用方要填 `Deck`）");
                return;
            }

            // ① 英雄卡图（原版 `base.image.sprite = item.DeckHero.cardImageReference.Load()`）
            Texture2D hero = CardArt.DeckHero(deck.heroId);
            PxRect heroR = DeckAnchor(box, 0.044f, 0.180f, 0.956f, 0.93547f);
            if (hero != null)
            {
                MenuDraw.Rect(node, hero, heroR, NodeIcon, st.QArt, null, false, st.Clip, st.ClipSoftness);
                res.Art = deck.heroId;
            }
            else
            {
                res.MissingArt = true;
                InventoryManager.Note("deck-hero:" + deck.heroId,
                    "预组卡组 `" + deck.deckId + "` 的督军 `" + deck.heroId + "` 取不到卡图（`CardArt.DeckHero`）⇒ 只画框与名字");
            }

            // 卡框（`Frame` = `40K_bt_deck`）
            Texture2D frame = Tex("40K_bt_deck");
            if (frame != null)
                MenuDraw.Rect(node, frame, DeckAnchor(box, 0f, 0.04935f, 1f, 0.958f), NodeDeckFrame, st.QArt + 1,
                              null, false, st.Clip, st.ClipSoftness);

            // ② 阵营徽记（原版 `armyIcon.sprite = ArmyUtilities.GetArmyIcon(item.DeckHero.cardArmy)`）
            Texture2D fac = Tex(DeckRuntime.FactionIcon(deck.faction));
            if (fac != null)
                MenuDraw.Rect(node, fac, DeckAnchor(box, -0.042f, 0.11253f, 0.296f, 0.32425f), NodeDeckFaction,
                              st.QArt + 2, null, true, st.Clip, st.ClipSoftness);

            // ③④ 模式图标：有图才画（原版 `gameModeIcon.enabled = 图标非空`）
            string gmName = MenuDraw.GameModeIconFile(deck.gameMode);
            Texture2D gm = Tex(gmName);
            if (gm != null)
                MenuDraw.Rect(node, gm, DeckAnchor(box, 0.682f, 0.11253f, 1.02f, 0.32425f), NodeDeckGameMode,
                              st.QArt + 2, null, true, st.Clip, st.ClipSoftness);

            // 名字（原版 `GetItemName` = 词条 `"PrebuiltDecks/" + deckId`；`Deck.DisplayName` 就是这个口）
            float fs = 32f * (box.H / DeckDrawerRefH);
            ClippedText(node, DeckAnchor(box, 0.08f, 0.052f, 0.92f, 0.15f), deck.DisplayName, Color.white,
                        NodeItemName, fs, st.QText, box.W, st);

            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item)) Quantity(node, box, qty, st);   // 2026-10-28（B89_E17c）：原版 `ItemDrawerOptions.stackable`（+ prefab 的 `quantity` 槽是否为 null）—— 判据/边界 → `Shell/ItemDrawerOptions.cs` 文件头。⚠️ 零行数变化（本行只换内容）
        }

        /// <summary>原版 `DeckAndCardbackDrawer.Draw`：`base.Draw(item, q)`，然后**若 `cardBackDrawer` 非空**再画卡背。
        /// <para>🔴 **判据实情**：本地能找到的**两处** `DeckAndCardbackDrawer` 实例（都在
        /// `General Basic Offer … Variant Deck_cardback_avatar` 那两个宿主 prefab 里）**`cardBackDrawer` 都是 null**
        /// ⇒ 原版在**当前能观察到的全部实例**上，这一跳与 `DeckDrawer.Draw` **完全等价**（只是多一个 null 判断）。
        /// 独立 prefab `Deck and Cardback Drawer` 本地**查不到**（`assets_full` 全 91 个 bundle 按 GameObject 名 +
        /// 全 JSON 搜 `"Deck and Cardback"` 均无，只有内嵌节点）⇒ 「卡背子抽屉」那一格的节点树**判据空**。
        /// 所以这里：① 有 `cardBackDrawer` 等价物的数据（`WithCardback` + `Deck.Cardback`）就画一层卡背；
        /// ② 画不出来时**出声**（⛔ 不静默）。</para></summary>
        static void DeckAndCardback(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                   ref ItemDrawResult res)
        {
            Deck(node, box, item, qty, st, ref res);
            if (item.Deck == null) return;

            Texture2D cb = item.Deck.Cardback;
            if (cb == null)
            {
                InventoryManager.Note("deck-cardback:" + item.Deck.deckId,
                    "预组卡组 `" + item.Deck.deckId + "` 的卡背（`" + (item.Deck.cardback ?? "?")
                    + "`）取不到 ⇒ 不画卡背那一层（`DeckAndCardbackDrawer` 原版对 `cardBackDrawer == null` 也是跳过）");
                return;
            }
            // 卡背画在**右下角**那块空档上（原版那格是 `CosmeticDrawer` 自己的 prefab 节点，本地查不到 ⇒
            // 位置是**我们挑的**，等拿到真 prefab 再改）。⚠️ 这一条是**我们挑的，不是原版做法**。
            float side = Mathf.Min(box.W, box.H) * 0.26f;
            var cbr = new PxRect(box.x2 - side - box.W * 0.06f, box.y2 - side - box.H * 0.04f,
                                 box.x2 - box.W * 0.06f, box.y2 - box.H * 0.04f);
            MenuDraw.Rect(node, cb, cbr, NodeDeckCardback, st.QArt + 3, null, true, st.Clip, st.ClipSoftness);
        }

        // 本抽屉自己那几个节点名（诊断/自检按它认；与 `NodeIcon` 等同一套写法）
        const string NodeDeckFrame = "Deck Frame";
        const string NodeDeckFaction = "Deck Faction Icon";
        const string NodeDeckGameMode = "Deck Game Mode Icon";
        const string NodeDeckCardback = "Deck Cardback";
    }

    // ============================================================ 追加区 · 2026-10-26（批次 5 · 代理 G8）
    //
    // 【这一段补的是什么】原版 `ItemDrawer<T>` 是 **MonoBehaviour + IPointerClickHandler** ——
    //   「画出来的那一格抽屉」**本身就是一个可点格子**。我们的抽屉只是 `MenuDraw` 建的节点树，
    //   `ItemDrawerView` 那 9 个「画完之后再调」的方法**全库 0 调用点** ⇒ 功能与死代码等同
    //   （F7 第 4 轮 §五·4 的结论）。这一区把它接上，并把原来缺的三处判据补齐。
    //
    // 【判据 ①（点击的第一道闸）】`out_cpp2il_dev_ilspy/ItemDrawer.cs:917-951`（🥈 唯一有真方法体那份）逐句：
    //   `if (!showItemInfoOnClick) return;` → `if (item != null) { if (onClick == null) HandlePointerClick();
    //   else onClick(); if (inspectSound != null) inspectSound.Play2D(); } else CustomDebug.LogError(…)`。
    //   🔴 **第一道闸是【序列化字段】`showItemInfoOnClick`**（`dump.cs:93379` `[SerializeField] protected bool`），
    //      而 `ToggleShowInfoOnClick(bool)` 在 `:876-878` **不是空体**（🔴 2026-10-29 R-K 用 VA + 374 份 prefab 全量复扫证明：
    //      原版写的是 `this+0x44 = option`，ILSpy 读成 `{ }` = 丢赋值；`OnPointerClick.c:13` 读的正是这一格）⇒ 它那 4 个调用点
    //      （`MissionRewardItem:20` / `EndBattleRewardHolder:15` / `GenericArmyItemDrawer:35` / `CarouselOfferPopupDrawer:305`）是**真置位**。⛔ 但仍不能「一律可点」，要按预制体实例的那个值来。
    //   值从哪来（2026-10-26 现扫）：`assets_full` 全树 **374 份**带该字段的 MB ⇒ `0=296 / 1=78`；
    //      按 `m_Script` → `bundle_Waprforge_monoscripts/MonoScript_<pathID>.json` 的 `m_ClassName` 归类：
    //      CardDrawer 19/19=1 · WildcardDrawer 1/1=1 · DeckDrawer 1/1=1 · DeckAndCardbackDrawer 2/2=1 ·
    //      CardAlternateArtDrawer 7/7=1 · AvatarBorderDrawer 5/5=1 · **CardbackDrawer 23 个 0 / 3 个 1（混合）** ·
    //      WildcardIconDrawer 1/1=0 · XSollaOfferDrawer 1/1=0 · PremiumIconDrawer/TitleIconDrawer/ForgePointIconDrawer 全 0。
    //      ⇒ **同一类里也会不一致** ⇒ 只有「全票一致」的那几档敢给默认值，混合档留 `SetShowInfoOnClick()` 由调用方定。
    //   我们实现的抽屉只有五档 ⇒ 表里没命中的（含 `DrawerIcon`：**原版全库没有 `IconDrawer` 这个类**）一律 0 + 出一次声。
    //
    // 【判据 ②（点击音）】`inspectSound` 的 PPtr = `{m_FileID:6, m_PathID:-5758964851120979674}`（374 份 MB 同值）。
    //   2026-10-26 用 UnityPy 读**真包**（`d:/2/unity_run_ref/.../soundcollection_assets_all.bundle`）解出：
    //   `m_Name = "ButtonClick 1"` · `clipList = [AudioClip "UI Click 1"]` · pitch/volume 全 1.0 · 节流 0.1s。
    //   ✅ **2026-10-27 订正（铁律 5；本行原来写「工程没有 `UI Click 1.wav` ⇒ 不出声」）**：现在**有了** ——
    //      cue 已进 `工具/import_original_sfx.py` 的 `EXTRA_CUES`、clip 已拷进 `Resources/Art/audio/sfx/` ⇒ 真播（见下）。
    //
    // 【判据 ③（句柄表）】`ItemDrawer.cs:36-68` 静态 `Draw` 末段 = `drawerReferences.Add(itemDrawer, ref)`；
    //   `:98-112` 两个 `Release` = `dict.TryGetValue ⇒ ref.Release(General)`；`:134-141` `OnDestroy` = `OnRelease()` + 同一次 Release；
    //   `drawerReferences` 的 `.cctor` 在 `:171-175`。活调用点（现核）：`WhereToGetCardsSection.cs:205` 调实例 `Release()`、
    //   `GachaRewardContainer.cs:81` 调静态 `Release(itemDrawer)`。
    //   ⚠️ **差异（如实记）**：原版的「句柄」是 Addressables 的 `ComponentReference`，`Release` **不从字典里删键**
    //      （对已释放句柄再 release 是安全空操作）；我们的图走 `Resources`、没有加载句柄 ⇒ 净效果等价物 = **解除登记**
    //      （`HandleCount` 减一、第二次调用是空操作）。
    //
    // 【判据 ④（Refresh）】`ItemDrawer.cs:344-352` 全篇一句 `Draw(item, amount, options)` ⇒ **同一格重画**，
    //   装饰层（premium / ephemeral / converted）原版也不会被 `Refresh` 重刷 ⇒ 我们同样不重刷。
    //   入参（`item` / `amount` / `options`）是实例字段 `+0x38` / `+0x40` / `+0x28` ⇒ 句柄自己留了一份 `Spec` / `Quantity` / `_style`。
    //
    // 🔴 **行号守恒**：本条只往**文件尾**加东西（`ItemDrawResult` / `ItemDrawerView` 都改成 partial）；
    //   中间那几处改动全是**同一行内替换**（`Draw` 的 switch 仍是 7 行、`OnPointerClick` 仍是 11 行）
    //   ⇒ 账里钉在 `Shell/ItemDrawer.cs:427…1605` 的那批 A 落点**零漂移**。⛔ 别把这些新成员挪进上面。

    /// <summary>`ItemDrawResult` 的第二部（⛔ 别加进 `:235` 那个本体 —— 见追加区头注「行号守恒」）。</summary>
    public partial struct ItemDrawResult
    {
        /// <summary>画出来的那一格的**句柄**（原版 `Draw` 的返回值本身就是个 `ItemDrawer` 组件）。
        /// `null` = 没画（`Node == null` 那一支）。</summary>
        public ItemDrawerView View;
    }

    public static partial class ItemDrawer
    {
        /// <summary>④ 抽屉自己的 Draw —— **只此一份**（`Draw` 与 `ItemDrawerView.Refresh()` 共用；
        /// 两处各写一遍 switch 迟早不一致）。</summary>
        internal static void Dispatch(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                      ItemDrawerStyle style, ref ItemDrawResult res)
        {
            switch (drawer)
            {
                case DrawerWildcard: Wildcard(node, box, item, quantity, style, ref res); break;
                case DrawerWildcardIcon: WildcardIcon(node, box, item, quantity, style, ref res); break;
                case DrawerIcon: Icon(node, box, item, quantity, style, ref res); break;
                case DrawerDeck: Deck(node, box, item, quantity, style, ref res); break;
                case DrawerDeckAndCardback: DeckAndCardback(node, box, item, quantity, style, ref res); break; case DrawerCampaignPoint: CampaignPoint(node, box, item, quantity, style, ref res); break; case DrawerCurrency: DrawCurrency(node, box, item, quantity, style, ref res); break; case DrawerAvatar: Avatar(node, box, item, quantity, style, ref res); break; case DrawerAvatarBorder: AvatarBorder(node, box, item, quantity, style, ref res); break; case DrawerCardback: Cardback(node, box, item, quantity, style, ref res); break; case DrawerCosmetic: Cosmetic(node, box, item, quantity, style, ref res); break; case DrawerContainer: Container(node, box, item, quantity, style, ref res); break; case DrawerRandomCard: RandomCard(node, box, item, quantity, style, ref res); break; case DrawerTitle: case DrawerTitleHorizontal: Title(node, box, item, quantity, style, ref res); break; case DrawerTitleIcon: TitleIcon(node, box, item, quantity, style, ref res); break; case DrawerPremium: Premium(node, box, item, quantity, style, ref res); break; case DrawerPremiumIcon: PremiumIcon(node, box, item, quantity, style, ref res); break; case DrawerExpansionPremium: ExpansionPremium(node, box, item, quantity, style, ref res); break; case DrawerForgePoint: ForgePoint(node, box, item, quantity, style, ref res); break; case DrawerForgePointIcon: ForgePointIcon(node, box, item, quantity, style, ref res); break; case DrawerExpansionPassPoint: ExpansionPassPoint(node, box, item, quantity, style, ref res); break; case DrawerAllianceBadge: AllianceBadge(node, box, item, quantity, style, ref res); break; case DrawerXSolla: XSollaOffer(node, box, item, quantity, style, ref res); break; case DrawerCard: CardDrawerItem(node, box, item, quantity, style, ref res); break; case DrawerCardAltArt: CardAltArtItem(node, box, item, quantity, style, ref res); break;   // 🆕 2026-10-28（B90-N6）；`DrawerCurrency` 起的 18 档 = 2026-10-30（B101_E3）；最后两档（卡牌）= 2026-10-30（B101_R8，判据 = `Shell/ItemDrawerCard.cs` 文件头）
                default: Placeholder(node, box, item, quantity, style, ref res); break;
            }
        }

        /// <summary>画完之后的「建句柄 + 登记进句柄表 + 按原版那道序列化闸决定要不要可点」。</summary>
        static ItemDrawResult Bind(ItemDrawResult res, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle style)
        {
            var v = new ItemDrawerView(res, box, style);
            v.RememberFace(item.Label, item.Art);      // 句柄自己留一份（`ItemSpec` 出了这句就没了）
            v.Spec = item; v.Quantity = quantity;      // `Refresh()` 要用（原版是实例字段 +0x38/+0x40）
            v.ShowInfoOnClick = ShowInfoOnClickOf(res.Drawer);
            res.View = v;
            _handles[v] = res.Drawer; ItemDrawerLeash.Attach(v);   // 原版 `drawerReferences.Add(itemDrawer, ref)`；后半句 = 原版 `OnDestroy` 的等价钩子（见 `ItemDrawerLeash.cs`）
            if (v.ShowInfoOnClick) v.AttachHit(style); // 原版 `OnPointerClick` 那道闸为真 ⇒ 这一格可点
            return res;
        }

        /// <summary>原版抽屉 prefab 的 `showItemInfoOnClick`（**只有「全票一致」的类才敢给默认值**）。
        /// 证据 = 追加区头注 ①（2026-10-26 全树扫的 374 份 MB + 类名归类）。</summary>
        public static bool ShowInfoOnClickOf(string drawer)
        {
            if (drawer == DrawerWildcard || drawer == DrawerDeck || drawer == DrawerDeckAndCardback || drawer == DrawerAvatarBorder || drawer == DrawerForgePoint || drawer == DrawerTitle || drawer == DrawerExpansionPremium || drawer == DrawerCard || drawer == DrawerCardAltArt) return true;   // 后四个 = 2026-10-30（B101_E3）：施工单 §三-2 里**全票一致为 1** 的四档（AvatarBorderDrawer 5/5 · ForgePointDrawer 1/1 · TitleDrawer 3/3 · ExpansionPassPremiumDrawer 9/9）；混合档（Cardback/Container/Premium/PremiumIcon/TitleHorizontal/AllianceBadge/Avatar/RandomCard）仍留 0、由调用方 `SetShowInfoOnClick` 定
            if (drawer == DrawerWildcardIcon) return false;
            if (drawer == DrawerIcon)
                Note("showinfo|" + drawer, "原版没有 `IconDrawer` 这个类（`cpp2il` 全库 0 个），"
                     + "374 份带 `showItemInfoOnClick` 的抽屉 MB 里**没有一份**能对上它 ⇒ 这一档**判据空**，按 0 办（不可点）");
            return false;
        }

        /// <summary>句柄表（原版 `drawerReferences`）。`Release` 只解除登记，见追加区头注 ③。</summary>
        static readonly System.Collections.Generic.Dictionary<ItemDrawerView, string> _handles
            = new System.Collections.Generic.Dictionary<ItemDrawerView, string>();

        /// <summary>已登记的抽屉句柄数（自检读口）。</summary>
        public static int HandleCount { get { return _handles.Count; } }

        /// <summary>自检用：清空句柄表（⛔ 只给自检，别在生产路径调）。</summary>
        public static void ClearHandlesForTest() { _handles.Clear(); }

        /// <summary>原版 `ItemDrawer.Release(ItemDrawer)` / 实例 `Release()` 的等价物（追加区头注 ③）。</summary>
        public static void Release(ItemDrawerView view)
        {
            if (view == null) return;
            if (!_handles.Remove(view))
                Note("release|miss", "`Release` 收到一个**没登记过**的抽屉句柄 ⇒ 原版那两句都是 "
                     + "`dict.TryGetValue` 落空（什么都不做）⇒ 我们同样不动");
        }

        /// <summary>原版 `inspectSound.Play2D()`（`ItemDrawer.cs:944-947`）—— **cue 表里查不到时**才走这里
        /// （正常那一支见 `PlayInspectCue`；⛔ 本方法只出声说明、不拿别的 clip 顶）。</summary>
        internal static void NoteInspectCueMissing()
        {
            Note("inspectSound", "抽屉点击音那条 cue `" + WindowSounds.ButtonClick1Cue + "` 在表里查不到"
                 + "（表 = `Resources/animfx_sounds.json`，产出方 = `工具/import_original_sfx.py` 的 `EXTRA_CUES`）；"
                 + "原版这一跳 = `inspectSound.Play2D()`，clip `UI Click 1`（2026-10-26 读真包现核）。"
                 + "⇒ 这一格**不出声**（⛔ 不拿别的 clip 顶），跑一次那个导入器即可");
        }
    }

    public sealed partial class ItemDrawerView
    {
        /// <summary>原版 `ItemDrawer<T>.showItemInfoOnClick`（序列化字段，`dump.cs:93379`）—— **点击的第一道闸**：
        /// 假 ⇒ 连 `OverrideOnClickAction` 覆写与音效都**不会发生**（原版 `OnPointerClick` 头一句就 return）。</summary>
        public bool ShowInfoOnClick { get; set; }

        /// <summary>画这一格用的那三样（原版实例字段 `item +0x38` / `amount +0x40` / `options +0x28`）——
        /// `Refresh()` 重画要用。</summary>
        internal ItemSpec Spec;
        internal int Quantity;

        /// <summary>点击命中区那个节点（原版这一层就是组件自己那颗 `Graphic` 的 `RectTransform`）。</summary>
        public Transform HitNode { get; private set; }

        /// <summary>按**这一格**改那道闸（原版是逐 prefab 实例的序列化值；混合档 `CardbackDrawer` 靠它定）。</summary>
        public void SetShowInfoOnClick(bool on)
        {
            ShowInfoOnClick = on;
            if (on && HitNode == null) AttachHit(_style);
            else if (!on && HitNode != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(HitNode.gameObject);
                else UnityEngine.Object.DestroyImmediate(HitNode.gameObject);
                HitNode = null;
            }
        }

        /// <summary>建点击命中区。🔶 自己设计（非原版）：队列 = `QDecor + 6` 是**自选值** —— `ItemDrawerStyle` 没有「命中档」这一格，取「最高视觉层 `QDecor + 4` 的 ephemeral 字」再 +2 · 2026-10-26 G8 选值、调度台记账：**需真机验**（点一格商店野牌；若抢点击只改这一个数）</summary>
        internal void AttachHit(ItemDrawerStyle style)
        {
            if (Node == null || HitNode != null) return;
            HitNode = MenuDraw.Hit(Node, "Hit", _box, style.QDecor + 6, () => OnPointerClick(), clip: style.Clip);
            if (HitNode == null)
                Debug.LogWarning("[ItemDrawer] `" + Drawer + "` 的点击命中区没建起来（`MenuDraw.Hit` 返回 null）"
                               + " ⇒ 这一格**真鼠标点不动**（不静默）");
        }

        /// <summary>原版 `ItemDrawer<T>.Refresh()`（`ItemDrawer.cs:344-352`，全篇一句 `Draw(item, amount, options)`）
        /// —— **同一格重画**（不重建节点；装饰层原版也不会被 `Refresh` 重刷，我们同样不重刷）。</summary>
        public void Refresh()
        {
            if (Node == null) return;
            MenuDraw.ClearChildren(Node);
            HitNode = null;                                   // 命中区是 Node 的子件 ⇒ 刚被一起清了
            var res = new ItemDrawResult { Id = Id, Kind = Kind, Drawer = Drawer };
            ItemDrawer.Dispatch(Drawer, Node, _box, Spec, Quantity, _style, ref res);
            if (ShowInfoOnClick) AttachHit(_style);
        }

        /// <summary>原版 `ItemDrawer.Release()`（实例版，`ItemDrawer.cs:106-112`）。</summary>
        public void Release() { ItemDrawer.Release(this); }

        /// <summary>原版 `inspectSound.Play2D()`（`ItemDrawer.cs:944-947`）—— 见追加区头注 ②。</summary>
        void PlayInspectSound() { ItemDrawer.PlayInspectCue(); }
    }

    // ============================================================ 追加区 · 2026-10-27（B9_W6DECK · 账 `卡组:75` + `收藏:273`）
    //
    // 【这一段补的两件】
    //   ① `卡组:75`：原版 `DeckDrawer : ItemDrawer<PrebuiltDeck>` **覆写了 `HandlePointerClick`**
    //      （`cpp2il/out_cpp2il_dev_ilspy/DeckDrawer.cs:17-28`；交叉核对 `decomp_full/DeckDrawer__HandlePointerClick.c` 逐句）：
    //        `new CardDeck(item)` → `new DeckInfoContext(deck, **View(2)**, selectButton = null,
    //        checkOwnership = *(this+0x70)（`[SerializeField] bool shouldCheckOwnership`，初值 **true**）,
    //        editButtonAction = null)` → `WindowsManager.OpenWindow<DeckInfoPopup>(ctx)`。
    //      ⇒ 点「一副卡组」那格抽屉 = **开卡组详情窗（View 态）**，不是基类那条「开物品详情窗」。
    //      🔴 `.c` 的六个实参里有**两个 0**，含义不同：第 3 个 = 「没有 `selectButton`」、
    //         第 5 个 = 「没有 `editButtonAction`」；第 4 个是 `uVar1 = *(param_1+0x70)`（那个字段）。
    //      ⚠️ **不走 `DeckInfoPopup.CreateForPicked`**：那一支强制 `SelectButtonProvided = true`（会多一颗
    //         `Select Deck`），而原版这里 `selectButton = null`。同口径先例 = `Shell/LiveOpsEventWindow.cs`
    //         的 `OpenDeckInfo`（它引的 `.c` 是 `DeckInfoContext___ctor(ctx, deck, 0, 0, 1, 0, 0)`）。
    //      ⚠️ `DeckDrawer.SetShowOwnership(bool)`（`DeckDrawer.cs:30-33`）只写那个字段，**反编译全库 0 调用点**
    //         ⇒ 我们只提供方法、不接任何调用点（与原版一致）。
    //      🔴 **时间限定（与施工单同批读）**：`PrebuiltDeck` 实例 225 个里只有 2 个被 Demo `DeckInfo` SO、
    //         1 个被 `Intro.unity:7792 starterDeck` 引用，**没有任何商店/奖励 SO 以 `ObtainableItem` 形式发放它**
    //         （原版走远端目录）⇒ **本 build 的玩家在游戏里看不到这一格**，只有自检夹具造得出来。
    //         远端目录一旦回来，这一跳**不用再改代码**就可见。
    //
    //   ② `收藏:273`：原版 `ItemDrawer.OnPointerClick` 末句 `inspectSound.Play2D()` ——
    //      `inspectSound` 的 PPtr 解开 = AudioCue **`ButtonClick 1`** → clip **`UI Click 1`**
    //      （pitch/vol 1.0 · 节流 0.1 s；2026-10-26 用 UnityPy 读 `soundcollection_assets_all.bundle` 现核）。
    //      这条 cue 与 clip **现在已经都在工程里**（`工具/import_original_sfx.py` 的 `EXTRA_CUES` 收了
    //      `ButtonClick 1`、`UI Click 1.wav` 已拷进 `Resources/Art/audio/sfx/`）⇒ `PlayInspectSound()`
    //      从「只出声说明」改成**真播**，走壳侧唯一播放口 `WindowSounds.PlayFx`。
    //      ⛔ 别在别处再写一遍 cue 名：名字的唯一来源 = `WindowSounds.ButtonClick1Cue`。
    //
    // 🔴 **行号守恒**：本段只往**文件尾**加东西；对上面那两处生产代码的改动都是**同一行内替换**
    //   （`HandlePointerClick` 的第一句、`PlayInspectSound` 的方法体）⇒ 账里钉在
    //   `Shell/ItemDrawer.cs:427…1968` 的那批 A 落点**零漂移**。⛔ 别把这些新成员挪进上面。

    public static partial class ItemDrawer
    {
        /// <summary>最后一次由**卡组抽屉**开出来的 `Deck info Popup`（自检读口；`null` = 还没开过 / 已收尾）。
        /// 同族先例 = `Shell/LiveOpsEventWindow.LastDeckInfo`。</summary>
        public static DeckInfoPopup LastDeckInfo;

        /// <summary>原版 `inspectSound.Play2D()`（`ItemDrawer.cs:944-947`）—— cue `ButtonClick 1` / clip `UI Click 1`，
        /// **FX 组**。判据与「这条 cue 怎么进的工程」见追加区头注 ②。
        /// <para>表里查不到 ⇒ 退回原来那条**出声说明**（⛔ 不静默、也⛔ 不拿别的 clip 顶）。
        /// ⚠️ **不看 `PlayFx` 的返回值**：它返回 false 还有「被 `timeToPlayAgain` 节流」那一支
        /// （原版同一句 `AudioCue.CanPlay` 的行为，不是失败）⇒ 按返回值判缺失会**误报**。</para></summary>
        internal static void PlayInspectCue()
        {
            if (WarpforgeVFX.WFSoundBank.HasCue(WindowSounds.ButtonClick1Cue))
                WindowSounds.PlayFx(WindowSounds.ButtonClick1Cue, "ItemDrawer.OnPointerClick");
            else
                NoteInspectCueMissing();
        }

        /// <summary>原版 `DeckDrawer.HandlePointerClick()`（`DeckDrawer.cs:17-28` + `.c` 逐句）——
        /// 点「一副卡组」那格 ⇒ 开 `Deck info Popup`：`State = View` · `selectButton = null` ·
        /// `CheckOwnership = shouldCheckOwnership` · `editButtonAction = null` · `IsPlayerDeck = false`。
        /// <para>返回 `false` = 这一格不是卡组抽屉 / 没有卡组数据 ⇒ 调用方继续走基类那条「开物品详情窗」。</para>
        /// <para>🔴 `IsPlayerDeck = false` = 原版 `InventoryManager.HasItem(预组)` 为假（预组不在玩家卡组库里）
        /// ⇒ `Share` / `Delete` / `Duplicate` / `Practice` 按判据藏；`Edit Deck` 另由
        /// `DeckInfoPopup.EditDeckShown()` 那半个合取项藏（判据与回退条件钉在那个方法上，⛔ 别在这里再写一遍）。</para>
        /// <para>⚠️ `DeckIndex` 传 **−1**：预组**不在 `DeckLibrary` 里**（同 `LiveOpsEventWindow.OpenDeckInfo` 的先例）。</para></summary>
        public static bool ShowDeckInfo(ItemSpec item, bool checkOwnership)
        {
            var deck = item.Deck;
            if (deck == null)
            {
                // 判据空那一档（与 `Deck()` 画占位板同一条）：出声，但**不要把点击吞掉**
                // —— 基类那条「开物品详情窗」还能给出一个东西可看（原版这一格根本不会有 null）。
                Note("deckinfo|" + (item.Id ?? "<空>"),
                     "物品 `" + (item.Id ?? "<空>") + "` 走的是卡组抽屉，但 `ItemSpec.Deck` 是 null ⇒ 开不了卡组详情"
                     + "（调用方要在 `ItemSpec.Deck` 里填 `PrebuiltDecks.Deck`）");
                return false;
            }
            var mgr = WindowsManager.Get();
            if (mgr == null)                       // `Get()` 理论上不给 null；留一道**出声**的闸（不许静默失败）
            {
                Debug.LogWarning("[ItemDrawer] 没有 `WindowsManager` ⇒ 卡组抽屉点不开卡组详情（"
                               + deck.deckId + "）");
                return false;
            }
            var w = DeckInfoPopup.Create(mgr, -1, DeckInfoPopup.DeckInfoState.View);
            w.OverrideDeck = PrebuiltDecks.ToPlayerDeck(deck);
            w.OverrideInfo = DeckSelectionPopup.InfoOf(deck);
            w.IsPlayerDeck = false;
            w.CheckOwnership = checkOwnership;     // 原版 `*(param_1 + 0x70)`
            mgr.OpenWindow(w);
            LastDeckInfo = w;
            return true;
        }
    }

    public sealed partial class ItemDrawerView
    {
        /// <summary>原版 `DeckDrawer.shouldCheckOwnership`（`[SerializeField]`，字段初值 **true** ——
        /// `cpp2il/out_cpp2il_dev_ilspy/DeckDrawer.cs:15`）。只在「开卡组详情」那一条路上被读。</summary>
        bool _showOwnership = true;

        /// <summary>这一格现在会把 `shouldCheckOwnership` 传成什么（自检读口）。</summary>
        public bool ShowOwnership { get { return _showOwnership; } }

        /// <summary>原版 `DeckDrawer.SetShowOwnership(bool)`（`DeckDrawer.cs:30-33`，**只写字段**；
        /// 反编译全库 **0 调用点** ⇒ 我们也不接调用点）。</summary>
        public void SetShowOwnership(bool showOwnership) { _showOwnership = showOwnership; }

        /// <summary>`HandlePointerClick()` 里那一跳：卡组抽屉 ⇒ 开卡组详情窗（= 原版 `DeckDrawer` 的重写）。
        /// 返回 `true` = 这一跳已吃掉，别再走基类的「开物品详情窗」。</summary>
        internal bool DeckClick()
        {
            if (Drawer != ItemDrawer.DrawerDeck && Drawer != ItemDrawer.DrawerDeckAndCardback) return false;
            return ItemDrawer.ShowDeckInfo(Spec, _showOwnership);
        }
    }

    public static partial class ItemDrawer
    {
        // 🆕 2026-10-28（B90-N6）原版 `CampaignPointDrawer` 的等价实现（账 `战役:53 DrawForArmy` / `战役:55 GetIcon`）。
        //   判据：① `CampaignPointDrawer.cs:26`（`DrawForArmy(army)`：`armyText = CardArmyToString(army)` +
        //         `ArmyUtilities.GetArmyIcon(army)`；`image.enabled = image.m_Sprite`；`Draw` 默认 `CardArmy.Neutral`）；
        //         ② `CampaignPoints.cs:46`（`GetIcon(size)`：Small→`smallIcon`、Large→`bigIcon`）。
        //   两张图本仓都已在 `Resources/Art/ui_menu/`（`40K_genearl_icon_Campaign_points` = small ·
        //   `40K_genearl_icon_Campaign_points_big` = big；⚠️ 原版拼写就是 `genearl`）。
        //   ⚠️ 可达性（如实）：我方数据里战役点是标量计数，当前没有「战役点作为奖励 item」流过 `ItemDrawer.Draw`
        //      ⇒ 这条抽屉今天**零可观测**（账 `战役:53` 的 B89-R1d 结论仍成立）。建它是按铁律 11
        //      「只分先后、不分做不做」补全抽屉库，落点与几何照同族 `Wildcard`（⛔ 不是照 prefab 量的 ——
        //      原版抽屉几何我们没量，见本文件头 ②）。

        /// <summary>原版抽屉类名（`ItemTypeSets` 里 `CampaignPoints` 的 `IconDrawer` 档就是它）。</summary>
        public const string DrawerCampaignPoint = "CampaignPointDrawer";

        /// <summary>原版 `CampaignPoints.GetIcon(IconSize)` 的等价物：`large` ⇒ `bigIcon`、否则 `smallIcon`。
        /// 两个落盘名都在 `Resources/Art/ui_menu/`（⚠️ 原版拼写 `genearl`）。</summary>
        public static string CampaignPointIconName(bool large)
        {
            return large ? "40K_genearl_icon_Campaign_points_big" : "40K_genearl_icon_Campaign_points";
        }

        /// <summary>原版 `CampaignPointDrawer.Draw(CampaignPoints item)` ⇒ **`DrawForArmy(item.CardArmy)`** 的等价实现。
        /// 🔴 **2026-10-29（B96_W1）就地订正（铁律 5）—— 原版这格只画【一层】，而且是【替换】不是叠加**：证据 + 出处全在方法体首段那十行注释里。
        /// 🔶 自己设计（非原版）：`Square()` 那套**居中方块**是我们挑的 —— 理由在方法体末段。</summary>
        static void CampaignPoint(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            // `CampaignPointDrawer__DrawForArmy.c:22-27`（现读）**只有两段**：
            //   ① `if (armyText) armyText.text = GameStaticData.CardArmyToString(army);`
            //   ② `if (image) { image.sprite = ArmyUtilities.GetArmyIcon(army); image.enabled = image.m_Sprite != null; }`
            // ⇒ **主图 = 阵营徽记**（`image` 被**替换**，不是叠加）· `CampaignPoints.GetIcon` 不在这支里（`CampaignPoints__GetIcon.c` 全量反编译 **零调用点**）。
            // 🔴 两处 prefab 事实（`Icon Campaign Points Drawer Variant.prefab` = `Campaign Tab.prefab` 内嵌的那一份，逐字段现读）：
            //   · `armyText: {fileID: 0}` ⇒ **阵营名那层原版根本不画**（只有非 Icon 变体 `Campaign Points Drawer.prefab` 才有 `Army` 节点）；
            //   · 那张 `Image` 的出厂 sprite 恰好是 `40K_genearl_icon_Campaign points_big`（guid `d2b2111c…`）—— 运行时被 `DrawForArmy` **换掉** ⇒ 它**不是**这格的最终画面（原来按它画 = 画错）。
            // ⚠️ 几何：`Content/Image` 的 `anchorMin/Max = (0,0) → (1,1)` + `m_PreserveAspect = 1` ⇒ 铺满抽屉框、等比内接 ⇒ `ArmyFill = 1`（= `Square(box, 1)`）。
            // 第 1 段（`armyText`）：本变体是空引用 ⇒ 原版跳过 ⇒ 我们只在 `st.NamePx > 0` 时才补这一层（`CampaignPointStyle()` 给 0 = 不画，与 prefab 一致）。
            // 第 2 段（`image`）：取不到徽记 ⇒ 原版把 `Image` **关掉**（整格不画、也**不补占位板**）。
            string fac = ArmyName(item.CardArmy);          // `Neutral(0)` ⇒ `GetArmyIcon` 落空 ⇒ 原版把那张 `Image` 关掉
            if (fac == null) return;
            string art = DeckRuntime.FactionIcon(fac);
            var tex = Tex(art);
            if (tex == null) { res.MissingArt = true; return; }
            res.Art = art;
            MenuDraw.Rect(node, tex, Square(box, st.ArmyFill), NodeArmyIcon, st.QArt, null, true, st.Clip, st.ClipSoftness);
            // 🔶 自己设计（非原版）：`Square()` 那套**居中方块**是我们挑的 —— 原版由 `Content` 锚点直接铺满，我们没有那层 `Content` 包装；
            //   必须自己定：没有锚点层可照抄（抽屉直接画在 `node` 上）· 执行代理 B96_W1 2026-10-29 定。
            float k = box.H / DecorRefH;
            if (st.NamePx > 0f)
                ClippedText(node, NameStrip(box, st.NamePx), Loc.T("Armies/" + fac), Color.white, NodeArmyName,
                            st.NamePx, st.QText, box.W - 20f, st, 0, 12f * k, 75f * k, 36f * k);
            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item)) Quantity(node, box, qty, st);
        }

        // ============================================================ 🆕 2026-10-28（B90-N6）战役点「奖励 item」数据路径 + 本地累计账
        //  目的：让「战役点」作为**奖励 item** 真的能流进 `ItemDrawer`（原版 `CampaignPointDrawer` 就是被
        //  `CampaignPoints` 奖励流调到的）。`CampaignPointSpec` 给出 `Type="CampaignPoints"` ⇒ PickDrawer 走真表 → `CampaignPointDrawer`。
        //  🔶 自己设计（非原版）：本地累计账 + 阵营默认值 + 标签文案 —— 原版战役点是服务端 `PlayerData` 标量（本地 0）·
        //     上游服务端发奖按 b 档本地模拟 · 调度台 2026-10-28 裁定

        /// <summary>造一条「战役点奖励」的 <see cref="ItemSpec"/>（`Type="CampaignPoints"` ⇒ `PickDrawer` 走真表 → `CampaignPointDrawer`）。
        /// <paramref name="army"/> = 原版 `CampaignPointDrawer.DrawForArmy(army)` 的阵营（`0`=Neutral ⇒ 原版把那张 `Image` 关掉、整格不画；⚠️ `Art` 只是提示，`CampaignPoint` 画的是阵营徽记）。</summary>
        public static ItemSpec CampaignPointSpec(int army, int amount)
        {
            return new ItemSpec
            {
                Id = "CampaignPoints",
                Kind = ItemKind.Generic,
                Type = "CampaignPoints",       // 原版类型名（`ItemTypeSets` 的键）⇒ 走真表 → CampaignPointDrawer
                CardArmy = army,
                Art = CampaignPointIconName(true),
                Label = "Campaign Points",
            };
        }

        /// <summary>把一条「战役点奖励」画进 <paramref name="parent"/>（走 `Draw` 那条四步链）。
        /// 返回结果；`res.Drawer == DrawerCampaignPoint` 表示这一票真的走到了 `CampaignPointDrawer`。
        /// <para>⚠️ **画 ≠ 发奖**：本方法只画。真正的「本地累计」在 <see cref="GrantCampaignPointsLocal"/>，
        /// 由**领取那一刻**的调用方调（原版是服务端发奖；本地按 b 档模拟）。</para></summary>
        public static ItemDrawResult DrawCampaignPointsReward(Transform parent, PxRect box, int amount, int army,
                                                              ItemDrawerStyle style)
        {
            if (parent == null || box.W <= 0.01f || box.H <= 0.01f)
                return new ItemDrawResult { Id = "CampaignPoints", Kind = ItemKind.Generic };
            return Draw(parent, box, CampaignPointSpec(army, amount), amount, DrawerOverride.Default, style);
        }

        /// <summary>🔶 本地战役点账（原版 = 服务端 `PlayerData` 标量）。键 = 阵营（`CardArmy` 值，`0`=Neutral）。</summary>
        public static readonly System.Collections.Generic.Dictionary<int, int> CampaignPointsLocal
            = new System.Collections.Generic.Dictionary<int, int>();
        static bool _cpSaidOnce;

        /// <summary>🔶 本地给战役点（模拟服务端发奖）。只记账 + **出声一次**（⛔ 不静默）。
        /// 调用点 = 战役奖励「领取」那一刻（落在 `Shell/CampaignRewardWindow.cs` / `Shell/CampaignData.cs`，本代理白名单外 ⇒ 见报告 §需要别的代理做的）。</summary>
        public static void GrantCampaignPointsLocal(int army, int amount)
        {
            if (amount <= 0) return;
            int cur; CampaignPointsLocal.TryGetValue(army, out cur);
            CampaignPointsLocal[army] = cur + amount;
            if (!_cpSaidOnce)
            {
                _cpSaidOnce = true;
                Debug.Log("[ItemDrawer] 战役点本地记账：阵营 " + army + " +" + amount + " ⇒ " + CampaignPointsLocal[army]
                        + "（🔶 原版是服务端 PlayerData 标量，本地模拟；本条只出声一次）");
            }
        }

        /// <summary>🔶 本地战役点读数（自检/展示用）。</summary>
        public static int CampaignPointsLocalOf(int army)
        {
            int cur; return CampaignPointsLocal.TryGetValue(army, out cur) ? cur : 0;
        }
    }

    // ============================================================ 原版 `GenericArmyItem`（B94_S3 · 2026-10-29）
    //
    // 判据（逐句实读）：
    //   · `d:/2/tools/decomp_full/GenericArmyItem__GetItem.c`（**无参那一支**：`LiveOpsManager.GetHandler<T>()`
    //     → 非 null 就 `GetItem(handler + 0x7C)`，否则 `GetItem(0)`）
    //   · `d:/2/tools/decomp_full/GenericArmyItem__get_CurrentArmy.c`（同一个跳：handler null ⇒ **返回 0**）
    //   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GenericArmyItem.cs`：类 = `ObtainableItem` 的子类；
    //     嵌套 `record Entry { CardArmy army; ItemInfo<ObtainableItem> reward; }`；
    //     谓词 `b__0` = `entry.army(+0x10) == 目标.army(+0x10)`（`DisplayClass5_0___GetItem_b__0.c` 实读）
    //   · `GetItem(CardArmy army)` 的方法体**在 `decomp_full` 里没有**（只有无参那一份）⇒ 按 cpp2il 的
    //     `entries.TryGetValue(army)` 否则 `values.FirstOrDefault(x.army == army)` 复刻（那一行是 ILSpy 现读）。
    //
    // ⚠️ **两处如实标**：
    //   ① 原版 `handler` 是 `CampaignHandler`（我们仓里**没有这个类**）。它的「当前阵营」= `handler + 0x7C`，
    //      而本工程的等价物早就定了 = `DailyData.HeaderArmyValue`（`DailyData.cs` 那一节写明
    //      「≡ 原版 `CampaignHandler.SelectedArmy`，`DailyRewardPopup__Open.c` 读 `+0x7C`」）⇒ 默认走它。
    //   ② 原版 `GetItem(army)` 找不到时 `FirstOrDefault` 返回 **null**（引用类型）；我们的 `ItemSpec` 是 `struct`
    //      ⇒ 返回 `default`，**判据 = `Id == null`**（调用方按这个判「没有」）。
    /// <summary>原版 `GenericArmyItem`（`ObtainableItem` 子类：按**当前阵营**取一条物品）的等价实现。
    /// <para>它是「阵营 → 物品」那张表 + 两条取法：无参（当前阵营）与按阵营。</para>
    /// <para>⚠️ 本工程**没有消费点**（原版挂在 LiveOps 商店的若干个 drawer 上）⇒ 判据齐、行为齐、无调用方，
    /// 由 `Editor/B93S3Assert.cs` 钉住。</para></summary>
    public sealed class GenericArmyItem
    {
        /// <summary>= 原版嵌套 `GenericArmyItem.Entry`（`CardArmy army` ＋ `ItemInfo&lt;ObtainableItem&gt; reward`）。</summary>
        public struct Entry
        {
            public int Army;         // 原版 `army`（`+0x10`）
            public ItemSpec Reward;  // 原版 `reward`（`ItemInfo<ObtainableItem>`；我们 = `ItemSpec`）
            public Entry(int army, ItemSpec reward) { Army = army; Reward = reward; }
        }

        readonly System.Collections.Generic.List<Entry> _entries;

        /// <summary>= 原版那个序列化的 `entries` 表（本机没有 SO ⇒ 由调用方喂）。
        /// ⚠️ 原版是 `Dictionary&lt;CardArmy, …&gt;` 形状（`TryGetValue` 那一支）＋ `values` 线性兜底；
        /// 我们只有一份 `List`，**两种取法都在它上面做**（`TryGetValue` 用首条匹配代替 —— 顺序只影响重复键，
        /// 而原版 `Dictionary` 也不允许重复键 ⇒ 语义一致）。</summary>
        public GenericArmyItem(System.Collections.Generic.IEnumerable<Entry> entries)
        {
            _entries = entries != null
                     ? new System.Collections.Generic.List<Entry>(entries)
                     : new System.Collections.Generic.List<Entry>();
        }

        /// <summary>= 原版 `GenericArmyItem.CurrentArmy`（`get_CurrentArmy.c`）：
        /// `handler = LiveOpsManager.GetHandler&lt;CampaignHandler&gt;()` ⇒ `handler == null ? 0 : handler.CurrentArmy`。
        /// <para>⚠️ 如实标 ①：`CampaignHandler` 我们仓里没有 ⇒ 走 <see cref="CurrentArmyProvider"/>；
        /// **没装** = 原版「没有 handler」那一支 ⇒ **0**（Neutral）。</para></summary>
        public static int CurrentArmy
        {
            get
            {
                if (CurrentArmyProvider == null) return 0;          // 原版 handler == null ⇒ return 0
                int? a = CurrentArmyProvider();                      // null = 这一拍没有 handler
                return a.HasValue ? a.Value : 0;
            }
        }

        /// <summary>= 原版那个 `handler + 0x7C`（`CampaignHandler.SelectedArmy`）的本地来源。
        /// 默认 = `DailyData.HeaderArmyValue`（本工程的等价物，判据见类头如实标 ①）。
        /// **返回 `null`** = 这一拍没有 handler（原版那一支 ⇒ 阵营 0）。</summary>
        public static Func<int?> CurrentArmyProvider = () => DailyData.HeaderArmyValue;

        /// <summary>= 原版 `GenericArmyItem.GetItem()`：**按当前阵营取**。
        /// <para>`.c` 逐句：`handler = LiveOpsManager.GetHandler&lt;CampaignHandler&gt;()`；
        /// `if (handler != null) return GetItem(handler.CurrentArmy); return GetItem(0);`</para></summary>
        public ItemSpec GetItem() { return GetItem(CurrentArmy); }

        /// <summary>= 原版 `GenericArmyItem.GetItem(CardArmy army)`：先 `entries.TryGetValue(army)`，
        /// 命中不到再 `values.FirstOrDefault(x.army == army)`。**都没有** ⇒ 原版 null，
        /// 我们 `default(ItemSpec)`（判据 = `Id == null`）。</summary>
        public ItemSpec GetItem(int army)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Army == army) return _entries[i].Reward;
            return default(ItemSpec);
        }

        /// <summary>自检口：把静态 provider 还原成默认（它是静态字段 ⇒ 每条断言收尾都要调）。</summary>
        public static void ResetForTest() { CurrentArmyProvider = () => DailyData.HeaderArmyValue; }
    }
}
