# B89_E13 — mixer 快照取值（原版 6 条快照的**真实数值**）

> 执行代理 `E13` · 2026-10-28 · 域 `音频_语音` · 账 `音频/语音:12` / `:80`
> 交付物：本报告 + `账/_parts/B89_E13.tsv` + `Editor/AudioSetup.cs`（追加 `SetSnapshotValues()`）
> + `Editor/AudioSnapshotCheck.cs`（**加 1 条断言**）

## §0 一句话结论

**原版值查到 6 条 / 自定 0 条数值**（6 条 dB 全是解包资源里逐字段抄的，有出处）；
**但「写进我们哪个组」这一层是投影**（🔶 1 处自定，见 §2.3）—— 因为原版是 **3 份 mixer**、
快照落在 **Music / FX 两个子 mixer** 上，我们只有 1 份混好的 mixer。

---

## §1 原版取值表

### §1.1 先定「`SnapshotsType` → 原版快照」的映射（**从 SO 实读，不是按名字猜**）

判据 = 原版 `AudioMixerAddresableReferences` 的**序列化实例**：
`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3594.json`
（`m_Script.m_PathID = -8863975554607179919` = `MonoScript` 里的 `AudioMixerAddresableReferences`）
—— 它的 `snapshots[]` 就是 `AudioMixerReference<SnapshotsType, AudioMixerSnapshot>` 数组，
`customType` 即 `SnapshotsType` 的值：

| customType | `SnapshotsType`（`dump.cs:17953-17962`） | 原版快照名 | 所属 mixer | 快照 pathID |
|---|---|---|---|---|
| 0 | `FxNormal` | `General` | **FX** | `6764119139241457266` |
| 1 | `FxNoVolume` | `NoVolume` | **FX** | `8068468039719941746` |
| 2 | `MusicBattleMusicLow` | `BattleMusicLow` | **Music** | `-3926211066113174551` |
| 3 | `MusicBattleMusicMedium` | `BattleMusicMedium` | **Music** | `774629946988549097` |
| 4 | `MusicBattleMusicHigh` | `BattleMusicHigh` | **Music** | `-1533776937643420695` |
| 5 | `MusicBattleMusicNoMusic` | `NoBattleMusic` | **Music** | `2527524072104055785` |

同一份 SO 的 `groups[]`（`customType 0..6`）—— 顺带把「原版把组挂在哪」也钉住了：

| customType | 组 | 所属 mixer |
|---|---|---|
| 0 | `General` | FX |
| 1 | `General` | Music |
| 2 | `Voices` | Main Mixer |
| 3 | `Jingles` | Main Mixer |
| 4 | `BattleGroupLow` | Music |
| 5 | `BattleGroupMedium` | Music |
| 6 | `BattleGroupHigh` | Music |

⚠️ **13 个战场各有一份** `AudioMixerAddresableReferences` 实例（`MonoBehaviour_3594 / 3789 / 3715 / 3559 /
3758 / 3670 / 3645 / 3649 / 3611 / 3760 / 3815 / 3614 / 4191`），**13/13 的 `snapshots[]` 与 `groups[]` 逐项相同**
（已脚本逐份比过）⇒ 不是「某个战场特例」。

### §1.2 原版是 **3 份** AudioMixer（不是 1 份）

出处（**两份来源逐值相同**，已脚本比过 `m_MixerConstant.snapshots`）：

* `d:/2/新解包资源/assets_full/bundle_audiocontrol_assets_all/AudioMixerController/*.json`（3 份 + 8 份快照 + 12 份组）
* `d:/2/新解包资源/assets_full/sharedassets0/AudioMixerController/{308,309,310}.json`（同一批，pathID 换成 308/309/310）

| mixer | pathID | 组（`m_MixerConstant.groupNameBuffer`） | 快照 | 起始快照 |
|---|---|---|---|---|
| **Main Mixer** | `4889565729752889476` | `Master · FX · Music · Voices · Jingles` | 只有 `Snapshot` | `Snapshot` |
| **Music** | `-371249942676188183` | `Master · General · BattleGroupLow · BattleGroupMedium · BattleGroupHigh` | `General · BattleMusicLow · BattleMusicMedium · BattleMusicHigh · NoBattleMusic` | `BattleMusicLow` |
| **FX** | `-3989599372716181902` | `Master · General` | `General · NoVolume` | `General` |

### §1.3 逐条数值（判据 = `AudioMixerController.m_MixerConstant.snapshots[i].values`）

`values` 的下标由 `m_MixerConstant.groups[j].volumeIndex / pitchIndex` 定：

* **Music** 组序：`Master` 0/1 · `General` 2/3 · `BattleGroupLow` 4/5 · `BattleGroupMedium` 6/7 · `BattleGroupHigh` 8/9
* **FX** 组序：`Master` 0/1 · `General` 2/3

#### FX mixer（`AudioMixerController_-3989599372716181902.json`）

| 快照 | 原始 `values` | Master 音量 | General 音量 | pitch | 过渡覆盖 |
|---|---|---|---|---|---|
| `General`（`FxNormal`） | `[0.0, 1.0, 0.0, 1.0]` | **0.0 dB** | 0.0 dB | 1.0 | **无**（`transitionTypes: []`） |
| `NoVolume`（`FxNoVolume`） | `[-80.0, 1.0, 0.0, 1.0]` | **−80.0 dB** | 0.0 dB | 1.0 | **无** |

⇒ 原版「关 SFX」= **把 FX 子 mixer 的 Master 打到 −80 dB**（整棵 FX 子树静音），General 不动。

#### Music mixer（`AudioMixerController_-371249942676188183.json`）

| 快照 | Master | General | Low | Medium | High | pitch | 过渡覆盖 |
|---|---|---|---|---|---|---|---|
| `General` | 0.0 | 0.0 | **0.0** | −80.0 | −80.0 | 全 1.0 | `Attenuation` 打在 `[4]`（Low 音量） |
| `BattleMusicLow` | 0.0 | 0.0 | **0.0** | −80.0 | −80.0 | 全 1.0 | `Attenuation` 打在 `[4,6,8]` |
| `BattleMusicMedium` | 0.0 | 0.0 | −80.0 | **0.025578511878848076** | −80.0 | 全 1.0 | `Attenuation` 打在 `[4,6,8]` |
| `BattleMusicHigh` | 0.0 | 0.0 | −80.0 | −80.0 | **0.0** | 全 1.0 | `Attenuation` 打在 `[4,6,8]` |
| `NoBattleMusic` | 0.0 | 0.0 | −80.0 | −80.0 | −80.0 | 全 1.0 | `Attenuation` 打在 `[4,6,8]` |

原始 `values`（10 个 float，按上表组序展开，单位 dB / pitch）：

| 快照 | nameHash | values |
|---|---|---|
| `General` | `26480598` | `[0,1, 0,1, 0,1, -80,1, -80,1]` |
| `BattleMusicLow` | `2144997362` | `[0,1, 0,1, 0,1, -80,1, -80,1]`（**与 `General` 逐值相同**） |
| `BattleMusicMedium` | `1301416538` | `[0,1, 0,1, -80,1, 0.025578511878848076,1, -80,1]` |
| `BattleMusicHigh` | `59422316` | `[0,1, 0,1, -80,1, -80,1, 0,1]` |
| `NoBattleMusic` | `4178815000` | `[0,1, 0,1, -80,1, -80,1, -80,1]` |

⇒ 原版「切战斗音乐档」= **只切三条 BattleGroup 的音量**（Master / General 恒 0 dB），
`−80 dB` = 静音；`Medium` 那一档的 `+0.0255785 dB` 是原版**唯一的非 0 增益**（≈ 无感，但它就是原值，照抄）。

**过渡覆盖的枚举值** `6` = `UnityEditor.Audio.ParameterTransitionType.Attenuation`
（`ilspycmd` 反编译 `UnityEditor.CoreModule.dll`：`Lerp=0 · Smoothstep=1 · Squared=2 · SquareRoot=3 ·
BrickwallStart=4 · BrickwallEnd=5 · Attenuation=6`）。
原版 2 条 FX 快照**一条覆盖都没有** ⇒ 关/开 SFX 用的是 `TransitionTo(t)` 传进来的统一时长（2 s）。

#### Main Mixer 的唯一快照（记录用，**本批不写进我们的快照**）

`Snapshot`（nameHash `3577444195`）`values` =
`[-0.16685187816619873, 1, 0, 1, 0, 1, 0, 1, 0, 1, -25, 2, 0.2, 0.3, 0, 10, 1, -35, 2, 0.2, 0.3, 0, 10, 1, 0, 0, 0]`

按 Main 组序（`Master` 0/1 · `FX` 2/3 · `Music` 4/5 · `Voices` 6/7 · `Jingles` 8/9）拆：

| Master | FX | Music | Voices | Jingles |
|---|---|---|---|---|
| **−0.16685188 dB** | 0.0 | 0.0 | 0.0 | 0.0 |

下标 10..26 是效果自动化参数（`m_MixerConstant.effects[]` 的 `parameterIndices`），见 §4 第 3 条。

### §1.4 「查不到」的部分（如实记）

| 想知道 | 结论 |
|---|---|
| 6 条快照的组音量 / pitch | **全部查到**（上表） |
| `TransitionTo` 的**秒数** | **不在 mixer 资产里** —— 由调用方传；原版 `SoundManager.ToggleSFX` 传 `((instant?1:0)^1)*2f`（instant ⇒ 0 s，否则 2 s），`BattleMusicManager` 另有 3/5/5/2 s（已在 `Battle/BattleMusic.cs` 文件头记过） |
| 快照的**逐参数过渡时长** | **查不到**：`m_MixerConstant.snapshots[i]` 只有 `transitionTypes`/`transitionIndices`（= 曲线类型 + 哪几个参数有覆盖），**没有 fade 时长字段**；时长走 `TransitionTo(t)` |
| Main Mixer 那 9 个效果的**名字** | **查不到**：`AudioMixerEffectController` 对象没被导出（`assets_full` 下**没有** `AudioMixerEffectController/` 目录），`pluginEffectNameBuffer: [0]` = 空 ⇒ 只能读 `type` 数值（见 §4 第 3 条） |
| 搜过哪些地方（否定证据的边界） | `assets_full` **全树按目录名** `AudioMixerController / AudioMixerGroupController / AudioMixerSnapshotController` 扫 ⇒ **只有两处**：`bundle_audiocontrol_assets_all`（3 / 12 / 8 份）与 `sharedassets0`（3 / 12 / 8 份，与前者逐值相同）。另按文件名 `*mixer*` 扫全树 ⇒ 只多出 13 个战场的 `AudioMixerAddresableReferences.json`（GameObject 壳）与 `level0` 的一份。**没有别的 mixer**。 |

---

## §2 `SetSnapshotValues()` 做了什么

### §2.1 入口与反射用到的内部 API

`-executeMethod AudioSetup.SetSnapshotValues`（同 `[MenuItem("Warpforge/音频/写快照数值")]`）。
整套是**反射**（`UnityEditor.Audio.*` 全是 internal），签名照 **`ilspycmd` 反编译
Unity 6000.3.23f1 的 `UnityEditor.CoreModule.dll`** 实测抄的（不是猜的）：

```
AudioMixerController.GetAllAudioGroupsSlow()                        → List<AudioMixerGroupController>
AudioMixerController.snapshots                                      → AudioMixerSnapshotController[]
AudioMixerGroupController.SetValueForVolume(AudioMixerController, AudioMixerSnapshotController, float)
AudioMixerGroupController.GetValueForVolume(AudioMixerController, AudioMixerSnapshotController) → float
AudioMixerGroupController.GetGUIDForVolume()                        → GUID
AudioMixerSnapshotController.SetTransitionTypeOverride(GUID, ParameterTransitionType)
AudioMixerSnapshotController.ClearTransitionTypeOverride(GUID)
```

写入后用 `GetValueForVolume` **逐条读回**，偏差 > 0.001 dB 就 `LogError` + 计失败。
最后 `SetDirty(controller + 6 条快照)` → `AssetDatabase.SaveAssets()`（native 写不 SaveAssets 不落盘）。

### §2.2 写进我们 mixer 的哪一格（**6 条都写**）

| 我们的快照名 | `SnapshotsType` | 写的组 | 写的 dB（**原版值**） | 过渡覆盖 | 原版快照 |
|---|---|---|---|---|---|
| `SFX On` | `FxNormal` | `FX` | **0.000** | 无（原版也空） | `FX/General`（Master 0 dB） |
| `SFX Off` | `FxNoVolume` | `FX` | **−80.000** | 无（原版也空） | `FX/NoVolume`（Master −80 dB） |
| `Music Low` | `MusicBattleMusicLow` | `Music` | **0.000** | `Attenuation` | `Music/BattleMusicLow`（Low 0 dB） |
| `Music Medium` | `MusicBattleMusicMedium` | `Music` | **0.025578512** | `Attenuation` | `Music/BattleMusicMedium`（Medium 0.025578511878848076 dB） |
| `Music High` | `MusicBattleMusicHigh` | `Music` | **0.000** | `Attenuation` | `Music/BattleMusicHigh`（High 0 dB） |
| `Music None` | `MusicBattleMusicNoMusic` | `Music` | **−80.000** | `Attenuation` | `Music/NoBattleMusic`（三条全 −80 dB） |

**原版值 6 条 / 自定值 0 条。** pitch 原版全 1.0，我们不动（默认就是 1.0）。

### §2.3 🔶 自己设计（非原版）的**唯一**一处：`原版快照 → 我们哪个组` 这层投影

代码里的标记（`AudioSetup.cs`，值表上方那一段）：

```
//  ==== 🔶 自己设计（非原版）：`原版快照 → 我们哪个组` 这一层【投影】====
```

* **为什么必须自己定**：原版快照落在**子 mixer**（FX 的 `Master`、Music 的 `BattleGroup*`）上；
  我们是**一份混好的 mixer**（`Master + FX/Music/Voices/Jingles`），**没有子 mixer、也没有
  `BattleGroup*`** ⇒ 没有任何一格能 1:1 对上。
* **投影口径**：`SFX *` → `FX` 组（= FX 子树有效增益：其 Master+General 都 0 dB ⇒ 取 Master 那档）；
  `Music *` → `Music` 组（= Master + General + 当前听得见的那条 BattleGroup 之和）。
* **谁定的 + 日期**：调度台 2026-10-28 派活包 §二 明确要求「`SFX *` 动 FX/Jingles 组、`Music *` 动
  Music 组」，本代理按该口径落地并把代价写死（下面两条）。

**两条代价（别当等价）**：

1. `Music Low / Medium / High` 数值上几乎同值（差 ≤ 0.026 dB）—— 因为「放哪一条轨道」在我们这儿是
   `Battle/BattleMusic.cs` 用**代码线性淡变**做的（那是最早的等价实现，文件头已如实记），不是 mixer
   切的。**6 条里只有 `Music None`（−80 dB）听得出差别。**
2. 🔴 **原版快照不与音量滑块抢；我们会抢** —— 原版 `VolumeFX` / `VolumeMusic` 挂在 **Main Mixer 的组**
   上，而快照挂在**子 mixer** 上，两根量不同；我们的 `.mixer` 里 `m_ExposedParameters` 的
   `VolumeFX`/`VolumeMusic` 的 **guid 就是该组的 `m_Volume`**（现读：
   `VolumeFX` = `eec8383b82a5828469552ee45332ccb7` = `FX.m_Volume`；
   `VolumeMusic` = `894f6eda8190955459e1650c10422af1` = `Music.m_Volume`）
   ⇒ **切快照会覆盖滑块设过的值**（例：音效滑块设 −6 dB → `ToggleSFX(false)` → −80 → `ToggleSFX(true)`
   → 回到 0 dB 而不是 −6 dB）。
   **根治**（不在本入口权限内，交调度台）：给 `FX`/`Music` 各加一个子组（`FX Content` / `Music Content`），
   音源改挂子组，快照写子组 —— 那是**运行时路由**改动（`Core/WarpforgeAudio.cs` 的 `FindMatchingGroups`）。
   ⚠️ **缓解事实**：`WarpforgeAudio.ToggleSFX` 今天**零调用方**（`grep -rn "ToggleSFX" Assets` 只命中
   `Core/WarpforgeAudio.cs` 自身与两个 Editor 文件）⇒ 现在还没有线上回归；接线时才会暴露。

### §2.4 幂等

`SetValueForVolume` / `SetTransitionTypeOverride` / `ClearTransitionTypeOverride` 都是**覆盖写字**，
同一组值跑两次结果相同（无新增对象、无累积）。快照**壳**不重建（沿用 `CreateSnapshots`）。

---

## §3 typecheck 读数

`TMPDIR=/tmp/wf_B89E13 bash d:/4/Unity/工具/typecheck.sh`，共跑 5 次（本波 13 路并行，别人的文件在动）：

| 次 | 运行时程序集 | 编辑器程序集 | 报错落在哪 |
|---|---|---|---|
| 1 | 0 | **2** | `Editor/GameStaticDataCheck.cs:78,148`（别人的活） |
| 2 | 0 | **1** | `Editor/GameStaticDataCheck.cs:154`（行号在动 ⇒ 对方正在写） |
| 3 | 0 | **0** | —（全绿） |
| 4 | 0 | 0（`grep error CS` = 0 行） | —（全绿） |
| 5 | **6** | **0** | `Shell/PromptPopup.cs:214,282`（别人的活） |
| 6（收尾） | **0** | **0** | —（**全绿**；别人那几个文件在此期间写完了） |

**我的两个文件（`Editor/AudioSetup.cs` · `Editor/AudioSnapshotCheck.cs`）在编辑器程序集里，
`grep "AudioSetup.cs|AudioSnapshotCheck.cs"` = 0 条错误。** 剩下的报错**全部**落在白名单之外、
且几次跑之间行号/文件都在变（= 别的代理正在写）⇒ 按简报「❌ 别当成自己的错去改别人的文件」处理。

---

## §4 顺手发现（不是本批能修的，请调度台分流）

1. 🔴 **原版是 3 份 mixer，我们压成了 1 份** —— 结构性偏离。影响两处：① 快照与滑块抢同一根浮点
   （§2.3 代价 2）；② 原版 `MixerType` 到组的映射里有 3 个 `BattleGroup*` 组，我们一个都没有。
   建议新账：**给 `FX`/`Music` 加子组并改路由**（判据已齐，见 §1.1/§1.2）。
2. 🔴 **原版 Music 的 `BattleGroupLow/Medium/High` 三轨机制没复刻** —— 我们是 `BattleMusic` 代码淡变。
   `BattleMusic.cs` 文件头**已经如实写明**是「等价物、不是原件」⇒ 这条属于**已知偏离**，
   但账上「原版靠 mixer 切三轨」这半边今天仍是空的（要不要补、怎么补需调度台定）。
3. **mixer 的效果链完全没复刻**（新账，判据在 §1.2 的两个 json）：
   * **Main Mixer**：9 个 effect —— 每个内容组一个 `type: -2`（与我们的 `Attenuation` 同位、无参数）；
     `FX` 与 `Music` 各多一个 **`type: -5`、带 7 个自动化参数**的效果（`parameterIndices` 10..23）；
     `Voices`/`Jingles`/`FX`/`Music` 另有 **`type: -3` 的 send**（`sendTargetEffectIndex` + `wetMixLevelIndex`
     24/25/26 非空）。
   * **FX mixer**：2 个 effect（`type: -2` + `type: -4`），`numSideChainBuffers: 1`。
   * ⚠️ **效果名查不到**（`AudioMixerEffectController` 没被导出，`pluginEffectNameBuffer: [0]`），
     只能记 `type` 数值 —— 要定名得回二进制或编辑器。
   我们现在的 mixer **每组只有一个 `Attenuation`**（现读 `Main Mixer.mixer`）。
4. **原版 Main Mixer 起始快照的 `Master` = −0.16685187816619873 dB**，我们 `Master` = 0 dB。
   差 0.17 dB（听不出），但它是原版值，记一笔免得以后被当成「我们编的」。
5. **原版暴露参数的分布**（这条是**好消息**，说明滑块那条链没跑偏）：
   `Main Mixer` 暴露 5 条（`exposedParameterIndices = [2,8,0,4,6]` = FX / Jingles / **Master** / Music / Voices 的音量）
   · `Music` mixer 暴露 3 条（`[8,4,6]` = BattleGroupHigh/Low/Medium 音量）· `FX` mixer **一条都没有**。
   ⇒ 原版 `SetMixerVolume` 的 `"Volume"+枚举名` 确实打在 **Main Mixer 的组**上，与我们的
   `VolumeFX/Music/Voices/Jingles` 一致 ✓。**多出来的那一条**是 `Master` 音量（我们没暴露、也没有对应滑块）
   —— 原版设置页有没有「总音量」那根滑块，需另外查（不在本批）。
6. 我们 mixer 的 **`m_TargetSnapshot` = `Music None`**（编辑器状态字段）。写值之后它带 −80 dB 的 Music 值；
   运行时起始快照是 `m_StartSnapshot` = `SFX On`（不带 Music 值）⇒ **开局不受影响**；但在 Mixer 窗口里
   看到的「当前快照」会是 `Music None`，**别当成 bug**。
7. `Music` mixer 的 `General` 与 `BattleMusicLow` 两条快照**逐值完全相同**（原版自己也冗余），
   ⇒ 若将来要「省一条」，那是原版就有的冗余，不是我们漏了。

---

## §5 调度台要跑的命令与期望读数

### ① 写值（一次性；幂等，可重跑）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod AudioSetup.SetSnapshotValues -logFile -
```

期望日志（6 行 + 1 行合计）：

```
AUD-SV [0] `SFX On`     → 组 `FX`    =   0.000 dB · 过渡覆盖 无（原版这两条也是空的） · 原版快照 `FX/General`
AUD-SV [1] `SFX Off`    → 组 `FX`    = -80.000 dB · 过渡覆盖 无（原版这两条也是空的） · 原版快照 `FX/NoVolume`
AUD-SV [2] `Music Low`  → 组 `Music` =   0.000 dB · 过渡覆盖 Attenuation · 原版快照 `Music/BattleMusicLow`
AUD-SV [3] `Music Medium` → 组 `Music` = 0.026 dB · 过渡覆盖 Attenuation · 原版快照 `Music/BattleMusicMedium`
AUD-SV [4] `Music High` → 组 `Music` =   0.000 dB · 过渡覆盖 Attenuation · 原版快照 `Music/BattleMusicHigh`
AUD-SV [5] `Music None` → 组 `Music` = -80.000 dB · 过渡覆盖 Attenuation · 原版快照 `Music/NoBattleMusic`
AUD-SV === 合计：6 通过 / 0 失败 ===
```

**跑前先留一份 `Main Mixer.mixer` 的副本**（本入口会写盘；幂等，但如果要回退得靠副本或 git）。

### ② 复核（只读，不写盘）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod AudioSetup.VerifySnapshotValues -logFile -
```

期望：`AUD-SV-V` 6 行全 `✓`，合计 `6 通过 / 0 失败`。

### ③ 现有自检（已在 `_run_8_checks.sh` 第 58 行里，**不必改脚本**）

```bash
... -executeMethod AudioSnapshotCheck.Run -logFile -
```

期望：**10 通过 / 0 失败**（原来是 9 条，本批加了 **1 条**
「6 条快照的**组音量**已按原版写进去」）。
⚠️ **写值之前跑它会是 9 通过 / 1 失败** —— 那条**本来就应该红**（它在盯的就是「壳有、值没有」）。

### ④ 不跑 Unity 也能核的两眼（可选）

```bash
grep -c "m_FloatValues: {}" "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Audio/Main Mixer.mixer"
# 期望 = 0（跑之前是 6）
grep -A1 "m_Volume: eec8383b82a5828469552ee45332ccb7" "…/Main Mixer.mixer"   # FX 组的音量参数 guid
```

⚠️ 若 `SFX On` / `Music Low` / `Music High` 这三条写的是 **0.0 dB**，Unity 有可能**不落**显式的 0 项
（`m_FloatValues` 只存相对默认有覆盖的项）⇒ `grep -c "m_FloatValues: {}"` 可能是 3 而不是 0。
**这不是失败**：判据是 `VerifySnapshotValues` / `AudioSnapshotCheck` 的**读回**（走
`GetValueForVolume`，能区分「显式 0」与「没有」），而不是 YAML 文本形状。
