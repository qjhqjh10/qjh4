# W58 · 战斗线 —— 非教程 No-mulligan 模式的开场独白（`W58-INTROFIX`，1020）

> 执行代理 **W58-INTROFIX**。任务 = 修掉 `T18` 顺手查出的真偏离：**非教程的 No-mulligan 模式我们原来不播开场独白，原版会播**。
> ⛔ 未碰 `Editor/BattleScene.cs`（另一代理 `A1220` 占用）、`Core/`、`Shell/`、`RuleEngine/`、任何正本。
> ⛔ 未跑 Unity / `*.Run` / player / 截图；未动 git。✅ 只跑秒级 typecheck。

---

## 1. 结论

1. **原版闸门已现核**：会话里那句「非教程的 No-mulligan 模式会播」**成立**，判据链四条（见 §2），关键点：
   `_StartBattleSequence` 只把**教程**分叉到 `TutorialStartSequence`；其余（含 `showMulligan = false` 的遭遇模式）
   一律走 `_SetupMulliganPhase` → `ClickMulliganDone`（不弹换牌 UI）→ `FinishMulliganFinalPhase`
   → `StartBattlePhase` → **`ShowHeroesIntroMessage`（开场独白）**。
2. **已在无换牌路径补上调用**：`BattleDriver.Begin` 的「无换牌」支在 `InitializeAutoZoom()` 之后
   按**模式自己的开关** `Ctx.Vars.showMulligan == false` 调 `StartIntroMonologue()`。
   教程 / 战役 / 教程回放 / CutScene 由 `StartIntroMonologue` 内部的原版闸门挡住。
3. **避免重复播**：新增 `_introStarted` 闩（一局一次），`Begin` 每局复位（同时把 `_introStage` 拨回 -1，
   防止上一局没播完的独白在下一局接着念）。
4. **判据只留一处**：闸门收在 `StartIntroMonologue` → `IsTutorialOrCampaignMatch(Ctx.MatchType)`，
   两个调用点（换牌后 `BeginBattleAfterSetup` / 无换牌支）共用；helper 复用 `MatchTypes.IsTutorialMatch ∪ IsCampaignLike`。
5. 复用现有 `SpeakIntro` / `CardChatIntro.CustomSound`：无换牌路与换牌路**同一条**播法，定制音照旧生效。
6. typecheck：运行时 0 / 编辑器 0。

---

## 2. 原版闸门表（状态 → 播不播 + 出处）

| # | 状态（原版口径） | 开场独白（`ShowHeroesIntroMessage`） | 出处（`d:/2/tools/decomp_full/`） |
|---|---|---|---|
| 1 | **教程**（`matchType = 100 Tutorial`） | **不播**（走 `TutorialStartSequence` → 战役/教程那条 `ShowHeroesIntroMessageCampaign` 之外的分支；聊天 Intro 不产出） | `BattleManager._StartBattleSequence_d__331__MoveNext.c:81-88`（`IsTutorialMatch` ⇒ `TutorialStartSequence`）· `BattleManager._StartBattlePhase_d__334__MoveNext.c:143-148` |
| 2 | **战役**（`130 Campaign`） | **不播**（`IsTutorialOrCampaignMatch() == 1`） | `BattleManager__IsTutorialOrCampaignMatch.c`（真值集 `{100,130,140,190}`）· `_StartBattlePhase:143-148` |
| 3 | **教程回放**（`140 TutorialReplay`） | **不播**（同上真值集；且 `IsTutorialMatch` 也为真） | 同上 |
| 4 | **CutScene / Scene**（`190 Scene`） | **不播**（同上真值集） | 同上 |
| 5 | **普通局 · 有换牌**（`showMulligan = true`，如 Classic / Practice / Duel…） | **播**（等玩家换完牌、`StartBattlePhase` 时播） | `_SetupMulliganPhase_d__341__MoveNext.c:46`（`showMulligan != 0` ⇒ 走换牌 UI）→ `_FinishMulliganFinalPhase_d__351__MoveNext.c:233` → `_StartBattlePhase:141-148` |
| 6 | **普通局 · No-mulligan**（`showMulligan = false`，如遭遇 / Skirmish 文案 `No mulligan`） | **播**（不弹换牌 UI，直接 `ClickMulliganDone` → 同一收尾链） | `_SetupMulliganPhase_d__341__MoveNext.c:88-91`（`*([matchData+0xa8]+0x49) == 0` ⇒ `ClickMulliganDone`）· `_FinishMulliganFinalPhase:233` · `_StartBattlePhase:141-148` |
| 7 | **回放**（`matchType = 160 Replay`） | 原版**会**播（160 不在排除集；`_StartBattlePhase` 先 `ClickPlayReplay` 再走同一条闸） | `_StartBattlePhase_d__334__MoveNext.c:36-47` 与 `:141-148` |
| 8 | **观战** | **查不到独立入口**：本 build 的 `MatchType` 19 档里没有「观战」；没有可复刻的状态 | `RuleEngine/Core/BattleContext.cs:1617-1638`（19 档枚举，逐字照抄 dump） |

> 判据明细（与上表逐条对应）：
> - 排除集的**唯一真值** = `IsTutorialOrCampaignMatch`：`matchType` 落在 `{100, 130, 140, 190}` ⇒ 真。
>   `BattleManager__IsTutorialOrCampaignMatch.c` 的分支可逐句读成：
>   `< 0x83` 时 `≠100 && ≠130` ⇒ 假；`≥0x83` 时 `≠140 && ≠190` ⇒ 假；否则真。
> - `_StartBattlePhase:141` 那一句 `SpriteAtlasReference__ValidateAsset(self, 10)` 走的是另一个分支
>   （真 ⇒ `ShowHeroesIntroMessageCampaign`）。⚠️ 这个符号名是反编译的误标 thunk，**方法体读不到**（见 §5·1）。
>   它只影响「战役脚本开场」那一条，对第 5/6/7 档（我们要复刻的三档）判据不构成反例。

### 2·1 我们改前 / 改后对照

| 我们的路径 | 改前 | 改后 |
|---|---|---|
| 换牌路（`MulliganOpen == true` → `BeginBattleAfterSetup`） | 播（`StartIntroMonologue`） | 播（闸门不变：非教程/战役） |
| **无换牌路**（`MulliganOpen == false`） | **一句都不播**（只在 `MulliganOpen` 支里 return，`BeginBattleAfterSetup` 够不到） | **`Vars.showMulligan == false`（模式自己说 No mulligan）⇒ 播**；教程/战役照旧不播 |
| 教程（`MulliganOpen == false`、`Vars.showMulligan == false`） | 不播（偶然对上） | 不播（由模式闸明确挡住） |

⚠️ **闸门为什么用 `Ctx.Vars.showMulligan`、不用 `!Ctx.MulliganOpen`**：
`mulliganEnabled` 是我们**批处理自检的钉子**（`BattleDriver.cs:5144` 默认 `false`；产品场景 `Battle.unity` 里烘成 `true`，
`BattleScene.BuildAndSaveScene:178-179` 显式置 true），**原版没有对应物**。若拿 `!MulliganOpen` 当模式，
整套 `BattleScene.Run` 里几十处 `Begin(...)`（默认 `mulliganEnabled=false`）都会开局自动播独白，
污染后面所有气泡/语音断言 —— 而那**不是原版的任何一种模式**。

---

## 3. 改法（符号）

### 3·1 `CardPresentation/Battle/BattleDriver.cs`（CRLF，行尾未变）

| 行 | 符号 | 改动 |
|---|---|---|
| `:3315-3320` | `Begin` 的「本局一次的闩」段 | 新增 `_introStarted = false;` + `_introStage = -1;`（每局复位；防「第 2 局一句都不播」与「上一局没播完的独白接念到新一局」） |
| `:3769-3787` | `Begin` 的无换牌支（`InitializeAutoZoom()` 之后、`SetHint` 之前） | 新增：`if (!Ctx.Vars.showMulligan) StartIntroMonologue();`（四条反编译出处写在注释里） |
| `:6246-6248` | 新字段 `_introStarted` | 一局只播一次的闩 |
| `:6252-6275` | `StartIntroMonologue()` | ① 新闸：`if (Ctx == null \|\| IsTutorialOrCampaignMatch(Ctx.MatchType)) return;` ② `if (_introStarted) return;`，通过后 `_introStarted = true;`（原「无条件 `_introStage=-1`」挪到过了闸之后） |
| `:6277-6284` | 新 helper `static bool IsTutorialOrCampaignMatch(MatchType t)` | `MatchTypes.IsTutorialMatch(t) \|\| MatchTypes.IsCampaignLike(t)` —— 与 `BattleManager__IsTutorialOrCampaignMatch.c` 的 `{100,130,140,190}` 同集，⛔ 不另列四个数 |

- 复用点：`SpeakIntro`（选键 + `CardChatIntro.CustomSound`）、`UnitChatPanel.Speak`、`TickIntroMonologue` —— **零新播放器/零新气泡**。
- `UnitChatPanel.cs` **未改**（T18 的 `Entry.SoundGuid/SoundAsset` + `CustomSound` 已够用）。
- 细节：`_introStarted` 只在「闸过 + `_unitChat` 就绪 + `VoiceLines.Ready`」之后置真 ——
  若 HUD/语音表还没就绪，保留后续再试的机会。

---

## 4. 断言块（可直接粘贴）

**插入位置（符号）**：`Editor/BattleScene.cs` 第 14c 节「开局独白」块内，
`Check(drv.IntroStage < 0, $"★ 两条都播完 → 独白自己收尾（实得 {drv.IntroStage}）");`
那一行**之后**、该 `{ }` 的闭合 `}` 之前（现读 `:15167-15169`）。

```csharp
                    // ---- 🆕 2026-10-20（战斗线 `W58-INTROFIX`）：**非教程的 No-mulligan 模式也播开场独白** ----
                    //   原版判据（逐句）：
                    //    · `BattleManager._StartBattleSequence_d__331__MoveNext.c:81-88` —— 只有教程走
                    //      `TutorialStartSequence`；其余（含 `showMulligan = false` 的遭遇）走 `_SetupMulliganPhase`；
                    //    · `BattleManager._SetupMulliganPhase_d__341__MoveNext.c:88-91` —— `showMulligan == 0`
                    //      ⇒ 不弹换牌 UI，直接 `ClickMulliganDone`（= 我们这条「无换牌」路）；
                    //    · `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:233` → `StartBattlePhase`；
                    //    · `BattleManager._StartBattlePhase_d__334__MoveNext.c:141-148` ——
                    //      `IsTutorialOrCampaignMatch() == 0` 才 `ShowHeroesIntroMessage`
                    //      （真值集 = matchType {100,130,140,190}，`BattleManager__IsTutorialOrCampaignMatch.c`）。
                    {
                        var poolNM = CardDatabase.Load();
                        string myFacNM = drv.Ctx.Players[drv.MySeat].Warlord.Card.Faction;
                        string foeFacNM = drv.Ctx.Players[1 - drv.MySeat].Warlord.Card.Faction;
                        var myNM = MakeDeck(poolNM, myFacNM, 0, "自检·W58 非教程无换牌(我)", (int)GameMode.Skirmish);
                        var foeNM = MakeDeck(poolNM, foeFacNM, 0, "自检·W58 非教程无换牌(敌)", (int)GameMode.Skirmish);
                        Check(myNM != null && foeNM != null,
                              $"（前提）凑出两副遭遇牌（{myFacNM} / {foeFacNM}）");
                        bool savedMulNM = drv.mulliganEnabled;
                        try
                        {
                            drv.mulliganEnabled = false;
                            drv.Begin(seed: 20261020, myDeck: myNM, foeDeck: foeNM, foeFaction: foeFacNM,
                                      vars: GameplayVariables.For(GameMode.Skirmish));
                            Check(!drv.Ctx.Vars.showMulligan && !drv.Ctx.MulliganOpen
                                  && drv.Ctx.MatchType == MatchTypes.For(GameMode.Skirmish),
                                  "（前提）这一局是**非教程的 No-mulligan 模式**（遭遇）："
                                + $"showMulligan={drv.Ctx.Vars.showMulligan} / MulliganOpen={drv.Ctx.MulliganOpen}"
                                + $" / MatchType={drv.Ctx.MatchType}（{(int)drv.Ctx.MatchType}）");
                            // ① 🔴 **灭自证**：把闸门改回「只走换牌路」（删掉 `Begin` 无换牌支那一句）
                            //    ⇒ `IntroStage` 恒 -1，本条必红。
                            Check(drv.IntroStage == 0,
                                  $"★ 非教程 No-mulligan（遭遇）开局 ⇒ 开场独白**自动起播**"
                                + $"（状态机 = {drv.IntroStage}，期望 0；旧实现「只走换牌路」会给 -1）");
                            int firstNM = drv.IntroFirstForTest;
                            Check(firstNM == 0 || firstNM == 1,
                                  $"★ 先手侧定下来了（原版 `IsPlayerFirstIntro`；实得 {firstNM}）");
                            bool spokeNM = false;
                            for (float t = 0f; t < 1f && !spokeNM; t += 1f / 30f)
                            { Step(1f / 30f); spokeNM = chat.LastEvent == "Intro" && chat.ShownSide == firstNM; }
                            Check(spokeNM,
                                  $"★ 非教程 No-mulligan 也**真的开口**（`LastEvent=Intro` + 气泡在 {firstNM} 侧；"
                                + $"实得 LastEvent={chat.LastEvent} / ShownSide={chat.ShownSide}）"
                                + $" —— 文本「{chat.ShownText}」");
                            // ② 无换牌路的开场白与换牌路**同一条 `SpeakIntro`** ⇒ 官方键 / customSound 都要对。
                            var spkNM = drv.Ctx.Players[firstNM].Warlord.Card;
                            var oppNM = drv.Ctx.Players[1 - firstNM].Warlord.Card;
                            bool mirrorNM = spkNM.Id == oppNM.Id;
                            bool isPlayerNM = firstNM == drv.MySeat;
                            CardChatIntro.Entry eNM = default(CardChatIntro.Entry);
                            bool hasNM = (!mirrorNM || isPlayerNM)
                                      && CardChatIntro.TryResolve(spkNM.Id, oppNM.Id, oppNM.Faction, out eNM);
                            string wantTermNM = hasNM ? eNM.Key
                                             : CardChatIntro.TermFor(spkNM.Id,
                                                   mirrorNM && !isPlayerNM ? "-Chat1" : "-Chat0");
                            Check(chat.LastChatTerm == wantTermNM,
                                  $"★ 无换牌路的开场白**文本键**照旧（原版 `GetChatMessage`）："
                                + $"期望 `{wantTermNM}` / 实得 `{chat.LastChatTerm}`");
                            if (hasNM && !string.IsNullOrEmpty(eNM.SoundAsset))
                                Check(chat.LastClipLoaded && chat.LastClipName == eNM.SoundAsset,
                                      $"★ 无换牌路的开场白**播的是 customSound**（原版 `GetChatSoundAsset`）："
                                    + $"期望 `{eNM.SoundAsset}` / 实得 `{chat.LastClipName}`（loaded={chat.LastClipLoaded}）");
                            else
                                Debug.Log(P + "   （^ 这一对没有定制音条目 ⇒ customSound 那条**没测**，不是通过）");
                            // ③ **教程仍不播**（`IsTutorialOrCampaignMatch` 把 100/130/140/190 挡在外面）。
                            //    先把这一局的独白放完，否则残留气泡会让下一条读错。
                            for (float t = 0f; t < 10f && drv.IntroStage >= 0; t += 1f / 30f) Step(1f / 30f);
                            drv.BeginTutorial(0);
                            Check(drv.Ctx.Tutorial != null && !drv.Ctx.MulliganOpen
                                  && drv.Ctx.MatchType == MatchType.Tutorial,
                                  "（前提）教程局：无换牌 + `matchType = 100`");
                            Check(drv.IntroStage < 0,
                                  $"★ 教程局 ⇒ **不播**开场独白（原版 `IsTutorialOrCampaignMatch`；"
                                + $"实得状态机 {drv.IntroStage}）");
                        }
                        finally
                        {
                            // 还原这一节原来的局面（后续小节复用同一个 driver：经典 + 原来的 `mulliganEnabled`）
                            drv.mulliganEnabled = savedMulNM;
                            drv.Begin("Ultramarines", "Goff", 20260917);
                        }
                    }
```

**覆盖 / 灭自证说明**

| 断言 | 证什么 | 灭自证（改哪一处会红） |
|---|---|---|
| ① `IntroStage == 0` | 非教程 No-mulligan 开局**自动起播** | 删掉 `BattleDriver.cs:3786` 的 `StartIntroMonologue()`（= 退回「只走换牌路」）⇒ 恒 -1，必红 |
| ①`spokeNM` | 真开口（气泡在正确一侧、事件名 `Intro`） | 同上；另把 `StartIntroMonologue` 的闸改成无条件 return 也会红 |
| ② `LastChatTerm` | 无换牌路文本键与 `GetChatMessage` 一致 | 把 `Begin` 里的调用改成另写一套「无键」的 Speak ⇒ 红 |
| ② `LastClipName` | 无换牌路也走 `customSound`（`GetChatSoundAsset`） | 把 `SpeakIntro` 里 `wantCustom` 那支改回 voice_lines ⇒ 条目对得上时红 |
| ③ 教程 `IntroStage < 0` | 教程**不播**（闸门真值集） | 把 `IsTutorialOrCampaignMatch` 删成 `return false;` ⇒ 教程局会起播，本条红 |

⚠️ 块尾**必须**还原（`finally` 里的 `Begin("Ultramarines","Goff",20260917)`）——
后面 ChatPopup 等小节复用同一个 `BattleDriver`，不还原会把「教程局」留给它们。

---

## 5. 没查清的部分（⛔ 不猜）

1. **`_StartBattlePhase:141` 的 `SpriteAtlasReference__ValidateAsset(self, 10)` 读不到方法体**：
   这个符号名是反编译的误标（同名 thunk 出现在几十个 `.c` 文件顶部，真实签名被剥），
   而它守的是「**战役脚本开场**」那条分支。对我们复刻的三档（普通有换牌 / 普通 No-mulligan / 回放）
   它应当为假（否则这些档会走战役开场），但**没有逐句判据** ⇒ 只按 `IsTutorialOrCampaignMatch` 建闸。
   若将来发现某个**非战役**模式也走进那条分支，再回来补。
2. **回放（160）这条路我们没有自动起播的落点**：`PlayReplay` 走 `BeginFromPendingCore → Begin` +
   直接灌动作，`MulliganDone` 只走 `RuleCore.EndMulligan` / `NetAfterTurnStart`，
   **不经过** `OnMulliganDone → BeginBattleAfterSetup`。原版回放会播（160 不在排除集），
   我们**仍然不播** —— 这是**本次未修**的同类缺口（不在本任务目标内，如实记账）。
   若录的是遭遇（`showMulligan=false`）回放，`Begin` 的 no-mulligan 支会起播，反而与原版一致；
   不对称只在「录的是有换牌的模式」。
3. **观战**：本 build `MatchType` 19 档里没有观战档，找不到可复刻的状态（表里已标「查不到」）。
4. **战役局在我们工程里的入口**：`GameMode.Campaign` 存在，但没有实际开局链；
   闸门已按 `matchType = 130` 写好，将来接战役时不需要再改这个文件。
5. 自检口径：本波**没有跑**任何 `*.Run`（铁律 12），断言块只做了静态核对（类型/符号/作用域），
   第一次真跑才能确认 `BeginTutorial(0)` 在本节上下文里的耗时与副作用可接受。

---

## 6. 改动清单

| 文件 | 性质 | 行尾 |
|---|---|---|
| `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | 3 处：`Begin` 复位闩（`:3319-3324`）· 无换牌支调用（`:3769-3787`）· `StartIntroMonologue` 闸 + `_introStarted` + `IsTutorialOrCampaignMatch`（`:6246-6284`） | **CRLF 保持**（22673 CRLF / 22673 LF / bare LF **0**，二进制复核） |
| `d:/4/Unity/资料/普查产出_1020/W58_战斗线_NoMulligan开场独白.md` | 本报告（新建） | LF |

- `git diff --numstat`（本文件，工作区现读）：`BattleDriver.cs 159/23`。
  ⚠️ 其中 **110/21 是 T12（开局运镜）+ T18（customSound）在本任务开始前就有的未提交改动**；
  本任务净增 **49 行 / 删 2 行**（都在上面三处）。
- `UnitChatPanel.cs` 本任务**零改动**（`git diff` 的 147/7 全部来自前面的 T18/T17）。
- ⛔ 未碰 `Editor/BattleScene.cs`（断言块只写在本报告 §4，交给主对话合并）、`Core/`、`Shell/`、`RuleEngine/`、正本。

---

## 7. typecheck 结果

```
TMPDIR=/tmp/wf_introfix bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

最后一次跑在**全部改动之后**，无并发干扰（错误数 0）。
