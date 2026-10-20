# W58 · 战斗线 T18 —— 开场白 `customSound`（GUID → 音频）+ Intro 调用点查全（1020）

> 任务：把 `T18` 剩余的 `customSound` GUID → 音频接上，并把 `Intro` 调用点查全。
> 执行代理 **W58-T18**。⛔ 未动 `Editor/BattleScene.cs`（另一代理在写断言）· ⛔ 未跑 Unity / `*.Run` / player · ⛔ 未动 git、未改任何正本。

---

## 1. 结论

1. **`customSound` 已接通**：生成器把每条开场白的 `soundGuid` + 解出的 `soundAsset`（AudioClip 资产名）写进**全量件与运行时件**；运行时 `BattleDriver.SpeakIntro` 按原版 `ChatManager.GetChatSoundAsset` 的次序**先取定制音**，取不到则**出声一次并回落到无音**（气泡/文字照原版显示），⛔ 不猜成默认音。
2. **GUID 全部能解**：运行时 197 条开场白**每一条都带** `customSound` → **190 个不同 GUID**，用当前 build 的束枚举表 `guid_map_bundles.tsv` **190/190 解出 AudioClip**；旧 `guid_map.tsv` **0/190**（音频不在那份目录覆盖范围）。190 个音频文件在 `d:/2/新解包资源/assets_full/` 与我们的 `Resources/Art/audio/vo/` **两边都在**。
3. **`Intro` 调用点查全**：原版只有 **3 处**会产出 `ChatMessage.Intro=0` 的聊天（正常局的开场独白 · 教程 `AiChat` 空文本回落 · 教程 `AiChatBig` 空文本回落）。我们已接第 1 处；第 2/3 处在当前教程数据里**不可达**（63 条聊天动作 **63/63** 都带非空 `arg`；`AiChatBig` 数据 **0 条**），报告给出复用方式与缺口。
4. `HideCardShadow` **完全未涉及**（本次改动只碰聊天开场白的音源选择）。

---

## 2. `soundGuid` 解析三类统计（现读）

**来源**：`数据/游戏数据/card_chat_intro.json`（全量 60 张 / 244 条）与运行时件（49 张 / 197 条）。
字段出处：`CustomIntroChatData.cs:10` / `CustomIntroByArmyChatData.cs:10` —— `AssetReferenceTyped<AudioClip> customSound`。

| 类别 | 全量件 | 运行时件 |
|---|---|---|
| 开场白条目总数 | 244（精确 215 + 按阵营 29） | **197**（精确 168 + 按阵营 29） |
| 带 `soundGuid` 的条目 | 230（另 14 条在 11 张**非运行时** SO 上，无 `customSound`） | **197 / 197（100%）** |
| 不同 GUID 数 | **190** | **190** |
| 能解（GUID → AudioClip 资产名） | **190 / 190** | **190 / 190** |
| 解不出 | **0** | **0** |
| 本地有音频文件 | **190 / 190** | **190 / 190** |

- 解析表：**`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`**（6,174 行，2026-10-18 直接枚举本地束容器键）。
- ⚠️ 旧表 **`资料/普查产出_第二十七会话/guid_map.tsv`（4,985 行）对这批 GUID 命中 0/190** —— 它是从 Addressables 目录出发的，**音频不在那份覆盖里**；本任务仍把旧表读进来作兜底（primary 压过 fallback）。
- 资产类型/位置：**190/190 = `AudioClip`**，全部在 13 个 `bundle_*cardassets_assets_all` 里。
- 文件形态：**179 条 `.ogg` + 11 条 `.wav`**（`d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/AudioClip/<soundAsset>.<ext>`）。
- 我们的落点：`MyGame/Assets/CardPresentation/Resources/Art/audio/vo/<soundAsset>.<ext>` —— **190/190 已在**（语音线导入器 `工具/import_original_audio.py` 之前就按卡 SO 引用面导全）。
- 资产名不是统一 `vs*`：同时存在 `VO_Goff_Zagstruk_gen6` / `Sautekh_Orikan_mirror` 这种「定制音用别的台词池片段」的情形 —— 所以**只能按 GUID 解，不能按名字猜**。

---

## 3. 运行时接线（符号）

判据（逐句读）：`d:/2/tools/decomp_full/ChatManager__GetChatSoundAsset.c`

```
idx=0:
  非同一督军 || isPlayer:
    ① 精确档条目命中且 entry.customSound(+0x18) != null ⇒ AssetReferenceTyped<AudioClip>.Load(它)
    ② 否则按阵营档的同一条
    ③ 两档都没有 ⇒ RawCardScript.GetChatSound(0)（= introChatSound → defaultChatSound）
  同一督军 && !isPlayer ⇒ GetChatSound(1)（mirrorChatSound → defaultChatSound）
```

**落地**：

| 符号 | 改动 |
|---|---|
| `工具/gen_card_chat_intro.py` → `_read_guid_map()` / `_load_guid_map()` / `sound_asset()` | 读束枚举表（旧表兜底）；每条写 `soundGuid` + `soundAsset`；解不出记 `unresolvedSoundGuids` 并打印 |
| `UnitChatPanel.CardChatIntro.Entry` | 新增 `SoundGuid` / `SoundAsset` 两格（精确档、按阵营档都填） |
| `UnitChatPanel.CardChatIntro.TryResolve` | 两档返回值都带上这两格 |
| `UnitChatPanel.CardChatIntro.CustomSound(soundGuid, soundAsset)` | **唯一加载口**：`Resources.Load<AudioClip>(VoiceLines.ClipRoot + <asset>)`（与语音线同一条 `Art/audio/vo/`，⛔ 没新开播放器）；`soundGuid` 空 = 没有定制音（不算失败）；GUID 解不出 / 资源不在 ⇒ `Fail()` **`Debug.LogWarning` 一次**（按 GUID 去重）+ `SoundLoadFailures++` + `LastSoundError` |
| `UnitChatPanel.CardChatIntro.EntriesWithSound` / `SoundUnresolvedEntries` / `SoundLoadFailures` / `LastSoundError` / `ResetSoundWarningsForTest()` | 自检可读的口（解不出条目数、失败计数、原因） |
| `BattleDriver.SpeakIntro`（`BattleDriver.cs:6270`） | 先 `wantCustom = hasEntry && SoundGuid 非空`：<br>· **是** ⇒ `clip = CardChatIntro.CustomSound(...)`，`clipFile = SoundAsset`；即使 clip 为 null **也照常 `Speak`**（原版 `DisplayWarlordRegularChatMessage` clip 为 null 仍 `ShowChatBox`）；<br>· **否** ⇒ 原逻辑（`VoiceLines.TryPick` → `VoiceLines.Clip`，取不到就 return）<br>文字仍传 voice_lines 英文作兜底，官方词条由 `UnitChatPanel.Speak` 优先 |
| `UnitChatPanel.Speak` doc | 补一句「clip 为 null 的唯一例外 = 开场白定制音取不到，气泡照显示」 |

播放通道：仍是 `UnitChatPanel` 气泡上的那颗 `AudioSource`（`UnitChatPanel.cs:577/582`，`outputAudioMixerGroup = WarpforgeAudio.VoicesGroup`）⇒ 设置窗的「语音」音量照旧生效。

---

## 4. `Intro` 调用点清单

### 4·A 原版（`d:/2/tools/decomp_full/`，全仓 grep `GetChatMessage` / `GetChatSoundAsset` / `ShowHeroIntro*`）

| # | 调用点 | 链路 | 产出的音/文 | 我们 |
|---|---|---|---|---|
| 1 | `BattleManager._StartBattlePhase_d__334__MoveNext.c:146` | `VoiceLinesController.ShowHeroesIntroMessage` → `ShowHeroIntroMessage(isPlayer)` → `DisplayWarlordChatMessage(_, 0, isPlayer, 1)`（`VoiceLinesController._ShowHeroIntroMessage_d__64__MoveNext.c:17`） | `GetChatMessage(0,isPlayer)` + **`GetChatSoundAsset(0,isPlayer)`** | ✅ `BattleDriver.StartIntroMonologue` → `SpeakIntro`（唯一入口；`BeginBattleAfterSetup` 调，`BattleDriver.cs:5355`） |
| 2 | `AiScripted__ExecuteAction.c:237`（`AiChat 60` 且 `ScriptedAction.GetLocalizedText` **为空**） | `VoiceLinesController.DisplayWarlordChatMessage(vlc, 0, isPlayer=0, skipCanChat=1)` | AI 督军的 Intro（**含 customSound** + `GetChatMessage(0,0)`） | ❌ 未接（见 §4·C） |
| 3 | `AiScripted__ExecuteAction.c:331`（`AiChatBig 65` 且文本为空） | `ChatManager.GetChatMessage(0, 0)` → `BattleManager.DisplayBigChat` | 只有文本；**无声音** | ❌ 未接（`DisplayBigChat` 在本 build 反编译里是空壳，只有两行日志/校验：`BattleManager__DisplayBigChat.c`） |
| — | `VoiceLinesController__GetAudioToPlay.c:5` | `GetChatSoundAsset(...)` | — | **全仓无调用者**（间接/死代码，未接） |

闸门（#1）：`_StartBattlePhase` 里 `IsTutorialOrCampaignMatch()` 为真时**不进** `ShowHeroesIntroMessage`，改走 `ShowHeroesIntroMessageCampaign`（那条是战役脚本回合，不是聊天开场白，`BattleManager._ShowHeroesIntroMessageCampaign_d__339__MoveNext.c`）。
我们：教程局 `Ctx.MulliganOpen = false`（`RuleCore.cs:194` / `BattleEvent.cs:133` 的注释与算式）⇒ 走 `Begin()` 的「无换牌」支，**不调 `StartIntroMonologue`** ⇒ 结果与原版一致（机制不同、效果同）。

### 4·B 我们代码里与 Intro 相关的全部符号

- `BattleDriver.StartIntroMonologue()`（`BattleDriver.cs:6227`，`public` 供自检）→ 唯一产品调用点 `BeginBattleAfterSetup`（`:5355`）。
- `BattleDriver.TickIntroMonologue()` → `SpeakIntro(side)`（`:6266/6270`）。
- `UnitChatPanel.ChatTermKey` 的 `"Intro" → "-Chat0"`（`UnitChatPanel.cs:750`）—— 只在没有完整键时才会用到；`SpeakIntro` 现在总是传完整键（`chatTermOverride`）。
- `VoiceLines.ForVersus` / `ForMirror`（`BattleDriver.cs:6285-6286`）：③ 兜底/同督军那一链的**声音**顺序（`intro`→`default` / `mirror`→`default`）。
- 自检：`Editor/BattleScene.cs:15072`（点名触发）、`:14940-15110`（顺序/先手/静默断言）。

### 4·C 教程 / 战役那一支能不能复用

- **`AiChat` 空文本**（#2）：可以复用同一条链 ——
  `CardChatIntro.TryResolve(<AI 督军 id>, <玩家督军 id>, <玩家阵营>, out e)` → `CardChatIntro.CustomSound(e.SoundGuid, e.SoundAsset)` → `UnitChatPanel.Speak(side=1, …, text, clip, e.SoundAsset, chatTermOverride: e.Key ?? TermFor(aiId, "-Chat0"))`。**文字档 ③** = `-Chat0`（原版 `GetChatMessage(0,0)` 的兜底）。
- **当前不可达**：`tutorial_stages.json` 6 关 63 条聊天动作，**`arg` 全部非空**（`AiChat 27` / `PlayerChat 31` / `RadioMessage 5`），`AiChatBig` **0 条** ⇒ 原版那两个「空文本回落 Intro」今天一次都不会触发。
- **差什么**：① `DisplayBigChat` 在本 build 反编译里是空壳（方法体只剩校验/日志）⇒ #3 的**表现层**没法照抄，只能自定；② #2/#3 若将来数据出现空文本，需要先定「气泡 vs 大聊天窗」的表现（数据里 0 条，没有判据）。

---

## 5. 断言块（可直接粘贴）

### 5·A 数据级 —— 插在 `CardChatIntro` 数据断言那一节**之后**

**插入位置（符号）**：`Editor/BattleScene.cs` 里，`CardChatIntro` 那一节以
`+ "AM1→UM46 没有精确档 ⇒ **默认 1**（原版 \`IsPlayerFirstIntro\` **只**在精确档取值）");` + `}` 结束；
**紧接其后的空行**、`// 🔴 **先把上一节还挂着的气泡放完**` **之前** 插入：

```csharp
                    // ---- 🆕 2026-10-20（战斗线 `T18`）：开场白 `customSound`（原版 `GetChatSoundAsset`）----
                    //   判据 = `ChatManager__GetChatSoundAsset.c`（精确档/按阵营档的 customSound 优先，
                    //   两档都没有才回落卡自己的 GetChatSound(0)）；数据/生成器 = `工具/gen_card_chat_intro.py`。
                    {
                        Check(CardChatIntro.Loaded && CardChatIntro.EntriesWithSound == 197
                              && CardChatIntro.SoundUnresolvedEntries == 0,
                              $"★ 开场白定制音：带 `soundGuid` 的条目 {CardChatIntro.EntriesWithSound} 条"
                            + $"（预期 197 = 运行时件全部条目）· GUID 解不出 {CardChatIntro.SoundUnresolvedEntries}（预期 0）");
                        CardChatIntro.Entry se1;
                        bool okS = CardChatIntro.TryResolve("AM1", "BL34", "BlackLegion", out se1);
                        Check(okS
                              && se1.SoundGuid == "e95f2810256747844821683a031771bc"
                              && se1.SoundAsset == "VO_AM_Arcadian Leontus_vsAbaddon",
                              $"★ 精确档条目带上了 `customSound`：AM1 vs BL34 → guid `{se1.SoundGuid}` / "
                            + $"asset `{se1.SoundAsset}`（期望 e95f2810… / VO_AM_Arcadian Leontus_vsAbaddon）");
                        var clipS = CardChatIntro.CustomSound(se1.SoundGuid, se1.SoundAsset);
                        Check(clipS != null,
                              $"★ 定制音从 `Resources/{VoiceLines.ClipRoot}<asset>` 真能取到"
                            + (clipS != null ? $"（{se1.SoundAsset}，{clipS.length:F2}s）" : "（**取不到**）"));
                        // **缺失时出声且不崩**：GUID 在、资源不在 ⇒ null + 失败计数 +1 + 留原因（日志另有一行 warning）
                        int fail0 = CardChatIntro.SoundLoadFailures;
                        var clipBad = CardChatIntro.CustomSound("00000000000000000000000000000000",
                                                                "__W58_no_such_custom_sound__");
                        Check(clipBad == null && CardChatIntro.SoundLoadFailures == fail0 + 1
                              && !string.IsNullOrEmpty(CardChatIntro.LastSoundError),
                              $"★ 定制音缺失 ⇒ 返回 null（不崩）+ 计一次失败 + 留下原因：`{CardChatIntro.LastSoundError}`");
                        // GUID 解不出资产名（生成器没解出来）同样出声、同样不崩
                        int fail1 = CardChatIntro.SoundLoadFailures;
                        var clipNoAsset = CardChatIntro.CustomSound("ffffffffffffffffffffffffffffffff", "");
                        Check(clipNoAsset == null && CardChatIntro.SoundLoadFailures == fail1 + 1
                              && !string.IsNullOrEmpty(CardChatIntro.LastSoundError),
                              $"★ GUID 解不出资产名 ⇒ 返回 null（不崩）+ 计一次失败：`{CardChatIntro.LastSoundError}`");
                        // 文本那半不受影响：同一条目的 Key / hasText 仍对
                        Check(se1.Key == "Chat_Messages/AM1-Chat0-BL34" && se1.HasText,
                              $"★ 加定制音后**文本仍走官方键**：`{se1.Key}` / hasText={se1.HasText}");
                    }
```

### 5·B 端到端 —— 插在 `Check(firstSpoke, …)` **之后**、`// ② **串行的核心判据**` **之前**

**插入位置（符号）**：`Editor/BattleScene.cs` 里 `Check(firstSpoke, $"★ 先手（{…Name}）先开口");` 那一行之后。

```csharp
                    // ---- 🆕 2026-10-20（`T18`）：真播的那一条用的是 `customSound`（不是 voice_lines 里那条）----
                    {
                        var spkS = drv.Ctx.Players[firstSide].Warlord.Card;
                        var oppS = drv.Ctx.Players[1 - firstSide].Warlord.Card;
                        bool mirrorS = spkS.Id == oppS.Id;
                        bool isPlayerS = firstSide == drv.MySeat;
                        CardChatIntro.Entry eS;
                        bool hasS = (!mirrorS || isPlayerS)
                                 && CardChatIntro.TryResolve(spkS.Id, oppS.Id, oppS.Faction, out eS);
                        if (hasS && !string.IsNullOrEmpty(eS.SoundAsset))
                            Check(chat.LastClipLoaded && chat.LastClipName == eS.SoundAsset,
                                  $"★ 先手开场白**播的是 customSound**（原版 `GetChatSoundAsset`）："
                                + $"期望 `{eS.SoundAsset}` / 实得 `{chat.LastClipName}`（loaded={chat.LastClipLoaded}）");
                        else
                            Debug.Log(P + "   （^ 本局先手这一对没有定制音条目 ⇒ 这一条**没测**，不是通过）");
                    }
```

> 断言覆盖：带 `soundGuid` 的条目解析（5·A 第 1-3 条）· 缺失时出声且不崩（5·A 第 4-5 条）· 文本仍正确（5·A 第 6 条 + 原有 `CardChatIntro` 文本断言）· 真播路径用定制音（5·B）。

---

## 6. 没查清的部分

1. **`AssetReferenceTyped<AudioClip>.Load` 失败时原版返回什么**：`GetChatSoundAsset` 命中条目后只在 `+0x18 != 0` 时 Load；解引用失败的分支在反编译里落在「无返回」helper（`FUN_1803f47a0`，同族都是空引用抛出），**没法判定它是抛异常还是交空**。我们的口径 = **回落到无音**（最接近 `DisplayWarlordRegularChatMessage` 对 null clip 的处理），已在代码注释与本报告写明。
2. **非运行时 SO 的 14 条无 `customSound` 条目**：不影响运行时件；若将来教程/战役用它们，需要按 §4·C 的复用链补。
3. **`DisplayBigChat` 的表现层**：本 build 反编译是空壳（`BattleManager__DisplayBigChat.c` 只剩校验 + 日志），原版大聊天窗长什么样**查不到** ⇒ 若将来要接 #3，表现层是缺口。
4. **`VoiceLinesController.GetAudioToPlay` 的调用者**：全仓 grep 不到调用点（只有定义），是否经反射/委托调用**未查**。
5. ⚠️ **顺手发现（未改）**：`Begin()` 的「无换牌」支（`BattleDriver.cs:3758` 一带）**不调 `StartIntroMonologue`**。教程局靠它偶然对上原版闸门；但**非教程的 No-mulligan 模式**（遭遇战等）原版 `StartBattlePhase` 仍会播开场独白 ⇒ 我们那里**少播一次**。本任务未动（属另一条账）。

---

## 7. 改动清单

| 文件 | 性质 |
|---|---|
| `d:/4/Unity/工具/gen_card_chat_intro.py` | 扩：读 `guid_map_bundles.tsv`（旧表兜底）→ 每条写 `soundGuid` + `soundAsset`；统计/出声；`--check` 幂等 |
| `d:/4/Unity/数据/游戏数据/card_chat_intro.json` | 由生成器写（全量件加两格 + `unresolvedSoundGuids` + counts） |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_chat_intro.json` | 由生成器写（运行时件加两格 + counts） |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/UnitChatPanel.cs` | `Entry`/`OppEntry`/`ArmyEntry` 加两格；解析；`CustomSound` 加载口 + 失败出声 + 自检口 |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | `SpeakIntro` 先取定制音；取不到演无音；文字/键逻辑不变 |

- `git diff --numstat`（工作区现读）：`BattleDriver.cs 110/21` · `UnitChatPanel.cs 147/7` · `Resources/card_chat_intro.json 596/199` · `gen_card_chat_intro.py 138/9` · `数据/游戏数据/card_chat_intro.json 499/247`（JSON 行数是「每条多两格」的正常增长，不是行尾翻转）。
  ⚠️ `BattleDriver.cs` 的 110/21 **含另一代理 `T12`（开局运镜）在本任务开始前就已存在的未提交改动**；本任务在该文件里的净改动只有 `SpeakIntro` 那一段（`hasEntry` + `wantCustom` + 选 clip/clipFile）。`UnitChatPanel.cs` 的改动全部是本任务的。
- 行尾：`BattleDriver.cs` 仍 **CRLF（22626/22626）**；`UnitChatPanel.cs` 仍 **LF**；生成器仍 **LF**。
- ⛔ `Editor/BattleScene.cs`、`Core/`、`Shell/`、正本、`CLAUDE.md`、`项目任务.md`：**未碰**。

## 8. typecheck 结果

```
TMPDIR=/tmp/wf_t18 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

生成器自检：`python -I -X utf8 工具/gen_card_chat_intro.py --check` **连跑两次均「全过 ✅」**。
