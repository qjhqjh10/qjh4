# B9_W22GLOW —— `UIBorderGlow` 五条 C 结项

域：`特效动画` · 原版类：`UIBorderGlow` · 日期：2026-10-27
被改文件（唯一）：`Unity/MyGame/Assets/CardPresentation/Shell/UIBorderGlow.cs`

---

## 一、结论

五条 C **全部 C → A**，且**没有**依赖「`_Reveal` shader 缺失」那件事（shader 本体仍不在本轮范围）。
另顺带修掉 **2 处原版记录错**（铁律 5）与 **1 处顺序隐患**（会把 `_Reveal` 做出来后也看不见 / 污染共享材质）。

| 账行 | 方法 | 现等级（现核=改前） | 改后 | 一句话 |
|---|---|---|---|---|
| 425 | `CalculateViewPosition()` | **C** | **A** | 原版只有静态降级形态（没相机缓存）；现组件侧补实例方法，逐句对齐 `.c` |
| 430 | `ModifyMesh(VertexHelper)` | **C** | **A** | 本类现在**真的实现 `IMeshModifier`**；`ImageQuad` 那侧由载体自动装上（原来根本没装） |
| 432 | `OnRectTransformDimensionsChange()` | **C** | **A** | 照原版两句实现（空 `ModifyMesh(null)` + 丢返回值的 `CalculateViewPosition`） |
| 433 | `OnTransformParentChanged()` | **C** | **A** | 同上；两个 `.c` 机器码完全相同 = **ICF 折叠**，不是「文件名与内容不符」 |
| 438 | `Update()` | **C** | **A** | 照原版形状：`hasChanged` 闸 → 触发重写 → 把标志清回 false |
| 423 | `Awake()` | A（**记录有错**） | A | 「`graphicOriginalColor` 只写不读」**不成立**，已订正 |
| 428 | `DoAnimation()` | A（**记录有错**） | A | `+0x68` 谜团已解 = `graphicOriginalColor.a`，原版**会还原 α**；已照抄 |
| 434 | `Play()` | A | A | 顺序调整：先克隆材质再改 α（否则污染共享材质） |

**滚动推进口径下的「批次」含义**：这 5 条的共同卡点是「载体没通」，而载体的**安装**正好落在本文件
（`ForgeTab.cs:1043` 那个唯一挂载点只 `AddComponent<UIBorderGlow>()`，**没挂 `UIViewportUvWriter`**）。
所以修法**全部在 `UIBorderGlow.cs` 内**，`ForgeTab.cs` / `CardDisplayEffects.cs` 一行没动，也不在「等 J-04」那批了。

> 「等包 J-04（`ImageQuad` 原生写 4 分量 uv）」**不再是这 5 条的阻塞条件** —— 载体自愈（每次 `ImageQuad.RebuildMesh()` 都 `new Mesh()`，载体比对**网格实例**发现变了就整篇重写）。J-04 落地后可以**整体删掉**载体（⛔ 别两处都写）。

---

## 二、证据（文件:行号）

判据第一权威 = `d:/2/tools/decomp_full/`（`.c`），交叉 = `out_cpp2il_dev_ilspy` / `out_il2csharp`。

### 425 `CalculateViewPosition()` — `decomp_full/UIBorderGlow__CalculateViewPosition.c`（全 57 行）
- `:16-19` 静态类初始化守卫（Ghidra 噪声，无行为）
- `:20` 读 `+0x48` = `currentCamera`；`:21-24` 取 `op_Implicit`（Unity 的 `!= null`）
- `:25-29` **为空才** `Camera.main` 并写回 `+0x48`（**取到就缓存，不再问**）
- `:31-32` `local_res8 = uStackX_c = 0x3f000000` ⇒ 默认返回值 **(0.5, 0.5)**（`0x3f000000` = 0.5f）
- `:36-37` 相机仍为空 ⇒ 直接返回默认值（**不抛、不报**）
- `:38-47` `Component.transform` → `Transform.get_position` → `Camera.WorldToViewportPoint` → 取 **xy**（`CONCAT44(uStackX_c, local_res8)`）
- 交叉：`out_cpp2il_dev_ilspy/UIBorderGlow.cs:227-261`（ILSpy 把 `0x3f000000` 显示成 `(Vector2)1056964608`，同一件事）
- 我方：`Shell/UIBorderGlow.cs:361`（新）+ `WarpforgeVFX/Runtime/UIViewportUvWriter.cs:85-90`（算法本体，**单源**）

### 430 `ModifyMesh(VertexHelper)` — `decomp_full/UIBorderGlow__ModifyMesh.c`（全 96 行）
- `:44-45` `Behaviour.get_isActiveAndEnabled` ⇒ 闸
- `:46` `CalculateViewPosition()` 只算一次
- `:68` 循环条件 `i < verts.currentVertCount`；`:70` `PopulateUIVertex(ref v, i)`
- `:77-82` `local_68/uStack_64/uStack_60/uStack_5c` 四个连续 4 字节：`uStack_64 = view.x`、`uStack_60 = view.y`
  ⇒ 落在 `UIVertex.uv0` 的 **z / w**（`uv0.xy` 从 `local_d8`/`uStack_d0` 原样带过去）
- `:91` `SetUIVertex(v, i)` 回写
- 交叉：`out_cpp2il_dev_ilspy/UIBorderGlow.cs:288-311`（末尾 `Unsafe.SubtractByteOffset(ref obj2, 112)` = `UIVertex` 布局）
- 我方：`Shell/UIBorderGlow.cs:378`（新，原接口实现）+ `UIViewportUvWriter.cs:98-137`（`ImageQuad` 载体）

### 432 `OnRectTransformDimensionsChange()` / 433 `OnTransformParentChanged()`
- `decomp_full/UIBorderGlow__OnRectTransformDimensionsChange.c`（全 **8** 行）：`:5` `NetworkingPeer__OnMessage(this,0)`；`:6` `UIBorderGlow__CalculateViewPosition(this,0)`
- `decomp_full/UIBorderGlow__OnTransformParentChanged.c`（全 8 行）：**逐字节同形**
- `:5` 那个 `NetworkingPeer__OnMessage` = **空方法桩的 ICF 折叠**：ILSpy 同位置是 `ModifyMesh((Mesh)null)`
  （`out_cpp2il_dev_ilspy/UIBorderGlow.cs:272-276` / `:278-282`），而 `ModifyMesh(Mesh)` 体是空的（同文件 `:284-286`，账 429 判 D）
  ⇒ `.c` 与 ILSpy **并不矛盾**，两边说的都是「一个空调用 + `CalculateViewPosition`」
- 关键：**返回值被丢弃**（`Vector2 vector = CalculateViewPosition();` 之后没人用）⇒ 净效果**只有刷新 `+0x48` 相机缓存**，
  **不重写网格、不 `SetVerticesDirty`**
- 我方：`Shell/UIBorderGlow.cs:407` / `:415`（新）

### 438 `Update()` — `decomp_full/UIBorderGlow__Update.c`（全 27 行）
- `:9-14` `if (!transform.hasChanged) return;`
- `:15-17` `MyGraphic.SetVerticesDirty()`（虚槽 `+0x2f8`，第二个实参是 IL2CPP 的 `MethodInfo`，不是业务参数）
- `:18-21` `transform.hasChanged = false`
- 交叉：`out_cpp2il_dev_ilspy/UIBorderGlow.cs:143-153`（同形；`MyGraphic` = **`MaskableGraphic`**）
- 我方：`Shell/UIBorderGlow.cs:396`（新）

### 423 `Awake()`（记录订正）
- `decomp_full/UIBorderGlow__Awake.c:23-24`：`+0x5c` ← `color` 的 r,g（8 字节）、`+0x64` ← 8 字节 ⇒ **`+0x64`=b、`+0x68`=a**

### 428 `DoAnimation()`（记录订正 / `+0x68` 解谜）
- `decomp_full/UIBorderGlow__DoAnimation.c:42`：`UIBorderGlow__ChangeGraphicAlpha(param_1, *(undefined4 *)(param_1 + 0x68), 0)`
  ⇒ 传进去的就是 **`graphicOriginalColor.a`**
- 位置：在 `SetEase` 之后、`return` 之前 = **同步执行**（不是在补间结束时）
- 结论：原版 `Play()`/`OnEnable()` 把 α 置 0 → `DoAnimation()` **立刻**还原成原值。
  **净可见状态 = 原 α**（辉光靠 `_Reveal` 演，不是靠 α）。若 `CacheMaterial()` 失败（无材质）则不还原 ⇒ α 停在 0（原版 wart）。
- ILSpy 侧那行是 `NoteDecompilerIssue("Unmanaged memory load: [this @ rcx (UIBorderGlow)+68]")` —— **`.c` 能读，别当未知**

### 434 `Play()`（顺序隐患）
- `ForgeTab.cs:1118-1120` `ForgeGlowMaterial()` **缓存一份 `_glowMat` 给所有 cell 共用**
- 我们 `ImageQuad.SetTint` 写的是 **`MeshRenderer.sharedMaterial.color`**（`Battle/ImageQuad.cs` 的 `SetTint`）
  ⇒ 若照原版顺序（先 `ChangeGraphicAlpha(0)` 再 `CacheMaterial()`），会把**共享材质**的 α 改成 0

---

## 三、逐条：原版逐句 → 我们原来 → 改成什么 → 怎么验

### 425 `CalculateViewPosition()`
- **原版**：懒取 `Camera.main` 缓存 `+0x48` → 默认 `(0.5,0.5)` → 有相机 `WorldToViewportPoint(transform.position).xy`。
- **我们原来**：只有 `UIViewportUvWriter.CalculateViewPosition(Camera, Vector3)` 静态降级形态 ——
  **没有相机缓存**（`ResolveCamera` 里 `cam == null` 就每帧再问一次），`UIBorderGlow` 侧**压根没有这个方法**。
- **改成**：`UIBorderGlow.CalculateViewPosition()` 实例方法，逐句对齐 `.c`；相机缓存进 `_currentCamera`；
  **算法本体只转调** `UIViewportUvWriter.CalculateViewPosition`（「两处写同一条规则 = 迟早不一致」）。
  同时把缓存的相机**回填**给载体的 `cam`（仅当载体 `cam` 还是 null）。
- **怎么验**：`SelfCheckViewUv`（见下）+ 断「无相机时返回 `(0.5,0.5)`」（`FallbackViewPosition` 是两个文件共用的同一个常量）。

### 430 `ModifyMesh(VertexHelper)`
- **原版**：`isActiveAndEnabled` 闸 → 算一次视口坐标 → 遍历**全部顶点**，只改 `uv0.z/.w`，`uv0.xy` 与其它分量原样带回。
- **我们原来**：**没有这个接口**（类不实现 `IMeshModifier`）；`ImageQuad` 那侧有等价物，但
  **`ForgeTab.cs:1043` 只 `AddComponent<UIBorderGlow>()`、没挂 `UIViewportUvWriter`** ⇒ 等价物根本不在场 ⇒ `uv0.zw` 恒 0。
- **改成**：① 类上实现 `UnityEngine.UI.IMeshModifier`，`ModifyMesh(VertexHelper)` 逐句照 `.c`（含闸）；
  ② `ModifyMesh(Mesh)` 照原版**留空**（账 429 判 D）；
  ③ `Awake()` → `Bookkeep()` → `EnsureCarrier()`：`GetComponent` 不到就 `AddComponent<UIViewportUvWriter>()`
  ⇒ **原版「装上这个组件就有 `uv0.zw`」的单件契约在我们这套渲染模型上恢复了**，不用改任何挂载点。
- **怎么验**：新断言 `SelfCheckViewUv()`（`Shell/UIBorderGlow.cs:422`）：
  前提硬检查 = 载体在 / 网格在（⛔ 不是 `if (前提) Check(...)`，会静默跳过）；结论 =
  `mesh.GetUVs(0)` 的**分量数 == 顶点数**、且**每个顶点的 `z/.w` == 载体的 `ViewPosition`**。

### 432 / 433 两个 RectTransform 回调
- **原版**：`ModifyMesh(null)`（空实现）+ `CalculateViewPosition()`（**返回值丢弃**）。**不碰网格**。
- **我们原来**：**没有这两个方法**（类不是 `UIBehaviour`，也没写同名消息）。文档把「净效果」记成由载体每帧比对覆盖 —— 结论对，但**入口本身是缺的**。
- **改成**：照原版两句实现（`ModifyMesh(Mesh)` 空实现承担前半句）。**故意不加**「重写 uv0.zw」——
  `uv0.zw` 只依赖**世界位置**，尺寸变了不该重写（原版也没重写）；换父引起的位置变化由载体下一帧比对补上。
- **怎么验**：静态对账（`.c` 两句 ↔ 代码两句）；动态上这两条**无可观测副作用**（只有相机缓存），
  所以**不写断言**（写了也是自证）。

### 438 `Update()`
- **原版**：`if (!transform.hasChanged) return;` → `MyGraphic.SetVerticesDirty()` → `transform.hasChanged = false`。
- **我们原来**：**没有 `Update`**；靠载体 `LateUpdate` 每帧比对（🔶 自己设计，见下）。
- **改成**：照原版形状实现 —— `hasChanged` 闸 → 让载体重写一次（`ApplyNow()` 自己比对，没变不写）→ 把标志清回 false。
  **原版就清这个共享标志**，照抄（同族先例：`Shell/UIScaleToFit.cs:108` 是同一个形状）。
- **怎么验**：静态对账 + 载体自己的 `Writes` 计数；行为上「位置没变 ⇒ 不重写」由 `ApplyNow` 的比对保证。

---

## 四、改动清单（`Shell/UIBorderGlow.cs`，唯一被改的 `.cs`）

行号 **零漂移**：账里引用的 `:97 Awake` · `:103 OnEnable` · `:120 Play` · `:129 Stop` · `:136 ChangeGraphicAlpha`
· `:167 CacheMaterial` · `:190 DelayedAnim` · `:205 Anim` **全部原地不动**（新成员一律追加在类尾 `:305` 之后）。

| # | 位置 | 改动 |
|---|---|---|
| 1 | `:6-7`（头注释） | 就地订正：`ModifyMesh(VertexHelper)` **不**重映射顶点，只写 `uv0.zw`（原来写成「按 `_Reveal` 重映射」） |
| 2 | `:21-22`（头注释） | 就地订正：`graphicOriginalColor` **有被读**（`DoAnimation` 末句还原 α），原「不恢复」是错的 |
| 3 | `:34-37`（头注释） | 就地订正：`+0x68` 已解 = `graphicOriginalColor.a`，原「未知、不复刻」作废 |
| 4 | `:48` | 类声明加 `UnityEngine.UI.IMeshModifier` |
| 5 | `:99` | `Awake` 里 `CacheRefs(); Bookkeep();`（记 `graphicOriginalColor` + 装载体） |
| 6 | `:122` | `Play()` 先 `CacheMaterial()` 再 `ChangeGraphicAlpha()`（避免改到共享材质） |
| 7 | `:219` | `Anim()` 无 `_Reveal` 分支：补 α 还原（否则播一次就永久 α=0 ⇒ 图块消失） |
| 8 | `:230-232` | `Anim()` reveal 分支：**先** `SetFloat(revealFrom)` **再**等 `animDelay`（原版顺序）+ α 还原 |
| 9 | `:330` `Bookkeep()` | 新 —— 原版 `Awake` 尾部两件事（含装载体） |
| 10 | `:339` `EnsureCarrier()` | 新 —— `GetComponent`/`AddComponent<UIViewportUvWriter>`（幂等，🔶 自己设计载体） |
| 11 | `:348` `CurrentColor()` | 新 —— `ChangeGraphicAlpha` 读的那一路颜色（`ImageQuad.Tint` 或 `Graphic.color`） |
| 12 | `:361` `CalculateViewPosition()` | 新 —— 原版实例方法（相机缓存 + 单源转调） |
| 13 | `:371` `ModifyMesh(Mesh)` | 新 —— 空实现（照原版） |
| 14 | `:378` `ModifyMesh(VertexHelper)` | 新 —— 原版逐句 |
| 15 | `:396` `Update()` | 新 —— 原版逐句 |
| 16 | `:407` `OnRectTransformDimensionsChange()` | 新 —— 原版逐句 |
| 17 | `:415` `OnTransformParentChanged()` | 新 —— 原版逐句 |
| 18 | `:422` `SelfCheckViewUv()` | 新断言（前提硬检查 + 结论） |

标记：载体那一段（`_uvCarrier` / `EnsureCarrier`）按铁律 18 打了 **`🔶 自己设计（非原版）`**，
写明「原版这活是组件自己干的 / 为什么我们得旁挂 / 调度台 2026-10-27 裁定」。
相机缓存、`ModifyMesh`、`Update`、两个回调、α 还原 **都是复刻，不打标**。

**验证**：`TMPDIR=/tmp/wf_w22 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时程序集 0 错误**（我这条）。
过程中另两次跑出的错误分别落在 `Shell/CampaignTab.cs` / `Shell/ForgeTab.cs`（W16 在写）与
`Editor/BattleScene.cs`（W11 在写）—— 全部是 `CS0103 当前上下文中不存在名称 AttachArmyShine/SyncArmyShine/K4HoldAssert`，
**不是我负责的文件、也不是我引入的**（按铁律 13·3 记下、不改别人的文件）。

---

## 五、没查清的部分

1. **`OnRectTransformDimensionsChange` / `OnTransformParentChanged` 这两条消息，Unity 到底会不会发给「普通 `MonoBehaviour`」** ——
   **没验证**。`UnityEngine.CoreModule.dll` 里搜不到这两个字符串（消息名在 native 侧），本轮又不许跑 Unity，
   所以没法实测。**但这条不影响结论**：这两个方法在原版里净效果**只有刷新相机缓存**，而载体的 `LateUpdate`
   每帧都会自己比对（含换父引起的位置变化）⇒ 消息到不到，最终 `uv0.zw` 都一样。
   要钉死的话：恢复自检后跑一次 `ShellScene.Run`，在两个回调里各打一条 `Debug.Log` 看有没有。
2. **`MaskableGraphic` vs `Graphic`**：原版 `MyGraphic` 取出的是 **`MaskableGraphic`**（`UIBorderGlow__get_MyGraphic.c:19`
   `GetComponent<MaskableGraphic>()`），我们 `CacheRefs()` 取的是更宽的 `Graphic`。
   对 `color` 这一路**无差**（`MaskableGraphic : Graphic`），但原版在拿不到 `MaskableGraphic` 时会**抛**
   （`Awake.c:26-28` / `get_MyGraphic.c` 走 `FUN_1803f47a0` = IL2CPP 空引用抛出），我们不会。
   这算「我们更宽容」，**没改**（改它属于 423 那行的口径，不是这五条）。**要不要收窄，等调度台裁。**
3. **`_Reveal` shader（`Everguild/UI/UI Border Highlight Appear`）本体**：仍不在本轮范围。
   `UIViewportUvWriter.cs:45-48` 头注释说「它对材质用不用不作判断」——照旧。
   ⚠️ 那个 shader 的算式三种取法全断（RDEF=0 / JSON 空 / `m_CommonParameters` 字段本 build 不存在），
   唯一剩余路 = Unity 差分实验，**需要跑 Unity，不在本代理权限内**。
4. **`ForgeGlowMaterial()` 那份 `_glowMat` 是不是真的被多个 cell 共用**：我按 `ForgeTab.cs:1118-1120`（`static Material _glowMat` 缓存）
   断定「是共享的」并据此调整了 `Play()` 顺序。**没有逐 cell 追调用链**（那文件属 W16，只读）。
   即便判断偏保守，调整后的净结果也与原版一致（原版 `DoAnimation` 末句本来就把 α 还原成原值），**不构成风险**。
5. **本轮没有跑任何 Unity**（红线 1）⇒ 上面所有结论都是**静态对账**级别；
   「`uv0.zw` 写进去之后扫光长什么样」要等 shader 落地 + 进游戏看，属验收，不在本轮。

---

## 六、顺手发现（该立账 / 已被本批消化）

1. 🔴 **`+0x68` 那个「反编译器未解字段」其实是 `graphicOriginalColor.a`** —— 账 428 与文件头都写着「未知、不复刻」。
   后果是**实打实的功能缺陷**：原版播完 α 回到原值（图块可见），我们停在 0（**图块永久消失**）。
   **已修**（`Anim()` 两处还原）。这条同时解释了「为什么 `graphicOriginalColor` 明明只写不读却要存」。
2. 🔴 **`Play()` 的「先置 α、再克隆材质」在我们的载体上是错的** —— 我们 `ImageQuad.Tint` 写的是
   `sharedMaterial.color`，而 `ForgeGlowMaterial()` 返回的是**缓存共享**的那一份 ⇒ 会污染所有 cell。
   **已修**（调换顺序）。
3. 🔴 **`ForgeTab.cs:1043` 那个唯一挂载点只挂了 `UIBorderGlow`、没挂 `UIViewportUvWriter`** ——
   账里 425/430/432/433/438 五条「载体没通」**真正的那一半缺口**就在这里。
   **已在 `UIBorderGlow` 内自愈**（`EnsureCarrier`），`ForgeTab.cs` / `CardDisplayEffects.cs`（W20 在写）**一行没动**。
4. ⚠️ **`OnTransformParentChanged.c` 与 `OnRectTransformDimensionsChange.c` 内容逐字节相同** ——
   这是 **ICF 折叠**（链接器把相同机器码合成一个地址），**不是** CLAUDE.md 里那条「文件名与内容不符」的坑。
   判法：两个方法体在 ILSpy 里也逐字相同、语义一致。
5. ⚠️ **`UIBorderGlow__Update.c:17` 那个 `(**(code **)(*plVar3 + 0x2f8))(plVar3, *(*plVar3 + 0x300))`** ——
   第二个实参是 IL2CPP 的 `MethodInfo`，**不是**业务参数。别把 `+0x300` 当「传了个字段进去」去反推语义。
6. 📌 **`CardDisplayEffects.cs` 那个挂载点（`Level Up Effect Down`）用的材质仍是 `Sprites/Default`**（没有 `_Reveal`）——
   该文件里 2026-10-27 的注释说「缺的是这块图用的材质」。那不是我这轮能动的（W20 的文件），
   但**它现在至少不会因为 `Play()` 而永久变透明了**（改动 7 的 α 还原把这条一并兜住）。

---

## 七、账补丁

`资料/完全复刻/账/_parts/B9_W22GLOW.tsv`（9 行：表头 + 8 行；键 = `(原版类, 方法)`）。
覆盖：425 / 430 / 432 / 433 / 438（C→A）+ 423 / 428 / 434（记录订正，等级仍 A）。
`自我复核`：本文件 `Shell/UIBorderGlow.cs` 的**行号零漂移**，账里那 8 条落点不需要 `--fix`。
