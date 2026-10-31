# 判据 · 聊天消息行高与滚动内容高（`ChatTab` / `ChatMessageRow`）

> 产出：`B106_D4`（只读诊断）· 2026-10-31 · 完整诊断见 `资料/完全复刻/报告/B106_D4.md`
> 消费者：`CollectionScene` 里聊天滚动区那一节断言的修订者（`Editor/CollectionScene.cs:11580-11660`）

## 一、结论（可直接照做）

**原版 `ChatMessageRow` 的行高是动态的**，不是 OSA `_DefaultItemSize = 60`：

```
行高 h   = 10 (VLG padT) + 10 (padB) + 25 (头) + 12 (VLG spacing) + 正文行盒
         = 57 + 正文行盒
步进     = h + 10 (OSA _ContentSpacing)
内容高   = 5 (_ContentPadding.top) + Σh_i + 10×(n−1) + 5 (_ContentPadding.bottom)
正文行盒 = TMP 行盒（Pragati：`m_LineHeight 90 / m_PointSize 95 × 字号`）
```

本机实测档（20 条 · 单行 22px · Pragati）：

| 量 | 值 |
|---|---|
| 正文行盒 | **20.842** = `90/95×22` |
| 行高 h | **77.842** |
| 步进 | 87.842 |
| 内容高（20 条） | **1756.84** ⇒ 内容底 1917.84 |
| `ClampHi`（视口 750） | **1006.84** |
| 偏移 0 建出行数 | **9**（i=0..8；第 9 行 top 956.6 > 911） |
| 首行中心 | **204.92** = 166 + h/2 |
| 偏移最大时 `msg-15` 中心 | **515.71** |
| 偏移最大时最后一行 Hit 上沿 | **823.16** = 828.16 + 10 − 15 |

## 二、判据出处（逐条可核）

| # | 事实 | 出处（现读） |
|---|---|---|
| 1 | 行根 `ContentSizeFitter m_VerticalFit=1 (MinSize)` + `VerticalLayoutGroup m_Padding(L25,R60,T10,B10) m_Spacing=12 m_ChildControlHeight=0` | `d:/2/tools/assetripper_out/ExportedProject/Assets/ChatMessageRow.prefab:286-315` |
| 2 | `Player Header` / `Friend Header` RT `m_SizeDelta.y = 25` | 同上 `:533`、`:981` |
| 3 | `Message` RT `sizeDelta.y = 0`、自身 `ContentSizeFitter m_VerticalFit: 2 (PreferredSize)`、锚点 `y = −47`；TMP `m_fontSize: 22`、`m_lineSpacing: 0`、`m_TextWrappingMode: 1`、`m_VerticalAlignment: 256` | 同上 `:1484-1500`、`:1610-1617`、`:1559-1577` |
| 4 | `RowBackground` `LayoutElement m_IgnoreLayout = 1`（不参与行高） | 同上 `:405-425` |
| 5 | 原版把量出的行高回灌：`ChatContentView.UpdateViewsHolder` → `MarkForRebuild()` + `ScheduleComputeVisibilityTwinPass(true)`；`OnItemHeightChangedPreTwinPass` 清 `HasPendingVisualSizeChange`；`ChatEntryView.MarkForRebuild` 把 `ContentSizeFitter.enabled = 1` | `/d/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChatContentView.cs:288-306`；`/d/2/tools/decomp_full/ChatEntryView__MarkForRebuild.c`、`ChatEntryView__UnmarkForRebuild.c` |
| 6 | `_Params._DefaultItemSize = 60` 是 OSA 估计值，且 `_PrefabControlsDefaultItemSize = 1`（运行期被 prefab 尺寸覆盖） | `assets_full/bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/MonoBehaviour_-8786325734254409834.json` |
| 7 | 字体行盒：`m_PointSize: 95` · `m_LineHeight: 90` ⇒ 22px 字号 = 20.842 | `Unity/MyGame/Assets/CardPresentation/Resources/Fonts/Pragati-Regular SDF.asset:20,23` |
| 8 | 我们实现：`RowHeightFor` = `max(60, 57 + MeasureMessagePx)`；量高走 `Label.WorldH`（TMP `textBounds`）×108 | `Unity/MyGame/Assets/CardPresentation/Shell/ChatTab.cs:210-220,230-250` |
| 9 | 正文框随行高：`r.y1+47 → r.y2−10` | `Unity/MyGame/Assets/CardPresentation/Shell/ChatPanel.cs:732-733,849` |

## 三、结论怎么用（断言修订口径）

- 期望值**必须从公式推**（`LineH = 90/95×22` 是独立来源），**⛔ 不要回读 `RowHeightFor` / `ClampHi`**（自证，CLAUDE.md §二）。
- 再加一条**结构**断言兜住将来漂移：`首行中心 − 视口顶 − padTop == 该行 rect 高 / 2`。
- 已知过期文案（一并改）：`Editor/CollectionScene.cs:11580` · `:11600-11601` · `:11606` · `:11608` · `:11621-11622` · `:11639`。

## 四、缺口（写清楚，别当已知）

1. **OSA 基类 `UpdateItemSizeOnTwinPass` 的默认返回**：本地 `OSA.Core.dll` 是签名桩（`return 0f;`），`cpp2il` 的 `OSA.Core` 产物是空体 `{ }`，`decomp_full` 无 OSA.Core，联网取上游源码被网络策略挡 ⇒ **「twin pass 后原版内容高的精确数值」本地拿不到**。它只影响期望值精度，不影响「行高动态」这一结论。
2. **原版只对已实现的行回灌尺寸**（OSA 语义），我们量全部行 ⇒ 机制差异，建议立账，别混进这 7 条。
3. `ChatTab.cs:221` 的 `Mathf.Max(60, …)` 是**自设计下限**（原版空正文时应为 57）—— 按 §18 打标或去掉，本轮未定。
