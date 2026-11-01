# B116_W17 · `B-326`：`ImageQuad` 补 `uv1` 通道

**结论：接了。** `Battle/ImageQuad.cs` **997 → 1,079 行**（+82，**全部追加在类尾**；三处调用是**同物理行追加**，行数不变）。
纯 LF、裸 CR = 0。⛔ 没跑 Unity、没动 git / 账本 / 活状态表 / shader。

| 闸 | 读数 |
|---|---|
| `工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `工具/check_ledger_points.py` | A **5,831 通过 / 漂移 0 / 失败 0**（本文件被账引用 8 行：`:119-141` `:349` `:457` `:517` `:711` —— **行号全部未动**） |
| 行尾 | 前 997 行 / CRLF 0 / 裸 CR 0 → 后 1,079 行 / CRLF 0 / 裸 CR 0（同口径） |

---

## 一、判据（沿用 `B116_W11`，未重查）

- 编解码：`uv1 = (xMin − 2·影 − 4·镜, yMin, xMax, yMax)`，**只有 `.x` 被偏移**（`b16.hlsl:122-128`）。
- 生产者（原版）：`GraphicProxy.s_OnModifyVertex` ⇒ `vt.uv1 = GetBounds(这批顶点的 uv)` —— 即**该图形这批顶点 uv 的包围矩形**。
- 「只有挂 `UIEffect` 的图形才有 uv1」 ⇒ 我们的等价物 = **渲染器上的材质是 `CardPresentation/WildcardShine`** 的 quad
  （全工程**唯一**会读 `uv1` 的 shader，`grep "uv1" Resources/Shaders/` 可复核）。

## 二、做法（↔ 判据）

1. 新增 `SyncShineUv1()`（类尾）：`UsesShineShader` 为假 ⇒ **第一句就 return**（连 `mesh.uv` 都不读）。
2. 写什么 = **现存网格 uv 通道（uv0）的包围矩形**，**每个顶点写同一组 `(xMin, yMin, xMax, yMax)`**，
   走 `mesh.SetUVs(1, List<Vector4>)`。🔴 **必须是 `Vector4` 重载** —— `Vector2` 重载会把 `z/w` 写成 0，
   shader 那边 `uv1.z > uv1.x && uv1.w > uv1.y` 恒假 ⇒ 落到兜底、白接。
3. 三个调用点（全部**同物理行追加**，零行数变化）：
   - `RebuildMesh` 尾 `:503`：单 quad / 平铺 / `fillAmount` 裁剪后那一段（与真实网格同源）；
   - `RebuildNineSliceMesh` 尾 `:977`：九宫格**单网格** ⇒ 包围矩形 = **整块 sprite 矩形**（⛔ 不是每段自己的）；
   - `SetMaterial` 尾 `:303`：**这一句是关键** —— 挂载顺序是「先建网格、后换材质」
     （`WildcardShineLayer.Attach`：`new Material(sh)` → 配置 → `q.SetMaterial(mat)`），没有它 uv1 永远补不上。
4. 读口（自检用，风格同既有 `IsNineSlice` / `SoftEdgeKidCount`）：`UsesShineShader` · `Uv1VertexCount`。

## 三、「不变」的证明

1. **代码路径**：非 shine 材质 ⇒ `SyncShineUv1` 一次 null 检查 + 一次 shader 名比较后返回；
   本类的 `_mf.sharedMesh` **只**在 `RebuildMesh` / `RebuildNineSliceMesh` 里赋值（外部无写入点，
   `grep "sharedMesh =" CardPresentation/` 已核：其余命中全在 `CardView` / `Label` / `Backdrop`）⇒
   **非 shine quad 的 vertices / uv / colors / triangles 一个字节不变**。
2. **机制上也看不出差**：四条盒测（`WildcardShine.shader:553-556`）只在**网格被外扩**（描边/阴影/边缘）时才裁得掉东西，
   本链**从不外扩**（`B116_W11` §一）⇒ 所有顶点都落在自己的 uv 矩形内 ⇒ `wfBox ≡ 1`；
   而 `uv1.x = xMin ≥ 0` ⇒ `wfShadow`（原版 `_287`）**恒 false** ⇒ **Shadow 支仍不可达**。
   ⇒ 与 W11 那条「无数据兜底 = 单位矩形」的结果**逐项相同**，只是数据来源从兜底换成了真值。
3. **镜像 uv 不会误开影子闸**：全仓 6 处 `SetUvRect(new Rect(1, 0, -1, 1))`（负宽镜像），
   我们按**顶点 uv 的 min/max** 算 ⇒ 归一化成 `(0..1)`，`uv1.x` 不会是负数。

## 四、下次 Unity 窗口怎么验

- 🔴 **`_287` 读不到**：它是 frag 里的**局部变量**（`bool wfShadow = u1x < 0.0;`，shader `:546`），**不是 uniform**
  ⇒ C# 侧无法读、也**不要期望它翻转**。代理读数（三条）：
  ① `ImageQuad.Uv1VertexCount` —— 应 = `mesh.vertexCount`（单 quad **4** / 九宫格单网格 **4×格数**；**0 = 通道没接上**）；
  ② `mesh.HasVertexAttribute(VertexAttribute.TexCoord1)`；
  ③ 材质侧 `Mat.HasProperty("_TransitionRate")`（shader 根本没编出来的对照口，见 W11 §六·1）。
- 两处画面率：**`CollectionScene.WildcardShineRate`** · **`DeckScene.WcShineRate`**（`-1` = 层没挂）。
- **期望 = 逐像素相同**（理由见三·2）。若出现差异：那不是本次改动的问题，而是「某处 `SetUvRect` 的矩形与它实际绘制的网格不符」
  被真值暴露出来了 ⇒ 先查那处调用点，别回退这一段。

## 五、顺手发现（交调度台分流，本代理未改动任何别处）

1. 九宫格有**两条实现**、语义不强一致：单网格（`SetNineSlice`）⇒ 整块矩形；`CreateNineSlice` 的 **9 个子 quad** ⇒
   各写自己那一段。后者按「一个图形 = 一个 quad」口径是自洽的；**但若将来真做描边/阴影外扩，两条路的裁剪框会不同** —— 记一笔。
2. W11 在 shader 里留的「无数据兜底」现在对**本链**已不会触发（数据总是真值）。**按红线没删**（仍留给非 `ImageQuad` 生产者）。
3. 没建新断言文件：本项目已有「断言文件建了但没人接线」的先例，而 `DeckScene` / `CollectionScene` 两个宿主不在我简报的白名单里
   ⇒ 只给了上面的**读口**，接线由调度台派给宿主写手（一条 `Check(q.Uv1VertexCount == 4, …)` 即可）。

## 六、改动清单（单文件 `CardPresentation/Battle/ImageQuad.cs`）

| 位置 | 改动 |
|---|---|
| `:303` `SetMaterial` 末 | 同物理行 + `SyncShineUv1();`（换材质即补 uv1） |
| `:503` `RebuildMesh` 末 | 同物理行 + `SyncShineUv1();` |
| `:977` `RebuildNineSliceMesh` 末 | 同物理行 + `SyncShineUv1();` |
| 类尾 `:997+` | 新增判据注释段 · `const ShineShaderName` · `UsesShineShader` · `Uv1VertexCount` · `SyncShineUv1()`；**🔶 自己设计** 标记 1 处（「按 shader 名判定」这一跳，写全 是什么/为什么必须自定/谁定的+日期） |
