# B148 · W13 — `DeckScene` 剩余 20 条失败清算

> 执行写手：W13 · 2026-11-05 · 批次 `B148`
> 白名单：`CardPresentation/Editor/DeckScene.cs`（主战场）· `Editor/B108_A3_Aud02Deck_Assert.cs`（= 简报里的
> `B108A3*`，实际文件名带下划线）· `Editor/B141_W74_Assert.cs`。**另动了 1 处产品代码**（见 §三，已确证是产品缺陷）。
> ⛔ **本报告不含「已通过」**：Unity 宿主只有调度台能跑，这里只有静态验收（typecheck + 账闸）。

---

## 一、这 20 条是怎么数出来的（`D:/4/_tmp_view/deck.log`，21:41 那次）

日志尾 `DK === 结束：2694/2714 通过，20 条失败 ===`，但**逐条不是 20 行 DK** —— 20 = 17 行 DK 汇总
+ `[B141_W53] G8` 1 条 + `B141W74 E3` 1 条 + `B108A3 G9` 那 1 条（它同时触发它自己的 `✗` 和宿主那条聚合 `✗`，
所以 1 个根因占 2 行）。`>>> A1007 探针` 那条 `✗` **不计入**（它在独立 `CheckSink` 里，是本宿主**故意**造的红）。

| # | 日志行 | 断言 | 判定 |
|---|---|---|---|
| 1 | `698782` | `[B108A3] G9` 卡背 `Army Filter/Title` 的 `m_margin` 左 = 22.7（实得 0.21） | **(β) 断言读数口错** |
| 2 | `698799` | `DK ★ AUD02 … 失败 0`（实得 1） | 随 #1 |
| 3 | `703539` | `（前提）$fac:TauEmpire 的图示活着` | **(δ) 夹具前提** |
| 4 | `703555` | `★ ② TauEmpire 渲染上沿 = 156`（实测 120.1） | 同 #3 |
| 5 | `703912` | `（前提）Army 行中心 y = 140.02`（实测 360.02） | 同 #3 |
| 6 | `703929` | `（前提）它的框 [335,385] 压在带口 156 上` | 同 #3 |
| 7 | `703977` | `（前提·相对判据）不裁时字形顶 341.3` | 同 #3 |
| 8 | `703993` | `★★ A289 字顶 = 156`（实测 341.82） | 同 #3 |
| 9 | `704770` | `再推 0.15 秒 ⇒ 到位（一共 0.3 秒）`（实测 **0.9375**） | **(α) 产品缺陷**（见 §三） |
| 10 | `705310` | `小标题 Army 渲染左沿 = 2.2`（实测 134.288） | 同 #3 |
| 11 | `707634` | `自适应下界 = 2px`（实测 1.7061） | **(β) 断言读数口错** |
| 12 | `707651` | `自适应上界 = 32px`（实测 27.2970） | 同 #11 |
| 13 | `707668` | `自适应基准 = 36px`（实测 30.7091） | 同 #11 |
| 14 | `709963` | `[B141_W53] G8 对照标签缺省面` | **已由 `B148-W3` 在同一批里改掉**（21:42，晚于这次跑），⛔ 我没动 |
| 15 | `710620` | `B141W74 E3 清空后加 B 的牌 ⇒ 锁成 B`（实得 null） | **(δ) 夹具绕过写点** |
| 16 | `717661` | `灭自证② Describe(EffectOnlyCard)`（实得 `InvalidCard`） | **(β) 断言旧期望** |
| 17 | `718880` | `正文键 = 兜底键 InvalidDeck`（实得 `IncompleteDeck`） | **(β) 断言旧期望** |
| 18 | `718895` | `画出来的是那条键的词条`（中文列实得 IncompleteDeck 那句） | 随 #17 |
| 19 | `740882` | `……而且照旧回一个名字（「」）` | **(β) 断言旧期望** |
| 20 | `741454` | `……而且入库的就是这一副（名字对得上）`（期望「」，实得「安维尔·凯尔托克」） | 随 #19 |

---

## 二、逐族：判据 / 改了什么 / 下次该看到什么

### 族 A（#1 #2）`B108_A3` G9 —— margin 读数口错（β）

- **判据**：原版 `Army Filter/Title` 的 `m_margin = (22.7, 0, 0, 0)`（`判据/AUD02_裁定_卡组编辑.tsv:462`）。
- **根因**：产品写的是 `Label.SetMarginPx(22.7,…)` ⇒ `_margin = 22.7/108`（**世界单位**），再同步给 `_tmp.margin`
  ⇒ 读 `titleTmp.margin.x` 得 **0.21**（22.7 ÷ 108）。**产品是对的**：同一条判据在 `Editor/B141_W26_Assert.cs` §4-12
  用 **`Label.MarginPx`**（= `_margin × PxPerUnit`，画布 px）断 22.7，**那一宿主是绿的**。
- **改**：G9 改读 `title.MarginPx.x`（`B108_A3_Aud02Deck_Assert.cs:434` 区域，含一行注释说明两个量纲）。
- **下次应看到**：`[B108A3] G9` 绿 ⇒ 宿主那条 `★ AUD02 … 失败 0` 也绿（2 条一起消失）。

### 族 B（#3–#8、#10）`Army` 行在「有督军」时是**隐藏**的 —— 夹具前提（δ）

- **判据**（第一权威，`.c` 逐句）：`CardArmyFilter__SetFilterToDeck.c:16-26` —— 有督军 **或** 有牌 ⇒ 走锁那支，
  由 `CardArmyFilter__FilterToArmy.c` **末句 `UnityEngine_GameObject__SetActive(go, 0)`** 把 **Army 整行关掉**；
  两者皆空才走 else 支 `SetActive(go, 1)`。我们照抄了（`FilterPanelModel.ArmyRowVisible` /
  `Cell.Hidden` / `Title.Hidden`，2026-11-04 `B141_W30 · W129 3-01`）。
- **为什么现在才红**：W12（本批）为了别的断言给夹具塞了一副**有督军**的满编 30 张 ⇒ Army 行合法地隐藏了；
  而 A224②/A289 两节是 2026-10-10/11 写的，那时我们**还没**实现这处隐藏，断言默认 Army 行在场。
  实测数字对得上：`Army` 小标题中心停在**行顶未摆位的 360.02**（该在 140.02）、`$fac:TauEmpire` 那格
  `activeSelf = false`、几何没被 `ClipCellToBand` 裁过（120.1 而不是 156）。
- **改**（`DeckScene.cs`，两节各一个局部作用域）：
  1. 临时把卡组清空（`WarlordId/DefensiveId/CardIds` 三样存下来 → 清空）⇒ `ArmyLocked` 假 ⇒ 原版 `SetActive(true)` 那一支；
  2. `_rt.UiToggleFilters()` **两次**（关→开）强制重建抽屉（这是唯一能从宿主触发重建的公开口），重新滚到该节的滚动位；
  3. 量完**逐项还原**并再重建回「有督军」形态。`Deck.DeckArmy` 字段**一个字没动**。
  ⚠️ 只是**临时态**、不落盘、不碰卡池（`RefreshFilters` 只重建抽屉，不刷卡池）。
- **A289 的参考行同时由 `Energy Cost` 换成 `Rarity`**：这是**原版的结构性事实** ——
  `CostRowVisible`（原版 `CardCostFilter__SetFilterToDeck.c:21` `SetActive(deckHero != null)`）意味着
  「Army 行可见 ⇔ 无督军 ⇔ Energy Cost 行隐藏」，两者**不可能同屏**（旧断言的前提在原版里根本不存在）。
  `Rarity` 是同一屏里整块在带口内（框 ≈ `[690.02, 740.02]`，实测推导见下）的可见行，照样能现推「框顶 → 字形顶」偏置。
- **A289 ③（`Type` 整块在带外）挪到清空**之前**跑**：原版 `CardTypeFilter` 在无督军时整行也 `SetActive(false)`
  （`typeHidden = costRowNeedsWarlord && !HasWarlord`），所以那一条必须在**有督军的正常态**下量（它本来就是绿的）。
- **下次应看到**：`Army` 中心 y = **140.02**、框 **[115.02, 165.02]** 跨过 156、`$fac:TauEmpire` 格子
  `activeSelf = true` 且**渲染上沿 = 156.0**、A289 字顶 = **156.0**、左沿 = **2.2**、②′ 越界行数 = 0；
  `Type` 那条（③）与改动前同值。

### 族 C（#11–#13）自适应三档 —— 读数口错（β）

- **判据**：原版 `row_x*`（`Deck Selector Card Info button` 族的 `Count`）`m_fontSizeMin/Max/Base = 2/32/36`。
- **根因**：那三颗用的是 **静态单参** `Label.FontSizeToPx(x)` —— 它固定走 **CJK 汉字墨高**（≈0.0853 em），
  而 `row_x*` 的字体面是 **`LatinCardBody`**（`DeckRuntime.cs:2171` 显式传的；真 em = 0.1）⇒ 恒差 **×0.853**
  （2→1.7061 · 32→27.2970 · 36→30.7091，与实测逐位吻合）。同文件 `CheckA1212`（:700）早就用**实例口**
  `lb.FontSizeToPxNow(x, lb.ActualFace)` 并注释了这条坑。
- **改**：三处换成 `x0.FontSizeToPxNow(x0.FontSizeMin/Max/Base, x0.ActualFace)`。
  `SetAutoFitBox` 内部把 px 折成 em 用的就是 `NominalPx() = FontSizeToPxNow(cur, ActualFace)` ⇒ 往返**逐位**等于原版 px。
- **下次应看到**：`实测 2.0000 / 32.0000 / 36.0000`。

### 族 D（#15）`B141W74 E3` —— 夹具绕过写点（δ）

- **判据**：原版 `CardDeck.AddCard`：**加之前** `CardCount(hero:1)==0` ⇒ `deckArmy = card.cardArmy`；
  `RemoveCard` 在 `CardCount(hero:1)==0` 时写 Neutral。⇒ **原版到不了「有牌 + deckArmy = Neutral」这个状态**。
- **根因**：夹具 `e.ClearCards()`（写 Neutral）之后直接 `e.Deck.CardIds.Add("W74_UB")` —— **绕过了 `AddCard` 的写点**
  ⇒ `DeckRules.DeckArmy` 第 ② 支「字段已定值 ⇒ 字段说了算」返回 null ⇒ `ResolveArmyLatch(null, …)` = null。
- **改**：夹具改走玩家那条路 `e.TryAdd("W74_UB", out why)`（`DeckEditorState.cs:481` 就是原版那句写点），
  外加一条 `E3（前提）` 断言把「加进去了没有」钉死（⛔ 不静默）。**断言本身一个字没动。**
- **下次应看到**：`E3` 绿（`ArmyLatchFaction = Ultramarines`，只列 B 的战将）。

### 族 E（#16–#20）`Describe` / 上屏键 / 新卡组名 —— 断言旧期望（β）

三处都是 **2026-11-04 `B141` 改了口径、断言没跟**：

1. **#16**：`DeckRules.Describe(EffectOnlyCard)` 现在 = `MenuDeck/Error/InvalidCard`（对账 3-18，判据
   `CardDeck__CanAddCard.c:40-47`：`inventoryOptions(+0x4c) != 0 && showInCollectionOptions(+0x48) != 5` ⇒
   `DAT_1842cf4f0 = InvalidCard`）。断言还写着 catch-all `InvalidDeck`（那是「不印键名」的兜底，不是原版那一支）
   ⇒ 期望改 `InvalidCard`，并就地改掉相邻两段过期注释。
2. **#17 #18**：`ShowInvalidDeckPopUp` 在 `B141·W29·3-19` 从**数字族**改成 **`DeckRules.ErrorTextKey`**（与横幅同源）。
   原版 `DeckUtility.ToRawLocalizationString` 的 `{0}` 装的是**枚举名**，`TooFewCards` 属 `IncompleteDeck=5`
   ⇒ 上屏键 = `MenuDeck/Error/IncompleteDeck`、正文 = 它中文列「您的卡组不完整。您需要将30张牌…」。
   断言改这两处（`wantMsg = Loc.T("MenuDeck/Error/IncompleteDeck")`）。
3. **#19 #20**：`CollectionData.CreateDeck` 自 `B141-W32`（条目 3-13）按 `CardDeck__GetEmptyDeck.c:10-27`
   把新卡组名清成**空串**（名字由第一次 `Done` 的 `DeckUtility__ValidateDeck.c:70-76` 用**督军本地化卡名**补）。
   ⇒ `#19` 期望从「非空」翻成「**空**」；`#20` 原来比的是那个空名字，现在改成按**身份**断
   （督军 + 张数 + 名字三点都对，且名字已被补成「安维尔·凯尔托克」而不是空串）—— 比旧写法鉴别力更强。

- **下次应看到**：`Describe(EffectOnlyCard)` = `MenuDeck/Error/InvalidCard`；弹窗 `MessageKey` =
  `MenuDeck/Error/IncompleteDeck`、`MessageShown` = 上面那句中文；`CreateDeck` 回空串；入库那套的
  督军/张数/名字与编辑中的那副一致。

### 族 F（#14）`B141_W53 G8` —— 不是我的

`B141_W53_Assert.cs` 在 **21:42**（晚于 21:41 那次跑）已被同批的 `B148-W3` 改成「缺省面 = `LatinCardBody`」，
`git diff` 里能看到。⛔ 我没动那个文件。

---

## 三、动了 1 处**产品代码**（已确证是产品缺陷，⛔ 不是为过断言）

`CardPresentation/Deck/DeckRuntime.cs:2694`（`StepDrawer`）——

```diff
- if (!tw.Done || tw.Target != d.SlideTarget) { tw.From = d.Slide; tw.T = 0f; … }
+ if (tw.Done || tw.Target != d.SlideTarget) { tw.From = d.Slide; tw.T = 0f; … }
```

- **`Done` 的语义 = 「已经到位」**（`StartDrawerSlide` 里 `tw0.Done = d.Slide == d.SlideTarget`；早退那一句
  `if (d.Slide == d.SlideTarget) { DrawerTw(d).Done = true; return; }`）。所以「要重起补间」的条件是
  **`Done` 为真**（或目标变了），写成 `!tw.Done` = **每一帧都重起**。
- **指纹**：`t=0.15s` 时读出 `0.75`（对），**再 0.15s** 应到 `1.0`，实测 **`0.9375` =
  `Lerp(0.75, 1, 1−(1−0.5)²)`** —— 正是「从当前位置（0.75）重新起一段、`T` 归 0」的结果，**逐位吻合**。
- **真机影响**：60fps 下每帧只走剩余距离的 **10.8%** ⇒ 原版 `DOAnchorPosX(…, 0.3)` 的 0.3 秒滑行变成
  **~2.5 秒的指数逼近**，且 `Slide` 永不**恰好**等于 `SlideTarget`（「到位」那一支长期不触发）。
- **判据**：原版 `CollectionFilterController<T>.Toggle` = `DOTween.Kill` + **`DOAnchorPosX(rect, x, 0.3)`**
  （`hiddenPosition.x = −550` / `origin = −165`，行程 −385）—— **单段补间**，不是每帧重起。
- 同行已写 `✅ 已复刻（2026-11-05 · B148·W13 · …）` 标记。**行数零变化。**

---

## 四、静态验收（本代理只做到这一步）

| 项 | 命令 | 结果 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_w13 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 · 编辑器 0** |
| 账闸（只读） | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | A 5983 · 漂移 **4** · 失败 **1** —— **全部在 `Battle/SettingsPanel.cs`**（与本笔无关，W12 那次就是同 4+1）；`DeckScene.cs` / `DeckRuntime.cs` / `B141_W74_Assert.cs` / `B108_A3_*.cs` **0 漂移 0 失败** |
| 行尾 | 二进制计数 | `DeckScene.cs` CRLF 10639/10639 · 裸 CR 0；`DeckRuntime.cs` CRLF 10750/10750 · 裸 CR 0；两个 `Editor/*_Assert.cs` 仍 LF |

**行数变化**（账按 `文件:行号` 引用，列在这里备查）：`DeckScene.cs` 10602 → **10639（+37）** ·
`B141_W74_Assert.cs` 230 → **237（+7）** · `B108_A3_Aud02Deck_Assert.cs` 479 → **483（+4）** ·
`DeckRuntime.cs` **10750 不变**。账闸复跑确认**没有新漂移**（DeckScene 那 37 行落在账没有 A 落点的区段）。

---

## 五、下一次跑 `DeckScene.Run` 该看到的数字（⛔ 不是「已通过」）

- `DK === 结束：2714/2714`（或至少 **2712/2714**，见下）；
- 本节列出的 19 条（G8 由 W3 负责）应当全绿；
- **可能残留 1–2 条**、且都是**新暴露**而非回归：
  1. `A289 ②′` 若在清空态下量到 `Energy Cost`/`Type` 也「越界」——它们那时是 `SetActive(false)`，循环里已 `continue` 跳过，
     理论上不会；若真红，看它报的是哪一行；
  2. `A224②` 若报「滚到底时没有一格压在带口上」——说明重建后 `UiScrollFilters(1e6f)` 的钳位与清空前不同
     （内容高只跟阵营数走，理论上不变），那是本节要现读的第一个数。
- ⚠️ **本笔动了产品代码（`StepDrawer`）**，若调度台要跑全套，`DeckScene.Run` 是必跑的（抽屉滑动的两条断言就在它里面）。

---

## 六、顺手发现（不在本笔白名单 / 交调度台分流）

1. `DeckRuntime.StepDrawer` 那个 `!tw.Done` 是 **2026-11-04 `B141`（3-12/3-29 改 OutQuad）** 引入的，
   而当时只改了 `DeckScene` 的**第一条** tick 期望（0.5 → 0.75）、**第二条没跑过**（那一版宿主的 `deck` 还在崩）
   ⇒ 缺陷躲了一整天。**同族建议**：凡是「把 `MoveTowards` 换成补间状态机」的改动，至少留**两次** tick 的断言（本笔补上了）。
2. `FilterPanelModel.CostRowVisible`（无督军 ⇒ 隐藏费用行）与 `ArmyRowVisible`（有督军 ⇒ 隐藏阵营行）是
   **互斥**的两半，任何「同一屏同时用到 Army 行 + Energy Cost 行」的断言都是**原版到不了的状态**。
   本笔把 A289 的参考行换成 `Rarity`；**别处若还有同形写法**（`grep "Energy Cost" Editor/*.cs`）建议一起核。
3. `List<DeckEditorState>` 那类「直接改 `Deck.CardIds` 绕过 `TryAdd`」的夹具写法在 `Editor/` 里可能不止 E3 一处：
   凡是「清空后再加牌」的夹具，都要走 `TryAdd`/`SetWarlord`，否则 `deckArmy` 会停在 `Neutral`（静默）。
