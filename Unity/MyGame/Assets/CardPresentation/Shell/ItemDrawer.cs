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
    public struct ItemSpec
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
    public struct ItemDrawResult
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
    public static class ItemDrawer
    {
        // ============================================================ 抽屉名（诊断 + 自检按它认）

        /// <summary>野牌全抽屉（原版 `WildcardDrawer : ItemDrawer&lt;Wildcard&gt;`）。</summary>
        public const string DrawerWildcard = "WildcardDrawer";
        /// <summary>野牌图标档（原版 `WildcardIconDrawer : ItemDrawer&lt;Wildcard&gt;`，`Override = Icon` 那档）。</summary>
        public const string DrawerWildcardIcon = "WildcardIconDrawer";
        /// <summary>通用图标抽屉 —— **我们建的**（原版对应哪个类**读不出来**）。</summary>
        public const string DrawerIcon = "IconDrawer";
        /// <summary>占位板抽屉 —— **我们建的**（判据空的物品走它；原版这边是「真抽屉 + prefab 自带的图」）。</summary>
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
        /// 而同一个文件 `:545-547`（`&lt;/remarks>` 里那条「`noov` 随守卫一起删掉」）/ `:601-607`（下面 `pick`
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
        /// <para>⚠️ **表里的类 ≠ 我们实现的抽屉**：本工程真正照原版画法实现的只有野牌那两档
        /// （见 <see cref="DrawerImpls"/>）；别的类查得到、但 `ImplOfDrawerClass` 返回 `null`
        /// ⇒ `PickDrawer` **出声 + 兜底**（⛔ 不静默画成别的样子）。</para>
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
                         + "（`DrawerImpls` 只覆盖野牌那两档）⇒ 兜底，**不静默画成别的样子**");
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
        /// （同一个文件 `:545-547` / `:601-607` 自己已经写着「守卫已删」）。搬的四条理由：
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
        /// <para>⚠️ **只覆盖两个**：本工程**真正照原版画法实现的抽屉只有野牌那两档**
        /// （<see cref="DrawerWildcard"/> 照 `WildcardDrawer__Draw.c` 三层、<see cref="DrawerWildcardIcon"/>
        /// 照 `WildcardIconDrawer__Draw.c`；其余 18 个原版抽屉**没实现**）。
        /// ⇒ 表里列不到的类返回 `null`，<see cref="PickDrawer"/> **出声 + 兜底**，⛔ 不静默画成别的样子。</para>
        /// <para>🔴 `"WildcardDrawer:WildcardDrawer"` 这种「左右同名」**是巧合，不是恒等**：左边是**原版**
        /// 那个类的名字，右边是**我们**给实现的常量名（<see cref="DrawerWildcard"/>）。
        /// 我们的常量名当年就是照原版类名起的 ⇒ 今天撞在一起。**别据此把这张表当成「名字相同就算实现」**。</para></summary>
        public static readonly string[] DrawerImpls =
        {
            "WildcardDrawer:" + DrawerWildcard,          // 原版 `WildcardDrawer : ItemDrawer<Wildcard>`
            "WildcardIconDrawer:" + DrawerWildcardIcon,  // 原版 `WildcardIconDrawer : ItemDrawer<Wildcard>`
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
            var s = new ItemSpec { Id = targetId, Kind = ItemKind.Unknown, Art = art, Label = label };
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
            if (s.Kind == ItemKind.Unknown && !string.IsNullOrEmpty(art)) s.Kind = ItemKind.Generic;
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
                string.IsNullOrEmpty(style.NodeName) ? "Item Drawer" : style.NodeName, box);

            switch (drawer)                                                        // ④ 抽屉自己的 Draw
            {
                case DrawerWildcard: Wildcard(res.Node, box, item, quantity, style, ref res); break;
                case DrawerWildcardIcon: WildcardIcon(res.Node, box, item, quantity, style, ref res); break;
                case DrawerIcon: Icon(res.Node, box, item, quantity, style, ref res); break;
                default: Placeholder(res.Node, box, item, quantity, style, ref res); break;
            }
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
            ClippedText(root, lb, AlreadyOwnedText, Color.white, NodeAlreadyOwned, 75f * k, st.QDecor + 5,
                        lb.W, st, 0, 12f * k, 75f * k, 36f * k, true);
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
            var row = new PxRect(strip.x1, strip.CY - iconSide * 0.5f, strip.x2, strip.CY + iconSide * 0.5f);
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
            //   （= 原版 `Price Display` 那个节点的矩形，640.8×108 设计值 ⇒ 与六份 prefab 逐位相同），
            //   `折行` 照原版关回 0 ⇒ 这一格是**下界守卫**、不吃掉任何排版自由度。
            var lb = MenuDraw.Text(pd, row, s, Color.white, NodePriceText, textPx, q, row.W,
                                   13.46f * k, 65f * k, 39f * k);
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
                //   压在视口边上整段画出去（**静默**）。同一函数里 `:996` 的 `VisibleAbove`（粗筛）与
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

        /// <summary>第 2 跳那段时间串 —— **原版 `SupportMethods.GetTimeString` 的逐句移植**
        /// （`decomp_full/SupportMethods__GetTimeString.c` 170 行全文读；调用点的三个开关
        /// `eventTimer: true, clampTo0: false, hide0Values: false` 是 `SetEphemeralDisplay` 里**立即数**读到的）。
        /// <para>结构（`hide0Values = false` 这一支，也就是我们唯一用到的那一支）：
        /// `TotalDays &gt; 1` ⇒ `DH(Days, Hours)` · 否则 `TotalHours &gt; 1` ⇒ `Minutes == 0 ? H_Only(hours) : HM(hours, minutes)`
        /// · 否则 ⇒ `MS(minutes, seconds)`。另两支（`hide0Values = true` / `eventTimer = false`）也照抄了，
        /// 免得以后有人真用上时是另一套。</para>
        /// <para>🔴 **六个词条 key 是解出来的、正文是兜底**：key 逐地址从 `script.json` 的 `ScriptString` 解出 =
        /// `MainMenu/Time/{DH,D_Only,H_Only,HM,M_Only,MS}`（地址 `0x42C1B10 / 0x42C1C10 / 0x42C2010 /
        /// 0x42C1F10 / 0x42C2310 / 0x42C2210`）；**词条正文在远端 CCD、本地没有** ⇒
        /// 我们按 **调用方那两个 `Replace`** 反推格式串 —— 只有 `"{0}d {1}h"` / `"{0}h {1}m"` 这种写法里
        /// 才会出现 `0h` / `0m` 这两个子串（原版紧接着就把它们删掉）⇒ 兜底串 = `{0}d {1}h` / `{0}d` / `{0}h` /
        /// `{0}h {1}m` / `{0}m` / `{0}m {1}s`。**这是【我们挑的】**，别当原版文案。</para>
        /// <remarks>**改坏法**：把 `"0h"` 那一句 `Replace` 删掉 ⇒ `2d 0h` 那种串会原样冒出来（自检那条红）。</remarks></summary>
        public static string EphemeralTextOf(long ms)
        {
            if (ms < 0) ms = 0;
            var span = System.TimeSpan.FromMilliseconds(ms);
            string t;
            if (span.TotalDays > 1d)
                t = string.Format("{0}d {1}h", span.Days, span.Hours);          // `MainMenu/Time/DH`
            else if (span.TotalHours > 1d)
                t = span.Minutes == 0 ? string.Format("{0}h", (int)span.TotalHours)   // `MainMenu/Time/H_Only`
                                      : string.Format("{0}h {1}m", (int)span.TotalHours, span.Minutes); // `HM`
            else
                t = string.Format("{0}m {1}s", span.Minutes, span.Seconds);     // `MainMenu/Time/MS`
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
            // `armyText`：⚠️ 原版过一道 I2 本地化（`GameStaticData__CardArmyToString.c`：`Enum.ToString`
            //   拼词条 key → `I2_Loc.GetTranslation`），而**词条表在远端 CCD、本地没有**
            //   ⇒ 我们画的是**枚举名**（`Ultramarines`），**不是原版屏幕上那串**（同 `Claim`/`Claimed` 那两处的口径）
            if (st.NamePx > 0f && fac != null)
                ClippedText(node, NameStrip(box, st.NamePx), fac, Color.white, NodeArmyName,
                            st.NamePx, st.QText, box.W - 20f, st);
            if (st.QuantityPx > 0f) Quantity(node, box, qty, st);
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
            var tex = Tex(item.Art);
            if (tex == null)
            {
                Placeholder(node, box, item, qty, st, ref res);
                res.MissingArt = true;
                return;
            }
            res.Art = item.Art;
            MenuDraw.Rect(node, tex, Square(box, st.IconFill), NodeIcon, st.QArt, null, true, st.Clip,
                          st.ClipSoftness);
            if (st.QuantityPx > 0f) Quantity(node, box, qty, st);
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
                ClippedText(node, br, item.Label, Color.white, NodeItemName, st.NamePx, st.QText, br.W, st);
            if (st.QuantityPx > 0f) Quantity(node, box, qty, st);
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
            const float side = 10f, top = 10f;
            return new PxRect(box.x1 + side, box.y1 + top, box.x2 - side, box.y1 + top + px * 1.4f);
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
            ClippedText(node, qr, "x" + qty, Color.white, NodeQuantity, st.QuantityPx, st.QText, qr.W, st, 2);
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
        /// 而 `MenuDraw.ClipText` **必须是最后一步**（A781/A435：任何一次重排都会把裁好的顶点抹掉）
        /// ⇒ 「关折行」只能在这里、`ClipText` **之前**做，⛔ 调用方拿到返回值再关就晚了。</param>
        static Label ClippedText(Transform node, PxRect r, string s, Color color, string name,
                                 float fontPx, int q, float wrapPx, ItemDrawerStyle st, int align = 0,
                                 float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f,
                                 bool wrapOff = false)
        {
            // 🔴 2026-10-13（A435）：`Visible` → **`VisibleAbove`**（纯矩形函数手上没有 `Transform`，
            //   解析不了节点 ⇒ 必须走带 `parent` 的那个重载，判据 → `MenuDraw.VisibleAbove` 的注释）。
            if (!MenuDraw.VisibleAbove(node, r, st.Clip)) return null;
            var lb = MenuDraw.Text(node, r, s, color, name, fontPx, q, wrapPx,
                                   autoMinPx, autoMaxPx, autoBasePx);
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
    }
}
