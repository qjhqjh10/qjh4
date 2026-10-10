// ============================================================================
//  「单卡渲染」量尺 —— 把卡池里指定的几张卡按**对局里那一条代码路径**渲染成 PNG。
//
// 为什么要它：卡面组装对不对，**截图看不出细节、自检数字也看不出「像不像」** ——
// 唯一靠得住的尺子是**跟原版拼好的卡图并排比**：
//   `D:/2/Warpforge部队卡片/<阵营>/<类别>/…png`（原版 Print&Play 卡图，就是成品卡面）。
// 这个探针把我们的卡按同一张卡渲成同尺寸的图，放进 `_tmp_view/cardface/`，肉眼一比就知道差在哪。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod CardFaceProbe.Run -logFile -
//
// ⚠️ 卡面数据走 `BattleDriver.ToCardData` / `FaceText`（**探针不另写一份**，否则两边迟早不一致）。
// ============================================================================
using System.IO;
using UnityEngine;
using TMPro;
using RuleEngine;
using CardPresentation;

public static class CardFaceProbe
{
    const string OutDir = "d:/4/_tmp_view/cardface";
    // 渲染尺寸：跟着卡本体宽高比（2.0927 : 3.3313 = 0.628），留一点边给卡框
    const int W = 700, H = 1115;

    /// <summary>要渲哪几张（英文名 = 卡池里的 `name`）。
    /// 2026-09-13 扩成**按稀有度/类型覆盖面**挑** —— 用户要求核对「稀有度宝石、数值的位置」与
    /// 「插图有没有白边/黑边/超出卡框」，只渲一两种看不出问题（宝石是**按稀有度取色**的，
    /// 战术卡没有数值格，督军卡有「角色越出卡框」）。</summary>
    static readonly string[] Names =
    {
        "Heavy Intercessor",     // 单位卡（有兵种行、有数值）
        "Aggressor Sergeant",    // 手牌里看着碎的那张（对照它是卡本身的问题还是缩小导致的）
        "Roboute Guilliman",     // 督军 —— **角色越出卡框**最明显的一张（对照原版成品卡）
        "Armoured Offensive",    // 战术卡（没有抠图 → 只有一层）
        "Pariah Vanguard",       // 用户点名要看的那张
        "Canoptek Scarab",       // Sautekh common 单位（小费用、有护甲？）
        "Lychguard",             // Sautekh epic 单位
        "stormlord",             // Sautekh legendary 督军「风暴王」（用户点名问的）
        "Awakened Dynasty",      // Sautekh common 战术卡
        "Lord of the Storm",     // Sautekh legendary 战术卡
        "Da Old Ways",           // Goff rare 战术卡
        "Beast Snagga Boy",      // Goff rare 单位
        // ── 2026-09-15 加：**卡面图标**的三种记号各来一张（判据见 `资料/卡面图标_现状与缺口.md` §二之五）
        "Armorium Cherub",       // 修女会（`SOR67`）—— 行首 `☀`（符号**吃掉**，画成信仰图标）
        "Farseer",               // 灵族（`ASH_Farseer`）—— `①`（灵魂石档位，数字烘在图里 ⇒ 连数字吃掉）
        // ── 2026-09-15 加：**这一轮补立绘的那批**（原来 21 张配不上图，工具改完只剩 `Moment of Grace`）
        //    判据：`资料/PnP卡图_逐张对账_0915.md` §五 / §六。名字都**唯一**，不会撞同名跨阵营的卡。
        "Dark Pact of Excess",   // `BL20` —— 原版叫 `Mark of Chaos_Slaanesh`（改名，按 id 点名）
        "Chosen of the Four",    // `BL2`  —— 原版叫 `Warmaster`
        "Lord Kaphrael",         // `EC1`  —— 原版叫 `Lord Exultant`
        "Veldras the Sublime",   // `EC24` —— 原版叫 `Threnodic Choir Flawless`（**前缀陷阱**那张）
        "Undying Legions",       // `SAU61`—— 原版叫 `Annihilation Command`
        "Awakened Obelisk",      // `SAU65`—— 与重复行 `SAU_Awakening_Obelisk` **共用一张图**
        "Alien Idol",            // `GSC66`—— 防御卡，验「`defence_` 变体没被配成 `strat_`」
        "Moment of Grace",       // `SOR47`—— 原版旧名是 `Righteous Repugnance`（**错开一位**那张，用户点的）
        "Righteous Repugnance",  // `SOR6` —— 它该拿的是 `Purgator Mirabilis` 那张图（金色赎罪引擎）
        // ── 2026-09-19 加：**用户点名要并排比的那张**（PnP 有 `Aeldari/3部队/Warpforge_06_Howling-Banshee-Exarch.png`）
        //    并排比出来的问题：我们把「文字底板」做成了下半截一块**不透明白/黑板**，而 PnP 是
        //    **立绘铺满整张卡、文字直接压在立绘上**。见 `资料/PnP卡图_逐张对账_0915.md` 与本次的更正。
        "Howling Banshee Exarch",// `ASH79`
        // ── 2026-09-21 加：**卡面「关键词段」带图标**的验收尺子（铁律 10 第 6 条）。
        //    这张卡的 `desc` 是 `Rally: Stun an enemy` —— **`Waystone` / `Flank` 两个字在正文里
        //    一个字都没有**，它们只能从「关键词段」那一条路印出来。PnP 成品卡面印的是
        //    `◈Waystone.  ⬇Flank.`（`Aeldari/3部队/Warpforge_20_Howling-Banshee.png`，已并排核过）
        //    ⇒ 这张是「关键词段有没有画出来」最灵敏的一张。**名字与 Exarch 是两张卡，别混**。
        "Howling Banshee",       // `ASH20`
        // ── 🆕 2026-10-21 加：**铁律 10 第 6 条那条「并排比」欠着的 9 张**（合并规格 #88）──
        //    两批各自留了一处「判据已核、只差并排渲一张」的缺口，原报告都点名「这 `.cs` 不在我白名单 ⇒ 没加」：
        //      · `W5e` 那 4 张（`A1347` 修完 `oath` 徽记的**位置与数字**之后没人渲过）——
        //        卡面一律是 `〔徽记〕Oath N: …`（记号的**位置**与 `N` 都要逐格对）；
        //        判据 = `资料/普查产出_第十三会话/W5e_A1347徽记回归.md` §四·2（它点名要加 `Devastator Marine`）。
        //      · `W7` 那 5 张（`A1363` 给**非首句**的 `N ☀:` 补上了信仰图标）——
        //        要核「`☀` 有没有画在数字之后、句尾有没有多出/漏掉句号」；
        //        判据 = `资料/普查产出_第十三会话/W7_A1363与A1364.md` §五·3（「并排渲一张」那一栏）。
        //    ⚠️ PnP 成品卡图在 `d:/2/Warpforge部队卡片/{Ultramarines,Sorotitas}/…`（900×1200）——
        //      它只能当**内容**的尺子（卡名 / 费用 / 攻血甲 / 效果文字 / 记号），
        //      **版式与显隐规则不许当尺子**（铁律 10 第 3 条：PnP 是印刷品，不是数字版规格）。
        "Devastator Marine",     // `UM80` —— `W5e`① 卡面：`〔紫尖刺〕Blast 2. 〔蓝圆盘+白袍人形〕Oath 4: Deal 5 damage to an enemy troop`
        "Phobos Librarian",      // `UM81` —— `W5e`② 卡面：`〔兜帽〕Camouflage. 〔同一枚〕Oath 3: Deal 2-4 damage to an enemy. If target dies, gain 〔盾〕Shield`
        "Scout Sniper",          // `UM_Scout_Sniper` —— `W5e`③ 卡面：`〔兜帽〕Camouflage. 〔准星〕Sniper. 〔同一枚〕Oath 1: Deal 1 damage`
        "Firestrike Servo Turret", // `UM74` —— `W5e`④ 卡面：`〔导弹〕Long Range. 〔哨戒〕Sentry 2. 〔同一枚〕Oath 3: Gain 〔哨戒〕Sentry 2`
        "Dominion Superior",     // `SOR28` —— `W7`① 卡面：`⚡Rally: Deal 1 damage.` ／ `2 ⟨金太阳⟩: Deal 1 additional damage`
        "Devout Warriors",       // `SOR43` —— `W7`② 卡面：`Draw a troop.` ／ `2 ⟨金太阳⟩: Draw an additional troop`
        "Beacon of Faith",       // `SOR44` —— `W7`③ 卡面：`4 ⟨金太阳⟩: …`（**图标与冒号之间没有空格**）／ `7 ⟨金太阳⟩ : …`（**有空格、句尾无句号**）
        "Trial of Suffering",    // `SOR49` —— `W7`④ 卡面：`2 ⟨金太阳⟩: Give it ⟨银盾⟩Armour 2 instead`
        "Imperial Creed",        // `SOR59` —— `W7`⑤ 卡面：`⟨螺旋⟩Stun three random enemies.` ／ `4 ⟨金太阳⟩: Refill 3 Energy`
    };

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        var pool = CardDatabase.Load();
        Debug.Log($"[cardface] 卡池 {pool.Count} 张，开始渲染 {Names.Length} 张 → {OutDir}");

        foreach (var name in Names)
        {
            var def = FindByName(pool, name);
            if (def == null) { Debug.LogError($"[cardface] 卡池里没有 `{name}`"); continue; }

            var data = BattleDriver.ToCardData(def, def.Faction);
            // 把**运行时真正加载到的那张贴图** dump 出来 —— 排查「卡面渲出来和磁盘上的 PNG 不一样」时，
            // 这是唯一的决定性证据（截图里看不出是贴图的问题还是采样/压缩的问题）
            {
                var tex = CardArt.Portrait(BattleDriver.ArtKey(def));   // ⚠️ 立绘按 id 取名（自造的 26 张按卡名）
                if (tex != null)
                {
                    File.WriteAllBytes(Path.Combine(OutDir, SafeName(name) + "_tex.png"), tex.EncodeToPNG());
                    Debug.Log($"[cardface] 贴图 {name}: {tex.width}×{tex.height} format={tex.format} mip={tex.mipmapCount}");
                }
            }
            var root = new GameObject("probe_" + name);
            var view = CardView.Create(root.transform, data, name);
            // 卡的 z 都是 0 附近，相机放远一点正对着拍
            Shot(view, Path.Combine(OutDir, SafeName(name) + ".png"));

            // 诊断：再渲一张**只有底层**（关掉前景抠图层）——分层看杂色到底谁带来的
            {
                CardView.DebugNoArtFront = true;
                var root2 = new GameObject("probe_nofront_" + name);
                var v2 = CardView.Create(root2.transform, data, name + "_nofront");
                Shot(v2, Path.Combine(OutDir, SafeName(name) + "_nofront.png"));
                Object.DestroyImmediate(root2);
                CardView.DebugNoArtFront = false;
            }
            // 再渲一张**不可打出**态：手牌上费用不够的卡就是这个状态，
            // 用来确认「卡片四周那圈白边」到底是不是状态描边（`_rim`）
            view.SetHighlight(CardHighlightState.Unplayable);
            Shot(view, Path.Combine(OutDir, SafeName(name) + "_unplayable.png"));
            // 选中态：描边**该**出现（羽化后应该是软光，不是硬方框）
            view.SetHighlight(CardHighlightState.Selected);
            Shot(view, Path.Combine(OutDir, SafeName(name) + "_selected.png"));
            view.SetHighlight(CardHighlightState.Normal);

            var uv = view.FrameUvRect;
            Debug.Log($"[cardface] {name} 卡本体 {CardView.Width:F4}×{CardView.Height:F4}  "
                    + $"卡框实绘 {view.FrameDrawSize.x:F4}×{view.FrameDrawSize.y:F4}  "
                    + $"卡框 UV x[{uv.xMin:F3},{uv.xMax:F3}] y[{uv.yMin:F3},{uv.yMax:F3}]  "
                    + $"立绘实绘 {view.ArtDrawSize.x:F4}×{view.ArtDrawSize.y:F4}");

            foreach (var t in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                t.ForceMeshUpdate();
                Debug.Log($"[cardface] {name}/{t.name}  fontSize={t.fontSize:F3}  "
                        + $"rect宽={t.rectTransform.sizeDelta.x:F3}  "
                        + $"bounds={t.textBounds.size.x:F3}x{t.textBounds.size.y:F3}  "
                        + $"lines={t.textInfo.lineCount}  "
                        + $"vAlign={(int)t.verticalAlignment}  "     // 🔴 A848：原版卡名那层是 Midline(4096)
                        + $"inkCenterY={InkCenterY(t):F4}  boxCenterY={t.textBounds.center.y:F4}  "
                        + $"text='{Trim(t.text)}'");
            }
            Object.DestroyImmediate(root);

            // 手牌 vs 场上：同一张卡按两种**真实尺寸**各渲一张 ——
            // 用来量「数值 / 稀有度宝石会不会跟着卡的大小偏移」。卡本体在 scale=1 时是
            // 2.0927 世界单位（= 226 px @108 px/单位），手牌 165 px / 场上 137.2 px（出处
            // `资料/对战排版_原版数值与改造方案.md`）→ 缩放 0.7301 / 0.6071。
            // ⚠️ 结构上不可能偏（`SetPose` 只改 `localScale`，所有图层都是卡单位坐标），
            //    但用户点名要查，就**渲出来量**：两张图缩到同尺寸比像素差。
            // ⚠️ **必须在 `DestroyImmediate(root)` 之后** —— 第一版放在前面，
            //    主视图还立在场景里，于是两张卡**叠在一张图上**（重影）。
            if (name == "Heavy Intercessor")
            {
                var scales = new float[] { 0.7301f, 0.6071f };
                var tags = new string[] { "hand", "board" };
                for (int i = 0; i < scales.Length; i++)
                {
                    var r2 = new GameObject("probe_" + tags[i]);
                    var v2 = CardView.Create(r2.transform, data, name + "_" + tags[i]);
                    v2.SetPose(Vector3.zero, 0f, scales[i]);
                    Shot(v2, Path.Combine(OutDir, SafeName(name) + "_" + tags[i] + ".png"));
                    Object.DestroyImmediate(r2);
                }

                // 场上形态（`CardFace.Board`，尺寸取场上那档）—— 原版 `inPlay` 时把**整张 `2DCard` 关掉**，
                // 只剩立绘 + 攻/血/甲（+ 7 槽徽标；徽标要事件驱动，探针里没有）。
                // 2026-09-19 加：三条错修完之后，**「场上」这一档还没有肉眼图**、只有断言
                //（`BattleScene.cs` 那 5 条）—— 铁律 10 第 6 条要求「至少并排渲一次」，这条补上。
                {
                    var r3 = new GameObject("probe_inplay");
                    var v3 = CardView.Create(r3.transform, data, name + "_inplay");
                    v3.SetFace(CardFace.Board);
                    v3.SetPose(Vector3.zero, 0f, 0.6071f);
                    Shot(v3, Path.Combine(OutDir, SafeName(name) + "_inplay.png"));
                    Object.DestroyImmediate(r3);
                }
            }
        }
        Debug.Log("[cardface] 结束");
    }

    /// <summary>全池渲染 —— 「逐张并排验收」的输入清单（2026-09-15）。
    ///
    /// 为什么单开一个入口：`Run()` 的 `Names[]` 是**为了排查某个具体问题手挑的 26 张**，
    /// 而「逐张并排验收」要的是**全池 1127 张**（抽查看不出阵营级/系统性缺陷 ——
    /// 阵营行印成 `BLACKLEGION` 就是这么漏到验收阶段的）。
    ///
    /// 输出：`_tmp_view/cardface_all/&lt;净化后的卡id>.png` + `_manifest.tsv`
    /// （`id / faction / name / file` —— 下游 python 拼版**只认这份清单**，不靠文件名反推）。
    /// ⚠️ **只渲主视图**：`_tex`/`_nofront`/`_selected`/`_unplayable` 那四张是排查用的，全池跑太贵。
    /// ⚠️ 走的是 `BattleDriver.ToCardData` + `CardView.Create`，**与对局同一条代码路径**。
    /// </summary>
    public static void RunAll()
    {
        const string OutAll = "d:/4/_tmp_view/cardface_all";
        Directory.CreateDirectory(OutAll);
        var pool = CardDatabase.Load();
        var manifest = new System.Text.StringBuilder("id\tfaction\tname\tfile\n");
        int n = 0, fail = 0;
        Debug.Log($"[cardface-all] 卡池 {pool.Count} 张 → {OutAll}");

        foreach (var def in pool)
        {
            string file = SafeName(def.Id) + ".png";
            try
            {
                var data = BattleDriver.ToCardData(def, def.Faction);
                var root = new GameObject("probe_all_" + def.Id);
                var view = CardView.Create(root.transform, data, def.Id);
                Shot(view, Path.Combine(OutAll, file), quiet: true);
                Object.DestroyImmediate(root);
                manifest.Append(def.Id).Append('\t').Append(def.Faction).Append('\t')
                        .Append(def.Name).Append('\t').Append(file).Append('\n');
                n++;
            }
            catch (System.Exception e)
            {
                // 不许静默失败：渲不出来的卡要留在日志里，且不写进清单（下游才不会当它「已渲」）
                Debug.LogError($"[cardface-all] `{def.Id}` 渲不出来：{e.GetType().Name}: {e.Message}");
                fail++;
            }
            // 1127 张贴图一路加载不释放会撑爆内存；批处理下没有帧循环，得手动回收
            if (n % 100 == 0) { Resources.UnloadUnusedAssets(); }
        }

        File.WriteAllText(Path.Combine(OutAll, "_manifest.tsv"), manifest.ToString(),
                          new System.Text.UTF8Encoding(false));
        Debug.Log($"[cardface-all] 渲完成功 {n} · 失败 {fail} · 清单 {OutAll}/_manifest.tsv");
    }

    static CardDef FindByName(System.Collections.Generic.List<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c.Name == name) return c;
        return null;
    }

    static string SafeName(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    static string Trim(string s)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= 24 ? s : s.Substring(0, 24) + "…");

    /// <summary>**字墨**的纵向中心（TMP 本地坐标）—— 取该 TMP **全部可见字形顶点** y 范围的中点。
    ///
    /// 🔴 为什么要它（`A848` 那条账）：原版卡名那一层是 **`Midline`**，而 `textBounds` 给的是
    /// **排版框**、**不是字墨** —— 卡名是**混合大小写、带下伸部**（`Howling Banshee Exarch`），
    /// 两者的中心差得很明显（`Label.OrigInkCenterPx(Midline)` 对这类串恒返回 0、只覆盖
    /// 「全大写/数字串」）。⇒ **只能用网格顶点量**。
    /// ⚠️ 调用前必须已经 `ForceMeshUpdate()`（对象激活之后再调，否则 `textInfo` 是垃圾）。
    /// 🔴 **2026-10-18（`A965`）更正：这里原来写「⚠️ 只取 `meshInfo[0]`：一张卡面 TMP 只用一个字体材质；
    ///    将来若真有 TMP 切多材质，这里要改成遍历全部」—— 【当天就改了】**（判据 / 改坏法 =
    ///    `资料/普查产出_1018/S2_A851槽号.md`；同族 `Editor/RewardsScene.cs` 的 `TmpVertPx` /
    ///    `TmpVertsAndAlpha` / `TmpGlyphUvW` 是**同一天**按**同一套**改的）：
    ///    ① **那条自陈的前提本来就不成立** —— 卡面效果文字里**现在就带 `&lt;sprite name=…&gt;`**
    ///    （`Core/CardText.cs` / `Core/CardIcons.cs`；`Core/TmpFont.cs:163` 给**全工程每颗 TMP** 挂了
    ///    `CardIcons.SpriteAsset`）⇒ TMP 会**切出第二个材质槽**，**不是「将来」**。
    ///    ② **改前为什么是缺陷（且静默）**：`characterInfo[i].vertexIndex` 是**按材质槽**编的
    ///    （`TMP_Text.cs:5524-5541` `FillCharacterVertexBuffers`：`index_X4 = meshInfo[materialReferenceIndex].vertexCount`，
    ///    各槽**各自从 0 起切**）⇒ 拿别的槽里的字去索引 0 号槽，读到的是 **0 号槽里「同下标」那个字**的四角 ——
    ///    索引仍在数组内、量出来「像个数」、**不报错**。
    ///    ③ **改前为什么没爆**（本笔与 `A851` 的**唯一**区别）：本处原来扫的是 `meshInfo[0]` 的**整个已分配数组**
    ///    （`TMP_MeshInfo.cs:249-261` 按 **2 的幂**扩容 ⇒ 尾巴上有补齐的零槽，`Clear` / `ClearUnusedVertices`
    ///    把它们清成 `Vector3.zero`），而本工程标签的字墨**跨 `y=0`**（`Middle` / `Midline` 都把字块摆在矩形
    ///    原点两侧）⇒ 那些零槽**落在字墨范围内、动不了 min/max** ⇒ 单槽时两种写法**逐位同值**。
    ///    **改坏法**：越界槽若不当场 `continue`（落回 0 号槽）就等于回到老缺陷上。
    /// ⚠️ 本笔**顺带**把扫描集合收敛成「只认 `isVisible` 的字」（这本来就是本 doc 第一行写的口径，代码原来没收）——
    ///    不可见的字（空格等）TMP 给的是**四角全 0**（`TextMeshPro.cs:4536-4541`）。
    /// 📌 **本笔的回归网【半空】**（详据 = `资料/普查产出_1018/S6_A965卡面探针取槽.md` §2/§3）：本文件**全文件零断言** ⇒「红」这条路本来就不存在；
    ///    `title` / `army` / `race` 三层**逐位同值**（`army`/`race` 网格容量 == 用量、无零槽；`title` 的补齐零槽经实测 `0.0000` 反证**落在字墨之内**）⇒ **改回旧写法也不变**；
    ///    而 **`keywords`（效果文字那层）今天就带 `&lt;sprite&gt;`**（`Core/CardText.cs:290`；sprite asset 的材质是独立一份 ⇒ **2 个槽**）⇒ 旧写法**漏掉图标**、读数会动 ⇒ **本笔在这层是活的**。
    ///    要「有牙」得先实跑确认那颗标签的 `textInfo.meshInfo.Length ≥ 2`（**要跑 Unity**）—— 线索交调度台。</summary>
    /// <returns>字墨中点（TMP 本地单位的 y）；**一个可见字形都量不到时回 `0f`**（哨兵值不变 ——
    /// ⚠️ 它与「字墨中点恰好是 0」撞车，调用点只打日志、不拿它判存在性）。</returns>
    static float InkCenterY(TMP_Text t)
    {
        var ti = t.textInfo;
        if (ti == null || ti.meshInfo == null || ti.meshInfo.Length == 0 || ti.characterInfo == null) return 0f;
        // 🔴 **2026-10-18（`A965`）**：网格槽按**字形自己的** `materialReferenceIndex` 取（原来写死 `[0]`，
        //    一行里混了 `<sprite name=…>` 就会把这个字串到 0 号槽里**同下标那个字**的顶点上）。
        //    取法照 `Editor/ShellScene.cs` 的 `SpanOfTmp` / `Editor/RewardsScene.cs` 的 `TmpVertPx`（`A851` 那三处）。
        var meshes = ti.meshInfo;
        var chr = ti.characterInfo;
        float lo = float.MaxValue, hi = float.MinValue;
        int n = Mathf.Min(ti.characterCount, chr.Length);
        for (int i = 0; i < n; i++)
        {
            if (!chr[i].isVisible) continue;                     // 不可见的字 TMP 写四角全 0，别当字墨
            int slot = chr[i].materialReferenceIndex;
            if (slot < 0 || slot >= meshes.Length) continue;     // 槽号越界 ⇒ 这个字不出点（⛔ 不静默落回 0 号槽）
            var v = meshes[slot].vertices;
            if (v == null) continue;
            int vi = chr[i].vertexIndex;
            for (int k = 0; k < 4; k++)                          // 每个字恒占 4 槽（`TMP_Text.cs:5535`）
            {
                int idx = vi + k;
                if (idx < 0 || idx >= v.Length) continue;        // 角越界 ⇒ 跳这个角（⛔ 不静默当 0）
                float y = v[idx].y;
                if (y < lo) lo = y;
                if (y > hi) hi = y;
            }
        }
        if (lo > hi) return 0f;   // 一个可见字都没有（空串 / 全不可见）—— 与改前「0 号槽取不到网格」同一个哨兵值
        return (lo + hi) * 0.5f;
    }

    /// <summary>把这张卡单独渲进一张图 —— 相机正交、正好框住整张卡（含卡框外沿）</summary>
    static void Shot(CardView view, string path, bool quiet = false)
    {
        var camGo = new GameObject("probeCam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        // 视野比卡本体大一圈：卡框是 2.2452×3.2572 且偏下 0.03，别把它切了
        cam.orthographicSize = 3.5f * 0.5f * 1.06f;
        cam.aspect = (float)W / H;
        cam.transform.position = new Vector3(0f, -0.03f, -20f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);   // 深底（想查「哪里是透的」就临时改成亮绿）

        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(camGo);
        if (!quiet) { Debug.Log($"[cardface] 写出 {path}"); }
    }
}
