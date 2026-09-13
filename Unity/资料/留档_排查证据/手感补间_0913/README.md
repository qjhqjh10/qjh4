# 手感补间六件（发牌入场 / 攻击位移 / 命中抖动 / 阵亡消散 / 数值过渡 / 手牌重排）—— 2026-09-13

> 起因：用户 2026-09-13 定的顺序「战斗界面剩下的整块」里的第 ③ 步（①② 是墓地日志与那批小件）。
> 结论：**六件全做完，而且参数全部换成了原版数据**（不是照手感调的）。

## 一句话账

| | 事实 |
|---|---|
| **以前** | `CardTween` 的文件头自己写着「**目前用的是照着量级取的近似值**」；`DeploySequence` 也一样 |
| **这一轮** | 参数从**四个原版来源**里挖出来，集中到 `CardPresentation/Core/CardFeel.cs`，逐条标出处 |
| **六件** | ① 发牌入场 ② 攻击位移 ③ 命中抖动 ④ 阵亡消散 ⑤ 数值过渡（伤害飘字）⑥ 手牌重排 |
| **顺带修的真 bug** | `SyncBoard` 在引擎判死的**那一帧**就销毁视图 → 阵亡消散**一帧都演不出来**（截图看不出，断言抓到的） |
| **改了既有两处数** | `EventTiming` 的 `DurationOf(Attack)` 0.3→**0.65**（漏了蓄力）、`DurationOf(Hit)` 0.5→**0.75**（漏了复位段） |

## 四个数据源（以后做动作先来这四个地方找）

| # | 在哪 | 有什么 |
|---|---|---|
| ① | `d:/2/新解包资源/assets_full/*/AnimationClip/`（**99 条**） | `Card Hand To Board`(0.9167s/17 曲线，**完成事件在 0.55**) · `Card Display Open` · `InBattleDamageCounter Variation 1`(1.8333s) · `Minion Death Icon` · `BattleHudShow/Hide` … |
| ② | `d:/4/Unity/数据/游戏数据/tween/`（74 个） | `Recoil *` / `Impact *` / `Summon Troop` / `EC Heldrake Dissapear UP` …，`ease` 字段就是 **DOTween 的 `Ease` 枚举**。⚠️ **其中 17 个是 Cinemachine 相机抖动预设**（`CameraShakePreset`），**不是卡在抖** |
| ③ | `d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_1744609728290659264.json` | `timeToChargeAttack 0.35` / `chargeAttackAngle -10` / `chargeBackModifier 0.5` / `attackStepTime 0.1` / `attackRotationAngle 25` / `timeToLand 0.2` / `cardMovementSpeed 10.8` / `meleeHitCameraShakePreset`(amp 2.0)…（**全库只有这一份带这些字段**） |
| ④ | `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out2/`（协程体） | `WaitForSeconds` 是**明文**：出手 1.3 / 出一张牌 0.3 / 抽牌 1.1 … |

`_DAT_` 常量（`DoPushBack` 的 0.4 / 0.3）用 `资料/战斗规则与数值_出处.md` §三 的办法从
`GameAssembly.dll` 读出来 —— ⚠️ **那节的脚本这一轮修过一次**（PE 段表字段读串位，照抄会读出乱数）。

## 图

| 文件 | 是什么 |
|---|---|
| `当事人对照.png` | **两行 × 三帧**：上排＝攻击者（我方督军）rest → **蓄力 −6.9°** → 前冲；下排＝受击者（对面槽 0）rest → **命中（弹开 0.15 世界单位、歪 −1.9°）** → **消散中（alpha 0.88，整张明显发灰）** |
| `四帧连拍.png` | 四个时刻的战场全景：发牌 → 出手 → 命中+飘字 → 阵亡消散（视角宽，细节小） |

数值都是**自检里量出来的**，不是从图上量的（图上只看得出「确实在动」）。

## 怎么复现

```bash
unset ELECTRON_RUN_AS_NODE
"D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod BattleScene.Run -logFile -
# 筛输出：grep "^BT "　—— 认 `--- 手感补间` 那一节（28 条断言）
# 图落在 d:/4/_tmp_view/battle/19_发牌入场之后.png … 22_阵亡消散.png
```

⚠️ **采样必须细推**：`Step(dt)` 是「先推事件、再把补间推 dt 秒」，一次推 0.45 s 等于把刚起步的
那一下弹跳整段跳过去（第一版就这么错的，量出来位移恒为 0.000）。用例里用的是 `AdvanceTo(相对秒)`
—— 每步 1/60，推到「刚好过那一刻」。

## 哪些是原版、哪些是我们的（**别混**）

> ⚠️ **2026-09-13 补（用户追问「这些确定都是原版解包资料里说明的参数吧」）—— 不是全部。**
> 已经把这件事做成**代码里的登记表**：`CardFeel.Catalog` + `Src.Field/Derived/Ours` 三档，
> 自检**用反射核对「有没有常量没登记」**（漏一个直接红）并把整张表打进日志。当时的账：
> **40 条里 30 条是原版字段 · 7 条是推导 · 3 条是我们挑的。**

| 档 | 条数 | 是哪些 |
|---|---|---|
| **原版字段**（解包资产 / clip 关键帧 / 反编译常量） | 30 | 所有**时长**与**缓动**、vibrato、弹性、蓄力三件套（0.35 / −10° / 0.5）、`attackStepTime`、clip 的每个关键时刻、飘字的每个关键帧、`DoPushBack` 的 0.4/8/0.3 |
| **原版推导**（从原版值换算） | 7 | `UnitsToOurs`（182.14÷108）、两个 px/单位常量、旋转 punch 的度数（3D `(-3,-3,0)` → 取模长当 2D 的 z 角）、消散时长（0.7−0.1667）、发牌时长（借 clip 的完成时刻 0.55） |
| **我们挑的** | 3 | 轻/重击的**幅度顶替**（原版走 `GetUnitSize × GetPushBackFactor`，**方法体没被反编译**）、重击**分档门槛**（伤害 ≥4） |

**常量之外还有几处「我们的」**（登记表盖不住，写在 `CardFeel.cs` 文件头）：
位移**方向**映射（原版 punch 在 3D 的 z 轴上，2D 卡没有 z）· 阵亡的**表现形式**（原版是材质溶解，
我们用透明+上浮+缩小，**时长照原版**）· 飘字的字号/颜色/位置偏移 · 发牌起点 ·
手牌重排的 0.18s（`CardTween.RelayoutDuration`，原版查不到）。

## 手牌 → 战场的落位：0.92s（我们挑的）→ **0.30s（原版字段）**

- 出处：**`MinionManager.minionToConversionPointTime = 0.3`** ——
  `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json`。
  **判定它是权威实例的依据**：同一份的 `desiredScale 0.36` + `MinionSeparation 0.82` 正是尺寸桥
  （137.2 px = 2.0927×0.36×182.14；149.3 px = 0.82×182.14），`slotsPerSide = 4` 与 9 格棋盘一致。
  ⚠️ 另一份 **MB 4373 是旧预设**（0.69 / 1.53，槽位数组只有 **3** 个 —— 那个被推翻的「7 格」误读就是它）。
- 调用链（真反编译）：`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` →
  `MinionManager.PlayMinionFromHand(…, timeToMove, …)` → `WaitForSeconds(GetMinionConversionTime(card))`。
- ⚠️ **`Card Hand To Board`（0.9167s）不是移动时长** —— 它是「2D 卡 → 3D 身体」的**交接**动画
  （2D 卡 0.3333–0.6667 淡出、3D 身体 0.1667–0.7 溶解出现、完成事件在 **0.55**）。原来注释认错了来源。

## 顺带修掉的两个真 bug（都只有断言抓得到）

1. **跨局残留的事件**：`Begin()` 换新一局时没清事件时间线，而事件**只带格位号、不带「第几局」** ——
   上一局排在未来的 `Death P2@0` 到点后会去杀**新一局站在同一格的那张卡**。
   实测症状：自检里刚摆好的受击者，在命中之前就进了消散表。
   ⚠️ 附带更阴的一条：队列按时间有序，**一条「未来」的事件会把后面早就过期的全堵住**。
   ⇒ `Begin()` 清时间线；`AdvanceTimeline` 改成取「到点事件里**最早**的那条」，不再假设队头最早。
2. **DOTween 的 `Join` 接的是「当前游标」而不是序列开头** —— 写错就把整条序列悄悄拖长：
   落位那两条 0.3s 从 0.105 起算 → 序列实际 **0.405s**（旧版更离谱：0.92 → **1.10s**），
   `OnComplete`（→ 引擎真的出牌）也跟着晚；飘字那条同样被拖长 0.12s。
   ⇒ 全改 `Insert(0f, …)`，并加了一组 **「序列真实长度 == 标的时长」** 的断言（六条，全过）。

## 下一步的现成线索（这轮查到没做）

1. **挨打时震镜头**：原版 `meleeHitCameraShakePreset`（amplitude 2.0）+ 17 个 `Shake *` 预设。
   ⚠️ 要做得先决定「2D 战场怎么表达 3D 的相机抖动」（动相机？动整个战场 quad？）。
2. **AnimFX 脚本层**（原版 18 个模块 / 2346 个组件的行为没还原）。
3. `SupportMethods.GetPushBackFactor`（按单位体型分档的挨打幅度）**方法体没被反编译** → 查不到。
4. 卡预制体里**还没接上**的字段：`attackRotationAngle 25` / `attackPositionYOffset 0.5` /
   `playerAttackMargin 1.7` / `enemyAttackMargin 1.0` / `timeToLand 0.2` / `cardMovementSpeed 10.8`
   （⚠️ `cardMovementSpeed` 的**用途查不到**：调用点没反编译，拿它算时长会得到 0.12s 这种明显不对的数）。
