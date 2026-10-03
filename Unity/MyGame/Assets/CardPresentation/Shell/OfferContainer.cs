// OfferContainer.cs — 「商品条目容器」族（原版 `General Basic Offer Container *`，**19 个 prefab**）的
//                    **共用骨架构造器 + 19 条变体表**
//
// ============================ 出处（唯一正本） ============================
//  · 正本：`资料/阶段二_商店_原版规格.md` **§五·一**（19 件清单 / 11 种抽屉 / 三条硬限制 / 两个别照抄的坑）
//    ⚠️ 那份正本里的「三条硬限制」**第②条（什么时候挂哪个抽屉）2026-10-04（A43）已解出** ——
//       材料在下面「A43」那一段；**正本本身还没改**（不是本批白名单，由调度台派活）。
//  · 逐份实测（**本文件那张表就是它跑出来的**，2026-10-03 实跑 19 次）：
//      python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 3 --relative
//      （`--depth 6` 再跑一遍看 `name-bg` / `Price Display Button` / `WebShop Button Square Variant` 的子树）
//      坐标一律是**相对根左上角**（左上原点 · y 向下）
//  · 抽屉库与「选哪个抽屉」：`资料/普查产出_1003/ItemDrawer_抽屉系统.md` + `Shell/ItemDrawer.cs`
//    —— **判据只此一份**，本文件只**转调** `ItemDrawer.Draw(...)`，不另写一套
//  · 原版谁在用这一族（反编译）：`d:/2/tools/decomp_full/ContainerService__OpenOfferContainer.c` +
//    `Everguild.LiveOps.ContainerOfferEvent.*` ⇒ **服务端驱动的 LiveOps 报价**
//  · 抽屉那一侧的字段/类名：`资料/普查产出_1003/ItemDrawer_抽屉系统.md` §二（23 个 `ItemDrawer<T>` 子类）
//
// ============================ 🔴 两条硬限制（+ 一条**已解出**）—— 决定了「只能建骨架」 ============================
//  ① **母版↔Variant 的派生关系查不到** —— 真包里 `PrefabInstance` 对象数 = 0、GameObject typetree 只剩
//     `m_Component/m_Layer/m_Name/m_Tag/m_IsActive` 5 键（player build 已剥掉 prefab 实例信息），
//     解包目录也没有 `Prefab/` 类目录 ⇒ **没有裸的母版文件**（`GameObject/` 与真包里都没有）。
//  ② ✅ **「什么时候挂哪个抽屉」2026-10-04 已解出（A43）** —— 不再是一条限制，见下面「A43」那一段。
//     🔴 **更正痕迹（铁律 5，两次）**：这一条最早写的是「`ShopOfferContainer.GeneralOfferPopupDrawer.DrawRewards`
//     的**方法体是空 stub**」—— **那句话是假的**（那是个 **14,920 字节的完整方法体**，含三个 LINQ lambda +
//     `Enumerable.First` + `List.RemoveAt` + `ComponentReference.Release`；**空 stub 不会有 lambda**。
//     对照：真小的 `ItemDrawer__Draw.c` 才 1,659 字节）；2026-10-04（W1）改成「**没解出来**，是待做的活」；
//     **同一天（A43）就解出来了** ⇒ 现在写「**解出来了**」，判据逐条在 A43 那一段里。
//     ⚠️ 「空 stub」这句**不是本文件首创**：正本 §五·一 与 `资料/普查产出_1003/ItemDrawer_抽屉系统.md` §六
//     早就这么写着 —— **那两处也该订正**（材料已在，由调度台派活）。
//  ③ **抽屉美术全是运行期赋值** —— `Content→Image` 一律 `<无图>`，sprite 名查不到；
//     可复用的固定件只有 `40k_main_bt_nametag` · `40k_Profile_display_title` · `40K_general_icon_lock` ·
//     `40k_campaign_Premium-icon` · `Player Profile Border`。
//     ⚠️ 这一条只覆盖**抽屉内部**；骨架自己那几件（`Highlight` / `Icon`）**是有 sprite 名的**，
//     见下面骨架行的 🆕 标注（2026-10-04 A34-F3 订正：它们原来被误记成「未解出 ⇒ 判据空 ⇒ 不建」）。
//
// ============================ 🔴 我们这条链【没有数据源】 ============================
// 原版这 19 件是**服务端 LiveOps 报价**的载体（`ContainerService.OpenOfferContainer` 那一族），
// 本地**一个报价数据都没有**（15 个 LiveOps SO 全是过期活动）。
// ⇒ 本文件**只建「构造器 + 19 条变体表」**，⛔ **不编假入口**（不挂到任何菜单上、不假装点了会开什么）。
//    `Build(...)` 的 `onClick` 由调用方给；**给 null 就如实出声**（红线：不许静默失败）。
//
// ============================ 骨架（19 份**逐字段同构**，实测） ============================
//   <根 · 名字 = prefab 名 · 尺寸见变体表>
//     ├─ `raycast target`              整根那么大（原版是 `Image,EverguildButton` · `trans=1` ⇒ 整卡可点）
//     ├─ `background`                  整根 · `Image.m_Enabled = 0`（**19 份里只有 `Small` 那件是 1**）
//     │    ├─ `foreground`             整根 · `Image.m_Enabled = 0`（19 份全是 0）
//     │    ├─ `Dynamic Content`        **锚框 100×100**（矩形逐份见变体表）· 无脚本无 Graphic
//     │    │    └─ <抽屉槽 × N>         **只有这里逐份不同**（名字 = 原版抽屉节点名，见变体表）
//     │    └─ `name-bg`                `Image` 无图 · `Simple` · `m_Color = (0,0,0,0.431)`
//     │         ├─ `name`              TMP · 逐代字号见下
//     │         ├─ `type`              TMP · 逐代字号见下
//     │         ├─ `Price Display Button`      （脚本 `PriceDisplayButton`）
//     │         │    └─ `Generic UI Button`    `40K_button` · `Simple` + `preserveAspect` · `m_Color = (0.902,0.637,0.18,1)`
//     │         │         └─ `Price Display` （`HorizontalLayoutGroup`）→ `text`
//     │         ├─ `WebShop Button Square Variant`  （`HL=40K_button_square_hover` · `P=40K_button_square_pressed`）
//     │         │    ├─ `Highlight`      **`OctagonUI Filled Fade SDF`** 128² · 九宫 52 · `Sliced` · α**0.8** · `ppuMul 2`
//     │         │    │                   🆕 2026-10-04（A34-F3）：原来这里写「sprite 未解出 ⇒ 不建」，**是假的**
//     │         │    ├─ `Button Image`   `40K_button_square` 103×107 · `Sliced` · `ppuMul = 2.5`
//     │         │    ├─ `Icon`           **`40K_Icon_Discount_Gold`** 128² · `Simple` · **`scl 1.2`**
//     │         │    │                   🆕 2026-10-04（A34-F3）：同上，「未解出」是假记录
//     │         │    └─ （**没有** INACT 子件 —— 那颗出厂 INACT 的 `Image` 是 `name-bg` 的孩子，见下）
//     │         ├─ `Image`             🔴 出厂 **INACT**、无图 · **父节点是 `name-bg`**（不是 `WebShop`！）
//     │         │                       —— 2026-10-04 **A34-F3 订正**：原来建成了 `WebShop` 的孩子。
//     │         │                       判据 = 原始 `m_Children` 父链 + `menu_dump --depth 5` 的缩进层（19 份一致）
//     │         └─ `Available Counter`  TMP · 出厂 `'Available: 1/5'`
//     │                                 （`name-bg` 孩子序 = `name` / `type` / `Price` / `WebShop` / **`Image`** / `Available`）
//     ├─ `Badge`                       （0,10 → 根宽,75.8）· `WF_Special offer_Value` 324×87 **九宫 162,0,162,0** ·
//     │                                 `Sliced` · `m_Color = (0.651,0,0,1)`
//     │    └─ `Text (TMP)`             出厂 `'+60% value'` fs36 · 白 · `Left/Top` · 折行=1
//     └─ `Timer`                       （10,71 → 231.6,96 · 逐代同矩形）
//          ├─ `Timer Text`             出厂 `'5d 20h 15m'` · `Left/Midline` —— 🔴 **字号逐份**（见下）
//          └─ `Icon`                   `WF_icon_clock` 64×64 · `Simple` + `preserveAspect`
//
//  🔴 **`Title Drawer Horizontal Variant (1)` 不是 `Dynamic Content` 的孩子**（2026-10-04 A34-F1 订正）：
//     19 份里**只有** `…Variant Booster_title_resource` 这一份**多一个**直接挂在 `background` 下的抽屉槽，
//     且**排在 `name-bg` 之后**（原版 `background` 孩子序 = `foreground` / `Dynamic Content` / `name-bg` / **它**）。
//     ⇒ 全表「`Dynamic Content` 下的槽」合计 **87**（不是 88）；那一格由 `Variant.BgDrawers` 单独承载。
//     判据 = `python 工具/menu_dump.py bundle_menus_assets_all "General Basic Offer Container Variant Booster_title_resource" --depth 2 --relative`
//     （它的缩进是 **2** = `background` 的孩子；其余 18 份的 `background` 只有 3 个孩子）。
//
//  文字那几件的 **auto / 折行** 是**逐个节点实读**的，别一律开（见 `LabelFit`）：
//    `name` `auto[10,42]` 不折行 · `type` `auto[10,字号]` 不折行 · `Available Counter` `auto[10,字号]` 不折行 ·
//    `Price…/text` `auto[13.46,40]` 不折行 · **`Badge/Text (TMP)` fs36 钉死（无 auto）+ 折行** ·
//    **`Timer Text` `auto[最小,最大]` + 折行**（🔴 三档都**逐份**，见下）。横对齐**除价签那个 `text`（`Center/Capline`）外全是 `Left`**。
//
//  🔴 **`Timer Text` 的字号/自适应范围【逐份】**（2026-10-04 A34-F2 订正；铁律 5·c「一个值 ≠ 全部情况」）：
//    · `TypeFs = 30` 的 **10 份** ⇒ `字号 28` `auto[18, 28]`
//    · `TypeFs = 34` 的 **9 份** ⇒ `字号 30.6` `auto[10, 32]`   ⚠️ **上限 32 ≠ 字号 30.6**，别写成同一个数
//    ⇒ 本文件原来对着全部 19 份恒传 `28f, 18f` ⇒ **9/19 变体字号偏小**。现在走 `TimerTextFit(TypeFs, …)`
//      （那张实测表写成**函数**，值不在两支里会**出声**，见它的注释）。
//    判据 = 19 份逐份 `menu_dump --depth 3 --relative`（**没有第三档**，10 + 9 = 19 逐份核过）。
//
// ============================ 三个「几何档」（`name-bg` 那几件的矩形/字号按代分） ============================
//  🔴 **19 份按 `name-bg` 那一窝分成三代**（都是实读，见 `Geo391 / Geo778 / GeoSmall` 三个常量的注释）：
//    · `Geo391`  = **391×930**（3 份：`Booster_CardOrAltArt` 那三家）· `name` fs**42** · `type`/`Available` fs**34**
//    · `Geo778`  = **339×778**（15 份）· `name` fs**36.7** · `type`/`Available` 有 fs**30** 与 fs**34** 两支
//                  （**同一份里 `type` 与 `Available Counter` 的字号恒等** —— 15 份逐份核过）
//    · `GeoSmall`= **339×390**（1 份：`Small …`）· `name` fs36.7 · `type`/`Available` fs34
//  ⇒ `type`/`Available` 的字号放在 `Variant.TypeFs` 上（它是**逐份**的，不是逐代的）。
//  ⚠️ **`name` 恒 `auto[10,42]` · `字距 −3` · 白**；`type` 恒 `auto[10,最大=字号]` · `m_Color (0.717,0.717,0.717,1)`。
//
// ============================ 🔴 两个「别照抄的坑」（正本 §五·一 末） ============================
//  ① `Small`(339×390) 的抽屉**仍写着 778 档的坐标**（例 `Icon Container Drawer Variant` 的 y1 = −31.7，
//     顶端越出卡顶 31.7px）——**用途未判** ⇒ 本文件**不照抄任何抽屉矩形**（见下一条）。
//  ② 带 `AspectRatioFitter` 的抽屉量出来的矩形**会退化**（实测：`Icon Premium Campaign Drawer Variant`
//     高 **9797.25** / y −4412.3→5384.9 · `scl = 0.04925`；`Icon Expansion Pass Premium Drawer Variant (1)`
//     宽 **0.00**）——那是**未展开的模板位**（列表布局没跑）。
//  🔴 **2026-10-04（W1-3）把这条纪律写全**：② 的**适用范围不止抽屉** —— 凡是**挂着 `AspectRatioFitter`
//     的节点**，dump 给的矩形都**不是运行时值**（我们的 `工具/menu_dump.py` **只跑 `LayoutGroup`、
//     不模拟 `AspectRatioFitter`**）⇒ **一律别照抄它的矩形，要按那个组件的语义自己算**。
//     ⚠️ 本文件原来只对**抽屉**用了这条纪律，对 `WebShop Button Square Variant/Icon` **没用** ⇒
//     那个图标画宽了 21%~28%（见 `BuildWebShop` 里那一段注释）。**这次的活例**：dump 的 57.78 是**序列化宽**，
//     真值 = 高 ⇒ 正方形（铁律 5·c「一个值 ≠ 全部情况」）。
//  ⇒ **做法**：抽屉槽一律摆在 **`Dynamic Content` 那个锚框（100×100）上**，并且**一律走
//     `ItemDrawer.Draw(slot, box, item, qty, override, style)`** —— 理由**只有**上面那两条坑
//     （抽屉自己的矩形是**未展开的模板位** / 挂 `AspectRatioFitter` 的**会退化**），
//     ⛔ **不是**「本地读不到」。
//     🔴 **2026-10-04 订正（铁律 5 —— 本行原来写「`ItemDrawerConfig` SO 没解包，见 `ItemDrawer.cs` 文件头
//     『读不到的两样』」）**：那句话**两半都是假的**（同一份文件头往上 10 行就是「那两个 `DAT_` 常量**已解出**」，
//     两份说法打架）：
//       · **映射表已解出** —— `资料/普查产出_1004/ItemDrawerConfig_映射表.md`（脚本 `工具/read_itemdrawerconfig.py`，
//         20 条 + 19 档 override + 36 个 GUID，零条靠猜）；
//       · **抽屉 prefab 本身也 dump 得出来** —— `python 工具/menu_dump.py bundle_menus_assets_all "Deck Drawer"`
//         （`"Wildcard Drawer"` 同）能把**整棵子树**摊开：矩形 / `act` / 组件类名 / sprite 名全在。
//     ⇒ 「我们挑的」这个**前提倒了**（「我们还在挑」这个**事实**没倒）⇒ **照 dump 出来的抽屉几何改版式**
//     是一件**还没做的活**（`Shell/ItemDrawer.cs` 文件头已同步订正，件本身记在报告/正本里，⛔ 不是本批）。
//     ⚠️ 挂在 `background` 下的那一格（`BgDrawers`）也走**同一条规则**（同样摆在 100×100 锚框上、
//     同样转调 `ItemDrawer.Draw`）—— 它自己的矩形（211.33×76.05）**没照抄**，理由是上面这两条坑对它同样成立。
//
// ============================ ✅ A43：**哪个槽被填** —— 照原版（2026-10-04 解出） ============================
//  原版那个决定器是 `GeneralOfferPopupDrawer.DrawRewards`（**完整方法体**，见硬限制②的更正痕迹）。四步：
//    ① **建池**：`GetComponentsInChildren<ItemDrawer>(true)`（`:87-88`；**宿主 = popup 那个组件自己**
//       （`param_1`）⇒ 这一遍的作用域是**整棵 popup**、**先序 = 我们要建的树的先序**）
//       ⇒ 同一次循环里逐个 **`SetActive(false)`**（`:112` —— **池里每一个都先关**）
//       后按**它自己的 `GetType()`** 塞进 `Dictionary<Type, List<…>>`（`:113-153`）；
//       🔴 **有一个改名特例**：池键若 == `DeckAndCardbackDrawer` 就当作 **`DeckDrawer`**（`:113-126`）。
//       那两个 `DAT_` 常量**已解出**（`d:/2/tools/il2cpp_out/script.json:1965803` / `:1965838` 的
//       `DeckAndCardbackDrawer_var` / `DeckDrawer_var`，`Signature` = `Il2CppType*`）—— 值就是这两个类名。
//       ✅ **「池里每一个先关」这一下我们做了**（2026-10-04，收 R-X1 的 F3）：`Build` 的 `fill:true` 那一支
//       **建每个槽时就先关**（`SetSlotActive`），命中的那个再被 `:363` 打开
//       ⇒ **出厂 ACTIVE 但没命中的槽不会再留开着**（`…Single Item Type` 那 12 槽里 4 个 ACTIVE 的场合就是它）。
//    ② 每个奖励项：`ItemDrawer.GetDrawerConfig(item.GetType(), **0x1e**)`（`:319`，`0x1e = 30 = DrawerOverride.OfferPopups`）；
//       `drawer == null` ⇒ **`continue`（这一项什么都不画，也不占槽）**（`:331-332`）。
//    ③ 用 **`cfg.drawer.GetType()`**（= 那张表指向的 **prefab 上抽屉组件的类**）去池里找**第一条**满足
//       `ReflectionHelper.Is(池键, 那个类)` 且列表非空的池（`GeneralOfferPopupDrawer…b__3.c:21`）。
//       🔴 `ReflectionHelper.Is(o, t)` = **`t.IsAssignableFrom(o)`** = 「**o 是 t**」（`ReflectionHelper__Is.c`
//       的实体：`param_2.IsAssignableFrom(param_1)`，两处调用都是「第一参数 **is-a** 第二参数」）
//       ⇒ **槽的类可以是 cfg 那个类的【子类】** —— 这不是多余的一条：19 份里 `Deck Drawer` 槽挂的是
//       **`DeckAndCardbackDrawer`**（`DeckAndCardbackDrawer : DeckDrawer`），而 `PrebuiltDeck` 那条 cfg 指向的
//       `Deck Drawer` prefab 挂的是 **`DeckDrawer`** ⇒ **靠 is-a 才配得上**（严格相等会**一个都配不上**）。
//    ④ `pool[0]`（**池内先序第一个**）→ **抽象的 `Draw`**（虚表槽 `+0x188` = 签名桩
//       `ItemDrawer<T>.Draw(ObtainableItem item, int amount = 1, ItemDrawerOptions options = null)`）——
//       **第二实参是硬编码立即数 `1`**（`:357`；`DrawItemReward.c` 那一处同形）→ `SetActive(true)`（`:363`）
//       → **`RemoveAt(0)`**（`:364`）⇒ **同类奖励第 n 个落第 n 个同类槽**（`PickSlot` 的 `ordinal` 就是它）；
//       最后 `Release()`（`:366-372`）。
//       🔴 **本行原来写的是「`Setup(item,1,…)`」—— 两处都不对**（2026-10-04 R-X1 查出、就地订正）：
//       ① 那一步调的是**抽象的 `Draw`**，签名桩里**没有 `Setup`**（`ItemDrawer.cs` 只有 `Draw`）；
//       ② 我们原来把 `Content.Quantity`（默认 1）传进去、原版是**写死的 1** ⇒ 现已照原版改成字面量 `1`
//          （`FillSlot`），那个没有读者的 `Content.Quantity` 字段**同时删掉**（留着就是「设了不生效」的静默陷阱）。
//    ⚠️ **本族现在仍是「一次只装一项」**（`Build` 只填 `ordinal = 0` 那**一个**同类槽）：原版是按报价里的
//       **每一个奖励项**逐个走 ②③④（`:296-372` 的外层循环，每项消费池里第一个同类槽、`RemoveAt(0)`）
//       ⇒ **「一次装多个奖励」是一件【还没做】的偏离**（判据就在同一段反编译里；要做，只有先后 —— 铁律 11）。
//  · 选类型那两级（`ItemDrawerConfig__GetReference.c` + `…ItemDrawerReference__GetDrawer.c`）：
//      **精确相等**（`b__0`）→ **`entry.Type` 是 `itemType` 的祖先**（`b__1` 的 `Is(itemType, entryType)`）→
//      都没有 = **空**（`GetReference.c:40-45` 返回 `(0,0)`）；再按 `override` 在 `customDrawerOverrides` 里
//      **按键找**（`:62-72`，`override == 0` 直接用主档），**找不到仍然回落主档**。
//  · 两张表（**逐条有出处，零条靠猜**）：
//      ① **槽名 → 槽身上的抽屉类** = `SlotTypes`（19 份**逐份实读**，链 = 节点组件的 `m_Script` →
//         `MonoScript.m_ClassName`，与 `工具/read_itemdrawerconfig.py` 同一条链）；
//      ② **itemType → 抽屉类** = `ItemTypeSets`（= `ItemDrawerConfig` SO 那张 20 行的表，
//         出处 `资料/普查产出_1004/ItemDrawerConfig_映射表.md`；**`OfferPopups`(30) 全表只有 3 条**，
//         其余 17 个类型在这一档**回落主档**）。
//  ⚠️ **仍然是我们挑的**：`Content.ItemType`（**原版那个类型是服务端 payload 里 `item.GetType()` 来的**，
//     我们这套 `ItemSpec` 里没有 ⇒ 由**调用方**给名字）。给空 ⇒ **不填任何槽 + 出声**（判据空，不静默）。
//     `ItemKind.Wildcard` 例外：它的原版类型**有判据**（= `Wildcard`，见 `TypeOfKind`）。

// ============================ 🔴 我们挑的（原版读不到，逐条出声） ============================
//  · **物品本身**（画什么图 / 有没有图）**不是这里定的** —— 抽屉内部的画法照旧走 `ItemDrawer.Draw`
//    （那一边的替身口径写在 `Shell/ItemDrawer.cs` 文件头）。本文件只管**填哪个槽**（A43）。
//  · **每个变体的抽屉矩形**是我们定的（一律 = `Dynamic Content` 锚框，理由见上一条坑）。
//  · **`name` / `type` / `Price` / `Badge` / `Timer` 的文案与物品**全由调用方给（原版是服务端给的）。
//  · **文字纵排是近似**：原版 `Available Counter` 是 `Left/Bottom`、`Badge/Text (TMP)` 是 `Left/Top`，
//    我们统一按**框内居中**画（全工程口径，同 `BoosterPackOpenWindow` 那条）。字号/颜色/横对齐是原版值。
//  · **`Price Display` 那一段的排版是我们挑的**：原版它是 `HorizontalLayoutGroup`（`icon` + `text`），
//    而 `icon` 的图在 prefab 里是空的（运行期赋币种图标）、`text` 的量出来的宽是 **0**（布局没跑）
//    ⇒ 我们只建 `Price Display` 空节点 + 一个 `text`，按**按钮整框居中**画，不摆 HLG。
//  · **两处音频 / `Highlight` 那颗 `UIBorderGlow` 的「呼吸」** **没做**。
//    🆕 **2026-10-04（A34-F3）订正这一条的措辞**：原来写「**图**与 cue 都查不到」—— **图是查得到的**
//    （`OctagonUI Filled Fade SDF`），现在**静态画**出来了；查不到的只有**两段音频的 cue**。
//    而「呼吸」这一层本身**也不成立**：19 份逐份实读 `UIBorderGlow` 的三个字段 = `animDelay 0` ·
//    `animTime 1` · **`playOnEnable 0`** ⇒ **它根本不会自己动**，出厂就是静帧。
//    ⇒ 静态画在出厂色（α0.8）**就是原版的静止态**，这里没有偏离。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>「商品条目容器」族（原版 `General Basic Offer Container *` **19 个 prefab**）的共用骨架构造器。
    /// <para>🔴 **这一族本期只建构造器 + 变体表 + 断言，没有入口** —— 原版它是**服务端 LiveOps 报价**的载体
    /// （`ContainerService.OpenOfferContainer`），本地没有报价数据源（见文件头）。</para></summary>
    public static class OfferContainer
    {
        // ============================================================ 节点名（照 prefab · 逐字 · 断言按它找）

        public const string NRaycast = "raycast target";
        public const string NBackground = "background";
        public const string NForeground = "foreground";
        public const string NDynamic = "Dynamic Content";
        public const string NNameBg = "name-bg";
        public const string NName = "name";
        public const string NType = "type";
        public const string NPrice = "Price Display Button";
        public const string NPriceBtn = "Generic UI Button";
        public const string NPriceBox = "Price Display";
        public const string NPriceText = "text";
        public const string NWebShop = "WebShop Button Square Variant";
        public const string NWebShopImage = "Button Image";
        /// <summary>🆕 2026-10-04（A34-F3）：`Highlight` 那颗发光圈（`OctagonUI Filled Fade SDF`）。
        /// ⚠️ 它在 `WebShop` 里**排第一**（原版兄弟序 = `Highlight` / `Button Image` / `Icon`）⇒ 画在最底下。</summary>
        public const string NWebShopHighlight = "Highlight";
        /// <summary>🆕 2026-10-04（A34-F3）：折扣金币图标（`40K_Icon_Discount_Gold` · `scl 1.2`）。
        /// 🔴 **它与 `Timer/Icon` 同名**（都叫 `Icon`）⇒ 找它俩要用**父链 / 直接孩子**，别用 `FindChild` 一路搜子树
        /// （本工程踩过「按名字找找到别人家」的坑）。</summary>
        public const string NWebShopIcon = "Icon";
        /// <summary>🔴 出厂 **INACT** 的空图子件 —— **父节点是 `name-bg`**（不是 `WebShop`！）。
        /// 2026-10-04 **A34-F3 订正**：原来把它建成了 `WebShop` 的孩子。
        /// 判据 = 原始 `m_Children` 父链（该 RT `4201396528864545241` 的 `m_Father` 指向 `name-bg` 的 RT
        /// `-7588359647778321959`）+ `menu_dump --depth 5` 的缩进层（19 份一致）。</summary>
        public const string NWebShopGfx = "Image";
        public const string NAvail = "Available Counter";
        public const string NBadge = "Badge";
        public const string NBadgeText = "Text (TMP)";
        public const string NTimer = "Timer";
        public const string NTimerText = "Timer Text";
        public const string NTimerIcon = "Icon";

        /// <summary>`MenuDraw.Hit` 在里面那个 quad 的名字（命中区节点本身叫 `raycast target`）。</summary>
        public const string NHitQuad = "Hit";

        // ============================================================ 固定件的图（判据 = 正本 §五·一 + 实测）

        /// <summary>`Badge` 的图。⚠️ prefab 里那个名字带**空格**（`WF_Special offer_Value`），
        /// 工程里导入成下划线名（`CardArt.MenuUi` 不做「空格 → 下划线」转换）⇒ 代码里写导入后的名字。</summary>
        public const string BadgeArt = "WF_Special_offer_Value";
        /// <summary>`Badge` 的九宫格边界（贴图像素 · L,B,R,T）—— `m_Border` 实读，与 324×87 自洽。
        /// 🔴 **L+R = 324 = 贴图宽**（`Sprite/WF_Special offer_Value.json` 的 `m_Border = (162,0,162,0)`），
        /// 也就是**只有左右两个端帽、没有中段**。两处后果（都实测过，如实记）：
        /// ① 我们这份共用助手 `ImageQuad.CreateNineSlice` 会因为 `uR &lt;= uL` 打一条
        ///    `border 比图还大，退回单块` 的警告 —— **那是误报**（它是「端帽铺满整张图」这种合法形状，不是真超了）；
        /// ② 我们这里**两个端帽各被拉成 169.5px**（`sc = 339÷324 = 0.9688` 后 `wm` 归零），
        ///    而原版 uGUI 的 `GetAdjustedBorders` 只会在 `border.x + border.z **>** rect.width` 时才缩
        ///    ⇒ 原版是 **162 + 15px（中段取同一列纹素）+ 162**。差 7.5px/端帽（≈4.6%），**画面看不出来**。
        ///    ⚠️ 这一条**没改共用助手**（`MenuDraw` / `ImageQuad` 不在本次白名单），只记在这里。</summary>
        public static readonly Vector4 BadgeBorder = new Vector4(162f, 0f, 162f, 0f);
        public const float BadgeTexW = 324f, BadgeTexH = 87f;
        /// <summary>`Badge` 的 `Image.m_Color`（实读）。</summary>
        public static readonly Color BadgeTint = new Color(0.651f, 0f, 0f, 1f);

        /// <summary>`Timer/Icon` 的图（与商店页时间条同一张）。原版那张是 `preserveAspect` 的 64²，
        /// 而框只有 30×25 ⇒ 按 `min(框)` 内接，与商店页 `Clock Icon` 同一条口径。</summary>
        public const string ClockArt = "WF_icon_clock";

        /// <summary>`Price Display Button/Generic UI Button` 的图 + 色（实读；与商店格那颗**同一份值**
        /// ⇒ 色直接引 `ShopTabPage.PriceTint`，不抄第二份）。</summary>
        public const string PriceArt = "40K_button";

        /// <summary>`WebShop Button Square Variant/Button Image` 的图 —— 正本 §五·一 的骨架行写的
        /// `40K_button_square 103×107`（贴图本体实读也是 103×107，自洽）。
        /// 🔴 **不在** §五·一 那份「可复用固定件」清单里（那 5 个是别的），是**骨架行**给的。</summary>
        public const string WebShopArt = "40K_button_square";
        /// <summary>`40K_button_square` 的 `m_Border`（L,B,R,T）与本体尺寸 —— `Sprite/40K_button_square.json` 实读
        /// （在 `bundle_duplicateassetisolation_assets_all` 与 `sharedassets0` 里，两处同值）。</summary>
        public static readonly Vector4 WebShopBorder = new Vector4(36f, 28f, 37f, 29f);
        public const float WebShopTexW = 103f, WebShopTexH = 107f;
        /// <summary>`Button Image` 的 `m_PixelsPerUnitMultiplier`（实读 = 2.5）⇒ **画出来的角块**
        /// = `m_Border ÷ 2.5`（判据 → `MenuDraw.Nine` 的 `borderOutPx` 注释）。</summary>
        public const float WebShopPpuMul = 2.5f;

        /// <summary>🆕 **2026-10-04（A34-F3）**：`WebShop Button Square Variant/Highlight` 的图。
        /// 实读 = **`OctagonUI Filled Fade SDF`**（128² · 九宫 **52,52,52,52** · `Sliced` · `m_Color (1,1,1,0.8)` ·
        /// `ppuMul = 2`）。
        /// <para>🔴 **本文件原来写「sprite 未解出 ⇒ 判据空 ⇒ 不建」是【假记录】**（审查代理 2026-10-04 查出）：
        /// 这张图**当时就已经在** `Resources/Art/ui_menu/`（导入名把空格换成下划线）。
        /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "General Basic Offer Container Variant Booster_title_resource" --depth 5 --relative`。</para></summary>
        public const string HighlightArt = "OctagonUI_Filled_Fade_SDF";
        /// <summary>`Highlight` 的 `m_Border`（L,B,R,T · 贴图像素）—— `Sprite/OctagonUI Filled Fade SDF.json` 实读 52,52,52,52。</summary>
        public static readonly Vector4 HighlightBorder = new Vector4(52f, 52f, 52f, 52f);
        public const float HighlightTexW = 128f, HighlightTexH = 128f;
        /// <summary>`Highlight` 的 `m_PixelsPerUnitMultiplier`（实读 = 2.0）⇒ 画出来的角块 = `52 ÷ 2 = 26px`。</summary>
        public const float HighlightPpuMul = 2f;
        /// <summary>`Highlight` 的 `m_Color`（实读 `(1,1,1,0.8)`）。
        /// ⚠️ 它自己在出厂态**不会动**：`UIBorderGlow` 三字段实读 = `animDelay 0` · `animTime 1` · **`playOnEnable 0`**
        /// ⇒ 静态画在 α0.8 **就是原版的静止态**（没有偏离）。</summary>
        public static readonly Color HighlightTint = new Color(1f, 1f, 1f, 0.8f);

        /// <summary>🆕 **2026-10-04（A34-F3）**：`WebShop Button Square Variant/Icon` 的图。
        /// 实读 = **`40K_Icon_Discount_Gold`**（128² · `Simple` · **无 `preserveAspect`** · `m_Color (1,1,1,1)`），
        /// 节点上带 **`m_LocalScale = 1.2`**（⇒ 渲出来比矩形大 1.2 倍，见 <see cref="Geo.WebShopIcon"/> 的注释）。</summary>
        public const string IconArt = "40K_Icon_Discount_Gold";
        /// <summary>那个 `Icon` 节点的 `m_LocalScale`（实读 = 1.2 · 19 份一致）。
        /// ⚠️ 乘的对象是**正方形**（那条 `AspectRatioFitter` ⇒ 宽 := 高），不是 `Geo.WebShopIcon` 的布局矩形
        /// —— 见 <see cref="Geo.WebShopIcon"/> 的注释与 `BuildWebShop` 里那一段。</summary>
        public const float WebShopIconScale = 1.2f;

        /// <summary>`name-bg` 的 `Image.m_Color`（实读，无图 ⇒ 画一块半透明黑板）。</summary>
        public static readonly Color NameBgTint = new Color(0f, 0f, 0f, 0.431f);
        /// <summary>`type` 的 `m_fontColor`（实读；19 份一致）。</summary>
        public static readonly Color TypeColor = new Color(0.717f, 0.717f, 0.717f, 1f);
        /// <summary>`name` 的 `m_fontColor`（实读；19 份一致）。</summary>
        public static readonly Color NameColor = new Color(1f, 1f, 1f, 1f);

        // ============================================================ 出厂文本（实读 · 19 份逐字相同）

        /// <summary>`Badge/Text (TMP)` 的出厂文本（实读）。</summary>
        public const string DefBadgeText = "+60% value";
        /// <summary>`Timer/Timer Text` 的出厂文本（实读）。</summary>
        public const string DefTimerText = "5d 20h 15m";
        /// <summary>`Available Counter` 的出厂文本（实读）。</summary>
        public const string DefAvailText = "Available: 1/5";
        /// <summary>`name` / `type` 的出厂文本（实读；原版运行期由服务端覆盖）。</summary>
        public const string DefNameText = "Legendary Wildcard Bundle ";
        public const string DefTypeText = "Booster Pack";

        // ============================================================ 渲染队列（**我们挑的**：原版这一族没有宿主窗口）

        // 相对基准的偏移：
        // ⚠️ **`raycast target` 是整根那么大**（原版就是），而 `WebShop Button Square Variant` 是它里面的
        //    另一颗钮 ⇒ 后者的命中区**必须排得更靠上**（`QoWebShopHit > QoHit`），否则那一颗**点不动**
        //    （`PointerLayer` 取队列最高的那条；本工程 2026-10-03 在 `BoosterInfoPopup` 上刚栽过一次）。
        // 🔴 **2026-10-04（A34-F3）**：`WebShop` 那一窝现在有**三层画得出来的**（`Highlight` → `Button Image`
        //    → `Icon`），**必须各占一档** —— 原版是靠 `m_Children` 的兄弟序定的（后画的盖前面的），
        //    而我们这套是透明队列 + 逐 quad 一份材质 ⇒ **同档 = 谁压谁不定**（`CLAUDE.md` §三那条）。
        //    兄弟序（原版实读）= `Highlight` / `Button Image` / `Icon` ⇒ 队列也照这个从小到大。
        //    ⚠️ 这三档都**必须大于 `QoText`**（4）：原版 `name-bg` 的孩子序里 `WebShop` 在 `name`/`type`
        //    之后 ⇒ 它那圈发光会**盖在**那两行字上（两者矩形确实相交，实测 y 701–790 vs 682–717）。
        const int QoBg = 0, QoNameBg = 1, QoDrawer = 2, QoBadge = 3, QoText = 4,
                  QoWebShopHi = 6, QoWebShop = 7, QoWebShopIcon = 8,
                  QoHit = 9, QoWebShopHit = 10;

        // ============================================================ 一个「几何档」

        /// <summary>一代骨架的矩形表（`name-bg` 那一窝）。**每一格都是 `menu_dump --relative` 实读值**，
        /// 坐标是**相对根左上角**。三代见下三个常量。</summary>
        public struct Geo
        {
            public float W, H;                  // 根尺寸
            public float NameFs;                // `name` 的字号
            public PxRect NameBg, Name, Type, Price, WebShop, WebShopDummy, Avail,
                          BadgeText, Timer, TimerText, TimerIcon;
            /// <summary>🆕 **2026-10-04（A34-F3）**：`WebShop Button Square Variant/Highlight` 的矩形。
            /// ⚠️ 它**越出 `WebShop` 的框**（左右各 ~21.4、上下各 ~18.2）—— 那是**发光圈**，原版就是这么溢的
            /// （`Geo391` 那份甚至越出根右边界 6.9px）。**别按「父框内」去裁它。**</summary>
            public PxRect WebShopHighlight;
            /// <summary>🆕 **2026-10-04（A34-F3）**：`WebShop Button Square Variant/Icon` 的矩形 ——
            /// **dump 给的布局矩形**（**未乘 `scl 1.2`** · **也未跑 `AspectRatioFitter`**）。
            /// 🔴 **2026-10-04（W1-3）订正用法**：那个节点上挂着 `AspectRatioFitter`
            /// （`m_AspectMode 2 HeightControlsWidth` · `m_AspectRatio 1.0`）⇒ 运行时**宽 := 高**
            /// ⇒ **`W` 不是渲染宽**（按它画会宽多 21%~28%）。**渲染边长取 `H`**（见 `BuildWebShop`）。
            /// ⚠️ `W` 仍有用：**中心**（`CW/CY`，`pivot 0.5` ⇒ 改宽不动中心）。
            /// 另：`m_LocalScale = 1.2`（见 <see cref="WebShopIconScale"/>）⇒ 渲染 = 那个正方形 ×1.2。</summary>
            public PxRect WebShopIcon;
            /// <summary>`background` 的 `Image.m_Enabled`（**19 份里只有 `Small` 那件是 1**）。</summary>
            public bool BgOn;
            /// <summary>`foreground` 的 `Image.m_Enabled`（19 份**全是 0**）。</summary>
            public bool FgOn;
        }

        /// <summary>**391×930** 那一代（3 份：`Booster_CardOrAltArt` 三家）。
        /// `name` fs**42** `auto[10,42]` / `type` fs**34** `auto[10,34]`。</summary>
        public static readonly Geo Geo391 = new Geo
        {
            W = 391f, H = 930f, NameFs = 42f,
            NameBg = new PxRect(2.5f, 756.5f, 388.2f, 931.0f),
            Name = new PxRect(14.1f, 769.8f, 376.6f, 823.8f),
            Type = new PxRect(14.1f, 819.8f, 376.6f, 854.8f),
            Price = new PxRect(11.8f, 860.3f, 274.0f, 922.6f),
            WebShop = new PxRect(286.7f, 859.9f, 376.6f, 922.6f),
            WebShopDummy = new PxRect(298.2f, 861.2f, 368.9f, 911.8f),
            WebShopHighlight = new PxRect(265.4f, 841.7f, 397.9f, 940.8f),
            WebShopIcon = new PxRect(298.3f, 863.7f, 365.0f, 918.8f),
            Avail = new PxRect(12.5f, 710.6f, 320.5f, 751.5f),
            BadgeText = new PxRect(16f, 26f, 380.2f, 59.8f),
            Timer = new PxRect(10f, 71f, 231.6f, 96f),
            TimerText = new PxRect(40f, 69f, 261.6f, 98f),
            TimerIcon = new PxRect(10f, 71f, 40f, 96f),
            BgOn = false, FgOn = false,
        };

        /// <summary>**339×778** 那一代（15 份）。`name` fs**36.7** `auto[10,42]`。
        /// ⚠️ `type`/`Available` 的字号**逐份**（30 或 34）⇒ 在 `Variant.TypeFs` 上。</summary>
        public static readonly Geo Geo778 = new Geo
        {
            W = 339f, H = 778f, NameFs = 36.7f,
            NameBg = new PxRect(2.5f, 632.8f, 336.2f, 779.0f),
            Name = new PxRect(12.5f, 631.9f, 326.2f, 685.9f),
            Type = new PxRect(12.5f, 681.9f, 326.2f, 716.9f),
            Price = new PxRect(10.5f, 719.8f, 237.3f, 772.0f),
            WebShop = new PxRect(248.4f, 719.5f, 326.2f, 772.0f),
            WebShopDummy = new PxRect(258.3f, 720.5f, 319.5f, 762.9f),
            WebShopHighlight = new PxRect(227.0f, 701.2f, 347.5f, 790.2f),
            WebShopIcon = new PxRect(258.4f, 723.2f, 316.2f, 768.2f),
            Avail = new PxRect(12.5f, 586.9f, 320.5f, 627.8f),
            BadgeText = new PxRect(16f, 26f, 328.2f, 59.8f),
            Timer = new PxRect(10f, 71f, 231.6f, 96f),
            TimerText = new PxRect(40f, 69f, 261.6f, 98f),
            TimerIcon = new PxRect(10f, 71f, 40f, 96f),
            BgOn = false, FgOn = false,
        };

        /// <summary>**339×390** 那一代（1 份：`Small … Single Item Type`）。
        /// 🔴 与 `Geo778` 的差别**只有 `name-bg` 那一窝的 y**（往上平移 386.6）+ **`background` 的 `Image` 是 enabled**。
        /// ⚠️ 它的**抽屉**仍写着 778 档坐标（`Icon Container Drawer Variant` 的 y1 = **−31.7**，越出卡顶）
        /// —— 用途未判，我们照旧**不照抄抽屉矩形**（文件头那条坑）。</summary>
        public static readonly Geo GeoSmall = new Geo
        {
            W = 339f, H = 390f, NameFs = 36.7f,
            NameBg = new PxRect(2.5f, 246.2f, 336.2f, 391.0f),
            Name = new PxRect(12.5f, 244.6f, 326.2f, 298.6f),
            Type = new PxRect(12.5f, 294.6f, 326.2f, 329.6f),
            Price = new PxRect(10.5f, 332.4f, 237.3f, 384.1f),
            WebShop = new PxRect(248.4f, 332.1f, 326.2f, 384.1f),
            WebShopDummy = new PxRect(258.3f, 333.1f, 319.5f, 375.1f),
            WebShopHighlight = new PxRect(227.0f, 313.9f, 347.5f, 402.3f),
            WebShopIcon = new PxRect(258.4f, 335.9f, 316.2f, 380.3f),
            Avail = new PxRect(12.5f, 200.4f, 320.5f, 241.2f),
            BadgeText = new PxRect(16f, 26f, 328.2f, 59.8f),
            Timer = new PxRect(10f, 71f, 231.6f, 96f),
            TimerText = new PxRect(40f, 69f, 261.6f, 98f),
            TimerIcon = new PxRect(10f, 71f, 40f, 96f),
            BgOn = true, FgOn = false,
        };

        // ============================================================ 19 条变体表

        /// <summary>一个变体。**四个字段的出处逐条写在 <see cref="Variants"/> 那张表上**。</summary>
        public struct Variant
        {
            /// <summary>**原 prefab 名**（= 我们建出来的根节点名，逐字照抄）。</summary>
            public string Prefab;
            /// <summary>几何档（见 `Geo391 / Geo778 / GeoSmall`）。</summary>
            public Geo G;
            /// <summary>`type` / `Available Counter` 的字号（**逐份**：30 或 34；同一份里两者恒等）。
            /// `name` 的字号在 `G.NameFs`（逐代）。</summary>
            public float TypeFs;
            /// <summary>`Dynamic Content` 锚框 —— **相对根左上角**，19 份**恒为 100×100**，只有 y 不同。</summary>
            public PxRect Dyn;
            /// <summary>`Dynamic Content` 下挂的抽屉节点名（**兄弟序**，与 prefab 逐字，含 `" (1)"` 这类重名后缀）。</summary>
            public string[] Drawers;
            /// <summary>其中**出厂 INACT** 的那几个（`null` = 一个都没有）。建成但 `SetActive(false)`。</summary>
            public string[] Off;
            /// <summary>🆕 **2026-10-04（A34-F1）**：**直接挂在 `background` 下**的抽屉槽（兄弟序里排在 `name-bg` 之后）。
            /// 19 份里**只有 `…Variant Booster_title_resource` 这一份有**（1 格 = `Title Drawer Horizontal Variant (1)`）；
            /// 其余 18 份是 `null`。⚠️ **它不算在 `Drawers` 里**，也不算进「`Dynamic Content` 下的槽」那个总数。
            /// 判据 = 原始 `m_Children` 父链（`menu_dump` 的缩进层是 2 = `background` 的孩子）。</summary>
            public string[] BgDrawers;

            public Variant(string prefab, Geo g, float typeFs, PxRect dyn, string[] drawers, string[] off)
            { Prefab = prefab; G = g; TypeFs = typeFs; Dyn = dyn; Drawers = drawers; Off = off; BgDrawers = null; }
        }

        /// <summary>🔴 **`Timer Text` 的字号 / 自适应范围是【逐份】的**（2026-10-04 A34-F2；铁律 5·c）。
        /// 实测（19 份逐份 `menu_dump --depth 3 --relative`，**没有第三档**）：
        /// <code>TypeFs = 30 的 10 份 ⇒ 字号 28.0   auto[18, 28]
        /// TypeFs = 34 的 9 份 ⇒ 字号 30.6   auto[10, 32]   ⚠️ 上限 32 ≠ 字号 30.6</code>
        /// ⇒ 本文件原来对全部 19 份恒传 `28f, 18f`、且把 `fontPx` 当上限 ⇒ **9/19 变体字号偏小**。
        /// <para>⚠️ 这三档**不是从 `TypeFs` 推出来的**，是**逐份读出来的**；这里只是把那张实测表写成了函数。
        /// 传进来的 `TypeFs` 不在这两支里 ⇒ **出声**（不许静默用一个默认值兜过去，红线）。</para></summary>
        public static void TimerTextFit(float typeFs, out float fontPx, out float minPx, out float maxPx)
        {
            if (Mathf.Abs(typeFs - 30f) < 0.01f) { fontPx = 28f; minPx = 18f; maxPx = 28f; return; }
            if (Mathf.Abs(typeFs - 34f) < 0.01f) { fontPx = 30.6f; minPx = 10f; maxPx = 32f; return; }
            Debug.LogWarning("[OfferContainer] `Timer Text` 的字号档**没有实测值**（`TypeFs = " + typeFs
                             + "` 不在 30/34 这两支里）⇒ 退回 28/18/28 并**在此出声**（不许静默兜底）");
            fontPx = 28f; minPx = 18f; maxPx = 28f;
        }

        /// <summary>根宽 → `Badge` 的矩形（`0,10 → 根宽,75.8`，19 份**只有宽度跟根走**）。</summary>
        public static PxRect BadgeRect(Geo g) { return new PxRect(0f, 10f, g.W, 75.8f); }

        /// <summary>**19 条变体表**。每条上面那行短注释就是**它自己的记录**（根尺寸 · `Dynamic Content` 矩形 ·
        /// 抽屉槽数 · 出厂 INACT 数 · 字号档），**逐条**都来自同一条可复现命令（2026-10-03 逐份跑）：
        /// <code>python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "&lt;下面那条的名字&gt;" --depth 3 --relative</code>
        /// （`--depth 6` 那一遍用来核 `name-bg` / `Price Display Button` / `WebShop Button Square Variant` 的子树。）
        /// `INACT` 标记取自同一份 dump 的 `act` 列。**每一格都不是推的。**
        /// <para>⚠️ **没有裸的「General Basic Offer Container」母版文件**（`GameObject/` 与真包里都没有）——
        /// 正本 §五·一 已经写明，这里再钉一次。</para>
        /// <para>🆕 **2026-10-03 实读补充（正本 §五·一 说「去重后 17 种结构」，按这份实读可以再细分）**：
        /// `Dynamic Content` 的 **y 在 339×778 那一代里还有 4 个值**（358 ×9 份 · 295 ×1 · 280 ×3 · 272 ×2），
        /// 加上 `type`/`Available` 的 **fs30 / fs34 两支** ⇒ 若按「根尺寸 + Dyn 矩形 + 字号档 + 抽屉组合」
        /// 去重，**是 19 组、没有一对完全相同**；正本那个 17 是**只按结构（抽屉组合 + 卡高）**去重的口径。
        /// 两种口径都在，别把其中一个当另一个。</para></summary>
        public static readonly Variant[] Variants =
        {
            // ---- 391×930 那三家（`name` fs42 · `type`/`Available` fs34）----
            // 391×930 · Dyn(145.5,348)→(245.5,448) · **3 槽**（INACT **0**）
            new Variant("General Basic Offer Container Booster_CardOrAltArt", Geo391, 34f,
                        new PxRect(145.5f, 348f, 245.5f, 448f),
                        new[] { "Icon Container Drawer Variant", "Card Alternate Art Drawer", "Card Drawer" }, null),

            // 391×930 · Dyn(145.5,348)→(245.5,448) · **6 槽**（INACT **1** = `Card Drawer`）
            new Variant("General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title", Geo391, 34f,
                        new PxRect(145.5f, 348f, 245.5f, 448f),
                        new[] { "Cardback Drawer", "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Card Alternate Art Drawer", "Card Drawer", "Title Drawer Horizontal Variant (1)" },
                        new[] { "Card Drawer" }),

            // 391×930 · Dyn(145.5,348)→(245.5,448) · **6 槽**（INACT **1** = `Card Drawer`）
            new Variant("General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle", Geo391, 34f,
                        new PxRect(145.5f, 348f, 245.5f, 448f),
                        new[] { "Cardback Drawer", "Icon Container Drawer Variant", "Card Alternate Art Drawer",
                                "Card Drawer", "Title Drawer Horizontal Variant (1)", "Icon Avatar Drawer Variant" },
                        new[] { "Card Drawer" }),

            // ---- 339×778 那十五家 ----
            // 339×778 · Dyn(119.5,358)→(219.5,458) · **2 槽**（INACT 0）· `type`/`Available` fs**30**
            new Variant("General Basic Offer Container Variant 2 Currencies", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Currency Drawer Variant", "Icon Currency Drawer Variant (1)" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster + 2 Currencies", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Icon Currency Drawer Variant (1)",
                                "Icon Currency Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **4 槽**（INACT 0）· fs**30**
            // 🔴 正本 §五·一 拿它当「代表骨架」举例的那一份（也是本工程实拍抽的那份）
            new Variant("General Basic Offer Container Variant Booster_avatar_cardback_title", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer",
                                "Title Drawer Horizontal Variant", "Icon Avatar Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster_avatar_resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Icon Currency Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster_cardback_resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer",
                                "Icon Currency Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **2 槽**（INACT 0）· fs**30**
            // 🔴 **🆕 会员/称号那一格挂在 `background` 下、不在 `Dynamic Content` 下** —— 2026-10-04（A34-F1）订正：
            //    这一份原来记成「**3 槽**」，多出来的那个 `Title Drawer Horizontal Variant (1)`
            //    **是 `background` 的直接孩子**（原版 `background` 孩子序 = `foreground` / `Dynamic Content` /
            //    `name-bg` / **它**），**19 份里只有这一份如此** ⇒ **全表槽数 88 → 87**（`Dump()` 会打这个数）。
            //    判据 = `menu_dump.py bundle_menus_assets_all "General Basic Offer Container Variant Booster_title_resource" --depth 2 --relative`
            //    （缩进层 = 2；其余 18 份 `background` 都只有 3 个孩子）。
            new Variant("General Basic Offer Container Variant Booster_title_resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Icon Currency Drawer Variant" }, null)
            { BgDrawers = new[] { "Title Drawer Horizontal Variant (1)" } },

            // 339×778 · Dyn(119.5,280)→(219.5,380) · **3 槽**（INACT 0）· fs**34**
            // 🔴 `Deck Drawer` **只有这一份用到**（正本 §五·一）
            new Variant("General Basic Offer Container Variant Deck_cardback_avatar", Geo778, 34f,
                        new PxRect(119.5f, 280f, 219.5f, 380f),
                        new[] { "Deck Drawer", "Cardback Drawer", "Icon Avatar Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **5 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer", "Title Drawer Horizontal Variant",
                                "Icon Avatar Drawer Variant", "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **7 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource",
                        Geo778, 30f, new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer", "Title Drawer Horizontal Variant",
                                "Icon Avatar Drawer Variant", "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Currency Drawer Variant", "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,280)→(219.5,380) · **4 槽**（INACT 0）· fs**34**
            new Variant("General Basic Offer Container Variant Premium_Premium_cardback_avatar", Geo778, 34f,
                        new PxRect(119.5f, 280f, 219.5f, 380f),
                        new[] { "Cardback Drawer", "Icon Avatar Drawer Variant",
                                "Icon Premium Campaign Drawer Variant",
                                "Icon Premium Campaign Drawer Variant (1)" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_Resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Currency Drawer Variant", "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,295)→(219.5,395) · **5 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_booster_title_avatarOrResource", Geo778, 30f,
                        new PxRect(119.5f, 295f, 219.5f, 395f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Title Drawer Horizontal Variant", "Icon Currency Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,272)→(219.5,372) · **12 槽**（INACT **8**）· fs**34**
            // ⚠️ 这一份的抽屉**绝大多数出厂是 INACT** —— 原版运行期由那四步决定器（`DrawRewards`）挑一个开，
            //    **2026-10-04（A43）已解出**：按物品类型选（见文件头 A43 那一段 + `PickSlot`），
            //    选中那一个会被 `SetActive(true)` 打开（原版 `:363`）⇒ 出厂 INACT **不是**「永远不显示」。
            //    ⚠️ 原先这里写「目前没解出来（不是空 stub）」，**已订正**（铁律 5）。
            new Variant("General Basic Offer Container Variant Single Item Type", Geo778, 34f,
                        new PxRect(119.5f, 272f, 219.5f, 372f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Icon Avatar Drawer Variant 2", "Title Drawer Horizontal Variant",
                                "Icon Currency Drawer Variant", "Icon Currency Drawer Variant 2",
                                "Icon Currency Drawer Variant 3", "Icon Premium Campaign Drawer Variant",
                                "Card Drawer", "Icon Expansion Pass Premium Drawer Variant (1)",
                                // 🔴 这一件**只有这一份用到**（正本 §五·一）
                                "Icon Avatar Border Drawer", "Cardback Drawer" },
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Icon Avatar Drawer Variant 2", "Title Drawer Horizontal Variant",
                                "Icon Currency Drawer Variant", "Icon Currency Drawer Variant 2",
                                "Icon Currency Drawer Variant 3", "Icon Premium Campaign Drawer Variant" }),

            // 339×778 · Dyn(119.5,272)→(219.5,372) · **3 槽**（INACT 0）· fs**34**
            new Variant("General Basic Offer Container Variant avatarOrTitle_resource", Geo778, 34f,
                        new PxRect(119.5f, 272f, 219.5f, 372f),
                        new[] { "Icon Currency Drawer Variant", "Title Drawer Horizontal Variant (1)",
                                "Icon Avatar Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,280)→(219.5,380) · **6 槽**（INACT 0）· fs**34**
            new Variant("General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource",
                        Geo778, 34f, new PxRect(119.5f, 280f, 219.5f, 380f),
                        new[] { "Cardback Drawer", "Icon Avatar Drawer Variant", "Title Drawer Horizontal Variant",
                                "Icon Currency Drawer Variant", "Icon Currency Drawer Variant 2",
                                "Icon Premium Campaign Drawer Variant" }, null),

            // ---- 339×390 那一份（`Small …`）----
            // 339×**390**（唯一那个 `Small`）· Dyn(119.5,78)→(219.5,178) · **7 槽**（INACT **6**）· fs**34**
            // 🔴 它是 19 份里**唯一** `background` 的 `Image.m_Enabled = 1` 的一份（其余 18 份都是 0）
            new Variant("Small General Basic Offer Container Variant Single Item Type", GeoSmall, 34f,
                        new PxRect(119.5f, 78f, 219.5f, 178f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Title Drawer Horizontal Variant", "Icon Currency Drawer Variant",
                                "Cardback Drawer", "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" },
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Title Drawer Horizontal Variant", "Cardback Drawer",
                                "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" }),
        };

        /// <summary>按名字取一条变体（自检按表逐条建时用）。找不到 ⇒ `false`。</summary>
        public static bool Find(string prefab, out Variant v)
        {
            for (int i = 0; i < Variants.Length; i++)
                if (Variants[i].Prefab == prefab) { v = Variants[i]; return true; }
            v = default(Variant);
            return false;
        }

        // ============================================================ ✅ A43：按类型选槽（**照原版**，见文件头那一段）

        /// <summary>一个抽屉槽：**节点名（原 prefab 名，逐字）** → **它身上那个组件类的类名**。
        /// <para>判据链（**19 份逐份实读**，2026-10-04 A43）= 该节点的组件 `m_Script` → MonoScript 的 `m_ClassName`
        /// （与 `工具/read_itemdrawerconfig.py` 同一条；重名后缀 ` (1)` / ` 2` / ` 3` 是**同一份 prefab 的多个实例**，
        /// 类逐个核过、都相同）。命令可复现：
        /// <code>python -c "…（读 bundle_menus_assets_all 的这 19 个 prefab，走 m_Script→m_ClassName）"</code></para>
        /// 🔴 **名字不能推类**：`Deck Drawer` 这一格挂的是 **`DeckAndCardbackDrawer`**，而同名 prefab 在
        /// `ItemDrawerConfig` 里挂的是 `DeckDrawer`（`DeckAndCardbackDrawer : DeckDrawer`，签名桩实读）
        /// —— 这一条正是原版必须用 **is-a** 而不是等号的原因（见 <see cref="SlotClassMatches"/>）。</summary>
        public struct SlotType
        {
            public string Slot;
            public string Class;
            public SlotType(string slot, string cls) { Slot = slot; Class = cls; }
        }

        /// <summary>**19 份里出现过的 18 个槽名 → 它的抽屉类**（名单与 <see cref="Variants"/> 逐字一致）。</summary>
        public static readonly SlotType[] SlotTypes =
        {
            // `ItemDrawer<ShopContainerBase>`（表里那一条写的是 `Everguild.LiveOps.ShopContainer`）
            new SlotType("Icon Container Drawer Variant", "ContainerDrawer"),
            // 卡 / 异画 / 卡背 / 督军头像 / 称号 / 资源
            new SlotType("Card Drawer", "CardDrawer"),
            new SlotType("Card Alternate Art Drawer", "CardAlternateArtDrawer"),
            new SlotType("Cardback Drawer", "CardbackDrawer"),
            new SlotType("Icon Avatar Drawer Variant", "AvatarDrawer"),
            new SlotType("Icon Avatar Drawer Variant 2", "AvatarDrawer"),
            new SlotType("Title Drawer Horizontal Variant", "TitleDrawerHorizontal"),
            new SlotType("Title Drawer Horizontal Variant (1)", "TitleDrawerHorizontal"),
            new SlotType("Icon Currency Drawer Variant", "CurrencyDrawer"),
            new SlotType("Icon Currency Drawer Variant (1)", "CurrencyDrawer"),
            new SlotType("Icon Currency Drawer Variant 2", "CurrencyDrawer"),
            new SlotType("Icon Currency Drawer Variant 3", "CurrencyDrawer"),
            new SlotType("Icon Premium Campaign Drawer Variant", "PremiumDrawer"),
            new SlotType("Icon Premium Campaign Drawer Variant (1)", "PremiumDrawer"),
            new SlotType("Icon Expansion Pass Premium Drawer Variant", "ExpansionPassPremiumDrawer"),
            new SlotType("Icon Expansion Pass Premium Drawer Variant (1)", "ExpansionPassPremiumDrawer"),
            // ⚠️ 这一格**不在 `ItemDrawerConfig` 那张表里**（表里只有 `Avatar Border Drawer` 与
            //    `… Shop Variant` 两份，类都是 `AvatarBorderDrawer`）⇒ 类名是**从这一格自己身上读的**。
            new SlotType("Icon Avatar Border Drawer", "AvatarBorderDrawer"),
            // 🔴 **名字 ≠ 类的那一格**（见结构体 summary）
            new SlotType("Deck Drawer", "DeckAndCardbackDrawer"),
        };

        /// <summary>抽屉类的**基类链**（`is-a` 匹配要沿它上溯）—— **签名桩实读**，
        /// 出处 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/<类>.cs` 的 `class X : Y` 原文
        /// （`DeckAndCardbackDrawer.cs:3` · `TitleDrawerHorizontal.cs:6` · `PremiumIconDrawer.cs:1`）。
        /// <para>⚠️ 只列**本族配得上**的三条：其余 `*Drawer` 之间**没有继承关系**
        /// （`WildcardIconDrawer` / `RandomCardIconDrawer` / `TitleIconDrawer` / `ForgePointIconDrawer` 都是
        /// **直接 `ItemDrawer&lt;T&gt;` 的兄弟**，不是 `XxxDrawer` 的子类 —— 签名桩逐份核过）。</para></summary>
        static readonly string[] DrawerParents =
        {
            "DeckAndCardbackDrawer:DeckDrawer",
            "TitleDrawerHorizontal:TitleDrawer",
            "PremiumIconDrawer:PremiumDrawer",
        };

        /// <summary>槽名 → 抽屉类。**表里没有 ⇒ `null`**（调用方一律出声，别静默当匹配失败）。</summary>
        public static string DrawerClassOf(string slot)
        {
            for (int i = 0; i < SlotTypes.Length; i++) if (SlotTypes[i].Slot == slot) return SlotTypes[i].Class;
            return null;
        }

        /// <summary>`cls` 的基类（<see cref="DrawerParents"/> 里查不到 ⇒ `null` = 链到头了）。
        /// ⚠️ 分隔符同 <see cref="ParentItem"/>（`:`, 不是 `>`）。</summary>
        static string ParentDrawer(string cls)
        {
            for (int i = 0; i < DrawerParents.Length; i++)
            {
                int gt = DrawerParents[i].IndexOf(':');
                if (DrawerParents[i].Substring(0, gt) == cls) return DrawerParents[i].Substring(gt + 1);
            }
            return null;
        }

        /// <summary>槽的类配不配得上 cfg 那个类 —— **照原版**：`ReflectionHelper.Is(槽类, cfg 类)`
        /// = `cfg 类.IsAssignableFrom(槽类)` = 「**槽类是 cfg 类（或它的子类）**」
        /// （`ReflectionHelper__Is.c` 的实体是 `param_2.IsAssignableFrom(param_1)`；调用点
        /// `GeneralOfferPopupDrawer.__c__DisplayClass1_0___DrawRewards_b__3.c:21`）。
        /// ⇒ 沿**槽类**的基类链上溯找 `cfgClass`。
        /// <para>⚠️ 原版池键还有个**改名特例**（`DrawRewards.c:113-126`：池键是 `DeckAndCardbackDrawer` 就当作
        /// `DeckDrawer`；那两个 `DAT_` 常量已解出 = `script.json:1965803` / `:1965838`）。
        /// **本函数不需要它**：那条改名只影响**池的键**，而 20 条 cfg 里**没有一条**的类是
        /// `DeckAndCardbackDrawer` ⇒ 「改名」与「沿基链上溯」在这 20 条上**给同一个结果**
        /// （改名反而会**丢掉**「cfg 类是 `DeckAndCardbackDrawer`」这种情形，而那种情形不存在）。</para></summary>
        public static bool SlotClassMatches(string slotClass, string cfgClass)
        {
            string c = slotClass;
            for (int guard = 0; c != null && guard < 8; guard++)
            {
                if (c == cfgClass) return true;
                c = ParentDrawer(c);
            }
            return false;
        }

        /// <summary>`ItemDrawerConfig` 里的一条：**物品类型 → 抽屉类**。
        /// 出处 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md`（**整张表从 SO 原始字节解出**：
        /// 20 个类型 + 19 档 override，36 个 GUID，**零条靠猜**；脚本 `工具/read_itemdrawerconfig.py`）。
        /// <para>🔴 **`OfferPopups`(30) 这一档全表只有 3 条**（映射表 §⑤.2）⇒ 其余 17 个类型在这一档
        /// **回落主档**（`…GetDrawer.c:24-33`：按键找不到 `useDefault` 那条路）。</para></summary>
        public struct ItemTypeSet
        {
            /// <summary>原版 `TypeReference` 的名字（**逐字**，含命名空间；= 查表的键）。</summary>
            public string Type;
            /// <summary>`drawerReference`（主档 · `override == 0`）指向的 prefab 上的**抽屉类**。</summary>
            public string Main;
            /// <summary>`customDrawerOverrides` 里**键 = `OfferPopups`(30)** 那条的抽屉类；`null` = 这条**没写 30** ⇒ 回落主档。</summary>
            public string Popup;
            public ItemTypeSet(string t, string m, string p) { Type = t; Main = m; Popup = p; }
        }

        /// <summary>**`ItemDrawerConfig` 的 20 条**（`Main` / `Popup` 两列 = 那份 prefab 上的抽屉类）。
        /// ⚠️ **本表只抄了 `OfferPopups`(30) 这一档** —— 别的档（10 `Icon` / 15 `Horizontal` / 20 `Shop`）
        /// **是有判据的，只是本表没抄**：`资料/普查产出_1004/ItemDrawerConfig_映射表.md` 把 **19 档 override
        /// 全解出来了**（每一条都有 prefab 名 + 抽屉类 + GUID，含 `Icon`(10) / `Horizontal`(15) / `Shop`(20)）。
        /// 🔴 **2026-10-04 订正（铁律 5 / 10 / 11）**：本行原来写「别的档**没有判据**」—— **那句是假的**，
        /// 真实情况是「**本表没抄这一档**」⇒ 按铁律 11 该写成：**要做**（判据 → 映射表那 39 行表），
        /// **先做哪个** → `Icon`(10)（22 处 `ItemDrawer.Draw` 调用点里 **5 处**用它，全是「小徽记」场合）。
        /// 在抄进来之前，`ResolveDrawerClass` 遇到那些档**出声 + 返回 `null`**，
        /// ⛔ **不拿主档顶**（那会把「这一档的变体」静默画成别的样子）。</summary>
        public static readonly ItemTypeSet[] ItemTypeSets =
        {
            new ItemTypeSet("Currency", "CurrencyDrawer", null),
            new ItemTypeSet("PlayerAvatar", "AvatarDrawer", null),
            new ItemTypeSet("Everguild.LiveOps.ShopContainer", "ContainerDrawer", null),
            new ItemTypeSet("RawCardScript", "CardDrawer", null),
            new ItemTypeSet("DropTableItem", "RandomCardDrawer", null),
            new ItemTypeSet("CosmeticItemCardback", "CardbackDrawer", null),
            new ItemTypeSet("Wildcard", "WildcardDrawer", null),
            new ItemTypeSet("CampaignPoints", "CampaignPointDrawer", null),
            new ItemTypeSet("ForgePoints", "ForgePointDrawer", null),
            new ItemTypeSet("Everguild.LiveOps.ExpansionPassPoints", "ExpansionPassPointDrawer", null),
            new ItemTypeSet("PrebuiltDeck", "DeckDrawer", null),
            // 下面这三条就是**全表写着 30 的那三条**
            new ItemTypeSet("CosmeticItemTitle", "TitleDrawer", "TitleDrawerHorizontal"),
            new ItemTypeSet("ExpansionPremiumItem", "ExpansionPassPremiumDrawer", "ExpansionPassPremiumDrawer"),
            new ItemTypeSet("CosmeticItemAvatarBorder", "AvatarBorderDrawer", "AvatarBorderDrawer"),
            new ItemTypeSet("PremiumItem", "PremiumDrawer", null),
            new ItemTypeSet("VIPPremiumItem", "PremiumDrawer", null),
            new ItemTypeSet("AllianceTrophyData", "AllianceBadgeDrawer", null),
            new ItemTypeSet("XSollaBundleItem", "XSollaOfferDrawer", null),
            new ItemTypeSet("AlternateArtCard", "CardAlternateArtDrawer", null),
            new ItemTypeSet("GenericArmyItem", "GenericArmyItemDrawer", null),
        };

        /// <summary>**物品类型的基类链**（`GetReference` 第二轮 `Is(itemType, entryType)` 要沿它上溯）。
        /// 出处 = 签名桩（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/<类>.cs` 的 `class X : Y`）。
        /// <para>⚠️ 表里**只出现真正存在的父子对**；这些中间基类**自己不是** config 条目
        /// （`CosmeticItem` / `PlayerItem` / `Points&lt;T&gt;` / `ShopContainerBase` / `ObtainableItem`）。</para>
        /// <para>🔴 **最后三条 = 「表里没有、但它的祖宗在表里」那种类型的实据**（各自的下层已核过**没有更多子类**）：
        /// `PrebuiltSortedDeck : PrebuiltDeck`（`PrebuiltSortedDeck.cs:1`）· `Energy : Currency`（`Energy.cs:1`）·
        /// `DlcBundle : ShopContainer`（`DlcBundle.cs:4`）。**去掉任何一条，对应的那一行就会变成「判据空」**。</para>
        /// <para>⛔ 这张表**不完整就出声**（查不到 ⇒ `ResolveDrawerClass` 返回 `null` + `Debug.LogWarning`），
        /// **不猜**一个最近的条目顶上（红线：不许静默失败）。</para></summary>
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
        /// ⚠️ **分隔符是 `:`，不是 `>`** —— 类名里有 `Points&lt;T&gt;` 这种**带尖括号**的
        /// （用 `>` 当分隔符会被 `IndexOf('>')` 在 `<T>` 那里切断，静默解出 `"Points&lt;T"` 这种坏键）。</summary>
        static string ParentItem(string type)
        {
            for (int i = 0; i < ItemParents.Length; i++)
            {
                int gt = ItemParents[i].IndexOf(':');
                if (ItemParents[i].Substring(0, gt) == type) return ItemParents[i].Substring(gt + 1);
            }
            return null;
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
        /// **要做**（判据齐），先做哪个 → 排在「补 `ItemParents` 全链」之后（②不修的话，①修了还是近似）。</para></summary>
        /// <returns>`null` = **判据空**（原版这一项**什么都不画**）；`how` 里写明是哪一条（一律出声，不静默）。</returns>
        /// <remarks>🆕 2026-10-04（收 R-X1 的 F10b）：`&lt;returns&gt;` 原来**嵌在 `&lt;summary&gt;` 里、且与
        /// `&lt;/summary&gt;` 挤在同行** —— 现在拆成 C# 文档注释的常规形状（`summary` 先闭、`returns` 在后）。
        /// ⚠️ 「那一行原来是**没开标签的** `&lt;/returns&gt;`」这个说法我**没复核出来**（工作区那一版那一行
        /// 是 `&lt;returns&gt;…&lt;/returns&gt;` 成对的）—— 按铁律 2 如实记：**发现的是「位置不规范」，不是「标签没开」**。</remarks>
        public static string ResolveDrawerClass(string itemType, DrawerOverride ov, out string how)
        {
            if (string.IsNullOrEmpty(itemType))
            {
                how = "物品类型**空**（`Content.ItemType` 没给）⇒ 照原版「没有配置 ⇒ 这一项什么都不画」";
                return null;
            }
            ItemTypeSet e = default(ItemTypeSet);
            bool found = false;
            string via = null;
            string t = itemType;
            for (int guard = 0; t != null && guard < 12; guard++)
            {
                for (int i = 0; i < ItemTypeSets.Length; i++)
                    if (ItemTypeSets[i].Type == t) { e = ItemTypeSets[i]; found = true; break; }
                if (found) break;
                t = ParentItem(t);
                if (t != null) via = t;
            }
            if (!found)
            {
                // 🔴 **这条消息必须说真话**（2026-10-04 收 R-X1 的 F5）：代码查的是**本文件那张 `ItemParents`（21 条）**，
                //    **不是真 `.NET` 基类链** ⇒ 原来的措辞「它的**基类链**上也没有」是 **overclaim**
                //    （任何「真链上有、只是本表漏抄了」的类型都会打出同一句，而那种情况下**原版会命中、我们会空手**）。
                how = "本文件那张 `ItemDrawerConfig` 表（" + ItemTypeSets.Length + " 条）里**没有** `" + itemType
                      + "`，沿**本文件那张 `ItemParents`（" + ItemParents.Length + " 条）**上溯**也没命中** —— "
                      + "⚠️ **这不等于原版判空**：原版第二轮用的是真 `Type.IsAssignableFrom`"
                      + "（`ItemDrawerConfig__GetReference.c:35-45`），**表漏一条祖先时原版会命中、我们会空手** ⇒ "
                      + "照原版「没有配置 ⇒ 这一项什么都不画」+ **在此出声**";
                Debug.LogWarning("[OfferContainer] " + how + "（→ `资料/普查产出_1004/ItemDrawerConfig_映射表.md` 的 20 条）");
                return null;
            }

            string cls = e.Main;
            string pick = ov == DrawerOverride.Default ? "主档（`override == 0`）"
                                                      : "`customDrawerOverrides` 里键 = `" + (int)ov + "` 那一档";
            if (ov != DrawerOverride.Default && ov != DrawerOverride.OfferPopups)
            {
                // 2026-10-04 订正（收 R-X1 的 F10a）：**判据是有的**（映射表把 19 档 override 全解出来了），
                //   缺的是**本表没抄这一档** ⇒ 措辞不能再写「判据空」。**要做**（判据 → 映射表），
                //   先做哪个 → `Icon`(10)（22 处调用点里 5 处用它）。在抄进来之前**不拿主档顶**（那是静默画错）。
                how = "**本表还没抄 `" + ov + "`(" + (int)ov + ") 这一档**（判据在 `ItemDrawerConfig_映射表.md` 的 19 档 "
                      + "override 里、**是有的**；本文件只抄了 `OfferPopups`(30) 那一档，出处 `DrawRewards.c:319` 的 `0x1e`）"
                      + "⇒ 本函数返回 `null`（照原版「没有配置 ⇒ 不画」），⛔ **不拿主档顶**";
                Debug.LogWarning("[OfferContainer] `" + itemType + "`：" + how);
                return null;
            }
            if (ov == DrawerOverride.OfferPopups)
            {
                if (e.Popup != null) cls = e.Popup;
                else pick = "**回落主档**（这一条没写 `OfferPopups`(30) —— 全表 20 条里只有 3 条写了，映射表 §⑤.2）";
            }
            how = "`" + itemType + "`" + (via != null ? "（基类链上溯到 `" + e.Type + "`）" : "")
                  + " + " + pick + " ⇒ 抽屉类 **`" + cls + "`**";
            return cls;
        }

        /// <summary>选槽的结果（`PoolIndex < 0` = **不填**；`Why` 一律写明理由 —— 成功与失败都写）。</summary>
        public struct Pick
        {
            /// <summary>**池下标**（`0 .. PoolCount(v)-1`）：`Dynamic Content` 下那批按兄弟序在前、
            /// `background` 下那一格在后（= 原版 `GetComponentsInChildren` 的**先序**）。`-1` = 不填。</summary>
            public int PoolIndex;
            /// <summary>用到的抽屉类（`null` = 没解出）。</summary>
            public string DrawerClass;
            /// <summary>逐跳说明（哪条类型匹配 / 哪一档 / 命中哪个槽 / 为什么没命中）。</summary>
            public string Why;
        }

        /// <summary>变体里**池**的大小 = `Dynamic Content` 下那批 + `background` 下那一格。</summary>
        public static int PoolCount(Variant v)
        {
            return (v.Drawers != null ? v.Drawers.Length : 0) + (v.BgDrawers != null ? v.BgDrawers.Length : 0);
        }

        /// <summary>池下标 → 槽节点名（越界 ⇒ `null`）。</summary>
        public static string SlotNameAt(Variant v, int poolIndex)
        {
            int dn = v.Drawers != null ? v.Drawers.Length : 0;
            if (poolIndex < 0) return null;
            if (poolIndex < dn) return v.Drawers[poolIndex];
            int b = poolIndex - dn;
            return (v.BgDrawers != null && b < v.BgDrawers.Length) ? v.BgDrawers[b] : null;
        }

        /// <summary>池下标是不是**挂在 `background` 下**的那一格（<see cref="Variant.BgDrawers"/>）。</summary>
        public static bool IsBgPoolIndex(Variant v, int poolIndex)
        {
            return poolIndex >= (v.Drawers != null ? v.Drawers.Length : 0);
        }

        /// <summary>**哪个槽会被填** —— 照原版的四步（文件头 A43 那一段）。
        /// <para>池 = <see cref="SlotNameAt"/> 那个序（**先序**，与原版 `GetComponentsInChildren` 一致）；
        /// 命中 = `SlotClassMatches(槽的类, cfg 的类)`；`ordinal` = **同类奖励第几个**（原版的 `RemoveAt(0)`
        /// 就是它：第一个同类奖励拿池里第一个同类槽，第二个拿下一个……）。</para></summary>
        /// <returns><see cref="Pick.PoolIndex"/> &lt; 0 ⇒ **这一项什么都不画**（照原版 `continue`），`Why` 里写明原因。</returns>
        public static Pick PickSlot(Variant v, string itemType, DrawerOverride ov, int ordinal = 0)
        {
            var p = new Pick { PoolIndex = -1, DrawerClass = null, Why = null };
            string how;
            string cls = ResolveDrawerClass(itemType, ov, out how);
            p.DrawerClass = cls;
            if (cls == null) { p.Why = how; return p; }

            int seen = 0, n = PoolCount(v);
            var kinds = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++)
            {
                string sn = SlotNameAt(v, i);
                string sc = DrawerClassOf(sn);
                if (sc == null)
                {
                    kinds.Append("[").Append(i).Append("]`").Append(sn).Append("`=**类表里没有**（判据空）; ");
                    continue;
                }
                if (!SlotClassMatches(sc, cls)) { kinds.Append("[").Append(i).Append("]`").Append(sn).Append("`=").Append(sc).Append("; "); continue; }
                if (seen < ordinal) { seen++; kinds.Append("[").Append(i).Append("]`").Append(sn).Append("`=同类(第").Append(seen).Append("个，被 `ordinal` 跳过); "); continue; }
                p.PoolIndex = i;
                p.Why = how + "；池里第 " + (i + 1) + " 个（下标 " + i + "，"
                        + (IsBgPoolIndex(v, i) ? "**挂在 `background` 下**" : "`Dynamic Content` 下")
                        + "）槽 `" + sn + "` 是第 " + (ordinal + 1) + " 个同类槽 ⇒ 填它";
                return p;
            }
            p.Why = how + "；但这个变体的池（" + n + " 个槽）里**一个同类槽都没有** ⇒ "
                    + "照原版「池里找不到 ⇒ 什么都不画」。池的类逐格：" + kinds;
            return p;
        }

        /// <summary>`ItemKind` → **原版类型名**（只有一档**有判据**）：
        /// `Wildcard` ⇒ `"Wildcard"`（`ItemDrawer.Spec` 认出的野牌，其 SO 就是 `Wildcard` 这个类 ——
        /// 4 条 `Wildcard&lt;阵营&gt;&lt;档&gt;` SO 实读 + `ItemDrawerConfig` 表里有 `Wildcard` 这一条）。
        /// 其余 ⇒ `null`（**判据空**，不许猜：`Generic`/`Unknown` 是**我们**的枚举，不是原版的类型）。</summary>
        public static string TypeOfKind(ItemKind kind)
        {
            return kind == ItemKind.Wildcard ? "Wildcard" : null;
        }

        /// <summary>这个抽屉节点名在该变体里是不是**出厂 INACT**。</summary>
        public static bool IsOff(Variant v, string drawer)
        {
            if (v.Off == null) return false;
            for (int i = 0; i < v.Off.Length; i++) if (v.Off[i] == drawer) return true;
            return false;
        }

        /// <summary>建一个抽屉槽时给它的**开/关**（两条入槽路共用这一份 —— 免得规则写两处）。
        /// <para>· `poolClosed == true`（= A43 那四步跑了，`fill:true`）⇒ **整池先全关**：
        ///   原版 `DrawRewards.c:112` 就是池里**每一个** `SetActive(false)`，命中的那个随后才被 `:363` 打开
        ///   ⇒ 出厂 ACTIVE 但**没命中**的槽**不会**再留开着（典型：`…Single Item Type` 那 12 槽里 4 个 ACTIVE，
        ///   填 `Cardback Drawer` 时原版会把另外 3 个关掉）。</para>
        /// <para>· `poolClosed == false`（本工程自检要「prefab 出厂态」那一支）⇒ **只有 `Variant.Off` 里那几个关**。</para>
        /// <para>⚠️ **作用域**：原版那一下的宿主是**popup 自己**（`GetComponentsInChildren` 的 `param_1`，`:87`）
        /// ⇒ 原版关的是**整棵 popup 的池**；我们这一层一次只建**一个容器** ⇒ 关的是**这一棵容器**里的池
        /// （原版 popup 里若还有别的容器，它们的槽也会被一起关 —— 那边我们看不到，**如实记**）。</para>
        /// <para>🆕 2026-10-04（收 R-X1 的 F3）：这一条原来**整个省掉了**（只有 `IsOff` 那几个关），
        /// 属**沉默偏离** —— 现已照原版做，并由自检那条「填完之后**只有命中的槽**开着」钉住。</para>
        /// <para>⚠️ **两个如实记**：① 那一池的槽在我们这棵树上是**裸节点**（`MenuDraw.Node` 只建 `Transform`，
        /// 无 Graphic、无 Hit）⇒ 现在**证明不了这一条「画面上看得出来」**；照原版做是因为**判据就在那儿**
        /// （铁律 11），不是为了画面。② 曾经把这一下定性成「**那是 popup 那遍的事**」——**不对**：
        /// `:112`（整池先关）与 `:363`（命中再开）**都在 `DrawRewards` 这一个方法里**，是一开一关的**同一套**
        /// （`:112` 在建池那次循环里、`:363` 在「每个奖励项」那次循环 `:296-372` 里）——
        /// 它就是**填槽这一步的第 0 拍**，⛔ 不是别处的事。</para></summary>
        static void SetSlotActive(Transform slot, Variant v, string name, bool poolClosed)
        {
            if (poolClosed) { slot.gameObject.SetActive(false); return; }
            if (IsOff(v, name)) slot.gameObject.SetActive(false);
        }

        // ============================================================ 容器里装什么（**全由调用方给**）

        /// <summary>一个容器要显示的内容。原版这些**全由服务端报价给**（本地没有）。
        /// 空字符串 ⇒ 那一段**不画**（不是画个空框）。</summary>
        public struct Content
        {
            /// <summary>抽屉要画的东西（`ItemDrawer.Spec(...)` 组）。`Kind == None` ⇒ 抽屉**不画**（原版第②步）。</summary>
            public ItemSpec Item;
            /// <summary>🆕 **A43**：这一项的**原版 `ObtainableItem` 子类型名** —— **查 `ItemTypeSets` 那张表的键**
            /// （逐字，含命名空间；例 `"Currency"` · `"RawCardScript"` · `"Everguild.LiveOps.ShopContainer"`）。
            /// <para>🔴 **原版这个类型是 `item.GetType()` 来的**（`DrawRewards.c:315` 的虚调用 +
            /// `ItemDrawer.GetDrawerConfig` 里的 `System_Object__GetType`），**我们这套 `ItemSpec` 里没有 ⇒ 由调用方给**。
            /// 空 ⇒ **不是「随便挑一个槽」**，而是**照原版「没有配置 ⇒ 这一项什么都不画」+ 出声**（见 `PickSlot`）。</para>
            /// <para>只有 `ItemKind.Wildcard` 能**从 `Item` 自己推**（<see cref="TypeOfKind"/>）—— 那个有判据。</para>
            /// <para>⚠️ **它只管「填哪个槽」**；槽**里面**画什么仍走 `ItemDrawer.Draw`（那一边的替身口径见 `ItemDrawer.cs` 文件头）。</para></summary>
            public string ItemType;
            public string Name;         // `name`
            public string Type;         // `type`
            public string Price;        // `Price Display Button/…/text`
            public string BadgeText;    // `Badge/Text (TMP)`（出厂 `DefBadgeText`）
            public string TimerText;    // `Timer/Timer Text`（出厂 `DefTimerText`）
            public string Available;    // `Available Counter`（出厂 `DefAvailText`）；**空 = 不画**

            /// <summary>照出厂值填一份（**只有抽屉那一段是空的** —— 它要物品，出厂没有）。</summary>
            public static Content Def()
            {
                return new Content
                {
                    Item = default(ItemSpec),
                    Name = DefNameText, Type = DefTypeText, Price = null,
                    BadgeText = DefBadgeText, TimerText = DefTimerText, Available = DefAvailText,
                };
            }
        }
        // 🔴 **2026-10-04（收 R-X1 的 F6）：删掉了 `Content.Quantity`**。原版这一跳是**硬编码立即数 `1`**
        //    （`GeneralOfferPopupDrawer__DrawRewards.c:357` 传给虚表 `+0x188` 的第二实参；`DrawItemReward.c` 同形），
        //    不是「报价里那一项的数量」⇒ 本族**根本没有数量输入口**。那个字段**全库没有任何读者**
        //    （只有 `Def()` 写 1、`FillSlot` 读它），留着就是「设了不生效」的**静默陷阱**（同 R-X1 的 F9 那条教训）。
        //    ⚠️ 要数量的是**别的链**（例：`CampaignRewardWindow` 传 `spec.Quantity`）—— 那些不经过本族，没动。

        /// <summary>建出来的东西（自检靠它认「填没填 / 填的是哪个槽」）。</summary>
        public struct Built
        {
            public Transform Root;      // 容器根（名字 = `Variant.Prefab`）
            public Transform Dynamic;   // `Dynamic Content`
            public Transform Filled;    // 被填的那个抽屉槽；`null` = 一个都没填
            public string Drawer;       // `ItemDrawer` 实际选的抽屉名；`null` = 没画
            public bool Placeholder;    // 走了占位板（调用方**必须出声**）
            public bool MissingArt;     // 有图取不到
            public bool FallbackArt;    // 用了退档图（原版那张取不到 —— 调用方**必须出声**）
            /// <summary>🆕 **A43**：选中槽的**池下标**（口径见 <see cref="Pick.PoolIndex"/>）。
            /// 🔴 **`-1` = 没填** —— 这个初值在 `Build` 开头**显式设**（2026-10-04 收 R-X1 的 F9）：
            /// 本字段原来是 `default(int) = 0`，而 **`0` 是一个合法池下标**（池非空时 = 池里第 1 格）
            /// ⇒ 「没填」与「填了第 1 格」在这一个字段上**不可分**（谁直接读它就会静默取错）。
            /// ⚠️ **读它之前仍要先看 <see cref="Filled"/>**（`Transform`，`null` = 一个槽都没填）——
            /// 两个字段由同一处（`FillSlot`）写，`Filled == null` ⟺ `FilledPool < 0`。</summary>
            public int FilledPool;
            /// <summary>🆕 **A43**：选槽那一跳解出的**抽屉类**（`null` = 没解出）。
            /// ⚠️ **它非空 ≠ 填了槽**：类型解得出、池里没有同类槽时它是**类名**而 `Filled` 仍是 `null`
            /// （例：`Wildcard` ⇒ `WildcardDrawer`，而 19 份的 18 个槽名里没有这个类）。</summary>
            public string DrawerClass;
            /// <summary>🆕 **A43**：选槽的**逐跳说明**（成功与失败都写；失败时调用方**必须出声**，见 `Build` 里那段）。</summary>
            public string SlotNote;
        }

        // ============================================================ 建

        /// <summary>照变体表建一棵容器骨架，返回根节点。
        /// <para>坐标：`(x, y)` 是根左上角（画布像素 · 左上原点 · y 向下）；尺寸取 `v.G.W/H`。</para>
        /// <para>🔴 **落点**：原版这一族由 `ContainerService.OpenOfferContainer` 在**服务端报价**到达时实例化，
        /// 我们**没有那个数据源** ⇒ 这里只提供 `(x,y)` 让调用方摆，**不编入口**（文件头）。</para></summary>
        public static Built Build(Transform parent, Variant v, float x, float y, Content c,
                                  int qBase, System.Action onClick, bool fill = true)
        {
            var b = new Built();
            b.FilledPool = -1;      // 🔴 F9：显式初值 —— `default(int) = 0` 是个**合法池下标**，不能兼职表示「没填」
            var g = v.G;
            PxRect R(float x1, float y1, float x2, float y2) { return new PxRect(x + x1, y + y1, x + x2, y + y2); }

            // ---- 根 ----
            b.Root = MenuDraw.Node(parent, v.Prefab, new PxRect(x, y, x + g.W, y + g.H));

            // ---- `background` / `foreground`：**只在该份的 `Image.m_Enabled = 1` 时画**（无图 ⇒ 纯色块）----
            var bg = MenuDraw.Node(b.Root, NBackground, R(0f, 0f, g.W, g.H));
            if (g.BgOn)
                MenuDraw.Rect(bg, CardArt.Solid(), R(0f, 0f, g.W, g.H), NBackground + " Gfx", qBase + QoBg,
                              new Color(1f, 1f, 1f, 1f));
            var fg = MenuDraw.Node(bg, NForeground, R(0f, 0f, g.W, g.H));
            if (g.FgOn)
                MenuDraw.Rect(fg, CardArt.Solid(), R(0f, 0f, g.W, g.H), NForeground + " Gfx", qBase + QoBg,
                              new Color(1f, 1f, 1f, 1f));

            // ---- `Dynamic Content`：**锚框**（原版无脚本无 Graphic）+ 抽屉槽 ----
            b.Dynamic = MenuDraw.Node(bg, NDynamic, R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2));

            // ---- ✅ A43：**填哪个槽** = 照原版「按物品类型选」（文件头那一段的四步）----
            //   🔴 这里原来是 `FirstActiveIndex`（兄弟序里第一个非 INACT 的槽）—— **那是我们挑的启发式**，
            //     2026-10-04 换成原版规则。**没命中就一个槽都不填**（照原版 `continue`），并且**出声**。
            //   ✅ **原版第①步那一下「池里每一个先 `SetActive(false)`」（`:112`）也照做了**
            //     （2026-10-04 收 R-X1 的 F3；落在 `SetSlotActive` 里，两条入槽路共用）。
            //   ⚠️ **本族仍是「一次只装一项」**（`ordinal = 0`，只填**第一个**同类槽）—— 原版是按报价里的
            //     **每一个奖励项**逐个装（`:296-372` 的外层循环）。**这是一条还没做的偏离**（见文件头 ④ 末）。
            int filled = -1;
            if (fill)
            {
                // 类型：调用方给的优先；没给时只有 `Wildcard` 推得出来（`TypeOfKind`），其余是**判据空**
                string it = !string.IsNullOrEmpty(c.ItemType) ? c.ItemType : TypeOfKind(c.Item.Kind);
                if (string.IsNullOrEmpty(it))
                {
                    b.SlotNote = "物品类型的**判据空**（`Content.ItemType` 没给，`ItemKind." + c.Item.Kind
                                 + "` 也推不出原版类型 —— 只有 `Wildcard` 推得出）";
                    Debug.LogWarning("[OfferContainer] `" + v.Prefab + "`：**没填任何槽** —— " + b.SlotNote
                                     + "；照原版「没有配置 ⇒ 这一项什么都不画」（`DrawRewards.c:331-332`）");
                }
                else
                {
                    // `DrawerOverride.OfferPopups` = 30 = 原版那句 `0x1e`（`DrawRewards.c:319`）
                    // `ordinal = 0` = 这一份报价里的第 1 个同类项（原版 `RemoveAt(0)` 消费池）
                    var pick = PickSlot(v, it, DrawerOverride.OfferPopups);
                    filled = pick.PoolIndex;
                    b.FilledPool = filled; b.DrawerClass = pick.DrawerClass; b.SlotNote = pick.Why;
                    if (filled < 0)
                        Debug.LogWarning("[OfferContainer] `" + v.Prefab + "`：**没填任何槽** —— " + pick.Why);
                }
            }
            int dynN = v.Drawers != null ? v.Drawers.Length : 0;
            if (v.Drawers != null)
            {
                for (int i = 0; i < v.Drawers.Length; i++)
                {
                    // 抽屉槽一律摆在 `Dynamic Content` 的框上（**不照抄 prefab 的抽屉矩形** —— 那是未展开的模板位，见文件头）
                    var slot = MenuDraw.Node(b.Dynamic, v.Drawers[i],
                                             R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2));
                    SetSlotActive(slot, v, v.Drawers[i], fill);
                    if (i != filled) continue;
                    // 🔴 原版选中就 `SetActive(true)`（`DrawRewards.c:363`）—— 本族那 16 个**出厂 INACT** 的槽
                    //    就是靠这一下打开的（它们正是「这一份用不上」时预置关着的那几个）。
                    slot.gameObject.SetActive(true);
                    FillSlot(slot, R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2), c, qBase, v.Drawers[i], ref b);
                }
            }

            // ---- `name-bg` 那一窝 ----
            var nb = MenuDraw.Node(bg, NNameBg, R(g.NameBg.x1, g.NameBg.y1, g.NameBg.x2, g.NameBg.y2));
            MenuDraw.Rect(nb, CardArt.Solid(), R(g.NameBg.x1, g.NameBg.y1, g.NameBg.x2, g.NameBg.y2),
                          "name-bg Gfx", qBase + QoNameBg, NameBgTint);

            LabelFit(nb, R(g.Name.x1, g.Name.y1, g.Name.x2, g.Name.y2), c.Name, NameColor, NName,
                     g.NameFs, 10f, qBase + QoText);
            LabelFit(nb, R(g.Type.x1, g.Type.y1, g.Type.x2, g.Type.y2), c.Type, TypeColor, NType,
                     v.TypeFs, 10f, qBase + QoText);
            BuildPrice(nb, R, g, c, qBase);
            BuildWebShop(nb, R, g, qBase);

            // 出厂 INACT 的那颗空 `Image` —— 🔴 **它是 `name-bg` 的孩子**（2026-10-04 A34-F3 订正），
            //   兄弟序在 `WebShop` 之后、`Available Counter` **之前**（原版 `m_Children` 实读）。
            {
                var dummy = MenuDraw.Node(nb, NWebShopGfx, R(g.WebShopDummy.x1, g.WebShopDummy.y1,
                                                             g.WebShopDummy.x2, g.WebShopDummy.y2));
                dummy.gameObject.SetActive(false);                      // 出厂 INACT（照抄）
            }

            // `Available Counter` —— 空串 ⇒ **不画**（原版那一格是 `Available: 1/5`，0 次限购时它整件也关）
            if (!string.IsNullOrEmpty(c.Available))
                LabelFit(nb, R(g.Avail.x1, g.Avail.y1, g.Avail.x2, g.Avail.y2), c.Available,
                         NameColor, NAvail, v.TypeFs, 10f, qBase + QoText);

            // ---- 🆕 `background` **自己**的抽屉槽（`Variant.BgDrawers`）----
            //   2026-10-04（A34-F1）：`…Variant Booster_title_resource` 那一份有一个抽屉槽**不挂在
            //   `Dynamic Content` 下**，而是 `background` 的**第 4 个孩子、排在 `name-bg` 之后**。
            //   ⇒ 位置照原版（`bg` 的孩子序：`foreground` / `Dynamic Content` / `name-bg` / **本格**）；
            //     框与画法照本文件既有那条规则（摆在 100×100 锚框上 + 转调 `ItemDrawer.Draw`），
            //     **不照抄它自己的矩形**（211.33×76.05 —— 理由同文件头「两个别照抄的坑」）。
            //   🔴 **A43 订正**：这一格**参与**挑选 —— 原版的池是 `GetComponentsInChildren<ItemDrawer>(true)`
            //     （`DrawRewards.c:87-95`），**整棵容器**（含 `background` 下这一格）**都在池里**；
            //     先序上它排在 `name-bg` 之后 ⇒ **池下标 = `Dynamic Content` 那批之后**（`SlotNameAt` 的口径）。
            //     原来那条注释写「它不参与挑选」是**启发式时代**的说法（`FirstActiveIndex` 只看 `v.Drawers`）。
            if (v.BgDrawers != null)
                for (int i = 0; i < v.BgDrawers.Length; i++)
                {
                    var slot = MenuDraw.Node(bg, v.BgDrawers[i],
                                             R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2));
                    SetSlotActive(slot, v, v.BgDrawers[i], fill);      // 同 `Dynamic Content` 那一批（池先全关）
                    if (dynN + i != filled) continue;
                    slot.gameObject.SetActive(true);    // 同 `Dynamic Content` 那一批：选中就开（`DrawRewards.c:363`）
                    FillSlot(slot, R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2), c, qBase, v.BgDrawers[i], ref b);
                }

            // ---- `Badge`（根下，不在 `name-bg` 里）----
            {
                var br = BadgeRect(g);
                var bar = R(br.x1, br.y1, br.x2, br.y2);
                var badge = MenuDraw.Node(b.Root, NBadge, bar);
                MenuDraw.Nine(badge, CardArt.MenuUi(BadgeArt), bar, BadgeBorder, BadgeTexW, BadgeTexH,
                              qBase + QoBadge, BadgeTint, true, NBadge + " Gfx");
                if (!string.IsNullOrEmpty(c.BadgeText))
                {
                    var tr = g.BadgeText;
                    // 原版 `Text (TMP)`：**fs36 钉死（无 auto）· 折行=1 · Left/Top**
                    LabelFit(badge, R(tr.x1, tr.y1, tr.x2, tr.y2), c.BadgeText, NameColor, NBadgeText,
                             36f, 0f, qBase + QoText, true, true);
                }
            }

            // ---- `Timer`（根下）----
            {
                var tr = g.Timer;
                var timer = MenuDraw.Node(b.Root, NTimer, R(tr.x1, tr.y1, tr.x2, tr.y2));
                var ic = g.TimerIcon;
                var icr = R(ic.x1, ic.y1, ic.x2, ic.y2);
                float side = Mathf.Min(icr.W, icr.H);
                var icn = MenuDraw.Node(timer, NTimerIcon,
                                        new PxRect(icr.CX - side * 0.5f, icr.CY - side * 0.5f,
                                                   icr.CX + side * 0.5f, icr.CY + side * 0.5f));
                MenuDraw.Rect(icn, CardArt.MenuUi(ClockArt),
                              new PxRect(icr.CX - side * 0.5f, icr.CY - side * 0.5f,
                                         icr.CX + side * 0.5f, icr.CY + side * 0.5f),
                              "Icon Gfx", qBase + QoText, null, true);
                if (!string.IsNullOrEmpty(c.TimerText))
                {
                    var tt = g.TimerText;
                    // 原版 `Timer Text`：**折行=1** · `Left/Midline` —— 🔴 **字号与自适应范围【逐份】**
                    //（`TypeFs=30` ⇒ fs28 `auto[18,28]`；`TypeFs=34` ⇒ fs30.6 `auto[10,32]`）。
                    // ⚠️ **上限 ≠ 字号**（34 那一支是 30.6 对 32）⇒ `LabelFit` 那一格单列出来，别拿 `fontPx` 顶上。
                    float tfs, tmin, tmax;
                    TimerTextFit(v.TypeFs, out tfs, out tmin, out tmax);
                    LabelFit(timer, R(tt.x1, tt.y1, tt.x2, tt.y2), c.TimerText, NameColor, NTimerText,
                             tfs, tmin, qBase + QoText, true, true, tmax);
                }
            }

            // ---- `raycast target`：整根那么大的命中区（原版 `Image,EverguildButton` · `trans=1`）----
            // 🔴 原版点它走 `ShopOfferContainer.OnClick → OpenContainer`，**那一族我们没有** ⇒
            //    `onClick` 由调用方给。**给 null 就如实出声**（建的时候说一次 + 真点了再说一次）——
            //    红线「不许静默失败」+「点了必须有反应」，两条都要满足。
            if (onClick == null)
            {
                string who = v.Prefab;
                Debug.Log("[OfferContainer] `" + who + "`：整卡的点击入口**没有接**"
                          + "（原版 `ShopOfferContainer.OnClick → OpenContainer` 那族我们没有；"
                          + "而这一族的数据源是服务端 LiveOps 报价 ⇒ 本期只建骨架，见 `Shell/OfferContainer.cs` 文件头）");
                onClick = () => Debug.Log("[OfferContainer] `" + who + "` 被点了 —— 还是**没有入口**"
                                          + "（同上：这一族本期只建骨架，不编假入口）");
            }
            MenuDraw.Hit(b.Root, NRaycast, R(0f, 0f, g.W, g.H), qBase + QoHit, onClick);
            return b;
        }

        /// <summary>把一个抽屉槽填上 —— **两条入槽路共用这一份**（`Dynamic Content` 下那批 + `background` 下那一格），
        /// 免得「同一条规则写两处」（`CLAUDE.md` §三）。
        /// <para>🔴 **档位 = `DrawerOverride.OfferPopups`(30)** —— 原版这一族挂抽屉走的就是这一档：
        /// `GeneralOfferPopupDrawer__DrawRewards.c:319` 把 **`0x1e`** 传给 `ItemDrawer.GetDrawerConfig`。
        /// ⚠️ 本文件原来在这里写的是「取 `Shop`(20) 档，**这是我们挑的**」—— 那是在判据没读到的时候挑的；
        /// **现在读到了，按读到的改**（铁律 5）。</para>
        /// <para>一律走已有的 `ItemDrawer.Draw(...)`（**判据只此一份**，本文件只转调）。</para>
        /// <para>🔴 **数量传字面量 `1`**（2026-10-04 收 R-X1 的 F6）：原版那一步是**硬编码立即数 `1`**
        /// （`DrawRewards.c:357` 虚表槽 `+0x188` 的第二实参；`DrawItemReward.c` 同形），**不是**报价里那一项的数量
        /// ⇒ 本族**没有数量输入口**（原来传的 `Content.Quantity` 已删，见 `Content` 后面那段）。
        /// ⚠️ 这里 `QuantityPx = 0` ⇒ 抽屉**本来就不画数量**，两种传法在画面上**一模一样**
        /// —— 这正说明它是**静默偏离**（只有判据能发现），所以要照判据改。</para></summary>
        static void FillSlot(Transform slot, PxRect box, Content c, int qBase, string slotName, ref Built b)
        {
            var st = ItemDrawerStyle.Default(qBase + QoDrawer, qBase + QoDrawer, qBase + QoText);
            st.NodeName = "Item Drawer";
            st.IconFill = 0.7f; st.QuantityPx = 0f; st.NamePx = 0f;
            var drew = ItemDrawer.Draw(slot, box, c.Item, 1, DrawerOverride.OfferPopups, st);   // `1` = 原版立即数（`:357`）
            b.Filled = slot; b.Drawer = drew.Drawer;
            b.Placeholder = drew.Placeholder; b.MissingArt = drew.MissingArt;
            b.FallbackArt = drew.FallbackArt;
            // 红线：不许静默失败 —— 判据说「该画」却没画出来 / 落了占位板 / 用了退档图，都要出声
            if (drew.Node == null)
                Debug.LogWarning("[OfferContainer] 槽 `" + slotName
                                 + "`：`ItemDrawer.Draw` **什么都没画**（物品判据空，原版第②步）");
            else if (drew.Placeholder)
                Debug.LogWarning("[OfferContainer] 槽 `" + slotName
                                 + "`：抽屉**落了占位板**（`" + (c.Item.Id ?? "<空>") + "` 的图取不到）");
            else if (drew.FallbackArt)
                Debug.LogWarning("[OfferContainer] 槽 `" + slotName
                                 + "`：抽屉用了**退档图**（`" + drew.Art + "`）");
        }

        /// <summary>`Price Display Button` → `Generic UI Button` → `Price Display` → `text`。
        /// 🔴 `Price Display` 原版是个 `HorizontalLayoutGroup`（`icon` + `text`），而 `icon` 的图**出厂是空的**、
        /// `text` 量出来的宽是 **0**（布局没跑）⇒ 我们只建节点 + 一个 `text`，**按按钮整框居中**画（**我们挑的**）。</summary>
        static void BuildPrice(Transform nb, System.Func<float, float, float, float, PxRect> R, Geo g, Content c, int qBase)
        {
            var pr = g.Price;
            var node = MenuDraw.Node(nb, NPrice, R(pr.x1, pr.y1, pr.x2, pr.y2));
            var box = MenuDraw.Node(node, NPriceBtn, R(pr.x1, pr.y1, pr.x2, pr.y2));
            MenuDraw.Rect(box, CardArt.MenuUi(PriceArt), R(pr.x1, pr.y1, pr.x2, pr.y2), "Button Gfx",
                          qBase + QoText, ShopTabPage.PriceTint, true);
            var inner = MenuDraw.Node(box, NPriceBox, R(pr.x1, pr.y1, pr.x2, pr.y2));
            if (!string.IsNullOrEmpty(c.Price))
                LabelFit(inner, R(pr.x1, pr.y1, pr.x2, pr.y2), c.Price, NameColor, NPriceText, 38.15f, 13.46f,
                         qBase + QoText, false);
        }

        /// <summary>`WebShop Button Square Variant` —— **三件全建**（2026-10-04 A34-F3 订正）：
        /// `Highlight`（`OctagonUI Filled Fade SDF` · 九宫 52 → 画出来角块 **26**）·
        /// `Button Image`（`40K_button_square` 103×107 · `ppuMul 2.5`）· `Icon`（`40K_Icon_Discount_Gold` · `scl 1.2`）。
        /// 🔴 **2026-10-04（W1-3）**：`Icon` 的**矩形是正方形**（宽 := 高）—— 那颗挂着
        /// `AspectRatioFitter (HeightControlsWidth · ratio 1.0)`，详见下面那一段注释。
        /// <para>🔴 **订正记录**：本方法原来写「`Highlight` 与 `Icon` 的 sprite 实读是『未解出』⇒ 判据空 ⇒ 不建」——
        /// **那两句话是假的**：两张图实读都有名字（第 4 代 dump 一行就给），而且**当时就已经在**
        /// `Resources/Art/ui_menu/` 里。21 张里少的只有 `40K_button_square_pressed`
        /// （那是**换图**那一档的，与建不建节点无关，见 `ShopWindow` / `WindowButton` 那条链）。
        /// ⚠️ 那颗出厂 INACT 的空 `Image` **不是这里的孩子** —— 它是 `name-bg` 的孩子，
        /// 由 <see cref="Build"/> 在 `WebShop` 之后单独建（见 `NWebShopGfx` 的注释）。</para></summary>
        static void BuildWebShop(Transform nb, System.Func<float, float, float, float, PxRect> R, Geo g, int qBase)
        {
            var wr = g.WebShop;
            var node = MenuDraw.Node(nb, NWebShop, R(wr.x1, wr.y1, wr.x2, wr.y2));

            // `Highlight`：**发光圈**，矩形比 `WebShop` 大一圈（左右各 21.3 / 上下各 18.2，原版就是溢出的）
            {
                var hr = g.WebShopHighlight;
                var bar = R(hr.x1, hr.y1, hr.x2, hr.y2);
                MenuDraw.Nine(node, CardArt.MenuUi(HighlightArt), bar, HighlightBorder,
                              HighlightTexW, HighlightTexH, qBase + QoWebShopHi, HighlightTint, true,
                              NWebShopHighlight, HighlightBorder / HighlightPpuMul);
            }

            var n9 = MenuDraw.Nine(node, CardArt.MenuUi(WebShopArt), R(wr.x1, wr.y1, wr.x2, wr.y2),
                                   WebShopBorder, WebShopTexW, WebShopTexH, qBase + QoWebShop, null, true,
                                   NWebShopImage, WebShopBorder / WebShopPpuMul);

            // `Icon`：`40K_Icon_Discount_Gold` 128² · **`Simple`（无 `preserveAspect`）** · `m_Color (1,1,1,1)`
            // 🔴 节点自己的 **`m_LocalScale = 1.2`**、`pivot = (0.5, 0.5)` ⇒ **渲出来 = 布局矩形以中心为轴 ×1.2**
            //    （`MenuDraw.Rect` 只吃矩形、不吃 scale ⇒ 这里**自己把矩形放大**，别指望它读 prefab 的缩放）。
            // 🔴🔴 **2026-10-04（W1-3）：宽取【高】、不取布局宽** —— 那个节点上挂着 `AspectRatioFitter`
            //    （原始 MB `MonoBehaviour_8337996828984527321`）= `m_Enabled 1` · **`m_AspectMode 2
            //    (HeightControlsWidth)`** · **`m_AspectRatio 1.0`**（19 份全同）；uGUI
            //    `AspectRatioFitter.cs` 的 `case AspectMode.HeightControlsWidth:`
            //    → `rectTransform.SetSizeWithCurrentAnchors(Horizontal, rectTransform.rect.height * m_AspectRatio)`
            //    ⇒ **原版渲出来是【正方形】**（`pivot 0.5` ⇒ 以中心为轴改宽，高不动）。
            //    ⚠️ 于是 `Geo.WebShopIcon.W`（dump 那格的 **57.78** 之类）**不能当渲染宽用** —— 按它画会**宽多 21%~28%**。
            //    ⚠️ **根因是我们的 dump 工具**：`工具/menu_dump.py` **只跑 `LayoutGroup`、不模拟 `AspectRatioFitter`**
            //    ⇒ 它给的是**序列化值**、不是运行时值（铁律 5·c「一个值 ≠ 全部情况」）。
            //    🔴 **同一条纪律本文件头已经用过一次**：「两个别照抄的坑」② 就是因为 `AspectRatioFitter`
            //    而**禁止照抄任何抽屉矩形** —— 对 `Icon` 当时**没走这条纪律**，这次补上。
            //    中心不变（`SetSizeWithCurrentAnchors` + `pivot 0.5`）⇒ 仍以布局矩形中心为准。
            {
                var ir = g.WebShopIcon;
                float side = ir.H * WebShopIconScale;                  // 宽 := 高（`AspectRatioFitter`）· 再乘 `m_LocalScale`
                float hs = side * 0.5f;
                var box = new PxRect(ir.CX - hs, ir.CY - hs, ir.CX + hs, ir.CY + hs);
                MenuDraw.Rect(node, CardArt.MenuUi(IconArt), R(box.x1, box.y1, box.x2, box.y2),
                              NWebShopIcon, qBase + QoWebShopIcon, new Color(1f, 1f, 1f, 1f));
            }

            // 悬停/按下换图：原版这一颗的 `HL/P` 实读到名字（`HL=40K_button_square_hover` · `P=…_pressed`）
            var hit = MenuDraw.Hit(node, "Hit", R(wr.x1, wr.y1, wr.x2, wr.y2),
                                   qBase + QoWebShopHit, WebShopClick);
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            if (wb != null && n9 != null) wb.BindNine(n9, WebShopArt, "40K_button_square_hover", "40K_button_square_pressed");
        }

        /// <summary>`WebShop` 那一颗点了干什么 —— **原版是打开网页商店**（`EverguildButton` 的
        /// `trans=2 target=<外部链接>`）。本地没有那个链接（`target` 是 PathID、内容查不到）⇒ **只出声**。</summary>
        static void WebShopClick()
        {
            Debug.Log("[OfferContainer] `WebShop Button Square Variant` 被打——原版是开网页商店"
                      + "（`EverguildButton.trans = 2` 的外链），**我们这里只出声、不跳转**（链接本体本地查不到）");
        }

        /// <summary>按原版那套 TMP 参数摆一段字（**逐项对着 dump 抄**）：
        /// `m_fontSize` 起手 · 有 `auto[最小,最大]` 的才开自适应（`minPx &gt; 0`）· 有 `折行=1` 的才限宽换行 · 左对齐。
        /// 🔴 **这几档是逐个节点实读的，别一律开**：`name`/`type`/`Available Counter`/`text` 是 **auto + 不折行**；
        /// `Badge/Text (TMP)` 是 **不 auto + 折行**（fs36 钉死）；`Timer Text` 是 **auto + 折行**（范围逐份）。
        /// 🔴 **`maxPx` 单列一个参数**（2026-10-04 A34-F2）：原版 `Timer Text` 在 `TypeFs=34` 那 9 份里
        /// **`m_fontSize = 30.6` 而上限 = 32** —— 两者**不相等** ⇒ 不能再拿 `fontPx` 充上限。
        /// `maxPx &lt;= 0` ⇒ 退回「上限 = `fontPx`」（其余节点的实测值都是这样）。
        /// 🔴 **顺序**：先 `SetAutoFitBox` 再对齐（对齐按**当前**文字宽算 —— 本工程踩过）。</summary>
        static Label LabelFit(Transform parent, PxRect r, string text, Color color, string name,
                              float fontPx, float minPx, int q, bool left = true, bool wrap = false,
                              float maxPx = 0f)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lb = MenuDraw.Text(parent, r, text, color, name, fontPx, q, wrap ? r.W : 0f);
            if (lb == null) return null;
            if (minPx > 0f && fontPx > minPx)
            {
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), minPx, maxPx > 0f ? maxPx : fontPx);
                // 🔴 **2026-10-04（首跑红了，就地补）**：`SetAutoFitBox` 内部的 `SetWrapWidth` 会**无条件**
                //   把换行模式设成 `Normal` ⇒ 「自适应 + 不折行」的件会被悄悄打开折行。
                //   原版这几件全是 `折行=0`（文件头 :80 那张实读表）⇒ 这里按 `wrap` 参数**还原**。
                lb.SetWrapping(wrap);
            }
            if (left) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>19 份的清单（自检打印 + 诊断用）。
        /// ⚠️ **`slots` 只数 `Dynamic Content` 下的槽**（= **87**，2026-10-04 A34-F1 订正；原来是 88）；
        /// `background` 下那一格（`bgSlots` = **1**）**单列**，别并进 `slots` —— 两者在原版里是**两个父节点**。</summary>
        public static string Dump()
        {
            int slots = 0, off = 0, bgSlots = 0;
            for (int i = 0; i < Variants.Length; i++)
            {
                slots += Variants[i].Drawers != null ? Variants[i].Drawers.Length : 0;
                off += Variants[i].Off != null ? Variants[i].Off.Length : 0;
                bgSlots += Variants[i].BgDrawers != null ? Variants[i].BgDrawers.Length : 0;
            }
            return "OfferContainer：变体 " + Variants.Length + " 个 · `Dynamic Content` 抽屉槽 " + slots
                   + " 个（其中出厂 INACT " + off + " 个）· `background` 下的槽 " + bgSlots + " 个";
        }
    }
}
