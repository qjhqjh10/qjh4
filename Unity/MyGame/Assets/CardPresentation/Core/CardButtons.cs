// CardButtons.cs — 卡片详情窗下缘**那两颗圆钮「建不建」**（判据只此一份）
//
// 为什么单开一个文件：菜单版（`Shell/CardDetailPopup`）与战斗版（`Battle/CardDisplayWindow`）
// 各有一套「语音钮 / 显示卡面文字钮」，而**建不建由卡决定**这条判据写两份迟早不一致。
//
// 原版出处（反编译 + 字段偏移逐条核过 → `资料/阶段二_卡片详情窗_原版规格.md` §十·3）：
//   · **语音钮** `voiceOverButton`（窗口 `+0x100`）：`SetCardVoiceOver` 按
//     `GetAvailableSounds().Count > 0` 决定；Hero/Minion 还看两个运行期开关
//     `EnableWarlordVOs` / `EnableTroopVOs`（实机都是 true）⇒ **等价判据 = 这张卡有没有声音**。
//     我们的等价物 = **`VoiceLines.Has(卡 id)`**（实测覆盖：hero 50/53 · unit 549/656 · **tactic 4/484**）。
//   · **眼睛钮** `showCardTextButton`（`+0x108`）：`ShowCard` 里
//     `SetActive(cardType == 0 || cardType == 10)` —— **0 = Minion、10 = Hero**
//     ⇒ 我们的等价判据 = `type` 是 `unit` / `hero`（战术卡与防御卡都不建）。
//
// ⚠️ 语音表的键 = **引擎卡 id**（原版卡）/ **卡名**（我们自设计的那 26 张）——
//    与 `BattleDriver.ToCardData` 里 `artId = ArtKey(c)` 是**同一件事**（那个键兼着立绘命名的活）。
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public static class CardButtons
    {
        /// <summary>语音表的索引键。原版卡 = 引擎卡 id；自设计卡 = 卡名（自然查不到 ⇒ 没有语音钮）。</summary>
        public static string VoiceKey(CardData d) { return !string.IsNullOrEmpty(d.artId) ? d.artId : d.id; }
        public static string VoiceKey(CardDef def) { return BattleDriver.ArtKey(def); }

        /// <summary>语音钮建不建 —— **这张卡有没有单位语音**。</summary>
        public static bool HasVoice(CardData d) { return VoiceLines.Has(VoiceKey(d)); }
        public static bool HasVoice(CardDef def) { return def != null && VoiceLines.Has(VoiceKey(def)); }

        /// <summary>眼睛钮（显示卡面文字）建不建 —— 原版 `cardType == 0 || == 10`。</summary>
        public static bool HasTextButton(CardData d) { return HasTextButton(d.type); }
        public static bool HasTextButton(CardDef def) { return def != null && HasTextButton(def.Type); }

        static bool HasTextButton(string type) { return type == "unit" || type == "hero"; }
    }
}
