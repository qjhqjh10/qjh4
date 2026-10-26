# B9 · 菜单 / 商店 / 锻造 / 战役 四个域的 C 行 · 派活包（按目标文件切片）

> 判据代理 `K3SH`（第八十四会话「完全复刻」）· 2026-10-26 · **只读核**（未改 `.cs` / 未改主账表 / 未改正本；未跑 Unity；未动 git）
> 配套：逐条施工单 = `判据/B9_菜单商店C_施工单.tsv`（12 列 × **122** 行）· 判读与证据 = `报告/B9_K3SH_菜单商店C施工单.md`
> 🔴 **稳定性**：施工单的「账行号」是**快照**（形如 `商店.tsv:42`，因为同号在多份账里会撞）。写手一律**先按「原版类 + 方法名」在对应账里定位，再对照本包的行号**。
> 🔴 **账行号的写法变了**：本表跨 4 份账（`菜单_外壳/商店/锻造/战役`），只写数字会撞号 ⇒ 一律写成 `<账文件>:<行号>`（行号 = 该文件里的物理行，表头 = 第 1 行）。

---

## §0 用法（四句话）

1. **一个包 = 一组「文件互不相交」的目标文件 = 一个写手**。包内按「先 P0 / 再 P1 / 最后 P2」做，做完一条就在施工单里勾掉。
2. 🔴 **共用件（§2）一文件只能有一个写手** —— 本批 5 个包会因为共用件互相等待，**顺序见 §2 的排程**，别各自开工。
3. 每包做完先跑 **`bash 工具/typecheck.sh`**，再按「本包验收口」验；⛔ 写手不跑 Unity 批处理、不跑全量自检（主对话在同步点统一跑）。
4. 🔴 **本批 122 条里只有 54 条今天可动手**（A 9 · B 3 · C 9 · D 5 · E 7 · F 13 · G 5 · J 1 · K 2），其余 **68 条整批挂在「活动线暂停」上**（§3·H）—— 先看 §1 的「可动/不可动」那一列再派活。

---

## §1 包总表

| 包名 | 目标文件（**独占**） | 条数 | 今天可动? | 前置依赖 | 施工单账行号（快照） | 本包验收口（摘要） |
|---|---|---|---|---|---|---|
| **A** 主菜单几何与背景 | `Shell/MainMenuRuntime.cs` · `Shell/MenuDraw.cs`（按需）· 新 `Shell/MenuSprites.cs` · 新 `Shell/BoostMenu.cs` | **9** | ⚠️ 4 条要口径、5 条可动 | 无（A 内部先定「是否支持非 16:9」） | 菜单 2·3·4·5·43·44·45·471·472 | 四档 aspect 的卡行 y 对照表；背景 = friendlyWorld；加成条 建/删/倒计时 |
| **B** 外壳共用件（窗口 / 滚动 / 锚点） | `Shell/WindowsManager.cs` · `Shell/MenuScroll.cs` | **3** | ✅ | 无 | 菜单 612·720·721 | 滚到目标 0.667s 二次缓出 + 精确落位；锚点回挂后每窗回自己 placement |
| **C** 徽记 + 战役内容 + 锻造页头 | `Shell/BadgeNotifications.cs` · `Shell/CampaignTab.cs` · `Shell/CampaignData.cs` · `Shell/ForgeTab.cs` · `Shell/ItemDrawer.cs` | **9** | ✅（除 P0 的 shader 那半） | 无（shader 那半联动 `I`） | 战役 4·12·19·53·55 · 商店 239·240·241 · 锻造 73 | 逐格徽记只亮该阵营；点数可变 + 刷新；战役点按阵营画；Small/Large 两套图 |
| **D** 背包 / 货币层 | `Shell/Inventory.cs` · `Shell/DailyData.cs`（Wallet） | **5** | ✅（但**调度台已裁「暂不建」**） | 无 | 商店 54·59·60·61·62 | `Inventory<Currency>` + `HasEnough`；余额/上限读数 |
| **E** 商店货架 / 报价数据 | `Shell/ShopData.cs` · `Shell/OfferContainer.cs` · `Shell/BaseOfferPopup.cs` · 新 `Shell/OfferData.cs` | **7** | ✅（轮播 UI 那 4 条裁定「排后」） | 无 | 商店 42·43·44·45·93·94·163 | items 在前 + offerLists 合并保序；同 TargetId 折叠 = 1 条数量 N；轮播 N=1 时逐像素同 |
| **F** 卡组格 / 收藏格（**跨域**：收藏线 + 卡组编辑线） | `Shell/CollectionTabPage.cs` · `Deck/DeckRuntime.cs` · `Core/CardView.cs` · `Shell/CollectionData.cs` | **13** | ✅（banned 那条要活动线） | 与卡组编辑线/收藏线**同一文件**，须先排所有权 | 菜单 54·58·60·100·101·102·105·106·107·108·109·110·111 | 边缘滚 10%/s；近垂直起拖只滚动；新卡角标 1.1/0.35s OutBounce |
| **G** 错误弹窗 / 提示 / 对决闸 | 新 `Shell/ErrorPopupWindowController.cs` · `Shell/DuelPopupWindow.cs` · `Shell/MenuWindowBase.cs` · `Core/Tooltip.cs` · `Shell/CardTierConfig.cs` · 新 `Shell/ReferralManager.cs` | **5** | ✅（563 只做本地模拟） | 无 | 菜单 134·157·236·245·563 | 逐枚举值文案 = 官方 41 条之一；无卡组 ⇒ 弹 DontHaveDeckForGameMode |
| **H** 活动线整批（暂停） | 活动线新件 + `Shell/RankedEventWindow.cs` + `Shell/MainMenuRuntime.cs`（❌ 与 A 撞） | **68** | ❌ **不派活** | **A 必须先做完**（同改 MainMenuRuntime）+ LiveOps 层 | 见 §3·H | 活动数据到位后才能验 |
| **I** 特效（shader 合件） | 特效侧（Coffee `UIEffect` Shiny + transition 贴图） | 1（与 C 的 P0 同一件） | ✅ | 与轮抽 `UpdateDeckInfoPanelAnimSync` 合一件 | 战役 19（流光那半） | 进度条播 1.0s 流光；轮抽那处同步生效 |
| **J** 锻造加点（**跨域**：战斗线） | `CardPresentation/Battle/BattleDriver.cs` | **1** | ⚠️ 需先定「谁给点」口径 | 战斗线写手 | 锻造 65 | 一局打完 ⇒ 该阵营锻造点增；跨阵营不串账 |
| **K** 布局时机审计（只读） | 无（产出 = 733 份归属表） | **2** | ✅ | 无 | 菜单 403·404 | 每个 `fixOnEnable` 实例 ↔ 我方「内容变化即重算」的时机对齐 |

**合计**：A9 + B3 + C9 + D5 + E7 + F13 + G5 + H68 + I(并入 C) + J1 + K2 = **122**。
**今天可派** = A(可动部分) + B3 + C9 + D5 + E7 + F13 + G5 + J1 + K2；**H 整批 68 条不派**。

---

## §2 共享瓶颈（**一文件只能一个写手** —— 这是本批最容易撞车的地方）

| 共用件 | 被哪些包用 | 被几条引用 | 处理方式（排程） |
|---|---|---|---|
| **`Shell/MainMenuRuntime.cs`** | **A**（9 条里的 6 条）+ **H**（18 条活动容器） | 27 处提到 | 🔴 **A 先做完并落盘 → H 才能动**。H 今天不跑，所以现实中不冲突；但恢复活动线时必须先拿这份文件的写权 |
| **`Shell/MenuDraw.cs`** | A（AspectScale 若落这里）+ K（Recompute 窄口）+ 所有画层的包 | 6 处 | ⛔ **别把新函数塞进 MenuDraw**：A 自带私有 `AspectScale`；K 的 `Recompute` 只在确需共用时才进 MenuDraw，且必须 A/K 串行 |
| **`Shell/BadgeNotifications.cs`** | **C**（战役/锻造逐格徽记）+ **D**（BoosterPackBadge 那条，已挪进 C）+ 导航/收藏（他人） | 4 处 | C 独占；**D 不再碰它**（239-241 三条已并入 C）；任何域的徽记改动都排 C 之后 |
| **`Shell/WindowsManager.cs`** | B（720/721 锚点）+ G（报错出口 ShowPopUp）+ 全壳所有窗口 | 3 处 | B 独占；G 的 `ShowError` 若要求改 WindowsManager ⇒ **等 B 落盘**（G 可先用现成的 ShowPopUp 口） |
| **`Shell/Inventory.cs`** | D（货币）+ C（239-241 若走 Inventory）+ 收藏/商店 | 8 处 | D 独占；C 的 239-241 **只用现有 `Inventory<T>` 读口**、不改它 |
| **`Shell/ItemDrawer.cs`** | **C**（战役点按阵营画 / Small-Large 两套图） | 2 处 | C 独占；⛔ 别改通用路径的默认行为（全壳都在调） |
| **`Shell/MenuScroll.cs`** | B（612 滚到子件）+ 各页签滚动 | 1 处 | B 独占（全壳唯一滚动实现） |
| **`Deck/DeckRuntime.cs`**（8838 行） | **F**（10 条拖拽） | 11 处 | 🔴 与**卡组编辑线的写手**抢同一文件 ⇒ **F 开工前必须由调度台确认该线没人正在写** |
| **`Shell/CollectionTabPage.cs`** | **F** | 13 处 | F 独占（收藏线同文件，需排所有权） |
| **`Shell/ForgeTab.cs`** | **C**（页头 + 徽记驱动）+ 锻造线 | 3 处 | C 独占；锻造线若也要改，先合批 |
| **`CardPresentation/Battle/BattleDriver.cs`** | **J**（锻造加点） | 1 处 | 战斗线独占；J 必须排在战斗线写手之后，且**只走 `LocalAct`/结算口** |
| 活动线新件（`Shell/Liveop*.cs` / 活动页 / `RankedEventWindow.cs`） | **H** | 17 处 | H 今天不派；恢复时**整批一个写手**（这些文件互相强耦合） |

---

## §3 各包明细

### A · 主菜单几何与背景（`Shell/MainMenuRuntime.cs`）· 9 条
- **序**：① **先定口径**（调度台 / 用户）：是否支持非 16:9 —— 裁「钉 16:9」⇒ 2/3/4/5 四条可判 D 并写明理由；裁「跟窗口变」⇒ 按 ② 做 ② 2/4/5（P1，立 `AspectScale()` + 重算 `CardRow0Top`）③ 3（OnDisable 退订，一行）④ 471/472（MenuSprites 世界图；先解 SO 再定接法）⑤ 43/44/45（BoostMenu 整类新建，随活动/加成线）
- **前置**：无。**但 H 包 18 条同改本文件 ⇒ A 先落盘**
- **验收口**：`CardRow0Top` 在 1430×883 下 ≈148.5、16:9 = 182.8；背景 = friendlyWorld（无图回渐变且出声）；加成条 建/删/倒计时三条断言
- **风险**：一行 y 错 = 整行卡偏；`originalItemSize` **不序列化**（别去 prefab 找）；`BoostMenu` 未激活早退别丢

### B · 外壳共用件（`WindowsManager.cs` + `MenuScroll.cs`）· 3 条
- **序**：① 612（P1，`FocusOnItem(target, speed=1.5, onFinish)`：二次缓出 + **结束时精确贴 target** + onFinish 恰一次；**先按 `MenuScroll` 的 offset 模型换算，⛔ 不照搬 uGUI 归一化公式**）② 720（EnableAnchor 一行 = `RegisterAnchorOverride(transform)`）③ 721（DisableAnchor = 置 null + **子件回挂** `GetWindowAnchor(window.placement)`）
- **前置**：无。720/721 要先回答「我们到底做不做转场」——现状 `Shell/` 里 **0 命中** `SimpleSceneTransition`
- **验收口**：滚到视口外目标 ⇒ 0.667s 到位、误差 <1px、onFinish 一次；锚点关闭后每窗回自己的 placement（不是留在锚点下）
- **风险**：`MenuScroll` 是全壳唯一滚动实现；`SetParent` 保留世界坐标（别顺手改 localPosition）

### C · 徽记 + 战役内容 + 锻造页头 · 9 条
- **序**：① **战役 4**（P1，`CampaignBadge` 加 `army` + provider 按 army 过滤 + 每格 `BadgeWiring.Attach`）② 商店 239/240/241（同文件，BoosterPackBadge provider）③ 战役 53（DrawForArmy）+ 55（Small/Large 两套图；**先查图在不在再改接口**）④ 战役 12（`Points` 可变 + `AddPoints`；会碰既有「点数恒定」断言）⑤ 锻造 73（ForgeTab 页头四件 + 同一 flag 控显隐）⑥ **战役 19 的流光半**（P0，与 `I` 合成一件：Coffee `UIEffect` Shiny + transition 贴图）
- **前置**：19 的 shader 半靠 `I`；其余无
- **验收口**：只有有可领节点的**那一格**亮红点；`AddPoints(+5)` ⇒ +5 且红点刷新；战役点图标按阵营；两档图取到不同 sprite；页头四件同显同隐
- **风险**：`BadgeNotifications.cs` / `ItemDrawer.cs` / `ForgeTab.cs` 都是跨域共用件；徽记 quad 独立渲染队列（`QArmyBadge=3024`）

### D · 背包 / 货币层 · 5 条
- **序**：① 54（`HasEnough` + `EventCondition` 最小骨架）② 59/60/61/62（`Inventory<Currency>` + `ToCurrency` 表）
- **前置**：无。⚠️ **调度台 2026-10-25 已裁「暂不建，等出现消费点」** ⇒ 本包只在口径改成「要做」时才派；若派，先出「层已在、缺的只是 Currency 那一份」这句订正
- **验收口**：余额 ≥ min ⇒ true / < ⇒ false / 缺单例 ⇒ 出声；`CanGetMore` 上限语义（`maxAmount<0` = 无上限）
- **风险**：没有消费点时建了就是死代码（同 `InventoryManager` 那 24 条的处理）

### E · 商店货架 / 报价数据 · 7 条
- **序**：① 94（`Items` + `OfferLists` 合并口、**保序**；现有静态货架改喂它）② 93（存盘补 rarity/itemType，两列）③ 163（`ToRewards`：同 TargetId **折叠**、数量 = Count；四支顺序待 `.c` 复核）④ 42/43/44/45（轮播 UI，裁定「排后」；**过滤谓词不可及 ⇒ 可注入、默认 null**）
- **前置**：无
- **验收口**：items 3 + offerLists 2×2 ⇒ 7 条且 items 在前；同容器写 3 次 ⇒ 1 条数量 3；轮播 N=1 时逐像素同现状
- **风险**：`_shopItems` 是**字典**（丢顺序）⇒ 必须换成保序的两份列表；⛔ 折叠是语义不是优化

### F · 卡组格 / 收藏格（跨域）· 13 条
- **序**：① 101（P1，边缘自动滚 50px / 10 per s / 只垂直 / 双侧 + clamp01）② 106（近垂直起拖 30°–150° 让给滚动）③ 100/102/105/107/108/109/110/111（其余 8 条**先出对账表**，等价的直接改判 A/B）④ 54/60（新卡角标：`CardProgress.IsNew` + `New Card Badge` 层 + 1.1/0.35s OutBounce）⑤ 58（banned，随活动线）
- **前置**：🔴 **必须先由调度台确认卡组编辑线没人正在写 `Deck/DeckRuntime.cs`**（8838 行热点）
- **验收口**：拖到边缘 1s ⇒ 位移 ≈10%；近垂直起拖只滚动；新卡角标 0.35s 缩到 1.1；松手落到最近邻居后一位
- **风险**：`emptySpaceObject` 是**静态单件**（多实例共用）；`DOScale` 终点 1.1 是原版行为

### G · 错误弹窗 / 提示 / 对决闸 · 5 条
- **序**：① 157（P1，`CustomError.ToString()` → `"CustomErrors/"+name`；41 条词条已在官方表；`Undefined` **没有词条** ⇒ 照原版兜底、⛔ 不自造）② 134（`CanPlay`：无卡组 ⇒ 弹 `MainMenu/General/DontHaveDeckForGameMode`；显示那半等活动线）③ 245（tooltip 的 `preventPassingClickEventToParent`：241 颗里 **1 颗**）④ 236（收集等级 tooltip：XP 曲线若本地没有 ⇒ 打 `🔶 自己设计`）⑤ 563（推荐码：只做**明确标注的本地模拟**）
- **前置**：无
- **验收口**：逐枚举值文案 = 官方 41 条之一；`ErrorLoadReplay`/`ErrorLoadDeck` 与既有键逐字一致；无卡组 ⇒ 弹窗且不开局
- **风险**：⚠️ 新件 `Shell/ErrorPopupWindowController.cs` **不在既有白名单** ⇒ 需调度台分配；ILSpy 把 `ShowPopUp` 第 4/5 实参读成 default（按 `.c` 取）

### H · 活动线整批（**今天不派活**）· 68 条
- **组成**：菜单 18（`Liveop*` 17 + Gacha Tab 的 `AutoLayoutSpacing` 1）· 商店 39（Campaign 付费点数 5 · ExpansionPass 5 · Gacha 8 · GachaEvent/Data 4 · GachaEventTab 7 · GachaRewardContainer 4 · GachaRewardsPanel 3 · ContainerOfferEvent 2 · MainMenuOfferContainer 1）· 战役 11（`RankedDeckSelector` 窗内轮播）
- **不派活的三个理由（都要写进账）**：① **用户 2026-10-25 暂停活动线** ② 这批活动**已过期**且用户拍板「过期的活动不做」③ 判据只到反编译 / 过期 SO 快照，没有可跑的运行时数据
- **恢复条件**：LiveOps 层（`LiveOpsManager` + `LiveOpsEvent<TSave>` + 活动数据源）建起来 ⇒ 整批一个写手；且 **A 包必须已落盘**（同改 `MainMenuRuntime.cs`）
- **已核可先做的纯函数（不属于 H，可顺手）**：Gacha 保底高亮 = `(n ≥ requiredOpeningsToSpecialItem) && !PlayerHasAllSpecialItems()`（三输入一输出）；`GachaEventTab` 的 `107/111/113`（开窗 / Reset / ToFocus）不依赖服务端

### I · 特效（shader 合件）· 与 C 的 P0 同一件
- Coffee `UIEffect` 的 **Shiny**（TransitionFilter 4）+ `m_TransitionTex`（pid `-6023190078349834598`）：战役进度条流光（duration 1.0s · rotation 120 · width 0.25 · softness 0.5 · Blend 1/Src 1/Dst 10 · keepAspect 1）与轮抽 `UpdateDeckInfoPanelAnimSync` 缺的是**同一份** ⇒ 一件做完两处受益
- ⛔ **别用 quad 近似**（已裁）

### J · 锻造加点（跨域 → 战斗线）· 1 条
- 锻造 65：`CollectPoints` 找到 `campaign.army == point.cardArmy` 的 forge ⇒ 点数 += amount ⇒ 可领则 `Refresh<ForgeBadge>()`
- 🔴 **必须走 `BattleDriver.LocalAct` / 结算口**（回放记账）；每局给多少要先定 ⇒ **打 `🔶 自己设计`**

### K · 布局时机审计（只读，先审计后补丁）· 2 条
- 菜单 403/404（`LayoutGroupContentFixer`）：我们**没有 uGUI 布局系统** ⇒ 真正要的是「**733** 份 `fixOnEnable` 实例（menus 包 708 + mainmenu 场景 12 + 13 个战场各 1）↔ 我方『内容变化即重算』时机」的**对齐表**
- **做法**：① 脚本扫 `bundle_menus_assets_all/MonoBehaviour/*.json` 的 `fixOnEnable` 字段 ⇒ 出「prefab / GO / 父链」清单 ② 逐条问「我方那个容器在内容变化后有没有重算」③ 缺的补一个 `Recompute()`（补丁**散进各窗所属的包**，⛔ 不新开写手）

---

## §4 跨域移交（本批 4 处，⛔ 不能在本域内自行开工）

| 条目 | 归属线 | 移交理由 | 交接方式 |
|---|---|---|---|
| 菜单 100–111（`DragAndDropItem` 10 条） | **卡组编辑线**（+ 收藏线） | 目标文件 `Deck/DeckRuntime.cs` / `Shell/CollectionTabPage.cs` 都不是菜单域的文件；原版实例在 `Collection Deck` | 本包 F 已含全部判据与验收口，**由卡组编辑线的调度台接管**（`DeckRuntime.cs` 是 8838 行热点，必须先确认无人正在写） |
| 菜单 54/58/60（新卡角标 / banned） | **收藏线**（banned 另需活动线） | `CardProgress` / 卡面层 / 卡组格都在收藏与卡组编辑域 | 同上，随 F |
| 锻造 65（战斗给锻造点） | **战斗线** | 落点在 `BattleDriver.cs`，且必须走回放记账口 | 移交时附「每局给多少 = 待拍板」 |
| 战役 19 的 shader 半 / 战役 53·55（`ItemDrawer`） | **特效线 / 物品抽屉（共用件）** | shader + 贴图与轮抽同源；`ItemDrawer.cs` 1900+ 行被全壳调用 | shader 合一件；`ItemDrawer` 归 C 独占 |

---

## §5 P3 候选表（服务器 / 账户 / 云后端 —— **只给证据 + 时间限定，⛔ 不判 D，调度台裁定**）

| # | 候选 | 本批涉及条目 | 证据（能指到行 / 资产） | 时间限定（为什么今天做不了） |
|---|---|---|---|---|
| 1 | **PlayFab 错误弹窗族** | 菜单 157 的一部分 | `cpp2il_dev/ErrorPopupWindowController.cs:79-110`（`ShowPlayfabError(PlayFabError, Action)`，词条 `PlayfabErrors/<code>`）；`CustomErrors/*` 41 条里 20+ 条是网络/账户语义 | 原版调用链在 `PlayFab` 回调里；引擎侧无 PlayFab 客户端，弹窗入口做了也**没人调** |
| 2 | **CloudScript / CloudHandle** | 商店 46/47、菜单 134 | `CustomErrors/CloudscriptCallError` 词条存在；`ErrorPopupWindowController.ShowErrorMessage(…, cloudscriptTrace)` 的第 3 个形参就是云脚本 trace | 只在云函数回执里出现；本地无云脚本通道 |
| 3 | **登录 / 账户绑定** | —（不属本四域的直接行） | `CustomErrors/{Unauthorized, DuplicateConnection, NewerAccountVersion, ExcededMaxSessionLength}` | 已在调度台 2026-10-25 的第一批裁定里（`PlayerDataManager` 73 行带后端 ⇒ D） |
| 4 | **服务器匹配 / 排行榜** | 战役 108–118（`RankedDeckSelector` 的 `rankedEvent`） | `cpp2il_dev/RankedDeckSelector.cs:260`（`Initialize(IPlayEvent rankedEvent)` 取「上次卡组阵营」= 服务端玩家数据） | 排位事件数据来自 LiveOps/服务端；活动线暂停 + 这批活动已过期 |
| 5 | **远端商店目录 / 卡池 / liveops 数据** | **商店 36–40 · 88–92 · 95–120 · 162**（共 34 条）+ 菜单 18 条 + 锻造 65 的「谁给点」 | 每个活动都是 `Everguild.LiveOps.*Event` 的 `GetBaseData()/GetBaseSave()`（每活动一份 `TSave`）；本批已把 30 个方法的逐条步骤写进施工单 | 🔴 **本地只有过期 SO 快照、无下发**；用户 2026-10-25 暂停活动线；用户拍板「过期的活动不做」 |
| 6 | **推荐码（ReferralManager → PlayFab）** | 菜单 563 | `cpp2il_dev/ReferralPopupWindow.cs:159-186`（`SetReferrer(name, onComplete, onError)`） | 提交路径只有 PlayFab；本地能做的是「明确标注的模拟」 |
| 7 | **遥测埋点 / 崩溃上报 / 反作弊** | 本四域 **0 条**（未出现在 C 行里） | — | 本批不涉及；如要覆盖需另开普查 |

⚠️ **明确不在 P3**：商店的「真实经济」—— 我们的口径是**钱包固定 2000、卡池全解锁、照常能买**，所以 `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick`（88–92）照原版做 UI、购买在本地口径下「必成功」。

---

## §6 三条给写手的提醒（本批实测出来的坑）

1. 🔴 **`账行号` 跨 4 份账，只写数字会撞**（`菜单.tsv:4` 与 `战役.tsv:4` 是两条完全不同的行）⇒ 施工单一律 `<账文件>:<行号>`。
2. 🔴 **`Inventory<T>` 层已经存在**（`Shell/Inventory.cs`，2026-10-26 Q1 补的 `OnItemCountModified` 钩子）—— 商店 59–62 / 239–241 的账里写的「整层没有」**已过期**，别再按「从零建背包」估工。
3. 🔴 **本批有 3 处判据读不出/读错，施工时按施工单里的「判据不完整」标注处理，⛔ 别自己发明语义**：
   - `NavigationPanelController.BuildEventButtons` 的过滤谓词（ILSpy 全是 0/0 链）；
   - `AutoLayoutSpacingByItemSize` 的曲线自变量是**宽高比**（16:9 处与 `16/9÷aspect` 同值 ⇒ 极易抄错）；
   - `ErrorPopupWindowController.ShowErrorMessage` 的 `ShowPopUp` 第 4/5 实参（ILSpy 读成 default，按 `.c` 取）。
