# B117-WCOOL3 — 剩余三处音效冷却收口

执行代理 `B117-WCOOL3` · 2026-11-02 · 任务 `B117-R68`

> 起因：`B117-WSOUNDCOOL`（同日）把 FX cue 的 `timeToPlayAgain` 冷却收成
> `WarpforgeVFX.WFSoundPlayer.CanPlayByCooldown` 一处之后，**还有三处各自独立、且都漏了**：
> 两处自己写了一份式子（其中一处连 `last != 0` 那半句都缺，另一处时钟口径不对），
> 第三处（Jingles）**一行冷却都没有**。

---

## 一、三处各改了什么

公式的**唯一实现** = `WarpforgeVFX/Runtime/WFSoundPlayer.cs`：

| 位置 | 内容 |
|---|---|
| `:158` | `internal static bool CanPlayByCooldown(WFSoundCue cue)` —— 原 `private`，**提成 `internal`**；现在只转调下面那条（共用 `_lastAt` 表） |
| `:170` | `internal static bool CanPlayByCooldown<TKey>(Dictionary<TKey,float> table, TKey key, float timeToPlayAgain)` —— **公式的唯一实现**（`now = Time.time`；`last != 0 && now - last < ttp` ⇒ 挡；否则写戳放行） |

> 判据（第一权威）：
> `d:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21`
> （`if ((Time.time - last < timeToPlayAgain) && (last != 0)) return 0;`）
> ＋ `:31` 的「**先写戳、再挑 clip**」次序。
> 调用点族：`SoundManager__Play`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:257`）。

### ① `Battle/RemnantSfx.cs:187-196`（键 = 档名 `Aeldari/collect` …，时钟 `Time.time`）

- 删掉自己那份 `TryGetValue + Time.time - last < ttp` 式子，改调 `WFSoundPlayer.CanPlayByCooldown(_lastAt, key, ttpa)`
  （`:192`，`ttpa = cue?.timeToPlayAgain ?? 0`）。
- **顺带补回了原版那半句 `last != 0`**：改前只要 `Time.time` 还很小（`< ttp`）而表里又没有，
  `TryGetValue` 为假 ⇒ 本来也放行；**但**改前一旦 `_lastAt` 里被写进 `0`（`Time.time == 0` 那一帧播过），
  就会把后续全挡 —— 现在与 `ConfigAudioSource.c:19` 逐字一致。
- **行数守恒**：替换前后同为 10 行，`RemnantSfx` 全文仍 213 行；`_lastAt` / `Throttled` / `ResetThrottle` 一字未动。

### ② `Shell/WindowSounds.cs:200-212`（键 = cue 名）

- 改调 `WFSoundPlayer.CanPlayByCooldown(_lastAt, cueName, cue.timeToPlayAgain)`（`:208`）。
- **时钟从 `Time.realtimeSinceStartup` 改成 `Time.time`**（详见 §二）。
- **行数守恒**：替换前后同为 13 行，全文仍 257 行；`Played` / `Throttled` / `LastCue` / `ResetCounters` 一字未动，
  `_lastAt` 仍是**本类自己那张按 cue 名**的表（⛔ 没有并进 `WFSoundPlayer._lastAt`）⇒ `ResetCounters` 的语义不变。

### ③ `Core/WarpforgeAudio.cs:149-174`（`PlayJingle`，挂 **Jingles** 组）

- 改前：**一行冷却都没有**（直接 `PickClip` → 播）。
- 改后：在 `cue == null` 之后、**`PickClip` 之前**加一道
  `if (!WFSoundPlayer.CanPlayByCooldown(cue)) { JingleThrottled++; return null; }`（`:153`）。
  位置照原版 `ConfigAudioSource.c:31` 的次序 —— **先写戳、再挑 clip**（挑不出 clip 也算「这次播过」）。
- 冷却表**与 FX 共用** `WFSoundPlayer._lastAt`（键 = `WFSoundCue` 资产实例）。判据：
  原版 `lastTimePlayed` 就住在 `AudioCue` 上（`+0x34`，`AudioCue__OnEnable.c` 写回 0），
  **与从哪条 `Play2D` 路进来无关** ⇒ `Play2D(cue, FX)` 与 `Play2D(cue, Jingles)` 共用同一条冷却。
- 新增读数口（**类尾追加**，`:416` / `:419`）：`public static int JingleThrottled` · `public static void ResetJingleThrottle()`。
- **行数守恒**：`PlayJingle` 仍是 26 行（改的是 151-154 这 4 行内部，未增删行）⇒
  下游账落点 `:195-199 (SetVoiceOver)` · `:286 (GetSnapshot)` · `:309 (ToggleSFX)` **全部未漂**（已逐条 `sed -n` 现读核对）。
  ⚠️ 为了让这 4 行装下「冷却 + 挑 clip + clip 空 + Enabled」，
  `var clip = …PickClip(…);` 与 `if (clip == null || !Enabled) return null;` 现在在**同一物理行**上（语义与改前等价：
  先判挑不到、再判 `Enabled`，`||` 短路）。

### 为什么不用 `WFSoundPlayer.Play` 播 Jingles

`WFSoundPlayer` 的音源**写死 `FxGroup`**（它服务一整族特效音效）。原版这两条走 `MixerType.Jingles`
（`VolumeJingles` 与 `VolumeFX` 恰好同值 ⇒ 接错了**听不出来**，那正是它接错的保护色）。
⇒ 只借用**冷却判定**，播还是 `WarpforgeAudio.PlayJingle` 自己播（挂 `JinglesGroup`）。

---

## 二、`WindowSounds` 的时钟：**改成 `Time.time`**（已改）

**判据**：原版 `AudioCue__ConfigAudioSource.c:19-21` 判的就是 **`Time.time`**；而
`SoundManager.Play` 是**所有**按 cue 播的路径的唯一入口（`SoundManager.cs:257`）——
FX / Jingles / `Play2D` / `Play3D` / `PlaySoundOnTime` 全从它过。我们那处用的
`Time.realtimeSinceStartup` **没有任何判据支撑**（历史原因；`Time.realtimeSinceStartup` 与 `Time.time`
在 `timeScale != 1` 或暂停时会分叉 ⇒ 它会与原版行为不同）。

**会不会让现有断言红**：不会。

- `Editor/B97_SHOP_Assert.cs:133`（B4/B5）走的是「`ResetCounters()` → 播一次 → 再播一次」，
  两次调用间隔 ≈ 0 s ⇒ `Time.time - last < 0.1` 恒成立 ⇒ 第二次照样被挡、`Throttled == 1`。
  该断言读的 `WindowSounds.Throttled` **值语义一个字没变**。
- 批处理里 `Time.time` 不前进 —— 这与改前的 `realtimeSinceStartup` 表现（也在跑）**不冲突**：
  本文件的 A3/B3/C3「窗口外」用的是**把 ttp 压到 0** 的等价手法（沿用 `B117_WSOUNDCOOL_Assert` 的先例），
  ⛔ 不依赖时钟前进。

---

## 三、typecheck

```
TMPDIR=/tmp/wf_wcool3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⛔ 未跑 Unity 批处理（按简报）。断言文件已写成，**等调度台接宿主**。

---

## 四、断言：`Assets/CardPresentation/Editor/B117_WCOOL3_Assert.cs`（新建，314 行）

`public static void Run()` —— **零前提**（不读存档 / 不写 PlayerPrefs / 不出声：全程 `WFSoundPlayer.Enabled = false`，
夹具 cue 的 `clips` 为空 ⇒ 连音源对象都不会建）。挂法：宿主聚合行**同一物理行**追加
`B117_WCOOL3_Assert.Run(); pass += B117_WCOOL3_Assert.Pass; fail += B117_WCOOL3_Assert.Fail;`

| 段 | 条数 | 内容 |
|---|---|---|
| A `RemnantSfx` | 4 | A0 前提（区间表 ttp > 0）· A1 前提（第一次真播）· **A2 窗口内不播** · **A3 窗口外能播** |
| B `WindowSounds` | 6 | B0 前提 · B1 前提 · **B2 窗口内不播** · **B3 窗口外能播** · B4 走唯一实现（源码面） · B5 时钟 = `Time.time`、无 `realtimeSinceStartup`（源码面，跳过注释行） |
| C `PlayJingle` | 4 | C0 前提（夹具挑不出 clip）· C1 前提 · **C2 窗口内被挡（`JingleThrottled` +1）** · **C3 窗口外能播** |
| D 公用公式 | 6 | D0 前提（反射找得到 `internal` 泛型）· **D1 ★★ 灭自证** · D2 窗口内 · D3 窗口外 · D4 `ttp == 0` 永远放行 |

合计 **20 条**（每处 ≥ 2 条窗口两态，另加 1 条灭自证 + 前提/源码面）。

**灭自证（D1）**：`Dictionary{"never" → 0f}` + 窗口 `float.MaxValue` ⇒ 调 `CanPlayByCooldown` 必须**放行**。
🧨 把原版那半句 `last != 0` 删掉（= 把「表里没有 / 初值 0」当成「刚刚播过」）⇒ `now - 0 < float.MaxValue` 恒真
⇒ 第一次就被拦 ⇒ **D1 红**。三处共用这一个公式 ⇒ **一条灭自证覆盖三处**。
另：A2/B2/C2 各自删掉那处冷却 ⇔ 红；B4 改回自己那份式子 ⇔ 红；B5 改回旧时钟 ⇔ 红。

⚠️ **`internal` 的可见性**：本仓**没有 `InternalsVisibleTo`**（同族先例
`Editor/B108_A6_ReuseShop_Assert.cs:221`、`Editor/B97W34_Assert.cs:26`）⇒ 编辑器程序集看不见
`WFSoundPlayer.CanPlayByCooldown<TKey>`，D 段**用反射调**（`FindGenericCooldown()`，按「泛型定义 + 3 参数」筛）。
这不是绕过判据：D 段断的是**公式本身**，A/B/C 三段断的是**三处真的走了它**。

---

## 五、动没动 `Throttled` 的语义

**没动。** 三处都保留原有读口，值语义一字未改：

| 读口 | 改前 | 改后 |
|---|---|---|
| `RemnantSfx.Throttled` | 被 `timeToPlayAgain` 挡一次 +1 | **同**（`CanPlayByCooldown` 返 false 时才 `Throttled++`） |
| `WindowSounds.Throttled` | 同 | **同**（`ResetCounters` 仍只清本类的 `_lastAt` + `_warned`） |
| `WarpforgeAudio.JingleThrottled` | **原来没有这个读口** | 新增（类尾），只为自检；⛔ 不参与播放决策 |

`WFSoundPlayer.Throttled` / `LastCueThrottled` 也**没有**被 `PlayJingle` 触碰（`BattleSfx.DrainOne` 读它们）。

---

## 六、给调度台的跟进项（本代理不改账）

1. **账落点要重指**（公式搬家了，旧落点现在落在注释上）：
   - `账/音频_语音.tsv:2`（`AudioCue.CanPlay()`）的**等价实现**建议改指
     `WarpforgeVFX/Runtime/WFSoundPlayer.cs:158 / :170`（唯一实现）；
     同行 备注里的「同一口径三处（`WindowSounds.cs:202` · `BattleSfx.cs:174` · `RemnantSfx.cs:187`）」**已过期** ——
     `WindowSounds.cs:202` 与 `RemnantSfx.cs:187` 现在是注释行，`BattleSfx` 那份已由 `B117-WSOUNDCOOL` 删除。
   - `Shell/WindowSounds.cs:184（Play）` 仍是有效落点（行号未漂，已现读确认）。
2. **`PlayJingle` 的冷却与 `WindowSounds` 自己的冷却会叠两层**（Jingles 路径：`WindowSounds.Play` 先按 cue 名挡一次，
   `PlayJingle` 再按 cue 实例挡一次）。原版只有一层（`ConfigAudioSource`）。
   目前不产生可见差异（两者窗口同值、同刻、同键空间），但这是**结构差** —— 建议另记一条账，
   由调度台决定是否把 `WindowSounds` 的 FX/Jingles 那层也交给公用公式后**只留一层**。
   ⚠️ 真要做会动 `Throttled` 的计数时机 ⇒ 属「语义变更」，不是本代理的权限范围。
3. 本文件**没有**接线到任何 `Editor/*Scene.cs`（简报红线）。接哪个宿主由调度台定；
   A 段需要 `Resources/Art/audio/sfx/`（`.gitignore`），取不到时 A1 会红（那是「跑 `工具/import_remnant_sfx.py`」的信号）。

---

## 七、改了哪些文件（全量）

| 文件 | 改动 |
|---|---|
| `Assets/WarpforgeVFX/Runtime/WFSoundPlayer.cs` | `CanPlayByCooldown` 提 `internal` + 新增泛型唯一实现（`:170`） |
| `Assets/CardPresentation/Battle/RemnantSfx.cs` | `:187-196` 转调唯一实现（10 行守恒） |
| `Assets/CardPresentation/Shell/WindowSounds.cs` | `:200-212` 转调唯一实现 + 时钟改 `Time.time`（13 行守恒） |
| `Assets/CardPresentation/Core/WarpforgeAudio.cs` | `PlayJingle` 加冷却闸（4 行内改）＋ 类尾新增 `JingleThrottled` / `ResetJingleThrottle` |
| `Assets/CardPresentation/Editor/B117_WCOOL3_Assert.cs` | **新建**（314 行，20 条断言） |

行尾：五个文件全部 **LF、0 个裸 CR**（已用二进制计数核过，未被翻）。
`git diff --numstat`：`RemnantSfx 10/10` · `WindowSounds 9/9` · `WarpforgeAudio 18/3` · `WFSoundPlayer 70/2`
（后两者的增量全在**类尾**或**函数内部替换**，下游账落点行号未漂）。
