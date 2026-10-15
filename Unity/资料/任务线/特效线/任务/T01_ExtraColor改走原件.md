# T01 · `Everguild/FX/Extra Color` 整条改走原件 —— **本线目前最大的一件**

> **状态**：`✗ 未做`（**判据齐**，但**动手前要先补两格** —— 见 §六）
> **关联 A 编号**：⚠️ **暂无账号**（本线的账不走 A 表；2026-10-15 现核）
> **判据全集** → `资料/普查产出_第二十三会话/V23_特效ExtraColor的PS读数.md`（§一 结论 · §二 判据 · §三 为什么旧 A/B 不能判 · §四 该做的事 · §五 没查清）
> **待办正本** → `项目任务.md` §三 第 4 条「⏭ 还开着的」**第 1 件** · 正本 `资料/特效还原_进度与交接.md` §十四 末节 第 1 条（`:1818-1825`）

---

## 一、要做什么

把 `Everguild/FX/Extra Color` 从 `Replacements`（**落到我们自建那份**）挪进 `UseOriginal`（**走原版编译件**）。
**做完之后画面有什么不同**：用它那一族的效果 —— **741 处引用 / 一次性动 ~974 条效果** —— **不再系统性偏暗**。

---

## 二、判据

1. **原件就在随包 `wf_shaders.bundle` 里、且带编译字节码**（2026-10-12 实读，`V23` 本笔）。
2. 🔴 **今天它不在白名单** —— `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs`
   `:200-201` 白纸黑字写着：
   > `⏸ 暂缓的两个大头（改它们会一次性动 ~974 条效果，要单独一批 + 一次全量 sweep 量过再动）：`
   > `` `Everguild/FX/Extra Color`（741）· `Everguild/FX/Particle Distortion Affect Transparents`（233）``
   而 `:208` 把它映到 `WarpforgeVFX/Particles/Extra Color`。
3. 🔴 **根因 = 一份自建顶了 8 个原版 shader** —— `V23` §2·4：`Replacements` 里**指向 `WarpforgeVFX/Particles/Extra Color` 的原版名**共
   9 条（`:208 / :262 / :264 / :265 / :280 / :290 / :291 / :297 / :298`），其中 `Particles/Additive` 是**死条目**：
   `Everguild/FX/Extra Color` · `Everguild/FX/Particle Premultiply` · `Everguild/FX/TrailShader_Fading` ·
   `Shader Graphs/Doomweaver effect` · `Shader Graphs/Eclipse Tau` · `Mobile/Particles/Additive` ·
   `Mobile/Particles/Alpha Blended` · `Particles/Standard Unlit`（＋死条目 `Particles/Additive`）。
   ⇒ 我们**按内置族的算式**写了那唯一一份，而 `Everguild/FX/Extra Color` 那一族**多一跳**。
4. **那一跳是什么，已逐条读完**（`V23` §2·2 与 §2·3）：**32/32 个 VS 变体**都写 `o2 = LinearToSRGB(cb2[1]) × vcol`，
   Universal Forward 的 **12 个 PS 变体（去重 3 种形状）全都把 `v2`（= `o2`）乘进最终色**
   ⇒ **最终色 = `tex × LinearToSRGB(_Color) × vcol`**。（`cb2[1]` **就是 `_Color`**。）
5. 🔴🔴 **旧的那次 A/B 不能用来判这一条**（`V23` §三，**这条比结论本身重要**）：
   2026-10-02 用的判据是 `|ln(自建 ÷ 白名单)|`，而它**只对「白名单里的名字」有分辨力** ——
   对不在白名单的 `Everguild/FX/Extra Color`，**正常态与 `WFBIND_FORCE_BUILTIN=1` 两趟都落到我们那份**
   ⇒ 那个比值**恒等于 1、看不见对错**。
   ⇒ 「`>0.10` 15 → 126」量的是「**我们那份 shader 变了多少**」，**不是「离原版近了多少」**。
   ⇒ **有效的尺子是另外两把**（都拿**原版侧**做基准）：`EffectCompare`（`WFCMP_ISO=all` / `MATDUMP`）·
   `EffectSweepBatch` 的 `sweep_orig.tsv` ↔ `sweep_exp.tsv`。
6. **两条候选路**（`V23` §四，按判据强弱排）：
   | # | 做法 | 成本 |
   |---|---|---|
   | **①** | **加进 `UseOriginal` 白名单** —— 直接绑原版编译件 = **零猜测、精确复刻那一跳**，741 处一次到位 | 小（改名 + 一次 A/B） |
   | **②** | 退一步：给它**单开一份自建**（算式 `tex × LinearToSRGB(_Color) × vcol`），**别与内置族共用一份** | 新 shader + **两处**映射表 + 一次小批对拍 |
   | ③ | 两族共用那份的 `_Color` **保持不动**（现状）—— ⛔ **别再单独乘一次**（那正是把内置族弄坏的那一手） | — |

---

## 三、我们现状

- `WarpforgeShaderMap.cs:208`：`{ "Everguild/FX/Extra Color", "WarpforgeVFX/Particles/Extra Color" }` ⇒ **走我们那份**。
- `WarpforgeShaderMap.cs:200-201`：明确标着「⏸ 暂缓」。
- `UseOriginal` 白名单现读共 **59 条**，**不含** `Everguild/FX/Extra Color`。
- 我们那份 `.mat` 里 `_Color` **157/658 非白**（`(4,4,4,1)` ×18 · `(2,√2…)` ×9 · `(766.9961,…,0.6588)` ×4 …）—— `V23` §五。
- 🔴 **效能范围要点明**：这一族**几乎全在 `UseOriginal` 里** ⇒ 绑原件后**只在正常态生效**；
  **兜底路**（原件包不在场、`TryResolve` 落到 `Replacements`）**仍走我们那份** —— 所以 ①② 两条都要**两条路各看一眼**。

---

## 四、怎么改（**分三步，先做第 1 步**）

1. 🔴 **先证「不品红」** —— 跑 `ShaderFallbackProbe`（`MyGame/Assets/WarpforgeArena1/Editor/ShaderFallbackProbe.cs`）。
   **为什么必须先证**：`TryResolve:434-441` 记着「bundle 里的 URP shader 在编辑器下渲不出来」那条
   （症状 = 品红）。**这一格 `V23` §五 明写「没跑」。**
2. 🔴 **A/B** —— `EffectCompare`（**小批** + `WFCMP_ISO=all`；要逐属性再叠 `WFCMP_MATDUMP=1`）。
   ⛔ **别再用 `|ln(自建 ÷ 白名单)|`**（见 §二 第 5 条）。
3. **落改** —— 走 ① 就把名字挪进 `WarpforgeShaderMap.cs` 的 `UseOriginal`；
   走 ② 就**新开一份 shader**，并把**两处映射表同步改**（运行时的 `WarpforgeShaderMap` + **导出期的 `EffectExporter.ShaderMap`**）——
   ⚠️ 「两份不同步」是本线的老坑（正本 §〇之三 判据 ② 记过同款）。

⚠️ **这是一件共用件改动** ⇒ 按本线 §三 硬规矩：**单独一批** + 改完**立刻**跑 `bash d:/4/Unity/工具/typecheck.sh`。

---

## 五、做完了怎么验（**照现行口径**）

**现在**：只跑秒级类型检查（`bash d:/4/Unity/工具/typecheck.sh`）。
**量差异**（属「复刻的输入」，不是验收）：`EffectCompare` 对拍 → 需要全量口径时重跑 `EffectSweepBatch` 两趟
（`WFSWEEP_SIDE=orig` 先、不设后）→ `analyze_sweep.py` 重生 **`资料/特效还原台账.tsv`**。
🔴 **数字只看台账那两处**（台账 + `兜底路审计_1002j.tsv`），**别往任务文件里抄数**。

---

## 六、还差什么 / 判不准的

1. 🔴 **`_Color` 在运行时到底以什么值到达 shader —— 没读到**：
   `battleprefabs_vfxandmisc_assets_all.bundle` 里按 `m_Shader.read()` 筛 `Extra Color` **命中 0 个**
   ⇒ **材质不在那个包里**（`V23` §五；`D:/4/_tmp_view/vfx23/ec_mats.py` 留着，**下轮换包再跑**）。
2. 🔴 **「`_Color` 是 HDR 还是非 HDR 属性」没读** —— 决定上传时**做不做 sRGB→线性转换**
   （`m_PropInfo.m_Props[0].m_Flags` 的第 16/32 位）。**这是 ①/② 落地前该补的一格。**
3. **`LinearToSRGB(_Color.a)` 参与 `o2.w`** —— 我们的 `col.a` 路径**完全没管 `_Color.a`**；
   ①/② 落地时要连这条一起判（**今天不可观测**）。
4. **「VS 里为什么会有那条乘法」仍未读通**（正本 §十四 末节 第 1 条自带这个尾巴）。
5. ⛔ **别再试第三次「把 `LinearToSRGB(_Color)` 乘回共用那份 shader」** —— 试过，`>0.10` **15 → 126**，明确退步。
6. ⚠️ **仓库里现存的 `sweep_*.tsv` 已不可用于本笔** —— `资料/比对基线/README.md` 自己写着
   「**仓库里现在这份 TSV 是旧定义 —— 两趟都要重扫**」。`V23` 离线试算过（`EC-only` 组均值 `ln(exp/orig) = −0.0564`、
   中位 `−0.0088`；`no-EC` 组均值 `−0.0160`、中位 `0.0000`）⇒ **方向对（EC 那族偏暗）但幅度被旧口径稀释到判不了**，
   **不拿它下结论**，只留作线索。
7. **`Everguild/FX/Particle Distortion Affect Transparents`（233）** —— 与 EC **同在 `:200-201` 那句「暂缓的两个大头」里**，
   **不在本件范围内**；它另有一笔真 bug（属性表里没有混合相关属性、混合写死在 pass 状态里）见
   `资料/特效还原_进度与交接.md` §P1-a0 末段。
