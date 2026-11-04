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
    /// 所有会改内容的操作都会立刻落盘（`Save`），失败时不吞掉 —— `LastError` 里**说得出原因**。
    /// ⚠️ 🔴 **2026-10-18（`G8`）就地更正**：本行原来写「`LastError` 里**有人话**」——
    /// 从那一天起它是**诊断串**（技术用，`DeckStore` 不再拼中文整句），
    /// **不再是给玩家看的主文案**；显示层走 `Loc`（读存档 → <see cref="LastLoadIssueTerm"/>；
    /// 落盘失败 → `CardPresentation.DeckRuntime.TermSaveFailed`）。
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

        /// <summary>
        /// 🔴 **2026-10-18（`G3` · 真差异 ②）：「读存档」这件事的**类型** —— 判据是它，**不是文案**。
        ///
        /// **改之前**：`LastError = note.Contains("失败") ? note : null` —— 拿**中文字符串**当判据。
        /// 那几条 `note` 是 `DeckStore.LoadAll` 写给**人**看的句子；一旦它们走本地化
        /// （英文档 / 改一次措辞），`Contains("失败")` 恒 false ⇒ `LastError` **永远是 null**
        /// ⇒ 卡组编辑窗底部那条报错横幅（`DeckRuntime._storeErr`）**静默消失**：玩家看到的是
        /// 「读取成功、只是卡组没了」。
        /// ⚠️ **2026-10-18（`G8`）补一句**：那条横幅（`_storeErr`）2026-10-17 已按 **D10 删件**删掉
        /// （原版侧栏没有它），而 `DeckStore` 那三条 `note` 也**改成了诊断串**（不再是中文整句）
        /// ⇒ 上面那个「静默消失」的**入口**今天已经不存在了，这条判据仍然要留着：
        /// 它挡的是「**类型跟着文案走**」这个形状，而不是某一条具体路径。
        ///
        /// **现在**：类型由 <see cref="ClassifyLoad"/> 判（结构性判据 = 「有没有存档文件」+
        /// 「`DeckStore` 有没有话要说」），文案只是 <see cref="LastError"/> 里的那句话
        /// ⇒ **换语言 / 换措辞都不会改类型**。
        /// 🔴 **2026-10-18（`G8`）**：显示层要挑词条就走 <see cref="LastLoadIssueTerm"/>
        /// （**从错误码来**），⛔ 别读 `LastError` 那串字。
        /// </summary>
        public enum DeckLoadIssue
        {
            /// <summary>读到了（`DeckStore` 没话说）。</summary>
            None = 0,
            /// <summary>**还没有存档**（第一次跑）—— ✅ **不是失败**（不该弹报错横幅）。</summary>
            NoSaveFile = 1,
            /// <summary>存档在、但读不出来 / 解析不了 —— 🔴 **要出声**（写进 <see cref="LastError"/>）。</summary>
            Failed = 2,
        }

        /// <summary>上次 <see cref="Load"/> 读存档的结果类型（**判类型用它，判文案用 <see cref="LastError"/>**）。</summary>
        public DeckLoadIssue LastLoadIssue { get; private set; }

        /// <summary>上次 <see cref="Load"/> 读存档的**错误码**（= `DeckStore.LoadNote`，
        /// 🔴 **2026-10-18 `G8` 新增**）—— 它比 <see cref="LastLoadIssue"/> 细一档
        /// （`Empty` 与 `ReadFailed` 在那个枚举里都是 `Failed`）。
        ///
        /// 🔴 **显示层挑词条走 <see cref="LastLoadIssueTerm"/>（它从这个码来）**，
        /// ⛔ **别去读 <see cref="LastError"/> 那串字** —— 那一串是**诊断**（技术用），
        /// 拿它当显示主路 = 把英文/中文技术串喷到界面上（`G3` 拆的就是这个坑）。</summary>
        public DeckStore.LoadNote LastLoadCode { get; private set; }

        /// <summary>读存档出问题时该显示的**原版词条键**（`"CustomErrors/ErrorLoadDeck"`）；
        /// 读到了 / 第一次跑 ⇒ `null`（**那两个都不该显示任何东西**）。
        ///
        /// ⚠️ **显示层必须走 `Loc.T(它)`** —— 键名照原版（出处与载体写在
        /// `DeckStore.ErrorLoadDeckTerm` / `DeckStore.TermKeyOf` 上）。
        /// 消费侧今天在 `BattleDriver.PickSavedDeck`（它那个 `note`，
        /// 出在开局提示行上）—— 那一处在 `Battle/**`（**不在 `G8` 的白名单**）⇒ 只留指针，见交件报告。</summary>
        public string LastLoadIssueTerm { get { return DeckStore.TermKeyOf(LastLoadCode); } }

        /// <summary>
        /// **「这次读存档算不算失败」的【唯一】判据** —— `Load()` 与自检**共用这一处**
        /// （⛔ 别再往 `Load` 里写第二份 `Contains("失败")`：两处写同一条规则 = 迟早不一致）。
        ///
        /// 判据是**结构性的**、与文案无关：
        ///   · `note == null` ⇒ `DeckStore` 无话可说 = 读到了一份存档（哪怕是 0 套的合法存档）⇒ `None`；
        ///   · `note != null` + **存档文件在** ⇒ 文件在却读不出 = **失败**（解析不了 / 读异常，
        ///     `DeckStore.LoadAll` 那两条 `note` 都在这个分支里）；
        ///   · `note != null` + **文件不在** ⇒ 第一次跑（`DeckStore` 的 `LoadNote.NoSaveFile`）⇒ **不是失败**。
        /// ⚠️ `note` 里的**字句一个字都没参与判断** —— 这正是本次整改要的那条性质。
        /// 🔴 **2026-10-18（`G8`）**：`note` 现在**已经不是中文句子**，而是**诊断串**
        /// （`"no save file"` / `"save file has no decks"` / `"read failed: …"`）——
        /// 本判据**照旧不看它写了什么**，只看它**是不是 null**（那是「`DeckStore` 有没有话要说」这个信号）。
        /// </summary>
        /// <param name="note">`DeckStore.LoadAll` 带出来的那句**诊断**（`null` = 没话说）。</param>
        /// <param name="saveFileExisted">**调用 `LoadAll` 之前**存档文件在不在。</param>
        public static DeckLoadIssue ClassifyLoad(string note, bool saveFileExisted)
        {
            if (note == null) return DeckLoadIssue.None;
            return saveFileExisted ? DeckLoadIssue.Failed : DeckLoadIssue.NoSaveFile;
        }

        /// <summary>读存档目录里的全部卡组。读不到就是空库（**不抛异常**）。</summary>
        public static DeckLibrary Load()
        {
            var lib = new DeckLibrary();
            DeckStore.LoadNote note;
            string detail;
            int current;
            // ⚠️ 「文件在不在」必须在 **读档（`Parse` / 原来的 `LoadAll`）之前**取 —— 读取失败/成功都会改动文件系统状态，
            //    取在之后就会把「第一次跑」和「文件在但读不出来」判反。🆕 2026-10-30（`B99_W7` 就地订正 `B99_W6`）：读取改走**注入钩子** `ReadLegacyText`（Shell 侧注册成 `SaveCenter.ReadThroughLegacy` —— **先中央档、再旧档**）⇒ 「文件在不在」**还要 OR 上中央档**；🔴 但那个 OR **只在读钩子也在时才问**（`CentralSaveExists` 由注册方按 `StoreOn` 收口），否则会与 `ReadThroughLegacy` 的 `StoreOn` 支自相矛盾（= W6 的假阳性，见类尾那一段）。
            bool existed = System.IO.File.Exists(DeckStore.Path) || (ReadLegacyText != null && CentralSaveExists != null && CentralSaveExists());
            var list = ReadDecksThroughHooks(out note, out detail, out current);
            if (list != null) lib._decks.AddRange(list);
            // 「上次在编辑哪一套」也存了 —— 重新打开还停在那一套上
            if (list != null && list.Count > 0) lib._current = (current >= 0 && current < list.Count) ? current : 0;   lib.ApplyCurrentDeckKey();   // ✅ 2026-11-04 · B141 · UI-05 D6：`CurrentDeck`（卡组 id 字符串）优先于 payload 里的旧 `current:int`（原版 `PlayerDataManager.cs:11472`）
            // 🔴 **2026-10-18（`G3`）**：类型走 `ClassifyLoad`（**不看汉字**）。
            // 🔴 **2026-10-18（`G8`）**：上面那个 `detail` 现在**是诊断串**（不是给人看的中文句子），
            //    `ClassifyLoad` 的判据照旧**只**看「有没有话」+「文件在不在」两件结构性的事
            //    ⇒ **两处都不看文案**。另存一份错误码（`LastLoadCode`）给显示层挑词条用。
            lib.LastLoadCode = note;
            lib.LastLoadIssue = ClassifyLoad(detail, existed);
            // `LastError` 里装的是**诊断**（技术用），⛔ 不是显示主路 —— 显示层走 `LastLoadIssueTerm`。
            lib.LastError = lib.LastLoadIssue == DeckLoadIssue.Failed ? detail : null;
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
        [System.Obsolete("潜伏误用（B106_W7 2026-10-31）：原版「新建」不入库（A1697 / SelectDecksTab.CreateDeck ⇒ GetEmptyDeck + 直接开窗）。生产请用 NewUnsaved() 造、走 Done 那条 Validate→Insert 链；本口只服务自检夹具。")] public PlayerDeck Create(string name, int gameMode = 0)
        {
            var d = new PlayerDeck(UniqueName(string.IsNullOrEmpty(name) ? "新卡组" : name),
                                   null, null, null, gameMode); d.SetDeckArmyNeutral();   // 🆕 2026-11-01（`B111_R1WB`）：原版 `GetEmptyDeck` ⇒ `deckArmy = Neutral`
            _decks.Add(d);
            _current = _decks.Count - 1;
            SaveOrWarn("Create");        // 🔴 A398：返回值是对象 ⇒ 落盘成败走 `SaveOrWarn`（原来是裸 `Save();`，把结果吞了）
            return d;
        }

        /// <summary>造一副**空卡组，但【不登记进库】** —— 原版 `CardDeck.GetEmptyDeck()` 那一跳
        /// （`SelectDecksTab__CreateDeck.c:17` 在开编辑器**之前**唯一做的事；`CardDeck__GetEmptyDeck.c:28-32`
        /// 逐字段：督军 `+0x40 = 0` · 防御 `+0x48 = 0` · 模式 `+0x50 = 0`）。
        ///
        /// <para>🔴 **2026-10-12（`A1697`）新开这个口，为的是「新建卡组不入库」那条原版链**
        /// （用户 2026-10-12 点名：「没选战将的卡组…这按照原版复刻」）——
        /// 原版「新建」**从不入库**：`SelectDecksTab__CreateDeck.c` 造完空卡组**直接开编辑窗**
        /// （`:28` 那句 `WindowsManager__OpenWindow&lt;DeckEditingWindow&gt;(…)`），入库的唯一路径是
        /// `DeckEditingWindow__TrySaveDeck.c:100 UploadDeck`（**先 `:80 ValidateDeck`**，不合法 `:81-98`
        /// 弹错误窗后 `return`、绝不走到 `:100`）⇒ 原版「新建 → 不选战将 → 退出」**什么都不剩**。</para>
        ///
        /// <para>⚠️ **与 `Create` 的分工**：那个口是「造 + 入库 + 落盘」三合一（今天只剩演示卡组 /
        /// 自检夹具在走）；本口**只造**，入库走 `Insert`（由卡组编辑窗 `Done` 那一拍调）。
        /// ⛔ **别把本口写成「`Create` 去掉 `Save` 再共用一个实现」** —— 那样两个口的语义会互相牵制
        /// （旧 `Create` 的调用点要的恰恰是「建完就在库里」）。</para></summary>
        public PlayerDeck NewUnsaved(string name, int gameMode = 0)
        {
            // ⚠️ 重名去重**仍然走 `UniqueName`**（它只读 `_decks`、不写）—— 这样「不入库的新卡组」
            //    与库里已有的同名卡组**当场就区分开**（否则入库那一刻会平白多一个「 2」）。
            var nd = new PlayerDeck(UniqueName(string.IsNullOrEmpty(name) ? "新卡组" : name),
                                  null, null, null, gameMode); nd.SetDeckArmyNeutral(); return nd;   // 🆕 2026-11-01（`B111_R1WB`）：原版 `GetEmptyDeck` ⇒ `deckArmy = Neutral`
        }

        /// <summary>把一副**还没入库**的卡组登记进库并落盘 —— 入库的唯一入口，照原版
        /// `DeckEditingWindow__UploadDeck.c`（`:63 CardDeck__CopyDeck` 之后 `:74 PlayerDataManager__UploadUnsyncedDeck`）。
        ///
        /// <para>🔴 **2026-10-12（`A1697`）**：与 `Create` 的差别只有一处 —— **对象是外面造好递进来的**
        /// （= 编辑器 `Done` 那一刻正在编辑的那一副）⇒ 本方法**不造对象**，只做「加进 `_decks` +
        /// 把 `_current` 指到它 + 落盘」。名字的重名去重由调用方保证（`NewUnsaved` 已经过 `UniqueName`）。</para>
        ///
        /// <para>⚠️ **落盘失败时【不】把它摘出去**（与 `Create` / `Duplicate` 一致：内存里留着、
        /// `LastError` 说得出原因、返回 `false`）—— A503 那条口径就是「说没成比说成功诚实」。
        /// ⚠️ 调用方**别把 `false` 当成「没入库」再插一遍**（`DeckRuntime.CommitDeck` 那一侧
        /// **无论成败都会结束「未入库」状态**，理由写在那边）。</para></summary>
        public bool Insert(PlayerDeck deck)
        {
            if (deck == null) return false;
            _decks.Add(deck);
            _current = _decks.Count - 1;
            return Save();               // 照 `Rename` / `Delete` / `CommitCurrent` 的先例：**不吞**落盘结果
        }

        /// <summary>改名。空名字不接受（免得 UI 上出现一行看不见的东西）。
        /// ⚠️ **2026-10-12（A365）注**：这条**不是编辑窗那条路** —— 编辑窗改的是
        /// `DeckEditorState.SetDeckName`（照原版 `DeckEditingPanel__ChangeName`，**空名照收**，
        /// 合法时再由 `Validate()` 用督军卡名补上）。本方法全仓**只剩 `RuleEngineTest` 在用**
        /// （`RuleEngine/Editor/DeckRulesTest.cs` 里 `lib.Rename(0, "")` 那条「空名字不接受」的断言钉着它）⇒
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

        /// <summary>复制一套（原版 `duplicateButton` 那个）。返回新的那套。
        ///
        /// <para>🔴 **2026-10-25（完全复刻 · 执行代理 A · 收藏域代理 C 的改动请求 ①）：名字后缀改成原版的 `+`。**
        /// 判据 = `DeckInfoPopup.DuplicateDeck`（`DeckInfoPopup.cs:1084` 现读）：
        /// `string text = copiedDeck2.deckName + &quot;+&quot;; copiedDeck2.deckName = text;`
        /// —— **原文逐字就是 `名字 + "+"`，没有查重、没有序号**。
        /// ⚠️ 原来我们写的是 `+ " 副本"`（自造）。
        /// ⚠️ **为什么可以不做唯一化**：本库的卡组**全部按下标寻址**（`Rename(index)` / `Delete(index)` /
        ///    `_current` 也是下标；落盘 `DeckStore` 存的是**有序列表**、不按名字反查）
        ///    ⇒ 同名不会选错卡组。原版也只是把 `syncedToServer` 置假后上传，服务端允许重名。
        ///    ⛔ 别为「好看」再加序号 —— 那会与卡面上看到的原版行为不符。</para></summary>
        public PlayerDeck Duplicate(int index)
        {
            if (index < 0 || index >= _decks.Count) return null;
            var src = _decks[index];
            var copy = src.Clone();
            copy.Name = src.Name + "+";      // 原版 `deckName + "+"`（见上面 doc）
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

        /// <summary>
        /// **按给定顺序重排卡组库**（收藏页 Deck 页的「拖拽排序」落点）。
        ///
        /// <para>🆕 **2026-10-25（完全复刻 · 执行代理 A · 收藏域代理 `C` 的改动请求 ③ = `K3 P2-③`）** 新加。</para>
        ///
        /// <para>**判据（第一权威）**：原版 `DeckCollectionDisplay.SaveDeckOrder` →
        /// `DeckInventory.UpdateOrder(List&lt;CardDeck&gt; newOrder)`（`DeckInventory.cs:489`）
        /// → `PlayerDataManager.OrderDecks` 落盘；拖拽的**数据算法**来自
        /// `CollectionDisplay.ReorderCollectionByHierarchy`（`CollectionDisplay.cs:969`，现读）
        /// = **Remove + Insert**（不是交换、不是整体重建）+ 保持滚动位置（那一半是 UI）。</para>
        ///
        /// <para>🔴 **与原版的一处【不得不】不同**：原版按 **`deckId`** 认卡组
        /// （`DeckInventory._003CUpdateOrder_003Eb__0` 比的就是 `d.deckId == deck.deckId`），
        /// 而我们**没有 `deckId`**（`PlayerDeck` 只有 `Name`；见 `Shell/CollectionData.cs` 文件头那条老账）
        /// ⇒ 这里**只能按 `Name` 认**。</para>
        ///
        /// <para>**语义**（逐条）：① 按 <paramref name="namesInOrder"/> 的顺序摆；同名取**第一套还没被用掉**的
        /// （与 `CollectionData.IndexOf` 同口径）；② 名字查不到的**保留原位**（= 追加到末尾、彼此保持原相对顺序）
        /// —— ⛔ **绝不丢卡组**，且**出声**（本仓红线：不许静默）；③ `_current`（当前选中）跟着**那一套**走
        /// （不是跟着下标走）；④ 末尾 `return Save();`（照 <see cref="Delete"/> 的 A398 口径，
        /// ⛔ 不再多调一次 `Save`）。</para>
        ///
        /// <para>⚠️ **同名卡组**：`UpdateOrder` 会按顺序各取一套，所以「A、A」这种会有歧义 —— 这是按名认的
        /// 固有代价；原版有 `deckId` 所以没有这个问题。出声时会把这一类也报出来。</para>
        ///
        /// <para>🔴 **2026-10-31（B104_P4）就地订正（铁律 5）**：上面「与原版的一处【不得不】不同」那段 **已不成立** ——
        /// `PlayerDeck` 现在有 `DeckId`（`partial`，本文件尾），并且本类另有一个
        /// <see cref="UpdateOrder(IList{PlayerDeck})"/> **按对象身份**重排的重载（原版 `UpdateOrder(List&lt;CardDeck&gt;)`
        /// 的语义对位）。**生产代码（收藏窗拖拽）已改走那个重载**；本方法只为旧调用点（`Editor/*` 自检夹具）保留。</para>
        /// </summary>
        /// <returns>`false` = 参数非法 / 重排后落盘失败（`LastError` 说原因）。</returns>
        public bool UpdateOrder(IList<string> namesInOrder)
        {
            if (namesInOrder == null || namesInOrder.Count == 0 || _decks.Count == 0) return false;

            var selected = (_current >= 0 && _current < _decks.Count) ? _decks[_current] : null;

            var used = new bool[_decks.Count];
            var ordered = new List<PlayerDeck>(_decks.Count);
            var missing = new List<string>();
            foreach (var nm in namesInOrder)
            {
                int hit = -1;
                for (int i = 0; i < _decks.Count; i++)
                    if (!used[i] && string.Equals(_decks[i].Name, nm, StringComparison.Ordinal)) { hit = i; break; }
                if (hit < 0) { missing.Add(nm ?? "<null>"); continue; }
                used[hit] = true;
                ordered.Add(_decks[hit]);
            }
            // 名字没给到的（或查不到的）**保留原位**：按原相对顺序接在末尾 —— ⛔ 不丢。
            int kept = 0;
            for (int i = 0; i < _decks.Count; i++)
                if (!used[i]) { ordered.Add(_decks[i]); kept++; }

            if (missing.Count > 0 || kept > 0)
                UnityEngine.Debug.LogWarning("[DeckLibrary] `UpdateOrder`：按名重排时有 "
                    + (missing.Count + kept) + " 套卡组**没有出现在新顺序里**（名字对不上的 "
                    + missing.Count + " 条" + (missing.Count > 0 ? "：" + string.Join(" / ", missing) : "")
                    + "）⇒ 它们**按原相对顺序接在末尾**（⛔ 没有丢）。");

            _decks.Clear();
            _decks.AddRange(ordered);

            _current = selected != null ? _decks.IndexOf(selected) : -1;
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
            dst.CardbackId = deck.CardbackId; dst.DeckArmy = deck.DeckArmy;   // 🆕 2026-11-01（`B111_R1WB`）：原版 `CardDeck.CopyDeck` 逐格拷 ⇒ 阵营也要拷，否则 Done 一次就把 `deckArmy` 静默丢了
            // 🆕 2026-09-26：模式**只在新建立时定**（照原版），所以这里**不跟着编辑器的副本走** ——
            //   编辑器改的是内容，改不了模式（原版也没有那条路）。留着这一行是为了「导入/复制」那条路
            //   能把模式带进库；`CommitCurrent` 的调用方（`DeckRuntime` 保存）传进来的副本本来就带着同一个值。
            dst.GameMode = deck.GameMode;
            // 🔴 **2026-10-12（A363）**：这里原来是「`Save();` 然后 `return true;`」——
            //    **返回值把落盘结果吞了**（写失败也报成功）。A363 起调用方（`DeckRuntime.CommitDeck`）
            //    **要靠这个返回值**决定「脏标记清不清」和要不要出声「保存失败」，所以照实回传。
            return Save();
        }

        /// <summary>落盘。失败不吞 —— 原因（**诊断串**，见类头那条更正）写进 <see cref="LastError"/>，
        /// 调用方要出声就出声；⛔ 别把它当界面文案（显示层走 `Loc`）。🆕 2026-10-30（`B99_W7` 就地订正 `B99_W6`）：真落盘走**注入钩子** `SaveLegacyText`（Shell 侧注册成 `SaveCenter.SaveThroughLegacy` = 中央档 + 旧档双写；**落盘关着**时只写旧档）；🔴 **钩子为 `null` 时走 `DeckStore.SaveAll`** —— 那是 W6 之前那条纯文件路（逐字同一调用），⛔ 别删（自检 / 编辑器 / 无 Shell 的场景全靠它）。</summary>
        public bool Save()
        {
            string err;   EnsureCurrentDeckIdInMemory();   // ✅ 2026-11-04 · B141 · UI-05 D6：当前那一套必须有 id（`CurrentDeck` 要按 id 指它；这次 Save 会把 id 一起写进 PlayerDecks）
            bool ok = WriteDecksThroughHooks(out err);
            LastError = ok ? null : err;
            return ok;
        }

        /// <summary>落盘；**失败就出声** —— 给「返回值是对象、装不下落盘成败」的那三个方法用。
        ///
        /// <para>🔴 **2026-10-12（A398）**：<see cref="Create"/> / <see cref="Duplicate"/> / <see cref="Add"/>
        /// 返回的是它们造出来的对象（`PlayerDeck`，不是 `bool`）⇒ **`return Save();` 编不过**，
        /// 那三处原来写的是「`Save();` + `return &lt;对象&gt;;`」= 把落盘结果吞了。本笔补两条出口：
        /// ① `Save()` 内部照旧把原因写进 <see cref="LastError"/>（⚠️ **2026-10-18 `G8` 起是诊断串** ——
        /// 「卡组编辑窗的页头 `_storeErr` 会显示它」那半句**已过期**：那件 2026-10-17 按 D10 删件删掉了）；
        /// ② 这里再打一条**警告** —— 调用点哪怕完全不看 `LastError`，控制台也不会一声不响
        /// （红线「不许静默失败」）。</para>
        ///
        /// <para>⛔ **别为了回传 `bool` 去改那三个方法的签名** —— `Shell/CollectionData.cs` 那几处调用点
        /// 会跟着断。真要一个 bool，用 <see cref="CommitCurrent"/>（编辑窗那条路就是它）。
        /// 🔴 **2026-10-13（A647）就地更正（铁律 5）**：本句原来还缀着一个括号
        /// 「（它们现在是「调完再自己 `Lib.Save()` 一次」的写法）」—— **那半句已经过期**：
        /// A503/A611 之后 `Shell/CollectionData.cs` 那三处**都不再自己 `Lib.Save()`** 了
        /// （`Shell/CollectionData.cs` 里那三处各挂着一句「⛔ 别在后面再加一次 `Lib.Save()`」—— grep 那句话即可）。</para>
        ///
        /// <para>🔴 **2026-10-13（A647）就地更正（铁律 5）** —— 这一段原文是
        /// 「⚠️ **如实标注（本笔没做的那一半）**：`Shell/CollectionData.cs` 的 `CreateDeck` / `DuplicateDeck` /
        /// `ImportDeck` **既不读返回值、也不读 `LastError`** ⇒ 写盘失败时玩家看到的是「操作成功」，而盘上没变。
        /// **那是另一笔账**（越了本件的白名单），本笔只在数据层把话说出来。」
        /// <br/>**实际情况（A503 / A611 起，那三处全读了）**：`CreateDeck`（`CollectionData.CreateDeck`，返回卡组名、
        /// 落盘失败回空串）· `DuplicateDeck`（`CollectionData.DuplicateDeck`，同上，另有出口 `LastDuplicateError`）·
        /// `ImportDeck`（`CollectionData.ImportDeck`，返回名 + `out string why`）—— 三处都读 `Lib.LastError` 并 `Debug.LogWarning`；
        /// 调用点也读返回值（例：`Shell/CollectionWindow.cs` 里那句 `string name = CollectionData.CreateDeck();`）。
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
        //     原版自己的预组卡资产里逐字写着 `"AM1"`（督军）/ `cardLibraryIds:["AM14",…]` / `"GOF22"`，
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
            if (!string.IsNullOrEmpty(d.DefensiveId)) sb.Append(':').Append(RawEntry(d.DefensiveId));
            if (!string.IsNullOrEmpty(d.WarlordId)) sb.Append(':').Append(RawEntry(d.WarlordId));
            foreach (var id in d.CardIds ?? new List<string>())
                if (!string.IsNullOrEmpty(id)) sb.Append(':').Append(RawEntry(id));
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
            // ✅ 已复刻（2026-11-04 · B141 · 判据 `B141_W129_对账_卡组色彩.md` 3-24 = `CardDeck__DeserializeDeckString.c:54-59`）：**缺 `;` 不再早退** —— 原版只在 `Split(';').Length > 1` 时才 `Int32.Parse` 尾巴，否则 `gameMode` 保持 0（Classic = 原版 `PlayModes.Classic`）并**继续解析**（`.c:61-69` 只看 `[0]`）⇒ 一条没有 `;` 的卡组串在原版是**合法**的。我们原来在这里 `return null` = 把这类串整条判死（`Classic` 分享串尾巴被剪掉的情形就这么丢的）。⛔ 别改回去。

            var parts = head.Split(':');
            string name = parts.Length > 0 ? parts[0].Replace(ColonEscape, ":") : "";

            // 裸 id 列表（**先不迁移**：代际要看整串的双解才能定，见下）
            var rawIds = new List<string>();
            for (int i = 1; i < parts.Length; i++)
                if (!string.IsNullOrEmpty(parts[i])) rawIds.Add(parts[i]);

            string deckName = name ?? "";   // 🔴 2026-11-03（B123-C2 · `B121-COLL-14`）去掉「导入的卡组」兜底：原版 `CardDeck__DeserializeDeckString.c` 把 `Split(':')[0]` 原样交给 `DeckBasicSetup`，而 `CardDeck__DeckBasicSetup.c:21-24` 直接 `*(param_1+0x10)=param_2`（名字）⇒ **空名守卫不存在**（补 GUID 的是 `+0x18` 那个 **id**，不是名字）
            if (lookup == null)
                { var noLookup = new PlayerDeck(deckName, null, null, rawIds, gameMode); noLookup.Name = deckName ?? ""; noLookup.SetDeckArmyNeutral(); return noLookup; }   // 🆕 2026-11-01（`B111_R1WB`）：没有 hero ⇒ 原版 `DeckBasicSetup` 那一档 = Neutral｜🆕 2026-11-03（`B123-C2` 首跑）：`PlayerDeck` ctor 对空名会补「新卡组」⇒ 导入路按原版**原样写回**（`CardDeck__DeckBasicSetup.c:21-24` 无空名守卫）

            // ① **现代解**：按卡的类型摘（原版 `DeserializeDeckString.c:147,155` —— 按类型不按位置）
            List<string> modernDropped;
            var modern = SplitByType(deckName, rawIds, lookup, gameMode, out modernDropped);
            if (DeckRules.Validate(modern, lookup, modern.IsSkirmish) == DeckError.None)
            {
                LastDroppedIds = modernDropped;
                return modern;
            }

            // ② **旧代解**：整串按代际表迁一次（**一次遍历、只看原始值**），再按类型摘。
            //    🔴 判据 = 双解（现代不合法 ∧ 旧代合法才采）；两解都非法 ⇒ 按现代解返回并出声。
            //    ⚠️ 表**不是单射**（见 `LegacyCardIds.cs` 文件头）⇒ 只许这样整串迁一次，
            //       ⛔ 别在下面的循环里逐条 `Migrate`（那会边迁边查、链式 id 会静默改成第三张卡）。
            var legacyIds = new List<string>(rawIds);
            int changed = LegacyCardIds.MigrateIds(legacyIds);
            if (changed > 0)
            {
                List<string> legacyDropped;
                var legacy = SplitByType(deckName, legacyIds, lookup, gameMode, out legacyDropped);
                if (DeckRules.Validate(legacy, lookup, legacy.IsSkirmish) == DeckError.None)
                {
                    UnityEngine.Debug.Log("[LegacyCardIds] 导入的卡组串「" + name + "」：现代解不合法、旧代解合法"
                                        + " ⇒ 按**双解**判定迁移 " + changed + " 处（⛔ 只改内存）");
                    LastDroppedIds = legacyDropped;
                    return legacy;
                }
                UnityEngine.Debug.LogWarning("[LegacyCardIds] 导入的卡组串「" + name + "」：现代解与旧代解**都不合法**"
                                           + " ⇒ **按现代解返回、不迁移**（⛔ 不猜）");
            }
            LastDroppedIds = modernDropped;
            return modern;
        }

        /// <summary>把一串裸 id 按**卡的类型**摘成 `PlayerDeck` —— 照原版 `DeserializeDeckString.c:147,155`
        /// （**按类型摘，不按位置**；同名多张时第一张当专属卡、其余当普通卡，不丢）。
        /// 卡池里查不到的按原版**丢掉**，但记在 <paramref name="dropped"/> 里（→ `LastDroppedIds`）。</summary>
        static PlayerDeck SplitByType(string name, List<string> ids, Func<string, CardDef> lookup,
                                      int gameMode, out List<string> dropped)
        {
            dropped = new List<string>();
            string hero = null, def = null;
            var rest = new List<string>();
            foreach (var rawId in ids)
            {
                if (string.IsNullOrEmpty(rawId)) continue;
                // 🔴 账 `HUD:92` / `HUD:93`（卡组层那一半）：异画副本（`#HB_SW1`，或**原版分享串**里的
                //   克隆 id `HB_SW1`）先归一成我们卡组里的写法、再按**原卡**分类
                //   （判据 / 口径 → `PlayerDeck` 类尾那段注）。认不出才算 dropped。
                string id = PlayerDeck.NormalizeEntryId(rawId, lookup);
                var c = id == null ? null : PlayerDeck.ResolveEntry(id, lookup, out _);
                if (c == null) { dropped.Add(rawId); continue; }
                if (c.Type == "defence") { if (def == null) def = id; }   // ✅ 已复刻（2026-11-04 · B141 · 判据 `B141_W129_对账_卡组色彩.md` 3-24 = `CardDeck__DeserializeDeckString.c:152-159`）：**多余的防御卡丢弃** —— 原版从尾到头扫，每遇到一张就 `RemoveAt`，只把**最后遍历到**（倒序 ⇒ 串里**第一张**）那格交给 `DeckBasicSetup` 的防御位；剩下的一张都不进卡表（我们原来 `rest.Add(id)` = 把多余防御卡当普通卡带进卡组，原版没有这个行为）
                else if (c.Type == "hero") { if (hero == null) hero = id; }   // ✅ 同上 · 督军那一条（`.c:141-151`）：多余督军**丢弃**，只留串里第一张
                else rest.Add(id);
            }
            var d = new PlayerDeck(name, hero, def, rest, gameMode); d.SetDeckArmyFromHeroId(hero, lookup); return d;   // 🆕 2026-11-01（`B111_R1WB`）：原版 `DeserializeDeckString` → `DeckBasicSetup` ⇒ `deckArmy` **只由督军定、⛔ 不看卡表**
        }

        /// <summary>加一套外部来的卡组（导入用）。名字重了自动加序号。</summary>
        public PlayerDeck Add(PlayerDeck d)
        {
            if (d == null) return null;
            d.Name = string.IsNullOrEmpty(d.Name) ? "" : UniqueName(d.Name);   // 🔴 2026-11-03（B123-C2 · `B121-COLL-14`）：空名**保持空名**（原版不补文案、也不给空名编序号 —— `CardDeck__DeckBasicSetup.c:21-24` 直接赋值）；非空名照旧去重
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

        /// <summary>我们卡组里的条目 → **原版分享串里那一份的写法**：异画副本 `#HB_SW1` ⇒ 克隆 id `HB_SW1`
        /// （`AlternateArtCard.GetIdForClonedCard()` = `uniqueId.Replace("#", "")`），其余原样返回。
        /// <para>为什么：原版 `CardDeck.DeserializeDeckString` 拿串里那一条去
        /// `AssetLocator.GetAsset&lt;RawCardScript&gt;` 找的是**克隆卡资产**（`HB_…`），不是背包物品 id
        /// （`#HB_…`）⇒ 把 `#HB_…` 写进分享串，原版读不回来。</para>
        /// <para>⚠️ **没查清**：clone 资产（`HB_&lt;id&gt;`）不在我们 allcards 快照的 1,351 条里
        /// （那里 `altArtFromId` 全空）⇒「分享串里是 `HB_SW1`」是**两条反汇编推出来的**
        /// （`GetIdForClonedCard` + `DeserializeDeckString`），不是直接读到一条真的卡组串。
        /// 详见 `PlayerDeck` 类尾那段注的「没查清」。</para></summary>
        static string RawEntry(string id)
        {
            return PlayerDeck.IsAltArtEntry(id) ? id.Substring(1) : id;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  「中央档」注入钩子 —— 解掉 `B99_W6` 引入的反向依赖（2026-10-30 · `B99_W7`）
        //
        //  为什么：W6 把 `CardPresentation.SaveCenter.*` / `SaveKeys.*` 直接写进了本类 ⇒
        //  `RuleEngine/` 反向依赖 `CardPresentation/`（成文约束 = `Core/AchievementTally.cs:281`
        //  「⛔ 别在 `RuleEngine/` 里插 `CardPresentation.*`」）。
        //  这里只留**三个注入点** —— 本类**不认识** `SaveCenter`、`SaveKeys`、`StoreOn`：
        //    · `ReadLegacyText(旧档完整路径)` ⇒ 原文（`null` = 两边都没有）
        //    · `SaveLegacyText(旧档完整路径, payloadJson, out error)` ⇒ 是否真的写成
        //    · `CentralSaveExists()` ⇒ 中央档在不在（**注册方负责先判 `StoreOn`**，见下）
        //
        //  🔴 三个都是 `null` ⇒ 走 `DeckStore.LoadAll` / `DeckStore.SaveAll` 这两条**纯文件**老路
        //     （= W6 之前那一版，**逐字同一调用**）⇒ 批处理自检 / 编辑器 / 没有 Shell 的场景零变化。
        //  🔴 `CentralSaveExists` **必须由注册方按 `StoreOn` 收口**
        //     （注册成 `() => SaveCenter.StoreOn && SaveCenter.Exists(key)`）：
        //     本类**不引入「落盘门」这个概念**，否则「文件在不在」会和 `ReadThroughLegacy` 的
        //     `StoreOn` 支自相矛盾 —— 那正是 W6 留下的假阳性：批处理里 `DeckStore.OverridePath`
        //     指临时文件，而 `SaveCenter.Exists` 看的是 `persistentDataPath` 下的中央档
        //     ⇒ 玩家真有中央档时 `existed` 恒 true ⇒ `ClassifyLoad("no save file", true)` = `Failed`
        //     （应为 `NoSaveFile`）。本类这一侧另有一道闸：`ReadLegacyText == null` 时**根本不问**
        //     `CentralSaveExists`（两个钩子必须同进同退）。
        //  ⚠️ 成员**追加在类尾**（铁律 18·b：本文件有 7 处账落点，别插进类体中间）。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>读旧档 / 中央档原文的注入点（`null` ⇒ 走 <see cref="DeckStore.LoadAll"/>）。
        /// <para>🔴 **两处行为必须逐字等价于 W6 的 <c>SaveCenter.ReadThroughLegacy</c>**：
        /// 先中央档、再旧档；都没有 ⇒ `null`（`DeckStore.Parse(null)` = `NoSaveFile`）。</para></summary>
        public static Func<string, string> ReadLegacyText;

        /// <summary>写「中央档 + 旧档」的注入点（`null` ⇒ 走 <see cref="DeckStore.SaveAll"/>）。
        /// <para>🔴 **必须等价于 W6 的 <c>SaveCenter.SaveThroughLegacy</c>**：落盘开着 ⇒ 中央档 + 旧档双写；
        /// **落盘关着 ⇒ 只写旧档**（那条支由 `SaveCenter` 自己判，本类不重复实现）。</para></summary>
        public static DeckLegacySaver SaveLegacyText;

        /// <summary>「中央档在不在」的注入点（`null` ⇒ 只看旧档）。
        /// 🔴 **注册方负责先判 `StoreOn`** —— 本类不知道「落盘门」是什么（理由见上面那一段）。</summary>
        public static Func<bool> CentralSaveExists;

        /// <summary>写盘注入点的签名 —— `out error` 装**诊断串**；返回 `true` 才算真的写成。</summary>
        public delegate bool DeckLegacySaver(string legacyFullPath, string payloadJson, out string error);

        /// <summary><see cref="Load"/> 的读档一跳：**有钩子**走「中央档 → 旧档」，
        /// **没有钩子**走 W6 之前那条纯文件路（<see cref="DeckStore.LoadAll"/>，逐字同一调用）。</summary>
        static List<PlayerDeck> ReadDecksThroughHooks(out DeckStore.LoadNote note, out string detail, out int current)
        {
            List<PlayerDeck> primary;
            if (ReadLegacyText != null) primary = DeckStore.Parse(ReadLegacyText(DeckStore.Path), out note, out detail, out current);
            else primary = DeckStore.LoadAll(out note, out detail, out current);
            return MergeFallbackDecks(primary, ref note, ref detail);   // ✅ 2026-11-04 · B141 · UI-05 D6：原版 `PlayerDecks` 缺 ⇒ 读 `Decks`；`PlayerDecksFast` 永远 Concat（`DeckInventory.cs:209/216`）
        }

        /// <summary><see cref="Save"/> 的落盘一跳：**有钩子**走「中央档 + 旧档」，
        /// **没有钩子**走 W6 之前那条纯文件路（<see cref="DeckStore.SaveAll"/>，逐字同一调用）。
        /// ⚠️ `DeckStore.Serialize` **只在有钩子时才调** —— 没钩子时多序列化一遍纯属浪费（而且那份串没人要）。</summary>
        bool WriteDecksThroughHooks(out string error)
        {
            if (SaveLegacyText != null)
                return SaveLegacyText(DeckStore.Path, DeckStore.SerializeStore(_decks), out error) && WriteCurrentDeckKey(out error);   // ✅ 2026-11-04 · B141 · UI-05 D6：中央档 payload **不含 current**；当前选中写独立 key `CurrentDeck`（卡组 id）
            return DeckStore.SaveAll(_decks, _current, out error);
        }
        // ================================================================================================
        //  🆕 2026-10-31（B104_P4）· 卡组**身份**（原版 `CardDeck.deckId`）—— 追加在类尾（零行号漂移）
        //
        //  为什么要有：原版全链按 **`deckId`** 认卡组，不看名字 ——
        //    · 重排：`DeckInventory._003CUpdateOrder_003Eb__0` = `d.deckId == this.deck.Item.deckId`
        //      （`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/DeckInventory.cs:578-581` 现读；
        //       `.c` 侧同址 = `DeckInventory.__c__DisplayClass4_0___UpdateOrder_b__0.c` 的 `System_String__Equals`
        //       比 `+0x18` = `CardDeck.deckId`）。
        //    · 活动卡组槽：`PlayEventSaveGame.defaultDeckId`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayEventSaveGame.cs`
        //      现读，字段与 `deckName` 分开）⇒ `IPlayEvent.SetDeck/GetEventDeck` 存的是 **id**。
        //  ⛔ 别拿名字当 id：同名卡组（重命名 / 导入）会选错。本仓里 `PlayerDeck` 没有 id 字段，
        //  本次在同一个命名空间里用 `partial` 补上（父机类文件 `Core/DeckRules.cs` 未动一行）。
        //  ⚠️ 旧存档没这个键 ⇒ `null`；第一次需要时才补发（`EnsureDeckId`）。
        // ================================================================================================

        /// <summary>新发一个卡组 id（与原版 `CardDeck.deckId` 同用途：**跨进程稳定的身份串**）。
        /// ❗ `Guid.NewGuid().ToString("N")` = 32 位小写十六进制，**不含冒号 / `;`** —— 卡组分享串里那两个字符是分隔符。</summary>
        public static string NewDeckId() { return Guid.NewGuid().ToString("N"); }

        /// <summary>某套卡组的 <see cref="PlayerDeck.DeckId"/>（下标越界 / 还没发过 id ⇒ `null`）。</summary>
        public string DeckIdAt(int index)
        {
            if (index < 0 || index >= _decks.Count) return null;
            var id = _decks[index].DeckId;
            return string.IsNullOrEmpty(id) ? null : id;
        }

        /// <summary>按 id 反查下标（查不到 / 空 id ⇒ `-1`）。与**按名反查**
        /// <see cref="RuleEngine.DeckLibrary"/> 的同族口不同：这里对的是**唯一身份**（同名不歧义）。</summary>
        public int IndexOfDeckId(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return -1;
            for (int i = 0; i < _decks.Count; i++)
                if (string.Equals(_decks[i].DeckId, deckId, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>取某套卡组的 id；还没有就**现发一个并落盘**（返回那个 id）。
        /// 下标越界 ⇒ `null`。落盘失败也返回 id（内存里已经稳定），警告由 `SaveOrWarn` 打。</summary>
        public string EnsureDeckId(int index)
        {
            if (index < 0 || index >= _decks.Count) return null;
            var d = _decks[index];
            if (!string.IsNullOrEmpty(d.DeckId)) return d.DeckId;
            d.DeckId = NewDeckId();
            SaveOrWarn("EnsureDeckId");          // A398 口径：返回值是字符串、装不下落盘成败 ⇒ 走响声口
            return d.DeckId;
        }

        /// <summary>**按对象身份**重排卡组库 —— 原版 `DeckInventory.UpdateOrder(List&lt;CardDeck&gt; newOrder)`
        /// 的语义对位（原版在里面按 `deckId` 对；我们这边的 `newOrder` 就是**库里那些对象本体**，
        /// 对象引用就是身份 ⇒ 不存在「同名拿错」这一档）。
        ///
        /// <para>行为与按名那个重载**逐条对齐**：① 表里没有的卡组**保留原位接在末尾**
        /// （绝不丢，且**出声**）；② 重复出现的对象只算一次；③ `_current` 跟着**那一套**走
        /// （不跟下标）；④ 尾句 `return Save()`。</para>
        /// <para>⛔ 不要把它写成「先转名字再调上一个重载」—— 那就是原病（同名时按名对不上身份）。</para></summary>
        /// <returns>`false` = 参数非法 / 重排后落盘失败（<see cref="LastError"/> 说原因）。</returns>
        public bool UpdateOrder(IList<PlayerDeck> newOrder)
        {
            if (newOrder == null || newOrder.Count == 0 || _decks.Count == 0) return false;

            var selected = (_current >= 0 && _current < _decks.Count) ? _decks[_current] : null;

            var ordered = new List<PlayerDeck>(_decks.Count);
            int missing = 0;
            foreach (var d in newOrder)
            {
                if (d == null) continue;
                bool already = false;
                for (int k = 0; k < ordered.Count; k++) if (ReferenceEquals(ordered[k], d)) { already = true; break; }
                if (already) continue;                       // 同一对象重复出现 ⇒ 只算一次
                bool inLib = false;
                for (int k = 0; k < _decks.Count; k++) if (ReferenceEquals(_decks[k], d)) { inLib = true; break; }
                if (!inLib) { missing++; continue; }         // 不属于本库 ⇒ 不插进来（与按名那个的 `missing` 同形）
                ordered.Add(d);
            }
            // 没出现在新顺序里的 ⇒ 保留原相对顺序接在末尾（⛔ 不丢）。
            int kept = 0;
            for (int i = 0; i < _decks.Count; i++)
            {
                bool listed = false;
                for (int k = 0; k < ordered.Count; k++) if (ReferenceEquals(ordered[k], _decks[i])) { listed = true; break; }
                if (!listed) { ordered.Add(_decks[i]); kept++; }
            }

            if (missing > 0 || kept > 0)
                UnityEngine.Debug.LogWarning("[DeckLibrary] `UpdateOrder`（按对象身份）："
                    + (missing + kept) + " 套卡组**没有出现在新顺序里**（不属于本库 "
                    + missing + " 条）⇒ 它们**按原相对顺序接在末尾**（⛔ 没有丢）。");

            _decks.Clear();
            _decks.AddRange(ordered);

            _current = selected != null ? _decks.IndexOf(selected) : -1;
            return Save();
        }

        /// <summary>⚠️ **原版没有这个口**（父机类不对外暴露「只改内存选中项」）：
        /// **只改内存里的 `_current`、不落盘** —— 给「选中失败后把高亮放回去」那一档用
        /// （底盘本来就没改，所以不能再调 `Select`——那会又试一次写盘）。</summary>
        public void RestoreSelection(int index)
        {
            if (index >= 0 && index < _decks.Count) _current = index;
            else _current = -1;
        }

        // ================================================================================================
        //  🆕 2026-11-04（执行代理 `B141_W15` · UI-05 D6）· 「当前卡组」拆成独立 key（原版 `CurrentDeck`）
        //
        //  判据（现读）：
        //   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:11472-11473` ——
        //     `GetPlayerOnlineDataString(..., PlayerDataKey.CurrentDeck)` → `currentDeckId`（**字符串 = 卡组 id**）。
        //   · `d:/2/tools/decomp_full/PlayerDataManager__set_currentDeckId.c:5` —— 写字段偏移 `+0x238`。
        //   · `DeckInventory.cs:209` —— `PlayerDecks` = `AzureDeckInfo[]`（**没有 current**）。
        //  ⇒ 本类只加**注入点**（和既有的 `ReadLegacyText` 同一套纪律：本类不认识 `SaveCenter` / `SaveKeys`）。
        //  ⚠️ 成员追加在类尾（本文件下游有账的 `文件:行` 落点，别插进类体中间）。
        // ================================================================================================

        /// <summary>`CurrentDeck` key 的**读**注入点（返回原文；`null` = 没有）。
        /// 注册方（Shell）接 `SaveCenter.ReadThroughLegacy(SaveKeys.CurrentDeck, null)`。
        /// <para>`null` ⇒ 不读这个 key（退回旧形状：`PlayerDecks` payload 里的 `current:int`）。</para>
        /// <para>✅ 已复刻（2026-11-04 · B141 · `PlayerDataManager.cs:11472-11473`）。</para></summary>
        public static Func<string> ReadCurrentDeckText;

        /// <summary>`CurrentDeck` key 的**写**注入点（参数 = **卡组 id**；返回是否写成功）。
        /// 注册方接 `id => SaveCenter.SetRaw(SaveKeys.CurrentDeck, DeckStore.SerializeCurrentDeck(id))`。</summary>
        public static Func<string, bool> SaveCurrentDeckText;

        /// <summary>`Decks` key 的读注入点（原版 `Dictionary&lt;string, AzureDeckInfo&gt;`；**只在 `PlayerDecks` 缺时**用）。
        /// 注册方接 `() => SaveCenter.ReadThroughLegacy(SaveKeys.Decks, null)`。</summary>
        public static Func<string> ReadDecksKeyText;

        /// <summary>`PlayerDecksFast` key 的读注入点（原版 `AzureDeckInfo[]`；**总是**与 `PlayerDecks` 拼起来）。
        /// 注册方接 `() => SaveCenter.ReadThroughLegacy(SaveKeys.PlayerDecksFast, null)`。</summary>
        public static Func<string> ReadPlayerDecksFastText;

        /// <summary>本次落盘前保证「当前那一套」在内存里有 `DeckId`（`PlayerDecks` 要带着它一起写，
        /// `CurrentDeck` 才能按 id 指回来）。
        /// ⚠️ ⛔ 别改成 `EnsureDeckId(_current)` —— 那个会**再调一次 `Save()`**（递归）。</summary>
        void EnsureCurrentDeckIdInMemory()
        {
            if (_current < 0 || _current >= _decks.Count) return;
            var d = _decks[_current];
            if (d != null && string.IsNullOrEmpty(d.DeckId)) d.DeckId = NewDeckId();
        }

        /// <summary>写 `CurrentDeck`（值 = 卡组 id）。没有注入点 ⇒ **跳过并返回 true**（无 Shell 的纯文件老路，旧行为）。</summary>
        bool WriteCurrentDeckKey(out string error)
        {
            error = null;
            if (SaveCurrentDeckText == null) return true;      // 没接键：不写（不编一个没人读的文件）
            string id = DeckIdAt(_current);
            if (string.IsNullOrEmpty(id)) return true;         // 空库 / 越界：原版也没有「当前」
            if (SaveCurrentDeckText(id)) return true;
            error = "CurrentDeck 键写失败（见上一条 `[SaveCenter]` 日志）";
            return false;
        }

        /// <summary>`Load` 之后按 `CurrentDeck`（id）校正 `_current`；
        /// **旧档没有这个 key ⇒ 保留 payload 里的 `current:int`**（向后兼容，⛔ 不归 0）。</summary>
        void ApplyCurrentDeckKey()
        {
            if (ReadCurrentDeckText == null) return;
            string raw = SafeReadKey(ReadCurrentDeckText, "CurrentDeck");
            string id = DeckStore.ParseCurrentDeckId(raw);
            if (string.IsNullOrEmpty(id)) return;               // 旧档 / 没有这个 key ⇒ 用旧 current
            int byId = IndexOfDeckId(id);
            if (byId >= 0) { _current = byId; return; }
            UnityEngine.Debug.LogWarning("[DeckLibrary] `CurrentDeck` 里的 id = `" + id + "` 在库里 " + _decks.Count
                           + " 套里查不到 ⇒ 保留旧档的 `current` = " + _current + "（⛔ 不静默归 0）");
        }

        /// <summary>原版读顺序：有 `PlayerDecks` 就用它 + **并 `PlayerDecksFast`**；
        /// `PlayerDecks` 没有卡组 ⇒ 读 `Decks`（map）+ `PlayerDecksFast`（`DeckInventory.cs:209/216`）。
        /// <para>兜底生效时把 <paramref name="note"/> 改成 `None`（读到东西了，⛔ 不是「第一次跑」）。</para></summary>
        static List<PlayerDeck> MergeFallbackDecks(List<PlayerDeck> primary, ref DeckStore.LoadNote note, ref string detail)
        {
            var fast = ReadPlayerDecksFastText == null ? null
                     : DeckStore.ParseAzureDeckArray(SafeReadKey(ReadPlayerDecksFastText, "PlayerDecksFast"));
            if (primary != null && primary.Count > 0)
            {
                if (fast == null || fast.Count == 0) return primary;     // 原版 Concat：Fast 空 ⇒ 原样
                var merged = new List<PlayerDeck>(primary);
                merged.AddRange(fast);
                UnityEngine.Debug.Log("[DeckLibrary] `PlayerDecks` " + primary.Count + " 套 + `PlayerDecksFast` " + fast.Count
                        + " 套 ⇒ 合并 " + merged.Count + " 套（原版 `DeckInventory__OnFinishUnpack.c:45-58`）");
                return merged;
            }
            var map = ReadDecksKeyText == null ? null
                    : DeckStore.ParseAzureDeckMap(SafeReadKey(ReadDecksKeyText, "Decks"));
            var list = new List<PlayerDeck>();
            if (map != null) list.AddRange(map);
            if (fast != null) list.AddRange(fast);
            if (list.Count > 0)
            {
                note = DeckStore.LoadNote.None;
                detail = null;
                UnityEngine.Debug.Log("[DeckLibrary] `PlayerDecks` 没有卡组 ⇒ 按原版读兜底：`Decks`(" + (map == null ? 0 : map.Count)
                        + ") + `PlayerDecksFast`(" + (fast == null ? 0 : fast.Count) + ") = " + list.Count + " 套"
                        + "（`PlayerDataManager.cs:11459-11470` + `DeckInventory.cs:216`）");
            }
            return list;
        }

        /// <summary>跑注入点读，抛了 ⇒ 出声 + 当作 `null`（⛔ 不让一个坏 key 把整条读档打崩）。</summary>
        static string SafeReadKey(Func<string> read, string what)
        {
            try { return read(); }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[DeckLibrary] 读 `" + what + "` 抛了（" + e.GetType().Name + ": " + e.Message + "）⇒ 当作没有");
                return null;
            }
        }
    }

    /// <summary>⚠️ 这一块是 **2026-10-31（B104_P4）追加在「`DeckLibrary.cs` 文件尾」**的 partial：
    /// 给 <see cref="PlayerDeck"/> 补上原版的 **`CardDeck.deckId`**。
    /// <para>判据 ⇒ 本文件 `DeckLibrary` 类尾那一段注释（重排谓词 + `PlayEventSaveGame.defaultDeckId`）。</para>
    /// <para>⚠️ 为何用 `partial` 而不是去 `Core/DeckRules.cs` 里插一行：那个文件下游有账的 `文件:行` 落点，
    /// 插行会把它们全部打漂（铁律 18·b）；而 `JsonUtility` 反射看的是**类型**、不看声明在哪个文件
    /// ⇒ 写在这里同样会进存档。</para>
    /// <para>⚠️ **拷贝点**：`PlayerDeck.Clone()`（`Core/DeckRules.cs:772`）不拄本字段 ⇒ 复制出来的新卡组**没有 id**
    /// （下一次用到时 `EnsureDeckId` 现发），这正是要的（两套卡组不能共用同一个身份）。
    /// `DeckLibrary.CommitCurrent` 逐字段拷、**不动**库里那一套的 `DeckId` ⇒ 编辑保存后身份不变。</para>
    public partial class PlayerDeck
    {
        /// <summary>原版 `CardDeck.deckId`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardDeck.cs:12` 现读）。
        /// 空/null = 旧存档里还没发过 id。取用一律走 `DeckLibrary.EnsureDeckId`（它会补发 + 落盘）。</summary>
        public string DeckId;
    }
}
