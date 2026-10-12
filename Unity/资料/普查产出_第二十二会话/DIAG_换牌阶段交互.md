# DIAG 换牌(mulligan)阶段交互 —— 原版 vs 我们

> 只读诊断（用户 2026-10-12 报：「战斗开始前替换手牌的阶段，你搞错了，**不是点击手卡替换**」）。
> 判据第一权威 = 反编译 `d:/2/tools/decomp_full/`；佐证 = 解包资源 + 原版运行期 dump（`资料/原版参照图/Unity参照管线_0825/data/panel_0914b/*.tsv`）。
> 全部路径：反编译只写文件名（目录同上）；本仓从 `d:/4/Unity/MyGame/Assets/CardPresentation/` 起。

## 一、原版是怎么做的（逐句：玩家操作 → 系统反应 → 确认出口）

1. **起手牌被「抽」进换牌区**：`PlayerHand.<AddCardsToMulligan>`（`PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:100-125`）逐张
   `SetupCardInMulligan`（记 `mulliganOption`，`PlayerHand__SetupCardInMulligan.c:20-22`）→ `MoveCardToMulliganPos`
   （`PlayerHand__MoveCardToMulliganPos.c:26-30`：位置取自 **`MulliganManager.MulliganAnchor`（`+0x58`）的 `CardsHorizontalLayout.GetPosition(i,n)`**）
   → `CardScript.DrawCard(...)` + `SetStateAsMulligan(card,false)` ⇒ **cardState = 0xD**（`CardScript__SetStateAsMulligan.c:20`：`state = 0xD + flag`）。每张之间 `WaitForSeconds(timeToDraw*常量)`。
2. **每张牌上运行时生成一个 `MulliganFrame`**（`MulliganManager__SetupMulliganButton.c:26-32,50,104-110`）：以**该卡当时的世界位**为位、**该卡的 localScale** 为缩放，实例化预制体 `cardMulliganButton(+0x50)`，挂进 `mulliganFrameButtons(+0x68)`，并给它的 `changeCardButton(+0x38).onClick` 挂 `MulliganFrame.ChangeCardButtonOnClick`。
3. **点那颗钮 = 标记/取消标记那张牌**：`MulliganFrame__ChangeCardButtonOnClick.c:5` → `CardScript.ClickMulliganSelected(card, true)`
   →（`CardScript__ClickMulliganSelected.c:27,31-53`，闸：`UIstate == 0xb`、`isPlayer`、`mulliganOption != 0x1e`）
   `0xE ⇄ 0xD` 翻转 + `BattleCardUI.SetAsMulliganSelected(bool)`（生成/销毁卡上那块「已选」高亮，`BattleCardUI__SetAsMulliganSelected.c:40-53`）+ 播 `Toggle` 音。
   - **钮上的字逐张变**：`term = (card+0x228 == 0xE) ? <Undo> : <Replace>`，`MulliganFrame.{SetupMulligan.c:24, UpdateButtonText.c:14, ChangeCardButtonOnClick.c:16}` 三处逐字相同。
     字在预制体的 `Button Text`（`Localize`，`bundle_battlesharedresources_assets_all/MonoBehaviour/MonoBehaviour_-7805435555962860294.json` 的默认 `mTerm` = `Battle/Mulligan/ButtonDone`）。
     **两条词条的绑定（推断，见 §五·1）**：字面量槽按 metadata 字符串堆顺序对齐 ⇒ `+0x4288E18 = Battle/Mulligan/Replace`（未标记）、`+0x4288F08 = Battle/Mulligan/Undo`（已标记）。
4. 🔴 **点手牌本身 ≠ 标记 —— 是「展示这张牌」**：`CardScript__OnTouchUpAsButton.c:52-58`
   （`cardState ∈ {0xD, 0xE}` 这一段就是换牌阶段）→ 置 `displayingCardFlag(+0x24a)` → **`BattleManager.DisplayCard(bm, this, 1, 1)`**
   （第 4 参 = 强制展示，绕开战斗阶段门控；指针移开由 `OnTouchExit.c:15-16` 收）。
   换牌期卡的碰撞体是**开着且原始尺寸**（`CardCollider2D__SetColliderNotInHand.c:11-24`：state ≠ 0xB/6 走 enable 分支）⇒ 卡就是**给点着看**的。
5. **确认出口 = 点 Continue**（`MulliganContinueButton/Button` → `MulliganManager.ClickMulliganDone` → `ProcessMulliganDone`）；
   **键盘 `Space`/`Enter` 同效**（`MulliganManager__Update.c:8-9`，门控 = `buttonsGroup.activeSelf`）；
   **倒计时归零同效**（`BattleManager._MulliganCountdown_d__347__MoveNext.c`：`<10` 把 `0:0S` 写进 `mulliganButtonText`、`<=0` 调 `ProcessMulliganDone`；⚠️ `matchType 0x32` 离线练习局**整段 return**）。
   提交的是 **state == 0xE 的那些牌**（`PlayerHand__GetMulliganCardsToDiscard.c:29`），落 `matchData+0x80`（`BattleManager__ClickMulliganDone.c:36-40`）。
6. 收尾：`ProcessMulliganDone` 灭 `buttonsGroup`/`mulliganTextObj` + **销毁全部 `MulliganFrame`**（`MulliganManager__ProcessMulliganDone.c:30-58`）；
   「眼睛」`HideMulliganButton` → `ToggleMulliganVisibility` → `ShowMulliganElements(!activeInHierarchy)` 一次收四样（两个 GO + 每张卡/每颗 frame + `Shade.SwitchShade`）。
7. 换牌区/面板的静态真值（running dump）：`Mulligan > MulliganAnchor / ButtonsGroup{MulliganContinueButton(Button+Text「继续」+CircleButton) · HideMulliganButton} / MulliganText{Text / TurnText}`；
   两个 `CardsHorizontalLayout` 实例 = **`MulliganAnchor` 的 MB_4881**（换牌面板）与 `ChooseCardMenuAnchor` 的 MB_4052（选牌面板），二者参数**逐字相同**（`m_scale=183.41` · `spacing=502.2` · `nForMaxHeight=7` · `maxHeight=0.44` · `useRotation=0`）；
   `/MulliganAnchor` 在场景里**只有 Transform**（`GameObject/MulliganAnchor.json`）。树的全文 → `panel_0914b/p3_mulligan_tree.tsv`。

## 二、我们现在是怎么做的（文件:行号）

- `Battle/MulliganPanel.cs:531-568` `HandleClick`：① `HitDone` → `OnDone(marks)` ② `HitEye` → 开关 ③ `ButtonIndexAt` → `ToggleMark`
  ④ 🔴 **`:563-565` 「点卡本身也能标记/取消」（注释自己写着「原版是卡片上的按钮；我们两条路都留着，**点卡这条路是我们加的**」）** ⑤ `:567` 无条件 `return true`（点哪儿都吃掉）。
- `Battle/MulliganPanel.cs:494-499` `ToggleMark`（无任何闸）；`:511-525` `ApplyMarks`（置灰 + `UI_Button_Mulligan(_Pressed)` + 字换 `Replace/Undo`）。
- `Battle/BattleDriver.cs:4359-4380` `HandleMulligan`：键盘 → `DoneWorldPos`；`ReleasedThisFrame()` → `_mulligan.HandleClick(WorldPointer())`，**恒 `return true`**（这一帧输入全归它）。
  调用点 `:7434`（`TickMulligan` 之后、`HandleChoose` 之前）。
- `Battle/BattleDriver.cs:4417` `interaction.enabled = false`（"换牌阶段不让拖牌/悬停/轻点"）⇒ **轻点那条路被整段掐掉**；
  正常手牌轻点是 `:7028-7045` `OnCardTapped` → `_cardDisplay.Toggle(...)`（= 原版 `DisplayCard`），换牌期走不到。
- 每张牌那颗钮 = 我们自己排的（`MulliganPanel.cs:356-412`，图 `UI_Button_Mulligan` 410×124、宽=卡宽、贴在卡下缘 30% 卡高），文件头已声明「位置和大小是我们排的」。

## 三、逐项差异表

| 项 | 原版 | 我们 | 出处 |
|---|---|---|---|
| **点手牌** | **把这张牌放大展示**（`DisplayCard(card,1,1)`，指针移开关） | 🔴 **标记/取消「要换这牌」** | 原版 `CardScript__OnTouchUpAsButton.c:52-58` ↔ 我们 `MulliganPanel.cs:563-565` |
| 点卡上那颗钮 | 标记/取消（`ClickMulliganSelected`） | 同（这条对了） | `MulliganFrame__ChangeCardButtonOnClick.c:5` ↔ `MulliganPanel.cs:560-561` |
| 钮的两档字 | `Replace` / `Undo` | 同（`Loc` 表两条都在） | `MulliganFrame__UpdateButtonText.c:14` ↔ `MulliganPanel.cs:113,127,131` |
| **不可换的牌** | `mulliganOption == 0x1e` 时**点也不动** | **没有这个闸**（全仓 0 命中 `mulliganOption`） | `CardScript__ClickMulliganSelected.c:27` ↔ 我们无 |
| 牌在哪 | 被逐张抽进 **MulliganAnchor** 那一行（该 layout `spacing 502.2`） | 留在手牌位（`interaction.enabled=false`），钮贴着卡摆 | `PlayerHand__MoveCardToMulliganPos.c:26-30` + MB_4881 ↔ `MulliganPanel.cs:327-332,388` |
| 钮的形 | 运行时实例化 `MulliganFrame`（绿条 `InfoText` + `Change Card Button` + `Button Text`），位/缩放 = 该卡 | 我们挑的图与摆法（已声明） | 预制体 `bundle_battlesharedresources_assets_all/MonoBehaviour/MonoBehaviour_-9187586159504044806.json` ↔ `MulliganPanel.cs:356-412` |
| 确认出口 | Continue 钮 + Space/Enter + 倒计时 | 三样都有（对上了） | `MulliganManager__Update.c:8-9` ↔ `BattleDriver.cs:4359-4418,4316-4345` |

## 四、改动清单（文件:行号 / 改什么 / 顺序 / 会不会动录像与联机）

1. `Battle/MulliganPanel.cs:563-565` —— **删掉「点卡 = ToggleMark」那一段**（连同 `:392` 那句「没图就只靠点卡那条路」的兜底口径一起改：钮没图时应**出声**或换占位图，⛔ 不许退回点卡）。
2. `Battle/MulliganPanel.cs:567` —— 命中卡时**不要 `return true`**（面板只吃自己的件），或直接暴露出 `CardIndexAt(world)` 给驱动层，由驱动层转发（口径见下条）。
3. `Battle/BattleDriver.cs:4359-4380` —— 换牌期的**卡上轻点**要接到原版那条链：`_mulligan` 没吃掉的点击 → `OnCardTapped(card)`（`Battle/BattleDriver.cs:7028-7045` → `CardDisplayWindow`）。
   ⚠️ 需要放开 `:4417` 那道「`interaction.enabled = false`」（原版换牌期卡的碰撞体是**开着**的）——⛔ 只放**轻点**、别顺手把拖牌/悬停也放出来（原版换牌期不走拖牌）。
4. `Editor/BattleScene.cs` 换牌那两节（`:12969-13245`、`:12847-12870`）—— 补两条断言：
   ① **「点卡不该标记」**（`IsMarked` 不变、`CardBtnWordAt` 不变）—— 现在**一条都没有**（全仓只有 `SimulateMulliganToggle` 走钮那条路，所以这个偏离一路活到今天）；
   ② **「点卡应开展示窗」**（`_cardDisplay.Visible` 为真）。另加一个 `SimulateMulliganCardTap(i)` 到 `BattleDriver.cs:5149` 那一族。
5. `Battle/MulliganPanel.cs` 文件头 + `BattleDriver.cs:4418` 提示行（`Loc.cs:1680 Battle/Hint/MulliganPick`）—— 措辞里「点牌上的『换』」仍成立，但要**补上「点牌本身是看牌」**这个口径（否则下一个人还会照旧注释加回来）。
6. **录像 / 联机：无影响**。标记只在 `ApplyMarks` 里动表现；动作流的两条记账口都在 `OnMulliganDone`（`BattleDriver.cs:~4460-4480`）：`RecRaw(NetActionKind.Mulligan, _me, marks)` + `RuleCore.Mulligan/Ctx` → `RecRaw(MulliganDone)`，
   以及 AI 那侧的 `RecRaw(NetActionKind.Mulligan, 1-_me, aiMarks)`（`OpenMulligan`）。**换的是「哪些手势能翻转标记」，不是动作流的形状** ⇒ 录像可回放性与主机定序都不受影响。
7. §三 第 4/5 两行（`mulliganOption` 闸 · 牌是否该被抽进 MulliganAnchor）是**另两笔账**，与本条修复不同源；按铁律 11「全都要做、只有先后」——建议**先做 1~5**（用户点名的这一条），4/5 两行另开一单。

## 五、没查清的部分

1. **两条词条↔两个状态的绑定是推断，不是直读**（`SetupMulligan.c:24` / `UpdateButtonText.c:14` 里的两个字面量地址 `+0x4288E18` / `+0x4288F08` 我**没读出字符串本体**）。
   依据：`global-metadata.dat` 的字面量堆顺序为 `ButtonDone → Instructions → Replace → Undo → WaitEnemy → secondTurn`，而连续槽位 `0x4288D20 / E18 / F08 / 9000 / 90F0` 按同一顺序落位、且三处独立命中（`WaitEnemy` = `SetWaitingForEnemy` 那一行、`secondTurn` = `MulliganText/TurnText`「你是第二个行动」、`Instructions` = 首行「选择要在首轮替换的牌」）⇒ 剩两槽 = `Replace` / `Undo`。**要坐实得跑一次原版点那颗钮看字**（今天没跑）。
2. **`mulliganOption == 0x1e` 是什么、非教程局会不会出现**：只在 `PlayerHand.GetMulliganOption` / `AI.GetAiMulliganCards` / 教程剧本的强制清单里见到，非教程局大概恒不触发 —— **没验**。
3. **换牌期牌到底停在哪**：我们 `Editor/BattleScene.cs:12999` 那行注释写「原版换牌阶段**不动手牌位置**」，但 `AddCardsToMulligan` 明确调 `MoveCardToMulliganPos` 把牌 `DrawCard` 到 `MulliganAnchor` 的 layout 去（该 layout 与选牌面板那份**参数逐字相同**）。
   **两份说法打架**，我没算出 `MulliganAnchor` 与手牌位是否恰好重合，也**没跑到实况**（离线局拿不到手牌注入，`资料/真Play待验清单.md` 的 E 类）。⇒ 列为待查，⛔ 别当结论用。
4. **原版 `DisplayCard` 第 3 个参数（我们这里恒传 1）代表什么**没查（第 4 个 = 强制展示，已确认）。
5. **实况没跑**：本轮全程只读（不跑 Unity、不动 git）。点卡那条现在的真机表现（点下去到底标记了没有）**没有截图佐证**，但代码路径是闭合的（`HandleMulligan` 恒 `return true` ⇒ 走不到 `OnCardTapped`）。
