# AUD8 · 一张卡的「被玩家使用」路径：入口 / 中途 / 取消 / 失败反馈

> 只读审查 · 2026-10-25 · 第三块（**不含**攻击命中与准星表现 = AU3；**不含**关键词词条 = AUD7）。
> 路径：`Battle/` = `D:/4/Unity/MyGame/Assets/CardPresentation/Battle/`；`Hand/` = `…/CardPresentation/Hand/`；`Core/` = `D:/4/Unity/MyGame/Assets/RuleEngine/Core/`。
> 判据优先级：`d:/2/tools/decomp_full/`（下称 `dc/`）> `d:/2/新解包资源/assets_full/` > 成品卡图。⛔ 粉丝规则书 / `rule_core.gd` 不作判据。
> 运行时：`_tmp_view/player/real_shell_1025.log` · `_tmp_view/shots/2026-10-25_第七十五会话/` · `_tmp_view/click_log.txt`（10-25 段 = 第 11684 行起，Battle 段 48 次点击）。

## 一、主表

| # | 路径（玩家想干什么） | 入口（点/拖/右键/快捷键） | 中途每一步 | 取消路径 | 失败/非法时怎么告知 | 引擎侧落点(file:line) | 与原版差异 | 修法 |
|---|---|---|---|---|---|---|---|---|
| 1 | **打出单位卡** | 左键按住手牌卡 → 拖过 10px（`TouchInput` 关 = 按下即抓） | 卡跟手放大 1.1 → 手牌让位（喂**指针**给 `GetClosestInHandSlot`）→ 只点亮**它真会去的那一格**＋让位预览 → 松手 → 0.65 s 飞行（0.4 s 处手牌卡面→3D 体）→ `OnDeployed` → `BeginPlay`（先问 ask）→ `DoPlay` → 引擎 | 拖回手牌 / 拖到空白 = **正常取消**（弹回走手牌重排 0.2 s） | 落在格位但引擎拒 → `cantdo` 语音 ＋ 文字 **「拖到目标上再松手」**（🔴 文案错，见 #17） | `Hand/CardInteraction.cs:882-1025`（Release）· `:814-856`（DropReject）· `Battle/BattleDriver.cs:5322-5345`（CanDropAtSlot）· `:12873-13027`（OnCardDeployed/DoPlay）· `Board/DeploySequence.cs:144` | ① 被拒文案错；② 引擎真拒时让位预览不回弹（见「顺手发现 3」）；其余一致 | 见 #17（文案）；预览回弹：`BattleDriver.cs:12937` 那条早退里补 `EndPreviewReturn()` + `SyncBoard`/`RefreshAll` 一次 |
| 2 | **打出战术卡（无目标）** | 同上（拖到棋盘任意处松手） | `DeploySequence.IsInline` = 非单位卡 ⇒ **不飞、无中央展示** → `DoPlay` 战术支：原地放大到 1.0 + α→0（0.2 s）→ 引擎结算 | 同上 | 同上 | `Board/DeploySequence.cs:132-145` · `BattleDriver.cs:12965-13004` · `dc/CardScript__NonTargetSpellPlayed.c:7-25` | 一致（原版**也没有**「中央展示」：`NonTargetSpellPlayed` = 卡状态 6 → 播音 → `GoToCemetery`；溶解那半在 `_ResolvePlayCardFromHand…:633,638`） | — |
| 3 | **打出战术卡（敌方目标）** | 拖到**敌方**单位身上松手 | 卡本身转绿（`ValidTarget`）；**不点亮落点、不预览让位**（战术卡不落格位，`CardInteraction.cs:726`）→ 松手 → 引擎按 `spec.Side=="enemy"` 取敌方同号格 | 松手没命中任何格 = 正常取消（只有文字） | 目标非法：`CanDropAtSlot` 先返回 false → 掉落回我方棋盘判定 → 多半 `MissedSlot` ⇒ **无语音、只有「拖到目标上再松手」** | `Core/EffectResolver.cs:1843-1866`（CanPlayTactic 的目标校验）· `CardInteraction.cs:830-835`（先试敌方半场） | 原版：`IsValidSpellTarget` 失败 ⇒ `NotifyCantDoAction` **语音+文字**（`dc/BattleManager__Update.c:660-673`）；我们只有文字 | `BattleDriver.cs:12152`（`OnCardReturned`）—— 需要知道「是不是战术卡拖空」：让 `CardInteraction.Release` 把 `DropRejectKind` 与 `card.Data.isUnit` 一起带出来（新增事件形参），仅当 `!isUnit` 时补 `SpeakCantDo()` |
| 4 | **打出战术卡（友方目标）** | 拖到**友方**单位身上松手 | 同上 | 同上 | 🔴 **没有任何提示就打到别人身上** | `CardInteraction.cs:830-835`（先试 foeBoard，**不看卡面要哪一侧**）→ `BattleDriver.cs:5344` → `Core/EffectResolver.cs:1864`（侧由 `spec.Side` 定，**与指针在哪块棋盘无关**） | 🔴 **真缺陷**：友方目标卡拖到**敌方**单位上，只要我方同号格有合法友军 ⇒ 落点判「合法」→ 效果加在**自己人**身上、指针下的敌人毫无反应；反向（敌方目标卡拖到自己人身上）同理 | `CardInteraction.DropReject`：把 `CanDropAtSlot` 改成 `Func<int, BoardLayout, CardView, bool>`；`BattleDriver.cs:5322` 侧比对「`which == foeBoard`」与 `EffectText.PickSide(Parse(desc))`（`Core/EffectText.cs:1327`），不一致直接 false，**并且不许再回落到另一块棋盘** |
| 5 | **单位卡的指向性 Rally/Deploy 目标** | （原版）单位落位**之后**再点一个目标 | 🔴 **我们整条没有** | — | 无（静默） | 原版 `dc/BattleManager__NeedToChooseMinionTarget.c:7-19`（`isPlayer && cardType==0 && targeted && matchType!=0xa0`）→ `dc/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:849-860` → `ChooseUnitTarget`（UIstate 8 等点击）；我们：`Core/EffectResolver.cs:1753-1782` 的 `else` 支 = **从池子头取 N 个** | 🔴 **真缺陷**：现池 **16 张**单位卡的 desc 命中「`Deploy/Rally/Ambush/Backlash:` + `a(n) enemy/friendly troop\|unit`」（=`cards_engine.json` 正则上限，逐张 `targeted=true` 待解析器复核；例 `AM70 Scion Medic` / `DA5 Apothecary` / `EC33 Alluress` / `SW12 Thunderwolf Cavalry` / `GOF18 Painboy Sniklaw`）⇒ 目标由「第一个」静默决定 | 新增 `RuleCore.NeedChooseDeployTarget(ctx,p,idx)`（照 `NeedToChooseMinionTarget`）；`PlayCard` 加可选 `deployTargetSlot`；`BattleDriver.DoPlay` 落地后进「选目标」态（复用 ④ 的点击派发/准星），把 `chosen` 传进 `ResolveTargetsCore` |
| 6 | **主动能力（敌方目标）** | 按/拖场上我方单位 >10px → 三钮选择器 → 悬停选中「技能」格 | `CommitCommand` → 写 `LastAttackType` → 换档音 → 点亮敌方合法目标 + 准星 + 技能卡面板 → 点敌方单位 → `Resolve`（有 ask 先问）→ `DoResolve` | 点条外取消（**自造**，AU3 #29）；点空处/点自己人 = 静默取消（见 #16） | 面板「释放」而 0 合法目标 → `NoTargetAvailable`；点了个打不了的目标 → `InvalidTarget`；`Exhausted`/`Stunned` → `UnitNotReady`/`STUNNED` | `BattleDriver.cs:14267-14370`（CommitCommand）· `:14865-14936`（DoResolve）· `Core/RuleCore.cs:10694-10714`（UseAbility） | 一致（攻击侧细节见 AU3） | — |
| 7 | **主动能力（友方/任意目标）** | 同上 | 同上，但目标侧**恒为敌方** | 同上 | 同上 | 🔴 引擎已按 criteria 定侧：`Core/RuleCore.cs:10297-10326`（`CanUseAbility(..., out targetOwner)`）· `:10408-10440`（`AbilityPickOwnersOf` / `ResolveAbilityTargetOwner`）；驱动 **`BattleDriver.cs:14085-14127` 三档一律 `return 1-_me`** | 🔴 **引擎支持但玩家点不到**（友方 24 行 / 任意 36 行 criteria，今天可达 0 张 `Ability:`，但夹具可造） | `AbilityPickOwner` 三档映射（Enemy→`1-_me` / Friendly→`_me` / Any→两侧）；`HighlightTargetsCore:14472` 与 `HitCommandTargetSlot:14226` 改成两侧并集；`DoResolve` 的 `act.TargetP`（`:14884` 硬写 `1-_me`）改成由调用方传入 |
| 8 | **攻击（只看入口与取消）** | 同上三钮 → 近战/远程格 | 同上；命中/准星表现归 AU3 | 点条外（自造）；**点空处 = 静默 CancelAttack**；**点自己人 = 静默取消** | 目标非法且点到一张卡 → 引擎码 → `NotifyCantDoAction(HintForCode)`（`CantAttack*` 7 档）✔ | `BattleDriver.cs:13730-13747` · `:14903-14925` · `dc/BattleManager__Update.c:1040-1092`（invalid → `IsValidAttackTarget(notify)` → `CancelAttack`） | 点空处静默 = 原版；**点自己人静默 = 原版会提示**（`IsValidAttackTarget` 内部 Notify） | 同 #16 的修法（点空才取消；点到卡就走引擎给按因的码） |
| 9 | **换牌（mulligan）** | 面板：每张卡下方「换」钮 = 标记/取消标记；点卡本身 = 放大看；眼睛 = 收/放 UI（连压暗一起）；`Enter`/`Space` = 完成 | 标记 toggle → 完成时把 `_marked` 交给 `RuleCore.Mulligan` | 逐张再点「换」取消标记（原版无「整批放弃」） | 面板吃掉所有点击（`return true`），没有非法态 | `Battle/MulliganPanel.cs:1429-1439`（HandleClick）· `:1351-1360`（ToggleMark）· `BattleDriver.cs:7946-7969`（HandleMulligan） | 一致（键盘档 `dc/MulliganManager__Update.c` 本来就有） | — |
| 10 | **选牌 / 选效果（效果发起的弃牌·墓地取回·牌库取回·展示）** | 结算到 ask 点自动弹面板；点卡选一项 | 候选 >上限时**先随机抽**再摆（`GetChoiceOptions` 同口径）→ 点卡选中（**单选单向**，再点同一张无效）→ `Continue` 提交 | **无取消路径**（原版也没有） | 空选点确认 = 什么都不做（`dc/ChooseCardMenu__ClickChooseDone.c:13-18`；我们 `_confirmInteractable` 同） | `BattleDriver.cs:10201-10360`（ShowAsk/NextAsk）· `Battle/CardChoicePanel.cs:902-920`（TogglePick）· `:625-630`（OnUguiDone） | 一致 | — |
| 11 | **手牌陷阱（打出 = 丢弃）** | 同 #2 | 同 #2；引擎只记一条日志并进墓地 | 同 #2 | 同 #2 | `Core/EffectResolver.cs:2319-2322` · `:8020-8037`（回合末那支才真结算） | 一致 | — |
| 12 | **看牌：开/关卡面展示窗** | 手牌/场上单位**轻点**（位移 < 0.3 且没出过卡）→ `Toggle` | 开窗；指针**离开源卡且不在窗内** ⇒ 每帧判一次自动关；点压暗层 ⇒ 关；**再点同一张 ⇒ 关** | 三条：再点 / 移开指针 / 点压暗 | `TutorialAllowsCardDisplay` 假 ⇒ 静默不开（原版 `hideLargeCardDisplay` 同，静默）✔ | `Hand/CardInteraction.cs:546,1018`（OnTapped）· `BattleDriver.cs:12177-12192`（OnCardTapped）· `:12092-12118`（ToggleUnitCard）· `:12215-12232`（离开即关） | 🔴 **选目标中 / 打法选择器开着时手牌轻点仍能开窗**；原版 `CanDisplayHandCard` 要求 `UIstate==1`（`dc/BattleManager__CanDisplayHandCard.c:7-11`） | `OnCardTapped` 开头加 `if (IsTargeting \|\| (selector != null && selector.Visible)) return;`（`BattleDriver.cs:12183` 之前） |
| 13 | **点自己牌堆 → 摊开牌库** | 左键点我方牌堆 | 开 `MultiCardDisplay`（标题「你的牌库」），Continue / 背景关 | 点 Continue / 点背景 | 无 | `BattleDriver.cs:13719`（入口）· `:12590-12599`（ShowMyDeck） | 🔴 **原版没有这个入口**（`dc/BattleManager__CanDisplayDeckSize/ShowDeckSize.c` 只是显示**张数**；多卡窗只由动作 86 `showCardsWindow` 开）；标题还是**硬编码中文** | 删 `:13719` 那一条（多卡窗仍由 `OpenShowCardsWindow:12623` 服务原版动作 86）；若保留，标题必须走 `Loc` 词条 |
| 14 | **右键语义** | 按住右键 | = 镜头平移/缩放（`Battle/CombatCameraZoom.cs:267-299` 读 `TouchInputManager.TouchPressedSecondary`）；同时是手牌**悬停进入 / 起拖 / 跟手**的闸（`Hand/CardInteraction.cs:1175-1184`） | — | — | `dc/CardScript__IsRightButtonOrTwoFingerTouch.c:8-10`（`GetMouseButton(1) \|\| touchCount>1`，**电平**） | 一致（原版那三个入口不设闸：`OnTouchDown`/`OnTouchExit`/`OnTouchUpAsButton`） | — |
| 15 | **ESC** | — | 三处：普通对局开设置窗（`HandleSettingsEsc`，且 `IsTargeting`/选择器开着时**不认**）；关语音气泡；终局 / 回放出口 | — | — | `BattleDriver.cs:5719-5735` · `:6395` · `:11662-11671` · `dc/BattleHud__Update.c:20-35` · `dc/BattleManager__Update.c:1099-1103,1303-1340` | 一致：**ESC 不取消选目标、不关打法选择器** | — |
| 16 | **点空处 / 再点一次自己** | — | 点空处：选目标中 → `ClearSelection()`；选择器开着 → 取消（自造）；其它 → 无反应。再点自己：手牌 / 场上单位都 = 关展示窗 | — | 🔴 **选目标点空处 / 点自己人 = 静默取消** | `BattleDriver.cs:13742-13746` · `dc/BattleManager__FinishedChoosingAbilityTargetByClick.c:25-45`（target==null 支 → `NotifyCantDoAction` + `CancelAbilityTargeting`）· `dc/BattleManager__Update.c:1200-1213` | 🔴 原版**出声**（技能支 = `DragToTarget`；攻击支 = `CantAttack*`），我们静默 | 把 `HitCommandTargetSlot`（`:14226`）改成「**指针下的任何棋盘卡** → (座位, 格号)」；④（`:13742`）改成：**点到卡 ⇒ `Resolve(_command, t, 座位, fromUi:true)` 交给引擎按因报错**（技能 → `InvalidTarget`、攻击 → `CantAttack*`），**只有点空**才 `ClearSelection()`。⚠️ 技能那支要让 `Resolve/DoResolve` 带上**目标座位**（今天是裸 `slot`；友方目标修完 #7 后必须带侧，否则又会跨侧解） |
| 17 | **非法操作五档** | — | ① 费用不够 ② 两侧棋盘满 ③ 目标非法 ④ 非己方回合 ⑤ 手牌满 | — | ①–④ **全部落到「拖到目标上再松手」**＋`cantdo` 语音；⑤ 正确（`HandFullText`，`BattleDriver.cs:17662`） | `dc/BattleManager__CanPlayCard.c:82-152`（`HasEnoughMana`→`NotEnoughMana` / `IsAvailableSlot`→`NotEnoughRoom` / `IsValidSpellTarget`→`NoTargetAvailable` / `allowPlayerActions`→`NotYourTurn`，**每档都 `NotifyCantDoAction`**）· 我们：`BattleDriver.cs:5322-5345` 只返回 bool，`Core/RuleCodes.cs:375-378` 有 1:1 词条却用不上 | 🔴 **四档全错文案**：原版按因给 4 条不同提示，我们统一给「拖到目标上」 | `CanDropAtSlot` 改成带 `out int code`（或 `BattleDriver` 存「最后一次落点被拒的 `RuleCodes`」）；`OnIllegalAction`（`:7448`）读它 → `NotifyCantDoAction(HintForCode(code))`；`OnCardReturned`（`:12173`）只在 `kind==MissedSlot && !isUnit` 时写 `DragToTarget` |
| 18 | **回放中的操作** | — | 允许：点击单位/手牌看牌、回放条、ESC 退 | — | 出牌 / 选打法 / 放技能 / 结束回合：全部显式拒（出声）✔ | `BattleDriver.cs:5329`（CanDropAtSlot）· `:14275`（CommitCommand）· `:12895`（DoPlay）· `:15164`（EndPlayerTurn） | 🔴 **手牌仍可拖起、拖起来再弹回**，并会走 `OnIllegalAction` 播 `cantdo` + 写「拖到目标上」；原版拖拽闸本身就含 `matchType != 0xA0`（`dc/CardScript__OnTouchDrag.c:17-23`） | 同 #19 |
| 19 | **模态窗 / 选目标 / 选择器开着时的手牌** | — | 🔴 都可：悬停、拖起、落下、轻点开窗（`Hand/CardInteraction` 是独立 `MonoBehaviour`，只吃 `Frozen`） | — | 落下后由 #17 那一套错文案兜底 | `Hand/CardInteraction.cs:192`（`Frozen`）· `BattleDriver.cs:5410`（接线只含 `PlayerInputFrozen \|\| WaitingForBlockingAction`）· `:12381-12399`（`HudButtonsBlocked` 那 8 扇窗在别处判） | 🔴 **原版闸 = `UIstate==1 && matchType!=0xA0 && !rightHeld`**（`dc/CardScript__OnTouchDrag.c:17-23`）＋全屏压暗层吃射线。我们**两样都没有** ⇒ 选牌面板/设置/日志/多卡/联盟/聊天/展示窗/选目标/选择器/回放 全都能被手牌操作穿透 | ✅ **本轮已修（在 `CardInteraction` 内，未动驱动）**：`Hand/CardInteraction.cs:262 BattleStateBlocked` （`Ctx.IsOver` ∨ `PointerOverBlockingUi` ∨ `IsTargeting` ∨ `SelectorOpen`）+ `:286 ReplayDragBlocked`（回放**只禁拖**）+ `:310 AbortDrag`（闸落下时把手里那张收回手牌）+ `:526 BlockedThisFrame`（`Update` 与自检共用）。⚠️ 回放档**没有**整段关：原版 `OnTouchDown`/`OnTouchUpAsButton` 无 0xA0 闸 ⇒ 轻点看牌照旧允许 |

## 二、我特别要回答的三问

**Q1 有没有「玩家能做但引擎不认」/「引擎支持但玩家点不到」？** 有 5 条，按价值排：
① **友方/任意目标的主动能力**（#7）：引擎 2026-10-25 已通（`Core/RuleCore.cs:10297-10326`），驱动仍恒敌方（`BattleDriver.cs:14126`）⇒ **引擎支持、玩家点不到**。
② **单位卡的指向性 Rally/Deploy**（#5）：原版有「落位后再点目标」一整步（`dc/BattleManager__NeedToChooseMinionTarget.c`），我们整条没有 ⇒ 引擎 `firstAdded` 静默挑（现池 16 张可达）。
③ **跨侧落点**（#4）：友方目标卡拖到敌人身上照样打得出去、效果落在自己同号格 ⇒ **玩家能做、语义全错**。
④ **手牌在模态窗/选目标/回放里仍可拖**（#19/#18）⇒ 玩家能做、原版不能。
⑤ **`DoResolve` 的 `TargetP` 恒 `1-_me`**（`:14884`）⇒ 友方目标技一旦开启，录像/联机会按敌方侧重放（今天 0 张可达，属「记录层不认」）。

**Q2 静默失败清单**（点了没反应 / 没有任何提示）：
1. 选目标时**点空处 / 点自己人** → `ClearSelection()` 静默（#16；原版 `NotifyCantDoAction` + 取消）。
2. ①费用不够 ②格满 ③目标非法 ④非己方回合 → **统一错文案**「拖到目标上再松手」+ cantdo 语音（#17）。
3. 战术卡松手没命中任何格 → **只有文字没有语音**（原版这一支是语音+文字，`dc/BattleManager__Update.c:660-673`）。
4. `SpeakCantDo` 有「正在说话就不插播」闸（`BattleDriver.cs:7373`，原版同）⇒ 连点两下只出一声。
5. 单位卡指向性目标被 `firstAdded` 挑走，**玩家界面零提示**（#5）。
6. 「引擎替玩家挑」只在**战斗日志**里（`ReportUnaskedChoices:13047`），现场界面看不到。
7. `click_log.txt` 10-25 Battle 段 #38/#40/#42/#44 同一坐标**交替「无日志」** —— 是展示窗「点前台卡不换位」的正常 no-op，但 `SwapToFront` 的 no-op 支一行日志都没有，排查时会误判成「没接上」（诊断项，非原版行为）。

**Q3 同一件事有没有两条路径、行为是否一致？**
· **关展示窗**三条（再点 / 移开 / 点压暗）→ 都关，一致。· **开窗**两入口（手牌轻点 / 场上单位轻点）→ 同一 `CardDisplayWindow.Toggle`，一致 —— 但**手牌那一路少了 UIstate 闸**（#12）。· **取消选目标**三条（点空 / 点自己人 / 点选择器外）→ 结果一致（都 `ClearSelection`），**提示不一致**（原版只有「点到非法卡」那条出声）。· **换牌**：「换」钮（标记）vs 点卡（看牌）分工清楚，一致。· **打法三选一**只有「悬停即选」一条（`AttackSelector` 没挂 `BattleHudButton`），与原版 `CardDisplayAttackTypeButton__Update` 的悬停语义一致（详见 AU3 #7）。

## 三、顺手发现（⛔ 未改，最多 8 条）

1. **AUD7 文件头与 AU3 引用「ESC 取消选择（`BattleDriver.cs:11673`）」是错的**：那是 `EscPressed` 的**定义**，只被设置窗 / 聊天 / 终局 / 回放四处消费，**不取消任何选中态**（本轮 #15 已核）。建议订正那两处。
2. `BattleDriver.cs:13903` 用 `CardText.Phrase("THIS UNIT CANNOT ACT")` —— 不是 `Battle/Tips/*` 官方词条（W62 删自拟文案时漏网）；`:13887` 的 `STUNNED` 有注释说明是刻意保留，这一条没有。
3. `interaction.DropAccepted` 只在 `DoPlay` **成功**路径清（`:12952`），而 `Release` 已把 `_previewMoved` 清空（`EndPreviewReturn:24938-24953`）⇒ 若随后引擎拒（`:12937` 早退）被推开的单位**停在预览位**，直到下一次 `SyncBoard`。
4. `ShowMyDeck` 的标题 `"你的牌库"` 硬编码中文（`:12598`），未走 `Loc`。
5. `SimpleAI.cs:420-434` 对 `NeedsPick` 技能只扫 `foeBoard`，而 `CanUseAbility` 现在按 criteria 解到**己方**棋盘 ⇒ 一旦有友方目标技能，AI 会按「自己第 t 格」判合法性却把 `TargetP=foe` 写进动作（录像/联机按敌方侧重放）。今天 0 张可达。
6. 引擎 `RuleCore.AbilityPickSideOf`（`Core/RuleCore.cs:10372`）与驱动私有同名副本（`BattleDriver.cs:14134`）是**两份判据**；驱动那份还带「引擎结算层只认敌方」的**过期理由**（`RuleCore.cs:10297` 已改），#7 修完要删掉那份副本、改调引擎。
7. 修 #19 会让若干「手牌在任意窗口下都能拖」的既有断言变红 ⇒ 同步点先 `grep` `SetFrozenForTest` / `interaction.Frozen`（`Editor/BattleScene.cs` 里那几条）。
8. `BattleDriver.cs:12396` 的模态清单里 `_logPanel` 是后补的（注释自己写着「原来不在表里」）—— 建议把「模态窗集合」收成一处（`HudButtonsBlocked`），别再有第二个清单（#19 的修法依赖它）。

## 四、本轮执行（2026-10-25，授权只改 `Hand/CardInteraction.cs`）

- ✅ **#4 跨侧落点**（友方目标卡拖敌方 / 反向）—— 已修：`Hand/CardInteraction.cs:1003 TacticTargetSide` + `:1081-1118 DropReject`（判据 = `EffectText.PickSide`，错侧 `EngineRefused` 且**不再回落**）。
- ✅ **#19 输入闸**（模态窗 / 选目标 / 打法选择器 / 终局 + 回放只禁拖）—— 已修：`:262 BattleStateBlocked` / `:286 ReplayDragBlocked` / `:310 AbortDrag` / `:526 BlockedThisFrame` / `:567 TickInput`。
- 断言**已接线**（第二版授权）：`Editor/HandAssertE8.cs` 新增 `E_DropSide` / `F_InputGate` / `G_ReplayDrag` 共 **22 条**；`Editor/BattleScene.cs` §10 新增 **3 条**真实引擎复核（`X9-A/B/C`）—— `HandAssertE8.Run` 已由 `BattleScene.cs:13667` 并进 `BattleScene.Run` 的合计。
- 执行报告（含断言清单 / 灭自证 / 风险点 / 残留）→ **`X9_跨侧落点与输入闸_执行报告.md`**。
- ⛔ 本表 #7 / #5 / #17 / #13 与「顺手发现 3/4」**仍未做**（`BattleDriver.cs` 被占）。
