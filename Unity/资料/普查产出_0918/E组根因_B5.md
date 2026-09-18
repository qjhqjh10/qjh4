# E 组根因 · 块 B5「Mesh 粒子」32 条 · 2026-09-18

> 数据源：`资料/比对基线/sweep_orig.tsv` · `sweep_exp.tsv`（2026-09-15 20:51，8 时刻 × 957 效果）
> · `资料/特效还原台账.tsv`（同一次）· `普查产出_0917/效果_shader对账.tsv` · `MyGame/Assets/WarpforgeVFX/{Prefabs,Materials}/` · `效果库报告.tsv`
> 清单：`_tmp_view/E0918/B5_Mesh粒子.tsv`（按 `|ln|` 降序）。**未跑 Unity。**

## 一、一句话结论 + 锅类计数

**本块 32 条不是一个锅：偏亮那一半（22 条）有一半以上（9 条）是「2026-09-15 为了救 C 组而补的近似替代 shader 没做溶解/遮罩/顶点流」——那一次修复把 C 组变成了 E 组，所以「已修的锅」不会让 E 组变小；偏暗那一半（10 条）查不出统一根因，最难的一条线索是一组只在偏暗侧出现的共享材质（`Chestrays` / `Dust` / `line_light` / `DustPuffParticle` / `EnergyShockwave`）。**

| 锅 | 判据（一句话） | 条数 |
|---|---|---|
| **C1′**（新增，见 §一·注） | 用到 §三 B 组 shader（`Shader Graphs/Fx_ParticleDissolve_apb`/`Fx_RockDissolve`），只映射到**近似替代** `WarpforgeVFX/Particles/Extra Color`，溶解没做 ⇒ 粒子不按时消失 | **9** |
| **B/E′** | 覆盖率几乎不变（L 比 1.0–1.2）而亮度和涨 1.5–2.5 倍（S 比）= **同一批像素变亮**，不是几何 | **9** |
| **J**（新增） | 导出/原版**有内容时段**不同（一侧已熄一侧仍亮） | **3** |
| **H**（新增） | 导出侧全程有一个原版没有的巨大面片（亮点 ≥1700 而原版 ≤12，且 sum/亮点 < 0.01） | **1** |
| **D** | 查不出根因，要实拍隔离渲染 | **10** |
| A · F · G · 抓屏 | 本轮**逐条排除**：A 无（无「贴图全丢」的 def）· F 无（见下）· G 无（本块 0 条用 Matcap） | **0** |

> **注 C1′ 的判据**：`WarpforgeShaderMap.cs:121-125` 里这三个 ShaderGraph 是 **2026-09-15 才补的映射**，注释自己写着「⚠️ **近似**（拿不到 ShaderGraph 的属性表，按名字挑最接近的自建 shader）」；同文件 `:89-90` 写「⚠️ **全部是近似**：……（真实的溶解 / UV 滚动 / 顶点流**没做**）」。
> **实测支持**：全 958 条里「用到 §三 B 组 shader」的 14 条中 **8 条是 E（7 偏亮 1 偏暗）**，E 率 57%；而「两张表都没有」的 A 组 82 条 E 率只有 26%、精确映射的 O 组 35% —— **B 组是本块偏亮最高的单因子**。
> 🔴 **锅 F 逐条排除**：旧表把 `Artillery Ground` / `Goff_Rok_Invasion` 记成「`m_RenderMode: 4` 且 `m_Mesh: {fileID: 0}`」。实测那两个 `m_Mesh: 0` 的渲染器是**同一个 prefab 里被禁用的根渲染器**（`m_Enabled: 0`，且 `m_Materials` 为空）—— 本来就不画，**不构成导出缺陷**。证据：`Prefabs/Artillery Ground.prefab`（13 个 PSR，唯一 mesh=0 的是 `Artillery Ground` 节点，enabled=0）· `Prefabs/Goff_Rok_Invasion.prefab`（6 个 PSR，同理）。

---

## 二、逐效果表（32 行）

`症状` = 8 个时刻的**亮点数 `原版/导出`**（0.15 / 0.30 / 0.50 / 0.75 / 1.00 / 1.50 / 2.00 / 3.00 s）；
`L` = 共亮时刻的亮点数比、`S` = 亮度和比、`mp` = 每亮像素亮度 `原版→导出`（三者都出自 sweep 原始列，**判读只用 lit/sum**）。
`材质` = `效果_shader对账.tsv` 的材质数；`⚠` 标出映射有缺口的原版 shader（A=两张表都没 · B=只在运行时表 · D=表里但近似替代）。

| # | 效果名 | 亮/暗 | \|ln\| | 症状（原版/导出 亮点数 × 8 时刻；L·S·mp） | 材质数 / 原版 shader | 锅 | 证据（`文件:行号`） | 置信 | 下一步看哪 |
|---|---|---|---|---|---|---|---|---|---|
| 1 | BlastEffect | 亮 | 1.813 | 192/691 192/1155 216/1613 210/1572 **19/1312** 0/0 0/0 0/0；L 3.60→7.49 · S 4.43→8.84 · mp 0.83→0.76 | 7；⚠B `Shader Graphs/Fx_ParticleDissolve_apb`(`Mat_Fx_ParticleSet_apb`) · ⚠B `Fx_RockDissolve`(`Mat_Fx_Rock`) | **C1′** | `WarpforgeShaderMap.cs`「⚠️ 近似…溶解没做」；台账 `导出多播到 1.00s` | 中高 | 单位 `Mat_Fx_ParticleSet_apb`/`Mat_Fx_Rock` 两个发射器（`EffectIso.Run`），看是不是粒子到寿命末期**不溶解、整块留在画面上** |
| 2 | Invoke Minion Hits Ground Legendary | 亮 | 1.502 | 724/1984 896/3381 1051/4492 809/4140 **173/3449** 2/8 0/0 0/0；L 2.74→19.94 · S 3.28→26.7 | 12；⚠B `Fx_ParticleDissolve_apb`+`Fx_RockDissolve` | **C1′** | 同上；台账 `导出多播到 1.00s`、`单帧旧判定 只有导出有` | 中高 | 同上。⚠️ `效果库报告.tsv` 把这条记成 `Z 1.00` —— 那是**旧单帧 1.2s 指标**，与 8 时刻扫描方向都不同，已按当前台账为准 |
| 3 | Invoke Minion Legendary ALT | 亮 | 1.496 | 760/2078 948/3548 1096/4712 851/4341 **192/3636** 4/7 0/0 0/0；L 2.73→18.94 | 7；⚠B 同两个 ShaderGraph | **C1′** | 同 #1 | 中高 | 同 #1 |
| 4 | Artillery Ground | 亮 | 1.210 | 0/0 0/0 0/0 0/0 **377/1170** 0/0 0/0 0/0；L 3.10 · S 3.35 · mp 1.15→1.24 | 12；⚠B `Fx_ParticleDissolve_apb` · ⚠D `Mobile/Particles/Additive`+`Alpha Blended` | **C1′** | 同 #1；锅 F **已排除**（`Prefabs/Artillery Ground.prefab` 那个 mesh=0 的 PSR 是 `m_Enabled: 0`） | 中 | 同 #1；另注意 `destroyTime 2.50 < 自然时长 6.65` |
| 5 | DaIrongobEffect | 暗 | 1.081 | 1617/660 1822/989 2051/1216 2208/1261 1821/913 634/647 0/0 0/0；L 0.41→0.59 · S 0.18→0.58 · **mp 0.84→0.36** | 9；⚠D `Mobile/Particles/Additive`×2(`line_light`)；`Chestrays` 用标准 URP | **D**（线索 `Chestrays`） | `Chestrays` 全局：53 条 E 里 **41 条偏暗**（基线偏暗率 23%→77%）——全库**最强偏暗关联材料** | 中 | 单位 `Chestrays.mat`（`MatIsoProbe`）比原版材质的 `_BaseColor/_EmissionColor/_BaseMap`；它是标准 URP shader ⇒ **数值是活的** |
| 6 | Invoke Minion Hits Ground | 亮 | 1.043 | 494/1812 679/3106 931/4051 835/3772 **1/3282** 0/0 0/0 0/0；L 3.67→4.57 · S 2.48→6.31 | 5；⚠B `Fx_ParticleDissolve_apb`+`Fx_RockDissolve` | **C1′** | 同 #1；`控制器数 = 0`（原版就没有 AnimFXController） | 中高 | 同 #1 |
| 7 | BulletImpact_Ork Missile Big | 亮 | 0.889 | 175/309 461/836 462/1657 **131/1238** 0/22 0/0 0/0 0/0；L 1.77→9.45 · S 2.29→24.1 · mp 0.60→0.35 | 12；⚠D `Everguild/FX/Particle Premultiply`+`Mobile/Particles/Alpha Blended`；无 ShaderGraph | **C1′**（副：J） | 0.75s 时原版已衰减到 mp 0.137、导出仍 0.351（导出**晚熄**）；台账 `寿命差异` 空 | 中 | 比两版 `ParticleSystem.main.startLifetime`/`Emission` 曲线（`ParticleModuleProbe`）——Particle Premultiply 是我们的近似，预乘那套是不是把 alpha 抬高了 |
| 8 | HeroOfTheEmpire OLD | 暗 | 0.800 | 1112/1082 1512/1290 **2630/1287** 2355/1069 1387/498 **1922/407** 44/9 0/0；L 0.97→0.21 · S 0.86→0.22 | 8；⚠D `Mobile/Particles/Additive`；`Universal Render Pipeline/Lit`(`PlasmaExplosion_add`) | **J** | 1.50s 原版 1922 亮点、导出只剩 407 ⇒ **导出提前约 0.5–1.5s 结束** | 高 | 查 `destroyTime 4.00` / `WFModuleDoDestroyInTime`：导出的收尾时刻是不是按 `destroyTime` 而不是原版的自然寿命 |
| 9 | Cosmic Serpent_Secondary | 亮 | 0.791 | 421/419 909/909 284/285 312/384 326/453 336/533 249/579 242/557；**前三刻逐像素相同**，0.75 起 S 1.91→5.05 | 12；⚠A `Everguild/Cards/Gem Crystal Glitter` · ⚠D `Unlit UV scroll`+`Additive` | **C1′** | 前 0.5s L=1.00/S=1.00（两侧一模一样）⇒ **差分只可能来自 0.75s 之后新增的分量**（`LightningTrail` 拖尾 / `Line Disappear`） | 中 | 只隔离 0.75s 之后出现的那几个渲染器；`Everguild/Cards/Gem Crystal Glitter` 是 A 组（两表都没） |
| 10 | Cosmic Serpent | 亮 | 0.786 | 421/419 909/909 284/285 312/384 326/453 336/533 249/579 242/557；同 #9 | 12；同 #9 | **C1′** | 同 #9；`destroyTime 13.00` 远大于其他 | 中 | 同 #9 |
| 11 | CreateCard Necron | 亮 | 0.761 | 146/197 340/388 395/457 381/537 543/661 360/524 452/638 0/0；L 1.14→1.46 · S 1.47→2.99 · mp 1.03→1.63 | 9；⚠D `Everguild/FX/Particle Dissolve Mask` | **D** | 覆盖率只涨 1.1–1.4 倍而亮度和涨 1.5–3.0 ⇒ 逐像素变亮；但唯一缺口 shader 是 `Particle Dissolve Mask`（近似） | 低 | 先量 `Generic Particle Dissolve For Sprites Orks Cardback` 一个 def 的 `_Color/_EmissionColor`（它最可能吃到近似替代） |
| 12 | EarthCrack_Single | 暗 | 0.755 | 31/22 224/156 577/395 1097/780 1529/1145 1992/1420 1797/1399 546/129；L 0.68→0.78 · S 0.43→0.50 · **mp 0.80→0.55 全程一致** | 3；**全部标准 URP Particles/Unlit**（无任何映射缺口） | **D** | 逐像素比在 6 个时刻上稳定在 0.49–0.57 ⇒ 不是某一段的锅，是**整条材质链系统性暗 45%** | 中 | 唯一没被排除的环节是 `EffectExporter.ImportTexture`（GPUBit→`GetPixels` 线性值→`SetPixels`→PNG 的 sRGB 二次变换）。比 `Textures/EarthCrack*.png` 与原版同张贴图的像素均值 |
| 13 | Goff_WaaaghRed_skulls | 暗 | 0.747 | 271/109 310/173 276/150 490/511 733/858 0/0 0/0 0/0；L 0.40→1.17 · mp 1.23→0.80 | 6；⚠D `Mobile/Particles/Additive`；`ray_light` 用标准 shader | **D** | 0.15–0.50 覆盖只有原版 0.40–0.56 且逐像素也低；0.75 之后反超（导出晚熄） | 低 | 0.15–0.50 段与 0.75+ 段要分开看；先比 `ray_light`（本块两个方向都出现 ⇒ 单看材质可排除） |
| 14 | DutyEffect | 亮 | 0.647 | 0/0 980/1090 991/1072 983/1069 881/1045 0/0 0/0 0/0；**L 1.08→1.19 · S 1.83→2.00 · mp 0.99→1.76** | 7；⚠A `Everguild/FX/FX Shine For Animation` · ⚠D `Additive` | **B/E′** | 亮点数几乎逐一相同（980/1090）而亮度和翻倍 ⇒ 同一批像素亮 1.8 倍；`FX Shine For Animation` 是**两张表都没有** ⇒ 掉 URP 兜底 | 中 | 单位 `FX Shine For Animation.mat`：它大概是**靠遮罩让亮带扫过**的，我们的替代把整片点亮 |
| 15 | UM_Buff | 亮 | 0.608 | 0/0 **12/0 17/0** 272/100 1706/2947 1255/2915 45/45 0/0；L 0.37→2.32 · mp 0.43→0.40 | 8；⚠D `Mobile/Particles/Additive`(`ray_light`,`treasure_ray`,`Flash1`) | **B/E′** | **相位差**：0.30/0.50 原版有 12/17 亮点而导出是 0 ⇒ 导出的发射**起得晚**；1.0/1.5 导出又多 1.7–2.3 倍 | 中 | 先比 `startDelay` / `Emission` 曲线（`ParticleModuleProbe`），再比 `ray_light` |
| 16 | Goff_Rok_Invasion | 暗 | 0.595 | **2/1800 2/1797 4/1797 8/1796 11/1797 12/1796 12/1797** 191/159；t=3.00 L 0.83 · S 0.55 | 4；⚠D `Mobile/Particles/Additive`+`Alpha Blended`+`Multiply` | **H** | 导出侧 0.15–2.00s **全程 1796–1800 个亮点**而原版只有 2–12 个；`sum` 只 5–11 ⇒ 每像素 ~0.003，是**一大片近乎全黑的面片**。旧表在这条挂的锅 F（mesh=0）**已排除**（那个 PSR 是 `m_Enabled: 0`） | 高 | 找这 1800 像素是谁：`Star_35_Multiply`（`Mobile/Particles/Multiply` → `InferBlend` 给 DstColor/Zero）· `Ork_Rok` · `treasure_ray` 逐个 `SetActive(false)` 看亮点数 |
| 17 | Buff_DaemonicPact | 亮 | 0.585 | 17/76 71/159 169/272 267/377 317/438 387/515 563/687 168/374；L 1.22→2.24 · S 1.09→3.60 · mp 0.09→0.14 | 9；⚠D `Mobile/Particles/Alpha Blended`(`smokesoft_dark`) | **B/E′** | mp 只有 0.09–0.14（两侧都是暗烟），但导出**亮点数多 1.2–2.2 倍** ⇒ 烟雾的**覆盖面积**胀了 | 低 | 这套材质全是 `Alpha Blended` 近似 ⇒ 比原版 `ParticleSystem` 的 `startSize` 与 `_MainTex` 的 alpha 范围 |
| 18 | ArdAsNailsEffect | 暗 | 0.549 | 682/633 1066/920 1221/917 1220/849 1124/621 **607/15** 2/2 0/0；L 0.93→0.55 · S 0.90→0.51 | 8；⚠O `Everguild/Sprites/Sprite Additive`→`WarpforgeVFX/Sprites/Additive` | **J** | 台账白纸黑字 `寿命差异 = 导出提前 1.50s 结束`；实测 1.50s 原版 607 / 导出 15 | 高 | 同 #8：查 `destroyTime 2.00`（`自然时长 7.00`）是不是把还在播的粒子收掉了 |
| 19 | RegimentEffect | 亮 | 0.540 | 0/0 1334/1425 1400/1473 1365/1458 1216/1415 0/0 0/0 0/0；**L 1.05→1.16 · S 1.63→1.89 · mp 1.03→1.72** | 6；⚠A `FX Shine For Animation` · ⚠D `Additive` | **B/E′** | 同 #14（同一对材质、同一签名：覆盖 +5%、亮度 ×1.7） | 中 | 同 #14 |
| 20 | UM_TacticalInsight | 亮 | 0.538 | 80/96 151/190 236/324 1357/1527 1442/1550 559/605 0/0 0/0；L 1.07→1.37 · S 1.34→2.50 · mp **1.14→2.12** | 7；⚠D `Mobile/Particles/Additive`×3 | **B/E′** | 0.30/0.50 亮度和已 2.23/2.50 倍而覆盖只 1.26/1.37 倍 | 中 | 同 #14 |
| 21 | BulletImpact_Ork Missiles 1 | 亮 | 0.525 | 76/86 20/36 21/47 5/34 0/0 0/0 0/0 0/0 | 18；⚠D `Additive`+`Alpha Blended` | **D** | 台账置信度=**低**，峰值亮点只有 86（噪声下限 60）；只有 0.15s 两侧都过下限 | 低 | **先别看这条**：8 时刻采样窗对这个瞬时效果太稀疏，要 `EffectTimeSweep` 加密到 0.05–0.3s |
| 22 | Duty Reset | 亮 | 0.521 | 138/144 965/1019 991/1019 996/1019 796/869 0/0 0/0 0/0；**L 1.02→1.09 · S 1.24→1.89 · mp 1.07→1.92** | 8；⚠A `FX Shine For Animation` · ⚠D `Additive` | **B/E′** | 同 #14 | 中 | 同 #14 |
| 23 | ConcussiveEffect | 亮 | 0.485 | 573/646 **9/428 3/606 0/547 0/480** 0/0 0/0 0/0 | 9；⚠B `Fx_ParticleDissolve_apb` | **J**（副 C1′） | 原版 0.15s 之后就没有了（9/3/0/0），导出一路亮到 1.00s ⇒ **导出多播约 0.85s**；台账 `导出多播到 1.00s` | 中高 | 同 #1：`Mat_Fx_ParticleSet_apb` 不溶解 ⇒ 本该在 0.2s 内消失的粒子留到 1s |
| 24 | Sau_ReconstitutionProtocol | 暗 | 0.460 | 0/0 0/0 5/5 3/3 8/8 3/6 11/12 402/386；t=3.00 L 0.96 · S 0.63 · mp 1.25→0.82 | 9；⚠D `Additive`；⚠A `Hexagon_1`(`Extra Color`) | **D** | 目标效果只在 3.00s 一刻体量够；覆盖几乎相同、逐像素暗 18% ⇒ 幅度小 ⇒ 落 D 是**样本不足**，不是查过 | 低 | 加密集采样（2.5–3.5s 段）；单看 `Hexagon_1` 与 `Chestrays` |
| 25 | OrkBuff_KrumpDaGitz | 暗 | 0.456 | 112/151 155/169 472/362 588/446 596/400 601/360 621/354 0/0；L **1.35→0.57 单调下滑** · mp 0.75→0.53 | 12；⚠D `Particle Shine Custom Vertex Streams` · `Additive` | **D** | 0.15s 时导出还比原版亮 1.35 倍，之后一路掉到 0.57 ⇒ **导出侧粒子衰减/消失得比原版快**（不是整体暗） | 中 | 查 `startLifetime` 与 `Color over Lifetime` 曲线（`ParticleModuleProbe`）：C1′ 那套近似 shader（`Particle Shine Custom Vertex Streams`）**顶点流没做**，粒子"闪"不起来可能就是这条 |
| 26 | BloodThirstEffect_Gain | 亮 | 0.434 | **624/654 769/801 890/920 1016/1048** 497/499 229/229 1/1 0/0；L 1.03→1.05 · **S 1.92→1.78→1.64→1.45 · mp 0.84→1.53** | 9；⚠D `Particle Shine Custom Vertex Streams`+`Additive` | **B/E′** | **本块最干净的一条**：亮点数逐一几乎相等（差 ≤5%），亮度和却 1.4–1.9 倍 ⇒ 几何完全没变、**只可能是材质把同一批像素点亮了**；且 1.00s 之后两侧完全一致（1.00/1.00） | 高 | 直接单位这个效果：`Generic Trait Particle Shine` / `Fire1` / `ray_light` 三个 def，读导出材质的 `_Color/_EmissionColor` vs 原版 |
| 27 | UM_MasterOfArcanaEffect | 亮 | 0.417 | 22/39 149/190 252/388 **73/212** 38/146 0/0 0/0 0/0；L 1.28→2.90 · S 1.38→4.34 · mp 1.17→0.96 | 18；⚠D `Mobile/Particles/Additive` | **C1′**（副多播）| 0.75/1.00s 原版已衰减（73/38）导出仍 212/146 ⇒ 导出晚熄；`WarpforgeShaderMap` 注释点名这条用 `Doomweaver effect`（O 组） | 低 | 同 #23 |
| 28 | Teleport Recall | 亮 | 0.415 | 30/55 290/348 667/719 1200/1753 **2143/4104** 804/804 103/48 0/0；1.00s L 1.92，1.50s L **1.00** | 7；⚠O `Everguild/FX/Extra Color` | **D** | 只有 1.00s 那一格导出翻倍（2143→4104），1.50s 又**完全相同**（804/804）⇒ 1.0s 处多了一段**额外的爆发**，不是整体亮 | 中 | 逐发射器开关找出 1.0s 处的多出者；`Lightning Burst Random add` / `Smoke Sprite Sheet Additive Soft` 优先 |
| 29 | EC Target | 亮 | 0.405 | 47/82 603/634 **675/1072** 0/0 0/0 0/0 0/0 0/0；L 1.05→1.59 · S 1.08→1.91 · mp 0.81→0.86 | 7；⚠O `Everguild/FX/Extra Color` · ⚠D `Additive`+`Particles/Standard Unlit` | **D** | 0.30s 两侧几乎相同（603/634），0.50s 导出 1.59 倍 ⇒ 导出**后段还在长** | 低 | 同 #28（逐发射器开关，锁定 0.5s 才显形的那个） |
| 30 | MobEffect | 暗 | 0.395 | 18/14 402/323 410/333 415/335 397/310 26/26 16/16 2/2；L 0.80→0.78 · S 0.68→0.67 · mp 1.01→0.71 | 8；⚠D `Particle Shine Custom Vertex Streams`+`Additive` | **D** | 均匀偏暗 20%（覆盖 ×0.8、逐像素 ×0.75），且 1.5s 之后两侧**完全相同** ⇒ 差异只在主段 | 中 | 同 #25；`ray_light` 在本块两个方向都出现（亮 8 / 暗 4）⇒ 单看它可排除，要从 `Particle Shine Custom Vertex Streams` 入手 |
| 31 | Gain_Sentry | 亮 | 0.379 | 414/547 444/563 376/467 398/469 **159/159** 0/0 0/0 0/0；L 1.18→1.32 · **S 1.12→2.11** · mp 1.37→2.18 | 8；⚠D `Particle Shine Custom Vertex Streams` | **B/E′** | 1.00s 两侧**亮度和完全相同**（113/113），0.15–0.30 逐像素亮 2.18 倍 | 中 | 同 #26 |
| 32 | MoreDakkaEffect | 亮 | 0.347 | 1998/2881 2382/2864 2408/2552 2311/2585 1385/2467 0/0 0/0 0/0；**L 1.06→1.78 · S 1.36→1.88** | 5；**全部标准（Sprites/URP）** | **B/E′** | 0.30–0.75 覆盖只 +6~12% 而亮度和 +36~42%；1.00s 覆盖 1.78 倍（导出晚熄） | 中 | 无映射缺口 ⇒ 是数值/几何锅；先比 `Circle Particle`/`DustPuffParticle` 的 `startSize` 与原版 `_MainTex` |

---

## 三、最值得先修的 3 条

1. **锅 C1′（9 条）—— 2026-09-15 补的近似映射本身就是新的偏亮源。**
   判据硬：用到 §三 B 组 shader 的 14 条效果 E 率 **57%**（7 偏亮），基线 33%；而「两张表都没有」的 A 组 82 条 E 率只有 26%。
   症状也自洽：`BlastEffect`/`Invoke Minion*` 都是**覆盖率 3–7 倍 + 尾部拖长到 1.00s**，正是「该溶解掉的粒子没溶解」。
   **为什么这是第一条**：它一次性解释 9 条，而且是**上一轮"修复"引入的**——按现口径回滚或补做 dissolve，就能同时验证「E 组会不会掉」这件事本身。
   **动手前**：先给 `Mat_Fx_ParticleSet_apb` 一个带 dissolve 的自建 shader（判据 = 单位该发射器后亮点数应随 t 下降而不是平着），**只改这一个 def**，再重跑 sweep。

2. **锅 B/E′（9 条）—— 「残留混合值」那套判据（`InferBlend` 按原版 shader 名）没覆盖 `Everguild/FX/FX Shine For Animation` / `Particle Shine Custom Vertex Streams` 这一类。**
   判据硬：`DutyEffect`/`RegimentEffect`/`Duty Reset`/`BloodThirstEffect_Gain` 四条都是**亮点数几乎逐一相同、亮度和翻 1.5–2.0 倍**（`BloodThirstEffect_Gain` 最干净：624/654→1016/1048 覆盖差 ≤5%，S 1.9→1.45）。
   说明不是几何、不是粒子数，**就是同一批像素被点亮了**。`InferBlend` 对这两个名字**返回 null**（不含 additive/alpha blended/multiply/premultiply 关键词）⇒ 混合落在自建 shader 的默认值上。
   **为什么这是第二条**：它不依赖任何新工具，是纯判据缺口，改一处 `InferBlend` 就能 A/B。

3. **锅 J（3 条）—— 导出的收尾时刻不一样（`HeroOfTheEmpire OLD` 1.5s 原版 1922/导出 407、`ArdAsNailsEffect` 607/15、`ConcussiveEffect` 多播 0.85s）。**
   判据硬：这三条都是**一侧已熄一侧仍亮**，不是亮度差；`ArdAsNailsEffect` 台账已写「导出提前 1.50s 结束」。
   **为什么排在第三**：它只有 3 条，但**根因单一**（`destroyTime`/`WFModuleDoDestroyInTime` 的收尾口径），而且**影响所有效果**——修对了是全库收益，不只是这 3 条。

> **偏暗那 10 条（锅 D）有一条共同的、可执行的线索**：把「只在偏暗侧出现」的材料按 log-odds 排，前五名是
> `Chestrays`(亮12/暗41) · `Dust`(3/7) · `line_light`(3/7) · `DustPuffParticle`(3/5) · `EnergyShockwave`(3/5)（基线 亮:暗 = 77:23）。
> 它们**全部映射到标准 shader**（URP Particles/Unlit 或已修过的 `Mobile/Particles/Additive`）⇒ **原版属性表完整、数值是活的** ⇒ 锅在**材质数值本身**，不是映射。
> 反面对照：`ray_light` 在偏亮 8 条、偏暗 4 条都出现 ⇒ **单看它可排除**（别再把它当偏亮/偏暗的锅）。

---

## 四、判不出来的（10 条，锅 D）

`DaIrongobEffect` · `CreateCard Necron` · `EarthCrack_Single` · `Goff_WaaaghRed_skulls` · `BulletImpact_Ork Missiles 1` ·
`Teleport Recall` · `EC Target` · `MobEffect` · `Sau_ReconstitutionProtocol` · `OrkBuff_KrumpDaGitz`

- 其中 **3 条是「样本不足」而不是「查过没查出」**：`BulletImpact_Ork Missiles 1`（峰值 86，只有 1 个时刻过噪声下限）· `Sau_ReconstitutionProtocol`（只在 3.00s 一刻有体量）· `EC Target`（只有 2 个时刻）。⇒ 这 3 条应该先**加密集采样窗口**再谈根因，别按现在的数字改。
- 其余 7 条在四处（binder 记录的 shader 名 · 材质贴图槽 · mesh 槽 · 渲染模式）都干净，**只能单位实拍隔离**（`EffectIso.Run` / `MatIsoProbe`）。

## 五、两条要记的「尺子」问题（本轮踩到）

1. **`效果库报告.tsv`（2026-09-18 00:59 生成）的`判定/亮度比`两列不是当前台账**，是 `WarpforgeEffectLibrary` 源 JSON 里那套**旧单帧 1.2s 指标**（`EffectLibraryBuilder.cs:147,153` 直接写 `e.ratio`）。
   后果：**32 条里有 20 条方向都不同** —— `BlastEffect` 旧 `E- 0.25`(偏暗) vs 现 `6.13`(偏亮)，`Invoke Minion Hits Ground` 旧 `0.11` vs 现 `2.84`。
   **一律以 8 时刻扫描（`sweep_*.tsv`）为准**；`效果库报告` 那两列只能当历史。
2. **我自己的第一个解析器出过假阳性**：用 `re.split('\n  - name: ')` 切 prefab 时，**每个 def 块的最后一行没有尾随 `\n`**，导致 `texVals` 永远解析成空 —— 一度报出「DustPuffParticle 的 `_BaseMap` 丢了」这类结论，**是解析器 bug，不是资源缺陷**。已改判：本块**没有**「贴图全丢」的 def（锅 A 不成立）。
   判据：`Prefabs/BlastEffect.prefab:29853-29856` 起 `texNames: [_BaseMap]` / `texVals: [{fileID: 2800000, guid: 4cfd0474…}]` 一一对应。
