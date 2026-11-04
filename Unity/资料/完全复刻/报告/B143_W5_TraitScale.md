# B143-W5 · `B142-TRAIT-SCALE` —— `Trait Tooltip` 的 1.25 整颗缩放

> 执行写手 `B143-W5`（2026-11-05）。判据由 `B143-R2` 查清（`资料/完全复刻/判据/B143_四条待判据.md` §3），本件只做落地。
> ⛔ 没跑 Unity 批处理 / 没起 player / 没动 git。

## ① 结论

**🟠 已修**。原版 `Trait Tooltip.prefab` **根**上的 `TransformScalerBySmallScreenUI`（`menuScale = 1.25`）等价物已接上：
trait 档 ⇒ 根 ×1.25（且**小屏开关开着**）；切回基础档 ⇒ 回 (1,1,1)。基础档（`BasicToolTip`）那头原版**没有**这颗组件 ⇒ 我们的组件常挂但 `menuScale = 1` ⇒ `enabled = false`。

| | |
|---|---|
| 组件在不在 | ✅ **在**：`Assets/CardPresentation/Shell/TransformScalerBySmallScreenUI.cs`（`GUID`/类名/`menuScale`/`SetScale`/`Initialize` 裸 `!=`/`Tick` 的 0.0001 守卫全部逐句对原版）⇒ **本件一个字都没改它** |
| 改在哪 | `Core/Tooltip.cs`（**3 行改、0 行增减**：`partial` 声明 + 两处同一物理行内追加调用）· **新建** `Core/Tooltip.TraitScale.cs`（成员全在这里） |

## ② 帧口径结论（⚠️ 施工前必须查的那条）

| 问 | 答 | 判据 |
|---|---|---|
| 我们的 tooltip 根有没有 `localScale`？ | **没有**：`Core/Tooltip.cs` 全文件 `grep localScale` = **0 命中**；根 GO `TooltipLayer` 两档都是默认 (1,1,1)（uGUI 档只设 anchor/pivot/sizeDelta/anchoredPosition，世界档连 RectTransform 都没有） | 现读 `Tooltip.cs:231-250` |
| 那「乘 `localScale`」会不会乘到错帧上？ | **不会**：`localScale` 是**无量纲**的，与「px 帧 / 世界帧」无关 —— 它只决定「这棵子树相对根的像素尺寸」 | uGUI 语义 |
| 缩放中心在哪？ | uGUI 档根 `pivot (0.5, 0.5)`；世界档根的「原点」就是子件坐标系的原点。两档的 `Pivot()` 都只写**子件** `localPosition`（`_bgRoot` / `_body` / trait 三件）⇒ 缩放按根原点整体放大，与原版「根上那颗组件、子件全 1」**同构** | `Tooltip.cs:555-568` / `:776-786` |
| 还有别人写根的 `localScale` 吗？ | **没有**（全仓 `grep` tooltip + scale = 0 命中） | 2026-11-05 现扫 |

⇒ 结论：**照原版乘根 `localScale` 即可**，不需要把 1.25 折进 px 矩形（那反而会与「子件全 1、只有根非 1」的原版形状不一致）。

## ③ 行为（照原版两条方法）

- `Initialize()`（= 原版 `__Initialize.c` 全文）：`enabled = menuScale != 1f && SmallScreenUI.Enabled`（**裸 `!=`**）。
- `Tick()`（= 原版 `__LateUpdate.c` 全文）：当前 `localScale` 与上次自己设过的值逐分量差平方和 `< 0.0001` ⇒ 返回；否则 `localScale = 当前 × menuScale` 并**回读**。
- 生产路径：Unity 只在 `enabled` 时调 `LateUpdate`；**批处理没有帧循环** ⇒ 我们的 `ApplyTraitScale` 在 `enabled` 时显式补一拍 `Tick()`（幂等）。

## ④ 改动清单

| 文件 | 行数 | 改了什么 |
|---|---|---|
| `Assets/CardPresentation/Core/Tooltip.cs` | **3 / 3（净 0；1353 行不变）** | ① `:60` `public class Tooltip` → `public partial class Tooltip`（零行数变化，见下「为什么 partial」）；② `:673` `if (_body == null) return;` **同一物理行**追加 `ApplyTraitScale(true);`；③ `:772` `_traitMode = false;` **同一物理行**追加 `ApplyTraitScale(false);` |
| `Assets/CardPresentation/Core/Tooltip.TraitScale.cs` | **新建**（+~120 行） | `TraitMenuScale = 1.25f` 常量 · `_scaler` 字段 · `ApplyTraitScale(bool)` · 三个自检读口（`RootScaleForTest` / `TraitScalerEnabledForTest` / `TraitMenuScaleForTest`） |
| `Assets/CardPresentation/Editor/B143_W5_TraitScale_Assert.cs` | **新建** | 断言（见 ⑥；**还没接线**，见「待办」） |

**为什么走 partial 而不是加进 `Tooltip.cs`**：`账/*.tsv` 有 **58 处**按行号引用 `Core/Tooltip.cs`，其中三处（`:1161` / `:1188` / `:1329`）在 **`TipText`**（`Tooltip` 类之后）⇒ 往 `Tooltip` 里插一行，那三条**整体漂**。本仓纪律明写「热点文件走新 partial 文件，⛔ 别插进既有成员之间」（`CLAUDE.md` §派活四条长期纪律）。所以 `Tooltip` 加 `partial`（零行数），新成员放新文件；两处调用用**同一物理行内追加**（也是本仓既有写法），**净行数 0** ⇒ 账闸零漂移（实测见 ⑦）。

## ⑤ ⚠️ 与施工单的一处就地订正（铁律 5）

施工单 §3·4 步骤 2 写的是：

```csharp
_scaler.Initialize();
_scaler.Tick();          // ← 无条件
```

**不成立。** `TransformScalerBySmallScreenUI.Tick()` 自己写着「**本函数不自己判 `enabled`**（Unity 只在启用时调 `LateUpdate`）」⇒ 开关**关着**时无条件 `Tick()` 会照样把 1.25 乘上去（= 「小屏关着也放大」那个 bug；也正好会被本件 B3 断言抓住）。
⇒ 落地成：**`enabled` 才 `Tick()`**；`enabled == false` 时把根复位成 `(1,1,1)`（= 原版那颗 prefab 上根本没有组件的情形）。

另一处更强的口径：`ApplyTraitScale(true)` 放在 `LayoutTrait` **每次**进都跑（不是只 `!_traitMode` 那一次）——我们的 tooltip 是**常驻单例**，原版每次弹都是 `Instantiate` 一份新 prefab（`OnEnable → Initialize` 会重读开关）⇒ 只在首次进档做一次会留下「中途改开关」的过期状态。`Tick()`/`Initialize()` 都幂等，代价为 0。

## ⑥ 断言（`Editor/B143_W5_TraitScale_Assert.cs`）

| 条目 | 断什么 | 🧨 什么坏法会让它红 |
|---|---|---|
| A1 / A1' | `TraitMenuScale == 1.25` **且 ≠ 1** | 改成 1 ⇒ 两条红 |
| A2①/②/③ | 源码里 `LayoutTrait` **和** `ClearTrait` **都**调了 `ApplyTraitScale`；`Tooltip` 是 `partial` | 删掉任一条调用 ⇒ 对应条红 |
| B1①②③ | 小屏 ON + trait：根 `localScale.x == 1.25`、`menuScale == 1.25`、`enabled == true` | `TraitMenuScale` 改 1 ⇒ B1③ 红 |
| **B2（灭自证）** | 同一实例切回基础档 ⇒ 根回 **1**、`menuScale == 1` | 删 `ClearTrait` 那句 `ApplyTraitScale(false)` ⇒ 根停在 1.25 ⇒ 红 |
| B3 | 小屏 **OFF** + trait ⇒ 根 **1**、`enabled == false` | 无条件 `Tick()` ⇒ 红；`Initialize` 改成恒 true ⇒ 红 |
| B4 | 关→开再弹 ⇒ 回到 1.25（单例跨档不残留） | 状态清不干净 ⇒ 红 |

**待办（不在本代理白名单）**：断言文件需要宿主一行接线（`Editor/BattleScene.cs` 或 `Editor/ShellScene.cs` 的汇总行）：
`B143_W5_TraitScale_Assert.Run(); _sink.Pass += …Pass; _sink.Fail += …Fail;`
⚠️ 该段会反复建/拆 tooltip 单例，结束已 `DestroyImmediate` + `SmallScreenUI.ResetForTest()`（`PersistOverride` 期间**不写 `PlayerPrefs`**）。

## ⑦ 验证

```text
运行时错误数: 0
编辑器错误数: 0
```

- `TMPDIR=/tmp/wf_w5 bash D:/4/Unity/工具/typecheck.sh` ⇒ 上一行两行（两个新文件 + `Tooltip.cs` 都编过）。
- 账闸只读：`python -I -X utf8 工具/check_ledger_points.py` ⇒ 输出里 **`Core/Tooltip.cs` 0 条**（`grep -c` = 0）⇒ 我这三处改动**零漂移**；当次全表 `漂移 239 / 失败 20` 全在别人的在飞文件上。
- ⚠️ 新文件没有 `.meta`（`B142_E18_Assert.cs` 同样没有 ⇒ 本仓惯例是让 Unity 首次导入时生成）。

## ⑧ 没查清 / 待裁定

1. **没跑实况**：原版社交/战斗实机没跑，1.25 是 **prefab 序列化值 + 反编译**（两源一致），没在真机看到「小屏档下 trait tooltip 真的大 25%」。
2. **`Trait Tooltip` 根是不是我们 `TooltipLayer` 的精确对应物**：原版根下只有一层 `Content`，我们根下直接是九宫底 + 文字/三件 ⇒ 缩放乘在根上**视觉等价**，但**层级不同**（不影响本件）。
3. **其他 tooltip 变体**（`Battle Tooltip Variant` / `Updated Tooltip Variant` / `- ranked`）**都没有这颗组件** —— ⛔ 别一起加（`B143_四条待判据.md` §3·5.3）。
4. 缩放后**命中区/锚点**是否与原版一致：本件只改 `localScale`，`Pivot()` 的算式没动；「放大后 tooltip 会不会遮住鼠标/出屏」这类**观感**问题需要真机看（本轮没看）。
