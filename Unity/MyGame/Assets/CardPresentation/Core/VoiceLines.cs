// VoiceLines.cs — 原版**单位语音**的运行时索引（哪张卡、哪个事件、放哪条音频、说什么）
//
// 数据从哪来：
//   · `Resources/voice_lines.json`（**进仓库**的分析产物）—— 由 `工具/import_original_audio.py`
//     从 `d:/4/Unity/数据/索引/card_index.json` 的 `cards[].voice` + 音频源清点合并而成；
//   · 音频本体在 `Resources/Art/audio/vo/`（**被 .gitignore 排除**，和美术同一条规矩：
//     原版资产不进仓库；重跑那个脚本就能再生）。
//
// ⚠️ **能查到的与查不到的**（`资料/战斗UI_原版对账表.md` §三·〇 :127）：
//   · 查得到：**原版有哪些语音、哪条属于哪张卡**（`card_index.json` 的权威映射，
//     595 张卡 / 1787 条，实测**一条不差**都找得到音频文件）；
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
        /// 另有合并键 **`vsec&bl`**（两家同时）与 **`vsorkstyranids`**（Orks+Tyranids 两家）。
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
        /// ⚠️ **不是「谁对谁说」** —— `vs<X>` 里的 X **只说对手是谁**（2026-09-18 三条独立证据：
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

        /// <summary>`vs~<对手词>` 这种**虚拟 ev** 的判定。
        ///
        /// 🔴 **方向与护栏都不是随手写的 —— 实测出来的**（99 个 `vs*` 条目上量过）：
        ///   · **方向**：只做「**素材侧的短词 ⊂ 对手的全名**」（`vscalgar` → `calgar` ⊂ `marneuscalgar`）。
        ///     **反向绝对不做**（`marneucalgar` ⊄ `calgar`）—— 反了会一条都匹配不上。
        ///   · **护栏 ≥4 字符**：**短词是危险的那一侧** —— 不设护栏的话 `vsda` → `da` 会同时命中
        ///     `DarkAngels` 与 `Ael**da**ri` ⇒ **播错语音**。按「宁可认不出，不可认错」，短于 4 的一律不认。
        ///   · 命中率：**57/99（只对卡名）/ 59/99（卡名∪阵营，带护栏）** —— 剩下的**推不出**，见下。
        ///
        /// 🔴 **「阵营级」的那批不走这里** —— 它们**查 `VsFactionTokens` 表**（精确匹配，不做猜测）。
        ///   本函数只管「**人名级**」的 `vs~<对手名>`。
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
