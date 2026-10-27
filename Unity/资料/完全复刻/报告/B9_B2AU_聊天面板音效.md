# B9 · 音频/语音线 C · 代理 `B2AU` 报告（A-03 + A-04）

> 执行代理 `B2AU` · 2026-10-27 · **写手**（改了 2 个 `.cs`）
> 派活包 = `判据/B9_音频C_派活包.md` §2 的 **A-03（10 条）** 与 **A-04（6 条）**
> ⛔ 本波**没做** A-05（`BattleDriver.cs` 归 `E3CMP` 独占）—— 报告里凡写「未接线」的都是这一条约束造成的。
> 验收口：`TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（现读）。

---

## ① 结论（逐条）

### A-03 `Battle/ChatPopupPanel.cs`（10 条 · 全在**本文件内**落地）

| 账行 | 原版方法 | 状态 | 落点（本文件成员） | 备注 |
|---|---|---|---|---|
| 116 | `VoiceLinesController.ForcePopUpClose` | ✅ **实现** | `ForcePopUpClose(bool immediate = false)` | 闸 = `Visible`（等价 `+0x78 == 0`）；⏳ **调用点未接**（`ChoiceOfCardPlayer` 在 `BattleDriver`）→ 见 §③-1 |
| 134 | `VoiceLinesController.SetUpChatOptions` | ✅ **实现** | `SetChatContext(warlordId, MatchType)` + `SetupChatOptionTypes` | 3 元素表 `{110,120,80}`；⏳ 督军来源在 `BattleDriver` → 见 §③-1 |
| 144 | `VoiceLinesPopupSelector.Hide` | ✅ **实现** | `Advance` 的 OutCubic（`OutCubic` + `_fadeFrom/_fadeT`） | `t=0.5 ⇒ α=0.875`（线性是 0.5） |
| 145 | `..IsAvailable` | ✅ **实现** | `IsAvailable(MatchType, MatchType[])` | ⚠️ 原版**当前不可达**（见 §③-4）⇒ 只能夹具验 |
| 146 | `..IsIndexOutOfEnumBounds` | ✅ **实现** | `IsIndexOutOfEnumBounds(int)` + `ChatMessageEnumCount = 11` | 已被 `ApplyChatLines` / `ChatLineText` **真用上**（原版那个本体 0 调用者） |
| 147 | `..IsMatchOneOfCollection` | ✅ **实现** | `IsMatchOneOfCollection(…)`（`IsAvailable` 转调它） | LINQ `Any` 语义：`null`/空 ⇒ **false** |
| 150 | `..RefreshChatLines` | ✅ **实现** + 两处调用 | `RefreshChatLines()`；`Build()` 之后一次、`Show()` 里一次 | 文字来源 = `LineTermFor(warlordId,i)` → `Loc.T`；缺键/无督军退回分类词并**出声一次** |
| 151 | `..SetTransformedWarlordNewChats` | ✅ **实现** | `SetTransformedWarlordNewChats(warlordId, updateAvailable)` + `TransformedWarlordTypes` | **2 元素**表 `{110,120}`，与 134 那张 3 元素表**并存不统一**（原版如此，注释写死） |
| 152 | `..SetupChatOptions` | ✅ **实现** | `ApplyChatLines(possibleTypes, updateAvailable)` | 3 元素表 + `interactable` 那一支（越界才真算） |
| 153 | `..Show` | ✅ **实现** | `Show()` 里 `RefreshChatLines()` → 再开淡入 | 次序照 `__Show.c:12-15`（`SetActive` → `interactable` → 刷新 → tween） |

**A-03 一句话**：10 条代码全部落地；**其中 134 / 150 / 152 / 153 要在游戏里真看到「督军那 6 句台词」，还差 `BattleDriver` 里一行接线**（§③-1 给了那一行）。在接线之前面板行为与改之前**逐字相同**（印分类词），因此既有断言 `BattleScene.cs:22157`（6 颗钮 = `Battle/Chat/*` 分类词）**不会红**。

### A-04 `Battle/UnitChatPanel.cs`（6 条）

| 账行 | 原版方法 | 状态 | 落点 | 备注 |
|---|---|---|---|---|
| 94 (P1) | `VoiceLineBox.HideChat` | ✅ **实现** | `MakeBubble` 的 `AudioSource` 挂到**每颗气泡自己的底图 GO**；`HideBubble` 补 `Stop()` + `maxVisibleCharacters = 0` | 原版五步在一个方法里（`__HideChat.c:9,10,12,15,22`） |
| 85 (P1) | `UnitsVoiceLinesPanel.HideChatBox(type)` | ✅ **新增 `HideBox(int side)`** | `HideBox` + `HideAll()` 改走它 | 供 A-05 的 108 直接调 |
| 87 | `UnitsVoiceLinesPanel.IsAnyVoiceLinePlaying` | ✅ **实现** | `Bubble.isPlaying` + `IsSpeaking` 读它 | 判据 `+0x40`（见 §②-6） |
| 93 | `VoiceLineBox.FinishChat(duration)` | ✅ **实现** | `TickBubble`：去掉了「打字期间 return」与 dt 补偿 | 寿命与打字**并发** |
| 97 | `VoiceLineBox.ShowChat` | ✅ **实现** | `Bubble.delay` + `ChatRestartDelay = 0.1f` | **触发条件已查清**（见 §②-7） |
| 124 | `VoiceLinesController.IsAnyVoiceLinePlaying` | ✅ **同一处改动** | 同 87；消费点 `BattleDriver.SpeakCantDo` 的闸**已同构** | 见 §②-8 |

---

## ② 证据（原版出处，逐条）

1. **94 / 85 的「五步同一方法」**：`VoiceLineBox__HideChat.c:8-28` = ①`AudioSource.Stop()`(9) ②`+0x40=0`(10) ③波形件关(12) ④`SetActive(false)`(15) ⑤`maxVisibleCharacters=0`(22)；面板侧 `UnitsVoiceLinesPanel__HideChatBox.c:9-25` 逐句同形（先 `GetChatBox(type)` 只取那一颗）。⇒ 原版**没有**「只关画面不停音」这一档，我们原来把两者拆开是偏离。
2. **93 的「并发」**：`VoiceLineBox._ShowChat_d__19__MoveNext.c:77-82` 是**两句并排** `StartCoroutine(AnimateText)` + `StartCoroutine(FinishChat(duration))`；`FinishChat` 体内只有 `WaitForSeconds(duration)`（`VoiceLineBox._FinishChat_d__21__MoveNext.c:17-18`）、到点才 `+0x40=0`(:32)。原注释「`FinishChat` 排在 `AnimateText` 之后」读的是**行序**，不是「等它跑完」。
3. **87 / 124 的 `+0x40` 生命周期**：置 1 在 `VoiceLineBox._ShowChat_d__18__MoveNext.c:17`（`ShowChat` **被调用的那一刻**，即在 `d__19` 的视觉工作**之前**）；置 0 在 `_FinishChat_d__21__MoveNext.c:32` / `VoiceLineBox__HideChat.c:10` / `UnitsVoiceLinesPanel__HideChatBox.c:12` / `__SetUpChatBox.c:10` / `__Initialize.c`。读侧 = `UnitsVoiceLinesPanel__IsAnyVoiceLinePlaying.c:5-12`（三个盒子都非 null ⇒ `box3+0x40 || box2+0x40 || box1+0x40`）、`VoiceLinesController__IsAnyVoiceLinePlaying.c:7-12`。
4. **97 的 0.1 s**：`_ShowChat_d__19__MoveNext.c:60-67` `if (0 < +0x44) { StopCoroutine(AnimateText); yield return WaitForSeconds(DAT_1834b2dc4); }`，`0x1834b2dc4` = **0.1f**。
5. **116 的极性**：`VoiceLinesController__ForcePopUpClose.c:5-9` `if (+0x78 == 0) return;` + `VoiceLinesPopupSelector.Hide()` ⇒ `+0x78` = `PopUpShowed(0)/PopUpHid(1)`、**0 = 弹窗开着**才关（与派活包里的定案一致）。
6. **134 / 150 / 153 的写字与重刷**：`VoiceLinesPopupSelector__SetupChatOptions.c:102-110`（建时逐颗 `GetLocalizedChatMsg(warlord, i+5)`）、`__RefreshChatLines.c:71-81`（同一句）、**调用点 = `__Show.c:15`**（`SetActive(true)` + `interactable=true` 之后、tween 之前）。
7. **144 / 153 的缓动**：`__Show.c:31-34` / `__Hide.c:34-37` 的 `DOAnchorPos`/`DOFade` 同挂一条 `Sequence`，两处都 `SetEase(9)` = `Ease.OutCubic`。
8. **124 的消费点同构**：`BattleDriver.SpeakCantDo()`（现读 `:7623` 那一带）第一道闸就是 `if (_unitChat.IsSpeaking) return;` —— 与 `BattleTipController.NotifyCantDoAction` 的 `IsAnyVoiceLinePlaying()==false` 同义。`IsSpeaking` 改严之后这条闸**自动对齐原版**（改前短语音只挡 0.4 s，原版挡满 2 s）。

---

## ③ 没查清 / 缺资源 / 接不了的（逐条写清为什么）

1. 🔴 **A-03 的 116 / 134 / 150 / 152 / 153 全都差同一行接线 —— 位置在 `BattleDriver.cs`（本波归 `E3CMP` 独占，我一个字没动）**：
   - 建面板那一处（`BattleDriver.cs:29607` `_chatPopup = ChatPopupPanel.Create(root, chatPopupCv);` 之后）加一行：
     `_chatPopup.SetChatContext(myDeck != null ? myDeck.WarlordId : null, Ctx.MatchType);`
     （`myDeck.WarlordId` 现成：`BattleDriver.WarlordFaction`(`:12840`) 就是那么取的；`Ctx.MatchType` = `RuleEngine.MatchType`，见 `BattleContext.cs:951`。）
   - 116 的调用点 = 开选牌面板那一跳（原版 `BattleManager.ChoiceOfCardPlayer`），加一行：
     `if (_chatPopup != null) _chatPopup.ForcePopUpClose(immediate: !Application.isPlaying);`
     （战后那两处**已经接了**，见 `BattleDriver.cs:31078-31082` —— 那一处保持 `HideImmediate` 不动。）
   ⚠️ **接线时同批要改的一条断言**：`Editor/BattleScene.cs:22157` 现在断「6 颗钮 = `Battle/Chat/*` 分类词」；
   接线之后钮上会变成**督军台词全文** ⇒ 那条必红，应改成「= `Loc.T("Chat_Messages/<战将>-Chat<5+i>")`，
   且两个不同督军不同」。
2. **151 的触发源没有**：原版 `SetTransformedWarlordNewChats` 由「督军变身换卡」那条链调用，而**上游整条链还没复刻**（派活包自己写着「上游『督军换卡』整条也没复刻」+「与 A-05 的 133 同一处」）。⇒ 方法已实现、**今天 0 生产调用者**（夹具可直调），不是漏做。
3. **`BattleHudButton` 没有 `interactable` 口**（现读 `BattleHudButton.cs` 只有 `Attach/OnPointerClick/ClickForTest/ClickCountForTest/ResetClickCountForTest`）。原版逐颗 `Selectable.interactable` 落在 `Btn.interactable` 上，**在「收下点击」那一刻判**（`OnUguiButtonClicked` / `PointerUpAt`）—— 语义等价（不可点时点不动），且**不动共用件**。⚠️ 当前数据下这一格**恒 true**（见下一条），所以视觉上无差。
4. **145 / 146 / 147 原版当前不可达**（派活包已知）：调用点在 `i+5 >= Enum.GetValues(ChatMessage).Length` 那一支，而钮恒 6 颗、枚举 11 个 ⇒ 6 颗钮**永远 `interactable = true`**。我们**照原版留了这道闸**（`ApplyChatLines` 里），并用 `ChatMessageEnumCount = 11` 而不是 `ChatTerms.Length`（⛔ 后者是我们数组长，将来枚举加值会静默错位）。
5. **`+0x44` 语义：不是「没查清」，本轮查清了**（派活包写的是「未查清 ⇒ 先按有语音等 0.1 s」）。实读 = **打字机已显字数**（`_AnimateText_d__22__MoveNext.c:21` 清零、`:52-53` 打完再清零、`:56-57` 自增）⇒ `0 < +0x44` ⇔ **上一句打字打到一半被新的一句顶掉**。所以实现的是这个条件（`Speak` 里 `interruptedTyping`），**不是**「有语音就等」。
6. **数据预检（派活包要求「先跑一遍 56/56 命中」）**：已跑 —— `Resources/card_chat_intro.json` 的 **56** 张运行时卡 × `Chat5…Chat10` = **336 条键，官方表 `数据/游戏数据/官方本地化_en+zh-CN.json` 全部命中、中文列非空（missing = 0）**。
7. **`OnlyIfQuiet`（召唤并发闸）仍是 `AnyShown` 不是 `IsSpeaking`**（派活包 §A-04 的 87 那条提醒过「⛔ 别拿 `AnyShown` 替代」）。三槽模型下两者现在**逐刻同值**（`isPlaying` 与气泡可见性同生同灭）⇒ 行为无差，**本条没动**（属 B1 的设计，改它没有收益、只有风险）。

---

## ④ 改动清单（**只动了两个文件**，`git status` 现核）

| 文件 | 增/删行 | 改了什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Battle/ChatPopupPanel.cs` | +272 / −3 | `Btn.interactable`；`Build`/`Show` 里调 `RefreshChatLines`；`Hide`/`Show`/`HideImmediate` 的淡变状态；`Advance` 改 OutCubic + `OutCubic()`；点击闸；**类尾新增** `SetChatContext` / `RefreshChatLines` / `ApplyChatLines` / `ChatLineText` / `SetTransformedWarlordNewChats` / `IsIndexOutOfEnumBounds` / `IsAvailable` / `IsMatchOneOfCollection` / `ForcePopUpClose` / `ChatMessageEnumCount` / `SetupChatOptionTypes` / `TransformedWarlordTypes` / `ButtonInteractableOf` |
| `Unity/MyGame/Assets/CardPresentation/Battle/UnitChatPanel.cs` | +174 / −27 | `Bubble.isPlaying` + `Bubble.delay`（含 `ResetState` 两行）；`AudioSource` 改挂每颗气泡底图 GO；`Speak` 记 `interruptedTyping` + 置 `isPlaying/delay`；`HideBubble` 补 `Stop()`/`maxVisibleCharacters=0`；**新增 `HideBox(int)`** 且 `HideAll` 改走它；`IsSpeaking` 改读 `isPlaying`；`TickBubble` delay + 并发寿命（删 dt 补偿）；类尾 4 个只读自检口 + `ChatRestartDelay = 0.1f` |

**行尾**：两个文件都是**纯 LF**（`CRLF=0 / LF=1584` 与 `0 / 2061`，`bareCR=0`，二进制现读）⇒ 没有被翻行尾。新增成员一律**追加在类尾**（`Btn` 那个字段加在 `Btn` 类尾、`Bubble` 两个字段加在 `Bubble` 类尾），以尽量少漂移下游账的 `文件:行号`。

---

## ⑤ 顺手发现（本批**没有**顺手改，交给调度台分流）

1. 🔴 **`Loc.HasEntry` 只认手写 `Table`（现读 754 条），不认官方全表** —— 而 `Chat_Messages/*` 这一族（**1,736 条**）**全在官方全表**（`Resources/official_loc_13lang.json` / 10,862 键）。
   我第一版就是拿 `Loc.HasEntry` 判的，自查时发现它会**恒 false** ⇒ 6 颗钮永远退回分类词（**而画面上有字、不报错**，静默）。已改用 `Loc.T(key)` 与键名比（口径 = `UnitChatPanel.OfficialChatTextOfTerm`）。
   📌 **建议**：这条写进「已知的坑」那一族（与 CLAUDE.md §三「判『会不会把键名印到屏幕上』必须三档一起算」同源，但**入口不同** —— 那条讲 `Loc.T`，这条讲 `Loc.HasEntry`，后者**根本不含后两档**）。
2. `BattleDriver` 有两处注释**已过期**（A13 三槽之后 `Speak` 不再 `HideAll`）：`:16481-16482`「我们同一时刻**只有一颗气泡亮着**（`Speak` 开头 `HideAll()`）」、`:18051-18058`「`Speak` 开头就 `HideAll()` ⇒ 只关某侧与全关效果相等」。**我没改**（不在白名单）—— 108 那条（`HideAll` → `HideBox(BubbleSideOf(seat))`）接上去之后，这两句就**必须**改，否则会误导下一个会话。
3. 原版 **`+0x40` 是在 `ShowChat` 调用那一刻置 1**（`_ShowChat_d__18` 那个状态机只干这件事，然后启动 `_ShowChat_d__19` 干视觉）—— 值得写进判据：**「isPlaying 的起点早于 SetActive/Play」**，与「`+0x44` 早于/晚于」都容易看反。
4. `ChatPopupPanel.Build()` 会在**每局开打时**打一条 `LogWarning`（「还没有喂本局我方督军」）—— 这是**有意的出声**（P1 没接线这件事必须可见），接线之后自动消失。如果调度台觉得吵，可以降成 `Log`，但**不建议**删。

---

## ⑥ 该跑哪条宿主自检

- **只跑 `BattleScene.Run`**（`-executeMethod BattleScene.Run`）。
  理由：改的两个文件只被战斗侧使用（`BattleDriver` 建它们；`Shell/MainMenu/Rewards/Shop/Collection/Settings/Deck` 一个都不引用，现核 `grep`）。
  而且 **E17 的打字机断言就在这条宿主里**（`Editor/BattleScene.cs:44086` → `BattleFxAssertE17.Run()` → `RunTypewriterLive`）⇒ 93/97 的改动正好被它覆盖。
- **不用跑** `RuleEngineTest`（没碰 `RuleEngine/**`）、不用跑联机三条（没碰 `Net/`）。
- ⚠️ **本波不跑 Unity**（子代理纪律）。跑之前请先确认 `Editor/BattleScene.cs` 的 ChatPopup 块（`:22126` 起）与 E17 块没有被别的代理正在改。

### 断言配方（给写断言的那一位，期望值都取**原版判据侧**、⛔ 不读我们的常数）

```csharp
// ── A-03 ───────────────────────────────────────────────────────────
// 150/134：两个不同督军的 6 句不同，且逐字 = 官方表
pop.SetChatContext("AM1", RuleEngine.MatchType.ClosedDeck);  pop.RefreshChatLines();
Check(pop.TextAt(0) == Loc.T("Chat_Messages/AM1-Chat5") && pop.TextAt(5) == Loc.T("Chat_Messages/AM1-Chat10"),
      "★ 钮上印的是**该督军那 6 句台词全文**（原版 `RefreshChatLines.c:71`）");
pop.SetChatContext("BL34", RuleEngine.MatchType.ClosedDeck); pop.RefreshChatLines();
Check(pop.TextAt(0) != Loc.T("Chat_Messages/AM1-Chat5"), "★ 换督军 ⇒ 文字跟着换");

// 144/153：alpha 三点（OutCubic）
pop.Show(); pop.Advance(0.25f);
Check(Mathf.Abs(pop.Alpha - 0.875f) < 0.01f, "★ t=0.5 ⇒ α=0.875（OutCubic）｜🧨 改回线性 ⇒ 0.5 ⇒ 红");
pop.Advance(0.25f);
Check(pop.Alpha > 0.999f, "★ t=1 ⇒ α=1");

// 145/147/146：纯函数
Check(ChatPopupPanel.IsAvailable(RuleEngine.MatchType.ClosedDeck, ChatPopupPanel.SetupChatOptionTypes)
   && !ChatPopupPanel.IsAvailable(RuleEngine.MatchType.Practice, ChatPopupPanel.SetupChatOptionTypes)
   && !ChatPopupPanel.IsMatchOneOfCollection(RuleEngine.MatchType.ClosedDeck, null)
   && !ChatPopupPanel.IsMatchOneOfCollection(RuleEngine.MatchType.ClosedDeck, new RuleEngine.MatchType[0]),
      "★ IsAvailable/IsMatchOneOfCollection（空集/ null ⇒ false）");
Check(ChatPopupPanel.IsIndexOutOfEnumBounds(11) && !ChatPopupPanel.IsIndexOutOfEnumBounds(10),
      "★ 越界判据 = `ChatMessage` 枚举长 11（⛔ 不是 `ChatTerms.Length`）");

// 116：开着才关
pop.HideImmediate(); Check(!pop.ForcePopUpClose(true), "★ 本来就没开 ⇒ 返回 false（原版 +0x78 != 0 直接 return）");
pop.Show();          Check(pop.ForcePopUpClose(true) && !pop.Visible, "★ 开着 ⇒ 关掉并返回 true");

// ── A-04 ───────────────────────────────────────────────────────────
// 94：三颗各挂一份 AudioSource 在自己的底图 GO 上
Check(chat.BubbleAudioOnOwnObjectForTest(0) && chat.BubbleAudioOnOwnObjectForTest(1)
      && chat.BubbleAudioOnOwnObjectForTest(2), "★ 每颗气泡自己一颗 AudioSource（原版 `VoiceLineBox+0x30`）");
chat.Speak(0, null, "Probe", null, null, "x", null, expectVoice: false);
chat.HideBox(0);
Check(!chat.BubbleAudioPlayingForTest(0), "★ HideBubble 之后 audio.isPlaying == false（原版 `__HideChat.c:9`）");

// 85：按颗关（先说两颗，再只关一侧）
chat.Speak(0, null, "P0", null, null, "x", null, expectVoice: false);
chat.Speak(1, null, "P1", null, null, "y", null, expectVoice: false);
Check(chat.BubbleShownForTest(0) && chat.BubbleShownForTest(1), "（前提）两颗都亮着");
Check(chat.HideBox(1) && !chat.BubbleShownForTest(1) && chat.BubbleShownForTest(0),
      "★ HideBox(1) 只关敌方那颗（原版 `GetChatBox(type)` 只取那一颗）");

// 87/124：短语音播完 ⇒ isPlaying 仍 true（灭自证：audio 已 false）
chat.Speak(0, "AM1", "CantDo", null, null, "x", shortClip /*0.4s*/);
for (int i = 0; i < 30; i++) chat.Advance(1f/30f);          // ≈1 s
Check(!chat.BubbleAudioPlayingForTest(0) && chat.BubbleIsPlayingForTest(0) && chat.IsSpeaking,
      "★ 音频播完但气泡还在说（原版 `+0x40` 撑到 max(2, len+0.3)）｜🧨 改回读 audio ⇒ 红");

// 93：150 字 + 3 s 语音 ⇒ 总寿命 ≈ 3.3 s（改前 ≈4.8 s）
//      推进 3.35 s 后应当已收；推进到 3.2 s 时还亮着。

// 97：上一句打字打到一半被顶掉 ⇒ 0.1 s 等一拍
chat.Speak(0, null, "A", null, null, new string('A', 40), null, expectVoice: false);
chat.Advance(0.2f);                                        // 40 字 ≈ 0.4 s ⇒ 此刻**还在打**（已显 ≈20 字）
chat.Speak(0, null, "B", null, null, new string('B', 40), null, expectVoice: false);
Check(Mathf.Abs(chat.BubbleDelayForTest(0) - 0.1f) < 1e-4f, "★ 打断 ⇒ 0.1 s 等一拍（原版 `ShowChat d__19:60-67`）");
chat.Advance(0.05f); Check(chat.VisibleCharsForTest == 0, "★ 0.05 s ⇒ 仍 0 个字");
chat.Advance(0.10f); Check(chat.VisibleCharsForTest >= 1, "★ 0.15 s ⇒ 已开始出字");
```

---

## 第二批（2026-10-27 · 调度台加派）

> 目标 = `判据/B9_剩余C_施工单.tsv` 的 294 / 295 / 460 / 489 / 461 / 464 / 440 七条。
> 约束照旧：只碰 `CardPresentation/Battle/` 与 `Shell/` 下不属于别人的文件；⛔ 不碰 `BattleDriver.cs`（`E3CMP`）/`RuleEngine/Core/**`（`E1RNG`）/`Core/CardFeel.cs`、`CardView.cs`（`E2UDW`）。

## ① 结论：**7 条里 6 条早已落地**（前几轮代理做的），本轮只是**逐条核实**；真正缺的那 1 条我做完了

| 账行 | 方法 | 状态（本轮核实） | 落点 | 谁做的 |
|---|---|---|---|---|
| 294 | `DisplayBigChat(string, CardScript)` | ✅ **已实现** | `Battle/BattleBigChat.cs:95` `Display(panel, text, CardDef, …)` | `E5P12`；接线 `E2P11` |
| 295 | `DisplayBigChat(string, Sprite)` | ✅ **已实现**（唯一实现） | `Battle/BattleBigChat.cs:115` `Display(panel, text, Texture2D, …)` | `E5P12` |
| 460 | `PlayIconAnim(…)` | ✅ **已实现** | `Battle/AbilityIconAnim.cs:284` `Enqueue(…)`（+ `:304` 数值重载） | `R1T3A` |
| 489 | `ResolveAbilityIconAnim(BattleAction)` | ✅ **已实现** | `Battle/AbilityIconAnim.cs:363` `Tick(dt)`（一步式状态机）+ `:148` `PlayNow` | `R1T3A` |
| 461 | `PlayNextSoundInQueue()` | ✅ **已实现** | `Battle/BattleSfx.cs:295` `DrainOne()` + `:286` `Tick` + `:384` `Pump`（PlayerLoop） | `E5P12` |
| 464 | `PlaySoundAsset(SoundAsset_OLD)` | ✅ **已实现** | `Battle/BattleSfx.cs:267` `Enqueue`（`PlayCore` 只入队） | `E5P12` |
| 440 | `LogActivatedTrigger(bool, AbilityTrigger)` | 🟡 **本轮补了表现层那一半**（`Shell/BattleLogData.cs`）；引擎侧早已有、但**有偏差**；**接线缺一行** | `Shell/BattleLogData.cs` 新增 `LogTriggerAbility` / `TriggerAbility` 事件 / `Match.Triggers` | `B2AU`（本轮） |

**核实方法与证据**（逐条不是"看着像"，是现读）：
- 294/295：`BattleBigChat.Display` 两道 guard 与原版逐字同（空文案 ⇒ `LogWarning(EmptyChatWarning)`；立绘 null ⇒ `LogError(NoUnitError)` 且不显示）；重载 A 取 `FullHeroSprite`（= `CardArt.TryDeckHero`，判据就是 `fullHeroSpriteReference`）再转发 B。**已接线**：`BattleDriver.cs:17198` 的 `*ChatBig` 支调 `BattleBigChat.Display(_unitChat, text, who)`（`E2P11` 2026-10-26）。
- 460/489：`AbilityIconAnim` 的两个时长我已独立复核 —— `read_literal.py` 实读 `0x1834b2bb4 = 0.5`（`+0x4B0`）· `0x1834b2bb0 = 0.2`（`0x4C8`），与源文件头写的值一致；`LandingTrigger = 440` 与 `dump.cs:20291 AbilityTrigger.Landing = 440` 一致。🔴 **原版这两条在本 build 零调用点**（两棵树 `grep` 都只命中定义）⇒ 类里**故意不接触发源**，等宿主调度。
- 461/464：FIFO 已在（入队/出队/泵/自检读口齐全）。**顺带确认了派活包那句「串行播、间隔 ≈ clip 时长」是错的**，而 `BattleSfx` 文件头已经按实读订正成「原版 = `Update` 每帧取一条」（`BattleManager__Update.c:152` 那句调用**确实在 Update 里、无 `isPlaying` 闸**）；默认档 `OnePerFrame` 就是原版，串行档是可选演示。

## ② 本轮唯一真改动：`440`（`CardPresentation/Shell/BattleLogData.cs`，+170/−0，纯 LF）

**先纠一个判据**（施工单那条是猜的，我实读到的是另一回事 —— 铁律 5）：
- 施工单写：「加 `LogTriggerAbility(isPlayer, trigger, cardId)`；在 `RuleCore` 发触发事件的消费点记账；与 `LogMatchEnd` 同一份存档结构」。
- **实读**（`out_cpp2il_dev_ilspy/BattleManager.cs:48762-48765` + `ChallengeLogMgr.cs:578-585`）：
  ```csharp
  public void LogActivatedTrigger(bool isPlayer, AbilityTrigger trigger) => challengeLog.LogTriggerAbility(isPlayer, trigger);

  public void LogTriggerAbility(bool isPlayer, AbilityTrigger trigger) {
      if (trigger != AbilityTrigger.WhileInPlay && trigger != AbilityTrigger.WhileInPlayVariable
          && trigger != AbilityTrigger.TriggeredSynapse)
          Signal.Raise(new TriggerAbilitySignal(trigger, isPlayer));
  }
  ```
  ⇒ **它不是日志、不落盘、没有 cardId**：只是一条进程内信号，**并过滤掉 20 / 240 / 630 三个 trigger**。

**所以本轮落成两半，各自标了来源**：
- **复刻的那一半**（`BattleLogData.LogTriggerAbility(bool isPlayer, int trigger, string triggerName = null)`）：
  ① 三条过滤（`TriggerWhileInPlay = 20` · `TriggerWhileInPlayVariable = 240` · `TriggerTriggeredSynapse = 630`，值取自 `dump.cs:20242/20275/20311`）；
  ② 发 `public static event Action<bool,int> TriggerAbility`（= `Signal.Raise`；订阅者抛异常逐个 `try` + `LogError`，⛔ 不让它带崩结算）。
- **🔶 自设计的那一半**：按局累计（`TriggerStat` 按 `(方, trigger)` 合一条）+ 挂在 `Match.Triggers` 上（`Add()` 时拷入并清空 buffer）⇒ 「一局触发 N 次 ⇒ 条数 = N」可断言、可在战报里看。**原版不存任何东西**，这一格在代码与账里都标了 `🔶 自己设计`。

**🔴 同时查出一条引擎侧偏差（不在我的白名单，只报不改）**：`RuleEngine/Core/AchievementTally.AddTrigger`（`AchievementTally.cs:169-184`）**不过滤那三条** ⇒ 它对 `WhileInPlay(20)` / `WhileInPlayVariable(240)` / `TriggeredSynapse(630)` **会多计**，与原版 `LogTriggerAbility` 的语义不符（这三条在原版**永远不发信号**）。判断上它可能是有意的（成就统计想算「每回合光环触发」），但**与挑战日志那一层不是同一口径** ⇒ 要么在 `Emit` 那一跳加过滤、要么在账里写明「成就口径 ≠ 挑战口径」。

## ③ 挡在别人身上的接线清单（一条都不在我白名单里）

| # | 要接的一行 | 文件（谁的） | 判据 |
|---|---|---|---|
| 1 | `BattleLogData.LogTriggerAbility(e.Player == _me, CardDef.AbilityTriggerNumber(BattleEvent.SplitKeywordAbility(e.Keyword, out _)), e.Keyword);` | `BattleDriver.cs`（`E3CMP`）· `PlaySignal` 的 `EvtKind.Trigger` 支（现读 `:22561` 那一带） | ⚠️ `e.Keyword` 带 `#<能力序号>` 后缀 ⇒ 先 `SplitKeywordAbility`（引擎的**唯一**解码处）；`e.Player` 是**座位**不是「是不是本人」 ⇒ 用 `== _me` |
| 2 | 三条过滤的分歧（见 §②末） | `RuleEngine/Core/AchievementTally.cs` / `BattleContext.CountTriggerForAchievements`（`E1RNG`） | 原版 `ChallengeLogMgr.cs:580` |
| 3 | 460/489 的触发源（原版零调用点，**别硬接**） | `BattleDriver.cs`（`E3CMP`） | 若要调度：`AbilityIconAnim.Enqueue(view, wait, "Landing", ActionPriority.Instant, …)` |
| 4 | 294/295 的教程动作（已接，仅记录） | `BattleDriver.cs:17198` | ✅ 已接，无需动 |

## ④ 自检
- `TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（现读，第二轮改动之后）。
- **该跑**：`BattleScene.Run`（战斗侧写完战报时会走 `BattleLogData.Add`）+ **`ShellScene.Run` 与 `MainMenuScene.Run`**（它们各有一段战报/存档往返的断言，`Match` 多了 `Triggers` 一格 ⇒ 只想确认 JsonUtility 往返没坏）。
- **不跑** `RuleEngineTest`（没碰引擎）、不跑联机三条。
- 断言配方（给写断言的那一位）：
```csharp
BattleLogData.ResetTriggerLogForTest();
Check(!BattleLogData.LogTriggerAbility(true, 20) && !BattleLogData.LogTriggerAbility(false, 240)
   && !BattleLogData.LogTriggerAbility(true, 630), "★ 三条 trigger **不发信号**（原版 ChallengeLogMgr.cs:580）");
int seen = 0; System.Action<bool,int> h = (p, t) => seen++; BattleLogData.TriggerAbility += h;
Check(BattleLogData.LogTriggerAbility(true, 440, "Landing") && seen == 1, "★ 其余 trigger 发一条信号（= Signal.Raise）");
BattleLogData.TriggerAbility -= h;
BattleLogData.LogTriggerAbility(true, 440, "Landing");           // 同一 (方,trigger) 累计
Check(BattleLogData.TriggerTotal == 2 && BattleLogData.CurrentMatchTriggers.Count == 1, "★ 按局累计：2 次 / 1 条");
var m = new BattleLogData.Match(); BattleLogData.Add(m);
Check(m.Triggers != null && m.Triggers.Length == 1 && m.Triggers[0].Count == 2
   && BattleLogData.CurrentMatchTriggers.Count == 0, "★ 挂进对局记录后 buffer 清空");
BattleLogData.ResetForTest();                                     // 别把这条假记录留在表里
```
## ⑤ 第二批的顺手发现
1. **施工单 440 那条判据是猜的**（见 §②「先纠一个判据」）—— 建议把 `B9_剩余C_施工单.tsv` 的 440 行「原版逐条步骤 / 施工步骤 / 目标文件」就地改掉（我没改，那是判据件、不在我的写权限里）。
2. **`AchievementTally` 不过滤 20/240/630** ⇒ 与挑战日志口径不一致（见 §②末）。
3. 460/489 与 294/295 都是「原版自建但**本 build 零调用点**」的族 —— 这类条目**不要拿「没接线」当「没做」**，账里应写「可达性 0（原版零调用点）」。

## ⑥ 账补丁
- 文件：`资料/完全复刻/账/_parts/B9_B2AU.tsv`（**1 行** = 440；11 列，`状态` 保持 `B` 未动、`等级` 未动，只改了 `差异` / `轮次`(= `B9_B2AU`) / `备注`）。
  生成器：`账/_parts/_gen_B9_B2AU.py`（读主表 → 写补丁；⛔ 主表只读，未写）。
- ⚠️ **只放 440 一条**：其余 6 行的账早已被 `B9_E5P12` / `B9_R1T3A` 写全（我逐条核过、内容仍成立）⇒ 不重复放行，免得多份补丁撞同键。
- 本条落点那一列仍写的是引擎侧 `BattleContext.Emit`（既有内容，按「只改三列」的规矩没动）；**新的落点我在 `差异` 里写全了**（`Shell/BattleLogData.cs`）—— 合并时若要同步 `我们的落点` 那一列，请照 `差异` 第二句改。

---

## 第三批（2026-10-27 · `战斗界面_HUD.tsv` 状态 C 的 8 条）

> 结论先说：**这 8 条没有一条能在我的白名单内闭合** —— 逐条核完，全部落在
> `BattleDriver.cs`（`E4BDR`）/ `RuleEngine/Core`+`Data`（`E1RNG`）/ 「数据源本地不可得」/ 「已由别的载体实现」这四类里。
> 按派活纪律（「碰到被判据指向别人的 ⇒ 只写待接线 + 精确改法」）⇒ **本轮 0 个 `.cs` 改动**，产出 = 8 条精确结论 + 账补丁。

| # | 方法 | 结论 | 卡在谁身上 |
|---|---|---|---|
| 20 | `SetupActiveSkill` | 🟡 面板侧已就位；**账里那句「生成器不 emit」已过期**（它**已经** emit ability 级），真断点在后两级 | `CardDef`/`CardDatabase`（E1RNG）+ `ShowSkillPanel`（E4BDR） |
| 306 | `SetupTutorialObj` | ⛔ **维持「不建」**（复验成立：源码里确实没有这件；数据源 `TutorialEvent` 本地不可得） | 教程线（等本地入参） |
| 364 | `AutoProcessDone` | ⛔ 缺「录像里的候选集」⇒ 面板摆不出正确候选 | `BattleDriver.BeginAsk` 记录（E4BDR） |
| 378 | `ChooseCardMenu.Setup` | 同上（①②③⑤ 已在）；另确认「标题回落 `Instructions-<id>` 优先」现在已在 | 同上 |
| 750 | `DisplayOffensiveCardInCenter` | 🟡 **落点指错载体** —— 原版移动的是**卡本体**；我们已在 `BattleDriver` reveal 链实现，残差只有 `enemyHand` 世界点映射（🔶） | 无（已是 🔶 既定口径） |
| 755 | `GetCurrentLocalizedAbilityDescription` | 🟡 三档后缀链（`-load` / `-effect` / `Card_Abilities/GenericSiegeText`）没做；**数据现成**，卡在 `SkillPanel.Show` 收不到 `cardId/needsLoading` | `SkillPanel` 加形参 ⇒ 调用点 `BattleDriver`（E4BDR） |
| 756 | `GetEnemyCardPlayedDisplayPosition` | 🟡 同 750（落点指错载体；公式与偏移已在注释与 reveal 链里） | 无（🔶 既定） |
| 758 | `GetOffensiveCardPlayedDisplayPosition` | 🟡 同 750/756（`−25 / 30` 两个偏移值已读出） | 无（🔶 既定） |

### 三条「账里写错了 / 已过期」的更正（本轮实读）
1. **行 20 那句「① `工具/gen_cards_truth.py` 的 `scan_ability` 不 emit ability 级 `targetsAvailableCounter`」—— 不成立**：
   现读 `工具/gen_cards_truth.py:308-311`，`for k in ("targetCriteria", "targetsAvailableCounter")` 就写在 **ability 级**的循环里、
   `compact_criteria` 之后 `out[k] = c` ⇒ **生成器那一半已经做了**。
   真正的断点：② `RuleEngine/Core/CardDef.cs` 只落了**卡级** `avail` 行（v22，`+0x298` 口径；ability 级没有那一格）
   + `RuleEngine/Data/CardDatabase.cs:802` 同；③ `BattleDriver.ShowSkillPanel` 读的是卡级 `CardFaceRules.HasTargetsAvailableCounter`。
2. **行 750/756/758 的「我们的落点 = `CardDisplayWindow.cs:1221 (Show)`〔居中展示〕」指错了载体**：
   这三个原版方法返回的是**卡本体**的展示位（`enemyHand.transform.position + right×Xoffset − up×Yoffset`，移动端换 X 偏移字段），
   它们被 `CardScript._DisplayOffensiveCardInCenter_d__264` / `_DisplayEnemyCardInCenter_d__263` 消费 ——
   我们这一侧的等价物**已经在 `BattleDriver` 的 reveal 链**里（`RevealStartPx/EndPx` `:9712-9713`、`RevealMoveDur 0.15` `:9667`、
   `RevealRotDur 0.1` `:9668`、终点缩放 `GetEnemyCardPlayedDisplayScale` `:25390-25470`，推导 `:9644-9660`）。
   `CardDisplayWindow` 是**放大窗**（另一个载体）⇒ ⛔ 不该在那里再加一套偏移（否则同一件事两条常驻路径）。
   残差只有一项且**已被标成 🔶**：`enemyHand` 那个世界点在我们这边没有判据 ⇒ `RevealStartPx/EndPx` 是我们挑的。
3. **行 755 的原版逻辑本轮补全**（`decomp_full/CardScript__GetCurrentLocalizedAbilityDescription.c` + ILSpy `:17102-17157` 逐行对齐）：
   ```
   if (DoesAbilityRequireLoading()) {
       if (!activeAbilityLoaded) return Desc("-load")  ?? ModifyLocalization(Loc("Card_Abilities/GenericSiegeText"));
       else                      return Desc("-effect") ?? Desc();          // 裸键
   } else {
       ability = card.GetActiveAbility(...);
       if (ability != null && ability.needsLoading && HasRemovedTrait(DefinedTrait.reload))
           return DescRemovingSiege();                                        // ← 这一支的后缀没解出来
       return Desc();                                                          // 裸键
   }
   ```
   **数据我们已经有**：`CardDef.ActiveAbilityNeedsLoading`（v21，`CardDef.cs:239`）、`ConditionEval.ReloadTrait = "reload"`；
   **卡住的是入口**：`SkillPanel.Show(cardName, EffectSpec ability, …)` 收不到 `cardId / needsLoading / loaded`。
   精确改法（给 `E4BDR`）：加**可选**形参
   `string cardId = null, int needsLoading = CardDef.GateUnknown, bool abilityLoaded = false, bool hasRemovedReload = false`，
   再按上表三档取 `Card_Description/<id>-load` / `-effect` / `Card_Abilities/GenericSiegeText`
   （后缀字符串是 ILSpy `:17109` / `:17120` 直读的）。⚠️ `RemovingSiege` 那一支**没解出后缀**
   （`RawCardScript__GetLocalizedAbilityDescRemovingSiege.c` 内部还拼了 5 个常量）⇒ 要做那一档得先解它，⛔ 别猜。

### 为什么一条 `.cs` 都没改（逐条可核）
- 20 / 755：闭环都要动 `CardDef`+`CardDatabase`（`RuleEngine/**`）或 `SkillPanel` 的**调用点**（`BattleDriver`）。
- 364 / 378：读侧（`Battle/ReplayStore.cs`）**我能改**，但没有生产者时那一格永远为空 = 死件 ⇒ 不单建（与账里「⛔ 别只把面板画出来却摆错候选」同一口径）。
- 306：源码里确实没有实现，且建出来恒不可达（数据源不可得）⇒ 维持不建。
- 750 / 756 / 758：行为**已经在** `BattleDriver` 的 reveal 链里，且残差是已标注的 🔶；在 `CardDisplayWindow` 再写一遍就是第二条路径。

### 自检
- **本轮 0 个 `.cs` 改动** ⇒ 按铁律 12「纯文档 / 纯数据 ⇒ 一条都不用跑」⇒ **不跑任何宿主，也不跑 `typecheck` 之外的东西**。
- 现状（供参考）：`TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` 在第三批开工前最后一次 = **运行时 0 / 编辑器 0**；本批未改代码，故不变。
- 建议下一批（`BattleDriver` 放开之后）按 §上表右列逐条派：**20 / 755 / 364** 三条都是「一行到三行」的接线。

### 账补丁
`资料/完全复刻/账/_parts/B9_B2AU2.tsv` = **8 行**（全 8 条：`状态` 一律**不动**仍 `C`，只改 `差异` / `轮次`=`B9_B2AU2` / `备注`；11 列逐字同主表表头）。
生成器：`账/_parts/_gen_B9_B2AU2.py`（读主表 → 写补丁，⛔ 主表只读）。
其中**行 20 与 750/756/758 的 `差异` 是更正性质的改写**（原来那两句是错的/过期），⚠️ 合并时**别退回旧文本**。

---

## 第四批（2026-10-27 · 音频线派活包剩下的包 A-01 / A-02 / A-06 / A-07 / A-08）

> 结论：**A-01 与 A-08 早已落地/本就不派活**；**A-07 本轮新建完成**；**A-02 我把 `.cs` 侧写完、资产侧阻塞在主对话**；
> **A-06 整件不在本波白名单**（`Net/`）⇒ 只给精确改法。

| 包 | 条数 | 状态 | 本轮做了什么 |
|---|---|---|---|
| **A-01** `ShellRuntime` + 新建 `Core/AudioSourcesController.cs` | 16 | ✅ **早已落地** | 抽查核实（见下），**没动**（`ShellRuntime.cs` 属 `B1UI`/`E3CMP`） |
| **A-02** mixer 快照 + `WarpforgeAudio` | 2 | 🟡 **`.cs` 侧本轮写完**；资产侧阻塞 | 新增 `SnapshotsType` + `GetSnapshot` + `ToggleSFX` |
| **A-06** `Net/NetChat.cs` + `NetNetBattle.cs` | 1 | ⛔ 不在白名单 | 报告里给三处精确改法 |
| **A-07** 新建 `Effects/UIEffectTogglerPlaySound.cs` | 2 | ✅ **本轮新建完成** | 逐句照原版（31 行） |
| **A-08** 不派活（3 条归档 D） | — | — | 不动 |

### A-01（16 条）—— 已落地，本轮**只抽查不重做**
- `CardPresentation/Core/AudioSourcesController.cs` **已存在**（17,965 B），十个方法齐：`Init / PlayInstant / FadeIn / FadeOut / Blend / Stop / Tick` 公开，
  `SwitchMainAudioSource / IncrementVolume / SetMaxVolume / DecrementVolume / SetMinVolume / VolumeLerp` 私有（= 原版可见性），
  常量 `MAX_VOLUME 1.0 / MIN_VOLUME 0.0 / LERP_DURATION 3.0`（判据 `AudioSourcesController__.cctor.c:9-11` 写在文件头）。
- `Shell/ShellRuntime.cs` 里 `AudioSourcesController` **13 处引用**；`MusicTransitionType` 三档（FadeIn/Blend/Instant）在 `PlayMusic` 里齐全（`:1222-1249`），
  `musicVolumeMultiplier = 0.2` 的口径写在 `:92` 并与 `WarpforgeAudio.MusicVolumeMultiplier` 对照（`+0x30`→那格），过场/停音乐那两支在 `:1299-1307`。
- ⚠️ **这不是 16 条逐条复核**（那要整份读 `ShellRuntime` 1,164 行 + 七个调用点），只抽查了「文件在、方法齐、特征常量对、三档过渡在」四件事。
  若要「16 条逐条」，建议另派一个只读代理。

### A-02（2 条）—— 本轮**落了 `.cs` 侧**（`CardPresentation/Core/WarpforgeAudio.cs`）
- 新增：`enum SnapshotsType`（**六个值逐字抄** `dump.cs:17953-17962`：`FxNormal=0 / FxNoVolume=1 / MusicBattleMusicLow=2 / …Medium=3 / …High=4 / …NoMusic=5`）
  + `SnapshotNames`（六个名字，**🔶 自己设计**，已在代码里标）+ `GetSnapshot(SnapshotsType)`（`FindSnapshot` + **缺件出声一次** + `SnapshotMisses` 读口）
  + `ToggleSFX(option, instant)`（`instant ? 0 : 2` 秒 `TransitionTo`，判据 ILSpy `SoundManager.cs:596-602`）。
- 🔴 **资产侧没做、也做不了**：`Resources/Audio/Main Mixer.mixer` 现读 `m_Snapshots` **只有 1 条（默认块）**、也没有 `BattleGroup Low/Medium/High`。
  建快照**必须跑 Unity 编辑器脚本** ⇒ 只有主对话能跑。**建的时候名字必须与 `SnapshotNames` 一致**：
  `SFX On` / `SFX Off` / `Music Low` / `Music Medium` / `Music High` / `Music None`。
- 今天的行为：`GetSnapshot` **恒 null** ⇒ `ToggleSFX` 什么都不做，但**每次缺件出声一次**（不静默 = 派活包要求的形态）。
- ⚠️ 调用点（`Shell/SettingsWindow.cs` 的 SFX 开关）不在本波白名单 ⇒ 归 `B1UI`。

### A-06（1 条）—— 精确改法（`Net/` 不在白名单，未动）
1. `Net/NetChat.cs` 里 `ShouldRecord` 的注释「4 = HurryUp」→ **4 = Concede**
   （枚举实读：`0 Intro · 1 MirrorMatch · 2 Bored · 3 ICantDoThat · 4 Concede · 5…10 Greet/Threat/WellPlayed/Taunt/Sorry/Oops`）。
2. 发送闸 `IsButtonLine` → `IsRelayable`（`id != 0 && id != 1`；判据 `DisplayWarlordChatMessage.c:65`）。
3. **收侧保留 `IsButtonLine`**（防伪造越界）—— ⛔ 不要两边都换。
4. 依赖项：认输那一句要 `BattleDriver` 侧调 `OnLocalChat(4)`（A-05 的活，已在第二批报告里提过）。

### A-07（2 条）—— 本轮新建 `CardPresentation/Effects/UIEffectTogglerPlaySound.cs`
- 逐句照原版（ILSpy `UIEffectTogglerPlaySound.cs` 全文 31 行 + `decomp_full/UIEffectTogglerPlaySound__{DoUpdate,AnimationFinished,ctor}.c`）：
  `DoUpdate` **只推目标态那一个槽**（`(!targetStateEnabled ? offSound : onSound)?.Update(currentTime, null)`，第二实参原版就是 **null** ⇒ 落 2D 分支）；
  `AnimationFinished` 两槽都 `Reset()`；`deactivateOnDisable = true` 走基类默认（原版构造函数那一句，语义等价）。
- **复用不复制**：槽类型 = `AnimFXController.cs` 里**已有**的顶层 `[Serializable] public class PlaySoundOnTime`（⛔ 没抄第二份、也没动那个文件）。
- 形态差（已在文件头标）：原版两槽 `private` + 预制体序列化；我们是代码里建 ⇒ `public` 字段（同 `UIEffectTogglerImage.fadeTargets` 口径），空槽 = 不出声（`Ready` 可判）。
- **断言缺口**：派活包要的「两槽 `numberOfTimesPlayed` 归零 / 递增」**读不到** —— 那个字段是 `AnimFXController.cs:182` 的 **private**，而那个文件不在本波白名单。
  改法（2 行，追加在 `PlaySoundOnTime` **类尾**、⛔ 别重构它以免行号漂移）：`public int PlayedCountForTest { get { return numberOfTimesPlayed; } }`。
  本类已自备等价读口 `ResetCount / DrivenOnCount / DrivenOffCount / LastDroveOn`（🔶 我们加的）。

### 自检
- `TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（现读）。
  ⚠️ 中途有一次 4~6 条错，全部在 `RuleEngine/Core/{Enchantment,EffectResolver}.cs`（并发代理正在改）⇒ 重跑一次即 0/0（按纪律注明）。
- **本轮没有改任何引擎行为**（新增的类与函数**都还没有调用点**：A-07 的 toggler 没有实例化方，A-02 的快照取不到）⇒ **不需要**跑 `ruleprobe`，也**不需要**重钉基线。
- **宿主自检**：本波不必跑。等资产/接线落地后再按覆盖面跑 —— A-02 落到设置窗 ⇒ `SettingsScene.Run`；A-07 若接到战斗/菜单的 toggler ⇒ 对应宿主（`BattleScene.Run` / `ShellScene.Run`）。

### 账补丁
`资料/完全复刻/账/_parts/B9_B2AU3.tsv` = **4 行**（`音频_语音.tsv` 的行 12 / 80 / 82 / 83；`状态` 一律未动仍 `C`，
改 `我们的落点` / `差异` / `备注` / `轮次`=`B9_B2AU3`）。生成器 `_parts/_gen_B9_B2AU3.py`（主表只读）。

### 没做完的部分（如实）
- **A-01 的 16 条逐条复核**没做（只抽查四件事，见上）。
- **A-02 的资产侧**（4~6 个快照 + 3 个 BattleGroup）必须由主对话跑 Unity 编辑器脚本。
- **A-06 与 A-01 的写点**都在白名单外（`Net/`、`Shell/`），按要求只写改法。

---

## 第五批（2026-10-27 · A-06 落地 + 测试读口 + A-01 的 16 条逐条复核）

### ① A-06（`CardPresentation/Net/**`）—— 已做完，三处
| # | 改动 | 落点 |
|---|---|---|
| ① | `ShouldRecord` 的注释「4 = HurryUp」→ **4 = Concede**（枚举 `0 Intro/1 MirrorMatch/2 Bored/3 ICantDoThat/4 Concede/5…10 Greet…Oops`） | `Net/NetChat.cs:107-108` + `:116` 行尾注释 |
| ② | **发送闸**从 `IsButtonLine(5…10)` **收窄成只用 `IsRelayable`**（`chatId != 0 && chatId != 1`） | `Net/NetBattle.cs:1248`（`OnLocalChat`） |
| ③ | **收侧保留 `IsButtonLine`**（防伪造越界）—— ⛔ 两侧的闸本来就不是同一条 | `Net/NetBattle.cs:1650`（未动） |

- 效果：`chatId = 4`（认输）**现在发得出去了** —— 与 `E4BDR` 已落的 `BattleDriver.OnLocalChat(4)` **两侧同时生效**。
- 判据 = 原版 `DisplayWarlordChatMessage.c:65` 的发送闸**只有 0/1 两道**，没有「必须落在 5…10」这一条（那条是我们自己加的，会把认输挡掉）。
- 警告文案跟着改了（原来印的是「不在 6 颗台词钮那一档」，现在印「0/1 开幕台词」）。

### ② `AnimFXController.cs` 的测试读口（2 行，**追加在 `PlaySoundOnTime` 类尾**）
`public int PlayedCountForTest { get { return numberOfTimesPlayed; } }` —— 只加读口、**行为零改动**、不重构（避行号漂移）。
⇒ 第四批 A-07 那两条验收断言（「`AnimationFinished()` 后两槽归零」/「`DoUpdate` 推过之后计数 > 0」）**现在读得到了**。

### ③ A-01 的 16 条逐条复核（判据表）

> 复核口径：每条都对着 `音频_语音.tsv` 的 `判据要点` + 反编译 `.c`，去代码里找实现与常量，**不是看账里写的 A 就抄 A**。
> 16 条**全部维持 A**；只发现 **1 处真缺口**（68 的接线），下面单列。

| 账行 | 方法 | 判 | 证据（file:line） |
|---|---|---|---|
| 19 | `Blend(AudioClip)` | **A** | `Core/AudioSourcesController.cs:160` `FadeOut()`→`SwitchMainAudioSource()`→`FadeIn(clip)`（两源交叉） |
| 20 | `DecrementVolume(src)` | **A** | `:236` `VolumeLerp(1→0)`，到点 `volume=0` **且** `enabled=false` |
| 21 | `FadeIn(clip)` | **A** | `:138` `PlayClip` → `IncrementVolume`（3 s 0→1） |
| 22 | `FadeOut()` | **A** | `:151` 取当前主源 → `DecrementVolume` |
| 23 | `IncrementVolume(src)` | **A** | `:222` `VolumeLerp(0→1)`，到点 `volume=1` |
| 27 | `SetMaxVolume(src)` | **A** | `:229` `volume=1.0`。⚠️ **原版全库无调用点**（`grep -rl` = 1 只有自己）⇒ 照抄形状、保持 private |
| 28 | `SetMinVolume(src)` | **A** | `:243` `volume=0.0`。同上，原版无调用点 |
| 29 | `Stop()` | **A** | `:170` `enabled=false` + `Stop()`（`__Stop.c:6,8`） |
| 30 | `SwitchMainAudioSource()` | **A** | `:184` `current==first ? second : first`；原版 private ⇒ 我们 private |
| 31 | `VolumeLerp(src,s,e)` | **A** | `:250` 3 s 线性、`t` 夹 [0,1]，由 `Tick(now)` 每帧推（原版 `Task` ⇒ 补间队列等价，🔶 已在文件头标） |
| 62 | `CheckQueue(sceneTarget)` | **A** | `Shell/ShellRuntime.cs:647`；两个调用点（`LoadSceneWithFade` 切前 + `OnSceneLoadedApplyChrome` 覆盖直调） |
| 63 | `DelayedQueuedPlay(clip,t)` | **A** | `ShellRuntime.cs:379-392` `TickMusic` 每帧判到点 ⇒ `PlayMusic(clip, **Blend**)`（原版 `WaitForSeconds` 的等价物） |
| 66 | `PlayMusic(clip,type)` | **A** | `ShellRuntime.cs:1226-1250` 三档 + **其他值照原版 `throw`**。66 的响度裁定已落：`0.2` 移到 `WarpforgeAudio.MusicVolumeMultiplier`（乘进 mixer） |
| 67 | `PlayMusicQueued(clip)` | **A** | `ShellRuntime.cs:1258-` `delay = len − time − 3.0`，下限 0、上限不夹 |
| 68 | `StopMusic(bool instant)` | **A（实现）／接线缺** | `ShellRuntime.cs:1303` `instant ? Stop() : FadeOut()`；⚠️ **`StopMenuMusic()`（`:1316`）全仓零调用点** ⇒ 见下 |
| 69 | `MusicVolumePlayerPrefsSetting.Start()` | **A** | `ShellRuntime.cs:1064-1068` `PlayerPrefs.GetFloat(MusicPrefKey, 源自身音量)` ⇒ `volume = 源音量 × v`；键 `"MusicVolume"` 在 `Core/WarpforgeAudio.cs:26`，另一读点 `:52` |

#### 复核里唯一的真缺口（68 的接线）
`StopMenuMusic()` **只有定义、没有任何调用点**（`grep -rn StopMenuMusic` = 1 条 = 它自己）。
原版那一句在 `BattleManager.OnInitializationComplete` ⇒ 我们这边必须由**开战入口**调，
否则**原症状照旧**：壳是 DDOL、菜单 idle 主题跟着活 ⇒ 进战斗后**菜单音乐与战斗 BGM 叠着放**。
精确改法（`E4BDR` 的文件）：
```csharp
// Battle/BattleDriver.cs 的 Begin（或 BuildHud 之后、战斗 BGM 起播之前）加一行：
// 判据 = BattleManager.OnInitializationComplete 里的 MusicManager.StopMusic()（原版默认档 = 淡出）
ShellRuntime.Instance?.StopMenuMusic(instant: false);
```
⚠️ 那句 `instant` 走哪一档要按原版那一处实参定（`StopMenuMusic` 的 doc 说「原版默认 = false = 3 s 淡出」）—— 我没读到 `OnInitializationComplete` 的实参，**这一格留给接线的人核**（别照我这行抄「false」当判据）。

### 自检
- `TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**（重跑后现读）。
  ⚠️ 中途一次 3 条错，全在 `Shell/CardDisplayEffects.cs`（并发代理在改，不是我的文件）⇒ 按纪律**重跑一次即 0/0**。
- 🔴 **本轮动了联机行为**（`Net/NetBattle.OnLocalChat` 的闸）⇒ **该跑 `NetSelfTest.Run` + `NetBattleTest.Run`**（铁律 12：碰 `Net/` 加跑联机那几条）。
  重点看：`chatId=4` 现在**发得出去**（改前被挡）；伪造 `chatId=2` 仍被**收侧**丢掉并出声。
- `CardPresentation/Battle/AnimFXController.cs` 只加了读口（行为零改动）⇒ 不必额外跑宿主。
- 三个文件都**纯 LF、无裸 CR**。

### 账补丁
`资料/完全复刻/账/_parts/B9_B2AU4.tsv` = **17 行**（A-01 的 16 行 + A-06 对应的 `音频_语音.tsv` 行 114
`VoiceLinesController.DisplayWarlordChatMessage`；`状态` 一律未动，改 `备注` / `轮次`=`B9_B2AU4`）。
生成器 `_parts/_gen_B9_B2AU4.py`（主表只读）。
⚠️ 行 114 原本是 `C` —— 我**没动它**（A-06 只做了 Net 那一半，显示链本体不归我），备注里写清了「哪一半做了、哪一半没做」。

---

## 第六批（2026-10-27 · `音频_语音.tsv` 状态 C 的 **34 条**逐条判定）

> 结论：**A 23 条 · B 8 条 · D 3 条**。**本轮 0 个 `.cs` 改动** —— 34 条里没有一条还能在我白名单内再往前推一步：
> 23 条早就实现了（多数是我前几批或 `E4BDR`/`E5P12`/`R1T3A` 做的），8 条只差**别人文件里的一行接线**，3 条是**原版自身不可达**。
> 全部判定与证据写进 `账/_parts/B9_B2AU5.tsv`（`状态` 从 `C` 改成 `A`/`B`/`D`）。

### A（23 条）—— 已实现且闭合，本轮只把状态补正
| 组 | 行 | 谁做的 / 证据 |
|---|---|---|
| A-04 单位语音条 | 85 · 87 · 93 · 94 · 97 · 124 | **我（第一批）**：`Battle/UnitChatPanel.cs`（`HideBox` / `isPlaying` / 并发寿命 / `AudioSource` 挂每颗气泡 / `+0x44` 0.1 s 等一拍） |
| A-03 聊天弹窗（自足那半） | 144 · 145 · 146 · 147 | **我（第一批）**：`Battle/ChatPopupPanel.cs`（OutCubic 淡变 / `IsAvailable` 3 元素表 / `IsIndexOutOfEnumBounds` 用 `ChatMessageEnumCount=11` / `IsMatchOneOfCollection`） |
| A-07 开关音 | 82 · 83 | **我（第四/五批）**：`Effects/UIEffectTogglerPlaySound.cs` + `PlayedCountForTest` 读口 |
| A-05 控制器族（`E4BDR`） | 102 · 103 · 108 · 114 · 115 · 118 · 131 · 132 · 133 · 136 | `E4BDR` 已落；本轮现核 `BattleDriver.cs` 命中数：`CanChat`30 · `CanUseVOs`8 · `CloseChatBox`19 · `DisplayWarlordRegularChatMessage`8 · `GenerateVoiceLineData`2 · `SetPopUpDisabled/Enabled`9 · `SetUpChatOptions`1 · `ShowHeroesIntroMessage`12 · `SetTransformedWarlordNewChats`6 |
| 稀有度 cue | **35** | **`E4BDR` 当天已修**（`rarity == "legendary" && !Has(Ambush)`；`GetInvokeCue.c:5-8` 只在 `num==4` 给 legendary ⇒ 伏击走普通 cue）。可达性 0（池内 0 张 ambush）⇒ 只能夹具验 |

### B（8 条）—— 实现都在，只差**别人文件里的一行**（⛔ 不是不做）
| 行 | 方法 | 缺什么 |
|---|---|---|
| 12 | `GetSnapshot(SnapshotsType)` | **资产侧**：`Main Mixer.mixer` 里建 6 个快照（**必须跑 Unity**），名字须 = `WarpforgeAudio.SnapshotNames` |
| 80 | `ToggleSFX(bool,bool)` | 同上（依赖 12）；调用点 = `Shell/SettingsWindow.cs`（`B1UI`） |
| 116 | `VoiceLinesController.ForcePopUpClose` | **调用点**：开选牌面板那一跳（原版 `ChoiceOfCardPlayer`）⇒ `BattleDriver` 一行 |
| 134 | `VoiceLinesController.SetUpChatOptions` | **接线**：`_chatPopup.SetChatContext(myDeck?.WarlordId, Ctx.MatchType)`（建完面板后一行） |
| 150 | `VoiceLinesPopupSelector.RefreshChatLines` | 同 134（拿到督军 id 之后钮上才是台词全文；今天仍印分类词 = 与改前逐字相同） |
| 151 | `VoiceLinesPopupSelector.SetTransformedWarlordNewChats` | 上游「督军换卡」整条未复刻 ⇒ 今天零调用者（方法已实现，2 元素表与 Setup 的 3 元素表**故意不统一**） |
| 152 | `VoiceLinesPopupSelector.SetupChatOptions` | 同 134 |
| 153 | `VoiceLinesPopupSelector.Show` | 同 150（`Show` 里已经在刷，只缺数据） |

### D（3 条）—— 原版自身不可达（铁律 11 例外①），**时间限定 = 2026-10-27 本 build 现读**
| 行 | 方法 | 复现（本轮亲跑） |
|---|---|---|
| 143 | `ClosePopupHorizontal()` | `grep -rl ClosePopupHorizontal d:/2/tools/decomp_full` = **1**（只有它自己） |
| 149 | `OpenPopupHorizontal()` | `grep -rl OpenPopupHorizontal …` = **1**（只有它自己） |
| 154 | `Toggle(bool)` | `head -3 VoiceLinesPopupSelector__Toggle.c` 的内容是 **`Card2DController__Toggle`**（文件名与内容不符） |

### 铁律 11 的交代
- **没有一条判「不做」**：8 条 B 全是「等一行接线 / 等一份 Unity 建的资产」；3 条 D 是**原版自己就没有调用者**（附复现命令 + 时间限定）。
- 🔴 **休眠机制**：35（伏击 cue）、116（选牌前关弹窗）、151（督军变身重设台词）**今天都不可达**，但**实现都在**
  （35 已在 `BattleDriver`、116/151 已在 `ChatPopupPanel`）—— 这正是铁律 11 要的「排先后、不是不做」。

### 自检
- **本轮 0 个 `.cs` 改动**（纯账 + 判据复核）⇒ 按铁律 12 不跑任何宿主；`TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` 现状 = **运行时 0 / 编辑器 0**。

### 账补丁
`资料/完全复刻/账/_parts/B9_B2AU5.tsv` = **34 行**（11 列同主表；**`状态` C→A/B/D**、`备注` 写证据与解锁条件、`轮次`=`B9_B2AU5`）。
生成器 `_parts/_gen_B9_B2AU5.py`（主表只读）。⚠️ 本轮**改了 `状态`**（前几批按规矩不动，这一批的任务就是判定）。





