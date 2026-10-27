# B9_W11SHAKE —— 震屏的**方向链**（两条真偏离：`PushBackDir` 退路值 + `ShakeCamera` 缺水平通道）

> 执行代理 `B9_W11SHAKE` · 2026-10-27 · 白名单内改动只落在
> `Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs` 与
> `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（+ 本报告 + `账/_parts/B9_W11SHAKE.tsv`）。
> `TMPDIR=/tmp/wf_w11 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（2026-10-27 现跑）。
> ⛔ 未跑任何 Unity（无 `-executeMethod` / 无 `_run_8_checks.sh`）· 未动 git · 未改正本 / 账主表 / 别人的报告。
> 🔴 **行号守恒**：`git diff -U0` 的 hunk = `@@ -1210,2 +1210,2 @@`·`-1214 +1214`·`-1220 +1220`·`-1226 +1226`·`-1429 +1429`·
> `-1433,2 +1433,2`·`-1452,2 +1452,2`·`-1647 +1647`·`-1686 +1686`·`-3822,0 +3823,364`（纯类尾追加）；
> `BattleScene.cs` = `-13439,2 +13439,2`·`-24175 +24175`·`-48487,0 +48488,122`（纯文件尾追加）⇒ **下游账零漂移**。

---

## 一、结论

| # | 偏离 | 原版 | 我们（改前） | 改后 | 状态 |
|---|---|---|---|---|---|
| ① | `PushBackDir` 的「两点重合」退路 | **`Vector3.zero`**（`DAT_1842da2a8+0xb8` = `Vector3` 静态块首格 = `zeroVector // 0x0`） | `Vector3.up`（注释自承「原版常量读出不来」） | **`Vector3.zero`** + 断言同步改 | ✅ 落地（A，纯值） |
| ② | `ShakeCamera` 的**方向通道** | `FromToRotation(down, v)·up` 的 **x（屏幕水平）+ y（屏幕竖直）**，`v = normalize(目标棋盘坐标)` | 只取 `.y` 一个通道 | `ShakeScreenDirOf` 取 **两个通道**；`ShakeCamera` 的位移改走它 | ✅ 本体落地 |
| ②′ | 原版挨打那一路的**入口** | `SetDirection(目标棋盘坐标)` + `SetAmplitude(2/4/8)` + `Play()` | 无（`ShakeCameraForHit` 用的是 preset 自带的 `(0,0.2,0.2)`） | 新增 `ShakeCameraForAttack(cam, hudRoot, 目标坐标, 攻击方近战)` | ✅ 落地（B 档：**调用点待接**，`BattleDriver.cs` 本波归 W10） |
| ②″ | 「棋盘坐标」的**坐标系** | 原版场地根在 **x = 100** | —— | `OrigBoardPosOf` = `our + (100, 0, 0)`，y 归 0 | ✅ 落地（`OriginalArenaOriginX` 与本文件 `ArenaOriginX` 由断言钉同值） |

一句话：**② 不是「原版有、我们没有」而是「我们只做了原版那一下的一个投影」。** 原版喂进 `SetDirection` 的是
**目标的棋盘坐标**，它的 `y` 恒为 0 ⇒ `FromToRotation(down, v)·up` 的 **y 分量恒 0** ⇒ 只保留竖直通道的话
「照抄」的结果是**画面完全不震**。所以本件先把**屏幕水平通道**补上（既有三档的 `.x` 恒 0 ⇒ **既有行为逐位不变**），
再照原版补一条 `ShakeCameraForAttack`。

---

## 二、证据（文件:行号）

### 2·1 `PushBackDir` 的退路值 = `Vector3.zero`（三条独立证据，同一结论）

| 证据 | 内容 |
|---|---|
| `decomp_full/CardScript__ReceiveAttackAnim.c:61-73` | `if (DAT_1834b2f44 < fVar7) { dir = d/len; } else { uVar5 = **(undefined8 **)(DAT_1842da2a8 + 0xb8); fVar10 = *(float *)(*(…)+1); }` ⇒ 走**静态常量向量**（8 + 4 字节 = 整个 Vector3） |
| `il2cpp_out/dump.cs`（`UnityEngine.Vector3` 字段表） | `private static readonly Vector3 zeroVector; // 0x0` · `oneVector // 0xC` · `upVector // 0x18` ⇒ **`+0xb8` 那块静态存储的第 0 格 = `zeroVector`** |
| `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:47675-47712` | 同一个 `DAT_1842da2a8 + 0xb8` 在 `GetBoardPosOfCard` 的 null 支上，ILSpy 直接给出名字 **`x = (float)Vector3.zeroVector;`** |

> ⚠️ 同一判据在本文件里原来有**两个说法**：`CardFeel.cs:1210` 的注释写「读不出名字 ⇒ 沿用 `Vector3.up`」，
> 而 `CardFeel.cs:3234` 那段注释一直写「静态块首格 = zero」。**按铁律 5 已就地统一成 zero**（改动清单 §4·1 第 1 条）。

### 2·2 原版喂进 `SetDirection` 的是**完整 3 分量**的棋盘坐标（订正上一路的「`(x,0,z)`」）

| 步骤 | 判据 |
|---|---|
| 方向从哪来 | `decomp_full/BattleManager._ResolveAttack_d__438__MoveNext.c:893-901`：`GetBoardPosOfCard(&local_168, bm, 目标卡)` → `AttackMeleeAnim(攻击方, &local_178, 目标卡)` |
| 怎么带进闭包 | `CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:14-17`：`local_28/local_20` = 闭包里的 `attackDirection` → `ResolveAttackAnimationEffects(card, anchor, &local_28, 0)`（ILSpy 的字段名就叫 **`attackDirection`**，`CardScript.cs:100-102`） |
| 那个 Vector3 是什么 | `decomp_full/BattleManager__GetBoardPosOfCard.c:22-43` |
| **到底是 3 分量还是 x/z** | 🔴 **VA 级反汇编 `0x180965BC0`**（`工具/rva_of_method.py` + `工具/disasm_va.py`，本件现跑）：<br>`movsd xmm0, qword ptr [rax]`（拷 x、y）· `mov eax, dword ptr [rax+8]`（拷 z）· `movsd qword ptr [rbx], xmm0` · `mov dword ptr [rbx+8], eax` ⇒ **12 字节整抄**。<br>⇒ ILSpy 那版 `x = position.x; z = position.z;`（`BattleManager.cs:47695-47710`）是它**丢 store** 的老毛病（与本文件里「ILSpy 丢 `= true` 写点」「ILSpy 丢实参常量」同族）。 |
| 取值时 y 是多少 | 原版场上卡的卡根在**地面**：`MinionManager__FillMinionPositions.c` 把 y **显式写成 0**；督军走 `GetHeroBoardPos(局部位 + Vector3.zeroVector)` ⇒ 也 ≈ 0 |

### 2·3 消费端：`m_Velocity` 与 `m_AmplitudeGain`

`decomp_full/CameraShakerManager._DoShake_d__33__MoveNext.c`（全文 152 行，本件亲读）：

```
:30-37   if (!manager.shakesEnabled) return;            // 闸（G12 已接成 CardFeel.ShakesEnabled）
:69      fVar11 = Vector3.Magnitude(direction)           // 0x1805063C0 = sqrt(x²+y²+z²)（已 VA 反汇编核对）
:72-75   m_Velocity    = Vector3.Normalize(direction)     // 存 iterator+0x60
:79      m_AmplitudeGain = fVar11 * amplitude             // definition+0x38 = |dir| × amplitude
:109-116 delay 夹到 [0, DAT_1834b2d94]
:142-145 GenerateImpulseWithVelocity(source, &m_Velocity)
```

`CameraShakePreset__Play.c:19-23` 把 preset 的 8 个字段按实参顺序送进 `DoShake`；
`OverwriteCameraShakePreset__GetModifiedPreset.c:87-119` 证明 `SetDirection`（写 `+0x38..0x40`、置位 `+0x34`）与
`SetAmplitude`（写 `+0x28`、置位 `+0x24`）**都是原样覆盖 preset 的对应字段**，没有归一 / 没有缩放。
`SetDirection` 的**唯一**调用点 = `CardScript__ResolveAttackAnimationEffects.c:54`（全库 `grep -l SetDirection` 只命中它自己与该定义）。

### 2·4 原版场地根在 x = 100（决定 `|direction| ≈ 101`）

| 判据 | 内容 |
|---|---|
| 场景层级 | `bundle_scenes_scenes_battlearena1/Transform/`：`Transform_1300`（根，原点）→ `Transform_1301` = `BattleBoardElements` **localPosition (100, 0, 0)** → `1319/1315` → 两片 `MinionArea`（1309 z=−6.655 / 1314 z=+1.043） |
| 相机 / impulse 源 | `Transform_1366`（`CinemachineImpulseSource` + `CinemachineImpulseListener` 同一 GameObject 539）local (0, 2.222, −13.572)，**父 = 1301** ⇒ 世界 x ≈ 100，与相机同一处 ⇒ `DistanceDecay = 1`、`distance/speed ≈ 0` |
| 原版代码自己也这么说 | `DAT_1834b2e08 = 100.0` 被当「场地根 x」用（`资料/普查产出_1012/H8_取景x半与设置面板行.md:33`）；`CombatCameraZoom.cs:195` 记同一条 |
| 我们这一侧 | `Editor/BattleScene.cs:46667` `const float ArenaOriginX = 100f` + `:46875` `arenaGo.localPosition = (-ArenaOriginX, 0, 0)` |

---

## 六、屏幕通道映射表（世界 x/z ↔ 屏幕 x/y，逐格给判据）

> 这张表是 ② 的**判据本体**：不做这张表就没法判断「原版那一下在屏幕上长什么样」。

| 原版世界轴 | 屏幕上是什么 | 我们世界轴 | 换算 | 判据出处 |
|---|---|---|---|---|
| **x** | **屏幕水平**（+x 朝画面右） | x | **差一个常数 +100**：`orig.x = our.x + 100` | 相机与棋盘同挂在 `Transform_1301`（x=100）下、朝向 +z；`H8:33` 的 `100.0` 字面量；我们建场时整体 −100（`BattleScene.cs:46875`） |
| **y** | **屏幕竖直**（+y 朝画面上） | y | **同向、但基准不同**：原版卡根 = 地面（y≈0），我们的卡根 = 卡心（`ArenaSlots.RootPosition` 补半卡高 ≈ +0.38） | `MinionManager__FillMinionPositions.c`（y 显式写 0）· `BattleManager__GetHeroBoardPos.c`（+ `Vector3.zeroVector`）· `CardPresentation/Board/ArenaSlots.cs` `RootPosition` 的注释 |
| **z** | **景深**（正交相机下**屏幕上不动**） | z | 同值同向，**丢掉** | ⚠️ **这是我们的 2D 折算，不是原版句子**：原版 z 位移会变成「朝/背镜头」，正交相机下不可见（`CardFeel.cs:490` 那条「软连接」自 2026-09-14 起就是这么记的） |
| 旋转后 `up` 的 **`.x`** | **屏幕水平分量** | 我们世界 x 的位移 | `ShakeScreenDirOf(direction).x` | `LegacySignalSource::GetSignal`（RVA 0xA2B350，ISIL 现读）：`pos = FromToRotation(Vector3.down, m_Velocity) * pos` |
| 旋转后 `up` 的 **`.y`** | **屏幕竖直分量** | 我们世界 y 的位移 | `ShakeScreenUpOf(direction)`（= `ShakeScreenDirOf(...).y`） | 同上（2026-10-23 R9 已坐实，本件不改它的数值） |
| 旋转后 `up` 的 **`.z`** | 景深分量 | —— | 丢掉 | 同上（见 z 那一行的「我们的折算」） |

**三个方向的具体取值**（本件断言逐条钉住）：

| 输入 `direction` | `ShakeScreenDirOf` = (`x` 水平, `y` 竖直) | 来源 |
|---|---|---|
| `(0, 0.2, 0.2)`（`Shake Hit Small` preset） | `(0, −0.70711)` | preset 真件 |
| `(0, −1, 0)`（`Shake Land on ground`） | `(0, +1)` | preset 真件 |
| `(0, 0, 2)`（`Shake Forward Pull`） | `(0, 0)` | preset 真件（纯景深 ⇒ 屏幕上不动） |
| **`(100.82, 0, −6.655)`**（我方 5 号槽的棋盘坐标） | **`(−0.99783, 0)`** | 原版挨打那一路：`v = dir/|dir|`、`v.y = 0` ⇒ `R·up = (−vx, 0, −vz)` ⇒ **纯水平、竖直恒 0** |

> ⚠️ 表里最后一行有个**反直觉但必须照抄**的后果：棋盘坐标的 x 恒为 **+100 左右**（两侧都是），
> ⇒ 屏幕水平分量**恒为负**（`≈ −1`）⇒ 原版两侧挨打时镜头都朝**画面的 −x 方向**抖，与「被打的一方在左还是在右」无关。
> 这是「拿坐标当方向」这个原版写法自带的性质，⛔ 不是我们引入的，别「修正」。

---

## 三、没查清的部分（逐条写「还差什么」）

1. 🔴 **量级没真机核过（这是本件唯一的实质悬空）**。按机器码算：`m_AmplitudeGain = |棋盘坐标| × 2/4/8 ≈ 202 / 405 / 810`，
   信号峰值 0.0745 ⇒ 原版世界单位峰值 ≈ 15 / 30 / 60（相机在棋盘平面的可见高 = **14.77**）；
   折到我们这套单位（屏高 = 10）≈ **10.2 / 20.3 / 40.7**（= 20.380 / 40.760 / 81.519 × 噪声峰 0.5 × |屏幕方向| 0.99783），即 **1 / 2 / 4 个屏高**。而 preset 自带那版只有
   `2.0 × 0.28284 = 0.566`（≈ 3 px）—— **差 ~357 倍**。
   * 本机能拿到的证据**全都支持「棋盘在 x≈100」**：场景 Transform 层级、相机/impulse 源的世界位置、原版代码自己那个 `100.0` 字面量。
   * **还差什么**：一次真机（原版已关服 ⇒ 只能看录像/实拍；或我们的 player 接上 `ShakeCameraForAttack` 后看像不像）。
   * **所以本件把换算收在 `OrigBoardPosOf` 一个函数里**：若真机验收判定「不该 +100」，**只改那一行**，⛔ 别动幅度表 2/4/8。
2. 🟡 **`ShakeHitSmall` 的时间轴与 direction 的耦合**：原版这一档只覆盖 `direction` 与 `amplitude` 两个字段，
   其余五项（frequency / attack / sustain / decay / rawSignal）都取 preset 自带值 —— 这一条 `GetModifiedPreset.c:76-119`
   **逐格核过**（`+0x14/0x18/0x1c/0x20/0x24/0x28/0x30/0x38` 那 8 个写点哪些带开关），**判据是完整的**；
   但「`amplitude` 覆盖成 8 之后 `rot = SlerpUnclamped(identity, rot, gain)` 在 gain ≫ 1 时的外插形状」**没实测**
   （我们 `ShakeOrientation` 照公式实现；gain ≈ 800 时外插角会到几十度 —— 这是量级问题的另一面，同 §三·1）。
3. 🟡 **`_ResolveAnim_d__466` / `_ResolveTraitAnim_d__465` 那两处 `GetBoardPosOfCard` 的用法没查**：
   本件只核了「攻击」那一条链。另外两条把坐标喂给什么（大概率是特效生成点，属**坐标**语义）**没逐句读** ——
   不影响本件（方向链只此一处），但如果将来有人怀疑「坐标当方向」是不是普遍写法，要回去看那两处。
4. 🟡 **`unitsShake` / `ResolveUnitShake` / `ReturnToDeck` 的调用点**（上一路 `B9_W2FEEL` 留的）**本件没碰**：
   它们的接线口在 `Battle/BattleDriver.cs`，本波归 W10。

---

## 四、改动清单

### 4·1 `Assets/CardPresentation/Core/CardFeel.cs`（**零行号漂移**：全部 1:1 替换 + 类尾追加）

| # | 位置（改后行号） | 改动 | 行数 |
|---|---|---|---|
| 1 | `:1210` / `:1214` | 注释就地订正：`DAT_1842da2a8+0xb8` = `Vector3.zeroVector`（`dump.cs` 的 `zeroVector // 0x0`），**不是**「读不出名字」 | 5→5 / 1→1 |
| 2 | `:1220` | `PushBackDir` 的 doc：退路改成 **`Vector3.zero`** | 1→1 |
| 3 | `:1226` | `if (len <= PushBackDegenEpsilon) return Vector3.zero;`（原 `Vector3.up`） | 1→1 |
| 4 | `:1429` / `:1433` / `:1434` / `:1452` / `:1453` | `ShakeCamera`：`screenUp` → **`screenDir`**（两通道）；`LastShakeVertical = (LastShakeScreenDir = screenDir).y`；位移 `d = screenDir * (worldAmp * env * noise)` | 各 1→1 |
| 5 | `:1647` / `:1686` | `SettleShake` / `ResetShakePhases` 一并复位新读数 `LastShakeScreenDir` | 各 1→1 |
| 6 | `:4059-4068` | 类尾 #16 注记订正：`GetBoardPosOfCard` **不是** `(x,0,z)`（VA 级 12 字节整抄）；并写明水平通道已接 | 10→10 |
| 7 | `:4100` | 🆕 `public static Vector3 ShakeScreenDirOf(Vector3)` —— 原版 `FromToRotation(down,v)·up` 的 **x + y** 两条投影（z 丢掉） | 新（类尾） |
| 8 | `:4118` | 🆕 `public static readonly float OriginalArenaOriginX = 100f`（`static readonly` 而非 `const`：`Unclassified()` 会把未登记的 `public const float` 判红，而往 `Catalog` 插行会让下游账漂） | 新（类尾） |
| 9 | `:4128` | 🆕 `OrigBoardPosOf(Vector3)` —— 我们的坐标 → 原版棋盘坐标（x+100 · y 归 0 · z 不动） | 新（类尾） |
| 10 | `:4135` | 🆕 `LastShakeScreenDir`（自检读数口） | 新（类尾） |
| 11 | `:4153` | 🆕 `ShakeWorldAmplitudeForAttack(Vector3, int)` = `|origBoardPos| × 2/4/8 × ShakeBandPeak × (73.1÷108)` | 新（类尾） |
| 12 | `:4177` | 🆕 `ShakeCameraForAttack(cam, hudRoot, ourBoardPos, 攻击方近战, delay, extraCam, phaseSeed)` | 新（类尾） |

**既有 API 一个签名都没改**（`ShakeCamera` / `ShakeCameraForHit` / `ShakeCameraForLand` / `ShakeScreenUpOf` /
`PushBackDir` 的形参表逐字未变）；`ShakeCamera` 的行为**只有**「方向带世界 x 分量时多出一段水平位移」，
而现有 15 个调用点传的 direction 的 `.x` 恒 0（`(0,0.2,0.2)` / `(0,−1,0)` / `(0,0,2)` 那一族）⇒ **线上行为逐位不变**。

### 4·2 `Assets/CardPresentation/Editor/BattleScene.cs`（**零行号漂移**）

| # | 位置（改后行号） | 改动 | 行数 |
|---|---|---|---|
| 1 | `:13439` | `PushBackDir(x,x) == Vector3.up` → **`== Vector3.zero`**，文案写清判据（`zeroVector // 0x0`）与「退回 up ⇒ 红」 | 2→2 |
| 2 | `:24175` | 行内追加 `W11ShakeAssert.Check(cam, hudRootT, ArenaOriginX, Check);`（断言体在**文件尾**，取「把 `Check` 当委托传出去」这条路而不是在 `Run` 中间插行） | 1→1 |
| 3 | `:48506` | 🆕 `static class W11ShakeAssert`（7 条断言，见 §4·3）；**追加在文件尾** ⇒ 上面的行号一个不动 | 新（+122 行） |

### 4·3 断言（7 条，全部「先前提硬检查、再结论」）

| # | 断言 | 灭自证 / 判别式 |
|---|---|---|
| ① | `CardFeel.OriginalArenaOriginX == BattleScene.ArenaOriginX == 100`（**前提**） | 只改一处 ⇒ 红（否则方向静默差 100 个单位） |
| ② | `OrigBoardPosOf((0.82, 0.38, −6.655)) == (100.82, 0, −6.655)` | 漏 +100（幅度差 ~353×）或照抄我们的 y（凭空多一个竖直分量）⇒ 红 |
| ③ | 既有三档的竖直分量 `−0.70711 / +1 / 0`，且它们的**水平分量必须恒 0** | 把方向写成 `direction.normalized` 直用（跳过旋转）⇒ 竖直全变 ⇒ 红 |
| ④ | 棋盘坐标 `(100.82,0,−6.655)` ⇒ 屏幕方向 `(−0.99783, 0)`（**水平 ≈ −1、竖直恒 0**） | 只取 `.y`（2026-10-27 前）⇒ 原版这一档**完全不震**，红在 `.x` |
| ⑤ | `ShakeWorldAmplitudeForAttack` 三档 = `|dir| × 2/4/8 × 0.149 × 73.1/108`（20.380 / 40.760 / 81.519） | 用 preset 的 0.28284 顶替 ⇒ 小 ~357× ⇒ 红 |
| ⑥ | **灭自证**：棋盘那一路的增益是 preset 自带方向的 **> 300 倍**（实测 ≈ 357） | 悄悄把 preset 方向顶回去 ⇒ 变成 1.0 ⇒ 红 |
| ⑦ | **端到端**：`ShakeCameraForAttack(..., 近战 5)` ⇒ 相机当场位移 `Δ = (−0.997832 × amp4 × n0, 0, 0)`；HUD 根与相机**同向等量**；随后 `ShakeCameraForHit` 的 **水平分量 = 0**（两通道互不串）；收尾相机回原位 | 旧实现（只乘 `Vector3.up`）⇒ Δx 恒 0 ⇒ 红；HUD 只跟竖直 ⇒ 横抖时 HUD 跟着飞 ⇒ 红 |

> ⑦ 里的噪声用**固定种子 4242**（同既有做法），并先断言 `|n0| > 0.05` —— 否则「噪声恰好过零」时这条会空转成绿。

### 4·4 账补丁 `Unity/资料/完全复刻/账/_parts/B9_W11SHAKE.tsv`（3 行，11 列，LF）

`特效动画.tsv` 的三行**整行替换**（键 = (原版类, 方法)，`merge_ledger_parts.py:186-235` 现读）：

| 键 | 改了什么 |
|---|---|
| `CardScript` / `ResolveAttackAnimationEffects(...)` | 判据要点补 VA 级「3 分量」订正 + 场地 x=100；落点补 6 处新成员行号；差异改成「方向链已接 + 还差调用点 + 量级待真机核」；轮次 → `B9_W11SHAKE` |
| `CameraShakerManager` / `DoShake(...rawSignalToUse)` | 判据要点补全「`m_Velocity = normalize` / `m_AmplitudeGain = |dir| × amplitude` / delay 夹取 / shakesEnabled 闸」四格；落点补 `ShakeScreenDirOf` 与 `ShakeCameraForAttack` |
| `CardScript` / `ReceiveAttackAnim(...)` | 判据要点补「两点重合退路 = `Vector3.zero`（`zeroVector // 0x0`）」；落点 = `CardFeel.cs:1219-1226` + `BattleScene.cs:13439` |

`check_patch_conflicts.py` ⇒ **没有跨补丁重复键（2 份补丁）**。合并时请**整份 `--only B9_W11SHAKE`**（3 行都是替换，不是新增）。

---

## 五、顺手发现（都能复核；⛔ 都不在本件白名单外改过）

1. 🔴 **`GetBoardPosOfCard` 的 ILSpy 版与机器码不一致**（`(x,0,z)` vs 12 字节整抄）。这是「ILSpy 丢 store」的又一例；
   **同一份 ILSpy 输出在 `ReceiveAttackAnim` 上也丢了退化分支的整个 body**（`CardScript.cs:13075-13077` 只剩一个空 `if`）。
   ⇒ 口径：**凡「ILSpy 写得很简洁」的地方，先怀疑它丢了 store，用 `.c` / VA 反编译复核**
   （与 CLAUDE.md 那条「ILSpy 会把实参常量反编译成 `default`」同族）。
2. 🟡 **`ShakeWorldAmplitude`（preset 那一路）与本件新增的 `ShakeWorldAmplitudeForAttack` 差 ~357 倍** ——
   两者都「照原版」，只是**输入不同**（0.28284 vs 101）。⛔ 别把其中一个「统一」成另一个。
3. 🟡 **`ShakeCamera` 的位移 `d` 现在可能是任意平面向量**，而 HUD 的反补算式
   `X' = C + d + M·(X − C)` 与 `OnKill` 的逐位还原**都只依赖 `d` 本身** ⇒ 水平通道不需要改那两处
   （断言 ⑦ 的第三条就是在钉这一格）；但**将来若有人给 `d` 加 z 分量**，HUD 那条会静默偏 —— 已在该断言里加 `Δz < 1e-5`。
4. 🟡 **`CardFeel.Unclassified()` 的坑**（本件踩到一次设计决策）：新增 `public const float` 而未登记 `Catalog` ⇒ 自检红；
   而往 `Catalog` 插一行会让下游 `文件:行号` 全漂。本件改成 `public static readonly float` 绕开（语义相同）。
   ⇒ 建议记账：**「运行时新常量」与「Catalog 登记」这对矛盾**长期看要么改成 `static readonly`，要么给 `Catalog` 加个尾部区。
5. 🟡 **`Editor/BattleScene.cs` 里的 `Check` 是 `Run` 的局部函数** ⇒ 任何「新增断言块」都天然带行号漂移风险。
   本件用的**委托外传 + 文件尾类**这条路可复用（`W11ShakeAssert.Check(cam, hudRoot, ArenaOriginX, Check)`），
   建议后续同类批次照抄。
6. 🔴 **给调度台的合并提醒（不是我的补丁，但会撞）**：`check_patch_conflicts.py` 现读报出**一条跨补丁重复键** ——
   `DeckGeneralInfoDemo.WarlordInfoButtonClick()` 同时出现在 `B9_W15CVIEW.tsv:6`（状态 C）与 `B9_W8PREB.tsv:4`（状态 A），
   按字典序 `W8PREB` 后合 ⇒ **A 会盖掉 C**。合并前请先按那条工具打印的顺序逐份 `--only <完整文件名>` 合。
   （本件 `B9_W11SHAKE.tsv` **不在这条冲突里**。）
