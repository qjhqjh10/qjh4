# B117-WMINIONCURVE —— `minionMovementInfo` 那族曲线的关键帧斜率（Hermite）

> 执行代理 `B117-WMINIONCURVE` · 2026-11-02。任务来源 = `B117-R66`（`B117-WCURVE` 留下的「`minionMovementInfo`
> 那族仍丢 `inSlope/outSlope`」）。
> 判据正本 = `判据/B117-WCURVE_动画曲线斜率.md` §2.1/§2.2/§七·1（**格式 = 同一套 4 数格式**，⛔ 本文不重抄）。
> 原版语义 = `CardScript._MoveAndDisappearAnim_d__358.MoveNext`（三条位移 tween 各自 `SetEase(curve)`）
> + `CardScript__ApplyMovementDisappearAnimRotations.c:54`（`curve.Evaluate(t01)`）。
> 源 clip（第一权威）= `assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json` 的
> `animInfo.minionMovementInfo.*.m_Curve[*].inSlope/outSlope`（全表 `weightedMode = 0`）。

## 一、改了什么

| 文件 | 改动 | 落点 |
|---|---|---|
| `Unity/工具/gen_cardanim_clips.py` | `_minion_move_block` 里 9 条曲线的 `_curve_flat(...)` 全部加 **`with_slopes=True`**（`mmCx/mmCy/mmCz` · `mvcx/mvcy/mvcz` · `mmRxC/mmRyC/mmRzC`）；三处 doc 同步改（模块头、`_curve_flat` 的「⛔ minion 必须 2 数」、`_minion_move_block` 的短名说明） | `:378`（三条位移）· `:429`（三条旋转）· doc `:46-47/:192-195/:358` |
| `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | `MinionCurve` 改成**先判格式**：`CardAnimOrder.HasSlopes(flat)` ⇒ 4 数格式（每键 `t,v,in,out`），否则旧的 2 数格式 | `:24108-24115`（**8 行，行数守恒**；`:24117` 起未动） |
| 同上 | **新成员 `MinionKey`**：逐键构造 `Keyframe`（4 数格式把 `in/out` 交给 `inTangent/outTangent`；2 数格式按**弦斜率**补切线 —— 见 §四） | **类尾** `:40758` 之后（`BattleDriver` 类结束前；全仓账对 `BattleDriver.cs` 的最大行号引用 = **40005**，本追加不影响任何 A 行） |

* **格式判据只留一份**：长度 `%4==1` + 首格 `-1` 一律走 `CardAnimOrder.HasSlopes`（⛔ 本文/本代码都没有第二份）。
* **Hermite 没有第二份实现**：`BattleDriver` 把切线交给 **Unity 自己的 `AnimationCurve`**（原版就是它）；
  `CardAnimOrder.HermiteAt` 是 `private static`（`CardAnimOrder.cs:394`，本笔红线冻结该文件），
  所以**没法直接调它** —— 改成「用 Unity 的同一个算法」而不是「复制一份」。断言 `M2c` 把两者在 401 个点上对齐（最大差 < 1e-4）。
* 生成器**裸跑仍 = 用法 + `rc=2` 零写盘**；重生成用的是 `--write`，`--check` 幂等（见 §三）。

## 二、数据实测（`--write` 前后逐键比对，备份 = 改前那两份 json）

| 指标 | 数字 |
|---|---|
| 整表行数 | **1067** |
| **变了的行** | **9**（`Scarab_Explosion` · `SporeMine_Explosion` · `DA Teleport Recall/Summon Next/Trigger Board Only/Trigger Effect` · `Generic Recall` · `Summon Generic` · `TeleportCardAnim`） |
| **没变的行** | **1058**（逐字节相同） |
| 变的**列** | 8 个：`mmCx mmCy mmCz mvcx mvcy mvcz mmRxC mmRyC` |
| 变的曲线列数 | **17**（17 条从 2 数 → 4 数） |
| 没变的曲线列数 | **23**（40 条 minion 曲线列里，`≥2` 键的有 40 条；另 2 条单键 `[0,0]` 不计） |

* 每条的格数变化：3 键 6→13 · 4 键 8→17 · 5 键 10→21（= `1+4n`）。
* **线性曲线逐字节不变**：`SporeMine` 的 x/z 位移 `[0,0,1,1]`、`CleansingFlamesEffect` 的 4 条常数曲线
  `[0,1,1,1]`、`DA Teleport Recall` 等的 `[0,0,1,1]` —— 全部仍是 2 数格式，一个字节没动。
* 其余字段（`ease` / `ofx·ofy·ofz` / `ush` / `tw` …）**零变化**（整表 diff 只落在上面那 8 列上）。

## 三、误差读数（400 点 × 每条曲线；`t ∈ [0,1]` 等距）

| 口径 | 最大误差 | 出处 |
|---|---|---|
| **新产线求值 vs 源 clip 真值** | **2.982e-04** | `Scarab_Explosion.mmCy @ t=0.223` |
| 同上，但**只算「值/斜率 4 位截断」的贡献**（时间网格取同一条） | **4.974e-05** | `Scarab_Explosion.mmCx @ t=0.619` |
| **旧产线求值 vs 源 clip 真值**（改前） | **4.312e-01** | `Scarab_Explosion.mmRxC @ t=0.677` |

🔴 **2.982e-04 里没有一分是「斜率」带来的**：误差分解（同一条 4 数曲线，只改截断位数）——
全精度 **4.3e-09** · **只把时间截到 4 位** **3.180e-04** · 时间+值 4 位 2.980e-04 · 全 4 位 2.982e-04。
⇒ 那是 `_curve_flat` **一直就有的「时间 4 位小数」**（旧 2 数格式同样截；旧口径下它被同一个截断掩盖了），
而不是本笔新引入的 —— 本笔真正引入的误差是上表第二行 **4.97e-05 < 1e-4**。

代表案例（旧产线 / 新产线 / 源真值）：

| 曲线 | t | 旧 | 新 | 真值 |
|---|---|---|---|---|
| `SporeMine_Explosion.mmRxC` | 0.50 | −0.416799 | **−0.756863** | −0.756829 |
| `DA Teleport Recall.mmRyC` | 0.25 | 0.519469 | **0.639398** | 0.639369 |
| `SporeMine_Explosion.mvcy` | 0.30 | 1.671800 | **1.555300** | 1.555300 |
| `DA Teleport Summon Next.mvcy` | 0.25 | 0.847196 | **0.752564** | 0.752559 |

## 四、⚠️ 额外发现并**当场修掉**的一条：旧的 2 数格式根本不是「线性」

`MinionCurve` 旧实现对 2 数格式写的是 `new Keyframe(t, v)` —— 那个构造函数是
`Keyframe(float,float) : this(time, value, 0f, 0f)` ⇒ **`inTangent`/`outTangent` 全 0** ⇒
`AnimationCurve.Evaluate` 给的是**零切线 Hermite（`3u²−2u³` 的平滑阶跃）**，不是线性。
而生成器保留 2 数格式的**唯一理由**就是「这条曲线用 Hermite 与用分段线性逐值相同」
（`_curve_needs_slopes` 拿「源切线 vs 弦斜率」判的）—— 两边**不自洽**：

| 曲线 | t | 旧求值（零切线） | 修后 / 源真值 |
|---|---|---|---|
| `DA Teleport Recall.mmCy`（`[0,0,1,1]`，原版线性） | 0.30 | **0.216000** | **0.300000** |
| `DA Teleport Summon Next.mvcy`（`[0,1.006,0.9968,−0.0045]`） | 0.25 | 0.847196 | 0.752564 |

⇒ 修法（已落进 `MinionKey`）：2 数格式按**弦斜率**补切线。因为 `_curve_needs_slopes == false` 已经保证
「源 `inSlope/outSlope` 逐值等于弦斜率（容差 1e-6）」，所以弦切线的 Hermite **就是原版曲线的逐值**。
影响面 = 那 23 条仍走 2 数格式的 minion 曲线（`DA Teleport *` / `TeleportCardAnim` / `Summon Generic` /
`Generic Recall` 的 y 位移曲线；`CleansingFlamesEffect` 是常数曲线，求值不变）。
🔴 **数据字节零变化**（§二那条「23 列逐字节不变」仍然成立）—— 变的是**求值**。
📌 若调度台认为这超出 `B117-R66` 的范围：撤销点 = `MinionKey` 的 `else` 支（改回 `new Keyframe(t, v)`），
并删断言 `M4a`；其余（含 §一 那两处）不受影响。

## 五、断言（新文件 `Assets/CardPresentation/Editor/B117_WMINIONCURVE_Assert.cs`）

* **27 条**，静态类 `B117WMinionCurveAssert`（`Run()` / `Pass` / `Fail`，与本批同族一致）。
* 分组：`M1` 数据格式 11 条 · `M2` 4 数格式求值（合成）5 条 · `M3` 真实数据 6 条 · `M4` 旧 2 数格式 5 条。
* **被测对象是真产线**：`BattleDriver.MinionCurve(float[])` 是 `private static` ⇒ 用反射调它
  （⛔ 不在断言里重搭一份 `Keyframe` 逻辑，否则就是自己证明自己）。
* 灭自证的三条判别式：
  * `M2b`：合成 `[-1,0,0,0,4, 1,1,0,0]`（in ≠ out）在 0.25 处 = **0.71875**；`in/out` 写反 ⇒ 0.15625 ⇒ 红
    （真实数据里 `inSlope == outSlope` 成对出现，**分不出顺序**，所以必须用合成的）。
  * `M3b`：同一条 `SporeMine` rotateX 数据**故意丢切线**求值得 −0.4168，与 `M3a` 的期望 −0.7569 相差 **0.34 > 0.3**
    ⇒ 证明 `M3a` 不是「怎么读都过」。
  * `M4a`：线性曲线在 0.3 处必须 **0.3**；改回零切线 ⇒ **0.216** ⇒ 红。
* ⚠️ **宿主尚未接线**（本代理不动 `Editor/*Scene.cs`）。接法 = `BattleScene.cs` 那条聚合行（`:46910` 一带，
  与 `B117WCurveAssert.Run()` 同一物理行，**行内追加 ⇒ 零行数变化**）：
  ```csharp
  B117WMinionCurveAssert.Run(); pass += B117WMinionCurveAssert.Pass; fail += B117WMinionCurveAssert.Fail;
  ```

## 六、🔴 需要**另一个写手**接的一件：`BattleScene.cs` 的 R18 旧断言会红

`Editor/BattleScene.cs:1974` 的 `R18①` 现在写着 **`rotCurve.Length == 6`**（= 假定 `SporeMine_Explosion.rotateX`
是 2 数格式）。本笔之后它是 **13**（3 键 × 4 数 + 标记）⇒ **那一条（以及紧随其后 `if (rotCurve.Length >= 6)` 块里的
`c[0..5]` 索引）会红**。本代理的白名单禁改 `Editor/*Scene.cs`，所以**没有动它**，现成补丁如下（替换 `:1973-1996`）：

```csharp
                Check(rotOk && rotOn && Mathf.Abs(rotDeg - 45f) < 1e-4f
                      && rotCurve != null && CardAnimOrder.HasSlopes(rotCurve) && rotCurve.Length == 13
                      && rotMove && Mathf.Abs(rotDur - 0.6f) < 1e-4f,
                      "★ R18①：`SporeMine_Explosion` 的 **rotateX** 三列 + 位移都进表了"
                    + $"（`RotXOn`={rotOn} / `RotXDeg`={rotDeg:F1} / 曲线格数 {(rotCurve?.Length ?? 0)} / "
                    + $"`MoveBody`={rotMove} / `animTime`={rotDur:F3}）"
                    + "｜🧨 `RtRow` 少一列（`mmRxE/mmRxM/mmRxC`）/ 不赋值 ⇒ 红");
                if (rotCurve != null && CardAnimOrder.HasSlopes(rotCurve) && rotCurve.Length == 13)
                {
                    var c = rotCurve;      // 4 数格式：[-1, t,v,in,out, …]
                    Check(Mathf.Abs(c[1]) < 1e-4f && Mathf.Abs(c[2]) < 1e-4f
                          && Mathf.Abs(c[5] - 0.2373f) < 1e-4f && Mathf.Abs(c[6] + 0.952f) < 1e-3f
                          && Mathf.Abs(c[9] - 1f) < 1e-4f && Mathf.Abs(c[10] - 1f) < 1e-4f,
                          "★ R18①：曲线三键 = **(0,0) · (0.2373, −0.952) · (1, 1)**"
                        + $"（现读 [{c[1]:F4},{c[2]:F4}] [{c[5]:F4},{c[6]:F4}] [{c[9]:F4},{c[10]:F4}]；"
                        + $"切线 [{c[3]:F3}/{c[4]:F3}] [{c[7]:F3}/{c[8]:F3}] [{c[11]:F3}/{c[12]:F3}]）"
                        + "｜🧨 被钳/改写 ⇒ 中段那一拧消失 ⇒ 红");
                    // …（②/③ 那两段 `kMid` / `xMid` / `xEnd` 不用改：断言点恰好落在**键上**，
                    //     带不带切线求值相同；只需把下面这条 `AnimationCurve` 的构造换成带切线版）
                    var curveR18 = new AnimationCurve(new Keyframe(c[1], c[2], c[3], c[4]),
                                                      new Keyframe(c[5], c[6], c[7], c[8]),
                                                      new Keyframe(c[9], c[10], c[11], c[12]));
```

## 七、边界与没做的

* **没跑 Unity**（红线）：断言只做了 `typecheck`；`BattleScene.Run` 的实跑留给调度台。因此 §六 那条
  「会红」是按代码逐句推出来的（`rotCurve.Length` 现读 = 13，离线由 `cardanim_clips.json` 逐值核过）。
* `mmCx/mmCy/mmCz` 三列**没有运行期消费者**（`CardAnimClips` 只把 `mv*` 投影出去）—— 但它们与
  `mvcx/mvcy/mvcz` **同源同值**，一并改是为了「同一族一个格式」，⛔ 不是新增消费。
* 未动 `CardAnimOrder.cs`（冻结）· `Core/CardView.cs` · `Editor/*Scene.cs` · `d:/2` · git；未跑 Unity 批处理。
* 时间精度（4 位小数）**没动** —— 它是 `_curve_flat` 的既有约定，动它会连带改 `ease`/`ofx` 那批已定稿数据（§三 已量化其影响）。
