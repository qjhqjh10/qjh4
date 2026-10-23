# K1 · `新账-66` 的 alpha 口径（立绘透卡框 / 外发光淡出）—— 查证与落地

**日期**：2026-10-24 · **范围**：只做 `新账-66` 的「alpha 口径未定」这一件（tier3/tier4 拱顶实拍仍属真机项，本批不做）。
**结论一句话**：原版是 **`1 − α`（一次乘法）**，⛔ **不是 `(1−α)²`**；我们现有的实现**已经一致**，本轮只订正注释 + 补断言，**没改任何渲染行为**。

---

## 一、结论：原版公式（一手判据）

### 1·1 直接判据 = 原版 shader 的**真反编译体**（不是 blob 反推）

`UI Extensions/SoftMaskShader`（解包资产 `resources/Shader/Shader_35.json`，`m_AssetGUID` → `0406db5a…`；
Ruri 反编译产物 `D:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/UI Extensions_SoftMaskShader.shader`）：

```hlsl
// 片元（省去取样细节，逐字对应反编译体）
float4 _42 = _MainTex.Sample(...);          // 被遮罩的那颗 Graphic
float  _47 = _42.w;                         // 它的贴图 alpha
float  _62 = _47 * COLOR.w;                 // × 顶点色 alpha = a_obj
float4 _98 = _AlphaMask.SampleGrad(...);    // 遮罩图
float  _105 = _98.w;                        // α_mask
float  _119 = (CutOff >= _105) ? 0 : (HardBlend ? 1 : _105);   // CutOff=0 / HardBlend=0 ⇒ α_mask
precise float _125 = 1.0f - _119;                              // ← 这里的 `1−α`
precise float _127 = ((_FlipAlphaMask == 1u) ? _125 : _119) * _62;
precise float _146 = mad(_MaskEffectStrength, mad(-_47, COLOR.w, _127), _62);  // = lerp(a_obj, _127, M)
```

即 **`a_out = a_obj · (1 − M·α_mask)`**（`M = _MaskEffectStrength`，默认 **1**）—— **一次乘法**，整份 shader 里
没有任何平方/自乘。`FlipAlphaMask=1` 只是把 `α_mask` 换成 `1−α_mask` 的那一个 `lerp`。

### 1·2 它作用在**谁**身上（这决定了「会不会乘两次」）

| 事实 | 出处（一手） |
|---|---|
| `SoftMaskScript` 实例挂在 **CardFrame** GameObject 上 | `bundle_staticgeneralassets_assets_all/MonoBehaviour/MonoBehaviour_5590396362908354958.json` → `m_GameObject` = `CardFrame_4761873281729546638` |
| `MaskArea` = **CardImage 的 RectTransform** | 同上 `MaskArea.PathID = -7122465826688546418` → `RectTransform_-7122465826688546418.json` 的 `m_GameObject` → `GameObject/CardImage.json` |
| `AlphaMask` 序列化值 = **null**（151/153 个实例都是；只有 menus 里 2 个非空） | 全量扫 `assets_full/**/MonoBehaviour/*.json`（153 个含 `FlipAlphaMask`） |
| 它把材质挂到 **CardFrame 自己那颗 Image** | `SoftMaskScript__OnEnable.c` → `ToggleMask(1)` → `GetComponent<Image>().material = maskMaterial` |
| 另外挂到 **textBackground** 那颗 Image | `CardImageController__ChangeState.c` 状态 1 分支：`uVar4 = maskScript+0x80(maskMaterial)` → `plVar3 = this+0x50(textBackground)` → `set_material`；字段名见 `CardImageController.cs` 的 `textBackground`（注释原文 *"This is needed so the card image overflows also here"*） |
| **立绘（CardImage）与卡框（CardFrame）是兄弟**，`Front` 子序 `Card Highlight And Shadow(1) → CardImage(2) → Textbackgrounds(3) → CardFrame(4) → Card Info(5) → EffectAnchor(6)` | `RectTransform_-8145770350504326770.json` 的 `m_Children` + `GameObject/*.json` 的 `m_Component` 反查 |

⇒ **立绘既不是被遮罩图层的子节点、也不吃这颗 mask**。那条「立绘会被乘两次 ⇒ `(1−α)²`」的担心**前提不存在**。

### 1·3 那么「立绘在卡框半透明处的可见份额」到底怎么来的

- **层序**：立绘在下（兄弟序 2）、卡框在上（4）；
- **混合**：卡框 = 标准 alpha-over（`Everguild/Card ImageUI` 反编译体是 `Blend SrcAlpha OneMinusSrcAlpha`；
  UI 默认材质同；我们那层 `CardPresentation/CardSprite` 是等价的预乘写法 `Blend One OneMinusSrcAlpha` + `c.rgb *= c.a`）；
- ⇒ **立绘可见份额 = `a_立绘 × (1 − a_框)`，只乘一次**。这就是 `1 − α`，与 SoftMask **无关**。

### 1·4 原版还有一道**硬矩形**裁切（与上式无关，但别混进来）

`Everguild/Card ImageUI` 的片元（Ruri 反编译体 `b561210ac1934488e9c22d3d1c8e18d5/Sub0_Pass0_Fragment_b7.hlsl`）：

```hlsl
SV_Target.w = COLOR.w * clamp(
    ((_Clamp_Range.z >= uv.x) || (uv.x > _Clamp_Range.x)) ? 0 :
    ((uv.y > _Clamp_Range.y)    || (_Clamp_Range.w >= uv.y)) ? 0 : tex.a, 0, 1);
```

= `_Clamp_Range`（UV 空间 `(z, x] × (w, y]`）之外 α=0 —— **硬矩形**，不是按轮廓、也不是渐变。

### 1·5 `SoftMask` 在**这个配置**下实际做了什么（顺手订正旧记录）

`AlphaMask = null` ⇒ 采样**默认白**（shader Properties 声明 `_AlphaMask (…, 2D) = "white"`）⇒ `α_mask = 1` ⇒
`a_out = 0`。而 `_MaskEffectStrength` 由 `CardImageController` 在状态 0/1 之间 `DOFloat` 到 0 / 1
（`CardImageController__DoChange.c` 的 `DOFloat(maskMaterial, endValue, propID)`）。
⇒ 它的实际作用 = **把「它挂到的那颗 Graphic」（CardFrame / textBackground）整层淡没**，
**不是**「用立绘 alpha 给卡框挖洞」。⚠️ 这一条与多份旧文档里写的相反（见 §五 改动清单）。

---

## 二、我们现状：**一致**（渲染零改动）

| 环节 | 我们 | 原版 | 结论 |
|---|---|---|---|
| 层序 | `shadow +0.06 → art +0.03 → textBg +0.015 → frame 0 → info −0.02 → 文字 −0.03`（`CardView.cs` 的 z 约定，越小越靠前） | `CardImage(2) → Textbackgrounds(3) → CardFrame(4)` | ✅ 一致 |
| 立绘层材质 | `CardPresentation/ArtOpaque`（**忽略贴图 alpha，α 当 1**） | `Everguild/Card ImageUI`（**吃**贴图 alpha + 硬矩形） | ⚠️ **已有明写的偏离**（`OpaqueMaterial` 的 doc：668/1126 张立绘是角色抠图，卡面要「完整插图」）；对本条公式的影响 = 我们的 `a_立绘` 恒为 1，原版是 `a_tex` |
| 立绘在框半透明处的份额 | 由 GPU 的 alpha-over 产生 = `1 − a_框` | 同 | ✅ 一致 |
| 探针期望值 | `1f - alpha[idx]/255f` | 同 | ✅ 一致（本轮收口到一个函数里） |
| 是否有第二层遮罩 | **没有**（我们从不建 SoftMask） | 唯一那颗 SoftMask **不作用于立绘** | ✅ 等价 |

**⇒ 渲染没有要改的。** 本轮按任务要求「只订正注释写清判据」。

---

## 三、改动清单

### 3·1 `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs`（CRLF 保持，+80/−22 行左右）

1. **新增 `public static float ArtVisibleThroughFrame(float frameAlpha01)`**（放在 `FrameArtBevelCoverage` 之前）——
   `return 1f - Mathf.Clamp01(frameAlpha01);`，doc 里写全五条判据（层序 / 混合 / SoftMask 真实作用 / 兄弟关系 / 硬矩形），
   并注明「我们这层 ArtOpaque ⇒ 退化成 `1 − a_框`」「**判据只此一处**」。
2. `FrameArtBevelCoverage` 里的 `float exp = 1f - alpha[idx] / 255f;` → `CardView.ArtVisibleThroughFrame(alpha[idx] / 255f)`
   （**公式只留一处**）。
3. **订正 7 处注释**（都是「原版靠 SoftMask/FlipAlphaMask 给立绘挖洞」这一族错口径），逐处加 `⚠️ 2026-10-24 订正`：
   - `:877` 立绘矩形那段（两条原话都错：方向错 + `AlphaMask` 是 null）；
   - `:8555` `FrameArtWindow` 的 doc；
   - `:8651` `FrameBevelOpaquePx` 的 doc（**重写**为「层序 + alpha-over」三条一手判据）；
   - `:9035` `ArtMeshInFrame` 的 `clipWindow` 分支；
   - `:9103` `FrameArtCoverage` 的「判据（原版口径）= SoftMaskScript + FlipAlphaMask」段（**重写**）；
   - `:9197` `FrameArtBevelCoverage` 的「① 立绘贡献 = 1−卡框α（原版 FlipAlphaMask=1 的等价物）」；
   - `:3569 / :3625 / :3674 / :9695`（大图档 4 处，同一错口径的复制）。

### 3·2 `Unity/MyGame/Assets/CardPresentation/Editor/CardBaseDemo.cs`（CRLF 保持，+52 行）

- 新增 `static void AssertArtAlphaThroughFrame()`，并在 `AssertFrameCutout(cam)` 之后调用一次
  （宿主在 `工具/_run_8_checks.sh` 的 12 条里 ⇒ 真会被跑到）。

---

## 四、断言：怎么才会红

| # | 断言 | 改坏法（都会红） |
|---|---|---|
| ① | `ArtVisibleThroughFrame(α)` 对 5 个**常数期望值**（0.5→0.5 · 0.9→0.1 · 0.1→0.9 · 0→1 · 1→0）误差 < 1e-4 | 改成 `(1−α)²`（0.5→0.25、0.9→0.01）· 改成反相 `α` · 改成 `(1−α)³` … |
| ② | 实测卡：`ArtZ > FrameZ`（立绘在卡框**后面**） | 把立绘层挪到框上（z 反过来）⇒ 立绘整块盖住框，不再有 `1−α` 透出 |
| ③ | 实测卡：`ArtShaderName == "CardPresentation/ArtOpaque"`（**不吃贴图 alpha**） | 换成吃 alpha 的材质 / 给立绘加逐顶点 alpha —— 那正是「若确认平方」要走的改法 |

**灭自证**：① 的期望值是**常数**（从「立绘在下、框在上」的标准 alpha-over 直接算出来），不是把实现再算一遍；
②③ 量的是**运行时真建出来的那一层**（`Create` 出来的 CardView 的访问器），不是读常量。
②③ 之所以要一起断：只断公式的话，有人可以把公式留着、却把层序反过来或给立绘加顶点 alpha，①照样绿。

`bash 工具/typecheck.sh` → **运行时 0 错 / 编辑器 0 错**（改动后立即跑过；本批**没跑 Unity 批处理**，按红线）。

---

## 五、没查清的部分（如实列出）

1. **`_MaskEffectStrength` 的运行时终值**只从反编译读到「状态 0 → 0 / 状态 1 → 1」，且 `CARD_IMAGE_CLAMP_RANGE_WHEN_SMALL/BIG`
   两个静态常量的**具体数值**没解（读它们要走 `_DAT_` 常量 + VA→文件偏移，本轮没做，**与 alpha 公式无关**）。
2. **`AlphaMask` 会不会在运行时被别处赋值**：全量反编译里**没找到任何写它的点**（只有 `SoftMaskScript__SetMask` 读它），
   序列化 151/153 为 null ⇒ 按现状是「恒 null」。如果哪天发现运行时赋值，§1·5 的结论要重判（§1·1 的公式**不受影响**）。
3. **「AlphaMask=null ⇒ 绑白」**是 Unity 的标准行为（shader 里声明了 `= "white"`），但**没有在本工程实机渲一张验证**
   （需要 Unity；本轮红线不跑）。
4. **tier3/tier4 拱顶实拍**：仍欠（本批不做，属真机项）。

---

## 六、顺手发现（⚠️ 未改，只记账）

1. **旧文档里「原版大图态 = 用立绘 alpha 给卡框挖洞」这条口径是错的**（不止一处）：
   按 §1·2/§1·5，状态 1 打开 SoftMask 后，**框与文字底板是整层淡没**（`AlphaMask=null` ⇒ α_mask=1 ⇒ 输出 0），
   **不是**按立绘轮廓挖洞。⇒ 我们用 `_artFront`（角色抠图压在框上）近似它，**与原版那一步不完全等价**（原版那一步框直接没了）。
   本轮只在注释里加了「⚠️ 订正 + 与原版不完全等价」，**没改实现**（那是大图档的事，不在 `新账-66` 范围）。
   影响面需要单独评估：`CardImageController.ChangeState(1)` 在**本 build 里只被 `CardImageControllerTester`（调试组件）**
   调用（`grep ChangeState` 命中的唯一运行期调用点），`CardDisplayWindow.ShowBattleCard` 走的是 `ChangeSize(Small, instant)` ⇒
   大图态在这版游戏里可能**根本没被产品路径用到** ⇒ 我们的 `_artFront` 是「给大图档好看」的近似，不是复刻。
2. **立绘的硬矩形 `_Clamp_Range`（`0.18..0.82 × 0..1`）我们没有复刻**：原版立绘在 UV 上被硬裁成中间 64% 宽；
   我们靠「条带贴合卡框开口」达到等效可见范围（甚至更贴轮廓）。二者在**框的半透明斜面**处结果相同（都靠 `1−α`），
   在**框外**区域不同（原版可能露出 0，我们的条带直接不画）。⚠️ 若以后要逐像素对拍大图态，这一条要单独核。
3. `Shader_35.json` 的 `m_Name`（顶层）是空串，真正的名字在 `m_ParsedForm.m_Name` —— 按名字找 shader 时别读错字段。
