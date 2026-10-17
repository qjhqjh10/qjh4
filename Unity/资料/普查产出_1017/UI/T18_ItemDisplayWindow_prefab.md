# T18 · `Item Display Window` 判据（2026-10-17）

> 来源：`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Item Display Window" --depth 8 --relative --md`（现读）
> ＋ `d:/2/tools/decomp_full/ItemDisplayWindow__Open.c` · `ItemDisplayWindowContext__.ctor.c` · `CardbackCollectionTab__OnItemSelected.c`

## 一、为什么现在要建它（T25 的落点）

`CardbackCollectionTab__OnItemSelected.c:17-24`：
```
uVar2 = ItemDisplayWindowContext(item, 1);           // ctor 符号错配成 UpgradeCardSignal___ctor
WindowsManager.OpenWindow<ItemDisplayWindow>(instance, uVar2, 0, …);
```
⇒ **卡背页点一张卡背，原版开的就是 `Item Display Window`**（不是猜）。
`ItemDisplayWindowContext` 字段：`+0x10 = item`（`CosmeticItem`）· `+0x18 = amount`（int，卡背传 1）。
`ItemDisplayWindow__Open`：`ItemDrawer.Draw(item, amount)` → 名称走 `I2 GetTermTranslation(item+0x20)` →
空白则关名称节点 → `FlavourTextSO.GetClanFlavorBackground(army)` 换底图 → `ItemInformationPanel.Initialize(item, …)`。

## 二、prefab 树（现读 · 1920×1080 设计系）

| 层 | 名字 | 矩形 | 组件 / 文字 |
|---|---|---|---|
| 0 | `Item Display Window` | 全屏 | `ItemDisplayWindow`；字段 = `closeButton, closeOnESC, closeSound, extraScaleSmallScreen, flavourTextSo, informationPanel, itemHolder, lorePanel, loreText, openSound, type, updateNavPanel, useDefaultCloseSoundIfNull, windowsPlacement` |
| ·1 | `Menu Dark Background` | 全屏（4574.60×2572.36 外扩） | `Image` + `BackgroundCloseButton` + `BackgroundOverDrawController`；tint **(0,0,0,0.773)** ⇒ 点它 = 关窗 |
| ·1 | `Options` | 0,0→1920,1080 | — |
| ··2 | `Panel` | `1263.53,298.35 → 1656.47,798.35`（392.93×500） | `Image`（`UI_Deck_Selection_Back` 439×664，Sliced 九宫 `0,325,0,35`）+ `ItemInformationPanel`（`descriptionText/nameText/typeText`） |
| ···3 | `Title` | `1302.83,323.35 → 1617.17,373.35` | TMP fs 37 · base 36 · auto[10~37] · Center/Middle · 折行 0 |
| ···3 | `Description` | `1302.83,373.35 → 1617.17,673.35` | TMP fs 35 · base 36 · auto[10~35] · Center/Middle · **折行 1** |
| ···3 | `Type` | `1302.83,673.35 → 1617.17,723.35` | TMP fs 35 · base 36 · auto[10~35] · Center/Middle · 折行 0 |
| ·1 | `Card Display` | `584,106 → 1336,974`（752×868） | — |
| ··2 | `FlavourTextBG` | `300,880.34 → 1620,1058.34`（1320×178） | `Image`（出厂无图）+ `TransformScalerBySmallScreenUI` |
| ···3 | `LoreText` | `335,922.61 → 1585,1016.08`（1250×93.47） | TMP fs 32 · base 36 · auto[12~32] · Center/Middle · **折行 1** |
| ·1 | `Item Holder` | `635,215 → 1285,865`（650×650） | —（`ItemDrawer.Draw` 的宿主） |

## 三、还差什么（实现前要补的判据）

1. `closeOnESC` / `type` / `windowsPlacement` / `extraScaleSmallScreen` / `closeSound` / `openSound` 的**序列化值**（`menu_dump` 只印了字段名、没印窗参值）——
   用 `menu_rect.py` / MB JSON 现读。
2. `ItemInformationPanel` 的 `descriptionText/nameText/typeText` 三格**各自喂什么**（`Initialize` 的方法体在 `decomp_full` 里可读）。
3. `FlavourTextSO.GetClanFlavorBackground(army)` 的**阵营 → 图**映射（本地 SO）。
4. `ItemDrawer.Draw(item, amount)` 的 `item` 是 `CosmeticItem`（卡背）⇒ 我们侧用 `Core/CardbackTable` + `Resources/Cardbacks.json` 的 233 条对接；
   `CosmeticItem.LocalizedName` 走 `Cardbacks/<nameTextReference>`（R7 §A1853① 已查实 157 条）。
