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
// ============================ 🔴 本地【读不到】的两样 —— 别再查 ============================
//  ① **「类型 → 抽屉 prefab」那张映射表**（= `ItemDrawerConfig` SO）：`grep -rl customDrawerOverrides assets_full` **0 命中**、
//     `assets_full/globalgamemanagers/MonoBehaviour/` 是**空目录**（只有类名的 `MonoScript`）⇒ **读不出来**。
//     ⇒ 本文件 `PickDrawer` 的表**是我们推的**，每条都在注释里写了依据（类名族 + 22 处调用点的 override 值 + §四 的画法）。
//  ② **抽屉 prefab 本身**（普查 §三 那 6 份里**没有一份是野牌抽屉**）⇒ 抽屉**内部每一层的矩形/字号**是**我们挑的**；
//     只有「**画哪几层、每层放哪张图**」有判据（§四 那两个 `Draw` 的字段赋值，逐行照抄）。
//     📌 **一条真的几何旁证**（不是猜）：`Daily Reward Popup Item Drawer` 里那个抽屉实例的 `Content/Image`
//     **与抽屉根同矩形**（286.60×293.27，`scl 1.1`、`preserveAspect`）⇒「主图层铺满抽屉框」这一条有依据，
//     **其余（图标占框的比例、文字的条位）没有**。
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
        /// <summary>礼包弹窗档。⚠️ **22 处调用点里一处都没用到它**（表里有这个值而已）。</summary>
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
        /// <summary>野牌专用。**判据** = 该物品 SO 的 `cardRarity`（1..4）。</summary>
        public int Rarity;
        /// <summary>野牌专用。**判据** = 该物品 SO 的 `cardArmy`（`CardArmy` 枚举值，10 = Ultramarines）。</summary>
        public int CardArmy;
        /// <summary>数据层那张「id → 图」表给的图名（战役这边 = `CampaignData.ItemIcon`）。`null` = 判据空。</summary>
        public string Art;
        /// <summary>没有图时占位板上写的短名（`CampaignData.ItemShortName`）。</summary>
        public string Label;
    }

    /// <summary>抽屉的版式参数 —— **全是调用方给的**（原版这些值在抽屉 prefab 里，本地没有）。
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
        /// `ItemDrawerOptions.stackable` / `showName` 决定的，**而那张配置表本地没有**
        /// ⇒ 我们用调用方给的这两个字号当替身（**我们挑的**：谁调用谁定，逐处有注释）。</summary>
        public float QuantityPx;
        /// <summary>名字的字号（画布像素）；**0 = 不画名字**（占位板短名 / 野牌阵营名）。同 `QuantityPx`：替 `options.showName`。</summary>
        public float NamePx;
        /// <summary>裁切边界（滚动区画内容前给一次，同 `MenuWindowBase.Clip`）。</summary>
        public PxRect? Clip;

        /// <summary>默认档。`IconFill 0.7` / `QuantityPx 34` / `NamePx 26` 就是战役奖励窗格子（200×300）
        /// 原来那三个量（图标 140 = 200×0.7），**为了不动既有的量渲染断言**。</summary>
        public static ItemDrawerStyle Default(int qBoard, int qArt, int qText)
        {
            return new ItemDrawerStyle
            {
                NodeName = "Item Drawer", QBoard = qBoard, QArt = qArt, QText = qText,
                IconFill = 0.7f, ArmyFill = 0.34f, QuantityPx = 34f, NamePx = 26f, Clip = null,
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
        // 原版这些层是 `ItemDrawerComponents` 的 `background/image/label/quantity` 字段，**节点名在 prefab 里、本地读不到**）
        public const string NodeIcon = "Icon";
        public const string NodePlaceholder = "IconPlaceholder";
        public const string NodeItemName = "ItemName";
        public const string NodeQuantity = "Quantity";
        public const string NodeArmyIcon = "Army Icon";
        public const string NodeArmyName = "Army Name";

        /// <summary>占位板底色（**我们挑的**：中性深灰 —— 不拿别的图冒充，也不与真图混淆）。</summary>
        public static readonly Color BoardColor = new Color(0.16f, 0.16f, 0.18f, 1f);

        // ============================================================ ① 选抽屉

        /// <summary>**照 `ItemDrawerConfig.ItemDrawerReference.GetDrawer` + `ItemDrawerConfig.GetReference`**：
        /// 第一轮按**精确类型**找、第二轮按 **is-a** 兜底（那两条谓词 = `…_GetReference_b__0.c` 的 `Equals`
        /// 与 `b__1.c` 的 `ReflectionHelper.Is`）；找到之后再按 `drawerOverride` 取该档变体，**找不到回落主抽屉**。
        /// <para>🔴 **映射表本身是我们推的**（原版那张 `ItemDrawerConfig` SO 本地没有，见文件头）。
        /// 两条推断依据：① `*IconDrawer` 这一族确实存在（`WildcardIconDrawer` / `ForgePointIconDrawer` /
        /// `RandomCardIconDrawer` / `TitleIconDrawer` —— 普查 §二）；② 22 处调用点里用 `Icon(10)` 的 **5 处**
        /// **全是「小徽记」场合**（战役节点 / 结算格 / 通知红点 / 进度格 / 任务格）。
        /// 其余档（`Shop` / `Horizontal` / `OfferPopups`）我们**没有**对应变体 ⇒ 照原版「找不到就回落主抽屉」。</para>
        /// <returns>`null` = 原版第 ②步的「不画」。</returns></summary>
        public static string PickDrawer(ItemSpec item, DrawerOverride drawerOverride)
        {
            if (item.Kind == ItemKind.None) return null;          // ② 原版：drawer == null ⇒ 不画

            string main;
            switch (item.Kind)
            {
                case ItemKind.Wildcard: main = DrawerWildcard; break;
                case ItemKind.Generic: main = DrawerIcon; break;
                default: main = DrawerPlaceholder; break;         // 判据空 ⇒ 占位板（**我们挑的**）
            }
            string alt = OverrideDrawer(item.Kind, drawerOverride);
            return alt ?? main;                                   // 变体找不到 ⇒ 回落主抽屉
        }

        /// <summary>`customDrawerOverrides[override]` 的替身（**我们挑的**，依据见 `PickDrawer`）。</summary>
        static string OverrideDrawer(ItemKind kind, DrawerOverride ov)
        {
            if (ov == DrawerOverride.Icon && kind == ItemKind.Wildcard) return DrawerWildcardIcon;
            return null;
        }

        // ============================================================ 从 id 推物品（**我们挑的**）

        /// <summary>从 id + 数据层给的图/短名组一个 <see cref="ItemSpec"/>。
        /// 🔴 **种类是从 id 推的**（原版按 .NET 类型分，我们读不到那套）—— 三条依据：
        /// ① `Wildcard&lt;阵营&gt;&lt;档&gt;`：本地 4 条 SO 实测 `cardRarity` = 后缀、`cardArmy` = 10，
        ///    `物品 SO` 就在 `bundle_menus_assets_all/MonoBehaviour/WildcardUltramarines1..4.json`；
        ///    ⚠️ **「阵营名写进 id ⇒ cardArmy 就是它」这一步是【推广】**（只有 UM 那 4 条可证）——
        ///    认不出阵营名时 `CardArmy = 0`，那一层就不画（不猜）。
        /// ② `Booster Pack Ultramarines`：SO 的 `containerPreviewImage` 实读到图名 ⇒ 有图、但**用哪个抽屉不知道** ⇒ `Generic`。
        /// ③ 其余：`art` 有 ⇒ `Generic`；`art` 没有 ⇒ `Unknown`（**判据空**，走占位板 + 出声）。</summary>
        public static ItemSpec Spec(string targetId, string art, string label)
        {
            var s = new ItemSpec { Id = targetId, Kind = ItemKind.Unknown, Art = art, Label = label };
            if (string.IsNullOrEmpty(targetId)) { s.Kind = ItemKind.None; return s; }

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
                                   NodeArmyIcon, st.QArt, null, true, st.Clip);
            }
            // `armyText`：⚠️ 原版过一道 I2 本地化（`GameStaticData__CardArmyToString.c`：`Enum.ToString`
            //   拼词条 key → `I2_Loc.GetTranslation`），而**词条表在远端 CCD、本地没有**
            //   ⇒ 我们画的是**枚举名**（`Ultramarines`），**不是原版屏幕上那串**（同 `Claim`/`Claimed` 那两处的口径）
            if (st.NamePx > 0f && fac != null)
                MenuDraw.Text(node, NameStrip(box, st.NamePx), fac, Color.white, NodeArmyName,
                              st.NamePx, st.QText, box.W - 20f);
            if (st.QuantityPx > 0f) Quantity(node, box, qty, st);
        }

        /// <summary>**照 `WildcardIconDrawer__Draw.c`**：只写 `image` = `iconsByRarity[cardRarity−1]`
        /// （`Count <= i` 时 `FirstOrDefault` = 那张图取不到就不设，我们照「取不到 ⇒ 不画」处理）。
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
            MenuDraw.Rect(node, tex, Square(box, st.IconFill), NodeIcon, st.QArt, null, true, st.Clip);
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
            MenuDraw.Rect(node, tex, Square(box, st.IconFill), NodeIcon, st.QArt, null, true, st.Clip);
            if (st.QuantityPx > 0f) Quantity(node, box, qty, st);
        }

        /// <summary>占位板（**我们建的**）：中性底色 + 短名 + 数量，并把 `Placeholder` 记上 ——
        /// 调用方**必须**把 `Placeholder` 为真的那些 id **逐条出声**（`CLAUDE.md` §三：不许静默失败）。
        /// 原版没有这一档：它那边每个物品都有真抽屉（图在 prefab 里），我们这边**判据空**才落到这儿。</summary>
        static void Placeholder(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
        {
            res.Placeholder = true;
            var br = Square(box, st.IconFill);
            MenuDraw.Rect(node, CardArt.Solid(), br, NodePlaceholder, st.QBoard, BoardColor, false, st.Clip);
            if (st.NamePx > 0f && !string.IsNullOrEmpty(item.Label))
                MenuDraw.Text(node, br, item.Label, Color.white, NodeItemName, st.NamePx, st.QText, br.W);
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
        /// 原版这一格是 `ItemDrawerComponents.Quantity`，**它到底怎么排版在 prefab 里、本地读不到**。</summary>
        static void Quantity(Transform node, PxRect box, int qty, ItemDrawerStyle st)
        {
            const float side = 10f, top = 60f, bottom = 15f;
            var qr = new PxRect(box.x1 + side, box.y2 - top, box.x2 - side, box.y2 - bottom);
            var lb = MenuDraw.Text(node, qr, "x" + qty, Color.white, NodeQuantity, st.QuantityPx, st.QText, qr.W);
            if (lb != null) MenuDraw.AlignRight(lb, qr);
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
