// DailyStreakTab.cs — 🆕「每日连胜」页（自建 · A815），奖励窗左栏第 4 格。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**——
//    本类会被 `AddComponent<DailyStreakTab>()`（`RewardsWindow.BuildTabContents`）⇒ 必须同名文件，
//    否则建不出脚本资产、场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 🆕 每日连胜页（自建 · A815）

    /// <summary>🆕 **「每日连胜」页（A815）** —— 奖励窗左栏**第 4 格**。
    ///
    /// <para>🔴 **这一页整个是【自建】的**（**用户 2026-10-15 拍板**），三条边界：
    /// ① **只显示**「本地累计的连续登录天数」，**不做奖励**（不发卡包/金币/骷髅，不接 `Wallet`）；
    /// ② **不接 LiveOps**、不连服务器（原版「每日连胜」是一个 live-ops 事件
    /// `DailyStreakWindow : LiveOpsEventWindow&lt;MainMenuMission&gt;`，数据在远端 CCD）；
    /// ③ 天数**来自 `DailyData` 的本地记录**（跨天 +1 · 断天归 1 · 落盘），⛔ **不是写死的数**。</para>
    ///
    /// <para>⚠️ **原版侧的口径**（照 `资料/普查产出_1015/R5_A815_A817_A819判据.md` §一）：
    /// 原版奖励窗的 `tabs` **只有 Missions/Forge/Campaign 三条**，第 4 格是**母版按钮**；实拍里那个
    /// 「每日连胜」页签是 `BaseParentEventWindow.SetupTabs` 运行期按 live-ops 事件加出来的，
    /// 点下去开的是**另一扇全屏窗**（不是本窗的一页）。我们**没有**那套数据 ⇒ 按用户拍板做成**本窗的第 4 页**。</para>
    ///
    /// <para>⚠️ **凡本页里的数字/文案，出处都写在各处注释里**：矩形与字号**全是我们挑的**
    /// （原版没有这一页 ⇒ 没有可照的尺子）；文案复用 `DailyData` 里的两条 —— `StreakCurrentLabel` 仍是
    /// **我们挑的**，`StreakInfoText` **2026-10-19（W57 · UI-6）起 = 官方词条** `Rewards Menu/DailyStreak/Help`
    /// （🔴 本行原文写「两条都已经标着『我们挑的』」—— 对后一条**已过期**，2026-10-19 UI-7 就地订正，铁律 5）。</para></summary>
    public class DailyStreakTab : WindowTabBase
    {
        public override WindowTabType Type { get { return WindowTabType.DailyStreak; } }

        RewardsWindow _win;
        Transform _root;

        /// <summary>页矩形：与 `Campaign Tab` 同一格（`RewardsWindow.TabRectFor(0f, 0f)` 算出来 = 330.69..1920 × 70.94..1080）。
        /// 🔶 自己设计（非原版）：每日连胜页的页面矩形 `TabL/TabT/TabR/TabB` · 原版没有这一页 · TODO(谁定的)｜原注：⚠️ **我们挑的** —— 原版没有这一页，没有可照的矩形。</summary>
        public const float TabL = 330.69f, TabT = 70.94f, TabR = 1920f, TabB = 1080f;

        /// <summary>渲染队列：与同族「页」同档（`ForgeTab`/`CampaignTab` 那一带 3027/3064 之内、弹窗 3110+ 之下）。
        /// ⚠️ **我们挑的**（本页没有弹窗，取同族页的中间档）。
        /// 🔴 **`public`**：`Editor/RewardsScene.cs` 有一条「**弹窗的压暗层档必须高于所有页**」的断言，
        /// 2026-10-16（A815）起它把本页这一档也**算进那个比较**（只比常量、不量树）⇒ 得看得见。
        /// ⛔ 改这里的数会让那条断言跟着变，别把最高那一档抬到 3130 以上。</summary>
        public const int QTitle = 3020, QLabel = 3021, QValue = 3022, QInfo = 3023;

        /// 🔶 自己设计（非原版）：大标题文案 `Daily Streak` · 原版没有这一页（用户实拍中文是「每日连胜」，本壳一律英文） · TODO(谁定的)｜原注：<summary>大标题那条**我们挑的**文案（用户实拍那页的大标题是「每日连胜」；本壳一律英文）。</summary>
        public const string TitleText = "Daily Streak";
        /// 🔶 自己设计（非原版）：三条句子的画布矩形 `TitleR/LabelR/ValueR/InfoR` · 原版没有这一页 · TODO(谁定的)｜原注：<summary>三条句子的**画布矩形**（`x1,x2,y1,y2`）—— **全是我们挑的**（原版没有这一页）。
        /// 竖着排在页面中段：标题 → 「Current streak:」+ 数值 → 说明句。</summary>
        static readonly PxRect TitleR = new PxRect(600f, 320f, 1650f, 400f);
        static readonly PxRect LabelR = new PxRect(620f, 560f, 1180f, 640f);
        static readonly PxRect ValueR = new PxRect(1200f, 540f, 1520f, 660f);
        static readonly PxRect InfoR  = new PxRect(600f, 700f, 1650f, 760f);

        /// <summary>文字色：标题/数值取左栏文案那条 **#F4E1AC**（`MainMenuSubmenuWindow.BuildTabButton` 用的同一个色），
        /// 🔶 自己设计（非原版）：标题 / 数值 / 说明三档文字色（Gold / Grey） · 原版没有这一页 · TODO(谁定的)｜原注：「Current streak:」用白、说明句用灰 —— **我们挑的**。</summary>
        static readonly Color Gold = new Color(0.9569f, 0.8824f, 0.6745f);
        static readonly Color Grey = new Color(0.70f, 0.70f, 0.70f);

        Label _value;

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }

        public override void Setup() { Build(); }

        /// <summary>每次切到本页时**重读一次**本地记录（`DailyData.StreakLoggedDays`）。
        /// 🔴 **为什么必须刷**：连登数是**跨天变的**（进壳那一拍 +1），而本页的节点建一次就不重建
        /// （原版 `SetActive` 换页语义 ⇒ `ChangeTab` 只开关 activeSelf）⇒ 不刷就停在开窗那一刻的旧数上
        /// （静默、且只在「跨天之后没重开窗」时才现形）。</summary>
        public override void OnOpen() { RefreshValue(); }

        /// <summary>本页此刻该显示的数值串（**唯一读口**：`OnOpen` 与自检都走它 ⇒ 两处不会各说各的）。</summary>
        public static string ValueText() { return DailyData.StreakLoggedDays.ToString(); }

        /// <summary>自检用：本页**此刻画出来的**那个数（`null` = 那颗字没建出来）。</summary>
        public string DisplayedValue { get { return _value != null ? _value.Text : null; } }

        /// <summary>把数值那一颗字的正文换成 `ValueText()`（⛔ 不重建节点 —— 只改字）。</summary>
        public void RefreshValue()
        {
            if (_value != null) _value.SetText(ValueText());
        }

        /// <summary>建页内容（**可重复调**：先清空本页子件，与各页同一条纪律）。
        /// 🔴 **节点名一律带 `Daily Streak ` 前缀** —— 本仓自检大量用「按名字全树找」（`FindChild`），
        /// 而**连登弹窗**（`DailyStreakPopup`，同一场景里也会被建出来）已经有 `Current Streak` /
        /// `Current Streak Value` 这两颗同名节点 ⇒ 不加前缀就会**撞名**（`FindChild` 取第一个命中 ⇒
        /// 那些断言会静默量到**别人家的**节点）。⛔ 别把前缀去掉。</summary>
        public void Build()
        {
            MenuDraw.ClearChildren(_root);

            // 大标题（**我们挑的**）
            _win.Text(_root, TitleText, TitleR.x1, TitleR.x2, TitleR.y1, TitleR.y2, 6, Gold,
                      "Daily Streak Title", 70f);   // A1024③ 判不出（原版页签是 live-ops 运行期加的）⇒ 保持 Cjk
            // 「Current streak:」—— 复用 `DailyData.StreakCurrentLabel()`（那条串自己标着「我们挑的」；
            // 🔴 **不在这里再写一份字面量**：字面量只留在 `DailyData` 那一处，与连登弹窗共用同一条）。
            _win.Text(_root, DailyData.StreakCurrentLabel(), LabelR.x1, LabelR.x2, LabelR.y1, LabelR.y2,
                      6, Color.white, "Daily Streak Label", 70f, face: TmpFont.Face.LatinUi);
            // 数值 —— **本页唯一的数据件**，值 = `DailyData.StreakLoggedDays`
            _value = _win.Text(_root, ValueText(), ValueR.x1, ValueR.x2, ValueR.y1, ValueR.y2,
                               6, Gold, "Daily Streak Value", 90f, face: TmpFont.Face.LatinUi);
            // 说明句（复用 `DailyData.StreakInfoText()`）。
            // 🔴 **2026-10-19（W57 · UI-7）就地订正（铁律 5）**：本行原来写「同样『我们挑的』」—— **已过期**：
            //   该串自 UI-6 起 = **官方词条** `Rewards Menu/DailyStreak/Help`
            //   （判据 / 更正痕迹 → `DailyData.StreakInfoText()` 的 doc）。
            // 🔴 **`wrapPx` 不能省**：官方英文句约 103 字符，而本页框只有 `InfoR.W` = **1050px**（字号 36）
            //   —— `Label.BuildTmp` 对单行标签硬写 `NoWrap`（`Battle/Label.cs` 的 `SetWrapWidth` doc）
            //   ⇒ 不传就一路画出框外。折行宽取**本框宽**，与连登弹窗那两颗原版 TMP 同口径
            //   （原版 `m_TextWrappingMode = 1`（Normal）⇒ `DailyStreakPopup` 传 `S_Info.W` / `F_Info.W`）。
            _win.Text(_root, DailyData.StreakInfoText(), InfoR.x1, InfoR.x2, InfoR.y1, InfoR.y2,
                      6, Grey, "Daily Streak Info", 36f, wrapPx: InfoR.W, face: TmpFont.Face.LatinCardBody);
        }
    }
}
