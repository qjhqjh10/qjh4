# 战场线 · 用素材重新布置（T00）· 2026-10-23

> 用户 2026-10-22 指示：「**用素材重新布置**」。
> 本文只记**做了什么 / 验了什么 / 还欠什么**；判据正本仍是
> `资料/普查产出_1022/J-A_战场素材与材质诊断.md`。

---

## 〇、一句话

**原版战场里会动的 9 处布景动画 + 6 个机械 controller 已经接回我们实例化出来的战场树**，
`ArenaAnimsProbe.Run` 端到端 **10/10 场通过、0 没接上**（2026-10-23 实测）。

---

## 一、根因与做法

J-A §1·4 定性的「素材没达到预期」最大一块 = **A1–A3**：
26 条 clip 里只有相机 intro 有数据 · 5 个 `AnimatorController` 一个都没进工程 ·
49 个 `Animation` 组件是**空的**（`ArenaBuilder` 只 `AddComponent<Animation>()`，`clip` 一个都没赋）。

本轮把这条链补上：

| # | 件 | 做法 | 产物 |
|---|---|---|---|
| 1 | **9 条 legacy「场景动画」clip 进包** | `ARENA_ANIM_GROUPS` 加 6 组（astra / blacklegion / darkangels / emperorschildren / genestealers / tau）＋ `Battle Arena 3 Animations` 并进 `_shared` | `StreamingAssets/WarpforgeVFX/wf_arena_anims_*.bundle`（共 **9 只**） |
| 2 | **节点 → clip/controller 映射** | 新工具：读组件本体（UnityPy）+ **按包**解析 PPtr（本包优先，再查 shared） | `数据/游戏数据/arena_scene_anims.json` + 运行期副本 `CardPresentation/Resources/arena_scene_anims.json` |
| 3 | **运行时接线** | 新 `CardPresentation/Battle/ArenaSceneAnims.cs`；`ArenaRuntimeLoader.Load()` 实例化后调用 `Bind()` | `Animation` ×9（9 clip）· `Animator` ×6（6 controller） |
| 4 | **取件口扩到多包** | `WarpforgeAnimatorBridge` 加 `ExtraBundleRelPaths`（9 只小包）+ `FromAll` / `FromAllClip`；**主包优先、老调用点零影响** | `WarpforgeVFX/Runtime/WarpforgeAnimatorBridge.cs` |
| 5 | **端到端探针** | `ArenaAnimsProbe.Run` 加第 ③ 段：逐场实例化 prefab → `Bind()` → 比「接上数 vs 表里条数」 | `WarpforgeArena1/Editor/ArenaAnimsProbe.cs` |

**为什么 legacy 也走原件、不走「JSON 重建曲线」**：这 9 条里有三种**非 Transform 绑定** ——
`GameObject.m_IsActive`（Railgun 显隐）· `MonoBehaviour.m_Enabled` · `SkinnedMeshRenderer.blendShape.*`
（Black Legion 的变形）。`SetCurve` 重建要逐类对齐，而原件就在包里 ⇒ **取原件最省且最忠实**。
（相机 intro 仍走 `CameraIntro.cs` 的 JSON 重建 —— **⛔ 别接第二份**。）

---

## 二、验过的（判据 = 日志行）

```
AAP === 19 取到 / 0 没取到 ===
AAP === 场景绑定：10 过 / 0 红（10 场）===
```

14 条 clip（5 muscle + 9 legacy）+ 5 个 controller 全部取到；10 场（有动画的场）逐场
`Animation n/n` · `Animator n/n` · `没接上 0`。日志：`_tmp_view/w66_arenaprobe2.log`。

| 场 | 接上的 |
|---|---|
| battlearena1 | Animator 2（`Cannon 1` ×2） |
| battlearena2 | Animator 3（泵 / 竞技场 2 / 动态骷髅） |
| battlearena3 | Animation 1（`Battle Arena 3 Animations`） |
| astramilitarum | Animation 1（`Astra Militarum Scene Animations`） |
| blacklegion | Animation 1（`Black Legion Scene Animation`，含 blendShape） |
| darkangels | Animation 1（`Dark Angels Scene animations`） |
| emperorschildren | Animation 1（`Emperor's Children Scene Animations`） |
| genestealers | Animation 1（`Genestealers Scene Animations`） |
| leviathan | Animator 1（`Battle Arena Leviathan`） |
| tauviorla | Animation 3（Railgun BIG / Turret 1 / Turret 2） |
| aeldari / sororitas / spacewolves | 原版就没有场景侧动画 ⇒ 0（不是缺口） |

**对得上 J-A 的 49**：J-A §1·4 A3 数到「13 场 `Animation/` 目录 3/3/4/3/4/4/5/4/4/3/3/3/6 = **49**」。
本轮普查：**跳过 39**（每场 3 个：`BattleHud` / `Cinemachine Vcam` / `Card Display Window`，各有系统）
＋ **本表接线 9** ＋ **原版零 clip 1**（`darkangels/Scenario/Directional Light`）= **49** ⇒ **一条不差**。
⇒ 「49 个 Animation 组件一个 clip 都没赋」这个洞**已经全部分因、没有剩余**。

---

## 三、逐条判据（现读）

- **每场各自的 clip**：`assets_full/bundle_scenes_scenes_<场>/AnimationClip/` 里逐个读
  `m_Name`；`Animation.m_Animations` 的 PPtr **pid 是包内局部的**（各场都有 17/18/19/20）
  ⇒ **⛔ 不许拿全库 pid 字典查**（会静默取错包；本轮第一版探针就踩了这个，已改）。
- **`Leviathan Bunker Door Parent` 的 `Animator` 没有 controller**（`m_Controller` pid = 0）——
  原版自己就是空的 ⇒ 不是缺口，`gen_arena_scene_anims.py` 跳过。
- **`BattlePrefab/BattleHud` / `Cinemachine Vcam` / `Card Display Window` 已各有系统**
  （`BattleHudAnim` / `CameraIntro` / 战斗线 T04）⇒ 生成器按路径跳过，共 39 条，逐条列在工具输出里。

---

## 四、还欠什么（⛔ 不是「不做」，只是排在后面 —— 铁律 11）

1. **真机看动没动**：批处理只验了「组件接上、clip/controller 取到」，
   **没验「画面里真的在动」**。要起 player 拍两张间隔 ≥2 s 的图比像素差
   （建议 `battlearena2`：泵 + 骷髅最显眼）。→ 记入 `真Play待验清单`。
2. **15 个原版场景 prefab 的导出**（J-A 路线③ 第 4 批）—— 本轮**没做**。
   它是「整树替换 / 离线备用」的前置，**与当前视觉效果无直接关系**
   （真根因是本轮接的 A1–A3）。仍留在 T00 的欠账里，顺序排在 T01–T08 之后。
   ⚠️ 真要做时必须先 pilot 跨 CAB 容器引用（J-A §2·6 R1）。
3. **`animator_controllers.json` 的 `Cannon 1` 数据错**（跨包同 pid 覆盖）——
   属引擎线本轮在修（`gen_animator_controllers.py:350`）；本轮的运行时取件**不读那张表**，
   所以两者互不影响。

---

## 五、顺手发现（只报，未改）

1. `battlearena1` 的炮台节点在原版是 **`Animator`**（不是 `Animation`）——
   我们之前连 `Animator` 组件都不建 ⇒ 这也是 J-A A2 说「一帧都不动」的直接原因；
   本轮由 `ArenaSceneAnims` 统一补。
2. `Battle Arena Astra Militarum Baked` 等 6 个节点名**不带 `Scenario/` 前缀**（根级），
   而 12 个 `Scenario` 根同名 ⇒ 路径必须**逐字**用生成器给的那条，⛔ 别按叶名找。
3. `Directional Light` 上原版有一个**零 clip 的 `Animation`**（`m_Animations` 空）——
   生成器跳过它；我们的 prefab 里那份空组件保持原样即可。
