# B120_F · 音效 / 语音 / BGM 参数审计

> 执行代理 `B120_F` · 2026-11-02 · **只读**（未改任何 `.cs` / 账 / 正本，未跑 Unity）。
> 判据优先级：`decomp_full/*.c` → `out_cpp2il_dev_ilspy/*` → 签名桩 → 常量字节 → 解包资产 → 既有判据/账（现读复核）。

## 〇、一句话

**音量/混音器/BGM 五拍/cue 随机化包装这几族已经逐值对得上；真正的偏差集中在「三颗自建/自管 `AudioSource` 的参数」与「战斗音效被我们多加了一层队列」**：
单位语音（attack/death 那两族 VO）我们走 **Voices 滑块**、原版走 **音效滑块 + 无混音组**；FX 池 **24 vs 20**、
3D 最近距离 **1 vs 15.61**；残骸音效 **Linear/1/40 vs Logarithmic/15.61/500**；`BattleManager.PlayNextSoundInQueue`
那条 FIFO **只服务旧 `SoundAsset_OLD` / `AudioClip` 路**，而我们把它套到了整族 cue 上。

---

## 一、逐参数对照表

| # | 参数 | 原版值 | 原版出处 | 我们值 | 我们出处 | 判定 | 备注 / 影响 |
|---|---|---|---|---|---|---|---|
| 1 | 线性音量 → dB | `v == 0 ? −80 : log10(v) × 20` | `SoundManager__SetMixerVolume.c`（`FUN_18048a970` = `Log10`，`DAT_1834b2…` 20.0） | 同式（`v <= 0` 走 −80） | `Core/WarpforgeAudio.cs:128-132`（`Write`） | ✅一致 | `Clamp01` 之后 `<= 0` 与 `== 0` 等价 |
| 2 | 四通道取值 | Music ← 音乐量 × **0.2**；Voices ← 语音量；FX ← 音效量；Jingles ← **与 FX 同值** | `MusicManager.musicVolumeMultiplier`（`bundle_staticgeneralassets_assets_all/MonoBehaviour/MonoBehaviour_-1722128571091716916.json`）＋ `_SetMusicAndFxVolumes` | 同值，0.2 为常量 | `WarpforgeAudio.cs:28 / 69-72` | ✅一致 | 0.2 只乘在 mixer 那一格 |
| 3 | 落盘范围 | **只有音乐**（key `MusicVolume`）；FX / Voices 不落盘 | `GameStaticData.MUSIC_PLAYER_PREFS_VOLUME` | 同 | `WarpforgeAudio.cs:26 / 52 / 183` | ✅一致 | |
| 4 | 混音器结构 | 3 份 mixer：主（Master/FX/Music/Voices/Jingles）＋ Music 子（General + BattleGroup Low/Medium/High）＋ FX 子（General/NoVolume）；**8 条快照** | `bundle_audiocontrol_assets_all/AudioMixerGroupController/*` 本轮现读：`Voices=5814470231619483780`(主) · `BattleGroupLow=2233882122052686825` · `BattleGroupMedium=-4077555794175536151` · `BattleGroupHigh=-6809311551296449559` · `General=-6837651415228254606`(Music) · `Jingles=-3363046660692756348` · `FX=-3369427022867580796` | 三份 mixer 已建，快照名逐条照抄 | `WarpforgeAudio.cs:257-278 / 330-380`；`Resources/Audio/{Main Mixer,Music,FX}.mixer` | ✅一致 | 已由 `B109_E17_Mixer_Assert` 机检，本片未重复 |
| 5 | `ToggleSFX` 过渡时长 | `((instant?1:0)^1) × 2` ⇒ instant ⇒ **0 s**，否则 **2 s** | `SoundManager__ToggleSFX.c` / ILSpy `SoundManager.cs:597-604` | `instant ? 0f : 2f` | `WarpforgeAudio.cs:309-314` | ✅一致 | |
| 6 | 快照枚举 | `FxNormal=0 · FxNoVolume=1 · MusicBattleMusicLow=2 · …NoMusic=5` | `dump.cs`（`WarpforgeAudio.cs:242-244` 已记） | 逐值照抄 | `WarpforgeAudio.cs:258-278` | ✅一致 | |
| 7 | cue 资产的随机化包装 | 每 cue 一条 **clipList**（1..9 条）+ `Random.Range(0,count)` 挑 clip + `Random.Range(minPitch,maxPitch)` / `Random.Range(minVolume,maxVolume)` | `AudioCue__ConfigAudioSource.c:35-49` | `rng.Next(clips.Length)` + `Lerp(minPitch,maxPitch,t)` / `Lerp(minVolume,maxVolume,t)` | `WarpforgeVFX/Runtime/WFSoundPlayer.cs:107-111`；`WFSoundBank.PickClip`（`:154-164`） | ✅一致 | 我们多了「坏 clip 跳过」的健壮性，分布不变 |
| 8 | `AudioCue` 默认区间 | `minPitch=maxPitch=minVolume=maxVolume = 0x3f800000`(1.0) · `timeToPlayAgain = 0x3dcccccd`(**0.1**) | `AudioCue__.ctor.c`（逐字节） | `minPitch/maxPitch/minVolume/maxVolume = 1`，**`timeToPlayAgain = 0`** | `WarpforgeVFX/Runtime/WFSoundBank.cs:26-28` | ⚠️已知待修 | 数据面已带真值 ⇒ 当前无可听差异；但「表里缺这一格时」我们会**永不冷却**、原版是 0.1 s。建议默认改 0.1 |
| 9 | cue 覆盖 | `bundle_soundcollection_assets_all/MonoBehaviour` 里 **610** 个 AudioCue 资产 | 本轮现数（`grep -l minPitch` = 610） | `animfx_sounds.json` **513** 条（覆盖 508 个资产名）+ `battle_sounds.json` **27** 条（键 = 原版**字段名**）+ 残骸 6 条走 `remnant_cue_props.json` | `Resources/animfx_sounds.json` · `battle_sounds.json` · `WarpforgeVFX/Runtime/WFSoundBank.cs:43-50` | ⚠️已知待修 | **102 个 cue 资产没有进任何表**（`Aeldari To Waystone` 等 6 条由残骸那条路覆盖 ⇒ 实际悬空 ≈96）。**谁在调用未分类**（见 §四） |
| 10 | 单位语音（`attack`/`death` 那两族 VO）的**音量与路由** | `PlayOneShot(clip, GameStaticData.SoundFxVolume)` ⇒ **音效滑块**；源 = `BattleManager.audioSource`，场景实例 `AudioSource_1595`：`OutputAudioMixerGroup = {m_FileID:0, m_PathID:0}` ⇒ **无混音组（走 Master）**；闸 = `SoundFxVolume > 0`（**=0 时完全不播**） | `decomp_full/BattleManager__PlaySoundClip.c:28-35` ＋ ILSpy `BattleManager.cs:41160-41166`（`GameStaticData.SoundFxVolume__BackingField > 0f`）＋ `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4371.json`（= `BattleManager`，`audioSource → 1595`）＋ `.../AudioSource/AudioSource_1595.json`（`OutputAudioMixerGroup` 全 0 · `Priority 154`） | `PlayOneShot(clip, AudioListener.volume)`，源挂 **`Voices`** 组 | `Battle/BattleDriver.cs:7657`（`outputAudioMixerGroup = VoicesGroup`）· `:7685`（`PlayOneShot(clip, AudioListener.volume)`） | ❌不一致 | **听感差**：原版战斗内攻击/阵亡台词跟**音效**滑块走、不受**语音**滑块影响，且音效关到 0 时整族**不播**；我们正好相反。代码注释把那个 getter 判成 `AudioListener.volume`，**与 ILSpy 的 `SoundFxVolume` 闸不符**（同一处 `FUN_180632C40` 既做闸又做音量） |
| 11 | 气泡语音那颗 `AudioSource` | `Volume 1.0` · `Priority 154` · `Spatialize false` · `PlayOnAwake false` · 组 = `Voices`(5814470231619483780) · `clip =` 后 `Play()`（**不是 `PlayOneShot`**） | `AudioSource_1598 / 1600`（`bundle_scenes_scenes_battlearena1/AudioSource/`）＋ `VoiceLineBox__SetUpAudio.c`（`set_clip` → `Play`） | `volume = 1f` · `priority = 154` · `VoicesGroup` · `clip=` + `Play()` | `Battle/UnitChatPanel.cs:760 / 767 / 1124-1129` | ✅一致 | |
| 12 | 气泡寿命 | `AudioClip == null ? 2 : max(2, length + 0.3)`（`MAX_AUDIO_DURATION = 2` 是**下限**不是上限；`DAT_1834b2dc8 = 0.3` / `DAT_1834b2bbc = 2.0`，本轮实读常量字节） | `VoiceLinesController__GetCurrentClipLength.c` + ILSpy `VoiceLinesController.cs:423-441` | `clip == null ? 2.0f : Mathf.Max(2.0f, clip.length + 0.3f)` | `Battle/UnitChatPanel.cs:1200-1203` | ✅一致 | |
| 13 | 聊天钮冷却 | `CHAT_INTERACTABLE_COOLDOWN = 4f`（`DAT_1834b2df0 = 4.0`，本轮实读） | `VoiceLinesController.cs:232` ＋ `_SetChatButtonOnCooldown_d__58__MoveNext.c`（`WaitForSeconds(+0x60)`） | `Cooldown = 4f` | `Battle/ChatPopupPanel.cs:136-138`；消费点 `BattleDriver.cs:6890` | ✅一致 | |
| 14 | VO 可用闸 | `CanUseVOs(i)`：`matchType != 100 && != 140` 时按**远端配置** `+0x10` / `+0x11` 两支开关；否则恒 true | `VoiceLinesController__CanUseVOs.c` | 未实现（本地无远端配置） | — | ⚠️已知待修 | 属 P3（远端配置）；关服后判据在远端 ⇒ 建议按「恒 true」并在账里标 |
| 15 | 语音波形件参数 | `spectrumModifier` / `speedChange` ctor 均 **10.0**（`0x41200000`）；场景实例 `15.0 / 10.0`；`float[64]`；**前 16 bin 求和**；`FFTWindow = 2 = Hamming`；写 `_MovementRange`；**不判 `isPlaying`** | `AudioWaveEqualizer__{ctor,Initialize,Toggle,Update}.c` | 逐条照抄，默认 10/10、场景值 15/10 | `Core/VoiceWaveDriver.cs:58-73 / 130-172` | ✅一致 | |
| 16 | 战斗 BGM 三轨 `AudioSource` | `PlayOnAwake 1` · `Loop 1` · `Volume 1.0` · `Priority 128` · `min 1` · `max 500` · `rolloff 0`(Log) · 组 = BattleGroup Low/Med/High | `AudioSource_1597 / 1599 / 1601`（组 pathID 2233882122052686825 / -6809311551296449559 / -4077555794175536151 本轮实读映射） | `loop = true` · `playOnAwake = false` 后手动 `Play()` + `Pause()` · `volume` 0→1 · 组 = `BattleGroupGroup(i)` | `Battle/BattleMusic.cs:134-143 / 139 / 230` | ✅一致 | `playOnAwake` 由我们手动起，等效（都从 0 秒起、都被 NoMusic 快照压住） |
| 17 | BGM 开场两拍 | 第 0 帧 `NoBattleMusic.TransitionTo(0)` + `ToggleSFX(false, instant:true)` + `yield WaitForEndOfFrame`；第 1 帧 `ToggleSFX(true)`（2 s 淡回）+ `battleLow.TransitionTo(3f)` | `BattleMusicManager._Start_d__13__MoveNext.c:34,37,50,55,60,65,69,85,88`（本轮重读，`DAT_1834b2e8c = 3.0` 实读） | 同两拍 + `OnDestroy` 兜底 | `Battle/BattleMusic.cs:149-155 / 160 / 265` | ✅一致 | 兜底口是我们加的（原版协程不提前销毁） |
| 18 | BGM 四条时长 | 淡入 Low **3** · 切档 **5**(`timeToChange`) · 结算回 Low **5** · 开门静音 **2**(`DAT_1834b2bbc = 2.0`) | `BattleMusicManager__.ctor.c`（`0x50=5.0 · 0x54=5.0`）＋ `DAT_1834b2bbc=2.0` ＋ `DAT_1834b2e8c=3.0`（本轮 `read_literal.py` 实读 4B） | 3 / 5 / 5 / 2 | `Battle/BattleMusic.cs:42-45` | ✅一致 | |
| 19 | BGM 血线与查档 | `h = min(双方督军当前生命)`；`h > midHealthChange` → Low · `> highHealthChange` → Medium · 否则 High；每 `timeToCheck` 秒查一次。**13 场序列化一律 mid=18 / high=10 / change=5 / check=5**，`useMultipleTracks` = 1 的 **7 场** | 本轮逐场现读 13 份 `bundle_scenes_scenes_battlearena*/MonoBehaviour` 的 `BattleMusicManager` JSON（13/13 同上） | 表里 13 行逐值相同；`LevelFor` 同序 | `Resources/battle_bgm.json` ＋ `Battle/BattleMusic.cs:191-196` | ✅一致 | 我们 DTO 把 `midHealthChange` 写成 `float`，原版是 **`int`**（无行为影响） |
| 20 | BGM 省 CPU 的 Pause 判据 | 读**混音组 dB**：`<= −60`（`DAT_1834b3278 = −60` 本轮实读）且在播 ⇒ `Pause()`；`> −60` 且没在播 ⇒ 从兄弟轨取 `timeSamples` 对齐后 `Play()` | `AudioSourceTogglerByVolume__Update.c` + `__GetActiveBrotherTimeSample.c` | 无该组件；用 `_to[]` 记账，**淡变结束时**把不响的那几条 `Pause()`，`StartFade` 时把三条全部 `UnPause()` 并各取一次 `timeSamples` | `Battle/BattleMusic.cs:228-237 / 249-252` | ⚠️已知待修 | 结构性近似：原版按 **−60 dB 穿越时刻**暂停/恢复，我们按**淡变结束**；且 `StartFade` 会把**三条全部解暂停**（原版只解「组音量升起来的那条」）。CPU 与 `timeSamples` 对齐语义等价，听觉上差异极小 |
| 21 | 菜单音乐交叉淡变 | `LERP_DURATION = 0x40400000 = 3.0f`；`PlayMusic` 三档（0 FadeIn / 1 Blend = FadeOut+切源+FadeIn / 2 Instant 强写 1.0）；`VolumeLerp` 每帧按 `Time.time` 线性、夹 [0,1] | `AudioSourcesController__.cctor.c:9-11` ＋ `MusicManager__PlayMusic.c` | 逐条照抄（3 s，起点恒 1/0 的已知怪癖也照抄） | `Core/AudioSourcesController.cs:62-68 / 125-166 / 247-285` | ✅一致 | |
| 22 | **FX 音源池容量** | `amountToPool = 20` | `d:/2/tools/assetripper_out/ExportedProject/Assets/SoundManager.prefab`（`amountToPool: 20`，本轮现读） | `PoolSize = 24` | `WarpforgeVFX/Runtime/WFSoundPlayer.cs:27` | ❌不一致 | 24 是我们挑的数（无判据）。影响：并发上限不同 |
| 23 | **池耗尽策略** | 遍历池取**第一个未激活**的；全在用 ⇒ `CustomDebug.LogError("There are not enough audio sources!")` 并返回 **null ⇒ 这一声不播** | `SoundManager__GetAudioSource.c`（`DAT_1842c30c0` = 那句错误串）＋ ILSpy `SoundManager.cs:392-429` | 轮转：优先挑没在响的，**全在响就盖掉最早那个** | `WFSoundPlayer.cs:57-73` | ❌不一致 | 高并发下原版**丢新声**、我们**掐旧声**；且原版会报错、我们静默 |
| 24 | **3D 衰减模板** | `MinDistance = 15.61` · `MaxDistance = 500` · `rolloffMode = 0`(Logarithmic) · `Priority = 128` · `DopplerLevel = 1` · `Spatialize 0` | `ExportedProject/Assets/Audio Source Template.prefab`（本轮现读） | `AddComponent<AudioSource>()` **不改任何衰减参数** ⇒ Unity 默认 `min 1` / `max 500` / Logarithmic / `priority 128` / doppler 1 | `WFSoundPlayer.cs:62-64`（只设 `playOnAwake`）+ `:113-120`（只设 `spatialBlend` / 位置 / 组 / pitch） | ❌不一致 | **3D 音（889 条 `is2d=0`）的衰减起点差 15.6 倍**：原版 15.61 世界单位内满音量，我们 1 单位外就开始掉 |
| 25 | **残骸音效的 3D 参数** | 残骸三族是 `AudioCue`（`RemnantBody.toRemnantSound/deathSound`、`RemnantAeldari.waystoneCollectSound`、`RemnantNecrons.reanimateSound`）⇒ 走 `Play3D` → `SoundManager` 池 ⇒ **同一份模板**：Logarithmic / 15.61 / 500 | `Warpforge_code/Scripts/Assembly-CSharp/RemnantBody.cs:17,26` ＋ 上一条的模板 | `rolloffMode = Linear` · `minDistance = 1` · `maxDistance = 40` | `Battle/RemnantSfx.cs:174-176` | ❌不一致 | 三处**全都不同**（曲线 + 起点 + 终点）。`max 40` 会让远处残骸声直接消失（原版到 500 才衰减完） |
| 26 | 残骸音效的音量 | 原版 = FX 混音组（`Play3D` 默认 `mixerType = FX`）× cue 的 `volume` 随机 | `SoundManager__Play3D.c`（默认 `param_4 = FX`）＋ `AudioCue__ConfigAudioSource.c:44` | `PlayOneShot(clip, WarpforgeAudio.SoundFx * vol)`，**源不挂任何混音组** | `Battle/RemnantSfx.cs:204` | ⚠️已知待修 | **净增益等价**（同一个音效滑块、一次衰减），但绕过了 FX 组 ⇒ 不受 `ToggleSFX` 的 2 s duck 影响，也不吃 FX 组的任何其它处理 |
| 27 | Jingles 冷却与路由 | `SoundManager.Play2D(cue, MixerType.Jingles)` 与 FX 那条**同一个 `ConfigAudioSource`** ⇒ 一样过 `timeToPlayAgain`；组 = `Jingles` | `SoundManager__ConfigureAudioSourceAndPlay.c`（`+0x60` 那支 = jinglesGroup） | `PlayJingle` 先过 `CanPlayByCooldown`，再挂 `JinglesGroup` | `Core/WarpforgeAudio.cs:149-174` | ✅一致 | 冷却表与 FX **共用**（判据：`lastTimePlayed` 住在 cue 上，与从哪条路播无关） |
| 28 | 不经 cue 的直播（如 `OvertimeStart`） | `SoundManager.Play2D(AudioClip)` 不经过 `AudioCue` ⇒ **无冷却、pitch=1、volume=1** | `SoundManager.cs:341-390`（`audioSource.pitch = 1f; audioSource.volume = 1f;`） | `Play(AudioClip, is2d, volume, pitch)` 无冷却，调用点传 `1/1` | `WFSoundPlayer.cs:80-91` | ✅一致 | |
| 29 | **战斗 cue 的发出方式** | **每条调用点直接** `SoundManager.Play2D(cue, FX)` / `Play3D(cue, pos, FX)`：`DrawCard.c:50` · `ChangeAttackType.c:53` · `LandOnGround.c:41` · `_UnitDeath_d__446:368` …（本轮再核 4 处）；`soundsToPlay` 那条 FIFO **只**吃 `PlaySoundAsset(SoundAsset_OLD)`（唯一调用点 `AiScripted.cs:2060`）与 `PlaySoundClip(AudioClip)`（`CardScript` 的 `attackSound`/`deathSound`，**不走队列**） | `decomp_full/{CardScript__DrawCard,CardScript__ChangeAttackType,CardScript__LandOnGround,BattleManager__PlayNextSoundInQueue}.c` ＋ `BattleManager__PlaySoundAsset.c`（`param_2` 是 `SoundAsset_OLD`：`+0x10 = clip`、`+0x18 = volume`） | **整族 27 条 cue 全部入队**，`Tick` 每帧出队一条 | `Battle/BattleSfx.cs:172-179 / 267-327 / 386-412` | ❌不一致 | 同帧多条 cue 被摊到**连续帧**（≈16 ms × N）。单条 cue 延迟 ≤1 帧，听感影响小；但「同一拍落地 + 攻击 + 受击」会散开。建议改直放（或 `Tick` 内抽干） |
| 30 | `WindowSounds` 的冷却层数 | **一层**（`ConfigAudioSource`） | `AudioCue__ConfigAudioSource.c:19-21` | **两层**：`WindowSounds.Play` 先按 cue 名挡一次，`WFSoundPlayer.Play` / `PlayJingle` 再按 cue 实例挡一次 | `Shell/WindowSounds.cs:205-209` ＋ `WFSoundPlayer.cs:104` / `WarpforgeAudio.cs:153` | ⚠️已知待修 | 同窗口同刻 ⇒ 当前无可见差异；但外部直接播过同一条 cue 时，`WindowSounds` 会**计 `Played++` 却没响**（计数与事实不符） |
| 31 | VO 音频来源字段 | `RawCardScript.GetAttackSound()` / `GetDeathSound()`（`rawCardData.attackAudio` / `deathAudio`）→ `PlaySoundClip` | `RawCardScript.cs:553` ＋ ILSpy `CardScript.cs:497` / `:5164` | 同（走 `VoiceLines.ForAttack/ForDeath` → `PlayUnitVoiceClipOnly`） | `Core/VoiceLines.cs:28-29`；`BattleDriver.cs:7665-7691` | ✅一致 | 只有第 10 条的**音量/路由**与阈值不同 |

---

## 已核参数数

**31 条**（上表 #1–#31）：`✅一致` **19** · `❌不一致` **6** · `⚠️` **6** · `❓查不到` **0**。

## ❌ 数

**6 条**（= §三 的前 6 行）。另 6 条 `⚠️` 见 §三 后 3 行与主表 #8 / #20 / #26。

---

## 二、`timeToPlayAgain` 四处清单（现行状态，逐个现读）

公式的**唯一实现** = `WarpforgeVFX/Runtime/WFSoundPlayer.cs:170-179`（泛型，`now = CooldownClock`）。
判据 = `decomp_full/AudioCue__ConfigAudioSource.c:19-21`（`if ((Time.time − last < timeToPlayAgain) && (last != 0)) return 0;`）
＋ `:31` 的「**先写戳、再挑 clip**」。原版**只有一处**（`AudioCue` 上 `+0x30` / `+0x34`），键 = **cue 资产实例**，时钟 = `Time.time`。

| 处 | 文件:行 | 值（窗口） | 键 | 时钟 | 谁在读 | 是否生效 | 与原版一致? |
|---|---|---|---|---|---|---|---|
| ① FX / 特效 / 战斗 cue | `WarpforgeVFX/Runtime/WFSoundPlayer.cs:104`（门）→ `:158`（单参重载）→ `:170`（唯一实现） | `cue.timeToPlayAgain`：`battle_sounds.json` **27/27 = 0.1**；`animfx_sounds.json` 513 条分布 = `0.1`×213 · `0`×158 · `0.5`×54 · `0.05`×39 · `0.2`×30 · `1.0`×9 · `0.01`×8 · `0.15`×1 · `2.05`×1 | **`WFSoundCue` 实例**（= 原版 cue 资产） | `CooldownClock`（`:214`）：`Time.time`，**`Time.time <= 0` 时回退 `Time.realtimeSinceStartup`** | `WFSoundPlayer.Play(cue,…)` 的所有调用点（12 处：`BattleSfx.DrainOne` · `AnimFXController` · `WarpforgeEffectPlayer` · `CardView` · `DraggableController` · `AttackSelector` · `BattleDriver` · `NetMatchmaking` · `WindowSounds` …） | ✅ 生效（生产路径） | ✅ 公式逐字同形；⚠️ 时钟多了一条**批处理兜底**（🔶 自己设计，已在 `:210-213` 标） |
| ② 残骸三时机 | `Battle/RemnantSfx.cs:192`（转调同一实现） | `remnant_cue_props.json`：**6/6 = 0.1**（灵族三档 pitch 0.8–1.24 / vol 0.3；死灵三档 vol 1.0） | **字符串** `"Aeldari/toRemnant"` 这类（**本类自己那张 `_lastAt`**） | 同一 `CooldownClock` | `RemnantSfx.Play` 的调用点（残骸出现 / 收集 / 被打掉） | ✅ 生效 | ⚠️ 键是**字符串**不是 cue 资产。本 build 6 个键 ↔ 6 条 cue 一对一 ⇒ **等价**；若将来两档共用一条 cue 会分叉 |
| ③ 壳侧按钮 / 开包 | `Shell/WindowSounds.cs:208`（转调同一实现） | `cue.timeToPlayAgain`（`animfx_sounds` 里的真值：这批多为 0.1，`Booster Open *` 为 1.0） | **字符串 cue 名**（**本类自己那张 `_lastAt`**） | 同一 `CooldownClock`（2026-11-02 从 `Time.realtimeSinceStartup` 改过来） | `WindowSounds.PlayFx` / `PlayJingles` 的调用点 | ✅ 生效 | ⚠️ 键是 cue **名**；且见下表「双层」 |
| ④ Jingles（结算胜/败/平） | `Core/WarpforgeAudio.cs:153`（转调同一实现） | 同 ① | **`WFSoundCue` 实例**，**与 ① 共用同一张 `_lastAt`** | 同一 `CooldownClock` | `WarpforgeAudio.PlayJingle` 的调用点（`BattleDoors` 三支、开包窗两条） | ✅ 生效 | ✅ 键与表都对（判据：`lastTimePlayed` 住在 cue 上，与哪条 `Play2D` 路无关） |

**已删掉的两份副本（本片复核确认已不存在）**：
`Battle/BattleSfx.cs`（原按**字段名**计时，现只剩 `:139-140` 的删除说明注释）·
`Battle/TapHitMaterialDefinition.cs`（原 `CanPlayCue` 15 行，现只剩 `:68` / `:147` 的占位注释）。

**四处仍存在的偏差（按严重度）**

1. 🔴 **③ 是双层**：`WindowSounds.Play` 自己挡一次（键 = cue 名），随后 `WFSoundPlayer.Play` / `PlayJingle` 再挡一次（键 = cue 实例）。
   原版只有一层。同刻同窗口 ⇒ 当前不可见，但会造成「`Played++` 了却没响」（外部先播过同一条 cue 时）。
2. 🟠 **② / ③ 的键空间与原版不同**（字符串而非 cue 资产）。当前数据下一对一，属**潜在**差异。
3. 🟠 **`ResetThrottle()` 只清 ①④ 那张表**（`WFSoundPlayer._lastAt`），**不清** ②（`RemnantSfx._lastAt`）与 ③（`WindowSounds._lastAt`）。
   ⇒ `BattleSfx.ResetCounters()`（它调 `ResetThrottle`）不是「全项目冷却一起清」，与它的 doc「全部读口清零」有缝。
4. 🟡 **`CooldownClock` 的 `Time.time <= 0` 回退**是我们加的（🔶 自己设计，真实播放环境永不触发）。文档已标，可接受。

---

## 三、差异清单（按影响排序）

> **前 6 条 = `❌不一致`；后 3 条 = `⚠️`（#7 当前不产生可听差异 · #8 当前数据面已带真值 · #9 判据在远端）**。计数以本节为准 **❌ = 6**。

| # | 一句话 | 原版 | 我们 | 出处 |
|---|---|---|---|---|
| 1 | **单位语音（attack/death VO）走错滑块、多乘一个因子** | 音量 = **音效滑块**（`SoundFxVolume`），源**无混音组**，`SoundFxVolume == 0` 时**整族不播** | 挂 **Voices** 组（语音滑块）＋ `PlayOneShot(…, AudioListener.volume)` | 原版 `BattleManager__PlaySoundClip.c:28-35` / `AudioSource_1595.json` ↔ 我们 `BattleDriver.cs:7657,7685` |
| 2 | **3D 衰减最近距离** | `MinDistance = 15.61` | 默认 `1`（`AddComponent` 后没设） | `Audio Source Template.prefab` ↔ `WFSoundPlayer.cs:62-64` |
| 3 | **残骸音效三处 3D 参数全不同** | Logarithmic / 15.61 / 500 | **Linear / 1 / 40** | 模板（同 #2）↔ `RemnantSfx.cs:174-176` |
| 4 | **战斗 cue 被我们排队** | 每条调用点直接 `Play2D`/`Play3D` | 整族入队、每帧出队一条 | `CardScript__DrawCard.c:50` 等 ↔ `BattleSfx.cs:172-179` |
| 5 | **FX 池容量** | `amountToPool = 20` | `PoolSize = 24` | `SoundManager.prefab` ↔ `WFSoundPlayer.cs:27` |
| 6 | **池耗尽行为** | 全部活跃 ⇒ `LogError` + **不播** | 轮转**掐掉最旧的一条** | `SoundManager__GetAudioSource.c` ↔ `WFSoundPlayer.cs:57-73` |
| 7 | **cue 覆盖** | 610 条 AudioCue 资产 | 表内 508 个资产名（另 6 条走残骸专用表）⇒ **≈96 个资产悬空** | `bundle_soundcollection_assets_all` ↔ `Resources/animfx_sounds.json` |
| 8 | **`WFSoundCue.timeToPlayAgain` 默认值** | `0.1`（`AudioCue` ctor） | `0` | `AudioCue__.ctor.c` ↔ `WFSoundBank.cs:28` |
| 9 | **`CanUseVOs` 闸未实现** | 按远端配置两支开关（`+0x10` / `+0x11`） | 恒放行 | `VoiceLinesController__CanUseVOs.c` ↔ （无落点） |

> ⚠️ 上表 #7 的「≈96 悬空」**只表示「没进我们任何表」**，不等于「原版有用」。见 §四。

---

## 四、覆盖边界

- **核过的族**：音量→dB 映射、四通道倍率、落盘范围、3 份 mixer + 8 快照结构、`ToggleSFX` 过渡、
  cue 随机化包装（clip/pitch/volume）、`AudioCue` 默认区间、`timeToPlayAgain` 四处 + 唯一实现、
  气泡语音源 5 个字段 + 寿命公式 + 聊天钮冷却、`VoiceWaveDriver` 全部参数、
  BGM 五拍 + 四条时长 + 13 场表 + 三轨 `AudioSource` 字段、菜单交叉淡变 3 s、
  FX 池容量 / 耗尽策略 / 3D 模板参数、残骸 3D 参数与音量路径、Jingles 路由与冷却、不经 cue 的直播。
- **没核 / 核不动的**：
  1. **`animfx_sounds` 里 513 条 cue 的「谁在用」未逐条对账** —— 本轮只核了「原版有 610 条资产、我们表里 508 条名」，没做「每一条原版 cue → 它的调用方 → 我们有没有接线」的双向对账。
  2. **`AiScripted` 那条 `PlaySoundAsset(SoundAsset_OLD)` 路径我们整体没有**（原版 `AiScripted.cs:2060` 会往 FIFO 塞 `SoundAsset_OLD`）。它属 AI 脚本演出，本轮只确认「这条 FIFO 不服务 cue 族」，**没做它自己的复刻**。
  3. **`CanUseVOs` 的远端配置**（`UnityRemoteConfigManager` 两个开关）—— 关服，判据在远端。
  4. **13 场 aside 的 `MusicManager` 菜单音乐曲目选择规则**未核（本轮只核了交叉淡变机制与 3 s 常量）。
  5. **`AudioSourceMixerGroupFixer`** 只在 `Start()` 里改组（`AudioSourceMixerGroupFixer__Start.c`），本片**没核它挂在哪些实例上**。
  6. **「改了音量后 mixer 快照会不会盖掉滑块」** 这一层已在 `B109_E17` 判过（子 mixer 分开），本片未重复。
- **不可执行的边界**：本片全程只读，**没有出声验证**（批处理下 `Time.time` 恒 0、`AudioSetup.PlayCheck` 需 Play 模式）。

---

## 五、搜索过但没找到的路径与关键词

| 想找 | 搜过哪 | 结果 |
|---|---|---|
| 原版「战斗 cue 入队」的证据 | `out_cpp2il_dev_ilspy` 全树 `grep "\.PlaySoundAsset\(\|\.PlaySoundClip\(\|\.PlayNextSoundInQueue\("` | 只有 3 个调用点：`AiScripted.cs:2060`（`SoundAsset_OLD`）、`CardScript.cs:506`（`attackSound`）、`CardScript.cs:5164`（`deathSound`）。**`soundCollection.*` 的 AudioCue 一律直接 `Play2D`** |
| `FUN_180632C40` 的符号名 | `d:/2/tools/il2cpp_out/dump.cs` 搜 `632C40` / `SoundFxVolume` / `AudioListener` | dump.cs 里**没有 `RVA: 0x632C40` 这一行**（该地址属内联/共享体）。只能靠 `PlaySoundClip` 的闸 `GameStaticData.SoundFxVolume__BackingField > 0f`（ILSpy）反推它是同一个 getter |
| `SoundManager` 池模板的组 | `bundle_scenes_scenes_battlearena1/AudioSource/*.json` 的 `OutputAudioMixerGroup` + `bundle_audiocontrol_assets_all/AudioMixerGroupController/*` 的 `m_Name` | 已解：`5814470231619483780 = Voices`；`1595`（BattleManager 那颗）组为 **0** |
| `VoiceLinesController` 的 `+0x60` 名字 | `Assembly-CSharp/VoiceLinesController.cs` 全字段 + ILSpy | `chatButtonCooldown`（`WaitForSeconds`），源常量 `CHAT_INTERACTABLE_COOLDOWN = 4f` |
| 原版 AI 表情/闲聊节流 | `decomp_full` 里 `VoiceLinesController__{ChatAIResponse,CheckIfEnemyChatReceivedForTip}` | 只有 `chatsReceivedInTurn` 与远端配置；**没找到 14 s 那一档**。我们 `BattleDriver.cs:7064 AiEmoteCooldown = 14f` 目前在 `:7458` 已注明「冷却已删」，**未在本片判定** |
| `SoundManager.prefab` 的 `amountToPool` | `assets_full` 只有 `GameObject/SoundManager.json`（无组件字段）；改走 `assetripper_out/ExportedProject/Assets/SoundManager.prefab` | 已解：20 |
| `Audio Source Template` 的 3D 参数 | `assets_full` 按名字搜不到；走 AssetRipper 工程 | 已解：15.61 / 500 / Log / 128 / Doppler 1 |

---

## 六、附：本片现读到的关键常量（`工具/read_literal.py` · 4B float）

| 地址 | 值 | 用途 |
|---|---|---|
| `0x1834b2bbc` | **2.0** | BGM 开门淡出时长 · 气泡时长下限 |
| `0x1834b3278` | **−60** | `AudioSourceTogglerByVolume` 的暂停/恢复阈值（dB） |
| `0x1834b2dc8` | **0.3** | 气泡时长 = clip.length + 0.3 |
| `0x1834b2e8c` | **3.0** | `battleLow.TransitionTo(3f)` |
| `0x1834b2df0` | **4.0** | `CHAT_INTERACTABLE_COOLDOWN` |
| `0x1834b2bb8` | **1.0** | `SoundManager.Play` 里 pitch / volume 复位 |
