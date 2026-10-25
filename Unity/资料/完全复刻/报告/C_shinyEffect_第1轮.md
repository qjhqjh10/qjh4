# C · `shinyEffect`（万能牌徽记「闪一下」）· 参数已读全，实现卡在 shader / 贴图

> 2026-10-25 · 执行代理 C。**本轮无代码改动**（只读 + 落账 + 本报告）。
> 结论：**参数全读到了**，但照判据实现还缺**两件我们没有的东西** ⇒ 按你的规矩「读不到/缺件就写清差什么，别猜」，**没有硬做**。

---

## ① 读法（按你给的路子，一次就通）
1. `WildcardDisplay` 的 MB（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-9188051066216764495.json`）里
   `shinyEffect = PPtr{ m_FileID: 0, m_PathID: -3271889046122976335 }`（**同包引用**）。
2. 该 pathID 直接命中 `MonoBehaviour_-3271889046122976335.json` = **`UIEffectTweener`**（参数见 ②）。
3. 顺着它的 `m_GameObject`（`-2571570060004938831`）找兄弟组件：先按 `RectTransform_8749932977937198001` 反查到 GO（父 `955468390103264177`），
   再在 GO 的组件表里拿到 **`UIEffect`**（`MonoBehaviour_-7763153431244756047.json`，`m_Script` 与 tweener 不同）—— 参数见 ③。
   > ⚠️ 那个 **GO 本身没有以 pathID 命名的 JSON**（`grep -rl "<pid>"` 只在它的组件文件里命中）⇒ 别用「按 pid 找 GameObject」这条，**用它的 RectTransform 反查**。

## ② `UIEffectTweener`（驱动那一半）
| 字段 | 值 | 含义 |
|---|---|---|
| `m_Duration` | **1.0** | 时长 1 秒 |
| `m_Delay` | 0 | 无延迟 |
| `m_Curve` | **(0,0) → (1,1)**，两端斜率各 1 | **线性 0→1** |
| `m_SeparateReverseCurve` | 0 | 反向用同一条曲线 |
| `m_WrapMode` | **0** | **播放一次**（不循环） |
| `m_PlayOnEnable` | 0 | 不在 OnEnable 自动播（由代码 `Play(resetTime:true)` 打） |
| `m_ResetTimeOnEnable` | 1 | 启用时把时间归零 |
| `m_Direction` / `m_UpdateMode` | 0 / 0 | — |

## ③ `UIEffect`（被驱动那一半 —— 一个 **Shiny 过渡**）
| 字段 | 值 | 含义 |
|---|---|---|
| **`m_TransitionFilter`** | **4** | **Shiny**（Coffee UI Effects 的 `TransitionFilter.Shiny`） |
| `m_TransitionRotation` | **139°** | 斜扫角度 |
| `m_TransitionWidth` | **0.213** | 光带宽度 |
| `m_TransitionSoftness` | **0.533** | 光带软边 |
| `m_TransitionKeepAspectRatio` | 1 | 保长宽比 |
| `m_TransitionRate` | 1.0 | 速率 |
| `m_TransitionColor` | **(2,2,2,1)** | **超白**（乘 2 ⇒ 叠加发光感） |
| `m_TransitionColorFilter` | 6 | 颜色滤镜档 |
| `m_TransitionAutoPlaySpeed` | 0 | **不自动播**（由 tweener 驱动 `effectFactor` 0→1） |
| `m_BlendType` / `m_SrcBlendMode` / `m_DstBlendMode` | 1 / 1 / 10 | 混合档 |
| **`m_TransitionTex`** | **PPtr `m_FileID: 14` / `m_PathID: -6023190078349834598`** | **外部引用** ⇒ 那张 shine 贴图 |

## ④ 为什么没实现（缺两件，都不是「猜一猜」能补的）
1. **Coffee `UIEffect` 的 Shiny 着色器** —— 我们工程**没有**这个 shader 家族（我们的 quad 走自建 `WPFX`/`ImageQuad` shader）。
   照判据做 = **先把它移植过来**；`工具/dump_shader_blob.py` 能取 DXBC（项目里已经用它读过 shader），但那是**一笔独立的活**（不是「接一根线」）。
2. **那张 shine 贴图**（`m_FileID: 14` 的外部引用）—— 要解 `bundle_menus_assets_all` 那份 serialized file 的 **externals 表**才能定位 bundle 与文件；
   本仓已知「`m_FileID` 按 externals 解」的做法（`资料` 里记过），但**对象级解析要另做一步**。
⚠️ 用我们的加法 quad 做一条「白条横扫」**是可行的近似**，但那是**发明观感**（角度/软边/颜色都会不一样）⇒ 按你「读不到别猜」的口径**没做**。

---

## ⑤ 需要你裁（二选一）
- **(a) 移植那个 Shiny shader**（+ 定位 shine 贴图）—— 忠实，但要一整轮（读 DXBC → 写 shader → 对拍）。
- **(b) 用加法 quad 近似**（我们的渲染：白色斜带 + 1 s 线性 + 139°/0.213/0.533 尽量对上）—— 快，但**是偏离**，要你在报告里留痕（用户看到的是「差不多闪一下」）。
👉 我倾向 **(a)**（这项目已有 shader 对拍的工具链：`dump_shader_blob.py` + `EffectCompare`），但**等你拍板**。
⛔ 在那之前 `WildcardDisplay.OnEnable` **保持 C**（账里已把 ②③ 两组参数与「缺什么」写死）。

---

## ⑥ 顺带（可复用）
- **拿 GO 的 pid 找它的 GameObject**：`GameObject/` 里**不一定**有按 pid 命名的文件 ⇒ 可靠办法 = **扫 `RectTransform/`（或 `Transform/`）里 `m_GameObject.m_PathID == pid` 的那份**，再从它的 `m_Father` 往上走（本次一次命中）。
- `UIEffectTweener` 的**参数**好读（就那几个字段），真正麻烦的是**它驱动的 UIEffect 着色器 + 外部贴图** —— 下次遇到同类「闪一下/光扫」，先确认这两件在不在，再决定要不要开工。

---

## ⑦ 追加：裁定 (a) 忠实移植 —— 着色器**已定位**（并就地订正一个假结论）

### ⑦·0 🔴 就地订正（铁律 5）：我上一版写的「这一包没导出 `Image`」**是假的**
**错因**：`bundle_menus_assets_all` **确实没有 `Image/` 目录（0 个）**，但 **UI 的 `Image` 组件全在 `MonoBehaviour/` 里**
（那一包有 **6,876 个带 `m_Sprite` 的 MonoBehaviour`）。判据 = `ls …/MonoBehaviour | grep -l '"m_Sprite"'`。
⇒ **老规矩：读 UI 组件要去 `MonoBehaviour/` 按 `m_GameObject`/`m_Script` 反查，不是找 `Image/` 目录。**
（同一族的假结论本项目犯过一次：**「主菜单包没导出 Image」**。以后遇到「某个类型没导出」先想它是不是个 `MonoBehaviour` 子类。）

### ⑦·1 按正确找法重走一遍，**Shader 找到了**
1. 闪光那个 GO = `GameObject/Content_-2571570060004938831.json`；
2. 在 `MonoBehaviour/` 里按 `m_GameObject.m_PathID == -2571570060004938831` 反查到 **5 条**，其中
   **`MonoBehaviour_5058986616577207217.json` 带 `m_Sprite` ⇒ 就是那颗 `Image`**；
   它的 **`m_Material` = `m_PathID 0`（= 默认 UI 材质）** ⇒ **材质上没有 shader** —— 因为 Coffee 的 `UIEffect`
   是**运行期自建材质**、按名字 `Shader.Find` 取 shader（所以材质引用是空的，不是「没导出」）。
3. 全库按名字找 ⇒ **`globalgamemanagers/Shader/Shader_77.json`**，其 `m_ParsedForm.m_Name` = **`Hidden/UI/Default (UIEffect)`** ✅
   （`m_Name` 那一格是空串，**这就是 `dump_shader_blob.py "UIEffect"` 0 命中的原因** —— 它按 `m_Name` 索引）。
   该资产带 **`compressedBlob` / `compressedLengths` / `decompressedLengths` / `offsets` / `stageCounts`** ⇒ **DXBC 在**。
4. 它的 **pass 状态**（`Shader_77.json` 的 `m_ParsedForm.m_SubShaders[0].m_Passes[0].m_State`）：
   `_SrcBlend` / `_DstBlend` / `_ColorMask` / `unity_GUIZTestMode` **全是「材质属性驱动」**（`val 0` + `name` 指向属性名）
   ⇒ **混合档是数据驱动的**，与 UIEffect 里那组 `m_BlendType 1 / m_SrcBlendMode 1 / m_DstBlendMode 10` 对上。

### ⑦·2 下一步（下一轮第一件事，只剩「读 DXBC + 写 shader」）
1. **`dump_shader_blob.py` 按 `m_Name` 索引 ⇒ 对这份「`m_Name` 空、真名在 `m_ParsedForm`」的资产 0 命中**
   ⇒ 给它加一条**按资产路径/pathID** 的入口（或直接用 `m_ParsedForm.m_ProgramMask` + `compressedBlob` 手解）；
2. 解出 DXBC 后**按关键字变体裁 `TRANSITION_SHINY` 那一段**，抄出「`_TransitionRotation 139°` / `_TransitionWidth 0.213` / `_TransitionSoftness 0.533` / `_TransitionColor (2,2,2,1)` → UV 光带」的式子；
3. 写 `CardPresentation/Shaders/WildcardShine.shader`（**⛔ 不放 `WarpforgeVFX/**`**），参数名照原版；
4. 接线：`SetWildcardArmy` 换旗时 `effectFactor` **1 s 线性 0→1**（`m_WrapMode 0` 播一次）；
5. 贴图：`m_TransitionTex` = `m_FileID 14 / m_PathID -6023190078349834598`（外部引用）—— 解不出就**先用占位把参数链跑通**，账里写「shader 已移植、贴图待 external 解析」；
6. 断言：因子 0→1（1 s）+ shader 参数逐值 = prefab。

> 📌 **P2**，排所有 P0/P1 之后；不会挤掉别的活。

---

## ⑧ `_TransitionRotation` 到底走哪条路 —— **第一步确认：它不是着色器常量**（硬证据）

调度台要求「先确认它是不是烘进 `_TransitionTex_ST`（UV 矩阵）· ⛔ 别把 139 硬编码进 shader」。本轮查到：

### ⑧·1 硬证据（三条）
1. **DXBC 常量表里没有 `_TransitionRotation`** —— 修好工具后 dump 出的 **41 个常量**里，Transition 那一族只有
   `_TransitionWidth` · `_TransitionSoftness` · `_TransitionRate` · `_TransitionReverse` · `_TransitionColor` ·
   `_TransitionColorFilter` · `_TransitionColorGlow` · `_TransitionTex` · `_TransitionTex_ST` · `_TransitionTex_Speed` ·
   `_TransitionAutoPlaySpeed` —— **没有 rotation**。
2. **组件上它确实是一个序列化字段**：`m_TransitionRotation = 139.0`，且字段 doc 写着
   **「Effect rotation (0–360). NOTE: This property is shared between `Transition Filter` and `Detail Filter`.」**
   （`/d/2/Warpforge_code/Scripts/Coffee.UIEffect/Coffee/UIEffects/UIEffect.cs:69-72`）
3. **`_TransitionTex_ST` 是「缩放/偏移」那条老语义**：组件上同时有 `m_TransitionTexScale {1,1}` · `m_TransitionTexOffset {0,0}` · `m_TransitionTexSpeed {0,0}`
   ⇒ ST 那 4 个数已被 scale/offset 占用，**不太可能再塞旋转**。

### ⑧·2 还没定死的那一步（下一轮读 DXBC 只查这一件）
旋转只有两条可能的路径：
- **(a) 打进某个向量**（例如 ST 的 2×2 折叠 / 或另有一条我们还没认出的向量常量）；
- **(b) 组件侧算好、改 UV 或顶点**（v5 插件里确实有 `MeshExtensions` / `TransformExtensionsForTransformSensitivity` 这类件）。
⚠️ **v5 插件的 C# 方法体在 `decomp_full` 里是 0 条**（`Coffee_UIEffects_UIEffect*` 一个都没导出）⇒ **只能靠 DXBC**。
👉 **对移植的影响 = 0**：无论 (a) 还是 (b)，**我们的 shader 都把旋转做成一个 uniform**（例：`_ShineRotation`），
由组件侧喂 **`m_TransitionRotation`（139°）**、在 UV 空间绕中心旋转 —— **这就不是硬编码**。

### ⑧·3 顺带抄全的 Transition 参数（写 shader 时直接用）
`m_TransitionFilter = 4 (Shiny)` · `m_TransitionRotation 139` · `m_TransitionWidth 0.213` · `m_TransitionSoftness 0.533` ·
`m_TransitionKeepAspectRatio 1` · `m_TransitionRate 1.0` · `m_TransitionReverse 0` · **`m_TransitionRange {Min 0, Max 1}`** ·
`m_TransitionColor (2,2,2,1)` · `m_TransitionColorFilter 6` · `m_TransitionColorGlow 0` ·
**`m_TransitionGradient`**（8 键，实际只用 2 键：key0 白 → key1 白，`ctime0 0 / ctime1 65535`、alpha 同样）·
`m_TransitionTex` 外部引用（fileID 14 / pathID -6023190078349834598）· `m_TransitionTexScale/Offset/Speed` 全默认 ·
`m_TransitionAutoPlaySpeed 0`。

### ⑧·4 下一步（不变）
读 DXBC 的 `TRANSITION_SHINY` 分支（**只查 ⑧·2 那一件事**）→ 写 `CardPresentation/Shaders/WildcardShine.shader`
（旋转走 **uniform**）→ 接线 `effectFactor` 1 s 线性 0→1 → 占位贴图跑通 → 断言。**P2，排 P0/P1 之后。**
