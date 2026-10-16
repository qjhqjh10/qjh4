# T09 · `AttackSelector`（原版 `Drag Attack Selector`）迁 uGUI（执行代理 A3）

> 写手：并行执行子代理 `A3`（白名单 = `Battle/AttackSelector.cs` + 本报告）。日期 2026-10-16。
> 判据 = `任务/T09_整层换uGUI.md` §五·补⑨/⑩ · §八·3/4 · `普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 · `T09b_uGUI拦路判据.md` §A·2#5/§A·3。
> 范例（同批同组，代码已落地）= `Battle/SkillPanel.cs` 的 `UseHost` / `UguiMode` / `CanvasRect` 那一套。
> ⚠️ 行号 = 本报告落盘那一刻的现读值；文件再动一句就会漂 ⇒ **核对时按符号名 grep**。

---

## ① 结论

1. **迁完了**（代码）：`AttackSelector` 现在**双模式** —— 宿主 `RectTransform` 由**驱动层**通过
   `UseHost(host)` 登记，非 `null` ⇒ **画布档**（图 = uGUI `Image`、字 = `Label` 的 uGUI 后端）；
   **没人登记 ⇒ 世界档，与改之前逐位相同**。
   `TMPDIR=/tmp/wf_a3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
2. 🔴 **本件【不建自己的画布】、也【不挑画布】**（口径待主对话裁，两种站得住的读法见 §③·1）——
   这一点是**照着主对话 2026-10-16 的追加口径**落的：原版那个节点是 `rm 2` + `overrideSorting = false`
   + 排序层 `Default`（**实读** `Canvas_2759`：`m_OverrideSorting False` / `m_SortingLayerID 0` /
   `m_SortingOrder 0` / `m_PlaneDistance 100.0`）⇒ 它**不另立排序组**、随父画布按**兄弟序**渲染。
   ⇒ 宿主由外面喂进来（先例 `Tooltip.UseHost` / `ErrorMessageBanner.Create(..., uguiHost)` /
   同批同组的 `SkillPanel.UseHost`）。
3. **「最低兄弟序」我实现了**（这一半没有歧义）：本件进宿主容器时拿 **`siblingIndex = 0`**
   —— 靠 `HudCanvas.Add(_frame, ZFrameBottom)`，`ZFrameBottom = 100f` 是**哨兵**（`HudCanvas.Group.Add`
   按 z **降序**排兄弟序 ⇒ z 比同容器任何件都大 = 排最前 = 画在最底下；宿主画布里那些件的 z 都在
   −2.3…0.6 这一带）。原版判据 = 它是 `Safe area FrontCanvas` 的 `m_Children[0]`（兄弟序 **0/14**）。
4. **命中（本件有：拖拽起手）两档都对**：`UpdatePointer` 改成**世界点比世界点**
   （`_icons[i].transform.position` − 喂进来的 `world`），半径按档取（世界档 = `W(ButtonSize)/2×Scale`；
   画布档 = **画布单位→世界单位**的系数 `_frame.lossyScale` × 半径）。
   ⛔ 没有出现「命中框大 108 倍」那个坑：**一次都没有拿 px 去比世界点**（见 §④·3 的四个换口）。
5. ⚠️ **画布档现在【没有生效】**：没人调 `UseHost` ⇒ 跑起来仍是世界档（= 与改之前逐位相同）。
   要让本件真的走画布档，**必须先落 §③·1 的裁定 + §③·2 那三行接线**。
   这是**有意的安全中间态**（口径未定 ⇒ 不动画面；⛔ 不是「半迁移」——半迁移态是「一半件在画布、
   一半在世界」，本件两档各自**整套**成立，见 `UseHost` 的 doc）。

---

## ② 改动清单（`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/AttackSelector.cs`，**只有一个文件**）

> ⚠️ 行号现读（`grep -n <符号名>` 可复现）。文件是 **CRLF**（改完仍 1026 CRLF / 1026 LF —— 全 CRLF）。

| 处（现读行号） | 一句话 |
|---|---|
| `:1-33` 文件头 | 新增一段：本件的 uGUI 口径（原版对应物 / 兄弟序 0/14 / `overrideSorting=false` / 双模式 / 命中的量纲坑四处） |
| `:105-111` | 新常量 `ButtonRowDownPx = BarH*0.5f − BgDropY`（42.7485）—— 原来那个 `W(BarH*0.5 − BgDropY)` 的**具名化**（值不变）；附「这是我们自己的摆法、不是原版」的如实标注 |
| `:151-160` `BgSizePx` | 画布档改读 **`HudCanvas.SizePx`（= `sizeDelta`）**；⛔ 不走「世界尺寸 ×108」（那条在批处理下偏 0.75 倍） |
| `:180-186` | 新常量 `ZFrameBottom = 100f`（哨兵：帧在宿主容器里排最前 ⇒ **最低兄弟序**） |
| `:330-338` `EnterOffset` | 单位口从 `W(…)*Scale` 换成 **`US(…)`**（世界档 = 原式逐位不变；画布档 = 设计 px、**不乘 `Scale`**） |
| `:365-370` | 字段类型 `ImageQuad[] _icons/_rings`、`ImageQuad _background` → **`HudPiece`**（适配层；`Label` 不动） |
| `:388-440` | 新成员：`FrameName` / `_host` / `_frame` / `UguiMode` / `CanvasRect` / `FrameRect` / **`UseHost(host)`** |
| `:442-459` `Rebuild()` | 换档：拆旧建新（保留「正在显示」这一档状态；`Show` 会重放一次入场动画） |
| `:461-483` `ClearPieces()` | 只销毁**本件自己**的节点（含帧）—— ⛔ 不碰共享的宿主画布 |
| `:489-499` `U/US/Z` | **三个单位换算口**（双模式的全部机关；世界档分别退化成 `W` / `W*Scale` / 原值） |
| `:523-540` `Build()` | 分档：`_host != null` ⇒ `BuildOnCanvas`，否则 `BuildInWorld`；末尾照旧 `Hide()` |
| `:544-600` `BuildOnCanvas(host)`（新） | 帧 = 原版 GO 名 `Drag Attack Selector`、rect **照 `RT_3166`**（中心锚 + 中心枢轴 + `sd 621.7×122.2`）、`HudCanvas.Add(_frame, ZFrameBottom)`；底板/三颗钮/黄圈/角标一律 `HudCanvas.MakeImageInParent` / `MakeText`（**设计 px**） |
| `:602-647` `BuildInWorld()` | **就是原来那段 `Build()` 的建件部分，一句没改**（`ImageQuad.Create` + `Label.Create`，逐位相同） |
| `:649-653` `AspectOf(tex)`（新） | 画布档自己算「按高定、宽由贴图比例来」（**用不了** `HudCanvas.QuadFromTopPx` —— 它的 x/y 是屏幕绝对 px） |
| `:741-759` `ApplyOptionIcons` | 换图时画布档**补一句宽**（`Image` 换 sprite 不会动 `sizeDelta`；世界档的 `ImageQuad.SetTexture` 会重算宽高比） |
| `:785-874` `RefreshLayout` | 原点按档取（画布档 = 帧摆到锚点上、件相对帧中心；世界档 = `_anchorAt`），**下面共用同一串式子**（过 `U`/`US`/`Z`） |
| `:877-886` `AnchorLocal()`（新） | 世界锚点 → **帧的局部坐标**：走 `_frame.parent.InverseTransformPoint`（⛔ 不用 `ToDesignPixel`：它的 y 写死 108） |
| `:899-940` `UpdatePointer` | 半径按档 + 判据改成**世界点**（`transform.position`）；只比 x/y（照旧） |
| `:964-976` `ContainsBar` | **只加注释、判据一字未动**（比的全是世界量 ⇒ 两档都不用改）；顺手记一条老偏离 |
| `:991-999` `ButtonWorld` | 读回口从 `localPosition` 改成 **`position`**（世界点；世界档下与原来逐位相同） |

---

## ③ 接线清单（**白名单外，⛔ 我没动**）

### ③·1 🔴 待裁：本件并进哪一块画布（**两种读法都站得住，我不自选**）

**资源层事实**（现读）：原版 `Drag Attack Selector` 这个 GO 身上**确实有一个 `Canvas` 组件**
（`Canvas_2759.json`：`m_RenderMode 2` · `m_OverrideSorting **False**` · `m_SortingLayerID **0**`（`Default`）·
`m_SortingOrder 0` · `m_PlaneDistance 100.0`），而它的父链是 `Safe area FrontCanvas`（`RT_3055`）→
`FrontCanvas`（`Canvas_2762`：`rm 1` / `overrideSorting **true**` / `OverHand` / 0）。
⇒ 它**随父画布按兄弟序渲染**（`T09b` §A·0·3），是该组**最底下一件**（`m_Children[0]`）。

| 读法 | 落地（接线一行） | 代价 / 风险 |
|---|---|---|
| **A（技术口径）** 并进该组**最底下那块既有画布** = `SubCanvasRank` 的 `OverHand` 段第一块（现读 = **`"Mulligan Canvas"`**） | `selector.UseHost(HudCanvas.EnsureSubCanvas(null, "Mulligan Canvas", HudCanvas.SortingLayer.OverHand, 0))` | ① **耦合**：两块无关的件共用一块排序组（画面上等价 —— 组内靠兄弟序）；`grep` 过 5 块 `OverHand` 画布，**没有任何一处**对子画布 `SetActive(false)`/`Destroy` ⇒ 今天不触发，但将来谁改 `MulliganPanel` 的启停就会连带本件 ② 名字上不好读（「选择器挂在换牌画布里」） |
| **B（结构口径）** 给本件**单独建一块** `"Drag Attack Selector Canvas"` | `selector.UseHost(HudCanvas.EnsureSubCanvas(null, "Drag Attack Selector Canvas", HudCanvas.SortingLayer.OverHand, 0))` ＋ **`HudCanvas.SubCanvasRank` 的 `OverHand` 段最前面加一条**（见 ③·3） | 与 `T09` §八·3 的字面「⛔ 不是另起一块画布」冲突；**但**「它身上没有画布组件」这句在**资源层不准确**（`CV_2759` 就在它身上，只是 `overrideSorting=false`） |

🔑 **两种读法渲染结果相同**（都是 `OverHand`/0、组内最底）—— 差别只在**排序组怎么组织**与**耦合面**。
⇒ **`AttackSelector.cs` 对两种都成立**（它不建画布、宿主后喂）；裁定落地 = 改 `③·2` 那一行的**画布名**。
⚠️ 若裁 **B**：本件进它那块画布后同样拿 `siblingIndex = 0`（`ZFrameBottom` 哨兵，与宿主无关）。

### ③·2 `BattleDriver.cs` —— **宿主传递**（现读：`BuildHud()` 起于 `:13874`，收尾在 `:15600` 附近、下一个方法是 `PlaceDeckLights` `:15607`）

在 `BuildHud()` 的收尾（HUD 建完之后）加三行：

```csharp
// 🔴 `T09` 批 3：攻击方式选择器。本件**不建画布**（原版那节点 overrideSorting=false ⇒ 不另立排序组）
//   ⇒ 宿主 = 【③·1 裁定】那一块既有画布；它进宿主容器后 siblingIndex = 0（原版兄弟序 0/14）。
var selHost = HudCanvas.EnsureSubCanvas(null, "Mulligan Canvas",      // ← 换名字即换读法（A/B）
                                       HudCanvas.SortingLayer.OverHand, 0);
if (selector != null)
{
    if (selHost != null) selector.UseHost(selHost);
    else Debug.LogError("[Battle] 攻击方式选择器：宿主画布没建起来 ⇒ 它留在世界档");
}
```

- ⚠️ **次序不敏感**：登记排在第一次 `Build()` 之后也行 —— `UseHost` 会拆旧建新（**打一条 `Debug.Log`**）。
  实际上**总会走一次重建**：`Awake()` 里那次 `Build()` 一定早于驱动层（`Awake` < 所有 `Start`）⇒
  那一趟建的是世界档、紧接着被拆掉。**没有画面影响**（那一趟的件全是隐藏的，`Build()` 末尾就 `Hide()`）。
- ⚠️ `EnsureSubCanvas` **按名字幂等** ⇒ 若 `MulliganPanel.Create` 还没跑，这一行会**先把块建出来**
  （它之后会复用同一块）—— 这是既有行为（`EnsureSubCanvas` 的 doc 明说可以当「建/取」用）。

#### 注册表 / `ReanchorHud` —— 🔴 **一件都不用摘**

选择器**不在**那 4 个注册表里（`_hudImages` / `_hudImageSpots` / `_hudExtras` / `_playedPips`）——
它是**独立节点**（`BattleDriver.cs:57` 的 `public AttackSelector selector;`，节点由
`Editor/BattleScene.cs:23817-23819` 建）。
🔴 **`ReanchorHud` 里那一句 `if (selector != null) selector.RefreshLayout();`（现读 `BattleDriver.cs:16217`）
要留着** —— 画布档的帧位置是 `InverseTransformPoint` 从**世界锚点**算的，换分辨率 / 换画布变换之后
**必须重排**（不是「迁走就不用管了」）。

### ③·3 `HudCanvas.cs` 的 `SubCanvasRank`

- **读法 A**：🔴 **不用加、也不用并任何一条**（本件不建画布）。
- **读法 B**：在 `// ── OverHand` 那段注释之后、`"Mulligan Canvas"` **之前**插一条：

```csharp
"Drag Attack Selector Canvas",      // 0/14（原版 `Safe area FrontCanvas` 的 m_Children[0]）
```

  出处：`RectTransform/RectTransform_3055.json` 的 `m_Children[0]` · `T09b` §A·3 · `T09c` §一
  （原版兄弟序 **0/14**、`Default`/0、`overrideSorting=false`）。

### ③·4 `Editor/BattleScene.cs` —— 命中相关（含必须现改的过期文案）

| 处（现读） | 要做什么 |
|---|---|
| `:20931-20938` | 🔴 **文案要改（铁律 5）**：那段前提注释写着「`ButtonWorld(k)` 回的也是 `localPosition`」「`UpdatePointer` 也算 `localPosition`」—— **现在两处都读世界点 `transform.position`**。判据仍在，但理由换成：「`UpdatePointer` 是拿**世界点**比**世界点**，两档同义」。⚠️ `:20941` 那条 `sel964.transform.position == Vector3.zero` **可以留着**（仍为真、且它同时是「选择器根没被搬走」的守卫），只是**不再是命中的前提** |
| `:20968` | 同一句话在断言文案里也有一份（「`ButtonWorld` 回的是 `localPosition`，`UpdatePointer` 也算 `localPosition` ⇒ 往返本身是构造性的」）⇒ 一并订正 |
| `:5261-5320`（入场动画那一段） | **不用改**（`p0`/`pMid`/`p1` 只彼此相差，画布档下入境外扩 = 45 设计 px ≈ 0.31 世界单位 > 0.01） |
| `:5633-5635`（`BgSizePx` 那两条） | **不用改**：画布档读 `sizeDelta` ⇒ 恰好 514.8×125.4（原版值），世界档原样 |
| `:402-407`（那三处显式 `Build()`） | 注释里补一句：`AttackSelector.Build()` 现在**先建世界档**（宿主还没登记），驱动层登记后由 `UseHost` 拆旧建新 ⇒ **那一句 `driver.selector.Build()` 要留着**（自检里没有 `Awake`，它是节点诞生的唯一入口） |

**建议新增的断言**（画布档专用；⛔ 都由主对话在同步点决定要不要落）：

```csharp
// ① 档位 + 宿主（判据只此一处的两个口）
Check(sel.UguiMode && sel.CanvasRect != null, "★ T09/A3：攻击选择器已迁到画布档");
var sc = sel.CanvasRect.GetComponent<Canvas>();
Check(sc != null && sc.overrideSorting && sc.sortingLayerID == HudCanvas.SortingLayer.OverHand
      && sc.sortingOrder == 0, "★ 宿主画布 = OverHand / order 0");
// ② 【最低兄弟序】—— 原版 m_Children[0]，这是它的机械证明
Check(sel.FrameRect.GetSiblingIndex() == 0, "★ 帧在宿主容器里排最前（= 画在最底下，原版兄弟序 0/14）");
// ③ 帧的 rect 照原版 RT_3166（621.7249755859375 × 122.24700164794922 ⇒ 我们常量 621.7/122.2）
var fs = HudCanvas.SizePx(sel.FrameRect);
Check(Mathf.Abs(fs.x - 621.7f) < 0.5f && Mathf.Abs(fs.y - 122.2f) < 0.5f, $"★ 帧 rect {fs.x}×{fs.y}");
// ④ 🔴 【灭自证】画布档下**一件世界空间 quad 都不该剩**（写法照 A218 那条 CardDisplayWindow）
Check(sel.FrameRect.GetComponentsInChildren<ImageQuad>(true).Length == 0,
      "★ T09/A3（机械证明）：画布档下帧里一件世界空间 ImageQuad 都不剩｜🧨 漏迁一件 ⇒ 红");
// ⑤ 【锚点投影】把 `InverseTransformPoint` 那一跳钉住（⛔ 别拿 AnchorLocal 反算回来比 —— 那是同义反复）
Check(Mathf.Abs(sel.FrameRect.position.x - sel.AnchorWorld.x) < 1e-3f
      && Mathf.Abs(sel.FrameRect.position.y - sel.AnchorWorld.y) < 1e-3f,
      "★ 帧落在锚点的正投影上（x/y 与 `AnchorWorld` 同值；z 是画布平面）");
// ⑥ 【双模式往返】世界档那一半：另起一个探针实例（别动战斗里的那一个）
var pgo = new GameObject("A3Probe"); var probe = pgo.AddComponent<AttackSelector>();
probe.Build();  Check(!probe.UguiMode && pgo.transform.GetComponentsInChildren<ImageQuad>(true).Length > 0,
                      "★ 没登记宿主 ⇒ 世界档（与改之前逐位相同）");
probe.UseHost(sel.CanvasRect);  Check(probe.UguiMode, "★ 登记宿主 ⇒ 画布档");
Object.DestroyImmediate(probe.FrameRect.gameObject); Object.DestroyImmediate(pgo);   // ⚠️ 帧是画布上的节点、不随父销毁
```

---

## ④ 判据与出处（逐条，⛔ 没有一条是猜的）

1. **原版对应物 / 位置 / 排序**：GO `Drag Attack Selector` @ `Canvas < FrontCanvas < Safe area FrontCanvas`，
   兄弟序 **0/14** · `Default`(0) / order 0 · `overrideSorting = false`
   —— 现读 `bundle_scenes_scenes_battlearena2/Canvas/Canvas_2759.json`（`m_OverrideSorting False`、
   `m_SortingLayerID 0`、`m_SortingOrder 0`、`m_PlaneDistance 100.0`）、
   `RectTransform/RectTransform_3166.json`（`m_Father 3055` =
   `Safe area FrontCanvas`）、`T09b` §A·2#5 / §A·3 · `T09c` §一 那一行。
2. **原版那一级的 rect（逐值，供「固定 px vs 锚点拉伸」那一条裁定核）**：
   `RectTransform_3166` ⇒ `m_AnchorMin = m_AnchorMax = (0.5, 0.5)` · `m_Pivot (0.5, 0.5)` ·
   `m_AnchoredPosition (0, 0)` · `m_SizeDelta (621.7249755859375, 122.24700164794922)` ·
   `m_LocalScale (1,1,1)`；子件 4 个 = `Select Attack Background`(`RT_3254`：`ap (0,−18.375)` ·
   `sd (780,285)` · **pivot `(0.5,1)`** · `scale 0.44`)、`Select Melee Button`(`RT_3563`) ·
   `Select Active Skill Button`(`RT_2819`) · `Select Range Button`(`RT_2997`)
   —— 三颗钮都是 `ap (0,0)` · `sd (118.372, 118.806)` · pivot 居中 · scale 1。
   ⇒ 🔴 **这一级【不是】锚点拉伸**（中心锚 + 固定 sizeDelta）⇒ 本件**不受**「面板在 4:3 下只有原版 75%」
   那条待裁口径的影响（`④·2` 的值就是判据）。
3. **命中的量纲（本件最该防的坑）**：`T09` §五·补⑥ 末（「旧 quad 的局部单位 = 世界单位 · uGUI = 设计 px，
   差 108」）+ §五·补④ 最后两行（带命中的件要么走 `GetWorldCorners` 系、要么等输入那一步）。
   本件里**四处**（逐处都按档换过）：`RefreshLayout` 摆位（`U`/`US`）· `EnterOffset`（`US`）·
   `UpdatePointer` 的圆半径（`_frame.lossyScale`）· `ButtonWorld` 读回（`position`）。
   🔑 半径算法：**画布单位→世界单位**的系数 = `_frame.lossyScale`（它就是「1 画布单位 = 多少世界单位」，
   批处理下两轴不同：x 1/108 · y 1/144 ⇒ 取 **min**，保守方向；真机两轴相等 ⇒ 与旧值逐位一致）。
4. **画布单位 ↔ 世界单位**：`T09` §五·补③ 一（16:9 下 1 单位 = 1 设计 px = 1/108 世界单位）+
   §七·8（批处理下**两轴系数不同**、`Screen 640×480` 而 `cam.aspect` 钉 16:9 ⇒ x 1/108 · y 1/144
   ⇒ 「世界尺寸 ÷1/108 折 px」的读回偏 0.75 倍）。
   ⇒ 本件**锚点换算一律走 `InverseTransformPoint`**（只有画布自己的变换是精确的）、**尺寸读回一律读
   `sizeDelta`**（`HudCanvas.SizePx`）—— 两条都按 §七·8 的正解。
5. **双模式的先例**：`Core/Tooltip.cs` 的 `UseHost(host)` · `Battle/ErrorMessageBanner.cs:430` 的
   `Create(..., uguiHost)` · 同批同组的 `Battle/SkillPanel.cs`（`UseHost`/`UguiMode`/`CanvasRect`，
   且它也用 `_cv.InverseTransformPoint(world)` 与 `_cv.lossyScale` —— **两条独立收敛**）。
6. **⛔ 别推翻的那条**：攻击方式选择器 = **横排 + 扇开**（原版只有 3 个实例）—— `战斗复刻_正本.md` §七 #7。
   本件**只换渲染层与单位**：横排顺序（`Order`）、居中算法、1.3 倍 + 黄圈挂「已选打法」、
   入场动画（累计视口拖拽位移驱动）**一行没动**。
7. **锚点来源不许动**：`BattleDriver` 喂的是 `playerBoard.DropTargetWorld(slot)`（`A` 账，见 `任务总表` T03 那行）
   ⇒ 本件照收，`_anchorAt` 的语义与出处没变。

---

## ⑤ 没查清 / 没做的（⛔ 不许猜的都写在这儿）

1. 🔴 **「并进哪一块画布」未裁** —— 两种读法都站得住（§③·1），**我不自选**（主对话 2026-10-16 明示）。
   ⇒ 现状 = 世界档（画面与改之前逐位相同），**本件迁 uGUI 的效果尚未生效**。
2. ✅ **关于 A2 报的那条「固定设计 px vs 原版锚点拉伸」——本件这一级【不是 stretch】、不受影响**（值照原样给全）：

   | 项 | 原版 `Drag Attack Selector`（`RT_3166`） |
   |---|---|
   | `m_AnchorMin` / `m_AnchorMax` | `(0.5, 0.5)` / `(0.5, 0.5)`（中心锚） |
   | `m_Pivot` | `(0.5, 0.5)` |
   | `m_AnchoredPosition` | `(0, 0)` |
   | `m_SizeDelta` | `(621.7249755859375, 122.24700164794922)` |
   | `m_LocalScale` | `(1, 1, 1)` |
   | 子件 | `Select Attack Background`(`RT_3254`：`ap (0,−18.375)` · `sd (780,285)` · pivot `(0.5,1)` · `scale 0.44`) · `Select Melee Button`(`RT_3563`) · `Select Active Skill Button`(`RT_2819`) · `Select Range Button`(`RT_2997`) —— 三颗钮都 `ap (0,0)` · `sd (118.372, 118.806)` · pivot 居中 · `scale 1` |

   ⇒ **非 16:9 下的差值 = 0**：4:3（1440×1080）下 `CanvasScaler(Expand)` 的 `scaleFactor = 0.75`，
   原版那一级与我们的画布档帧**都**按 0.75 缩 ⇒ 画出来都是 466.29 × 91.69 屏 px（**逐位一致**）。
   ⚠️ 对照：**旧的世界档**在 4:3 下画的是 621.7 × 122.2 屏 px（`W(px)=px/108`，跟宽高比无关）
   ⇒ 比原版**大 1.333 倍** —— 那正是本件要修的那条，迁到画布档之后**自动修掉**。
   ⇒ **本件对那条待裁口径没有输入、也不需要跟着改**（这一级没有 stretch 可言）。

3. ⚠️ **没跑自检、没起 player**（现行口径「先复刻后检查」）⇒ 画布档的**画面**只有「逐值推导 + §③·4 的
   建议断言」保证，**没有实机验证**。具体没验的：① 帧落在锚点上（`InverseTransformPoint` 那一跳）；
   ② 三颗钮在非 16:9 下的相对间距（设计 px ⇒ 跟画布缩放）；③ 字（`Label` uGUI 后端）与图的相对大小。
4. ⚠️ **画布档的「字」大小**：`Label` 在 uGUI 档一个 `scale` 档 = **7 px**，世界档 = `7/100` 世界单位
   = 1080p 下 **7.56 px**（`Label.cs:287` 的 `CapHeightWorldOfScale` 与 `:294` 的 `CapPxOfScale`）
   ⇒ **16:9 下 uGUI 档的字比世界档小 ~7.4%**。这是 `Label` 加后端时**主对话已定**的口径
   （`T09` §五·补⑨ 四），**不是我改的**、也**不在本件白名单** ⇒ 如实记，⛔ 我没动。
5. ⚠️ **画布档的圆半径取两轴的 min**：批处理下画布两轴系数不同（1/108 vs 1/144），
   本件取 min ⇒ 批处理自检里命中圈比「画出来的那圈」略小（真机两轴相等 ⇒ 精确一致）。
   ⇒ 这是一处**如实标注的近似**，要逐位得让命中圈按轴分别取系数（⛔ 本件没做：那会与「原版是一个圆」
   这个语义打架，且真机上两者等价）。
6. ⚠️ **`raycastTarget` 仍是 `false`**：画布档下三颗钮是 `Image` 但**不吃 uGUI 射线**
   （`HudCanvas.MakeImageInParent` 出厂就 `false`）—— 与 `T09` §八·11（那两扇的钮也是 false、原版是 1）
   **同一笔账**：**第 4 步（输入）要一起翻**。在那一批落地之前，本件的命中仍走手搓那一路（现状）。
7. ⚠️ **`Highlight` 的父链与原版不同**（见 §⑥·2）—— 画出来同一处，**没改**。
8. ⚠️ **`BarW`/`BarH` 与原版差 0.025 / 0.047 px**（见 §⑥·3）—— **没改**（改它要动 `ButtonRowDownPx` 等一串）。
9. ⚠️ **顺手发现但本件没动的两条老偏离**（§⑥·1 的按钮行低 24.37 px · §⑥·4 的 `ContainsBar` 域）
   —— 都**不属于**「只换渲染层与单位」⇒ 按铁律 11 **记着要做**，别当已复刻。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **我们那三颗钮比原版低 24.37 px**（**迁移前就如此**）：以**底板顶端**为参照，
   原版三颗钮的中心在底板顶**下方 18.375 px**（`Select Attack Background` 的 `ap (0,−18.375)` + pivot `(0.5,1)`，
   而三颗钮的 `ap` 是 `(0,0)` —— `RT_3254` / `RT_3563` / `RT_2819` / `RT_2997` 现读）；
   我们在底板顶下方 **42.7485 px**（`ButtonRowDownPx`）⇒ **低 24.37 px**。
   同一件事的另一种说法：原版那个节点的中心 = 「三颗钮那一行」，而**我们**把节点中心放在了
   「底板中心往下 42.7485」的位置。⇒ **要做**（铁律 11），但**要连 `center` / `BgDropY` 那一串一起重算**，
   ⛔ 本件「只换渲染层与单位」不动它。
2. ⚠️ **原版 `Highlight` 是**按钮的子对象（`Select Melee Button/Highlight`，`RT_3563` 的 children 里有），
   我们是**平铺的兄弟件**（`_rings[i]` 与 `_icons[i]` 同级）⇒ 画出来同一处、缩放也是各调各的
   （`RefreshPicked` 里两边都写了 `s`），差别只在父链。**没改**（改它 = 动层级结构，超出本件范围）。
3. ⚠️ **`BarW`/`BarH` 比原版短 0.025 / 0.047 px**：我们 `621.7 / 122.2`，原版
   `621.7249755859375 / 122.24700164794922`（`RT_3166`）。本件把我们的常量写进了帧的 `sizeDelta`
   （子件全是中心锚 ⇒ **画面上一分钱不差**）⇒ 只影响「帧 rect」那一条断言的容差（建议容差 0.5 px）。
4. 🔴 **`ContainsBar` 的盒子以【世界原点（屏心）】为中心，而条子挂在【被拖单位】上**（2026-09-29 起）
   —— 只有单位恰好在屏心时两者才重合 ⇒ 「点槽外 = 取消」的判定域**在单位偏在一边时是错的**
   （`BattleDriver.cs:8592` 用它）。
   ⚠️ 它**与本件无关**（比的全是世界量 ⇒ 两档同病），而且改它是**改输入行为** ⇒
   ⛔ 本件**只加注释、没动判据**（`AttackSelector.cs:964-976`）。判据留档：`_anchorAt` 是 2026-09-29 起
   由 `BattleDriver` 喂 `playerBoard.DropTargetWorld(slot)`（账 → `任务总表` T03 那行）。
5. ⚠️ **同批写手在 `SkillPanel.cs` 里用了一套一模一样的 `UseHost`/`UguiMode`/`CanvasRect`/`_cv.lossyScale`/
   `_cv.InverseTransformPoint`**（我是独立写的，两条收敛）⇒ 建议主对话在同步点核一下**两件的命名与语义
   是否逐字一致**（`T09` 一句话原则：同一件事别长出两个名字）。
   ℹ️ `SkillPanel` **有**自己的子画布（`"ActiveSkillDesc Canvas"`），原因是原版 `ActiveSkillDesc`
   **根本没有 Canvas 组件**（继承 `FrontCanvas`）—— 与本件（有 Canvas、但 `overrideSorting=false`）
   **不是同一档**；这一处差异值得在裁定 §③·1 时一并看。
6. ⚠️ `Editor/BattleScene.cs:20941` 那条前提断言（「选择器根在世界原点」）**仍然为真**，
   但它想守的那件事（`UpdatePointer` 拿 `localPosition` 相减）**已经不成立了** ⇒ 文案过期（见 §③·4）。

---

## 可直接粘进总表的一行

`T09` 批 3 · `AttackSelector`（原版 `Drag Attack Selector`）：代码已迁 uGUI、**双模式**（宿主 `UseHost` 后喂 ⇒ ⛔ 本件**不建画布、不挑画布**；没人登记 ⇒ 世界档与改之前逐位相同）· 命中两档都对（半径走 `_frame.lossyScale`、判据改世界点）· 帧进宿主容器拿 **`siblingIndex = 0`**（原版兄弟序 0/14）· `typecheck` 0 错 · ⚠️ **未跑自检/未起 player**（现行口径）。
🔴 **待主对话裁两条**：① **并进哪一块画布**（读法 A = 既有 `"Mulligan Canvas"` sibIndex 0 / 读法 B = 单独建 `"Drag Attack Selector Canvas"` + `SubCanvasRank` 的 `OverHand` 段最前一条，两种渲染结果相同、代码都成立，⛔ 我不自选）② 顺手查出**三颗钮比原版低 24.37 px**（老偏离，**要做**、不在本件范围）。
⏭ 裁定后落 `BattleDriver.BuildHud()` 收尾那三行宿主传递（`ReanchorHud` 那句 `selector.RefreshLayout()` **留着**、4 个注册表**不用摘**）+ `BattleScene.cs` 的过期文案与 6 条新断言（详见 `资料/普查产出_1016并行/T09_AttackSelector_报告.md`）。
