// BattleBigChat.cs — 原版 `BattleManager.DisplayBigChat` 的两个重载（教程 / 战役里「督军大聊天」那一层）
//
// ═════════════════════════════════════════════════════════════════════════════
//  判据链（第一权威 = 反编译 + **VA 反汇编**；🔴 2026-10-29 定案：本 build 两个重载【都是空桩】—— 不是「被剥」，是真的不显示）
//  （2026-10-29 按 §18·0 从「自设计显示」翻成「照抄空桩」；旧结论保留在本文件末尾的 [旧 …] 段）
//  ① **重载 A** `decomp_full/BattleManager__DisplayBigChat.c:13-26` + ILSpy `:21664`（VA `@0x1809604B0`）：
//       `DisplayBigChat(string chatMsgText, CardScript talkingUnit)`
//       · `if (string.IsNullOrEmpty(chatMsgText)) LogWarning("$Trying to send empty chat")`（`.c:24-26`）；
//       · `else if (talkingUnit == null) { LogError("$DisplayChatBox without talkingUnit defined"); return; }`（`.c:18-22`）；
//       · 否则 **tail-call `RawCardScript$$get_fullHeroSprite` 并把返回值丢弃**（VA：`jmp 0x18094c780` = 该属性 getter）。
//  ② **重载 B** `decomp_full/BattleManager__DisplayBigChat.c` 同一支 + ILSpy `:21683`（VA `@0x180960580`）：
//       `DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)` —— 两道 guard **逐字同上**；
//       「文案非空 ∧ sprite 非空」⇒ **直接 `ret`**（VA：0x1809605F6 `je` → 收尾），**没有第三个调用**。
//  ③ 🔴 **定案（2026-10-29 · 调度台按用户 §18·0 裁定）**：两个重载在本 build **都是空桩** ——
//     · 旧说法「显示调用被反编译剥掉了」**不成立**：VA 反汇编里正常路径本来就没有调用（A 只有属性 getter、B 直接返回）；
//     · ⇒ 正确复刻 = **被调用、但什么也不显示**（= 原版可观测行为：`*ChatBig` 在正常支**看不到气泡**）。
//     · ⛔ 旧版那段「按三条侧面证据选 `RadioChat` 来显示」= 在**原版没有的功能上自造**，已按 §18·0 删除该行为；
//       那三条侧面证据（场景只有 3 颗框 / Sprite 参数拿不到阵营 / 同族 tip 走 Radio）**只作历史留痕**，不再是实现依据。
//  ④ **调用方**（`AiScripted__ExecuteAction.c` / ILSpy `AiScripted.cs:1933-1946` · `:2406` · `:2057`）：
//       · `AiChatBig(65)`：文案 = `GetLocalizedText()`，空则 `ChatManager.GetChatMessage(Intro, false)`；
//         且 **actingUnitType == EnemyMinion(40)** 时**不**走 BigChat 而是走 `DisplayChatBox`（`.c:296-337`）；
//         **说话人 = `manager.GetHero(playerHeroY: false)`**（= 敌方督军）⇒ 走重载 A（空桩）。
//       · `PlayerChatBig(55)`：**actingUnitType == PlayerMinion(30)** 时走 `DisplayChatBox`（`.c:611-620`）；否则 ⇒ 重载 B（空桩）。
//       ⇒ 「大聊天」在正常支**什么都不显示**；那句「A 只是把 CardScript 换成 fullHeroSprite 再转发 B」**也是错的**（VA 见 ①）。
//  ⑤ **立绘（历史）**：`fullHeroSprite` ↔ 我们 `CardArt.TryDeckHero`；⚠️ **空桩之后这条链不再被调用**，
//     保留 `FullHeroSprite` / `ApplyPortrait` 只为判据若翻转（原版改回可见）时能启用；⛔ 别在空桩路径里调它。
//  ⑥ **时长（历史）**：`VoiceLineData` 的 `duration` = 2.0 s（`GetCurrentClipLength(null)`）—— **空桩之后不消费**；
//     脚本本身的等待仍由 `waitBefore/waitAfter` 决定（见 `TutorialScript.PlayScriptedTurn` 的 `A899` 节）。
//
//  ⚠️ 与普通小气泡的关系：`DisplayChatBox`（单位说话，走 Player/Enemy 两颗、带卡面插图）**照常显示**；
//     `*ChatBig` 的「普通气泡」分支只在小兵目标那一支出现（④ 的两个 minion 条件）⇒ **两条路是两条，别合并**。
//
//  📌 本文件的公开面（`Display` 两个重载 / `Shown` / `LastText` / `LastError` / `ResetCounters`）**签名不变**：
//     调用点（`BattleDriver.ApplyTutorialActionView`）无需改；⚠️ 但它那句「大框聊天已显示」的 `Ctx.Log` 文案需它的 owner 改（本代理无权）。
//
//  ── 以下为 2026-10-29 之前的旧判据链（保留原文以便对账，⛔ 不是现状）─────────────────────────
//  [旧 ①] A 取 `talkingUnit.rawCardData.fullHeroSprite`（ILSpy `:21672`）—— **然后就没有下文了**。
//  [旧 ②] B 两道 guard（文案空 ⇒ warning；sprite 空 ⇒ error），**显示调用同样读不到**。
//  [旧 ③] 「显示调用判据拿不到（两棵树都只剩 guard）」—— 已由 VA 反汇编**证伪**（见现行 ③）。
//  [旧 ③-a/b/c] 三条侧面证据：场景 `Unit Chat` 只有 3 颗框（`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:255-306`）·
//      Sprite 参数拿不到阵营 · 同族 tip 走 `ShowChatBox(2, …)` ⇒ 当时据此选了 `RadioChat`（现作废）。
//  [旧 ④] 「两个入口都进重载 B（A 把 CardScript 换成 fullHeroSprite 再转发）」—— VA 证伪（A 不转发，见现行 ①）。
//  [旧 ⑤] 立绘 = `fullHeroSprite`；我们 `CardArt.TryDeckHero`（判据 = `fullHeroSpriteReference`，56 张里 36 张 SO 原文 + 20 张 GUID→容器）。
//  [旧 ⑥] 时长 = `UnitChatPanel.Duration(null)` = 2.0 s（与 `GetCurrentClipLength(null)` 同值）。
//
//  🔴 结论：**照抄空桩** —— 调用照旧、**不显示**、**一次出声**说明「这不是缺陷是复刻」；⛔ 别改回可见。
// ═════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>
    /// 原版 `BattleManager.DisplayBigChat(string, CardScript)` / `(string, Sprite)` 两个重载。
    /// <para>🔴 2026-10-29 定案：两个重载在本 build **都是空桩**（VA `@0x1809604B0` / `@0x180960580`）⇒
    /// 本类**被调用但不显示任何东西**（照抄原版）；⛔ 别把它改回可见。</para>
    /// </summary>
    public static class BattleBigChat
    {
        /// <summary>原版 `VoiceLinesOnSceneType.Radio` = **2**（`UnitsVoiceLinesPanel__GetChatBox.c` 的 `+0x30`）。
        /// ⚠️ **2026-10-29 起只是历史常量**：空桩之后不再用它选框；保留它只为判据若翻转成「可见」时能直接复用（见文件头 ③）。
        /// 旧的 `🔶 自己设计` 依据（三条侧面证据）**已作废**，原文留痕在文件头 [旧 ③-a/b/c]。
        /// ⛔ 别在空桩路径里再调 `Speak(Radio)`。</summary>
        public const int Side = 2;
        /// <summary>`RadioChat` 那一颗立绘件的**对象名**（`UnitChatPanel.MakeBubble` 里 `"chat_card_" + name`）。</summary>
        public const string PortraitNodeName = "chat_card_RadioChat";
        /// <summary>原版那两句调试串**逐字照抄**（`CustomDebug` 的 `$` 前缀是原版的拼接标记，保留以便对账）。</summary>
        public const string EmptyChatWarning = "$Trying to send empty chat";
        public const string NoUnitError = "$DisplayChatBox without talkingUnit defined";
        /// <summary>`Speak` 的 `evt` 默认值 —— ⚠️ 它只参与「官方词条键 `Chat_Messages/<id>-Chat<后缀>`」的拼法，
        /// 大聊天的文案是**调用方给的原文**（原版 `action.GetLocalizedText()`）⇒ 故意用一个**不会命中**的
        /// 后缀，保证显示的就是传进来的那句（⛔ 别改成能命中的值，那会把原文换掉）。</summary>
        public const string DefaultEvt = "BigChat";

        /// <summary>自检：**成功显示了几句** —— 空桩之后恒 **0**（这是判据，不是缺陷；`Shown++` 已删）。</summary>
        public static int Shown { get; private set; }
        /// <summary>自检：最后一次**收到**的文案（空桩不显示，但入参会记下来，便于对账）。</summary>
        public static string LastText { get; private set; }
        /// <summary>自检：最后一回是否**真的换上了**立绘 —— 空桩之后恒 `false`（`ApplyPortrait` 已无调用点）。</summary>
        public static bool LastPortraitApplied { get; private set; }
        /// <summary>自检：最后一次的 guard 档（`"empty"` / `"noportrait"` / `null` = 没挡）。</summary>
        public static string LastError { get; private set; }

        /// <summary>清计数（自检每段之前调）。</summary>
        public static void ResetCounters()
        {
            Shown = 0; LastText = null; LastPortraitApplied = false; LastError = null;
            _warnedNoRadio = false;
        }
        static bool _warnedNoRadio;

        /// <summary>原版重载 A：`DisplayBigChat(string chatMsgText, CardScript talkingUnit)`。
        /// ⚠️ **VA 更正（2026-10-29）**：A **不转发** 重载 B —— 它的正常路径是 `tail-call RawCardScript$$get_fullHeroSprite` 后**丢弃返回值并返回**（`@0x1809604B0`）。
        /// <returns>true = 已按原版处理完（⚠️ **不是**「显示了」：两个重载在本 build 都是空桩）。</returns></summary>
        public static bool Display(UnitChatPanel panel, string text, RuleEngine.CardDef talkingUnit,
                                   float seconds = 0f, string evt = DefaultEvt)
        {
            if (string.IsNullOrEmpty(text)) { LastError = "empty"; Debug.LogWarning("[BattleBigChat] " + EmptyChatWarning); return AcknowledgeStub(); }   // 原版两道 guard 之一（A 与 B 各自独立一份，= 原版形状）
            if (talkingUnit == null) { LastError = "noportrait"; Debug.LogError("[BattleBigChat] " + NoUnitError); return AcknowledgeStub(); }        // 原版另一道 guard（A 是 `talkingUnit == null`，B 是 `sprite == null`）
            return AcknowledgeStub();   // 原版：取 `rawCardData.fullHeroSprite` 后**丢弃**（VA：tail-call 到 getter 即返回）—— 我们不取、不显示、不转 B
        }

        /// <summary>原版重载 B：`DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)`。
        /// 🔴 **2026-10-29 定案（调度台裁定 · 用户 §18·0 口径「含原版 bug / 死支 / 空桩一律照抄」）**：两个重载在本 build **都是空桩** ——
        /// VA `@0x180960580`：正常路径（文案非空 ∧ sprite 非空）**直接 return**，没有第三个调用；
        /// 早退两支只调 `CustomDebug.LogWarning`（空文案）/ `CustomDebug.LogError`（sprite null）。
        /// ⇒ 本方法照抄：**被调用、但什么也不显示**（⛔ 别改回可见：那会造出原版没有的大聊天）。
        /// <para>两道 guard 与原版逐字同串（`$Trying to send empty chat` / `$DisplayChatBox without talkingUnit defined`）。</para>
        /// <para>返回值 **true** = 「已按原版处理完」，**不是**「显示了」；`Shown` 不 ++。调用方**不许**在 true/false 上分支去补普通气泡
        /// （原版 BigChat 之后是公共出口，不回落小气泡）。</para>
        /// <para>⚠️ 原版 `*ChatBig` 的「普通气泡」只出现在 acting 是小兵那一支（`PlayerChatBig`+`PlayerMinion(30)` / `AiChatBig`+`EnemyMinion(40)`）⇒
        /// 那个分档属调用方（BattleDriver），见方法体内注。今天教程数据 `*ChatBig` 0 条 ⇒ 零可观测差。</para>
        /// <para>形参 `panel` / `artKey` / `seconds` / `evt` 不再消费；`ApplyPortrait`（旧立绘桥）因此当前无调用点，保留待判据翻转。</para>
        /// </summary>
        public static bool Display(UnitChatPanel panel, string text, Texture2D portrait,
                                   string artKey = null, float seconds = 0f, string evt = DefaultEvt)
        {
            LastText = text;
            LastPortraitApplied = false;

            // 🔴 2026-10-29（调度台裁定 · §18·0「含原版 bug / 死支 / 空桩一律照抄」）：
            //    原版 `BattleManager.DisplayBigChat` 两个重载在本 build **都是空桩**（VA 实证，不是「反编译丢体」）：
            //      · 重载 A `@0x1809604B0`：两道 guard 之后 **tail-call `RawCardScript$$get_fullHeroSprite` 并把返回值丢弃**后返回；
            //      · 重载 B `@0x180960580`：「文案非空 ∧ sprite 非空」时**直接 return**（正常路径**没有第三个调用**）。
            //    ⇒ 本方法照抄：**被调用、但什么也不显示**。⛔ 别把它改回可见（那会造出原版没有的大聊天）。
            //    VA 同时证明 **重载 A 并不转发重载 B**（A 的 tail-call 是属性 getter）⇒ 两者各自独立为空桩。
            //
            // ⚠️ 调用方（BattleDriver.ApplyTutorialActionView）注意：原版 `*ChatBig` 两支**不是无条件进这里** ——
            //    `AiScripted__ExecuteAction.c:611-620`（PlayerChatBig 0x37）与 `:296-337`（AiChatBig 0x41）：
            //      · `PlayerChatBig(55)`：actingUnitType **== PlayerMinion(30)** ⇒ 走 `DisplayChatBox`（**普通气泡**）；否则 ⇒ 本方法（空桩）。
            //      · `AiChatBig(65)`：actingUnitType **== EnemyMinion(40)** ⇒ 走 `DisplayChatBox`；否则 ⇒ 本方法（空桩）。
            //    ⇒ 「先试本方法、失败再回退小气泡」那个写法**与原版不同**：原版的「普通气泡」只在小兵目标那一支出现。
            //    今天 6 关教程 `*ChatBig` **0 条**（`E5P12` 实测）⇒ 零可观测差；但接数据前请调用方把上面两行分档补上。
            //
            // ⚠️ 账更正（铁律 5）①：账 294/295 原写「重载 A 取 `fullHeroSprite` 后**转发给**重载 B」—— VA 证明**不转发**。
            // ⚠️ 账更正 ②：账 294/295 原写「显示调用本体被剥（反编译丢体）」—— VA 证明**不是丢体、是真的空桩**（两条正常路径都没有第三个调用）。
            // ⚠️ 账更正 ③：旧注释把「显示目标 = RadioChat」记成 `🔶 自己设计` —— 那是**在原版没有的功能上自造**；本次按 §18·0 删掉该行为（代码保留、不显示）。
            //
            if (string.IsNullOrEmpty(text))
            {
                LastError = "empty";
                Debug.LogWarning("[BattleBigChat] " + EmptyChatWarning);   // 原版 `CustomDebug.LogWarning`（这一支唯一行为）
                return AcknowledgeStub();   // ⚠️ 原版没有失败分支 ⇒ 调用方**不许**因它退回小气泡（回退 = 造原版没有的行为）
            }
            if (portrait == null)
            {
                LastError = "noportrait";
                Debug.LogError("[BattleBigChat] " + NoUnitError);          // 原版 `CustomDebug.LogError`（这一支唯一行为）
                return AcknowledgeStub();   // 同上：原版照样不显示、也不会改走普通气泡
            }
            LastError = null;
            // 形参 `panel` / `artKey` / `seconds` / `evt` 现在**全不消费**（空桩没有显示口）—— 保留签名只为不改调用方与接口形状。
            // `ApplyPortrait`（立绘桥）因此**当前无调用点**；保留它 + 它的 doc，等判据若翻转成「可见」再启用（⛔ 别删）。
            // `Shown` 计数**不 ++**（保持 0）：那个计数只能代表「真的显示了」，空桩不许把它刷成 1。
            // `EmptyChatWarning` / `NoUnitError` 两句串仍是原版原文（`$` 前缀保留），审计时可直接 grep。
            // `Side`（RadioChat=2）与 `PortraitNodeName` 现在无消费者（保留常量，见 `ApplyPortrait` 的 doc）。
            // 语音不在这里放：原版 BigChat 的 VoiceLineData 是 `audioClip: null`；脚本自带的 `sound` 由 ExecuteAction 末尾 `PlaySoundAsset` 放。
            // 📌 保留 `BattleBigChat` 这个类型与两个 `Display` 签名：调用点不改；将来判据若翻转只需把这里改回可见。
            return AcknowledgeStub();   // 返回值 = 「已按原版处理完」，**不是**「显示了」
        }

        /// <summary>原版 `talkingUnit.rawCardData.fullHeroSprite` 的等价物。
        /// 判据 = `CardArt.TryDeckHero`（那张表的两个来源**就是** `fullHeroSpriteReference`）；
        /// 取不到 ⇒ 退回 `CardArt.Portrait`（卡面插图，**兜底、非原版**，且 `TryDeckHero` 已经出过声）。</summary>
        public static Texture2D FullHeroSprite(RuleEngine.CardDef card)
        {
            if (card == null) return null;
            Texture2D tex;
            if (CardArt.TryDeckHero(card.Id, out tex) && tex != null) return tex;
            return CardArt.Portrait(card.Id);
        }

        /// <summary>把传入的立绘写进 `RadioChat` 那颗气泡的立绘位（桥，理由见 <see cref="Display(UnitChatPanel, string, Texture2D, string, float, string)"/> 的 doc）。
        /// 比例**不写死**：换图前先量那颗件当前的设计 px 宽高比，换完按同一个比拽回去
        /// （`UnitChatPanel.MakeBubble` 建件时设的就是原版那一格 `144.46 × 157.46`）⇒ 本文件不复制它的常量。</summary>
        static void ApplyPortrait(UnitChatPanel panel, Texture2D tex)
        {
            Transform node = panel.transform.Find(PortraitNodeName);
            if (node == null)
            {
                Debug.LogWarning($"[BattleBigChat] 找不到立绘件 `{PortraitNodeName}`（面板层级换了？）"
                               + " ⇒ 这一句**退回卡面插图**显示（不静默；修法见本文件 Display(Texture2D) 的 doc）。");
                return;
            }

            var img = node.GetComponent<Image>();
            HudPiece piece;
            if (img != null) piece = HudPiece.Of(node as RectTransform, img);
            else
            {
                var quad = node.GetComponent<ImageQuad>();       // 世界档（没迁 uGUI 的分支）
                piece = quad != null ? HudPiece.Of(quad) : null;
            }
            if (piece == null)
            {
                Debug.LogWarning($"[BattleBigChat] 立绘件 `{PortraitNodeName}` 上既没有 `Image` 也没有 `ImageQuad`"
                               + " ⇒ 退回卡面插图（不静默）。");
                return;
            }

            Vector2 before = piece.SizePx;
            float aspect = (before.y > 0.01f) ? before.x / before.y : 0f;
            piece.SetTexture(tex);
            if (aspect > 0f) piece.SetAspect(aspect);             // 换图会把贴图比例带进来，拽回原版矩形
            LastPortraitApplied = true;
        }

        // ==================================================================
        //  🆕 2026-10-29（调度台裁定）· 空桩的唯一出声口
        // ==================================================================
        //  两个 `Display` 都走这里：**一次** `Debug.Log` 说明「原版此功能是空桩、我们照抄」，
        //  然后返回 **true**（= 已按原版处理完；**不是**「显示了」）。`Shown` 一律不 ++。

        /// <summary>空桩的一次性出声 + 统一返回值（唯一一处，理由见 `Display(Texture2D)` 的 doc）。
        /// <para>⚠️ 返回 **true** 的语义 = 「已按原版处理完」；调用方**不许**据此再补一句「已显示」的日志，也不许在 false 上回退普通气泡
        /// （原版 BigChat 之后就是公共出口）。</para></summary>
        static bool AcknowledgeStub()
        {
            if (!_warnedStub)
            {
                _warnedStub = true;
                Debug.Log("[BattleBigChat] 原版 `BattleManager.DisplayBigChat`（A `@0x1809604B0` / B `@0x180960580`）"
                        + "在本 build **是空桩**（A 取完 `rawCardData.fullHeroSprite` 丢弃；B 两道 guard 之后直接 return）"
                        + " ⇒ 我们照抄：**不显示**。这不是缺陷、是复刻（用户 §18·0 口径 · 调度台 2026-10-29 裁定）；⛔ 别把它改回可见。");
            }
            return true;
        }
        static bool _warnedStub;
    }
}
