# W58 · 自检断言线 · `A1350` 读口 API 新增（执行报告）

> 写手 **W58-HELPERS** · 2026-10-20 · 第五十八会话
> 范围：**只新增三个读口**（`MenuCheck.DesignPx(Vector3,Transform)` · `MenuCheck.PointerFramePx` ·
> `SelfCheckTextMetrics.RenderedRectDesign`）。⛔ 没改任何现有函数的行为、没改宿主、没动 git / 正本 / Unity / `*.Run`。

---

## 结论

- ✅ 三个函数**全部新增完毕**，签名 / 语义 / doc 注释齐全，**现有函数一个字未动**（`git diff --numstat` 两份都是**纯新增、0 删除**）。
- ✅ 秒级 typecheck：**运行时 0 错 / 编辑器 0 错**（`TMPDIR=/tmp/wf_helpers bash d:/4/Unity/工具/typecheck.sh`）。
- ⚠️ 一处**实现选择需调度台知悉**：`RenderedRectDesign` 的四角换算**转调 `MenuCheck.DesignPx(corner, tmp.transform)`**，
  而不是在 `SelfCheckTextMetrics` 里再写一份「除父级 `lossyScale` + `DivByScale` 守卫」——
  理由是 `MenuDraw.PosInDesignSpace` 的形参是 `Transform`（**没有世界点重载**），若在文字侧再抄一份除法，
  就等于全仓出现**第三份**同一算式（铁律：一条规则只留一处）。因此「四角先过设计空间」这一步的**唯一实现**
  = `MenuCheck.DesignPx(Vector3, Transform)`，语义与 `PosInDesignSpace(tmp.transform)` 逐字一致（除的是 TMP 节点的**父级**那一级）。

---

## 证据（文件:行号 · 现读 2026-10-20）

| 事实 | 出处 |
|---|---|
| `DesignPx(Transform)` 已存在（第一批） | `Unity/MyGame/Assets/CardPresentation/Editor/MenuCheck.cs:161` |
| `RectCenterMode` / `NodeRectPx` 已存在（第一批） | `MenuCheck.cs:190`（枚举）/ `:243`（`NodeRectPx`） |
| `MenuDraw.PosInDesignSpace` = 除**父级** `lossyScale`，根不除 | `Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:71-79` |
| `MenuDraw.DivByScale`（`private`，退化守卫） | `MenuDraw.cs:83-84` |
| `MenuDraw.PixelOfDesign` = `LayoutSpace.ToDesignPixel` 一行转调 | `MenuDraw.cs:629` |
| `LayoutSpace.ToDesignPixel` = x 走实测 `PxPerWorldX`、y = `PxY` | `Unity/MyGame/Assets/CardPresentation/Core/LayoutSpace.cs:299-302` |
| `LayoutSpace.PxPerWorldX`（指针帧与设计帧同一条斜率） | `LayoutSpace.cs:278-285` |
| `RenderedRect`（世界帧，**本次未改**） | `Unity/MyGame/Assets/CardPresentation/Editor/SelfCheckTextMetrics.cs:57-75` |
| 尺寸帧缺陷（中心乙式 + 尺寸甲式） | `资料/普查产出_1019/W57_自检线_A1350读口统一计划.md` §2·3 |
| 指针帧必须保留（⛔ 别喂设计帧） | 同上 §2·1 注释 + `Shell/PointerLayer.cs:1071-1089` |

---

## 三个函数的最终签名与实现要点

### 1. `MenuCheck.DesignPx(Vector3 worldPoint, Transform owner)`
> `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MenuCheck.cs:178`

```csharp
public static Vector2 DesignPx(Vector3 worldPoint, Transform owner)
{
    if (owner == null) return new Vector2(-99999f, -99999f);
    var above = owner.parent;
    if (above == null) return MenuDraw.PixelOfDesign(worldPoint);   // 根：链上没有缩放可除
    var k = above.lossyScale;
    return MenuDraw.PixelOfDesign(new Vector3(DivByScale(worldPoint.x, k.x),
                                              DivByScale(worldPoint.y, k.y),
                                              DivByScale(worldPoint.z, k.z)));
}
```

要点：
- **除的是 `owner` 的父级那一级 `lossyScale`**（= 甲式 `PxOf(t.position.x)` 漏掉的那一级；
  与 `PosInDesignSpace` 同一口径：自己的 `localScale` 只影响孩子、不影响自己的位置）。
- `owner == null` ⇒ `(-99999,-99999)`（与 `DesignPx(Transform)` 同哨兵，⛔ 不落回 960,540）。
- `owner.parent == null` ⇒ **不除**（照 `PosInDesignSpace` 的根分支）。
- 退化守卫（非有限 / `|k| < 1e-6` ⇒ 原样返回）由新增的 **private** `DivByScale` 承担
  （`MenuCheck.cs:192`，逐字副本自 `MenuDraw.DivByScale`；那份是 `private`，**没有去改生产侧可见性**）。

### 2. `MenuCheck.PointerFramePx(Transform t)`
> `MenuCheck.cs:206`

```csharp
public static Vector2 PointerFramePx(Transform t)
    => t == null ? new Vector2(-99999f, -99999f)
                 : LayoutSpace.ToDesignPixel(t.position);
```

要点：
- **指针帧 = 不除父链缩放**（画出来多大就多大），与 `PointerLayer.HitBoxPx` 同帧。
- 名字是 `PointerFramePx`（⛔ 不叫 `PointerPx` —— 那是 `Battle/TutorialOverlay.cs` 的私有常量）。
- `t == null` ⇒ `(-99999,-99999)`。

### 3. `SelfCheckTextMetrics.RenderedRectDesign(Transform t, out float x1, out float y1, out float x2, out float y2)`
> `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/SelfCheckTextMetrics.cs:91`

要点：
- 量法与 `RenderedRect` **逐句相同**（`GetComponentInChildren<TextMeshPro>(true)` → `textBounds` →
  `localToWorldMatrix` 四角 → `min/max`），**唯一差异 = 四角再过设计空间**。
- 四角换算转调 `MenuCheck.DesignPx(corner, tmp.transform)` ⇒ 除的是 **TMP 节点父级**那一级
  （= `PosInDesignSpace(tmp.transform)` 的同一级），然后 `PixelOfDesign` 换 px。
- 契约与 `RenderedRect` 逐字一致：`t == null` / 无 TMP ⇒ `false`、四个 `out` 全 0；空串 4.29e9 哨兵照旧是
  「量法没生效」，不做特殊处理；旋转件报四角 AABB（与旧口一致）。
- **`RenderedRect` 本体一个字未改**（`git diff` 该函数区域无改动）。

---

## 没查清的部分

1. **`RenderedRectDesign` 的 `owner` 该不该恒是 `tmp.transform`**（而不是外层 `t`）—— 本批按 `RenderedRect` 的量法
   （四角来自 `tmp.transform.localToWorldMatrix`）取 `tmp.transform`。若某调用点的期望是「按外层 `t` 的父级缩放除」，
   需要调度台逐处判（W57 §七·4 同类问题仍开着）。
2. **`TMP.textBounds` 在父链带缩放时是否本身已含缩放** —— 本次按 `RenderedRect` 原样搬（`textBounds` 是局部空间 Bounds、
   经 `localToWorldMatrix` 变世界），未实跑两态（`*` 一个都没跑，铁律 12）。单位缩放下两函数应逐位相同；
   带 M 那一档的读数差异**未实测**，`W57` §V6 已登记「攒到复刻完成再跑」。
3. **实际调用点未接** —— 本批只新增 API；宿主（ShopScene / RewardsScene / CollectionScene / MainMenuScene / ShellScene）
   的转调属于后续批次，⛔ 本次一行未碰。

---

## 改动清单

| 文件 | 行 | 变化 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/MenuCheck.cs` | `:165-209`（新增节） | `+ DesignPx(Vector3,Transform)` · `+ DivByScale`(private) · `+ PointerFramePx`（**+45 行 / −0**） |
| `Unity/MyGame/Assets/CardPresentation/Editor/SelfCheckTextMetrics.cs` | `:77-111`（新增节） | `+ RenderedRectDesign`（**+35 行 / −0**） |

- `git diff --numstat`：`45 0` / `35 0` ⇒ **纯新增、无删除、未翻行尾**（两份都是纯 LF，改前 `CRLF 0 / LF 311`、`CRLF 0 / LF 138`）。
- 未碰：任何宿主、`CLAUDE.md`、`项目任务.md`、任务线正本、其它 `资料/*.md`（本报告除外）。

---

## typecheck 结果

```
TMPDIR=/tmp/wf_helpers bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 跑了 **2 次**（第一次落盘后、修完一处 doc 注释边界后各一次），两次都是 0 / 0。
- ⚠️ 过程记录：第一次编辑 `SelfCheckTextMetrics.cs` 时，锚点吃掉了下一段 doc 的 `<summary>` 首行 ——
  已在同一文件内就地补回（当前 diff 为纯新增、0 删除），typecheck 复跑通过。
- ⛔ 未跑任何 `*.Run` / Unity 批处理 / 截图 / `wf_click.py`（铁律 12 现行口径）。
