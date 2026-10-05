# B1 · A354 + A360 · `Editor/BattleScene.cs` 两族弱断言补牙（写手 · 2026-10-12）

> 只改一个文件（白名单内）：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`。
> ⛔ 没跑 Unity · ⛔ 没动 git（只读的 `git status` / `git diff`）· ⛔ 没改两张正本 · ⛔ 没越白名单（`Battle/BattleDriver.cs` 一个字节没碰）。
> 秒级类型检查：**运行时 0 / 编辑器 0**（中间两次撞上别的写手的半成品，见 §六）。

## 一、结论（三句）

1. **A354 —— 两处「没牙口」都换成真采曲线**（不是断常量、也不是只断次数）：
   `CardFeel` 挨打后坐那条采到**反向过冲（穿零，负向 ≈ −20% 正向峰值）**，准星换打法那条采到**单峰、从不低于原始 scale** ——
   两条**互相能分辨**（把任一处的实参换到另一处那档，各自的断言立刻红）。
2. **A360 —— 两条恒真断言都换成「这张卡自己的位姿」当静止位**（同一世界系），并各配一条**灭自证**
   （与 A337 的 `sep3D`、A359 的 `dLinePop` 同型），顺手把改前 `:7583` 那条**同病的诊断日志**（现 `:7719`）也换到同一个靶。
   `:7588` 那句 `eBoard.SlotPosition(victim)` 现在**一处都不剩**（全文件 `eBoard.SlotPosition(victim)` 只剩 `:7760` 飘字那一段的**对照**用法，那是 A359 的灭自证，✅ 该留）。`pBoard.SlotPosition(probe)` 也只剩注释里那句「原来是什么」。
3. **两处都按铁律 5 就地订正了错前提**：准星那条旧注释写「punch 会来回振荡」，而 `vibrato = 0` 时
   `count` 被钳成 **2**（单峰、构造性不振荡）—— 前提是错的，已改写并**保留更正痕迹**。

## 二、判据（动手前查到的原件，逐条带出处）

| # | 判据 | 出处（都是**现读**） |
|---|---|---|
| 1 | `DOTween.Punch` 的算法：`count = (int)(vibrato × duration)`、**`< 2` 钳成 2**；`end[0] = direction` · `end[count-1] = 0` · 中间奇数格 `end[i] = −ClampMagnitude(direction, mag × elasticity)`，**`mag` 每轮按 `\|direction\|/count` 递减** | `资料/已知的坑.md:3962-3970`（2026-10-11 那条；两条独立路径：我们 DLL 的 IL ＋ 原版 `GameAssembly.dll` 反汇编）。⚠️ 我只**采信**它，没有重读 IL（本件白名单只有一个 `.cs`）——**复核入口已在报告里写死**，见 §五·1 |
| 2 | `CardFeel` 挨打那条的实参是 `DOPunchPosition(d × ToOurs(mag), 0.4, 8, 0.3)`（生产入口 = `CardFeel.HitReact`，`BattleDriver.PlayHitFeel` 调它） | `Core/CardFeel.cs:58/672-673` · `Battle/BattleDriver.cs:5922-5966` |
| 3 | 准星那条的实参是 `DOPunchScale(_crossOriginalScale × 0.2, 0.5f, 0, 1f)`，且**只有 `PunchIfKindChanged` 一处**写 `_cross.localScale` | `Battle/TargetReticle.cs:608-617`（`localScale` 全文件只有 `:371/:613/:623` 三处，前两处是存/归位） |
| 4 | punch 是**相对位移**（`ToArray(...).NoFrom()` ＋ punch 起始模式 ⇒ 值 = 起始值 + 关键帧） | 同上 #1；旁证：`RewardWindow` 那次实测轨迹 `1.0 → 1.1 → 1.0`（`资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#3） |
| 5 | 准星那条 punch **在 `SimulateCommand(Ranged)` 里同步创建**（`CommitCommand → ShowReticleNow → reticle.Show → PunchIfKindChanged`），创建时**一格补间时间都没走** ⇒ 可以从 t=0 采样 | `Battle/BattleDriver.cs:4919-4946`（`CommitCommand`）· `:4995-5000`（`ShowReticleNow`）· `Battle/TargetReticle.cs:575-602` |
| 6 | A360 的病灶：卡的 3D 位置（`ArenaSlots.RootPosition`，我方 z = −6.655 / 敌方 z = +1.043）与 `SlotPosition`（HUD 正交平面，z 恒 0）**不同世界系** | `Board/ArenaSlots.cs:58` · `Board/BoardLayout.cs:359-368` · `资料/普查产出_1012/S2_战斗侧_开账现核.md` §一 A360 |
| 7 | 「卡到静止位」是**立即** `SetPose`（不是补间）⇒ 时间线没推之前那一刻的位姿**就是**静止位，可以当靶 | `Battle/BattleDriver.cs:6224-6232`（`SyncBoard` → `SetPose`）· WB3 报告 §二·4 |
| 8 | 攻击者那一段是**朝受击者**冲（`endPos = 受击者位置 − dir × margin`） | `Core/CardFeel.cs:586-593`（`MeleeEndPos`）· `:597-613`（`MeleeAttack` 段1 `DOMove`） |
| 9 | 批处理下补间推进方式 = `CardTween.Mode = Manual`（入口第一行就设死）⇒ `DOTween.ManualUpdate` 能推 | `Editor/BattleScene.cs:192` · `Core/CardTween.cs:70-73` |

**⚠️ 本件没采纳的「更硬」方案**（理由见 §五·2）：靶取 `ArenaSlots.RootPosition(...)` 做**绝对**断言。

## 三、改动清单（5 处 hunk，同一个文件；每处一句为什么）

| # | 位置（改后行号） | 改了什么 | 为什么 |
|---|---|---|---|
| 1 | `BattleScene.cs:3279-3316` | **新增**：换打法 punch 的**曲线采样**两条（`sMax`/`sMin` vs 归位值） | 原来那处只有「触发次数」（改前 `:3283`，现 `:3328` 的 `PunchCount >= 2`）—— 对「弹成什么形状」**无感** |
| 2 | `BattleScene.cs:3320-3330` | **就地订正**旧注释「punch 会来回振荡再回到原位」＋ 把 `PunchCount`（`:3328`）那条**降级**成「只断触发过」 | 那个前提是错的（`vibrato = 0` ⇒ `count` 钳 2 ⇒ 单峰）；留着会让下一个人继续按错口径写断言（铁律 5） |
| 3 | `BattleScene.cs:7511-7552` | **新增**：挨打后坐 punch 的**曲线采样**两条（正向峰值 + **反向过冲**） | 上面那条（`:7506-7509`）**只断常量** 0.4/8/0.3 —— 常量全对而曲线写成单调补间也照样绿 |
| 4 | `BattleScene.cs:7623-7625` · `:7659-7686` · `:7698-7710` | 攻击者/受击者的「静止位」改成**卡自己出手前那一刻的位姿**（`restAtk` / `restVic`）＋ 一条灭自证（`sepLine`）＋ `:7719` 诊断日志同靶 | 原来拿 **HUD 平面 `SlotPosition`** 量 **3D 卡**：z 就差 6~7 ⇒ `Distance > 2` ⇒ `> 0.02f` **恒真**（A360） |
| 5 | `BattleScene.cs:7725-7729` | **新增** ② 的**方向**断言（位移方向 · 受击者方向 > 0.3） | 「冲了，但冲反了 / 冲到别处」这一档原来的断言分辨不出来（2026-09-29 修掉的正是这个缺陷类） |

**没动**：`:7721` 那条 `Check(vicView != null, …)`、`:7730-7731` 的旋转断言、飘字那一段（A359 已收口）。
**行号会漂**：本件之后同文件任何增删都会挪这些号 —— 认**锚点文字**别认行号（本仓已吃过多次）。

## 四、新增 / 改动断言逐条（断什么 · 改坏了会不会红）

### A354-a · 准星换打法那一下（`vibrato = 0` ⇒ `count = 0 → 钳 2` ⇒ **单峰**）

| # | 断什么 | 🧨 改坏了会不会红 |
|---|---|---|
| a1 `:3307-3310` | `sMax − sRest > 0.02 × sRest`（峰值高于归位值 ⇒ **真弹了**；实测预期 `sRest = 1`、峰值 ≈ 1.2） | ✔ 红：把 `DOPunchScale` 删掉 / `ScaleOnChangeModifier` 写 0 / punch 挂在别的节点上（`CrossScaleX` 恒 1） |
| a2 `:3311-3315` | `sMin ≥ sRest − 1e-3`（**单峰、不向下穿零**） | ✔ 红：`vibrato` 调到 ≥ 6（`(int)(6×0.5) = 3`）⇒ `end[1] = −ClampMagnitude(dir, mag×elasticity)` 压到原始 scale 以下。⚠️ **a1 是 a2 的挡板**：把 punch 拿掉时 a2 恒真（"一次都没弹"也"不穿零"），靠 a1 才抓得住 |

### A354-b · `CardFeel` 挨打后坐（`8 × 0.4` ⇒ `count = 3` ⇒ **会穿零**）

| # | 断什么 | 🧨 改坏了会不会红 |
|---|---|---|
| b1 `:7544-7546` | 正向峰值 `hi > 0.05` 世界单位（沿 `Flat(away)` 轴；生产入口 `CardFeel.HitReact(ptr, right, false, 0, 5)` ⇒ 幅度 1.0 原版单位 = 1.6865 我们单位） | ✔ 红：`DOPunchPosition` 换成 `DOMove` 那类单调补间（若落点就是原地则 `hi = 0`）/ `PushBackMagnitude` 恒 0 / 补间没被泵 |
| b2 `:7547-7550` | 负向最低 `lo < −0.05 × hi`（**反向过冲 = 穿零**；预期 `≈ −0.2 × hi`） | ✔ 红：`PushBackVibrato` ≤ 5（`5×0.4 = 2` ⇒ 段数掉回 2 ⇒ 曲线变单峰）/ `PushBackElasticity` 写 0（`ClampMagnitude(dir, 0) = 0` ⇒ 第 2 段回 0）/ 整条换成单峰实现 |

> **a / b 的互相分辨力**（这就是 A354 要的牙口）：把准星那条的实参改成 `vibrato ≥ 6` ⇒ a2 红；
> 把挨打那条的 `vibrato` 降到 ≤ 5 ⇒ b2 红。两条断言**能分开「单峰」和「穿零」这两种状态**。

### A360 · 两条「离开了静止位」

| # | 断什么 | 🧨 改坏了会不会红 |
|---|---|---|
| c1 `:7674` | 受击者的视图在场上（挨打**之前**那一刻；没有它，c4 的 `restVic` 会退化成 `Vector3.zero` ⇒ 假绿） | ✔ 红：`RefreshAll` 不为新落子建视图（那一档 `:7717` 也会红，两条互为佐证） |
| c2 `:7681-7685` | ★ 灭自证：`Distance(restAtk, pBoard.SlotPosition(probe)) > 0.02`（预期 ≈ 7.x 世界单位） | ✔ 红：3D 战场被关掉 / 卡视图被搬进 HUD 平面（那两条主断言的前提就不成立了） |
| c3 `:7698-7710` | ★ ② 位移 `> 0.02` 世界单位 **＋ 方向朝受击者**（`Dot(...) > 0.3`，预期 ≈ 1） | ✔ 红：`CardFeel.MeleeAttack` 拿掉 / 段1 的 `DOMove` 目标算错（**2026-09-29 修掉的「两个世界系相减」若复发** ⇒ 方向乱）/ 幅度 0 |
| c4 `:7724-7729` | ③ 位移 `> 0.02` 世界单位（受击者 vs **它自己挨打前的位姿**） | ✔ 红：`CardFeel.HitReact` 拿掉 / 幅度 0 / 补间不推进（**修之前这一条恒绿**） |

**上面「改坏法」的共同点**：把靶换回 `SlotPosition` ⇒ c3/c4 **恒绿**（那正是 A360 病灶）—— 所以 c2 那条灭自证
**放在读数里**（不是只写在注释里）：它把「两个候选靶必须真的分得开」钉成一条可审计的断言。
（⚠️ 如实说：c2 对「有人把靶改回 `SlotPosition`」这件事**本身**并不报红 —— 它和 A337 的 `sep3D` 是同一种
「前提可见化」，挡的是「卡视图根本不在 3D 世界里」那一类。真正的挡板是 c3/c4 的读数本身。）

## 五、没查清 / 判不了的（⛔ 不猜）

1. **`DOTween.Punch` 的 IL 我**没有**亲自重读** —— 采信 `资料/已知的坑.md`（2026-10-11）那两条独立路径的结论
   （它给出：我们的 `Assets/Plugins/Demigiant/DOTween/DOTween.dll` 用 Mono.Cecil 读，原版走
   `D:/2/unity_run_ref/GameAssembly.dll` 的 capstone；⚠️ 该条已注明 `D:/2/Warpforge_code/Assemblies/DOTween.dll`
   与 `il2cpp_out/DummyDll/DOTween.dll` **都是空壳**）。**复核入口**：那两处 DA 地址/工具都在那条记录里。
   本件白名单只有一个 `.cs`，**没有重读 IL 的手段**（不跑 Unity、不新增工具）。
2. **两条新采样断言的实测值我没跑**（⛔ 简报禁跑 Unity）—— 阈值都是**按判据手算**留了 ≥ 4× 余量：
   b2 预期比值 0.2 vs 阈值 0.05；b1 预期 1.6865 vs 阈值 0.05；a1 预期 0.2×s₀ vs 阈值 0.02×s₀。
   ⚠️ 若下次跑 `BattleScene.Run` 时这两族红，**先看读数与预期差多少**（差一个数量级 = 我的模型错，
   差一点 = 采样步长/缓动）—— 别急着改常量。
3. **`_crossOriginalScale` 的具体数值没实读**（读的是「`ImageQuad.Create` 不改 `localScale`」⇒ 应为 1）：
   `Battle/ImageQuad.cs` 全文件**没有** `localScale` 写入（grep 0 命中）。⇒ a1 的阈值写成**相对**形式
   （`0.02 × sRest`），即使它不是 1 也成立。
4. **A360 那两条没做成「绝对」断言**（靶 `ArenaSlots.RootPosition(...)`）：那要挑对 `CardScale`/`HeroScale`
   与督军位分支（WB3 §五·3 已记「督军位那一格投影点可能不准」）——**没有实跑手段能确认**，
   按「不许把查不到的写成猜测」放弃，改用**同一世界系的自比较**（对「动了没动」这件事判别力等价，且不引入新风险）。
5. **`MeleeEndPos` 的 `margin`（1.7 / 1.0）量纲没核**（它是我们世界单位还是原版单位）—— **不影响本件**：
   c3 只断方向，不断距离。
6. **本件没有验证「c3 的朝向在 2D 兜底布局下也成立」**：c3 不依赖投影，2D 下同样成立（`MeleeAttack` 两支共用），
   但**没跑过 2D 那一档**。

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **`CardFeel.Lunge` 在生产里是死代码** —— 全仓只有 `Editor/BattleScene.cs:7490` 那个自检调它，
   `BattleDriver.PlayAttackFeel` 走的是 **`CardFeel.MeleeAttack`（三段式序列）**。
   它的实参是 `AttackPunchVibrato = 10 × AttackPunchDuration = 0.3` ⇒ **`count = 3`，同样是"会穿零"那一族**，
   但既然是死代码，**它的曲线形状不是生产行为**（本件按简报只覆盖简报点名的两个生产调用点，没给它加断言）。
   ⚠️ 若要动它，顺带看一眼 `Core/CardFeel.cs:532` 那句「从前是 `Charge() 0.35s` 之后才 `Lunge()`」的历史。
2. 🟡 **`CardFeel.HitReact` 的第二条 punch（旋转）也是 `count = 3`**（`HitRotVibrato = 7 × HitRotDuration = 0.5`），
   而 `:7727` 那条旋转断言只断「转过 > 0.3°」—— 同族的「形状」没查（**不在本件范围**）。
3. 🟡 **`TargetReticle.Show()` 是无条件调用**（`ShowReticleNow` 不看 `Visible`）⇒ 每次 `CommitCommand` 都可能 punch；
   本件靠「`_lastPunchKind` 没变就不 punch」这一层（`TargetReticle.cs:610`）保证「只在换打法那一下」。**没跑过**。
4. 🟡 **`:7719` 那条诊断日志原来打的是错坐标**（`eBoard.SlotPosition(victim)`，3D 下恒 > 2）——
   这类「只打日志、不影响红绿」的同病还有几处（WB3 报告 §六·5 列过 `BattleDriver.cs:5719/5730` 等），
   本件只顺手改了这一处**紧邻诊断**，别处**没碰**。

## 七、跑过的检查（原文）

```
$ TMPDIR=/tmp/wf_b1 bash d:/4/Unity/工具/typecheck.sh      # 第 1 次
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0

（第 2 次）运行时 0 / 编辑器 0      ← 前 3 处改动之后
（第 3 次）运行时错误数: 3：
  Shell/MissionsTab.cs(908,17) CS7036（`BuildMilestone` 少一个实参）
  Battle/ScenarioBlendables.cs(1679,48) / (1680,26) CS0103（找不到 `Destroy` / `DestroyImmediate`）
  → 全部在**别人的文件**里（本件白名单外），按简报 §红线：记下来、不改
（第 4 次，等 90s 后复跑）运行时 0 / 编辑器 1：
  Editor/RewardsScene.cs(2031,56) CS0103（`wkCard37` 未定义）→ 同样是**别的写手正在写的半成品**
（第 5 次，等 100s 后复跑）运行时 0 / 编辑器 0   ✅ 全部自愈
```
**⇒ 我的文件全程 0 错**（那 4 条错分别是 3 个别的文件，且两条独立复跑都自愈了 = 是「写手在跑」的典型症状）。

**行尾核对**（铁律 12）：`BattleScene.cs` 改前 **CRLF 10505 / LF 10505**，改后 **CRLF 10644 / LF 10644**
（纯 CRLF 文件，用 Edit 工具改的，**没翻**）。`git diff --numstat` = **146 加 / 7 删**，**5 个 hunk**，
全部落在本件那 5 处（逐 hunk 核过，没有夹带别人的改动）。
