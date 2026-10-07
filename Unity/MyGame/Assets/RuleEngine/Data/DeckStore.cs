// DeckStore.cs — 卡组存档（读写玩家自己组的卡组）
//
// ⚠️ 本文件用 UnityEngine（`Application.persistentDataPath` + `JsonUtility`）。
//    `Core/` 下的东西一律不碰 Unity，规则/模型在 `Core/DeckRules.cs` 里。
//
// 存在哪：`Application.persistentDataPath/WarpforgeDecks.json`
//   · Windows 编辑器/独立版都在 `%USERPROFILE%\AppData\LocalLow\<公司名>\<产品名>\`
//   · 不进工程、不进仓库、卸载才没 —— 原版的卡组是存在**服务器**上的
//     （`CardDeck.syncedToServer` / `deckId`），单机只能落到本地，这一点和原版不同。
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
                return OverridePath ?? System.IO.Path.Combine(Application.persistentDataPath, FileName);
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
            /// <summary>存档在，但解析出来是空的（`dto == null || dto.decks == null`）。</summary>
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
            try
            {
                var text = File.ReadAllText(path);
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null)
                {
                    note = LoadNote.Empty;
                    detail = "save file has no decks";
                    return new List<PlayerDeck>();
                }
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
        /// `RuleEngine/Editor/DeckRulesTest.cs:217/241/457` 那几条既存断言用（**那个文件不在本件白名单里**，
        /// ⇒ 没跟着改；`DeckRulesTest.cs:243` 的 `note.Contains("失败")` 因此会红，见交件报告）。
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
                var dto = new Dto { version = 1, current = current, decks = decks ?? new List<PlayerDeck>() };
                var json = JsonUtility.ToJson(dto, true);
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
    }
}
