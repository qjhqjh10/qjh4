# E 组 · `Sprites/Default`–`Sprites/Mask` 族（35 条）—— E 率为什么是别人的 3 倍

> 2026-09-18 · 只读调查（**未跑 Unity**）。数据：`资料/特效还原台账.tsv`(09-18 20:39) ·
> `资料/比对基线/sweep_{orig,exp}.tsv`(09-18 20:38 / 20:40，**当前构建**，与 `Prefabs/`(09-17 23:50) 同一代) ·
> `资料/普查产出_0917/效果_shader对账.tsv` · `MyGame/Assets/WarpforgeVFX/{Prefabs,Materials}` ·
> 原版 = `D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`
> （**UnityPy 1.24.2 实读对象树**，不是 JSON 桩；解包副本 `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`，`SpriteMask/` 下 133 个对象）。

## ① 一句话结论

**`Sprites/Default` 不是机制，是「这个 prefab 带一个 `Sprite Mask` 节点」的标记。**
那个 `Sprites/Default` 材质挂在遮罩节点上**被禁用（`m_Enabled: 0`）的 `SpriteRenderer`** 上，`Sprites/Mask` 挂在**同一个 GameObject 上的 `SpriteMask`** 上 ——
两者是**一个节点贡献的两条材质定义**，所以永远同进同出（117 条 / 122 条，差的是脚本判定口径）。
我们对这个节点的导出与原版**逐字段零差异**（个数 / 层级 / enable / maskInteraction / 材质 / transform 全同；957 个 prefab 里 941 个整树逐渲染器相同）。
⇒ **3 倍的 E 率是家族混淆（card-draw / trait-icon 类图标效果），不是 SpriteMask 造成的。**

## ② 三样本组件对照表（原版用 UnityPy 实读，导出用 YAML 逐文档解析）

| | UM_CardDraw | SAU_CardDraw | FrenziedEffect |
|---|---|---|---|
| 原版根 pathID | 802239406013749788 | −2383233696338876608 | −3933897846611088915 |
| 组件直方图 **原版 = 导出** | GO 7 / Transform 7 / PS 5 / **PSR 5** / **SpriteMask 1** / **SpriteRenderer 1** / MB 1 = 27 | 同左 = 27 | GO 5 / Transform 5 / PS 3 / **PSR 3** / **SpriteMask 1** / **SpriteRenderer 1** / MB 1 = 19 |
| `<E>/Sprite Mask` → `SpriteMask` | en=**1**，mat `Sprites-Mask.mat`（built-in fileID **10757** = `Sprites/Mask`） | 同左 | 同左 |
| `<E>/Sprite Mask` → `SpriteRenderer` | en=**0**，mat `Sprites-Default.mat`（built-in fileID **10753** = `Sprites/Default`） | 同左 | 同左 |
| 该节点 Transform | scale `(22.0870, 30.2240, 20.3647)` pos `(0.0125, 0.0188, 0.1250)` | 同左 | 同左 |
| 全部渲染器 `m_MaskInteraction` | **0 / 0 / 0 …（全 0）**（原版原始 typetree 核过，不是 typed-read 猜的） | 全 0 | 全 0 |
| PSR 逐条（路径 / name / enable / `m_SortingOrder`） | Trait Icon=0/sord1 · Glow 1/0 · Ring 1/0 · Shine Square 1/0 · Stars 1/−1（`m_SortingLayerID 2007638115`）—— 与导出**逐条相同** | 同左（Ring 用 `Mobile/Particles/Additive`） | Trait Icon 1/1 · Glow 1/0 · Particle System 1/0 —— 相同 |

**差异：无。** 三类候选逐一排除：
「原版有而我们没有」= 0（mask 节点在）；「我们多出来」= 0（`SpriteMask` 恰好 1 个、`SpriteRenderer` 恰好 1 个且 en=0）；
「enable 状态不同」= 0；「shader 换过」= `Sprites/Default` / `Sprites/Mask` 两张表都是映射到**自己**（`EffectExporter.cs:58-59`），没换。
`EffectIso.cs:208/:701` 关 SpriteMask 这件事**扫描根本没走到** —— `EffectSweepBatch.RenderAt:347` 不碰 SpriteMask，
所以台账那 8 个采样点是**遮罩开着**渲的；但两侧同样开着 ⇒ 依旧不构成口径差。

## ③ 35 条的偏亮/偏暗比例与 `|ln|` 分布

- 口径：台账 `判定` ∈ {偏亮, 偏暗} 且 `效果_shader对账.tsv` 的原 shader 列表含 `Sprites/Default` → **117 条命中，其中 E 判定 35 条**。
- **偏亮 19 / 偏暗 16**（≈ 54% / 46%）。整池 E 组是 84 亮 / 67 暗（55.6% / 44.4%）—— **方向分布与全池一致，没有偏亮倾斜**。
- `|ln|`：min 0.347 · p25 0.443 · **中位 0.509** · p75 0.800 · max 3.189；`|ln|>0.8` 只有 **8 条**，`>1.0` 只有 **4 条**。
  对比全池 E 组的中位 `0.560` —— **这 35 条不比别的 E 更糟，只是压线过阈（带是 0.7–1.4）的数量多。**
- 面积 vs 强度分解（取两边 lit 都 ≥60 的时刻，`area = lit_e/lit_o`，`intensity = (sum/lit)_e ÷ (sum/lit)_o`）：

  | 组 | n | area 中位 | intensity 中位 | `area` 越阈占比 | `intensity` 越阈占比 |
  |---|---|---|---|---|---|
  | 本族 E（35） | 35 | **1.033** | 1.048 | **25.7%** | **62.9%** |
  | 其余 E（116） | 116 | **1.185** | 1.024 | 31.0% | 49.1% |
  | Z「对得上」(580) | 580 | 1.008 | 1.000 | 3.6% | 2.6% |

  ⇒ 本族的分歧是**强度**型的（62.9% vs 25.7%），**不是「多画/少画一块面积」**；全池其余 E 才有明显的面积成分（中位 1.185）。
  ⚠️ 三个样本恰好是本族里最重的，**不代表本族中位**：UM_CardDraw area 2.03/int 1.28 · SAU_CardDraw 2.12/1.42 · FrenziedEffect **1.54/2.32**。
  其中 `FrenziedEffect` 0.75s 处 **lit 1747(原版) vs 1759(导出)、几乎同覆盖**，而线性 sum 1377 → 3564（**2.6×**）——
  **同一片像素更亮**，`lit` 涨的那部分是亮度上去后跨过点亮阈值的边缘像素，不是新几何。

## ④ 机制（**查不到「由 SpriteMask 引起」的机制**，理由如下）

1. **遮罩是死的**：这 3 条所有渲染器 `m_MaskInteraction = 0`（`0=None`），遮罩不裁任何人；`m_IsCustomRangeActive=false`、front/back order 全 0。
   把 `SpriteMask.enabled=false`（`EffectIso` 的做法）对画面**一个像素都不改**。
   ⚠️ 全族里确实有 **77 个原版 prefab 的 PSR 带 `maskInteraction=1|2`**（我导出的 78 个），这些遮罩是真起作用的 ——
   但它们的值**两侧一一相同**，且这 3 个样本**都不在这 77 个里**。
2. **方向不对**：19 亮 / 16 暗。任何「我们多画一层 / 少画一层」的几何差异都会单向偏；实测不偏。
3. **形状不对**：分歧在强度（62.9%），不在面积（25.7%）；`FrenziedEffect` 更是同覆盖、2.6× 强度。
4. **标记本身是伪的**：`Sprites/Default` ⇔ «有 `Sprite Mask` 节点» ⇔ 122 个 prefab，其中 82 个同时是 `Trait Icon` 类。
   控制混淆后仍是这个家族在抬 E 率 —— `TraitIcon=True 且无 mask` 也有 **30.8%**（n=107），`mask=True` **28.7%**（n=122），两者交叉 34.1%；
   而 shader 种数、发射器数（12.9/22.9/15.7/11.1% 非单调）、峰值点数（各组内 mask 仍抬升）、
   「有没有 Extra Color 槽」（740/957 都有，饱和）**都解释不掉**。
5. **真正的锅已另有其名**：这 3 条的强度分歧与 `资料/普查产出_0918/E组根因_B1a.md` 已确立的
   **锅H′**（自建 `WFParticlesExtraColor.shader:157-158` 的 `col.rgb += tex.rgb * _EmissionColor.rgb * IN.color.rgb`
   ⇒ 该槽多加一整份贴图 ≈ +1×）与 **锅A′**（def 的 `texNames` 没有 `_MainTex`/`_BaseMap` ⇒ 贴图落回白）**逐条吻合**：
   UM/SAU 的 `Shine Square`、Frenzied 的 `Trait Icon`+`Particle System` 都落在自建 Extra Color 路径上。

**结论：这 35 条的 3× E 率目前**没有**指向 SpriteMask 的机制，最省事的解释是家族构成
（icon 尺寸、3–5 个发射器、可见输出集中在 1–2 个 sprite 槽 ⇒ 只要那个槽在锅H′/锅A′ 上，整图比值就整体抬到 2–3×；
15 个发射器的大爆炸把同一个 +1× 稀释掉就落回带内）。
⚠️ 这一条**只是相容，不是证明**：发射器数单独看并不能复现 3×。

## ⑤ 修法 / 验证建议（不要动 SpriteMask）

1. **别把 `Sprites/Default` 当根因桶**。它只是「有 Sprite Mask 节点」的索引，改它不会动 E 率。
   真要分桶，用 **`fam`（`per_effect_tech.json`）+ 发射器数 + 可见主导槽的 shader** 三维。
2. **动手改的是锅H′/锅A′**（材料层）：`Assets/WarpforgeVFX/Shaders/WFParticlesExtraColor.shader:157-158` 的 `+=` 项。
   `_EmissionColor` 在**当前构建里 110/122（mask）与 741/835（非 mask）都有 ≥0.99 的值**，即锅H′ 处处在，所以它对 E 率**不区分**，只能解释「有多亮」。
3. **一次性把「家族 vs 机制」判开（最便宜的实验，串行跑一次 `EffectSweepBatch.Run`，只扫本族 122 个）**：
   在 `EffectSweepBatch.RenderAt:347` 起的渲染里加一行
   `foreach (var sm in inst.GetComponentsInChildren<SpriteMask>(true)) sm.enabled = false;`（照抄 `EffectIso.cs:208`），
   **只跑 orig/exp 两趟的这 122 个**（`sweep_targets.txt` 里列 prefab 名）。预期：**数字纹丝不动**（④·1 已证明遮罩是死的）。
   若真没动 ⇒ 从此可以把 SpriteMask 从嫌疑名单划掉，把力气全压在材料层。
4. **判「家族 vs 机制」的正规做法**：把对照组做成**同发射器数 ±1、同「落在自建 Extra Color 上的槽数」**，
   再重算本族 E 率。若 3× 消失 ⇒ 是构成混淆；若仍剩 ≈2× ⇒ 才值得回头专查这 122 个（那时重点看 `Trait Icon` 子树的粒子模块，不是遮罩）。
5. **顺手核账（`EffectExporter.cs:337-345 / 818-820`）**：那两处注释说的「实测 **78** 个 prefab 有被遮罩的粒子」，
   与本文 **77（原版）/ 78（导出）** 那个「PSR 带 `maskInteraction≠0`」的数**是同一件事，互相印证** ——
   注释里那句机制（「`VisibleInsideMask` 的粒子完全靠 SpriteMask 的精灵裁形」）**只对那 78 个成立**，
   本族另外 44 个（含这 3 个样本）遮罩是死的。
   导出侧已核：**133/133 个 `SpriteMask` 的 `m_Sprite` 都非空**（没有精灵漏导）。
