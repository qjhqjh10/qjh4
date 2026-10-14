// VoiceLines.cs — 原版**单位语音**的运行时索引（哪张卡、哪个事件、放哪条音频、说什么）
//
// 数据从哪来：
//   · `Resources/voice_lines.json`（**进仓库**的分析产物）—— 由 `工具/import_original_audio.py` 生成。
//     🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1831`/`A1832`）**：原来这里写「从 `card_index.json` 的
//     `cards[].voice` + 音频源清点合并而成」—— **归属口径换了**：现在**以卡 SO 的
//     `summonAudio`/`attackAudio`/`deathAudio` 为权威**（582 条），`card_index.json` 的 `cards[].voice`
//     **降为兜底**（1208 条），名字对不上的仍按名猜但**标 `src:"orphan"`**（58 条；**9 条**四种来源都认不出）。
//     ⚠️ 根因 = **`card_index.json` 它自己错了**（`d:/2/解包整理/` 那份 2026-08-15 的产物，那次卡 SO 还没解出来，
//     它靠名字/目录对、对不上就**并到邻近那张卡**）⇒ 它在 `d:/2` **没改**，只降级。
//     现读数与逐条对照 → `资料/普查产出_第二十八会话/W7_语音导入漏件与归属.md`。
//   · 音频本体在 `Resources/Art/audio/vo/`（**被 .gitignore 排除**，和美术同一条规矩：
//     原版资产不进仓库；重跑那个脚本就能再生）。
//
// ⚠️ **能查到的与查不到的**（`资料/战斗UI_原版对账表.md` §三·〇 :127）：
//   · 查得到：**原版有哪些语音、哪条属于哪张卡**（**卡 SO 是权威**；现读 **637 张卡** ——
//     ~~595 张卡 / 1787 条~~ 是 2026-10-14 归属订正**之前**的读数，⛔ 别再用，现读数见 W7 报告）；
//   · 查不到：**原版在什么时机播哪一条**。文件名后缀给出了候选集合
//     （`greet / attack / death / concede / hurry / intro / mirror / threat / wp / gen1-7 / vs<对手>`），
//     但没有任何代码或数据说「哪个事件对应哪个后缀」。
//   ⇒ 下面 <see cref="PickForDeploy"/> 等**事件→后缀的对应是我们定的**（按字面意思），
//     **不是原版规格**。普通单位卡原版只有**一条**出场台词（`ev = "line"`），
//     所有事件复用那一条（`语音索引.md` §三：原版就是这么设计的）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class VoiceLines
    {
        /// <summary>`Resources` 下那张表的名字（不带扩展名）</summary>
        const string ResourcePath = "voice_lines";
        /// <summary>音频目录（相对 `Resources`）—— 和 `工具/import_original_audio.py` 的落点一致</summary>
        public const string ClipRoot = "Art/audio/vo/";

        [System.Serializable] class Line { public string ev; public string file; public string text; }
        [System.Serializable] class Card { public string id; public string name; public string faction; public List<Line> lines; }
        [System.Serializable] class Db { public int version; public string note; public List<Card> cards; }

        static Dictionary<string, Card> _byId;
        static bool _tried;

        /// <summary>表读进来了没有（读不出来时 <see cref="LoadError"/> 里是人话）。
        /// ⚠️ **这里必须自己 `EnsureLoaded()`** —— 否则第一次问它时永远答「没有」
        /// （懒加载还没被触发），而紧跟其后的 `CardCount` 又会把它加载起来 ⇒
        /// **同一条断言里两个数互相打架**（2026-09-17 自检当场抓到）。</summary>
        public static bool Ready { get { EnsureLoaded(); return _byId != null && _byId.Count > 0; } }
        /// <summary>读失败的原因（**不静默**：自检会把它打出来）</summary>
        public static string LoadError { get; private set; }
        /// <summary>有语音的卡数 / 台词条数（自检用）</summary>
        public static int CardCount { get { EnsureLoaded(); return _byId != null ? _byId.Count : 0; } }
        public static int LineCount { get; private set; }

        static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;

            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                // ⚠️ 这**不是**错误配置，是「音频没导进来」—— 表现层据此什么也不播，并说出来
                LoadError = $"`Resources/{ResourcePath}.json` 没读到（跑 工具/import_original_audio.py 生成）";
                return;
            }
            Db db = null;
            try { db = JsonUtility.FromJson<Db>(ta.text); }
            catch (System.Exception e) { LoadError = $"voice_lines.json 解析失败：{e.Message}"; return; }

            if (db == null || db.cards == null) { LoadError = "voice_lines.json 里没有 cards"; return; }

            _byId = new Dictionary<string, Card>();
            int n = 0;
            foreach (var c in db.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                _byId[c.id] = c;
                if (c.lines != null) n += c.lines.Count;
            }
            LineCount = n;
            // 打一行**给人看的**载入结果：真包日志里能一眼看出「语音系统起来了没有」——
            // 没有这一行的话，「表没读到」和「这几张卡本来就没语音」在日志上**长得一模一样**
            //（本工程的红线是不许静默失败，所以这里主动报一次数）。
            Debug.Log($"[VoiceLines] 语音表已载入：{_byId.Count} 张卡 / {n} 条台词（{ResourcePath}.json）");
        }

        /// <summary>这张卡有没有语音（自检/报表用）</summary>
        public static bool Has(string cardId)
        {
            EnsureLoaded();
            return cardId != null && _byId != null && _byId.ContainsKey(cardId);
        }

        // ==================================================================
        //  🆕 2026-10-14（A1817）逐卡**精确**语音绑定
        //
        //  🔴 **为什么要有这一档**：下面那整段「事件 → 后缀」**是我们定的**（原版只留下「这张卡
        //     有哪些台词」，没留下「什么时机播哪一条」）。**但原版卡 SO 上有**：
        //     `RawCardScript.summonAudio` / `attackAudio` / `deathAudio` 三个**逐卡**字段
        //     （原值快照 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`，
        //     解出 **505 / 41 / 42 条**）。它们以前**只进全量表、没进运行时**
        //     （`工具/gen_card_vfx_by_card.py` 把 `_assetRefs` 整类跳过了）⇒ 表现层只能按
        //     文件名后缀猜「这张卡这个事件该放哪条」。
        //
        //  **数据**：`Resources/card_vfx_by_card.json` 的 `cards[].audio`
        //          （与 `数据/游戏数据/card_vfx_by_card.json` 由**同一次运行**产出 ⇒ 不会分叉）。
        //  **取值**：**精确优先、猜的后缀兜底**（调用点 = `BattleDriver.SpeakFor`）——
        //          ⛔ **不是替换**：精确表没覆盖的卡（约 580 张里没绑定的）照样走老后缀顺序。
        //  ⚠️ **精确表 ≠ 我们的台词表**：同一条 `VO_*` 在我们表里可能**挂在别的卡名下**
        //     （🔴 **2026-10-14 就地订正（铁律 5 · `A1832`）**：原来写「实测 34 处」——
        //     归属改以**卡 SO** 为权威后共 **36 处**变化 = **32 条改判** + **4 条新进**，
        //     例 `BL4`「Chaos Sergeant」的 `summonAudio` 原来被记在 `BL46` 名下；
        //     ⚠️ 另订正一处：原写「有任意 audio 引用却不在表里 = 35 张」，**实为 34 张**
        //     —— 多的那 1 张是 `BL47`，它唯一那条引用 `assetName` 是 **null**、没名字可对）
        //     ⇒ **卡 SO 才是权威**（判据：卡名逐条对得上）。
        //     气泡文字因此一律**按文件名反查**（`_textByFile`），不按卡 id 查。
        //  ⚠️ ~~**精确命中的音频可能不在工程里**（`import_original_audio.py` 按 `card_index.json` 的引用导；
        //     实测 **4 条** SO 引用不在 index 里）~~
        //     🔴 **2026-10-14 更正（`A1831`）**：那 4 条**已补进工程** —— 导入器现在**也按卡 SO 的引用面导**
        //     （`used_files` 先由它填，拷贝步自动带上）⇒ 现读「卡 SO 引用的音频缺件」= **0 条**。
        //     仍保留那两档兜底（`Clip()` 出声 + 退回后缀）—— 防将来再出现缺件时静默。
        // ==================================================================

        /// <summary>逐卡精确绑定的**事件名**（= `cards[].audio[].ev` 的取值）。
        /// 🔴 **翻译表只此一处** —— 生成侧 `工具/gen_card_vfx_by_card.py` 的 `AUDIO_SO_FIELD_EVENT`
        /// （`summonAudio → deploy` · `attackAudio → attack` · `deathAudio → death`）。</summary>
        public const string ExactDeploy = "deploy";   // ← 原版卡 SO `summonAudio`
        /// <inheritdoc cref="ExactDeploy"/>
        public const string ExactAttack = "attack";   // ← 原版卡 SO `attackAudio`
        /// <inheritdoc cref="ExactDeploy"/>
        public const string ExactDeath = "death";     // ← 原版卡 SO `deathAudio`
        /// <summary>精确档认的三个事件 —— **唯一一份**（自检遍历它，⛔ 别在别处再抄一遍）。</summary>
        public static readonly string[] ExactEvents = { ExactDeploy, ExactAttack, ExactDeath };

        // ---- 运行时那份 JSON 的形状（**数组**，`JsonUtility` 不支持字典）----
        //  ⚠️ **只声明本档用得上的字段**：`JsonUtility` 会忽略 DTO 里没有的键 ⇒
        //     同一个文件里 `cards[].events`（`VfxMap` 那份）在这儿**不会被读**，互不干扰。
        [System.Serializable] class RtAudio { public string ev; public string n; }
        [System.Serializable] class RtCard { public string id; public RtAudio[] audio; }
        [System.Serializable] class RtFile { public string[] confPriority; public RtCard[] cards; }

        static Dictionary<string, Dictionary<string, string>> _exactById;
        /// <summary>音频文件名（去扩展名）→ 台词原文 —— 专给**精确命中**那一条反查气泡文字用。</summary>
        static Dictionary<string, string> _textByFile;
        static bool _triedExact;

        /// <summary>精确表读进来了没有（读不出来时 <see cref="ExactError"/> 里是人话）。
        /// ⚠️ 与 <see cref="Ready"/> 同一条规矩：**自己先 `EnsureExactLoaded()`**，别答「没有」。</summary>
        public static bool ExactReady { get { EnsureExactLoaded(); return ExactEntries > 0; } }
        /// <summary>读失败的原因（**不静默**：自检会把它打出来）</summary>
        public static string ExactError { get; private set; }
        /// <summary>有精确绑定的卡数 / 条数（自检用）</summary>
        public static int ExactCards { get { EnsureExactLoaded(); return _exactById != null ? _exactById.Count : 0; } }
        public static int ExactEntries { get; private set; }
        /// <summary>所有有精确绑定的卡 id（自检用它遍历，⛔ 别自己去读 JSON 再算一遍）</summary>
        public static IEnumerable<string> ExactCardIds
        {
            get
            {
                EnsureExactLoaded();
                return _exactById != null ? (IEnumerable<string>)_exactById.Keys : new string[0];
            }
        }

        static void EnsureExactLoaded()
        {
            if (_triedExact) return;
            _triedExact = true;                    // 失败也只试一次（别每帧重试）

            // ⚠️ 资源路径**借用 `VfxMap` 的那一个常量** —— 同属 `CardPresentation`，
            //    在这里另写一份字符串就会变成「两处各记一次」（本仓红线）。
            var asset = Resources.Load<TextAsset>(VfxMap.CardTableResourcePath);
            if (asset == null)
            {
                ExactError = $"`Resources/{VfxMap.CardTableResourcePath}.json` 没读到 ⇒ 逐卡精确语音这一档"
                           + "**不起作用**（退回按后缀猜的那一套）。重跑："
                           + "\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_card_vfx_by_card.py";
                Debug.LogWarning("[VoiceLines] " + ExactError);
                return;
            }
            RtFile db;
            try { db = JsonUtility.FromJson<RtFile>(asset.text); }
            catch (System.Exception e)
            {
                ExactError = $"`Resources/{VfxMap.CardTableResourcePath}.json` 解析失败：{e.Message} ⇒ "
                           + "逐卡精确语音这一档不起作用（退回后缀兜底）";
                Debug.LogError("[VoiceLines] " + ExactError);
                return;
            }
            if (db == null || db.cards == null)
            {
                ExactError = $"`Resources/{VfxMap.CardTableResourcePath}.json` 里没有 `cards` ⇒ 逐卡精确语音不起作用";
                Debug.LogError("[VoiceLines] " + ExactError);
                return;
            }

            _exactById = new Dictionary<string, Dictionary<string, string>>();
            int n = 0;
            foreach (var c in db.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.id) || c.audio == null) continue;
                Dictionary<string, string> m;
                if (!_exactById.TryGetValue(c.id, out m))
                {
                    m = new Dictionary<string, string>();
                    _exactById[c.id] = m;
                }
                foreach (var a in c.audio)
                {
                    if (a == null || string.IsNullOrEmpty(a.ev) || string.IsNullOrEmpty(a.n)) continue;
                    m[a.ev] = a.n;                 // 同一 (卡, 事件) 生成侧已去重（取第一条）
                    n++;
                }
            }
            ExactEntries = n;

            // 气泡文字的反查表 —— 从**已经载入的**台词表建（同一次懒加载，不另读文件）
            EnsureLoaded();
            _textByFile = new Dictionary<string, string>();
            if (_byId != null)
                foreach (var kv in _byId)
                {
                    var c = kv.Value;
                    if (c == null || c.lines == null) continue;
                    foreach (var l in c.lines)
                    {
                        if (l == null || string.IsNullOrEmpty(l.file) || string.IsNullOrEmpty(l.text)) continue;
                        var k = System.IO.Path.GetFileNameWithoutExtension(l.file);
                        if (!_textByFile.ContainsKey(k)) _textByFile[k] = l.text;
                    }
                }

            Debug.Log($"[VoiceLines] 逐卡精确语音表已载入：{_exactById.Count} 张卡 / {n} 条"
                    + $"（`Resources/{VfxMap.CardTableResourcePath}.json` 的 `cards[].audio`）");
        }

        /// <summary>**精确档**：原版卡 SO 给这张卡的**这个事件**绑定的那一条音频。
        /// 返回 false = 这张卡在这个事件上**没有**精确绑定（⇒ 调用方退回后缀兜底）。
        /// <paramref name="text"/> 是气泡文字（按**文件名**反查台词表；查不到给 null，
        /// 调用方会印卡名 —— **不是静默**，见 `UnitChatPanel.Speak`）。</summary>
        public static bool TryPickExact(string cardId, string evt, out string clipName, out string text)
        {
            clipName = null; text = null;
            if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(evt)) return false;
            EnsureExactLoaded();
            if (_exactById == null) return false;
            Dictionary<string, string> m;
            if (!_exactById.TryGetValue(cardId, out m)) return false;
            if (!m.TryGetValue(evt, out clipName) || string.IsNullOrEmpty(clipName)) { clipName = null; return false; }
            if (_textByFile != null)
                _textByFile.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(clipName), out text);
            return true;
        }

        /// <summary>精确档的**事件名 → 后缀顺序**（= 兜底那一套）。给「精确 miss 之后退哪儿」用，
        /// 也给自检算「精确值 vs 后缀兜底值」用。认不出的事件返回 null。</summary>
        public static string[] SuffixOrderFor(string evt)
        {
            switch (evt)
            {
                case ExactDeploy: return ForDeploy;
                case ExactAttack: return ForAttack;
                case ExactDeath:  return ForDeath;
            }
            return null;
        }

        // ==================================================================
        //  事件 → 后缀（**这一段是我们定的**，原版没查到时机的对应关系）
        // ==================================================================

        /// <summary>部署（`EvtKind.Deploy`）：先找 `greet`，再 `intro`，最后退回那张卡的出场台词。</summary>
        public static readonly string[] ForDeploy = { "greet", "intro", "line" };
        /// <summary>攻击宣言（`EvtKind.Attack`）：`attack` → 出场台词。</summary>
        public static readonly string[] ForAttack = { "attack", "line" };
        /// <summary>阵亡（`EvtKind.Death`）：`death` → 出场台词。</summary>
        public static readonly string[] ForDeath = { "death", "line" };
        /// <summary>投降：`concede`（只有督军那一族有）。</summary>
        public static readonly string[] ForConcede = { "concede", "line" };

        // ==================================================================
        //  🔴 2026-09-18 新增三组 —— **这一批不再是「我们定的」，是原版实据**
        //  出处：`资料/语音线_原版规格与ASR管道.md` §一（全量反编译定案）
        // ==================================================================

        /// <summary>**开局独白**（原版 `ChatMessage.Intro` = 枚举 0；`RawCardScript.introChatSound`）。
        /// 原版：战斗开始、双方督军各说一次，**严格先手→后手串行**、各自等语音播完；教程/战役局**不播**。
        /// ⚠️ 只有督军有 `intro`（全池 54 条）⇒ 普通单位这里返回 false 是**对的**。</summary>
        public static readonly string[] ForIntro = { "intro" };

        /// <summary>**同督军对局的开场白**（原版 `ChatMessage.MirrorMatch` = 枚举 1）。
        /// 原版在 `AreSameWarlords()` 为真时用它**替换** intro；没有则回落普通 intro —— 所以顺序是 mirror → intro。</summary>
        public static readonly string[] ForMirror = { "mirror", "intro" };

        /// <summary>**玩家做了非法操作**（原版 `ChatMessage.ICantDoThat` = 枚举 3）。
        /// 原版链路：`BattleTipController.NotifyCantDoAction` → `DisplayLocalChatMessage(vlc, 3, skipCanChat=1)`，
        /// 由 `BattleManager` 里 **14 个「操作被拒」点**调用（打不出牌 / 技能用不了 / 用不了路标石 /
        /// 目标不合法 / 回合没走完就点结束 …）。闸门：**正在播语音时不打断**。
        /// ⚠️ **只有 `cantdo` 一条、没有回落** —— 原版取不到词条时落回 `defaultChatSound`（0x188，我们数据里没有），
        ///    回落成 `line` 会播「出场台词」，**那不是原版行为**。</summary>
        public static readonly string[] ForCantDo = { "cantdo" };

        /// <summary>**本回合时间不多了**（原版 `ChatMessage.Bored` = 枚举 2，词条名是 `hurry`）。
        /// 🔴 反编译一手链（`资料/全量反编译_入口与用法.md` 的查法）：
        /// · `ClockManager__Update.c:44-51`：`latch_0xb8==0 &amp;&amp; timeToHurryUp_0xb4 + elapsed > GetTotalTime()`
        ///   → 打日志、`latch=1`、`BattleManager.DisplayHurryUpChatMessage()`。
        ///   常量 `timeToHurryUp = 35.0`（`ClockManager__.ctor.c:11`，`0x420c0000`）。
        /// · **每回合一次**：`ClockManager__StartTimer.c:28` 把 `0xb8=0`；`StartTimer` 的唯一调用点
        ///   是 `BattleManager._NextTurn_d__395__MoveNext.c:426`（每次转手）。
        /// · 前置闸门（同函数 `:26-29`）：`IsPlayerTurn &amp;&amp; clockRunning`；`:32-34`：`UIstate==8` 时 return。
        /// · 播哪条：`DisplayHurryUpChatMessage` → `DisplayLocalChatMessage(vlc, 2, 0)` → `CanChat`
        ///   （**要过聊天闸门**、正在播时不打断）→ `GetHero(isPlayer: true)` ⇒ **说话人是我方督军**。
        /// ⚠️ **只有 `hurry` 一条、没有回落** —— 理由同 `ForCantDo`：原版取不到词条时落
        ///    `defaultChatSound`，回落成 `line` 会播「出场台词」，那不是原版行为。</summary>
        public static readonly string[] ForHurry = { "hurry" };

        /// <summary>**`ChatPopup` 那 6 个钮**（原版 `ChatMessage` 枚举 **5…10**）。
        /// 原版是**写死起点 5、然后按按钮下标 +1**（`VoiceLinesPopupSelector__SetupChatOptions.c:54,112`
        /// 与 `__RefreshChatLines.c:71` **两处独立证实**）—— **不是随机抽 6 条**。
        /// ⇒ 下标 0…5 依次对应：`Greet` / `Threat` / `WellPlayed` / `Taunt` / `Sorry` / `Oops`。</summary>
        public static readonly string[] ForChatButton =
            { "greet", "threat", "wp", "gen1", "gen2", "gen3" };

        /// <summary>🔴 **阵营级**的对手词 —— 对手**阵营**是 key 时该念的 token 集合。
        ///
        /// **值是集合，不是单值**：有 6 个 token **覆盖多个阵营** ——
        /// `vssm`/`vsspacemarine` 覆盖 **DarkAngels+SpaceWolves+Ultramarines**（都是 Space Marines）、
        /// `vscsm`/`vschaos` 覆盖 **BlackLegion+EmperorsChildren**（都是 Chaos Space Marines）、
        /// 另有合并键 **`vsec&amp;bl`**（两家同时）与 **`vsorkstyranids`**（Orks+Tyranids 两家）。
        /// ⇒ 老那套「一个 token 一个阵营」的模型**表达不了这些**。
        ///
        /// **父军团关系**（10 军团 → 13 个 `CardArmy`，**只有两个军团是多对一**）：
        /// `Space Marines` → DA/SW/UM 三个 · `Chaos Space Marines` → BL/EC 两个 · 其余 8 个 1:1。
        /// 出处：原版 **没有**父军团枚举（`grep` 全表只有 `CardArmy` 这一层，见 `dump.cs:45634`），
        /// 父军团是**资源包命名**给的：`aeldarisaimhanncardassets` / `chaosspacemarinesblacklegioncardassets` /
        /// `spacemarinesdarkangelscardassets` …（`d:/2/新解包资源/assets_full/`）。
        ///
        /// ⚠️ **名单刻意不全** —— 只收「**实据**」那 76 个里属于阵营级的；
        /// 判成「**推断**」与「**认不出**」的一律不进来（项目原则：宁可认不出，不可认错）。
        /// 认不出的 17 个（`vstbc1..4` = 原版自己的 **To Be Confirmed 占位符**、`vsdg`/`vsts`/`vswe`/`vsnl`/
        /// `vsdrukhari`/`vsmortarion` = **未实装军团**、`vsdaemon`/`vsimperial`/`vsknighttitan` = 泛称）
        /// 与完整依据见 `资料/语音线_原版规格与ASR管道.md` §1.5.1。</summary>
        static readonly Dictionary<string, string[]> VsFactionTokens = new Dictionary<string, string[]>
        {
            // ---- 父军团 1:1 的 8 个 ----
            { "SaimHann",       new[] { "vsaeldari" } },
            { "AstraMilitarum", new[] { "vsastramilitarum", "vsastramillitarum" } },  // 后者是**原版自己拼错**的（多一个 l）
            { "Sautekh",        new[] { "vsnecron", "vsnecrons" } },
            { "Goff",           new[] { "vsork", "vsorks", "vsorkstyranids" } },
            { "Sororitas",      new[] { "vssororitas" } },
            { "TauEmpire",      new[] { "vstau", "vskroot", "vskrootshaper" } },
            { "Genestealers",   new string[0] },                                     // 表里没有阵营级词（只有人名级）
            { "Leviathan",      new[] { "vstyranid", "vstyranids", "vsorkstyranids" } },
            // ---- 父军团**多对一**的两个：同一条 token 要出现在它的每个子阵营下 ----
            { "DarkAngels",       new[] { "vsda", "vsdarkangels", "vssm", "vsspacemarine" } },
            { "SpaceWolves",      new[] { "vssm", "vsspacemarine" } },
            { "Ultramarines",     new[] { "vsum", "vssm", "vsspacemarine" } },
            { "BlackLegion",      new[] { "vsbl", "vscsm", "vschaos", "vsec&bl" } },
            { "EmperorsChildren", new[] { "vsec", "vscsm", "vschaos", "vsec&bl" } },
        };

        /// <summary>**自检用**：阵营级 vs 表的只读视图（阵营 → token 列表）。
        /// 自检拿它断言「表里每个阵营名都合法、每个 token 都能在某张卡上找到」。</summary>
        public static IEnumerable<KeyValuePair<string, string[]>> VsFactionTable
        {
            get { return VsFactionTokens; }
        }

        /// <summary>**打特定对手时的开场白**（原版 `ChatManager.GetCustomIntro`）。
        /// 原版回落链：**先** `GetCustomIntro(己方, 对方督军)` → **再** `GetCustomIntroByArmy(己方, 对方阵营)`
        /// → 都失败退回普通 `intro`（本方法的返回顺序**就是照这条链排的**）。
        ///
        /// ⚠️ **不是「谁对谁说」** —— `vs&lt;X>` 里的 X **只说对手是谁**（2026-09-18 三条独立证据：
        /// 同一 token 出现在 9 个不同主讲下 · 同一对对手双向各录一条 · 236 个文件名逐条扫 0 反例）。
        ///
        /// ⚠️ **主讲也不只限督军** —— `Hound of Abaddon` / `Ghallaron's Champion` / `Acolyte Iconward` /
        /// `Celestian Sacresant Aveline` 这些**普通单位也在说 `vs*` 开场白**，因为 `customIntroChatData`
        /// 挂在 `RawCardScript`（**所有卡**）上。⇒ 别把这条链只接在督军身上。
        ///
        /// **人名级怎么匹配**：素材侧只取名字的**前段**（`vsursula` / `vscalgar` / `vsabaddon`），
        /// 还有撇号变体（`vsaun'va` = `vsaunva`）与截断写法（`vsuriel` = `vsurielventris`）⇒
        /// 用「**短词 ⊂ 对手全名**」+ **长度 ≥ 4 的护栏**（详见 `EvMatches`）。
        /// **阵营级**则**查 `VsFactionTokens` 表**，不做任何猜测。</summary>
        public static string[] ForVersus(string opponentWarlord, string opponentFaction)
        {
            var list = new List<string>(8);

            // ① **对手督军优先**（原版先查 `GetCustomIntro`，查不到才按阵营兜底）
            var d = VersNorm(opponentWarlord);
            if (!string.IsNullOrEmpty(d)) list.Add("vs~" + d);

            // ② 再按**对手阵营**兜底（查表；一个阵营可能有多条，也可能一条都没有）
            if (!string.IsNullOrEmpty(opponentFaction))
            {
                string[] toks;
                if (VsFactionTokens.TryGetValue(opponentFaction, out toks))
                    foreach (var t in toks)
                        if (!list.Contains(t)) list.Add(t);
            }

            // ③ 最后回落普通 `intro` —— **这就是原版回落链的最后一跳**，不是「我们兜底」
            if (!list.Contains("intro")) list.Add("intro");
            return list.ToArray();
        }

        /// <summary>`vs*` 的归一化：小写 → 只留字母数字 → **去掉尾部一个 `s`**（治 `vsorks`/`vstyranids`/`vstyranid`）。
        /// ⚠️ 光有它不够 —— 判定方向与长度护栏见 `EvMatches`。</summary>
        static string VersNorm(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var t = System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]", "");
            return t.EndsWith("s") ? t.Substring(0, t.Length - 1) : t;
        }

        /// <summary>`vs~&lt;对手词>` 这种**虚拟 ev** 的判定。
        ///
        /// 🔴 **方向与护栏都不是随手写的 —— 实测出来的**（99 个 `vs*` 条目上量过）：
        ///   · **方向**：只做「**素材侧的短词 ⊂ 对手的全名**」（`vscalgar` → `calgar` ⊂ `marneuscalgar`）。
        ///     **反向绝对不做**（`marneucalgar` ⊄ `calgar`）—— 反了会一条都匹配不上。
        ///   · **护栏 ≥4 字符**：**短词是危险的那一侧** —— 不设护栏的话 `vsda` → `da` 会同时命中
        ///     `DarkAngels` 与 `Ael**da**ri` ⇒ **播错语音**。按「宁可认不出，不可认错」，短于 4 的一律不认。
        ///   · 命中率：**57/99（只对卡名）/ 59/99（卡名∪阵营，带护栏）** —— 剩下的**推不出**，见下。
        ///
        /// 🔴 **「阵营级」的那批不走这里** —— 它们**查 `VsFactionTokens` 表**（精确匹配，不做猜测）。
        ///   本函数只管「**人名级**」的 `vs~&lt;对手名>`。
        ///
        /// 🔴 **2026-09-18 更正（原写「两套词汇」是错的）**：
        ///   原来这里写「`vs` 用军团名、我们的 `faction` 是子阵营名，两套词汇对不上」——
        ///   **不成立**。实测原版**只有 `CardArmy` 这一个枚举**（`dump.cs:45634`），
        ///   **我们的 13 个 `faction` 就是 `CardArmy` 本身**；所谓「父军团」是**资源包命名**给的
        ///   （`aeldarisaimhanncardassets` / `spacemarinesdarkangelscardassets` …），不在一层枚举里。
        ///   ⇒ 所以**没有「词汇对不上」这回事**，只是**阵营级词要查表**、人名级词要按包含比。
        ///
        /// **人名级为什么能靠包含比**：素材侧只取名字前段（`vsursula`←`Ursula Creed`、
        ///   `vscalgar`←`Marneus Calgar`），还有撇号变体（`vsaun'va` = `vsaunva`）与截断写法
        ///   （`vsuriel` = `vsurielventris`）⇒ 用「短词 ⊂ 对手全名」正好覆盖这三种。
        /// **匹配不上就回落 `intro` —— 那正是原版的回落行为**，不是静默失败。
        /// 完整名单与量法见 `资料/语音线_原版规格与ASR管道.md` §1.5.1。</summary>
        static bool EvMatches(string realEv, string virtEv)
        {
            if (realEv == null || virtEv == null) return false;
            if (!virtEv.StartsWith("vs~")) return realEv == virtEv;
            var want = virtEv.Substring(3);
            if (!realEv.StartsWith("vs")) return false;
            var have = VersNorm(realEv.Substring(2));
            // 短路顺序：先看护栏（短词一律不认），再比包含
            return have != null && have.Length >= VsMinLen && want.Length > 0 && want.Contains(have);
        }

        /// <summary>`vs*` 短词的**最小可认长度** —— 见 `EvMatches` 的说明（短词会误匹配）。</summary>
        const int VsMinLen = 4;

        /// <summary>
        /// **放大窗那个语音按钮**（原版 `voiceOverButton`）用：这张卡**随便哪一条**都行 ——
        /// 它是「听一下这张卡的声音」，不属于任何一个战斗事件。
        /// 顺序按「出场 → 攻击 → 阵亡 → 投降 → 拒绝」，取第一条有的。
        /// ⚠️ 原来这里直接借用 `ForDeploy`（`greet`/`intro`/`line`），实测**会挂空**：
        ///    有的卡只有 `attack` / `death`（没有 greet/intro/line）⇒ 自检里那条断言变红。
        /// </summary>
        public static readonly string[] ForVoiceOver = { "greet", "intro", "line", "attack", "death", "concede", "cant" };

        /// <summary>
        /// 按**优先级顺序**挑一条：第一个命中的后缀赢。返回 false = 这张卡一条语音都没有。
        /// <paramref name="rng"/> 用于在**同一后缀有多条**时挑一条（原版督军有 `gen1..7` 这种台词池）；
        /// 传 null 就取第一条（定死，便于自检）。
        /// </summary>
        public static bool TryPick(string cardId, string[] order, System.Random rng,
                                   out string clipName, out string text)
        {
            string ev;
            return TryPick(cardId, order, rng, out clipName, out text, out ev);
        }

        /// <summary>同上，**额外把命中的那个 `ev` 交出来**。
        /// 🔴 **为什么要它**：`out clipName` 给的是**文件名**（`VO_AM_Ursula Creed_vsTyranids.ogg`），
        /// **不是 ev** —— 想断言「命中的到底是 `vs*` 还是回落的 `intro`」时，光看文件名会判错
        /// （2026-09-18 自检里就这么**假绿**过一次：我拿文件名判 `StartsWith("vs")`，恒假）。
        /// 判「有没有命中 `vs` 族」**必须用这个重载拿 ev**。</summary>
        public static bool TryPick(string cardId, string[] order, System.Random rng,
                                   out string clipName, out string text, out string ev)
        {
            clipName = null; text = null; ev = null;
            EnsureLoaded();
            if (_byId == null || cardId == null) return false;
            Card c;
            if (!_byId.TryGetValue(cardId, out c) || c.lines == null) return false;

            foreach (var want in order)
            {
                // 同一后缀可能有几条（台词池）—— 收集后按 rng 挑
                // ⚠️ 用 `EvMatches` 而不是 `==`：`vs~<对手词>` 这种**虚拟 ev** 要按包含比对
                //    （真 ev 是 `vstyranids` 这类，见 `ForVersus` 的说明）。
                int hits = 0;
                for (int i = 0; i < c.lines.Count; i++)
                    if (c.lines[i] != null && EvMatches(c.lines[i].ev, want)) hits++;
                if (hits == 0) continue;

                int pick = 0;
                if (rng != null && hits > 1) pick = rng.Next(0, hits);
                int seen = 0;
                for (int i = 0; i < c.lines.Count; i++)
                {
                    var l = c.lines[i];
                    if (l == null || !EvMatches(l.ev, want)) continue;
                    if (seen++ != pick) continue;
                    clipName = l.file;
                    text = l.text;
                    ev = l.ev;                 // ← 真 ev（虚拟 ev 不交出去，交出去的是那条真行自己的 ev）
                    return true;
                }
            }
            return false;
        }

        /// <summary>取音频（**没有就返回 null** —— 调用方据此不播、也不显示气泡）。</summary>
        public static AudioClip Clip(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string noExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var clip = Resources.Load<AudioClip>(ClipRoot + noExt);
            if (clip == null && !_missingLogged.Contains(noExt))
            {
                _missingLogged.Add(noExt);
                // 表里有、音频却不在 = **资源没导全**，要说出来（红线）
                Debug.LogWarning($"[VoiceLines] 表里引用了 `{fileName}`，但 `Resources/{ClipRoot}{noExt}` 没找到");
            }
            return clip;
        }
        static readonly HashSet<string> _missingLogged = new HashSet<string>();
    }
}
