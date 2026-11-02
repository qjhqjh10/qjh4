# B117-WSOUNDCOOL — `AudioCue.timeToPlayAgain` 冷却收成一处实现

执行代理 `B117-WSOUNDCOOL` · 2026-11-02 · 战斗域（音效）

---

## 一、结论（一句话）

`timeToPlayAgain` 的闸现在**只有一处实现**：`WarpforgeVFX/Runtime/WFSoundPlayer.cs` 的
`CanPlayByCooldown(cue)`；`BattleSfx`（按**字段名**）与 `TapHitMaterialDefinition`（按 **cue**）那两份副本已删。
⇒ `账/音频_语音.tsv:3`（`AudioCue.ConfigAudioSource` · 状态 **A** · 落点 `WFSoundPlayer.cs:97`）**现在名副其实**。

---

## 二、判据（第一权威，逐行实读）

| 出处 | 内容 |
|---|---|
| `D:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21` | `if ((Time.time - *(float*)(p+0x34) < *(float*)(p+0x30)) && (*(float*)(p+0x34) != 0.0)) return 0;` ⇒ 窗口内**且之前真播过** ⇒ 不播 |
| `AudioCue__ConfigAudioSource.c:31` | `*(float*)(param_1 + 0x34) = extraout_XMM0_Da;` —— **先写戳**，之后（`:35` 起）才 `Random.Range` 挑 clip ⇒ **挑不出 clip 也算「这次播过」** |
| `AudioCue__CanPlay.c` | 同一道式子的独立等价体（只判、不播） |
| `AudioCue__OnEnable.c` | `lastTimePlayed = 0f` ⇒ **0 = 从没播过**（这就是 `!= 0` 那半句的来由） |
| `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:257` | 唯一调用点族：`SoundManager.Play(AudioCue, …)` 里 `if (!((AudioCue)position).ConfigAudioSource(ref audioSource2))` ⇒ 原版**所有按 cue 播**的路径（`Play2D` / `Play3D` / `Play2DDelayed` / `PlaySoundOnTime.Update`）都过它 |
| 偏移 `+0x30` / `+0x34` | `timeToPlayAgain` / `lastTimePlayed`（`AudioCue` 的实例字段 ⇒ **键 = cue 资产实例**，不是 cue 名、更不是调用方字段名） |

---

## 三、三处各改了什么

### 3.1 `WarpforgeVFX/Runtime/WFSoundPlayer.cs`（**新增唯一实现**）

- `Play(WFSoundCue cue, Vector3, bool)`：`Played++` 之后、`!Enabled` 之前插入闸
  ```csharp
  LastCueThrottled = false;
  if (cue == null) return null;
  if (!CanPlayByCooldown(cue)) { Throttled++; LastCueThrottled = true; return null; }
  if (!Enabled) return null;
  ```
  · 闸排在 `!Enabled` **之外** —— 批处理自检关掉出声时仍照旧冷却（与改前 `BattleSfx` 那份一致，也是 `B117_G10_12` A2 能过的前提）。
  · `CanPlayByCooldown` 排 `PickClip` **之前** —— 复刻 `ConfigAudioSource.c:31` 的「先写戳再挑 clip」。
- 类尾追加（**新成员全部在类尾**，`WFSoundPlayer.cs:37` / `:97` 两个账落点逐字未动）：
  `_lastAt`（`Dictionary<WFSoundCue,float>`，按**引用身份**计）· `Throttled` · `LastCueThrottled` ·
  `CanPlayByCooldown` · `ResetThrottle()`。
- ⚠️ `Play(AudioClip, …)` 重载**没有**也不该有这道闸 —— 原版 `SoundManager.Play2D(AudioClip)` 同样不经过 `AudioCue`。

### 3.2 `CardPresentation/Battle/BattleSfx.cs`（删副本，改「观察」）

- 删 `DrainOne` 里的闸（原 `:304-315`）：按 **`p.field`（字段名）** 计时的 `_lastAt` + `Time.time` 比较。
- 改成就地观察：`WFSoundPlayer.Play(...)` 返回 `null` 且 `LastCueThrottled` 为真 ⇒ `Throttled++`。
- 删字段 `_lastAt`（原 `:139-140`），`ResetCounters()` 改为 `WarpforgeVFX.WFSoundPlayer.ResetThrottle()`
  （原契约本来就写着「清节流表」，`B117_G10_12` 的 A5 靠它）。
- **行数守恒**：改前后都是 **470 行 / 470 个 LF**；6 个账落点（`:25 / :78 / :148 / :174 / :267 / :295`）逐字未动。

### 3.3 `CardPresentation/Battle/TapHitMaterialDefinition.cs`（删副本）

- 删 `CanPlayCue(cue)`（15 行）与 `LastCuePlayed` 表、`ClearCache()` 里那句 `LastCuePlayed.Clear()`；
  `PlayParticle` 的第 ⑤ 步改为直接 `WFSoundPlayer.Play(cue, position, is2d: false)`。
- 文件头那条**已过期**的说明（「`WFSoundPlayer.Play` **没有**这层冷却 ⇒ 本类补上」）就地改成订正版（铁律 5）。
- **行数守恒**：改前后都是 **235 行 / 235 个 LF**；12 个账落点（`:20 / :42 / :84 / :92 / :95 / :164 / :169 / :191 / :200 / :201 / :205 / :212`）逐字未动
  ⇒ 删除处用**等长注释块**占位（块内明确写着「这是行号占位，⛔ 别删」）。

---

## 四、删副本后语义为什么不变

| 维度 | 改前 `BattleSfx` 副本 | 改前 `TapHit` 副本 | 改后（`WFSoundPlayer`） |
|---|---|---|---|
| 键 | **字段名** | cue | **cue**（= 原版 `+0x34` 在 cue 上） |
| 时钟 | `Time.time` | `Time.time` | `Time.time` |
| 公式 | `TryGet && now-last < ttp`（**缺** `last != 0`） | `TryGet && last != 0 && now-last < ttp` | `TryGet && last != 0 && now-last < ttp`（**与原版逐字同形**） |
| 写戳 | 在调用 `Play` 之前 | 在调用 `Play` 之前 | 在 `PickClip` 之前（原版次序） |

- **`TapHit` ⇒ 零行为变化**：它本来就是「cue 解得出就调 `WFSoundPlayer.Play(cue, …)`」，
  闸挪进 `Play` 之后仍在**同一时刻、同一条 cue、同一个公式**上判。
- **`BattleSfx` ⇒ 一处**有意的**语义修正**（这正是删它的理由）：旧写法按字段名分，
  两个字段指向**同一条 cue** 时各算各的冷却；原版按 cue ⇒ 一起冷却。
  本 build 的 `battle_sounds.json` 里两族 cue 名实测**零交集**、字段与 cue 一对一
  ⇒ **当前数据下两者逐位等价**，差异只在「将来出现两字段共用一条 cue」时。
- 出队侧三件与时序相关的行为**没动**：照样出队、照样 `LogAdd(field, null, 0)`（`Log` 是**出队**日志）、
  照样 `ScheduleNext(0f)`（⛔ 未改成别的值；`ScheduleNext(0)` 会改出队节奏这条已在代码注释里写死）。

---

## 五、验证

- **秒级类型检查**（`TMPDIR=/tmp/wf_wsoundcool bash d:/4/Unity/工具/typecheck.sh`）：
  **运行时程序集 0 错误**（三个被改的 `.cs` 都在运行时程序集）。
  编辑器程序集报了 **4 条**，全部在 `Assets/RuleEngine/Editor/RuleEngineTest_B117DROPPOD.cs`
  （`CS1061 UnitState 未包含 ApplyDropPodOnSummon`）—— **不是本代理的文件**（另一个执行代理的活），
  按简报「错误全部集中在不是你负责的文件 ⇒ 记录后重跑」处置，**未去改它**。
  ⚠️ 重跑后的结果见第六节。
- **断言**：新建 `Assets/CardPresentation/Editor/B117_WSOUNDCOOL_Assert.cs`，`Run()` 共 **7 条**：
  `A0`（硬前提 `Time.time > 0`）· `A1`（窗口内第二次不播，`Throttled` +1）· `A2`（**灭自证**：从没播过的 cue 第一次不许被拦，
  `ttl = 1e9` ⇒ 「把 `last` 初值当 0」的写法必红）· `A3`（窗口过期能播）· `A4`（`ttl == 0` 永不拦）·
  `A5`（**先写戳再挑 clip**：挑不出 clip 也算播过）· `A6`（键 = cue，不是全局一把闸）。
  挂法写在文件头（由调度台接，本代理**不改任何 `Editor/*Scene.cs`**）。
- ⚠️ **批处理里 `Time.time` 不前进** ⇒ 「等窗口过期」用「把 `timeToPlayAgain` 压到 0」表达（闸只比 `now - last < ttp`，判据同一形状）。

---

## 六、时序 / 风险

1. **冷却触发的时刻晚了「一拍」**（预期内、且更贴原版）：`BattleSfx` 的闸在**出队**那一刻，
   `WFSoundPlayer` 的闸在**真播**那一刻 —— 两者在 `DrainOne` 里是**同一句**（`Play` 紧跟在后面），没有中间可插入的其它播放。
   但**在 `BattleSfx` 之外**调用 `WFSoundPlayer.Play(cue, …)` 的路径（`AnimFXController` / `WarpforgeEffectPlayer` /
   `CardView` / `DraggableController` / `AttackSelector` / `BattleDriver` / `NetMatchmaking` / `WindowSounds` … 共 12 处），
   现在**会**与战斗音效共享同一条 cue 的冷却 —— **这正是原版形状**（原版 `SoundManager.Play(AudioCue)` 一个口全过），
   但它们**改前不受任何冷却** ⇒ 这是本批**除 `BattleSfx` 之外**最大的一处行为变化。
2. **`BattleSfx.Played` 与 `WFSoundPlayer.Played` 语义未动**：`Played++` 仍在闸**之前**（= 请求次数，含被挡的），
   所有既有读它增量的断言（`AttackSelector` / `B117_G10_10b` / `B117_G10_24`）不受影响。
3. **`BattleSfx.Throttled` 现在只统计「经本类出队、且被 `WFSoundPlayer` 挡掉」的次数**（与改前同名同义）；
   `WFSoundPlayer.Throttled` 是**全局**计（含其它路径），两者别混用。
4. **`ResetCounters()` 会清全局冷却表**（`WFSoundPlayer.ResetThrottle()`）—— 这是它原本就声明的契约（「清节流表」），
   但副作用面从「BattleSfx 私有表」扩大到「全项目冷却表」；只在自检里调，生产路径不调。
5. 🔴 **`Time.time == 0` 的边界**：原版 `&& (last != 0)` 哨兵在「`Time.time` 恰为 0 时的第一次播放」下会**写进 0**，
   于是此后仍视作「从没播过」⇒ 不冷却。本实现**照抄**这个行为（铁律 11 / §18·0）。断言 `A0` 把这条写成**硬前提**，
   前提不成立就当场红（⛔ 不是静默跳过）。

---

## 七、顺手发现（⛔ 未改，超出本代理白名单）

同一道闸在**另外两处**还各有一份**独立**实现（都与本批收口的那三处**互不影响**）：

| 位置 | 键 | 时钟 | 备注 |
|---|---|---|---|
| `CardPresentation/Battle/RemnantSfx.cs:130-196` | `key`（档名） | `Time.time` | `BattleScene.cs:16992` 有一条活断言读它的 `Throttled` |
| `CardPresentation/Shell/WindowSounds.cs:169-212` | `cueName`（字符串） | **`Time.realtimeSinceStartup`** | `B97_SHOP_Assert.cs:133` 读它的 `Throttled` |
| `CardPresentation/Core/WarpforgeAudio.cs:150-176`（`PlayJingle`） | **无冷却** | — | 壳侧两条 `MixerType.Jingles`，原版同样过 `SoundManager.Play(AudioCue)` ⇒ 缺闸 |

⚠️ `WindowSounds` 用 `Time.realtimeSinceStartup`、原版用 `Time.time` ⇒ 那一份的**时钟本身**与原版不同。
⇒ 建议另开一条账（优先级由调度台定）：**三处都应改成调 `WFSoundPlayer`/`WarpforgeAudio` 那唯一一层**，
但 `RemnantSfx` / `WindowSounds` 的 `Throttled` 读口已被现有断言引用，改动要连断言一起动
⇒ 不在本批（本批白名单只到 `WFSoundPlayer` / `BattleSfx` / `TapHitMaterialDefinition`）。

另：`WarpforgeAudio.PlayJingle` 走 **Jingles** mixer 组，不能直接复用 `WFSoundPlayer.Play`（它写死挂 `FxGroup`）；
若要给它补闸，应把 `CanPlayByCooldown` 提成 `internal` 供其调用（⛔ 别在 `WarpforgeAudio` 里再写一遍公式）。

---

## 八、改动的文件清单

| 文件 | 性质 |
|---|---|
| `Unity/MyGame/Assets/WarpforgeVFX/Runtime/WFSoundPlayer.cs` | 改（新增唯一实现 + 类尾成员） |
| `Unity/MyGame/Assets/CardPresentation/Battle/BattleSfx.cs` | 改（删副本、改观察、清表改调 `ResetThrottle`） |
| `Unity/MyGame/Assets/CardPresentation/Battle/TapHitMaterialDefinition.cs` | 改（删副本 + 文件头订正） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B117_WSOUNDCOOL_Assert.cs` | **新建**（7 条断言） |
| `Unity/资料/完全复刻/判据/B117-WSOUNDCOOL_音效冷却.md` | **新建**（本文件） |

⛔ 未动：账主表 · `Battle/BattleDriver.cs` · `Core/CardView.cs` · `Editor/*Scene.cs` · `d:/2` · git。
