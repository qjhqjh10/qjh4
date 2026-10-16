# T09 · SkillPanel（原版 `ActiveSkillDesc`）迁 uGUI（执行代理 A2）

> 日期 2026-10-16 · 白名单 = `CardPresentation/Battle/SkillPanel.cs` + 本报告。
> ⛔ 未跑任何 `*.Run` / 未起 player / 未动 git / 白名单外一个字节未改（`git status` 里另几个 `.cs` 是别的代理在飞的）。
> ✅ **`TMPDIR=/tmp/wf_a2 bash d:/4/Unity/工具/typecheck.sh` ⇒ `SkillPanel.cs` 0 错**
> （同时段的「运行时错误数 20~27」**全部落在 `ChatPopupPanel.cs`** —— 另一个代理正写到一半，不是本件引入的）。

---

## ① 结论

1. `SkillPanel` 已改成 **双模式**（主对话硬要求，先例 = `Tooltip.UseHost(host)` / `ErrorMessageBanner.Create(…, uguiHost)`）：
   · **宿主没登记 ⇒ 世界档**，与改之前**逐位相同**（16:9 与非 16:9 都是）；
   · **登记了（`UseHost(子画布)`）⇒ 画布档**：7 个件建在 `HudCanvas.EnsureSubCanvas(null, "ActiveSkillDesc Canvas", OverHand, 0)` 那块子画布上，
     坐标 = **设计 px**、缩放交给 `CanvasScaler(1920×1080/Expand)`。
2. 两档**共用同一组面板内归一化常量**（`BgRect`/`NameRect`/`CostIconRect`/…），只在最后一步换坐标系：
   世界档 `InPanel`（世界单位）／ 画布档 `CvPos`（设计 px）。**判据只此一处**（`BoxW/BoxH/Place/PlaceQuad`）。
3. **16:9 逐位相同**（画布 `scaleFactor = 1`、两轴 1 单位 = 1/108 世界单位）；别的宽高比由 `CanvasScaler` 接管 —— 这正是本件要修的那件事。
4. **排序**：层/order 照 `T09c` §一 我那一行 = `OverHand` / **0** / 兄弟序 **1/14**（子画布名 `"ActiveSkillDesc Canvas"`）。
   ⚠️ **同组同 order ⇒ 先后只由兄弟序定** ⇒ **还差一步**：`HudCanvas.SubCanvasRank` 要插一条（见 §三）——**那一步在别人的文件里，本件没做**（不插时它建得最晚 ⇒ 会排在 `Mulligan`/`Card Display Window` 等**之上**，与原版子件序 **1 < 3** 相反）。
5. 🔴 **一条口径差异（属全批，不是本扇独有）已如实记 → §五·1**：画布档的位/尺寸按 **固定设计 px** 给（`T09` §五·补③ 一 那条），
   而原版那一级是**锚点拉伸**（`anchorMin.y=0 / max.y=0.3040129840`）⇒ **4:3 下我们那块面板只有原版的 75%**（22.6% vs 30.2% 屏高）。
   **16:9 与今天逐位相同**；窄屏这一档**比今天的世界档还小**（今天 30.07%）。⛔ 我没擅自改口径，请主对话裁定（§五·1 给了正解与代价）。
6. 现有断言**逐条核过一遍，画布档下都还成立**（论证 → §四·4/5），所以 §三 那批「该改的断言」实际只有**新增**（没有要改的既有期望值）。

---

### 🔴 更正（2026-10-16 · 收口波执行代理 `G4` · 铁律 5 —— **原文一字未删，只在这里标明哪句被推翻**）

> 两处被 `T09_SkillPanel_锚点补正_报告.md`（`A10`）**现核推翻**。⚠️ **按本报告写断言的人请先读这一段**。

**更正① —— §五·1 那张表「我们·画布档」行的「面板中心（离屏底）」那一格【串了帧】。**
- **原文（本文 §五·1，`:160`，仍保留在上面）**：「| **我们·画布档**（固定 px） | `324.82×0.75 = 243.6 px` ⇒ **22.56%** | `219.19×0.75 = 164.4 px` ⇒ **15.22%** |」。
- **错在哪**：`219.19` 是 **16:9 帧**里「面板中心离**屏**底」的距离（= 16:9 画布半高 `540` − 面板中心 `320.81`）。
  **4:3 下画布半高是 `720` 单位**（画布 `1920×1440`）⇒ 离屏底 = `720 − 320.81 = 399.19` 单位。
- **正确值（4:3 · 屏 `1440×1080` · `scaleFactor 0.75`）**：**`399.19` 单位 = `299.4` 屏 px = `27.72%` 屏高**（= `(720 − 320.81) × 0.75`）。
  ⚠️ 同表**「面板高 `22.56%`」那一格是对的**（尺寸与帧原点无关，不用改）。
- **结论不变**：画布档**既偏小、位置也偏** —— 只是那两格里的**中心**那一格要换成 `299.4 px / 27.72%`。
- **出处** → `资料/普查产出_1016并行/T09_SkillPanel_锚点补正_报告.md` §⑥·1（`A10` 现核；那边「原版」行用的 `1440` 是对的）。

**更正② —— §⑥·4 那条「批处理构图提醒」的结论【方向反了】。**
- **原文（本文 §⑥·4，`:191`）**：「⇒ `06c_技能卡面板` 那张截图**不能与改之前并排比**」。
- **实况**：**改之前**画布档的位/尺寸是**写死 16:9** 的（`PanelPxW/H` + `PanelCenterCvPx` 全按 `1920×1080` 算）⇒ 那两张截图**能**并排比；
  **改之后**（`A10` 的 `PanelCvRect` 按原版 `RT_3104` 锚点现算）面板才**跟着画布高走**。
  ⇒ **「不能并排比」的时刻是【这次改动之后】**（若批处理 `_cv.rect` 有效 ⇒ 构图从 16:9 变成 `1920×1440` 的 4:3-ish），**不是改之前**。
- ⚠️ **以实跑为准**：`A10` **没跑**（现行「先复刻后检查」口径）⇒ 本条**方向已定、数值待实跑**
  （§五·2 那条「批处理 `_cv.rect` 量不量得到」**仍没验**）。
- **出处** → `资料/普查产出_1016并行/T09_SkillPanel_锚点补正_报告.md` §⑥·2。

---

## ② 改动清单（文件:行号 + 一句话）

| 文件:行号 | 一句话 |
|---|---|
| `Battle/SkillPanel.cs:38-70` | 文件头新增 `T09` 批 3 那一整块（原版宿主 / 双模式 / 坐标两档 / 退场的口 / **两条如实记的差异** / 本扇**零射线件**） |
| `:83-93` | 四处常量注释按档补齐（`PanelPxW/H` = 画布档的尺 · `ZBg/ZText/ZLight` 画布档只用来排兄弟序 · `ZPanel` 只喂世界档的壳 · `PxPerUnit` 世界档专属） |
| `:198-213` | 字段 `ImageQuad _bg,_light,_manaIcon` → **`HudPiece`**；新增 `RectTransform _cv`（= 宿主，null ⇒ 世界档）· `public const string SubCanvasName` · `UguiMode` · `CanvasRect` |
| `:238-294` | 新增 **`UseHost(host)`**（双模式的总开关；`null` ⇒ 出声 + 留世界档）· **`Teardown()`**（换档时拆旧，⛔ 防「两边都建」）· `KillGo` |
| `:304-305` | `Build()` 拆成 **`if (UguiMode) BuildCanvas(); else BuildWorld();`** |
| `:320-327` | `BuildWorld()` = **改之前那一版**照搬（唯一差别：三颗图过一道 `HudPiece.Of(...)`，建出来的仍是旧 quad） |
| `:345-363` | 新增 **`BuildCanvas()`** —— `MakeCv` 建图（`HudCanvas.MakeImageInParent`）· `MakeLabel`→`HudCanvas.MakeText`；缺图/缺字体**出声**（⛔ 不静默、⛔ 不退回世界档） |
| `:374-386` | `MakeLabel` 按档给字号：世界档 `WorldOfPx(px)`（÷108）· 画布档 **`SetGlyphHeight(px)`（设计 px，⛔ 不除 108）** |
| `:426-479` | `RefreshLayout` 两档合一：世界档仍摆壳 + `PanelW/H`；画布档位/尺寸全是**设计 px 常量**（幂等、换分辨率不必重排）；新增 `BoxW/BoxH/MarginW`（**框/边距按档取量纲**） |
| `:484-545` | 新增 `Wpx/Hpx`（画布档的尺）· `PanelCenterCvPx`（面板中心在子画布里的局部坐标 = **−320.81**）· `CvPos`（面板内归一化 → 子画布局部、y 向上、**z 恒 0**）· `SizePx` · `SetHeightCv` · `Place(…)` 四个重载 |
| `:550-590` | `PlaceManaIcon`（`side` 判据一处，两档各走各的尺寸口）· `PlaceQuad` 改成**按档两条支**（画布档 = `sizeDelta` 直接吃 px、**不需要** `SetAspect`） |
| `:644-656` | **`Contains` 按档取坐标系**：世界档 `transform`（世界单位）· 画布档 **`_cv`**（设计 px、原点 = 画布中心）—— 与「件真正画在哪」同一个变换 |
| `:675-687` | `DrawnWorldSize` 加画布档那一支（`sizeDelta × lossyScale`）；两口**契约恒为世界单位**（跨件比较用） |
| `:713-729` | `HitBoxWorldSize` 加画布档那一支（`PanelPxW/H × _cv.lossyScale`）—— 与上面两个实绘口**同一把尺** |
| `:871-891` | 新增 **`OnCanvas`**（灭自证：每一件都是 uGUI 件**且**挂在画布父链下）+ `IsUgui(HudPiece)` / `IsUgui(Label)` |
| `:898-915` | `Describe()` 头一行加**档位**（两档的件单位不同，不带这一格读数没法横向比） |
| `:597-601` / `:145-152` | `FitBox` / `AutoMinPx` 两处 doc **就地订正**（铁律 5）：框的量纲**按档取**，原写「一律世界单位」只对世界档成立 |

---

## ③ 接线清单（白名单外，⛔ 我一个字节没改）

### ③·1 `Battle/BattleDriver.cs` —— **登记宿主（本件跑起来的前提）**

插在 `BuildHud()` 里 `aboveCv` 那一段之后（现核 **`:14434`** 附近，或 `BuildHud` 内任一处），**必须早于第一次 `skillPanel.Build()`**：

```csharp
            // 🆕 2026-10-16（T09 批 3）：技能面板（原版 `ActiveSkillDesc`，OverHand/0、兄弟序 1/14）
            //   组件是**场景挂的**（没有 Create）⇒ 宿主由这里登记；不登记 ⇒ 它留在世界档（逐位等于改之前）。
            if (skillPanel != null)
            {
                HudCanvas.Ensure(LayoutSpace.Cam);      // 幂等；与 `MulliganPanel.Create` 同一条写法
                skillPanel.UseHost(HudCanvas.EnsureSubCanvas(null, SkillPanel.SubCanvasName,
                                                             HudCanvas.SortingLayer.OverHand, 0));
            }
```

- ⚠️ **那句 `HudCanvas.Ensure` 不是多余的**：`:14430` 的 `EnsureSubCanvas` 之前**没有** `Ensure`，它靠 `:14073 BuildEnergyLights()` →
  `AddEnergyLight` 内部的 `Ensure` 兜；而 `BuildEnergyLights` 在 Glow 贴图缺失时**提前 return** ⇒ 那一档根画布根本立不起来
  （**顺手发现 ⑥·1**）。
- ✅ **`ReanchorHud` 里 `:16219` 的 `if (skillPanel != null) skillPanel.RefreshLayout();` 不用摘** ——
  ① 宿主没登记（子画布建不起来 / 探针自己造的场景）时**世界档仍然要它重锚**；② 登记了它是幂等的无操作。
  ⇒ 这条与 §五·补⑩ 第 6 步（「摘掉重锚」）**不冲突**，请照此保留。
- ✅ **四个注册表无事可做**：`_hudLabels/_hudSpots/_hudImages/_hudImageSpots` 里**本来就没有** SkillPanel 的件
  （`grep` 现核：字段成员只有 HUD 图/字那两族）⇒ 本扇**没有**要摘的条目。
- ⚠️ **重建开销**：生产路径里 `Awake()`（若跑）会先建一次**世界档**，随后这一句登记 ⇒ `UseHost` 会**拆掉重建成画布档**（已实现，`UseHost` 里出声说明）。
  想省掉这一次重建，就把登记挪到第一次 `Build()` 之前（例如 `BattleDriver.Awake` / `Begin()` 的最前面）—— **由主对话定**。

### ③·2 `Battle/HudCanvas.cs` —— `SubCanvasRank` 插一条（**同层同 order 的先后就靠它**）

- `:470-483` 的 **`OverHand` 段最前面**插：`"ActiveSkillDesc Canvas",   // 1/14`（**插在 `"Mulligan Canvas"` 之前**）。
  判据 = **原版 `Safe area FrontCanvas` 的 14 个子件序**（`T09b` §A·3）：`[1] ActiveSkillDesc` 在 `[3] Mulligan` **之前**
  ⇒ 表是「升序 = 先画在下」⇒ 必须在最前。（`T09c` §一 我这一行：`RT_3104` · `RT_3055.m_Children` · `OverHand`/**0**/兄弟序 **1/14**。）
- `:464` 那句注释 **已过期**，要改：「⚠️ `SkillPanel`(1) / `Unit Chat`(2) **还没迁**，迁了要**插到前面**」
  ⇒ `SkillPanel` **本次已迁**（就是上面那条）；`Unit Chat` 由另一个代理在做（**只报不改**）。
- ⛔ 没有别的 `HudCanvas` 缺口：本扇用 `MakeImageInParent`（中心锚）就够，
  **没有**新造 `ImageInParent`/`MakeSlicedPiece+border=0` 那种权宜件（§七·10 那条不会再长第二份）。

### ③·3 `Editor/BattleScene.cs` —— 该改/该加的断言

| 位置 | 处置 |
|---|---|
| `:409` `driver.skillPanel.Build()` | **保留**。⚠️ 它在 `Begin()`(:410) **之前** ⇒ 那一刻还没登记宿主 ⇒ 建的是**世界档**，随后 `Begin`→`BuildHud` 登记时会拆掉重建一次。想省这一次重建就把这一句挪到 `Begin` 之后（**由主对话定**，见 ③·1 末）。 |
| `:7045-7111` 技能链那 15 条 | **一条都不用改**（逐条核过：`Visible/ShownName/ShownTargets/TargetsText/Alpha/CurrentLight` 全与档无关；`sp.transform.position` 那两句的成立性 → §四·4）。 |
| `:21243-21301` `E2⑥` 探针 | **一条都不用改**（两个实绘口 + 命中框口已改成「两档同一把世界尺」→ §四·5）。 |
| `:23454-23455` | 不受影响（`FindObjectOfType<SkillPanel>()`）。 |
| `:23695` `cam.aspect = LayoutSpace.DesignAspect` | 不动。⚠️ 送它一条**提醒**：批处理里 `Screen = 640×480` 而 aspect 钉 16:9 ⇒ 画布 = **1920×1440** ⇒ **画布档的构图 ≈「按 4:3 排版」** ⇒ `Shot(cam,"06c_技能卡面板")` **不能与改之前并排比**（`T09` §七·8；**不是缺陷**）。 |

**该【新增】的断言（`★` = 灭自证，期望值取**判据侧**）**——建议挂在 `:7103` 那一带（技能面板那一段收尾）：

```csharp
Check(sp.UguiMode == (sp.CanvasRect != null), "★ 档位判据自洽（`UguiMode` ⇔ `CanvasRect != null`）");
var spCv = sp.CanvasRect;
Check(spCv != null && spCv.overrideSorting, "★ 技能面板子画布 `overrideSorting = true`");
Check(spCv != null && spCv.sortingLayerID == HudCanvas.SortingLayer.OverHand, "★ …层 = `OverHand`");
Check(spCv != null && spCv.sortingOrder == 0, "★ …order = 0（原版 `ActiveSkillDesc` 那一行）");
Check(spCv != null && spCv.GetComponent<CanvasScaler>() == null, "★ 子画布**不带** `CanvasScaler`（原版全场景只有一个、在根上）");
Check(sp.OnCanvas, "★🔑（灭自证）面板那 6 件**都在 uGUI 子画布上** —— 半迁移态（一半画布一半世界）在 16:9 下逐位相同、全绿，只有非 16:9 才露");
Check(sp.UguiMode || !sp.OnCanvas, "★（灭自证·反面）**世界档下 `OnCanvas` 必须为假** —— 防「把 `OnCanvas` 写成恒真」（那会让上面那条失去判别力）");
```
> ⚠️ 这一组**要放在 `Begin()` 之后**（`:410` 之后）才断得到画布档；放在 `:409` 那一带断到的是世界档。
> 🔴 **两条口径提醒**（`CLAUDE.md` §三）：① 别用「`UguiMode`」替 `OnCanvas`（前者只看宿主登记了没，不看件建在哪 —— **两边同源 = 同义反复**）；
> ② 期望值取**判据侧**（`HudCanvas.SortingLayer.OverHand` / 原版的 order 0），**别取 `HudCanvas.SubCanvas(name).sortingOrder`**（那是拿实现证明实现）。

### ③·4 其余（都不在 T09 正文那 8 步里，供主对话排期）

- `WfSlider` / `BattleDriver.AdvanceTimeline`：**与本扇无关**（本面板没有任何随时间推进的东西 —— 淡入淡出走 DOTween + `CardTween.Mode`，与世界档同一套）。
- `Editor/BattleScene.cs` `E2⑥` 那段 `hits964.Add(...)` 的**说明文字**「实绘读法 = `WorldW/H × lossyScale`」在画布档下已不成立（改成读 `sizeDelta`）⇒ 措辞可顺带补一句（**只报不改**）。

---

## ④ 判据与出处

1. **原版宿主那一行**：`资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 `SkillPanel` 行
   —— `ActiveSkillDesc` @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`，继承 `FrontCanvas`(rm 1)、`OverHand`、order **0**、兄弟序 **1/14**；出处 `RT_3104` · `RT_3055.m_Children`。
   §三 的裁定（优先级高于 §一）里没有涉及本扇的条目。
2. **我自己现核了一遍那个节点**（`battlearena2`，工具 = `python -I -X utf8 工具/_probe_node_rect.py bundle_scenes_scenes_battlearena2 ActiveSkillDesc "Safe area FrontCanvas"`）：
   `RT_3104` = `aMin(0.6655767, 0)` · `aMax(0.9657887, 0.3040129840)` · `piv(0.5,0.5)` · `ap(−606.1101, 55.0009)` · `sd(−0.0003662, −3.51)` ·
   `scale(1,1,1)` —— 与 `SkillPanel.cs` 文件头（2026-10-10 那串逐级折算）**逐位吻合** ✓（⇒ 面板矩形 576.41×324.82、中心 (960.0, 219.19)、底部居中）。
   ⚠️ 那个工具对**被锚拉伸的祖先**（`Safe area FrontCanvas` 的 `sd=(0,0)`）算不出内层 —— 它打印的「尺寸 0×0」是**已知局限**，本件用的是**我们既有的归一化常量**，不受它影响。
3. **坐标模型**：`T09` §五·补③ 一（px 常量原样搬进 `RectTransform`；16:9 = 逐位相同；4:3 = `scaleFactor 0.75`）；
   **关卡 1**（`MenuDraw.SetPxSize` = ÷108，uGUI 档要走 `HudCanvas` 的口）§五·补⑩ 一；**配方 8 步** §五·补⑩ 六；
   **面板族不能先于面板内部的字迁**（故本扇**图与字同批**）§五·补⑨ 三/四。
   批量处理下的量纲效应（两轴系数不同）→ §七·8（本件**读回一律读 `sizeDelta`**，不写死在 108 上）。
4. **`sp.transform.position` 那两条断言（`BattleScene.cs:7098/7101`）为什么照样成立**：
   壳 `transform` **两档都摆**在 `LayoutSpace.ToWorld(面板中心)`（见 `RefreshLayout` 那一段注释）。
   批处理里（Screen 640×480、aspect 钉 16:9 ⇒ 画布 1920×1440、`localScale = (1/108, 1/144)`）：
   壳的世界 y = **−2.9705**；画布档的命中框（世界）= 中心 **−2.228** ± 半高 **1.128** ⇒ `[−3.356, −1.100]`
   ⇒ **−2.9705 在框内，离下沿 0.386 世界单位（≈55 canvas px）** ✓；真 16:9 下两者**逐位重合**。
   4:3 真屏同理（壳 20.3% 屏高 vs 面板 3.9%–26.5% 屏高）✓。
5. **`E2⑥` 探针为什么不用改**：`LightDrawnSize` / `BgDrawnSize` / `HitBoxWorldSize` 三个口在画布档都乘**同一跳** `lossyScale`
   ⇒ 那一节的三条布尔（内侧中 / 外侧不中 / 底板中心中）**与 `lossyScale` 取多少无关**（逐值代过：内侧 129.6 < 288.2 canvas px、外侧 432 > 288.2、
   底板中心偏移 (−5.4, −24.8) 在框内），且**两者量的是同一把世界尺** ⇒ 与 `Contains` 咬合。
6. **双模式先例**：`Core/Tooltip.cs:153 UseHost(host)` · `Battle/ErrorMessageBanner.Create(…, uguiHost)`（批 2）。
7. **`HideAbilityButton`**（原版那块透明捕获区，`m_Color.a=0` / `Sliced` / `RaycastTarget=1`）**我们没建**，
   等价语义（点面板外 ⇒ 收起）在 `BattleDriver` 那条通用路上 ⇒ 本扇**零射线件**，`raycastTarget` 一条都不用动（§三·硬要求那条不适用）。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **尺寸口径那条差异（属全批，请主对话裁定）**：画布档按**固定设计 px**给面板矩形（`T09` §五·补③ 一 那条口径），
   而原版 `ActiveSkillDesc` 那一级是**锚点拉伸**（`aMin.y=0 / aMax.y=0.3040129840 / sd.y=−3.51`）。
   逐值（4:3 · 1440×1080 屏 ⇒ canvas 1920×1440 单位、`scaleFactor 0.75`）：

   | | 面板高（占屏高） | 面板中心（离屏底） |
   |---|---|---|
   | **原版**（锚点拉伸） | `(0.3040130×1440 − 3.51)×0.75 = 325.7 px` ⇒ **30.16%** | `(0.152×1440 + 55.0)×0.75 = 205.4 px` ⇒ **19.02%** |
   | **我们·画布档**（固定 px） | `324.82×0.75 = 243.6 px` ⇒ **22.56%** | `219.19×0.75 = 164.4 px` ⇒ **15.22%** 🔴 **本格串了帧（`219.19` 是 16:9 帧的数）—— 4:3 正确值 = `399.19 单位 = 299.4 屏 px = 27.72%`，见文首「更正①」** |
   | **我们·世界档**（今天） | `3.007 世界单位 = 30.07% 屏高` | 20.3% |

   ⇒ **16:9 三档一致**；**4:3 下画布档只有原版的 75%**（而**今天的世界档 ≈ 原版**）。
   **正解**（我没做）：把面板那一级按原版 `RT_3104` 的 `anchorMin/Max + ap + sd` **原样锚上**，7 颗子件按**面板内比例**（`anchorMin/Max`）挂它下面
   —— 那样 4:3 下也会跟着变高。**代价**：① 与配方第 2 步（`FillParent`）、与 §五·补③ 一 那条口径**不一致**（要主对话改口径）；
   ② 子件要改成 stretch 锚点、字号框也要跟着面板重算（`RefreshLayout` 那套就不够用了）；③ 已在飞的另外 4 扇（`Mulligan`/`MultiCardDisplay`/`ChooseCardMenu`/`CardDisplay Window`）**同样是固定 px**
   ⇒ **要么整批一起改，要么整批都记着**。⛔ 我没擅自改。
2. **节点层级少一级**：原版是「父画布 → `ActiveSkillDesc`（面板矩形那一级）→ 7 件」；我们画布档是「子画布 → 7 件」。
   （我们这边对应 `ActiveSkillDesc` 的是**世界空间那层壳** `transform`，它两档都不装画布上的件。）
   ⛔ 按 §五·补⑩ 二「别顺手改嵌套、差异如实记」—— **只记**。若主对话要补那一级，是一行 `MakeImageInParent` 之外的重排（子件坐标要从"画布中心"改成"面板中心"，`CvPos` 里减掉 `PanelCenterCvPx` 即可）。
3. **`A1284`（`T15`）没被本次迁移解决** —— 它的判据是「`SkillPanel.cs:62-64` 那类**逐级折算到面板坐标**的自陈注释要**逐颗回代复核**」
   （7 颗里 2 颗折错，已由 `A1242①` 修了 `CostIcon/CostText` 那两颗）。本次**一个 rect 常量都没改**、两档共用同一组归一化比例
   ⇒ `A1284` 仍**开着**。⚠️ 我这次**只**做了「面板那一级」的回代（`RT_3104` 逐字段，见 §四·2），**7 颗子件的逐颗回代没做**（不在本件范围，且那个工具对被锚拉伸的祖先断链）。
4. **`HideAbilityButton` 没建**（原版那块 `RaycastTarget=1` 的透明捕获区）—— 它是**输入那一档**（`T09` 第 4 步）的活；
   本次按「本扇零射线件」处理，**没有**给任何件开 `raycastTarget`（与 §三 硬要求「查到不一致就点出来」对照：**不存在不一致**，因为我们**没有任何带命中的件**）。
5. **原版 7 颗子件自身的 `anchorMin/Max` 没读**（我只用了我们既有的归一化比例）—— 要落 §五·1 那个正解才有用，本次没读（`_probe_node_rect.py` 对被锚拉伸的祖先断链，得换读法：直接按 `m_Children` 逐颗现读 `RectTransform_*.json`）。
6. **没跑果**：本件**只跑过秒级类型检查**（现行「先复刻后检查」口径）。画布档在**真机**上的落点/手感/三档宽高比并排 —— **未验**（属收尾那次统一验收）。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **`BattleDriver.cs:14430` 的 `EnsureSubCanvas` 之前没有 `HudCanvas.Ensure`** —— 它靠 `:14073 BuildEnergyLights()` → `AddEnergyLight`（→ `:16171 HudCanvas.Ensure`）兜；
   而 `BuildEnergyLights` 在 Glow 贴图取不到时会**提前 return** ⇒ 那一档 `RootRect == null` ⇒ `AboveShader` 子画布**静默拿不到**（退回 HUD 根 + 一条 error 日志）。
   建议在那句前面补 `HudCanvas.Ensure(LayoutSpace.Cam);`（幂等、零副作用；`MulliganPanel.Create` 就是这么写的）。**同上文 ③·1 那句**。
2. ⚠️ **`HudCanvas.Group` 的僵尸条目又多一条触发路径**：`UseHost` 在 `Build()` 之后被调时走 `Teardown()` ⇒ 旧件被销毁、**z 账里的条目留着**
   （既有账 `T09` §七·4 的 `PruneDestroyed`）。今天无害（同一个容器里新条目的相对次序仍对），多局/换档会积累。
3. ⚠️ **`HudCanvas.SubCanvasRank` 的注释（`:464`）已过期**（写着 `SkillPanel` 还没迁）—— 见 ③·2。
4. ⚠️ **批处理构图的提醒**（不是缺陷，但会被误读成缺陷）：`Screen = 640×480`（4:3）而 `cam.aspect` 钉 16:9 ⇒ 画布是 **1920×1440** 单位
   ⇒ 画布档的东西**按 4:3 的口径**排版（面板 ≈ 22.6% 屏高、中心 15.2%）。**真机 16:9 下才与今天逐位相同**。
   ⇒ `06c_技能卡面板` 那张截图**不能与改之前并排比**；要比就按 `T09` §五 那两条（真机三档 + `wf_click.py`）。
   🔴 **2026-10-16 更正（见文首「更正②」）**：本条**方向反了** —— 「不能并排比」的**是改之后**（改之前画布档写死 16:9，那两张截图**能**比）。
5. ✅ **好消息**：本扇**没有**带入任何新的「同一条规矩的第二份写法」—— 两个可能长出权宜件的口（`ImageInParent` / 直写 `sizeDelta`）
   我用的都是 `HudCanvas` 现成的 `MakeImageInParent`（§七·10 那条欠账本扇**不欠**）。

---

## 可直接粘进总表的一行

| `T09` 批 3 | `SkillPanel`（原版 `ActiveSkillDesc`，`OverHand`/**0**/兄弟序 **1/14**）**代码已改完**（`Battle/SkillPanel.cs`，双模式：不登记宿主 = 世界档逐位不变），typecheck 0 错。⏭ **差三步接线（白名单外）**：① `BattleDriver.BuildHud` 里 `skillPanel.UseHost(HudCanvas.EnsureSubCanvas(null, SkillPanel.SubCanvasName, OverHand, 0))` ② `HudCanvas.SubCanvasRank` 的 `OverHand` 段**最前**插 `"ActiveSkillDesc Canvas"`（判据 = 原版子件序 1 < 3）③ `BattleScene.cs` 新增 7 条断言（`OnCanvas` 灭自证那两条必须有）。🔴 **一条待裁**：画布档按固定设计 px 给面板矩形，**4:3 下只有原版的 75%**（22.6% vs 30.2% 屏高，正解 = 按 `RT_3104` 的锚点拉伸，**全批 5 扇同病**）→ 见报告 §五·1 |
