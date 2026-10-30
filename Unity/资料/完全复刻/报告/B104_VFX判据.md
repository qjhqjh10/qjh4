# B104 · 特效判据缺口调查报告（2026-10-31 · 只读代理）

> 派活：调度台（用户 2026-10-31「优先解决全部特效/动画问题」）。
> 本代理**只读**；4 份产出由调度台代写落盘：`判据/B104_特效触发链_补判.tsv` · `判据/B104_anim曲线对账.tsv` · `判据/B104_A2判据缺口.md` · 本文件。

## 一、做了什么

| # | 任务 | 结果 |
|---|---|---|
| T1 | 触发链 3 条「判不了」重解 | **3/3 解完**：`targetedSpellAnim` → 原版本身没有（1,351/1,351 张全空 PPtr）；`mulliganSelectedPrefab` → 已接（等价实现 `MulliganPanel.cs:1171/1516`）；`genericBattleAnims` → 原版本身没有（全包唯一 BattleAnims SO） |
| T2 | A2 的 47 条 `.anim` 逐曲线对账 | **476 条曲线 0 差异**；2 条资产级差异（`Card Explosion` 空壳由运行期桥接兜住；46 条 legacy `StopTime=1` 待 Unity 判） |
| T3.1 | `Tap *` 是否棋盘点地 | **不是**：`TapParticleController.Update` 射线打 layer 0+30（场景道具），棋盘在 layer 11 ⇒ 点棋盘空地原版无反馈 |
| T3.2 | 44 条「有 AnimFX 无播放点」是否远端驱动 | 先纠正 1 条（`Quest UI Trigger` 有本地播放点）⇒ **剩 43 条**；五项本地证据全 0 ⇒ **判据为空**（远端不可证实/否证） |

## 二、触发链最终计数（现读）

`判据/B9_特效触发链.tsv` 共 45 条数据行：**已接 30 / 未接 1 / 判不了 0 / 原版无 14**。

订正来源（全部 2026-10-31）：
- `B104_VfxMap`：第 14 行 `CancelAnimsFromUnit` → 原版无 · 第 16 行 `RemoveDestroyableAnims` → 已接 · 第 45 行 `Ambush End(1)` → 原版无。
- `B104_VFX判据`：第 18 行 `StartTargetedSpellAnim` → 原版无 · 第 27 行 `SetAsMulliganSelected` → 已接 · 第 46 行 `genericBattleAnims` → 原版无。
- `B104_V1V3` 静态检查：第 20 行 `ResolveBloodThirstAnim` 落点改为 `BattleDriver.cs:37274 PlaySw34BloodThirstHighlight480`（`:23008` 接线），原落点符号 `HighlightBloodThirst` 只存在于注释。

## 三、怎么查的

- UnityPy 1.25.3 读 **PC 84 包**（`d:/2/10.13游戏/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64`）与**安卓 1.35.0 卡 SO 缓存**；`assets_full` 只作旁证。
- 反编译：`decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp`。
- `catalog_main.json` 的 base64 key data 解码。
- 全程**按 pid / GUID 追**，⛔ 未用「按名字搜不到」当否定。

## 四、未做 / 判不了

1. 43 条远端驱动：本地五项证据全 0，远端包未下载 ⇒ 判据为空。
2. `StopTime=1`（46 条 legacy）是否截断：要 Unity 判；`Card Explosion` 运行期桥接同样只做静态读。
3. 47 条曲线对账未含 10-27 新增 3 条与 7 条 `ArenaOriginalPrefabs`。

## 五、覆盖边界

① PC 无卡 SO 包，卡 SO 唯一来源是安卓缓存；② 安卓缓存只有 3 个包；③ 远端 bundle 未下载、catalog 无 asset 级映射；④ 未跑 Unity。
