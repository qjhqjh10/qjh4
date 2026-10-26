# B8_AT · 悬案定案报告（四组 39 条）

> 判据代理 `B8_AT`，2026-10-26。**只写判据与账补丁，未改任何 `.cs`。**
> 产物：
> - 账补丁 `资料/完全复刻/账/_parts/B8_AT.tsv`（39 行 + 表头，11 列，LF）
> - 判据表 `资料/完全复刻/判据/B8_AT_悬案定案表.tsv`（39 行，8 列）
>
> 机检：`check_patch_conflicts.py` → ✅ 无跨补丁重复键；`merge_ledger_parts.py --dry` → **更新 48 · 新增 0 · 无变化 0**（48 = 本代理 39 + 另一代理 9，全部按 `(原版类, 方法)` 唯一命中）。

## 一、结论与关键数字

| 组 | 条数 | D | C | B | A（未改） |
|---|---|---|---|---|---|
| A 组（`B8_死代码D复判` 里「建议 A + 落点 —/不需要」） | **21** | 15 | 4 | 2 | — |
| B 组（`无落点610` 的 `无处可落`） | **12** | 12 | 0 | 0 | — |
| C 组（`收藏:203` 二选一） | **1** | 0 | 1 | 0 | — |
| D 组（`SegmentationController` 5 条） | **5** | 5 | 0 | 0 | — |
| **合计** | **39** | **32** | **5** | **2** | **1（`UICardInfoItem.ToggleInvalid`）** |

- **A 组实际是 22 条**（按题面筛选条件命中的行），其中 **1 条不在「待定案」范围内**：`卡牌面_渲染.tsv:168 UICardInfoItem.ToggleInvalid(bool)` 的账上状态已是 **A**、落点 `CardPresentation/Core/CardView.cs:7870（SetAlpha）` 经现核是**真声明行**（`public void SetAlpha(float a)`），`差异` 已写「全解锁下两侧都不可达」+ `[非缺口]` 标注，2026-10-25 已有裁定。⇒ **维持 A，本次不打补丁**（只在本报告与本组判据表旁的说明里记一笔）。
- **判 D 的三种真依据**（每行备注里都写明是哪一种，⛔ 没有一行是「影响小所以不做」）：
  1. **原版方法体为空 / 只有一句日志 / 只有一句 LogWarning**（`RVA 0x4B33B0` 或 `.c` 实读）—— 12 条；
  2. **原版出货里不可达**（0 调用点三源核实 + 无数据入口 / 形参不可能挂 UnityEvent）—— 12 条；
  3. **服务端 / 账户侧（P3）** —— 8 条。
- **判 C 的前置**：原版**有行为且可达**（有活调用点或被内联进活方法），我方**没有**对应实现——不是「结构等价」（那是 B），也不是「原版没有」（那是 D）。

## 二、逐条依据摘要

### A 组（21 条打补丁）

| 新 | 类.方法 | 决定依据（一行） |
|---|---|---|
| D | `CardScript.HasMutation()` | 0 调用点三源 + `SpellType.Mutation=130`（dump.cs:45966）在 1,351 张里 **0 张** |
| D | `CardScript.ResolvePsyker()` | 0 调用点三源（private，`dump.cs:35886`）+ `UsesPsychicEnergy()`=`return false`（cpp2il EntityScript.cs:3026-3029）+ psyker 只作卡名 |
| D | `CardScript.TriggerMutation()` | 0 调用点三源 + `AbilityTrigger.Mutated=460`（dump.cs:20294）全池 **0 条** |
| D | `CustomAbilityLogic.CanPlayAbility(...)` | 体 = `return true`；全仓 **0 个子类** ⇒ 虚方法从不被调 |
| D | `CustomAbilityLogic.PlayAbility(...)` | **`RVA 0x4B33B0` 空桩**（dump.cs:21762）+ cpp2il `{ }` |
| D | `EntityScript.GetMutations()` | 0 调用点三源 + spellType 130 池内 0 |
| **C** | `BattleCardManager.CheckToBeAlternate(...)` | 真体（dump.cs:25866）**被内联进活的 `CreateCard`** ⇒ 原版有行为且可达；我们战斗建卡不查异画（缺口已在 `CreateCard` 行 C，**本条不另计工作量**） |
| D | `BattleScoreUiManager.ScoreUpAnimation(Action)` | 0 调用点三源 + `Action` 形参不可能挂 UnityEvent ⇒ 不可达（⚠️ **体非空**：161 行真动画） |
| D | `CemeteryManager.DisplayCardTraits(...)` | **`RVA 0x4B33B0` 空桩**（dump.cs:37155）+ cpp2il `{ }` |
| D | `ClockManager.ChangeMaterialHurryUp(bool)` | 0 调用点三源 + private ⇒ 不可挂 UnityEvent（⚠️ 体非空：换 `timeRunningOutMaterial`） |
| D | `ClockManager.SetFillPercentage(float)` | **`RVA 0x4B33B0` 空桩**（dump.cs:37828） |
| **B** | `ManaBuff.DeepCopy()` | 体 = `MemberwiseClone`（cpp2il ManaBuff.cs:48-70）；**行为活在内联**（见 §三·1）；我们等价 = `RuleEngine/Core/EffectText.cs:857 EffectOp.Clone` / `EffectResolver.cs:2956 b.Clone()` |
| **B** | `ManaBuff.GetNewInstanceOfThisEffect()` | 体 = new + 逐字段拷 11 格（值类型全拷）；非 override（dump.cs:19646）⇒ 虚拟派发到不了；我们等价 = 同上 |
| D | `PracticeModePopup.IsSavedDeck(...)` | 0 调用点三源 + private + 返回 bool ⇒ 不可达（连带：原版「记住上次卡组」只写不读） |
| D | `DeckGeneralInfoDemo.SetUnlockState(bool)` | 0 调用点三源 + private（⚠️ 体非空：换 `Demo/DeckLockedForTutorial` 串） |
| D | `DeckManager.ToggleCardbackPreview(bool)` | 0 调用点三源 + private；**已排掉一个假调用方**（见 §五·2） |
| D | `PrebuiltDeck.RefreshList()` | 0 调用点三源 + 体只有「置空表」一句 |
| D | `PrebuiltPack.SetCardLibrary(List<...>)` | 0 调用点三源 + 我们预组包生成期装好、无运行期灌库这一步 |
| **C** | `CosmeticItem.UnloadReferences()` | 体 = `imageReference.Release()`（dump.cs:18129）；**4 个活调用点**；我们 `CardArt._cache` 只增不减 ⇒ 不是结构等价 |
| **C** | `CursorByArmySO.ReleaseArmyCursor(CardArmy)` | 体 = 放该阵营 cursor/highlight 两个句柄（dump.cs:50941）；**活调用点** `ArmyUtilities.ReleaseCursorForArmy` |
| **C** | `FlavourTextSO.ReleaseFlavorBackground(CardArmy)` | 体 = 命中就 Release + 无条件 LogWarning（dump.cs:22799）；**2 个活调用点** |

### B 组（12 条，全 D）

| 类.方法 | 判 D 的真依据 |
|---|---|
| `BattleManager.GcCollectWhileInactive()` | 状态机 = `WaitForSeconds` → `ActionsLeftInQueue()<1` 时 `GC.Collect()`，**不写任何状态/表现**。⚠️ **它不��死代码**（cpp2il BattleManager.cs:35259 有 `StartCoroutine`） |
| `BattleManager.IsDesiredHumanOpponent()` | 0 调用点三源 + 纯谓词；语义 = 匹配时判要不要真人对手（服务端侧） |
| `BattleManager.IsGameAlteringELO(MatchType)` | 唯一调用点在 `_ShowFinishScreen_d__362__MoveNext`，且包在 `if (onlineMatchResult != null)` 内、只算 `startRank = NewRating − EarnedRating` ⇒ **离线局永不进入** |
| `BattleManager.IsSupiciousGame(bool)` | 0 调用点三源 + private + 纯反作弊谓词 |
| `BattleManager.RequiresResultRecording()` | 0 调用点三源 + 纯谓词 `matchType != 50`（「要不要上报战果」） |
| `BattleManager.SaveMatchResultAnalytics(...)` | 服务端/遥测：195 行 analytics 载荷 + 上传；活调用点 `FinishedProcessingMatchResult` |
| `BattleManager.SaveMatchWinner()` | 服务端链一环：校验阶段 → 写 winner → 转调 `ProcessMatchResult()`（该行同表已判 D，理由=服务端上报） |
| `BattleManager.ShareScreenResult()` | 整链**只有日志**：末跳 `PlayerDataManager.ShareScreenPicture` 体只有一句 `CustomDebug.Log`（decomp_full:8-16） |
| `CardScript.ShowAbilityAnim()` | **`RVA 0x4B33B0` 空桩**（dump.cs:35811）+ cpp2il `{ }` + 0 调用点 |
| `CardScript.ShowAbilityIcon()` | **`RVA 0x4B33B0` 空桩**（dump.cs:35808）+ cpp2il `{ }` + 0 调用点 |
| `CardScript.ShowDescription(bool)` | 体**只有一句 `CustomDebug.Log`**（⚠️ **它是活的**：`ResolveRevealOrders`/`ResolveDiscardFromHand` 状态机都调它） |
| `CardScript.ShowManaCostAura()` | 体**只有一句 `LogWarning("ShowManaCostAura not implemented")`**（⚠️ **它是活的**：`AiScripted.ExecuteAction`） |

### C 组（1 条）

`收藏.tsv:203 GameModesIconCollection.Release()`（账务订正：这是**嵌套私有类 `GameModeIcon`** 的方法）：

- 体 = `AssetReferenceTyped<object>.Release(handle)`（decomp_full `GameModesIconCollection.GameModeIcon__Release.c:9-14`，dump.cs:119333 / RVA 0x878370）。
- 本体 0 直调，**同一句被内联进活的** `GameModesIconCollection.Release(Nullable<PlayModes>)`（`…__Release.c:38-52`）与 `ReleaseAll()`（:20-36）。
- 唯一入口 `ScriptableObjectsCollectionsUtilities.ReleaseGameModeIcon`（`:3-8`）的两个调用点**都传 `null`**（cpp2il `CollectionDeck.cs:434` / `LiveopUIDrawer_GameModeIcon.cs:93`）⇒ 出货里只放默认图标。
- 我们：`CardArt.Get` 的 `_cache` **只增不减**（`Core/CardArt.cs:1060-1067`），模式图标走 `CardArt.MenuUi`（`Shell/MenuDraw.cs:3506`）⇒ **没有释放那一段**，不是结构等价。
- **可执行结论（选①，但比题面的①便宜得多）**：**不必建 Addressables 句柄层**，只在既有 `CardArt._cache` 上加「解除登记」：
  - 落点 = `CardPresentation/Core/CardArt.cs:1060`，新增 `CardArt.Release(string name)`（`_cache.Remove`；`Resources.UnloadAsset` 只在确认无持有者时）；
  - 调用口 = `CardPresentation/Shell/MenuDraw.cs:3506`（模式图标那格重建前先放）/ 卡组格关闭处；
  - **工作量：小（≈25 行 + 1–2 处调用）**，且**同一工作项一次覆盖 A 组的 201 / 221 / 304 三条**（卡背、阵营光标、flavour 背景）。
  - 先例（本仓已有、照抄即可）：`Shell/ItemDrawer.cs:1889 _handles` + `ItemDrawer.Release` + `ItemDrawerLeash.cs` —— 同样是「原版放 Addressables 句柄，我们的净效果等价物 = 解除登记」。

### D 组（5 条，全 D）

`SegmentationController` 的 `GetAttribute<T>` / `GetRawAttribute` / `IncreaseAttribute` / `RemoveTag` / `SetAttribute`：

- **数据面 = PlayFab 玩家档案/属性/Tags**（P3）：`Unpack(GetPlayerCombinedInfoResultPayload)` 的形参就是 PlayFab 类型；`SetAttribute` / `AddTag` 的 `upload=true` 走 `PlayfabWrapper.GenericCloudScriptHandler(0x41c, …)`（云脚本，`decomp_full/SegmentationController__SetAttribute.c:20-38`）。
- **客户端可达性**：唯一入口 = `SingletonBehaviour<SegmentationController>.Instance`，而 `SingletonBehaviour<T>.get_Instance` **不创建对象**，只做 `FindObjectOfType<T>()`，找不到就 LogError 返回 null（`decomp_generic_trial/out/SingletonBehaviour_object___get_Instance__1258290.c:36-52`）；27 处调用点全部 **null-check 后放弃**（`Everguild.LiveOps/TagCondition.cs:36-41`、`AttributeCondition.cs:31-37`）。
- ⚠️ **题面那条「84 个 bundle 里 0 实例」我没能坐实**（见 §五·4）—— 判 D 不靠它。

## 三、与上一路建议不一致的（逐条 + 为什么）

1. **`ManaBuff.DeepCopy()` / `GetNewInstanceOfThisEffect()`：上一路的「0 调用者 ⇒ 无行为」不成立。**
   `AbilityLogic.PlayAbility` 里有一句 `((object)manaBuff).MemberwiseClone()`（cpp2il `AbilityLogic.cs:275`）—— 那正是 `ManaBuff.DeepCopy()` 的体被**编译器内联**。⇒ 行为在原版**是可达的**。
   两人的结论碰巧都是 B，但**理由必须换**：不是「死了所以无所谓」，而是「原版活的、我们结构等价」。
   ⇒ 方法论：**ILSpy 全树「只找到定义」≠「不可达」—— 内联会让调用点从源码里消失**。本条与 CLAUDE.md 已有的「搜不到赋值点 ≠ 没有来源」「cpp2il 会静默丢字段赋值」同族。

2. **`GcCollectWhileInactive()`：定位表把它当「无需复刻的死东西」，但它有活调用点。**
   cpp2il `BattleManager.cs:35259` 现读 `StartCoroutine(obj8)`（`obj8` 就是该协程的状态机）；decomp_full 没解出调用者 = 724 条缺体所致。判 D 的依据换成「只触发一次 GC、不写任何状态/表现」。

3. **`ShowDescription` / `ShowManaCostAura`：账里写「0 调用点」，实际都是活的。**
   前者被 `BattleManager._ResolveRevealOrders_d__549__MoveNext.c:53` 与 `_ResolveDiscardFromHand_d__544__MoveNext.c` 调；后者唯一调用者 `AiScripted.ExecuteAction`。⇒ 这两条判 D 的依据只能是「方法体只有日志」（口径 1 逐字命中），⛔ 不能写「0 调用点」。

4. **`SaveMatchWinner()`：定位表判「服务端」，我同意结论但把理由说准。**
   它体内**没有**任何网络调用 —— 体是「校验阶段(0x294==0x3c) → 写 winner(+0x2e4) → 转调 `ProcessMatchResult()`」。之所以仍是服务端，是因为 `ProcessMatchResult()` 那行同表已判 D（服务端上报）。本行的本地可观察部分 = 结算屏，走 `ShowFinishScreen`（B 行）。

5. **`IsGameAlteringELO`：定位表只说「服务端」，我补上「客户端不执行它」的代码行。**
   唯一调用点在 `_ShowFinishScreen_d__362__MoveNext` 且包在 `if (currentMatchResult.onlineMatchResult != null)` 内（cpp2il `BattleManager.cs:13312`）。

6. **三条 Addressables 释放（201/221/304）：上一路建议 A，我改 C。**
   上一路的 A 理由「无引用计数可放」描述的是**我们**这一侧，但原版那三步**是活的、且我们确实缺**；`CardArt._cache` 只增不减是真差异。按口径 2 ⇒ C。这与 C 组（收藏:203）是**同一个工作项**，合计 ≈25 行，成本极低。

7. **A 组其余 15 条：上一路建议 A（「死代码不足以判 D」），我改 D。**
   上一路把口径悬给调度台，本批已由调度台定：口径 1 第三款「没有任何可观察副作用」+ 口径 4「出货数据 0 入口」。**每一行都写清了是「空体」「只有日志」还是「不可达」**，且都带时间限定。

## 四、没查清的部分（如实记）

1. **A 组「0 调用点」只挡得住 UnityEvent 与委托，挡不住「调用方本身是 724 条缺体之一」。**
   我做的三源 = `decomp_full` 全树 / `out_cpp2il_dev_ilspy` 全树 / `assets_full` 25 个方法名统搜（0 命中；对照 `CheckLinkText` 4 命中证明搜法有效）。铁律 17 的未覆盖面②（**724 条无方法体**）意味着理论上仍可能漏一个调用方。已逐行在备注里标「时间限定」。
2. **`SaveMatchResultAnalytics` 195 行只读了前 ~60 行 + 文件头**；中段未逐句（形态已明确：analytics 载荷拼装）。判 D 不依赖逐句。
3. **`PrebuiltPack.SetCardLibrary` 92 行只读到第 ~50 行**（清表 + 逐项 Add 的形状已明确）。
4. **`SingletonBehaviour<T>.Instance` 只确认了「不创建对象、只 `FindObjectOfType`」这一支**；`SingletonInitializableBehaviour<T>`（另一个类，另有 `get_Instance`）**没读**，本批 5 条也不依赖它。
5. **`SegmentationController.GetAttribute<T>` 只读了 `<int>` 实例**；`<__Il2CppFullySharedGenericType>` 实例（RVA 0xD9D200）未读。
6. **D 组「客户端到底有没有 `SegmentationController` 实例」仍未坐实**（见 §五·4）—— 我的 D 判据绕开了它，但如果将来在某个 bundle 里抓到实例，这 5 行的「不可达」那一半要重核（「数据面在 PlayFab」那一半不受影响）。
7. **`等级` 列**给 C 行填的 P0/P1 是**我按方案 §五 的域归属判的**，不是原版判据。

## 五、顺手发现（不在四组内，交调度台分流）

1. **`decomp_full` 又多 9 例「文件名与内容不符」**（已知 6 例 → 本批新抓 9 例）。最阴的是它们**看起来像真判据**：

   | 文件名 | 实际内容 | 危险 |
   |---|---|---|
   | `CustomAbilityLogic__CanPlayAbility.c` | `BaseAddressableLibrary__ValidateLibraryLoad { return 1; }` | 会让人以为「原版恒 true」（碰巧对，但理由错） |
   | `CustomAbilityLogic__PlayAbility.c` | `NetworkingPeer__OnMessage { return; }` | 会让人以为「空体」（碰巧对） |
   | `CemeteryManager__DisplayCardTraits.c` | 同上 | 同上 |
   | `ClockManager__SetFillPercentage.c` | 同上 | 同上 |
   | `CardScript__ShowAbilityAnim.c` / `__ShowAbilityIcon.c` | 同上 | 同上 |
   | `ManaBuff__DeepCopy.c` | `System_Delegate.Clone` | 会让人以为「它不是 MemberwiseClone」 |
   | `EntityScript__UsesPsychicEnergy.c` | `SpriteAtlasReference__ValidateAsset { return 0; }` | 会让人以为「灵能货币恒 false」（碰巧对） |
   | `CampaignWindowTab__ToggleChooseArmyText.c` | `DeckManager__ToggleCardbackPreview` | **会凭空造出一个调用方**（本批差点把这个当「ToggleCardbackPreview 是活的」） |

   ⇒ **建议**：`// RVA:` 与 cpp2il 交叉成为默认动作；尤其「`.c` 里只有一句 return / 一个常量」这种形状，先怀疑内容不符。

2. **`RVA 0x4B33B0` 是结案利器，本批第一次大规模用**：7 条「空体」全部靠 `dump.cs` 里 `// RVA: 0x4B33B0` 钉死（PlayAbility / DisplayCardTraits / SetFillPercentage / ShowAbilityAnim / ShowAbilityIcon 等）。它比读 `.c` 快且不会被内容不符骗。建议写进准则：「判空体先看 RVA 是不是 0x4B33B0，是就不用再读体」。

3. **反向陷阱：`PrebuiltDeck.RefreshList()` 与 `PrebuiltPack.RefreshList()` 是两个类的同名方法**（cpp2il `PrebuiltDeck.cs:394` / `PrebuiltPack.cs:175`）。反查「调用点」时按裸名 grep 会串类。

4. **「84 个 bundle 里 0 实例」这条实测的对照失败**（**上一路结论仍对、理由要换**）：
   `bundle_Waprforge_monoscripts/MonoScript/` 确为 **722 份**、确实**没有** `SegmentationController`；但对照组的 `PlayerDataManager`（`dump.cs` 明确 `: MonoBehaviour`）、`MainTutorialManager` / `PurchasesManager`（都是 `: SingletonBehaviour<T>`）**同样不在那 722 份里** —— 而它们必然有实例。
   ⇒ 那 722 份**不是「所有被挂载过的脚本」的完整普查**，**负面结论不可独用**。旁证：`globalgamemanagers/MonoScript/` 有 4,453 份（含 `SegmentationController` 本体，pathID 4356），说明「脚本资产存在」与「有没有实例」是两件事，而后者只有 PPtr 级扫描才能答。
   （另：`m_Script` 的 `m_PathID` 是 64 位哈希，例 `bundle_menus_assets_all/.../MonoBehaviour_1002020118586957703.json` → `m_PathID: 1015376240363272691`；按 `MonoScript_4356` 那种小号去 grep 会**满屏假命中** —— 我实测 `"m_PathID": 4356` 在 20+ 个文件里出现，全是无关对象。）

5. **`CardArt.Get` 的 `_cache` 只增不减是**全工程唯一**图缓存**（`Core/CardArt.cs:1060-1067`）—— C 组那 4 条（201/221/304/收藏:203）的等价释放只要改这一处，不必逐图打补丁。附带收益：`CardArt.Describe()` 已经会数「已加载 N 张」，可以直接当自检观测量。
