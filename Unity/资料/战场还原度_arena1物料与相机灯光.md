# battlearena1 战场物料 —— 逐件清单与相机/灯光实值（2026-09-19）

> **用途**：第 12 行「战场还原度」的**开工输入** —— 要把战场从「一张烘平的背景图」还原成真 3D 场景，
> 需要知道的**每一件物料、每个数值**都在这儿（或这儿指的地方）。
> **逐件数值的正本** = `Assets/WarpforgeArena1/arena1_manifest.json`（逐件 pos/rot/**scale**/贴图/粒子参数）
> + `资料/说明书/02_战场_场景/battlearena1.md`（逐节点带坐标）。**本文只记结论、纠错与判据，不重抄那张表。**
> 关联：`资料/战场还原度_差距清单_0917.md`（差集分类）· `资料/3DBody_原版场上卡体规格.md`（3D 卡体）。

---

## 一、总量（实读说明书全树 + 重建场景对账）

**105 条物料** = 灯光 **1** · 相机 **2** · 网格装饰 **29** · 粒子 **60** · 其它 **13**。

---

## 二、四条开工前必须先知道的

### 1. 🔴 重建场景里的「68 处 ParticleSystem」是**组件块数，不是 68 个粒子**

`BattleArena1.unity` 里是 **34 个 `ParticleSystem` + 34 个 `ParticleSystemRenderer`**（合计 68 块）。
说明书里 **60 个**粒子，**只搬了 34 个** —— 但**缺的那 26 个不是战场粒子**：

| 缺的名字 | 个数 |
|---|---|
| `Glow Acummulated (1)` · `Glow Acummulated` · `Energy Moving Left` · `Accumulated Energy` | 各 4 |
| `Glow Acummulated Ring` · `Energy` · `Energy Accumulation VFX On` · `Energy Accumulation VFX Off` · `Shrink glow` | 各 2 |

它们的坐标全在 **x ∈ [−0.5, 77.5]、y ≈ −1.5**（说明书 `:490-501` `:507-514` `:518-525`，Transform `:1260-1458`）——
**不是战场坐标（战场 x≈100），而是 2D 层卡牌/能量条的 UI 粒子**。
⇒ **做 3D 战场还原时可以整批排除**；真正的战场粒子（Embers / WildFire / Steam / Dust Floor / Droppods /
战机 / Shockwave / TinyFlames / ElectricalSparks / RisingSteam …）**34 个全在重建场景里**。
> 拆分法：`arena1_manifest.json` 的 `particles` 数组与说明书粒子表**按下标逐一比对，name 全等**（无错位）。

### 2. 相机（原版是**透视**，我们现在是正交 —— 这是「不像原版」的一大来源）

| 相机 | 值 | 出处 |
|---|---|---|
| **BoardCamera**（主相机） | **FOV = 46.397182** · near 0.3 · far 300 · **lensShift = (0, **−0.205**)** · 世界 pos = **(100.00, 2.222075, −13.57198)** · 朝向 = **单位四元数 (0,0,0,1)**（朝 +Z、无俯仰偏航滚转） | 说明书 `:530` + TF `:269` + manifest + 重建场景 `:153910-153945`（**四处一致**） |
| UI Camera | FOV 40 · near 0.3 · far 300 · pos **(−343.00, 0.00, −18.519)** · lensShift (0,0) | 说明书 `:531` |
| Cinemachine Vcam | 与 BoardCamera **完全同位**（虚拟相机，原版靠 Brain 驱动 BoardCamera） | TF `:317` |

⚠️ `lensShift.y = −0.205` 的作用是**把画面整体上移、让地板占比更大** —— 只抄 FOV 不抄它，透视还是不对。
⚠️ Vcam 的阻尼/跟随参数说明书没给 ⇒ **查不到**；UI Camera 是不是正交也**查不到**。

### 3. 灯光与 RenderSettings

| 项 | 值 | 出处 |
|---|---|---|
| `Directional Light` 类型/颜色/强度 | `m_Type=1`（定向）· color **(1.0, 0.9568627, 0.8392157)**（暖白）· intensity **1.0** · shadows **Soft**(2) | 说明书 `:84` `:529` + TF `:180` + manifest |
| 位置 / 旋转 | 世界 pos **(112.5, 2.89856, −3.337305)** · 局部 quat **(0.085304, 0.887653, −0.41392, 0.182936)** | 同上 |
| 角度（**换算值，非原文**） | euler ≈ **(130°, −23.29°, 0°)**，光轴**俯角约 50°** | 本次用 Unity ZXY 反解 |
| RenderSettings | `fog=False` · `ambientSky=(1,1,1,1)` · `ambientGround=(0.5896226, 0.5896226, 1.0, 1.0)` · `ambientIntensity=0.41` | 说明书 `:532` |

⚠️ **shadow 的细字段**（bias/normalBias/nearPlane/resolution/strength）与 cullingMask/bounceIntensity **查不到**
—— 重建场景里那套是 **Unity 默认值**（`BuildArena1.cs:306` 只写死了 `LightShadows.Soft`），**别当原件值用**。
⚠️ `ambientEquatorColor` / `ambientMode` 说明书没给：重建场景写的是 equator `(0.114,0.125,0.133)` + `Trilight`，
但 `BuildArena1.cs:322-329` 只设了 sky/ground/intensity ⇒ **equator 是 Unity 默认灰、不是原件值**。

### 4. 哪些「看着像物料、其实不是」

- **`Smoke Column` 共 6 根**（`Left 1/2/3` · `Left Light` · `Right 1/2`，说明书 `:161-166`）：
  无 MeshFilter、无 ParticleSystem、**连脚本都没有** = **纯空 Transform**；提取资源目录里也**没有**任何 Smoke Column 资产
  ⇒ **它们就是「已经烘进背景图、不要再摆一遍」的那批**。
  ⚠️ **纠错**：`资料/战场还原度_差距清单_0917.md:74` 写的是「**8 根**烟柱」，说明书全树里只有 **6 个**节点。
  （至于那几缕烟**到底烘在哪张贴图**上，说明书没写 ⇒ **查不到**；参考渲染图里对应位置确实有深色烟羽。）
- **`Sun flare`**（`:121`）：无 MESH 无 PS，但 2D 树 `2D层:39` 标了 `script` ⇒ **挂脚本的光晕节点**，不是网格/粒子。
- **不是物料的定位件**（别当物料摆）：`Cube`×6 · `Collision`×7 · `Floor plane`(子) · `TapParticleController` ·
  `Shadow Receiver`（有材质 `Transparent Shadow Receiver`，但树里无 MESH 标签、工程里也没导这个材质）。
- **三层实体背景板**（是**真网格**、不是「烘」的概念）：`Background`（天空板，z=−3.34）·
  `Back background`（z=44.35）· `Background Building 1/2`（z=90.09）。

---

## 三、重建场景里**没有**、但原版有的（要补的）

| 缺什么 | 说明 |
|---|---|
| `Cache Stealth`（**3D 卡网格 `Card 3D WH40k`**） | 重建场景里**只剩一个空 Transform**（无 MeshFilter/MeshRenderer）。⚠️ 它与「场上卡用 3D 卡体」那件事的关系见 `资料/3DBody_原版场上卡体规格.md`（用途仍未确认） |
| `Cinemachine Vcam` · `UI Camera` | 相机侧的两件 |
| `Shadow Receiver` | 有材质、工程里没导 |
| 准星三件（`CrosshairLine 3D` / `Crosshair` / `Attack Target Reticle`）· 单位底下 3D 高亮三件套（`Minion Position Highlight` + `Shadow` + `Glow`） | 见差距清单 §二 第 15 条 |
| `Particle colliders` ×6 | 粒子碰撞面，**不渲染**，不是视觉缺口 |

---

## 四、出处

- 逐件数值：`Assets/WarpforgeArena1/arena1_manifest.json` · `资料/说明书/02_战场_场景/battlearena1.md`
- 重建场景实读：`Assets/WarpforgeArena1/Scenes/BattleArena1.unity`（**运行时从不加载**，只活在 `ArtBaker` 烘焙管线里）
- 2D 层对照：`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md`
- 我们这侧要改的两处：`Editor/BattleScene.cs:3432`（`BuildScene` 建相机/背景）· `Battle/BattleBackdrop.cs`（现在那张烘平的背景图）