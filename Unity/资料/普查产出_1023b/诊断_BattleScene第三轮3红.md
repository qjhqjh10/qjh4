# BattleScene 第三轮 3 红 —— 只读诊断（2026-10-23）

> 输入：`_tmp_view/t67_sync/battle4.log`（`=== 合计：4504 通过 / 3 失败 ===`）。
> 本轮改动：战斗表现尾巴（R9 竖直符号 / `OrientationNoise` / notPeak + 新账-70）、γ 手牌数框高、W62 断言修复、引擎 B-16/B-31/B-17。
> 红线：本报告只读产出，**没改源码/测试/台账、没跑 Unity、没动 git**。

## 〇、一句话结论

| # | 红 | 分类 | 一句话 |
|---|---|---|---|
| 1 | R9 尾账 灭自证（t=0 旋转不是恒等） | **α 断言写错** | `Quaternion.Angle` 有 **0.162°** 的盲区（dot 阈值 `1−1e-6`），而这条测的角度只有 **0.0396°** ⇒ **结构上永远红**。实现没问题。 |
| 2 | 场上四个数值画在屏幕上（4 格全 0px） | **δ 夹具前提（+ 次生 α）** | 整轮 **渲染管线初始化失败**：首个 `Camera.Render()` 抛 `InsufficientExecutionStackException`，之后 **82 次** `Blitter is already initialized` ⇒ 全程没出过图（73 张截图逐字节相同）。测量本体根本没被执行到。**不是数值层回归。** |
| 3 | 旋转 gain 与包络各乘一次 | **α 断言写错** | 同 #1 的盲区：`qQuart = 0.125° < 0.162°` ⇒ `Angle` 返回 0；而 `qHalf = 0.25°` 在盲区外，所以打印出正确的 0.250。 |

**必须修实现的：没有。** 三条全部指向断言/夹具（#2 还需要复跑确认渲染恢复）。

---

## 一、红 1 —— `R9 尾账 灭自证：默认信号带 OrientationNoise ⇒ t=0 的旋转不是恒等（0.000°）`

**日志**：`battle4.log:61553`
```
BT    ✗ ★ R9 尾账 **灭自证**：默认信号带 `OrientationNoise` ⇒ t=0 的旋转**不是恒等**（0.000°）｜🧨 把 rot 那一路删掉（只平移）⇒ 这条红
  at BattleScene/<>c__DisplayClass29_0:<RunCore>g__Check|3 ... BattleScene.cs:524
  at BattleScene:RunCore () ... BattleScene.cs:19833
```
前一条（HUD 刚性变换 / 相机真的转了）都是 ✓（`battle4.log:61527`、`:61540`）。
🔑 **`:61540` 那条自己就是盲区的实证**：它打印「与参考值夹角 **0.000°**」，而按 `CardFeel.cs:1182`
（`camTr.rotation = camHomeRot * q`）与 §下面的算式，相机那一刻实际转了 **0.0396°** ——
打印的 0.000 是 `Quaternion.Angle` 的盲区，不是没转。
⚠️ 反过来说：**这条「相机真的转了」本身也抓不到「rot 那一路被删掉」**（删了也是 0.000 ⇒ 绿）——
`Angle` 的盲区让 `19838` / `19833` / `19923` / `19927` / `20168` 这几条一起失效，见 §四。

**分类**：**α（断言写错）** —— 不是 β、不是 γ、不是 δ。

**根因**：断言用的量角器 `Quaternion.Angle` 在小角度下**恒返回 0**。

- 判据（本机 Unity 6000.3.23f1 的 `UnityEngine.CoreModule.dll`，IL 现读）：
  - `Quaternion.IsEqualUsingDot` IL = `02 22 EF FF 7F 3F FE 02 2A`
    = `ldarg.0; ldc.r4 0x3F7FFFEF(=1.0f−1e-6f); cgt; ret` ⇒ **`dot > 1 − 1e-6`**。
  - `Quaternion.Angle(a,b)` IL（rid 0x222D）= `IsEqualUsingDot(min(|Dot|,1)) ? 0.0f : Acos(dot)*2*Rad2Deg`。
  - ⇒ **夹角 < `2·acos(1−1e-6)` = 0.16206° 时，`Quaternion.Angle` 返回精确的 `0.0f`**。
  - ⚠️ 源码注释 `BattleScene.cs:19842-19843` 写「`Quaternion ==` 有 ~1e-6 dot 阈值 …… ⇒ 只比角度」——
    **`Angle` 用的是同一个 `IsEqualUsingDot`，同一个盲区**。这条绕法不成立。

- 这条测的实际角度（现算）：
  - `CardFeel.ShakePhaseOffset(4242)` = **708.8521205**（`CardFeel.cs:1414-1418`；用 `System.Random` 复算，
    先与 .NET 已知值校准：`Random(0).Next()=1559595546`、`Random(1).Next()=534011718` 两处一致）。
  - `ShakeOrientationRef(0, 708.85, 0.05, 2.0*0.28284)`（`BattleScene.cs:38640-38651`）：
    euler = **(0.0683°, −0.0112°, +0.0109°)**，幅值 **0.0701°**；× gain 0.56568 ⇒ **0.0396°**。
  - 0.0396° ≪ 0.162° ⇒ `Angle` 返回 0 ⇒ 打印 `0.000°` ⇒ `> 0.01f` 判红。

- **更强的一条：这条断言在默认 gain 下【结构上不可能通过】**。
  三条带峰值之和：X `0.5×(0.09+0.14+0.15)=0.19°`、Y `0.074°`、Z `0.021°`；
  乘 `ShakeHitRotationGain` 0.56568 ⇒ 三轴上限 (0.1075°, 0.0419°, 0.0119°)，
  合成最大 **0.116° < 0.162°**。⇒ 无论相位、无论实现对不对，`Quaternion.Angle` **永远读到 0**。

- **实现本身是对的**（不是 β）：
  - ② 单带 0.5° 那条 ✓（`battle4.log:61885`）—— 0.5° 在盲区外，`Angle(qOne, Euler(0.5,0,0)) < 1e-3` 真能测。
  - ③ 前半 `gain 0.5 ⇒ 0.250°` 实测正好 0.250 —— 证明 `SlerpUnclamped` 真的按 gain 缩放（0.5°→0.25°）。
  - `ShakeOrientation`（`CardFeel.cs:1528-1553`）逐句对判据：
    `T = t·frequencyGain + phase` → 三轴各 `Σ A·0.5·sin(2π·f·T)` → `Euler(rx,ry,rz)`（度）
    → `SlerpUnclamped(I,q,rotationGain)` → `SlerpUnclamped(I,q,envelope)`，与
    `R9_rawSignal_算式.md` §2·5/§2·7 的两处 `SlerpUnclamped` 一致。

**最小修法**（只改断言，`BattleScene.cs:19828-19847`）：
在小角度上换成**不设阈值的量角器**，例如加一个自检专用静态方法：
```csharp
// 小角度专用：Quaternion.Angle 在 dot>1-1e-6（夹角<0.162°）时返回 0，量不了屏震这种 0.0x° 的旋转
static float AngleDegTiny(Quaternion a, Quaternion b)
{
    Quaternion d = a * Quaternion.Inverse(b);
    return 2f * Mathf.Atan2(new Vector3(d.x, d.y, d.z).magnitude, Mathf.Abs(d.w)) * Mathf.Rad2Deg;
}
```
然后把 `19844` 的 `Quaternion.Angle(identity, q0) > 0.01f` 换成 `AngleDegTiny(...) > 0.01f`
（期望值 0.0396°，留 4× 余量）。同一个盲区还影响下面几条（见 §四）。

**风险**：极低（纯断言）。修完这条才真正具有「把 rot 一路删掉 ⇒ 红」的能力（删掉后 `AngleDegTiny = 0`）。

---

## 二、红 3 —— `旋转 gain 与包络各乘一次：gain 0.5 ⇒ 0.250°、再乘包络 0.5 ⇒ 0.000°（应 0.25° / 0.125°）`

**日志**：`battle4.log:61904`
```
BT    ✗ ★ 旋转 gain 与包络**各乘一次**：gain 0.5 ⇒ 0.250°、再乘包络 0.5 ⇒ 0.000°（应 0.25° / 0.125°）｜🧨 只乘一次或都不乘 ⇒ 红
  at BattleScene:RunCore () ... BattleScene.cs:20156
```

**分类**：**α（断言写错）** —— 与红 1 同一个盲区。

**根因**：
- 被测值：`qHalf = ShakeOrientation(0.25, 0, 1, oneOr, 0.5, 1)` ⇒ 0.5° × 0.5 = **0.25°**；
  `qQuart = ShakeOrientation(..., 0.5, 0.5)` ⇒ 0.5° × 0.5 × 0.5 = **0.125°**。
- `Quaternion.Angle(I, 0.25°)` = 0.250（盲区外，**实测打印正确**）；
  `Quaternion.Angle(I, 0.125°)` **= 0.000**（0.125° < 0.162°，见 §一 的 IL 判据）。
- 打印出来的两个数（0.250 / 0.000）与「实现正确 + 量角器盲区」**逐位吻合**；
  和「实现只乘一次」也对不上（那样 qHalf 会是 0.500），和「都不乘」更对不上（0.500）。
- `SlerpUnclamped` 本身没有小角度阈值：托管侧 IL 只是 `ldarg.0/1/2; call 0x060021A1; ret`（native）。

**最小修法**：`BattleScene.cs:20154-20160` 同样改用 `AngleDegTiny`；或把标定点抬到盲区外
（gain 1 / 0.5 / 0.25 用 1° 输入：1° → 0.5° → 0.25°，全部可测），并保留「两次各乘一次」的判别力。

**风险**：极低。注意**只改这一条不够** —— 红 1 与 §四那几条同一个根。

---

## 三、红 2 —— `场上四个数值真的画在屏幕上 …… [stat1:0px] [stat2:0px] [stat3:0px] [stat4:0px]`

**日志**：`battle4.log:8663`（前面 8620-8662 是同一原因的异常刷屏）
```
BT    ✗ ★ 场上四个数值**真的画在屏幕上**（改那一格的值 ⇒ 那一格那个方窗里的像素必须变；……）
      —— 差异不足的格： [stat1:0px] [stat2:0px] [stat3:0px] [stat4:0px]（量到 4/4 格 · 1920×1080 · 卡宽 394px · 窗 ±39px）
  at BattleScene:CheckBoardStatStack ... BattleScene.cs:38132
```

**分类**：**δ（夹具前提被破坏）+ 次生 α（断言缺「渲染器活着」的前提）**。
**不是 β（数值层没画）、不是 γ（本批回归）。**

**根因（两层）**：

1. **整轮渲染管线是死的**（这是 0px 的直接原因）。
   - 首个 `Camera.Render()` 就抛异常：`battle4.log:4828`
     ```
     InsufficientExecutionStackException: Insufficient stack to continue executing the program safely.
       at UnityEngine.ScriptableObject.CreateScriptableObjectInstanceFromType
       at UnityEngine.Rendering.VolumeManager.EvaluateVolumeDefaultState
       at UnityEngine.Rendering.VolumeManager.Initialize
       at UnityEngine.Rendering.Universal.UniversalRenderPipeline..ctor
       at UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.CreatePipeline
       at UnityEngine.Rendering.RenderPipelineAsset.InternalCreatePipeline
       ...
       at UnityEngine.Camera.Render()
       at BattleScene:Shot(Camera, String) ... BattleScene.cs:38234   ← 第一次 board3D.Render()
       at BattleScene:RunCore() ... BattleScene.cs:809
     ```
   - 之后 **82 次** `Exception: Blitter is already initialized ...`
     （首条 `battle4.log:7278`；`grep -c "Exception: Blitter" = 82`）——
     管线每次渲染都被重新创建、每次都在 `UniversalRenderPipeline..ctor`（`Blitter.Initialize`）里失败
     ⇒ **本次进程再没渲出过一帧**。
   - **实据：整轮 73 张截图逐字节相同**。`_tmp_view/battle/` 里 93 个 PNG，其中 **73 个 md5 全是**
     `8912490f47db34d8781506f033ef98e9`、大小全 **36,314 B**，mtime 全 **14:07**（= battle4 那一轮；
     `01_开局 / 02_… / 05_对局结束 / 06_攻击方式选择器` 同一张图）。剩下 20 个是更早轮次的旧文件。
   - ⇒ `CheckBoardStatStack` 的「基准图」和「改值后的图」是**同一张空白图**，四个窗 diff 必然全 0。
     `measured=4/4`、`cardPx=394px`、`half=±39px` 与通过轮次**完全相同**（见下），说明几何与探针都对，
     只是像素内容没渲出来。

2. **断言无法区分「数值没画」与「渲染器没出图」**（次生 α）。
   `BattleScene.cs:38108-38136` 直接拿两张渲染图比，没有任何「卡面真的画出来了」的前置
   （只有「四格都在画面里」的位置前提，`:38104`）。所以一次引擎级渲染故障会被伪装成「数值层缺陷」。

**证据：这不是数值层回归、也不是本批 γ/W62 引起**

- **历史**：同一轮会话里前两次跑同一条断言**都是绿**：
  - `battle.log:8423` `✓ …… 差异不足的格：无（量到 4/4 格 · 1920×1080 · 卡宽 394px · 窗 ±39px）`
  - `battle2.log:8687` 同上（`=== 合计：4482 通过 / 1 失败 ===`）
  - 几何（394px / ±39px）与 battle4 逐字相同 ⇒ 相机、卡、探针摆位都没变。
- **`CardView.cs` 自 12:09 起未再改动**（battle.log 12:58 / battle2.log 13:09 都晚于它）
  ⇒ 本轮「`SetQueue` / `F4` sortingOrder / 数值层」这类代码在**通过的那两轮里就是同一份**。
- **`CheckBoardStatStack` 相对 HEAD 零改动**：`git diff -U2 -- Editor/BattleScene.cs` 里
  对 `CheckBoardStatStack` / `StatInkProbe` / `RenderToTex` / `cardPx` **一处命中都没有**
  ⇒ 这条测试整个会话没被动过。
- **γ / W62 与渲染无关**：γ = 手牌数 TMP 字号（`已知γ_手牌数框高_执行报告.md`：x100→x10 的档位换算）；
  W62 = 断言扫描对象从世界壳换成 `PanelRect`（`W62断言修复_执行报告.md`）。都不碰相机/URP/数值层。
- **「RunCore 帧太大撑爆栈」不成立**（顺手排除了这个猜测）：从 14:06 编出的
  `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` 现读 —— `BattleScene.RunCore` =
  IL 536,547 B / 5,068 个局部变量 / **估算栈帧 ≈ 45.3 KB**（x64，按局部类型求和）。离爆栈很远。
- **本轮是唯一一次**：`_tmp_view` 全库只有 `battle4.log` 出现过这两个引擎异常
  （`grep -rl` 命中：`battle4.log` + 两个二进制 `global-metadata.dat` + `Blitter.cs` 源码，
  其余任何一轮日志 0 次）。

**最小修法**：

1. **给这条断言补一条「渲染器活着」的前置**（`BattleScene.cs:38108` 之后、量 diff 之前）：
   在 `basePx` 上数「卡面矩形内明显不同于相机背景色的像素」，要求 > 0；否则报
   「**渲染管线没出图（URP 初始化失败）**」而不是「数值没画」。
   （`Shot` 里已有 `lit` 计数的同款写法，`BattleScene.cs:37986-37991` 可直接照抄。）
   这样同类引擎故障会以**可辨识的夹具红**出现，不会误导人去改 `CardView`。
2. **复跑一次** `BattleScene.Run`。预期：管线正常初始化 ⇒ 像 battle.log/battle2.log 一样绿。
3. 若复现：把**进程的第一次 `Camera.Render()` 挪到浅栈**（例如 `Run()` 里、`RunCore()` 之前渲一次
   1×1 RT 触发 URP 初始化），避开 `Shot → board3D.Render()` 这条深栈首渲路径。

**风险**：若不做第 1 步、直接按「数值没画」去改 `CardView`/队列，会改坏本来正确的层（前两轮已证明它是对的）。
第 1 步只加前置，不改被测实现，风险最低。

---

## 四、顺手发现（找几条报几条，⛔ 均未改）

同一个 `Quaternion.Angle` 盲区还让这几条**变成恒绿（假绿）**，它们都在本轮新增的 R9 断言里：

| 位置 | 断言 | 为什么恒绿 |
|---|---|---|
| `BattleScene.cs:19838-19840` | 相机 `Angle(camRot, camHomeRot*q0) < 0.05°`，文案写「🧨 不施加 `OrientationNoise` ⇒ 这条红」 | 两侧差 0.0396° < 0.162° ⇒ `Angle = 0` ⇒ **删掉 rot 也绿**（文案承诺是假的） |
| `BattleScene.cs:19833-19837` | HUD 根旋转 `Angle(hudRot, M0*hudHomeRotW) < 0.05°` | 同上 |
| `BattleScene.cs:20168-20180` | ⑤ 灭自证：6 个相位点上 `Angle(got, want) < 1e-2` | got/want 都是 ≤0.0925°（见下）⇒ 两边都读 0 ⇒ **轴序互换/少带也抓不到** |
| `BattleScene.cs:19923-19927` | 跑完「相机/HUD 旋转放回原位 < 1e-3°」 | 复位量与残留量都在盲区内 ⇒ 恒 0 |

（⑤ 的 6 个相位现算幅值：0.0843 / 0.0675 / 0.0180 / 0.0542 / 0.1634 / 0.0442°，
乘 gain 后 0.0925 / 0.0382 / 0.0102 / 0.0306 / 0.0477 / 0.0250°，**全部 < 0.162°**。）

建议：把 `AngleDegTiny` 放在 `BattleScene` 的工具区，**这四条 + 红 1 + 红 3 一起换掉**（一处判据）。

另：`CardView.cs:7947-7948` 也有 `Quaternion.Angle(...) > 0.01f` 的用法，量级未查证；
若那里也是 0.0x° 级，属同一个盲区（本条只是提醒，未核）。

---

## 五、判据出处

- `Quaternion.Angle` / `IsEqualUsingDot`：`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll`
  （`dotnet` 元数据现读，`Quaternion.IsEqualUsingDot` IL = `02 22 EF FF 7F 3F FE 02 2A`；`Angle` rid 0x222D）。
- `ShakeOrientation` 算式：`Core/CardFeel.cs:1528-1553`；数据 `CardFeel.cs:428-441`；
  判据 `资料/普查产出_1023b/R9_rawSignal_算式.md` §2·5 / §2·7。
- 参考实现 `ShakeOrientationRef`：`Editor/BattleScene.cs:38640-38651`。
- 数值像素断言：`Editor/BattleScene.cs:38016-38136`；渲染口 `:244-254`（`RenderToTex`）；截图口 `:38192-38276`。
- 渲染故障：`_tmp_view/t67_sync/battle4.log:4828`（首个异常）、`:7278` 起 82 次 Blitter、`:8663`（红）。
- 历史对照：`battle.log:8423` ✓ / `battle2.log:8687` ✓（同几何 394px / ±39px）。
- 截图实据：`_tmp_view/battle/*.png` 73 个同 md5 `8912490f47db34d8781506f033ef98e9`、同 36,314 B、mtime 14:07。
