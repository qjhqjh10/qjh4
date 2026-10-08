# R-ENG · 引擎族 7 条现核 + 判据检索

> 只读现核代理 **R-ENG** · 2026-10-18 · HEAD = `fe239e9`（工作树有别人在飞的改动，**本代理一处未碰**）
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改任何文件（**本文件是唯一产出**）
> 判据口径：**① 全量反编译方法体（第一权威）→ ② 解包资源字段 → ③ 本仓既有判据**

---

## 1. 一句话结论

| 分类 | 条数 | 是哪几条 |
|---|---|---|
| **判据本轮【补齐】、可现在就派** | **5** | `A858`（**行为不用改**，只补注释+断言）· `A896` · `A899` · `A947`（**零可观测差异**）· `A962` 的 ② |
| **假账（已做过 / 不用改）** | **2** | **`A962` 的 ①（已收口，实现+断言都在）** · **`A858` 的「要查的那一格」（查实了：**不该挪**）** |
| **判据【仍缺】、⛔ 现在不能派「照原版改」** | **1(+1)** | `A963`（卡 SO 在本地缺失的 bundle 里）· ＋ `A338` 的「≈44 处」**账本身对不上**（无法派，也不建议派） |
| **可派但【要先裁一次】** | **2** | `A896`（我们那条守卫原版没有对应物）· `A963`（只能先派「止血」件） |

**本轮从原版新挖出来的六条硬判据**（原先账上写「没查 / 没查实 / 没读清 / 没有判据」的，全部有了）：

1. **`A858`**：原版「本回合死过」是**每张卡一个 `turnDied`（`CardScript +0x250`）**，**全库唯一写点 = 反噬入队那一跳**（`CardScript__TriggerUnitBacklashActions.c:82`）⇒ **我们不该挪**。
2. **`A947`**：那两个 trait 号 = **`0x96`(150) `DefinedTrait.cantAttack`** / **`0x370`(880) `DefinedTrait.nonCombatant`**。
3. **`A947` 附带**：原版**不是**「纯查询时」—— `canAct`（`+0x230`）是**缓存位**，`fast`/`flank`/`ferocity`/`oath` 在**落地的 trait 激活那一跳**写它。
4. **`A899`**：`+0x28` 的困惑解开了 —— 是 **`ScriptedActionCampaignData.waitTime`**；`textReference` 在 **`+0x20`**（**`ScriptedAction.textReference` 才是 `+0x28`** —— 两张表搞混了）。
5. **`A896`**：原版**有**这条 y 判据，而且比的是**指针**（`mouseCanvasPos.y`），那条线 = 场景节点 **`HandLimitArea`**（`BattleManager.playerHandPosLimitObj`）。
6. **`A963`**：原版「给手牌挂效果」**只有一条路**（`AbilityEffect.buffHand = 50` → `PlayerHand.AddHandEffect`，**带 criteria、会扩散给后进手牌的牌**）；`buffSelf = 40` 那条**没有任何记录**。**BL15 走哪条 = 卡 SO 里的整数 ⇒ 在本地缺失的 bundle 里**。

---

## 2. 逐条表

| 编号 | ①还成不成立 | ②判据(`文件:行号`) | ③要改哪些文件 | ④最小改法（一句话） | ⑤自检宿主 | ⑥能否现在派 | 置信度 |
|---|---|---|---|---|---|---|---|
| **`A858`** | **行为✅不用改**；**记录必须改**（账上「判据未查」已作废） | 写点 `decomp_full/CardScript__TriggerUnitBacklashActions.c:82`；回合号 `BattleManager.turnCounter`@`+0x3F8`（`BattleManager__set_turnCounter.c:10`·`BattleManager._NextTurn_d__395__MoveNext.c:175`）；读点 `CardScript__HasDiedThisTurn.c`/`__HasDiedSinceLastTurn.c`；消费 `FilterMethods__MeetsConditionCriteria.c:254-266`（`0xd'd`=221）；调用点 `CardScript__CheckIfDead.c:135`(死亡支)/`:141`(**变残骸支**)；字段名 `il2cpp_out/dump.cs:35470`(`turnDied // 0x250`) | `RuleEngine/Core/RuleCore.cs`（**仅注释** `:3111-3115`）· 建议加断言 `RuleEngine/Editor/RuleEngineTest.cs` | **别挪** `ctx.DiedThisTurn++`；把那三行注释换成查实的判据（写点=反噬入队、不是进墓地），并补一条「反噬期间它已算死者、`DeadUnits` 还没算」的断言 | `RuleEngineTest.Run` | ✅ **可派**（纯注释+断言，零行为改动） | **高** |
| **`A896`** | **成立**（我们那句仍是 `t.position`）· 且**「没有原版判据」这一条已被推翻** | `decomp_full/BattleManager__Update.c:151-157`（线 = `playerHandPosLimitObj.transform.position`）· `:99-124`（每帧写 `mouseCanvasPos`）· `:192-195`/`:249`/`:291`（`线.y < mouse.y`）· `:324`（`pointerCursorStart.y` 再叠 `globalVars` 一个偏移）；字段名 `dump.cs`（`playerHandPosLimitObj // 0x180`·`mouseCanvasPos // 0x434`·`pointerCursorStart // 0x320`）；**那条线是哪个节点**：`解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_4371.json:184-187`（`m_PathID 141`）→ `…/GameObject/HandLimitArea_141.json`；13 场全树都是 `HandLimitArea [-0,779 1x1]` | `CardPresentation/Hand/CardInteraction.cs`（`:262`）· 断言宿主 `Editor/BattleScene.cs` | 把守卫里的 `LayoutSpace.ToNormalized(t.position).y` 换成**指针**（`world`，同方法形参）；**先由调度台裁一句**：我们这条守卫原版没有对应物（原版那条 y 判据守的是「要不要试着打到场上」，**手牌让位在 case 2 尾部无条件跑**）⇒ 「保留但不换」「保留并换指针」「删掉」三选一 | `BattleScene.Run` | ⚠️ **判据齐、但要先裁**（裁完即可派） | **高**（判据）/ **中**（该不该保留） |
| **`A899`** | **成立**（我们只做了 `waitBefore/waitAfter` 那一半，`SkippableActionWait` 三条语义没做） | `decomp_full/AiScripted__GetCurrentWaitTime.c:14-43`（**战役**版）= `元素+0x28` + 音效时长；`AiScripted__GetDelay.c:14-43`（**教程**版）= `+0x24 waitAfter` / `+0x20 waitBefore` + 两条前置；`AiScripted._SkippableActionWait_d__15__MoveNext.c:25/26/60-70`；`AiScripted__CanSkipAction.c`（可跳过的 5 档）；字段名 `dump.cs:89410-89422`（`ScriptedActionCampaignData.waitTime // 0x28`·`textReference // 0x20`）· `dump.cs:42111-42122`（`ScriptedAction.textReference // 0x28`）· `dump.cs:24549-24554`（`AiScripted.actionCounter // 0x10`·`actionCampaignCounter // 0x14`） | 注释：`RuleEngine/Core/TutorialScript.cs:711` · `CardPresentation/Battle/BattleDriver.cs:7956`；**要做的那一半**在演出节拍那批（`BattleDriver` 的 `_postTimer` 一族） | 把三条语义写进注释（`time==0` ⇒ 不等 / 聊天那 5 档可被点掉 / 其余真等 `time` 秒），**并纠正 `BattleDriver.cs:7956` 那句「教程那份重载缺失」**（`GetCurrentWaitTime` 全库只有一个签名，教程侧用的是 `GetDelay`，**两份都在本地**） | `BattleScene.Run` | ✅ **可派**（判据齐；实际落码随演出节拍那批 `A940`，本件先落注释） | **高** |
| **`A947`** | **成立**（`noncombatant` 确实没有），但**账上的前提一半是错的** | 两个 trait 号：`decomp_full/CardScript__CanAttackNow.c:25`(`0x96`)/`:27`(`0x370`) → `dump.cs:45721-45896` 的 `DefinedTrait`（**150 = `cantAttack`**·**880 = `nonCombatant`**）；返回值 `CanAttackNow.c:65-74` 读 `canAttack // 0x23C`，前置 `:57-64` 读 `canAct // 0x230`；`CanActNow` = 两者之或；**`canAct` 的写点** `CardScript__ActivateTraitsOnSummonOrEnchantment.c`（`0x28` fast/`0x1cc` flank ⇒ 只 `canAct`；`0x4f1` ferocity/`0x4fb` oath ⇒ `canAct`+`canAttack`）· `CardScript__ActivateMinion.c:39/68` · `CardScript__Stun.c:105` | `RuleEngine/Core/RuleCore.cs`（`CanAttackNow` 补一条查询时禁令）· `Core/CardDef.cs`（`KeywordTable` 加 `noncombatant` + `Implemented`） | 在 `RuleCore.CanAttackNow` 里补一句 **查询时**读 `noncombatant`（`cantattack` 已经在 `IsValidTarget:2002` 读过）；**⛔ 别把 `Exhausted` 改成现算** —— 原版 `canAct` 就是缓存位 | `RuleEngineTest.Run` | ✅ **可派**，但**零可观测差异**（全池 1126 张**没有一张**带这两个 trait、也没有卡面授予）⇒ 属**结构复原** | **高** |
| **`A338`** | ⚠️ **本体已收口**（344 处/72 文件）；**「≈44 处」这笔账本身对不上**（见 §4）· **卡组子集 5 处**今天**仍开着且行号又漂了一轮** | 报告 `资料/普查产出_1016/A338_行号引用改符号.md:14/16/79-121`；卡组子集 5 处 = `资料/普查产出_1017/盘点_卡组编辑部分文档.md:16`（**§4 已逐处现读**）；`A895`（`Editor/BattleScene.cs` 5 处）= `资料/历史/A表批次行归档_1018.md:35` | 「约 44 处」散在 ~30 个文件（§4 逐条列出）；卡组子集 = `Shell/{PointerLayer,MenuScroll,CollectionWindow}.cs` · `Editor/{DeckScene,BattleScene}.cs` | 「≈44」那批**无法逐处派**（原文自己就判不出该指哪）⇒ **只派卡组子集 5 处 + `A895` 那 5 处**：一律改成**按符号认**（`X.cs` 的 `成员名`），⛔ 不抄行号 | **0 条**（纯注释；A338 本件自己也是「零 Unity 自检」） | ⚠️ **卡组子集可派**；**「≈44」那批⛔ 不能派**（账不成立，见 §4） | 卡组子集**高** / 「≈44」**低**（账本身） |
| **`A962`** | **① 假账（已收口）** · **② 成立、仍开** | **①** 实现 `RuleEngine/Core/EffectResolver.cs:4826-4864`（快照 → 棋盘跳 → 四条非棋盘跳）+ 注释 `:4756-4771`；**断言** `RuleEngine/Editor/RuleEngineTest.cs:19493-19512`（④ 次序 + `:19509` 前提）· **②** 我们 `Core/RuleCore.cs:1135-1148`；原版 `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:276-295`（`RemoveCardFromDeck` → `BroadcastCardDrawn` → `SetCardTurnDrawn`）· `BattleManager__RemoveCardFromDeck.c`（只 `List.Remove`）· `PlayerHand._AddDrawnCardToHand_d__37`（进手牌在**更后面**） | **②**：`RuleEngine/Core/RuleCore.cs`（`Draw` 里两句调换次序）· `RuleEngine/Editor/RuleEngineTest.cs`（判别式断言） | ① **销账即可**（判据+实现+断言三样都在）。② 把 `inst.DrawnThisTurn = true` 挪到 `BroadcastWhen(Draw, …)` **之后**，并加一条判别式断言（挡「两边一起改回旧写法」） | `RuleEngineTest.Run` | ✅ **② 可派**；① 只需销账 | **高** |
| **`A963`** | **成立**（本批 `fe239e9` 新引入的行为改动 + 记录会扩散） | 我们侧 `RuleEngine/Core/EffectResolver.cs:5176-5181`（self-op 挂 `HandTroopCriteria`）· `:5227-5236`（`HandListenerSelfOp`）· `:2764-2769`（`HandTroopCriteria` 定义）· `:2948-2970`（记录进 `HandEffectRecords`）；原版两条路：`AbilityLogic__PlayAbility.c:1431`（外面那道闸 = `iVar8 == 0x32` = **`AbilityEffect.buffHand = 50`**，`dump.cs:20645+`）vs 同方法里 `BattleManager__AddEffect(bm, actingCard, …)`（= `buffSelf = 40`）；`PlayerHand__AddHandEffect.c:88-139`（`:90-110` 先登记 → `:112-139` 再按 `targetCriteria @+0x20` 逐张贴）· 触发侧 `BattleManagerSupport__BroadcastCardDrawn.c:40-72` · `CardScript__CardDrawn.c:33` | `RuleEngine/Core/EffectResolver.cs`（`BroadcastHandWhen` 的 self-op 那一句）· `RuleEngine/Editor/RuleEngineTest.cs` | ⛔ **先别照原版改**（判据缺一格：BL15 的 `AbilityEffect` 是 40 还是 50）。**可派的只有止血件**：把 `criteria: HandTroopCriteria` 换成**不会扩散**的做法（收到「只匹配它自己」或干脆不登记记录）——依据是**「`gain X`（无主语）在卡面上没有任何『给别人的手牌』的意思」** | `RuleEngineTest.Run` | ⛔ **不能派「照原版改」**；✅ **可派止血件（要先裁）** | **高**（机制）/ **缺**（原版那格） |

---

## 3. 逐条展开

### `A858` · `ctx.DiedThisTurn++` 该不该下移 —— **不该挪，账上那格已查实**

**账上写的**：「原版那个等价计数在**哪一跳** `++`（若也在「进墓地」那一跳 ⇒ 我们得跟着挪）。**判据未查**」。

**现查实**（全部亲读方法体）：

- 原版**没有「本回合阵亡数」这种计数**。有的是**每张卡一个 `turnDied`**：`CardScript.turnDied`（`il2cpp_out/dump.cs:35470` `private int <turnDied>k__BackingField; // 0x250`）。
- **全库唯一写点** = `decomp_full/CardScript__TriggerUnitBacklashActions.c:70-82` —— 取 `CardScript.BattleMgr`（`+0x258`）的 **`turnCounter`（`+0x3F8`，`ObscuredInt`）**，解码后写进 `+0x250`。`turnCounter` 的写点：`BattleManager__set_turnCounter.c:10` · `BattleManager._NextTurn_d__395__MoveNext.c:175`（换回合）· `SetupInitialState.c:122` · `BattleFinished.c:84`。
- **读点是两个谓词**：`CardScript__HasDiedThisTurn.c` = `turnDied == turnCounter`；`CardScript__HasDiedSinceLastTurn.c` = `turnDied == turnCounter || turnDied == turnCounter − 1`。
- **消费者**：`FilterMethods__MeetsConditionCriteria.c:254-266`（`0xDD` = `UnitConditions.diedThisTurn = 221`；`0xDE` = `diedSinceLastTurn = 222`；枚举见 `dump.cs:20749`）。
- **`TriggerUnitBacklashActions` 由谁调**：`CardScript__CheckIfDead.c:135`（正常死亡支）与 **`:141`（「变残骸」支）** 两处。

⇒ **「哪一跳 `++`」的答案 = 「反噬入队」那一跳**，而它**早于**「从棋盘移除」（在另一条协程 `_ResolveMinionDeath_d__454` 里）、**更早于**「进墓地」（`UnitDeath` 协程最后一跳）。
⇒ **我们把它留在反噬之前是对的**（`RuleCore.cs:3111-3115`），**一行都不用挪**；账上「若也在进墓地那一跳就得跟着挪」的假设**不成立**。

⚠️ 两处**如实挂着、未判**（不拿猜测填空）：
1. **表示法不同**：我们是一个**计数**（`ctx.DiedThisTurn`，回合开始清零 `RuleCore.cs:718`，消费点 `EffectResolver.cs:5451`），原版是**每张卡一个回合号**。用它的是 3 张真卡（`AM39 Valkyrie` / `DA_Smothering_Decree` / `TL63 Rapacious Hunger`，全池 grep `for each one that dies`）——**「计数」与「按 `turnDied` 数一遍」是否逐位等价没验**（要造一张「被打死又被捞回来」的用例才分得开）。
2. **变残骸那一支**：原版 `CheckIfDead` 的**变残骸**分岔**也调** `TriggerUnitBacklashActions` ⇒ **也会写 `turnDied`** ⇒ 残骸的 `HasDiedThisTurn()` 为**真**；我们那一支**不 `++`**（`RuleCore.cs` 残骸支）。**这是否算偏离：未判**（原版那个谓词用于筛选，我们的计数用于「数几个」——同一格里两种语义不同的用途）。

### `A896` · `UpdateDrag` 的 y 守卫 —— **判据有了，而且比的是指针**

**账上写的**：「没有原版判据，如实挂着」。

**现查实**（`decomp_full/BattleManager__Update.c`，亲读）：

- `:99-124`：每帧 `mouseCanvasPos`（`BattleManager +0x434`，Vector3，`+0x438` = y）← `GetMousePosInWorldSpaceCanvas()`（`Input.mousePosition` → `Camera.ScreenToWorldPoint(…, canvas.planeDistance)`）。
- `:151-157`：**switch 之前**先取 `playerHandPosLimitObj`（`+0x180`）的 `transform.position` 存进 `local_5a8` ⇒ 后面各支里 `local_5a8._4_4_` = **那条线的 y**。
- `:192-195` / `:249` / `:291`：`if (线.y < mouse.y) { … pointerCursorStart.y（+0x324）+ globalVars.<k> < mouse.y … }` ⇒ **两处比的全是指针量，一次都没读「被拖那张卡」的 transform**。
- **那条线是哪个节点**（本轮新查实）：`BattleManager.playerHandPosLimitObj`（`dump.cs`）→ 序列化引用 `解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_4371.json:184-187`（`m_PathID = 141`）→ `…/GameObject/HandLimitArea_141.json`（`m_Name: "HandLimitArea"`）；**13 张战场的全树里都是 `HandLimitArea [-0,779 1x1]`**。

⇒ 「该不该也换指针」的答案：**该换** —— 原版两处判据读的都是指针。

⚠️ **但有一件必须先裁的事**：原版那条 y 判据**守的不是「手牌让位」** —— `DisplayHandWithCardSpace`（让位）在 `case 2` 尾部是**无条件**跑的（`:339-347`），y 判据守的是「**要不要把手牌打到场上**」。⇒ 我们那句守卫**本身在原版没有对应物**（它是我们为了「拖到棋盘上时手牌别乱滑」加的）。所以正确做法是**先裁**：保留（换指针）/ 保留（原样）/ 删掉。
⚠️ **还差**：`HandLimitArea` 的 y=779 与我们 `PlayerLayout.PlayerLineY = 0.3444`（= 708 距顶）的换算**没验**（树 dump 的坐标约定与 canvas 缩放都没核）⇒ 换指针时**别顺手把阈值也换掉**。

### `A899` · 「等动画时长怎么算」 —— **两张表搞混了，判据其实全在本地**

**账上写的**：「`GetCurrentWaitTime.c` 里 `+0x28` 与 `textReference(+0x28)` 位置对不上 ⇒ 留给执行器那批」。

**错因（本轮查实）**：**是两个不同的类** ——
- `ScriptedActionCampaignData`（`dump.cs:89410-89422`）：`stageAction 0x10 / soundAssetName 0x18 / **textReference 0x20** / **waitTime 0x28** / actionType 0x2C`；
- `ScriptedAction`（`dump.cs:42111-42122`）：`actionType 0x10 / sound 0x18 / **waitBefore 0x20** / **waitAfter 0x24** / **textReference 0x28**`。

⇒ `GetCurrentWaitTime` 的 `param_3` 是 **`List<ScriptedActionCampaignData>`**（`dump.cs:24668`），所以 `元素+0x28` = **`waitTime`**，**不是** `textReference`。**"+0x28 对不上"就此解除。**

**原版三条语义（全在本地，逐句读出来）**：

1. **战役脚本** —— `AiScripted__GetCurrentWaitTime.c:14-43`：
   下标 = `AiScripted.actionCampaignCounter (+0x14)`；`index >= Count` 或元素为空 ⇒ **返回 0.0**；
   否则 = `ScriptedActionCampaignData.GetSoundAsset(元素, 声音管理器)。GetDuration()`（**取不到 ⇒ 加 0**）+ **`元素.waitTime`**。
   消费点：`BattleManager._ExecuteCampaignScriptedTurn_d__598__MoveNext.c:73` → `WaitForSecondsRealtime`。
2. **教程脚本** —— `AiScripted__GetDelay.c:14-43`（下标 = `actionCounter (+0x10)`；`delayBefore == false` ⇒ 下标 −1、返回 **`+0x24 waitAfter`**；`== true` ⇒ 返回 **`+0x20 waitBefore`**）：
   **两条前置**：`TurnScriptedData.scriptedActions (+0x20)` 为空/下标越界 ⇒ **0**；`该动作.scriptedActionData (+0x48)` 的 `Count < 1` ⇒ **0**。
   消费点：`BattleManager._ExecuteScriptedTurn_d__597__MoveNext.c:245/:354` → `:260/:365` `SkippableActionWait`。
3. **`SkippableActionWait(action, time)`** —— `AiScripted._SkippableActionWait_d__15__MoveNext.c`：
   `time == 0` ⇒ **立刻返回、一秒不等**（`:25`）；
   `CanSkipAction(action)` 为真 ⇒ **可跳过支**（逐帧累加 `Time.deltaTime` 到 `time`，期间 `AiScripted.skipNextAction (+0x29)` 一置位就当场收工）；
   否则 ⇒ **`WaitForSeconds(time)`**。
   而 `AiScripted__CanSkipAction.c` = `action.scriptedActionData[0].actionType (+0x10)` ∈ **{0x32,0x37,0x3C,0x41,0x5A}** = `PlayerChat(50)/PlayerChatBig(55)/AiChat(60)/AiChatBig(65)/RadioMessage(90)`（`dump.cs:42004+`）——**恰好等于我们 `BattleDriver.IsTutorialChatKind` 那五档**（互相印证）。

**顺带订正**：`BattleDriver.cs:7956` 那句「**教程那份 `AiScripted.GetCurrentWaitTime` 重载同样缺失**」**是错的** —— `GetCurrentWaitTime` 全库**只有一个签名**（`dump.cs:24668`），**落盘的这份就是它**；教程侧走的是 `GetDelay`，**两份都在本地**。

### `A947` · 「能不能动」那两个 trait 号 —— **查实了，但账上的前提一半不成立**

**账上写的**：「原版『能不能动』是**查询时读 trait**；改成查询时现判是**另一件**（那两个 trait 号没查实）」。

**现查实**：`CardScript__CanAttackNow.c` 里只有两处 `HasCurrentTrait` —— `:25` `0x96`、`:27` `0x370`：
- **`0x96` = 150 = `DefinedTrait.cantAttack`**
- **`0x370` = 880 = `DefinedTrait.nonCombatant`**
（枚举全文 `dump.cs:45721-45896`；交叉验证：同一枚举里 `backlash = 120 = 0x78`，正是 `TriggerUnitBacklashActions.c:51` 读的那个；`jam = 130 = 0x82`，正是 `A885` 遗留的「`0x82` 是哪个词条没查清」——**顺手解开了那条尾巴**。）

**但前提「原版是纯查询时」只对一半**（本轮新查实）：
- `CanAttackNow` 的返回值读的是**缓存字段** `CardScript.canAttack (+0x23C)`，而前置读 `CardScript.canAct (+0x230)`（`CanAttackNow.c:57-74`）。
- **`canAct` 的写点是「事件驱动」的**：`CardScript__Awake.c:24` · `ActivateMinion.c:39/68` · **`ActivateTraitsOnSummonOrEnchantment.c`（`0x28`=fast / `0x1CC`=flank ⇒ 只写 `canAct`；`0x4F1`=ferocity / `0x4FB`=oath ⇒ `canAct` **和** `canAttack`）** · `Stun.c:105` · `set_canAct.c`。
⇒ 原版 = **缓存位（`canAct`/`canAttack`）+ 查询时两道禁令（`cantAttack`/`nonCombatant`）**。**我们 `Exhausted`（缓存位，三入口重算 `HasDeployExemption`）+ `CanAttackNow` 里查询时读 `Exhausted/IsStunned/bloodthirst/pindown` 的模型【已经同构】** —— 所以 A947 要做的事**不是**「把 `Exhausted` 改成现算」，而是**只补 `noncombatant` 这一条查询时禁令**（`cantattack` 已在 `RuleCore.cs:2002` 查询时读过）。

**零可观测差异的证据**：`cards_engine.json` 全池 1126 张里 `cantattack` / `noncombatant` **各 0 命中**；卡面授予它们的写法（`can't attack` / `noncombatant`）也 **0 命中**；`KeywordTable` 里**没有** `noncombatant`（`CardDef.cs:1917` 只有 `CantAttack = "cantattack"`，`Implemented` 集 `:2273-2275`）。⇒ 属**结构复原**，别写成「修了一个可见缺陷」。

### `A338` · 全仓「指向我们自己 `.cs` + 行号」的引用 —— **卡组子集仍开；「≈44」那笔账不成立**

见 §4（逐处列出）。结论三条：
1. **本体已收口**（`344 处 / 72 文件`，判「对得上、留」≈300）。
2. 🔴 **「没查清 ≈44 处」这个数【对不上账】**：原报告 §⑤ 的 A 条自己标的是「**22 处**」、B 条「3 处」= **25 个条目**，而 A 条底下逐条列的 `文件:行号` 引用**远多于 22**（一条里常带 4~6 个坐标）。⇒ 「≈44」**没有逐处清单**，**不能当派活依据**；且报告自己在 §⑤·C6 写着「**本件不该再拆**…再派一个写手只能拿到同样的判断，**不建议重开**」。
3. **卡组子集 5 处 + `A895` 5 处**是**可派的**（判据 = 「按符号认」，见 §4 逐处现读）。

### `A962` · 事件广播 ↔ 记账的先后

**① 假账（已收口）—— 三样都在**：
- **实现**：`EffectResolver.cs:4756-4771`（注释逐行写着 2026-10-18 `W5` 把「快照挪到四条非棋盘跳之前、四条非棋盘跳挪到棋盘跳之后」）+ `:4772-4790` 快照 + `:4786-4830` 棋盘跳 + `:4840-4864` 四条非棋盘跳（`BroadcastCostWhen → BroadcastPersistentWhen → BroadcastHandTrapWhen → BroadcastHandWhen`）。
- **原版判据**：`BattleManagerSupport__BroadcastUnitSummoned.c:21`（自己）/`:25-39`（场上每张，带 `IsInPlay` 过滤）/`:44-52`（当前回合方手牌）/`:53-70`（另一方手牌）——**与账上记的 `:21 / :25-39 / :44-64` 逐段吻合**。
- **断言**：`Editor/RuleEngineTest.cs:19493-19512`（「④ 广播次序：**棋盘那一跳在手牌那一跳【之前】**」，含 `:19509` 的前提断与 `:19512` 主断）。

**② 仍开、判据齐（比账上那三行更全）**：
原版 `BattleManager._ResolveDrawCard_d__432__MoveNext.c:274-300` 的逐跳是
**`RemoveCardFromDeck`（`:276`；`BattleManager__RemoveCardFromDeck.c` 体里只有一句 `List.Remove`）→ `BroadcastCardDrawn`（`:283`）→ `BroadcastTrapResolved` / `CemeteryManager.AddDrawTrapAction` → `SetCardTurnDrawn`（`:295`，= 「本回合抽到的」那笔账）**，
而**「进手牌」在更后面**（`PlayerHand._AddDrawnCardToHand_d__37` 那条协程 / `PlayerHand__AddDrawnCardToHand.c`）。
我们 `RuleCore.Draw`（`Core/RuleCore.cs:1135-1148`）：`MoveDeckTopToHand`（出牌库 + 进手牌 + `SetupCardInHand` + 手牌上限）→ `inst.DrawnThisTurn = true` → `BroadcastWhen(Draw, …)`。⇒ **记账在广播之前**，**确是偏离**。

⚠️ **顺手发现（未判）**：原版广播**发生在「已出牌库、还没进手牌」的那一刻**，我们是「**已经进了手牌**才广播」⇒ 手牌那两跳看到的集合不同；后果之一：**被抽出来那张牌自己**会不会把「自己的抽牌」当成 `OtherCardDrawn`（= 触发 `When you draw a card`）——**未判**（原版 `BroadcastCardDrawn.c:64` 那一跳显式跳过 `param_2`，但另一跳 `:40-51` 扫的是当时的手牌，那张牌那时还不在里面）。

### `A963` · `Rubric Marine`（`BL15`）—— **判据缺一格（卡 SO），但「扩散」这一半已可判**

**现状（静态读实）**：`BroadcastHandWhen` 的 self-op 那一支（`EffectResolver.cs:5176-5181`）把「无主语的 `gain`」挂到**那一份手牌实例**上，**并且**传的 `criteria = HandTroopCriteria`（`:2764-2769`，`Kind:"unit"`）⇒ `AttachHandEffect` 尾段（`:2948-2970`）把这条 `entry` 收进 `PlayerState.HandEffectRecords` ⇒ 由 `RuleCore.SetupCardInHand` 的消费者按 criteria 筛**后进手牌**的牌（原版判据链见 `普查产出_1018/DB_手牌效果登记6红.md` §4）。
⇒ **`BL15` 一旦落进这一支，它的 `darkpact(fate)` 会被发给「之后进手牌的每一张部队卡」**。

**原版两条路（本轮新查实的判据）**：
- **路 A（记录 + criteria + 会扩散）**：`AbilityEffect.buffHand = 50`（`dump.cs:20645+`）——`AbilityLogic__PlayAbility.c:1431` 就在 `iVar8 == 0x32`（= 50）那道闸里面，它取 `HandEffect.GetNewInstanceOfThisEffect(ability+0x60)`、把 `acting = param_3+0x10` 写进效果，再 `BattleManager.AddHandEffect(...)` ⇒ `BattleManager__AddHandEffect.c`（建 `BattleAction(0x19)` 入队）→ `ResolveAddHandEffect` → `PlayerHand.AddHandEffect`。
  `PlayerHand__AddHandEffect.c:88-139` = 两步：**`:90-110` 先无条件把记录收进 `activeEffects`（`PlayerHand +0x48`）**，**`:112-139` 再遍历 `currentHand`**，对每张 `CheckIfMeetsCriteria(源卡, 手牌那张, 手牌那张, **该记录.targetCriteria @+0x20**)` 为真的 `CardScript.AddEffect`。**后进手牌的牌也吃**（`PlayerHand__CheckEffectsOnNewCard.c` / `SetupCardInHand.c:53-59`）。
- **路 B（只落自己、**没有记录**）**：`AbilityEffect.buffSelf = 40` ⇒ 同一方法里那句 `BattleManager__AddEffect(bm, /*acting card*/ param_3+0x10, cardEffect, …)` ⇒ **只加到 acting card 那一份 `CardScript` 上**，**不产生任何记录、不会扩散**。
- **触发那一半（两路共用）**：抽牌时 `BattleManagerSupport__BroadcastCardDrawn.c:40-72` 会把**抽牌那一方的整只手牌逐张** `CardScript.CardDrawn(手牌那张, 抽到的卡)` → `RawCardScript.OnCardDrawn`（`CardScript__CardDrawn.c:33`）⇒ **acting card = 手牌里那张牌自己**。⇒ 这一半与我们的形状一致。

**⇒ 缺的那一格**：`BL15` 的 `AbilityEffect` 是 **40** 还是 **50** ⇒ 写在卡的 SO 里，而**本地缺那只 bundle**。
**搜过哪些地方（如实列）**：13 只 `bundle_*cardassets_assets_all` **全部只有 `Sprite/Texture2D/AudioClip` 三个子目录、没有 `MonoBehaviour/`**（`bundle_chaosspacemarinesblacklegioncardassets_assets_all` 里 `Rubric Marine` 只有 `CSM_BlackLegion_inf_Rubric Marine.{json,png}` + `VO_BL_Rubric Marine - swirling dust.ogg`）· 91 只 bundle 里**没有任何 `allcards`/`cards` 类 bundle**（与 `A897` 一致）· `bundle_Waprforge_monoscripts` 只有脚本 · `d:/4/Unity/数据/游戏数据/{card_stats,card_stats_raw,cards,effect_index}.json` 只有 desc/数值/VFX（`effect_index.json` 是特效台账，不是能力表）· `d:/2/解包整理/01_卡牌/卡组数据/MonoBehaviour/`（2210 个）经抽读是**卡包**（`packId`/`cardIds`），**不是卡 SO**。

**⇒ 建议（⛔ 不是裁定，交调度台）**：**止血件**可以现在派 —— 不论原版走 40 还是 50，`criteria = HandTroopCriteria`（= 「手牌里的部队」）**都不是从这张卡的卡面推出来的**，卡面是 `gain`（自指），没有任何「给别人的手牌」的意思；而路 B **根本没有记录**。⇒ 止血口径二选一（**由调度台定**）：把这条 self-op 的 criteria 收成「只匹配它自己」，或**不登记记录**。⛔ 在拿到卡 SO 之前**别写成「照原版」**。

---

## 4. `A338` 的 ≈44 处 + 卡组子集 5 处（逐处）

### 4·A 卡组子集 —— 5 处（**判据齐、可派**；⚠️ 两处「真身」也要订正）

> 源：`资料/普查产出_1017/盘点_卡组编辑部分文档.md:16`。**逐处在今天（`fe239e9`）重读过**。

| # | 引用点（现读） | 它写的是 | 今天的真身（现读） | 判定 |
|---|---|---|---|---|
| 1 | `Shell/PointerLayer.cs:12` | `` `Deck/DeckRuntime.cs:732 HandlePointer()` / `:1379 Hit()` `` | `:732` = `Root = transform; foreach…DestroySafe`；`:1379` = `_cosmoFltBg = Img("cosmoflt_bg"…)`。**真身** = `void HandlePointer()` **`:3057`** · `bool Hit(ImageQuad, Vector3)` **`:5814`** | 🔴 **漂号坐实**（⚠️ **盘点记的「真身 `:1973`/`:2000`」今天也已过期** ⇒ 一律按符号认） |
| 2 | `Shell/MenuScroll.cs:29` | `` `Deck/DeckRuntime.cs:1106-1113` ``（`HandleScroll` 的 `dy * 0.4f`） | `:1106-1113` = 页签名牌那一段。**真身** = `void HandleScroll()` **`:4663`**、`dy * 0.4f` 在 **`:4670`** | 🔴 **漂号坐实** |
| 3 | `Shell/CollectionWindow.cs:1924`（盘点记 `:1777`） | `` `Deck/DeckRuntime.cs:2982` / `:3110` 那两行 ``（同一条「折行按原版逐处实读」判据） | `:2982` = `SetOn(_doneHl, err == …)`（A330 保存闸）· `:3110` = `EndCosmeticDragAt`。**那段判据真正的两行** = `lb.SetWrapping(c.LabelWrap == 1)` 的 **`:5159`** 与 **`:5369`** | 🔴 **漂号坐实**（⚠️ 目标行号是**推定**的：原文没写明是哪两行） |
| 4 | `Editor/DeckScene.cs:2341` | `` `DeckRuntime.cs:1606-1610`，`HandleButtons` 是最后一站 ``（同段还引 `HandleDeckRowClick(:1634)` / `HandleCosmeticClick(:1159)`） | `:1606-1610` = `DrawerHome` / `PrepareDrawerForBuild`。**真身** = `bool HandleButtons(Vector2)` **`:3378`** · `HandleDeckRowClick` **`:3220`** · `HandleCosmeticClick` **`:2161`** | 🔴 **三处全漂** |
| 5 | `Editor/BattleScene.cs:5885` | `` 本仓同一扇主菜单见 `Deck/DeckRuntime.cs:2081` `` | `:2081` = `ApplyCosmCellVisibility(); RefreshCosmeticDrawer();`。**真身** = 返回主菜单那句在 **`:3263-3266`**（`Application.CanStreamedLevelBeLoaded("MainMenu")` + `SceneManager.LoadScene("MainMenu")`） | 🔴 **漂号坐实** |

⚠️ **同族里「顺手扫到、我没判」的 4 处**（都在 `DeckRuntime.cs` 上，**低价值、别当结论用**）：`Battle/BattleDriver.cs:7689` 引 `:364` · `Shell/MainMenuRuntime.cs:251` 引 `:418` · `Editor/DeckScene.cs:3303` 引 `:4218` · `RuleEngine/Editor/RuleEngineTest.cs:19753` 引 `:2978`（**这一处像是「对得上」**：`:2978` 那一段讲的正是「侧栏那两件原版都没有 ⇒ 已删」）。

### 4·B `A895` 一族 —— `Editor/BattleScene.cs` 5 处（**判据齐、可派**）

> 源：`资料/历史/A表批次行_归档_1018.md:35`（B12 报告 §④·2 同）。⇒ **归 `A338` 一件收掉**。
> ⚠️ 报告记的是「我插入 29 行之前的读数」，**今天又要再核一遍**（本代理**没逐处重读**，见 §6）。

`:12597` 引 `BattleScene.cs:14641` · `:12599` 的裸 `:12510`/`:12522` · `:2544`/`:2556`/`:2602` 引 `Hand/CardInteraction.cs:442`（改前真身 454、B12 加注释后 465）。

### 4·C 「没查清 ≈44 处」

🔴 **这一笔账【本身不成立】，无法逐处列出** —— 理由三条：
1. 原报告 `普查产出_1016/A338_行号引用改符号.md:16` 写「没查清 ≈44 处（逐条见 §⑤）」，而 **§⑤ 里 A 条自己标的是「22 处」、B 条「3 处」**（合计 **25 个条目**，且 A 条每条里常带 4~6 个 `文件:行号`）⇒ **两个数不自洽，且没有一份「逐处清单」**。
2. 原报告 §⑤·A 的每一条**都写着「我判不出该指哪」**（目标行号与注释描述对不上、**找不到它真身**）⇒ **这批的判据天然不齐**，派写手只能拿到同样的判断（报告 §⑤·C6 原话：**「本件不该再拆…不建议重开」**）。
3. 卡组子集 5 处与 `A895` 5 处**已从这批里分出去**（4·A / 4·B），是**可派**的那部分。

**§⑤·A 的条目（原样抄，供将来真要做时定位；⛔ 别当派活清单）**：
`Shell/PracticeModePopup.cs:494/502/792/796/853/924/1238` · `Shell/WindowsManager.cs:480-486`、`:875→:892` · `Shell/RewardWindow.cs:1006-1023`、`:222` · `Shell/CampaignRewardWindow.cs:62`、`:246-260`、`:316` · `Shell/MenuDraw.cs:121`、`:241-242`、`:1409`、`:268` · `Shell/MissionsTab.cs:850`、`:1237/1244/1254` · `Shell/LeaderboardRow.cs:222` · `Shell/RankedTab.cs:285`、`:317` · `Shell/CollectionWindow.cs:1834`、`:1788` · `Shell/BattleLogTab.cs:164` · `Shell/DeckInfoPopup.cs:270-274`、`:966` · `Shell/ImportDeckPopup.cs:103` · `Shell/TrophyInfoPopup.cs:202`、`:428` · `Shell/PromptPopup.cs:968` · `Battle/WfSlider.cs:338` · `Battle/Label.cs:549-570` · 5 份自检宿主各自的「`CheckShadeRule` 包装」坐标（`:52/:52/:165/:71/:679`）· `Editor/RewardsScene.cs:1511,1513,1520,2817`、`:1934`、`:2891` · `Editor/BattleScene.cs:2111`、`:4898`、`:6861`、`:3872` · `Editor/MainMenuScene.cs:172-174`、`:2381-2383`、`:2769-2772` · `Editor/CollectionScene.cs:505-513`、`:2435`、`:190` · `Editor/ShopScene.cs:512`、`:874`、`:662` · `Editor/DeckScene.cs` 的 `ForgeTab.cs:572/609 · :658/674` 一族（作者已标「数字已过期」）· `Battle/ScenarioBlendables.cs:935` · `Battle/ImageQuad.cs:163-171` · `Core/CardView.cs:121` · `Core/Badges.cs:47-51` · `Shell/WindowsManager.cs:381` · `Editor/SettingsScene.cs:2473`。
**§⑤·B（工程外路径，按规不动）3 处**：`Deck/DeckRuntime.cs` 与 `Battle/PromptPopup` 引 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs:NNN` · `Shell/MainMenuRuntime.cs:587` / `WindowsManager.cs:1086` 同族（**扫描脚本的假阳性**）。
**§⑤·C（顺手发现，原报告已提，仍是开的）**：诊断消息字符串里另有 **8 处** `.cs:NNN` 引用（`CombatCameraZoom.cs:678` 等）；`WF_DOC=1` 的 **CS1570 剩 4 条/2 处**在**别人未提交块**里（`Shell/ViewportClip.cs:245-246` · `Battle/Label.cs:1393`）。

---

## 5. 顺手发现（**都没改**）

| # | 发现 | 出处 | 置信度 |
|---|---|---|---|
| 1 | 🔴 **「`0x82` 是哪个词条」那条尾巴解开了** —— `0x82` = 130 = **`DefinedTrait.jam`**；同表还验出 `0x78` = 120 = `backlash`（正是 `TriggerUnitBacklashActions.c:51` 读的那个） | `dump.cs:45721-45896`（`DefinedTrait` 全表）· `CardScript__TriggerUnitBacklashActions.c:51` | **高** |
| 2 | 🔴 **`A963` 的同族**：`canAct` 的写点里 **`0x4FB` = 1275 = `oath`** 也会 `canAct = true` **且** `canAttack = **false**`　🔴 **2026-10-18 第三会话就地订正（铁律 5）**：本行原来写「且 `canAttack = true`」—— **那是错的**。现读 `CardScript__ActivateTraitsOnSummonOrEnchantment.c` 的 `:91` / `:96`（`op_Implicit(local_30, 0, 0)`）= `canAct(+0x230) = 真` · `canAttack(+0x23C) = 假`，且写法与 `ferocity`(`0x4F1`)**逐字节同形**；配套判据 = `EntityScript__get_displaySummonSickness.c`（`+0x58 && !fast && !flank`）+ `ActivateMinion.c:39-42` ⇒ **`canAttack` 的语义 = 「本回合上的场，且不带 `fast`/`flank`」** ⇒ `ferocity`/`oath` **只免「能动」、不免「不能攻击」**。 —— 而我们的 `HasDeployExemption`（`RuleCore.cs:1354`）**三词表只收 `fast`/`flank`/`ferocity`，没有 `oath`** | `CardScript__ActivateTraitsOnSummonOrEnchantment.c`（`0x28`/`0x1CC` vs `0x4F1`/`0x4FB`）· `RuleCore.cs:1354-1357` | **中**（**机制坐实**；「`oath` 该不该并进豁免」**未判** —— `oath` 是我们引擎里另一条独立机制） |
| 3 | **`A947` 的同族**：原版「能不能攻击」**还叠了两道非 trait 的闸** —— `BattleManager +0x244`/`+0x246`（回合方）与 `UIstate ∈ {1,6}`（`CanAttackNow.c:30-56`）；我们那边只有 `p != ctx.Active`（`RuleCore.cs:2075`） | `CardScript__CanAttackNow.c:29-56` | **中** |
| 4 | ⚠️ **界面的「手牌上限线」原版是个真节点**：`HandLimitArea`（13 场全树都是 `[-0,779 1x1]`），我们**没有这个节点**，用的是常量 `PlayerLineY = 0.3444`（= 708 距顶） | `07_场景/battlearena1/GameObject/HandLimitArea_141.json` · `资料/说明书/01_战斗_对战/2D层_battlearena*全树.md` · `Editor/BattleScene.cs:122` | **高**（节点存在）/ **未查**（两者换算） |
| 5 | ⚠️ **`A962` ② 的连带**：原版「抽牌」的三件事发生在「**已出牌库、还没进手牌**」那一刻，我们是「**进了手牌**才广播」⇒ 手牌监听看到的集合不同（被抽那张会不会把自己的抽牌当 `OtherCardDrawn`） | `BattleManager._ResolveDrawCard_d__432__MoveNext.c:276-300` · `PlayerHand._AddDrawnCardToHand_d__37` | **中**（未判） |
| 6 | ⚠️ **`A338` 的卡组子集，盘点记的「真身」也已过期**（`HandlePointer` 记 `:1973`，今天 `:3057`）⇒ 这一类引用**连「订正后的行号」都不能信**，只能按符号认 | §4·A 逐处 | **高** |

---

## 6. 没查清的部分（⛔ 不拿猜测填空）

| # | 还差什么 | 卡在哪 |
|---|---|---|
| 1 | `A963`：`BL15` 卡 SO 里的 **`AbilityEffect` 是 40（`buffSelf`）还是 50（`buffHand`）** | **本地缺 `allcards_assets_all.bundle`**（`A897`）；**已列搜过的地方**（§3·A963）⇒ 判据在**远端 CCD / 缺的那只包** |
| 2 | `A858`：(a) 「我们一个计数」与「原版每卡一个 `turnDied` + 逐张数」**是否逐位等价**；(b) **「变残骸」那一支原版也写 `turnDied`** 而我们不 `++` ⇒ 是否偏离 | 要造用例：被打死又被捞回 / 翻成残骸后再数一次；`A858` 的 3 张真卡（`AM39`/`SMOTHERING`/`TL63`）**要跑对局才分得开**（本代理不许跑 Unity） |
| 3 | `A896`：`HandLimitArea` 的 **y=779** 与我们的 `PlayerLineY = 0.3444`（708 距顶）**换算关系** | 树 dump 的坐标约定 + canvas 缩放**都没核**；且原版比的是 `transform.position`（世界）与 `mouseCanvasPos`（canvas），**两者为什么能直接比**没查 |
| 4 | `A895` 的 5 处 | 报告给的读数在**插入 29 行之前**，本代理**没逐处重读**（`Editor/BattleScene.cs` 正在被别的写手改） |
| 5 | §4·C 那 4 处「顺手扫到」的 `DeckRuntime.cs` 引用 | 未逐处判（低价值；其中 `RuleEngineTest.cs:19753` 那一处**像是**对得上） |
| 6 | `A962` ② 的连带（§5·5） | 未判：要不要「先广播、后进手牌」一起改（改它会影响手牌那两跳看到的集合） |
| 7 | `A899`：`SkippableActionWait` 的「可跳过支」**那条链在界面上的落点**（那个 `+0x48` 旗标挂在谁身上） | `AiScripted._SkippableActionWait_d__15__MoveNext.c:40-48` 取的是 `FUN_1805e38d0(0)` 的 `+0x228`（一个 UI 对象），**没反查到它是什么**；⚠️ 而且我们**根本没有「演出等待可被点击跳过」这一层** |

---

## 7. 系统性毛病自查（三条）

- **断言自证 / 同义反复**：本轮**没写断言**（只读代理）。针对「要派的那几条」点名提醒 ——
  · `A962` ② 要**判别式**（挡「把 `DrawnThisTurn` 与广播两边一起改回旧写法还全绿」）；
  · `A858` 若加断言，判据必须取**原版语义**（「反噬期间它已算死者、`DeadUnits` 还没算」），⛔ 不是复述我们的常量；
  · `A947` 补 `noncombatant` 时，**全池零张带它** ⇒ 必须**造夹具**（`give it Non-Combatant` 那种），否则「什么都没发生」也能全绿（**这条正是「靠什么都没发生的假断言」**）。
- **弱断言分不出两种状态**：`A896` 那条若只断「指针在左 ⇒ 插槽是左」会被卡追上指针之后掩盖 ⇒ 必须**喂 `dt = 0`**（B12 已用过这条手法，可照抄）。
- **靠什么都没发生的假断言**：见上 `A947`；另 `A899` 若要断「可跳过」，前提得先断「这一条真是聊天族」（`CanSkipAction` 那 5 档），否则恒为「跳过支」或「常支」之一。
