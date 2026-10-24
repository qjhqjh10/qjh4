# W21 · `_Clamp_Range` 语义订正 —— 执行报告

> 执行代理 · 2026-10-24 · 白名单内只改 `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs`
> ⛔ 未跑 Unity / 未动 git / 未改任何正本 / 未碰其它 `.cs`
> 判据：`资料/普查产出_1024d/X1_三处判据补查.md` §一（本轮只读该节，未重查）

---

## 一、结论（一句话）

`CardView.cs` 里所有把 `_Clamp_Range` 写成「乘法 / 矩形外 α=0 / 真裁切 / 原版等价物」的注释与 doc **全部就地订正**为反汇编口径
**`alpha = saturate(alpha + inside01)`（矩形内把 α 顶到 1、矩形外原样保留 ⇒ 原版从不把 α 置 0）**，
并带上 2026-10-24 的更正痕与判据出处。**改动全部是注释 / XML doc，零代码、零行为变化。**

---

## 二、改动清单（行号为改后现状，文件均为 `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs`）

| # | 位置 | 改了什么 |
| --- | --- | --- |
| 1 | `:883-906`（`ClampRangeSmall` 上面那段总说明） | 标题「硬矩形」→「`_Clamp_Range` 矩形」；算式 `α *= (…) ? 1 : 0` → **`alpha = saturate(alpha + ((…)? 1 : 0))`**，补指令流 `ge → and → movc → mad_sat`、四个 PS 变体逐字节相同；「矩形外整块 α=0」→ **矩形外原样保留（用贴图 alpha）**；补 `$Globals` **byte 112 = cb0[7]**、`m_Attributes=[]`（无 Toggle、无门控）、`m_DefValue`；补更正痕（「⚠️ 2026-10-24 更正：原来写『矩形外整块 α=0』，实际原版从不把 α 置 0」）与命名约定（下文「硬矩形 / clampKind」= 这块 UV 矩形，**不裁画面**） |
| 2 | `:920-923`（UV 段） | 「只等于图宽两端各**切** 1.2% / 2.6%」→「只**落在**两端各 1.2% / 2.6% **之外**」，并补「这个区间是『矩形内 α 顶到 1』的作用范围，⛔ 不是切掉」 |
| 3 | `:936-961`（「我们怎么落」） | 翻面理由从「被原版实况证伪」改成**「由反汇编直接否掉」**（四条截图/NCC/离线证据降级为旁证）；`_artFront` 明确写成**「我们自己的近似、判据不足」**；删掉「这正是原版对 `CardImage` 做的事」；把「未闭合：受不受同一硬矩形」换成 `X1` §一·⑤ 的**已知可见偏差**（原版矩形外用贴图 alpha ⇒ 单位卡那圈原版比我们更透明） |
| 4 | `:1123-1128`（2DCard 立绘说明） | 「立绘可见范围 = 卡框贴图 alpha **+ 它自己的硬矩形**」→ 卡框 alpha 决定可见范围，`_Clamp_Range` **不裁画面**；带更正痕 |
| 5 | `:1192-1195`（`ArtLayerMesh` 自检口的 doc） | 「判不了原版硬矩形有没有真的夹住」→「判不了**我们那层网格**有没有被 `clampKind` 的矩形缩进去」+ 更正痕 |
| 6 | `:3932-3946`（`Build` 里 `_art` 档位那段） | 同上：翻面理由换成反汇编；四条实况证据标为旁证；保留「⛔ 不要再按卡型/状态传 clampKind」 |
| 7 | `:3949-3953`（`Build` 里 `_artFront` 那段） | 删掉「= 原版 `CardImage` 在那条链上的等价物」，改为「我们自己的近似」（附判据指针） |
| 8 | `:9688-9691`（`ArtMeshInFrame` 条带早退路的 `F4` 注释） | 补一句：原版这个矩形**只把矩形内 α 顶到 1、不裁画面**，这里「取交集」是我们的近似（原注释的 C# 判据仍然成立） |
| 9 | `:9715-9719`（`ArtMeshInFrame` 取交集处） | 删掉「这正是原版 shader『矩形外 α=0』的等价物」，改为「裁画面是我们自己的近似、判据不足」 |
| 10 | `:9970-9971`（`ArtVisibleThroughFrame` 第 ⑤ 条） | 「UV 不在矩形内 ⇒ α=0」→ **`alpha = saturate(alpha + inside01)`，矩形外原样保留**；带更正痕 |
| 11 | `:10663-10677`（`ArtClampKindFor` 的 doc 开头两条 `<para>`） | 加「同日再订正：**翻面的理由换了**」——决定性判据是指令流（它根本不裁 ⇒ 给 `_art` 套硬矩形在原版没有对应物）；四条实况证据明确降级为旁证 |
| 12 | `:10693-10698`（同一 doc 的末段） | 删掉「原版那一层确实是 `_Clamp_Range` 的等价物」，改为「`_artFront` 的硬裁是**我们自己的近似、判据不足**；只在 opt-in（`UseFrontLayer` 出厂 false）时可见 ⇒ 生产画面不变」 |

> `X1` 指的 `:891/893` 两处原句（`α *= (…) ? 1 : 0` 与「矩形外整块 α=0」）在 #1 里逐字订正。
> 另外按铁律 5「同一句话被复制到别处要一起改」把同一口径的 **10 处副本**（#2~#12）一并订正。

---

## 三、判据出处

- **本轮判据（只读它）**：`资料/普查产出_1024d/X1_三处判据补查.md` **§一**（`:9-83`）。
- 它引的一手来源（未重查，照引）：
  - `bundle_shaders_assets_all/Shader/Shader_-2318977921299023320.json`（`Everguild/Card ImageUI`）：
    `_Clamp_Range` 在 `$Globals` byte 112 = cb0[7]、`m_Attributes=[]`、`m_DefValue=(0.81,1,0.18,0)`。
  - 四个 PS 变体反汇编（`工具/disasm_dxbc.py`）：`ge → and → movc → mad_sat`，`mad_sat dst,a,b,c = saturate(a*b+c)`。
  - 材质：全库只有 3 个带该属性，只有 `Card Image Overflow (0.82,1,0.18,0)` 真生效（190 处引用全在 `CardImage` 节点）；另两个是死值。
  - `CardImageController__ChangeState.c:30-32` 提前 return ⇒ 产品路径用材质序列化值 0.82。

---

## 四、是否改变可见行为 —— **否**

理由（三条，缺一不可）：

1. **改动全部落在注释与 XML doc 上**。`git diff -U0` 过滤掉 `//` / `///` 行后，本文件只剩**一处**非注释改动：
   `:2357` 的 `From3DBody(0f, 0f, 0f)` —— 那是**本批之前别的代理**改的（残骸体挂点），**不是我动的**。
2. **生产路径不套 clamp**：`ArtClampKindFor` 恒返回 0（`CardView.cs:10701`），`ArtLayerMeshFor` 传 `clampKind: ArtClampKindFor(...)`（`:10721`）
   ⇒ `clampCard` 恒空 ⇒ `ArtMeshInFrame` 那段取交集**一次都不执行**（`DebugDisableArtClamp` 之外没有任何路径把它翻非 0）。
3. **`_artFront` 是 opt-in**：`UseFrontLayer` 出厂 `false` ⇒ 它那两档 `clampKind 1/2` 在出货画面里也走不到。

> ⇒ 按任务 ③，**纯注释改动不加断言**。现有断言（`CardBaseDemo.AssertTacticArtClamp` /
> `CardFaceProbe` A7 ④a·④b）断的是**我们这层网格的几何**，口径没变、期望值没变，不需要改。

### 是否需要改行为（如实答，本批不做）

**需要，但那是另一件事**。原版是**逐像素**做 `saturate(alpha + inside01)`：
矩形**外**用贴图自己的 alpha；我们 `_art` 用 `ArtOpaque`（α 恒 1）⇒ **矩形外那圈我们比原版更不透明**
（`X1` §一·⑤ 记的同一件事）。要真复刻得改渲染侧（让 `_art` 在矩形外采贴图 alpha），
**属于「改渲染行为」，任务明确禁止顺手改** ⇒ 只记录，建议主对话单独立账。
`_artFront`（opt-in）则是「缩网格」近似原版的「矩形内顶到 1」，同样是近似而非等价。

---

## 五、没做到 / 未查清

- `X1` §一「还没查清」的 `DOColor` tween 支第 3 实参是否真为 `_Clamp_Range` 的 propertyID —— 本批未重查，
  不影响本次口径（产品路径走不到状态 1）。
- 除 `CardView.cs` 外，其它文件里可能还有同一句错口径（白名单禁止改）。本批**未**去 grep 其它 `.cs`；
  若主对话要收口，建议按其指示另派一件。

---

## 六、顺手发现（不属于本批改动）

1. **typecheck：运行时程序集 0 错；编辑器程序集 3 错，全在别人的文件**
   `Assets/CardPresentation/Editor/BattleScene.cs(2429,58)`：`CS1525 / CS1073 / CS1056`，
   实际内容是插值字符串里用了**全角括号**：
   `+ $"{expectBadged} 张**（那份数据里过三列的徽标图 {（inPlay == null ? -1 : inPlay.Count）} 种）"`
   —— `{（…）}` 里的 `（` 不是 C# 语法。**不是我的文件、也不是本批改动**；隔 20 s 用独立 `TMPDIR` 重跑仍在
   （判据 = 铁律 13·3：错误全部集中在某个不属于自己的文件 ⇒ 报告、别去改）。
2. `CardView.cs` 在本批开始前**已经是 M 状态**（别人刚改过残骸体挂点，`:2357`）—— 按符号找的位点，未受影响。

---

## 七、自检记录

```text
cd /d/4/Unity && TMPDIR=/tmp/wf_clamp bash d:/4/Unity/工具/typecheck.sh
  --- 运行时程序集 ---   运行时错误数: 0
  --- 编辑器程序集 ---   编辑器错误数: 3（全部在 Editor/BattleScene.cs:2429，见 §六）
行尾：CRLF 11966 / LF 11966（全 CRLF，未被翻）
git diff --numstat（CardView.cs，含本批前别人的 1 行代码改动）：98 / 44
```
