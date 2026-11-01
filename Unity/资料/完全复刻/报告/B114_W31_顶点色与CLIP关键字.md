# `B-261`（顶点色）+ `B-262`（`UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`）—— 写手 `W-31`

> 写手 `W-31` · 2026-11-01。**没跑 Unity**（`-executeMethod` 全禁）· **没动 git** · 没碰
> `Battle/ImageQuad.cs`（共用件，本波不给）/ `Shell/ItemDrawer*` / `Shell/ItemDrawerLayout.cs` / `RuleEngine/**` /
> `工具/ruleprobe/**` / `Editor/BattleScene.cs` 与 `Editor/ShellScene.cs` 的聚合行 / 两份正本 / `账/*.tsv` / `Scenes/*.unity`。
> **备份（改前原样）**：`d:/4/_tmp_view/b114_w31/WFUIBorderHighlight.shader.bak`（**228 行** / 15,772 B / LF）·
> `d:/4/_tmp_view/b114_w31/UIBorderGlow.cs.bak`（**482 行** / 29,530 B / LF）。
> （行数一律用 `wc -l` 口径 = 换行符个数，与 `报告/B114_W13_边框shader其余5处.md` 一致。）

---

## 〇 一句话

**两件都落地了。** `B-261`：`UIBorderGlow.ChangeGraphicAlpha` 从「写 `_Color`」改走**顶点色通道**
（并在「材质有 `_Color`」时**不换路**，避免双乘；写不进网格时**出声**）。
`B-262`：按 uGUI `UI/Default` 的约定补齐两条 `multi_compile_local` + `UnityGet2DClipping` + `clip(a-0.001)`；
**本工程确实没有 `_ClipRect` 的数据源**（结构性，不是没接线）⇒ **补了今天恒不生效**，已写进 shader 文件头与断言标签。

**顺带修掉同一族的第二处静默**：`CurrentColor()` 在「材质没有 `_Color`、通道也没开」时读 `_quad.Tint`
**不可信** ⇒ `graphicOriginalColor.a` 可能是 0 ⇒ `Anim()` 末句会把 α「还原」成 0（**画面永不可见**）。

`typecheck` = **0 / 0**（⚠️ **不覆盖 shader**）。账闸 `check_ledger_points.py` = **漂移 0 / 失败 0**。

---

## 一 活 1 · `B-261` 逐条

### 1·1 判据（原版）

| 项 | 内容 |
|---|---|
| 原版方法 | `ChangeGraphicAlpha(float target)` —— 原版 `UIBorderGlow.cs:262`（账 `特效动画:426` 已登记，判 A） |
| 它改的是什么 | `MyGraphic.color`。uGUI 里 `Graphic.color` 是**顶点色**（`CanvasRenderer.SetColor` → 每顶点 `COLOR`） |
| 本族 shader 的消费口 | 顶点色：判据 → `判据/B114_R5_Reveal_DXBC.md` §2·1「VS `o2 = v4`」、§2·2 ⑧ 末句「`mul o0.xyzw, r0.xyzw, v2.xyzw`」<br>原版 `m_PropInfo` 19 条里**没有 `_Color`** ⇒ 写 `_Color` 是**结构性落空**，⛔ 不是「少配一个属性」 |

### 1·2 现状（改前）与**真实的受害面**

改前 `Shell/UIBorderGlow.cs:141-143`：

```
var c = _quad.Tint;      // = _mr.sharedMaterial.color（= `_Color`）
c.a = target;
_quad.SetTint(c);        // 通道没开 ⇒ 写 `_Color`
```

`ImageQuad.SetTint` → `ApplyTintColor`，`_vertexTint == false` 时写 `sharedMaterial.color`。现读全工程
`AddComponent<UIBorderGlow>` 只有**两处**（`grep` 全仓）：

| 站点 | 用的材质 | 改前是不是静默落空 |
|---|---|---|
| `Shell/ForgeTab.cs:1063`（`Collect Highlight Effect`） | `ForgeGlowMaterial()` = **原版 bundle shader** `Everguild/UI/UI Border Highlight Appear`（`WarpforgeShaderMap.TryResolve`） | ✅ **是**（那颗属性表里没有 `_Color`）⇒ **B-261 的真实受害面就在这一条** |
| `Shell/CardDisplayEffects.cs:366`（`Level Up Effect` / `… Down`） | 本工程 `WarpforgeVFX/UI/UI Border Highlight` | ❌ 不是 —— 同一件在 `:327` 已因 `im.hasAmp` 调过 `quad.SetVertexTint(tint)`，**通道本来就开着**，`SetTint` 早就在走 `Mesh.colors` |

> ⇒ 结论不变（要改），但**「谁能看见」要说准**：改前真正受影响的是 Forge 那颗紫边；
> `CardDisplayEffects` 那两件在 `W-8` 改完 `hasAmp` 之后其实已经正确。

### 1·3 顺手挖出的**同一个病的第二处**（也修了，同属 `B-261`）

`CurrentColor()`（`UIBorderGlow.cs:348`，`Awake` 的 `Bookkeep()` 用它记 `graphicOriginalColor`）
读的也是 `_quad.Tint`。而 **`Material.color` 在 `_Color` 不存在时是「会打一条
`Material doesn't have a color property '_Color'` 之后给一个默认值」** —— 那个默认值**不是我们能依赖的量**。
一旦它给的是全零，`_originalColor.a == 0` ⇒ `Anim()` 末句 `ChangeGraphicAlpha(_originalColor.a)`
就把 α「还原」成 **0** ⇒ **Forge 那颗紫边即使 `Play()` 了也永远不可见**（而原版是还原成 `Awake` 时记下的原值）。

> 这条与本波主任务同源：**`_Color` 不是唯一通道**，而 `Tint` 这个读口在两种材质族上语义不同（铁律 5·c）。
> 改法：`CurrentColor()` 在「材质没有 `_Color`、顶点色通道也没开」时**兜白**（uGUI「未改过色」的中性值）。

### 1·4 结论（改了什么）

1. `ChangeGraphicAlpha`（**同一物理行**，行数零变化）：
   ```csharp
   if (!_quad.UsesVertexTint && !QuadMaterialHasColor()) _quad.SetVertexTintChannel(true); _quad.SetTint(c); WarnIfVertexTintUnreachable();
   ```
   * **先开通道**：`SetVertexTintChannel(true)` ⇒ 之后 `SetTint` 走 `Mesh.colors`。
   * **为什么还用 `SetTint` 而不是 `SetVertexTint(c)`**：`SetVertexTint(c)` 只「开通道 + 推自己的色」，
     **不把色转给软边子块**；通道开着时 `SetTint` 会（`ApplyTintColor` 的顶点分支 + `_softKids` 循环）。
     本组件将来若挂到有软边的 quad 上，走 `SetTint` 才不漏。
   * **材质有 `_Color` 时 `SetVertexTintChannel` 不打开** ⇒ 旧路（写 `_Color`）照旧。
     理由：那种材质上旧路**本来就生效**，换路之后顶点色会与已有 `_Color` **双乘**（rgb 变暗）。
     今天两条生产链都不落这一档，留着它是给「将来挂到 `Sprites/Default` 上」兜底。
2. `CurrentColor()`（**同一物理行**）：`(_quad.UsesVertexTint || QuadMaterialHasColor()) ? _quad.Tint : Color.white`。
3. **出声**（类尾新增）：`WarnIfVertexTintUnreachable()` —— 顶点色这条路要 `MeshFilter` + 网格才落得下去
   （`ImageQuad.UploadVertexTint` 在 `_mf == null` 时**直接 return**）；没有就喊一次，⛔ 不让「改了 α、画面一动不动」变成静默。
4. 为什么**没有**改用 `ImageQuad.SetVertexTint`：见上面第 1 条（软边子块那半句）。
   ⚠️ 与本任务单里「`ImageQuad.SetVertexTint` 那套」并不矛盾 —— 我用的是**同一套通道**的两个口。

---

## 二 活 2 · `B-262` 逐条

### 2·1 判据（原版）

* `m_ParsedForm.m_KeywordNames[18] = UNITY_UI_CLIP_RECT` · `[19] = UNITY_UI_ALPHACLIP`，
  `m_KeywordFlags` 与 `_DEBUG`/`_AUTOROTATION`/`_FLIPDIRECTION` **同为 1** ⇒ 同一档（local）。
  出处 → `报告/B114_W13_边框shader其余5处.md` §一·②（W-13 现读原版 JSON）。
* 原版编译产物里确有 clip / alphaclip 组合（R5 §2·1 括注「同样三个关键字状态下 clip/alphaclip 组合的 VS **完全相同**」）。
* 原版的 `_ClipRect` 落在 `$Globals`（`cb0[131]`），**不是** `UnityPerMaterial`（R5 §三·❓6）⇒ 它是 uGUI 的
  **per-renderer / global** 量，由 `CanvasRenderer` / `RectMask2D` 逐件喂。

### 2·2 现状（改前）

`shader:122-124` 只有三条 `#pragma shader_feature_local`（`_DEBUG` / `_AUTOROTATION` / `_FLIPDIRECTION`）；
**没有** CLIP_RECT / ALPHACLIP 两路。

### 2·3 有没有 mask 数据源 —— **结构性没有**（这是本条的结论重点）

| 检查 | 现读结果 |
|---|---|
| 全仓 `_ClipRect` / `UnityGet2DClipping` / `UNITY_UI_CLIP_RECT` 的**生产写入点** | **0**。命中只有：TMP 自己的 shader 与 cginc；本项目 shader 里**只有注释**（`WildcardShine.shader:51-52`、`WFUIAdditive.shader:11-12/34-39`、`MenuDraw.cs:397/609/637`）；**没有任何 C# 调 `SetVector("_ClipRect")` / `EnableKeyword("UNITY_UI_CLIP_RECT")`** |
| 载体能不能被 uGUI 接管 | **不能**。`ImageQuad.Create`（`Battle/ImageQuad.cs:133-137`）建的是 `MeshFilter` + `MeshRenderer`，**没有 `CanvasRenderer` / `Graphic`** ⇒ `RectMask2D` 那套（`MaskableGraphic` → `canvasRenderer.SetClipRect`）**根本找不到它** |
| 那本工程的裁切是怎么做的 | **几何等效**：`Shell/MenuDraw.cs` 的 `ApplySoftEdges`（按渐隐带内沿把 quad 切开 + 逐顶点 alpha 斜坡，`SoftAlpha` 的判据就是 uGUI `UI/Default` 那句 `saturate((_ClipRect.zw-_ClipRect.xy-abs(...))*mask.zw)` 的化简）+ `Shell/ViewportClip`（`ClipPx`）。二者都**不经过 shader** |

⇒ **补了也不会生效，而且这是结构性的**（不是「还没接线」）：`multi_compile_local` 的两个变体默认都不开，
`_ClipRect` 也没人设。要让这两路真生效，得有人：
`material.SetVector("_ClipRect", 世界坐标(minX,minY,maxX,maxY))` + `material.EnableKeyword("UNITY_UI_CLIP_RECT")`，
并且**得有一条世界空间的裁剪框数据源**（今天不存在）。

### 2·4 结论（改了什么 + 为什么不算「半截」）

按 uGUI `UI/Default` 逐项补全（**不是只加 pragma**）：

1. `Properties` 加 `[HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)`
   * 原版在 `$Globals`，我们只能当材质属性喂（`MeshRenderer` 没有 `CanvasRenderer`）⇒ 这是**我们这一侧的必要差异**，已在注释里写死。
   * 默认值是 uGUI 的**「不裁」档** ⇒ 即使有人误开关键字，也**不会整块消失**（⛔ 别改成 0）。
2. 两条 `#pragma multi_compile_local _ UNITY_UI_CLIP_RECT` / `_ UNITY_UI_ALPHACLIP`（uGUI 约定，不是 `shader_feature`）。
   ⚠️ 三条**原版 feature 关键字保持 `shader_feature_local`**（原版出包会裁变体，W-13 有硬证），断言 B7 就是挡这个的。
3. `v2f` 加 `float2 worldXY : TEXCOORD3;`，`vert` 里 `mul(unity_ObjectToWorld, v.vertex).xy`（与 `_ClipRect` 同空间）。
4. `#include "UnityUI.cginc"` + `frag` 末尾（**顺序照 `UI/Default`：先乘裁切、再 alpha clip**）：
   ```hlsl
   #ifdef UNITY_UI_CLIP_RECT
       o.a *= UnityGet2DClipping(i.worldXY, _ClipRect);
   #endif
   #ifdef UNITY_UI_ALPHACLIP
       clip (o.a - 0.001);
   #endif
   ```
   * `UnityGet2DClipping` **用了本机那一份**（`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/CGIncludes/UnityUI.cginc:4-8`
     现读 = `step(clipRect.xy, p) * step(p, clipRect.zw)` 的**硬边**，**没有 softness 项**）⇒ 我们与它逐字一致，⛔ 没自己加软边。

> **「别做半截」怎么处理**：pragma + 属性声明 + 算式 + 顺序 + 默认值 全都补齐了，**唯一的缺口是数据源**，
> 而那是**工程架构层面今天不存在的东西**（`ImageQuad` 不是 uGUI），已在 shader 文件头 ⑧、pragma 上方注释、
> 断言标签三处写死「恒不生效」。⛔ 没有把它写成「已复刻」。

---

## 三 改动清单 + 净增净减

| 文件 | 净变化 | 说明 |
|---|---|---|
| `Assets/CardPresentation/Shell/UIBorderGlow.cs` | **482 → 533 行**（**+51 净**；`+54 −3`） | 3 处**原位替换**（行数零变化：`:141` / `:143` / `:351`）+ **51 行全部追加在类尾** |
| `Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` | **228 → 270 行**（**+42 净**；`+44 −2`） | 属性 1 条 + 2 条 pragma + 1 个 include + v2f/vert/frag 各 1 处 + 文件头 ⑧ 与 ⑥ 的订正 |
| `Assets/CardPresentation/Editor/B114_W31_Assert.cs` | **新建**（259 行） | 断言；宿主未接（片段见 §五） |
| `Assets/CardPresentation/Editor/B114_W31_Assert.cs.meta` | 新建 | `guid: 351f9220fc882b20b1ca7f0268846485` |

**账不漂**（现读验过，不是推断）：

* `UIBorderGlow.cs` 在账里的 **13 条落点** —— `:97 / :103 / :120 / :129 / :136 / :167 / :190 / :205 / :361 / :378 / :396 / :407 / :415`
  （`grep "Shell/UIBorderGlow.cs:" 账/` 现数 13 条，全在 `账/特效动画.tsv:423..438`）—— **逐条打印现值，一行都没动**。
* `WFUIBorderHighlight.shader` **不在任何账落点里**（`grep -rn "WFUIBorderHighlight" 账/` = 0 命中）。
* `python -I -X utf8 Unity/工具/check_ledger_points.py` ⇒ **A 5841 条 · 通过 5841 · 漂移 0 · 失败 0**。

行尾（现读，二进制数出来的）：三个文件 **CRLF=0 / 裸 CR=0 / 无 BOM**，仍全是 **LF**。

---

## 四 哪些要等 Unity 验（我跑不了）

1. 🔴 **shader 真的编得过** —— `typecheck` **不覆盖 shader**。本波新增了：`#include "UnityUI.cginc"`、
   一个 `float2 worldXY : TEXCOORD3` 插值器、两条 `multi_compile_local`（变体数 ×4）。
   预期风险点：插值器包装（`COLOR`+TEXCOORD0..3 = 5 个寄存器，ps_3_0 够）；`UnityUI.cginc` 与 `UnityCG.cginc` 无符号冲突（现读该文件只定义 3 个函数）。
2. `B114_W31_Assert.Run()` 需要宿主接线（片段见 §五），**接线后跑一次**才能把 A/C 两段的绿/红变实。
3. `Shader.Find("WarpforgeVFX/UI/UI Border Highlight")` 只在**编辑器**里可靠：这颗 shader 不在 `Resources/`、
   也不在 AlwaysIncluded（`B110_W5_ShaderSupport_Assert.cs:132-138` 的 Entry 写的是「只靠 `.mat` 的 guid 引用进包」）
   ⇒ ⛔ **别把断言里 `Shader.Find` 那一步搬进 player**。
4. **观感**：Forge 那颗 `Collect Highlight Effect`（紫边呼吸）与升级边框两件的**α / 染色**是否还在
   —— 改前 Forge 那条是「α 写进空气」，改后第一次真正生效，属于**行为变化**（预期 = 变对）。
5. `.mat` 重新导入的 diff（新增 `_ClipRect` 属性）—— 要 Unity 才看得到。

---

## 五 断言（`B114_W31_Assert.cs`）与接线片段

入口 `B114_W31_Assert.Run()`；`Pass` / `Fail` 读口已备。

**可粘贴片段**（⚠️ 只由调度台改宿主；插在 `Editor/BattleScene.cs:46896` 那条聚合行里、
`B114_W18_Assert.Fail;` 之后、`Debug.Log(P + $"=== 合计…` 之前；**同一物理行、第一个 `//` 之前**）：

```csharp
 B114_W31_Assert.Run(); pass += B114_W31_Assert.Pass; fail += B114_W31_Assert.Fail;
```

覆盖（共 **30** 条 `Check`，前提不成立时后面的会跳过 —— 前提本身**各自也是一条断言**，⛔ 不是 `if (前提) Check(结论)`）：

| 段 | 断什么 |
|---|---|
| **A**（活 1 运行时） | `A0` shader 取得到 · `A1` 载体建得出 · **`A2` 前提：这颗 shader 没有 `_Color`** · **`A2b` `_ClipRect` 真在属性表里**（这条顺带冒烟「pragma 写了但属性没声明」）· `A5` 通道被打开 · **`A6` 四角顶点色 α = 0.25** · `A6b` `Tint.a` 读回一致 |
| **A7/A8（灭自证）** | 关掉通道 ⇒ 顶点色**回到白**；再打开 ⇒ 回到 0.25 ⇒ 证明 A6 量的**确实是这条通道**，不是别处恰好给的 0.25 |
| **A9–A11** | 材质**有** `_Color` 时 `UsesVertexTint` 保持 false、`_Color.a` 被写成 0.25（⛔ 换路会双乘） |
| **B**（活 2 源码级） | `B1/B2` 两条 pragma 各 1 · **`B3` 裁切真的乘进 alpha（不是只有 pragma）** · `B4` alpha clip 真的写了 · **`B5` 顺序 = 先裁切再用 alpha clip** · `B6` `_ClipRect` 默认值是「不裁」档（⛔ 0 = 全裁）· **`B7/B7b` 灭自证：三条原版 feature 关键字没被改成 `multi_compile_local`** |
| **C**（活 1 源码级） | `C1` 开通道那句在 · `C2`「材质有 `_Color` 不换路」那道闸在 · `C3` 推色仍走 `SetTint`（会转给软边子块）· `C4` 出声口在位（1 定义 + 1 调用）· `C5` `CurrentColor()` 的白兜底在 |

> ⚠️ B 段全部是**文本级**断言（`typecheck` 不覆盖 shader、本波不许跑 Unity）。
> 唯一能证明「编得过」的是 Unity 窗口；`A2b` 是**次强**的那一条（属性表真的被解析出来了）。

---

## 六 没查清的部分（如实写，⛔ 没猜测）

1. 🔴 **`Material.color` 的 getter 在「材质没有 `_Color`」时到底返回什么**（白 / 黑 / 全零）—— 本机没跑 Unity，
   只能从「它会打一条 `Material doesn't have a color property '_Color'`」推出**不可依赖**。
   我们的兜底（白）在两种返回值下都安全；但**「改前 Forge 那颗紫边到底可不可见」我只能说「取决于那个返回值」**，
   没验。⇒ 这条也是 §四·4 要看的。
2. 原版 `Collect Highlight Effect` 那颗 Image 的 `m_Color` **具体值没读**（只按 uGUI 默认白兜底）。
   影响面很小：`_originalColor` 的 **RGB 从不被 `ChangeGraphicAlpha` 使用**，只有 `.a` 决定「动画末句还原成多少」，
   白 ⇒ 还原成 1.0。
3. **原版那 5 份材质到底有没有开过 `UNITY_UI_CLIP_RECT`** —— R5 §三·15 的表里 5 份 `m_ValidKeywords` 只有
   `[]` / `_AUTOROTATION,_FLIPDIRECTION` / `_AUTOROTATION,_DEBUG` / `_DEBUG`，**没有一份带 CLIP_RECT**
   ⇒ 原版这两路很可能也是**备而不用**。没进一步查（不影响本波结论：我们同样没有数据源）。
4. 加了 `_ClipRect` 之后 `.mat` 的重新导入 diff 没看（要 Unity）。

---

## 七 顺手发现（⛔ 一条都没动手改）

1. 🔴 **`Shell/MenuFxMaterials.cs:586-590` 的注释已经过期**：那里写着
   「`UIBorderGlow.ChangeGraphicAlpha` 走的是 `ImageQuad.SetTint` → `sharedMaterial.color`，那会**每次播都打一条**
   `Material doesn't have a color property '_Color'`」—— **本波之后不成立了**（没有 `_Color` 的材质会先开顶点色通道）。
   该文件不在我的白名单 ⇒ 请调度台订正（它同时也是 `RevealSweep` 「为什么不用 `UIBorderGlow`」那段裁决书的论据之一，
   订正时**别把结论一起推翻**：那条理由里「原版是一颗 Image、我们是九宫多块」仍然成立）。
2. **`CardDisplayEffects` 与 `UIBorderGlow` 是同一个病的两条独立路**：那边是「**建件时**一次性推色」（`im.hasAmp` → `SetVertexTint`），
   这边是「**播放时**改 α」（本波）。今天两边都对，但**没有任何一条断言把它们绑在一起** ——
   谁哪天动了 `ImageQuad.ApplyTintColor` 的「通道没开」分支，两边会一起静默退回去。
   建议另立一条通用断言：「这颗 quad 用的材质**没有 `_Color`** ⇒ `UsesVertexTint` 必须为真」（可挂在 `CardDisplayEffects` 那两个节点上）。
3. **`WildcardShine.shader:51-52` / `WFUIAdditive.shader:34-39` 明确写着「我们没有 `_ClipRect` 通道 ⇒ 少这两路」**，
   与本波给 `WFUIBorderHighlight` 补上这两路的**口径不一致**。我**没动那两个**（不在白名单，且它们的结论各有独立判据：
   材质 keywords 为空 / 走的是几何裁切）。若调度台要统一口径，请另立条目。
4. `Assets/CardPresentation/Scenes/CollectionCheck.unity` 里有 **2 个序列化的 `UIBorderGlow`**
   （`:69117` / `:234706`）—— 那是自检产物场景（`.gitignore` 里 `CollectionCheck.unity` 已取消跟踪的那份是 68 MB 版；
   这一份仍在 `Scenes/`）。与本波无关，但**别把它当「生产用法」的参照**。
