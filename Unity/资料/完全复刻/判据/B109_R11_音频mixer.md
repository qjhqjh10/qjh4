# B109_R11 · 音频 mixer 结构（原版 3 份 mixer / 12 组 / 8 快照）—— 判据 + 施工单

> 只读查判据（2026-10-31 · 第一百零九会话）。**只查不导、不改任何源码/资产/账/正本**：没跑 Unity、没动 git、没写 `d:/2` 一个字节。本件**不重复** `报告/B100_X13.md` §一 已给的结论，只补完整树 + 逐条数值 + 施工与断言。
> 判据源：`d:/2/新解包资源/assets_full/bundle_audiocontrol_assets_all/`（3 mixer + 12 组 + 8 快照 JSON，逐字段亲读）· `d:/2/tools/assetripper_out/ExportedProject/Assets/AudioMixerController/{Main Mixer,Music,FX}.mixer`（YAML，逐行）· ILSpy `out_cpp2il_dev_ilspy/{SoundManager,MixerType,SnapshotsType,BattleMusicManager}.cs`。

---

## 〇、三条**订正**（旧结论是错的，⛔ 别按它干）

1. 🔴 **`Music.m_Children` 为空不是缺陷。** 原版 Main 的 `Music`/`FX` 组 `m_Children` **同样为空**（`Main Mixer.mixer:210-216`/`:148-154`）。子 mixer 是**从自己身上**挂的：`Music.mixer:10` `m_OutputGroup: {fileID: 24300018, guid: 8a821401…, type: 2}`。⇒ 要做的是**新建 `Music.mixer`/`FX.mixer` 并把它们的 `m_OutputGroup` 指向我们 Main 的对应组**，⛔ 不是往 `Music.m_Children` 里塞东西。
2. 🔴 **AssetRipper `.mixer` 的 `m_ExposedParameters[].name` 是别名**（`oiLPOWJ`/`vkPOuSL` 这类），**不是真名**。Unity 的 mixer 参数名哈希 = **CRC32**（本件实测：两组互不相干的字符串 CRC32 完全相同）⇒ 真名可反查，已全部反出（§1.3）。⛔ 别照 YAML 的 `name:` 写字面量。
3. 🔴 我们现有 6 个快照名是**自造**（`SFX On/Off`、`Music Low/Medium/High/None`）。原版真名见 §1.4。

> 另：`ExportedProject` 有两份**内容等价、GUID 不同**的拷贝（`Assets/AudioMixerController/` guid `8a821401…` 与 `Assets/` 根 `fc05d5ca…`，分别来自 `bundle_audiocontrol` 与 `sharedassets0`；组 5/5/2、快照 1/5/2 一一对应）。**用 `AudioMixerController/` 那份** —— 它是 `AudioMixerAddresableReferences` SO 引用的那一套（§1.6）。

---

## §1 原版树

### 1.1 三份 mixer（`AudioMixerController_<pid>.json` 逐字段）

| mixer | pathID | `m_OutputGroup` | `m_MasterGroup` | 组 | 快照 | `m_StartSnapshot` | `m_SuspendThreshold` |
|---|---|---|---:|---:|---|---|---:|
| **Main Mixer** | `4889565729752889476` | `0`（根） | `-2133074371187165052` | 5 | 1 | `Snapshot` | **−50** |
| **Music**（子） | `-371249942676188183` | `5554697269227239556` = **Main.Music** | `-4022650930637646871` | 5 | 5 | `BattleMusicLow` | **−50** |
| **FX**（子） | `-3989599372716181902` | `-3369427022867580796` = **Main.FX** | `-125591673012652430` | 2 | 2 | `General` | **−50** |

### 1.2 十二个组（父链 / 音量参数 GUID / 暴露名 / 起始 dB）

> 「音量参数 GUID」= 组自己的 `m_Volume` —— **就是** `SetFloat` 打的那个参数（⛔ 不是 `Attenuation` 效果的 `m_MixLevel`）。
> 「起始 dB」= **该 mixer `m_StartSnapshot` 落在这一格的值**（组没有独立音量，永远由快照说了算）。

| mixer | 组 | 父 | 音量 GUID | 暴露名 | 起始 dB |
|---|---|---|---|---|---:|
| Main | `Master` | 根 | `6dd1ade045fe3e64ab40064f6863482b` | `VolumeMaster` | **−0.16685188** |
| Main | `FX` | Master | `ce3fed0a7ad4a204ea16e7a6ad796623` | `VolumeFX` | 0 |
| Main | `Music` | Master | `0fde27a32aa8a7a488f489cce7d57588` | `VolumeMusic` | 0 |
| Main | `Voices` | Master | `9894439d0be324c42ad9e7ae6eeb9061` | `VolumeVoices` | 0 |
| Main | `Jingles` | Master | `08fd954a22c333548ac3b73f56b656da` | `VolumeJingles` | 0 |
| Music | `Master` | 根 | `23d9bf2219c310c47aeaf98b798eb36b` | —（未暴露） | 0 |
| Music | `General` | Master | `7b3ec67203fdacc4db6a935e700391f9` | —（未暴露） | 0 |
| Music | `BattleGroupLow` | General | `204f879f1c94e7a448aba50d9ae545c2` | `TrackLowVolume` | **0** |
| Music | `BattleGroupMedium` | General | `4e2042df4c9642b408467dcbe768ae76` | `TrackMediumVolume` | **−80** |
| Music | `BattleGroupHigh` | General | `e2629d6b666f4c54b961f55a87fbe89d` | `TrackHighVolume` | **−80** |
| FX | `Master` | 根 | `1f6b675a5c22b5d40887c5b1715c606a` | —（未暴露） | 0 |
| FX | `General` | Master | `c2e495f03b8fc074a8f133878fe0e76d` | —（未暴露） | 0 |

出处：`AudioMixerGroupController_<pid>.json` ×12 的 `m_Name/m_Children`；GUID 与值 = 各 `.mixer` 的 `m_Volume` 与快照块；组序 = `m_MixerConstant.groups[].parentConstantIndex`。

### 1.3 暴露参数 = CRC32 反查出的**真名**（YAML 里那串是别名）

实测：`crc32("VolumeFX") == crc32("oiLPOWJ") == 2169679594`、`crc32("TrackHighVolume") == crc32("vkPOuSL") == 927329217`，**五对五全中**。

| mixer | 真名 | CRC32 | 对应 |
|---|---|---:|---|
| Main | `VolumeFX` | 2169679594 | FX 组音量 |
| Main | `VolumeJingles` | 1550590027 | Jingles 组音量 |
| Main | `VolumeMaster` | 324528242 | Master 组音量（**代码从不 SetFloat 它**） |
| Main | `VolumeMusic` | 4251868589 | Music 组音量（玩家「音乐」滑块） |
| Main | `VolumeVoices` | 2495387347 | Voices 组音量 |
| Music | `TrackHighVolume` | 927329217 | BattleGroupHigh 音量 |
| Music | `TrackLowVolume` | 1898192573 | BattleGroupLow 音量 |
| Music | `TrackMediumVolume` | 3973261710 | BattleGroupMedium 音量 |
| FX | —— | —— | 一条都没暴露 |

复跑：`python -I -X utf8 -c "import zlib;print(zlib.crc32(b'VolumeFX'))"` → `2169679594`。
代码判据：`SoundManager.cs:569-596` 的 `SetMixerVolume` 里 `name = "Volume" + ((Enum)…).ToString()`；枚举 `MixerType.cs:3-9`
（`FX, Music, Voices, Jingles, BattleGroupLow, BattleGroupMedium, BattleGroupHigh`）⇒ 只有 `VolumeFX/Music/Voices/Jingles` 会被打到；
`VolumeMaster` 与 Music 那三条 `Track*Volume` **在原版是死参数（无调用方），照建、不接**。

### 1.4 八条快照（逐条 dB，全部现读）

`values` 下标由 `groups[j].volumeIndex/pitchIndex` 定（Music：Master 0/1 · General 2/3 · Low 4/5 · Medium 6/7 · High 8/9）。

| mixer | 快照 | Master | General | Low | Medium | High | 过渡覆盖 |
|---|---|---:|---:|---:|---:|---:|---|
| Main | `Snapshot`（起始） | −0.16685188 | FX/Music/Voices/Jingles 全 0 | — | — | — | 无 |
| FX | `General`（起始） | 0 | 0 | — | — | — | **无** |
| FX | `NoVolume` | **−80** | 0 | — | — | — | **无** |
| Music | `General` | 0 | 0 | 0 | −80 | −80 | 只覆盖 Low（`[4]`） |
| Music | `BattleMusicLow`（起始） | 0 | 0 | **0** | −80 | −80 | `[4,6,8]` |
| Music | `BattleMusicMedium` | 0 | 0 | −80 | **0.025578512** | −80 | `[4,6,8]` |
| Music | `BattleMusicHigh` | 0 | 0 | −80 | −80 | **0** | `[4,6,8]` |
| Music | `NoBattleMusic` | 0 | 0 | −80 | −80 | −80 | `[4,6,8]` |

`transitionTypes = 6`（`ParameterTransitionType` 第 6 档，E13 记作 `Attenuation`），`transitionIndices` 指向**三个 BattleGroup 的音量参数**（不是 Master/General）。
语义（`BattleMusicManager.cs:290-372`）：`min(双方督军当前生命) >18`→Low · `>10`→Medium · `≤10`→High，`TransitionTo(5f)`；
`FinishMatch()` 回 Low；开门 `FadeOut()`→`NoBattleMusic.TransitionTo(2f)`；进场先 `noBattleMusic.TransitionTo(0f)`+`ToggleSFX(false,instant:true)` → 等一帧 → `ToggleSFX(true)`+`battleLow.TransitionTo(3f)`。

### 1.5 效果链（我们完全没有这一层）

| mixer.组 | 效果（顺序） |
|---|---|
| Main.Master | `Attenuation` |
| Main.FX | `Attenuation` → **`Duck Volume`**（7 参数 `Param_0..6`） |
| Main.Music | `Attenuation` → **`Duck Volume`**（7 参数） |
| Main.Voices | `Attenuation` → **`Send`→FX.Duck** → **`Send`→Music.Duck**（`m_EnableWetMix:1`） |
| Main.Jingles | `Attenuation` → **`Send`→Music.Duck** |
| Music 五组 / FX.Master / FX.General | 各自 `Attenuation`；FX.Master 多一条 **`Receive`** |

Duck 缺省值（`Main Mixer.mixer:331-344`）：FX duck = `−25 / 2 / 0.2 / 0.3 / 0 / 10 / 1`；Music duck = `−35 / 2 / 0.2 / 0.3 / 0 / 10 / 1`；三条 Send 电平 = `0` dB。`numSideChainBuffers`：Main 2 · FX 1 · Music 0。

### 1.6 音源路由（实据，不是推测）

| 音源 | 挂哪个组 | 出处 |
|---|---|---|
| 战斗 BGM 三条轨 | **Music.BattleGroupLow/Medium/High** | 13 个战场各 1 份，`bundle_scenes_scenes_battlearena*/AudioSource/*.json` 的 `OutputAudioMixerGroup.m_PathID`（现读 **13/13 场**都是 low/med/high 各 1） |
| 菜单音乐（两个源） | **Music.General** | `bundle_staticgeneralassets_assets_all/AudioSource/AudioSource_2305825550575662284.json`、`_8606218221231011020.json` |
| 战场环境/语音源（3 个） | **Main.Voices** | `bundle_scenes_scenes_battlearena1/AudioSource/AudioSource_{1596,1598,1600}.json` |
| 运行时 FX/Voices/Jingles | FX→**FX.General** · Voices→**Main.Voices** · Jingles→**Main.Jingles** | `SoundManager.cs:137-157` 走 `GetMixerGroup(MixerType)`；`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3594.json` 的 `groups[0..3]` 直接给 pathID |

⇒ **滑块 `VolumeMusic` 与「战斗 BGM 换轨」是两根不同的浮点**（前者在 Main.Music，后者在 Music 子 mixer 的三个 BattleGroup）—— E13 记的「两条代价」在原版结构下自然消失。

---

## §2 我们现状（`Resources/Audio/Main Mixer.mixer` 现读 8,783 B）逐条差

| # | 项 | 原版 | 我们 | 判据点 |
|---|---|---|---|---|
| 1 | mixer 份数 | **3** | **1** | `find -iname *.mixer` 只有 `Main Mixer.mixer` |
| 2 | 组数 | **12** | **5** | `:158-264` 五个 `AudioMixerGroupController` |
| 3 | Music 子 mixer 路由 | Music.General / BattleGroup×3 | **不存在**；BGM 挂 `Main.Music` | `Battle/BattleMusic.cs:139` · `Shell/ShellRuntime.cs:901` |
| 4 | FX 子 mixer 路由 | FX.General | 直接挂 `Main.FX` | `Core/WarpforgeAudio.cs:78-80` |
| 5 | 暴露参数 | Main 5 + Music 3 = **8 条** | **4 条** | 缺 `VolumeMaster`；Music 三条随子 mixer 一起建 |
| 6 | 快照 | **8 条**（Main 1 / FX 2 / Music 5） | **6 条**，全在 Main、名字自造 | `:128-134` |
| 7 | 快照落点 | **子 mixer 的 BattleGroup** | **`VolumeMusic` 这根玩家滑块参数** | `:265-293` 六条全写 `894f6eda…` ⇒ E13 那两条代价的根源 |
| 8 | 效果链 | Duck Volume + Send + Receive | **一条都没有**（每组只有 `Attenuation`） | `:16-34` / `:63-76` |
| 9 | `m_SuspendThreshold` | **−50** | **−80** | `:136` |
| 10 | `m_StartSnapshot` / `m_TargetSnapshot` | `Snapshot` / `Snapshot` | `SFX On` / `Music None` | `:135` / `:157` |
| 11 | Master 缺省音量 | −0.16685188 | ✅ 一致 | `:172` / `:206` |

---

## §3 施工方案

### 3.1 ✅ 能**纯文本**改的（不跑 Unity）

**(A) 新建 `Resources/Audio/Music.mixer` 与 `FX.mixer`** —— 拿原版 YAML 当模板，**整份照抄**（含 `m_MixerConstant` 的 groups/effects/snapshots 三张表，那是自洽闭包，改一个字段要改一片），只动一处：
- `Music.mixer`：`m_OutputGroup: {fileID: 1621529463892821992, guid: b4f23f9c2ed2b6049839123e7a4b6041, type: 2}`（`1621529463892821992` = 我们 `Main Mixer.mixer:227` 的 `Music` 组块；guid = `Main Mixer.mixer.meta`）
- `FX.mixer`：`m_OutputGroup: {fileID: -7176780287274492476, guid: b4f23f9c2ed2b6049839123e7a4b6041, type: 2}`（`-7176780287274492476` = `:16` 的 `FX` 组块）
- 各写一份新 `.meta`（`NativeFormatImporter` + `mainObjectFileID: 24100000` + **新 guid**，两件互不相同）
- ⚠️ **不要**同时改 `Master.m_Children`（§〇-1）

**(B) 改 `Main Mixer.mixer`**：`:136` `-80`→**`-50`**；`:139-147` 补第 5 条暴露参数 `guid: 36724ffed37717040baf0cc002255e7d` / `name: VolumeMaster`（`:172` 就是 Master 的 `m_Volume`）；快照删掉自造 6 条、改回原版 1 条 `Snapshot`（值 = §1.4 第一行 + §1.5 的 duck/send 值）。
🔴 **效果链（Duck/Send/Receive）强烈建议不做文本手改**：每条新效果都要在 `m_MixerConstant.effects[]` 补行（`type`：`-2` Attenuation · `-3` Send · `-4` Receive · `-5` Duck Volume）并把参数 GUID 追加进 `snapshots[0].values`，**索引错一位整份 mixer 就崩、且是静默的（导入后组树空掉）**。要么整份照抄原版 Main Mixer，要么在 Unity 窗口里加。

**(C) `.cs` 改动清单**（不跑 Unity 就能写、能过秒级 typecheck）：
| 文件 | 改什么 |
|---|---|
| `Core/WarpforgeAudio.cs` | `:270-276` `SnapshotNames` 拆成 Main/FX/Music 三份；`GetSnapshot` 改成在**对应子 mixer** 上 `FindSnapshot`；`:78-92` FX/Music 改取子 mixer 的 `General`；新增 `MusicMixer`/`FxMixer`/`BattleGroupGroup(i)` |
| `Battle/BattleMusic.cs` | `:139` 三条轨改挂 `BattleGroupGroup(i)`；把 `:25-31` 的「等价物」线性淡变换成 `snapshot.TransitionTo(3/5/5/2)`；`FinishMatch` 回 `BattleMusicLow` |
| `Battle/BattleDoors.cs:339`·`BattleDriver.cs:7638,17441`·`CardDisplayWindow.cs:697`·`UnitChatPanel.cs:767`·`CampaignScriptRunner.cs:995` | **不改**（Voices/Jingles 仍在 Main、名字不变） |
| `Editor/AudioSetup.cs` | `:396-411` `SnapshotValuesTable` 作废（改由 §1.4 驱动）；`Verify()` 增加 §4 的 1/3/4/7 条 |
| `工具/setup_audio_mixer.py` | `GROUPS` 四组逻辑**保持**；子 mixer 不走它 |

### 3.2 ⚠️ 必须**在 Unity 里做**的（子代理不许跑，铁律 13·3）

1. **导入校验**：三份 `.mixer` 放进去后跑 `-executeMethod AudioSetup.Verify`，确认 Unity 认这份 YAML。
   判据：`Resources.Load<AudioMixer>("Audio/Music")` 非 null + `FindSnapshot("BattleMusicLow")` 非 null + `GetFloat("TrackHighVolume")` 返回 true。
2. **若 Unity 不认**（组树空掉/快照丢失）⇒ 退回手工：Audio Mixer 窗口建 `Music`/`FX` 两份 mixer → 把子 mixer 的输出组拖到父 mixer 的组 **Output** 字段 → 手工建 5/2 条快照。**这一步没有文本替代。**
3. 真机听感验收（`资料/真Play待验清单.md` 那一档）：切档有没有真换轨、语音有没有压低音乐。

---

## §4 断言要点 + 风险

**断言（放 `Editor/AudioSetup.cs` 的 `Verify()`，编辑器侧、可批处理）**
1. 三份 mixer 都 `Resources.Load` 得到。
2. **跨资产挂接硬断言**：`MusicMixer.outputAudioMixerGroup.groupID == 我们 Main 里 Music 组的 groupID`（⛔ 别只比 `name` —— 两边都叫 `Music`）；
   `Music.FindMatchingGroups("BattleGroupLow/Medium/High")` 各 1 个。
3. 暴露参数 8 条全在（`GetFloat` 返回 true）：§1.3 全表。
4. 快照 8 条全在：Main `Snapshot` · FX `General/NoVolume` · Music `General/BattleMusicLow/Medium/High/NoBattleMusic`。
5. 🔴 **灭自证断言**：Music 切 `BattleMusicLow` ⇒ `TrackLowVolume≈0 dB` 且 `TrackHighVolume==−80`；切 `BattleMusicHigh` ⇒ **恰好相反**。只有这条能证明「快照落在 BattleGroup 上、没落在 `VolumeMusic` 上」。
6. **滑块与快照不互抢**：Music 快照块的 `m_FloatValues` 里**不得出现 `VolumeMusic` 的 guid**（文本层可断）；真值往返只能真机/Play 验（批处理无音频线程，`AudioSetup.cs:122-131` 已记）。
7. **结构断言（文本层）**：`Music.mixer` 的 `m_OutputGroup.guid == Main Mixer.mixer.meta` 的 guid，且 `fileID == Main 里 m_Name: Music 那个组块的 &id` —— 挡「挂到同名的另一个 Music 组上」。

**风险**
- 🔴 **改 mixer 会不会影响现有音量滑块？不会。** 三根滑块打的 `VolumeFX/VolumeMusic/VolumeVoices/VolumeJingles` **在 Main 上原地保留**，子 mixer 只是让声音多经一级（FX.General→FX.Master→Main.FX）。唯一前提：音源改挂子 mixer 之后那四个参数**仍在上游**（已核：都是）。
- ⚠️ **`m_MixerConstant` 是 native 数据、索引互相引用**；手改错一位 = 静默崩（见 §3.1-B）。
- ⚠️ 子 mixer 放 `Resources/` ⇒ 5 条快照一起进包；**路径不能改**（`m_OutputGroup` 里的 guid 引用不变，`Resources.Load` 路径要跟着改）。
- ⚠️ 换快照后 `BattleMusic` 的三条轨要**常驻播放**（原版另有 `AudioSourceTogglerByVolume` 按组音量 ≤−60 dB 自动 `Pause`/对齐 `timeSamples` 再 `Play`）。不接那个组件**不影响正确性**，只白烧 CPU —— 另开一条，别在本批硬塞。

---

## §5 还差什么

1. **「整份拷贝原版 YAML 能被 Unity 6000.3.23f1 认」是推断，没实测**（依据：两份导出都来自 Unity 6000.2、字段集合与 `工具/_audio_mixer_base.yaml` 一致）⇒ 第一个执行代理**必须先做 §3.2-1 的导入校验**。
2. **`Duck Volume` 的 7 个 `Param_*` 语义没查**（本件只抄了值 −25/2/0.2/0.3/0/10/1）；要复刻「语音压低音乐」的听感得知道 `Param_1=2` 是 ratio 还是别的。
3. **`AudioSourceTogglerByVolume` 不在本批范围**（`BattleMusic.cs` 文件头 ⑥ 已记判据，接不接另议）。
4. `VolumeMaster` 与 Music 那三条 `Track*Volume` 在原版**无调用方** ⇒ 复刻时照建、不接（铁律 18·0 照抄原版死参数）。
5. 本件**没跑 Unity、没动 git、没改任何源码/资产/账/正本**；只写了本文件。
