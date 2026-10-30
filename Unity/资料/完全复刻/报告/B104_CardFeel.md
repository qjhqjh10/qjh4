# B104_CardFeel — 屏震 `rawSignal` 收尾 + 相机/HUD 同向平移

> 执行代理 `B104_CardFeel` · 2026-10-31 · 第一百零四会话
> 白名单：`Core/CardFeel.cs` · `WarpforgeVFX/Runtime/WFModuleScreenShake*.cs` · `工具/gen_shake_presets.py` 及产物 ·
> 新建 `Editor/B104_CardFeel_Assert.cs`（**未接线**，由调度台做）· 本报告。

---

## 〇、结论（一句话）

**台账 W65 里那句「⏳ 仍欠落地 4+1 处」是过期的**：R9 的 **4+1 处全部已落地**（逐条证据见 §一），
`gen_shake_presets.py --check` 与现有产物逐字段一致。本次真正补到的是**两件台账没记的东西**：

1. 🔴 **`ShakeCamera` 的 `delay` 语义错**：无条件 `apply(0)` 把「等待期」也画成了位移
   —— 434 个屏震模块里 **139 个**带正 delay（183 条正 delay 条目，其中 **140 条**用 `attackTime == 0` 的 preset），
   等待最长 **6.2 s**；原版在这段是 `WaitForSeconds`、相机不动。**已修**（零行数变化）。
2. **「相机 + HUD 同向平移」的原版判据找到了**（此前记成「迁移件、没有判据」）：13 个战场
   `useCanvasShake = 0` + HUD 由独立 `UI Camera` 渲染（无 Brain / 无 listener）⇒ 原版就是「只有战场相机震」。
   我们的单相机等价实现站得住，注释已按判据重写（零行数变化）。

---

## 一、R9 逐处现状（回源核，⛔ 不是照台账）

| # | 环节 | 现状 | 证据（现读） |
|---|---|---|---|
| 1 | **生成器抽波形** | ✅ 已落地 | `工具/gen_shake_presets.py:103` `read_signal()` 收 `PositionNoise`/`OrientationNoise` 全 3 octave×X/Y/Z；`:150` pathID→信号名；`:154` `hasRawSignal`；`:182` 写 `signals`/`defaultSignal` |
| 2 | **`shake_presets.json`** | ✅ 已落地 | `Resources/WarpforgeVFX/shake_presets.json`（472 行）：`count=17` · `defaultSignal="Warpforge 6D Shake"` · `signals` **4 份** · 每个 preset 有 `rawSignal`（**5 个非空**：Forward Pull / Land on ground / Upwards Hand / UI Earthquake / UI Hit） |
| 3 | **`WFModuleScreenShake.PresetRec/Request/SignalFor`** | ✅ 已落地 | `PresetRec.rawSignal` `:244-252` · `Request.signal` `:115` · `SignalFor()` `:334-353` · `Resolve()` `:358-433`（G12：`usePresetSignal` 默认 false ⇒ 池化档恒默认信号，已按 `OverwriteCameraShakePreset__GetModifiedPreset.c` 核过：只写 7 个字段、`+0x38 rawSignal` 从不写） |
| 4 | **`CardFeel.ShakeNoise01`** | ✅ 已落地（逐带 `Constant` 1/0/1·`PerlinNoise(u,0)`·三条带相加·共享 `T`·`/0.149`） | `Core/CardFeel.cs:1767-1781`；`ShakeBandConstant` `:378`；`ShakeBandPeak` `:386`（保持 0.149）；`ShakePhaseRange` `:1656` = ±1000；`ShakeOrientation` `:1809-1834` |
| 5 | **`BattleDriver` 传参那 5 个字段** | ✅ 已落地（`frequency/attack/sustain/decay/signal` 全传） | `Battle/BattleDriver.cs:3579` `HookAnimFxShake` → `:3594-3596` `signal/frequencyGain/attackTime`（同一调用里还有 `sustainTime/decayTime`）；`ShakeSignalOf` `:3611`（json→运行时的 Orientation 轴序） |
| + | 三条真件尾巴（`verticalSign` 删除 · `OrientationNoise` · 旧断言换灭自证） | ✅ 已落地 | `ShakeScreenDirOf` `:4100`；`ShakeCamera` `:1382` 起（q 后乘、HUD 刚体反补 `:1467-1475`） |

⇒ **R9 本体不需要再改**；台账那一行建议由调度台订正为「✅ 全落地」（我无权改台账）。

---

## 二、改了什么

### 2·1 `Core/CardFeel.cs`（**零行数变化**，未插行）

| 位置 | 改动 |
|---|---|
| `ShakeCamera`（`:1337-1343` 注释块） | 把「**迁移件、不是原版做法**」改成**原版判据 + 等价实现**（见 §三）。**7 行→7 行** |
| `ShakeCamera`（`:1478-1486`） | 🔴 **修 `delay` 语义**：`apply(0f)` → `if (delay <= 0f) apply(0f);`；`SetDelay(delay)` → `SetDelay(delay > 0f ? delay : 0f)`（原版把负 delay 夹到 0）。**行数不变** |

**为什么 `delay` 是缺陷**（机器码级）：
`decomp_full/CameraShakerManager._DoShake_d__33__MoveNext.c:236-270` —— 算出 `delay` 后先把负值夹 0；
`delay == 0` 才**同一拍** `GenerateImpulseWithVelocity`，否则 `WaitForSeconds(delay)` 再生成。
我们原来在**建补间时**无条件 `apply(0)` ⇒ `attackTime == 0` 的 preset（Hit Small / Hit Tiny / Land / Hit Big / Hit Medium / Forward Pull / Upwards Hand）
会在等待期就被推到 `t=0` 的位移上。

**影响面（现读 `数据/游戏数据/animfx_modules.json` + `shake_presets.json`）**：
434 个 `AnimFXModuleScreenShake` 模块 · `cameraShakes`+`manualTriggerCameraShakes` 共 508 条 delay；
**183 条 delay > 0**（**139 个模块**至少一条），其中 **140 条**的 preset `attackTime == 0`；最大 delay **6.2 s**。
另有 `BattleDriver.PlayBeatShake`（`screenShake` 57 行）传 `delay = max(0, ShakeDelay)`，同样受益。

### 2·2 `工具/gen_shake_presets.py`（+22 行）

- 新增 `--check`（`:67` + main 里的分支）：**只比对、零写盘**，逐字段比对现有产物与生成器输出，
  **忽略 `generated` 时间戳**（产物里带时间戳 ⇒ 「两次逐字节一致」永远不成立，闸的正确口径是「除时间戳外逐字段一致」）。
- `--dry` / 默认写盘 / `-h` 行为与以前一致。

### 2·3 `Resources/WarpforgeVFX/shake_presets.json`

**未重生成**（`--check` 现读 **OK**：与生成器逐字段一致；重生成只会刷新 `generated` 时间戳，是噪声 diff）。
产出现读：**472 行** · `count=17` · `signals=4` · 5 个 non-default `rawSignal`。

### 2·4 新建 `CardPresentation/Editor/B104_CardFeel_Assert.cs`（**未接线**）

`B104_CardFeel_Assert.Run()`，覆盖：
- **A 数据**：默认信号 Y 三带（1.90/0.059/**1** · 9.10/0.040/**0** · 55.54/0.050/**1**）· 4 份信号 Y 列逐位相同 ·
  json vs 出厂表逐位一致 · `SignalFor` 两条（空→默认 / `…One Axis`）· `Shake Hit Small` 六项真件值 · 池化档恒默认信号；
- **B 算式**：sin 一拍（±0.5/0.149、0 点）· Perlin 一拍（`PerlinNoise(u,0)` 且**不等于** `(0,u)`）· 三条带相加（手算）·
  `frequencyGain` 是纯时间缩放 · phase 在共享 `T` 里 · 值域 ±0.5；
- **C 灭自证**：旧实现（三条全 Perlin / `×2−1` / 轴 `(0,f·t+phase)` / 相位不分带）16 点逐点对不上 · 「三条全 Perlin」8 点对不上；
- **D `delay`**：`delay=0` 当场位移 · `delay=0.5` 等待期相机 **0 位移** · 0.55 s 后才起震且值 = 幅度×噪声 · 收尾复位；
- **E 相机/HUD**：**不依赖实现的不变量** —— `cam.InverseTransformPoint(hud.position)` 在震动前后不变
  （漂移 < 2e-4）；并用「不补偿时会漂多少」做灭自证。

### 2·5 B2（采样轴）断言修正 —— 2026-10-31 第一轮 `BattleScene.Run` 之后

**现象**：`B104CF 42/1`，红的 B2 在 `u=1.23`：`实得 1.012E+000 · x 轴参考 1.012E+000 · y 轴参考 1.012E+000`
（= 这一刻 `P(u,0)` 与 `P(0,u)` **撞在一起**，两支都看不出差别）。

**先回源确认「是不是恒等对称」——不是。** 两条**同一次运行**内的证据：
1. 同一次 `B104CF` 里 B2 的两个采样点中，**`u=0.37` 是通过的**（要求 `|P(u,0) − P(0,u)| > 1e-4`），
   只有 `u=1.23` 撞上；若恒等，两点都会红。
2. 同一次运行的日志 `_tmp_view/host_battlescene.log:119122`：`BattleScene` 自己那条 R9 断言
   （`BattleScene.cs:24606-24626`，固定 6 个点 `u=0.13…0.78`，判据要求 `|P(u,0) − P(0,u)| > 1.49e-4`）**6/6 通过**。
⇒ Unity 的 `Mathf.PerlinNoise` **在一般点上两支可区分**；`u=1.23` 是**重合点**，拿单点当判别器会把正确实现判红。

**处理（只改断言，未动生产代码、未放宽其他断言）**：
扫 `u = 0.11 … 2.64`（24 个），**先算两支参考值，差 ≤1e-3 的点跳过、不计入分母**；
要求 **≥4 个有判别力的点、且全部站 x 轴**（`axOk == axTested`）。
- 若实现改回 `(0,u)`：每个判别点都返回 `wantY`（与 `wantX` 差 >1e-3）⇒ `axOk = 0` ⇒ 红。
- 若哪天真的两支恒等：`axTested = 0 < 4` ⇒ **也红**（⛔ 不会退化成「条件永不成立」的恒真断言）。

---

## 三、原版判据：HUD 为什么不该震（此前是空的）

| 判据 | 现读 |
|---|---|
| 13 个战场的 `CameraShakerManager` | `assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_45xx.json`：**13/13** `useCanvasShake = 0` · `canvas = null` · `impulseListener` 非空（**主菜单那份是 `useCanvasShake = 1`**，那是另一条链） |
| 震源/消费方 | battlearena1：GO `Cinemachine Vcam` 上挂 `CinemachineImpulseSource`（`MonoBehaviour_4914`）+ `CinemachineImpulseListener`（`MonoBehaviour_4546`，`m_UseCameraSpace = 1`）；`BoardCamera` 上有 `CinemachineBrain`（`MonoBehaviour_5303`） |
| UI 为什么不跟 | GO `UI Camera`（Camera_1462）**没有 Brain、没有 listener**；两块 `ScreenSpaceCamera` 画布（`Canvas_2569`/`Canvas_2572`）都指向它 ⇒ HUD 由独立相机渲染、震不到 |
| `useCanvasShake = 1` 时才怎样 | `CameraShakerManager.ToggleListener` / `_DoShake_d__33`（ISIL）：那条支路用 `independentImpulseListener` + 把 `canvas.renderMode` 临时改 `WorldSpace`，收尾再改回 `ScreenSpaceCamera` —— 即「连 UI 一起震」是**显式开关**，战斗里关着 |

⇒ 我们的实现（单相机 + `hudRoot` 同向刚体变换）是**等价复刻**：
屏幕档 uGUI（`HudCanvas`，`ScreenSpaceCamera`）本来就跟相机走、无需补偿；`hudRoot` 下的世界档载体需要补偿。
棋盘 / 手牌 / 特效不被补偿 ⇒ 跟着相机晃 —— 与原版「战场相机震、UI 相机不震」的可见结果一致。

---

## 四、typecheck

- 🔴 第一次跑被**别人的在飞改动**挡住：`Shell/CampaignData.cs(688,13) error CS0103: 不存在名称 "Debug"`（顶部缺 `using UnityEngine;`）
  ⇒ 运行时程序集编不过，脚本不清旧 `WFCheck.dll`（当时 mtime 10-25）⇒ 编辑器那 138 条全是**级联假错**。
- ✅ 那次验证方式：用同一份 rsp，**只在临时目录**给 `CampaignData.cs` 补一行 `using UnityEngine;`（不碰仓库）编运行时 **0 error**，
  再用它的输出编编辑器程序集（含本件新文件）**0 error**。
- ✅ **现在（该文件已被其写手修好）**：`TMPDIR=/tmp/wf_cf bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 五、交给调度台的 `BattleDriver` 补丁

**无。** R9 要求的「5 个字段」早已在 `HookAnimFxShake`（`BattleDriver.cs:3588-3600`）传全：
`direction / signal / frequencyGain / attackTime / sustainTime / decayTime / rotationGain`。
`delay` 修复在 `CardFeel.ShakeCamera` 内部完成，调用点零改动。

**需要调度台做的只有一件**：把 `Editor/B104_CardFeel_Assert.cs` 接进宿主（推荐 `BattleScene.Run`，因为它本来就有 DOTween / 相机夹具）：

```csharp
B104_CardFeel_Assert.Run(); _pass += B104_CardFeel_Assert.Pass; _fail += B104_CardFeel_Assert.Fail;
```

---

## 六、没查清 / 判据缺口

1. **`CameraShakerManager.DoShake(..., ShakeAxis, ...)` 那条轴选信号支路**（`ShakeAxisSignal[] shakeSignals`）：
   ILSpy 全树**除自己外 0 调用点**，`assets_full` 里 **0 处 `m_MethodName: DoShake`** ⇒ **本 build 没有生产者**，
   我们没实装。⚠️ `CameraShakerManager__DoShake.c`（decomp_full）只有 rawSignal 那个重载，
   轴重载的体**读不到**（可能是 724 条无方法体之一）⇒ 「休眠」这个结论**靠的是 0 调用点，不是读到的空体**。
2. **`CameraShake`（DOTween 老组件，`DoLegacyShake` 的唯一生产者）**：序列化字段
   `baseShakeTime` / `scaleFactorX` / `shakeRepetitons` 在 `assets_full` **全库 0 命中** ⇒ **没有实例**，
   `DoLegacyShake` 是无入口的休眠档，未实装。
3. **`m_UseCameraSpace = 1` 的位置修正**：原版把冲量位置转到相机空间再加 `PositionCorrection`；
   我们按**世界空间**施加。战斗相机静止时 `FinalOrientation = identity`（scene 件 Transform_1318 旋转 = 恒等），
   q 只有 ~0.04° ⇒ 两者差 < 1e-4，未单独建模。**若将来相机有姿态**，这一条要重开。
4. **`ShakeCamera` 的 `apply(0)`（delay==0）在真实帧循环里等于「比首帧早一个 Update」** ——
   原版是协程里同一拍生成冲量；差异 ≤1 帧，未量化。

---

## 七、顺手发现（⛔ 我没改）

1. **`Assets/CardPresentation/Shell/CampaignData.cs:688` 用了 `Debug.LogError`，但文件顶部没有 `using UnityEngine;`**
   ⇒ 一度让运行时程序集编不过、`typecheck.sh` 全红（编辑器那 138 条是级联假错）。
   ✅ **已由其写手修好**（2026-10-31 现读 `typecheck` 0/0）；留痕备查。
2. `资料/任务线/特效线/任务总表.md` §一 W65 行**自相矛盾**：同一行先写「✅ 已落地（4+1 处全做完）」，
   后写「⏳ 仍欠落地 4+1 处」⇒ 建议删掉后半句（我无权重写台账）。
3. `ShakeCamera` 的 `TutorialOverlay` / `SkillPanel` 备注「画布上的件不跟 HudRoot 的震屏」：
   它们建在根 `Canvas`（`ScreenSpaceCamera`）下 ⇒ **本来就不会被相机震到、也不需要补偿**；
   该备注当成「已知差异」记着会误导。建议该文件写手按 `useCanvasShake = 0` 这条判据重新表述（不是缺陷）。
