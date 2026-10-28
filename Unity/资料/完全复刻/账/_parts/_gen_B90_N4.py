# -*- coding: utf-8 -*-
"""B90-N4 账补丁生成器（只读主表 + 只写 _parts/B90_N4.tsv）。

用法（必须带显式动作参数，缺参只打印用法）：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B90_N4.py --apply
"""
import io, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
LEDGER = os.path.dirname(HERE)
OUT = os.path.join(HERE, "B90_N4.tsv")
HEADER = "域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注"

if "--apply" not in sys.argv:
    print(__doc__)
    sys.exit(0)


def read_rows(name):
    b = io.open(os.path.join(LEDGER, name), "rb").read()
    nl = b"\r\n" if b.count(b"\r\n") > b.count(b"\n") - b.count(b"\r\n") else b"\n"
    lines = [x for x in b.split(nl)]
    out = []
    for ln in lines:
        if not ln.strip():
            continue
        out.append(ln.decode("utf-8"))
    return out


# (源文件, 原版类, 方法须包含的串) -> 覆盖的 5 列
# 覆盖项 = (状态, 我们的落点, 差异, 等级, 备注)
OV = {}

OV[("卡组编辑.tsv", "DeckInfoPopup", "_003CDeleteDeck_003Eb__27_3")] = (
    "A",
    "Shell/DeckInfoPopup.cs:2441-2442（DeleteDeckNow 的两支）· Shell/CollectionData.cs:545（RestoreDeck）· :527（DeckDeleteOutcome）· :213-214（DeleteDeck 写档）",
    "2026-10-28 B90-N4 **本轮补做 → 升 A**（原版两步现在都有）：① 失败横幅 = `DeckInfoPopup.cs:2442` 的 `MessageToast.ShowError(\"MenuDeck/Error/FailedToDelete\", true)`"
    "（= `__DeleteDeck_b__27_3.c:18` 的 `UIMessageController__ShowError`，键 `DAT_1842cf1f0` → `0x42CF1F0`）；`Editor/MenuCheck.cs:567-575` 有断言盯 "
    "`LastRawText`/`ShowCount`/`popUpWindow==null`。② **道具退回 = 本轮新做**：`DeckInfoPopup.cs:2440` 删之前 `CollectionData.Raw(DeckIndex)` 抓住那一副 → "
    "`CollectionData.LastDeleteOutcome == DeletedNotSaved` 时 `CollectionData.RestoreDeck(doomed)`（`CollectionData.cs:545`，内部走 "
    "`DeckLibrary.Insert` = `_decks.Add` + `Save()`）。"
    "🔴 **与原版的状态对应（本轮就地订正旧判词，实据在此）**：原版退的是「**已从库存摘掉、后端删除失败**」那件 `CardDeck`；我方 `DeckLibrary.Delete` 是 "
    "`return Save()`（`RuleEngine/Data/DeckLibrary.cs:276`）⇒ 落盘失败时 `_decks` 里**已经 `RemoveAt` 过**（= 内存与盘分叉）—— **这就是同一个状态**，"
    "判据由新增的结构化出口 `DeckDeleteOutcome`（`CollectionData.cs:527`）显式给出，⛔ 不再靠 `LastDeleteError` 里那几个字去猜。"
    "旧判词的「缺的是 `Inventory<PlayerDeck>` 库存层」**过重**：现核只需要一个回插薄口（`DeckLibrary.Insert` 本来就在）。"
    "⚠️ `DeleteDeck` 的 `bool` / `LastDeleteError` **一个字没改**（`Editor/CollectionScene.cs:11586-11599` 的 A503 断言钉着「删了没落盘 ⇒ 内存里确实少一套」）"
    "⇒ 退回放在**调用方**（= 原版回调所在的那一层）。⛔ 别把这行改回 D。",
    "P2",
    "状态轨迹：R1 A → B89-E16b C → B89-E16c 补横幅（仍 C）→ B89-E16d 维持 C → **B90-N4 补退回支 ⇒ A**。"
    "｜⚠️ 未跑 Unity：`CollectionScene`/`MenuCheck` 的既有断言未复跑；新增读口 `CollectionData.LastDeleteOutcome` 可直接断言「删了没落盘 ⇒ DeletedNotSaved 且 DeckCount 回到 n」"
    "（把 `DeckStore.OverridePath` 指到写不进去的路径那条夹具即可）。")

OV[("战斗界面_HUD.tsv", "ActiveWarlordAbility", "_003COnTouchUpAsButton_003Eb__12_0")] = (
    "C",
    "CardPresentation/Core/CardHighlight.cs:989（PressedBtnSetActive）",
    "2026-10-28 B90-N4 现核（**仍 C**）：时序与开关**都在** —— `CardHighlight.cs:957` `PressedBtnHideDelay = 0.4f`（= `.rdata DAT_1834b3158`）· "
    "`:989` 钩子口 `PressedBtnSetActive` · `:996` 收进 Tick；宿主 `BattleDriver.cs:35111-35114` 用 `_waPressedBtn` 收尾（松手后照样跑完）。"
    "**缺的仍只是那颗按钮本体**（rect / sprite 本地取不到）。本轮又按「⛔ 不按名字搜、按脚本反查」复核：`ActiveWarlordAbility` 的 MonoScript "
    "全库只有 `assets_full/globalgamemanagers/MonoScript/MonoScript_2686.json`；`grep '\"PressedBtn\"'` 全库 **0 命中**"
    "（⚠️ **该 0 不能当否定证据** —— 序列化数据不含字段名）；GO 名搜索 B87-R14 已做两轮。⇒ 判据仍空，维持 C（可见行为缺，不是时序缺）。",
    "P2",
    "与 683 合成一条施工单（同一颗 `PressedBtn`）。落点只能是钩子口 —— `:957` 是常量、不作 A 落点；本行判 C 是因为**可见行为缺**（按压反馈）。")

OV[("特效动画.tsv", "CardEffectsService", "EnableEffects")] = (
    "B",
    "CardPresentation/Battle/CardDisplayWindow.cs:915（SetEffectRows）",
    "2026-10-28 B90-N4 现核：① **已复刻**（`SetEffectRows` 把「吃的槽数」与「非空条数」分开 · `AttrText` 数值 0 回空串 · `RowsOf` 只认 `== null`；"
    "判据 `EnableCardEffectSlots.c:46-60` 的 `cursor += 1` 两支）；② **判 D**（载体上「非空 > 0」与「环境效果卡」互斥 ⇒ 建出来是恒不可达的死代码，"
    "判据 `DisplayCardEffects.c:118-135`）；③ **仍缺** = 进槽前倒序过滤 `buffType ∈ {6, 0x23, 0x1e}` 或 (`0x10` 且 `!HasQuests`)（`DisplayCardEffects.c:44-53`）—— "
    "**2026-10-28 逐字段现读** `RuleEngine/Core/UnitState.cs:1637-1671` 的 `TempBuff` 仍只有 `IsKeyword/Name/Value/Owner/UntilMyNextTurn/Src/SourceCard/Mod/TraitRec` "
    "八个成员，**没有 `buffType`** ⇒ 卡在 RuleEngine（E4 地盘，本批禁碰）。本代理零改动。",
    "P2",
    "① 已闭（B88_E6，断言同刻改过）· ② D（理由+出处见差异列）· ③ 数据侧转 E4（`UnitState.TempBuff` 扩 `buffType`）。"
    "⚠️ 连带：`特效动画:191/192/196/197`（A 行）正文里写的 `on = i < n` 现在多了 `&& !IsEmptyRow(...)` 一半 —— 语义未变、措辞待同步。")

for cls in ("CardAlternateArtDrawer", "CardDrawer"):
    OV[("菜单_外壳.tsv", cls, "ToggleNewCardBadge(bool isNewCard, bool withAnimation)")] = (
        "B",
        "CardPresentation/Core/CardView.cs:15139（ToggleNewCardBadge）",
        "2026-10-28 B90-N4 现核：**视图侧零残差** —— `Core/CardView.cs:15139` 的 `ToggleNewCardBadge(bool,bool)` 三句"
        "（`SetActive(option)` / `withAnimation && option` 才 `DOScale(1.1, 0.35, OutBounce)`；弹完停在 1.1 不回弹）与 "
        "`ilspy/BasicCardUI.cs:713-727` 逐句对得上；自检读口 `NewCardBadgeBuilt` / `NewCardBadgeShown` 也在（`:15158` 附近）。"
        "**仍 B 的两条都不在本批白名单**：①「这张是不是新卡」的**数据** —— `CardProgress`（`Shell/CardDetailPopup.cs:110` 起）现读只有 "
        "`levels / owned / wildcards` 三张表，**没有 new 标记**；② **调用点** —— 收藏格 / 详情窗在 `Shell/CollectionWindow.cs` 一族"
        "（原版**每张 Draw 各调一次**，我们卡视图会复用 ⇒ 调用方必须每张都设一次，否则残留上一张的角标）。本代理零改动。"
        "⚠️ 两条调用路径（`CardDrawer` / `CardAlternateArtDrawer`）的原版实现**逐字节同体**（良性 ICF 折叠），落点共用一处。",
        "P2",
        "N5定级 P2·序09：还差 = 「新卡」数据 + 收藏格/详情窗的调用点。｜判据三份：① 调用链（两份 `.c` 同体）② 真实现（`BasicCardUI.cs`）"
        "③ 几何/贴图（UnityPy 现读 `Card Drawer` prefab 的 `New Card Badge`）。")

OV[("菜单_外壳.tsv", "CardDrawer", "ToggleBanned(PlayModes mode)")] = (
    "B",
    "—（数据源缺失 + 落点属卡组编辑线 Deck/**）",
    "2026-10-28 B90-N4 现核（判据全文 + 缺口逐条）：`CardDrawer__ToggleBanned.c` 三句 —— ① `GameModes.GetActiveEvent(mode)` 为空 ⇒ **整段 no-op**；"
    "② `PlayEventData.IsCardBanned(card)`；③ `cardImage(+0x90).ToggleGreyScale(!banned)` + `banIcon(+0x68).SetActive(banned)`。"
    "我方现核：① `Shell/GameModesHandler.cs:228` `GetActiveEvent(PlayModes)` **已在**；② `Shell/PlayEvent.cs:347-352` `IsCardBanned(string)` **已在**"
    "（语义照抄：缓存 null ⇒ 恒 false）；③ **缺两半** —— (a) **载荷**：`bannedCardIds`（`PlayEvent.cs:344`）全仓**只有声明 + `OnAfterRead` 的读 + "
    "`PrebuiltDecks.cs` 的形参传递，没有写入点** ⇒ 恒 null ⇒ `IsCardBanned` 恒 false（= 原版「没绑活动」那一支，**行为逐字等价**，但功能等于没有）；"
    "(b) **卡格上那颗 `Ban Icon` 层我们没有**。几何判据已取到（可直接照抄）：`资料/说明书/04_界面UI/菜单全树.md:686` 的卡行 "
    "`Ban Icon`（真图 sprite pathID `4627540309930320646`，尺寸档 `461x334` / `516x383`；`-4138590900053668541` 是 2x2 占位）。"
    "⇒ 要落地 = ①活动线补 `bannedCards` 载荷 + ②`DeckRuntime` 卡行建 `Ban Icon` 层 + ③灰度（`ImageQuad` 只能改色、**做不到真灰度**，要 shader/材质侧）。"
    "本代理零改动（建出来恒不可见的层 = 死代码，⛔ 不建）。",
    "P2",
    "N5定级 P2·序10。｜[B89-R3b 订正仍有效]「活动线暂停」**作废**（活动线 2026-10-25 起已开）；真卡点 = 无 `bannedCards` 载荷 + 无 Ban Icon 层。"
    "｜落点：`CardPresentation/Deck/DeckRuntime.cs`（卡行那一段）。")

OV[("特效动画.tsv", "CardEffectItem", "LoadEffect(CardEffect effect, bool isPlayer)")] = (
    "C",
    "CardPresentation/Battle/CardDisplayWindow.cs:851（RowsOf）+ :915（SetEffectRows）",
    "2026-10-28 B90-N4 现核：三条残差**全部**落在「原版 `CardEffect` 有、`UnitState.TempBuff` 没有」的字段上 —— ① `+0x90/+0x91` 的 "
    "`visibleForPlayers`/`hiddenForPlayers` 掩码；② `+0x38` 的 `buffType` 分派（`8`/`0x11` 双行合成、`+0x78` 四个专名、`+0x40 > 1` 追加数值）；"
    "③ `+0x20` 的 `effectName` 优先于卡名。**2026-10-28 逐字段现读** `RuleEngine/Core/UnitState.cs:1637-1671`：八个成员里**一个都没有** ⇒ "
    "视图侧 `CardDisplayWindow.cs:851/915` **不可单方面补**；`RuleEngine/**` 本批禁碰。本代理零改动。"
    "⛔ 别用 `TempBuff.Name` 反推 buffType（那是第二份判据源）。",
    "P2",
    "工单 = `TempBuff` 扩档三项（`buffType` / `visibleForPlayers`+`hiddenForPlayers` / `effectName`），之后在 `RowsOf(:851)` + `SetEffectRows(:915)` 逐句落地。"
    "判据全文 → `报告/B88_E6_卡展示与手牌.md` §三·1。")

# ---------------- DragAndDropItem 六条（本轮补做 + 等价实现）----------------
DRAG_LAND = ("CardPresentation/Deck/DeckRuntime.cs:5028（MoveDragPieces 调用）· :5346（CaptureDragPieces 调用）· :5351（ClearDragPieces 调用）· "
             "类尾 :9259 CaptureDragPieces / :9288 MoveDragPieces / :9301 ClearDragPieces / :9305 DragPiecesFollowingCount")

OV[("菜单_外壳.tsv", "DragAndDropItem", "CreateEmptyObject()")] = (
    "A",
    DRAG_LAND,
    "2026-10-28 B90-N4 现核（**升 A · 等价实现**）：`CreateEmptyObject.c` 全文 = `new GameObject(name)` → `SetParent(transform.parent)` → "
    "`position = transform.position` → `localScale = 静态字段` → **末尾 `SetActive(false)`**（真正激活在 `OnBeginDrag` 里）。它唯一的可见职能 = "
    "「被拖件离开后**占住那一格排版位**」（uGUI LayoutGroup 靠它不塌）。我方行位置由 `RefreshDeckList → MoveRow`（`DeckRuntime.cs:4509`）"
    "按数据下标现算、**没有 LayoutGroup** ⇒ 被拖行整体离开后原槽天然留空（且 `ClipRowToBand` 对该槽跳过裁切，`DeckRuntime.cs:4411-4412`）"
    "⇒ 占位件无可观测职能。等价实现 = 本轮 `CaptureDragPieces/MoveDragPieces`（整行 7 件一起走）。"
    "⚠️ 原版那两半的**另一半**（reparent 到 **canvas 根**）也要有等价物 —— 本轮已补：拖拽期把这一行抬到 `QDragRowBg..QDragRowText`（3050..3054，高于本窗所有面板：侧栏 3000 / 筛选栏 3020-3023），"
    "且 `DragAutoScroll` 刷行后**再抬一次**（`RefreshDeckList → Move()` 会把队列写回行档）；松手由 `RefreshAll` 自动写回。读口 `DeckRuntime.cs:9359 DragTopRenderQueueForTest`。",
    "P2",
    "N5定级 P2·序07。｜[B89-R3d 旧判 B] 本轮改判 A：原判词说的是「整类未建」，而**占位件的职能**（留空一格）在本架构里由『整行离开 + 数据驱动排版』达成；「在 canvas 根上」那一半由拖拽队列档达成。")

OV[("菜单_外壳.tsv", "DragAndDropItem", "Initialize(")] = (
    "A",
    DRAG_LAND,
    "2026-10-28 B90-N4 现核（**升 A · 等价实现**）：`Initialize.c` = `scrollRect == null` ⇒ `CustomDebug.LogError` + `enabled = false`；否则 "
    "`enabled = true` + 存 `+0x20`(scrollRect) + 缓存 RectTransform `+0x38` +（没有静态占位就）`CreateEmptyObject`。"
    "我方**没有 uGUI ScrollRect、也没有这一颗组件**：滚动由 `_deckScroll` + `DragEdgeScrollFor`（阈值 **50** / 速度 **10**，判据 = "
    "`DragAndDropItem__.ctor.c` 的 `+0x28 = 0x42480000 = 50.0f` / `+0x2c = 0x41200000 = 10.0f`）自己算，那一族已判 A"
    "（`CheckScrollDrag` / `Update` / `OnEndDrag`）。⇒ 「绑定滚动区 + 打开组件」的功能（拖到边缘自动滚）在 `DragAutoScroll` 落地"
    "⇒ 本行按同一口径判 A（无同名组件、无可绑对象）。",
    "P2",
    "N5定级 P2·序07：同 `Awake`。｜[B89-R3d 旧判 B] 本轮改判 A：功能半边已由 `DragAutoScroll` 承担。")

OV[("菜单_外壳.tsv", "DragAndDropItem", "OnBeginDrag(")] = (
    "A",
    DRAG_LAND,
    "2026-10-28 B90-N4 **本轮补做**：`OnBeginDrag.c` 两半 —— ①「记起点偏移」= `_offset = mouseWorld − transform.position`（存 `+0x4c`/`+0x54`）；"
    "②「开占位」= `CreateEmptyObject` + `SetSiblingIndex(原下标)` + `SetActive(true)` + 把件 reparent 到 root。"
    "我方 ① 现在**逐件**记偏移（`CaptureDragPieces`：行底/色条/描边/费用圆/费用字/份数/行名 = 7 件，`offset_i = fromWorld − piece.position`）—— "
    "**修前**只有 `_dragOrigin`（指针起点），**件本身不记偏移** ⇒ 起拖瞬间行心吸到指针（可见跳变）；② 整行 7 件一起离开 ⇒ 原槽留空 = 占位件职能。"
    "⚠️ **本轮新查出、未做**：这方法开头还有**拖拽角度闸** —— `angle = |atan2(delta.y, delta.x)| · 57.2958`，`angle < 30 || 150 < angle` 才走「拖件」，"
    "落在 30..150（偏竖向）就**转交 ScrollRect 滚动**（常量本批用 `工具/read_literal.py` 现读：`0x1834b3168` = **30.0** · `0x1834b2d90` = **150.0** · "
    "`0x1834b2e98` = 57.2958 · `0x1834b2e60` = 0x7FFFFFFF 取绝对值）。我方列表没有 ScrollRect，且**竖向拖拽现在承担「重排」**（`EndDragAt`，另一行已判 A）"
    "⇒ 加闸会改掉一个已验收的行为，本批**不动**，交调度台裁（报告 §待裁）。",
    "P2",
    "N5定级 P2·序07。｜[B89-R3d 旧判「等价一半」] 本轮补成整行刚性 ⇒ 升 A；残留见差异列的角度闸（新发现）。")

OV[("菜单_外壳.tsv", "DragAndDropItem", "OnDestroy()")] = (
    "A",
    DRAG_LAND,
    "2026-10-28 B90-N4 现核（**升 A**）：`OnDestroy.c` 全文一句 `Object.Destroy(静态占位件)`（`+0xb8` 那一格；`Destroy(null)` 安全 ⇒ 没建占位时是 no-op）。"
    "我方**没有那颗常驻占位实体**（见 `CreateEmptyObject` 行），拖拽只持有引用 `_dragPieces`/`_dragOffsets` ⇒ 松手时 `ClearDragPieces()` 清空（本轮补，"
    "`DeckRuntime.cs:5351`），窗口销毁时随类一起走 ⇒ 没有需要显式 Destroy 的东西。⇒ 等价实现（无可释放对象）。",
    "P2",
    "N5定级 P2·序07。｜[B89-R3d 旧判 B「无对应物」] 本轮把「无对应物」确认为「无对象可释放」⇒ A。")

OV[("菜单_外壳.tsv", "DragAndDropItem", "OnDrag(")] = (
    "A",
    DRAG_LAND,
    "2026-10-28 B90-N4 **本轮补做**：`OnDrag.c` = `pointerWorld = GetMousePositionInWoldSpace(mousePosition)` → `position = pointerWorld − offset`"
    "（offset 取自 `+0x4c/+0x54`，= **刚性跟随**）。**修前**我方是 `_dragQuad.transform.position = (wp.x, wp.y, z)` —— 把行心**吸到指针**，且**只搬行底 + 行名两颗** ⇒ "
    "① 起拖瞬间整行跳到指针下（丢掉抓取偏移）；② 色条/描边/费用圆/费用字/份数 **4 件留在空槽里**（拖拽时「半行走了、半行还在」，可见缺陷）。"
    "**修后** = `MoveDragPieces(wp)`：7 件各按自己的 offset 刚性平移，逐帧等价于原版「整件一起走」；断言读口 `DragPiecesFollowingCount`（= 7 才算整行）。"
    "⚠️ 位置之外还有**层级**：原版把件 reparent 到 canvas 根 ⇒ 拖出列表时画在所有面板之上；我方等价物 = 拖拽期队列档 `QDragRowBg..QDragRowText`（3050..3054，> 侧栏 3000 / 筛选栏 3020-3023），"
    "并在 `DragAutoScroll` 刷行后重抬 —— ⛔ 只在起拖设一次会被 `RefreshDeckList → Move()` 写回行档（`MoveDragPieces(wp, reapplyQueues:true)` 就是为这条）。",
    "P2",
    "N5定级 P2·序07。｜[B89-R3d 旧判「等价」] 本轮认定旧写法丢抓取偏移且只搬 2 件（可见缺陷）⇒ 补做后升 A。")

OV[("菜单_外壳.tsv", "DragAndDropItem", "Toggle(bool option)")] = (
    "A",
    DRAG_LAND,
    "2026-10-28 B90-N4 现核（**升 A · 等价实现**）：`DragAndDropItem__Toggle.c` 体就一句 `Behaviour::set_enabled(option)`（首行是 `TouchInputManager__Toggle` —— "
    "**良性 ICF 折叠**，⛔ 别判「解错」）。它是**组件开关**：`Toggle(false)` ⇒ 组件不再收 `OnBeginDrag/OnDrag`。我方没有对应组件，等价物 = `_dragging` 状态位"
    "（`StartRowDrag` 置真 `DeckRuntime.cs:5345` · `EndDrag` 置假 `:5351`；`Update` 里 `if (_dragging && _dragQuad != null)` 就是「开着才吃拖拽事件」）。"
    "⇒ 语义等价（没有可开关的组件）。",
    "P2",
    "N5定级 P2·序07。｜[B89-R3d 旧判 B] 旧词自相矛盾（既写「语义等价」又判 B）⇒ 本轮判 A。")

OV[("菜单_外壳.tsv", "UIGradientUtils", "LocalPositionMatrix(")] = (
    "A",
    "CardPresentation/Core/CardArt.cs:787/794（Gradient）· :839/845（GradientKeys）",
    "2026-10-28 B90-N4 **本轮补做**：矩阵判据两源现读——`UIGradientUtils__LocalPositionMatrix.c` 全文 + `out_il2csharp/UIGradientUtils.cs:20-36`："
    "`m00 = dir.x/w · m01 = dir.y/h · m10 = dir.y/w · m11 = dir.x/h`，`m02/m12` 由 `xMin/yMin` 平移项构成、末尾 `^ -0.0`（= **取负**，常量 `0x1834b2c80` 现读 = `-0.0f`）。"
    "**消费端** `UIGradient__ModifyMesh.c:107-158` 逐句：先把 `dir` **除成单位长**（`if (1e-5 < |dir|) dir /= |dir|`），再 `t = pos · (dir/w, dir/h) + 平移`，随后**直接 `clamp01`**（无色域重映射）。"
    "我方 `CardArt.Gradient/GradientKeys` 本轮改为 **方向单位化 + 不再除 `|dx|+|dy|`**（那一除是**我们自加的满量程拉伸** ⇒ 斜向色带比原版**窄**，差 1～1.41 倍）；"
    "矩形项等价：原版 `xMin/yMin` 两项在「贴图铺满 quad」口径下与我们的 `u,v ∈ [-0.5,0.5]` 恒等；矩阵**第二行**（`m10/m11/m12`）原版**没有消费者**（`ModifyMesh` 只读第 0 分量）⇒ 不物化。"
    "⚠️ **视觉面**：改了所有非轴对齐渐变的色带宽度（`Gradient(…,188f)` 那族：主菜单底图 / 战役-活动底图 / 每日奖励等）⇒ 需在同步点并排图复核。",
    "P2",
    "⚠️ 时效：判据 2026-10-28 现读；视觉待并排复核。｜同族未动：`CompensateAspectRatio` 乘的是**哪一个分量**（我们乘 `dy = sin·h/w`，判据行文写 `dir.x·h/w`）—— 与本行正交，本轮不改，见报告 §待裁。")

OV[("菜单_外壳.tsv", "MenuSprites", "GetFriendlyWorldSprite()")] = (
    "C",
    "无（`MenuSprites` 全仓 0 命中；主菜单背景 = `Core/CardArt.Gradient` 代码渐变）",
    "2026-10-28 B90-N4 现核（**仍 C，缺的是资产不是行为**）：`MenuSprites__GetFriendlyWorldSprite.c` 全文一句 `return *(this + 0x38)`（首行是 "
    "`PhotonLagSimulationGui__get_Peer` —— 同体折叠，⛔ 不是解错），与 `out_il2csharp/MenuSprites.cs:47-50` 一致 ⇒ 「返回序列化字段 `friendlyWorld`」。"
    "**本机取不到那件资产**：`MenuSprites` 这只 SO 只有 `globalgamemanagers` 的 MonoScript、没有实例（同 `GetWorldSprite` 行）；主菜单背景现由 "
    "`MainMenuRuntime.cs:773` 的 `CardArt.Gradient` 代码渐变生成。⇒ 缺的是「字段指向的那张图」，判 D 的条件（原版没有/剥掉）**不成立**，故留 C。",
    "P2",
    "N5定级 P2·序11。｜与 `GetWorldSprite(int playerTier)` 同一件 SO（`worlds[]` 一条都没有）。")

# ---------------- 生成 ----------------
header = None
rows_out = []
used = set()
for fname in ("卡组编辑.tsv", "战斗界面_HUD.tsv", "特效动画.tsv", "菜单_外壳.tsv"):
    for line in read_rows(fname):
        cols = line.split("\t")
        if cols and cols[0] == "域" and len(cols) >= 4 and cols[1] == "原版类":
            header = line
            continue
        if len(cols) != 11:
            continue
        for (f, cls, m), val in OV.items():
            if f != fname or cols[1] != cls or m not in cols[2]:
                continue
            key = (f, cls, m)
            if key in used:
                raise SystemExit("重复命中：%s %s %s" % (f, cls, m))
            used.add(key)
            new = cols[:]
            new[5] = val[0]
            new[6] = val[1]
            new[7] = val[2]
            new[8] = val[3]
            new[9] = "B90"
            new[10] = val[4]
            rows_out.append("\t".join(new))

missing = set(OV.keys()) - used
if missing:
    raise SystemExit("这些覆盖项没在主表里找到：\n" + "\n".join(map(str, sorted(missing))))
if header is None:
    raise SystemExit("没读到表头")

buf = "\r\n".join([header] + rows_out) + "\r\n"
io.open(OUT, "wb").write(buf.encode("utf-8"))
print("写出 %s：%d 行（+表头）" % (OUT, len(rows_out)))
