# -*- coding: utf-8 -*-
# ⚠️ 本批（B93_U1.tsv）**已并入主表**；列的 off-by-one 已由 B93_U1b / B93_U1c 修复。
#    ⛔ 不要为了「重跑」而覆盖已合并的补丁 —— 只在确认需要重出时才跑，并带着 b/c 两份一起核。
# B93_U1 patch generator: 只改「未分类_其他.tsv」里归我（B/C 61 行）中的本轮落地/订正行。
import io

MAIN = 'd:/4/Unity/资料/完全复刻/账/未分类_其他.tsv'
OUT = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1.tsv'
TAG = '｜[B93_U1 2026-10-28] '
RND = 'B93_U1 2026-10-28'

b = io.open(MAIN, 'rb').read().decode('utf-8')
lines = b.split('\n')
hdr = lines[0].split('\t')
assert len(hdr) == 11, hdr
rows = [l.split('\t') for l in lines[1:] if l.strip()]
idx = {(r[1], r[2]): r for r in rows}

U = {}


def add(cls, meth, **kw):
    U[(cls, meth)] = kw


# ============================ A 档迁移 ============================
add('RectExtensions', 'GetWorldRect(RectTransform rect)',
    status='A',
    land='CardPresentation/Core/UguiRect.cs:345（GetWorldRect）（等价实现 = 同签名 `RectTransform → Rect`，逐句照 `decomp_full/RectExtensions__GetWorldRect.c:20-29`）',
    judge='cpp2il RectExtensions.cs:246-278 = UIExtension.GetCorners 取四角，用中心与 (max-min) 组 Rect｜B93_U1 逐句复核：以 `c[0]` 为基点、`c[2]` 为**对角** → 中心 = `c0 + 对角线 × 0.5`（`DAT_1834b2bb4` = **0.5**）、size = 对角线；旋转矩形上同样给**轴对齐包围盒**',
    diff='机制不同 → **已对齐**：本轮不再拿「我们全在同一 px 空间」搪塞，而按原版**补了同形方法本体**（`RectTransform → Rect`，`Core/UguiRect.cs:345`），静态复用四角数组的语义也与原版一致。⚠️ 本仓调用点仍无（原版调用方 = PolyAndCode 回收列表 + `UIExtension` 同族）',
    note='B→A（补方法本体，判据逐句；⚠️ 无本仓调用点，与原版调用方不同域）')

add('UIExtension', 'GetCorners(RectTransform rectTransform)',
    status='A',
    land='CardPresentation/Core/UguiRect.cs:320（GetCorners）（等价实现 = `GetWorldCorners(4)` 写进静态复用数组，原版 `DAT_18429f330` 同形）',
    judge='UIExtension.cs:7 GetWorldCorners(4) 后返回数组｜B93_U1 逐句：四角写进**静态复用**的 `Vector3[4]`（原版 `FUN_1803f37c0(DAT_18429f330,4)`）⇒ 后一次调用覆盖上一次的返回值，语义相同；`null` 入参原版走 throw（`FUN_1803f47a0`）',
    diff='机制不同 → **已对齐**：界面坐标仍用 `PxRect`，但**方法本体已按原版落地**（同签名、静态复用数组、null 抛）。⚠️ 调用点无（原版调用方 = `RectExtensions` / PolyAndCode）—— 本轮 `RectExtensions.GetWorldRect` 已用上它',
    note='B→A（B92_N5b 调度台裁定的「缺口 = 缺方法本体」本轮补上；调用点仍无）')

for name, ln, off, chk, corner in [
        ('MaxX', 328, '0x38 = 0x20 + 2×0xC', '`2 < count`', 'corners[2].x（右上）'),
        ('MaxY', 330, '0x30 = 0x20 + 1×0xC + 4', '`1 < count`', 'corners[1].y（左上）'),
        ('MinX', 332, '0x20', '`count != 0`', 'corners[0].x（左下）'),
        ('MinY', 334, '0x24', '`count != 0`', 'corners[0].y（左下）')]:
    add('UIExtension', '%s(RectTransform rectTransform)' % name,
        status='A',
        land='CardPresentation/Core/UguiRect.cs:%d（%s）（等价实现 = 固定下标取角：`%s`）' % (ln, name, corner),
        judge='UIExtension.cs:%s｜B93_U1 逐句（`.c` 的数组字节偏移）：**不是「扫四角取最大/最小」，是按固定下标取某一颗角** —— `%s`（偏移 `%s`；长度校验 `%s` 也逐条对上）⇒ 轴对齐矩形上 Max/Min 同值，**旋转过的矩形上不同**（铁律 5·c）。Unity 角序 = 左下→左上→右上→右下（旁证 `Battle/HudPiece.cs:441`）' % ('36' if name == 'MaxX' else ('18' if name == 'MaxY' else ('45' if name == 'MinX' else '27')), corner, off, chk),
        diff='机制不同 → **已对齐**：方法本体按原版落地（固定下标，⛔ 不是 `Mathf.Max` 扫描）。⚠️ 调用点无（同 `GetCorners`）',
        note='B→A（含「名字叫 Max/Min、实际取哪颗角」这条判据）')

add('RectExtensions', 'OverlapsAny(RectTransform rect, IEnumerable<RectTransform> rects, out RectTransform overlap)',
    status='A',
    land='CardPresentation/Core/UguiRect.cs:244（OverlapsAny）（等价实现 = PxRect 版：严格 AABB 相交 + 按传入序取第一个；`:258` 两参重载）',
    judge='cpp2il :69-... = 按传入序返回第一个相交的 RectTransform｜B92_B4b 已核 `.c`：`fVar13+fVar11 <= fVar12` 三条「不交」取反 ⇒ **严格不等** AABB；命中即写 out 并返回，**不排序、不取最近**',
    diff='统一件**已建**（`:244`/`:258`）。残差 = 宿主改用（`Deck/DeckRuntime.cs` / `Shell/CollectionWindow.cs` 仍各写各的内联式；`UguiRect.OverlapsAny(` 现读 0 处真调用）⇒ 那是「宿主改用 + 复测」的活，另账，不再挂在方法级这行',
    note='B→A（方法级：判据 `.c` 逐句、实现 `:244` 逐句；宿主改用另账）')

add('GenericArmyItem', 'GetID()',
    status='A',
    land='CardPresentation/Shell/ItemDrawer.cs:126（Id）（等价物 = 字段直读：`ItemSpec.Id` 就是「这一项的 id」）',
    judge='`decomp_full/GenericArmyItem__GetID.c` = `return *(this + 0x30)`（⚠️ **`.c` 首行是 `RoomOptions__get_plugins` = ICF 折叠**，体是同一句「返回 +0x30 字段」）',
    diff='等价物 = **字段直读**（§4.1 纯取值器例外）：原版 `GetID` 无逻辑、只返回自身 id 字段；我们的 `ItemSpec.Id`（服务端 id = SO 的 `m_Name` / `RewardInfo.targetId`）就是同一格。⚠️ `ItemSpec` 是「原版 `ObtainableItem` 的替身」，没有 `GenericArmyItem` 这个子类型本身',
    note='B→A（纯取值器 ⇒ 字段直读）')

add('GenericArmyItem', 'GetItem()',
    status='B',
    land='CardPresentation/Shell/ItemDrawer.cs:126（Id）（等价物 = 调用方按阵营构造 `ItemSpec`，⛔ 无「当前阵营」上下文）',
    judge='`decomp_full/GenericArmyItem__GetItem.c`：`handler = LiveOpsManager.GetHandler<T>()` → `GetItem(handler.CurrentArmy)`（`handler + 0x7c`；handler 为 null ⇒ 传 0）',
    diff='缺口 = **「当前活动阵营」这个上下文**：原版从 LiveOps handler 的 `CurrentArmy(+0x7c)` 取，本地没有 handler 状态 ⇒ `ItemSpec.CardArmy` 一律由调用方显式给（= 原版 `GetItem(army)` 那一支的入参）。0 参档在本仓**没有对应物**，也不该造一个假的「当前阵营」',
    note='落点订正（原写 `:427` = `ItemTypeSets` 表里那一行，不是等价物）；状态仍 B')

add('GenericArmyItem', 'GetItem(CardArmy army)',
    status='B',
    land='CardPresentation/Shell/ItemDrawer.cs:126（Id）（等价物 = `ItemSpec.CardArmy` 字段由调用方按阵营填）',
    judge='同 `GetItem()`：按 `CardArmy` 取该项（`GenericArmyItem__GetItem(param_1, army, 0)`）',
    diff='按阵营取这一支的**入参语义已由 `ItemSpec.CardArmy` 承载**（野牌/战役点/GenericArmyItem 三种抽屉都读它：`ItemDrawer.cs:138`）；缺的是「按阵营反查 item 表」的数据源（原版 `LiveOpsManager` 的 handler 列表）',
    note='落点订正；状态仍 B')

add('ReferralContainer', 'OnClick()',
    status='A',
    land='CardPresentation/Shell/WindowsManager.cs:1297（OpenReferralPopup）（等价实现 = 开推荐人窗：`OpenByRef` 取 prefab 引用 + `Show(view)`）',
    judge='`decomp_full/ReferralContainer__OnClick.c` 体 = `return ReferralContainer.OpenPopup()`（⚠️ 该产物**首行是 `ReferralContainer__TryPurchase`**：两个方法体逐字相同 = ICF 折叠，不是文件错位 —— `TryPurchase.c` 同体）',
    diff='行为一致：原版点钮 = 开 `ReferralPopupWindow`（`OpenPopup` → `WindowsManager.OpenWindow(GetComponentReference(7, 0xc), data)`）；我们 = `WindowsManager.OpenReferralPopup`（同一 prefab 引用 → 同一个开窗口）。⚠️ **容器本体（推荐人条）未建 ⇒ 无生产调用点**（见 `ContainerBuilder`/`ContainerHolder` 那 7 行）',
    note='C→A（方法级；两句体 = 转调 `OpenPopup`，按 §4.1「多个原版方法由同一段代码实现」的共享落点口径）')

add('ReferralContainer', 'OpenPopup()',
    status='A',
    land='CardPresentation/Shell/WindowsManager.cs:1297（OpenReferralPopup）（等价实现 = `OpenByRef(PrefabRefReferralPopup, Create)` + `Show`）',
    judge='`decomp_full/ReferralContainer__OpenPopup.c`：`data(+0x30)` 非空 ⇒ `WindowsManager.Instance.OpenWindow(LiveOpsAssetUtility.GetComponentReference(7, 0xc), data)`',
    diff='行为一致（同一开窗口 + 同一 prefab 引用；原版 `data` ⇒ 我们的 `ReferralPopupWindow.ReferralView`）。⚠️ 原版 `data` 是 LiveOps 报价数据（远端）⇒ 我们 `null` 走原版自带的 no-data 档',
    note='C→A')

add('ReferralContainer', 'Refresh()',
    status='C', level='P2',
    land='CardPresentation/Shell/WindowsManager.cs:1297（OpenReferralPopup）（开窗那一跳有；**价签那一跳没有**）',
    judge='`decomp_full/ReferralContainer__Refresh.c`：取 `priceDisplay(+0x40)` 组件 → `I2_Loc.GetTranslation(<价格键>)` → `PriceDisplay.Setup(priceDisplay, 译串)`',
    diff='**缺价签**：`Shell/ReferralPopupWindow.cs` 全文件 `Price|Setup` **0 命中** ⇒ 没有 `PriceDisplay.Setup` 的对应物（原版那一格显示推荐奖励的价格串，键在官方本地化表里、可读）⇒ 本行确实是「该做没做」',
    note='判据补全（原版体逐句），维持 C/P2')

add('ReferralContainer', 'TryPurchase()',
    status='A',
    land='CardPresentation/Shell/WindowsManager.cs:1297（OpenReferralPopup）（等价实现 = 同 `OpenPopup`：转调开窗）',
    judge='`decomp_full/ReferralContainer__TryPurchase.c` = `return OpenPopup()`（与 `OnClick` 逐字同体）',
    diff='行为一致（同一个开窗口）。⚠️ 名字叫 TryPurchase 但原版**不做购买**、只是开窗 ⇒ 我们也没有多做一步',
    note='C→A')

add('EmptyGraphic', 'OnPopulateMesh(VertexHelper vh)',
    status='A',
    land='CardPresentation/Shell/MenuDraw.cs:2197（Hit）（等价实现 = 命中节点不画：`hq.SetTint(new Color(0,0,0,0))` 在 `:2292`）',
    judge='`decomp_full/EmptyGraphic__OnPopulateMesh.c`（全文 5 行）= `VertexHelper.Clear(vh)` ⇒ **不画任何顶点、但 Graphic 仍在**（`raycastTarget` 照样命中）；实例 = 各 `Hit`/纯命中节点',
    diff='机制不同但语义等价：原版把网格清空（图形在、画面无），我们是「建命中节点 + 把 quad 的 tint 设成全透明」⇒ 同样「画面无、命中在」。⚠️ 不能改成「不建 quad」：`PointerLayer.CollectHits` 只认 `ImageQuad`（A26 那个真缺陷）',
    note='B→A（逐句对 `VertexHelper.Clear` 那一支）')

add('MessageList', 'Refresh()',
    status='A',
    land='CardPresentation/Shell/InboxWindow.cs:872（Initialize）（等价实现 = 清空 `_messages` 后重铺：`BuildMessageList` + `BuildMessageRows`）',
    judge='`decomp_full/MessageList__Refresh.c` = 按 `messages` 重铺列表（先清后建）',
    diff='行为一致：原版无参重铺 = 我们 `Initialize(messages)`（`_messages.Clear()` → 重填 → `BuildMessageList()`）；滚动改变时的幂等重铺 = `BuildMessageRows`（先清后建，`PointerLayer.UnregisterOwnedBy` 先撤登记）',
    note='B→A（方法级；入口名不同：`Initialize(messages)` 与 `MessageList.Initialize` 同形）')

add('UnitOnBoardAttackTypeSelector', 'Toggle(bool option, CardScript card)',
    status='A',
    land='CardPresentation/Battle/AttackSelector.cs:1160（Show）（等价实现 = 展开/收起选择器，选项集由驱动算好传入）',
    judge='`decomp_full/UnitOnBoardAttackTypeSelector__Toggle.c`：`card==null` ⇒ 清 `+0x38` 且 `option=false` → `gameObject.SetActive(option)` → 后续按 `card` 填选项，并把整条 `set_position(单位位置)`（`:112-126`）',
    diff='一致（原记「等价实现 = Show」维持）：`Show(options,…,atWorld,…)` = 展开入口（`atWorld` = 原版那一句 `set_position`）；收起 = `Hide()`；选项集由 `BattleDriver` 算好传入（= 原版按 `card.attackType` 现算那段搬到驱动侧）',
    note='B→A（P0 行，方法级两侧都对上）')

# ============================ C 档迁移 ============================
add('UICornersGradient', 'ModifyMesh(VertexHelper vh)',
    status='C', level='P2',
    land='CardPresentation/Core/CardArt.cs:776（Gradient）（⚠️ 只是**两色线性**渐变，不是四角）',
    judge='`decomp_full/UICornersGradient__ModifyMesh.c`：`BaseMeshEffect` 之后逐顶点按**四个角各自一个颜色**插值改写顶点色（topLeft/topRight/bottomLeft/bottomRight）',
    diff='**原写「等价 = CardArt 渐变烘焙」不成立**：`Core/CardArt.cs:776 Gradient(c1,c2,angle)` / `:822 GradientKeys` 都是「两色 + 角度」线性渐变（判据写的是 `UIGradientUtils`/`UIGradient`），**没有四角插值** ⇒ 缺口 = 四角渐变的逐顶点实现 + 用它的 prefab 实例。ℹ️ 本轮未逐实例数（m_Script pathID 未解），先按「该做没做」记 C',
    note='B→C（机制缺口坐实：两色线性 ≠ 四角插值）')

add('RectSizeLimiter', 'SetLayoutHorizontal()',
    status='C', level='P2',
    land='CardPresentation/Shell/PracticeModePopup.cs:516（WnL/WnT/WnR/WnB 常量）（右沿 = `DnR`，代码里已标 🔶 自己设计）',
    judge='`decomp_full/RectSizeLimiter__SetLayoutHorizontal.c`：`minSize.x>0` 且 `rect.width < minSize.x` ⇒ `SetSizeWithCurrentAnchors(Horizontal, minSize.x)`；`maxSize.x>0` 且 `rect.width > maxSize.x` ⇒ 设为 `maxSize.x`；**改完发一次 SizeChanged**（`NetworkingPeer__OnMessage(+0x38, …, 0x1000)` = IL2CPP 对 UnityEvent 调用的折叠）',
    diff='**原写「原版判据拿不到」已被推翻**（`.c` 全文可读）。缺口 = ① 读 prefab 实例的 `minSize/maxSize` 两个 Vector2；② 按上面那支夹宽（`Warlord Name` 原版出厂**宽 0**，靠这两个组件运行时算出来）。我们今天的 `WnR` 是**我们挑的**（取 `Deck Name` 的 `DnR`）⇒ 这正是铁律 11 要补的那一格',
    note='B→C（P2）—— 判据已可读、原「拿不到」理由作废；⚠️ `PracticeModePopup.cs:515` 那条 🔶 标记的「理由」也要跟着改（本轮只报告，不动代码）')

add('RectSizeLimiter', 'SetLayoutVertical()',
    status='C', level='P2',
    land='CardPresentation/Shell/PracticeModePopup.cs:516（WnL/WnT/WnR/WnB 常量）（高 `WnB−WnT` 同样是固定值）',
    judge='`decomp_full/RectSizeLimiter__SetLayoutVertical.c`：与横向同形，夹的是 `rect.height` 对 `minSize.y / maxSize.y`，改完同样发 SizeChanged',
    diff='同 `SetLayoutHorizontal()`：判据可读、我们没做（高度按固定矩形建）',
    note='B→C（P2）')

CB = [
    ('MakeContainer(IContainerProvider evt, EventComponentReferenceType componentReferenceType, Transform parent)',
     '按 `componentReferenceType` 从事件里取 prefab 引用再建容器'),
    ('MakeContainer(IContainerProvider evt, Transform parent)',
     '默认档：转调上面那个重载'),
    ('MakeContainer(IContainerProvider evt, UiContainer container, Transform parent)',
     '把已建好的 `UiContainer` 挂到 holder 上并注册'),
]
for meth, which in CB:
    add('ContainerBuilder', meth,
        status='C', level='P1',
        land='CardPresentation/Shell/OfferContainer.cs:214（OfferContainer 类头）（**只覆盖商店货架**，不是通用构建器）',
        judge='通用容器构建器：%s（`decomp_full/ContainerBuilder__MakeContainer*.c`）' % which,
        diff='「活动线暂停」不能当「不做」（铁律 11）⇒ 改判 **该做没做**：本仓只有商店货架那一条自建链（`Shell/OfferContainer.cs`），**没有** `ContainerBuilder` 的通用入口（三个重载）与它服务的 LiveOps 容器表 ⇒ 活动窗/推荐人等容器件都接不上（`ReferralContainer` 4 行即为此例）',
        note='B→C（P1）—— 「活动线暂停」不是「不做」')

CH = [
    ('Clear()', '清空注册表'),
    ('GetContainers()', '返回已注册容器列表'),
    ('TryAdd(IContainerProvider provider)', '去重登记一个 provider'),
    ('TryAdd<T>(IContainerProvider provider, out T container)', '登记并回传强类型句柄'),
]
for meth, body in CH:
    add('ContainerHolder', meth,
        status='C', level='P1',
        land='CardPresentation/Shell/OfferContainer.cs:214（OfferContainer 类头）（**无**容器注册表；最近的东西 = 商店货架自建链）',
        judge='`ContainerHolder.%s` = %s（`decomp_full/ContainerHolder__*.c`）' % (meth.split('(')[0], body),
        diff='同 `ContainerBuilder` 三行：通用容器体系（builder + 注册表）整族未建 ⇒ 该做没做（P1），不是「机制不同」',
        note='B→C（P1）')

# ============================ 证据/落点订正（状态不变） ============================
add('ArmyProgressBar', 'SetArmies(CardArmy leftArmy, CardArmy rightArmy)',
    land='CardPresentation/Shell/DraftModeWindow.cs:4178（BuildAlliancePage）（只建了 `2Armies Progress Bar` 的壳：`:4185`；**`SetArmies` 未接**，双阵营图标/比分留空）',
    note='落点规范化（原为「—」）：我们那页明确出声「双阵营图标、比分、计数器留空态」⇒ 缺的正是 `SetArmies` 那两颗徽记；军队来自 draft 事件数据（远端）⇒ 维持 C/P2')

add('Countdown', 'Activate(float speed, int secondsDuration)',
    land='CardPresentation/Shell/TimerDisplay.cs:54（TimerDisplay）（⚠️ **不是同一个类**：我们这份服务的是原版 `TimerDisplay`，`Countdown` 组件本体**没有**）',
    judge='启动倒计时（速度 + 总秒数）：`Countdown.cs:23`｜B93_U1 逐句（`.c`）：取同 GO 的 TextMesh → `remaining = TimeSpan(0,0,secondsDuration,0)` → 文本 = `String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds)` → 存 `speed(+0x44)`、起点 `DateTime.Now`、running 位',
    note='B92_N5 要求的「先按 m_Script pathID 反查实例」本轮试过：`Countdown` 的 MonoScript **不在** `assets_full/bundle_Waprforge_monoscripts`（722 个 addressable 类）也不在 `_tmp_view/apk135/monoscripts.json` ⇒ 这条路要换包，**本轮没做成**；维持 B（方法体判据已补全）')

add('AddSabotageCard', 'OnInitialize()',
    note='B93_U1 逐句：体 = `new Action(this.<handler>)` + `Signal.Register<...>(action, <event token>)`；🔴 **`AddSabotageCard__OnReset.c` 也是 `Signal.Register`（另一个 event token）—— 不是反注册**。业务行为侧我们已有：`EffectResolver.cs:4221 ctx.Tally.AddSabotage(owner)` → `AchievementTally.cs:231 AddSabotage`（造出破坏即计数）。⇒ 只剩「订阅这一层」无同形入口 ⇒ 维持 B、等该形态统一口径（见报告 §4）')

add('CollectSpiritStone', 'OnReset()',
    judge='~~反注册（MissionChallenge 重置时摘掉回调）~~ 🔴 **B93_U1 就地订正**：`decomp_full/CollectSpiritStone__OnReset.c` 体 = `new Action(...)` + `Signal.Register<...>(..., <另一个 event token>)` ⇒ **不是反注册，是换一个事件类型再订阅**（`OnInitialize` 同体、token 不同）',
    land='CardPresentation/Battle/BattleDriver.cs:19474（PlaySignals）（`:19576` = `EvtKind.CollectWaystone ⇒ _waystonesCollected[seat] += …`；`:32547` = 结算交出并清零；消费口 `Shell/ProfileData.cs:682`）',
    note='B93_U1：原判「反注册」**不成立**（已就地订正）；落点规范化（原写 `:19459（UnitDamageCueIndex）`，那一行与路基石无关）；本行「缺订阅/注销这一跳」⇒ 实际缺的是「订阅这一层」⇒ 维持 C/P2、等该形态统一口径')

add('CreateSecret', 'OnInitialize()',
    judge='cpp2il :15-19 = Signal.Register(OnAddCard)｜B93_U1 逐句：体 = `Signal.Register<...>(new Action(this, <handler>), <event token>)`（`CreateSecret__OnReset.c` 同体、token 不同 ⇒ OnReset 也是**订阅**、不是反注册）',
    land='RuleEngine/Core/AchievementTally.cs:225（AddSecret）（等价实现 = 造出隐秘时直接记一笔：`EffectResolver.cs:5051` / `:13311` 调 `ctx.Tally.AddSecret(owner)`）',
    note='落点订正（原写 `EffectResolver.cs:3926` 是一行无关的 `cnames += …`）；状态仍 B（订阅层无同形入口）')

add('CreateSecret', 'OnReset()',
    judge='同 `OnInitialize`（`CreateSecret__OnReset.c` 体 = `Signal.Register`，token 不同）⇒ **不是反注册**',
    land='RuleEngine/Core/AchievementTally.cs:225（AddSecret）（等价实现 = 同 `OnInitialize`：直接记一笔、不经总线）',
    note='B93_U1：原「反注册那一跳」的说法作废；状态仍 B')

add('CursorByArmySO', 'GetCursor(CardArmy cardArmy, out Sprite cursor, out Sprite highlight)',
    judge='在 armyCursors 表里按阵营取「指针图 + 高亮图」两张 Sprite（找到回 true）｜B93_U1 逐句（`.c`）：线性扫表，`entry.army(+0x10) == 入参` 才算命中，再对 `entry` 的两个 `AssetReferenceTyped` 各 `Load(true)` 一次写 out；**扫完没有 ⇒ 两个 out 都置 null 并 return 0（不是抛）**',
    land='CardPresentation/Shell/TutorialPointerUi.cs:284（Rebuild）（只画 `ArtPointer = Cursor_Space_Marine_01` 这一对，`:85`）',
    note='落点规范化（原指 `:82` 一行注释）；缺口不变 = 补 20 张图的 Unity import（⛔ 本代理不跑 Unity）')

add('FactionScore', 'Initialize(CardArmy faction, int currentScore, int highestScore)',
    judge='建一行：阵营徽记 + 两个分数文本｜B93_U1 补：原版 `Initialize` 还会写 `score = currentScore` / `highest = highestScore` 两条文本',
    diff='机制不同：`RankedTab.cs:631 BuildRow` 建徽记 + 两条 `Alliance Rating Display` 行；🔴 **分数留空**（`AllFactions` 那支明写「各阵营分数是服务器的」）⇒ 徽记那一半对齐、两条分数文本无本地数据源（P3 类数据），故留 B 不升 A',
    note='证据补全；状态仍 B')

add('ContentSizeFitterMinMax', 'ClampSize()',
    judge='`decomp_full/ContentSizeFitterMinMax__ClampSize.c`（全文）：读 `rectTransform.sizeDelta`，**按两条 per-axis 开关分别**夹 —— x：`+0x38`(enabled)/`+0x3c`(min)/`+0x40`(max)；y：`+0x44`/`+0x48`/`+0x4c`；夹完 `set_sizeDelta`',
    diff='**部分等价**：`Core/Tooltip.cs:606` = `Mathf.Clamp(bw + PadX*2, MinW, MaxW)` —— 只有**宽度**一轴、且没有「enabled 开关」这一层（高度不夹）。⇒ 维持 B（缺方法本体：这是一个 uGUI `ContentSizeFitter` 子类，挂在本仓不存在的 uGUI 层级上）',
    note='方法体判据补全（逐句）；按 B92_N5b 调度台裁定③(a) 维持 B')

add('ContentSizeFitterMinMax', 'SetLayoutHorizontal()',
    judge='`decomp_full/ContentSizeFitterMinMax__SetLayoutHorizontal.c`（全文 3 句）：**先调基类** `ContentSizeFitter.SetLayoutHorizontal`，**再** `ClampSize()`',
    diff='同 `ClampSize`（我们只有宽度 clamp、没有基类那一步）⇒ 维持 B',
    note='方法体判据补全；维持 B')

add('ContentSizeFitterMinMax', 'SetLayoutVertical()',
    judge='`decomp_full/ContentSizeFitterMinMax__SetLayoutVertical.c`（全文 3 句）：先调基类 `SetLayoutVertical`，再 `ClampSize()`',
    diff='同 `ClampSize` ⇒ 维持 B',
    note='方法体判据补全；维持 B')

add('UIPreferedSizeFixer', 'ApplyConfig()',
    land='CardPresentation/Core/Tooltip.cs:606（Clamp）（只有**宽度**一轴的 clamp；原版「拷布局值 + 按源 rect 定高」那一层无对应物）',
    judge='`decomp_full/UIPreferedSizeFixer__ApplyConfig.c`：`+0x38` 开关开 ⇒ 把源件(`+0x20`) 的四组布局值逐组搬到目标件(`+0x30`)（虚槽 `0x2f8/0x308/0x388/0x398`），再用源 `RectTransform` 的 rect 高走 `0x378` 写目标；⚠️ **`CalculateLayoutInputHorizontal.c` 的内容就是本方法**（本轮现读首行 = `UIPreferedSizeFixer__ApplyConfig`）⇒ 那份产物是重复件，别当独立方法',
    diff='机制不同：原版是「把已配好的布局值拷给目标件 + 按源 rect 定高」的通用修正器；我们的自适应在 `Core/Tooltip.cs` 里按 clamp 现算，**没有「拷布局值」这一层** ⇒ 维持 B（缺方法本体 + 调用点）',
    note='方法体判据补全；维持 B')

add('UIPreferedSizeFixer', 'CalculateLayoutInputHorizontal()',
    land='CardPresentation/Core/Tooltip.cs:606（Clamp）（同 `ApplyConfig`：只有宽度 clamp；本方法体在本机读不到）',
    judge='⚠️ `decomp_full/UIPreferedSizeFixer__CalculateLayoutInputHorizontal.c` 的**内容 = `ApplyConfig`**（首行逐字）⇒ 该文件不是本方法的体（IL2CPP 折叠/重名），**本方法体在本机读不到**',
    diff='同 `ApplyConfig()`：机制不同、且本方法体读不到 ⇒ 维持 B（缺方法本体 + 调用点）',
    note='B93_U1：**判据侧缺体**（文件名与内容不符实例，已记入报告）；维持 B')

add('ListExtensions', 'SpliceWhere<T>(IList<T> list, Func<T, bool> predicate, int count)',
    note='B93_U1：B92_N6 写的「升 A」**缺一个方法级落点**（要有 `文件:行（声明名）`）；本轮未定位到单一代表调用点 ⇒ 留 B（与「工具类无同名方法时不许凭内联就升 A」同口径）')

add('ListExtensions', 'GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f)',
    note='B93_U1：同 `SpliceWhere` —— 升 A 需方法级落点；`.c` 是泛型共享体（首行是 `FUN_180d10c50`，GetMax/GetMin 共用同一地址 = ICF）⇒ 判据侧也只能按「泛型比较器」读，维持 B')

add('ListExtensions', 'GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f)',
    note='B93_U1：同 `GetMax`（两个方法**同址**，`.c` 首行都是 `FUN_180d10c50`）⇒ 维持 B')

add('PlayerInfo', 'SetBotInfo(string botName, int botElo)',
    judge='cpp2il :401-418 = name=botName · IsBot=true · ProfileAllianceInfo=new · botElo>0 则 CurrentEloRating=botElo｜B93_U1 逐句（`.c`）：写 `+0x18 = botName`、`+0x11 = 1`（IsBot）、取组件写 `+0xa0`（AllianceInfo）、**`0 < botElo` 才**写 `+0x40`（elo）⇒ 三个无条件 + 一个带闸',
    note='B93_U1：天然落点 = `Shell/StrangerProfilePage.cs`（我们「看别人的档案」那一页）＋ `PlayerProfileWindow.ViewedPlayer`；缺口仍整支 ⇒ 维持 C（本轮未动代码，避免与 D 线撞车）')

add('ContainerService', 'OpenOfferContainer(TransactionEventLocation location, string offerId, string containerInstanceId, ShopOfferBase offer, Action<IReadOnlyList<RewardInfo>> onComplete, Action<string> onError, bool autoCollect)',
    note='B93_U1：本行属 `Shell/OfferContainer.cs`（D1/D2/S1 名下）⇒ **只报告不改**。判据补记：`.c` 体 = 三层嵌套闭包（加载容器 → 取报价 → 回调），唯一一次外部调用在完成回调里；缺口仍是「本地没有报价数据源」⇒ 维持 C/P2')

add('UIResourceCounter', 'SetQuantity(int newQuantity, bool animated)',
    note='B93_U1：落点 `Shell/MainMenuRuntime.cs` 属 D3 ⇒ **只报告不改**；B92-N4c 的结论（残差 = `layoutGroupContentFixer.FixLayout()`，`RewardProgressBarPanel.FixLayout` 是钩子不可复用）本轮复核成立 ⇒ 维持 C/P2')

add('UIResourceCounter', 'Toggle(bool option)',
    judge='`decomp_full/UIResourceCounter__Toggle.c` 首行是 `Card2DController__Toggle`（**ICF 折叠**：两边都是一句 `gameObject.SetActive(option)`）⇒ 行为 = 显隐整颗货币件',
    note='B93_U1：维持 B（HUD 各处按需 `SetActive`，没有「整颗货币件」的同形入口）')

# ============================ 只报告（别人的文件）+ 证据补全 ============================
add('BoardCriteria', 'DeepCopy()',
    judge='`decomp_full/BoardCriteria__DeepCopy.c`（B93_U1 逐句）：`(BoardCriteria)MemberwiseClone()`（`System_Delegate__Clone` = 折叠）→ 再把嵌套的 `targetCriteria(+0x18)` 用 `TargetCriteria.DeepCopy()` 克隆一份写回 `+0x18`',
    note='**只报告不改**（`RuleEngine/Core/CountEval.cs` 属 F4）：原版要「深」克隆（嵌套判据也复制）是为了让运行时能就地改判据；我们的判据是 DTO/值语义、不共享引用 ⇒ 「深不深」在本地不产生可观测差异，但**没有方法级落点** ⇒ 维持 B')

add('HealCriteria', 'DeepCopy()',
    judge='`decomp_full/HealCriteria__DeepCopy.c`（B93_U1 逐句）：**只有** `(HealCriteria)MemberwiseClone()`（无嵌套克隆）',
    note='**只报告不改**（`RuleEngine/Data/AiScoringData.cs` 属 RuleEngine ⇒ 不在本代理白名单）：我们的评分 DTO 只读共享、不克隆 ⇒ 维持 B（无方法级落点）')

add('Enchantments', 'TryGetCard(T cardEffect, out RawCardScript cardToReturn)',
    note='**只报告不改**（`RuleEngine/Core/Enchantment.cs` 属 E25）：⚠️ 本轮在 `decomp_full` 里**没有** `Enchantments__TryGetCard*.c`（泛型方法体只在 `GenericInstMethod` 子块，需按类名单导）⇒ 判据侧**缺体**；维持 B')

add('FeatureChecker', 'OnEnable()',
    note='B93_U1：判据（`FeatureConfig` 的 featureKey）在**远端**，本地拿不到 ⇒ 这一格属「等远端/§29·d」类，不是本批能做的；维持 C/P2（⛔ 不拿「影响小」当理由）')

add('GameObjectUniqueIdentifier', 'FindComponent(string id)',
    judge='`decomp_full/GameObjectUniqueIdentifier__FindComponent.c`：取静态 `s_registry`（`**(DAT_18427bd00 + 0xb8)`）→ 命中 `id` 就回该组件、未命中回 null（`FUN_181776150` = `Dictionary.TryGetValue`）',
    note='B93_U1：按 B92_N5b 调度台裁定③(a) 维持 B（缺口 = 「id → 组件」注册表本体 + 注册点）；本轮仍没有找到按 id 取件的真实消费方')

add('IconRotation', 'Start()',
    judge='`decomp_full/IconRotation__Start.c`（全文 1 句）：`this.<accum>(+0x30) = 0f`',
    note='B93_U1：维持 C/P2（战场自转件缺驱动；实例 = `battlearenablacklegion` 的 `Background_Ring_1/2`）')

add('IconRotation', 'Update()',
    judge='`decomp_full/IconRotation__Update.c`（全文）：`accum += Time.deltaTime`；**`accum >= period(+0x2c)`** 时 `accum = 0` 并 `transform.Rotate(rot(+0x20), rot(+0x24), rot(+0x28))` ⇒ **每 period 秒转一个固定欧拉步长**（不是每帧连续转）',
    note='B93_U1：判据补全（周期触发的「定步长」而非「按帧连续」——实现时最容易写错的一格）；维持 C/P2')

add('MaterialChangerByQuality', 'Initialize()',
    note='B93_U1：维持 C/P2（画质档只走 `QualitySettings.SetQualityLevel`，这一族物件换材质/着色器整类未做）；**只报告不改**（`Editor/SettingsScene.cs` 不在本代理白名单）')

add('MaterialChangerByQuality', 'TryApply(EverguildQualitySetting qualityTest, MeshRenderer meshRenderer)',
    note='B93_U1：维持 C/P2（同 `Initialize()`）')

add('MilestoneScoreBar', 'Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones, Sprite resourceIcon)',
    judge='SetProgressBar 设滑块，再按 milestones 逐个初始化 scoringLines（行数多于阈值则 LogError）；MilestoneScoreBar.cs:39｜B93_U1 逐句（`.c`）：先 `SetProgressBar(thresholds, currentScore)`，再按下标逐行 `GenericScoreBarLine.Initialize(thresholds[i], resourceIcon)`；`lineCount > thresholdCount` 那支打 `CustomDebug.LogError` 后返回',
    diff='机制不同：原版这一件「建条 + 逐行喂阈值」是一步；我们的 `AllianceEventScoreInfo.SetMilestones:181` 承的是 `AllianceScoreBar.Initialize`（三态：换开箱图/压暗/不动），MilestoneScoreBar 的「逐行 threshold + 图标」由 `Build` 里的 `_scores` 五行承担 ⇒ 两个原版方法共用同一批落点 ⇒ 按 2026-10-25 调度台「同一落点覆盖两个原版方法 ⇒ 留 B」维持 B',
    note='判据补全（原版体逐句）；按调度台口径维持 B')

add('ProgressBar', 'SetProgress(ScoringDataInfo scoringDataInfo)',
    judge='转调 SetProgress(CurrentScore, NextLevelScore, "{0}/{1}", useWholeNumbers)；ProgressBar.cs:58｜B93_U1 逐句（`.c`）：**外层没有自己的行为** —— 只把 `ScoringDataInfo` 的 `+0x10` / `+0x18` 两个 float 传给内层 `SetProgress(current, max, format, true, false)`（比值与文本都在内层 = 账 :497 那一行）',
    diff='机制不同：内层的「按 current/max 算填充 + 写 "{0}/{1}"」在 `AllianceEventScoreInfo.cs:153 SetProgress(float)`（比值由调用方先算）+ 文本标签；⇒ 同一落点覆盖两个重载 ⇒ 按调度台口径维持 B',
    note='判据补全（外层 = 纯转调，这一条让「外层还有行为」的顾虑消失）；维持 B')

add('SelectAttackBackgroundController', 'Awake()',
    land='CardPresentation/Battle/AttackSelector.cs:842（BuildOnCanvas）（底板 `SelectorBackground` 在 `:857` 建；原版 `Awake` 的「缓存子件引用」在我们这条过程化建法里没有对应层）',
    diff='机制不同：原版 `Awake` 只是取 `RectTransform` / 子件引用备用；我们过程化建、引用在 `Show()/BuildOnCanvas()` 时连 ⇒ 行为（底板存在且挂对图/尺寸）一致，但**没有 `Awake` 那一跳** ⇒ 维持 B（调度台口径：A 需两侧同形方法）',
    note='落点规范化（原写「CardPresentation/Battle/AttackSelector.cs（建选择器）」无行号）')

add('SimpleRotation', 'Update()',
    judge='`decomp_full/SimpleRotation__Update.c`（全文）：`euler = transform.eulerAngles`，**逐轴** `euler[i] += speed[i](+0x20/+0x24/+0x28) * Time.deltaTime`，再 `set_eulerAngles` ⇒ **按帧连续**转（与 `IconRotation` 的「周期定步长」相反）',
    note='B93_U1：判据补全（两种旋转组件的区别已写清）；维持 C/P2（竞技场 39 个自转件缺驱动；我们只有加载齿轮 −20°/s 那一处手写复刻）')

add('UIMessageController', 'OnDestroy()',
    judge='cpp2il :82-89 = 若自己就是 Instance ⇒ Destroy(gameObject)｜B93_U1 补：原版这一跳的语义 = **单例语义的收尾**（自己还是 `Instance` 才销毁），不是「清池」',
    note='B93_U1：维持 C（B91-L1 已现核：`ErrorMessageBanner` **没有**单例 `Instance`、也没有 `Awake`/`OnDestroy`）——缺口 = 单例守卫 + `OnDestroy` 收尾；⚠️ 补一条：我们的 `HideAll()`（`:1027`）只收池、不是这一跳')

# ============================ 输出 ============================
out = ['\t'.join(hdr)]
missing = []
for k, upd in U.items():
    if k not in idx:
        missing.append(k)
        continue
    r = list(idx[k])
    # ⚠️ 2026-10-28 更正：原来写成 r[3]（= 判据出处）是 off-by-one，判据要点是 r[4]。
    # 已并入主表的那批由 B93_U1b/B93_U1c 修复；本脚本若重跑会写出正确列。
    r[4] = upd.get('judge', r[4])
    r[5] = upd.get('status', r[5])
    r[6] = upd.get('land', r[6])
    r[7] = upd.get('diff', r[7])
    r[8] = upd.get('level', r[8])
    r[9] = (r[9] + ' · B93_U1') if ('B93_U1' not in r[9]) else r[9]
    r[10] = (r[10] + TAG + upd['note']) if r[10].strip() else (TAG + upd['note'])
    assert len(r) == 11, (k, len(r))
    out.append('\t'.join(r))

if missing:
    raise SystemExit('MISSING KEYS: %r' % (missing,))

data = '\n'.join(out) + '\n'
io.open(OUT, 'wb').write(data.encode('utf-8'))

# 回环校验：重读 → 逐行 11 列 + 键能对回主表 + 无 \r
chk = io.open(OUT, 'rb').read().decode('utf-8')
cl = chk.split('\n')
assert cl[0].split('\t') == hdr, 'header mismatch'
n = 0
for l in cl[1:]:
    if not l.strip():
        continue
    f = l.split('\t')
    assert len(f) == 11, ('cols', len(f), l[:80])
    assert (f[1], f[2]) in idx, ('key', f[1], f[2])
    n += 1
assert '\r' not in chk
print('rows written', n, 'updates', len(U))

