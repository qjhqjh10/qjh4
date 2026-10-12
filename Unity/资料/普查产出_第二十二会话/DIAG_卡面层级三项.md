# DIAG 卡面层级三项

> 只读诊断（`A1658` / `A1659` / `A1665` 三合一）。**没跑 Unity、没改任何生产代码**。
> 层号坐标约定（本文件全篇）：**z 越小 = 离相机越近 = 画在上面**（依据：`_art`+0.03 → `frame`0 → `_info`−0.02 → 文字−0.03，且 `HandLayout` 的「左边的压在上面」= z 随序号变大）。

## 一、结论

**(a) 确认缺陷 —— 值错，不是缺层。** 那层 = `CardView._textBg`（`AddLayer("textBg", …)`），z = `TextBgZ = **−0.012**`。
在我们的约定下 −0.012 比卡框(0) **更靠相机** ⇒ 底板压在卡框**之上**。原版子序（底→顶）是
`Card Highlight And Shadow → CardImage → **Textbackgrounds** → **CardFrame** → Card Info` ⇒ 底板应在卡框**之下**。**确认**。
附带一条更要紧的事实：**全卡面没有任何显式 renderQueue** ——「谁设这个队列」的答案是**没人设**，
层序完全交给「到相机的 3D 距离」排序（见二·1）。

**(b) 不是缺陷（不是「画在两处/少一层」那类分层问题）。** 图标与文字**在同一条 TMP 网格里**：图标 = `<sprite>` 行内字形，
TMP 为它单独建的 `TMP_SubMesh` 子物体的 `localPosition=0 / localRotation=identity / localScale=1`
⇒ **同一父物体、同一 z、同一默认队列 3000**。所以「不在一个层面」不可能来自「各画一层」。
真因候选三条（都可查、见二·2）：① 🔴 **卡面文字那几层根本没挂到倾摆节点 `2DCard` 下**（挂在 `transform`，而所有 quad 层挂在 `FaceRoot`）⇒ 倾摆非零时文字/图标与底板/卡框**真的分成两个平面**；② 图标是**独立渲染器**、且**吃不到文字那 0.05 的描边**（材质是 `TextMeshPro/Sprite`）；③ 图标大小是拿**成品印刷卡图**标定的（`CalibScale=0.776`，目标 图标÷大写=2.0）。

**(c) 确认缺陷（缺一整套机制）。** 卡与卡之间没有层级机制：手牌的重叠只靠 `HandLayout.zOrderStep=0.08`（真实卡内 z 跨度是 **0.09** > 0.08，注释里写的 0.06 漏了 `shadow` 层），而场上各格 z **恒为 0**（卡间零分离）。
全工程唯一「每张卡演一段队列」的机制 = `CardFan.SetCardQueue`，**只有 4 个窗口在用，手牌/场上都没用**。

## 二、证据

### 1. 层与 z（`CardPresentation/Core/CardView.cs`）
- `AddLayer` `:2297-2313`：只写 `go.transform.localPosition = (0,0,z)` + `new Material(BaseMaterial())`，
  `BaseMaterial()` = `new Material(Shader.Find("Sprites/Default"))` `:3156-3158`（默认 **3000**）⇒ **建层处从不设队列**。
- `TextBgZ = -0.012f` `:427`；上方注释 `:416-426` 自陈：「**不能照原版的卡框之下摆**（会挡住 opt-in 的前景层 `_artFront`）」
  「🔴 本值**仍留在 −0.012（即画在卡框之前）**」「**还没查清**：出厂态下这块 64.7% 的黑底会不会压暗卡框自己那一圈」。
- 同一空间（`FaceRoot`）各层 z：`shadow` **+0.06**（`:2388`）· `art` +0.03 · `frame` **0** · `textBg` −0.012 ·
  `costBg`/`armourIcon` −0.015 · `gem` −0.01 · `info` −0.02 · 文字 `TextZ` **−0.03**（`:466`）；
  徽标 −0.095/−0.10/−0.105（`:1372-1374`，**只在场上**）。
- 底板两个尺寸/α：`:413-415`（W 1.7766 / H 1.55 / α **0.647**）；建层 `:1689-1693`；形态开关 `:779 Show(_textBg, !board)`。
- 层序说明（我们的口径）：`:379-382` 写明原版子序与「我们取 +0.015（在 art 与 frame 之间）」——**现读是 −0.012**。

### 2. 图标与文字的落层
- 图标来源：`Core/CardIcons.cs:186 Rewrite(...)` 把记号换成 `<sprite name=…>`；`Core/TmpFont.cs:200` 统一挂 `t.spriteAsset`。
- 卡面 TMP 建在 **`transform`**：`CardView.cs:2159 t = TmpFont.NewText(transform, name, …)`（摆放 `PlaceAt(..., TextZ)` `:2176-2177`）；
  角标计数 TMP 同样 `:1420`。
- 而**所有 quad 层挂在 `FaceRoot`**：`:2301`（`AddLayer`）· `:1465`（徽标底板）· `:1782`（环）· `:5059`（四个数值层）；
  `FaceRoot` = 子节点 `2DCard`（`:644`、`:1596-1605`），**倾摆 `AutoCardRotation` 就挂在它身上**（位移中 ≤±10°、停手 2 秒回正，`Core/AutoCardRotation.cs:1-27`）。
  ⇒ 🔴 **文字/图标不跟倾摆，底板/卡框/立绘跟** —— 这就是字面上的「不在一个层面」。（顺手发现，非本轮三问的预设口径）
- TMP 精灵子网格的做法（TMP 侧，非我们代码）：`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_SubMesh.cs:234-252`
  `AddSubTextObject`：新建子 GO、`localPosition=Vector3.zero / localRotation=identity / localScale=one`、
  **自带 MeshRenderer**（所以它**单独参与距离排序**）、材质 = sprite asset 的材质。
- 图标材质：`Assets/CardPresentation/Resources/Fonts/Warpforge Trait TextSprites.asset:11147-11149`
  （`m_Name: Warpforge Trait TextSprites Material`、shader guid `cf81…` = `TextMesh Pro/Shaders/TMP_Sprite.shader`、
  `m_CustomRenderQueue: -1`；该 shader `:25 "Queue"="Transparent"`）。
  文字材质走 `TmpFont.ApplyOutline`（`TmpFont.cs:158-178`；调用 `CardView.cs:2043-2046`）：`OUTLINE_ON` + 宽 **0.05** 黑 ⇒ **图标吃不到**。
- 图标尺寸标定：`Editor/IconSetup.cs:57-88 CalibScale = 0.776f`（目标 = 图标墨迹÷拉丁大写 = **2.0**、图标=卡高 4.0%），
  **尺子 = `D:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`（成品印刷卡图）**。

### 3. 队列机制（唯一那一套 + 手牌/场上的现状）
- `Core/CardFan.cs:128-136 SetCardQueue`：把整棵子树**所有 MeshRenderer**（跳 `Label`）与 `Label` 全设成同一个 `q`；
  `QueueFor` `:142`、`ApplyQueues` `:148-153`、基数 `QCardBase = 3009` `:81`。**调用点只有 4 处**：
  `Battle/CardDisplayWindow.cs:690,736`（战斗「点卡看详情」）· `Shell/CardDetailPopup.cs:487,580`（菜单详情窗，`QCardStack=3106`，
  `Shell/CardDetailPopup.cs:143`）· `Shell/BoosterPackOpenWindow.cs:458` · `Battle/BattleDriver.cs:6358`（日志悬停卡）。
  ⚠️ 手牌/场上**一处都不调**。
- 手牌：`Hand/HandLayout.cs:611 z = i * zOrderStep - (hovered ? frontZ : 0)`，`zOrderStep = 0.08f` `:215`；
  注释 `:211-215` 自陈「**必须大于卡内部各层 z 跨度（0.06）**…否则后面那张卡的卡面文字会穿到前面那张卡上面」（2026-09-12 实测过——
  **同一个症状，用户今天又报了一次**）；`:216-237` 自陈「原版靠**嵌套 Canvas 的 sortingOrder + SetSiblingIndex**，
  我们世界空间 quad ⇒ **z 是唯一手段**」，并写明正确做法是「**让每张卡的所有图层共用一段 render queue**（`ImageQuad.SetRenderQueue`）
  —— 那要动 `CardView` 的分层」⇒ **这笔账当时没做**。
- 跨度探针：`Editor/CardBaseDemo.cs:1655-1673 LocalZSpan(...)`，`:1591-1594` **只打诊断、不断言**（理由自陈：不能跑 Unity 实测那个跨度）。
  现读卡内层集合 ⇒ 跨度 = 0.06 − (−0.03) = **0.09**（`shadow` 层 `:2388` +0.06 没被那条注释算进去）。
- 场上格位：`Board/BoardLayout.cs:364 SlotPosition = LayoutSpace.ToWorld(NxOf(slot), lineY)`；
  `Core/LayoutSpace.cs:78-83 ToWorld(...) → z = 0f` ⇒ **2D 那条路各格共面**，卡间 z 分离 = 0。
  （3D 那条路走 `ArenaSlots.TryResolveSlot`，本轮没读。）

## 三、我们 vs 原版

| 项 | 我们值 | 原版值 | 出处 |
|---|---|---|---|
| 效果文字底板 z | **−0.012**（压住卡框） | 子序在 `CardImage` 与 `CardFrame` **之间**（= 卡框之下） | 原版子序：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md:308`（D1「CardImage < CardFrame < 数值」那一段 + `:273` 层序行）；我们这一侧 `CardView.cs:379-382,417-427` |
| 底板尺寸/α/sprite | 1.7766×1.55 / 1.8635×0.9621 · α0.647 · `Card Text smooth background` | 同（照抄） | `CardView.cs:372-377,413-415`；`子代理读报_2dcard_0827.md:270` |
| 层间载体 | **只有 z**（材质默认 queue 3000，建层处不设队列） | **嵌套 Canvas `sortingOrder` + `SetSiblingIndex`**（`CardScript.SetSortingOrder`） | 我们：`CardView.cs:2297-2313`；原版：`HandLayout.cs:216-237` 引的反编译（`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:55` 等） |
| 每张卡一段队列 | 只有 `CardFan.SetCardQueue`（4 个窗口） | 每张卡一个 `sortingOrder`（名字长度−序号−1） | 同上 |
| 手牌卡间 z 步长 | 0.08（须 > 卡内跨度，实为 0.09） | `useZOrder` 加量 0.001 ≈ 0.015 px，**不是分层手段** | `HandLayout.cs:211-237`（引 `CardsHorizontalLayout..cctor` `Z_OFFSET_BY_POSITION_INDEX=0x3a83126f` 与 `…GetPosition.c` 末句） |
| 场上格位 z | 2D 路恒 0；3D 路 = 竞技场地形 | 场上卡在 3D 卡体上（`2DCard` 整关） | `BoardLayout.cs:364` · `LayoutSpace.cs:78-83` · `CardView.cs:752-768` |
| 图标与文字 | **同一 TMP 网格**，图标走 `TMP_SubMesh` 子物体（同 pos/rot/scale，自带渲染器） | 同构（原版也是 TMP 行内 sprite；`DescTextUnit` 的 `m_spriteAsset` 是空 ⇒ 吃 TMP Settings 的默认 sprite asset） | `TMP_SubMesh.cs:234-252` · 我们 `IconSetup.cs:1-14`（引 `bundle_menus_assets_all` 14 个组件 `m_text` 写 `<sprite name=…>`）· 原版组件 `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_1782256530001533888.json`（`m_spriteAsset` PathID 0、`m_fontSize 23.55`、`m_VerticalAlignment 512`） |
| 图标材质/队列 | `TextMeshPro/Sprite`、queue 3000、无描边（文字有 0.05 黑描边） | 同名同 shader 同 queue（`m_CustomRenderQueue −1`） | 我们 `…Trait TextSprites.asset:11147-11149`；原版 `bundle_fonts_assets_all/Material/Material_1211625883834822624.json`（`m_Name: TextMeshPro/Sprite`、`m_CustomRenderQueue −1`） |
| 图标大小 | 图标÷大写 = **2.0**（`CalibScale 0.776`），尺子 = **印刷品** | **未核**（数字版那一侧无判据） | `IconSetup.cs:57-88`；铁律 10 第 3 条（PnP 是印刷品，不能当版式尺子） |

## 四、修法建议

**(a) 把底板挪回卡框之下。** 最小改动 = `TextBgZ` 由 `−0.012` 改回 **≈ +0.015**（`art`+0.03 与 `frame`0 之间；`:381` 那段注释记的就是这个值）。
改哪里：`CardView.cs:427`。风险：① 出厂关着的 `_artFront`(−0.008) 与底板的前后关系会变（`:417-420` 担心的正是它；按 z 表 `_artFront` 恒比新底板靠前，理论上不受影响，但**要并排渲一张核**——铁律 10 第 6 条）；② 别只改 z 忘了同族的 y 那笔账（`:388-403`）。
更彻底（与 (c) 同一改法）：给卡面各层**显式 renderQueue**，底板给一段低于卡框的号 —— 那才是本工程自己定的分层手段（铁律「分层要用 `SetRenderQueue`，不能用 z」）。

**(b) 两件事分开做。** ① **把 TMP 文字层从 `transform` 改挂 `FaceRoot`**（`CardView.cs:2159` 与 `:1420` 两处）——
这才是「文字/图标与卡面同面」的机制修法；风险 = 动共用件、影响**全部**卡面文字与角标（静止时 `2DCard` 是 identity ⇒ 位置不变），
**必须配一条「倾摆非零时文字与底板仍同面」的断言**，并并排渲一张倾摆态。
② 图标「像贴纸」那条**先别动**：原版同样是「文字材质带描边 / 图标材质 `TextMeshPro/Sprite` 不带」——
要消掉它得换自建图标 shader，那是**超出原版**的改动。③ 图标尺寸若要改，先给「数字版图标÷大写」找判据（现判据是印刷品，按铁律 10 第 3 条不能当版式尺子）。

**(c) 把 `CardFan.SetCardQueue` 那套搬进 `CardView` 内部**：每张卡实例给一段固定队列基 `q`，
卡内各层固定 `q + 偏移`（文字最高 → 底板 → 数值 → 卡框 → 立绘 → 阴影），卡间由布局给不同的 `q`（手牌/场上各一段基数）。
改哪里：`CardView` 建层处（`AddLayer`/`BuildTextLayers`）＋ 布局给的序号（`HandLayout.Place`/`BoardLayout`）。
风险：① **共用件**（每张卡都受影响，要和 `CardFan` 已用的 3009 / 3106 段对齐，⛔ 别两处各写一套）；
② 一旦走队列，`z` 就只用于几何、不再管层序 ⇒ 要同时清掉 `zOrderStep` 那条「必须大于跨度」的隐含契约（或至少改成断言）。
③ 修的时候把 (a)(b) 一起判，别各修各的（三问同族）。

## 五、没查清的部分

1. **用户当时看的是哪个视图**（手牌 / 场上 / 详情窗）—— **没有截图**。本报告按代码与层表推断「手牌」是 (c) 的主要现场；
   场上按设计不重叠（`BoardLayout.cs:25-55`：格距 149.3 px > 卡宽 137.2 px），但**3D 那条路的实际屏幕重叠没量过**。
2. **TMP 那个图标子渲染器与主文字渲染器在距离排序下的实际先后**（是否真会落到文字底板两侧）—— **只读查不出**，
   要真机渲一张并读帧调试/RenderQueue。
3. **数字版原版的「图标÷大写」比**（我们目标是 2.0，尺子是印刷品）—— **没核**。原版卡面进不去（关服、deck 链路无实拍），
   要核得走「跑原版并拍到卡面」那条路。
4. `use3D` 那条路各格的 z 是否共面（本轮只读了 2D 的 `ToWorld` z=0；3D 走 `ArenaSlots`）。
5. 倾摆非零时文字与底板**实际错开多少**（只按 `AutoCardRotation` 的 ±10° 估算，**没渲图实测**）。
