// DraftModeWindow.cs — 轮抽（Draft）**免费/常规**那一扇窗：原版 `Draft Mode Menu Demo`
//   （`DraftModeMenuDemo : GameWindowWithTabs`；**不是**限时版 `Draft Mode Timed Mode Window`）
//
// ==================================================================
//  出处（唯一判据）
// ------------------------------------------------------------------
// · **原版规格正本** = `资料/普查产出_1024b/S3_轮抽窗原版规格.md`（94 KB，逐节点：路径 / 矩形 / anchor / pivot /
//   字号 / 图片 PathID / 初始 active / 显隐判据；§三 交互链 17 步、§四 空态、§五 缺口、§七 查不到）。
//   本文件每一段几何的注释都写「照 S3 §x.y」或「我们挑的」。
// · 复核用的现读（本轮亲自跑过，数值直接抄进常量）：
//   `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<节点名>" --depth N --relative`
//   —— `Draft Mode Menu Demo` / `Draft Mode Pay State` / `Draft Mode Select Packs State` /
//   `Draft Mode Select Warlord State` / `Draft Mode Ongoing State` / `Draft Mode Deck Info Panel` /
//   `Prebuilt Pack UI` / `Generic Select Card Container` **八棵树逐棵现读**。
// · 窗参（根 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7910505541983962427.json` 逐字段实读）：
//   `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//   `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1`.
// · 状态机：`decomp_full/DraftModeMenuDemo__GetTabFromState.c` / `__GetStartingTab.c` / `__GoToCurrentState.c`；
//   枚举值 `dump.cs:126782`（`DraftModeState`：Intro0 / PayScreen2 / SelectWarlord5 / SelectPack10 /
//   OngoingRun15 / Finished20 / Collected22）。
// · 抽包 / 组卡：**只走** `Shell/DraftPacks.cs`（`RollRound` / `BuildClosedDeck`）—— ⛔ 本文件不另写一套抽样。
// · 进对局：`BattleDriver.SetPendingPlayMode(GameMode.ClosedDeck)` + `PrebuiltDecks.SetPendingBattleDeck`
//   （= 既有的「本局用这副牌」通道，判据 → `PrebuiltDecks.SetPendingBattleDeck` 那段注释）。
//
// ==================================================================
//  🔴 本批的边界（用户口径 / 派单）
// ------------------------------------------------------------------
// · **只做免费档**：`Premium Button` / `Premium Ten Button` **不建**（原版 `premium10Price < 0` 时整节点
//   `SetActive(false)` ⇒ 序列化上就允许「没有这两档」；我们把它当成「本地档位永远不存在」）。
//   ⚠️ 价格 `premiumPrice` / `premium10Price` **是服务端值、本地 0 命中**（S3 §五）⇒ 不编价格。
// · **重掷**：`Reroll` 整行**已建**（2026-10-24 第七十四会话 `W7`）。价格规则照原版
//   `GetReRollCosts`（`FirstOrDefault(当前次数)` → `LastOrDefault` → 表空 ⇒ 0）；🔴 **价格表本身在服务端、
//   本地 0 命中**（D2 §五·1）⇒ 我们那张表是**空的** ⇒ 恒「免费」。拿到真值时只填 `ReRollCosts` 那一格。
//   ⚠️ 原版这一刻走**服务端扣费**，本地**只重抽、不扣钱**（并在日志里说清）。
// · **不做限时版**：`Draft Mode Timed Mode Window` 是另一份 prefab（Pay/Warlord 两页换 Timed 子类 + 多
//   阵营进度条 / 联盟积分，S3 §2.10）⇒ 本窗一个节点都不带它。
// · **Intro 页不建节点**：原版 `Draft Mode Intro State` **不在 `tabs` 里**、全反编译找不到它的激活者
//   （S3 §七·1「查不到」）；而 `GetTabFromState(0)` 落到 **Pay 页**（else 分支）⇒ 我们的
//   `DraftModeState.Intro` **路由到 Pay 页**，⛔ 不凭空造一个第 5 页。
// · **付费活动倒计时 / 计时器不建**：`Timer` 读的是活动结束时刻（服务端下发的 liveop 数据，本地 0 命中）
//   ⇒ 建一个假倒计时 = 骗玩家。`Open()` 出声说明。
//
// ==================================================================
//  ⚠️ 缺口表（S3 §五）—— 哪些数是原版、哪些是我们挑的
// ------------------------------------------------------------------
//  | 参数 | 谁给的 | 我们的取法 |
//  |---|---|---|
//  | `CardsPerPack = 3` | **原版真值**（882/882 包实测） | `DraftPacks.CardsPerPack` |
//  | `draftRounds = 4` | 服务端（`DraftEventData+0x98`），本地 0 命中 | `DraftPacks.RoundsPerDraft`（**我们挑的**） |
//  | `packsPerDraft = 3` | 服务端（`+0x9C`） | `DraftPacks.PacksPerRoundOffered`（**我们挑的**） |
//  | `warlordsPerDraft` | 服务端（`+0xA0`），本地 0 命中 | `WarlordsPerDraft = 3`（**依据版面证据 + 用户口述定的**，⛔ 不是读到的字段值 —— 判据见那一条常量的 doc） |
//  | `seed` | **服务端事件实例字段**，本地不存在 | 本窗自己抽（`BeginRun` 收一个 seed；**打进日志**，可复现） |
//  | `Rewards` / `premiumPrice` | 服务端 | 本轮不做（见上「本批的边界」） |
//  | `reRollCosts` | 服务端（`+0xE0`），本地 0 命中 | 表**空** ⇒ 恒免费（规则照原版 `GetReRollCosts`；见 `ReRollCost`） |
//
//  🔴 **`warlordsPerDraft` 取 3 的版面理由**（2026-10-24 第七十四会话 · `D2` 查证 + `W7` 落地）：
//     卡格布局宽 412.75 + 80 宽 `Spacing` + HLG `m_Spacing 20.37` ⇒ 中心步距 **533.49**；
//     3 张总宽 **1479.73** ⇒ 居中、两边各余 ~220，一屏放得下。4 张 = **2013.22 > 1920** 会溢出，
//     而原版那颗 `ScrollRect` 的序列化值是 **`m_Enabled = 0`**（现读，`⛔MULTI-COMP`）⇒ 拖不动。
//     ⇒ 3 张时**不建滚动区**（本文件 `BuildWarlordPage` 的那一支），与原版「组件级禁用」同观感。
//
// ==================================================================
//  🔴 建窗纪律（本工程踩过的，逐条照办）
// ------------------------------------------------------------------
// · `Open()` 第一句就 `PointerLayer.UnregisterOwnedBy(gameObject)` —— 本方法会清子件并重建 `MenuScroll`，
//   旧滚动条目不清会**每重建一次净涨 1 条**（判据 → `Shell/LiveOpsEventWindow.cs` 的 `Build()` 首段）。
// · 压暗层点击 = **关窗**（`MenuDraw.ShadeHit`，档落在压暗层自己那一档、严格低于本窗内容命中档）。
// · 红底那一块**吸收**点击（`MenuDraw.Absorb`）：原版点红底什么都不做，只有压暗层外沿是「点外面关窗」。
// · 文字/按钮都要**挂在原版那个父节点底下**（`FindChild(钮,"Button Text")` 靠它）；节点名逐字照原版。
//
// ==================================================================
//  🆕 2026-10-24（第七十三会话 · 执行代理 `E3`）：接上三条尾巴
// ------------------------------------------------------------------
//  **(A) 卡包预览弹窗** —— 原版 `PrebuiltPackUI.PreviewButtonOnClick` → `DraftModeSelectPacksStateDemo.PackPreview`
//      → `UIMultiCardDisplay.Initialize(pack.GetCardLibrary(), null)` + `WindowsManager.OpenWindow(多卡展示窗)`。
//      判据（逐句）：`decomp_full/DraftModeSelectPacksStateDemo__PackPreview.c` ·
//      `UIMultiCardDisplay__{Initialize,Awake,Open,Close}.c` · `dump.cs:110035`（7 个字段）；
//      版面 = `bundle_menus_assets_all` 的 `Generic Multi Card Display`（**父链实读到 Draft Mode Menu Demo**，
//      根 RT `anchor(0,.5)-(1,.5) apos(0,−20.475) size(0,818.04)`）+ `MonoBehaviour_1014665548285115077.json`
//      （窗参 `type=1 / windowsPlacement=15 / closeOnESC=1 / updateNavPanel=0`，`enableCardTooltips=1`，
//      `mask = {PathID:0}`）。🔴 **那颗 `BackgroundCloseButton.window` 是空引用**（`MonoBehaviour_-1185320953084347707.json`）
//      ⇒ 原版**点背景不关窗**（与 `PopUpGameWindow` 同族）⇒ 我们用 `MenuDraw.Absorb`，⛔ 不是 `ShadeHit`。
//      ⚠️ 原版这一刻是**独立一扇窗**；本轮白名单只允许改本文件 ⇒ 落成**同一棵树里的内嵌层**
//      （新增一个 MonoBehaviour 类会破掉「211 个类名==文件名」那条普查、正是 `!u!115` 的成因）。
//      ESC 那一档：预览开着时 `Close()` 先关预览（见 `Close` 覆写）。
//  **(B) 战斗结果回灌** —— 原版 `DraftEventBase.OnMatchFinished(matchWon, result, cb)`
//      → `PlayerDataManager.RecordEventMatchFinished` →（服务端）写 `numberOfMatchesWon(+0x48)` /
//      `numberOfMatchesLost(+0x4C)` → **`DraftEventBase.UpdateState`** 重算 `DraftState`。
//      本地没有服务器 ⇒ 战绩的落地源 = **既有的本地对局记录** `Shell/BattleLogData.cs`（结算处写，落盘），
//      只认 `Mode == PlayModeNames.Name(GameMode.ClosedDeck)` 的那些条（⛔ 不碰 `BattleDriver`）。
//      一局的进度存在**静态** `_run`（`RunSave`）里 ⇒ 关窗、进对局、回壳再开窗都在。
//  **(C) `Finished` 的触发者** —— 🔴 **不是「4 轮打完」**：`UpdateState` 里
//      `selectedPacks.Count < draftRounds ⇒ SelectPack(10)`、否则 `OngoingRun(15)`（VA `0x18181417D..0x1818141DD`）。
//      `Finished(20)` / `Collected(22)` 只在**同一处**给出（`0x1818142F0` 那段 `cmove edx, 0x14 / 0x16`）：
//      `save.joinState == Finished(2)` · `numberOfMatchesLost >= data.maxTries` · `numberOfMatchesWon >= data.matches.Length`
//      —— 前两条本地有判据，第三条的 `matches.Length` **在服务端、本地 0 命中**（S3 §五）⇒ 我们**不设胜场上限**（如实标）。
//      `FinishRun(onFinish)` 是**显式置 20** 的那一支（`..._FinishRun_g__Callback_0.c`：先
//      `set_DraftState(0x14)`、再 `UpdateState`、再回调、最后 `Signal.Raise(DraftRunFinished)`）。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `DraftModeState`（`dump.cs:126782`）。**值照原版**，别改。</summary>
    public enum DraftModeState
    {
        Intro = 0,
        PayScreen = 2,
        SelectWarlord = 5,
        SelectPack = 10,
        OngoingRun = 15,
        Finished = 20,
        Collected = 22,
    }

    /// <summary>原版 `PlayEventSaveGame.JoinStates`（`dump.cs:62813-62820`，**值照原版**）。
    /// 只用来复刻 `DraftEventBase.UpdateState` 的第一条收尾判据（`joinState == Finished(2)`）。</summary>
    public enum DraftJoinState
    {
        NotJoined = 0,
        Joined = 1,
        Finished = 2,
        Expired = 3,
    }

    /// <summary>原版 `PayStates`（`dump.cs:126806-126814`，**值照原版**）。`DraftEventBase.UpdateState`
    /// 拿 `joinState == Joined && payState == notPaid(0)` 判「该落在 Intro 页」。</summary>
    public enum DraftPayState
    {
        NotPaid = 0,
        Free = 1,
        Premium = 2,
        PremiumTen = 3,
    }

    /// <summary>轮抽一局的**本地存档**（= 原版 `DraftEventSaveGame` 里我们真正用得上的那几格）。
    ///
    /// <para>🔴 **为什么要有它**：原版这一局活在**服务端**的 `DraftEventSaveGame` 里 —— 关窗、进对局、
    /// 回壳再开窗，`selectedWarlord / selectedPacks / selectedCards / numberOfMatchesWon / numberOfMatchesLost /
    /// rewardsCollected` 全都还在（`dump.cs:126817-126832`）。本地没有服务器 ⇒ 用**进程内静态对象**顶这一格：
    /// 它活过「窗被销毁 + 场景切换」，⛔ 但**活不过退出游戏**（那一段要落盘，见文件末「顺手发现」）。</para>
    ///
    /// <para>字段对应（原版偏移逐条注明，⛔ 别改名去凑别的意思）：
    /// `numberOfMatchesWon`(+0x48) / `numberOfMatchesLost`(+0x4C) / `rewardsCollected`(+0x70) /
    /// `payState`(+0x78) / `selectedWarlord`(+0x88) / `selectedPacks`(+0x90) / `selectedCards`(+0x98) /
    /// `joinState`(+0x30，父类 `PlayEventSaveGame`)。
    /// `consumedDraftMatches` 与 `seed` 是**本地新增**（服务端没有对等物，见各自注释）。</para></summary>
    [Serializable]
    public class DraftRunSave
    {
        /// <summary>原版 `PlayEventSaveGame.joinState`(+0x30)。`Finished(2)` ⇒ `UpdateState` 直接走收尾分支。</summary>
        public int joinState;
        /// <summary>原版 `DraftEventSaveGame.payState`(+0x78)。我们只有 `NotPaid(0)` / `Free(1)` 两档
        /// （付费档本轮不做 ⇒ ⛔ 永远不写 2/3）。</summary>
        public int payState;
        /// <summary>原版 `numberOfMatchesWon`(+0x48) —— **Victories 那个数字就是它**
        /// （`DraftRunDisplay__Initialize.c`：`victoryCounterText.text = save+0x48.ToString()`）。</summary>
        public int matchesWon;
        /// <summary>原版 `numberOfMatchesLost`(+0x4C) —— 败场格亮几颗就是它。</summary>
        public int matchesLost;
        /// <summary>原版 `rewardsCollected`(+0x70)。`UpdateState` 收尾时 `false ⇒ Finished(20)`、`true ⇒ Collected(22)`。</summary>
        public bool rewardsCollected;
        /// <summary>本局种子（**本地新增**：原版 seed 是服务端事件字段，见 S3 §五）。</summary>
        public int seed;
        /// <summary>原版 `selectedWarlord`(+0x88)，我们存卡 id。</summary>
        public string warlordId;
        /// <summary>原版 `selectedPacks`(+0x90)，我们存包的唯一键（`DraftPacks.PackDto.key`，⛔ 不是 `packId`）。</summary>
        public string[] pickedKeys;
        /// <summary>原版 `selectedCards`(+0x98)。</summary>
        public string[] selectedCards;
        /// <summary>原版 `DraftEventSaveGame.currentReRolls` —— 本局已经重掷过几次。
        /// `RefreshReRollPrice` 拿它去 `GetReRollCosts` 里找**当前那一档**价格
        /// （`DraftModeSelectPacksStateDemo__RefreshReRollPrice.c`：`GetReRollCosts(data, save+0xB0)`）。</summary>
        public int reRolls;
        /// <summary>🔴 **本地新增**：`BattleLogData` 里已经吃进战绩的轮抽对局条数。
        /// 对局记录是「新的在前」（`BattleLogData.Add` 里 `_all.Insert(0, m)`）⇒ 未吃过的就是前
        /// `(总条数 − 这个数)` 条。开新局时**先记基线**（否则会把上一局的对局算到这一局头上）。</summary>
        public int consumedDraftMatches;
    }

    /// <summary>轮抽的**免费/常规**窗（原版 `Draft Mode Menu Demo`）。
    ///
    /// <para>页面切换**按状态类型**（原版 `GameWindowWithTabs.GetTab&lt;T&gt;()` + `ChangeTabCO`），
    /// ⛔ 不是页签按钮 —— 原版 prefab 里没有任何页签节点（S3 §〇·3）。我们不挂 `MonoBehaviour` 页类
    /// （嵌套类建不出脚本资产，判据 → `CLAUDE.md` §二 的 `!u!115` 那一段），用
    /// <see cref="PageFor"/> 那一张 **state → 页节点** 的表代替 `GetTab&lt;T&gt;()`。</para>
    ///
    /// <para>⚠️ **本类不碰战斗**：`To Battle` 只做「组牌 + 把牌交给既有开战通道 + 切场景」，
    /// 战斗内一行不动。</para></summary>
    public class DraftModeWindow : GameWindow
    {
        public const string RootName = "Draft Mode Menu Demo";

        // ============================================================ 渲染队列（自成一档 4130–4199）
        // 相邻带（现读全仓队列常量，最高一条 = ToastNotifications 4072–4076 · `QGlow` 4110 / 4112–4116）
        // ⇒ 本窗从 **4130** 起，留 13 档空隙。
        public const int QShade = 4130;   // 压暗整屏（`Menu Dark Background` 的 quad + 它的命中区）
        public const int QBg1 = 4131;     // 红底（`Reward Background Get Reward`）
        public const int QBg2 = 4132;     // `Noise`
        public const int QBg3 = 4133;     // `Menu Vignette`
        public const int QPanel = 4135;   // 右栏面板 / 奖励面板的底 / 右栏那三颗 toggle
        /// <summary>**卡面【后面】**那一层（督军立绘 · `Packs Mask` 的底板 · 单包卡的底与描边）。
        /// 🔴 它必须**严格低于** `QCardBase` —— 否则卡面会被自己的底板盖死（本工程的老坑：
        /// 同档/高挡的透明层谁盖谁不可控，判据 → `Shell/LiveOpsEventWindow.cs` 的队列梯子注释）。</summary>
        public const int QPanelBack = 4136;   // 底板（`Packs Mask` 的底板 / 督军立绘）
        public const int QPanelBack2 = 4137;  // 压在底板上的那一层（**单包卡的底** —— 它压在 Mask 底板上）
        public const int QPanelArt = 4138;    // 再上一层（包卡的橙色描边 / 分隔线）
        /// <summary>卡面那一段的**基号**：**一张卡占 1 档**（`CardView.SetQueue` 把整张卡盖成同一号；
        /// 卡内层序由 `CardView` 自己按层名定）。同一屏最多 5 张选将卡 / 9 张包内小卡 ⇒ 4140…4148。</summary>
        public const int QCardBase = 4140;
        public const int QArt = 4160;     // 按钮底（压在卡面上）
        public const int QArt1 = 4161;    // 压在 `QArt` 上的那一层（图标 / 金框）
        public const int QText = 4165;
        public const int QHit = 4170;     // 内容命中区（**必须 > 卡面**，否则卡盖住按钮点不到）
        /// <summary>`MenuDraw.ShadeHit` 要求压暗档 **严格低于**内容命中档 —— 本窗 = `QShade` &lt; `QHit`。</summary>
        public const int QHitContentMin = QHit;

        // ---- 卡包预览层（原版 `Generic Multi Card Display`）自占 4180–4199 那一段 ----
        // 🔴 它必须**整层压在本窗所有内容命中区之上**（否则预览开着还能点下面的包卡）：
        //    `QPreviewShade(4180) > QHit(4170)`。同族先例 = `Shell/BoosterInfoPopup.cs` 的压暗层必须低于自己的内容。
        public const int QPreviewShade = 4180;    // `Menu Dark Background` 的图
        public const int QPreviewCardBase = 4182; // 三张卡（一张 1 档：4182/4183/4184）
        public const int QPreviewArt = 4190;      // Back 钮的底图
        public const int QPreviewText = 4191;     // `Header Text` / `Back` 字
        /// <summary>预览层的内容命中区（= `Absorb` 取 `−1` = 4193）。</summary>
        public const int QPreviewHit = 4194;

        // ============================================================ 规模（见文件头「缺口表」）
        /// <summary>选将页给几张战将。
        /// <para>🔴 **3 是依据「版面证据 + 用户口述」定的，⛔ 不是读到的字段值**：
        /// 原版机制是 `DraftEventData.warlordsPerDraft`（`+0xA0`），**值由服务端下发、本地 0 命中**
        /// （`资料/普查产出_1024d/D2_轮抽各界面原版规格.md` §1.1 / §五·1 已核：四个 bundle + 关服前存档
        /// + 官方本地化 10854 条全扫过）。三条版面旁证都指向 3：
        /// ① 一屏放得下几张 —— 卡格布局宽 **412.75** + 80 宽 `Spacing` + HLG `m_Spacing 20.37`
        ///    ⇒ 步距 **533.49**；3 张总宽 **1479.73**（居中、两边各余 ~220）；4 张 = 2013.22 **&gt; 1920**
        ///    会溢出，而原版那颗 `ScrollRect` 是 `m_Enabled = 0`（拖不动）；
        /// ② 同一 HLG 算法自证：prefab 里 1 卡 + 1 `Spacing` 的 CSF 结果 = **513.12 = 412.75+20.37+80**；
        /// ③ 包区容量同构：3 包刚好（`Packs Mask` 1077.65；3×305.89+2×13 = 943.67），4 包 = 1262.56 会被裁。
        /// 用户 2026-10-24 真机点名「原来是 4 个，原版本应该是三个」。</para>
        /// <para>改这一个数会同时改选将页的卡片数、总宽与是否建滚动（其余地方全跟着它）。</para></summary>
        public const int WarlordsPerDraft = 3;

        /// <summary>一局最多输几场就收尾（= <see cref="LossSlots"/>，判据见那一行）。
        /// 原版是 `StagedEventData.maxTries`（`+0x90`，**服务端值、本地 0 命中**）⇒ 3 这个数**我们挑的**。</summary>
        public const int MaxTries = LossSlots;

        /// <summary>右栏 `CardCounter` 的**分母**。
        /// <para>原版 = `DraftModeDeckInfoEnergyBalancePanel.Initialize` 里的
        /// `Concat(trans, count, "/", deckSize)`，而 `deckSize` 取
        /// `PlayEventData.gameplayVariables(+0x80).deckSize(+0x24)` —— **服务端下发**
        /// （`dump.cs:130233 / 130178`；D2 §2.4）。</para>
        /// <para>本地那一份 = `GameplayVariables.For(GameMode.ClosedDeck).deckSize`（= **12**）——
        /// ⚠️ **也是我们挑的**（复用遭遇那套的 12，依据 = 每包 3 张 × 4 轮 = 12 张的结构性一致；
        /// 见 `RuleEngine/Core/GameplayVariables.cs` 的 `ClosedDeck` doc）。拿到服务端真值时改那**一处**。</para>
        /// <para>🔴 ⛔ 别再硬写 `RoundsPerDraft × CardsPerPack` —— 那是同一个数的**第二份**写法，
        /// 两处迟早不一致（本文件的 `CardCounter` 原来就是这么写的）。</para></summary>
        public static int DeckSizeForDraft
        {
            get { return GameplayVariables.For(GameMode.ClosedDeck).deckSize; }
        }

        // ============================================================ 重掷（`Reroll`）
        /// <summary>本局已经重掷过几次（原版 `DraftEventSaveGame.currentReRolls`）。</summary>
        public int ReRolls { get { return _run != null ? _run.reRolls : 0; } }

        /// <summary>重掷价格表（原版 `DraftEventData.reRollCosts`，`+0xE0`）。
        /// <para>🔴 **空表 = 我们挑的**（不是原版值）：服务端下发，本地 0 命中（D2 §五·1）。
        /// **空表的语义照原版** = `GetReRollCosts` 返回 **0 = 免费**（见下面 `ReRollCost` 的规则）。
        /// 拿到服务端真值时把这一格填上即可 —— ⛔ 别在别处另写一套价格规则。</para></summary>
        static readonly int[] ReRollCosts = new int[0];

        /// <summary>`GetReRollCosts(currentReRolls)` 的**原版规则**（逐句照
        /// `d:/2/tools/decomp_full/Everguild.LiveOps.DraftEventData__GetReRollCosts.c`）：
        /// `FirstOrDefault(entry.numberOfReRolls == 当前次数)` → 没有就 `LastOrDefault`（**沿用最后一档**）
        /// → 表空 ⇒ **0（免费）**。**原版没有「次数用完」的上限**。
        /// <para>`PriceDisplayButton.Setup(cost, …)`：`cost == 0` ⇒ 原版走 `PriceDisplay.SetupFree`
        /// ⇒ 画「Free」（且按钮**可点**，`PriceDisplayButton__Setup.c` 的 `param_2 == 0` 那一支）。</para></summary>
        public int ReRollCost
        {
            get
            {
                if (ReRollCosts.Length == 0) return 0;
                int cur = ReRolls;
                for (int i = 0; i < ReRollCosts.Length; i++)
                    if (i == cur) return ReRollCosts[i];
                return ReRollCosts[ReRollCosts.Length - 1];
            }
        }

        // ============================================================ 背景（S3 §2.1 现读）
        static readonly PxRect ShadeR = new PxRect(-1327.30f, -773.63f, 3247.30f, 1798.73f);
        static readonly PxRect RedR = new PxRect(0f, 94.60f, 1920f, 985.40f);
        static readonly PxRect VigR = new PxRect(0f, -27.20f, 1920f, 1052.30f);
        static readonly Vector4 RedBorder = new Vector4(0f, 26f, 0f, 26f);   // 21×81 · border(0,26,0,26)
        const float RedTexW = 21f, RedTexH = 81f;
        /// <summary>`Noise` 的 Tiled 格 = 贴图 1024 ÷ `m_PixelsPerUnitMultiplier 1.77`。</summary>
        const float NoiseTilePx = 1024f / 1.77f;

        // ============================================================ 顶部 title 栏（S3 §2.1）
        // 原版 `Game Mode Header With Back Button` = 550×109.55 @ (0,526) · anchor (0,0.5) · pivot (0,1)
        // ⇒ 设计中（左上原点）= y 14 … 123.55。下面几格照兄弟窗（`LiveOpsEventWindow.BuildHeader`）
        // 的同一套结构、按本窗那 26.86 的位移整体上移（-26.86 = 40.86 − 14）。
        const float HdrL = 0f, HdrT = 14f, HdrR = 550f, HdrB = 123.55f;
        const float PlateR = 690.86f;                     // 底板被 `ContentSizeFitter` 撑开到 690.86
        const float WingT = 14.01f, WingB = 129.37f;      // `Header Background (1)`
        const float WingL = -462.10f, WingR = 87.90f;
        const float BackL = -24.40f, BackT = 16.02f, BackR = 143.48f, BackB = 127.35f;
        static readonly string[] ToggleArtOnOff = { "40K_settings_button_hover", "40K_settings_button" };

        // ============================================================ Pay 页（S3 §2.3 · Demo 版）
        // 换算：`Scale menu` = 1920×1082 @ (272,33)（anchor/pivot 都是 (0.5,0.5)）
        // ⇒ 页内 center = (1232, 507)；下面每格 = center + (anchoredPosition.x, −anchoredPosition.y)。
        static readonly PxRect PayTitleR = new PxRect(957.36f, 275.80f, 1524.64f, 321.48f);
        static readonly PxRect PayHelpR = new PxRect(966f, 380.10f, 1516f, 510.50f);
        static readonly PxRect PayDescR = new PxRect(966f, 526.73f, 1516f, 661.88f);
        static readonly PxRect PayBarR = new PxRect(957.36f, 653.77f, 1524.64f, 822.24f);
        static readonly PxRect FreeBtnR = new PxRect(966.91f, 707.25f, 1136.91f, 768.77f);
        static readonly PxRect FreeTxR = new PxRect(978.5f, 714.5f, 1125.3f, 762.5f);

        // ============================================================ SelectWarlord 页（S3 §2.4 现读）
        static readonly PxRect WlTitleR = new PxRect(53.60f, 148.60f, 1866.40f, 225.40f);
        static readonly PxRect WlViewR = new PxRect(0f, 101.90f, 1920f, 1006.90f);
        static readonly PxRect WlContBtnR = new PxRect(1526.50f, 932.70f, 1866.40f, 1026.50f);
        static readonly PxRect WlContTxR = new PxRect(1539.7f, 938.9f, 1853.2f, 1020.3f);
        /// <summary>选将格：布局框 412.75×617.69 @ `localScale 1.221` ⇒ 视觉 503.97×754.20（S3 §2.4 / 现读）。</summary>
        const float WlSlotW = 412.75f, WlSlotH = 617.69f, WlSlotScale = 1.221f, WlSlotGap = 80f;
        const float WlSlotVisW = WlSlotW * WlSlotScale;      // 503.97
        const float WlSlotVisH = WlSlotH * WlSlotScale;      // 754.20
        /// <summary>`Content` 那颗 HLG 的 `m_Spacing`（现读 **20.37**）。`UICardSelector.Initialize` 在
        /// 奇数位插一个宽 80 的 `Spacing` 预制体、偶数位插卡格 ⇒ 卡与卡之间是
        /// **20.37（HLG 缝）+ 80（Spacing 件）+ 20.37（HLG 缝）**。</summary>
        const float WlHlgSpacing = 20.37f;
        /// <summary>卡格中心步距 = `412.75 + 80 + 2×20.37` = **533.49**
        /// （D2 §1.1 用同一算法自证：prefab 里 1 卡 + 1 `Spacing` = **513.12 = 412.75+20.37+80**）。
        /// 🔴 旧值 **492.75** 漏了 HLG 那两道缝 —— 那是「4 张挤一排、第 5 张出屏」的成因之一。</summary>
        const float WlStep = WlSlotW + WlSlotGap + 2f * WlHlgSpacing;   // 533.49

        /// <summary>`WarlordsPerDraft` 张卡在 `Content` 里的**布局总宽**：
        /// `N×412.75 + (N−1)×80 + 2(N−1)×20.37`（3 张 = **1479.73**）。</summary>
        static float WarlordRowWidth()
        {
            int n = WarlordsPerDraft;
            if (n <= 0) return 0f;
            return n * WlSlotW + (n - 1) * (WlSlotGap + 2f * WlHlgSpacing);
        }

        /// <summary>第一格**布局框**的左沿。原版 `Content` 是 `HLG align = MiddleCenter(4)` + `CSF H=MinSize`
        /// ⇒ 整行不足一屏时**居中**（3 张：`(1920 − 1479.73) / 2` = **220.135**）；超一屏时从视口左沿起排。</summary>
        static float WarlordRowStartX()
        {
            float total = WarlordRowWidth();
            return WlViewR.x1 + Mathf.Max(0f, (WlViewR.W - total) * 0.5f);
        }
        /// <summary>选将格在视口里的中心 y：`Content` 顶部 101.9 + 锚点偏移 452.52（S3 §2.4 现读）。</summary>
        const float WlSlotCy = 554.42f;
        /// <summary>`Select Button` 在选将格里的**视觉**矩形（现读 `Generic Select Card Container`：
        /// `101.4,585.5 → 311.4,667.1`；已含 1.221 的换算）—— 相对格子视觉框左上角。</summary>
        static readonly PxRect WlSelBtnOff = new PxRect(101.40f, 585.50f, 311.40f, 667.10f);
        static readonly PxRect WlSelTxOff = new PxRect(112.50f, 601.90f, 299.60f, 650.70f);
        /// <summary>卡本体（原版 `2DCard`）的**视觉**框 = `386.60 × 615.42`（`menu_dump bundle_menus_assets_all
        /// "Generic Select Card Container"` 现读那一行：`2DCard 13.1 -19.6 399.7 595.8 386.60 615.42 ⇲ls=184.7`）。
        /// 🔴 **2026-10-24 就地订正（铁律 5）**：本行原来是 `751.40` —— 那是 S3 §2.4 那句
        /// 「2DCard **386.6×615.42 × 1.221 = 472×751.4**」**把容器那 1.221 重复乘了一次**
        /// （现读的 `⇲ls=184.7` = `CardUI 151.3 × 容器 1.221`，**已经含**容器缩放）。
        /// 代价（真机截图 `_tmp_view/shots/2026-10-24_第七十三会话/23_draft_sel.png` 实测）：卡比原版大 1.221 倍，
        /// 到 **751.4** 高 ⇒ `Select Button`（y 585.5–667.1）从「压在卡体下沿之外」变成**压在战将数值行上**，
        /// 四张卡挤成一排、格间距从原版的 106 px 缩到 21 px —— 这正是用户 2026-10-24 报的「这个错了」。</summary>
        const float WlCardBodyH = 615.42f;
        /// <summary>`2DCard` 视觉框的左上角（**相对选将格视觉框的左上角**，同一次现读 `13.1, −19.6`）。
        /// ⚠️ 两格都**不是 0**：卡在格子里偏左上（因为容器那 1.221 是绕**布局框中心**缩放的，而卡是按布局框
        /// 左上角摆的）⇒ ⛔ 别拿「格心」摆卡（旧写法居中对齐 ⇒ 卡比原版低 89.0、右 45.6 px）。</summary>
        static readonly Vector2 WlCardOff = new Vector2(13.10f, -19.60f);
        /// <summary>滚动一格多少画布像素。🔴 原版那台 `ScrollRect` 的 `m_ScrollSensitivity` 查不到
        /// （组件被禁，现读只有 `h=1/v=0`），**120 是我们挑的**（与主菜单模式卡同一条口径）。</summary>
        const float WlScrollNotch = 120f;

        // ============================================================ SelectPack 页（S3 §2.5 现读）
        static readonly PxRect WarlordImgR = new PxRect(-258.20f, -17.70f, 814.20f, 1054.80f);
        static readonly PxRect SubTitleR = new PxRect(498.10f, 263.20f, 1472.70f, 326.60f);
        static readonly PxRect PackLabelR = new PxRect(698.60f, 317.60f, 916.50f, 365.50f);
        static readonly PxRect StageR = new PxRect(927.60f, 322.60f, 1145.50f, 360.50f);
        static readonly PxRect PacksMaskR = new PxRect(395f, 178.30f, 1472.70f, 837.10f);
        static readonly PxRect PacksContR = new PxRect(510.50f, 272.60f, 1456.20f, 938.20f);
        static readonly Vector4 MaskBorder = new Vector4(42f, 363f, 655f, 81f);   // 1100×701 · ppuMult 2
        const float MaskTexW = 1100f, MaskTexH = 701f;
        const float PacksContSpacing = 13f;

        // ---- `Reroll` 行（`Draft Mode Select Packs State > Reroll`；`menu_dump --depth 8 --relative` 现读）----
        //   `Reroll` 698.2,802.7 → 1337.8,931.3
        //   `Reroll Text`（'Reroll packs:' · 54.8px · Right/**Midline**）686.2,878.5 → 1018.0,940.4
        //   `Price Display Button 2 Variant`（313.63×61.88 · `Generic UI Button` = `UI_Button_Mulligan` 九宫）
        //      1033.9,878.5 → 1347.5,940.4
        //   里面 `Price Display`（279.76×57.27 · HLG icon+text · ARF 4.8848）1049.9,880.8 → 1329.7,938.0
        //      · `icon` 布局 57.27² ×1.2 ⇒ 视觉 68.73²（sprite 在 prefab 里是**空**，运行时按货币喂）
        //      · `text` 90.53×57.27 · 54px · Center/**Capline**
        //   🔴 `Reroll Text` 在原版是 `EverguildTextMeshPro`、**没挂 `Localize`**（字面量 'Reroll packs:'）
        //      ⇒ 官方本地化里也没有这条键（现扫 10,854 条，只有 `Missions/ReRollMessage`，那是别的东西）
        //      ⇒ 我们**照原版印英文**（⛔ 不自己编一条中文）。
        static readonly PxRect RerollR = new PxRect(698.20f, 802.70f, 1337.80f, 931.30f);
        static readonly PxRect RerollTxR = new PxRect(686.20f, 878.50f, 1018.00f, 940.40f);
        static readonly PxRect RerollBtnR = new PxRect(1033.90f, 878.50f, 1347.50f, 940.40f);
        static readonly PxRect RerollPriceR = new PxRect(1049.90f, 880.80f, 1329.70f, 938.00f);

        // ---- 单包卡（`Prebuilt Pack UI`，独立根 prefab；S3 §2.6）----
        const float PackW = 305.89f, PackH = 390.94f;
        static readonly PxRect PackTitleR = new PxRect(4.40f, 25.50f, 301.50f, 58.70f);
        static readonly PxRect PackSepR = new PxRect(0f, 70.40f, 305.89f, 73.30f);
        /// <summary>包内 3 张缩略卡的区域（🔴 **我们挑的布局**：原版按 `pack.GetCardLibrary()` 逐张实例化
        /// `cardInfoItem` = `UICardInfoItem` 信息行，那颗模板 prefab 的尺寸本地解不出
        /// （`cardInfoItem` 的 PathID `-5880222813801091413` 在 `GameObject/` 里 0 命中）⇒
        /// 我们改成 **3 张缩略卡面 + 卡名**（复用 `CardView`，判据 → 派单「卡区按 3 张实例化（复用现有卡面渲染件）」）。</summary>
        static readonly PxRect PackCardsArea = new PxRect(4.95f, 84f, 300.94f, 240f);
        static readonly PxRect PackNameArea = new PxRect(4.95f, 242f, 300.94f, 262f);
        static readonly PxRect PackSelR = new PxRect(41.90f, 300f, 264.00f, 349.49f);   // 222.10×49.49
        static readonly PxRect PackSelTxR = new PxRect(52.80f, 301.5f, 253.10f, 347.99f);

        // ============================================================ Ongoing 页（S3 §2.7 现读 · 已含 1.06）
        static readonly PxRect MainContR = new PxRect(118.70f, 156.60f, 1505.90f, 880.50f);
        static readonly PxRect OnWlImgR = new PxRect(-290.40f, -49.90f, 846.40f, 1087.00f);
        static readonly PxRect OnTitleR = new PxRect(673.80f, 191.50f, 1495.60f, 255.10f);
        static readonly PxRect OnVictR = new PxRect(909.20f, 327.80f, 1141.00f, 373.80f);
        static readonly PxRect OnVictNumR = new PxRect(1148.40f, 303.40f, 1297.60f, 398.10f);
        static readonly PxRect OnStagesR = new PxRect(746.60f, 408.80f, 1425.10f, 683.50f);
        static readonly PxRect OnDefeatR = new PxRect(697.90f, 742.50f, 825.40f, 798.00f);
        static readonly PxRect OnLossesR = new PxRect(825.40f, 728.10f, 1072.50f, 812.40f);
        static readonly PxRect OnResetR = new PxRect(1230.70f, 748.10f, 1417.30f, 804.20f);
        static readonly PxRect OnResetTxR = new PxRect(1240.5f, 754.5f, 1406.9f, 797.8f);
        static readonly PxRect OnBattleR = new PxRect(1455.70f, 927.70f, 1896.10f, 1048.30f);
        static readonly PxRect OnBattleTxR = new PxRect(1471.20f, 934.70f, 1880.00f, 1041.20f);
        /// <summary>`Stages Container` 的 GridLayoutGroup：cell 80×80 · spacing (23.7, 30.04) · align MiddleCenter · 2 列。</summary>
        const float StageCell = 80f, StageGapX = 23.70f, StageGapY = 30.04f;
        /// <summary>`Losses container` 的 HLG：3 格 · spacing 9.86 · 格子 67.74²（S3 §2.13 `Losses Info UI_ref`）。</summary>
        const float LossCell = 67.74f, LossGap = 9.86f;
        const int LossSlots = 3;      // 「失败 3 场结束」（S3 §2.7：Defeat Marks 模板恒 3 格）

        // ---- 奖励面板（`DraftRewardPanel`，只有 Finished 显示；S3 §2.8 现读）----
        static readonly PxRect RewardPanelR = new PxRect(1505.90f, 168.30f, 1896.10f, 880.50f);
        static readonly PxRect CrateGlowR = new PxRect(1444.50f, 245.90f, 1957.50f, 702.90f);
        static readonly PxRect CrateR = new PxRect(1505.90f, 311.90f, 1896.10f, 636.90f);
        static readonly PxRect CollectR = new PxRect(1537.60f, 627.70f, 1864.40f, 706.10f);
        static readonly PxRect CollectTxR = new PxRect(1552.2f, 628.3f, 1848.8f, 705.5f);

        // ============================================================ 右栏 `Draft Mode Deck Info Panel`（S3 §2.9 现读）
        static readonly PxRect DeckPanelR = new PxRect(1504.94f, 204.41f, 1895.07f, 916.55f);
        static readonly PxRect DeckTabsBarR = new PxRect(1542.94f, 147.71f, 1857.04f, 210.86f);
        static readonly PxRect[] DeckToggleR =
        {
            new PxRect(1542.94f, 140.01f, 1625.03f, 218.46f),
            new PxRect(1638.04f, 140.01f, 1720.13f, 218.46f),
            new PxRect(1733.14f, 140.01f, 1815.23f, 218.46f),
        };
        /// <summary>三颗 toggle 的图标（原版 `Icon` 64×64，左/上各内缩 9.1 / 7.3；第三颗 `localScale 1.1`）。
        /// <para>🔴 **2026-10-24（第七十四会话 · `W7`）就地订正（铁律 5）**：这里原来写「**前两张图本地没有**」
        /// —— **不成立**。三张图的 sprite 本地都在：
        /// `d:/2/Warpforge_tools/data/ui_extract/atlasindividual_assets_0_mainmenu/Sprite/`
        /// 的 `40K_Info_button_draft menu.png` / `40K_Cards_button_draft menu.png` 现核存在
        /// （另有一份在 `素材/Warpforge原版/图集/0_mainmenu/Sprite/`）。它们**只是没导进
        /// `Resources/Art/ui_menu/`**（`工具/import_original_art.py` 的 `MENU_IMAGES` 表里没登记这两条）。
        /// 第三张 `40K_Chat_icon_Alliance v2` **已经在** `Resources/Art/ui_menu/` 里。</para>
        /// <para>⚠️ 名字里的空格按 `CardArt.MenuUi` 的既有约定由**调用方**换成下划线
        /// （`Core/CardArt.cs` 的 `TryDeckHero` 就是 `art.Replace(' ', '_')`）—— ⛔ 别改 `MenuUi` 那条约定本身。
        /// ⇒ 这两张图**导进 `Resources/` 之后不用改代码**就会自动出现；取不到时照旧**出声**、⛔ 不拿别的图顶。</para></summary>
        static readonly string[] DeckToggleIconArt =
        {
            "40K_Info_button_draft_menu", "40K_Cards_button_draft_menu", "40K_Chat_icon_Alliance_v2",
        };
        static readonly PxRect DeckTitleR = new PxRect(1542.40f, 256.17f, 1857.60f, 306.17f);
        static readonly PxRect DeckCounterR = new PxRect(1565.40f, 821.09f, 1834.60f, 871.09f);
        static readonly PxRect DeckEnergyR = new PxRect(1537.95f, 540.42f, 1862.05f, 590.42f);
        /// <summary>`Card List` 页的行区（🔴 **我们挑的**：原版 `DrafModeDeckInfoCardListPanel` 是
        /// `DeckListDrawer` + `Scroll View`，格子模板在另一份 prefab 里 ⇒ 我们按行排）.</summary>
        static readonly PxRect DeckListR = new PxRect(1542f, 330f, 1858f, 800f);
        const float DeckRowH = 38f;

        // ---- `Card List` 页的头部（原版 `Cards in deck panel` 那三个节点，`menu_dump --depth 4 --relative` 现读）----
        //   `Warlord Name` 261.85×41.49 · 33px · **Left/Middle** · `EverguildButton`（点了开卡片详情）
        //   `Card counter` 78.86×41.49 · 28px · **Right/Middle** · 色 (0.783,0.783,0.783) · prefab 默认文本 `30/30`
        //   `Separator`    338.40×2.90 · `40k_Separator Fade Sides Horizontal`（九宫 63,0,63,0 · ppuMul 2.89）
        //   `Scroll View`  330.97×588.88（行区从它开始）
        //   🔴 三者的**相对次序**照 prefab（名字左 / 计数右 / 分隔线在两者之下 / 列表在分隔线之下）；
        //   ⚠️ **绝对 y 不照抄**：原版那个容器（415.47×785.72）的落点与我们的 `DeckListR` 不同
        //   （`DeckListR` 是我们挑的）⇒ 头部贴**我们那个框的顶**，⛔ 照抄 prefab 的绝对 y 会压到行上。
        const float DeckListHeaderH = 41.49f;
        const float DeckListBodyTop = 58f;      // = 头部 41.49 + 分隔线 ~2.9 + 缝，行区从这里起
        /// <summary>`Card counter` 的文本（原版
        /// `DrafModeDeckInfoCardListPanel.Initialize`：`deck.CardCount + "/" + gameplayVariables.deckSize`）。
        /// ⚠️ 分母与右栏 `CardCounter` 是**同一个口径**（`DeckSizeForDraft`），⛔ 别写第二份。</summary>
        static string DeckListCounterString(int cards) { return cards + "/" + DeckSizeForDraft; }

        // ============================================================ 右栏 Deck Info 页的「内容块」
        //  `menu_dump bundle_menus_assets_all "Draft Mode Deck Info Panel" --depth 12 --relative` 现读。
        //  平移量 = `DeckPanelR` 的左上角 (1504.94, 204.41)（与 `DeckToggleR[0].x1/y1` 对齐验证过）。
        // ---- 卡背 + 战将卡面 ----
        /// <summary>`Cardback`（原版 156.70×226.08，`↻1.60°`）与它的**子件** `Cardback Front`（156.26×226.41，`↻-3.43°`）。
        /// 🔴 两件都带 z 旋转 —— **已按原版角度画**（`MenuDraw.Rect` 的 `rotZDeg`，2026-10-24 `W18`）：
        /// 值取自 prefab 的 `m_LocalRotation`（`menu_dump … "Draft Mode Deck Info Panel" --depth 12 --relative` 现读
        /// 表末「↻ 2 处带 `m_LocalRotation`」那一块），两件的 `m_Pivot` 都是 **(0.5,0.5)** ⇒ 绕框心转。
        /// 🔴 **子件关系照 prefab**：树里 `Cardback Front` 的深度是 **4**（父 = `Cardback`；锚点 stretch、
        /// `sizeDelta ≈ (−0.44, 0.33)`）⇒ 两级角度**叠加**，画出来的世界角 = **−1.83°**。
        /// ⚠️ 这两个矩形都是**未旋转帧**的布局框（旋转由 `rotZDeg` 施加），⛔ 别改成「旋转后的外接框」；
        /// `DeckBackFrontR` 是**父件未旋转局部帧**里的框（= `menu_dump --relative` 印的那个值，工具明说
        /// 「落在旋转件下面的节点没按父级旋转重算」），⛔ 别再自己去乘一个 +1.60°。</summary>
        static readonly PxRect DeckBackR = new PxRect(1614.54f, 306.91f, 1771.24f, 532.91f);
        static readonly PxRect DeckBackFrontR = new PxRect(1625.94f, 301.81f, 1782.14f, 528.21f);
        /// <summary>两件卡背的 z 旋转（度）—— **prefab `m_LocalRotation` 实读**，⛔ 不是我们挑的。
        /// `Cardback Front` 这一份是**局部**角（父件那 +1.60° 由父子关系叠上去）。</summary>
        const float DeckBackRotZ = 1.60f;
        const float DeckBackFrontRotZ = -3.43f;
        /// <summary>战将卡（原版 `CardUI-Warlord`：布局 2.54×3.37 × `localScale 75.67`
        /// ⇒ **视觉 192.48×254.89**；中心现读 = (1697.34, 420.31)）。</summary>
        static readonly PxRect DeckWarlordCardR = new PxRect(1601.10f, 292.87f, 1793.58f, 547.75f);

        // ---- `Deck Information cost drawer`（9 行费用分布）----
        /// <summary>底板（原版 `Background` 179.12×225.46 · 无图 · 色 (0.123,0.123,0.123,1)）。</summary>
        static readonly PxRect DeckCostBgR = new PxRect(1607.84f, 586.81f, 1786.94f, 812.31f);
        /// <summary>第 0 行 `Deck CostQuanityt Row Drawer` 的矩形（253.24×21.42）；
        /// 后面每行 **+25.30**（`Content` 是 VLG · `spacing 3.43`，现读逐行 y 间距一致）。</summary>
        static readonly PxRect DeckCostRow0R = new PxRect(1570.74f, 586.81f, 1823.98f, 608.23f);
        const float DeckCostRowStep = 25.30f;
        /// <summary>行内三件 —— **相对行矩形左上角**的偏移（现读 `69.6−65.8 / 106.7−65.8 / 285.8−65.8`）。</summary>
        static readonly PxRect DeckCostCostOff = new PxRect(3.80f, -0.60f, 32.80f, 22.00f);
        static readonly PxRect DeckCostSliderOff = new PxRect(40.90f, -0.60f, 212.30f, 22.00f);
        static readonly PxRect DeckCostCountOff = new PxRect(220.00f, -0.60f, 248.90f, 22.00f);
        /// <summary>`DeckEnergyCostDrawer.NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`（`dump.cs:105906` 现读）
        /// ⇒ 滑块 fill = `clamp(张数 / 10, 0, 1)`。
        /// 分桶规则（`d:/2/tools/decomp_full/DeckEnergyCostDrawer__Initialize.c` 反汇编现读）：
        /// `card.cost &lt; 行数−1` ⇒ 落第 `cost` 行；**否则全进最后一行**（所以最后一行是「8+」）。</summary>
        const int DeckCostRows = 9;
        const float DeckCostNormalization = 10f;

        // ============================================================ 卡包预览（原版 `Generic Multi Card Display`）
        // 🔴 几何**全部**来自 prefab 实读（`bundle_menus_assets_all`；根 RT = `RectTransform_-5481408756880803131.json`
        //    的 `anchor(0,0.5)-(1,0.5) · apos(0,-20.475) · size(0,818.04) · pivot(0.5,0.5)`）：
        //    设计帧 1920×1080 ⇒ 根 = x[0,1920] · 竖直中心 540−20.475 = 519.525（左下原点）
        //    ⇒ 左上原点 y[151.455, 969.495]。下列每个矩形都是**该根之下**的窗口坐标（= menu_dump 的
        //    `--relative` 那套，已与根矩形相加过一次，⛔ 别再减一次）。
        static readonly PxRect PrevRootR = new PxRect(0f, 151.46f, 1920f, 969.50f);
        /// <summary>`Menu Dark Background`：锚点 (0.5,0.5) · size 4574.6×2572.36 · apos (0,0)
        /// ⇒ 以预览根中心 (960, 560.475) 为中心铺开；色 `(0,0,0,0.773)`（`m_Color` 实读同款）。</summary>
        static readonly PxRect PrevShadeR = new PxRect(-1327.30f, -725.68f, 3247.30f, 1846.63f);
        /// <summary>`Header Text`：锚点 (0.5,1) · pivot (0.5,1) · size 1192.37×63.20
        /// ⇒ x [363.82,1556.19] · 贴着预览根上沿。字号 38 / 基准 31.9 / 自动 18–38 / Center-Middle。
        /// 🔴 **运行期是隐藏的**：`PackPreview` 给 `Initialize(cards, headerTitle)` 传的 `headerTitle = null`
        /// （`DraftModeSelectPacksStateDemo__PackPreview.c`：`UIMultiCardDisplay__Initialize(lVar2, uVar1, 0, 0)`），
        /// 而 `Initialize` 末句 `SetActive(!IsNullOrEmpty(headerTitle))` ⇒ 原版这一刻**不显示标题**。</summary>
        static readonly PxRect PrevHeaderR = new PxRect(363.82f, 151.46f, 1556.19f, 214.66f);
        /// <summary>三张卡的**视觉**尺寸（`CardUI Reference` 布局框 2.54×3.37 × `m_LocalScale 223.1`）。</summary>
        const float PrevCardW = 567.60f, PrevCardH = 751.67f;
        /// <summary>卡心的 y（= 预览根竖直中心 560.475，`Content` 的 HLG `childAlignment = MiddleCenter`）。</summary>
        const float PrevCardCy = 560.48f;
        /// <summary>`Content` 的中心 x：根中心 960 + `apos.x −8` = **952**。</summary>
        const float PrevContentCx = 952f;
        /// <summary>`CardUI Reference` 的**布局框宽**（`m_SizeDelta.x = 2.54` —— 卡体不参与布局，
        /// 画出来的是 `localScale` 放大的那 567.6；HLG 只按布局框排）。</summary>
        const float PrevSlotBoxW = 2.54f;
        /// <summary>`Content` 上那颗 `HorizontalLayoutGroup.m_Spacing = 500`（实读）。</summary>
        const float PrevSlotSpacing = 500f;
        /// <summary>三张卡的中心 x（**由上面三个数现算**，不另存一份）：
        /// 总宽 = 3×2.54 + 2×500 = 1007.62；左沿 = 952 − 503.81 = 448.19
        /// ⇒ 中心 = 448.19 + 1.27 + i×502.54 = **449.46 / 952.00 / 1454.54**。</summary>
        public static float PackPreviewCardCx(int i)
        {
            return PrevContentCx - (3f * PrevSlotBoxW + 2f * PrevSlotSpacing) * 0.5f
                   + PrevSlotBoxW * 0.5f + i * (PrevSlotBoxW + PrevSlotSpacing);
        }
        /// <summary>`Close button`（实读 185.8,825.3 → 250.0,888.5 = 64.24×63.24；图 `40k_UI_bt_back` 88×87）
        /// 与它右边那颗 `Text`（'Back'，305.33×63.24，左对齐）。⚠️ 两颗都在预览根**下沿之外**
        /// （根到 y 969.5 为止，按钮在 825.3–888.5 ⇒ 其实在根里面靠下）—— 位置照实读，⛔ 别按「贴底」重推。</summary>
        static readonly PxRect PrevBackBtnR = new PxRect(185.80f, 825.30f, 250.00f, 888.50f);
        static readonly PxRect PrevBackTxR = new PxRect(259.10f, 825.30f, 564.40f, 888.50f);

        // ============================================================ 状态
        /// <summary>当前页（原版 `DraftModeState`）。</summary>
        public DraftModeState State { get; private set; }
        /// <summary>本窗最近打开的那一扇（自检用，照 `LiveOpsEventWindow.LastOpened` 的形状）。</summary>
        public static DraftModeWindow LastOpened { get; private set; }

        /// <summary>🔴 **一局的存档**（原版 `DraftEventSaveGame`，在服务端；我们放**静态**）。
        /// 它必须活过「窗被销毁 + 进对局 + 回壳」⇒ ⛔ 不能是实例字段（窗一关就没了，那正是
        /// 「胜负场恒 0」的成因）。`null` = 没有进行中的一局。见 <see cref="DraftRunSave"/> 的注释。</summary>
        static DraftRunSave _run;

        /// <summary>自检/入口读这一局存档（只读口 —— ⛔ 别在生产路径拿它算账，统一走 `UpdateState`）。</summary>
        public static DraftRunSave RunForTest { get { return _run; } }
        /// <summary>自检：直接喂一份存档（`null` = 清掉）。⚠️ 用完请调 <see cref="ClearRunForTest"/>。</summary>
        public static void SetRunForTest(DraftRunSave run) { _run = run; }
        /// <summary>自检收尾：把静态存档清干净（否则下一次真 Play / 下一个断开会**接着自检那一局**）。</summary>
        public static void ClearRunForTest() { _run = null; }

        int _seed;
        /// <summary>本局种子（**日志里会打**，照它重开就是同一串包）。</summary>
        public int Seed { get { return _seed; } }
        /// <summary>自检/入口想钉种子时用；`0` = 每局现抽。</summary>
        public int SeedOverride;

        readonly List<string> _pickedKeys = new List<string>();
        readonly List<string> _selectedCards = new List<string>();
        List<DraftPacks.PackDto> _offered = new List<DraftPacks.PackDto>();
        List<CardDef> _warlords = new List<CardDef>();
        string _warlordId;
        DraftPacks.ClosedDeck _deck;
        /// <summary>右栏三颗 toggle 的选中项：0 = Deck Info · 1 = Card List · 2 = Alliance Info（互斥）。
        /// 🔴 **开窗默认 0（Deck Info）** —— ⚠️ 原版 prefab 序列化的是 `Deck Info isOn=0 / Card List isOn=1 /
        /// Alliance isOn=0`，而同族的 `Initialize` 又把三个内容页全 `SetActive(false)`、**只留 Energy 页**（S3 §2.9）
        /// ⇒ 原版那一刻「亮的 toggle」与「显示的内容页」**不一致**（序列化残留）。
        /// 我们取「亮的那个 = 显示的那个」（0），**这一条是我们挑的**（照原版那对矛盾没有意义）。</summary>
        int _deckTab;

        /// <summary>已经选过的包（`key`，按选包顺序 ⇒ 顺序就是轮次）。</summary>
        public IList<string> PickedPackKeys { get { return _pickedKeys; } }
        /// <summary>已经拿到手的卡（每选 1 包 +3；原版 `DraftEvent.selectedCards`）。</summary>
        public IList<string> SelectedCards { get { return _selectedCards; } }
        /// <summary>`CurrentDraftRound` = 已选包数（**0 起**，原版同义：`selectedPacks.Count`）。</summary>
        public int CurrentDraftRound { get { return _pickedKeys.Count; } }
        /// <summary>这一轮「一屏给出的包」—— **就是 `DraftPacks.RollRound` 的返回**（⛔ 不是写死 3）。</summary>
        public IList<DraftPacks.PackDto> OfferedPacks { get { return _offered; } }
        /// <summary>当前选中的战将（卡 id；空 = 还没选）。</summary>
        public string WarlordId { get { return _warlordId; } }
        /// <summary>这一轮可选战将（`WarlordsPerDraft` 张，按 seed 确定性抽）。</summary>
        public IList<CardDef> Warlords { get { return _warlords; } }
        /// <summary>`To Battle` 真的把这一局交出去了没有（批处理下不切场景，只记账）。</summary>
        public bool StartedBattle { get; private set; }
        /// <summary>组出来的 ClosedDeck（`LaunchBattle` 那一刻才有）。</summary>
        public DraftPacks.ClosedDeck ClosedDeckBuilt { get { return _deck; } }

        /// <summary>取不到的图（自检读口 —— 非空就是「有件根本没画」；照 `LiveOpsEventWindow.MissingArt`）。</summary>
        readonly List<string> _missingArt = new List<string>();
        public List<string> MissingArt { get { return _missingArt; } }

        // 页面与常驻件
        Transform _pagePay, _pageWarlord, _pagePacks, _pageOngoing;
        Transform _packsContainer, _warlordViewport, _warlordContent, _deckTabHolder;
        Transform _deckPanelRoot, _rewardPanelRoot, _packHint, _warlordHint;
        /// <summary>右栏 `CardCounter` 这一页要不要显示（原版 `Initialize(draft, showCardCounter)` 的第 2 参：
        /// SelectPack 页 `true` / Ongoing 页 `false`）。由 `GoToState` 每页写。</summary>
        bool _deckShowCounter = true;
        /// <summary>右栏 Deck Info 页的内容块（原版 `Energy View panel`）：卡背 + 战将卡面 + 9 行费用分布。
        /// 只在这一页（`_deckTab == 0`）显示 —— 原版 `Initialize` 也是「三页只留 Energy 页」。</summary>
        Transform _deckInfoHolder;
        Label _stageCounter, _cardsCounter, _victoriesNum, _deckTitle;
        /// <summary>右栏「Energy Balance」那行标题（原版 `Energy View panel > Energy balance` ——
        /// 它**属于 Deck Info 页**，切到 Card List / Alliance 页时要跟着关）。</summary>
        Label _deckEnergyLabel;
        /// <summary>`Reroll` 那颗价格钮上的字（原版 `Price Display > text`）。
        /// 免费档原版画的是「Free」（`PriceDisplay.SetupFree`），见 `ReRollCost`。</summary>
        Label _rerollPrice;
        /// <summary>`Card List` 页那颗 `Card counter`（`n/12`；每次重刷重建）。</summary>
        Label _deckListCounter;
        MenuScroll _warlordScroll;
        readonly List<Transform> _warlordNodes = new List<Transform>();
        /// <summary>选将页每格的卡视图（与 `_warlords` 同序；`null` = 那一格没建出来）。
        /// 🆕 2026-10-24（U1）：选中要改**高亮**，⛔ 不重建整张卡（重建一次 ~13 ms × 5）。</summary>
        readonly List<CardView> _warlordViews = new List<CardView>();
        /// <summary>选将页每格的 `Select Button` 命中区（`WindowButton`）—— 选中后那一格要 `Interactable = false`
        /// （原版 `GenericSelectCardContainer.ToggleSelected` 的 `interactable = !selected`）。</summary>
        readonly List<WindowButton> _warlordSelBtns = new List<WindowButton>();
        /// <summary>选将页每格**卡本体**的命中区（`CardBodyHit`）。
        /// 🔴 它是**吸收层**（`MenuDraw.Absorb`，**无 `onClick`**）—— 原版 `2DCard/UI Collider` 上没有任何处理器
        /// ⇒ 点卡体**什么都不做**（不选中、不换页、不关窗）。判据与代码里那段长注释见 `RebuildWarlordCards`。
        /// 自检要量它在不在、队列多少、以及「那一点真正吃到的是不是它」。</summary>
        readonly List<WindowButton> _warlordBodyHits = new List<WindowButton>();
        /// <summary>选将页 `Continue` 的 `WindowButton`（出厂 `Interactable = false` = 原版 `OnOpen` 那一句）。</summary>
        WindowButton _warlordContBtn;
        readonly List<CardView> _packCardViews = new List<CardView>();
        // 🆕 胜/败场格的宿主（`GoToState` 每次进 Ongoing 页都要按**真战绩**重画 ⇒ 得留引用）
        Transform _winMarksHolder, _lossMarksHolder;
        // 🆕 卡包预览层（原版 `Generic Multi Card Display`）
        Transform _previewRoot, _previewCards;
        Label _previewHeader;
        readonly List<CardView> _previewCardViews = new List<CardView>();
        /// <summary>🔴 预览里每张卡的**引擎 id**（`CardDef.Id`，即 `pack.cardIds[i]`）。
        /// ⚠️ 必须单独记：`CardView.Data.id` 存的是**显示名**（`BattleDriver.ToCardData` 里 `id = c.Name`），
        /// 拿它当卡身份会读成 `Autarch` 而不是 `ASH83`（2026-10-24 第七十三会话 E12 修）。
        /// 索引与 `_previewCardViews` **逐位对齐**（两边都在 `if (cv == null) continue;` 之后 `Add`）。</summary>
        readonly List<string> _previewCardEngineIds = new List<string>();
        /// <summary>预览里每张卡**建的时候原样喂给 `MenuDraw.Node` / `CardView.SetPose`** 的那份矩形
        /// （自检读口用；⛔ 这不是第二份摆位逻辑 —— 它记的就是画出来的那个框）。</summary>
        readonly List<PxRect> _previewCardRects = new List<PxRect>();

        // ============================================================ 取图（取不到**必须出声**）
        /// <summary>按**已归一的名字**取 `Art/ui*/` 下的图；取不到就记进 `MissingArt` 并让界面上留空。</summary>
        /// <remarks>🔴 **2026-10-24（`W23`）就地订正（铁律 5）—— 本窗有 5 张图【一直画不出来】**：
        /// <see cref="CardArt.MenuUi"/> **不做**「空格→下划线」归一（`Core/CardArt.cs:802-813`，规矩是
        /// **调用方换**），而本文件原来有 6 处传的是**带空格的原版切片名** ⇒ `Resources.Load` 落空
        /// ⇒ 图上不屏、`MissingArt` 里记一笔（真机症状：轮抽窗少了红底 / Noise / 宝箱金框、
        /// 包卡少了橙描边与分隔线）。今天全部换成下划线名（文件都在 `Resources/Art/ui_menu/`）：
        /// `40k_general_popup_simple_red` · `UI_Dirt_And_Noise_skratches` · `Crate_Border_Highlight` ·
        /// `OctagonUI_Border_SDF` · `40k_Separator_Fade_Sides_Horizontal`（两处）。
        /// ⛔ **别去改 `MenuUi` 的约定** —— 那会影响全工程（`CardArt` 那条 doc 已经写明分工）。
        /// 断言侧由 `Editor/DraftWindowAssertD2.cs` 的 `A7_MissingArt` 钉成 `miss.Count == 0`。</remarks>
        Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !_missingArt.Contains(name)) _missingArt.Add(name);
            return t;
        }

        // ============================================================ 建窗
        /// <param name="seed">本局种子。`0` = 每局现抽（生产路径）；自检传定值以便复现（同 `SeedOverride`）。</param>
        public static DraftModeWindow Create(WindowsManager mgr, int seed = 0)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<DraftModeWindow>();
            win.SeedOverride = seed;
            // ---- 窗参（逐字段实读根 MB `MonoBehaviour_-7910505541983962427.json`）----
            win.type = WindowType.Popup;                       // `type = 1`
            win.placement = WindowsPlacement.Popup;            // `windowsPlacement = 15`
            win.closeOnEsc = true;                             // `closeOnESC = 1`
            win.updateNavPanel = false;                        // `updateNavPanel = 0`
            win.extraScaleSmallScreen = 1f;                    // `extraScaleSmallScreen = 1.0`（= 不覆盖）
            // `openSound` / `closeSound` 实读都是空 PPtr、`useDefaultCloseSoundIfNull = 1`
            // ⇒ 一个 clip 都不赋（关窗音走基类默认 `ButtonClick Back`）。
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Draft] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            MainMenuRuntime.SetNavPanelVisible(false, true);   // 整屏红底窗 ⇒ 收起左竖导航（同 `RankedEventWindow.Open`）
            int seed = SeedOverride != 0 ? SeedOverride : NewSeed();
            // 🔴 **原版 `DraftModeMenuDemo.Open` → `GetStartingTab` → `ChangeTabCO(GetTabFromState(DraftState))`**
            //    （`decomp_full/DraftModeMenuDemo__Open.c` / `__GoToCurrentState.c`）——
            //    窗**不会**因为「又点了一次模式卡」就重置这一局：进度在服务端的 `DraftEventSaveGame` 里。
            //    本地那份就在 `_run`（见它的注释）⇒ 有存档就**接着打**，没有才开新局。
            //    ⚠️ 已领奖（`rewardsCollected`）的那一局不再续（原版 `ResetCollectedDraft` 会把它清掉）。
            if (SeedOverride != 0)
            {
                // 自检/入口**钉死了种子** ⇒ 一定开新局（否则同一份断言跑第二次会「接着上一局」）。
                BeginRun(seed);
            }
            else if (_run != null && !_run.rewardsCollected)
            {
                ResumeRun();
            }
            else
            {
                BeginRun(seed);
            }
        }

        static int NewSeed() { return unchecked((int)(DateTime.Now.Ticks & 0x7FFFFFFF)); }

        public override void Close()
        {
            MainMenuRuntime.SetNavPanelVisible(true, true);
            base.Close();
        }

        /// <summary>ESC 的**第一层**是预览（原版预览是**另一扇窗** `Generic Multi Card Display`，
        /// 它的窗参 `closeOnESC = 1`，`MonoBehaviour_1014665548285115077.json`）⇒ 预览开着时按一次只关预览，
        /// 再按一次才轮到轮抽窗自己。
        /// 🔴 **为什么覆写这一支而不是 `Close()`**：`Close()` 也会被 `WindowsManager.CloseAllWindows()` 等
        /// 生产路径调用，在那里「先关预览、不关窗」会把壳卡住；ESC 才是「逐层退」的那一条。</summary>
        public override bool ESCPressed()
        {
            // 第一道门槛与基类同一个（原版预览窗也有那一道：`EventSystemController.eventSystem.enabled`）
            if (PackPreviewVisible && PointerLayer.InputEnabled)
            {
                Debug.Log("[Draft] ESC ⇒ **关预览层**（原版那是另一扇窗、`closeOnESC = 1`；再按一次才关轮抽窗）");
                ClosePackPreview();
                return true;
            }
            return base.ESCPressed();
        }

        /// <summary>开一局新的轮抽（**重置全部状态**并落到 Pay 页）。`Open()` / 「放弃本轮」都走它。</summary>
        public void BeginRun(int seed)
        {
            _seed = seed;
            _pickedKeys.Clear();
            _selectedCards.Clear();
            _offered = new List<DraftPacks.PackDto>();
            _warlordId = null;
            _deck = null;
            _deckTab = 0;
            StartedBattle = false;
            // 🔴 新的一局 ⇒ 新的 `DraftEventSaveGame`（原版 `FreeEventEntrance` 成功那一支才建）。
            //    ⚠️ `consumedDraftMatches` 的**基线**必须在这里记：否则上一局（甚至别的存档）打过的
            //    轮抽对局会被算进这一局（= 开局就白送几个胜场）。
            _run = new DraftRunSave
            {
                joinState = (int)DraftJoinState.NotJoined,
                payState = (int)DraftPayState.NotPaid,
                matchesWon = 0, matchesLost = 0, rewardsCollected = false,
                seed = seed, warlordId = null, reRolls = 0,
                pickedKeys = new string[0], selectedCards = new string[0],
                consumedDraftMatches = CountDraftMatchesIn(BattleLogData.All),
            };
            _warlords = RollWarlords(_seed, WarlordsPerDraft);
            Build();
            GoToState(DraftModeState.PayScreen);
            Debug.Log("[Draft] 开一局（种子 " + _seed + "，战将候选 " + _warlords.Count + " 张；"
                    + "`draftRounds` = " + DraftPacks.RoundsPerDraft + " 轮 × `packsPerDraft` = "
                    + DraftPacks.PacksPerRoundOffered + " 包 —— 🔴 这三个数是**我们挑的**，原版真值在服务端（S3 §五））"
                    + "；战绩基线：对局记录里已有 " + _run.consumedDraftMatches + " 条轮抽对局（⛔ 不算进本局）");
            Debug.Log("[Draft] 本轮**不建**这两件（原版有、但判据缺失）："
                    + "① `Premium` / `10x Premium` 两档（价格 `premiumPrice`/`premium10Price` 在服务端 ⇒ 不编价格）；"
                    + "② `Timer`（活动结束时刻在服务端 ⇒ 不建假倒计时）。"
                    + "⚠️ `Reroll` 那一行**建了**，但价格表 `reRollCosts` 也在服务端、本地 0 命中 ⇒ 表空 ⇒ 恒「免费」"
                    + "（原版 `PriceDisplay.SetupFree` 同观感）；原版那一下的服务端扣费本地不做。");
        }

        /// <summary>接着打**存档里那一局**（原版 `Open → GoToCurrentState`：按服务端的 `DraftState` 落页）。
        /// 顺序要紧：先按存档把状态装回来 → `Build()` → **先吃战绩**（原版 `OnMatchFinished` 早就在服务端
        /// 把计数写好了）→ 再 `UpdateState()` 决定落哪一页。</summary>
        public void ResumeRun()
        {
            if (_run == null) { Debug.LogWarning("[Draft] `ResumeRun()` 没有存档 ⇒ 忽略"); return; }
            _seed = _run.seed;
            _warlordId = _run.warlordId;
            _pickedKeys.Clear();
            if (_run.pickedKeys != null) _pickedKeys.AddRange(_run.pickedKeys);
            _selectedCards.Clear();
            if (_run.selectedCards != null) _selectedCards.AddRange(_run.selectedCards);
            _offered = new List<DraftPacks.PackDto>();
            _deck = null;
            _deckTab = 0;
            StartedBattle = false;
            _warlords = RollWarlords(_seed, WarlordsPerDraft);
            Build();
            // 原版 `Open` 的落页走的是 **`GetTabFromState(DraftState)`**（存着的那个状态），
            // 不是 `UpdateState` 现算 ⇒ 先处理「还没报名」这一档（那一局连 Pay 页都没过）。
            if (_run.joinState == (int)DraftJoinState.NotJoined)
            {
                Debug.Log("[Draft] 接着上一局（种子 " + _seed + "）：**还没报名**（`joinState = NotJoined`）"
                        + " ⇒ 落 Pay 页（原版 `GetStartingTab` / `GetTabFromState`）");
                GoToState(DraftModeState.PayScreen);
                return;
            }
            int pending = IngestBattleResults();
            Debug.Log("[Draft] 接着上一局（种子 " + _seed + " · 战将 " + (_warlordId ?? "<未选>") + " · "
                    + _pickedKeys.Count + "/" + DraftPacks.RoundsPerDraft + " 包 · 战绩 "
                    + Victories + " 胜 " + Defeats + " 负"
                    + (pending > 0 ? "；本次补吃了 " + pending + " 场刚打完的轮抽对局" : "；没有新的轮抽对局")
                    + "）");
            UpdateState();      // 原版 `GoToCurrentState` —— 由**状态**决定落哪一页
        }

        void Build()
        {
            // 🔴 首句撤旧滚动区（本方法会清子件，旧条目不清 = 每重建一次净涨 1 条；
            //    判据 → `Shell/LiveOpsEventWindow.Build()` 首段那三条理由）
            PointerLayer.UnregisterOwnedBy(gameObject);
            _missingArt.Clear();
            _warlordNodes.Clear();
            _warlordViews.Clear();
            _warlordSelBtns.Clear();
            _warlordBodyHits.Clear();
            _warlordContBtn = null;
            _rerollPrice = null;
            _deckInfoHolder = null; _deckEnergyLabel = null;
            _packCardViews.Clear();
            _previewCardViews.Clear();
            _previewCardEngineIds.Clear();
            _previewCardRects.Clear();
            _previewRoot = null; _previewCards = null; _previewHeader = null;
            _warlordScroll = null;
            MenuDraw.ClearChildren(transform);

            var root = transform;
            BuildBackdrop(root);
            BuildHeader(root);
            // `Select Pack Anim Anchor (It must be out of Select Pack State)`：原版 sz=100×100 ·
            // anchor/pivot (0.5,0.5) · anchoredPosition (0,0) ⇒ **屏幕正中**（离屏？不 —— 名字里的
            // "out of Select Pack State" 说的是「它是状态页的**兄弟**、不在页里」，不是「在屏外」）。
            // ⚠️ 原版的选包飞出动画落点；我们**不做飞行动画**（时长常量只在 `DAT_*` 里，S3 §五·3）
            // ⇒ 节点照建（结构与断言认得到），位置照原版。
            MenuDraw.Node(root, "Select Pack Anim Anchor (It must be out of Select Pack State)",
                          new PxRect(910f, 490f, 1010f, 590f));

            _pagePay = BuildPayPage(root);
            _pageWarlord = BuildWarlordPage(root);
            _pagePacks = BuildPacksPage(root);
            _pageOngoing = BuildOngoingPage(root);
            _deckPanelRoot = BuildDeckInfoPanel(root);
            // 🆕 (A) 卡包预览层（原版 `Generic Multi Card Display`）—— 建好即**隐藏**（原版 `UIMultiCardDisplay.Awake`
            //    第一句就是 `referenceCard.gameObject.SetActive(false)`/整窗关着；我们的整层默认关）。
            _previewRoot = BuildPackPreview(root);
        }

        // ------------------------------------------------------------ 背景（S3 §2.1 现读的四层）
        void BuildBackdrop(Transform root)
        {
            var gen = MenuDraw.Node(root, "General Red Background", new PxRect(0f, 0f, 1920f, 1080f));
            // ① `Menu Dark Background`（纯黑 0.773）+ 命中区（= 原版 `BackgroundCloseButton`，点它关窗）
            MenuDraw.Rect(gen, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, new Color(0f, 0f, 0f, 0.773f));
            MenuDraw.ShadeHit(gen, new PxRect(0f, 0f, 1920f, 1080f), QShade, QHitContentMin,
                              () => Close(), "BackdropHit");
            // ② `Reward Background Get Reward`（红底九宫）+ 吸收层（原版点红底什么都不做）
            MenuDraw.Nine(gen, Tex("40k_general_popup_simple_red"), RedR, RedBorder, RedTexW, RedTexH,
                          QBg1, null, true, "Reward Background Get Reward");
            // 🔴 **2026-10-24（真机 F1）就地改：窗内吸收层降一档（`QHit − 2`）** —— 原来它算出来是
            //   `QHitContentMin − 1` = **4169**，而选将页那张卡的 `CardBodyHit` 也是 4169
            //   （`Absorb` 的 `−1` 算法）⇒ **同队列 ⇒ 谁吃到退化成枚举顺序**，真机 `click_log`
            //   2026-10-24 #37/#39 就是「点在卡上、吃到的是 `AbsorbHit`」。
            //   现在梯子 = 压暗 4130 < **窗内吸收 4168** < **卡体 4169** < 内容钮 4170（`Select Button` 等）。
            //   ⚠️ `Absorb` 的档 = 第三个实参 `−1` ⇒ 这里传 `QHitContentMin − 1` 才得到 4168。
            MenuDraw.Absorb(gen, "AbsorbHit", RedR, QShade, QHitContentMin - 1);
            // ③ `Noise`（Tiled · 原版 tint (0.311,0.127,0,0.718)）
            MenuDraw.Tiled(gen, Tex("UI_Dirt_And_Noise_skratches"), RedR, NoiseTilePx, QBg2, "Noise",
                           new Color(0.311f, 0.127f, 0f, 0.718f));
            // ④ `Menu Vignette`（纯黑 0.58）
            MenuDraw.Rect(gen, CardArt.Solid(), VigR, "Menu Vignette", QBg3, new Color(0f, 0f, 0f, 0.58f));
        }

        void BuildHeader(Transform root)
        {
            float titleL = HdrL + 155f;                      // 原版 HLG padLeft 155
            WindowHeader.WithBackButton(root, new WindowHeader.Spec
            {
                RootName = WindowHeader.RootNameGameMode,
                RootRect = new PxRect(HdrL, HdrT, HdrR, HdrB),
                PlateRect = new PxRect(HdrL, WingT, PlateR, WingB),
                TitleRect = new PxRect(titleL, HdrT + 16.36f, titleL + 369.36f, HdrT + 99.01f),
                // 标题文案 = 原版 `WindowHeaderWithBackButton.Initialize` 换上的本模式名
                // （`Demo/DraftMode/Title` = `Draft Mode` / `选秀模式`；官方本地化现读）
                TitleText = Loc.T("Demo/DraftMode/Title"),
                TitleMode = WindowHeader.TitleFit.FitAfterSpacing,
                TitleFitW = 369.36f, TitleFitH = 82.65f,
                TitleVAlign = Label.VAlign.Capline,          // 本 prefab 那颗 `Window Title` = 8192 Capline
                WingRect = new PxRect(WingL, WingT, WingR, WingB),
                BackRect = new PxRect(BackL, BackT, BackR, BackB),
                BackButtonStyle = WindowHeader.BackStyle.QuadOnHeader,
                BackKeepAspect = true,
                BackSwapArt = "UI_Button_Menu_Back",
                PlateTex = Tex("WF_Campaign_Info_Background"),
                BackTex = Tex("UI_Button_Menu_Back"),
                QPlate = QArt, QWing = QArt1, QTitle = QText, QBack = QArt1, QHit = QHit,
                OnBack = () => Close(),
            });
        }

        // ------------------------------------------------------------ Pay 页（只留免费档）
        Transform BuildPayPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Pay State", new PxRect(0f, 0f, 1920f, 1080f));
            // 原版树：`Pay State > Scale menu > {Title, Game mode instructions, Description 1, ButtonsLayout}`
            //    （`Scale menu` 自己挂 `TransformScalerBySmallScreenUI`，序列化 1920×1082 @ (272,33)）
            //    ⇒ 三颗文本必须挂在它底下（节点名逐字照原版；`MenuDraw` 用绝对坐标，多插这一层位置不变）。
            var bar = MenuDraw.Node(page, "Scale menu", new PxRect(272f, -34f, 2192f, 1048f));
            // 标题 / 说明 / 描述（`Localize` 三颗，官方词条现读）
            MenuDraw.Text(bar, PayTitleR, Loc.T("Demo/DraftMode/TitleIntroScreen"), Color.white,
                          "Game Mode Title", 37.30f, QText, PayTitleR.W, 25f, 72f, 47.73f,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Text(bar, PayHelpR, Loc.T("Demo/DraftMode/IntroHelp1"), Color.white,
                          "Game mode instructions", 28.55f, QText, PayHelpR.W, 18f, 32f, 22.50f,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Text(bar, PayDescR, Loc.T("Demo/DraftMode/IntroHelp2"), Color.white,
                          "Description 1", 30f, QText, PayDescR.W, 18f, 32f, 22.50f,
                          face: TmpFont.Face.LatinCardBody);
            // `ButtonsLayout`（原版 HLG：spacing 0 · align MiddleCenter）—— 三颗钮里我们只建 Free
            var btns = MenuDraw.Node(bar, "ButtonsLayout", PayBarR);
            var freeNode = MenuDraw.Node(btns, "Free Button", FreeBtnR);
            var freeBg = MenuDraw.Rect(freeNode, Tex("UI_Button_Mulligan"), FreeBtnR, "Bg", QArt, null, true);
            var freeTx = MenuDraw.Text(freeNode, FreeTxR, Loc.T("Demo/DraftMode/FreeButton"), Color.white,
                                       "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(freeTx, Label.VAlign.Capline, FreeTxR);        // 原版 align 2/8192
            MenuDraw.Hit(freeNode, "Hit", FreeBtnR, QHit, ClickFree, freeBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            // ⚠️ 原版 `FreeTimeText`（170×70 @ (0,-75)，「Available in …」）是**免费时段未开放**那一支
            // （`GetFreeButtonInteractable` + `Update()` 刷倒计时）。我们没有「免费时段」这条服务端配置
            // ⇒ 不建（建了就得编一个倒计时）。原版那时它 `SetActive(false)` 之外的路径也读不到。
            return page;
        }

        // ------------------------------------------------------------ SelectWarlord 页
        Transform BuildWarlordPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Select Warlord State", new PxRect(0f, 0f, 1920f, 1080f));
            var title = MenuDraw.Text(page, WlTitleR, Loc.T("Demo/DraftMode/SelectWarlord"), Color.white,
                                      "Title", 42f, QText, WlTitleR.W, 10f, 42f, 32.80f,
                                      face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(title, Label.VAlign.Midline, WlTitleR);        // 原版 4096 = Midline

            // `Generic Card Selector Menu`(ScrollRect h=1 v=0) > `Viewport`(Mask) > `Content`(HLG)
            var menu = MenuDraw.Node(page, "Generic Card Selector Menu", WlViewR);
            var view = MenuDraw.Node(menu, "Viewport", WlViewR);
            // 视口裁切 = 原版这颗 `Viewport` 上的 `Mask`（`RectMask2D` 的等价物）；padding / softness 全 0
            // （原版那一颗现读没有软边；硬边 ⇒ `(0,0)`）。
            var vc = view.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = Vector2Int.zero;
            vc.CaptureNow();
            _warlordViewport = view;

            float total = WarlordRowWidth();
            // 🔴 原版 `Generic Card Selector Menu` 那颗 `ScrollRect` 的序列化值就是 **`m_Enabled = 0`**
            //   （`menu_dump … "Generic Card Selector Menu"` 现读；见 D2 §2.1「ScrollRect … m_Enabled=0」）。
            //   `WarlordsPerDraft = 3` ⇒ 总宽 1479.73 < 1920，**一格都滚不动** ⇒ 我们**不建滚动区**
            //   （不注册、不做位移），与「组件级禁用」同观感。
            //   ⚠️ `WarlordsPerDraft` 若将来变大到超一屏，这里会自动建滚动区 —— 那是**兜底**，
            //     不是原版的做法（原版那一刻也拖不动）；真到那时再按原版重判。
            if (total > WlViewR.W)
            {
                _warlordScroll = new MenuScroll(WlViewR, WlViewR.x1, WlViewR.x1 + total, WlScrollNotch);
                _warlordScroll.Owner = gameObject;
                _warlordScroll.ClipNode = vc;
                _warlordScroll.OnChanged = RebuildWarlordCards;
                PointerLayer.RegisterScroll(_warlordScroll);
            }
            else _warlordScroll = null;
            // `Content` 的宽照「整行宽 / 至少一屏」给（原版 `ContentSizeFitter H=MinSize` 撑到子件宽；
            // 这里用同一口径的手算值，居中由 `WarlordRowStartX()` 负责）。
            _warlordContent = MenuDraw.Node(view, "Content", new PxRect(WlViewR.x1, WlViewR.y1,
                                                                      WlViewR.x1 + Mathf.Max(total, WlViewR.W),
                                                                      WlViewR.y2));

            // 空态提示（🔴 我们自拟 —— 原版这一页**没有空态节点**，S3 §四「没有可选战将」那一行：
            // 「原版没有空态文案（该页无 Empty/Warning 节点）⇒ 判定：查不到原版空态，需自建」）
            _warlordHint = MenuDraw.Node(page, "NoWarlordHint", new PxRect(360f, 470f, 1560f, 540f));
            MenuDraw.Text(_warlordHint, new PxRect(360f, 470f, 1560f, 540f),
                          OurText("本机卡池里没有可选战将 —— 轮抽开不了（这一句是我们自拟的：原版这一页没有空态节点）",
                                  "No warlord is available in this build - the draft cannot start."),
                          new Color(1f, 0.85f, 0.4f, 1f), "Text", 30f, QText, 1200f, 18f, 30f, 24f,
                          face: TmpFont.Face.LatinCardBody);
            _warlordHint.gameObject.SetActive(false);

            var cont = MenuDraw.Node(page, "Generic Simplified UI Button_updated", WlContBtnR);
            var contBg = MenuDraw.Rect(cont, Tex("UI_Button_Mulligan"), WlContBtnR, "Bg", QArt, null, true);
            var contTx = MenuDraw.Text(cont, WlContTxR, Loc.T("Battle/Tips/Continue"), Color.white,
                                       "Button Text", 65f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(contTx, Label.VAlign.Capline, WlContTxR);     // 原版 align 2/8192
            var contHit = MenuDraw.Hit(cont, "Hit", WlContBtnR, QHit, () => ContinueAfterWarlord(), contBg,
                                       "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                                       "UI_Button_Mulligan_Pressed",
                                       clickCue: WindowSounds.PositiveCue);
            // 原版 `DraftModeWarlordSelectionStateDemo.OnOpen` 末句：`continueButton.interactable = false`
            // （`...__OnOpen.c` 实读）⇒ 出厂就是灰的；`SelectWarlord` 才置 true（`...__SelectWarlord.c`）。
            _warlordContBtn = contHit != null ? contHit.GetComponent<WindowButton>() : null;
            if (_warlordContBtn != null) _warlordContBtn.Interactable = false;
            if (contHit == null)
                Debug.LogWarning("[Draft] 选将页 `Continue` 的命中区没建出来 ⇒ 真鼠标点不到（判据同 `MenuDraw.Hit` 的告警）。");
            // ⚠️ 原版 `DEBUG_REROLL_BUTTON`（212.93×54.49，粉色）在**正式包预制体里也是 ON**，
            //     S3 §2.4 明写「施工时删掉或置灰」⇒ 本窗**不建**（不给玩家一颗没实现的调试钮）。
            return page;
        }

        void RebuildWarlordCards()
        {
            if (_warlordContent == null) return;
            for (int i = _warlordContent.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_warlordContent.GetChild(i).gameObject);
            _warlordNodes.Clear();
            _warlordViews.Clear();
            _warlordSelBtns.Clear();
            _warlordBodyHits.Clear();
            bool empty = _warlords == null || _warlords.Count == 0;
            if (_warlordHint != null) _warlordHint.gameObject.SetActive(empty);
            if (empty)
            {
                Debug.LogWarning("[Draft] 可选战将 = 0 ⇒ 选将页**一张卡都不实例化**、`Continue` 保持灰"
                               + "（原版同解：`UICardSelector.Initialize(空列表)`；提示句是我们自拟的）。");
                return;
            }
            float y1 = WlSlotCy - WlSlotVisH * 0.5f;
            float y2 = WlSlotCy + WlSlotVisH * 0.5f;
            // 整行居中（3 张：首格布局中心 x = 220.135 + 206.375 = **426.51**，步距 **533.49**）。
            float rowStartX = WarlordRowStartX();
            for (int i = 0; i < _warlords.Count; i++)
            {
                var def = _warlords[i];
                if (def == null) continue;
                float cx = rowStartX + WlSlotW * 0.5f + i * WlStep;
                var rawSlotR = new PxRect(cx - WlSlotVisW * 0.5f, y1, cx + WlSlotVisW * 0.5f, y2);
                // 3 张时不建滚动（`_warlordScroll == null`）⇒ 直接用原框，⛔ 别在这儿假设它非空。
                var slotR = _warlordScroll != null ? _warlordScroll.Shift(rawSlotR) : rawSlotR;
                var node = MenuDraw.Node(_warlordContent, "Generic Select Card Container", slotR);
                var content = MenuDraw.Node(node, "Content", slotR);

                // 卡面（原版 `BasicCardUI` 的 `2DCard`）：**视觉** 386.60×615.42，左上角 = 格视觉框 + (13.1,−19.6)
                // —— 见 `WlCardBodyH` / `WlCardOff` 那两段（2026-10-24 订正：原版是 386.6×615.42，不是 472×751.4）。
                float cardScale = WlCardBodyH / (CardView.Height * 108f);
                float cardW = WlCardBodyH * (CardView.Width / CardView.Height);   // 386.60（卡本体宽高比 = 原版 2DCard）
                var cardR = new PxRect(slotR.x1 + WlCardOff.x, slotR.y1 + WlCardOff.y,
                                       slotR.x1 + WlCardOff.x + cardW, slotR.y1 + WlCardOff.y + WlCardBodyH);
                var cv = CardView.Create(content, BattleDriver.ToCardData(def, def.Faction), "CardUI");
                bool sel = !string.IsNullOrEmpty(_warlordId) && def.Id == _warlordId;
                if (cv != null)
                {
                    cv.SetPose(MenuDraw.Local(content, cardR.x1, cardR.y1, cardR.x2, cardR.y2), 0f, cardScale);
                    cv.SetQueue(QCardBase + i);
                    // 选中态：原版 `GenericSelectCardContainer.ToggleSelected` 第一句 =
                    // `Card2DController.ToggleHighlight(on)`（`...__ToggleSelected.c` 实读；`selectedContent`
                    // 在同一份 prefab 里是 **null** ⇒ 原版没有「已选」贴图节点）。我们映射到卡自己的 `Selected` 档
                    // （`CardHighlightState.Selected` = 原版 `selected`，白）—— ⚠️ 「用哪一档」是我们的映射（如实标）。
                    cv.SetHighlight(sel ? CardHighlightState.Selected : CardHighlightState.Normal);
                }
                _warlordViews.Add(cv);

                // 卡本体那一块**吸收**点击 —— 原版是 `2DCard` 里的 `UI Collider`
                // （`UIGenericEventCatcher + NonDrawingGraphic`，`menu_dump` 现读那张卡的那一行；
                //  它身上**没有任何处理器**，`GenericSelectCardContainer` 也只把 `selectButton` 接去了
                //  `SelectWarlord`（`...__SelectButtonOnClick.c`））
                // ⇒ 原版点卡体 = **射线打到卡自己、父链上没有处理器 ⇒ 什么都不做**。
                // 我们原来这块是空的 ⇒ 点卡体会穿透到窗内吸收层 `AbsorbHit`（观感同「卡点不动」，
                // 但命中件不同、且会被窗子的关闭链吃掉）。
                //
                // 🔴 **2026-10-24（第七十四会话 · `W7`）就地改回原版口径**：
                //   上一批（`W3`）按一份**给错了的指令**把它改成了 `MenuDraw.Hit + ChooseWarlord(id)`
                //   （「点卡体该选中」）—— 那与判据不符。判据（第一权威 = 反编译 + prefab 实读）：
                //   · `2DCard/UI Collider` = `UIGenericEventCatcher + NonDrawingGraphic`，**无处理器**；
                //   · `GenericSelectCardContainer` 的 `OnEnable` 只把 `selectButton` 接到 `SelectWarlord`
                //     （`decomp_full/GenericSelectCardContainer__SelectButtonOnClick.c` / `__OnEnable.c`）；
                //   · `selectedContent` 在两个实例里都是 **null** ⇒ 原版连「已选」贴图都没有。
                //   ⇒ **点卡体什么都不做**（吸收掉，⛔ 不选中、不换页、不关窗）。
                //   这一格的命中档 = `QHit − 1` = **4169**：**严格高于**窗内吸收层 4168、
                //   **严格低于** `Select Button` 的 4170 ⇒ 卡体吃得下自己那一下、又不会抢按钮
                //   （两者在 y 762.82…773.14 有 10px 重叠带）。
                string id = def.Id;
                var bodyHit = MenuDraw.Absorb(content, "CardBodyHit", cardR, QShade, QHit, WlViewR);
                _warlordBodyHits.Add(bodyHit != null ? bodyHit.GetComponent<WindowButton>() : null);

                // `Select Button`（原版 210.02×81.66 视觉，压在卡体下沿之外）
                var selR = new PxRect(slotR.x1 + WlSelBtnOff.x1, slotR.y1 + WlSelBtnOff.y1,
                                      slotR.x1 + WlSelBtnOff.x2, slotR.y1 + WlSelBtnOff.y2);
                var txR = new PxRect(slotR.x1 + WlSelTxOff.x1, slotR.y1 + WlSelTxOff.y1,
                                     slotR.x1 + WlSelTxOff.x2, slotR.y1 + WlSelTxOff.y2);
                var selNode = MenuDraw.Node(content, "Select Button", selR);
                var selBg = MenuDraw.Rect(selNode, Tex("UI_Button_Mulligan"), selR, "Bg", QArt, null, true);
                var selTx = MenuDraw.Text(selNode, txR, Loc.T("Battle/Prebattle/SelectButton"), Color.white,
                                          "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
                MenuDraw.SetVAlign(selTx, Label.VAlign.Capline, txR);        // 原版 align 2/8192
                var selHit = MenuDraw.Hit(selNode, "Hit", selR, QHit, () => ChooseWarlord(id), selBg,
                                          "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                                          "UI_Button_Mulligan_Pressed",
                                          clip: WlViewR, clickCue: WindowSounds.SelectAvatarCue);
                // 原版 `ToggleSelected` 末两句：`selectButton.interactable = !selected`（选中那格自己变灰）
                var selWb = selHit != null ? selHit.GetComponent<WindowButton>() : null;
                if (selWb != null) selWb.Interactable = !sel;
                _warlordSelBtns.Add(selWb);

                _warlordNodes.Add(node);
            }
            ApplyWarlordSelectionVisuals();
        }

        /// <summary>按 `_warlordId` 刷一次「选中态」的三处可见反馈（⛔ 不重建卡）：
        /// ① 选中的那张卡 `CardHighlightState.Selected`；② 那格的 `Select Button` 灰掉（原版
        /// `ToggleSelected` 的 `interactable = !selected`）；③ `Continue` 可点（原版 `SelectWarlord` 末句
        /// `continueButton.interactable = true`；`OnOpen` 出厂是 **false** ⇒ 按钮变灰）。</summary>
        void ApplyWarlordSelectionVisuals()
        {
            for (int i = 0; i < _warlordViews.Count; i++)
            {
                var cv = _warlordViews[i];
                bool sel = cv != null && i < _warlords.Count && _warlords[i] != null
                           && !string.IsNullOrEmpty(_warlordId) && _warlords[i].Id == _warlordId;
                if (cv != null) cv.SetHighlight(sel ? CardHighlightState.Selected : CardHighlightState.Normal);
                if (i < _warlordSelBtns.Count && _warlordSelBtns[i] != null)
                    _warlordSelBtns[i].Interactable = !sel;
            }
            if (_warlordContBtn != null)
                _warlordContBtn.Interactable = !string.IsNullOrEmpty(_warlordId);
        }

        // ---- 选将页的自检读口（2026-10-24 U1）---------------------------------------------
        // 🔴 这三个口**只转发**，⛔ 不在里面算版面（自检要量生产那一份；判据同 `PointerLayer.HitBoxForTest`
        //    那段「转发不加工」）。版面期望值写在自检里、来自 prefab（见 `DraftAssertD3` 的 U1 那一组）。

        /// <summary>选将页第 <paramref name="i"/> 格的卡视图（`null` = 那一格没建 / 越界）。</summary>
        public CardView WarlordCardViewForTest(int i)
            => (i >= 0 && i < _warlordViews.Count) ? _warlordViews[i] : null;

        /// <summary>选将页第 <paramref name="i"/> 格 `Select Button` 的 `WindowButton`
        /// （要量命中区就再走 `PointerLayer.HitBoxForTest`；`null` = 越界或那一格的命中区没建出来）。</summary>
        public WindowButton WarlordSelectButtonForTest(int i)
            => (i >= 0 && i < _warlordSelBtns.Count) ? _warlordSelBtns[i] : null;

        /// <summary>选将页第 <paramref name="i"/> 格**卡本体**（`CardBodyHit`）的 `WindowButton`
        /// （`null` = 越界 / 没建出来）。⚠️ 它是**吸收层**（`onClick == null`）—— 见 `_warlordBodyHits` 的注释。</summary>
        public WindowButton WarlordCardBodyHitForTest(int i)
            => (i >= 0 && i < _warlordBodyHits.Count) ? _warlordBodyHits[i] : null;

        /// <summary>🆕 2026-10-24（真机 F1）：**这一扇窗**里，设计 px 那一点上**真正吃到点击**的是哪一颗钮。
        /// <para>判据与生产 `PointerLayer.HitButton` **三条键同序**：渲染队列↓ → `z` 小的先 → 后兄弟先
        /// （第三条的口径见 `Shell/PointerLayer.cs` 的 `IsAboveInHierarchy`）。候选表与命中框**都走生产的
        /// 那两个读口**（`AllButtonsForTest` / `HitBoxForTest`），本方法**只**做「挑赢家」这一步转发。</para>
        /// <para>🔴 为什么要这个口：F1 的漏网原因就是自检**直调 `Click()`** —— 那绕开了「谁的队列高」，
        /// 于是「同队列被窗内吸收层吃掉」这个真机缺陷全绿。有了它，断言可以断「那一点吃到的是谁」。</para>
        /// <para>⚠️ 与 `Editor/DraftAssertD3.cs` 里那份私有的 `HitTestInWindow` 是**同一条规则的第二份**
        /// （那份不在本轮白名单、没能收口到这里）—— 将来两处要合成一处。</para></summary>
        public static WindowButton PointWinnerForTest(Transform win, float px, float py, out int q, out string who)
        {
            WindowButton best = null; ImageQuad bestQ0 = null;
            int bestQ = int.MinValue; float bestZ = float.MaxValue;
            string desc = "空点（本窗没有钮盖住这一点）";
            var all = PointerLayer.AllButtonsForTest();
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null || b.transform == null || win == null || !b.transform.IsChildOf(win)) continue;
                Vector2 c, h; ImageQuad quad;
                if (!PointerLayer.HitBoxForTest(b, out c, out h, out quad)) continue;
                if (Mathf.Abs(px - c.x) > h.x || Mathf.Abs(py - c.y) > h.y) continue;
                int qq = quad != null ? quad.RenderQueue : int.MinValue;
                float z = quad != null ? quad.transform.position.z : 0f;
                bool take;
                if (best == null) take = true;
                else if (qq != bestQ) take = qq > bestQ;
                else if (z < bestZ - 1e-4f) take = true;
                else if (z > bestZ + 1e-4f) take = false;
                else take = AboveInHierarchyForTest(quad != null ? quad.transform : null,
                                                    bestQ0 != null ? bestQ0.transform : null);
                if (!take) continue;
                best = b; bestQ0 = quad; bestQ = qq; bestZ = z;
                desc = b.name + "（队列 " + qq + "）";
            }
            q = bestQ; who = desc; return best;
        }

        /// <summary>同队列同 `z` 时 = **后兄弟先**（口径逐字照 `PointerLayer.IsAboveInHierarchy`；那份是
        /// `private`，本文件转发不到 ⇒ 只能留第二份，⚠️ 改生产那份时这份要跟着改）。</summary>
        static bool AboveInHierarchyForTest(Transform a, Transform b)
        {
            if (a == b || a == null) return false;
            if (b == null) return true;
            var pa = new List<Transform>(); var pb = new List<Transform>();
            for (var t = a; t != null; t = t.parent) pa.Insert(0, t);
            for (var t = b; t != null; t = t.parent) pb.Insert(0, t);
            int n = Mathf.Min(pa.Count, pb.Count);
            for (int i = 0; i < n; i++)
            {
                if (pa[i] == pb[i]) continue;
                return pa[i].GetSiblingIndex() > pb[i].GetSiblingIndex();
            }
            return pa.Count > pb.Count;
        }

        /// <summary>选将页 `Continue` 的 `WindowButton`（原版 `OnOpen` 出厂 `interactable = false`）。</summary>
        public WindowButton WarlordContinueButtonForTest { get { return _warlordContBtn; } }

        /// <summary>选将页**建没建横向滚动区**（`true` = 整行超一屏、建了 `MenuScroll`）。
        /// 原版 `Generic Card Selector Menu` 的 `ScrollRect.m_Enabled = 0`（拖不动）⇒ 3 张时这里必须是 `false`。</summary>
        public bool WarlordScrollingForTest { get { return _warlordScroll != null; } }

        /// <summary>右栏 `CardCounter` 当前印的字（自检读口）。</summary>
        public string CardsCounterText { get { return _cardsCounter != null ? _cardsCounter.Text : null; } }
        /// <summary>`Reroll` 那颗价格钮当前印的字（自检读口）。</summary>
        public string RerollPriceText { get { return _rerollPrice != null ? _rerollPrice.Text : null; } }
        /// <summary>`Reroll` 整行那棵子树（自检用；`null` = 没建出来）。</summary>
        public Transform RerollNode { get { return _pagePacks != null ? _pagePacks.Find("Reroll") : null; } }
        /// <summary>右栏 Deck Info 页的内容块（原版 `Energy View panel`；自检读口）。</summary>
        public Transform DeckInfoHolder { get { return _deckInfoHolder; } }
        /// <summary>`Card List` 页的 `Card counter` 当前印的字（自检读口；`null` = 不在那一页）。</summary>
        public string DeckListCounterText { get { return _deckListCounter != null ? _deckListCounter.Text : null; } }

        /// <summary>选将页那一格**画出来的**卡（`2DCard`）在设计 px 下的矩形 —— **从 `CardView` 自己量**
        /// （`BaseScale × CardView.Width/Height × 108` + 它的世界位置），⛔ 不是回放我们喂进去的那个框
        /// （回放 = 拿实现证明实现）。</summary>
        public bool WarlordCardRectRenderedForTest(int i, out PxRect r)
        {
            r = default(PxRect);
            var cv = WarlordCardViewForTest(i);
            if (cv == null) return false;
            var c = LayoutSpace.ToDesignPixel(cv.transform.position);
            float h = CardView.Height * cv.BaseScale * 108f;
            float w = CardView.Width * cv.BaseScale * 108f;
            r = new PxRect(c.x - w * 0.5f, c.y - h * 0.5f, c.x + w * 0.5f, c.y + h * 0.5f);
            return true;
        }


        // ------------------------------------------------------------ SelectPack 页
        Transform BuildPacksPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Select Packs State", new PxRect(0f, 0f, 1920f, 1080f));

            // `Warlord Image`（1072.5²，`sprite=0` ⇒ 运行时喂督军立绘；**sprite 为空 ⇒ 整节点关**，S3 §2.5）
            var wlNode = MenuDraw.Node(page, "Warlord Image", WarlordImgR);

            var content = MenuDraw.Node(page, "Content", new PxRect(118.70f, 156.60f, 1500.30f, 880.50f));
            MenuDraw.Text(content, SubTitleR, Loc.T("MenuDeck/HUD/ChooseCards"), Color.white,
                          "SubTitle", 42f, QText, SubTitleR.W, 10f, 42f, 35.16f,
                          face: TmpFont.Face.LatinCardBody);

            // `Packs`（HLG align MiddleCenter · spacing 11.1）：`Pack` 标签 + `Stage Counter`
            var packsRow = MenuDraw.Node(content, "Packs", new PxRect(698.60f, 317.60f, 1145.50f, 365.50f));
            var packLb = MenuDraw.Text(packsRow, PackLabelR, Loc.T("MainMenu/DraftPack"), Color.white,
                                       "Pack", 35f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(packLb, PackLabelR);                  // 原版 hAlign = Right
            // 🔴 `Stage Counter` 的**文本**在 `RefreshPacksPage()` 里写（`{round+1}/{draftRounds}`）；
            //    初值写一次只是为了让批处理里「节点 + 字」都在。
            _stageCounter = MenuDraw.Text(packsRow, StageR, "1/" + DraftPacks.RoundsPerDraft, Color.white,
                                          "Stage Counter", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_stageCounter, StageR);                // 原版 hAlign = Left

            // `Packs Mask`（`Image(ty=1) + Mask`）+ 里面那层 `Packs Container`（HLG spacing 13）
            var mask = MenuDraw.Node(content, "Packs Mask", PacksMaskR);
            MenuDraw.Nine(mask, Tex("UI_Deck_Information_Back"), PacksMaskR, MaskBorder, MaskTexW, MaskTexH,
                          QPanelBack, new Color(0.802f, 0.802f, 0.802f, 1f), true, "Image",
                          new Vector4(MaskBorder.x * 0.5f, MaskBorder.y * 0.5f,
                                      MaskBorder.z * 0.5f, MaskBorder.w * 0.5f));
            var mvc = mask.gameObject.AddComponent<ViewportClip>();
            mvc.padding = Vector4.zero;
            mvc.softness = Vector2Int.zero;
            mvc.CaptureNow();
            _packsContainer = MenuDraw.Node(mask, "Packs Container", PacksContR);

            // 空态提示（🔴 我们自拟：原版包不够时**不报错、不占位**（S3 §四），我们照它「有几张画几张」，
            // 只在**一张都没有**时补一句人话 + 出声）
            _packHint = MenuDraw.Node(page, "NoPackHint", new PxRect(560f, 560f, 1360f, 630f));
            MenuDraw.Text(_packHint, new PxRect(560f, 560f, 1360f, 630f),
                          OurText("这个阵营没有可用的卡包了 —— 轮抽开不下去（这一句是我们自拟的）",
                                  "No card pack is available for this army - the draft cannot continue."),
                          new Color(1f, 0.85f, 0.4f, 1f), "Text", 30f, QText, 800f, 18f, 30f, 24f,
                          face: TmpFont.Face.LatinCardBody);
            _packHint.gameObject.SetActive(false);

            // ---- `Reroll` 行（原版有、上一批没建；2026-10-24 第七十四会话 `W7` 补上）----
            //   几何/字号/对齐 = prefab 现读（见 `RerollR` 那一组的注释）。价格规则见 `ReRollCost`。
            var reroll = MenuDraw.Node(page, "Reroll", RerollR);
            var rerollTx = MenuDraw.Text(reroll, RerollTxR, "Reroll packs:", Color.white, "Reroll Text",
                                         54.80f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(rerollTx, RerollTxR);                       // 原版 hAlign = Right
            MenuDraw.SetVAlign(rerollTx, Label.VAlign.Midline, RerollTxR);  // 原版 vAlign = Midline
            var rerollBtn = MenuDraw.Node(reroll, "Price Display Button 2 Variant", RerollBtnR);
            var rerollBg = MenuDraw.Rect(rerollBtn, Tex("UI_Button_Mulligan"), RerollBtnR, "Generic UI Button",
                                         QArt, null, true);
            // `Price Display`：原版是 HLG(icon + text) + ARF 4.8848。**免费档（cost == 0）时原版走
            //   `PriceDisplay.SetupFree` ⇒ 只给文本、不给 icon**（`PriceDisplay__Setup.c` 的 `param_2 == null`
            //   那一支）⇒ 我们直接把那句字铺在 `Price Display` 那个框里居中，⛔ 不画货币图标
            //   （表空 ⇒ 恒免费；货币图本地也没有对应 sprite）。
            _rerollPrice = MenuDraw.Text(rerollBtn, RerollPriceR, "", Color.white, "Price Display",
                                         54f, QText, RerollPriceR.W, 12f, 54f, 39f,
                                         face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_rerollPrice, Label.VAlign.Capline, RerollPriceR);   // 原版 `text` = Center/Capline
            MenuDraw.Hit(rerollBtn, "Hit", RerollBtnR, QHit, () => ReRollPacks(), rerollBg,
                         "UI_Button_Mulligan", "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed");
            _warlordImgNode = wlNode;
            return page;
        }

        Transform _warlordImgNode;

        // ------------------------------------------------------------ Ongoing 页（含 Finished）
        Transform BuildOngoingPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Ongoing State", new PxRect(0f, 0f, 1920f, 1080f));
            // `MainContent`（`DraftRunDisplay`，`localScale = 1.06` —— 下面的坐标**已含**那次缩放，现读值）
            var main = MenuDraw.Node(page, "MainContent", MainContR);
            _ongoingWarlordNode = MenuDraw.Node(main, "Warlord Image", OnWlImgR);
            var scaled = MenuDraw.Node(main, "Scaled Content", new PxRect(1030.70f, 498.40f, 1136.70f, 604.40f));
            var onTitle = MenuDraw.Text(scaled, OnTitleR, Loc.T("Demo/DraftMode/Title"), Color.white,
                                        "Game Mode Title", 45f, QText, OnTitleR.W, 10f, 45f, 47.73f,
                                        face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(onTitle, Label.VAlign.Midline, OnTitleR);     // 原版 align 2/4096
            // `Victories:` + 数字
            var vict = MenuDraw.Text(scaled, OnVictR, Loc.T("Demo/DraftMode/Victories"), Color.white,
                                     "Victories text", 45.80f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(vict, OnVictR);
            _victoriesNum = MenuDraw.Text(scaled, OnVictNumR, "0", Color.white, "Victories text Number",
                                          89.23f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_victoriesNum, OnVictNumR);
            MenuDraw.SetVAlign(_victoriesNum, Label.VAlign.Midline, OnVictNumR);   // 原版 align 1/4096

            // 胜场格（`Stages Container` GridLayoutGroup：cell 80² · spacing (23.7,30.04) · 2 列 · MiddleCenter）
            var winMarks = MenuDraw.Node(scaled, "Win Marks", new PxRect(697.90f, 373.80f, 1471.60f, 708.80f));
            // `Background Back`：原版那颗 `Image` **组件级禁用**（`m_Enabled = 0`）⇒ 原版不画 ⇒ 不建
            _winMarksHolder = winMarks;
            RebuildStageMarks();

            // 败场（`Defeats:` + 3 格：原版 `Losses container` HLG spacing 9.86 · 格子 67.74²）
            var defeat = MenuDraw.Text(scaled, OnDefeatR, Loc.T("Demo/DraftMode/Losses"), Color.white,
                                       "Defeat Marks", 42.85f, QText, OnDefeatR.W, 18f, 50f, 36f,
                                       face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(defeat, OnDefeatR);
            _lossMarksHolder = MenuDraw.Node(scaled, "Losses container", OnLossesR);
            RebuildDefeatMarks();

            // `Quote End`（只有 Finished 时显示；按胜场档取一句，S3 §2.7）
            _quoteEnd = MenuDraw.Text(main, new PxRect(489.30f, 245f, 1501.60f, 305.50f), "", Color.white,
                                      "Quote End", 32f, QText, 1012.3f, 10f, 32f, 26f,
                                      face: TmpFont.Face.LatinCardBody);
            if (_quoteEnd != null) _quoteEnd.gameObject.SetActive(false);

            // `Reset Event Button`（Ongoing 显示 · Finished 隐藏；文案 = `Demo/DraftMode/ResignDraftButton`）
            _resetNode = MenuDraw.Node(scaled, "Reset Event Button", OnResetR);
            var resetBg = MenuDraw.Rect(_resetNode, Tex("UI_Button_Mulligan"), OnResetR, "Bg", QArt, null, true);
            var resetTx = MenuDraw.Text(_resetNode, OnResetTxR, Loc.T("Demo/DraftMode/ResignDraftButton"),
                                        Color.white, "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(resetTx, Label.VAlign.Capline, OnResetTxR);   // 原版 align 2/8192
            MenuDraw.Hit(_resetNode, "Hit", OnResetR, QHit, Resign, resetBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed");

            // `To Battle Button`（Ongoing 显示 · Finished 隐藏）
            _toBattleNode = MenuDraw.Node(page, "To Battle Button", OnBattleR);
            var battleBg = MenuDraw.Rect(_toBattleNode, Tex("UI_Button_Mulligan"), OnBattleR, "Bg", QArt, null, true);
            var battleTx = MenuDraw.Text(_toBattleNode, OnBattleTxR, Loc.T("Demo/ToBattle"), Color.white,
                                         "Button Text", 74.25f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(battleTx, Label.VAlign.Capline, OnBattleTxR);  // 原版 align 2/8192
            MenuDraw.Hit(_toBattleNode, "Hit", OnBattleR, QHit, () => LaunchBattle(), battleBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);

            // `Debug Win` / `Debug Battle Button Button`：原版 `OnEnable` **强制 SetActive(false)**（正式包也不显示）
            // ⇒ ⛔ 不建。

            _rewardPanelRoot = BuildRewardPanel(page);
            return page;
        }

        Transform _ongoingWarlordNode, _resetNode, _toBattleNode;
        Label _quoteEnd;
        readonly List<Transform> _winMarks = new List<Transform>();
        readonly List<Transform> _lossMarks = new List<Transform>();

        /// <summary>胜场格（原版 `DraftRunDisplay.SetupStageMarks(winMarkHolder, save.won, data.matches.Length, …)`）。
        /// <para>原版：**格子数 = `max`**、每格先把 `imageObject`（子件 `+0x20`）`SetActive(i &lt; current)` ——
        /// 即「前 `current` 格亮」。`current` = `numberOfMatchesWon`（`DraftRunDisplay__Initialize.c` 实读）。</para>
        /// <para>🔴 **`max` 我们不知道**：原版是 `data.matches.Length`（服务端 `EventMatchInfo[]`，本地 0 命中）。
        /// 我们取 `max(RoundsPerDraft, 胜场)` —— 下界沿用原来那个「4 格」的占位口径，
        /// 同时在**胜场超过它时自动加格**，这样**不会把赢过的场次藏起来**（静默少画 = 撒谎）。
        /// ⛔ 别把它写死成 4 或 13（13 是奖励档阈值，不是 `matches.Length`）。</para></summary>
        void RebuildStageMarks()
        {
            var holder = _winMarksHolder;
            if (holder == null) return;
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            _winMarks.Clear();
            int wins = Victories;
            int n = Mathf.Max(DraftPacks.RoundsPerDraft, wins);
            int cols = 2;
            float gridW = cols * StageCell + (cols - 1) * StageGapX;
            float x0 = OnStagesR.CX - gridW * 0.5f;
            int rows = Mathf.CeilToInt(n / (float)cols);
            float gridH = rows * StageCell + (rows - 1) * StageGapY;
            float y0 = OnStagesR.CY - gridH * 0.5f;
            for (int i = 0; i < n; i++)
            {
                int c = i % cols, r = i / cols;
                float x = x0 + c * (StageCell + StageGapX), y = y0 + r * (StageCell + StageGapY);
                var cell = new PxRect(x, y, x + StageCell, y + StageCell);
                var cellNode = MenuDraw.Node(holder, "Stage Info UI_ref", cell);
                // 空槽 = 半透明黑块（原版那格 `imageObject` 的 `winIndicatorPrefab` 图在外部依赖里、本地没有）
                MenuDraw.Rect(cellNode, CardArt.Solid(), cell, "Slot", QArt, new Color(0f, 0f, 0f, 0.45f));
                // 🆕 亮的那一格（= 原版 `imageObject.SetActive(i < current)`）—— 图本地没有 ⇒ 画金色块占位
                var mark = MenuDraw.Rect(cellNode, CardArt.Solid(), cell, "Mark", QArt1,
                                         new Color(1f, 0.68f, 0.18f, 0.95f));
                if (mark != null)
                {
                    mark.gameObject.SetActive(i < wins);
                    if (i < wins) _winMarks.Add(mark.transform);
                }
            }
            if (n > 0 && CardArt.MenuUi("40k_Victory_Mark") == null)
                Debug.Log("[Draft] 胜场格的**亮格图**（原版 `winIndicatorPrefab.imageObject`）在外部依赖里、**本地没有**"
                        + " ⇒ 亮的那几格画的是金色占位块（⛔ 没拿别的图顶）。胜场 = " + wins + " / 画了 " + n + " 格");
        }

        /// <summary>败场格（原版 `SetupStageMarks(defeatMarkHolder, save.lost, data.maxTries, …)`）——
        /// 格子数 = `maxTries`（我们取 <see cref="MaxTries"/> = 3，与 prefab 的 3 格一致）。</summary>
        void RebuildDefeatMarks()
        {
            var holder = _lossMarksHolder;
            if (holder == null) return;
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            _lossMarks.Clear();
            int losses = Defeats;
            float lx = OnLossesR.x1;
            for (int i = 0; i < MaxTries; i++)
            {
                var cell = new PxRect(lx, OnLossesR.CY - LossCell * 0.5f, lx + LossCell, OnLossesR.CY + LossCell * 0.5f);
                MenuDraw.Node(holder, "Losses Info UI_ref", cell);
                // 空槽 = 一颗半透明黑块（原版 `Losses Info UI_ref` 的图 `ext10:-5957999898718987630` 在
                // 外部依赖里、本地没有 ⇒ 画占位，⛔ 不拿别的骷髅图顶）
                MenuDraw.Rect(holder, CardArt.Solid(), cell, "Slot", QArt, new Color(0f, 0f, 0f, 0.45f));
                var mark = MenuDraw.Rect(holder, CardArt.Solid(), cell, "Mark", QArt1,
                                         new Color(0.85f, 0.16f, 0.13f, 0.95f));
                if (mark != null)
                {
                    mark.gameObject.SetActive(i < losses);
                    if (i < losses) _lossMarks.Add(mark.transform);
                }
                lx += LossCell + LossGap;
            }
            if (CardArt.MenuUi("40k_Defeat_Mark") == null)
                Debug.Log("[Draft] 败场格的原版图（`Losses Info UI_ref` 的 sprite）在外部依赖里、**本地没有**"
                        + " ⇒ 现在画的是半透明黑块占位（⛔ 没拿别的图顶）。败场 = " + losses + " / " + MaxTries);
        }
        /// <summary>真的**亮着**的胜场格数（自检读口 —— 断「胜场真的是从战绩来的」）。</summary>
        public int FilledWinMarks { get { return _winMarks.Count; } }
        /// <summary>真的**亮着**的败场格数（自检读口）。</summary>
        public int FilledLossMarks { get { return _lossMarks.Count; } }

        // ------------------------------------------------------------ 奖励面板（Finished）
        Transform BuildRewardPanel(Transform page)
        {
            var panel = MenuDraw.Node(page, "Reward Info Panel", RewardPanelR);
            var info = MenuDraw.Node(panel, "Reward Info", RewardPanelR);
            // `Highlight Crate`（`Crate Border Highlight` · 金色 0.427 · **只画边框** fillCenter=0）
            MenuDraw.Nine(info, Tex("Crate_Border_Highlight"), CrateGlowR, new Vector4(0f, 0f, 0f, 0f), 128f, 128f,
                          QArt1, new Color(0.98f, 0.801f, 0.0588f, 0.427f), false, "Highlight Crate",
                          null, null, default(Vector2));
            // `Crate`：原版 `DraftRewardPanel.Initialize` 按 **`CurrentStage`（= 胜场 `save+0x48`）** 换六档
            // （`<3 / 3–4 / 5–6 / 7–8 / 9–12 / ≥13`，反汇编逐档读出来；S3 §2.8 记的是同一组阈值）。
            // 🔴 **本地只有 `40k_Crate_Tier1_Iron` 一张图**（`Resources/Art/ui_menu/` 其余档 0 命中）
            // ⇒ 恒画 Tier1 并出声，⛔ 不编图名。`CurrentStage` 那个读口已经接上（胜场一到就能换）。
            MenuDraw.Rect(info, Tex("40k_Crate_Tier1_Iron"), CrateR, "Crate", QArt, null, true);
            if (CurrentStage >= 3)
                Debug.Log("[Draft] `CurrentStage = " + CurrentStage + "` ⇒ 原版这一刻该换更高档的宝箱图，"
                        + "但本地**只有** `40k_Crate_Tier1_Iron` 一张（其余五档 0 命中）⇒ 仍画 Tier1（⛔ 不编图名）");
            var btn = MenuDraw.Node(info, "Collect Button", CollectR);
            var bg = MenuDraw.Rect(btn, Tex("UI_Button_Mulligan"), CollectR, "Bg", QArt, null, true);
            var collectTx = MenuDraw.Text(btn, CollectTxR, Loc.T("MenuShop/CollectButton"), Color.white,
                                          "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(collectTx, Label.VAlign.Capline, CollectTxR);  // 原版 align 2/8192
            MenuDraw.Hit(btn, "Hit", CollectR, QHit, CollectReward, bg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            panel.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ 右栏（Deck Info Panel）
        Transform BuildDeckInfoPanel(Transform root)
        {
            var panel = MenuDraw.Node(root, "Draft Mode Deck Info Panel", DeckPanelR);
            // `Buttons`（HLG + ToggleGroup；三颗互斥）—— 原版那颗 bar 宽 314.05（现读），
            // 三颗 82.09×78.45 · spacing 12.98 · align MiddleLeft ⇒ 从 bar 左沿起排。
            MenuDraw.Node(panel, "Buttons", DeckTabsBarR);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var r = DeckToggleR[i];
                var tog = MenuDraw.Node(panel, DeckToggleNames[i], r);
                var bgNode = MenuDraw.Node(tog, "button_bg", r);
                var bgQ = MenuDraw.Rect(bgNode, Tex(i == _deckTab ? ToggleArtOnOff[0] : ToggleArtOnOff[1]), r,
                                        "Image", QArt, new Color(0.868f, 0.0264f, 0f, 1f));
                // `Icon` 64×64（左/上内缩 9.1 / 7.3；第三颗 ×1.1）
                bool big = i == 2;
                float side = big ? 70.40f : 64f;
                float icx = r.x1 + 9.1f + 32f, icy = r.y1 + 7.3f + 32f;
                var icR = new PxRect(icx - side * 0.5f, icy - side * 0.5f, icx + side * 0.5f, icy + side * 0.5f);
                MenuDraw.Node(tog, "Icon", icR);
                var icon = Tex(DeckToggleIconArt[i]);
                if (icon != null) MenuDraw.Rect(tog, icon, icR, "Image", QArt1, null, true);
                else Debug.LogWarning("[Draft] 右栏 toggle 图标「" + DeckToggleIconArt[i] + "」取不到"
                                    + "（期望 `Resources/Art/ui_menu/" + DeckToggleIconArt[i] + ".png`）"
                                    + " ⇒ 这一格只有底、没有图标（⛔ 不拿别的图顶）");
                MenuDraw.Hit(tog, "Hit", r, QHit, () => SetDeckTab(idx), bgQ, ToggleArtOnOff[1],
                             ToggleArtOnOff[0], ToggleArtOnOff[0], clickCue: WindowSounds.SelectToggleCue);
            }
            // `Generic Window Red Background Small`：原版**初始 act=off**（`UIEffectTweener` 只在
            // 「选包成功」那一下 Play 闪一次，S3 §2.9 末行）⇒ 节点照建、**默认关着**；
            // 本轮不做那一下闪光（动画时长常量只在 `DAT_*` 里）—— 见 `PickPack` 那条出声。
            var flash = MenuDraw.Nine(panel, Tex("UI_Deck_Selection_Back"),
                                      new PxRect(1500f, 215.30f, 1900f, 911.76f),
                                      new Vector4(0f, 325f, 0f, 35f), 439f, 664f, QPanel, null, true,
                                      "Generic Window Red Background Small");
            if (flash != null) flash.SetActive(false);
            // `content` 里只有三页；本批建两页（Deck Info / Card List），第三页（Alliance）**留空 + 出声**
            _deckTitle = MenuDraw.Text(panel, DeckTitleR, Loc.T("MenuDeck/HUD/DeckDescription/DeckInfo"), Color.white,
                                       "Title", 36f, QText, DeckTitleR.W, 18f, 36f, 36f,
                                       face: TmpFont.Face.LatinCardBody);
            _cardsCounter = MenuDraw.Text(panel, DeckCounterR, "", Color.white, "CardCounter", 36f, QText,
                                          face: TmpFont.Face.LatinCardBody);
            _deckEnergyLabel = MenuDraw.Text(panel, DeckEnergyR, Loc.T("Demo/EnergyBalance/EnergyBalance"), Color.white,
                                             "Energy balance", 36f, QText, DeckEnergyR.W, 18f, 36f, 36f,
                                             face: TmpFont.Face.LatinCardBody);
            _deckTabHolder = MenuDraw.Node(panel, "Cards in deck panel", DeckListR);
            // Deck Info 页的内容块（原版 `Energy View panel`）—— 内容在 `RefreshDeckPanel()` 里按
            // 「当前选了几张 / 战将是谁」重建（它是**会变**的：选包、选将都会动它）。
            _deckInfoHolder = MenuDraw.Node(panel, "Energy View panel", DeckPanelR);
            return panel;
        }
        static readonly string[] DeckToggleNames = { "Deck Info Toggle", "Card List Toggle", "Alliance Info Toggle" };

        // ============================================================ 卡包预览层（原版 `Generic Multi Card Display`）
        /// <summary>建**预览层**（原版那扇多卡展示窗的树）。默认隐藏。
        /// <para>树（逐节点照 prefab，S3 §2.5「预览」+ 本轮现读）：`Generic Multi Card Display`
        /// → `Menu Dark Background`(吃射线) · `Header Text` · `Viewport/Content/CardUI Reference…` ·
        /// `Close button` + `Close button/Text`('Back')。</para>
        /// <para>🔴 **`BackgroundCloseButton.window` 是空引用**（`MonoBehaviour_-1185320953084347707.json`:
        /// `"window": {"m_PathID": 0}` + `onClick` 空）⇒ 原版**点背景什么都不做**（同族：`PopUpGameWindow`）。
        /// ⇒ 用 `MenuDraw.Absorb`（**不是** `ShadeHit`）。唯一的关窗路 = `Close button`（+ ESC）。</para></summary>
        Transform BuildPackPreview(Transform root)
        {
            var node = MenuDraw.Node(root, "Generic Multi Card Display", PrevRootR);
            // 压暗整屏（原版 `Menu Dark Background`：纯粹一块吃射线的黑，`m_Color = (0,0,0,0.773)`）
            MenuDraw.Rect(node, CardArt.Solid(), PrevShadeR, "Menu Dark Background", QPreviewShade,
                          new Color(0f, 0f, 0f, 0.773f));
            MenuDraw.Absorb(node, "BackdropAbsorb", PrevShadeR, QPreviewShade, QPreviewHit);
            // `Header Text`：🔴 **默认关**（`PackPreview` 传 `headerTitle = null`，见 `PrevHeaderR` 的注释）
            _previewHeader = MenuDraw.Text(node, PrevHeaderR, "Header Text", Color.white, "Header Text",
                                           38f, QPreviewText, PrevHeaderR.W, 18f, 38f, 31.9f,
                                           face: TmpFont.Face.LatinCardBody);
            if (_previewHeader != null) _previewHeader.gameObject.SetActive(false);
            // `Viewport`（原版那颗 `ScrollRect` 组件 `m_Enabled = 0` ⇒ 拖不动 ⇒ 我们照「不动」做）
            var viewport = MenuDraw.Node(node, "Viewport", PrevRootR);
            _previewCards = MenuDraw.Node(viewport, "Content", PrevRootR);
            // `Close button`（图 `40k_UI_bt_back` 88×87 · 文案 'Back'）
            var btn = MenuDraw.Node(node, "Close button", PrevBackBtnR);
            var bg = MenuDraw.Rect(btn, Tex("40k_UI_bt_back"), PrevBackBtnR, "Bg", QPreviewArt, null, true);
            var tx = MenuDraw.Text(btn, PrevBackTxR,
                                   Loc.T("MainMenu/MainButtons/ButtonLabel/Back"), Color.white,
                                   "Text", 38f, QPreviewText, PrevBackTxR.W, 18f, 38f, 31.9f,
                                   face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(tx, PrevBackTxR);                 // 原版 align = Left/Middle
            // 原版那颗钮 `m_Transition = 2`(SpriteSwap)：base `40k_UI_bt_back` / hover `40k_UI_bt_back_hover_back`
            // / pressed `40k_UI_bt_back_pressed` —— ⚠️ **pressed 那张本地没有**（`ui_menu/` 0 命中）
            // ⇒ 传 null（按下时保持 hover 那帧）+ 出声，⛔ 不拿别的图顶。
            if (CardArt.MenuUi("40k_UI_bt_back_pressed") == null)
                Debug.Log("[Draft] 预览窗 `Close button` 的 **pressed** 图 `40k_UI_bt_back_pressed` **本地没有**"
                        + "（`Resources/Art/ui_menu/` 0 命中）⇒ 按下时保持 hover 那帧（⛔ 不拿别的图顶）");
            MenuDraw.Hit(btn, "Hit", PrevBackBtnR, QPreviewHit, ClosePackPreview, bg,
                         "40k_UI_bt_back", "40k_UI_bt_back_hover_back", null,
                         clickCue: WindowSounds.DefaultCloseCue);
            node.gameObject.SetActive(false);
            return node;
        }

        /// <summary>点包卡**空白处** ⇒ 预览包里那几张卡（原版 `BackgroundClick` → `PackPreview(pack)`）。
        /// ⚠️ 原版那颗 `previewPackSound` 的 clip 名查不到（S3 §五·4）⇒ 静音 + 出声一次。</summary>
        public bool ShowPackPreview(string packKey)
        {
            if (_previewRoot == null || _previewCards == null)
            {
                Debug.LogWarning("[Draft] 预览层**没建出来**（`_previewRoot == null`）⇒ 这次点击什么都不做");
                return false;
            }
            DraftPacks.PackDto pack = null;
            for (int i = 0; i < _offered.Count; i++)
                if (_offered[i] != null && _offered[i].key == packKey) { pack = _offered[i]; break; }
            if (pack == null)
            {
                Debug.LogWarning("[Draft] 预览 `" + packKey + "` **不在这一轮给出的包里** ⇒ 忽略"
                               + "（原版只认 `packsUIContainer` 里那几张的 `PackPreview`）");
                return false;
            }
            for (int i = _previewCards.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_previewCards.GetChild(i).gameObject);
            _previewCardViews.Clear();
            _previewCardEngineIds.Clear();
            _previewCardRects.Clear();

            // 原版 `PrebuiltPack.GetCardLibrary()` ⇒ 包内那几张卡（882/882 包恒 3 张）
            var defs = new List<CardDef>();
            var ids = pack.cardIds ?? new string[0];
            for (int k = 0; k < ids.Length; k++)
            {
                var def = CardDatabase.FindById(CardPool(), ids[k]);
                if (def == null)
                {
                    Debug.LogWarning("[Draft] 预览：包 `" + pack.packId + "` 第 " + (k + 1) + " 张卡 id `"
                                   + ids[k] + "` 在卡池里查不到 ⇒ 那一格**没画**（⛔ 不假装画了）");
                    continue;
                }
                defs.Add(def);
            }
            // 摆位：原版 `Content` 的 `HorizontalLayoutGroup`（`m_Spacing = 500` · `childAlignment = MiddleCenter`）
            // 排的是 `CardUI Reference` 的**布局框**（2.54×3.37），而画出来的是 `m_LocalScale 223.1` 放大后的
            // 567.6×751.67 ⇒ 「布局框中心 = 视觉框中心」。中心 x 见 `PackPreviewCardCx`。
            for (int i = 0; i < defs.Count; i++)
            {
                float cx = PackPreviewCardCx(i);
                var cell = new PxRect(cx - PrevCardW * 0.5f, PrevCardCy - PrevCardH * 0.5f,
                                      cx + PrevCardW * 0.5f, PrevCardCy + PrevCardH * 0.5f);
                var cellNode = MenuDraw.Node(_previewCards, "CardUI Reference", cell);
                var cv = CardView.Create(cellNode, BattleDriver.ToCardData(defs[i], defs[i].Faction), "CardUI");
                if (cv == null) continue;
                float sc = PrevCardH / (CardView.Height * 108f);
                cv.SetPose(MenuDraw.Local(cellNode, cell.x1, cell.y1, cell.x2, cell.y2), 0f, sc);
                cv.SetQueue(QPreviewCardBase + i);
                _previewCardViews.Add(cv);
                _previewCardEngineIds.Add(defs[i].Id);   // ⚠️ 必须在这句**之后**（`cv == null` 那条已 continue）
                _previewCardRects.Add(cell);
            }
            _previewRoot.gameObject.SetActive(true);
            Debug.Log("[Draft] 预览包 `" + pack.packId + "`（" + pack.packName + "）⇒ 画了 " + _previewCardViews.Count
                    + " 张卡（原版 `PackPreview` → `UIMultiCardDisplay.Initialize(GetCardLibrary(pack), null)`；"
                    + "🔴 `previewPackSound` 的 clip 名查不到 ⇒ 静音）");
            return true;
        }

        /// <summary>关掉预览层（唯一关窗路 = `Close button` / ESC / 换页）。</summary>
        public void ClosePackPreview()
        {
            if (_previewRoot == null) return;
            if (!_previewRoot.gameObject.activeSelf) return;
            _previewRoot.gameObject.SetActive(false);
            if (_previewCards != null)
                for (int i = _previewCards.childCount - 1; i >= 0; i--)
                    RewardsWindow.DestroySafe(_previewCards.GetChild(i).gameObject);
            _previewCardViews.Clear();
            _previewCardEngineIds.Clear();
            Debug.Log("[Draft] 关预览层（原版是另一扇窗 `UIMultiCardDisplay.Close()`）");
        }

        /// <summary>预览层开着没有（自检读口）。</summary>
        public bool PackPreviewVisible { get { return _previewRoot != null && _previewRoot.gameObject.activeSelf; } }
        /// <summary>预览层节点（自检读口 —— 原版 prefab 名 `Generic Multi Card Display`）。</summary>
        public Transform PackPreviewNode { get { return _previewRoot; } }
        /// <summary>预览层里**真的建出来**几张卡（自检读口）。</summary>
        public int PackPreviewCardCount { get { return _previewCardViews.Count; } }
        /// <summary>预览层那张 `Header Text` 现在显示没有（原版 `PackPreview` 传 null ⇒ **关**）。</summary>
        public bool PackPreviewHeaderVisible
        {
            get { return _previewHeader != null && _previewHeader.gameObject.activeSelf; }
        }
        /// <summary>预览里第 <paramref name="i"/> 张卡**建的时候那份矩形**（自检读口；`false` = 没这一张）。
        /// ⚠️ 它记的是**喂给 `MenuDraw.Node` / `CardView.SetPose` 的同一个框**（不是另算一遍），
        /// 「节点真的落在那个框上」由 `MenuDraw` 那一层保证 —— 与本仓其它版面断言同一分工。</summary>
        public bool PackPreviewCardRectAt(int i, out PxRect r)
        {
            if (i < 0 || i >= _previewCardRects.Count) { r = default(PxRect); return false; }
            r = _previewCardRects[i];
            return true;
        }
        /// <summary>预览里第 <paramref name="i"/> 张卡的**引擎 id**（自检读口 —— 断「真的是这个包的 3 张」）。
        /// 返回 `CardDef.Id`（= `pack.cardIds[i]` 解出来的那一张），⛔ **不是显示名** ——
        /// `CardView.Data.id` 存的是显示名（`BattleDriver.ToCardData` 的 `id = c.Name`），语义不同。
        /// （2026-10-24 第七十三会话 E12 订正：原 doc 写「显示的卡 id」，实现当时返回的其实是显示名。）</summary>
        public string PackPreviewCardIdAt(int i)
        {
            if (i < 0 || i >= _previewCardEngineIds.Count) return null;
            var id = _previewCardEngineIds[i];
            return string.IsNullOrEmpty(id) ? null : id;
        }


        // ============================================================ 换页（= 原版 `GetTab<T>()`）
        /// <summary>state → 页节点（原版 `GetTabFromState` 那张表）。
        /// ⚠️ `Intro` / `PayScreen` **都落 Pay 页**（`GetTabFromState(0)` 的 else 分支 —— S3 §〇·3 / §七·1）；
        /// `OngoingRun` / `Finished` 同落 Ongoing 页（只是显隐不同）。</summary>
        Transform PageFor(DraftModeState s)
        {
            switch (s)
            {
                case DraftModeState.SelectWarlord: return _pageWarlord;
                case DraftModeState.SelectPack: return _pagePacks;
                case DraftModeState.OngoingRun:
                case DraftModeState.Finished: return _pageOngoing;
                default: return _pagePay;      // Intro / PayScreen
            }
        }

        public void GoToState(DraftModeState s)
        {
            // 换页 ⇒ 预览层先收（原版那是另一扇窗，`ChangeTabCO` 不会带着它走）
            if (s != State && PackPreviewVisible) ClosePackPreview();
            // `Collected`（22）：原版「窗口自行关闭/刷新」（S3 §一 的表）⇒ 先记下状态、再关窗。
            // ⚠️ **`State` 要写**（原版那一刻 `DraftState` 真的变成了 22，`UpdateState` 的
            //    `rewardsCollected ? 22 : 20` 就是给它写的）—— 只是这一档没有独立页。
            if (s == DraftModeState.Collected)
            {
                State = s;
                if (PackPreviewVisible) ClosePackPreview();   // 预览开着也要能关窗（`Close()` 覆写会先关预览）
                Debug.Log("[Draft] 状态 `Collected(22)` ⇒ 关窗（原版那一档没有独立页）。");
                Close();
                return;
            }
            var page = PageFor(s);
            State = s;
            SetOn(_pagePay, page == _pagePay);
            SetOn(_pageWarlord, page == _pageWarlord);
            SetOn(_pagePacks, page == _pagePacks);
            SetOn(_pageOngoing, page == _pageOngoing);

            bool showDeckPanel = (s == DraftModeState.SelectPack || s == DraftModeState.OngoingRun);
            SetOn(_deckPanelRoot, showDeckPanel);
            // 原版 `DraftModeDeckInfoPanel.Initialize(draft, showCardCounter)`：**SelectPack 页传 `true`、
            //   Ongoing 页传 `false`**，那个第 2 参控制 `CardCounter` 整节点的显隐
            //   （`DraftModeSelectPacksStateDemo__OnOpen` 传 1；S3 §2.9 + D2 §2.4 现读）。
            _deckShowCounter = (s == DraftModeState.SelectPack);
            SetOn(_rewardPanelRoot, s == DraftModeState.Finished);
            // Finished：隐藏 `To Battle` / `Reset Event Button` / `Timer`，显示 `Quote End`（S3 §2.7 判据）
            bool finished = (s == DraftModeState.Finished);
            SetOn(_toBattleNode, !finished);
            SetOn(_resetNode, !finished);
            if (_quoteEnd != null) _quoteEnd.gameObject.SetActive(finished);
            if (finished && _quoteEnd != null) _quoteEnd.SetText(Loc.T(QuoteTermForWins(Victories)));

            if (page == _pageWarlord) RebuildWarlordCards();
            if (page == _pagePacks) RefreshPacksPage();
            if (page == _pageOngoing) RefreshOngoingPage();
            if (showDeckPanel) RefreshDeckPanel();
            Debug.Log("[Draft] 换页 ⇒ " + s + "（页 = " + (page != null ? page.name : "<none>") + "）");
        }

        static void SetOn(Transform t, bool on) { if (t != null) t.gameObject.SetActive(on); }

        /// <summary>`Quote End` 的本地化键 —— 按**胜场**分档（五个键的名字本身就是档位：
        /// `Demo/DraftMode/Quote{0,1-4,5-8,9-11,12}Victories`，官方本地化表现读）。
        /// ⚠️ 原版 `DraftRunDisplay.ChooseQuote` 算的是 `wins / data.matches.Length` 归一值再比
        /// `DraftEndQuote.normalizedVictoriesRange`，而 `matches.Length` 是**服务端值、本地 0 命中**
        /// （S3 §五）⇒ 归一值算不出来；本地**唯一**能用的判据就是那五个键名 ⇒ 按胜场取档。
        /// 🔴 这一条是「用键名反推档位」，⛔ 不是原版的归一算法。</summary>
        public static string QuoteTermForWins(int wins)
        {
            if (wins <= 0) return "Demo/DraftMode/Quote0Victories";
            if (wins <= 4) return "Demo/DraftMode/Quote1-4Victories";
            if (wins <= 8) return "Demo/DraftMode/Quote5-8Victories";
            if (wins <= 11) return "Demo/DraftMode/Quote9-11Victories";
            return "Demo/DraftMode/Quote12Victories";
        }


        // ============================================================ 状态机（原版 `DraftEventBase.UpdateState`）
        /// <summary>**唯一**决定「现在该在哪一页」的地方 —— 原版 `DraftEventBase.UpdateState`
        /// （泛型实例 VA **`0x181814090`**，逐句反汇编解出）的本地等价物。
        ///
        /// <para>原版有 **6 个**调用点（`grep` 那 6 个闭包逐条核过）：`OnMatchFinished`→`HandleComplete`、
        /// `PickWarlord` 成功、`PickPack` 成功、`FreeEventEntrance` 成功、`PayEventEntrance` 成功、
        /// **`FinishRun` 的 `Callback`**。（`..._DisplayClass15_0_..._HandleComplete_0.c` /
        /// `..._DisplayClass23_0_..._b__0.c` / `..._DisplayClass21_0_..._b__0.c` /
        /// `..._DisplayClass32_0_..._OnSuccess_1.c` / `..._DisplayClass31_0_..._OnSuccess_1.c` /
        /// `..._DisplayClass33_0_..._FinishRun_g__Callback_0.c`）⇒ 我们那六条动作也**只**走这里。</para>
        ///
        /// <para>原版的分支（指令流 → C#）：
        /// ① `save.joinState == 2 (Finished)` **或** `save.matchesLost >= data.maxTries`
        ///    **或** `save.matchesWon >= data.matches.Length` ⇒ 收尾：`state = rewardsCollected ? 22 : 20`。
        /// ② 否则若 `selectedWarlord == null` ⇒ `selectedCards.Clear()` + `eventDeck = null`，
        ///    再 `state = (joinState == Joined && payState == notPaid) ? 0 (Intro) : 5 (SelectWarlord)`。
        /// ③ 否则 `state = (selectedPacks.Count < draftRounds) ? 10 (SelectPack) : 15 (OngoingRun)`。</para>
        ///
        /// <para>🔴 **① 的第三条我们办不到**：`data.matches`（`StagedEventData+0x88`，`EventMatchInfo[]`）
        /// 是服务端下发的比赛表，**本地 0 命中**（S3 §五）⇒ 我们**不设胜场上限**（胜负只在「输满
        /// <see cref="MaxTries"/> 场」与显式 <see cref="FinishRun"/> 两处收尾）。
        /// ⛔ 别自己编一个「赢够 N 场就结束」—— 那会把玩家的一局提前掐掉。</para></summary>
        public void UpdateState()
        {
            if (_run == null)
            {
                // 没有存档 = 状态机没有输入（原版那一刻 save 为 null 会直接崩）⇒ 如实出声、别静默乱跳。
                Debug.LogWarning("[Draft] `UpdateState()` 时**没有一局存档**（`_run == null`）—— 不动页面。");
                return;
            }
            bool finished = _run.joinState == (int)DraftJoinState.Finished
                            || _run.matchesLost >= MaxTries;
            if (finished)
            {
                _run.joinState = (int)DraftJoinState.Finished;      // 一旦收尾就钉住（原版 `set_DraftState` 那半边）
                GoToState(_run.rewardsCollected ? DraftModeState.Collected : DraftModeState.Finished);
                return;
            }
            if (string.IsNullOrEmpty(_warlordId))
            {
                if (_selectedCards.Count > 0)
                {
                    Debug.Log("[Draft] `UpdateState`②：还没选战将 ⇒ 清掉 `selectedCards`（"
                            + _selectedCards.Count + " 张）与 `eventDeck`（原版逐句如此）");
                    _selectedCards.Clear();
                    _run.selectedCards = new string[0];
                }
                _deck = null;
                bool intro = _run.joinState == (int)DraftJoinState.Joined && _run.payState == (int)DraftPayState.NotPaid;
                GoToState(intro ? DraftModeState.Intro : DraftModeState.SelectWarlord);
                return;
            }
            GoToState(_pickedKeys.Count < DraftPacks.RoundsPerDraft
                      ? DraftModeState.SelectPack : DraftModeState.OngoingRun);
        }

        /// <summary>原版 `DraftEventBase.FinishRun(onFinish)`（Slot 101）的本地等价物 —— **显式收尾**那一支：
        /// 判据 = `..._FinishRun_g__Callback_0.c` 的**逐句**：① `set_DraftState(0x14)`（= Finished(20)）
        /// ② `UpdateState()` ③ 回调 ④ `Signal.Raise(new DraftRunFinished(draft))`。
        /// ⚠️ 反编译里**搜不到它的调用点**（接口方法 Slot 101，调用方多半在服务端/大厅流程里）⇒
        /// 我们保留它给「对局已打完但服务端判定收尾」这类入口用，并在日志里说清是**显式**那一支。</summary>
        public void FinishRun()
        {
            if (_run == null) { Debug.LogWarning("[Draft] `FinishRun()` 没有存档 ⇒ 忽略"); return; }
            _run.joinState = (int)DraftJoinState.Finished;
            Debug.Log("[Draft] `FinishRun()`：显式置 `joinState = Finished(2)`（原版先 `set_DraftState(20)`、"
                    + "再 `UpdateState()` —— 后者会把它落成 Finished(20) 或 Collected(22)）");
            UpdateState();
        }

        // ---------------- 战斗结果回灌（原版 `OnMatchFinished` → `RecordEventMatchFinished`）
        /// <summary>把「已经打完、还没算进本局」的轮抽对局吃进战绩（原版 `DraftEventBase.OnMatchFinished`
        /// 「`save.numberOfMatchesWon/Lost` 由服务端 +1」那一步的本地等价物）。
        /// 数据源 = **既有的本地对局记录** `Shell/BattleLogData.cs`（结算处 `RecordBattleLog()` 写、且落盘）。
        /// <returns>这次吃了几场（0 = 没有新的）。</returns></summary>
        public int IngestBattleResults() { return IngestBattleResults(BattleLogData.All); }

        /// <summary>同上，但**日志从参数来**（自检用：⛔ 不去改全局 `BattleLogData` 的那张表）。</summary>
        public int IngestBattleResults(IList<BattleLogData.Match> log)
        {
            if (_run == null) return 0;
            var outcomes = DraftOutcomes(log);                     // 新 → 旧
            int pending = outcomes.Count - _run.consumedDraftMatches;
            if (pending < 0)
            {
                // 只可能来自「记录被清/裁掉」（`BattleLogData` 只留 50 条）—— 出声，别静默把账算错。
                Debug.LogWarning("[Draft] 对局记录里的轮抽场次（" + outcomes.Count + "）**少于**本局已记的 "
                               + _run.consumedDraftMatches + " ⇒ 记录被清过/裁过；把游标拉回 " + outcomes.Count
                               + "（少掉的那几场**无法补记**）");
                _run.consumedDraftMatches = outcomes.Count;
                return 0;
            }
            if (pending == 0) return 0;
            ApplyOutcomes(_run, outcomes, pending);
            _run.consumedDraftMatches = outcomes.Count;
            Debug.Log("[Draft] 战绩回灌：吃了 " + pending + " 场轮抽对局 ⇒ 现在 " + _run.matchesWon + " 胜 "
                    + _run.matchesLost + " 负（原版这两格在服务端 `DraftEventSaveGame+0x48 / +0x4C`）");
            // 原版 `OnMatchFinished` → `HandleComplete` 的第 1 句就是 `UpdateState()`（战绩变了，状态可能收尾）
            UpdateState();
            return pending;
        }

        /// <summary>从对局记录里挑出**轮抽的对局**并按「新 → 旧」给胜负：`true` 胜 · `false` 负 · `null` 平。
        /// <para>判据：轮抽对局的模式号 = `GameMode.ClosedDeck`（原版 `DraftEventBase.get_EventPlayMode`；
        /// 我们 `LaunchBattle()` 走的就是 `BattleDriver.SetPendingPlayMode(ClosedDeck)`，
        /// 而 `RecordBattleLog()` 把 `PlayModeNames.Name(Ctx.PlayMode)` 原样写进 `Match.Mode`）。</para>
        /// <para>⚠️ 平局**不算进任何一边**（原版 `numberOfMatchesDraw` 是**第三格** `+0x50`，我们不复用它）。</para></summary>
        public static List<bool?> DraftOutcomes(IList<BattleLogData.Match> log)
        {
            var outp = new List<bool?>();
            if (log == null) return outp;
            string draftMode = PlayModeNames.Name(GameMode.ClosedDeck);
            for (int i = 0; i < log.Count; i++)
            {
                var m = log[i];
                if (m == null || m.Mode != draftMode) continue;
                outp.Add(m.Result == BattleLogData.Outcome.Victory ? (bool?)true
                       : m.Result == BattleLogData.Outcome.Defeat ? (bool?)false
                       : null);
            }
            return outp;
        }

        /// <summary>把 <paramref name="count"/> 条结果吃进战绩（纯函数，⛔ 不碰任何全局状态 —— 自检直接喂它）。
        /// `newestFirst` 的顺序对「胜/负各加几」没有影响（原版那两格也只是计数器）。</summary>
        public static int ApplyOutcomes(DraftRunSave run, IList<bool?> newestFirst, int count = -1)
        {
            if (run == null || newestFirst == null) return 0;
            int n = count < 0 ? newestFirst.Count : Mathf.Min(count, newestFirst.Count);
            int applied = 0;
            for (int i = 0; i < n; i++)
            {
                if (newestFirst[i] == true) { run.matchesWon++; applied++; }
                else if (newestFirst[i] == false) { run.matchesLost++; applied++; }
                // null = 平局 ⇒ 两边都不动（见 `DraftOutcomes` 的注）
            }
            return applied;
        }

        /// <summary>一份对局记录里有几条轮抽局（开新局时当基线用）。</summary>
        public static int CountDraftMatchesIn(IList<BattleLogData.Match> log)
        {
            if (log == null) return 0;
            string draftMode = PlayModeNames.Name(GameMode.ClosedDeck);
            int n = 0;
            for (int i = 0; i < log.Count; i++) if (log[i] != null && log[i].Mode == draftMode) n++;
            return n;
        }

        /// <summary>把**实例上**的进度写回存档（原版这些格子在服务端，我们每次变更都要同步一次 ——
        /// 窗一关就只剩 `_run` 了）。`why` 只进日志。</summary>
        void SyncRun(string why)
        {
            if (_run == null) return;
            _run.seed = _seed;
            _run.warlordId = _warlordId;
            _run.pickedKeys = _pickedKeys.ToArray();
            _run.selectedCards = _selectedCards.ToArray();
            Debug.Log("[Draft] 存档同步（" + why + "）：joinState=" + _run.joinState + " payState=" + _run.payState
                    + " 战绩 " + _run.matchesWon + "胜" + _run.matchesLost + "负 · 已选 " + _run.pickedKeys.Length
                    + " 包 / " + _run.selectedCards.Length + " 张");
        }

        /// <summary>胜场（原版 `numberOfMatchesWon`；`DraftRunDisplay` 的 `Victories` 数字就是它）。</summary>
        public int Victories { get { return _run != null ? _run.matchesWon : 0; } }
        /// <summary>败场（原版 `numberOfMatchesLost`；`Losses container` 亮几格就是它）。</summary>
        public int Defeats { get { return _run != null ? _run.matchesLost : 0; } }
        /// <summary>原版 `IDraftEvent.CurrentStage`（`DraftEventBase.get_CurrentStage`）—— 奖励面板按它挑宝箱档，
        /// 而 `DraftRewardPanel.Initialize` 读的是 `save+0x48`（= 胜场）⇒ **本属性 = <see cref="Victories"/>**。</summary>
        public int CurrentStage { get { return Victories; } }


        // ------------------------------------------------------------ 各页刷新
        void RefreshPacksPage()
        {
            if (_packsContainer == null) return;
            // `Warlord Image`：按选中战将的**方形立绘**喂（原版运行时喂 `warlord.fullHeroSprite`；
            // sprite 为空 ⇒ 整节点关）。我们走 `CardArt.TryDeckHero`（2048² 方形立绘，与排位窗同一条口径）。
            if (_warlordImgNode != null)
            {
                // 先清旧 quad（本页每次重刷都会重建这一层；不清就会一次叠一层）
                for (int i = _warlordImgNode.childCount - 1; i >= 0; i--)
                    RewardsWindow.DestroySafe(_warlordImgNode.GetChild(i).gameObject);
                Texture2D hero = null;
                bool ok = !string.IsNullOrEmpty(_warlordId) && CardArt.TryDeckHero(_warlordId, out hero) && hero != null;
                if (ok) MenuDraw.Rect(_warlordImgNode, hero, WarlordImgR, "Image", QPanelBack, null, true);
                _warlordImgNode.gameObject.SetActive(ok);      // 取不到 ⇒ 整节点关 + 出声
                if (!ok)
                    Debug.LogWarning("[Draft] `Warlord Image` 取不到方形立绘（" + (_warlordId ?? "<未选战将>")
                                   + "）⇒ **整节点关**（原版同解：`sprite == null ⇒ SetActive(false)`）。");
            }

            _offered = RollOffers();
            for (int i = _packsContainer.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_packsContainer.GetChild(i).gameObject);
            _packCardViews.Clear();

            _stageCounter.SetText((CurrentDraftRound + 1) + "/" + DraftPacks.RoundsPerDraft);
            MenuDraw.AlignLeft(_stageCounter, StageR);     // 换字之后要重新左对齐（原版 hAlign = Left）
            // `Reroll` 的价格（原版 `RefreshReRollPrice` 每次进这一页都会刷）：
            //   `cost == 0` ⇒ 原版 `PriceDisplay.SetupFree` 画「Free」；否则画数字。
            if (_rerollPrice != null)
                _rerollPrice.SetText(ReRollCost <= 0 ? Loc.T("MainMenu/Free") : ReRollCost.ToString());
            bool empty = _offered.Count == 0;
            if (_packHint != null) _packHint.gameObject.SetActive(empty);
            if (empty)
                Debug.LogWarning("[Draft] 第 " + (CurrentDraftRound + 1) + " 轮的包 = **0 个**（`RollRound` 返回空）"
                               + " ⇒ 这一页不实例化包卡（原版同解：有几张画几张、不报错不占位；提示句是我们自拟的）。");

            float cy = PacksContR.CY;
            for (int i = 0; i < _offered.Count; i++)
            {
                float x1 = PacksContR.x1 + i * (PackW + PacksContSpacing);
                var r = new PxRect(x1, cy - PackH * 0.5f, x1 + PackW, cy + PackH * 0.5f);
                BuildPackCard(_packsContainer, _offered[i], i, r);
            }
            Debug.Log("[Draft] 第 " + (CurrentDraftRound + 1) + " 轮给出 **" + _offered.Count + "** 个包"
                    + "（= `DraftPacks.RollRound(seed=" + _seed + ", round=" + CurrentDraftRound
                    + ", army=" + WarlordArmyId() + ", count=" + DraftPacks.PacksPerRoundOffered + ")` 的返回条数）");
        }

        void BuildPackCard(Transform parent, DraftPacks.PackDto pack, int index, PxRect r)
        {
            var node = MenuDraw.Node(parent, "Prebuilt Pack UI", r);
            var content = MenuDraw.Node(node, "Content", r);
            // 包底 + 橙色描边（原版 `BackgroundImage` = `UI_Deck_Information_submenu_Back` 九宫 18,18,18,18
            // · `Highlight` = `OctagonUI Border SDF` 九宫 52² 橙色 (1,0.558,0.078)）
            MenuDraw.Nine(content, Tex("UI_Deck_Information_submenu_Back"),
                          new PxRect(r.x1, r.y1, r.x2, r.y2), new Vector4(18f, 18f, 18f, 18f), 69f, 63f,
                          QPanelBack2, null, true, "BackgroundImage", new Vector4(9f, 9f, 9f, 9f));
            MenuDraw.Nine(content, Tex("OctagonUI_Border_SDF"),
                          new PxRect(r.x1 + 2f, r.y1 + 2f, r.x2 - 2f, r.y2 - 2f),
                          new Vector4(52f, 52f, 52f, 52f), 128f, 128f, QPanelArt,
                          new Color(1f, 0.558f, 0.078f, 1f), false, "Highlight", null, null, default(Vector2));

            MenuDraw.Text(content, Abs(r, PackTitleR), PackTitle(pack), Color.white,
                          "Pack title", 35f, QText, PackTitleR.W, 18f, 35f, 36f,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Nine(content, Tex("40k_Separator_Fade_Sides_Horizontal"), Abs(r, PackSepR),
                          new Vector4(63f, 0f, 63f, 0f), 128f, 4f, QPanelArt,
                          new Color(0.757f, 0.502f, 0.122f, 1f), true, "Separator",
                          new Vector4(63f / 2.89f, 0f, 63f / 2.89f, 0f));

            // 3 张缩略卡（每包恒 3 张 —— `DraftPacks.CardsPerPack`；🔴 布局是我们挑的，见 `PackCardsArea`）
            string[] ids = pack.cardIds ?? new string[0];
            float cw = PackCardsArea.W / 3f - 10f;                 // 3 列，列间 15
            float ch = PackCardsArea.H;
            for (int k = 0; k < ids.Length && k < 3; k++)
            {
                var def = CardDatabase.FindById(CardPool(), ids[k]);
                if (def == null)
                {
                    Debug.LogWarning("[Draft] 包 `" + pack.packId + "` 第 " + (k + 1) + " 张卡 id `" + ids[k]
                                   + "` 在卡池里查不到 ⇒ 那一格**没画**（⛔ 不假装画了）。");
                    continue;
                }
                float cx1 = r.x1 + PackCardsArea.x1 + k * (cw + 15f);
                var cellR = new PxRect(cx1, r.y1 + PackCardsArea.y1, cx1 + cw, r.y1 + PackCardsArea.y2);
                var cv = CardView.Create(content, BattleDriver.ToCardData(def, def.Faction), "CardUI");
                if (cv != null)
                {
                    float sc = ch / (CardView.Height * 108f);
                    cv.SetPose(MenuDraw.Local(content, cellR.x1, cellR.y1, cellR.x2, cellR.y2), 0f, sc);
                    cv.SetQueue(QCardBase + index * 3 + k);
                    _packCardViews.Add(cv);
                }
                var nameR = new PxRect(cellR.x1, r.y1 + PackNameArea.y1, cellR.x2, r.y1 + PackNameArea.y2);
                MenuDraw.Text(content, nameR, CardText.Name(def), Color.white, "Card Name", 18f, QText,
                              nameR.W, 12f, 18f, 18f, face: TmpFont.Face.LatinCardBody);
            }

            // `Select Button`（222.10×49.49 · 文案 `Battle/Prebattle/SelectButton`）
            var selR = Abs(r, PackSelR);
            var selNode = MenuDraw.Node(content, "Select Button", selR);
            var bg = MenuDraw.Rect(selNode, Tex("UI_Button_Mulligan"), selR, "Bg", QArt, null, true);
            var packSelTx = MenuDraw.Text(selNode, Abs(r, PackSelTxR), Loc.T("Battle/Prebattle/SelectButton"),
                                          Color.white, "Button Text", 36f, QText,
                                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(packSelTx, Label.VAlign.Capline, Abs(r, PackSelTxR));   // 原版 align 2/8192
            string key = pack.key;
            MenuDraw.Hit(selNode, "Hit", selR, QHit, () => PickPack(key), bg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            // `BackgroundClick`（点包卡**空白处** ⇒ 预览弹窗）：原版那颗 `Image(a=0) + UIGenericEventCatcher`
            // → `DraftModeSelectPacksStateDemo.PackPreview(pack)`（S3 §2.5 表「预览」那一行）。
            // 🔴 档必须**低于** `Select Button`（4170）—— 原版这一刻两颗钮是不同节点、按钮在上层，
            //    点按钮是「选包」、点空白才是「预览」（`PrebuiltPackUI` 的 `selectButton` / `previewButton`）。
            MenuDraw.Hit(content, "BackgroundClick", new PxRect(r.x1, r.y1, r.x2, r.y2), QHitContentMin - 1,
                         () => ShowPackPreview(key), null, null, null, null);
        }

        /// <summary>包名。原版 `PrebuiltPack.GetLocalizedName` 走
        /// `Localize("PackName/" + (nameRefId 非空 ? nameRefId : packId))`
        /// （`d:/2/tools/decomp_full/PrebuiltPack__GetLocalizedName.c`：`+0x28` = nameRefId、`+0x20` = packId）。
        /// 官方本地化里 **`PackName/*` 856 条、855 个 unique packId 全命中（含中文）** ⇒ 我们照同一条键取。
        /// <para>⚠️ 我们这份数据（`Resources/draft_packs.json`）**没有 `nameRefId` 这一列** ⇒ 一律走 `packId`
        /// （原版那一跳只在 `nameRefId` 非空时才不同；本地 855/855 用 `packId` 就全命中）。</para>
        /// <para>取不到（理论上不该发生）⇒ 回 `packName`，并**出声一次**（⛔ 不静默印英文）。</para></summary>
        static string PackTitle(DraftPacks.PackDto pack)
        {
            string id = pack.packId;
            string key = "PackName/" + id;
            string s = Loc.T(key);
            if (!string.IsNullOrEmpty(s) && s != key) return s;
            if (id != null && _warnedPackName.Add(id))     // `Add` 首次为 true ⇒ 同一条键只出声一次
                Debug.LogWarning("[Draft] 包名键 `" + key + "` 在本地化表里取不到 ⇒ 回退英文 `"
                               + (pack.packName ?? id) + "`"
                               + "（D2 现读官方表 `PackName/*` 855/855 全命中 ⇒ 出现这条要查数据/键名）");
            return pack.packName ?? id;
        }
        static readonly HashSet<string> _warnedPackName = new HashSet<string>();

        /// <summary>把「相对包卡左上角」的矩形搬成绝对坐标。</summary>
        static PxRect Abs(PxRect outer, PxRect off)
        {
            return new PxRect(outer.x1 + off.x1, outer.y1 + off.y1, outer.x1 + off.x2, outer.y1 + off.y2);
        }

        void RefreshOngoingPage()
        {
            // 🆕 (B)：`Victories` 那个数字 = `save.numberOfMatchesWon`（原版 `DraftRunDisplay.Initialize`
            // 第一句 `victoryCounterText.text = save+0x48.ToString()`）—— 不再是常量 0。
            if (_victoriesNum != null) _victoriesNum.SetText(Victories.ToString());
            RebuildStageMarks();
            RebuildDefeatMarks();
            if (_ongoingWarlordNode != null)
            {
                for (int i = _ongoingWarlordNode.childCount - 1; i >= 0; i--)
                    RewardsWindow.DestroySafe(_ongoingWarlordNode.GetChild(i).gameObject);
                Texture2D hero = null;
                bool ok = !string.IsNullOrEmpty(_warlordId) && CardArt.TryDeckHero(_warlordId, out hero) && hero != null;
                if (ok) MenuDraw.Rect(_ongoingWarlordNode, hero, OnWlImgR, "Image", QPanelBack, null, true);
                _ongoingWarlordNode.gameObject.SetActive(ok);     // 原版 §2.13 那份 `Warlord Image` 初始就是 off
            }
        }

        /// <summary>右栏三颗 toggle 的动作（生产路径 = 点 toggle；自检也直调这一个 —— 同一份实现）。</summary>
        public void SetDeckTab(int tab)
        {
            _deckTab = Mathf.Clamp(tab, 0, 2);
            RefreshDeckPanel();
        }

        void RefreshDeckPanel()
        {
            if (_deckPanelRoot == null) return;
            // 三颗互斥（原版 `ToggleGroup`）：后两页的 `SetActive` 由这里统一给
            for (int i = 0; i < 3; i++)
            {
                var tog = _deckPanelRoot.Find(DeckToggleNames[i]);
                if (tog == null) continue;
                var bg = tog.Find("button_bg");
                var q = bg != null ? bg.Find("Image") : null;
                var iq = q != null ? q.GetComponent<ImageQuad>() : null;
                var tex = Tex(i == _deckTab ? ToggleArtOnOff[0] : ToggleArtOnOff[1]);
                if (iq != null && tex != null) iq.SetTexture(tex, true);
            }
            int n = _selectedCards.Count;
            if (_cardsCounter != null)
                // 原版 `DraftModeDeckInfoEnergyBalancePanel.Initialize`：
                // `Concat(trans, count, "/", deckSize)` —— **`"Cards:"` 与数字之间没有空格**
                // （中文词条是「卡：」，全角冒号）。原来是 `Loc.T(...) + " " + n`，多了一个空格。
                _cardsCounter.SetText(Loc.T("Demo/EnergyBalance/Cards") + n + "/" + DeckSizeForDraft);
            // 页内容：0 = Deck Info（**原版 `Energy View panel` 那一整页**：标题 + 计数 + 能量平衡标题
            //                     + 卡背/战将卡 + 9 行费用分布 —— 这些件在原版**都属于这一页**，
            //                     切到另两页要一起关，判据 = prefab 的父链现读）
            //         1 = Card List（头部 + 已拿到的卡名）
            //         2 = Alliance Info（原版要联盟数据/排行榜，本地 0）⇒ 自拟一句提示 + 出声
            bool infoTab = _deckTab == 0;
            if (_deckTitle != null) _deckTitle.gameObject.SetActive(infoTab);
            // `CardCounter` 的显隐 = 「在 Deck Info 页」**且**「原版那一页的 `showCardCounter`」
            //   （SelectPack 页 true / Ongoing 页 false，见 `GoToState`）。
            if (_cardsCounter != null) _cardsCounter.gameObject.SetActive(infoTab && _deckShowCounter);
            if (_deckEnergyLabel != null) _deckEnergyLabel.gameObject.SetActive(infoTab);
            if (_deckInfoHolder != null)
            {
                for (int i = _deckInfoHolder.childCount - 1; i >= 0; i--)
                    RewardsWindow.DestroySafe(_deckInfoHolder.GetChild(i).gameObject);
                if (infoTab) BuildDeckInfoContent();
            }
            if (_deckTabHolder == null) return;
            for (int i = _deckTabHolder.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_deckTabHolder.GetChild(i).gameObject);
            _deckListCounter = null;
            if (_deckTab == 1)
            {
                // ---- 头部（原版 `Cards in deck panel > Warlord Name / Card counter / Separator`）----
                //   ⚠️ 绝对 y 按**我们那个框**排（见 `DeckListHeaderH` 那一组的注释），结构/字号/对齐照 prefab。
                var wlDef = string.IsNullOrEmpty(_warlordId) ? null : CardDatabase.FindById(CardPool(), _warlordId);
                float headY2 = DeckListR.y1 + DeckListHeaderH;
                var nameR = new PxRect(DeckListR.x1 + 6f, DeckListR.y1, DeckListR.x1 + 268f, headY2);
                // 原版 prefab 默认文本就是 'Warlord Name'（那是占位），运行时换成 `GetLocalizedCardName(战将卡)`。
                var nameTx = MenuDraw.Text(_deckTabHolder, nameR,
                                           wlDef != null ? CardText.Name(wlDef) : "Warlord Name",
                                           Color.white, "Warlord Name", 33f, QText, nameR.W, 18f, 33f, 33f,
                                           face: TmpFont.Face.LatinCardBody);
                MenuDraw.AlignLeft(nameTx, nameR);                                   // 原版 hAlign = Left
                MenuDraw.SetVAlign(nameTx, Label.VAlign.Middle, nameR);              // 原版 vAlign = Middle
                // 原版这颗是 `EverguildButton`：`WarlordTextButtonOnClick` → `CardDisplayWindow.ShowCard(战将卡)`
                MenuDraw.Hit(_deckTabHolder, "Warlord Name Hit", nameR, QHit, OpenWarlordCard, null, null, null, null);
                var cntR = new PxRect(DeckListR.x2 - 78.86f, DeckListR.y1, DeckListR.x2, headY2);
                var cntTx = MenuDraw.Text(_deckTabHolder, cntR, DeckListCounterString(n),
                                          new Color(0.783f, 0.783f, 0.783f, 1f), "Card counter", 28f, QText,
                                          cntR.W, 18f, 28f, 28f, face: TmpFont.Face.LatinCardBody);
                _deckListCounter = cntTx;
                MenuDraw.AlignRight(cntTx, cntR);                                    // 原版 hAlign = Right
                MenuDraw.SetVAlign(cntTx, Label.VAlign.Middle, cntR);
                // 分隔线（原版 `40k_Separator Fade Sides Horizontal` 九宫 63,0,63,0 · ppuMul 2.89）
                var sepR = new PxRect(DeckListR.x1, headY2 + 2f, DeckListR.x2, headY2 + 4.9f);
                var sepTex = Tex("40k_Separator_Fade_Sides_Horizontal");
                if (sepTex != null)
                    MenuDraw.Nine(_deckTabHolder, sepTex, sepR, new Vector4(63f, 0f, 63f, 0f), 128f, 4f, QPanelArt,
                                  new Color(0.757f, 0.502f, 0.122f, 1f), true, "Separator",
                                  new Vector4(63f / 2.89f, 0f, 63f / 2.89f, 0f));
                else
                    MenuDraw.Rect(_deckTabHolder, CardArt.Solid(), sepR, "Separator", QPanelArt,
                                  new Color(0.757f, 0.502f, 0.122f, 1f));

                int bodyRows = Mathf.Max(0, Mathf.FloorToInt((DeckListR.H - DeckListBodyTop) / DeckRowH));
                int rows = Mathf.Min(n, bodyRows);
                for (int i = 0; i < rows; i++)
                {
                    var def = CardDatabase.FindById(CardPool(), _selectedCards[i]);
                    float ry = DeckListR.y1 + DeckListBodyTop + i * DeckRowH;
                    var rr = new PxRect(DeckListR.x1, ry, DeckListR.x2, ry + DeckRowH);
                    MenuDraw.Text(_deckTabHolder, rr, (i + 1) + ". " + (def != null ? CardText.Name(def) : _selectedCards[i]),
                                  Color.white, "Card Row", 26f, QText, rr.W, 16f, 26f, 24f,
                                  face: TmpFont.Face.LatinCardBody);
                }
                if (n > bodyRows)
                    Debug.LogWarning("[Draft] `Card List` 页放不下 " + n + " 行（行高 " + DeckRowH + " · 可放 " + bodyRows
                                   + "）—— 原版这一页是 `Scroll View`（`ScrollRect v=1`），**我们这一版没有滚动**"
                                   + " ⇒ 多出来的 " + (n - bodyRows) + " 行**看不到**（⛔ 不假装画了）。");
            }
            else if (_deckTab == 2)
            {
                MenuDraw.Text(_deckTabHolder, new PxRect(DeckListR.x1, DeckListR.y1, DeckListR.x2, DeckListR.y1 + 120f),
                              OurText("联盟信息要服务端的联盟/排行榜数据，本机没有（这一句是我们自拟的）",
                                      "Alliance info needs server-side alliance and ranking data."),
                              new Color(1f, 0.85f, 0.4f, 1f), "Alliance Hint", 26f, QText, DeckListR.W,
                              16f, 26f, 24f, face: TmpFont.Face.LatinCardBody);
                Debug.Log("[Draft] `Alliance Info` 页要 `LeaderboardManager.GetCachedLeaderboard(...)` 的数据"
                        + "（原版那段调用见 S3 §2.9）—— 本机没有服务器 ⇒ **页留空 + 一句自拟提示**（⛔ 不编联盟数据）。");
            }
        }

        /// <summary>右栏 Deck Info 页那三块内容：**卡背（两层）+ 战将卡面 + 9 行费用分布**。
        /// <para>判据：几何 = `menu_dump … "Draft Mode Deck Info Panel" --depth 12 --relative` 现读；
        /// 分桶与归一化 = `d:/2/tools/decomp_full/DeckEnergyCostDrawer__Initialize.c` 反汇编现读 +
        /// `dump.cs:105906`（`NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`）。</para>
        /// <para>⚠️ **颜色是我们挑的**（原版 `Slider` 的 track/fill sprite 在外部依赖里、本地读不到
        /// —— prefab 那一行的 `Slider` 只列了字段、没有 sprite）；几何与文本口径**不是**我们挑的。</para></summary>
        void BuildDeckInfoContent()
        {
            var host = _deckInfoHolder;
            if (host == null) return;

            // ---- ① 卡背（原版 `Cardback` + `Cardback Front` 两层，运行时按阵营喂图）----
            string faction = WarlordFaction;
            var back = CardArt.CardBack(faction);
            if (back != null)
            {
                // 两件各带原版的 z 旋转（`Cardback` +1.60° / `Cardback Front` −3.43°，prefab `m_LocalRotation` 实读）。
                // 🔴 **`Cardback Front` 在 prefab 里是 `Cardback` 的【子件】**（`menu_dump` 的树深 **4**、锚点 stretch、
                //    `sizeDelta ≈ 0`）⇒ 父件那 +1.60° 先转、子件再叠自己那 −3.43° ⇒ **画出来的世界角 = −1.83°**。
                //    ⛔ 别把两件建成兄弟（那样子件世界角会是 −3.43°，且树的形状与 prefab 不符）。
                var cbBack = MenuDraw.Rect(host, back, DeckBackR, "Cardback", QCardBase - 1, null, true,
                                           rotZDeg: DeckBackRotZ);
                // 子件要挂在父件那颗 quad 下才叠得到 +1.60°；`cbBack` 只可能在贴图为空时是 null，而上面
                // `back != null` 已挡过 ⇒ 这一支**理论不可达**，真到了就出声（⛔ 不静默画一层错的）。
                if (cbBack == null)
                    Debug.LogWarning("[Draft] 右栏 `Cardback` 那颗 quad 没建出来 ⇒ `Cardback Front` 只能挂到 "
                                   + "`Energy View panel` 下，**父件那 +1.60° 叠不上去**（世界角会是 −3.43° 而不是 −1.83°）。");
                MenuDraw.Rect(cbBack != null ? cbBack.transform : host, back, DeckBackFrontR,
                              "Cardback Front", QCardBase - 1, null, true, rotZDeg: DeckBackFrontRotZ);
            }
            else
                Debug.LogWarning("[Draft] 右栏 `Cardback` 取不到（阵营 " + faction + "）⇒ 这一层没画（⛔ 不拿别的图顶）。");

            // ---- ② 战将卡面（原版 `CardUI-Warlord` = 一张 `BasicCardUI`，视觉 192.48×254.89）----
            var wlDef = string.IsNullOrEmpty(_warlordId) ? null : CardDatabase.FindById(CardPool(), _warlordId);
            if (wlDef != null)
            {
                var cv = CardView.Create(host, BattleDriver.ToCardData(wlDef, wlDef.Faction), "CardUI-Warlord");
                if (cv != null)
                {
                    float sc = DeckWarlordCardR.H / (CardView.Height * 108f);
                    cv.SetPose(MenuDraw.Local(host, DeckWarlordCardR.x1, DeckWarlordCardR.y1,
                                              DeckWarlordCardR.x2, DeckWarlordCardR.y2), 0f, sc);
                    cv.SetQueue(QCardBase);           // 卡背在 QCardBase−1 ⇒ 卡面压在上面
                }
            }

            // ---- ③ `Deck Information cost drawer`：底板 + 9 行 ----
            MenuDraw.Rect(host, CardArt.Solid(), DeckCostBgR, "Background", QPanelBack,
                          new Color(0.123f, 0.123f, 0.123f, 1f));
            var buckets = new int[DeckCostRows];
            for (int i = 0; i < _selectedCards.Count; i++)
            {
                var d = CardDatabase.FindById(CardPool(), _selectedCards[i]);
                if (d == null || d.Cost < 0) continue;        // 无费用（-1）那一档不计入（原版按 `card.cost` 分桶）
                // `card.cost < 行数−1` ⇒ 落第 cost 行；**否则全进最后一行**（原版那一支现读）
                buckets[Mathf.Min(d.Cost, DeckCostRows - 1)]++;
            }
            var costColor = new Color(0.296f, 0.774f, 0.496f, 1f);   // 原版 `Card Cost` 色 (0.296,0.774,0.496)
            // `Content`（原版那颗 VLG，`spacing 3.43`、9 行）—— 行挂在它下面（节点名逐字照 prefab）
            var costContent = MenuDraw.Node(host, "Content",
                                            new PxRect(DeckCostRow0R.x1, DeckCostBgR.y1,
                                                       DeckCostRow0R.x2, DeckCostBgR.y2));
            for (int i = 0; i < DeckCostRows; i++)
            {
                float y1 = DeckCostRow0R.y1 + i * DeckCostRowStep;
                var rowR = new PxRect(DeckCostRow0R.x1, y1, DeckCostRow0R.x2, y1 + DeckCostRow0R.H);
                var rowNode = MenuDraw.Node(costContent, "Deck CostQuanityt Row Drawer", rowR);
                // `Card Cost`（最后一行 = 「8+」，原版 `maxCostToDraw` 那一条 tooltip）
                var costR = new PxRect(rowR.x1 + DeckCostCostOff.x1, rowR.y1 + DeckCostCostOff.y1,
                                       rowR.x1 + DeckCostCostOff.x2, rowR.y1 + DeckCostCostOff.y2);
                MenuDraw.Text(rowNode, costR, i == DeckCostRows - 1 ? (DeckCostRows - 1) + "+" : i.ToString(),
                              costColor, "Card Cost", 25f, QText, costR.W, 12f, 25f, 25f,
                              face: TmpFont.Face.LatinCardBody);
                // `Slider`（fill = clamp(张数 / 10)）
                var slR = new PxRect(rowR.x1 + DeckCostSliderOff.x1, rowR.y1 + DeckCostSliderOff.y1,
                                     rowR.x1 + DeckCostSliderOff.x2, rowR.y1 + DeckCostSliderOff.y2);
                float ratio = Mathf.Clamp01(buckets[i] / DeckCostNormalization);
                var sliderNode = MenuDraw.Node(rowNode, "Slider", slR);
                MenuDraw.Rect(sliderNode, CardArt.Solid(), slR, "Track", QPanelBack2,
                              new Color(0f, 0f, 0f, 0.55f));
                if (ratio > 0f)
                    MenuDraw.Rect(sliderNode, CardArt.Solid(),
                                  new PxRect(slR.x1, slR.y1, slR.x1 + slR.W * ratio, slR.y2), "Fill", QPanelArt,
                                  costColor);
                // `Cards in deck`（张数）
                var cntR = new PxRect(rowR.x1 + DeckCostCountOff.x1, rowR.y1 + DeckCostCountOff.y1,
                                      rowR.x1 + DeckCostCountOff.x2, rowR.y1 + DeckCostCountOff.y2);
                MenuDraw.Text(rowNode, cntR, buckets[i].ToString(), Color.white, "Cards in deck", 25f, QText,
                              cntR.W, 12f, 25f, 25f, face: TmpFont.Face.LatinCardBody);
            }
        }

        // ============================================================ 动作（按钮回调 = 自检直调，同一份实现）
        /// <summary>Pay 页 `Free Button` ⇒ 选将页（原版 `FreeButtonOnClick` → `FreeEventEntrance` 成功后
        /// `GoToState(SelectWarlord)`；我们本地没有服务端 ⇒ 直接走成功那一支，并**出声说明**）。</summary>
        public void ClickFree()
        {
            if (State != DraftModeState.PayScreen && State != DraftModeState.Intro)
            { Debug.LogWarning("[Draft] `Free` 只在 Pay 页有效（当前 " + State + "）"); return; }
            Debug.Log("[Draft] `Free Button` ⇒ 进选将页（原版 `Draft.FreeEventEntrance` 是服务端调用、"
                    + "本地没有服务器 ⇒ **本地直接走成功那一支**，这一条是我们挑的；原版成功回调里调的是 `UpdateState`）。");
            if (_run != null)
            {
                _run.joinState = (int)DraftJoinState.Joined;      // 原版 `FreeEventEntrance` 成功 = 报名成功
                _run.payState = (int)DraftPayState.Free;          // `PayStates.free = 1`
                SyncRun("Free 报名成功");
            }
            UpdateState();      // 原版 `..._FreeEventEntrance_g__OnSuccess_1.c` 的第 2 句就是它
        }

        /// <summary>`Reroll` 那颗钮：把**这一轮**的包重新抽一遍（原版 `ReRollClicked` →
        /// `NetworkingPeer` 走服务端扣费/重抽；本地没有服务器 ⇒ 我们只做「重抽」那一半，
        /// **不扣钱**并在日志里说清）。
        /// <para>判据：`d:/2/tools/decomp_full/DraftModeSelectPacksStateDemo__ReRollClicked.c`
        /// （成功回调里 `save.currentReRolls++` 再 `ShowPacks()`）· `...__RefreshReRollPrice.c`
        /// （价格 = `GetReRollCosts(data, save+0xB0)`）· `DraftEventData__GetReRollCosts.c`（规则见 `ReRollCost`）。
        /// ⚠️ **原版没有「次数用完」的上限** ⇒ 我们也不设。</para>
        /// <para>🔴 **如实标**：原版那一刻是**服务端调用**（`FUN_1800020b0(7, …)` = `ILiveOps` 7 号槽），
        /// 扣费与合法性判定都在服务端；我们本地**只重抽、不扣费**（经济本来也是固定资源那一档）。
        /// 重抽的确定性 = `seed + reRolls×7919`（见 `RollOffers`）⇒ 同一次重掷永远同一组包。</para></summary>
        public bool ReRollPacks()
        {
            if (State != DraftModeState.SelectPack)
            { Debug.LogWarning("[Draft] `Reroll` 只在选包页有效（当前 " + State + "）"); return false; }
            if (_run != null) _run.reRolls++;
            int cost = ReRollCost;
            RefreshPacksPage();
            SyncRun("重掷了一次");
            Debug.Log("[Draft] `Reroll` ⇒ 第 " + (CurrentDraftRound + 1) + " 轮重新抽包（第 " + ReRolls
                    + " 次；价格 " + cost + (cost <= 0 ? " = 免费" : "") + "）—— "
                    + "🔴 原版这一刻走服务端扣费，本地**不扣钱**（只重抽）；`reRollCosts` 表空 ⇒ 恒免费。");
            return true;
        }

        /// <summary>`Card List` 页那颗「战将名」：原版 `DrafModeDeckInfoCardListPanel.WarlordTextButtonOnClick`
        /// → `CardDisplayWindow.Instance.ShowCard(战将卡)`（= 我们的 `CardDetailPopup`）。
        /// 开新的之前先把**本窗开过的**上一扇收掉（同 `CollectionWindow.OpenCardDetail` 那套处置；
        /// ⚠️ 那份的 `LastCardDetail` 是**那个文件的**静态，不属于本窗 ⇒ 这里自己记一份，⛔ 不越界去改它）。</summary>
        public void OpenWarlordCard()
        {
            var def = string.IsNullOrEmpty(_warlordId) ? null : CardDatabase.FindById(CardPool(), _warlordId);
            if (def == null)
            {
                Debug.LogWarning("[Draft] `Card List` 上面那颗「战将名」点不动：**还没选战将**"
                               + "（原版那一刻 `deck.warlordCard` 是空 ⇒ 它自己也是空引用）。");
                return;
            }
            if (Manager == null)
            {
                Debug.LogWarning("[Draft] 没有 `WindowsManager` ⇒ 开不了卡片详情窗（" + def.Name + "）。");
                return;
            }
            if (_warlordDetail != null)
            {
                if (_warlordDetail.CurrentState != WindowState.Closed) _warlordDetail.Close();
                RewardsWindow.DestroySafe(_warlordDetail.gameObject);
                _warlordDetail = null;
            }
            _warlordDetail = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(_warlordDetail);
            _warlordDetail.ShowCard(def);       // 原版 `ShowCard` 复用同一个窗（不新建）
            Debug.Log("[Draft] `Card List` 点战将名 ⇒ 开卡片详情窗：「" + def.Name + "」");
        }
        /// <summary>本窗开出来的那一扇卡片详情（重开时先收掉，见 `OpenWarlordCard`）。</summary>
        CardDetailPopup _warlordDetail;

        /// <summary>选一位战将（本地暂存，原版 `SelectWarlord(card)`；此时 `Continue` 才变可点）。</summary>
        public bool ChooseWarlord(string cardId)
        {
            if (State != DraftModeState.SelectWarlord)
            { Debug.LogWarning("[Draft] 选将只在 SelectWarlord 页有效（当前 " + State + "）"); return false; }
            bool found = false;
            for (int i = 0; i < _warlords.Count; i++)
                if (_warlords[i] != null && _warlords[i].Id == cardId) { found = true; break; }
            if (!found)
            {
                Debug.LogWarning("[Draft] 选的战将 `" + cardId + "` **不在本局候选里** ⇒ 忽略（原版 `UICardSelector` 只给候选卡）。");
                return false;
            }
            _warlordId = cardId;
            // 原版 `SelectWarlord(card)` 只做两件事：存卡 + `continueButton.interactable = true`
            // （`...__SelectWarlord.c`）；卡自己的高亮与「那格按钮灰掉」在 `GenericSelectCardContainer.ToggleSelected`
            // 里（`Card2DController.ToggleHighlight(true)` + `selectButton.interactable = false`）。
            // ⇒ 我们同步刷这三处可见反馈（⛔ 不重建卡、不重建命中区）。
            ApplyWarlordSelectionVisuals();
            Debug.Log("[Draft] 选中战将 `" + cardId + "`（阵营 " + WarlordFaction + " / army " + WarlordArmyId() + "）"
                    + "；`Continue` 现在可点（原版 `SelectWarlord` 末句）");
            return true;
        }

        /// <summary>`Continue` ⇒ 选包页（原版 `ContinueButtonOnClick` → `PickWarlord` 成功后
        /// `CheckWarlordSelectionFinished` → Packs 页）。没选战将时按钮**本来就是灰的**（原版
        /// `continueButton.interactable = false`，S3 §2.4）⇒ 这里如实拒绝并出声。</summary>
        public bool ContinueAfterWarlord()
        {
            if (State != DraftModeState.SelectWarlord) return false;
            if (string.IsNullOrEmpty(_warlordId))
            {
                Debug.LogWarning("[Draft] `Continue` 点不动：**还没选战将**（原版 `Continue` 出厂就是灰的，"
                               + "点了等于没点 —— 这一下不出弹窗，与 `interactable = false` 同观感）。");
                return false;
            }
            // 战将定下来之后，候选包池就定了（按**阵营**，不是按战将 —— `DraftPacks.RollRound` 的粒度）
            SyncRun("选定战将");
            UpdateState();      // 原版 `PickWarlord` 成功 → `..._b__0.c` 第一句就是 `UpdateState`
            return true;
        }

        /// <summary>选一个包：+3 张、轮次 +1；没到末轮 ⇒ 直接给下一轮的包；到末轮 ⇒ 进 Ongoing 页。
        /// （原版 `PackSelected` → `PickPack` → `UpdateDeckInfoPanelAnimSync`，S3 §2.5 那两行。）</summary>
        public bool PickPack(string packKey)
        {
            if (State != DraftModeState.SelectPack)
            { Debug.LogWarning("[Draft] 选包只在 SelectPack 页有效（当前 " + State + "）"); return false; }
            DraftPacks.PackDto pack = null;
            for (int i = 0; i < _offered.Count; i++)
                if (_offered[i] != null && _offered[i].key == packKey) { pack = _offered[i]; break; }
            if (pack == null)
            {
                Debug.LogWarning("[Draft] `" + packKey + "` **不在这一轮给出的包里** ⇒ 忽略（原版只认 `packsUIContainer` 里那几张）。");
                return false;
            }
            _pickedKeys.Add(pack.key);
            if (pack.cardIds != null) _selectedCards.AddRange(pack.cardIds);
            _deck = null;                     // 牌变了 ⇒ 之前组的那副作废
            // 原版这一刻：右栏 `DOPunchScale` + `UIEffectTweener.Play`（红底闪一下，S3 §2.5「进下一轮」那一行）。
            // 本轮不做动画（时长常量只在 `DAT_*` 里）—— 出声一次，⛔ 不假装播了。
            Debug.Log("[Draft] 选包成功：原版这里右栏会 `DOPunchScale` + 闪一下红底（`UIEffectTweener`）—— "
                    + "**本轮不做动画**（S3 §五·3：时长/缓动只有 `DAT_*` 符号，查不到真值）。");
            Debug.Log("[Draft] 选包 `" + pack.packId + "`（" + pack.packName + "）⇒ 手里 "
                    + _selectedCards.Count + " 张 / 第 " + CurrentDraftRound + " 轮完成");
            SyncRun("选了一包");
            // 原版 `PickPack` 成功 → `..._b__0.c` 末句 `UpdateState()`：
            // `CurrentDraftRound < draftRounds` ⇒ `SelectPack`（并 `ShowPacks()` 再来一轮）；否则 `OngoingRun`。
            // 🔴 **「4 轮打完」在这里 = `OngoingRun(15)`，⛔ 不是 `Finished(20)`**（判据 → `UpdateState` 的注释）。
            UpdateState();
            return true;
        }

        /// <summary>`To Battle`：组 ClosedDeck ⇒ 交给既有开战通道 ⇒ 切战场（批处理只记账）。
        /// ⚠️ **战斗内一行不动**。</summary>
        public bool LaunchBattle()
        {
            if (State != DraftModeState.OngoingRun)
            { Debug.LogWarning("[Draft] `To Battle` 只在 OngoingRun 页有效（当前 " + State + "）"); return false; }
            if (string.IsNullOrEmpty(_warlordId))
            {
                Debug.LogWarning("[Draft] 没有战将 ⇒ 开不了局（原版 `GetClosedDeckByPackIds(warlordId, …)` 需要它）。");
                if (Manager != null) Manager.ShowPopUp(Loc.T("MenuDeck/Error/CantStartNoWarlord"),
                                                       Loc.T("MainMenu/General/OK"), null);
                return false;
            }
            if (_pickedKeys.Count == 0)
            {
                Debug.LogWarning("[Draft] 一个包都没选 ⇒ 开不了局。");
                if (Manager != null) Manager.ShowPopUp(Loc.T("Battle/Tips/NoCards"), Loc.T("MainMenu/General/OK"), null);
                return false;
            }
            // `ClosedDeck.warlordId` 是 **int**（原版 `GetClosedDeckByPackIds` 的标记位、不参与选卡 —— D1 已核）；
            // 我们的战将 id 是字符串（`AM3`）⇒ 这里放**阵营 id** 当标记。🔴 这一条是我们挑的。
            var deck = DraftPacks.BuildClosedDeck(WarlordArmyId(), _pickedKeys);
            if (deck == null) return false;
            _deck = deck;

            // 交给既有通道：`PrebuiltDecks.PendingSource` + `BattleDriver._pendingPlayMode`
            var pre = new PrebuiltDecks.Deck
            {
                deckId = "draft_" + _seed,
                name = "Draft " + _seed,
                nameZh = "选秀 " + _seed,
                faction = WarlordFaction,
                armyOrder = WarlordArmyId(),
                // 🔴 **我们挑的**：`GameplayVariables.For` 里**没有 ClosedDeck 那一档**
                // （`RuleEngine/Core/GameplayVariables.cs:444-449` ⇒ 落到 Classic **30 张**），
                // 而轮抽牌恒 12 张（4 包 × 3）⇒ 这副牌按 **Skirmish（12 张 / 4 传说）** 那套参数打；
                // 真实模式号仍走 `SetPendingPlayMode(GameMode.ClosedDeck)`（日志/记录/骷髅口径用那个）。
                gameMode = (int)GameMode.Skirmish,
                difficulty = 0,
                heroId = _warlordId,
                cardback = null,
                cardbackFrom = "none",
                cardIds = deck.cardIds,
                complete = true,
                missing = new string[0],
                problems = new string[0],
                overLegendary = new string[0],
            };
            PrebuiltDecks.SetPendingBattleDeck(pre);
            BattleDriver.SetPendingPlayMode(GameMode.ClosedDeck);
            StartedBattle = true;

            string scene = ArenaByArmy.BattleSceneNameFor(WarlordFaction);
            Debug.Log("[Draft] 开战：`ClosedDeck` · 战将 `" + _warlordId + "` · " + deck.cardIds.Length
                    + " 张（" + deck.packIds.Length + " 包）· 战场 `" + scene + "`"
                    + "（本局模式号 = `ClosedDeck(7)`；⚠️ 参数表按 `Skirmish` 那套 12 张 —— 见 `LaunchBattle` 注释）");
            if (Application.isBatchMode)
            {
                Debug.Log("[Draft] （批处理：不切场景，只记账 —— `StartedBattle = true`、"
                        + "`PendingPlayMode = " + BattleDriver.PendingPlayMode + "`）");
                return true;
            }
            ShellRuntime.LoadSceneWithFade(scene);
            return true;
        }

        /// <summary>`Reset Event Button`（原版文案 = `Demo/DraftMode/ResignDraftButton`「放弃」）：
        /// 原版 `ResignButtonClick` → `ShowPopUp(ExitDraftQuestion)` 二选一 → 确认后 `RefundRun`（S3 §三·16）。
        /// 我们：弹同一句官方词条，**OK = 重开一轮**（退款/服务端那半边本地没有 ⇒ 不假装退了什么）。</summary>
        public void Resign()
        {
            Debug.Log("[Draft] `Resign`（原版 `Draft.RefundRun` 是服务端调用 —— 本地没有服务器 ⇒ "
                    + "**只重开一轮、不假装退款**；顺手把本地那一局存档清掉 = 原版 `RefundRun` 的等价物）。");
            _run = null;                    // 放弃本轮 ⇒ 这一局作废（否则 `BeginRun` 之外的路径会接着它）
            if (Manager != null)
                Manager.ShowPopUp(Loc.T("Demo/DraftMode/ExitDraftQuestion"), Loc.T("MainMenu/General/OK"),
                                  () => BeginRun(NewSeed()));
            else
                BeginRun(NewSeed());
        }

        /// <summary>`Collect Button`（只有 Finished 有）：原版 `Draft.CollectRewards` 是服务端调用，
        /// 发什么奖在 `DraftEventData.Rewards`（本地 0 命中）⇒ 我们**如实说「奖励表在服务端」**，
        /// 然后走 `Collected`（关窗）。⛔ 不发假奖。</summary>
        public void CollectReward()
        {
            Debug.LogWarning("[Draft] `Collect reward`：**奖励表（`DraftEventData.Rewards`）在服务端、本地 0 命中**"
                           + " ⇒ 本窗**不发任何奖励**，只把 `rewardsCollected` 置真并走 `UpdateState`"
                           + "（原版 `Draft.CollectRewards` 成功后 `rewardsCollected = true` ⇒ 状态从 Finished(20) 变 Collected(22)）。"
                           + "⛔ 不编奖励（铁律：查不到就说查不到）。");
            if (_run != null) _run.rewardsCollected = true;
            UpdateState();                 // ⇒ `Collected` ⇒ `GoToState` 里关窗
            // 原版 `ResetCollectedDraft`：领完就把这一局清掉（否则下次开窗会接着一局已领奖的收尾页）
            _run = null;
        }

        // ============================================================ 抽将 / 抽包 / 查表
        static List<CardDef> _cardPool;
        static List<CardDef> CardPool()
        {
            if (_cardPool == null) _cardPool = CardDatabase.Load();
            return _cardPool;
        }

        /// <summary>本局的候选择将 —— **按 seed 确定性抽**（原版 `GetRandomWarlords()` 的**方法体缺失**
        /// （泛型基类里，S3 §七·2）⇒ 形状我们照 `RollRound` 那一条：逐位一份 `System.Random`）。
        /// 🔴 候选池 = 本机卡池里**所有** `type == "hero"` 的卡（原版由 `specificArmies/specificWarlords/
        /// guaranteedArmies/blacklistedWarlords` 过滤，**那四个都在服务端**、我们取空 ⇒ 全池）。</summary>
        public static List<CardDef> RollWarlords(int seed, int count)
        {
            var pool = new List<CardDef>();
            var all = CardPool();
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Type == "hero") pool.Add(all[i]);
            var outp = new List<CardDef>(count);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int slotSeed = unchecked((seed * 83492791) ^ (i * 19349663) ^ 0x5F3A1);
                int idx = new System.Random(slotSeed).Next(pool.Count);
                outp.Add(pool[idx]);
                pool.RemoveAt(idx);        // 同一局不重复
            }
            return outp;
        }

        /// <summary>这一轮给出的包 —— **就是 `DraftPacks.RollRound` 的返回**（⛔ 本类不另写抽样；
        /// `count` 也直接取 `DraftPacks.PacksPerRoundOffered`，不写死 3）。</summary>
        List<DraftPacks.PackDto> RollOffers()
        {
            var excl = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _pickedKeys.Count; i++)
            {
                excl.Add(_pickedKeys[i]);
                var p = DraftPacks.Resolve(_pickedKeys[i]);
                if (p != null) excl.Add(p.packId);
            }
            // 🔴 重掷的**唯一做法** = 换盐（原版 `ShowPacks` 用 `Random.InitState(seed + round + i)` 逐轮
            //    逐位确定；我们这份是 `RollRound(seed, round, …)` ⇒ 把 `seed` 换成一个**仍然可复现**的数）。
            //    `7919` 只是「不同次数给不同盐」的一个质数常量（**我们挑的**）；`reRolls` 会进日志与存档
            //    ⇒ 同 `seed + reRolls` 永远重放出同一组包。
            int rollSeed = unchecked(_seed + ReRolls * 7919);
            return DraftPacks.RollRound(rollSeed, CurrentDraftRound, WarlordArmyId(),
                                        DraftPacks.PacksPerRoundOffered, excl);
        }

        /// <summary>选中战将的阵营名（空 = 还没选 ⇒ 回落到 `Ultramarines`，只为让 UI 有个确定的池子；
        /// 调用点都会先拦住「没选战将」那一档）。</summary>
        public string WarlordFaction
        {
            get
            {
                if (string.IsNullOrEmpty(_warlordId)) return DeckRuntime.NoArmyFaction;
                for (int i = 0; i < _warlords.Count; i++)
                    if (_warlords[i] != null && _warlords[i].Id == _warlordId) return _warlords[i].Faction;
                var def = CardDatabase.FindById(CardPool(), _warlordId);
                return def != null ? def.Faction : DeckRuntime.NoArmyFaction;
            }
        }

        /// <summary>选中战将的 `CardArmy` 值（`DraftPacks.RollRound` 的过滤键）。</summary>
        public int WarlordArmyId() { return ArmyIcons.Id(WarlordFaction); }

        /// <summary>`Stage Counter` 现在印的字（自检读口 —— 「文本 = {轮次+1}/{轮数}」那条断言读它）。</summary>
        public string StageCounterText { get { return _stageCounter != null ? _stageCounter.Text : null; } }
        /// <summary>`Packs Container` 底下**真的建出来**几个包卡（自检读口 —— 断「= `RollRound` 的返回条数」）。</summary>
        public int VisiblePackCount { get { return _packsContainer != null ? _packsContainer.childCount : 0; } }
        /// <summary>选将页真的建出来几张卡（自检读口）。</summary>
        public int VisibleWarlordCount { get { return _warlordNodes.Count; } }
        /// <summary>右栏 `Card Counter` 现在印的字。</summary>
        public string DeckCounterText { get { return _cardsCounter != null ? _cardsCounter.Text : null; } }
        /// <summary>页节点（自检读口，按 state 取）。</summary>
        public Transform PageNode(DraftModeState s) { return PageFor(s); }
        /// <summary>右栏面板节点（自检读口 —— 显隐按页变，见 `GoToState`）。</summary>
        public Transform DeckInfoPanelNode { get { return _deckPanelRoot; } }
        /// <summary>奖励面板节点（只有 `Finished` 显示）。</summary>
        public Transform RewardPanelNode { get { return _rewardPanelRoot; } }
        /// <summary>Ongoing 页 `Victories text Number` 现在印的字（自检读口 —— 断「胜场来自真战绩」）。</summary>
        public string VictoriesText { get { return _victoriesNum != null ? _victoriesNum.Text : null; } }
        /// <summary>`Quote End` 现在印的字（只有 `Finished` 显示）。</summary>
        public string QuoteText { get { return _quoteEnd != null ? _quoteEnd.Text : null; } }
        /// <summary>`To Battle Button` 节点（Finished 时要藏起来 —— S3 §2.7 判据）。</summary>
        public Transform ToBattleNode { get { return _toBattleNode; } }

        /// <summary>我们自拟的文案（**只在原版确实没有节点/词条时用**）—— 中英按当前语档挑，
        /// 每一条的调用点都写明「这一句是我们自拟的」。形状照 `Shell/CampaignTab.cs:1314` 那一条。</summary>
        static string OurText(string zh, string en)
        {
            return Loc.Current == AvailableLanguages.Chinese ? zh : en;
        }
    }
}
