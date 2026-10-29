# B97_MENU —— `账/菜单_外壳.tsv` 的 11 条 B/C 清零（第九十七会话 · 2026-10-29）

> 执行代理 `B97_MENU`（有写权限）。目标 = `菜单_外壳.tsv` 里 **11 条**（B4 + C7）：
> `:58` · `:142` `:143` `:149` `:150` `:151` `:154` `:155` · `:427` `:434` `:438`。
> 判据分诊 → `判据/B96_菜单未分类44_分诊.md` §一「菜单/外壳」那一段。

---

## 〇、一句话结论

**11 条全部判 A**（账补丁 `账/_parts/B97_MENU.tsv`，11 行 + 表头，11 列）。

| 组 | 条数 | 做了什么 | 有没有今天跑得到的验收 |
|---|---|---|---|
| `DynamicList` 家族 | **7** | **新建** `Shell/DynamicList.cs`（照原版逐成员）；把 `LiveOpsEventFilter.Triggers`/`.Conditions` 的**静态类型**改过去（原版 `+0x30`/`+0x38` 就是它） | ✅ 全部可跑（纯逻辑，DL-0..DL-10） |
| 卡格 ban 徽标（`:58` ③） | **1** | `Deck/DeckRuntime.cs` 卡池卡格加 `Ban Icon` + `Banned Text`（几何逐值来自 prefab JSON） | ✅ 数据面可跑；卡池视觉面要 DeckScene 宿主 |
| 主菜单活动容器 | **3** | `Shell/MainMenuRuntime.cs` 类尾补 `CleanupContainers` / `InstantiateLiveOp` / `RefreshLiveOpContainers(bool)` + holder/容器表 + `SetData`/`Release` | ⚠️ **今天不可达**（无在期活动数据）⇒ 只钉「不误伤」 |

`typecheck` = **0 / 0**（运行时时程序集 + 编辑器程序集）。`check_patch_conflicts` = 无重复键。
`check_ledger_points` 的输出里**没有一条**落在本批改过的四个文件上（`DeckRuntime.cs` / `MainMenuRuntime.cs` / `LiveOpsEvent.cs` / `LiveOpsFilter.cs` ⇒ 行号零漂移）。

---

## 一、逐条结论（11 条，一条不落）

### 1. `菜单_外壳.tsv:58` `CardDrawer.ToggleBanned(PlayModes mode)` — **B → A**

**本条残差**（分诊 §二）：① 活动线补 `bannedCardIds` 载荷 · ② `CardView` 加 ban 覆盖层 · ③ DeckRuntime 拿不到 mode · ④ 灰度方向。

**本轮判据订正（第一权威现读）**：`decomp_full/CardDrawer__ToggleBanned.c:1-52` 那一支的 ban 图标**不是**「461×334 大叉」，
而是 `bundle_menus_assets_all` 里 **`Collection Card.prefab`** 的 `CardUI/Ban Icon`：

| 量 | 现读值 | 出处 |
|---|---|---|
| `Collection Card` 根 | 350 × 512 | `GameObject/Collection Card.json` → `RectTransform_-914581999851801256` |
| `CardUI` | stretch + `sizeDelta (−350,−512)`、**`m_LocalScale = 150`** | `RectTransform_6631878794057741656` ⇒ 子件坐标是**卡内单位**（1 单位 = 150 px），与 `CardView.Width/Height = 2.0927/3.3313`（原版 `2DCard` 的尺寸）**同一套** |
| `Ban Icon` | 中心锚、`m_SizeDelta = 1.75 × 1.75`、`m_LocalScale = 1` | `RectTransform_8858818889435772248` |
| 它的 `Image` | sprite = `40k_Cross_icon_cross_big Banned card`（512²、PPU 100）、**`m_PreserveAspect = 1`**、`m_Color` 白、`m_RaycastTarget = 0` | `MonoBehaviour_703390393061078360` |
| 子件 `Banned Text` | anchor `x 0.1..0.9` / `y 0.4..0.6`（`sizeDelta 0`）；TMP `m_fontSizeBase 36` · autosize `0.25…72` · Center(2)/Middle(4096) · `m_fontStyle 17`（Bold+UpperCase）· 白 | `RectTransform_2530570991428695384` / `MonoBehaviour_-3469432334489946792` |
| 本地化键 | **`MenuDeck/Banned`**（官方表 = `Banned` / `禁止`） | `MonoBehaviour_-7220881934041615016.mTerm` |
| 图在不在本地 | **在**：`Resources/Art/ui_menu/40k_Cross_icon_cross_big_Banned_card.png` | 现读目录 |

**动了什么**：`CardPresentation/Deck/DeckRuntime.cs`（**类尾新增 partial 段**）
- `:9537 SetPoolBanIcon(int vi, bool banned, float cx, float cardCenterY, PoolMetrics P)` —— 尺寸 = `1.75 × PxPerUnit × P.CardScale`
  （**与卡本身同一条换算**：卡高 = `CardView.Height × 108 × CardScale` ⇒ 图标:卡 = 1.75 : 2.0927，即原版那个比例）。
  文字框 = `0.8 × 0.2` 个方框、字号按其等比缩（`side × 36/262.5`），`SetAutoFitBox` + `Midline`（原版 Center/Middle）。
- `:9568 ShowPoolBanIcon(int vi, bool on)` = 原版 `banIcon.gameObject.SetActive(banned)`。
- `:9576 ClearPoolBanIcons()` —— 换档（桌面 ⇄ 小屏）要跟「张数条」一起拆（尺寸是建时烘进去的）。
- 调用点：`RefreshPool` 里 `v.SetHighlight(...)` **同一行末尾**追加（行号零漂移）；空位那一支 `ShowPoolCounter(vi,false)` **同一行**追加 `ShowPoolBanIcon(vi,false)`；
  D24 换档那段 `_poolBars.Clear(); _poolBarTexts.Clear();` **同一行**追加 `ClearPoolBanIcons();`。

**四条残差现状**：
① **写入口已在**（上一批）：`Shell/PlayEvent.cs` 文件尾 `PlayEventDataExtensions.SetBannedCards(this PlayEventData, IEnumerable<string>)`。
   ⚠️ **数据源仍是远端**（LiveOps 活动 / `BannedCardsSO`，P3）⇒ 今天 `banned` 恒 false = **原版空态**，功能休眠（⛔ 没伪造禁卡表）。
② 卡格 ban 徽标：**本轮补齐**（见上）。
③ 「这卡组属哪个 mode」：本窗没有该状态（mode 是活动/服务器侧）⇒ 上一批用静态注入点 `DeckRuntime.CurrentEventData` 代替，已标 **🔶 自己设计（非原版）** 并写明理由。
④ 灰度方向：原版 `CollectionCard__UpdateCardVisuals.c:44-47` 是 `isOwned ? isBanned : 1`（**banned 才灰**），
   我们的 `SetHighlight(Unplayable)` ⇒ `SetGrey(1)` **等价**（旧账里那句「SetGrey(banned?0:1)」已被上一批就地订正）。

---

### 2–8. `菜单_外壳.tsv:142 / 143 / 149 / 150 / 151 / 154 / 155` `DynamicList<T>` 七个成员 — **C → A**

**新建** `CardPresentation/Shell/DynamicList.cs`（逐成员照 `out_cpp2il_dev_ilspy/DynamicList.cs` 647 行 + `dump.cs:115653-115848`）：

| 账行 | 成员 | 落点 | 原版体 |
|---|---|---|---|
| `:142` | `Add(T element)` | `DynamicList.cs:132` | `.cs:555` `list.Add` |
| `:143` | `Clear()` | `DynamicList.cs:155` | `.cs:593`（**原地清**，引用不失效；`Array.Clear` 那句是死支） |
| `:149` | `GetList()` | `DynamicList.cs:88` | `.cs:198` `return list`（**不是副本**） |
| `:150` | `IndexOf(T element)` | `DynamicList.cs:146` | `.cs:582` |
| `:151` | `Inser(T element, int index)` | `DynamicList.cs:140` | `.cs:577` `list.Insert(index, element)` —— 🔴 **拼写 `Inser` 与形参次序都是原版的**，⛔ 别「顺手改对」 |
| `:154` | `Remove(T element)` | `DynamicList.cs:162` | `.cs:605`（返回值被丢掉） |
| `:155` | `RemoveAt(int index)` | `DynamicList.cs:168` | `.cs:610` |

另外照建了**基类** `public abstract class DynamicList`（`.cs:642-647`，两个**无体**抽象方法）—— 那两个在账里是
`:148` / `:153` 的 **D（原版本身没有体）**，⛔ **没有**因为「我们新建了基类」就把它们改档。

**关键决策 ①：`[SerializeReference]` 照抄。** 原版字段就是 `[SerializeReference] [SerializeField] private List<T> list;`。
本工程这条链**从不走 `JsonUtility`**（唯一被 Json 化的 LiveOps 类型是 `LiveOpsSave`，`Shell/LiveOpsEvent.cs:993`；
`Triggers`/`Conditions` 只由代码/自检注入）⇒ 该特性在这里惰性、不打断任何装载。⚠️ 将来若有人拿 `JsonUtility` 去序列化
`LiveOpsEventFilter`，必须回来读 `DynamicList.cs` 文件头「如实标 ③」。

**关键决策 ②：它不能是「建了却 0 调用方」的重复件。** 所以把**真消费者**的静态类型改过去了：
`Shell/LiveOpsEvent.cs:135` `Triggers`、`:139` `Conditions`（原版 `EventFilter+0x30/+0x38` 就是 `DynamicList<T>`）——
读它们的是 `Shell/LiveOpsFilter.cs` 的 `RegisterTriggers` / `CleanupTriggers` / `Validate`（都在跑，且有 P1/P2 自检）。
配套改了 3 处 `= new List<...>{...}`（`:1348` / `:1403` / `:1659`）——⚠️ **`DynamicList` 原版没实现 `IEnumerable`**
⇒ 集合初始化器编不过（`CS1922`），改成逐条 `Add`（**同一行**，行号零漂移）。

**⚠️ 顺带发现（账务，交调度台）**：这 7 条与 `泛型与启动链_补读.tsv:725-736` 的 7 条是**同一个键**
⇒ 真复刻率分母虚高 7 条（分诊 §二·三 已记）。合并时**先跑 `check_patch_conflicts.py`**，并注意字典序 ≠ 语义。

---

### 9–11. `菜单_外壳.tsv:427 / 434 / 438` `MainMenuWindow` 容器三件 — **B → A**

**判据（三份 `.c` 全文现读）**：`MainMenuWindow__CleanupContainers.c`(44) · `__InstantiateLiveOp.c`(49) · `__RefreshLiveOpContainers.c`(231)。

**补的两样（= 分诊说的残差）**，全部落在 `CardPresentation/Shell/MainMenuRuntime.cs` **文件末尾的第二个 `partial` 段**（类声明加 `partial`，**零行数变化**）：

| 原版 | 我们的落点 | 说明 |
|---|---|---|
| `CleanupContainers(仍在用的, 当前全部)` | `:4971` | `.c` 逐句：**从尾往前**扫；`Contains` ⇒ 跳过；否则 Destroy + RemoveAt |
| `InstantiateLiveOp(LiveopsMainMenuData)` | `:4993` | `MakeContainer(...)` → 收进 `list(+0xb0)` |
| `RefreshLiveOpContainers(bool forceClean)` | `:5028` | 两支都在：`forceClean` ⇒ 全清重建；否则 ⇒ **增量回收复用**（命中 ⇒ `SetData` 那一跳；未命中 ⇒ 实例化；最后 `CleanupContainers`） |
| holder(`+0x98`) | `_liveOpHolder` = **`ContainerHolder`**（真类，`Shell/OfferContainer.cs:1784`） | `Host` = `GameModes/Viewport/Content` |
| 容器 list(`+0xb0`) | `LiveOpContainerTable` = `_liveOpHolder.GetContainers()` | 🔶 见下 |
| 容器的 `SetData` / `Release` | `:5059` `PlaceModeCardViaContainer` / `ReleaseLiveOpContainer` | 数据没变 ⇒ **只挪根节点**（子件是绝对 px 摆在根的原点系里）；数据变了 ⇒ 只重建那一颗 |

**四条如实标（都写在文件头）**：
① 🔶 **自己设计（非原版）**：原版 `+0x98`(ContainerHolder 组件) 与 `+0xb0`(窗口自己的 `List`) 是**两格**，我们让
   `ContainerHolder.containers` 当那张表（同一批对象、行为等价）⇒ **形状合并**。调度台 2026-10-29 授权。
② **容器 = 卡节点本身**：原版那一步是 `Instantiate(容器 prefab, holder)`，我们的模式卡是**过程式建**的
   （`BuildModeCardForCard`），没有 prefab ⇒ 同一件事的两种写法（原版 prefab 的外观/字段正是 `BuildModeCard` 复刻的那批）。
③ `SetData` 那一跳原版是虚函数（`vtable+0x198`，ILSpy 没给名）⇒ 我们按「数据没变只挪位 / 变了只重建那一颗」实现，对外可观察结果相同。
④ `GetEventsForDisplayingInMenu()` 的等价物 = `ModeCardsFromLiveOpEvents(now)`；🔶 数据源 = `LiveOpsLocalCatalog`（关服，§18·0 允许）。

**改动方式（零行漂移）**：`_placeAllCards` 里 `Put` 那个 `BuildModeCardForCard(...)` 调用**只换了被调名**
（→ `PlaceModeCardViaContainer`，同一行）；`RebuildModeCards(bool)` 的 7 行**原样保留**
（注释 + 一行转发 `RefreshLiveOpContainers(forceClean)`）。因为本文件被账按 `文件:行号` 引用（本批之前最大 `:4718`，
B97_SHOP 又在 `:4066` 用同一行追加过一跳）—— **一切新增都在文件末尾**。

**⚠️ 休眠（如实记）**：本机 13 份活动 SO 的日期**全在 2025 年** ⇒ `ModeCardsFromLiveOpEvents` 今天恒 `null`
⇒ 走手挑 5 张兜底 ⇒ **增量支今天跑不到**（那 5 张**不进容器表**：原版主菜单没有静态卡，且它们的句柄
`_draftStateNode` 一族在 `_placeAllCards` 开头被清空、依赖重建重填，参与复用会拿到已清句柄的旧节点 —— 已在代码里写死原因）。

---

## 二、断言

新建 `CardPresentation/Editor/B97_MENU_Assert.cs`（**没碰** `Editor/RuleEngineTest.cs`）。

| 方法 | 覆盖 | 依赖场景 | 建议宿主 + 插在哪 |
|---|---|---|---|
| `RunAll()` → `CheckB97MenuDynamicList()` | DL-0..DL-10：`Add`/`Count`/索引器/`GetList`（**同一对象**）/`IndexOf`/`Inser`（**形参次序反着来也绿**）/`Remove`/`RemoveAt`/`Clear`（原地清）/`Get<T1>`/`GetAll<T1>`/`GetIndexType`/基类虚分派 + **`Triggers`/`Conditions` 的静态类型已是 `DynamicList<T>`** | 无 | `Editor/ShellScene.cs` 的 `Run()` 里任一 `Section(...)` 之后 |
| `CheckB97MenuBanBadge()` | BB-1..3 禁卡表语义（`null` ⇒ 恒假 / 灌表即生效 / 清表回恒假）；BB-4 徽标 sprite 在本地；BB-5 `MenuDeck/Banned` 能取到词条 | 无 | 同上（`RunAll()` 里已串好） |
| `CheckB97MenuDeckPoolBanIcon(DeckRuntime drv)` | PB-2..6：无禁卡表 ⇒ 隐藏；禁掉第 0 格那张 ⇒ 显示 + 字非空；**灭自证**：禁的是别的卡 ⇒ 仍隐藏 | `DeckScene`（要卡池里有卡） | `Editor/DeckScene.cs`，**卡池几何/张数条那批断言之后**（它会 `RefreshAll()` 两次再还原，插在前面会冲掉那些读数） |
| `CheckB97MenuLiveOpContainers(MainMenuRuntime menu)` | MC-1..5：无在期活动 ⇒ 容器表为空；滚一下（真触发 `RebuildModeCards(false)` = **增量支**）后仍为空；读口不抛 | `MainMenuScene` | `Editor/MainMenuScene.cs`，模式卡 `CheckCard` 那批之后 |

`🧨` 灭自证写在每条的 doc 里（例：`Inser` 的形参次序被「顺手改对」⇒ DL-4b 红；`Triggers` 退回裸 `List<T>` ⇒ DL-10c 红；
把非容器卡误登记 ⇒ MC-3 红）。

---

## 三、账补丁

`资料/完全复刻/账/_parts/B97_MENU.tsv` —— **11 行 + 1 行表头**，11 列，`方法` 列**带完整签名**，
裸 `split("\t")` 读写（未用 csv 模块）。11 条状态全部 **A**；`轮次` 列都带 `B97_MENU 2026-10-29`。

`python -I -X utf8 工具/check_patch_conflicts.py` ⇒ **没有跨补丁重复键（2 份补丁）**。

---

## 四、没查清 / 交调度台

1. 🔴 **`:427/:434/:438` 的增量支没有端到端验收** —— 本机没有任何在期活动数据（13 份 SO 全过期 + 关服）。
   要真验「复用同一颗 / 数据变了才重建」，得先有 liveop 数据（服务器侧）。**本轮只钉了「不误伤 + 不误登记」。**
2. 🔴 **`DynamicList` 的重复账未并**：`菜单_外壳.tsv:142/143/149/150/151/154/155` ↔ `泛型与启动链_补读.tsv:725-736`
   是**同一批成员的两份账**（7 键重复）。本代理只动了 `菜单_外壳.tsv` 那 7 条；**另一份不归我**。合并顺序请按
   `check_patch_conflicts.py` 的裁定（字典序 ≠ 语义）。
3. ⚠️ **`DynamicList` 建了之后的第一处调用点**（防「白建」）：就是 `LiveOpsEventFilter.Triggers`/`.Conditions`
   （`Shell/LiveOpsEvent.cs:135/139`）+ 三处自检注入。⛔ 若将来有人把这两格的类型改回 `List<T>`，`B97_MENU_Assert` 的 DL-10c/d 会红。
4. ⚠️ **`:58` 剩下的只有远端数据**（在期活动的禁卡表）。代码三件（写入口 / 真读 / 徽标）全在；数据一到，
   卡面灰 + 徽标 + 张数条三处同时生效。**今天整条功能休眠**（= 原版空态，不是「我们没做」）。
5. ℹ️ **`MainMenuRuntime.cs` 与 `B97_SHOP` 的共存**：它先前在同一文件写了 `BindMainMenuOfferContainer` 一族
   （`:4066` 同一行追加 + 类尾 89 行）。我只动 `_placeAllCards` 的一个被调名、`RebuildModeCards` 的 7 行、
   两处过期注释、以及**文件末尾另一个 `partial` 段**——**没有覆盖**它那四个成员。合并时按整个文件提交即可。
6. ℹ️ **`ContainerHolder` 的用法**：本批只用了它的 `Host` + `GetContainers()`；`TryAdd` / `MakeContainer`
   那两条**没用**（原版 `InstantiateLiveOp` 也不走 `TryAdd` —— 它直接 `ContainerBuilder.MakeContainer` 再塞 list）。

---

## 五、改动清单

| 文件 | 性质 | 行号影响 |
|---|---|---|
| `CardPresentation/Shell/DynamicList.cs` | **新建**（原版 `DynamicList` / `DynamicList<T>`） | — |
| `CardPresentation/Shell/LiveOpsEvent.cs` | 2 行（`Triggers` / `Conditions` 的静态类型） | 零漂移 |
| `CardPresentation/Shell/LiveOpsFilter.cs` | 3 行（三处 `new List<...>{...}` ⇒ 逐条 `Add`） | 零漂移 |
| `CardPresentation/Deck/DeckRuntime.cs` | 3 处同一行追加 + **类尾 partial 段**（ban 徽标） | 已有行零漂移（+110 行全在 EOF） |
| `CardPresentation/Shell/MainMenuRuntime.cs` | 类声明加 `partial`、`Put` 的被调名、`RebuildModeCards` 7 行重写、2 处过期注释就地订正、**文件末尾 partial 段**（容器三件） | ≤`:4718` 的账落点零漂移 |
| `CardPresentation/Editor/B97_MENU_Assert.cs` | **新建**（断言） | — |
| `资料/完全复刻/账/_parts/B97_MENU.tsv` | **新建**（11 行账补丁） | — |

**红线**：未碰 `Editor/RuleEngineTest.cs` · 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git · 未跑 Unity 批处理；
未碰其它代理名下的文件（`GachaEventWindow.cs` / `RankedTab.cs` / `FactionScore.cs` / `UnitState.cs` / `Enchantment.cs` /
`TutorialScript*.cs` / `SimpleAI.cs` / `BattleContext.cs` / `CampaignScriptRunner.cs` / `TransformUnit_B94B12.cs` /
`RuleCore.cs` / `EffectResolver.cs` / `B94B11_Assert.cs` / `B97_*_Assert.cs`）。
