// CampaignScriptRunner.cs — 原版 `AiScripted` 的**战役脚本那一半**（数据 + 执行器 + 等待时长）
//
// ═════════════════════════════════════════════════════════════════════════════
//  一句话：原版 `AiScripted` 里**教程**与**战役**是同一个类的两套重载，共用同一个实例、
//  但**各用各的指针**（教程 `actionCounter @+0x10` · 战役 `actionCampaignCounter @+0x14`）。
//  我们的教程那一半早就在 `RuleEngine/Core/TutorialScript.cs`（`TutorialScript`）；
//  **战役这一半（本文件）之前完全不存在** —— 账 `战斗逻辑驱动.tsv` 里那 5 行都写着「查不到（战役未做）」。
//
//  判据出处（🥇 = 反编译；⚠️ 这两份重载**同名**，`.c` 只落下了教程那份 —— 战役那份走 VA 反汇编）：
//   · 🥇 `d:/2/tools/decomp_full/AiScripted__GetCurrentWaitTime.c:14-43`（**全库只有这一个签名**，
//     `param_3` 就是 `List<ScriptedActionCampaignData>` ⇒ 落盘的这份**就是战役那份**）
//   · 🥇 `d:/2/tools/decomp_full/AiScripted__HasPendingActions.c:6-11`（**战役**重载，读 `+0x14`）
//   · 🥇 `d:/2/tools/decomp_full/AiScripted__PlayNextScriptedAction.c:11-21`（**战役**重载）
//   · 🥇 `d:/2/tools/decomp_full/AiScripted__PlayScriptedTurn.c:11-28`（**战役**重载，读 `+0x14`）
//   · 🥇 `d:/2/tools/decomp_full/AiScripted__ResetActionCounter.c`（一次写 8 字节）
//   · 🥇 `d:/2/tools/decomp_full/AiScripted__UpdateTurn.c`（只清 `+0x10`，**不清战役指针**）
//   · 🔴 `ExecuteAction(BattleManager, ScriptedActionCampaignData)` **在 `decomp_full` 里没有产物** ——
//     `AiScripted__ExecuteAction.c` 落盘的是**教程**那份（读 `+0x48 scriptedActionData` / 字符串 `+0x10`）。
//     战役那份的判据 = **VA 反汇编 `0x1809407F0`–`0x180940DAF`**
//     （`python -I -X utf8 工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x1809407F0 400`）
//     ＋ 🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AiScripted.cs:1915-2061`（**这份有真体**，两份逐句对得上）。
//     ⚠️ 同名重载互相覆盖是**已知坑**（账 `战斗逻辑驱动.tsv:112` 的备注就记着这一条）。
//   · 数据结构 🥇 `d:/2/tools/il2cpp_out/dump.cs:89410-89437`（`ScriptedActionCampaignData`）
//     · `SmallTipData` `dump.cs:24731-24766` · `TutorialTipParams` `dump.cs:24712-24729`
//     · `SoundAsset_OLD`（`clip +0x10` / `volume +0x18` / `GetDuration()`）。
//
//  🔴 **数据源本地不存在（如实记，⛔ 别自己编一套战役脚本格式）**：
//     `ScriptedActionCampaignData` 的四张表挂在 `WarlordPathData`（`dump.cs:89285-89289/89300+`：
//     `preMulliganScriptedActions +0x38` / `postMulliganScriptedActions +0x40` / `victoryScriptedActions +0x48`），
//     而 `WarlordPathData` 只在 **`TimedCampaignObject : ScriptableObject`**（`dump.cs:2290`）里 ——
//     **那是服务端 LiveOps 下发的资产**。现读取证：
//       · `d:/2/新解包资源/assets_full` 全树 `WarlordPath1|preMulliganScriptedActions` 只命中
//         **教程**那 7 份 `bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage*.json`
//         （教程 SO 的 `preMulliganScriptedActions` 是 `TurnScriptedData`，**不是**战役的四张表）；
//       · `TimedCampaign` 只有 1 个命中 = `globalgamemanagers/MonoScript/MonoScript_4429.json`（**类注册**，不是实例）；
//       · `d:/2/关服前存档`（835 MB，含远端 Addressables 缓存）按 `preMulliganScriptedActions` /
//         `radioSpeakerCard` / `TimedCampaign` 三个词二进制搜 ⇒ **0 命中**。
//     ⇒ 本文件**只落「执行器 + 数据结构」**（结构与原版逐字段对齐，不是我们发明的），
//       **不造任何战役脚本文本**；`NullCampaignScriptHost` 是「宿主还没有」时的出声兜底。
//
//  ⚠️ 与教程那份的**分工**（⛔ 别混）：
//     · 教程：`TutorialScript.PlayScriptedTurn(ctx)` + `GetDelay(manager, TurnScriptedData, delayBefore)`（`+0x20/+0x24`）；
//     · 战役：**本文件** + `GetCurrentWaitTime(...)`（`+0x28 waitTime` ＋ 音效时长）。
//     `+0x28` 在两张表上是**两个不同的字段**（战役 = `waitTime`，教程 = `ScriptedAction.textReference`）——
//     判据正本见 `RuleEngine/Core/TutorialScript.cs` 的 `PlayScriptedTurn` 注释里 `A899` 那一节（⛔ 这里不抄第二份）。
//
//  ⚠️ 与 `TutorialScript` **不共享落点**：原版是「同一个 `AiScripted` 实例上的两个方法体重载」，
//     我们的教程计数器在 `RuleEngine/Core/TutorialScript.cs`（那文件有别的写手）；本文件只持战役那一个指针。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;   // ScriptedActionType / CardDef

namespace CardPresentation
{
    // ==================================================================
    //  一、数据结构（逐字段照 `dump.cs`，⛔ 别加/改语义）
    // ==================================================================

    /// <summary>原版 `TutorialTipParams`（`d:/2/tools/il2cpp_out/dump.cs:24712-24729`）。
    /// ⚠️ 名字里的 "Tutorial" 是原版的类名 —— **战役的 `ScriptedActionCampaignData.smallTipParams` 用的就是它**
    /// （`dump.cs:89418`），所以本类**不是**教程专用的。</summary>
    [Serializable]
    public class CampaignTipParams
    {
        public int positionReference;   // PositionReference（`dump.cs:24675-24698`）
        public int positionRelation;    // PositionRelation（`dump.cs:24701-24709`）
        public bool showLeftArrow;
        public bool showRightArrow;
        public float tipDuration;
        public bool waitForTip;
        public bool tipWithContinue;
        public bool preventSkipTip;
    }

    /// <summary>原版 `SmallTipData`（`d:/2/tools/il2cpp_out/dump.cs:24731-24766`）。
    /// 构造语义逐句照 `decomp_full/SmallTipData__.ctor.c`：
    /// `tipDuration` 先置 **6.0f（`0x40c00000`）**、随后被 `tipParams.tipDuration` 覆盖；
    /// `tipParams == null` ⇒ 原版**抛 NRE**（这里同样抛，⛔ 不静默兜底）。</summary>
    public class CampaignSmallTipData
    {
        public string tipText;              // 0x10
        public Vector2 tipPosition;         // 0x18
        public bool showLeftArrow;          // 0x20
        public bool showRightArrow;         // 0x21
        public float tipDuration;           // 0x24
        public bool waitForTip;             // 0x28
        public bool preventSkipTip;         // 0x29
        public bool tipWithContinue;        // 0x2A
        public string tipTextReference;     // 0x30

        public CampaignSmallTipData(string tipText, CampaignTipParams tipParams,
                                    Vector2 tipPosition, string textReference = "")
        {
            tipDuration = 6.0f;                          // `SmallTipData__.ctor.c:7`（`0x40c00000`）
            this.tipText = tipText;
            if (tipParams == null)
                throw new ArgumentNullException(nameof(tipParams),
                    "原版 `SmallTipData..ctor` 在 `tipParams == null` 时抛 NRE（decomp_full/SmallTipData__.ctor.c:23-24）");
            showLeftArrow = tipParams.showLeftArrow;      // src +0x18 → 0x20
            showRightArrow = tipParams.showRightArrow;    // src +0x19 → 0x21
            tipDuration = tipParams.tipDuration;          // src +0x1C → 0x24（覆盖上面那个 6.0f）
            this.tipPosition = tipPosition;               // `SmallTipData__.ctor.c:14`
            waitForTip = tipParams.waitForTip;            // src +0x20 → 0x28
            preventSkipTip = tipParams.preventSkipTip;    // src +0x22 → 0x29
            tipWithContinue = tipParams.tipWithContinue;  // src +0x21 → 0x2A
            this.tipTextReference = textReference;        // → 0x30
        }
    }

    /// <summary>原版 `SoundAsset_OLD`（`dump.cs:3220`）。
    /// `GetDuration()` 逐句照 `decomp_full/SoundAsset_OLD__GetDuration.c`：**`clip == null ⇒ 0`**，否则 `clip.length`。</summary>
    public struct CampaignSoundAsset
    {
        public AudioClip clip;      // 0x10
        public float volume;        // 0x18

        public float GetDuration()
        {
            return clip != null ? clip.length : 0f;   // `SoundAsset_OLD__GetDuration.c:19-26`
        }
    }

    /// <summary>原版 `ScriptedActionCampaignData`（`d:/2/tools/il2cpp_out/dump.cs:89410-89437`）。
    /// 字段名/类型/顺序**与 SO 序列化一致**（⚠️ 偏移写在注释里，方便对着 `dump.cs` 复核）。</summary>
    [Serializable]
    public class CampaignScriptAction
    {
        public int stageAction;                         // 0x10
        public string soundAssetName;                   // 0x18
        public string textReference;                    // 0x20
        public float waitTime;                          // 0x28
        public ScriptedActionType actionType;           // 0x2C
        public CampaignTipParams smallTipParams;        // 0x30
        public string radioSpeakerCard;                 // 0x38

        // 原版 `ScriptedActionCampaignData.GetLocalizedText()`（`decomp_full/ScriptedActionCampaignData__GetLocalizedText.c`）：
        //   `LocalizationManager.GetTranslation(前缀 + textReference)`
        //   —— 前缀字面量**实读** = **`"Event_Description/"`**（`tool`：`il2cpp_out/stringliteral.json` 里
        //   `address = 0x42A9FE0` 那条；同一个槽被 `0x180797A98` 的 `mov rcx,[rip+…]` 直接取）。
        //   现核：官方全表 `Resources/official_loc_13lang.json` 里 `Event_Description/` 前缀共 **145** 条 ⇒ 这条路**接得上**。
        /// <summary>
        /// 取这条动作的**本地化文案**（原版 `GetLocalizedText`，键 = `"Event_Description/" + textReference`）。
        ///
        /// 🔶 自己设计（非原版）：**缺键时返回空串**（原版 `GetTranslation` 缺键返回什么，本机不可验 ——
        /// 原版那张表是**远端** I2 词条表，缺键行为拿不到判据）。
        /// 为什么必须自己定：另一条可选路是「回键名」，那会把 `Event_Description/…` **直接印到屏幕上**
        /// （`Loc.T` 缺键就返键名，见 `Core/Loc.cs` 的那条文档；项目红线不许把键名印出来）。
        /// 出声由 `Loc.T` 自己做（缺键 `LogWarning` 且按 `MissingCount` 记账）。
        /// 调度台 2026-10-27 裁定。
        /// </summary>
        public string GetLocalizedText()
        {
            if (string.IsNullOrEmpty(textReference)) return "";
            string key = "Event_Description/" + textReference;
            string v = Loc.T(key);
            // `Loc.T` 缺键 = 返回键名本身（`Core/Loc.cs:135` 那条文档）⇒ 认出这一档，别把它当译文。
            if (v == key) return "";
            return v;
        }

        /// <summary>
        /// 原版 `GetSoundAsset(AssetBundleManager)`（`decomp_full/ScriptedActionCampaignData__GetSoundAsset.c`）：
        /// · `soundAssetName` 空 ⇒ **返回 null**（`:14-15` 的 `IsNullOrEmpty(+0x18)`）；
        /// · 否则新建 `SoundAsset_OLD`：`clip = GetBundledSound(soundAssetName)`、
        ///   **`volume = 0x3f800000` = 恒 1.0**（`:28` —— SO 里那一格运行时不采信）。
        /// `GetBundledSound`（`decomp_full/ScriptedActionCampaignData__GetBundledSound.c`）的语义 =
        /// **逐个已加载的 AssetBundle 试 `LoadAsset&lt;AudioClip&gt;(name)`，第一个非 null 的胜**；
        /// 全都没有 ⇒ `clip = null`（**对象照样建出来**，`GetDuration()` 于是 0）。
        /// </summary>
        public CampaignSoundAsset? GetSoundAsset(Func<string, AudioClip> resolveBundledSound)
        {
            if (string.IsNullOrEmpty(soundAssetName)) return null;
            AudioClip clip = resolveBundledSound != null ? resolveBundledSound(soundAssetName) : null;
            return new CampaignSoundAsset { clip = clip, volume = 1.0f };
        }
    }

    // ==================================================================
    //  二、宿主（原版这些调用都落在 `BattleManager` / `VoiceLinesController` /
    //      `ChatManager` / `PlayerDataManager` 上 —— 这里收敛成一个接口，
    //      ⛔ 语义逐条照原版方法名，别在这里加原版没有的行为）
    // ==================================================================

    /// <summary>战役脚本执行时要调的那几处外部世界（原版分散在 5 个单例/管理器上）。
    /// <para>每条都注明了**原版是哪一个方法**；实现方（将来的接线）只管把我们的资产/卡表按同一语义接上。</para></summary>
    public interface ICampaignScriptHost
    {
        /// <summary>原版 `CustomDebug.Log`（`ExecuteAction` 开头那一条）。</summary>
        void Log(string message);

        /// <summary>出声口（原版没有专门的 warning 口；我们的红线是「不许静默失败」）。
        /// 🔶 自己设计（非原版）：原版这几处拿不到数据时**直接 NRE**；我们改成出声 + 跳过。
        /// 为什么必须自己定：NRE 会把整条脚本链打断，而「取不到」在离线复刻里是**常态**。
        /// 调度台 2026-10-27 裁定。</summary>
        void LogWarning(string message);

        /// <summary>原版 `ScriptedActionCampaignData.GetBundledSound` 里那一步
        /// `AssetBundle.LoadAsset&lt;AudioClip&gt;(name)`（**逐个已加载包试**）。取不到 ⇒ `null`。</summary>
        AudioClip ResolveBundledSound(string soundName);

        /// <summary>原版 `BattleManager.GetHero(bool playerHeroY)`（`dump.cs:32640`）。
        /// `true` = **玩家**督军，`false` = **敌方**督军。</summary>
        CardDef GetHero(bool playerHeroY);

        /// <summary>原版 `PlayerDataManager.singletonManager.allCardCollection.Find(x =&gt; x.uniqueId == id)`
        /// （`ExecuteAction` 两处 lambda，`decomp_full/AiScripted.__c__DisplayClass33_0___ExecuteAction_b__0.c`）。
        /// 找不到 ⇒ `null`（**原版随后就 NRE**，见 `LogWarning` 那条）。</summary>
        CardDef FindCardByUniqueId(string uniqueId);

        /// <summary>原版 `VoiceLinesController.DisplayChatBox(string chatMsgText, CardScript talkingUnit, AudioClip audioClip)`
        /// （`dump.cs:42921`）。</summary>
        void DisplayChatBox(string text, CardDef talkingUnit, AudioClip clip);

        /// <summary>原版 `BattleManager.DisplayBigChat(string chatMsgText, CardScript talkingUnit)`
        /// （`dump.cs:31704`，重载 A）。</summary>
        void DisplayBigChat(string text, CardDef talkingUnit);

        /// <summary>原版 `BattleManager.DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)`
        /// （`dump.cs:31707`，重载 B —— **唯一实现**）。
        /// ⚠️ 我们的美术管到 `Texture2D`（`BattleBigChat.Display` 的第二个重载也是 `Texture2D`）⇒ 这里用 `Texture2D`。</summary>
        void DisplayBigChat(string text, Texture2D talkingUnitSprite);

        /// <summary>原版 `VoiceLinesController.ShowRadioMessage(SmallTipData tipData, Sprite speakerSprite,
        /// AudioClip audioClip, CardTypeOptions cardType = 0)`（`dump.cs:42924`）。
        /// `cardType` 原版取自 **`RawCardScript.cardType @+0x94`**（`dump.cs` 里 `CardTypeOptions cardType; // 0x94`）。</summary>
        void ShowRadioMessage(CampaignSmallTipData tipData, Texture2D speakerSprite,
                              AudioClip audioClip, int cardType);

        /// <summary>原版 `VoiceLinesController.DisplayWarlordChatMessage(ChatMessage chatMsgIndex, bool isPlayer,
        /// bool ignoreUiState = false)`（`dump.cs:42918`）。`chatMsgIndex` 用整数（原版 `ChatMessage.Intro = 0`，
        /// ⚠️ 我们工程里**没有** `ChatMessage` 枚举，见 `Battle/ChatPopupPanel.cs:1396`）。</summary>
        void DisplayWarlordChatMessage(int chatMsgIndex, bool isPlayer, bool ignoreUiState);

        /// <summary>原版 `BattleManager.LandWarlordsInCampaign()`（`dump.cs:31432`）。</summary>
        void LandWarlordsInCampaign();

        /// <summary>原版 `BattleManager.PlaySoundAsset(SoundAsset_OLD)`（`dump.cs:32562`）—— `ExecuteAction` 的末尾一跳。</summary>
        void PlaySoundAsset(CampaignSoundAsset soundAsset);

        /// <summary>原版 `ChatManager.GetChatMessage(ChatMessage.Intro, isPlayer)`（`AiChatBig` 文案为空时的兜底）。</summary>
        string GetIntroChatMessage(bool isPlayer);

        /// <summary>原版 `MousePositionDebug.GetMouseClickPosition(...)`（`RadioMessage` 那条的 `tipPosition`）。
        /// ⚠️ 这个名字是 `工具/resolve_va.py` 对 `0x1804F0EC0` 的解 —— 它是 IL2CPP 里被 ICF 折叠过的共享体，
        /// **名字只能当旁证**；语义由调用点定（进 `SmallTipData.tipPosition`，是个 `Vector2`）。</summary>
        Vector2 GetMouseClickPosition();

        /// <summary>原版 `RawCardScript.fullHeroSpriteReference.Load()`（`PlayerChatBig` 那条的立绘）。
        /// 我们这一档的等价物 = `Core/CardArt.TryDeckHero(card.Id, out tex)`（它的判据本身就是
        /// `fullHeroSpriteReference`，见 `Battle/BattleBigChat.cs` 的 ⑤）。取不到 ⇒ `null`。
        /// <para>⚠️ **原版那两级跳被折叠进这一条**：`rawCard.referenceCard?.fullHeroSpriteReference`
        /// **优先**，否则 `rawCard.fullHeroSpriteReference`（`AiScripted.cs:2020-2036`）。
        /// 我们的 `CardDef` 没有 `referenceCard` 这一格，而 `CardArt.TryDeckHero` 自己已经按
        /// 「SO 原文 36 + GUID→容器 20」把那一族取全了（见它自己的 doc）⇒ 折叠，如实记在这里。</para></summary>
        Texture2D FullHeroSprite(CardDef card);

        /// <summary>原版 `RawCardScript.cardSprite`（RVA `0x94C550`，`ExecuteAction` 的 `RadioMessage` 那一支
        /// 在反汇编 `0x180940B0C` 调它拿 `<b>卡面插图</b>`）。⚠️ **与 <see cref="FullHeroSprite"/> 不是一回事** ——
        /// 电台那一条要的是卡面插图，`PlayerChatBig` 要的才是督军整身立绘（原版两处取的是两个不同属性）。
        /// 我们这一档 = `Core/CardArt.Portrait(card.Id)`。</summary>
        Texture2D CardSprite(CardDef card);

        /// <summary>原版 `RawCardScript.cardType @+0x94`（`CardTypeOptions`）。</summary>
        int CardTypeOf(CardDef card);
    }

    /// <summary>
    /// 默认宿主 = **出声但不动作**（战役局目前**开不起来**、也**没有数据**，见文件头那一段）。
    ///
    /// 🔶 自己设计（非原版）：原版没有这个类 —— 每一跳都落在真实 `BattleManager` 上。
    /// 为什么必须有它：本波要求把执行器落下来，但 ① 战役局入口查不到（`MatchType.Campaign` 的构造点
    /// 全量反编译 0 命中，见 `资料/完全复刻/判据/战役开局_判据与落点.md` 开头那段）② 战役脚本数据本地不存在。
    /// ⇒ 没有它，`ExecuteAction` 就**没有可调的下一跳**，只能写成空实现（= 静默失败，撞红线）。
    /// 它把每一跳都打成一条日志，接线时按日志逐条对。
    /// 调度台 2026-10-27 裁定。
    /// </summary>
    public sealed class NullCampaignScriptHost : ICampaignScriptHost
    {
        readonly List<string> _warned = new List<string>();

        public void Log(string message) { Debug.Log("[Campaign] " + message); }

        public void LogWarning(string message)
        {
            string k = message ?? "";
            if (!_warned.Contains(k)) { _warned.Add(k); Debug.LogWarning("[Campaign] " + k); }
        }

        public AudioClip ResolveBundledSound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return null;
            // 我们本地唯一一条音频路（教程那一族已经在用同一个根，见 `BattleDriver.PlayTutorialSound`）。
            var clip = Resources.Load<AudioClip>(VoiceLines.ClipRoot + soundName);
            if (clip == null)
                LogWarning($"音效 `{soundName}` 取不到（找 `Resources/{VoiceLines.ClipRoot}{soundName}`）"
                         + " ⇒ 这一条**没声**（不算 0 秒，见 `GetSoundAsset` 的语义）");
            return clip;
        }

        public CardDef GetHero(bool playerHeroY)
        {
            LogWarning($"`GetHero({playerHeroY})` 没有接线（战役局还没开）⇒ 这一跳跳过");
            return null;
        }

        public CardDef FindCardByUniqueId(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;
            LogWarning($"`allCardCollection.Find(uniqueId == \"{uniqueId}\")` 没有接线 ⇒ 这一跳跳过");
            return null;
        }

        public void DisplayChatBox(string text, CardDef talkingUnit, AudioClip clip)
        {
            Log($"DisplayChatBox「{text}」unit={(talkingUnit != null ? talkingUnit.Id : "<null>")} clip={(clip != null ? clip.name : "<null>")}");
        }

        public void DisplayBigChat(string text, CardDef talkingUnit)
        {
            Log($"DisplayBigChat(hero)「{text}」unit={(talkingUnit != null ? talkingUnit.Id : "<null>")}");
        }

        public void DisplayBigChat(string text, Texture2D talkingUnitSprite)
        {
            Log($"DisplayBigChat(sprite)「{text}」sprite={(talkingUnitSprite != null ? talkingUnitSprite.name : "<null>")}");
        }

        public void ShowRadioMessage(CampaignSmallTipData tipData, Texture2D speakerSprite,
                                     AudioClip audioClip, int cardType)
        {
            // 🆕 2026-10-27（批次 9 · `E7ENG`）：**这一格（`cardType` 的「分支」）还没接线** ——
            //   原版 `VoiceLinesController.ShowRadioMessage` 会按 `CardTypeOptions` 分档（`CanUseVOs` 一族），
            //   我们这里目前**只把值打进日志**（值本身现在是对的：见下面 `CardTypeOf`）。
            //   ⛔ 本批**只补值、不发明分支** —— 接线时「值 + 分支」两件要一起做（判据先从
            //   `VoiceLinesController__ShowRadioMessage` 的反编译体取，别按 `CanUseVOs` 猜）。
            Log($"ShowRadioMessage「{(tipData != null ? tipData.tipText : "<null>")}」"
              + $"sprite={(speakerSprite != null ? speakerSprite.name : "<null>")} cardType={cardType}");
        }

        public void DisplayWarlordChatMessage(int chatMsgIndex, bool isPlayer, bool ignoreUiState)
        {
            Log($"DisplayWarlordChatMessage(idx={chatMsgIndex}, isPlayer={isPlayer}, ignoreUiState={ignoreUiState})");
        }

        public void LandWarlordsInCampaign() { Log("LandWarlordsInCampaign()"); }

        public void PlaySoundAsset(CampaignSoundAsset soundAsset)
        {
            Log($"PlaySoundAsset clip={(soundAsset.clip != null ? soundAsset.clip.name : "<null>")} vol={soundAsset.volume}");
        }

        public string GetIntroChatMessage(bool isPlayer)
        {
            LogWarning($"`ChatManager.GetChatMessage(Intro, {isPlayer})` 没有接线 ⇒ 文案用空串");
            return "";
        }

        public Vector2 GetMouseClickPosition()
        {
            LogWarning("`MousePositionDebug.GetMouseClickPosition` 没有接线 ⇒ tipPosition = (0,0)");
            return Vector2.zero;
        }

        public Texture2D FullHeroSprite(CardDef card)
        {
            if (card == null) return null;
            Texture2D tex;
            if (CardArt.TryDeckHero(card.Id, out tex)) return tex;
            LogWarning($"`fullHeroSprite`（`CardArt.TryDeckHero`）取不到 `{card.Id}`");
            return null;
        }

        public Texture2D CardSprite(CardDef card)
        {
            if (card == null) return null;
            var tex = CardArt.Portrait(card.Id);
            if (tex == null) LogWarning($"`cardSprite`（`CardArt.Portrait`）取不到 `{card.Id}`");
            return tex;
        }

        public int CardTypeOf(CardDef card)
        {
            // 🆕 2026-10-27（批次 9 · `E7ENG` · 普查 `判据/cardType判据点普查.md` §3·1）：**这一格接线了**。
            //   判据 = 原版 `RawCardScript.cardType @+0x94`（`CardTypeOptions`：`Minion 0 / Hero 10 /
            //   Tactic 20 / Whispers 40`）—— 卡表 v34 起有真值列（`cards_engine.json` 的 `cardType`），
            //   由 `CardDatabase` 构造后挂到 `CardDef.CardType`（`-1` = 未知）。
            //   ⚠️ **`-1` 时出声并退回 0**（= 改前行为；⛔ 别静默把它当成「这张真的是 Minion」）。
            if (card == null)
            {
                LogWarning("`CardTypeOf(null)` ⇒ 传 0（没有卡可判）");
                return 0;
            }
            if (card.CardType >= 0) return card.CardType;
            LogWarning($"`RawCardScript.cardType` **判不了**（`{card.Id}`：卡表 < v34 没有 `cardType` 列，"
                     + "或这张卡不在真值快照里）⇒ 传 0（⛔ 不是「它真的是 Minion」）");
            return 0;
        }
    }

    // ==================================================================
    //  三、执行器（原版 `AiScripted` 的战役那一半）
    // ==================================================================

    /// <summary>
    /// 原版 `AiScripted` 的**战役脚本**执行器：一个指针 + 四个驱动方法 + 一个动作执行。
    /// <para>⚠️ **不是** `TutorialScript` 的一部分 —— 原版两者是同一个类的不同重载，
    /// 但**各用各的指针**（战役 `actionCampaignCounter @+0x14`）；我们的教程那一半在
    /// `RuleEngine/Core/TutorialScript.cs`。⛔ 别把两边的指针混成一个。</para>
    /// </summary>
    public class CampaignScriptRunner
    {
        /// <summary>原版 `AiScripted.actionCampaignCounter`（**`+0x14`**，`dump.cs:24553`）。
        /// 🔴 与教程那个 `actionCounter`（**`+0x10`**）是**两个不同的字段** —— 同名重载靠它区分，
        /// 把 `+0x14` 写成 `+0x10` 是这一族最常踩的坑（账 `战斗逻辑驱动.tsv:112` 的备注）。
        /// ⚠️ 它**不被 `UpdateTurn` 清零**（`decomp_full/AiScripted__UpdateTurn.c`：只写 `+0x10`）。</summary>
        public int ActionCampaignCounter { get; set; }

        /// <summary>
        /// 原版 `AiScripted.PlayScriptedTurn(BattleManager, List&lt;ScriptedActionCampaignData&gt;)`
        /// —— 判据 🥇 `decomp_full/AiScripted__PlayScriptedTurn.c:11-28`（**战役**那份）、
        /// 🥈 `out_cpp2il_dev_ilspy/AiScripted.cs:262-289`。
        /// <para>逐句：`turnData` 非空 → **指针 ≥ Count 就返 false**（`:12`/`:28`）→ 取 `list[指针]`
        /// → `ExecuteAction(manager, 元素)` → **指针 ++**（`:20`）→ 返回 `指针 &lt; Count`（`:25-26`）。</para>
        /// <para>⚠️ **没有「玩家自己做」那一档** —— 那是**教程**重载才有的闸
        /// （反汇编 `0x180944125` 的 `playerAction` 判据）；战役数据里没有 `playerAction` 这个字段
        /// （`dump.cs:89410-89419` 逐字段核过）⇒ ⛔ 别把教程那条闸搬过来。</para>
        /// <para>⚠️ `turnData == null` 时原版**抛 NRE**（`:11` + `:30-31` 的 `FUN_1803f47a0`）；
        /// 我们返 `false`（「没有待办」）—— 调用点在协程里恒有列表，此差属域外，如实记在这里。</para>
        /// </summary>
        public bool PlayScriptedTurn(ICampaignScriptHost host, List<CampaignScriptAction> turnData)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (turnData == null) return false;                       // 原版 NRE（域外，见 doc）
            if (ActionCampaignCounter >= turnData.Count) return false; // `PlayScriptedTurn.c:12`
            var action = turnData[ActionCampaignCounter];              // `:17-18`
            ExecuteAction(host, action);                               // `:19`
            ActionCampaignCounter++;                                   // `:20`
            return ActionCampaignCounter < turnData.Count;             // `:25-26`
        }

        /// <summary>
        /// 原版 `AiScripted.PlayNextScriptedAction(BattleManager, List&lt;ScriptedActionCampaignData&gt;)`
        /// —— 判据 🥇 `decomp_full/AiScripted__PlayNextScriptedAction.c:11-21`、
        /// 🥈 `out_cpp2il_dev_ilspy/AiScripted.cs:443-464`。
        /// <para>🔴 **与 `PlayScriptedTurn` 的唯一差别 = 它没有「到头」那道前闸**（`:11-13` 直接取 `list[指针]`）：
        /// 指针越界 ⇒ 原版是 `List.get_Item` 抛 `ArgumentOutOfRangeException`。
        /// 我们照抄（抛出），⛔ 别偷偷加一条 `return false` —— 那会让「调用点以为还有动作」这个
        /// 真错误静默消失。</para>
        /// </summary>
        public bool PlayNextScriptedAction(ICampaignScriptHost host, List<CampaignScriptAction> turnData)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (turnData == null) throw new ArgumentNullException(nameof(turnData),
                "原版 `PlayNextScriptedAction` 对 null 列表抛 NRE（decomp_full/AiScripted__PlayNextScriptedAction.c:24-25）");
            if (ActionCampaignCounter < 0 || ActionCampaignCounter >= turnData.Count)
                throw new ArgumentOutOfRangeException(nameof(turnData),
                    $"`PlayNextScriptedAction` 没有前闸：指针 {ActionCampaignCounter} 越界（Count={turnData.Count}）"
                    + " —— 原版在这里就是 `List.get_Item` 抛 ArgumentOutOfRange（`PlayNextScriptedAction.c:12-13`）");
            var action = turnData[ActionCampaignCounter];
            ExecuteAction(host, action);
            ActionCampaignCounter++;
            return ActionCampaignCounter < turnData.Count;
        }

        /// <summary>
        /// 原版 `AiScripted.HasPendingActions(List&lt;ScriptedActionCampaignData&gt;)` ——
        /// 判据 🥇 `decomp_full/AiScripted__HasPendingActions.c:6-11`（**战役**那份，读 `+0x14`）、
        /// 🥈 `out_cpp2il_dev_ilspy/AiScripted.cs:491-503`。
        /// <para>逐句只有一句：`actionCampaignCounter &lt; turnData.Count`（`:10-11`）。
        /// ⚠️ **不需要「列表非空」那一层**（`Count = 0` 时 `指针 0 &lt; 0` = false，天然对）。</para>
        /// <para>⚠️ `turnData == null` 时原版抛 NRE —— 我们返 `false`（同 `PlayScriptedTurn` 那条口径）。</para>
        /// </summary>
        public bool HasPendingActions(List<CampaignScriptAction> turnData)
        {
            if (turnData == null) return false;                     // 原版 NRE（域外）
            return ActionCampaignCounter < turnData.Count;          // `HasPendingActions.c:10-11`
        }

        /// <summary>
        /// 原版 `AiScripted.GetCurrentWaitTime(BattleManager, List&lt;ScriptedActionCampaignData&gt;)` ——
        /// 判据 🥇 `decomp_full/AiScripted__GetCurrentWaitTime.c:14-43`（**全库只有这一个签名** ⇒ 落盘这份就是战役那份）、
        /// 🥈 `out_cpp2il_dev_ilspy/AiScripted.cs:3556-3572`。
        /// <para>逐句：`指针 &lt; Count` 且元素非空 ⇒ `GetSoundAsset(...).GetDuration()`（**取不到声音按 0**，`:30`）
        /// **＋ 元素 `waitTime @+0x28`**（`:37`）；其余一律 **0**（`:43`）。</para>
        /// <para>消费点：`BattleManager._ExecuteCampaignScriptedTurn_d__598__MoveNext.c:73` → `WaitForSecondsRealtime`。</para>
        /// <para>⚠️ 形参 `host` 只在原版用来取 `PlayerDataManager.singletonManager.bundlesManager`
        /// （= 我们这里 `ResolveBundledSound` 那一跳）；原版**不读** `battleManager` 的任何字段
        /// （ILSpy `:3556-3572` 全文没有它）⇒ 我们保留这个形参只为「签名与原版同形」。</para>
        /// </summary>
        public float GetCurrentWaitTime(ICampaignScriptHost host, List<CampaignScriptAction> turnScriptedData)
        {
            if (turnScriptedData == null) return 0f;                 // 原版 NRE（域外）
            if (ActionCampaignCounter >= turnScriptedData.Count) return 0f;   // `:15`/`:43`
            var action = turnScriptedData[ActionCampaignCounter];    // `:16-17`
            if (action == null) return 0f;                           // `:18`/`:43`
            var sound = action.GetSoundAsset(host != null ? (Func<string, AudioClip>)host.ResolveBundledSound : null);
            float dur = sound.HasValue ? sound.Value.GetDuration() : 0f;       // `:28-36`（null ⇒ 0）
            return dur + action.waitTime;                                     // `:37`
        }

        /// <summary>
        /// 原版 `AiScripted.ExecuteAction(BattleManager, ScriptedActionCampaignData)` ——
        /// 🔴 **`decomp_full` 里没有这一份的产物**（同名重载被教程那份覆盖）。判据 =
        /// 反汇编 **VA `0x1809407F0`–`0x180940DAF`** ＋ 🥈 `out_cpp2il_dev_ilspy/AiScripted.cs:1915-2061`。
        ///
        /// <para>**六支**（`switch (action.actionType)`，`:1905`/`:1915`；其余档位**什么都不做**）：</para>
        /// <code>
        /// AiChatBig(65):   msg = IsNullOrEmpty(text) ? ChatManager.GetChatMessage(Intro, false) : text
        ///                  manager.DisplayBigChat(msg, manager.GetHero(false))          // 重载 A
        /// RadioMessage(90): tip = new SmallTipData(text, action.smallTipParams, 鼠标点击位置, null)
        ///                  card = allCardCollection.Find(x => x.uniqueId == action.radioSpeakerCard)
        ///                  voiceLines.ShowRadioMessage(tip, card.cardSprite, sound.clip, card.cardType)
        /// LandWarlords(160): manager.LandWarlordsInCampaign()
        /// PlayerChat(50):  voiceLines.DisplayChatBox(text, manager.GetHero(true), sound.clip)
        /// PlayerChatBig(55): raw = manager.GetHero(true).rawCardData
        ///                  if (action.radioSpeakerCard 非空) raw = allCardCollection.Find(同名)
        ///                  sprite = raw.referenceCard?.fullHeroSpriteReference
        ///                           ?? (raw.fullHeroSpriteReference.RuntimeKeyIsValid() ? 它 : null)
        ///                  manager.DisplayBigChat(text, sprite)                          // 重载 B
        /// AiChat(60):      if (text 非空) voiceLines.DisplayChatBox(text, GetHero(false), sound.clip)
        ///                  else voiceLines.DisplayWarlordChatMessage(Intro, isPlayer:false, ignoreUiState:true)
        /// </code>
        /// <para>**末尾（六支共用）**：`if (soundAsset != null) manager.PlaySoundAsset(soundAsset)`
        /// （反汇编 `0x1809409FF`…`0x180940A0D`：`test esi,esi / je skip / call 0x180971890`）。</para>
        /// <para>**开头**（反汇编 `0x1809408C8`–`0x180940907`）：先 `Log("Execute Campaign Scripted AI Action " + actionType)`
        /// （`String.Concat` + `Enum.ToString`）→ `soundAsset = action.GetSoundAsset(...)` → `text = action.GetLocalizedText()`
        /// —— **音效在 switch 之前就算好**（所以「没声」不影响文案）。</para>
        ///
        /// <para>🔶 自己设计（非原版）：**原版那两处「取不到就 NRE」改成出声 + 跳过这一支**。
        /// 具体两条：① `RadioMessage` 里 `Find` 返回 null（原版 `.c` 直接取 `rawCardScript.cardSprite` ⇒ NRE）；
        /// ② `PlayerChatBig` 里立绘取不到（原版照样调 `DisplayBigChat`，框自己 guard）。
        /// 为什么：离线复刻里「卡表/立绘缺一张」是常态，NRE 会把整条脚本回合打断；
        /// 我们的红线是「不许静默失败」⇒ 出声 + 跳过 = 可观测且不炸。调度台 2026-10-27 裁定。</para>
        /// </summary>
        public void ExecuteAction(ICampaignScriptHost host, CampaignScriptAction action)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (action == null)
                throw new ArgumentNullException(nameof(action),
                    "原版 `ExecuteAction` 起手 `test r8,r8 / je throw`（反汇编 `0x1809408AB`）—— action 为 null 直接 NRE");

            host.Log("Execute Campaign Scripted AI Action " + action.actionType);   // `0x1809408C8-0x180940907`
            var sound = action.GetSoundAsset(host.ResolveBundledSound);             // `0x180940935-0x18094097A`
            string text = action.GetLocalizedText();                                // `0x18094097F-0x180940996`

            switch (action.actionType)                                              // `0x1809409A6` 起
            {
                case ScriptedActionType.AiChatBig:                                  // 65
                {
                    string msg = string.IsNullOrEmpty(text) ? host.GetIntroChatMessage(false) : text;
                    host.DisplayBigChat(msg, host.GetHero(false));                  // 重载 A（`0x1809604B0`）
                    break;
                }

                case ScriptedActionType.RadioMessage:                               // 90
                {
                    var tip = new CampaignSmallTipData(text, action.smallTipParams,
                                                       host.GetMouseClickPosition(), null);
                    var card = host.FindCardByUniqueId(action.radioSpeakerCard);
                    if (card == null)
                    {
                        host.LogWarning($"[RadioMessage] `allCardCollection` 里找不到 `uniqueId == \"{action.radioSpeakerCard}\"`"
                                      + " ⇒ 这一条**不显示**（原版在这里 NRE）");
                        break;
                    }
                    // 原版：`voiceLines.ShowRadioMessage(tip, rawCard.cardSprite, sound.clip, (CardTypeOptions)rawCard.cardType)`
                    //   —— 立绘取的是 **`cardSprite`（卡面插图）**，反汇编 `0x180940B0C` 调 `RawCardScript.get_cardSprite`；
                    //   ⛔ 不是 `fullHeroSprite`（那是 `PlayerChatBig` 才用的，`0x180940D00` 那一支）。
                    host.ShowRadioMessage(tip, host.CardSprite(card),
                                          sound.HasValue ? sound.Value.clip : null,
                                          host.CardTypeOf(card));                   // `0x1806634C0`
                    break;
                }

                case ScriptedActionType.LandWarlords:                               // 160
                    host.LandWarlordsInCampaign();                                  // `0x18096F2F0`
                    break;

                case ScriptedActionType.PlayerChat:                                 // 50
                    host.DisplayChatBox(text, host.GetHero(true), sound.HasValue ? sound.Value.clip : null);
                    break;

                case ScriptedActionType.PlayerChatBig:                              // 55
                {
                    var card = host.GetHero(true);
                    if (!string.IsNullOrEmpty(action.radioSpeakerCard))
                    {
                        var byId = host.FindCardByUniqueId(action.radioSpeakerCard);
                        if (byId == null)
                            host.LogWarning($"[PlayerChatBig] 找不到 `uniqueId == \"{action.radioSpeakerCard}\"` ⇒ 用 hero 自己的立绘"
                                          + "（原版这里 NRE）");
                        else card = byId;
                    }
                    host.DisplayBigChat(text, host.FullHeroSprite(card));            // 重载 B（`0x180960580`）
                    break;
                }

                case ScriptedActionType.AiChat:                                     // 60
                    if (!string.IsNullOrEmpty(text))
                        host.DisplayChatBox(text, host.GetHero(false), sound.HasValue ? sound.Value.clip : null);
                    else
                        host.DisplayWarlordChatMessage(0 /* ChatMessage.Intro */, false, true);   // `0x180662340`
                    break;

                // 其余所有档位：原版**什么都不做**（落到公共收尾 `0x1809409FF`）—— ⛔ 别自己加行为。
            }

            if (sound.HasValue) host.PlaySoundAsset(sound.Value);                   // `0x1809409FF-0x180940A0D`
        }

        /// <summary>
        /// 原版 `AiScripted.ResetActionCounter()`（`decomp_full/AiScripted__ResetActionCounter.c`）：
        /// `*(longlong *)(this + 0x10) = 0` —— **一次写 8 字节**，把 `actionCounter(+0x10)` 与
        /// `actionCampaignCounter(+0x14)` **一起**清零。
        /// <para>⚠️ 本类只持**战役**那一个指针；教程那个的归零在
        /// `RuleEngine/Core/TutorialScript.cs` 的 `ResetActionCounter()`（那边是它唯一的家，⛔ 别在这里再存一份）。</para>
        /// </summary>
        public void ResetActionCounter() { ActionCampaignCounter = 0; }
    }

    // ==================================================================
    //  四、回合驱动器（原版 `BattleManager.ExecuteCampaignScriptedTurn` 那个**协程**）
    // ==================================================================

    /// <summary>一拍的结果：**等多久** + **是不是跑完了**。
    /// ⚠️ 顺序：`finished == true` 时，**先等完 `waitSeconds` 再收尾**（原版是 `yield wait` → 收尾，见下）。</summary>
    public struct CampaignScriptStep
    {
        public bool finished;
        public float waitSeconds;
    }

    /// <summary>
    /// 原版 `BattleManager.ExecuteCampaignScriptedTurn(List&lt;ScriptedActionCampaignData&gt;, Action onComplete)`
    /// 的**真体**（kickoff 在 🥈 `out_cpp2il_dev_ilspy/BattleManager.cs:35578-35592`，
    /// 真体在 🥇 `decomp_full/BattleManager._ExecuteCampaignScriptedTurn_d__598__MoveNext.c` 全文
    /// ＋ 🥈 同文件 `BattleManager.cs:2701-2825`）。
    ///
    /// <para>**原版的时序**（逐句）：</para>
    /// <code>
    /// ① `coroutineCounter(+0x534)++` → Log("Start ExecuteScriptedTurn. coroutineCounter " + n)
    /// ② 列表 null 或 `Count == 0` ⇒ LogWarning("ExecuteCampaignScriptedTurn with null turnScriptedData " + (list==null)) → **结束**
    /// ③ `duringScriptedActionsFlag(+0x268) = true`
    /// ④ `AiScripted.ResetActionCounter()`（**8 字节**：教程 + 战役两个指针一起清零）
    /// ⑤ `availableActions = true`
    /// ⑥ ⊙ `while (availableActions) {`
    ///      a. Log("Resolve next action") → **yield `WaitForSecondsRealtime(1.0f)`**
    ///           （常量实读 `DAT_1834b2bb8` = `0x3f800000` = 1.0f）
    ///      b. **先** `wait = GetCurrentWaitTime(manager, list)`（**这一条**的时长 —— 下标还没动）
    ///      c. **再** `availableActions = PlayScriptedTurn(manager, list)`（执行这一条 + 指针 ++）
    ///      d. `duringScriptedActionsFlag = true` → **yield `WaitForSecondsRealtime(wait)`**
    ///    `}`
    /// ⑦ `duringScriptedActionsFlag = false` → `onComplete?.Invoke()`
    /// </code>
    /// <para>🔴 **b/c 的次序是判据**（`_d__598__MoveNext.c:75-87`）：先算时长、后执行 ——
    /// 因为 `GetCurrentWaitTime` 读的是**指针还指着的那一条**（= 即将播放的那条）。写反了会让
    /// 每一拍都等**上一条**的时长（静默、只在有多条不同时长的脚本里看得出）。</para>
    ///
    /// <para>🔶 自己设计（非原版）：**协程 → `Begin/Pump/End` 三步的口**。
    /// 原版是 `IEnumerator` + 两次 `yield return new WaitForSecondsRealtime(...)`；
    /// 我们的驱动侧是「每帧一拍」的口（同 `BattleDriver.DriveTutorialScript` 那套），
    /// 所以这里把「等」交回调用方（`CampaignScriptStep.waitSeconds`），
    /// **两次相邻的 yield 合并成一次加和**（a 的 1.0f ＋ d 的 wait）—— 两者之间没有别的动作，
    /// 时长等价；差别只有「中间多一条 Log("Resolve next action")」的时机，如实记在这里。
    /// 另：`duringScriptedActionsFlag` 的**置 false 时机**保持原版的「跑完再置」——
    /// 由调用方在等完最后一拍的 `waitSeconds` **之后**调 <see cref="End"/>。
    /// 调度台 2026-10-27 裁定。</para>
    /// </summary>
    public class CampaignScriptTurnDriver
    {
        /// <summary>原版 `BattleManager.coroutineCounter`（`dump.cs:30924`，**`+0x534`**）。
        /// ⚠️ 这一个计数器**与教程那个协程（`_ExecuteScriptedTurn_d__597`）共用** ——
        /// 接线时两边必须指向同一个计数器，⛔ 别各数各的。</summary>
        public int CoroutineCounter { get; set; }

        /// <summary>原版 `BattleManager.duringScriptedActionsFlag`（`dump.cs:30929`，**`+0x268`**，`ObscuredBool`）。
        /// 原版在脚本序列开始时置 `true`、**跑完（等完最后一拍）** 才置 `false`。</summary>
        public bool DuringScriptedActions { get; private set; }

        /// <summary>一拍与一拍之间的固定等待（原版 `WaitForSecondsRealtime(1f)`，常量 `0x3f800000`）。</summary>
        public const float ResolveNextActionDelay = 1.0f;

        public CampaignScriptRunner Runner { get; } = new CampaignScriptRunner();

        ICampaignScriptHost _host;
        List<CampaignScriptAction> _turnData;
        bool _availableActions;
        bool _begun;

        /// <summary>
        /// 原版协程的 state 0（`_d__598__MoveNext.c:33-58`）。
        /// <para>`finished == true` ⇒ **这一回合压根没跑**（列表 null/空 —— 原版同样出声后结束，且**不置** flag）；
        /// 否则调用方按 `waitSeconds` 等完再 <see cref="Pump"/>。</para>
        /// </summary>
        public CampaignScriptStep Begin(ICampaignScriptHost host, List<CampaignScriptAction> turnData)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            _host = host; _turnData = turnData; _begun = false; _availableActions = true;

            CoroutineCounter++;
            host.Log("Start ExecuteScriptedTurn. coroutineCounter " + CoroutineCounter);   // `:41-44`

            if (turnData == null || turnData.Count == 0)                                  // `:46-57`
            {
                host.LogWarning("ExecuteCampaignScriptedTurn with null turnScriptedData " + (turnData == null));
                return new CampaignScriptStep { finished = true, waitSeconds = 0f };
            }

            DuringScriptedActions = true;                       // `:63`（`+0x268 = ObscuredBool(true)`）
            Runner.ResetActionCounter();                        // `:70`（`0x180944280`，8 字节一起清）
            _begun = true;
            // 原版接着落进 IL_0435：`availableActions` 为真 ⇒ 先等 1.0 s 再去执行第一条。
            return new CampaignScriptStep { finished = false, waitSeconds = ResolveNextActionDelay };
        }

        /// <summary>
        /// 原版协程的 state 1 + 下一轮的 IL_0435（`_d__598__MoveNext.c:64-95` ＋ `:110-120`）。
        /// <para>**先算时长、后执行**（见类 doc 里 🔴 那一条）。</para>
        /// <returns>`finished == true` ⇒ 没有下一条了；调用方**等完 `waitSeconds`** 再调 <see cref="End"/>。</returns>
        /// </summary>
        public CampaignScriptStep Pump()
        {
            if (!_begun || !_availableActions) return new CampaignScriptStep { finished = true, waitSeconds = 0f };

            float wait = Runner.GetCurrentWaitTime(_host, _turnData);          // 原版 b（`:78-82`，**在前**）
            _availableActions = Runner.PlayScriptedTurn(_host, _turnData);     // 原版 c（`:84-86`，**在后**）

            if (_availableActions)
            {
                // 原版 d 的 `yield wait` ＋ 下一轮 a 的 Log/`yield 1.0s`（两者之间没有动作 ⇒ 合并）
                return new CampaignScriptStep { finished = false, waitSeconds = wait + ResolveNextActionDelay };
            }
            return new CampaignScriptStep { finished = true, waitSeconds = wait };
        }

        /// <summary>
        /// 原版协程的收尾（`_d__598__MoveNext.c:122-136`）：`duringScriptedActionsFlag = false`，
        /// 然后 `onComplete?.Invoke()`。⛔ **必须在等完最后一拍的 `waitSeconds` 之后调**（否则 flag 早关）。
        /// </summary>
        public void End()
        {
            DuringScriptedActions = false;
            _begun = false;
            _availableActions = false;
            _turnData = null;
            _host = null;
        }

        /// <summary>默认构造（**不接**教程那半边 —— 与从前逐字等价）。</summary>
        public CampaignScriptTurnDriver() { }

        /// <summary>🆕 2026-10-27（`E3CMP` · 第四批）：把原版那**一次写 8 字节**接上。
        /// 判据 🥇 `decomp_full/AiScripted__ResetActionCounter.c` = `*(long long *)(this + 0x10) = 0`
        /// ⇒ `actionCounter(+0x10)`（教程）与 `actionCampaignCounter(+0x14)`（战役）**一起**清零。
        /// 我们两个计数器分在两层（教程 = `RuleEngine/Core/TutorialScript.cs`，战役 = 本类的 `Runner`），
        /// ⇒ 在这里把本执行器的战役指针挂到教程那边的归零口（`TutorialScript.ResetCampaignActionCounter`）。
        /// `tutorial == null` ⇒ 不挂，行为与从前逐字等价。</summary>
        public CampaignScriptTurnDriver(RuleEngine.TutorialScript tutorial)
        {
            if (tutorial != null) tutorial.ResetCampaignActionCounter = () => Runner.ActionCampaignCounter = 0;
        }
    }

    // ==================================================================
    //  五、**真实宿主**（账 94 · 执行代理 `B97_W3` 2026-10-29 新建）
    // ==================================================================
    //  与上面的 `NullCampaignScriptHost` 分工：
    //    · `NullCampaignScriptHost` = 「宿主还没有」时的**出声兜底**（战役局还没开、数据也不在本地）；
    //    · **本类** = 把 `ICampaignScriptHost` 那 15 个成员逐条接到**工程里已有的真实资源/管理器**上
    //      （卡表 / `UnitChatPanel` / `VoiceLines` / `CardArt` / `AudioSource` / `Input`）。
    //
    //  🔴 两条必须写死的口径：
    //    ① **`DisplayBigChat` 两个重载在本 build 是【空桩】**（VA 实证：`0x1809604B0` 重载 A、`0x180960580` 重载 B
    //       的正常路径都直接 return；调度台 2026-10-29 裁定「照抄空桩」）⇒ 本类**只转发**给
    //       `Battle/BattleBigChat.Display`（那个类已经按「照抄空桩」实现），⛔ **别在这里自己显示**
    //       —— 自己显示会比原版**多**一屏大聊天。
    //    ② 战役脚本数据（`TimedCampaignObject` / `WarlordPathData`）**本地不存在**（判据 → 本文件文件头那段）
    //       ⇒ 接上本类之后仍然**零可观测**。这是「休眠 ≠ 不做」（铁律 11），⛔ 不是「接上就完事」。
    //
    //  ⚠️ 接线口（`BattleDriver` 的动作口）**不在本代理白名单** ⇒ 精确补丁写在报告 `报告/B97_W3.md`，
    //     由调度台转派。本类只要求构造时给一个 `BattleDriver`。
    public sealed class BattleCampaignScriptHost : ICampaignScriptHost
    {
        readonly BattleDriver _driver;
        readonly List<string> _warned = new List<string>();
        AudioSource _sfx;
        Dictionary<string, RuleEngine.CardDef> _byId;

        /// <summary>`driver == null` ⇒ 每个成员都走「出声 + 空值」，但**仍然不是** Null 宿主的语义
        /// （它会**真的**去读卡表 / 美术 / 语音；拿不到才出声）。自检里可以用它验「不接线时不 NRE」。</summary>
        public BattleCampaignScriptHost(BattleDriver driver) { _driver = driver; }

        RuleEngine.BattleContext Ctx { get { return _driver != null ? _driver.Ctx : null; } }
        UnitChatPanel Chat { get { return _driver != null ? _driver.UnitChat : null; } }
        int Me { get { return _driver != null ? _driver.MySeat : 0; } }

        void Warn(string message)
        {
            string k = message ?? "";
            if (_warned.Contains(k)) return;
            _warned.Add(k);
            Debug.LogWarning("[Campaign] " + k);
        }

        /// <summary>原版 `CustomDebug.Log`（`ExecuteAction` 开头那一条）。</summary>
        public void Log(string message) { Debug.Log("[Campaign] " + message); }

        /// <summary>原版没有 warning 口；本工程红线「不许静默失败」⇒ 出声（按文案去重）。</summary>
        public void LogWarning(string message) { Warn(message); }

        /// <summary>原版 `ScriptedActionCampaignData.GetBundledSound` 里的 `AssetBundle.LoadAsset&lt;AudioClip&gt;(name)`。
        /// 我们本地唯一一条音频路 = `Resources/{VoiceLines.ClipRoot}{name}`（教程那一族已经在用同一个根）。</summary>
        public AudioClip ResolveBundledSound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return null;
            var clip = Resources.Load<AudioClip>(VoiceLines.ClipRoot + soundName);
            if (clip == null)
                Warn($"音效 `{soundName}` 取不到（找 `Resources/{VoiceLines.ClipRoot}{soundName}`）"
                   + " ⇒ 这一条**没声**");
            return clip;
        }

        /// <summary>原版 `BattleManager.GetHero(bool playerHeroY)`：`true` = **玩家**督军、`false` = **敌方**督军。
        /// 座位换算用 `BattleDriver.MySeat`（本地玩家恒 `_me`）。</summary>
        public RuleEngine.CardDef GetHero(bool playerHeroY)
        {
            var ctx = Ctx;
            if (ctx == null || ctx.Players == null) { Warn("`GetHero`：对局上下文还没建 ⇒ 没有督军可给"); return null; }
            int seat = playerHeroY ? Me : 1 - Me;
            if (seat < 0 || seat >= ctx.Players.Length || ctx.Players[seat] == null)
            { Warn($"`GetHero({playerHeroY})`：座位 {seat} 不存在"); return null; }
            var w = ctx.Players[seat].Warlord;
            if (w == null || w.Card == null) { Warn($"`GetHero({playerHeroY})`：座位 {seat} 没有督军卡"); return null; }
            return w.Card;
        }

        /// <summary>原版 `PlayerDataManager.allCardCollection.Find(x =&gt; x.uniqueId == id)` —— 我们 = 卡表全池。</summary>
        public RuleEngine.CardDef FindCardByUniqueId(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;
            if (_byId == null)
            {
                var pool = RuleEngine.CardDatabase.Load();
                _byId = pool != null ? RuleEngine.CardDatabase.IdIndex(pool) : new Dictionary<string, RuleEngine.CardDef>();
            }
            RuleEngine.CardDef def;
            if (_byId.TryGetValue(uniqueId, out def) && def != null) return def;
            Warn($"卡表里找不到 `uniqueId == \"{uniqueId}\"`（`CardDatabase.Load()` 全池）");
            return null;
        }

        /// <summary>这张卡归哪一侧（`CardScript.isPlayer` 的等价物）：
        /// 督军格 → 棋盘 → 手牌，逐层按**实例号（Id）**比（⛔ 不按 `CardDef` 引用比 —— 同一张卡可能两份定义）。
        /// 认不出 ⇒ `-1`（调用方出声，⛔ 不猜）。</summary>
        int SeatOf(RuleEngine.CardDef card)
        {
            var ctx = Ctx;
            if (ctx == null || card == null || ctx.Players == null) return -1;
            for (int p = 0; p < ctx.Players.Length && p < 2; p++)
            {
                var ps = ctx.Players[p];
                if (ps == null) continue;
                if (ps.Warlord != null && ps.Warlord.Card != null && ps.Warlord.Card.Id == card.Id) return p;
                if (ps.Board != null)
                    for (int s = 0; s < ps.Board.Length; s++)
                    {
                        var u = ps.Board[s];
                        if (u != null && u.Card != null && u.Card.Id == card.Id) return p;
                    }
                if (ps.Hand != null)
                    for (int i = 0; i < ps.Hand.Count; i++)
                    {
                        var h = ps.Hand[i];
                        if (h != null && h.Card != null && h.Card.Id == card.Id) return p;
                    }
            }
            return -1;
        }

        /// <summary>**座位 → 气泡序号**：原版三颗盒子 `0 = 我方 / 1 = 敌方 / 2 = RadioChat`，
        /// 且是**视图侧**（本地玩家恒 0）⇒ 与 `BattleDriver.BubbleSideOf` 同一口径
        /// （那个是 private，本类自己算一遍：`seat == MySeat ? 0 : 1`）。</summary>
        int BubbleOf(int seat) { return seat == Me ? 0 : 1; }

        /// <summary>原版 `VoiceLinesController.DisplayChatBox(string, CardScript talkingUnit, AudioClip)`。
        /// 气泡两侧由 `talkingUnit.isPlayer` 定 ⇒ 我们按 <see cref="SeatOf"/> 求座位再换成气泡序号。</summary>
        public void DisplayChatBox(string text, RuleEngine.CardDef talkingUnit, AudioClip clip)
        {
            var chat = Chat;
            if (chat == null) { Warn("`DisplayChatBox`：`UnitChatPanel` 还没建（非战斗 HUD 阶段）⇒ 这一句没显示"); return; }
            if (string.IsNullOrEmpty(text)) { Warn("`DisplayChatBox`：文案为空（原版 `$Trying to send empty chat`）⇒ 不显示"); return; }
            int seat = SeatOf(talkingUnit);
            if (seat < 0) { Warn($"`DisplayChatBox`：认不出「{talkingUnit?.Id}」是哪一侧 ⇒ 按敌方气泡显示"); seat = 1 - Me; }
            string id = talkingUnit != null ? talkingUnit.Id : "CampaignChat";
            string artKey = talkingUnit != null ? BattleDriver.ArtKey(talkingUnit) : null;
            string name = talkingUnit != null ? talkingUnit.Name : null;
            bool shown = chat.Speak(BubbleOf(seat), id, "CampaignChat", artKey, name, text, clip,
                                    null, null, null, false, clip != null);
            if (!shown) Warn($"`DisplayChatBox`「{text}」**没显示出来**（`UnitChatPanel.Speak` 返回 false）");
        }

        /// <summary>原版 `BattleManager.DisplayBigChat(string, CardScript)`（重载 A）。
        /// 🔴 **本 build 是空桩**（VA `0x1809604B0`）⇒ 只转发，**不显示**。</summary>
        public void DisplayBigChat(string text, RuleEngine.CardDef talkingUnit)
        {
            BattleBigChat.Display(Chat, text, talkingUnit);
        }

        /// <summary>原版 `BattleManager.DisplayBigChat(string, Sprite)`（重载 B）。
        /// 🔴 **本 build 是空桩**（VA `0x180960580`）⇒ 只转发，**不显示**。</summary>
        public void DisplayBigChat(string text, Texture2D talkingUnitSprite)
        {
            BattleBigChat.Display(Chat, text, talkingUnitSprite);
        }

        /// <summary>原版 `VoiceLinesController.ShowRadioMessage(SmallTipData, Sprite, AudioClip, CardTypeOptions)`
        /// —— 明确传给**第 3 颗盒子 `RadioChat`**（`VoiceLinesController__ShowRadioMessage.c:28` 传 `2`）。
        /// <para>⚠️ **如实记的缺口**：我们的 `UnitChatPanel.Speak` 只吃 `artKey`（字符串），**不吃 `Texture2D`**
        /// ⇒ 电台那一条**只有文字 + 语音、没有立绘**（原版那一格是 `rawCard.cardSprite`）。
        /// 要补得给 `UnitChatPanel` 加一条「按贴图说话」的口（不在本代理白名单）。</para></summary>
        public void ShowRadioMessage(CampaignSmallTipData tipData, Texture2D speakerSprite,
                                     AudioClip audioClip, int cardType)
        {
            var chat = Chat;
            if (chat == null) { Warn("`ShowRadioMessage`：`UnitChatPanel` 还没建 ⇒ 这一条没显示"); return; }
            string text = tipData != null ? tipData.tipText : null;
            if (string.IsNullOrEmpty(text)) { Warn("`ShowRadioMessage`：`tipText` 为空 ⇒ 不显示"); return; }
            if (speakerSprite == null) Warn("`ShowRadioMessage`：立绘为空（原版 `DisplayChatBox without talkingUnit defined`）");
            // `cardType` 那一格原版用来过 `CanUseVOs` 闸（`VoiceLinesController__ShowRadioMessage`）；
            // 我们的 `UnitChatPanel.Speak` 没有这条闸 ⇒ 只把值原样记进日志（⛔ 不发明分支）。
            bool shown = chat.Speak(BattleBigChat.Side /* 2 = RadioChat */, "CampaignRadio", "CampaignRadio",
                                    null, null, text, audioClip, null, null, null, false, audioClip != null);
            if (!shown) Warn($"`ShowRadioMessage`「{text}」**没显示出来**（`UnitChatPanel.Speak` 返回 false）");
            Log($"ShowRadioMessage：cardType={cardType}（原版只在 `CanUseVOs` 闸里用它；我们没有那条闸）");
        }

        /// <summary>原版 `VoiceLinesController.DisplayWarlordChatMessage(ChatMessage chatMsgIndex, bool isPlayer, bool ignoreUiState)`。
        /// <para>🔴 **只落 `ChatMessage.Intro = 0` 这一档**（战役脚本里唯一用到的那一档：
        /// `AiScripted.ExecuteAction` 的 `AiChat` 空文案支传的就是 `Intro`）。文案取法 = 该侧督军的
        /// **开场白官方词条**（`Chat_Messages/&lt;chatId&gt;-Chat0`，`CardChatIntro.TermFor` + `VoiceLines.ForIntro`）；
        /// **其余枚举值出声、不显示**（我们的工程里没有 `ChatMessage` 枚举，见
        /// `Battle/ChatPopupPanel.cs:1396`；⛔ 不按序号猜是哪一句）。</para>
        /// <para>⚠️ `ignoreUiState`（原版用来跳过「UI 状态不允许聊天」那道闸）我们没有那条闸 ⇒ 无操作。</para></summary>
        public void DisplayWarlordChatMessage(int chatMsgIndex, bool isPlayer, bool ignoreUiState)
        {
            var chat = Chat;
            if (chat == null) { Warn("`DisplayWarlordChatMessage`：`UnitChatPanel` 还没建 ⇒ 这一句没显示"); return; }
            if (chatMsgIndex != 0)
            {
                Warn($"`DisplayWarlordChatMessage(idx={chatMsgIndex})`：本工程只落了 `ChatMessage.Intro(0)` 这一档"
                   + " ⇒ **这一句不显示**（⛔ 不按序号猜台词）");
                return;
            }
            var hex = GetHero(isPlayer);
            if (hex == null) return;
            int seat = isPlayer ? Me : 1 - Me;
            string term = CardChatIntro.TermFor(hex.Id, "-Chat0");
            string text = UnitChatPanel.OfficialChatTextOfTerm(term, out term);
            string file, voiceText, ev;
            if (!VoiceLines.TryPick(hex.Id, VoiceLines.ForIntro, null, out file, out voiceText, out ev))
            { file = null; voiceText = null; }
            if (string.IsNullOrEmpty(text)) text = voiceText;                 // 官方表缺键 ⇒ 回落 voice_lines 英文
            if (string.IsNullOrEmpty(text)) { Warn($"`DisplayWarlordChatMessage(Intro, {isPlayer})`：文案取不到 ⇒ 这一句不显示"); return; }
            var clip = VoiceLines.Clip(file);
            bool shown = chat.Speak(BubbleOf(seat), hex.Id, "CampaignIntro", BattleDriver.ArtKey(hex), hex.Name,
                                    text, clip, file, null, term, false, clip != null);
            if (!shown) Warn($"`DisplayWarlordChatMessage(Intro, {isPlayer})`「{text}」**没显示出来**");
        }

        /// <summary>原版 `BattleManager.LandWarlordsInCampaign()`（`BattleManager__LandWarlordsInCampaign.c` 全文：
        /// 只是 `StartCoroutine` 一个 `<Int32Enum>` 闭包，枚举实参 0 ⇒ 真体在 `CardScript._HeroLandIntoField_d__318__MoveNext`）。
        /// <para>⚠️ **如实记的缺口**：我们这一跳的等价物（督军 `HeroLandIntoField` 落地演出）**没有公开入口**
        /// —— `CardView` 那一侧要 `abovePos/landPos`，而求那两个点的 `Get2DPosOfCard` 是 `BattleDriver` 的 private；
        /// `DeploySequence.Play(...)` 虽是 public，但坐标得调用方给 ⇒ 本类**只出声、不放演出**。
        /// 补法 = 给 `BattleDriver` 加一个 `public void CampaignLandWarlords()`（补丁写在报告 §②）。</para></summary>
        public void LandWarlordsInCampaign()
        {
            Warn("`LandWarlordsInCampaign`：督军落地演出的公开入口还没接（`HeroLandIntoField` 一族；"
               + "坐标要靠 `BattleDriver.Get2DPosOfCard` 这个 private 口）⇒ 这一跳**没有演出**");
        }

        /// <summary>原版 `BattleManager.PlaySoundAsset(SoundAsset_OLD)` —— `ExecuteAction` 的末尾一跳。
        /// 音量口径逐字照 `PlayNextSoundInQueue`：`PlayOneShot(clip, AudioListener.volume * soundAsset.volume)`
        /// （`soundAsset.volume` 恒 1.0，判据 → `ScriptedActionCampaignData.GetSoundAsset`）。</summary>
        public void PlaySoundAsset(CampaignSoundAsset soundAsset)
        {
            if (soundAsset.clip == null) return;      // 原版 `GetSoundAsset` 取不到音 ⇒ clip 为空 ⇒ 这一条本来就没声
            if (_driver == null) { Warn("`PlaySoundAsset`：没有 `BattleDriver` ⇒ 没有可挂 `AudioSource` 的宿主"); return; }
            if (_sfx == null)
            {
                _sfx = _driver.gameObject.AddComponent<AudioSource>();
                _sfx.playOnAwake = false;
                _sfx.spatialBlend = 0f;                       // 原版 `Play2D`
                _sfx.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;
            }
            _sfx.PlayOneShot(soundAsset.clip, AudioListener.volume * soundAsset.volume);
        }

        /// <summary>原版 `ChatManager.GetChatMessage(ChatMessage.Intro, isPlayer)`（`AiChatBig` 文案为空时的兜底）。
        /// 我们 = 该侧督军的开场白官方词条（`Chat_Messages/&lt;chatId&gt;-Chat0`）；缺键 ⇒ 空串 + 出声（⛔ 不自译）。</summary>
        public string GetIntroChatMessage(bool isPlayer)
        {
            var hex = GetHero(isPlayer);
            if (hex == null) return "";
            string term = CardChatIntro.TermFor(hex.Id, "-Chat0");
            string text = UnitChatPanel.OfficialChatTextOfTerm(term, out term);
            if (string.IsNullOrEmpty(text))
            {
                Warn($"`ChatManager.GetChatMessage(Intro, {isPlayer})`：官方表里没有 `{term}` ⇒ 文案用空串");
                return "";
            }
            return text;
        }

        /// <summary>原版 `MousePositionDebug.GetMouseClickPosition()`（`RadioMessage` 那条的 `tipPosition`）。
        /// ⚠️ 我们取**当前指针位置**（`Input.mousePosition`）—— 原版记的是**上一次点击**的位置；
        /// 这一格只影响大提示（`SmallTipData.tipPosition`）指向哪儿，且战役数据本地不存在 ⇒ 今天零可观测。</summary>
        public Vector2 GetMouseClickPosition() { return Input.mousePosition; }

        /// <summary>原版 `RawCardScript.fullHeroSpriteReference.Load()`（`PlayerChatBig` 那条的立绘）。
        /// 我们的等价物 = `CardArt.TryDeckHero`（它的判据本身就是 `fullHeroSpriteReference`）。</summary>
        public Texture2D FullHeroSprite(RuleEngine.CardDef card)
        {
            if (card == null) return null;
            Texture2D tex;
            if (CardArt.TryDeckHero(card.Id, out tex)) return tex;
            Warn($"`fullHeroSprite`（`CardArt.TryDeckHero`）取不到 `{card.Id}`");
            return null;
        }

        /// <summary>原版 `RawCardScript.cardSprite`（`RadioMessage` 那一支的**卡面插图**，与 `FullHeroSprite` 不是一回事）。</summary>
        public Texture2D CardSprite(RuleEngine.CardDef card)
        {
            if (card == null) return null;
            var tex = CardArt.Portrait(card.Id);
            if (tex == null) Warn($"`cardSprite`（`CardArt.Portrait`）取不到 `{card.Id}`");
            return tex;
        }

        /// <summary>原版 `RawCardScript.cardType @+0x94`（`CardTypeOptions`：`Minion 0 / Hero 10 / Tactic 20 / Whispers 40`）。</summary>
        public int CardTypeOf(RuleEngine.CardDef card)
        {
            if (card == null) { Warn("`CardTypeOf(null)` ⇒ 传 0"); return 0; }
            if (card.CardType >= 0) return card.CardType;
            Warn($"`RawCardScript.cardType` **判不了**（`{card.Id}`：卡表没有 `cardType` 列，或这张卡不在真值快照里）⇒ 传 0");
            return 0;
        }
    }
}
