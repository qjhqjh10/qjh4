# B9 · 执行代理 `E4BDR` 报告 —— `BattleDriver` 接线（A-03 接线 · A-05 十条 · 过期注释 · 新账「出对半」）

> 会话：2026-10-27 · 执行代理 `E4BDR` · **写手**
> 白名单 = `CardPresentation/Battle/BattleDriver.cs`（独占）· `CardPresentation/Editor/BattleScene.cs`（只改断言那几行）
> 类型检查：`TMPDIR=/tmp/wf_E4BDR bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**（**收尾那一跑**，两个文件都已改完之后跑的）。
> ⚠️ 中途两次跑出过**别家代理**正在写的 `RuleEngine/**` 错（`EffectResolver.cs` 的 `HandExpireOf`/`DrawnNow`/`DrawHandHopTakes` · `Enchantment.cs` 的 `CardDef.EnchantTrait` · `TutorialScript.cs` 的 `RunActiveAbility` · `AchievementTally.cs` 的 `TriggerWhileInPlay*`）—— 本批**一个 `RuleEngine/**` 文件都没碰**，它写完即回到 0/0。
> 行尾：两个文件都是**纯 CRLF**（`CRLF == LF`、`bareCR = 0`，二进制现读）⇒ 没被翻。改动规模：`git diff --numstat` = **BattleDriver.cs +370/−38 · BattleScene.cs +64/−14**。

---

## ① 结论（逐件）

| 件 | 内容 | 状态 |
|---|---|---|
| **1** | **A-03 两行接线**：`Begin` 里 `SetChatContext(本局我方督军, MatchType)`；`ShowAsk` 开选牌面板前 `ForcePopUpClose` | ✅ 做了；**同批改了** `Editor/BattleScene.cs` 那条断言（见 ④-1） |
| **2** | **A-05 十条**（35·102·103·108·114·115·116·118·131·132·133·136 里落在 `BattleDriver` 的那部分） | ✅ 全做；**两处按原版 `.c` 判据偏离了施工单**（见 ③-②③） |
| **3** | 两处过期注释（`Speak` 不再 `HideAll`） | ✅ 就地改（行数中性） |
| **4** | 🔴 新账「**出对半**」：targeted 战术/附魔卡进等目标态时 `DissolveWholeCard(0.2)`，取消时 `UnDissolveWholeCard` | ✅ 做了，并且**让 `UnDissolve` 的 α 0→1 变得可观察**（批处理档可断言） |

一句话：**「督军那 6 句台词」这条链现在通了**（面板拿到本局我方督军 → 6 颗钮印 `Chat_Messages/<战将>-Chat5..10`）；**开场白期间禁聊天**、**按颗关气泡**、**认输那句两半**、**`invoke` cue 判反**、**audio 可为 null**、**出对半** 全部落地。

---

## ② 证据（逐条，全部本轮亲读）

### 35 `BattleCardSounds.GetInvokeCue`（判反了）
- `decomp_full/BattleCardSounds__GetInvokeCue.c:3-7`：`if (param_2 == 4) return +0x18; return +0x10;`。
- `decomp_full/CardScript__CardToMinionAnim.c:20-26`：`if (HasCurrentTrait(this, 0x4b0, 0) == '\0') uVar4 = get_cardRarity(this); else uVar4 = 1;`
  ⇒ **带 ambush ⇒ rarity 强制成 1 ⇒ 走普通 cue**。
- ⇒ 条件改成 `card.Rarity == "legendary" && !card.Has(KeywordTable.Ambush)`。
- 另一调用点 `CardToMinionAnimTeleport` **不判 ambush**；我们这条链只服务落地那一拍（`PlayDeployLandingCues` 两个调用点都来自 `Deploy`），**没有 Teleport 部署入口** ⇒ ⛔ 没加 `teleport` 形参（施工单第③步的「若…则加」不成立）。
- **顺手就地订正**：`LandDustFieldFor` 那段「与 `InvokeCueFieldFor` 规则**故意不同**」**是错的** —— 两条判据**逐条同形**（`!ambush && rarity == 4`）。已改（铁律 5）。

### 102 `VoiceLinesController.CanChat`
- `VoiceLinesController__CanChat.c`：`isPlayer == 0 ⇒ if (+0x40 != 0) return 0`；`isPlayer != 0 ⇒ if (+0x78 == 0) return 0`；否则 1。
- `+0x78` 的四个写点：`__PopUpShowed.c:13` = 0 · `__PopUpHid.c:13` = 1 · `__SetPopUpDisabled.c:5` = 0 · `__SetPopUpEnabled.c:5` = 1 ⇒ **0 = 本机这一侧被挡**。
- 落地：`_chatPopUpBlocked`（Disable/Enable 那一半）＋ `ChatPopupPanel.Visible`（Showed/Hid 那一半）→ `ChatPopUpBlocksLocal`；`CanAiChat()` 吃它；`SpeakHurry` 吃 `CanLocalChat()`；`ClickChatHud` 吃 `ChatPopUpBlocksLocal`（`__ClickChat.c:9` 的 `if (+0x78 != 0)`）。

### 103 `VoiceLinesController.CanUseVOs`
- `__CanUseVOs.c:23-48`：① `matchType ∈ {100,140}` ⇒ 1；② `cardType == 0` ⇒ `RemoteConfig+0x10`；③ `== 10` ⇒ `RemoteConfig+0x11`；④ 其余 ⇒ 1。
- 卡型映射沿用已核过的那一份（`WhenEvent.ActingCardTypeMatches` 的注释）：`unit`=Minion(0) · `hero`=Hero(10) · `tactic`/`defence`=Tactic(20) · `whispers`=Whispers(40)。
- 两个远端开关 = **本地常量 `true`**，打了 `🔶 自己设计`（`UnityRemoteConfigData` 本地不可得）。

### 108 `VoiceLinesController.CloseChatBox` / `CloseRadio`
- `VoiceLinesController__CloseRadio.c:5` ⇒ `HideChatBox(+0x48, **2**)`。
- `VoiceLinesController__CloseChatBox.c:5` ⇒ `HideChatBox(+0x48, **param_2 ^ 1**)`。
- `AiScripted__CloseChatBoxes.c:19-40`：`if (+0x40 == 0) { bVar3 = (type == 0x32) ? 1 : (type == 0x37); } else bVar3 = 1;` ⇒ `isPlayer = playerAction || type ∈ {50,55}`。
- 落地：`CloseTutorialChatBoxes` 由 `HideAll()` 改成 `HideBox(2)` / `HideBox(BubbleSideOf(isPlayer ? _me : 1-_me))`；`StopVoice()` 保留兜底。

### 114 认输那句（三处）
- 发送侧：`VoiceLinesController__DisplayWarlordChatMessage.c:60-68` 的闸是 `idx != 0 && idx != 1` ⇒ **4 过得去**。
- 收侧：`DisplayWarlordRegularChatMessage` 由 `GetChatSoundAsset(4)` 取 `concede` 那族。
- 落地：`ChatIdConcede = 4`；我方认输 `SpeakConcede(_me)` → `_net.OnLocalChat(4)`；`NetRemoteResign` 里 `reason == Forfeit` 时 `SpeakConcede(1-_me)`；`DisplayRemoteChat` 认 `chatId == 4`（原来会落进「越界」被丢）。

### 115 / 118 audio 可为 null
- `VoiceLineBox._ShowChat_d__19_MoveNext.c:37-45`：只有 `data+0x28 != null` 才 `Play()`；`DisplayWarlordRegularChatMessage.c:40-44` 先组 data 再 `ShowChatBox`，**不因 audio 为空早退**。
- 落地：`SpeakWarlordLine` / `SpeakIntro` 去掉 `if (clip == null) return`，并传 `expectVoice: clip != null`。

### 116 `ForcePopUpClose`
- `BattleManager__ChoiceOfCardPlayer.c:27`：`UnpauseForAnim` → `CancelActionStates` → `UIstate = 14` → **`VoiceLinesController__ForcePopUpClose`** → `ActiveSkillDisplay.FadeOut` → `ChooseCardMenu.Setup`。
- `__ForcePopUpClose.c:5-9`：`if (+0x78 == 0) return;` + 选择器 `Hide()` ⇒ **开着才关**（极性已定案，原账那句「读出来是反的」可划掉）。

### 131/132/136 `SetPopUpDisabled` / `SetPopUpEnabled` / `ShowHeroesIntroMessage`
- `__SetPopUpDisabled.c:5` / `__SetPopUpEnabled.c:5` 各一句写 `+0x78`；调用点 = `BattleManager__OnInitializationComplete.c:20` · `_ShowHeroesIntroMessageCampaign_d__339_MoveNext.c:48`（Disable）/ `:41`（Enable）。
- 落地：`StartIntroMonologue` 真起播时 `DisableChatPopUp()`；`TickIntroMonologue` 收尾 + 三条早退（教程/战役 · 语音表没就绪 · 先手侧算不出）各 `EnableChatPopUp()`。

### 新账「出对半」
- `CardScript__MoveIntoFieldFromHand.c:47-75`：`cardType == 0x14(Tactic)` 且 `targeted`（`0x28 Whispers` 同一支）⇒ `StartSpellFrom` + `StartTargetedSpellAnim` + **`CardScript__DissolveWholeCard(globalVars + 0xb0)`**；`cardType == 0`（单位）那一支是 `StopReflected()` 后 **return**（**不溶**）。
- `CardScript__DissolveWholeCard.c:9-33` 四步：① `cardCollider2D.Toggle(cardType == 10)` ② `DOScale(transform, Vector3.one × +0x40(spellPlayedScale = 1.0), t)` ③ `StopShowingCodex` + `StopReflected` + `ToggleHighlight(false)` ④ `FadeCardItems(0, t)`。
- 淡的量 = `fadeGroup`（`BasicCardUI +0x68`，uGUI `CanvasGroup`）= 我们的 **`SetAlpha2D`**（⛔ 不是 `SetAlpha`，见 `CardView.cs:6541` 那条既有纪律 + `E2UDW` 报告 §②-972）。
- 撤销 = `CardScript__UnDissolveWholeCard` ⇒ `CardFeel.UnDissolveWholeCard`（`E2UDW` 本轮已把「淡的量」与「缓动」改对）。

---

## ③ 没查清 / 偏离施工单的地方（逐条写清）

1. **`NetBattle.OnLocalChat` 的发送闸仍是 `IsButtonLine(5…10)`** ⇒ 我加的 `_net.OnLocalChat(ChatIdConcede = 4)` 现在会被它挡下（打一条 `LogWarning`，不会静默）。
   判据那一半在 `Net/NetChat.cs` / `Net/NetBattle.cs` = **`A-06` 的白名单**，本波我一个字没动。
   ⇒ **认输那句「上网」这一半要等 `A-06` 把闸换成 `IsRelayable`**。收侧我已经认 `chatId == 4`，所以对面一改好，两侧同时生效。
2. 🔴 **`SpeakCantDo` 我【没有】加 `CanChat` 闸 —— 与施工单第②步不同，这是按 `.c` 判据改的**：
   `BattleTipController__NotifyCantDoAction.c:11` 传的是 `DisplayLocalChatMessage(vlc, **3**, **1**)`，第三实参 = `skipCanChat`；
   而 `VoiceLinesController__DisplayLocalChatMessage.c:5-8` 只在 `param_3 == 0` 时才查 `CanChat`
   ⇒ **`cantdo` 在弹窗开着 / 开场白期间照样说**（原版 `skipCanChat: 1` 就是为它准备的）。
   同族的 `DisplayHurryUpChatMessage.c:3` 传的是 **`0`** ⇒ `SpeakHurry` **要**过闸（已加 `CanLocalChat()`）。
   施工单把两句一并写成「也过 `CanAiChat()`」；我按第一权威（反编译方法体）只做了 Hurry，并在 `SpeakCantDo` 的 doc 里写死了这条判据，⛔ 别照施工单补回去。
3. **`+0x78` 那道闸的落位与原版分支不同（如实记）**：原版在 `isPlayer != 0`（本机自己那一侧）分支；施工单要求并进共享闸 `CanAiChat()`（断言也这么写）⇒ 现在**弹窗开着时，对面发来的那一句也会被挡**。这是照施工单执行，⛔ 别把它当成「原版也这样」。
4. **`Begin` 里我没有 `DisableChatPopUp()`**（原版 `OnInitializationComplete` 有）：换牌流程若没走完就没人 Enable ⇒ **整局聊天钮都点不开**（自检那一档尤其会被静默污染）。
   ⇒ 收口到「开场白真的开始 / 演完（或本局不播）」两处；代价是 **Begin→换牌完** 那一段没有 `+0x78` 闸（`HudButtonsBlocked` 本来也挡着那一档，观感无差）。
5. **`CardFeel.ReturnToHand` 只还原 `ArtAlpha`，不碰 `Alpha2D`**：取消支在 `animateFeel` 档走的是它（`W9K01` 调度台裁定），
   所以我在那条路**之前**调 `UndissolveFieldCard`（= `UnDissolveWholeCard` 把 `Alpha2D` 0→1）。
   副作用：真机取消时「0.2 s 淡回」与「0.92 s 反向 clip」**同时**演 —— 比原版（只 `UnDissolveWholeCard`）多一段回手动画。
   ⚠️ 这是 `W9K01` 那条裁定的既有效果，**本轮不动它**，只记在这里供调度台裁。
6. **`DissolveWholeCard` 的第③步三句在我们这侧无对应层**：`StopShowingCodex`（我们没有 codex）/ `StopReflected`（战术卡没有反射体）/ `ToggleHighlight(false)`（非单位卡不进点亮）⇒ 记为「无对应物」，⛔ 没有假装做了。
7. **`ChatTermKey` 不走 `ChatIdOf`**（115/118 的顺带项）：落点在 `UnitChatPanel`，**不在本波白名单** ⇒ 仍欠。
8. **`StopVoice()` 我保留了**（`HideBox` 已经自带 `AudioSource.Stop()`）：多停一颗无害，留着是原来那句兜底（同一时刻只有一颗会响）。

---

## ④ 改动清单

### ④-1 `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（+370 / −38）

| # | 落点（符号） | 改了什么 |
|---|---|---|
| 1 | `NetRemoteResign` | `reason == Forfeit` 时 `SpeakConcede(1-_me)`；说不出来出声 |
| 2 | `Begin`（`BuildHud()` 之后） | `_chatPopup.SetChatContext(本局我方督军.Card.Id, Ctx.MatchType)`（每次开局都跑）；`_fieldDissolvedCard = null` |
| 3 | `ClickChatHud` | 补 `+0x78` 闸（`ChatPopUpBlocksLocal`）+ 出声 |
| 4 | `SpeakWarlordLine` | 补 `CanUseVOs(w.Card.Type)`；`clip == null` 不再整句丢（`expectVoice: clip != null`） |
| 5 | `CanAiChat` | 补 `if (ChatPopUpBlocksLocal) return false;` |
| 6 | `DisplayRemoteChat` | 认 `chatId == ChatIdConcede(4)`（走 `SpeakConcede`，绕 `CanChat`）；越界提示文案同步 |
| 7 | `SpeakConcede` | `void → bool`（说不出来要能被调用方看见） |
| 8 | `SpeakHurry` | 补 `if (!CanLocalChat()) return false;` |
| 9 | `SpeakCantDo` doc | 写死「本方法**不过** `CanChat`」的判据链（防下一个会话照施工单补错） |
| 10 | `Forfeit` | 认输：`SpeakConcede(_me)` → `_net.OnLocalChat(ChatIdConcede)` |
| 11 | `TryBeginPlayTargetPick` | 非单位 targeted 卡进等目标态 ⇒ `DissolveWholeCardInField(card)`；「又拖一张」那支补 `UndissolveFieldCard(prevCard)` |
| 12 | `ResolvePlayTargetByUnit` | 回合易主 / 取消两支各补 `UndissolveFieldCard(...)` |
| 13 | `DoPlay` 战术支 | `SetAlpha` → **`SetAlpha2D`** + 显式 `Ease.OutQuad`；清 `_fieldDissolvedCard` |
| 14 | `DoPlay` 引擎拒绝支 | 补 `UndissolveFieldCard(card)`（否则那张牌回手后是**透明**的） |
| 15 | `ShowAsk` | 开选牌面板前 `_chatPopup.ForcePopUpClose(immediate: !Application.isPlaying)` |
| 16 | `StartIntroMonologue` | 真起播时 `DisableChatPopUp()`；三条早退各 `EnableChatPopUp()` |
| 17 | `TickIntroMonologue` | 收尾 `EnableChatPopUp()` |
| 18 | `CloseTutorialChatBoxes` | `HideAll()` → 按颗 `HideBox(...)`（含 `isPlayer` 算法） |
| 19 | `InvokeCueFieldFor` | 条件改 `legendary && !Ambush`；doc 就地订正 |
| 20 | `LandDustFieldFor` doc | 就地订正「两条规则故意不同」→ **同形** |
| 21 | 两处过期注释 | 「`Speak` 开头 `HideAll` ⇒ 只有一颗气泡」→ 订正为 A13 三槽后的真实形状 |
| 22 | **类尾**（新增成员，⛔ 没插进已有成员之间） | `_chatPopUpBlocked` · `ChatPopUpBlocksLocal` · `CanLocalChat()` · `DisableChatPopUp()` / `EnableChatPopUp()` · `ChatPopUpBlockedForTest` / `ChatPopUpDisabledForTest` · `ChatIdConcede` · `CanUseVOs(string)` + `EnableTroopVOs/EnableWarlordVOs`（🔶）+ `CanUseVOsForTest` · `SetTransformedWarlordNewChats(string,bool)` · `_fieldDissolvedCard` + `FieldDissolvedForTest` + `DissolveWholeCardInField` + `UndissolveFieldCard` |

### ④-2 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（+64 / −14，只在 ChatPopup 那一段）

1. **①b 那条断言换判据**（原来断「6 颗钮 = `Battle/Chat/*` 分类词」）：
   现在断 ① 面板 warlord == 引擎侧本局我方督军（前提）② 6 颗钮逐字 == `Loc.T("Chat_Messages/<战将>-Chat" + (5+i))`
   **且不等于分类词**（灭自证：没接线 ⇒ 退回分类词 ⇒ 红）③ 换督军（AM1 / BL34）文字跟着换，然后还原。
   ⚠️ 期望值取自 **官方表**（`Loc.T`），⛔ 不是读我们自己的常量；`Loc.T` 缺键会回键名，已排掉那一档（否则「等于自己」会假绿）。
2. **新增 ①c 四条**：`CanUseVOs`（hero/unit/tactic 都放行）· 弹窗开着 ⇒ `ChatPopUpBlockedForTest && !CanAiChatForTest` ·
   弹窗关回去 ⇒ 闸撤掉（灭自证：把两个量合成一个，这两条必有一条红）· `ForcePopUpClose`（关着返 false / 开着真关）·
   `SetTransformedWarlordNewChats` 转发（BL34 的 `-Chat5`）。
   ⚠️ 这一节跑完面板的 warlord 停在 **BL34**（后面几节不再读 `TextAt`，现核无影响）。

### ④-3 账补丁 `Unity/资料/完全复刻/账/_parts/B9_E4BDR.tsv`（16 行）
- 生成器 `_parts/_gen_B9_E4BDR.py`（可复跑）；11 列表头逐字照抄主表，**只改 `差异`/`备注`/`轮次`**，`状态` 原样带走。
- 定位靠 **`原版类 + 方法`**（⛔ 不按行号；主表正被多路并行改，施工单的行号已漂 1–2 位）。
- 覆盖：`音频_语音.tsv` 13 行（`GetInvokeCue` · `CanChat` · `CanUseVOs` · `CloseChatBox` · `DisplayWarlordChatMessage` ·
  `DisplayWarlordRegularChatMessage` · `ForcePopUpClose` · `GenerateVoiceLineData(ChatMessage,bool)` · `SetPopUpDisabled` ·
  `SetPopUpEnabled` · `SetTransformedWarlordNewChats` · `SetUpChatOptions` · `ShowHeroesIntroMessage`）
  ＋ 三份主表的「出对半」横跨行：`战斗界面_HUD.tsv` `MoveIntoFieldFromHand()` · `特效动画.tsv` `DissolveWholeCard(float)` ·
  `战斗逻辑驱动.tsv` `UnDissolveWholeCard()`。
- 校验：`merge_ledger_parts.py --dry` = **会更新 56 行 / 新增 0 / 无变化 0**（含别家补丁），我的 16 行**全部恰好命中 1 处**；
  `check_patch_conflicts.py` = **✅ 没有跨补丁重复键（3 份补丁）**。

---

## ⑤ 该跑哪条宿主自检

- **必跑 `BattleScene.Run`**（`-executeMethod BattleScene.Run`）。
  理由：本批只改战斗侧两件（`BattleDriver` 建 `ChatPopup`/`UnitChatPanel`；`Editor/BattleScene.cs` 就是它的宿主）；
  `Shell / MainMenu / Rewards / Shop / Collection / Settings / Deck` 一个都不引用这两个文件（现核）。
- **不必**：`RuleEngineTest`（没碰 `RuleEngine/**` —— 见 ① 那句「别家代理在写」）· 联机三条（没碰 `Net/**`）。
- 🔴 **跑到本批时请重点看这三处**（最可能红）：
  1. **ChatPopup 那一段的 ①b/①c** —— 新判据；`AM1`/`BL34` 的 `Chat5…10` 已核（官方表 10,862 键里都在）。
  2. **`AI 回话` 那一节**（`CanAiChatForTest`）—— 现在多一道「弹窗开着 ⇒ 不许聊」；`SpeakChatAt` 是**先 `Hide()` 再说话**，
     所以正常路径不受影响；若哪条夹具在**面板开着**时断言 AI 说话，那条会红（且**那是原版行为**）。
  3. **两段式选目标的 19·a/19·b 两节** —— 夹具都是**单位卡**（现核 `targeted: 1` 只有两处、`type` 都是 `unit`）⇒ 不溶卡、不受影响。
     新账「出对半」**今天没有自动断言**（缺非单位 targeted 夹具）—— 建议调度台补一条：
     `FixtureTactic(targeted:1)` 落位 ⇒ `FieldDissolvedForTest == true` 且 `HandViewAt(...).Alpha2D == 0`；
     点空白取消 ⇒ `FieldDissolvedForTest == false` 且 `Alpha2D == 1`（批处理档 α 是当场落的，可断）。

---

## ⑥ 顺手发现（⛔ 本批没改，交调度台分流）

1. 🔴 **账的 `文件:行号` 会大面积漂**：我在 `BattleDriver.cs` 前中段插了行（`+370/−38`）⇒ 指向该文件的 A 行需要
   `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`（**只能在「没有 `.cs` 写手 + 补丁已合并完」时跑一次**）。
   我的新增成员**全部追加在类尾**（那些落点不受影响）。
2. 🔴 **`NetBattle.OnLocalChat` 的闸 = `IsButtonLine(5…10)`**（`Net/NetBattle.cs` 那一带）—— 它同时挡掉了「认输那句」`chatId=4`。
   `A-06` 改 `IsRelayable` 之后才通（本报告 ③-①）。
3. **`Loc.HasEntry` 只认手写 `Table`**（`B2AU` 已报过一次）：本次我在自检里改用 `Loc.T(key) != key` 的写法，
   ⛔ 别再用 `HasEntry` 判 `Chat_Messages/*` 那一族（1,736 条全在官方全表）。
4. **`CardFeel.ReturnToHand` 不碰 `Alpha2D`**（只 `ArtAlpha`）：谁要是把 `SetAlpha2D` 用到别的离场路上，
   取消/回手那一档会留一张**透明卡**（本批的 `UndissolveFieldCard` 就是为此补的）。建议在 `ReturnToHand` 的 doc 里加一句提醒（该文件不在本波白名单）。
5. **`ChatPopupPanel.SetChatContext` 会连 `_matchType` 一起写**：自检里换督军验文字时**必须**把同一个 `MatchType` 传回去，
   否则 `IsAvailable` 那一档会跟着变（我这一节传的都是 `pdrv.Ctx.MatchType`）。

---
---

# 第二批（2026-10-27 · 调度台加派 6 件）

> 白名单追加：`CardPresentation/Battle/SkillPanel.cs`（第 2 件要求「给 `SkillPanel.Show` 补三个形参」⇒ 只做**追加可选形参**，+77/−2）。
> 类型检查：`TMPDIR=/tmp/wf_E4BDR bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（收尾那一跑，三个文件都已改完之后）。
> 行尾：`BattleDriver.cs` / `BattleScene.cs` **纯 CRLF**、`SkillPanel.cs` **纯 LF**（都是原样，没翻）——二进制现读。

## ②-① 六件逐条

| # | 件 | 结论 |
|---|---|---|
| 1 | **20 `SetupActiveSkill`**（后缀闸） | ✅ `ShowSkillPanel` 的闸从**卡级**换成原版的 **ability 级**（`ActiveAbility +0x40`）；**并就地订正两条过期口径**（见 ②-③，这条建议**重要**） |
| 2 | **755 `GetCurrentLocalizedAbilityDescription`** | ✅ 三档后缀链落地（`-load` / `-effect` / 裸键 + `GenericSiegeText` 兜底），`SkillPanel.Show` 加 **5 个可选形参**并接上；`RemovingSiege` 那一支**判据没解出来** ⇒ 出声 + 退回裸键（⛔ 没猜） |
| 3 | **134 `ShoultSetAiOpponent`** | 🟡 **纯函数落了、但没接线 —— 请裁**（理由见 ②-④：原版这个方法 **private + 全量反编译 0 调用点**，而接到 `DriveAiTurn` 会让 7 个模式的单机局**整局没人出牌**） |
| 4 | **22 `CompleteBattleFinished`** | ✅ 四条残余全补：2.0 s **只在认输时等** · `+0x1c == 0` ⇒ **整条协程结束**（可测字段）· 第二段等待 **0.25 s** · `LeaveBattleRoom(force = 对面认输)` |
| 5 | 文案过期（0.65 → 0.75 / 四段 → 五段） | ✅ `BattleDriver.cs` **9 处** + `Editor/BattleScene.cs` **3 处**，全部**行数中性** |
| 6 | 「出对半」自动断言 | ✅ `Editor/BattleScene.cs` 新增 19·c 一节（3 条**前提**断言在前，2 条结论断言 × 2 段） |

## ②-② 证据（本轮亲读）

### 20 后缀闸 = **ability 级**
- `cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:160`：`TargetCriteria tc = activeAbility.targetsAvailableCounter;`
  `if (activeAbility.targetsAvailableCounter != null && tc.targetsAffected != TargetsAffected.any)` ——
  `activeAbility` 来自 `parentCard.GetActiveAbility()`（**`ActiveAbility` 对象**）⇒ 读的是 **`ActiveAbility +0x40`**，
  ⛔ 不是卡级 `RawCardScript.targetsAvailableCounter(+0x298)`（那一格 = `CardScript.ShouldShowTargetsAvailable` = 手牌下方「N 个可用」那行）。
- 我们读不到那一格（`CardDef` 只落卡级 `avail` 行）⇒ `BattleDriver.ShouldShowAbilityTargetsCounter` 今天恒 `false`
  （= 原版「字段为 null」那一支）。**数据现扫**（`资料/普查产出_1018/allcards_1350/cards_raw.json`，1,351 张逐卡）：
  `activeAbility.targetsAvailableCounter.targetsAffected != 0` = **0 张**；`alternativeAbility` **0**；`cardAbilities[]` **0**；
  卡根级非 any = **17 张**，而那 17 张 `hasActiveAbility` **全 0**（⇒ `SetupActiveSkill` 对它们根本不会跑）。

### 755 三档后缀链
- `CardScript__GetCurrentLocalizedAbilityDescription.c` 全文 + ILSpy `CardScript.cs:17102-17157`：
  `"-load"`（未加载）→ 缺键落 `Card_Abilities/GenericSiegeText`；`"-effect"`（已加载）→ 缺键落裸键；否则 `GetLocalizedAbilityDesc()` 裸键；
  第三支（`ability.needsLoading && HasRemovedTrait(0x1ea reload)`）⇒ `GetLocalizedAbilityDescRemovingSiege()`。
- `DoesAbilityRequireLoading`（`CardScript__DoesAbilityRequireLoading.c:13-38`）= `hasActiveAbility(+0x220)` ∧ 遍历 `activeEffects(+0x108)`
  **无** `HasRemovedTrait(reload)` ∧ `ability.needsLoading(+0x22)`。
- 键拼法 = `RawCardScript__GetLocalizedAbilityDesc.c:19-27`：`"Card_Description/" + (textReferenceId ?? uniqueId) + suffix`
  （`IsNullOrEmpty` 为假才采信 ⇒ 缺键往下落）。
- 我们的输入：`CardDef.HasActiveAbility`（v21）· `CardDef.ActiveAbilityNeedsLoading`（v21，现读 1,351 张**全 0**）·
  `UnitState.HasRemovedTrait("reload")`（`ConditionEval.ReloadTrait`）。`activeAbilityLoaded(+0x224)` 我们**没有装填机制** ⇒ 恒 false。

### 22 `CompleteBattleFinished`
- `BattleManager._CompleteBattleFinished_d__389__MoveNext.c` 状态机三段（state 0 / 1 / 2）+ `dump.cs` 字段表：
  `BattleManager +0x500 = RankedMatchResult _currentMatchResult` ⇒ `+0x1c finishedProcessingResult(bool)` ·
  `+0x24 battleResult(BattleResult)` · `+0x28 battleWinner(BattleWinner)`。
  ⚠️ **不是** `MatchResultData`（那一个的 `+0x1c` 是 `showAdAfterMatch`）—— 按错那张表会得出反判。
- 常量：`0x1834b2bbc` = **2.0f**（既有记录）· `0x1834b2e7c` = **0.25f**（本轮按调度台给的值落地，与本报告第一批的「还没读出来」相反）。
- `BattleNetworkManager__LeaveBattleRoom.c:29-41`：`if (param_2 == 0) NetworkCustomManager__RemoveRoomAfterLeaving(...)`
  ⇒ `force` **只**影响「服务器房间删除」；我们没有那一步。

### 134 `ShoultSetAiOpponent`
- `decomp_full/MatchData__ShoultSetAiOpponent.c` 全文 = `u = playMode - 3; if (u < 10) switch(u) { case 4/7/8: break; default: return 1; } return 0;`
  （ILSpy `MatchData.cs:764-778`：`obj <= 9` + 一张跳表，同形）⇒ AI = {3,4,5,6,8,9,12}。
- 🔴 **`private` 且 0 调用点**：`grep -rl ShoultSetAiOpponent d:/2/tools/decomp_full` 只命中它自己那份；
  `grep -rl "180736af0"`（按 VA）也 0；ILSpy/cpp2il/dump.cs 三份里都只有定义。

## ②-③ 🔴 「账写错了」清单（本批新查出的，建议调度台回写）

1. **`战斗界面_HUD.tsv` 行 20 的差异句「池内 17 张该拼」是错的**（`B8_BL` 的口径）——
   那 17 张 `hasActiveAbility == 0`，`SetupActiveSkill` 对它们**根本不会跑**；而带主动能力的 88 张，其
   **ability 级** `targetsAvailableCounter` **全是 any** ⇒ **原版在这份数据上永远不拼那个后缀**。
   （已在 `B9_E4BDR2.tsv` 该行的 `差异` 列就地写更正。）
2. **`B9_B2AU` 第三批报告 §「三条更正」第 1 条**说「真断点在 `CardDef`/`CardDatabase` 只落卡级 + `ShowSkillPanel` 读卡级」——
   **前半对、后半已修**；但同一段里「生成器不 emit ability 级」的反驳**成立**（我复核：`gen_cards_truth.py:296-303` 在 `compact_active` 里 emit）。
   ⇒ 那条**建议保留更正痕迹**，别把「池内 17 张该拼」再抄回来。
3. **施工单 `B9_音频C_派活包.md` §A-05 第②步**把 `SpeakCantDo` 与 `SpeakHurry` 写成「都过 `CanAiChat()`」——
   `BattleTipController__NotifyCantDoAction.c:11` 传 `skipCanChat = 1` ⇒ **`cantdo` 原版绕开这道闸**（第一批已按 `.c` 只做 Hurry）。
4. **`战斗界面_HUD.tsv` 行 702（`CompleteBattleFinished`）原写「② uiState 那一档没做、常量没读出来」**——
   `+0x1c` 是 `finishedProcessingResult`（不是 `uiState`）、常量 = **0.25 s**；`+0x500` 那张对象是 `RankedMatchResult`（不是 `MatchResultData`）。

## ②-④ 需要调度台裁的一件事（134）

**两个候选落点，各有代价**（我没有擅自选）：
- **(a) 只接 `botSeatThisGame`**（= match-data 语义：「对面是不是电脑」）⇒ 引擎行为会变（防御卡路由 / `MatchTypes.Effective` 的 matchType 翻转）
  ⇒ 可能动到 `ruleprobe` 指纹与 `BattleScene` 里依赖 `Ctx.BotSeat` 的断言。
- **(b) 接 `DriveAiTurn`** ⇒ `Classic/Duel/PracticeLodge/ClosedDeck/Replay/RankedFriendly/Skirmish` 的**单机局整局没人出牌**（stall）。
- **(c) 维持现状**（`AiShouldDriveOpponent = _net == null`）+ 把纯函数留在类尾（本轮采用），等原版那个消费方被解出来再定。

## ②-⑤ 改动清单（第二批）

| 文件 | 增/删 | 内容 |
|---|---|---|
| `Battle/BattleDriver.cs` | （累计 +528/−65，含第一批） | `ShowSkillPanel`：ability 级闸 + 5 个新实参 / `DoesAbilityRequireLoading` 的三条输入；`CompleteBattleFinished` 三段（认输才 2.0 s · `finishedProcessingResult` 闸 · 0.25 s · `force`）；`EndPanelRealtimeWait → EndPanelForfeitHold` + `EndPanelPostWait`；`_endPanelGateDone` / `_matchResultProcessed`(+`SetMatchResultProcessedForTest`)；`LeaveNetRoom(force)`；**类尾**：`ShouldShowAbilityTargetsCounter` · `ShoultSetAiOpponent(GameMode)`(+`...ForTest`)；**9 处 0.65 → 0.75 文案** |
| `Battle/SkillPanel.cs` | **+77 / −2** | `Show` 追加 5 个**可选**形参；`_descBase` 改走 `AbilityDescription`（三档链，缺键判据 `Loc.T(k) != k`）；类尾 `AbilityDescription` + `ResetAbilityDescWarnForTest` |
| `Editor/BattleScene.cs` | （累计 +129/−20，含第一批） | 新增 **19·c** 一节（出对半：3 前提 + 4 结论）；**3 处「四段」→「五段」/ 补 `LandTailTime` 那一项** |

账补丁：`账/_parts/B9_E4BDR2.tsv`（**7 行**，生成器 `_parts/_gen_B9_E4BDR2.py`）——
`ActiveSkillDisplay.SetupActiveSkill` · `CardScript.GetCurrentLocalizedAbilityDescription()` · `BattleManager.CompleteBattleFinished()` ·
`MatchData.ShoultSetAiOpponent(PlayModes)` + 出对半那三行（追加「自动断言已加」）。
`merge_ledger_parts.py --dry` 通过；`check_patch_conflicts.py` ⚠️ **2 条与 `B9_B2AU2.tsv` 重键**
（`SetupActiveSkill` / `GetCurrentLocalizedAbilityDescription`）⇒ **合并顺序：先 `B9_B2AU2.tsv`、后 `B9_E4BDR2.tsv`**（后者的内容更新，应赢）。

## ②-⑥ 该跑哪条宿主自检（不变）

**`BattleScene.Run`**（三个文件都只被战斗侧引用）。
本批新增/改动的断言最可能红的地方：
1. **19·c 出对半那一节** —— 全新；夹具是 `tactic` + `targeted: 1`（落空格避开「松手即结算」快捷路）。
2. **技能面板那几条**（`TargetsText` / `DescText` 结尾）—— 现在**默认不再拼后缀**（`targetsAny: true`）。
   现有断言里 `Ironclad` 那条本来就断「空」（✅ 不受影响）；「拼法」那几条是**显式** `SetTargetsAvailable(...)` 喂的（✅ 不受影响）。
   ⚠️ 若还有别的地方**默认**读 `TargetsText` 非空 ⇒ 那条会红，而**那是原版行为**（这份数据里不拼）。
3. **结算面板**那几条 —— 批处理档两段等待都跳过（`Application.isPlaying == false`），行为与改前**逐字相同**。

## ②-⑦ 收尾补记（同批 · 铁律 5）

技能面板那条**旧断言**（`Editor/BattleScene.cs`「面板那行**闸**」）原来断的是**卡级**
`CardFaceRules.HasTargetsAvailableCounter` —— 与上面第 1 件改后的判据**不同源**了（它还会绿，但断的是旧口径）。
已同批改成断 `BattleDriver.ShouldShowAbilityTargetsCounterForTest(...)`（新增的**只读自检口**，生产唯一读点仍是 `ShowSkillPanel`），
并在断言文案里写清「ability 级 `+0x40` 我们读不到 ⇒ 整段不拼」。
⇒ 现在这一条**真的在断新闸**（把闸改成恒拼 ⇒ 红）。

最终三件规模（本会话累计）：`BattleDriver.cs` **+533/−65** · `Editor/BattleScene.cs` **+134/−21** · `Battle/SkillPanel.cs` **+77/−2**。
行尾：前两个 **CRLF**、`SkillPanel.cs` **LF**（都是原样，`bareCR = 0`）。

---
---

# 第三批（2026-10-27 · 9 件：特效包 J-05 七条 + 引擎侧两条）

> 类型检查：`TMPDIR=/tmp/wf_E4BDR bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（收尾那一跑）。
> 行尾：`BattleDriver.cs` / `Editor/BattleScene.cs` 仍是**纯 CRLF**（`bareCR = 0`）。
> 本会话累计：`BattleDriver.cs` **+681/−71** · `Editor/BattleScene.cs` **+171/−21** · `Battle/SkillPanel.cs` **+77/−2**。

## ③-① 逐件

| # | 账 | 结论 |
|---|---|---|
| 1 | **505** `ApplyMovementDisappearAnimRotations` | ✅ **1 行**：`SlerpUnclamped` → **`LerpUnclamped`**；注释里「必须 `SlerpUnclamped`」就地订正为「必须 `Unclamped`、原版用 `Lerp`」 |
| 2 | **508** `DissolveWholeCard` ③④⑤ | ✅ 补 3 句（落点 `StopCardUiExtrasForDissolve`，**两处**调用：`DissolveWholeCardInField` + `DoPlay` 战术支）：⑤ `SetHighlight(Normal)` 有真对应层；③④ **没有那两层** ⇒ **出声一次** |
| 3 | **403** `SwarmVFXController.Initialize` | ✅ 补 `v.SetBody3DVisible(false)`；**材质交接 = 不换**（判据见下）；+ 自检断言 ⑤（含前提断言） |
| 4 | **507** `CardToMinionAnim` | 🟡 **只补第③步 `ToggleBody3D(true)`；`PlayInvokeAnimation` 那一半仍未接**（两条硬理由，见 ③-③） |
| 5 | **175** `RotateAnim` | ✅ **0 行行为改动**：`pt==5` 那一支多写一格**只读读数** + `BattleScene` 两条断言（含**灭自证**） |
| 6 | **498** `StopTargetedSpellAnim` | ⏸ **按指令 0 行**（判据待读；前置 = 逐卡 `targetedSpellAnim` 表） |
| 7 | **510** `ForceDisableCanActAnim` | ⛔ **D（证据本波独立复核过，见 ③-④）**，0 行代码 |
| 8 | **68 的消费点** | ✅ `BattleDriver.Begin` 调 `ShellRuntime.Instance.StopMenuMusic(instant: false)`；**`instant` 实参本波现读 = 0(false)** |
| 9 | **532 的消费点** | ✅ `PlaySignal` 加 `EvtKind.EnchantmentRemoved` 分支（②无条件 `CancelPendingAnimsOf` / ③`15\|16` 才 `RemoveDestroyableAnims`） |

## ③-② 证据（本轮亲读）

- **505**：`CardScript__ApplyMovementDisappearAnimRotations.c:54` = `Quaternion.LerpUnclamped(originalRotation, q, t)`；
  ILSpy `CardScript.cs:12713-12758` 同。
- **403**：`SwarmVFXController__Initialize.c:20-22` = `mat = renderer(+0x20).GetMaterial()` → `CardMaterialHelper.ConfigureMaterial(mat, card)`
  → `CardUI(+0x290).ToggleBody3D(false, null)`。`ConfigureMaterial` 的**全部内容**只有两条 `SetTexture`
  （`_CardImage` = 卡面立绘纹理 · `_MatCap` = 按 `CardTier` 取的 matcap —— `CardView.ConfigureRemnantMaterials` 的 doc 逐行读过），
  而它的目标是 **`SwarmVFXController` 自己的 renderer**；我们的 `WarpforgeEffectPlayer` 没有「按卡改写特效材质」这条路 ⇒ **不换 + 出声**。
- **507**：`BattleCardUI__PlayInvokeAnimation.c:20-45` 四步 = `SetCardMaterial(...)` → `Animation.Play(<clip>)`（`speed = 1.0`）
  → **`ToggleBody3D(true)`** → `DelayedResetMaterial(CardScript.GetHandToBoardAnimEventTime(card))`。
- **175**：`CardAnimController__RotateAnim.c:31-127`（pt==5 支）—— 这条上一轮 `J4FX` 已核，本轮只加只读读数。
- **510**：见 ③-④。
- **68**：`BattleManager__OnInitializationComplete.c:32` = `MusicManager__StopMusic(<实例>, 0, 0)` ⇒ `instant = false`（3 s 淡出）。
- **532**：`E7ENG` 的 §④·4 给了逐字代码；本波照抄并把 `EvtKind.EnchantmentRemoved` 的「🔶 自己设计」出处一并记在账里。

## ③-③ 507 为什么**只做一半**（⛔ 不是我偷懒）

1. **那支 clip 的名字没读成字面量**：`BattleCardUI__PlayInvokeAnimation.c:20-22` 取的是
   `**(DAT_1842c3528 + 0xb8)` = `BattleCardUI` **静态块里的字符串**（不是逐卡序列化字段）。
   旁证很强 —— 同一方法末尾 `DelayedResetMaterial(GetHandToBoardAnimEventTime(card))` 用的正是那条动画的**事件时刻**
   —— 但**旁证不是字面量**（与 508 的 `Vector3.one` 同一档，那里也标了「判据充分、非字面量直读」）。
2. **我们的等价片对 Board 形态直接不播**：`CardFeel.HandToBoard` 第一句
   `if (card.Face == CardFace.Board) return null;` —— 而 AI / 自动打出这条路的落地视图**已经是 Board 形态**
   （`SyncBoard` 建的）⇒ 直接复用**什么都不会发生**；要播得先把它退回手牌形态（改形态语义 ⇒ 牵动
   `SyncBoard` / 卡面 / 高亮三条链）⇒ 本波**不擅自做**。
3. 已做的部分：`LandAiDeployFlight` 里 `v.SetBody3DVisible(true)`（③）+ `WarnInvokeAnimNotWiredOnce()`（**出声一次**说明另一半没接）。
   ⚠️ 玩家部署那条路**已经**在 `StartHandToBoardClip`（0.3 s）播过同一支 ⇒ 本行**只服务 AI / 自动打出**，⛔ 别加两处（会播两遍）。
   📌 **要闭合**：① VA 反汇编 + `.rdata` 读那串 clip 名；② 单独立账裁「AI 落地视图要不要先退回手牌形态」。

## ③-④ 510 判 D 的证据（本波独立复核，与 `J4FX` 同结论）

| 查法 | 结果 |
|---|---|
| `grep -rl ForceDisableCanActAnim d:/2/tools/decomp_full` | **只命中 `CardScript__ForceDisableCanActAnim.c` 自己** |
| ILSpy 全树（`out_cpp2il_dev_ilspy`） | 只有 `CardScript.cs` 的**定义** |
| `assets_full`（24.7 万文件）全树 `grep -rl` | **0 命中**；**正控**同树搜 `CardHandToBoardAnimationFinished` ⇒ 命中 `bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3614292623624764332.json`（证明这条搜法有效） |

⇒ **0 代码调用点 + 0 序列化字符串引用** ⇒ 铁律 11 例外①（原版本身不触发）⇒ **判 D、不写代码**。

## ③-⑤ 改动清单（第三批）

| 文件 | 内容 |
|---|---|
| `Battle/BattleDriver.cs` | `MinionAxisRot`：`LerpUnclamped`（1 行）+ 两处注释；`DoPlay` 战术支 & `DissolveWholeCardInField`：调 `StopCardUiExtrasForDissolve`；`PlaySwarmMergeFeel`：②·b（`SetBody3DVisible(false)` + 材质不换出声）；`LandAiDeployFlight`：`SetBody3DVisible(true)` + 出声；`PlayAbilityBeat` 的 `pt==5` 支：写只读读数；`Begin`：`StopMenuMusic(instant:false)`；`PlaySignal`：`EvtKind.EnchantmentRemoved` 分支；**类尾**：`StopCardUiExtrasForDissolve`/`ResetDissolveExtrasWarnForTest` · `_swarmMergeMaterialWarned` · `_lastBoardCenterAngleDeg`(+`LastBoardCenterAngleForTest`/`ResetBoardCenterAngleProbeForTest`) · `_warnedInvokeAnimNotWired`/`WarnInvokeAnimNotWiredOnce` · `_warnedNoShellForMusic` |
| `Editor/BattleScene.cs` | 虫群合并那一节新增 **⑤**（`!Body3DVisible`，**前提断言在前** + 用**合并前**的值还原）；「偏差6」那一节新增 **175 的两条**（`pt=5` ⇒ 读数 ∈ [0,360)；`pt=0` ⇒ 读数仍 −1 = 灭自证） |
| `账/_parts/B9_E4BDR3.tsv` | **9 行**（生成器 `_parts/_gen_B9_E4BDR3.py`）：`ApplyMovementDisappearAnimRotations` · `DissolveWholeCard` · `SwarmVFXController.Initialize` · `CardToMinionAnim` · `RotateAnim` · `StopTargetedSpellAnim` · `ForceDisableCanActAnim` · `MusicManager.StopMusic` · `ResolveRemoveEffect` |

`merge_ledger_parts.py --dry` = 更新 **9** / 新增 0；`check_patch_conflicts.py` = ✅ **没有跨补丁重复键（1 份补丁）**。

## ③-⑥ 该跑哪条宿主自检

**`BattleScene.Run`**（全部落点都在战斗链上）。
最可能红/要看的三处：
1. **虫群合并 ⑤ 的「（前提）3D 体是显示着的」** —— 若那一节视图没有 3D 体（`use3DBoard` 档不同），这条会红，**账 403 的断言要换到 3D 档验**（代码本身不用改）。
2. **`pt=5` 的读数断言** —— 夹具用的是 `Run` 里现成的 `abEvt`/`abVfx`（`Player=bme / Slot=bws`）；若那一节 `enemyBoard` 为 null，`pt=5` 支会走 `BeatSoundOff`、读数保持 −1 ⇒ 第一条红（= 夹具前提问题，会立刻看得见，不会假绿）。
3. **`Begin` 新增的 `StopMenuMusic`** —— 批处理里 `ShellRuntime.Instance` 为 null ⇒ 只出声一次，不影响断言。

---
---

# 第四批（2026-10-27 · 507 的 clip 字面量 + 形态独立裁定 + `BattleScene.Run` 预期清单）

> 本批 **0 行 `.cs` 改动**（纯判据 + 报告）⇒ `typecheck` 与第三批收尾那一跑相同（**运行时 0 / 编辑器 0**）。
> 未跑 Unity、未动 git、未改正本与账主表、未改 `RuleEngine/**` / `CardFeel.cs` / `Shell/**`。

## ④-① 结论：`PlayInvokeAnimation` 播的就是 **`Card Hand To Board`**（字面量已读出）

**三段证据闭环**（⛔ 不再是「旁证」）：

1. **静态块只有一个字符串**：`decomp_full/BattleCardUI__.cctor.c` 全文只有**一句**写静态字段 ——
   `**(undefined8 **)(DAT_1842c3528 + 0xb8) = **DAT_1842cc848**`（= `static_fields[0]`）。
   ⇒ `BattleCardUI` 的**唯一**静态字符串就是 `DAT_1842cc848`。
2. **那个地址的字符串 = `Card Hand To Board`**：`d:/2/tools/il2cpp_out/stringliteral.json` 里
   `{"address": "0x42CC848", "value": "Card Hand To Board"}` ——
   `0x42CC848` 正是 `DAT_1842cc848` 的 RVA（`0x1842cc848 − 0x180000000`）**逐位相同**，且该地址在表里**唯一**。
3. **同一格被 `GetHandToBoardAnimEventTime` 使用**：`CardScript__GetHandToBoardAnimEventTime.c:13-20`
   也是 `Animation.get_Item(***(DAT_1842c3528+0xb8))` → 取该 clip 的 `events[0].time`。
   而 bundle 里 `Card Hand To Board`（`bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3614292623624764332.json`）
   `m_Name = "Card Hand To Board"`、`m_Events[0] = (CardHandToBoardAnimationFinished, **0.55**)` ——
   与 `CardFeel.HandToBoardDone = 0.55`、与 `PlayInvokeAnimation` 末尾
   `DelayedResetMaterial(GetHandToBoardAnimEventTime(card))` **三处对得上**。

⇒ **507 的「是否复用 `CardFeel.HandToBoard`」答：是（clip 归属正确，⛔ 不需要第二支 clip）。**
第三批那句「clip 名没读成字面量」**就此结案**（账补丁会改）。

**复现命令**（只读）：
```
grep -rl DAT_1842c3528 d:/2/tools/decomp_full          # 只有 4 个文件：cctor / PlayInvokeAnimation / PlayBackToHandAnimation / GetHandToBoardAnimEventTime
cat d:/2/tools/decomp_full/BattleCardUI__.cctor.c      # 唯一一句：static_fields[0] = DAT_1842cc848
python -I -X utf8 -c "import json,io;d=json.load(io.open('d:/2/tools/il2cpp_out/stringliteral.json',encoding='utf-8'));print([x for x in d if x['address']=='0x42CC848'])"
# ⇒ [{'value': 'Card Hand To Board', 'address': '0x42CC848'}]
```

## ④-② 形态问题的**独立裁定**（⛔ 本批不改形态语义）

**事实**：`CardFeel.HandToBoard` 第一句 `if (card.Face == CardFace.Board) return null;`，而 AI / 自动打出这条路上
落地视图是 `SyncBoard` 建的、**生来就是 `CardFace.Board`** ⇒ 直接复用**什么都不会播**。

**根因（结构差异，不是少写一行）**：原版 `CardToMinionAnim` 跑在**同一个 `CardScript` 对象**上 ——
那张卡从手牌飞出去、再由 `Card Hand To Board` 这条 clip 自己完成「2D 卡面淡出 + 3D 体溶解出来」的交叉；
我们这条链上**飞的是卡背 quad、落地的场上视图是另一个对象**（B2/B5 那两跳的既定形状）⇒
「在手牌形态下播 clip」这个**前提在我们这侧不成立**。

**三个候选（各有代价，本批只记录，⛔ 不实现）**：

| 案 | 做法 | 代价 / 风险 |
|---|---|---|
| **A** | 落地那一刻把**场上视图** `SetFace(Hand)` → 播 `HandToBoard` → clip 收口时 `SetFace(Board)` | 改的是**形态语义**：`SetFace(Hand)` 会重建手牌那套层（卡框/费用/文字）并改排序队列；`SyncBoard` 每帧都可能 `SetData`/重排 ⇒ 与补间抢状态（本仓已有「补间期间别让 `SetPose` 回弹」那一族坑）。**牵动 `SyncBoard` / 卡面 / 高亮三条链** ⇒ 必须单独派 + 单独验收 |
| **B**（现状） | 只做 `ToggleBody3D(true)`，clip 那一半留白 + 出声 | AI/自动打出这张牌**少了 0.9167 s 的登场动画**（原版有）；玩家拖拽那条**有**（`StartHandToBoardClip`）⇒ 两条路视觉不一致 |
| **C** | 飞行载体从「卡背 quad」换成「手牌形态的卡视图」，落地后由它自己变形成场上视图 | 等于重做 B2/B5 那条飞行链（`_aiDeployFlights` / `StartAiDeployFlight` / 卡背 / 交接 0.4 s 全靠它）⇒ 影响面最大 |

**建议**：**A**（最小改动、且忠于「同一支 clip 在同一个对象上演」的原意），但必须单独立账，验收口 =
「AI 出单位 ⇒ 落地那一拍能看到 2D 卡面淡出 + 3D 体出来；`Body3DVisible` 在 clip 0.55 前后发生变化；
`PlayInvokeCount` 恰好 1（⛔ 别与玩家那条叠成 2）」。**本批按指令不动。**

## ④-③ 给 `BattleScene.Run` 的**预期清单**（哪条红 = 夹具前提问题、哪条红 = 真回归）

**静态核过的两条前提**（本轮现读）：
- **`enemyBoard` 在 `BattleScene.Run` 里不可能为 null**：`Editor/BattleScene.cs` 的建场段
  （`enemyBoard = eGo.AddComponent<BoardLayout>()` / `playerBoard = pGo.AddComponent<BoardLayout>()` → `driver.enemyBoard = enemyBoard`）
  **无条件**建并挂上 ⇒ `pt==5` 那一支的 `foeBoard != null` 恒成立。
- **`use3DBoard = boardCam != null`**（同一建场段）⇒ 场景档为 **true**；自检里有一节**临时**置 `false` 再翻回 `true`
  （那一节的注释自己写着「翻回来了」）。而 `_body3D` 是 `SetFace(CardFace.Board)` 时**懒建**的
  （`CardView.cs` 的 `if (board && _body3D == null) _body3D = BuildBody3D(...)`），**与 `use3DBoard` 无关**；
  失败只会发生在 `BuildBody3D` 取不到网格/材质（那条路会打 warning，不静默）。

| 断言 | 红 ⇒ 判定 | 怎么确认 |
|---|---|---|
| 虫群合并 **⑤（前提）「3D 体是显示着的」** | **夹具前提问题**（不是 403 回归） | 那句代码是 `v.SetBody3DVisible(false)`，对 `_body3D == null` 是 **no-op**；本仓另有多处断言 `Body3DVisible == true`（说明这份工程里 `BuildBody3D` 正常）。**处置**：把 403 那条换到 `use3DBoard = true` 的段去验，**代码不用改** |
| 虫群合并 **⑤（结论）「`!Body3DVisible`」** | **真回归** | `PlaySwarmMergeFeel` 的 ②·b 那句被删 / 被后面某处 `SetBody3DVisible(true)` 覆盖（例如 `RefreshCanActForView` 或 `SyncBoard` 在这一拍之后又把它打开） |
| **`pt=5` 读数 ∈ [0,360)** | **真回归**（`enemyBoard` 为 null 这条**已排除**，见上） | `ptIsBoardCenter` 分支被删 / `_lastBoardCenterAngleDeg` 那一句被删 / `pt` 的判据从 `== (int)PositionOption.boardCenter`（=5）被改 |
| **`pt=0` 读数仍 −1（灭自证）** | **真回归** | 读数被写进了**通用支**（那会让上面那条失去鉴别力 —— 恒有值也能绿） |
| `Begin` 新增的 `StopMenuMusic` | 不会红 | 批处理里 `ShellRuntime.Instance == null` ⇒ 只 `Debug.Log` 一次；真机那条链有壳 |

## ④-④ 账

本批**没有新账补丁**（第三批的 `B9_E4BDR3.tsv` 已在合并时写进主表；507 那行的「clip 名没读成字面量」这一句
**已由本批结案**，请同步点在那行的 `差异`/`备注` 里补一句：
「2026-10-27 `E4BDR4`：clip 名已读出 = **`Card Hand To Board`**（`stringliteral.json` @ `0x42CC848`，
与 `BattleCardUI.__cctor` 唯一静态字段同址、且被 `GetHandToBoardAnimEventTime` 同址使用）⇒ **复用 `CardFeel.HandToBoard` 在 clip 归属上正确**；
只剩『形态』那一条独立裁定（见 `报告/B9_E4BDR_BattleDriver接线.md` 第四批 ④-②）。」）——
⚠️ 我**没有**动主表（纪律），这条留给你在同步点合并。

## ④-⑤ 反汇编复核（`工具/disasm_va.py`，按调度台给的路线）

`BattleCardUI.PlayInvokeAnimation` = RVA `0x994930`（`il2cpp_out/dump.cs:26449`），`disasm_va` 前 40 条里那两行**逐字节**印证了 `.c` 的读法：

```
180994993  48 8b 05 8e eb 92 03   mov rax, qword ptr [rip + 0x392eb8e]   ; 下一条地址 0x18099499A + 0x392eb8E = 0x1842C3528 = DAT_1842c3528 ✓
1809949B9  48 8b 80 b8 00 00 00   mov rax, qword ptr [rax + 0xb8]        ; = static_fields ✓
1809949C9  48 8b 10               mov rdx, qword ptr [rax]               ; = static_fields[0]（唯一那个静态字符串）
1809949D2  e8 49 69 5a 02         call 0x182f3b320                       ; Animation.get_Item(anim, 那串名字)
```
⇒ 「`PlayInvokeAnimation` 用的是 `BattleCardUI` 的**唯一静态字符串**」这一点由**指令流**坐实（不再只是 `.c` 的伪代码）。

⚠️ 如实记一处**没做**的：`BattleCardUI..cctor` 的 RVA **`dump.cs` 里没列**（IL2CPP 清单省掉了静态构造函数），
所以「cctor 把 `DAT_1842cc848` 写进 `static_fields[0]`」这一步**只能靠 `.c` + `stringliteral.json` 两份产物互证**，
没有第三条指令级证据 —— 但两处**逐位同址**（`0x42CC848`）+ 第三处语义交叉（`GetHandToBoardAnimEventTime` 同址取该 clip 的 `events[0].time` = 0.55）
已经足够定案。

---
---

# 第五批（2026-10-27 · `BattleScene.Run` 16 红修复：5 个根因）

> 定性来源 = `报告/B9_D1BG_BattleScene16红定性.md`。本批改了 **3 个文件**（都是 `Editor/` 或 `Battle/`，白名单内）：
> `Battle/BattleDriver.cs`（E）· `Editor/BattleScene.cs`（A/B/C/F）· `Editor/BattleChatAssertE7.cs`（E 的失败文案，调度台点名授权）。
> `typecheck` = **运行时 0 / 编辑器 0**（收尾那一跑）。行尾：两个 CRLF 文件仍纯 CRLF。

## ⑤-① 逐根因

| 根因 | 条 | 做了什么 |
|---|---|---|
| **E**（#13–#16）`CanAiChat` 新闸 + `_chatPopUpBlocked` 无每局复位 | 4 | ✅ `Begin` 的「本局的账不跨局」块里补 **`EnableChatPopUp()`** ＋ **`_chatPopup.HideImmediate()`**（两个都补，见下）；`BattleChatAssertE7.cs` 的 B0 失败文案补 `ChatPopUpDisabled` / `ChatPopUpBlocked` / `ChatMuted` 三个读数 |
| **B**（#8）R18③ 阈值按旧 `Slerp` 调 | 1 | ✅ 阈值 `xMin < -38f` → **`< -35f`**；文案 `≈ −43°` → **`≈ −37.5°`**；🧨 改坏法描述**改正**（原来写反了）；**数据侧那两条也改成 `LerpUnclamped` 复算**、期望区间 `(-40,-35)`（与行为侧同源，`Slerp` 的 −42.8 会被区间**顶出去**⇒ 也红） |
| **C**（#9）tooltip 拿 `Trait()` 比 `ByLink()` | 1 | ✅ 期望值改 `TipText.ByLink(foundKey)`；**另加灭自证** `ShownBody != ByLink(另一个词)`（`altKey = foundKey=="melee" ? "ranged" : "melee"`） |
| **F**（#12）`ActiveAbility` 桩断言 vs 已实现 | 1 | ✅ 改成「已实现」三格判别式：`Unhandled` **里没有**它（不是桩）＋ `Unresolved` **里有**它（真实现跑了、夹具没写 `unitType` ⇒ `Fail` 出声）＋ 返回 false。⛔ 没动 `RunActiveAbility` |
| **A**（#1–#7）拖拽夹具以 `targeted` 单位卡开局 | 7 | ✅ 挑牌条件加 `Card.IsUnit && Card.Targeted != 1`；**凑不出就注入夹具卡** `SelfTestDragPlain`（+9 能量 + `RefreshAll`）；**硬前提断言** `Check(dragIdx >= 0 && HandViewAt(dragIdx) != null, "（前提）…")`；并在格位断言前加**探针**日志（`awaitingPlayTarget` / 引擎格位 / 场卡视图 / 手牌视图） |
| **D**（#10–#11）`ProfileData.TitleIndex` 读口兜底 | 2 | ⛔ **未动**（定性报告判它要调度台裁：读口兜底 vs 产品可用性；且落点在 `Shell/ProfileData.cs`，不在本波白名单） |

## ⑤-② E 的细节（为什么两个都补）

- **`_chatPopUpBlocked` 确实没有任何每局复位**（全类只有 `DisableChatPopUp` / `EnableChatPopUp` 两个写点）。
  真理据：原版 `+0x78` 属于**每局新建的 `VoiceLinesController`** ⇒ 语义上每局默认 **1（未禁）**；我们复用同一台 driver
  ⇒ 必须显式拨回。真机后果：上一局独白没演完就 Restart ⇒ **整局聊天钮点不开**（`ClickChatHud` 第一道 + `CanAiChat` 三道）。
- **弹窗也收一次**：`PopUpHid()` 那一半在原版跟着**新场景**一起消失；我们同一块面板跨局 ⇒ `HideImmediate()`
  （对「本来没开」是空操作）。
- 落点 = `Begin` 的 `_introStage = -1;` 之后（在 `BuildHud()` 与 `StartIntroMonologue()` **之前**）⇒ 独白真起播时仍会再 `Disable` 一次，次序正确。
- **覆盖**：E7 的 B0 现在就是这条的钉子（再删掉 `EnableChatPopUp()` ⇒ B0 红，且文案会打印三个读数 ⇒ 一眼看出卡在哪一档）。

## ⑤-③ A 的探针（先加探针，再改夹具 —— 按指令顺序）

```csharp
Debug.Log(P + $"   [DBG 16红#1] awaitingPlayTarget={driver.AwaitingPlayTarget}"
          + $" · 引擎格位有单位={c2.Players[0].Board[freeSlot] != null}"
          + $" · 场卡视图在={driver.BoardViewAt(freeSlot, true) != null}"
          + $" · 手牌视图还在={driver.HandViewAt(dragIdx) != null}"
          + "（awaiting=真 ⇒ 这张牌是 targeted 的，夹具挑错了；awaiting=假且格位空 ⇒ 改判「飞行没落地」= 另一条根因）");
```
⇒ 下次复跑这一行就把 D1BG「查不清」那条钉死了（不用再靠「无日志」反推）。

## ⑤-④ 本批**没做**的（逐条写清）

1. **#10/#11（D）**：`Shell/ProfileData.cs` 的 `TitleIndex` 读口兜底 —— 不在白名单，且是「原版判据 vs 产品可用性」的裁定题。
2. **#2 的建议半条**（`missIdx` 挑选再加「这张视图确实还在 `_cards` 里」）：落点在 `CardInteraction`（白名单外），
   而根因是 #1 ⇒ #1 修好后本条自然回绿。**若复跑仍红**，再派给该文件的写手。
3. **`TutorialScript.cs` 四处过期 doc**（`CLAUDE.md` 铁律 5，`E4BDR` 无 `RuleEngine/**` 权限）：
   `:1138` / `:1177` 的「只有 `ActiveAbility` 走得进来」、`:659`、`:877` 附近同一句 —— 实现已接
   `RunActiveAbility`（`TutorialScript.cs:1072`），这四句现在**反向误导**（下一个人会照它把真实现删回去）。
   📌 建议下一个拿到 `RuleEngine/**` 的代理改成：「**`ActiveAbility` 已实现**（`RunActiveAbility`，判据
   `AiScripted__ExecuteAction.c:388-420` + `:608`）；认不出 acting 时 `Fail` + 进 `Unresolved`」。
4. **顺手发现 ①（`RE ✗ 诊断入口过期`，不计入 16）**：反射看到 357 个用例方法、产物只认 356（缺
   `TestB9EngForcedAttackAndShuffleGate`）⇒ 需 `PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`
   **重生成**（⛔ 别手改产物）—— 本批没跑（不在白名单，且要跑 python 生成器属同步点动作）。
5. **顺手发现 ②（E7 B 段没做 `ChatMutedSource` 的隔离）**：C 段存/还原了它、B 段没有 ⇒ 一旦设置窗那条拉式接线真接上，
   B0 会**先**红在静音闸（顺序问题）。落点 `BattleChatAssertE7.cs`（本波只授权了 B0 的**失败文案**）⇒ 留给下一批。

## ⑤-⑤ 复跑时看什么

- `BT` 计数应从 **16 → 0**；`E7` 从 **4 → 0**（E7 那四条最可能的残余 = #13 若其实是「弹窗可见残留」以外的东西 ⇒ 新文案会直接打印出来）。
- `#10/#11` 两条（D）**预期仍红**（本轮没改、等裁定）⇒ 合成后应是 **2 红**，其余全绿。

---
---

# 第六批（2026-10-27 · 刀 2a：被拒动作的 `ctx.Events` 撤回）

> 判据 = `报告/B9_D6NB_NetBattle第9步分叉.md`（算术闭环：4 × 31² = **3844** 与实测指纹差**一字不差**）。
> 改动只一个文件 `CardPresentation/Battle/BattleDriver.cs`；`typecheck` = **运行时 0 / 编辑器 0**；行尾仍纯 CRLF。

## ⑥-① 做了什么

`LocalAct`（本地动作的**唯一**包装）里：

```csharp
int evBefore = Ctx.Events.Count;          // ① 调用引擎【之前】记水位
int code = apply();
if (code != RuleCodes.OK) DiscardEventsAddedSince(evBefore, act, code);   // ② 非 OK ⇒ 撤回 + 出声
```

新增 `DiscardEventsAddedSince(int count, AiAction act, int code)`（类尾）：
- 把 `Ctx.Events` 里自 `count` 之后的尾部**整段 `RemoveRange`**；
- 🔴 **撤回前先把每一行原样打进 `Debug.LogWarning`**（⛔ 不许静默 —— 这正是不许静默失败那条）；
- 累计读数 `DiscardedEventLineCountForTest`（只读，供以后的钉子用）；
- **守卫**：`count` 越界 / 期间被 `BattleContext.Log` 的 2000 条上限从**头部**裁过 ⇒ **什么都不动**
  （宁可让指纹照旧不等，也别乱截别人写的事件）。

⛔ **只截 `Events`，不碰 `Signals`**（`Signals` 是表现层队列，消费者与语义都不同）。
⚠️ 撤回点在 `ReportUnaskedChoices()` **之前** —— 那一跳自己也会 `ctx.Log`，它写的是「引擎替我挑了几处」，
不属于「被拒那条动作的过程日志」，不该一起撤。

## ⑥-② `LocalAct` 的**出口清单**（逐个交代，别只补一处）

| # | 出口 | 在 `apply()` 前/后 | 要不要撤 `Events` | 现状 |
|---|---|---|---|---|
| 1 | `if (_replaySession) { …; return RuleCodes.ErrBadHand; }` | **前** | **不需要**（一行引擎代码都没跑 ⇒ 0 行追加） | 原样 |
| 2 | `int code = apply();`（**唯一**的引擎调用点） | — | — | 已加水位 |
| 3 | `code != OK` ⇒ 落到末尾 `return code;` | 后 | **要** | ✅ `DiscardEventsAddedSince(...)` |
| 4 | `if (_net != null && code == RuleCodes.OK) _net.OnLocalAction(act);` | 后 | 只发成功那条（本批**没动**：被拒**不该**广播，这口径是对的） | 原样 |
| 5 | `if (code == RuleCodes.OK) RecAct(...)` / `if (code == RuleCodes.OK) _actionsThisTurn++;` | 后 | 只记成功 | 原样 |
| 6 | `return code;` | 后 | 统一出口（上面 3 已覆盖） | 原样 |

⚠️ **`apply()` 抛异常**不在这张表里：异常会**原样往上抛**（本批**没有**包 `try/catch` —— 包了就是吞异常，
违反「不许静默失败」）。如实记：若某天真有引擎抛异常，那条路的 `Events` 尾部不会被撤（那时远不是指纹问题）。

## ⑥-③ 为什么这个修法不会掩盖真 bug

- **只撤「本机试了一条、引擎拒了」的那条动作**：它本来**不广播**（#4），两端局面因此**相同**；
  撤掉的只是「描述一次没有发生的结算」的诊断文字 ⇒ 撤完之后两端 `Events.Count` 一致、`PlayerHash` 本来就一致。
- **引擎真出问题（两端跑同一条动作却得出不同局面）时 `PlayerHash` 仍然不等** ⇒ 分叉照旧会被抓到
  （`NetProtocol.Fingerprint` 里那两个 `PlayerHash` 没动）。
- **不允许静默**：撤回的每一行都进 `LogWarning`；累计读数 `DiscardedEventLineCountForTest` 可被断言。
- `ApplyLoggedAction`（收到的那条动作的落地口）**故意不撤**：那一侧两端跑的是**同一条**动作 ⇒ 对称；
  若某端被拒，那是**真的分叉**，⛔ 别用撤来掩盖。

## ⑥-④ 顺带（只写判据，⛔ 没改）

`NetProtocol.cs:574` 那句注释说 `ctx.Events` 是「**待播事件队列**、表现层随时抽干」—— **与事实不符**：
`BattleContext.Log` 只是 `Events.Add` + 2000 条上限（`BattleContext.cs:1855-1859`），
**全仓没有任何 drain**（本轮再核：`Ctx.Events` 的写点只有 `Log`，读点里也没有 `Clear`/`RemoveRange(0,…)` 之外的抽干）。
⇒ 结论（「别拿 `Events` 当局面判据」）仍然对，但**理由要改**：
「它是**只增不减的诊断日志**（除上限裁剪），而 `Fingerprint` 把它的**条数**算进了指纹 ⇒ 任何一端多写一行就分叉」。
落点 `Net/NetProtocol.cs` **不在本波白名单** ⇒ 留给联机线写手（`铁律 5`：那是「写错的路由」，不是笔误）。

## ⑥-⑤ 顺手就地订正（`铁律 5`）
`WarnInvokeAnimNotWiredOnce` 的日志原文还写着「**clip 名没读成字面量**」—— 第四批已经读出来了（`Card Hand To Board`），
那句现在会**反向误导**。已就地改成「clip 名已读出 = `Card Hand To Board` ⇒ 复用 `CardFeel.HandToBoard` 在 clip 归属上正确；
仍差的是**形态**」。

## ⑥-⑥ 复跑时看什么

1. **`NetBattleTest` 第 9 步**：`SendFingerprint` 两端相等 ⇒ 原来那条 `Abort` / `LeaveWithoutResult` 不再出现
   （这是本缺陷的**正解口**；预期 `通过 243 / 失败 5` → 失败数下降）。
2. 日志里会出现一条 **`[Battle] 本机动作 ... 被引擎拒（...）⇒ 它写下的 N 行诊断日志已从 ctx.Events 撤回`**
   —— 看到它就说明这条路被走到了、且撤了 N 行（N 应为 **4**，与 D6NB 的算术闭环一致）。
3. `BattleScene.Run`：单机局不受影响（`_net == null` 时那条撤回**照样执行**，但指纹没人比 ⇒ 唯一可见变化是
   日志里多那一行、以及 `Events` 里少那几行诊断；现有断言没有依赖「被拒后 `Events` 变长」的）。
   ⚠️ 若某条断言**恰好**读了 `Ctx.Events` 的条数/尾部（本轮 grep 过 `Editor/BattleScene.cs`：没有）⇒ 不会有红。

## ⑥-⑦ 🔴 补一条关键事实：**测试夹具自己早就打了同一个补丁**（本轮现读）

`Editor/NetBattleTest.cs:402-413`（2026-10-27 `E5ENG` 加，注释里就引了 `D6NB` 报告）已经做了
「每次试失败就把这次追加的行截掉」：

```csharp
int evBefore = ctx.Events.Count;
foreach (var cand in PickActions(ctx)) {
    …
    if (rc == RuleCodes.OK) { act = cand; code = rc; break; }
    if (ctx.Events.Count > evBefore) ctx.Events.RemoveRange(evBefore, ctx.Events.Count - evBefore);
    …
}
```

⇒ 两点推论（写清楚，免得下一个人误判）：

1. **`NetBattleTest` 第 9 步可能早就绿了**（那次 08:15 的日志早于这一笔）—— 所以**别拿它当本批的验收口**。
   本批修的是**生产那边**（`BattleDriver.LocalAct`）—— 也就是**真机**：玩家点一个付不起的主动技能 ⇒
   引擎返非 OK + 写诊断行 ⇒ 那条动作不广播 ⇒ 回合末指纹不等 ⇒ **误判打岔、对局中止**。**这一档测试照不到**。
2. 两处**必须同时存在**：夹具那份是「测试自己的本地账要干净」；生产这份是「玩家的本地账要干净」。
   ⛔ 别因为「测试里已经截了」就把生产那份删掉（那正是这次挖出来的真缺陷）；也⛔ 别把夹具那份当成产品行为。

另：`Editor/BattleScene.cs:31310` 会 `driver.Ctx.Events.Find(s => …Contains("[BattleResult]"))` ——
撤回**不会**动到那些行：`[BattleResult]` 是 `RuleCore.Forfeit` 写的，**不走** `LocalAct` 的 `apply()` 窗口
（本批只撤「`apply()` 那一次调用」追加的尾部）。✅ 这条断言不受影响。
