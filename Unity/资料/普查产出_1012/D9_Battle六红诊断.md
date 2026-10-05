# D9 · `BattleScene.Run` 六条红 —— 只读诊断（2026-10-12）

> **只诊断，一个字没改**（除本文件）。日志实读 = `d:/4/_tmp_view/battle.log`（3,112,093 B · mtime 2026-10-06 01:15 · 40,474 行）
> ⚠️ **简报里给的路径 `d:/4/Unity/_tmp_view/battle.log` 不存在**（那份 `_tmp_view` 在 `d:/4/`，不在 `d:/4/Unity/`）——
> 以下**所有行号都按 `d:/4/_tmp_view/battle.log`**。整条结果行：`:39301` `BT === 结束：1460 通过 / 6 失败 ===`。

---

## 一、结论表

| 红 | 断言 | 根因（一句话） | 证据 | 置信度 | 最小改法 |
|---|---|---|---|---|---|
| 1 | `:38780` ★A423 钮的位置 | **不是假阴、也不是 float32 噪声**：断言拿**原版未取整的矩形**（中心 (50.1508, 599.097)）当期望，而实现里的 px 字面量是**取整过的**（`17.9 / 568.2 / 64.44 / 61.85` ⇒ 中心 (50.12, 599.125)），差 (0.0308, 0.028) px = (2.85e-4, 2.59e-4) 世界单位 ⇒ **超过它自己 1e-4 世界单位（≈0.011 px）的阈值** | `Editor/BattleScene.cs:10224-10231`（期望/阈值）· `Battle/BattleDriver.cs:7880-7888`（HudAbs 换算）· `BattleDriver.cs:7971`（那一行字面量）· 算术见 §二·1 | **高**（两个轴的理论差 `-2.85e-4 / -2.59e-4` 与日志 `(-0.0003, -0.0003)` 逐位吻合） | 把 `BattleDriver.cs:7971` 的四个 px 换成**原版未取整值** `17.9293f, 568.174f, 64.443f, 61.846f`（⇒ 中心恰为 50.1508 / 599.097，画出来仍是 61.846 ⇒ 另两条 A423 断言不受影响）。⛔ **别改阈值** |
| 2 | `:39055` ★A388 真卸载后钩子清零 | **断言的前提是错的**：`BattleDriver` 是**不带 `[ExecuteAlways]`** 的普通 MonoBehaviour，而**自检跑在编辑模式**（`EditorSceneManager.NewScene`，`Application.isPlaying == false`）⇒ `DestroyImmediate(组件)` **不会派发 `OnDestroy`** ⇒ `DetachStaticHooks()` 根本没被调过（**实现没毛病**：同一次运行里直接调它是 19 → 0 全绿，日志 `:38912`） | `BattleScene.cs:10331-10347`（该段）· `BattleDriver.cs:1761-1772 / 1852-1869`· `BattleScene.cs:97`· 坑表 `资料/已知的坑.md:709`、`:3986`（FX4 正反例同框）· `资料/阶段二_滚动与指针_原版规格.md:164` · 本日志 `:629-637`（`SubscribeScreenSizeSource` 是被 `Initialize(bool)` 调的，**不是 `Awake`**） | 高（「OnDestroy 没被调」= 实读；归因到「编辑模式不派生命周期」= 本仓已实证的同一族规矩，⛔ OnDestroy **本仓没单独实证过**） | 这条**在批处理里恒不可验**。最小改法 = 保留 `driver.Begin(...)` 前提与 `DestroyImmediate`，把那条 `Check(StaticHookCount == 0)` 改成**按 `Application.isPlaying` 分档**：Play 下真断 0、编辑模式下**出声说明环境限制**（⛔ 不许写成绿）。真路径留在 `资料/真Play待验清单.md`（W4 §四.4 已列）。想要硬判据另加 5 行探针（见 §二·2） |
| 3 | `:39115` ★A431 arena2 建不出来 | **不是旁挂/清单问题，是 `MakeAnimFx` 在编辑模式把刚建好的宿主【当场销毁】了**：`RocketTrail` 是 5 条里**唯一** `preventDestroy = 0` 且 `enabled ∧ goActive` 都开的那条 ⇒ `MakeAnimFx` 里那段「批处理没有帧循环 ⇒ 走 `DestroyImmediate`」把宿主 GO（= 我们刚新建的节点）**同步删掉**，然后 `return c`（一个**已销毁**的组件，Unity 的 `==` 判它 null）⇒ `BuildSceneAnimFx` 记成「`MakeAnimFx` 建不出来（宿主/字段缺）」 | `Battle/ScenarioBlendables.cs:1649-1722`（尤其 `:1709-1718`）· `:2051-2052`（那条 `c == null`）· 数据 `Resources/EnvBlendables.json` 的 `sceneStandalone[battlearena2]`：`enabled=1 · goActive=1 · preventDestroy=0 · destroyTime=6` · **同一次运行里 A341 那条断言已实证这一跳会当场销毁宿主**（日志 `:3717-3730`，`BattleScene.cs:1817-1821`）· 日志里**没有**「宿主对象解析不到」/「没有 `preventDestroy`」两条告警 ⇒ 排除「宿主找不到」「字段缺」 | **高** | 让**场景侧这一条链**不要在编辑模式做「当场销毁」的模拟（那条支路真正要的是「组件建出来 + 5 个子件改挂」）：给 `MakeAnimFx` 再加一个「编辑模式要不要模拟销毁」的形参、`BuildSceneAnimFx` 传 `false`（A341 那条合成探针保持原样 ⇒ 它那条断言不红）。⛔ 运行时那一档（`Application.isPlaying` ⇒ `Destroy(go, 6s)`）**一个字都不许动**。**口径归调度台**（另一条路是「编辑模式一律不模拟」+ 改 A341 那条断言，见 §二·3） |
| 4 | `:39127` RocketTrail 底下 ≥5 颗粒子（实得 -1） | **同 3 的后果**：`Transform.Find("Scenario/Battle Arena 2 Particles/RocketTrail")` 找不到 = 那 3 个新建节点里的 **RocketTrail 节点本身被删了**（`rk == null` ⇒ 打印 -1）。这条正好**反证了根因是「销毁」而不是「`AddComponent` 返回 null」**：若只是组件没建，节点还在、这条会打 ≥0 而不是 -1 | 同上 · 另：`Resources/ArenaPrefabs/battlearena2.prefab`（mtime 10-05 17:11）里**没有** `Scenario` / `Battle Arena 2 Particles` / `RocketTrail`（各 0 次命中）⇒ 那条链**只可能**来自 `sceneStandaloneBuild.nodes[]` 新建的 3 个节点，不存在「找错了另一棵树」这一档 | **高** | 同 3（修掉当场销毁之后，3 个节点 + 5 个改挂子件都在 ⇒ 这条自然绿） |
| 5 | `:39139` 组件 `enabled` + 宿主 `activeSelf` | **同 3 的后果**：`rk == null ⇒ c2 == null` ⇒ 打印 `enabled=False · activeSelf=False`（不是「值填错了」，是**对象没了**；旁挂里这两个值本来就是 `1/1`） | 同上 + 数据 `enabled=1.0 · goActive=1.0` | **高** | 同 3 |
| 6 | `:39297` 三场合计（4/8/0/1 vs 5/8/5/0） | **同 3 的汇总**，四个数逐一有解：组件 4 = 2(arena3) + 2(tau) + **0**(arena2) · 节点 8 = 3+4+1 **全对** · 改挂 0（arena2 那 5 条被 `continue` 跳过）· 没对上 1（就是那条被误报的 `MakeAnimFx`） | 日志 `:39103`（arena2）`:39187`（arena3）`:39261`（tau）· `ScenarioBlendables.cs:2052`（`continue` 在 ③ 改挂**之前**） | **高** | 同 3 |

> 🔴 **对简报里那个「是不是同源于 A418 没做」的判断：不是**（详见 §三）。A418 那三步**不会**让这四条转绿；而且**真去跑**那三步时，还有一件必须同时改的事（见 §五·A）。

---

## 二、每条展开

### 1 · A423 那颗钮的位置（`:38780`）—— 真红，且**不是** float32 假阴

**断言**（`Editor/BattleScene.cs:10217-10232`）：
```
want423 = hudRoot.TransformPoint(LayoutSpace.ToWorld(50.1508f/1920f, 480.903f/1080f));
Check(|按钮世界坐标 − want| < 1e-4 世界单位)
```
**实现**（`BattleDriver.cs:7971`）：`HudAbs(root, "40k_UI_bt_center_camera", 17.9f, 568.2f, 64.44f, 61.85f, "CenterCameraButton")`；
`HudAbs`（`:7880-7888`）把 `(x, y, w, h)` 换算成 `cx=(x+w/2)/1920`、`cy=1−(y+h/2)/1080`。

**算术（两个解都是「实读数字代进去」）**：

| 量 | 期望（断言） | 实现（HudAbs） | 差 |
|---|---|---|---|
| 画布中心 x（px，左起） | `50.1508`（= LeftAnchor 中心 50 + 0.150757） | `17.9 + 64.44/2` = **50.12** | **0.0308 px** |
| 画布中心 y（px，顶起） | `599.097`（= 540 + 59.097） | `568.2 + 61.85/2` = **599.125** | **0.028 px** |
| ⇒ 世界 x（`VisibleWidth 17.7778`） | — | — | **−2.852e-4** |
| ⇒ 世界 y（`VisibleHeight 10`） | — | — | **−2.593e-4** |

日志实得 `(-0.0003, -0.0003)`（`F4` 四舍五入）—— **两个轴的符号与量级逐位吻合** ⇒ 根因就是**字面量取整**，不是浮点往返噪声（那些是 1e-7 量级、差 3000 倍）。

**判据侧**：原版矩形未取整的值是 `64.443 × 61.846`（同一条断言自己的 `CameraResetHitPxW/H = 48.443 × 45.846` = 该矩形四边各收 8 ⇒ 反推得 64.443 / 61.846；`BattleScene.cs:10211` 那条画的尺寸也是按 61.846 断的，容差 0.05 ⇒ 我们现在画 61.850 **恰好过关**）。左上角口径 = `(17.9293, 568.174)`（`BattleScene.cs:10219` 的注释里就写着）。⇒ **改 4 个数即可**。

⚠️ **顺带**：断言的**文案**写「画布 (50.1508, **480.903**)」是**标错**了 —— 同一件在 `:8539` 的粗口径写的是 `At("CenterCameraButton", 50.15f, 599.1f)`（那套 `HudExtraPosPx` 就是 y 向下的画布 px，`BattleDriver.cs:4004-4014`）⇒ 真画布中心是 **599.097**；`480.903 = 1080 − 599.097` 只是**翻转后的数**（喂给 `ToWorld` 的 y01 恰好等价于 `1 − 599.097/1080`，所以**值是对的、标签是错的**）⇒ 建议顺手订正文案，免得下一个人「按 480.903 修代码」把方向改反。

### 2 · A388 的 (b) 半（`:39055`）—— 断言前提错，不是实现漏摘

**断言原文**（`BattleScene.cs:10331-10347`）：先 `driver.Begin(...)` 把钩子接回来（`hookedBeforeB = 17 > 0` ✓ **前提这半是对的**，H1 §三 那条要求也照做了），再 `DestroyImmediate(driver)`，断 `StaticHookCount == 0`。
**实得**：卸前 17 → 现存 **17**。

**为什么只有「OnDestroy 没被调」这一种可能**：
1. `DetachStaticHooks()` 本身**没问题**：同一轮里 (a) 段直接调它 = **摘前 19 → 摘掉 19 → 现存 0**（日志 `:38912`，`BattleScene.cs:10314-10317`）。`OnDestroy` 的第一句就是它（`BattleDriver.cs:1767`），**在** `if (_net == null) return;` **之前**。
   ⚠️ **19 与 (b) 那个 17 的差是 2**，差额已能点名：`UnitTweenRuntime.HeroBySeat` / `SeatOf` 这两个槽**只在 `BuildHud` 里赋值**（`BattleDriver.cs:7462/7471`），而 `BuildHud` 头部有 `if (_hudBuilt) return;`（`:7442-7443`）⇒ 第二次 `Begin()` **不会再写它们**（`UnitTweenRuntime.Install()` 只挂 `WFModuleTween.OnInvoke` / `UnitTweenTable.HeroOf` 那两条**不在清单里的**，`Core/UnitTweenRuntime.cs:45-55`）。19 − 2 = **17** ✓ 与 (b) 的前提行（日志 `:39043`）逐字吻合 ⇒ **本条不影响红因**，但见 §五·F。
2. **没有第二处会重新挂上**：全仓挂点只有 `BattleDriver.Begin`（`:1985-1986` 的 `HookAnimFxShake/HookAnimFxCards`）、`AttachPostFx`（`:2205`）与 `Editor/AnimFXCheck.cs`（编辑器自检，自存 `prev`）；`ForEachStaticHook`（`:1794-1841`）是**唯一**的清除清单 ⇒ 「刚摘完又被挂回来」在这条路径上不存在（销毁的那一下不跑 `Begin`、也不跑任何别的 `OnDestroy`）。
3. ⇒ 只剩「`OnDestroy` 压根没跑」。

**环境事实（本仓多次实证）**：
* 自检跑在**编辑模式**：`BattleScene.cs:97` `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)`；这一轮里 `Application.isPlaying == false` 由 A341 那条断言自己盖章（`BattleScene.cs:1817-1821`，日志 `:3717`）。
* `资料/已知的坑.md:709`（2026-09-23）：「**自检跑在编辑模式**，而编辑模式下 `Awake/OnEnable`/`Update` **只对带 `[ExecuteAlways]` 的脚本**才跑」；`:3986`（2026-10-11 FX4，**正/反例同框实证**）：分界**不是**「父链激活不激活」而是「**类型带不带 `[ExecuteAlways]`**」（`WindowsManager` 挂在**激活的**父链下、`Awake` 也没跑）。
* `BattleDriver` = `public class BattleDriver : MonoBehaviour, INetBattleHost`（`BattleDriver.cs:22`）——**没有任何 `[ExecuteAlways]`**（全仓 grep：只有 `WindowHolder` / TMP 那一族带）。
* 旁证：本日志里 `CombatCameraZoom.SubscribeScreenSizeSource()` 的调用栈是 `Initialize(bool)`，**不是 `Awake`**（日志 `:629-637`）——原版这两句就写在 `Awake` 里（`CombatCameraZoom.cs:343-349`），我们是显式补调的。

⛔ **如实标注**：本仓的硬证据覆盖的是 **`Awake`/`OnEnable`/`Update`**；`OnDestroy` 属**同一族**消息、**本仓没有单独实证过**（`W4:98` 那句「`DestroyImmediate` 它**同步**调 `OnDestroy`」就是这个未证的假设）。⇒ **想要把这条坐实**（仍不跑 Unity、写手 5 行就能做）：
```csharp
// 一次性探针（放在 A388 (b) 段里，用完即弃）
// 1) 在场景里 AddComponent 一个【不带 ExecuteAlways】的小 MonoBehaviour（静态计数在 OnDestroy 里 +1）
// 2) DestroyImmediate 它 ⇒ 计数 +1 ⇒ 规矩是「父链激活就派」；计数不变 ⇒ 规矩是「带特性才派」
```
两档都能给出结论（**这就是判「断言前提错」还是「环境限制」的裁断实验**）。

### 3–6 · A431 四条（`:39115` / `:39127` / `:39139` / `:39297`）—— 一条根因：`MakeAnimFx` 在编辑模式【当场销毁宿主】

**链条（全部实读）**：

1. `BattleScene.cs:10390-10392` 实例化 `Resources/ArenaPrefabs/battlearena2.prefab` 的探针根，调 `BuildSceneAnimFx(probe.transform, "battlearena2")`。
2. `ScenarioBlendables.cs:2007-2024` **① 先建缺的节点**：`sceneStandaloneBuild[battlearena2].nodes[]` 有 **3** 条 = `Scenario` → `Scenario/Battle Arena 2 Particles` → `…/RocketTrail` ⇒ 现场 `new GameObject` 三个（日志「新建节点 3」= **期望值，全对**）。
3. `:2027-2051` **② 取旁挂两个开关**：`enabled = 1.0`、`goActive = 1.0`（数据实读）⇒ `goActive = true`，`selfDestroyOk = (en != 0 && goActive) = true`。
4. `:2051` 调 `MakeAnimFx(it, res, true)`；里面 `pd = t.GetF("preventDestroy")` = **0.0**（不 < 0 ⇒ 过第二道闸）、`c.destroyTime = 6.0`。
5. 🔴 `:1709-1718`：
   ```csharp
   if (selfDestroyOk && !c.preventDestroy && c.destroyTime > 0f) {
       SelfDestroyScheduled++;
       if (Application.isPlaying) Object.Destroy(c.gameObject, c.destroyTime);
       else                       Object.DestroyImmediate(c.gameObject);   // ← 编辑模式：当场删
   }
   return c;   // ← c 已经是【已销毁】的组件
   ```
6. 回到 `:2052`：`if (c == null) { missed.Add($"{it.cls}(`{t.leaf}`):`MakeAnimFx` 建不出来（宿主/字段缺）"); continue; }`
   —— Unity 的 `==` 重载把**已销毁**对象判成 null ⇒ **正好命中这一句、正好打印这句话、正好跳过 ③ 改挂**。
7. 后果逐条对上四个数：组件 **0**、新建节点 **3**、改挂 **0**（`continue` 在 ③ 之前）、没对上 **1**；节点被删 ⇒ 第 4 条 `Find(...)` → `null` ⇒ **-1**、第 5 条 `c2 == null` ⇒ `False/False`。

**为什么排除另外两个候选（简报里那两个假设）**：
* **「宿主解析不到 / 旁挂旧了」**：`MakeAnimFx:1657-1667` 与 `BuildSceneAnimFx:2043-2046` 那两条路**都会 `LogWarning`**；本日志在 arena2 那一段（`:39090-39103`）**没有任何 EnvBlend 告警**，紧挨着的下一条就是汇总行 ⇒ 没走到那两条。
* **「`AddComponent` 失败」**（`:1688` 的静默 `return null`）：那样**节点不会被删** ⇒ 第 4 条会打 **0**（节点在、底下 0 颗粒子）而不是 **-1**。实得 **-1** ⇒ 节点确实没了 ⇒ 只有「销毁」解释得通。

**同一次运行里的自证**：A341 那两条断言（`BattleScene.cs:1805-1821`）**恰好**测的就是这段代码，而且**都绿**：
`BT ✓ ★ A341：preventDestroy = false 那条支路真的执行了（计数 0 → 1）` + `BT ✓ ★ A341：批处理这一档（Application.isPlaying == false）走的是 DestroyImmediate ⇒ 探针宿主当场没了`（日志 `:3705` / `:3717`）。
⇒ 同一份代码，一处在**断言它该销毁**（合成探针）、一处在**期待它别销毁**（真数据）—— 两件今天的活**口径打架**，这才是这四条红的本质。

**数据实读**（`Resources/EnvBlendables.json` · mtime **2026-10-05 22:31**）：

| 场 | target | enabled | goActive | preventDestroy | destroyTime | 结果（本轮） |
|---|---|---|---|---|---|---|
| battlearena2 | `RocketTrail` | **1** | **1** | **0** ⇒ 会真自毁 | **6** | **0 建出 / 1 没对上** ← 唯一一条踩中 |
| battlearena3 | `Lightning_Green` | 0 | 1 | 0 | 4 | 2 建出 ✓（`selfDestroyOk=false`） |
| battlearena3 | `Lightning_Green (1)` | 0 | 1 | 0 | 4 | （同上） |
| battlearenatauviorla | `Railgun BIG (1)` | 1 | 1 | **1** | 4 | 2 建出 ✓（`!preventDestroy` 为假） |
| battlearenatauviorla | `Big Gun Effect` | 1 | 0 | **1** | 4 | （同上） |

⇒ **5 条里只有 `RocketTrail` 会踩中**，与「三场合计只有 arena2 那一格红」完全一致。
`sceneStandaloneBuild[battlearena2]` = `nodes 3 / reparent 5`（`BigExplosion · Smoke · Twinkle · Fire Small · Launch Smoke`，**正是断言里点名的那 5 个**）—— 数据侧与期望值**一个数都不差**。

**生产路径影响**：`ArenaRuntimeLoader.Load()`（`:112-114`）在**真 Play** 下 `Application.isPlaying == true` ⇒ 走 `Destroy(go, 6s)`，**不受影响**；这条缺陷**只在编辑模式自检里现形**（但 Arena2 一旦成为自检主场景 —— 现在主场景是 arena1 —— 会同样红）。

**⛔ 口径归调度台**（我不裁）：
* 选项甲（改动最小、A341 不动）：给 `MakeAnimFx` 再加一个「编辑模式是否模拟销毁」的形参，**只有** `BuildSceneAnimFx` 传 `false`（这条链要的就是「组件在 + 5 个子件改挂」，而原版那一刻它们**确实都在**）。
* 选项乙（更彻底）：编辑模式**一律不模拟销毁**（只 `SelfDestroyScheduled++`），同时把 A341 那条「宿主当场没了」改成「宿主还在 + 计数 +1」（**如实说**：编辑模式验不了销毁那半）。

---

## 三、判定：A431 那四条**不是**「A418 没做」引起的（附「真去跑那三步」的连带影响）

**结论：不同源。** 证据：
1. **失败发生在「宿主已找到、字段已读全」之后**：两条会出声的闸门（宿主找不到 / `preventDestroy` 缺）**都没有告警**（§二·3）。
2. **A431 读的是 `Resources/EnvBlendables.json`（mtime 10-05 22:31）+ `Resources/ArenaPrefabs/*.prefab`（三件都是 10-05 17:11）**，而不是 `battlearena2_manifest.json`（09-30 06:47）本身；那 3 个节点是**我们自己新建的**（`nodes[]`），**与 prefab 里原本有什么无关**（实读 prefab 里 `Scenario` / `Battle Arena 2 Particles` / `RocketTrail` **各 0 处命中**）。
3. **同族的两场（arena3 / tau）在本轮**读的也是**同一天（09-30 06:47）的旧清单**，A431 对它们**全绿**（2/4/0/0 与 2/1/0/0 逐数吻合）⇒ 「清单旧 ⇒ 建不出来」在本轮**不成立**。
4. 四条红的四个数**每一个都能由 §二·3 那条链独立解释**，不需要引入第二因。

**但要提醒调度台一件反过来会被踩的事** 🔴：**真去跑** A418 那三步（`gen_unity_arena_manifest.py --arena battlearena2` → `gen_env_blendables.py` → `BuildArenaPrefabs`）之后：
* 按 `资料/普查产出_1012/H6_Embers与工具注释.md` §2.4 的**现读重算**（**只读引用，我未独立复核**）：`reparent[]` 会从 **5 → 6**（多出 `Embers`，闸门①「无贴图」原来把它吃掉了），`nodes[]` 仍 3。
* ⇒ `BattleScene.cs:10382` 的 `wantReparen = { 5, 0, 0 }`（以及断言文案「原版直读 5」、第 4 条点名的 5 个粒子名）**要跟着改成 6**，否则 A431 会**因为另一个原因**再红一次。
* `SceneStandaloneDataCount()`（items 条数 = 5）**不受影响**（H6：`sceneStandalone 3 场 5 条` 不变）。

---

## 四、没查清（⛔ 不猜）

1. **`OnDestroy` 在编辑模式是否一律不派**：本仓硬证据覆盖 `Awake/OnEnable/Update`；`OnDestroy` **没有单独实证**。§二·2 给了 5 行探针，代价极小，建议**先跑它再落这条结论的措辞**。
2. **`EnvBlendables.json`（10-05 22:31）早于 A160（10-06 的跨包锚点修复）**：它对 arena2 那 3 个 `nodes[]` / 5 条 `reparent[]` 的 `pos` 有没有影响，**我没查**（本轮用不到：那 3 个节点是**新建**的、5 个改挂对象在 prefab 里**各只有一份同名**，按「名字 + 最近位置」解析不会撞）。
3. **A423 那一族还有没有别的取整字面量**（同一批 `HudAbs` 调用里 `_chatBtn` 用 `50.9 / 880.2 / 64.44 / 61.85`，其中心 83.12 vs 粗口径 83.15）：**没查**它有没有更细的判据在管（`:8539` 那条容差 1.5 px ⇒ 今天不红）。
4. **`BuildSceneAnimFx` ① 建节点**是**无条件** `new GameObject`（`:2011-2023`，不查「是不是已经有了」）：若将来清单重跑后 prefab 里真出现了这条链，会**建重**（名字重复、`nodes` 计数照样 3、**静默**）。本轮 prefab 里没有 ⇒ 不现形。**属顺手发现，未验证是否有现实触发路径。**
5. **真 Play 下这条链的观感**：runtime 走 `Destroy(go, 6s)` ⇒ 与「原版 `RocketTrail` 6 秒后连同子件消失」同路，**我没跑到实况**（本件不跑 Unity）。

---

## 五、顺手发现（⛔ 只报不改）

| # | 发现 | 出处 |
|---|---|---|
| A | 🔴 **`BuildSceneAnimFx` 把「组件自毁」误报成「宿主/字段缺」** —— 同一句话还会被用来解释「宿主找不到」那两档 ⇒ **本轮把调度台引向了 A418**（诊断成本直接体现在这次六红里）。建议给 `SceneAnimFxMissedWhat` 单开一档理由（例如 `:2052` 那一条之前先判 `c == null && 组件是自毁掉的`） | `ScenarioBlendables.cs:1709-1718` / `:2052` |
| B | 🟡 `MakeAnimFx` 有 **3 处静默 `return null`**（`:1651` targets/res 为空 · `:1721` 循环里没有 `kind=="animfx"` · `:1688` `AddComponent` 返回 null）—— 全都会走到 A 那句话，且**不出声**（本仓红线：不许静默失败） | 同上 |
| C | 🟡 A423 断言的文案「画布 (50.1508, **480.903**)」与真值 **599.097** 不符（`:8539` 那条粗口径用的是 599.1；`HudExtraPosPx` 的口径见 `BattleDriver.cs:4004-4014`）。**值对、标签错**，容易误导下一个人「按 480.903 改代码」 | `BattleScene.cs:10217-10231` |
| D | 🟡 A423 的位置阈值（1e-4 世界单位 = **0.011 px**）**比我们自己字面量的精度（0.1 px 取整）还紧** —— 同族粗口径用的是 1.5 px。这不是「阈值错」，是「实现要照原版精确值写」的提醒 | `BattleScene.cs:10228` / `:8526` |
| E | 🟡 `H1_CombatAutoZoom.md:117` 说「`CheckSavedScene` 的 `OpenScene` 已经把 driver 拆了（`OnDestroy` 跑过、计数早就是 0）⇒ 放它之后恒真」——**若 §二·2 的结论成立，这句前提也是错的**（`OpenScene` 同样不派 `OnDestroy`）。H1 **把这条挪到它前面是对的**（结果没损失），但理由要跟着订正；顺带暴露一个**没人验过**的点：`OpenScene` 之后那 17 条钩子**还指着已销毁的 driver**（正是 A388 要防的事） | `资料/普查产出_1012/H1_CombatAutoZoom.md:117` |
| F | 🔴 **「要先 `driver.Begin(...)` 把这一场接回来」这句只在 17/19 上成立**（本轮实读：A388 (a) 摘掉 **19**，第二次 `Begin()` 只接回 **17**）。差额 = `UnitTweenRuntime.HeroBySeat` / `SeatOf`，它们**只**在 `BuildHud` 里赋值，而 `BuildHud` 被 `_hudBuilt` 闩住（第二次 `Begin` 不再跑）⇒ 谁要是在 (a) 段之后再用补间/VFX 那一族，会**静默**拿到 null（`UnitTweenRuntime.ResolveHero` 遇 null 直接 `return null`，`Core/UnitTweenRuntime.cs:66`）。要修就是「再捞一次那两个槽」或者把 `_hudBuilt` 那道闩与「钩子重挂」分开；**归调度台定，本件没动** | 日志 `:38912`（19）vs `:39043`（17）· `BattleDriver.cs:7442-7443 / 7462-7471` · `Core/UnitTweenRuntime.cs:45-55` |

---

## 六、我这一轮做了什么 / 没做什么

* **读**：日志 6 处现场（含 `:3705-3741` 的 A341、`:629-637` 的 `CombatCameraZoom`）+ 代码 6 处（`BattleScene.cs` A423/A388/A431 三段、`BattleDriver.cs` 的 `HudAbs/OnDestroy/ForEachStaticHook/Begin 挂点`、`ScenarioBlendables.cs` 的 `MakeAnimFx/BuildSceneAnimFx`、`ArenaRuntimeLoader.cs:112-114`）+ 数据 3 处（`Resources/EnvBlendables.json` 解析、`ArenaPrefabs/battlearena2.prefab` 名字命中、三份 manifest / prefab 的 mtime）+ 本仓档案 5 处（`已知的坑.md:709/3986`、`阶段二_滚动与指针_原版规格.md:164`、`FX1` §二、`H6` §2.4、`W4` §三）。
* ⛔ **没跑 Unity** · ⛔ 没动 git · ⛔ 没改任何生产代码 / 正本 / 别人的报告（除本文件）· ⛔ 没把一个推断写成事实（§四 全列了「没查清」）。
* 本文件是**唯一**写盘的东西。
