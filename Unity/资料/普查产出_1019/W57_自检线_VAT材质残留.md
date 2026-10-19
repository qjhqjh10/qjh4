# W57 自检线 —— `WarpforgeVatDriver` 的 `.materials` + `TargetReticle` 的 `.material` setter（SC-5）

日期：2026-10-19 · 代理：SC-5 · 波次口径：**只做不验**（未跑 Unity / `*.Run` / player；只跑秒级 typecheck）

判据：
- `资料/普查产出_1019/W57_自检线_材质残留与校验脚本.md` §五·1 / §五·2（SC-4 的顺手发现）
- 先例：`Core/CardFeel.cs:1511` 的 `WritableMaterial` + `:1530` 的 `ProbeWritableMaterial`；
  `Battle/ScenarioMaterialFader.cs:30-92`（SC-4）
- `A1967` 同族：编辑模式下 `Renderer.material` 的 **getter** 会实例化并打
  `Instantiating material due to calling renderer.material during edit mode …`

---

## 一、两处结论

### A. `Battle/WarpforgeVatDriver.cs` —— **改**（`.materials` → 显式私有实例 + 缓存 + 释放）

**原值（动手前现读，行号为改前）**

| 位置 | 原代码 |
|---|---|
| `Awake`（旧 `:254`） | `_mats = r.materials;` |
| `UpdateMaterials(bool shared)`（旧 `:319`） | `_mats = shared ? r.sharedMaterials : r.materials;` |

**新值（改后现读）**

| 位置 | 新代码 |
|---|---|
| `Awake` `:263` | `BindInstanceMaterials(r);` |
| `UpdateMaterials` `:331` | `if (shared) BindSharedMaterials(r); else BindInstanceMaterials(r);` |
| 新增 `BindInstanceMaterials` `:352-377` | 读 `r.sharedMaterials` → 逐槽 `new Material(src) { name = src.name + " (VatDriver)" }` → 赋回 `r.sharedMaterials`；**已经是我们的这一组则直接复用**（按元素引用比较） |
| 新增 `BindSharedMaterials` `:379-386` | 原 `shared = true` 那一支（直接吃 `sharedMaterials`，不复制） |
| 新增 `ReleaseOwned` `:388-401` · `OnDestroy` `:403` | 换代 / 组件销毁时释放我们自己造的实例（别人的资产不碰） |
| 新增 `SameRefs` `:405-412` | 元素引用比较（`sharedMaterials` getter 每次都返回新数组，比数组引用会误判） |

**为什么不是机械替换（三条）**

1. **不能直接改成 `sharedMaterials`** —— `SendDataToRenderer` 每帧往 `_mats` 写 `_State` / 两张顶点贴图；
   而 `WarpforgeEffectBinder.Cache` 建出来的材质是**跨效果共享**的（binder `:218-223` 已明确警告：
   `shared = true` 会把状态写进全效果共用的那一份，串到别的 VAT 效果上）⇒ 必须保持「逐渲染器私有实例」。
2. **不能每帧/每次调用都无脑 `new`** —— 换成显式实例后如果每次都复制，每次 `UpdateMaterials(false)`
   都再生一代；故加「渲染器上已经是我们这一组就复用」的判据。
3. **这是写路径不是读路径** —— 按简报的判据走「显式 `new Material(...)` 赋回 + 缓存」，不是简单换 `sharedMaterials`。

**它到底是不是缺陷（如实）**

- **材质泄漏（真缺陷）**：`.materials` 造出来的实例**不随 GameObject 销毁**
  （Unity 文档明说「It is your responsibility to destroy the materials when the game object is being destroyed」）。
  原实现里每条运行路径都会留下孤儿实例：
  ① `Awake` 先实例化一组 → `WarpforgeEffectBinder.Awake/Apply()` 把 `sharedMaterials` 换成重建后的新材质
  ⇒ 第一组直接变孤儿；
  ② `Apply()` 末尾 `UpdateMaterials(false)` 再实例化一组。
  现在换代时由 `ReleaseOwned()` 销毁、`OnDestroy` 兜底。
- **不是每帧泄漏**：`Awake` 只一次；`UpdateMaterials` 只在 `WarpforgeEffectBinder.Apply()`（每个效果实例一次）
  与探针里调。逐帧写的是 `_mats` 里的属性，**不是**重新取 `.materials`。
- **编辑模式警告**：`Renderer.materials` 的 getter 在托管层是 `GetMaterialArray()`（实例化 getter，IL 现核 →
  §二），与 `Renderer.material` 的 getter 同族。⚠️ 但**本组件今天在编辑模式基本不会跑**：
  `WarpforgeVatDriver` 没有 `[ExecuteAlways]`，批处理自检的 `AddComponent` 不跑 `Awake`；
  只有 `Vanguard Frame Animated VAT` / `VanguardIdleEffect` 两个 prefab 带它，而
  `Editor/BattleScene.cs:7340-7358` 只 `LoadAssetAtPath` 取 binder 定义、**不实例化**。
  ⇒ 这一条是**防回归**（有人加 `[ExecuteAlways]`、或用 `WarpforgeEffectPlayer` 在编辑模式生成
  Vanguard 效果 —— `WarpforgeEffectPlayer.Init` 在 `!Application.isPlaying` 时会主动 `binder.Apply()`，
  见 `WarpforgeEffectPlayer.cs:216`），⛔ 不是「现在日志里已经有这条警告」。

### B. `Battle/TargetReticle.cs:368` —— **不改**

- 现代码（现读 `:368`）：`_line.material = BuildLineMaterial();`
- **判据 = Unity 托管层 IL 现核**（`UnityEngine.CoreModule.dll`，见 §二）：
  - `Renderer.set_material(Material)` → `call Renderer::SetMaterial(value)`（**只有这一句**）
  - `Renderer.set_sharedMaterial(Material)` → `call Renderer::SetMaterial(value)`（**逐字相同**）
  - 实例化警告只发生在 **getter**：`get_material()` → `GetMaterial()` · `get_materials()` → `GetMaterialArray()`
  ⇒ **`material` 的 setter 与 `sharedMaterial` 的 setter 是同一个调用**，不可能触发那条警告。
- 另两条事实：① 赋进去的是 `BuildLineMaterial()` **当场 `new Material(sh)`** 的私有实例
  （不是工程资产、没有别的使用者）⇒ 就算改成 `sharedMaterial` 也是同一个对象、零行为差；
  ② `Build()` 有 `if (_built) return;` 闸（`:334-337`）⇒ **一次性**，不是每帧。
- 结论：**不改**（改成 `sharedMaterial` 只会制造一次无意义的 diff）。

---

## 二、判据：`UnityEngine.CoreModule.dll` 的属性访问器 IL（2026-10-19 现读）

工具（临时；`d:/4/Unity/_tmp_view/` 已被 `.gitignore:235` 的 `/Unity/_tmp_*/` 忽略）：
`d:/4/Unity/_tmp_view/sc5_il/{dump.exe,line.exe}`，用 Unity 自带的 `Unity.Cecil.dll` 读 IL。实测（节选）：

```
=== Property materials ===
-- getter --
  UnityEngine.Material[] UnityEngine.Renderer::get_materials()
    IL_001d: call UnityEngine.Material[] UnityEngine.Renderer::GetMaterialArray()
-- setter --
  System.Void UnityEngine.Renderer::set_materials(UnityEngine.Material[])
    IL_0003: call System.Void UnityEngine.Renderer::SetMaterialArray(UnityEngine.Material[])

=== Property material ===
-- getter --
  UnityEngine.Material UnityEngine.Renderer::get_material()
    IL_001d: call UnityEngine.Material UnityEngine.Renderer::GetMaterial()
-- setter --
  System.Void UnityEngine.Renderer::set_material(UnityEngine.Material)
    IL_0003: call System.Void UnityEngine.Renderer::SetMaterial(UnityEngine.Material)

=== Property sharedMaterial ===
-- setter --
  System.Void UnityEngine.Renderer::set_sharedMaterial(UnityEngine.Material)
    IL_0003: call System.Void UnityEngine.Renderer::SetMaterial(UnityEngine.Material)

=== Property sharedMaterials ===
-- getter --
  IL_0002: call UnityEngine.Material[] UnityEngine.Renderer::GetSharedMaterialArray()
-- setter --
  IL_0003: call System.Void UnityEngine.Renderer::SetMaterialArray(UnityEngine.Material[])
```

`UnityEngine.CoreModule.xml:34807-34811` 同样写着：`Renderer.materials` = 「Returns all the
**instantiated** materials of this object.」⇒ getter 会实例化，setter 不会。

⚠️ **由此翻出一条旧注释要订正**：`Battle/ScenarioGenericMaterialBlend.cs:90` 写着
「`.material` setter 在编辑模式下同样会触发（警告）」—— **与 IL 不符**（setter 就是 `SetMaterial`，
与 `sharedMaterial` setter 逐字相同）。该文件**不在本批白名单**、未改，请主对话另行派活（铁律 5）。

---

## 三、改动清单

| 文件 | 位置（改后现读） | 内容 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Battle/WarpforgeVatDriver.cs` | `:60-66` | 文件头「原版 ↔ 本组件」那一行跟随新实现订正 |
| 同上 | `:237` | 新增 `bool _ownsMats`（私有实例所有权） |
| 同上 | `:260-263` | `Awake` 的 `_mats = r.materials` → `BindInstanceMaterials(r)` |
| 同上 | `:320-331` | `UpdateMaterials(bool)` 两分支改走 `BindSharedMaterials` / `BindInstanceMaterials` |
| 同上 | `:334-412` | 新增 `BindInstanceMaterials` · `BindSharedMaterials` · `ReleaseOwned` · `OnDestroy` · `SameRefs` |
| 同上 | `:500-621` | `#if UNITY_EDITOR` 的 `ProbeMaterialBinding()` + `VatMaterialProbe`（产品路径不调用） |
| `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` | `:6698-6732` | 新增断言块（10 条，接在 SC-4 的 `A1967` 残尾块之后） |
| `.../Battle/TargetReticle.cs` | — | **未改**（见 §一·B） |

行尾核对（二进制计数）：`WarpforgeVatDriver.cs` CRLF 0 / LF 623（原 LF，未翻）·
`RewardsScene.cs` CRLF 0 / LF 12211（原 LF，未翻）。
`git diff --numstat`：`WarpforgeVatDriver.cs` **217/4**（不是整篇重写）；`RewardsScene.cs` **485/62**
（含本会话之前已有的改动；本次插入块 = **35 行**）。

---

## 四、断言（宿主 = `Editor/RewardsScene.cs:6703-6731`，共 10 条）

`WarpforgeVFX.WarpforgeVatDriver.ProbeMaterialBinding()` 的读数逐项断：

1. 探针跑得起来（`Error == null`）
2. **灭自证**：故意走 `.material` ⇒ 抓到 **≥1** 条 Unity 警告（抓不到 = 检测器失效，下面 0 条无意义）
3. 负例前提：`.material` 给的确实是一份独立实例
4. 被测 `BindInstanceMaterials` ⇒ **0 条**警告（🧨 改回 `r.materials` ⇒ 这条红）
5. 驱动拿到的是独立于源材质的私有实例
6. 私有实例已赋回 `sharedMaterials`（渲染器真的在用它）
7. 源材质没被写脏（在副本上写 `renderQueue = 4000`，源仍是 3000）
8. 同一份渲染器材质再绑一次 ⇒ **复用同一实例**（false = 每次调用都再生一代）
9. 渲染器材质被换掉（= binder `Apply()` 重建）⇒ 生成新一代私有实例、第二份源材质不脏
10. 换代时**上一代私有实例被销毁**（false = 每次 `Apply()` 漏一组 —— 原来 `.materials` 的行为）

⚠️ 第 7 条**故意用 `renderQueue` 而不是 `_State`**：探针用的 `Sprites/Default` 上没有 `_State`，
拿它比会恒等于默认值 ⇒ 假绿。

---

## 五、没查清的部分

1. **原生 `GetMaterialArray()` 在编辑模式是否真的打那条警告 —— 本批没有实跑复核**（本波口径 = 只做不验）。
   托管层只能证明它调的是实例化 getter、与 `GetMaterial()` 同族；SC-4 的结论据此沿用。
   断言里**灭自证**用的是已实测可靠的 `.material` getter，所以「正例 0 条」本身可判。
2. **探针本身没有真跑**（`RewardsScene.Run` 未执行）—— 按用户口径攒到复刻完成再统一验收。
   首跑若第 10 条（`OldReleased`）红，先看 `DestroyImmediate` 是否被 `OnDestroy` 抢跑
   （探针在 `finally` 里先销毁宿主再收尾，逻辑上是幂等的）。
3. **`UpdateMaterials(true)` 分支没有运行路径** —— 全仓唯一调用点 `WarpforgeEffectBinder.cs:223`
   传 `false`；`true` 那支只由 typecheck 保证编得过。
4. **`TargetReticle` 的 `BuildLineMaterial()` 没有 `OnDestroy` 释放**（每组件 1 份 Material，
   靠场景卸载/`Resources.UnloadUnusedAssets` 兜底）。量级极小、且不在本任务范围 ——
   记为**顺手发现**，未改（要做得在 `TargetReticle` 里补 `OnDestroy`，但本批只判该不该改那行赋值）。
5. **并行代理的中间噪声**：另一个代理在写 `Battle/BattleEventSystem.cs`（新增文件，缺
   `using UnityEngine.UI;`）与 `Editor/BattleScene.cs`（`ClickHudButtonForTest` 未定义）。
   中间两次 typecheck 各报过 1 条，**均为白名单外、非本代理改动**；等它们落盘后最终 typecheck = 0/0。

---

## 六、typecheck

```
TMPDIR=/tmp/wf_sc5 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

没有跑任何 Unity 批处理 / `*.Run` / player（遵守本波「只做不验」）。
