# T06d · 战斗 BGM 判据

> 只读普查（2026-10-16）。判据序：反编译 `d:/2/tools/decomp_full/` › `dump.cs` › 原始 bundle（UnityPy 实读）› 场景资产。
> 一句话：**战斗 BGM = 每场战场场景自带的三条「强度分轨」（或单轨），靠 `Music` 混音器的 4 个快照硬切音轨；没有全局歌单、没有按模式/阵营挑歌。**

## 一、放哪首（表/字段 → 取值链）

**不是**一张表挑的 —— **曲子烘在战场场景里**：

```
<arena>.unity（StreamedScene，bundle = bundle_scenes_scenes_<arena>）
  └ Battle Music Controller        GO，挂 BattleMusicManager（arena1 = MonoBehaviour 5188）
      ├ Music Low Intensity        GO [+ AudioSource + AudioSourceTogglerByVolume + AudioSourceMixerGroupFixer]
      ├ Music Medium Intensity     同上
      └ Music High Intensity       同上
```

- 每个 AudioSource：`m_PlayOnAwake = true` · `Loop = true` · `m_Volume = 1.0`；混音组依次 = `BattleGroupLow/Medium/High`（由 `AudioSourceMixerGroupFixer.mixerType = 4/5/6` 在 `Start()` 里赋组，`AudioSourceMixerGroupFixer__Start.c:22`）。
- 🔴 **clip 存在 `AudioSource.m_Resource`（`{_FileID:3, _PathID:30/31/32}`），不是 `m_audioClip`** —— 后者 13 场**恒 0**。
  出处：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/AudioSource/AudioSource_1597.json`（`m_audioClip={0,0}` / `m_Resource={3,31}`）；UnityPy 实读同一 bundle 印证：clip 31 = `Warpforge Ultramarines Combat Low`，长度 **129.438 s**。
  ⛔ 只读 `m_audioClip` 会得出「BGM 没有 clip」的**假结论**。

逐场（UnityPy 实读 13 个 `scenes_scenes_battlearena*.bundle`）：

| 战场 | `useMultipleTracks` | 曲子 |
|---|---|---|
| arena1 / 2 / 3 | 1 | `Warpforge Ultramarines` / `Orks` / `Necrons` `Combat Low·Medium·High`（三轨，各 129.44 s） |
| aeldari / blacklegion / leviathan | 1 | `Warpforge Aeldari` / `Black Legion` / `Tyranid` `Combat L·M·H` |
| **astramilitarum** | **1** | **只有 1 首 `Warpforge_Astra_Militarum`，三个 source 装同一首**（异常档，见 §六-2） |
| darkangels / emperorschildren / spacewolves | 0 | `Warpforge_Dark_angel_Theme` / `…Emperors_Children_Theme` / `…Space_wolves_Theme`（同一首 ×3） |
| genestealers / sororitas / tau | 0 | `…Genestaler Cults Combat Medium` / `…Sororitas Combat Medium` / `…Tau Empire Combat Medium`（同一首 ×3） |

- `useMultipleTracks == false` ⇒ `Initialize()` 直接 **`enabled = useMultipleTracks`**（`BattleMusicManager__Initialize.c:5`）⇒ 组件停用 ⇒ `Update()` 不再切档 ⇒ 单曲从头放到尾。
- 混音器层级（UnityPy 实读 `bundle_audiocontrol_assets_all`）：`Main Mixer: Master → {FX, Music, Voices, Jingles}`；**`Music` 是子混音器**：`Master → General → {BattleGroupLow, BattleGroupMedium, BattleGroupHigh}`（组名取自 `AudioMixerGroupController` 资产名 + mixer 的 `groupNameBuffer`）。
- **战斗 BGM 不走 `MusicManager`**：全仓 `PlayMusic` / `PlayMusicQueued` 的调用点**只有** `MainMenuMusicController__Initialize.c`（菜单主题/待机曲），战斗侧一条都没有。

## 二、什么时候切

| # | 事件 | 动作 | 出处 |
|---|---|---|---|
| 1 | 战场场景加载（`playOnAwake`） | 三条分轨**各自从 0 秒起同时播**（Loop）；此时混音器在 `m_StartSnapshot = BattleMusicLow` ⇒ 只有 Low 轨听得见 | `Music` mixer 的 `m_StartSnapshot`；`AudioSource_1597/1599/1601.json` |
| 2 | `BattleManager.OnInitializationComplete` | `MusicManager.StopMusic()`（淡出菜单曲）→ `StartCoroutine(BattleMusicManager.Init())` → 再起一条协程（§五-3） | `BattleManager__OnInitializationComplete.c:32,34,35,45` |
| 3 | `BattleMusicManager.Init()` | `hasMatchEnded = false`，**yield 一帧** | `BattleMusicManager._Init_d__15__MoveNext.c` |
| 4 | `BattleMusicManager.Start()` 第 0 帧 | 取快照 2/3/4/5 → **`noBattleMusic.TransitionTo(0f)`（瞬时静音）** → `SoundManager.ToggleSFX(false, instant:true)` → yield 一帧 | `..._Start_d__13__MoveNext.c:50,55,60,65,69,85` |
| 5 | `Start()` 第 1 帧 | `SoundManager.ToggleSFX(true)` → **`battleLow.TransitionTo(3.0f)`（3 秒淡入到 Low）** | 同上 `:34,37`；常数 `0x1834b2e8c = 3.0f`（`工具/read_literal.py`） |
| 6 | 对局中每 **`timeToCheck` = 5.0 s** | `timePassed += Time.deltaTime`；`>= timeToCheck` ⇒ 归零 + `CheckMusicState()` | `BattleMusicManager__Update.c:9-16`（另要求 `InitializableBehaviour.status == 2 (Initialized)` 且 `!hasMatchEnded`，字段 0x24/0x64） |
| 7 | `CheckMusicState()` 判出不同档 | `snapshot.TransitionTo(timeToChange = 5.0f)`（**5 秒交叉淡变**） | `BattleMusicManager__CheckMusicState.c:65-78`（`timeToChange` @0x50） |
| 8 | `BattleManager.BattleFinished` | `FinishMatch()`：`hasMatchEnded = true` + **`battleLow.TransitionTo(5.0f)`** —— ⚠️ 回 **Low**，**不是**静音 | `BattleManager__BattleFinished.c:44`；`BattleMusicManager__FinishMatch.c:4-8` |
| 9 | `EndBattleDoors.SetupDoor`（结算开门/门内视频前） | `FadeOut()`：**`noBattleMusic.TransitionTo(2.0f)`** | `EndBattleDoors__SetupDoor.c:140`；`BattleMusicManager__FadeOut.c:5-7`；`0x1834b2bbc = 2.0f` |

**旁带件**（不是切档，是省 CPU）：`AudioSourceTogglerByVolume.Update` 每帧读**自己那条分轨所在组的音量**（`audiomixerVolumeLabel` = `TrackLowVolume / TrackMediumVolume / TrackHighVolume`，即 mixer 暴露参数）——
`音量 > -60 dB 且没在播` ⇒ 从**兄弟轨**取 `timeSamples` 对齐后 `Play()`；`音量 <= -60 dB 且在播` ⇒ `Pause()`。
出处 `AudioSourceTogglerByVolume__Update.c:17,21,40,42,52`；阈值 `0x1834b3278 = -60.0f`（read_literal）。

## 三、三档强度快照

**判据式**（`BattleMusicManager__CheckMusicState.c:24-78`；字段偏移经 `dump.cs:98701-98720` 逐条核过）：

```
h = min( GetHero(true).currentHealth , GetHero(false).currentHealth )   // 两个督军的【当前生命】
h >  midHealthChange(18)        → BattleMusicLow    (this+0x30)
highHealthChange(10) < h <= 18  → BattleMusicMedium (this+0x38)
h <= highHealthChange(10)       → BattleMusicHigh   (this+0x40)
```

- `BattleManager.GetHero(bool playerHeroY)` → `CardScript`（`dump.cs:32640`）；生命 = **`EntityScript.<currentHealth>k__BackingField`，`ObscuredInt` @0x68**（`dump.cs` EntityScript 字段块；0x68→0x7C 共 20 字节，正好对上反编译里 0x68/0x70/0x78 三段读取）。
- 🔴 **取「两个督军里较低的那个」**，不是「我方」「差值」「变化量」—— 字段名叫 `HealthChange`，但**语义就是血线阈值**（不是 Δ，没有基线）。
- 字段与初值（`BattleMusicManager__.ctor.c:6-9`）：`timeToChange = 5.0f`(0x50) · `timeToCheck = 5.0f`(0x54) · `midHealthChange`(0x58) 初值 **20** · `highHealthChange`(0x5c) 初值 **10**。
- 🔴 **13 场序列化值一律 `midHealthChange = 18` / `highHealthChange = 10`**（`bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_<pid>.json`，例 arena1 = 5188；“18” 与 C# 初值 20 不同）⇒ **落地用 18 / 10**。
- 快照音量（UnityPy 实读 `audiocontrol_assets_all` 的 `Music` mixer `m_MixerConstant.snapshots`；顺序已用 `snapshotGUIDs` 与 `AudioMixerSnapshotController` 资产名**逐条对上**）：

| 快照 | BattleGroupLow | BattleGroupMedium | BattleGroupHigh |
|---|---|---|---|
| `General`（`m_StartSnapshot` 之外的默认档，非战斗） | 0 dB | −80 dB | −80 dB |
| **`BattleMusicLow`** | **0 dB** | −80 dB | −80 dB |
| **`BattleMusicMedium`** | −80 dB | **+0.026 dB** | −80 dB |
| **`BattleMusicHigh`** | −80 dB | −80 dB | **0 dB** |
| **`NoBattleMusic`** | −80 dB | −80 dB | −80 dB |

  即**硬切音轨**（−80 dB = 静音），**不是**音量渐变；听感上的渐变全来自 `TransitionTo` 的 3 / 5 / 2 秒过渡。
- 换快照的触发点只有：`Update` 每 5 s 的 `CheckMusicState()` + §二 的 4/8/9 三处。`ChangeState()` 是被内联进 `CheckMusicState` 的私有 helper（`BattleMusicManager__ChangeState.c` 行为一致，`BattleMusicState{LowIntensity=0, MidIntensity=1, HighIntensity=2}`）。
- 快照 PPtr **13 场字面值完全相同**（`battleLow = -3926211066113174551` …），来源 = `AudioMixerAddresableReferences.GetSnapshot(SnapshotsType)`，枚举 `SnapshotsType{MusicBattleMusicLow/Medium/High/NoMusic = 2/3/4/5}`。

## 四、我们这边最小实现建议

1. **曲子**：13 场的 ogg **已在本地** `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/AudioClip/*.ogg`。搬进 `Resources/`（例 `Audio/AudioBattle/<arena>_Low|Medium|High.ogg`），**逐场一张旁挂表**（`arena → 三元组 + useMultipleTracks`），照战场线的「旁挂数据 + 生成脚本」模式。⚠️ 抄表按 **`m_Resource`** 认曲子。
2. **混音器**：我们的 `CardPresentation/Resources/Audio/Main Mixer.mixer` 现只有 `Master/FX/Music/Voices/Jingles` + **1 个快照 `Snapshot`、0 个暴露参数**（实读该文件）⇒ 要补：`Music` 下加 `BattleGroupLow/Medium/High`、加 4 个快照（音量照上表）、暴露 `TrackLowVolume/TrackMediumVolume/TrackHighVolume`。⚠️ `WarpforgeAudio.ApplyAll()`（`WarpforgeAudio.cs:66-72`）写的是 `VolumeMusic/VolumeFX/…` —— 原版三个 BattleGroup 是 **`Music` 的子组**，天生被 `VolumeMusic` 管住，加组时别破坏这条。
3. **组件**：建 `BattleMusic`（三个子 GO 各带 `AudioSource(loop, playOnAwake)` + 一个「按混音器音量暂停/续播并 `timeSamples` 对齐」的小件），参数照抄 `useMultipleTracks / timeToChange=5 / timeToCheck=5 / midHealthChange=18 / highHealthChange=10`。
4. **接线点**：开战 → 静音一帧 → 3 s 淡入 Low；每 5 s 取 `min(双方督军 currentHealth)` 判档、5 s 淡变；`BattleFinished` → 回 Low(5 s)；结算开门 → `NoBattleMusic`(2 s)。
5. **可写断言**：① 单轨场 `useMultipleTracks==0` ⇒ 不切档（组件停用）；② 阈值 18/10 且取 **min**（一只督军 25 血 / 另一只 8 血 ⇒ 必须 High）；③ `FinishMatch` 落在 **Low** 不是静音；④ 时长 3 s / 5 s / 2 s。

## 五、没查清的

1. **`Track*Volume` 暴露参数名 ↔ 哈希对不上**：mixer 里 `exposedParameterNames = [927329217, 1898192573, 3973261710]`、`exposedParameterIndices = [8,4,6]`（= High/Low/Medium 三组音量，**语义已定**）。但拿 `TrackLowVolume/TrackMediumVolume/TrackHighVolume` 试 `Unity *31` / `djb2` / `FNV-1` / `FNV-1a`（大小写 × 种子 4 种组合）**全不匹配** ⇒ 哪个哈希对哪个标签**没查清**（不影响落地：每个 toggler 读的是「自己那条轨所在组」的音量，对应关系由构造保证）。
2. **`<Start>d__13` 那一帧 yield 的具体对象**读不出（反编译显示成编译器生成类型，两处调用点是 `<>f__AnonymousType31`）—— 语义「等一帧」明确，类型未定。
3. **`OnInitializationComplete` 里紧跟着起的第二条协程**（`BattleManager__OnInitializationComplete.c:45`；捕获对象类型是 `<>f__AnonymousType7<Army>` ⇒ `new { Army = <某阵营> }`）**没查**。与 BGM 无直接关系，但可能是「按阵营播开场」一类，留给别的件。
4. **`decomp_full` 非全量**（25,096 / 26,282）。本轮 grep 过的词：`PlayMusic` / `PlayMusicQueued` / `MusicManager__StopMusic` → 调用点**全部枚举到**；`BattleGroup` / `SnapshotsType` / `MusicTransitionType` / `MusicClip` / `battleMusic` / `BGM` → **`decomp_full` 里 0 命中**（枚举成员名不落方法体）⇒ 枚举值与组名一律取自**签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`** 与 **`dump.cs`**，非 grep。
5. 我们侧 **没有任何** 战斗 BGM 代码：`grep -rl "BattleMusic|battleLow|BattleGroupLow|MusicManager|PlayMusic" --include=*.cs` 只命中 `WarpforgeAudio.cs`（注释里提到 BattleGroup 组名）、`ShellScene.cs`、`ShellRuntime.cs`（菜单曲）⇒ 这一格确实是「判据零份」，不是「做了没记」。

## 六、顺手发现

1. 🔴 **`AudioSource` 的 clip 在 `m_Resource`**（§一）—— 不只影响 BGM：**任何**按 `m_audioClip` 判「原版这个 AudioSource 放什么」的结论都要回头复核。实测 `assets_full/.../AudioSource_1597.json` **两个字段都在**（`m_audioClip={0,0}` / `m_Resource={3,31}`）⇒ 判据是齐的，是**读法**的问题。
2. 🔴 **Astra Militarum 是异常档**：`useMultipleTracks = 1` 但**只有一首** `Warpforge_Astra_Militarum`，三个 intensity source 装同一首 ⇒ 三档切换对它**听不出差别**。⛔ 别按「有 3 首 ogg ⇔ `useMultipleTracks=1`」推（genestealers / sororitas / tau 只有 1 首 `…Combat Medium`，但 `useMultipleTracks = 0`）。
3. `MusicBlendTest`（`MusicBlendTest__ChangeMusicIntensity.c` / `…ChangeToLow/Medium/High.c`）是**原版自己的调试场景**：3 个 AudioSource + 3 个快照 + `transitionTime = 0.3f`（`MusicBlendTest__.ctor.c:4` 的 `0x3e99999a`），切档时把非当前轨的 volume 直接压成 **0.0f**（`_DAT_1834b2c00` 4B 读 = `0.0`）。**不是战斗逻辑**，但可作「三轨 blend 就是这个意思」的独立旁证。
4. `MusicManager.PlayMusicQueued` 的等待上限 `0x1834b2d94` 其实是 **`float.MaxValue`**（int 值 `0x7F7FFFFF`）—— 工具 `read_literal.py` 因「值太大」把它标成「不像字面量」。⇒ 菜单曲排队**没有实际上限**，只是 `max(0, 剩余时长 - 3s)` 封底（`MusicManager__PlayMusicQueued.c`）。下次见到 `0x7F7FFFFF` 别当成读错地址。
