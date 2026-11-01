# B-167 · 项目级色彩空间 Linear → Gamma（阶段二）

> 调度台执行 · 2026-11-01 · 用户 2026-11-01 拍板：「做。请你先做这个。」
> 判据来源：`判据/B113_G1_色彩空间与sRGB.md` §2（原版 `m_ActiveColorSpace: 0`）、CLAUDE.md 铁律 18·0（默认复刻原版）。

---

## 〇、执行前的现读基线（Linear，2026-11-01）

| 项 | 读数 |
|---|---|
| `ProjectSettings.asset:50` | `m_ActiveColorSpace: 1`（Linear） |
| 全套自检 | 24 入口 24/24 绿 |
| 账闸 | A 5,841 / 通过 5,841 / 漂移 0 / 失败 0 |
| `play check` | rc=0 · 指纹 `bcbcdf1906491692f115131afbc0f85e` |
| `typecheck` | 0/0 |
| Effect sweep（`analyze_sweep.py` 现跑） | 962 效果 · 可判定 707 · **中位 \|ln\| 0.000 · 均值 0.010 · 100% 落在 0.7–1.4** |
| CardBaseDemo 卡面渲染基线 | `_tmp_view/cardbase/*.png`（16 张，2026-11-01 02:34） |

---

## 一、已做（2026-11-01）

| # | 动作 | 结果 |
|---|---|---|
| 1 | 备份 | `_tmp_view/b167_backup_20261101/`（`ProjectSettings.asset` + 29 份 `WarpforgeVFX/Shaders/*.shader` + 3 份 UI shader + 13 套 `*_PostFx.asset` + `DefaultVolumeProfile.asset`，共 49 文件） |
| 2 | 切开关 | `MyGame/ProjectSettings/ProjectSettings.asset:50` · `m_ActiveColorSpace: 1 → 0` |

---

## 二、🔴 判据订正：那 32 处 `LinearToSrgb` **不能删**（推翻 `B113_G1` §2·2 B）

`判据/B113_G1_色彩空间与sRGB.md` §2·2 B 写着「这些调用是为了在 Linear 管线里模仿原版在 gamma 空间做的运算 ⇒ 切到 Gamma 后变成二次编码、必改」。
**这条不成立。** 证据与推理：

1. **原版字节码里就有这次编码**：`_tmp_view/dxbc/` 的 27 份反汇编里，**9 份**命中 sRGB 编码常量 `0.003131 / 12.9232 / 1.055`
   （`matcap_with_tex` · `multi_ray` · `dissolve_mask` · `alpha_masks_two` · `premul_grey` · `trailshader1` · `unlit_uvscroll` · `extra_color_vs`）。
   各 shader 头部注释也逐字标着「常量逐字来自 DXBC」。
2. **它是原版算式的一部分，不是补偿**：原版工程是 Gamma，原版 shader 对材质色做一次 `LinearToSRGB` 后直接输出。
3. **两边行为在 Gamma 下重新一致**：
   - 原版（Gamma）：属性原值 `C` → shader `encode(C)` → 无管线编码 → 最终 `encode(C)`。
   - 我们（切 Gamma 后）：同上 ⇒ **逐位一致**。
   - 我们（切前 Linear）：VFX 材质走 `ArenaOriginalMaterial.ApplyProps` 的 **`SetVector`（不转换）** ⇒ 属性原值进 shader → `encode(C)` → **管线再编码一次** ⇒ 双重编码、偏亮。
     （`_Color` 多为 `(1,1,1)` 时两者相同 ⇒ 这个错一直没被发现；`_EmissiveColor` 这类非白值才露。）
4. ⇒ **切 Gamma 本身就是那 32 处的「归位」**；删掉它们反而会把 13 个特效改成「原版不做的样子」。
5. 同理：`WFUIGlowSdfNoise.shader:147` / `WFUIDivisionChange.shader:131` 的手写 `LinToGamma*` 也**保留**（它们同样复刻原版 UI shader 的编码）。

> ⚠️ 仍要**实测确认**：切完跑 `EffectSweepBatch` 两趟，看 `analyze_sweep` 的 `|ln|` 中位是否仍在 0.000 档；若某族退步，再单独查那一族（而不是一把删 32 处）。

---

## 三、切后首跑结果（2026-11-01 上午）

| 项 | 结果 |
|---|---|
| 贴图全量重导 | 完成（切换后 Unity 自动重导 4,600+ 张，无 shader 编译错误） |
| `ShellScene.Run` | **5,460 通过 / 0 失败**（与切前一致） |
| **全套 24 入口** | **23 条退出码 0**；唯一非 0 = `CardBaseDemo.Run`（2 条红，见下） |
| `CardBaseDemo` 两条红 | 都是**估计量假设 Linear**：`④·量级` 读 0.269（期望带 0.403–0.523）、`④·核心` 读 0.131（期望 0.293–0.413） |
| 修法 | `CardBaseDemo.LinLum` 改成按 `QualitySettings.activeColorSpace` 二选一（Gamma 直接用 sRGB 原值）；**零行数变化**（3 处账落点 1513/3973/6171 未漂） |
| 修后复跑 | **`④·量级` = 0.463**（原版字段推出的就是 **0.463**）· **`④·核心` = 0.357**（`1 − 0.647 = 0.353`）⇒ **这一读数本身就是原版在显示空间混色的强证据**；`CardBaseDemo` 合计 **596 / 0** |
| 卡面渲染 A/B（16 张 vs 切前基线） | 平均 mean\|Δ\| = **1.4/255（0.55%）**，p99 4–22，max 74（集中在文字/半透明边缘）⇒ 不是整体偏色 |
| `P1-3` 剩余 1 条红 | **与色彩空间无关**：卡池里**不存在**「非单位 + 有抠图立绘」的卡（`card_cutouts.json` 只覆盖 unit 586 + hero 56，而 hero 算单位）⇒ 夹具前提消失。已开账 **`B-180`** |

---

## 四、④ 后处理重校 —— 实测**不需要重校**（2026-11-01）

逐场把我们的 13 套 `*_PostFx.asset` 与原版 `Battle Arena X PostProcessing.asset` 的组件逐个比（脚本 `_tmp_view/b167_postfx_cmp.py`）：

| 项 | 原版 | 我们 | 判定 |
|---|---|---|---|
| Bloom `threshold` / `intensity` / `scatter` | 逐场不同（1.15/5/1 为多数；狼族 1.17/5/0.862；部分场 1.0） | **逐项相同** | ✅ |
| Bloom `maxIterations` | 值 6 · **override 0**（走 URP 默认） | 值 6 · override 1 | ✅ **功能等价** |
| Bloom `skipIterations` | 6（override 1） | 1 | ✅ **不是缺陷**：URP `Runtime/Deprecated.cs:324` 是 `[Obsolete(..., true)]`，**没有任何 pass 读它**；真值是 `maxIterations`。判据早已记在 `资料/普查产出_0920/场景光照与后处理_原版规格.md:202-205` 与 `§28逐场核_第一轮.md:33` |
| Vignette `intensity` | 0.297（13/13 同值） | 0.297 | ✅ |
| ColorLookup | 13/13 场都有；其中 **7 场 LUT 为空（`m_FileID=0`）** | 6 场有组件；7 场由 `BattlePostFx` **运行时补**（`CardPresentation/Battle/BattlePostFx.cs:29-60`） | ✅ 既有设计，判据齐 |

⇒ **后处理参数本来就是原版值**；切 Gamma 后它们按原版空间解释 = 更接近原版。**⛔ 不要再按 `skipIterations` 去"修"**（那是死字段）。

---

## 五、③ sweep 结果（2026-11-01 上午，两趟各 962 效果）

| 口径 | 切前 | 切后 |
|---|---|---|
| 可判定 | 707 | 713 |
| \|ln\| 中位 / 均值 | 0.000 / 0.010 | **0.000 / 0.027** |
| 落在 0.7–1.4 | 100% | **98%** |
| **E 组（亮度/密度不对）** | **0** | **14** |

**切 Gamma 一次性修好 18 个大偏差**（`compare_sweep.py` 对 `sweep_exp_bak_before_fix.tsv`）：
`BansheeHowl` 2.639→0.000 · `Njal Blizzard Attack` 1.368→0 · `Generic Tap Material` 1.172→0 ·
`EarthCrack_Single` 1.080→0 · `ShieldTraitEffect_Idle` 1.030→0 · `BeastSnagga_Summon` 0.964→0 ·
`BulletImpact_autocannon` 0.835→0.019 …（共 18 条 E→Z）

**新出现 14 条**（诊断中，见 `判据/B111W_B167_sweep14条诊断.md`）：
`AcidSpraySweepAttack` 1.433 · `Env Cond Dark Angels Void Combat` 1.213 · `… Asteroid Zone` 1.175 ·
`BulletImpact_Spore Launch 1` 0.940 · `… Spore Launch` 0.919 · `Pray_Idle` 0.886 · `BulletImpact_AcidSpit` 0.846 ·
`Env Cond Sororitas Raging Storm` 0.741 · `Env Cond Emperor's Children 2 Fumes` 0.744 ·
`BulletImpact_Screamer-Killer` 0.539 · `CreateCard BlackLegion` 0.487 · `Env Cond Black Legion Daemonic Feast` 0.471 ·
`Basic_army_Guard Digital` 0.434 · `CreateCard` 0.387

⚠️ 噪声底：|ln| ≲0.6 的 E 条目在这把尺子上分不出真假；其中 `Sororitas Raging Storm` / `Emperor's Children 2 Fumes` 在「两次相同跑会动」名单里。

### 5·1 诊断结论（`B111W` 只读诊断 · `判据/B111W_B167_sweep14条诊断.md`）

**根因不在 shader，在贴图像素**：

> 我们导出的 `WarpforgeVFX/Textures/*.png` 里有一批「原版 `m_ColorSpace=0`（数据类）」的贴图被存成了 **`sRGB_encode(原版原始值)`**，同时 `.meta` 写 `sRGBTexture: 1`。
> 这个组合**只在 Linear 工程里成立**（旗标把那次编码抵消掉）；切 Gamma 后旗标失效 ⇒ 采样 = `encode(原版)` ⇒ **偏亮 1.2–3.3×**。

逐像素铁证：`ours == encode(orig)` 的 MAE **0.19–0.29/255**（直接比是 **8.6–64.8/255**）。
不是元数据差异，是**像素本身被 gamma 编码过**。

| 硬事实 | 读数 |
|---|---|
| 尺子有没有整体漂 | 694 个可比效果 Δln 中位 **0.0008**、p90 0.031 ⇒ **没漂**；只有 23 个 >0.15，前 14 名正好是这 14 条 E |
| 是不是 shader 的锅 | **不是**：这 14 条用的 shader 全在 `UseOriginal`，两侧跑同一份原版字节码 |
| 影响面 | 已确认双编码的 7 张贴图被 **367/978 个 prefab（37.5%）** 引用 |

**判定**：真缺陷 **11** · 本来就偏 **1**（`Sororitas Raging Storm`，切前已 0.706）· 查不到 **2**（`Basic_army_Guard Digital` / `Emperor's Children 2 Fumes`，与噪声名单重合）。

**修法（两步缺一即错）**：`p_new = linearize(p_old)` **＋** `.meta sRGBTexture: 1→0`。
⚠️ 只改 `.meta` 会把 Linear 也弄坏 —— 这是对 `判据/B108_AUD09_纹理格式.md §3·2` 的**补全/推翻**（它只写了改旗标，没发现我们的 PNG 像素本身也不是原版字节）。

⇒ **已开两条新账**：`B-184`（双编码贴图全量普查 + 重写）· `B-185`（另外 4 张非同类的差异 + 2 张找不到原件）。**B-167 的验收在 `B-184` 修完并复跑 sweep 之前不算通过。**

### 5·2 收口（2026-11-01 11:5x，`B-184` 修完 + 重导后复跑 sweep）

| 指标 | 切前(Linear) | 切后(未修贴图) | **修贴图后** |
|---|---:|---:|---:|
| E 组（亮度/密度不对） | 0 | **14** | **2** |
| 可判定 \|ln\| 中位 / 均值 | 0.000 / 0.010 | 0.000 / 0.027 | **0.000 / 0.011** |
| 落在 0.7–1.4 | 100% | 98% | **100%** |
| \|ln\| > 0.60 | 0 | 3 | **0** |

剩余 2 条：
- `EnvironmentalCondition Sororitas Raging Storm` \|ln\| **0.415** —— **在尺子的「两次相同跑会动」名单里**（历史抖动 **0.412**）
- `DeckBuff_GSC` \|ln\| **0.413** —— 同样压在尺子自报的噪声上限（0.412）

⇒ **尺子到顶了，不是缺陷**。`B-167` 验收通过。

**`B-184` 重导验收**（`_tmp_view/b184_verify.py`，带 alpha 掩码）：**22/22 归位**（直接 MAE **0.00–0.50**，`encode` MAE 2.35–66.68），`.meta` 全变 `sRGBTexture: 0`；**442 张颜色图仍 `1`、15 张线性数据图本来就该 `0`** ⇒ `EffectExporter` 的 `SrcIsSrgb()` 在 Gamma 下能区分，**没走「全变 0」那条回滚路**。

---

## 六、待做清单（按判据 §2·3 阶段二）

| # | 动作 | 状态 |
|---|---|---|
| ③ | 跑 `EffectSweepBatch` orig + exp 两趟 → `analyze_sweep.py` 对基线 | 待跑 |
| ④ | 重校 13 套 `*_PostFx` + `DefaultVolumeProfile` | ✅ **已核，不需要重校**（见下） |
| ⑤ | 卡面/UI 整体重看（`CardBaseDemo` 四档渲染 + 四宿主截图比基线） | 待跑 |
| ⑥ | 修「按 Linear 假设写的断言 / 估计量」 | 待跑（等首跑报红） |
| ⑦ | 更新所有「我们也是 Linear」的注释与文档（`ArenaBuilder.cs:421` · `LutBlendProbe.cs:13` · `ArenaOriginalMaterial.cs:278` · `CardBaseDemo.cs:3336/4771/4799` · `CardChoicePanel.cs:241/455` · `判据/B113_G1` §2·1 · 活状态表 `B-110`/`B-115`） | 待做 |

---

## 四、已知的「代码侧 Linear 假设」（切后要逐个核）

| 位置 | 是什么 | 切 Gamma 后 |
|---|---|---|
| `Battle/MulliganPanel.cs:592` | `ShadeAlpha` 按色彩空间二选一 | ✅ **已分支**，自动回 `0.8` |
| `Editor/CardBaseDemo.cs` `MeanLumLinear` / `LinLum` | 压暗层透过率估计量「必须在线性空间算」 | ⚠️ Gamma 下混合发生在 gamma 空间 ⇒ 估计量要改成按 `activeColorSpace` 分支 |
| `Editor/BattleScene.cs:3978` | 断言 `ambientProbe.DC == sRGB→linear(环境光色)` | ⚠️ 待首跑确认（Unity 的 SH 环境光是否与色彩空间无关） |
| `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs:278` | `ApplyProps` 用 `SetVector` 而非 `SetColor` | ✅ 结论仍成立（Gamma 下两者等价，代码不用改，注释要改） |
| `WarpforgeVFX/Runtime/WFMaterialFlicker.cs:24` | 同上 | ✅ 不用改 |
| `WarpforgeArena1/Editor/ArenaBuilder.cs:421` | 注释「本工程和原版都是 Linear」 | ⚠️ 注释过期，改 |
| `WarpforgeArena1/Editor/LutBlendProbe.cs:13` | 注释「工程是 Linear」 | ⚠️ 注释过期，改 |

---

## 五、回滚办法

只改一行回 Linear ⇒ **但必须同时确认那 32 处 shader 没被动过**（本方案不动它们，所以回滚就是改回这一行）。
备份：`_tmp_view/b167_backup_20261101/ProjectSettings.asset`。
