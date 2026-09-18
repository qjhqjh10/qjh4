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
            clipName = null; text = null;
            EnsureLoaded();
            if (_byId == null || cardId == null) return false;
            Card c;
            if (!_byId.TryGetValue(cardId, out c) || c.lines == null) return false;

            foreach (var want in order)
            {
                // 同一后缀可能有几条（台词池）—— 收集后按 rng 挑
                int hits = 0;
                for (int i = 0; i < c.lines.Count; i++)
                    if (c.lines[i] != null && c.lines[i].ev == want) hits++;
                if (hits == 0) continue;

                int pick = 0;
                if (rng != null && hits > 1) pick = rng.Next(0, hits);
                int seen = 0;
                for (int i = 0; i < c.lines.Count; i++)
                {
                    var l = c.lines[i];
                    if (l == null || l.ev != want) continue;
                    if (seen++ != pick) continue;
                    clipName = l.file;
                    text = l.text;
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
