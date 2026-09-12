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

        /// <summary>从一份卡组串导入（原版 `ImportDeckPopup` 那条路）。
        /// 格式：`名称|督军id|防御卡id|卡1,卡2,...` —— **我们自己定的**，原版的 `CardDeck.Serialize`
        /// 编码方式在客户端 DLL 里没反编译出来（见方案文档六-3），所以这不是原版格式。</summary>
        public const char ImportSep = '|';

        public static string ExportString(PlayerDeck d)
        {
            if (d == null) return "";
            return string.Join(ImportSep.ToString(), new[]
            {
                d.Name ?? "", d.WarlordId ?? "", d.DefensiveId ?? "",
                string.Join(",", d.CardIds ?? new List<string>()),
            });
        }

        /// <summary>解析导入串。**解析不了就返回 null**（不猜、不半懂不懂地建半套）。
        /// `lookup` 用来验证卡 id 在不在池子里 —— 不在就整条拒掉。</summary>
        public static PlayerDeck ImportString(string s, Func<string, CardDef> lookup)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var parts = s.Split(ImportSep);
            if (parts.Length != 4) return null;

            var ids = new List<string>();
            if (!string.IsNullOrEmpty(parts[3]))
                foreach (var id in parts[3].Split(','))
                    if (!string.IsNullOrEmpty(id)) ids.Add(id.Trim());

            // 判据分两档，别混：
            //   · 字段**空的** → 只是「还没配完的卡组」，**允许**导进来再补（界面上会显示还差什么）
            //   · 字段**非空但池子里没有** → 这是一条坏串，**整条拒掉**（不建半套）
            if (lookup != null)
            {
                if (!string.IsNullOrEmpty(parts[1]) && lookup(parts[1]) == null) return null;
                if (!string.IsNullOrEmpty(parts[2]) && lookup(parts[2]) == null) return null;
                foreach (var id in ids) if (lookup(id) == null) return null;
            }

            return new PlayerDeck(string.IsNullOrEmpty(parts[0]) ? "导入的卡组" : parts[0],
                                  parts[1], parts[2], ids);
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
