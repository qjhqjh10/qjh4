// BattleHudLanguage.cs — 战斗 HUD 的「换语言 ⇒ 重刷整棵树」广播口
//
// ============================================================================================
//  判据（全部亲读）
// ============================================================================================
//  ① 原版广播口 = `BattleHud.BroadcastChangedLanguage()`（🥈 `cpp2il/out_cpp2il_dev_ilspy/
//     BattleHud.cs:399-407`）：方法体**只有一句** `BroadcastMessage("ChangedLanguage")`。
//  ② 触发链 = `LanguageSelector.cs:46/48`（设置窗那颗语言下拉）
//     → `BattleManager.BroadcastChangedLanguage()`（`BattleManager.cs:21652`）
//     → `battleHud.BroadcastChangedLanguage()`。
//     ⇒ **只有「局内改语言」这一条入口**（不是每次启动、也不是每次开窗）。
//  ③ 四个实现者（`BroadcastMessage` 按名字打到整棵子树）：
//     · `BasicCardUI.ChangedLanguage()`（`BasicCardUI.cs:465`）→ `SetCardTextsExcludingStats(rawCardData)`
//       = 重写**卡名 / 描述 / 兵种 / 阵营**（⛔ 不动数值）
//     · `BattleCardUI.ChangedLanguage()`（`BattleCardUI.cs:992`，override）→ 同上 + 在手牌里再走一次
//       `SetCardTextInHand(option:true)`
//     · `ClockManager.ChangedLanguage()`（`ClockManager.cs:197`）—— 它还**另**在 `Start()` 里
//       `LocalizationManager.OnLocalizeCallback += ChangedLanguage`（`:101` / `:806`）
//     · `CemeteryManager.ChangedLanguage()`（`CemeteryManager.cs:261`）= 把战斗日志**每一行**的
//       文案按当前语言重拼一遍（遍历 `cemeteryActions` / `newCemeteryActionList`，逐行重设
//       `cemeterySliderText`）。
//  ④ 我们这一侧**为什么需要一个显式广播**：`Core/Loc.SetLanguage` 的 doc 明写「**本类不发事件** ——
//     调用方自己把已经画出来的字重设一遍」（那是按「两个设置窗不会同时开着」推的）。可**战斗 HUD 里
//     已经画出来的字**（卡面 / 日志行）不会因为那一句自己重排 ⇒ 原版有这个广播，我们缺的正是它。
//
// ============================================================================================
//  ⚠️ 本文件只提供「广播口」这一件；**订阅方分属三条线**（见 `报告/E6_第3轮.md` §⑤）：
//    · 卡面重排字（`Core/CardView.cs`）—— **E7**
//    · 战斗日志重拼行（`BattleDriver.RefreshBattleLog`）—— **E5**
//    · 回合钮文案 —— **不需要**订阅：我们的 `BattleDriver.UpdateClockLabel` 每帧重算一遍
//      （原版 `ClockManager` 那一条是因为它只在 `ChangedLanguage` 里重设）。
//  ⛔ 别把本类改成「自动去满地找组件」——原版 `BroadcastMessage` 打的是**整棵 HUD 子树**，
//     而我们的卡面/日志不是 UGUI 子件（是 `CardView` / 数据行）⇒ 显式订阅才是等价的那条路。
// ============================================================================================
using System;

namespace CardPresentation
{
    /// <summary>战斗 HUD 的「换语言」广播（原版 `BattleHud.BroadcastChangedLanguage` 的等价物）。
    /// <para>只在**局内改语言**时由 <see cref="SettingsPanel.ChooseLanguage"/> 发一次。</para></summary>
    public static class BattleHudLanguage
    {
        /// <summary>订阅方（卡面 / 日志 …）。</summary>
        public static event Action Changed;

        /// <summary>发一次广播，返回**订户个数**（0 = 没人听 ⇒ 调用方会把这件事写进日志，⛔ 不静默）。</summary>
        public static int Notify()
        {
            var h = Changed;
            if (h == null) return 0;
            int n = h.GetInvocationList().Length;
            h();
            return n;
        }

        /// <summary>清空订户（自检用；`SettingsPanel` 每次 `Build` 都重订一次，⛔ 别让静态表越积越多）。</summary>
        public static void ClearForTest() { Changed = null; }

        /// <summary>现在有几个订户（自检用）。</summary>
        public static int SubscriberCount
        {
            get
            {
                var h = Changed;
                return h == null ? 0 : h.GetInvocationList().Length;
            }
        }
    }
}
