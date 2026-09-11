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

**更稳的验收指标**（不受带宽边界影响，建议以后用它）：
`|ln(亮度比)|` 的中位数与均值。当前这份数据：可判定 722 个，**中位 0.196 / 均值 0.397**，
64% 落在 `|ln| ≤ 0.336`（即 0.7–1.4）带内。
