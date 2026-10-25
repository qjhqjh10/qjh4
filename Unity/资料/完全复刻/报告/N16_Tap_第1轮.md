# N16 · Tap 族（点战场地面没有反应）第 1 轮 —— 执行报告

> 代理：`N16 · 执行` · 日期 2026-10-25 · 判据正本 = `Unity/资料/完全复刻/判据/Tap族_地面点击反馈.md`
> （**已按订正版**执行：播放行为体 = `TapHitMaterialDefinition.ParticleCollisionDefinition.PlayParticle`，
> 149 行 `.c` 亲读；`CheckCollision` 归属 `TapParticleController`、零调用）。
> ⛔ 本批**没跑 Unity**（任何 `-executeMethod` / 批处理）；只跑了 python 生成器 / 导入器 + 秒级类型检查。

---

## ① 结论

| 件 | 状态 | 一句话 |
|---|---|---|
| **T1 · 组件** | ✅ 代码写完、类型检查 0/0 | 新建 `Battle/TapParticleController.cs` + `Battle/TapHitMaterialDefinition.cs`；**八道闸全做**，组件关掉时**一帧都不耗** |
| **T2 · 音频** | ✅ 真导入完 | 10 条 cue 进表（461→**471**）、**21 条 clip** 拷进 `Resources/Art/audio/sfx/` 并补 21 份 `.meta` |
| **manifest** | ✅ 生成器改完，13 场逐场重跑 | 新增 `tapHits[]`，**条数与 `assets_full` 当前 build 逐场相等**（20/27/14/13/27/15/6/1/9/11/11/1/11 = **166**） |
| **建场侧** | ✅ 代码写完、类型检查 0/0 | `ArenaBuilder.BuildTapHits()`：建同型碰撞体 + 挂定义 + 建控制器 + **产物自检** |
| **Unity 建场** | ⏸ **留给调度台** | 命令清单见 §⑥（**一场一场**跑，⛔ 不许 `--all`） |

**一句最该先知道的事（会直接影响验收口径）**：
> **原版点「棋盘空地」其实也不触发。** 每场那颗 `CardLowBoardLimit` 在原版是 **layer 11（`BoardCardLimit`）**，
> 而射线掩码是 `0x40000001`（= layer 0 + layer 30）⇒ **它不在掩码里**。判据 §2.2 那句
> 「玩家点棋盘空地最常撞到的就是它」**与硬数据冲突**（见 §⑤-1，附复现命令）。
> ⇒ 真机上**能看见反馈的是那些道具**（木桶 / 栅栏 / 发电机 / 隐形 `Collision` 盒），不是棋盘。
> 另外 `battlearenaemperorschildren` / `battlearenaspacewolves` 两场**全部 tapHits 都不在掩码内**（各只有棋盘那 1 条）
> ⇒ 这两场**原版也不会有任何地面点击反馈**（照原版＝我们也没有）。

---

## ② 判据与证据（关键几条，全部亲读）

| 结论 | 证据 |
|---|---|
| 触发方式 = **独立组件自己读按下沿**，不是 uGUI / 不是 `OnMouseDown` | `DEC/TapParticleController__Update.c`（385 行、**0 条 `//IL_`**）：`Input__GetMouseButtonDown(0)` → `Camera__ScreenPointToRay` → `Physics__RaycastNonAlloc(ray, hits, DAT_1834b2d94, mask, 0)` |
| 掩码 / hits 上限 / 预偏移 | 掩码 = `*(int*)(param_1+0x28)` 经 `LayerMask→int`；资产 `collisionMask.m_Bits=1073744897`（13 场同值）；hits 数组在 `+0x30`（构造函数 `new RaycastHit[3]`）；`0x1834b2dc4 = 0.1f` |
| 最近优先且**不按组件过滤** | `.c` 循环只比 `hit+0x20`（`RaycastHit.distance`）取最小；随后 `GetComponent<TapHitMaterialDefinition>()` **null 就 `return`**（不顺延） |
| 播放行为体 = `PlayParticle`（149 行） | `DEC/TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c`：①`particles`/`audio` 任一无效 ⇒ 整条 return ②**按 prefab 全局缓存一件、反复挪** ③`parent==null` ⇒ `position + normal×0.1` + `LookRotation(normal)`；非空 ⇒ `SetParent` + 本地零位 ④`Play()` ⑤音效在**命中点**（`AudioCue__Play3D(position, 0)`） |
| 冷却 = 定义自带 + cue 自带 | `TapHitMaterialDefinition__Update.c`（`remainingNotPlayTime -= dt; ≤0 ⇒ enabled=false`）+ `AudioCue__CanPlay.c`（`Time.time - last < timeToPlayAgain && last != 0`）——**`lastTimePlayed` 存在 cue 资产上**⇒ 本实现按 cue 做静态冷却，⛔ 没改 `WFSoundPlayer` |
| 死字段两个 | `useTargetAnchor`（全量 ilspy 只有声明）、`instancedParticle`（`PlayParticle.c` 里 `param_1+0x20` 零出现）⇒ **都没补逻辑**，`useTargetAnchor` 只照抄序列化值 |
| 教程局整件关 | `DEC/TapParticleController__Initialize.c`：`set_enabled(iVar1 != 100)`；`MatchType.Tutorial = 100` |
| 3D 碰撞体**四种**都要（不止 Box/Mesh） | 13 场实测（按 dump 文件数，含 `X.json`+`X_X.json` 重复）：`BoxCollider` 114 · `MeshCollider` 206（其中 **convex 154**；**去重后 103 条 / convex 77**）· `SphereCollider` 8 · `CapsuleCollider` 2 |
| **layer 11 不在掩码** | 原版 `globalgamemanagers/TagManager/TagManager_3.json` 的 `layers[]`：0=`Default` · 11=`BoardCardLimit` · 12=`ParticleTapCollider` · 30=`GroundCollider`；掩码 `0x40000001` = bit0 + bit30 |
| 每场只有 1 颗控制器 | `assets_full` 里 13 个场景各 1 件（判据 §1.1）；本实现由 `ArenaBuilder` 每场建 1 颗 |
| 无碰撞体那条（arena3 场根） | `07_场景/battlearena3` 的 `Battle Arena 3 Baked` 有定义但**没有 3D 碰撞体** ⇒ 原版永不触发；清单里 `collider=null`，建场侧**跳过** |
| 共享 prefab 的 45 条**不是缺口** | `battlesharedresources` 45 条是**prefab 资产自己的对象**；每场场景里有各自的副本（例：leviathan 场景自带 `Bunker_wall` / `Prop_front_3` / `Toxic_pool_wall`）⇒ 逐场抽取已覆盖 |

---

## ③ 改动清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Battle/TapParticleController.cs` | **新建** | 独立 `MonoBehaviour`；`CollisionMask=0x40000001`、`MaxHits=3`、`NormalOffset=0.1f`；八道闸；`debugCollisions` 与原版同文案；`Initialize()` 公开（教程局关）；拿不到 `BattleDriver` 10 s 后出声并永久关 |
| `MyGame/Assets/CardPresentation/Battle/TapHitMaterialDefinition.cs` | **新建** | 原版字段名逐字（`particleDefinition{particles,audio}` / `delayTimeToPlayAgain` / `useTargetAnchor` / `targetAnchor` / `remainingNotPlayTime`）；`PlayParticle` 照 149 行；粒子按 prefab 缓存；cue 冷却按 cue 静态；`Awake` 关自己、`Update` 减冷却 |
| `Unity/工具/scripts快照/gen_unity_arena_manifest.py` | 改 | 新增 `TAP_SCRIPT_PID` / `TAP_ASSET_NAMES`（30 个 GUID→名）/ `TAP_HITS_EXPECT`；`read_tap_hits()` 读 def + 四种 3D 碰撞体（含 `MeshCollider.m_Mesh` 名字）+ 锚点 + 世界 TRS；manifest 新增 `tapHits[]`；控制台新增读数与告警。**顺手修了一个既有缺口**：粒子 `matShader` 在 `shader_info()` 解不出时，按材质 `m_Shader` 的 PathID 到 `assets_full/*/Shader/Shader_<pid>.json` 兜底（**只填空值**，见 §⑤-8） |
| `13 份 arenas/*_manifest.json` | 重跑生成器 | 每份只**新增** `tapHits[]` 段（无删改）；13 场全部用**最终版生成器**跑过 |
| `Unity/工具/import_original_sfx.py` | 改（+18/−1） | `EXTRA_CUES` 追加**10 条 Tap cue**（带本轮亲核的判据注释） |
| `MyGame/Assets/CardPresentation/Resources/animfx_sounds.json` | 重跑导入器 | 461 → **471** 条 cue（只增 10 条） |
| `MyGame/Assets/CardPresentation/Resources/Art/audio/sfx/*.wav(+.meta)` | **新增 21 + 21** | `Hit Rock 1-3` / `Hit Fire 1-3` / `Hit Floor Generic 1-2` / `Hit Metal Light 1-2` / `Hit Heavy Metal 1-2` / `Hit Spark 1-3` / `Hit Squishy 1-2` / `Hit Water 1-2` / `Hit Monolith` / `Squig Growl 1`（`Resources/Art/` 已被 `.gitignore` 排除） |
| `MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` | 改（+379/−0，**CRLF 未翻**） | ①`Manifest.tapHits` + 三个序列化类 ②`BuildTapHits()`（建同型碰撞体 / 挂定义 / 建控制器）③`EnsureMeshReadable()`（convex MeshCollider 必需）④`VerifyPrefabTapHits()` 产物自检（数 prefab 文本里的脚本 guid） |

---

## ④ 核验

- **`TMPDIR=/tmp/wf_n16 bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0**（两端各跑过多次；
  中途两次撞上别的代理正在写的 `Shell/ChatPanel.cs` / `Shell/AllianceMemberTab.cs` 报错，**不是本批文件**，收尾那次已 0/0）。
- **行尾**：`ArenaBuilder.cs` 改完仍 **CRLF 5630 / LF 5630**（`git diff --numstat` = 379/0，不是整篇翻）。
- **音频**：`import_original_sfx.py` 报「解析成功 **471/471** 个 cue；已拷 **21** 条 · 补 meta **21** 条」；
  逐条复核 10 条 Tap cue 的 `clips / pitch / volume / timeToPlayAgain`，与判据 §2.4 那张表**逐格相等**
  （本轮是**从 bundle 的 `clipList` 现读**，不是抄文档）。
- **manifest `tapHits[]`**（13 场，逐场核过两遍：① 生成器日志 ② 离线探针 `_tmp_view/n16/probe_tap.py`）：

| 场 | tapHits | 掩码内 | 无碰撞体 | 锚点 | hasMesh |
|---|---|---|---|---|---|
| battlearena1 | 20 | 19 | 0 | 0 | 7 |
| battlearena2 | 27 | 26 | 0 | 1 | 6 |
| battlearena3 | 14 | 13 | **1** | 3 | 10 |
| battlearenaaeldari | 13 | 12 | 0 | 0 | 9 |
| battlearenaastramilitarum | 27 | 26 | 0 | 0 | 26 |
| battlearenablacklegion | 15 | 14 | 0 | 7 | 10 |
| battlearenadarkangels | 6 | 5 | 0 | 0 | 5 |
| battlearenaemperorschildren | 1 | **0** | 0 | 0 | 0 |
| battlearenagenestealers | 9 | 8 | 0 | 0 | 8 |
| battlearenaleviathan | 11 | 10 | 0 | 0 | 9 |
| battlearenasororitas | 11 | 10 | 0 | 2 | 10 |
| battlearenaspacewolves | 1 | **0** | 0 | 0 | 0 |
| battlearenatauviorla | 11 | 10 | 0 | 0 | 9 |
| **合计** | **166** | 153 | 1 | 13 | 109 |

  —— 166 与 `assets_full` 当前 build 的逐场条数**逐场相等**（判据 §2.2 那张表的 13 行）；
  166 + 45（shared prefab）+ 2（prefab 资产）= **213** ✓。

- **判据 §2.2 / §0·b 里那几项「未抽核」的，本批顺手全核了一遍（都成立）**：
  · 总数 **213** · `targetAnchor` 非空 **18**（= 逐场 13 + shared prefab 5）·
    `delayTimeToPlayAgain` 六档 **191 / 2 / 12 / 4 / 3 / 1**（0 / 0.1 / 0.2 / 0.5 / 1.0 / 60.0）·
    **25 种（粒子,音效）组合、每一种的次数也逐行相等**（56/34/23/20/14/10/7/7/5/5/4/4/3/3/2×6/1×4）；
    30 个 GUID **30/30 全部解析出名字**。
  · 复现：用生成器里的 `TAP_ASSET_NAMES` 扫 `assets_full` 的 213 份 def（本报告的口径就是把那张表当输入）。
- **§0·b 两处「建议自己确认」的，本批确认了，其中 B-3 的**理由**要订正**（见 §⑤-1）。

---

## ⑤ 没查清 / 已知偏差

1. 🔴 **判据 §2.2 的「棋盘地面最常撞到」与硬数据冲突**（本批发现的**判据错误**，不改判据文件，报给调度台）：
   `CardLowBoardLimit` 在原版是 **layer 11 = `BoardCardLimit`**，掩码 `0x40000001` **不含 bit11**。
   复现：`python -I -X utf8 工具/_guid_resolve_probe.py g …` 无关；直接读
   `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/GameObject/CardLowBoardLimit.json` 的 `m_Layer`（=11）
   + `globalgamemanagers/TagManager/TagManager_3.json` 的 `layers[11]`。**我没有改层**（照原版设 11）。
2. **判据 §3.4 B-3 的「理由」要订正（结论不变）**：原判据说选最近那条用的是「`hit + 0x20` 的 float」。
   实际 `0x20` 是 **il2cpp 托管数组的数据起点**（数组对象头 0x20，长度在 `+0x18`）——
   `lVar10 + 0x20 + i*0x2c` 就是**第 i 个元素的首地址**，`0x2c = 44` 是 `RaycastHit` 的步长。
   那个被误名的 getter（`MinMaxCurve.get_constantMax`）实际是 `RaycastHit.get_distance`，
   由它自己的方法体去读结构内的字段。
   **结论仍成立**（取最小 `distance`）：① 同一指针也喂给 `RaycastHit__get_collider`（同一元素）；
   ② 调试日志的格式串里那一格就是 `Distance {d}`；③ 选完之后的 `Bounds.get_center/extents`
   拿的是**元素内存的 0..11 / 12..23**（= point / normal，与 B-2 一致）。
   复现：`sed -n '265,290p' D:/2/tools/decomp_full/TapParticleController__Update.c`。
3. **5 条锚点解析不到**（会退回「世界点 + 法线」，属**偏差**，见判据 T5）：
   `battlearena2` 的 `Squig Anchor`；`battlearenablacklegion` 的 `Grill Anchor` / `Grill anchor` / `GameObject`×2。
   原因：那几颗锚点节点**我们根本没建出来**（不在 `meshes[]`、也不在 `<场>_groups.json` 的 `nodes[]` 里）。
   能接上的 **7 条**（arena3 `Tap Particles Anchor`×2、blacklegion `Firepit Anchor`/`(1)`/`Ornament Anchor`、
   sororitas `GameObject`×2）走的是 `ApplyGroupNodes` 建出来的节点；
   13 条锚点里有 **1 条**属于 arena3 那条**没有碰撞体、永远不触发**的定义 ⇒ 实际参与建场的锚点 **12 条 = 7 接上 + 5 失败**。
   **T5 要补的是生成器/旁挂那一侧**（把 `Grill Anchor` / `Grill anchor` / 两个匿名 `GameObject` / `Squig Anchor` 也建出来）。
4. ~~`battlearena1/2/3` 的 manifest 缺 `collider.mesh`~~ → **已补**：13 场全部用**最终版生成器**重跑过
   （arena4–13 由后台串行循环跑、arena1/2/3 另补跑），`MeshCollider` 条目 **100% 带网格名**
   （全 13 场逐条核过，缺失 0 条）。
5. **convex MeshCollider 与网格 Read/Write**：我们的 `.obj.meta` 全是 `isReadable: 0`，
   而去重后 **77/103** 条 MeshCollider 是 convex。建场侧已加 `EnsureMeshReadable()`（只对真用到的那几颗开、并重导）。
   ⚠️ **这条只有跑一次 Unity 建场才知道实际效果**（会多出一批 `.obj.meta` 的 `isReadable: 1` 改动）。
6. **3D 音效的衰减参数没核**：原版 `SoundManager.Play` 里 `AudioSource` 的 `minDistance/maxDistance/rolloff` 未读；
   我们走项目既有 `WFSoundPlayer`（全项目同一套）。属「没查到」，不是「原版没有」。
7. 顺手发现（**不改，只报**）：`import_original_sfx.py` 的反向查漏报出 **4 个候选**，都不是本批引入的：
   `Cardmove`（`Editor/SoundCueAssertT.cs:77`）· `Asteroid Zone`（`WarpforgeArena1/Editor/AnimFXCheck.cs:2115`）·
   `Draft Mode Loss Jingle`（`Shell/DraftModeWindow.cs:2645`）· `Dawn Attack`（`AnimFXCheck.cs:2132`）。
   要么补 `EXTRA_CUES`、要么写进 `CODE_ONLY_KNOWN` —— 归音效线/壳线定夺。
8. **判据 §5.3 的 T1 验收条件也受第 1 条影响**：它写「真 Play：**点棋盘空地**有粒子 + 有音」——
   按原版层设置这条**达不到**（见 §①）。建议把 T1 的验收改成「**点木桶 / 栅栏 / 发电机**那一带的地面」。
9. **顺手修的一个既有缺口（不是 Tap 的活，但不修会在我这一轮造成退化）**：粒子 `matShader` 那一格。
   `shader_info()` 只在**场景包自己的 CAB** 里找 shader，而有一批 shader 在
   `bundle_Warpforge_unitybuiltinassets` 里 —— 例 `battlearenaemperorschildren` 的 `bubble_light`
   → `Shader_2855819893914472268.json` = `Mobile/Particles/Additive`。
   不补的话我这轮重跑会把那 **3 个 `Bubbles` 的 `matShader` 从正确的名字变成 `null`**（旧清单里本来是对的）。
   已在生成器里补一条**只填空值**的兜底（按材质 `m_Shader` 的 PathID 到 `assets_full/*/Shader/` 取
   `m_ParsedForm.m_Name`），并接进控制台日志（`[粒子 matShader 走了 assets_full 兜底]`）。
   复现/判据：`python -I -X utf8 -c "import json;print(json.load(open('d:/2/新解包资源/assets_full/bundle_Warpforge_unitybuiltinassets/Shader/Shader_2855819893914472268.json',encoding='utf-8'))['m_ParsedForm']['m_Name'])"`。
10. **三份旧清单顺带被 generator 追上（与 Tap 无关，值更正）**：`sororitas` / `spacewolves` / `emperorschildren`
   这三份上次重跑得早，本轮重跑后 `queue: -1` 变成真值（`matQueue` 45+28 条、`subMats.queue` 59+49 条 →
   `2000/3000`），另有 JSON 数组换行格式差异。判据 = `CLAUDE.md` §15 的 2026-10-12 就地订正那条：
   `ApplyRenderState` 是 `if (queue > 0) mat.renderQueue = queue;`，`-1 → 3000` **对画面没有影响**、只是那一列更真。
   ⇒ 这三份的 diff **不是纯新增**（`git diff --numstat` = 46/0（已靠兜底补回）、1089/202、464/139），其余 10 份是纯新增。
11. **模态窗闸用的是现成的公开口 `BattleDriver.PointerOverBlockingUi()`**（= 设置/聊天/换牌/选牌/多卡/放大/联盟/日志 八扇），
   **本批没有改 `BattleDriver`**（文件不归我）。与判据 §5.2-4 建议的 `TapBlocked` 相比：
   · **终局**由第 ② 道闸（`Ctx.IsOver`）覆盖 ✓；· **回放条 / 攻击目标选择器（`selector`）不在那个口里** ——
   它们只能靠第 ④ 道闸（`IsPointerOverGameObject`，即点在它们身上时）挡住；
   原版 `WindowsManager` 到底把哪些算「窗」**仍未解**（判据 §6 第 2 条）⇒ 这一格属「没查到」，不是「已对齐」。
   若以后要严格照原版，就得在 `BattleDriver` 里补一个 `TapBlocked`（把回放条/选择器一起算进去）。

---

## ⑥ 给调度台的执行命令清单（⚠️ 一场一场跑，⛔ 不许 `--all`）

前置：**已全部做完**（13 份 manifest 已用最终版生成器重跑、T2 音频已导入）—— 你只需要跑 Unity。

然后**逐场**重建 prefab（战斗实际用的是 `Resources/ArenaPrefabs/<场>.prefab`，**这一条是必跑项**）。
**⛔ 别写 `for` 循环 / 别用 `--all`** —— 铁律 15 / 13·3：Unity 同一工程只能一个实例，**逐条串行执行**，每条：

```bash
unset ELECTRON_RUN_AS_NODE
WF_ARENA=<场> "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod ArenaBuilder.BuildPrefabFromCLI -logFile -
```

13 条（按这个顺序，每场一条）：
`battlearena1` → `battlearena2` → `battlearena3` → `battlearenaaeldari` → `battlearenaastramilitarum` →
`battlearenablacklegion` → `battlearenadarkangels` → `battlearenaemperorschildren` →
`battlearenagenestealers` → `battlearenaleviathan` → `battlearenasororitas` → `battlearenaspacewolves` →
`battlearenatauviorla`

**每场日志里要看的三行**（找不到就说明没进盘 ⇒ 别当它生效）：
1. `[Arena] Tap（点地面反馈）：<场> 清单 N 条 ⇒ 建碰撞体 … · 挂定义 … · 控制器 1 颗 · **掩码外…**`
2. `[Arena] Tap 自检：<场> 建场时挂定义 X 个 / 控制器 1 颗 vs prefab 产物里 … —— **相等 ✅**`
3. 若有 `Read/Write Enabled` 的行 ⇒ 说明那几颗 OBJ 的 `.meta` 会被改（**预期**）。

**期望读数**（= 用清单 + `<场>_groups.json` 离线算出来的；建场日志应逐格对上）：
「建碰撞体」= 有碰撞体的条数（arena3 那条没有碰撞体、照原版跳过）；
「另建空宿主」是**上界**（`hasMesh=false` 的条数 —— 若某个名字恰好能对上已建的非网格节点会少建一颗，日志会打实际值）。

| 场 | tapHits | 建碰撞体/挂定义 | 另建空宿主（上界） | 掩码外 | 锚点接上 / 失败 |
|---|---|---|---|---|---|
| battlearena1 | 20 | 20 | 13 | 1 | 0 / 0 |
| battlearena2 | 27 | 27 | 21 | 1 | 0 / **1** |
| battlearena3 | 14 | 13 | 3 | 1 | 1 / 1 |
| battlearenaaeldari | 13 | 13 | 4 | 1 | 0 / 0 |
| battlearenaastramilitarum | 27 | 27 | 1 | 1 | 0 / 0 |
| battlearenablacklegion | 15 | 15 | 5 | 1 | 3 / **4** |
| battlearenadarkangels | 6 | 6 | 1 | 1 | 0 / 0 |
| battlearenaemperorschildren | 1 | 1 | 1 | 1 | 0 / 0 |
| battlearenagenestealers | 9 | 9 | 1 | 1 | 0 / 0 |
| battlearenaleviathan | 11 | 11 | 2 | 1 | 0 / 0 |
| battlearenasororitas | 11 | 11 | 1 | 1 | 2 / 0 |
| battlearenaspacewolves | 1 | 1 | 1 | 1 | 0 / 0 |
| battlearenatauviorla | 11 | 11 | 2 | 1 | 0 / 0 |
| **合计** | **166** | **165** | **56** | **13** | **7 / 5** |

⚠️ 「掩码外 = 1」在**每一场**都是那颗 `CardLowBoardLimit`（layer 11）—— 见 §① / §⑤-1。
⚠️ 锚点那 5 条失败会打 `退回「世界点 + 法线 × 0.1」` 的 `LogWarning`（**这是偏差**，见 §⑤-2）。

> 🔴 **2026-10-25 更正（Q2 · `报告/Q2_建场清单与锚点_第2轮.md`）**：
> ① 上表 `battlearena3` 的「锚点接上 / 失败」**原来是 `2 / 0`，改成 `1 / 1`** —— 原值错在**没把 `ScaleClose` 的符号口径算进去**：
> `Monolith Front Right/Tap Particles Anchor` 的 `localScale = -0.025694`、父 44.0796 ⇒ Unity `lossyScale = (-1.1326, …)`，
> 而清单侧 `world_scale()` 给的是**模长**（恒 ≥0）`+1.1326` ⇒ 逐轴相减**结构性必假**，那颗锚点被判「找不到」。
> 该条已在 `ArenaBuilder.ScaleClose` 里修掉（两侧取绝对值），修完这一格**应当变成 `2 / 0`**（= 上表原值，但理由是修了 bug，不是算错了）。
> ② 下表那 **5 条失败**（arena2 1 + blacklegion 4）已改成 **`BuildTapHits` 找不到就按清单世界 TRS 自建空节点**（铁律 11）⇒
> **建场日志里不会再出现「锚点 `X` 找不到」的 `LogWarning`**，取而代之是一条「**按清单世界 TRS 自建**」的 `Debug.Log`；
> 汇总行的锚点段变成「接上 N / 失败 0（本场带锚点 N 条，**其中自建 K 条**）」。逐字预期见 `Q2_…_第2轮.md` §④·2（追加轮已更新）。
> ⇒ **上表「合计 7 / 5」与 arena2 / blacklegion 那两格是修复前的口径**，以 Q2 报告为准。

可选（只看画面，不影响战斗）：`-executeMethod ArenaBuilder.BuildFromCLI`（单场场景）/
`ArenaBuilder.RenderPreviewFromCLI`（单场渲图）。

真机验收提示（**别等错东西**）：
· 点**木桶 / 栅栏 / 发电机**附近的地面 ⇒ 应该有粒子 + 一声（`Tap Hit Metal` / `Tap Hit Metal Heavy`）。
· 点**棋盘空中央** ⇒ **不该有反应**（原版也这样，见 §①）。
· 打开 `TapParticleController.debugCollisions`（Inspector 勾上）可以看原版同款日志
  `[frame] Detected N collisions` / `Selected <collider>` —— 这是最快的「闸到底卡在哪一道」的诊断口。
