# B116_W11 · `WildcardShine.shader` 剩余三块（`B-313`）

**结论**：三块全部落地（Shadow 用「结构性实现 + 死支照抄」的口径；`uv1` 的**数据通道**仍未接，本波给了方案与兜底）。
文件 **621 → 734 行**（+113，纯 LF / 裸 CR = 0，括号 28/28 平衡）。⛔ 没跑 Unity、没动 git/账本/活状态表。
判据全量 → `资料/完全复刻/判据/B116_W11_uv1与Shadow判据.md`。

---

## 一、`uv1`（**做了**，判据已解）

**生产者**（`D:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll`，20,823 行，本地可读）：

```bash
D:/2/tools/ilspycmd/ilspycmd.exe D:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll > uieffect_all.cs
```

| 类 / 方法（行） | 干什么 |
| --- | --- |
| `UIEffectBase.ModifyMesh(VertexHelper)` `:10539-10555` | 转调 `context.ModifyMesh(graphic, transitionRoot, vh, flag)` |
| `UIEffectContext.ModifyMesh` `:12878-13060` | 逐顶点调 `onModifyVertex(vt, uvMask)`（`:12998-13030`） |
| `GraphicProxy` 静态构造 `:16446-16460` | 三钩子注册点 |
| `s_OnModifyVertex` `:16260-16268` | `vt.uv1 = uv`（由 `Rect uvMask` 建） |
| `s_OnMarkAsShadow` `:16240-16245` | `uv1.x -= s`（实参 `s = 2f`，`:16820`） |

`uvMask` = `UIVertexUtil.GetBounds(...)` 的**这批顶点 uv 包围矩形** ⇒ 对 Graphic 就是 **sprite 的 uv 矩形**。

**编解码（唯一权威 = shader 算术，`b16.hlsl:122-128`）**：`uv1 = (xMin − 2·影 − 4·镜, yMin, xMax, yMax)`，
**只有 `.x` 被偏移**（`.y/.z/.w` 在盒测 `b16.hlsl:149-155` 里直接用原值 ⇒ 整体偏移必错，反证只有 x）：

```hlsl
_238 = uv1.x < -3          // 镜像；_284 = _238 ? uv1.x+4 : uv1.x
_287 = _284 < 0            // 影子（**同时是 Shadow 滤波支的总闸**）；_290 = _284 + (_287?2:0) = xMin
盒测（b16:144-163 / b13:44-63）：uv.x>=_290 · uv1.z>=uv.x · uv.y>=uv1.y · uv1.w>=uv.y  ← 四条全乘进预乘色+α
```

📌 **它为何存在**：`UIVertexUtil.Expand`（描边/阴影/边缘会**外扩网格**）之后 uv 被外推到矩形外，四条边把外扩那圈裁回 sprite 矩形。**不外扩就恒 1**。

**落点**：`appdata` 加 `float4 uv1 : TEXCOORD1`、`v2f` 加 `uv1 : TEXCOORD3`、`vert` 里原样透传（判据 `b1.hlsl:52,112`）、`frag` 开头按上式解码 + 盒测。
⛔ **镜像那一支未实现**（`_MirrorRootViewMatrix` 的生产者 `UpdateViewMatrix` 在 Cpp2IL 体里局部变量被打散，拿不到逐句算式；原版 `m_Flip` 152/152 全 0 ⇒ 永不出现）。**没拿屏幕坐标凑**，如实记在 shader 与判据里。

## 二、`_TextureSampleAdd`（**做了**）

五份 fragment **一律 `:8`** 的 `float4 _TextureSampleAdd : packoffset(c3)`；用法 `_61.._64 = tex + add`
（`b13:34-37` · `b16:134-137`）。原版**不在 `Properties` 里**（清单 `.shader:2-11`）——由 uGUI 运行时写。
我们这条链没有那一跳 ⇒ `[HideInInspector] _TextureSampleAdd ("TextureSampleAdd", Vector) = (0,0,0,0)`
（🔶 自己设计，先例 `WFUIAdditive.shader:105`）。对 Image 恒 0 ⇒ **观感零变化**。

## 三、Shadow 族（**结构做了；生产路径永不进入**）

- 形态 = **与基础 `_ColorFilter` 互斥的另一条颜色滤波支**，不是「画一层阴影」：
  `if (_287) {…Shadow…} else {…_ColorFilter…}`（`b16.hlsl:320/521`；同族 `b14:108` · `b15:324` · `b17:159`）。
  几何（多副本 + 迭代 + 衰减）在 **CPU 侧** `ShadowUtil.DoShadow(verts, vectors, distance, iteration, fade)`
  ⇒ `_ShadowIteration` / `_ShadowFade` **不是 uniform**，shader 里没有它们。
- 9 档算式与基础支**逐例相同**（只是颜色换 `_ShadowColor`，实测 `b16:327-513` 的第 1/2/4/5/6/8 例与 `b14:316-481` 同形）
  ⇒ 新增 `wfBaseOrShadowFilter(p, a, shadow)` 复用 `wfColorFilterBase`，三处调用点（TONE / 过渡 / EDGE）全部改走它。
- 🔴 **两支的旋钮不同一套**：Shadow 支**没有** `_ColorIntensity` / `_ColorGlow` 的 lerp；
  alpha 只在 `filter > 0` 时乘 `(1 − _ShadowColorGlow)`（判据 `b16.hlsl:514-516`）。
- 📌 **原版生产路径一次都不走**：全库 152 颗 `UIEffect` 的 `m_ShadowMode` **全 0**（`判据/J-12…:154`）
  ⇒ 按 18·0「死支也照抄」实现，⛔ 别删（我没删也没简化）。

## 四、`B-313` ② `Gradation` / `_Detail` / `Sampling`（**查清了：按「不实现」结案，不是「还没做」**）

1. 清单 `.shader:44-49` **只有 6 条** `multi_compile_local`，无 GRADATION/DETAIL/TARGET/SAMPLING 关键字。
2. `grep -rn "Gradation\|_Detail\|GradationTex\|DetailTex"` 扫 `Shader/` 下**全部 422 份产物 = 0 命中**。
3. `b16` 的 11 条 `_Globals_Unmapped_at_*` **每条只出现 1 次**（只有声明、0 处使用）⇒ 没有「名字丢了但真在用」的隐藏参数。
4. 旁证：Coffee 的 C# **确实**会设这些名字（`s_Gradation*` / `s_DetailTex`，`uieffect_all.cs:10990-11040`）并持有
   `s_GradationKeywords` 表 —— **但本 build 的 shader 不认** ⇒ 原版里它们是 **no-op**；且 `m_GradationMode` / `m_DetailFilter` **152/152 全 0**。
5. `Sampling` 已实现（`_SamplingScale` 进 EDGE 的 12 向采样，`b17.hlsl:386-397`）。

⇒ 实现 Gradation/Detail = **造出原版没有的行为**（18·0）⇒ 结案为「不实现」。

---

## 五、顺手发现 / 需要调度台分流的

1. 🔴 **已就地订正（在我这个文件里）**：旧预乘把 `COLOR.a` 乘了**两遍**（`tex*i.color` 后再 `rgb*c.a`）。原版 `_375.._380` = `COLOR.rgb · (tex+add).rgb · (tex+add).a`，`COLOR.a` 只从末跳 `_72 = round(COLOR.w·255)/255` 进**一次**（`b16:103` / `b16:727` · `b13:65-66`）。现役 5 颗实例 `COLOR.a = 1` ⇒ **现有画面不变**；`_Color.a` / CanvasGroup 淡出时才会显形。
2. 🔴 **`ImageQuad` 没有 uv1 通道**（`Battle/ImageQuad.cs:497` 只 `m.uv = …`）⇒ 本波在 frag 里加了 **「无数据兜底 = 单位矩形」**（🔶 判据：合法矩形必有 `z>x && w>y`；不满足就按 (0,0,1,1)）。兜底后行为与本波之前**逐项相同**（掩码恒 1）。**接法（需另一个写手，`ImageQuad.cs` 不在我白名单）**：`RebuildMesh` 里 `m.SetUVs(1, new[]{ (uv.xMin,uv.yMin,uv.xMax,uv.yMax) × 4 })`；`ImageQuadNine`（`:969`）同理按整块 sprite 矩形写同一组值（九宫格要**整块矩形**，不是每段自己的）。接上后这段代码自动生效，**且届时影子支仍然不会出现**（原版 `ShadowMode` 全 0）。

---

## 六、下次 Unity 窗口验证什么

1. **Console 里搜 `WildcardShine`**（`Shader error in 'CardPresentation/WildcardShine'`）。
   ⚠️ **别只看断言**：`Shader.Find(...)` 对编译失败的 shader **仍返回非 null**、`.name` 也不变
   ⇒ `CollectionScene` / `DeckScene` 那两条「shader 名对不对」的断言**抓不到编译失败**。要配一条
   `Mat.HasProperty("_TextureSampleAdd") && Mat.HasProperty("_ShadowColorFilter")`（编译失败 ⇒ HasProperty 恒 false）。
2. **指令数 / 临时寄存器**（本波最大风险：`#pragma target 3.0` = SM3，**512 指令 / 32 temp**，而这波又内联了一整套 9 档滤波）：
   Console 搜 `too many instructions` · `temporary registers exceeded` · `X4502`。若触顶，两级阶梯：
   ① `#pragma target 4.0`（SM4 无 512 上限）；② 更忠实 = 原版本来就是 `:35-36` 的 `#pragma target 5.0` + `use_dxc`。
   ⛔ **不许退回 2.5**（那条 `🔶 自己设计` 的说明仍在 `:210-241`）。
3. **变体数**：6 条 `multi_compile_local` 名义 64 组合，但 `#if` 链是**精确匹配** ⇒ 预期 fragment 20 档 + vertex 4 档
   （与原版一致；实际编出来的是当前 target 下的子集）。看点 = 有没有「某档没编出来」（Console 报 `Shader ... not found`）。
4. **画面两态**：`CollectionScene.Run` 的 `WildcardShineRate` / `StepWildcardShineForTest(dt)`；`DeckScene.Run` 的
   `WcShineRate`（`-1` = 层没挂）。**期望：与 B115_W22 那一版逐像素相同**（三块都是结构性补齐，现役参数下全恒等）。
5. **顺手核一条**：手动 `SetFloat("_ShadowColorFilter", 4)` 也不该变画面 —— `_287`（影子闸）来自 uv1、uv1 恒为
   「无数据」⇒ 影子支走不到。**这条能证伪「错把影子支当默认」**。

---

## 七、改动清单（单文件，`CardPresentation/Resources/Shaders/WildcardShine.shader`）

| 位置 | 改动 |
| --- | --- |
| 文件头 `:97-126` | 新增 `B116_W11` 那一节（三块做了什么 / Gradation 结案 / 镜像与影子几何的缺口） |
| `Properties` `:146-154` | + `[HideInInspector] _TextureSampleAdd` · + `_ShadowColorFilter/_ShadowColor/_ShadowColorGlow`（`:195-197`） |
| CG uniforms `:283-310` | + `float4 _TextureSampleAdd` · + Shadow 三个 |
| `appdata` / `v2f` / `vert` | + `uv1 : TEXCOORD1` / `uv1 : TEXCOORD3` / 透传（`:337`） |
| `frag` 开头 `:516-558` | 采样改为 `tx = tex2D + _TextureSampleAdd`；预乘改为原版口径（不含 `COLOR.a`）；新增 uv1 解码 + 盒测 |
| 新增函数 `:496-514` | `wfBaseOrShadowFilter(p, a, shadow)`（Shadow 支 / 基础支二选一） |
| 三处调用点 | `wfBaseColorFilter` → `wfBaseOrShadowFilter`（TONE `:570` · 过渡 `:629` · EDGE `:669`） |
| `:710`（原 `:597`） | 文档瑕疵就地订正：「默认 2.5 档」→「**3.0** 档（见 `:242` 的 `#pragma target 3.0`）」，**零行数变化** |

🔶 标记 2 处：`_TextureSampleAdd` 的 Properties 声明 · uv1「无数据兜底」（都写全了「是什么/为什么必须自定/谁定的+日期」）。
