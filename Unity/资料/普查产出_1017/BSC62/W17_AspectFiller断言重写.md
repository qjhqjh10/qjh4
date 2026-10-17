# W17 · AspectFiller 断言按 uGUI 重写

位置：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` §8b 2a-①（改后 `:15250-15357`）。
只动这一段的断言；`BattleDriver.BuildOneAspectFiller` / `HudCanvas` / `HudPiece` 一个字没改。

## 一、逐条：原断言 → 新断言 → 为什么（判据）

| # | 原断言 | 新断言 | 判据 |
|---|---|---|---|
| 查找口 | `drv.hudRoot.Find(n).GetComponent<ImageQuad>()` | `HudCanvas.RootRect.Find(n)` → `HudPiece.Of(rt, Image)` | 根画布是 `new GameObject("BattleHud Canvas")`、**无 `SetParent`** ⇒ 根对象；`Transform.Find` 不递归 ⇒ 旧口恒 null。先例 = 同文件 `EW8`（`CheckHudBox`，约 `:500`） |
| 1 | 两枚 quad 都建出来了 | 两枚**件**都建出来了（仍为前提，失败时下面 6 条等于没验） | 原版 `Aspect Ratio Filler Top` / `… Bottom`，13/13 战场都有 |
| 2（新） | — | 两枚 `HudPiece.IsUgui == true` | `W15` 的迁移档位；原版是 `BattleHud/Canvas` 直接子件（身上无 Canvas/CanvasGroup）。改回世界档 `ImageQuad` ⇒ 这条红 |
| 3 | `WorldW × 108` 比 3252.123…×1842.020… | **`Rect.sizeDelta`** 直接比 3252.123046875 × 1842.02001953125（容差 1e-3 **设计 px**） | uGUI 档一个 canvas 单位 = 一个设计 px（`HudCanvas` 文件头 / `MakeAnchored` 原样写入）；原版 `RectTransform_{3021,2916}.json` 的 `sizeDelta` 未取整 |
| 4（新） | — | 两枚 `Image.preserveAspect == false` | 原版 `m_Sprite = null`、`m_Type = 0` + **`m_PreserveAspect = 0`** ⇒ 拉满矩形（打开会按 1×1 白图比例内接成正方形） |
| 5（新，前提） | — | 两枚 `pivot == (0.5,0.5)` | `HudCanvas.CenterPx` 读的是**枢轴点**（那件 doc：非居中 pivot 会读偏）；pivot=中心时读到的才是矩形中心。原版这两颗 anchor/pivot 都是 (0.5,0.5) |
| 6 | `LayoutSpace.ToPixel(transform.position)` 比 `(960.0007,−1101.1)` / `(960,2181.1)` | **`HudCanvas.CenterPx(rt)`** 比 `960 + ap.x` / `540 − ap.y` | 原版 `anchoredPosition`：上 `0.0006713899783790112 / 1641.0999755859375`、下 `-3.051800013054162e-05 / -1641.0999755859375`（y 向上、相对 Canvas 中心） |
| 7 | `Tint` = (0,0,0,1) | 不变（uGUI 支 `HudPiece.Tint` 转发 `Image.color`） | 原版 `m_Color (0,0,0,1)` |
| 8 | `RenderQueue == 2999` **且** `localPosition.z == +5` | **兄弟序 = 0 / 1 且父 = `HudCanvas.RootRect`** | 原版 = `BattleHud/Canvas` 的 `m_Children[0]/[1]`（画在所有 UI 之下）。uGUI 里**序小 = 画在底下**。见 §二 |
| 9 | `activeSelf` 两枚都 true | 不变（`HudPiece.gameObject.activeSelf`） | 原版 `m_IsActive` 13/13 都是 true |
| 10 | 上缘几何 −180.09 | 不变（半高改由 `sizeDelta.y × 0.5` 算） | 540 − 1842.020…/2 = −180.0899658203125 |
| 11 | 下缘几何 1260.09 | 同上 | 540 + 1842.020…/2 = 1260.0899658203125 |
| 12 | 「身上没有 `Collider`」 | **`Image.raycastTarget == false`** | 原版 **`m_RaycastTarget = 0`**。uGUI 档有真射线系统 ⇒ 直接读原字段（旧那半条是世界档的替代判别） |

合计：原 9 条（含查找口一行）→ 新 **12 条**。其中 3 条是新增（档位 / `preserveAspect` / pivot 前提）、
1 条换了判据（`Collider` → `raycastTarget`）、1 条整体替换（队列+z → 兄弟序）。

## 二、哪几条在 uGUI 档不适用（如实说 + 替代判别）

1. **`RenderQueue == 2999`** —— 不适用。uGUI 件走 Canvas 排序，**`HudPiece` 根本没有 `RenderQueue` 口**；
   原版那颗件的真值是「层序 = `m_Children[0]/[1]`」而不是某个队列号。
   替代判别 = **同一父节点内 `GetSiblingIndex()` = 0 / 1**（同一父 = `HudCanvas.RootRect`）。
2. **`z = +5`** —— 不适用。uGUI 下 `localPosition.z` 不参与排序。`HudPiece.OrderZ` 在 uGUI 支返回的是
   `HudCanvas.OrderZOf` = **我们自己的 z 账（字面量 5）**，拿它断言就是「读实现给的值再比回去」= 自证 ⇒ **没断**。
   替代判别同上（兄弟序）。
3. **`Collider == null`** —— 不适用（uGUI 的指针判定不看 Collider，看 `GraphicRaycaster` + `Image.raycastTarget`）。
   替代判别 = `Image.raycastTarget == false`，**比原来更强**（直接是原版那个字段）。

## 三、灭自证：期望值取自哪一侧

全部取自**原版侧**，没有一条是从我们的常量/实现读回来再比：

- 尺寸 `3252.123046875 × 1842.02001953125`：原版 `RectTransform_{3021,2916}.json` 的 `sizeDelta` 字面量（沿用原断言的字面量，未改数）。
- 中心 `960/ap/540`：原版 `anchoredPosition` 字面量（上 `0.0006713899783790112 / 1641.0999755859375`、下 `-3.051800013054162e-05 / -1641.0999755859375`）。
  段内**没有**读 `BattleDriver.AspectFillerTopX/…Y` 这些常量。
- 兄弟序 `0 / 1`：原版 `m_Children[0]/[1]`。
- `preserveAspect` / `raycastTarget` / `activeSelf` / `m_Color`：原版 `m_PreserveAspect` / `m_RaycastTarget` / `m_IsActive` / `m_Color`。
- ⚠️ 两条内缘几何（10/11）在**尺寸 + 中心都过**的前提下是算术推论 —— 与改前同构（原来也是由 `WorldH` + `pc` 推），
  保留是为了把「为什么看不见」写成可读断言；它们**不单独提供鉴别力**（如实注明）。

## 四、类型检查结果

```
TMPDIR=/tmp/wf_w17 bash d:/4/Unity/工具/typecheck.sh
运行时错误数: 0
编辑器错误数: 0
```

行尾复核：`CRLF 25954 / LF 25954`（纯 CRLF 未翻）；`git diff --numstat` = 452/146
（该文件在本轮之前已有别人的改动，这 452/146 **不是**我这一段的行数）。

## 五、没能判的

1. **没跑 `BattleScene.Run`**（本轮口径明令不跑）⇒ 下面这些只是**静态推演**、未经实跑：
   - 兄弟序真的会是 **0 / 1**。推演链：`BuildAspectRatioFillers` 用 z = 5，本仓 HUD 件 z 最大只有 0.3
     （`HudImageZ`；窗口族都是负数）⇒ `HudCanvas.Group.Add` 按 z **降序**插 ⇒ 两枚落在组内 0/1；
     `ApplySubCanvasRank` 只把子画布排到 `childCount` 末尾、不动 0/1。**未实跑核。**
   - `CenterPx` 的实得值与 `960/ap` 是否逐位吻合（算式与 `MakeAnchored` 的入参对得上，但未运行）。
2. **原版 `RectTransform_3021/2916.json` 未亲自重读** —— 沿用了原断言里的字面量与 `BattleDriver` 文件头出处；
   本轮没有回解包资源复核这两个数（若裁定「必须现读」，需另开一步）。
3. **同名多件**：`RootRect.Find` 只回第一个；本件只有一份，未验重复建件的情形。
4. `HudPiece.Of(t as RectTransform, …)` 依赖节点是 `RectTransform`；`MakeAnchored` 建的必是，但若将来换造法要重核。

## 六、顺手发现（⛔ 没动手）

1. `BattleScene.cs:14736 / 14761 / 19066` 仍拿 `drv.hudRoot` 算**世界坐标期望值**（`want513` / `wantEx` / `want423`）——
   这几处是「反向补震根」的合法用法，**不是**查找口问题，本轮未动；若那些件也迁了档，量纲要单独核（**未查**）。
2. `HudPiece.SizePx` 的 doc 自己写明「拉伸锚件会读成 (0,0)」并点了四个调用点；本段直接用的 `Rect.sizeDelta`（点锚件），
   没有踩这个坑。但**同族其它断言**若改用 `HudPiece.SizePx` 去量那四件（`_shade` / `_dim`）会静默得 0 —— 记一笔。
3. `HudCanvas.CenterPx` 的名字（「Center」）与实现（读**枢轴点**）不一致，已有 doc 订正；本段靠「pivot = 0.5 前提」兜住。
   建议将来改名或加 `RectCenterPx`（**未做**，属 `HudCanvas.cs`，不在我的白名单）。
