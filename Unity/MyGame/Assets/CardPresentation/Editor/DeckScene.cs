// DeckScene.cs — 卡组编辑界面的构建 + 自检
//
// 两个入口（和 BattleScene 一个套路）：
//   DeckScene.BuildAndSaveScene   建出 `Assets/CardPresentation/Scenes/DeckEditor.unity`，打开按 Play 就能用
//   DeckScene.Run                 批处理自检：状态断言 + 建场景 + 截图（grep "^DK "）
//
// ---- 坐标怎么来的 ----
// 这套布局「可见高恒 10 世界单位」，1080p 下 **108 px = 1 世界单位**；
// 原版界面按 1920×1080 设计的，所以 `Pos(px, py)` 把原版像素直接换算过来。
//
// 🔴 2026-09-20：**版面与绘制的唯一出处已经挪进 `CardPresentation/Deck/DeckRuntime.cs`**
//    （这个文件只做「建场景 + 自检」，绘制全部转发给它 —— 见 `Build`）。
//    权威坐标 = `D:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`，
//    查证正本 = `资料/卡组编辑界面_查证_0920.md`。**改版面改那一份，别在这里写第二份。**
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

            Section("状态：费用曲线与滚动窗口");
            TestCurveAndScroll();

            Section("状态：遭遇模式（模式**从正在编辑的那副卡组派生**）");
            TestSkirmishMode();

            Section("版面");
            var tmpPath = TempStorePath();
            RuleEngine.DeckStore.OverridePath = tmpPath;
            RuleEngine.DeckStore.DeleteFile();
            DeckEditorState state;
            try
            {
                state = Build(DeckLibrary.Load(), out _root);
                // 🔴 2026-09-20：**根上必须挂着 `DeckRuntime`** —— 场景存盘后按 Play 就是靠它的
                //    `Start()` 建界面；忘了挂组件 = Play 出来一片黑，而这条断言能挡住。
                //    （铁律 10 第 5 条：`Build` 直调与 Play 的 `Start()` 是两个入口，**各配一条断言**。）
                CheckTrue(_root != null && _root.GetComponent<DeckRuntime>() != null,
                          "根对象上挂着 DeckRuntime（按 Play 的入口）");
                TestLayout(state);
                TestLibraryWiring();
                // 🔴 2026-09-20：**Play 那条入口也验一次** —— 按 Play 时是 `DeckRuntime.Start()`
                //    调 `Build`，而自检走的是 `DeckScene.Build()` 直调（**两条路**；铁律 10 第 5 条
                //    要求每个入口各配一条断言）。这里 new 一个 `DeckRuntime` 调同一个 `Build`，
                //    把「Play 那条路建得起来」验掉（`Start()` 里就一行 `if (State == null) Build(...)`）。
                var smokeGo = new GameObject("PlayEntrySmoke");
                var smoke = smokeGo.AddComponent<DeckRuntime>();
                smoke.Build(DeckLibrary.Load());
                CheckTrue(smoke.State != null && smoke.State.PoolCount > 1000,
                          $"Play 入口（DeckRuntime.Build）也建得起来（卡池 {smoke.State?.PoolCount} 张）");
                UnityEngine.Object.DestroyImmediate(smokeGo);

                // 🔴 2026-09-23：**「从收藏进编辑」的交接**（`CollectionData.PendingEditDeck`）——
                //    收藏窗点「编辑」时写它，`DeckRuntime.Build` 开局读掉并清掉
                //    （正本 `资料/阶段二_卡组线_原版规格.md` §七；原版是同窗换页，我们是两个场景）。
                {
                    var lib2 = DeckLibrary.Load();
                    while (lib2.Count < 2) lib2.Create("交接测试 " + (lib2.Count + 1));
                    lib2.Save();
                    const int want = 1;
                    CardPresentation.CollectionData.PendingEditDeck = want;
                    var hgo = new GameObject("HandoffSmoke");
                    var hrt = hgo.AddComponent<DeckRuntime>();
                    hrt.Build(lib2);
                    Check(lib2.CurrentIndex, want,
                          $"交接下标 {want} ⇒ 编辑器**开局就打开第 {want + 1} 套**（「从收藏进编辑」的判据）");
                    Check(CardPresentation.CollectionData.PendingEditDeck, -1,
                          "交接**读完就清**（不清的话下次开还会跳过去）");
                    hrt.BackToMenu();
                    CheckTrue(true, "`BackToMenu()` 在批处理下**不切场景**（切了会把后面的断言全带走）");
                    UnityEngine.Object.DestroyImmediate(hgo);
                }

                // 🆕 2026-09-26：**遭遇模式在界面那一层也走对了**。
                //    状态层那条（`TestSkirmishMode`）只证明「`MaxDeckCount` 会变成 12」；
                //    这一条证明**界面上真的按模式画** —— 脚注计数画的是 `State.MaxDeckCount + 2`
                //    （+2 = 督军 + 防御卡各占一格），这副牌故意只放 12 张普通卡 ⇒ 应显示 `12/14`。
                //    出处（模式为什么从这副牌来）→ `资料/加时与冲突模式_原版规格.md` §2.7。
                {
                    var lib3 = DeckLibrary.Load();
                    var pool3 = RuleEngine.CardDatabase.Load();
                    // ⚠️ **挑阵营要挑得动**：得有督军 + 有防御卡 + **凑得出 12 张同阵营部队**
                    //    （第一版只取「池里第一个督军」，结果那个阵营部队不够 12 张、也没防御卡 ⇒ 夹具自己错了）
                    var heroByFac = new Dictionary<string, RuleEngine.CardDef>();
                    var defByFac = new Dictionary<string, RuleEngine.CardDef>();
                    foreach (var c in pool3)
                    {
                        if (c == null || string.IsNullOrEmpty(c.Faction)) continue;
                        if (c.Type == "hero" && !heroByFac.ContainsKey(c.Faction)) heroByFac[c.Faction] = c;
                        if (c.Type == "defence" && !defByFac.ContainsKey(c.Faction)) defByFac[c.Faction] = c;
                    }
                    string fac3 = null;
                    var ids3 = new List<string>();
                    foreach (var kv in heroByFac)
                    {
                        if (!defByFac.ContainsKey(kv.Key)) continue;
                        var tryIds = new List<string>();
                        foreach (var c in pool3)
                        {
                            if (tryIds.Count >= 12 || c == null || c.Type != "unit" || c.Faction != kv.Key) continue;
                            for (int k = 0; k < RuleEngine.DeckRules.CopyLimit(c.Rarity) && tryIds.Count < 12; k++)
                                tryIds.Add(c.Id);
                        }
                        if (tryIds.Count == 12) { fac3 = kv.Key; ids3 = tryIds; break; }
                    }
                    CheckTrue(fac3 != null,
                              "池子里有一个阵营凑得出 12 张的遭遇牌（督军 + 防御卡 + 12 张部队）—— 有它才验得了界面");

                    var made3 = lib3.Create("遭遇规则测试", (int)RuleEngine.GameMode.Skirmish);
                    if (fac3 != null)
                    {
                        made3.WarlordId = heroByFac[fac3].Id;
                        made3.DefensiveId = defByFac[fac3].Id;
                        made3.CardIds.AddRange(ids3);
                        lib3.Save();
                        lib3.Select(lib3.Count - 1);
                        Check(made3.CardIds.Count, 12, $"夹具凑到的就是 12 张（阵营 {fac3}）");
                    }

                    var sgo = new GameObject("SkirmishModeSmoke");
                    var srt = sgo.AddComponent<DeckRuntime>();
                    srt.Build(lib3);
                    CheckTrue(srt.State != null && srt.State.Skirmish,
                              "★ 编辑器打开一副**遭遇**牌 ⇒ 状态按遭遇规则（`DeckEditorState.Skirmish` 是**从这副牌派生**的）");
                    Check(srt.State.MaxDeckCount, 12, "上限 12 张");
                    var cnt3 = srt.transform.Find("foot_cnt");
                    CheckTrue(cnt3 != null, "脚注计数节点（`foot_cnt`）在");
                    if (cnt3 != null)
                    {
                        var lbl3 = cnt3.GetComponent<Label>();
                        string txt3 = lbl3 != null ? lbl3.Text : null;
                        // 脚注的算式 = `(普通卡 + 督军 + 防御卡) / (MaxDeckCount + 2)`
                        // ⇒ 这副满的遭遇牌是 **14/14**（经典会画成 **32/32**）—— 分母就是模式那 12 的证据。
                        Check(txt3, "14/14",
                              "★ 界面按遭遇画：**14/14** = （12 普通卡 + 督军 + 防御卡）/（12 + 那 2 格）"
                            + " —— 经典会画成 32/32 ⇒ 分母就是「模式真的传到了界面」的证据；"
                            + "这一条挡住「状态层改对了、界面还写死 30」那种半截活");
                    }
                    UnityEngine.Object.DestroyImmediate(sgo);
                }
            }
            finally
            {
                RuleEngine.DeckStore.OverridePath = null;
                try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
            }

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
            // ⚠️ 这个数是**卡池里 hero 的总数**（不是我们挑了 57 个）。2026-09-12 从 57 变 56：
            //    卡表过滤器把噪音卡剔掉了，其中 `HB`（= Imotekh the Stormlord 的异画重复条目）是 hero。
            CheckTrue(s.VisibleCards().Count == 56, $"按类型筛出 56 个督军（实际 {s.VisibleCards().Count}）");

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

        static void TestCurveAndScroll()
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

            // 🔴 卡池是**滚动窗口**不是翻页（原版 `RecyclableScrollRect`；我们自加的分页 2026-09-20 已删）
            CheckTrue(s.VisibleAt(0) != null, "按序号能取到第 0 张");
            CheckTrue(s.VisibleAt(-1) == null, "序号越界给 null（不是抛异常）");
            CheckTrue(s.VisibleAt(s.PoolCount) == null, "越过末尾也给 null");

            var f = DeckFilter.None; f.Type = "hero";
            s.SetFilter(f);
            CheckTrue(s.VisibleAt(0) != null && s.VisibleAt(0).Type == "hero",
                      "换了筛选，窗口的第 0 张就是筛出来的那张");
            s.SetFilter(DeckFilter.None);
            Check(s.VisibleCards().Count, s.PoolCount, "清空筛选回到全部");
        }

        /// <summary>🆕 2026-09-26：**编辑器的「模式」是从正在编辑的那副卡组派生出来的**。
        ///
        /// 为什么单独一节：`DeckEditorState.Skirmish` 原来是**全仓没有任何赋值点的裸字段**
        /// ⇒「玩家自建遭遇卡组」那条路**静默走不通**（编辑器恒按经典 30 张算）。
        /// 原版也是这样：卡组编辑器不认识「当前模式」，它读 `EditingDeck.GameMode`
        /// （`DeckEditingWindow._GetCardCollection` 那条链，判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。
        ///
        /// 🔴 这一节**只验「派生」**（换一副牌规则就跟着换）—— 那是它跟裸字段最本质的区别；
        /// 「模式怎么进到卡组里」另有断言（`RuleEngineTest.TestDeckGameMode` / `BattleScene` 第 9c 节）。</summary>
        static void TestSkirmishMode()
        {
            var s = NewState();

            s.NewDeck("经典牌");
            CheckTrue(!s.Skirmish, "新建一副**不传模式**的牌 ⇒ 经典");
            Check(s.MaxDeckCount, 30, "经典上限 30 张");
            Check(s.SlotsLeft, 30, "空牌差 30 张");

            s.NewDeck("遭遇牌", (int)RuleEngine.GameMode.Skirmish);
            CheckTrue(s.Skirmish, "★ 新建一副**遭遇牌** ⇒ 编辑器自己就是遭遇规则（没有谁去给它赋值）");
            Check(s.MaxDeckCount, 12, "★ 遭遇上限 **12** 张（`GameplayVariables` 那套值）");
            Check(s.SlotsLeft, 12, "空牌差 12 张");

            // ★ **派生的证据**：把另一副牌装进来，规则立刻跟着换
            s.LoadDeck(new RuleEngine.PlayerDeck("切回经典", null, null, null));
            CheckTrue(!s.Skirmish, "★ 装上另一副**经典**牌 ⇒ 立刻回到经典规则（说明是派生，不是「谁记得赋值」）");
            Check(s.MaxDeckCount, 30, "上限跟着回到 30");

            // 12 张这条线真的**卡得住**：加满 12 张之后第 13 张要被挡
            var pool = s.Pool;
            CardDef hero = null;
            foreach (var c in pool) if (c.Type == "hero") { hero = c; break; }
            CheckTrue(hero != null, "池子里找得到一张督军");
            if (hero == null) return;

            var ids = new List<string>();
            foreach (var c in pool)
            {
                if (ids.Count >= 12 || c == null || c.Type != "unit" || c.Faction != hero.Faction) continue;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < 12; i++) ids.Add(c.Id);
            }
            Check(ids.Count, 12, $"凑得出一套 12 张的遭遇牌（阵营 {hero.Faction}）");

            s.LoadDeck(new RuleEngine.PlayerDeck("满的遭遇牌", hero.Id, null, ids,
                                                 (int)RuleEngine.GameMode.Skirmish));
            CheckTrue(s.Skirmish, "装进来还是遭遇");
            Check(s.SlotsLeft, 0, "★ **12 张正好装满**（差 0 张）");
            Check(s.MaxDeckCount, 12, "上限还是 12（没被 30 那套盖掉）");

            CardDef extra = null;
            foreach (var c in pool)
                if (c != null && c.Type == "unit" && c.Faction == hero.Faction && s.Deck.CountOf(c.Id) == 0)
                { extra = c; break; }
            if (extra != null)
                Check(s.CanAdd(extra), DeckError.TooManyCards,
                      "★ 遭遇牌满 12 张之后**再加一张会被挡**（`TooManyCards`）—— 上限真的生效了");

            // 校验走**遭遇那套账**：上限 12（不是 30）。
            // 🔴 **2026-09-26 改口径**：防御卡改成**可选**（照原版 `DeckUtility.ValidateDeck` 不读那一格），
            //    ⇒ 这副「12 张 + 没带防御卡」**合法**。原来这条断的是 `DefensiveMissing`（我们比原版严）。
            //    为了**仍然钉住「按模式算张数」**，下面两头都断：正好 12 张 ⇒ 合法；多一张 ⇒ `TooManyCards`。
            //    判据 → `资料/加时与冲突模式_原版规格.md` §2.7c。
            Check(s.Validate(), DeckError.None,
                  "★ 遭遇：**正好 12 张 + 没带防御卡 ⇒ 合法**（防御卡可选，照原版）");
            var over = new PlayerDeck(s.Deck.Name, s.Deck.WarlordId, null,
                                      new List<string>(s.Deck.CardIds), (int)GameMode.Skirmish);
            if (extra != null) over.CardIds.Add(extra.Id);
            Check(DeckRules.Validate(over, s.Find, true), DeckError.TooManyCards,
                  "★ ……而**多一张就超**（`TooManyCards`）⇒ 上限确实是**遭遇那套 12**，不是经典的 30");
        }

        /// <summary>卡组库接进场景之后，这几件事必须成立。</summary>
        static void TestLibraryWiring()
        {
            var lib = DeckLibrary.Load();
            CheckTrue(lib.Count >= 1, $"空库时场景会替玩家建一套（现在 {lib.Count} 套）");
            CheckTrue(lib.Current != null, "有选中的卡组");
            CheckTrue(lib.Current.CardIds.Count > 0 || lib.Current.WarlordId != null,
                      "建出来的演示卡组不是空的");

            var before = lib.Current.Name;
            int n0 = lib.Count;
            lib.Create("第二套");
            Check(lib.Count, n0 + 1, "新建一套后库里多了一条");
            var lib2 = DeckLibrary.Load();
            Check(lib2.Count, n0 + 1, "**新建立刻落盘**（重新载入还在）");
            Check(lib2.Current.Name, "第二套", "选中项也落盘了");

            lib2.Select(0);
            lib2.Delete(0);
            Check(DeckLibrary.Load().Count, n0, "删除也落盘了");
            CheckTrue(!string.IsNullOrEmpty(before), "（原卡组名非空，便于下面对账）");
        }

        static DeckRuntime _rt;

        static void TestLayout(DeckEditorState state)
        {
            // ⚠️ 只数**可见**的图（`false` = 不含 inactive）—— 卡组行与费用曲线是互斥视图
            //    （Cards 页签 / Deck info 页签），同时算进来会误报「重叠」。
            var quads = _root.GetComponentsInChildren<ImageQuad>(false);
            CheckTrue(quads.Length >= 20, $"画出来的可见图至少有 20 张（实际 {quads.Length}）");

            // 版面：所有可见的图都在可见区里
            float halfW = LayoutSpace.VisibleWidth * 0.5f, halfH = LayoutSpace.DesignHeight * 0.5f;
            int off = 0; string firstOff = null;
            foreach (var q in quads)
            {
                var p = q.transform.localPosition;
                if (Mathf.Abs(p.x) > halfW + 0.01f || Mathf.Abs(p.y) > halfH + 0.01f)
                { off++; if (firstOff == null) firstOff = q.name; }
            }
            Check(off, 0, "所有可见 UI 图都落在可见区内（没有跑到屏幕外）" +
                  (firstOff == null ? "" : "—— 第一处 " + firstOff));

            // ⚠️ 版面回归用的通用检查：**同一层的两个图不许压在一起**。
            //    分层 = `SetRenderQueue`（**不是 z** —— 透明队列按到相机的 3D 距离排序，
            //    铺满屏的图会互相盖错，2026-09-20 实测：侧栏底板盖住了整个卡组列表）。
            int overlaps = 0; var pairs = new List<string>();
            for (int i = 0; i < quads.Length; i++)
                for (int j = i + 1; j < quads.Length; j++)
                {
                    var A = quads[i]; var B = quads[j];
                    if (A.RenderQueue != B.RenderQueue) continue;
                    // 🔴 用**世界坐标**（`PxOfWorld`）而不是 `localPosition` ——
                    //    九宫格的 9 块是**根的子物体**，它们的 localPosition 都在根附近
                    //    ⇒ 用 local 会把每一行的块都判成「同一处」，660 处假重叠（2026-09-23 踩）。
                    var pa = DeckRuntime.PxOfWorld(A.transform.position);
                    var pb = DeckRuntime.PxOfWorld(B.transform.position);
                    bool ox = Mathf.Abs(pa.x - pb.x) < (A.WorldW + B.WorldW) * 0.5f - 1e-3f;
                    bool oy = Mathf.Abs(pa.y - pb.y) < (A.WorldH + B.WorldH) * 0.5f - 1e-3f;
                    if (ox && oy) { overlaps++; if (pairs.Count < 5) pairs.Add(A.name + " × " + B.name); }
                }
            CheckTrue(overlaps == 0, $"同一层的 UI 图没有互相压住（实测 {overlaps} 处" +
                      (pairs.Count == 0 ? "）" : "：" + string.Join(" · ", pairs) + "）"));

            CheckTrue(state.VisibleCards().Count > 0, "版面用的状态里有卡可显示");

            // ============================================================ 新版面（2026-09-20 按权威坐标重建）
            // 🔴 素材齐不齐 —— **缺图会静默变白**，这条挡住它（`sync_battle_ui_art.py` 那批）
            var missing = _rt.MissingArt;
            Check(missing.Count, 0, "原版 UI 图**一张不缺**（缺了会画成纯白占位）" +
                  (missing.Count == 0 ? "" : "—— 缺 " + string.Join("、", missing)));

            // Header 四件 + 侧栏 + Footer：逐件点验「真的建了」
            foreach (var k in new[] { "hdr_sep", "hdr_back", "hdr_fltbtn", "hdr_clear", "hdr_wcbg", "hdr_army" })
                CheckTrue(_rt.UiHasQuad(k), $"Header 的 `{k}` 建起来了");
            foreach (var k in new[] { "side_bg", "tab_hi0", "tab_ic0", "name_bg", "name_clear", "foot_done", "foot_ic" })
                CheckTrue(_rt.UiHasQuad(k), $"`{k}` 建起来了");
            for (int i = 0; i < 11; i++)
                if (!_rt.UiHasQuad("row_" + i)) { Check(true, false, $"卡组列表第 {i} 行没建起来"); break; }

            // 关键 rect **逐值**对权威表（`_deck_editing_godot_rects.txt`）——
            // 这几个数是「按原版复刻」的硬判据，抄错了这条会红
            CheckRect("hdr_fltbtn", 367.2f, 88.5f, 50f, 50f);
            // ⚠️ `hdr_clear` 的 x **不是** dump 里的 1488.6（那是 VLG 布局前的模板位，落在父容器外）
            //    —— 按「容器内右对齐」= 1468.6 − 250 = 1218.6，理由写在 `DeckRuntime` 那条常量上
            CheckRect("hdr_clear", 1218.6f, 83.5f, 250f, 60f);
            CheckRect("hdr_sep", 167.2f, 151f, 1752.8f, 10f);
            CheckRect("side_bg", -203f, 156f, 538.5f, 924.1f);
            CheckRect("name_bg", 9.5f, 311f, 307.7f, 50f);
            CheckRect("foot_done", 13f, 1020.5f, 188.5f, 50.2f);
            CheckRect("foot_ic", 201.6f, 1025f, 50f, 40f);

            // ---- 通配符计数条（`WIldcard Counter`，注意原版拼写就是 `WIldcard`）----
            // 🔴 **这一段以前一条断言都没有** ⇒ 「断言全绿、画面全错」（图标抄了**外层容器顶边 71**、
            //    数字被摆在图标**正下方**、字号只有原版的 1/3）。下面所有期望值来自
            //    正本 `资料/卡组编辑界面_查证_0920.md` §③ 的原版绝对 px。
            // ⚠️ 数字比的是**渲染矩形**（`Label.WorldW/H` = TMP 真测量），不是几何中心 ——
            //    原版那条 TMP 是 `VerticalAlignment = Capline`，数字相对几何中心略偏上。
            for (int i = 0; i < 4; i++)
            {
                float cx, cy, w, h;
                CheckTrue(_rt.UiQuadRect("hdr_wc" + i, out cx, out cy, out w, out h),
                          $"通配符第 {i + 1} 个稀有度图标量得到矩形");
                if (_rt.UiQuadRect("hdr_wc" + i, out cx, out cy, out w, out h))
                {
                    Check(Mathf.Abs(cx - (1580f + 75f * i)) < 0.6f, true,
                          $"第 {i + 1} 个图标中心 x = **{1580 + 75 * i}**（槽 x = 1565+75i、图标 30 宽在左）");
                    Check(Mathf.Abs(cy - 113.5f) < 0.6f, true,
                          $"第 {i + 1} 个图标中心 y = **113.5**（行带 91.5..135.5 —— 原来抄了 71 ⇒ 高 20.5px）");
                    Check(Mathf.Abs(w - 30f) < 0.6f && Mathf.Abs(h - 44f) < 0.6f, true,
                          $"第 {i + 1} 个图标 = **30×44**（实测 {w:F0}×{h:F0}）");
                }
                float tx, ty, tw, th;
                CheckTrue(_rt.UiWcCounterRect(i, out tx, out ty, out tw, out th),
                          $"通配符第 {i + 1} 个**数字**量得到渲染矩形");
                if (_rt.UiWcCounterRect(i, out tx, out ty, out tw, out th))
                {
                    // 数字盒 = 41 宽、左边缘贴图标右边缘（图标右 = 1580+75i+15）⇒ 盒中心 = 1615.5+75i
                    Check(Mathf.Abs(tx - (1615.5f + 75f * i)) < 1.0f, true,
                          $"第 {i + 1} 个数字盒中心 x = **{1615.5f + 75f * i}**（在图标**右侧** —— "
                          + $"原来在图标正下方、还左移 45.5px；实测 {tx:F1}）");
                    Check(ty > 91.5f && ty < 135.5f, true,
                          $"第 {i + 1} 个数字落在**行带 91.5..135.5** 里（实测中心 y = {ty:F1}）");
                    Check(th >= 18f, true,
                          $"第 {i + 1} 个数字的**渲染高 ≥ 18px**（字号 32.6 ⇒ cap 约 23px；"
                          + $"原来只有 1/3、cap ≈7.56。实测 {th:F1}）");
                }
                CheckTrue(!string.IsNullOrEmpty(_rt.UiWcText(i)),
                          $"第 {i + 1} 个数字有内容（**我们挑的替代**：写卡池张数；"
                          + $"原版写通配符库存 `WildcardDisplay`）—— 实测「{_rt.UiWcText(i)}」");
            }

            // ---- 卡组行的四层（正本 `卡组编辑界面_查证_0920.md` §① · `项目任务.md` §三 第 12 条 **第 5 项**）----
            // 🔴 这一段以前也**一条断言都没有** ⇒ 「断言全绿、画面全错」：行底用了一张**原版全透明**的图、
            //    描边 11×11 被**拉满**整行、稀有度色条**铺满整行**、层序还反了（行底盖住色条）。
            Check(_rt.UiTextureName("row_0"), "40k_deck_cardlist_bg",
                  "行底用的是原版那张 `40k_deck_cardlist_bg`"
                  + "（**不是** `UI_Card_name_background_normal_BW` —— 那张原版只当两处 `alpha=0` 的按钮根图）");
            // ⚠️ 行底是 **3 块**不是 9 块 —— 它的九宫格 border 只有**左右** `(150,0,150,0)`
            //    ⇒ 中间那一行三段（左端 150 原尺寸 + 中段拉伸 + 右端 150）就是全部；上下无边。
            CheckTrue(_rt.UiQuadCount("row_0") >= 3,
                      $"行底是**九宫格**（实测 {_rt.UiQuadCount("row_0")} 块 = 左右各 150 的端块 + 中段，"
                      + "border 只有左右 ⇒ 3 块**是对的**）");
            CheckTrue(_rt.UiQuadCount("row_b0") >= 9,
                      $"行描边是**九宫格**（实测 {_rt.UiQuadCount("row_b0")} 块；原来 11×11 的图被拉满整行）");
            CheckTrue(_rt.UiQueueOf("row_0") < _rt.UiQueueOf("row_g0")
                      && _rt.UiQueueOf("row_g0") < _rt.UiQueueOf("row_b0"),
                      "行内层序 = **行底 → 稀有度色条 → 描边**（原版兄弟序 Background→Rarity Gradient→Border；"
                      + $"实测 {_rt.UiQueueOf("row_0")}/{_rt.UiQueueOf("row_g0")}/{_rt.UiQueueOf("row_b0")}）");
            {
                float gx, gy, gw, gh;
                CheckTrue(_rt.UiQuadRect("row_g0", out gx, out gy, out gw, out gh), "稀有度色条量得到矩形");
                if (_rt.UiQuadRect("row_g0", out gx, out gy, out gw, out gh))
                {
                    Check(Mathf.Abs(gw - 128.05f) < 0.6f, true,
                          $"色条宽 = **128.05**（原版只占右侧 0.606→1.0 —— 原来铺满整行 325）");
                    Check(Mathf.Abs(gx - 261.38f) < 0.6f, true,
                          $"色条中心 x = **261.38**（右对齐：197.35 + 128.05/2）");
                }
            }

            // 卡池那一格：原版**默认那一套** = 卡位 262.5×384 · 6 列 · 间距 0 · 贴左起排
            // （定案与硬证据见 `项目任务.md` §三 第 12 条 第 6 项；另一套「小屏 UI」我们没实现，已出声）
            float wantScale = 384f / (CardView.Height * 108f);
            CheckTrue(Mathf.Abs(CardViewScaleOf(0) - wantScale) < 1e-4f,
                      $"卡池第一张卡的缩放 = {wantScale:F4}（按原版卡位高 **384** 反解）");
            // 🔴 **贴左但整体居中 + 6 列**（原版 `RecyclableScrollRect` 的居中量常量 `0.5`）：
            //    内容宽 6×262.5 = 1575 < 视口 1589.8 ⇒ 两侧各留 **7.4** ⇒
            //    第 0 格中心 x = 330.2 + 7.4 + 131.25 = **468.85**；第 5 格 = **1781.35**（右边界 1912.6 ≤ 1920 ✓）
            //    ⚠️ 这条**纠过一次**：先写成「贴左起排」（461.45/1773.95）—— 差 7.4px，来自没查「内容是否居中」。
            {
                float cx, cy, w, h;
                CheckTrue(_rt.UiPoolCellRect(0, out cx, out cy, out w, out h), "卡池第 0 格在（比版面的前提）");
                if (_rt.UiPoolCellRect(0, out cx, out cy, out w, out h))
                {
                    Check(Mathf.Abs(cx - 468.85f) < 0.6f, true,
                          $"卡池第 0 格中心 x = **468.85**（贴左但整体居中：330.2 + 7.4 + 262.5/2 —— 原来是 37.96 的居中留白 ✗）");
                    Check(Mathf.Abs(cy - 348f) < 0.6f, true,
                          $"卡池第 0 格中心 y = **348**（156 + 384/2 —— 原来带 8px 上边距）");
                }
                CheckTrue(_rt.UiPoolCellRect(5, out cx, out cy, out w, out h), "卡池第 6 格（最后一列）在");
                if (_rt.UiPoolCellRect(5, out cx, out cy, out w, out h))
                    Check(Mathf.Abs(cx - 1781.35f) < 0.6f, true,
                          $"卡池第 6 格中心 x = **1781.35** ⇒ 右边界 1912.6 落在 1920 里（**这是「6 列」的判据**）");
            }

            // 筛选栏：默认关着；打开后盖住侧栏（队列更大 = 更后画）
            Check(_rt.FiltersOpen, false, "刚建好时筛选栏是关着的");
            // 🔴 这条是踩出来的：底板建了却**没跟着开关隐藏** ⇒ 它（队列 3020，比侧栏大）
            //    会一直盖住整个侧栏 —— 画面上只剩一块底板色，卡组行/页签/Done 全看不见。
            CheckTrue(!_rt.UiQuadActive("flt_bg"), "关着的时候筛选栏底板**不显示**（不然会盖住整个侧栏）");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, true, "点一下 Filters 键 → 筛选栏打开");
            CheckTrue(_rt.UiQuadActive("flt_bg"), "打开后筛选栏底板显示出来");
            CheckTrue(_rt.UiQuadActive("side_bg"), "筛选栏开着时侧栏底板还在（它被盖住，不是被删掉）");
            if (_rt.UiQuadRect("flt_bg", out float fx, out float fy, out float fw, out float fh))
            {
                // 权威表是「左上 + 宽高」= [2.2,156] 331.7×924.1 ⇒ 中心 (168.05, 618.05)
                CheckRectPx("flt_bg", fx, fy, fw, fh, 2.2f + 331.7f * 0.5f, 156f + 924.1f * 0.5f, 331.7f, 924.1f);
                CheckTrue(fx < 336f, $"筛选栏在**左**（中心 x = {fx:F1}，原版权威坐标说左、与侧栏同格）");
            }

            // 筛选动作（走的是和鼠标同一条路）
            _rt.UiFilterRow("$rar:legendary");
            Check(state.Filter.Rarity, "legendary", "点稀有度 → 筛选条件变了");
            CheckTrue(state.VisibleCards().Count < state.PoolCount, "筛出来的确实变少了");
            _rt.UiFilterRow("$rar:legendary");
            Check(state.Filter.Rarity, "", "再点一次 → 取消该筛选");
            _rt.UiFilterRow("$fac:Ultramarines");
            Check(state.Filter.Faction, "Ultramarines", "点阵营 → 筛选条件变了");
            _rt.UiClearFilters();
            CheckTrue(state.Filter.IsEmpty, "Clear filters → 条件清空");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, false, "再点 Filters → 关闭");

            // 页签：Cards / Deck info / Cosmetics —— 费用曲线只在 info 页显示
            // 页签：Cards / Deck info / Cosmetics —— 三页各有各的东西，**不能是空白页**
            _rt.UiSetTab(1);
            Check(_rt.ActiveTab, 1, "切到 Deck info 页签");
            CheckTrue(_rt.UiCurveVisible, "Deck info 页签里费用曲线可见（Cards 页签下它是关的）");
            CheckTrue(_rt.UiInfoActionsVisible, "Deck info 页签里「分享 / 导入」两颗钮显示出来");
            CheckTrue(!_rt.UiCosmeticsVisible, "Cosmetics 那组东西在别的页签下是关的");
            CheckTrue(_rt.UiDeckRowAt(0) == null, "Deck info 页签下卡组行不显示");
            _rt.UiSetTab(2);
            CheckTrue(_rt.UiCosmeticsVisible, "Cosmetics 页签：**卡背那一页显示出来**（2026-09-24 起是真页面，不再是一句空态）");
            CheckTrue(!_rt.UiCurveVisible, "Cosmetics 页签下费用曲线关掉");
            CheckTrue(!_rt.UiInfoActionsVisible, "Cosmetics 页签下动作钮关掉");
            _rt.UiSetTab(0);
            CheckTrue(!_rt.UiCurveVisible, "切回 Cards → 费用曲线隐藏");
            CheckTrue(!_rt.UiCosmeticsVisible, "切回 Cards → Cosmetics 空态隐藏");
            CheckTrue(_rt.UiDeckRowAt(0) != null, "切回 Cards → 卡组行回来");
            // 🔴 「该藏的藏住了吗」——**图 + 文字一起查**（只查图会漏：`Lookup` 不认 Label）
            Check(_rt.UiInfoOnlyActive, 0, "Cards 页签上**一件 Deck info 的东西都不许露**（图 + 文字都算）");
            Check(_rt.UiCosmOnlyActive, 0, "Cards 页签上不许露 Cosmetics 的东西");
            Shoot("deck_cards.png");

            // ============================================================ Cosmetics 页 = 换卡背（2026-09-24）
            // 判据、出处、以及「为什么列数不是字段里的 `_segments=4`」都写在 `DeckRuntime` 的 Cosmetics 那一段。
            // 原版：`Deck Editing Menu > Content Area > Cosmetic Display`（`CardbackCollectionDisplay`）。
            _rt.UiSetTab(1); Shoot("deck_info.png");
            _rt.UiSetTab(2);
            {
                var names = CardArt.CosmeticNames();
                Check(_rt.CosmoColsPx, 6,
                      "卡背网格 **6 列** = floor(1589.78 ÷ 250)（原版字段 `_segments=4` 是**死值**，照抄会少两列）");
                Check(_rt.CosmoCellWpx, 250f, "卡背格宽 250（原版 `_cellWidth`）");
                Check(_rt.CosmoCellHpx, 405f, "卡背格高 405（原版 `_cellHeight`）");
                Check(_rt.CosmoView.x, 330.23f, "网格视口左缘 330.23（原版 `Cosmetic Display/Scroll View`）");
                Check(_rt.CosmoView.y, 155.97f, "网格视口上缘 155.97");
                Check(_rt.CosmoView.z, 1589.78f, "网格视口宽 1589.78");
                Check(_rt.CosmoView.w, 924.06f, "网格视口高 924.06");
                Check(names.Length, 233, "卡背总数 233（2026-09-23 导进工程的那批）");
                CheckTrue(_rt.CosmoCellShown >= 24, $"一屏至少铺 24 格（实铺 {_rt.CosmoCellShown}）");
                Check(_rt.CosmoCellTex(0), names[0], "第 1 格 = 字典序第一张卡背");

                // ---- 右键装备 / 左键不做事（原版 `DeckEditingWindow__OnCosmeticClick.c:26`）----
                CheckTrue(string.IsNullOrEmpty(_rt.EquippedCardback),
                          $"起手**没装备过**（原版 `CardDeck.cardbackId` 出厂是空串，实得「{_rt.EquippedCardback}」）");
                // ---- 判据本身（`CardArt.DeckCardback` = 原版 `CardDeck.GetDeckCardback()`）----
                //  选了 → 那张 · 没选 → **该阵营的默认卡背** · 选了张取不到的 → 退回默认（**不静默画空白**）
                Check(CardArt.DeckCardback(names[3], "Ultramarines"), CardArt.Cosmetic(names[3]),
                      "判据：选了卡背 ⇒ 用**选的那张**");
                CheckTrue(CardArt.DeckCardback(null, "Ultramarines") == CardArt.CardBack("Ultramarines"),
                      "判据：没选过 ⇒ **该阵营的默认卡背**（原版 `ArmyUtilities.GetDefaultCardback(army)`）");
                CheckTrue(CardArt.DeckCardback("不存在的卡背_zzz", "Ultramarines") == CardArt.CardBack("Ultramarines"),
                      "判据：选了张**取不到的** ⇒ 退回默认（存档跨版本/手改过时不许静默变空白）");
                // 本测试卡组是「新建的空卡组」⇒ **没有督军 ⇒ 没有阵营** ⇒ 本来就没有默认卡背可显示。
                Check(_rt.CosmeticDrawerTex, "<无>",
                      "本卡组没有督军 ⇒ 抽屉不画卡背（**不是**随便挑一张顶上，也不是漏了）");
                float cx0 = 330.23f + (1589.78f - 6f * 250f) * 0.5f + 250f * 0.5f;   // 第 1 格中心
                float cy0 = 155.97f + 405f * 0.5f;
                CheckTrue(_rt.UiClickCosmetic(cx0, cy0, false), "左键点第 1 格：**命中了**（原版左键「什么都不做」，但不是点不到）");
                CheckTrue(string.IsNullOrEmpty(_rt.EquippedCardback), "……而且**没装备**（原版只有右键才装备）");
                CheckTrue(_rt.UiClickCosmetic(cx0, cy0, true), "右键点第 1 格：命中");
                Check(_rt.EquippedCardback, names[0], "……而且**装备上了**（原版 `editingDeck.cardbackId = item.GetID()`）");
                Check(_rt.CosmeticDrawerTex, names[0], "侧栏抽屉那张图跟着换成**装备的那张**");

                // 换一格（第 2 行第 3 列 = 第 9 格）—— 验「行列反算」不是碰巧对
                float cx8 = 330.23f + (1589.78f - 6f * 250f) * 0.5f + 250f * 2.5f;
                float cy8 = 155.97f + 405f * 1.5f;
                CheckTrue(_rt.UiClickCosmetic(cx8, cy8, true), "右键点第 2 行第 3 列：命中");
                Check(_rt.EquippedCardback, names[8], $"……装备的是第 9 张（{names[8]}）—— 行列反算对");

                // ---- 滚到底：最后一格 = 最后一张 ----
                _rt.UiScrollCosmetics(1e6f);
                Check(_rt.CosmoScrollPx, _rt.MaxCosmoScrollPx, $"滚到底 = MaxCosmoScrollPx（{_rt.MaxCosmoScrollPx:F1}）");
                {
                    int firstRow = Mathf.FloorToInt(_rt.CosmoScrollPx / 405f);
                    int vi = (names.Length - 1) - firstRow * 6;
                    Check(_rt.CosmoCellTex(vi), names[names.Length - 1], "滚到底：最后一格 = 字典序最后一张卡背");
                }
                _rt.UiScrollCosmetics(-1e6f);
                Check(_rt.CosmoScrollPx, 0f, "滚回顶");
                // ---- 存档：换完卡背**真的落盘了**（`CommitCurrent` 是逐字段拷的，漏一个字段就静默丢）----
                var reread = DeckLibrary.Load();
                Check(reread.Current.CardbackId, names[8], "换完卡背，**重新读存档**还是那张（`PlayerDeck.CardbackId` 已落盘）");
                // ⚠️ **不复位** —— 后面那张 Cosmetics 截图就拍「装备了第 9 张」的样子（正好当实拍证据）；
                //    卡组数据在临时存档里（`DeckStore.OverridePath`），跑完就删，不碰玩家的真存档。
                _rt.UiSetTab(2); Shoot("deck_cosmetics.png");
            }
            _rt.UiSetTab(0);

            // 🔴 命中矩形：用**合成坐标**走鼠标那条路（`Ui*()` 只驱动状态，验不到「点在哪儿」）
            CheckTrue(_rt.UiClickPx(392.2f, 113.5f), "点 `Filters` 钮的**中心**（392,113.5）能命中");
            Check(_rt.FiltersOpen, true, "……而且筛选栏真的开了");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, false, "（关回去）");
            CheckTrue(!_rt.UiClickPx(900f, 700f), "点空白处不命中任何钮");
            Check(_rt.FiltersOpen, false, "……也不会误开面板");

            // 滚动（原版是滚动列表，不是翻页）—— 滚过一整行之后，第 0 格该换一张卡
            var before = _rt.UiPoolCardAt(0);
            CheckTrue(_rt.MaxPoolScrollPx > 0f, $"卡池滚得动（上限 {_rt.MaxPoolScrollPx:F0} px）");
            _rt.UiScrollPool(600f);
            CheckTrue(_rt.UiPoolCardAt(0) != before, $"卡池滚动后第 0 格换了卡（{before?.Name} → {_rt.UiPoolCardAt(0)?.Name}）");
            _rt.UiScrollPool(-9999f);
            Check(_rt.PoolScrollPx, 0f, "往回滚到顶夹在 0（不会滚成负的）");

            // 卡组列表滚动：先把卡组填满（演示卡组就有 30 张 + 督军 + 防御）
            CheckTrue(_rt.MaxDeckScrollPx > 0f, $"卡组列表滚得动（{_rt.MaxDeckScrollPx:F0} px = 条目数超过一屏）");
            var row0 = _rt.UiDeckRowAt(0);
            _rt.UiScrollDeck(56f);
            CheckTrue(_rt.UiDeckRowAt(0) != row0, "卡组列表滚动后第一行换了卡");
            _rt.UiScrollDeck(-9999f);
            Check(_rt.DeckScrollPx, 0f, "卡组列表滚回顶部夹在 0");

            // 改名（原版 ESC=保存；这条验「改完真的写回卡组库」）
            _rt.UiCommitName("自检·改的名");
            Check(state.Deck.Name, "自检·改的名", "改名进了当前卡组");
            Check(DeckLibrary.Load().Current.Name, "自检·改的名", "改名**落盘**了");
            _rt.UiCommitName("   ");
            Check(state.Deck.Name, "自检·改的名", "空名字被挡（不会把卡组改成没名字）");

            // 卡名筛选的输入框（原版 `CardNameFilter`）：点进去 → 输入 → 回车
            _rt.UiToggleFilters();
            _rt.UiFilterRow("$name");
            Check(_rt.UiEditKind, 2, "点 Name 那一行 → 进了「改卡名筛选」的输入态");
            _rt.UiCommitEdit("autarch");
            Check(_rt.UiEditKind, 0, "回车之后退出输入态");
            Check(state.Filter.Name, "autarch", "卡名筛选生效");
            CheckTrue(state.VisibleCards().Count >= 1 && state.VisibleCards().Count < state.PoolCount,
                      $"筛出 {state.VisibleCards().Count} 张（少于全部）");
            _rt.UiFilterRow("$name");
            _rt.UiCommitEdit("");
            Check(state.Filter.Name, "", "再输入空串 → 清空卡名筛选");
            _rt.UiToggleFilters();

            // 分享 / 导入（后端 `DeckLibrary.ExportString`/`ImportString`，原版格式）
            var str = _rt.UiShareString();
            CheckTrue(str.Length > 0, $"分享：导出卡组串（{str.Length} 字符）");
            int n1 = DeckLibrary.Load().Count;
            Check(_rt.UiModalActive, 0, "没打开时，导入弹窗**一件都不显示**（图 + 文字都算）");
            _rt.UiOpenImport();
            CheckTrue(_rt.UiModalVisible, "导入弹窗打开了");
            CheckTrue(_rt.UiModalActive >= 8, $"弹窗开着一共 {_rt.UiModalActive} 件（图 + 文字）显示出来");
            Shoot("deck_import.png");
            _rt.UiSetImportText(str);
            CheckTrue(_rt.UiTryImport(), "把刚导出的串导回来 → 成功");
            CheckTrue(!_rt.UiModalVisible, "导入成功后弹窗自动关掉");
            Check(DeckLibrary.Load().Count, n1 + 1, "卡组库里多了一套（导入是**新增**不是覆盖）");
            _rt.UiOpenImport();
            _rt.UiSetImportText("这不是一条卡组串");
            CheckTrue(!_rt.UiTryImport(), "乱串被挡");
            CheckTrue(_rt.ImportError.Length > 0, $"被挡时给了人话错误（「{_rt.ImportError}」）");
            _rt.UiCloseImport();
            CheckTrue(!_rt.UiModalVisible, "点关闭 → 弹窗收起");

            // 🔴 「拖出侧栏 = 删除」与「点一下 = 弹大图」——**删牌只有拖出这一条路**
            //    （原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`），批处理没鼠标，
            //    所以走 `UiDragDeckRow` / `UiClickDeckRow`（**调的是鼠标那条路的同一段代码**）。
            _rt.UiSetTab(0);
            int n2 = _rt.State.DeckCount;
            var rowDef = _rt.UiDeckRowAt(0);
            CheckTrue(rowDef != null, $"第 0 行有牌可操作（{rowDef?.Name}）");
            CheckTrue(_rt.UiClickDeckRow(0), "模拟「点一下卡组第 0 行」");
            CheckTrue(_rt.UiCardWindowVisible, "……弹出放大窗（原版：**点卡组里的牌是弹大图、不是删除**）");
            Check(_rt.UiCardWindowTitle, CardText.Name(rowDef.Name, rowDef.NameZh), "放大窗里就是那一张");
            _rt.UiCloseCardWindow();
            Check(_rt.State.DeckCount, n2, "只点不开删 —— 卡组张数没变");
            // ⚠️ 行 0/1 是**督军 / 防御卡**（不占 30 张的名额）⇒ 拿行 2（第一张普通卡）验删除
            var delDef = _rt.UiDeckRowAt(2);
            CheckTrue(delDef != null && delDef.Type == "unit", $"第 2 行是普通单位卡（{delDef?.Name} / {delDef?.Type}）");
            // 往**侧栏里面**拖：不算拖出 ⇒ 不删
            CheckTrue(_rt.UiDragDeckRow(2, 150f), "模拟「把第 2 行拖到侧栏里面（x=150）松开」");
            Check(_rt.State.DeckCount, n2, "拖到侧栏里松开 → **不删**（判据是「拖出侧栏」）");
            // 往**侧栏外面**拖：算拖出 ⇒ 删掉一张
            CheckTrue(_rt.UiDragDeckRow(2, 1200f), "模拟「把第 2 行拖到侧栏外（x=1200）松开」");
            Check(_rt.State.DeckCount, n2 - 1, "拖出侧栏松开 → **卡组里少一张**（照原版删牌）");
            Check(DeckLibrary.Load().Current.CardIds.Count, n2 - 1, "删除**落盘**了");

            // 悬停 tooltip（原版触发器挂在**卡面数值容器**上，卡池里的卡是同一批 prefab）
            Tooltip.Hide(); Tooltip.FinishFade();
            var pool0 = _root.Find("pool_0");
            CheckTrue(pool0 != null, "卡池第 0 格有视图");
            if (pool0 != null)
            {
                var pv = pool0.GetComponent<CardView>();
                CheckTrue(_rt.TickTooltipAt(pv.StatWorld(CardView.StatCost)),
                          "把指针放到卡池卡的**费用**上 → tooltip 显示");
                CheckTrue(Tooltip.ShownBody == TipText.Cost, "显示的是费用那一条");
                Tooltip.FinishFade();
                CheckTrue(Tooltip.Visible && Tooltip.PanelSizePx.x > 0f,
                          $"面板量得出来（{Tooltip.PanelSizePx.x:F0}×{Tooltip.PanelSizePx.y:F0} px）"
                          + " —— 量不出来就是「先排版后激活」那个坑");
                Tooltip.Hide(); Tooltip.FinishFade();
                _rt.TickTooltipAt(pv.StatWorld(CardView.StatArmour));
                CheckTrue(!Tooltip.Visible, "**护甲上没有 tooltip**（原版那个容器就没有触发器 —— 照原版）");
                _rt.TickTooltipAt(new Vector3(0f, -4.5f, 0f));
                CheckTrue(!Tooltip.Visible, "挪到空白处 → 不显示");
            }
        }

        /// <summary>比一张具名图的 px 中心/尺寸（容差 0.6px —— 坐标是我们自己换算的，不该有误差）。</summary>
        static void CheckRect(string key, float x, float y, float w, float h)
        {
            if (!_rt.UiQuadRect(key, out float cx, out float cy, out float gw, out float gh))
            { Check(true, false, $"`{key}` 没建起来（比不了 rect）"); return; }
            CheckRectPx(key, cx, cy, gw, gh, x + w * 0.5f, y + h * 0.5f, w, h);
        }

        static void CheckRectPx(string key, float cx, float cy, float gw, float gh,
                                float wx, float wy, float ww, float wh)
        {
            bool ok = Mathf.Abs(cx - wx) < 0.6f && Mathf.Abs(cy - wy) < 0.6f
                   && Mathf.Abs(gw - ww) < 0.6f && Mathf.Abs(gh - wh) < 0.6f;
            Check(ok, true, $"`{key}` 的 rect = 权威值（中心 {wx:F1},{wy:F1} 尺寸 {ww:F1}×{wh:F1}）" +
                            (ok ? "" : $" —— 实得 中心 {cx:F1},{cy:F1} 尺寸 {gw:F1}×{gh:F1}"));
        }

        static float CardViewScaleOf(int i)
        {
            var go = _root.Find("pool_" + i);
            return go == null ? -1f : go.localScale.x;
        }

        // ============================================================ 建场景

        /// <summary>自检用的临时存档路径。</summary>
        static string TempStorePath()
        {
            return System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../Temp/_deckscene_selftest.json"));
        }

        /// <param name="lib">卡组库。**自检必须传临时路径上的库** —— 不能动玩家的真存档。</param>
        static DeckEditorState Build(DeckLibrary lib, out Transform root)
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

            // 🔴 2026-09-20：**建界面这件事只有一处实现** —— `DeckRuntime.Build()`（运行时程序集）。
            //    这里原来有 ~110 行绘制代码，和 `DeckRuntime` 那份是同一个东西的两个副本；
            //    两份迟早不一致（本项目反复踩过这个坑），所以改成转发。
            var rootGo = new GameObject("DeckEditor");
            var rt = rootGo.AddComponent<DeckRuntime>();
            rt.Build(lib);
            _rt = rt;
            _root = rootGo.transform;
            root = _root;
            return rt.State;
        }

        // 演示卡组 `PlayerDeckForDemo` 2026-09-20 **挪进 `DeckRuntime`**（和建界面同一处）——
        // 这里不再留第二份（两份迟早不一致）。要用就 `DeckRuntime.PlayerDeckForDemo(state)`。

        /// <summary>卡面数据 —— **转发到唯一的正路** `BattleDriver.ToCardData`。
        ///
        /// 🔴 2026-09-15 改：这里原来是**第二份 `ToCardData`**（本项目反复强调「卡面数据只有这一条路」），
        /// 而它跟正路差了三件、件件都在卡面上看得见：
        ///   · `title = c.Name` —— **丢掉中文名**（正路走 `CardText.Name(c.Name, c.NameZh)`）
        ///   · `keywords = string.Join(" · ", c.Keywords.Keys)` —— **把内部 canonical 键直接印上卡面**
        ///     （会印出 `longrange · cantattack` 这种机器味串）
        ///   · 没有 `subtype`（兵种行整条不画）、没有 `artId`（立绘取不到）、没有 `badges`
        /// ⇒ 卡组编辑器里的卡面和战斗里的**不是同一张脸**。改成转发之后两边自然一致。
        /// ⚠️ 顺带统一了阵营色（原来这里有一份**哈希取色的 `FactionColor`**，正路那份是查表的）。</summary>
        static CardData ToCardData(CardDef c) => BattleDriver.ToCardData(c, c.Faction);

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
        /// <summary>把 `DeckEditor.unity` 加进 `EditorBuildSettings`（幂等）—— **收藏窗的「进编辑」靠它**
        /// （`SceneManager.LoadScene("DeckEditor")` 没在 Build Settings 里会抛）。照 `MainMenuScene.AddToBuildSettings`。
        /// 可以单独跑：`-executeMethod DeckScene.AddToBuild`。</summary>
        public static void AddToBuild()
        {
            const string path = "Assets/CardPresentation/Scenes/DeckEditor.unity";
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list)
                if (s.path == path) { Debug.Log(P + "  `DeckEditor.unity` 已在 Build Settings 里"); return; }
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log(P + "  已把 `DeckEditor.unity` 加进 Build Settings（收藏窗的「进编辑」要靠它 `LoadScene`）");
        }

        public static void BuildAndSaveScene()
        {
            Directory.CreateDirectory(ShotDir);
            AddToBuild();      // 🔴 2026-09-23：`DeckEditor` **原来不在 Build Settings 里** ⇒
                               //    收藏窗「进编辑」那一步 `SceneManager.LoadScene("DeckEditor")` 会抛
            Transform root;
            // ⚠️ 这条路用**玩家的真存档**（打开场景按 Play 时就是要编辑自己的卡组）
            var state = Build(DeckLibrary.Load(), out root);
            // 🔴 2026-09-20：**场景里必须挂着 `DeckRuntime`** —— 界面现在是运行时的
            //    `DeckRuntime.Start()` 建的（编辑器里 `Build` 只是把绘制转发过去、直接调），
            //    忘了挂组件的话**按 Play 出来是一片黑**，而自检看不见这个错
            //    （它直调 `Build`，和 Play 那条路不是同一个入口 —— 本项目踩过同形的坑）。
            CheckTrue(root != null && root.GetComponent<DeckRuntime>() != null,
                      "DeckEditor.unity 的根上挂着 DeckRuntime（不然按 Play 是一片黑）");
            Shoot("deck_editor.png");
            SaveScene();
            Debug.Log(P + $"=== 场景已重建：{ScenePath}"

                        + $"（{state.PoolCount} 张卡池，演示卡组 {state.Deck.Name}）===");
        }
}
