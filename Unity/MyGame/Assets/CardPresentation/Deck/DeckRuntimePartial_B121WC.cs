// DeckRuntimePartial_B121WC.cs — 执行代理 `B121_WC`（第一百二十一/二十二会话 · 2026-11-03）
//
// 🔴 **为什么单开一个 partial 文件**：`DeckRuntime.cs` 有 **157 处账落点按 `文件:行号` 引用**（最深 `:10085`）
//   ⇒ 往它中间插成员会让下游整体漂移。本文件里的成员**全部是新增**（⛔ 不改既有成员的语义），
//   落在**新文件**里，`DeckRuntime.cs` 那边只做「同一物理行追加」的接线（零行数变化）。
//
// 本批四件（用户 2026-11-03 要求 · 子报告 `报告/B121_COLL_收藏界面审计.md` / `B121_COLL_C_卡组编辑窗.md`）：
//   · **COLL-03** `RowReorderEnabled`（= `false`）：卡组行「拖动 = 重排」**整条关掉**（原版这扇窗没有重排）。
//   · **COLL-05** `SetDoneButtonText`：Done 那颗钮的文案随「卡组合不合法」在 `Done` / `Discard` 之间切。
//   · **COLL-06**（部分）`PunchDoneButton`：「不合法 → 合法」那一下的 `DOPunchScale` 弹跳。
//   · **COLL-04** `DeckShareString`：`Share` 写进剪贴板的内容补上**可读头部**（原版四段 + 末行 base64）。
//
// ⚠️ 判据出处写在每个成员的 doc 上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    public partial class DeckRuntime
    {
        // ============================================================================================
        //  COLL-03 · 「卡组行内拖动 = 重排」的总闸 —— **关**（原版这扇窗没有重排）
        // ============================================================================================
        //
        // **判据**：`Deck Editing Menu` 那棵树**全树没有 `DragAndDropItem`**（本批现读；
        //   反编译 `CardDeck.ToggleReorder` / `SetSortMode` 在本 build **0 调用点**、字段 `readonly bool` 恒 false
        //   ⇒ 原版编辑窗**没有「拖行重排」这条交互**）；行被拖起之后真正发生的事是**删除**
        //   （`DeckEditingPanel__CheckCardSlotDrag.c` → `RemoveCard`，我们 = `EndDrag` 的「拖出右缘」那一支）。
        //   ⚠️ 我们原来那条重排的依据疑似**错挂**到收藏窗那族 `CollectionDeck` 的 `DragAndDropItem`
        //   （那是**卡组列表**的重排、且也是 P2 未建）—— 详见 `报告/B121_COLL_收藏界面审计.md` §3·2 与
        //   `B121_COLL_C_卡组编辑窗.md` 问题 5。
        //
        // 🔴 **为什么用「开关常量」而不是把代码删掉**：`DeckRuntime.cs` 的 `EndDragAt` 在**账落点之间**
        //   ⇒ 删那 20 行会让下面 157 条落点整体漂移（铁律 18·b）。所以行为上**关死**、代码留档，
        //   `EndDragAt` 的注释里写明「⛔ 别恢复」。纯逻辑那半边（`DeckEditorState.TryMoveInList` /
        //   `TryMove` + 它自己的自检块）**保留**：那是数据层能力，本笔只摘界面接线。
        // ⚠️ 用 `static readonly`（不是 `const`）⇒ 编译器不会把它折成「死代码」警告，闸读起来仍是活的。
        static readonly bool RowReorderEnabled = false;

        /// <summary>自检读口：**行内拖动重排已经关掉**（`RowReorderEnabled == false`）。COLL-03 的验收点 ——
        /// 原版这扇窗没有重排；本读口让断言不必去碰私有字段。</summary>
        public static bool RowReorderDisabled { get { return !RowReorderEnabled; } }

        // ============================================================================================
        //  COLL-05 / COLL-06 · Done 钮：文案随合法性切 + 「变合法」那一下的弹跳
        // ============================================================================================
        //
        // **原版**（🥇 il2csharp 可读版 `DeckEditingWindow.cs:558-589` 的 `UpdateDoneButton(bool firstTime)`，
        //   逐句）：① `text = ValidateDeck(...) ? doneKey : discardKey`（**两个分支都写**）；
        //   ② `firstTime` ⇒ `Toggle(valid, 0f)`（首帧**硬切**）；③ 变合法 ⇒ `Play2D(doneButtonEnabled)` +
        //   `Toggle(true, 0.3f)` + `DOKill(transform, true)` + `DOPunchScale(one×0.05, 0.5s, vibrato 5, elasticity 1.0)`；
        //   ④ 变不合法 ⇒ `Toggle(false, 0.3f)`。
        //
        // **我们的落点**：文案 = `_footDoneText`（`BuildFooter` 里那颗 `foot_done_t`）；Highlight 灯 =
        //   `ToggleDoneHighlight`（既有，A330）；Shiny 扫光 = `FootShine`（既有）—— 三样都由 `RefreshHeader`
        //   一次 `State.Validate()` 的结果驱动（**同一条判据**，原版也是同一次 `ValidateDeck`）。
        //
        // ⚠️ **如实记：本笔没做齐的两小项**（都在原版 `UpdateDoneButton` 里）：
        //   ① ✅ **2026-11-03 已补（`B121-AUD-05`）**：`Play2D(doneButtonEnabled)` 的 cue 名解出来了 =
        //      **`Deck Editing Valid Deck`**（PPtr → 束 externals → cue 包装，判据链见本文件 COLL-11 那段）
        //      ⇒ 现在在「变合法」那一拍播（`PunchDoneButton` 里那句），⛔ 仍然不拿 `WindowSounds.PositiveCue` 顶。
        //   ② `Toggle(valid, 0.3f)` 的**淡入时长**：`Done Highlight` 那颗控制器 `Toggle` 的**默认时长就是 0.3f**
        //      （`UIEffectTogglerController.Toggle(bool, float duration = 0.3f, …)`）⇒ 这一条**本来就对**；
        //      扫光层 `FootShine` **不该**吃这个 0.3（原版那颗的 `DoUpdate` 是空桩，真正动的是 tweener 自己的 1.0s/Loop）。
        //   ③ 「首帧 `Toggle(valid,0)`」这一档**还没对齐**（我们首帧也走 0.3 淡入）—— 精确对齐要动 `ToggleDoneHighlight`，补丁见报告。

        /// <summary>Done 那颗钮上的文字（`BuildFooter` 建完之后挂进来；`null` = 还没建到页脚）。</summary>
        Label _footDoneText;

        /// <summary>Done 那颗钮的两个文案键 —— **原版那两个序列化 `LocalizedString` 字段的名字**
        /// （`DeckEditingWindow.doneKey` / `discardKey`；官方 13 语言表两个键都齐：完成 / 丢弃）。</summary>
        public const string DoneButtonKey = "MenuDeck/MenuButtons/Done";
        /// <summary>见 <see cref="DoneButtonKey"/>（原版 `discardKey`）。</summary>
        public const string DiscardButtonKey = "MenuDeck/MenuButtons/Discard";

        /// <summary>按合法性写 Done 钮上的文字（原版 `UpdateDoneButton` 第 ① 句 —— **每次刷新都写**，
        /// 合法的写 `完成`、不合法的写 `丢弃`）。判据 → 本文件 COLL-05 那段。</summary>
        void SetDoneButtonText(bool valid)
        {
            if (_footDoneText == null) return;              // 页脚还没建 ⇒ 下一次刷新再来（⛔ 不新建第二颗）
            _footDoneText.SetText(Loc.T(valid ? DoneButtonKey : DiscardButtonKey));
        }

        /// <summary>「不合法 → 合法」那一下的弹跳（原版 `DOPunchScale(one×0.05, 0.5s, 5, 1.0)`；
        /// 前一句 `DOKill(transform, true)` 也在）。判据 → 本文件 COLL-05 那段。
        /// <para>⚠️ 调用点自己负责「只在这一拍调」——`RefreshHeader` 里靠 `_footDoneValidLast` 记上一态。</para>
        /// <para>🆕 **2026-11-03（`B121-AUD-05`）**：原版这一拍**还有一声**（`Play2D(doneButtonEnabled)`），
        /// 就排在 `Toggle(true, 0.3f)` 之后、`DOKill` 之前 —— 这里先播它，再 `DOKill` + 弹跳。判据 → 本文件 COLL-11。</para></summary>
        void PunchDoneButton()
        {
            var q = Lookup("foot_done");
            if (q == null) return;                          // 页脚还没建 ⇒ 不弹（⛔ 不静默报错，那是正常时序）
            PlayDoneButtonEnabledCue();                     // 原版 `SoundManager.Play2D(doneButtonEnabled, MixerType.FX)`
            var tf = q.transform;
            DOTween.Kill(tf, true);                         // 原版 `ShortcutExtensions.DOKill(transform, true)`
            tf.DOPunchScale(Vector3.one * 0.05f, 0.5f, 5, 1f);
        }

        /// <summary>上一次刷新「卡组合不合法」的读数（`PunchDoneButton` 只在 **false → true** 那一拍弹）。
        /// <para>🔴 **初值 `true`（不是 `false`）**：原版 `UpdateDoneButton(firstTime: true)` 那一档
        /// **不播弹跳**（只 `Toggle(valid, 0f)` 硬切）⇒ 首帧「本来就是合法」的那一副**不该弹**；
        /// 初值 `true` 正好把首帧吃掉（首帧不合法时这里被写成 `false`，之后变合法才弹）。</para></summary>
        bool _footDoneValidLast = true;

        // ============================================================================================
        //  COLL-04 · `Share` 写进剪贴板的内容：补**可读头部**（原版 `MakeDeckString`）
        // ============================================================================================
        //
        // **原版逐词实读**（`decomp_full/DeckInfoPopup__MakeDeckString.c` + 字面量表
        //   `资料/普查产出_1025/K3_收藏域复刻审查_执行报告.md` §P1-②）：
        //   `"* " + 卡组名 + "\n*\n* " + Loc("Card_Race/Warlord") + ": " + 督军名 + "\n*\n"`
        //   + 每张**唯一**卡 `"* " + 张数 + "x " + 卡名 + "\n"`
        //   + `"*\n"` + `CardDeck.Serialize(卡组)`（**base64 载荷**）。
        //   ⚠️ 末行 base64 **前面**还有一行 `*`（`"\n*\n"`）⇒ 导入端取「最后一行」正好拿到 base64
        //   （`ImportDeckPopup__TryImport.c:14-18` = `Split('\n').Last()`）—— 两边**互相兼容**，⛔ 别改分隔符。
        //   ⚠️ 英文/中文的「Warlord」取自 `Card_Race/Warlord`（官方表：Warlord / 战将），⛔ 不写死。
        /// <summary>把 <paramref name="payload"/>（= `DeckLibrary.ExportString` 的 base64 **载荷**）
        /// 包成原版那种**可读头部 + 末行载荷**的分享串。判据 → 上面那段（⛔ 别改分隔符，导入端按最后一行取）。</summary>
        public static string DeckShareString(PlayerDeck deck, string payload)
        {
            if (deck == null || string.IsNullOrEmpty(payload)) return payload;
            var sb = new StringBuilder(payload.Length + 256);
            sb.Append("* ").Append(deck.Name ?? "").Append("\n*\n* ")
              .Append(Loc.T("Card_Race/Warlord")).Append(": ").Append(ShareCardName(deck.WarlordId)).Append("\n*\n");
            // 每张**唯一**卡一行：`* Nx 卡名`（原版遍历的是 `cardLibrary` 的**去重**视图，
            //   张数 = `CardDeck.CardCopiesInDeck`）。顺序 = 卡组里的**首次出现序**（不重排）。
            var order = new List<string>();
            var copies = new Dictionary<string, int>();
            var ids = deck.CardIds;
            if (ids != null)
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    int n;
                    if (copies.TryGetValue(id, out n)) { copies[id] = n + 1; continue; }
                    copies[id] = 1;
                    order.Add(id);
                }
            for (int i = 0; i < order.Count; i++)
                sb.Append("* ").Append(copies[order[i]]).Append("x ").Append(ShareCardName(order[i])).Append("\n");
            sb.Append("*\n").Append(payload);
            return sb.ToString();
        }

        /// <summary>分享串里的卡名（**当前语言**的显示名；卡池里查不到这个 id ⇒ 回退 id，⛔ 不印空串）。
        /// 判据 = 原版 `RawCardScript.GetLocalizedCardName`。</summary>
        static string ShareCardName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            var c = CollectionData.Card(id);
            return c != null ? CardText.Name(c) : CardText.Name(id);
        }

        // ============================================================================================
        //  B121-COLL-11 / B121-AUD-05 · Done 钮「变合法」的那一声（原版 `Play2D(doneButtonEnabled)`）
        // ============================================================================================
        //
        // **原版逐句**（🥇 il2csharp `DeckEditingWindow.cs:558-589` 的 `UpdateDoneButton(bool firstTime)`）：
        //   ① `text = ValidateDeck(...) ? doneKey : discardKey`（两个分支都写）；
        //   ② `firstTime` ⇒ `doneButtonTogglerEffect.Toggle(valid, 0f)`（**硬切、不播声**）；
        //   ③ **非首帧 且 合法** ⇒ `SoundManager.Play2D(doneButtonEnabled, MixerType.FX)` →
        //      `Toggle(true, 0.3f)` → `DOKill(transform, true)` → `DOPunchScale(one×0.05, 0.5s, 5, 1.0)`；
        //   ④ 非首帧 且 不合法 ⇒ `Toggle(false, 0.3f)`。
        //   🔴 **组 = FX（0）**，⛔ 不是 Jingles（走 Jingles 的是 `EndBattleDoors` 的胜/败/平，那是另一族）。
        //
        // **cue 名 = `Deck Editing Valid Deck`**（`B121-AUD-05` 解出来的；`B121_WC` 当时只记「没解出」）。
        //   判据链（可复跑，⛔ 别按名字猜）：
        //     ① prefab：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5918047927837519652.json`
        //        （= `Deck Editing Menu` 根那颗 · `m_Script` pathID **4613934872454734249** = `DeckEditingWindow`）
        //        的序列化字段 `doneButtonEnabled = PPtr(m_FileID 6, m_PathID -8540943293993850510)`；
        //     ② externals：该 AssetsFile 的 `externals[5]`（`m_FileID` 是 **1 基**）= **CAB-62d1945b7005c115fb946f0d264a205b**
        //        （= `soundcollection_assets_all` 那一份）；
        //     ③ 解引用：那个束里 pathID `-8540943293993850510` 的 `AudioCue` 包装 = **`Deck Editing Valid Deck`**，
        //        `clipList = [Progress Jingle]` · pitch 1.0 · **vol 1.0** · `timeToPlayAgain 0.1`。
        //   ✅ 这条 cue **早在 `animfx_sounds.json`**（`import_original_sfx.py` 的 `EXTRA_CUES` 收的，参数逐值相同）
        //      ⇒ `WFSoundBank.TryGetCue` 查得到、`WindowSounds.PlayFx` 直接能播，**不需要改音效表**。
        /// <summary>原版 `DeckEditingWindow.doneButtonEnabled` 那条 cue 的**资产名**（判据链 → 本节头）。</summary>
        public const string DoneEnabledCue = "Deck Editing Valid Deck";

        /// <summary>首帧闸：原版 `UpdateDoneButton(firstTime: true)` **不播声**（只 `Toggle(valid, 0f)` 硬切）。
        /// <para>`false` = 还没走过第一次刷新（`PunchDoneButton` 用不到它，它是给精确接线
        /// <see cref="UpdateDoneButtonSound"/> 留的）。</para></summary>
        bool _footDoneFirstSeen;

        /// <summary>播「这副卡组合法了」的那一声（原版 `SoundManager.Play2D(doneButtonEnabled, MixerType.FX)`）。
        /// <para>走 `WindowSounds.PlayFx`：cue 表 + `timeToPlayAgain` 节流 + FX 组**一处收口**（铁律 6：⛔ 别在这里
        /// 再挑一次 clip / 自己 `Lerp` 音量）。这条 cue 是 pitch 1.0 · vol 1.0，与原版一致。</para>
        /// <para>🔴 表里查不到 ⇒ `WindowSounds.Play` **自己出声一次**并返回 false（红线：不静默）。</para></summary>
        void PlayDoneButtonEnabledCue()
        {
            WindowSounds.PlayFx(DoneEnabledCue, "卡组编辑窗 Done 钮「变合法」（原版 UpdateDoneButton）");
        }

        /// <summary>原版「**非首帧 且 合法** ⇒ 播那一句」的等价物（含首帧闸）。
        /// <para>⚠️ **今天还没接线**（`DeckRuntime.cs:4869` 不在本写手白名单）⇒ 眼下由 <see cref="PunchDoneButton"/>
        /// 在「不合法→合法」那一拍代播。精确对齐（原版**每次合法刷新**都播，不只变合法那一拍）的补丁 →
        /// `报告/B121_AUD_四条.md` §补丁；补丁落地时把 `PunchDoneButton` 里那句删掉。</para></summary>
        void UpdateDoneButtonSound(bool valid)
        {
            bool first = !_footDoneFirstSeen;
            _footDoneFirstSeen = true;
            if (first || !valid) return;        // 原版：首帧不播；不合法那一支只有 `Toggle(false, 0.3f)`
            PlayDoneButtonEnabledCue();
        }
    }
}
