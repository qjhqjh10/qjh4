# B117-FIXH — 换牌卡「材质队列」D6⑦（全项目最后一条失败断言）

> 日期 2026-11-02 · 修复代理 `B117-FIXH` · 断言位置 `Editor/BattleScene.cs:29347`（**未改**）
> 只动两个文件：`Core/CardView.cs` · `Battle/MulliganPanel.cs`

## 一、真因（`B117-FIXF` 那版的定位只对了一半，就地订正）

失败件（`_tmp_view/battle.log:800479`）**9 颗，全部是 `TMP SubMesh [...]`**，主网格一颗都不违规：

```
#0/TMP SubMesh [Asar-Regular Atlas Material ...]=3000 · #0/TMP SubMesh [Pragati-Regular ...]=3000
#1/TMP SubMesh [Asar...]=3000 · #1/TMP SubMesh [Pragati...]=3000 · #1/TMP SubMesh [Warpforge Trait TextSprites Material]=3000
#2/TMP SubMesh [Asar...]=3000 · #2/TMP SubMesh [Warpforge Trait TextSprites Material]=3000 · #2/TMP SubMesh [Pragati...]×2=3000
```

`FIXF` 记的机制是「`OnEnable → SetAllDirty → UpdateMaterial`（`TextMeshPro.cs:307-320`）把渲染器换回
`m_sharedMaterial`」。**那条路只对主网格成立，而且它发生在 `OnEnable` 的同步段 = 在我们盖章之前**
⇒ 后面的 `SetQueue` 会把它重新盖回去，所以它**不是**本缺陷的成因（这正好解释了「主网格一颗不违规」）。

**真凶 = 延迟一拍的文本重建：**

1. 卡体 `SetActive(false) → (true)` ⇒ `TMP_Text.OnEnable`（`TextMeshPro.cs:630-668`）→ `SetAllDirty()` ⇒ 文本标脏；
2. 真正重建**不在这一拍**，而在 `Canvas.willRenderCanvases → TMP_UpdateManager.DoRebuilds`
   （`TMP_UpdateManager.cs:52`）。自检里那一拍就是紧随其后的 `CaptureBoard(pxShadeOn)` / `Shot(...)` 的 `Camera.Render()`；
3. 重建里 `SetArraySizes`（`TextMeshPro.cs:1947-1952`）：

```csharp
if (m_subTextObjects[i].sharedMaterial == null
 || m_subTextObjects[i].sharedMaterial.GetInstanceID() != m_materialReferences[i].material.GetInstanceID())
{
    m_subTextObjects[i].sharedMaterial = m_materialReferences[i].material;   // ← 字体图集 / sprite 材质，队列 3000
```

⇒ 我们盖在子网格渲染器上的 3007 克隆**被静默换回 3000**。这就是「按两次眼睛之后整段掉到压暗层 3006 之下」。

## 二、TMP 源码核过的两件事（简报要求）

| 问题 | 结论 | 依据 |
|---|---|---|
| 设 `fontMaterial` / `fontSharedMaterial` 会不会再触发 `SetMaterialDirty → UpdateMaterial` 反而把渲染器换回去？ | **不会**。`UpdateMaterial` 只在两者**实例不同**时才赋值；我们在 `mr.sharedMaterial = cached` **之后**再把内部那份指到同一实例 ⇒ 比较相等 ⇒ **无操作** | `TextMeshPro.cs:307-320` + `:1233-1245` |
| 会不会每颗 TMP 都新建一份实例材质？ | **不会**。造实例的是 `fontMaterial`（`GetMaterial`，`TextMeshPro.cs:1182-1201`）；`fontSharedMaterial` 的 setter 只是**存引用 + 重算 padding + SetMaterialDirty**。我们赋的就是 `QueueMaterial` 里**按 (源材质, 队列) 共享**的那一份克隆 | `TextMeshPro.cs:1233-1245` · `CardView.QueueMaterial` |

## 三、改了什么

### 1. `Core/CardView.cs`（根因，行数守恒）
- `StampQueue`：`st.Assigned = cached;` 这一行**同一物理行追加** `SyncTmpSharedMaterial(mr, cached);`
  （该行原来没有 `//` ⇒ 不存在「代码落进注释」的坑；`CardView.cs` 账里最深锚点 `:15640`，行数不变得以保住）。
- 类尾新增两个成员（`:15999` / `:16023`，均在最后一条锚点之后）：
  - `static void SyncTmpSharedMaterial(Renderer, Material)`：把**主 TMP** 的 `fontSharedMaterial` 指到渲染器上那份。
    **不动子网格** `TMP_SubMesh`：它的 `sharedMaterial` 每次重建都被 `SetArraySizes` 重写（同步了也留不住），
    而且它的 setter 在编辑器下会给 GameObject **改名**（`TMP_SubMesh.cs:527-530`）——白改还制造噪声。
  - `public void RestampQueuesAfterEnable()`：**先 `ForceMeshUpdate()` 把重建逼出来**
    （`OnPreRenderObject` 在 `TextMeshPro.cs:2158-2159` 清掉 `m_havePropertiesChanged` / `m_isLayoutDirty`
    ⇒ 之后那一拍不会再重建），**再 `SetQueue(_queue)`** ⇒ 最后落到渲染器上的就是我们盖的那一份。
    只对 `textInfo.materialCount > 1`（真有子网格）的 TMP 逼重建，未激活的跳过。

### 2. `Battle/MulliganPanel.cs`
- `SetContentHidden(show:true)`：`SetQueue(QMulliganCard)` 之后追加 `RestampQueuesAfterEnable()`。
- 上一版（`FIXF`）那段**根因说明按铁律 5 就地订正**（把「UpdateMaterial 换回去」改成「延迟重建的 SetArraySizes」）。

## 四、验证

- ✅ `TMPDIR=/tmp/wf_fixh bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ⏳ Unity 批处理由调度台统一跑。**预期：`BattleScene.Run` 的 `D6⑦` 转绿（预计 10132 通过 / 0 失败）**。
  - 机理自证：重建被 `ForceMeshUpdate` 提前吃掉并清脏 ⇒ 之后渲染那一拍不再重建 ⇒ 子网格材质停在 3007。
  - 若仍红，最可能的残留是「重建在 `RestampQueuesAfterEnable` **之后**又被别的东西标脏」——
    那时把 `ForceMeshUpdate` 换成 `Canvas.ForceUpdateCanvases()` + 再盖章即可（本次未用，因为批处理下它只跑 UGUI 那半边）。
- ⛔ 断言 `D6⑦` **一字未动**、未放宽、未改恒真。

## 五、顺手发现（没改，交调度台分流）

- `TMP_SubMesh.OnEnable`（`TMP_SubMesh.cs:291-293`）会把 `_ClipRect` **写进 `m_sharedMaterial`**。
  在「内部材质 = 字体资产那份」的情况下，那是**写共享字体资产材质**（本项目 2026-09-27 踩过的同一类）；
  `SyncTmpSharedMaterial` 之后这一写落到我们自己的克隆上，顺带把这条隐患收了。
