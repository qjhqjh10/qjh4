# B8_AP · 卡牌面/卡组编辑/收藏 落在 `Core/**` + `Deck/**` 的 B 行（升 A 收口）

> 会话：第八十二会话（完全复刻 · 批次 8）· 执行代理 **AP**（代号 `B8_AP`）· 2026-10-26
> 白名单：`Unity/MyGame/Assets/CardPresentation/Core/**`（除 `CardView.cs` / `CardFeel.cs` / `CardDatabase*`）· `CardPresentation/Deck/**`
> 账补丁：`资料/完全复刻/账/_parts/B8_AP.tsv`（13 行 = **11 A** + 1 C + 1 D · 11 列 · LF）
> ⛔ 未碰主表 · 未碰 git · 未跑 Unity 批处理 · 未改正本

---

## 〇、先说一件影响「挑行」口径的事实（铁律 5·b：先落盘）

派单写的三个域（`卡牌面_渲染` / `卡组编辑` / `收藏`）**当前 B 行合计只有 11 条，且一条都不落在本代理白名单里**：

| 域 | 当前 B 行 | 落点 | 归谁 |
|---|---|---|---|
| 卡牌面_渲染 | **0 条** | — | — |
| 卡组编辑 | 3 条（252 / 278 / 275 之外的 B 已清） | 全部 `Shell/PrebuiltDecks.cs` | 壳侧代理 |
| 收藏 | 8 条（47 / 142 / 204 / 265 / 266 / 267 / 269 / 297） | `Shell/CollectionTabPage.cs` · `Shell/ItemDrawer*.cs` · 或无方法级落点 | 壳侧代理 |

⇒ 本代理按简报「**只做落点在你白名单里的**」，改从**落点在本白名单**的 B 行里挑。
其中 **6 条（`泛型与启动链_补读` 60/67/69/138/139/141）的 `域` 列本来就是「收藏」** —— 正是本批的主场域（过滤模型落在 `Core/FilterPanelModel.cs` + `Deck/DeckEditorState.cs`）。

---

## 一、结论（数字）

- **真升 A = 11 条**（每条 G0–G8 逐格打勾；见 §二）。
- **B → C = 1 条**（`CardScript.ActivateTraitAnim`：查出一个**真缺口**，见 §六·(a)）。
- **B → D = 1 条**（`ToggleCollectionFilter<T,>.FilterItem`：基类只有抽象声明、无体）。
- **改代码 1 处 / 5 行**（`Core/DraggableController.cs` 的 `Draggable<T>.OnDrag/OnRelease` 由空体补齐为原版的 `SetActive`，另订正一段错注释；**净行数变化 0**）。
- 自查：`typecheck` **运行时 0 / 编辑器 0** · `check_ledger_points`（本次补丁）**A=11 通过=11 漂移=0 失败=0** · `check_a_vs_diff` **强 0 / 弱 6（全部是 G2 该写的机制差异句）** · `verify_annot` **缺注解 0**

| # | 域 | 原版类.方法 | 判据（反编译来源） | 落点 | 观测量 |
|---|---|---|---|---|---|
| 1 | 收藏 | `CollectionFilter<T>.SetupFilter` | 🥇 `decomp_generic_trial/out/CollectionFilter_object___SetupFilter__4d20f0.c:1-9` · 🥈 `CollectionFilter.cs:9-15` | `Core/FilterPanelModel.cs:647-854` + `:865-908` | 点一格后 `Filter` 的集合成员 |
| 2 | 收藏 | `CollectionFilterController<T>.SetupFilters` | 🥇 `…SetupFilters__15f0740.c:1-98` · 🥈 `CollectionFilterController.cs:205` | `Core/FilterPanelModel.cs:647-854` · `Deck/DeckEditorState.cs:56-59` | 出厂全不筛 ⇒ 命中 1128/1128 |
| 3 | 收藏 | `CollectionFilterController<T>.Filter(List<T>)` | 🥇 `…_Filter__15ef810.c:1-60` | `Deck/DeckEditorState.cs:568-641` | UM+legendary ⇒ 31（数据独立数） |
| 4 | 收藏 | `ToggleCollectionFilter<T,>.SetupFilter` | 🥇 `…_SetupFilter__13661f0.c:1-124` · 🥈 `ToggleCollectionFilter.cs:147` | `Core/FilterPanelModel.cs:647-854` · `:865-908` | epic+legendary 并集 ⇒ 428 |
| 5 | 收藏 | `ToggleCollectionFilter<T,>.OnFilterChange` | 🥇 `…_OnFilterChange__13658e0.c:1-68` · 🥈 `ToggleCollectionFilter.cs:260` | `Core/FilterPanelModel.cs:865-908` · `:910-919` · `Deck/DeckEditorState.cs:79-128` | 同上 |
| 6 | 收藏 | `ToggleCollectionFilter<T,>.FilterItem`（**D**） | 🥈 `ToggleCollectionFilter.cs:609`（abstract，泛型清单无体） | —（子类语义 → `DeckEditorState.cs:79-128`） | n/a |
| 7 | 未分类/其他 | `ArmyItem<T>.TryGetItem` | 🥈 `ArmyItem.cs:11-22` · 🥇 `…TryGetItem__19f43b0.c` | `Core/ArmyIcons.cs:89-95` · `:99-103` | 13 条表命中 / `0` 未命中 |
| 8 | 战斗逻辑驱动 | `CardScript.ActivateTraitAnim`（**C**） | 🥇 `decomp_full/CardScript__ActivateTraitAnim.c:1-31` | `Core/TraitFrames.cs:812-854` · `:862-893` | 缺口未修 |
| 9 | 战斗逻辑驱动 | `CardScript.RemoveStatusAnim` | 🥇 `decomp_full/CardScript__RemoveStatusAnim.c:1-12` | `Core/TraitFrames.cs:973-987` · `:989-1004` | 两本字典框数 ⇒ 0/0 |
| 10 | 菜单/外壳 | `BlobShadowController.Start` | 🥇 `decomp_full/BlobShadowController__Start.c:1-45` | `Core/BlobShadow.cs:123-132` · `:80-120` | 纯黑 α0.36078429 / 直径 1.25·2.0567584·0.88586 |
| 11 | 菜单/外壳 | `Draggable.OnDrag` | 🥈 `Draggable.cs:33-37` + ICF 同址 | `Core/DraggableController.cs:120` | 预览体 `activeSelf` |
| 12 | 菜单/外壳 | `Draggable.OnRelease` | 🥈 `Draggable.cs:39-43` · 🥇 `…OnRelease__15f1090.c` | `Core/DraggableController.cs:121` | 同上（false） |
| 13 | 菜单/外壳 | `EverguildTooltipItem.GetPivotPosition(AnchorPositions)` | 🥇 `decomp_full/EverguildTooltipItem__GetPivotPosition.c:1-62` · 🥈 `EverguildTooltipItem.cs:176-219` | `Core/Tooltip.cs:531-548`（消费 :549-559） | anchor 10/15 两侧面板中心相差一个面板宽 |

---

## 二、逐条证据（G0–G8 摘录；完整句在账的「判据要点/差异/备注」三列）

### 2·1 收藏 · 过滤模型（行 60 / 67 / 69 / 138 / 139 / 141）

**共同的判据基础（这是本批最硬的一条方法论收获）**：`CollectionFilterController<T>` 一族是**泛型类**，ILSpy 那份（🥈）对它的方法体**大量丢类型信息**（`NoteDecompilerIssue("Unmanaged memory load")` 满屏）。**但体是有的**——`D:/2/tools/decomp_generic_trial/out/`（2026-10-25 G6/G7 那批 640 个 `.c`）里逐条在：

| 原版方法 | 泛型产物 |
|---|---|
| `CollectionFilter<T>.SetupFilter` | `CollectionFilter_object___SetupFilter__4d20f0.c`（**9 行**；与 `FriendInfo__set_Room` ICF 同址 ⇒ 体就是一句赋值） |
| `CollectionFilterController<T>.SetupFilters` | `CollectionFilterController_object___SetupFilters__15f0740.c`（98 行） |
| `CollectionFilterController<T>.Filter` | `CollectionFilterController_object___Filter__15ef810.c`（60 行） |
| `ToggleCollectionFilter<T,>.SetupFilter` | `ToggleCollectionFilter_object__int___SetupFilter__13661f0.c`（124 行；**首行是 `FUN_`** = 名字含 `", "`，不是没解出来） |
| `ToggleCollectionFilter<T,>.OnFilterChange` | `ToggleCollectionFilter_object__int___OnFilterChange__13658e0.c`（68 行） |
| `ToggleCollectionFilter<T,>.FilterItem` | **无**（基类 `abstract`，见行 141 的 D 判定） |

**逐条机制（对得上/差异在哪）**

1. **行 60 `SetupFilter`**：原版体只有一句 `this.controller(+0x20) = controller`（另加 GC 写屏障 `FUN_1803f3730`）。**我们根本没有 per-filter 对象图** —— 「controller 引用」的等价物是「Cell 自带 key（`$fac:`/`$rar:`/`$cost:`/`$type:`）+ 点击时把 `state` 传进 `FilterPanelModel.Click`」。⇒ G2 写成「同一绑定的两种载体」，不写成「等价」。
2. **行 67 `SetupFilters`**：原版顺序 = ① `filterIconHolder.SetActive(false)` ② 给 `clearFiltersButton` 的 UnityEvent `AddListener` ③ `foreach filters → filter.SetupFilter(this)`（vtable `+0x178`）④ **`ClearFilters()`（这一跳判据文档漏了，见 §六·(b)）** ⑤ `originalAnchorPosition = rectTransform.anchoredPosition`（存 `+0x50/+0x54`）⑥ `rectTransform.anchoredPosition = (hiddenX@+0x38, 原 y)`。我们：`Build` 建整栏 + 出厂 `DeckFilter.None`（其余五个字段全空 = 原版「ClearFilters 之后」的同一状态；`Owned`/`Upgradable` 两个独立开关原版也不清）。
3. **行 69 `Filter`**：原版 `foreach (filters) collection = filter.Filter(collection)`（虚拟调用 `+0x198`，**链式 AND**），末尾 `new List<T>(collection)` 复制一份。我们 `VisibleCards()` 是**单趟 `foreach(pool)` + 五个 `continue`** —— 同一结果集、零中间分配。⚠️ 我们把**排序**也并进了这个方法（原版在 `CollectionTab.GetCollection` 里）⇒ 那一段不归本行。
4. **行 138 `ToggleCollectionFilter.SetupFilter`**：原版逐选项 `Instantiate(prefab, transform.parent)` → `SetActive(true)` → `CollectionFilterToggle.Initialize(clone, option)` → `typeFilters[option] = clone.IsOn`（`FUN_181704af0` = `Dictionary.set_Item`）→ `clone.OnValueChanged.AddListener(<SetupFilter>b__5_0)`；末了把模板 GO `SetActive(false)`。我们 `Build` 每档产一格、`On` 初值 = `DeckFilter.*Selected`（同一谓词的另一端）；`Click` 写 `HashSet`（`Factions/Rarities/Types/Costs`）。
5. **行 139 `OnFilterChange`**：原版 = ① 逐选项把 `IsOn` 收进 `typeFilters` ② `base.OnFilterChange()` = `controller.NotifyFilterChange()`（🥈 `CollectionFilter.cs:17-27`）。我们是「点一下 = 集合变一次」，没有「收」这一拍；`<SetupFilter>b__5_0` 那条回调在 🥈 `ToggleCollectionFilter.cs:803-805` 明写 `OnFilterChange()` —— 与我们 `Click → SetFilter → 宿主刷新` 同一形状。
6. **行 141 `FilterItem`（D）**：基类 `public abstract bool FilterItem(T card)`（🥈 `:609`），泛型 615 条清单里**没有它的体** ⇒ 原版这一行本身**不产生行为**。真体在 4 个具体子类，逐条列进账的备注（`CardCostFilter` 夹 `[1,8]` 后 `ContainsKey` / `CardRarityFilter` `ContainsKey(rarity@+0x28)` / `CardTypeFilter` `ContainsKey(cardType@+0x94)` / `CardArmyFilter` `TryGetValue(army@+0x2c)` **查不到 = 通过**）。

**可观察量（G3，两侧数值都给了推导链）**

| 量 | 原版侧（独立推导） | 我们侧（读数口） |
|---|---|---|
| 不设筛选 | 全池 **1128**（`cards_engine.json` 的 `count`） | `Editor/DeckScene.cs:1833` `Check(all, s.PoolCount, "不设筛选时命中全部")` |
| 类型 = Warlord | 池内 `type=="hero"` = **56**（1128 张里的分布：unit 586 / tactic 446 / hero 56 / defence 39 / enchantment 1） | `Editor/DeckScene.cs:2111` `CheckTrue(… == 56, "按类型筛出 56 个督军")` |
| 阵营 = Ultramarines | **134**；再加 legendary ⇒ **31** | `Editor/DeckScene.cs:1838/1845` 会打印 `ult` / `leg` 实得值（断言只断 `leg < ult`） |
| 稀有度多选 epic+legendary | 231 + 197 = **428**（并集） | `Editor/DeckScene.cs:2048-2104` 的多选段（`hitsR` / `hitsCostSel`） |

⚠️ **诚实边界**：上表「我们侧」我**没有跑**（G7：自检由调度台在同步点跑）⇒ 账里写的是「读数口 + 原版侧推导值」，**不冒充实测**。

### 2·2 行 570 `ArmyItem<T>.TryGetItem`

- G0：🥈 `ArmyItem.cs:11-22` 逐句可读：`if (army != cardArmy) { returnItem = default; return false; } returnItem = item; return true;`；🥇 `…TryGetItem__19f43b0.c` 与 `AlternateArtStyleItem<object>$$TryGetItem` **ICF 同址**（同体）。
- G1：我们 `Core/ArmyIcons.cs:89-95` 的 `TryGet(int, out Entry)` 把**原版的「逐条谓词」+ 上层 `ArmyIconsSO.GetArmyIcon` 的遍历**合成一处（同一结果集）。
- G2：拆分方式不同（一个方法 vs 两层）；`Neutral(0)` 原版表里没有 ⇒ 两边都「取不到」（`IconName(0) == null`）。
- G3：13 条表逐 army 命中 / `0` 未命中；读数口 = `Editor/MainMenuScene.cs:6829-6848`（「图就是 `ArmyUtilities.GetArmyIcon` 那一张（13 阵营表）」）+ `Editor/BadgeFilterAssert.cs:288`（army 20 的技能钮图 = null）。

### 2·3 行 975 `RemoveStatusAnim`

- G0：🥇 `CardScript__RemoveStatusAnim.c:1-12` —— 只有一句 `BattleCardUI.CleanStatusAnims(ui, 1)`，**第二参写死 1 = 全清**。
- G1：`Core/TraitFrames.cs:973-987`（`Clean(stillHas, clearAll)` + `CleanOne`：`if (clearAll || !stillHas.Contains(key))` 逐条 `Drop`）+ `:989-1004`（`Drop` → `Exit`）。原版「一个方法、写死全清」= 我们 `clearAll: true` 档。
- G2：原版单档；我们三档（`Remove` 单条 / `Clean` 谓词 / `clearAll`）⇒ 全清语义落在 `clearAll` 那一支；**收尾载体不同**（原版 `AnimFXController.Exit()`，我们 `WarpforgeEffectPlayer.Exit()`，没有控制器则 `Kill()`）。
- G3：两本字典的框数 → 全清后各 **0**；读数口 = `Editor/BattleScene.cs:10468-10506`（「trait 不再持有 ⇒ 那张框被清掉」/「vanguard 撤掉 ⇒ 同样清掉」）。
- G5 反例：`Editor/BattleScene.cs:10548-10556`（没绑定的 `flying` ⇒ 不挂框、也不演）。

### 2·4 行 41 `BlobShadowController.Start`

- G0：🥇 `BlobShadowController__Start.c:1-45`：读 `SpriteRenderer.color` → `+0x40`(原色) / `+0x50`(当前色) 各存一份、**当前色 alpha 置 0**（`+0x5c = 0`）；`+0x70 = target.y − self.y`；`+0x74 = localScale`。
- G1：`Core/BlobShadow.cs:123-132` 的 `Capture()` 记原色 / 淡出色（alpha 0）/ 原 scale；出生链 `:80-120`（`Create` 写入原版 `m_Color = (0,0,0,0.36078429)` 后 `Capture()` → `Sync()`）。
- G2：① 原版把「当前色」先置 alpha 0、等下一帧 `Update` 才填；我们同帧 `Sync()` 就填成正确值（**净结果同**）② `targetYCenterOffset` 原版参与 `Update` 的 `t` 计算（`Controller__Update.c` 那句 `(target.y − offset − floorY)/maxY`）；我们的影子 `localPosition = 0`、与 target 同点 ⇒ offset **恒 0** ⇒ 不存该字段（等价，不是漏一句）。
- G3：`CurrentColor`（纯黑 α0.36078429×卡 alpha）· `WorldDiameter`（= 1.25 × **2.0567584** × **0.88586** × 卡缩放）· 世界 y（= `floorY 0` + `yOffsetForSnap 0.01`）；读数口 = `Editor/BattleScene.cs:20315-20336`。原版侧三个值全部来自资产实读（`SpriteRenderer_-8386903980694135872.json` / `Transform_6339896688388119488.json` / `MonoBehaviour_2235663227690458048.json`）。
- G5：整卡淡出时影子跟着淡（同一断言的 `v.Alpha` 项）。

### 2·5 行 120 / 123 `Draggable<T>.OnDrag` / `.OnRelease`（**本批唯一改了代码的一处**）

- G0：🥈 `Draggable.cs:33-37` = `gameObject.SetActive(value: true)`；`:39-43` = `SetActive(false)`。**旁证**：`decomp_generic_trial/out/Draggable_object___OnRelease__15f1090.c` 与 `CollectionFilterController<object>__<Toggle>b__9_0` **ICF 同址**（后者就是那条 `SetActive(false)` 的 lambda）。
- **铁律 5 订正**：我们原来的注释写「`OnDrag()`/`OnRelease()` 两个方法体**没有反编译产物**（`all_methods.txt` 里也没有带 `Draggable<…>` 前缀的条目）⇒ 留空」—— **那条是错的**（体在 ILSpy 那份里，而且 4 行就完了）。已就地改掉注释 + 补齐两个体。
- G2：本仓**没有 UGUI `EventSystem`** ⇒ 这两个方法今天**没有调用点**（预览显隐走 `DeckRuntime` 的合成指针层 + `DraggableController.OnBeginDrag/OnEndDrag`）⇒ 补齐是**零行为变化**的等价件（改动前后 `typecheck` 0/0）。
- G3：预览体 `activeSelf`（`DeckRuntime.UiCosmeticPreviewActive`），读点 `Editor/DeckScene.cs:2334`（起拖 ⇒ true）/ `:2378`（松手 ⇒ false）。

### 2·6 行 238 `EverguildTooltipItem.GetPivotPosition(AnchorPositions)`

- G0（本批**证据最硬**的一条）：🥇 `decomp_full/EverguildTooltipItem__GetPivotPosition.c:1-62` —— 10 个 case 的返回值是**双浮点包在 64 位里**，逐个解开：`0x3f0000003f000000` = (0.5,0.5) · `0x3f00000000000000` = (0,0.5) · `CONCAT44(0x3f000000,1.0f)` = (1,0.5) · `CONCAT44(1.0f,0x3f000000)` = (0.5,1) · `1.0f<<32` = (0,1) · `0x3f8000003f800000` = (1,1) · `0x3f000000`(32 位) = (0.5,0) · `0` = (0,0) · `1.0f`(32 位) = (1,0)；`default` 分支 `LogWarning` 之后**返回初值 (0.5,0.5)**。🥈 `EverguildTooltipItem.cs:176-219` 是同一张表（浮点被 ILSpy 打成 `Vector2)1056964608` 这种位型）。枚举值另核：`AnchorPositions` = 0/5/10/15/17/20/25/27/30/55（`/d/2/Warpforge_code/Scripts/Assembly-CSharp/AnchorPositions.cs`）。
- G1：我们 `CardPresentation/Core/Tooltip.cs:531-548` 的 `void Pivot(int anchor)` 是**逐档相同**的 9 格表 + `default → 出声 + 回落 (0.5,0.5)`；`:549-559` 再把它兑现成「面板中心 = 锚点 + (0.5 − pivot) × 尺寸」。
- G2：原版**返回 Vector2**（pivot 交给调用方）；我们**在同一个方法里就直接落成面板中心并摆件**，不单独返回 pivot —— 同一张表的两种载体。默认档两边都是「出声 + 回落中心」，**不抛异常**。
- G3：`Tooltip.PanelCenterWorld` 在 anchor 10 / 15 的两侧差 —— 原版侧由 pivot 表推：10=(0,0.5) ⇒ 面板往**右**长、15=(1,0.5) ⇒ 往**左**长、两者中心**相差正好一个面板宽**、y 不偏；我们侧读数口 = `Editor/BattleScene.cs:24468-24493`（`c10` / `c15` + 三条 Check + `PanelSizePx.x / 108`）。
- **铁律 5 就地订正**：原账这行的落点写「`Core/Tooltip.cs` 贴边翻转」—— **不成立**（全文 grep 无贴边/Flip 逻辑）；真实落点就是 `Pivot()`。

---

## 三、改动的文件 / 行（新增行数 = **0**）

| 文件 | 行 | 改什么 | 行数变化 |
|---|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/DraggableController.cs` | 108-110 | 注释订正（原「没有反编译产物 ⇒ 留空」→ 真体在 `Draggable.cs:33-43` + ICF 佐证） | **0**（3 行改 3 行） |
| 同上 | 120 / 121 | `OnDrag() { gameObject.SetActive(true); }` · `OnRelease() { gameObject.SetActive(false); }` | **0**（2 行改 2 行） |

- 文件总行数改前改后都是 **282**（`wc -l` 与 `git show HEAD:` 对照）；**裸 CR 0 / 全 LF，行尾未翻**。
- `bash 工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**。
- ⚠️ 之所以**一行不加**：这个文件被账按 `文件:行号` 引用（`DraggableController<T>.b__6_2` 那行等），插行会让 `check_ledger_points` 报漂移（CLAUDE.md 铁律 18·b 的同族纪律）。

其它 11 条**没有改代码**（它们本来就是已对齐的，缺的只是「判据写全 + 可观察量」）。

---

## 四、断言草稿（交给调度台在同步点跑，⛔ 本代理不跑、不认领通过）

```csharp
// —— A. Draggable<T> 两个新体（DeckScene 或 CollectionScene，拖拽那一段之后）——
var preview = rt.UiCosmeticPreview;                 // = DraggableController<T>.Preview
preview.gameObject.SetActive(false);
preview.OnDrag();
CheckTrue(preview.gameObject.activeSelf,
          "★ `Draggable<T>.OnDrag` ⇒ 预览体 `SetActive(true)`（判据 Draggable.cs:33-37）");
preview.OnRelease();
CheckTrue(!preview.gameObject.activeSelf,
          "★ `Draggable<T>.OnRelease` ⇒ `SetActive(false)`（判据 Draggable.cs:39-43）"
        + "｜🧨 把两个体互换（true/false）⇒ 两条都红");

// —— B. 过滤多选并集（数据驱动；期望值来自 cards_engine.json 独立数）——
var f = DeckFilter.None; f.Rarities = new HashSet<string> { "epic", "legendary" };
s.SetFilter(f);
Check(s.VisibleCards().Count, 428,
      "★ 稀有度 epic+legendary = **并集 428** 张（池内 231+197）"
    + "｜🧨 改成交集 / 只认最后一个 ⇒ 实得 ≠ 428");
s.SetFilter(DeckFilter.None);

// —— C. `inPlayAnim` 闸（**先修 §六·(a) 再开**）——
CheckTrue(!TraitFrames.PlaysInPlayAnim("armour"),
          "★ `armour` 的 `inPlayAnim=false` ⇒ 状态帧一条都不播（原版 ActivateTraitAnim.c:20 那个前置 bool）");
CheckTrue(TraitFrames.PlaysInPlayAnim("stealth"),
          "★ `stealth` 的 `inPlayAnim=true` ⇒ 照旧播（这条防「一刀切关掉」）"
// —— D. pivot 表（**已有，不用新写**）——
// Editor/BattleScene.cs:24468-24493 已经钉了 anchor 10/15 三件事（往右长 / 往左长 / 差一个面板宽 + y 不偏），
// 原版侧期望值由 `GetPivotPosition.c` 的 pivot 表推出 ⇒ 本行升 A 就是靠它，⛔ 别再写第二条同义断言。
```

---

## 五、没查清的部分（⛔ 没拿猜测填空）

1. **`CollectionTab<T>.GetCollection()`（账 `泛型与启动链_补读`:97，仍 B）**：链形状读到了（`CardCollectionTab__GetCollection.c:50-146` = `Where` + 5×`OrderBy/ThenBy` + `ToList`），但**5 个键 lambda 在 Ghidra 里被 ICF 折叠解错名**（`b__8_1` 落到 `AlternateArtCardCollectionTab` 的同址函数、`b__8_2` 落到 `CardsCollection` 的同址函数）。⇒ **键的逐条身份未坐实**，本代理不升 A。现用的 4 个键（阵营 → 非督军 → 费用 → 名 + 收藏多的 `inventoryOptions`）取自另一份**未折叠**的 `DeckEditingWindow__GetCardCollection.c:106/121/136/151`（`SortForDisplay` 的 doc 已引），但「Tab 那 5 个键与它逐条相同」仍是**推断**。
2. **`ActivateIconAnim` / `ActivateTriggerTraitAnim`（账 `战斗逻辑驱动`:763 / :769，仍 B）**：原版体读到了（🥇 `CardScript__ActivateIconAnim.c` / `__ActivateTriggerTraitAnim.c`：`GetTraitFromAbilityTrigger(trigger)` → `HighlightTraitIcon` → `DisplayTriggerAnim(..., playSound=true)`，返回值就是 `DisplayTriggerAnim` 的 bool）。**卡住的是第一跳**：`CardTraitCollection.GetTraitFromAbilityTrigger` 在我们的对应是 `Core/VfxMap.cs` 里那套「AbilityTrigger ↔ trait」映射（`IsAbilityTrigger` / 归一化），**未核到方法级**，且 `HighlightTraitIcon`（图标脉冲）我们并进了 `DisplayTrigger` 的补间，没有独立函数 ⇒ 两侧都不是干净的方法级落点。
3. **`ActivateTraitParticlesFromCode`（账 :766，仍 B，建议改 C）**：原版 = 找 `TriggerType==5` 那条 → `BattleManager.GetTraitEffect(cardData, trait, 5, army)` → `GetCurrentTraitValueWithModifiers(trait)` → `BattleManager.ResolveTraitAnim(effect, caster, **callback=targetCard**, this, **value**)` + `StartCoroutine`。我们 `Core/TraitParticles.cs:168-183` 的 `Play` 只有「按 army 取 prefab + 挂上去」——**少了 `traitValue` 与 `targetCard` 回调**（原账写的「逐句 3/4」就是这个）。⇒ 是**缺口**不是待核。
4. **`Core/Tooltip.cs` 那一族（`EverguildTooltipItem.Awake/GetPivotPosition/Show` · `ContentSizeFitterMinMax.ClampSize/SetLayoutHorizontal/Vertical` · `TextLinkProcessing.*`）**：原版体读了（例如 `ContentSizeFitterMinMax` = UGUI 布局回调里把 `sizeDelta` 夹进 `[min,max]`），但**我们没有 UGUI 布局管线**（面板尺寸在 `Tooltip.cs:606` 直接 `Mathf.Clamp(bw+PadX*2, MinW, MaxW)`）。「同语义 / 不同载体」的边界没核到能升 A 的程度 ⇒ 一行都没动。
5. **`Core/ScreenUtilities.cs` 那三行（账 `菜单_外壳`:604/605/606，仍 B）**：函数本体是**逐句照抄**的（`GetScreenDPI` 的 100 下限 / `GetScreenPhysicalSize` 的 `sqrt(w²+h²)/max(100,dpi)` / `GetDiagonalSizeReferenceValue` 的 `clamp01((英寸−4.7)/0.9000001)`），但**原版两个消费点都在 `Hand/**`**（`PlayerHand.cs:163/187` 的间距插值、`VarsDevice.cs:19` 的 >7.0 大屏档）⇒ **今天没有任何可观察量**。这是**前一任代理明确的「不记 A」决定**，本代理复核后**同意并保持 B**（⛔ 不为了让数字好看去升它）。
6. **`GameStaticData.GetLanguageCode` / `ArmyUtilities.GetArmyByCode` 那 13 条（域 = 其他_基础设施 / 本地化，落点在 `Core/Loc.cs` / `Core/ArmyIcons.cs`，仍 B）**：本代理**未处理**（不在派单的三个域里，且其中几条的等价物根本不是同一种键空间 —— 例：`GetArmyByCode("GSC")` 的入参是**短代码**，我们 `ArmyIcons.Id` 收的是**阵营名**）。

---

## 六、顺手发现（都带回证据，交给调度台分流）

### (a) 🔴 **真缺口：`TraitFrames.DisplayStatus` 缺 `CardTrait._hasInPlayAnim` 闸 —— 6~8 条 trait 会多播 Start 帧**

- 判据：🥇 `CardScript__ActivateTraitAnim.c:20` —— `if (*(char *)(lVar1 + 0x20) != '\0')` 是**整段的前置闸**，为假时**连 `DisplayStatusAnim` 都不调**（Start(0) 那一档也进不去）。字段身份已坐实：`/d/2/Warpforge_code/Scripts/Assembly-CSharp/CardTrait.cs:27-29` `private bool _hasInPlayAnim;`（序列化顺序上正是 `+0x20`）。
- 我们的现状：`Core/TraitFrames.cs:812-854` 的 `DisplayStatus` **无条件先 `PlayStartRows`**（`:862-893`），闸只体现在「绑定表里有没有 OnPlay 行」。
- **数据侧实测**（`数据/游戏数据/trait_collection.json`，77 条 trait）：
  - `inPlayAnim` = **true 39 / false 36 / null 2**
  - **`inPlayAnim=false` 却有 `TriggerType==0` 行 = 8 条**：`Armour` · `Companion` · `Flank` · `Pack` · `Rally` · `Tide`（这 6 条 `effectAnim` **非空**，原版会**不播**而我们**会播**）· `FirstStrike` / `Piercing`（`effectAnim` 是空引用，两边都不播）
  - **反向 = 0 条**（`inPlayAnim=false` 却没有 OnPlay(2) 行的 = 0）⇒ 原账那句「用绑定表有行代替」**只在 OnPlay 半边成立**，Start 半边**不成立**。
- 修法（两步，都**不在本代理白名单**）：① `Unity/工具/gen_trait_effect_bindings.py` 把 `inPlayAnim` 带进绑定表 → ② `TraitFrames.DisplayStatus` 入口加闸。改完必须重跑 `Editor/BattleScene.cs:10435-10560` 那一段（`stealth` / `vanguard` / `oath` / `bloodthirst` 四条），因为「一刀切关掉」会把它们打红。
- 账：本行已由 A 计划改判 **C（等级 P1）**，写进补丁。

### (b) 判据文档漏了一跳：`SetupFilters` 里还有一次 `ClearFilters()`

`资料/完全复刻/判据/共用件_过滤模型_B升A.md` §1 第 2 条把原版 `SetupFilters` 写成「藏图标 → 绑清空钮 → 逐个 SetupFilter → 缓存 originalAnchorPosition」，**漏了 ④ `ClearFilters()`**（🥇 `CollectionFilterController_object___SetupFilters__15f0740.c` 里那次对 `CollectionFilterController<object>__ClearFilters` 的直接调用）。这不是纯顺序问题：它是**「面板一建出来就是全清态」**的判据来源。我只把结论写进账的备注（补丁第 2 行的差异列），**文档本身没改**（不在白名单）—— 请调度台修正该节。

### (c) 三条已判 A 的账行，落点是**复制粘贴错的**

`账/收藏.tsv` 的 **316 / 317 / 318**（`ToggleCollectionFilter.SetupFilter` / `_003CFilter_003Eb__7_2` / `_003CSetupFilter_003Eb__5_0`）三行的「我们的落点」**全部**写成 `Shell/CollectionWindow.cs:2827（等价实现 = ClearFiltersNow）` —— 那两个是**建格 / 单卡谓词 / 开关回调**，与「清空筛选」不是一回事；而同一段的 311（`ClearFilters()`）也指 `:2827`。⇒ 316/317/318 是错的（大概率批量改写时一起刷了）。本代理**不改收藏域的行**（落点不在白名单），只报告。

### (d) 行 140（收藏 · `CollectionFilterController<T>.SetupFilters`，已 A）的「差异」列还挂着待补事项

它写「落点写成 `FilterPanelModel.cs:647-830` + `CollectionWindow.cs:2964-3010` 两段范围」。Core 这半边本代理已核到 **647-854**（`Build` 的真结尾是 854，830 那个数偏小）；壳侧那一段请壳代理补。

### (e) `Draggable<T>` 两个空体已修（见 §三）—— 顺带更正了一条**写进代码注释里的错知识**
「`all_methods.txt` 里没有带 `Draggable<…>` 前缀的条目 ⇒ 没产物」这条推断法本身有坑：泛型类的体在 `GenericInstMethod` 子块里，按 `// RVA:` 非负筛名单会把它们**整类筛掉**（CLAUDE.md 已记 G6 那一课）。这次是**ILSpy 那份里就有**，只是当时没打开那个文件。

---

## 七、给调度台的请求

1. **合并补丁**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_AP`（⚠️ 合并前先跑 `check_patch_conflicts.py`；我这份 13 行与其它补丁是否撞键未验，键已逐条核过主表存在）。
2. **改判两行的等级/状态**已写在补丁里：`ActivateTraitAnim` **B→C（P1）**；`ToggleCollectionFilter.FilterItem` **B→D**。若调度台不认「抽象声明 = D」这一支（`判据/共用件_过滤模型_B升A.md` §2 第 12 条当时判的是 A），请以调度台裁定为准，把该行改回 A 并给一个子类级的落点。
3. **派活（跨文件，本代理白名单外）**：
   - `Unity/工具/gen_trait_effect_bindings.py` + `Core/TraitFrames.cs`：补 §六·(a) 的 `inPlayAnim` 闸（**P1，1~2 小时**）。
   - `Shell/ItemDrawer.cs` / `Shell/PrebuiltDecks.cs` / `Shell/CollectionTabPage.cs`：接走 §〇 那张表里的 11 条（收藏 8 + 卡组编辑 3）。
   - `Battle/BattleDriver.cs` + `Battle/CardDisplayWindow.cs`：`ToggleTooltips` / `GetTooltipPosition` 两条 C（账 `卡牌面_渲染`:21 / :120）差的都只是**调用点一行**（Core 侧口子已备）。
   - `资料/完全复刻/判据/共用件_过滤模型_B升A.md`：按 §六·(b) 补 `ClearFilters()` 那一跳。
4. **下一批可直接接手的（本代理核过判据、只差工作量的）**：`CollectionTab<T>.GetCollection`（要先把 5 个键 lambda 用 VA 反汇编或 `dump.cs` 的方法表点名）、`ActivateIconAnim` / `ActivateTriggerTraitAnim`（差 `GetTraitFromAbilityTrigger` 那一跳的方法级对应）、`ActivateTraitParticlesFromCode`（差 `traitValue` + `targetCard` 回调）。

---

## 八、本批的自查命令与输出（照抄）

```
$ TMPDIR=/tmp/wf_b8ap bash d:/4/Unity/工具/typecheck.sh
运行时错误数: 0
编辑器错误数: 0

$ python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/Unity/资料/完全复刻/账/_parts/B8_AP.tsv
OK B8_AP.tsv    A=11   通过=11   漂移=0   告警=6   失败=0

$ python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py d:/4/Unity/资料/完全复刻/账/_parts/B8_AP.tsv
合计 6 条（强 0 · 弱 6）   ← 6 条全是 G2 要求写的「机制差异」句（不同/没有），逐条回读无误

$ python -I -X utf8 d:/4/Unity/工具/verify_annot.py d:/4/Unity/资料/完全复刻/账/_parts/B8_AP.tsv
同名 715 · 异名已注解 2402 · 缺注解 0
```

（`check_ledger_points` 的 5 条「告警」是「一个落点串里带了多段引用、机检只认第一段」那类，非失败；0 漂移。）
