# B9 · J5AU · 音频/语音线 剩余 C 50 条 施工单 + cue→WindowSounds 映射（只读判据）

> 代理 `J5AU` · 第八十四会话 · **2026-10-26** · **只读**：未改 `.cs` / 资产 / 主账表 / 正本；未跑 Unity；未动 git（唯一写出的是本文件 + 两张判据产物）。
> 产物：`判据/B9_音频C_施工单.tsv`（12 列 × 50）· `判据/B9_音频C_派活包.md` · **`判据/音效cue到WindowSounds_映射.tsv`（12 列 × 614，音效线欠的那一步）**

---

## §一 结论（先看这 8 条）

1. **50 条 C 行全部逐条读过判据**（第一权威 `d:/2/tools/decomp_full/`，次 = 解包资源字段），50/50 都能指到具体行；**没有一条是「判据查不到」**。建议分档：**P1 6 条 · P2 41 条 · D 3 条**（D = 原版 0 调用点/文件名与内容不符）。
2. 🔴 **推翻了账里 2 条「死代码」结论**：`VoiceLinesController.SetPopUpDisabled/SetPopUpEnabled`（账行 131/132）**不是死代码** —— 调用点实读存在（`BattleManager__OnInitializationComplete.c:20`、`BattleManager._ShowHeroesIntroMessageCampaign_d__339_MoveNext.c:41/48`）。账里当年看到的「0 调用点」是因为 `Missions__BlockAutoUpdates.c` / `Missions__EnableAutoUpdates.c` 这两个文件的内容**就是**这两个方法体（文件名与内容不符的旧坑）⇒ 按名字找调用点拿到假货。**这两条应改 P2 并排进开工单**。
3. 🔴 **`+0x78` 的语义结案**（账行 102/116/131/132/136 五条都依赖它）：`PopUpShowed` 写 **0**、`PopUpHid` 写 **1** ⇒ **1 = 无弹窗（可聊天）、0 = 弹窗开着（禁聊天）**。`CanChat(isPlayer=true)` 要求它非 0、`ForcePopUpClose` 要求它为 0、`ClickChat` 要求它非 0 —— **三处逻辑自洽**。⇒ 账行 116 备注里那句「原版前置条件读出来是反的」**可以划掉**。
4. 🔴 **发现一条判据用错（会影响听感）**：原版 `MusicManager.PlayMusic` 的 **Instant 档把 `source.volume` 强制写成 1.0**（`MusicManager__PlayMusic.c:71-78` 读静态表 `[0]`），而我们把序列化的 `MusicVolumeMultiplier = 0.2` 当系数长期乘着（`ShellRuntime.cs:84/839/1130`）⇒ **同一条 `VolumeMusic` 下我们比原版小 5 倍**。这条要动一条既有断言（`Editor/ShellScene.cs:4732`）⇒ **必须调度台裁**（按原版 1.0 / 保留 0.2 并标 `🔶 自己设计`）。
5. 🔴 **账行 35 判反了**：原版 `CardToMinionAnim` 里 `rarity = HasCurrentTrait(ambush) ? 1 : cardRarity`（`CardScript__CardToMinionAnim.c:20-26`）⇒ **伏击单位走普通 `invokeCUE`**；我们从 2026-10-16 起写成「伏击 ⇒ legendary」。卡池现 0 张 ambush ⇒ 只能靠夹具验。
6. 🔴 **账里「我们的落点」行号已大面积过期**：`BattleDriver.cs` 漂移 **+78 ~ +528 行**（6 处逐条列表见派活包 §3·5）；账行 87/124 引的 `UnitChatPanel.cs:7548/7613/7616` **文件都错了**（`SpeakCantDo`/`SpeakHurry` 在 `BattleDriver.cs:7623/7691`）。⇒ 写手**按符号名找**，⛔ 别按行号。
7. **音效线欠的那一步做完了**：`判据/音效cue到WindowSounds_映射.tsv` —— 原版 **614 条 cue**（610 条 `bundle_soundcollection` + 4 条 `bundle_battleprefabs_vfxandmisc`）逐条给出 **原版 cue 名 / PathID / clip 真名 / 随机区间 / 我们的 key / 我们侧出处 / 原版消费者（类×引用数）/ 状态**。统计：**对上 506 · 对上·走clip代替 5 · 缺·UI 20 · 缺·战场环境 17 · 缺·活动数据 2 · 缺·本地无任何引用 64 · 多余 0**。
8. **最值得先做的一条新发现**：`ButtonClick 1`（pid `-5758964851120979674`）在原版被 **275 个 MonoBehaviour 的 `inspectSound` 字段**引用（`ContainerDrawer×41 / CurrencyDrawer×39 / CampaignPointDrawer×38 / AvatarDrawer×35 …`，275/276 全是 `inspectSound`），**我们 `WindowSounds` 里没有这个 key** ⇒ 所有「点物品看详情」的那一声我们放的是默认 `ButtonClick`（另一个 clip：`UI Select Toggle` vs `UI Click 1`）。

---

## §二 cue → WindowSounds 映射表（方法与结论）

### 2·1 口径（为什么这样做）
- 「key」在我们这边 = **cue 资产名**（`WFSoundBank` 按名字查，名字来自 `bundle_soundcollection` 的 `AudioCue.m_Name`）⇒ 映射表的本质是**原版 614 条 cue 资产 ↔ 我们的表/常量/代码**的逐条对账，不是「随便写个对应关系」。
- **PathID 与 clip 名只能读 bundle**：解包目录的文件名里没有 PathID，`clipList` 存的是 PathID。本轮用 **UnityPy 直读 `soundcollection_assets_all.bundle`**（610 cue / 618 clip，705 条 clip 引用里 703 条解出真名）。
- **GUID 映射补上了**：`bundle_soundcollection` 的 `m_Container` 613 个键**就是 32 位 GUID**（610 条对上 cue）⇒ 我把 610 个 GUID 拿到 84 个 bundle 全量 grep，补出「只被 `AssetReference` 引用、没有 PPtr 引用」的那一档（29 条 cue 有 GUID 引用，其中 2 条在 106 条缺 key 的集合里）。

### 2·2 状态统计（614 条，逐条见 TSV）
| 状态 | 条数 | 含义 / 处理 |
|---|---|---|
| **对上** | **506** | 我们有 key（`animfx_sounds.json` / `battle_sounds.json` 的 `srcCue` / `WindowSounds.<常量>` / 代码按名播） |
| **对上·走clip代替** | **5** | 残骸 6 条 cue 我们**故意不放 cue**、直接放 clip（`RemnantSfx.ClipOf`，判据见其文件头）；不算缺 |
| **缺·UI（20）** | 20 | 原版 UI 组件在引用、我们**没有 key** ⇒ 详见 2·3 |
| **缺·战场环境（17）** | 17 | `ShakeAndSoundObject`（13 场战斗的震屏/环境音宿主）引用 ⇒ 归**战场线**（属特效/环境，不属 WindowSounds） |
| **缺·活动数据（2）** | 2 | `Ork Expansion 1 jingle` / `Space Wolves Relase Jingle` 只被 `ParentEventData.drawData`（GUID）引用 ⇒ 归**活动线** |
| **缺·本地无任何引用（64）** | 64 | **原版资产在本地、但全库既无 PPtr 也无 AssetGUID 引用** ⇒ 见 §五·3 |
| **多余（我们的 key 不在原版）** | **0** | —（注：一开始有 4 个看起来「多余」的 key，实为 `bundle_battleprefabs_vfxandmisc` 里的另一批 cue，已补成 4 行） |

### 2·3 「缺·UI」20 条（这是 WindowSounds 真正的欠账）
| cue | 原版消费者 | 我们要不要做 |
|---|---|---|
| **`ButtonClick 1`** | `inspectSound`（Drawer 一族 275 处） | **要**：加 key + 在「点物品/卡/货币看详情」那一跳播（原版语义 = 查看，不是按钮） |
| **`Booster Open Orks / Tau / Sororitas / SaimHann / BlackLegion / Emperors Children / AstraMilitarum`**（7 条） | `BoosterPackVisualItem.openBoosterPack`（+ `UIEffectTogglerPlaySound`） | **要**：原版共 12 档，我们 `BoosterOpenCue()` 只覆盖 5 档（Standard/Ultramarines/Sautekh/SpaceWolves/Tyranid 复用）⇒ ⚠️ **当前开包窗的 `Army` 只认 4 个阵营，这 7 档今天多半不可达**；先把 cue 名补进表，等开包窗支持那些阵营时直接生效（并补「阵营 → cue」的 prefab 配对判据） |
| **`Ranked Seal SFX`** | `MilestoneStep×18`（段位里程碑） | 要（段位奖励那条链路落点先核） |
| **`UI Raid Claim Reward Ching` / `…Wait`** | `DailyRewardSelector` / `DailyRewardItemContainer` 等 | 要（日常/活动奖励领取） |
| **`Energy Accumulated` / `Energy Accumulated Used`** | `ManaTypeHolder×26`（每场战斗 preload） | 要（HUD 能量累积/消耗；**与 2026-10-25 R15 的「弹跳有闸」是同一处宿主**） |
| **`Draft Pack Appear / Selected Moving / Finish Animation` · `Draft Prebuilt Pack preview`** | `PrebuiltPackUI` | 要（轮抽/预组包 UI） |
| **`Forge Card`** | `CardDisplayLevelUpAnimationController` | 要（锻造升级） |
| **`Deck Editing Valid Deck`** | `DeckEditingWindow` | 要（卡组编辑「卡组合法」提示音） |
| **`Gacha vault open`** | `GachaCratePanel` | 要（若做宝箱） |

### 2·4 三条必须记住的映射陷阱
1. **cue 名 ≠ clip 名**：`startTurn → Start Trun` · `minionInitiatingAttack → SelectUnit` · `Tap Monolith → Hit Monolith` · `NecronsCardReanimate → CardReanimate`。⛔ 别拿 cue 名去 `Resources/` 找文件。
2. **同名不同资产**：`ButtonClick`（clip `UI Select Toggle`）vs `CardClick 1`（**同一个 clip**，只差 pitch 0.8–1.1 随机）vs `ButtonClick 1`（clip `UI Click 1`）⇒ ⛔ 别按 clip 相同就合并 cue。
3. **`m_Name` 有重名**：610 条 cue 里 **2 对同名**（`Stun Effect` ×2、`Will of Gork` ×2，PathID 不同）⇒ 按名字查表时**后写的会静默顶掉先写的**（`WFSoundBank.Ensure` 有撞名警告，但只在**两张表之间**报，同一张表内不报）。

---

## §三 逐条发现（判据订正 + 我们侧实况）

按「影响面」排序，20 条里每条都是**判据级**的发现（不是复述账）。

1. **`SetPopUpDisabled/Enabled` 不是死代码**（账行 131/132）—— 见 §一·2。同时留下一条方法论：**「按文件名找调用点」会拿到假货**（`Missions__BlockAutoUpdates.c` 的内容是 `VoiceLinesController__SetPopUpDisabled`）。
2. **`+0x78` 极性结案**（账行 102/116/131/132/136）—— 见 §一·3。
3. **`PlayMusic` 三档 + Instant 档 volume=1.0**（账行 66）—— `MusicTransitionType = {FadeIn=0, Blend=1, Instant=2}`，非法值抛 `ArgumentOutOfRangeException`。原版 Instant 档 **`volume = maxVolume = 1.0`**；我们的 `MusicVolumeMultiplier=0.2` 与它冲突 ⇒ 见 §一·4。
4. **`PlayMusicQueued` 的「提前 3 s」是硬数字**（账行 63/67）：`delay = clip.length - source.time - 3.0f`，下限 0、**上限 = `float.MaxValue`（等于不夹）**。两个常数本轮用 `工具/read_literal.py` 读出来了：`0x1834b2e8c = 3.0f`、`0x1834b2d94 = float.MaxValue`。
5. **`AudioSourcesController` 的三个常量**（账行 19-31 全部依赖）：`AudioSourcesController__.cctor.c:9-11` = `[maxVolume=1.0f, minVolume=0.0f, LERP_DURATION=3.0f]` ⇒ 所有「3 s 淡变」的数字都有出处，⛔ 不用再猜。
6. **`MusicManager.CheckQueue` 的形参 `sceneTarget` 在方法体里一次都没用**（账行 62）—— 语义只能从「场景切换入口调它」推出来。我们实现时**不要留这个形参**。
7. **`MusicVolumePlayerPrefsSetting.Start` 的默认值是「源自身音量」**（账行 69）：`PlayerPrefs.GetFloat(key, source.volume)` ⇒ 初装 = `0.65 × 0.65 = 0.4225`。我们的过场视频是裸 0.65。
8. **`SoundManager.ToggleSFX` 的第三个实参被反编译器吞了**（账行 80）：ILSpy `SoundManager.cs:601-602` 保留算法 `timeToReach = ((instant?1:0)^1)*2` ⇒ `instant ⇒ 0f`、否则 **2f**。Cpp2IL 的 `.c` 只有两个参数。
9. **`VoiceLineBox.ShowChat` 是「两个协程并发」**（账行 93/97）：`_ShowChat_d__19_MoveNext.c:77-82` 同时起 `AnimateText` 与 `FinishChat(duration)`；并且在起播前有一个 `if (+0x44 > 0) WaitForSeconds(0.1f)`（常数 `0x1834b2dc4 = 0.1f` 已读）。⇒ 我们「打字打完才扣寿命」每句多停 `0.01×字数` 秒。⚠️ `+0x44` 的语义**没查清**（已在施工单里标「条件未定」，⛔ 别写成「原版总是等 0.1 s」）。
10. **`HideChat` / `HideChatBox` 是「停音 + 关画面 + 清字」一步做完**（账行 85/94）：`VoiceLineBox__HideChat.c:9-22`、`UnitsVoiceLinesPanel__HideChatBox.c:11-24`。我们的根因不在方法而是**组件挂点**：三颗 `AudioSource` 都挂在面板根（`UnitChatPanel.cs:714`），原版每颗 `VoiceLineBox` 自带 ⇒ 原版 `SetActive(false)` 顺带停音。
11. **`VoiceLinesController.CanUseVOs` 的 matchType 旁路是 100/140**（账行 103）—— 与账里写的一致；两个远端开关在 `RemoteConfig +0x10/+0x11`，本地不可得 ⇒ 离线等价 = 都当真（要标 🔶）。
12. **`DisplayWarlordChatMessage` 的发送闸是 `id != 0 && id != 1`**（账行 114）：`VoiceLinesController__DisplayWarlordChatMessage.c:65`。我们的 `NetChat.IsButtonLine`（5…10）**比原版窄** ⇒ 认输（`ChatMessage.Concede = 4`）发不出去。⚠️ 顺手订正：`NetChat.cs:114` 注释写「4 = HurryUp」，**枚举实读 4 = Concede**（`ChatMessage.cs`）。
13. **`DisplayWarlordChatMessage` 的 `0x37` 记录闸是 `id != 0 && matchType != 0xA0 && id != 4`**（账行 114）—— 我们 `NetChat.ShouldRecord` 三条都对，只是注释里 4 的名字错。
14. **`DisplayWarlordRegularChatMessage` 在 audio 为 null 时仍然 `ShowChatBox`**（账行 115/118）：只有 `ShowChat` 里判 `data.audio != null` 才 `Play()` ⇒ 气泡照显、时长走 `Duration(null)=2.0`。我们现在**整句丢**（连文本都不显示）。
15. **`ForcePopUpClose` 有三个调用点**（账行 116）：`StopBattleAfterBattleEnd` / `ClosePopupsAfterBattleEnd` / **`ChoiceOfCardPlayer`**（开选牌面板之前）—— 我们只接了前两个（同一个出口）。
16. **`VoiceLinesPopupSelector.Show` 里真的调 `RefreshChatLines()`**（账行 150/153）：`__Show.c:15`。淡变曲线 `SetEase(9)` = **OutCubic**（`Hide.c:37` / `Show.c:34`）；展开/收起用的是**两个不同时长字段**（`+0x48` vs `+0x4c`），`DOFade` 目标 `+0x1834b2bb8 = 1.0f`（已读）。
17. **`SetupChatOptions` 的两处 matchType 表在原版里就不一致**（账行 152 vs 151）：`Setup` = **3 元素 `{110,120,80}`**、`SetTransformedWarlordNewChats` = **2 元素 `{110,120}`** ⇒ **照抄别统一**。
18. **`SetupChatOptions` 的 `IsAvailable` 只在「钮下标 ≥ 枚举长」时才被调用**（账行 145/146/147）：`SetupChatOptions.c:87` 的 `if (i < enumCount) skip` ⇒ 钮 5…10、枚举 11 ⇒ **三个过滤函数当前永不触发**。⇒ 实现可以写，但必须标「等界外钮出现才生效」+ 夹具断言。
19. **`VoiceLinesPopupSelector.Toggle` 的文件内容不是它自己**（账行 154）：`VoiceLinesPopupSelector__Toggle.c` 里是 `Card2DController__Toggle`（只有一句 `SetActive`）、全库 0 调用点 ⇒ D。
20. **`GetInvokeCue` 的 ambush 分支**（账行 35）—— 见 §一·5。

---

## §四 派活建议（7 包 / 47 条；3 条归档 D）

| 优先 | 包 | 条数 | 一句话 | 独占文件 |
|---|---|---|---|---|
| ① | **A-05** | 10 | `BattleDriver` 的语音/聊天驱动（含 35 判反、131/132 判据订正、108 按颗关） | `BattleDriver.cs` |
| ① | **A-04** | 6 | 语音条（94 停音 + 85 按颗关 + 87/124 isPlaying + 93/97 寿命） | `UnitChatPanel.cs` |
| ① | **A-03** | 10 | 聊天面板（134/150/152/153 同一处、144/153 曲线、116 选牌前收） | `ChatPopupPanel.cs` |
| ② | **A-01** | 16 | 双源控制器 + 音乐三档 + 68 停菜单 + 69 过场音量 | `ShellRuntime.cs` |
| ② | **A-07** | 2 | `UIEffectTogglerPlaySound`（复用已有 `PlaySoundOnTime`） | 新文件 |
| ③ | **A-06** | 1 | 联机认输台词（Net 两件；BattleDriver 三处由 A-05 代做） | `Net/NetChat.cs`、`Net/NetBattle.cs` |
| ④ | **A-02** | 2 | mixer 4 快照 + 3 组（**只能主对话跑 Unity**） | `Main Mixer.mixer` 等 |
| — | **A-08** | 3 | 143 / 149 / 154 归档 D（原版 0 调用点） | — |

**顺序理由**：A-04 先出 `HideBox`/`isPlaying`（A-05 的 108/102 依赖它）；A-03 与 A-05 的 133/151 是**同一逻辑的两半**（前者选择器层、后者控制器层转发）⇒ 两包开工前**先对齐签名**；A-01 里 66 那条要先等调度台裁「音乐响度」。

---

## §五 没查清的 / 覆盖边界 / 证据

### 5·1 明确「没查清」（⛔ 不许当已解）
1. `VoiceLineBox` 的 **`+0x44`** 字段语义（决定「起播前 0.1 s 等一拍」的条件是否恒真）。
2. `VoiceLinesController` 的 `+0x20 / +0x28` 两个回调槽（`Hide/Show` 末尾调的那个委托）对应原版哪两个事件名 —— 只按调用形态推定。
3. `AudioSourcesController` 的 `_a/_b` 两个源在 prefab 里的实际挂载与 `Init` 的入参（`AudioSourcesController__Init.c` 未读）。
4. 原版 C# 里是否**按名字**播 cue（字符串调用）—— 决定 §五·3 的 64 条是不是真死资产。
5. `Sororitas Shrine Bombardment Audio` 的 4 条 clip、`Buff Black Legion 3` 的 1 条 clip：跨文件 PPtr，本轮未解出 clip 名。

### 5·2 覆盖边界（治具口径）
- **本批读的**：50 条的原版方法体（`.c` 优先，必要时用 `out_cpp2il_dev_ilspy` 交叉）+ 我们的落点源码 + `Main Mixer.mixer` + 610 条 cue 的 bundle 字段 + 84 个 bundle 的引用扫描。
- **本批没读/没验**：编译、运行时手感、听感、Unity 资产改动（mixer）。⇒ 「判据齐」**不等于**「做对了」。
- **反编译陷阱本轮实遇 2 处**：① `SetTransformedWarlordNewChats` 三个实参全被吞（`.c:6` 成空调用）⇒ 必须读选择器层那份；② `SoundManager.ToggleSFX` 第三实参被吞 ⇒ 必须用 ILSpy 版的算法补。另有 1 处**文件名与内容不符**（`VoiceLinesPopupSelector__Toggle.c`）与 **1 对符号串位**（`Missions__BlockAutoUpdates` / `EnableAutoUpdates`）。
- **数字全部现读**：本文所有条数/行号都是 2026-10-26 快照；⛔ 别抄进别的文档当常量。

### 5·3 「64 条本地无任何引用」的证据与保留意见
- **做法**：610 条 cue 的 PathID → `grep -rlF -f allpids.txt --include=*.json d:/2/新解包资源/assets_full`（约 20 s，命中 1994 个文件）；610 条 cue 的 **AssetGUID**（bundle 容器键）→ 同样全量 grep（命中 230 个文件）。**同时排掉 `*/AssetBundle/*.json`**（那是包内清单，会把每条 cue 都算一次引用）。
- **结论**：64 条 cue 在本地**两种引用都为 0**（清单见映射表 `状态 = 缺·本地无任何引用`，含 `Trait Blast / Trait Rally / Trait Stealth …` 全族、`Bolter_4shot / Gauss Attack * / Plasma Explosion` 全族、`Mulligan Replace`、`End Turn Button Move`、`Match Found`、`UI Confirm 1..3`、`ButtonClick 2..5`、`CardClick 3` …）。
- **保留意见（⛔ 别直接判「原版没用」）**：这些名字里有一整族**明显应该有用**（Trait 音、武器音、Mulligan、Match Found）⇒ 更可能是 **① 原版 C# 按名字播**、或 **② 这批资产属于更早的 build 残留**。**定案需要**：全量反编译里的字符串扫描（`decomp_full/*.c` 找 cue 名字面量）—— 本轮**没做**（属「已知未覆盖面」，见派活包 §4 P3-4）。

### 5·4 关键证据坐标（便于复查）
| 结论 | 证据 |
|---|---|
| `+0x78` 极性 | `VoiceLinesController__PopUpShowed.c:13`（=0）· `__PopUpHid.c:13`（=1）· `__CanChat.c:27` · `__ForcePopUpClose.c:5` · `__ClickChat.c:9` |
| SetPopUp* 调用点 | `BattleManager__OnInitializationComplete.c:20` · `BattleManager._ShowHeroesIntroMessageCampaign_d__339_MoveNext.c:41,48` |
| AudioSourcesController 常量 | `AudioSourcesController__.cctor.c:9-11` |
| 提前 3 s | `MusicManager__PlayMusicQueued.c:43-52` + `read_literal 0x1834b2e8c = 3.0f` |
| Instant 档 volume=1.0 | `MusicManager__PlayMusic.c:71-78` |
| ToggleSFX timeToReach | `cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:601-602` |
| ShowChat 并发 + 0.1 s | `VoiceLineBox._ShowChat_d__19_MoveNext.c:60-67,77-82` + `read_literal 0x1834b2dc4 = 0.1f` |
| 发送闸 id != 0/1 | `VoiceLinesController__DisplayWarlordChatMessage.c:65` |
| ChatMessage 枚举（4=Concede） | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/ChatMessage.cs` |
| ambush ⇒ rarity 1 | `CardScript__CardToMinionAnim.c:20-26` + `BattleCardSounds__GetInvokeCue.c:5-8` |
| ButtonClick 1 = inspectSound×275 | 本轮扫描：276 个文件命中，字段 `inspectSound`×275 |
| mixer 现状 | `Resources/Audio/Main Mixer.mixer`：组 `FX/Master/Voices/Music/Jingles`、快照只有默认 `Snapshot` |
