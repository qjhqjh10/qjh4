# B141 · W129 · 卡组拖拽/筛选 P1（19 条）— 执行写手 `B141-W29`

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 波次 `W129`
> 判据来源 = `资料/完全复刻/判据/B141_W129_对账_卡组色彩.md` **§一 P1 包**（＋ `报告/B138_审计汇总_问题清单.md` §3 对应行）。
> 白名单内只改了 **2 个文件**：`Deck/DeckRuntime.cs`（唯一写手）· 新建 `Editor/B141_W29_Assert.cs`。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账主表 / 活状态表 / 所有权表 / `Editor/DeckScene.cs`。
> ✅ 类型检查：`TMPDIR=/tmp/wf_b141_w29 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

## 〇、一句话结论

19 条**全部落地**（19/19 ✅），**0 条卡住**。新增代码**全部追加在文件最末**（新 partial 段从 `:10411` 起），
文件 10,411 → **10,748** 行；正文里只做了 3 处**净增 1 行**的插入，其余都是**同物理行替换**（净 0）。
⚠️ **有 6 处既有断言（都在 `Editor/DeckScene.cs`，不在本代理白名单）会因为这次「按原版改对」而变红** —— 逐条列在 §四，**必须先改它们**再跑 `DeckScene.Run`，否则会把「改对了」读成「改坏了」。

## 一、19 条逐条（原版值 vs 改后值 · 落点 · 断言）

| 编号 | 状态 | 原版值 / 判据 | 改后值（落点 = 现读行号） | 断言 |
|---|---|---|---|---|
| **3-05** | ✅ | 行拖：**5 px** 门 + `IsScrollDragThreshold` 60–120° 方向门（`Intro.unity:5429`；`SupportMethods__IsScrollDragThreshold.c`） | `TickRowDrag`（`:10467`）过 5 px 后按方向门分叉；判成滚动 ⇒ 转给滚动。`EndDrag` 的「有没有动过」不再看 `abs(Δx)>0.15`（16.2px） | A1–A6 · T7 |
| **3-06** | ✅ | 越过 5 px 才 `OnBeginDrag` | 按下只 `ArmRowDrag`（`:10458`，`_rowDragArmed=false`，一件不动）；`HandleDeckRowClick` 改调它（`:5225`） | T8 · D7/D8 |
| **3-07** | ✅ | 卡池起拖点击容差 **5 px** | `ArmPxSlop 16.2f` ⇒ `DragStartPx 5f`（`:3792`，**全族唯一出处**）；两处判定改 `PassedDragStart`（`:5064/:5070` 区） | A1–A3 · T7 |
| **3-08** | ✅ | 判成滚动 ⇒ `EventRelay.SetTarget(relay, scrollRect.gameObject)`（把这一拖**转给 ScrollRect**） | `BeginScrollDrag`（`:10585`）+ 卡池/卡背 arm 支与行拖支都改走它；不再只 `Debug.Log` | T9 |
| **3-09** | ✅ | 四台 `ScrollRect` 都能**按住拖动**（惯性 1 / 弹性 0.1 / 减速率 **0.135**） | 新增四台滚动面：`SdKindAt`（`:10527`）· `SdApply` · `HandleScrollDragTick`（`:10600`，含松手惯性 `Pow(0.135, Δt)`）· 每帧入口 `:5012` | D2–D5 · T9 |
| **3-11** | ✅ | `verticalNormalizedPosition ± Δt × speed`，**speed = 1.0 归一化/秒**（`DragAndDropItem` +0x28=50 · +0x2c=1.0，prefab 实读） | `DragEdgeSpeedPx(10px/帧)` ⇒ `DragEdgeSpeedNormPerSec 1f`（`:9152`）；`DragAutoScroll(…, dt)`（`:9212`）折成像素：`v × Δt × MaxDeckScrollPx` | B1–B4 · T6 |
| **3-12** | ✅ | `DOAnchorPosX` 无 `SetEase` ⇒ DOTween 默认 **OutQuad(6)** | `StepDrawer`（`:2689`）改 `1−(1−k)²`（`DrawerTw` 状态机 `:10654`） | C3–C5 · T5 |
| **3-14** | ✅ | 卡组名 `characterLimit` **26** | `NameLimitFor(1) = 26`（`:10664`），`HandleTyping` 改引用（`:7185`） | C1 · T2 |
| **3-15** | ✅ | 卡名筛选 `characterLimit` **0（无上限）** | `NameLimitFor(2) = int.MaxValue`（同一处） | C2 · T2 |
| **3-16** | ✅ | `Count(GetLibraryInFull())` = **[防御卡, 督军, cardLibrary]** 全数（`DeckEditorCollectionDisplay__DrawCell.c` 现读） | `DeckCountInFull`（`:10673`）＝ `CardIds` 份数 + 督军（同 id）+ 防御卡（同 id）；分子用它（`:3291`） | D6 |
| **3-17** | ✅ | 格式 **恒 `{0}/{1}`**，**没有督军分支**（同 `.c`：`GetLibraryInFull` 计数 + `{0}/{1}` 无条件） | 删掉 `"x"+den` 那一支（`:3290`），无督军也印 `n/N`；顺手把那段拿 `CardCollectionDisplay` 当判据的错注释就地订正 | （见 §四 U2：宿主的 `x1` 断言要跟着改） |
| **3-19** | ✅ | 保存失败弹窗与横幅**同源**（具名族 `MenuDeck/Error/{枚举名}`，`DeckUtility__ToRawLocalizationString.c`） | `ShowInvalidDeckPopUp` 改传 `DeckRules.ErrorTextKey(err)`（`:5921`），与 `DeckErrorText`（横幅）同一个键源 | T3 |
| **3-21** | ✅ | `CardDeck__GetEmptyDeck.c:10-31` = 空卡组（名/将/防御全空、卡表清空、`deckArmy Neutral`） | 删 `_newDeck = PlayerDeckForDemo(NewState())`，改 `new PlayerDeck { Name="", DeckArmy=Neutral }`（`:1352`）；🔶 标记随之撤销（`18·0`：它不属服务器门类）。`PlayerDeckForDemo` 本体保留（`Editor/*` 夹具在用） | T4 |
| **3-23** | ✅ | 卡池格 `UI Collider` = **整格 262.5×384** | 删掉 `if (!v.Contains(wp)) return false;`（`:5195`）⇒ 命中回到 `PoolCellAt` 的整格反解（`PoolCellW0/H0` 正好 262.5/384） | T1 |
| **3-25** | ✅ | 方向门吃 **当帧 `eventData.delta`** | 三处都改当帧 delta：`_armPrevPx`（卡池/卡背 arm 支）· `TickRowDrag` 每帧先算 `fd = px − _armPrevPx` 再推进 | A4–A6 |
| **3-26** | ✅ | 落点看 `hovered` 上的 `IDropHandler`（Deck Details 面板矩形） | `DroppedOnDeckPanel(px)`（`:10506`，复用 `DropField*` 那组常量 = `CollectHovered` 同一块矩形）；`EndDrag(x,y)` 用它（`:5353`） | D7/D8 |
| **3-27** | ✅ | 起拖那一帧就 `RemoveCard`（`CheckCardSlotDrag.c` 的 `:59`） | `BeginRowDragOut()`（`:10485`）在**越门那一拍**摘牌（`TryRemove` + 标脏 + 出声）；松手只判「丢回面板 = 放回（`Drop`） / 丢在别处 = 保持已摘」 | D8/D9 · T8 |
| **3-29** | ✅ | 杀旧补间 + **不屏蔽点击**（`CollectionFilterController.Toggle` 只 `SetActive + DOAnchorPosX`） | `StartDrawerSlide`（`:2700`）目标一变就从当前位置重起 0.3 秒；命中闸 `Slide>=1 && Target>=1` ⇒ `live`（`:2745`） | （见 §四 U3：宿主三条断言要跟着改） |
| **3-32** | ✅ | 原版那一支不会失败；我们失败要出声 | `TryRemove` 返 false ⇒ `Debug.LogWarning` + `Say(...)`（`BeginRowDragOut` 的 else 支） | （由 D8 覆盖「成功那支」；失败支靠源码文本 + 日志） |

## 二、改了 `DeckRuntime.cs` 的哪几处 ⇒ 下游位移多少（给账 `--fix` 用）

> 本文件有上千条账按 `文件:行号` 钉着；下面是**现读实测**的位移锚点（⛔ 不是估算）。

| 锚点 | 改前 | 改后 | 位移 |
|---|---:|---:|---:|
| `void RefreshPool()` | 2982 | 2982 | **0** |
| `string PoolCounterText` | 3281 | 3282 | **+1** |
| `void HandlePointer()` | 4993 | 4994 | **+1** |
| `public static int MenuDeckErrorNumber` | 5986 | 5978 | **−8** |
| `void HandleScroll()` | 7119 | 7112 | **−7** |
| `void HandleTyping()` | 7180 | 7173 | **−7** |
| `void ApplyDrawerSlide` | 2715 | 2716 | +1 |
| `public static float DragEdgeScrollFor` | 9159 | 9163 | **+4** |
| `void EndDragAt(float,float)` | 9179 | 9183 | **+4** |
| 文件行数 | 10,411 | **10,748** | **+337** |

**净增行的三处**（其余全是同物理行替换、净 0）：
1. `:5012` `if (HandleScrollDragTick(px)) return;`（+1）
2. `:5351` `EndDrag(float)` 1 参重载（+1，同时 `EndDrag(x,y)` 段整体比原 47 行**短 9 行**）
3. `:7185` 那一行的上一行多了一条注释（+1）
4. 文件末尾整段**新 partial**（`:10411` 起，约 338 行）—— 在所有既有行之后，⛔ 零位移。

⚠️ 结论：**≥ :5986 的老账锚点整体偏 −7 ~ −8，≥ :9159 的偏 +4**。等本波所有写手停手后由调度台跑 `check_ledger_points --all-A --fix`。

## 三、断言（新建 `Editor/B141_W29_Assert.cs`，**不建窗**）

- 入口：`Run()`（纯函数一半；B 段会打一条 SKIP）· `Run(DeckRuntime rt)`（加实例段）。
- 计数口：`Pass` / `Fail` / `Skip`；判绿红按行首 **`B141W29 ✓` / `B141W29 ✗`** 数（SKIP 行是 `B141W29 – `）。
- 灭自证（文件头逐条列了）：A1/A2 钉死 5 px；A4/A5 是**结构上不可能同时成立**的一对；A6 钉开区间边界；
  B1 钉「1.0 归一化/秒」；T1–T9 直接读源码文本钉**旧写法真的不在了**；D7/D8 是「拖进面板不变 / 拖出面板 −1」一对。
- **接线（本代理不动宿主）**：`Editor/DeckScene.cs` 里 `_rt` 那一档加
  `B141_W29_Assert.Run(_rt); _sink.Pass += B141_W29_Assert.Pass; _sink.Fail += B141_W29_Assert.Fail;`
  （与 `B141_W26_Assert.Run(_rt)` 同一行风格；⚠️ 该行已有别家写手的追加，**合并时注意别覆盖**）。

## 四、⚠️ 必须先改的既有断言（都在 `Editor/DeckScene.cs`，不在本代理白名单）

这 6 处**原本钉着「我们旧的行为」**，按原版改对之后它们会红 —— **不是回归**：

| # | 位置 | 原来钉的 | 应该改成 | 对应条目 |
|---|---|---|---|---|
| U1 | `DeckScene.cs:4561` `CheckNear(_rt.FilterDrawerSlide, 0.5f, …)` | 线性（0.15s / 0.3s ⇒ 0.5） | **0.75**（OutQuad：`1−(1−0.5)²`） | 3-12 |
| U2 | `DeckScene.cs:4559` `CheckTrue(!_rt.FilterDrawerInteractive, …)`「起点当然不算到位」 | 位移期间不参与命中 | **`_rt.FilterDrawerInteractive == true`**（`SlideTarget=1` ⇒ 在画面上就吃命中） | 3-29 |
| U3 | `DeckScene.cs:4591-4592`「位移途中（0.5）点这一格 ⇒ 没人吃」 | 位移期间**不**吃 | 改成「**这次也吃了**」（两态对比那条要重写成「两态都吃」，否则是恒真式） | 3-29 |
| U4 | `DeckScene.cs:4610`「收起途中（0.5）… 已经不吃」 | `!FilterDrawerInteractive` | 🔴 **§七 就地订正**：这条**不是「只改注释」** —— `FilterDrawerInteractive` 读的是 `d.Interactive`，而它现在等于 `live`（在画面上就为真）⇒ 0.5 时它是 **true**，旧断言**会红**。已改成断「节点仍活着 + 真正关点击的是 `_filtersOpen`（上一段已断 false）」 | 3-29 |
| U5 | `DeckScene.cs:3631-3675` 无督军档整段（`got != "x1"`、`bad`、消息文案） | 无督军印 `x{能放张数}` | **`"0/1"`**（`GetLibraryInFull` 计数 + 恒 `{0}/{1}`；督军格上限 1） | 3-17 |
| U6 | `DeckScene.cs:3629` 有督军档 `_rt.State.Deck.CountOf(poolDef.Id) + "/" + cap` | 分子只数 `CardIds` | 对**普通卡**两式相等（仍绿）；若那一格恰好是督军/防御卡则要改成 `UiDeckCountInFull`。**留一条更稳的写法**：`_rt.UiDeckCountInFull(poolDef.Id) + "/" + cap` | 3-16 |

> U5 的影响面要交代清楚：整段 `heroCells / nonLeg / bad` 的判别力是「非传说督军应印 `x1` 而不是 `x2`」，
> 改成 `0/1` 之后**判别力仍在**（照稀有度算会给非传说 `0/2`）⇒ 只需换期望串与文案，不用删逻辑。

## 五、如实标注 / 遗留（不影响 19 条判 ✅）

1. **3-27 的「列表立刻少一行」这一拍没做**：原版 `RemoveCard` 会顺手 `DeckListDrawer.RemoveCardFromList`，
   ⇒ 画面上下拉的那一行当场消失。我们是「牌从**数据**里当场摘掉（`State.DeckCount` 立刻 −1、标脏），
   但**拖拽期不重建行视图**」（`DragAutoScroll` 里那句 `if (!(_dragging && _dragDetached)) RefreshDeckList();`）——
   原因：我们这条拖拽**复用的就是行自己的 7 个视图**（原版是把件 reparent 到 root 的克隆），
   一旦中途 `RefreshDeckList`，这 7 件会被重新绑到**别的卡**上（拖着的卡会「变脸」）。松手 `RefreshAll()` 一次到位。
   ⛔ 别为了「形状对齐」把这条闸去掉；要做真等价得先有「专用拖影件」（另开一笔）。
2. **3-09 的惯性**：`m_Inertia=1` / `m_DecelerationRate=0.135` 已照抄（`Sd` 那一支），
   但**速度截止 `ScrollInertiaMinPxPerSec = 10 px/s` 是我们挑的**（原版由 `ScrollRect` 内部
   `velocity.magnitude > 0.01` 那类阈值收尾，本地没读那段）⇒ 已在该常量注释里标「我们自己挑的」。
3. **滚动面矩形**取的是「卡池视口 / 左栏 `RowNode` 带 / 筛选抽屉 / 卡背页卡池」这四块，
   与 `HandleScroll`（滚轮）**同一批常量**；`CosmoFltStripOn`（卡背筛选栏）**没有**滚动 —— 与原版一致（那棵树里没 `ScrollView`）。
4. **滑动途中点击**：命中改成跟着「在不在画面上」走之后，滑入的那 0.3 秒里，
   我们这套**区域命中**用的是抽屉的**固定面板矩形**（原版是件自己挪走）⇒ 那一瞬间点「最终位置」会命中。
   0.3 秒的近似，⛔ 不算原版行为，如实记在这里。
5. **`NameLimitFor`** 只按 `_editKind` 分档；原版两个输入框的 `characterLimit` 值（26 / 0）来自
   `报告/B138_审计汇总_问题清单.md` §3 的 3-14/3-15 行（B131 卡组 2-1 #8/#9），本轮**没有**再回 prefab 现读。
6. **`5-14` 之类不属本包**，未碰。

## 六、自检

- ✅ `bash 工具/typecheck.sh` → 运行时 **0** / 编辑器 **0**（本轮共跑 5 次，最后两次都是 0/0）。
- ⛔ 未跑 Unity（红线）；`Editor/DeckScene.Run` 那批**要等 §四 的 6 处宿主断言改完**再跑。
- 断言条数：`B141_W29_Assert` 现读 **A 6 条 · B 4 条 · C 5 条 · T 9 条 · D 10 条（其中 D0/D1/D7a/D8a 是前提）＝ 34 条**（`Run(_rt)` 口径；`Run()` 无实例时 A4–A6 记 SKIP、D 段记 SKIP）。

---

## 七、续做 `B141-W29b`：宿主断言更新 + 接线（2026-11-04）

> 白名单：`Editor/DeckScene.cs`（**只改 §四 那 6 处 + 接线**）· `Editor/B141_W29_Assert.cs`（同步）· 本报告。
> ⛔ 未跑 Unity · 未动 git · 未改账主表。✅ **`DeckScene.cs` 行数 10,582 → 10,582（零变化）**；CRLF 10,582/10,582、裸 CR 0。

## 七·一 7 处逐条（改前 ⇒ 改后）

| # | 位置（现读行号） | 改前 | 改后 |
|---|---|---|---|
| **U1** | `DeckScene.cs:4561-4562` | `CheckNear(_rt.FilterDrawerSlide, 0.5f, 0.01f, "…进度 0.5…")` | **`0.75f`** + 文案「OutQuad `1−(1−0.5)²`；线性会给 0.5 ⇒ 必红」 |
| **U2** | `DeckScene.cs:4559` | `CheckTrue(!_rt.FilterDrawerInteractive, "…起点当然不算到位 ⇒ 命中仍失效")` | `CheckTrue(_rt.FilterDrawerInteractive, "…目标是开 ⇒ 在画面上就算生效（§3-29 原版 `Toggle` 不屏蔽点击）")` |
| **U3** | `DeckScene.cs:4578-4599`（22 行 → 22 行） | 两态 = 「位移途中**不吃** / 到位吃」 | 两态 = **「开=吃（含途中 0.5 也吃）/ 关到底=不吃」**，中间加一次「关→重开」把状态还原；`关到底` 那条把 `UiClickPx` 放在**最前**（避免被 `&&` 短路成恒真） |
| **U4** | `DeckScene.cs:4610` | `CheckTrue(!_rt.FilterDrawerInteractive, "…但已经不吃点击/滚轮")` | `CheckTrue(_rt.FilterDrawerInteractive, "…节点仍活着 ⇒ 原始命中位为真；真正关掉点击的是逻辑态 `_filtersOpen`（上一段已断 = false）")` |
| **U5** | `DeckScene.cs:3631-3634`（注释 4 行） · `:3651-3654`（注释 4 行） · `:3664` · `:3668` · `:3672` · `:3674-3675` | 无督军档整段钉 `x1`（判据写的是收藏页 `CardCollectionDisplay__SetCell`） | 注释改成 `DeckEditorCollectionDisplay__DrawCell.c`（`Count(GetLibraryInFull)` + 恒 `{0}/{1}`）；期望串 `x1` ⇒ **`0/1`**，反例 `x2` ⇒ **`0/2`** |
| **U6** | `DeckScene.cs:3629`（+ 上一行注释） | `_rt.State.Deck.CountOf(poolDef.Id) + "/" + cap` | **`_rt.UiDeckCountInFull(poolDef.Id) + "/" + cap`**（含督军/防御卡那两张） |
| **U7** | `DeckScene.cs:5555` | `… _sink.Fail += B141_W26_Assert.Fail;   // 🆕 2026-11-04（B141_W26）：…` | `… _sink.Fail += B141_W26_Assert.Fail; B141_W29_Assert.Run(_rt); _sink.Pass += B141_W29_Assert.Pass; _sink.Fail += B141_W29_Assert.Fail;   // 🆕 2026-11-04（B141_W26 / B141_W29 合并追加 ⇒ 行数零变化）：…`（**插在 `//` 之前** —— 那行本身以注释结尾，追加到行末会落进注释里） |

## 七·二 🔴 就地订正 §四 U4（铁律 5）

上一轮我在 §四 写「U4 收起途中仍绿、只改语义注释」—— **那句不成立**。
`FilterDrawerInteractive` 读的是 `d.Interactive`，它现在 = `live`（`Slide>0 || SlideTarget>0` 且 `PageOn`）⇒ 收起途中（0.5）它是 **true**，
旧断言 `CheckTrue(!_rt.FilterDrawerInteractive, …)` **会红**。已按「节点仍活着 / 真正关点击的是 `_filtersOpen`」改写（见上表 U4）。

## 七·三 断言文件同步（`B141_W29_Assert.cs`）

- `GroupA_Thresholds` 改成**吃宿主递进来的 `rt`**（不再 `FindObjectOfType` 兜底，拿不到才回退） —— 免得在同一工程里挑到另一台实例。
- `GroupD_Instance` 重排成**只读**：`_tab != 0` ⇒ D1–D4 出声 SKIP；`UiDragScroll` 滚完按**进入时的位置**还原（⛔ 不是置 0）。
- **3-26 改成纯函数断言**（新增 `DeckRuntime.UiDroppedOnDeckPanel(x,y)`）：面板内 `(150,700)` = 真 · 右外 `(1500,700)` = 假 · **只在 y 上出界 `(150,200)` = 假** —— 最后一条是「只比 x」的旧写法**必错**的那一格，是最硬的灭自证。
- **3-27/3-28（行拖摘牌/放回）整条链故意不在这里跑**：它会改 `CardIds` 与 `DeckDirty`，而宿主这一刻后面还有一批「起手必须干净」的断言（`DeckScene.cs:6310` / `:8056` 等）。那条链由宿主**既有**的一对覆盖：`UiDragDeckRow(2,150)` 张数不变（丢回面板）/ `UiDragDeckRow(2,1200)` 少一张（丢在面板外）—— 两条都走 3-26/3-27 的同一段新代码。
- 结果：`Run(_rt)` 口径 **A6 · B4 · C5 · T9 · D6（+1 条 SKIP 说明）= 30 条断言 + 2 条 SKIP**。

## 七·四 灭自证（改回旧值必红）

- **U1**：把 0.75 改回 0.5 ⇒ 红（OutQuad 给 0.75）。
- **U2/U4**：把 `FilterDrawerInteractive` 那一侧再取反 ⇒ 红。
- **U3**：把「途中仍吃」那段删掉，或把「关到底不吃」的 `UiClickPx` 挪到 `&&` 后面 ⇒ 前者弱化、后者被短路成恒真 ⇒ 审查时按这一行认。
- **U5**：期望串改回 `x1` ⇒ 红（实现出 `0/1`）。
- **U6**：改回 `CountOf` ⇒ 对**普通卡**仍绿（两式等价），**对督军/防御卡那两格才红** —— 这是这条的边界，如实记。
- **U7**：删掉 `B141_W29_Assert.Run(_rt)` ⇒ 本文件 **0 条**断言被跑（判据：日志里一条 `B141W29` 都没有）。

## 七·五 自检

- ✅ `TMPDIR=/tmp/wf_b141_w29b bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- `Editor/DeckScene.cs`：**10,582 → 10,582 行**（7 处全在同一物理行内或等价行数替换）。
- ⛔ 未跑 `DeckScene.Run`（红线：Unity 批处理只有调度台能跑）；跑之前**不需要再改**本文件的任何断言。

---

## 八、续做 `B141-W29c`：收掉 W30 留下的两条筛选断言（2026-11-04）

> 背景：`B141_W30`（P2 包）落地了 **3-01** —— `armyHidden = !ArmyRowVisible(state)`（原版 `CardArmyFilter__SetFilterToDeck.c:20-25`：
> **有督军 __或__ `CardCount(hero) > 0`** ⇒ `FilterToArmy(deckArmy)` 后整行 `SetActive(false)`）。
> 于是 `DeckScene` 里两条「按旧世界」写的断言必红。⛔ 未跑 Unity · 未动 git · 未改账主表。

### 八·一 两条结果（改前 ⇒ 改后）

| # | 位置（现读行号） | 改前 | 改后 |
|---|---|---|---|
| **V1** | `DeckScene.cs:3945-3948`（N14 ② 反向控制） | `$fac:<首阵营>` 那格 **`activeSelf == true`**（"Army 那一族照旧活着"）—— 而这一拍 `ClearWarlord()` 后**卡组仍有 30 张** ⇒ 按原版 `.c:20-25` 的「**有牌**」半句，Army 行**也该隐藏** ⇒ **必红** | **拆成一对**：① `$fac:<首阵营>` 那格 **`!activeSelf`**（Army 隐藏，判据写明是「有督军 **或** `CardCount(hero) > 0`」）；② 反向控制换成 **`$rar:common` 仍 `activeSelf`**（稀有度行不受那条闸影响）—— 这样「整栏一起关掉」那种改法照样被 ① 抓住 |
| **V2** | `DeckScene.cs:4482-4504`（抽屉里同层不重叠那一节） | `draw = GetComponentsInChildren<ImageQuad>(**false**)` 后 `CheckTrue(draw.Count >= 30, …)` —— 13 颗阵营格被 `SetActive(false)` ⇒ 计数掉 13 ⇒ **必红** | **拆两条**：① **含 inactive** 的健全检查（`allDraw` 用 `true` 再扫一遍）+ **差额判据**（`allDraw.Count - draw.Count >= 13` = 隐藏的就是那 13 格）；② **逐格 `activeSelf`**：`foreach Factions()` ⇒ `$fac:*` **全部隐藏**（`aOn == 0` 且格数 == 阵营数）／`foreach RarityKeys` ⇒ `$rar:*` **全部显示**（`rOn == rN > 0`）。重叠那一段仍用**可见**那一份（`draw`）量，语义不变 |

### 八·二 附带的一处注释订正（铁律 5，同一段内）

`DeckScene.cs:3916-3918` 原来写着「② 的反向控制要拿 Army 首格当对照」+「别改成滚到底，否则 Army 那一族照旧活着会假红」——
这两句在 3-01 落地后**都不成立了**。已就地改成：「Army 格按 3-01 的闸隐藏，② 改断『Army 隐藏 / 稀有度行照旧活着』」（3 行 → 3 行）。

### 八·三 灭自证

- **把 `armyHidden` 改回恒 false**（Army 行恢复显示）⇒ V1 的「`!activeSelf`」红 + V2 的 `aOn == 0` 红 + `allDraw-draw >= 13` 红（三处同时红）。
- **把 `draw` 换回不含 inactive** ⇒ V2 的差额判据红（`draw == allDraw` ⇒ 差 0）。
- **把整栏一起关掉** ⇒ `$rar:common` 不活 ⇒ V1 第二条 + V2 的 `rOn == rN` 红。
- **只关稀有度行不关 Army** ⇒ V1 第一条 + V2 的 `rOn` 红，两条都抓。

### 八·四 自检 / 行数

- ✅ `TMPDIR=/tmp/wf_b141_w29c bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  ⚠️ 中间有一次报 `Shell/SettingsWindow.cs(3741) CS0103 BindSelected` —— 那是**别的写手当时正在改那个文件**（不是本批）；
  隔一次复跑即 0/0（`CLAUDE.md` 那条「错误全在不是你的文件上 ⇒ 不是你的问题」）。
- `DeckScene.cs`：**10,582 → 10,584 行（+2）**（V1 那 4 行换成 4 行、零变化；V2 那 22 行换成 24 行）。
  CRLF 10,584/10,584、裸 CR 0（行尾没翻）。
- ⛔ 未跑 `DeckScene.Run`；本文件的两条已按新口径改好，不需要再动。
