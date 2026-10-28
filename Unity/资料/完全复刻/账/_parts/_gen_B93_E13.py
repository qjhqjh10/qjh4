# -*- coding: utf-8 -*-
"""B93_E13 账补丁生成器（批次 G1「商店族」14 条）· 2026-10-28

纪律（照 CLAUDE.md）：
  · 账 .tsv 一律 **裸 split("\\t")** 读写（⛔ 不用 csv 模块）
  · 首行 **11 列表头**
  · 只写改动过的行；写前读 → 改 → 再读回**逐列比对 11 列**
  · 行尾 = LF（与 `账/_parts/B93_E12.tsv` / `B93_E14.tsv` 一致）
"""
import io
import os

ROOT = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(ROOT, "_parts", "B93_E13.tsv")
HEADER = "域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注"

# key = (主表文件名, 原版类, 方法名前缀) → 被替换的列（索引 5..10：状态/落点/差异/等级/轮次/备注）
OV = {}


def row(f, cls, meth, status, where, diff, level, rnd, note):
    OV[(f, cls, meth)] = {5: status, 6: where, 7: diff, 8: level, 9: rnd, 10: note}


row("商店.tsv", "GachaEventTab", "Reset()", "B",
    "Shell/GachaEventWindow.cs:1309（`ResetEventTab`）· :639（`BuildChest` 里 `_getCardsClick = OnGetCardsClicked`）",
    "**2026-10-28 B93_E13（B12·G1）**：原版两句 —— ① `ActionScheduler.Release(+0x68)`；② `getCardsButton` 的 "
    "`UnityEvent.RemoveAllListeners()`。**② 已落地**：`getCardsButton` 的监听改成走字段 `_getCardsClick`"
    "（= 那个 `UnityEvent` 的 0/1 个监听者），`ResetEventTab()` 把它置 null ⇒「摘监听」真的会发生；"
    "`Build()` 之后 `BuildChest` 把它重新挂上（= 原版 `AddListener`）。**① 只做到形状**：`EventTabScheduledHandle` "
    "恒 null + 出声（本仓全树无 `ActionScheduler`）。⚠️ 原版还有一道 `getCardsButton == null ⇒ return` 的早退，照抄。"
    "今天「整窗重建」本来就把两件都盖住 ⇒ 无可观察差，但形状与断言齐了 ⇒ 维持 B（不升 A：调度器不在）。",
    "P2", "B93",
    "断言 → `Editor/ShopFamilyAssertE13.cs` C8–C10（Build 后监听在 / `ResetEventTab` 后归零 / 再 Build 又回来 = 两态对照）。"
    "宿主建议：`Editor/ShopScene.cs`（本批所有权不含宿主，接线两行写在报告里）。")

row("商店.tsv", "GachaEventTab", "SetEventTab(ILiveOps liveOps)", "B",
    "Shell/GachaEventWindow.cs:1286（`SetEventTab`）",
    "**2026-10-28 B93_E13（B12·G1）**：原版四句 —— ① 存 `liveOps`(+0x58) ② `as GachaEvent`(+0x60) "
    "③ +0x60 为 null ⇒ 早退 ④ `CurrentOffer != null` ⇒ `ActionScheduler.Schedule` 存句柄(+0x68)。"
    "**①②③ 已落地**（实参做成 `EventTabSource{LiveOpsId, IsGachaEvent, CurrentOffer}` 三格显式对位，"
    "判据 `GachaEventTab__SetEventTab.c:20-100`）；**④ 只有形状**（本仓无 `ActionScheduler` ⇒ 句柄恒 null + 出声）。"
    "⚠️ **不冒充**：原版存的是接口引用并做 `is` 判定，我们只有 id + 布尔（`Shell/LiveOpsLocalCatalog.cs` 里没有 Gacha 这一档）。",
    "P2", "B93",
    "断言 → 同文件 C1–C6（判型过 / 不过 两态 + 句柄恒 null）。⛔ 别升 A —— 动作调度器不在。")

row("商店.tsv", "MainMenuOfferContainer", "OnInitialize(LiveOpsEvent playEvent)", "B",
    "**新建** `Shell/MainMenuOfferContainer.cs:201（OnInitialize）` · :68（`MainMenuOfferDrawer` = 原版 "
    "`GeneralOfferPopupDrawerBase`）· :111（`MainMenuCarouselDrawer`，行为全在 `OfferContainer.Carousel.State`）",
    "**2026-10-28 B93_E13（B12·G1）C→B：四拍全部落地**（`判据` = `out_cpp2il_dev_ilspy/MainMenuOfferContainer.cs`"
    "（类型名全在）+ `decomp_full/MainMenuOfferContainer__OnInitialize.c`（三道 null 闸的**顺序**））："
    "① `base.OnInitialize` → **宿主注入口** `BaseInitializer`（主菜单模式卡那一段已在 `Shell/MainMenuRuntime.cs:3993` 起，"
    "⛔ 不写第二份）；② `as ShopOfferEvent` → `OfferEventData`/`OfferBaseData` 显式对位；"
    "③ `GetComponent<GeneralOfferPopupDrawerBase>()` → `GetComponentInChildren<MainMenuOfferDrawer>()`"
    "（本仓是扁平 quad 树，没有「同一 GO 挂一堆组件」那种形态）；④ `drawer.Initialize(GetBaseData())` 落地。"
    "🔴 **一处有意偏离**：`playEvent` 判型不过时原版**抛 NRE**（`.c` 末尾 `FUN_1803f47a0` 不返回）⇒ 我们出声 + 早退。"
    "⚠️ **仍欠：宿主未接线**（没有任何调用方 —— `Shell/MainMenuRuntime.cs` 不在本批所有权内）⇒ 判 B 不判 A。",
    "P1", "B93",
    "断言 → 同文件 H1–H8（无抽屉 ⇒ return / 四拍逐拍 / null 事件两态）。"
    "数据层已在（`Shell/LiveOpsEventFactory.cs:6` 注册 ShopOfferEvent+V2 · `Shell/ShopTabPage.cs:809` V2 开容器）"
    "⇒ 缺的只有「主菜单那件容器」本身，本文件即它。")

row("商店.tsv", "VIPCardShopTab", "VipTimerOnOnRunOutOfTime()", "A",
    "Shell/ShopTabPage.cs:1027（`OnVipTimerRunOutOfTime` = `CheckVIPState` 的等价 `Setup()`）· :1241（`VipExpiryKnown`）"
    "· :1245（`VipTimerRunOutOfTime` 自检读口）",
    "**维持 A（本句闭环）**：原版体只有一句 `CheckVIPState()`，等价 = `Setup()` 重建 VIP 状态行/遮罩；"
    "订阅在 `BuildVipStatus`（`vipTimer.OnRunOutOfTime += …`）、退订在 `OnClose`，逐字对原版 `OnOpen`/`OnClose`。"
    "**残差（单列，不算在 A 里）**：VIP 到期时刻本机取不到（`ShopData.VipExpiresAt` 恒 null，缺 "
    "`DailyStoreData.requiredItem`）⇒ `TimerDisplay` 恒 `--`、这个回调**今天一次都不会被触发**。"
    "⛔ 不许为了「看起来会动」编一个到期时刻（用户口径：不编日期）。",
    "P1", "B93",
    "断言 → 同文件 G1–G3（目标时刻必须 null + 缺数据必须有出声理由）。⚠️ 若有人把 `VipExpiresAt` 编成具体日期 ⇒ G1 立刻红。")

row("商店.tsv", "CampaignPremiumPointsPanel", "CollectButtonClicked()", "B",
    "Shell/CampaignTab.cs:1768（`CollectButtonClicked`）· :1539（`ApplyPremiumState` 改 public 供三态断言）",
    "**2026-10-28 B93_E13（B12·G1）：三档形状全部落地**（判据 `CampaignPremiumPointsPanel__CollectButtonClicked.c`）："
    "state 0 ⇒ 调**订阅方委托**（原版 `+0xa0`，`call [委托+0x18]`）—— 本地无订阅方 ⇒ 出声（可注入位 `OnCollectSubscriber`）；"
    "state 1 ⇒ 原版 `WindowsManager.OpenWindow(prefab 引用 +0x40, boxed int(campaign.<+0x18>+0x1b8))`，"
    "两样都在远端 ⇒ 出声（⛔ 不猜成某扇窗）；其它（含 state 2）⇒ **整段 nothing**（读口 `LastCollectBranch == -1`）。"
    "⚠️ `LocalPremiumState()` 恒 2 ⇒ 今天恒走第三档。⇒ 维持 B（形到、远端输入不在）。",
    "P2", "B93",
    "断言 → 同文件 E1–E7（state 2 = 无动作 / state 0 / state 1 —— 三档必须**分得开**）。"
    "原来的内联 `Debug.Log(\"…本轮不实现\")` 已删（那等于「假装没有这一支」）。")

row("商店.tsv", "CarouselOfferPopupDrawer", "DoReset()", "B",
    "Shell/OfferContainer.cs:1155（`Carousel.State.DoReset`）",
    "**2026-10-28 B93_E13（B12·G1）C→B：缺的整段生命周期已补**（`currentItemDrawers` + `scroll.AutoScroll` + "
    "`ManualInit` 三格状态）。**「循环内 ReleaseAssets 的顺序存疑」已闭合**：ILSpy `CarouselOfferPopupDrawer.cs:320-345` "
    "实读 = **先 `ReleaseAssets()`、再 `Destroy(component.gameObject)`**，两句都在循环体内。"
    "🔴 **`ReleaseAssets()` 本身是原版的 TODO 空实现**（`GeneralOfferPopupDrawerBase.ReleaseAssets` 的体只有 "
    "`foreach (…) CustomDebug.LogWarning(\"[TODO] Implement itemdrawer release\")`）⇒ **照抄：出声、不释放**，"
    "且它在循环里被调 N 次（O(N²) 条告警，原版就是这个形状）。清表 + 逐条销毁由 `destroy` 回调落地。"
    "⚠️ 仍无 UI 宿主（本店每份报价只有 1 条奖励 ⇒ 轮播路径不可达）⇒ B。",
    "P2", "B93",
    "断言 → 同文件 B10–B12（逐条销毁 / 清表 / **释放是 TODO 空实现**这条 —— 把它真实现出来 B12 就红）。")

row("商店.tsv", "CarouselOfferPopupDrawer", "DrawRewards(IEnumerable<RewardInfo> rewards)", "B",
    "Shell/OfferContainer.cs:1076（`Carousel.State.DrawRewards`）· :998（`IsCurrency` = 谓词②）",
    "**2026-10-28 B93_E13（B12·G1）C→B，且判据从「否」升到「部分闭合」**（铁律 5 就地订正 —— 原记「两个谓词语义都查不到」）："
    "**谓词② 已解 = `x.Item is Currency`**（`out_cpp2il_dev_ilspy/CarouselOfferPopupDrawer.cs` 的 `isinst` 形状与 "
    "`MainMenuOfferContainer.OnInitialize` 里 `playEvent as ShopOfferEvent` **逐字节同形**，且带 `typeof(Currency)`）；"
    "**谓词① 仍未解**（`ObtainableItem` 上的虚 `bool`，槽 `+0x1C8`，两份反编译都没方法名）⇒ 走可注入 `Carousel.Filter`，"
    "`null` = 不过滤 + 出声。三件确定的全部落地：**先第二组（货币）后第一组**；`SpliceWhere` **摘走**命中项 ⇒ 两组不重复"
    "（判据 `ListExtensions.SpliceWhere` ILSpy `:2362-2420`）；**两组条数之和 < 4**（ctor 实读 "
    "`thresholdForEnableAutoScroll = 4`）⇒ `AutoScroll = false`。",
    "P2", "B93",
    "断言 → 同文件 A1–A4（判据字面量）+ B1–B9 · B13–B15（次序 / 去重 / 阈值两态 / 谓词① 可注入 / `typeOf == null` 不默认 true）。"
    "⛔ 谓词① 别猜。")

row("商店.tsv", "CarouselOfferPopupDrawer", "Initialize(ShopOfferBase shopOfferData)", "B",
    "Shell/OfferContainer.cs:1046（`Carousel.State.Initialize`）· :1067（`ManualInit`）",
    "**2026-10-28 B93_E13（B12·G1）C→B：两句都落地**。① `base.Initialize(offer)` = "
    "`DrawRewards(offer is ShopOfferDataV2 ? Items.Concat(v2 那一串) : Items)`（判据 `GeneralOfferPopupDrawerBase.cs:17-50`；"
    "`v2Extra` 非 null 即代表「是 V2」）；② `scroll.ManualInit()` = `ManualInit(visible)`。"
    "⚠️ `UI_InfiniteScroll` 是第三方（本地**有 prefab 无源码**）⇒ 只留它保证的那一件事：「初始化完了 + 一屏 N 槽」，"
    "槽数由调用方给（那个数原版是从布局量的，我们没有那份源码）。",
    "P2", "B93",
    "断言 → 同文件 B1–B2。")

row("商店.tsv", "ExpansionPassPurchasePremiumPanel", "PurchasePremiumClick()", "B",
    "Shell/ExpansionPassEventWindow.cs:2685（`PurchasePremiumClick`）· :2700（`PurchasePremiumClicks` 读口）"
    "· 绑定在 `BuildNotPremiumBranch` 的 `MenuDraw.Hit(btn, \"Hit\", NpButtonR, QHit, () => PurchasePremiumClick(), …)`",
    "**2026-10-28 B93_E13（B12·G1）**：整段从内联 lambda 搬成方法（六句形状齐）。**本次补的那一条** = 原版第 ⑤ 句那条 "
    "`CustomDebug.LogWarning` **带事件 id**：现在我们打的告警里有 `_event.Id`，没有事件数据时明写 `<没有事件数据>`，"
    "⛔ 不编 id。**仍欠**：第 ③ 句 `LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)` 的窗口引用 + "
    "开出来之后那两条回调（`b__12_0` = 重跑 `Initialize`）的宿主 —— 都**在远端 premium 报价资产**里"
    "（`ExpansionPassData.PremiumOfferData` 本地没有）。⇒ 维持 B。",
    "P2", "B93",
    "断言 → 同文件 F1–F4（`false` ≠ 空函数：`PurchasePremiumClicks` 必须 +1）。")

row("商店.tsv", "ExpansionPassPurchasePremiumPanel", "Start()", "A",
    "Shell/ExpansionPassEventWindow.cs:2438（`StartPurchasePremiumPanel` 等价记账）· 真实绑定落在 "
    "`BuildNotPremiumBranch` 的 `MenuDraw.Hit(… () => PurchasePremiumClick() …)`",
    "**2026-10-28 B93_E13（B12·G1）B→A：本句闭环**。原版 `Start()` 只有一句 "
    "`purchasePremiumButton.OnClick.AddListener(PurchasePremiumClick)`（`UnityEvent` 在按钮 `+0x100`）；"
    "我们的等价 = 建树时 `MenuDraw.Hit` 挂**同一个** `PurchasePremiumClick`（⛔ 不第二次 `AddListener` —— "
    "两处写同一条规则迟早不一致）。**残差单列**（不算在 A 里）：`Initialize` 那两个输入（远端事件 + 关闭回调）"
    "仍在远端，见上一行。",
    "P2", "B93",
    "断言 → 同文件 F3（经 `PurchasePremiumClick()` 读口：点一次 ⇒ 计数 +1，端到端证明绑定通）。")

row("商店.tsv", "FixedDateDailyStoreItem", "OnAfterWrite(JToken rawJson)", "D",
    "无（不复刻）",
    "**D 保持；「施工前复核」这一条本次已做完**（原记：『施工前复核该 JToken 是否会进存档载荷』）："
    "① 体只把 `item.Reference` 的 `rarity` 与 `Reference.GetType()` 的类型名写进**这份配置自己的 JSON**"
    "（`rawJson[\"rarity\"]` / `rawJson[<类型名>]`）—— `FixedDateDailyStoreItem` 是 "
    "`Everguild.LiveOps.DailyStoreData.fixedDateItems` 的元素，而 `DailyStoreData : ShopStoreData` = **store 配置**，"
    "不是玩家存档 DTO；② 0 处后端调用；③ 玩家侧不可见。"
    "⚠️ **「0 调用点」≠ 死代码**：`IAfterWrite` 的实现方共 4 个（`GenericArmyItem` / `MissionChallenge` / `ScoringPoint` / 本条），"
    "调用方是**反射分派**，本机反编译树里按名字搜 0 命中 ⇒ 那是「反射入口」不是「没人用」（与 CLAUDE.md「搜不到赋值点 ≠ 没有来源」同族）。",
    "P2", "B93",
    "铁律 11 例外①（原版无可观察行为）。判据 → `FixedDateDailyStoreItem.cs:12-40`（`out_cpp2il_dev_ilspy`）+ "
    "`Everguild.LiveOps/DailyStoreData.cs:89`（`fixedDateItems`）。⛔ 别再重复查这一条。")

row("商店.tsv", "GachaCratePanel", "Awake()", "B",
    "Shell/GachaEventWindow.cs:1352（`BuildCrateVideo`）· ② 仍在 `BuildPage`（`Video Background` 出厂关着）"
    "· ⑤ 仍在 `BuildPriceButton`",
    "**2026-10-28 B93_E13（B12·G1）：①③④ 本次落地** —— `Video Player` 节点上**真挂 `VideoPlayer`**，"
    "`enabled = false`（①）· `targetCamera = Camera.main`（④）· `started += OnCrateVideoStarted`（③）。"
    "两道**资产缺口当场出声**：`clip` 未导入 `Resources/`（源 = `bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4`；"
    "我们挑的资源名 = `Art/videos/gacha_crate_opening`）；`crateOpenSound` 的 **cue 名解不出来**"
    "（prefab 的 `m_FileID 6` 外部引用、pathID `-8931646904024042903`，那份 CAB 不在本机 84 个 bundle 里）"
    "⇒ 回调照挂、里面只出声不播（⛔ 不拿 `WindowSounds` 里别的 cue 顶替）。",
    "P2", "B93",
    "断言 → 同文件 D11–D13（节点在 / **真挂了 `VideoPlayer`** / 出厂 `enabled == false`）。")

row("商店.tsv", "GachaCratePanel", "OpenCrateButtonClick()", "B",
    "Shell/GachaEventWindow.cs:1398（`OpenCrateButtonClick`）· :1425（`TryPurchaseCrate`）· :1436（`RewardWindowClosed`）"
    "· :1452/:1462（`CrateFlowBegin`/`CrateFlowStep` = `CrateAnimationFinished` 三档）",
    "**2026-10-28 B93_E13（B12·G1）：六句全落地**。①`videoBackground.SetActive(true)` ②`videoPlayer.enabled = true` "
    "③`Play()` ④`EventSystemController.Toggle(false)` → **`PointerLayer.SetInputEnabled(false)`**（本仓没有 UGUI "
    "`EventSystem`/`EventSystemController`；判据 = `Editor/ShellScene.cs` A77㉑① 那条订正；⚠️ `cpp2il` 的 `.c` 把这一句"
    "打成 `ChatPreviewMessage__Enable` = **类名错位**，ILSpy 那份写的是 `EventSystemController.Toggle`）"
    "⑤`crateRewards = null; crateOpenned = false; CurrentOffer.TryPurchaseOffer(…)` ⑥`StartCoroutine(CrateAnimationFinished())` "
    "抽成**可步进的显式状态机**（批处理没有帧循环 ⇒ 自检能逐步驱动）。"
    "🔴 **有意偏离**：⑤ 的服务器回包本地**永远不到** ⇒ 原版会**无限等待**（黑屏 + 输入关着）；"
    "我们**出声 + 收摊**（`RewardWindowClosed()` + **把输入还回去**），⛔ 不假装开箱、也不留一个永挂的协程。",
    "P1", "B93",
    "断言 → 同文件 D1–D9（含 ★★「收摊必须把输入还回去」—— 删掉那句 `SetInputEnabled(true)` 就红；"
    "以及「没有回包 ⇒ 停在 state 1」这个原版行为）。")

row("战役.tsv", "CampaignArmyUIDecorator", "OnObjectUpdated(ArmyItemContainer armyContainer)", "A",
    "Shell/CampaignTab.cs:875（`OnArmyAmountChanged`）· :902（`PlayArmyIconShine`）"
    "· :928（`AttachArmyShine` 建扫光层）",
    "**2026-10-28 B93_E13（B12·G1）现读复核 ⇒ A，残差归零（账要订正）**：① 「数量变了才动」已落地"
    "（`amount == _armyAmountShown ⇒ return` = 原版 `currentAmount != amount` 那道闸）；"
    "② `shinyEffect.Play(resetTime: true)` **已于 2026-10-27 由 `B9_W16SHINE` 落地** = `WildcardShineLayer.Play(true)`"
    "（0→1 线性、`m_Duration 1.0`；shader `Resources/Shaders/WildcardShine.shader` 是 Coffee `TRANSITION_SHINY` 的移植；"
    "transition 贴图按 `1-saturate(uv.y)` 等价，**不需要解那张图**）；③ `CheckFillColor()` = 重建滑条（逐格比例 + 满档换绿）。"
    "⚠️ 账里原来那句「缺 Coffee `UIEffect` 着色器 + transition 贴图 ⇒ 已裁 P2」**已过期**（铁律 5 就地订正）。"
    "只播**当前选中阵营**那一格（原版逐格挂，只有变过的格子播，⛔ 别全播）。",
    "P0", "B93",
    "断言 → 同文件 I1–I9（`Params.Icon` 的 7 个原版字段逐格 + 与 `WildcardDisplay` 的 0.75 s 做两态对照，"
    "证明 1.0 s 有区分度）。⛔ 别再按旧账去「补流光」。")

def main():
    out_rows = []
    for fname in ("商店.tsv", "战役.tsv"):
        path = os.path.join(ROOT, fname)
        b = io.open(path, "rb").read()
        text = b.decode("utf-8-sig")
        assert b.count(b"\r\n") == 0, fname + " 不是 LF（裸 CR 也要先查）: crlf=" + str(b.count(b"\r\n"))
        assert b.count(b"\r") == 0, fname + " 有裸 CR"
        lines = text.split("\n")
        header = lines[0].split("\t")
        assert len(header) == 11, (fname, "header cols", len(header))
        for i in range(1, len(lines)):
            if not lines[i]:
                continue
            cols = lines[i].split("\t")
            if len(cols) < 11:
                continue
            for key, repl in OV.items():
                kf, kcls, kmeth = key
                if kf != fname or cols[1] != kcls or not cols[2].startswith(kmeth):
                    continue
                for idx, val in repl.items():
                    assert "\t" not in val, (key, idx, "含制表符")
                    assert "\n" not in val and "\r" not in val, (key, idx, "含换行")
                    cols[idx] = val
                out_rows.append(cols)
    assert len(out_rows) == 14, "期望 14 行，实得 " + str(len(out_rows))

    body = "\n".join([HEADER] + ["\t".join(r) for r in out_rows]) + "\n"
    io.open(OUT, "wb").write(body.encode("utf-8"))

    # ---- 回环校验：重新按裸 split 读回来，逐列比 11 列 ----
    rb = io.open(OUT, "rb").read()
    rtext = rb.decode("utf-8-sig")
    rlines = rtext.split("\n")
    assert len(rlines[0].split("\t")) == 11, "回读表头不是 11 列"
    assert rlines[0] == HEADER, "回读表头不一致"
    n = 0
    for ln in rlines[1:]:
        if not ln:
            continue
        c = ln.split("\t")
        assert len(c) == 11, "回读列数 " + str(len(c)) + " ≠ 11: " + c[1] + "." + c[2]
        n += 1
    assert n == 14, "回读行数 " + str(n)
    print("OK 写出", OUT)
    print("行数", n, "· 表头 11 列 · LF · 回读逐列校验通过")


if __name__ == "__main__":
    main()
