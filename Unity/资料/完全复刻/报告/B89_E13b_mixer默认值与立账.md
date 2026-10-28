# B89_E13b — mixer 默认值（`Master`）与三条立账

> 执行代理 `E13`（转岗第二棒）· 2026-10-28 · 域 `音频_语音`
> 前置：`B89_E13`（`报告/B89_E13_mixer快照取值.md`，6/6 原版快照值已查到并写入）
> 本批交付：`Editor/AudioSetup.cs` 追加 `SetMixerDefaults()`（+ 只读 `VerifyMixerDefaults()`）
> · `Editor/AudioSnapshotCheck.cs` **加 1 条断言**（9 → 11 条，含 E13 那条共加 2）·
> `账/_parts/B89_E13b.tsv`（3 行）

---

## §0 一句话

`SetMixerDefaults()` **建了**；把原版 `Main Mixer` 起始快照的 `Master = −0.16685187816619873 dB`
写进我们**每一条**快照（6 条 × 1 组 = 6 格）。
🔴 **换算用的是「直接写 dB」——不是 `10^(dB/20)`**：派活简报说的「组存线性 0–1」**不成立**，反证三条见 §1.1。

---

## §1 `SetMixerDefaults()` 做了什么

### §1.1 🔴 单位：**dB，不是线性 0–1**（三条反证）

派活包写的是「`AudioMixerGroupController` 存的是**线性 0–1**，dB → 线性 = `10^(dB/20)`」。
**这条不成立**，所以本入口**直接写 dB**。反证：

1. **同一个 `values` 数组里既装音量又装 pitch**。原版 `m_MixerConstant.snapshots[i].values`：
   音量那几位是 `0.0` / `−80.0`，pitch 那几位是 `1.0`。
   **线性增益不可能出现负数**（−80 只能是 dB）⇒ 音量位 = dB。
2. **编辑器侧 `AudioMixerGroupController` 在本 Unity 版本里根本没有 `volume` 属性。**
   `ilspycmd` 反编译 `Unity6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEditor.CoreModule.dll`
   得到的成员只有 `GetGUIDForVolume()` · `GetValueForVolume(controller, snapshot)` ·
   `SetValueForVolume(controller, snapshot, float)` ⇒ 它读写的就是上面那个 `values` 的同一格
   ⇒ **同一个单位 = dB**。
3. **「线性 0–1」是另一个 API 的口径**：`UnityEngine.Audio.AudioMixerGroup.volume`（**运行时**类型）
   才是线性；与本入口用的编辑器侧 `SetValueForVolume` 不是同一个东西。

**反证的实际后果**（为什么必须写对）：若按线性写 `10^(−0.16685/20) = 0.9810`，落进资产会被当
**+0.98 dB** 读出来 —— 比原版**高 1.15 dB**、而且方向与自己想减的方向相反。
本入口写完会 `GetValueForVolume` 读回比对（容差 0.001 dB），写错会**立刻报红**，不会静默。

### §1.2 写进哪儿（🔶 一处投影）

| 写的东西 | 我们 mixer 里的格 | 值（原版） | 出处 |
|---|---|---|---|
| `Master` 组音量 | **每一条**快照（6 条） | **−0.16685188 dB** | `AudioMixerController_4889565729752889476.json` 的 `m_MixerConstant.snapshots[0].values[0]` = `-0.16685187816619873` |

原版另 4 个组（`FX` / `Music` / `Voices` / `Jingles`）起始值都是 **0.0 dB**，与我们现状相同 ⇒ 不用写。

**🔶 自己设计（非原版）：写进每条快照，而不是「组默认值」。**
原版那条值在 `Main Mixer` 的**唯一一条**快照 `Snapshot` 里（Music/FX 两个子 mixer 的 6 条快照
根本不碰 Main Mixer 的 `Master`）；我们的 mixer 有 **6 条**快照，且**没有「组默认值」这一格**
（见 §1.1 反证 2：组音量只按快照存）⇒ 只能往 6 条里各写一份。
效果等价：切到哪条快照，`Master` 都是 −0.16685188 dB。· 调度台 2026-10-28 转岗（派活包 §2）。

**⚠️ 与音量滑块不冲突**：`Master` 组在本 mixer 里**没有被暴露**
（`m_ExposedParameters` 只有 `VolumeFX` / `VolumeMusic` / `VolumeVoices` / `VolumeJingles`），
运行时 `WarpforgeAudio.ApplyAll`（`Core/WarpforgeAudio.cs:67`）也不写它
⇒ **这一步完全没碰暴露参数**，不会覆盖任何滑块设过的值。

### §1.3 与 `SetSnapshotValues()` 的关系（**写的是不相交的格**）

| 入口 | 写的格 |
|---|---|
| `SetSnapshotValues`（E13） | `SFX *` → `FX` 组 · `Music *` → `Music` 组 |
| `SetMixerDefaults`（E13b） | **全部 6 条快照** 的 `Master` 组 |

⇒ 两组格**没有交集**，**跑的顺序无关、互不覆盖**；两个入口都幂等（覆盖写字）。

### §1.4 幂等 / 落盘

* `SetValueForVolume` 是覆盖写字 ⇒ 同值重写结果不变。
* 写后 `EditorUtility.SetDirty(controller + 全部快照)` → `AssetDatabase.SaveAssets()`（native 写不存不落盘）。
* **⛔ 全程没有碰 `Resources/Audio/Main Mixer.mixer` 一个字节**（只经 Unity 内部 API 写）。

---

## §2 三条立账 —— 以及一个**口径冲突**（如实报）

派活要求「写进补丁的新行、`状态=C`」。**做不到把这三条记成 C 行**，原因如下（不是偷懒，是账的 schema）：

* 账的**主键是 `(原版类, 方法)`**，`方法` 必须是**单个原版方法的签名**（`merge_ledger_parts.py`
  第 64–78 行会拒掉不合规的行），而**全仓账的 `原版类` 清一色是游戏侧类**（`Assembly-CSharp`）。
* 但这三条**都是「原版 mixer 资产里的数据/结构」**，**没有任何一个游戏侧方法拥有它** ——
  效果链、起始快照的 Master 值、暴露参数，都是 `.mixer` 资产的序列化内容（Unity 编辑器类型
  `AudioMixerController` 的数据，`AudioMixerController` 不是游戏类）。
  ⇒ 若硬要新 key，只能**编一个原版类**（例如把 `AudioMixerController.CreateDefaultAsset(string path)`
  当 key），那是**假归因**，比不记账更糟（铁律 2 / 「判据与资产冲突 ⇒ 停下给反证」）。
* 另一个原因是：三条最贴切的 3 个方法在账里**都已经是 `A`**（
  `AudioMixerAddresableReferences.GetMixerGroup` · `SoundManager.SetMixerVolume` ·
  `AudioMixerAddresableReferences.GetSnapshot`）。用同 key 写 C 会**把已复刻的方法降级成未复刻** —— 也是假账。

**所以三条写成「现有 A 行的备注」，并在备注里用固定标签 `🔴 C 级待办（资产侧，未复刻）` 标出**
（可 `grep "C 级待办（资产侧" 账/*.tsv` 一把捞出）：

| # | 记在哪一行 | 内容 |
|---|---|---|
| ① | `AudioMixerAddresableReferences` / `GetMixerGroup(MixerType mixerType)` | **mixer 效果链完全没复刻**（详见 §3.1） |
| ② | `AudioMixerAddresableReferences` / `GetSnapshot(SnapshotsType snapshotType)` | **原版 Main 起始快照 `Master` = −0.16685 dB、我们 0**（本批已补，见 §1） |
| ③ | `SoundManager` / `SetMixerVolume(MixerType mixerType, float volume)` | **原版暴露参数 5 条（含 `Master`）、我们 4 条**（详见 §3.2） |
| 附 | `GetMixerGroup` 那一行的备注末尾 | **「切快照抢滑块」的根治方案**（详见 §4） |

⚠️ **如果调度台坚持要它们是独立 C 行**，需要先定一个「资产键」约定（例如允许
`原版类 = 资产名`（`Main Mixer`）+ `方法 = 字段路径`）。我没有自创 —— 那会开一个全仓新形态。

---

## §3 三条立账的判据（都在原版解包资源里，可复核）

### §3.1 ① mixer 效果链完全没复刻

判据 = `assets_full/bundle_audiocontrol_assets_all/AudioMixerController/AudioMixerController_4889565729752889476.json`
的 `m_MixerConstant.effects[]`（**9 条**）+ `AudioMixerController_-3989599372716181902.json`（**2 条**）：

| mixer | effects | 说明 |
|---|---|---|
| **Main Mixer** | 9 条 | 每个内容组一条 `type: -2`（与我们的 `Attenuation` 同位、`parameterIndices` 空）；`FX` 与 `Music` 各多一条 **`type: -5`、带 7 个自动化参数**（`parameterIndices` 10..16 / 17..23）；`FX`/`Music`/`Voices`/`Jingles` 另有 **`type: -3` 的 send**（`sendTargetEffectIndex` 非 −1、`wetMixLevelIndex` = 24/25/26） |
| **FX** | 2 条 | `type: -2` + `type: -4`；`numSideChainBuffers: 1` |

我们现读 `Main Mixer.mixer`：**每组只有一条 `Attenuation`**，没有任何 `type: -5` / `-3` / `-4`。

⚠️ **效果名查不到**：`AudioMixerEffectController` 对象**没有被导出**（`assets_full` 下没有
`AudioMixerEffectController/` 目录），`pluginEffectNameBuffer: [0]` 为空 ⇒ 只能读到 `type` 的
**数值**，⛔ **别按 type 号编名字**（要定名得回二进制或编辑器），本报告一律只写数值。

### §3.2 ③ 暴露参数 5 条 vs 我们 4 条

判据 = `AudioMixerController_4889565729752889476.json` 的 `m_MixerConstant`：
`exposedParameterNames` 5 个 hash · `exposedParameterIndices = [2, 8, 0, 4, 6]`。
按 `groups[]` 的 `volumeIndex` 解（`Master` 0/1 · `FX` 2/3 · `Music` 4/5 · `Voices` 6/7 · `Jingles` 8/9）：

| index | 组 |
|---|---|
| 2 | `FX` |
| 8 | `Jingles` |
| **0** | **`Master`** |
| 4 | `Music` |
| 6 | `Voices` |

⇒ 原版暴露 = `VolumeFX` · `VolumeJingles` · **`VolumeMaster`** · `VolumeMusic` · `VolumeVoices`。
我们 `m_ExposedParameters` 只有 4 条（丢的是 `Master`）。

* `MixerType`（`dump.cs:17939-17950`）= `FX=0 Music=1 Voices=2 Jingles=3 BattleGroupLow=4
  BattleGroupMedium=5 BattleGroupHigh=6` —— **没有 Master** ⇒ 按 `"Volume"+枚举名` 拼名判定，
  全工程**没有 `VolumeMaster` 的写入点**。
  ⚠️ **但「搜不到字面量」不是否定证据**（名字是拼出来的）⇒ 记「**还没查清**：原版那根 Master
  参数到底有没有 UI 在写」。
* 顺带：原版 `Music` mixer 暴露 3 条（`exposedParameterIndices = [8,4,6]` = `BattleGroupHigh/Low/Medium`
  音量），正好对应 `MixerType.BattleGroup*` 三档；但 `SoundManager.mixer` 指向的是 **Main Mixer**
  （SO 的 `AudioMixer` 字段 = pathID 4889565729752889476）⇒ 在原版里
  `SetMixerVolume(BattleGroupLow/Medium/High, …)` 是打到 Main Mixer 上**不存在的参数名**
  （`SetFloat` 返 false 的空操作）。谁真正写 Music mixer 那 3 条，本机没查到 ⇒ 记「还没查清」。

### §3.3 ② Main 起始快照的 `Master`

判据 = 同文件的 `m_MixerConstant.snapshots[0].values[0] = -0.16685187816619873`（dB）。
我们原来 0 dB ⇒ 整体响度比原版高 **0.17 dB**（听不出，但它是原版值，见 `B89_E13` 报告 §4 第 4 条）。
**本批已补**（§1）。

---

## §4 「切快照会抢音量滑块」—— 三段说清

派活要求写清 ① 现状为什么抢 ② 原版为什么不抢 ③ 根治方案要立账。三条都在下面。

### §4.1 现状为什么会抢

我们只有 **1 份** mixer，`m_ExposedParameters` 里的 `VolumeFX` / `VolumeMusic` 的 **guid 就是**
该组的 `m_Volume`（现读 `Main Mixer.mixer`）：

```
- guid: eec8383b82a5828469552ee45332ccb7   name: VolumeFX      ← FX.m_Volume    同一个 guid
- guid: 894f6eda8190955459e1650c10422af1   name: VolumeMusic   ← Music.m_Volume  同一个 guid
```

而快照的 `m_FloatValues` 也是**按参数 guid** 存的（Unity 的 snapshot 就是「一组参数 guid → 值」）。
⇒ **滑块（`AudioMixer.SetFloat`）与快照（`TransitionTo`）写的是同一根浮点**。
`WarpforgeAudio.Write`（`Core/WarpforgeAudio.cs:128`）把线性 0–1 转 dB 后 `SetFloat`；
`ToggleSFX` 随后 `TransitionTo` 到 `SFX Off`（`FX = −80 dB`）⇒ **覆盖掉滑块设过的值**；
再切回 `SFX On`（`FX = 0 dB`）⇒ 回到 **0 dB 而不是用户设的 −6 dB**。

⚠️ **缓解事实**：`WarpforgeAudio.ToggleSFX` 今天**零调用方**
（`grep -rn "ToggleSFX" Assets` 只命中 `Core/WarpforgeAudio.cs` 自身 + 两个 Editor 文件）
⇒ 现在还没有线上回归；**接线（做设置页那根 SFX 开关键）时才暴露**。

### §4.2 原版为什么不抢

原版是 **3 份 mixer**，两根量**天然分层**：

| | 谁写 | 写在哪 |
|---|---|---|
| **音量滑块** | `SoundManager.SetMixerVolume` → `AudioMixer.SetFloat("Volume"+枚举名, dB)` | **Main Mixer** 的组（`exposedParameterIndices` = FX/Jingles/Master/Music/Voices） |
| **快照** | `snapshot.TransitionTo(t)` | **Music 子 mixer** 的 `BattleGroupLow/Medium/High` · **FX 子 mixer** 的 `Master` |

两套参数在**不同的 mixer 资产**里，guid 不同 ⇒ 互不影响。
（这也是原版「关 SFX」为什么是打 FX **子** mixer 的 `Master` 而不是打 `VolumeFX`。）

### §4.3 根治方案（**立账，⛔ 本批没动结构**）

给 `FX` / `Music` 各加一个**子组**（如 `FX Content` / `Music Content`）：

1. mixer 结构：`FX` → 子组 `FX Content`，`Music` → 子组 `Music Content`（`AudioMixerController.AddChildToParent`）；
2. 运行时路由：所有音源从 `FindMatchingGroups("FX")[0]` / `FindMatchingGroups("Music")[0]`
   改成挂到子组（`Core/WarpforgeAudio.cs:78/86`，`Core/` 那几处 `RouteEffectSounds`）；
   ⚠️ `FindMatchingGroups` 是**子串匹配** ⇒ 加了 `FX Content` 之后 `FindMatchingGroups("FX")`
   会返回 2 个（父 + 子）⇒ 这一步必须与路由改动**同批**做，否则会静默改错组；
3. 快照只写子组（`SetSnapshotValues` 的 `SnapshotValuesTable` 把组名改成 `FX Content` / `Music Content`）。

**为什么不本批做**：它要改运行时路由（`Core/WarpforgeAudio.cs`、`WarpforgeVFX` 的 `FxGroup`、
`Battle/BattleMusic.cs`、`Shell/ShellRuntime.cs` 都读这些组），属于**跨面改动**，
且会改变所有音效的挂组方式 ⇒ 需要**单独一批 + 用户拍板**（派活包明确「⛔ 别自己改 mixer 结构」）。

---

## §5 typecheck 读数

`TMPDIR=/tmp/wf_B89E13b bash d:/4/Unity/工具/typecheck.sh`

⚠️ **本批 4 次采样全部被「运行时节」的别人在写的文件挡住**，编辑器程序集因此**根本编不起来**
（`error CS0006: 未能找到元数据文件 wfcheck/WFCheck.dll` —— 运行时程序集失败时编辑器那份不会被编译），
所以**我的两个文件在本轮一次也没被真正编到**。逐次读数：

| 次 | 运行时程序集 | 编辑器程序集 | 报错落在哪（全部白名单外） |
|---|---|---|---|
| 1 | 1 | CS0006 | `RuleEngine/Core/BattleContext.cs:2350`（`MatchTypes.ResolvingExtrinsic` 静态类里放了实例成员） |
| 2 | 1 | CS0006 | 同上 |
| 3 | 1 | CS0006 | 同上 |
| 4 | 3 | CS0006 | `Core/TMProWarpText.cs:148,149`（`Bounds.extent` 不存在）· `Shell/BaseEventPopup.cs:558`（`LiveOpsLocalCatalog.GetAssetReference` 不存在） |

**这几行的每一次都不一样**（行号/文件在变）= 别人正在写。按简报「错误全部集中在白名单之外 ⇒
别人的活，写报告别去改」处理。

**第 ③ 条兜底验法（别人一直挡着时用的）**：`TMPDIR=/tmp/wf_B89E13 D:/2/Warpforge_tools/py312/python.exe
d:/4/Unity/工具/gen_csc_rsp.py` 重生成 rsp（含当前全部源码）→ 直接 `csc @wf_csc_editor.rsp`
对着**上一次成功编译出来的** `WFCheck.dll`（06:56 那份）编编辑器程序集。
读数：报错全是 `CS0117/CS1061「类型 X 不包含成员 Y」`（= 那份 DLL 是旧的，别人后加的运行时候成员不在里面），
**`grep "AudioSetup.cs|AudioSnapshotCheck.cs"` = 0 条** ⇒ 我的两个文件在这个口径下也**零错误**。

**✅ 第 5 次（收尾）：`运行时错误数: 0` / `编辑器错误数: 0` —— 全绿**（别人那几个文件在此期间修完了），
**这一次我的两个文件是真正被编到的**（编辑器程序集全量编译）⇒ §1 的代码**已过类型检查**。

---

## §6 调度台要跑的命令与期望读数

### ① 补 `Master` 默认值（新，一次性；幂等）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod AudioSetup.SetMixerDefaults -logFile -
```

期望日志：

```
AUD-SVD 快照 `SFX On` / 组 `Master` = -0.16685 dB（原版 −0.16685188 dB）
AUD-SVD 快照 `SFX Off` / 组 `Master` = -0.16685 dB
AUD-SVD 快照 `Music Low` / 组 `Master` = -0.16685 dB
AUD-SVD 快照 `Music Medium` / 组 `Master` = -0.16685 dB
AUD-SVD 快照 `Music High` / 组 `Master` = -0.16685 dB
AUD-SVD 快照 `Music None` / 组 `Master` = -0.16685 dB
AUD-SVD **6 条快照 × 1 个组 = 6 格**
AUD-SVD === 合计：6 通过 / 0 失败 ===
```

⚠️ 若读回是 `0.98100` 一类的值 ⇒ 那说明 `SetValueForVolume` 真的做了线性换算（**与 §1.1 三条反证矛盾**）
⇒ 请**停下**把读数发回来，别自己改常数。

### ② 只读复核（新）

```bash
… -executeMethod AudioSetup.VerifyMixerDefaults -logFile -
```

期望：`AUD-SVD-V` 6 行全 `✓`，合计 `6 通过 / 0 失败`。

### ③ 现有自检（**条数变了：9 → 11**）

```bash
… -executeMethod AudioSnapshotCheck.Run -logFile -
```

期望 **11 通过 / 0 失败**。本批加的 2 条是：
* `6 条快照的**组音量**已按原版写进去`（E13，`SetSnapshotValues` 之后就绿）
* `` `Master` 组的默认音量 = 原版 −0.16685188 dB ``（E13b，**`SetMixerDefaults` 之前会红**）

⚠️ 所以：**只跑了 `SetSnapshotValues` 时会是 10 通过 / 1 失败**，那条**本来就该红**。
（`_run_8_checks.sh:58` 已含本宿主，脚本不用改。）

### ④ 顺序

`SetSnapshotValues` 与 `SetMixerDefaults` **写的是不相交的格**（`FX`/`Music` vs `Master`）
⇒ **顺序无关**。但两条都在 `AudioSnapshotCheck.Run` 之前跑完才能拿到 11/0。

### ⑤ 账补丁合并顺序（**有重叠，必须说清**）

* `B89_E13.tsv` 有 2 行：`GetSnapshot` · `ToggleSFX`
* `B89_E13b.tsv` 有 3 行：`GetMixerGroup` · `SetMixerVolume` · `GetSnapshot`
* 🔴 **`GetSnapshot` 在两份补丁里都有** —— `B89_E13b` 的那一行是 `B89_E13` 那行的**超集**
  （含 E13 全部正文 + E13b 新增）。`check_patch_conflicts.py` 现读显示 `B89_E13b` **胜**。
  ⇒ **按 `--only B89_E13` → `--only B89_E13b` 的顺序合并**（或直接按文件名序，结果一致），
  ⛔ 别反过来（反了会把 E13b 的新增冲掉）。
