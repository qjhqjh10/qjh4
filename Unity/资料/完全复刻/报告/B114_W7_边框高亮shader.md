# B-45 · `WFUIBorderHighlight.shader` 的 `_Reveal` 链按 DXBC 重写（写手 `W-7`）

> 写手 `W-7` · 2026-11-01。**只改了一个文件**：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`。
> 没跑 Unity（`-executeMethod` 全禁）· 没动 git · 没碰 `.mat` / 账 / 活状态表 / 正本 / 场景 / 工程设置。
> 判据只有一个：`Unity/资料/完全复刻/判据/B114_R5_Reveal_DXBC.md`（原版 DXBC 逐指令还原）。

---

## 〇 一句话

原文件头自己写着「本文件是**属性表兼容的替身，不是原版像素级复刻**」+ `🔶 自己设计（非原版）：噪声取模、揭开方向、柔化曲线都是我们挑的`。
这一版把那句话**删掉**了：片元/顶点两条链都按 DXBC 逐指令重写，`🔶 自己设计` 标记在本文件里**归零**（两处缺名字的常量按「推断（非判据）」标记，⛔ 不是自设计）。

---

## 一 逐条对照表（判据 → 原实现 → 新实现）

### ① 片元：`_Reveal` 那条链（判据 §2·2 ⑦ / §2·3）

| | 内容 |
|---|---|
| **判据** | `m = 1 − smoothstep(_Reveal − _Smoothness, _Reveal + _Smoothness, ang)`；`_Smoothness` = **绝对半宽**（整圈 = 1.0，软边全宽 = 2S）；`ang = v5.w = atan2(q.y,q.x)·0.159236`，值域 **(−0.5, +0.5]**、**不 +0.5**；`_FLIPDIRECTION` 变体只是把 `v5.w` 取负。 |
| **原实现** | `shader:103` `ang = atan2(c.y,c.x)/6.2831853 + 0.5`（0..1 的角）；`shader:116-118` `reveal=(_Reveal+1)*0.5` · `soft=max(_Smoothness,0.001)` · `opened=saturate((reveal*(1+soft)-ang)/soft)` —— 「按比值展开 + **相对**柔化」。 |
| **新实现** | 角度在 VS 产出（新 `:130-166`，`:164` = `atan2(q.y,q.x)*0.159236`）；片元 `:197-203`：`ang = i.ang` → `_FLIPDIRECTION` 取负（`:199-201`）→ `m = 1.0 - smoothstep(_Reveal - S, _Reveal + S, ang)`（`:203`）。 |
| **差异后果（判据 §四① 点名的那条）** | 原来的 `reveal=(R+1)/2` 会让**边界整体偏一个角、软边宽度错 2 倍**；现在边界中心角**正好等于 `_Reveal`**。 |

### ② 顶点：角度坐标在 VS 里算（判据 §2·1）

| | 内容 |
|---|---|
| **判据** | `p = 屏幕UV − uv0.zw`（VS-A/VS-C）或 `uv0.xy − 0.5`（VS-B）；`ang_deg = _StartOffset (+ _Rotation) (+ 360·frac(t·自转))`；`rad = radians(ang_deg)`；`q = (p.x·c + p.y·s, −p.x·s + p.y·c)`（**顺时针**）；`o.ang = atan2(q.y,q.x)·0.159236`。`_StartOffset` = CB1 byte 32、**只在 VS 读**、单位度。 |
| **原实现** | 全在片元里：`shader:102-103` `c = i.uv - 0.5` + `atan2/2π + 0.5`（以**元素 uv 中心**为原点，不是视口坐标）；`shader:104` flip 用 `1−ang`；`shader:105` `rot = _Rotation + _Time.y*(_AUTOROTATION? _AutoRotationSpeed : 0)`（**度/秒与秒混用**，再 `*0.01` 塞进噪声 uv）；`_StartOffset` **完全没进算式**。 |
| **新实现** | `:130-166`。`appdata.uv0` 改成 `float4`（`:101`）以拿 `.zw`；`:139` `sp = ComputeScreenPos(pos).xy/.w`；`:142-156` 三分支（`_DEBUG` / `_AUTOROTATION` / 默认）；`:158-165` 度→弧度 → 顺时针旋转 → `atan2 * 0.159236`。`v2f.ang` 走 `TEXCOORD2`（`:110`）。 |
| **旧注释订正（铁律 5）** | 旧 `shader:106-109` 写着「`_StartOffset` **不进算式** …… 原版像素代码不读它」——**是错的**。判据 §三·6 / §五·1：它在 `progVertex.m_CommonParameters` 里**按名绑在 byte 32**，VS 里两处读它。新文件头 `:29-30` 已就地订正成「**不进的是片元那 10 条指令**，它决定「缝」在哪」。 |

### ③ 颜色链：原版整条补上（判据 §2·2 ①～⑥）

| | 内容 |
|---|---|
| **判据** | ① 屏幕 UV → ② 噪声 UV = `(sp.x, 1−sp.y)·z − 0.5`，再绕中心转 `frac(t·y)` 圈，`+0.5` → ③ `nz = tex2D(_Noise_R,nuv).r·2−1` → ④ Pulsating Scale `s = 0.5(1−x)(sin(t·y)+1)+x`，`uv2 = s(uv−0.5)+0.5`，`uv2 += nz·_Noise_Strength.x`（**同一个标量加到 x 和 y**）→ ⑤ `c = tex2D(_MainTex, uv2)` → ⑥ `k = smoothstep01(saturate((c.r−off.x)/(off.y−off.x)))`，`col = lerp(LinearToSRGB(_ColorSecondary.rgb), LinearToSRGB(_ColorMain.rgb), k)`；**边框形状来自 `c.a`**。 |
| **原实现** | **从来没采样 `_MainTex`**（声明了但 frag 一次 `tex2D` 都没有）；`shader:110-112` 噪声 uv = `ang·_Scale_X_Speed_Y.x + rot·0.01` / `_Time.y·y·0.01 + length(c)·z`（语义全不对）；`shader:121` 自己拿 `edge = saturate(1−(0.5−length(c))·2)` 造**径向衰减**当边框；`shader:124` `lerp(_ColorMain,_ColorSecondary,saturate(n)) + _Color_offsets.rgb`（拿**噪声**当插值因子、`_Color_offsets` 当颜色加）；`shader:125` `col *= 1+_Add`；**没有 LinearToSRGB**。 |
| **新实现** | `:171` 屏幕 UV · `:173-178` 噪声旋转 · `:181` 噪声采样 · `:183-187` Pulsating Scale（`.x` 当**缩放下限**、`.y` 当弧度/秒、`.z` 当屏幕缩放）· `:190` 采 `_MainTex` · `:193-195` 红通道两档阈值 + `LinearToSRGB` 双色插值 · `:207-208` `c.a` 参与输出。`LinearToSRGB` 按原版 10 条指令实现（`:122-128`：`log2·0.4166667 → exp2 → ·1.055−0.055`，`≤0.0031308` 走 `·12.92321`）。 |

### ④ 属性表（判据 §三·5/10、§五·2/3；默认值另用原版 `m_PropInfo` 现读校对）

| 属性 | 原版（`m_PropInfo` 现读） | 我们原来 | 现在 |
|---|---|---|---|
| `_Range` | **不存在** | `Float = 89` | **删** |
| `_RangeStartOffset` | **不存在** | `Float = 196` | **删** |
| `_Add` | **不存在** | `Float = 0.09` | **删** |
| `_ColorMask` | **不存在** | `Float = 15` | **删** |
| `_Reveal` | `Range [−0.5, 1] = 1` | `Range(-1,1) = 0.723` | `Range(-0.5,1) = 1`（`:64`） |
| `_Smoothness` | `Range [0,1] = 0.05` | `Range(0,1) = 0.032` | `Range(0,1) = 0.05`（`:65`） |
| `_Noise_R` | 默认贴图 **`"white"`** | `"gray"` | `"white"`（`:59`） |
| `_StartOffset` | `Type 3`（Range）`[0, 360] = 0` | `Float = 0` | `Range(0,360) = 0`（`:63`） |
| `_ColorMain` | `(1, 0.887489, 0, 1)` | `(1,1,1,1)` | 照抄（`:55`） |
| `_ColorSecondary` | `(1, 0, 0, 1)` | `(1,1,1,1)` | 照抄（`:56`） |
| `_Color_offsets` | `(0.66, 1, 0, 0)` | `(0,0,0,0)` | 照抄（`:57`） |
| `_Noise_Strength_X_Speed_Y_Scale_Z` | `(0.01, 1, 1, 0)` | `(0.04, 0, 8.17, 0)`（抄的是**材质值**） | 照抄（`:60`） |
| `_Scale_X_Speed_Y` | `(0.9, 3, 0, 0)` | `(1,1,0,0)` | 照抄（`:62`） |
| `_DEBUG` / `_AUTOROTATION` / `_FLIPDIRECTION` | 带 `[Toggle(关键字)]`；默认 1 / 0 / 0 | 无 `Toggle`、默认 0/0/0 | 照抄（`:58/61/67`） |
| `_AutoRotationSpeed` | `1` | `-180` | `1`（`:68`） |
| `_QueueOffset` / `_QueueControl` | HideInInspector，默认 `0` / `−1` | 无 HideInInspector，默认 0/0 | 照抄（`:69-70`） |
| `_MainTex` | 不在 `m_PropInfo`，但 `progFragment.m_TextureParams` **按名绑 t0/s0** | `[PerRendererData]` 声明（从没采样） | 保留声明，**现在真采它**（`:54` / `:190`） |
| `_RendererColor` | **原版没有** | `[HideInInspector]` 默认白 | 保留（默认白 = 恒等；见 §五·6） |

> ⚠️ 标「照抄」的那 5 行是**超出任务单第 4 条点名的三格**的额外对齐 —— 判据是同一份原版 `m_PropInfo`（逐条 `m_DefValue` 现读），
> 目的是让「新建材质的出厂观感」也等于原版。**只有一份 `.mat` 用这颗 shader，且它把这 5 个值全显式写了 ⇒ 对现网零影响**（见 §五·2）。
> 如果调度台认为超出本轮范围，回退这 5 行不影响第 1/2/3/5 条任何一条。

### ⑤ 输出 / 混合（判据 §2·2 ⑧、§三·13）

| | 内容 |
|---|---|
| **判据** | 输出**预乘**：`o.rgb = col·c.a·m·col_v.rgb`，`o.a = c.a·m·col_v.a`；`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`（**不是加色**）。 |
| **原实现** | `shader:58` `Blend SrcAlpha One`（**加色**）；`shader:126` `return fixed4(col*i.color.rgb, a*i.color.a*_ColorMain.a)`（α 里还多乘了 `_ColorMain.a`，原版没有）。 |
| **新实现** | `:82` `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`；`:207-208` 逐字照判据。 |

---

## 二 改了哪些行 + 净增/净减

- 文件：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`
- **132 行 → 215 行**；`git diff --numstat` = **160 增 / 77 删 / 净 +83**。
- 行尾保持 **LF**（`CRLF=0`、`bareCR=0`、无 BOM）· 本文件**不在任何 `账/*.tsv` 的落点里**（已 `grep` 过）⇒ 行号变化不会造成账漂移。
- 结构性改动：
  | 旧 | 新 | 说明 |
  |---|---|---|
  | `:16-20` 「替身，不是像素级复刻」+ `🔶 自己设计` | `:18-49` | 换成「这一版做了什么 / 两处推断 / 已知差异」三节 |
  | `:12-14` 把**材质值**当成 shader 默认值写进头注释 | `:9-16` | 改成原版 `m_PropInfo` 的 `m_DefValue` 真值（铁律 5 就地订正） |
  | `:37-45` 4 条原版不存在的属性 | 删除 | 见 §一·④ |
  | `:58` `Blend SrcAlpha One` | `:82` | 见 §一·⑤ |
  | `:70-81` appdata/v2f（2 分量 uv、无 ang） | `:98-111` | uv0 改 `float4`；新增 `screenPos` / `ang` |
  | `:99-127` 整个 frag | `:168-209` | 按 §2·2 的 ①～⑧ 重写 |
  | `:106-109` 「`_StartOffset` 不进算式」 | 删除（订正进 `:29-30`） | 铁律 5 |
  | — | `:130-166` 新 vert | 角度坐标改到 VS 算（含三分支） |

---

## 三 哪些是【推断】（⛔ 不是判据）

判据 §三·❓1/❓2：原版 `UnityPerMaterial` 的 **byte 88 / byte 92** 两格**没有名字作证**（RDEF 段被剥；serde 里 unmapped）。
本波**实现了它们**，并在代码里按任务要求打了固定标记（可 `grep "推断（非判据）"`）：

| 格 | 推断成 | 依据（判据原文） | 落点 |
|---|---|---|---|
| byte 88 = `cb1[5].z` | **`_Rotation`** | VS-A 里 `_StartOffset + cb1[5].z` 再 `×0.017453`（度→弧度）⇒ 唯一「角度」候选 | `:153-155` |
| byte 92 = `cb1[5].w` | **`_AutoRotationSpeed`** | VS-C 里 `360·frac(t·X)` 的 `X` ⇒ 唯一「速度」候选；唯一走 VS-C 的材质值 `−0.5` 按「圈/秒」正好 2 s 一圈 | `:151-152` |

**没有任何 `🔶 自己设计` 标记** —— 这两格是有证据、缺名字，不是我们编的数值（任务单第 2 条明确要求）。

另有两处**等价实现**（不是推断、也不是自设计，写进文件头 `:44-47`）：
- 屏幕 UV：原版在片元里由 `SV_POSITION.xy / _ScaledScreenParams` 重算；我们用 VS 的 `ComputeScreenPos` **齐次坐标逐像素除 w**（平面 quad 下逐像素同值），`_ScreenParams` 代替 URP-only 的 `_ScaledScreenParams`。
- `_TimeParameters.x`（URP 全局，内置 CG 里**没有声明** —— 已 `grep UnityCG.cginc` 确认）⇒ 用 `_Time.y`（同为「秒」）。
- `tex2D_bias(_GlobalMipBias)` → 内置 CG 的 `tex2D`（同一次采样的隐式 mip 选择）。

---

## 四 没查清的部分（如实写，⛔ 没猜测）

1. **`_FLIPDIRECTION` 在【顶点段】有没有副作用 —— 测不出来**（判据 §三·❓3：本地只编译过 `_AUTOROTATION + _FLIPDIRECTION` 这一种组合，自转与翻转分不开）。
   ⇒ 本实现只在**片元**取负（那是确定的那条），顶点段**不加**任何翻转项。若将来差分实验证明 VS-C 里还有一拍翻转，要回来补。
2. **`o4.w = 2·length(q)`（径向距离）算出来没人用**（判据 §三·❓5）⇒ 本实现**不输出**它（PS 的 INTERP3 是未使用寄存器）。
3. **`_QueueOffset` / `_QueueControl` 具体落在 CB 的哪一格**（判据 §三·❓4 说无法证明）⇒ 只按原版默认值/`HideInInspector` 声明，**不参与任何算式**（与旧版一致）。
4. **原版 4 个 pass 只做了 pass0**（任务单 ③ 明确「这一条本波不做」）⇒ 已写进文件头 `:41` 作为待办线索。
5. **`_DEBUG` 与 `_AUTOROTATION` 同开时**（原版材质 `Border Gacha Button Glow` 就是这个组合）：判据只说 VS-B **没有** `_Rotation` 与自转 ⇒ 本实现让 `_DEBUG` 分支**优先**（`:142-145`），即同开时也不自转。这一条**是从「VS-B 只有 `_StartOffset`」直接推的，没有单独的 VS-B + 自转 变体可对**。

---

## 五 顺手发现（⛔ 一条都没动手改）

1. 🔴 **「边框形状」现在真的来自 `_MainTex.a` 了，而我们那条链喂的是 1×1 白图 ⇒ 观感会从「径向环」变成「整块亮」**。
   链路：`CardPresentation/Shell/CardDisplayEffects.cs:302` 用 `CardArt.Solid()`（`Core/CardArt.cs:743`，**1×1 白、α=1**）建 quad；
   `:313-316` 原版有 sprite 时把 tint 的 α 置 0；但 `:385` 的 `AnimateUIMaterialProperty` 把材质换成
   `new Material(Border Effect For Level up.mat)`（`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs:300/330`）⇒ **那笔 α=0 的 `_Color` 丢了**，
   `_MainTex` 退回 shader 默认 `"white"` ⇒ `c.a = 1`。
   ⇒ 旧 shader 靠 `:121` 自造径向衰减画出一个「环」；新 shader 照原版走 sprite alpha，**白图 ⇒ 整块矩形**。
   **这不是本波引入的错误，是「把机制改对之后，缺的那张 sprite 露出来了」** —— 要真出边框，得补原版那张 sprite（或换一张带 alpha 边框的贴图）。
   是否要为此单独立账，请调度台裁定。
2. **唯一一份用这颗 shader 的材质**：`Assets/CardPresentation/Resources/Art/vfx/Border Effect For Level up.mat`（全仓 `grep` shader GUID `8d5ed6ff6f6883649b60650e625a9160` 只命中它）。
   它把 `_Reveal -0.5` / `_Smoothness 0.032` / 三组颜色 / `_Noise_Strength` / `_Scale_X_Speed_Y` **全显式写了**，`m_ValidKeywords: []`
   ⇒ §一·④ 那些默认值改动、以及 `_DEBUG` 默认改 1，**对现网零影响**（材质值优先）。
3. **`ImageQuad.SetTint` 写的是 `_Color`，而原版这颗 shader 没有 `_Color`** —— 换到本材质之后任何 `Tint`/`ChangeGraphicAlpha` 写入都会丢
   （`UIBorderGlow.ChangeGraphicAlpha` 走的就是 `ImageQuad.Tint`）。原版靠的是**顶点色**（`COLOR`），我们这颗 shader 也读顶点色 ⇒
   正确通路应是 `ImageQuad.SetVertexTint`（`B112W_B08` 已给 `MenuFxMaterials.ApplyProto` 用了这一条，但 `CardDisplayEffects` 这条链**还没换**）。
   旧 shader 同样没有 `_Color` ⇒ **不是本波回归**。
4. **`shader_feature_local` 的变体可能被裁剪**：`_AUTOROTATION` / `_FLIPDIRECTION` / `_DEBUG` 目前**没有任何材质在开**，
   `shader_feature_local` 在出包时会把这些变体裁掉；若将来有运行期 `EnableKeyword`（现在 `MenuFxMaterials` 是对**原版 bundle shader**
   的材质开关键字，不经过我们这颗）就需要改成 `multi_compile_local`。原版这三个也是 feature 关键字，本波照原版，未改。
5. **`_MainTex` 不在原版 `m_PropInfo` 里，但 `progFragment.m_TextureParams` 按名把它绑到 t0/s0** ——
   说明原版是**靠 uGUI 的 per-renderer 纹理**喂进去的（`[PerRendererData]` 那一类），Unity 导出 `m_PropInfo` 时没列出来。
   我们把它写进 `Properties` 是**本工程载体需要**（`ImageQuad` 走 `Material.mainTexture`），不算「多出来的属性」。
6. `_RendererColor` 是原版没有的（`Sprites/Default` 一族残留）。本波**保留**（默认白 = 恒等，全仓没有任何地方 `SetColor("_RendererColor")`），
   以免动到既有染色路径；要彻底对齐原版可以删，但那是另一件活。
7. **原版 `Cull Back` / 我们 `Cull Off`**：本波**没改**（判据 §三·14 只是记事实，不在任务单的 5 条里）。理由：本工程大量 quad 用镜像缩放，
   改成 `Cull Back` 会让朝向相反的那些**整块消失**（静默）。已写进文件头 `:42` 作为已知差异。

---

## 六 编译验证

- `TMPDIR=/tmp/wf_w7 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  ⚠️ **typecheck 只编 C#，覆盖不到 shader**。
- 额外做的人工通读（逐项核过）：预处理块配对（`_DEBUG` 外层 / `_AUTOROTATION` 内层 3 层 `#if/#else/#endif` 配平）·
  所有用到的 uniform 都声明了（`_MainTex/_Noise_R` 两个 sampler；`_ColorMain/_ColorSecondary/_Color_offsets/_Noise_Strength…/_Scale_X_Speed_Y/_RendererColor` 六个 float4；
  9 个 float）· 内建函数签名（`sincos(x,out s,out c)` / `atan2(y,x)` / `radians` / `frac` / `exp2` / `log2` / `lerp` / `smoothstep` / `saturate`）·
  `v2f` 语义不冲突（`SV_POSITION` + `TEXCOORD0/1/2` + `COLOR`）· `Blend` 两段式语法 · `[Toggle(...)]` 与 `shader_feature_local` 同名对应 ·
  `float4 uv0 : TEXCOORD0` 在 2 分量网格上取 `.zw` = 0（Unity 缺省补 0）· 写回后行尾仍为 LF、无 BOM。

> **编译验证只能等 Unity 窗口**（本波红线：不跑 `-executeMethod`）。调度台开窗口时请跑一次 `B110_W5_ShaderSupport_Assert.RunCli`
> （它只断言「文件能 Load 成 Shader / 名字对得上 / `Shader.Find` 取得到」，**不跑 shader 编译器**），
> 真正的编译错要到**编辑器里实际编译一次**（或任一用到这颗 shader 的宿主）才会现形。
