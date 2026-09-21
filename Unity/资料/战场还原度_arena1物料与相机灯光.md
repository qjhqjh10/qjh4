# battlearena1 战场物料 —— 逐件清单与相机/灯光实值（2026-09-19）

> **用途**：第 12 行「战场还原度」的**接线输入与出处** —— **2026-09-20 已落地**：战场已改成**真 3D**
> （`Arena3D` = 重建的 28 个道具网格 + 34 个粒子系统，由一台**透视** `BoardCamera` 画；烘平的背景图**退成兜底**）。
> 本文是**接线当时用的输入与出处** —— 需要知道的**每一件物料、每个数值**都在这儿（或这儿指的地方）。
> **逐件数值的正本** = `Assets/WarpforgeArena1/arenas/battlearena1/battlearena1_manifest.json`（逐件 pos/rot/**scale**/贴图/粒子参数）
> + `资料/说明书/02_战场_场景/battlearena1.md`（逐节点带坐标）。**本文只记结论、纠错与判据，不重抄那张表。**
> 关联：`资料/战场还原度_差距清单_0917.md`（差集分类）· `资料/3DBody_原版场上卡体规格.md`（3D 卡体）。

---

## 一、总量（实读说明书全树 + 重建场景对账）

**105 条物料**（这是 **arena1 说明书全树**的数）= 灯光 **1** · 相机 **2** · 网格装饰 **29** · 粒子 **60** · 其它 **13**。

⚠️ 「网格装饰 **29**」是**清单条数** —— 实际建出 **28**、跳过 **1**（`Cache Stealth`，见 §三）；
逐场「清单条数 / 建出 / 跳过 / 粒子数」= `资料/普查产出_0920/场景光照与后处理_原版规格.md` §10.5。

---

## 二、四条开工前必须先知道的

### 1. 粒子的对账判据

> 拆分法：`battlearena1_manifest.json` 的 `particles` 数组与说明书粒子表**按下标逐一比对，name 全等**（无错位）。

⚠️ 两条**容易数错**的：① 重建场景 `Scenes/battlearena1.unity`（文件名**小写**；13 场**逐场一个**）里是
**34 个 `ParticleSystem` + 34 个 `ParticleSystemRenderer`** —— 「68 处」是**组件块数、不是 68 个粒子**；
说明书里 60 个、只搬 34 个，**缺的那 26 个是 2D 层卡牌/能量条的 UI 粒子**（名字与坐标 = 说明书
`:490-501` `:507-514` `:518-525` / Transform `:1260-1458`，**做 3D 战场时整批排除**）。
② 逐场粒子数，以及「材质本来就没贴图的粒子被渲成白色方块」那 **18 个（6 场）** 怎么处理，
正本 = `资料/普查产出_0920/场景光照与后处理_原版规格.md` §十 / §十一。

### 2. 相机（**已照原版改成两台**：战场 = 透视 `BoardCamera`（物理相机）、卡牌/HUD = 正交）

| 相机 | 值 | 出处 |
|---|---|---|
| **BoardCamera**（主相机） | **FOV = 46.397182** · near 0.3 · far 300 · **lensShift = (0, **−0.205**)**（⚠️ **静态值只是初值**，见下） · 世界 pos = **(100.00, 2.222075, −13.57198)** · 朝向 = **单位四元数 (0,0,0,1)**（朝 +Z、无俯仰偏航滚转） | 说明书 `:530` + TF `:269` + manifest + 重建场景 `:153910-153945`（**四处一致**） |
| UI Camera | FOV 40 · near 0.3 · far 300 · pos **(−343.00, 0.00, −18.519)** · lensShift (0,0) | 说明书 `:531` |
| Cinemachine Vcam | 与 BoardCamera **完全同位**（虚拟相机，原版靠 Brain 驱动 BoardCamera） | TF `:317` |

⚠️ `lensShift.y = −0.205` 的作用是**把画面整体上移、让地板占比更大**；但**原版运行时还会重算它**
（`BattleCameraSreenSize` → `CameraVerticalFramer.CalculateFraming`）⇒ 静态值只是初值。
⚠️ **那个静态值在我们这儿从来没生效过**（没开物理相机，Unity 静默忽略）；现在由 `BattleScene.BoardFramer` 现算，
**16:9 → `lensShift.y = −0.2103`**。根因、公式与实测 = `资料/3DBody_原版场上卡体规格.md` §四之五。
⚠️ Vcam 的阻尼/跟随参数说明书没给 ⇒ **查不到**；UI Camera 是不是正交也**查不到**。

### 3. 灯光与 RenderSettings

| 项 | 值 | 出处 |
|---|---|---|
| `Directional Light` 类型/颜色/强度 | `m_Type=1`（定向）· color **(1.0, 0.9568627, 0.8392157)**（暖白）· intensity **1.0** · shadows **Soft**(2) | 说明书 `:84` `:529` + TF `:180` + manifest |
| 位置 / 旋转 | 世界 pos **(112.5, 2.89856, −3.337305)** · 局部 quat **(0.085304, 0.887653, −0.41392, 0.182936)** | 同上 |
| 角度（**换算值，非原文**） | euler ≈ **(130°, −23.29°, 0°)**，光轴**俯角约 50°** | 本次用 Unity ZXY 反解 |
| RenderSettings | `fog=False` · `ambientSky=(1,1,1,1)`（⚠️ **场景值 —— 生效的不是它**，见下） · `ambientGround=(0.5896226, 0.5896226, 1.0, 1.0)` · `ambientIntensity=0.41`（**Flat 下不参与**） | 说明书 `:532` |

⚠️ **shadow 的细字段**（bias/normalBias/nearPlane/resolution/strength）与 cullingMask/bounceIntensity **查不到**
—— 重建场景里那套是 **Unity 默认值**（`ArenaBuilder.cs:306` 只写死了 `LightShadows.Soft`），**别当原件值用**。
> 🔴 **2026-09-20 更正**：**上面这两句已过期** —— 细字段**查得到**，就在 `07_场景/<场景>/Light/Light_*.json`：
> `m_Shadows = {m_Type, m_Resolution, m_CustomResolution, m_Strength, m_Bias, m_NormalBias}` · `m_CullingMask` ·
> `m_BounceIntensity`。生成器现在把这些**逐场抄进清单**（`shadowType` / `shadowStrength` / `shadowBias`），
> `ArenaBuilder.ApplyLightAndAmbient` 照着建。逐场值见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §五。

> 🔴 **2026-09-20 更正：`ambientMode` / equator 那两条都错**（原文已删）：
> ① **`m_AmbientMode` 是 3 = `Flat`**（13 场全是），**不是 `Trilight`**；
> ② **equator 原件有值**：`(0.3255, 0.5862, 1.0)`（`RenderSettings_1604.json` 的 `m_AmbientEquatorColor`）——
> 我们当时看到的 `(0.114,0.125,0.133)` 是 **Unity 默认值**（说明那时确实没读到原件）。
> ③ 🔴 **而且是 Flat ⇒ equator 根本不参与**；真正生效的是 **`defaultEnvironment.ambientColor`**
> （运行时覆盖了场景里的 `m_AmbientSkyColor`）。
> 全部证据 = `资料/普查产出_0920/场景光照与后处理_原版规格.md` §二。

### 4. 哪些「看着像物料、其实不是」

- **`Smoke Column` 共 6 根**（`Left 1/2/3` · `Left Light` · `Right 1/2`，说明书 `:161-166`）：
  无 MeshFilter、无 ParticleSystem、**连脚本都没有** = **纯空 Transform**；提取资源目录里也**没有**任何 Smoke Column 资产
  ⇒ **它们就是「已经烘进背景图、不要再摆一遍」的那批**。
  ⚠️ **纠错（2026-09-19）**：原来写的是「**8 根**烟柱」，说明书全树里只有 **6 个**节点。
  （至于那几缕烟**到底烘在哪张贴图**上，说明书没写 ⇒ **查不到**；参考渲染图里对应位置确实有深色烟羽。）
- **`Sun flare`**（`:121`）：无 MESH 无 PS，2D 树 `2D层:39` 标了 `script`。🔴 **2026-09-21 更正：它不是「随便挂个脚本的光晕节点」，是 URP 的 `LensFlareComponentSRP`** —— 引用资产 **`Sun_Flare_1`**（PathID `1192636038419116098`，**7 个元素 + 6 张贴图**），**在原版画面上是一个真的日盘**（arena1 那个位置原版最亮 246 / 我们 204，位置逐点相同）。**已按原版建好**（6 场有：arena1/2/aeldari/astramilitarum/leviathan/tauviorla；生成器 `工具/gen_sunflare_cs.py`）。⚠️ **截图看不到它**（只在 `RenderFinalPass` 画进后备缓冲）⇒ 要验只能进带窗口的 Play 或 player。
- **不是物料的定位件**（别当物料摆）：`Cube`×6 · `Collision`×7 · `Floor plane`(子) · `TapParticleController` ·
  `Shadow Receiver`（有材质 `Transparent Shadow Receiver`，但树里无 MESH 标签、工程里也没导这个材质）。
- **三层实体背景板**（是**真网格**、不是「烘」的概念）：`Background`（天空板，z=−3.34）·
  `Back background`（z=44.35）· `Background Building 1/2`（z=90.09）。

---

## 三、重建场景里**没有**、原版却有的（逐件交代 —— 谁要补、谁不用）

| 缺什么 | 说明 |
|---|---|
| `Cache Stealth`（**3D 卡网格 `Card 3D WH40k`**） | 重建场景里**只剩一个空 Transform**（无 MeshFilter/MeshRenderer）。✅ **2026-09-20 已定案**：它是**场景素材预热缓存**（`m_IsActive:false`，挂在 `Cache [No delete]` 下）—— **不是场上卡**（每场都跳过的就是它）。详见 `资料/3DBody_原版场上卡体规格.md` §四末 |
| `Cinemachine Vcam` · `UI Camera` | 相机侧的两件 |
| `Shadow Receiver` | 有材质、工程里没导 |
| 准星三件（`CrosshairLine 3D` / `Crosshair` / `Attack Target Reticle`）· 单位底下 3D 高亮三件套（`Minion Position Highlight` + `Shadow` + `Glow`） | 见差距清单 §二 第 15 条 |
| `Particle colliders` ×6 | 粒子碰撞面，**不渲染**，不是视觉缺口 |

---

## 四、出处

- 逐件数值：`Assets/WarpforgeArena1/arenas/battlearena1/battlearena1_manifest.json` · `资料/说明书/02_战场_场景/battlearena1.md`
- 重建场景实读：`Assets/WarpforgeArena1/Scenes/battlearena1.unity`（文件名**小写**，13 场**逐场一个**；
  **运行时从不加载**，只活在 `ArtBaker` 烘焙管线里）
- 2D 层对照：`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md`
- 我们这侧**已改**（2026-09-20）：`Editor/BattleScene.cs` 的 `BuildScene` 里建 `Arena3D` + 透视 `BoardCamera`；
  `Battle/BattleBackdrop.cs` **退成兜底**（只在 3D 资产缺失时才用那张烘平的背景图）