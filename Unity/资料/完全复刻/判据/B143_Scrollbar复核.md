# `B143_Scrollbar复核` —— 第三方复核 `B143-W7` 的滚动条改动

> 复核人 `B143-W5`（2026-11-05）· **只读**：没改代码 / 没跑 Unity / 没动 git。
> 被复核的对象：`Shell/MenuScroll.cs` 类尾新增的 `ScrollbarTint` / `ScrollbarBinding` / `BindScrollbar` /
> `SyncScrollbar` / `ScrollbarDragTo` / `MeasurePtrPerDesign` / `ScrollbarSelfCheck`（`:705-880`），
> `SetOffset`（`:336`）/ `Tick`（`:610`）各一句行内 hook，以及 `Shell/AllianceMemberTab.cs:2808-2816` 的 9 行调用。

## 结论

**🟠 需改**（1 处真问题 + 3 处提示级）。真问题**不是**振荡，而是**两处「要不要显示滚动条」的口径不一致**，
它会让「15 项（>14）」这一档的滚动条**永远不显示**，并让 `B142_E18_Assert` 新增的 `C3` 当场红。补丁在 §5。

| # | 问题 | 判定 |
|---|---|---|
| Q1 | 共用件零影响（除两句 hook 外有没有别的路径碰新字段） | **基本成立**（只有那两句；但有 3 条**不经过 hook** 的路径，见 §1） |
| Q2 | AutoHide 关节点不碰 Handle ⇒ 会不会留「Handle 定格在中间」 | **不会**（整根关掉 + 重开按 `Offset` 重算）；有一处 `Track.H` 的边角 |
| Q3 | `RegisterDrag` 注册/注销是否成对；隐藏时会不会占着指针层 | **不会占**（隐藏 ⇒ 打不到；销毁 ⇒ `TryGetDrag`/`PruneDrags` 会清）；**弱泄漏**可接受 |
| Q4 | 与 245/228 改动会不会互相触发振荡 | **振荡：不会**；**冲突：会**（见 §4，这是本单要改的那一处） |

## 1. Q1 —— 共用件零影响

**hook 只有两句**（全文件 `grep "Scrollbar"` 只有这两处 + 新段本身）：

| 行 | 代码 | 触发条件 |
|---|---|---|
| `:336` | `if (OnChanged != null) OnChanged(); if (Scrollbar != null) SyncScrollbar();` | `SetOffset` 里、**与 `OnChanged()` 同一物理行**（未绑 ⇒ 只多一次 null 判断） |
| `:610` | `if (moved && OnChanged != null) OnChanged(); if (Scrollbar != null && moved) SyncScrollbar();` | `Tick` 末尾 |

**`MenuScroll` 是普通类**（不是 `MonoBehaviour`）⇒ 全文件 **没有** `OnEnable` / `OnDisable` / `Dispose` / `Reset` 这类生命周期口；新字段（`Scrollbar` 属性 / `_sbDragging` / `_sbStartY` / `_sbStartNorm`）**只在上面两句与新段内部被读写**。✅

**但有三条「改 `Offset` 但不经过 hook」的路径**（提示级，不是缺陷）：

| 路径 | 会不会漏同步 | 结论 |
|---|---|---|
| `Wheel` → `Apply()`（`:505-512` 里直接 `Offset = v`） | 漏 | 生产每帧 `PointerLayer.Update → s.Tick(dt)`（`PointerLayer.cs:1529`）⇒ **下一帧补上**；⚠️ **批处理没有帧循环** ⇒ 自检里 `Wheel()`/`DragTo()` 之后要**手动 `Tick(0.016f)`** 才读得到 Handle 的新位置 |
| `DragTo` → `Apply()` | 同上 | 同上 |
| `SetExtraPadding` / `ToggleScrollLock`（改 `MaxOffset` 而不动 `Offset`） | 漏（连 `Offset` 都没变，`SetOffset` 都不会进） | 今天只有锻造线用这两条，**没绑滚动条** ⇒ 不可观测；将来若绑了，**要在调完之后补一次 `SyncScrollbar()`** |

## 2. Q2 —— AutoHide 会不会留「Handle 定格在中间」

`SyncScrollbar`（`:813-833`）的顺序是：

```csharp
bool show = MaxOffset > 0f;
if (b.Root != null && b.Root.gameObject.activeSelf != show) b.Root.gameObject.SetActive(show);
if (!show || b.Handle == null || b.Track.H <= 0.01f) return;      // ← 关的那一支：直接 return，不动 Handle
… 按 Offset/MaxOffset 重算 Handle 矩形 …
```

⇒ **不会有「定格在中间」的残留视觉**：
- 关的时候**整根 `Scrollbar` 被 `SetActive(false)`**，Handle 是它的后代 ⇒ 一起不画；
- 再开的时候**每次都从 `Offset` 重算**（不读旧 Handle 位置）⇒ 位置一定对；
- `!show` 早退也顺手挡住了 `0 / 0`（`Offset/MaxOffset` 的 NaN）那一支 ✅。

**两处边角（提示级）**：
1. `Track.H <= 0.01f` 时 `show` 仍可能为真 ⇒ **滚动条会显示、但 Handle 停在上一帧的位置**。今天 `Track` = `Sliding Area` 恒有高（= `SbW` 那棵 573 级），不可达。
2. `MaxOffset` 由 >0 变 0 时**不会**顺手把 `Offset` 夹回 0（`SyncScrollbar` 只读不写）。`Offset` 会停在旧值，直到下一次 `SetOffset`。今天 0 处会让已绑滚动区的 `MaxOffset` 中途变 0（下拉的 `Content` 高是建列表那一刻定死的）⇒ 不可观测；将来若让下拉支持「边滚边重建」，这条会现形。

## 3. Q3 —— `RegisterDrag` 的注册 / 注销

- **注册**：`BindScrollbar`（`:789-808`）在 `HandleQuad != null` 时给 Handle 挂 `WindowButton` 并 `PointerLayer.RegisterDrag(wb, …)`（`:801`）。**幂等**：`RegisterDrag` 内部先 `PruneDrags()` 再按 Button 去重（`PointerLayer.cs:327-330`）⇒ 重复绑不会叠加 ✅。
- **注销**：`MenuScroll` **没有**反注册口（它不是 `MonoBehaviour`，没有 `OnDestroy`）；`AllianceDropdownBox` 也没有调 `UnregisterDrag` / `UnregisterDragsOwnedBy`。
- **会不会占着指针层？不会**：
  - 打通这条路的前提是**指针先命中那颗钮**（`PointerLayer.Update`：`_down = HitButton(px,py)` → `TryGetDrag(_down, …)`），而 `HitQuad` 要求 `activeInHierarchy`（`PointerLayer.cs` 的 `HitQuad`/`QuadContainsX` 那一族）⇒ **Handle 在关着的 `Scrollbar` 里永远打不到**；
  - 宿主销毁后，`TryGetDrag` 每次按下时会 `if (d.Button == null) { _drags.RemoveAt(i); }`（`PointerLayer.cs:373`），`PruneDrags`（`:379-383`）也会清 ⇒ **死条目不会永久堆积**。
- **弱泄漏（可接受）**：每建一个下拉会多一条 drag 登记，直到**下一次任何 `RegisterDrag` 或下一次按下**才被清。今天只有 2 个下拉 + 2 个夹具 ⇒ 量级可忽略；建议 W7 在 `BindScrollbar` 的 doc 里补一句「不反注册，靠 `PruneDrags` 清」。

## 4. Q4 —— 与 245/228 改动会不会互相触发

### 4.1 振荡：**不会**
- `Viewport`/`Item` 的宽**只在 `AllianceDropdownBox.BuildList` 里算一次**（`:2549-2553`：`needBar = contentH > viewH + 0.01f` → `itemW = needBar ? w - 17 : w`）；
- `SyncScrollbar` 只做两件事：`Scrollbar.SetActive` + 挪 Handle 的 `localPosition`（`MenuDraw.ApplyPxRect`），**从不回写 `Viewport` / `Content` / `MaxOffset`**；
- 故「开滚动条 → Viewport 变窄 → 内容高变化 → 又不需要滚动条」这条链**在本仓不存在**（我们的内容高 = `rows × ItemH + ItemGap`，与宽度无关）。✅

### 4.2 冲突：**会**（🔴 本单要改的就是这一处）

「要不要显示滚动条」现在有**两个口径**，且 n > 14 时**相反**：

| 口径 | 算式 | n = 15 时 |
|---|---|---|
| 生产（我这一轮加的） | `needBar = n × ItemH + ItemGap > viewH + 0.01f`（**按原版的真实内容高 = 全部 n 行**） | **true** |
| `MenuScroll`（W7 的 `SyncScrollbar`） | `show = MaxOffset > 0f`；而 `MaxOffset` 只认**实际建出来的 `Content` 高** = `rows(14) × ItemH + ItemGap` | **false** |

**执行顺序**（`AllianceMemberTab.cs:2807-2816`）：先 `_scrollbar.SetActive(needBar)`（true），**紧接着** `_scroll.BindScrollbar(...)` → `SyncScrollbar()` → `show = false` → **又 `SetActive(false)`**。
⇒ **n > 14 时滚动条永远不显示**（`needBar` 被 `SyncScrollbar` 覆盖），且 `B142_E18_Assert` 的 **`C3`（15 项 ⇒ `ScrollbarShown == true`）当场红**。

> 注：14 项及以下**不受影响** —— 我按 float32 复算过 `MaxOffset`（`MenuScroll.TopAligned(vpR, contentR.H)` → `ContentX2 = vpR.y1 + (vpR.y2 − vpR.y1)`）：三个真实站点（`LanguagesDropdown` / `Config fields` / `Field14` / `Field3`）都是**逐位 `MaxOffset == 0.0`**（不是 `1e-5`）⇒ `show = false` 稳，`B0⑥` 那条断「14 项不显示」是安全的。

## 5. 补丁（🟠 需改 · 一行 + 一个形参）

**根因**：`MenuScroll` 的「内容范围」喂的是**已建行数**（`contentR.H`），而生产判 `needBar` 用的是**原版真实内容高**（`n × ItemH + ItemGap`）。两边统一即可。

`AllianceMemberTab.cs`（我的文件，但按本次口径**只给补丁不落**）：

```csharp
// ① 调用点（:2554）
BuildScrollbar(listR, vpR, contentR, vc, needBar);
→ BuildScrollbar(listR, vpR, contentR, vc, needBar, contentH);

// ② 签名（:2774）
void BuildScrollbar(PxRect listR, PxRect vpR, PxRect contentR, ViewportClip vc, bool needBar)
→ void BuildScrollbar(PxRect listR, PxRect vpR, PxRect contentR, ViewportClip vc, bool needBar, float contentH)

// ③ 建滚动区那一行（:2799）
_scroll = MenuScroll.TopAligned(vpR, contentR.H, ScrollSensitivity);
→ // ✅ 2026-11-05 B143-W5（B143-W7 复核）：内容范围要用**原版真实内容高**（全部 n 行），
  //   ⛔ 不是已建行数 —— 否则 `SyncScrollbar` 的 `MaxOffset > 0` 与 `needBar` 在 n > 14 时相反。
  _scroll = MenuScroll.TopAligned(vpR, contentH, ScrollSensitivity);
```

**影响**：n ≤ 14 时 `contentH == contentR.H`（同一算式）⇒ **逐位不变**；n > 14 时 `MaxOffset = (15−14) × 40.8707 > 0` ⇒ 与 `needBar` 一致，`C3` 转绿。

**仍存的已知缺口（不是本补丁能治的，已在 `报告/B143_W5_下拉滚动.md` §④-6 记过）**：n > 14 时只建了 14 行，滚动会把 Handle 挪动而**内容不跟着重建**（没挂 `OnChanged`）。今天 n 最大 14 ⇒ 不可达；要真支持得补「按偏移重建行 + 重命中区」一整条。

## 6. 其余复核（没问题，留作依据）

- **Handle 几何与方向**：`span = size × Track.H`、`h = span + PadY`、`y1 = Track.y1 + norm × (Track.H − span) − PadY/2`；`norm = 0`（滚到顶）⇒ Handle 贴**顶**、`norm = 1` ⇒ 贴**底**（`y1 + h = Track.y2 + PadY/2`）。与 UGUI `BottomToTop` + `value = verticalNormalizedPosition` 的视觉一致 ✅。
- **色偏表**：四个值全部**转引** `WindowButton` 的常量（`ScrollbarTint`），`ScrollbarSelfCheck()` 逐值自检 —— 与 prefab 6 份 `Scrollbar` 的 `m_Colors` 对上 ✅；`m_TargetGraphic` = Handle 自己的 Image ✅（段头写清了）。
- **拖拽方向**：指针向下（`pageY` 增）⇒ `dyDesign > 0` ⇒ `norm` 增 ⇒ `Offset` 增 ⇒ 内容上移（看后面的行）—— 与「拖滑块向下 = 内容向上滚」同向 ✅。`PtrPerDesign` 用 `PointerFramePxFromDesign` 量一次并缓存，量不出时回落 1 ✅。
