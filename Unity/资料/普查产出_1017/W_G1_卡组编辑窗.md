# W_G1 · 卡组编辑窗（`DeckRuntime.cs`）—— 写手报告（2026-10-17）

> 白名单内实际改动：`Deck/DeckRuntime.cs` · `Editor/DeckScene.cs`（只补/改断言）· **新建 `Shell/TopBar.cs`**。
> ⛔ 没碰：`Core/FilterPanelModel.cs` · `Core/CardView.cs` · `Shell/{DeckSelectionPopup,CardDetailPopup,ImportDeckPopup,MainMenuRuntime}.cs` · `RuleEngine/**` · 两张正本。
> **没跑 Unity**（红线）；改完 `.cs` 每次都跑秒级类型检查，末次 **0/0**。
> 行尾：改完逐个核过（`DeckRuntime.cs` CRLF 5241/5241 · `DeckScene.cs` CRLF 4566/4566；新文件 `TopBar.cs` 随 `Shell/` 目录用 LF）。

---

## 一、逐条结论（原版 / 我们 / 改成什么 / 判据出处）

| # | 原版 | 我们（改前） | 改成什么 | 判据出处 |
|---|---|---|---|---|
| **D36–D38** | 卡组编辑 Rarity 列距 `5`（步进 105）· Cost `7`（72）· Type `m_Padding.Left 40` | 只传了共用那套（收藏窗的 7/15/15） | `FilterPanelModel.Build(..., raritySpX: 5, costSpX: 7, typePadL: 40)` **一行**（缺省常量一个字没动） | `DeckRuntime.cs` 调用点注释 + 模型侧已有常量 |
| **D48** | `Card Cost` 的 TMP **绿 (0.29557,0.77358,0.49591)** · `Cards in deck` **白 (1,1,1)** | 两处都传 `Ink` | 两个新常量 `CurveLbTint` / `CurveNumTint` | ✅ **自己复核过**：`MB 7891025530621652772` → GO `Card Cost`（绿）· `MB 871731656867376932` → GO `Cards in deck`（白），两颗 `fs 25` · `auto=0` · Center/Middle |
| **D34** | `Deck Name` 的 `Text`+`Placeholder`：`HA 1`(Left) · **`VA 8192`** | 居中 | `SetAlignLeft()` + `MenuDraw.SetVAlign(Capline, 框)` + 每帧 `AlignLeftOn(19.5)` | `MB -4659515947641016540`（Placeholder '`Tap to edit deck name`'）· `MB -9162762440952910044`（兄弟 `Text`）。🔴 **订正施工单**：`8192` 是 **Capline(`0x2000`)**，**不是** `Baseline`（`0x800`，本工程 `Label.VAlign` 里也没有那一档） |
| **D43** | 卡背抽屉 `Army Filter/Title` 有 `Army` 小标题 | 没画 | 新建常驻件 `cosmoflt_title`（挂抽屉容器，显隐交给 `ApplyDrawerSlide`） | `HA 1` · `VA 512` · `fs **32**` · **`m_enableAutoSizing = 0`** ← 施工单写的「auto[18..72]」是**死值残留**（自适应关着）。矩形 = 面板内 `(0, CosmoSpacing1)` |
| **D7/D9/D10/D35/D44** | 这 5 类件**原版都没有** | 都画着 | **全删**（含常量 `QPoolInfo=3006`（留空号不复用）· `ClickOrder` 两项 · `HandleButtons`/`KeyLive` 各自的分支 · 自检读数 `UiInfoActionsVisible`） | 施工单 D7/D9/D10/D35/D44 |
| **D45** | `Confirm` = VLG(`MiddleCenter`) 跑完 **x 720.83 · y 569.86..644.86 · 478.343×75** | 写死 `721,611,478,75` | 改成 `720.83 / 569.93 / 478.343 / 75` | `Buttons` RT = 733.9×90 @593.05,562.43 + `VLG(align 4, pad 0, ctrl 0)` + 子件 `478.343×75`。🔴 代码里「y 取 615 会冒出窗口下沿」**是错的**（窗口底 685.93，余量 41.07）—— 注释已订正 |
| **D46** | `Input Field/Text Area` = `HA 1` / **`VA 256`(Top)** | 居中 | `SetAlignLeft()` + `SetVAlign(Top)` + 每帧 `AlignLeftOn(630)` | 按 `m_text == 'Enter text...'` 认出的那颗 `Placeholder`：`HA 1 VA 256 fs 29 auto 0` |
| **D47** | 根下有一颗**整屏** `Background`（`EverguildButton` ⇒ 点窗外关窗） | 只有窗口底板 | 加整屏压暗层（`QModal−1` = 3099 · 黑 `alpha 0.396`，与外壳那份**同一个数**）+ 命中区 `imp_shade`（`ClickOrder` **排最后**） | `menu_rect.py … "Import Deck Popup" --depth 3`：`Background 0.50,0.50 → 1919.50,1079.50` |
| **D33** | `maxCostToDraw(8)` 那一格加后缀 | 没做（后缀串没解出） | 后缀 = **`"+"`**；左列第 8 格印 `8+`；**同时**把「费用 ≥ 8」并进那一桶 | 🔴 `DAT_184275430` → RVA `0x4275430` → `stringliteral.json` = **`"+"`**。🔴 **订正施工单**：`DeckCostQuantityRowDrawer__Initialize.c:99-109` 把后缀加在 **`cardCost`（= `Card Cost`，左列）**上，**不是 `Cards in deck`**（两个字段引用逐颗解出来：`cardsInDeck → GO('Cards in deck')` · `cardCost → GO('Card Cost')`）。「8 及以上」= `DeckEnergyCostDrawer__Initialize.c:150-158` 把费用超行数的**夹进最后一格**（我们原来把 9..20 的**全丢了**） |
| **D20** | `Header/Filters/Icon` = **`40k_bt_icon_search`**（27×27 · PA=1 · 启用） | 也是这张图 | **不改** —— 结论 = 「**原版就是这张图**」 | `menu_dump.py … "Deck Editing Menu" --depth 4` 该行逐字段 |
| **D23** | —— | 卡背缺图**静默**（截图里 2 格空框） | 新口 `CosmeticTexOrWarn`（**一个名字只出声一次**）+ 侧栏抽屉那条链同样出声 | `CLAUDE.md` §三「不许静默失败」；本文件 `Ui()` 那条既有告警是同一形状 |
| **D24** | 两套：桌面 **6 列 262.5×384** / 小屏 **4 列 393.75×576** | 编译期常量写死桌面那套 | 常量 → `PoolMetrics` **属性**（按 `SmallScreenUI.Enabled` 现算）；列数走**原版那条式子** `floor(视口宽 ÷ 格宽)`；换档时重建张数条 + 收起多出来的格子 | `项目任务.md` §三 第 12 条 第 6 项（`_mobileSizeScale = 1.5`）· `Shell/TransformScalerBySmallScreenUI.cs` 的 `SmallScreenUI` |
| **D15** | `Content Area/Background` = **纯色板 + `UIGradient`**（`m_Sprite = null`！）· 矩形 `167.18,70.97 → 1920,1080.03` · `m_color1 (0.2235294,0.0117647,0.0196078)` / `m_color2 (0.0470588,0,0.0156863)` · `m_angle 82` | **纯黑** | 新增 `area_bg`（最低档 `QAreaBg = 2980`），角度按已订正的口径传 **188** | `MB -3308698737451929820` 等 8 颗（本包**全是同一对色同一个角**）+ `Content Area` 的孩子序。实拍采样复核：右上亮 `(33,1,4)`、左下暗 `(24,1,6)` ⇒ 与 188° 自洽 |
| **D21** | 分母 = **`min(拥有,上限)`** | `min(拥有,上限)` | **不改**（我们本来就对） | `DeckEditorCollectionDisplay__DrawCell.c:42-76` 逐句：`:54 if (iVar6 < iVar7) iVar7 = iVar6;` ⇒ 施工单「看着是上限」**不成立**。⚠️ **别**照那条去删 `Mathf.Min` |
| **D22** | 那一帧**不显示**；prefab 里 active，被 `GridLayoutGroup`(85×70) 压到 `592.2` | 常显（250×60 @1218.6） | **不改**（判据不足，见 §三·1） | 见 §三 |
| **D13/D14** | 顶栏整条：左上 头像+玩家名+信封+人形图标；右上 三项资源+齿轮 | **一件都没有** | **新建共用件 `Shell/TopBar.cs`**，`DeckRuntime.BuildTopBar()` 调它 | 逐参照 `MainMenuRuntime` 的 `BuildUpperBar/BuildPlayerProfile/BuildResourcesBar/BuildTopAvatar`（那边每个数带 pid） |
| **附加条** | 行上 `Rarity Gradient` + `Background Border` **两件**按 `CardRarityColorsSO` 上色 | 只有色条，且色值是「我们挑的」 | 两件都上；色值表 = SO 那六档 | `UICardInfoItem__Initialize.c:139-156`（`GetColor(PlayerItem.get_Rarity)` → 逐颗 `set_color`）；`imagesToChangeColorByRarity` 逐颗解出来 = `Rarity Gradient` / `Background Border`；实例逐颗是 `Deck Selector *` 行。档号 ↔ 名字 = `CardRarity.cs`(`None0/Common1/Rare2/Epic3/Legendary4/Special5`) |

## 二、改动清单（含断言）

**`DeckRuntime.cs`**（改 707 行 / 删 148 行）：上表每条各自的落点；新增 `PoolMetrics/Pool`、`CurveCostText/CurveLastBucket/CurvePlusSuffix`、`RarityColorsBySo/RarityTier`、`TintTree`、`CosmeticTexOrWarn`、`BuildAreaBackground/BuildTopBar`、`QAreaBg/QLowest`。
**`Shell/TopBar.cs`（新，326 行）**：`Parts` + `Build` + `RefreshTopAvatarIfChanged`；队列**复用** `MainMenuRuntime.QBar*`。
**`DeckScene.cs`（改 364 / 删 30）**：新增一节 `G1：差异账`（13 组），并在 3 处**改写**旧断言（见 §三·5）。要点：
- 每组的期望值都是**原版字面量**；**删件那一组是反向断言**（`FindDeep(...) == null` **且** `!UiBtnRegistered`）；
- **灭自证**两条：D24 用 `UiPoolCellRect(17)` 一条**结构上不可能两边一起绿**（换档后那 6 格必须消失）；D48 加了一条「两列颜色必须不同」的判别式；
- 新的自检口（都在 `DeckRuntime`）：`UiBtnRegistered` · `UiLastSay` · `UiRowRarityTint` · `UiCosmeticTex(Calls)/UiCardbackArtWarnCount` · `UiPoolColsNow/UiPoolCellWNow/UiPoolCellHNow/UiSetSmallScreenUIForTest` · `UiTopBar/UiRefreshTopAvatar` · `UiQuadTex`。

## 三、没查清的部分（如实）

1. **D22 `Clear filters` 的显隐触发点 = 没查到**（⛔ 本轮**不改**）。三条已核实：prefab 里它 `m_IsActive=True`（全包 11 个同名件）；父容器 `GridLayoutGroup`(cellSize 85×70 · pad 0 · 1 列 · align 3 · CSF h:MinSize) 跑完会把它压成 `85×70 @592.2`；**实拍那一块是空的**（设计坐标 555..765×60..170 逐点采样 = 渐变底 + 分隔线）。`CollectionFilterController<T>` 的 `clearFiltersButton` 字段在**基类桩**里有，但 `decomp_full` 里该类只导出了 ctor + `NotifyFilterChange` 的两个 lambda（只做 `ForceToggle.ForceOff`）；`CardCollectionFilterController` 也只有 ctor + `SetFiltersToDeck`。⇒ **要收这条需要实况**（进原版看那颗钮在「没筛/筛了/清空后」三种状态的 `activeSelf`）。同一条实况也能顺手定**它的位置**（我们按右对齐推的 1218.6 与上面的 592.2 不一致）。
2. **`CardRarityColorsSO.GetColor` 的 fallback 常量 `DAT_1834b2e50` 没解出**（查不到时返回的那组）。我们这边**如实**落「表里第 0 档（白）」并写进注释。
3. **D33 的 `.Count` 口径**：原版 `{0}` = `Enumerable.Count(GetLibraryInFull(), 谓词)`（编辑中卡组里这张卡几张）—— 与我们 `Deck.CountOf` 同义；但原版那段的 live-ops/自定义模式分支（`GetMaxCopiesInDeck` 那一支）**没逐句核**。
4. **D24 的「一屏铺几行」**：原版是 `RecyclableScrollRect` 运行时算的；我们仍是**我们挑的 3 行**（文件头 ③ 那条旧账，本轮没动）。换到小屏档后 3 行 × 576 = 1728 远超视口 924.1（`pool_*` 没有裁切层）—— **这条与桌面档同一个形状**（桌面 3×384=1152 也超），不是本轮引入的，**但确实值得另开一笔**。
5. **`Shell/ImportDeckPopup.cs` 里那句「原版都是 hAlign=Center」是错的**（判据见 D46）—— 那个文件**不在本批白名单** ⇒ 只在本报告与 `DeckRuntime` 注释里订正，**改由 G6 的写手收口**。

## 四、未做完 / 需调度台转派

1. **`MainMenuRuntime` 收口到 `TopBar`（另开一笔）**：本轮按「⛔ 不许改 MainMenuRuntime」的要求**复刻了一份** ⇒ 顶栏参数**两份并存（明账）**。要收口 = 把 `MainMenuRuntime.BuildUpperBar/BuildPlayerProfile/BuildResourcesBar/BuildTopAvatar` 删掉、转调 `TopBar.Build`（并把它那几个 `private` 常量一并搬过来）。
2. **`ShareDeckString()` 失去生产入口**：它原来只挂在删掉的那颗 `info_share` 上。原版这个动作的家在 **`DeckInfoPopup.ShareDeck()`**（`decomp_full/DeckInfoPopup__ShareDeck.c`）⇒ 落点是 `Shell/DeckInfoPopup.cs`，**不在本批白名单** ⇒ 请派给那个文件的写手。方法本体**保留**（`public`，可自检调）并在注释里写明了原因。
3. **顶栏那些钮没接点击**（设置/信箱/挑战/头像）—— 原版它们开的是主菜单那一层的窗；要接得连 `WindowsManager` + 指针层一起搬进 `DeckEditor` 场景。**已如实写进代码注释**。
4. **D2/D3/D4（中文化三处）** —— 按调度台指示**跳过**（等语言表）。
5. **G6 的那半条 D46**（`Shell/ImportDeckPopup.cs` 的 `RefreshInputText` 用的是 `anchor(.5,.5)`，也没 `SetAlignLeft`）—— 同一条判据的第二个落点，**不在本批白名单**。

## 五、类型检查结果（原样贴，末次）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ 中途一次红是**别人正在写一半的 `Battle/CardDisplayWindow.cs`**（`CS0103: _eyeBtn` 三条），
按 `CLAUDE.md` §13·3 判据「错误全集中在不是自己负责的文件上 ⇒ 不是我的问题」，等它写完后重跑即 0。

📌 **`Shell/TopBar.cs` 没有 `TopBar.cs.meta`**（本轮没跑 Unity ⇒ 生成不了）。
没有任何资产按 guid 引用它（新脚本），下次导入时 Unity 会自己补 —— **但若收口时发现「脚本没被编进去」，
先看这个 meta 在不在**。
