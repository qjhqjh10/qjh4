# E 组根因普查（2026-09-18）· 汇总

> **产出口**：6 个子代理并行跑的，逐块明细在 `资料/普查产出_0918/E组根因_B*.md`（**各自带逐效果表**）。
> 本文只做**合并**：跨块收敛的根因 · 与旧表的冲突 · 下一步。
> 输入台账：`资料/特效还原台账.tsv`（E 组 242 条 = 偏亮 169 / 偏暗 73）·
> 清单 `_tmp_view/E0918/B*.tsv`（切块脚本见下）· 技术构成 `资料/比对基线/per_effect_tech.json`。

## 〇、🔴 两条**先读**的前提（不读会白干）

1. **本文所有数字描述的是「重导前」的资产**：亮度数字来自 **2026-09-15 20:48–20:51** 的 sweep，
   而 `Assets/WarpforgeVFX/{Prefabs,Materials}` 是 **2026-09-17 23:43** 全量重导的，
   `WarpforgeShaderMap` 的 21 个 `UseOriginal` 白名单是 **2026-09-18 18:36** 才加的。
   ⇒ **「E 组现在是多少」全项目没有数字**。已启动重扫（`WFSWEEP_SIDE=orig` 一趟 → 不设再一趟 → `analyze_sweep.py`），
   **重扫后本文的锅类计数要按新台账重核**。
2. **尺子本身也有两处已知不干净**（都会把结论带偏，已分别量过）：
   - **原版侧随跑法变**：同一效果逐时点最大偏差 20.9%（`StrikeEffect` 单跑 437 / 小批 344 / 全量 435），
     疑似**全局 shader 关键字泄漏**（进程级状态）。
   - **🔴 抓屏那一族是「尺子被换掉」不是「资产退步」**（B3 块实测，见 §二 锅 R）。

## 一、跨块收敛的头号根因：**锅 H₁ —— 我们自建 shader 多了一个原版没有的属性**

**四块独立收敛到它**（B1a / B1b / B2，以及 B3 的对照）：
`WarpforgeVFX/Particles/Extra Color`（替代 `Everguild/FX/Extra Color`）里声明了
`[HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)`，并在 frag 里
`col.rgb += tex.rgb * _EmissionColor.rgb * IN.color.rgb`。

🔴 **原版那张属性表里根本没有 `_EmissionColor`** —— 21 项，逐字见
`资料/普查产出_0917/shader属性表_块1.md:278`（2026-09-18 主对话**亲读复核**过）。
⇒ 这个属性是**我们凭空加的**，而 `CLAUDE.md` 三 那条规矩正是「**属性表里没有的，一律硬编码**」——
这次不是「把死值当活值用」，是**反过来**加了一个原版不存在的活值。
🔴 **默认值是白**：def 里没记这个属性的材质会**吃默认白** ⇒ 那行加法**多加一整份贴图（≈2×）**。

| 块 | 命中 | 判据 |
|---|---|---|
| **B1a**（纯精灵图·最差一半 66） | **52** | 本块 50 份 def 的 `_EmissionColor` **50/50 全是 `(1,1,1,1)`**，而同名「原版 shader 那份」散在 0/<1/>1 ⇒ **那个 1 是默认值、不是原版值**（样本 `Prefabs/Buff_Mana.prefab` def0 vs def2） |
| **B1b**（纯精灵图·其余 66） | **31** | 原版属性表 21 项没有它（`块1.md:278`）；实测 `Spark`=137 · `Smoke Sprite Sheet Extra Additive`=26.6 · `Iron_Halo*_add`=4.62 |
| **B2**（Matcap 42） | **30 = 全部偏亮条** | 原版 `m_ParsedForm.m_PropInfo` 属性名集合 − 我们 shader 的属性名集合 ⇒ 多出的名字在材质/默认值上非零 |

**影响面**：`Everguild/FX/Extra Color` 一家覆盖 **741 条效果**，`WarpforgeShaderMap.Replacements` 里 25 个名字都指向它。

⚠️ **别当成 2026-09-15 那次的回退**：那次只把 `col = tex*_Color*_EmissionColor*IN.color`（**乘法**）
改成**加法**，**加法项本身没删** —— 也正是「**已修的锅没让 E 组变小**」的一条实证
（旧台账 176 → 现 241 那个对比本身也不可信，见 §〇 前提 1）。
⚠️ **不能拍脑袋删**：09-13 当初加它，是为了修**另一批 11 条偏暗**（`WFParticlesExtraColor.shader:16-22` 的旧注释
—— 那条注释**写着「原版有 `_EmissionColor`」是错的**，主对话 2026-09-18 已就地更正）。
⇒ **按重扫后的新台账定**，两边都有实测才算数。

**对称的另一半 · 锅 H₂（偏暗）**：我们导出器**剔掉了原版的 `_EMISSION` 关键字**
（`EffectExporter.cs:374-391` 一律剔），而那批原版材质**带** `_EMISSION` ⇒ 我们少一层发光。
B1b 量到 **29 条**，期望比值 `1/1.5 = 0.667`、实测中位 **0.63** ✓。
⚠️ **与 09-11 的一条旧裁定冲突**（`EffectExporter.cs:378-383` 记着「剔掉 `_EMISSION`，12 个效果 L1 0.11→0.0003」）
⇒ **必须先复测再动**（B1b 给了最便宜的做法：只挑 `Buff_Orange` 单跑两趟小扫；⚠️ 小批会整份覆盖 `sweep_*.tsv`，**先备份**）。

> ⚠️ **锅名撞车**：B1b 把「偏暗（剔 `_EMISSION`）」叫锅 **H**、「偏亮（多属性）」叫锅 **I**；
> B2 把「偏亮」叫锅 **H**。**本文统一成 `H₁ = 偏亮（多属性）` / `H₂ = 偏暗（剔关键字）`**，
> 引用分块文档时注意对号。

## 二、其余锅类（按「能不能马上动手」排）

| 锅 | 条数 | 是什么 | 能不能马上修 |
|---|---|---|---|
| **R · 尺子被换掉**（B3 新增） | **14** | **抓屏修正**让 `_GrabPassTransparent` 真绑上后，**原版那一侧的假灰块消失** ⇒ 抓屏族 233 个 prefab 的**原版**亮度和 **−50%**（同窗口对照组 724 个只动 **−0.1%**）⇒ 整族 `exp/orig` 从 **0.524 翻到 1.228**，判定从「偏暗 24 / 偏亮 3」变成「偏亮 19 / 偏暗 5」 | ❌ **不是资产问题，别修资产**。而且 `EffectSweepBatch.RenderAt()` 每时刻只渲**一帧**，那个 Feature 的设计是**抓上一帧** ⇒ 在扫描里抓的是**另一个时刻/另一个效果** ⇒ **要判这一族得先在扫描进程里关掉它重扫** |
| **C1′ · 近似替代没做溶解**（B5 新增） | **9** | 用到 `Shader Graphs/Fx_ParticleDissolve_apb`/`Fx_RockDissolve` 的效果只映射到**近似替代**、**溶解/遮罩/顶点流没做** ⇒ 覆盖率 3–7 倍 + 尾部拖到 1.00s。硬判据：用这组的 14 条效果 **E 率 57%**，基线 33%，而「两张表都没有」的 A 组 82 条只有 **26%** | ✅ **能修**（改一处 def + 重扫验证「E 组会不会掉」）—— **这正是「C 组变 E 组」的那一笔** |
| **G · Matcap** | **11**（B2） | 拆三句：①**属性表缺口**有铁证（原版 `Matcap Full Options` **35** 属性 vs 我们 **25**，缺的 12 个已列）②关键字**不是没搬**（B2 第一版判错、已留更正痕迹；真失效点是 `_USEEMISSION` 我们没有分支 + 我们写成 `_APPLYAMBIENTCOLOR_ON` 而原版是 `_APPLYAMBIENTCOLOR`）③**`×_MatCap` 导致系统性变暗**方向对、量级对、**机制未证** | ⚠️ 先做**判别实验**：把 `_MatCap` 换成 white 单跑这 42 条看 `\|ln\|` 收不收紧 |
| **A′ · 贴图名对不上** | **9**（B1a）/ **3**（B46） | def 的 `texNames` 只有 `_Disolve`/`_SecondaryTex`（**没有 `_MainTex`**）⇒ 白面片；B46 那一族真因是贴图叫 **`_MainTexture`** 而自建 shader 只声明 `_MainTex` ⇒ **`WarpforgeEffectBinder.cs:117` 静默不设** ⇒ 实心拖尾（全库 6 条） | ✅ 09-18 白名单**已改走原件**，但**从未测过** ⇒ 重扫后应自动收账 |
| **T · 时序包络** | **10**（B1a） | `r_peak` 落在 0.7–1.4（峰值那帧本来是对的）而 spread ≥0.9（`Buff_Mana` 前段 10× / 峰值 3.3×）。**只调亮度倍数会把对的那段改坏** | ⚠️ 要逐模块比（工具 `ParticleModuleProbe` 现成） |
| **J · 时序/寿命** | **3**（B5） | 两侧有内容时段不同：`HeroOfTheEmpire OLD` 1.5s 原版 1922/导出 407 · `ArdAsNailsEffect` 607/15 · `ConcussiveEffect` 导出多播 0.85s ⇒ 根因单一（`destroyTime`/`DoDestroyInTime` 收尾口径） | ✅ 能修，修对是全库收益 |
| **B/E′ · 混合值** | **9**（B5） | `InferBlend` 按 shader 名推混合，对 `Everguild/FX/FX Shine For Animation` / `Particle Shine Custom Vertex Streams` **返回 null** ⇒ 落在自建 shader 默认值上。最干净样本 `BloodThirstEffect_Gain`：亮点数几乎相等（差 ≤5%）而亮度和 1.4–1.9 倍 | ✅ 能修 |
| **D · 判不出** | 各处合计 | 见各块文末 | ⚠️ 要实拍 / 逐 prefab 比 |

## 三、🔴 与旧表的冲突（**旧锅名单有几条是假的，别照着修**）

`资料/特效E组_逐效果根因表.md`（2026-09-13，按**更旧**的台账分的组）里的：

| 旧账 | 实测结论 | 出处 |
|---|---|---|
| 锅 **A**（`trailMaterial` 绑到贴图全丢的 def，35 条） | **已不成立**：trail 槽指向的 def **0/66 缺 `_MainTex`**（B1a）· **23/23 都有值**（B1b）· 全库 523 处拖尾槽零贴图 **0 处**（B46）。旧账说的「42 个空 texNames」是导出器自己造的**同名兄弟 def**、会被 `Apply()` 覆盖回去 | B1a/B1b/B46 各自逐条查过 |
| 锅 **F**（prefab 发射器不画，16 条） | **在本块是假的**：那些 `m_RenderMode:5 / m_Mesh:{fileID:0}` 渲染器是**同一 prefab 里 `m_Enabled:0` 的禁用根渲染器**（`m_Materials` 空），本来就不画。另：`ShapeModule.m_Mesh: {fileID: 10207}` 是**内建发射形状网格**、不是丢件 | B5 · B1b |
| 「抓屏一个 Feature 覆盖 41 条」记成**已收账**（旧表 `:161`） | **方向反了**：那不是收账，是**换尺子**（见 §二 锅 R） | B3 |

## 四、🔴 两条「尺子」更正（用别的工具时别再被坑）

1. **`效果库报告.tsv` 的「判定 / 亮度比」两列不是当前台账** —— 那是 `WarpforgeEffectLibrary` 里
   旧的**单帧 1.2s** 指标（`EffectLibraryBuilder.cs:147,153`）。B5 块 **32 条里 20 条方向都不同**
   （`BlastEffect` 旧 `E- 0.25` 偏暗 vs 8 时刻 sweep 的 `6.13` 偏亮）。
   ⇒ **一律以 8 时刻 sweep 为准**。
2. **旧表把「一个 Renderer Feature 覆盖全部 41 条」记成已收账** —— 见上表。

## 五、下一步（按顺序）

1. **重扫**（已启动）→ 出台账 → **按新台账重核本文所有计数**。
2. **锅 H₁**（自建 Extra Color 多属 `_EmissionColor`）：先看新台账里那 741 条效果的方向，
   再决定「删加法项 + 默认值改黑」还是保留（09-13 那 11 条偏暗要一起看）。
3. **锅 R**：把 `GrabPassTransparentFeature` 在**扫描进程里**关掉重扫一次，那 14 条才有可解释的判定。
4. **锅 C1′**（9 条）：改那一处 def，重扫看 E 组掉不掉。
5. **锅 G**：`_MatCap` 换 white 的判别实验（42 条单跑）。

## 六、怎么复跑

```bash
# 切块（脚本口径：按台账 技术构成 分桶，B1 再按 |ln| 切一半）
#   产物 d:/4/_tmp_view/E0918/B*.tsv，本轮用的桶：
#   B1a/B1b(纯精灵图 66+66) · B2(Matcap 42) · B3(抓屏扭曲 24) · B5(Mesh粒子 32) · B46(拖尾+杂项 12)
# 重扫（⚠️ 两趟分进程、顺序不能反；跑前先 cp 备份 sweep_*.tsv —— EffectSweepBatch.cs:193/198 是 File.WriteAllText）
WFSWEEP_SIDE=orig "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_o.log"
"$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_e.log"
"D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/analyze_sweep.py"
```

## 七、出处

- 六份逐块明细（**逐效果一行一条**）：`资料/普查产出_0918/E组根因_B1a.md` · `_B1b.md` · `_B2.md` · `_B3.md` · `_B5.md` · `_B46.md`
- 原版属性表：`资料/普查产出_0917/shader属性表_块1.md:278`（`Everguild/FX/Extra Color` 21 项）
- 自建 shader：`Assets/WarpforgeVFX/Shaders/WFParticlesExtraColor.shader`（`_EmissionColor` 声明与注释）· `WFMatcap.shader`
- 旧表（**有假锅，见 §三**）：`资料/特效E组_逐效果根因表.md`
- 台账与切块清单：`资料/特效还原台账.tsv` · `_tmp_view/E0918/B*.tsv`
