# `B-246` · `WFUIBorderHighlight.shader` 剩下的 5 处与原版不一致（写手 `W-13`）

> 写手 `W-13` · 2026-11-01。**只改了一个文件**：`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`。
> 没跑 Unity（`-executeMethod` 全禁）· 没动 git · 没碰 `.mat` / 账 / 活状态表 / 正本 / 场景 / 工程设置 / 别人的文件。
> 备份（改前原样）：**`d:/4/_tmp_view/b114_w13/WFUIBorderHighlight.shader.bak`**（215 行 / 13,896 B / LF）。
> 判据：`判据/B114_R5_Reveal_DXBC.md` ＋ **本次现读**原版 shader 的 `m_ParsedForm`（`m_PropInfo` / `m_KeywordNames` /
> `m_KeywordFlags` / 4 个 pass 的 `m_State` / 编译产物组合）＋ 本机 URP 17.3.0 源码（消费者判定）。

---

## 〇 一句话

5 条里 **2 条改了代码**（① `Cull Off → Cull Back`、④ 删非原版属性 `_RendererColor`）、
**3 条判完**（② `shader_feature_local` **本来就与原版一致**、③ `_Color` **无需补**（调用方已由 W-8 换成顶点色）、
⑤ 原版另外 3 个 pass **本工程 0 消费者**，不补）。
顺带就地订正了文件头一条错记：**pass2/3 不是「另一份 unlit + colMask=0」，而是 `DepthNormalsOnly` / `SHADOWCASTER`**。

---

## 一 逐条对照表

### ① `Cull Off` vs 原版 `Cull Back` —— ✅ **改了**

| | 内容 |
|---|---|
| **原版判据** | `Shader_-4463273156108355430.json` → `m_ParsedForm.m_SubShaders[0].m_Passes[0].m_State.culling.val = 2`（= `Cull Back`）；**4 个 pass 全是 2**。 |
| **我们现状（改前）** | `shader:78` `Cull Off`。 |
| **结论** | **照原版改成 `Cull Back`**（现 `shader:90`）。 |
| **「不会掉面」三条证据（这条必须先证，否则改完整块消失是静默的）** | ① 本工程 quad 的三角形序 = `{0,2,1}` `{0,3,2}`（`Battle/ImageQuad.cs:500`），顶点序 BL→BR→TR→TL ⇒ **顺时针**；uGUI `Image.AddQuad` 是 `{0,1,2}` `{0,3,2}`、顶点序 BL→TL→TR→BR（包缓存 `com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:1470-1476`）⇒ **同为顺时针**。Unity 的正面 = 屏幕空间顺时针 ⇒ 我们与原来的 uGUI 网格**正面同向**。② 唯一消费者 = `Shell/CardDisplayEffects.cs` 的升级边框，父链 `root.localScale = Vector3.one`（同文件 `:67`），无镜像/负缩放。③ 本 shader 只有 1 份材质（`Resources/Art/vfx/Border Effect For Level up.mat`，全仓按 shader GUID `8d5ed6ff6f6883649b60650e625a9160` 只命中它），只被这条链用。 |
| **残留风险（照原版承担）** | 若卡体被翻到背面，这层边框会不可见 —— **原版同样如此**（它也是 `Cull Back`）。⛔ 别为了「看得见」改回 `Cull Off`。 |

### ② `shader_feature_local` ⇒ 出包裁变体 —— ✅ **判完：原版也是 feature（会被裁），我们已一致，不改**

| | 内容 |
|---|---|
| **原版判据（现读 JSON）** | `m_ParsedForm.m_KeywordNames`：`[15]=_DEBUG` · `[16]=_AUTOROTATION` · `[17]=_FLIPDIRECTION`。<br>`m_ParsedForm.m_KeywordFlags`：**这三条都是 `1`**；同表 `[18]=UNITY_UI_CLIP_RECT` / `[19]=UNITY_UI_ALPHACLIP` **也是 `1`**（uGUI 源码里那两条是 `multi_compile_local`）⇒ **flag 1 = local 档**。<br>**决定性证据（不依赖 flag 语义）**：pass0 的编译产物里这三个关键字的**去重组合只有 4 种**：`()` · `(_DEBUG)` · `(_DEBUG,_AUTOROTATION)` · `(_AUTOROTATION,_FLIPDIRECTION)`（8 种可能只有 4 种）—— **恰好等于 5 份材质 `m_ValidKeywords` 的并集**（`[]` ×2 · `_AUTOROTATION,_FLIPDIRECTION` · `_AUTOROTATION,_DEBUG` · `_DEBUG`）。<br>⇒ 原版是 **「出包按材质裁掉未用变体」的 feature**；若用 `multi_compile`，8 种会全在。 |
| **我们现状** | `shader:107-109` 三条 `#pragma shader_feature_local`（W-7 就是照原版写的）。 |
| **结论** | **不改**（已与原版等价），只把这组证据补进注释（`shader:104-106`）。<br>⚠️ 工程内**没有任何** `EnableKeyword` 打在这颗 shader 上（`MenuFxMaterials` 那几处开关键字的是**原版 bundle shader**，走 `WarpforgeShaderMap.TryResolve`，不经过本文件）⇒ 裁变体不会造成「编辑器里能开、出包开不了」。 |

### ③ `ImageQuad.SetTint` 写 `_Color`，而原版这颗 shader 没有 `_Color` —— ✅ **判完：不给 shader 补 `_Color`（原版确实靠顶点色）**

| | 内容 |
|---|---|
| **原版判据** | ① `m_PropInfo.m_Props` **19 条**（16 条用户属性 + `unity_Lightmaps` / `unity_LightmapsInd` / `unity_ShadowMasks`），**没有 `_Color`**（也**没有 `_RendererColor`**，见 ④）。② 原版 VS `o2 = v4`（**顶点色 `COLOR`**，判据 `B114_R5` §2·1）。③ 原版 PS 末句 `mul o0.xyzw, r0.xyzw, v2.xyzw`（**× 顶点色**，判据 §2·2 ⑧）。 |
| **我们现状** | shader 也**没有** `_Color`（与原版一致）。**调用方已经是顶点色通路**：`Shell/CardDisplayEffects.cs:326` `if (im.hasAmp) quad.SetVertexTint(tint);`（**W-8 本轮已改**，`im.hasAmp` 正是 `Level Up Effect` / `Level Up Effect Down` 两件）。<br>`ImageQuad.SetVertexTint`（`Battle/ImageQuad.cs:775`）= 开通道 + 推色；`SetTint` 在通道开时走 `ApplyTintColor` → `Mesh.colors`（`:783-795`），**不写 `_Color`**。<br>`UIBorderGlow.ChangeGraphicAlpha` 走 `_quad.SetTint`（`Shell/UIBorderGlow.cs:136-152`）⇒ 在顶点色通道下**落点正确**（而且顶点色在 `AnimateUIMaterialProperty` 换材质后仍然在网格上，这正是它该走顶点色的原因）。 |
| **结论** | **shader 不改**（加 `_Color` 反而多一条原版没有的属性，且会与顶点色重复相乘）。<br>⚠️ 仍然成立的一条纪律：`ImageQuad.SetTint` 的**通用路**（通道没开时）写 `_Color`（`ImageQuad.cs:794`）—— 任何**将来**把 quad 换成这颗材质的代码都必须用 `SetVertexTint`，否则静默落空。已写进 shader 文件头 ⑥。 |

### ④ `_RendererColor` 非原版属性 —— ✅ **判完：不是兼容层（零消费者）⇒ 删**

| | 内容 |
|---|---|
| **原版判据** | `m_PropInfo` 19 条里**没有 `_RendererColor`**；原版 VS 是 `o2 = v4`（**不乘**任何 `_RendererColor`）。 |
| **消费者普查** | 全仓 `_RendererColor` 的**写点 = 0**：没有任何 `SetColor("_RendererColor")` / `material.SetColor` / MaterialPropertyBlock 写它；唯一那份 `.mat` 也没有这条属性；其余命中全是 `Sprites/Default` 一族 / 战场贴图 / `OriginalShaderBlendTable` 里**别的 shader** 的条目。<br>而且 `_RendererColor` 是 **SpriteRenderer** 的 per-renderer 量（Unity 自动填），我们的渲染件是 **MeshRenderer**（`ImageQuad.Create`）⇒ 那条填充路径根本不存在。 |
| **结论** | **它是 `Sprites/Default` 的残留，不是我们需要的兼容层（零消费者、默认白 = 恒等、删掉零行为变化）⇒ 按本条判据删掉**：属性声明 / `float4 _RendererColor;` / `v.color * _RendererColor` 三处。<br>现 `shader:149` = `o.color = v.color;`（逐字等于原版 `o2 = v4`）。 |

### ⑤ 原版 4 个 pass，我们只有 pass0 —— ✅ **判完：另外 3 个本工程 0 消费者，不补（反证见 §五）**

| | 内容 |
|---|---|
| **原版判据（现读 JSON，逐个 pass 的 `m_State.m_Tags` + `m_State`）** | **pass0** = 主渲染（无 `LIGHTMODE`；`culling 2` · `zTest 4` · `zWrite 0` · `colMask 15` · `Blend 5/10, 1/10`）。<br>**pass1** = `LIGHTMODE=MOTIONVECTORS`（`zWrite 1` · `colMask 12`）。<br>**pass2** = `LIGHTMODE=DepthNormalsOnly`（`zWrite 1` · `colMask 15`）。<br>**pass3** = `LIGHTMODE=SHADOWCASTER`（`zWrite 1` · **`colMask 0`**）。 |
| **我们现状** | 只有 pass0；pass0 状态除 `Cull` 外逐项对齐（`ZWrite Off` · `ZTest LEqual` · `Blend` · `ColorMask` 默认 RGBA=15）。 |
| **结论** | **不补**：这 3 个 pass 在本工程**结构上进不去 / 触发不了**（逐条反证见 §五）。⛔ 别为了「文件长得像原版」把 3 个永不执行的 pass 塞进来（只会多编译变体、并且给后人「我们有深度法线/运动矢量」的错觉）。<br>**恢复条件**：若将来本 shader 被改到**不透明队列**、或渲染件把 `shadowCastingMode` 打开，这三条要重新评估。 |

> 📌 **铁律 5 就地订正**：W-7 报告 §四·4 与 shader 旧文件头把 pass2 写成「另一份 unlit」、pass3 写成「`colMask=0`」；
> **实测 pass2 = `DepthNormalsOnly`、pass3 = `SHADOWCASTER`**（`colMask=0` 是 pass3 的属性）。已在 shader 文件头 `:41-42` 改对。
> W-7 那份报告**不归我改**，请调度台同步。

---

## 二 改动 + 净增净减

- 文件：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`
- **215 行 → 228 行**，**净 +13**；行尾仍 **LF**（`CRLF=0`、`bareCR=0`、无 BOM）。
- **不在任何 `账/*.tsv` 的落点里**（`grep -rl WFUIBorderHighlight Unity/资料/完全复刻/账/` = 0 命中）⇒ 行号变化**不造成账漂移**。
- 逐条落点：

| 改动 | 位置（改后） | 说明 |
|---|---|---|
| `Cull Off` → `Cull Back` | `:90` | 原版 `culling.val = 2`；附「顺时针=正面」判据注释 |
| 删 `_RendererColor` 属性 | 原 `:71`（现无） | `Properties` 里不再声明 |
| 删 `float4 _RendererColor;` | 原 `:116`（现无） | |
| `o.color = v.color * _RendererColor` → `o.color = v.color` | `:149` | 文本逐字等于原版 `o2 = v4` |
| 关键字注释补证据 | `:104-106` | 判据写进注释（pragma 本身**没动**） |
| 文件头「已知差异」整节重写 | `:40-60` | ① pass 名单纠正 + 无消费者结论；② Cull 已改；③ 关键字结论；⑥ `_Color` 纪律；⑦ `_RendererColor` 已删 |

---

## 三 哪些需要别人接手

0. 🔴 **`typecheck` 现在红着 1 条，但不是我的文件**：`Assets/CardPresentation/Shell/ItemDrawerPartial_B114W12.cs(47,29)`
   `CS0102: 类型「ItemDrawer」已经包含「NodeCampaignGlow」的定义`。该文件 mtime = **17:00:27**（我开工后新出现的），
   归 **W-11 / W-12**。我在自己改完后第一次 `typecheck` 是 **0/0**（当时这个文件还不存在），之后我只动过 shader
   ⇒ 按「错误集中在不是自己负责的文件上 ⇒ 那不是你的问题」处理，**⛔ 我没去改它**。请调度台转给 W-12。
1. **无必须项**（第 ③ 条的调用方改动 W-8 已经在 `CardDisplayEffects.cs:326` 做完，我这边只做判定）。
2. **建议**（不阻塞）：请调度台订正 `报告/B114_W7_边框高亮shader.md` §四·4「原版 4 个 pass 只做了 pass0 … pass2 是另一份 unlit」
   —— 现读 pass2 = `DepthNormalsOnly`、pass3 = `SHADOWCASTER`。
3. **同步点建议验一次**（需要 Unity 窗口，我跑不了）：材质换到 `Border Effect For Level up` 之后，
   `Level Up Effect` / `Level Up Effect Down` 两件的**染色与 α** 是否还在（它们现在走顶点色）。顺带看
   `Cull Back` 下这两块**正常显示**（不掉面）。

---

## 四 没查清的部分（如实写，⛔ 没猜测）

1. **`m_KeywordFlags` 的 bit 语义没有官方文档作证**（本机只有二进制资产）。「1 = local」是从
   「`UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP` 也是 1，而 uGUI 那两条是 `multi_compile_local`」**反推**的。
   但「原版是 feature（出包会裁）」有**独立硬证**（4/8 组合 = 材质并集），不依赖这条反推。
   ⇒ 无论 flag 语义如何，本轮结论不变：**我们已经是 local + feature，且工程内 0 处 `EnableKeyword` 打在这颗 shader 上**。
2. **原版 pass1/2/3 各自的变体数/裁剪情况没逐个统计**（只数了 pass0 的 VS/PS 组合）。不影响「本工程无消费者」。
3. **原版 `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP` 在原始使用场景里到底有没有生效**（原版父链上有没有
   `RectMask2D` / `Mask`）**没查**。见 §六·1。
4. **`Cull Back` 的实际观感没在 Unity 里看一眼**（红线：不跑 Unity；`typecheck` 不编 shader）。

---

## 五 原版 4 个 pass 的消费者反证全表（第 ⑤ 条）

**本工程事实（现读）**：URP `17.3.0`（`Packages/manifest.json:8`）· `GraphicsSettings.asset:88` 挂 `PC_RPAsset` ·
`PC_Renderer.asset:69` `m_RenderingMode: 2`（Forward+）· `:23` `m_RequireDepthTexture: 1` ·
渲染器特性 = `ScreenSpaceAmbientOcclusion`（`m_Active: 1`、`Source: 1` = DepthNormals）+ `GrabPassTransparent`。
本 shader 的**队列 = Transparent**（SubShader `Queue=Transparent`；`.mat` `m_CustomRenderQueue: -1`；运行期
`CardDisplayEffects.SetLayerRenderQueue` 设 **3115**）。

| 原版 pass | 消费者判据 | 本工程有没有 |
|---|---|---|
| **pass1** `LIGHTMODE=MOTIONVECTORS` | URP `Runtime/Passes/MotionVectorRenderPass.cs:35`：`m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, opaqueLayerMask)` ⇒ **只画不透明队列**。且运动矢量本身还要相机要求（默认 volume 里 `MotionBlur` 的 `intensity: 0` / `mode: 0` = CameraOnly，`Assets/Settings/DefaultVolumeProfile.asset:79-92`）。 | ❌ **没有**（队列被排除） |
| **pass2** `LIGHTMODE=DepthNormalsOnly` | URP `Runtime/UniversalRenderer.cs:364`：`new DepthNormalOnlyPass(..., RenderQueueRange.opaque, prepassLayerMask)` ⇒ **只画不透明队列**。SSAO（`Source=DepthNormals`）虽开，但 SSAO 也只作用于不透明像素。 | ❌ **没有**（队列被排除） |
| **pass3** `LIGHTMODE=SHADOWCASTER` (`colMask 0`) | 阴影投射由渲染件的 `shadowCastingMode` 决定；唯一渲染件由 `Battle/ImageQuad.cs:136` 建，**显式 `shadowCastingMode = Off`**；全链（`CardDisplayEffects` / `UIBorderGlow` / `AnimateUIMaterialProperty`）**没有任何地方改回 On**。 | ❌ **没有**（渲染件不投影） |
| **pass0** 主渲染 | 无 `LIGHTMODE` ⇒ URP 走 `SRPDefaultUnlit`，透明队列画。 | ✅ 有（我们就是这一支） |

> ⚠️ 我**没有**逐条去查「将来会不会有人把这块 quad 改成不透明 / 开阴影」—— 那是恢复条件，已写进 §一⑤ 与文件头 ①。

---

## 六 顺手发现（⛔ 一条都没动手改）

1. ⚠️ **原版有 `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP` 两个 local 关键字（`m_KeywordNames[18]/[19]`、flags=1），我们没有**
   ⇒ 若将来这颗 shader 被 RectMask2D / 裁切链罩住，我们**不会裁**（原版会）。
   当前唯一消费者是 `2DCard` 下的四件 MeshRenderer（`CardDisplayEffects`），**不在 Canvas 遮罩链下**；
   但原版 `Card Display` 祖先上有没有 `RectMask2D` **我没查**（§四·3）⇒ 是否要补这两个关键字，留给调度台裁定。
2. ⚪ **原版 SubShader 标签**是 `RenderPipeline=UniversalPipeline` + `UniversalMaterialType=Unlit` + `ShaderGraphTargetId=…`；
   我们的是 uGUI 那套（`Queue/RenderType/IgnoreProjector/PreviewType/CanUseSpriteAtlas`，**没写 `RenderPipeline`**）。
   这是「两条管线都能跑」的刻意差异（不在本次 5 条内），本轮**未动**。
3. ⚪ `_QueueOffset`（默认 0）/ `_QueueControl`（默认 −1）/ `HideInInspector` 已与原版逐值一致（`m_PropInfo` 现读），不用改。
4. ⚪ W-7 报告 §四·4 / shader 旧头的 pass 名单有误（见 §一⑤ 末尾的订正说明）。
