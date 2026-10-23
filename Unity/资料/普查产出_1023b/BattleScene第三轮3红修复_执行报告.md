# BattleScene 第三轮 3 红 · 修复执行报告（2026-10-23）

> 输入 = `诊断_BattleScene第三轮3红.md`（只读结论：红 1 = α · 红 2 = δ + 次生 α · 红 3 = α）。
> **本轮只改测试侧**（`Editor/BattleScene.cs`），**生产代码零改动**（`BattleDriver` / `CardFeel` / `CardView` / `RuleEngine/**` 一行没碰）。
> 红线遵守：**没跑 Unity**、**没动 git**、没改正本（只动了本线台账那一行）、`BattleScene.cs` **CRLF 未翻**。

---

## 〇、一句话结论

三条红**全部按诊断修完**：新增 `AngleDegTiny`（无阈值量角器）并替换 **8 处** `Quaternion.Angle` 判据
（3 条红涉及 4 处 + 同盲区假绿 4 处），其中 **2 处阈值 0.05°→0.01°**（只换量角器不够，见 §1·3）；
四数值像素断言加**「渲染器活着」前置** + `Run()` 里加**浅栈渲染预热**。
typecheck **0/0**；`BattleScene.cs` **39033/39033 行 CRLF**（bare LF = 0）。

---

## 一、红 1 / 红 3 —— `Quaternion.Angle` 的 0.162° 盲区（α）

### 1·1 新量角器 `AngleDegTiny`（`BattleScene.cs:38745`，工具区、`ShakeOrientationRef` 之前）

```csharp
static float AngleDegTiny(Quaternion a, Quaternion b)
{
    Quaternion d = a * Quaternion.Inverse(b);
    return 2f * Mathf.Atan2(new Vector3(d.x, d.y, d.z).magnitude, Mathf.Abs(d.w)) * Mathf.Rad2Deg;
}
```

- 取 `|w|` ⇒ 自动折掉 `q` / `−q` 双覆盖，结果落在 `[0, π]`，就是**最小夹角**。
- **不设任何 dot 阈值** ⇒ 0.0x° 的旋转量得出来。大角度下与 `Quaternion.Angle` 数值一致（差浮点末位）。
- doc 里写死了盲区判据：本机 `UnityEngine.CoreModule.dll` 的 `IsEqualUsingDot` IL
  = `ldc.r4 0x3F7FFFEF`（`1 − 1e-6`）+ `cgt` ⇒ **夹角 < `2·acos(1−1e-6)` = 0.16206° 时返回精确 0**。

### 1·2 逐处替换（行号为改动后的现读行号）

| 行 | 断言 | 原 | 现 | 阈值 |
| --- | --- | --- | --- | --- |
| `:19887` | HUD 根刚体变换的**旋转** | `Quaternion.Angle < 0.05f` | `AngleDegTiny < 0.01f` | **0.05 → 0.01** |
| `:19894` | 相机**真的转了** | `Quaternion.Angle < 0.05f` | `AngleDegTiny < 0.01f` | **0.05 → 0.01** |
| `:19903` | ★ 灭自证：t=0 旋转**不是恒等** | `Quaternion.Angle > 0.01f`（**恒 0 ⇒ 结构上永远红**） | `AngleDegTiny > 0.01f` | 0.01（信号 0.0396° ⇒ 4× 余量） |
| `:19986` | 相机旋转**放回原位** | `Quaternion.Angle < 1e-3f`（恒 0 ⇒ 假绿） | `AngleDegTiny < 1e-3f` | 1e-3（复位是逐位还原） |
| `:19990` | HUD 根旋转**放回原位** | 同上 | `AngleDegTiny < 1e-3f` | 1e-3 |
| `:20213` | ② 单带 0.5°（**不在盲区**） | `Quaternion.Angle(qOne,qWant) < 1e-3f` | `AngleDegTiny` | 1e-3（仅统一量角器） |
| `:20224/20225` | ③ gain 与包络**各乘一次**（红 3） | `Quaternion.Angle`（`qQuart` 0.125° 恒读 0 ⇒ **永远红**） | `AngleDegTiny` | 1e-2（不动） |
| `:20247` | ⑤ 灭自证 6 个相位 vs 参考实现 | `Quaternion.Angle`（两边都读 0 ⇒ **恒绿**） | `AngleDegTiny` | 1e-2（不动） |

顺带订正了 `:19897-19902` 那段**错的注释**（原文写「`Quaternion ==` 有 1e-6 阈值 ⇒ 只比角度」——
`Angle` 用的是**同一个** `IsEqualUsingDot` ⇒ 那条绕法不成立，诊断 §一 已证）。

### 1·3 为什么**只换量角器不够**（两条必须同时收紧阈值）

`:19887` / `:19894` 这两条的实际信号只有 **0.0396°**（t=0 / 默认信号 / gain 0.56568）。
把量角器换掉、阈值仍留 **0.05°** 的话：「HUD 不跟着转」「相机压根不施加旋转」这两种坏法
**差 0.0396° < 0.05°** ⇒ 照样绿（这正是它们 2026-10-23 假绿的原因）。
⇒ 阈值改成 **0.01°**（≪ 0.0396°，而实现与参考的浮点残差只有 ~1e-4° 级）。

### 1·4 期望值的现算（独立于实现，可复查）

- `ShakePhaseOffset(4242)` = 708.8521205（诊断已与 .NET 已知值校准）。
- `ShakeOrientationRef(0, 708.85, 0.05, 2.0×0.28284)`：euler = **(0.0683, −0.0112, +0.0109)°**
  ⇒ 幅值 **0.0701°** × gain 0.56568 = **0.0396°**（本轮用 python 复算，与诊断 §一 逐位吻合）。
- ⑤ 的 6 个相位合成量级：raw 0.0843 / 0.0675 / 0.0180 / 0.0542 / 0.1634 / 0.0442°
  ⇒ 乘 gain 后 **0.0477 / 0.0382 / 0.0102 / 0.0306 / 0.0925 / 0.0250°**（全 < 0.162°）。
- `qHalf` = 0.25°、`qQuart` = 0.125°。
- **数据核对**：`Resources/WarpforgeVFX/shake_presets.json` 的 `Warpforge 6D Shake.orientation`
  14 个数值与 `ShakeOrientationRef` 里写死的字面量**逐位相同**（5.83/0.09/9.17/0.14/57.17/0.15 ·
  1.8/0.059/11.35/0.041/54.17/0.048 · 2.38/0.017/10.52/0.009/63.76/0.016）⇒ 换量角器后
  「实现 vs 参考」的残差应该只有浮点末位，`0.01°` 的阈值余量很大。

### 1·5 修完之后的鉴别力（灭自证）

- 把 rot 那一路**整条删掉**（只平移）⇒ `:19894` 差 0.0396° > 0.01° ⇒ **红**；`:19903` 读 0 ⇒ **红**。
- HUD 不跟着转 ⇒ `:19887` 差 0.0396° ⇒ 红。
- `SlerpUnclamped` 只做一次 / 都不做 ⇒ `:20224` 读 0.5° ⇒ 红。
- 轴序 X/Y 互换 / 少一条带 / 相位不共享 `T` ⇒ `:20247` 的 `worstO` 远超 0.01° ⇒ 红。

---

## 二、红 2 —— 场上四个数值 0px（δ 夹具前提 + 次生 α）

### 2·1 前置：先证明「这一帧真的画出了像素」（`BattleScene.cs:38183-38212`）

在 `basePx` 拿到之后、量 diff 之前，数两种「画面真的存在」的证据：

| 量 | 定义 | 为什么 |
| --- | --- | --- |
| `litBase` | 与 `bc.backgroundColor` 的 RGB 差之和 > 0.02 的像素数 | 与 `DumpCam`（`Shot` 同款）**同一写法、同一阈值** |
| `structBase` | 与画面左下角 `basePx[0]` 的 8-bit 差之和 > 5 的像素数 | 防住「整张是**别的**纯色」那一档（纯色帧同样是「没出图」，而它可能恰好 ≠ `backgroundColor`） |

`renderAlive = litBase > 0 && structBase > 0`：

- **不成立** ⇒ `check(false, "★ **（前提）**数值渲图那几帧真的画出了像素（渲染管线活着） … ⇒ **渲染管线没出图** … **这一条红不是数值层缺陷**，跳过下面的像素差判据")`
  并把整段**像素差判据（含 4 次改值重渲）整块跳过**；
- 成立 ⇒ 照旧跑那 4 格像素差，**阈值一个字没动**（`diff < 12` 判红、`thr 0.05f`）。

⛔ **没有为了让红变绿去放宽任何阈值** —— 前提失败照样是一条**红**，只是把「引擎级渲染故障」与「数值层缺陷」分开打印。

### 2·2 浅栈预热：`WarmUpRenderPipeline()`（`BattleScene.cs:416`，由 `Run()` 在 `RunCore()` 之前调用，`:393-399`）

- 造一台临时 `Camera`（`enabled = false`、`clearFlags = SolidColor`、8×8 RT）+ **手工 `Render()` 一次**，
  只为触发 URP 的一次性初始化（`InternalCreatePipeline → UniversalRenderPipeline..ctor → VolumeManager.Initialize`）。
- 用完**立刻 `DestroyImmediate`**、释放 RT、还原 `RenderTexture.active`；**不读回像素、不写图、不碰产品代码**。
- 抛异常时 **catch 住只 `Debug.Log` 一笔**（⛔ 不用 `LogError` —— 那会被当成断言外的错误）：
  **旧行为就是「首渲抛异常、整轮照跑 4504 条」**，预热失败也不许中断整轮；真相交给 §2·1 的前提条去报。
- 诊断 §三 第 3 条建议的落地。⚠️ **`RunCore` 栈帧实测 ~45 KB、离爆栈很远**（诊断已排除「帧太大」），
  **为什么在深栈上炸还没有定论** ⇒ 这一手是**预防性**的，**是否真的避开待下次复跑确认**。

---

## 三、typecheck / 不变量

| 项 | 结果 |
| --- | --- |
| `TMPDIR=/tmp/wf_bs3 bash d:/4/Unity/工具/typecheck.sh` | 运行时 **0** · 编辑器 **0**（0/0） |
| `BattleScene.cs` 行尾 | **CRLF 39033 / LF 39033 · bare LF = 0**（未翻） |
| 生产代码 | **零改动**（`git status` 里 `BattleDriver` / `CardFeel` / `CardView` / `RuleEngine/**` 的改动都是**本轮之前**就有的） |
| 改动文件 | `Editor/BattleScene.cs` · 本线 `任务线/战斗线/任务总表.md`（新增一行）· 本报告 |
| 断言条目数 | **+1**（新的「（前提）渲图没出图」条；原来那条像素差判据仍在，只是被前置包起来） |

---

## 四、没查清 / 残余（⛔ 没猜）

1. **首次 `Camera.Render()` 的 `InsufficientExecutionStackException` 会不会复现** —— 只有**复跑**才知道。
   本轮按红线**没跑 Unity**。若复跑仍复现：看日志里有没有
   `渲染管线预热失败` 那一笔，以及 `CheckBoardStatStack` 的「（前提）」那条 ——
   它会明确告诉你是**引擎故障**还是**数值层缺陷**。
2. **`CardView.cs:7947-7948` 的 `Quaternion.Angle(...) > 0.01f`**（诊断 §四 末尾的提醒）：
   属**生产代码**、且量级**未查证** ⇒ 本轮**没动**。若它是 0.0x° 级，同属这个盲区，需要另开一条账。
3. **新阈值的实跑余量**：`:19887` / `:19894` 收到 **0.01°** 之后，理论上残差只有浮点级
   （实现与参考走同一条算式、`shake_presets.json` 的数值逐位相同），但**没有实跑过**。
   若复跑出现 0.01°～0.04° 的红，**先查** `DefaultShakeSignal` 的载入是否把 JSON 数值规整过
   （如 `float.Parse` 后再 `round`），**别急着放宽阈值**。
4. `Quaternion.Angle` 在本文件还剩 **6 处**（现读行号）：`:18560` `:18565`（翻面 **>60°**）·
   `:18698`（爆散体朝向 **<0.5°**）· `:21613`（T12 t=0 位姿 **>0.25°** 判坏）·
   `:21616` / `:21687`（T12 回本位，**<0.05°**）。前三条的阈值**远大于** 0.162° 盲区 ⇒ 鉴别力无损；
   后两条阈值 **0.05° 落在盲区里** ⇒ 只能抓到 ≥0.162° 的残留（是**弱化**、不是恒绿）——
   它们**不在本轮的修复清单内**、且仍能抓大错，按铁律 5·b **记在这里，未改**。
5. `:20233`「没有 `OrientationNoise` ⇒ 旋转恒等」用的是 `== Quaternion.identity`（同一个 dot 阈值）。
   此处**两侧都恰好是精确的 identity**（`OrientationOctaves = 0` 直接 return identity）⇒ 现状成立；
   若将来实现改成「算出一个 0 旋转」会有 0.16° 以下的假绿，**本轮未改**（改了也只是把同一个盲区挪个位置）。

---

## 五、判据出处

- 盲区 IL：`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll`
  （`Quaternion.IsEqualUsingDot` IL = `02 22 EF FF 7F 3F FE 02 2A`；`Angle` rid 0x222D）—— 转引诊断 §五。
- 算式：`Core/CardFeel.cs:1528-1553`（`ShakeOrientation`，两次 `SlerpUnclamped`）；
  参考实现 `Editor/BattleScene.cs` 的 `ShakeOrientationRef`；数据 `Resources/WarpforgeVFX/shake_presets.json`。
- 渲染故障：`_tmp_view/t67_sync/battle4.log:4828`（首个异常）、`:7278` 起 82 次 Blitter、`:8663`（红）。
- 诊断：`资料/普查产出_1023b/诊断_BattleScene第三轮3红.md`（§一 / §二 / §三 / §四）。
