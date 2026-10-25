# X11 · EffectIso 效果清单可筛选 —— 执行报告（2026-10-25）

## 一、清单从哪来（机制）
- **不是**扫目录、**不是**从缓存读 —— 效果清单是 `EffectIso.cs` 里**硬编码的 `Targets` 数组**（改前 9 条，原 `:38`）。
- `资料/比对基线/iso_frames.tsv` 只是**两侧分进程的共享缓存**：orig 趟写「每个效果的相机取景 + 槽位标签」，exp 趟读它做**同机位、同槽位编号**的渲染。它**不含**"要跑哪些效果"。
- 两侧条件互斥（orig 加载全部 84 个源包，exp 一个都不加载），故必须两趟。取景由 `FrameCamera()`（整体包围盒 → pos/look/fov/near/far）算，槽位标签由 `BuildLabels()`（原版/导出按索引对齐，`路径#i`）。
- ⇒ 扩清单**不需要重扫**：把名字交给筛选即可，orig 趟只重算被点名那几个的取景条目。

## 二、改了什么（只动 `Assets/WarpforgeArena1/Editor/EffectIso.cs`）
| 位置 | 改动 |
|---|---|
| `:37-44` | 新增 `WFISO_FX` 读取 + `IsFiltered` |
| `:47` | 原 `Targets` 改名 `DefaultTargets`（**内容与顺序一字未改**） |
| `:78-111` | 新增 `Targets` 惰性属性 + `SelectTargets()`：候选池 = `DefaultTargets` ∪ `Assets/WarpforgeVFX/Prefabs/*.prefab` 文件名（`AssetDatabase.FindAssets`），**逗号分隔 · 子串匹配 · 大小写不敏感** |
| `:152-156` | 统计文件重置：默认仍整份删；筛跑时 `PruneStats` 只剪本次 target 的行 |
| `:167-171` | 取景缓存重置：默认仍整份删；筛跑时 `PruneFrameCache` 只剪本次效果的行，随后 orig 趟**真正重新生成**这些条目（不是只读旧缓存） |
| `:1465-1486` | 新增 `PruneFrameCache` / `PruneStats` |

## 三、主对话要跑的两条命令
bash 前缀变量会继承到 Unity 子进程，`run_iso.sh` **不用改**：
```bash
# 三条一起跑（T02 的 Void Combat + 新账-69 的另两条），只跑 orig+exp 各一趟
WFISO_FX="Void Combat,Raging Storm,Necron Variant" bash d:/4/Unity/工具/run_iso.sh both

# 单条
WFISO_FX="Void Combat" bash d:/4/Unity/工具/run_iso.sh both
```
- 每条子串在本 prefab 目录**唯一命中**（已核）：`Void Combat` → `Environmental Condition Dark Angels Void Combat`；`Raging Storm` → `EnvironmentalCondition Sororitas Raging Storm`；`Necron Variant` → `ShieldTraitEffect_Idle Necron Variant`。
- 日志会打 `[iso] WFISO_FX="…" ⇒ 选中 N 个：…`；**一个都没命中会 `LogError` 且本趟什么都不跑**（不静默）。
- ⚠️ orig / exp **两趟必须用同一份 `WFISO_FX`**，否则 exp 趟取不到取景（会逐条 LogError）。

## 四、默认档为什么等价
不设 `WFISO_FX` 时 `SelectTargets()` 第一句就 `return DefaultTargets` —— 同一数组、同一顺序、不碰 AssetDatabase、无分支；两处文件重置也仍走原来的 `File.Delete`。⇒ 默认档行为与改动前一致。

## 五、typecheck
`TMPDIR=/tmp/wf_iso bash d:/4/Unity/工具/typecheck.sh` → 运行时 **0** / 编辑器 **0**。未跑 Unity。

## 六、RT ×0.25 事实（只陈述，**未改 GrabScreen**）
- 工程侧 `grep "0.25"`：`WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs` **0** · `WarpforgeVFX/Shaders/WFDistortion.shader` **0** · `WarpforgeArena1/Editor/DistortProbe.cs` **0**（`EffectSweepBatch` / `SweepStabilityProbe` / `WarpforgeSetup` 同为 0）。「0 命中」**属实**。
- 反编译侧 41 个 `GrabScreenFeature*.c` 十进制 `0.25` 也是 **0**；**但 `GrabScreenFeature.Settings__.ctor.c:11` 与 `GrabScreenFeatureRenderGraphAPI.Settings__.ctor.c:11` 都是 `*(undefined4 *)(param_1 + 0x1c) = 0x3e800000;` = `0.25f`**（默认 `textureSizeMultiplier`）。⇒ 「RT ×0.25」**是真的，只是反编译里是十六进制浮点字面量**，用十进制串 grep 必然 0 命中。
- 我们现在：`GrabPassTransparentFeature.cs:184-188` 用 `graph.GetTextureDesc(res.activeColorTexture)` 建同尺寸 RT ⇒ **全分辨率、无 0.25 缩放**。

## 七、没查清
- 三个效果在 **orig bundle** 里 `g.name` 是否逐字等于导出 prefab 名：**只做了间接核**（导出 prefab 根 `m_Name` = 文件名；同链路 9 条既有目标都能跑）。真实 bundle 侧要等 orig 趟日志确认。
- 这三条各自的槽位数 / 是否多发射器：要跑了才知道。

## 八、顺手发现
- `iso_frames.tsv` 现存 9 行（= 默认档）；筛跑**不会**挤掉另外几行（已从"整份删"改成"剪枝"）。
- 台账里 `Raging Storm` 的**全名**是 `EnvironmentalCondition Sororitas Raging Storm`（无空格分隔前两段），筛选用子串所以两种写法都能命中；若哪天要按 `DefaultTargets` 硬写，必须用全名。
