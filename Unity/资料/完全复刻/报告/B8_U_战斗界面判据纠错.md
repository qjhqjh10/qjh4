# B8_U · 战斗界面/HUD 判据纠错 + 15 条 C 行处置

> 执行代理 `B8_U` · 2026-10-26
> 白名单：`Assets/CardPresentation/Battle/**`（除 `BattleDriver.cs`、`SettingsPanel.cs` 正被改段）· 账补丁 `账/_parts/B8_U.tsv` · 本报告
> 纪律：⛔ 未改主表 · ⛔ 未碰 git · ⛔ 未跑 Unity 批处理 · typecheck **0 / 0**（跑 3 次，前两次的错误全在别人的在飞文件）

---

## ① 结论

### 1·1 七处判据出处/语义错误 → 各落成什么

| # | 账原写 | 实读（本轮现核） | 落成 |
|---|---|---|---|
| 1 | `BasicCardUI.cs:583-586`（`SetEmptyRarityStars`） | **`:514-517`**（`:583-586` = `OnDescriptionShow`，另一个方法） | 账改；另补 `CardRarityHolderController.cs:13-52`（`clamp(rarity-1,0,len-1)`，`maxRarity` 不匹配时 `LogError` 后仍取 `sprites[0]` —— **两支都落 sprite[0]**） |
| 2 | `SetupActiveSkill` 的闸在 `:170` | 闸在 **`:160`**；**并且** `:170` 只是局部变量 `string targetAvailableText;` 的声明，J 说的「else 支 = 描述原样」在 **`:193`** | 账改 + **代码注释同步订正**（`SkillPanel.cs:2083`，零行数变化） |
| 3 | `ManaTypeHolder.SetSavedMana :186-260` | 体是 **`:186-246`**（`:248` 起 = `ToggleManaAccumulation`） | 账改**两行**（`ManaManager.SetManaSaved` + `ManaTypeHolder.SetSavedMana`）；⚠️ 同一处错行号还写在 `BattleDriver.cs:32630` 的注释里 ⇒ **改动请求**（白名单外） |
| 4 | `ObjAnimEffects` 七行整体串一格 | 串的是 **八行**（`Start` 也中）：方法名↔体错位见下表 | 账 8 行**全部重写**判据出处/要点/落点/差异/备注 |
| 5 | 未标 | `ResolvePlaySound`(:313) / `ResolveScreenShake`(:338) = **原版死代码**（`private` + 全类 **0 调用者**，`PlayEffect` 把两段**内联**在 :184-206 / :207-214） | 账两行判 **D**（铁律 11 例外①：原版本身不调它），并写死「行为核销到 `PlayEffect` 那一行，⛔ 别另建两个永不跑的方法」 |
| 6 | `特效动画.tsv:196` 建议 `B→C` | **J 的建议不成立**：上限**早已实现**且 = **5**（13 场序列化实测 `cardEffectSlots` 数组长恒 5；`CardWinBox.EffSlots = 5` + `Mathf.Min` 截断 + 超限出声，`A985⑨` 就在钉它） | 账判 **A**（附 4 条观测量/灭自证线索） |
| 7 | （提醒）`<i>` vs 纯文本 | 手牌侧 = `desc + "<i>" + 后缀 + "</i>"`（**:1342 无空格**，desc 取 `GetLocalizedCardDescWithLineBreak`）；技能面板 = `desc + " " + 后缀`（纯文本） | **不抽共用函数**已写进 `SkillPanel.cs` 的注释（就地改，零行数变化）+ 账 HUD:147 备注 |

**`ObjAnimEffects` 的正确映射（八行）**

| 行（账物理行 289–296） | 方法 | 真体 | 本轮状态 |
|---|---|---|---|
| 289 | `Start()` :94-135 | `if (waitToPlayEffects) return;` + 遍历 `animEffects` 分流（= 「自动播」入口） | **D → C**（原 D 的依据是别的方法，见下） |
| 290 | `PlayDelayedEffect` :222-235 | **只是延迟协程**：`WaitForSeconds(animDelay)` → `PlayEffect`（状态机 :13-86） | C |
| 291 | `PlayEffect` :175-220 | **按 `effectType` 分派**（20→`ResolveAnimPrefab`；30/40→**内联**；10→`ResolveFade`；0→什么都不做） | C |
| 292 | `PlayEffects` :137-173 | 遍历 `animEffects`：`animDelay != 0 && effectType != Fade` ⇒ 起协程，否则立即 `PlayEffect`；**无 `waitToPlayEffects` 闸** | C |
| 293 | `ResolveAnimPrefab` :289-311 | `!_disableParticles && animPrefab != null` ⇒ `Instantiate` 挂到自己 transform；空 ⇒ `LogWarning("Undefinded anim prefab")` | C |
| 294 | `ResolveFade` :237-287 | `spritesToFade`/`textsToFade` 逐项 `SetAlpha(start)` → `DOFade(end, animDuration).SetDelay(animDelay)` | C |
| 295 | `ResolvePlaySound` :313-336 | `PlayOneShot(clip, SoundFxVolume*volume)`；无 source ⇒ LogWarning | **C → D**（死代码） |
| 296 | `ResolveScreenShake` :338-345 | `FindObjectsOfTypeAll<CameraShake>()[0].DoScreenShake(shakeInfo)` | **C → D**（死代码；且 13 场 `CameraShake` 0 实例 ⇒ 内联那段也空转） |

> 原账把 `PlayEffect`/`PlayEffects`/`ResolveFade`/`ResolveAnimPrefab` 的体互相错开一格，且「点亮的落点」也一起错（`ResolveFade` 的落点写的是 `WarpforgeEffectPlayer` 的 prefab 入口）⇒ **照原账实现会写错 4 个方法**。

### 1·2 十五条 C 行的去向

| 账物理行 | 方法 | 去向 | 卡在哪 |
|---|---|---|---|
| **57** | `BasicCardUI.SetEmptyRarityStars` | **A** | —— （`F6` 的 `rarity=null` + `B8_N` 的 `SetStatsHidden` + 本代理 `ShowBattleCard` 那句调用） |
| **306** | `CardDisplayWindow.ShowBattleCard` | **A** | —— （伏击支 ①~⑧ 全落，① = 本轮接上） |
| 19 | `ActiveSkillDisplay.SetupActiveSkill` | C | ① `EffectSpec` 无 `maxTargets`/`targetsAffected`（引擎线）② `BattleDriver.ShowSkillPanel` 未喂（F5） |
| 92 | `BattleCardManager.CreateCard` | C | `DeckRules.PlayerDeck` 无「这一份是异画副本」那一格（数据/引擎线） |
| 100 | `BattleCardUI.BattleCardSetup` | C | `CardArt` 无 `DeckCardbackSdf`（E7）+ `DeckRuntime.cardbackId`（数据侧） |
| 107 | `BattleCardUI.ChooseDissolveMaterial` | C | `Core/CardView.cs`（E7）—— 两份材质资产都在工程里 |
| 147 | `BattleCardUI.ShowAvailableTargets` | C | `Core/CardView` 描述写口 · `RuleCore.PlayTargetPool` · `Hand/CardInteraction` 三处都不在白名单 |
| 152 | `BattleCardUI.ShowOriginalRemnantCard` | C | `Core/CardView.cs:2496-2522 / :2632`（E7） |
| **181** | `BattleErrorUIManager.OpponentDesyncCancelMatch` | **撤出补丁** | **`_parts/B8_L.tsv` 已判 A 并落地**（`Net/NetBattle.cs:1815 LeaveWithoutResult` + `Close(say:false)`）⇒ 本代理那份 C 是过期的；**若合并会把 A 盖回 C**（`check_patch_conflicts` 当时报的正是这一条） |
| 188 | `BattleHud.BroadcastChangedLanguage` | C | `Core/CardView` 的「卡面重排字」（E7）；日志半边已接 |
| 305 | `CardDisplayWindow.SetupTutorialObj` | C | **判据已由本代理补齐**（见 §2·5）：剩 = 教程线接线 + 我们窗里建那四条 TMP |
| 363 | `ChooseCardMenu.AutoProcessDone` | C | `BattleDriver.cs:1123` 回放分支（E5） |
| 377 | `ChooseCardMenu.Setup` | C | 同上（面板本体三件已在 `CardChoicePanel` 里） |
| 536 | `ManaManager.SetManaSaved` | C | 引擎没有 `savedMana` 这一格（`PlayerState.ManaCarry` 在 BeginTurn 立刻兑现清零） |
| 547 | `ManaTypeHolder.SetSavedMana` | C | 同上 + 两个 cue（`manaAccumulated`/`manaAccumulatedUsed`）全仓 0 命中 + `UpdateHud` 每帧覆写 |

**合计：A 2 · C 13（其中 1 条因 `B8_L` 已 A 而撤出）**；另有 `特效动画` 域 **A 1（CardEffectsService）· D 2（两个死代码）· C 6**。

---

## ② 逐条证据

### 2·1 `SetEmptyRarityStars`（账物理行 57）
- `cpp2il/out_cpp2il_dev_ilspy/BasicCardUI.cs:508-517`：`SetCardRarityStars()` = `SetCardRarity(rawCardData.cardRarity, maxCardRarity)`；**`SetEmptyRarityStars()` 在 `:514-517`** = `SetCardRarity(0, maxCardRarity)`。
- `CardRarityHolderController.cs:13-52`：`maxRarity == rarityLevelSprites.Length` ⇒ 索引 `clamp(rarity-1,0,len-1)`；不匹配 ⇒ `LogError("[ERROR] Max rarity doesn't match the number of rarity sprites")` 后取 `array[0]`。⇒ 两支都 = `sprites[0]` = 最低档宝石。
- 我们：`CardDisplayWindow.cs:1877` `masked.rarity = null` ⇒ `CardView.RarityGem`（`Core/CardView.cs:11834`）落 common 那张；卡框走 `CardTierFor`（恒 ≥ 1）不受影响。
- **G0–G8**：G0 ✓（🥈 + 方法体在位）· G1 ✓（落点行号范围，逐句读过）· G2 ✓（**机制差别**：原版「索引夹到 0」，我们「未知档 ⇒ common」，落到同一张贴图）· G3 ✓（宝石贴图 == `RarityGem(null)`）· G4/G5 见下 · G6 ✓ · G7 typecheck 0/0 · G8 本节。
- ⚠️ **G4/G5 未跑宿主**：本批不跑 Unity ⇒ 观测量读口备齐但**断言还没写也没跑**（见 §⑥）。

### 2·2 `SetupActiveSkill` 的闸（账物理行 19）
`ActiveSkillDisplay.cs:157-196` 逐行：`:160` `if (activeAbility.targetsAvailableCounter != null && targetsAvailableCounter.targetsAffected != TargetsAffected.any)`；`:172` `if (maxTargets == 1 && targets._size > 0)`；`:178` `GetTargetAvailableText(第一个目标名)` / `:185` `GetTargetsAvailableText(count)`；`:188` `text7 = text6 + " " + targetAvailableText`；`:196` `textMeshProUGUI2.text = text7`；else 支 `:191-195`（`:193` = `GetCurrentLocalizedAbilityDescription()`）。`:170` 只是 `string targetAvailableText;` 声明 —— **不是判据**。

### 2·3 `ManaTypeHolder.SetSavedMana` 体尾
`cpp2il/.../ManaTypeHolder.cs`：`:186` 方法头 → 六步 → `:245` `ToggleManaAccumulation(option, onComplete);` → `:246` `}`；`:248` 已是 `private unsafe void ToggleManaAccumulation`。

### 2·4 `ObjAnimEffects` 八行
见 §1·1 的表。**额外一条**（本轮现读）：`Start()`（:94-135）的**真体**= `if (waitToPlayEffects) return;` + 与 `PlayEffects` 同一套遍历；而原账给它的「要点」与「D 的依据」**都是 `ResolveScreenShake` 的内容**（`CameraShake` 0 实例 ⇒ 空转）⇒ D 的依据不成立 ⇒ 改 **C**，并把「9 处实例的 `waitToPlayEffects`/`animEffects` 序列化值未读」写进备注。死代码的两条：`ResolvePlaySound`/`ResolveScreenShake` 都是 `private`，本类 grep 调用点 **0**（只有方法定义行）⇒ 判据硬。

### 2·5 `SetupTutorialObj`（账物理行 305）—— 把「判据未取」补上
- 🥇 `decomp_full/CardDisplayWindow__SetupTutorialObj.c:26-72`：`BattleManager.Instance == null` ⇒ 强制 forceHide；`LiveOpsManager.GetHandler<GameModes>().GetActiveEvent<TutorialEvent>()` 非空且 `event+0x20` 非空 ⇒ 取那个 Dictionary 里某 key 的计数；闸 = `!forceHide && enableTutorial(+0xe0) && 计数 < 6`。
- 开之后：`tutorialObj.SetActive(true)` → `tutorialObjUnit.SetActive(rawCardData.IsUnit())` → `tutorialObjCost.SetActive(cardType != Hero(10) && manaCost(+0x78) > 0)`。
- **`forceHide` 的真来源** = `ShowBattleCard:664 bool showOptions = DisplayCardEffects(card)` ⇒ `ShowCard(..., showOptions, ...)` ⇒ `:446 SetupTutorialObj(card, showOptions)` ⇒ **「这张卡有非空效果行就不画教程指向物」**。
- **13 场序列化现读**（`Battle Arena*.unity` 的 `CardDisplayWindow` 组件，`enableTutorial = 1`；Arena 1/2/3 核了值、13 场核到节点在）：`TutorialObjs` 根 = 全屏 RT（anchor 0,0–1,1 · size 3501.37×2291.01 · pos −46.56/4.38），两个子件 =
  · `UnitObjs`（center · 100×100）→ `MeleeText` 380×55 @(−474,−218) 词条 `Battle/Tips/MeleeAttack` · `RangedText` 380×55 @(−410,−289.59) `Battle/Tips/RangeAttack`（**Range** 不是 Ranged）· `HealthText` 380×55 @(481,−273) `Battle/Tips/HealthPoints`
  · `EnergyText` 340.269×59.342 @(497,252) 词条 `Battle/Tips/EnergyCost`（默认字 `Energy Cost`）

### 2·6 `ShowBattleCard` 伏击支（账物理行 306）—— 本轮把 ① 接上
- 原版 :657-708 的八件：①`ToggleStats(false)` ②`SetEmptyRarityStars` ③`SetTexts("","",Minion,0)` ④换伏击图 ⑤`ChangeSize(Small)` ⑥`DisableAllCards` ⑦`loreObjectBG` 关 ⑧`voiceOverButton` 关。
- 我们：`CardDisplayWindow.cs:1870-1894` —— ②③④⑥⑦⑧ 早前已落；**① 本轮接上**：`Show(masked, null, effects);` **之后** `SlotViews[0].SetStatsHidden(true, hideArmourCell: true)`（必须在 `Show` 之后 —— 视图是 `Show` → `BuildFan` 里**新建**的）。
- **⑤ 的结论（本轮新得）**：`ChangeSize(Small, instant)` 在**产品路径上是空转** —— `CardImageController.ChangeState` 见 `currentState == Small(0)` 提前 return（图本来就是 Small；出处与我们 `CardView.cs:1006-1008` 那段判据一致）⇒ **不需要单独档位件**，不构成缺口。
- **G2 机制差别**：原版窗里是 `cardUIs[]` 多实例，我们这一叠只建主卡；②的宝石是「未知档 ⇒ common」而不是「索引夹到 0」（同张贴图）。
- **G3 观测量**：`MainCardShowsAmbushArt`（`ArtLayerTexture == FaceDownArtTexture()`，⛔ 不读自家 bool）· `CardView.StatsHiddenFor(s)`（真）· `AmbushMarkText(s) == "?"`（读口 `Core/CardView.cs:14283 / :14362`）· `ShownTitle == ""`。

### 2·7 `CardEffectsService.EnableCardEffectSlots`（账物理行 196，J 的 `B→C` 不成立）
- `cpp2il/.../CardEffectsService.cs` 全文 149 行现读：`:91-132 EnableCardEffectSlots`（逐条 `LoadEffect`；**文本空的也占一槽**（`Disable` 但槽号 +1 ⇒ 空洞）；非空才 `num++`；`_indexCardEffectSlot >= slots.Length` ⇒ `return num` 静默截断）、`:18-89 EnableEffects`（= 它 + `DisableLeftoverSlots`，返回 `num > 0`）、`:134-148 DisableLeftoverSlots`。
- **13 场序列化实测**：`cardEffectSlots` 数组长 **恒 = 5**（Arena 1/2/3/Aeldari/Astra/BlackLegion/DarkAngels/EmperorsChildren/Genestealers/Leviathan/Sororitas/SpaceWolves/Tau 逐个读出的 fileID 列表都是 5 项）。
- 我们：`CardDisplayWindow.cs:915-943 SetEffectRows` = `Mathf.Min(rows.Count, CardWinBox.EffSlots)` + 超限 `LogWarning`；`Core/CardWinBox.cs:88 EffSlots = 5`；`Editor/BattleScene.cs:9583-9590` 的 `A985⑨` 断言 `EffSlots == 5`、`:9676` 的灭自证句就在钉「去掉 `Mathf.Min` 会越界」。
- **唯一的机制差别**：原版空文本占槽 ⇒ 中间留空洞；我们按 buff 铸行 ⇒ 不留空洞（净可观察：最多 5 行，不会更多）。
- ⇒ **A**（观测量 = 一张挂 6 条限时增益的单位开窗后 `EffectRowCount`：原版 5 / 我们 5）。

---

## ③ 改了哪些文件 / 哪些行（新增行数）

| 文件 | 改动 | 行数 |
|---|---|---|
| `Assets/CardPresentation/Battle/SkillPanel.cs` | `:2083` 注释 `:170` → `:160`（并补 `:193`/`:188`/`:196`/`:172` 的真实分工）；`:2064` 注释追加「⛔ 别与手牌侧抽共用函数」+ `<i>`/无空格/带换行 desc 的判据 | **0**（2103 → 2103） |
| `Assets/CardPresentation/Battle/CardDisplayWindow.cs` | `ShowBattleCard`（:1870-1894）：把原来 3 行「① 做不了」的 `LogWarning` 换成 3 行注释 + `:1887` 的 `SetStatsHidden(true, hideArmourCell: true)` 调用；`SetEffectRows` 的 doc 就地补「槽数上限 5 = 原版 `cardEffectSlots.Length`，13 场实测」 | **+1**（1909 → 1910；`MainCardShowsAmbushArt` :1898 → :1899，账里那处引用已在补丁里同步改成 :1899） |
| `资料/完全复刻/账/_parts/B8_U.tsv` | **23 行**（战斗界面_HUD 14 + 特效动画 9）：11 列 · LF · 无 CRLF/裸 CR | 新建 |
| `工具/_tmp_b8u_patch.py` | 补丁生成脚本（可复跑校验；不需要就删） | 新建 |

**自查**：`check_a_vs_diff.py` = 13 条弱信号（**全在别的域**：卡组编辑 10 / 收藏 2 / 卡牌面_渲染 1；本补丁未合入，A 行**零命中**缺口词）· `verify_annot.py` = 同名 723 · 异名已注解 2300 · **缺注解 0** · `check_patch_conflicts.py` = **无跨补丁重复键**（5 份补丁）· 自己按工具同一张词表扫补丁里的 A/D 行 = **0 命中**。

---

## ④ 没查清的部分（如实写）

1. **`ObjAnimEffects.Start()` 是否真的空转**：9 处实例的 `waitToPlayEffects` / `animEffects` 序列化值**本轮没读到**（AssetRipper 导出里按 `ObjAnimEffects.cs` 的 meta GUID 搜 prefab/scene = **0 命中**，说明那批 prefab 没被导出 ⇒ 要走原始 bundle 解）。若全是 `waitToPlayEffects=1` + 空数组，本行可回判 D。
2. **`SetupTutorialObj` 那道闸里的 Dictionary key**（`DAT_1842c3bd8`）**没解**，含义疑为「教程进度计数」；另外**其余 10 场的 rect 没逐场读**（只深读了 Arena 1，另核了 2/3 场的 `enableTutorial` 值）。
3. **`ResolveAnimPrefab` 的 `Instantiate` 两个实参**：反编译成 `(&num)(&vector)`（两个 default 局部）⇒ **疑似置零**；方法体里**没看到 `posOffset` 参与**。若原版真用 `posOffset` 偏移，行为会差一个位置 —— 需要 VA 级反汇编复核。
4. **两条 A 的断言没写也没跑**：本批不跑 Unity 宿主（红线），观测量读口已备（§2·1 / §2·6），断言要调度台安排。
5. `CardScript:14231`（伏击翻开那一支）在 Board 面的观感**沿用上一轮的结论**（宝石层在 Board 面不画）—— 本轮**没有独立复核**。

---

## ⑤ 顺手发现（不属于本批，但该报）

1. **`BattleDriver.cs:32630`** 的注释仍写 `cpp2il …/ManaTypeHolder.cs:186-260` ⇒ 与账同步订正（一行；`BattleDriver.cs` 不在本代理白名单）。
2. **账的「物理行」比文件真行小 1**（J §3·5）：本代理所有定位都用 `(类, 方法)` 键 + assert，**没按行号切**，所以不受影响；建议在 `调度台日志` 落一笔口径。
3. **13 场 `cardEffectSlots` 数组长恒 = 5** —— 这是 `CardWinBox.EffSlots = 5` 的**出处**（以前只有常量断言、没有出处）。
4. **`ShowBattleCard` 的 ⑤ `ChangeSize(Small)` 在产品路径上净效果为零**（`ChangeState` 早退）⇒ 「我们没实现这一档」不构成缺口。这条以前没记过，容易下次被人当缺口重做。
5. **`B8_N` 的 `CardView.SetStatsHidden` 与我的 HUD:57 / HUD:307 是同一件缺口的两半**：typecheck 中间态曾报 `Core/CardView.cs(7236/7242/13115) CS0103`（那是它在写、还没声明）。⇒ **两批必须一起合**，否则我的 A 不成立（我在账里已写死这个依赖）。
6. **`B8_L.tsv` 与我的补丁撞了 `OpponentDesync` 那一行**（它 A、我 C）—— 我撤出了那一行；它的落地（`LeaveWithoutResult` + `Close(say:false)`）与我从 J 那里得到的判据**互相独立地一致**。
7. **`判据/B8_特效动画与战斗界面_判据表.tsv` 里 `MainCardShowsAmbushArt` 的 `:1898` 现在是 `:1899`** —— 我在 `ShowBattleCard` 里 +1 行导致的（位置在它上方）。账里那处引用已在补丁中同步改成 `:1899`；判据表**不在本代理白名单**，请调度台顺手订正，或由合并时的 `--fix` 一并扫。
8. `check_a_vs_diff` 那 13 条弱信号里有 **10 条在「卡组编辑」**（`DeckSelectorMenuItemDemo.*` 一族 `/ DeckGeneralInfoDemo.*`／`DeckUtility.*`／`PrebuiltDeck.*`，备注都写「§4.1 同一个 文件:行 覆盖多个方法 ⇒ 只能 B」）——那族的措辞看着是**正当解释**，但按工具口径会被一直报，建议那一线的写手考虑换成不被词表命中的说法（或加 `[非缺口]`）。**本代理未动**。

---

## ⑥ 给调度台的请求

1. **合并顺序**：`B8_N`（`Core/CardView.SetStatsHidden` 写口）与 `B8_U`（`ShowBattleCard` 调用）**同一同步点**合入 —— 我的 HUD:57 / HUD:307 两条 A 依赖它。
2. **`B8_L.tsv` 必须先于任何带 C 的同键补丁**（我已撤，本代理这份没有该键）；合并前跑 `check_patch_conflicts.py`（现读：4→5 份补丁、无重复键）。
3. **`Editor/BattleScene.cs`（白名单外）补一条断言**：「伏击支四项」= `MainCardShowsAmbushArt` + `CardView.StatsHiddenFor(s)` + `AmbushMarkText(s) == "?"` + `ShownTitle == ""`；**灭自证两条**：① 只换图不清字那版要红 ② 只改数据不落层（不调 `SetStatsHidden`）那版要红。
4. **一行订正**：`BattleDriver.cs:32630` 的 `:186-260` → `:186-246`。
5. **派引擎线**：`EffectSpec` 加 `maxTargets` / `targetsAffected`，再接 `BattleDriver.ShowSkillPanel` 喂值（解 HUD:19 与 HUD:147 的一半）。
6. **派教程线**：`SetupTutorialObj` 的四条 TMP（text/词条/rect/名字）已在 HUD:305 的「差异」列里备齐，可直接照建 + 一个「教程在跑」入参；`forceHide` 的等价量我们有现成的（`SetEffectRows` 的 `n > 0`）。
7. `工具/_tmp_b8u_patch.py` 是否留仓：留着可复跑校验补丁生成，不要我就删。
