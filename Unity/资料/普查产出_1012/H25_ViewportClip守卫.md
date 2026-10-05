# H25 —— A435①【取状态那一路】补全（A198② 阶段 2 的第一块）

> 写手：H25（2026-10-12）· 判据 = `资料/普查产出_1012/V8_判据补查.md` §A198②（原版语义）
> + `资料/普查产出_1012/H10_ViewportClip阶段1.md`（§二 `Resolve` 现读 / §四 行为表 · 改名法 / §五 待接线清单）
> 白名单：**改** `Shell/MenuDraw.cs` · `Shell/MenuWindowBase.cs`（只有那 9 处守卫里的 2 处）· `Shell/ViewportClip.cs`
> ⛔ 没碰：`Shell/{WindowsManager,PopUpGameWindow,CampaignRewardWindow}.cs` · `Editor/*` · `Battle/*` ·
> `Core|Deck|RuleEngine|Net/**` · **52 个设站点**（一个都没动）· 两张正本 · git
> 📌 行号一律是**本件改完那一刻**的坐标（H10 §一·5 那条「行号会漂」照旧成立）。

---

## 一、结论

1. **「取状态那一路」补全了，共 4 件**（每件都是「有节点时也必须吃裁切」的那半边）：
   - **2 处守卫**（`MenuWindowBase.Text` / `TextBox`）改成吃 `ViewportClip.Resolve` 的**解析结果**；
   - **`Visible` / `ClipRect` 那两个纯矩形函数**各配一个**节点态重载**（`VisibleAbove` / `ClipRectAbove`）——
     它们手上只有矩形、**没有 `Transform`**，所以「解析」这一步只能落在调用方，重载就是那句「先解析」；
   - **`ClippedTextGuard` 不再存「解析后的快照」**，改存**取状态的那两个实参**，每次重裁**重新解析**；
   - 顺手把 `ClipText` 的注释里那句**已经过期的**「本壳到不了节点态」改掉（铁律 5）。
2. 🔴 **行为逐位不变**（**结构性**保证，不是「应该没事」）：全仓**没有任何节点**
   （`grep -rn "AddComponent<ViewportClip>"` 只命中 `ViewportClip.Hang`，本件也没加）
   ⇒ 每一处解析都落在 `Resolve` 的第 1 支（形参非空 ⇒ 原样返回、**连父链都不走**）或第 3 支（都没有 ⇒ 返回形参本身），
   而这两支对参数都是**恒等式**。逐条验算见 §四。
3. **只改「取状态」，没改「谁设状态」**：52 个设站点、`GameWindow.Clip/ClipSoftness/ClipPad` 三个字段、
   `MenuDraw.{Rect,Nine,Tiled,Hit,DeckCell}` 的**实现**（`_st` 那几段是 H10 阶段 1 做的）**一个字节没动**。
4. 🆕 **加了一个「迁移漏删探测器」**：`ViewportClip.NodeShadowedByParam`（§二·C），它记
   「父链上明明有节点、却被非空的显式形参盖住」的次数 —— 迁移期那正是**唯一**会留下痕迹的状态（其余表现全静默）。
5. ⛔ **7 处同形守卫【没动】**（`WindowsManager.Text:356` · `CollectionWindow:1845` · `ChatPanel:618` ·
   `PlayerProfileWindow:645` · `ItemDrawer:1007,1243` · `SettingsWindow:1740` · `AllianceMemberTab:801` ·
   `PracticeModePopup:1740`）—— **理由与判据见 §六·1**（它们吃的不是 `GameWindow` 那三兄弟，收编要先定口径，是调度台的事）。

---

## 二、改动清单（文件:行号 = 改后坐标 · 每处一句为什么）

### A. `Shell/MenuWindowBase.cs`（**只有那 2 处守卫**）

| 行号 | 改什么 | 为什么（说清**改坏法**见 §三） |
|---|---|---|
| `:307` `Text` | `if (!Visible(r, RenderClip))` → `var _st = Resolve(parent, RenderClip, ClipSoftness, zero); if (!Visible(r, _st.RenderClip))` | 本窗没设 `Clip` 时 `RenderClip` 是 `null` ⇒ `Visible` 判「一律可见」，**父链上的节点对这个判断完全不起作用**。放进 `Resolve` 之后，判可见用的是**这一处真正会用的那个框** |
| `:317` `Text` | `if (RenderClip.HasValue) ClipText(lb, RenderClip, ClipSoftness);` → `if (_st.RenderClip.HasValue) ClipText(lb, RenderClip, ClipSoftness);` | `RenderClip == null` ⇒ **根本不调 `ClipText`** ⇒ 节点态永远到不了文字（H10 §五·1 记的就是这一格）。改判 `_st` 之后调用**发生了**；而形参仍传**原样那一份**（`RenderClip`/`ClipSoftness`）⇒ 让 `ClipText` 与守卫**自己**跟着父链解析（见 §二·C，⛔ 别改成传 `_st` —— 那样守卫就退回「快照」） |
| `:338` `TextBox` | 同上（`Visible` 那处） | 同 `Text`：`TextBox` 是**另一条**建字的路（每日任务行那一族），漏了它 = 那一条永远不吃节点 |
| `:343` `TextBox` | 同上（`ClipText` 那处） | 同上 |

**两处都遵守的一条硬约束**（代码里也写了）：`Visible` 与 `ClipText` **必须用同一份 `_st`** ——
否则会出现「按节点判了可见、却按本窗字段（= 不裁）建出来」这种**半拉子状态**（静默，且只在挂节点时现形）。

### B. `Shell/MenuDraw.cs`

| 行号 | 改什么 | 为什么 |
|---|---|---|
| `:233` `ClipRectAbove(parent, r, clip, out outRect)` | **新增重载**：`ClipRect(r, Resolve(parent, clip, default, default).RenderClip, out outRect)` | `ClipRect` 是**纯矩形函数**（H10 §五·3）：它手上没有 `Transform` ⇒ **解析不了节点**。于是「手上只有矩形」的调用点（典型 = `MenuScroll.Intersects(onScreen)` 那 19 处构建循环）**只能内联一遍求交**，而内联那一份**天然吃不到节点**。本重载把「先解析、再求交」压成一句，**求交仍然只有 `ClipRect` 一份实现**（本重载一句比较都没写） |
| `:295` `VisibleAbove(parent, r, clip)` | **新增重载**：`Visible(r, Resolve(...).RenderClip)` | 同上（`Visible` 版）。两者都是**给阶段 2 的一句话接口**；`clip` 形参**照旧要传**（非空 = 显式覆盖 ⇒ 与旧路逐位相同），⛔ 别当「空剪切版」用 |
| `:977` `ClipText` | `ArmTextGuard(lb, c, softPx)` → `ArmTextGuard(lb, clip, softPx)` | 交出去的从「**解析后的框**」换成「**调用方原样那一份**」—— 守卫要靠它**重新解析**（理由 → `ClippedTextGuard` 类注释）。同时把那句「本壳今天到不了节点态 / 阶段 2 的活，本阶段不动那些调用点」**就地订正**（本件正是那个「阶段 2」） |
| `:1048` `ArmTextGuard` | 形参 `PxRect clip` → `PxRect? clip` | 契约变成「**可空**」，并在文档注释里写死：「⛔ 别改成解析后的框」 |
| `:2378-2382` `ClippedTextGuard` 字段 | `PxRect _clip` → `PxRect? _clipArg`（+ `_softArg` 文档） | 存法换了（快照 → 实参），见 §二·C |
| `:2388` `Arm(Label, PxRect?, Vector2)` | 签名放宽 + 注释 | 同上 |
| `:2402` `CurClip(out PxRect, out Vector2)` | **新增私有函数**：`Resolve(_lb.transform, _clipArg, _softArg, default)` ⇒ `false` = **这一刻没有裁切** | 重裁时**当场**解析。返回 `false` 而不是「给个空矩形」：`ClipTextNow` 的 `PxRect` 是**非空**的 ⇒ 顶上就等于**把字全裁没**（静默） |
| `:2421` `Reclip()` | 先 `CurClip`，取不到就 **return**（不再拿快照裁） | 见 §二·C |
| `:2436` `OnTextChanged()` | 同上 | 事件那条路与「定完版面」那条路**必须同口径**，否则同一代里两刀裁在两个框上（静默） |

### C. 🔴 `ClippedTextGuard`：为什么必须从「快照」改成「重解析」（这是本件的第 3 件活）

`Arm` 原来存的是 `ClipText` **解析之后**的那一份 `PxRect`。这在阶段 1 看着没问题（没有节点），
但阶段 2 一挂节点就**同时**错两处、而且**都不出声**：

| 情形 | 旧写法（快照） | 现在 |
|---|---|---|
| 节点**后挂**（或从别的窗迁过来） | 存的是「没有裁切」⇒ 之后每次重排都重裁一刀**空框**（= `Reclip` 拿着旧框）⇒ **永远不裁** | `_clipArg == null` ⇒ 每次重裁重新解析 ⇒ **跟上** |
| 节点**挪了 / 改了尺寸** | 存的是**旧框** ⇒ 字被切在**错误的位置**上 | 同上 ⇒ 跟上 |
| 显式形参（旧路） | 存的就是形参 | ⛔ **逐位不变**：`Resolve` 第 1 支「形参非空 ⇒ 原样返回、连父链都不走」 |

⇒ 结论：**存实参、不存结果**。`Resolve` 是纯函数（不返 `null`），代价 = 每次重裁多走一遍父链
（旧路那一档 `clip` 非空 ⇒ 第 1 支早退，但**仍会走一次 `FindAbove`** 来数漏删 —— 见 §二·D）。

### D. 🆕 `Shell/ViewportClip.cs`

| 行号 | 改什么 | 为什么 |
|---|---|---|
| `:229-237` `Resolve` 第 1 支 | 加一句 `if (FindAbove(parent) != null) NodeShadowedByParam++;` | **迁移漏删探测器**：形参赢是**设计行为**（不出声、不断言红），于是「旧设站点没删干净」时画面表现**恰好是**「节点挂在那儿、一个像素没生效」而**一切正常** ⇒ 这个计数是那种状态**唯一**的痕迹。⚠️ 同时把 `FindAbove` 的代价带进第 1 支（原来第一句就返回）：每级一次 `GetComponent`，相对建几何是噪声级 |
| `:274` `NodeShadowedByParam` | 新增计数 + 文档 | 断法：**迁移完成（52 处全改完）后断 0**；⚠️ **迁移进行中非 0 是正常的**（它是**进度指标**、不是缺陷计数，⛔ 别拿去卡每一批） |
| `:267` `NodeResolutions` 文档 | 补一句「谁调它多了一处」 | `ClippedTextGuard.CurClip` 现在**每次重裁都解析** ⇒ 一个被裁过的标签每重排一次就 +1（节点态下）。✅ **今天仍是 0**（无节点 ⇒ 恒落第 1/3 支）⇒ H10 §4.1 那条 `== 0` 断言**照旧成立** |
| `:54-66` 文件头 | 加「A435①：取状态那一路已补全」+ 两条可观测不变量 | 阶段 1 那段只写了 `NodeResolutions == 0`；现在多了 `NodeShadowedByParam == 0` |

---

## 三、逐条**改坏法**（⛔ 每条都要能让 §五 某一条断言变红；不许只断「建起来了」）

| 改坏哪一处 | 现象 | 哪条红 |
|---|---|---|
| `MenuWindowBase.Text:317` 把 `_st` 改回 `RenderClip` | 本窗没设 `Clip` 而父链有节点时 ⇒ **文字整块不裁**（从视口里画出去） | §五·B2 |
| `MenuWindowBase.Text:307` 把 `_st.RenderClip` 改回 `RenderClip` | 同一幕：**判可见用的框**与**真正裁的框**分家 ⇒ 整块落在节点外的标签**照样被建出来**（建了但 `ClipText` 会把它切到 0 宽，**看不见却占着**） | §五·B2 的「标签在不在」那一条 |
| `TextBox:338/343` 同上（只改 `Text` 不改 `TextBox`） | 每日任务那一族**整条路**不吃节点（同一扇窗里一半文字吃、一半不吃） | §五·B2 的 `TextBox` 版（⛔ 两处要**各一条**，别只断 `Text`） |
| `MenuDraw.ClipText:981` 把 `ArmTextGuard(lb, clip, …)` 改回 `ArmTextGuard(lb, c, …)`（= 存解析结果） | 节点**挪走**（或后挂）之后重裁，字仍按**旧框**裁 ⇒ 该露的没露 / 该切的没切（幂等被破坏，`TextReclipAfterPlace` 依然 +1，**看不出错**） | §五·B3 |
| `ClippedTextGuard.CurClip` 改回「用 `_clipArg.HasValue` 直接拿 `_clipArg.Value`」 | 节点态那一路直接**抛异常或裁成 0** —— 那正是它**不该**退回 `ClipTextNow` 的写法的原因 | §五·B3 |
| `CurClip` 取不到框时改成「回一个空矩形」 | 那一刻所有被守卫重裁的字**被裁到没有**（静默） | §五·B3 的「没有裁切时字仍在」那一条 |
| `VisibleAbove` 改成直接 `Visible(r, clip)`（不做 `Resolve`） | 与**没加过这个重载**等价 ⇒ 节点态到不了那一类调用点 | §五·B4 |
| `ClipRectAbove` 里 `Resolve` 的 `clip` 形参换成 `null` | **显式覆盖被丢掉** ⇒ 迁移期「旧路还在设」的那些站点会被父链上的节点**抢先裁**（正是文件头那段先后顺序要防的事） | §五·B4 的「形参赢」那一条 |
| 删掉 `NodeShadowedByParam++` | 漏删旧设站点时**一点痕迹都没有**（全静默） | §五·B5 |

---

## 四、行为不变性**怎么证的**（今天全仓 0 个节点 ⇒ 逐位等价于旧路）

**判据面**（结构性，不是实测）：全仓**没有任何节点挂 `ViewportClip`**（`grep -rn "AddComponent<ViewportClip>"` 只命中
`ViewportClip.Hang` 里的那一句），而 `Resolve` 的三支在**无节点**时只有两种落法：
`clip != null` ⇒ 第 1 支（原样返回 `(clip, softPx, pad)`）；`clip == null` ⇒ 第 3 支（`(null, softPx, pad)`）。
⇒ 每个调用点拿到的 `RenderClip = PaddedClip(clip, Vector4.zero)` 与 `Softness = softPx` **逐位等于**原来的形参。

逐处验算（`RenderClip` 那一份的恒等式 = `PaddedClip(clip, zero)` 首句 `pad == Vector4.zero ⇒ return clip`）：

| # | 站点 | 旧 | 新 | 等价性 |
|---|---|---|---|---|
| 1 | `Text:307` `Visible` | `Visible(r, RenderClip)` | `Visible(r, _st.RenderClip)`（`RenderClip` 非空时 `_st.RenderClip ≡ RenderClip`；为空时两边都是「无裁切 ⇒ `true`」，除非父链有节点 —— **那正是要补的那一格**） | ✅ 无节点时逐位相同 |
| 2 | `Text:317` `ClipText` | `RenderClip.HasValue` 判 + 形参 `RenderClip` | `_st.RenderClip.HasValue` 判 + 形参**仍是** `RenderClip` | ✅ 无节点时判据与实参都相同 |
| 3 | `TextBox:338/343` | 同 #1/#2 | 同 #1/#2 | ✅ |
| 4 | `ClipText:981` 的 `ArmTextGuard` | `Arm(lb, c, softPx)`（`c` = `PaddedClip(clip, zero)`） | `Arm(lb, clip, softPx)` ⇒ 重裁时 `Resolve` 出来的 `RenderClip` **还是** `PaddedClip(clip, zero)` = 同一个 `c` | ✅ 旧路逐位相同 |
| 5 | `ClippedTextGuard.CurClip` | 无此函数（用快照） | `Resolve(_lb.transform, _clipArg, _softArg, zero)` | ✅ 无节点 + `_clipArg` 非空 ⇒ 同 #4 |
| 6 | `VisibleAbove` / `ClipRectAbove` | **没有这两个重载**（新接口） | `Resolve` + 原函数 | ✅ 零调用点（见 §六·2）⇒ 不可能改行为 |
| 7 | `NodeShadowedByParam++` | 无 | 只在 `clip != null` 时多走一次 `FindAbove` | ✅ 无节点 ⇒ 永不自增（唯一副作用是那次查找） |

**两处【已知的】非逐位差异**（都**不是**行为差异，如实列出来）：

1. **`NodeResolutions` / `FindAbove` 的调用次数**：形参非空那一支现在**也会走一次父链**
   （为了数 `NodeShadowedByParam`）。这是**计数器与开销**，不是画面行为；无节点 ⇒ `NodeShadowedByParam` 恒 0。
2. **`Reclip` 在「这一刻没有裁切」时不再调 `ClipTextNow`**（旧代码会调，且它会返回 `false`）。
   ⇒ `TextClipUnavailable` 那个**告警计数**在这一档少加一次、`TextReclipAfterPlace` 也少一次「没裁成」的记账。
   ⚠️ **只有在「守卫已挂，但之后父链与形参都变成空」时才可能发生**（旧路那一档形参非空 ⇒ 不可能；
   唯一现实入口是「节点先挂、后被拿掉」）—— 今天无节点 ⇒ **一次都不会走到**。如实记在这里，别当没看见。
3. （顺带）`CurClip` 交给 `ClipTextNow` 的软边**没有**先夹非负 —— 旧代码存的是夹过的值。
   但 `ClipTextNow` 首句就是 `softPx.x < 0 ⇒ 0f`（唯一的取用点），⇒ **不可观测**。

**编译**：`TMPDIR=/tmp/wf_h25 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
（改完每一批都跑过；最后一次在 §二·D 之后）。

---

## 五、待接线（断言 —— ⛔ 本件**没碰** `Editor/*`，夹具归调度台）

### 5.1（**最便宜、最该先加的那一条**）取状态恒不走节点
```csharp
CheckTrue(ViewportClip.NodeResolutions == 0 && ViewportClip.NodeShadowedByParam == 0,
    "A435①：全仓不挂节点 ⇒ 取裁切状态【恒】不依赖父链（走了一次就说明有人偷偷挂了节点）");
```
放在**任意既有** `*Scene.Run` 末尾即可（每跑一次 `-executeMethod` 是**新进程**，静态计数从 0 起）。
⚠️ 它与 5.2 **必须分在两个场景里**（5.2 自己会挂节点 ⇒ 会把两个计数都顶起来）。

### 5.2 **文字吃节点态**（`MenuWindowBase.Text` / `TextBox` 那两处守卫）
```csharp
// ① 视口节点 V=(100,100)-(500,500)、pad 0、soft 0（软边另开一条，见 5.2b）
var vp = ViewportClip.Hang(root, "Viewport", new PxRect(100,100,500,500), Vector4.zero, Vector2Int.zero);
// ② 一段**越界**的字：矩形 (0,0)-(1000,60) 的垂直中心落在 V 里 ⇒ 节点态下应被切到 x∈[100,500]
var lb = win.Text(vp.transform, "WIDE TEXT …", 0, 1000, 300, 360, 5, Color.white, "T");
CheckTrue(lb != null, "节点态下这段字【建出来了】（两侧都压着节点边界 ⇒ 必须有交集）");
// ③ 🔴 牙口：直接读 TMP 的顶点（⛔ 别拿 `TextReclipAfterPlace` 自证 —— 那是被调方的计数器）
var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>();
/* 逐顶点播 tmp.textInfo.meshInfo[..].vertices ⇒ 全部 x ∈ [V.x1−ε, V.x2+ε]（转成各自的那套单位）*/
CheckTrue(minX >= 节点左沿−ε && maxX <= 节点右沿+ε, "节点态的框真的作用在文字网格上（不是整段画出去）");
// ④ 反面（= §三 第 1 条改坏法）：把 ② 的 parent 换成 root（节点不在父链上）⇒ 同一段字**不许**被切
CheckTrue(ViewportClip.NodeResolutions > 0, "这条路**带电**（只断「建出来了」改坏实现不会红）");
```
`TextBox` 那条同形（换 `win.TextBox(vp.transform, r, …)`）—— ⛔ **两处各一条**，只断 `Text` 会漏掉整整一族。

**5.2b（软边那一条）**：给节点 `soft = (0,25)`、把字压在 y 边界上 ⇒ 断言「贴边那两行顶点的
`colors32.a` 被削、框内不受影响」（判据 = `MenuDraw.ClipTmpMesh` 的软边那一半，⛔ 别只断几何）。

### 5.3 **守卫跟着节点走**（本件的第 3 件活 —— ⛔ 这条**不能**拿 `MenuDraw.TextReclipAfterPlace` 单独自证）
```csharp
// 沿用 5.2 的场景：② 建完字之后，把节点**挪到别处**（`MenuDraw.Node` / `ApplyPxRect` 重建，或直接改 sizeDelta）
// ③ 调 lb.RefreshBounds()（= 「定完版面」那条路的末句 ⇒ 会调 ClippedTextGuard.Reclip）
// ④ 🔴 牙口 = 【顶点的最大 x】必须跟着**新框**变
CheckTrue(顶点 maxX ≈ 新框右沿, "重裁用的是【当下】解析出来的框，不是 Arm 那一刻的快照");
```
- ⚠️ **必须比顶点**：比 `lb.WorldW` 不行（`RefreshBounds` 只重算尺寸，**裁切不动 `WorldW`**）；
  比计数器也不行（旧写法一样 +1 —— **这正是这条断言存在的全部理由**）。
- ⛔ **批处理里没有重排事件**（没有帧循环）：`RefreshBounds()` 那条路能验（它是显式调用），
  `OnTextChanged` 那条**只能真 Play**（H10 §四 那条纪律照旧）。

### 5.4 **`VisibleAbove` / `ClipRectAbove`**（新接口，今天零调用点）
```csharp
// 在 5.2 的节点下：一个**整块落在节点外**的矩形 ⇒ VisibleAbove(...) == false；同一矩形给 root ⇒ == true
// 再加一条「显式覆盖赢」：VisibleAbove(vp.transform, r, new PxRect(0,0,8,8)) 用的是那个 8×8（不看节点）
```
（形状照 H10 §4.2 那四步的 ⑤；这两条是**接口**的验收，不是生产行为的验收。）

### 5.5 迁移期自查（52 个设站点那件活的牙口，**本件不做**）
```csharp
CheckTrue(ViewportClip.NodeShadowedByParam == 0, "迁完之后不该再有「节点被旧设站点盖住」的地方");
```

---

## 六、没查清 / 明确不知道的

1. 🔴 **其余 7 处同形守卫为什么没动 —— 以及它们各自到底该不该动**：那 7 处（+ `PracticeModePopup`）
   **有两种不同的来源**，不能一刀切：
   · **页/视图自己的 clip 字段**：`CollectionWindow.Clip`（继承自 `MainMenuSubmenuWindow` ⇒ **就是 `GameWindow` 那一份**）、
     `PlayerProfileWindow.Clip:508`（`ProfilePage` 里声明的那一份）、
     `ItemDrawerOptions.Clip:173`（`ItemDrawerOptions` 上的）、`AllianceMemberTab._trophyClip`；
   · **形参**：`ChatPanel`（A78① 的裁定 = 本窗**逐件传**、不靠字段）、`SettingsWindow:1740`、
     `PracticeModePopup:1740`、`DeckRuntime:3400`。
   ⚠️ 其中 `PlayerProfileWindow` / `ItemDrawer` / `AllianceMemberTab` 那几份**还没查**「它们与
   `GameWindow.Clip` 是不是同一份状态」（H10 §七·1 记的那 4 份平行载体就是这件事），
   而「页自己的 clip 与节点谁优先」是**口径**、得调度台定 —— ⛔ 我不发明。⇒ 一并留给阶段 2 的下一块。
   ⚠️ 另一条**已经查清**的差异：`ClipText` **内部**已经从标签自己解析父链（H10 阶段 1 做的）⇒
   那 7 处里凡是**形参为 `null`** 的（`ChatPanel` / `SettingsWindow` / `PracticeModePopup` / `DeckRuntime`）
   **其实已经部分吃节点**（调用方传 `null` 时由 `ClipText` 现解析）；它们的守卫只在
   **实参非空**那一档拦，拦的**不是**节点态 ⇒ **今天判它们「不吃节点」是错的**。
   而 `CollectionWindow:1845` / `PlayerProfileWindow:645` / `ItemDrawer:1007,1243` / `AllianceMemberTab:801`
   那 5 处**确实**被自己的 `Clip` 拦死（`GameWindow` 那份今天恒非空 ⇒ 节点永远不生效）。
2. **`VisibleAbove` / `ClipRectAbove` 零调用点** ⇒ 它们只有注释里的「待接线」，**没有生产路径的实测**
   （也就**不可能**改今天的行为，见 §四 #6）。
3. **`NodeShadowedByParam` 的真实代价没实测**（没跑 Unity）：每级一次 `GetComponent`，
   按 `FindAbove` 那条注释估算是噪声级；⚠️ 若真量出可感开销，改法**不是**加缓存（会静默失效），
   而是在迁移完成后把那一句整个删掉（它本来就是迁移期的脚手架）。
4. **`CurClip` 与 `Arm` 之间节点被移走的竞态**：两者都在主线程同步路径上（建 → 定版面），
   理论上不存在并发；**没查**有没有人从别处异步改节点（今天没有节点，无从查起）。
5. 🔴 **`Reclip` 在「这一刻没有裁切」时不再调 `ClipTextNow`** ⇒ 两个计数器在那**一整档**少记一次
   （§四 那第二条）。今天走不到（无节点 + 旧路恒非空），但**阶段 2 挂节点之后**要重看。

---

## 七、顺手发现（⚠️ **只报不改**）

1. 🔴 **`ChatPanel` / `SettingsWindow` / `PracticeModePopup` / `DeckRuntime` 那几处「不吃节点」是【误判】** ——
   它们的 `clip` 形参可以是 `null`，而 `ClipText` 自己会解析父链 ⇒ **节点态早就部分可达**了。
   记在这里免得下一块照一份错的清单去改（判据 = `MenuDraw.ClipText:979` 那两句 + 这四处的调用实参）。
2. **`MenuScroll.Intersects(onScreen)` 那 19 处构建循环**（H10 §五·3 记的）**今天仍然到不了节点态** ——
   本件给了 `VisibleAbove` 这个接口，但**没改调用它**（`MenuScroll` 不在白名单）⇒ 阶段 2 的下一块照 §二·B 接。
   ⚠️ 提醒：`MenuScroll.Intersects` 的语义是「**只判滚动轴**」（`MenuScroll.cs:359` 转调 `MenuDraw.Visible` 之前
   它自己那句注释写着这一点），改成两轴会把**横轴框外的件**也一起不建 —— 那是可见行为变化，别顺手改。
3. **`ViewportClip.cs.meta` 仍然没有**（H10 §五·5 那条照旧）：我没跑 Unity ⇒ 下次批处理导入时才会生成。
   `gen_csc_rsp.py` 会自动扫到新 `.cs` ⇒ 类型检查已经过了（本件又跑了几次，都是 0/0）。
4. `MenuDraw.cs` 里 `ClipTextNow` 的调用契约③（「在已经夹过的网格上再夹一次 = 越裁越错」）
   与本件**不冲突**：`Reclip` / `OnTextChanged` 走的都是 `ClipTextNow`，与建时那一刀**同一个函数**、
   `ClipTmpMesh` 又写的是**绝对值**（A225-② 已改成幂等）⇒ 多裁几刀结果一样。

---

## 八、自检状态（按简报：本轮**不跑** 11 条）
- ✅ **跑过**：`TMPDIR=/tmp/wf_h25 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了 3 次，最后一次在最后一批改动之后）。
- ⛔ 未跑 Unity 批处理；⛔ 未跑 `_run_8_checks.sh`；⛔ 未写任何 `Editor/*` 断言（§五 全部是**待接线**）。
- 📌 **影响面提示**（调度台决定复跑时机）：本件动的是 **`Shell/MenuDraw.cs` + `Shell/MenuWindowBase.cs` 两个共用件**
  ⇒ 按铁律 12 第 2 条要先 `grep` 谁在用 —— `MenuDraw.{Text,TextBox,ClipText,Visible,ClipRect}` 与
  `MenuWindowBase.{Text,TextBox}` 的使用者覆盖**整个 `Shell/`**（`Visible`/`ClipRect` 另有 `Deck/` 与 `Editor/` 的读者）
  ⇒ 真要复跑就按覆盖面走**全套**（不是「只跑一条宿主」）。
  ⚠️ 但**零行为变化**是结构性的（§四）：无节点这一条不变量在**每一条**宿主里都成立。
