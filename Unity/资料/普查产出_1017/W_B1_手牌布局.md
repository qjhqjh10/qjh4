# W_B1 手牌布局（战斗侧第一批）—— 交接报告

> 批：**B1** · 2026-10-17 · 写手代理 · 唯一判据文件 = `资料/手牌布局_原版算法与参数.md`（§七 的 12 项对账）
> 本批做**开着的 5 项**（7 / 8 / 9 / 11 / 12）。改动**只有两个文件**：
> `Assets/CardPresentation/Hand/HandLayout.cs` · `Assets/CardPresentation/Editor/CardBaseDemo.cs`

## ① 结论（逐项：原版 / 我们原来 / 改成什么 / 判据出处）

| # | 原版 | 我们原来 | 结果 | 判据 |
|---|---|---|---|---|
| **7** `useZOrder` | z 只加 **0.001 世界单位 ≈ 0.015 px**；**分层根本不用 z** | `zOrderStep = 0.08`（自造，必须 > 卡内层 z 跨度） | ➖ **「原版没有等价物」= 不适用**，保留 0.08，注释补全判据 + 加「z 随序号单调靠后」断言 | 见下 |
| **8** 选中让位 | **装得下就把 `extra` 归零** ⇒ **只有压缩态（我方 ≥8 张）才让位** | 🔴 **门闸反了**（`!IsCompressed` ⇒ ≤7 张才让），且没有 `useExtraSpaceOnSelectedCard` 这道门 | ✅ **落地**（方向改正 + 补字段 + 带空位时按槽位空间比序号） | `CardsHorizontalLayout__GetPosition.c:201-214` · `PlayerHand__PositionCardInHand.c:28-30` |
| **9** 小屏档 | `GameStaticData.smallScreenUI ? m_maxLayoutSizeSmallScreen : m_maxLayoutSize`（**与屏宽无关**） | 按「可见宽 < 设计宽」猜 | ✅ **落地**：读 `SmallScreenUI.Enabled`（工程里已有的等价物） | `CardsHorizontalLayout__GetPosition.c:83-88` |
| **11** `GetClosestInHandSlot` | **逐卡比 `\|指针.x − 卡.x\|` 取最近**；严格小于才换（平局取小序号）；初值 **1e6** | 按阈值累加（分界落在**卡中心**上） | ✅ **落地**（分界改到两卡**中点**；候选槽数同时修正为 `others + 1`） | `CardsHorizontalLayout__GetClosestInHandSlot.c:14-33`（初值 `.rdata 0x1834b3354` 实读 = 1e6 · 掩码 `0x1834b2e60` = 0x7FFFFFFF） |
| **12** `m_inverted` | 翻序号 | 未实现 | ➖ **全库查过：52/52 = 0 ⇒ 维持现状**（注释里写明） | 见 ③ |

### 项 7 查证结论（「原版怎么解决」= 有确凿判据，不是「查不清」）

- 那 0.001（`CardsHorizontalLayout..cctor` 的 `0x3a83126f`）加在**世界位置 z** 上，**排不出前后**，UGUI 也不按 z 排序。
- **原版真正的分层 = 嵌套 Canvas 的 `sortingOrder` + 兄弟序**：
  `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:55`
  `CardScript.SetSortingOrder(名单长度 − 序号 − 1)` → `CardScript__SetSortingOrder.c:6`
  → `BattleCardUI__SetSortingOrder.c:8,11`：`Canvas.set_sortingOrder(order)` **且** `Transform.SetSiblingIndex(order)`。
  ⇒ 序号越大排序越靠后 ⇒ **左边压上面** —— 与我们 `z = i * zOrderStep`（z 越大越远）**语义一致，只是载体不同**。
- 我们是世界空间 `ImageQuad`，**没有 canvas 排序** ⇒ `z` 是唯一手段 ⇒ **照抄 0.001 = 全部同层、卡面文字横穿邻牌**（2026-09-12 实测过）。
  ⇒ 这一格按铁律 3 标注为「**不是原版的做法**」，并写进代码。⚠️ 真要换成原版那种手段，得让每张卡的**所有图层**共用一段 render queue（`ImageQuad.SetRenderQueue`）—— 那要动 `Core/CardView.cs`，**不在本批白名单**。

## ② 改动清单（含断言）

**`Hand/HandLayout.cs`**（+139 / −26 行）

1. **项 9**：`MaxLayoutWorld()` 的 `small` 判据 `VisibleWidth < DesignWidth` → **`SmallScreenUI.Enabled`**；方法改 `public`（自检要读它）；`maxLayoutSizeSmallScreen` 的 Tooltip 一并订正。
2. **项 11**：`GetClosestInHandSlot` 重写为 `argmin|Δx|`：候选槽 = `cardsInHand + 1` 个（与 `Refresh` 的 `slots = n + 1` **同一套** —— 旧写法用 `cardsInHand` 个位置，**与 Refresh 差一档间距口径**）；初值常量 `ClosestSlotInitialDistance = 1e6f`；平局取小序号（照原版严格小于）。
3. **项 8**：`Refresh` 里那道门 `!IsCompressed(slots)` → **`IsCompressed(slots)`**；补 `useExtraSpaceOnSelectedCard` 字段（默认 true，`ConfigureForEnemy()` 置 false）；让位比较改用**槽位空间**的 `selSlot`（带空位时 `slot` 会整体错开 1）。
4. **项 7**：`zOrderStep` 的注释整段重写（补齐上面那套判据，明确「不适用」而非「我们挑的、理由模糊」）。
5. **项 12**：`invertRotation` 下面加一段注释：全库 52 份实例 `m_inverted` 全 0 ⇒ 不实现；判据备份 `GetPosition.c:55`。
6. 文件头补一段「2026-10-17 B1 批收了哪四处」。

**`Editor/CardBaseDemo.cs`**（+203 / −0 行）—— 新增一节 `AssertHandLayout(cam)` + 两个小工具（`LocalZSpan` / `WalkZ`），共 **15 条断言**，跑在 `SaveScene` **之后**、探针自建自拆：

| 断言 | 判别式（把哪一处改回去 ⇒ 本行变红） |
|---|---|
| 小屏开关开着 ⇒ 上限切 `m_maxLayoutSizeSmallScreen`（差值 = `(0.59−0.51)×可见宽`） | —— |
| **4:3 不是触发条件**（可见宽 < 设计宽，但开关关着 ⇒ 仍走 0.59） | 旧「按可见宽猜」在这一格走 0.51 |
| **超宽 + 开关开着 ⇒ 仍走小屏档** | 旧「按可见宽猜」在这一格走 0.59 |
| 最近空位：最左 ⇒ 0 / 最右 ⇒ `others`（不是 `others+1`） | —— |
| 槽中心 +30% 间距 ⇒ 仍判这一个槽（8 个槽全测） | 旧「一过卡中心就 +1」⇒ 整排错成下一个槽 |
| 槽中心 +70% 间距 ⇒ 判下一个槽（末端夹住） | —— |
| 槽 2/3 中点左 0.01 ⇒ 2、右 0.01 ⇒ 3 | 旧写法左边那半就判 3 |
| 层序 z 随序号单调靠后（= 原版 `sortingOrder = 长度−序号−1`） | `zOrderStep` 变号 ⇒ 变「右压左」 |
| 让位量 = 原版 **2.0 世界单位**（按 `OurUnitsPerWorldUnit` 算，**不读我们的字段**） | 把 `selectedCardExtra` 清零 ⇒ 红（灭自证） |
| **压缩态（8 张）⇒ 两侧整半边各让 2.0 世界单位** | 只挪紧邻那张（自造 `neighborShift`）⇒ 红 |
| **装得下（5 张）⇒ 一点不让** | 门闸改回 `!IsCompressed` ⇒ 红 |
| 悬停抬起 = **30 px** / 放大 = **1.3** | `hoverLift` 改回 0.06 世界单位（6.5 px）⇒ 红 |
| 敌方那份 `useExtraSpaceOnSelectedCard = 0` | —— |

- 层序那条另有一行**只打诊断、不断言**（`LocalZSpan` 量出卡内层 z 跨度 × 卡缩放 vs 步长）—— ⛔ 理由写在代码里：卡内层集合随卡面档位变，**本批不能跑 Unity 实测那个跨度**，写成断言会假绿/假红。

## ③ 没查清 / 没做的部分

1. **项 12 的「全库」口径**：查的是 `assets_full` 里按 `m_betweenElementsSpacing`（CardsHorizontalLayout 专有字段名）定位到的 **52 个 MonoBehaviour**（= 13 战场 × 4 份：我方/敌方/选牌/换牌），逐个读 `"m_inverted"` —— **52/52 = 0**。若还有**别的 bundle**（非 `scenes_scenes_battlearena*`）藏着该组件，本次没扫到；判据字段名是唯一的，漏的可能性低。
2. **`files_with_matches` 只按字段名找**：不排除有实例被内联/非序列化，但原版是序列化 bool，不适用。
3. **项 7 的「换成 render queue」没做**（要动 `CardView`，越白名单）。
4. **`IsCompressed` 的阈值只按 `allowSelectCardLayoutOverFlow = 1` 实现**：四份实例都是 1，代码里恒按 =1 处理（`totalWidth` 不含 `2*halfExtra`）；若将来出现 =0 的实例要另开一支，代码注释已写明。

## ④ 顺手发现（**要报、我没改** —— 判据/改动点在别的文件或别的批）

1. 🔴 **`资料/手牌布局_原版算法与参数.md` 有两处写反，我没权限改**：
   - §二 伪代码里 `else { extra = 0f; }` 那行的**内联注释**写「**装不下** ⇒ 连选中外扩也取消」—— 方向反了（那个 `else` 是**装得下**那一支；它自己的伪代码是对的，只有注释错）。
   - §八之三 第 1 条「换成原版的 `selectedCardExtra` … **并加 `IsCompressed` 门闸**」—— 照这个字面去读会得到相反的门闸方向；**我们这一批就是照它写反的那一版**（2026-09-25 那次）。请主对话就地订正。
2. **`Hand/CardInteraction.cs:226` 传错了位置**：原版传的是**指针位置**（`BattleManager__Update.c:123-127` 每帧把 `GetMousePosInWorldSpaceCanvas()` 写进 `+0x434`，`:220-222` 拿它调 `GetClosestInHandSlot`）；我们传的是 `_dragging.transform.position`（带 `Lerp` 平滑、**滞后于指针**）。差一个「跟手延迟」，**判据点在 `CardInteraction.cs`（不在本批白名单）** ⇒ 请派给那个文件的写手。
3. **`HandLayout.frontZ = 0.5` 不足以把悬停那张提到最前**（12 张时它右边 5 张 + 左边 6 张**全都**压在它上面：`z_hover = 6×0.08−0.5 = −0.02`，而 `z_j = 0.08j ≥ 0 > −0.02`）。**⚠️ 但原版也是这样** —— `ShowCardInHand` 只靠「抬起 30 px + 放大 1.3 + 两侧让位」露出，**没有**给被展示那张特殊 `sortingOrder`（`MoveCardsInHandToPosition` 对每张牌一律 `长度−序号−1`）⇒ **这不是「与原版不符」**，是本批查清后**故意不动**。要动得连同层序公式一起想。

## ⑤ 类型检查结果（原样贴；`TMPDIR=/tmp/wf_b1 bash d:/4/Unity/工具/typecheck.sh`）

```
--- 运行时程序集 ---
Assets\CardPresentation\Battle\SettingsPanel.cs(463,13): error CS0103: 当前上下文中不存在名称"_langField"
…（同文件共 18 条）
运行时错误数: 18
--- 编辑器程序集 ---
Assets\CardPresentation\Editor\BattleScene.cs(14986,47): error CS0234: 命名空间"WarpforgeVFX"中不存在类型或命名空间名"WFSceneModule"
编辑器错误数: 1
```

🔴 **这些错全部集中在不是我负责的文件上**（`Battle/SettingsPanel.cs` 正在被另一个代理写、`Editor/BattleScene.cs` 是战斗侧断言宿主的在飞改动），
**`Hand/HandLayout.cs` 与 `Editor/CardBaseDemo.cs` 一条错都没有**。三次连跑的错误文件还在换（`SettingsWindow.cs` → `SettingsPanel.cs`）⇒ 印证是并发写手。**不许当成我这条的问题去改别人的文件**。

## ⑥ 还欠什么

- **断言一条都没跑**（简报：⛔ 不许跑 Unity）⇒ 由主对话在同步点跑 `CardBaseDemo.Run`（宿主 = `Editor/CardBaseDemo.cs`）。
- 项 8 的方向改成「压缩态才让位」后，**12 张手牌会看到两侧整排各让 29.7 px** —— 这是原版行为，**但请跑完自检后看一眼截图**（断言测不出「看着对不对」）。
