# B9_W44HUD · `战斗界面_HUD` 域剩余 8 条 C · 定位与尽量落地

> 代理 **B9_W44HUD** · 2026-10-27 · 只读定位为主 + 白名单内改动。
> 任务 = 逐条定位 `ActiveSkillDisplay / CardDisplayWindow / ChooseCardMenu / CardScript.*` 那 8 条 C，尽量落地。
> ⛔ 未跑任何 Unity（红线 1）；只跑了秒级类型检查：`TMPDIR=/tmp/wf_w44 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> 账补丁 = `账/_parts/B9_W44HUD.tsv`（**8 行 + 表头**，只含改动行，⛔ 不是整表快照）。

---

## 一 结论

1. **8 条逐条现核，状态仍全部 `C`** —— 没有一条能升 A：残差**全部落在别人的文件（`BattleDriver.cs` = W40）或数据侧**（`CardDef` / `CardDatabase` / 卡 SO 的 `activeAbilityTextReferenceId`），本代理白名单内**没有可落地的行为改动**。
2. 本笔唯一代码改动 = `Battle/SkillPanel.cs` 两处补 `🔶 自己设计（非原版）` 标记（`:2130` / `:2184`，**原地改写已有注释行、零行数变化**；`SkillPanel.cs` 仍 2,246 行、纯 LF、裸 CR 0）。
3. **两条账内过期口径就地订正**（铁律 5）：
   - **第 2 条「缺教程指向物子树本身」不成立** —— 同族子树早在 `Battle/TutorialOverlay.cs:827-853` 建好（节点名就是 `TutorialObjs`，四个子件 `MeleeText/RangedText/HealthText/EnergyText`）。真残差 = **父链不同** + **闸未实现** + **三条 SetActive 未实现**（详见 §二·第 2 条）。
   - **`hasActiveAbility == 1` 的分母**：`cards_truth.json`（1,351 个 SO）里 **88** 张；其中 `Tempestus Scion`（id = 32 位 hex `66486b3f…`）**不在 1,128 池内** ⇒ **池内 87**。两个数都对，⛔ 别混用（`CardDatabase.cs:554` 写的是池内 87）。
4. **第 5/8 条的落点判据已闭合、且【已落地】**：`enemyHand` 屏幕锚点 = **(960,50) px @1080p** ⇒ 进攻档 `RevealEndPx` 应为 **(589,495)**。本笔工作期间（`BattleDriver.cs` mtime **19:05**）另一路已把它改掉 —— **现读 `BattleDriver.cs:9860` = `new Vector2(589f, 495f)`**、头注 `:9805-9857` 已改成「原版推算值」。⇒ 我**不再改**，只在 §六·① 留一条「怎么复核」。
   - ⚠️ 第 8 条（`GetOffensiveCardPlayedDisplayPosition`）已由另一路 `账/_parts/B9_W49BDR3.tsv` **判 A**，且与现场一致（`RevealEndPx` 已落地）⇒ **本补丁故意不含第 8 条**（否则会把它压回 C）。`check_patch_conflicts.py` 现读 **0 条跨补丁重复键**。
5. **第 7 条落点是错的（本笔新算）**：`DisplayEnemyCardInCenter` 把卡移到**屏幕正中** `(960,540)`（`BattleDriver.cs:25911-25913`），原版移 `enemyHand + 敌方档偏移` ⇒ **(574,406)**。⇒ 改动请求。

---

## 二 逐条（账行 / 现等级现核 / 原版逐句 / 我们原来 / 改成什么 / 怎么验）

### ① `ActiveSkillDisplay.SetupActiveSkill(CardScript parentCard)` — 现等级 **C**（= 账里现值，未过期）

- **原版逐句**（`cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:115-196`，本笔亲读）：
  1. `costText.text = currentAbilityCost.ToString()`（`:135`）—— 能力费。
  2. `nameText.text` = `Card_Abilities/Ability_Name_<activeAbilityTextReferenceId ?? uniqueId>` 过 `ModifyLocalization`，**空则回落 `GetLocalizedCardName()`**（`:139-155`）。
  3. **后缀闸** `activeAbility.targetsAvailableCounter != null && targetsAffected != TargetsAffected.any`（`:160`）；真则 `AbilityLogic.GetTargets(...)`（`:163`）再分两档：
     - `maxTargets == 1 && targets.Count > 0` ⇒ `GetTargetAvailableText(targets[0].GetLocalizedCardName())` = **`"(" + 名字 + ")"`**（`SupportMethods.cs:1494`）
     - 否则 ⇒ `GetTargetsAvailableText(n)` = 词条 `Battle/HUD/TargetsAvailable` 的 `{0}` 替换（`:1487`）
     - 拼法 `desc + " " + 后缀`（`:188`）⇒ 写进 `descText`（`:196`）
  4. 闸假 ⇒ `descText = 描述原样`（`:193`）。
- **我们原来**：后缀闸 `ShouldShowAbilityTargetsCounter`（`BattleDriver.cs:35031`）**恒 false**；后缀由 `SkillPanel.SetTargetsSuffix`（`:2093-2106`）拼；描述链在 `SkillPanel.AbilityDescription`（`:2133`）；名字 = 卡名（`BattleDriver.cs:15606`）；费用恒 `"0"`（`SkillPanel.cs:1564`）。
- **改成什么**（四条，**全在白名单外** ⇒ 改动请求 ④⑤）：
  1. `RuleEngine/Core/CardDef.cs` + `CardDatabase` 补 ability 级 `targetsAvailableCounter` 两格（`targetsAffected` / `maxTargets`）—— 现读 `cards_truth.json` **全 1,351 张 0 条** ⇒ **今天恒 false 是正确的**（这条不是「我们错了」，是「数据里没有」）；
  2. `BattleDriver.ShowSkillPanel:15606` 把 `maxTargets` / `firstTargetName` 喂进去（现在传 `0, null` ⇒ **单目标那一支结构上不可达**）；
  3. `costText` 要 `currentAbilityCost`（引擎无该字段）；
  4. `nameText` 的 `Ability_Name_*` 那一支没写 —— 但官方 10,854 键里该前缀**现读 0 条** ⇒ 今天用卡名**恰好等价**，数据一出现就会漏。
- **怎么验**：`cards_truth.json` 里 `activeAbility.targetsAvailableCounter` 计数（现 0）；一旦 >0，`ShouldShowAbilityTargetsCounter` 必须跟着变真、且面板出现 `" (名字)"` 或 `" {n} 可用"`（自检口 `ShouldShowAbilityTargetsCounterForTest:35036`）。

### ② `CardDisplayWindow.SetupTutorialObj(RawCardScript rawCardData, bool forceHide)` — 现等级 **C**

- **原版逐句**（`decomp_full/CardDisplayWindow__SetupTutorialObj.c:26-72`，本笔亲读全文）：
  - `BattleManager.Instance == null` ⇒ **强制** `forceHide = true`（`:33-36`）；
  - 六项闸：`LiveOpsManager.GetHandler<GameModes>()` 非空 → `GetActiveEvent<TutorialEvent>()` 非空 → 事件 `+0x20` 非空 → `(事件+0x20)+0x70` 的字典非空 → `FUN_1816aaff0(字典)` 得一个计数 → 条件 `!forceHide && enableTutorial(+0xe0) && 计数 < 6`（`:41-46`）；
  - **真支**：`tutorialObj(+0xa0).SetActive(true)` → `tutorialObjUnit(+0xa8).SetActive(rawCardData.IsUnit())` → `tutorialObjCost(+0xb0).SetActive(cardType(+0x94) != 10 /*Hero*/ && ~(uint)manaCost(+0x78) >> 31)`，⚠️ 末项 = **非负**，不是 `> 0`（`:48-58`）；
  - **假支**：`tutorialObj(+0xa0).SetActive(false)`（`:60-63`，共享 `LAB_1807fb629`，**不动**另两个节点）—— 账的「判据要点」只写了真支，**假支漏记**；
  - ⚠️ 原版 `rawCardData == null` 或 `+0xa0/+0xa8/+0xb0` 为 null 时**落到 `FUN_1803f47a0()`（throw）**。
- **我们原来**（现核，纠正账里的旧描述）：`CardDisplayWindow.cs` 里 `TutorialObjs` **只在 3 处注释**（`:21` / `:245` / `:1591`），`SetupTutorialObj` 零命中 —— 这句成立。**但子树不是没有**：`Battle/TutorialOverlay.cs:827-853` 的 `Build()` 建了 `TutorialObjs` + `MeleeText/RangedText/HealthText/EnergyText`，显隐口 `SetAnnotations(on,texts,anchorsWorlds)`（`:1082`，关在 `:899`）；`TutorialOverlay.cs:103-106` 已如实登记「父链与原版不同（我们挂 `Tutorial Canvas`、原版在 `Card Display/Card Display Window`）」。
- **改成什么**：本笔**不建**（三条理由，均有出处）① 闸的数据源 = LiveOps 教程事件，本地任何形态都没有（`Shell/TutorialModePopup.cs:29`：随 PlayFab userInfo 下发）⇒ 建出来**恒不可达**；② 我们的标注四块由**教程脚本动作**驱动（`SetAnnotations`），再叠一条「显示窗打开就亮」会形成**双控**，而无真机可验（自检暂停）；③ 树根 rect 需与父链缩放联立（`B2AU` 结论，本笔未复核逐节 scale）。
- **怎么验**（将来）：`TutorialOverlay.AnnotationsVisible:346` + 显示窗 `ShowBattleCard` 那一跳；两档 = 教程局 / 非教程局。
- **接口点（给教程线）**：闸 + 三条 SetActive 落在 `CardDisplayWindow`，子树复用 `TutorialOverlay` 的 `_annoGo`；**父链要不要归位到 `Card Display`** 需调度台裁（现在挂在 `Tutorial Canvas`，是一次已登记的偏离）。

### ③ `ChooseCardMenu.AutoProcessDone()` — 现等级 **C**

- **原版逐句**（状态机 `ChooseCardMenu.cs:35-62`、方法定义 `:699`、调用点 `:627-638`，本笔亲读）：协程 = `WaitForSeconds(2f)` → `ProcessChooseDone()`；**只在 `matchType == Replay` 且 `!isReplayPlayActive` 时启动**，且启动前先 `SelectCard(_chooseCardUis[0]._cardUI)`（回放自动选第一张）。
- **我们原来**：**没有对应物**，且回放局**根本不弹面板** —— `BeginAsk`（`BattleDriver.cs:11015`）只有 `BeginPlay:11050` / `Resolve` 两个调用点；回放答案取自动作上的「执行前快照」（`BattleDriver.cs:19002`）。
- **改成什么**（改动请求 ③）：录像头加「本次选择候选集」（`CardDef.Id` 数组），写点 = `BeginAsk` 那一跳（候选就是 `_askCands`，`BattleDriver.cs:11186`）；回放路径把它喂 `ChoosePanel` + 显式秒数泵 2 s 后 `OnChooseDone`。⛔ 别只画面板不摆候选。
- **怎么验**：一局含 `choosecard` 的战斗 → 存档 → 回放，面板必须弹出、2 s 后自动选中第一张并继续。

### ④ `ChooseCardMenu.Setup(...)` — 现等级 **C**

- **原版逐句**（`ChooseCardMenu.cs:559-660`，本笔亲读）：① 标题 `Battle/ChooseCard/Instructions-<actingCardId>` → 回落 `Battle/ChooseCard/Instructions`（`:576-593`）；② 继续钮文案 `Battle/Mulligan/ButtonDone` 且 `interactable=false`（`:593-594`）；③ `actingCardId ∈ {"offensive","defensive"}` ⇒ `chooseMenuAnchor.SetActive(false)`（`:596-616`，藏 anchor 在 `:613-614`）；④ **回放自动选**（`:627-638`）；⑤ `timerCounter <= 1` ⇒ `EndSelectionWithDelay`（`:640-647`）。
- **我们原来**：①②⑤ 在（`BattleDriver.ShowAsk:11138` / `_choosePanel.Open:11278` / `ChoosePanel.ResolveTitle:202` / `ConfirmLabel:124` / `CardChoicePanel.TickOpenDelay:732`）；**④ 缺**（与 ③ 同一笔）；③ **无对应物** —— 我们**没有 `ChooseCardMenuAnchor` 这个节点**（卡片行直接挂面板下、`LayOutCards:826` 自己排），该句无落点（`ChoosePanel.Open` 的 doc 早已记「原版 `isEnviromental` 到底改了哪些视觉/文案查不到」）。
- **改成什么**：并入 ③ 的同一笔改动请求（候选集 + 回放自动选）；③ 保持「无对应物」，⛔ 别为一个不存在的节点造新节点。
- **怎么验**：同 ③；标题回落链要保「`Instructions-<id>` 优先于无后缀那条」（现在在 `ChoosePanel.ResolveTitle:202-207`）。

### ⑤ `CardScript.DisplayOffensiveCardInCenter(float,float)` — 现等级 **C**

- **原版逐句**（`decomp_full/CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c:20-100`，本笔亲读）：
  - state 0：`SetCardVisibility(Front)` → `DOMove(GetOffensiveCardPlayedDisplayPosition(), 0.15, SetEase 3)` → `DORotate(Vector3.zero, 0.1)` → `WaitForSeconds(0.1)`；
  - state 1：`DOScale(GetEnemyCardPlayedDisplayScale(), timeToDraw)` → `WaitForSeconds(0.66 × stepTime)`；**⚠️ 本状态机没有 `TurnCardAround`**（翻面只在 d__263）⇒ 我们不做翻面**是对的**；
  - state 2：`DisplayCreatedByText` + `ToggleHighlight(false)` → `WaitForSeconds(1.2 × stepTime)`；state 3：收「由谁创建」那行。
- **我们原来**：reveal 链 `BeginOffensiveRevealOrApply:9968` / `TickOffensiveReveal:10019` / `ApplyRevealInnerPose:10118`，时长/相位 `9826-9853` 全对；终点原先是我们挑的 `(960,520)`。
- **改成什么**：**已有人落地（本笔工作期间，`BattleDriver.cs` mtime 19:05）** —— 现读 `:9860` = `new Vector2(589f, 495f)`，头注已改成「原版推算值」；`RevealStartPx (960,660)` 按设计**保留 🔶**（原版起点 = 世界零、**不在 UI 平面**，正交布局不可表示；起手 0.01 缩放 ⇒ 观感无关），且已就地标 `🔶 自己设计 … 调度台 2026-10-27 裁定`（`:9859`）。
- **⇒ 本行残差已清**（只剩那个已标注的 🔶 起点）⇒ **建议调度台复判 A/B，本笔不自行升级**（`A` 的门槛是「逐条对过 + 行为一致」，起点那半格是已裁定的自设计）。
- **怎么验**：真机看揭示卡落点「偏左、垂直近中」；批处理可断两个像素点之间的距离（不要断浮点精确相等）。

### ⑥ `CardScript.GetCurrentLocalizedAbilityDescription()` — 现等级 **C**

- **原版逐句**（`decomp_full/CardScript__GetCurrentLocalizedAbilityDescription.c` 全文，本笔亲读）：`DoesAbilityRequireLoading()` 真 ⇒（`!activeAbilityLoaded` ⇒ `Desc("-load")`，**空则** `ModifyLocalization(Loc("Card_Abilities/GenericSiegeText"))`；否则 `Desc("-effect")`，空则 `Desc()` 裸键）；假 ⇒（`ability.needsLoading && HasRemovedTrait(reload)` ⇒ `DescRemovingSiege()`；否则裸键）。`Desc(suffix)` 键 = `Card_Abilities/ + (activeAbilityTextReferenceId ?? uniqueId) + suffix`。
- **我们原来**：`SkillPanel.AbilityDescription:2133` 三档 + `AbilityBareText:2187` / `AbilityRemovingSiege:2202` / `AbilityTerm:2214` / `AbilityRaw:2223` / `AbilityMarked:2239`，与上面逐句对齐（`B9_W21SKILL` 已修完三处取键偏离）。本笔现读复核「数据里没有」三条：官方 10,854 键里 `Card_Abilities/*-load` = **0**、`*-effect` = **0**、`Card_Abilities/GenericSiegeText` = **0**。
- **改成什么**：
  1. **🔴 第四支缺口（本笔新查出）**：`RawCardScript.GetLocalizedAbilityDesc` 还有一支 —— `activeAbilityTextReferenceId.Contains("((")` 时**整条跳过本地化、把 id 当正文**送 `ModifyLocalization`（`RawCardScript.cs:2384-2405`）。我们这层没有。它是「34 条拼不出来」里那 **30 条 `Effect<名>`** 的真实归属（它们**不是键、是正文**）。
  2. 数据列 `activeAbilityTextReferenceId`（原版 SO `+0x248`）—— 来源 = 1,351 个卡 SO，⛔ 表现层猜不出来。
  3. 本笔已把「缺键退回模板句/裸描述」两处补上 `🔶 自己设计` 标记（`SkillPanel.cs:2130` / `:2184`，零行数）。
- **怎么验**：`hasActiveAbility==1` 且能拼出 `Card_Abilities/<id>` 的 86 张卡，技能面板文本必须与官方表逐字一致；今天 `SOR66`（Armorium Cherub）走模板句（原版会用它的 textReferenceId 那条）。

### ⑦ `CardScript.GetEnemyCardPlayedDisplayPosition()` — 现等级 **C**

- **原版逐句**（`decomp_full/CardScript__GetEnemyCardPlayedDisplayPosition.c:60-75`）：`enemyHand.transform.position + Vector3.right × enemyCardDisplayXoffset(+0x54 = −26) − Vector3.up × enemyCardDisplayYoffset(+0x50 = 24)`，移动档 X 换 `+0x58 = +25`（⚠️ **符号与桌面档相反**，是两个字段）。`_DisplayEnemyCardInCenter` 的 state 0 用它 `DOMove(…, 0.15)`（`…d__263__MoveNext.c:28-46`）。
- **我们原来**：`BattleDriver.DisplayEnemyCardInCenter:25877` 把卡移到**屏幕正中** —— `centerWorld = LayoutSpace.ToWorld(0.5f, 0.5f)`（`:25911-25913`），`DOLocalMove(centerLocal, 0.15)`（`:25940`）⇒ **(960,540)**。
- **改成什么**：终点换成 `enemyHand 锚点(960,50) + PlayedCardDisplayOffsetPx(false, SmallScreenUI.Enabled, 1080)` ⇒ 桌面档 **(574,406)**（`x = 960 − 26×14.835 = 574.3`；`y = 50 + 24×14.835 = 406.0`）；小屏档 **(1331,406)**。偏移/公式已在 `GameplayVariables.cs:607-623` / `PlayedCardDisplayOffsetPx:682`，**现读零消费者**。**改点在 `BattleDriver.cs` ⇒ 改动请求 ②**。
- **怎么验**：真机比对「打出的非单位卡停在偏左、略偏上」；批处理断落点与屏幕中心的差 > 100 px 且更靠左（结构断言，别比浮点）。

### ⑧ `CardScript.GetOffensiveCardPlayedDisplayPosition()` — 账里现值 **C**，但**另一路已判 A**（本笔不覆盖）

- **原版逐句**：与 ⑦ **除读哪三个偏移字段外逐行相同**（`.c:60-75` 两处），进攻档 `Yoffset(+0x5c) = 30` / `Xoffset(+0x60) = −25` / 移动档 `(+0x64) = −25`。
- **我们原来**：同一 reveal 链（见 ⑤），`RevealEndPx` 原为 `(960,520)`。
- **现状（本笔现读）**：`RevealEndPx = (589,495)` **已落地**（`BattleDriver.cs:9860`），公式只有一份（`GameplayVariables.PlayedCardDisplayPosition:650`，⛔ 别写第二份）。
- **⇒ 本笔不改这一行**：`账/_parts/B9_W49BDR3.tsv` 已判 **A**，且与现场一致 ⇒ **我的补丁故意不含第 8 条**（跨补丁重复键会把它的 A 压回 C，`check_patch_conflicts.py` 现读 0 条重复键）。
- **怎么验**：同 ⑤。

---

## 三 证据（文件:行号）

| 断言 | 出处 |
|---|---|
| 后缀闸 `:160`、单目标支 `:172`、拼串 `:188`、`descText` `:196`；`costText` `:135`；`nameText` `:139-155` | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:115-196` |
| `GetTargetAvailableText` = 括号包名字；`GetTargetsAvailableText` = 词条 `{0}` 替换 | `…/SupportMethods.cs:1478-1496` |
| 我们侧后缀两档 + 闸 | `CardPresentation/Battle/SkillPanel.cs:2093-2106`；`BattleDriver.cs:15607`；`:35031-35034`（恒 false） |
| 技能描述三档链 | `CardPresentation/Battle/SkillPanel.cs:2133-2164` |
| `Contains("((")` 第三支（跳过本地化、id 当正文） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2384-2405` |
| 官方表：10,854 键 · `Card_Abilities/*` 120 · `Ability_Name_*` 0 · `-load`/`-effect`/`GenericSiegeText` 各 0 | `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（本笔现读） |
| `hasActiveAbility==1`：SO 88 / 池内 87；能拼出 86 且全在 88 内 | `RuleEngine/Resources/cards_truth.json` + `cards_engine.json`（本笔现读）；`CardDatabase.cs:554` |
| `SetupTutorialObj` 全 body（含假支与 throw） | `d:/2/tools/decomp_full/CardDisplayWindow__SetupTutorialObj.c:26-72` |
| 我们已有 `TutorialObjs` 子树 + 四个 Text + 显隐口 | `CardPresentation/Battle/TutorialOverlay.cs:827-853` / `:899` / `:1082` / `AnnotationsVisible:346` |
| 父链偏离已登记 | `CardPresentation/Battle/TutorialOverlay.cs:103-106` |
| 闸数据源在远端 | `CardPresentation/Shell/TutorialModePopup.cs:29` |
| `AutoProcessDone` = 2 s → `ProcessChooseDone`；只在 Replay 且 `!isReplayPlayActive` 启动 | `…/ChooseCardMenu.cs:35-62` / `:699` / `:627-638` |
| 回放局不弹面板（答案取自「执行前快照」） | `BattleDriver.cs:11015`（`BeginAsk`）/ `:11050`（唯一调用点）/ `:19002` |
| `Setup` 五段（标题 / 钮 / 藏 anchor / 回放自动选 / 延迟） | `…/ChooseCardMenu.cs:559-660`（`:576-593` / `:593-594` / `:596-616` / `:627-638` / `:640-647`） |
| 我们侧 ①②⑤ + 无 anchor 节点 | `BattleDriver.cs:11138` / `:11278`；`ChoosePanel.cs:202-222`；`CardChoicePanel.cs:732` |
| d__264 状态机（含「没有 TurnCardAround」） | `decomp_full/CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c:20-100` |
| 我们 reveal 链 + 时长 + px 常量 | `BattleDriver.cs:9968` / `:10019` / `:10118-10125` / `:9826-9853` / `:9859-9860` |
| `enemyHand` 屏幕锚点 (960,50) + `RevealEndPx` (589,495) | `资料/完全复刻/判据/enemyHand屏幕锚点_判定.md:14` / `:49` / `:142-146` |
| 敌方档偏移 −26/+24、进攻档 −25/+30、移动档符号 | `RuleEngine/Core/GameplayVariables.cs:607-623`；公式 `:650-661`；px 换算 `:682-693`（**零消费者**） |
| 我们这条把卡移到屏幕正中 | `BattleDriver.cs:25911-25913` / `:25940` |
| 敌方展示状态机 state0 走 `GetEnemyCardPlayedDisplayPosition` | `decomp_full/CardScript._DisplayEnemyCardInCenter_d__263__MoveNext.c:28-46` |
| ability 级 `targetsAvailableCounter` 生成器已 emit | `工具/gen_cards_truth.py:308` |

---

## 四 没查清的部分

1. **第 2 条**：`TutorialObjs` 树根 rect 与父链缩放**未逐节联立复核**（沿用 `B2AU` 的结论）；`+0xa0/+0xa8/+0xb0` 三个节点在 prefab 里的实际 rect/字体未读。⇒ 要建之前必须先做这一步。
2. **第 2 条**：原版闸里 `FUN_1816aaff0(字典)` 到底是 `Where(...).Count()` 还是字典的 `Count`（账里已订正为 `.Count`），本笔**未再反汇编复核**（沿用两档反编译一致的结论）。
3. **第 1 条**：`currentAbilityCost`（原版 `CardScript` 字段）**在引擎里没有对应字段**，也没查它被哪些效果改（装填/减费）⇒ 费用格今天只能保持 `"0"`。
4. **第 6 条**：`activeAbilityTextReferenceId` 那 1,351 个 SO 字段**没有提取**（来源已知 = SO `+0x248`，`D:/2/关服前存档/…/__data` 有 allcards）；34 条不可拼里除 30 条 `Effect<名>` 外那 **4 条非卡 id** 未逐条看。
5. **第 5/7/8 条**：**非 1080p 档**的落点未算（`enemyHand屏幕锚点_判定.md` §五·3 已说明按宽匹配会让 1 画布单位 ≠ 1 px）；我们 `LayoutSpace` 走全局映射，本条不算新问题。
6. **第 7 条**：我们这条的**起点**是「那一侧手牌末位」（`BattleDriver.cs:25909`），原版搬的是卡本体在棋盘/手牌上的当前位置 —— 已知降级（`:26033-26035` 如实登记），本笔未改。

---

## 五 改动清单

| # | 文件 | 行 | 改了什么 | 行数变化 | 校验 |
|---|---|---|---|---|---|
| 1 | `CardPresentation/Battle/SkillPanel.cs` | `:2130` | `AbilityDescription` 的 doc 头补 `🔶 自己设计（非原版）：两条后缀键都缺时退回裸描述 …· 调度台 2026-10-27 裁定` | **0**（原地改写） | typecheck 0/0 · 行数 2,246 不变 · 纯 LF / 裸 CR 0 |
| 2 | `CardPresentation/Battle/SkillPanel.cs` | `:2184` | `AbilityBareText` 的 doc 补 `🔶 自己设计（非原版）：缺键退回模板句 …` | **0**（原地改写） | 同上 |
| 3 | `资料/完全复刻/报告/B9_W44HUD_战斗界面剩余C.md` | — | 本报告（新建） | — | — |
| 4 | `资料/完全复刻/账/_parts/B9_W44HUD.tsv` | — | **7 行**补丁（**只含改动行** + 表头；11 列；`方法` 列全是带 `(...)` 的完整签名；**故意不含第 8 条**） | — | 已逐行断言 11 列、无 tab/换行污染；`check_patch_conflicts.py` = 0 条跨补丁重复键 |

⛔ 没动：`BattleDriver.cs`（W40）· `RuleEngine/**` · `*.unity` · `ProjectSettings/` · 账主表 · 正本 · 别人的报告。

---

## 六 改动请求（给 W40 之后接 `BattleDriver.cs` 的人）

> 下面每一处都**只改一个常量/一处表达式**，判据都闭合了。

### ① `RevealEndPx` → (589,495)（覆盖第 5、8 条）—— ✅ **本笔工作期间已落地，只需复核**
- 现读 `BattleDriver.cs:9860` = `new Vector2(589f, 495f)`、`RevealStartPx` 仍 `(960,660)` 且已标 `🔶 自己设计 … 调度台 2026-10-27 裁定`（`:9859`）；头注 `:9805-9857` 已改成「原版推算值」。（§一·4）
- **还差一步（仍未做）**：常量是**写死的 1080p 值**。更彻底的做法 = 换成运行期算式 `enemyHandAnchorPx + VarsGlobal.PlayedCardDisplayOffsetPx(offensive: true, SmallScreenUI.Enabled, 1080f)`，锚点 `(960,50)` ⇒ 4:3 / 小屏档自动跟着变（小屏档进攻档 X 同为 −25，故只有 4:3 那档会差）。
- 复核：真机看揭示卡停在「偏左、垂直近中」；或断言 `Vector2.Distance(FromPixel(589,495), FromPixel(960,520)) > 100f`。

### ② `DisplayEnemyCardInCenter` 的终点 → (574,406)（第 7 条）
- `BattleDriver.cs:25911-25913`：`centerWorld = LayoutSpace.ToWorld(0.5f, 0.5f)` 换成 `LayoutSpace.FromPixel(960 + dx, 50 + dy)`，其中 `(dx,dy) = VarsGlobal.PlayedCardDisplayOffsetPx(offensive: false, SmallScreenUI.Enabled, 1080f)`（桌面档 ⇒ `(−385.7, +356.0)` ⇒ **(574,406)**；小屏档 X = `+25×14.835 = +370.9` ⇒ (1331,406)）。
- 顺便把 `PlayedCardDisplayOffsetPx` / `PlayedCardDisplayPosition` 从「零消费者」变成唯一入口（⛔ 别在 `BattleDriver` 里再写一份 ×14.835）。
- 验证：同 ①；**别断浮点**，断「落点与屏幕中心差 > 100 px 且 x 更靠左」。

### ③ 回放选牌面板 + 2 s 自动选（第 3、4 条，同一笔）
1. 录像头加一格 `askCandidates`（`CardDef.Id` 列表；`ReplayRecord` 新增字段、`version` 不 +1，照 `tutorialStage` / `botSeatPlus2` 的先例）；写入点 = `BeginAsk`（`BattleDriver.cs:11015`）或 `ShowAsk` 里 `_askCands` 定稿之后（`:11186`）。
2. 逐动作粒度要能定位到「第几次 ask」（现在没有 ask 序号 ⇒ 建议 `MsgAction` 挂 `askIndex` 或平行数组）。
3. 回放路径：同一个 `ShowAsk` 里 `_rec != null` 时走原版分支 —— 先选第一张，再**显式秒数**泵 2 s（批处理无协程）后调 `OnChooseDone`；`isReplayPlayActive` 那半档（回放正在播）只选不自动确认。
4. ⛔ 别只画面板不摆候选；⛔ 别把「标题回落 `Instructions-<id>` 优先」挪走（`ChoosePanel.ResolveTitle:202-207`）。

### ④ 技能面板两处喂参（第 1 条）
- `BattleDriver.ShowSkillPanel:15606`：`maxTargets` 传 ability 级 `targetsAvailableCounter.maxTargets`，`firstTargetName` 传「第一个合法目标的本地化卡名」；
- `SkillPanel.Show:1564`：费用格从 `"0"` 改成引擎喂进来的 `currentAbilityCost`（需先加字段）。

### ⑤ 数据侧（第 1、6 条）
- `RuleEngine/Core/CardDef.cs` + `CardDatabase`：ability 级 `targetsAvailableCounter`（`targetsAffected` / `maxTargets`）；`activeAbilityTextReferenceId`（SO `+0x248`）；同时补 `RawCardScript.GetLocalizedAbilityDesc` 的 `Contains("((")` 那一支（正文直接用）。
- 提醒：生成器**已** emit ability 级 `targetsAvailableCounter`（`工具/gen_cards_truth.py:308`），**生成器不用改**。

---

## 七 顺手发现（只报，未越界）

1. **`hasActiveAbility==1` 有两个正确数**：SO 口径 **88**（`cards_truth.json`，1,351 张）/ 池口径 **87**（`cards_engine.json` 1,128 张）；差的那张 = `Tempestus Scion`（id = `66486b3f61682ab42ba2ce0fa921002f`，32 位 hex，**不在池内**）。`CardDatabase.cs:554` 写 87 是池口径，**对**；引用时必须说清分母。
2. **`GameplayVariables.PlayedCardDisplayPosition:650` / `PlayedCardDisplayOffsetPx:682` 现读零消费者** —— 判据齐了、没接线，第 7/8 条卡的就是这一步（全仓 grep 只命中定义处）。
3. **`Card_Abilities/Ability_Name_*` 在官方 10,854 键里 0 条** ⇒ 原版 `nameText` 那条今天**必然**回落卡名 ⇒ 我们用卡名**恰好等价**；但**分支本身没写**，数据一出现就会漏（第 1 条④）。
4. **`RawCardScript.GetLocalizedAbilityDesc` 的 `Contains("((")` 第三支**（`RawCardScript.cs:2384`）：命中时**整条跳过本地化、把 id 当正文**。这解释了「34 条拼不出来」里 30 条 `Effect<名>` —— **它们不是键**。账里「34 条」的口径应改成「4 条真缺 + 30 条走正文支」。
5. **`SetupTutorialObj` 的假支（关 `tutorialObj`）账里漏记**；且原版 `rawCardData == null` 时**落到 throw**（`FUN_1803f47a0`），不是「什么都不做」。
6. **`ChooseCardMenu.Setup` 的 ③ 还有第二个作用**（`actingCardId ∈ {offensive, defensive}` ⇒ `chooseMenuAnchor.SetActive(false)`，`:596-616`）—— 我们**没有** `ChooseCardMenuAnchor` 节点，该句无对应物；账的「判据要点」只写了「只影响藏 anchor」，没写「我们无此节点」。
7. **`BattleDriver.cs` 行号漂移 5 条**（本轮现读修正）：`BeginOffensiveRevealOrApply` 9940→**9968** · `ShowAsk` 10968→**11138** · `DisplayEnemyCardInCenter` 25782→**25877** · `ShowSkillPanel` 无行号→**15587** · `ShouldShowAbilityTargetsCounter` 无行号→**35031**。W40 还在改该文件 ⇒ 合并时**以现读为准**（`check_ledger_points --fix` 按符号名重定位即可）。
8. **并发提示（给调度台）**：本笔工作期间 `BattleDriver.cs` 被另一路改过（mtime **19:05**，`RevealEndPx` 落位 + `RevealStartPx` 补 `🔶` 标记）；同域还有一份未合并补丁 `账/_parts/B9_W49BDR3.tsv`（把第 8 条判 **A**）。⇒ **我的补丁已移出第 8 条**，现读 `check_patch_conflicts.py` = **0 条跨补丁重复键**；两份补丁可任意顺序合并。⚠️ 主表 `战斗界面_HUD.tsv` 在本笔之前（19:00）也被改过 16 行（不是本笔）—— 我的补丁是**基于改后版本**生成的，已把这些内容原样保留。

---

## 八 交接给下一个代理（若需继续）

- 本笔已闭合：① 8 条的**现状定位与残差归属**（哪条卡在谁的文件上）；② `RevealEndPx = (589,495)`（**已落地**）与 **敌方档 (574,406)**（**未落地**）两个数值；③ 第 2 条「子树已在」这一条订正；④ 第 6 条的第四支缺口。
- **可立刻接着做的**（都在 `BattleDriver.cs`，需 W40 让位或由 W40 自己做）：§六 **②（敌方档落点 574,406）+ ④（技能面板两处喂参）+ ① 的第二步（常量换运行期算式）** —— 都是一两个表达式，判据全闭合。
- **必须先有数据/裁定才能做的**：第 2 条（闸数据源在远端 + 父链归位要裁定）· 第 3/4 条（录像格式扩字段 + 回放路径）· 第 6 条（`activeAbilityTextReferenceId` 提取）。
- ⛔ 别再往 `CardDisplayWindow` 里加「展示位偏移」（那是卡本体的位移，`B2AU` / `W13CDW` 已两次写明）；⛔ 别把 `ShouldShowAbilityTargetsCounter` 改回卡级读法。
