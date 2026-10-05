# H2 · 场景侧 `AnimFXController` 那一族（A393 主 + A395 顺手核 + A394 先查证）（写手 · 2026-10-12）

> 一句话：**A393 的数据与代码都做完了**（旁挂新开 `sceneStandalone` / `sceneStandaloneBuild` 两节 +
> `ScenarioBlendableFactory.BuildSceneAnimFx`，**建法复用同一个 `MakeAnimFx`**）；🔴 **调用点还差一行**
> （在两个白名单外的文件里）⇒ 见 §五「待接线」。**A395 查清了**（不是 prefab 旧，是 `renderMode = 5` 那道闸）。
> **A394 查清了：`modules` 不是空的** —— 7 个实例里 1 个有模块（`AnimFXModuleScreenShake`），要真做得多条线，
> 路线在 §四。

---

## 一、结论（三件各一句）

| 件 | 结论 | 落地 |
|---|---|---|
| **A393** | ✅ **数据 + 代码做完**（那 5 条有账、有建法、有可观测点）。🔴 **本件收工时还没接线**：`BuildSceneAnimFx` 的调用点 = **只有一处**，落点在 `Battle/ArenaRuntimeLoader.cs`（**本件白名单外**）⇒ 本件收工时**一条都不会真建出来**（**后来已由 A417 接上那一行** —— 见 `H7_A393接线.md`）。⚠️ **2026-10-12 更正**：本格原来写「调用点在 `Battle/ArenaRuntimeLoader.cs` **/ `Battle/EnvironmentApplier.cs`**」= **两个调用点**；**实际只有一处**（`EnvironmentApplier.cs` 的 `git diff --numstat` = **无输出**，一个字没动）—— 硬理由见 **§5.1** 那段「为什么不放在 `EnvironmentApplier.EnsureSceneBlendables()`」与 **H7 报告 §二**。⛔ **别再去补第二处**。 | 生成器 + 工厂 + 2 份 JSON |
| **A395** | ✅ **查清**：`battlearena2` 的 `RocketTrail` 在 prefab 里 0 命中**不是「prefab 比清单旧」，是 `ArenaBuilder` 的第四道闸把它挡掉的** —— 清单里那条 `renderMode = 5 (None)`（判据 = `ArenaBuilder.cs:2032-2039`，`p.renderMode == 5 ⇒ nPsNone++; continue`）。**原版自己就是 `None`**（不画这个对象）。 | 只查证，没改 |
| **A394** | ✅ **查清：`modules` 非空**（7 个里 **1** 个）。⇒ 按件里的规矩**把内容列出来 + 给路线，不硬做**（真要做得先给场景侧一条模块线，见 §四）。 | 只查证，没改 |

---

## 二、7 个实例逐条表（UnityPy 直读 15 个 `scenes_scenes_*` 包 · **不设任何过滤**）

探针：`d:/4/_tmp_view/h2_animfx_scan.py`（原始输出 `d:/4/_tmp_view/h2_animfx_scan.json`）·
宿主细节：`d:/4/_tmp_view/h2_animfx_hosts.py`。**与 W3 §七·1 那张表逐格对上**（本件是独立复算）。

| # | 场 | pid | 宿主（原版整条路径） | GO `m_IsActive` | 组件 `m_Enabled` | `preventDestroy` | `destroyTime` | `exitDestroyTime` | sounds / exitSounds / modules | 归谁管 · 我们怎么建 |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `battlearena2` | 4317 | `Scenario/Battle Arena 2 Particles/RocketTrail` | True | 1 | **0** | **6.0** | 3.0 | 0 / 0 / 0 | **不归任何 blendable** → `sceneStandalone` |
| 2 | `battlearena3` | 4187 | `Scenario/Particle Effects/Lightning_Green` | True | **0** | **0** | 4.0 | 3.0 | 0 / 0 / 0 | 同上（**组件关着 ⇒ 原版不跑**） |
| 3 | `battlearena3` | 4450 | `Scenario/Particle Effects/Lightning_Green (1)` | True | **0** | **0** | 4.0 | 3.0 | 0 / 0 / 0 | 同上（**组件关着**） |
| 4 | `battlearenatauviorla` | 4767 | `…/Railgun Turret 2/Railgun Turret Base.002/Cylinder.003/Railgun turret` | True | 1 | 1 | 1.8 | 3.0 | **1** / 0 / 0 | `TauCannonAnimationStopper.animFXController`（**A340 已接**） |
| 5 | `battlearenatauviorla` | 5247 | `…/Railgun Turret 1/Railgun Turret Base.001/Cylinder.001/Railgun turret` | True | 1 | 1 | 1.8 | 3.0 | **1** / 0 / 0 | 同上（A340 已接） |
| 6 | `battlearenatauviorla` | 5360 | `…/Railgun BIG (1)/Big Gun Effect` | **False** | 1 | 1 | 4.0 | 3.0 | **1** / 0 / 0 | **不归任何 blendable** → `sceneStandalone`（**宿主 GO 关着 ⇒ 原版不跑**） |
| 7 | `battlearenatauviorla` | 5474 | `…/Railgun BIG (1)` | True | 1 | 1 | 4.0 | 3.0 | 0 / 0 / **1** | **不归任何 blendable** → `sceneStandalone`（模块那一支见 §四） |

* **排除是按 pid 引用关系做的**（`gen_env_blendables.py` 的 `Bundle.referenced_pids` 扫 `CLASSES` 里所有目标字段），
  **不是**按名字/场次写死的名单 ⇒ 那 2 条（#4/#5）自动不进 `sceneStandalone`。
* 宿主细节（本件实读）：`RocketTrail` GO 上 = `Transform + ParticleSystem + ParticleSystemRenderer + AnimFXController`；
  `Lightning_Green` 两个 GO 上各有 **`DestroyByTime`（`lifeTime = 3.0`）而它自己 `m_Enabled = 0`** ⇒ 也是关的；
  `Railgun BIG (1)` GO 上 = `MeshFilter + MeshRenderer + Animation + AnimFXController + AnimFXModuleScreenShake`。
* 🔴 **这 5 条各自「到底有没有效果」**（决定了这一件值不值得做）：
  | 实例 | 原版真的会发生什么 | 我们不做会怎样 |
  |---|---|---|
  | #1 `RocketTrail` | `destroyTime = 6` ⇒ **进场景 6 秒后销毁自己**，而它下面挂着 **6 个粒子子件**（`BigExplosion` · `Smoke` · `Embers` · `Twinkle` · `Fire Small` · `Launch Smoke`）⇒ **6 秒后整片火箭尾迹消失**。⚠️ 逐条实读：**`looping = True` 的是 `Smoke`(1s) · `Embers`(2s) · `Fire Small`(5s)**（这三条是我们这边「永远在飘」的），另 3 条是一次性（`duration = 2s`，播完自己停） | 🔴 **我们的战场会永远拖着那两条循环粒子**（`Smoke` + `Fire Small`；`Embers` 那条**我们根本没建**，见 §八·1）——**这是本件唯一有可见效果的一条** |
  | #2 / #3 `Lightning_Green` | 组件关着、`DestroyByTime` 也关着 ⇒ **什么都不发生** | 无（如实记着即可） |
  | #6 `Big Gun Effect` | GO 关着 ⇒ **什么都不发生** | 无 |
  | #7 `Railgun BIG (1)` | 无音效、`preventDestroy = 1` ⇒ 不销毁；`modules` 那一支见 §四 | 无（它是模块那件事的锚点） |

---

## 三、改动清单（文件:行号 = **本件改完那一刻现读**）

| # | 文件:行号 | 改了什么 | 为什么 |
|---|---|---|---|
| 1 | `工具/gen_env_blendables.py:173` | 新增 `SCENE_STANDALONE_CLASSES = ('AnimFXController',)` | 「场景侧、不被任何 blendable 引用」这一族的类名表（与 A137 的 `STANDALONE_CLASSES` 并列） |
| 2 | `工具/gen_env_blendables.py:83-90` | `import gen_arena_groups as G` | 建节点/改挂的判据**已经在那个模块里**（原版包读法 + no-scale 世界链 + `ArenaBuilder` 闸门对读）⇒ 复用，⛔ 不写第二遍（铁律 6） |
| 3 | `工具/gen_env_blendables.py:649-669` | 新增 `Bundle.referenced_pids()` | 「这个 pid 已经被某条 blendable 收走了吗」= 排除依据（同一个对象上建两次组件是错的） |
| 4 | `工具/gen_env_blendables.py:676-726` | 新增 `Bundle.collect_scene_standalone()` | A393 的正体：收那 5 条，外加两个**开关**键 `enabled`（组件 `m_Enabled`）与 `goActive`（**沿父链与过**的 `activeInHierarchy`） |
| 5 | `工具/gen_env_blendables.py:853-975` | 新增 `group_node_paths()` + `scene_standalone_build()` | 算「要新建哪些节点」（祖先链 + 宿主自己 · 原版 local TRS）与「哪些已建子件要改挂回宿主下」；**复用 `G.Scene` / `G.is_built` / `G.quality_off`** |
| 6 | `工具/gen_env_blendables.py:1114-1186` | 新增 `check_scene_standalone()` | 三条自检（宿主在不在 · `nodes[]` 的父路径通不通 · `reparent[]` 的名字在不在）；不通过就进 `_unresolved` |
| 7 | `工具/gen_env_blendables.py:1316-1345` | `main()`：场景侧每场多跑一遍 `collect_scene_standalone` + 算 `scene_standalone_build` | 接上 §5 |
| 8 | `工具/gen_env_blendables.py:1352-1400` | `stats` 加 6 个计数 + 一段 `print`（逐条列路径、逐场列 `nodes`/`reparent`） | **可观测点**：`--check` 的输出里能一眼看到这 5 条与它们缺什么 |
| 9 | `工具/gen_env_blendables.py:1422-1435` | `_schema` 订正（补 `sceneStandalone` / `sceneStandaloneBuild` 两节的口径） | 铁律 5 |
| 10 | `工具/gen_env_blendables.py:1447-1464` | 全量 JSON 新增 `sceneStandalone` / `sceneStandaloneBuild` / `_sceneStandaloneMissing` | ⚠️ **没有**把这几条塞进 `_missingTargets` —— 那张表是 `gen_arena_groups.py` 的输入，塞进去会**改动 A191 已验收的那两份 `_groups.json`**（理由写在 `_sceneStandaloneMissing` 那条注释里） |
| 11 | `工具/gen_env_blendables.py:1517-1535` | 摊平版 JSON 新增同两节（**形制与 `standalone` 完全一样** ⇒ 运行时复用 `EnvBlendables.Group`，不新开 DTO） | 同上 |
| 12 | `…/Battle/ScenarioBlendables.cs:1639` | `MakeAnimFx` 加参数 **`bool selfDestroyOk = true`** | 🔴 原版那句 `OnEnable`（含自毁）**只在「组件开着 ∧ 宿主 GO 开着」时才跑**；`Lightning_Green` 那两条如果不传 `false`，我们会排定一次**原版根本不会有**的销毁（`Destroy(go,t)` 取消不掉）。默认 `true` ⇒ **blendable 那条路一个字节不变** |
| 13 | `…/Battle/ScenarioBlendables.cs:1671-1684` | 那两句自毁条件外面再套一层 `selfDestroyOk` | 同上 |
| 14 | `…/Battle/ScenarioBlendables.cs:1809-1852` | 新增类注释块（这一族是什么、怎么建、**调用点待接线**） | 让下一个会话知道这条链的存在 |
| 15 | `…/Battle/ScenarioBlendables.cs:1857-1892` | 新增 DTO `AnimFxNode` / `AnimFxReparent` / `AnimFxBuildGroup` / `SceneStandaloneFile`（**局部 DTO**，照 `EnvironmentApplier.StandaloneFile` 那个先例） | `Core/EnvBlendables.cs` 不在白名单里，而 `JsonUtility` 会**忽略**目标类型没有的键 ⇒ 只解析自己要的那两节 |
| 16 | `…/Battle/ScenarioBlendables.cs:1894-1939` | `LoadSceneStandalone()` + `SceneStandaloneDataCount()` + 5 个可观测属性 | 自检用；`SceneAnimFxCalls` 能把「数据在、但没人调」与「建了 0 条」分开 |
| 17 | `…/Battle/ScenarioBlendables.cs:1949-2070` | **`BuildSceneAnimFx(arenaRoot, arenaKey)`** | ① 建缺的节点（浅→深 · 原版 local TRS）② **调 `MakeAnimFx`**（同一个建法）③ 按 `enabled`/`goActive` 设开关 ④ 改挂 ⑤ 计数 + 逐条出声；**同一战场实例不重复建**（判据 = 传进来的那个 root，不是场名 ⇒ 换场重建时会重新建） |
| 18 | `…/Battle/ScenarioBlendables.cs:2072-2106` | `ResolveAnimFxParent` / `FirstAnimFxTarget` / `Vec3` / `Quat` / `Normalize` | 父路径解析走**生产那份 `SceneResolver`**（名字 + 最近位置），⛔ 不另写一套找对象的 |
| 19 | `…/Battle/ScenarioBlendables.cs:1-40 区（文件头）`·`1693-1706`·`1645-1655` | 三处**过期话订正** | ① 文件头补 A393 那一节；② `SelfDestroyScheduled` 的注释原来写「那 3 个不归任何 blendable 管 ⇒ 工厂今天仍走不到」——**那一节现在开了**；③ `MakeAnimFx` 里 `preventDestroy` 那段「它们走不到这里」同样过期 |
| 20 | `MyGame/Assets/Resources/EnvBlendables.json` · `数据/游戏数据/env_blendables.json` | **由生成器重生成**（没手改一个字节） | 见 §六 |

* **⛔ 没碰**：`ArenaBuilder.cs`（实测它在工作区里有一份 **A349 的注释订正**，**不是本件改的**，`+9/−1`）·
  `ArenaBuilder` 相关的 sidecar 与 `arenas/**`（一个字没动 —— 本件的建节点/改挂走**运行时**，
  不经过 `_groups.json`）· `Battle/AnimFXController.cs` · `Editor/*.cs` · `Shell/*` · `BattleDriver.cs`。
* **行尾**：改完二进制数过 —— `ScenarioBlendables.cs` **CRLF 0 / LF 2260** ·
  `gen_env_blendables.py` **0 / 1543** · 两份 JSON **0 / 29784 · 0 / 28817**。全程 Edit/Write，**没用 `sed -i`**。

### 3.1 「怎么建」的答案（逐条带出处 —— 这是 A393 要判的那一跳）

| 问题 | 答案 | 出处 |
|---|---|---|
| 进哪一节 | 新开 **`sceneStandalone`**（形制与 prefab 侧的 `standalone` **完全一样**：`EnvBlendables.Group`，每条**一个 target = 组件自己那个 GameObject**，`kind` 仍是 `animfx`） | `gen_env_blendables.py` 的 `collect_scene_standalone`（判据 = `STANDALONE_CLASSES` 那一节 A137 的先例） |
| 挂在哪一级 | **不挂在任何 blendable 上** —— 它们是场景里**常驻**的普通 `MonoBehaviour`（原版序列化在场景里，从「战场出现」那一刻就在）⇒ 入口 = `ScenarioBlendableFactory.BuildSceneAnimFx(arenaRoot, arenaKey)`，**调用点 = 战场实例化之后**（原版没有「等某个环境生效」这一跳） | 原版 `AnimFXController__OnEnable.c`（场景加载即跑）· 我们这边 `ArenaRuntimeLoader.Load` 是战场出现的唯一入口 |
| 组件怎么建 | **复用 `MakeAnimFx`**（同一个 `kind == "animfx"`、同一份字段装配：`preventDestroy` / `destroyTime` / `exitDestroyTime` / `sounds` / `exitSounds` / `modules` 出声） | `ScenarioBlendables.cs` 的 `MakeAnimFx`（A196 + A340 + A341） |
| 宿主我们没有怎么办 | 旁挂 `sceneStandaloneBuild.nodes[]`（**祖先链 + 宿主自己** · 原版 local TRS）⇒ 运行时现场建 | 判据 = 原版场景包直读；**复用 `gen_arena_groups.Scene`**（no-scale 世界链 / local TRS 的口径只留那一处） |
| 为什么要 `reparent[]` | `RocketTrail` 那颗 `destroyTime = 6` 一销毁要**连带 6 个子件粒子一起消失**；我们那 6 个粒子是**平铺**在战场根下的 ⇒ 不改挂就只销毁一个空壳（**静默**少一半效果） | 本件实测：6 个子件的世界位置与清单**逐轴吻合到 1e-5**（`h2_animfx_hosts.py`） |
| 为什么只给 `RocketTrail` 收 `reparent` | 只有它「真会销毁宿主」（组件开着 ∧ GO 开着 ∧ `preventDestroy = 0`）；另 4 个永远不销毁任何东西 ⇒ 对它们改挂只是白白动树 | `scene_standalone_build` 里那道闸 + §二那张「到底有没有效果」表 |

---

## 四、A394 的答案：`modules` 层**不是空的**

### 4.1 事实（UnityPy 直读）

**7 个实例里 `modules` 非空的 = 1 个**（其余 6 个都是 `[]`）：

* **`battlearenatauviorla` pid 5474**（宿主 `…/Railgun BIG (1)`）：`modules = [ {m_FileID: 0, m_PathID: 5320} ]`。
* 那个 pid **5320 就在同一个场景包、同一个 CAB 里**（`m_FileID: 0`），类名 = **`AnimFXModuleScreenShake`**，
  它**挂在同一个 GO 上**（`m_GameObject` = 1143 = `Railgun BIG (1)`）。**逐字段实读**：

  | 字段 | 值 | 出处 |
  |---|---|---|
  | `actionStart` | **0** | `AnimFXModuleBase.ActionStart` 枚举：`Initialize = 0 / Exit = 5 / DoDestroy = 10 / Manual = 15`（判据 = 签名桩 `AnimFXModuleBase.cs`） |
  | `cameraShakes` | `[]`（空） | 同上 |
  | `manualTriggerCameraShakes` | **1 条**：`presetSO = {m_FileID: 20, m_PathID: -8043713765525655674}` · `delay = 0` · `overwriteSustainTime/Amplitude/Frequency/Direction/AttackTime/DecayTime` **全是 0**（= **不覆盖**，用 preset 自己的值）· `direction = (0,0,0)` | 同上 |

  ⚠️ `presetSO` 是**跨包引用**（`m_FileID: 20`）；W3 那边记的解是 **`Shake Earthquake`** SO（在
  `数据/游戏数据/animfx_modules.json` 的 `Scenario` 组里）—— 本件**没有重新解这一条**（只读到了引用的形状）。

### 4.2 结论 + 路线（⚠️ **本件没做**，按件里的规矩只给路线）

**结论：`modules` 非空 ⇒ 按件里的规矩「列出来 + 给路线，别当场硬做」。** 要做它得先建**三条**东西：

1. **场景侧一条模块线**：`AnimFXController.modules` 的类型是 `List<AnimFXModuleBase>`；我们这条线**没有**这个基类
   （特效那条线的 `WarpforgeVFX.WFEffectModule.Initialize` 收的是 `WarpforgeEffectPlayer`，是**另一个控制器**）
   ⇒ 要一个**收 `AnimFXController`** 的模块基类 + `SetData` 里那句 `m.Initialize(this)` 落地。
   出处：`AnimFXController.SetData`（我们那份现在**出声**不做，见「有意偏离 ③」）。
2. **一套相机震动**：`OverwriteCameraShakePreset`（`presetSO/delay/overwrite*/amplitude/...`）+
   那个 preset SO（`Shake Earthquake`）+ 谁来真的摇相机。**今天工程里没有**（要复核，本件只查到引用形状）。
3. **触发点**：`actionStart = 0 (Initialize)` ⇒ 下一次摇动发生在 `SetData` 那一刻。
   ⚠️ **但 `SetData` 的调用点全是卡的**（`CardScript.PlayTriggerAnim` / `CardAnimController.Initialize` /
   `RemnantBody.DoDestroyByAttack` / `RemnantAeldari.CollectWaystoneEffect`）—— `Railgun BIG (1)` 是**战场物件**，
   ⇒ **原版这个模块很可能一次都不触发**（**没查清**，见 §七）。

⇒ **建议**：单开一件（判据已在上表），**不要**并进 A393 —— 它牵出的是「场景侧模块线 + 相机震动」两条新线。

---

## 五、待接线（本件白名单外 —— 请调度台派）

> 🔴 **今天这 5 条一条都不会真建出来**：数据在、代码在，**但没人调 `BuildSceneAnimFx`**。
> 可观测点 `ScenarioBlendableFactory.SceneAnimFxCalls`（恒 0 = 还没接线）。

### 5.1 调用点（**一行**）

**落点**：`MyGame/Assets/CardPresentation/Battle/ArenaRuntimeLoader.cs` 的 `Load()`，
在 `Current = Instantiate(prefab, transform);` + `Current.name = ...; CurrentKey = arenaKey;` **之后**、
`ArenaSceneState.Apply(...)` **之前**（认**代码锚点**、别认行号 —— 那个文件本波没人在改，但行号会漂）：

```csharp
            Current = Instantiate(prefab, transform);
            Current.name = "Warpforge_" + arenaKey;
            CurrentKey = arenaKey;
            LoadCount++;
            // 🆕 A393：场景侧那 5 条 `AnimFXController`（不被任何 blendable 管）—— 原版它们
            //   序列化在场景里 ⇒ 从「战场出现」这一刻就该在。判据 → 资料/普查产出_1012/H2_场景侧AnimFX.md
            CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(Current.transform, arenaKey);
```

* **为什么不放在 `EnvironmentApplier.EnsureSceneBlendables()`**：那条链只在**打出一张进攻卡**时
  才第一次跑（`BattleDriver.cs` 里 `_envApplier.Apply(...)` 的唯一调用点是进攻卡那一段）⇒ 放那儿
  这些组件**大半局都不存在**，与原版不符。
* **幂等**：`BuildSceneAnimFx` 自己按**传进来的那个 root** 去重（同一实例不重复建；换场 `Unload` 之后
  新实例会重新建）⇒ 调用点不用做任何判重。

### 5.2 断言（落点 = `CardPresentation/Editor/BattleScene.cs`，本件不碰）

**A · 数据侧**（跟场次无关，最便宜的一条）：
`ScenarioBlendableFactory.SceneStandaloneDataCount() == 5`（原版直读 = 5；`-1` = 那节没读到）。

**B · 生产那一跳**（**必须在跑完 `ArenaBuilder.BuildArenaPrefabs` 之后**；用**独立探针根**，
别挂在 `tvInst` 里扰动既有断言；收工 `DestroyImmediate`）：

```csharp
// 用生产那份 prefab，不手搓
var p2 = Resources.Load<GameObject>("ArenaPrefabs/battlearena2");
var probe = UnityEngine.Object.Instantiate(p2);           // 或 Instantiate(p2, null)
int n = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probe.transform, "battlearena2");
Check(n == 1
   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated == 3   // Scenario / Battle Arena 2 Particles / RocketTrail
   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented == 5     // BigExplosion/Smoke/Twinkle/Fire Small/Launch Smoke
   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed == 0,
  "★ A393：`battlearena2` 的 `RocketTrail` 那一族建出来了（1 组件 + 3 节点 + 5 改挂；原版那个
   `destroyTime = 6` 一销毁要连带这 5 个子件粒子）…");
// 再断一条「父子关系真的建立了」（比数字更硬）：5 个改挂对象的父必须是那颗 RocketTrail
var rk = probe.transform.Find("Scenario/Battle Arena 2 Particles/RocketTrail");
Check(rk != null && rk.GetComponentsInChildren<ParticleSystem>(true).Length >= 5, "…");
UnityEngine.Object.DestroyImmediate(probe);
```

* **期望值（写死的那几个 —— 判据是原版包，不是我们的实现）**：
  `battlearenatauviorla` ⇒ `Built == 2` · `NodesCreated == 1`（只有 `Big Gun Effect`）· `Reparented == 0` ·
  且 `Big Gun Effect` 上那颗组件的 **`enabled == true` 而 `gameObject.activeSelf == false`**（照抄原版）、
  `Railgun BIG (1)` 那颗的 `preventDestroy == true`。
  `battlearena3` ⇒ `Built == 2` · `NodesCreated == 4` · `Reparented == 0` · **两颗组件 `enabled == false`**（原版就是关的）。
  ⚠️ **同一个 `arenaRoot` 调两次 ⇒ 第二次返回 0**（幂等）；**换一个 root（新实例）再调会重新建** ⇒
  自检里每场用**各自的探针根**。
* **🧨 改坏法**：把 `gen_env_blendables.py` 的 `main()` 里那句 `sb.collect_scene_standalone(scripts)` 去掉、
  重生成旁挂 ⇒ `sceneStandalone` 缺 ⇒ `BuildSceneAnimFx` 出声且返回 0 ⇒ **红**。

### 5.3 一处**已过期、但本件不能改**的注释（`Battle/AnimFXController.cs` —— 白名单外）

那个文件里**三处**还写着「那 3 个 `preventDestroy = 0` 的实例不归任何 blendable 管 ⇒ 我们这条链根本不建它们」
（文件头「场景侧实测」那段 · 「有意偏离 ③」的计数那段 · `preventDestroy` 字段那段）——**A393 之后不成立了**
（它们现在有专门的入口）。**请派一个能改那个文件的写手订正**（铁律 5：记错的就地改掉）。

---

## 六、数据变更（两份 JSON 的差，逐条核过）

* **本件带来**：`MyGame/Assets/Resources/EnvBlendables.json` 的 `git diff` = **+693 / −0**（纯新增）——
  多出的是 `sceneStandalone`（3 场 5 条）+ `sceneStandaloneBuild`（8 个节点 + 5 条改挂）。
  **target 总数、`prefabs` / `scene` / `standalone` 三节一个字节没动**。
* `数据/游戏数据/env_blendables.json` = **+736 / −54**。那 **54 行删除不是本件带来的**：
  它含 W3（2026-10-12）那次的「顺带落回真值」（`scene_arenas_ok 11 → 13` · `_missingTargets 5 → 0` ·
  `_standaloneMissing` 那条 A211 的 `Particles Orbital`）—— 那份文件在 `HEAD` 里还是 A191/A211 **之前**的版本，
  而工作区里已经是 W3 重生成过的 ⇒ diff 里那 54 行是**两次重生成叠在一起**的结果。**逐行看过：全是
  `_schema` / 3 个 stats 计数 / `_missingTargets` 5 条 / `_standaloneMissing` 1 条**，没有一条 target 或字段被删。
* 新增计数（跑完写盘的实测值）：`scene_standalone_arenas 3` · `scene_standalone_items 5` ·
  `scene_standalone_nodes 8` · `scene_standalone_reparent 5` · `_unresolved = []` · `_missingTargets = []` ·
  新增 `_sceneStandaloneMissing` = 3 条（**已声明缺口**，不是 `_unresolved`）。
* ⚠️ **没有把这几条塞进 `_missingTargets`**：那张表是 `gen_arena_groups.py` 的**输入**（它会把整棵子树展开成
  空节点）⇒ 塞进去会**改动 A191 已验收的那两份 `_groups.json`**（`battlearenatauviorla` 会多出 20+ 个空节点、
  `battlearenadarkangels` 也会被动到）。本件走**运行时**那条路（`sceneStandaloneBuild.nodes[]`）。

---

## 七、没查清 / 本件做不到的（⛔ 不猜）

1. 🔴 **调用点不在本件白名单** ⇒ 今天 `SceneAnimFxCalls == 0`，**一条都没真建出来**（见 §五）。
2. **批处理 / 运行时的行为一次 Unity 都没跑过**（红线：本件不跑 Unity）⇒ `nodes[]` 建成什么样、
   `reparent` 的 `SetParent(host, true)` 会不会与既有断言打架，**只能静态核对**。
3. **原版那个 `AnimFXModuleScreenShake` 到底会不会触发**：`actionStart = 0 (Initialize)` 指向 `SetData`，
   而 `SetData` 的 4 个调用点全是**卡**的（不是战场物件）⇒ 怀疑它**一次都不触发**。
   **要查清还差**：在 `decomp_full` 里把 `AnimFXController__SetData.c` / `AnimFXModuleScreenShake__*.c`
   的**调用者**全找出来，再核 `Railgun BIG (1)` 上的 `Animation` 组件有没有把
   `AnimEventDoShake` / `TriggerCameraShake` 挂在 animation event 上（那两个是 `public`）。
4. **`presetSO`（`{m_FileID: 20, m_PathID: -8043713765525655674}`）是哪个 SO**：本件**直接沿用** W3 记的
   `Shake Earthquake`（`animfx_components.json` 的 `Scenario` 组），**没有重新解这一条引用**。
5. **`RocketTrail/Embers` 的 `texFile` 为空**（见 §八·1）—— 是「材质真无贴图」还是「清单生成器漏解」，**没查**。
6. **`-1f` 哨兵这个形制**：我给 `enabled` / `goActive` 用的是与 `preventDestroy` **同一条**哨兵判据
   （`GetF(k, -1f)` ⇒ `< 0` = 键不在）。W3 那边（`soundUnresolved`）走的是**另一个形制**（显式标记键），
   理由是 `EnvironmentApplier.HasField` 是 `private`。**本件没有引入 `HasField` 的第二份**，
   但两处形制仍然不统一 —— **请调度台裁**（要么把 `HasField` 收口到 `Core/EnvBlendables.cs` 两处共用，
   要么认下这两档并存）。
7. **新建的那个 `Scenario` 分组节点**会不会与别的按名字找对象的代码撞车（我们 13 场平铺战场里
   **本来没有** `Scenario` 这个根节点）—— 本件只核到：`check_scene_side` / `SceneResolver` /
   `ArenaBuilder.FindBuilt` 都是**名字 + 位置**、不依赖「它在哪一层」⇒ 应当无影响，但**没跑过**。

---

## 八、顺手发现（⛔ 一个都没改，只报）

1. 🟡 **`battlearena2` 的 `RocketTrail/Embers` 那条粒子在清单里 `texFile = None`** ⇒ 被 `ArenaBuilder`
   的「无贴图」闸（`:2011-2016`）挡掉、**我们根本没有这颗粒子**（所以它不在 §二的 `reparent` 名单里 ——
   名单是「我们真建出来的」）。**原版那颗是有的**（`ParticleSystemRenderer.m_Materials[0]` =
   `{m_FileID: 8, m_PathID: -3007032700292194130}`，实测）⇒ 嫌疑是**清单生成器漏解了它的贴图**，
   不是「原版无贴图那一类」。**没查清**是哪种 ⇒ 归战场线。
   （判据：`h2_animfx_scan.py` 的两条实测输出 + `battlearena2_manifest.json` 的那条 `Embers`。）
2. 🔴 **`工具/gen_arena_groups.py` 的 `Scene.chain()` 碰上 `RectTransform` 会抛 `KeyError`**
   （它只索引 `Transform`：`self.tr = {p: d for p, (k, d) in t.items() if k == 'Transform'}`，
   而 `chain` 顺着 `m_Father` 走、父可能是 `RectTransform`）。
   实测：对**全部** GameObject 调一次 `chain` 就炸（`KeyError: 2981`）。
   今天不触发（它只对 `wanted_paths` 里那几条调），**但 `gen_env_blendables.py` 新调它的那条路我也加了
   `try/except` + 出声**（那次改动之后 `--check` 全过、没有触发）。⇒ **建议**：那个文件要么把
   `RectTransform` 也收进去，要么显式出声。
3. 🟡 **另一个写手的半成品**（⛔ 本件没碰、**收工时已自行补完**）：`Shell/RewardWindow.cs(591,13)` 与
   `(737,13)` 曾报 **`CS0103: 当前上下文中不存在名称 TickAppearFx / ClearAppearFx`**（运行时程序集 2 条）。
   **判据**：两条诊断**全部集中在一个不是本件的文件**上 ⇒ 不是本件的问题；本件的文件
   （`ScenarioBlendables.cs` / `gen_env_blendables.py`）**全程零诊断**。
   本件收工前的**最后一次**类型检查已经是 **0 / 0**（见 §九）。
4. **`ArenaBuilder.cs` 在工作区里有一份不属于本件的改动**（`+9/−1`，标注 **A349** 的注释订正）；
   **本件一个字没动它**（`git diff` 核过）。
5. 🟡 `gen_env_blendables.py` 的 `print`：「owner 分组节点不在平铺清单里的场」那条**信息**里包含
   `battlearena2(1)` / `battlearena3(2)` —— 那 3 条正是本件这 5 个里的 3 个宿主所在的场
   （它们的 owner 是原版分组节点）。**不是新问题**，只是说明这条信息与本件同源。
6. ⚠️ **W3 报告里那句「派单里的白名单路径有一处不存在」（`Assets/CardPresentation/Resources/`
   下没有 `EnvBlendables.json`）仍然成立** —— 本件按生成器的真实路径（`MyGame/Assets/Resources/`）重生成，两份产物都写了。

---

## 九、跑过的检查（原文）

```text
# ① 生成器自检（本件的验收条件）
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe 工具/gen_env_blendables.py --check
  ⇒ ✅ 两侧自检全过（`_unresolved` 空；已知缺口见上面那条「（信息）」与 `_missingTargets`）
  ⇒ 条目 139 · standalone：13 个根（13 个对上）/ 27 条 = spawner 24 + controller 3 · 缺根 0
  ⇒ 🆕 sceneStandalone（A393）：3 场 / 5 条 · 要新建节点 8 个 · 要改挂回宿主下 5 个
  （逐场 nodes/reparent 的清单见上面那段输出；`read soundcollection_assets_all.bundle` 那次也照旧）
# ② 写盘（两份产物都由它写；本件一个字节没手改）
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe 工具/gen_env_blendables.py   ⇒ 写出两份 JSON
# ③ 秒级类型检查（改完 .cs 的最后一次 —— **收工前跑了两次，第二次在全仓都干净之后**）
TMPDIR=/tmp/wf_h2 bash d:/4/Unity/工具/typecheck.sh
  运行时错误数: 0
  编辑器错误数: 0
  （中间某一次跑出 `运行时错误数: 2`，**全是别的写手的**：Shell\RewardWindow.cs(591,13) / (737,13) 的
    `CS0103`（那两句半成品）。判据：诊断**只出现在不是本件的文件**上 ⇒ 不是本件的问题；那两处补完之后
    最终两行都是 0。本件的文件全程零诊断 —— 第一次跑（`ScenarioBlendables.cs` 刚加完那一大段）就是 0/0。）
# ④ 探针（只读，不需要 Unity）
D:/2/Warpforge_tools/py312/python.exe _tmp_view/h2_animfx_scan.py     # 7 个实例的逐条字段
D:/2/Warpforge_tools/py312/python.exe _tmp_view/h2_animfx_hosts.py <场> <GO 名>   # 宿主的组件/父子/谁引用它
```

* ⛔ **没跑 Unity**（红线）· ⛔ 没动 git（只读了 `git diff --numstat` / `git status`）· ⛔ 没改两张正本 ·
  ⛔ 没越白名单（动过的文件 = §三 那一张表）· ⛔ 没跑自检（用户口径：A 表清完再跑）。
