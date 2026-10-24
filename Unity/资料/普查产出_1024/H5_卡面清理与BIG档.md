# H5 — 大图档 BIG 状态按 overdraw 两轴门控 + 两处死代码清理

**日期**：2026-10-24 · **身份**：卡面收尾代理（H5）· **调度台**：主对话
**白名单内改了 2 个文件**：`Core/CardView.cs` · `Editor/CardBaseDemo.cs`
**没跑 Unity**（红线）· **没动 git** · **没改正本** · 秒级类型检查**绿**（运行时 0 错 / 编辑器 0 错）

> ⚠️ **先说一件必须让调度台知道的事**：`Editor/CardBaseDemo.cs` **同时有另一个代理（`H6`）在写**
> （我读进来之后，`AssertTacticArtClamp` 被 `H6` 重构成 `AssertTacticArtClamp` + `AssertArtClampForSample`
> 两个方法、并加了防御卡样本；文件在我工作期间又被写过一次）。两笔改动目前**都在盘上、都还能编过**，
> 但这不是「一个文件同一时刻只有一个写手」—— 合并前请让 owner 再核一遍这个文件。详见 §六·1。

---

## 件 1 · 原版战术 / 防御卡会不会进 BIG 状态 —— **判据成立，已按原版对齐**

### 1·1 判据（一手，亲读 `d:/2/tools/decomp_full/`，逐条给了消费点）

| # | 问题 | 结论 | 出处 |
|---|---|---|---|
| ① | 谁调 `ChangeState(Small↔Big)`、什么条件 | `BasicCardUI.SetImageOverDraw(ImageInFrameSize, bool)` → `CardImageController.ChangeSize` → `ChangeState`。**三处调用点条件完全一致**：`useOverDraw == 0`（`RawCardScript + 0x58`）**或** `idx != 0` ⇒ `Small`；只有 **`idx == 0` ∧ `useOverDraw != 0`** ⇒ `Big` | `BasicCardUI.cs:334`（签名桩）· `CardImageController__ChangeSize.c:5` · `CardDisplayWindow__ShowCard.c:62-84` · `CardDisplayWindow__CardOverdrawHandle.c:7-39` · `CardDisplayWindow__ChangeCardPosition.c:74-79` |
| ② | `useOverDraw` 只控制**放大**，还是也控制**进不进 Big** | **控制进不进 Big（= 控制状态本身）**，放大只是状态 1 的一个副作用 | 同上三处 —— 传进去的是**整个状态枚举**（`ImageInFrameSize.Small=0 / Big=1`，`CardImageController.cs:7-11`），不是倍率 |
| ③ | 状态 1 到底改了什么 | ① `_Clamp_Range` 换 **BIG** `(0.75, 0.89, 0.25, 0.1)`（静态字段 `+0x10` = cctor 第二份）；② 立绘 `originalImageScale × bigSizeMultiplier`（1.27）；③ `frameMask` 打开 | `CardImageController__ChangeState.c:84-113`（BIG 在 `:100-104`、倍率 `:90-95`、mask `:105-108`） |
| ④ | Small 那一档用哪个 clamp | **材质序列化默认 `x=0.82`** —— `ChangeState` 第一句 `if (currentState == stateToChange) return;`（`:30-32`），而 Small 是出厂态 ⇒ **不写材质**，用默认值 = 我们 `ClampRangeSmall` 的 `(0.82, 1, 0.18, 0)` | `CardImageController__ChangeState.c:30-32` · `CardView.cs` 的 `ClampRangeSmall` doc |
| ⑤ | 我们两条链与之一不一致 | **不一致（已修）**：`bigState` 原来只有 `_bigImage && BigCardOverdraw`，**不读两条 overdraw 轴** ⇒ 详情窗里 ①战术/防御卡（池内 `useOverDraw` 全 0）②**所有相关卡**（`idx != 0`）都被烘成 **BIG** 硬矩形 | `CardView.cs` 改前 `:3661`；证据链 = `D3` §1·3 · `H1` §五·4 |

⇒ `D3` 的结论**成立且要往前推一步**：`useOverDraw` 不是「只决定要不要 ×1.27」，而是**决定状态**；
`D3` 之所以只在倍率上补门，是因为它只核了 `SetImage` 那一处、没核「谁把卡送进状态 1」。

### 1·2 改动（`Core/CardView.cs`）

1. 🆕 **`bool BigImageState`**（`:10254`）—— **判据只此一处**：
   `_bigImage && BigCardOverdraw && _artOverdraw && _useOverDraw`
   （大图档 × 建卡前总开关 × **前台/idx==0** × **逐卡 `useOverDraw`**）。
   doc 里逐条写了判据 ①②③④ 的原文与行号，以及「为什么 D3 那版只在两轴默认 `true` 时与它一致」。
2. 🆕 **`ArtScaleFor(d, bigState)`**（`:10260`）· **`ArtClampKindFor(d, bigState)`**（`:10265`）·
   **`ArtLayerMeshFor(...)`**（`:10271`）· **`ArtFrontMeshFor(...)`**（`:10278`）—— 把「状态 → 网格」的
   四个算式**各收一处**，`Build` 与状态重烘**共用**（⛔ 不再两处各写一遍）。
3. 🔴 **`Build` 里 `bigState` 改成读 `BigImageState`**（`:3674`）。出生这一趟两轴都是默认 `true`
   ⇒ **与改前逐位相同**（下面 §五·1 有逐条对账）。
4. 🔴🆕 **`RebakeBigStateMeshes()`**（`:10289`）+ **`ApplyBigStateAxes()`**（`:10310`）——
   **为什么非要它**：两条轴是窗口在 **`Create` 之后**才赋值的
   （`Battle/CardDisplayWindow.cs:1393-1394` · `Shell/CardDetailPopup.cs:928-929`；
   `Create` 不收这个形参 —— `CardView.cs:1686-1701`），而档位与 ×1.27 都是**烘进顶点**的
   ⇒ 改属性改不到网格。两个 setter（`ArtOverdraw` `:10152` / `UseOverDraw` `:10190`）现在走
   `ApplyBigStateAxes()`：开关 `_artFront` + **按新状态重烘 `_art` / `_frame`(uv2) / `_artFront` 三块网格**。
   这正是原版 `SetImageOverDraw → ChangeState → DoChange` 那一步（我们不做 `DoChange` 的 `scaleTime` 补间，见 §五·4）。
   网格全走静态缓存（键含倍率/档位/`clipWindow`）⇒ 切回去拿到的就是原来那一块，**幂等**。
5. 新增两个字段 `_artTexBuilt` / `_builtData`（`:1130-1131`）—— 重烘按**建网格时那份**输入算
   （`Create` 里 `Build` 跑在 `SetData` **之前**，那时 `Data` 还是默认值，⛔ 不能读它）。
6. 铁律 5 就地订正（保留痕迹）：
   - `BigArtScaleApplies` 的 doc：原来写「我们这一侧**没有**『`useOverDraw=0` ⇒ 不放大』那一步」
     —— **已不成立**，补了订正段并写明「类型门」与「状态门」是**与**关系、两条都不能删（`:10100-10108`）。
   - `ArtOverdraw` / `UseOverDraw` 两个属性的 doc：原来只写「关掉 `_artFront`」，补上「**还改 `_art` 的硬矩形档位**」（`:10139-10142` / `:10181-10184`）。

### 1·3 可见变化范围（静态推断，⚠️ 没渲图 —— 见 §五·3）

| 卡 / 格 | 状态 | 变化 |
|---|---|---|
| 前台单位卡（`useOverDraw=1` + idx 0） | Big → Big | **零变化**（池内 585+56 张都属于这一类） |
| **相关卡**（idx != 0）· 单位卡 | Big → **Small** | **零可见变化**：`_art` 的 `clampKind` 对单位卡恒 **0**；`_artFront` 本来就藏着；只有 `_frame.uv2` 变（**没有任何 shader 消费它**） |
| **战术 / 防御卡**（无论前台还是相关） | Big → **Small** | 🔴 **有可见变化**：`_art` 的硬矩形从 BIG 换成 SMALL —— x 半宽 **±0.714584 → ±0.914668** 卡单位（0.25→0.32 × 2.7484 × 1.04），v 从 `(0.1, 0.89]` 放宽到 `(0, 1]` ⇒ 详情窗里**立绘多露出一圈**（仍被 `clipWindow` 的卡框开口夹着，实际可见 = SMALL 矩形 ∩ 卡框开口） |

⇒ 「多露出一圈」是**原版的行为**（原版战术卡在详情窗里停在 Small），不是退步。

---

## 件 2 · 死代码 / 死参数清理（**行为逐位不变**）

### 2·1 `ArtMeshInFrame` 的 `frameQuad` 形参 —— **已删**

- **核清**：方法体（`:9252-9402`）从头到尾**没有一处**读 `frameQuad`；板尺寸/中心全走
  `ArtBoard(art, artScale, …)`（`:9287`）。`awk`/`grep` 扫过：签名那一行之外**零命中**（与 `H1` §六·3 一致）。
- **签名变更**：`ArtMeshInFrame(Texture2D art, Vector3 frameQuad, float artScale = 1f, …)`
  → `ArtMeshInFrame(Texture2D art, float artScale = 1f, …)`（`:9252`）。
- **全仓 grep 过调用点**（`Assets/**/*.cs`）：**只有本文件 6 处**，全部改完 ——
  `ArtMeshForTest`（`:9413`）· 两处 `CheckArtClamp…` 的内部参考网格（`:9467` / `:9601`）· 新收的两个
  `ArtLayerMeshFor` / `ArtFrontMeshFor`（`:10271-10284`）。**其它文件零调用**（`ArtMeshInFrame` 是 `private static`）。
- **顺手订正两处过期注释**：`FrameQuad` 的 doc 里「`ArtMeshInFrame` 虽然还收这个参数」（`:10444`）
  已改成「那个死形参已删，本函数消费者只剩 `FrameMesh`」；`ArtMeshInFrame` 自己补了删除理由的 doc。
- ⚠️ **一处我改不到的过期引用**：`Editor/CardFaceProbe.cs:1007` 的注释里还写着
  `ArtMeshInFrame(artTex, FrameQuad(frameTex), 1f, null, false, 1)`（**黑名单文件**）。那只是「改坏法」的散文，
  不影响编译/断言，但字面已过期 —— 请 owner 顺手改。

### 2·2 `artFrontMesh` 的 `: artMesh` 兜底分支 —— **确认死代码，已随重构消失**

- **核清（`H1` §六·4 的记账对）**：改前 `Build` 里
  `var artFrontMesh = bigState ? <大图档网格> : artMesh;`，而 `_artFront` 真正用的 `AddLayer`
  三元在 `bigState == false` 时走的是**它自己的一次 `ArtMeshInFrame(…, 1)`**（带 `clipWindow: !d.isUnit`）
  ⇒ 那个 `: artMesh` **一次都没被读到**。
- **做法**：把「小图档那一支」搬进 `ArtFrontMeshFor(d, artTex, frameTex, false)`
  （参数与原内联调用**逐字相同**：`scale 1.0` · `frameTex` · `clipWindow: !d.isUnit` · `clampKind 1`），
  `AddLayer` 直接用它 ⇒ 死支消失、`artFrontMesh` 变量第一次「名副其实」。
- ⚠️ **顺手核出的一个细节**（`H1` 说「两份逐位相同」只对了一半）：`artMesh` 与那一支在
  **非单位卡**时逐位相同，在**单位卡**时**不同**（`clampKind` 0 vs 1、`clipWindow` false vs true）。
  今天那层在单位卡上恒被藏（`ArtFrontVisible`），所以以前「没人发现」；**这条差异现在被写进注释**（`:3699-3706`）。

---

## 三、断言逐条 + 灭自证

新增 `CardBaseDemo.AssertBigStateOverDrawGate()`（`:941`，调用点 `:302`），**12 条 `Check`**。
样本都是池内真卡、真立绘、真卡框：

| 用途 | 卡 | 说明 |
|---|---|---|
| 战术卡 | `Armoured Squadron`（AM52 · 1024²） | 池内 `useOverDraw = 0` |
| 单位卡 | `Howling Banshee Exarch`（ASH79 · 671×1024） | 池内 `useOverDraw = 1` |

卡框钉 `TacticArtClampProbeTier = 4`（H4 的教训：**只有开口比硬矩形宽时，硬矩形才决定网格**），
`DebugFrameTier` / `BigCardOverdraw` / `DebugDisableArtClamp` 三个全局都在 `try/finally` 里复原。

| # | 断言 | 怎么才会红 |
|---|---|---|
| P1 | 卡池里找得到两张样本 | 卡名改了 / 卡池换表 |
| P2 | 类型对（战术 `isUnit=false` · 单位 `isUnit=true`） | 样本取错 ⇒ 后面全空验 |
| P3 | 战术卡的「BIG / SMALL」两份参考网格**彼此不同** | `ClampRangeSmall == ClampRangeBig` 被人改歪 ⇒ 空验 |
| P4 | 战术卡**刚建出来**时 = BIG 那一份 | 状态门被写成恒 `false` ⇒ 前提没了 |
| ① | `UseOverDraw = false` ⇒ `_art` 网格 = **SMALL** 那一份、且 ≠ BIG | 🧨 状态不看 `useOverDraw`（= D3 那一版）⇒ 仍 BIG ⇒ 红 |
| ①b | **灭自证**：置回 `true` ⇒ 逐顶点**回到** BIG | 🧨 重烘那步删掉（只降不升）/ 一次性降档 ⇒ 红 |
| ② | `ArtOverdraw = false`（= 原版 `idx != 0` 的相关卡）⇒ 同样落 SMALL | 🧨 状态漏掉前台轴 ⇒ 仍 BIG ⇒ 红 |
| ②b | **灭自证**：置回前台 ⇒ 回到 BIG | 同上 |
| P5 | 单位卡「×1.27 + BIG」与「1.0 + SMALL」两份参考**彼此不同** | 倍率/档位被改歪 ⇒ 空验 |
| P6 | 单位卡（`useOverDraw=1` + 前台）**刚建出来**时 `_artFront` = **×1.27** 那一份 | 状态门恒 `false` ⇒ 前提没了 |
| ③ | 单位卡 `UseOverDraw = false` ⇒ `_artFront` 落回 **1.0** 那一份、且 ≠ ×1.27 | 🧨 状态门只挡非单位卡 ⇒ 仍 ×1.27 ⇒ 红 |
| ③b | **灭自证**：置回 ⇒ 回到 ×1.27 | 同上 |

**灭自证（结构性，四条互相咬住）：**

1. **①/①b 方向相反**：① 要「降下去」、①b 要「升回来」⇒ 把状态门改成恒真 / 恒假 / 常量降档，
   **必有一条红**（两边一起改也不行）。
2. **② 用的是另一条轴**（`_artOverdraw`）⇒ 只实现 `useOverDraw` 那一半会红。
3. **③ 换了一层**（单位卡量 `_artFront`、战术卡量 `_art`）+ **换了量纲**（×1.27 vs 1.0）⇒
   「只删一条断言/只改一边」跨不过去。
4. **P3 / P5 是「两份参考彼此不同」的显式前提** ⇒ 谁把两档常量改成相等（这会让 ①②③ 全部变成
   「等于同一份」的空验），**P3/P5 当场红**。
5. **量的是真卡的真网格**（`ArtLayerMesh` / `ArtFrontMeshForTest`），⛔ 不是把 `BigImageState` 读一遍
   —— 档位与倍率都烘在顶点里，静态读常量证明不了渲染用的是哪一档。

---

## 四、预期读数

- **本件给 `CardBaseDemo` 加 12 条**（全绿时 `_pass += 12`）。
  简报给的现读基线是 **336/0** ⇒ 若没有别的批次同动这个文件，期望 **348/0**。
- ⚠️ **但这个文件同时被 `H6` 在改**（它重写了 `AssertTacticArtClamp` + 新增 `AssertArtClampForSample`
  与防御卡样本，条数也变了）⇒ **实际合计 = 合并后宿主自己的数**，⛔ 别拿 348 当判据；
  本件只保证「**+12**」这一份。
- `CardFaceProbe`：**本件不改它的任何输入**（它的 `BuildBigFrontMesh` 不碰两条 overDraw 轴，两轴默认 `true`
  ⇒ 它建出来的卡仍是 BIG）⇒ 期望仍 **125/0**（D3 那 4 条也不受影响）。
- **本件没跑 Unity**（红线）⇒ 上面两个数都是**预计**，不是实测。

---

## 五、没查清 / 没做（如实）

1. **没跑 Unity、没渲图** ⇒ ①所有断言读数、②`CardBaseDemo` 的合计、③§1·3 那张「可见变化」表
   **全是静态推断**。铁律 10 第 6 条要求的「并排渲一次」**没做**。
2. **重构前后逐条对账（静态核过，等价性论证）**：`Build` 的那四处算式与改前**逐参数相同** ——
   `artScale`（`ArtScaleFor(d, bigState)` ≡ 旧三元）· `artMesh`（`ArtLayerMeshFor(…, bigState)` ≡ 旧的
   `clampKind: d.isUnit ? 0 : (bigState ? 2 : 1)` + `clipWindow: !d.isUnit` + `scale 1f`）·
   `artFrontMesh` 大图档支（`ArtScaleFor(d,true)` = 旧 `artScale`，因为该支恒 `bigState=true`）·
   `_artFront` 小图档支（同 §2·2）。⇒ **出生那一趟逐位不变**，变化只来自「轴之后被赋值」。
3. **可见变化的样本没渲**：战术/防御卡在详情窗里「多露一圈」是**判据推的**（SMALL vs BIG 的 UV 窗口），
   没有真机/并排图。⚠️ 这条属于「改完要并排看」的欠账。
4. **原版 `DoChange` 的补间没复刻**：原版换状态走 `scaleTime` 补间（`ChangeState` 的 `instant` 参数，
   `ChangeCardPosition` 传 `instant=0`）；我们是**瞬时**换网格（和现有 `_artFront` 显隐一样是硬切）。
   ⇒ 换位时战术卡的硬矩形会**跳一下**。要不要补间，属于另一笔（不阻塞本条）。
5. **那条「池内 1 张 unit 的 `useOverDraw=0`」没有单独样卡**：它现在会被状态门正确挡掉（走 ③ 同一条代码路），
   但没拿那张真卡跑过断言。
6. **`_artFront` 在相关卡上的隐藏网格也变了**（SMALL 那一份）—— 那一层恒不显示，**没验**；
   若将来 `ArtFrontVisible` 改口径让它露出来，网格已经是「状态 0」那一份（是对的，但没渲染证据）。
7. **没动**：`D3` 的 `BigArtScaleApplies`（类型门）· `A7` 的 `ArtClampUv01` / `DebugDisableArtClamp` ·
   小卡档 / `UseFrontLayer` 那条老口子 · 任何 `CardFaceProbe` / `Battle` / `Shell` 文件。

---

## 六、顺手发现（⚠️ 我没改，只记账）

1. 🔴🔴 **`Editor/CardBaseDemo.cs` 有并发写手（`H6`）** —— 我读进来时 `AssertTacticArtClamp` 是 H1/H4
   那一版（单方法、`goOn/goOff/goUnit` 一个 `try`），我插完方法后再看，它已被重构成
   `AssertTacticArtClamp`（749 行收尾）+ `AssertArtClampForSample`（811）+ 防御卡样本 `Sacred Altar`；
   文件 mtime 在我工作期间**又被刷新过一次**。当前两笔改动**都在盘上、能编过**，
   但这违反「一个文件同一时刻只有一个写手」（铁律 13·3）⇒ **合并前请 owner 再核一遍**，
   并考虑把「同宿主不同方法」的活拆到不同批次。
2. **`CardView.Create` 不收「这一格是不是前台 / 这张卡 useOverDraw」** —— 两条轴只能在建卡**之后**赋值，
   所以「状态变了要重烘」这一步是**结构性必需**的。更干净的长期做法是让 `Create` 收这两个参数、
   `Build` 一次到位（省掉重烘）；但那要改 `Battle/CardDisplayWindow.cs` 与 `Shell/CardDetailPopup.cs`
   （都在本件黑名单）⇒ 只记在这里。
3. **`SetData` 至今不重建立绘几何**（只换材质贴图）。加了重烘之后，`_artTexBuilt` / `_builtData`
   仍取**建卡那一趟**的输入 ⇒ 若将来有「同一个视图换一张立绘不同的卡」并且随后动 overDraw 轴，
   重烘会用旧图的比例算板。今天的两个窗口都是「一张卡一个视图」，走不到 ⇒ 记账即可。
4. **`D3` §1·2 与 `H6` 的新说法打架（未裁）**：`D3` 说池内 39 张 `defence` 的 `RawCardScript.cardType`
   读出来是 **20**、且全表没有一张读到 **5**；而 `H6` 写进 `AssertArtClampForSample` 的 doc 里说
   「防御卡（`cardType = 5`）」。两人都声称有判据 ⇒ **需要调度台裁**（`cards_truth.json` 现读一遍即可）。
   与本条无关（本件判的是 `isUnit` + 两条 overDraw 轴，不读 `cardType`）。
5. **`CardFaceProbe.cs:1007` 的注释已过期**（`ArtMeshInFrame` 的旧签名），黑名单外的手请顺手改（见 §2·1）。
6. **`BigImageState` 与 `ArtFrontVisible` 现在是「同一条轴组合」的两种表述**（后者多一个
   `_artFront != null` 与 `_faceMode != Board` 的层存在性判断）。**故意没有合并**：一个管「状态 / 网格」，
   一个管「层亮不亮」；合并会把「层没建」误当成「状态 0」。已在两处 doc 互相指认。

---

## 七、改动清单（行号为提交前现读，含并行批次的位移）

```
Unity/MyGame/Assets/CardPresentation/Core/CardView.cs        （纯 CRLF；11340 / 11340）
  :1130-1131   新字段 `_artTexBuilt` / `_builtData`（重烘的输入）
  :3668-3707   `Build`：`bigState = BigImageState` · 四处网格改走 `Art*For(...)` · 死支折叠
  :9252        `ArtMeshInFrame` 删掉死形参 `frameQuad`（+ 4 行 doc）
  :9413 / :9467 / :9601   三处调用点跟着改
  :10100-10108 `BigArtScaleApplies` doc 的订正段（铁律 5）
  :10139 / :10181  两个属性 doc 补「还改硬矩形档位」
  :10152 / :10190  两个 setter 改走 `ApplyBigStateAxes()`
  :10202-10317 🆕 判据大段 + `BigImageState` / `ArtScaleFor` / `ArtClampKindFor` /
               `ArtLayerMeshFor` / `ArtFrontMeshFor` / `RebakeBigStateMeshes` / `ApplyBigStateAxes`
  :10444       `FrameQuad` doc 的过期句订正

Unity/MyGame/Assets/CardPresentation/Editor/CardBaseDemo.cs   （纯 CRLF；4206 / 4206）
  :302         调用 `AssertBigStateOverDrawGate();`
  :917-1018    新方法 `AssertBigStateOverDrawGate()`（12 条 `Check`）
```

**行尾**（二进制 `rb` 数的，⛔ 不是文本模式）：`CardView.cs` **11340 / 11340** ·
`CardBaseDemo.cs` **4206 / 4206**，都是纯 CRLF、`\r\r` = 0 ⇒ 没有整篇翻行尾。
**`git diff --numstat`**：`CardView.cs 509/23` · `CardBaseDemo.cs 481/0` ——
两个数里都含**本批其它代理的存量**（H1/D3/H6…），本件自己的增量约 **+142/−13** 与 **+102/−0**。
**秒级类型检查**：`TMPDIR=/tmp/wf_h5 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
