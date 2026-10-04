# X · `RewardsScene.Run` 抛 NRE（`TextMeshPro.UpdateVertexData`）—— 根因 + 修法

> **任务**：`RewardsScene.Run` 实跑抛 `NullReferenceException`、连「合计」都打不出来（`_tmp_view/rewards.log`，23:01）。
> **白名单**：`Shell/MenuDraw.cs` · `Battle/Label.cs` · `Shell/ForgeTab.cs`（只读）· `Shell/MenuScroll.cs`（只读）· 本报告。
> **本件只动了 `Shell/MenuDraw.cs` 一个文件**（`Label.cs` / `ForgeTab.cs` / `MenuScroll.cs` **一个字没改**）。
> **本件没跑 Unity**（简报禁止）⇒「修完不崩」是**静态论证**，实证由主对话在同步点跑 `RewardsScene.Run`。

---

## 结论（根因一句话）

**`MenuDraw.ClipTmpMesh` 那句 `tmp.UpdateVertexData(…)` 会去读 TMP 的私有字段 `m_mesh` 并往里赋值（`TextMeshPro.cs:417/430`，**裸字段、没有任何 null 检查**）；而本轮崩溃的标签是建在【未激活的锻造页】里的 —— 它的 `TextMeshPro.Awake()` 一次都没跑过 ⇒ `m_mesh == null`，可它的字形模型却是满的（`TmpFont.SetWrapWidth` 走的 `GetTextInfo()` **没有 `m_isAwake` 这道闸**、且跳过写网格的 Phase III）⇒ 于是「有字模 + 没有渲染网格」这个状态第一次被 `any == true` 这条路上传踩中 ⇒ NRE。**

**这不是 A225 的回归**：本案 `moved == true`（真有字的角被夹出框），**新旧两种 `any` 口径都会走到那句上传**（见 §五）。
**诱因是 A188 新加的「跨边那一幕」**：它第一次把一格滚到视口左沿（该格中心 `330.968`），使 `LevelLabel`（那一格里唯一贴着视口边的文字）横跨裁切框左沿 ⇒ 第一次产生「被夹的字」。

---

## 一、证据

### 1. 崩的就是哪一句（读**真**反编译，不是猜）

`_tmp_view/rewards.log:7169-7178`：

```
NullReferenceException: Object reference not set to an instance of an object
  at TMPro.TextMeshPro.UpdateVertexData (TMP_VertexDataUpdateFlags) [0x00033] in .\…\TextMeshPro.cs:430
  at CardPresentation.MenuDraw.ClipTmpMesh (…) [0x002e3] in MenuDraw.cs:837
  at CardPresentation.MenuDraw.ClipTextNow (…) [0x0004d] in MenuDraw.cs:745
  at CardPresentation.MenuDraw.ClipText (…) [0x0004e] in MenuDraw.cs:730
  at CardPresentation.MainMenuSubmenuWindow.TextBox (…) [0x0003f] in MenuWindowBase.cs:282
  at CardPresentation.ForgeTab.BuildCell (int) [0x00176] in ForgeTab.cs:733
  at CardPresentation.ForgeTab.BuildRewardCells () [0x0009c] in ForgeTab.cs:672
  at CardPresentation.MenuScroll.SetOffset (float) [0x0004b] in MenuScroll.cs:178
  at CardPresentation.MenuScroll.ScrollBy (float) [0x00000] in MenuScroll.cs:167
  at RewardsScene.Run () [0x036a9] in Editor/RewardsScene.cs:1934
```

`ForgeTab.cs:733` = `var lvLab = _win.TextBox(cell, lv, (i + 1).ToString(), Color.white, "LevelLabel", 45f, 0f);`
—— 是**等级牌那一段文字**（`LevelLabel`），不是格里的图、也不是别处。

**IL 级证据**（探针见 §四；映的是 `Library/ScriptAssemblies/Unity.TextMeshPro.dll` = 本轮真跑的那份，
`Unity.TextMeshPro.dll` 的 mtime **2026-09-16 21:42** 比 `PackageCache` 里的源码 **2026-08-17 06:03** 新 ⇒ **DLL 出自同一份源码**）：

```
TextMeshPro.UpdateVertexData(params=1):
  IL_0000..: m_textInfo.materialCount → for(i < materialCount)
  IL_0013: ldloc i / IL_0014: brtrue  →  IL_0016: ldarg.0; IL_0017: ldfld TMP_Text.m_mesh   ← ① 裸字段读
  IL_001F..: （else 支）ldfld m_subTextObjects; ldelem; callvirt TMP_SubMesh::get_mesh
  IL_0033: ldloc.2                     ← ② 行 430 那条语句从这里开始（= 报的 0x33）
  IL_0035: ldfld TMP_Text.m_textInfo   IL_003A: ldfld TMP_TextInfo.meshInfo
  IL_0040: ldelema TMP_MeshInfo        IL_0045: ldfld vertices
  IL_004A: callvirt Mesh::set_vertices ← ③ 这里把 null 抛出来（`UnityEngine.Mesh` 的 setter 没有 managed 段的守卫）
```

⇒ 报的 `[0x00033]` = **行 430 那条语句的开头**（`0x33` 就是 `ldloc.2`；Mono 的 stack trace 落在
**语句/序列点**上，同一条链上其余各帧报的也全是**调用点**）。行 430 那条语句里能 NRE 的只有两个解引用：
`m_textInfo.meshInfo`（字段读）与 `mesh` 这个局部（= `m_mesh`）。

### 2. `m_textInfo.meshInfo` **不可能**为 null（排除法的一半）

全库扫 `Stfld TMP_TextInfo.meshInfo`（探针 `field meshInfo`，扫 `TextMeshPro` / `TMP_Text` / `TMP_TextInfo` 三个类）：

```
TMP_TextInfo..ctor        IL_004A Stfld TMP_TextInfo.meshInfo     ← 两个 ctor 各一次
TMP_TextInfo.ClearAllData IL_007B Stfld TMP_TextInfo.meshInfo     ← materialCount 同时被清 0
```

三个写着**全是「赋一个新数组」**（`TMP_TextInfo.cs:48/59/74`、`:121`），没有任何一处赋 null；
`TMP_TextInfo.Resize`（`:302-311`）走的是 `Array.Resize` ⇒ 也不会变 null。
⇒ **能 null 的只剩 `m_mesh`。**

### 3. `m_mesh` 只有两个写点 —— 而它只在 `Awake` 里被创建

全库扫 `Stfld TMP_Text.m_mesh`：

```
TextMeshPro.get_mesh  IL_0014 Stfld TMP_Text.m_mesh   ← 懒建（属性）
TextMeshPro.Awake     IL_00B5 Stfld TMP_Text.m_mesh   ← = 源码 TextMeshPro.cs:584
```

**没有任何一处赋 null**；`OnDestroy`（`:692`）与 `Reset`（`:725`）只是 `DestroyImmediate(m_mesh)`（字段留着已销毁对象）。

`TextMeshPro.Awake` 的 IL：**只有一个 early return** ——

```
IL_0000: TMP_Settings.instance  IL_0005: ldnull  IL_0006: (op_Equality)  IL_000B: brfalse → IL_0033（正常体）
IL_002B..: m_isWaitingOnResourceLoad = true      IL_0032: **Ret**        ← 唯一一处提前退出
IL_00A1: ldfld TMP_Text.m_mesh  IL_00A7: ldnull  IL_00A8: (op_Equality)  IL_00AD: brfalse
IL_00B5: m_mesh = new Mesh()    …                IL_00E9: m_textInfo = new TMP_TextInfo(this)
IL_017A: m_isAwake = true                                                 ← Awake 的最后一句
```

⚠️ `m_mesh == null` 用的**是 Unity 的 `==` 重载**（IL 里是 `call op_Equality`，不是 `ldnull; ceq`）
⇒ **已销毁的 `Mesh` 会被判成 null 并重建** ⇒ `Awake` 走完就**不可能**留下一个坏网格。
⇒ **`m_mesh == null` ⟺ `Awake()` 根本没跑过**（对象在**未激活的父链**下 `AddComponent<TextMeshPro>()`）。

### 4. 那「文字却生成过」是怎么来的？—— 我们自己在走的那条**没有 `m_isAwake` 闸**的路

`any == true` 是走到那句上传的前提，而 `any` 要求 `ti.characterCount > 0`
（`MenuDraw.cs:784` 的 `n = Min(characterCount, characterInfo.Length)`）⇒ **字模被生成过**。
字模生成有两条路：

| 路 | 闸 | 会不会碰 `m_mesh` |
|---|---|---|
| `ForceMeshUpdate()` → `OnPreRenderObject()`（`TextMeshPro.cs:2114-2119`） | **要 `m_isAwake`** | 会（Phase III 的 6 处 `m_mesh`） |
| **`GetTextInfo(string)`（`:360-374`）** | **没有任何 `m_isAwake` 检查** | **不会** —— 它把 `m_renderMode = DontRender`，而 Phase III 的闸是 `m_renderMode == Render && IsActive()`（`:5024`）⇒ **跳过** |

而**我们每个 `TextBox` 都在走第二条**：

`Shell/MenuWindowBase.cs:281` → `MenuDraw.TextBox`（`MenuDraw.cs:1179`）→ **`lb.SetWrapWidth(…)`**（`Battle/Label.cs:208`）
→ **`TmpFont.SetWrapWidth`（`Core/TmpFont.cs:208-214`）**：

```csharp
t.textWrappingMode = TextWrappingModes.Normal;
t.rectTransform.sizeDelta = new Vector2(width, 0f);
t.GetTextInfo(t.text);                    // ← 无条件，注释写着「逼它重算 ComputeMarginSize」
```

⇒ **`characterCount > 0` + `meshInfo[0].vertices/uvs0/colors32` 全都有 + `m_mesh` 一个字节没碰**。**这就是那个状态。**

### 5. 现场为什么是「未激活的父链」（有日志实据）

* `Editor/RewardsScene.cs:1557` 断言 **`Forge Tab` 出厂 `activeSelf=false`**（原版 §二·4），
  日志 `rewards.log:5994` 是 **✓**；接着 `:1561/:1565` 只点了第 2 键（Campaign）与第 1 键（Missions）。
* 自检**直到 `:2077` / `:2219` 才 `win.tabButtons.Click(2)` 切到锻造页** —— 而崩溃在 **`:1934`（在它之前）**
  ⇒ 滚动重建那些格时，**锻造页整条父链是关着的**。
* 而滚动回调照样重建：`MenuScroll.SetOffset`（`:178` `OnChanged()`）→ `ForgeTab.BuildRewardCells`
  ——**它不看页签开没开**（`MenuScroll.cs:64-70` 的用法注释也是这么写的）。
* ⚠️ **反推的实据**：整份 `rewards.log` 里 `[MenuDraw]` **零命中**（`grep -c "MenuDraw]" rewards.log` = **0**）
  ⇒ `MenuDraw.TextClipUnavailable` 全程 **0** ⇒ `ClipTextNow` 每次都**取到了 TMP**
  （`lb.GetComponentInChildren<TextMeshPro>()` 在「父链未激活、自身 `activeSelf=true`」时确实返回了它）。
  ⇒ 这也**反证了**「不是点阵兜底那条后端」（那条走 `ClipQuadMesh`，有自己的守卫、不会崩）。

### 6. 为什么**以前**没炸：要「真有一个字的角被夹出框」才会走到那句上传

`any` 的两个来源（`MenuDraw.cs:804-834`）：`moved`（四角被 `Mathf.Clamp` 夹到框边）或 alpha 字节真的变了。
本文那一格里**唯一贴着视口边的文字是 `LevelLabel`**（`ForgeTab.cs:733`；等级牌 85×88，摆在该格横向中心附近），
而它要被夹，**该格中心必须落在裁切框左沿 10px 以内**（`RenderClip` 左沿 = `331.0 + padL10 = 340.968`）。

* 偏移 0 / 最右 / 708（= 前面的几次重建，`:1876/:1882/:1883`）：所有可见格的等级牌都**完整落在框内**
  （格心离框沿远）——**或者整块在框外 ⇒ `MenuDraw.Visible` 直接把那个标签整块不建** ⇒ 没有夹切 ⇒ `any == false` ⇒ 不会走到上传。
* **A188 的「跨边那一幕」（`:1934`）** 把该格中心滚到 **330.968** ⇒ 等级牌横跨 `340.968`
  ⇒ 数字那个字形被夹 ⇒ `moved = true` ⇒ **第一次**走到那句上传 ⇒ NRE。

---

## 二、改动清单（**只一个文件**）

| 文件:行 | 改了什么 | 为什么 |
|---|---|---|
| `Shell/MenuDraw.cs`（`ClipTmpMesh` 的 `if (any)` 那一句，现 `:859-909`） | 上传**之前**加一道判据：`var tmf = tmp.GetComponent<MeshFilter>(); if (tmf == null \|\| tmf.sharedMesh == null) { TextClipUploadSkipped++; 限流警告; return false; }`，通过之后才 `tmp.UpdateVertexData(…)` | `UpdateVertexData` 读的是**私有字段 `m_mesh`**，那一份不在时它会当场 NRE（根因见 §1/§3）。**判据必须用 `MeshFilter.sharedMesh`，⛔ 不能用 `tmp.mesh` 那个属性** —— 属性在 `m_mesh == null` 时会自己 `new Mesh()`（`TextMeshPro.cs:143-155`），既把雷捂住、又让上传进到一份**没挂到 MeshFilter 上、根本不会被画出来**的网格（静默）。`sharedMesh` 同时兜住「已销毁」那一档（Unity 的 `==` 重载判 null） |
| 同上（新增 `public static int TextClipUploadSkipped`，`:770-786`） | 新计数器（**与 `TextClipUnavailable` 分开**） | 「算出来的那一刀没能上传」必须**出声**（红线：不许静默失败）。⚠️ **刻意不并进 `TextClipUnavailable`**：那条在 `Editor/RewardsScene.cs:2891` 被断成 `== 0`，本场景会让它假红。注释里写明「它 != 0 是**正常**的、别断成 0」 |
| 同上（`ClipText` / `ClipTextNow` / `ClipTmpMesh` 三条 summary） | 就地补上「返回值 `false` 现在有两种含义」+ 新计数器 | 铁律 5：旧的 summary 写着「返回 true = 真的动过」、且只提 `TextClipUnavailable` 一种出声方式 ⇒ 改了实现不改说法就是留一条误导给下一个会话 |

**没动的**：`UpdateVertexData` 仍然在「该上传」的时候上传（⛔ 不是「删掉那句」）；
⛔ 没有 `try/catch`；⛔ 没有改 `any` 的口径（A225 那套绝对值写入原样保留）。

---

## 三、为什么不会再崩（逐条论证 · 可静态复核）

1. **上传的入口没变**：仍然只有 `any == true` 才走到。变的只是「走到之后先验一下上传目标在不在」。
2. **判据覆盖了 `m_mesh` 为坏的全部两种成因**：
   * **`Awake` 没跑过** ⇒ TMP 身上**连 `MeshFilter` 组件都没有**（它是 `Awake()` 里
     `gameObject.AddComponent<MeshFilter>()` 挂的，`TextMeshPro.cs:577-579`）⇒ `GetComponent<MeshFilter>()` 判 null ⇒ 跳过 ✓
   * **`m_mesh` 被销毁**（`OnDestroy:692` / `Reset:725` 的 `DestroyImmediate`）⇒ `sharedMesh` 持有的是已销毁对象、
     Unity 的 `==` 重载把它判成 null ⇒ 跳过 ✓
   * **`OnDisable` 把 `sharedMesh = null`**（`:682`）⇒ 跳过 ✓（那种状态本来就没在渲染）
   * ⚠️ 刻意**不用** `tmp.meshFilter`（那个属性会**顺手 AddComponent**）—— 判据不该有副作用。
3. **判据通过时，上传目标就是同一个对象**：`m_meshFilter.sharedMesh` 只在两处被写成 `m_mesh`
   （`Awake:589` / `OnEnable:659`）⇒ `sharedMesh != null` ⇒ `m_mesh` 非空且活着 ⇒ `:430` 那句不可能再 NRE。
4. **同一条语句里另一个解引用也安全**：`m_textInfo.meshInfo` 的写点已穷举（§1.2，三处都是赋新数组）⇒ 不为 null；
   `materialCount ≤ meshInfo.Length`（`SetArraySizes` 里紧随的 `Resize`，`TextMeshPro.cs:1918-1920`）；
   `i ≥ 1` 的历史支路 `m_subTextObjects[i].mesh` 也安全 —— 那些子件由 `SetArraySizes:1936-1938` 建，
   而 `DestroySubMeshObjects()` 全库**零调用**（`grep` 只有三处定义、没有调用点）⇒ 数组不会被人清空。
   （⚠️ 本条只对「本包里 `materialCount` 与 `meshInfo`/`m_subTextObjects` 同代」成立；将来真用上多材质文本要复核。）
5. **不静默**：计数器 + 前 3 次一条警告，正文写清「这一刻那段文字根本没在渲染」。
6. **那一刀不会丢**：`ClipText`（`MenuDraw.cs:731-733`）在 `ClipTextNow` 之后**无条件** `ArmTextGuard(lb, …)`
   —— 守卫已经挂上；等标签**真被显示出来**（父链激活 ⇒ `Awake`/`OnEnable` ⇒ `SetAllDirty` ⇒ TMP 重排发
   `ON_TEXT_CHANGED`）时，`ClippedTextGuard.Reclip()` 会照常补这一刀（`Shell/MenuDraw.cs:886-890`）。
   ⚠️ **这一条在真 Play 里成立**（有帧循环）；**批处理自检里没有帧循环**，那一档不会被验到 —— 如实记，不假装。
7. **改坏法（断言怎么分辨）**：把这道判据删掉 ⇒ `RewardsScene.Run` 立刻回到今天的 NRE（连合计都打不出来）；
   把它改成 `if (tmp.mesh == null)` ⇒ 雷被捂住（属性会 new 一个网格）⇒ **不会红**，所以判据一旦写成那个样子就失去鉴别力
   —— 报告里点明这一点，免得下一个人「化简」成属性版。

**修完之后，什么情况下这条**（`UpdateVertexData`）**仍会走到**：TMP 已 `Awake` 且那份网格活着 ——
**那就是正常情形**（盘面上的每个标签都是这么渲染的），`UpdateVertexData` 本来就是为它写的；
`any == true` 时上传的是同一代 `meshInfo` 数组，与 TMP 自己的 Phase III 走的是同一条路。

---

## 四、本件用到的工具（`TmpFont`/`MenuDraw` 之外，一次性、不在工程里）

为了把「到底是哪个解引用」钉死，写了个 ~120 行的 .NET 探针（`C:\Users\qjh36\AppData\Local\Temp\ilp3\`，
用 `System.Reflection.Metadata` 读 IL、`System.Reflection.Emit.OpCodes` 拿操作数长度表），两种模式：

* `il <dll> <type> <method> <参数个数> <lo> <hi>` —— 逐条打 IL（字段/方法 token 已解析成名字）；
* `field <dll> <字段名>` —— 扫全库列出该字段的 `Stfld/Ldfld/Ldflda` 站点（「谁能把它写成 null」就靠它）。

⚠️ 两条踩过的坑（下次直接用）：① .NET 8 的 `OpCode.Size` **不含操作数**，要按 `OperandType` 算长度；
② `msbuild` 的 obj 目录要干净，否则会报「特性重复」。

---

## 五、A225 是不是诱因？（简报点名要核的那一条）

**不是。** 三条理由：

1. **`any` 在本案走的是 `moved` 那一支**：`ClipQuad` 把被夹的角回传 `moved = true` ⇒
   `ClipTmpMesh` 的 `touched = true` ⇒ `any = true`。**旧口径**（`moved || cut > 0`）在这一幕同样为 true
   ⇒ 旧代码**一样会走到**那句 `tmp.UpdateVertexData(…)` ⇒ **一样会崩**（`rewards.log` 这一跑之前从没崩过，
   只是因为**从来没有用例造成过夹切**，见 §1.6）。
2. A225 改的是「**没被切、但落在渐隐带里**的字」那一半（`cc.a * al[k]` → 绝对值 `BaseCornerAlpha × al[k]`）——
   影响的是**写入值**，不是「走不走得到上传」。
3. 反过来说，A225 那两处（绝对值写入 + `RefreshBounds` 尾巴的 `Reclip`）在本案里是**必要的**：
   不幂等的话，`Reclip` 每次重排重裁会把软边带越乘越暗（静默）。**别把这两处回退**。

---

## 六、没查清的部分（⛔ 不猜）

1. **`GetComponentInChildren<T>()` 在「父链未激活」时到底跳过不跳过** —— 我是**从本跑日志反推**的
   （`[MenuDraw]` 零命中 ⇒ 每次都取到 TMP），**没有**去核 Unity 文档/源码。
   ⚠️ 若它其实会跳过，那 §1.5 那句要换个说法；**但 `m_mesh == null` 这个结论与修法都不受影响**
   （取到 TMP 这件事本身是崩在那句上的前提）。
2. **`GetTextInfo` 那条链的其余环节没逐行核**：我只证明了「它没有 `m_isAwake` 闸 + 跳 Phase III」，
   **没有**证明 `SetText(text)`（它里面第一句）在 `m_TextProcessingArray == null`（`Awake` 没跑）时
   自己会把它分配出来。**实证口径**：主对话跑一次 `RewardsScene.Run`，看新计数器
   **`MenuDraw.TextClipUploadSkipped` 是否 > 0** —— 是 ⇒ 本诊断在运行时坐实（同时说明那条路真的在跑）；
   是 0 且仍然崩 ⇒ 我漏了别的成因，请把 log 发回来。
3. **`m_mesh` 的「已销毁」那一档在本跑里没法区分**（也兜住了）——本件的判据对两档一视同仁。
4. **没跑 Unity**（简报禁止）⇒ §三 全是静态论证 + 出处的组合，**没有**运行时证据。

---

## 七、顺手发现的（⛔ 本件一个都没改）

1. **`Core/TmpFont.SetWrapWidth`（`:208-214`）在未激活的对象上照样做一整趟字形生成** ——
   它要的是 `ComputeMarginSize()`（`OnEnable`/`GetTextInfo`/`OnValidate` 三处才跑），
   代价是「字模生成了、网格却没有」这个状态（本件的根因状态）。
   判据上它没错，但**加一道 `t.isActiveAndEnabled` 之类的闸**能让这类状态根本不存在。
   ⛔ 不在本件白名单（`Core/*`），**只记账**。
2. **`Editor/RewardsScene.cs` 里「锻造页」那一大片断言（`:700-1934`）全都是在【未激活的页】上量的**
   ——页签要等到 `:2077` 才切过去。量矩形/命中区不受影响（今天全绿），但**任何人想在那一段里验
   「文字被软边削 alpha / 被裁切」都会拿到「没生效」**（`ForceMeshUpdate` 在未激活对象上不出网格 ——
   `资料/规则引擎_进度与交接.md:347` 那条坑）。建议主对话记一笔。
3. **`ClipTmpMesh` 里 `i ≥ 1`（多材质子件）那条支路全工程没被走到过**：今天的文本都不用 `<sprite>`+别种字体
   混排。将来卡面图标那类文本若也接进 `ClipText`，要复核 `m_subTextObjects[i]`（本件只从「有没有人清空它」这一侧
   查证过，见 §三·4）。
4. **`MenuScroll.SetOffset` 会在页签**关着**的时候照样触发整页重建**（`ForgeTab.BuildRewardCells`）——
   本件的现场就是这么来的。原版那边页签关着时有没有这条链，本件**没查**（⛔ 出白名单）。
