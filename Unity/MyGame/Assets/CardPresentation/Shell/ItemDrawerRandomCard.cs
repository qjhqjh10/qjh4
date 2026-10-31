// ============================================================================ 随机卡抽屉（原版 `RandomCardDrawer`）—— **唯一「稀有度 × 阵营」双轴显隐的一档**
//
//  判据（施工单 B8 + §三-1 的 4 组合表；方法体 = `cpp2il/out_cpp2il_dev_ilspy/RandomCardDrawer.cs`）：
//    `Draw(DropTableItem item, int quantity = 1)` 五段（逐句）：
//      ① `baseBackground.SetActive(item.cardRarity == 0);`
//      ② `background.gameObject.SetActive(item.cardRarity != 0);`
//         `background.sprite = raritySprites[item.cardRarity - 1];`
//      ③ `randomArmyIcon.SetActive(item.cardArmy == CardArmy.Neutral);`
//      ④ `base.image.gameObject.SetActive(item.cardArmy != CardArmy.Neutral);`
//         `base.image.sprite = ArmyUtilities.GetArmyIcon(item.cardArmy);`
//      ⑤ `if ((bool)armyText) armyText.text = GameStaticData.CardArmyToString(item.cardArmy);`
//    ⇒ 四个状态见施工单 §三-1 那张表（**⛔ 别写成 if/else 只有两支**）：
//      rarity==0 / >0 决定「底图用哪一层」，army==Neutral / ≠ 决定「右半边画圆底还是阵营徽记」，**两轴正交**。
//    ⚠️ 第 ⑤ 段**恒执行**（不看 army）—— 但 `Icon Random Card Drawer Variant` 的 `armyText` 槽是 **null**
//      ⇒ 那一档写不进去（我们按「档位」区分：本函数只服务 `RandomCardDrawer` 这个类，两条 override 都指向它，
//      见映射表 §②：`Icon` 档也是 `RandomCardDrawer`）。⚠️ 这条**今天没有可观测差异**的判定手段
//      （`icon` 变体的判别在 `PickDrawer` 之后丢了）⇒ 我们按「基础档」画（有阵营名），如实记在报告里。
//
//  ── 数据从哪来 ───────────────────────────────────────────────────────────────
//    `item.cardRarity` → `ItemSpec.Rarity`（`Spec` 只给野牌填过；`DropTableItem` 的 `cardRarity` 本地 SO
//    只在 `DT Ultramarines All` 两份里（`IdTypes`），**我们还没抄那两格**）· `item.cardArmy` → `ItemSpec.CardArmy`
//    ⇒ 今天默认 `0/0` = 「无稀有度 + 中立」那一格（原版同样会画 Base Background + Random Image）。
//    `raritySprites` = prefab 的 4 条 PPtr（`m_FileID 2`）：本地实读到的同族四张 =
//      `40k_Rewards_icon_base card_{common,rare,epic,legendary}`（`bundle_atlasindividual_assets_0_mainmenu/Sprite`，
//      2026-10-30 现读）；`Background` 那颗的出厂 sprite 正是 `…_common`（= rarity 1 那一档）⇒ 两边互证。
//
//  ── 版面（prefab 实读 · `Random Card Drawer`）────────────────────────────────
//    根 712.8×1080 · `Content` 712×1080 ·
//    `Base Background` 铺满 (0.4,0)-(712.4,1080) · `Background` (0.4,0)-(712.4,**1134**)（比 Content 高 54）·
//    `Image` 与 `Random Image` **同矩形** (0.4,108)-(712.4,648) · `Army` (0.4,702)-(712.4,810) ·
//    `Label`（`Quantity` + `Name`，fs 75/auto[12~75]）
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `RandomCardDrawer : ItemDrawer&lt;DropTableItem&gt;`。</summary>
        public const string DrawerRandomCard = "RandomCardDrawer";

        /// <summary>`Base Background` 那一层（rarity == 0 才显示；与共享底图同一张）。</summary>
        const string RandomBaseSprite = "40k_Rewards_icon_base_card";
        /// <summary>`randomArmyIcon` 那颗的 sprite（prefab 出厂值 `UI_Button_Round_background`）。</summary>
        const string RandomArmyIconSprite = "UI_Button_Round_background";
        /// <summary>`raritySprites[1..4]` 的后缀（见文件头「数据从哪来」）。</summary>
        static readonly string[] RandomRarity = { "common", "rare", "epic", "legendary" };

        /// <summary>`cardRarity` → 那张底图的图名；越界 ⇒ `null`（原版这里会**抛 `List.get_Item` 越界**，
        /// 我们出声 + 不画，⛔ 不拿别的档顶）。</summary>
        static string RandomRaritySprite(int rarity)
        {
            if (rarity < 1 || rarity > RandomRarity.Length) return null;
            return "40k_Rewards_icon_base_card_" + RandomRarity[rarity - 1];
        }

        /// <summary>原版 `RandomCardDrawer.Draw`（五段，双轴正交 —— 见文件头）。</summary>
        static void RandomCard(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                               ref ItemDrawResult res)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            int rarity = item.Rarity;      // = `item.cardRarity`
            int army = item.CardArmy;      // = `item.cardArmy`（0 = Neutral）

            // ① / ② 底图两层（互斥）
            if (rarity == 0)
            {
                var baseTex = Tex(RandomBaseSprite);
                if (!DrawerImage(content, fr.Map(DrCx1, 0f, DrCx2, DrRootH), baseTex, RandomBaseSprite, st,
                                 ref res, "Base Background"))
                    Note("randomcard|base", "随机卡抽屉的 `Base Background` 图 `" + RandomBaseSprite
                         + "` 取不到 ⇒ 这一层不画（⛔ 不拿别的图顶替）");
            }
            else
            {
                var bgTex = Tex(RandomRaritySprite(rarity));
                if (!DrawerImage(content, fr.Map(DrCx1, 0f, DrCx2, 1134f), bgTex, RandomRaritySprite(rarity), st,
                                 ref res, NodeBackground))
                    Note("randomcard|rarity|" + rarity,
                         "随机卡抽屉的 `cardRarity = " + rarity + "` 没有对应底图"
                         + "（`raritySprites` 只有 1..4 → `" + string.Join("/", RandomRarity) + "`；"
                         + "原版这一支会 `List.get_Item` 越界抛异常）⇒ 这一层不画");
            }

            // ③ / ④ 右半边（互斥）：中立 ⇒ 圆底（+ 问号子件）；否则 ⇒ 阵营徽记
            var slotR = fr.Map(DrCx1, 108f, DrCx2, 648f);
            if (army == 0)
            {
                var round = Tex(RandomArmyIconSprite);
                if (round != null)
                {
                    var randomImage = MenuDraw.Rect(content, round, slotR, "Random Image", st.QArt, null, true,
                                                    st.Clip, st.ClipSoftness);
                    // ③b `Content/Random Image/Icon`（🆕 2026-10-31 · `B109_E18`）：原版这颗**恒画**，但它的父件
                    //   `Random Image` 出厂 `m_IsActive = 0`、由 `RandomCardDrawer.Draw` 第 ③ 段
                    //   `randomArmyIcon.SetActive(item.cardArmy == CardArmy.Neutral)` 打开 ⇒ **只有中立阵营才看得见**
                    //   （`decomp…/RandomCardDrawer.cs:24-30`）。我们这一支本来就只在 `army == 0` 里 ⇒ 条件等价。
                    //   ⚠️ 父件被整块裁掉（`MenuDraw.Rect` 返 null）时子件也不建 —— 与原版「父件不画、子件跟着不画」同义。
                    if (randomImage != null) DrawRandomIcon(randomImage.transform, fr, st);
                }
                else
                    Note("randomcard|round", "`Random Image` 的图 `" + RandomArmyIconSprite + "` 取不到 ⇒ 中立那一格空着");
            }
            else
            {
                var aTex = ArmyIcons.Texture(army);
                if (!DrawerImage(content, slotR, aTex, ArmyIcons.IconName(army), st, ref res))
                    Note("randomcard|army|" + army,
                         "随机卡抽屉的阵营 " + army + " 没有徽记图（`ArmyIcons.Texture`）⇒ 原版 `GetArmyIcon` 返 null"
                         + "、那层 `image` 关着，我们也不画");
            }

            // ⑤ armyText（恒执行；`Neutral` ⇒ 原版 `CardArmyToString(0)` 是空串 ⇒ 不画）
            DrawerArmyName(content, fr, army, st);

            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: true);
        }

        // ============================================================ `Content/Random Image/Icon`（🆕 2026-10-31 · `B109_E18`）
        //
        //  原版判据（逐条，判据全文 → `资料/完全复刻/判据/B109_R9_资源侧剩余.md` §1）：
        //    · `Random Card Drawer.prefab` → `Content/Random Image`（INACT）→ 子件 `Icon`
        //      （`Image` · Simple · `m_PreserveAspect 1` · `m_Color` 白 · `m_RaycastTarget 1`）
        //    · `Icon` 的 `m_Sprite` = **`40k_Rewards_icon__`**（62×117 图集切片，guid `95c82c9c484c78e42a314def29a0a2e2`）
        //    · ⛔ **不是** `cardIcon` 字段那张（`40k_general_wildcard_common_small`）—— 原版 `Draw` 不读它。
        //    · ⛔ **不是** `Random Image` 自己那张（`UI_Button_Round_background`，那是父件）。
        //
        //  ⚠️ 为什么新增的成员全在这里：本仓纪律「新成员追加到类尾」（插在中间会让下游行号整体漂移）。

        /// <summary>`Content/Random Image/Icon` 那颗问号图（prefab 实读，见上面那一节）。</summary>
        const string RandomIconSprite = "40k_Rewards_icon__";

        /// <summary>`Content/Random Image/Icon` 的设计 px 矩形（左 / 上 / 右 / 下，左上原点 · y 向下）。
        /// <para>判据 = `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Random Card Drawer" --depth 6`：
        /// `Random Image` 框 (0.4,108)-(712.4,648) = 712×540；`Icon` 锚点 (0.2,0.2)-(0.8,0.8) 内接其中 ⇒
        /// 0.4+0.2×712 = **142.8** · 0.4+0.8×712 = **570.0** · 108+0.2×540 = **216** · 108+0.8×540 = **540**
        /// （`menu_dump` 同一跑印的绝对矩形正是 `142.8,216 - 570.0,540`）。</para>
        /// <para>⚠️ 判据文档 §1 那四个数写作「x 142.4→569.6 · y 540→864」：前半按「父框 x 从 0 起」算，差 0.4 px；
        /// 后半是 **y 向上**的锚点值。本实现取 `menu_dump` 的实测绝对矩形（本仓 `PxRect` 是 y 向下），
        /// 断言的期望值也按这一份 —— 两处都以 `menu_dump` 为准。</para></summary>
        const float RandomIconX1 = 142.8f, RandomIconY1 = 216f, RandomIconX2 = 570.0f, RandomIconY2 = 540f;

        /// <summary>画 `Content/Random Image/Icon`（原版那颗 `randomArmyIcon` 的**子件**）。
        /// <para>调用前提：`item.cardArmy == 0`（中立）—— 见 <see cref="RandomCard"/> 第 ③ 段。</para>
        /// <para>图在 `Resources/` 里取不到 ⇒ 出声一次、**不画**（⛔ 不拿别的图顶替；`CLAUDE.md` §三「不许静默失败」）。</para>
        /// <para>队列：与原版「父件圆底 + 子件问号」不设材质差这一点一致，照本仓既有先例
        /// （`Shell/BoosterInfoPopup.cs` 的「圆底盘 + 子件 `Background`/`Icon`」）**父子同 `QArt`** ——
        /// 子件后建、同队列内后画。（⛔ 不用 z 分层：`CLAUDE.md` §三「分层要用渲染队列，不能用 z」。）</para></summary>
        static void DrawRandomIcon(Transform parent, DrawerFrame fr, ItemDrawerStyle st)
        {
            var tex = Tex(RandomIconSprite);
            if (tex == null)
            {
                Note("randomcard|icon", "随机卡抽屉的 `Content/Random Image/Icon` 图 `" + RandomIconSprite
                     + "` 在 `Resources/` 里取不到 ⇒ 这一层不画"
                     + "（导入口 = `工具/import_original_art.py` 的 `MENU_IMAGES` 那一行）");
                return;
            }
            MenuDraw.Rect(parent, tex, fr.Map(RandomIconX1, RandomIconY1, RandomIconX2, RandomIconY2),
                          "Icon", st.QArt, null, true, st.Clip, st.ClipSoftness);
        }
    }
}
