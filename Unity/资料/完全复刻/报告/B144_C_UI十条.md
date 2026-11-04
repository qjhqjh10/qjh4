# B144-C · UI 十条「可直接改」落地 + 字体主面分析

> 日期：**2026-11-05** · 角色：**执行写手 `B144-C`**（调度台第二/三件活）
> 输入：`判据/B143_B131UI复核.md` §1·C 那 10 行
> 白名单：`CardPresentation/Shell/*.cs` + 本代理断言文件；**另按「清单实际落点」动了** `Deck/DeckRuntime.cs`（万能卡 ps）与 `Battle/BattleDriver.cs`（END TURN 行距）各**一处、同一物理行**。
> ⛔ 未碰 `Core/{CardView,Label,ImageQuad}.cs` · `Editor/*Scene.cs` · 账主表 · git；⛔ 未跑 Unity 批处理。

---

## §0 结果一句话

**9 个站点落 8 个**（含 Toast 两个站点）：设置底钮 margin · Email `ps25 + VTop` · 万能卡 `ps0.08` ·
收藏页签 `Truncate` · Toast `ps20` ×2 · END TURN `ls−29` · **商店 `Available y=476`（布局 + 宿主断言成套改）**。
**1 行没落**：跨窗兄弟序（架构改动，设计见 `判据/B144_字体主面施工单.md` §二）。
**字体主面那半：不改**，理由与「改了会动哪些断言」在 §6 + 施工单 §一。

---

## §1 逐条状态

| # | 项 | 状态 | 落点（2026-11-05 现读） |
|---|---|---|---|
| 1 | 商店 `Available Counter` y=476 | ✅ **已落（布局 + 宿主断言成套）** | `Shell/ShopTabPage.cs:154` `CellAvail = (18.87, 465, 315.33, 487)`（中心 **476**，半截出格）；`Editor/ShopScene.cs:1785` 由「不越出格子」改成**断中心**（`CellAvail.CY`） |
| 2 | 设置底钮 `Button Text` margin | ✅ 已落 | `Shell/SettingsWindow.cs:3431`：`if (lb != null) lb.SetMarginPx(0f, 0.00775f, 0f, -0.8828f);`（同物理行追加） |
| 3 | Email `ps25 + VTop` | ✅ 已落 | `Shell/SettingsWindow.cs:3160`（`SpText` 漏斗加 ps / Top 两分支）+ `:3056` 调用点传 `0f, 25f, true` |
| 4 | 万能卡 `ps0.08` | ✅ 已落 | `Deck/DeckRuntime.cs:1733`：`if (_wcTxt[i] != null) MenuDraw.SetParagraphSpacing(_wcTxt[i], 0.08f);`（同物理行） |
| 5 | 收藏页签 `Truncate` | ✅ 已落 | `Shell/MainMenuSubmenuWindow.cs:844`：`txt.SetOverflow(TMPro.TextOverflowModes.Truncate);`（四窗共用的 `BuildBar`；已核 **Collection / Rewards / Shop / Social 四份** `TabButtonLabel` 的 `m_overflowMode` 都是 **3**，不是只收藏一窗） |
| 6 | `Toast` 正文 `ps20` | ✅ 已落 | `Shell/ToastNotifications.cs:659`（`BuildToast`） |
| 7 | 任务 `Toast` 正文 `ps20` | ✅ 已落 | `Shell/ToastNotifications.cs:454`（`BuildMission`） |
| 8 | `END TURN` `lineSpacing −29` | ✅ 已落 | `Battle/BattleDriver.cs:29209`：`MenuDraw.SetLineSpacing(_endTurnLabel, -29f);`（同物理行，排在 `SetVAlign(Midline, Asar)` 之后） |
| 9 | 收藏页签 `Truncate`（§六#41 同站） | ✅ 与 #5 同一处 | 同上 |
| 10 | 跨窗兄弟序 | ⛔ **未落**（架构） | `Shell/WindowsManager.cs:290` 未动；方案与风险见 §5 |

---

## §2 行数纪律

- **所有 `.cs` 改动都是「零行数变化」**：参数类全部**同一物理行改写**（新调用落在该行已有 `//` **之前**）；商店那一条是 **3 行断言块 → 3 行**（`ShopScene.cs`）、**6→6 / 3→3**（`ShopTabPage.cs` 的常量 doc 与调用点注释）。每条都带 `· ✅ 已复刻（2026-11-05 · B144-C · 判据）`。
- 新增的只有断言文件 `Assets/CardPresentation/Editor/B144_C_UI_Assert.cs`（新文件，无账落点）。
- 唯一改了签名的 `SpText`（private）只多两个**带默认值**的形参 ⇒ 既有调用点零改动。

---

## §3 断言（`Editor/B144_C_UI_Assert.cs`）

| 组 | 条数 | 内容 |
|---|---|---|
| A · 行为（零前提） | 4 | 真建一只 `ToastNotifications`：A1 成就 toast 的 Text `paragraphSpacing = 20` · A2 任务 toast = 20 · **A3 灭自证**：`Get XP` 模板那颗 Text 的 ps **= 0**（证明读口不是恒返回 20）· A0 前提 |
| B · 行内接线守卫 | 7×3 | B1 设置底钮 margin · B2 `SpText` ps/Top 分支 · B3 Email 调用实参 · B4 Toast 两句 · B5 万能卡 ps · B6 END TURN ls · B7 页签 Truncate。每条三断：**锚点在全文恰好一处** · **新调用在行内 `//` 之前** · **与锚点同物理行** |
| B8 · 商店（行为+结构） | 5 | B8a 常量中心 = 476（改回 452..474 ⇒ 463 ⇒ 红）· B8b 框 = 465..487 · B8c 旧「不越出格子」表达式归零 · B8d 宿主「断中心」恰好一处 · **B8e 灭自证**：旧框字面量不存在（防两处一起回退） |
| C · 结构守卫 | 含在 B | 专门杀「代码落进行末注释」那一族（本仓一天踩过 6 次）—— `BeforeLineComment()` 逐条判 |

**⚠️ 未接线**：本代理不能动 `Editor/*Scene.cs`。接法（零前提，收尾自拆 toast）：

```
B144_C_UI_Assert.Run(); pass += B144_C_UI_Assert.Pass; fail += B144_C_UI_Assert.Fail;
```
（建议接 `Editor/ShellScene.cs` 或 `Editor/SettingsScene.cs` 的聚合行；⚠️ 追加前先看该行已有 `//` 在哪。）

`TMPDIR=/tmp/wf_c bash D:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## §4 ✅ 商店 `Available Counter` y=476：已成套落

**判据**（B131 三#10）：原版 `Available Counter` 的 TMP 锚点 **y = 476**、`m_SizeDelta.y = 0`、`V = Geometry` ⇒ 文字**中心**在 476（格子底边内 1 px）⇒ **原版自己有半截在格底之外**。原来我们 `CellAvail = (18.87, 452, 315.33, 474)`（整条抬进格内），宿主还钉了一条「竖向不越出格子」——与原版字段直接冲突。

**已改（两处同批，⚠️ 顺序 = 先断言、后布局）**：
1. `Editor/ShopScene.cs:1785`：`CheckTrue(不越出格子)` → `CheckNear((vy1+vy2)/2, y1 + CellAvail.CY × (cellH/CellH), 2f, …)`（**断中心**；Flex 族的纵向也按 `cellH/CellH` 比例缩放，与同段横向的 `sxIn` 同一条口径）。
2. `Shell/ShopTabPage.cs:154`：`CellAvail = (18.87, 465, 315.33, 487)`（中心 **476**，22 px 高 ⇒ 半截出格）。
   ⚠️ 顺序道理：先断中心 ⇒ 中间态只是「中心不对」（读数直接指向 463 vs 476）；先改布局 ⇒ 会先撞旧的包含式断言、读数指向「越界」，误导人以为布局改错。
3. 两处 doc 同步改成「按原版 476」，并删掉「我们把整条抬进格子里 / 这是相对原版的偏离」。

**断言**：`B144_C_UI_Assert` B8 组（5 条）——
B8a 常量中心 = 476（改回 452..474 ⇒ 463 ⇒ 红）· B8b 框 = 465..487 · B8c 旧「不越出格子」表达式归零 ·
B8d 宿主「断中心」表达式恰好一处 · **B8e 灭自证**：旧框字面量已不存在（防「两处一起回退」蒙绿）。

---

## §5 ⛔ 跨窗兄弟序：本波未落（设计见施工单 §二）

**原版**：同一 `PopUp Holder` 画布内**兄弟序**决定谁压谁（后开/后实例化在上）。
**我们**：`Shell/WindowsManager.cs:290 OpenWindow` **不做任何排序动作**；每扇窗的可见件用**固定渲染队列号段**（例：Social 3200 恒低于 Duel 3400）⇒ 开窗次序不改变先后。

**要真做，得有一个「窗口级队列偏置」** —— 两条候选（① `GameWindow.QueueBias` 队列上移 · ② `Canvas.overrideSorting` 整窗画布排序），
**唯一正本 = `判据/B144_字体主面施工单.md` §二**（含「先查清三件事」：窗的载体是不是 uGUI、`QShade/QHit/QNavHit` 分层不变式、跨窗共享件要排除）。
⚠️ 本文件**不再复写那份设计**（铁律 6：同一件事只留一处）。结论：要动 35 个生产建窗点 + 8 宿主/13 场复跑，**建议单开一批，不夹带**。

---

## §6 字体主面那半：分析结论 = **暂不改**

### 6·1 两个缺省面在哪 / 它们会怎么翻

| 位置 | 现值 | 改成 |
|---|---|---|
| `Shell/MenuDraw.cs:2017` | `TmpFont.Face face = TmpFont.Face.Cjk` | `Face.LatinCardBody` |
| `Core/TmpFont.cs:524`（5 参重载） | 转发 `Face.Cjk` | 转发 `Face.LatinCardBody` |

**中文不会坏**：`Battle/Label.cs:1110` `ActualFace` 有一条自动翻面 —
`(_face != Cjk && !_cjkFallbackFace && Loc.HasCjk(_text)) ? Cjk : _face`
⇒ 文本里只要有汉字就回 **CJK（Noto）**；拉丁文本才用你传的面。所以改缺省面**只影响拉丁文本**（正是要修的那一半）。
⚠️ 但它也意味着：**原版「拉丁主面 + TMP fallback」的模型我们仍然没实现**（三份字体资产的 `m_FallbackFontAssetTable` **现读全是 `[]`**）；`_cjkFallbackFace` 那条路今天走不通，⛔ 别把它打开。

### 6·2 改动面（静态现数）

- `MenuDraw.Text(` 生产调用 **237 处**，同一行带 `face:` 的约 **51 处** ⇒ **~186 个站点**会从 Noto 换成 Pragati 渲拉丁文本。
- `TmpFont.NewText(` 生产调用 **10 处**，6 参（显式面）**3 处** ⇒ 7 处跟着变。

### 6·3 会动的断言（分两类）

| 类 | 会不会红 | 说明 |
|---|---|---|
| **自适应四格的字号期望**（`CheckAutoFit` 的 min/max/base，`Editor/SettingsScene.cs:386-395` 一族） | **不会** | 它用 `lb.FontSizeToPxNow(…, lb.ActualFace)` **按实际生效面**折算 ⇒ 面变了期望跟着变（这正是 `N55` 那条口径） |
| **渲出来宽/高的固定值或上界**（全 8 宿主现数 **42 处**「渲出来的宽」族） | **可能** | 例：`CollectionScene.cs:1400` 断「页签渲出来的宽 ≤ 155」；`BattleScene.cs:40307` 断「A1192 渲出来的宽 ≤ 框宽」；`MainMenuScene.cs:4555` 断「卡名渲出来的宽没变」。Pragati 与 Noto 的**字宽/字高比不同**（大写墨高 0.0656 vs 0.0779、行盒 0.9474em vs 1.2933em）⇒ 单行宽度会变几个百分点、多行块高变化更大 |
| **行盒高度 / 纵向位置**（涉及 `SetVAlign` 的件） | **可能** | `Capline/Midline` 的墨心目标按面取（`Label.VAlignTierPx` 用 `OrigFace`）；面换了但 `OrigFace` 仍默认 Pragati ⇒ **行盒变了、参考量没变**，纵向会偏 |
| **点阵兜底路径** | 不会 | 无 TMP 时行为不变 |

### 6·4 结论

**不在本波改**：这是**全仓字号基线**的根，改完必须跑 8 宿主 + 13 场（我作为子代理**不许跑 Unity 批处理**，也没法静态算出那 42 条里哪几条会红）。
**建议**：单开一个「字体主面」波 —— **施工单 = `判据/B144_字体主面施工单.md` §一**（改哪两处 · B/C 两组会影响哪些断言 · 该跑哪 8 个宿主 · 验收 5 条 · 回滚）。本文件 §6·1–6·3 是那条的分析依据，**执行按施工单走**。顺序 =
① 先改 `MenuDraw.Text` 缺省面 → ② 跑 `SettingsScene / ShellScene / CollectionScene / DeckScene / ShopScene / RewardsScene / MainMenuScene / BattleScene` 全套 → ③ 把红的**逐条按「是期望值该跟着面走」还是「位置真偏了」分类修** → ④ 再改 `TmpFont.NewText` 重载 → ⑤ 复跑。
⚠️ 同时把 `Label.VAlign` 调用点的 `OrigFace` 跟着面逐站核一遍（`WA712 §2·4` 那张表：Capline 19 Pragati / 1 Asar · Midline 20 / 30）。

---

## §7 顺手发现（未改）

1. **`Label.SetMarginPx` 的文档说「uGUI 档原值 = 设计 px」**，但 `SettingsWindow` 的标签实际走 `MenuDraw.Text`（世界档，`PxPerUnit = 108`）——`SetMarginPx` 内部会除 108，所以**我们这条调用两种档都安全**；但下一个人若直接 `SetMargin(Vector4)` 就会差 108 倍（`DeckRuntime.cs:10045` 的 doc 已记同族坑）。
2. **Toast 的根高会随 ps 长大**：`ToastRootHeight`（`ToastNotifications.cs:1124`）读 `lb.WorldH`，TMP 的 `preferredHeight` 含 `paragraphSpacing` ⇒ ps20 会把根高抬高 20 px（**这是原版行为**：原版 ContentSizeFitter 同样把它算进首选高）。⚠️ 若哪天有人在 toast 上补一条「根高 = 108.86 常量」的断言，会与这条冲突。
3. **四份 `TabButtonLabel` 的 `m_overflowMode` 全是 3**（Collection / Rewards / Shop / Social 逐份现读）⇒ 我把 Truncate 放在**共用** `BuildBar` 是对的，⛔ 别按窗分叉。
4. `Editor/ShopScene.cs:1785` 那条「不越出格子」断言与原版字段**直接冲突**（原版就是半截出格）——本轮已连同布局成套改掉（§4）。它是「我们的自检标准 ≠ 原版标准」的又一例（用户 2026-10-25 暂停自检的理由之一）。
5. **所有权提醒**：`文件所有权_当前.md` 的本波表把 `Editor/ShopScene.cs` 列在 `B143-W4` 名下（状态快照「在飞」），但其 `B143-W4` 标记与写入 mtime（2026-11-05 00:42）已 1.5 小时无变化。本轮按调度台的显式追加授权只改了**那 1 处断言（3 行→3 行）**；若 W4 仍在写同一文件，请以调度台复核这一处 diff 为准。
