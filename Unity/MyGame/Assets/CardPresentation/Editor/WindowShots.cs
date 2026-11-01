// WindowShots.cs — **B-55 第 4 件：「单扇窗口 → PNG」的生产端宿主**（2026-11-01 · 第一百一十三会话 `X6`）
//
// 用法（⚠️ 必须用 Unity 批处理跑，见 CLAUDE.md §二；本文件**不跑 Unity**，只保证编得过）：
//
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod WindowShots.RunFromCLI -logFile -
//
//   只跑一个宿主 / 一扇窗：
//     … -executeMethod WindowShots.RunFromCLI -wfWinHost mainmenu
//     … -executeMethod WindowShots.RunFromCLI -wfWinOnly shop
//
//   空图闸的自检（灭自证 · 纯算术、不建场景）：
//     … -executeMethod WindowShots.GateSelfTest
//
// 🔴 **空图闸 = 结构判据**（2026-11-01 · `B-240`）：原来是「平均亮度 ≤ 3 ⇒ 疑似空图」，
//    而排位窗族自带整屏 `(0,0,0,0.773)` 黑遮罩 ⇒ 平均亮度天生 2.9/3.0（同族 `ranked_division_change` 3.3），
//    旧闸把**内容齐全的两扇窗**判成空图（假红）。现改为「非黑像素占比 + 窗根子树件数」两条独立判据，
//    门槛与实测出处写在 `NonBlackMinPct` 那一段；纯黑图 / 空容器仍判空（`GateSelfTest` 里是负例）。
//
// 🔴 **出图宿主自带模糊底**（2026-11-01 · `B-240②`）：`MainMenuScene` 这台宿主**没有壳**
//    ⇒ 原版那件 `UIBakeScreen`（`MainMenuWindow.ToBackground/Hide/Close` 烘的「上一帧 UI 模糊快照」）缺位
//    ⇒ **31 张窗图**的底是纯黑、系统性偏暗。现在由 `EnsureBake()` 在场景根建一件（⛔ 不进菜单树），
//    `Shell/GameWindow.cs` **一行不用改**（它的查找链首选 `UIBakeScreen.Current`）。
//    实际 RT 尺寸 / 是否退化会印进 `_index.tsv` 的 `说明` 列（批处理下 `Screen` 尺寸若为 0，底会退化成一整块平均色）。
//
// 产物：
//   `d:/4/_tmp_view/shots/win/<slug>.png`       每扇窗一张（**我们的**渲染，固定 1920×1080）
//   `d:/4/_tmp_view/shots/win/_index.tsv`       窗名 | 宿主 | 输出文件 | 分辨率 | rc | 说明
//
// 🔴 **尺寸口径**：`W × H` 是**我们的**渲染尺寸（1920×1080，与 `MenuCheck.Shoot` / 各宿主自检逐字相同）。
//    **原版参照图（用户实拍 / `资料/原版参照图/…`）的尺寸是另一回事** —— 并排比时由
//    `工具/fp_sbs_check.py` 做归一，⛔ 别拿本表的 1920×1080 当「原版的尺寸」。
//
// 🔴 **为什么要有这个文件**（现读证据）：`grep -rn "RenderWindowTo" Assets --include=*.cs` = **0**、
//    `grep -rn "shots/win"` = **0** ⇒ 在此之前**没有任何东西**产出「窗口」那一侧的截图，
//    `判据/功能点_并排比记账.tsv` 的 `件=窗口` 那一列因此**永远是空的**（那张表现在只有表头）。
//
// 🔴 **窗名清单从哪来**（⚠️ 单子上写的是「从 `判据/功能点_oracle规格.tsv` 的『窗口』列读」，**先看列结构**）：
//    现读那张表是 **12 列**（`FP_ID | 功能点 | 域 | oracle类型 | 输入/命令 | 期望 | 读数来源 | 判定方式 |
//    覆盖档 | 证据 | 日期 | 备注`）—— **没有「窗口」这一列**。
//    ⇒ 本文件的做法：① 仍然去读它，**找到了 `窗口` / `窗口名` 列就用它**（逐条对照下面的内建表）；
//      ② 找不到（= 今天这一版）⇒ **用内建表**并在日志里明写「规格表没有这一列」。
//    ⛔ 不静默：规格表点名了、而我们的内建表里没有对应开法的窗，**逐条进索引表并 rc=3 + 写明原因**。
//
// 🔴 **只读**：本文件**只建场景 + 渲图**，⛔ `SaveScene` / ⛔ 不动任何窗的布局或逻辑 / ⛔ 不动账与正本。
//    每个宿主的建场都走它**自己**那条现成的私有 `Build(...)`（经各文件类尾那一行公开查询口），
//    所以画出来的就是自检时那一棵一模一样的树。
//
// 📌 宿主为什么只有两个（`MainMenuScene` + `DeckScene`），其余六个见 `_index.tsv` 的 rc=3 行与
//    `资料/完全复刻/报告/B113X6_窗口渲图宿主.md` §「跑不了的窗」。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CardPresentation;
using UnityEditor;
using UnityEngine;

public static class WindowShots
{
    const string P = "[WinShot] ";

    /// <summary>产物目录（与 `MenuCheck.Shoot` 的 7 个宿主自检目录**分开**：这里是「窗口一扇一张」的正式交付物）。</summary>
    const string OutDir = "d:/4/_tmp_view/shots/win";

    /// <summary>🆕 `判据/功能点_oracle规格.tsv` 的绝对路径（只读；用来找「窗口」列）。</summary>
    const string SpecPath = "d:/4/Unity/资料/完全复刻/判据/功能点_oracle规格.tsv";
    const string IndexName = "_index.tsv";

    /// <summary>我们的渲染尺寸。🔴 固定值 = 与 `MenuCheck.Shoot` / 各宿主自检**逐字相同**（1920×1080，
    /// `LayoutSpace.DesignHeight = 1080`）。⛔ 别按窗口大小去算 —— 这里是「整屏一帧」。</summary>
    const int W = 1920, H = 1080;

    // ============================================================ 空图护栏（🔴 B-240 起 = **结构判据**，不再是「平均亮度」）
    //
    // 🔴 **为什么必须有一段护栏**：一张**全黑**的图静默写出去 = 「拍了但什么都没拍到」，与「没拍」一样坏，
    //    而 `fp_sbs_check` 只会把它当「有图」去比 ⇒ 必须在这里就标成 rc=1。
    //
    // 🔴 **为什么换掉「平均亮度 ≤ 3」**（`B-237` · 判据全文 = `资料/完全复刻/判据/B114_R1_两扇窗近黑.md`）：
    //    排位窗族 prefab 自带**整屏 `(0,0,0,0.773)` 黑遮罩**，渲染本身完全正确
    //    （实测像素比 `(60,200)` 177→40 = 0.226 · `(960,30)` (28,0,20)→(6,0,5) = 0.229，而 `1 − 0.773 = 0.227`）
    //    ⇒ 平均亮度天然只有 **2.9 / 3.0**；同族 `ranked_division_change` 是 **3.3**，只差 0.3 也被同一刀砍中
    //    ⇒ 旧闸的门槛**正好压在真实分布上沿**，判「空图」是**假红**。
    //    ⚠️ 这不是「把闸放松」：真·空图（相机没渲 / 窗被 `SetActive(false)` / 根是空容器）下面两条判据都过不去。

    /// <summary>判据①：**非黑像素占比**下限（%）。低于它 ⇒ 判空图。
    /// <para>**【门槛怎么来的】** = **全部 30 张**窗图实测（2026-11-01 现读 `d:/4/_tmp_view/shots/win/*.png`，
    /// 1920×1080 逐像素，`max(r,g,b) &gt; NonBlackChannel` 记作非黑）：最暗三张 ——
    /// `ranked_season_ended` **8.91%** · `ranked_new_season` **8.97%** · `ranked_division_change` **11.11%**；
    /// 最亮 `main_menu` **96.87%**。⇒ 门槛取 **1.0%**（= 最低真实值的 **1/8.9**，不贴边）；
    /// 而**纯黑图恒 0.00%** ⇒ 旧闸能抓的「全黑 = 什么都没画」这一档，新闸照样抓（灭自证）。</para>
    /// <para>⚠️ 实测只覆盖**今天这 30 张**（= 现状的口径）。将来若某一扇真的整屏只有一个角标，
    /// 先量它的非黑占比再判「是闸太松还是窗真的空」—— ⛔ 别直接调小这个常量。</para></summary>
    const float NonBlackMinPct = 1.0f;

    /// <summary>「非黑」的通道阈值（0–255）。取 8：暗遮罩下的顶栏实测是 `(6,0,5)` ⇒ 仍算黑；
    /// 真正画出来的白字 / 图标 / 底板都远高于它。</summary>
    const int NonBlackChannel = 8;

    /// <summary>「亮像素」灰度门槛（0–255）—— **只用于读数与索引表，不参与判定**。
    /// 实测下限 = `ranked_reward_event` **0.144%**，排位两扇 1.34% / 1.42%，`main_menu` 55.89%
    /// ⇒ 留作下一个人核「这扇窗到底有没有文字」的旁证。⛔ 别拿它当闸（它离最暗真实值只差 3 倍，太贴边）。</summary>
    const int BrightLum = 40;

    // ============================================================ 宿主

    enum HostKind { MainMenu, Deck }

    static string HostName(HostKind h)
    {
        switch (h)
        {
            case HostKind.MainMenu: return "MainMenuScene";
            case HostKind.Deck: return "DeckScene";
            default: return "?";
        }
    }

    /// <summary>运行期上下文：当前已经建好的宿主根 / 主菜单实例。</summary>
    sealed class Ctx
    {
        /// <summary>🆕 **B-240②（方案 A）**：这一台宿主的**模糊底**读数（RT 尺寸 / 是否退化），
        /// 逐行拼进 `_index.tsv` 的 `说明` 列 —— 让「批处理下底是不是退化成一块纯色」一眼可判。
        /// `null` = 这一档没有模糊底（`DeckScene`）。</summary>
        public string BakeNote;
        public MainMenuRuntime Menu;
        public Transform DeckRoot;
    }

    sealed class Spec
    {
        public string Name;      // 窗名（索引表第 1 列；也是日志里那句话）
        public string Slug;      // 输出文件名（**ASCII** —— 避开非 ASCII 路径在 diff 管线里的编码风险）
        public HostKind Host;
        public Func<Ctx, Transform> Open;   // 返回**窗根**；null ⇒ 这一扇开不出来（不静默，见 RunOne）
        public string How;       // 用哪条生产 API 开的（写进日志/报告，方便下一个人核）
        public Spec(string n, string s, HostKind h, Func<Ctx, Transform> open, string how)
        { Name = n; Slug = s; Host = h; Open = open; How = how; }
    }

    /// <summary>`GameWindow` → 它的窗根（`null` 也照原样返回，让 RunOne 去判「没开出来」）。</summary>
    static Transform T(GameWindow w) { return w != null ? w.transform : null; }

    /// <summary>物品展示窗那一行的**样张内容** = `Resources/Cardbacks.json` 里 Ultramarines 的默认卡背
    /// （2026-11-01 现读 = `Cardback_UM_Campaign_Free`；取不到则退 `CardArt.CosmeticNames()` 的首个）。
    /// <para>🔴 **为什么必须给它内容**（`B-303` / `B115_R1`）：原版 `ItemDisplayWindow.Open` 在
    /// `context.item == null` 时 `LogError("No item available") + return`（`decomp_full/ItemDisplayWindow__Open.c:23-34`），
    /// 我们的 `Build()` 走的是同一条闸（`Shell/ItemDisplayWindow.cs:296-300`）⇒ **无参调用 = 空容器**
    /// （实测：窗根子树 渲染件 0 / 文本 0）。⛔ 别改成「`OpenItemDisplay` 无参时自己给一张卡背」——
    /// 原版 `null` 就是错误支，那样做等于自造一个原版没有的行为。</para>
    /// <para>取不到**不静默**：返回 `null` ⇒ `ForCardback(null)` 仍落回空上下文 ⇒ 这一行照旧 rc=1（那才是真红）。</para></summary>
    static string SampleCardback()
    {
        var n = CardbackTable.DefaultFor("Ultramarines");
        if (!string.IsNullOrEmpty(n)) return n;
        var names = CardArt.CosmeticNames();
        if (names != null && names.Length > 0) return names[0];
        Debug.LogError(P + "物品展示窗的样张卡背取不到（`Resources/Cardbacks.json` / `Resources/Art/cardbacks/` 都空）"
                          + " ⇒ 这一行会是空容器（rc=1，别当拍到了）");
        return null;
    }

    /// <summary>
    /// **内建窗表**。每一行都指名那条**生产开窗 API**（不是我们造的开法）。
    /// ⚠️ 顺序 = 实际执行顺序（同宿主的窗连在一起跑：一次建场、多扇窗）。
    /// </summary>
    static readonly Spec[] Specs =
    {
        // ---------------- 主菜单宿主（`MainMenuScene.BuildForWindowShots`）
        // 基线：不开启任何窗（`MainMenuRuntime.Build` 出厂态）—— 底下那些弹窗的「底图」就是它。
        new Spec("主菜单（无弹窗）", "main_menu", HostKind.MainMenu,
                 c => c.Menu != null ? c.Menu.transform : null,
                 "`MainMenuRuntime.Build()` 出厂态（不开任何窗）"),

        // ---- 左竖导航五扇（`MainMenuRuntime` 的公开开窗入口 = 原版 `OpenWindowButton` 那一跳）
        new Spec("收藏窗", "collection", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenCollection()) : null,
                 "`MainMenuRuntime.OpenCollection()`（原版 COLLECTION 导航钮，`closeAll: true`）"),
        new Spec("商店窗", "shop", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenShop()) : null,
                 "`MainMenuRuntime.OpenShop()`（原版 SHOP 导航钮，`closeAll: true`）"),
        new Spec("奖励窗", "rewards", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenRewards()) : null,
                 "`MainMenuRuntime.OpenRewards()`（原版 REWARDS 导航钮，`closeAll: true`）"),
        new Spec("社交窗", "social", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenSocial()) : null,
                 "`MainMenuRuntime.OpenSocial()`（原版第 5 颗导航钮 `40k_main_bt_friends`）"),
        new Spec("聊天窗", "chat", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenChat()) : null,
                 "`MainMenuRuntime.OpenChat()`（原版 `ChatPreview.OpenChat`）"),

        // ---- 顶栏 / 头像 / 信封 那一族（静态入口）
        new Spec("收件箱", "inbox", HostKind.MainMenu,
                 c => T(MainMenuRuntime.OpenInbox()),
                 "`MainMenuRuntime.OpenInbox()`（原版顶栏信封钮）"),
        new Spec("设置窗", "settings", HostKind.MainMenu,
                 c => T(MainMenuRuntime.OpenSettings()),
                 "`MainMenuRuntime.OpenSettings()`（原版顶栏齿轮钮）"),
        new Spec("玩家档案窗", "profile", HostKind.MainMenu,
                 c => T(MainMenuRuntime.OpenProfile()),
                 "`MainMenuRuntime.OpenProfile()`（原版顶栏头像钮）"),
        new Spec("对局历史弹窗", "battle_log", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenBattleLogPopup()) : null,
                 "`MainMenuRuntime.OpenBattleLogPopup()`（⚠️ 原版入口查不到 ⇒ 本仓只提供开法）"),
        new Spec("排行榜·遭遇", "leaderboard_skirmish", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenLeaderboard(LeaderboardKind.Skirmish)) : null,
                 "`MainMenuRuntime.OpenLeaderboard(LeaderboardKind.Skirmish)`"),
        new Spec("排行榜·经典", "leaderboard_classic", HostKind.MainMenu,
                 c => c.Menu != null ? T(c.Menu.OpenLeaderboard(LeaderboardKind.Classic)) : null,
                 "`MainMenuRuntime.OpenLeaderboard(LeaderboardKind.Classic)`"),

        // ---- 弹窗族（`WindowsManager.OpenXxx` 的静态入口 = 本仓「怎么开」的唯一出处）
        new Spec("欢迎弹窗", "welcome", HostKind.MainMenu,
                 c => T(WindowsManager.OpenWelcome()), "`WindowsManager.OpenWelcome()`"),
        new Spec("教程消息窗", "tutorial", HostKind.MainMenu,
                 c => T(WindowsManager.OpenTutorialMessage()), "`WindowsManager.OpenTutorialMessage()`"),
        new Spec("通用选项面板", "generic_options", HostKind.MainMenu,
                 c => T(WindowsManager.OpenGenericOptionsPanel()), "`WindowsManager.OpenGenericOptionsPanel()`"),
        // 🔴 **2026-11-01（`B115_R1` · `B-303`）**：本窗**没有「出厂内容」** —— 原版 `ItemDisplayWindow.Open` 在
        //   `context.item == null` 时 `LogError("No item available") + return`（`decomp_full/ItemDisplayWindow__Open.c:23-34`），
        //   我们的 `Build()` 同一条闸（`Shell/ItemDisplayWindow.cs:296-300`）⇒ 原来那一版**无参调用 = 空容器**
        //   （实测 渲染件 0 / 文本 0）。⇒ 这一行必须给**生产 Context**（= 收藏窗卡背页那一跳），⛔ 不是「给它一个默认值」。
        new Spec("物品展示窗", "item_display", HostKind.MainMenu,
                 c => T(WindowsManager.OpenItemDisplay(ItemDisplayWindow.ForCardback(SampleCardback()))),
                 "`WindowsManager.OpenItemDisplay(ItemDisplayWindow.ForCardback(<默认卡背>))`（= 收藏窗卡背页的生产那一跳）"),
        new Spec("卡牌来源窗", "where_to_get_cards", HostKind.MainMenu,
                 c => T(WindowsManager.OpenWhereToGetCards()), "`WindowsManager.OpenWhereToGetCards()`"),
        new Spec("活动承载窗", "parent_event", HostKind.MainMenu,
                 c => T(WindowsManager.OpenParentEvent()), "`WindowsManager.OpenParentEvent()`（左竖导航第 6 颗那一跳）"),
        new Spec("抽卡活动窗", "gacha_event", HostKind.MainMenu,
                 c => T(WindowsManager.OpenGachaEvent()), "`WindowsManager.OpenGachaEvent()`"),
        new Spec("团队活动窗", "raid_event", HostKind.MainMenu,
                 c => T(WindowsManager.OpenRaidEvent()), "`WindowsManager.OpenRaidEvent()`"),
        new Spec("战令窗", "expansion_pass", HostKind.MainMenu,
                 c => T(WindowsManager.OpenExpansionPassEvent()), "`WindowsManager.OpenExpansionPassEvent()`"),
        new Spec("段位变化窗", "ranked_division_change", HostKind.MainMenu,
                 c => T(WindowsManager.OpenRankedDivisionChange()), "`WindowsManager.OpenRankedDivisionChange()`"),
        new Spec("新赛季窗", "ranked_new_season", HostKind.MainMenu,
                 c => T(WindowsManager.OpenRankedNewSeason()), "`WindowsManager.OpenRankedNewSeason()`"),
        new Spec("赛季结束窗", "ranked_season_ended", HostKind.MainMenu,
                 c => T(WindowsManager.OpenRankedSeasonEnded()), "`WindowsManager.OpenRankedSeasonEnded()`"),
        new Spec("排位奖励窗", "ranked_reward_event", HostKind.MainMenu,
                 c => T(WindowsManager.OpenRankedRewardEvent()), "`WindowsManager.OpenRankedRewardEvent()`"),
        new Spec("反馈问卷窗", "give_feedback", HostKind.MainMenu,
                 c => T(WindowsManager.OpenGiveFeedback()), "`WindowsManager.OpenGiveFeedback()`"),
        new Spec("联盟成员选项", "alliance_member_options", HostKind.MainMenu,
                 c => T(WindowsManager.OpenAllianceMemberOptions()), "`WindowsManager.OpenAllianceMemberOptions()`"),
        new Spec("高级购买窗", "purchase_premium", HostKind.MainMenu,
                 c => T(WindowsManager.OpenPurchasePremiumWindow()), "`WindowsManager.OpenPurchasePremiumWindow()`"),
        new Spec("推荐人窗", "referral", HostKind.MainMenu,
                 c => T(WindowsManager.OpenReferralPopup()), "`WindowsManager.OpenReferralPopup()`"),
        new Spec("能源活动窗", "energy_event", HostKind.MainMenu,
                 c => T(WindowsManager.OpenEnergyEvent()), "`WindowsManager.OpenEnergyEvent()`"),
        new Spec("商店报价弹窗", "base_offer", HostKind.MainMenu,
                 c => T(WindowsManager.OpenBaseOfferPopup()), "`WindowsManager.OpenBaseOfferPopup()`"),
        new Spec("开局选卡组", "choose_army_deck", HostKind.MainMenu,
                 c => T(WindowsManager.OpenChooseArmyDeck()), "`WindowsManager.OpenChooseArmyDeck()`"),

        // ---------------- 卡组编辑宿主（`DeckScene.BuildForWindowShots`）
        // 卡片编辑不是一个 `GameWindow`（它是 `DeckRuntime` 建的一整棵树）⇒ 根 = `DeckEditor`。
        new Spec("卡组编辑窗", "deck_editor", HostKind.Deck,
                 c => c.DeckRoot, "`DeckScene.BuildForWindowShots()`（= 自检那条 `Build(DeckLibrary.Load(), out root)`）"),
    };

    /// <summary>
    /// **跑不了的窗**（逐条写明原因 —— 这是「⛔ 不静默跳过」那一半；它们照样进 `_index.tsv`，rc=3）。
    /// 🔴 每条都必须能回答「为什么今天产不出这一张」，⛔ 不许写「暂不支持」这种等于没写的。
    /// </summary>
    static readonly string[][] NotRun =
    {
        new[] { "战斗界面 / HUD", "BattleScene",
                "战斗 HUD 不是「可独立开窗的对象」—— 它由 `BattleDriver` 在开局的整条驱动链里建并逐回合改；"
              + "空场景里没有 `BattleContext`（原版卡数据在远端 CCD，`inj FAIL: empty Data`）⇒ 开不出「一扇窗」。"
              + "现成的实拍/渲染口 = `-executeMethod BattleScene.Run`（它自己会把战斗画面渲成 PNG）。见 rc=3" },
        new[] { "商店窗（单宿主版）", "ShopScene",
                "同一扇窗**已被 `MainMenuScene` 宿主以生产开窗路覆盖**（`MainMenuRuntime.OpenShop`）；"
              + "`ShopScene` 那份是**没有左栏/顶栏**的自检变体，两版并排会变成「同一扇窗两张不同底图」⇒ 不进本表" },
        new[] { "收藏窗（单宿主版）", "CollectionScene",
                "同上：已被 `MainMenuRuntime.OpenCollection` 覆盖；`CollectionScene` 是无左栏的自检变体" },
        new[] { "奖励窗（单宿主版）", "RewardsScene",
                "同上：已被 `MainMenuRuntime.OpenRewards` 覆盖；`RewardsScene` 是无左栏的自检变体" },
        new[] { "设置窗（单宿主版）", "SettingsScene",
                "同上：已被 `MainMenuRuntime.OpenSettings` 覆盖；`SettingsScene` 是无左栏的自检变体" },
        new[] { "主菜单壳（含开场动画后的完整壳）", "ShellScene",
                "`ShellRuntime` 的完整壳需要走 `ShellScene` 那套「建壳 → 开场动画 → `LoadMainMenu`」的时序；"
              + "本表只拍**窗口**，壳层基线由 `main_menu` 那一行承担。要看完整壳 ⇒ `-executeMethod ShellScene.Run`" },
    };

    // ============================================================ 入口

    [MenuItem("Warpforge/窗口渲图（WindowShots · B-55 第 4 件）")]
    public static void RunFromCLI()
    {
        int fail = 0, ok = 0, skip = 0;
        Directory.CreateDirectory(OutDir);

        string onlyHost = ArgValue("-wfWinHost");
        string onlySlug = ArgValue("-wfWinOnly");

        // 🔴 规格表那一列先读一遍（**先看列结构**，见文件头）。今天这一版**没有「窗口」列** ⇒ 返回空集。
        var specNames = LoadSpecWindowColumn(out string specNote);
        Debug.Log(P + specNote);

        var rows = new List<string>();
        var ctx = new Ctx();
        HostKind cur = (HostKind)(-1);
        bool hostBroken = false;

        try
        {
            foreach (var s in Specs)
            {
                if (!string.IsNullOrEmpty(onlyHost) && !string.Equals(onlyHost, s.Slug, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(onlyHost, HostName(s.Host), StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(onlyHost, s.Host == HostKind.MainMenu ? "mainmenu" : "deck", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(onlySlug) && !string.Equals(onlySlug, s.Slug, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 换宿主 ⇒ 建场（`Build` 会 `NewScene` 把上一棵整棵丢掉 ⇒ 同一时刻只有一台宿主活着）
                if (s.Host != cur)
                {
                    cur = s.Host;
                    hostBroken = !EnsureHost(s.Host, ctx);
                }

                string file = Path.Combine(OutDir, s.Slug + ".png");
                int rc; string note;
                if (hostBroken)
                {
                    rc = 3; note = "宿主没建起来（见上面的错误日志）；这一扇**没拍**";
                }
                else rc = RunOne(s, ctx, file, out note);

                if (rc == 0) ok++; else if (rc == 3) skip++; else fail++;
                rows.Add(Row(s.Name, HostName(s.Host), rc == 0 ? file : "", rc, note));
                Debug.Log(P + $"   {(rc == 0 ? "OK " : rc == 1 ? "空图" : rc == 2 ? "失败" : "跳过")} "
                            + $"`{s.Name}` ({s.Slug}) rc={rc} —— {note}");
            }

            // 规格表点名、但内建表没有的窗 ⇒ **逐条进表 + rc=3**（⛔ 静默跳过）
            foreach (var n in specNames)
            {
                bool known = false;
                foreach (var s in Specs) if (s.Name == n || s.Slug == n) { known = true; break; }
                if (known) continue;
                skip++;
                rows.Add(Row(n, "?", "", 3, "`功能点_oracle规格.tsv` 的「窗口」列点名了它，但 `WindowShots.Specs` 里没有对应开法 ⇒ 没拍"));
                Debug.LogWarning(P + $"   ⚠️ 规格表点名 `{n}`，但内建表里没有它 ⇒ rc=3（没拍）");
            }

            foreach (var nt in NotRun)
            {
                skip++;
                rows.Add(Row(nt[0], nt[1], "", 3, nt[2]));
            }
        }
        catch (Exception e)
        {
            // 整趟崩掉也要留下索引（否则外部只看到「没结果」）
            fail++;
            Debug.LogError(P + "整趟抛异常（索引仍会写出，但**后面的窗没跑**）：" + e);
            rows.Add(Row("（整趟）", "?", "", 2, "整趟抛异常：" + e.GetType().Name + " · " + e.Message));
        }

        WriteIndex(rows);
        Debug.Log(P + $"=== 合计：{ok} 拍成 / {fail} 失败或空图 / {skip} 跳过 ===  → {Path.Combine(OutDir, IndexName)}");

        if (Application.isBatchMode) EditorApplication.Exit(fail > 0 ? 1 : 0);
    }

    /// <summary>建好一台宿主。返回 `false` = 建场失败（异常/根为空）—— 调用方会把该宿主的窗**逐条**记成 rc=3。</summary>
    static bool EnsureHost(HostKind h, Ctx ctx)
    {
        Debug.Log(P + $"--- 建宿主 `{HostName(h)}`（只建不存；⛔ 不调 `SaveScene`）---");
        // 建场期间不出声（照 7 个宿主的 `Run`：`WFSoundPlayer.Enabled = false` 是**全局静态开关** ⇒ 必须还原）
        bool prevSound = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;
        try
        {
            switch (h)
            {
                case HostKind.MainMenu:
                    ctx.DeckRoot = null;
                    var root = MainMenuScene.BuildForWindowShots();
                    ctx.Menu = MainMenuRuntime.InstanceOrFind();
                    if (ctx.Menu == null || root == null)
                    {
                        Debug.LogError(P + "   `MainMenuScene.BuildForWindowShots()` 回来了，但根或 `MainMenuRuntime` 是 null");
                        return false;
                    }
                    ctx.BakeNote = EnsureBake();     // 🆕 B-240②（方案 A）：补上原版那件模糊快照底
                    return true;
                case HostKind.Deck:
                    ctx.Menu = null;
                    ctx.BakeNote = null;             // 卡组编辑那一档没有 `GameWindow` ⇒ 不走 `SetBake`，不该有底
                    ctx.DeckRoot = DeckScene.BuildForWindowShots();
                    if (ctx.DeckRoot == null) { Debug.LogError(P + "   卡组编辑宿主建出来是 null"); return false; }
                    return true;
                default:
                    Debug.LogError(P + "   未知宿主 " + h);
                    return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(P + $"   建宿主 `{HostName(h)}` 抛异常：" + e);
            return false;
        }
        finally { WarpforgeVFX.WFSoundPlayer.Enabled = prevSound; }
    }

    /// <summary>一扇窗：开 → 等版面 → 渲 → 关。rc：0 拍成 · 1 空图 · 2 失败 · 3 跳过。</summary>
    static int RunOne(Spec s, Ctx ctx, string file, out string note)
    {
        Transform root = null;
        try { root = s.Open(ctx); }
        catch (Exception e)
        {
            note = "开窗抛异常（" + s.How + "）：" + e.GetType().Name + " · " + e.Message;
            return 2;
        }
        if (root == null) { note = "开窗返回 null（" + s.How + "）⇒ 这一扇**没开出来**"; return 2; }

        try
        {
            Settle(root);
            int rc = Render(file, root, out string detail);
            note = s.How + " · " + detail
                 + (string.IsNullOrEmpty(ctx.BakeNote) ? "" : " · " + ctx.BakeNote);   // B-240②：模糊底读数逐行进索引
            return rc;
        }
        catch (Exception e)
        {
            note = "渲染抛异常：" + e.GetType().Name + " · " + e.Message;
            return 2;
        }
        finally
        {
            // 关掉这一扇 —— ⛔ 不关会在下一张图里叠着（`main_menu` 那一行不是窗，`GetComponent<GameWindow>` 为 null ⇒ 自动跳过）
            var gw = root.GetComponent<GameWindow>();
            if (gw != null)
            {
                try { gw.Close(); }
                catch (Exception e) { Debug.LogWarning(P + $"   `{s.Name}` 关窗抛异常（不影响已拍的那张）：" + e.Message); }
            }
        }
    }

    /// <summary>
    /// 「等版面」—— **批处理下没有帧循环**，所以只能显式推：
    /// ① `Canvas.ForceUpdateCanvases()` 跑 UGUI 的几何重建；② 每颗 `Label` 一次 `ForceRelayout()`
    /// （它是 `Label` 的公开口：`SetFontSize(现值)` → `ForceMeshUpdate()` → 再推一次版面）。
    /// ⚠️ 这与各宿主自检里用的那套**同一口径**（`SetAutoFitBox`/TMP 的教训：不推就是上一帧的模型）。
    /// </summary>
    static void Settle(Transform root)
    {
        Canvas.ForceUpdateCanvases();
        if (root != null)
        {
            var labels = root.GetComponentsInChildren<Label>(true);
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] != null) labels[i].ForceRelayout();
        }
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// 整屏渲一帧写到 `file`（形状照 `MenuCheck.Shoot`：同一套 `targetTexture` → `Render` → `ReadPixels` → `EncodeToPNG`）。
    /// 🔴 **`RenderTexture` 必须在 `finally` 里回收** —— 异常路径也要（否则批处理里漏一堆 RT）。
    /// 🔴 ⛔ **不用 `ScreenCapture.CaptureScreenshot`**（批处理下会崩，本仓已记）。
    /// </summary>
    /// <param name="root">这一扇的**窗根** —— 结构判据② 要数它子树里的可渲染件（见 <see cref="Measure"/>）。</param>
    static int Render(string file, Transform root, out string detail)
    {
        var cam = Camera.main;
        if (cam == null) { detail = "**没拍成** —— `Camera.main == null`（宿主没建出带 `MainCamera` 标签的相机）"; return 2; }

        RenderTexture rt = null;
        Texture2D tex = null;
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        try
        {
            rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();

            File.WriteAllBytes(file, tex.EncodeToPNG());

            // 🔴 B-240：判定改用**结构判据**（非黑像素占比 + 窗子树里的可渲染件），平均亮度只留读数。
            var st = Measure(tex, root);
            string read = $"{W}×{H} · 平均亮度 {st.MeanLum:F1} · 非黑 {st.NonBlackPct:F2}% · 亮像素 {st.BrightPct:F2}%"
                        + $" · 子件 渲染件 {st.Renderers} / 文本 {st.Labels}";
            if (IsBlank(st.MeanLum, st.NonBlackPct, st.BrightPct, st.Renderers, st.Labels, out string why))
            {
                detail = read + " · ⚠️ **疑似空图**（" + why + "）—— 文件已写出，但别拿它当「拍到了」";
                return 1;
            }
            detail = read;
            return 0;
        }
        catch (Exception e)
        {
            detail = "渲染/写盘抛异常：" + e.GetType().Name + " · " + e.Message;
            return 2;
        }
        finally
        {
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
        }
    }

    /// <summary>平均亮度（0–255），抽样步长 7 —— 与 7 个宿主自检的 `MeanBrightness` **同一口径**
    /// （`long` 累加、分母 = 采样数 ⇒ `(n + 6) / 7`）。</summary>
    static float MeanLum(Texture2D t)
    {
        if (t == null) return 0f;
        var px = t.GetPixels32();
        if (px.Length == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b;
        return sum / 3f / ((px.Length + 6) / 7);
    }

    // ============================================================ 索引表

    /// <summary>一行 = 一扇窗。⚠️ 前 5 列是单子上钉死的（`窗名 | 宿主 | 输出文件 | 分辨率 | rc`）；
    /// 第 6 列 `说明` 是**追加**的（「跑不了/空图」的原因必须落盘，⛔ 不许只印在日志里就完事）。</summary>
    static string Row(string name, string host, string file, int rc, string note)
    {
        string res = file.Length > 0 ? $"{W}×{H}" : "";
        return string.Join("\t", new[] { name, host, file, res, rc.ToString(), Clean(note) });
    }

    static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
    }

    static void WriteIndex(List<string> rows)
    {
        var sb = new StringBuilder();
        sb.Append("窗名\t宿主\t输出文件\t分辨率\trc\t说明\n");
        foreach (var r in rows) sb.Append(r).Append('\n');
        // ⚠️ 用 LF 写（消费方是本仓的 python，按行读；⛔ 别让 CRLF 在最后一列留一个 `\r`）
        File.WriteAllText(Path.Combine(OutDir, IndexName), sb.ToString(), new UTF8Encoding(false));
        Debug.Log(P + $"索引 `{Path.Combine(OutDir, IndexName)}`：{rows.Count} 行");
    }

    // ============================================================ 规格表那一列

    /// <summary>
    /// 从 `判据/功能点_oracle规格.tsv` 里找一列叫 `窗口` / `窗口名` 的，返回它下面的**去重窗名**。
    /// 🔴 现读那一版是 **12 列、没有这一列** ⇒ 返回空集 + 一条明写原因的 note（⛔ 不假装读过）。
    /// 表头解析用**裸 `split('\t')`**（与 `fp_sbs_check.py` / `merge_ledger_parts.py` 同一约定）。
    /// </summary>
    static HashSet<string> LoadSpecWindowColumn(out string note)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (!File.Exists(SpecPath)) { note = $"规格表不在（{SpecPath}）⇒ 用内建窗表"; return set; }

        string[] lines;
        try { lines = File.ReadAllLines(SpecPath); }
        catch (Exception e) { note = "读规格表失败（" + e.Message + "）⇒ 用内建窗表"; return set; }

        if (lines.Length == 0) { note = "规格表是空的 ⇒ 用内建窗表"; return set; }

        var head = lines[0].TrimStart('\uFEFF').Split('\t');
        int col = -1;
        for (int i = 0; i < head.Length; i++)
            if (head[i].Trim() == "窗口" || head[i].Trim() == "窗口名") { col = i; break; }

        if (col < 0)
        {
            note = $"规格表 `{SpecPath}` 现读 {head.Length} 列（{string.Join(" | ", head)}），**没有「窗口」列** "
                 + "⇒ 本趟用**内建窗表**（清单在 `WindowShots.Specs`，每行都标了它用哪条生产开窗 API）";
            return set;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var f = lines[i].Split('\t');
            if (col < f.Length && f[col].Trim().Length > 0) set.Add(f[col].Trim());
        }
        note = $"规格表第 {col + 1} 列 = 「窗口」，读到 {set.Count} 个不同的窗名 ⇒ 会与本文件的内建表逐条对照";
        return set;
    }

    /// <summary>批处理下的 `-wfWinOnly xxx` / `-wfWinHost xxx`（`Environment.GetCommandLineArgs()` 里找）。</summary>
    static string ArgValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }

    // ============================================================ 结构判据（🔴 B-240）

    /// <summary>一帧的**结构读数**（像素三项 + 窗根子树两件）。</summary>
    sealed class Stats
    {
        /// <summary>平均亮度（0–255，抽样步长 7，与各宿主 `MeanBrightness` 同一口径）。**只作读数、不参与判定**。</summary>
        public float MeanLum;
        /// <summary>非黑像素占比（%）：`max(r,g,b) &gt; NonBlackChannel` 记作非黑，**全像素**统计。</summary>
        public float NonBlackPct;
        /// <summary>亮像素占比（%）：灰度 `&gt;= BrightLum`，**只作读数**。</summary>
        public float BrightPct;
        /// <summary>窗根子树里的 `MeshRenderer` 数（含 inactive）。`ImageQuad` 与 `Label` 两种件都走它。</summary>
        public int Renderers;
        /// <summary>窗根子树里的 `Label` 数（含 inactive）。</summary>
        public int Labels;
    }

    /// <summary>量一帧的结构读数。⚠️ 像素遍历是**全像素**（1920×1080 = 2.07M，批处理里几毫秒）；
    /// 亮度那一项仍按步长 7 抽样（`i % 7 == 0` ⇒ 采样数 = `(n + 6) / 7`）⇒
    /// **与旧读数逐位可比**（索引表里「平均亮度」那一列不会因这次改动跳数）。
    /// <para>结构那两件取 `GetComponentsInChildren&lt;&gt;(true)`（**含 inactive**），
    /// 因为「窗开着但被 `SetActive(false)`」正是要抓的那一档 —— 那时件还在、却一个都没画。</para></summary>
    static Stats Measure(Texture2D t, Transform root)
    {
        var st = new Stats();
        if (root != null)
        {
            var mrs = root.GetComponentsInChildren<MeshRenderer>(true);
            st.Renderers = mrs != null ? mrs.Length : 0;
            var ls = root.GetComponentsInChildren<Label>(true);
            st.Labels = ls != null ? ls.Length : 0;
        }
        if (t == null) return st;

        var px = t.GetPixels32();
        if (px.Length == 0) return st;

        long lumSum = 0; long lumN = 0; int nonBlack = 0; int bright = 0;
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            int m = c.r > c.g ? (c.r > c.b ? c.r : c.b) : (c.g > c.b ? c.g : c.b);
            if (m > NonBlackChannel) nonBlack++;
            if ((c.r + c.g + c.b) >= BrightLum * 3) bright++;
            if (i % 7 == 0) { lumSum += c.r + c.g + c.b; lumN++; }
        }
        st.MeanLum = lumN > 0 ? lumSum / 3f / lumN : 0f;
        st.NonBlackPct = 100f * nonBlack / px.Length;
        st.BrightPct = 100f * bright / px.Length;
        return st;
    }

    /// <summary>**空图判定**（🔴 B-240）。纯函数 ⇒ 可被 <see cref="GateSelfTest"/> 离线逐档验证。
    /// <para>两条**互相独立**的判据，任一成立就判空：</para>
    /// <list type="number">
    /// <item><b>① 像素侧</b>：非黑占比 &lt; <see cref="NonBlackMinPct"/> ⇒ 屏幕上几乎什么都没画
    ///   （纯黑图恒 0.00%；今天 30 张实测最低 8.91%）。</item>
    /// <item><b>② 结构侧</b>：窗根子树里 `MeshRenderer` 与 `Label` **同时**为 0 ⇒ 开出来的是个空容器。
    ///   这一档**光看像素会漏**（残留着上一扇窗或黑底时，非黑占比可能是高的）。</item>
    /// </list>
    /// <para>⚠️ ② 取「**同时**为 0」是刻意的保守：只要求其一非零会误伤「纯文字窗 / 纯图窗」（本项目两种都存在）。</para>
    /// <para>`meanLum` / `brightPct` **不参与判定** —— 进签名只为让失败消息和自检档位里带全读数。</para></summary>
    static bool IsBlank(float meanLum, float nonBlackPct, float brightPct, int renderers, int labels, out string why)
    {
        if (nonBlackPct < NonBlackMinPct)
        {
            why = $"非黑占比 {nonBlackPct:F2}% < {NonBlackMinPct:F1}%（平均亮度 {meanLum:F1} 只作读数）";
            return true;
        }
        if (renderers == 0 && labels == 0)
        {
            why = "窗根子树里**渲染件 0 / 文本 0** ⇒ 开出来的是空容器"
                + $"（像素侧非黑 {nonBlackPct:F2}% 是残留内容，不是这一扇画的）";
            return true;
        }
        why = "";
        return false;
    }

    /// <summary>
    /// **闸的自检**（灭自证）：`… -executeMethod WindowShots.GateSelfTest`
    /// <para>逐档喂**合成读数**，断言「该判空的一定判空、该放过的一定放过」。前两档是**负例**
    /// （纯黑图 / 空容器 = 旧闸能抓的那一类），后三档是**正例**（含两扇曾被旧闸误杀的排位窗）。
    /// ⛔ 别为了让某扇窗变绿去删负例 —— 删了这条自检就变成自证。</para>
    /// <para>⚠️ 这是**纯算术**：不建场景、不渲图 ⇒ 批处理里几毫秒。像素项取**实测值**
    /// （2026-11-01 现读 `_tmp_view/shots/win/*.png`）；件数是**合成值**（这一趟不数真树）。</para>
    /// </summary>
    public static void GateSelfTest()
    {
        int fail = 0;
        void Case(string name, bool wantBlank, float lum, float nonBlack, float bright, int rnd, int lbl)
        {
            bool got = IsBlank(lum, nonBlack, bright, rnd, lbl, out string why);
            bool ok = got == wantBlank;
            if (!ok) fail++;
            Debug.Log(P + $"   {(ok ? "✓" : "✗")} `{name}`：期望 {(wantBlank ? "判空" : "放行")}，"
                        + $"实得 {(got ? "判空" : "放行")}{(got ? "（" + why + "）" : "")}"
                        + $"｜读数 亮度 {lum:F1} / 非黑 {nonBlack:F2}% / 亮像素 {bright:F2}% / 渲染件 {rnd} / 文本 {lbl}");
        }

        // ---- 负例（必须判空）----
        Case("纯黑图（相机没渲 / 整屏被遮死）", true, 0f, 0.00f, 0.00f, 12, 3);
        Case("空容器（像素非黑很高，但窗根子树里一个件都没有）", true, 73.2f, 96.87f, 55.89f, 0, 0);
        // ---- 正例（必须放行）：两扇曾被旧闸误杀的排位窗 + 基线 ----
        Case("新赛季窗（像素 = 实测 · 件数 = 合成）", false, 2.8f, 8.97f, 1.34f, 47, 3);
        Case("赛季结束窗（像素 = 实测 · 件数 = 合成）", false, 3.0f, 8.91f, 1.42f, 52, 6);
        Case("主菜单基线（像素 = 实测 · 件数 = 合成）", false, 73.2f, 96.87f, 55.89f, 512, 37);

        Debug.Log(P + (fail == 0
            ? "=== SELFTEST PASS 5/5（闸没被放松：纯黑图与空容器仍判空）==="
            : $"=== SELFTEST FAIL {fail}/5 ==="));
        if (fail > 0) Debug.LogError(P + "   闸的自检红了 ⇒ 这一趟的出图结果不能当验收");
        if (Application.isBatchMode) EditorApplication.Exit(fail > 0 ? 1 : 0);
    }

    // ============================================================ 模糊底（🔴 B-240② · 方案 A）

    /// <summary>退化下限：RT 任一边小于它 ⇒ 底下认不出是「模糊的主菜单」，只剩大块色。
    /// <para>判据 = **目标帧 1920×1080 的 1/8**（60×34）。正常档 RT = 帧尺寸 × `UIBakeScreen.RENDER_TEXTURE_MULTIPLIER`
    /// （= 0.25，原版 `CreateRT` 实读）⇒ **480×270**，正好是下限的 8 倍。低于下限只可能来自
    /// 「`Screen.width/height` 在批处理下是 0 ⇒ `CreateRT` 兜到 1×1」这一条（`UIBakeScreen.cs:170-176` 的既有兜底）。</para></summary>
    const int MinBakeRtW = 60, MinBakeRtH = 34;

    /// <summary>
    /// 给**出图宿主**补上原版那件 `UIBakeScreen`（`B-240②` · 报告 `B114_W6_窗图闸.md` §3·2 的**方案 A**）。
    /// <para>**为什么不必建整壳**：`GameWindow.SetBake` 的查找链 = `UIBakeScreen.Current`（静态，`Build()` 里赋）
    /// → 再退 `ShellRuntime.Instance.Bake` ⇒ 场上只要有一件**被 `Build()` 过**的本件就够，
    /// `ShellRuntime.Instance == null` **不妨碍** ⇒ `Shell/GameWindow.cs` **一行都不用改**。</para>
    /// <para>**为什么挂在场景根、⛔ 不进菜单树**：`MainMenuWindow.ToBackground()` 会把「背景 + 模式卡」整棵
    /// `SetActive(false)`；快照要是那棵树的孩子，就会跟它一起被藏掉 ⇒ 底还是黑的（这正是 `B-237` 那张图的成因）。</para>
    /// <para>**触发链**（静态读码）：开第一扇窗时 `WindowsManager.OpenWindow` 走两条支路之一 ——
    /// 全屏支 `HideAllWindows()` → `GameWindow.Hide()` → `SetBake(true)`；弹窗支 `currentWindow.ToBackground()`
    /// → `SetBake(true)`（`WindowsManager.cs:326` / `:332`）。两条支路**都是先烘、后藏**（`GameWindow.cs:653` / `:703`
    /// 都排在 `SetActive(false)` 之前）⇒ 快照里是「还没被藏掉的主菜单」，与原版一致。</para>
    /// <para>返回一行进 `_index.tsv` 的读数；建不出来时**也返回原因**（⛔ 不拿 null 冒充「没这回事」）。</para>
    /// </summary>
    static string EnsureBake()
    {
        try
        {
            // 原版节点名 = `Game UI ▸ Main Canvas ▸ Background Render Texture Holder`（判据 → `Shell/UIBakeScreen.cs` 文件头）
            var go = new GameObject("Background Render Texture Holder");
            var bake = go.AddComponent<UIBakeScreen>();
            bake.cameraToBake = Camera.main;
            bake.Build();          // 批处理/编辑模式下 `Awake` 不跑 ⇒ 显式调（同 `ShellRuntime.Build` 的规矩）

            if (bake.image == null)
            {
                Debug.LogWarning(P + "模糊底**没建成**：`UIBakeScreen.image == null`（`CardArt.Solid()` 没给图）"
                                   + " ⇒ 这一趟的窗图底下仍是**纯黑**（= 原版观感的缺口，⛔ 别当成拍好了）");
                return "模糊底：**没建成**（`image == null`）";
            }

            // 提前建一次 RT：只为把**真实尺寸**报出来（`Toggle(true)` 里也是这一步，幂等）。
            bake.CreateRT();
            var rt = bake.RT;
            if (rt == null) return "模糊底：**RT 没建出来**";

            string size = $"RT {rt.width}×{rt.height}（Screen {Screen.width}×{Screen.height}"
                        + $" × {UIBakeScreen.RENDER_TEXTURE_MULTIPLIER}）";
            if (rt.width < MinBakeRtW || rt.height < MinBakeRtH)
            {
                // 🔴 **必须出声**：这一档是「亮度对、观感不对」，**像素闸查不出来**（非黑占比会很高）⇒
                //    只能靠这条 Warning + 索引表读数让调度台看见。
                Debug.LogWarning(P + "模糊底**退化** —— " + size
                    + $" 小于下限 {MinBakeRtW}×{MinBakeRtH} ⇒ 底下只是**一块整屏平均色**，不是模糊的主菜单；"
                    + "成因 = 批处理下 `Screen.width/height` 很小/为 0，`UIBakeScreen.CreateRT()` 兜到 1×1"
                    + "（那是它原有的兜底，本轮**没有改它**）。判据：`_index.tsv` 每行的 `说明` 列都印了这一项。");
                return "⚠️ 模糊底**退化**：" + size + $" < 下限 {MinBakeRtW}×{MinBakeRtH} ⇒ 底是一块整屏平均色，不是模糊主菜单";
            }
            return "模糊底 " + size;
        }
        catch (Exception e)
        {
            Debug.LogWarning(P + "模糊底**建不出来**（异常）：" + e.GetType().Name + " · " + e.Message
                               + " ⇒ 这一趟的窗图底下仍是**纯黑**");
            return "模糊底：**建不出来**（" + e.GetType().Name + "）";
        }
    }
}
