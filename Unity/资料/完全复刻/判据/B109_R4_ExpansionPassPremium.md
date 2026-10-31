# B109_R4 · `Icon Expansion Pass Premium Drawer Variant` 的两层判据（`Background` / `Campaign Glow`）

> **代理**：`B109_R4`（只读查判据）· **日期**：2026-10-31 · **波次**：第一百零九会话 · 波 1
> **判据源**：`d:/2/tools/assetripper_out/ExportedProject/Assets/Icon Expansion Pass Premium Drawer Variant.prefab`（YAML 直读）
> ＋ 同族 5 份 prefab ＋ 3 份使用它的 popup prefab（**实例值**）＋ `assets_full/bundle_menus_assets_all/GameObject/`
> ＋ `cpp2il/out_cpp2il_dev_ilspy/{ExpansionPassPremiumDrawer,ItemDrawer}.cs`
> **没跑 Unity · 没动 git · 没改源码/账/正本**（只写了本文件）

---

## §1 原版 prefab 定位

| 项 | 值 |
|---|---|
| **bundle** | `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（`GameObject/Icon Expansion Pass Premium Drawer Variant*.json` 现读命中 5 份 —— 1 份 prefab 根 + popup 里的内联实例） |
| **prefab 文件** | `ExportedProject/Assets/Icon Expansion Pass Premium Drawer Variant.prefab`（1462 行） |
| **根 GO / RT** | GO `1709254077376921` · RT `224372920208702427` |
| **组件（根）** | `AspectRatioFitter`（m_AspectMode **2** / m_AspectRatio **0.66**，guid `d3e719b5…`=UnityEngine.UI，fileID `-1254083943`）· `ItemDrawerComponents`（guid `26d6053a…`）· **`ExpansionPassPremiumDrawer`**（guid `fe981cef…`） |

**一共 3 个生产实例**（`Shell/BaseOfferPopup.cs` 的抽屉表）：

| 行 | 宿主 popup prefab | 槽（实例 GO 名） | 实例 RT | 实例 RT 实读 |
|---|---|---|---|---|
| `:473` | `General Basic Offer Popup Variant Expansion pass`（GeoSmall） | `Icon Expansion Pass Premium Drawer Variant Variant`（**多一个 Variant = 实例改名**；库里无 `…Variant Variant.prefab`） | `224612649490615443` | aMin(0,0) aMax(1,1) · ap **(−165,−244)** · sd **(0,−235.5968)** · **localScale 1.4** · rot identity |
| `:565` | `…Variant Premium_Booster_avatar_cardback_title_resource`（GeoBig） | `Icon Expansion Pass Premium Drawer Variant` | — | aMin(0,0) aMax(1,1) · ap (−275,−206.00002) · sd (0,19.322388) · **scale 1** |
| `:573` | `…Variant Premium_Resource`（GeoBig） | `Icon Expansion Pass Premium Drawer Variant` | — | aMin(0,0) aMax(1,1) · ap (−530,−226.31) · sd (0,271.56) · **scale 1** |

> 三个实例的**内部子树逐字相同**（Content 712×1080、Campaign Glow 同一组 anchors/sizeDelta/sprite、`background: 0`）。
> 父窗口：GeoSmall = 1128.5542×663.2962（= 我们 `Geo.Window` `(395.72,188.35)-(1524.28,851.65)` ✅）；GeoBig = 1426.12×720.84 ✅。

---

## §2 节点表（prefab 本体 · 全部数值为序列化实读）

树（只有 1 条主干；`Converted` / `Ephemeral` 两棵出厂 `m_IsActive: 0`，不进画面）：

```
Icon Expansion Pass Premium Drawer Variant      (根)  active 1
└ Content                                       active 1
  ├ Campaign Glow                               active 1   ← 从 0 号子件开始，**在 Image 之下**
  ├ Image                                       active 1
  ├ Converted Drawer                            active **0**
  │ └ Price Display → { icon, text, AlreadyOwned }
  └ Ephemeral Drawer                            active **0**
    └ Price Display → { icon, text }
```

| 节点 | 父链 | 锚点 / 尺寸（原版单位） | 设计 px（`Content` 左上原点 · y 向下 · ×108 得 1080p px） | sprite | 颜色 / 材质 | 显隐 | 绘制顺序 |
|---|---|---|---|---|---|---|---|
| **根** | — | aMin(0,0) aMax(1,1) · sd(0,0) · pivot .5/.5 · scale 1 | 由实例框决定；`ARF(2, 0.66)` ⇒ **宽 = 高 × 0.66** | — | — | active 1 | — |
| **Content** | 根 | aMin=aMax=(0.5,0.5) · sd **(712, 1080)** · scale **(0,0,0)**（`UIScaleToFit{fitInParent:1,targetRect:根,padding:0}` 运行时算，⛔ 不是 bug） | 712 × 1080，居中 | — | — | active 1 | — |
| **Campaign Glow** | Content | aMin **(0, 0.4)** · aMax **(1, 0.8)** · aPos (0,0) · sd **(0.017456, −0.028564) ≈ 0** · scale 1 | **(0, 216) – (712, 648)** = 712 × 432 | **`40K_general_icon_Expansion Pass XP`**（guid `6c77cbd6b01d7c549952437eb07de765`，图集 256² 切片） | `m_Color` 白 (1,1,1,1) · `m_Type 0 Simple` · **`m_PreserveAspect 1`** · `m_PixelsPerUnitMultiplier 1` · material null · raycastTarget 1 | active 1（**恒显**） | 子件 **#0** ⇒ 在 Image **之下** |
| **Image**（= `ItemDrawerComponents.image` / `ExpansionPassPremiumDrawer.icon`） | Content | 同 Campaign Glow：aMin(0,0.4) · aMax(1,0.8) · aPos(0,0) · sd **(0,0)** · scale 1 | **(0, 216) – (712, 648)**（与横带**同框**） | `m_Sprite {fileID: 0}` = null（运行期 `icon.sprite = item.itemIcon.Load()` 灌） | 白 · Simple · **PA 1** · PPU 1 | active 1；`ExpansionPassPremiumDrawer` **无 `RuntimeKeyIsValid` 守卫** ⇒ 取不到就赋 null（图消失） | 子件 **#1** ⇒ 压在横带之上 |
| Converted Drawer | Content | aMin(.05,.15) aMax(.95,.25) · aPos(0,−126) · sd(−133.8386,−22.5571) | 506.96 × 85.44 @ 中心 (356, 990) | guid `22d4706b…` | — | **active 0** | 子件 #2 |
| Ephemeral Drawer | Content | 同上 | 同上 | — | — | **active 0** | 子件 #3 |

**槽位（`ItemDrawerComponents` guid `26d6053a…` · 根上）实读**：

```
background: {fileID: 0}   ← 🔴 **null**（本 prefab 没有 background 槽，也没有名叫 Background 的节点）
image:      114632457200066912  = Content/Image
label: {fileID: 0}        quantity: {fileID: 0}
premiumHighlight: 0       premiumBadge: 0        claimedWarning: 0
conversionDisplay → Converted Drawer GO · convertedItem → 它 Price Display 的组件 · convertedLabel → AlreadyOwned 的 TMP
ephemeralDisplay → Ephemeral Drawer GO · ephemeralText → 它 Price Display 的组件
```

**`ExpansionPassPremiumDrawer`（guid `fe981cef…`）实读**：`showItemInfoOnClick: 1` · `inspectSound`=远端 cue · `icon` → `Content/Image` · **`armyText: {fileID: 0}` = null**。

**行为（`out_cpp2il_dev_ilspy/ExpansionPassPremiumDrawer.cs`）**：

```csharp
protected override void Draw(ExpansionPremiumItem item, int quantity = 1) {
    if ((bool)icon)     icon.sprite = item.itemIcon.Load();          // 无 RuntimeKeyIsValid 守卫
    if ((bool)armyText) armyText.text = GameStaticData.CardArmyToString(item.cardArmy);   // armyText = null ⇒ 整句跳过
}
```
⇒ **`Draw` 只碰 `icon` 一颗**：`background` 槽为 null、`Campaign Glow` 是 prefab 自带的**静态层**（不被任何方法写）。

**与同族 4 份的对照（决定本条为什么不能套别人）**：

| prefab | `Background` 节点 | `background` 槽 | `Campaign Glow` | 横带几何（Content 设计 px） | 横带 sprite | `label` / `armyText` |
|---|---|---|---|---|---|---|
| **`Icon Expansion Pass Premium Drawer Variant`** | **没有** | **null** | 有 | **(0,216)-(712,648)** | **`40K_general_icon_Expansion Pass XP`** | **0 / 0** |
| `Expansion Pass Premium Drawer` | 有 | Background | 有 | (−114.66,146.46)-(826.66,717.54)（sd 229.31/139.08） | **`40k_campaign_Expansion Pass`** ⚠️ | 有 / 有 |
| `Premium Drawer` | 有 | Background | 有 | (−31.26,197.05)-(744.06,666.95) | `40K_genearl_icon_Campaign points_big` | 有 / 有 |
| `Icon Premium Campaign Drawer Variant` | 没有 | **Campaign Glow** | 有 | (−31.26,197.05)-(744.06,666.95)（Content 拉伸 712.8） | `40K_genearl_icon_Campaign points_big` | 0 / 0 |
| `Icon VIP Premium Campaign Drawer Variant` | 没有 | Campaign Glow | 有 | 铺满 1080² | `40K_genearl_icon_Campaign points_big` | 0 / 0 |
| `Premium Drawer No Glow` | 有 | Background | **没有** | — | — | 有 / 有 |

> 📌 三条**互不相同**的量：**槽归谁**（null / Background / Campaign Glow）· **横带几何** · **横带 sprite**。三份都不同 ⇒ 不能拿一组的常量套另一组。

---

## §3 我们现状（3 个槽）

调用链（**已接线**，`B108_A15` 落的）：`BaseOfferPopup.BuildDrawers`（`Shell/BaseOfferPopup.cs:1110`）→ `ItemDrawer.WithPremiumLayers(st, d.Name)`（`:1130`）→ `ItemDrawer.PremiumLayersOfSlot(name)` → `PremiumBody`（`Shell/ItemDrawerPremium.cs:49`）。
`BuildDrawers` 的 style 里 **`st.NamePx = 0f` / `st.QuantityPx = 0f` / `st.IconFill = 1f`**（`:1119-1121`）。

`PremiumLayersOfSlot("Icon Expansion Pass Premium Drawer Variant[ Variant]")`（`ItemDrawerPremium.cs:179-192`）**四条分支都不命中** ⇒ 返回 `null` ⇒ `layers = PremiumLayerFlags.Original`（`:157-158`）= **只画 `Background`、不画横带**。

| 层 | 原版 | 我们现在 | 差异 |
|---|---|---|---|
| `Content` | 712×1080（USF 缩进根） | `DrawerContent`（`ItemDrawerLayout.cs:113`）画成 `fr.Map(0.4,0,712.4,1080)` —— 712.8 基准 | ✅ 等价（0.11%） |
| **`Background`** | **没有这个节点** | `DrawerBackground`（`:128-145`，QBoard=3093）画整卡底板 `40k_Rewards_icon_base card`（`ItemDrawerPremium.cs:62`） | ❌ **多画一整层** |
| **`Campaign Glow`** | 有，恒显，(0,216)-(712,648)，sprite `40K_general_icon_Expansion_Pass_XP`，PA=1 | **完全不画**（`layers.CampaignGlow == false`，`ItemDrawerPremium.cs:68`） | ❌ **整层缺失** |
| `Image`（item icon） | (0,216)-(712,648) · PA 1 | `DrawerImage(fr.Map(0.4,216,712.4,648), …, keepAspect:true)`（`ItemDrawerPremium.cs:72`） | ✅ 等价（<0.2 px） |
| 阵营名 | `armyText` 槽 = **null** ⇒ 原版不画 | `PremiumBody(armyText:true)`，但 `DrawerArmyName` 有 `if (st.NamePx <= 0f) return;` 闸（`ItemDrawerLayout.cs:147`），本族 `NamePx=0` | ✅ 结果一致（**靠 style 兜的，不是靠判据**，见 §6-3） |
| 名字/数量 | `label` / `quantity` 槽 = **null** ⇒ 连 `Label` 节点都没有 | `DrawerNameQuantity(hasName:true, …)` 会建一个**空 `Label` 节点**（`:165`），但 `st.NamePx=0` ⇒ 不画字 | ⚠️ 多一个**不可见**的空节点 |
| 槽矩形 | 见 §1；**`:473` 那格带 `localScale 1.4`** | `d.R` = 282.28×427.7（= **缩放前**的 ARF 结果），`MenuDraw.Node` 不写 scale | ❌ **`:473` 小了 1.4 倍**（见 §6-1）；`:565/:573` scale=1 ⇒ ✅ |

---

## §4 施工单

### 4.1 先补图（否则横带画不出来）

`40K_general_icon_Expansion Pass XP` **不在 `Resources/`**（现读：`Resources/Art/ui_menu/` 906 张里 0 命中；源图有两份：
`Assets/CardPresentation/Art/原版/0_mainmenu/40K_general_icon_Expansion_Pass_XP.png`（50,372 B）
与解包缓存 `d:/2/Warpforge_tools/data/ui_extract/atlasindividual_assets_0_mainmenu/Sprite/40K_general_icon_Expansion Pass XP.png`）。

`工具/import_original_art.py` 的 `MENU_IMAGES`（`:523` 起）**加一行**（与同族 `40k_OfferBadge` 完全同形）：

```python
    # 🆕 2026-10-31（B109_R4）：高级抽屉 `Campaign Glow` 的出厂图。
    #   判据 = `Icon Expansion Pass Premium Drawer Variant.prefab` 的 `Content/Campaign Glow.m_Sprite` guid 6c77cbd6…；
    #   落盘名按 `CardArt.MenuUi` 惯例「空格→下划线」= `40K_general_icon_Expansion_Pass_XP`。
    ('40K_general_icon_Expansion Pass XP',    'atlasindividual_assets_0_mainmenu'),
```

跑法：`python -I -X utf8 d:/4/Unity/工具/import_original_art.py --check`（先看源在不在）→ 去掉 `--check` 再跑一次。

### 4.2 `Shell/ItemDrawerPremium.cs`（4 处，都是**追加/局部替换**）

**① `PremiumLayerFlags` 加一格 sprite**（放在 `GlowX2/GlowY2` 那行之后，`ItemDrawerPremium.cs:153` 后）：

```csharp
            /// <summary>`Campaign Glow` 的图（`CardArt.MenuUi` 名 · 空格→下划线）。`null` = 用 `PremiumGlowSprite`
            /// （= `40K_genearl_icon_Campaign_points_big`，只服务 `Premium Drawer` 那两档）。</summary>
            public string GlowSprite;
```

**② 新增一条预设**（放在 `NoBaseWithBand` 之后，`:168` 后）：

```csharp
            /// <summary>`Icon Expansion Pass Premium Drawer Variant`：**没有** `Background` 节点、`background` 槽 = null；
            /// 只有一条与 `Image` **同框**的 `Campaign Glow`（aMin(0,0.4) aMax(1,0.8) sd≈0 ⇒ (0,216)-(712,648) · PA=1）。
            /// 判据 → `判据/B109_R4_ExpansionPassPremium.md` §2（prefab 直读 + 3 个实例复核）。</summary>
            public static readonly PremiumLayerFlags NoBaseWithExpansionGlow =
                new PremiumLayerFlags { BaseBackground = false, CampaignGlow = true,
                                        GlowX1 = DrCx1, GlowY1 = DrImgY1, GlowX2 = DrCx2, GlowY2 = DrImgY2,
                                        GlowSprite = "40K_general_icon_Expansion_Pass_XP" };
```
> `DrCx1/DrImgY1/DrCx2/DrImgY2`（`ItemDrawerLayout.cs:65-67`）就是横带那一框，**与 `Image` 同一组常量** ⇒ 天然保证「同框」。prefab 是 712 基准、我们是 712.8 基准，差 0.11%（K=0.396 时 0.16 px），可忽略。

**③ `PremiumLayersOfSlot` 加一条分支**（放在 `Icon Premium Campaign Drawer Variant` 那条**之前/之后皆可**，两条前缀不重叠）：

```csharp
            if (slotName.StartsWith("Icon Expansion Pass Premium Drawer Variant", System.StringComparison.Ordinal))
                return PremiumLayerFlags.NoBaseWithExpansionGlow;
```

**④ `DrawCampaignGlow` 按预设取图**（`ItemDrawerPremium.cs:210-219`，把 `PremiumGlowSprite` 换成解析后的名字）：

```csharp
            string name = string.IsNullOrEmpty(lay.GlowSprite) ? PremiumGlowSprite : lay.GlowSprite;
            var tex = Tex(name);
            if (tex == null)
            {
                Note("premium|glow|" + name, "… `" + name + "` 在 `Resources/` 里取不到 …");
                return;
            }
            MenuDraw.Rect(content, tex, fr.Map(lay.GlowX1, lay.GlowY1, lay.GlowX2, lay.GlowY2),
                          NodeCampaignGlow, st.QBoard, null, true, st.Clip, st.ClipSoftness);   // keepAspect = prefab 的 PA=1
```

### 4.3 `Shell/BaseOfferPopup.cs:473` —— 补 `localScale 1.4`

那个实例（`General Basic Offer Popup Variant Expansion pass` 里的 `…Variant Variant`）RT 是 **`m_LocalScale: 1.4`**（三个 popup 实例里**只有它有**；另两处 + 同 popup 其余 10 个槽全是 1）。
`d.R` 是 ARF 之后的**缩放前**尺寸，`MenuDraw.Node` 不写 scale ⇒ 我们那格小了 1.4 倍。

改（**同一物理行、零行数变化**）：把 `R` 换成放大 1.4、中心不变的值：

```csharp
R = new PxRect(597.40f, 464.61f, 992.60f, 1063.39f)      // 旧 (653.86,550.15)-(936.14,977.85) × 1.4（中心 (795,764) 不变）
```

### 4.4 验收顺序
`typecheck` →（宿主未变，只需跑动了共用件的宿主）→ 真机截「Expansion pass」那扇弹窗看横带。

---

## §5 断言要点 + 还查不到的部分

**建议加进 `Editor/B108_R6_Assert.cs`（或新 `B109_R4_Assert.cs`），照 C1 那组的写法：**

| # | 断言 | 灭自证（怎么才会红） |
|---|---|---|
| E1 | `PremiumLayersOfSlot("Icon Expansion Pass Premium Drawer Variant")` 非 `null` | 分支被删 / 前缀打错 ⇒ null ⇒ 红 |
| E2 | 该档 `BaseBackground == false` | 改回 `Original`（= 画底板）⇒ 红 |
| E3 | 该档 `CampaignGlow == true` | 同上 ⇒ 红 |
| E4 | 四边 == `DrCx1 / 216 / DrCx2 / 648`（±0.01） | 抄成 `Expansion Pass Premium Drawer` 的 −114.66/146.46/826.66/717.54 ⇒ 红 |
| E5 | `GlowSprite == "40K_general_icon_Expansion_Pass_XP"` | 抄成 `PremiumGlowSprite`（Campaign points big）⇒ 红 |
| E6 | `PremiumLayersOfSlot("Icon Expansion Pass Premium Drawer Variant Variant")` 与 E1 **同值**（实例名多一个 `Variant` 也认得出） | 用 `==` 而不是 `StartsWith` ⇒ 红 |
| E7 | `PremiumLayersOfSlot("Expansion Pass Premium Drawer")` 仍是 `BaseWithWideBand` 且 `GlowSprite == "40k_campaign_Expansion_Pass"`（见 §6-2） | 把本条的预设套到它身上 ⇒ 红 |
| E8 | `BaseOfferPopup.Variants` 里 `Expansion pass` 那格的 `Icon Expansion Pass Premium Drawer Variant Variant` 矩形 == (597.40,464.61,992.60,1063.39) ±0.1 | 改回 ×1 那组 ⇒ 红（**灭自证**：另两处同槽名必须是 440.75… / 102.51… ⇒ 不可能同时满足） |

**还查不到 / 没验的：**

1. **横带的实际观感**（PA=1 下 256² 图在 712×432 框里居中成 432×432）—— 判据已定，**只差真机截图**（铁律 12 暂停期）。
2. **`UIScaleToFit` 的运行时数值**：序列化 `Content.localScale = (0,0,0)`，值由脚本在 OnEnable 算（`fitInParent:1` + `padding 0` ⇒ 把 712×1080 内接进根框）。我们的 `FrameOf(K = min(W/712.8, H/1080))` 是它的等价物 —— **但没有逐字读 `UIScaleToFit__OnEnable/__SetLayoutHorizontal` 的体**（族内已知：它的产物首行是 `UIScaleToFit__Start`，属 ICF 折叠，语义一致）。
3. **`item.itemIcon` 的运行时真值**（哪些 ExpansionPremiumItem 有图）—— 远端商店数据，本机没有。
4. **`Content` 宽 712 还是 712.8**：本 prefab 序列化直读是 **712**（`Premium Drawer` / `Icon Premium Campaign Drawer Variant` 也是 712 / 拉伸），我们全族用 712.8 —— 0.11% 差，未见任何副作用，**不建议改**（改了要动共用件）。

---

## §6 顺手发现（只报，未改）

1. 🔴 **`:473` 那个槽少了 `localScale 1.4`**（§4.3）。这是**唯一**带非 1 缩放的抽屉实例；因为其余 10+ 个槽都是 1，历史上按「矩形 = 序列化算出来的那个」建模就看不出来。**未跑实况**（该弹窗由服务端报价驱动，本地 `runtime_ui_dump_*` 里没有它）。
2. 🔴 **`Expansion Pass Premium Drawer` 的横带 sprite 我们是错的**：`B108_A15` 的 `BaseWithWideBand` 让它共用 `PremiumGlowSprite`（`40K_genearl_icon_Campaign points_big`），而该 prefab 的 `Campaign Glow.m_Sprite` guid `d2dc932d…` = **`40k_campaign_Expansion Pass`**（`Resources/Art/ui_menu/40k_campaign_Expansion_Pass.png` **已在**）。
   建议：`BaseWithWideBand` 加 `GlowSprite = "40k_campaign_Expansion_Pass"`。⚠️ 该档目前**零生产调用点**（B108_A15 §六-3），属**潜伏错**。
3. ⚠️ **`label` / `armyText` 槽为 null 这件事，我们现在是靠 `st.NamePx = 0f` 兜住的**（`BaseOfferPopup.BuildDrawers:1121`），不是靠判据。同族 `Icon Premium Campaign Drawer Variant` / `Icon VIP …` 同样 label=0 / armyText=0。
   一旦哪个调用方给 popup 抽屉设 `NamePx > 0`，就会**画出原版没有的阵营名 / 名字**。建议在 `PremiumLayerFlags` 里再加 `HasLabel` / `HasArmyText` 两位（本档与 `NoBaseWithBand` 置 false，`BaseWithBand` / `BaseWithWideBand` 置 true），由 `PremiumBody` 传给 `DrawerArmyName` / `DrawerNameQuantity`。**这是结构性问题，不属本次两层范围**，交调度台定价次。
4. ✅ `40K_general_icon_Expansion Pass XP` 的**源图本地有两份**（§4.1），`ExpansionPassEventWindow.cs:909-914` / `:1551` 也一直在报「未导入 ⇒ 不画」—— 补上 4.1 那一行会**顺带**让事件窗那颗 `Campaign Glow` 也能画（那边的画法另议，不要与本条混）。
5. 📌 建议**就地订正** `资料/完全复刻/报告/B108_A15.md` §五-2 的说法「`Icon Expansion Pass Premium Drawer Variant` 的两层没判据 ⇒ 保持旧形状」——本文件已给出判据，**旧形状（画 `Background`）是错的**。
