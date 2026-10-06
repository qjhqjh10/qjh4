// DeckLibrary.cs — 玩家的「卡组库」：多套卡组的新建/改名/复制/删除/选中 + 落盘
//
// 对应原版的 `DeckInventory : Inventory<CardDeck>` + `DeckInfoControls`
// （`editButton` / `duplicateButton` / `deleteButton` / `shareButton`，见
// `资料/卡组编辑界面_查证_0920.md` —— 🔴 **2026-10-10 改指（A262）**：原写 `资料/卡组编辑_原版数值与实现方案.md`，那份已被 0920 那份取代）。
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
    ///
    /// <para>🔴 **2026-10-12（A398）**：那几个「改完就落盘」的方法，**返回值必须说得清「落盘成没成」** ——
    /// · 返回 <c>bool</c> 的（<see cref="CommitCurrent"/> / <see cref="Rename"/> / <see cref="Delete"/>）
    ///   一律 **`return Save();`**（`CommitCurrent` 是 A363 先修的，本笔把同族的另两处对齐）。
    ///   原来那两处写的是「`Save();` 然后 `return true;`」⇒ **写失败也报成功**（静默）。
    /// · 返回**对象**的（<see cref="Create"/> / <see cref="Duplicate"/> / <see cref="Add"/>）**装不下**这件事
    ///   （`return Save();` 编不过，改签名会连带断 `Shell/CollectionData.cs` 那几处调用点）
    ///   ⇒ 走 <see cref="SaveOrWarn"/>：`LastError` 里有人话 **+** 一条警告（见那个方法的注释）。</para>
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
        /// <param name="gameMode">本副卡组的模式（`0` 经典 / `13` 遭遇）。照原版
        /// `SelectDecksTab.CreateDeck`：**新建那一刻就把当前模式打进卡组**，之后没有改的路径
        /// （判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。</param>
        public PlayerDeck Create(string name, int gameMode = 0)
        {
            var d = new PlayerDeck(UniqueName(string.IsNullOrEmpty(name) ? "新卡组" : name),
                                   null, null, null, gameMode);
            _decks.Add(d);
            _current = _decks.Count - 1;
            SaveOrWarn("Create");        // 🔴 A398：返回值是对象 ⇒ 落盘成败走 `SaveOrWarn`（原来是裸 `Save();`，把结果吞了）
            return d;
        }

        /// <summary>改名。空名字不接受（免得 UI 上出现一行看不见的东西）。
        /// ⚠️ **2026-10-12（A365）注**：这条**不是编辑窗那条路** —— 编辑窗改的是
        /// `DeckEditorState.SetDeckName`（照原版 `DeckEditingPanel__ChangeName`，**空名照收**，
        /// 合法时再由 `Validate()` 用督军卡名补上）。本方法全仓**只剩 `RuleEngineTest` 在用**
        /// （`RuleEngine/Editor/DeckRulesTest.cs:283-286` 那条「空名字不接受」的断言钉着它）⇒
        /// **别顺手把它也改成收空名**：编辑窗那侧已经对齐原版，这条改了只会把那条既存断言打红。</summary>
        public bool Rename(int index, string newName)
        {
            if (index < 0 || index >= _decks.Count) return false;
            if (string.IsNullOrEmpty(newName)) return false;
            _decks[index].Name = newName;
            // 🔴 **2026-10-12（A398）**：原来是「`Save();` 然后 `return true;`」—— 把落盘结果**吞了**。
            //    照 A363 给 `CommitCurrent` 定下的先例，这里也 **`return Save();`**（`Editor/DeckScene.cs`
            //    的 A398 那一节拿**写不进去的路径**钉着它：`Rename` 必须回 `false`）。
            //    ⚠️ **`false` 现在有两种意思**：① 下标越界 / 名字非法（**没改**）；② 改了但**没落盘**。
            //    调用方要分开这两件事，也得看 `LastError`（写失败时有人话）。
            return Save();
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
            SaveOrWarn("Duplicate");     // 🔴 A398：同上（返回值是对象）
            return copy;
        }

        public bool Delete(int index)
        {
            if (index < 0 || index >= _decks.Count) return false;
            _decks.RemoveAt(index);
            if (_decks.Count == 0) _current = -1;
            else if (_current >= _decks.Count) _current = _decks.Count - 1;
            else if (_current > index) _current--;      // 删的是前面的，选中项往前挪一位
            // 🔴 **2026-10-12（A398）**：原来是「`Save();` 然后 `return true;`」—— 把落盘结果**吞了**
            //    （存档写不进去也报「删成功」，玩家下次打开发现它还在）。照 `CommitCurrent` 的先例 **`return Save();`**。
            //    ⚠️ 同 `Rename`：`false` 现在有两种意思（越界没删 / 删了但没落盘），分清楚要看 `LastError`。
            //    ⚠️ **如实标注**：`Shell/CollectionData.DeleteDeck` 与 `DeckInfoPopup` 把两种意思混着用
            //    （失败时打印的是「删不了」），那是**另一笔账**（越了本件白名单，本件只在数据层把话说出来）。
            return Save();
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
            // ⚠️ **逐字段拷**：新加字段忘了在这一行补 = **换了卡背、存了、再打开就没了**（静默丢数据）。
            dst.CardbackId = deck.CardbackId;
            // 🆕 2026-09-26：模式**只在新建立时定**（照原版），所以这里**不跟着编辑器的副本走** ——
            //   编辑器改的是内容，改不了模式（原版也没有那条路）。留着这一行是为了「导入/复制」那条路
            //   能把模式带进库；`CommitCurrent` 的调用方（`DeckRuntime` 保存）传进来的副本本来就带着同一个值。
            dst.GameMode = deck.GameMode;
            // 🔴 **2026-10-12（A363）**：这里原来是「`Save();` 然后 `return true;`」——
            //    **返回值把落盘结果吞了**（写失败也报成功）。A363 起调用方（`DeckRuntime.CommitDeck`）
            //    **要靠这个返回值**决定「脏标记清不清」和要不要出声「保存失败」，所以照实回传。
            return Save();
        }

        /// <summary>落盘。失败不吞 —— 写进 <see cref="LastError"/>，UI 该显示出来。</summary>
        public bool Save()
        {
            string err;
            bool ok = DeckStore.SaveAll(_decks, _current, out err);
            LastError = ok ? null : err;
            return ok;
        }

        /// <summary>落盘；**失败就出声** —— 给「返回值是对象、装不下落盘成败」的那三个方法用。
        ///
        /// <para>🔴 **2026-10-12（A398）**：<see cref="Create"/> / <see cref="Duplicate"/> / <see cref="Add"/>
        /// 返回的是它们造出来的对象（`PlayerDeck`，不是 `bool`）⇒ **`return Save();` 编不过**，
        /// 那三处原来写的是「`Save();` + `return &lt;对象&gt;;`」= 把落盘结果吞了。本笔补两条出口：
        /// ① `Save()` 内部照旧把原因写进 <see cref="LastError"/>（卡组编辑窗的页头 `_storeErr` 会显示它）；
        /// ② 这里再打一条**警告** —— 调用点哪怕完全不看 `LastError`，控制台也不会一声不响
        /// （红线「不许静默失败」）。</para>
        ///
        /// <para>⛔ **别为了回传 `bool` 去改那三个方法的签名** —— `Shell/CollectionData.cs` 那几处调用点
        /// 会跟着断。真要一个 bool，用 <see cref="CommitCurrent"/>（编辑窗那条路就是它）。
        /// 🔴 **2026-10-13（A647）就地更正（铁律 5）**：本句原来还缀着一个括号
        /// 「（它们现在是「调完再自己 `Lib.Save()` 一次」的写法）」—— **那半句已经过期**：
        /// A503/A611 之后 `Shell/CollectionData.cs` 那三处**都不再自己 `Lib.Save()`** 了
        /// （`Shell/CollectionData.cs:194` / `:221` / `:243` 各挂着一句「⛔ 别在后面再加一次 `Lib.Save()`」）。</para>
        ///
        /// <para>🔴 **2026-10-13（A647）就地更正（铁律 5）** —— 这一段原文是
        /// 「⚠️ **如实标注（本笔没做的那一半）**：`Shell/CollectionData.cs` 的 `CreateDeck` / `DuplicateDeck` /
        /// `ImportDeck` **既不读返回值、也不读 `LastError`** ⇒ 写盘失败时玩家看到的是「操作成功」，而盘上没变。
        /// **那是另一笔账**（越了本件的白名单），本笔只在数据层把话说出来。」
        /// <br/>**实际情况（A503 / A611 起，那三处全读了）**：`CreateDeck`（`CollectionData.cs:240`，返回卡组名、
        /// 落盘失败回空串）· `DuplicateDeck`（`:190`，同上，另有出口 `LastDuplicateError`）·
        /// `ImportDeck`（`:215`，返回名 + `out string why`）—— 三处都读 `Lib.LastError` 并 `Debug.LogWarning`；
        /// 调用点也读返回值（例：`Shell/CollectionWindow.cs:2701` 的 `string name = CollectionData.CreateDeck();`）。
        /// <br/>**错因**：写这条注释时（A398）那几处**确实还没读**，A503/A611 补上之后**没有回头改这里**
        /// ⇒ 留下的是一条「其实已经做完的欠账」。⛔ **别照这段旧话去「补做」那三处**。</para></summary>
        bool SaveOrWarn(string who)
        {
            bool ok = Save();
            if (!ok)
                UnityEngine.Debug.LogWarning("[DeckLibrary] `" + who + "` 的内存改动**已生效**，但**落盘失败**："
                                           + (string.IsNullOrEmpty(LastError) ? "(没给原因)" : LastError)
                                           + " —— 它的返回值是对象、装不下这件事（见 `SaveOrWarn` 的注释）。");
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
            sb.Append(';').Append(d.GameMode);   // gameMode：照原版 `CardDeck__Serialize.c:68-75`（null 写 0）
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
            // 末尾那个整数 = `CardDeck.gameMode` —— 🆕 2026-09-26 起**读进卡组**（原来读了就丢）。
            // 照原版 `CardDeck__DeserializeDeckString.c:127,133-135`：它把整个 Nullable 写回卡组
            // ⇒ 导入一条原版玩家分享出来的**遭遇卡组串**，模式跟着进来。
            int gameMode = 0;
            if (semi >= 0)
            {
                if (!int.TryParse(text.Substring(semi + 1).Trim(), out gameMode))
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

            return new PlayerDeck(string.IsNullOrEmpty(name) ? "导入的卡组" : name, hero, def, rest, gameMode);
        }

        /// <summary>加一套外部来的卡组（导入用）。名字重了自动加序号。</summary>
        public PlayerDeck Add(PlayerDeck d)
        {
            if (d == null) return null;
            d.Name = UniqueName(string.IsNullOrEmpty(d.Name) ? "导入的卡组" : d.Name);
            _decks.Add(d);
            _current = _decks.Count - 1;
            SaveOrWarn("Add");           // 🔴 A398：同上（返回值是对象）
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
