# C · `dump_shader_blob.py` 修复（按名字找 shader 的盲点）+ UIEffect 的 DXBC 已可 dump

> 2026-10-25 · 执行代理 C。改 1 个工具文件（`Unity/工具/dump_shader_blob.py`，+21/−2，LF 未翻）。
> 本报告**订正了「0 命中」的根因**（与调度台转述的那条**不完全一样**，见 ①）。

---

## ① 根因订正：**不是「按 `m_Name` 索引」** —— 工具本来就在读 `m_ParsedForm.m_Name`

- 原代码 `:125` 就是 `name = (pf.m_Name if pf else "") or ""` ⇒ **名字这一半一直是对的**。
- 真正的盲点 = **它只扫 3 个文件**（`shaders_assets_all.bundle` + 我们重打包的两份 `wf_shaders*.bundle`），
  而**内置 shader**（含 `Hidden/UI/Default (UIEffect)`）在 **`Warpforge_Data` 的序列化文件**里
  （`globalgamemanagers` / `globalgamemanagers.assets` / `resources.assets` / `sharedassets0.assets`）⇒ **恒 0 命中**。
- ✅ **调度台那条「`m_Name` 是空串、真名在 `m_ParsedForm.m_Name`」本身是对的**（本轮实测 **202/202 全空**，见 ③）——
  只是工具**早就这么读了**，所以那条不是本次 0 命中的原因。两条一起记，避免下次又归错因。

---

## ② 改了什么（`Unity/工具/dump_shader_blob.py`）
1. **新增 `BUILTIN_FILES`**：`globalgamemanagers` / `globalgamemanagers.assets` / `resources.assets` / `sharedassets0.assets`
   （路径 = `AA` 上溯 **3** 层到 `Warpforge_Data`；⚠️ `AA` 在 `<Warpforge_Data>/StreamingAssets/aa/StandaloneWindows64`，上溯 2 层是 `StreamingAssets`，**别数错**）。
2. **扫描列表 = `BUNDLES + BUILTIN_FILES`**（`--list` 会打出**来自哪个文件**）。
3. **名字匹配两个都比**（`m_ParsedForm.m_Name` 优先、`m_Name` 兜底），`--list` 两列都打（实测全是 `m_Name=<空>`）。
4. **新增 `--list`**：只列名字（不读 blob，秒回）。

---

## ③ 三项验证（调度台要求的三条，逐条有输出）
1. **`UIEffect` 能查到** ✅
   `dump_shader_blob.py "UIEffect" --list` → `Hidden/UI/Default (UIEffect) | m_Name=<空> | globalgamemanagers.assets`
2. **回归：3 个原本能查到的仍能查到** ✅（都在 `shaders_assets_all.bundle`）
   `Particle Distortion` → `Everguild/FX/Particle Distortion Affect Transparents` ·
   `Eclipse Tau` → `Shader Graphs/Eclipse Tau` · `UI/Additive` → `UI/Additive`
3. **全库统计（决定过去有多少次查找是白找的）** ✅
   | 文件 | Shader 数 | `m_Name` 空 |
   |---|---:|---:|
   | `resources.assets` | 21 | 21 |
   | `sharedassets0.assets` | 2 | 2 |
   | `shaders_assets_all.bundle` | 45 | 45 |
   | `wf_shaders.bundle`（我们重打包） | 45 | 45 |
   | `wf_shaders_extra.bundle`（我们重打包） | 42 | 42 |
   | **合计** | **202** | **202（100%）** |
   ⇒ **所有 Shader 资产的 `m_Name` 都是空串**，名字**只能**从 `m_ParsedForm.m_Name` 取。
   过去「按名字找不到某个内置 shader」的尝试，**成因是「没扫内置文件」，不是「名字取错」** —— 两件事都要记住。

---

## ④ 顺带：**`UIEffect` 的 DXBC 已经能 dump 了**（Shiny 那一步的前置通了）
`dump_shader_blob.py "UIEffect"` 现输出：
- **字节码 446,148 字节**（魔数 `21 00 00 00`）；
- **41 个纹理/常量名**，Shiny 全家都在：
  `_TransitionWidth` · `_TransitionSoftness` · `_TransitionRate` · `_TransitionReverse` ·
  `_TransitionColor` · `_TransitionColorFilter` · `_TransitionColorGlow` ·
  **`_TransitionTex`** · `_TransitionTex_ST` · `_TransitionTex_Speed` · `_TransitionAutoPlaySpeed`。
- 🔴 **`_TransitionRotation` 不在常量表里** —— 而 UIEffect 组件上明明有 `m_TransitionRotation = 139`
  ⇒ **旋转很可能不是 shader 常量、而是被烘进 `_TransitionTex_ST`（UV 矩阵）** —— 这一条**下一轮读 DXBC 的第一个要确认的点**。
- `_EdgeShiny*`（`_EdgeShinyRate/Width/AutoPlaySpeed`）也在 ⇒ 这一族还有「Edge Shiny」变体，别与 Transition Shiny 混。

---

## ⑤ 结论（写进方案 §九）
1. **按名字找 shader 一律走 `m_ParsedForm.m_Name`**（`Shader.m_Name` 对**全部 202 个** shader 都是空串）。
2. **找 shader 必须把 `Warpforge_Data` 的四个内置序列化文件一起扫**（`globalgamemanagers` / `globalgamemanagers.assets` / `resources.assets` / `sharedassets0.assets`）—— 内置 shader 不在任何 bundle 里。
3. 本工具现在**两个口都能用**：`--list`（秒查名字+来源文件）· 默认（dump DXBC + 常量名）。

---

## ⑥ 下一步（回到 Shiny 那 5 步，顺序不变）
读 DXBC 的 `TRANSITION_SHINY` 分支（先确认旋转是否经 `_TransitionTex_ST`）→ 写 `CardPresentation/Shaders/WildcardShine.shader` →
接线 `effectFactor` 1 s 线性 0→1 → 占位贴图先跑通 → 断言。**P2，排 P0/P1 之后。**
