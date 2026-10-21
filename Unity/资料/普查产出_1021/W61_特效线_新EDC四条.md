# W61 特效线 · 新 E/C/D 四条（只读诊断）

> 代理 O · 2026-10-21 第六十一会话。只读；未改任何 `.cs` / 资产 / 台账。子代理无写盘能力，**正文由主对话原样落盘**。

## 0. 一句话结论

- **Tidewall Gunrig（C）= 假阳性**：`LIT_MIN=60` 稀疏阈值假象；同一份数据在 1021 normal 跑是 **Z**，最新跑是 **C**，两侧逐时点 `sum` 几乎逐位相同，亮度比中位 1.00–1.045。
- **BulletTrail_Single（D）= 假阳性**：原版峰值亮点 **59**，比 `LIT_MIN=60` 少 1 个像素就被判「原版空」；两侧 0.30–1.00s 都有内容，亮度比中位 1.045。
- **EnvironmentalCondition Sororitas Raging Storm（E）= 判据未闭合**：`尺子不稳定名单.tsv` 第 1 名（最大 `Δ|ln|` 0.4120，历史上判过「偏暗↔对得上」），当前 `|ln|` 0.425 已在噪声上限；1021 受控 A/B 指向「走原件」侧（normal 0.349 → builtin 0.031），但最新跑 prefab 已重导、orig 侧尾部也变了，E 的幅度不能归给单一资产。
- **ShieldTraitEffect_Idle Necron Variant（E）= 判据未闭合**：同一 prefab 在 1021 跑是 **Z 1.183**，最新跑是 **E 1.424**；TAU 1.37 / Necron 1.42 只差 3.9%，一带之隔。另发现一个**已证实**的导出缺陷：`Lightning Burst` 的 `ShapeModule.m_Mesh` 是 guid 全 0；但它只影响 0.15–0.5s，解释不了 2–3s 的 1.8–1.9×。

**净结论：四条里 0 条可直接判为「缺内容/丢效果」缺陷；C/D 是尺子假阳性；两条 E 都落在 `|ln|≲0.6` 分不出真假的区。**

## 1. 数据口径（关键）

- 当前 `特效还原台账.tsv`（mtime 22:00）来自 **`sweep_orig.tsv` + `sweep_exp.tsv`**（21:57/22:00，`WFSWEEP_SIDE=both` 新一趟，960 目标）。
- `sweep_{orig,exp_normal,exp_builtin}_1021.tsv` 是 **18:45 的第一趟**，对应 1021 账 Z699/W254/E5/D2/C0；**不是当前四行的来源**。
- `analyze_sweep.py` 固定读 `sweep_orig.tsv` + `sweep_exp.tsv`（`:51-53`）。
- 阈值：`LIT_MIN=60`（`:62`）；亮度带 0.7–1.4（`:69-70,183-188`）；`|ln|≲0.6` 的 E 分不出真假（`:260`）；E 计数同配置 ±1（`:255`）。

## 2. 逐条表

| 效果 | 根因 / 性质 | 证据 | 下一步 | 闭合 |
|---|---|---|---|---|
| **Tidewall Gunrig** | **C 是稀疏阈值假象**：`lit>=60` 把 59 当空；两侧都有内容，`sum` 逐位相同，亮度比 1.00–1.045 | `特效还原台账.tsv:2`；`sweep_orig.tsv:7258-7265`（0.30: 62/22）；`sweep_exp.tsv`（59/22）；`sweep_exp_normal_1021.tsv`（63/23→Z）；`analyze_sweep.py:62,162-175` | 判 **Z**；不重导/不改绑定/不补资产；按「低置信度稀疏假象」处理 | **闭合（假阳性）** |
| **BulletTrail_Single** | **D 是稀疏阈值假象**：原版峰值 `lit=59`，差 1 像素被判「原版空」；导出 62；两侧 0.30–1.00s 都有拖尾，亮度比 1.045 | `特效还原台账.tsv:3`；`sweep_orig.tsv:3082-3089`（0.75: 59/18）；`sweep_exp.tsv`（62/19） | 判 **Z**；不重导；如要视觉留证可跑 `EffectIso` 0.75s 单帧 | **闭合（假阳性）** |
| **Raging Storm** | **判据未闭合**：0.15–0.50s 比值 0.97–0.98（对得上）；0.75–3.00s 0.55–0.71（偏暗）。尾部由 4× `Smoke Traily Background` 主导（length 5.0/looping/prewarm），材质 `Sand Storm`→`Everguild/FX/Alpha Mask One Layer`，在 `UseOriginal` 白名单。1021 A/B：normal 0.349 → builtin **0.031**（偏离在走原件侧）。但该效果是不稳定名单第 1 名（0.4120），当前 `|ln|` 0.425 在噪声上限；21:54 又重导过 prefab，orig 侧尾部 64→86 | `特效还原台账.tsv:4`；两条 sweep 的 `:4018-4025`；`W60_真机与Unity_结果.md:49-51`；`尺子不稳定名单.tsv:2`；`WarpforgeShaderMap.cs:78,269`；原版 `ParticleSystem_7241470991743629147.json` | 单效果重扫两遍；`WFBIND_FORCE_BUILTIN="Alpha Mask One Layer"` 与 `"Extra Color"` **分开** A/B；⛔ 不全局撤白名单（W60 已证 78 条退步） | **未闭合**：缺单效果两跑 + 单 shader A/B |
| **ShieldTraitEffect_Idle Necron Variant** | **判据未闭合**：同一 prefab（mtime 10-10，未重导）1021 跑 1.183（Z）、最新跑 1.424（E）；orig 侧几乎不变、exp 侧 0.15→3.0s 变亮 7–37%；TAU 1.37 vs Necron 1.42 只差 3.9%。**已证实导出缺陷**：`Shield Exit/Lightning Burst` 的 `ShapeModule.m_Mesh` guid 全 0（`EffectExporter` 只导 `MeshFilter.m_Mesh`/`PSR.mesh`/SMR.sharedMesh，未导 ShapeModule），但该 PS 非循环 0.5s、粒子寿命 0.1s，只影响 0.15–0.5s，解释不了 2–3s 的 1.8–1.9× | `特效还原台账.tsv:5`；`sweep_*:6498-6505`；`ShieldTraitEffect_Idle Necron Variant.prefab:15473`；原版 `ParticleSystem_2564568966103877692.json:419`；`EffectExporter.cs:1846-1863`；`WarpforgeEffectBinder.cs:433-434` | (a) 导出器补 `ShapeModule.m_Mesh`，重导 5 个 ShieldTraitEffect_Idle 变体；(b) 单效果两跑 + `WFBIND_FORCE_BUILTIN=1`/`NO_PROP_FILTER`/`NOQUEUE` A/B；(c) 仍 >1.4 再按材质槽分 | **未闭合**：缺 ShapeModule 修复后重导 + 单效果 A/B |

## 3. 与噪声底/不稳定名单

- `|ln|≲0.6` 的 E 分不出真假（`准则与做法.md:133`；`analyze_sweep.py:260`）。
- 全池 733 个可比效果两次同配置跑：`|Δ|ln||` 中位/90 分位 0.0000、99 分位 0.09、**最大 0.412**；判定会变的只有 1 条卡在阈值上（`:251-257`）。
- **Tidewall / BulletTrail** 不在不稳定名单（名单跟踪 `|Δ|ln||`），但 `LIT_MIN=60` 是另一把独立的刀口，噪声统计不覆盖它 ⇒ C/D 计数也会 ±1，且与 E 计数独立。
- **Necron**：名单记录 0.09，但当前两跑 `|ln|` 0.168→0.353（Δ 0.185，是名单值的 2 倍）——名单是历史两跑，不是充分统计。

## 4. 建议动作

- **重导**：Necron（先补 ShapeModule.m_Mesh，再 `RunListed` 5 个变体）；Raging Storm **暂不重导**（21:54 刚重导）；Tidewall/BulletTrail **不重导**。
- **改绑定**：Raging Storm 只做目标化 A/B；Necron 先不改变绑定，用三个 env 做诊断 A/B；Tidewall/BulletTrail 无绑定可改。
- **补资产**：**无**。`Shield Ico` 网格已在（187 顶点与原版对上），缺的是把 mesh 赋给 `ShapeModule.m_Mesh`。
- **判原版如此**：Tidewall、BulletTrail → Z；Raging Storm / Necron → 判据未闭合。
- **可选修尺子**（工具线，单独开账）：`analyze_sweep.py` 对稀疏效果（峰值 <100 或 lit 在 LIT_MIN 附近）不要直接判 C/D；需全量重算 + 逐条 diff。

## 5. 没查清

1. Raging Storm：走原件的 `Alpha Mask One Layer` 为什么暗（缺单材质 A/B + `NO_PROP_FILTER` + 逐像素）。
2. Necron：同一 prefab 两跑 exp 侧变亮 7–37%（缺单效果两跑 + `WFSWEEP_GLOBALS=1` + 重复）。
3. Necron：null ShapeModule mesh 的退化行为与亮度影响（缺补导入后重导 + 隔离 A/B）。
4. Tidewall/BulletTrail：`LIT_MIN` 重标定副作用（缺全量 rescore + diff）。
5. 口径：当前四行以 21:57/22:00 那趟为准；1021 文件（18:45）下这两条 E 不存在。
