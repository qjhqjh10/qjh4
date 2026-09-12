# 比对基线 —— 特效还原的原始测量数据

> 建立：2026-09-11（第四轮）　|　由 `工具/analyze_sweep.py` 消费，产出 `资料/特效还原台账.tsv`

这里放的是**台账的原始数据**，不是给人看的成品。之所以留在 `资料/` 而不是临时目录：
它是「哪个效果对得上、哪个不对」的唯一凭据，重跑一次要 25 分钟，而且是判断任何改动
是进步还是退步的基准。**改动比对逻辑前后，都要能拿这份数据重算一遍。**

## 文件清单

| 文件 | 是什么 | 怎么来的 |
|---|---|---|
| `sweep_orig.tsv` | **原版侧**：958 个效果 × 8 个时刻的亮点数/亮度和 | `EffectSweepBatch.Run`（`Side="orig"`） |
| `sweep_exp.tsv` | **导出侧**：同上 | `EffectSweepBatch.Run`（`Side="exp"`） |
| `sweep_frames.tsv` | 每个效果的相机取景（位置/朝向/fov/near/far） | 原版那趟算出来缓存，导出那趟复用 |
| `per_effect_metrics.json` | 旧的**单帧**指标（1.2s 采样） | 已废弃，只作为「旧判定」对照列保留 |
| `per_effect_tech.json` | 每个效果的渲染器构成（Mesh 粒子/精灵图/发射器数/特效家族） | 扫导出 prefab 得到 |
| `AB_精灵修复_20260911.csv` | **一次改动的逐效果 A/B**（精灵导出修复） | `工具/compare_sweep.py 改前.tsv 改后.tsv --csv ...` |
| `历史/` | **改前的三份基线快照**（精灵修复之前） | 留着做「这个改动到底有没有用」的对照，**别删** |

TSV 列：`effect  time  lit  sum  lit_srgb  sum_srgb`
**判读用 `lit` / `sum`（原始值）。** `*_srgb` 那两列是排查色彩空间时加的，
实测把 sRGB 变换再套一遍会把所有比值压到 0.9 附近、掩盖真实差异 —— 别用。

## 怎么重新生成

```bash
# 1) 原版侧（会加载全部 84 个源 bundle，并产出取景缓存）
#    先把 EffectSweepBatch.cs 里的 Side 改成 "orig"
Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_o.log"

# 2) 导出侧（一个源 bundle 都不加载 = 真实运行时条件）
#    把 Side 改成 "exp"
Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_e.log"

# 3) 出台账
"D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/analyze_sweep.py"
```

**顺序不能反** —— 导出那趟依赖原版那趟产出的 `sweep_frames.tsv`。

## ⚠️ 两个必须记住的坑（都实测踩过，两个方向都能把 900+ 个效果误判）

1. **原版那趟必须加载全部 84 个源 bundle**。原版 prefab 的材质也要解析 shader，
   而 `battleprefabs` 里的 shader 不是可加载资产（`LoadAllAssets<Shader>()` 返回 0）。
   只加载它一个包 → 原版渲染成**品红** → 品红很亮 → 原版亮度和虚高 → 比值假性掉到 0.2。
2. **导出那趟一个源 bundle 都不能加载**。`wf_shaders_extra.bundle` 是从 `battleprefabs`
   抽出来的，那个包在场时 Unity 按「内容相同」判重、拒绝加载补充包 → 导出侧退回占位材质。

两边的条件互相冲突，所以**必须分两个进程跑**，结果按效果名合并。
试过在一个进程里 `Unload(false)` 卸包，但连 prefab 一起销毁了，不行。

## 当前这份数据的口径

- 8 个时刻：0.15 / 0.30 / 0.50 / 0.75 / 1.00 / 1.50 / 2.00 / 3.00 秒
- 噪声下限：两侧亮点数都 ≥ 60 才参与判定（256×256 = 65536 像素）
- 判定依据：在「两侧都亮」的时刻上取**亮度比的中位数**，落在 0.7–1.4 算对得上
- 置信度：峰值亮点 ≥500 高 / ≥200 中 / 其余低

**验收指标 = `|ln(亮度比)|` 的中位/均值**（不受带宽边界影响）。
当前这份数据：可判定 722 个，**中位 0.196 / 均值 0.397**，64% 落在 `|ln| ≤ 0.336`（即 0.7–1.4）带内。

> ⚠️ **「Z 计数」（对得上多少个）不是验收指标，只是分组口径。**
> 2026-09-11 实测：边界每挪 0.05，Z 计数就摆动 **22–54 个**（[0.70,1.40]→470，[0.65,1.45]→496，
> [0.75,1.33]→436），压在边界两侧 ±0.10 的「临界质量」有 **124 个**。
> 所以计数变化几十个说明不了任何事 —— 用 `|ln|` 的中位/均值。
> `工具/analyze_sweep.py` 每次运行都会把这张敏感度表打出来（「尺子自检」段），不用记。

> ⚠️⚠️ **尺子本身曾经是不确定的（2026-09-11 已修，务必保持）**
> 同一份资产、同一份代码**连跑两遍**导出侧：变好 246 / 变差 207，Z 477→486，
> `|ln|` 均值 0.374↔0.387 —— **噪声和要测的改动一样大**。
>
> 真因是**粒子系统的随机种子**：`useAutoRandomSeed` 默认是**开**的，每次重播都换种子，
> 粒子位置/大小/寿命每次都不同。判据（`StabilityProbe`）：同一份资产**在同一个进程里
> 连渲三次**得到 537 / 518 / 512 个亮点，而亮度和几乎不变 —— 分布变了、能量没变。
>
> 修法三件事缺一不可（`EffectSweepBatch.SeedAndReset()`）：
> ```csharp
> ps.useAutoRandomSeed = false;      // 关自动种子
> ps.randomSeed = 20260911;          // 显式钉死
> ps.Simulate(0f, true, restart: true, fixedTimeStep: true);   // 先复位到 0 时刻
> ps.Simulate(t,  true, restart: false, fixedTimeStep: true);  // 再推进
> ```
> 修完：两遍扫描 2968 行里只剩 11 行不同，且都是 1 个像素/1 个亮度单位（原来的 1490 行）。
>
> **顺带排除的两个嫌疑**（别再往这两条上查）：去掉渲染前的 `ps.Play()` 没用；
> 把 `fixedTimeStep` 从 false 改成 true 也没用。
>
> **做 A/B 之前先跑一遍 `StabilityProbe`**：尺子不确定的话，任何「改善了没有」都不成立。
