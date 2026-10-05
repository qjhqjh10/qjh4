# F3 · `CollectionScene` 那条红（`qOut` 期望 [0] 实得 [12]）

> 分类 **(b) 断言扫错了顶点** —— 实现是对的，改的是断言。白名单内改动：只 `Editor/CollectionScene.cs`。
> 作于 2026-10-11（本机日期 2026-10-05，文件名照派单给的 `_1010`）。

---

## ① 结论

**红的那条断言扫错了顶点集合**，不是裁切坏了。

`tm.vertices`（TMP 真上传的那份网格）里**不是「一个个字的四角」**：它还装着**非字形的退化占位槽**
（四角全被 TMP 写成 `Vector3.zero`、零面积、不进三角形 ⇒ **画不出来**）。整条数组一扫，
这些点被换算成像素后停在**那一段字自己的原点**上；这张卡压在下沿、字整段在视口外时，
它们就留在视口外 ⇒ 12 个（= **3 槽 × 4 点**）被数成「字画到视口外」。

**改法**：按 `characterInfo[i].vertexIndex` **只数字形四角** —— 读的仍是 `MeshFilter.sharedMesh`
（**真上传的那一份**，⛔ 没退回 `textInfo`）。这正好与 `MenuDraw.ClipTmpMesh` 的集合一致
（那份实现只认 `isVisible` 的字 ⇒ **压根不夹占位槽**）。

**判别力**：三条改坏法**都还成立**（逐条复核见 ③）。改前这条断言在本块的**唯一**用例下是**恒红**
（实得 12 > 0）⇒ 它等于没有判别力；改完才第一次能变绿。**改法不是「为了绿而削判别力」**：
削掉的是**画不出来的零面积槽**，一个字形都没少。

**还证明得了「文字被裁住了」吗** —— **能**，但有两个它证不到的边界（见 ④）。

---

## ② 证据

### 谁的语义对 —— 房子这侧

| 处 | 行 | 说明 |
|---|---|---|
| `Shell/MenuDraw.cs` | `:855-910`（`ClipTmpMesh`） | `:867 if (!ch.isVisible) continue;` · `:872-875` 取 `ch.vertexIndex + 0..3` ⇒ **裁切的定义域 = 可见字的四角**，占位槽不在内 |
| `Core/CardView.cs` | `:3642` | 卡上的文字裁切**转调** `MenuDraw.ClipTmpMesh`（A250 收口） |
| `Editor/CollectionScene.cs` | `:2419-2428` | **本次改**：`GlyphVertsOf(...)` 之后只走字形下标 |
| `Editor/ShellScene.cs` | `:123-149`（`TextMeshWidthPx`） | **先例**：这处早就是「按 `characterInfo` 取四角 + `materialReferenceIndex` 选数组」的写法 |
| `Editor/IconSizeProbe.cs` | `:140-148` | 同上，也是按字取 |

### 为什么 `vertices` 里会有非字形槽 —— TMP 源码（本机 `Library/PackageCache`，第一手）

路径 `d:/4/Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/`：

| 文件:行 | 事实 |
|---|---|
| `TextMeshPro.cs:4530-4541` | 不可见的字（空格 / 超出 `maxVisibleCharacters`）四角**全写成 `Vector3.zero`** 并 `isVisible = false` |
| `TextMeshPro.cs:4549-4552` | **紧接着照常调 `FillCharacterVertexBuffers(i)`** ⇒ 那 4 个零**照样写进网格数组** |
| `TMP_Text.cs:5524-5546` | `index_X4 = meshInfo[mi].vertexCount; characterInfo[i].vertexIndex = index_X4;` … `vertexCount = index_X4 + 4` ⇒ **每个字（含不可见的）恒占 4 槽** |
| `TMP_MeshInfo.cs:249-261` | `ResizeMeshInfo` 按 2 的幂 `Array.Resize` ⇒ 尾巴上还有一段**从没写过的零槽** |
| `TextMeshPro.cs:408-424` | `UpdateVertexData`：`i == 0 → mesh = m_mesh`、`mesh.vertices = meshInfo[0].vertices`（**整条数组，含上一条的零尾**）；`i > 0 → m_subTextObjects[i].mesh` |
| `TextMeshPro.cs:17` | `[RequireComponent(typeof(MeshRenderer))]` ⇒ `MeshRenderer` 与 `TextMeshPro` **同 GameObject**（`RenderExtentPx` 里那个 `mr.GetComponent<TextMeshPro>()` 才成立） |

⇒ 判据链闭合：**12 = 3 个不可见字的槽 × 4 个零角**，它们不在任何三角形里、画不出来，
而 `ClipTmpMesh` 也从不碰它们。

---

## ③ 改动清单（只 `MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`）

| 行 | 改动 |
|---|---|
| `:300-301` | `RenderExtentPx` 的 doc 补一句：**TMP 那份网格只量字形顶点** |
| `:302-330` | `RenderExtentPx`：TMP 自家网格改走 `GlyphVertsOf`（`gi.Count == 0` ⇒ 这一段一个点都不量）；别的网格照旧逐顶点量 |
| `:332-338` | `TextExtentPx` 的 doc 补同一句 |
| `:339-369` | `TextExtentPx`：由「遍历 `meshInfo[].vertices` 整条」改成「遍历可见字、取 `vertexIndex + 0..3`」（仍读 `textInfo` —— 这条的语义本来就是读模型） |
| `:371-407` | **新增** `static List<int> GlyphVertsOf(TMPro.TextMeshPro tmp, int meshVertCount)` —— 收 4 个条件：`isVisible` · `materialReferenceIndex == 0` · 下标边界；注释里写全了上面那 6 条判据 |
| `:2378-2381` | A250 块头部补一条 F3 记录（原写法为什么假红、现在按字取、三条改坏法复核结论） |
| `:2408-2428` | **红的那条**：`for (i < tvs.Length)` → `for (i < gv.Count)`，取 `tvs[gv[i]]`（`qn/qEdge/qOut/qy1/qy2` 的算法一字未动） |
| `:2430-2433` | 两条断言的文案改成「**上传的网格里的字形顶点**（N 个角）」/「按 `characterInfo[i].vertexIndex` 只数字形」 |

**同族一并改了**：`:300-345` 那两处（`RenderExtentPx` + `TextExtentPx`）**形状相同、一起改**，⛔ 没只改被点名的那一处。

### 改坏法逐条复核（**静态推理 —— 没跑 Unity，未实测**）

| 改坏法 | 改完还红吗 | 为什么 |
|---|---|---|
| ① 删掉 `CardView.ClipTextMesh` 里那句转调（文字不裁） | **仍红** | 不裁 ⇒ 这张卡那几段字整段在 `vp.y2` 之下 ⇒ 每个**字形角** `p.y > vp.y2 + 0.5` ⇒ `qOut = qn > 0` |
| ② 公共件改成「只写 `textInfo`、不上传」（删 `UpdateVertexData`） | **仍红** | 上传网格里仍是**未夹的**字形位置 ⇒ 同上（这正是本条值钱的地方：读的是上传那份） |
| ③ 抽掉 `RebuildCardsCells` 里 `v.SetPose(…, CardsViewport)` 第 4 实参（整卡不裁） | **仍红** | 同 ① 。顺带 `qEdge` 也会塌（贴边顶点没了），第二条断言一起红 |
| ④（新）把 `ClipTmpMesh` 改成「连占位槽也夹」 | **不影响** | 本条压根不量占位槽；改完也不该量 |
| ⑤（新）有人把这条断言改回「扫整条 `tvs`」 | **立刻变恒红**（不是变绿） | 就是本次的教训，已写进代码注释 |

---

## ④ 没查清的部分

1. **没跑 `CollectionScene.Run`** ⇒ 「红 → 绿」是**推理**，不是实测（Unity 批处理全局串行、只有调度台能跑）。
   推理里唯一的外来输入是「12 = 3 槽 × 4 点」这条诊断结论 —— 我复核了**机制**（② 那 6 条判据），
   但**没有独立核对「恰好 3 个槽」这个数**（要跑起来数 `characterInfo` 才能确证）。
2. **那 3 个槽到底是哪一类**：`isVisible == false` 有三种来源（空格 / 超出 `maxVisibleCharacters` / 2 的幂零尾），
   三种都被本次过滤掉，**但具体是哪一种没查**（不影响修法）。
3. **卡面标签是不是单材质**：只静态查了字体资产（`Resources/Fonts/NotoSerifCJK-Regular SDF.asset`，
   `m_FallbackFontAssetTable: []`，单图集单材质 ⇒ 字体材质恒是 `materialReferenceIndex 0`）。
   卡面文字里还有 `<sprite name="…">` 图标（`Core/CardIcons.cs:202` · `Core/CardText.cs:268`）——
   它们**是另一个材质**，但我**没跑起来确认收藏窗那张卡上实际有几段带图标、以及 `materialCount` 是不是 > 1**。
4. **改后这条还证明得了「文字被裁住了」吗** —— **能**，但有两个边界：
   - ✅ 三条合起来是真的判据：`qn > 0`（真有字形）+ `qOut == 0`（一个字形角都不在视口外）
     + `qEdge >= 4`（**有角恰好贴在视口边** = 被夹过的指纹）。本块的前提（那几段字整段在视口外）
     是几何事实，所以 `qOut == 0` 只可能由「被搬进来」造成。
   - ⚠️ **边界 A**：`qEdge` **两条横边都收**（`|p.y − vp.y1|` 或 `|p.y − vp.y2|`）。本块的前提是**切下沿**，
     所以更贴的写法是只认下沿：`qEdgeBot >= 4 && Mathf.Abs(qy2 - vp.y2) <= 0.5f`
     （**建议，未落地** —— 落地后能不能绿我没法验，故不擅自加断言）。
   - ⚠️ **边界 B**：只覆盖 `materialReferenceIndex == 0`（字体材质）的字形。
     带 `<sprite …>` 的图标顶点在**子件自己的网格**（`TMP_SubMesh`，`Runtime/TMP/TMP_SubMesh.cs`）上，
     这条断言**量不到**（**改前也量不到**，不是本次引入）。要覆盖得另加一条按子件 `MeshRenderer`/
     `TMP_SubMesh` 枚举的扫描（**建议，未落地**）。

---

## ⑤ 顺手发现（只报不改）

1. **同族扫法在别处已经是「按字取」**：`Editor/ShellScene.cs:123-149` · `Editor/IconSizeProbe.cs:140-148` ·
   `MainMenuScene.cs:7264-7273` · `RewardsScene.cs:340-351` —— 全都按 `characterInfo` 取。
   我把整个 `Assets/CardPresentation` 扫了一遍（`sharedMesh.vertices` / `mesh.vertices` /
   `meshInfo[].vertices` 三种写法）：**除本次这两处，没有第三处整条扫**。
   ⇒ 这条「整条扫 `vertices`」的坑**只存在于 `CollectionScene.cs`**，现在两处都收了。
2. **`RenderExtentPx` 的覆盖与红那条不同**（都是有意为之，现已写进注释）：
   它枚举的是 `MeshRenderer` ⇒ **会**量到 `TMP_SubMesh` 子件的网格（那部分仍是整条扫，行为未变）；
   而红那条枚举的是 `TextMeshPro` ⇒ 子件**根本不在枚举里**。
   要「图标也被量到」，得在红那条里补子件枚举（见 ④ 边界 B）。
3. **`qn` 的口径变了**：`（前提）…量得到**上传的网格**（{qn} 个顶点）` 现在数的是**字形角**
   （文案已改）。别的自检文件若照抄过这句话，别按旧意思读。

---

## ⑥ 跑过的检查

| 检查 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_f3 bash d:/4/Unity/工具/typecheck.sh` | **运行时错误数: 0 · 编辑器错误数: 0** ✅（用独立 `TMPDIR`，避免与别的写手互相覆盖） |
| `CollectionScene.Run`（那条红的正主） | **没跑** —— 批处理归调度台在同步点按覆盖面跑（本件是「只动一个自检宿主」的形态） |
| 行尾 | 文件本来就是 **LF**（`CRLF 0 / LF 4327`），改后仍是 LF ⇒ `git diff --numstat` 正常（无整篇翻行尾） |
