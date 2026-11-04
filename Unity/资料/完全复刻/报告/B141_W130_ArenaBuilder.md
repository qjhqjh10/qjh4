# B141 / W18 — `ArenaBuilder` 生成器后处理（W130-O6）+ `B124-PREVIEWMTIME`

- 批次 B141 · 会话 128 · 代理 **W18**（写手）
- 范围：`判据/B141_W130_对账_资源.md` §一 **16-02 / 16-03 / 16-08 / 16-11** · §二 **W130-O6** · **`B124-PREVIEWMTIME`**（2026-11-04 裁定采纳）
- 未跑 Unity、未动 git、未改 `d:/2`、未改账、未改正本与所有权表

## 一句话结果

四条网格参数（16-02/03 四字段 · 16-08 `m_MeshMetrics` · 16-11 法线通道 `dimension=52`）**现在只有一处写口**（`B140_WS_MeshFlags`），
并且**挂进了 `ArenaBuilder` 的五个生成器入口**（`Build` / `BuildFromCLI` / `BuildAll` / `BuildArenaPrefabs` / `BuildPrefabFromCLI`）——
重跑 `EffectExporter` 之后不会再「忘了跑一次工具」；预览时效守卫新增**源码 mtime 第三路**（比 **scene**，不比 preview），
`render_our_arenas.sh` 照旧放行，「改 `ArenaBuilder.cs` 不重建」新增为红。`typecheck 0/0`；两态用 Python 静态模拟 + 断言宿主各自验过。

## 一、16-02 / 16-03 / 16-08 / 16-11 各改了什么

### 16-02 · 16-03（网格四字段）—— **不新增写口，只接线**

- 写口原样保留：`WarpforgeArena1/Editor/B140_WS_MeshFlags.cs` 的 `ApplyAll(bool dry)`（清单 **101 份**、逐份原版值，⛔ 不统一填 -1/4/1/1）。
- 本轮新增的是**调用点**：`ArenaBuilder.MeshPostProcess(bool dry = false)`（`:6765`）**只转发** `B140_WS_MeshFlags.ApplyAll`，
  并接在五个生成器入口上（`:574` / `:576` / `:582` / `:617` / `:641`）。
  - `:582` = `BuildAll()`（`render_our_arenas.sh` 的第 ① 步）
  - `:617` = `BuildArenaPrefabs()`（第 ② 步）
  - `:641` = `BuildPrefabFromCLI()`
  - `:574` / `:576` = 菜单 `Build()` / CLI `BuildFromCLI()`（单场）
- 🔴 **为什么必须接线**：判据 16-11 原文「⛔ 别只改盘，`B140-WS-RERUN` 会冲回」——
  `EffectExporter` 重导出网格时这些字段会回到 Unity 默认值（`1 / 0 / 0 / 0` · metrics `1/1` · dimension `4` 或 `3`）。
  挂在建场入口后，「重跑导出 → 重建」这一条流水线自动把它补回来。
- ⚠️ **16-03 还多一道验收**：`KeepVertices/KeepIndices = 1/1` 要在**打包后**冒烟（消费点 `Core/CardView.cs:3792` 真读 `sharedMesh.vertices`）。
  编辑器断言（`B140_WS_Assert.CheckA_Flags` + 本轮 `B141_W18_Assert`）**不能替代**出包验证 —— 这条仍是 W132 的活。

### 16-08 `m_MeshMetrics`（58 份）

- 新清单 `B140_WS_MeshFlags.cs:165` `MetricManifest` = `<相对 Assets 路径>|<m_MeshMetrics[0]>|<m_MeshMetrics[1]>`，**58 条**。
- 值 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Mesh/<同名>.asset:160-161` 的**现读文本**（逐字复制，⛔ 不按几何重算）。
- 现读对账：101 份里 **58 份不同**（我们恒 `1 / 1`），其余 **43 份原版也是 `1 / 1`** ⇒ 不列进清单。
- 写法：文本级定点改写 → `AssetDatabase.ImportAsset(ForceUpdate)` → **重开文件按字节回读**；回读不符 ⇒ **报红**（⛔ 不静默）。
  为什么不用 `SerializedObject`：`m_MeshMetrics` 是 Unity 内部数组字段，按名取不稳（与 16-11 的 `dimension` 同族，后者 API 根本写不出高 4 位）。

### 16-11 `m_Channels.dimension = 52`（4 份）

- 新清单 `B140_WS_MeshFlags.cs:233` `DimManifest` = `<相对 Assets 路径>|<通道下标>|<dimension>`，**只有 4 条**：

  | 文件 | 通道 | 我们现读 | 原版 |
  |---|---|---|---|
  | `WarpforgeVFX/ArenaOriginalPrefabs/battlearenaaeldari/Tower 11.asset` | 1（法线） | 3 | 52 |
  | `WarpforgeVFX/Meshes/BoosterPack.asset` | 1 | 4 | 52 |
  | `WarpforgeVFX/Meshes/Card 3D Subdivided.asset` | 1（判据点名的那份 `:50`） | 4 | 52 |
  | `WarpforgeVFX/Meshes/Card_Remnant_HO Optimization 2.asset` | 1 | 4 | 52 |

- 🔴 **刻意不动的 4 份**：`Rock003` / `Rock004` / `Rock005` / `stone001` —— 原版是**压缩网格**（通道 `dimension` 全 `0`、`m_VertexCount: 0`、数据在 `.resS`），
  属 §16 **16-16「4 个压缩网格」** 那个 ❓ 条目。把它们按「全 0」写会与压缩数据对不上 ⇒ ⛔ 不在本条范围（断言里有一条专门守这一格）。
- 写法同 16-08（`PatchChannelDim`：只在 `m_Channels:` → `m_DataSize:` 区间内按**顺序第 N 个** `dimension` 行替换）。

### 判据/范围自检（写口自带）

- `ApplyAll` 末尾调用 `ApplyMetricsAndDims`（`:321`）⇒ 一个命令同时写三族，⛔ 没有第二个写口。
- `VerifyAll` 末尾调用 `VerifyMetricsAndDims`（`:404`）⇒ `B140_WS_MeshFlags.Verify` / `DryRun` / `Run` 三档都能一次看全。
- 清单条数护栏沿用 `ExpectedCount = 101`；新增两族都断言「落在四字段名单之内」「文件全部存在」「压缩网格不得入表」（见第三节断言宿主）。

## 二、`B124-PREVIEWMTIME` —— 时效守卫第三路（源码 mtime）

- 位置：`ArenaBuilder.EnsurePreviewFresh`（现 `:818-883`），第三路在 `:866-867`：
  `stale.AddRange(StalePreviewSources(sceneT));`
- 源文件（`ArenaBuilder.cs:6781` `PreviewSourceFiles()`）：
  1. `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`（本生成器，用 `Application.dataPath` 拼，⛔ 不靠 `MonoScript` 查询）
  2. `d:/2/Warpforge_tools/scripts/gen_unity_arena_manifest.py`（简报点名的上游清单生成器）
  3. `d:/4/Unity/工具/scripts快照/gen_unity_arena_manifest.py`（**额外的**：CLAUDE.md §15 写明这才是真正的写手工作副本；两份都给，改任一份都该红）
- **比较对象 = scene**（`scenePath` 的 `GetLastWriteTimeUtc`）：⛔ 不比 preview —— preview 在守卫**之后**才写，拿它比会**首渲即红**。
- 缺文件 ⇒ 只 `LogWarning` 跳过这一路（⛔ 不误判成红；跨机器时 `d:/2` 可能不在）。
- 逃生口：沿用既有 `WF_SKIP_STALE_GUARD=1`（只警告、照渲，日志明说「逃生口已使用」）。
- 提示文案仍给两条重建命令（scene → `BuildFromCLI` / prefab → `BuildPrefabFromCLI`）。

### 两态判定（Python 静态模拟 `EnsurePreviewFresh` 的判定顺序，2026-11-04 跑）

| 场景 | 判定 |
|---|---|
| A. `render_our_arenas.sh`（先 `BuildAll` + `BuildArenaPrefabs` 再渲） | **ALLOW** |
| B. `touch ArenaBuilder.cs` 不重建 | **RED**（原因 =「源 … 比【场景】新 ⇒ 改了生成器/清单生成器但场景没重建」） |
| C. B + `WF_SKIP_STALE_GUARD=1` | **ALLOW**（日志明说逃生口已用） |
| D. 改**清单生成器**不重建 | **RED** |
| E. 只动 `preview_*.png` | **ALLOW**（preview 不参与判定，保持原判据） |
| F. 源文件缺失 | **ALLOW**（只警告，不误判） |
| G. 改旁挂 `<场>_*.json` 不重建（既有行为） | **RED**（不变） |
| H. 改 manifest 不重建（既有行为） | **RED**（manifest > scene 且 > prefab，不变） |

- ⚠️ **盘上现状（本题改完、还没重建时）就是红的**：`Scenes/battlearena1.unity` = `2026-11-03 16:32Z`，
  而 `ArenaBuilder.cs`（本次写入）与 `工具/scripts快照/gen_unity_arena_manifest.py`（11-04 01:13）都比它新。
  **这是新判据的预期行为**（改生成器没重建），跑一次 `render_our_arenas.sh` / `BuildAll` 即转绿。

## 三、验证

1. **秒级类型检查**（隔离 TMPDIR，避免与并发写手互撞输出）：
   `TMPDIR=/tmp/wf_b141_w18 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**，且 `wf_csc_editor.rsp` 里确有
   `ArenaBuilder.cs` / `B140_WS_MeshFlags.cs` / `B141_W18_Assert.cs`（新文件已被生成器扫进源清单）。
   - ⚠️ 第一次跑曾报 **5 条运行时错**（`RuleEngine/Data/DeckStore.cs:461` `SplitTopLevelArray`、`DeckLibrary.cs:825/841/854/867` `Debug`）——
     **都不是我改的文件**，是别的写手当时正在写；约 10 分钟后复跑即 0/0（与 CLAUDE.md 铁律 13·3 的判据一致）。
2. **文本级改写的静态模拟**（用与 C# `PatchMetricLines` / `PatchChannelDim` **逐字相同**的正则，在**临时副本**上跑）：
   - metrics：58 条全部命中；**实际改动 66 行**（58 个 `[0]` + 8 个 `[1]`，其余 50 条的 `[1]` 本来就是 1 ⇒ 不动）；
     每次改动**只碰目标行**、行尾与行数守恒、回读等于目标值。
   - dimension：4 条全部命中，各改 1 行；`通道 1` 现读 3/4/4/4 → 52。
   - **BAD = 0**（脚本在 `/tmp/wf_b141_w18/sim_patch.py`，临时文件，不入库）。
3. **断言宿主**（新文件，`WarpforgeArena1/Editor/B141_W18_Assert.cs`，149 行）：
   `-executeMethod B141_W18_Assert.RunFromCLI`，四组：
   - ① 第三路**两态**（临时文件：源旧 ⇒ stale 0；源新 ⇒ stale 1 且原因含「场景没重建」；缺文件 ⇒ 0）
   - ② 源名单**现场生效**（`sceneT=2000` 时必然判出 ≥2 份）
   - ③ 写口清单**范围**（101 / metrics>0 / dimension **恰好 4** / 后两族都在 101 之内 / **压缩网格不得入表** / 文件全在）
   - ④ 盘上值 = 原版值（`VerifyMetricsAndDims`；⚠️ 该项**要求先跑过写口**，没跑过时红是事实）

## 四、行数变化

| 文件 | 前 | 后 | 说明 |
|---|---|---|---|
| `WarpforgeArena1/Editor/ArenaBuilder.cs` | 6738 | 6815 | +77：五个入口各 1 行、第三路 3 行 + 文档、类尾 3 个新成员 |
| `WarpforgeArena1/Editor/B140_WS_MeshFlags.cs` | 390 | 720 | +329：两份新清单（58 + 4 条）+ 读写口 + metrics/dimension 实现 |
| `WarpforgeArena1/Editor/B141_W18_Assert.cs` | — | 149 | 新建（无 `.meta`，与 `B140_WS_MeshFlags.cs` 同：等 Unity 首次导入自动生成） |

- 行尾：三份文件全部 **CRLF、无 BOM、无裸 CR**（`b.count(b'\r')-b.count(b'\r\n') == 0`）。
- 账影响：`账` 里对这两个文件的引用是 **`文件（方法名）`** 形式、**不带行号**（`账/_parts/_merged_B140/B140_WS.tsv:2-4`）
  ⇒ 本次无 `check_ledger_points` 漂移来源；新成员按纪律追加在**类尾**（`ArenaBuilder` 非 `partial`，⛔ 不新开 partial）。

## 五、哪些项必须 Unity 重建/重跑才生效

| 条目 | 现状 | 一次什么样的 Unity 动作能解决 |
|---|---|---|
| **16-02 / 16-03**（四字段） | 盘上仍 `1 / 0 / 0 / 0` 那批默认值 | 跑 `ArenaBuilder.BuildAll`（或任一场 `BuildFromCLI`）= 自动调写口；也可单独 `-executeMethod B140_WS_MeshFlags.Run`。**16-03 还要出包冒烟** |
| **16-08**（metrics） | 盘上仍 `1 / 1` | 同上（写口已并入 `ApplyAll`） |
| **16-11**（dimension） | 盘上仍 `3 / 4 / 4 / 4` | 同上；⚠️ **写回后要回读确认 Unity 没把它改写回去**（写口自带回读，不符会报红） |
| **16-04 / 16-05 / 16-06 · 15-05 · BF-08** | 代码+数据已落，盘上 prefab / 场景是 **2026-11-01 旧产物** | **本轮一个字都没改这些字段**（按简报要求）；**一次 `ArenaBuilder.BuildPrefabFromCLI` + `BuildFromCLI`（13 场，串行）会一并生效** —— 验收时⛔ 别误判成「修错了」 |

## 六、坑 / 未知（⛔ 别当成已解决）

1. ⚠️ **`m_MeshMetrics` 是否会被 Unity 在 `ImportAsset` 时重算**：本机没跑 Unity，无法实测。
   写口已按「写完必回读」设计 —— 如果 Unity 覆盖它，会**逐份报红**而不是静默通过。**首次 Unity 运行要看那行日志**。
2. ⚠️ **`dimension: 52` 的高 4 位（`0x30` 标志）会不会被 Unity 重新序列化时丢掉**：同上，靠回读判定。若丢，说明这条路在 Unity 6 走不通，
   需要改走「运行时 `SetVertexBufferParams` + 内部 API」或接受差异并记账（届时再判，⛔ 现在不猜）。
3. ⚠️ **守卫立刻变红是预期的**：改完本文件后 13 场场景都比源码旧 —— 下一次 `render_our_arenas.sh`/`BuildAll` 之前**不要**拿 `RenderPreviewFromCLI` 出图
   （除非显式 `WF_SKIP_STALE_GUARD=1`，那是「明知旧数据」的口子）。
4. ⚠️ 写口在 `BuildAll` 里会让**每次全量建场都做一遍 101 份 YAML 比对**（幂等、已对即跳过）；如果将来嫌慢，
   可以在 `BuildInternal` 层面只做一次，但 ⛔ 别为了快把它移出「重跑导出之后」的那条流水线。

## 七、出处

- 判据：`资料/完全复刻/判据/B141_W130_对账_资源.md` §一 16-02/16-03/16-08/16-11 · §二 W130-O6 · §三（W132 必须 Unity）· §四 顺手发现 #4
- 原版值：`d:/2/tools/assetripper_out/ExportedProject/Assets/Mesh/<同名>.asset`（`m_MeshMetrics[0/1]` `:160-161` · `m_Channels` 通道 1）
- 我们现值：`Assets/WarpforgeVFX/Meshes/Card 3D WH40k.asset:156-162` · `Card 3D Subdivided.asset:49-50`
- 写口：`Assets/WarpforgeArena1/Editor/B140_WS_MeshFlags.cs`（`Manifest` · `MetricManifest:165` · `DimManifest:233` · `ApplyMetricsAndDims:501` · `PatchMetricLines:663` · `PatchChannelDim:680`）
- 接线 + 守卫：`Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`（`MeshPostProcess:6765` · `PreviewSourceFiles:6781` · `StalePreviewSources:6792` · `StaleSources:6798` · 第三路 `:864-867`）
- 断言：`Assets/WarpforgeArena1/Editor/B141_W18_Assert.cs`
- `mtime` 第三路的原始缺陷记录：`判据/B114_战场五条时效核查.md:39` + `:137-139` §3·1（`EnsurePreviewFresh` 头部注释仍保留那段说明）
