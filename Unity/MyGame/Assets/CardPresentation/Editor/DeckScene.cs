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

        /// <summary>数值比较（±`tol`）—— 用来比**原版参数**那类量（颜色系数、像素尺寸）。</summary>
        static void CheckNear(float got, float want, float tol, string msg)
            => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（实测 {got:F4} ≈ 期望 {want:F4} ± {tol:F4}）");

        // ============================================================ 🆕 2026-10-04（A24）悬停 / 状态换图的断言
        //
        // 判据 = **原 prefab 的组件字段**（`m_Transition` / `m_SpriteState` / `EverguildToggle.onSprite|offSprite`），
        // 普查正本 = `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md`；
        // 本轮又把 5 颗的 `m_TargetGraphic` + 三张图名、7 颗的状态图名**逐字段复读了一遍**。

        /// <summary>把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
        /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
        static void CheckHoverSwap(Transform root, string what)
        {
            int n; string bad = WindowButton.AuditHoverSwap(root, out n);
            CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
            if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
        }

        /// <summary>**取不到的悬停图**一张都不许有（红线：不许静默画成没反应）。</summary>
        static void CheckNoMissingSwapArt(string what)
            => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                         what + "：**悬停图一张都不缺**（缺的会列在这里："
                         + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");

        static string TexName(Texture t) { return t != null ? t.name : "<无>"; }

        /// <summary>**逐颗**验一颗按钮的悬停换图（判据 = 原版那颗 `m_SpriteState` 的三张图名）。
        /// 三步都要过：① 组件层面三张图对得上 ② 直调 `Enter/Exit` 换得动 + 还原得回 + **不改矩形**
        /// （`ImageQuad.SetTexture` 会把宽高比冲成贴图自己的 —— A17 买过的教训）
        /// ③ **走鼠标那条路**（`UiHoverAt`，就是 `HandlePointer` 每帧调的那条）也能打到它。</summary>
        static void CheckHoverOne(string key, string node, string normal, string hover, string pressed, string what)
        {
            var t = _root != null ? _root.Find(node) : null;
            var wb = t != null ? t.GetComponent<WindowButton>() : null;
            if (wb == null)
            {
                CheckTrue(false, $"{what}：`{node}` 上**接了 `WindowButton`**（不然悬停时屏幕上什么都不发生）");
                return;
            }
            CheckTrue(wb.NormalTexForTest != null && wb.NormalTexForTest.name == normal,
                      $"{what}：常态图 = 原版 `{normal}`（实得 `{TexName(wb.NormalTexForTest)}`）");
            CheckTrue(wb.HoverTexForTest != null && wb.HoverTexForTest.name == hover,
                      $"{what}：高亮图 = 原版 `{hover}`（实得 `{TexName(wb.HoverTexForTest)}`）");
            CheckTrue(wb.PressedTexForTest != null && wb.PressedTexForTest.name == pressed,
                      $"{what}：按下图 = 原版 `{pressed}`（实得 `{TexName(wb.PressedTexForTest)}`）");
            var q = wb.target;
            if (q == null) { CheckTrue(false, $"{what}：换图落在**看得见的那一层**上（`target` 是空的）"); return; }
            float w0 = q.WorldW, h0 = q.WorldH;
            wb.Enter();
            CheckTrue(wb.CurrentTexForTest == wb.HoverTexForTest,
                      $"{what}：**悬停换得动**（实得 `{TexName(wb.CurrentTexForTest)}`）");
            wb.Exit();
            CheckTrue(wb.CurrentTexForTest == wb.NormalTexForTest,
                      $"{what}：**离开还原得回**（实得 `{TexName(wb.CurrentTexForTest)}`）");
            CheckTrue(Mathf.Abs(q.WorldW - w0) < 1e-4f && Mathf.Abs(q.WorldH - h0) < 1e-4f,
                      $"{what}：换图**不改矩形**（`SetTexture` 会冲掉宽高比 ⇒ 必须 `SetAspect` 拉回；实测 "
                      + $"{w0 * 108f:F1}×{h0 * 108f:F1} → {q.WorldW * 108f:F1}×{q.WorldH * 108f:F1} px）");
            // ③ 派发：走**鼠标那条路**（`UiHoverAt` = `HandlePointer` 里那条），命中口径 = 点击同一条
            if (_rt.UiBtnRect(key, out float bx, out float by, out float bw, out float bh))
            {
                CheckTrue(_rt.UiHoverAt(bx + bw * 0.5f, by + bh * 0.5f) == wb,
                          $"{what}：**走鼠标那条路**（`UiHoverAt`）打到的就是它（悬停派发接线通）");
                CheckTrue(wb.CurrentTexForTest == wb.HoverTexForTest,
                          $"{what}：派发过来的悬停也换成了 HL 图");
                _rt.UiHoverAt(1700f, 500f);                      // 移开（卡池那边，没有按钮）
                CheckTrue(wb.CurrentTexForTest == wb.NormalTexForTest,
                          $"{what}：移开 ⇒ 判据还原（`UiHoverAt` 那条路）");
            }
            else Check(true, false, $"{what}：`{key}` 在 `_btns` 里量不到（悬停派发比不了）");
        }

        // ============================================================ 自检

        public static void Run()
        {
            _pass = 0; _fail = 0; _failures.Clear();
            Directory.CreateDirectory(ShotDir);
            Debug.Log(P + "=== 卡组编辑自检 开始 ===");

            Section("状态：卡池与筛选");
            TestFilters();
            TestWarlordGatedPool();

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
                        // 🔴 **2026-09-28 改判据：脚注只数【卡组里那 N 张】，不再把督军/防御卡算进去。**
                        //   判据 = 原版实拍（`卡组编辑界面参考.png`）右下角印 **`30/30`**，而那一屏的卡表里
                        //   **只有卡组的 30 张**（督军在标题行、防御卡在自己的格子里）⇒ 原来那个
                        //   `(卡数 + 督军 + 防御卡) / (MaxDeckCount + 2)` 会印成 **32/32**，**多 2**。
                        //   出处：`资料/历史/五张参考图_逐件核对_0922.md` 第 10 条（挂着「要核」，已核完）。
                        //   ⇒ 遭遇模式这副满牌 = **12/12**（经典满编会画 **30/30**）—— **分母仍是模式的证据**。
                        Check(txt3, "12/12",
                              "★ 界面按遭遇画：**12/12** = 卡组那 12 张 / 遭遇上限 12"
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

        /// <summary>🆕 **2026-09-27（用户给的规格）**：卡组编辑的卡池**按督军分流** ——
        /// 「卡组没有督军的时候，右边卡库显示的是各个阵营的督军，放入督军后，右边卡库显示该阵营的卡牌」。
        /// ⚠️ **只有卡组编辑开这个开关**（`WarlordGatedPool`）；收藏窗与上面那些纯逻辑用例都走 false。</summary>
        static void TestWarlordGatedPool()
        {
            var s = new DeckEditorState(CardDatabase.Load());
            s.WarlordGatedPool = true;

            var noWl = s.VisibleCards();
            CheckTrue(noWl.Count > 0, $"**没督军**时卡池里有东西（{noWl.Count} 个）");
            var facs = new System.Collections.Generic.HashSet<string>();
            bool allHero = true;
            foreach (var c in noWl)
            {
                if (c.Type != "hero")
                { Check(true, false, $"没督军时卡池里混进了非督军：`{c.Name}`（{c.Type}）"); allHero = false; break; }
                facs.Add(c.Faction ?? "");
            }
            if (allHero) CheckTrue(true, $"没督军时**全是督军**（{noWl.Count} 张）");
            CheckTrue(facs.Count >= 10, $"**各阵营的督军都在**（覆盖 {facs.Count} 个阵营 —— 不是只列某一个）");

            CardDef w = noWl[0];
            s.SetWarlord(w.Id);
            var seen = s.VisibleCards();
            CheckTrue(seen.Count > 0, $"定了督军（`{w.Name}` / {w.Faction}）后卡池里有东西（{seen.Count} 个）");
            bool hasHero = false, hasDef = false, okFac = true;
            foreach (var c in seen)
            {
                if (!DeckRules.SameFaction(c.Faction, w.Faction))
                { Check(true, false, $"定了督军后混进了别的阵营：`{c.Name}`（{c.Faction} ≠ {w.Faction}）"); okFac = false; break; }
                if (c.Type == "hero") hasHero = true;
                if (c.Type == "defence") hasDef = true;
            }
            if (okFac) CheckTrue(true, $"定了督军后卡池**只剩该阵营**（{seen.Count} 张）");
            CheckTrue(hasHero, "该阵营**其它督军还在**（玩家可以换督军）");
            CheckTrue(hasDef, "该阵营的**防御卡在卡池里**（玩家要从这儿挑防御卡 —— 用户 2026-09-27）");

            // 🆕 **2026-09-27（用户拍板）：效果生成的卡（药剂/破坏/秘仪）不进卡池**
            //   帝王之子有 6 张药剂、暗黑天使有 5 张秘仪 —— 选这两家的督军，池子里一张都不该有。
            foreach (var probe in new[] { "EmperorsChildren", "DarkAngels" })
            {
                CardDef hw = null;
                var ph = s.Pool;
                for (int i = 0; i < ph.Count; i++)
                    if (ph[i].Type == "hero" && ph[i].Faction == probe) { hw = ph[i]; break; }
                if (hw == null) { Check(true, false, $"卡池里找不到 {probe} 的督军"); continue; }
                s.SetWarlord(hw.Id);
                int eff = 0; string firstEff = "";
                foreach (var c in s.VisibleCards())
                    if (DeckRules.IsEffectOnly(c.Subtype)) { eff++; if (firstEff.Length == 0) firstEff = c.Name; }
                Check(eff, 0, $"★ {probe} 的卡池里**没有效果生成的卡**（药剂/破坏/秘仪）"
                            + $"—— 现在有 {eff} 张" + (eff > 0 ? $"（如 `{firstEff}`）" : ""));
            }
            // 而且**加不进去**（负例：绕开卡池直接 TryAdd 一张药剂）
            {
                CardDef elix = null;
                var pe = s.Pool;
                for (int i = 0; i < pe.Count; i++) if (pe[i].Subtype == "Combat Elixir") { elix = pe[i]; break; }
                if (elix == null) Check(true, false, "卡池里找不到药剂卡（这一条没法验）");
                else
                {
                    string why;
                    bool added = s.TryAdd(elix, out why);
                    Check(added, false, $"★ 直接 `TryAdd` 一张药剂 `{elix.Name}` ⇒ **被拒**（{why}）"
                                      + " —— 用户 2026-09-27：药剂/破坏卡不能加入卡组");
                }
            }

            s.ClearWarlord();
            Check(s.VisibleCards().Count, noWl.Count, "清掉督军 ⇒ 卡池回到「只列各阵营督军」");

            // 🆕 **督军按阵营分组**（用户 2026-09-27 问「总不能混乱地排吧」）——
            //   判据：**同一个阵营的督军必须连成一段**，不许 A→B→A 这种交错。
            {
                var list = s.VisibleCards();
                int breaks = 0; string last = null;
                var closed = new System.Collections.Generic.HashSet<string>();
                foreach (var c in list)
                {
                    var f = c.Faction ?? "";
                    if (f != last) { if (!closed.Add(f)) breaks++; last = f; }
                }
                Check(breaks, 0, $"★ 督军**按阵营分组**、没有交错（重复出现的阵营段 = {breaks}；共 {list.Count} 位督军）");
            }
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
                // ⚠️ **卡池那一列是可滚动内容** ⇒ 排除掉：第 3 行只露头，它底下那条张数（`poolbar_*`）
                //    本来就在屏幕外（滚上来才看得到）。卡池的**卡**是 `CardView`（不是 `ImageQuad`），
                //    这条从来没扫到过它们；2026-09-28 新加的张数条是 `ImageQuad` ⇒ 不排除就会一直误报
                //    （实测第一处 `poolbar_12` = 第 3 行那张）。
                if (q.name != null && q.name.StartsWith("poolbar_")) continue;
                var p = q.transform.localPosition;
                if (Mathf.Abs(p.x) > halfW + 0.01f || Mathf.Abs(p.y) > halfH + 0.01f)
                { off++; if (firstOff == null) firstOff = q.name; }
            }
            Check(off, 0, "所有可见 UI 图都落在可见区内（没有跑到屏幕外）" +
                  (firstOff == null ? "" : "—— 第一处 " + firstOff));

            // ⚠️ 版面回归用的通用检查：**同一层的两个图不许压在一起**。
            //    分层 = `SetRenderQueue`（**不是 z** —— 透明队列按到相机的 3D 距离排序，
            //    铺满屏的图会互相盖错，2026-09-20 实测：侧栏底板盖住了整个卡组列表）。
            // 🔴🔴 **2026-10-04（A41 ③）：这条判据以前【单位混了 ⇒ 恒绿】** ——
            //    左边 `DeckRuntime.PxOfWorld(...)` 给的是 **px**，右边 `A.WorldW/B.WorldW` 是**世界单位**
            //    （1 世界单位 = 108px）⇒ 阈值实际只有 ~1–3px ⇒ **正常尺寸的重叠一个都抓不到**。
            //    修法 = 右边一律 `* PxPerUnit`；容差同时从 `1e-3` 放大到 `LayTol`（px）——
            //    本批新加宽的页签格**边缘恰好相接**（余量本来就只有 1e-3 px 那一档），太紧会误报。
            const float LayTol = 0.05f;      // px：小于它的「重叠」不报（浮点噪声），真重叠照报
            int overlaps = 0; var pairs = new List<string>();
            int sameQueue = 0;
            float tightest = float.MaxValue; string tightestPair = null;
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
                    float aw = A.WorldW * PxPerUnit, ah = A.WorldH * PxPerUnit;
                    float bw = B.WorldW * PxPerUnit, bh = B.WorldH * PxPerUnit;
                    float gx = Mathf.Abs(pa.x - pb.x) - (aw + bw) * 0.5f;   // > 0 = 两轴还要再靠近这么多才碰上
                    float gy = Mathf.Abs(pa.y - pb.y) - (ah + bh) * 0.5f;
                    sameQueue++;
                    float gap = Mathf.Max(gx, gy);                          // < 0 ⇒ 真的重叠了这么多 px
                    if (gap < tightest) { tightest = gap; tightestPair = A.name + " × " + B.name; }
                    if (gx < -LayTol && gy < -LayTol)
                    { overlaps++; if (pairs.Count < 5) pairs.Add(A.name + " × " + B.name); }
                }
            // （前提）真的比了足够多对 —— 防「循环静默地一对都没比」
            CheckTrue(sameQueue >= 100, $"同层组合真的比了 {sameQueue} 对（少于此数 ⇒ 这条检查其实没在跑）");
            CheckTrue(overlaps == 0, $"同一层的 UI 图没有互相压住（实测 {overlaps} 处" +
                      (pairs.Count == 0 ? "）" : "：" + string.Join(" · ", pairs) + "）"));
            // 🔴 这条是**上面那条判据的尺子**（A41 ③ 的「改坏会红」）：同层挨得最近的一对是
            //    **相邻两个页签格的边缘**，原版把它们做成**恰好相接**（`[0.3,109.2][109.2,218.2]`）
            //    ⇒ px 间隙 = **0**。若右边漏乘 `PxPerUnit`（老写法），这个数会变成 ~109 —— **这里会红**。
            //    期望值 0 出自**原版 rect 相接**，不是从我们的实现里读回来的。
            CheckNear(tightest, 0f, LayTol,
                      $"同层最紧的一对（{tightestPair}）**恰好相接**、px 间隙 = 0"
                      + "（判据 = 原版相邻页签格边缘相接；这条同时是「判据的两个量都是 px」的校准）");

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
            // 🔴 **2026-10-04（A51 F10）：页头分隔线原版也是【九宫格】，我们原来是单块拉伸。**
            //   判据 = dump 那行 `Separator Line [167.2,151]–[1920,161] 1752.83×10 | 40k_main_line 171×6
            //   九宫80,0,80,0 | Sliced ppuMul=0.75`（`Sprite/40k_main_line.json` 同值）。
            //   端帽 = `m_Border 80 ÷ ppuMul 0.75` = **106.6667px**；上下 `m_Border` 都是 **0**
            //   ⇒ 只有**中间那一行** ⇒ 一棵树 **3 块**（不是 9 块）。
            //   ⚠️ 拉伸的代价：1752.8 ÷ 171 = **10.25 倍**横向拉伸 ⇒ 两端 ~80px 的端帽被拉成 ~146px。
            Check(_rt.UiQuadCount("hdr_sep"), 3,
                  "页头分隔线是**九宫格**（原版 `m_Type=Sliced`；上下 border 是 0 ⇒ 只有中间一行、共 3 块）");
            Check(_rt.UiTextureName("hdr_sep"), "40k_main_line", "……图还是那张 `40k_main_line`");
            // 🔴 **2026-10-05（A57 ①）**：`hdr_sep` 是**直接挂在 `Root` 下的九宫格**（`NineSlice` 建，
            //    **不进 `_named`**）⇒ 它正是 `UiQuadActive` 缺兜底时**必答 false** 的那一类、而且**常显**。
            //    期望值 `true` 的出处 = 页头分隔线在卡组编辑界面上**一直画着**（原版 `Separator Line`
            //    `[167.2,151]–[1920,161]`；上面那条 `CheckRect("hdr_sep", …)` 钉着它摆在哪）——
            //    ⛔ 不是从 `UiQuadActive` 自己读回来的。
            //    ⚠️ 这条只验「**不是恒 false**」；「不是恒 true」那一半由上面 `flt_input` 的**关着那条**钉。
            CheckTrue(_rt.UiQuadActive("hdr_sep"),
                      "九宫格件 `hdr_sep`（页头分隔线，3 块、不在 `_named` 里）**读得出「露着」**"
                      + "（A57 ①：只走 `Lookup` 的旧写法在这里恒答 false）");
            // 块名 = `{根}_{i}{j}`（i = 横序号、j = 纵序号）；j=0/2 那两行高为 0 不建 ⇒ 左边那块是 `_01`
            if (_rt.UiQuadRect("hdr_sep/hdr_sep_01", out float spx, out float spy, out float spw, out float sph))
            {
                CheckNear(spw, 106.6667f, 0.6f,
                          "端帽宽 = 原版 `m_Border 80 ÷ ppuMul 0.75` = **106.6667**（不是 80 —— ppuMul 会缩放端帽）");
                CheckNear(sph, 10f, 0.6f, "……高 = 整条高 10（上下 border = 0 ⇒ 那一行就是整条）");
            }
            else Check(true, false, "`hdr_sep_01` 量得到（量不到 ⇒ 那层不是九宫格、退回单块拉伸了）");
            // 🔴 **2026-10-04（A41 ③ 修好单位之后抓出来的真重叠）**：页头分隔线与侧栏底板 / 页签高亮
            //   在 **y 156..161 那 5px** 上是**真重叠**的（逐对算：与 `Info [109.2,218.2]` 重叠 **51×5px**、
            //   与 `Cosmetics [218.2,327.1]` 重叠 **108.9×5px**、与 `side_bg` 168×5px —— 原版 rect 也一样重叠）。
            //   ⚠️ **2026-10-04 订正（R-W4 F7a）**：这句原来把 `Cosmetics` 写成 **160×5px** —— 160 是
            //   **整块 `Buttons`**（`327.1 − 167.2`）与分隔线的重叠，不是 `Cosmetics` 那一格。
            //   原版靠**兄弟序**分先后：`Content Area` 的孩子是 `Background → Header → Sidebar → …`
            //   ⇒ **侧栏及其页签压在分隔线之上**。我们原来三者（含分隔线）分别在 `QPanel`/`QSide`，
            //   方向还反着（分隔线压在侧栏上）⇒ 现在分隔线单开最底层（`QSep`）。
            //   这条断言盯的就是「层序不许再翻回去」。
            CheckTrue(_rt.UiQueueOf("hdr_sep") < _rt.UiQueueOf("side_bg")
                      && _rt.UiQueueOf("side_bg") < _rt.UiQueueOf("tab_hi0"),
                      "层序 = 分隔线 < 侧栏底板 < 页签高亮（= 原版兄弟序 `Header → Sidebar → 页签`；"
                      + $"实测 {_rt.UiQueueOf("hdr_sep")}/{_rt.UiQueueOf("side_bg")}/{_rt.UiQueueOf("tab_hi0")}）");
            CheckRect("side_bg", -203f, 156f, 538.5f, 924.1f);
            CheckRect("name_bg", 9.5f, 311f, 307.7f, 50f);
            // 🔴 **2026-10-04（A24）换图**：卡组名输入框的底图原来用的是 `40K_dropdown_bg`
            //   —— 那张是**导入弹窗**的输入框图（`Import Deck Popup/Window/Input Field`）。
            //   原版这颗的 Image 实测 = **`InputFieldBackground`**（Unity 内置 32×32 · 九宫格 (10,10,10,10)
            //   · ppu=200 · `m_Color=(0.0627,0,0,1)`），同窗 `Card Filters/…/Input Field` 也是这一张。
            //   判据 = prefab 的 Image 组件 + `FilterPanelModel.InputSprite`（那一份是共用的唯一出处）。
            Check(_rt.UiTextureName("name_bg"), "InputFieldBackground",
                  "卡组名输入框的底图 = 原版那张 `InputFieldBackground`（**不是** `40K_dropdown_bg`）");
            CheckTrue(_rt.UiQuadCount("name_bg") >= 9,
                      $"它是**九宫格**（原版 `m_Type=Sliced` + border 10；实测 {_rt.UiQuadCount("name_bg")} 块）");
            CheckRect("foot_done", 13f, 1020.5f, 188.5f, 50.2f);
            // 🔴 **2026-09-27 更正：原来写的是 `50f, 40f`（拉伸），那是错值。**
            //    PA 普查实读：原版 `Content Area/Sidebar/Footer/Image` 是 **PA=1**，
            //    贴图 `40k_general_icon_card_amount` **64×64** 塞进 50×40 ⇒ 原版实绘 **40×40**（**居中**）。
            //    框左 201.6、宽 50 ⇒ 内接后左 206.6、宽 40（中心 226.6 不动）。
            CheckRect("foot_ic", 206.6f, 1025f, 40f, 40f);

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
                    // 🔴 **2026-09-27 更正：原来断言的是 `30×44`（拉伸），那是错值。**
                    //    PA 普查实读：原版 `…/WIldcard Counter/Counters/*/Icon` 是 **PA=1 + Simple**，
                    //    4 张图分别是 42×51（Common）与 41×51（Rare/Epic/Legendary）塞进 30×44 的框
                    //    ⇒ 原版实绘 **30×36.4**（第 1 个）/ **30×37.3**（后三个）（**按框居中**）。
                    float wantH = (i == 0) ? 30f * 51f / 42f : 30f * 51f / 41f;
                    Check(Mathf.Abs(w - 30f) < 0.6f && Mathf.Abs(h - wantH) < 0.6f, true,
                          $"第 {i + 1} 个图标 = **30×{wantH:F0}**（PA=1 内接；实测 {w:F0}×{h:F0}）");
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
                Check(_rt.UiWcText(i), "99",
                      $"第 {i + 1} 个数字 = **恒定 `99`**（用户 2026-09-28 拍板；原版那 4 个数是"
                      + " `WildcardDisplay` 库存直出、跟着指针悬停那张卡的阵营走，我们没有 hover）");
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
            // 🔴 **2026-09-28 订正**：卡位 384 里**底下 38.272 是那条「张数」**（原版
            //    `Collection Card/Content/Counter` 的锚点算出来的，见 `DeckRuntime.PoolCounterH`）
            //    ⇒ **卡只占上面 345.728**（原来按 384 画 = 卡顶满格子、张数条压住卡底；实拍不是这样）。
            float wantScale = 345.728f / (CardView.Height * 108f);
            CheckTrue(Mathf.Abs(CardViewScaleOf(0) - wantScale) < 1e-4f,
                      $"卡池第一张卡的缩放 = {wantScale:F4}（按**卡位高 384 − 张数条 38.272** 反解）");
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
                    Check(Mathf.Abs(cy - 328.86f) < 0.6f, true,
                          $"卡池第 0 格**那张卡**的中心 y = **328.86**（156 + 345.728/2 —— 卡在张数条上方那一段里居中；"
                          + "原来是 348 = 整格中心，那是把卡画满整格的老口径 ✗）");
                }
                CheckTrue(_rt.UiPoolCellRect(5, out cx, out cy, out w, out h), "卡池第 6 格（最后一列）在");
                if (_rt.UiPoolCellRect(5, out cx, out cy, out w, out h))
                    Check(Mathf.Abs(cx - 1781.35f) < 0.6f, true,
                          $"卡池第 6 格中心 x = **1781.35** ⇒ 右边界 1912.6 落在 1920 里（**这是「6 列」的判据**）");
                // 🆕 2026-09-28：每格底下那条「张数」（原版 `Collection Card/Content/Counter`）
                CheckTrue(_rt.UiHasQuad("poolbar_0"), "卡池第 0 格**底下那条张数**的底图建出来了");
                var poolDef = _rt.UiPoolCellDef(0);
                CheckTrue(poolDef != null, "（前提）卡池第 0 格上有卡");
                if (poolDef != null)
                {
                    int cap = DeckRules.CopyLimit(poolDef.Rarity);
                    string wl = _rt.State.Deck.WarlordId;
                    // ① **有督军** ⇒ `{已在卡组}/{min(拥有, 上限)}` —— 原版 `DeckEditorCollectionDisplay__DrawCell`
                    //    的格式串**实测就是 `"{0}/{1}"`**（`stringliteral.json@0x426DE28`）。
                    if (!string.IsNullOrEmpty(wl))
                        Check(_rt.UiLabelText("poolcnt_0"), _rt.State.Deck.CountOf(poolDef.Id) + "/" + cap,
                              "★ 有督军时那条写 **`{已在卡组}/{能放的张数}`**（原版 `\"{0}/{1}\"`）");
                    // ② **还没有督军**（新建卡组 / 正在挑督军）⇒ `x{能放进卡组的张数}` ——
                    //    原版 `CardCollectionDisplay__SetCell.c:72` 的格式串是 **`x{0}`**（⚠️ 它喂的是**拥有数**）；
                    //    我们这版资源全解锁 ⇒ 照原式会显示 `x11` 那种没意义的数，按**用户 2026-09-28 的口径**
                    //    喂「能放进卡组的张数」。两个分支都要验 —— 就地把督军摘掉再放回去。
                    if (!string.IsNullOrEmpty(wl))
                    {
                        _rt.State.ClearWarlord();
                        _rt.RefreshAll();
                        Check(_rt.UiLabelText("poolcnt_0"), "x" + cap,
                              "★ 没督军时那条写 **`x{能放进卡组的张数}`**（原版 `x{0}`；见 `PoolCounterText` 的注释）");
                        _rt.State.SetWarlord(wl);
                        _rt.RefreshAll();
                        Check(_rt.State.Deck.WarlordId, wl, "（把督军放回去 —— 后面的断言仍按原状态跑）");
                    }
                }
            }

            // 筛选栏：默认关着；打开后盖住侧栏（队列更大 = 更后画）
            Check(_rt.FiltersOpen, false, "刚建好时筛选栏是关着的");
            // 🔴 这条是踩出来的：底板建了却**没跟着开关隐藏** ⇒ 它（队列 3020，比侧栏大）
            //    会一直盖住整个侧栏 —— 画面上只剩一块底板色，卡组行/页签/Done 全看不见。
            CheckTrue(!_rt.UiQuadActive("flt_bg"), "关着的时候筛选栏底板**不显示**（不然会盖住整个侧栏）");
            // 🔴 **2026-10-05（A57 ①）：上面那条 `flt_bg` 与下面 `side_bg` 都是 `Img()` 建的【单块】**
            //    —— `UiQuadActive` 以前**只查 `_named`**，对**九宫格 / 子树**件**恒答 false**（静默）。
            //    这两条拿抽屉里的**搜索框底 `flt_input`** 做**两态对照**：它是
            //    `ImageQuad.CreateNineSlice` 建的九宫格、**不进 `_named`**，而且挂在容器 `flt_drawer` 底下。
            //    · 关着 ⇒ **不露** —— 这一条同时钉住「这个读数**不是恒 true**」；
            //    · 打开 ⇒ **露** —— 这一条**去掉兜底必红**（旧写法两种状态都答 false）。
            //    ⚠️ 「打开」那条**不能**拿常显件（如 `hdr_sep`）顶替两态：常显件在两种状态下同值，
            //    它只能验「不是恒 false」、验不出「不是恒 true」（同段 `hdr_sep` 那条就是只验前一半）。
            CheckTrue(!_rt.UiQuadActive("flt_input"),
                      "九宫格件（搜索框底 `flt_input`）**关着时不露** —— 同时钉「这条读数不是恒 true」");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, true, "点一下 Filters 键 → 筛选栏打开");
            CheckTrue(_rt.UiQuadActive("flt_bg"), "打开后筛选栏底板显示出来");
            CheckTrue(_rt.UiQuadActive("flt_input"),
                      "……同一时刻**九宫格件**（搜索框底 `flt_input`，不在 `_named` 里、又挂在容器 "
                      + "`flt_drawer` 底下）也读得出「露着」（A57 ①：少了兜底这里会**谎报 false**）");
            CheckTrue(_rt.UiQuadActive("side_bg"), "筛选栏开着时侧栏底板还在（它被盖住，不是被删掉）");
            if (_rt.UiQuadRect("flt_bg", out float fx, out float fy, out float fw, out float fh))
            {
                // 权威表是「左上 + 宽高」= [2.2,156] 331.7×924.1 ⇒ 中心 (168.05, 618.05)
                CheckRectPx("flt_bg", fx, fy, fw, fh, 2.2f + 331.7f * 0.5f, 156f + 924.1f * 0.5f, 331.7f, 924.1f);
                CheckTrue(fx < 336f, $"筛选栏在**左**（中心 x = {fx:F1}，原版权威坐标说左、与侧栏同格）");
            }

            // 🆕 2026-09-28：**照原版补的三件**（搜索框 + Owned + Upgradable）+ 七行的行高 / 格尺寸。
            //   判据 = `资料/普查产出_0923/A3_Cards页.md` §5·1 实读（模型 = `Core/FilterPanelModel.cs`，与收藏窗同一份）；
            //   出厂态 = `资料/卡组编辑界面_查证_0920.md` 末节（2026-09-28 三路实读）。
            Check(_rt.UiFilterCellCount, 31, "抽屉里 31 个格子（2 + Army 13 + Rarity 5 + Cost 8 + Type 3）");
            if (_rt.UiFilterCell("$owned", out float ownCx, out float ownCy, out float ownW, out float ownH, out bool ownOn))
            {
                // 面板内 79.02 起、高 50（**整行可点**，原版就是那一行的 `EverguildToggle`）
                CheckRectPx("flt_owned", ownCx, ownCy, ownW, ownH, 2.2f + 331.7f * 0.5f, 156f + 79.02f + 25f, 331.7f, 50f);
                CheckTrue(ownOn, "`Owned` **出厂就是【开】**（prefab `m_IsOn=1` + `showOnlyOwnedCards` 初值 true）");
            }
            else Check(true, false, "找不到 `$owned` 那一格");
            if (_rt.UiFilterCell("$upgradable", out float upgCx, out float upgCy, out float upgW, out float upgH, out bool upgOn))
            {
                CheckRectPx("flt_upgradable", upgCx, upgCy, upgW, upgH, 2.2f + 331.7f * 0.5f, 156f + 129.02f + 25f, 331.7f, 50f);
                CheckTrue(!upgOn, "`Upgradable` **运行期是【关】**（`UpgradableCardFilter__SetupFilter.c:16` 强制置 false）");
            }
            else Check(true, false, "找不到 `$upgradable` 那一格");
            // 搜索框三件：`Input Field` 281.28×40 **居中** + `Text Area` + 尾图标 `40k_icon_search` 35×30
            _rt.UiFilterNameRects(out float inX, out float inY, out float inW, out float inH,
                                  out float taX, out float taY, out float taW, out float taH,
                                  out float icnX, out float icnY, out float icnW, out float icnH);
            CheckRectPx("flt_input", inX + inW * 0.5f, inY + inH * 0.5f, inW, inH,
                        2.2f + 25.21f + 281.28f * 0.5f, 156f + 19.51f + 20f, 281.28f, 40f);
            CheckRectPx("flt_searchicon", icnX + icnW * 0.5f, icnY + icnH * 0.5f, icnW, icnH,
                        2.2f + 266.55f + 17.5f, 156f + 24.51f + 15f, 35f, 30f);
            Check(_rt.UiFilterInputText, "Search", "空的时候搜索框画的是**占位符**（原版 `Placeholder` 原文）");
            // 🔴 2026-09-28（审核抓到）：前面那条「同一层不许压住」是在**抽屉还关着**时量的
            //    （`quads` 那会儿还没有这 31 格）⇒ 抽屉里的重叠一条都测不到。**开着再量一次。**
            {
                var draw = new List<ImageQuad>();
                foreach (var q in _root.GetComponentsInChildren<ImageQuad>(false))
                    if (q.name != null && q.name.StartsWith("flt_")) draw.Add(q);
                int bad = 0; var pair = new List<string>();
                for (int i = 0; i < draw.Count; i++)
                    for (int j = i + 1; j < draw.Count; j++)
                    {
                        var A = draw[i]; var B = draw[j];
                        if (A.RenderQueue != B.RenderQueue) continue;
                        var pa = DeckRuntime.PxOfWorld(A.transform.position);
                        var pb = DeckRuntime.PxOfWorld(B.transform.position);
                        // 🔴 2026-10-04（A41 ③）：**右边同样要乘 `PxPerUnit`** —— 这里是上面那条判据的
                        //    第二份拷贝（同一个「单位混了」的毛病，不改的话两条行为不一致）。
                        float bx = Mathf.Abs(pa.x - pb.x) - (A.WorldW + B.WorldW) * 0.5f * PxPerUnit;
                        float by = Mathf.Abs(pa.y - pb.y) - (A.WorldH + B.WorldH) * 0.5f * PxPerUnit;
                        if (bx < -LayTol && by < -LayTol) { bad++; if (pair.Count < 5) pair.Add(A.name + " × " + B.name); }
                    }
                CheckTrue(draw.Count >= 30, $"抽屉里量到 {draw.Count} 张可见图（31 格 + 底 + 输入框那几件）");
                CheckTrue(bad == 0, $"抽屉**开着**时同层的图也没有互相压住（实测 {bad} 处"
                          + (pair.Count == 0 ? "）" : "：" + string.Join(" · ", pair) + "）"));
            }
            Shoot("deck_filters.png");   // 改版面要看截图（断言测不出「字压住了 / 图标没出来」）

            // 两个开关点了真的改状态（`Upgradable` 在单机必然筛成空 —— 界面上会出声说明，不许静默）
            int visBefore = state.VisibleCards().Count;
            _rt.UiFilterRow("$upgradable");
            Check(state.Filter.Upgradable, true, "点 `Upgradable` ⇒ 开关打开");
            Check(state.VisibleCards().Count, 0, "打开后**一张不剩**（单机没有升级系统 ⇒ 恒空是预期的）");
            _rt.UiFilterRow("$upgradable");
            Check(state.VisibleCards().Count, visBefore, "再点一下 ⇒ 回到原来的张数");

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

            // 🆕 2026-10-04（§三第29条 **A67**）：**左抽屉的滑进/滑出**（原来是整块 `SetOn` 硬切）
            {
                Section("筛选抽屉滑入/滑出：行程 −385px + 0.3 秒 + 位移期间不吃点击（A67）");
                // 判据（唯一一处）= `CollectionFilterController<T>.Toggle(bool, bool)`：
                //   `DOTween.Kill` + **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`
                //   —— 收起 x = `hiddenPosition.x` = **−550**、展开 x = `originalAnchorPosition.x` = **−165**
                //   ⇒ **行程 = −385px**；`animationTime` = **0.3**。
                //   逐实例实据：menus 包里 6 个带 `hiddenPosition` 的实例**全是 (−550,0) / 0.3**，
                //   其中就有本窗这两棵（`Card Filters [2,156 332x924]` / `Cosmetic FIlter (inactive)`）。
                // ⚠️ **量的是渲染出来的东西**（`UiNodeRect("flt_drawer")` = 整栏那棵子树的包围盒，走世界坐标），
                //   **不是**读我们自己的常量：把 `LayoutSpace.Px(...)` 去掉（px 裸进世界单位）时这里量到 −59400。
                Check(_rt.FiltersOpen, false, "接着上一段：抽屉现在是关着的");
                CheckNear(_rt.FilterDrawerSlide, 0f, 0.001f, "关 ⇒ 进度 **0**（整栏停在 `hiddenPosition` 那一头）");
                CheckTrue(!_rt.FilterDrawerInteractive, "…不参与命中（滑出去那一头同样不吃）");
                CheckTrue(!_rt.UiQuadActive("flt_bg"), "…底板**不露**（关的是整栏那个容器，不是逐件 SetActive）");

                _rt.UiToggleFilters();
                CheckNear(_rt.FilterDrawerSlide, 1f, 0.001f, "点一次 ⇒ 进度 **1**（原位）—— 批处理没有帧循环，直接到位");
                CheckTrue(_rt.FilterDrawerInteractive, "…到位了 ⇒ 命中生效");
                CheckTrue(_rt.UiQuadActive("flt_bg"), "…底板露出来");

                float openCx, openCy, openW, openH;
                CheckTrue(_rt.UiNodeRect("flt_drawer", out openCx, out openCy, out openW, out openH),
                          $"量得到整栏那棵子树（{openW:F1}×{openH:F1}px）—— 它就是这次位移的**唯一对象**");
                CheckNear(openCx, 168.05f, 6f, "原位时整栏中心 x ≈ **168.05**（权威表 [2.2,156] 331.7×924.1 的中心）");

                // ① 时间：`animationTime` = 0.3 ⇒ 推进 0.15 秒正好走半个行程
                _rt.SetDrawerProgressForTest(false, 0f);
                CheckTrue(_rt.UiQuadActive("flt_bg"),
                          "钉在**滑出去的起点**但目标是开 ⇒ 节点**还活着**（原版先 SetActive 再动 tween；"
                          + "不然滑出来那 0.3 秒根本看不见东西）");
                CheckTrue(!_rt.FilterDrawerInteractive, "…起点当然不算到位 ⇒ 命中仍失效");
                _rt.TickDrawersForTest(0.15f);
                CheckNear(_rt.FilterDrawerSlide, 0.5f, 0.01f,
                          "推进 **0.15 秒**（`animationTime` = 0.3）⇒ 进度 **0.5**（**只有真按 0.3 秒走**才成立）");
                _rt.TickDrawersForTest(0.15f);
                CheckNear(_rt.FilterDrawerSlide, 1f, 0.001f, "再推 0.15 秒 ⇒ 到位（一共 0.3 秒）");
                CheckTrue(_rt.FilterDrawerInteractive, "…到位 ⇒ 命中生效");

                // ② 位移量：进度 0 与进度 1 两处量同一棵子树的位置差 = 原版那段行程
                _rt.SetDrawerProgressForTest(false, 0f);
                float shutCx, shutCy, shutW, shutH;
                CheckTrue(_rt.UiNodeRect("flt_drawer", out shutCx, out shutCy, out shutW, out shutH),
                          "滑出去那一头也量得到（容器关掉时 `GetComponentsInChildren(true)` 照样进）");
                CheckNear(shutCx - openCx, -385f, 1.5f,
                          $"行程 = **−385px**（收起 −550 / 展开 −165 之差；实测 {shutCx - openCx:F1}）"
                          + " —— 单位换算写错时这里是 −59,400（收藏窗真踩过那个 bug）");
                CheckTrue(shutCx + shutW * 0.5f < 0f,
                          $"滑出去之后**整栏右缘在屏外**（x = {shutCx + shutW * 0.5f:F1} < 0）");

                // ③ **两态对比**：同一个点，在「位移途中」与「完全到位」两种状态下结果必须不同
                //    （只断一半 = 弱断言：写成恒「不吃」/ 恒「吃」都会有一条绿）
                if (_rt.UiFilterCell("$owned", out float ocx, out float ocy, out float ocw, out float och,
                                     out bool oon))
                {
                    CheckTrue(oon, "（前提：`Owned` 出厂就是开的 —— 与上面那条一致）");
                    // ⚠️ 点在这一格的**右缘内侧**（中心 + 半个宽 − 4）：位移期间这一栏不吃的话，
                    //   点击会**往下落**到它盖住的东西上；本窗那三颗页签的右缘是 327.2 ⇒ 特意挑 ≈329.9，
                    //   免得「开关没被翻」是**点到了别处**、而不是「这一栏不吃」。
                    float clickX = ocx + ocw * 0.5f - 4f;
                    CheckTrue(clickX > 327.2f, $"（这一下落在 x = {clickX:F1} > 页签右缘 327.2 —— 落下去也点不到页签）");
                    _rt.SetDrawerProgressForTest(false, 0.5f);
                    bool own0 = state.Filter.Owned;
                    CheckTrue(!_rt.UiClickPx(clickX, ocy), "位移途中（进度 0.5）点这一格 ⇒ **没人吃这一下**");
                    Check(state.Filter.Owned, own0, "…`Owned` 开关**没被翻**（这一栏此刻不参与命中）");
                    _rt.SetDrawerProgressForTest(false, 1f);
                    CheckTrue(_rt.FilterDrawerInteractive, "推到 1 ⇒ 命中生效");
                    CheckTrue(_rt.UiClickPx(clickX, ocy), "★ **同一点** ⇒ 这次**吃**了（两态对比的另一半）");
                    Check(state.Filter.Owned, !own0, "…`Owned` **真被翻了**（写成恒不吃 / 恒吃，这里必红一条）");
                    _rt.UiClickPx(clickX, ocy);                       // 翻回来，别影响后面几段
                    Check(state.Filter.Owned, own0, "（还原成原来的样子）");
                }
                else Check(true, false, "找不到 `$owned` 那一格");

                // ④ 收起：逻辑态**立刻**翻假，但整栏要滑出去 0.3 秒 —— 那 0.3 秒里格子得跟着一起走
                //    （拆早了画面上只剩一块空底板在滑；原版是整栏连格子一起滑走）
                _rt.UiToggleFilters();
                Check(_rt.FiltersOpen, false, "点 Filters ⇒ 逻辑态**立刻**翻假（UI / 自检读的都是它）");
                CheckTrue(_rt.UiFilterCellObjects > 0,
                          $"…但格子**还留着**（{_rt.UiFilterCellObjects} 件）—— 收起途中要看见它们一起滑走");
                _rt.SetDrawerProgressForTest(false, 0.5f);
                CheckTrue(_rt.UiQuadActive("flt_bg"), "收起途中（进度 0.5）底板**还活着**（要画）");
                CheckTrue(!_rt.FilterDrawerInteractive, "…但**已经不吃**点击/滚轮");
                _rt.SetDrawerProgressForTest(false, 0f);
                CheckTrue(!_rt.UiQuadActive("flt_bg"), "滑到底 ⇒ 整栏关掉（`activeInHierarchy` 假 ⇒ 不再盖住侧栏）");
            }

            // 页签：Cards / Deck info / Cosmetics —— 费用曲线只在 info 页显示
            // 页签：Cards / Deck info / Cosmetics —— 三页各有各的东西，**不能是空白页**
            _rt.UiSetTab(1);
            Check(_rt.ActiveTab, 1, "切到 Deck info 页签");
            CheckTrue(_rt.UiCurveVisible, "Deck info 页签里费用曲线可见（Cards 页签下它是关的）");
            CheckTrue(_rt.UiInfoActionsVisible, "Deck info 页签里「分享 / 导入」两颗钮显示出来");
            CheckTrue(!_rt.UiCosmeticsVisible, "Cosmetics 那组东西在别的页签下是关的");
            CheckTrue(_rt.UiDeckRowAt(0) == null, "Deck info 页签下卡组行不显示");
            // 🔴 **2026-10-05（A57 ①）：同一条「行藏没藏」再过一遍【图】那一半** —— `UiDeckRowAt` 读的是
            //    状态（`_tab` + `shown`），答不出「那棵九宫格**树**真的关掉了吗」（`SetOn(_deckRowBg[i], …)`，
            //    `DeckRuntime.cs` 的 `RefreshDeckList`）。`row_0` 正是 A57 ① 点名的受影响件之一
            //    （九宫格、**不进 `_named`**）⇒ 下面这一对同时钉住「关得住」与「读得出关」。
            //    期望值出处：行属 Cards 页（原版 `Content Area/Sidebar` 的卡组列表只在 Cards 页签下），
            //    另由紧邻的 `UiDeckRowAt` 那两条独立佐证 —— ⛔ 不是从 `UiQuadActive` 自己读回来的。
            CheckTrue(!_rt.UiQuadActive("row_0"),
                      "Deck info 页签下**那棵九宫格行树真的关了**（`row_0` 不在 `_named` 里 ⇒ "
                      + "少了兜底这条会**碰巧**答 false，所以必须与下面那条成对看）");
            _rt.UiSetTab(2);
            CheckTrue(_rt.UiCosmeticsVisible, "Cosmetics 页签：**卡背那一页显示出来**（2026-09-24 起是真页面，不再是一句空态）");
            CheckTrue(!_rt.UiCurveVisible, "Cosmetics 页签下费用曲线关掉");
            CheckTrue(!_rt.UiInfoActionsVisible, "Cosmetics 页签下动作钮关掉");
            _rt.UiSetTab(0);
            CheckTrue(!_rt.UiCurveVisible, "切回 Cards → 费用曲线隐藏");
            CheckTrue(!_rt.UiCosmeticsVisible, "切回 Cards → Cosmetics 空态隐藏");
            CheckTrue(_rt.UiDeckRowAt(0) != null, "切回 Cards → 卡组行回来");
            // 🔴 **2026-10-05（A57 ①）**：与上面 `row_0` 那条**成对** —— 同一条九宫格行树，切回 Cards 页
            //    必须**露出来**。前面那条 `UiDeckRowAt(0) != null` 已经钉住「此刻确实有行」（非空卡组 +
            //    `_tab==0`，`DeckRuntime.cs` 的 `RefreshDeckList` 的 `on = cards && firstRow+i < shown.Count`）
            //    ⇒ 这里期望 `true` 有独立出处，**去掉兜底这条必红**（旧写法对九宫格件恒答 false）。
            CheckTrue(_rt.UiQuadActive("row_0"),
                      "切回 Cards → **那棵九宫格行树也真的露出来了**（A57 ① 点名受影响的 `row_*` 那一族）");
            // 🔴 「该藏的藏住了吗」——**图 + 文字一起查**（只查图会漏：`Lookup` 不认 Label）
            Check(_rt.UiInfoOnlyActive, 0, "Cards 页签上**一件 Deck info 的东西都不许露**（图 + 文字都算）");
            Check(_rt.UiCosmOnlyActive, 0, "Cards 页签上不许露 Cosmetics 的东西");

            // 🔴🔴 **2026-10-04（A41 ④）：「藏住了」还不够 —— 藏住的东西【不许吃点击】**。
            //   真缺陷（原来 `_btns` 里的矩形不跟着显隐走）：那两片空白（`info_share` / `info_import`，
            //     · (60..131, 636..707) = `info_share` ⇒ **一次点击静默把卡组串写进剪贴板**；
            //     · (200..271, 636..707) = `info_import` ⇒ **静默打开导入弹窗**）
            //   同一族还有第二处：导入弹窗**关着**时，它的输入框矩形 (610..1310, 370..511)
            //   正好落在**卡池**里 ⇒ 点在没卡的空白上会进入一个**看不见的文本编辑态**（`_editKind=3`）。
            //   判据 = 原版那套「`SetActive(false)` / `m_TargetGraphic.enabled=0` 之后就不再参与射线」
            //   （UGUI `Graphic.Raycast` 只对**激活且在画**的图形生效）⇒ 我们收口成 `DeckRuntime.KeyLive`。
            // 🔴 **2026-10-04 订正（R-W4 F1/F2）** —— 两句措辞原来都太宽：
            //   ① **「必现页」是 `Cosmetics`，不是 `Cards`**：真实指针链是
            //      `HandlePoolClick → HandleDeckRowClick → HandleCosmeticClick → HandleButtons`
            //      （`DeckRuntime.cs:1606-1610`，`HandleButtons` 是**最后一站**），而那两片矩形
            //      **整片落在卡组列表里**（x 0.4..325.4 · y 366..1010.1）⇒
            //        · **Cosmetics 页（必现）**：`_tab==2` ⇒ `HandleDeckRowClick`（`:1634` 要求 `_tab==0`）
            //          早退、`HandleCosmeticClick`（`:1159` 要求 `_tab==2`）只管 x ≥ `CosmoX` 那一片
            //          ⇒ **谁也拦不住**，直接落到 `HandleButtons`；
            //        · **Cards 页**：只要那一格真有行，`HandleDeckRowClick` 就先把它吃掉（开始行拖拽）
            //          ⇒ **点不到**；只有**那一格是空槽**时才漏得过去（`RefreshDeckList` 的
            //          `on = cards && (firstRow + i) < shown.Count`，`:1263`）。
            //   ② **下面这几条走的是 `KeyLive` 那条命中路**（`UiTopKeyAt` → `HitBtn`/`KeyLive`；
            //      `UiClickPx` 直调 `HandleButtons`），**不是**真实鼠标那条完整分派链
            //      ⇒ 只能说「**不被任何按钮**吃到」，不能说「谁都没吃到」（行处理器不在这一层）。
            {
                _rt.UiBtnRect("info_share", out float shX, out float shY, out float shW, out float shH);
                _rt.UiBtnRect("info_import", out float imX, out float imY, out float imW, out float imH);
                _rt.UiBtnRect("imp_input", out float ipX, out float ipY, out float ipW, out float ipH);
                float shCx = shX + shW * 0.5f, shCy = shY + shH * 0.5f;
                float imCx = imX + imW * 0.5f, imCy = imY + imH * 0.5f;
                float ipCx = ipX + ipW * 0.5f, ipCy = ipY + ipH * 0.5f;

                CheckTrue(_rt.ActiveTab == 0 && !_rt.UiInfoActionsVisible,
                          "（前提）现在在 Cards 页签、两颗动作钮是**隐藏**的");
                Check(_rt.UiTopKeyAt(shCx, shCy), null,
                      "分享钮那片空白**攒不到任何按钮 key**（`KeyLive` 那条命中路；隐藏时原来会静默分享到剪贴板）");
                CheckTrue(!_rt.UiClickPx(shCx, shCy), "……点下去也没有**任何按钮**吃到这一下（走 `HandleButtons`）");
                Check(_rt.UiTopKeyAt(imCx, imCy), null, "导入钮那片空白**同样攒不到任何按钮 key**");
                _rt.UiClickPx(imCx, imCy);
                Check(_rt.ImportOpen, false, "……点下去也不会**静默打开导入弹窗**");
                CheckTrue(!_rt.NameEditing, "（前提）现在不在文本编辑态");
                Check(_rt.UiTopKeyAt(ipCx, ipCy), null,
                      "导入弹窗**关着**时，`imp_input` 那片（在卡池里）命中不到任何东西");
                _rt.UiClickPx(ipCx, ipCy);
                CheckTrue(!_rt.NameEditing, "……也不会点进一个**看不见的输入态**（静默失败那一类）");

                // ---- 正向控制：**同一批点位**，在「显示出来」的时候必须照常命中 ----
                // （不然上一条可能是因为「这些 key 压根没注册」，那是另一种绿）
                _rt.UiOpenImport();
                Check(_rt.UiTopKeyAt(ipCx, ipCy), "imp_input",
                      "（正向控制）**弹窗开着**时同一点 = `imp_input` ⇒ 上一条不是「这个 key 没注册」");
                _rt.UiCloseImport();
                _rt.UiSetTab(1);
                Check(_rt.UiTopKeyAt(shCx, shCy), "info_share",
                      "（正向控制）切到 Deck info 页 ⇒ 同一点命中 `info_share`（只有藏起来时才不许命中）");
                Check(_rt.UiTopKeyAt(imCx, imCy), "info_import", "（正向控制）导入钮那位命中 `info_import`");
                _rt.UiClickPx(imCx, imCy);
                Check(_rt.ImportOpen, true, "……而且真的打开了导入弹窗（显示时照常、隐藏时才挡）");
                _rt.UiCloseImport();
                _rt.UiSetTab(0);
                CheckTrue(!_rt.ImportOpen && !_rt.NameEditing, "（收尾）弹窗关着、回到 Cards 页");
            }
            Shoot("deck_cards.png");

            // ============================================================ 🆕 2026-10-04（A24）悬停换图 + 状态换图
            // 这一整段以前**一条断言都没有** ⇒ 屏幕上「悬停什么都不发生」也全绿（普查 A24 的结论）。
            // 判据逐条 = 原 prefab 的字段；出处写在 `CheckHoverOne` 上面那一段。
            Section("悬停换图（原版 `m_Transition=2` 的那 5 颗）");
            {
                CheckHoverOne("hdr_back", "hdr_back", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页头 `Close`（文本 'Back'）");
                CheckHoverOne("hdr_clear", "hdr_clear", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页头 `Clear filters`");
                CheckHoverOne("foot_done", "foot_done", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页脚 `Done`");
                // 按下态（原版 `m_PressedSprite`）：按着的时候画按下图，抬起还原
                if (_rt.UiBtnRect("foot_done", out float dX, out float dY, out float dW, out float dH))
                {
                    var done = _root.Find("foot_done") != null ? _root.Find("foot_done").GetComponent<WindowButton>() : null;
                    CheckTrue(_rt.UiPressAt(dX + dW * 0.5f, dY + dH * 0.5f, true, false) == done,
                              "页脚 `Done`：左键按下 ⇒ 进**按下态**（原版那颗有 `m_PressedSprite`）");
                    if (done != null)
                        CheckTrue(done.CurrentTexForTest == done.PressedTexForTest,
                                  "按下时贴图 = 原版 `UI_Button_Mulligan_Pressed`");
                    _rt.UiPressAt(dX + dW * 0.5f, dY + dH * 0.5f, false, true);
                    if (done != null)
                        CheckTrue(done.CurrentTexForTest == done.NormalTexForTest, "抬起 ⇒ 还原成常态图");
                }
                // 导入弹窗那两颗是**模态件**（`_modalOnly` 出厂 `SetActive(false)`）⇒ 先打开再验
                _rt.UiOpenImport();
                CheckHoverOne("imp_ok", "imp_ok", "40K_button", "40K_button_hover", "40K_button_pressed",
                              "导入弹窗 `Confirm`");
                // ⚠️ 这颗的 `m_TargetGraphic` 实测 = **子件 `Icon`**（= 我们那颗 `imp_close_x`），
                //    不是按钮节点的圆底（普查 §二 块 B 写的是按钮节点的常态图 —— 本轮已就地订正）
                CheckHoverOne("imp_close", "imp_close_x", "40k_bt_close", "40k_bt_close_hover",
                              "40k_bt_close_pressed", "导入弹窗关闭钮（原版目标是**子件 `Icon`**）");
                // 🔴 **2026-10-04（A37 ⑤）**：那颗图标我们原来画 **75×75**（跟着圆底走）。
                //   判据（`python 工具/menu_dump.py bundle_menus_assets_all "Import Deck Popup" --depth 8`）：
                //   `Generic Close Button Green` = `[1317.3,202.1]–[1392.3,277.1]`（**75×75 圆底，我们那层是对的**）·
                //   子件 `Icon` = `[1326.6,212.4]–[1383.0,266.8]` = **56.37×54.50**，图 `40k_bt_close` 175×174
                //   ·`Simple (1,1,1,1)` **无 preserveAspect**（原版是拉进这个矩形）。
                if (_rt.UiQuadRect("imp_close_x", out float ix, out float iy, out float iw, out float ih))
                {
                    CheckNear(iw, 56.37f, 0.02f, "导入弹窗关闭图标的宽 = 原版子件 `Icon` 的 **56.37**（原来 75）");
                    CheckNear(ih, 54.5f, 0.02f, "...高 = **54.50**（原来 75）");
                    CheckNear(ix, 1354.785f, 0.02f, "...中心 x = 1326.6 + 56.37/2");
                    CheckNear(iy, 239.65f, 0.02f, "...中心 y = 212.4 + 54.5/2");
                }
                else CheckTrue(false, "`imp_close_x` 量得到矩形（量不到 = 那颗关闭图标根本没画）");
                // 模态遮挡：弹窗开着时**背后**那颗不该亮（原版靠弹窗的全屏暗底吃射线，我们没建那块暗底）
                CheckTrue(_rt.UiHoverAt(267.2f, 113.5f) == null,
                          "导入弹窗开着时，**弹窗背后**的 `Back` 不亮（`HoverTargetUnder` 的模态口径）");
                _rt.UiCloseImport();
                Check(_rt.ImportOpen, false, "（收尾）导入弹窗关掉");
                // 反向：**关着**时那两颗看不见的钮不许悬停得上（否则会在看不见的件上亮起来 —— 静默的那种错）
                CheckTrue(_rt.UiHoverAt(960f, 648.5f) == null,
                          "导入弹窗**关着**时，`Confirm` 那一片没有悬停反应（`imp_*` 只在弹窗开着时算）");
                // 兜底：整棵树里**每一颗**接了换图的都要「换得动 + 还原得回」，且图片一张不缺
                CheckHoverSwap(_root, "卡组编辑窗");
                CheckNoMissingSwapArt("卡组编辑窗");

                // 🔴 **2026-10-04（A37 ④）**：点击与悬停**共用同一张命中顺序表**（`DeckRuntime.ClickOrder`）。
                //   原来两套口径（点击走 `HandleButtons` 的 if 链 · 悬停走 `_btns` 的**登记顺序**），
                //   而 `HoverTargetUnder` 的注释却写着「命中口径 = 和点击同一条」—— **声明是错的**。
                //   这条断言盯的就是「两套再分叉」：漏写进表里的那颗会「亮得起来但点不到」。
                var order = DeckRuntime.UiClickOrder();
                var hkeys = _rt.UiHoverKeys();
                CheckTrue(hkeys.Count >= 5,
                          $"（前提）确实有接了悬停换图的按钮（实得 {hkeys.Count} 颗；少于 5 ⇒ 本批那 5 颗没接全）");
                var notInOrder = new List<string>();
                foreach (var k in hkeys) if (System.Array.IndexOf(order, k) < 0) notInOrder.Add(k);
                CheckTrue(notInOrder.Count == 0,
                          "接了悬停换图的 key **全都在 `ClickOrder` 里**（漏的会「亮得起来但点不到」；漏的："
                          + string.Join("、", notInOrder.ToArray()) + "）");
                var noRect = new List<string>();
                foreach (var k in order)
                    if (!_rt.UiBtnRect(k, out _, out _, out _, out _)) noRect.Add(k);
                CheckTrue(noRect.Count == 0,
                          "`ClickOrder` 里每个 key 都真的注册了矩形（写错 key / 按钮被删 ⇒ 红；坏的："
                          + string.Join("、", noRect.ToArray()) + "）");
            }

            Section("状态换图（原版 `trans=1` + `onSprite/offSprite` 的那 7 颗）");
            {
                // ① 页头 `Filters`（`EverguildToggle` · `changeSpriteOnValueChange=1` · `m_IsOn=0`）
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt",
                      "Filters 键**关着**时 = `40k_menu_bt`（原版 `offSprite`）");
                _rt.UiBtnRect("hdr_filters", out float fbX, out float fbY, out float fbW, out float fbH);
                _rt.UiClickPx(fbX + fbW * 0.5f, fbY + fbH * 0.5f);          // 走鼠标那条路点它
                Check(_rt.FiltersOpen, true, "（前提）点一下 Filters 键 ⇒ 筛选面板打开");
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt_pressed",
                      "面板开着 ⇒ 换成 `40k_menu_bt_pressed`（原版 `onSprite`；原来**开着也不换图**）");
                _rt.UiClickPx(fbX + fbW * 0.5f, fbY + fbH * 0.5f);
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt", "关回去 ⇒ 换回 `40k_menu_bt`");

                // ② 侧栏三页签 —— 判据 = UGUI `Toggle.PlayEffect`：`graphic`（= 子件 `Highlight`）的 alpha 0/1
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(0) - 1f) < 1e-4f,
                          "Cards 页签（选中）的 `Highlight` alpha = 1");
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(1) - 0f) < 1e-4f,
                          "Deck info（未选中）的 alpha = **0**（原来打 0.15 ⇒ 未选中的页签上压着一层 15% 幽灵高亮）");
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(2) - 0f) < 1e-4f,
                          "Cosmetics（未选中）的 alpha = 0");
                _rt.UiSetTab(1);
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(1) - 1f) < 1e-4f,
                          "切到 Deck info ⇒ 它的高亮亮起来");
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(0) - 0f) < 1e-4f,
                          "Cards 的高亮同时灭掉（三选一）");
                _rt.UiSetTab(0);

                // 🔴 **2026-10-04（A37 ①）**：上面那几条**只验 alpha ⇒ 对颜色是瞎的** ——
                //   把 tint 打成白色（我们原来就是）照样全绿。原版那颗高亮 = **灰度图 + 红 tint**：
                //   判据 ① prefab 实读：子件 `Highlight` 的 Image `m_Color = (1,0,0,1)`
                //     （`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 6`
                //       那行里的 `Sliced (1,0,0,1)`，左边写着 `40k_main_bt_selected BW` = 灰度图）。
                //   判据 ② 通道语义：`Toggle.PlayEffect` → `CrossFadeAlpha` → `useRGB:false`
                //     ⇒ **只动 alpha、两个态的 RGB 都是红**。
                //   同工程早就做对过一次：`Shell/MenuWindowBase.cs:420-424`（同图 + `(1,0,0,1)`）。
                for (int i = 0; i < 3; i++)
                {
                    string who = i == 0 ? "Cards" : (i == 1 ? "Deck info" : "Cosmetics");
                    var c = _rt.UiTabHighlightTint(i);
                    CheckTrue(Mathf.Abs(c.r - 1f) < 1e-4f && Mathf.Abs(c.g) < 1e-4f && Mathf.Abs(c.b) < 1e-4f,
                              $"`{who}` 高亮 tint 的 **RGB = (1,0,0)**（原版 `m_Color` 是纯红；我们原来打的是白）"
                              + $"（实测 {c.r:F3},{c.g:F3},{c.b:F3}）");
                }
                // 顺带（A37 ①）：原版那颗 `Highlight` 的 rect = **整格 108.96×150**（与父节点同矩形），
                //   我们原来画 100×100（只有图标那一方块）。
                //   判据 = dump 的 `Cards [0.3,156]–[109.2,306]` 与子件 `Highlight` 同串。
                if (_rt.UiQuadRect("tab_hi0", out float hx, out float hy, out float hw, out float hh))
                {
                    CheckNear(hw, 108.9667f, 0.02f, "`Cards` 高亮的宽 = **整格 108.96**（原版 `[0.3,109.2]`）");
                    CheckNear(hh, 150f, 0.02f, "`Cards` 高亮的高 = **整格 150**（原版 `[156,306]`）");
                    CheckNear(hx, 54.7833f, 0.02f, "高亮中心 x = 0.3 + 108.9667/2");
                    CheckNear(hy, 231f, 0.02f, "高亮中心 y = 156 + 150/2");
                }
                else CheckTrue(false, "`tab_hi0` 量得到矩形（量不到 = 那颗高亮根本没画）");

                // 🔴 **2026-10-04（A41 ⑥）**：上面那条只验了**矩形** —— 原版那颗 `Image` 的
                //   `m_Type = Sliced` + `m_Border (30,30,30,30)` + **`m_PixelsPerUnitMultiplier 0.92`**
                //   ⇒ 画出来是**九宫格**、角块 = `30 ÷ 0.92` = **32.6087px**；我们原来**单块拉伸**
                //   （71² 的图拉到 108.97×150 ⇒ 那条 ~30px 的软边被拉成 ~46px）。
                //   判据 = dump 那行的 `九宫30,30,30,30 | Sliced (1,0,0,1) ppuMul=0.9200000166893005`。
                Check(_rt.UiQuadCount("tab_hi0"), 9, "页签高亮是**九宫格**（原版 `Sliced`）—— 一棵树 9 块，不是单块拉伸");
                Check(_rt.UiTextureName("tab_hi0"), "40k_main_bt_selected_BW",
                      "……图还是那张 `40k_main_bt_selected BW`");
                Check(_rt.UiTabHighlightBlocks(0), 9, "……而且**整棵树**都在（`UiTabHighlightBlocks`；只建一块 = 漏了九宫格）");
                // 角块的大小只有量**单独那一块**才看得出来（`tab_hi0` 自己的包围盒永远是整格）
                if (_rt.UiQuadRect("tab_hi0/tab_hi0_00", out float cnx, out float cny, out float cnw, out float cnh))
                {
                    CheckNear(cnw, 32.6087f, 0.6f,
                              "角块宽 = 原版 `m_Border 30 ÷ ppuMul 0.92` = **32.6087**（不是 30 —— ppuMul 会缩放角块）");
                    CheckNear(cnh, 32.6087f, 0.6f, "……角块高 = 32.6087");
                }
                else Check(true, false, "`tab_hi0_00` 量得到（量不到 ⇒ 那层不是九宫格，退回单块了）");

                // 🔴 **2026-10-04（A41 ①）**：页签图标 `Icon` 的 rect = **整格 108.96×150** +
                //   `m_PreserveAspect = 1`，源图 126×126 ⇒ **实绘 108.9667²、在格子里居中**
                //   （我们原来画 100×100 ⇒ **小 8.97px ≈ 9%**）。
                //   判据 = dump 的 `Cards/Icon [0.3,156]–[109.2,306] 108.96×150 …… preserveAspect`。
                string[] tabWho = { "Cards", "Deck info", "Cosmetics" };
                string[] tabIc = { "40k_collection_bt_cards", "40k_collection_bt_decks", "40k_collection_bt_cosmetics" };
                for (int i = 0; i < 3; i++)
                {
                    Check(_rt.UiTextureName("tab_ic" + i), tabIc[i], $"`{tabWho[i]}` 图标用的是原版那张 `{tabIc[i]}`");
                    if (_rt.UiQuadRect("tab_ic" + i, out float icx, out float icy, out float icw, out float ich))
                    {
                        CheckNear(icw, 108.9667f, 0.6f,
                                  $"`{tabWho[i]}` 图标实绘宽 = 整格 **108.9667**（PA 内接 126² 的图；原来 100）");
                        CheckNear(ich, 108.9667f, 0.6f, $"`{tabWho[i]}` 图标实绘高 = **108.9667**（同上）");
                        CheckNear(icx, 0.3f + 108.9667f * i + 54.4833f, 0.6f, $"……中心 x = 格左缘 + 108.9667/2");
                        CheckNear(icy, 231f, 0.6f, "……中心 y = 156 + 150/2（在整格里居中）");
                    }
                    else Check(true, false, $"`tab_ic{i}` 量得到矩形（量不到 = 图标根本没画）");
                }

                // 🔴 **2026-10-04（A41 ②）**：页签**少了一块名牌底板**。原版 `Cards/Label`：
                //   图 `40k_main_bt_nametag`（109×41 · `Simple` · **无 PA** ⇒ 拉进矩形）、
                //   rect `[5.3,261]–[104.2,301]` = **98.96×40**（三个页签各 +108.97：`114.2` / `223.2` 逐条对上）；
                //   子件 TMP `Text` **与底板同一矩形**、`Center/Middle`、`m_enableAutoSizing=1` + `[10,34]`。
                //   我们原来只画 TMP 文字（100×26 @ y271）⇒ **没有底板、字框小 14px、锚点也不同**。
                //   页面左缘 = `Cards [0.3]` + 5.0 = 5.3。
                for (int i = 0; i < 3; i++)
                {
                    float x1 = 5.3f + 108.9667f * i;
                    Check(_rt.UiTextureName("tab_nm" + i), "40k_main_bt_nametag",
                          $"`{tabWho[i]}` 名牌底板 = 原版那张 `40k_main_bt_nametag`");
                    if (!_rt.UiQuadRect("tab_nm" + i, out float ncx, out float ncy, out float nw, out float nh))
                    { Check(true, false, $"`tab_nm{i}` 量得到矩形（量不到 = 名牌根本没画）"); continue; }
                    CheckNear(nw, 98.96f, 0.6f, $"`{tabWho[i]}` 名牌宽 = 原版 **98.96**（`[5.3,104.2]`）");
                    CheckNear(nh, 40f, 0.6f, $"……高 = **40**（`[261,301]`）");
                    CheckNear(ncx, x1 + 98.96f * 0.5f, 0.6f, $"……中心 x = {x1:F1} + 98.96/2");
                    CheckNear(ncy, 281f, 0.6f, "……中心 y = 261 + 40/2");
                    // 那行字：**不许画出名牌框**（原版就是靠 `m_enableAutoSizing` 把它缩进去的）
                    float fpx = _rt.UiTabLabelFontPx(i);
                    CheckTrue(fpx > 0f,
                              $"`{tabWho[i]}` 名牌字号读得到（≤0 = TMP 没起来、走的是点阵兜底；实测 {fpx:F2}）");
                    // 🔴 **2026-10-04（A46）：字色 = 原版 `m_fontColor`，恒白。**
                    //   判据 = prefab 实读三颗 `Text`（Cards / Deck info / Cosmetics）的 `m_fontColor`
                    //   **都是 `(1,1,1,1)`**（`menu_dump.py … "Deck Editing Menu"` 那三行的 `色=(1,1,1,1)`）
                    //   ⇒ 选中与未选中**同色**，区分只靠身后那块红高亮。
                    //   ⚠️ 我们原来打的是 `Gold : Ink`（两个都是**自己挑的**）⇒ 这条会红。
                    var lc = _rt.UiTabLabelColor(i);
                    CheckTrue(Mathf.Abs(lc.r - 1f) < 1e-4f && Mathf.Abs(lc.g - 1f) < 1e-4f
                              && Mathf.Abs(lc.b - 1f) < 1e-4f && Mathf.Abs(lc.a - 1f) < 1e-4f,
                              $"`{tabWho[i]}` 名牌字色 = 原版 `m_fontColor` **(1,1,1,1) 纯白**"
                              + $"（实测 {lc.r:F3},{lc.g:F3},{lc.b:F3},{lc.a:F3}；"
                              + "原来打的是我们自己挑的 `Gold : Ink`）");
                    if (_rt.UiTabLabelRect(i, out float tcx, out float tcy, out float tw, out float th))
                    {
                        CheckTrue(tw <= 98.96f + 0.6f && th <= 40f + 0.6f,
                                  $"`{tabWho[i]}` 那行字**没画名牌框**：渲染 {tw:F1}×{th:F1} ≤ 框 98.96×40"
                                  + "（画出去 = 漏了自适应）");
                        // 🔴 **2026-10-04（A51 F3）：这条是把原来那条【同义反复】的区间断言换掉的。**
                        //   原来断的是 `10 ≤ fpx ≤ 34` —— 那两个数**正是我们从 `TabNameMinPx/MaxPx`
                        //   传进去的**，而 TMP 的自适应**构造上**就落在 `[fontSizeMin, fontSizeMax]` 内
                        //   （`TextMeshPro.cs:3567-3580` 缩、`:4139-4149` 涨，两头都夹在这个区间）
                        //   ⇒ 把 max 改成 20、甚至把 `SetAutoFitBox` 整句删掉，那条**照样绿**。
                        //   ⇒ 换成**有区分力**的判据：TMP 只在「大一号就装不下」时才缩
                        //   （二分收敛到 **0.05 步长**，`TextMeshPro.cs:4149`；本工程 1 fontSize ≈ 10.24px
                        //     ⇒ 步长 ≈ **0.51px**）⇒ 结果只有两种可能：
                        //     ① 停在**原版上限 34**（判据 = 原版 `m_fontSizeMax = 34`；= 框根本没压它），或
                        //     ② 被压到**框的边界**上：宽贴住 **98.96** 或高贴住 **40**（= 原版 `Label` 的框）。
                        //   把 max 改成 20（或任何「小于框容得下的值」）⇒ 两个分支都不成立 ⇒ **真红**。
                        //   ⚠️ 容差取**比例**（94%）而不是像素等号：收敛点最多比边界低「一步」——
                        //      宽 ∝ 字号 ⇒ 低 0.51/fpx（fpx≈20 时 ≈ 2.5px）；高 ∝ 字号×行数 ⇒ 低 ≈ 0.51×行数
                        //      ⇒ 1~2.5px 的不确定度；而「字号被设成 20」那类错会让文本只占框的 ~50%(宽)/~72%(高)，
                        //      94% 这条线离两种情形都远。
                        const float FitFrac = 0.94f;
                        bool atMax = fpx >= 34f - 0.1f;
                        bool fillsW = tw >= 98.96f * FitFrac;
                        bool fillsH = th >= 40f * FitFrac;
                        CheckTrue(atMax || fillsW || fillsH,
                                  $"`{tabWho[i]}` 字号 = **原版上限 34**（框没压住它）**或**被压到**框的边界**上"
                                  + "（宽 98.96 / 高 40 的 ≥94%）—— 二者必居其一（TMP 只在『大一号就装不下』时才缩）；"
                                  + $"实测 字号 {fpx:F2} · 渲染 {tw:F1}×{th:F1}");
                        CheckTrue(Mathf.Abs(tcx - ncx) < 2f && Mathf.Abs(tcy - ncy) < 2f,
                                  $"……而且**居中**在名牌上（原版 `m_HorizontalAlignment=2` / `Middle`；"
                                  + $"字心 {tcx:F1},{tcy:F1} vs 名牌心 {ncx:F1},{ncy:F1}）");
                    }
                    else Check(true, false, $"`tab_tx{i}` 量得到渲染矩形");
                }
                // 🔴 **2026-10-04（A46）**：再加一条**「选中态与未选中态字色完全一样」** ——
                //   原版三颗 `Text` 的 `m_fontColor` 是**同一个值**（`(1,1,1,1)`），选中与否只改身后那块红高亮。
                //   此刻选中的是 `Cards`（上面那一段收尾 `UiSetTab(0)`）⇒ 正好一比一。
                //   （原来 `Gold : Ink` 那种「用字色区分选中」的写法会让这条红。）
                {
                    var cSel = _rt.UiTabLabelColor(0);      // 选中
                    var cUn = _rt.UiTabLabelColor(1);       // 未选中
                    CheckTrue(Mathf.Abs(cSel.r - cUn.r) < 1e-4f && Mathf.Abs(cSel.g - cUn.g) < 1e-4f
                              && Mathf.Abs(cSel.b - cUn.b) < 1e-4f && Mathf.Abs(cSel.a - cUn.a) < 1e-4f,
                              "选中的 `Cards` 与未选中的 `Deck info` **字色一模一样**"
                              + "（原版三颗 `m_fontColor` 同值、区分只靠红高亮）"
                              + $"（实测 选中 {cSel.r:F3},{cSel.g:F3},{cSel.b:F3} vs 未选中 {cUn.r:F3},{cUn.g:F3},{cUn.b:F3}）");
                }

                // ③ 筛选栏那三个开关：`40_main_bt_toggle_on` ↔ `40_main_bt_toggle_off`
                _rt.UiToggleFilters();
                Check(_rt.UiFilterCellTex("$owned"), "40_main_bt_toggle_on",
                      "`Owned only`（出厂开）画的是 `40_main_bt_toggle_on`");
                Check(_rt.UiFilterCellTex("$upgradable"), "40_main_bt_toggle_off",
                      "`Upgradable only`（出厂关）画的是 `40_main_bt_toggle_off`（原来**恒画 on 那张**）");
                bool gotOwn = _rt.UiFilterCell("$owned", out float oX, out float oY,
                                               out float oW, out float oH, out bool oOn);
                CheckTrue(gotOwn, "（前提）`$owned` 那一格量得到");
                CheckTrue(oOn, "（前提）`$owned` 出厂是【开】（原版 `m_IsOn=1`）");
                if (gotOwn)
                {
                    _rt.UiClickPx(oX, oY);                                   // 走鼠标那条路点这一格
                    Check(_rt.State.Filter.Owned, false, "（前提）点一下 ⇒ `Owned` 关掉");
                    Check(_rt.UiFilterCellTex("$owned"), "40_main_bt_toggle_off",
                          "关掉 ⇒ **换成 off 那张**（原来「从不换出来」，关掉的与打开的长得一模一样）");
                    _rt.UiClickPx(oX, oY);
                    Check(_rt.State.Filter.Owned, true, "（收尾）再点一下 ⇒ 回到出厂的【开】");
                    Check(_rt.UiFilterCellTex("$owned"), "40_main_bt_toggle_on", "开回去 ⇒ 换回 on 那张");
                }
                // 卡背页那颗（`Cosmetic FIlter > Filters > Owned Toggle`，出厂也是开）
                _rt.UiToggleFilters();                                       // 收起卡牌那套
                _rt.UiSetTab(2);
                _rt.UiToggleFilters();                                       // 开卡背那套（按 `_tab` 分派）
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt_pressed",
                      "卡背页开抽屉 ⇒ 页头 `Filters` **也**变成按下的图"
                      + "（两页的 `filterToggle` 在 prefab 里就是同一颗）");
                Check(_rt.UiCosmoFilterCellTex("$owned"), "40_main_bt_toggle_on",
                      "卡背页 `Owned only`（出厂开）画的是 `40_main_bt_toggle_on`");
                bool gotCos = _rt.UiCosmoFilterCell("$owned", out float cX, out float cY,
                                                    out float cW, out float cH, out bool cOn);
                CheckTrue(gotCos, "（前提）卡背抽屉里那一格量得到");
                CheckTrue(cOn, "（前提）卡背页那个 `Owned only` 出厂也是【开】");
                if (gotCos)
                {
                    _rt.UiClickPx(cX, cY);
                    Check(_rt.UiCosmoFilterCellTex("$owned"), "40_main_bt_toggle_off",
                          "卡背页关掉它 ⇒ 也换成 `40_main_bt_toggle_off`");
                    _rt.UiClickPx(cX, cY);
                    Check(_rt.UiCosmoFilterCellTex("$owned"), "40_main_bt_toggle_on", "（收尾）换回 on 那张");
                }
                _rt.UiToggleFilters();                                       // 收起卡背抽屉
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt",
                      "卡背抽屉收起来 ⇒ 页头那颗换回 `40k_menu_bt`");
                _rt.UiSetTab(0);
                Check(_rt.FiltersOpen, false, "（收尾）抽屉关着、回到 Cards 页签");
            }

            Section("卡组行的悬停色（原版 `m_HighlightedColor = 0.6887`）");
            {
                // 判据 = 原 prefab：`Deck Selector {Card Info button, Defensive Card Slot}` 的
                //   `m_Transition=1` + `m_TargetGraphic` = 子件 `Background`（图 `40k_deck_cardlist_bg`）
                //   + `m_HighlightedColor = (0.6886792,…)`；而 `Deck Selector Hero Card Info button`
                //   的目标图 `m_Color.a = 0`（全透明）⇒ **督军行悬停看不到变化**。
                var entries = _rt.DeckEntries();
                int rowCard = -1;
                for (int i = 0; i < entries.Count && i < 11; i++)
                    if (entries[i].Type != "hero") { rowCard = i; break; }
                CheckTrue(rowCard >= 0, "（前提）卡组里有一行**非督军**（才验得到「悬停变暗」）");
                if (rowCard >= 0 && _rt.UiRowRect(rowCard, out float rx, out float ry, out float rw, out float rh))
                {
                    // 悬停前先记下**别的几件**的颜色，验「只动目标那一件」（UGUI `ColorTint` 的语义）
                    var gradT = _root.Find("row_g" + rowCard);
                    var grad = gradT != null ? gradT.GetComponent<ImageQuad>() : null;
                    Color gradBefore = grad != null ? grad.Tint : Color.white;
                    var borderT = _root.Find("row_b" + rowCard);
                    var border = borderT != null ? borderT.GetComponentInChildren<ImageQuad>(true) : null;
                    Color borderBefore = border != null ? border.Tint : Color.white;

                    Check(_rt.UiRowHoverAt(rx + rw * 0.5f, ry + rh * 0.5f), rowCard,
                          $"指针压在第 {rowCard} 行（非督军）上 —— `UiRowHoverAt` 走的是鼠标那条路");
                    // 🔴 **2026-10-04（A37 ③）改**：这里原来写的是 `DeckRuntime.RowHoverK` —— **自证**
                    //   （期望值取我们自己的常量 ⇒ 把那常量改成错值 `0.9608` 照样绿）。
                    //   现在写死**原版字面量**：prefab `Deck Selector Defensive Card Slot` /
                    //   `Deck Selector Card Info button` 的 `m_Colors.m_HighlightedColor = 0.6886792182922363`
                    //   （实读 `python 工具/_probe_deckinfo.py bundle_menus_assets_all "Deck Selector Defensive Card Slot"`）。
                    CheckNear(_rt.UiRowBgTint(rowCard), 0.6886792f, 1e-3f,
                              "非督军行悬停 ⇒ 行底 **×0.6886792**（原版 `m_HighlightedColor` 字面量；"
                              + "不是全库默认的 0.9608）");
                    if (grad != null)
                        CheckTrue(grad.Tint == gradBefore,
                                  "悬停**只动行底那一件**：稀有度色条不变色（原版 `targetGraphic` = 子件 `Background`）");
                    if (border != null)
                        CheckTrue(border.Tint == borderBefore,
                                  "悬停**只动行底那一件**：行描边不变色（同上）");
                    _rt.UiRowHoverAt(1700f, 500f);                            // 指针移开
                    CheckNear(_rt.UiRowBgTint(rowCard), 1f, 1e-4f, "指针移开 ⇒ 行底还原成白（×1）");
                }
                if (_rt.UiRowRect(0, out float zx, out float zy, out float zw, out float zh)
                    && entries.Count > 0 && entries[0].Type == "hero")
                {
                    _rt.UiRowHoverAt(zx + zw * 0.5f, zy + zh * 0.5f);
                    CheckNear(_rt.UiRowBgTint(0), 1f, 1e-4f,
                              "**督军行**悬停**不上色**（原版那颗 `Deck Selector Hero Card Info button` 的"
                              + "目标图 `m_Color.a = 0` ⇒ 原版自己也看不见变化）");
                    _rt.UiRowHoverAt(1700f, 500f);
                }
            }

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

                // ---- 🆕 2026-10-03（A20）：**13 个阵营的默认卡背全都在** ----
                //  判据 = 原版 `DefaultCarbackByArmySO`（**13 条、没有 Neutral**）—— 它不在任何 bundle/导出里，
                //  读法 → `工具/read_default_cardbacks.py`（原始字节手工切）；表 → `Resources/Cardbacks.json` 的 `defaults`。
                Check(CardbackTable.DefaultCount, 13, "默认卡背表 **13 个阵营**（原版那 SO 就是 13 条、没有 Neutral）");
                {
                    var facs = new[] { "Ultramarines", "Goff", "SaimHann", "Sautekh", "BlackLegion", "Leviathan",
                                       "TauEmpire", "Sororitas", "Genestealers", "AstraMilitarum", "DarkAngels",
                                       "EmperorsChildren", "SpaceWolves" };
                    int ok = 0;
                    var bad = new System.Text.StringBuilder();
                    foreach (var f in facs)
                    {
                        var nm = CardbackTable.DefaultFor(f);
                        var t = CardArt.CardBack(f);
                        bool sdf = CardArt.CardBackSdf(f) != null;
                        if (!string.IsNullOrEmpty(nm) && t != null && sdf) ok++;
                        else bad.Append(f).Append("（").Append(string.IsNullOrEmpty(nm) ? "无表项" : nm)
                            .Append(t == null ? " · 图取不到" : "").Append(sdf ? "" : " · SDF 取不到").Append("）");
                    }
                    Check(ok, facs.Length,
                          "13 个阵营**都能取到默认卡背的图 + SDF**（缺的：" + (bad.Length == 0 ? "无" : bad.ToString()) + "）");
                    CheckTrue(CardbackTable.DefaultFor("Neutral") == null, "`Neutral` **没有**默认卡背（原版那表就没有它）");
                }
                // 🔴 **2026-10-03 就地更正（A20）**：这里原来断「本卡组没有督军 ⇒ 抽屉不画卡背」——
                //    **那句的前提是错的**：跑到这里卡组**已经有督军了**（上面 `SetWarlord` 那段），
                //    原来之所以量到「<无>」，是因为那个阵营的 `Art/cards/back_*.png` **压根没有那张图**
                //    （只有 4 个阵营有）⇒ **它断的其实是「图缺」、不是「不该画」**。
                //    A20 起 13 个阵营都有默认卡背 ⇒ **该画**，而且画的必须是**这副牌阵营**那张。
                {
                    var wlFac = _rt.FactionOf(_rt.State.Deck.WarlordId);
                    Check(_rt.CosmeticDrawerTex, CardbackTable.DefaultFor(wlFac),
                          "抽屉画的是**这副牌阵营的默认卡背**（原版 `ArmyUtilities.GetDefaultCardback(army)`；阵营 = "
                          + (string.IsNullOrEmpty(wlFac) ? "<空>" : wlFac) + "）");
                }
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

            // ============================================================ 🆕 2026-10-01
            //  卡背页那个左抽屉（原版 `Cosmetic FIlter` → `Army Filter` + `Owned Toggle`）。
            //  判据 = **实读那棵树**（`python 工具/menu_rect.py …bundle_menus_assets_all "Cosmetic FIlter" --depth 5`，
            //  命中两个同名节点、取 `-372539790263455964` 那个 = 卡组编辑这棵树）
            //  ＋ 原版 SO 的 `cardArmy`（表 → `Core/CardbackTable.cs`，生成 → `工具/gen_cardbacks.py`）。
            //  ⚠️ 在这之前，点这一页的 `Filters` 只会**如实出声「还没建」**。
            {
                Check(_rt.CosmoFiltersOpen, false, "卡背抽屉**出厂是关的**（原版 `Cosmetic FIlter` 是 INACT）");
                _rt.UiSetTab(2);
                _rt.UiClickPx(392.2f, 113.5f);                    // 页头那颗 `Filters`
                Check(_rt.CosmoFiltersOpen, true, "……点 `Filters` ⇒ **开的是卡背自己那个抽屉**（不再是「还没建」）");
                Check(_rt.FiltersOpen, false, "……而且**没有**把卡牌那七行端上来（两套是两棵 prefab）");
                Check(_rt.UiCosmoFilterCellCount, 14,
                      $"抽屉里 **14 格** = 13 个阵营 + 1 个 `Owned only`（实 {_rt.UiCosmoFilterCellCount}）");
                Shoot("deck_cosmo_filters.png");        // 实拍证据：抽屉开着的样子（改版面必看截图）
                {
                    // ⚠️ 用**第一个阵营**（`State.Factions()` 是**字典序**的）—— 第一版这里写死了 `Ultramarines`，
                    //    而它排第 13 ⇒ 落在第 5 行（+400 px）⇒ **断言自己错了**（自检当场抓到，已改）。
                    string fac0 = _rt.State.Factions()[0];
                    float x, y, w, h, wy; bool on, won;
                    CheckTrue(_rt.UiCosmoFilterCell("$fac:" + fac0, out x, out y, out w, out h, out on),
                              $"找得到阵营格 `$fac:{fac0}`");
                    CheckTrue(_rt.UiCosmoFilterCell("$owned", out w, out wy, out w, out h, out won),
                              "找得到 `$owned`（Owned only）那一格");
                    CheckTrue(wy > y + 100f,
                              $"`Owned` 在 Army 行**下面**（y {y:F0} → {wy:F0}）—— 原版这两行的**行序与卡牌那套相反**");
                    CheckTrue(won, "`Owned only` **出厂就是开的**（原版 `showOnlyOwnedCards` 初值 true + prefab `m_IsOn=1`）");

                    float cx, cy, cw, ch; bool con;
                    _rt.UiCosmoFilterCell("$fac:" + fac0, out cx, out cy, out cw, out ch, out con);
                    Check(cw, 100f, "阵营格 **100×100**（原版那棵树里 `Toggle` 模板的实测值）");
                    CheckTrue(Mathf.Abs(cy - (155.97f + 15f + 50f + 50f)) < 0.5f,
                          $"**第一个**阵营格中心 y = 抽屉顶 + 15 + 50 + 50 = {155.97f + 15f + 50f + 50f:F2}（实 {cy:F2}）"
                        + "—— Spacing 15 → Army `Title` 50 → `Content` 起");
                }

                // ---- 真的筛了没有（判据与铺格**同一份** `CardbackTable`）----
                string fac1 = _rt.State.Factions()[1];
                int allN = _rt.UiCosmoShownCount;
                _rt.UiCosmoFilterRow("$fac:" + fac1);
                int umN = _rt.UiCosmoShownCount;
                Check(_rt.CosmoFilterArmy, fac1, "点一个阵营 ⇒ 条件记下来了");
                CheckTrue(umN > 0 && umN < allN, $"…铺出来的张数真的变了（{allN} → {umN}）");
                Check(CardbackTable.ArmyOf(_rt.CosmoCellTex(0)), fac1,
                      $"……筛完第 1 格确实是那个阵营的卡背（`{_rt.CosmoCellTex(0)}`）");
                Check(_rt.State.Filter.Faction, "", "……而且**没污染卡池**那套筛选条件（两套分开，原版是两棵 prefab）");
                _rt.UiCosmoFilterRow("$owned");
                Check(_rt.CosmoFilterOwned, false, "`Owned only` 能切换（原版 `ToggleShowOwnedCards`）");
                Check(_rt.UiCosmoShownCount, umN,
                      "……但**张数不变** —— 单机全解锁，这个开关不改变结果（**如实标**，不是静默失效）");
                _rt.UiCosmoFilterRow("$owned");
                _rt.UiCosmoFilterRow("$fac:" + fac1);            // 再点一次 = 取消
                Check(_rt.CosmoFilterArmy, "", "阵营格**再点一次 = 取消**（与卡牌那套同一条手感）");
                Check(_rt.UiCosmoShownCount, allN, "……张数回到全量");

                // ---- 表本身：**每一张本地卡背都查得到阵营**（生成脚本对账过 243 SO → 233 图名；这条防手改坏）----
                var allCb = CardArt.CosmeticNames();
                int noArmy = 0;
                for (int i = 0; i < allCb.Length; i++)
                    if (string.IsNullOrEmpty(CardbackTable.ArmyOf(allCb[i]))) noArmy++;
                Check(noArmy, 0, $"**张张卡背都查得到阵营**（查不到 {noArmy} 张；表 = `Resources/Cardbacks.json`）");
                Check(CardbackTable.Count, allCb.Length,
                      $"表里条数 = 本地卡背张数（{CardbackTable.Count} vs {allCb.Length}）");
                // 🆕 2026-10-04（A67）：卡背抽屉走**同一套滑动引擎**；而且它多了「**页在不在**」那一半 ——
                //   切走再回来：**逻辑态保留**、画面跟着页瞬时开关、**不重放那 0.3 秒**。
                //   ⚠️ 「切页签不放动画」是**我们挑的**口径（原版 `OnEnable → ToggleFilters(bool)` 回来时
                //   会不会重跑 tween 判不出来 —— 泛型方法体缺失，见 `DeckRuntime` 的 §A67 那段）。
                CheckNear(_rt.CosmoFilterDrawerSlide, 1f, 0.001f, "卡背抽屉：点开后进度 = **1**（也是滑进来的）");
                CheckTrue(_rt.CosmoFilterDrawerInteractive, "…到位 ⇒ 参与命中");
                _rt.UiSetTab(0);
                Check(_rt.CosmoFiltersOpen, true, "切到 Cards 页 ⇒ 卡背抽屉的**逻辑态保留**");
                CheckTrue(!_rt.UiQuadActive("cosmoflt_bg"), "…但整栏**不露**（原版它挂在那张页底下）");
                CheckTrue(!_rt.CosmoFilterDrawerInteractive, "…也不参与命中");
                _rt.UiSetTab(2);
                CheckTrue(_rt.UiQuadActive("cosmoflt_bg"), "切回 Cosmetics ⇒ 又露出来了");
                CheckNear(_rt.CosmoFilterDrawerSlide, 1f, 0.001f, "…进度还是 **1**（页的开关不重放那 0.3 秒）");
                _rt.UiToggleFilters();                            // 关掉，别影响后面
                Check(_rt.CosmoFiltersOpen, false, "（抽屉关回去）");
                CheckNear(_rt.CosmoFilterDrawerSlide, 0f, 0.001f, "…也是**滑出去**的（A67）");
                CheckTrue(!_rt.CosmoFilterDrawerInteractive, "…关着就不参与命中");
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
