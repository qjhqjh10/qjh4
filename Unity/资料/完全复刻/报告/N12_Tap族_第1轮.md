# N12 · Tap 族（点战场地面没有反应）第 1 轮 —— 取证报告

> 角色：查判据代理（只读为主）。产出判据文件 → `Unity/资料/完全复刻/判据/Tap族_地面点击反馈.md`
> **本轮没有派子代理**（取证链是一条直的 grep→读→解析，切块反而更慢）。写了两个文件，别的一处没碰。

---

## ① 结论（五问各一句）

1. **谁接收点击**：原版是**一个独立组件 `TapParticleController` 在自己的 `Update` 里**读
   `Input.GetMouseButtonDown(0)` → `Camera.main.ScreenPointToRay` → `Physics.RaycastNonAlloc(…, 3, 0x40000001)`
   → 取**最近**那条碰撞体。我们断在**两处物理事实**上：**战场里 0 个 Collider、代码里 0 处 `Physics.Raycast`**
   （边沿 / 屏幕坐标 / UI 闸 / 世界换算我们全有，点击最后落进 `DrivePlayerTurn` 的空分支）。
2. **`TapHitMaterialDefinition` 是什么**：4 个序列化字段 —— `particleDefinition{particles,audio}` +
   `delayTimeToPlayAgain` + `useTargetAnchor`（**死字段**）+ `targetAnchor`。**213 个实例**分布在 15 个 bundle，
   去重后只有 **25 种（粒子,音效）组合**；**19 个粒子 prefab 全部解得出真名**（全在 `battleprefabs_vfxandmisc`，
   名字全是 `Tap *`）、**11 个音效 cue 全部解得出**。⇒ **「数据解不出」不成立**。
3. **`TapParticleController.Update` 真实行为**：ilspy 版确实有 **87 条 `//IL_` 注记**，但
   **Ghidra 版 `TapParticleController__Update.c` 0 条注记、控制流完整** ⇒ 行为**已 100% 复原**（判据文件 §3.2 给了可直接照抄的伪码）。
   真正不确定的只剩 **3 处 B 档**，其中 2 处已按偏移判定、1 处只在移动端、桌面不走。
4. **什么条件下不播**：`matchType == Tutorial(100)` **整件关掉**（`Initialize` 里 `enabled = matchType != 100`）；
   另有 7 道运行时闸：阶段 ≠ `MatchProper(50)` / 有窗开着 / 指针在 UI 上 / 不是按下沿 / 定义冷却中 / 资源无效 / 最近那条没挂定义。
5. **我们的落点**：新建 `Battle/TapParticleController.cs` + `Battle/TapHitMaterialDefinition.cs`；
   数据加成 `manifest` 的 `tapHits[]` 段（生成器 = `工具/scripts快照/gen_unity_arena_manifest.py`，⚠️ 真身不在 `d:/2`）；
   建场在 `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`；音频走 `工具/import_original_sfx.py` +
   `WarpforgeVFX.WFSoundPlayer`。**19 个粒子 prefab 已经全在工程里**（`WarpforgeVFX/Prefabs/`），
   只需要补 **10 条 cue / 20 条 wav**。

**一句话给调度台**：上一批判「做不了」的两个理由**都不成立** —— 反编译有干净的 Ghidra 版、数据全解得出且粒子已经在工程里。
真正的工作量在**我们侧没有碰撞体**（要扩 manifest 生成器 + 建场器），不在取证。

---

## ② 判据（要点，全文见判据文件）

| 项 | 值 | 出处 |
|---|---|---|
| 触发边沿 | 按下沿 `Input.GetMouseButtonDown(0)` | `DEC/TapParticleController__Update.c:130` |
| 射线 | `Physics.RaycastNonAlloc(ray, hits, float.MaxValue, (int)collisionMask)` | `.c:162-163` |
| 掩码 | `1073744897 = 0x40000001`（13 场全同） | `ASSETS/…battlearena*/MonoBehaviour` 的 `collisionMask.m_Bits` |
| hits 上限 | **3** | `ILSPY/TapParticleController.cs:512` |
| 选哪条 | **距离最近**（不排序、**也不按组件过滤**）；最近那条没组件就 `return`，**不顺延** | `.c:175-321` |
| 预偏移 | `position + normal × 0.1f`，`LookRotation(normal)` | `.c:85-104`；常量 `0x1834b2dc4 = 0.1`（`read_literal.py` 直读） |
| 锚点 | `targetAnchor != null` ⇒ 挂到锚点、`localPos=zero`/`localRot=identity`（**与点击位置无关**） | `.c:106-132` |
| 粒子实例 | **按 prefab 全局缓存一件**，反复挪位置；`OnDestroy` 整表清空 | `…PlayParticle.c:42-64,145` |
| 模式闸 | `enabled = (matchType != 100)`；`MatchType.Tutorial = 100` | `…__Initialize.c`；`ILSPY/MatchType.cs` |
| 阶段闸 | `battleStage != 50` ⇒ 不做事；`BattleStage.MatchProper = 50` | `.c:75`；`ILSPY/BattleStage.cs` |
| 窗闸 | `WindowsManager.Instance.currentWindow != null` ⇒ return | `.c:76-83` |
| UI 闸 | `EventSystem.current.IsPointerOverGameObject(pointerId)` | `.c:129-136` |
| 冷却 | `delayTimeToPlayAgain`（0/0.1/0.2/0.5/1.0/**60** 六档）+ cue 自带 0.1s | 全 213 条统计；`ILSPY/AudioCue.cs` |
| 挂点总数 | **213**（13 场 166 + 共享 prefab 45 + vfx prefab 2） | 全库按 MonoScript PathID 搜 |
| 每场必有 1 颗棋盘地面 | `Default Tap Floor + Tap Hit General` @ `BattlePrefab/…/CardBoardLimits/CardLowBoardLimit` | UnityPy 读 13 个场景束 |

---

## ③ 证据（最强的几条）

1. **`.c` 是干净的**：`TapParticleController__{Update,Initialize,CheckCollision}.c` ·
   `TapHitMaterialDefinition__{Awake,Update,TryPlayParticles,OnDestroy}.c` ·
   `TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c` —— **逐个 `grep -c 'IL_'` = 0**。
   ⇒ 上一批「`Update` 被反编译打烂」只对 ilspy 成立；**本轮的伪码是从 Ghidra 指令流读的**。
2. **字段偏移三方吻合**：`.c` 里 `+0x20`(particleDefinition) / `+0x28`(delay) / `+0x30`(targetAnchor) /
   `+0x38`(remainingNotPlayTime) ↔ `ILSPY` 字段顺序 ↔ 资产 JSON 的字段名（`particleDefinition` / `delayTimeToPlayAgain` / …）。
3. **213 条 = 25 组**，且 **19+11 个资产名全部解出**（`_guid_resolve_probe.py` 走二进制 `catalog.bin`）
   —— 逐条表在判据文件 §2.2（含次数）。
4. **21 条 clip 名全部解出**（UnityPy 读真束；解包目录里没有 PathID↔名映射，这条是唯一路径）。
5. **工程里已有全部 19 个粒子 prefab**：`Assets/WarpforgeVFX/Prefabs/Tap *.prefab`（40 文件 = 20 件 × 2），
   且 `playOnAwake: 0` / `looping: 0`（三颗都核）。
6. **我们侧断点的直接证据**：`grep "Physics.Raycast"` 在运行时 `.cs` 的 4 条命中**全是注释**；
   `grep "Collider"` 的非 Editor 命中**全是注释**；`BattleDriver.cs:17515` 白纸黑字写着
   「原版 = `!IsPointerOverGameObject` + `Physics.RaycastNonAlloc == 0`；**我们 = 那张命中表**」。

---

## ④ 没查清（3 条，都不挡 PC 落地）

1. **移动端 `pointerId` 那两行的确切 Touch 属性**（`get_Count` / `get_sizeType` 两个误名 getter）。
   桌面 `isMobilePlatform == false` 恒走 `-1`，**不影响验收**。补法：按 RVA 反汇编再核。
2. **`WindowsManager.currentWindow` 覆盖哪些窗**（对我们 = 模态窗清单）。补法：单独读那个类，或用
   `BattleDriver.Update` 已有的模态分支当唯一判据（建议后者，避免第二份清单）。
3. **`07_场景` dump 的重复文件是否夹带旧 build 值** —— arena1 抽核 40 文件 = 20 条 × 2、值完全一致，
   但**没有全 13 场逐条比**。建议直接以 `assets_full` 为数据源，`07_场景` 只当层级来源。

---

## ⑤ 顺手发现（执行代理在现场才能看到的那些）

1. **上一批的「数据不够」是误判**：`WarpforgeVFX/Prefabs/` 里 **22 件 Tap 相关 prefab 早就在**
   （需要的 19 件全齐），还多了 `Tap Blue Glow` / `Tap Blue Glow Blink` / `Tap Green Glow` 三件没用上的。
2. **`Buff Mana 1` 这条 cue 已经在 `Resources/animfx_sounds.json`**（含 `Buff_Mana.wav`）——
   所以 arena3 那条「Tap Rock + Buff Mana 1」**不需要新增音频**。
3. **我们的 arena manifest 完全不含 Collider**（只有 `meshes` / `particles` 两段）；
   而生成器的源 `07_场景/<场>/` 里 `BoxCollider/` `MeshCollider/` **是齐的** ⇒ 缺口在**生成器没提取**，不是数据没有。
4. **两个死字段 + 三个死方法**：`useTargetAnchor`（从不读）、`instancedParticle`（从不读写）、
   `IsValid()` / `TryPlayParticles()` / `CheckCollision()`（全反编译零调用点）。
   ⇒ 施工时**照写字段**即可，⛔ 不要为它们发明逻辑。
5. **arena3 有 1 条定义挂在没有碰撞体的宿主上**（`Scenario/Battle Arena 3 Baked` 根，`Tap Rock + Buff Mana 1`）
   —— 原版**永远不会触发它**。⛔ 不要「顺手」给它补碰撞体。
6. **`WFSoundPlayer.Play` 没有 cue 自身的冷却**（原版 `AudioCue` 有 `lastTimePlayed`/`timeToPlayAgain = 0.1`）。
   Tap 这一层要么按定义各存一份 cue 实例（照原版），要么显式记账偏差 —— ⛔ 别当它不存在。
7. **`BattleEventSystem` 已经存在**（`Battle/BattleEventSystem.cs:121 Ensure()`），
   UI 闸有现成等价物（`BattleDriver.PointerOverUi()`，`:8081`），**不需要另造**。
8. **反编译口径要更正**：本族 `.c` 0 条注记，别再拿「ilspy 87 条 IL 注记」当「做不了」的理由。

---

## ⑥ 建议怎么派下一批

**5 件，按依赖顺序；T1+T2 可并行（文件不相交）。**

| 顺序 | 件 | 改哪个文件 | 依赖 | 说明 |
|---|---|---|---|---|
| **T2**（可先做，独立） | 音频：10 条 cue + 20 条 clip 导入 | `工具/import_original_sfx.py` · `Resources/Art/audio/sfx/` · `Resources/animfx_sounds.json` | 无 | 最短路径、判据最全（判据文件 §2.4 表可直接抄）；`_sfx_meta.py` 负责 `.meta` |
| **T1** | 组件 + **棋盘那一颗**（`CardLowBoardLimit`） | 新建 `Battle/TapParticleController.cs` · `Battle/TapHitMaterialDefinition.cs` · `Editor/ArenaBuilder.cs` | 读 `WFSoundPlayer`（T2 不阻塞：没 cue 就不响并出声） | 最小可验收：真 Play 点棋盘有粒子 |
| **T3** | manifest 扩 `tapHits[]` + 13 场建碰撞体 | `工具/scripts快照/gen_unity_arena_manifest.py` · `Editor/ArenaBuilder.cs` · 13 份 manifest · 13 件 prefab | T1 | **逐场做，⛔ 不许 `--all` 一把梭**（铁律 15）；每场核实例数（20/27/14/13/27/15/6/1/9/11/11/1/11 + 每场 1 颗棋盘） |
| **T4** | 4 道闸（教程局 / 阶段 / 模态窗 / UI） | `BattleDriver.cs`（只暴露**一处**只读判据）+ T1 组件 | T1 | ⛔ 不在 Tap 组件里另列窗表 |
| **T5** | 18 条锚点 | manifest `anchor` + `ArenaBuilder` | T3 | 黑军团 7 / arena2 1 / arena3 2 / 共享 8 |

**不要派**：任何「按世界坐标算区域」的分析式替身（与原版语义不同）· 任何为死字段/死方法补的逻辑 ·
任何「每次 Instantiate 再销毁」的粒子实现（原版是按 prefab 缓存复用）。

**自检建议**：T1/T3 完成后 `bash 工具/typecheck.sh`；真正验收走**真 Play**（铁律 12 已恢复：起 player → 点棋盘 → 看粒子/听音）。

---

### 产出路径

- 判据：`D:/4/Unity/资料/完全复刻/判据/Tap族_地面点击反馈.md`
- 报告：`D:/4/Unity/资料/完全复刻/报告/N12_Tap族_第1轮.md`
