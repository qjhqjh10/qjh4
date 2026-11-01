# B116_W11 判据：`uv1` 编解码 · Shadow 支 · `_TextureSampleAdd` · Gradation/Detail

> 只读现核，2026-11-02。**产物**：`CardPresentation/Resources/Shaders/WildcardShine.shader`（原版 `Hidden/UI/Default (UIEffect)`）。
> 三份数据源：① 反编译 shader 产物 `D:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Hidden_UI_Default (UIEffect)/`
> （20 份 fragment `Sub0_Pass0_Fragment_b13..b32.hlsl` + 4 份 vertex `Sub0_Pass0_Vertex_b1..b4.hlsl`）；
> ② `Coffee.UIEffect.dll` 反编译；③ shader 清单 `Shader/Hidden_UI_Default (UIEffect).shader`（114 行）。
> ⚠️ **每份 hlsl 第 2 行 `// Keywords:`** 是变体归属的唯一判据（`b16` = `TRANSITION_SHINY`，我们那 5 颗实例走这一档）。

---

## 一、`uv1` 的生产者（`Coffee.UIEffect.dll`，本地能读）

```bash
D:/2/tools/ilspycmd/ilspycmd.exe D:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll > uieffect_all.cs   # 20,823 行
```

（实跑 rc=0；反编译产物 2026-11-02 落在 `D:/4/_tmp_view/uieffect/uieffect_all.cs`，**临时件，可重生成**）

| 位置（`uieffect_all.cs`） | 内容 | 结论 |
| --- | --- | --- |
| `:10243` | `public abstract class UIEffectBase : UIBehaviour, IMeshModifier, …` | 入口类 |
| `:10536` | `public void ModifyMesh(Mesh mesh) { }` | **空体**（走 `VertexHelper` 那条重载） |
| `:10539-10555` | `public virtual void ModifyMesh(VertexHelper vh)` → `context.ModifyMesh(graphic, transitionRoot, vh, flag)` | 转调 `UIEffectContext` |
| `:12878-13060` | `UIEffectContext.ModifyMesh(...)`（Cpp2IL 体噪声大，**关键闸可读**） | 见下 |
| `:12998-13030` | `if (onModifyVertex == null) {…} else { onModifyVertex(vt, uvMask) }` | **每颗顶点写一次 uv1** |
| `:16446-16460` | `GraphicProxy.OnPreModifyMesh`：`UIVertexUtil.onLerpVertex / ShadowUtil.onMarkAsShadow / UIEffectContext.onModifyVertex = …` | 三个钩子的注册点 |
| `:16240-16245` | `s_OnMarkAsShadow`：`float num = (float)vt.uv1 - s; vt.uv1 = (Vector4)num;` | **影子顶点：`uv1.x -= s`**（实参 `s = 2f`，见 `:16820` 那个 `float num8 = 2f;`） |
| `:16260-16268` | `s_OnModifyVertex`：`vt.uv1 = uv;`（`uv` 由 `Rect uvMask` 构造） | **每颗顶点先写「矩形」** |
| `:16705-16710`, `:16787-16910` | `ShadowUtil.DoShadow(...)` / `DoMirror(...)`：先复制一份顶点（`ApplyShadowZeroAlloc` / `ApplyMirror`），再对新顶点逐个调 `onMarkAsShadow` | 影子 = **同一网格里多出来的一圈顶点** |

**`uvMask` 从哪来**：`UIEffectContext.ModifyMesh` 里 `UIVertexUtil.GetBounds(s_WorkingVertices, first, count, out _, out reference)`
—— `reference` 是 `Rect`，即**这批顶点 uv 的包围矩形**。对 Graphic 是「整件一次」（`num2 = currentIndexCount`），
即 **= 该 sprite 的 uv 矩形**（九宫格也取全部 9 块顶点的并集 ⇒ 仍是整块 sprite 矩形）。

### 编解码（**唯一权威 = shader 算术，不是 C# 那行格式）**

反编译里 `vt.uv1 = (Vector4)num` 的 `Vector4` 从 `float` 转是**丢信息**的写法（`vt.uv1.x - s` 才是真身）。
判据改用 shader 侧算术，`b16.hlsl:122-128`：

```hlsl
bool  _238 = TEXCOORD_3.x < (-3.0f);                                  // 镜像标志
float _282 = TEXCOORD_3.x + 4.0f;
float _284 = _238 ? _282 : TEXCOORD_3.x;                              // 剥镜像
bool  _287 = _284 < 0.0f;                                             // 影子标志
float _290 = _284 + (_287 ? 2.0f : 0.0f);                             // 剥影子 ⇒ sprite 的 uv 左边界
```

⇒ **写入编码**：`uv1 = (xMin − 2·影 − 4·镜, yMin, xMax, yMax)`；**只有 `.x` 被偏移**（`.y/.z/.w` 在盒测里
直接用原值 `b16.hlsl:149-155`，若整体偏移则盒测必错 ⇒ 反证只有 x）。

**盒测**（`b16.hlsl:144-163`；`b13.hlsl:44-63` 同上形）：

```hlsl
_382 = uv.x >= _290;   _393 = uv1.z >= uv.x;   _402 = uv.y >= uv1.y;   _394 = uv1.w >= uv.y;
-- 四条边全部乘进「预乘色 + α」，位置 = 预乘之后、任何效果之前
```

📌 **它为什么存在**：`UIVertexUtil.Expand`（描边/阴影/边缘会**外扩网格**）之后 uv 被外推到矩形之外 ⇒
这四条边把外扩那圈裁回 sprite 矩形。**不外扩就恒 1**（纯 quad 的 uv 恰好等于矩形四角）。

**我们这条链的现状**：`ImageQuad.RebuildMesh`（`CardPresentation/Battle/ImageQuad.cs:497`）只建 `m.uv`（通道 0），
**没有 uv1** ⇒ 顶点属性读到 `(0,0,0,0)` ⇒ `uv1.z >= uv.x` 恒假 ⇒ 若直接上盒测会 **整块变黑**。
本波按「无数据兜底 = 单位矩形」处理（见报告 §四），行为与此前逐项相同。

**仍未解出的一支**：`_238`（镜像）在 `b16.hlsl:130` 用它在 `_MirrorRootViewMatrix` / `_RootViewMatrix`
之间二选一去采 `_TransitionTex`；矩阵生产者 `UIEffectContext.UpdateViewMatrix` 在 Cpp2IL 重建体里
**局部变量被打散**（只剩 `IL_xxxx` + `NoteDecompilerIssue`）⇒ **拿不到逐句算式**。原版 `m_Flip` 152/152 全 0
（`判据/J-12_Coffee前置判据_补充_第八十七会话.md:117`）⇒ 这一支永不出现。**未实现，如实记。**

---

## 二、Shadow 支 = 「与基础 `_ColorFilter` 互斥的另一条颜色滤波」

`b16.hlsl:320` `if (_287)` / `:521` `else`。**同族每一份效果变体恰有一处**：
`b14.hlsl:108`（变量名 `_71`）· `b15.hlsl:324` · `b17.hlsl:159`（变量名 `_285`）。

* **不是「画一层阴影」**：几何（多副本 + 迭代 + 衰减）在 **CPU 侧** `ShadowUtil.DoShadow(verts, vectors, distance, iteration, fade)`；
  `_ShadowIteration` / `_ShadowFade` **不是 uniform**。shader 只负责「影子顶点用哪套颜色滤波」。
* 旋钮 = `_ShadowColorFilter`（`b16.hlsl:41` c26.w）· `_ShadowColor`（`:42` c27）· `_ShadowColorGlow`（`:43` c28）。
* **9 档算式与基础 `_ColorFilter` 逐例相同**，只是颜色参数换成 `_ShadowColor`
  （实测 `b16.hlsl:327-513` 的第 1/2/4/5/6/8 例与 `b14.hlsl:316-481` 逐条同形）⇒ 复用同一段函数。
* 🔴 **两支的「后处理旋钮」不同一套**：
  * 基础支（`b16.hlsl:714-725`，`_666 = int(_ColorFilter) > 0`）：`rgb = lerp(p, pFilt, _ColorIntensity)`；
    `a = (1 − _ColorGlow·_ColorIntensity) · lerp(a, aFilt, _ColorIntensity)`。
  * Shadow 支（`b16.hlsl:514-516`）：**没有 lerp**（等价 `_ColorIntensity = 1`）：
    `a = (int(_ShadowColorFilter) > 0) ? (1 − _ShadowColorGlow)·aFilt : aFilt`，rgb 直接用滤波值。
* 📌 **原版生产路径永不进入**：全库 152 颗 `UIEffect` 的 `m_ShadowMode` **全 0（`ShadowMode.None`）**
  （`判据/J-12_Coffee前置判据_补充_第八十七会话.md:154`）⇒ 照 18·0「死支也照抄」实现，⛔ 别删。

---

## 三、`_TextureSampleAdd`

| 判据 | 内容 |
| --- | --- |
| 五份 fragment **一律 `:8`** | `float4 _TextureSampleAdd : packoffset(c3);`（`type_Globals`，紧跟 3 个 `_Globals_Unmapped`） |
| 用法 | `_61.._64 = tex.x/y/z/w + _TextureSampleAdd.x/y/z/w`（`b13.hlsl:34-37` · `b16.hlsl:134-137` · `b14:74-77` · `b15:134-137` · `b17:134-137` 处同形） |
| 声明位置 | **不在 `Properties` 里**：清单 `Hidden_UI_Default (UIEffect).shader:2-11` 只有 `_MainTex/_Color/_Stencil*/_ColorMask/_UseUIAlphaClip` |
| 值 | 由 uGUI（`Graphic`/`Image`）运行时写；普通 sprite（RGBA32）**恒 0**，Alpha8/ETC1 图集才非 0 |
| 我们 | `ImageQuad` + `MeshRenderer`，**没有那一跳** ⇒ 在 `Properties` 里声明出来把默认钉成 0（🔶，先例 `WFUIAdditive.shader:105`） |

⇒ `tx = tex2D(_MainTex, i.uv) + _TextureSampleAdd`；**观感零变化**。

**顺带订正的预乘口径**（同一次采样跳）：原版 `_375.._380`
= `COLOR.r` × `(tex+add).r` × `(tex+add).a` —— **预乘里不含 `COLOR.a`**；顶点 alpha 只从末跳
`_72 = round(COLOR.w·255)/255`（`b16.hlsl:103` 与 `:727`；`b13.hlsl:65-66`）进一次。
旧写法 `tex * i.color` 之后又 `rgb * c.a` ⇒ `COLOR.a` 被乘**两遍**（`_Color.a` / CanvasGroup 淡出时偏暗）。
现役 5 颗实例 `COLOR.a = 1` ⇒ 不改变现有画面。

---

## 四、`Gradation` / `Detail` / `Target` / `Sampling`（`B-313` ② 的结案）

**结论：原版这颗 shader 里根本没有它们 ⇒ 不实现（不是「还没做」）。** 三条独立证据：

1. 清单 `Hidden_UI_Default (UIEffect).shader:44-49` **只有 6 条** `#pragma multi_compile_local`
   （`UNITY_UI_ALPHACLIP / UNITY_UI_CLIP_RECT / TONE_GRAYSCALE / TRANSITION_DISSOLVE / TRANSITION_SHINY / EDGE_SHINY`）
   ⇒ 没有 GRADATION/DETAIL/TARGET/SAMPLING 关键字。
2. `grep -rn "Gradation\|_Detail\|_GradationTex\|DetailTex"` 扫 `Shader/` 下**全部 422 份产物 = 0 命中**。
3. `b16` 的 cbuffer 里 11 条 `_Globals_Unmapped_at_*`（c15.x/z/w · c17.z/w · c21.y/z/w · c26.x/y/z）
   **每一条都只出现 1 次**（只有声明、0 处使用）⇒ 没有「名字丢了但真在用」的隐藏参数。
4. 旁证：Coffee 的 C# **确实**会设这些名字（`s_GradationIntensity/…/s_GradationTex`、`s_DetailTex`，
   `uieffect_all.cs:10990-11040`）并持有一张 `s_GradationKeywords` 表 —— 但**本 build 的 shader 不认**
   ⇒ 在原版里它们是 **no-op**。且 `m_GradationMode` / `m_DetailFilter` **152/152 全 0**（同上 `J-12:154`）。

⇒ ⛔ 实现它们 = **造出原版没有的行为**（18·0）。`Sampling` 那一路本文件**已实现**
（`_SamplingScale` 进 EDGE 的 12 向采样，判据 `b17.hlsl:386-397`，`m_SamplingFilter` 152/152 全 0 亦然）。

---

## 五、复现命令

```bash
D=/d/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader
sed -n '1,3p'   "$D/Hidden_UI_Default (UIEffect)/Sub0_Pass0_Fragment_b16.hlsl"   # 变体归属
sed -n '122,128p' "$D/Hidden_UI_Default (UIEffect)/Sub0_Pass0_Fragment_b16.hlsl"  # uv1 解码
sed -n '144,163p' "$D/Hidden_UI_Default (UIEffect)/Sub0_Pass0_Fragment_b16.hlsl"  # 盒测
sed -n '316,521p' "$D/Hidden_UI_Default (UIEffect)/Sub0_Pass0_Fragment_b16.hlsl"  # Shadow/基础二选一
grep -n "multi_compile" "$D/Hidden_UI_Default (UIEffect).shader"                 # 只有 6 条
grep -rn "Gradation\|_Detail" "$D" | wc -l                                      # = 0
```
