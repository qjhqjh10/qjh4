# B115_W16 · 活动 `EventComponents` GUID → prefab 施工单（ACT-04 的正本）

> **输入** = `判据/B88_P1两条判据.md` §3.3（10-27 建的 **6 行**成表）＋ `判据/B115_旧任务对账_D_活动工具.md` 的 **ACT-04**（记「8 个 GUID 里的 6 个」）。
> **本文件干什么** = 补上缺的 **3 个 GUID**，并把 **9 条并成一张表**（那 6 行的值本轮**重跑复核、逐字一致**，所以本表可直接当施工单用；§3.3 那份仍留作证据来源，⛔ 别再抄第三份）。
> **口径** = 资源现读，**2026-11-01 · B115_W16**。⛔ 未跑 Unity、未动 git、未改任何源码/账/正本（本批唯一代码改动 = `Shell/LiveOpsHandler.cs` 的 ACT-01）。
> 🔴 **判根** = `D:/2/tools/assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里 **`m_Father: {fileID: 0}`** 的那个根。
> ⛔ **不读 `assets_full` 的同名 GameObject 定根** —— 同名常有多份副本（`Icon Campaign Points Drawer Variant` 有 36 份，见 CLAUDE.md 2026-11-01 那条）。

---

## 一、总表（9 行 = 13 份本地 SO 里出现的 **全部** component GUID）

| # | label（`LiveOpsComponentRef`） | GUID | `m_PathID` | prefab 根名（`m_Name` 实读） | 根上脚本（原版类） | 我们那边 | 谁在用（13 份 SO） |
|---|---|---|---|---|---|---|---|
| 1 | **10 `MainWindow`** | `9a26697357770d8458298d89467fd459` | `-3290546486736551612` | **`Parent Event Window`** | `BaseParentEventWindow`（+`TabbedWindowComponents`） | `Shell/ParentEventWindow.cs`（类 `ParentEventWindow`）· 根名常量 `WindowsManager.PrefabRefParentEvent`（`WindowsManager.cs:820`）· 开窗 `WindowsManager.OpenParentEvent`（`:1083`）/`OpenEventHub`（`:1164`） | `DarkZealots`(317) · `ExpansionOrks1`(337) · `ExpansionUM1`(400) · `ECRelease`(465) · `SWRelease`(530) —— **5 份** |
| 2 | **11 `Tab`** | `e6914cbdd45814f5f9bd40a854c12218` | `-555883983194387681` | **`Expansion Pass Tab`** | `ExpansionPassEventTab` | `Shell/ExpansionPassEventWindow.cs:160`（类 `ExpansionPassEventWindow`）· 根名常量 `WindowsManager.PrefabRefExpansionPassEvent`（`WindowsManager.cs:851`） | 4 份 `*Scoring`（379 / 444 / 506 / 572） |
| 3 | **35 `MainMenuContainer`** | `b28a4a686d3cc4366af8977a6ab69b5e` | `541940618620306034` | **`Base Game Mode Container 1x2 Raid or Army Release`** | `RaidOrArmyReleaseMenuContainer`（+`EverguildButton` · `FlexibleLayoutSizeOption` · `CustomLocalizeCallback`） | ⚠️ **没有同名类**：等价 = `MainMenuRuntime` 手建的「模式卡」+ 本 GUID 的容器表 `MainMenuContainerPrefabByGuid`（`MainMenuRuntime.cs:4099-4107`，🔶 那 5 张卡 ↔ 哪场活动由注入口给，见该处 doc） | `ExpansionUM1`(402) · `SWRelease`(532) —— **2 份** |
| 4 | 20 `TriggerPopup` | `e4c251967ff1a79468e8bd25b86b15b8` | `-710285199457649189` | `Base Event Popup` | `BaseEventPopup` | `EventPopupVariant.BaseEvent`（`Shell/BaseEventPopup.cs:64`）· 装配 `LiveOpsHandler.cs:1551` | `DarkZealots`(318)（`enabled=0`） |
| 5 | 20 `TriggerPopup` | `44c1eb3a347b840fd9e2e67ba85b7d02` | `-112613514281772147` | `Full Screen Event Announcement Variant` | `BaseEventPopup` | `EventPopupVariant.FullScreenAnnouncement`（`:72`）· 装配 `:1552` | 4 份 Parent（338 / 401 / 466 / 531） |
| 6 | 21 `ExpirePopup` | `be526b22c0924f54991b776d86261fed` | `5592865984204753315` | `Base Expiration Popup` | `BaseEventPopup` | `EventPopupVariant.BaseExpiration`（`:74`）· 装配 `:1553` | 4 份 `*Scoring`（380 / 445 / 507 / 573） |
| 7 | 30 `HelpPopup` | `2d7ec1a46a19245798bf28a5d1a030bb` | `5459663361566770346` | `Event Help Popup Variant` | `BaseEventPopup` | `EventPopupVariant.EventHelp`（`:66`）；⚠️ **不经** `LiveOpsPopupPresenter` 那条缝，走 `LiveOpsEventWindow.OpenHelp`（`Shell/LiveOpsEventWindow.cs:1531`） | 4 份 `*Scoring`（381 / 446 / 508 / 574） |
| 8 | 12 `Popup` | `3c7a2561b7c174784a9e6f1a97231492` | `-1012951434983057977` | `General Basic Offer Popup Variant Expansion pass` | `BaseOfferPopup`（+`GeneralOfferPopupDrawer`） | `BaseOfferPopup.Variants` 的一行（`Shell/BaseOfferPopup.cs:463`） | 4 份 `*_Premium_Item_Offer`（358 / 421 / 485 / 551） |
| 9 | 0 `Container` | `a033c0b722070408bb23fe113258b894` | `6912214828090912446` | `General Basic Offer Container Variant Single Item Type` | `ShopOfferContainer`（+`GeneralOfferPopupDrawer` · `FlexibleLayoutSizeOption`） | `OfferContainer` 的一个变体（`Shell/OfferContainer.cs:639`） | 4 份 `*_Premium_Item_Offer`（357 / 420 / 484 / 550） |

（「谁在用」列的行号 = `Shell/LiveOpsLocalCatalog.cs` 里那条 `C(...)` 的行号。）

**三行加粗 = 本批新补的 3 个（ACT-04 的缺口）**；其余 6 行与 `判据/B88_P1两条判据.md` §3.3 **逐字一致**（本轮重跑，见 §三）。

---

## 二、三个新补的（逐条带可复跑证据）

### ① `9a26697357770d8458298d89467fd459` → `Parent Event Window`（label **10**）

```
GUID 9a266973… → m_PathID -3290546486736551612     （AssetBundle_1.json 的 m_Container，297 条里的 1 条）
   → GameObject 'Parent Event Window'
     （见证组件 = RectTransform_5398495985660100932，它的 m_GameObject.m_PathID = -3290546486736551612）
   → Assets/Parent Event Window.prefab 的根：m_Father {fileID: 0}，m_Name = 'Parent Event Window'
   → 根上脚本 BaseParentEventWindow.cs（+ TabbedWindowComponents.cs）
```
* **它同时是 ACT-01 的前置件**：`LiveOpsEvent.SelectEvent()` 恒用 label 10（`Shell/LiveOpsEvent.cs:1334`）⇒ 5 份 Parent 事件点下去都落到这一扇。

### ② `e6914cbdd45814f5f9bd40a854c12218` → `Expansion Pass Tab`（label **11**）

```
GUID e6914cbd… → m_PathID -555883983194387681
   → GameObject 'Expansion Pass Tab'（见证组件 = RectTransform_-8590421974419862753）
   → Assets/Expansion Pass Tab.prefab 根 m_Father {fileID: 0}，m_Name = 'Expansion Pass Tab'
   → 根上脚本 ExpansionPassEventTab.cs
```
⚠️ **`Tab` 不是窗口**：它是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的页（我们建的是「承载窗外壳 + 恰好一页」的等价形态，见 `Shell/ExpansionPassEventWindow.cs` 文件头）。⛔ 别把它当 `MainWindow`。

### ③ `b28a4a686d3cc4366af8977a6ab69b5e` → `Base Game Mode Container 1x2 Raid or Army Release`（label **35**）

```
GUID b28a4a68… → m_PathID 541940618620306034
   → GameObject 'Base Game Mode Container 1x2 Raid or Army Release'（见证组件 = RectTransform_7695131988882162290）
   → Assets/Base Game Mode Container 1x2 Raid or Army Release.prefab 根 m_Father {fileID: 0}
   → 根上脚本 RaidOrArmyReleaseMenuContainer.cs（+ EverguildButton / FlexibleLayoutSizeOption / CustomLocalizeCallback）
```
⚠️ **它不是窗**：原版 `LiveopsMainMenuData` 带的是「**主菜单容器** prefab 引用」⇒ 这是**主菜单里那张模式卡**的容器模板，不是 `OpenWindow` 的目标。我们那 5 张模式卡是手挑的（🔶 如实标），这一格只用于 `MainMenuContainerPrefabByGuid`。

---

## 三、怎么复跑（三步，全是只读）

**① GUID → `m_PathID`**：读 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的 `m_Container`。
🔴 **结构记准**（旧文档没写、照猜会读出来 0 条）：它是**列表**，每项是 **`[<guid:32hex>, {preloadIndex, preloadSize, asset:{m_FileID, m_PathID}}]`** —— **不是** dict、**不是** `{first, second}`。现读 **297 条**。

**② `m_PathID` → GameObject 名**：`GameObject/*.json` **不存自己的 pathID**（文件名就是 `m_Name`），所以要**建反向索引**：
对每个 `GameObject/*.json` 读 `m_Component[].component.m_PathID`，查 `RectTransform/RectTransform_<pid>.json`（**文件名就是 pid**），读它的 `m_GameObject.m_PathID` ⇒ 那就是这个 GameObject 的 pathID。
（现读：16,768 个 GameObject 里有 **16,510** 个带 RectTransform，够覆盖全部 UGUI prefab。）

**③ 定根 + 定类**：在 `D:/2/tools/assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里找 `m_Father: {fileID: 0}` 的 Transform，取持有它的 GameObject 的 `m_Name`（= 判据根名）；根上的 `MonoBehaviour` 的 `m_Script = {fileID:11500000, guid:<32hex>, type:3}` → 在导出工程里找 `<guid>.cs.meta`（现读 **1,902** 个）→ 脚本文件名 = 原版类名。

> 本轮的三个一次性脚本在 `d:/4/_tmp_view/b115_w16/`（`guid_to_prefab.py` / `go_pid_index.py` / `prefab_root.py` / `prefab_root_scripts2.py`）—— scratch 目录，**别当正本依赖**；算法就是上面三步，重写一遍不到 50 行。

**交叉验**：`grep -o "C(LiveOpsComponentRef\.[A-Za-z]*, *\"[0-9a-f]\{32\}\")" Shell/LiveOpsLocalCatalog.cs | grep -o '[0-9a-f]\{32\}' | sort -u | wc -l` 现读 = **9**，且本表 §一 的 9 个 GUID 能一一对上。

---

## 四、订正（铁律 5）

1. 🔴 **`判据/B115_旧任务对账_D_活动工具.md` 的 ACT-04 行写「13 份 SO 一共只出现 8 个不同 GUID」—— 现读是 9 个**，而且它**同一行列出来的就是 9 条**（`MainWindow` / `Tab` / `MainMenuContainer` / `Container` / `Popup` / `TriggerPopup`**两个** / `ExpirePopup` / `HelpPopup`）⇒ **「8」是笔误**（错因：`TriggerPopup` 的 2 个 GUID 在那一行里被数成 1 个）。
2. 🔴 同一行的「怎么验」写 `grep -o '[0-9a-f]\{32\}' Shell/LiveOpsLocalCatalog.cs | sort -u | wc -l` **应 = 8** —— **这个命令不对**：它把 `EventAssets` 的 GUID 也数进去了，现读 = **36**。要用上面的 `C(LiveOpsComponentRef…)` 过滤版。
3. 🔴 `判据/B88_P1两条判据.md` §3.3 的表头写「label → GUID → prefab 根名 → 我们已有的枚举」，**覆盖 6 个 GUID**（不是「8 个里的 6 个」——当时分母就写错了）。本文件把分母改成 9。

---

## 五、还没查清的（如实记，⛔ 不猜）

1. **`MainWindow` 只有 Parent 这一条**：13 份本地 SO 里 Gacha / Raid / Energy 事件**一份都没有**（本地目录只有 `Parent` / `Scoring` / `PremiumItemOffer` / `FastMode` 四种，见 `LiveOpsLocalKind`）⇒ **这三族事件的 `MainWindow` GUID 本地拿不到**。它们的生产入口今天走的是 `MainMenuRuntime` 的模式卡（`:2721` Skirmish / `:2727` Ranked）与 `OpenEventHub`，**不是** `SelectEvent()`。
2. **`RaidOrArmyReleaseMenuContainer` 的字段语义没逐条读**（本批只要「GUID → prefab 真身」这一格）。要复刻它的 `OnInitialize` 得另开一条判据，判据在 `d:/2/tools/decomp_full/RaidOrArmyReleaseMenuContainer__*.c`（本轮**没读**，⛔ 别当成已核）。
3. **根脚本 `d3e719b59ab71ba3f6b398058c866280`** 在 `Base Game Mode Container 1x2 Raid or Army Release` 的根上，但在导出工程里**找不到对应的 `.cs.meta`**（1902 个脚本里没有）⇒ 它是**包内脚本 / 未导出**，本轮判不出类名。⚠️ 不影响第 ③ 行的结论（那一格要的是 prefab 真身）。

---

## 六、与 ACT-01 的关系

`Shell/LiveOpsHandler.cs` 的 `LiveOpsEventEntry`（文件尾，2026-11-01 追加）用的就是本表第 1 行：

```csharp
public const string ParentEventMainWindowGuid = "9a26697357770d8458298d89467fd459";
```

`LiveOpsLocalBoot.OpenEventPopup` 在查 `_popupVariantByGuid`（那张表**只装弹窗** label 20/21）**之前**先判 `IsEventWindowGuid(guid)` ⇒ 命中就 `WindowsManager.OpenEventHub(Parent)` 开 `Parent Event Window`。
⛔ 别把第 1 行塞进 `_popupVariantByGuid` —— 它要的是 `EventPopupVariant`（`BaseEventPopup` 的一个变体），塞不进「一扇窗」。
