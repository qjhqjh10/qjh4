// DeckStore.cs — 卡组存档（读写玩家自己组的卡组）
//
// ⚠️ 本文件用 UnityEngine（`JsonUtility`）+ `CardPresentation.SaveRoot`（存档根）。
//    `Core/` 下的东西一律不碰 Unity，规则/模型在 `Core/DeckRules.cs` 里。
//
// 存在哪：`SaveRoot.Path/WarpforgeDecks.json`（🆕 2026-11-03 B128：根从 `Application.persistentDataPath` 收到 **`C:\warpforgesave`**）· 🆕 2026-11-04（`B139_V4`）：**批处理（`-batchmode`）且没注入 `OverridePath` ⇒ 落临时沙箱**（类尾 `BatchSafePath`，玩家真根一个字节都不碰）
//   · ⚠️ 本条是 `RuleEngine`→`CardPresentation` 的**唯一**引用（只碰一个纯路径字符串）；`工具/ruleprobe` 的 csproj **不编本文件** ⇒ 不影响 headless 探针。
//   · 旧位置 `%USERPROFILE%\AppData\LocalLow\<公司名>\<产品名>\` 的旧档**永不删除**，只是不再读写。
//   · 原版的卡组存在**服务器**（`CardDeck.syncedToServer` / `deckId`），单机只能落到本地 —— 这一点和原版不同。
//
// 为什么自己读写文件而不是 `PlayerPrefs`：卡组是一份**有结构、会变长**的数据，
// PlayerPrefs 是给零散键值用的，塞 JSON 进去在 Windows 上还会写注册表。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RuleEngine
{
    public static class DeckStore
    {
        public const string FileName = "WarpforgeDecks.json";

        /// <summary>自检用的路径改写口。设了就用它，不设就用玩家目录 —— 自检不该动玩家的真存档。</summary>
        public static string OverridePath;

        /// <summary>存档文件的完整路径。UI 上要显示给玩家看的时候用它。</summary>
        public static string Path
        {
            get
            {
                return OverridePath ?? BatchSafePath();   // 🆕 2026-11-03 B128：根 = `C:\warpforgesave`（⚠️ 本条是 RuleEngine→CardPresentation 的**唯一**引用，见文件头注）· 🆕 2026-11-04（`B139_V4`）：第二档改走 `BatchSafePath()` = **批处理且没注入 ⇒ 沙箱**（`??` 语义未变：`null` 才走第二档，空串照旧原样返回）
            }
        }

        [Serializable]
        class Dto
        {
            public int version;
            /// <summary>当前选中的是第几套。**旧存档里没有这个字段 → 反序列化成 0**，
            /// 正好是「选第一套」，向后兼容。</summary>
            public int current;
            public List<PlayerDeck> decks;
        }

        /// <summary>
        /// 🔴 **2026-10-18（`G8` · `G3` 交回的那半）：「读存档出了什么事」的【类型】= 错误码。
        /// 判据是它，不是文案。**
        ///
        /// **改之前**：`LoadAll` 的三条出口（`:55` / `:64` / `:90`）直接拼**给人看的整句中文** ——
        /// `"还没有存档"` / `"存档解析失败（内容为空）"` / `"存档读取失败：" + e.Message`。
        /// 调用方只能拿 `note.Contains("失败")` 去猜类型（`G3` 已把它换成结构性判据），
        /// 而**那句话本身仍然是中文整句** ⇒ 一旦显示层走本地化，`LastError`（= 这句话）
        /// 就会把中文/英文**直接喷进英文界面**，或者更糟：判据跟着语言变（`资料/已知的坑.md` 那一族）。
        ///
        /// **照原版的形状**：原版把「出了什么事」拆成**枚举 + 词条键**两段 ——
        ///   · **枚举** = `CustomError`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CustomError.cs:24`
        ///     `ErrorLoadDeck = 170`）；
        ///   · **键** = **`"CustomErrors/" + 枚举名`** —— 拼法在
        ///     `d:/2/tools/decomp_full/CloudscriptHandler__HandleCustomError.c` 里**实读**：
        ///     `String.Concat(DAT_18425cca8 /* = "CustomErrors/"，stringliteral.json 0x425CCA8 */, 枚举名)`
        ///     → `WindowsManager.ShowPopUp(键)`（界面文案由它自己去 I2 表取）。
        ///   ⇒ **代码出码、界面出词条**，本层**一个给人看的字都不产**（见 <see cref="TermKeyOf"/>）。
        /// ⛔ 别在这一层拼中文/英文句子 —— 那正是「换语言就静默失效」的来源。
        /// </summary>
        public enum LoadNote
        {
            /// <summary>读到了（`DeckStore` 没话说 —— 哪怕是 0 套的合法存档）。</summary>
            None = 0,
            /// <summary>**存档文件不在**（第一次跑）—— ✅ **不是失败**（⛔ 界面上不该弹错）。</summary>
            NoSaveFile = 1,
            /// <summary>存档在，但**拿不出卡组** —— 2026-10-18 起判据 = `dto == null || dto.decks == null`
            /// **或原文 JSON 里没有 `decks` 键**（原文里原来只写了前两项 ⇒ 对「缺键」恒假 ⇒ 这一档曾是死代码；
            /// 订正经过见 `LoadAll` 里那段注释）。</summary>
            Empty = 2,
            /// <summary>存档在，但读的时候抛了（`File.ReadAllText` / `JsonUtility.FromJson`）。</summary>
            ReadFailed = 3,
        }

        /// <summary>读存档失败时该显示的那条**原版词条键**（`DeckLibrary.LastLoadIssueTerm` 用它）。
        ///
        /// 🔴 **键名 `CustomErrors/ErrorLoadDeck` 的载体（坑表 #20：报「没有」之前要把载体打全）**：
        ///   ① **代码字面量** —— `d:/2/tools/il2cpp_out/stringliteral.json` 的 `0x425D0A8`
        ///      （`RVA = 地址 − 0x180000000` ⇒ 反编译里那个 `DAT_18425d0a8`）；
        ///   ② **消费点** —— `d:/2/tools/decomp_full/SearchOpponentManager__SearchOpponent.c:89-90`：
        ///      `I2.Loc.LocalizationManager.GetTermTranslation(DAT_18425d0a8, …)`，结果交给一个回调
        ///      （那一支 = 原版「这副牌校验不过 ⇒ 把这句话说出去」）；
        ///   ③ **枚举** —— `CustomError.ErrorLoadDeck = 170`（同族键 = `"CustomErrors/" + 名字`）。
        /// ⚠️ **载波①（prefab 上那颗 `Localize.mTerm`）里没有这一条** —— 全库 24.7 万文件扫
        ///    `"mTerm": "CustomErrors/…"` 只命中 **2 条**（`DuplicateConnection` / `ErrorSavingMatch`，
        ///    都在 13 个战场场景里那颗 `BattleErrorUIManager` 上；`bundle_scenes_scenes_battlearena*/`）。
        ///    ⇒ 这条**不是「原版没有」，是载体不同**（只活在代码字面量里）。
        /// ⚠️ **英文/中文两列本地都取不到**（原版那套在**远端 I2 语言表**）—— `Loc.cs` 那一条**是我们自拟的**，
        ///    已如实标注。`数据/本地化/i18n/zh_CN.csv` 里也没有对应的英文源串（按第一列查过，0 命中）。</summary>
        public const string ErrorLoadDeckTerm = "CustomErrors/ErrorLoadDeck";

        /// <summary>错误码 → **原版词条键**；`None` / `NoSaveFile` **没有词条** ⇒ `null`。
        /// （原版卡组存在**服务器**上，没有「本机还没有存档」这件事；「第一次跑」在界面上本来就该
        ///  **什么都不显示** —— ⛔ 别为它编一条。）</summary>
        public static string TermKeyOf(LoadNote n)
        {
            switch (n)
            {
                case LoadNote.Empty:
                case LoadNote.ReadFailed:
                    return ErrorLoadDeckTerm;
                default:
                    return null;
            }
        }

        /// <summary>读全部卡组。返回**错误码**（<paramref name="note"/>）与一句**诊断串**
        /// （<paramref name="detail"/> —— **技术用，不是给人看的界面文案**；界面文案走
        /// <see cref="TermKeyOf"/> 那个键 + `CardPresentation.Loc`）。
        ///
        /// 文件不存在/坏了都返回空表 —— **不抛异常**，让调用方能自己决定怎么提示。
        ///
        /// ⚠️ <paramref name="detail"/> 在「有话要说」的三种情形下**一律非空** ——
        /// 它是「`DeckStore` 有没有话要说」这个**信号**（`DeckLibrary.ClassifyLoad` 拿它区分
        /// `None` 与「有话」）⇒ ⛔ 别把 `NoSaveFile` 那一条改成 `null`（那会把「第一次跑」
        /// 静默判成「读到了」）。</summary>
        public static List<PlayerDeck> LoadAll(out LoadNote note, out string detail, out int current)
        {
            note = LoadNote.None;
            detail = null;
            current = 0;
            var path = Path;
            if (!File.Exists(path))
            {
                note = LoadNote.NoSaveFile;
                detail = "no save file";
                return new List<PlayerDeck>();
            }
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                note = LoadNote.ReadFailed;
                detail = "read failed: " + e.GetType().Name + ": " + e.Message;
                return new List<PlayerDeck>();
            }
            return Parse(text, out note, out detail, out current);
        }

        /// <summary>Step 4 切换口（读）的**数据半**：把一份 payload 原文解析成卡组表。</summary>
        /// <remarks>
        /// <para>判据与 <see cref="LoadAll"/> **逐字同形** —— 它现在只是「读文件 → 调我」两跳；
        /// 单独拆出来是为了让**调用方**（`RuleEngine/Data/DeckLibrary.cs` 的 `Load`）能把「这份原文从哪读」
        /// 换成中央档（`CardPresentation.Shell.SaveCenter.ReadThroughLegacy`）而**不把本类的文件 IO 也带上**。
        /// 🔴 **分层**：`DeckStore` 在 `RuleEngine`、`SaveCenter` 在 `CardPresentation.Shell` ⇒ 双写的两行契约由
        /// **调用方**转发，⛔ 别让 `RuleEngine` 反向依赖 `Shell`（方案 §七 Step 4 / 施工单 界面 #6）。</para>
        /// <para>`text == null` ⇒ 与「文件不在」**同码**（<see cref="LoadNote.NoSaveFile"/>、`detail = "no save file"`）：
        /// 中央档与旧档都没有 = 第一次跑，⛔ 不是失败。</para>
        /// <para>⚠️ 这个重载**不做** `File.Exists` 判断 —— 「文件在不在」是**取原文之前**的事
        /// （`DeckLibrary.ClassifyLoad` 的 `saveFileExisted` 要它），取在之后会把「第一次跑」与「文件在但读不出来」判反。</para>
        /// </remarks>
        public static List<PlayerDeck> Parse(string text, out LoadNote note, out string detail, out int current)
        {
            note = LoadNote.None;
            detail = null;
            current = 0;
            if (text == null)
            {
                note = LoadNote.NoSaveFile;
                detail = "no save file";
                return new List<PlayerDeck>();
            }
            try
            {
                // 🔴 2026-10-18 订正（铁律 5：原来写 X、为什么恒假、证据在哪）：
                //    **原来这里只有 `dto == null || dto.decks == null`，那句对「合法 JSON、但缺 `decks` 键」的存档
                //    【恒假】** —— `JsonUtility` 把**缺键**的集合字段给成**空表、不是 `null`**
                //    （`Dto.decks` 是裸字段、没有初始化器；机制本身没读到序列化后端的源码级判据，
                //      但「`dto.decks` 非 null」这件事由下面两条**独立日志观测**坐实，改法不依赖机制）。
                //    ⇒ 这样的存档一路走到本方法末尾 `return dto.decks`（`note = None`、`detail = null`）
                //    ⇒ **`Empty` 这一支自首版 `51351fa` 起从没被走到过**（死代码；别处也从未改过这句）。
                //    **证据**：`_tmp_view/deck.log:17259`（断的是 `LoadAll` 的 **out 码本身**、
                //      **没经过 `ClassifyLoad`** —— 全批最直接的一条观测，实测 `LoadNote.None`）
                //      + `_tmp_view/ruleengine.log:58811`（`LastError` 实测 `null`）。
                //    **代价**：下游「类型 → 错误码 → 诊断串 → 词条键」四级出口全塌，再乘中英两档
                //      = 13 条红（`RuleEngineTest` 4 条 + `DeckScene` 9 条）。
                //    **改法**：判「这份存档有没有 `decks` 那一段」只能看**原文的 JSON 键** —— 结构性判据，
                //      不是给人看的文案（本层一个给人看的字都不产，见 `LoadNote` 的注释）。
                //    ⚠️ 用**带冒号**的 `"decks":`，**不是** `"decks"` —— 后者会被「卡组名字恰好叫 `decks`」
                //      的旧档误判（字符串值 `"decks"` 后面跟的是 `,` / `}`，**永远不是 `:`**）。
                //    ⚠️ 键名照我们的写入器：`SaveAll` 是**唯一**写入器、恒走 `JsonUtility.ToJson` ⇒ 恒写该键
                //      ⇒ **零迁移风险**（首版 `51351fa` 起 `{version, decks}` 这个形状就没变过）。
                //    ⛔ **别照兄弟写法加「或空」（把 `dto.decks.Count == 0` 也当失败）** ——
                //      `DeckLibrary.ClassifyLoad` 明写「**哪怕是 0 套的合法存档** ⇒ `None`」，而
                //      `DeckLibrary.Delete` 允许把库删到 0 套、`SaveAll` 会写出 `"decks": []`
                //      ⇒ 加「或空」会让「**删光卡组的玩家**」下次打开时看到「卡组存档读取失败」。
                //      **「空集合」≠「缺键」**（两个形状一样、语义不同的地方）。
                //    ⚠️ **本判据依赖一个离线验不了的前提**（如实标着，没查清）：`JsonUtility.ToJson` 对**空表**
                //      写出的是 `"decks": []`（而**不是省略该键**）—— 若它省略，则「删光卡组」的合法存档会被
                //      上面这句判成 `Empty`（**假报错**）。预期它写 `[]`（Unity 序列化不省略空数组），
                //      但 `JsonUtility` 只在 Unity 里跑、本地验不了 ⇒ **配套断言由另一份文件补**
                //      （「`SaveAll(0 套)` → `LoadAll` ⇒ 码 `None`、`LastLoadIssue = None`」）；
                //      ⛔ 本文件不碰测试文件。
                bool hasDecksKey = text.IndexOf("\"decks\":", StringComparison.Ordinal) >= 0;
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null || !hasDecksKey)
                {
                    note = LoadNote.Empty;
                    detail = "save file has no decks";
                    return new List<PlayerDeck>();
                }
                // 🔴 🆕 2026-10-18：**旧卡 id 的读时迁移闸**（卡池换过两代：`601005b` 一次性换 955 张 +
                //    `K06` 换掉最后那批自造 token，两次都没迁玩家存档）。
                //    为什么必须在【读档】这一处：`CardIds` 的真正来源只有这一个口
                //    （卡组列表 / 卡组编辑 / 开局 `BattleDriver.PickSavedDeck` 全经 `DeckLibrary.Load` → 这里）。
                //    ⚠️ 卡池查表口**只建一次**（`CardDatabase.DeckLookup`：① 稳定 id ② **卡名** ——
                //      第 ② 条是给 2026-09-13 之前**按卡名**存的旧存档留的，⛔ 别去掉；
                //      判**代际**看的是那张映射表，不看这个口）；
                //      池子没加载时它是 null ⇒ `MigrateDeckVerified` 会**整个跳过**（⛔ 不拿空池判「查不到」去清数据）。
                //    ⚠️ **只改内存、不改文件**（⛔ 不许动玩家存档）；**幂等**靠 `LegacyCardIds` 的**整副双解**闸门
                //      （不是靠「表里的值不是 key」—— 表里 890/1102 个值本身就是 key，见那个文件头）。
                //    逐条判据 / 表 → `RuleEngine/Data/LegacyCardIds.cs`。
                var cardLookup = LegacyCardIds.PoolLookup();
                // 归一化：JsonUtility 会把缺字段的字符串写成 null，UI 不想到处判空
                foreach (var d in dto.decks)
                {
                    if (d == null) continue;
                    d.Name = d.Name ?? "未命名";
                    d.WarlordId = d.WarlordId ?? "";
                    d.DefensiveId = d.DefensiveId ?? "";
                    // 卡背：旧存档没有这个键 ⇒ `JsonUtility` 给 null ⇒ 归一成空串（= 没选过，用阵营默认）。
                    // 判据见 `PlayerDeck.CardbackId`。⚠️ 消费者一律用 `string.IsNullOrEmpty` 判，
                    // 因为**新建**的卡组那条路给的是 null（不走这个归一化）。
                    d.CardbackId = d.CardbackId ?? "";
                    // 模式：🆕 2026-09-26 加的字段（`PlayerDeck.GameMode`）。旧存档没这个键 ⇒
                    // `JsonUtility` 给 **0** ⇒ 正好 = 经典，**和原版「null 写 0」同义**，不用另写归一化。
                    // ⚠️ 别在这里把「未知模式」改成 0 —— 原版是**原样存原样读**（`CardDeck__Serialize.c:68-75`），
                    //    「认不认识某个模式」由消费者（`GameplayVariables.For`）决定。
                    if (d.CardIds == null) d.CardIds = new List<string>();
                    // 🔴 旧卡 id 的读时迁移（两代换号；**只改内存**、幂等）。判据 → `LegacyCardIds.cs`。
                    LegacyCardIds.MigrateDeckVerified(d, cardLookup, "存档「" + d.Name + "」");
                }
                current = dto.current;
                if (current < 0 || (dto.decks.Count > 0 && current >= dto.decks.Count)) current = 0;
                return dto.decks;
            }
            catch (Exception e)
            {
                note = LoadNote.ReadFailed;
                // 诊断串 = 异常类型 + 异常自己的话。⚠️ 后半段是 .NET/Unity 给的（非中文 OS 上是英文），
                //    不是「我们产出的给人看的中文」；但它**仍然不该**当界面主文案
                //    （显示层一律走 `TermKeyOf` 那条键 + `Loc`）—— 它只是「兜底诊断」。
                detail = "read failed: " + e.GetType().Name + ": " + e.Message;
                return new List<PlayerDeck>();
            }
        }

        /// <summary>⚠️ **兼容重载（旧的 `out string note`）** —— 只给
        /// `RuleEngine/Editor/DeckRulesTest.cs` 里仍在用旧 `out string note` 重载的那几条断言用（**那个文件不在本件白名单里**，
        /// ⇒ 没跟着改；`DeckRulesTest` 里 `note.Contains("失败")` 那一半因此恒假（grep `note.Contains` 定位），见交件报告）。
        ///
        /// 🔴 **它现在吐的是 `detail`（诊断串），不再是「还没有存档」那类中文整句**
        /// （`"no save file"` / `"save file has no decks"` / `"read failed: …"`）。
        /// ⛔ 新代码一律走上面那个 `out LoadNote` 的版本 —— **判类型别拿这句话去比字**
        /// （那正是 `G3` 拆掉的那个坑）。</summary>
        public static List<PlayerDeck> LoadAll(out string note, out int current)
        {
            LoadNote _;
            return LoadAll(out _, out note, out current);
        }

        public static List<PlayerDeck> LoadAll(out string note) { int _; return LoadAll(out note, out _); }

        public static List<PlayerDeck> LoadAll() { string _; int __; return LoadAll(out _, out __); }

        /// <summary>Step 4 切换口（写）的**数据半**：把卡组表序列化成一份 payload（**纯内存，不碰盘**）。</summary>
        /// <remarks>
        /// <para>与 <see cref="SaveAll"/> 用的是**同一份** `Dto` 形状（`{version, current, decks}`）；拆出来是为了让
        /// 调用方能把「这份 payload 写到哪」换成中央档（`SaveCenter.SaveThroughLegacy(key, path, json, out err)`：
        /// 中央档带 `_v` 信封、旧档**不带**，两边同一份 payload）—— 那正是方案 §七 Step 4 的两行契约。</para>
        /// <para>⚠️ 形状要求：`SaveThroughLegacy` 只收**JSON 对象**（首尾必须是 `{ }`）。`JsonUtility.ToJson(dto)` 恒是对象
        /// （`Dto` 是 `[Serializable]` 的类、不是数组）⇒ 满足。</para>
        /// </remarks>
        public static string Serialize(List<PlayerDeck> decks, int current)
        {
            var dto = new Dto { version = 1, current = current, decks = decks ?? new List<PlayerDeck>() };
            return JsonUtility.ToJson(dto, true);
        }

        /// <summary>写全部卡组。返回 true = 写成功；失败时 `error` 里是**诊断串**
        /// （🔴 **不是给人看的界面文案** —— 界面文案走 `CardPresentation.Loc`；
        /// 见 `LoadNote` 那段注释：本层一个给人看的字都不产）。
        /// ⚠️ 调用方拿它当**兜底诊断**（`DeckRuntime.SaveFailReason` 就这么用），
        /// ⛔ 别拿它去比字判类型。</summary>
        public static bool SaveAll(List<PlayerDeck> decks, int current, out string error)
        {
            error = null;
            try
            {
                var json = Serialize(decks, current);
                // 先写临时文件再替换 —— 写一半崩了不至于把旧存档毁掉
                var tmp = Path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(Path)) File.Delete(Path);
                File.Move(tmp, Path);
                return true;
            }
            catch (Exception e)
            {
                error = "write failed: " + e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public static bool SaveAll(List<PlayerDeck> decks, out string error) { return SaveAll(decks, 0, out error); }

        public static bool SaveAll(List<PlayerDeck> decks) { string _; return SaveAll(decks, 0, out _); }

        /// <summary>删掉存档文件（自检用 —— 真实玩家路径上没有这个入口）。</summary>
        public static void DeleteFile()
        {
            try { if (File.Exists(Path)) File.Delete(Path); } catch { /* 自检里失败无所谓 */ }
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🔶 自己设计（非原版）：**批处理存档沙箱** · 判据 = 原版没有自检宿主（存档在服务端）· 调度台 2026-11-04（`B139_V4`）
        //  🆕 追加在**类尾**（`DeleteFile` 之后 ⇒ 上面所有账行号零漂移）；断言宿主 `Editor/B139_V4_Assert.cs` → `RuleEngineTest.Run`
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>沙箱目录名（`Application.temporaryCachePath` 下的一层）。</summary>
        public const string BatchSandboxDirName = "wfcheck_save";

        static string _sandboxFile;        // 本进程的沙箱文件（`null` = 还没算过）
        static bool _sandboxSaid;          // 「实际落点 / 建不出目录」只出声一次（⛔ 不刷屏）
        static readonly string _sandboxTag = Guid.NewGuid().ToString("N").Substring(0, 8);   // 一次进程一个沙箱

        /// <summary>批处理判据口。⚠️ **只给自检用** —— 宿主在交互式编辑器里跑时靠它把闸强制打开/关掉
        /// （否则「批处理才改道」这条判据在非批处理环境里根本跑不到，只能写成 `if (前提) Check(结论)` 那种静默跳过）。
        /// 正式代码恒为 `null` ⇒ 用 `Application.isBatchMode`。</summary>
        public static Func<bool> BatchModeProbe;

        /// <summary>这一趟算不算批处理（`BatchModeProbe` 优先，正式代码恒 `null`）。</summary>
        static bool BatchNow { get { return BatchModeProbe != null ? BatchModeProbe() : Application.isBatchMode; } }

        /// <summary>🔶 自己设计（非原版）：**批处理下、且没人注入 `OverridePath` 时**，`Path` 的落点改到临时沙箱。
        /// <para>· 这是什么：`Path` 的第二档。**非批处理时原样返回玩家真路径**（正式运行零变化）。</para>
        /// <para>· 为什么必须自己定：原版**没有自检宿主**（卡组存在服务端 `CardDeck.syncedToServer`）⇒
        ///   「自检别写玩家真档」这条判据在**原版里根本不存在**、本地也拿不到 ⇒ 只能我们定。</para>
        /// <para>· 谁定的 + 日期：调度台 **2026-11-04**（`B139_V4`；用户 2026-11-04 要求「一个任务就彻底完成」）。</para>
        /// <para>🔴 **为什么是「改道」不是「拒绝写」**：`CardPresentation/Editor/B102DECK_Assert.cs:191` 与
        ///   `RuleEngine/Editor/DeckRulesTest.cs:251` 都直接调 `SaveAll` 并断「写成功」⇒ 拒绝会让它们红；
        ///   改道 = 行为不变、只搬落点。</para>
        /// <para>🔴 **为什么目录名带一次性的进程 tag**：一次进程一个新沙箱 ⇒ 上一次自检留下的卡组**不会污染这一次**
        ///   （否则会出现「明明 `NoSaveFile` 却读回上一次那 2 套」的幽灵夹具）。目录落在系统临时区
        ///   （`Application.temporaryCachePath`），不占工程目录。</para>
        /// <para>⚠️ 建目录失败 ⇒ `LogError` **出声**、仍返回沙箱路径（`SaveAll` 那一步会自己回 `false`）；
        ///   ⛔ **绝不因此回落到玩家真存档根**。</para></summary>
        static string BatchSafePath()
        {
            // ⚠️ **先判批处理、再算真路径**：`CardPresentation.SaveRoot.Path` 会触发它的 `Resolve()`（建目录 + 写一个
            //    探针文件再删掉）⇒ 批处理这一趟**连它都不解析**，这才叫「玩家真存档根一个字节都不碰」。
            if (!BatchNow) return System.IO.Path.Combine(CardPresentation.SaveRoot.Path, FileName);
            if (_sandboxFile != null) return _sandboxFile;
            var dir = System.IO.Path.Combine(Application.temporaryCachePath, BatchSandboxDirName, _sandboxTag);
            _sandboxFile = System.IO.Path.Combine(dir, FileName);
            try
            {
                Directory.CreateDirectory(dir);
                if (!_sandboxSaid)
                {
                    _sandboxSaid = true;
                    Debug.Log("[DeckStore] 批处理（自检）⇒ 卡组存档落点 = 沙箱「" + _sandboxFile
                              + "」；玩家真存档根**一个字节都不碰**（本进程连 `SaveRoot.Path` 都不解析）"
                              + "（要落真根：显式设 `DeckStore.OverridePath`，或起一个非批处理进程）");
                }
            }
            catch (Exception e)
            {
                if (!_sandboxSaid)
                {
                    _sandboxSaid = true;
                    Debug.LogError("[DeckStore] 批处理沙箱目录建不出来（" + dir + "）：" + e.GetType().Name + ": " + e.Message
                                   + " ⇒ 本进程的卡组存档**读写会失败**（⛔ 但绝不回落到玩家真存档根）");
                }
            }
            return _sandboxFile;
        }

        // ================================================================================================
        //  🆕 2026-11-04（执行代理 `B141_W15` · UI-05 D6）· 存档 **key 形状**：`PlayerDecks` 不再折叠 `current`
        //
        //  原版判据（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` + `d:/2/tools/decomp_full/` 现读）：
        //   · `DeckInventory.cs:209` —— `PlayerDecks` = **`AzureDeckInfo[]`**（数组，**没有 current**）。
        //   · `DeckInventory.cs:216` + `decomp_full/DeckInventory__OnFinishUnpack.c:45-58` —— `PlayerDecksFast`
        //     也是 `AzureDeckInfo[]`，与 `PlayerDecks` **Concat**。
        //   · `PlayerDataManager.cs:11459-11470` —— `PlayerDecks` 缺时读 `Decks`，类型 = **`Dictionary<string, AzureDeckInfo>`**。
        //   · `PlayerDataManager.cs:11472-11473` + `decomp_full/PlayerDataManager__set_currentDeckId.c:5` ——
        //     `CurrentDeck` = **字符串卡组 id**（读进 `currentDeckId`，字段偏移 `+0x238`）。
        //  ⇒ 本文件出三件：① 新写形状 `{version,decks}`（**不含 current**）② `CurrentDeck` 的序列化/解析
        //     ③ `Decks` / `PlayerDecksFast` 的**读兜底**解析（原版客户端只读不写这两个）。
        //  ⚠️ 旧形状（`{version,current,decks}`）的**读**保持不变（`Parse` 仍读 `Dto.current`）⇒ 旧档能读。
        // ================================================================================================

        /// <summary>**新写形状**：`{version, decks}` —— 不含 `current`（原版 `PlayerDecks` 只有卡组表；
        /// 「当前选中」移到独立 key `CurrentDeck`，见 <see cref="SerializeCurrentDeck"/>）。
        /// <para>与 <see cref="Serialize"/> 的分工：后者保留旧形状（带 `current`），只服务**无 Shell 的纯文件老路**
        /// （`SaveAll`）与既有往返断言；**生产中央档走本方法**（`DeckLibrary.WriteDecksThroughHooks`）。</para>
        /// <para>✅ 已复刻（2026-11-04 · B141 · `cpp2il/out_cpp2il_dev_ilspy/DeckInventory.cs:209` +
        /// `DeckInventory.cs:216` + `PlayerDataManager.cs:11472`）。</para></summary>
        public static string SerializeStore(List<PlayerDeck> decks)
        {
            var dto = new DtoWrite { version = 1, decks = decks ?? new List<PlayerDeck>() };
            return JsonUtility.ToJson(dto, true);
        }

        /// <summary>`CurrentDeck` 的本地载荷 —— 原版是**裸字符串（卡组 id）**；我们的中央档
        /// `SaveCenter.SetRaw` 只收 JSON 对象 ⇒ 本地包成 `{"id":"…"}`。
        /// <para>🔶 自己设计（非原版）：包对象只是**本地承载**，key 名 / 语义 / 值是原版的（卡组 id 字符串）·
        /// 为什么必须自己定：原版载荷是 PlayFab user data 的裸串，本地 `SaveCenter` 的 `_v` 信封结构上装不下标量；
        /// 服务器已关服、判据在远端，本地只能定一个可逆封装 · 调度台 2026-11-04 裁定。</para>
        /// <para>✅ 已复刻（2026-11-04 · B141 · `PlayerDataManager.cs:11472-11473` + `set_currentDeckId.c:5`）。</para></summary>
        public static string SerializeCurrentDeck(string deckId)
        {
            return "{\"id\":" + QuoteJsonString(deckId ?? "") + "}";
        }

        /// <summary>解析 `CurrentDeck`：① 我们的 `{"id":"…"}` ② 原版裸 JSON 字符串 `"…"` ③ 裸文本。
        /// 空 / 解不出 ⇒ `null`（调用方退回旧档 payload 里的 `current:int`）。</summary>
        public static string ParseCurrentDeckId(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string t = text.Trim();
            if (t.Length == 0) return null;
            if (t[0] == '{')
            {
                try
                {
                    var dto = JsonUtility.FromJson<CurrentDeckDto>(t);
                    if (dto != null && !string.IsNullOrEmpty(dto.id)) return dto.id;
                }
                catch (Exception) { /* 落到下面几档 */ }
                return null;                     // 对象里没有 `id`（含 `{"id":""}`）⇒ 当作没有，⛔ 别把整段 JSON 当 id
            }
            if (t.Length >= 2 && t[0] == '"' && t[t.Length - 1] == '"')
                return UnescapeJsonString(t.Substring(1, t.Length - 2));
            return t;
        }

        /// <summary>`PlayerDecksFast` 的读兜底：原版 `GetOnlineData&lt;AzureDeckInfo[]&gt;(PlayerDataKey.PlayerDecksFast)`
        /// （`DeckInventory.cs:216`）。⛔ 只在**读**侧用 —— 原版客户端没有写点（写走服务端）。
        /// 解不出 ⇒ 空表 + 出声（⛔ 不静默当成「没有」）。</summary>
        public static List<PlayerDeck> ParseAzureDeckArray(string text)
        {
            var list = new List<PlayerDeck>();
            if (string.IsNullOrEmpty(text)) return list;
            string t = text.Trim();
            if (t.Length == 0 || t[0] != '[')
            {
                Debug.LogWarning("[DeckStore] `PlayerDecksFast` 不是 JSON 数组（首字符 = '" + (t.Length > 0 ? t[0] : ' ') + "'）⇒ 当作空表");
                return list;
            }
            try
            {
                foreach (var raw in SplitTopLevelArray(t))
                {
                    var a = JsonUtility.FromJson<AzureDeckDto>(raw);
                    var d = ToPlayerDeck(a, null, raw);
                    if (d != null) list.Add(d);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DeckStore] `PlayerDecksFast` 解析失败（" + e.GetType().Name + ": " + e.Message + "）⇒ 当作空表");
            }
            return list;
        }

        /// <summary>`Decks` 的读兜底：原版 `GetOnlineData&lt;object&gt;(PlayerDataKey.Decks)` 读成
        /// `Dictionary&lt;string, AzureDeckInfo&gt;`（`PlayerDataManager.cs:11459-11470`，只在 `PlayerDecks` 缺时读）。
        /// 值的 `id` 缺省时用 **map 的键**（原版按 id 键存）。解不出 ⇒ 空表 + 出声。</summary>
        public static List<PlayerDeck> ParseAzureDeckMap(string text)
        {
            var list = new List<PlayerDeck>();
            if (string.IsNullOrEmpty(text)) return list;
            string t = text.Trim();
            if (t.Length == 0 || t[0] != '{')
            {
                Debug.LogWarning("[DeckStore] `Decks` 不是 JSON 对象（首字符 = '" + (t.Length > 0 ? t[0] : ' ') + "'）⇒ 当作空表");
                return list;
            }
            foreach (var kv in SplitTopLevelObject(t))
            {
                try
                {
                    var a = JsonUtility.FromJson<AzureDeckDto>(kv.Value);
                    var d = ToPlayerDeck(a, kv.Key, kv.Value);
                    if (d != null) list.Add(d);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[DeckStore] `Decks` 的键 `" + kv.Key + "` 解析失败（" + e.GetType().Name + ": " + e.Message + "）⇒ 跳过它");
                }
            }
            return list;
        }

        // ------------------------------------------------------------ 内部：Azure 形状 → PlayerDeck

        [Serializable]
        class DtoWrite
        {
            public int version;
            public List<PlayerDeck> decks;
        }

        [Serializable]
        class CurrentDeckDto
        {
            public string id;
        }

        [Serializable]
        class AzureDeckDto
        {
            public string id;
            public string name;
            public string hero;
            public string characterId;
            public string defenseCard;
            public string cardback;
            public string warcry;
            public string prebuiltDeckId;
            public int deckTag;
            public long lastEditTime;
            public int gameMode;
            // ⚠️ `cards` 是 `Dictionary<string,int>` —— `JsonUtility` **不支持字典** ⇒ 由 `ParseCardsMap` 单独解。
        }

        /// <summary>原版 `AzureDeckInfo` → 我们的 <see cref="PlayerDeck"/>（只用于**读兜底**）。
        /// 字段对应：`name→Name` · `hero→WarlordId` · `defenseCard→DefensiveId` · `cards(id→张数)→CardIds`
        /// · `gameMode→GameMode` · `cardback→CardbackId` · `warcry→WarcryId` · `id→DeckId`。</summary>
        static PlayerDeck ToPlayerDeck(AzureDeckDto a, string idFromKey, string rawObjectJson)
        {
            if (a == null) return null;
            string id = string.IsNullOrEmpty(a.id) ? idFromKey : a.id;
            var d = new PlayerDeck(a.name ?? "未命名", a.hero ?? "", a.defenseCard ?? "",
                                   new List<string>(), a.gameMode)
            {
                CardbackId = a.cardback ?? "",
                WarcryId = a.warcry,
                DeckId = string.IsNullOrEmpty(id) ? null : id,
            };
            foreach (var kv in ParseCardsMap(rawObjectJson))
                for (int i = 0; i < kv.Value; i++) d.CardIds.Add(kv.Key);
            return d;
        }

        // ------------------------------------------------------------ 内部：极简 JSON 扫描（⛔ 不引第三方）

        static string QuoteJsonString(string s)
        {
            var sb = new System.Text.StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }

        static string UnescapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('\\') < 0) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i + 1 >= s.Length) { sb.Append(s[i]); continue; }
                char n = s[++i];
                switch (n)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < s.Length)
                        {
                            int code;
                            if (int.TryParse(s.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out code))
                            { sb.Append((char)code); i += 4; }
                        }
                        break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }

        static int SkipWs(string s, int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
            return i;
        }

        /// <summary>`s[i] == '"'` ⇒ 返回**闭引号之后**的下标（不含引号本身）；未闭合 ⇒ `-1`。</summary>
        static int SkipString(string s, int i)
        {
            if (i >= s.Length || s[i] != '"') return -1;
            for (int p = i + 1; p < s.Length; p++)
            {
                if (s[p] == '\\') { p++; continue; }
                if (s[p] == '"') return p + 1;
            }
            return -1;
        }

        /// <summary>`s[i]` 是 `{` 或 `[` ⇒ 返回匹配括号之后的下标；不匹配 ⇒ `-1`。</summary>
        static int MatchBracket(string s, int i, char open, char close)
        {
            if (i >= s.Length || s[i] != open) return -1;
            int depth = 0;
            for (int p = i; p < s.Length; p++)
            {
                char c = s[p];
                if (c == '"') { int e = SkipString(s, p); if (e < 0) return -1; p = e - 1; continue; }
                if (c == open) depth++;
                else if (c == close) { depth--; if (depth == 0) return p + 1; }
            }
            return -1;
        }

        /// <summary>找一个**顶层/嵌套**的 `"key":` 并把 `:` 的下标交回来（跳过字符串里的冒号；键后必须紧跟 `:`）。</summary>
        static int FindKeyColon(string s, string key)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '"') continue;
                int e = SkipString(s, i);
                if (e < 0) return -1;
                string name = UnescapeJsonString(s.Substring(i + 1, e - i - 2));
                int j = SkipWs(s, e);
                if (name == key && j < s.Length && s[j] == ':') return j;
                i = e - 1;
            }
            return -1;
        }

        /// <summary>解析 `"cards": {"<id>": <张数>, …}`（`JsonUtility` 不支持字典）。失败 ⇒ 空表。</summary>
        static List<KeyValuePair<string, int>> ParseCardsMap(string objectJson)
        {
            var list = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrEmpty(objectJson)) return list;
            int colon = FindKeyColon(objectJson, "cards");
            if (colon < 0) return list;
            int i = SkipWs(objectJson, colon + 1);
            if (i >= objectJson.Length || objectJson[i] != '{') return list;
            int end = MatchBracket(objectJson, i, '{', '}');
            if (end < 0) return list;
            int p = i + 1;
            while (p < end - 1)
            {
                p = SkipWs(objectJson, p);
                if (p >= end - 1) break;
                if (objectJson[p] == ',') { p++; continue; }
                if (objectJson[p] != '"') break;
                int ke = SkipString(objectJson, p);
                if (ke < 0) break;
                string key = UnescapeJsonString(objectJson.Substring(p + 1, ke - p - 2));
                p = SkipWs(objectJson, ke);
                if (p >= end - 1 || objectJson[p] != ':') break;
                p = SkipWs(objectJson, p + 1);
                int ns = p;
                if (p < end - 1 && objectJson[p] == '-') p++;
                while (p < end - 1 && objectJson[p] >= '0' && objectJson[p] <= '9') p++;
                int n = 1;
                if (p > ns) int.TryParse(objectJson.Substring(ns, p - ns), out n);
                if (n > 0) list.Add(new KeyValuePair<string, int>(key, n));
                p = SkipWs(objectJson, p);
                if (p < end - 1 && objectJson[p] == ',') p++;
            }
            return list;
        }

        /// <summary>把顶层对象拆成「键 → 值的原文」。只给 `Decks`（`Dictionary&lt;string, AzureDeckInfo&gt;`）用。</summary>
        static List<KeyValuePair<string, string>> SplitTopLevelObject(string text)
        {
            var list = new List<KeyValuePair<string, string>>();
            int end = MatchBracket(text, 0, '{', '}');
            if (end < 0)
            {
                Debug.LogWarning("[DeckStore] `Decks` 的顶层 `{` 没闭合 ⇒ 当作空表");
                return list;
            }
            int p = SkipWs(text, 1);
            while (p < end - 1)
            {
                if (text[p] == ',') { p = SkipWs(text, p + 1); continue; }
                if (text[p] != '"') break;
                int ke = SkipString(text, p);
                if (ke < 0) break;
                string key = UnescapeJsonString(text.Substring(p + 1, ke - p - 2));
                p = SkipWs(text, ke);
                if (p >= end - 1 || text[p] != ':') break;
                p = SkipWs(text, p + 1);
                int vs = p, ve;
                if (p < text.Length && (text[p] == '{' || text[p] == '['))
                    ve = MatchBracket(text, p, text[p], text[p] == '{' ? '}' : ']');
                else if (p < text.Length && text[p] == '"')
                    ve = SkipString(text, p);
                else
                {
                    ve = p;
                    while (ve < end - 1 && text[ve] != ',' && text[ve] != '}') ve++;
                }
                if (ve < 0 || ve > end - 1) break;
                list.Add(new KeyValuePair<string, string>(key, text.Substring(vs, ve - vs)));
                p = SkipWs(text, ve);
                if (p < end - 1 && text[p] == ',') p++;
            }
            return list;
        }

        /// <summary>把顶层数组拆成「每个元素的原文」。只给 `PlayerDecksFast`（`AzureDeckInfo[]`）用。
        /// ⚠️ 逐元素取原文是**必须的** —— `cards` 是 `Dictionary`（`JsonUtility` 不支持），
        /// 拿整段数组去扫 `"cards"` 会把**第一副的卡表**套给每一副。</summary>
        static List<string> SplitTopLevelArray(string text)
        {
            var list = new List<string>();
            int end = MatchBracket(text, 0, '[', ']');
            if (end < 0)
            {
                Debug.LogWarning("[DeckStore] `PlayerDecksFast` 的顶层 `[` 没闭合 ⇒ 当作空表");
                return list;
            }
            int p = SkipWs(text, 1);
            while (p < end - 1)
            {
                if (text[p] == ',') { p = SkipWs(text, p + 1); continue; }
                int vs = p, ve;
                if (text[p] == '{' || text[p] == '[')
                    ve = MatchBracket(text, p, text[p], text[p] == '{' ? '}' : ']');
                else if (text[p] == '"')
                    ve = SkipString(text, p);
                else
                {
                    ve = p;
                    while (ve < end - 1 && text[ve] != ',' && text[ve] != ']') ve++;
                }
                if (ve < 0 || ve > end - 1) break;
                list.Add(text.Substring(vs, ve - vs));
                p = SkipWs(text, ve);
                if (p < end - 1 && text[p] == ',') p++;
            }
            return list;
        }
    }
}
