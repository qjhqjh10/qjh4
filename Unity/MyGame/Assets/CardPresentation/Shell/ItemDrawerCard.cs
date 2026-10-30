// ============================================================================ 卡牌抽屉两档（原版 `CardDrawer` / `CardAlternateArtDrawer`）
//
//  判据（施工单 `判据/B100_D1_抽屉18档_施工单.md` 表 B 的 B6 / B7；方法体 = `d:/2/tools/decomp_full/`）：
//    `CardDrawer.Draw(RawCardScript item, int quantity = 1)`
//      `{ bannedIcon.gameObject.SetActive(false);          // ① 关掉那颗「禁用」横幅（`+0x68`）`
//      `  cardUI.SetRawCardData(item, null); }              // ② 第二个实参 = `CardTier?` null ⇒ 用卡 SO 自己的档`
//    `CardAlternateArtDrawer.Draw(AlternateArtCard item, int quantity = 1)`
//      `{ clonedCard = AssetLocator.GetAsset<RawCardScript>(item.GetIdForClonedCard());  // ① id.Replace("#","")`
//      `  cardUI.SetRawCardData(clonedCard, null); }                                     // ②（没有 bannedIcon 那一句）`
//  ⇒ **两档都只做一件事：把那棵 `2DCard` 子树画出来**。
//
//  ── 等价物 = `Core/CardView.cs`（**不是**"本机没有 `BasicCardUI`"，E3 的那条判断已推翻）──────────
//    `Card Drawer.prefab` 的 `CardUI/2DCard` 下：`Front/CardImage` · `Front/CardFrame` ·
//    `Rarity` + `Card Rarity Sprite` · `Cost/Melee/Range/Health Container+Text` · `Textbackgrounds` ·
//    `Card Info` —— 与 `CardView` 建出来的层**逐项同名同类**（`CardView` 就是从这棵树复刻的）。
//    `BasicCardUI` 另外挂的那几层里，`Draw` 要么不碰、要么直接关：
//      `newCardBadge` → `SetRawCardData` 第一句 `SetActive(false)` · `bannedIcon` → 同上 + `CardDrawer.Draw` 再关一次 ·
//      `createdByText` / `Card Ready for level up` → prefab 出厂 `m_IsActive=0` · `greyscaleController` → 不碰。
//    ⇒ **只画 `CardView` 就是等价物**，`ItemDrawerComponents` 四槽**全 null** ⇒ 没有底图/名字/数量。
//
//  ── 缩放：判据是一条**算式**，不是某个常量（`×0.4839` 只是某个宿主格子里量到的值）────────────
//    `UIScaleToFit__Fit.c`（VA `0x180872B00`）：
//      `if (paddingType == 0 /*Flat*/) targetW = targetRect.rect.w + padding.x;`
//      `else               /*Multiplier*/ targetW = targetRect.rect.w * padding.x;`
//      `ratio = min(targetW / this.rect.w, targetH / this.rect.h);  transform.localScale = (ratio,ratio,ratio)`
//    两份 prefab 的序列化字段（YAML 现读，逐字相同）：
//      `fitInParent = 1` · `targetRect = Content` · `padding = (1.35, 1.35)` · **`paddingType = 1`（乘）**
//      `CardUI.sizeDelta = (2.5437, 3.3686)`
//    ⇒ 每 1 个「卡单位」要画 `1.35 × 712 / 2.5437 = 377.87` 设计 px（宽度这一侧赢：
//      712/2.5437 = 279.9 < 1080/3.3686 = 320.6）。
//    ⇒ 卡体（2.0927 × 3.3313 卡单位）= **790.76 × 1258.85 设计 px**，中心 = `Content` 中心。
//      ⚠️ **它比 `Content`（712×1080）大 35%** —— 这是原版行为（`paddingType=1` 就是"过扫"），
//      ⛔ 别为了"整齐"把它缩回去（铁律 18·0：原版自己也这样）。
//    `CardView` 在 1 个世界单位 = 108 设计 px 下，scale = 1 时卡体宽 `CardView.Width × 108`；
//    ⇒ **`localScale = 377.87 × frameK / 108 = 3.4988 × frameK`**（`CardDrawerScale` 那个纯函数就是它）。
//
//  ── 🔴 原版 quirk（照抄，⛔ 别"修好"）────────────────────────────────────────
//    `SetRawCardData` 里写的是 `cardRarityHolderController.SetCardRarity(0, maxCardRarity)`，
//    而 `CardRarityHolderController__SetCardRarity.c` 是 `sprite = rarityLevelSprites[clamp(rarity-1,0,..)]`
//    ⇒ 传 0 **被夹成下标 0** ⇒ `Card Drawer` 里那颗稀有度宝石**恒为 `1_40k_cardframe_rarity_common_0`**，
//    **与这张卡真实稀有度无关**（该数组 5 张在 prefab 里现读：common/rare/epic/...）。
//
//  ── 未查清（如实记，施工按"替换"实现、两侧都幂等）────────────────────────────
//    `AlternateArtCard.GetIdForClonedCard()` = `id.Replace("#", "")` —— 两个字面量已用
//    `工具/read_il2cpp_string.py` 解出（`0x1842507f8` = `"#"`、`0x1842b80e0` = `""`）。
//    ⚠️ **但 `#` 挂在"异画物品 id"上还是"克隆卡 id"上，静态读不出来**；我们数据里**一条异画物品都没有**
//    （`Resources/profile_cosmetics.json` 0 个 `#`）⇒ 先把"去 `#`"实现成**幂等**（没有 `#` 就是原串），
//    方向查实之后不用改代码。
//    ⚠️ **异画立绘要不要替换**：原版只做了 `SetRawCardData(clonedCard)`（= 用**查出来的那张卡自己的
//    `cardSprite`**），没有去读 `AlternateArtCard.cardImage` ⇒ 我们**也照这条**（不额外套 `CardArt.AltArt`）。
//    若日后查实"克隆卡的 `cardSprite` 就是异画"，那也自然落在"查出来的那张卡"上，同样不用改这里。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `CardDrawer : ItemDrawer&lt;RawCardScript&gt;`。</summary>
        public const string DrawerCard = "CardDrawer";
        /// <summary>原版 `CardAlternateArtDrawer : ItemDrawer&lt;AlternateArtCard&gt;`。</summary>
        public const string DrawerCardAltArt = "CardAlternateArtDrawer";

        // ---- 版面常量（出处见文件头「缩放」那一节，全部是 prefab / 反编译实读，⛔ 不是我们挑的）----
        /// <summary>`CardUI` 的 `sizeDelta`（两份 prefab 逐字相同）。**USF 的分母**。</summary>
        const float DrCardUIW = 2.5437f, DrCardUIH = 3.3686f;
        /// <summary>`UIScaleToFit.padding`（`paddingType = 1` = **乘**）。</summary>
        const float DrCardPadMul = 1.35f;
        /// <summary>卡面根节点名（照 prefab 逐字；自检按它取件）。</summary>
        public const string NodeCardUI = "CardUI";

        /// <summary>原版 `UIScaleToFit` 那一档的**纯算式**（不建任何节点，供自检直接调）。
        /// <para>`frameK` = 抽屉根的设计 px → 实际 px 的换算（= `FrameOf` 的 `K`；整格时 = 1）。</para>
        /// <para>推导：`ratio = min(1.35×Content.w/2.5437, 1.35×Content.h/3.3686)`，宽度侧赢；
        /// `Content.w = 712 × frameK` ⇒ 每卡单位 `377.87 × frameK` 设计 px；
        /// 而 `CardView` 在 scale = 1 时 1 卡单位 = `ImageQuad.PixelsPerUnit`（= 108）设计 px。</para></summary>
        public static float CardDrawerScale(float frameK)
        {
            float perUnitPx = DrCardPadMul * DrRootW / DrCardUIW;      // 1.35 × 712 / 2.5437 = 377.87
            return perUnitPx * frameK / ImageQuad.PixelsPerUnit;       // ÷108 ⇒ 整格（frameK=1）时 3.4988
        }

        /// <summary>原版 `CardDrawer.Draw` 的等价实现（B6）。
        /// ⚠️ 方法名**不能叫 `CardFace`** —— 那会与 `CardPresentation.CardFace` 这个枚举同名，类内的
        /// `CardFace.Full` 解析到方法组（`CS0117`，2026-10-30 `B101_R8` 实测踩过）。</summary>
        static void CardDrawerItem(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle style,
                                   ref ItemDrawResult res)
        {
            DrawCardBody(node, box, item, quantity, style, ref res, altArt: false);
        }

        /// <summary>原版 `CardAlternateArtDrawer.Draw` 的等价实现（B7）。</summary>
        static void CardAltArtItem(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle style,
                                   ref ItemDrawResult res)
        {
            DrawCardBody(node, box, item, quantity, style, ref res, altArt: true);
        }

        /// <summary>两档共用的那一段（差别只有 §文件头 ①：B7 先 `id.Replace("#","")`）。
        /// ⚠️ **不要合并成一个函数后把「两档的差别」也吃掉** —— 差别就是"取哪张卡"，这里用 `altArt` 显式带着。</summary>
        static void DrawCardBody(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle style,
                                 ref ItemDrawResult res, bool altArt)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);

            // ① 卡 id：`ItemSpec.CardId` 优先（施工单 §四-2 那个位），空则回落 `item.Id`
            //    （原版 `RawCardScript` 物品的 id **就是**卡的资产名 ⇒ 同一件东西）。
            //    B7：`GetIdForClonedCard()` = `id.Replace("#","")` —— 幂等，没有 `#` 就是原串。
            string cardId = string.IsNullOrEmpty(item.CardId) ? item.Id : item.CardId;
            if (altArt && !string.IsNullOrEmpty(cardId)) cardId = cardId.Replace("#", "");

            var def = CollectionData.Card(cardId);
            if (def == null)
            {
                res.MissingArt = true;
                Note("carddrawer|" + (cardId ?? "<空>"),
                     "卡抽屉 `" + (item.Id ?? "?") + "` 的卡 id `" + (cardId ?? "<空>") + "` 在卡池里查不到"
                     + "（`CollectionData.Card` → `CardDatabase.Load()`）⇒ 这一层**不画**（⛔ 不拿别的图顶替）");
                return;
            }

            // ② `cardUI.SetRawCardData(item, null)` = 画出这张卡（等价物 = `CardView`，见文件头）。
            var data = BattleDriver.ToCardData(def, def.Faction);
            var v = CardView.Create(content, data, NodeCardUI, CardFace.Full, style.Clip);
            if (v == null)
            {
                res.MissingArt = true;
                Note("carddrawer|build|" + cardId, "卡抽屉 `" + cardId + "` 的 `CardView` 没建出来 ⇒ 这一层**不画**");
                return;
            }

            // ③ 位姿 + 缩放（算式见 `CardDrawerScale`）。卡根原点 = **卡心**（`CardFeel` 那条约定的同一个量）
            //    ⇒ 局部 (0,0) 正好落在 `Content` 中心；`Content` 的 pivot 也是中心（`MenuDraw.Node` 写死的）。
            //    ⚠️ 必须走 `SetPose`（它写 `_baseScale` 再 `ApplyScale`）—— 直接写 `transform.localScale`
            //    会被后续 `SetHighlightScale` / `SetPose` 按 `_baseScale(=1)` **静默冲回原大小**。
            v.SetPose(Vector3.zero, 0f, CardDrawerScale(fr.K));
            // 裁切：`style.Clip` 非空时 `Create` 已经 `SetClip`（显式覆盖优先）；为空 ⇒ 照
            // `MenuDraw.Rect(clip: null)` 那条同一语义，**沿父链**找最近的 `ViewportClip`
            // （先例 = `CollectionWindow.cs:1666`，那里也是 `SetPose` 之后紧跟这一句）。
            if (!style.Clip.HasValue) v.SetClipFromTree();
            v.SetQueue(style.QArt);                 // 卡面各层统一进这一档（`CardView` 自己的队列段约定）
            // ④ 原版 quirk：宝石恒 `common`（见文件头）—— 必须在 `SetQueue` **之后**调：
            //    `SetQueue` 会按 (源材质, 队列) 换一份克隆材质，先换贴图会被那一步盖掉。
            v.SetRarityGem("common");

            res.Art = cardId;
        }
    }
}
