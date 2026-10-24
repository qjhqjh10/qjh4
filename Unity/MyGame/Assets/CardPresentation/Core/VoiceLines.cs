// VoiceLines.cs — 原版**单位语音**的运行时索引（哪张卡、哪个事件、放哪条音频、说什么）
//
// 数据从哪来：
//   · `Resources/voice_lines.json`（**进仓库**的分析产物）—— 由 `工具/import_original_audio.py` 生成。
//     🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1831`/`A1832`）**：原来这里写「从 `card_index.json` 的
//     `cards[].voice` + 音频源清点合并而成」—— **归属口径换了**：现在**以卡 SO 的
//     `summonAudio`/`attackAudio`/`deathAudio` 为权威**（582 条），`card_index.json` 的 `cards[].voice`
//     **降为兜底**（1208 条），名字对不上的仍按名猜但**标 `src:"orphan"`**（58 条；**9 条**四种来源都认不出）。🔴 **2026-10-24 订正（铁律 5 · `D7` 现核）：`src="orphan"` 现读 246 条**，含 D7 新标的两类（19 条 `_vs_` 改 `intro-vs` + 203 条 `gen4..gen7` 死通道）；58 是更早的口径。
//     ⚠️ 根因 = **`card_index.json` 它自己错了**（`d:/2/解包整理/` 那份 2026-08-15 的产物，那次卡 SO 还没解出来，
//     它靠名字/目录对、对不上就**并到邻近那张卡**）⇒ 它在 `d:/2` **没改**，只降级。
//     现读数与逐条对照 → `资料/普查产出_第二十八会话/W7_语音导入漏件与归属.md`。
//   · 音频本体在 `Resources/Art/audio/vo/`（**被 .gitignore 排除**，和美术同一条规矩：
//     原版资产不进仓库；重跑那个脚本就能再生）。
//
// ⚠️ **能查到的与查不到的**（`资料/战斗UI_原版对账表.md` §三·〇 :127）：
//   · 查得到：**原版有哪些语音、哪条属于哪张卡**（**卡 SO 是权威**；现读 **640 张卡** ——
//     ~~595 张卡 / 1787 条~~ 是 2026-10-14 归属订正**之前**的读数，⛔ 别再用）；
//   · 查得到：**原版在什么时机播哪一条**。
//     🔴 **2026-10-16 就地订正（铁律 5 · 账 `A1768`）**：这里原来写
//     「**查不到：原版在什么时机播哪一条**……下面 `ForDeploy` 等**事件→后缀的对应是我们定的**
//     （按字面意思），**不是原版规格**」—— **不成立**：原版**15 个字段逐卡绑定了这件事**，
//     而且每个字段的消费点都在反编译里找得到（下表）。当时的错因 = 只看了文件名后缀、
//     没去读卡 SO 的字段与它的消费点。
//
// | 我们的事件 | 原版字段 | 消费点（`d:/2/tools/decomp_full/`） |
// |---|---|---|
// | `deploy` | `summonAudio`（`dump.cs:23270` @`0xB8`） | `VoiceLinesController__DisplaySummonMessage.c:31` → `RawCardScript__GetSummonSound` |
// | `attack` | `attackAudio`（`:23271` @`0xC0`） | `CardScript._AttackMeleeAnim_d__357__MoveNext.c:76` → `GetAttackSound` → `BattleManager__PlaySoundClip` |
// | `death`  | `deathAudio`（`:23272` @`0xC8`） | `CardScript._UnitDeath_d__446__MoveNext.c:82` → `GetDeathSound` → `PlaySoundClip` |
// | `intro` `mirror` `hurry` `cantdo` `concede` `greet` `threat` `wp` `gen1` `gen2` `gen3` |
// | **11 个** `*ChatSound`（`dump.cs:23301-23315` @`0x190`…`0x1E0`） | `RawCardScript__GetChatSound.c` 的 `switch`（枚举号 → 偏移，**只有 0..10**） |
// | `default` | `defaultChatSound`（`dump.cs:23300` @`0x188`）—— **不在 `switch` 里**，是它**末尾那一跳的兜底** |
//
//   🔴 **2026-10-24（B4 · 铁律 5）**：上面那两行原来写成「`default` 也是 `*ChatSound` 之一 · 12 个」——
//     **数错了**：`ChatMessage` 枚举只有 **11** 个值（`ChatMessage.cs:1-14`），`switch` 也就 11 个 case；
//     `defaultChatSound` 是**单独的兜底字段**（`+0x188`），不参与 `switch`。
//     ⇒ 事件名一共 **12** 个没错，但它们是「11 个 case + 1 个兜底」，不是「12 个同族字段」。
//
//   ⇒ **三条硬结论**（都写进了下面那些 `ForXxx` 的注释里）：
//   ① **战斗那三族没有回落** —— 三个消费点都是 `if (sound != null) 播`，取不到就**什么也不播**；
//   ② **聊天那一族有唯一一个回落 = `defaultChatSound`**（`GetChatSound` 末尾那一跳），
//      ⛔ **不是**我们以前写的 `line`；
//   ③ **`line` 这个我们自己造的名字退场**，改叫 `deploy`（= `summonAudio` 在生成侧的译名；
//      判据：`summonAudio` 指到的 clip 实测 **448/536** 就是「`VO_<阵营>_<卡名> - <台词>`」那种命名）。
//
//  🔴 **2026-10-24（B4 · 铁律 5 就地订正）**：`资料/普查产出_1024/B2_语音归属换真值SO.md` §八·1 写过
//     「`GetChatSound` 的 `default:` 分支只 `LogWarning` 后**返回空**、**不读** `defaultChatSound(+0x188)`」
//     —— **不成立，⛔ 别照它改**（下面各表的 `"default"` 第二跳是**对的**）。判据 = **指令流**：
//       `D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x180946C80`
//       （`RawCardScript$$GetChatSound`，RVA `0x946C80`）
//       · `0x180946D08 cmp esi,0xa / ja 0x180946DA7` ⇒ `default:` 那一支；
//       · `default:` 支在 `0x180946DE3 call CustomDebug.LogWarning` 之后**没有 `jmp`** ——
//         下一条就落在 `0x180946DE8`，也就是**各 case 支共用的那条尾巴**
//         （`.c` 里那句 `goto LAB_180946de8` 就是它；Ghidra 把这个标签画在了 `if` 块体内，容易被误读成「跳出去」）；
//       · 那条尾巴：`op_Inequality(clip,0)` 为假 ⇒ `0x180946E0C je 0x180946E47` ⇒
//         `0x180946E47 mov rcx,[rbx+0x188]` ⇒ `test/je` ⇒ **尾调 `AssetReferenceTyped.Load`**；
//       · `+0x188` = `defaultChatSound`（`d:/2/tools/il2cpp_out/dump.cs` 的字段偏移表）。
//     ⇒ **`defaultChatSound` 是原版真在用的兜底**，有两条路进去：
//       ① 枚举越界（`default:`）；② **更常用的那条** —— 某个 `*ChatSound` 字段存在、但 `Load()` 取不到 clip
//       时，各 case 支是 `0x180946D48 jmp 0x180946DE8`，**跟 `default:` 汇到同一处**。
//       ⚠️ case 支里那个 `if (字段指针 != 0)` **不是**「取不到就返回空」，那是 il2cpp 给
//       `callvirt` 补的空检查（失败直接抛）；Unity 把这类引用序列化成**非空但 GUID 为空**的对象，所以它不触发。
//     📌 **结论**：`ForIntro / ForMirror / ForCantDo / ForHurry / ForConcede / ForVersus / ForVoiceOver`
//       里的 `"default"` 第二跳**保持不动**；`voice_lines.json` 里 56 条 `ev="default"` 也**保留**。
//
//  🔴 **2026-10-24（B4）同 ev 多行的取值口径**：同一张卡上同一个 `ev` 可能有多行
//     （生成器的三种归属来源撞在一起），其中**只有一行是卡 SO 指的**，其余是按文件名猜的。
//     ⇒ `TryPick` **优先取「卡 SO 指名」的那一条**（`so` 非空，或 `src == "cardSO"`），
//     **仍然多解才随机**；把 SO 没指名的那些压掉时会打一行日志（不静默）。
//     判据实例（现读 `voice_lines.json`）：`ASH2` 的 `gen1`（SO→`_gen4.ogg`，另有 `_gen1.ogg`）·
//     `ASH2` 的 `intro`（SO→`_gen5.ogg`，另有 `_intro.ogg`）· `UM88` 的 `gen3`（SO→`_gen4.wav`，另有 `_gen3.wav`）。
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

        //  ⚠️ `src` / `so` 是生成器 `工具/import_original_audio.py` 写的**归属留痕**：
        //     `src` = `cardSO` / `orphan` / 空（空 = 二级/三级来源）；
        //     `so`  = 原版卡 SO 上**哪个字段**指的这条（`summonAudio` / `introChatSound` / `defaultChatSound` …），
        //             推不出（同一 clip 被两个字段共指）时为空。
        //     🔴 **2026-10-24（B4）起它们不只是留痕** —— `TryPick` 用它们把
        //     「**卡 SO 指名的那一条**」与「按文件名猜出来的那一条」分开（见 `TryPick` 的注释与文件头）。
        [System.Serializable]
        class Line
        {
            public string ev; public string file; public string text;
            public string src; public string so;
        }
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

        // ==================================================================
        //  🆕 2026-10-24（`E2` · `DG2` 红 #4）：**精确表注入口（自检专用）**
        //  为什么需要它：「精确档 ≠ 后缀兜底的复制品」那条灭自证断言，原来靠 `exOnly > 0`
        //  （兜底**结构上给不出**、只有精确档有）的**数据巧合**当见证者。本批 `D10(C4)` 把数据补齐
        //  （`EC57/deploy` 那唯一一条补上）之后，`exDiff + exOnly` **结构性恒 0** ⇒ 断言恒红，
        //  而且**没有任何真实数据能再当见证者**（精确档的值本来就来自同一个 SO 字段 ⇒ 两边都有时必然同一条）。
        //  ⇒ 改成**夹具级见证者**：自检注入一条「精确表有、兜底表给不出」的合成绑定，
        //     再断「`TryPickExact` 拿得到它」+「**还原之后**拿不到」。
        //    🧨 把精确档改回「照抄后缀兜底」/ 改成不看表直接回一条 ⇒ 合成 id 在 `voice_lines.json` 里
        //       根本不存在 ⇒ 注入值取不到 ⇒ 红；而「还原后必须取不到」挡的是「把表写死」那一类。
        //  ⚠️ **生产路径零变化**：`SetExactForTest` 全仓只有 `Editor/BattleScene.cs` 调。
        //  ⚠️ 形状照 `VfxMap.LoadForTest`（`F2` · R1 P2-8 的教训）：**注入与还原成对**，
        //     `using` 出作用域自动还原（抛异常也还原），并把「还回去了没有」做成**可断言**的口子。
        // ==================================================================

        /// <summary>自检用：<see cref="SetExactForTest"/> 返回的**还原作用域**。
        /// `using` 出作用域 ⇒ 把注入的那一条**还原成注入之前的样子**（原来同一 (卡, 事件) 有值 ⇒ 装回旧值；
        /// 原来没有 ⇒ 删掉，卡空了连卡一起删）。<see cref="Dispose"/> 幂等；抛异常也还原。</summary>
        public sealed class ExactInjectionScopeForTest : System.IDisposable
        {
            internal string CardId, Ev, PrevClip;
            internal bool HadPrev, Injected;
            bool _disposed;

            internal ExactInjectionScopeForTest() { }

            /// <summary>真的注入了没有（`false` = 精确表没载入 ⇒ 自检**应当据此报红**，⛔ 别当成通过）。</summary>
            public bool WasInjected { get { return Injected; } }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (!Injected || _exactById == null) return;
                Dictionary<string, string> m;
                if (!_exactById.TryGetValue(CardId, out m)) return;   // 表已被别的口清掉 ⇒ 不用管
                if (HadPrev) m[Ev] = PrevClip;
                else
                {
                    m.Remove(Ev);
                    if (m.Count == 0) _exactById.Remove(CardId);
                }
            }
        }

        /// <summary>🆕 2026-10-24（`E2` · `DG2` 红 #4）：**往精确表里塞一条合成绑定**（自检专用）。
        /// 只改内存表 `_exactById`（**不动** `Resources/card_vfx_by_card.json`、**不动** `ExactEntries` 计数 ——
        /// 那个计数是「载入时读了多少条」的读数，不是表长）。
        /// 推荐 `using (VoiceLines.SetExactForTest(id, ev, clip)) { … }` ⇒ 出作用域自动还原。
        /// ⚠️ 精确表没载入（见 <see cref="ExactError"/>）时**注入不了**：返回作用域的
        /// <see cref="ExactInjectionScopeForTest.WasInjected"/> = `false` —— 调用方要据此**报红**，⛔ 别默默当通过。</summary>
        public static ExactInjectionScopeForTest SetExactForTest(string cardId, string ev, string clip)
        {
            var scope = new ExactInjectionScopeForTest { CardId = cardId, Ev = ev };
            EnsureExactLoaded();
            if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(ev))
            {
                Debug.LogError("[VoiceLines][SetExactForTest] 卡 id / 事件名不能为空 ⇒ 未注入。");
                return scope;
            }
            if (_exactById == null)
            {
                Debug.LogError("[VoiceLines][SetExactForTest] 精确表没载入 ⇒ 未注入"
                             + (ExactError == null ? "。" : "（" + ExactError + "）"));
                return scope;
            }
            Dictionary<string, string> m;
            if (!_exactById.TryGetValue(cardId, out m))
            {
                m = new Dictionary<string, string>();
                _exactById[cardId] = m;
            }
            string prev;
            scope.HadPrev = m.TryGetValue(ev, out prev);
            scope.PrevClip = prev;
            m[ev] = clip;
            scope.Injected = true;
            return scope;
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
        //  事件 → 后缀顺序 —— 🔴 **2026-10-16 起是原版真值，不再是我们按字面意思定的**
        //  （账 `A1768`；字段 ↔ 消费点的对照表见文件头）
        // ==================================================================

        /// <summary>部署（`EvtKind.Deploy`）→ 原版卡 SO 的 **`summonAudio`**。
        ///
        /// 🔴 **原版没有回落**：`VoiceLinesController__DisplaySummonMessage.c:31` 拿
        /// `RawCardScript__GetSummonSound()`，取不到就**整条消息都不发**（气泡 + 语音一起没有）。
        /// ⛔ **以前这里是 `{ greet, intro, line }` —— 错两处**：
        ///  · `greet` / `intro` 是**聊天族**（`greetChatSound` / `introChatSound`），
        ///    原版**从来不在部署时念它们**；
        ///  · 于是**督军**（它们**没有** `summonAudio` —— 实测 59 位督军该字段全空）一上场就念
        ///    问候语，而原版督军登场**不响**。
        /// 判据落点：`资料/普查产出_第二十八会话/战斗事件层_现核.md` §A1817（记了 9 张同时有
        /// `greet`+`line` 的卡被 `greet` 抢先）。</summary>
        public static readonly string[] ForDeploy = { "deploy" };
        /// <summary>攻击宣言（`EvtKind.Attack`）→ 原版卡 SO 的 **`attackAudio`**。
        /// 消费点 = `CardScript._AttackMeleeAnim_d__357__MoveNext.c:76-83`（**只有近战那一支**），
        /// 取不到**不播**、也**不回落**出场台词。
        /// ⇒ 只有 59 张卡有这个字段（≈ 督军那一族），**普通单位挨打攻击时不说话是原版行为**。</summary>
        public static readonly string[] ForAttack = { "attack" };
        /// <summary>阵亡（`EvtKind.Death`）→ 原版卡 SO 的 **`deathAudio`**。
        /// 消费点 = `CardScript._UnitDeath_d__446__MoveNext.c:82-97`，同样**没有回落**。
        /// ⚠️ `deathAudio` 是**语音**（`VO_*`）；原版的**阵亡音效**（`minionDeathCUE` 那一族 SFX）
        /// 是**另一件活**，⛔ 别混。</summary>
        public static readonly string[] ForDeath = { "death" };
        /// <summary>投降 → 原版卡 SO 的 **`concedeChatSound`**，取不到落 **`defaultChatSound`**
        /// （`RawCardScript__GetChatSound.c` 的 `case 4` → `+0x1A0`；末尾那一跳 → `+0x188`）。
        /// ⛔ 原来回落到 `line`（出场台词）**不是原版行为**。</summary>
        public static readonly string[] ForConcede = { "concede", "default" };

        // ==================================================================
        //  🔴 2026-09-18 新增三组 —— **这一批不再是「我们定的」，是原版实据**
        //  出处：`资料/语音线_原版规格与ASR管道.md` §一（全量反编译定案）
        // ==================================================================

        /// <summary>**开局独白**（原版 `ChatMessage.Intro` = 枚举 0；`RawCardScript.introChatSound`）。
        /// 原版：战斗开始、双方督军各说一次，**严格先手→后手串行**、各自等语音播完；教程/战役局**不播**。
        /// ⚠️ 只有督军有 `intro`（全池 57 条）⇒ 普通单位这里返回 false 是**对的**。
        /// 🔴 **2026-10-16（`A1768`）补上第二跳 `default`**：`RawCardScript__GetChatSound.c`
        /// 取不到词条时落 `defaultChatSound`（`+0x188`）—— 那一族**现在进表了**
        /// （`voice_lines.json` 的 `ev="default"`，实测 49 条，与 `attackAudio` **同 GUID**）。
        /// ⛔ 不再落 `line`（出场台词），那不是原版行为。</summary>
        public static readonly string[] ForIntro = { "intro", "default" };

        /// <summary>**同督军对局的开场白**（原版 `ChatMessage.MirrorMatch` = 枚举 1）。
        /// 原版：`ChatManager__GetChatSoundAsset(0, isPlayer)` 里
        /// `AreSameWarlords() &amp;&amp; !isPlayer` ⇒ 改取 `GetChatSound(1)`（= `mirrorChatSound`）。
        /// 🔴 **2026-10-16（`A1768`）就地订正**：这里原来是 `{ mirror, intro }` —— **第二跳错了**。
        /// `GetChatSound` 自己的回落是 **`defaultChatSound`**，⛔ 不是 `intro`
        /// （`intro` 只是**上位**那条 `ChatMessage.Intro` 链的最后一跳，由 `ForVersus` / `ForIntro` 负责）。</summary>
        public static readonly string[] ForMirror = { "mirror", "default" };

        /// <summary>**玩家做了非法操作**（原版 `ChatMessage.ICantDoThat` = 枚举 3）。
        /// 原版链路：`BattleTipController.NotifyCantDoAction` → `DisplayLocalChatMessage(vlc, 3, skipCanChat=1)`，
        /// 由 `BattleManager` 里 **14 个「操作被拒」点**调用（打不出牌 / 技能用不了 / 用不了路标石 /
        /// 目标不合法 / 回合没走完就点结束 …）。闸门：**正在播语音时不打断**。
        /// 🔴 **2026-10-16（`A1768`）**：第二跳 `default` 是**原版自己的**（`GetChatSound(3)` →
        /// `iCantDoThatChatSound` @`0x1B0`，取不到落 `defaultChatSound` @`0x188`）；以前这里写成
        /// 「只有 `cantdo` 一条」是因为**那时 `default` 那一族没进表**，现在进表了。</summary>
        public static readonly string[] ForCantDo = { "cantdo", "default" };

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
        /// 🔴 **2026-10-16（`A1768`）**：`GetChatSound(2)` → `hurryChatSound` @`0x1A8`（原名
        /// `boredChatSound`），取不到落 `defaultChatSound` —— 第二跳**就是原版行为**，理由同 `ForCantDo`。</summary>
        public static readonly string[] ForHurry = { "hurry", "default" };

        /// <summary>**`ChatPopup` 那 6 个钮**（原版 `ChatMessage` 枚举 **5…10**）。
        /// 原版是**写死起点 5、然后按按钮下标 +1**（`VoiceLinesPopupSelector__SetupChatOptions.c:54,112`
        /// 与 `__RefreshChatLines.c:71` **两处独立证实**）—— **不是随机抽 6 条**。
        /// ⇒ 下标 0…5 依次对应：`Greet` / `Threat` / `WellPlayed` / `Taunt` / `Sorry` / `Oops`。
        /// ⚠️ **原版这 6 个也各有 `defaultChatSound` 那一跳**，但本表是**单值数组**（一个钮一条）
        /// ⇒ 第二跳**放不进这张表**，只能留在**消费点**。
        /// 🔴 **2026-10-16（修波 `FD` · 账 `G1-3`）就地订正（铁律 5）**：本行原来接着写
        /// **「消费点 `BattleDriver.cs` 用的是 `ForChatButton[idx]` 单元素 ⇒ 这里放不下；要补得先改那个
        /// 消费点」** —— **后半句【已过期】**：那个消费点**收口波（`G1`）已经改了**（现读
        /// `BattleDriver.cs:4202` = `TryPick(w.Card.Id, new[] { VoiceLines.ForChatButton[idx], "default" }, …)`）
        /// ⇒ **第二跳今天已经生效**，本表**保持单值不变**（`"default"` 那个字面量在消费点这一处）。
        /// 📌 更干净的收法（`A8` 建议、**未做**）：给本类加一个「返回该钮整条链」的口，那时消费点改回引用它，
        /// ⛔ 别在第三处再写一遍 `"default"`。</summary>
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
        /// → 都失败退回普通 `intro`。
        ///
        /// 🔴 **2026-10-24（`D9` · 铁律 5）就地订正**：这一行原来接着写
        /// 「本方法的返回顺序**就是照这条链排的**」—— **不成立（那时只排对了一半）**：
        /// 前两跳原来排的是**按文件名猜的** `vs~<对手名>` / 阵营词（`voice_lines.json` 的 `vs*` 行），
        /// 而原版前两跳的真身是**卡 SO 的 `customIntroChatData`**（今天由 `CardChatIntro.TryResolve`
        /// 在 `BattleDriver.SpeakIntro` 里**先**查）。⇒ **猜的压过真的**，实测会播错语音。
        /// 现在的顺序见方法体：**`intro` → `default` →（我们猜的）`vs~` → 阵营词**。
        ///
        /// ⚠️ **不是「谁对谁说」** —— `vs&lt;X&gt;` 里的 X **只说对手是谁**（2026-09-18 三条独立证据：
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
            // ① **卡 SO 的兜底档先走**：`intro` → `default`。
            //    原版 `ChatManager__GetChatSoundAsset` 的链是
            //    「`customIntro` → `customIntroByArmy` → `GetChatSound(0)`」；
            //    前两跳（有卡 SO 数据的那两档）已经由 `CardChatIntro.TryResolve` 在
            //    `BattleDriver.SpeakIntro` **先查过了** ⇒ 走到这里只剩 `GetChatSound(0)` 那一跳
            //    = `introChatSound`，再取不到才落 `GetChatSound` 自己内部的 `defaultChatSound`
            //    （🔴 2026-10-16 · `A1768`：`RawCardScript__GetChatSound.c` 取不到词条时落 `+0x188`）。
            var list = new List<string>(8) { "intro", "default" };

            // ② 🔴 **之后才是我们自己按文件名猜的 `vs~` / 阵营词 —— 必须排在最后**（见 `ForVersusGuess`）。
            //    ⛔ **不许把这一族整条删掉** —— 没有卡 SO 数据的卡（`BL5` / `BL67`：
            //    `introChatSound` 的 GUID 本身就是空的、表里也没有 `intro`/`default` 行）
            //    全靠它才不至于「完全没开场白」。真走到它时 `WarnFilenameVsFallback` 会出声一次（不静默）。
            foreach (var t in ForVersusGuess(opponentWarlord, opponentFaction))
                if (!list.Contains(t)) list.Add(t);
            return list.ToArray();
        }

        /// <summary>`vs~<paramref name="opponentWarlord"/>` + 对手**阵营**级词 ——
        /// **按文件名猜的那几跳**，单独拿出来只给「**已经确定播的是定制开场白**」那一路**取台词文字**用
        /// （`BattleDriver.SpeakIntro` 的 `wantCustom` 支：那时放的是卡 SO 的对手台词音，
        /// 文字就该配同一条对手台词，⛔ 不该落成普通 `intro` 的英文）。
        /// ⛔ **别拿它当播放顺序** —— 播放顺序用 <see cref="ForVersus"/>（它把这族排在最后）。
        ///
        /// 🔴 **原版没有这一跳**：它从不按「文件名里的对手名」找开场白 —— 对手台词在卡 SO 上是
        /// `customIntroChatData[].customSound`（= `ForVersus` ①之前那道 `TryResolve`）。
        /// `voice_lines.json` 里的 `vs*` 行是 `card_index.json` / 文件名猜出来的**投影**，
        /// 所以它在 `ForVersus` 里只能**兜底**：卡 SO 的 `introChatSound` 有值时，必须让 `intro` 先赢。
        ///
        /// 🔴 **2026-10-24（`D9` · 铁律 5）就地订正**：这一族**原来排在 `ForVersus` 最前面**
        /// （人名 `vs~` → 阵营词 → `intro` → `default`）⇒ **猜的压过真的**：
        /// `AM2` 对 `Abaddon` 该走 `introChatSound`，却被猜出来的 `vsabaddon` 抢走；
        /// 85 条未按对手键命中的 `_vs_` 里 A/B/C/D 四类全是这个形状
        /// （判据 → `资料/普查产出_1024/D8_补全chat_intro覆盖.md` §三）。</summary>
        public static string[] ForVersusGuess(string opponentWarlord, string opponentFaction)
        {
            var list = new List<string>(6);

            // ① 人名级：`vs~<对手督军>`（`EvMatches` 按「短词 ⊂ 对手全名」比）
            var d = VersNorm(opponentWarlord);
            if (!string.IsNullOrEmpty(d)) list.Add("vs~" + d);

            // ② 阵营级：查 `VsFactionTokens` 表（一个阵营可能有多条，也可能一条都没有）
            if (!string.IsNullOrEmpty(opponentFaction))
            {
                string[] toks;
                if (VsFactionTokens.TryGetValue(opponentFaction, out toks))
                    foreach (var t in toks)
                        if (!list.Contains(t)) list.Add(t);
            }
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
        /// 顺序按「出场 → 问候 → 独白 → 攻击 → 阵亡 → 投降 → 拒绝 → 兜底」，取第一条有的。
        /// ⚠️ 原来这里直接借用 `ForDeploy`（`greet`/`intro`/`line`），实测**会挂空**：
        ///    有的卡只有 `attack` / `death`（没有 greet/intro/line）⇒ 自检里那条断言变红。
        /// 🔴 **2026-10-16（`A1768`）**：`line` 改名 `deploy`（见文件头），本表跟着换；
        ///    末尾补 `default`（原版 `defaultChatSound`，实测那一族**不是** `attack` 专有）。
        /// </summary>
        public static readonly string[] ForVoiceOver =
            { "deploy", "greet", "intro", "attack", "death", "concede", "cantdo", "default" };

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
                //
                // 🔴 **2026-10-24（B4）同 ev 多行：优先「卡 SO 指名」的那一条**。
                //    为什么：同一张卡同一个 `ev` 会出现多行（三种归属来源撞在一起），
                //    其中**只有一行是原版卡 SO 真指的**，其余是**按文件名猜的**。随机挑会挑到 SO 没指的
                //    那一条 ⇒ 播错语音。现读实例（`Resources/voice_lines.json`）：
                //      · `ASH2` 的 `gen1`：SO→`_gen4.ogg`，另有猜的 `_gen1.ogg`；
                //      · `ASH2` 的 `intro`：SO→`_gen5.ogg`，另有猜的 `_intro.ogg`；
                //      · `UM88` 的 `gen3`：SO→`_gen4.wav`，另有猜的 `_gen3.wav`。
                //    判据列 = 生成器写的 `so`（原版字段名）/ `src == "cardSO"`（见 `Line` 的注释）。
                //    ⛔ **不是**按文件名猜（`UM88` 那条的文件序号和字段序号本来就对不上，正是原版如此）。
                //    **仍然多解才随机**（台词池），且把压掉的行打一行日志（不静默）。
                int hits = 0, soHits = 0;
                for (int i = 0; i < c.lines.Count; i++)
                {
                    var l = c.lines[i];
                    if (l == null || !EvMatches(l.ev, want)) continue;
                    hits++;
                    if (IsSoAttributed(l)) soHits++;
                }
                if (hits == 0) continue;

                bool soOnly = soHits > 0;
                if (soOnly && soHits < hits) WarnSuppressed(cardId, want, hits - soHits);
                int pool = soOnly ? soHits : hits;

                int pick = 0;
                if (rng != null && pool > 1) pick = rng.Next(0, pool);
                int seen = 0;
                for (int i = 0; i < c.lines.Count; i++)
                {
                    var l = c.lines[i];
                    if (l == null || !EvMatches(l.ev, want)) continue;
                    if (soOnly && !IsSoAttributed(l)) continue;   // ← 只有卡 SO 指名的那批进池
                    if (seen++ != pick) continue;
                    clipName = l.file;
                    text = l.text;
                    ev = l.ev;                 // ← 真 ev（虚拟 ev 不交出去，交出去的是那条真行自己的 ev）
                    return true;
                }
            }
            return false;
        }

        /// <summary>这一行是不是**原版卡 SO 指名**的那一条。
        /// `so` 非空 = 生成器解出了「哪个字段指它」；`src == "cardSO"` = 至少能确定来源是卡 SO
        /// （那 56 条「`attackAudio` + `defaultChatSound` 共指同一 clip」的行属于后者：
        ///  出处确定、但**代表不了哪一个角色**，所以 `so` 留空 —— 见 B2 报告 §七·3）。
        /// ⚠️ `src == "orphan"`（按名字猜的）与 `src` 为空的一律**不算**。</summary>
        static bool IsSoAttributed(Line l)
        {
            if (l == null) return false;
            if (!string.IsNullOrEmpty(l.so)) return true;
            return string.Equals(l.src, "cardSO", System.StringComparison.Ordinal);
        }

        /// <summary>「同 ev 多行、把 SO 没指名的压掉了」的日志 —— **每条只报一次**（别刷屏），
        /// 但不静默（红线：不许静默失败）。自检想验这条规则时直接看行为，不解析日志。</summary>
        static readonly HashSet<string> _suppressedLogged = new HashSet<string>();
        static void WarnSuppressed(string cardId, string ev, int dropped)
        {
            var key = cardId + "\u0001" + ev;
            if (!_suppressedLogged.Add(key)) return;
            Debug.Log($"[VoiceLines] {cardId} 的 `{ev}` 有 {dropped} 条不是卡 SO 指名的，"
                    + "按「SO 优先」压掉了（原版 SO 只指了其中一条；见 VoiceLines.TryPick 的注释）");
        }

        /// <summary>「这一句是靠**按文件名猜的** `vs*` 行兜的」—— 每条 (卡, ev) 只报一次（红线：不静默）。
        /// 🔴 **2026-10-24（`D9`）**：`vs*` 那一跳在 <see cref="ForVersus"/> 里已经排到**最后**，
        /// 所以真走到它 = 「这张卡**没有**卡 SO 的 `introChatSound` / `customIntroChatData`」——
        /// 就是 `BL5` / `BL67` 这类连兜底档都没录的卡。⛔ 别据此把那一跳删掉：有它才有开场白。
        /// 判据 → `资料/普查产出_1024/D9_开场白兜底顺序与脏标.md`。</summary>
        static readonly HashSet<string> _vsGuessLogged = new HashSet<string>();
        public static void WarnFilenameVsFallback(string cardId, string ev, string file)
        {
            var key = (cardId ?? "") + "\u0001" + (ev ?? "");
            if (!_vsGuessLogged.Add(key)) return;
            Debug.Log($"[VoiceLines] {cardId} 的开场白靠**按文件名猜的** `{ev}` 行兜底（`{file}`）"
                    + " —— 卡 SO 的 `introChatSound` / `customIntroChatData` 都没给这一句；"
                    + "原版没有这一跳，它是我们的兜底（`ForVersus` 已把它排到最后）");
        }

        /// <summary>自检用：这张卡上 `ev` **逐字等于** <paramref name="ev"/> 的原始行数。
        /// ⛔ 不走 `EvMatches` —— 这里量的是**数据本身**（夹具），不是取词逻辑。</summary>
        public static int LineRowCount(string cardId, string ev)
        {
            EnsureLoaded();
            Card c;
            if (cardId == null || _byId == null || !_byId.TryGetValue(cardId, out c) || c.lines == null) return 0;
            int n = 0;
            for (int i = 0; i < c.lines.Count; i++)
                if (c.lines[i] != null && string.Equals(c.lines[i].ev, ev, System.StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>自检用：同上，其中**卡 SO 指名**的条数（判据同 <see cref="IsSoAttributed"/>）。</summary>
        public static int SoRowCount(string cardId, string ev)
        {
            EnsureLoaded();
            Card c;
            if (cardId == null || _byId == null || !_byId.TryGetValue(cardId, out c) || c.lines == null) return 0;
            int n = 0;
            for (int i = 0; i < c.lines.Count; i++)
            {
                var l = c.lines[i];
                if (l != null && string.Equals(l.ev, ev, System.StringComparison.Ordinal) && IsSoAttributed(l)) n++;
            }
            return n;
        }

        // ==================================================================
        //  🆕 2026-10-24（`E3` · `DG3` 顺手发现 3）：**台词表注入口（自检专用）**
        //
        //  🔴 **为什么要有它**：「同 ev 多行时**永远取卡 SO 指名的那一条**」（`TryPick` 里 `soOnly` 那一支）
        //  在本批 `D5` 删掉那 9 条死行之后，**现数据里已经没有任何活的夹具** —— 全表只剩 4 组
        //  「(卡, ev) 多行」（`ASH3/BL35/BL36/SAU36` 的 `deploy`），**没有一组含 `src=cardSO`**
        //  ⇒ ⑤d 无论怎么改期望都**恒真**（一条重要规则长期无人守）。
        //  ⇒ 改成**夹具级见证者**（形状照 `E2` 的 `SetExactForTest`）：自检注入一组合成行
        //  （1 条 SO 指名 + N 条按名字猜的），再断「24 个种子都取 SO 那条」+「还原之后这个合成 id 取不到」。
        //  🧨 把 `TryPick` 里的 `soOnly` 去掉、回到纯随机 ⇒ 24 个种子下几乎必然抽到猜的那条 ⇒ 红；
        //  而「还原后必须取不到」挡的是「为了让它绿而把表写死」那一类。
        //  ⚠️ **生产路径零变化**：全仓只有 `WarpforgeArena1/Editor/AnimFXCheck.cs` 调它。
        //  ⚠️ 与 `SetExactForTest` 的区别：那个注的是**精确档**（`_exactById`，卡 SO 的
        //  `summonAudio/attackAudio/deathAudio`）；这个注的是**台词表本体**（`_byId`，即 `voice_lines.json`）。
        // ==================================================================

        /// <summary>自检用：<see cref="SetRowsForTest"/> 返回的**还原作用域**。
        /// `using` 出作用域 ⇒ 把那张卡**还原成注入之前的样子**（原来有 ⇒ 装回旧卡；原来没有 ⇒ 删掉）。
        /// <see cref="Dispose"/> 幂等；抛异常也还原。</summary>
        public sealed class RowsInjectionScopeForTest : System.IDisposable
        {
            internal string CardId;
            // 存成 `object`：`Card` 是**私有**嵌套类型，`internal` 字段不能把它写进签名（CS0052）；
            // 而**外层**不能访问嵌套类的 `private` 字段（C# 只有「嵌套 → 外层」单向可见）。
            // ⇒ 装箱成 `object`，`Dispose` 里再转回 `Card`（嵌套类看得到外层的私有类型）。
            internal object PrevCard;     // null = 注入之前这张卡不存在
            internal bool HadPrev, Injected;
            bool _disposed;

            internal RowsInjectionScopeForTest() { }

            /// <summary>真的注入了没有（`false` = 台词表没载入 ⇒ 自检**应当据此报红**，⛔ 别当成通过）。</summary>
            public bool WasInjected { get { return Injected; } }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (!Injected || _byId == null) return;
                if (HadPrev) _byId[CardId] = (Card)PrevCard;
                else _byId.Remove(CardId);
            }
        }

        /// <summary>🆕 2026-10-24（`E3` · `DG3` 顺手发现 3）：**往台词表里塞一张合成卡**（自检专用）。
        /// 只改内存表 `_byId`（**不动** `Resources/voice_lines.json`、**不动** `LineCount` ——
        /// 那个计数是「载入时读了多少条」的读数，不是表长）。
        /// <para>造出来的行：`soFile` 非空 ⇒ 一条 **`src = "cardSO"`** 的行（= 卡 SO 指名，`so` 留空 ——
        /// 与现数据里那 56 条「出处确定、代表不了哪个角色」的行同形状）；`guessedFiles` 每条 ⇒ 一行
        /// **`src = "orphan"`**（按文件名猜的，`TryPick` 必须把它压掉）。</para>
        /// <para>推荐 `using (VoiceLines.SetRowsForTest(id, ev, soClip, guessClip)) { … }` ⇒ 出作用域自动还原。</para>
        /// ⚠️ 台词表没载入（见 <see cref="LoadError"/>）时**注入不了**：返回作用域的
        /// <see cref="RowsInjectionScopeForTest.WasInjected"/> = `false` —— 调用方要据此**报红**，⛔ 别默默当通过。</summary>
        public static RowsInjectionScopeForTest SetRowsForTest(string cardId, string ev, string soFile,
                                                               params string[] guessedFiles)
        {
            var scope = new RowsInjectionScopeForTest { CardId = cardId };
            EnsureLoaded();
            if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(ev))
            {
                Debug.LogError("[VoiceLines][SetRowsForTest] 卡 id / 事件名不能为空 ⇒ 未注入。");
                return scope;
            }
            if (_byId == null)
            {
                Debug.LogError("[VoiceLines][SetRowsForTest] 台词表没载入 ⇒ 未注入"
                             + (LoadError == null ? "。" : "（" + LoadError + "）"));
                return scope;
            }
            Card prev;
            scope.HadPrev = _byId.TryGetValue(cardId, out prev);
            scope.PrevCard = prev;

            var lines = new List<Line>();
            if (!string.IsNullOrEmpty(soFile))
                lines.Add(new Line { ev = ev, file = soFile, text = null, src = "cardSO", so = null });
            if (guessedFiles != null)
                for (int i = 0; i < guessedFiles.Length; i++)
                    if (!string.IsNullOrEmpty(guessedFiles[i]))
                        lines.Add(new Line { ev = ev, file = guessedFiles[i], text = null, src = "orphan", so = null });

            _byId[cardId] = new Card { id = cardId, name = cardId, faction = null, lines = lines };
            scope.Injected = true;
            return scope;
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
