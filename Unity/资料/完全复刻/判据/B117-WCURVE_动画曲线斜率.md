# B117-WCURVE —— 卡牌动画 `easeCurve` 的关键帧斜率（Hermite）

> 执行代理 `B117-WCURVE` · 2026-11-02。任务来源 = `G10-11` 的发现。
> 判据（第一权威）：`d:/2/tools/decomp_full/CardAnimController__Update.c:36-97`（`easeCurve.Evaluate` /
> 三轴 `offsetMovementAxis.*.Evaluate`，前者输出 clamp、后者不 clamp）+ `assets_full` 的
> `bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json` 每个关键帧的 `inSlope/outSlope`。

## 一、问题（现读复核，数字与 `G10-11` 略有出入，以本节为准）

* 生成器 `工具/gen_cardanim_clips.py` / `工具/gen_card_anim_order.py` 的 `_curve_flat` **每键只写 `[t,v]`**，
  丢掉 `inSlope` / `outSlope` ⇒ 运行期 `CardAnimOrder.EaseAt/EaseAtRaw` 只能按**分段线性**读，而原版
  `AnimationCurve.Evaluate` 是**分段三次 Hermite** ⇒ 非线性曲线的形状是错的。
* 规模（逐条现读）：
  * 原始 bundle 里 `shouldMoveVFX == 1` 的 CardAnim = **136**；
  * 其中 **7 条不进运行期表**（5 个纯延时 `Wait_300ms/1s/1,5s/2s/3s` + `RelentlessMarchSummonTween` +
    `Self-Destruction_Effect`）⇒ `Resources/cardanim_clips.json` 里 `mv == 1` 的行 = **129**；
  * 这 129 条里 **13 条**的进度曲线 Hermite ≠ 分段线性（`G10-11` 说的「14 条」把
    `UnholySmite_random` 也算进去了 —— 它的曲线是 **(0,1)→(1,1) 常数、两端切线 0**，
    两种求值**逐值相同**，见 §四「常数曲线」）。
  * 三轴偏移曲线（`ofx/ofy/ofz`，同样是原版 `Evaluate`）另有 **39 条**非线性（ofx 6 · ofy 32 · ofz 1）。
* 最大偏差（按分段线性读 vs 真 Hermite）：`Atk_StikkaThrow` 一族中点 **0.5 → 0.75**（偏 0.25）；
  `Atk_JainasMor_Throw_Return` 0.30；`Atk_Deathspinner_arc_alt` 0.16。

## 二、改法

### 2.1 数据格式（两个生成器同一套约定）

* **线性曲线：保持不变** ⇒ 旧的 2 数格式 `[t0,v0,t1,v1,…]`（**逐字节不变**，这是「别回归」的那条）。
* **非线性曲线：4 数格式** `[-1, t0, v0, inSlope0, outSlope0, t1, v1, inSlope1, outSlope1, …]`
  * 首格 `-1` 是**标记**（时间永远 ≥ 0，不可能是真的时间）。
  * 作用：把两种格式做成**长度奇偶互斥** —— 4 数格式 = `1+4n`（长度 `% 4 == 1`，**奇数**），
    旧的 2 数格式 = `2m`（**偶数**）⇒ 运行期只靠长度就能判，**不需要额外的版本列**
    （`BattleDriver` 直接拿 `float[]` 调 `CardAnimOrder.EaseAt`，那里没有地方传版本号）。
* **哪些列带斜率**（只这两族，因为只有它们走 `AnimationCurve.Evaluate` 语义）：
  `easeCurve`（`EaseAt`）与 `offsetMovementAxis.{x,y,z}Axis`（`EaseAtRaw`）。
* ⛔ **`minionMovementInfo` 那几条曲线（`mmCx/mmCy/mmCz` · `mvcx/mvcy/mvcz` · `mmRxC/mmRyC/mmRzC`）
  必须保持 2 数格式**：消费方是 `BattleDriver.MinionCurve`（`flat.Length / 2` 硬按两数一组建
  `AnimationCurve`）—— 换格式会**静默读错**，而那个文件不在本笔改动范围（见 §六 未做项）。

### 2.2 生成器

| 文件 | 改动 | 落点 |
|---|---|---|
| `Unity/工具/gen_cardanim_clips.py` | `CURVE_SLOPE_TAG` + `_curve_needs_slopes()` + `_curve_flat(curve, with_slopes=False)`；`ease` 与 `ofx/ofy/ofz` 传 `with_slopes=True` | `:161/:164/:182`，调用点 `:756/:763-765` |
| 同上（CLI） | **裸跑 = 打印用法 + `rc=2`、零写盘**；真动作必须 `--write`（新增 `--check` / `--dry`） | `:834` `:850` |
| `Unity/工具/gen_card_anim_order.py` | 同形态的 `CURVE_SLOPE_TAG` / `_curve_needs_slopes` / `_curve_flat(..., with_slopes=False)`；`_ease_flat` 与 `ofx/ofy/ofz` 传 `with_slopes=True` | `:452/:455/:470`，调用点 `:496/:538-540` |
| 同上（CLI） | 裸跑 `rc` 由 **0 → 2**（`-h/--help` 仍 0）；原本裸跑就不写盘 | `:667` |

`_curve_needs_slopes` 的判据：**逐段**比较左键 `outSlope` / 右键 `inSlope` 与**弦斜率**；
全相等 ⇒ 两种求值逐值相同 ⇒ 不写斜率（保持 2 数格式）。斜率与值一样 `round(…, 4)`
（引入的最大误差见 §四）。

### 2.3 运行期（`CardPresentation/Battle/CardAnimOrder.cs`）

* `EaseAt` / `EaseAtRaw`：先判 `HasSlopes(...)`，是则走 `HermiteAt`，否则**原样保留**旧的分段线性分支
  （⇒ 旧 2 数格式 / 旧数据逐值不变）。落点 `:204` / `:225`。
* 新成员**追加在类尾**（`:380` 起，`ResetCounters()` 之后）：
  * `const float SlopeTag = -1f;`
  * `public static bool HasSlopes(float[] a)` —— `长度 ≥ 9 && 长度 % 4 == 1 && a[0] == -1`
  * `static float HermiteAt(float[] a, float x)` ——
    `h00·v0 + h10·dt·out0 + h01·v1 + h11·dt·in1`，`u = (x−t0)/dt`，**切线乘段长 `dt`**；
    末键取 `a[len-3]`（末键布局 `t,v,in,out` ⇒ 值在倒数第 3 格）。
* 改法**行数守恒**：`EaseAt` / `EaseAtRaw` 各 16 行（把两行 `float t0…` 合成一行换取 Hermite 分支那一行），
  两段 doc 各 6 / 4 行不变 ⇒ 文件原有的 363 行行号不动，新代码全部在类尾。

## 三、重跑与验证（全部实测）

* 重跑：`gen_cardanim_clips.py --write`（写 `数据/游戏数据/cardanim_clips.json` 与
  `Resources/cardanim_clips.json`，两份逐字节相同）+ `gen_card_anim_order.py --write-slim`。
* 幂等：`--check` = **OK**；`--check-slim` 幂等 = **True**。
* **字段级 diff（重跑前后，逐行比对 JSON）**：
  * `cardanim_clips.json`：**只有** `ease` 13 行、`ofx` 6 行、`ofy` 32 行、`ofz` 1 行变了；**其它 0 字段变化**。
  * `card_anim_order.json`：**只有** `ease` 3 拍、`ofx` 3 拍、`ofy` 1 拍变了。
* **没变的逐字节没变**：`mv == 1` 的 **129** 行里 **116 行**的 `ease` 数组与改前**完全相同**。
* **形状正确性**：把「逐行照抄 `EaseAt/EaseAtRaw` 分支」的实现，对 **每一条曲线取 400 个采样点**，
  与「直接按原版 clip 的 `inSlope/outSlope` 算 Hermite」比：
  * `cardanim_clips.json`：**max 误差 1.35e-06**（最差 = `Atk_JainasMor_Throw_Return`，来自斜率 4 位小数取整）；
  * `card_anim_order.json`：**max 误差 2.32e-06**（最差 = `Healing_Volley_Effect` 的 `ofy`）；
  * 两者都 **< 1e-4**（任务判据）。
* 常数曲线：`UnholySmite_random` 的 `ease` 是 `(0,1)→(1,1)` ⇒ **判成线性 ⇒ 保持 2 数格式**
  （写斜率没有意义）；两种求值任何进度都得 1。

### 13 条被改的 `ease`（线性读 → Hermite 读）

| CardAnim | 键 | 0.25 | 0.50 | 0.75 |
|---|---|---|---|---|
| `Atk_BulletImpact_MissileToTarget_warlord`（含 `_instant` / `_quick`） | 2 | 0.2500→**0.1562** | 0.5000 | 0.7500→**0.8438** |
| `Atk_Deathspinner_arc_alt` | 2 | 0.2500→**0.3215** | 0.5000→**0.6469** | 0.7500→**0.8988** |
| `Atk_JainasMor_Throw_Return` | 4 | 0.6250→**0.7323** | 1.0000→**1.0076**(clamp 1) | 0.6250→**0.7811** |
| `Atk_MissileToTarget_Orks` | 2 | 0.2500→**0.1562** | 0.5000 | 0.7500→**0.8438** |
| `Atk_Rokkit_Harpoon big` / `small` | 2 | 0.2500→**0.4375** | 0.5000→**0.7500** | 0.7500→**0.9375** |
| `Atk_StikkaThrow` / `_Quick_BadAim` / `_Warlord` / `_Warlord_Quick_BadAim` | 2 | 0.2500→**0.4375** | 0.5000→**0.7500** | 0.7500→**0.9375** |
| `DelightfulAgonies` | 2 | 0.2500→**0.1562** | 0.5000 | 0.7500→**0.8438** |

（`Atk_BulletImpact_MissileToTarget_warlord` 一族 = 两张键切线都是 0 ⇒ **平滑阶跃**，中点仍 0.5，
所以「中点」分不出它 —— 判别式要取 0.25 / 0.75。）

## 四、断言

新建 `Assets/CardPresentation/Editor/B117_WCURVE_Assert.cs`（静态类 `B117WCurveAssert.Run()`，
**宿主由调度台接**，本笔不动任何 `Editor/*Scene.cs`）：**36 条 `Check/Near`**（其中 4 条是硬前提）。

* W1 格式判别（6 条）：新旧格式的长度奇偶互斥、标记必须为 `-1`、`null`/空不抛。
* W2 Hermite 形状（8 条）：平滑阶跃 0.25 处 = 0.15625；★ **切线 × dt 的判别式**（`dt=0.5` 时 0.625，漏乘 dt 得 0.75）；
  clamp 两档；`EaseAtRaw` 不 clamp。
* W3 真实数据（16 条，4 条前提 + 12 条结论）：`Atk_StikkaThrow` 中点 **0.75** · `Atk_Deathspinner_arc_alt` **0.646875** ·
  `Atk_GrotGrenade` 的 y 弧 25% 处 **0.6466** · `Jain Zar` 末键 **0** / 0.75 处 **0.78113** ·
  `UnholySmite_random` 仍是 2 数格式 · 表内普查（格式不变量 + 带斜率条数 3 拍 `ease` / 4 条偏移 / 18 条旧格式）。
* W4 旧格式兼容（5 条）：`[0,0,1,1]` / 常数 / 3 键 / 零长段。

## 五、🔴 一处**必须由别的写手改**的连带（不在本代理的文件白名单里）

`Assets/CardPresentation/Editor/BattleScene.cs`（**当前由 `B117-WBT` 独占**）里那条「灭自证」断言：

```csharp
Check(nonlin83 >= 1 && nonlin83 <= 3,  // :40749
      $"★ 新账-83（**灭自证**）：非线性 `easeCurve` **{nonlin83}** 件（现读 1 = `Jain Zar`）…");
```

* 它数的是「池内攻击 mover 里 `EaseAt(Ease, 0.5) != 0.5` 的 (卡, 件) 对数」。
* **本笔之后该值 = 4**（`Atk_Deathspinner_arc_alt` 0.6469 · `Atk_JainasMor_Throw_Return` 1.0076 ·
  `Atk_Rokkit_Harpoon small` 0.75 · `Atk_StikkaThrow` 0.75）⇒ `<= 3` 会红。
  （`movers83 = 33` / `ao83 = 26` / `otv83 = 22` / `arc83 = 21` 都**不变**，已复算。）
* 建议改法：`nonlin83 >= 1 && nonlin83 <= 8`，文案「现读 1 = Jain Zar」→「现读 4」。
  ⚠️ 顺带：同文件 `:40452` 的 `mover.ease.Length / 2` 只出现在**日志文案**里（4 数格式下会印成 4 个关键帧），
  不是断言，不影响判红。

## 六、没做 / 没确认的

1. ⛔ **`minionMovementInfo` 那族曲线仍是 2 数格式**（消费方 `BattleDriver.MinionCurve` 按 `Length/2` 解析）。
   它们同样是原版 `AnimationCurve.Evaluate`（`CardScript.MoveAndDisappearAnim`），理论上也丢斜率；
   **要修必须同时改 `BattleDriver.MinionCurve`**（本笔红线不允许）⇒ 记为待办，判据 = 本次同一套。
2. `CardPresentation/Battle/CardAnimClips.cs:84` 那段 doc 还写着「修法（未做…）」—— **本笔已做完**，
   但该文件不在本代理白名单 ⇒ 留给调度台/该文件写手订正（铁律 5）。
3. `CardAnimOrder.cs` 文件头「本表 **62 张卡 / 99 拍**」已过期（现读 **93 卡 / 158 拍**，卡池增长所致）；
   与本次改动无关，未改（避免动正文行号）。
4. 未跑 Unity（红线）：断言文件只过 `typecheck`，**没有实机跑**；宿主接线后需跑一次对应宿主。
5. `EaseAt` 对 `x < 第一键时间` 的段做 Hermite 外推（与旧行为的分段线性外推同形）；原版此时走
   `m_PreInfinity = 2` 的环绕语义 —— 本表所有曲线首键时间都是 0，实际不会走到，未另行处理。

## 七、改动清单

* `Unity/工具/gen_cardanim_clips.py`（+85/−12 行）
* `Unity/工具/gen_card_anim_order.py`（+57/−12 行）
* `Unity/MyGame/Assets/CardPresentation/Battle/CardAnimOrder.cs`（+58/−7 行；原有 363 行行号不变，新成员在类尾）
* `Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json` · `Unity/数据/游戏数据/cardanim_clips.json`（两份逐字节相同）
* `Unity/MyGame/Assets/CardPresentation/Resources/card_anim_order.json`
* `Unity/MyGame/Assets/CardPresentation/Editor/B117_WCURVE_Assert.cs`（新建，22 条断言）
* `typecheck`：**运行时 0 / 编辑器 0**
