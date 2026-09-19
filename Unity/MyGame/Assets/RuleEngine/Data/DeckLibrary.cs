// DeckLibrary.cs — 玩家的「卡组库」：多套卡组的新建/改名/复制/删除/选中 + 落盘
//
// 对应原版的 `DeckInventory : Inventory<CardDeck>` + `DeckInfoControls`
// （`editButton` / `duplicateButton` / `deleteButton` / `shareButton`，见
// `资料/卡组编辑_原版数值与实现方案.md`）。
//
// ⚠️ 和原版不一样的地方（**别当 bug**）：
//   · 原版卡组存在**服务器**（`CardDeck.syncedToServer` / `deckId`），这里落本地文件；
//   · 原版 `DeckInfoControls` 还有 `shareButton` / `shareOnChatButton`（导出卡组串分享给别人），
//     单机没有分享对象，**不做**；
//   · 原版的「套数上限」在服务器配置里，本地查不到，**这里不设上限**。
//
// ⚠️ 本文件用 UnityEngine（`JsonUtility` 走 `Data/DeckStore`）。`Core/` 下的东西一律不碰 Unity。
using System;
using System.Collections.Generic;
using System.Text;

namespace RuleEngine
{
    /// <summary>
    /// 卡组库。**改这个类就要加断言**（`DeckScene.Run` 里那一段）。
    /// 所有会改内容的操作都会立刻落盘（`Save`），失败时不吞掉 —— `LastError` 里有人话。
    /// </summary>
    public class DeckLibrary
    {
        readonly List<PlayerDeck> _decks = new List<PlayerDeck>();
        int _current = -1;

        /// <summary>上一次落盘失败的原因（成功时是 null）。UI 上该把它显示出来。</summary>
        public string LastError { get; private set; }

        /// <summary>读存档目录里的全部卡组。读不到就是空库（**不抛异常**）。</summary>
        public static DeckLibrary Load()
        {
            var lib = new DeckLibrary();
            string note;
            int current;
            var list = DeckStore.LoadAll(out note, out current);
            if (list != null) lib._decks.AddRange(list);
            // 「上次在编辑哪一套」也存了 —— 重新打开还停在那一套上
            if (list != null && list.Count > 0) lib._current = (current >= 0 && current < list.Count) ? current : 0;
            lib.LastError = (note != null && note.Contains("失败")) ? note : null;
            return lib;
        }

        public IReadOnlyList<PlayerDeck> Decks { get { return _decks; } }
        public int Count { get { return _decks.Count; } }

        /// <summary>当前选中的那套在新卡组里是第几套（-1 = 一套都没有）。</summary>
        public int CurrentIndex { get { return _current; } }

        /// <summary>当前选中的卡组（没有就是 null）。</summary>
        public PlayerDeck Current
        {
            get { return (_current >= 0 && _current < _decks.Count) ? _decks[_current] : null; }
        }

        public void Select(int index)
        {
            if (index >= 0 && index < _decks.Count) _current = index;
        }

        /// <summary>新建一套空卡组并选中它。名字重了会自动加序号。</summary>
        public PlayerDeck Create(string name)
        {
            var d = new PlayerDeck(UniqueName(string.IsNullOrEmpty(name) ? "新卡组" : name), null, null, null);
            _decks.Add(d);
            _current = _decks.Count - 1;
            Save();
            return d;
        }

        /// <summary>改名。空名字不接受（免得 UI 上出现一行看不见的东西）。</summary>
        public bool Rename(int index, string newName)
        {
            if (index < 0 || index >= _decks.Count) return false;
            if (string.IsNullOrEmpty(newName)) return false;
            _decks[index].Name = newName;
            Save();
            return true;
        }

        /// <summary>复制一套（原版 `duplicateButton` 那个）。返回新的那套。</summary>
        public PlayerDeck Duplicate(int index)
        {
            if (index < 0 || index >= _decks.Count) return null;
            var src = _decks[index];
            var copy = src.Clone();
            copy.Name = UniqueName(src.Name + " 副本");
            _decks.Add(copy);
            _current = _decks.Count - 1;
            Save();
            return copy;
        }

        public bool Delete(int index)
        {
            if (index < 0 || index >= _decks.Count) return false;
            _decks.RemoveAt(index);
            if (_decks.Count == 0) _current = -1;
            else if (_current >= _decks.Count) _current = _decks.Count - 1;
            else if (_current > index) _current--;      // 删的是前面的，选中项往前挪一位
            Save();
            return true;
        }

        /// <summary>把当前选中的那套替换成 <paramref name="deck"/>（编辑器改完调它落盘）。
        /// 传进来的可能是别处的对象，所以整份拷进去。</summary>
        public bool CommitCurrent(PlayerDeck deck)
        {
            if (deck == null || _current < 0 || _current >= _decks.Count) return false;
            var dst = _decks[_current];
            dst.Name = deck.Name;
            dst.WarlordId = deck.WarlordId;
            dst.DefensiveId = deck.DefensiveId;
            dst.CardIds = new List<string>(deck.CardIds ?? new List<string>());
            Save();
            return true;
        }

        /// <summary>落盘。失败不吞 —— 写进 <see cref="LastError"/>，UI 该显示出来。</summary>
        public bool Save()
        {
            string err;
            bool ok = DeckStore.SaveAll(_decks, _current, out err);
            LastError = ok ? null : err;
            return ok;
        }

        // ==================================================================
        //  卡组串 —— **原版格式**（🔴 2026-09-19 从「我们自己定的」改过来）
        //
        //  原版格式（**逐字**，出处 `decomp_full/CardDeck__Serialize.c` / `__DeserializeDeckString.c`）：
        //
        //      Base64( UTF8(  <卡组名，其中的 ':' 换成 "%3A">  ':' <卡id>  ':' <卡id> …  ';' <gameMode>  ) )
        //
        //   · **无版本号 · 无校验位 · 无压缩**
        //   · ⚠️ **转义只做一处**：只有**卡组名**里的 `:` 会被换成 `%3A`；
        //     卡 id 与 `;` 都**不转义** ⇒ 名字里带 `;` 会把串弄坏（原版也这样，照抄）
        //   · 末尾那个整数 = `CardDeck.gameMode`（`Nullable<PlayModes>`，**为 null 时写 0**）
        //   · 卡序 = `CardDeck.GetLibraryInFull()`：**防御卡 → 督军 → 其余**
        //     （实据：函数体里两次 `List.Insert(0, …)`；字段 `defensiveCard // 0x48` ·
        //       `deckHero // 0x40` · `cardLibrary // 0x58` ⇒ 最终顺序 `[0x48, 0x40, 0x58…]`）
        //   · 导入端**按卡的类型**把防御卡/督军摘出来（`SpellType.DefensiveCard = 210` /
        //     `CardTypeOptions.Hero = 10`），**不是按位置** ⇒ 位置换了照样读得对
        //
        //  🔴 **为什么值得改成原版格式**：我们卡池的 id **与原版是同一个空间** ——
        //     原版自己的预组卡资产里逐字写着 `"AM3"`（督军）/ `cardLibraryIds:["AM12",…]` / `"GOF33"`，
        //     与 `cards_engine.json` 里的 id **一模一样**（出处见 `规则引擎_进度与交接.md:608`）。
        //     ⇒ 照原版格式做，**原版玩家分享出来的串我们直接读得了**；自己定一套就永远读不了。
        // ==================================================================

        /// <summary>卡组名里 `:` 的转义写法（原版 `CardDeck__Serialize.c:40` 的字面量 `0x1842594f8`）。</summary>
        public const string ColonEscape = "%3A";

        /// <summary>导出成**原版格式的卡组串**（可直接贴给别人 / 从原版贴过来）。</summary>
        public static string ExportString(PlayerDeck d)
        {
            if (d == null) return "";
            var sb = new StringBuilder();
            sb.Append((d.Name ?? "").Replace(":", ColonEscape));
            // 卡序照原版 `GetLibraryInFull()`：防御卡 → 督军 → 其余
            if (!string.IsNullOrEmpty(d.DefensiveId)) sb.Append(':').Append(d.DefensiveId);
            if (!string.IsNullOrEmpty(d.WarlordId)) sb.Append(':').Append(d.WarlordId);
            foreach (var id in d.CardIds ?? new List<string>())
                if (!string.IsNullOrEmpty(id)) sb.Append(':').Append(id);
            sb.Append(';').Append(0);        // gameMode：我们没有这个概念，照原版「null 写 0」
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>上一次导入里**被丢掉的 id**（卡池里查不到的）。
        /// 🔴 原版对查不到的卡是**静默丢掉**；项目红线是「不许静默失败」，而玩家看到的恰恰是
        /// 「牌少了」—— 所以**照原版丢，但记在这里**，UI 要能显示出来（现在还没有导入弹窗，先留着接口）。
        /// 每次 `ImportString` 都会重置它。</summary>
        public static List<string> LastDroppedIds { get; private set; } = new List<string>();

        /// <summary>解析导入串。**解析不了就返回 null**（不猜、不半懂不懂地建半套）。
        /// 判据：不是合法 Base64 / 不是合法 UTF-8 ⇒ null。卡池里查不到的**单张卡**按原版丢掉，
        /// 丢掉的记在 <see cref="LastDroppedIds"/> 里。</summary>
        public static PlayerDeck ImportString(string s, Func<string, CardDef> lookup)
        {
            LastDroppedIds = new List<string>();
            if (string.IsNullOrEmpty(s)) return null;

            string text;
            try
            {
                var bytes = Convert.FromBase64String(s.Trim());
                text = new UTF8Encoding(false, true).GetString(bytes);   // 非法 UTF-8 会抛 ⇒ 不算合法串
            }
            catch { return null; }

            // 先按 ';' 切：「名字+卡」和末尾那个整数（原版 `DeserializeDeckString.c:48,59`）
            int semi = text.IndexOf(';');
            string head = semi >= 0 ? text.Substring(0, semi) : text;
            // 末尾那个整数（gameMode）**读了不用** —— `PlayerDeck` 没有这个字段，如实记着别假装支持
            if (semi >= 0)
            {
                int gm;
                if (!int.TryParse(text.Substring(semi + 1).Trim(), out gm))
                    return null;                                        // 尾巴不是整数 ⇒ 不是这个格式
            }
            else return null;                                           // 连 ';' 都没有 ⇒ 不是这个格式

            var parts = head.Split(':');
            string name = parts.Length > 0 ? parts[0].Replace(ColonEscape, ":") : "";

            string hero = null, def = null;
            var rest = new List<string>();
            for (int i = 1; i < parts.Length; i++)
            {
                string id = parts[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (lookup == null) { rest.Add(id); continue; }

                var c = lookup(id);
                if (c == null) { LastDroppedIds.Add(id); continue; }     // 原版也丢，但我们要看得见
                // ⚠️ **按类型摘，不按位置**（原版 `DeserializeDeckString.c:147,155`）——
                //    位置换了照样读得对；同名多张时第一张当专属卡、其余当普通卡（不丢）
                if (c.Type == "defence") { if (def == null) def = id; else rest.Add(id); }
                else if (c.Type == "hero") { if (hero == null) hero = id; else rest.Add(id); }
                else rest.Add(id);
            }

            return new PlayerDeck(string.IsNullOrEmpty(name) ? "导入的卡组" : name, hero, def, rest);
        }

        /// <summary>加一套外部来的卡组（导入用）。名字重了自动加序号。</summary>
        public PlayerDeck Add(PlayerDeck d)
        {
            if (d == null) return null;
            d.Name = UniqueName(string.IsNullOrEmpty(d.Name) ? "导入的卡组" : d.Name);
            _decks.Add(d);
            _current = _decks.Count - 1;
            Save();
            return d;
        }

        /// <summary>库内不重名。重了就 `名字 2` / `名字 3` …</summary>
        public string UniqueName(string want)
        {
            bool Taken(string n)
            {
                foreach (var d in _decks) if (string.Equals(d.Name, n, StringComparison.Ordinal)) return true;
                return false;
            }
            if (!Taken(want)) return want;
            for (int i = 2; i < 1000; i++)
            {
                var n = want + " " + i;
                if (!Taken(n)) return n;
            }
            return want + " " + Guid.NewGuid().ToString("N").Substring(0, 4);
        }
    }
}
