# T09 · `AlliancePanelWindow`（原版 `Alliance Panel`，`OverHand` 13/14）就地翻 uGUI（执行代理 `A9`）

> 执行代理 `A9` · 2026-10-16 · 白名单 = `Shell/AlliancePanelWindow.cs` + 本报告。
> ⛔ 没跑 Unity（不跑 `*.Run`、不起 player）· ⛔ 没动 git · ⛔ 没改白名单外的文件（含 `Battle/BattleDriver.cs` ·
> `Editor/BattleScene.cs` · `Battle/HudCanvas.cs` —— 它们的改动**全部**在 §③ 接线清单里）。
> ✅ 类型检查 = `TMPDIR=/tmp/wf_a9 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完即跑，跑了两遍）。

---

## ① 结论

**做完了**：`Shell/AlliancePanelWindow.cs` **就地**翻成**双模式**（世界档逐位不变 + 新的 uGUI 画布档）。
画布档 = 原版 `Alliance Panel` 那一格同构的落点：子画布 `Alliance Panel Canvas` / `OverHand` / order 0；
根节点走**它自己的 rect**（815.044×475.470 @ 绝对 px (300.52, 179.80)，**不是** `FillParent`）；
**14 个可见件**（**7 张图** = 熄灯层 + `BG` + `BGFrame` 四块 + 头像框 · **7 颗字**）全部改由
`HudCanvas.{SetPxSize,PosPx,MakeImageInParent,MakeText}` 建，层深度由 **`ZOf(q) = −q`** 换算成兄弟序。

**没做完（三件，全部在 §③，都因为文件不在白名单）**：
① `BattleDriver.cs:15084` 那一句**还没换**（所以今天跑起来**仍是世界档** —— 这是**有意**的：迁移能力先做好、
由收口代理接线）；② `HudCanvas.SubCanvasRank` 还缺 `"Alliance Panel Canvas"` 那一条（**不补的话本件迁过去会与
`AboveShader`(8) 抢次序**）；③ `Editor/BattleScene.cs` 那族断言里有 **2 处吃世界单位**的要改（明细见 §③②）。
⇒ **本件今天对既有自检的影响 = 零**（世界档逐字未动，见 §② 那条「逐位不变」的机械证据）。

---

## ② 改动清单（`Unity/MyGame/Assets/CardPresentation/Shell/AlliancePanelWindow.cs`，416 → 629 行）

| 行 | 是什么 |
|---|---|
| `:53-82` | 文件头新增一整节「2026-10-16 已迁 uGUI（双模式）」：子画布 / 双模式口径 / **队列→兄弟序要取负**那条换算 / 命中那一格 / 一条如实记的口径分歧（见 §⑤·1）/ **⛔ 没顺手做的四件老缺口** |
| `:124-134` | 新增 `public const string SubCanvasName = "Alliance Panel Canvas"` + `static float ZOf(int q) { return -q; }`（**唯一一处**队列→z 换算，带判据） |
| `:228-236` | 新增 `bool _ugui` + `public bool OnCanvas`（自检用；同 `UnitChatPanel.OnCanvas` 先例） |
| `:249-293` | `Create` 加**第三个可选**形参 `Transform uguiHost = null`（**源兼容**：`Create(root)` 一字不改仍编译）—— 宿主非空⇒画布档；宿主不在 `Canvas` 下 ⇒ **出声 + 不建**（⛔ 不退回世界档）；根节点两档不同（世界档**裸 `Transform`**、画布档 `RectTransform`） |
| `:299-321` | ① 熄灯层：画布档走 `Img(...)`（**不建** `MenuDraw.ShadeHit` 那颗 `WindowButton`）；世界档原样 |
| `:323-330` | ② `Background`/`BG`/`BGFrame` 四块 → 走 `Node`/`Img` 两个双档助手（**名字、rect、父子嵌套、队列档全部逐值未动**） |
| `:332-342` | ③ 吸收层：**画布档不建**（同 ① 的理由），世界档原样 |
| `:344-349` | ④ 头像框：`Img(..., keepAspect: true)` —— 画布档那一支**复用同一份** `CardbackFace.Fit` 判据（算式与 `MenuDraw.Rect` 里那段逐字一致） |
| `:351-388` | ⑤⑥⑦ 7 颗字 → 走分支后的 `Txt(...)`（多带一个 `parentR` 实参：画布档要拿它算「相对父中心」） |
| `:433-449` | `HitBody` **按档分支**：世界档逐字不变；画布档走 `RectTransform.InverseTransformPoint`（任何宽高比都对） |
| `:451-477` | `Apply` 里那次「换字后补对齐」：**画布档不调**（那七颗 `anchor.x = 0` ⇒ 左缘本来就贴住；调 `MenuDraw.AlignLeft` 反而会用**世界单位**把节点挪走） |
| `:479-585` | 助手区新增 `LocalIn` / `Node` / `Img` 三个口 + `Txt` 改双分支（`Txt` 从 `static` 改成实例方法） |
| `:617-629` | 文件末尾「入口」一节补**待接线那两行代码**（给收口代理照抄） |

**🔴 「宿主 `null` ⇒ 逐位不变」的机械证据**（不是「我觉得一样」）：
世界档那一路的调用**逐条**仍是 `MenuDraw.ApplyPxRect`(`:288`) · `MenuDraw.Rect`(`:317`/`:524`) ·
`MenuDraw.ShadeHit`(`:320`) · `MenuDraw.Node`(`:503`) · `MenuDraw.Absorb`(`:342`) · `MenuDraw.Text`(`:572`) ·
`MenuDraw.SetVAlign`(`:580`) · `MenuDraw.AlignLeft`(`:471`/`:583`) —— 实参、顺序、渲染队列常量
（3980/4000/4010/4020/4030）**一个都没动**；新增的只有 `if (_ugui)` 那几处分支（宿主为 null ⇒ 走 `else`，
即原代码原文）。行尾：**LF / 0 个 CRLF**（改前改后一致，`git diff --numstat` = **256 / 43**，不是整篇重写）。

---

## ③ 接线清单（白名单外谁改哪一行）

### ① `Battle/BattleDriver.cs` —— 传宿主（`:15084` 建点）

现读 `:15084` = `_alliancePanel = AlliancePanelWindow.Create(root);`（`:15060` 是**注释行**，别照它）。
改成**两行 + 一行**：

```csharp
var cv = HudCanvas.EnsureSubCanvas(null, AlliancePanelWindow.SubCanvasName,
                                   HudCanvas.SortingLayer.OverHand, 0);
_alliancePanel = AlliancePanelWindow.Create(root, "Alliance Panel", cv);
```

- ⚠️ **`cv == null`（根 canvas 没立）时**：与同批各扇同一条口径 —— **别退回世界档**（半迁移态在 16:9 下看不出来）；
  本件的 `Create` 只在「宿主非空且不在 `Canvas` 下」时返回 null，**`cv == null` 传进来就是 `uguiHost == null`
  ⇒ 会静默走世界档** ⇒ 这一格要由**调用点**自己拦（或先 `HudCanvas.Ensure(LayoutSpace.Cam)`，同 `MulliganPanel.Create` 第一句）。
- ✅ **不需要摘任何注册表**：`grep -n "_alliancePanel"` 全仓只有 **6 处**（字段 `:1197` · 建点 `:15084` ·
  `Open` `:4068` · 三个自检口 `:4076/4078/4080`）—— **它不在 `_hudImages` / `_hudLabels` 那两个名单里**，
  而 `ReanchorHud`（`:16178`）**只遍历那两个名单**（不是「遍历 hudRoot 下所有 `ImageQuad`」）⇒
  **配方第 6 步（从 `ReanchorHud` + 4 个注册表里摘件）对本件无事可做**（迁上去之后它本来就不该被重锚）。
- ⚠️ **不要顺手改 `:15069-15081` 那颗钮**（`AlliancePanelOpenButton`）：它是 HUD 件、属于别的账。

### ② `CardPresentation/Editor/BattleScene.cs` —— A985③ / A1192 那一族（`:21458-21680`）

**前提**：那族断言读的是**节点名**（`panel985.Root.Find("NameHolder/PlayerLabel")` 等 7 条，逐字照 `Build` 的
`Node(…)+Txt(…, name)`）与 `Label` 两态/左缘 —— **节点名与嵌套两档完全相同** ⇒ 只有下面 **2 处**要动：

| 位置 | 今天怎么写 | 画布档要改成 | 为什么 |
|---|---|---|---|
| `:21544` + `:21551`（`left985`，A985③ 的**灭自证**那条） | `MenuDraw.PosInDesignSpace(NameLabel.transform).x − NameLabel.WorldW*0.5f`，再与 `LayoutSpace.PxX(left985) ≈ 276.220f` 比 | **画布档**：直接比「节点自己的绝对 px」= `MenuDraw.PosInDesignSpace(t).x + 960f ≈ 276.220f`（**不减 `WorldW/2`**） | 画布档那颗字 `anchor = (0, 0.5)` ⇒ **左缘 = 节点 x**；世界档是 `anchor = (0.5,0.5)` ⇒ 才需要减半宽。而 `PosInDesignSpace` 在画布档返回的**单位已经是设计 px**（父链缩放被除掉了）⇒ 再套 `PxX`（×108+960）会得到天文数字 |
| `:21591` + `:21598`（A1192 的 `RW1192`） | `\|t.textBounds.size.x\| × \|t.transform.lossyScale.x\| × pxPerWorld1192`（`pxPerWorld1192 = \|PxY(0)−PxY(1)\| = 108`） | **画布档**：`\|t.textBounds.size.x\|` **本身就是画布 px**（`TMP_UGUI` 的 `textBounds` 在 canvas 单位里）⇒ **不要再乘** `lossyScale × 108` | 16:9 下那两项**恰好**约掉（画布 x 缩放 = 1/108）⇒ 今天是**蒙对**的；**4:3 下画布单位 = 1/144 世界单位** ⇒ 乘出来只有真值的 **0.75 倍** ⇒ 那条「渲出来的宽 ≤ 框宽」会**松 25%**（漏判溢出，不产生假红） |

- ✅ **不用改的**：`L1192` 的 7 条路径查找 · `lb.AutoSizing` / `FontSizeMin/Max/Base` / `WrappingMode` 四格
  （都读 `_tmp` 字段，与档无关）· **点击链那 6 条**（`Click985(LayoutSpace.FromPixel(300f,250f))` = 窗内、
  `(960f,900f)` = 窗外）—— 因为 `HitBody` 已经按档分支，且 `BackgroundR` 那组常量**两档都在同一个绝对 px 帧**里。
- 🔴 **建议同批加两条新断言**（本件不能写 `BattleScene.cs`）：
  - **灭自证**：`panel985.OnCanvas == true` **且**那 7 颗字 + 那 7 张图**逐件** `GetComponentInParent<Canvas>() != null`
    —— 防的正是本件最坏那一档「一半在画布、一半在世界」（16:9 下逐位相同、全绿，只有别的宽高比才露）。
    ⛔ 别用「`Label != null`」顶替（那个**两档都真** ⇒ 同义反复）。
  - **次序**：`HudCanvas.SubCanvas(SubCanvasName).sortingLayerID == HudCanvas.SortingLayer.OverHand`
    **且** `sortingOrder == 0` **且** `overrideSorting == true` **且** 该子画布上**没有 `CanvasScaler`**。
- ⛔ **A1192 那 7 条「渲出宽 ≤ 框宽」在画布档下量法变了**（见上表）——**改量法**，⛔ 不要因为「反正绿」就留着。

### ③ `Battle/HudCanvas.cs` —— `SubCanvasRank` 补一条（**不补会与 `AboveShader`(8) 抢次序**）

`SubCanvasRank` = `HudCanvas.cs:470-483`；`OverHand` 段现读 5 项（`Mulligan` 3 · `Generic Multi Card Display
Combat` 4 · `ChooseCardMenu` 5 · `Card Display Window` 6 · `AboveShader` 8）。**加一行，放在 `"AboveShader Canvas"`
（`:482`）之后**（数组顺序 = 从**先画/在下**到**后画/在上** ⇒ 兄弟序大的往后放）：

```csharp
            "Alliance Panel Canvas",            // 13/14（原版 `Safe area FrontCanvas` 的 m_Children[13]）
```

- **出处**：`T09b_uGUI拦路判据.md` §A·3 那份 14 子件序 + `T09c` §一 我这一行（`Alliance Panel` 兄弟序 **13/14**）；
  子画布**自身在原版没有 `Canvas` 组件**（rm 列 = 「继承 `FrontCanvas`(1)」）⇒ 与 `Mulligan`/`ChooseCardMenu` 同构。
- ⚠️ **写给下一位写手**：`OverHand` 段**必须保持升序**。本组**还没迁**的原版格子是
  `DebugButtons`(**7**) · `PopupAnchor`(**9**) · `Tutorial`(**11**) · `LoadingMenu`(**12**) ——
  它们**都排在 8 与 13 之间** ⇒ 将来迁到它们时，那几条要**插在本条【前面】**，⛔ 别一律往后追加。

### ④ uGUI 档的**命中**怎么接（今天由 `BattleDriver.HitBody` 接 ⇒ **要不要改**）

- **「点窗外关窗 / 点窗内不关窗」= 一行都不用改**：`BattleDriver.HandleAlliancePanel`（`:4051`）走的是
  `AlliancePanelWindow.HitBody(world)`，而那一条**本件已经按档分支**（世界档逐字不变；画布档把世界点投进
  面板自己的局部坐标）。⚠️ 两档在 **16:9 下逐值相同**，非 16:9 下**画布档才是对的**（世界档那套
  `LayoutSpace.ToPixel` 是「假设所有件都 stretch」的全局映射）。
- **那两颗命中件（`ShadeHit` / `Absorb`）画布档不建** —— **零功能损失**：文件 `:326-328`/`:414-415` 自陈
  「**只有外壳那套 `PointerLayer` 会派发它**，战斗里不走那条路」；`grep -rn "CloseHit\|BackgroundAbsorb"` 全仓
  **没有任何一处**去查本扇窗的这两颗（那两族命中都在别的窗 / 壳侧）。
- ⛔ **本轮口径：不建 `EventSystem`、不开 `raycastTarget`**（判据 → `Battle/HudCanvas.cs:33-35`：
  判据还没收 + 现在没有可交互件）⇒ 画布档那 14 件**逐件 `raycastTarget = false`**（`MakeImageInParent`
  与 `TmpFont.NewTextUgui` 的出厂值，本件一处都没打开）。等 `T09` 第 4 步（迁按钮族）与第一个可点件同批建。

---

## ④ 判据与出处

| 结论 | 出处 |
|---|---|
| 原版那一格 = `Alliance Panel` @ `…/Safe area FrontCanvas` · `OverHand` / order 0 · 兄弟序 13/14 | `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md:39` · `T09_整层换uGUI.md:666`（14 子件序） |
| 根 RT 现核：`aMin=aMax=(0,1)` · `piv(0.13,0.825)` · `ap(-1.0444,-25.2717)` · `sd(815.044,475.470)` · `scale 0.9999718` | 自跑 `python -I -X utf8 工具/_probe_node_rect.py bundle_scenes_scenes_battlearena2 "Alliance Panel" "FrontCanvas"`（原样输出：`目标中心（相对 'FrontCanvas' 中心，y 向下）= (300.522, 179.799)` · `矩形（y 向上）：x [-107.000, 708.044] · y [-417.534, 57.936]`）· `…/assets_full/bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3052.json` |
| 14 个可见件 + 5 个容器**逐件绝对矩形**与原版**逐值吻合**（≤0.03 px） | 自写一次性脚本按 `RectTransform_*.json` 五元组 + 父链 + 累计 `m_LocalScale` 复算（**14/14 OK**，见 §⑤·2）；对账的另一半 = 本文件 `:137-177` 那 20 个 `PxRect` |
| 子画布口径（层/order + `overrideSorting`、⛔ 不加 `CanvasScaler`） | `T09_整层换uGUI.md` §五·补⑨ 一 · `HudCanvas.EnsureSubCanvas`（`Battle/HudCanvas.cs:421-455`） |
| **队列 → 兄弟序要取负** | `HudCanvas.Group.Add`（`:328-336`：「按 z 降序插入」= **z 大的排前面 = 画在底下**）· `UnitChatPanel.cs:96-100`（`ZPortrait = Z + 0.03`，注明「卡图在最底下」）· `BattleLogPanel.cs:245-254`（「值大的仍然在后面」+ `ZShade` 最大 = 压暗层） |
| 画布档的四个建件口（`SetPxSize` / `PosPx` / `MakeImageInParent` / `MakeText`）+ 单位 = **设计 px** | `T09_整层换uGUI.md` §五·补⑩ 一（表）· `Battle/HudCanvas.cs:219-260/505-513` |
| 兄弟口先例（`MulliganPanel` / `UnitChatPanel` / `BattleLogPanel` / `CardChoicePanel` / `CardDisplayWindow`） | 那五份文件头各一节（同一条配方） |
| 「宿主由调用方给、不传 ⇒ 留世界档」这条范式 | `Tooltip.UseHost`（`Core/Tooltip.cs:153`）· `ErrorMessageBanner.Create(..., uguiHost)`（`:430`）· `UnitChatPanel.Create(parent, uguiHost)`（`:295`） |
| 本扇窗今天**只在战斗里用**（壳侧 uGUI 化不是前置） | `S2_AlliancePanel跨线裁定_报告.md` §③（现核：全仓唯一生产调用点 = `BattleDriver.cs:15084`；壳侧 5 处**全是注释**）+ §⑨ 主对话裁定 |
| 迁 uGUI 的**全批口径分歧**（固定设计 px vs 点锚拉伸） | `S2` §⑨·9·5 那条「**中间态影响**」+ 主对话 2026-10-16 的裁定（本题书补发的那一段） |

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

### 1. 🔴 **如实记：本扇窗的根是【点锚】、⛔ 本件按现行口径用【中心锚 + 固定设计 px】落地（没改）**
逐值（题书点名要的那一串）：
```
Alliance Panel  RT 3052   aMin (0,1)   aMax (0,1)   piv (0.13, 0.825)
                          ap (-1.04443359375, -25.271728515625)
                          sd (815.0440063476562, 475.469970703125)   localScale 0.9999718070030212
父链两跳 Safe area FrontCanvas(3055) → FrontCanvas(3093)：两条都是 aMin(0,0) aMax(1,1) ap(0,0) sd(0,0)
```
⇒ **它不拉伸**（`aMin == aMax` ⇒ 固定尺寸），所以「4:3 下只有原版 ~75% 屏高」那条**在尺寸上不适用**；
但**位置**上仍有一处分家：原版的锚点在**父件左上角**（`Safe area FrontCanvas` = 整屏 ⇒ 屏幕左上角），
我们画布档把它当**中心锚 + 设计 px**（`HudCanvas.SetPxSize` + `PosPx`，与批 2/3 各扇同一套）。
**16:9 下逐位相同**；4:3（`CanvasScaler(Expand)` ⇒ 画布 = 1920×1440 单位、1 单位 = 0.75 屏 px）下：
原版顶边在屏幕顶**上方 43.4 px**，我们的在屏幕顶**下方 ≈91.6 px**（差 ≈135 px）。**本件按现行口径落地、⛔ 没改锚点**
（口径要整批一起换，半批换 = 半迁移态）。
**那两个数怎么来的**（4:3 · `Expand` ⇒ `scaleFactor 0.75` ⇒ 画布 = 1920×**1440** 单位、1 单位 = 0.75 屏 px）：
· **原版**（锚 = 屏幕左上角）：顶边 = `−57.933 × 0.75` = **−43.4 屏 px**（在屏幕顶**上方**）；
· **我们**（中心锚 + 设计 px）：节点中心在画布中心**上方 360.20 单位**（`540 − 179.795`），画布半高 **720** 单位
⇒ 中心距屏顶 `(720 − 360.20) × 0.75` = **269.85 屏 px**，框高 `475.470 × 0.75` = **356.6 屏 px**
⇒ 顶边 = `269.85 − 178.3` = **+91.6 屏 px**（在屏幕顶**下方**）⇒ 差 **≈135 px**。
（⚠️ 这一处分家**只在非 16:9 现形**；16:9 下 `scaleFactor = 1`、画布 = 1920×1080 ⇒ 两边逐位相同。）

### 2. ✅ **已做的对账：20 个 `PxRect` vs 原版子树 → 14/14 逐值吻合；只有 4 条是【有意重算】的**
自写脚本（`bundle_scenes_scenes_battlearena2`，局部矩形在**父的局部单位**里递推、实绘矩形 = 局部 × 累计
`lossyScale`）复算了 `Alliance Panel` 整棵子树，与本文件 20 个常量逐条比：

- **完全吻合（≤0.03 px，14 条）**：根 · `Close Background` · `Background` · `BG` · `BGFrame` + 四块 ·
  `Border`（头像框）· `Alliance` 组 · `Alliance Label` · `Alliance Name` · `NotInaAllianceText`。
  ⇒ 文件头那句「全部 = 实绘矩形，已乘 `localScale`」**成立**；也顺带证明本件画布档那套
  「绝对 px → 相对父中心」的换算（`LocalIn`）与原版的**父中心**口径是同一件事
  （抽样自证：熄灯层的局部中心算得 `(351.74, 41.66)`，与原版 prefab 里那颗的 `pos(351.755, 41.660)`
  + `anchor(0.5,0.5)` **逐值吻合** ✓）。
- **有意不同（4 条，不是缺陷）**：`NameHolder` / `TitleHolder` 与原版差 **376.96 px**（x/y 都对不上），
  4 颗字差 **130.5 / 130.5 / 89.4 / 89.4 px** —— 因为原版那 3 个 holder 的序列化 rect 是
  **`VerticalLayoutGroup` 跑之前的模板位**（两个 holder 都是 `x1==x2==180.000`、**0 宽**），
  本文件按 `UguiLayout.VerticalChild` **重算**过（`:351-359` 那段注释）。**x 与宽**两档都吻合
  （例：`PlayerLabel` 原版 x `180.000→273.939` vs 我们 `180.000→273.941`）。
  ⚠️ 于是那 3 行（含不建的 `Player Rating Display`）的**纵向落点仍然是「推的」**：
  `W_AlliancePanel.md` 的「没查清①」原样挂着 —— **没有运行期读数**（要一条真机/`SceneJumpShot` 的读法，
  属另开一件）。

### 3. ⛔ **四件老缺口仍然开着**（与迁 uGUI 无关，本件**没顺手做**，逐条确认在案）
① 头像立绘不画（`Image` 的 `m_Sprite = 0`，运行期灌；原版出厂态就不画）·
② 两处评级不建（`Main Icon`/`Individual rating value` 序列化 0×0 + 我们无对手评级数据）·
③ 徽章 `Frame`/`Badge` 不建（`m_Sprite = 0`；单机恒走「无联盟」那一支）·
④ 出场 `DOFade` + `DOScale` 没做，且**三个 DOTween 常量仍然没读**（要在 `GameAssembly.dll` 里取值）。

### 4. ⛔ **`T09` 批 3 收口仍未做**（本件只是它的一格）

- 本组还有**没迁**的原版格子：`SkillPanel`(1) · `Unit Chat`(2) · `DebugButtons`(7) · `PopupAnchor`(9) ·
  `Tutorial`(11) · `LoadingMenu`(12) —— 见 `T09` §八·1 / §八·3 / §八·11。
- `OverHand` 组收口要**与 HUD 图族 + HUD 字族一起**做（`T09` §五 第 5 步），本件**不构成**那一批。
- **「排序层 vs `renderQueue` 谁先比」** 这条**承重假设**（为什么「上画布就压得住还没迁的件」）
  **仍然没有实机验证**（`S2` §6.2 / `T09` §八·5）—— 本件没验（⛔ 现行口径不许起 player）。
- **`AboveShader` / `TipText` 我们建出来了没有** —— 没查（`T09` §六·4 同一条），与本件无关。

---

## ⑥ 顺手发现（不属于本件，**只报不改**）

### 1. 🔴 **`S2` 报告 §1.2 的「绝对矩形」x 列算错了：不是 `x[853.0, 1668.0]`，是 `x[-107.0, 708.0]`**
- `S2` 原文：`绝对矩形（1920×1080 左上原点）= x[853.0, 1668.0] · y[-57.9, 417.5]（顶出屏 57.9 px）`。
  **y 那半是对的，x 那半多加了 960**（`S2` 给 x 加了 960、给 y 没加 540，两行不对称）。
- **三条独立证据**（都指向 `x[-107.0, 708.0]`）：
  ① `_probe_node_rect.py` 自己那一行原始输出就是 `x [-107.000, 708.044]`（`S2` 是**后处理**时才 +960）；
  ② 本文件 `RootR.x1 = -107.00`（从 prefab 五元组 + 父链**复算**，且本报告 §⑤·2 证明它 14/14 吻合）；
  ③ `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:759` 那行原文是
  `Alliance Panel (inactive) [-107,1022 815x475]` —— 而该表的 x 列是**绝对左缘**（校准：`FrontCanvas [0,1080 0x0]`
  = 整屏的左缘 0；`EnemyInfo [50,1130 260x75]` = 由 prefab 算出的绝对左缘 50 ✓；y 列 = 绝对顶边 **+1080**，同一行
  的 `1022 = -58 + 1080` ✓）。
- ⇒ **本扇窗在屏幕【左上角】**（左缘出屏 107 px、顶边出屏 57.9 px，与「敌方名牌在左上」自洽），
  **不是** `S2` 说的右上区域。**`S2` §6.3「交集 A」的两侧是在不同帧里比的**（面板那侧被 +960 了，
  `Card Display` 那侧没有）⇒ 「相交 ≈483×311 px」这个数不成立；按两边都取绝对帧重算 = **≈124×311 px**
  （面板 `x[-107,708] × y[-57.9,417.5]` ∩ `Card Display x[584,1336] × y[106,974]` ⇒ `x[584,708] × y[106,417.5]`）。
  ⚠️ **结论方向不变**（两窗同开时**仍可见**，优先度那条裁定不受影响），但**数字要换**，且这句话在
  `T09` §八·2 与 `S2` §9·5 里各抄了一份（**铁律 5：两处都要改**）。
- ⚠️ 另一条**旁证**（顺手核的、可以放心用）：原版 `Safe area FrontCanvas` 在 `2D层_battlearena1全树.md:232` 那行
  **没有任何 `script` 标记**（该 dump 给每个 `MonoBehaviour` 打一个 `script`）⇒ 它身上**没有**安全区脚本
  ⇒ 「它的左上角 = 屏幕左上角」这个前提**成立**（否则本扇窗在刘海机上会被二次挪位）。

### 2. 🔴 **`T09` 题书里那句「值照搬」（渲染队列 → 兄弟序）会【把整扇窗画反】—— 本件按【意图】落地了**
- 题书原话：「迁到画布后**兄弟序**要表达同一个先后（**大的在后**，语义同向、**值照搬**）」。
- **`值照搬` 这一半不成立**：`HudCanvas.Add` 的 z **大的画在底下**（`HudCanvas.Group.Add` 按 z 降序插 +
  `UnitChatPanel.ZPortrait`/`BattleLogPanel.ZShade` 两条注释），而 `renderQueue` **大的画在上面**
  ⇒ **两者极性相反**。照搬（`QShade 3980` 当 z）⇒ 熄灯层变成**最小 z** ⇒ 被排到最后 ⇒ **整屏 α0.4627 黑罩在
  面板【上面】**（一眼可见的回归）。
- ⇒ 本件取 **`ZOf(q) = −q`**（`:134`，**唯一一处**换算）：相对先后**逐件不变**（熄灯层 −3980 最大 ⇒ 最底下 ✓）。
- 🔴 **建议**：这句在 `T09` §五·补⑩ 一 那张表的脚注里，**批 3 的其余各扇也会读它** ⇒ 谁维护那份正本就顺手改一句
  （本件不在白名单、没动）。

### 3. ⚠️ **`T09` §五·补⑩ 六 第 2 步（「根节点挂进子画布：`FillParent`」）对本件不适用**
那一步的前提是「面板根 = 整屏矩形」（`EndPanel` / `Mulligan` 那种 `aMin(0,0) aMax(1,1)` 的 stretch 根）。
本扇窗的根是**点锚 + 815×475 的自有矩形** ⇒ 本件走 `HudCanvas.SetPxSize` + `PosPx`（写它自己的 rect），
⛔ 没用 `FillParent`。**其它「自有 rect 的面板根」迁的时候要照这条分岔**（配方那一行建议也补一句）。

### 4. ✅ **顺手验证了一条与本件无关的老账（结论：不是缺陷）**
`S2` §1.2 的「尺寸 815.044 × 475.470」用的是**父链三级的 `sd` 都是 0** 这个假设解出来的 ——
我用「局部矩形逐级递推」重算了一遍（§⑤·2），**逐值吻合**，且顺带确认三级父链确实都是 stretch
（`FrontCanvas` / `Safe area FrontCanvas` / 根 `Canvas` 的 `sd` 全 0、`ap` 全 0、`localScale` 1）。

---

## 附：可直接粘进总表的一行（≤3 行）

`T09` 批 3（`OverHand` 组 · 兄弟序 13/14）**`AlliancePanelWindow` 已就地翻 uGUI（双模式：宿主 null ⇒ 世界档逐位不变）** —— 子画布 `Alliance Panel Canvas`／`OverHand`/0；14 件（7 图 + 7 字）走 `HudCanvas.*`、层深度 = `−renderQueue`（**照搬队列会画反**，见报告 §⑥·2）；`HitBody` 已按档分支。
**待接线三件（全在白名单外）**：① `BattleDriver.cs:15084` 换成 `EnsureSubCanvas(...SubCanvasName, OverHand, 0)` + `Create(root, "Alliance Panel", cv)` ② `HudCanvas.cs:482` 后补 `"Alliance Panel Canvas"`（13/14，⚠️ 将来的 7/9/11/12 要插在它**前面**）③ `BattleScene.cs` 那族断言 2 处量法要按档改（`:21544/21551` 左缘 / `:21591/21598` 的 `RW1192`）。
类型检查 0/0；没跑 Unity；20 个 `PxRect` 与原版子树**14/14 逐值吻合**（另 4 条是 VLG 重算，横向也吻合）；**顺手订正**：`S2` §1.2 的绝对 x 应是 `[-107, 708]`（不是 `[853,1668]`）⇒ §6.3「相交 483×311」按绝对帧重算 = **124×311**。
