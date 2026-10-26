# B9 · J8IN —— 基础设施 / 未分类 C 行分档（第八十四会话 · 2026-10-26）

> **身份**：只读判据代理 `J8IN`。⛔ 本报告之外**没有改动**任何 `.cs` / 资产 / 主账表 / 正本（红线）。
> **输入**：`账/其他_基础设施.tsv`（769 行，其中 **C 114**）· `账/未分类_其他.tsv`（754 行，其中 **C 55**）—— 合计 **169 条**。
> **产出**：
> · `判据/B9_基础设施与未分类C_分类.tsv`（169 行：逐条档位 + 理由 + 证据 + 还差什么）
> · `判据/B9_基础设施与未分类C_施工单.tsv`（**117** 行 = 全部 ① 类，12 列，按 **38 个工作单元**归并）
> · 本文件。
> **判据源**：`d:/2/tools/decomp_full/*.c`（第一）· `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（第二，交叉）· `d:/2/tools/assetripper_out/ExportedProject`（Unity 三源之一：prefab/场景/SO 的组件实例，按脚本 guid 计数）· `d:/2/新解包资源/assets_full`（UnityEvent `m_MethodName` 与资产名）。
> **行号一律按二进制/原文核**（`io.open(...,'rb')`；本批没碰任何 `.cs`）。

---

## 一、结论（五档计数）

### 1·1 总表

| 域 | C 行 | ① 可真做 | ② P3 候选 | ③ 第三方/引擎内部 | ④ 非缺口/死码/判据未查清 | ⑤ 编辑器·纯调试 |
|---|---|---|---|---|---|---|
| 其他/基础设施 | 114 | **86** | **5** | 0 | **22** | **1** |
| 未分类/其他 | 55 | **31** | **7** | 0 | **17** | 0 |
| **合计** | **169** | **117** | **12** | **0** | **39** | **1** |

**⛔ 本代理一条 D 都没判。** ② 的 12 条只给证据 + 时间限定，判不判 D 由调度台按方案 §4.1 决定。

### 1·2 五档口径（本批实际怎么用的）

- **① 可真做（117）**：方法体是**本地计算 / 本地状态 / 本地存档**，离线可做。**判据是方法体，不是数据源** —— 原版的输入来自服务器，但我们项目的既定边界是「UI + 服务端数据 ⇒ 数据本地模拟 + 出声」（方案 §五 · b 档 117 类），所以只要方法体不跟云说话，就按 ① 派活。
- **② P3 候选（12）**：方法体里**真的在跟云说话**（逐个给出 `.c` 行号）：`PlayfabWrapper.*` / `ConfigManager.GetConfig<FeatureConfig>()` / 服务器下发的容器实例 id。**时间限定**写在每行的「还差什么」列。
- **③ 第三方 SDK / 引擎内部 = 0 行**。逐个核过两域的 **33 个类名**：没有 Firebase / LiteNetLib / I2.Loc / DOTween / Spine 的**类**。有第三方**痕迹**但**不在这些行本身**的三处，已在行内注明：`Extensions.cs` 里混着 Photon 的助手（`GetPhotonView`/`MergeStringKeys`，本批 3 行是自有数学助手）· `showDevTools` 的消费方 `Reporter`（第三方调试面板）· `ContainerService` 回调里的 `GameAnalytics.RecordTransaction`（第三方埋点）。
- **④ 非缺口 / 死码 / 判据未查清（39）**，再分四种，**每种都写了判据行号**：
  | 子类 | 行数 | 例 |
  |---|---|---|
  | **原版死代码 / 原版自己没实现** | 13 | `Extensions.AlmostEquals`×3（0 调用点）· `GameStaticData.IsOneTurnEffect`/`CheckIfNewCards`/`FoundNewCard`×3 · `PlayerDataManager.GetMD5Hash`/`GetEnviromentString` · `UpdateLevelNews`（`Debug.LogError("[ERROR] Not implemented")`）· **`FatigueCard`×3（原版从不实例化它）** · `GameModes.<>n__0`（编译器蹦床） |
  | **等价实现已存在（我们已做了）** | 12 | `Decorator`×5 · `EphemeralState`×2 · `HuntMarkController` · `BackgroundOverDrawController`×2 · `ActionScheduler.Release`/`Update` |
  | **架构等价物不同（非缺口）** | 12 | `LoadableReference<T>` 全族（同族 5 行已由 G 判 D） |
  | **判据未查清（⚠️ 不是非缺口）** | 2 | `Points<T>.Collect` / `GetID`（体在 Cpp2IL 里全丢，需 VA 反汇编） |
- **⑤ 编辑器工具 / 纯调试（1）**：`PlayerDataManager.showDevTools()` —— 影响行为 = **只影响调试控制台（`Reporter`）的可见性**（`BattleSettingsWindow.cs:155/161` + `Reporter.cs:4091`），正常玩法零影响。

### 1·3 与上游「可达性」判定的差别（重要）

`N10·R2` 在备注里把 **21 行**标成「卡在远端 / 服务器账 / 无本地数据源 ⇒ 本域不动」。本代理按「**方法体里有没有真服务器调用**」重判后：

- **12 行改判 ①**（本地可算/可模拟）：`CollectSpiritStone`×3 · `ExpansionPassSave`×2 · `ItemInfo.Convert` · `PlayerInfo.GetVictoryCounters` · `ReferralContainer`×4 · `ResultCounterSharedData.GetValuesForMode`
- **2 行改判 ④**（判据未查清）：`Points`×2
- **7 行确属 ②**：`ContainerService` · `CryptoString`×5 · `FeatureChecker`

⇒ 「原版的数据在服务端」**不等于**「这一行不能做」。判 ② 的唯一依据是**方法体里那一跳**（本批 7 条都给了行号）。

---

## 二、逐条证据（按类；逐行证据见 `B9_基础设施与未分类C_分类.tsv`）

### 2·1 其他/基础设施

**`PlayerManager`（34 行，全 ① · P0）** — 战斗内 mana / 信仰 / 灵魂石 / 任务点的本地状态。
- 判据：`decomp_full/PlayerManager__<方法>.c` **逐条 grep `Playfab|CloudScript|CloudHandle` = 0**（抽查 `ActivateHero` 12 行 / `AddFaithMana` 50 / `UseMana` 100 / `ResetMana` 156，全 0）。
- 我方：`RuleEngine/Core/PlayerState.cs` 已有 Energy/MaxEnergy/Faith/SpiritStones/QuestPoints/SecretsPlayed/ManaCarry 字段；**差逐方法语义对齐**（不是差整个类）。
- 时间限定：无（纯离线）。

**`PlayerDataManager`（36 行：① 30 · ② 2 · ④ 3 · ⑤ 1）**
- **② 2 条（真服务器跳）**：`AddFriendToList` → `PlayerDataManager__AddFriendToList.c:41` `PlayfabWrapper__GenericCloudScriptHandler<object>(0x3f7,…)`；`FixSkirmishDeck` → `PlayerDataManager._FixSkirmishDeck_d__466__MoveNext.c:327` `…Async<object>(0x45d,…)`。
- **④ 3 条**：`GetEnviromentString`（体 = `return "LIVE"`、**0 调用点**，`PlayerDataManager.cs:7458-7466`）· `GetMD5Hash`（体完整、**0 调用点**，`:10778-10811`）· `UpdateLevelNews`（**原版自己 `Debug.LogError("[ERROR] Not implemented")`**，`:14665-14675`）。
- **⑤ 1 条**：`showDevTools`（`:7416`；两个活调用点见 §1·2）。
- **① 30 条**：全部 `decomp_full` 体 0 服务器调用（含 `SellSpareCards` 族 9 个产物文件、`GetClosedDeck*` 4 条查表、`BlockGame`）。其中一条**原判读错**已被我纠正：`BlockGame` **不是**强制更新链 —— 它是 `LoginManager.DuplicateConnection()` + `WindowsManager.ShowPopUp(两条本地化串)`（`PlayerDataManager__BlockGame.c:40-58`），全本地。
- ⚠️ 三条 0 调用点行的残留风险：ILSpy 会丢体（异步/泛型），故证据写的是「ILSpy 全树 + decomp_full + assets_full 三源全 0」。

**`LoadableReference`（12 行，全 ④）** — `LoadableReference<T>` 是原版「引用式存档 + Addressables」的容器（`LoadableReference.cs:7` 的接口列表），反编译树里被 **114 个文件**消费；我方等价物 = 按名取 `Resources`（`Core/CardArt.cs`）。**同族 5 行（`GetReference`/`GetTargetId`/`GetValidateReferences`…）已由代理 G 判 D**，本批 12 行同源，建议一起归档（若要判 D，需按「同族签名一起数」把 `ILoadableReference<T>` + 114 处消费点过一遍）。

**`GameModes`（12 行：① 8 · ② 3 · ④ 1）**
- ②：`CombatFinished`（`GameModes.cs:550` `currentEvent.MatchFinished(result, cb)`）· `InitializeHandler`（`GameModes._InitializeHandler_d__30__MoveNext.c:70` Playfab）· `<InitializeHandler>b__30_0`（服务器回包闭包）。
- ①：`DeserializeSharedData`（`:458-524` 纯 JSON 解析）· `GetActiveEvent`×3（`:562-660` 纯 LINQ）· 三个 setter + `SetResultCount`（`:418-456` 纯本地写）。
- ④：`<>n__0`（编译器蹦床，`:764-768`）。
- **账里「整层没有（LiveOpsHandler）」已过期** —— 见 §五·6。

**`GameStaticData`（7 行：① 4 · ④ 3）**
- ① 4 条**是活路径**：`GetEffectDesc`/`GetEffectName` 的唯一消费方 `CardEffectItem.LoadEffect`（`CardEffectItem.cs:50/67/137`）在导出集里命中 **15 个文件（含 13 个 Battle Arena 场景）**；`GetStatParam` 被 `GetEffectDesc` 调 **7 次**（`GameStaticData__GetEffectDesc.c:217/232/258/261/264/299/304`）；`GetTraitDesc` 的 3 个调用点全活、只差 `isAlt` 那一支。
- ④ 3 条**是死码**：`IsOneTurnEffect`（体完整、0 调用点）· `CheckIfNewCards` / `FoundNewCard`（private static、0 调用点）。⚠️ **「新卡角标」这个功能是活的**，但走的是另一条链（`BasicCardUI.ToggleNewCardBadge` ← `CardInBoosterPack.cs:124/852`），真缺口已记在 `账/菜单_外壳.tsv:54/60`。

**`ArmyUtilities`（2 行 ①）** / **`VarsGlobal`（8 行 ①）** / **`Extensions`（3 行 ④）** — 见分类表；`Extensions` 三档的 0 调用点证据：ILSpy 全树 `.AlmostEquals(` = 0、`assets_full` 247,612 对象 grep = **0**。

### 2·2 未分类/其他（55 行）

| 类 | 行 | 档 | 关键判据（我只列核过的） |
|---|---|---|---|
| `ActionScheduler` | 2,3,4,5,6 | ④ 2 / ① 3 | 我方 `LiveOpsHandler.cs:81-88`（Release）/ `:90-108`（Tick，`Schedule` 在 `:73-79`）已等价；**缺 `actionId` 去重 + `ScheduleRelative`**（原版 `ActionScheduler.cs:61-74/92-140`）。⚠️ 原版 `Update` **每帧只弹 1 条**、我们全弹（语义差已记） |
| `ArmyProgressBar` | 41 | ① | `ArmyProgressBar.cs:10`；导出集 ~5 实例；整件我们没建 |
| `BackgroundOverDrawController` | 65,66 | ④ | 原版是全局静态栈（`cpp2il BackgroundOverDrawController.cs:11-63`）；我方叠窗不允许两扇整屏底并存（`AllianceMemberOptionsPopup.cs:54-58`）⇒ 等价。**前提写进结论** |
| `CollectSpiritStone` | 165,166,167 | ① | `cpp2il CollectSpiritStone.cs:9/15/21`；我方 `ProfileData.cs:653/682` 的 tally switch 加一支即可；触发 id 610 我方已认（`Core/VfxMap.cs:1933`） |
| `ContainerService` | 191 | ② | `ContainerService.cs:571` `PlayfabWrapper.UnlockContainer`（+ 回调里 `GameAnalytics.RecordTransaction`） |
| `CryptoString` | 215-219 | ② | 它服务的是**账户凭据**：`PlayerDataManager.cs:10194/10199/10281/10404` 全是 `loginPassword`/`deviceId` —— 不是「存档加密」 |
| `CursorByArmySO` | 220 | ① | 21 张 `Cursor_*` json 在 `bundle_armycursors_assets_all/Sprite/`（**现读**）；我方固定 SM 一张 |
| `CustomRaycaster` | 230 | ① | `CustomRaycaster.cs:7`；导出集 ~25 实例；判据已由 E6·R3 查齐（改动请求） |
| `Decorator` | 241-245 | ④ | 唯一子类 `CampaignArmyUIDecorator` 的语义已由 `CampaignTab.cs:820 OnArmyAmountChanged`（doc 自 `:807` 起；`Refresh` 末尾 `:1030` 每刷一次、领取口 `:1232` 再喊一次）实现 |
| `EphemeralState` | 280,281 | ④ | 我方 `Shell/Inventory.cs:797 AddDuration` / `:803 FormatTime` **已建**（2026-10-26） |
| `ExpansionPassSave` | 293,294 | ① | `cpp2il Everguild.LiveOps/ExpansionPassSave.cs:19-38`；我方通行证窗已建、缺这张存档表 |
| `FatigueCard` | 298,299,300 | ④ | **原版自己没实现**：`BattleManager.cs:7307` `Debug.LogWarning("[WARNING] Fatigue is not implemented")`，`:7334-7353` 走「横幅 + 伤害」；`FatigueCard` 全树 0 引用、导出集 **0 实例**（guid `55843dc9…` 只命中自身 meta） |
| `FeatureChecker` | 301 | ② | `FeatureChecker.cs:13` `ConfigManager.GetConfig<FeatureConfig>()`；导出集 10 实例/6 文件（key 可逐条读） |
| `HuntMarkController` | 357 | ④ | 我方 `Core/TraitFrames.cs:1112 ctl.UpdateValue(value)` 已接线；W0–W8 已建 |
| `IconRotation` / `SimpleRotation` | 359,360 / 584 | ① | 2 实例（Black Legion Ring）/ **42 实例**（13 场 ×3 Highlight）；要动 `gen_arena_*.py` + manifest |
| `ItemInfo` | 375 | ① | `ItemInfo.cs:144`；唯一消费方 `Everguild.LiveOps/ShopContainer.cs:167` |
| `MaterialChangerByQuality` | 423,424 | ① | 5 实例**全在战场侧**（guid `656a95f9…` 计 6 含 meta；菜单侧 0）⇒ 归战斗线 |
| `NameGenerator` | 452-455 | ① | `NameGenerator.cs:21/32/97/235`；唯一调用 `MatchData__SetBotOpponent.c:100`；**语料 `.cctor` 丢了**（见 §三） |
| `PlayerInfo` | 478 | ① | `PlayerInfo.cs:419-470` 本地归并 |
| `Points` | 490,491 | ④ | 体在 Cpp2IL 里全丢（`Unmanaged memory load` + 间接跳转）⇒ 判据未查清 |
| `ReferralContainer` | 514-517 | ① | `ReferralContainer.cs:9/14/19/31` 全本地（取 prefab 引用 → 开窗）；1 实例 |
| `RendererExtensions` | 544,546 | ① | `RendererExtensions.cs:7/91`；消费者 `MessageContainer.cs:28/155`（收件箱音效可见性） |
| `ResultCounterSharedData` | 548 | ① | `ResultCounterSharedData.cs:9-28` 体完整可读；是 `GameModes.SetResultCount` 的依赖 |
| `UIResourceCounter` | 706,708 | ① | 数字滚动**已做**（`Shell/TopBar.cs:520/563` + `:578 CounterAnimTime=0.5f`）；**只剩 `layoutGroupContentFixer.FixLayout()`**（药丸宽/格宽重算，`TopBar.cs:516` 明写没做） |

---

## 三、没查清（⛔ 不许当「已查清」，也不许拿猜测填）

1. **`NameGenerator` 的语料**：`trainingNames` 在静态构造里初始化，而 Cpp2IL **丢了这个 `.cctor`**（ILSpy `NameGenerator.cs:13` 只有声明）。要取需 `dump.cs` 的 RVA 级反汇编或从 `all_strings.txt` 里筛。**取不出来就只能写「判据缺」。**
2. **`Points<T>.Collect` / `GetID`**：体 90% 是 `Unmanaged memory load` + 间接调用/间接跳转 ⇒ 需 VA 反汇编（⚠️ 泛型类方法 RVA 写在 `GenericInstMethod` 子块；同址多方法会撞名）。
3. **`GameModes.CombatFinished` 下游**：`IPlayEvent.MatchFinished` 的实现（哪一个事件子类、是否真发 PlayFab）没逐跳核 —— 我只核到「它把结果交给事件」，② 的时间限定就是这个。
4. **`PlayerInfo.GetVictoryCounters` 的源字典**：那张「卡 rawId → 场次」的字典**谁写**没查清（可能来自战报或服务器存档）⇒ 别自造字段。
5. **`BlockGame` 的两条本地化键**：`DAT_18425cea8` / `DAT_1842be318` 的键名没反查出来（文案内容要用 `read_literal` 取）。
6. **`SetLocalStartTime` 的消费方**：全仓 0 命中；写进去之后**谁读**没查清。
7. **`ShouldShowRatePopup` 的 UI**：评分弹窗的原版 UI 判据（prefab / 位置 / 文案）本代理没取，可能得另开一条 UI 活。
8. **`ExpansionPassSave` / `CollectSpiritStone` 的优先级**：两族都「本地可做但当前 0 消费方 / 0 可见后果」（通行证领取钮只出声；102 条成就 0 条用 CollectSpiritStone）—— 做不做由调度台排先后（铁律 11：**只有先后、不做不做**）。
9. **`LoadableReference` 若要判 D**：需按「同族签名一起数」过 `ILoadableReference<T>` + **114 处**消费点，本代理只做到类级 + 5 行既有 D 的先例。
10. **`CryptoString` 的判定边界**：本代理判 ②（它是账户凭据的加密）。若调度台认为「我们要的是把**我们自己的**存档也加密」，那是 `🔶 自己设计`，⛔ 不能记成原版行为。

---

## 四、改动清单

### 4·1 产出文件（只有这三个）

| 文件 | 内容 |
|---|---|
| `资料/完全复刻/判据/B9_基础设施与未分类C_分类.tsv` | 169 行 × 8 列（域/账行号/类/方法/档/一句理由/证据/还差什么） |
| `资料/完全复刻/判据/B9_基础设施与未分类C_施工单.tsv` | 117 行 × 12 列（只含 ①；按 38 个单元归并） |
| `资料/完全复刻/报告/B9_J8IN_基础设施与未分类C分档.md` | 本文件 |

**⛔ 未改**：任何 `.cs` / 资产 / `账/*.tsv` 主表 / `CLAUDE.md` / `项目任务.md` / 交接件。§五 的过期记录**只报不改**（红线）。

### 4·2 施工单的 38 个工作单元（按建议等级）

| 单元 | 行数 | 内容 | 目标文件 | 建议等级 |
|---|---|---|---|---|
| **W22** | 34 | `PlayerManager` mana/信仰/灵魂石/任务点本地状态族 | `RuleEngine/Core/PlayerState.cs` + `RuleCore.cs` | **P0** |
| **W02** | 3 | `GameStaticData` 效果名/描述/数值表（活路径：13 个战场场景） | `CardDisplayWindow.cs` + 新表 | P1 |
| **W04** | 8 | `GameModes` 本地半边（handler + 2 个数据类 + 3 个 GetActiveEvent） | 新 `Shell/GameModesHandler.cs` | P1 |
| **W05/W06/W07/W08/W09/W10/W11/W13/W14/W15/W17/W18/W21** | 26 | `PlayerDataManager` 12 个族（约战/卡组比较/重复登录/计数/DeckInfo/活动副查表/卖多余卡/crate/联机两件/默认值/阵营积分/demoDeck/教程上报） | `Shell/ProfileData.cs` · `RuleEngine/Data/DeckLibrary.cs` · `Shell/WindowsManager.cs` | P1（逐单元见施工单） |
| **W01** | 2 | `ArmyUtilities` 两张表（armyBackgrounds 417 / forgeIcons 421） | `Core/ArmyIcons.cs` + `Resources/Art/` | P1 |
| **U05** | 1 | `CustomRaycaster` 教程点击过滤（**改动请求**：落点在他人的文件） | `Shell/PointerLayer.cs` · `Shell/MenuTutorialFlow.cs` | P1 |
| **U14** | 1 | `ResultCounterSharedData`（与 W04 同批） | `LiveOpsSharedData.cs` | P1 |
| **W12** | 1 | 赛后 XP（与 W23 同批） | `Shell/ProfileData.cs` | P0（账内定级，本代理未降） |
| **W03 · U01/U02/U03/U04/U06/U07/U08/U09/U10/U11/U12/U13/U15 · W16/W19/W20/W23** | 41 | 其余（isAlt 列 · ActionScheduler 补齐 · ArmyProgressBar · CollectSpiritStone · 阵营指针图 · 通行证存档 · 战场自转 / 画质材质 · ItemInfo.Convert · NameGenerator · 胜利计数 · Referral 容器 · 收件箱可见性 · 药丸宽 · 战报 altId · localStartTime · 评分弹窗 · VarsGlobal 8 条） | 见施工单 | P2 |

### 4·3 建议派活顺序（判据已定 · 文件互不相交）

1. **先 W22**（P0 · 34 条 · 单一文件域 `RuleEngine/Core/`，与别的线不抢）。
2. **并行**：W02（`CardDisplayWindow.cs` + 新表）· W04+U14（新 `Shell/GameModesHandler.cs`）· W01+U04（`Core/ArmyIcons.cs` / `CardArt.cs`，都是资源表）。
3. **再**：`PlayerDataManager` 的 12 个族（多数落在 `Shell/ProfileData.cs` ⇒ **同一文件只能一个写手**，建议一次派 1–2 个代理串着做完）。
4. **战场侧三件**（U07/U08 + IconRotation/SimpleRotation）**必须与 ArenaBuilder 同批**（改了要重跑受影响的场，且别 `--all`）。
5. **U05** 是**改动请求**（文件所有权不在本域）⇒ 交给教程线/外壳线。

---

## 五、🔴 顺手发现（铁律 5：账/注释与事实不符 —— **只报不改**，请调度台订正）

> 这 9 条的共性：**上游代理写账/注释时，我们后来把事情做了（或原版本来就没做）**，而那句话没回头改。它们直接影响「这条到底还欠不欠」，也会误导下一个会话去**重做已做的东西**。

1. **`账/未分类_其他.tsv` 的 `ActionScheduler` 5 行（2/3/4/5/6）备注「我们没有延时回调队列 / 无服务器时间源」已过期。**
   实际：`Shell/LiveOpsHandler.cs:68-108` 有 `LiveOpsScheduler`（`Schedule(DateTime,Action)` / `Release` / `Tick` + `PendingCount`），且 **R14 起 `LiveOpsPump` 已把它挂到每帧**。⇒ 只剩 `actionId` 去重 + `ScheduleRelative` + 「每帧 1 条」三处真缺口。
2. **`Battle/TutorialOverlay.cs:50` 写「`CursorByArmySO` 那张对应表**本地没解出来**，全库只有它的 MonoScript」—— 已过期。**
   实际：`d:/2/新解包资源/assets_full/bundle_armycursors_assets_all/Sprite/` 现读 **21 个** `Cursor_*.json`（11 个阵营 × cursor/glow）。台账里这一行（未分类 220）本来就是对的，是**代码注释**错。
3. **未分类 357（`HuntMarkController.UpdateValue`）备注「`TraitEffectControllers.Attach` 没有任何生产调用点、`UpdateValue` 也没有调用者 —— 玩家仍看不到计数器」—— 已过期。**
   实际：`Core/TraitFrames.cs:1037` 有 `TraitEffectControllers.Attach(...)`、`:1112` 有 `ctl.UpdateValue(value)`（W0–W8 已建）。
4. **未分类 280/281（`EphemeralState`）备注「我们只存了一个 int 占位」—— 已过期。**
   实际：`Shell/Inventory.cs:748` 已有同名 `EphemeralState` 类，`:797 AddDuration`、`:803 FormatTime` 都实现（`Inventory.cs:26` 自己也写了「2026-10-26 就地订正」）。
5. **未分类 706/708（`UIResourceCounter`）备注「数字滚动动画没做」—— 只是半过期。**
   实际：滚动已做（`Shell/TopBar.cs:520 StepCounter`（逐条循环在 `:563 StepCounters`） + `:578 CounterAnimTime=0.5f` + `MainMenuRuntime.cs:2253`）；**真缺口只剩 `layoutGroupContentFixer.FixLayout()`**（`TopBar.cs:516` 明写「只重写文字、不重算药丸宽」）。
6. **`账/其他_基础设施.tsv` 的 `GameModes` 12 行备注「整层没有（判据/泛型与启动链.md §三 #1 `LiveOpsHandler`）」—— 已过期。**
   实际：`Shell/LiveOpsHandler.cs`（1407 行）+ `LiveOpsManager.cs`(356) + `LiveOpsSharedData.cs`(414) + `LiveOpsLocalCatalog.cs`(1139) + `ParentEventHandler`（`LiveOpsFilter.cs:1031`）都在；缺的只是 **`GameModes` 这个 handler 本身**。
7. **`账/其他_基础设施.tsv:522`（`UpdateLevelNews`）备注「升级新闻（服端下发）未做」—— 方向错。**
   实际：**原版自己没实现** —— `PlayerDataManager.cs:14671` = `Debug.LogError("[ERROR] Not implemented")`。⚠️ 别和 `UpdateUserLevelNews`（`:14551`/`:14620`，真发 CloudScript）混。
8. **未分类 298/299/300（`FatigueCard`）备注「缺：原版 FatigueCard.DrawFatigue 是整段演出」—— 判据错（会让人去建一段原版根本没有的演出）。**
   实际：`BattleManager.ResolveFatigue` 自己打 `[WARNING] Fatigue is not implemented`（`BattleManager.cs:7307`）然后走「横幅 + 伤害」；`FatigueCard` **全树 0 引用、导出集 0 实例**。我们现在的「疲劳横幅 + 伤害」就是原版行为。
9. **未分类 165-167（`CollectSpiritStone`）备注「日任务定义在服务端、本地判不了」—— 可达性被低估。**
   实际：tally 判定就在**我方**代码里（`Shell/ProfileData.cs:653 TallyDeltaOf`，`:682` 的 `case "CollectFaith"` 是同一个形状）⇒ 加一支就能跑，不是「判不了」。

**另有两条不属于「过期」，但值得记：**

10. **`Points<T>` 是泛型基类，`Collect`/`GetID` 的体在 Cpp2IL 里全丢** —— 若以后有人报「原版这方法没逻辑」，那是反编译产物问题，不是原版没有（按项目口径：**「Cpp2IL 说空」一律用 RVA 复核**）。
11. **`CursorByArmySO` 与 `armyIcons`/`forgeIcons` 是三个不同的 SO 实例**（`ArmyUtilities` 的 `+0x28 / +0x30 / +0x38`）。本批 W01 施工时**⛔ 别把一个族的表复制给另一个族** —— `Core/ArmyIcons.cs:41-44` 已经警告过一次。

---

## 六、账补丁（调度台 2026-10-26 授权后追加）

### 6·1 `账/_parts/B9_J8IN.tsv` —— 36 行

- 表头**原样复制** 11 列；`方法` 全部是**单方法完整签名带 `(...)`**；键 = `(域, 原版类, 方法)`，与主表**逐字**对得上（36 条全部从主表整行复制后只改指定列）。
- **只改 4 列**：`差异`（36）· `备注`（36）· `状态`（9）· `等级`（9）。`判据出处` / `判据要点` / `我们的落点` / `轮次` **一格没动**。
- **9 条状态变更**（= 报告 §五 里「账写的是 C、其实已做或原版没有」的那 9 条）：
  | 行 | 方法 | C → | 理由 |
  |---|---|---|---|
  | 未分类 2 | `ActionScheduler.Release` | **B** | `LiveOpsHandler.cs:81-88` 已等价 |
  | 未分类 6 | `ActionScheduler.Update` | **B** | `LiveOpsHandler.cs:90-108` Tick + `LiveOpsPump`（语义差：原版每帧 1 条） |
  | 未分类 280 | `EphemeralState.AddDuration` | **B** | `Shell/Inventory.cs:797` |
  | 未分类 281 | `EphemeralState.FormatTime` | **B** | `Shell/Inventory.cs:803` |
  | 未分类 357 | `HuntMarkController.UpdateValue` | **B** | `Core/TraitFrames.cs:1112` 已接线 |
  | 其他 522 | `PlayerDataManager.UpdateLevelNews` | **D** | **原版自己没实现**（`:14671` `Not implemented`） |
  | 未分类 298/299/300 | `FatigueCard`×3 | **D** | **原版从不实例化**（`BattleManager.cs:7307` + 导出集 0 实例） |
- **没改状态、只订正 差异/备注 的 27 行**（理由写在每行备注里，格式 = 原判据 + 实读值 + 出处）：
  `GameModes` **12 行**（「整层没有 LiveOpsHandler」过期；缺的只是这个 handler）· `ActionScheduler` 3 行（3/4/5：只剩 `actionId` 与 `ScheduleRelative`）· `CollectSpiritStone` 3 行（可达性被低估）· `CryptoString` 5 行（它保护的是**登录凭据**，不是存档）· `UIResourceCounter` 2 行（滚动已做，残差只剩 `FixLayout` —— **故意留 C**，别当已做完核销）· `CursorByArmySO` 1 行（订正的是我方代码注释 `TutorialOverlay.cs:50`）· `BlockGame` 1 行（**不是**强制更新链，是重复登录弹窗）。
- **三项自检（都在副本上做，⛔ 没碰主表、没合并）**：
  1. **模拟合并**（`merge_ledger_parts.py --dry` + 真合并到 `D:/tmp/j8in/ledger_copy` 副本）：`更新 36 · 新增 0 · 无变化 0` ⇒ **36 个键各唯一命中 1 行**，无「>1 处命中」无「0 处命中」；合并后主表仍是 769 / 754 行、无列数异常、**LF 未翻**。
  2. **`verify_annot`（副本）**：`同名 777 · 异名已注解 2783 · 缺注解 1` —— **仍是 1**，且那 1 条是 `战斗逻辑驱动:780 CardScript.HasWhileInPlayAdjacentEffect()`（不归本批）。
  3. **`check_patch_conflicts.py`（真 `_parts/`，未用 `--all`）**：`✅ 没有跨补丁重复键（2 份补丁）`。
- ⚠️ **差分核对**：副本 vs 原表逐行比 —— 36 行变化，**只有** 差异/备注/状态/等级 四列变；`域/原版类/方法` 一个都没动（键不可能漂移）。

### 6·2 🔴 `PlayerManager` 这 34 条【不是单文件】—— 请按两个写手/两个批次派

调度台问的那件事，答案要点名 **两个文件**（都在 `MyGame/Assets/RuleEngine/Core/`）：

| 文件 | 现读规模 | 该落的行 | 条数 |
|---|---|---|---|
| **`RuleEngine/Core/PlayerState.cs`** | **348 行**（Mana 23 处 / Faith 3 / SpiritStones 3 / QuestPoints 14） | 纯**取值口**：`GetCurrentFaith` · `GetCurrentMaintenance` · `GetCurrentQuestPoints` · `GetCurrentSpiritStone` · `GetSavedMana` · `GetSecretsPlayed` · `GetTotalMana` · `GetUnusedMana` · `GetHeroFullPosition` · `GetNextTo{Mana,PlasmaMana,PsychicMana}Transform` · `Get{Plasma,Psychic,SpiritStone}ManaTransform`（共 15）+ 纯查询 `HasEnoughMana`（1） | **16** |
| **`RuleEngine/Core/RuleCore.cs`** | **14,873 行**（Mana **501** 处 · Faith 39 · SpiritStone 17） | 改状态/结算：`ActivateHero` · `AddFaithMana` · `AddNewMana` · `AddQuestPoints` · `AddSecretPlayed` · `AddSpiritStoneMana` · `DestroyManaSlot` · `RefillCustomMana` · `RefillMana` · `RefillOneMana` · `ResetMana` · `SetAccumulationMana` · `SetQuestPointsWithDelay` · `SetSavedMana` · `SetupFullHero` · `ToggleCurrentMana` · `UseMana` · `UseSpiritStoneEnergy` | **18** |

- 🔴 **`RuleCore.cs` 是全项目最热的共用文件**（出牌与结算主链都在里面，且它已经**常年有写手**）⇒ 派这条活时：
  - **新成员一律追加到类尾**（CLAUDE.md §三「N9 实测」：插在中间会让整本账的 A 行落点整体漂移 —— `NetBattle.cs` 插 6 行就报 10 条漂移）；
  - 先做 `PlayerState.cs` 那 16 条（纯取值 + 少量字段补齐，冲突面小），**再**在「没有别的 `.cs` 写手」的窗口做 `RuleCore.cs` 那 18 条；
  - 可复用件已存在，⛔ 别重写：`RuleCore.cs:12925 SpendNormalMana` · `:11714 CanUseFaithAbility` / `:11740 UseFaithAbility`。
- 验证面：18 条改状态的全要配 `RuleEngineTest.Run` 断言，并且 **`ruleprobe check` 指纹必须不变**（这一族是纯引擎状态，不该改 AI 偏好或结算顺序）。

### 6·3 派活顺序（调度台已采纳，此处留档）

1. **W22 = `PlayerManager` mana 族 34 条**（P0；按 §6·2 拆 16 + 18 两批）。
2. 排队：**W02**（`CardEffectItem` 活路径 = 13 个战场场景 + 2 处）· **W04+U14**（新 `Shell/GameModesHandler.cs` + `ResultCounterSharedData`，与另外 12 条 GameModes 同批）· **W01+U04**（`Core/ArmyIcons.cs` 两张表 + 阵营指针图，都是资源表）。
3. ⛔ 提醒：`Shell/ProfileData.cs` 会被 `PlayerDataManager` 的 12 个族**同时**撞上（W05/W06/W07/W08/W09/W10/W11/W13/W14/W15/W17/W18/W19/W20/W21 共 14 个单元）⇒ **同一时刻只能一个写手**，建议一次派 1–2 个、串着做完再换。
