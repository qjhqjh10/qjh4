# B9 · 音频/语音线 剩余 C 50 条 · 派活包（按目标文件切片）

> 只读判据代理 `J5AU`（第八十四会话「完全复刻」）· 2026-10-26 · **只读核**（未改 `.cs` / 资产 / 主账表 / 正本；未跑 Unity；未动 git）
> 配套：逐条施工单 = `判据/B9_音频C_施工单.tsv`（12 列 × 50 行）· cue 清单 = `判据/音效cue到WindowSounds_映射.tsv`（12 列 × 614 行）· 判读 = `报告/B9_J5AU_音频C施工单.md`
> 🔴 **稳定性**：下面的「账行号」是**快照**（主表正被多个代理并行改）。**写手一律先按「原版类 + 方法名」在 `账/音频_语音.tsv` 里定位，再对照本包的行号。**

---

## §0 用法（四句话）

1. **一个包 = 一个目标文件（或一组同族小文件）= 一个写手**。包内顺序：先 P1、再 P2、最后 D（D 只归档不动手）。
2. **`BattleDriver.cs` / `ShellRuntime.cs` / `ChatPopupPanel.cs` / `UnitChatPanel.cs` 四个文件同一时刻各只能有一个写手**（见 §3）。
3. 每包做完先跑 **`bash 工具/typecheck.sh`**（秒级，几秒），再按「本包验收口」跑对应宿主；⛔ 写手**不跑全量 Unity 自检**（主对话在同步点统一跑）。
4. **账里「我们的落点」的行号大面积过期**（见 §3·5）—— 按**符号名**找，⛔ 别按行号找。

---

## §1 包总表

| 包名 | 文件（写手独占） | 条数 | 独占 | 前置依赖 | 账行号（快照） | 本包验收口 |
|---|---|---|---|---|---|---|
| **A-01** | `Core/AudioSourcesController.cs`（新建）+ `Shell/ShellRuntime.cs` | **16** | ✅ 独占 `ShellRuntime.cs` | 无（新建件先行） | 19·20·21·22·23·27·28·29·30·31·62·63·66·67·68·69 | 三点音量断言（0/1.5/3 s）+ 真机听菜单音乐（含 66 的 5 倍响度裁定） |
| **A-02** | `Resources/Audio/Main Mixer.mixer` + `Core/WarpforgeAudio.cs` + `Editor/AudioSetup.cs` + `Shell/SettingsWindow.cs` | **2** | ✅ 独占 mixer 资产 | 无，但 **必须主对话跑 Unity** | 12·80 | `FindSnapshot` 命中 4 个快照名 + `SettingsScene.Run` |
| **A-03** | `Battle/ChatPopupPanel.cs` | **10** | ✅ 独占 | A-04 的 `UnitChatPanel` 不需；116 与 `BattleDriver` 的选牌入口**同一包内自己做**（只改本文件 + 一处调用） | 116·134·144·145·146·147·150·151·152·153 | 6 颗钮文字按督军切换 + OutCubic 三点 alpha + 3 元素表 |
| **A-04** | `Battle/UnitChatPanel.cs` | **6** | ✅ 独占 | 无（先出 `HideBox`/`isPlaying` 供 A-05 用） | 85·87·93·94·97·124 | 按颗关 / 停音 / 寿命 3.3 s / AudioSource 挂到每颗 |
| **A-05** | `Battle/BattleDriver.cs` | **10** | ✅ **独占（第二瓶颈）** | A-04（HideBox）、A-03（Visible）、A-06（IsRelayable 已有） | 35·102·103·108·115·118·131·132·133·136 | `ruleprobe play check` 不变 + 真机开局/教程/聊天三条链 |
| **A-06** | `Net/NetChat.cs` + `Net/NetBattle.cs`（＋把 3 处 `BattleDriver` 调用**写成给 A-05 的施工单条目**，不自己改） | **1** | ✅ 独占 Net 两件 | **A-05** 的 `BattleDriver` 改动（认输那句要调 `OnLocalChat(4)`） | 114 | `NetSelfTest.Run` + `NetBattleTest.Run` |
| **A-07** | `Effects/UIEffectTogglerPlaySound.cs`（新建） | **2** | ✅ 独占新文件 | 无（复用 `AnimFXController.PlaySoundOnTime`，⛔ 不改那个文件） | 82·83 | 两个槽 `numberOfTimesPlayed` 归零 / 递增 |
| **A-08** | —（**不派活**） | **3** | — | — | 143·149·154 | 原版 0 调用点 / 文件名与内容不符 ⇒ 归档 D |

**合计**：7 个可派包 + 1 个不派活包 = 50 条（其中 3 条不派活 ⇒ **实际派活 47 条**）。

---

## §2 各包明细

### A-01 `ShellRuntime.cs` + 新建 `Core/AudioSourcesController.cs` · 16 条 · ✅ 独占
- **序**：① 先建 `Core/AudioSourcesController.cs`（10 条方法一次写完：`Blend/DecrementVolume/FadeIn/FadeOut/IncrementVolume/SetMaxVolume/SetMinVolume/Stop/SwitchMainAudioSource/VolumeLerp`，常量 `[max=1.0, min=0.0, LERP=3.0]` 写在类头注释里并标出处 `AudioSourcesController__.cctor.c:9-11`）→ ② `ShellRuntime` 的两个源改由它持有（`_musicB` 从死字段变成真用）→ ③ 62/67/63 的排队换协程（句柄字段 = 原版 `+0x40`）→ ④ 66 的三档过渡 → ⑤ **68（P1）进战斗停菜单音乐** → ⑥ 69 过场视频音量乘存档。
- **验收口**：三条断言（FadeIn/FadeOut 三点、Blend 换源、句柄可取消）+ **真机**「壳 → 战斗」不再叠放两条 BGM。
- **风险**：66 会改**全部音乐响度**（原版 Instant 档强制 `source.volume=1.0`，我们把序列化的 0.2 当系数）⇒ **必须先由调度台裁**：按原版 1.0 改，或保留 0.2 并在账里标 `🔶 自己设计`。裁完要同步改 `Editor/ShellScene.cs:4732` 那条断言。
- **⛔ 别做**：别在 `LateUpdate` 里继续轮询（原版是协程 + 提前 3 s）；别改 `WarpforgeAudio.Write` 的 mixer 写法。

### A-02 mixer 快照 · 2 条 · ✅ 独占资产
- **序**：① 给 `Main Mixer.mixer` 加 4 个快照（`SFX On` / `SFX Off` / 战斗 BGM 四档至少先出 `Music High` / `Music Low`）+ 3 个组（`BattleGroup Low/Medium/High`）→ ② `WarpforgeAudio.GetSnapshot(kind)` → ③ `ToggleSfx(on, instant)`（`instant ? 0f : 2f`，判据 = ILSpy `SoundManager.cs:601-602`）。
- **⚠️ 阻塞**：改 `.mixer` 必须跑编辑器脚本（`Editor/AudioSetup.cs` / `工具/setup_audio_mixer.py`）⇒ **只有主对话能跑 Unity**。写手可以先把 `.cs` 侧写好（取不到快照就**出声**），等主对话补资产后再验收。
- **验收口**：`AudioMixer.FindSnapshot("SFX Off")` 非 null；`SettingsScene.Run` 里切 SFX 开关不报缺快照。

### A-03 `ChatPopupPanel.cs` · 10 条 · ✅ 独占（P1 密集）
- **序**：① **134 / 150 / 152 / 153 同根，一次改完**（`RefreshChatLines()`：按本局我方督军取 `LineTermFor(warlordId,i)` 写 6 颗钮文本；`Build` 后调一次、`Show` 里再调一次；`Battle/Chat/*` 退成兜底）→ ② 144 / 153 的淡变曲线换 OutCubic（`1-(1-t)^3`）→ ③ 116 `ChoiceOfCardPlayer` 之前 `HideImmediate()` → ④ 145 / 146 / 147 越界与 matchType 过滤（**原版当前不触发**，只能夹具验）→ ⑤ 151 变身督军（与 A-05 的 133 同一逻辑，**两包各写一半前先口头对齐签名**）。
- **验收口**：6 颗钮文字逐字 = 官方表 `-Chat5..10`（两个不同督军各一次）；alpha 0/0.25/0.5 s ≈ 0/0.875/1.0；`i=11` ⇒ 返空串。
- **风险**：`P1` 用户可见；⚠️ 数据现成（官方表 **56 张卡**带齐 Chat5…10）⇒ **先跑一遍 56/56 命中**再动代码；⛔ 别把 `-ChatN` 拼法再写一份（只在 `UnitChatPanel.ChatTermKey` 一处）。

### A-04 `UnitChatPanel.cs` · 6 条 · ✅ 独占
- **序**：① 94（P1）把 `AudioSource` 挂到**每颗气泡自己的** GameObject（原版同构：`SetActive(false)` 顺带停音）+ `HideBubble` 补 `Stop()` 与 `maxVisibleCharacters=0` → ② 85（P1）新增 `HideBox(int side)`（按序号关一颗，五步照 `UnitsVoiceLinesPanel__HideChatBox.c:9-25`）→ ③ 87/124 合成一处：`Bubble.isPlaying` 取代 `audio.isPlaying`（口径 = `max(2, len+0.3)`）→ ④ 93/97 寿命用并发口径（打字不挡寿命）+ 起播前 0.1 s 等一拍。
- **验收口**：`HideBox(1)` 只关敌方；`HideBubble` 后 `audio.isPlaying == false`；150 字 + 3 s 语音 ⇒ 总寿命 ≈ 3.3 s；clip=null 气泡 `IsSpeaking` 仍 true 2 s。
- **风险**：改 AudioSource 挂点会影响 `VoicesGroup` 路由与既有读数口（`LastClipLoaded` 等）；85/94/108 同根 ⇒ **A-04 先落地、A-05 再调 `HideBox`**。

### A-05 `BattleDriver.cs` · 10 条 · ✅ 独占（第二瓶颈）
- **序**：① 35（判反了，先改）→ ② 131/132 + 102 + 136（`+0x78` 那一族，一次改完：`CanAiChat` 补「弹窗开着不许说」、开场 Disable/Enable）→ ③ 108（`HideAll` → `HideBox(BubbleSideOf(seat))`，调 A-04 的新口）→ ④ 115 / 118（audio 可空 + `CanUseVOs`）→ ⑤ 133（转发给 `ChatPopupPanel.SetTransformedWarlordNewChats`，与 A-03 的 151 对齐）→ ⑥ **114 的 3 处调用**（`NetRemoteResign` 补 `SpeakConcede`、认输走 `OnLocalChat(4)`）——这三个点**由 A-05 改**，A-06 只改 Net 两个文件。
- **验收口**：`bash 工具/ruleprobe.sh play check` 指纹**必须不变**（本包全是表现层）；真机走「开局 → 教程跳过 → 点聊天钮 → 认输」四条。
- **风险**：该文件 34,393 行、被多条线争用 ⇒ **先在本包内 `git status` 确认没别人在改**（调度台派活时点名）；🔴 `+0x78` 的极性**已经定案**（`PopUpShowed=0 / PopUpHid=1`），⛔ 别再按 `SetPopUpDisabled/Enabled` 的方法名猜。

### A-06 `Net/NetChat.cs` + `Net/NetBattle.cs` · 1 条 · ✅ 独占 Net 两件
- **序**：① `ShouldRecord` 的注释「4 = HurryUp」→ **4 = Concede**（`ChatMessage.cs` 枚举实读：0 Intro · 1 MirrorMatch · 2 Bored · 3 ICantDoThat · 4 Concede · 5…10 Greet/Threat/WellPlayed/Taunt/Sorry/Oops）；② 发送闸 `IsButtonLine` → `IsRelayable`（`id != 0 && id != 1`，判据 `DisplayWarlordChatMessage.c:65`）；③ 收侧**保留** `IsButtonLine`（防伪造越界）。
- **验收口**：`NetSelfTest.Run` + `NetBattleTest.Run` 全绿；伪造 `chatId=2` 仍被丢并出声；单机局行为逐字不变。
- **依赖**：③ 的认输那一句要 `BattleDriver` 侧调 `OnLocalChat(4)` ⇒ 由 A-05 做（见 A-05 第 ⑥ 步）。

### A-07 `Effects/UIEffectTogglerPlaySound.cs`（新建）· 2 条 · ✅ 独占
- **序**：① 新建类（两个 `PlaySoundOnTime` 槽 + `AnimationFinished` 两句 Reset + `DoUpdate` 挑槽）；② **复用** `Battle/AnimFXController.cs:159` 那个已有的 `PlaySoundOnTime` 内部类（⛔ 别写第二份，⛔ 也别把它改成顶层类型——会给 `AnimFXController` 造成行号漂移）。
- **验收口**：`AnimationFinished()` 后两槽 `numberOfTimesPlayed == 0`；`Toggle(true)` 后对应槽计数 > 0。

### A-08 不派活（3 条，归档 D）
| 账行 | 方法 | 为什么不做（铁律 11 例外①） | 复现命令 |
|---|---|---|---|
| 143 | `VoiceLinesPopupSelector.ClosePopupHorizontal()` | 原版全库 0 调用点 | `grep -rl ClosePopupHorizontal d:/2/tools/decomp_full` → 1（自身） |
| 149 | `VoiceLinesPopupSelector.OpenPopupHorizontal()` | 同上 | `grep -rl OpenPopupHorizontal d:/2/tools/decomp_full` → 1（自身） |
| 154 | `VoiceLinesPopupSelector.Toggle(bool)` | 同上；**且文件内容是 `Card2DController__Toggle`**（文件名与内容不符） | `head -3 VoiceLinesPopupSelector__Toggle.c` |

---

## §3 共享瓶颈与「别踩」清单

### 3·1 文件级独占（同一时刻只能一个写手）
| 文件 | 体积/规模 | 谁在用 | 本包占用 |
|---|---|---|---|
| `Battle/BattleDriver.cs` | 34,393 行 | 战斗线上所有包 | **A-05** |
| `Shell/ShellRuntime.cs` | 1,164 行 | 壳/主菜单 | **A-01** |
| `Battle/ChatPopupPanel.cs` | 1,323 行 | 聊天 UI | **A-03** |
| `Battle/UnitChatPanel.cs` | 1,917 行 | 语音条 | **A-04** |
| `Battle/BattleSfx.cs` | 472 行 | 战斗音效（本批**不动**） | — |
| `Shell/WindowSounds.cs` | 222 行 | 壳侧 cue 表（本批**不动**，只读口径） | — |
| `Resources/animfx_sounds.json` | 473 cue | 音效表（**要加 key 时**由 `工具/import_original_sfx.py` 重生，⛔ 别手改） | 见 §4 |
| `Resources/Audio/Main Mixer.mixer` | 资产 | 全部音频 | **A-02（需 Unity）** |

### 3·2 三套播放口，别混
- **战斗音效** `BattleSfx`（FIFO，键 = 原版 `SoundAssetCollection` **字段名**）
- **残骸** `RemnantSfx`（**直接放 clip**，不走 cue 名）
- **壳/UI** `WindowSounds`（键 = **cue 名**，走 `WFSoundBank` + `WFSoundPlayer`/`PlayJingle`）
- **语音** `UnitChatPanel`（`Resources/Art/audio/vo`，走 `Voices` mixer 组）
⇒ 本批 47 条**只碰壳/UI 与语音两条**，⛔ 别顺手改前两条。

### 3·3 「我们挑的」必须标（铁律 18）
本批已知要打的 🔶：① `CanUseVOs` 的两个远端开关默认 true（远端不可得）；② `+0x44` 未查清时的「有语音等 0.1 s」；③ 若裁 `MusicVolumeMultiplier=0.2` 保留，则 66 那条要标。

### 3·4 cue 名 ↔ clip 名 的三种关系（⛔ 别按名字找文件）
`cue 名 ≠ clip 名`（`startTurn → Start Trun`、`minionInitiatingAttack → SelectUnit`）；一个 cue 有 1~4 条 clip 随机挑；clip 名可能带尾随空格（导入时剥掉）。⇒ **一律查 `Resources/animfx_sounds.json` / `battle_sounds.json` 的 `clips[]`**。

### 3·5 🔴 账里「我们的落点」的行号**已大面积过期**（本批实测）
| 账里写 | 实际（2026-10-26 现读） | 漂移 |
|---|---|---|
| `BattleDriver.cs:20549`（InvokeCueFieldFor） | **:21077** | +528 |
| `BattleDriver.cs:6921`（CanAiChat） | **:6999** | +78 |
| `BattleDriver.cs:6732`（SpeakWarlordLine） | **:6810** | +78 |
| `BattleDriver.cs:17528`（CloseTutorialChatBoxes） | **:18038** | +510 |
| `BattleDriver.cs:9775 / :9846`（StartIntroMonologue / SpeakIntro） | **:10049 / :10120** | +274 |
| `ShellRuntime.cs:1125 / 1106 / 955` | **:1138（LateUpdate）/ :1119（PlayMenuMusic）/ :968（BuildIntro）** | −7 ~ −13 |
| `UnitChatPanel.cs:7548/7613/7616`（SpeakCantDo/Hurry 的引用） | 那两个方法在 **`BattleDriver.cs:7623 / :7691`**（**文件也错了**） | — |
| `BattleSfx.cs:174`（CanPlay 等价） | **:304-315**（节流那两段） | — |

---

## §4 P3 候选（远端音源 / 未下发资源）—— **只给证据 + 时间限定，⛔ 不判 D**

| # | 候选 | 证据（可复现） | 时间限定 | 需要什么才能定案 |
|---|---|---|---|---|
| P3-1 | **`SoundAssetCollection` 6 个悬挂 GUID 字段**：`abilityBoxClick` · `activateUnstunnable` · `activateSurvivor` · `activateTerror` · `activateSentinel` · `activateAutoAttack` | 6 个 GUID（如 `2eee736725bf3b64e93dbb212de0a8c4`）在 `catalog.bin` / `catalog_main.json` / `aa` 全目录 1.6 GB 裸搜 **0 命中** | 现读 = 2026-10-16（`T06`），2026-10-26 复核未变 | 远端 catalog（关服后不可得）⇒ 或等原版 live 数据 |
| P3-2 | 6 个**空引用**字段（原版自己就没声音）：`abilityBoxEnter` · `abilityBoxExit` · `syphon` · `activateBlast` · `activateSacrifice` · `dissolveCardInHand` | 亲读 shipped bundle 的 `SoundAssetCollection`，六格 `AssetReference` 为空 | 同上 | **不需要**——已可判「原版本身没有」（铁律 11 例外①） |
| P3-3 | `changeToActiveAbilityCUE`（`CardScript` 那格） | `m_FileID:0, m_PathID:0` 空 PPtr，但调用点存在（`CardScript__ChangeAttackType.c:53`） | 2026-10-16 | 同上（原版本身不响） |
| P3-4 | **64 条 cue 本地有资产、但全库无任何 PPtr 与 AssetGUID 引用** | 本轮亲测：610 条 cue 的 PPtr 引用扫 84 个 bundle（`grep -rlF` 610 个 pid）+ GUID 引用扫（610 个容器键），**64 条两种引用都 0 命中**（清单见映射表 `状态 = 缺·本地无任何引用`） | **2026-10-26 本轮** | ① 原版 C# 里是否**按名字**播放（`SoundManager.Play2D("Trait Blast")` 这类字符串调用）—— 需要全量反编译的字符串扫描；② 远端 liveops 配置是否下发 cue 名 |
| P3-5 | `Ork Expansion 1 jingle` / `Space Wolves Relase Jingle` | 只被 `ParentEventData.drawData`（**AssetGUID**）引用 ⇒ 活动抽卡数据 | 2026-10-26 | **属活动线**（不是音效线）：数据在本地，缺的是活动窗口的播放点 |
| P3-6 | `UnityRemoteConfigData +0x10 / +0x11`（EnableTroopVOs / EnableWarlordVOs） | 两个 bool 在远端配置里，本地无 `UnityRemoteConfigData` 资产（`GetRemoteConfigData()` 返回 null ⇒ 原版自己也会走空引用桩） | 2026-10-26 | 远端配置 ⇒ 离线等价 = 都当真（见 A-05 的 103） |
| P3-7 | `Sororitas Shrine Bombardment Audio` 的 4 条 clip、`Buff Black Legion 3` 的 1 条 clip | cue 在 `bundle_battleprefabs_vfxandmisc`，`clipList` 指向**别的 bundle** 的 pid ⇒ 本轮未解出 clip 名 | 2026-10-26 | 跨文件 PPtr（`m_FileID` 解 externals）——**不影响「我们有这个 key」**，只影响「文件在不在 `Resources/`」 |

> 🔴 **P3 纪律**：以上 7 条**都不是**「可以不做」的理由；P3-1/4/6 是**证据不足**（不是判据为空），一旦拿到远端数据要回来补。

---

## §5 账的覆盖边界（每份交接必带，用户 2026-10-25 要求）

- **本批覆盖**：`账/音频_语音.tsv` 的 **50 条 `状态=C`** 行（44 条 P2 + 6 条 P1），逐条给了「原版逐条步骤 / 我们缺什么 / 施工步骤 / 怎么验 / 目标文件 / 风险 / 建议等级」。
- **本批新增的独立产物**：`判据/音效cue到WindowSounds_映射.tsv` —— 首次把 **614 条原版 cue**（610 条 `bundle_soundcollection` + 4 条 `bundle_battleprefabs_vfxandmisc`）与我们的 key 逐条对上：**对上 506 · 对上·走clip代替 5 · 缺·UI 20 · 缺·战场环境 17 · 缺·活动数据 2 · 缺·本地无任何引用 64 · 多余 0**。
- **「读过」≠「复刻过」**：本批只读「原版方法体 + 序列化字段 + 我们的落点」；**编译不过 / 运行时手感 / 听感**一件都**没验**（代理不许跑 Unity）。
- **反编译口径**：第一权威 `d:/2/tools/decomp_full/`；被 `cpp2il` 吞掉实参 / 吞掉第三参数的地方（`SetTransformedWarlordNewChats` 三个实参全丢、`SoundManager.ToggleSFX` 的 `timeToReach`）**已用 `out_cpp2il_dev_ilspy` 与 `工具/read_literal.py` 交叉补齐**，逐条标在施工单里。
- **已知未覆盖**（本批相关）：① `VoiceLineBox` 的 `+0x44` 字段语义**没查清**；② `VoiceLinesController` 的 `+0x20`/`+0x28`/`+0x40` 等回调槽语义只按调用形态推定；③ 语音台词的**中文/英文表**只核了键名，没核 56 张卡的逐句文本；④ 原版 C# 里是否存在**按名字播 cue 的字符串调用**（决定 P3-4 的 64 条是不是真死资产）**没查**。
- **数字一律现读**：本文件所有条数 / 行号 / 状态计数都是 2026-10-26 快照，⛔ 别当常量抄进别的文档。
