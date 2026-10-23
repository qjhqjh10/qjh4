# 战场线 · prefab 13 场推广（YAML 级抽取 · staging 全量导出）

> 日期 2026-10-23 · 执行代理产出
> 判据 = `资料/普查产出_1023b/战场线_prefabYAML试点_battlearena3.md`（工具用法 / 丢 MonoBehaviour 口径 / 静态自检 9 条）
> ＋ `资料/普查产出_1023b/战场线_15prefab清单.md`（16 根 / 每场节点数 / blacklegion 跨根引用）
> 源 = `d:/2/tools/assetripper_out/ExportedProject`（**只读，一个字节都没改**）
> 工具 = `Unity/工具/extract_arena_prefab_yaml.py`（**扩展**：staging 默认输出 + `--merge-root`）
> 产物 = `d:/4/_tmp_view/arena_prefabs_out/`（**staging；`Assets/` 一个文件都没写**）
> [OK] 本次**没有跑 Unity**（按任务书）；产物要 import 一次才算最终验收。

---

## 一、结论（先看这六条）

1. **13 / 13 全绿**：13 颗 prefab 全部导出、**自检 13/13 或 11/11 全 PASS**（11 条 = 试点 9 条 + 我补的 2 条结构核对；
   合并的三场再加 2 条合并专项）。**没有任何一场失败**，所以没有「最大失败场」。
2. **节点总数 1 559**，与 `战场线_15prefab清单.md` §二「合计 16 颗根 / 1 559 节点」**逐场相加完全相等**
   （86+133+123+76+124+108+97+100+89+154+139+91+239 = 1 559）⇒ 父链遍历口径与清单一致、没有漏节点。
3. 🔴 **合并必须「按世界变换保持」来挂，不能把局部 TRS 置 0**（这是本次与任务书写法唯一的实质偏差，见 §三）。
   实测 **astra / EC 的 `Scenario` 根不在原点**（`(113.23796, -8.695, 19.85265)`），
   而它们的 `Baked` 根在原点 ⇒ 若按「根变换都是单位阵 ⇒ 直接挂上去」处理，
   **Baked 里的全部地形/建筑会整体平移 ~113 单位**。工具改成
   `新局部 TRS = inverse(Scenario 世界) × Baked 世界`，并配了「逐节点世界变换保持」自检（越限 0 / 124、0 / 108、0 / 100）。
   blacklegion 两边都在原点，局部 TRS 原样是 `(0,0,0)` —— 与任务书说法一致。
4. **幂等**：13 场各跑两遍，staging 全部 **2 150 个文件 sha256 逐一相同**（prefab / 资产 / `.meta` 全部字节不变）。
5. **与试点零回归**：`battlearena3__Scenario.prefab` 的 md5 = `6a8977016f5494c24432da9e3020a2a8`，
   与试点写进 `Assets/.../battlearena3/` 的那份**完全相同**。
6. **staging 体积 364 MB**（13 个 arena 目录合计；含 `_json` / `_logs` 共 368 MB，2 150 文件）；
   prefab 本体合计 **32.4 MB**。

---

## 二、逐场结果表

- 「合并根」= `--merge-root` 挂进来的独立 `Baked` 根（括号里是它的节点数；已含在「节点数」里）。
- 「资产（拷入/已有）」= 闭包里**真拷进 staging** 的文件数 / **我们工程里同 guid 已存在**（跳过拷贝）的文件数。
- 每场目录里还有：1 个 `.prefab` + `.prefab.meta` + 目录 `.meta` + 闭包资产 + 资产原 `.meta`。

| 场（arena key） | 节点数 | 合并根 | 丢 MonoBehaviour | 资产（拷入/已有） | 目录 MB | 自检 | prefab md5（前 12） |
| --- | --- | --- | --- | --- | --- | --- | --- |
| battlearena1 | 86 | — | 25 | 66 / 2 | 32.4 | 11/11 | `1c15b63f3d7d` |
| battlearena2 | 133 | — | 37 | 88 / 2 | 47.0 | 11/11 | `1188e681fd1b` |
| battlearena3 | 123 | — | 27 | 0 / 70 | 2.6 | 11/11 | `6a8977016f54` |
| battlearenaaeldari | 76 | — | 36 | 88 / 4 | 22.5 | 11/11 | `e002fe7dc5f6` |
| battlearenaastramilitarum | 124 | Battle Arena Astra Militarum Baked (84) | 38 | 103 / 1 | 37.1 | 13/13 | `21893d7c5a08` |
| battlearenablacklegion | 108 | Battle Arena Black Legion Baked 2 (70) | 22 | 72 / 3 | 30.2 | 13/13 | `e58b9a320a38` |
| battlearenadarkangels | 97 | — | 13 | 86 / 2 | 26.3 | 11/11 | `7618f2c368be` |
| battlearenaemperorschildren | 100 | Battle Arena Emperor's Children Baked (79) | 5 | 99 / 1 | 23.7 | 13/13 | `ac5c74adcded` |
| battlearenagenestealers | 89 | — | 14 | 72 / 3 | 28.9 | 11/11 | `6a73165a5ed1` |
| battlearenaleviathan | 154 | — | 25 | 93 / 5 | 28.5 | 11/11 | `f15fc08fbfca` |
| battlearenasororitas | 139 | — | 15 | 107 / 2 | 28.2 | 11/11 | `6ac1921de2c7` |
| battlearenaspacewolves | 91 | — | 12 | 69 / 1 | 19.3 | 11/11 | `b6432c32b9e3` |
| battlearenatauviorla | 239 | — | 32 | 94 / 7 | 37.3 | 11/11 | `f78529254bd4` |
| **合计** | **1 559** | 3 根 / 233 节点 | **301** | **1 037 / 103** | **364.0** | **13/13 场 PASS** | prefab 32.4 MB |

- **每场都是 `Scenario` 根**（含 `leviathan`，与清单 §一·1 一致）。
- prefab 命名：`<arena>__Scenario.prefab`（与任务书一致；合并场也是这个名字，Baked 是它下面的一个子节点）。
- 机器可读清单：**`d:/4/_tmp_view/arena_prefabs_out/_manifest.tsv`**
  （22 列：场 / prefab / 节点数 / 主根节点 / 合并根 / 合并节点 / 丢 MonoBehaviour / 闭包文档 / 保留文档 /
  外引 guid / 拷入 / 已有 / 仅导出 / 内建 / 解不到 / prefab 字节 / 目录字节 / 自检通过数 / 总数 / 结果 / md5）。
- 每场的完整摘要 JSON：`_json/<arena>.json`；运行日志：`_logs/<arena>.log`（第二遍在 `_logs/<arena>__pass2.log`）。

---

## 三、合并说明（3 场：astra / blacklegion / EC）

### 3·1 机制（工具 `--merge-root`）

1. 主根（`Scenario`）子树先算好；
2. 对每个 `--merge-root <名>`：按 `m_Father == 0` + `GameObject.m_Name` 找到那颗**独立根**，取它的整棵子树；
3. **父链改写**：被挂根的 `Transform.m_Father` 从 `{fileID: 0}` 改成**主根 Transform 的 fileID**；
4. **子链改写**：主根 `m_Children` **尾部追加**一条 `- {fileID: <被挂根>}`（原顺序不动）；
5. **局部 TRS 反算（世界变换保持）**：`新局部 TRS = inverse(主根世界 TRS) × 被挂根世界 TRS`
   （四元数乘 / 逆旋转 / 分量除 scale；近零归零、近单位四元数吸附成 `{0,0,0,1}`）；
6. 其余字段、文档顺序、fileID 重编号全部沿用试点口径。

被挂的那颗根 **保留为节点**（`Battle Arena … Baked` 这个名字还在）——
因为它自己身上挂着 `Animation` 组件（三场都是），删掉根节点就会把动画组件一起删掉。

### 3·2 逐场读数（工具打印，`_logs/<arena>.log`）

| 场 | 主根节点 | Baked 节点 | 合并后 | 主根世界位 | Baked 世界位 | 写入的局部 TRS（位置） |
| --- | --- | --- | --- | --- | --- | --- |
| battlearenaastramilitarum | 40 | 84 | **124** | `(113.23796, -8.695, 19.85265)` | `(0, 0, 0)` | **`(-113.23796, 8.695, -19.85265)`** |
| battlearenablacklegion | 38 | 70 | **108** | `(0, 0, 0)` | `(0, 0, 0)` | `(0, 0, 0)` |
| battlearenaemperorschildren | 21 | 79 | **100** | `(113.23796, -8.695, 19.85265)` | `(0, 0, 0)` | **`(-113.23796, 8.695, -19.85265)`** |

- 三颗 `Baked` 根**自身**确实都是单位阵（位置 0 / 旋转单位 / scale 1）—— 任务书这句是对的；
  **但 astra / EC 的父根 `Scenario` 不在原点**，所以「挂上去」这一步必须反算局部位置。
- **世界变换保持自检**（逐节点比 源场景 vs 产物）：三场都是 **越限 0 / 124、0 / 108、0 / 100**，
  位置最大偏差 **0**、scale 最大偏差 **0**、四元数最差点积 **1.000000000**。

### 3·3 blacklegion 的 4 条跨根 bare fileID

清单 §三·2 说的那 4 条（`Scenario/Particle Effects` 的 `ScenarioParticleSystemToggler.particleSystems:`
→ `Baked 2/Firepits/*`）在合并后**目标节点全部在产物里**，且新 fileID 已按统一偏移重编号：

| 源 fileID | 产物 fileID | 目标 |
| --- | --- | --- |
| 1702 | `1000000000001702` | `FirePits/FirePit` |
| 1706 | `1000000000001706` | `FirePits/FirePit/Smoke` |
| 1709 | `1000000000001709` | `FirePits.001/FirePit (1)` |
| 1743 | `1000000000001743` | `FirePits.001/FirePit (1)/Smoke` |

⚠️ 但**引用本身跟着 MonoBehaviour 一起丢了**（toggler 是 MonoBehaviour，按试点口径丢弃）——
即「目标在、引用不在 ⇒ 开关行为仍然没有」。要保住 toggler 就必须破例保留那一颗 MonoBehaviour
（工具支持 `--keep-mono`，但那会把该场所有 MonoBehaviour 都留下 ⇒ 需要一次单独裁定，见 §五·5）。

---

## 四、自检读数

### 4·1 工具内建（每场都跑，写在落盘文本上）

试点的 9 条（全部保留）：

| # | 检查 | 13 场结果 |
| --- | --- | --- |
| 1 | 节点数 = 原版子树节点数 | 13/13 PASS（合并场按「主根 + 合并根」核） |
| 2 | `m_PrefabInstance` 非零 = 0 | 13/13（0 处） |
| 3 | `m_CorrespondingSourceObject` 非零 = 0 | 13/13（0 处） |
| 4 | 根 `m_Father: {fileID: 0}` 且**只有它一个** | 13/13（m_Father=0 的 Transform 恰好 1 个） |
| 5 | 无 guid 的 fileID 引用全部落在产物内 | 13/13（悬空 **0** 处） |
| 6 | fileID 唯一 | 13/13 |
| 7 | fileID 全是大整数（≥1e15） | 13/13（小号 0 个） |
| 8 | PyYAML 逐文档解析 | 13/13（按 `--- !u!` 切块 + 每条补回 `%TAG` 头） |
| 9 | 每个外引 `{fileID,guid,type}` 都能解析 | 13/13（**解不到 0**；内建 0–2 个） |

新增 2 条（把试点里「工具外的一次性核对」收进工具）：

| # | 检查 | 结果 |
| --- | --- | --- |
| 10 | `m_Children` ↔ `m_Father` **一一对应**（含反向：每个非根必须出现在父的 `m_Children` 里） | 13/13 |
| 11 | 组件引用双向闭合（`m_Component` → 产物；组件 `m_GameObject` → 产物；`m_Component: []` 0 处） | 13/13 |

合并专项 2 条（只 3 场合并有）：

| # | 检查 | 结果 |
| --- | --- | --- |
| 12 | 合并后**世界变换保持**（逐节点：位置 / scale / 四元数） | 3/3，越限 0 |
| 13 | 跨根 bare 引用的**目标**都在产物内 | blacklegion 4 条命中；另两场 0 条（N/A 也 PASS） |

### 4·2 工具外的独立复核（**不复用工具代码**，另写一份解析器 + 世界变换）

- 13/13：**节点路径多重集**（含合并根前缀折叠）与**世界坐标多重集**（3 位小数）与源场景**完全相等**；
- 合并根在产物里**恰好 1 个**、`m_Father` 指向 `Scenario`、世界位 `(0,0,0)` = 源里那颗根；
- 10 场非合并场：路径 + 世界坐标多重集完全相等。

### 4·3 幂等

- 第一遍 2 150 个文件 sha256 快照 → 13 场**各再跑一遍** → 2 150 个文件 sha256 **逐一相同**（`diff` 为空）。
- `battlearena3` prefab 与试点产物 **md5 相同** ⇒ 本次工具改动（staging 默认 / `--merge-root` / 新自检）**没有动到非合并场的输出**。

### 4·4 `.meta` 完整性

13 个 arena 目录：每个都有 `<arena>.meta`，**所有子目录都带 `.meta`，缺 0 个**；
拷贝进来的资产**原 `.meta` 原样复制**（guid 不变 ⇒ prefab 外引自动成立）。
新建的（prefab 本体 + 各级目录）用确定性 guid `md5("wf-arenaoriginalprefabs:"+相对路径)`
—— 与试点同一套 salt，所以 `battlearena3` 的 guid 与试点**一致**。

---

## 五、没查清 / 已知代价（⛔ 别当成已查）

1. **Unity import 一次都没跑**（任务书不许跑 Unity）。产物是否被 6000.3.23f1 吃下、画面对不对，
   要等主对话 import。**这是唯一的最终验收缺口。**
2. **shader 没映射**（按任务书）：材质 `m_Shader` 仍指 ExportedProject 的 **Dummy shader guid**（149/149 是
   `//DummyShaderTextExporter`，**不拷**）⇒ import 后这些材质的 shader 槽会 **Missing / 品红**。
   每场跳过 `.shader` 4–13 个（最多 aeldari 13、leviathan 9、darkangels/sororitas/tauviorla 8）。
   映射表试点 §五已列（URP Particles Unlit / Everguild 各支），**本次没动**。
3. **动画 / 状态机 / 时间线一律没拷**（试点默认：只拷网格 / 材质 / 贴图 / Sprite）：
   - `.anim`：astra / blacklegion / darkangels / EC / genestealers / tauviorla(3) / arena3 各 1–3 个
     ⇒ `Animation` 组件的 clip 会 missing；
   - `.controller`：arena1（`Cannon 1`）· arena2（3 个）· leviathan（1 个）⇒ 这 3 场 `Animator` 无控制器；
   - `.playable`：leviathan `Leviathan Bunker Door Animation.playable` ⇒ `PlayableDirector` 无资产。
   要带上就加 `--copy-exts anim,controller,playable`（工具支持，**这次没开**，因为要与试点同口径）。
4. **`ParticlesLight.prefab` 没拷**（arena2 / arena3 的 `ParticleSystem` **`light:` 字段**指向它）⇒ 灯光模块模板 missing。
   同上，`--merge-root` 之外用 `--copy-exts prefab` 可补。
5. **MonoBehaviour 全丢**（试点口径）：13 场共 **301 个**，逐场见 §二。
   影响 = 静态树零变化，少掉的是「活着才会发生的行为」（淡入淡出 / 点按变色 / 材质闪烁 / 画质换材质 /
   粒子开关 / 镜面 / URP 相机附加数据）。**字段值没有留存**，要恢复得回 `d:/2` 的 `.unity` 重读。
   - ⚠️ **blacklegion 的 `ScenarioParticleSystemToggler`**：它那 4 条跨根引用随它一起消失（§3·3）。
     若要保住它，需要单独裁定是否破例保留该场 MonoBehaviour（`--keep-mono` 会保留全部 22 个）。
6. **序列化版本代差**：源 YAML 是 Unity **6000.2.6f2** 时代写的，我们工程是 **6000.3.23f1**。
   Unity 对更老版本有升级路径，**大概率没事、但没有实测**；若 import 后粒子参数缺失，先查这条。
7. **`LookAtConstraint`（type id `1183024399`，仅 arena3 4 个）**、**VFX Graph（aeldari 2 + leviathan 1）**：
   与试点同一不确定性（本机 DLL 里有类名，但 type id 是否与 6000.3 一致没验；VFX Graph 包我们没装）。
8. **与我们工程已有资产同名**：12 场 **56–95 个/场**（sororitas 95、EC 85、astra 81；`battlearena3` 为 0）
   —— guid 不同 ⇒ 会同时存在两份；主因是我们早先那条 `WarpforgeArena1/arenas/…/Models/*.obj` 搬运链。
   **本次没有去重、也没有改指向**（工具只打印告警，摘要里是 `name_dups_vs_our_project`）。
9. 🔴 **有 8 个共享材质住在「试点目录」里，13 场都依赖它**：试点已经把一批材质写进
   `Assets/WarpforgeVFX/ArenaOriginalPrefabs/battlearena3/`，所以本次这 8 个 guid 命中「我们工程已有」⇒ **跳过拷贝**：
   `Glow Additive` · `Glow Additive Extra Color` · `Smoke Sprite Sheet Flipbook Blending` ·
   `Smoke Sprite Sheet No blend` · `Material/Embers 1` · `Fireball_color_soft` · `Embers Circle` · `LightningTrail`
   （8 个 `.mat`）。除 `battlearena3` 外的 **12 场各有 1–7 条外引指向那里（合计 33 条）**。
   ⇒ **⛔ 试点目录不能删/挪**；`battlearena3` 自己的 staging 也因此只有 prefab + meta（2.6 MB，70 个 guid 全在 Assets）。
   （其余资产都真拷进了各自的 arena 目录。）
10. **`Scenario` 根位逐场不同**（8 场 `(113.238,-8.695,19.853)`）：产物**原样保留**（试点口径）。
    要「贴地可复用」用 `--zero-root-position`；⚠️ 该开关与「世界变换保持」自检互斥（工具会跳过那条）。
11. **staging 里跨场有重复**：`our_index` 只扫 `Assets/`（不扫 staging），所以除 §五·9 那 8 个材质外，
    其余共享资产**每场各拷一份**（13 场两两之间有 43 对重叠、重复文件 87 个；staging 唯一 guid 999 / 文件 1086）。
    这是有意的（宁可自足），不是缺陷；但也意味着 **364 MB ≠ 唯一资产量**。要独立分发某一场，见 §六·3。

---

## 六、主对话怎么验收

### 6·1 一眼看结果（不跑 Unity）

```bash
# 逐场自检结论（13 行，应全 PASS）
awk -F'\t' 'NR>1{print $1, $19"/"$20, $21}' d:/4/_tmp_view/arena_prefabs_out/_manifest.tsv
# 幂等（应与第一遍一模一样；本次已实测相同）
# 重跑任意一场：python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena <场> --root Scenario
```

### 6·2 真要 import（**由主对话跑 Unity**，子代理不许）

1. 建议**一次一场**（铁律 15），并从最复杂的 `battlearenatauviorla`（239 节点 / 929 文档）或合并场
   `battlearenaastramilitarum`（124 节点，含跨根 Baked）**先试一场**；
2. 把 `<staging>/<arena>/` **整个目录**（prefab + 资产 + 全部 `.meta`）拷到
   `Assets/WarpforgeVFX/ArenaOriginalPrefabs/` 下（该目录 `.gitignore` 已忽略，不进 git）；
3. import 后的**判据**（按顺序）：
   - Console 无 `unknown class` / `Failed to parse` 报错；
   - **Hierarchy 里 prefab 根 = 1 个，名为 `Scenario`；节点数与 `_manifest.tsv` 的 `nodes` 列一致**
     （可在 Editor 里 `GetComponentsInChildren<Transform>(true).Length` 核）；
   - 合并场再核一条：`transform.Find("Battle Arena … Baked") != null`（它是 Scenario 的直接子件）；
   - 场景里实例化后**位置**：astra / EC 测 `Battle Arena … Baked/Floor` 的世界坐标，
     应约 `(97.074, 5.274, 489.663)`（astra）/ `(99.509, -0.257, -6.279)`（EC）
     —— 若整体偏了 ~113 单位，就是合并局部 TRS 出了问题（本次已按世界保持算好）。
4. **画面上必然看到品红/缺 shader**（§五·2），这是**已知代价**、不是抽取失败；判「抽取对不对」看**节点 / 几何 / 贴图**。

### 6·3 关于「外引落在别的目录」（§五·9 那 8 个材质）

guid 在工程内**全局唯一** ⇒「同 guid 再拷一份」不是选项（两个资产同 guid 会冲突）。
现状是 8 个共享材质住在 `ArenaOriginalPrefabs/battlearena3/`，其余 12 场的 prefab 按 guid 引过去 ——
**只要这 8 个 guid 在工程里还在**（不管在哪个目录），引用就成立。
⇒ **⛔ 不要单独删/挪 `ArenaOriginalPrefabs/` 下的子目录**；要整理目录，先按 guid `grep` 谁在用。
（`_manifest.tsv` 的 `already_ours` 列 = 该场有多少条外引落在自己目录之外，就是这个意思。）

---

## 七、工具改动（`Unity/工具/extract_arena_prefab_yaml.py`）

| 改动 | 说明 |
| --- | --- |
| **默认输出改 staging** | `OUT_ROOT_DEFAULT = d:/4/_tmp_view/arena_prefabs_out`；要写工程用 `--out-root d:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs`（试点口径，`OUT_ROOT_LEGACY_ASSETS`） |
| 新增 `--out-root` | 产物根；`--out-dir` 仍可直指目录（给了 `--out-dir` 时 out_root 取它的父目录，兼容旧用法） |
| 新增 `--merge-root NAME` | 可多次；把独立根按**世界变换保持**挂到主根下（§三·1）；校验：不能等于主根、不能重复、子树不能与主子树重叠 |
| 新增自检 | `m_Children ↔ m_Father` 一一对应 · 组件双向闭合 · 合并世界变换保持 · 跨根 bare 引用目标在产物内 |
| `.meta` 逻辑参数化 | 目录 / prefab 的确定性 guid 一律按 **out_root 相对路径**算（与试点 salt 相同 ⇒ `battlearena3` guid 不变） |
| 摘要新增字段 | `prefab_md5` · `prefab_lines` · `out_root` · `out_dir` · `merge_roots`（含反算出的 local TRS 与源世界位）· `cross_root_bare_refs` · `nodes_main_tree` |

⛔ 仍然**没有** `--all`（铁律 15：一场一场做）。

---

## 八、证据（路径 / 读数）

- 工具：`d:/4/Unity/工具/extract_arena_prefab_yaml.py`
  （`OUT_ROOT_DEFAULT` / `make_local` / `patch_trs_lines` / `append_children_lines` / `--merge-root` / 自检 10–13）
- 源场景（13 份）：`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/*.unity`
- 三颗合并根（现读）：
  - `Battle Arena Astra Militarum Baked` — Transform `&1450` · GO `&307` · 84 节点 · 世界位 `(0,0,0)`
  - `Battle Arena Black Legion Baked 2` — Transform `&1449` · GO `&412` · 70 节点 · 世界位 `(0,0,0)`
  - `Battle Arena Emperor's Children Baked` — Transform `&1363` · GO `&1161` · 79 节点 · 世界位 `(0,0,0)`
  - 三者的 `Scenario` 根：astra `&1285` `(113.23796,-8.695,19.85265)` · blacklegion `&1246` `(0,0,0)` ·
    EC `&1249` `(113.23796,-8.695,19.85265)`
- 产物清单：`d:/4/_tmp_view/arena_prefabs_out/_manifest.tsv`（13 行 + 表头）
- 每场摘要：`d:/4/_tmp_view/arena_prefabs_out/_json/<arena>.json`
- 每场日志：`d:/4/_tmp_view/arena_prefabs_out/_logs/<arena>.log`、`_logs/<arena>__pass2.log`
- 试点报告（对照）：`d:/4/Unity/资料/普查产出_1023b/战场线_prefabYAML试点_battlearena3.md`
