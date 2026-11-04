# B139 · 包 Y —— ProjectSettings 与场景 YAML

> 执行代理 `Y` · 2026-11-04 · 输入 = `报告/B138_审计汇总_问题清单.md` §6/§7/§14/§16（＋原报告 `B132_审计_镜头与相机` · `B132_审计_物理与碰撞层` · `B136_审计_光照与后处理` · `B136_审计_网格与预制体结构`）。
> 判据全部**回源现读**（`d:/2/tools/assetripper_out/ExportedProject/` 的原版 `ProjectSettings/*.asset` 与 `Battle Arena 1.unity`），没有直接抄报告数字。
> 只改 `ProjectSettings/*.asset` 与两份场景 YAML；⛔ 未改任何 `.cs` / `.meta` / `Resources/**`；⛔ 未跑 Unity、未动 git、未动活状态表。
> 落点契约：**改完逐字段回读**（`_tmp_view/py/b139_y_verify.py`，全绿）· 账补丁 `账/_parts/B139_Y.tsv`（19 行，`--dry --only B139_Y` 通过：新增 19 / 报错 0）· `typecheck` 0/0。

---

## §一 已完成（逐条）

> 「回读确认」= 改完后用脚本按字段名重新解析该文件并与原版值逐值比对（不是看 diff）。

| 条目ID | 文件:行号 | 原版值 | 改前 | 改后 | 回读确认 |
|---|---|---|---|---|---|
| **7-01**（层名 7 格） | `ProjectSettings/TagManager.asset:20,22,23,25,26,41,44` | `[6]AfterTransparents` · `[8]CustomPostEffectIgnore` · `[9]2DElements` · `[11]BoardCardLimit` · `[12]ParticleTapCollider` · `[27]Blocking` · `[30]GroundCollider` | 7 格全空名；`[30]=WarpforgeIso`（语义冲突） | 逐格同原版 | ✅ `TagManager` **整份文件与原版逐字节相同**（脚本 `tm == otm` 为真） |
| **7-01**（tags 6 条） | `TagManager.asset:7-12` | `particle · FloorPositionReference · PopUpAnchor · UICamera · EditorPreview · MainCanvas` | 只有 `FloorPositionReference` | 6 条、顺序同原版 | ✅ 同上（逐字节） |
| **7-06**（2D 两格） | `ProjectSettings/Physics2DSettings.asset:45-46` | `m_ReuseCollisionCallbacks 1` · `m_AutoSyncTransforms 1` | `0` · `0` | `1` · `1` | ✅ 回读 1 / 1 |
| **7-06**（3D 四格） | `ProjectSettings/DynamicsManager.asset:22,23,30,36` | `AutoSync 1` · `Reuse 0` · `WorldBounds.Extent (256,256,256)` · `DefaultMaxAngularSpeed 7` | `0` · `1` · `(250,250,250)` · `50` | `1` · `0` · `(256,256,256)` · `7` | ✅ 回读逐值对上 |
| **16-01** | `ProjectSettings/GraphicsSettings.asset:92` | `m_TransparencySortMode: 3`（CustomAxis） | `0`（Default） | `3` | ✅ 回读 3；轴 `(0,0,1)` 本来就对 |
| **16-07** | `GraphicsSettings.asset:29-87`（内建项在 `:30-39`） | 内建 **10** 个：`7 / 15104 / 15105 / 15106 / 10753 / 10770 / 10782 / 16000 / 17000 / 16001` | 内建 7 个：`…/10770/10783`（缺 4 多 1） | 内建 10 个、顺次同原版 | ✅ 回读内建列表逐项相等、`10783` 已无 |
| **6-02**（tag 对调） | `CardPresentation/Scenes/Battle.unity:837`（BoardCamera）· `:2681`（HUD 相机） | BoardCamera = `MainCamera` · UI Camera = `UICamera` | `Untagged` · `MainCamera`（对调） | `MainCamera` · `UICamera` | ✅ 回读两个 `m_TagString`；`UICamera` tag 已在 TagManager 里（7-01 同批补） |
| **6-02**（AudioListener） | `Battle.unity` 新增 `!u!81 &557989652`，挂在 GO `557989648`（BoardCamera），并加进该 GO 的 `m_Component` | 全场唯一 listener 挂 BoardCamera | 场景里 **0** 台（运行期补到 `Camera.main` = HUD 相机） | 1 台、在 BoardCamera | ✅ 回读：全场景 `--- !u!81 ` 计 1 个、`m_GameObject = 557989648` |
| **6-03** | `Battle.unity:852`（BoardCamera）· `:2740`（HUD） | `(0.028763004, 0.028794324, 0.029411793, 0.019607844)` | `(0.055, 0.06, 0.08, 1)`（两台） | 两台逐值同原版 | ✅ 回读两台一致 |
| **6-05** | `Battle.unity:887,890`（Board）· `:2774,2775,2778`（HUD） | Board：`HDR 1 / MSAA 0 / Occl 0`；UI：`HDR 0 / MSAA 0 / Occl 0` | 两台全 `1/1/1` | Board `1/0/0`、HUD `0/0/0` | ✅ 回读逐值对上 |
| **6-01**（HUD 相机光学 12 格中可在 YAML 落地的） | `Battle.unity:2739-2764`（`!u!20 &1445258394`） | 透视 `orthographic 0` · `fov 40` · `near 0.3` · `far 300` · `clearFlags 4(Nothing)` · `depth 0` · `projMatrixMode 1` · `focal 50` · `sensor (36,24)` · `lensShift (0,0)` · `orthoSize 5` | `正交 1` · `fov 60` · `0.01` · `200` · `clearFlags 2` | `0 / 40 / 0.3 / 300 / 4`，其余本来就对 | ✅ 回读逐值对上（⚠️ 运行期会被 `LayoutSpace.Apply` 覆写，见 §二/§三） |
| **6-01**（UACD 三格） | `Battle.unity:2698-2700`（`!u!114 &1445258393`） | `renderShadows 0` · `requiresDepth 0` · `requiresOpaque 0` | `1 / 2 / 2` | `0 / 0 / 0` | ✅ 回读对上（`rendererIndex` 未改，见 §三） |
| **14-02** | `WarpforgeArena1/Scenes/battlearena*.unity`（13 场 `!u!104`）＋ `Battle.unity:28` | `m_SubtractiveShadowColor (0.6367924, 0.7275943, 1, 1)` | `(0.42, 0.478, 0.627, 1)`（Unity 默认） | 逐值同原版 | ✅ 13 场 + Battle.unity 全部回读通过 |
| **14-03** | 同上 · `Battle.unity:24-25` | `Equator (0.3254717, 0.5862051, 1, 1)` · `Ground (0.5896226, 0.5896226, 1, 1)` | `(0.114,0.125,0.133)` / `(0.047,0.043,0.035)` | 逐值同原版 | ✅ 14 份文件全部回读通过 |
| **14-04** | 13 场 `!u!157` ＋ `Battle.unity:71,74,88,89` | `BakeBackend 0` · `PVRSampleCount 500` · `GaussRadiusIndirect 5` · `GaussRadiusAO 2` | `2 / 512 / 1 / 1` | `0 / 500 / 5 / 2` | ✅ 14 份文件全部回读通过 |
| **14-05**（`m_StitchLightmapSeams`） | 13 场 arena 场景的**每个** MeshRenderer ＋ `Battle.unity` 18 个 | `0` | 全部 `1`（Unity 默认） | 全部 `0` | ✅ 回读：13 场 1206 个 + Battle.unity 18 个，`": 1"` 残留 **0**（逐场数：63/99/88/62/111/85/82/94/82/88/132/80/140） |

**改动的文件清单（9 类 / 17 份）**：`TagManager.asset` · `Physics2DSettings.asset` · `DynamicsManager.asset` · `GraphicsSettings.asset` · `CardPresentation/Scenes/Battle.unity` · `WarpforgeArena1/Scenes/battlearena*.unity`（13 份）。

**顺带做的一处横切（同一参数类别，铁律 21）**：14-02/14-03/14-04/14-05 的落点原本只写 13 场 arena 场景，但 `Battle.unity` 才是**运行期真正渲染战斗**的场景（`ArenaSceneState.Apply` 只写 `ambientLight/fog/skybox/sun`，**不写** equator/ground/subtractive/烘焙四项），所以这 4 组的同名字段在 `Battle.unity` 也一并改到原版值（零观测差，与 arena 侧口径一致）。已在账补丁的「我们的落点」列写明。

---

## §二 需要 Unity 窗口复核（6 条）

> 本包**未跑 Unity**（红线）。下面每条都写了「跑一次要看什么」。

1. **6-01（HUD 相机）—— 序列化值已对齐，但运行期会被 `.cs` 覆写掉**
   `Core/LayoutSpace.cs:78-83` 的 `Apply(cam)` 每次都强制 `orthographic = true` / `orthographicSize = 5` / `near 0.01` / `far 200` / `position (0,0,-20)`，而 `BattleDriver.Start()`（`:3019`）每次都调它 ⇒ **进 Play 后 HUD 相机又变回正交**。
   复核看什么：① 编辑器里打开 `Battle.unity`，HUD 相机 Inspector 是 `Perspective / FOV 40 / Near 0.3 / Far 300 / Clear Flags Don't Clear`；② 进 Play 后日志无异常、HUD 布局与改动前**逐像素一致**（因为被覆写）。
   **根治不在本包**：要真按原版跑，需 `Core/LayoutSpace.cs` 改口径 + `Battle/HudCanvas.cs:54` 的 `PlaneDistance 20.3 → 100`（原版画布 planeDistance 100；透视 fov40 + 距离 100 ⇒ 1 单位 ≈ 14.83 px，而不是我们现在的 108 px/单位）。
2. **6-02（tag 对调）—— `Camera.main` 从 HUD 相机换成 3D 相机**
   复核看什么：① 战斗中 `AudioListener` 全场**恰好 1 台**（在 BoardCamera 上），且 `BattleDriver.EnsureAudioListener` 不再打印「已补在…」；② 空间音频（左右声道）听感；③ `BattlePostFx.cs:253` / `TargetReticle.cs:519` / `LayoutSpace.cs:157` 三处**兜底**现在会拿到 3D 相机（正常路径都传了显式相机，只有兜底会变）；④ 自检 `BattleScene.cs:25253` 的诊断截图 `Shot(Camera.main, "29_悬停tooltip")` 会变成从 3D 相机拍。
3. **16-01（`m_TransparencySortMode: 3`）—— 全局透明排序换口径**
   这是本批**唯一会影响画面叠放**的改动（所有世界空间透明面片从「按到相机距离」改成「按世界 z」）。复核看什么：战场（卡 / 特效 / 地面贴片）、卡组编辑、外壳各扫一眼有没有「被盖住 / 该盖的没盖住」。按原版这条是**修**不是**坏**，但仍要眼见为实。
4. **16-07（`m_AlwaysIncludedShaders` 删 `10783` 加 4 个）—— 只在 player 构建里能验**
   编辑器里有内置资源兜底，看不出差异。复核看什么：打一次 player，看 UI / 精灵有没有变粉或丢失（`alwaysIncludedShaders` 决定打包时哪些 shader 不被剔除）。
5. **14-04（`m_BakeBackend: 0`）—— Unity 6 已删除 Enlighten 后端**
   值照抄原版了，但 Unity 6 打开场景时可能把它归一回 `2`。复核看什么：打开任一 arena 场景 → Lighting 面板的「Lightmapper」是不是 `Enlighten`（若被 Unity 改回，要在账里记成「引擎不支持、无法保持」）。
6. **7-06（`m_AutoSyncTransforms: 1`）—— 物理同步口径`
   复核看什么：战斗里卡的点选 / 拖拽 / 棋盘射线是否仍然准；以及性能没有明显掉（auto sync 更贵，我们原来的 4 处显式 `SyncTransforms()` 现在变冗余）。

---

## §三 未做 / 存疑（逐条写为什么）

1. **7-07 `m_sendNavigationEvents`（原版 0 / 我们 true）—— 未做，本包没有 YAML 落点。**
   证据：全仓 `grep -rn "m_sendNavigationEvents" Assets/` = **0 命中**；`grep -rn "^--- !u!76 \|m_Name: EventSystem" Assets --include=*.unity` = **0 命中** ⇒ 我们的 `EventSystem` 是**运行期 new 的**（`Battle/BattleEventSystem.cs:164`、`Shell/PointerLayer.cs`），场景里没有可改的序列化字段。
   正确落点 = 那两处 `AddComponent<EventSystem>()` 之后加 `es.sendNavigationEvents = false;` —— 属 `.cs` 包（本包红线：不碰 `.cs`）。已按 状态 `C` 记进账补丁。
2. **6-01 `cullingMask`（原版 HUD 相机 32 = 只画层 5 UI / 我们 4294966015）—— 未做，做了会让整个战斗 HUD 消失。**
   证据链：① `HudCanvas.Ensure` 用 `new GameObject("BattleHud Canvas")` 建根，**layer 0**；② 全仓 `.cs` **没有任何**把 HUD 件放到层 5 的赋值（`grep "\.layer = "` 全是 arena 层 8 / 卡层 10）；③ `LayoutSpace.Apply` **不重置** `cullingMask`，运行期也没有别处写它（`grep cullingMask` 在 `BattleDriver.cs` 只有 1 条**注释**）。
   ⇒ 直接写 32 = 相机不画层 0 = HUD 全没。本包选择**保留现值**并把该格留在账里（状态 `C`），等 HUD 件整体迁到层 5 之后再改这一格（属 HUD 架构包）。原版 `BoardCamera` 的 `cullingMask = 1137` 属 6-06（架构，本来就不在本包）。
3. **6-01 `m_RendererIndex`（原版 1 / 我们 −1）—— 未做，缺「第二份 UI renderer 资产」。**
   证据：`Assets/Settings/` 只有 `PC_Renderer` / `Mobile_Renderer` 两份；`PC_RPAsset.asset:19-21` 的 `m_RendererDataList` 只有 1 项、`m_DefaultRendererIndex: 0` ⇒ 指向 1 会越界回退到 0（等于没改，且可能刷警告）。原版那台是「专用 UI renderer：mask 32 / 0 个 feature / postProcessData 0」（B132 B20）。
   新建 `UI_Renderer.asset` + 改 RPAsset 的列表**不在本包白名单**（本包只碰 `ProjectSettings/*` 与两份场景）⇒ 留给 `Assets/Settings` 所属包；账里同一条 `C`。
4. **14-05 前半：2 份 `LightingSettings` 资产（`Intro/Settings.lighting.lighting`、`Simple Transition/Settings.lighting.lighting`）—— 未建，没有落点。**
   我们这两个场景**没有做成 Unity 场景**（开场是视频、转场是壳内淡入淡出）；`Battle.unity` / 13 场 arena 的 `m_LightingSettings` 都是 `{fileID: 0}`（原版那 13 场也是 0 ⇒ 原版这两份资产只服务那两个我们不复刻的场景）。强行建两份无引用的孤儿资产 = 假复刻 ⇒ 记为 `C`（等开场/转场做成 Unity 场景时随该场景补）。
5. **存疑但不属本包、已确认「现读与报告有出入」的两处**（铁律 5 记录，不改文件）：
   - `B136` 报告写我们 `m_AlwaysIncludedShaders` **54 条**，实测 `git show HEAD` 版本 = **55** 条（现读 58 = 55 − 1 + 4）。报告这一格计数偏 1。
   - `BattleDriver.cs:4879` 的注释写「原版 `level0` 的 `UI Camera` 上有一台全场唯一 listener」，而**战斗场景**里的唯一 listener 在 **BoardCamera**（本轮实读 `Battle Arena 1.unity:30640` 的 `!u!81 &1594`，`m_GameObject → &348`；全场景仅 1 台）。两条说的是两个场景（level0 = 常驻壳 / Battle Arena = 战斗场景），并不冲突，但注释容易被读成「战斗里也挂在 UI Camera」⇒ 建议 `.cs` 包就地把那句补一句限定。

---

## §四 顺手发现（不进本包，交给调度台分流）

1. 🔴 **`Battle.unity` 的这批改动会被生成器写回去** —— `Editor/BattleScene.cs` 里现读仍写旧值：`:47761` 与 `:47800` `backgroundColor = new Color(0.055f,0.06f,0.08f)`、`:47745`/`:47799` `clearFlags = SolidColor`、`:47763`/`:47844` `cullingMask`、`:47797` `camGo.tag = "MainCamera"`（HUD 相机）、`:47796` `new GameObject("Main Camera")`，而且 `:47798` 会调 `LayoutSpace.Apply(cam)`（强制正交）。⇒ **任何一次 `BattleScene.BuildAndSaveScene` / `BattleScene.Run` 都会把本包对 `Battle.unity` 的改动整批覆盖掉**。根治必须改 `Editor/BattleScene.cs` + `Core/LayoutSpace.cs`（都不在本包白名单）。
2. 🔴 **13 场 arena 场景是 `.gitignore` 的生成产物**（`.gitignore:64` 忽略 `Unity/MyGame/Assets/WarpforgeArena1/Scenes/`）⇒ 本包对它们的 14-02/14-03/14-04/14-05 改动**只存在于盘上、不会进 git**，且重新生成即失效。`grep "StitchLightmapSeams\|BakeBackend" WarpforgeArena1/Editor/ArenaBuilder.cs` = **0 命中**（生成器从没写过这几个字段，是 Unity 保存时的默认值）⇒ 根治要写进 `ArenaBuilder`（W8 包）。
3. **`WarpforgeArena1/Editor/EffectIso.cs:126` 的层 30 依赖安全**：它 `const int IsolateLayer = 30`、只在**槽位为空**时才 `slot.stringValue = "WarpforgeIso"`（`:135-141`）⇒ 层 30 改成 `GroundCollider` 不影响它（按层号工作）；只有 `:140` 那句日志文案会过时（永远打不出来）。
4. **`Battle.unity` 的 `m_AmbientMode: 0`（Skybox）与 13 场 arena 的 `3`（Flat）不一致**（属 B133 R6b / 8-10，`C` 档，不在本包，未动）。因为运行期 `ArenaSceneState.Apply` 会把模式设成 Flat，本包改的 equator/ground 在任何一档下都**不参与**计算，故零观测差。
5. **`TapParticleController.cs:213` 取 `Camera.main`** —— tag 对调后它拿到的正是原版语义里的那台（BoardCamera）；这条**从「碰巧不对」变成「正好对上」**，属本批的正向副作用。
6. `BattleScene.cs` 里与 HUD 相机相关的断言（`CheckHudCameraStack`，`:50085-50095`）按 `depth` 选相机、只断 `renderType`/`cameraStack`/`cullingMask 不含 8、10` ⇒ 本批改动后**仍应通过**（未跑 Unity，未证实）。
7. `Shell.unity` 有 1 台 `AudioListener`、`MainMenu.unity` 0 台；壳进战斗是单场景加载（壳相机不在 `DontDestroyOnLoad` 层级里，`LayoutSpace.cs:151`）⇒ 本批新增的战斗 listener 不会与壳的重叠成 2 台（推断，未跑 Unity）。

---

## §五 机器可复跑

```bash
# 逐字段回读（只读）
python -I -X utf8 d:/4/_tmp_view/py/b139_y_verify.py
# 账补丁结构校验（只读）
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B139_Y
# 类型检查（本包没改 .cs，应为 0/0）
TMPDIR=/tmp/wf_y bash d:/4/Unity/工具/typecheck.sh
```
