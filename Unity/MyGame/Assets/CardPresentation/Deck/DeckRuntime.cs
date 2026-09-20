// DeckRuntime.cs — 卡组编辑界面的**运行时控制器**
//
// 为什么需要它：`DeckEditorState`（纯 C#，322 行）早就写好、自检也覆盖了行为，
// 但**运行时交互层从来没建过** —— `DeckEditor.unity` 里只有 `ImageQuad`/`Label` 画的一堆
// 静态方块，点/拖/翻页一律不响应（`DeckScene.cs` 全文 `AddComponent` 只命中那台 Camera）。
// 这个类补上「谁听鼠标 + 谁重画」这一层。
//
// 🔴 交互判据**逐条照原版**（出处 `资料/卡组编辑界面_查证_0920.md` §二，那是三路反编译查证的正本）：
//   · 卡池卡：**左键 = 弹放大窗** · **右键 = 加进卡组**（`DeckEditingWindow__OnCardClick.c:16,42`）
//   · 卡组条目：**左键 = 弹放大窗**（**不是删除**！`DeckEditingWindow__Start.c:109` → `OpenCardInformation`）
//   · **删除只有「拖出」一条路**（`DeckEditingPanel__CheckCardSlotDrag.c:59,66`）—— 本文件按这个做
//   · Done = 保存（`DeckEditingWindow__TrySaveDeck.c`）
//
// ⚠️ **我们自加的三处**（原版没有，**别当原版**）：
//   ① 侧栏「新建 / 复制 / 删除卡组」三个钮（原版这套动作在 `DeckInfoPopup` 的 5 圆钮里）
//   ② 卡池**翻页**（原版是 `RecyclableScrollRect` 无限滚动，没有分页）
//   ③ 卡池 4×3 的网格尺寸（原版是运行时算的回收列表，节点树给不出一屏几列）
// 版面常量与 `Editor/DeckScene.cs` 共用同一份（后者转发到这里），**别再写第二份**。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡组编辑界面的运行时控制器。挂在 `DeckEditor.unity` 的根对象上。</summary>
    public class DeckRuntime : MonoBehaviour
    {
        public static DeckRuntime Instance { get; private set; }

        // ============================================================ 版面常量（唯一出处）
        // 原版界面按 1920×1080 设计；这套布局可见高 10 单位 → 108 px/单位
        public const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;   // = 108
        public const float ScreenW = 1920f, ScreenH = 1080f;
        const int Cols = 4, Rows = 3;                    // ⚠️ 我们挑的（见文件头 ③）
        const float CardScale = 0.62f;
        const float GridCx = 975f, GridCy = 520f, GridStepX = 250f, GridStepY = 260f;
        const float ZPanel = 0.30f, ZRow = 0.10f, ZBar = 0.10f;
        /// <summary>侧栏右边界（px）—— 卡组行「拖出去」的判据用它（原版是拖出 `DeckEditingPanel`）。</summary>
        const float SidebarRight = 350f;
        /// <summary>卡表最多显示几行 —— **8 是侧栏可用高度算出来的，不是拍的**：
        /// 第 1 行中心 y=390、步进 58、行高 54 ⇒ 第 8 行底边 = 390+7×58+27 = 823；
        /// 再往下就压到 `verdict`(910) / `counter`(960) / `btn_done`(1000) 了
        /// （实测：建成 12 行时自检立刻报 `deck_row_10 × btn_done` 重叠）。
        /// ⚠️ 原版是可滚动的卡组列表（`DeckListDrawer` 在 Scroll View 里），**我们 v1 只显示前 8 张**
        /// —— **这是自加的简化**，如实标着。</summary>
        const int MaxDeckRows = 8;

        // ============================================================ 状态
        public DeckEditorState State { get; private set; }
        public DeckLibrary Library { get; private set; }
        public Transform Root { get; private set; }

        Camera _cam;
        readonly List<CardView> _poolViews = new List<CardView>();
        readonly List<CardDef> _poolPage = new List<CardDef>();
        readonly List<ImageQuad> _deckRowQuads = new List<ImageQuad>();
        readonly List<Label> _deckRowTexts = new List<Label>();
        readonly List<Label> _deckTabTexts = new List<Label>();
        readonly Dictionary<string, ImageQuad> _quads = new Dictionary<string, ImageQuad>();
        Label _counter, _verdict, _pageLabel, _notice, _storeErr;
        string _noticeText = "";
        float _noticeUntil;

        // 拖拽状态（把卡组条目拖出侧栏 = 删除，照原版）
        ImageQuad _dragQuad;
        Label _dragText;
        int _dragRow = -1;
        bool _dragging;
        Vector3 _dragOrigin;

        // ============================================================ 生命周期

        void Awake() { Instance = this; }

        void Start()
        {
            // 按 Play 的入口 —— 和自检**必须是同一条路**（`CardPresentation` 的规矩：
            // 开局写在 Start 里 = 批处理下永远验不到，所以抽成公开的 Build 让两边都调它）
            if (State == null) Build(DeckLibrary.Load());
        }

        void Update()
        {
            HandlePointer();
            if (_notice != null && _noticeText.Length > 0 && Time.unscaledTime > _noticeUntil)
            {
                _noticeText = "";
                _notice.SetText("");
            }
        }

        // ============================================================ 建

        /// <summary>按像素坐标建整个界面。**自检与运行时调同一个**（见 <see cref="Start"/>）。</summary>
        public void Build(DeckLibrary lib)
        {
            Library = lib ?? DeckLibrary.Load();
            _cam = Camera.main;
            if (_cam != null) LayoutSpace.Apply(_cam);

            Root = transform;
            foreach (Transform c in Root) DestroySafe(c.gameObject);

            // 空库时先替玩家建一套（演示卡组），这样界面一打开就有内容
            if (Library.Count == 0)
            {
                var fresh = NewState();
                Library.Create("我的卡组");
                Library.CommitCurrent(PlayerDeckForDemo(fresh));
            }
            State = NewState();
            State.LoadDeck(Library.Current);

            BuildSidebar();
            BuildPool();
            BuildFilterBar();
            BuildBottomBar();
            RefreshAll();
        }

        DeckEditorState NewState()
        {
            return new DeckEditorState(CardDatabase.Load());
        }

        void BuildSidebar()
        {
            Quad(CardArt.DeckUi("UI_Deck_Selection_Back"), 175f, 540f, 439f, 664f, "sidebar_panel", ZPanel);
            Text("卡组编辑", 175f, 120f, 3, Color.white, "title");

            _deckTabTexts.Clear();
            for (int i = 0; i < MaxDeckRows; i++)
                _deckTabTexts.Add(Text("", 175f, 170f + i * 30f, 2, Color.gray, "deck_tab_" + i));

            // ⚠️ 我们自加的三个钮（见文件头 ①）
            Quad(CardArt.DeckUi("40k_general_bt_yellow_confirm"), 60f, 320f, 71f, 71f, "btn_new", ZRow);
            Quad(CardArt.DeckUi("40k_general_bt_yellow_duplicate"), 140f, 320f, 71f, 71f, "btn_dup", ZRow);
            Quad(CardArt.DeckUi("40k_general_bt_yellow_delete"), 220f, 320f, 71f, 71f, "btn_del", ZRow);
            _storeErr = Text("", 175f, 370f, 1, new Color(0.95f, 0.5f, 0.4f), "store_err");

            var rowTex = CardArt.DeckUi("40k_deck_cardlist_bg");
            _deckRowQuads.Clear(); _deckRowTexts.Clear();
            for (int i = 0; i < MaxDeckRows; i++)
            {
                _deckRowQuads.Add(Quad(rowTex, 175f, 390f + i * 58f, 318f, 54f, "deck_row_" + i, ZRow));
                _deckRowTexts.Add(Text("", 175f, 390f + i * 58f, 1, Color.white, "deck_row_text_" + i));
            }

            _counter = Text("", 175f, 960f, 2, Color.white, "counter");
            _verdict = Text("", 175f, 910f, 1, Color.white, "verdict");
            Quad(CardArt.DeckUi("40k_general_bt_yellow_confirm"), 130f, 1000f, 71f, 71f, "btn_done", ZBar);
            Quad(CardArt.DeckUi("40k_general_bt_yellow_close"), 230f, 1000f, 71f, 71f, "btn_close", ZBar);
        }

        void BuildPool()
        {
            _poolViews.Clear();
            for (int i = 0; i < Cols * Rows; i++)
            {
                int col = i % Cols, row = i / Cols;
                float cx = GridCx + (col - (Cols - 1) * 0.5f) * GridStepX;
                float cy = GridCy + (row - (Rows - 1) * 0.5f) * GridStepY;
                // ⚠️ `CardView.Create` 的 `CardData` 是 struct、不接受 null —— 先给 default，
                //    真正的数据在 `RefreshPool()` 里用 `SetData` 灌（这样 12 个视图可以复用、不重建）
                var v = CardView.Create(Root, default(CardData), "pool_" + i);
                v.SetPose(Pos(cx, cy), 0f, CardScale);
                v.gameObject.SetActive(false);
                _poolViews.Add(v);
            }
        }

        void BuildFilterBar()
        {
            // ⚠️ 版面：权威坐标说筛选栏在**左**（`_deck_editing_godot_rects.txt` R:174-176），
            //    我们原来放在右 —— **按原版改到左**是待办，本版先保持右侧（挪动要连自检一起改）
            float fx = 1750f, fy = 200f;
            Text("筛选", fx, fy, 2, Color.white, "filter_title");
            string[] pills = { "全部", "军  Ultramarines", "稀有度  传说", "费用  3", "类型  单位" };
            for (int i = 0; i < pills.Length; i++)
            {
                Quad(CardArt.DeckUi("40K_dropdown_field_closed"), fx, fy + 70f + i * 90f, 300f, 42f, "filter_field_" + i);
                Text(pills[i], fx, fy + 70f + i * 90f, 1, Color.white, "filter_text_" + i);
            }
            Quad(CardArt.DeckUi("40k_bt_icon_search"), fx, fy + 70f + pills.Length * 90f, 27f, 27f, "filter_search");
        }

        void BuildBottomBar()
        {
            // ⚠️ 翻页是我们自加的（见文件头 ②）—— 原版没有分页
            Quad(CardArt.DeckUi("40k_general_bt_yellow_back"), 700f, 1010f, 71f, 71f, "btn_prev", ZBar);
            Quad(CardArt.DeckUi("40k_general_bt_yellow_confirm"), 800f, 1010f, 71f, 71f, "btn_next", ZBar);
            _pageLabel = Text("", 1000f, 1010f, 2, Color.white, "page_label");
            _notice = Text("", 1000f, 940f, 1, new Color(0.95f, 0.85f, 0.5f), "notice");
        }

        // ============================================================ 刷新

        void RefreshAll()
        {
            RefreshPool();
            RefreshDeckList();
            RefreshHeader();
        }

        void RefreshPool()
        {
            _poolPage.Clear();
            _poolPage.AddRange(State.PageCards());
            for (int i = 0; i < _poolViews.Count; i++)
            {
                var v = _poolViews[i];
                if (i >= _poolPage.Count) { v.gameObject.SetActive(false); continue; }
                var def = _poolPage[i];
                v.gameObject.SetActive(true);
                v.SetData(BattleDriver.ToCardData(def, def.Faction));
                v.SetFace(CardFace.Full);
                var mark = State.CanAdd(def);
                v.SetHighlight(mark == DeckError.None ? CardHighlightState.Playable : CardHighlightState.Normal);
            }
        }

        void RefreshDeckList()
        {
            var shown = new List<string>(State.Deck.CardIds);
            if (State.Deck.WarlordId != null) shown.Insert(0, State.Deck.WarlordId);
            if (State.Deck.DefensiveId != null) shown.Insert(1, State.Deck.DefensiveId);

            for (int i = 0; i < _deckRowQuads.Count; i++)
            {
                bool on = i < shown.Count;
                _deckRowQuads[i].gameObject.SetActive(on);
                _deckRowTexts[i].gameObject.SetActive(on);
                if (!on) continue;
                var c = State.Find(shown[i]);
                _deckRowTexts[i].SetText(c == null ? "?" : CardText.Name(c.Name, c.NameZh));
            }
        }

        void RefreshHeader()
        {
            for (int i = 0; i < _deckTabTexts.Count; i++)
            {
                bool on = i < Library.Count;
                _deckTabTexts[i].gameObject.SetActive(on);
                if (!on) continue;
                bool cur = (i == Library.CurrentIndex);
                _deckTabTexts[i].SetText((cur ? "▶ " : "   ") + Library.Decks[i].Name);
                _deckTabTexts[i].SetColor(cur ? new Color(0.95f, 0.85f, 0.5f) : new Color(0.6f, 0.6f, 0.65f));
            }
            int total = State.DeckCount + (State.Deck.WarlordId != null ? 1 : 0)
                      + (State.Deck.DefensiveId != null ? 1 : 0);
            _counter.SetText($"{total} / {State.MaxDeckCount + 2}");
            var err = State.Validate();
            _verdict.SetText(err == DeckError.None ? "合法" : DeckRules.Describe(err));
            _verdict.SetColor(err == DeckError.None ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.95f, 0.6f, 0.4f));
            _pageLabel.SetText($"第 {State.Page + 1} / {State.PageCount} 页   共 {State.MatchCount} 张");
            _storeErr.SetText(Library.LastError ?? "");
        }

        void Say(string s)
        {
            _noticeText = s ?? "";
            _notice.SetText(_noticeText);
            _noticeUntil = Time.unscaledTime + 2.5f;
        }

        // ============================================================ 交互

        void HandlePointer()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 sp = mouse.position.ReadValue();
            Vector3 wp = LayoutSpace.ScreenToWorld(sp, _cam);

            bool downL = mouse.leftButton.wasPressedThisFrame;
            bool downR = mouse.rightButton.wasPressedThisFrame;

            // ---- 拖拽中：让那一行跟着鼠标，松开时判定 ----
            if (_dragging && _dragQuad != null)
            {
                _dragQuad.transform.position = new Vector3(wp.x, wp.y, _dragQuad.transform.position.z);
                if (_dragText != null) _dragText.transform.position = new Vector3(wp.x, wp.y, _dragText.transform.position.z);
                // 超过阈值才算「拖」——没超过就是「点」（松开时弹放大窗，照原版）
                if (!_draggingMoved && Mathf.Abs(wp.x - _dragOrigin.x) > 0.15f) _draggingMoved = true;
                if (mouse.leftButton.wasReleasedThisFrame) { EndDrag(sp.x); return; }
                if (!mouse.leftButton.isPressed) { EndDrag(sp.x); return; }
                return;
            }

            if (downL || downR)
            {
                if (HandlePoolClick(wp, downR)) return;
                if (HandleDeckRowClick(wp, downL)) return;
                if (downL && HandleButtons(wp)) return;
            }
        }

        /// <summary>卡池：**左键放大 / 右键加牌**（照原版 `DeckEditingWindow__OnCardClick.c:16,42`）。</summary>
        bool HandlePoolClick(Vector3 wp, bool right)
        {
            for (int i = 0; i < _poolViews.Count && i < _poolPage.Count; i++)
            {
                var v = _poolViews[i];
                if (!v.gameObject.activeSelf || !v.Contains(wp)) continue;
                var def = _poolPage[i];
                if (right) TryAddCard(def);
                else OpenCardWindow(def);
                return true;
            }
            return false;
        }

        /// <summary>卡组条目：**左键放大**（原版**不是删除**）；按住拖出侧栏 = 删除。</summary>
        bool HandleDeckRowClick(Vector3 wp, bool left)
        {
            for (int i = 0; i < _deckRowQuads.Count; i++)
            {
                var q = _deckRowQuads[i];
                if (!q.gameObject.activeSelf || !Hit(q, wp)) continue;
                if (!left) return true;
                var row = DeckRowAt(i);
                if (row == null) return true;
                // 记下起点：移动超过阈值就是「拖出」，否则松开时按「点击」处理（弹大图）
                _dragRow = i;
                _dragQuad = q; _dragText = _deckRowTexts[i];
                _dragOrigin = wp;
                _dragging = true;
                _draggingMoved = false;
                return true;
            }
            return false;
        }

        bool _draggingMoved;

        void EndDrag(float screenX)
        {
            _dragging = false;
            var q = _dragQuad; var t = _dragText; int row = _dragRow;
            _dragQuad = null; _dragText = null; _dragRow = -1;

            // 没怎么动 = 点击 ⇒ 弹放大窗（照原版）
            if (!_draggingMoved)
            {
                RefreshDeckList();
                var def0 = DeckRowAt(row);
                if (def0 != null) OpenCardWindow(def0);
                return;
            }
            // 拖出侧栏 = 删除（照原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）
            if (screenX > SidebarRight * (Screen.width / ScreenW))
            {
                var def = DeckRowAt(row);
                if (def != null && State.TryRemove(def))
                {
                    CommitDeck();
                    Say($"已移出卡组：{CardText.Name(def.Name, def.NameZh)}");
                }
            }
            RefreshAll();
        }

        CardDef DeckRowAt(int row)
        {
            var shown = new List<string>(State.Deck.CardIds);
            if (State.Deck.WarlordId != null) shown.Insert(0, State.Deck.WarlordId);
            if (State.Deck.DefensiveId != null) shown.Insert(1, State.Deck.DefensiveId);
            if (row < 0 || row >= shown.Count) return null;
            return State.Find(shown[row]);
        }

        bool HandleButtons(Vector3 wp)
        {
            if (HitNamed("btn_prev", wp)) { if (State.PrevPage()) RefreshPool(); return true; }
            if (HitNamed("btn_next", wp)) { if (State.NextPage()) RefreshPool(); return true; }
            if (HitNamed("btn_done", wp)) { CommitDeck(); Say("已保存"); return true; }
            if (HitNamed("btn_new", wp))
            {
                Library.Create(Library.UniqueName("新卡组"));
                State.LoadDeck(Library.Current);
                RefreshAll(); Say("已新建卡组"); return true;
            }
            if (HitNamed("btn_dup", wp))
            {
                Library.Duplicate(Library.CurrentIndex);
                State.LoadDeck(Library.Current);
                RefreshAll(); Say("已复制卡组"); return true;
            }
            if (HitNamed("btn_del", wp))
            {
                if (!Library.Delete(Library.CurrentIndex)) Say("删不掉：" + (Library.LastError ?? "未知原因"));
                else { State.LoadDeck(Library.Current); Say("已删除"); }
                RefreshAll(); return true;
            }
            // 卡组库页签：切当前卡组
            for (int i = 0; i < _deckTabTexts.Count; i++)
            {
                var q = _deckTabTexts[i];
                if (!q.gameObject.activeSelf) continue;
                if (HitLabel(q, wp))
                {
                    Library.Select(i);
                    State.LoadDeck(Library.Current);
                    RefreshAll(); return true;
                }
            }
            return false;
        }

        void TryAddCard(CardDef def)
        {
            string why;
            if (State.TryAdd(def, out why)) { CommitDeck(); RefreshAll(); Say("已加入：" + CardText.Name(def.Name, def.NameZh)); }
            else Say(why);
        }

        /// <summary>写回卡组库（原版是 `syncedToServer=false` 标脏 + Done 时才上传；我们单机直接落盘）。</summary>
        void CommitDeck()
        {
            Library.CommitCurrent(State.Deck);
            RefreshHeader();
        }

        CardDisplayWindow _cardWindow;

        void OpenCardWindow(CardDef def)
        {
            if (def == null) return;
            // 复用对战的放大窗（第 10 行查证：原版放大窗与战斗里那个是同一套 `CardDisplayWindow`）
            if (_cardWindow == null) _cardWindow = CardDisplayWindow.Create(Root);
            _cardWindow.Show(BattleDriver.ToCardData(def, def.Faction));
        }

        // ============================================================ 命中判定

        bool Hit(ImageQuad q, Vector3 wp)
        {
            if (q == null || !q.gameObject.activeSelf) return false;
            var p = q.transform.position;
            return Mathf.Abs(wp.x - p.x) <= q.WorldW * 0.5f && Mathf.Abs(wp.y - p.y) <= q.WorldH * 0.5f;
        }

        bool HitNamed(string name, Vector3 wp)
        {
            ImageQuad q;
            return _quads.TryGetValue(name, out q) && Hit(q, wp);
        }

        bool HitLabel(Label l, Vector3 wp)
        {
            return l != null && l.gameObject.activeSelf && l.Contains(wp);
        }

        // ============================================================ 建图工具（与自检共用）

        public Vector3 Pos(float px, float py)
        {
            return new Vector3((px - ScreenW * 0.5f) / PxPerUnit, (ScreenH * 0.5f - py) / PxPerUnit, 0f);
        }

        public float U(float px) { return px / PxPerUnit; }

        ImageQuad Quad(Texture2D tex, float cx, float cy, float w, float h, string name, float z = 0f)
        {
            var q = ImageQuad.Create(Root, tex, Pos(cx, cy) + new Vector3(0f, 0f, z), U(h),
                                     new Vector2(0.5f, 0.5f), name);
            _quads[name] = q;
            return q;
        }

        Label Text(string s, float cx, float cy, int scale, Color c, string name = null)
        {
            return Label.Create(Root, s, Pos(cx, cy), scale, c, new Vector2(0.5f, 0.5f), name);
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        /// <summary>自检/演示用的一副卡组：能凑合法就凑合法，凑不出就有什么用什么。</summary>
        public static PlayerDeck PlayerDeckForDemo(DeckEditorState state)
        {
            var deck = new PlayerDeck { Name = "复仇者之刃" };
            CardDef warlord = null;
            foreach (var c in state.Pool) if (c.Type == "hero") { warlord = c; break; }
            if (warlord == null) return deck;
            deck.WarlordId = warlord.Id;
            foreach (var c in state.Pool)
                if (c.Type == "defence" && DeckRules.SameFaction(c.Faction, warlord.Faction)) { deck.DefensiveId = c.Id; break; }
            foreach (var c in state.Pool)
            {
                if (deck.CardIds.Count >= DeckRules.ClassicCards) break;
                if (c.Type != "unit" || !DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && deck.CardIds.Count < DeckRules.ClassicCards; i++)
                    deck.CardIds.Add(c.Id);
            }
            return deck;
        }
    }
}
