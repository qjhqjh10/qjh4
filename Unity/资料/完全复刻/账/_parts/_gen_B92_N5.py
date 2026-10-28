# -*- coding: utf-8 -*-
"""B92 波 4 · N5 账补丁生成器（第九十二会话 2026-10-28）
用法：python -I -X utf8 _gen_B92_N5.py            # 只校验，不写盘
      python -I -X utf8 _gen_B92_N5.py --write    # 真写 账/_parts/B92_N5.tsv
纪律：裸 split("\t")；UTF-8-BOM + CRLF；方法列原样照抄主表（已是完整签名）。
"""
import io, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
LEDGER = os.path.join(ROOT, '账')
SHEET = os.path.join(ROOT, '判据', 'B92_波4_任务单_N5.tsv')
OUT = os.path.join(ROOT, '账', '_parts', 'B92_N5.tsv')

DOMAIN_FILE = {
    '日常/奖励': '日常_奖励.tsv',
    '未分类/其他': '未分类_其他.tsv',
    '收藏': '收藏.tsv',
    '其他/基础设施': '其他_基础设施.tsv',
    '菜单/外壳': '菜单_外壳.tsv',
}


def rows(path):
    b = io.open(path, 'rb').read()
    t = b.decode('utf-8-sig')
    return [l.split('\t') for l in t.replace('\r\n', '\n').split('\n') if l.strip()]


TASK = rows(SHEET)
HEAD = TASK[0]
assert len(HEAD) == 11, HEAD
CACHE = {k: rows(os.path.join(LEDGER, v)) for k, v in DOMAIN_FILE.items()}

# ============================ 每行的改动(按任务单数据行序号 1..60) ============================
# 值 = dict(status=?, landing=?, note=?)；note 会以 "｜🆕 …" 追加到原差异后面
D_BOOST = (
    "🔴 2026-10-28 B92_N5 判 **D**（§18·4 (b)+(d)，证据三条）："
    "① **宿主 `BoostMenu` 在本 build 的本地资源快照（`assets_full`，2025-07-30 那份）0 实例** —— "
    "全库只有类桩 `globalgamemanagers/MonoScript/MonoScript_2818.json` 一条；本代理复跑 "
    "`grep -l BoostMenu */GameObject/*.json` 与 `*/PrefabFile/*.json` **均 0 命中**，"
    "`bundle_menus_assets_all` 里也没有任何 prefab 的 `m_Script` 指向它 ⇒ 面板**没有宿主、永远建不出来**；"
    "② 面板数据来自 PlayFab 加成清单 ⇒ `BoostStore.Active` **恒空**（见本文件头 🔶 登记）；"
    "③ 本仓**零调用方**：`grep -rn BoostTimerPanel --include=*.cs` 除 `Shell/BoostTimerPanel.cs` 自身外 0 命中。"
    "⇒ 原版那条路在本 build **永不可达**（不是「我们没做」——实现体是逐句照抄的）。"
    "**可逆备注（发现生产点即回 C）**：若日后在**远端 Addressables** 或 `D:/2/关服前存档/安卓版本warpforge…`（安卓 1.35.0）"
    "里找到 `BoostMenu` prefab —— 本代理**未挖**那一份，属未覆盖面 —— 或本地出现加成数据源，"
    "**立即回 C** 并补宿主几何（宿主那层 `PanelSeparation = 30` 仍未建）。"
    "⚠️ **文件不删**（删文件要用户点头）；实现体 `BoostTimerPanel__Init.c` / `__SetDescription.c` / `__SetTimer.c` 都在，随时可接。"
)

MODS = {
    # ---------- 日常_奖励（1..13）----------
    1: dict(status='D', note=D_BOOST),
    2: dict(status='D', note=D_BOOST),
    3: dict(status='D', note=D_BOOST),
    4: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：落点 `Shell/DuelPopupWindow.cs:109` 只是**一扇窗里的一个节点**，"
        "不是这一跳的等价实现。原版方法体**在**（`decomp_full/ChallengeManager__AcceptPracticeChallenge.c`，245 行，本代理现读），"
        "同族 `SendPracticeInvitation` / `CancelPracticeInvitation` / `AcceptedFriendDuelNotification` 也都在 ⇒ "
        "它依赖的是 **PlayFab 私聊/邀约**那一套（邀约消息 + 挂起队列 + 接受后开局）。"
        "本仓 `CardPresentation/Net/` 只有**对局同步**（大厅匹配/换牌/重连），没有邀约消息类型。"
        "**还差什么**：① 新增邀约消息（P2P 层，`Net/`）+ 挂起队列；② 接受 → 建局的落点（可复用现有 `Battle!` 链）。"
        "两者都跨出本代理白名单 ⇒ 保持 C。")),
    5: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版体在（`decomp_full/DailyRewardTrackPanel__HandlePurchase.c`）。"
        "本侧真正的卡点**不在本行落点**：`_premiumOwned` 是 `Shell/DailyData.cs:760` 的 "
        "`static readonly bool _premiumOwned = false` ⇒ `DailyData.RewardStateOf(day, premium):792` 恒走 "
        "`PremiumLocked`，本文件（`Shell/DailyRewardPopup.cs`）**改不动它**。"
        "**还差什么**：① `DailyData._premiumOwned` 改成可写 + 加一个「本地模拟买」入口（**`Shell/DailyData.cs` 属 N2 白名单**）；"
        "② 价钱钮回调替换 —— 现挂在 `Shell/DailyRewardPopup.cs:589-599`（只 `Debug.Log` 一条，本代理白名单内，"
        "等 ① 到位后一行接上）；③ 续作（把第一条 `canCollect = true`）同批做。")),
    6: dict(note=(
        "🆕 2026-10-28 B92_N5：与上一行**同一批**（续作由购买链触发）。卡点同上一行："
        "`Shell/DailyData.cs:760` 的 `_premiumOwned` 只读（N2 白名单）⇒ 本代理无法落地。")),
    7: dict(note=(
        "🆕 2026-10-28 B92-N5 **判据已补齐**（本代理从 prefab 里挖出来了，原来记「本仓无该表解包产物」**不再成立**）："
        "`bundle_menus_assets_all/MonoBehaviour/` 里 **5 份** `OnQuestionAnswered` 实例，字段 "
        "`questionItem` / `objectToEnable` / `enableIfAnswered` / `questionFilter`（类桩 `Assembly-CSharp/OnQuestionAnswered.cs`）：\n"
        "  ① `MonoBehaviour_-4634440272722734642` questionItem=`-3360730068502130226` filter=`Other` enable=1 target=`1234327651705868750`\n"
        "  ② `MonoBehaviour_3452005510164369870` questionItem=`6479126351063536078` filter=`Yes` enable=1 target=`2528185972930433486`\n"
        "  ③ `MonoBehaviour_5543340576972919246` questionItem=`6479126351063536078` filter=`Yes` enable=1 target=`-3016622015888633394`\n"
        "  ④ `MonoBehaviour_6452858811180306894` questionItem=`6479126351063536078` filter=`Yes` enable=1 target=`-4389685308510121522`\n"
        "  ⑤ `MonoBehaviour_7419885581804622286` questionItem=`6858069252430483918` filter=`Other` enable=1 target=`-3816342073150811698`\n"
        "体（`decomp_full/OnQuestionAnswered__OnAnswered.c` 逐句）："
        "`objectToEnable.SetActive(item.Answered == enableIfAnswered && (filter 为空 || item.<答案串 +0x30>.Contains(filter)))`；"
        "Start 那份同一式子 + 订阅 `questionItem+0x38` 的 Action，OnDestroy 退订。"
        "**还差什么**：① 把 5 个 `objectToEnable` 的 pathID 解成我们窗里的节点（解星法："
        "`grep -rl <goPid> RectTransform/ Transform/` → 取 `m_Father` 链；本代理只解出 ①=RectTransform_5662686936166224334、②=RectTransform_2264494712085991886）；"
        "② 我们的 `FeedbackQuestionItem` 要有「答案原文」字段（原版 +0x30）—— 现 `Shell/GiveFeedbackPopup.cs` 只有选择态。")),
    8: dict(note=(
        "🆕 2026-10-28 B92_N5：与上一行同一份依赖表（已挖出，见上一行）。Start 那一跳 = **同一式子 + 订阅 + 初始显隐**；"
        "体在 `decomp_full/OnQuestionAnswered__Start.c`（订阅 = `Delegate.Combine` 到 `questionItem+0x38`）。"
        "**还差什么**：同上一行的 ① ②。")),
    9: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：生产者已接（见原差异 B91_W3），剩下的**只有两件事、且都跨白名单**："
        "① `GameModesHandler.DraftModeUnlocked` 默认 `true` → 应翻 `false` + 落盘持久化"
        "（原版住在 `PlayerDataManager.demoData.canEnterDraftMode`；`Shell/GameModesHandler.cs:462` 本代理可改，"
        "但**翻默认值会改轮抽入口闸**，必须先跑一次 `ShellScene`/`DeckScene` 自检确认无断言依赖「默认解锁」——本代理**不跑 Unity**，故未改）；"
        "② 原版第二句 `UploadOnlineDemoData()` = **P3**（无服务器）。⇒ 保持 B。")),
    10: dict(status='A', note=(
        "🆕 2026-10-28 B92_N5 升 **A**（依据 §3·1 ①：零生产调用点、上游在 B/C）："
        "原版体 `decomp_full/RewardService__CollectCurrencyReward.c` 全文现读 = "
        "`currency is Energy && SharedEventDataManager.GetContext<EnergyContext>() != null ⇒ EnergyContext.GrantEnergy(q)`，"
        "否则 `InventoryManager.Instance.Add(currency, q)`；本侧 `Shell/Inventory.cs:1960` 逐句同形（含「energy 但无 EnergyContext ⇒ 落 Add 兜底」那一支）。"
        "**永久现状**：生产发奖仍走 `Wallet.Grant`（图名记账）⇒ 本方法**零生产调用点**，上游 = reward 路径改造（账 654-668 族，仍 B/C）。"
        "无行为缺陷。")),
    11: dict(note=(
        "🆕 2026-10-28 B92_N5 现核（`decomp_full/RewardService__CollectItemReward.c` + `__CollectItemReward_object_.c` 全文）："
        "原版 = ① `CollectItemReward<T>(item, instanceId, quantity, InventoryManager)` ⇒ `InventoryManager.Add<T>(item, quantity, instanceId)`；"
        "② 再判「ephemeral 有效」（`reward+0x28` 非空 且 (`+0x20 > 0` 或 `+0x10` 的 LiveopsReference 解得出事件)）⇒ `InventoryManager.AssignEphemeralState(item, state)`。"
        "**还差什么**：本仓 `InventoryManager.Add` 有（`Shell/Inventory.cs:1151`），但 ② 那一跳要 `EphemeralState` + `LiveopsReference` 两层（都没有），"
        "且 `RewardInfo.IsEphemeral/EphemeralMs` 在 89 条战役奖励里**全是 0/null** ⇒ ② 今天恒不触发。"
        "⇒ 可只落 ①（约 10 行，落 `Shell/Inventory.cs` 的 `RewardService` 类尾），但要与 `CollectSingleReward` 的分派同批做才有人用。")),
    12: dict(note=(
        "🆕 2026-10-28 B92_N5 现核（`decomp_full/RewardService__CollectSingleReward.c` **189 行**全文扫过）："
        "它不是「按 Item 类型分派」那么薄 —— 体里依次有 "
        "① ephemeral 过期闸（`EphemeralState.EphemeralEndTime <= TimeHelper.ServerDateTime` ⇒ 直接 return）；"
        "② `NetworkingPeer.OnMessage(...)`（**联网上报**）；"
        "③ `Item as Currency` 判型（接口/slot 比较那套 `+0x130` 逻辑）；"
        "④ 非货币支还要走 `AlliancesManager.Instance.trophyController` 的 `FirstOrDefault(trophy.Id == …)` 把 `+0x38` 置 1（**联盟奖杯**）；"
        "⑤ 最后才 `CollectItemReward` / `CollectCurrencyReward`。"
        "⇒ 原差异里「按 `reward.Item` 类型分派」这句**只覆盖了 ③④⑤ 的骨架**，①②④ 需要 `NetworkingPeer` / `EphemeralState` / `AlliancesManager` 三层。"
        "**还差什么**：这三层本仓都没有（联盟 = P2 本地模拟、联网上报 = P3）⇒ 若要落，只能落「⑤ 分派 + ③ 判型」那一截并打 🔶。")),
    13: dict(status='A', landing='Shell/TimerDisplay.cs（LocalizationChange）', note=(
        "🆕 2026-10-28 B92_N5 **已实现升 A**：`TimerDisplay.LocalizationChange()` 逐句照 "
        "`decomp_full/TimerDisplay__LocalizationChange.c`（`if (!IsNullOrEmpty(_timerDescription)) SetTimerDescriptionText(LocalizationManager.GetTranslation(_timerDescription), false);`）"
        "⇒ 我们 `if (IsNullOrEmpty(_prefixKey)) return; _prefix = Loc.T(_prefixKey); Refresh();`。"
        "订阅口 = `Loc.LanguageChanged`（`Core/Loc.cs` 类尾，`SetLanguage` **真改了值**才发）；"
        "`OnEnable` 订阅 / `OnDisable` 注销（原版是 Awake 订阅、OnDestroy 退订 —— 我们组件随 Label 一起销毁，用 OnDisable 更保险）。"
        "**已接一个真实消费者**：`Shell/DailyRewardPopup.cs:708` 现在把键一并交下去（`prefixKey:`）。"
        "⚠️ **同时使 `账/日常_奖励.tsv` 两行过期**（`TimerDisplay.Awake` 的「不订阅全局本地化事件」、`TimerDisplay.OnDestroy` 的「无订阅可摘」）—— 那两行不在本批任务单，已写进报告 §6 交调度台。")),

    # ---------- 未分类_其他（14..44）----------
    14: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版体 = `MemberwiseClone()` + 深拷 `targetCriteria(+0x18)`。"
        "**我们的侧证是假的**（本行备注「我们 AiScoringData 的 Criteria 也走字段级复制」不成立）："
        "全文件 `grep -n 'DeepCopy\\|Clone\\|Copy()' RuleEngine/Data/AiScoringData.cs` = **0 命中** ⇒ 那些 DTO 是**一次加载后只读共享**。"
        "**还差什么（两条路，本代理不自行判 D）**：① 建一条「不可变审计」证明全链路无写点（`CardDef` / `AiScoringData.*Dto` 的写口普查），"
        "拿到证明后可判 §18·4 (d)；② 或在原版侧找到「克隆后就地改」的具体消费点，确认我们没有对应行为。")),
    15: dict(note=(
        "🆕 2026-10-28 B92_N5 现核（`decomp_full/ContentSizeFitterMinMax__ClampSize.c` 38 行 + `__SetLayoutHorizontal.c`）："
        "原版 = `ContentSizeFitter` 派生（`SetLayoutHorizontal` 先调**基类**再 `ClampSize()`）；"
        "`ClampSize` = 读 `rectTransform.sizeDelta`，两个分量各在 `useX/useY`(`+0x38/+0x44`) 为真时按 `min(+0x3c/+0x48)`/`max(+0x40/+0x4c)` 夹一次，再 `set_sizeDelta`。"
        "**组件本体只有 ~30 行**，不是「机制没法等价」，而是**我们没建这个组件**。"
        "⛔ **不能判 D**：uGUI 在本仓**确存在**（`CardPresentation/Battle/*` 用 Canvas/RectTransform/Image）⇒ §18·4 (d)「子系统根本没有」不成立。"
        "**还差什么**：需要**一个真实消费者**（哪个重建的窗口要「随内容伸缩但夹上下限」）才建；建了接上即 A。")),
    16: dict(note="🆕 2026-10-28 B92_N5：同 `ClampSize`（原版 = 基类调用 + 同一个 ClampSize，`__SetLayoutHorizontal.c` 全文两句）。组件未建的结论同一处。"),
    17: dict(note="🆕 2026-10-28 B92_N5：同 `ClampSize`（原版 `__SetLayoutVertical.c` = 基类调用 + 同一 ClampSize）。组件未建的结论同一处。"),
    18: dict(note=(
        "🆕 2026-10-28 B92_N5：判据在（`Countdown.cs:23` + Update）。**还差什么**：先按 `m_Script` pathID 反查「本地到底有几个实例」"
        "（原备注写「实例面仍未证」）—— 有实例才建组件（≈50 行），零实例则按 §18·4 (b) 判 D。本代理未做这一步（不在本批预算内，且 §19 要求查完到用掉 ≤2 天）。")),
    19: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：`bundle_armycursors_assets_all/Sprite/` 确有 21 个 `Cursor_*` json；"
        "本仓 `CardPresentation/Resources/Art/ui_menu/` 只有 `Cursor_Space_Marine_01` / `_01Glow` 一对。"
        "**还差什么（本代理做不到，⛔ 不是判据问题）**：补 20 张图的 Unity import —— 必须 `工具/import_original_art.py` + 跑一次 Unity 导入，"
        "两件都不在本代理权限内（⛔ 不跑 Unity、⛔ 不手写 .meta）。回来条件不变。")),
    20: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：`decomp_full/EmptyGraphic__OnPopulateMesh.c` **12 行**（清 `vh`），"
        "原版靠它做一个「吃射线不画像素」的 Graphic。我们命中区走 `MenuDraw.Hit`（世界空间 quad）⇒ 没有 uGUI Graphic 这一层。"
        "**还差什么**：只有在「重建的 uGUI 面板里需要一个纯命中件」时才建（~10 行 `MaskableGraphic` 子类）；今天零消费者。")),
    21: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版是「id → 组件」注册表（`registry.TryGetValue`）。我们靠直接引用 / `transform.Find` 取节点，"
        "**没有 id 注册表**。**还差什么**：需要一个真实消费者（哪个原版 prefab 用 `m_MethodName` 按 id 找件）；否则判 A 无落点。")),
    22: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版 `GenericArmyItem.GetID()` 体 = `return item.id`（数据持有者）。"
        "本行 `.c`（`GenericArmyItem__GetID.c` 那一族）**内容错位**（首行是别的类、只有一句 return）⇒ 判据只剩类桩。"
        "**还差什么**：我们的等价 = `Shell/ItemDrawer.cs:427` 的**类型映射**（按阵营建抽屉），不是「条目对象取 id」⇒ 要 A 得先有一个「阵营条目对象」实体，今天没有。")),
    23: dict(note="🆕 2026-10-28 B92_N5：同 `GetID`（无参 `GetItem()` = `LiveOpsManager.GetHandler<CampaignHandler>()` 命中取 `SelectedArmy`、否则 `Neutral` —— 这段在 `cpp2il out_cpp2il_dev_ilspy` 是可读的，`.c` 那份错位）。缺的仍是「阵营条目对象」。"),
    24: dict(note="🆕 2026-10-28 B92_N5：同 `GetID`（`entries.TryGetValue(army)` 否则 `values.FirstOrDefault(x.army == army)`）。缺的仍是「阵营条目对象」。"),
    25: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：`HealCriteria.DeepCopy` 与 `BoardCriteria.DeepCopy` 同族（克隆评分条件）。"
        "落点 `RuleEngine/Data/AiScoringData.cs:90`（`AiHealCriteriaDto` 真声明行）**只读共享、无 Clone**（同族行已验证：全文件 `Clone/Copy()` 0 命中）。"
        "**还差什么**：同 `BoardCriteria.DeepCopy` 的两条路（不可变审计 / 原版消费点）。")),
    26: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：5 个实例全在战场侧（原备注已核）。**归战斗线**，本域只留判据。"
        "本代理**未动**（`CardPresentation/Battle/**` 与 `Editor/**` 都在白名单外）。")),
    27: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版 `MessageList.Refresh()` = 用当前数据重铺整列表。"
        "我们 `Shell/InboxWindow.cs` 的 `Initialize(messages)`（`:872`）铺一次；`_messages` 是 `:281` 的 List，"
        "**重铺逻辑已存在**（`:716-745` 那段），只是没有单独的 `Refresh()` 入口。"
        "**还差什么**：一个「消息集合变了要重铺」的触发点（原版由 `MessageList` 自己持有数据并在事件里 Refresh）。"
        "⇒ 若要 A：把 `:716-745` 抽成 `Refresh()` 并把 `Initialize` 改成「存数据 + 调 Refresh」，再接一个真实触发点（新消息到达/已读态变化）。")),
    28: dict(note="🆕 2026-10-28 B92_N5 现核：`decomp_full/RectExtensions__GetWorldRect.c` 31 行 = `GetCorners` 四角 → 世界矩形。我们在同一 px 设计空间（`Core/UguiRect.cs` 的 `PxRect`），**没有世界坐标那一跳**。还差一整类 uGUI 世界换算的消费者才会建。"),
    29: dict(note="🆕 2026-10-28 B92_N5 现核：`decomp_full/RectSizeLimiter__SetLayoutHorizontal.c` 35 行 = 基类调用 + 按 `m_maxSize/m_minSize` `SetSizeWithCurrentAnchors`。与 `ContentSizeFitterMinMax` 同族（uGUI `ILayoutController`），缺的同样是「组件 + 一个真实消费者」。"),
    30: dict(note="🆕 2026-10-28 B92_N5：同 `SetLayoutHorizontal`（纵向那一半；同一组件的第二个回调）。"),
    31: dict(note=(
        "🆕 2026-10-28 B92-N5 就地补正一条**假缺口**：原备注「与 `ContainerService.OpenOfferContainer` 同一条卡点」里那条也确实没做，"
        "但**本行真正的主因是「活动容器框架整条未建」**（`UiContainer`/`ContainerHolder`/`ContainerBuilder` 本仓只有接口），"
        "与另三行（OpenPopup/Refresh/TryPurchase）**同一个根因** ⇒ 四行同进退。⛔ 不补框架先去挖 PlayFab 数据 = 白花一趟。")),
    32: dict(note="🆕 2026-10-28 B92_N5：同 `OnClick`（四行同一根因 = 活动容器框架未建）。"),
    33: dict(note="🆕 2026-10-28 B92_N5：同 `OnClick`（四行同一根因 = 活动容器框架未建）。"),
    34: dict(note="🆕 2026-10-28 B92_N5：同 `OnClick`（四行同一根因 = 活动容器框架未建）。"),
    35: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：`decomp_full/UICornersGradient__ModifyMesh.c` **131 行**（逐顶点按四角色插值）。"
        "原版是 uGUI `BaseMeshEffect` 回调；我们的角渐变在**生成贴图/顶点时**烘进 `Core/CardArt.cs`（`GradientKeys` 一族）⇒ 视觉等价、无运行时回调。"
        "**还差什么**：若某处需要一个「运行时改四角色」的 uGUI 件（例如动态换色底图）才建；今天零消费者。")),
    36: dict(note="🆕 2026-10-28 B92_N5 现核：`decomp_full/UIExtension__GetCorners.c` **19 行** = `GetWorldCorners(4)` 返回四角。我们同一 px 空间（`PxRect` 四角就是四个数）⇒ 无同形方法、也无需。"),
    37: dict(note="🆕 2026-10-28 B92_N5：同 `GetCorners`（`MaxX` ⇒ `PxRect.x2`；原版体 = 四角里取最大 x）。"),
    38: dict(note="🆕 2026-10-28 B92_N5：同 `GetCorners`（`MaxY` ⇒ `PxRect.y2`）。"),
    39: dict(note="🆕 2026-10-28 B92_N5：同 `GetCorners`（`MinX` ⇒ `PxRect.x1`）。"),
    40: dict(note="🆕 2026-10-28 B92_N5：同 `GetCorners`（`MinY` ⇒ `PxRect.y1`）。"),
    41: dict(note="🆕 2026-10-28 B92_N5：与 `ContentSizeFitterMinMax` / `RectSizeLimiter` 同族（uGUI `ILayoutController`），缺的是「通用组件 + 一个真实消费者」。⚠️ 原版该行 `.c` 与下一行**字节相同**（ICF 折叠），不是内容错位。"),
    42: dict(note="🆕 2026-10-28 B92_N5：同上（`CalculateLayoutInputHorizontal` 与 `ApplyConfig` 同机器码 —— ICF 折叠）。"),
    43: dict(status='A', landing='CardPresentation/Hand/HandLayout.cs（HoverLiftPxForDevice）',
           note=(
        "🆕 2026-10-28 B92_N5 **已实现升 A**：新增 `HandLayout.HoverLiftPxForDevice`（声明在**类尾**，零行号漂移 —— "
        "本文件在账里有 `文件:行号` 落点），逐句照 `decomp_full/VarsDevice__GetCardInHandYOffsetTouchInput.c`："
        "`GetScreenPhysicalSize() <= 7.0f ? 30 : 20`（两个值出自 SO `VarsDesktop` 的 `cardInHandShownYOffset: 30` / "
        "`cardInHandShownYOffsetMin: 20`，`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/VarsDesktop.json`）；"
        "`hoverLift` 的初值改成 `HoverLiftPxForDevice / 108f`（`Hand/HandLayout.cs:265`）。"
        "⚠️ **只覆盖桌面那一份 SO**：`VarsMobile`（25 / 12）本仓未建 ⇒ 移动端两档**没有**（如实标，⛔ 别当全情况）。")),
    44: dict(status='D', note=(
        "🔴 2026-10-28 B92_N5 判 **D**（§18·4 (c) 四源全负）。**同一原版方法在两张账里各有一行**（铁律：两行必须一致）："
        "`账/收藏.tsv:287`（`ItemDrawer.SetItemName`）**早就判了 D**，证据比我这一行强得多 —— "
        "`cpp2il` 全量正文搜 = 只有声明处；`assets_full` **4.4 GB 全树 grep `SetItemName` = 0 命中**"
        "（prefab `m_PersistentCalls` / 场景 / SO / Addressables 都在那份里）。本代理复核：类桩在，"
        "体在 `decomp_generic_trial/out/`（泛型类那一批），但**零调用点 + 零资源引用 ⇒ 原版可观测行为为零**。"
        "**时间限定**：本 build（`Assembly-CSharp.dll` 2026-09-17）· 资源快照 2025-07-30。"
        "**可逆备注**：发现任何生产点（`m_MethodName` 命中 / 新反编译产物里出现调用）即回 C。"
        "残留不确定面（照抄 `收藏.tsv:287` 的备注）：`Assembly-CSharp-firstpass.dll` 不在 cpp2il 产物内（无 ItemDrawer 族类型）。")),
    45: dict(note=(
        "🆕 2026-10-28 B92_N5 现核（体在 `decomp_generic_trial/out/AlternateArtStyleItem___Il2CppFullySharedGenericType___TryGetItem__19f42a0.c`，**本代理实读**）："
        "按 `alternateArtSyle` 在缓存数组（`+0xc0` 链）里**逐个比 int**，命中则把条目值拷进 `out returnItem` + 加一次引用句柄、返回 1；"
        "不命中则把 `returnItem` 清零、返回 0。**没有 Addressables 调用**。"
        "⇒ 原版**可观测行为非零**（按风格取异画、取不到 false）⇒ 本行**不该判 D**。"
        "⚠️ **与兄弟行冲突**：`账/未分类_其他.tsv:28`（`AlternateArtStyleItem`，非泛型写法）判了 D，理由「唯一动作 = `cacheGroup.GetAsset(...)`」—— "
        "**那条理由与本代理实读的体不符** ⇒ 请调度台把 `未分类_其他.tsv:28` 一并回 B（本代理**未改那一行**：不在本批任务单）。"
        "**还差什么**：我们的等价是「数据表 + 取图口」（`Core/CardArt.cs`，异画 7 张）—— 要 A 需要把「按风格取」抽成一个与键同形的查询方法并接一个真实调用点。")),

    # ---------- 收藏 由 泛型与启动链_补读 提供（44 已处理）----------

    # ---------- 其他/基础设施（46..59）----------
    46: dict(status='A', landing='Core/Loc.cs（Languages）+ Loc.Load 的合法值闸', note=(
        "🆕 2026-10-28 B92_N5 升 **A**（方法级等价已核）：原版体 "
        "`decomp_full/LoginManager__IsActivatedLanguage.c` 全文 = "
        "`for (i = 0; i < languagesDefinition.Count; i++) if (String.Equals(languagesDefinition[i], currentLanguage)) return true; return false;` "
        "—— 就是「这个语言名在启用清单里吗」的**成员测试**。"
        "我们的等价数据 = `Core/Loc.cs:197` 的 `Languages`（**12 项、声明序 = 原版下拉序 = 原版 `languagesDefinition` 的序**）；"
        "同一跳的**成员测试** = `Loc.Load():3706` 的 `Enum.IsDefined(typeof(AvailableLanguages), v)`（存档里那个语言值合不合法）。"
        "**差异（如实）**：① 原版按**字符串名**比、我们按**枚举值**（本地数据形态：`PlayerPrefs[\"Language\"]` 存 int）；"
        "② `languagesDefinition` 在线上是**登录回包里下发的启用集**，本地取「12 项全启用」—— 依据 = 官方 13 语言表全在"
        "（`Resources/official_loc_13lang.json`），本地无从区分「启用/未启用」⇒ 这是**服务器相关**的那一档，按 §18·0 允许本地定。")),
    47: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：`Shell/GameModesHandler.cs:263` 的式子已逐句（含 `mainRankedType != rankedInterface ⇒ 递归` 那一跳）。"
        "**还差什么（B89_RXb 那条仍成立，且本代理确认自己做不了）**：`IRankedEvent` 接口未建，而**唯一该实现它的类是 `RankedEventBase`**"
        "—— 那个文件在 **N2 白名单** ⇒ 本代理建接口也只能是一个零实现者的空接口。"
        "⇒ 正确顺序：N2 给 `RankedEventBase` 实现 `IRankedEvent` 的同批，把 `RankedEventInterfaceType` 换成 `typeof(IRankedEvent)` 并删掉注入点。"
        "⚠️ 另：原版 `config == null` 那一支是 **NRE**，我们出声 + 返回 null（已标注偏离，属「原版 bug」未照抄 —— 建议照 §18·0 改回 `throw`）。")),
    48: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：落点 `CardPresentation/Core/CardView.cs:5401` 的 `RaceTerm`，"
        "三个原版方法的等价分支**在同一方法的不同分支**（不是同一句）："
        "兵种串 = `:5405` 两段拼（`Card_Race/` + 种族串）、战术/防御 = 先经 `:5352 RaceKeyBySubtype` 映射 SpellType 的 6 个写法、"
        "印不印由 `:5313 SubtypeLine` 判。**还差什么**：调度台那条「同一 `文件:行` 覆盖多方法 ⇒ B」是按**文件:行**拍的；"
        "按**分支**看这三条其实各有各的落点。请裁定：若接受「同一方法的不同分支可分别判 A」⇒ 这三行（本行 + 下一行 + `CardRaceToString`）可一起升 A。")),
    49: dict(note="🆕 2026-10-28 B92_N5：同上一行 —— 等价分支在 `CardView.RaceKeyBySubtype(:5352)` + `RaceTerm(:5401)`，与 `MinionRaceToString` / `CardRaceToString` **不同分支**。等上一条的裁定。"),
    50: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：`MenuTutorialFlow.cs:1105` 的数 key 形态已按原版体改过（见原差异 B91_R2）。"
        "**还差什么（残差没变，但本代理把它说准）**：原版 `tutorialStages`(+0xF8) 的元素类型 = `TutorialStage`（`dump.cs:42050`，"
        "**战斗教程脚本场景**），我们数的是 `MenuTutorialStages`（菜单 FTUE `FlowTutorialStage`）⇒ **两套阶段表**。"
        "升 A 的条件 = 战斗教程子系统落地、两张表合一或建立映射。")),
    51: dict(note="🆕 2026-10-28 B92_N5：与 `AllTutorialsCompleted` 同一处缺口（原版体 = `IsTutorialIndexCompleted(0) && IsTutorialIndexCompleted(1)`，两个 index 都读 liveops 活动数据字典）。条件同上。" ),
    52: dict(note="🆕 2026-10-28 B92_N5：与 `AllTutorialsCompleted` 同一处缺口（原版 = 活动数据字典 `ContainsKey(i.ToString())`）。我们 `MenuTutorialFlow` 的 PlayerPrefs 进度位与 `StageSaveData{stageId,isCompleted}` 同形，但**数据源不是同一份**。条件同上。"),
    53: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版 = `ReadDataFromFile()` → `CryptoString.DecryptRijndael` → 把 ~25 格逐格灌进 `GameStaticData.playerData`；读不到 ⇒ `LoadStarterData()`。"
        "**还差什么（具体到可施工）**：需要「一份 `LocalPlayerData` 总档 + 逐格灌」这一跳 —— 即一张**设置总档 ↔ 各消费点**的映射表"
        "（音量/画质/触摸/语言/提示/帧率…各自由哪个消费点读）。映射表没有 ⇒ 现在灌不进去。")),
    54: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版读的是 `<playerId>.dat`（`BinaryFormatter` + 版本比较）。我们**不产出该文件**"
        "（`SaveCenter` 是每 key 一份 JSON；`LegacySaveReader` 读的是另一族旧档）⇒ 「读那一份 .dat」在本仓**没有对象**。"
        "**还差什么**：先有 53 行那份总档，本行才有落点。")),
    55: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版 = `BinaryFormatter.Serialize` 整份 `LocalPlayerData` 写进 `<playerId>.dat`，"
        "写完立刻 `SetMusicAndFxVolumes` + `SetTargetFramerate`（**落盘后把设置重新应用**）。"
        "我们 `SaveCenter.Set/SetRaw` 是等价落盘，但**没有「总档 + 落盘后重新应用」那一跳**。**还差什么**：同 53 行的总档 + 应用回调清单。")),
    56: dict(note="🆕 2026-10-28 B92_N5：与 `LoadPlayerData` / `SavePlayerData` 同一条缺口（原版 `async d__467`：写教程状态进 `demoData` 后 `UploadOnlineDemoData()`；上报 = P3）。我们 = `MenuTutorialFlow` 的 PlayerPrefs 进度位 ⇒ 缺「挂在 `DemoData` 上」那一层实体。"),
    57: dict(note="🆕 2026-10-28 B92_N5：原版体 = `DemoData.ResetTutorial(demoData)`（换新字典 + `gameAfterTutorialPlayed(+0x18)=false`）+ `UploadOnlineDemoData()`。我们 `ResetProgress` 清的是菜单 FTUE 阶段位 ⇒ **清的不是同一份状态**（原备注已说准）。**还差什么**：`DemoData` 那份字典（`cpp2il/out_cpp2il_dev_ilspy/DemoData.cs:68-76` 是它的体）先建出来，`ResetProgress` 才能改清它。"),
    58: dict(note=(
        "🆕 2026-10-28 B92_N5 现核：原版 = `InventoryManager.DeserializeInventory(userData+0x50, +0x68)` + `GetOnlineData(0x32)` ⇒ `RewardService.Collect(pending)`。"
        "**还差什么**：① 服务器背包的反序列化（我们没有 `userInfo` 那层）；② `RewardService.Collect`（**编排层，未建**）。"
        "两者都在 reward-event 家族（账 654-668 那一批）⇒ 本行随那一批走，不单独落。")),
    59: dict(status='A', note=(
        "🆕 2026-10-28 B92_N5 升 **A**（§3·1 ①）：原版 `decomp_full/PlayerDataManager__set_victoryCounters.c` **1-6 行全文现读** = "
        "`*(this+0x2c8) = value` + GC write barrier —— **纯 store，没有任何别的行为**。"
        "我们的等价落点 = `Shell/VictoryCountersStore.Save`（`:125`，整表落 key 44，含序列化 = write barrier 的等价），"
        "且**已被 `RecordVictory`（`:151`）真实使用** ⇒ 方法级等价成立。"
        "**残留不在本方法内**：引擎触发点（`RuleCore.CheckWinner` → `RecordVictory`）属 `RuleEngine/**`（E1 线），"
        "见本行原备注；那不是 `set_victoryCounters` 这个方法的行为。")),

    # ---------- 菜单/外壳（60）----------
    60: dict(status='A', note=(
        "🆕 2026-10-28 B92_N5 升 **A**，并 **就地推翻本行上一条判断（铁律 5）**：\n"
        "  · 原版体 `decomp_full/ParentConstraint__Sync.c` 全文现读 = 三个开关各抄一次："
        "`+0x28` ⇒ `transform.position = target.TransformPoint(offset+0x38)`；"
        "`+0x29` ⇒ `transform.rotation = target.rotation * offset(+0x44)`；"
        "`+0x2a` ⇒ `transform.localScale = target.localScale * offset(+0x54)`。\n"
        "  · **`[B91-L6]` 提的「接到 `EffectAnchorBoard`（跟 `_body3D`）」不成立**（照它做会引入偏差）："
        "原版那颗 `EffectAnchor` 的 `targetParent` 是 **`3DBody`**，而 `3DBody` 在 `Board Elements` 下"
        "**localPosition (0,0,0) / localRotation 单位 / localScale 1**"
        "（实读 `bundle_battleprefabs_vfxandmisc_assets_all/Transform/Transform_5025805728107043776.json`）"
        "⇒ 三个 inherit 全开但**等价于「普通子级」**。"
        "而我们的 `_body3D`（`CardView.BuildBody3D`）是 **`3DBody` + `Card 3D` 合并后的节点**（带 `Euler(0,180,0)` 与 `0.88586`）"
        "—— 跟它会让锚点**多转 180°、放大 13%**。\n"
        "  · 我们的 `CardView.EffectAnchorBoard`（`:2097`）把锚点**直接挂在卡根、局部位 = `BoardEffectAnchorLocal`**，"
        "即 = 原版序列化的 `(0, 1.34, −0.128)`（`Transform_-5295299710322893888.json` 实读）"
        "⇒ 经 `TransformPoint(单位变换)` / `× 单位四元数` / `× 单位缩放` 后**逐项等于原值** ⇒ **逐句等价，不需要 Sync**。\n"
        "  ⚠️ 唯一剩余不确定性：`3DBody` 上挂着 `Animation` + 飞行控制器（`initialHeightOffset/flyingHeight/yMovementCurve`）"
        "⇒ 它在原版里**会动**；但我们的飞行动画动的是**卡根**，锚点照样跟住（等价）。"
        "**结论：无需改代码**；⛔ 别再按「跟 `_body3D`」去接。")),
}

# 迁移行：任务单里 域=收藏 / 未分类_其他 的那两条，主表真身在 泛型与启动链_补读.tsv
MIGRATED = {44: '泛型与启动链_补读.tsv', 45: '泛型与启动链_补读.tsv'}


def find_row(i, task_row):
    dom, cls, meth = task_row[0], task_row[1], task_row[2]
    fn = MIGRATED.get(i) or DOMAIN_FILE[dom]
    rs = CACHE.get('__' + fn)
    if rs is None:
        rs = rows(os.path.join(LEDGER, fn))
        CACHE['__' + fn] = rs
    hits = [x for x in rs[1:] if len(x) >= 11 and x[0] == dom and x[1] == cls and x[2] == meth]
    assert len(hits) == 1, (i, fn, dom, cls, meth, 'hits=%d' % len(hits))
    return hits[0], fn


out = [HEAD]
changed = 0
for i in range(1, len(TASK)):
    tr = TASK[i]
    src, fn = find_row(i, tr)
    row = list(src)
    mod = MODS.get(i, {})
    if 'status' in mod:
        row[5] = mod['status']
    if 'landing' in mod:
        row[6] = mod['landing']
    if mod.get('note'):
        old = row[7].strip()
        row[7] = (old + '｜' if old else '') + mod['note']
        changed += 1
    assert len(row) == 11, (i, len(row))
    out.append(row)

if '--write' not in sys.argv:
    print('DRY-RUN：%d 行（其中改动 %d 行）' % (len(out) - 1, changed))
    from collections import Counter
    print('状态分布：', dict(Counter(r[5] for r in out[1:])))
    sys.exit(0)

buf = '\r\n'.join('\t'.join(c.replace('\t', ' ').replace('\r', '').replace('\n', '\\n') for c in r) for r in out) + '\r\n'
io.open(OUT, 'wb').write(b'\xef\xbb\xbf' + buf.encode('utf-8'))
# 回读自检（裸 split，逐列往返）
b = io.open(OUT, 'rb').read()
assert b[:3] == b'\xef\xbb\xbf'
assert b.count(b'\r\n') == len(out), (b.count(b'\r\n'), len(out))
back = b.decode('utf-8-sig').split('\r\n')
assert len(back) == len(out) + 1
for k in range(len(out)):
    assert len(back[k].split('\t')) == 11, k
print('WROTE %s  rows=%d  crlf=%d  bom=yes' % (OUT, len(out) - 1, b.count(b'\r\n')))
