# B8_V · 以「死代码 / 0 调用者」为由判 D 的行 —— 逐条复判

> 代理：判据代理 V（代号 B8_V）· 2026-10-26
> 产物：`判据/B8_死代码D复判.tsv`（**163 行** = P0 四域全量 132 + 非 P0 已核 31）· 本报告
> ⛔ 未改任何 `.cs` / 账主表 / `账/_parts/**` / 正本 / 别人的报告。

---

## ① 结论（先说数字）

| 项 | 数 |
|---|---|
| 全账 `D` 行 | **3,524**（21 个域） |
| 差异/备注里出现「死代码 / 0 调用 / 无消费者 / 不可达 / 全库无 …」的 D 行 | **360**（另有 26 行是更宽的词命中，共 386） |
| 其中**只靠**「死代码 / 0 调用」一条理由（无零实例、无数据 0 入口、无服务端、无等价物） | **169** |
| P0 四域的 D 行总数 | **513**（战斗界面_HUD 235 · 战斗逻辑驱动 172 · 卡组编辑 56 · 卡牌面_渲染 50） |
| **本次逐条复判**（P0 四域 · 132 行 · 占 P0 D 的 25.7%） | **132** |
| **建议改判** | **A 42 · C 4 · B 7 · 保持 D 79** |

**核心结论：调度台那句判断成立，而且比预想的更普遍。**
「死代码」这条理由在 P0 四域的 132 行里，**有 53 行（40%）不该据此判 D** —— 42 行应记 A（函数级等价物已存在 / 原版无可观察量），4 行应记 C（有真工作量、但铁律 11 只有先后），7 行应记 B（备注自己就写着「降 B」，状态列却还是 D）。
剩下 79 行**仍判 D**，但其中 **12 行**的**理由必须重写** —— 它们真实的依据是「原版 0 实例 / 出货数据 0 入口 / 原版服务端」，不是「死代码」：
`UpdateBastionr`(1) · `CursorManager`×3 · `SetEnemySecret`(1) · `ToggleCardState`(1) · 陷阱族 `SetAsActiveTrap`/`TrapResolved`/`TriggerOtherCardLanding`/`OnTrapDrawn`(4) · `AddDrawTrapAction`(1) · `HasSingleUseMutation`(1)。
（另 `UISafeAreaManager`×4 的措辞上一轮 E6 已就地订正过，本批复核同意。）

**账里同时存在两个相反结论的确切证据（29 条）**：备注写「保持 B / 降 B / A→B」，状态列却是 `D`。

---

## ② 按域交叉表（P0 四域，逐条见 TSV）

| 域 | 命中本主题的 D 行 | 建议 A | 建议 B | 建议 C | 保持 D | 备注 |
|---|---|---|---|---|---|---|
| 战斗逻辑驱动 | 40 | 24 | 0 | 3 | 13 | 13 个 D = 原版 LogError 无产出点 3（daemonHost 族）+ 出货数据 0 入口 5（Trap / Mutation / trigger 450）+ 远端 liveops 2（FTUE bot 配置）+ 原版 0 实例 3（YourTurnUI） |
| 战斗界面_HUD | 51 | 10 | 0 | 0 | 41 | 41 个 D = 类 0 实例 35（CemeteryLog 9 / TipPrebattle 10 / ResolutionManager 4 / UISafeAreaManager 4 / CursorManager 3 / UISizeManager 2 / BattleDeviceCompatibility 2 / RadioMessageScript 1）+ 出货数据 0 入口 2（SetEnemySecret / AddDrawTrapAction）+ null 字段闸死 2（UpdateBastionr / ToggleCardState）+ 服务端 2 |
| 卡组编辑 | 9 | 7 | 0 | 1 | 1 | **唯一一条「0 调用点」被证伪的 P0 行在这里**（`:290`） |
| 卡牌面_渲染 | 32 | 1 | 7 | 0 | 24 | 7 条是状态/备注冲突；23 条是零实例族（已逐类核实） |
| **合计** | **132** | **42** | **7** | **4** | **79** | |

**判据分族（这才是复习的复利）**

| 证据族 | 性质 | 是否落在 D 的三类里 | 本批处理 |
|---|---|---|---|
| **A. 原版 0 实例（死类）** | 本类**独有序列化字段**在 assets_full 全量 0 命中 | ✅ 「原版没有」 | 保持 D（**58 行**） |
| **A′. 有实例但零行为** | 14 个场景挂着，`Awake(){enabled=false}` 且无 Start/OnEnable/Update | ✅ 「原版没有（可观察）」 | 保持 D（**4 行** · UISafeAreaManager） |
| **B. 原版有体但永不可达（AddEffect 对 buffType 35 只 LogError）** | 方法有体、产出点自己就报错 | ✅ 「原版没有」 | 保持 D（**3 行**） |
| **C. 出货数据 0 入口（Trap / SecretOrder / Mutation / trigger 450 …）** | 代码活、数据 0 张 | ⚠️ 严格说**不是**三类之一 | 保持 D（**7 行**）+**理由必须改写**成「原版出货数据 0 入口 + 时间限定」（见 ④-A 政策点） |
| **C′. null 字段闸死** | 方法活、体内 `x != null` 恒 false（字段在出货包里是 null） | ✅ 「原版没有（可观察）」 | 保持 D（**2 行** · UpdateBastionr / ToggleCardState） |
| **D. 原版服务端 / 远端 liveops** | PlayFab / UnityServices / AIBotsConfig | ✅ 「原版服务端」 | 保持 D（**4 行**） |
| **E. 用户拍板边界** | 单机全解锁 ⇒ 持有数恒 ≥1 | ✅ 「用户拍板不做」 | 保持 D（**1 行** · TryRemoveCardsNotOwned） |
| **F. 真死代码（0 调用点 + 0 实例 + 0 字符串挂接）** | 无行为可对齐 | ❌ 不在三类里 | 改 **A**（42 行里绝大多数）或 **C**（有工作量） |
| **G. 机制已替代（方法活着，我们换了结构）** | 有活调用点 | ❌ 不在三类里 | 改 **A**（extrinsic 五族 + tooltip 七条 → 后者按备注改 **B**） |
| **H. 域外占位** | 备注就写「域外」，差异却写「死代码」 | ❌ 不是理由 | 清掉占位（2 行） |

---

## ③ 最值钱的发现

### ③-1 🔴 P0 里有一条「0 调用点」是**假的**（唯一一条）

`卡组编辑:290 PrebuiltPack.IsValid()`

- 原 D 的唯一理由：**「全反编译零调用点 ⇒ 死代码」**。
- 实际：`cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/DraftEventBase.cs:378`
  ```csharp
  internal bool _003CGetRandomPack_003Eb__0(PrebuiltPack pack)
  {
      ...
      if (pack.packArmy == rawCardScript.cardArmy) return pack.IsValid();   // ← 活调用点
  }
  ```
  `pack` 的类型就是 `PrebuiltPack`（形参签名写死）。⇒ **轮抽（DraftEventBase）域在用它**。
- 建议：**C**（约 10 行：`GetCardLibrary.Count == cardIds.Count && 无 IsHidden`，接轮抽取包那条）。
- 为什么重要：这不是措辞问题 —— 它推翻的是「这条 D 站得住」本身。而且它在**轮抽域**，是 P1 但必须做的线。
- 同族提示：这行备注还同时挂着「**域外**」占位（`D 理由 = 域外`），两个不合格理由叠在一行上。

### ③-2 🔴 六处「死代码」措辞把**有活调用点**的方法说成死代码（结论不变、理由要改）

| 账行 | 原措辞 | 实测 |
|---|---|---|
| 战斗界面_HUD:168 `BattleCardUI.UpdateBastionr` | 「原版死代码」 | **活调用点 `CardScript.cs:20629`**。死的是显示条件：`bastionText != null` 恒 false（本批实读 `MonoBehaviour_122102074273340352.json`：`bastionText.m_PathID = 0`）⇒ 原版自己也不显示堡垒数 |
| 战斗界面_HUD:140 `BattleCardUI.SetEnemySecret` | 「原版断链」 | **活调用点 2 处**（`BattleCardManager.cs:85` / `CardScript.cs:6650-6652`）。不可达的是数据：`spellType Trap(70)/SecretOrder(60)` 各 0 张 |
| 战斗逻辑驱动:1004/1070/1078/1428（陷阱族） | 部分写「死代码」 | **全部有活调用点**（`BattleCardManager.cs:89` / `BattleManager.cs:27869,27884` / `BattleManagerSupport.cs:1025,1035,1045` / `CardScript.cs:12074`），被 `armedTrap` 闸着 + 池内 0 张陷阱卡 |

⇒ **这 6 行判 D 的结论我认同**，但依据要从「死代码」换成「原版出货数据 0 入口（时间限定）」。

### ③-3 🔴 `CursorManager` 的 D 站得住，但账里写的**理由是错的**

- 原账：「`Cursor.visible=false` 那句的闸是 `SteamUtils.IsSteamRunningOnSteamDeck() && touchInput`（只有 Steam Deck + 触屏才藏）」。
- 实测（**两份反编译一致**）：`CursorManager.Start` 第一句就是**无条件** `Cursor.visible = false;` ——
  `decomp_full/CursorManager__Start.c`：`UnityEngine_Cursor__set_visible(0,0);`（函数最开头，无分支）；
  `out_cpp2il_dev_ilspy/CursorManager.cs:19`：`Cursor.visible = false;`。**没有任何 SteamUtils 闸**。
- 真正的依据（本批新取证，比原依据硬）：`assets_full/level0/GameObject/CursorManager.json` → **`"m_IsActive": false`** ⇒ 这个 GameObject 是关的，`Start/Update/OnGUI` 一个都不跑。
- 附带更正：原账「`cursorTextureNormal` 全 null + 全库 grep 0 命中」**不能**证明贴图为 null —— `level0/MonoBehaviour/` 只有 77 个文件、**该组件的体根本没被 dump**（`level0` 无 type tree）。所以「0 命中」这半句是无效证据。
- ⇒ **结论 D 不变**（PC 上就是系统光标，与我们一致），但**理由必须整条重写**。

### ③-4 🔴 29 行「状态列 = D，备注自己说 B」——账在自我矛盾

用脚本全账扫（正则 `保持 B / 降 B / 改 B / A→B / C→B / D→B / 归B / 退回 B / 记 B`）命中 **29 行**：

| 域 | 行 | 备注自己说的话 |
|---|---|---|
| 卡牌面_渲染 | 124,126,127,129,130,131,134（7 行） | 「2026-10-25（E）**A→B**：落点修不动，按调度台口径**降 B**」 |
| 收藏 | 32,105,108,203,205,207,263,270,275,277,279,283,285,288,294,342,343（17 行） | 「**保持 B**」（另含「等调度台裁」一条） |
| 教程_引导 | 115,117,118（3 行） | 「代理G **B→A** … **B 收尾**：判据不可及」 |
| 卡组编辑 | 183,230（2 行） | G6 第 5 轮把落点写成「无独立实现行可指」 |

⇒ 建议：这 29 行的**状态列**按备注落成 B（`收藏:203` 那条要调度台二选一：B 还是「D（机制已替代）」）。这是「同一套理由用在两个相反结论上」的直接物证。

### ③-5 ✅ 「零实例」这一族整体是**最硬**的（我做了正反双向对照）

零实例族（**58 行**：CemeteryLog×9 / TipPrebattle×10 / Trait 系×24 / ResolutionManager×4 / CursorManager×3 / UISizeManager×2 / BattleDeviceCompatibilityController×2 / YourTurnUI×3 / RadioMessageScript×1 —— 归口：战斗界面_HUD 31 + 卡牌面_渲染 24 + 战斗逻辑驱动 3）的判据是「类独有序列化字段名在 assets_full 全量 0 命中」。**这个搜法本批做了对照验证**：

- **正对照**（能命中才说明索引有效）：`m_safeZones` → **14** 文件（13 战场+主菜单，与账里写的一致）；`bastionText` → 1；`showCardTextButton` → **14**（13 个 0 + mainmenu 1 个 2514）。
- **反对照**（0 命中才说明真没有）：`targetCard1` / `centralCemetery` / `cemeteryCardImages` / `upperCemetery` / `openBgWidth` / `timeToOpenBg` / `abilitySlots` / `redBoxSprite` / `titleDesc` / `traitImage` / `contentOffset` / `spiritStoneIcon` / `timeToFadeStep` / `timeToWaitStep` / `lastScreenWidth` / `sizeChange` / `cursorTextureNormal` / `mobileSensorSizeX` / `mobileHandAreaSize` / `forceMobileSize` / `textDelay` / `moveTime` / `cemeteryShade` → **全部 0**。

⇒ 这一族可以放心判 D（「原版没有」），**不必再逐行重查**。

### ③-6 ✅ 本批同时给「0 调用点」的**方法学**补了一条硬教训

`战斗界面_HUD:339 CemeteryManager.AddDrawTrapAction` —— **只在 ILSpy 里 grep 会得出「0 调用点」的错误结论**：

- ILSpy dev 全树：只有定义处（`CemeteryManager.cs:802`）；
- `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c`：**有调用点**（包在 `if (armedTrap != 0)` 里）。
- 根因：**Cpp2IL 静默丢了 `_ResolveDrawCard_d__432` 这个 async 状态机的方法体**（ILSpy 里连该类都不存在）。

⇒ 「0 调用点」这个结论**必须三源合判**：① `decomp_full`（.c，29% 覆盖但含状态机）② `out_cpp2il_dev_ilspy`（真体、但会丢状态机）③ `assets_full`（UnityEvent `m_MethodName` / prefab / SO）。缺任何一源都可能得出相反的结论。

### ③-7 非 P0 里也确认了 2 条真·假「0 调用点」（顺手带出来的）

| 账行 | 原话 | 实测 |
|---|---|---|
| 菜单_外壳:612 `ScrollViewFocusFunctions.FocusOnItemCoroutine` | 「判死：同上（0 调用点）」 | **有 2 处活调用点**：`ForgeRewardSelector.cs:416` 与 `:438`（`IEnumerator routine = ScrollViewFocusFunctions.FocusOnItemCoroutine(rewardsScrollRect, target, 1.5f, onFinish);`）⇒ 锻造奖励选取的滚动定位。建议 **C** |
| 音频_语音:74 `SoundAsset_OLD.GetDuration` | 「遗留死代码」 | **有活调用点**：`AiScripted.cs:3566` `scriptedActionCampaignData.GetSoundAsset(...)?.GetDuration()`；且 `AiScripted.cs:1926` 明确写着 `SoundAsset_OLD soundAsset = ...GetSoundAsset(...)` ⇒ 返回类型就是它。体只有 `clip != null ? clip.length : 0f`。建议 **A**（我们本来就有 clip 长度）或 C（1 行） |

---

## ④ 没查清的部分（按「查不到就说查不到」）

### ④-A ⚠️ 政策点，需调度台拍一句（不是我能裁的）

「**原版有实现、但当前 build 出货数据 0 入口**」（Trap / SecretOrder / Mutation / trigger 450 / FTUE bot 配置 …）这一族在 **P0 内 7 行**（`HasSingleUseMutation` · `SetAsActiveTrap` · `TrapResolved` · `TriggerOtherCardLanding` · `OnTrapDrawn` · `SetEnemySecret` · `AddDrawTrapAction`），非 P0 未逐条核（估计另有数十行）。按 `方案 §4.1` 是「D + 时间限定」，但按调度台 2026-10-26 的新裁定（D 只留三类）它**不严格属于任何一类**。
本批处理：**建议保持 D，但把理由从「死代码」改写成「原版出货数据 0 入口（时间限定：当前 build / 2025-07-30 资源快照；数据出现须重跑）」**。若调度台要求严格三分，则这 7 行应改 C。**请裁一句**，之后统一改。

### ④-B 非 P0 的 228 行：只做了**机械扫描**，没有逐条人工判

- 我跑了全 360 行的机械复核：对每行的（原版类, 方法名）在 ILSpy dev 全树里找**非声明出现**。结果 **222 行有同名出现**，但绝大多数是**别的类的同名方法**（`Toggle` / `Start` / `OnEnable` / `IsValid` / `ContainsKey` / `OpenWindow` / `CreateInstance` / `ChangeState` / `SetSortingLayer` / `GetDuration` / `ToStringFull` …）—— 用方法名做跨类判定**天然不可靠**。
- 我对其中**名字足够独特（≥10 字符且非通用词）**的 **~40 条**逐条看了接收者类型，从中确认了上表 ③-7 两条真问题 + 排除了 8 条假阳性（`CemeteryLogManager.AddCardToCemetery`、`ManaBuff.GetNewInstanceOfThisEffect`、`MilestoneScoreBar.SetScoringMilestones`、`GameModesIconCollection.ReleaseAll`、`UITextGradient.ModifyMesh`、`WhereToGetCardsTester.OpenWindow`、`MultiKeyDictionary.ContainsKey`、`ReflectionExtensions.CreateInstance`）。
- **没做的**：非 P0 的 129 行「只靠死代码」的行（分布在 特效动画 36 · 菜单_外壳 31 · 未分类_其他 21 · 收藏 19 · 战役 8 · 社交 6 · 日常 3 · 音频 2 · 联机 2 · 轮抽 1 · 其他 1）**没有逐条人工复核**，等下一批。
- **TSV 里已额外收进 31 行非 P0**（都标了 `【非 P0 · 只复核了「0 调用点 / 接收者类型 / 状态-备注一致性」这三件】`）：菜单_外壳 1（`FocusOnItemCoroutine` → C）· 音频 1（`SoundAsset_OLD.GetDuration` → A）· 未分类_其他 7（3 条 Addressable 释放 → A；`RemoveWhere` 保持 D + 改写理由；3 条假阳性已排除）· 收藏 17（8 条 → B，其中 `:203` 是要调度台二选一；9 条 `ItemDrawer.ItemDrawer.*` 保持 D 但**账务要并入泛型行**）· 教程_引导 3（→ B）· 卡组编辑 2（`:183 :230` → B）。
  ⇒ 这 31 行可直接随 P0 那 132 行一起被脚本消费。
- 可复跑：本批的机械扫描脚本逻辑（ILSpy 全树 → 非声明出现 → 按「独特性」过滤）是纯文本的，重跑成本约 40 秒；落点见本报告 §③-6 的方法学。

### ④-C VA 级反汇编**没有重跑**

「0 调用点」我采用的是「两套文本反编译（decomp_full + ILSpy dev）+ assets_full 字符串」三条，**没有**对每条重跑 E8/E9 rel32 的二进制直调扫描（那要逐方法的 VA，3500 条不现实）。账里已有的 VA 结果我采信为旁证。⇒ **对独特方法名置信度高；对通用方法名置信度低**（已如上标注）。`IsValidActiveAbilityTarget` 那个「0 直调但属接口实现」的反例提醒：**接口/委托/反射调用不被任何文本 grep 覆盖**。

### ④-D 「724 条无方法体」的残余风险仍在

`decomp_full` 覆盖 25,096 / ~85,440 非平凡签名；ILSpy dev 补了大部分，但**已证明会丢 async 状态机体**（③-6）。我没法穷举「这 132 行里有没有哪一条的调用者恰好在被丢的体里」。⇒ 对每一行的复核结果，**建议都带「时间限定 / 本 build」**，与账里已有的写法保持一致。

### ④-E 一处「同族口径不一致」我压不住，留给调度台

`战斗界面_HUD:675 UnityServicesManager.InitializeUnityServices`：**备注写**「G2 ⑤：不在 Q4 a 档 ⇒ **保持 C**，等用户裁定（⛔ 别顺手判 D）」，**状态列是 D**。按 2026-10-26 新口径（P3 由调度台自行判、要写全证据），它属于「原版服务端」⇒ D 成立；但**备注那句「等用户裁定」是旧口径残留**，两句话必须删掉一句。

---

## ⑤ 顺手发现（在现场看到的、不属于本任务但该记的）

1. **`GameBlocked` 是假阳性**：13 个战场场景都含这个字样，逐条读后是本地化键 **`GameBlockedDuplicateConnection`**（`CustomErrors/DuplicateConnection`），不是方法名。
   ⇒ 以后「按方法名扫 prefab/SO」**必须点词边界**，否则本地化键表会成片假阳性（这次差一点就把 `BattleErrorUIManager.GameBlocked` 判成「有挂接」）。
2. **`未分类_其他.tsv` 有一批行 `差异` 列是空的**（`:201 :221 :304 :432 :394 :713 :747` 等），理由只写在 `备注` 的「断点①/②/④」里。
   ⇒ 任何**按 `差异` 列 grep** 的审计都会整批漏掉它们。这 7 条我也顺手判了：
   - `:201 CosmeticItem.UnloadReferences` / `:221 CursorByArmySO.ReleaseArmyCursor` / `:304 FlavourTextSO.ReleaseFlavorBackground` —— **都有活调用点**（`DeckGeneralInfoDemo.cs:76` / `ArmyUtilities.cs:235` / `CardDisplayWindow.cs:402`），D 的真实理由是「断点①：Addressable 引用计数释放，我们图本地导入、无引用计数可放」⇒ 建议 **A**（行为等价）。
   - `:394 ListExtensions.RemoveWhere` —— 唯一入口 `DeckEnergyCostDrawer.cs:90`；理由「断点②：单机全解锁 ⇒ 卡组里不可能有失效卡」⇒ 与 `卡组编辑:244` 同一类，属**用户边界②（全解锁）** ⇒ 保持 D + 理由改写。
   - `:432 MilestoneScoreBar.SetScoringMilestones` / `:713 UITextGradient.ModifyMesh` / `:747 WhereToGetCardsTester.OpenWindow` —— 我逐个排除了假阳性（接收者分别是 `AllianceScoreBar` / `UIBorderGlow` 自己的 `ModifyMesh` / 76 处别的 `OpenWindow`），账里原判定成立。
3. **`收藏:203 GameModesIconCollection.Release()`** 的备注**明确向调度台提了一个二选一**：「① 建句柄层（成本高、收益低）② 写成域口径『按名取图、无句柄』并改记 D（机制已替代）⇒ **等调度台裁**」—— 这条挂了一直没裁。
4. **`收藏:263…294` 十行 `ItemDrawer.ItemDrawer.*`**（抽象声明、无体）备注反复要求「**并入 `<T>` 那条**」，但状态仍是 D 而不是合并掉。
   ⇒ 与本次 F 类（E 族的「死代码」）同源：**抽象声明本身不产生行为**，合并进泛型那条才是干净做法。
5. **`.tsv` 里 `差异`/`备注` 的「术语漂移」值得统一**：同一件事在账里有 「死代码 / 断链 / 死类 / 零实例 / 0 调用点 / 无消费者 / 不可达 / 域外 / 机制已替代 / 断点①」十种写法。
   ⇒ 建议在 `方案 §四` 加一张「D 的**依据词表**」（我上面 §② 的 A–G 七族可直接用），以后判 D 只许用这七个词，**审计才能机器化**。

---

## 附：本次复核用到的方法（可复跑）

```
① 筛：python 工具/ledger_slice.py --status D --limit 0  →  按关键词分桶
② 代码两条：grep -rn "<方法名>" D:/2/tools/decomp_full/            (25096 个 .c)
            grep -rn "<方法名>" D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/   (2404 个 .cs)
③ 资源一条：rg -l -F -f <96 个方法名>.txt D:/2/新解包资源/assets_full    (24.07 万 JSON / 4.4 GB / 5.6 秒)
            正对照 m_MethodName → 860 文件（证明索引有效）
④ 实例一条：按「类独有序列化字段名」搜 assets_full；正对照 m_safeZones=14
⑤ 状态-备注一致性：正则扫全 3524 行 D 的备注列（保持B/降B/A→B/…）→ 29 条冲突
```
