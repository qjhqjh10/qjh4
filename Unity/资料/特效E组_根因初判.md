# 特效台账 E 组（亮度/密度不对）· 逐类根因初判（2026-09-13）

> 数据源：`资料/特效还原台账.tsv`（分组=E 且 置信度=高，**实测 176 行**；全表 958 行）
> ⚠️ **只读查证报告，结论是「嫌疑」不是定论**（本工程已踩过五次「尺子坏了当资产坏」）
> 口径提醒：台账 `判定` 列写的是「偏亮（导出更亮/更密）」/「偏暗（导出更暗/更稀）」；
> **「E 组」是 `分组` 列的值**。E 组共 248 行，其中高置信 176 行。

---

## 🔴 零、先看这一条：**台账比一次真修复还老**

| 时间 | 事 |
|---|---|
| 2026-09-11 20:12–20:14 | sweep 数据采集 |
| 2026-09-11 20:21 | 台账落盘 |
| **2026-09-12 10:18** | **`WFDistortion.shader` 被改**（提交 `71f2e0e`「特效线 P1-a0：抓屏扭曲 —— 推翻旧判据 + 修掉混合被残留值污染的真 bug」） |

⚠️ **这个时间差主对话已经独立核过**（`ls` 台账 09-11 20:21 · `git log` shader 09-12 10:22）。

⇒ **含抓屏扭曲的 45 行，量的还是「修之前」的导出结果。**
**先别按它们的现存数值改东西** —— 重跑一次 sweep 很可能自己掉一大截。

---

## 判读口径（下面每条「怎么推的」都走这四条）

1. `lit` = 与背景色差 L1 > 6/255×3 的像素数（**覆盖**）；`sum` = 这些像素的线性 RGB 和（**总能量**）。
   台账「亮度比中位」= `median(exp_sum / orig_sum)`（`EffectSweepBatch.cs:311-315`、`analyze_sweep.py:175`）。
2. 派生两个量，用来分「材质锅」还是「prefab 锅」：
   - **覆盖比 Rl** = exp_lit / orig_lit　　**逐像素比 Rpp** = (exp_sum/exp_lit) / (orig_sum/orig_lit)
   - Rl≈1 而 Rpp≠1 → 覆盖没变、每像素亮度变了 → **材质类**（alpha/混合/自发光/startColor）
   - Rl 明显≠1 → 覆盖变了 → **prefab 类**（大小/发射率/寿命/发射器缺失）
   - 但 `lit` 是**阈值量**：alpha 整体抬高时边缘淡像素也会越线、覆盖跟着涨。
     所以 **Rl 与 Rpp 同向变化时，一个「材质增益错」就能同时解释两者**，不必假设 prefab 也坏。
3. 置信门槛：峰值亮点 ≥500 算「高」。176 行里 **25 行峰值 <800、11 行 <600**，贴着门槛，
   这批比值本身不稳，**不要单独下结论**。

## 总览（按行数降序）

> `技术构成` 的「精灵图」= prefab 里有 SpriteRenderer，**176/176 全中、不具区分度**；
> 真正的区分位是 Mesh 粒子数 / 特效家族 fam / 发射器数。下面 7 块互斥、合计 176。

| 技术构成 | 行数 | \|ln\| 中位 | 偏亮/偏暗 | 峰值亮点中位 | 主要嫌疑 |
|---|---|---|---|---|---|
| 1 精灵图（无 Mesh/无扭曲/无 Matcap） | 89 | 0.718 | 57/32 | 1198 | 材质增益：偏亮=alpha/自发光放大，偏暗=alpha 偏低 |
| 2 Mesh粒子+精灵 | 31 | 0.672 | 21/10 | 1221 | 同块1，外加「Mesh 弹体/光环没导出」 |
| 3 抓屏扭曲+精灵 | 27 | 0.757 | 3/24 | 4140 | **先怀疑尺子**（见 §零）+ 原版那块恒定灰方块 |
| 4 扭曲+Matcap+Mesh | 13 | 1.075 | 4/9 | 1020 | 覆盖掉六成而每像素没怎么掉 → 缺发射器/大尺寸粒子 |
| 5 Matcap+Mesh | 9 | 0.892 | 3/6 | 1574 | 每像素掉得比覆盖狠 → Matcap 亮度/自发光 |
| 6 扭曲+Mesh | 5 | 0.916 | 2/3 | 1144 | 样本太小，两极分化，逐条看 |
| 7 Matcap+精灵 | 2 | 0.549 | 0/2 | 1446 | 材质类（Rl 0.89 / Rpp 0.66） |
| **合计** | **176** | **0.744** | **90/86** | 1350 | mean\|ln\|=0.975 |

## 逐类

### 1 精灵图（89 行：偏亮 57 / 偏暗 32）
- **共同嫌疑**：普通 billboard 精灵粒子，两侧偏亮偏暗都有，但**各自内部高度一致** → 材质类，不是 prefab。
- **怎么推的**：偏亮 57 行 Rs=2.37 / Rl=1.44 / Rpp=1.55；偏暗 32 行 Rs=0.57 / Rl=0.87 / Rpp=0.65。
  偏暗组 **Rl 0.87 接近 1 而 Rpp 掉到 0.65** = 覆盖基本对得上、每像素暗 35% → 判「材质 alpha/自发光偏低」；
  偏亮组 Rl 与 Rpp 同向涨，最省事的解释是**同一类材质增益偏大**（阈值量效应，见口径 2）。
- **需要实拍验证的**：`Buff_Mana`、`Healing_Circles`、`MarkOfTzeentch_enchantment`、`Basic_army_DA`、`Explosion_Debris`

### 2 Mesh粒子+精灵（31 行：偏亮 21 / 偏暗 10）
- **共同嫌疑**：同块 1 的材质增益；**偏暗的 10 行额外怀疑 Mesh 粒子（弹体/光环）没导出或材质没还原**。
- **怎么推的**：偏亮 Rs=1.96 / Rl=1.31 / Rpp=1.49（同块 1 形状）；偏暗 Rs=0.48 / Rl=0.63 / Rpp=0.72，
  覆盖掉得比逐像素狠 → 少了一整块几何体（坑表已记 `ParticleSystemRenderer.mesh` 漏导会让 Mesh 粒子整个消失）。
- **需要实拍验证的**：`Invoke Minion Hits Ground`、`DaIrongobEffect`、`BlueSummonCircle`、`Sau_HealProtocol`、`Protocol Base`

### 3 抓屏扭曲+精灵（27 行：偏亮 3 / 偏暗 24）
- **共同嫌疑（第一顺位是尺子，不是资产）**：文档 P1-a0 已实测 —— 原版扭曲材质在**没有内容可扭曲**的场景里
  输出恒定的 `half4(0.214)` 半透明灰方块（那张屏幕贴图在测量环境里根本没绑上）。白板/扫描就是空场景，
  所以这 27 行**原版那一侧的亮度里有一块是灰方块，不是特效**。24/27 偏暗、Rl=0.62（覆盖掉近四成）
  正好是「灰方块在导出侧没画出来」的形状。
- **第二嫌疑（真 bug，但台账没算进去）**：见 §零。
  `Stealth_Proc_UM`/`_Eldar` 的导出「lit 2000+ 但 sum≈0」就是**不透明黑块**特征。
- **怎么推的**：偏暗 24 行 Rs=0.47 / Rl=0.62 / Rpp=0.77（覆盖先掉、每像素后掉）；峰值亮点中位 4140 为全组最高。
  Rl 系统性 <1 且同族同向，符合「一个共用材质/共用 pass」而不是逐个 prefab 坏。
- **建议**：**先重跑 sweep 再看这批**。
- **需要实拍验证的**：`SynapseEffect`、`BansheeHowl_blood`、`Telepathic_Control_target`、`Impact_blunt_lightning`

### 4 扭曲+Matcap+Mesh（13 行：偏亮 4 / 偏暗 9，med|ln| 1.075 **全组最差**）
- **共同嫌疑（偏暗 9 行）**：**prefab —— 少了一部分发射器/大尺寸粒子**，而不是整体变暗。
  偏暗 Rs=0.34 / Rl=0.42 / **Rpp=0.86**：覆盖掉 58%，每像素几乎没掉 → 是「某一块整个没画」。
  本块 13 行里 8 行发射器 ≥48（median 40+），是大范围多发射器效果，最容易被「一个子发射器丢了」打到。
  次嫌疑：同块 3 的灰方块尺子问题（本块半数带扭曲）。
- **偏亮 4 行**（`Vortex Warhead Impact` 3.37、`EC Necrons Earthquake` 2.12、`EC Astra Militarum Planetary Invasion` 1.70、
  `Artillery Manticore down 4x spread` 1.41）：Rs 1.55 / Rl 1.42 / Rpp 1.15 → 覆盖先涨，同块 1 的增益形状。
- **怎么推的**：偏暗组 Rpp 明显比 Rl 高（0.86 vs 0.42）是「缺内容」的判别式；反之整体变暗会让两者一起掉。
  聚类证据：`Artillery Manticore up 2x / up 4x / BulletImpact_artillery_arc / BulletImpact_artillery_manticore_spread`
  四行挤在 Rs 0.34–0.37 —— **同一族同一因，一次能修四个**。
- **需要实拍验证的**：`Vortex Explosion Massive`、`Rapturous Ruination Board`、`Artillery Manticore up 4x`、`BulletImpact_artillery_arc`、`EC Emperor's Children 1 Green`

### 5 Matcap+Mesh（9 行：偏亮 3 / 偏暗 6）
- **共同嫌疑**：**Matcap 材质的亮度/自发光没还原对**（每像素掉得比覆盖狠）。
- **怎么推的**：偏暗 6 行 Rs=0.41 / **Rl=0.76** / **Rpp=0.56** —— 覆盖只掉 24% 而每像素掉 44%，
  方向与块 4 相反，指向材质而不是缺件。内部还能再分两簇：
  ① `Goff_GreatestWarboss` `Goff_ProphetOfDaWaaagh` `Buff_Tyranid Armor` `BeastSnagga_Summon` `Tyranid_Burrow`
  五行挤在 Rs 0.34–0.42（同一类 Matcap，同因）；
  ② `Recall_Eldar_2_quick_ground` / `_warlord` 两行 Rs 4.81 / 5.35（Rl 2.21、Rpp 2.56，覆盖与逐像素同时翻倍）。
- **需要实拍验证的**：`Tyranid_Burrow`、`Goff_GreatestWarboss`、`Buff_Tyranid Armor`、`Recall_Eldar_2_quick_ground`

### 6 扭曲+Mesh（5 行：偏亮 2 / 偏暗 3）
- **共同嫌疑**：**样本太小（5 行）且两极，不合并**；两条线索沿用块 3/块 4：
  偏暗的 `Sword_Slash_DA`(0.32) `EC Black Legion Helfire Outburst`(0.40) `Atk_JainasMor`(0.50) 与偏亮的
  `BulletImpact_Sonic Weapon 1`(2.02) `Hammer_Slam_SW`(5.07) **不是一回事**。
- **怎么推的**：5 行 Rl 中位 0.76、Rpp 中位 0.76（同向），覆盖与亮度一起掉，灰方块 + 缺件两种都能拟合，
  **读不出单一根因**。
- **需要实拍验证的**：`Sword_Slash_DA`、`Atk_JainasMor`、`Hammer_Slam_SW`

### 7 Matcap+精灵（2 行：0 亮 / 2 暗）
- **共同嫌疑**：材质类。`KhaineBuff`(0.50)、`VanguardIdleEffect`(0.67)，Rl 0.89 / Rpp 0.66。
- **怎么推的**：覆盖几乎没动、每像素掉三分之一 → 材质；只有 2 行，**不推广**。
- **需要实拍验证的**：`KhaineBuff`、`VanguardIdleEffect`

## 单独值得看的（不符合任何一类）

| 效果名 | 亮度比 | \|ln\| | 嫌疑 | 理由 |
|---|---|---|---|---|
| `CardPrefab` | 0.04 | 3.230 | **量错了对象，不是特效** | 卡牌 prefab（技术构成含 Sprite/UI/文字），8 帧恒定不变（原版 12552/30190，导出 2939/1195）→ **该从 E 组剔除** |
| `RemnantBody3D Aeldari` | 13.42 | 2.597 | 导出一张**不衰减的常驻层** | 导出 8 帧恒定 660–735 亮点、sum 1224–1291 完全不衰减；原版 96–296 且在衰减 → 不是「更亮」 |
| `Stealth_Proc_UM` / `_Eldar` | 0.01 / 0.02 | 4.500 / 4.132 | **不透明黑块**（混合状态） | 导出 lit 2242 但 sum≈0，逐像素 0.0004 vs 原版 0.423 → 像素比背景暗却不发光，正是 P1-a0 记的 `One/Zero` 残留值污染形状 |
| `Vortex Explosion Massive` | 0.00 | 5.528 | 整个发射器组没材质 | 导出逐像素 0.045 vs 原版 1.108（暗 25 倍）且覆盖只剩 10%，**双重缺失** |
| `Buff_Mana` / `_Mana_2` / `_Mana_3 Holy` | 9.25 / 8.74 / 7.88 | 2.224 / 2.168 / 2.064 | 同族共用材质，**一次修三个** | 三行全在 8–9 倍，且 lit 与逐像素同步涨（Rl≈1.87 / Rpp≈1.59） |
| `Recall_Eldar` 族（7 行） | 3.3–5.2 | 全偏亮 | 全组最同向的名字族 | 7/7 偏亮、med\|ln\|=1.570 → 族内共用 prefab/材质 |
| `Basic_army` 族（7 行） | 0.40–0.68 | 7/7 偏暗 | 最像「同一材质差一点点」 | med\|ln\|=0.443，全部轻微偏暗、方向一致 |
| `ArtificeEffect` | 0.62 | 0.482 | 精灵本体没显示（文档已点名） | P0-h 遗留：导出 85 vs 原版 2936，精灵贴图/alpha 而非粒子 |
| `Environmental Condition*` 族（7 行） | 0.33–2.12 | 3 亮 / 4 暗 | **全屏叠加类，量法可比性弱** | 文档已记渲染器半径 245+；白板取景下该**单列人工看，别按数值改** |

## 一句话收口

176 行里 **78 行各帧偏差方向一致**（疑整体增益/材质，块 1 占 45 行）、
**25 行至少一帧与实质内容吻合而中位仍偏离**（疑时序/相位）、
**22 行有明确寿命差异**（导出提前结束 15 / 多播 7）。
**含抓屏扭曲的 45 行先别动，重跑 sweep 再说。**

---

# 追加：按技术构成逐块定根因（2026-09-13 第三十三轮，派了 3 个子代理）

> 上一版只给「按数值分组」的初判；这一轮按**技术构成**（`资料/比对基线/per_effect_tech.json`）
> 把 176 个切成三块，逐效果落到**具体发射器 / 材质槽**，并给出**故障模式**。
> ⚠️ 三块的共同前提：**台账（09-11 20:21）比 `WFDistortion` 修复（09-12 10:18）还老**，
> 所以**没有一个 `|ln|` 数值是「修过了的旧数」** —— 下面的结论都是**结构性**的（哪一类锅），
> 不是「按数值改多少」。**改完任一条都要重跑 sweep 复核。**

## 一、精灵图系（89 个）——**三类锅，合计覆盖 66 条**

先纠一个口径：`per_effect_tech.json` 的 `sr`/`mr` **在全部 958 行都是 true**（常量），
`工具/analyze_sweep.py:148-149` 直接照抄当标签。实测 `Prefabs/Buff_Mana.prefab` 里
一个 `!u!212 SpriteRenderer` 都没有 ⇒ 这 89 个的真定义是「**无 Mesh 粒子 / 无扭曲 / 无 Matcap 的纯 billboard 粒子**」。
两条负面结论（实测）：① 89/89 的 `!u!199` 数与 tech 的 `psr` 一致 → **本块没丢发射器**；
② `Editor/MatKeyCheck.cs` 的「材质缓存键碰撞」在这 89 里 **0 碰撞，可排除**。

| 故障模式 | 条数 | 机制 |
|---|---|---|
| **A. `ParticleSystemRenderer.trailMaterial` 槽被绑到「贴图全丢的重导残留 def」** | **35**（30 亮 / 5 暗）| 42 个槽位 **42/42 `texNames` 为空**，而同一 prefab 有同名兄弟 def 带 `_MainTex`+`_Color`（`Prefabs/Buff_Mana.prefab` 的 `materials[2]` vs `[1]`）⇒ `_MainTex` 落回 `"white"` |
| **B. 内置 Standard 残留混合值被自建 shader 当真值用** | **32**（28 亮 / 4 暗）| 原版老 shader 把混合**写死在 pass 里**，材质上仍序列化着 `_SrcBlend=1/_DstBlend=0/_ZWrite=1`；`WFParticlesExtraColor.shader:53-54` 用 `Blend [_SrcBlend][_DstBlend]` + `ZWrite [_ZWrite]` ⇒ **One/Zero 不透明 + 写深度**（加法发光全废）。<br>🔴 **根因在 `WarpforgeEffectBinder.cs:122`**：`hasBlend = m.HasProperty("_SrcBlend")` **恒真** ⇒ `WarpforgeShaderMap.cs:60-76` 的 `InferBlend()` **永不执行**。与 09-12 修的 P1-a0 是**同一判据**，但那次只覆盖「shader 不声明 `_SrcBlend`」的情况。<br>重灾材质：`line_light`(10) · `ray_light`(11) · `Fire1`(8) · `smokesoft_blend`(5) |
| **C. 映射表 / 属性表缺口** | **26**（未映射 shader→保留占位材质 **16**：14 亮/2 暗 · `_ColorMode=1` 丢 `_EmissionColor` **11**：6 亮/5 暗，其中 4 条可量化偏暗）| 10 个 shader 名不在 `WarpforgeShaderMap.cs:22-55`（`Everguild/FX/Particle Dissolve Mask` · `Multi Ray` · `TrailShader_1` · `TrailShader_Fading` · `Unlit UV scroll` · `Particle Shine Custom Vertex Streams` · `Particle Premultiply Greyscale Coloring` · `Alpha Mask One Layer` · `Alpha Masks Two Layer` · `Shader Graphs/Doomweaver effect`）⇒ `Shader.Find` 找不到 ⇒ `WarpforgeEffectBinder.cs:91-95` 返回 null ⇒ **该槽保留占位材质**。<br>`_ColorMode=1` 那批的亮度写在 `_EmissionColor`（`DA_Winged_Sword_Glow_extra` = 4.62 vs `_Color`=1），`Binder.cs:103-108` 的 `m.HasProperty` 把它**静默丢掉** |

三者**互有重叠**（如 `DeckBuff_Green`、`Buff_Sororitas_quick_fire`、`SlayEffect`、`CreateCard EC Elixir` 同时中两条）。

## 二、Mesh 粒子 / Matcap 系（42 个）——**三类锅，合计覆盖 58 条**

| 故障模式 | 条数 | 机制 |
|---|---|---|
| **D. 残留混合值**（同上面 B）| **29** | 同一机制、同一处判据（`Binder.cs:122`）。重灾材质同上 |
| **E. prefab 侧发射器根本不画** | **16** | `m_RenderMode: 4`(Mesh) 且 **`m_Mesh: {fileID: 0}`**：`Artillery Ground`、`Goff_Rok_Invasion`；`m_RenderMode: 5`(None) 共 **18 个发射器 / 涉及 14 行**，其中 `KhaineBuff` 的 `Fire Twirl` 是该效果**唯一的** Matcap 发射器 |
| **F. 原版 shader 未进地图 → 落 URP 近似替代** | **13**（Matcap 系 11 行全中）| `WFMatcap.shader` 只认 25 个属性，原 `Matcap Full Options` 的 `_BorderColor1/2`、`_Noise`、`_NOISECHANNEL_R`、`_USEEMISSION`、`_EmissionTex` 全无对应；且 `WFMatcap.shader:144-146` **无条件** `_MainTex × _MatCap × _Intensity` ⇒ 若原版只采样 `_MainTex` 就是**系统性变暗**（实测 `Rpp` 一致落在 0.42–0.65）|

**唯一「同一材质两种方向」的证据**：`RockDebris.mat` 在 4 行偏暗、2 行偏亮 ⇒
那 2 行（`Recall_Eldar_2_quick_ground` / `_warlord`）是 **prefab 锅**，材质可排除。

## 三、抓屏扭曲系（45 个）——**一条系统性原因覆盖 41 条**

**结构性结论**：45 个里 **41 个的扭曲槽共用同一份材质 `Materials/RippleSubtle Distort.mat`**
（另 2 个 `Heat Distortion.mat`、1 个 `Explosion Distort.mat`、1 个 `Distortion Star Low Strength.mat`）。
四份都映射到 `WarpforgeVFX/FX/Distortion`（`WarpforgeShaderMap.cs:31`），
**读的是 `_CameraOpaqueTexture`，而原版（字节码实测）读 `_GrabPassTransparent`**
⇒ **一个 URP Renderer Feature 就能覆盖全部 41 条的扭曲分量**。
台账 `技术构成` 的「抓屏扭曲」来源 = `per_effect_tech.json` 的 `fam` 字段，45/45 与
`导出报告.tsv` 的「原 shader 含 Particle Distortion Affect Transparents」一致，**tag 无假阳**。

- **另有各自原因 4 条**：`Vortex Explosion Massive` 与 `Stealth_Proc_UM` = 09-12 已修的**混合残留黑块**；
  `EC Astra Militarum Planetary Invasion` = **9 个渲染器材质槽为 null**；
  `RemnantBody3D Aeldari` = **循环发射器常驻不衰减层**。
- 其中 21 条**另叠**「寿命差异 / 循环发射器 / 覆盖翻倍」，修扭曲时要一并处理。

## 四、三条读不出根因的（**别按台账数字改**）

`EarthCrack_Single` · `ArdAsNailsEffect` · `MoreDakkaEffect` —— 在 binder 记录、材质残留、
mesh 槽、渲染模式**四处都干净**，必须**实拍隔离渲染**（`EffectIso.Run`）才能定。

## 五、这一轮真正要修的三个点（按覆盖面）

1. 🔴 **`WarpforgeEffectBinder.cs:122` 的 `hasBlend` 判据** —— 一处修好同时解掉 **B(32) + D(29) = 61 条**。
   现在 `m.HasProperty("_SrcBlend")` 对我们自己写的 shader 恒真，把兜底的 `InferBlend()` 挡死了。
2. 🔴 **给 URP 加 Renderer Feature 填 `_GrabPassTransparent`** —— 解掉 **41 条**（P1-a0 的收尾）。
3. 🟡 **补 `WarpforgeShaderMap` 的 10 个未映射 shader + `_EmissionColor` 通路** —— 解掉 **26 条**。
