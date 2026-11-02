# B117-FIXA · 音效簇失败修复（2026-11-02）

> 现场 = `_tmp_view/shop.log` / `_tmp_view/battle.log`（本代理现读，未猜）。
> 结论先行：**所有 `Throttled` / 冷却类失败只有一条根因 —— 批处理里 `Time.time` 恒 `0.000`**；
> 另有一条 BT 失败（音高/音量 `0.000`）**不是同一根因**，它是 `Editor/BattleScene.cs` 里的断言取值时机问题，
> 本代理**无权改**那个文件 ⇒ 如实留红（§四）。

---

## 一、根因（一条，横跨三簇）

原版判据 `d:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21`：

```c
if ((Time.time - last < timeToPlayAgain) && (last != 0)) return 0;   // 不许播
// :31  先把 last = Time.time 写下去，再去挑 clip
```

`&& (last != 0)` 里的 **0 = 「从没播过」**（`AudioCue__OnEnable.c` 把它写回 0）。
而 **批处理 `-executeMethod` 没有帧循环 ⇒ `Time.time` 恒 `0.000`**（日志实证：`B117WSOUNDCOOL ✗ A0 … Time.time = 0.000`）。
于是 `last` 被写成 `0`，下一次判 `last != 0` **不成立** ⇒ **每一次播放都被当成「从没播过」** ⇒
**冷却在整个批处理里一秒都挡不住**，`Throttled` 永远是 0。

这不是实现写错（真实播放环境 `Time.time` 从第一帧起就 > 0，行为与原版逐位相同），
是**冷却的时钟口径不适配「无帧循环」的宿主**。

### 改法（两层，都在 `Assets/WarpforgeVFX/Runtime/WFSoundPlayer.cs` 的**类尾**新增）

1. **`WFSoundPlayer.CooldownClock`**（public 只读属性）—— 冷却用的「当前时刻」：
   `ClockOverride >= 0 ? ClockOverride : (Time.time > 0 ? Time.time : Time.realtimeSinceStartup)`。
   · **只在 `Time.time <= 0` 时**回退到 `Time.realtimeSinceStartup`（同一量纲：启动以来的秒数；
     这也是 `WindowSounds` 改回 `Time.time` **之前**一直在用的时钟 —— 它当年在批处理里是能让 B4/B5 变绿的那个）。
   · 真实播放环境 `Time.time` 从第一帧起 `> 0` ⇒ **这条回退永远不走**。
   · 已按铁律 18 打标：`🔶 自己设计（非原版）` + 为什么必须自定 + 谁定的（调度台 2026-11-02 `B117-FIXA`）。
2. **`WFSoundPlayer.ClockOverride`**（public 字段，默认 `-1`）—— **自检注入口**，把时钟钉成显式时刻。
   生产路径**从不**赋值。

`CanPlayByCooldown<TKey>` 里那句 `float now = Time.time;` 改成 `float now = CooldownClock;`（**同一行、行数守恒**，
账里引用的 `WFSoundPlayer.cs:37 / :97 / :111` 全部原位未动）。

> ⛔ 没有动 `last != 0` 那半句（原版语义照抄）；⛔ 没有把任何断言改成恒真或删掉。

---

## 二、每簇的改动

### A · `B117WSOUNDCOOL`（shop 5 条 + battle 5 条）

失败 = `A0 / A1 / A3 / A5 / A6`。`A0` 自己就是**硬前提红**（`Time.time = 0.000`），其余四条是它的下游。

改的是**断言**（`Assets/CardPresentation/Editor/B117_WSOUNDCOOL_Assert.cs`）：

- `Run()`：开场 `WFSoundPlayer.ClockOverride = 1000f`，`finally` 还原 —— 整组用**显式时刻**跑。
- `A0`：从「`Time.time > 0`」改成「`CooldownClock > 0` **且就是本组钉进去的那个时刻**」。
  红法仍在：把 `CooldownClock` 改回裸 `Time.time` ⇒ 批处理里读回 `0.000` ⇒ 红。
- `A3`：**不再**「把 `timeToPlayAgain` 压到 0」，改成**显式推进时钟**（`clock0 + ttp + 0.01`）。
  这同时把「窗口宽度」这条判据重新纳回被测面（原写法等于把窗口宽度测掉）。
- `A1 / A2 / A4 / A5 / A6` 逻辑一字未改 —— 时钟一对，它们自己就绿。

### B · `B117WCOOL3`（battle 4 条）

失败 = `A2 / B2 / C2 / D2`（三处生产路径 + 公用公式，全是同一个根因）。

改的是**断言**（`Assets/CardPresentation/Editor/B117_WCOOL3_Assert.cs`）：

- `Run()`：`WFSoundPlayer.ClockOverride = 2000f`，`finally` 还原。三处生产路径
  （`RemnantSfx` · `WindowSounds` · `WarpforgeAudio.PlayJingle`）共用同一个 `CanPlayByCooldown` ⇒ 钉一次全生效。
- **新增 `D5`**：直接量**生产端**时钟（临时把 `ClockOverride` 置 -1 再读 `CooldownClock`）必须 `> 0`。
  这是本次根因的**专用护栏**：把 `CooldownClock` 改回裸 `Time.time` ⇒ `D5` 红（A2/B2/C2 会跟着红）。
- `A1/A3/B1/B3/C1/C3/D1/D3/D4` 一字未改。

### C · `[SHOP] B4 / B5`（既有 2 条）

**先判「原版这一处该不该被挡」**——判据 `AudioCue__ConfigAudioSource.c:19-21` +
唯一调用点族 `SoundManager__Play`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:257`）：
**原版所有「按 cue 播」的路径都过这一道闸**，`WindowSounds` 那族（`Play2D(cue, MixerType.FX)`）**一样被挡**
⇒ **该挡**。所以 `WSOUNDCOOL` 的**实现是对的**，`B4/B5` 的期望（`Played` 停在 1、`Throttled` = 1）**也是对的** ——
它们红只是因为时钟。**⇒ 这两个文件一行都不用改**（`Editor/B97_SHOP_Assert.cs` 未改动）。

> 顺带说明 `B5` 的语义没变：它断的仍是「第二次**确实调到了** `PlayFx`（被闸挡），而不是调用方提前 return」。

### D · `B117G1012 A2`（**本代理新发现的第 11 条红，不在任务清单里**）

`battle.log:1103986`：`B117G1012 ✗ A2 同一字段连响两次 ⇒ 第 2 次被 timeToPlayAgain 挡掉 1 次（实得 Throttled=0）`
（合计 `18 通过 / 1 失败`）。同一个根因（`Editor/B117_G10_12_Assert.cs:96-97` 的旧注释还写着
「批处理里 `Time.time` 不前进 ⇒ 必然被挡」，那条注释与事实不符：0 时刻哨兵让闸**恒不生效**）。
时钟一修即绿，**该文件未改动**。

---

## 三、应该转绿的断言（给调度台复跑时对数）

| 日志 | 断言 | 变化 |
| --- | --- | --- |
| `shop.log` | `B117WSOUNDCOOL` A0 · A1 · A3 · A5 · A6 | 5 红 → 0 红（合计 **8 ✓ / 0 ✗**） |
| `battle.log` | `B117WSOUNDCOOL` A0 · A1 · A3 · A5 · A6 | 5 红 → 0（合计 **8 ✓ / 0 ✗**） |
| `battle.log` | `B117WCOOL3` A2 · B2 · C2 · D2 | 4 红 → 0（合计 **19 ✓ / 0 ✗**，含新增 D5） |
| `shop.log` | `[SHOP] B4` · `[SHOP] B5` | 2 红 → 0（**代码未改**，靠时钟） |
| `battle.log` | `BT ★ 同一条 cue 在 timeToPlayAgain(0.1s) 内再播被挡`（`BattleScene.cs:17013`） | 1 红 → 绿（**代码未改**） |
| `battle.log` | `B117G1012 A2` | 1 红 → 绿（**代码未改**；合计 **19 通过 / 0 失败**） |

---

## 四、仍会红的（如实写）

`BT ★ 播的时候音高落在原版给的区间里（实得 0.000）` 与 `BT ★ 音量也是从区间里取的`
（`battle.log:737408 / 737421`，落点 `Editor/BattleScene.cs:17004 / 17006`）—— **不是时钟问题**，诊断如下：

1. `BattleScene.cs:16981` 调 `RemnantSfx.ResetCounters()` ⇒ 按它自己的契约把
   **`LastPitch` / `LastVolume` 一起清零**（`Battle/RemnantSfx.cs:56`；该契约由 `B117_G10_12_Assert` B1 钉死，⛔ 不能改）。
2. `BattleScene.cs:16983` 紧接着调 `driver.PlayRemnantSfx(…, collectDeath: true)` —— 这一路**故意不播**
   （同一节的 :16988 就在断 `Played == 0`）⇒ **没有任何一次播放把 `LastPitch` 写回来**。
3. `:16996` 起转去断「区间表」（绿），然后 `:17004 / :17006` 读的仍是**被清零的** `LastPitch` / `LastVolume`。

⇒ 生产代码（`RemnantSfx.cs:200-209` 的 `Random.Range` 与 `LastPitch = pitch`）**没有短路**，
是**断言取值时机**错了。**修法属于 `Editor/BattleScene.cs` 的写手**（本代理红线禁止改 `Editor/*Scene.cs`）：

- 最小改法：把 `:17004 / :17006` 两条移到 `:16965` 之后（也就是**在** `:16968` 那次 `ResetCounters` **之前**），
  或
- 在 `:16996` 之前补一次真播：`RemnantSfx.ResetThrottle(); RemnantSfx.Play(RemnantSfx.Moment.Death, true);`
  （播完立刻读 `LastPitch/LastVolume`），再断 17004/17006。

⛔ **不要**去改 `RemnantSfx.ResetCounters()` 让它不清 pitch/volume —— 那是 `B117-57` 明确修过的行为，
`B117_G10_12` B1 直接钉着它（改了那条会红）。

---

## 五、影响面自查（「节流第一次在批处理里真的生效」）

改动前批处理里冷却**从未生效过**，所以必须逐个检查「同一 cue 连播两次」的既有断言：

- 全仓**真的去读音效计数器**的断言共 **34 行 / 9 个文件**（现读：
  `grep -rn "(WindowSounds|WFSoundPlayer|BattleSfx|RemnantSfx|WarpforgeAudio)\.(Played|Throttled|LastCueThrottled|JingleThrottled)"`，
  加 `B117_G10_24_Assert` 那处跨行实参）：`B117F11` · `B117_G10_12` · `B117_G10_24` · `B117_WSOUNDCOOL` ·
  `B97_SHOP` · `B98A5` · `BattleScene` · `CardFaceAssertE5` · `SoundCueAssertT` —— **逐个看过**：
  除本报告 §三 列出的那几条外，其余**每一组的两拍之间都调了 `ResetCounters`**（`WindowSounds` / `BattleSfx`
  的 `ResetCounters` 都会 `WFSoundPlayer.ResetThrottle()`，`RemnantSfx` 走独立的 `ResetThrottle()`）⇒ 不会互相节流；
  `B117_G10_24` 断的是「没有新增播放」（计数器不涨），节流只会让它更容易成立。
- `B117_G10_10a` 的 `PlayedCountForTest` 是它自己的计数器，与音效冷却无关。
- `BattleSfx.Played` / `WFSoundPlayer.Played` 是**请求计数**（闸之前自增），不受冷却影响。
- `RewardWindow.AppearSfxPlayed`（「每格一声 ×12」）走的是 **`WFSoundPlayer.Play(AudioClip, …)` 重载 ⇒ 压根没有冷却**。
- 已知的唯一行为变化：批处理里同一 cue 在窗口（多为 0.1 s）内的**重复请求现在真的会被挡** —— 这正是原版语义。

---

## 六、动了哪些文件

| 文件 | 改了什么 |
| --- | --- |
| `Assets/WarpforgeVFX/Runtime/WFSoundPlayer.cs` | 类尾新增 `ClockOverride` / `CooldownClock`（追加在类尾，前面行号不动）；`float now = Time.time;` → `CooldownClock`（同行替换）；一处 `<para>` 注释就地订正 |
| `Assets/CardPresentation/Editor/B117_WSOUNDCOOL_Assert.cs` | `Run()` 钉时钟 + 还原；`A0` 改成量 `CooldownClock`；`A3` 改成显式推进时钟；文件头「为什么能离线跑」就地改成新口径 |
| `Assets/CardPresentation/Editor/B117_WCOOL3_Assert.cs` | `Run()` 钉时钟 + 还原；**新增 `D5`**（生产端时钟非 0）；文件头两节就地订正 |
| `Assets/CardPresentation/Editor/B97_SHOP_Assert.cs` | **未改**（判定：实现与断言都对，红的只是时钟） |
| `Shell/WindowSounds.cs` · `Battle/RemnantSfx.cs` · `Core/WarpforgeAudio.cs` · `Battle/BattleSfx.cs` | **未改** |

**typecheck**：`TMPDIR=/tmp/wf_fixa bash Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
行尾：三个被改文件现读全部 **LF（CRLF 0 · 裸 CR 0）**，未翻行尾。
