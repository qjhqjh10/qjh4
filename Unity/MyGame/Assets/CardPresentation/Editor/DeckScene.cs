// DeckScene.cs — 卡组编辑界面的构建 + 自检
//
// 两个入口（和 BattleScene 一个套路）：
//   DeckScene.BuildAndSaveScene   建出 `Assets/CardPresentation/Scenes/DeckEditor.unity`，打开按 Play 就能用
//   DeckScene.Run                 批处理自检：状态断言 + 建场景 + 截图（grep "^DK "）
//
// ---- 坐标怎么来的 ----
// 这套布局「可见高恒 10 世界单位」，1080p 下 **108 px = 1 世界单位**；
// 原版界面按 1920×1080 设计的，所以 `P(px, py)` 把原版像素直接换算过来，
// 单位（宽度/高度）走 `U(px)`。出处见 `资料/卡组编辑_原版数值与实现方案.md` 第三节。
//
// ---- 哪些数是原版量出来的、哪些是我们挑的 ----
//   ✅ 原版量出来的：面板/行/按钮/图标尺寸（335 侧栏、318×54 卡表行、71×71 按钮、350×512 卡位…）
//   ⚠️ **我们挑的**：一屏 4 列 × 3 行、卡位缩到 0.85 —— 原版是回收滚动列表
//      （`RecyclableScrollRect`），节点树里给不出「一屏几列」。**这不是原版的做法**，别当原版抄。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuleEngine;
using CardPresentation;

// ⚠️ 放在**全局命名空间**（不加 namespace）—— `-executeMethod DeckScene.Run` 认的就是这个名字。
//    和 `BattleScene` / `CardBaseDemo` 一致；放进命名空间的话 Unity 会说「class could not be found」。
public static class DeckScene
{
        const string P = "DK ";
        const string ScenePath = "Assets/CardPresentation/Scenes/DeckEditor.unity";
        const string ShotDir = @"d:/4/_tmp_view/deck";

        /// <summary>原版界面按 1920×1080 设计；这套布局可见高 10 单位 → 108 px/单位</summary>
        const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;   // = 108
        const float ScreenW = 1920f, ScreenH = 1080f;

        // ⚠️ 我们挑的（原版是回收滚动列表，节点树给不出一屏几列）
        const int Cols = 4, Rows = 3;
        const float CardScale = 0.62f;
        // 卡格区域：侧栏占 0..350，筛选栏占 1600..1900，中间 350..1600 才是卡池的地盘。
        // 4 列 × 260 步进、3 行 × 260 步进 —— 第一版把步进写成 330、起始 x 写成 980，
        // 结果第 4 列压到筛选栏上、第 3 行跑到屏幕外（截图看出来的）
        const float GridCx = 975f, GridCy = 520f, GridStepX = 250f, GridStepY = 260f;

        static int _pass, _fail;
        static readonly List<string> _failures = new List<string>();

        static Transform _root;

        // ============================================================ 工具

        /// <summary>原版像素坐标 → 世界坐标（原点在屏幕中心，y 向上）</summary>
        static Vector3 Pos(float px, float py)
        {
            return new Vector3((px - ScreenW * 0.5f) / PxPerUnit, (ScreenH * 0.5f - py) / PxPerUnit, 0f);
        }

        /// <summary>原版像素长度 → 世界单位</summary>
        static float U(float px) { return px / PxPerUnit; }

        /// <param name="z">越**大**离相机越远（相机在 z=-20 朝 +z 看）。
        /// 同 z 的两个 quad 谁压谁看渲染顺序、不确定 —— HUD 那边也踩过这条，所以显式分层。</param>
        static ImageQuad Quad(Texture2D tex, float cx, float cy, float w, float h, string name, float z = 0f)
        {
            var q = ImageQuad.Create(_root, tex, Pos(cx, cy) + new Vector3(0f, 0f, z), U(h),
                                     new Vector2(0.5f, 0.5f), name);
            return q;
        }
        const float ZPanel = 0.30f, ZRow = 0.10f, ZBar = 0.10f;

        static Label Text(string s, float cx, float cy, int scale, Color c, string name = null)
        {
            return Label.Create(_root, s, Pos(cx, cy), scale, c, new Vector2(0.5f, 0.5f), name);
        }

        static void Section(string t) { Debug.Log(P + $"--- {t} ---"); }

        static void Check<T>(T got, T want, string msg)
        {
            if (EqualityComparer<T>.Default.Equals(got, want)) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
            else
            {
                _fail++;
                var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
                _failures.Add(line);
                Debug.LogError(P + $"   ✗ {line}");
            }
        }

        static void CheckTrue(bool c, string msg) { Check(c, true, msg); }

        // ============================================================ 自检

        public static void Run()
        {
            _pass = 0; _fail = 0; _failures.Clear();
            Directory.CreateDirectory(ShotDir);
            Debug.Log(P + "=== 卡组编辑自检 开始 ===");

            Section("状态：卡池与筛选");
            TestFilters();

            Section("状态：加牌 / 删牌 / 督军");
            TestEditing();

            Section("状态：费用曲线与翻页");
            TestCurveAndPaging();

            Section("版面");
            var state = Build(out _root);
            TestLayout(state);

            Shoot("deck_editor.png");
            SaveScene();

            int total = _pass + _fail;
            if (_fail == 0) Debug.Log(P + $"=== 结束：{_pass}/{total} 全过 ✅ ===");
            else
            {
                var sb = new System.Text.StringBuilder(P + $"=== 结束：{_pass}/{total} 通过，**{_fail} 条失败** ❌ ===");
                foreach (var f in _failures) sb.Append("\n").Append(P).Append("   ✗ ").Append(f);
                Debug.LogError(sb.ToString());
            }
            if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
        }

        static DeckEditorState NewState()
        {
            var pool = CardDatabase.Load();
            return new DeckEditorState(pool);
        }

        static void TestFilters()
        {
            var s = NewState();
            CheckTrue(s.PoolCount > 1000, $"卡池加载到 {s.PoolCount} 张");

            var all = s.VisibleCards().Count;
            Check(all, s.PoolCount, "不设筛选时命中全部");

            var f = DeckFilter.None; f.Faction = "Ultramarines";
            s.SetFilter(f);
            int ult = s.VisibleCards().Count;
            CheckTrue(ult > 0 && ult < all, $"按阵营筛 → {ult} 张（少于全部）");
            foreach (var c in s.VisibleCards())
                if (!DeckRules.SameFaction(c.Faction, "Ultramarines")) { Check(true, false, "筛出来的卡阵营不对"); break; }

            f = DeckFilter.None; f.Rarity = "legendary"; f.Faction = "Ultramarines";
            s.SetFilter(f);
            int leg = s.VisibleCards().Count;
            CheckTrue(leg > 0 && leg < ult, $"再加稀有度筛 → {leg} 张（更少）");

            f = DeckFilter.None; f.Cost = 3; f.Faction = "Ultramarines";
            s.SetFilter(f);
            foreach (var c in s.VisibleCards())
                if (c.Cost != 3) { Check(true, false, "费用筛选没生效"); break; }
            CheckTrue(s.VisibleCards().Count > 0, "按费用 3 筛能得到卡");

            f = DeckFilter.None; f.Name = "autarch";
            s.SetFilter(f);
            CheckTrue(s.VisibleCards().Count >= 1, "按卡名子串筛（大小写不敏感）能命中 Autarch");

            f = DeckFilter.None; f.Type = "hero";
            s.SetFilter(f);
            CheckTrue(s.VisibleCards().Count == 57, $"按类型筛出 57 个督军（实际 {s.VisibleCards().Count}）");

            s.SetFilter(DeckFilter.None);
            Check(s.VisibleCards().Count, s.PoolCount, "清掉筛选又回到全部");

            CheckTrue(s.Factions().Count == 13, $"阵营下拉有 13 项（实际 {s.Factions().Count}）");
            CheckTrue(s.Costs().Count > 0, "费用下拉非空");
        }

        static void TestEditing()
        {
            var s = NewState();
            CardDef warlord = null, unitA = null, unitB = null, tactic = null, def = null;
            foreach (var c in s.Pool)
            {
                if (warlord == null && c.Type == "hero") warlord = c;
            }
            foreach (var c in s.Pool)
            {
                if (!DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                // ⚠️ 专门挑**非传说**的单位卡来测「同名 2 张」—— 传说卡上限是 1，拿它测必然失败
                if (unitA == null && c.Type == "unit" && !DeckRules.IsLegendary(c.Rarity)) unitA = c;
                else if (unitB == null && c.Type == "unit" && c.Id != unitA?.Id) unitB = c;
                if (tactic == null && c.Type == "tactic") tactic = c;
                if (def == null && c.Type == "defence") def = c;
            }
            CheckTrue(warlord != null && unitA != null && unitB != null && def != null,
                      $"找到测试用的卡（督军 {warlord?.Name} / 单位 {unitA?.Name} / 防御 {def?.Name}）");

            string why;
            CheckTrue(s.TryAdd(warlord, out why), $"加督军（{why}）");
            Check(s.Deck.WarlordId, warlord.Id, "督军进了督军位，不是普通卡位");
            Check(s.DeckCount, 0, "督军不占 30 张的名额");

            s.TryAdd(def, out why);
            Check(s.Deck.DefensiveId, def.Id, "防御卡进了防御位，也不占名额");

            CheckTrue(s.TryAdd(unitA, out why), $"加单位卡（{why}）");
            CheckTrue(s.TryAdd(unitA, out why), $"同名第 2 张能加（{why}）");
            Check(s.DeckCount, 2, "卡组里 2 张");
            bool okThird = s.TryAdd(unitA, out why);
            CheckTrue(!okThird, $"同名第 3 张被挡（{why}）");
            CheckTrue(!string.IsNullOrEmpty(why), "被挡时给出了人话原因");

            // 传说卡：第 2 张就该被挡
            CardDef legendary = null;
            foreach (var c in s.Pool)
                if (c.Type == "unit" && DeckRules.SameFaction(c.Faction, warlord.Faction)
                    && DeckRules.IsLegendary(c.Rarity) && c.Id != unitA.Id) { legendary = c; break; }
            if (legendary != null)
            {
                s.TryAdd(legendary, out why);
                CheckTrue(!s.TryAdd(legendary, out why), $"传说卡第 2 张被挡（{why}）");
            }

            // 别阵营的卡
            CardDef other = null;
            foreach (var c in s.Pool)
                if (c.Type == "unit" && !DeckRules.SameFaction(c.Faction, warlord.Faction)) { other = c; break; }
            if (other != null)
                CheckTrue(!s.TryAdd(other, out why), $"别阵营的卡被挡（{why}）");

            // 换督军会清掉不合阵营的卡
            CardDef otherWarlord = null;
            foreach (var c in s.Pool)
                if (c.Type == "hero" && !DeckRules.SameFaction(c.Faction, warlord.Faction)) { otherWarlord = c; break; }
            if (otherWarlord != null)
            {
                int before = s.DeckCount;
                int removed = s.SetWarlord(otherWarlord.Id);
                Check(s.Deck.WarlordId, otherWarlord.Id, "换督军成功");
                Check(s.DeckCount, 0, "换督军把不合阵营的卡清空了");
                CheckTrue(removed > 0, $"并报告清掉了几张（{removed} 张，原来是 {before} 张）");
                CheckTrue(string.IsNullOrEmpty(s.Deck.DefensiveId), "防御卡也被清掉了（阵营不合）");
            }

            // 删
            s.SetWarlord(warlord.Id);
            s.TryAdd(unitA, out why);
            s.TryAdd(unitA, out why);
            Check(s.Deck.CountOf(unitA.Id), 2, "加回 2 张");
            CheckTrue(s.TryRemove(unitA), "删掉 1 张");
            Check(s.Deck.CountOf(unitA.Id), 1, "还剩 1 张");
            CheckTrue(s.TryRemove(unitA), "再删 1 张");
            CheckTrue(!s.TryRemove(unitA), "已经没有了 → 删不动");

            // 正常路径下加满 30 张 → 第 31 张被挡
            s.ClearCards();
            s.SetWarlord(warlord.Id);
            s.TryAdd(def, out why);
            int guard = 0;
            foreach (var c in s.Pool)
            {
                if (s.DeckCount >= s.MaxDeckCount || guard > 5000) break;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity); i++) { s.TryAdd(c, out why); guard++; }
            }
            Check(s.DeckCount, s.MaxDeckCount, $"能加满 {s.MaxDeckCount} 张");
            var overflow = s.Find(unitA.Id);
            CheckTrue(!s.TryAdd(overflow, out why), $"满 30 张后第 31 张被挡（{why}）");
        }

        static void TestCurveAndPaging()
        {
            var s = NewState();
            var curve = s.CostCurve();
            Check(curve.Length, 21, "费用曲线数组是 0..20");
            int sum = 0; foreach (var n in curve) sum += n;
            Check(sum, 0, "空卡组曲线全 0");

            CardDef unit = null;
            foreach (var c in s.Pool) if (c.Type == "unit" && c.Cost <= 20) { unit = c; break; }
            string why;
            for (int i = 0; i < DeckRules.CopyLimit(unit.Rarity); i++) s.TryAdd(unit, out why);
            curve = s.CostCurve();
            Check(curve[unit.Cost], DeckRules.CopyLimit(unit.Rarity),
                  $"曲线在费用 {unit.Cost} 上记到 {DeckRules.CopyLimit(unit.Rarity)} 张");

            // 翻页：筛选一变页码归零
            s.SetFilter(DeckFilter.None);
            Check(s.Page, 0, "页码从 0 开始");
            CheckTrue(s.PageCount > 1, $"全部卡池超过一页（共 {s.PageCount} 页）");
            var p0 = s.PageCards();
            Check(p0.Count, DeckEditorState.PageSize, $"第 0 页正好 {DeckEditorState.PageSize} 张");
            CheckTrue(s.NextPage(), "能翻到下一页");
            Check(s.Page, 1, "页码变成 1");
            var p1 = s.PageCards();
            CheckTrue(p1.Count > 0 && p1[0].Id != p0[0].Id, "第 1 页的内容和第 0 页不同");
            CheckTrue(s.PrevPage(), "能翻回来");
            Check(s.Page, 0, "回到第 0 页");
            CheckTrue(!s.PrevPage(), "第 0 页再往前翻 → 翻不动");

            var f = DeckFilter.None; f.Type = "hero";
            s.SetFilter(f);
            Check(s.Page, 0, "改筛选后页码归零（不归零会停在一个筛选后不存在的页上）");
            CheckTrue(s.PageCount >= 1, "督军那 57 张也有页数");

            // 页码越界要夹回来
            s.SetFilter(DeckFilter.None);
            s.NextPage(); s.NextPage();
            var narrow = DeckFilter.None; narrow.Name = "autarch";
            s.Filter = narrow;              // 故意绕过 SetFilter，制造越界
            s.ClampPage();
            CheckTrue(s.Page < s.PageCount, $"页码被夹回合法范围（{s.Page} < {s.PageCount}）");
        }

        static void TestLayout(DeckEditorState state)
        {
            var quads = _root.GetComponentsInChildren<ImageQuad>(true);
            CheckTrue(quads.Length >= 10, $"画出来的图至少有 10 张（实际 {quads.Length}）");

            // 版面：所有图都在可见区里
            float halfW = LayoutSpace.VisibleWidth * 0.5f, halfH = LayoutSpace.DesignHeight * 0.5f;
            int off = 0;
            foreach (var q in quads)
            {
                var p = q.transform.localPosition;
                if (Mathf.Abs(p.x) > halfW + 0.01f || Mathf.Abs(p.y) > halfH + 0.01f) off++;
            }
            Check(off, 0, "所有 UI 图都落在可见区内（没有跑到屏幕外）");

            CheckTrue(state.VisibleCards().Count > 0, "版面用的状态里有卡可显示");
            var page = state.PageCards();
            CheckTrue(page.Count > 0 && page.Count <= DeckEditorState.PageSize,
                      $"页面上的卡 {page.Count} 张（≤ {DeckEditorState.PageSize}）");
        }

        // ============================================================ 建场景

        static DeckEditorState Build(out Transform root)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            // ⚠️ 宽高比必须在建任何东西之前定死 —— 位置/尺寸都是建的时候算一次，
            //    批处理下相机默认 4:3，建完再改 16:9 会整片错位（踩过）
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);

            _root = new GameObject("DeckEditor").transform;
            root = _root;

            // ---- 状态 ----
            var state = NewState();
            state.LoadDeck(PlayerDeckForDemo(state));

            // ---- 左边栏（原版 335 px 宽）----
            Quad(CardArt.DeckUi("UI_Deck_Selection_Back"), 175f, 540f, 439f, 664f, "sidebar_panel", ZPanel);
            Text("卡组编辑", 175f, 150f, 3, Color.white, "title");
            Text(state.Deck.Name, 175f, 200f, 2, new Color(0.9f, 0.85f, 0.6f), "deck_name");

            // 卡组内容：原版卡表行 318×54
            float rowY = 260f;
            var rowTex = CardArt.DeckUi("40k_deck_cardlist_bg");
            var shown = new List<string>(state.Deck.CardIds);
            if (state.Deck.WarlordId != null) shown.Insert(0, state.Deck.WarlordId);
            if (state.Deck.DefensiveId != null) shown.Insert(1, state.Deck.DefensiveId);
            for (int i = 0; i < shown.Count && i < 8; i++)
            {
                var c = state.Find(shown[i]);
                Quad(rowTex, 175f, rowY, 318f, 54f, "deck_row_" + i, ZRow);
                Text(c == null ? "?" : c.Name, 175f, rowY, 1, Color.white, "deck_row_text_" + i);
                rowY += 58f;
            }
            if (shown.Count > 8) Text($"…另有 {shown.Count - 8} 张", 175f, rowY, 1, Color.gray, "deck_more");

            // 计数 + Done（原版 Done 189×50）
            int total = state.DeckCount + (state.Deck.WarlordId != null ? 1 : 0)
                      + (state.Deck.DefensiveId != null ? 1 : 0);
            Text($"{total} / {state.MaxDeckCount + 2}", 175f, 960f, 2, Color.white, "counter");
            var err = state.Validate();
            Text(err == DeckError.None ? "合法" : DeckRules.Describe(err), 175f, 910f, 1,
                 err == DeckError.None ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.95f, 0.6f, 0.4f), "verdict");
            Quad(CardArt.DeckUi("40k_general_bt_yellow_confirm"), 130f, 1000f, 71f, 71f, "btn_done");
            Quad(CardArt.DeckUi("40k_general_bt_yellow_close"), 230f, 1000f, 71f, 71f, "btn_close");

            // ---- 中间：卡池（4 列 × 3 行）⚠️ 行列数是我们挑的 ----
            float gx = GridCx, gy = GridCy, stepX = GridStepX, stepY = GridStepY;
            var page = state.PageCards();
            for (int i = 0; i < page.Count; i++)
            {
                int col = i % Cols, row = i / Cols;
                // ⚠️ 要从**中心减去半宽**，不能让第 0 列落在中心上 ——
                //    第一版写成 `gx + col*stepX`，结果 4 列整体右移半格，第 4 列压到筛选栏上（截图看出来的）
                float cx = gx + (col - (Cols - 1) * 0.5f) * stepX;
                float cy = gy + (row - (Rows - 1) * 0.5f) * stepY;
                var data = ToCardData(page[i]);
                var v = CardView.Create(root, data, "pool_" + i);
                v.SetPose(Pos(cx, cy), 0f, CardScale);
                var mark = state.CanAdd(page[i]);
                if (mark == DeckError.None) v.SetHighlight(CardHighlightState.Playable);
            }

            // ---- 筛选栏（原版 332 px 宽，在右侧）----
            float fx = 1750f, fy = 200f;
            Text("筛选", fx, fy, 2, Color.white, "filter_title");
            string[] pills = { "全部", "军  Ultramarines", "稀有度  传说", "费用  3", "类型  单位" };
            for (int i = 0; i < pills.Length; i++)
            {
                Quad(CardArt.DeckUi("40K_dropdown_field_closed"), fx, fy + 70f + i * 90f, 300f, 42f, "filter_field_" + i);
                Text(pills[i], fx, fy + 70f + i * 90f, 1, Color.white, "filter_text_" + i);
            }
            Quad(CardArt.DeckUi("40k_bt_icon_search"), fx, fy + 70f + pills.Length * 90f, 27f, 27f, "filter_search");

            // 翻页（不是原版的做法，原版是滚动列表）
            Quad(CardArt.DeckUi("40k_general_bt_yellow_back"), 700f, 1010f, 71f, 71f, "btn_prev", ZBar);
            Quad(CardArt.DeckUi("40k_general_bt_yellow_confirm"), 800f, 1010f, 71f, 71f, "btn_next", ZBar);
            Text($"第 {state.Page + 1} / {state.PageCount} 页   共 {state.MatchCount} 张",
                 1000f, 1010f, 2, Color.white, "page_label");

            return state;
        }

        /// <summary>自检/截图用的一副演示卡组：能凑合法就凑合法，凑不出就有什么用什么。</summary>
        static PlayerDeck PlayerDeckForDemo(DeckEditorState state)
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

        static CardData ToCardData(CardDef c)
        {
            return new CardData
            {
                id = c.Id,
                title = c.Name,
                cost = c.Cost,
                melee = c.Attack,
                ranged = c.RangedAttack,
                health = c.Health,
                keywords = string.Join(" · ", c.Keywords.Keys),
                isUnit = c.Type == "unit",
                frame = FactionColor(c.Faction),
                faction = c.Faction,
            };
        }

        static readonly Color[] Palette =
        {
            new Color(0.72f, 0.24f, 0.22f), new Color(0.24f, 0.48f, 0.72f),
            new Color(0.30f, 0.60f, 0.34f), new Color(0.62f, 0.50f, 0.20f),
            new Color(0.52f, 0.30f, 0.66f), new Color(0.24f, 0.58f, 0.58f),
        };

        static Color FactionColor(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return Palette[0];
            int h = 0;
            foreach (char ch in faction) h = (h * 31 + ch) & 0x7fffffff;
            return Palette[h % Palette.Length];
        }

        // ============================================================ 截图 / 存场景

        static void Shoot(string file)
        {
            var cam = Camera.main;
            if (cam == null) return;
            const int W = 1920, H = 1080;
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            RenderTexture.ReleaseTemporary(rt);
            Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
        }

        static void SaveScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            Debug.Log(P + $"  场景 {ScenePath}");
        }

        /// <summary>建出场景存盘，给人打开按 Play 用。</summary>
        public static void BuildAndSaveScene()
        {
            Directory.CreateDirectory(ShotDir);
            Transform root;
            var state = Build(out root);
            Shoot("deck_editor.png");
            SaveScene();
            Debug.Log(P + $"=== 场景已重建：{ScenePath}"

                        + $"（{state.PoolCount} 张卡池，演示卡组 {state.Deck.Name}）===");
        }
}
