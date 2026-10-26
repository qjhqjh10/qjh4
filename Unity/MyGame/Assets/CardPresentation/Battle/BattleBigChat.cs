// BattleBigChat.cs — 原版 `BattleManager.DisplayBigChat` 的两个重载（教程 / 战役里「督军大聊天」那一层）
//
// ═════════════════════════════════════════════════════════════════════════════
//  判据链（第一权威 = 反编译；**这一支的「显示调用」在原版里被剥掉了**，如实记在下面 ③）
// ═════════════════════════════════════════════════════════════════════════════
//  ① **重载 A** `decomp_full/BattleManager__DisplayBigChat.c:13-26` +
//     ILSpy `out_cpp2il_dev_ilspy/BattleManager.cs:21664`：
//       `DisplayBigChat(string chatMsgText, CardScript talkingUnit)`
//       · `if (string.IsNullOrEmpty(chatMsgText)) LogWarning("$Trying to send empty chat")`（`.c:24-26`）；
//       · `else if (talkingUnit == null) { LogError("$DisplayChatBox without talkingUnit defined"); **return**; }`
//         （`.c:18-22`，`.c` 里那句 `return` 是真的早退）；
//       · 否则取 `talkingUnit.rawCardData.fullHeroSprite`（ILSpy `:21672`）—— **然后就没有下文了**。
//  ② **重载 B（唯一实现）** `BattleManager__DisplayBigChat.c` 同一支 + ILSpy `:21683`：
//       `DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)` —— 两道 guard **逐字同上**
//       （文案空 ⇒ warning；sprite 空 ⇒ error），**显示调用同样读不到**。
//  ③ 🔴 **「真正的显示调用」判据拿不到**（两棵树都只剩 guard）：
//     · `decomp_full` 那一份的正文 = 两个 `CustomDebug__Log*` + 两次 `return`，**没有第三个调用**；
//     · ILSpy 那一份在 `sprite = …fullHeroSprite` 之后就断了（方法体里那句被反编译器整条丢掉，
//       与 `CLAUDE.md` 记的「ILSpy 丢内联/常量」是同一族）。
//     ⇒ 版式与「用哪一颗框」**没有第一手判据**，只能靠**可查的三条侧面证据**定，逐条如下：
//        (a) **场景树里没有第四颗聊天框**：13 个 `battlearena*` 场景的 2D 全树里，`Unit Chat` 下
//            只有 `PlayerChatDisplay` / `EnemyChatDisplay` / `RadioChat` 三颗 `VoiceLineBox`
//            （`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:255-306`；三颗 `sd` **逐字相同**
//            = `648.774 × 236.505`）⇒ **「大」不是「框更大」**，是「谁在说」。
//        (b) **`DisplayBigChat` 的第二参数是 `Sprite`**（不是 CardScript）⇒ 它**拿不到「哪一方」**
//            ⇒ 显示目标必须是**与阵营无关的那一颗** ⇒ 三颗里只有 `RadioChat`（`VoiceLinesOnSceneType.Radio`
//            = 2，`UnitsVoiceLinesPanel__GetChatBox.c`：`0→+0x20 / 1→+0x28 / 2→+0x30`）。
//        (c) 同族调用 `DisplayWarlordChatMessage` 的那条 tip 走的就是 `ShowChatBox(panel, **2**, data)`
//            （我们 `BattleDriver` 那一段已按它落地：`UnitChatPanel.Speak(2, …)`）。
//     ⇒ **本文件按 `RadioChat` 那一颗实现**，并把「选这一颗」如实标成 `🔶 自己设计`（见 `Show` 的 doc）。
//  ④ **调用方**（`AiScripted__ExecuteAction.c` / ILSpy `AiScripted.cs:1933-1946` · `:2406` · `:2057`）：
//       · `AiChatBig(65)` ⇒ 文案 = `action.GetLocalizedText()`，空则 `ChatManager.GetChatMessage(Intro, false)`；
//         **说话人 = `manager.GetHero(playerHeroY: false)`**（= 敌方督军）⇒ 走重载 A；
//       · 另一支（`PlayerChatBig(55)` 与「按卡取立绘」那条）先 `rawCard.fullHeroSpriteReference.Load()`
//         拿到 Sprite ⇒ 走**重载 B**。
//       ⇒ 两个入口都进重载 B（A 只是把 CardScript 换成它的 `fullHeroSprite` 再转发）—— 与本文件的形状一致。
//  ⑤ **立绘 = `fullHeroSprite`，不是卡面插图**：`RuleEngine` 侧没有 `rawCardData`，我们的等价物
//     = `CardArt.TryDeckHero(card.Id, out tex)` —— 那个函数**判据本身就是 `fullHeroSpriteReference`**
//     （`Core/CardArt.cs` 那段 doc 逐条写着：56 张里 36 张 SO 原文 + 20 张 GUID→容器）。
//     ⚠️ 取不到时 `TryDeckHero` **自己会 Warning**（⛔ 不静默），这里再退回 `CardArt.Portrait`（卡面插图，
//     **这一档是兜底，不是原版做法**）。
//  ⑥ **时长**：`VoiceLineData(chatMessage, sprite, audioClip, duration, cardType)`（`VoiceLineData.cs`）——
//     大聊天没有音频 ⇒ `duration = GetCurrentClipLength(null)` = **2.0 s**（我们 `UnitChatPanel.Duration(null)`
//     逐字同值：`if (clip == null) return 2.0f`）⇒ 委托给 `Speak` 就自动是 2.0，**不另抄一个常量**。
//
//  ⚠️ 与普通小气泡的关系：`DisplayChatBox`（单位说话）走 Player/Enemy 两颗（带卡面插图），
//     `DisplayBigChat` 走 Radio 那一颗（带**督军整身立绘**）⇒ **两条路是两条，别合并**。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>
    /// 原版 `BattleManager.DisplayBigChat(string, CardScript)` / `(string, Sprite)` 两个重载。
    /// <para>**只在 `UnitChatPanel` 已经有第三颗 `RadioChat` 时才有东西可显示**（原版那颗在场景里是
    /// `(inactive)`，运行时才亮）。</para>
    /// </summary>
    public static class BattleBigChat
    {
        /// <summary>原版 `VoiceLinesOnSceneType.Radio` = **2**（`UnitsVoiceLinesPanel__GetChatBox.c` 的 `+0x30`）。
        /// 为什么是这一颗（而不是 Player/Enemy）→ 文件头 ③(b)(c)。`🔶 自己设计（非原版）`：显示目标是我们
        /// 从三条侧面证据定的 · 原版显示调用被剥（`BattleManager__DisplayBigChat.c` 全文只剩 guard）·
        /// 调度台 2026-10-26 派活（`判据/B9_剩余C_派活包.md` §2·P-12）。</summary>
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

        /// <summary>自检：成功显示了几句。</summary>
        public static int Shown { get; private set; }
        /// <summary>自检：最后一次显示的文案。</summary>
        public static string LastText { get; private set; }
        /// <summary>自检：最后一次**真的换上了**传入的立绘（`false` = 退回了卡面插图 / 节点没找到）。</summary>
        public static bool LastPortraitApplied { get; private set; }
        /// <summary>自检：最后一次失败的原因（`"empty"` / `"noportrait"` / `"nopanel"` / `"speak"`（面板拒收）/ `null` = 没失败）。</summary>
        public static string LastError { get; private set; }

        /// <summary>清计数（自检每段之前调）。</summary>
        public static void ResetCounters()
        {
            Shown = 0; LastText = null; LastPortraitApplied = false; LastError = null;
            _warnedNoRadio = false;
        }
        static bool _warnedNoRadio;

        /// <summary>原版重载 A：`DisplayBigChat(string chatMsgText, CardScript talkingUnit)`。
        /// 取 `talkingUnit.rawCardData.fullHeroSprite` 后**转发给重载 B**（= 原版唯一实现）。
        /// <returns>true = 显示了（两道 guard 都没挡）。</returns></summary>
        public static bool Display(UnitChatPanel panel, string text, RuleEngine.CardDef talkingUnit,
                                   float seconds = 0f, string evt = DefaultEvt)
        {
            Texture2D hero = FullHeroSprite(talkingUnit);
            return Display(panel, text, hero,
                           talkingUnit != null ? talkingUnit.Id : null, seconds, evt);
        }

        /// <summary>原版重载 B：`DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)` —— **唯一实现**。
        /// <para>两道 guard 逐字照原版（⚠️ 空文案 ⇒ **warning 且不显示**；立绘 null ⇒ **error 且不显示**）。</para>
        /// <para>显示 = `UnitChatPanel.Speak(Radio, …, clip: null, expectVoice: false)` ——
        /// 即原版 `UnitsVoiceLinesPanel.ShowChatBox(type, VoiceLineData)`：气泡 + 打字机 + 2.0 s 寿命
        /// 全走那一处（⛔ 本文件不另建一套框/几何，见 `CLAUDE.md`「两处写同一条规则」）。</para>
        /// <para>🔶 自己设计（非原版）：立绘的**写入口**。`UnitChatPanel.Speak` 只收 `artKey`（→ 卡面插图），
        /// 而大聊天要的是 `fullHeroSprite`（方形督军立绘）⇒ 显示之后按节点名取那一颗 quad 换纹理。
        /// 为什么必须自己定：`UnitChatPanel` 的立绘口没有 texture 形参，而它在**别人的白名单**里，本轮不能改。
        /// 判据 = 原版 `VoiceLineData.sprite` 那一格就是 `fullHeroSprite`（`AiScripted.cs:2046-2057`）。
        /// 调度台 2026-10-26 派活。⛔ 节点名或比例取不到时**出声**并退回卡面插图，不静默。
        /// 📌 将来 `UnitChatPanel.Speak` 若加了 texture 口，**删掉这条桥**。</para>
        /// </summary>
        public static bool Display(UnitChatPanel panel, string text, Texture2D portrait,
                                   string artKey = null, float seconds = 0f, string evt = DefaultEvt)
        {
            LastText = text;
            LastPortraitApplied = false;
            if (string.IsNullOrEmpty(text))
            {
                LastError = "empty";
                Debug.LogWarning("[BattleBigChat] " + EmptyChatWarning);
                return false;
            }
            if (portrait == null)
            {
                LastError = "noportrait";
                Debug.LogError("[BattleBigChat] " + NoUnitError);
                return false;
            }
            if (panel == null)
            {
                LastError = "nopanel";
                Debug.LogError("[BattleBigChat] 没有 `UnitChatPanel` ⇒ 大聊天**没显示**（不静默）；"
                             + "它由 `BattleDriver.BuildHud` 建，非战斗 HUD 环境下调到这里就是装配缺口。");
                return false;
            }
            if (!panel.HasRadio && !_warnedNoRadio)
            {
                _warnedNoRadio = true;
                Debug.LogWarning("[BattleBigChat] 这一颗面板**没有第三颗 `RadioChat`**（`HasRadio == false`）"
                               + " ⇒ 大聊天会退回 `Speak` 的失败路径（原版那颗在场景里是 `(inactive)`，运行时才亮）。");
            }

            // 原版 `ShowChatBox(Radio, new VoiceLineData(text, sprite, audioClip: null, duration, cardType))`
            bool ok = panel.Speak(Side, artKey, evt, artKey, null, text, null,
                                  null, null, null, false, false);   // clip=null · expectVoice=false
            if (!ok)
            {
                LastError = "speak";
                Debug.LogWarning("[BattleBigChat] `UnitChatPanel.Speak(2, …)` 返回 false ⇒ 大聊天**没显示**（不静默）。");
                return false;
            }

            ApplyPortrait(panel, portrait);
            LastError = null;
            Shown++;
            return true;
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
    }
}
