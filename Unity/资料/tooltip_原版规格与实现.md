# tooltip（悬停信息层）—— 原版规格与我们的实现

> 建立：2026-09-21。**动手前先读这份**（规格部分来自全量反编译，别再重查）。
> 代码：`Assets/CardPresentation/Core/Tooltip.cs`（面板）+ `Battle/BattleDriver.cs` 的 `TickTooltip`
> + `Deck/DeckRuntime.cs` 的 `TickTooltip`（卡组编辑那边也挂了）+ `Core/CardView.cs` 的 `StatAt/StatWorld`。

---

## 一、原版规格（**逐条来自全量反编译**，出处 `d:/2/tools/decomp_full/`）

| 事 | 原版怎么做 | 出处 |
|---|---|---|
| 谁显示 | 触发器 `EverguildTooltipTrigger`：`OnPointerEnter` **立刻** Show · `OnPointerExit` **立刻** Hide（**没有延迟**） | `EverguildTooltipTrigger__OnPointerEnter.c:5` / `__OnPointerExit.c:5` |
| 另一个隐藏时机 | `Manager.Update`：鼠标**左键按下**且落点不是当前触发器 ⇒ Hide | `EverguildTooltipManager__Update.c:14-47` |
| 位置 | = **触发器自己的 `transform.position`**（**不跟鼠标**）+ prefab 的 `defaultOffset` + `data.Offset` | `EverguildTooltipTrigger__Show.c:44-49,88` |
| 朝向 | `rt.pivot = GetPivotPosition(TooltipAnchor)`，**9 项表**；未知值回落 (0.5,0.5) + `[WARNING] %s not implemented` | `EverguildTooltipItem__GetPivotPosition.c:14-63` |
| 动画 | **只有 alpha**：`DOFade(1, 0.3)` / `DOFade(0, 0.3)`（DOTween 默认 OutQuad）。**没有缩放、没有位移** | `__ShowAnimation.c:15-18` · `ANIMATION_TIME = 0.3f`（桩 `.cs:10`、.rdata 实测 0.3f） |
| 边缘翻面 | **没有** —— 全量反编译里**零处**用到 `Screen.width/height/safeArea` | 全文件 grep 无命中 |
| 面板挂点 | `TooltipMountPoint = override ?? manager.transform`（场景里没有 override ⇒ 就是管理器自己） | `EverguildTooltipManager__get_TooltipMountPoint.c:12-20` |

### 面板本体（**本地就有**）

- prefab：`assets_full/bundle_duplicateassetisolation_assets_all/GameObject/BasicToolTip.json`
  （脚本类 `EverguildTooltipItem`；另有 4 个变体 `Battle Tooltip Variant` / `Trait Tooltip` /
  `Updated Tooltip Variant` / `Updated Tooltip Variant - ranked`）
- 子物体：`Background` / `Title` / `Text (TMP)` / `Content` / `Line` / `Mask`
- 底图 = **`Smooth background square`**（32×32、**`m_Border = (12,2,12,12)`** ⇒ 九宫格）
  + `40k_Smooth shadow background`（102×102、`m_Border = (50,50,50,50)`）
- 正文 TMP 实测：**字号 28**（auto-size 10..28）、**纯白**、`HAlign=2`(Center)
- ⚠️ **基础 tooltip 只有正文、没有标题**（带标题的是 `EverguildTooltipWithTitle` / Trait 版）

### 🔴 一条**更正**（2026-09-20 前一直记错的）

`Tips/HealthTip` · `Tips/CostTip` · `Tips/MeleeAttackTip` · `Tips/RangedAttackTip` · `Tips/Hud/Skulls` …
**不是预制体名、是 I2 本地化 key** —— 它们只出现在触发器的 `text` 字段里，
而 `tooltipPrefab` 字段**全部指向同一个** tooltip 预制体。
实据：`assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour/MonoBehaviour_-4253307515847882232.json:17`
（`"text": "Tips/HealthTip"` 与同文件的 `"tooltipPrefab": {"m_PathID": -3209907009652840533}`）。

### 挂点（谁挂了触发器）

| 挂点 | 本地化 key | anchor | offset | 出处 |
|---|---|---|---|---|
| `Health Container` | `Tips/HealthTip` | **10** | (73.05, 0) | `子代理读报_2dcard_0827.md:93` |
| `Range Attack Container` | `Tips/RangedAttackTip` | **15** | (−53.54, 0) | 同上 `:98` |
| `Melee Attack Container` | `Tips/MeleeAttackTip` | **15** | (−49.33, 0) | 同上 `:100` |
| `Cost Container` | `Tips/CostTip` | **10** | (52.6, 0) | 同上 `:102` |
| 🔴 `Armour Container` | **没有** | — | — | 同上 `:95`「**此容器无任何脚本/无 tooltip**」 |
| `Milestones`（HUD） | `Tips/Hud/Skulls` | 5 | (0, 90, 0) | `子代理读报_back左区_0827.md:56,161` |
| 敌/我 能量 · 信仰 · 灵魂石 · 任务点 | `Tips/Hud/{Opponent,Player}{Energy,Faith,SpiritStone,QP}Count` | — | — | `子代理读报_back右区_0827.md:130-139` |

---

## 二、🔴 文案：**我们写的那一份，不是原版**

原版 tooltip 的文字走本地化 term，而 **I2 的词条表在远端 CCD**：
`assets_full` 全量扫过 —— **只有 key、一个 value 都没有**
（`I2Languages` 零命中 · `*.csv/*.tsv` 零命中 · `LanguageSourceData` 的序列化字段零命中 ·
`LanguageRemoteAsset` + `AddressableLocalizationLibrary.GetLocalHandle` 用 `Addressables.LoadAssetAsync` 远端取）。

⇒ `Tooltip.cs` 里的 **`TipText`** 每一句**照规则书**写（行号在注释里），
**拿到原版词条表之后整张表换掉即可**（接口不变）。
**别把这里的文案当成「原版这么说」。**

---

## 三、仍然是我们挑的（原版查不到，如实标）

- **内边距 / 最小宽高 / 行高**：原版是 uGUI 布局算的，我们这套是世界空间 quad ⇒ `PadX/PadY/MinW/MaxW/LineH` 是我们挑的。
- **手动折行**：我们的 `Label` 不自动换行 ⇒ 按**字符数**折（CJK 等宽）。
- **数值命中半径** `CardView.StatHitR = 0.22`：原版靠**容器 rect**（节点树里是 `0.4×0.4` 卡单位）。
- **offset 的换算**：原版是 UI 空间 px，我们按 **108 px/单位**折成世界单位 —— 这一步是近似。

---

## 四、🔴 做的时候踩的两个坑（**都是「面板在、字不在」**）

1. **`ForceMeshUpdate()` 在对象没激活时量不出尺寸** —— `Show()` 里原来先 `Layout()`（里面量文字）
   **再** `SetActive(true)` ⇒ TMP 报出来的 `textBounds` 是**垃圾**（`tmpW = 4.29e9`），
   面板宽度顶到上限、**一个字都看不见**。**先激活、再排版**。
   判据：`Tooltip.DebugDump()` / `Tooltip.PanelSizePx.x > 0`。
2. **`Label.SetRenderQueue` 在点阵后端下什么都不做** —— 它原来只写 `_tmp.fontMaterial.renderQueue`，
   用点阵渲染的文字**永远留在默认队列 3000**、被队列更大的面板盖住。已补 `_mr.sharedMaterial.renderQueue`。

---

## 五、还欠什么

- ✅ **2026-09-21 做完：Trait tooltip（关键词那套）**。
  原版 = `EverguildTraitTooltipItem`（比基础版多 **图标 + 标题**），链子**整条照抄**：
  关键词段整项套 `<link=<枚举名>>`（`GameStaticData__TraitNameToString.c:84-109`）
  → `TextTooltipController` 每帧 `TMP_TextUtilities.FindIntersectingLink` 命中
  → `EverguildTraitTooltipItem`。
  **我们的落点**：
  · 卡面这一侧 = `Core/CardText.cs` 的 `KeywordSegment`（每一项套 `<link=规范键>`）
  · 命中 = `Core/CardView.cs` 的 `LinkAt/KeywordAnchor/KeywordRect` → `Core/TmpFont.cs` 的 `TmpFont.LinkAt`
    （**用 TMP 自己的 `FindIntersectingLink`，不自己算版面**；判据只此一份，`Label.LinkAt` 也转发到它）
  · 文案 = `TipText.Trait(key)`，**表是生成的**：`工具/gen_trait_tips.py` 把
    `资料/关键词图标/_规则书关键词表.md`（规则书 `:161-225` 的 61 条）写成 `Resources/trait_tips.json`
    —— **不手抄进 C#**（61 条手抄迟早和规则书对不上）
  · 触发点 = `Battle/BattleDriver.cs` 的 `TickTraitTip`（挂在 `TickTooltipAt` 里，**先于数值 tooltip 判**）
  **三条断言**（`Editor/BattleScene.cs` §15c）：文案表 61 条 · 标题行带图标 · **端到端**
  （扫手牌关键词层的包围盒 → 命中的 `<link>` → `TickTooltipAt` 弹出的就是那一条，截图 `30_悬停关键词tooltip`）。
  ⚠️ **两处如实标「我们的做法」**：① 原版标题是**独立的 `TraitSprite` + `titleText` 两个对象**，
  我们做成**正文的第一行**（`Label` 那条路本来就支持行内 `<sprite>`）—— 内容一样、少两套版面；
  ② 规则书 61 条里没有的词（自造词 `ability`）**只出名字 + 明写「（规则书里没有这个词的条目）」**，
  **不编一句解释**（原版查不到描述时也是「只有图标 + 标题」）。
  ⚠️ **场上那套验不了** —— `CardFace.Board` 把整个 `2DCard` 关掉了（只剩攻血甲 + 徽标），
  关键词层没显示 ⇒ 端到端只能在**手牌**上验（`CardView.LinkAt` 里有一条 `activeInHierarchy` 守卫，
  防「鼠标划过看不见的字也弹 tooltip」）。
- ✅ **2026-09-21 做完：文字内联链接（效果正文那一半）**。
  **做法比原版便宜**：原版正文是本地化串、`[[枚举名]]` 由 `ModifyLocalization` 展开，
  我们**不用做词形识别** —— `CardIcons.Rewrite` 本来就要把那些行内 token 换成 `<sprite>`，
  **在同一处顺手包一层 `<link=Badges.KeyOf(图名)>`** 即可（幂等靠同一个 `tag` 变量；
  裸关键词那条把「图 + 紧跟的词」**整段**包进去，与原版 `<link><nobr>图+词</nobr></link>` 同构）。
  非关键词的那两个资源图走 `TipText.ByLink` 分派到现成的 `Melee` / `Ranged` / `QuestPoints`
  （`faith` / `spiritstone` / `sabotage` **本身就是关键词**，直接走 `Trait` 更全）。
  **覆盖断言**（`BattleScene.cs` §15c）：全池扫 `desc`、正文里带 `<link>` 的卡数 > 100。
  ⚠️ **两个坑都是这轮踩的，已修**：`CardIcons.StripTags` 原来只剥 `<sprite>`（新标签会原样印到点阵兜底画面上）·
  `Tooltip.Wrap` 原来**逐字符**折行（会把 `<sprite name="codex">` 从中间插 `\n` 掐成两半）。
- **HUD 只挂了骷髅 / 能量 / 任务点 / 信仰 / 灵魂石**（用现有 `ImageQuad` 的 `Contains` 判），
  原版还有玩家/对手的能量与上面那四种 —— 我们这边**没显示就不挂**（和显隐判据一致）。
- **没在真 Play 里用鼠标试过**（批处理走的是 `TickTooltipAt(world)` 那条同一函数）。
