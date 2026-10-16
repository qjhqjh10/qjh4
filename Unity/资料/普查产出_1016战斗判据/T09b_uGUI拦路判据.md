# T09b · uGUI 拦路判据（谁压谁 · UIstate 15 档）

> **查证人**：只读判据代理 · **日期** 2026-10-16 · **范围** 只读，未改任何 `.cs`/`.unity`/`.json`/正本。
> **数据源**（arena2，与 arena1 逐条比对一致）：
> `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/{Canvas,MonoBehaviour,GameObject,RectTransform}/`
> · `d:/2/新解包资源/assets_full/globalgamemanagers/TagManager/TagManager_3.json`
> · `d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/TagManager.asset`（同一份数据，YAML 版）
> · `d:/2/tools/decomp_full/BattleManager__Update.c` · `d:/2/tools/il2cpp_out/dump.cs`
> **⚠️ 跨场一致性已核**：`battlearena1` 与 `battlearena2` 的 8 个宿主逐条 `(renderMode, overrideSorting, layerID, order)` **完全相同**（脚本比对，7/7 SAME）。

---

## 一、条 A：8 个 GraphicRaycaster 的排序与兄弟序

### A·0 三条前置事实（先定死机制，再看值）

1. **全场景 `GraphicRaycaster` 恰好 8 个**（判据 = `MonoBehaviour/*.json` 里带 `m_BlockingObjects` 的实例，命中 **8** 个：pid 4256 / 4398 / 4589 / 4624 / 4841 / 4895 / 5058 / 5065）。宿主名与正本 §2·4 列的那 8 个**逐字吻合**。
2. **8 个 GR 自身的字段全部一模一样**：`m_IgnoreReversedGraphics=1` · `m_BlockingObjects=0`（None）· `m_BlockingMask.m_Bits=32`（= layer 5 `UI`）· `priority=0`（⚠️ 非标准 uGUI 字段，本 build 才有的，全 0）。
   ⇒ **「谁压谁」跟 GR 组件无关，完全由它挂的那个 `Canvas` 的排序三件套决定。**
3. **排序三件套 = `(m_SortingLayerID, m_SortingOrder, 兄弟序)`**。uGUI 先比**排序层**（层有全局先后），层内比 `m_SortingOrder`，再同则比**层级渲染序**（兄弟先后）。**`overrideSorting=false` 的嵌套 Canvas 不另立排序组，随父 Canvas 按兄弟序渲染；`overrideSorting=true` 的才另立一组、用自己的 `(layer, order)`。**

### A·1 排序层表（**从 `TagManager` 实读；层先后即渲染先后，下→上**）

| 序 | 层名 | `uniqueID`（= `m_SortingLayerID`） |
|---|---|---|
| 1 | `Default` | `0` |
| 2 | `Background` | `1954195551` |
| 3 | `Elements` | `229362899` |
| 4 | `OverElements` | `952166135` |
| 5 | `Hand` | `2099470215` |
| 6 | `HandMoving` | `887547619` |
| 7 | `OverHand` | `2007638115` |
| 8 | `PopUps` | `1054366423` |
| 9 | `Blocking` | `2286135553` |

出处：`globalgamemanagers/TagManager/TagManager_3.json` 的 `m_SortingLayers`（顺序即此表顺序）。
⚠️ **负 layerID 也在这张表里**：`-2008831743` 按无符号读 = `4294967296 − 2008831743 = 2286135553` = **`Blocking`**（`TooltipManager` / `LoadingMenu` 这两件用它）。

### A·2 8 个宿主逐条（**这是本判据的正主体**）

> 列义：`宿主名` / `Canvas 组件文件` / `renderMode` / `overrideSorting` / `排序层（名）` / `sortingOrder` / `父链` / `兄弟序`。
> `renderMode`：`1=ScreenSpaceCamera` · `2=ScreenSpaceOverlay`（⚠️ 见 A·3 的读法说明）。

| # | 宿主名 | Canvas 文件 | rm | override | 排序层 | order | 父链（→ 上溯） | 兄弟序 |
|---|---|---|---|---|---|---|---|---|
| 1 | **Canvas**（根） | `Canvas/Canvas_2765.json` | 1 | **false** | `Default` (0) | 0 | （根，无父） | — |
| 2 | **BackCanvas** | `Canvas/Canvas_2768.json` | 2 | **false** | `Default` (0) | 0 | `Canvas` | 2 |
| 3 | **FrontCanvas** | `Canvas/Canvas_2762.json` | 1 | **true** | `OverHand` (2007638115) | 0 | `Canvas` | 3 |
| 4 | **BattleDoors/Canvas** | `Canvas/Canvas_2767.json` | 2 | **true** | `PopUps` (1054366423) | 0 | `Canvas < BattleDoors` | 0 |
| 5 | **Drag Attack Selector** | `Canvas/Canvas_2759.json` | 2 | **false** | `Default` (0) | 0 | `Canvas < FrontCanvas < Safe area FrontCanvas` | 0 |
| 6 | **PopupAnchor** | `Canvas/Canvas_2776.json` | 2 | **true** | `PopUps` (1054366423) | **1** | `Canvas < FrontCanvas < Safe area FrontCanvas` | 9 |
| 7 | **CemeteryLogPanel** | `Canvas/Canvas_2772.json` | 2 | **true** | `PopUps` (1054366423) | **−1** | `Canvas < BackCanvas < Safe area BackCanvas < LeftArea` | 3 |
| 8 | **BattleSettingsPanel** | `Canvas/Canvas_2763.json` | 2 | **true** | `PopUps` (1054366423) | 0 | `Canvas < FrontCanvas < Safe area FrontCanvas` | 10 |

**⇒ 由这两列直接读出的赢家（同点重叠时谁收事件）：**
`PopUps 层 > OverHand 层 > Default 层`。在 `PopUps` 层内：`PopupAnchor(1) > BattleSettingsPanel(0) = BattleDoors/Canvas(0) > CemeteryLogPanel(−1)`。
HUD 主层（`FrontCanvas`，OverHand/0）**高于**根层的一切（Default/0，含 `BackCanvas` 与 `Drag Attack Selector`）。设置窗/弹窗锚点/战报窗都在 `PopUps` ⇒ **一定盖在 HUD 之上**。

### A·3 兄弟序明细（同层同 order 时靠它分先后）

- **根 Canvas 的直接子件序**（`RectTransform_2952.json` 的 `m_Children`，从左到右）：
  `[0] Aspect Ratio Filler Top` · `[1] Aspect Ratio Filler Bottom` · `[2] BackCanvas` · `[3] FrontCanvas` · `[4] BattleDoors` · `[5] Tutorial highlight` · `[6] Anim Anchors` · `[7] UI Error Message Controller (MUST BE ENABLED)`
- **`Safe area FrontCanvas` 的子件序**（`RectTransform_3055`）：
  `[0] Drag Attack Selector` · `[1] ActiveSkillDesc` · `[2] Unit Chat` · `[3] Mulligan` · `[4] Generic Multi Card Display Combat` · `[5] ChooseCardMenu` · `[6] Card Display Window` · `[7] DebugButtons` · `[8] AboveShader` · `[9] PopupAnchor` · `[10] BattleSettingsPanel` · `[11] Tutorial` · `[12] LoadingMenu` · `[13] Alliance Panel`
- **`LeftArea` 的子件序**（`RectTransform_3042`）：`[0] CardsPlayedInTurnHolder` · `[1] EnemyInfo` · `[2] PlayerInfo` · `[3] CemeteryLogPanel` · `[4] CardDrawPositions` · `[5] Left Anchor`
- 出处：各 `RectTransform/RectTransform_<pid>.json` 的 `m_Children` / `m_Father`（脚本解父链）。

### A·4 附带（同场景另 11 个 Canvas，**无 GR、控的是渲染不是输入**，供分层参考）

| 宿主 | 排序层 | order | 父 |
|---|---|---|---|
| Cardback Container (×2) | `Background` 1954195551 | 0 | PlayerDeck / EnemyDeck |
| Player Deck Size Container (×2) | `Background` 1954195551 | 1 | PlayerDeck / EnemyDeck |
| TooltipManager | **`Blocking`** (2286135553) | 0 | FrontCanvas |
| LoadingMenu | **`Blocking`** (2286135553) | 1000 | Safe area FrontCanvas |
| Tutorial | `HandMoving` 887547619 | 1500 | Safe area FrontCanvas |
| Tutorial highlight | `HandMoving` 887547619 | 601 | Canvas |
| TutorialPointerCombat | `OverHand` 2007638115 | 10 | Tutorial |
| UI Error Message Controller | `PopUps` 1054366423 | 1 | Canvas |
| BattleDoors | `Default` 0 | 0 | Canvas |
| Cardback Container/Deck Size (各 1) | 见 arena 明细 | — | — |

⚠️ **`renderMode` 读法**：序列化里只有根 Canvas 与 FrontCanvas 是 `1`（ScreenSpaceCamera），其余 17 个读成 `2`。**Unity 的嵌套 Canvas 运行时继承根 Canvas 的渲染模式**（嵌套时组件里的 `m_RenderMode` 不参与），所以**实际有效模式 = 全部 ScreenSpaceCamera**。此条是 Unity 引擎行为，**本次未跑原版实况验证**（见 §四）。

---

## 二、条 B：`UIstate` 15 档（逐个：值 / 名 / 含义 / 谁处理 / 怎么迁移）

### B·0 事实底座（先钉死）

- **真名与取值**：`enum UIstateOptions`（**非 `[Flags]`**，普通枚举），`dump.cs:46314-46332`（`TypeDefIndex: 971`）。字段本体 = `BattleManager` 的 `UIstate` 属性，**存在 `+0x290`**（`BattleManager__get_UIstate.c` / `__set_UIstate.c`：就是读写 `*(param_1+0x290)`）。
- **每帧的驱动器** = `BattleManager.Update`：**每帧恰好一次射线取「指针下的对象」**（`GetMousePosInWorldSpaceCanvas` → y 加 `+0x40` 偏移 → `GetTargetInPos` → 存进 `+0x440` = 下文 `*plVar25`），**然后 `switch(UIstate)` 决定拿这个对象做什么**（`BattleManager__Update.c:123-159`）。
- **`switch` 只处理 10 档**（`:159` 的 `switch`），**5 档一条输入都不处理**：`0 / 7 / 11 / 12 / 14`。
- **迁移全靠「谁写完这一帧」**：状态转移 = **别处的方法里直接写 `+0x290`**（setter 没有具名调用者，都是内联写）。全集见 B·2。

### B·1 逐档表（**15 档**）

| 值 | 枚举名 | 含义（照名字 + 代码位置） | 谁处理这一帧的输入 | 出处 |
|---|---|---|---|---|
| **0** | `inactive` | 未进行 / 无交互 | **不处理** | `Update.c` 无 case 0 |
| **1** | `normalBattle` | 普通对局，等玩家指卡/指钮 | **`Update` case 1 只做 AI 脚本检查**；真正的点击由**各卡/各钮自己的 uGUI 指针 handler** 收（门槛见下「门」） | `Update.c:160-190` |
| **2** | `movingHandCard` | 手牌被拖起、跟着指针 | `case 2`：拖动/换位；松手 → 回手牌，或 `MoveIntoFieldFromHand` 后**按卡型分流**到 3/4/5 | `Update.c:191-349`（转移点 `:264/:270/:280`） |
| **3** | `playingMinion` | 单位正被放到格位 | `case 3`：算最近可落格、放不成就退回手牌；放成 → 回 1（`ReassembleMinions` 后 `:407`） | `Update.c:350-625`（`:407` 置 1；`:621` 置 2） |
| **4** | `playingTargetedSpell` | 指定目标法术选目标中 | `case 4`：十字准星跟指针；对有效目标点下 → 入队法术动作；点空 → 回 2（`:828`） | `Update.c:626-830` |
| **5** | `playingNonTargetSpell` | 非指定目标法术打出中 | `case 5`：组法术动作、`AddAutoActionToQueue`；卡离手 → 回 2 | `Update.c:831-985` |
| **6** | `minionAttacking` | 单位正选攻击目标 | `case 6`：准星跟指针；点有效目标 → `AddAttackAction`/`ExecuteActiveAbility`；否则 `CancelAttack` → 回 1（`:1095`） | `Update.c:986-1096` |
| **7** | `finishBattleSequence` | 终局演出序列播放中 | **不处理**（演出由协程跑） | `Update.c` 无 case 7 |
| **8** | `postBattle` | 终局后（等离开） | `case 8`：**左键 Down 或 ESC → `LeaveBattle`** | `Update.c:1097-1102` |
| **9** | `choosingUnitTarget` | 选一个单位目标（效果） | `case 9`：落点存 `+0x338`；准星跟指针 | `Update.c:1103-1185`（与 13 同 case） |
| **10** | `targetedActiveAbility` | 指定目标主动技能选目标中 | `case 10`：点下 → 回 1、`CancelAbilityTargeting` | `Update.c:1186-1200` |
| **11** | `mulligan` | 换牌阶段 | **不处理**（换牌面板自己收输入） | `Update.c` 无 case 11 |
| **12** | `mulliganFinished` | 换牌收尾 | **不处理** | `Update.c` 无 case 12 |
| **13** | `choosingAbilityTarget` | 主动技能选目标中 | `case 0xd`：点下 → `FinishedChoosingAbilityTargetByClick` | `Update.c:1104-1185` |
| **14** | `choosingResolutionCard` | 选「结算用」的卡（弹窗） | **不处理**（该面板接管且**全局禁点**） | `Update.c` 无 case 14 |

### B·2 迁移图（**谁把它设成下一档**；出处 = 各 `BattleManager__*.c` 的 `*(...+0x290) = N`）

| 目标档 | 谁写它（出处） |
|---|---|
| **1 normalBattle** | `EndTurnClick.c:66` · `ClickChosenCardDone.c:25 / :105` · `_ResolvePlayCardFromHand…MoveNext.c:1333` · `_StartBattlePhase…MoveNext.c:165` · `Update.c:407 / :1095 / :1200` |
| **2 movingHandCard** | `StartMovingCardInHand.c:8` · `Update.c:621 / :828` |
| **3 playingMinion** | `Update.c:264`（**仅当** `cardType==0` 单位） |
| **4 playingTargetedSpell** | `Update.c:280`（法术 `targeted` 为真） |
| **5 playingNonTargetSpell** | `Update.c:270`（法术 `targeted` 为假） |
| **6 minionAttacking** | `StartAttackFrom.c:30` |
| **7 finishBattleSequence** | `FailedToReconnectAfterDisconnect.c:63` · `DeadHero.c:164` · `CancelMatchWithoutResult.c:21` |
| **8 postBattle** | `FinishedLeavingBattle.c:40` · `_CompleteBattleFinished…MoveNext.c:39` |
| **9 choosingUnitTarget** | `_ResolvePlayCardFromHand…MoveNext.c:389` |
| **10 targetedActiveAbility** | `StartTargetedActiveAbility.c:31` |
| **11 mulligan** | `_SetupMulliganPhase…MoveNext.c:29` |
| **12 mulliganFinished** | `ClickChosenCardDone.c:40` · `_SetupEnviromentalEffectPhase…MoveNext.c:129` |
| **13 choosingAbilityTarget** | `StartChoosingAbilityTargetByClick.c:15` |
| **14 choosingResolutionCard** | `ChoiceOfCardPlayer.c:20` |
| **0 inactive** | （未找到写入点 —— 见 §四） |

### B·3 「门」：别的类怎么用 `UIstate` 决定「这一帧我能不能动」

`UIstate` 不只被 `Update` 读，还被**各个交互件**读来给自己上锁：
- **卡能不能被攻击起手** `CardScript.CanAttackNow`：只允许 `UIstate ∈ {1 normalBattle, 6 minionAttacking}`（`CardScript__CanAttackNow.c:50-56`；另有 `card+0x40` 自己那道闸）。
- **卡能不能用技能** `CardScript.CanUseAbilityNow`：只允许 `UIstate ∈ {1, 10 targetedActiveAbility, 6, 13 choosingAbilityTarget}`（`CardScript__CanUseAbilityNow.c:73-87`）。
- **时钟** `ClockManager.Update`：`UIstate == 8 postBattle` ⇒ 直接返回、表停（`ClockManager__Update.c:32-34`）。

⇒ **模式总结**：`UIstate` = **「当前处于哪种战斗交互阶段」的单一根状态**；阶段决定 (a) `Update` 拿指针下的对象做什么、(b) 各卡/各钮自己收不收点击。

---

## 三、这两条对我们「15 处 `if…return` 链」与「建 Canvas 的层级」的直接结论

### C·1 条 A 的落地（建 Canvas 的层级）——**照原版是「1 根 + 若干个 `overrideSorting` 的子 Canvas」**

- **根 Canvas** 只有 1 个 `CanvasScaler`（**全场景唯一**，`MonoBehaviour_4940.json`，在 GO `Canvas`(1006) 上；`m_UiScaleMode=1` / `1920×1080` / `m_ScreenMatchMode=1`(Expand) / `ReferencePixelsPerUnit=100`）。⇒ **我们建根 Canvas 时也只在根上放 1 个 `CanvasScaler`，子 Canvas 一律不加 `CanvasScaler`**（加了会二次缩放）。
- **根 Canvas**：`m_RenderMode=1`(ScreenSpaceCamera) · `m_Camera`=hud 相机 · `m_PlaneDistance=100` · `m_SortingOrder=0` · `m_OverrideSorting=false`（`Canvas_2765.json`，与正本 §T09「§五·补②」记的 `Canvas_2762` 那份是**另一个宿主**——2762 是 `FrontCanvas`，2765 才是根；两者字段相近，别混）。
- **要压谁，靠两条**：(1) **排序层**（上表 9 层，`Default < … < OverHand < PopUps < Blocking`）；(2) `sortingOrder` + 兄弟序。⇒ 建层级时**给每族件选对「层 + order」**：
  - HUD 主层件（名牌 / 能量 / 时钟 / 牌堆 / 手牌）→ 至少要在 **`OverHand`**（= 原版 `FrontCanvas`）；
  - **设置窗 / 弹窗锚点 / 战报窗** → **`PopUps`**（原版这 4 件全在 PopUps，order −1/0/1）；
  - **tooltip / loading** → **`Blocking`**（最顶）。
  ⚠️ **实现选择**：本工程若要**逐位对齐**，得在 `ProjectSettings/TagManager.asset` 里**建出同名的 9 个排序层**再按表设值；也可退而用「单层 + 纯 `sortingOrder` + 兄弟序」，但那**不是逐位复刻**。判据以本表为准。
- **输入优先 = 渲染优先**：EventSystem 就是按 Canvas 排序挑赢家 ⇒ 上面的赢家序**同时是输入序**。这正对治普查 §二那条「面板被底板盖住 / 同一按触发两次」。

### C·2 条 B 的落地（我们那 15 处 `if…return` 链）——**别 1:1 搬成 switch；它其实是两种东西混在一起**

原版**没有**我们这种「一串模态 `if` 挡在前面」的写法，它把交互分给**两条独立通道**：

- **通道 ①「面板/弹窗优先级」= uGUI EventSystem + Canvas 排序**（= 条 A）。
  原版**设置窗 / 聊天 / 战报 / 联盟 / 回放条**这些**根本不在 `UIstate` 里** —— 它们是挂在 `PopUps` 层的普通 uGUI 件，谁在上谁收事件。我们链里这些分支（`HandleSettings` · `HandleChatPopup` · `HandleAlliancePanel` · `HandleBattleLog` · `HandleReplayBar` · `HandleCameraResetButton` · `HandleOffensiveButton` · `TickMultiCards` · `TickCardDisplayClick`）**换 uGUI 后就该由 EventSystem + Canvas 排序接管，不需要手写顺序**。
- **通道 ②「战斗交互阶段」= 一根 `UIstate` int**（= 条 B）。
  原版这里只覆盖**拖牌 / 放兵 / 指定法术选目标 / 非指定法术 / 攻击 / 选单位目标 / 主动技能选目标 / 换牌 / 选结算卡 / 终局**这些**「进行中的战斗动作」**。
  我们链里对应的是：`HandleMulligan`（→ 原版 11/12）· `HandleChoose`（→ 原版 14）· `DrivePlayerTurn` 内部那条拖拽/落位/攻击状态（→ 原版 2/3/4/5/6/9/10/13）· `Ctx.IsOver` / `HandleEndBattleExit`（→ 原版 8 postBattle / 7）。

**⇒ 正确做法（建议，供 T09 第 4/5 步定序用）：**
1. **不要**把 15 处 `if…return` 直接翻译成一个 `switch`。**先按上面两条通道拆开**：
   - 面板族 → 交给 uGUI 件 + Canvas 排序（条 A）；退掉手搓命中的同时**退掉这些 `if` 分支**。
   - 战斗交互族 → 建一根 `UIstate`-式 int（15 档照抄 B·1），`Update` 只保留 —— **一次射线取「指针下对象」→ `switch(UIstate)`**。
2. **「谁收输入」的判定口要收敛成一处**（B·3）：原版是各卡自己读 `UIstate` 上锁；我们换 uGUI 后应让**卡的 `IPointerClickHandler` 自己读那根状态**，而不是继续在 `BattleDriver.Update` 里替所有卡判断。
3. **换牌 / 选牌这两档本来就在 `UIstate` 里**（11/12/14）—— 说明它们**不是**「普通弹窗」，换 uGUI 后仍应受那根状态门控（别只当面板处理）。

---

## 四、还差什么 / 判不准的（⛔ 不猜，写清搜过哪些词 / 哪些路径）

1. **`renderMode` 的运行时有效值没跑实况**：本报告说「嵌套 Canvas 继承根的 ScreenSpaceCamera」是 **Unity 引擎行为**，**未在 `d:/2/unity_run_ref/` 跑原版 dump UItree 证**。要坐实得跑一次原版、dump 那 8 个 Canvas 的运行时 `renderMode`。（**搜过的路径**：只读了 `Canvas_*.json` 的序列化字段。）
2. **`UIstate = 0 inactive` 的写入点没找到**：`BattleManager` 全部方法里写 `+0x290` 的只有 B·2 那些；`+0x290)` 的内联写在别的类里**搜不全**（`+0x290` 是很多类共用的偏移，`grep "0x290) ="` 会命中 `NetworkingPeer` / `CardScript` / `Reporter` 等**同名偏移的不同字段**，无法用它作判据）。**⇒ 报「未找到写入点」**，没拿别的类当答案。（**搜过的词**：`0x290) = ` · `set_UIstate` · `UIstate`，路径 `d:/2/tools/decomp_full/` 全体。）
3. **各档「逐帧的完整动作序列」未逐行展开**：本报告给了**功能级**描述 + 关键调用点行号，`case 3/4/5` 里那些十几行一块的结构体拷贝（`.c` 里 `puVar18/puVar23` 那几段）是**编译器展开的入队**，未逐字节还原其字段义。**⇒ 需要更细时按 `Update.c` 行号再读。**
4. **`UIstate` 与 `PhotonNetwork`/联机的关系未展开**：`case 2/3` 里有 `IsNetworkedGame` / `SendCardReorder` 分支，联机那侧的换牌定序是否也走 `UIstate`，未查。
5. **8 个宿主之外，那 11 个「无 GR 的 Canvas」是否参与输入未判**：它们没有 `GraphicRaycaster`，但**是否靠祖先（根 Canvas / FrontCanvas）的 GR 被射线到**，未查（`GraphicRaycaster` 的子树收集边界本次没验证）。
（**条 A 搜过的词**：`m_BlockingObjects` · `m_IgnoreReversedGraphics` · `m_ReferenceResolution` · `GraphicRaycaster` · `m_SortingLayerID` · `m_SortingOrder` · `m_OverrideSorting` · `m_Children` · `m_Father`；路径 `bundle_scenes_scenes_battlearena1|2/` 的 `Canvas/` `MonoBehaviour/` `GameObject/` `RectTransform/`，加 `globalgamemanagers/TagManager/` 与 `assetripper_out/…/TagManager.asset`。）

---

## 五、顺手发现的新问题（铁律 5·b）

1. 🆕 **全场景只有 1 个 `CanvasScaler`，在根 Canvas 上**（`MonoBehaviour_4940.json`）—— 11 个嵌套 Canvas **一个都没有**。⇒ **T09 第 2 步「建 Canvas」时，`CanvasScaler` 只能挂根、子 Canvas 一个都不许加**（否则二次缩放）。普查 §四 第 2 步没写这条，建议补进「必同批避坑清单」。
2. 🆕 **GR 组件有个非标准字段 `priority`（全 0）** —— 标准 uGUI `GraphicRaycaster` 没这个字段。说明原版 build 的 ugui 版本/定制与我工程 `com.unity.ugui@27635d171b1a` 不同。⚠️ **对 T09 的影响**：`d:/4` 工程用标准 GR 即可（`priority` 是死值），但**别拿「字段名对不上」当「资源不对」**（本仓已多次踩过）。
3. 🆕 **正本「§五·补②」里引的 `Canvas_2762` 是 `FrontCanvas`、不是根 Canvas**：根 Canvas 是 `Canvas_2765.json`（GO 1006）。两者字段相近（都 `rm=1` / `plane=100` / `camera=1559`），**极易再次引错**——建议正本把「根 Canvas 的出处」改成 `Canvas_2765.json`（本条只记，未改正本）。
4. 🆕 **`BattleDoors` 的 Canvas（PopUps/0）与 `BattleSettingsPanel`（PopUps/0）同层同序** ⇒ 谁在上面**只由兄弟序定**；我们若把「开门」与「设置」都放同一 order，必须显式排兄弟序，否则**另一个宽高比下可能翻转**。
5. 🆕 **`CemeteryLogPanel` 挂 `PopUps` 而它父链在 `BackCanvas` 下**（`BackCanvas < Safe area BackCanvas < LeftArea < CemeteryLogPanel`）：`BackCanvas` 自身 `overrideSorting=false`（在 Default 层），但它子孙里的 `CemeteryLogPanel` **`overrideSorting=true` 跳到 PopUps**。⇒ **证明「子件能靠 overrideSorting 脱离父层的排序」**；我们分层时**别按父链推层，要按每个 Canvas 自己的三件套读**。
