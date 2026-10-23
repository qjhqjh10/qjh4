// ============================================================================
//  「逐阵营一张」卡面批量渲图 —— `T04`「12 阵营并排」验收的输入（2026-10-23）
//
// 为什么单开一个入口（而不是往 `CardFaceProbe` 里加）：
//   `CardFaceProbe.Names[]` 是**为排查具体问题手挑的 40 多张**（一张卡一个故事）；
//   而 `T04` 欠的那条验收是「**每个阵营各来一张，和 PnP 成品图并排比**」——
//   判据 = `资料/任务线/卡牌线/任务/T04_卡面几何与图标.md` §五：
//   看**费用图标位置 / 三围 / 文字是否居中 / 底板压框 / 稀有度宝石**。
//
// 🔴 **「12 阵营」是 SO 时代（2022 快照、无 SpaceWolves）的口径** —— 现读
//    `RuleEngine/Resources/cards_engine.json` 有 **13 个 faction 值**
//    （Ultramarines · Goff · SaimHann · BlackLegion · DarkAngels · Genestealers ·
//      TauEmpire · Sautekh · Leviathan · AstraMilitarum · Sororitas ·
//      EmperorsChildren · SpaceWolves）。
//    ⇒ 本探针**按阵营枚举**：`Picks` 里的 13 个各一张 + **池里出现但表里没有的阵营**
//      走兜底规则再补一张 ⇒ 今天是 **13 张 = 「12 阵营」的超集**。
//      ⛔ 不静默少一个阵营（铁律 11：与原版不符 / 复刻有缺漏，全部都要做，只有先后之分）。
//
// 用法（**主对话**统一跑；子代理不跑 Unity —— 铁律 13·3）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod CardFactionProbe.Run -logFile -
//
// 产出（`d:/4/_tmp_view/cardfaction/`）：
//   <阵营>_<卡名>.png          我们渲的那张卡（相机 / 尺寸与 `CardFaceProbe.Shot` **逐值一致**）
//   <阵营>_<卡名>_vsPnP.png    **并排图：左 = 我们，右 = PnP 成品卡**（两边用**同一份** `body_rect` 裁到卡体、
//                              再缩到同一高度 —— 判据见下面 `ComposeSideBySide` 的注释）
//   _manifest.tsv              阵营 / 卡名 / 卡 id / 输出文件 / PnP 路径 / 并排图
//                              （表头即列名；路径一律**绝对路径**；配不上 PnP 的格子**留空**并出声）
//
// ── 选卡规则（`Picks` 表是按这条规则挑出来的；写死 id 的理由见下）──────────────
//   ① `type == unit` —— 部队卡：有三围 + 兵种行 + 数值图标（战术/防御卡没有）
//   ② `desc` 与 `descZh` **都非空** —— ⛔ 不选效果文字为空的模板卡
//   ③ 立绘在盘上（`Resources/Art/cards/art_<id>.png`）
//   ④ PnP 有成品图 —— `d:/2/Warpforge部队卡片/<阵营>/3部队/…png` 真在盘上，
//      且能被 `_合并总表.md` 的**卡面印的名**精确对上（`check_pnp_cards.py` 的**精确轮**，
//      ⛔ 不用别名 / 不用模糊兜底 —— 那两张表都在 python 侧，本文件不复制第二份）
//   ⑤ 在这些候选里优先：**有护甲（银盾）** > **关键词多（徽标 / 行内图标）** >
//      **近战与远程都 > 0** > **效果文字 35~140 字**（2~3 行，才看得出「文字是否居中」）
//   🔴 **为什么把 13 个 id 写死、而不是运行时按分数现挑**：
//      评分要靠「关键词个数」，而 `CardDef.Keywords` 是 `KeywordTable.Parse` 规范化之后的
//      **字典**（认不出的词会被丢掉、护甲列还会补进来）⇒ 与 JSON 的 `keywords` 数组**不是一一对应**；
//      拿它排序会让「报告里列的 13 张」与「Unity 里实际渲的 13 张」**可能不一致**（静默漂移）。
//      写死 id（外加一条兜底规则）⇒ 报告 = 代码 = 产物，三者永远同一份。
//      ⚠️ id 不存在 / 阵营不符时**大声报**（`LogError`）并让该阵营走兜底规则 —— 不静默换一张。
//
// ⛔ 本文件**只读** `CardView` / `BattleDriver` 的公开入口，不改任何卡面生产代码
//    （`Core/**` 不在本文件的白名单里）。
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using RuleEngine;
using CardPresentation;

public static class CardFactionProbe
{
    const string OutDir = "d:/4/_tmp_view/cardfaction";
    const string PnpRoot = "d:/2/Warpforge部队卡片";

    // ── 渲图口径：与 `Editor/CardFaceProbe.cs` 的 `W / H / OrthoHalfH / RenderPixels / Shot`
    //    **逐值一致**（同一套相机、同一个深底）。
    //    ⛔ 本文件没有改 `CardFaceProbe.cs` 的权限（文件所有权）⇒ 只能复制这一小段；
    //    **改一处必须改另一处**，否则同一次验收里两张图的取景会不一样。
    const int W = 700, H = 1115;
    const float OrthoHalfH = 3.5f * 0.5f * 1.06f;   // = 1.855；卡框 2.2452×3.2572、中心 y −0.03
    const float CamY = -0.03f;

    // ── 并排图 ──
    const int SideH = 900;      // 裁块缩到的目标高度（px）
    const int SideGap = 24;     // 两张之间的空隙
    const int SidePad = 16;     // 画布四周留白
    const int PnpGap = 12;      // `body_rect` 闭运算半径 —— 与 `make_cardface_montage.py` 一致
    const float PnpKeep = 0.93f;// `body_rect` 的「段长 ≥ ref 的比例」—— 与 python 一致
    const byte PnpAlphaMin = 250;   // PnP 侧「实心」阈值 —— 与 python 的 `fg_pnp` 一致

    /// <summary>一个阵营的代表卡 + 它的 PnP 成品图（相对 `PnpRoot` 的路径）。</summary>
    struct Pick
    {
        public readonly string Faction;
        public readonly string CardId;
        public readonly string PnpRel;
        public Pick(string faction, string cardId, string pnpRel)
        { Faction = faction; CardId = cardId; PnpRel = pnpRel; }
    }

    // 🆕 2026-10-23：13 个阵营各一张（挑法 = 文件头 ①~⑤）。
    // PnP 路径来自 `Unity/资料/卡表核对_卡图提取/_合并总表.md` 的「文件名」列
    // （逐条 `File.Exists` 现验过；报告 `普查产出_1023b/卡牌12阵营并排_准备.md` 有逐张清单）。
    static readonly Pick[] Picks =
    {
        // 阵营（cards_engine.json 的 faction 值）   卡 id     PnP 成品图（相对 d:/2/Warpforge部队卡片）
        new Pick("AstraMilitarum",  "AM37",  "Astra Militarum/3部队/Warpforge_42_Rogal-Dorn-Tank.png"),
        new Pick("BlackLegion",     "BL31",  "Chaos/3部队/Warpforge_46_Maulerfiend.png"),
        new Pick("DarkAngels",      "DA65",  "Dark Angels/3部队/Warpforge_08_Chaplain-Gabutheron.png"),
        new Pick("EmperorsChildren","EC21",  "Emperor_s Children/3部队/Warpforge_29_Malgarash-the-Adamant.png"),
        new Pick("Genestealers",    "GSC38", "Genestealer Cult/3部队/Warpforge_45_Cult-Leman-Russ.png"),
        new Pick("Goff",            "GOF26", "Orks/3部队/Warpforge_39_Stompa.png"),
        new Pick("Leviathan",       "TL70",  "Tyranid/3部队/Warpforge_10_Norn-Emissary.png"),
        new Pick("SaimHann",        "ASH34", "Aeldari/3部队/Warpforge_42_Avatar-of-Khaine.png"),
        new Pick("Sautekh",         "SAU34", "Necron/3部队/Warpforge_43_Monolith.png"),
        new Pick("Sororitas",       "SOR35", "Sorotitas/3部队/Warpforge_39_Immolator.png"),
        new Pick("SpaceWolves",     "SW34",  "Space Wolves/3部队/Warpforge_39_Murderfang.png"),
        new Pick("TauEmpire",       "TAU27", "Tau/3部队/Warpforge_30_Broadside-Battlesuit.png"),
        new Pick("Ultramarines",    "UM101", "Ultramarines/3部队/Warpforge_19_1st-Company-Terminator.png"),
    };

    // ==================================================================
    //  入口
    // ==================================================================
    public static void Run()
    {
        _assertBad = 0;                   // 同一会话里跑两次时，计数不累计（下面 B 那条断言用它）
        Directory.CreateDirectory(OutDir);
        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0)
        {
            Debug.LogError("[cardfaction] 卡池是空的 ⇒ **一张都没渲**（别把这份日志读成通过）");
            return;
        }

        var byId = new Dictionary<string, CardDef>();
        var factions = new List<string>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (!byId.ContainsKey(c.Id)) byId[c.Id] = c;
            if (!factions.Contains(c.Faction)) factions.Add(c.Faction);
        }
        Debug.Log($"[cardfaction] 卡池 {pool.Count} 张 · {factions.Count} 个阵营 · "
                + $"Picks 表 {Picks.Length} 条 → 输出 {OutDir}");

        var manifest = new StringBuilder();
        manifest.Append("阵营\t卡名\t卡 id\t输出文件\tPnP 路径\t并排图\n");
        int ok = 0, fail = 0;
        int okPicks = 0;                  // ① 表里点名的 13 个阵营里**渲成功**的张数（B 那条断言量的就是它）
        var covered = new HashSet<string>();

        // ---- ① 表里点名的 13 个阵营 ----
        foreach (var pick in Picks)
        {
            covered.Add(pick.Faction);
            if (!factions.Contains(pick.Faction))
            {
                // 不静默：表过期（阵营改名 / 卡池改）必须看得见
                Debug.LogWarning($"[cardfaction] Picks 表里的阵营 `{pick.Faction}` 在现池里**不存在** "
                               + "⇒ 这一格跳过（表过期了吗？）");
                continue;
            }

            CardDef def;
            if (!byId.TryGetValue(pick.CardId, out def) || def.Faction != pick.Faction)
            {
                Debug.LogError($"[cardfaction] Picks 表里的 `{pick.CardId}`（{pick.Faction}）"
                             + "在现池里找不到 / 阵营不符 ⇒ 该阵营改走**兜底规则**（不静默换一张）");
                def = FallbackPick(pool, pick.Faction);
            }
            if (def == null)
            {
                Debug.LogError($"[cardfaction] 阵营 `{pick.Faction}` 连兜底规则也选不出卡 "
                             + "⇒ **这一格没渲**（别把缺图读成通过）");
                fail++;
                continue;
            }
            if (RenderOne(def, pick.PnpRel, manifest)) { ok++; okPicks++; }
            else fail++;
        }

        // ---- ② 池里有、表里没有的阵营（今天是 0 个；以后加阵营走这里，⛔ 不静默漏）----
        foreach (var fac in factions)
        {
            if (covered.Contains(fac)) continue;
            var def = FallbackPick(pool, fac);
            if (def == null)
            {
                Debug.LogError($"[cardfaction] 新阵营 `{fac}` 按兜底规则选不出卡（没有带效果文字+立绘的部队卡）"
                             + " ⇒ **这一格没渲**");
                fail++;
                continue;
            }
            Debug.LogWarning($"[cardfaction] 阵营 `{fac}` 不在 Picks 表里 ⇒ 按兜底规则选了 `{def.Id} {def.Name}`；"
                           + "**PnP 路径未知，清单里留空**（要补齐请把它加进 `Picks`）");
            if (RenderOne(def, "", manifest)) ok++; else fail++;
        }

        var manifestPath = Path.Combine(OutDir, "_manifest.tsv");
        File.WriteAllText(manifestPath, manifest.ToString(), new UTF8Encoding(false));
        Debug.Log($"[cardfaction] === 完成 {ok} 张 · 失败 {fail} 张 · 清单 {manifestPath} ===");
        if (fail > 0)
            Debug.LogError($"[cardfaction] 有 {fail} 张没渲出来 —— **这不是通过**，见上面逐条 LogError");

        // ═════════════════ B：把「13 张全成功」钉成一条**断言**（2026-10-23）═════════════════
        //
        // 🔴 **为什么补这一条**：原来这个探针全靠 `LogError + fail 计数` 兜着 ——
        //   人能看见红字，但**没有任何一条判据**说「13 张必须全出来」；
        //   少渲一张（阵营跳过 / 兜底也选不出卡 / 某张抛异常）时，只有一行 `Debug.LogWarning`
        //   （「表过期了吗？」）就过去了 ⇒ 并排验收会**静默少一张**。
        //
        // **判据**：`Picks` 表里点名的每一条都必须渲出图（`okPicks == Picks.Length`）**且**全程 0 失败。
        //   ⚠️ 只数 **① 循环**里那 13 条（`okPicks`）—— 池里以后**新增阵营**走 ② 循环兜底渲出来，
        //     那属于「多渲了」、**不该把这条判红**（`ok` 总数会 > 13，本条不看它）。
        //   ⚠️ `fail` 是**全程**的（② 循环失败也算）⇒ 「任意一张渲失败就红」。
        //
        // 🧨 **改坏法（必须能红）**：① 把 `Picks` 里任一条的 `CardId` 改成池里不存在的 id ⇒ 走兜底、
        //   若兜底也选不出 ⇒ `fail++` ⇒ 红；② 把某个 `Pick.Faction` 改成不存在的阵营名 ⇒ 该格 `continue`、
        //   `okPicks` 少 1 ⇒ 红；③ 让 `RenderOne` 里 `view` 建不出来（`CardView.Create` 抛异常）⇒ 红；
        //   ④ 把 `Picks` 加一条而池里没有那个阵营 ⇒ 红。
        Check(okPicks == Picks.Length && fail == 0,
              "★ 13 阵营并排 **全部渲出**：Picks 表 " + Picks.Length + " 条 → 成功 **" + okPicks + "** 张、失败 " +
              fail + " 张（全池共 " + ok + " 张）" +
              "。改坏法：① `Picks` 里任一条 id 改成池里不存在的 ⇒ 兜底也选不出 ⇒ fail++ ⇒ 红；" +
              "② 某个 `Pick.Faction` 写成不存在的阵营 ⇒ 该格 `continue`、okPicks 少 1 ⇒ 红；" +
              "③ `RenderOne` 里 `CardView.Create` 抛异常 ⇒ 红。",
              ref _assertBad);

        if (_assertBad > 0)
            Debug.LogError($"[cardfaction] **断言没过 {_assertBad} 条** —— 这不是通过（见上面 `!!` 那几行）");
        else
            Debug.Log($"[cardfaction] 断言全过（{Picks.Length} 张全部渲出）");
    }

    // ── 断言计数（与 `IconSetup.Verify` 的 `Check(bool,string,ref int)` 同一个写法）──
    static int _assertBad;

    static void Check(bool ok, string msg, ref int bad)
    {
        if (ok) Debug.Log("[cardfaction] OK " + msg);
        else { bad++; Debug.LogError("[cardfaction] !! " + msg); }
    }

    /// <summary>兜底选卡（只给「表里没有的阵营」用）：部队卡 + desc/descZh 非空 + 有立绘，
    /// 优先有护甲 → 近战远程都 &gt;0 → 效果文字长度接近 70（2~3 行）。**确定性**：最后按卡名序。</summary>
    static CardDef FallbackPick(List<CardDef> pool, string faction)
    {
        CardDef best = null;
        int bestArmour = -1, bestBoth = -1, bestDist = int.MaxValue;
        string bestName = null;
        foreach (var c in pool)
        {
            if (c == null || c.Faction != faction) continue;
            if (c.Type != "unit") continue;
            if (string.IsNullOrEmpty(Trimmed(c.Desc)) || string.IsNullOrEmpty(Trimmed(c.DescZh))) continue;
            if (CardArt.Portrait(BattleDriver.ArtKey(c)) == null) continue;   // 没立绘 ⇒ 卡面是占位图

            int armour = c.ArmourFromPool > 0 ? 1 : 0;
            int both = (c.Attack > 0 && c.RangedAttack > 0) ? 1 : 0;
            int dist = Mathf.Abs(Trimmed(c.Desc).Length - 70);
            bool better = armour > bestArmour
                       || (armour == bestArmour && both > bestBoth)
                       || (armour == bestArmour && both == bestBoth && dist < bestDist)
                       || (armour == bestArmour && both == bestBoth && dist == bestDist
                           && string.CompareOrdinal(c.Name, bestName ?? "") < 0);
            if (better)
            {
                best = c; bestArmour = armour; bestBoth = both; bestDist = dist; bestName = c.Name;
            }
        }
        return best;
    }

    static string Trimmed(string s) { return s == null ? "" : s.Trim(); }

    /// <summary>文件名净化 —— 与 `CardFaceProbe.SafeName` **逐字一致**（字母/数字保留，其余 → `_`）。
    /// ⛔ 那一份是 `private`，本文件没有改它的权限 ⇒ 只能复制；改一处必须改另一处。</summary>
    static string SafeName(string s)
    {
        var sb = new StringBuilder(string.IsNullOrEmpty(s) ? 0 : s.Length);
        if (!string.IsNullOrEmpty(s))
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    // ==================================================================
    //  渲一张 + 并排图 + 清单
    // ==================================================================
    static bool RenderOne(CardDef def, string pnpRel, StringBuilder manifest)
    {
        var root = new GameObject("probe_faction_" + def.Id);
        try
        {
            var data = BattleDriver.ToCardData(def, def.Faction);
            var view = CardView.Create(root.transform, data, def.Id);

            string baseName = SafeName(def.Faction) + "_" + SafeName(def.Name);
            string pngPath = Path.Combine(OutDir, baseName + ".png");
            Shot(view, pngPath);

            // PnP 路径：表里给的相对路径 → 绝对；不存在就**留空并出声**（⛔ 不猜别的文件）
            string pnpAbs = "";
            if (!string.IsNullOrEmpty(pnpRel))
            {
                pnpAbs = Path.Combine(PnpRoot, pnpRel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(pnpAbs))
                {
                    Debug.LogError($"[cardfaction] `{def.Id}` 的 PnP 图**不在盘上**：`{pnpAbs}` "
                                 + "⇒ 清单里留空、不出并排图（盘上文件被挪走了？）");
                    pnpAbs = "";
                }
            }

            string sbsPath = "";
            if (!string.IsNullOrEmpty(pnpAbs))
            {
                sbsPath = Path.Combine(OutDir, baseName + "_vsPnP.png");
                if (!ComposeSideBySide(view, pnpAbs, sbsPath)) sbsPath = "";   // 内部已 LogError
            }
            else
            {
                Debug.LogWarning($"[cardfaction] `{def.Id} {def.Name}` 没有 PnP 路径 ⇒ 只出单张图（不出并排图）");
            }

            manifest.Append(def.Faction).Append('\t')
                    .Append(def.Name).Append('\t')
                    .Append(def.Id).Append('\t')
                    .Append(pngPath).Append('\t')
                    .Append(pnpAbs).Append('\t')
                    .Append(sbsPath).Append('\n');

            var d = view.FrameDrawSize;
            Debug.Log($"[cardfaction] {def.Faction} · {def.Id} `{def.Name}` "
                    + $"（{def.Rarity} · cost {def.Cost} · {def.Attack}/{def.RangedAttack}/{def.Health}）"
                    + $" → {pngPath}"
                    + (sbsPath != "" ? $" · 并排 {sbsPath}" : "")
                    + $"  [卡框实绘 {d.x:F4}×{d.y:F4}]");
            return true;
        }
        catch (Exception e)
        {
            // 不许静默失败：渲不出来的卡留在日志里，且**不写进清单**（下游才不会当它「已渲」）
            Debug.LogError($"[cardfaction] `{def.Id}` 渲不出来：{e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            return false;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  并排图
    // ==================================================================
    /// <summary>把「我们的渲图」和「PnP 成品卡」**裁到各自的卡体外接矩形**、缩到同一高度、左右并排。
    ///
    /// <para>**左边 = 我们，右边 = PnP**（画面里不加字 —— 需要在报告/文件名里说明）。</para>
    ///
    /// <para>🔴 **为什么两边都要「按卡体裁」**：我们渲图的相机取景比卡大一圈（`OrthoHalfH` 比卡框高），
    /// 而 PnP 是 900×1200 带透明边的画布 —— 不裁直接并排，两张卡的**大小差**会盖过要看的
    /// 「图标位置 / 文字居中」这类**相对**差别。裁到卡体后，两张都是「卡面本身」。</para>
    ///
    /// <para>两侧用**同一份 `BodyRect` 判据**（`_close_1d` 闭运算 + 「过中线的连续段」+ 中位参考段长 × `keep`），
    /// 只是**输入不同**：
    /// · **PnP 侧** = 成品图自带的 alpha（阈值 `PnpAlphaMin`）；
    /// · **我们侧** = 只留 `frame` 一层、**透明底**渲一张的 alpha。
    ///   ⇒ 卡框贴图的 alpha 就是「可见卡体」（立绘在框下、数值/宝石在卡内），
    ///   与 PnP 找到的是同一个矩形。⚠️ 用同一个判据是**故意的**：卡框上可能有凸出主体之外的
    ///   装饰（哥特拱顶 / 角饰），各自用「外接矩形」量会让两边裁得不一样高、并排图里大小失真。</para>
    ///
    /// <para>⚠️ `BodyRect` 是逐行移植 `Unity/工具/make_cardface_montage.py` 的同名逻辑 ——
    /// 那份 python 是**判据正本**，本文件只是没有 python 时（Unity 批处理里）的等价实现，
    /// **改一处必须改另一处**（`PnpGap` / `PnpKeep` / 阈值都在上面）。</para>
    ///
    /// <para>⚠️ **为什么要单独渲一张「只有 frame」的**：整张卡最底层还有一层
    /// `Card Highlight And Shadow`（4.4281² 的 SDF 软影，比卡大得多）—— 不摘掉它，
    /// alpha 外接矩形会把软影算进去、裁出来的卡会小一圈。</para>
    /// </summary>
    static bool ComposeSideBySide(CardView view, string pnpPath, string outPath)
    {
        Texture2D pnpTex = null;
        try
        {
            // ---- 我们侧：只留 frame 那一层，透明底渲一张，量 alpha 外接矩形 ----
            int ox0, oy0, ox1, oy1;
            if (!OurFrameRect(view, out ox0, out oy0, out ox1, out oy1))
            {
                // 兜底：按卡框 quad 的矩形裁（相机参数与 `RenderPixels` 一致）
                FrameQuadCrop(out ox0, out oy0, out ox1, out oy1);
                Debug.LogWarning("[cardfaction] 量不到我们卡框的 alpha 外接矩形 ⇒ 退回按 `FrameQuadSize` 的矩形裁"
                               + "（并排图里我们那侧可能带一圈透明边）");
            }

            // ---- PnP 侧：读图 + `body_rect` ----
            pnpTex = LoadPng(pnpPath);
            if (pnpTex == null)
            {
                Debug.LogError($"[cardfaction] 读不了 PnP 图 `{pnpPath}` ⇒ 不出并排图（单张图照出）");
                return false;
            }
            var pnpPx = pnpTex.GetPixels32();
            int px0, py0, px1, py1;
            if (!BodyRect(pnpPx, pnpTex.width, pnpTex.height, PnpAlphaMin,
                          out px0, out py0, out px1, out py1))
            {
                px0 = 0; py0 = 0; px1 = pnpTex.width - 1; py1 = pnpTex.height - 1;
                Debug.LogWarning($"[cardfaction] PnP 卡体矩形量不出来（`{Path.GetFileName(pnpPath)}`）⇒ 用整张画布");
            }

            // ---- 我们侧：取**同一次渲染**的不透明版（深底）像素，按上面量到的矩形裁 ----
            var oursPx = RenderPixels(opaque: true);

            int owSrc = ox1 - ox0 + 1, ohSrc = oy1 - oy0 + 1;
            int pwSrc = px1 - px0 + 1, phSrc = py1 - py0 + 1;
            if (owSrc < 16 || ohSrc < 16 || pwSrc < 16 || phSrc < 16)
            {
                Debug.LogError($"[cardfaction] 裁块太小（我们 {owSrc}×{ohSrc} · PnP {pwSrc}×{phSrc}）"
                             + " ⇒ 不出并排图（别拿一张糊图当证据）");
                return false;
            }

            int ow = Mathf.Max(1, Mathf.RoundToInt(owSrc * (float)SideH / ohSrc));
            int pw = Mathf.Max(1, Mathf.RoundToInt(pwSrc * (float)SideH / phSrc));
            int cw = SidePad * 2 + ow + SideGap + pw;
            int ch = SidePad * 2 + SideH;
            var canvas = new Color32[cw * ch];
            var bg = new Color32(24, 26, 32, 255);           // 与 `make_cardface_montage.py` 的 PAGE_BG 同色
            for (int i = 0; i < canvas.Length; i++) canvas[i] = bg;

            BlitCrop(oursPx, W, H, ox0, oy0, owSrc, ohSrc, ow, SideH, canvas, cw, ch, SidePad, SidePad, bg);
            BlitCrop(pnpPx, pnpTex.width, pnpTex.height, px0, py0, pwSrc, phSrc, pw, SideH,
                     canvas, cw, ch, SidePad + ow + SideGap, SidePad, bg);

            var outTex = new Texture2D(cw, ch, TextureFormat.RGB24, false);
            outTex.SetPixels32(canvas);
            outTex.Apply();
            File.WriteAllBytes(outPath, outTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(outTex);

            Debug.Log($"[cardfaction]   并排图 {cw}×{ch}：我们裁 x[{ox0},{ox1}] y[{oy0},{oy1}]（{owSrc}×{ohSrc}）"
                    + $" · PnP 裁 x[{px0},{px1}] y[{py0},{py1}]（{pwSrc}×{phSrc}）· 两边缩到高 {SideH}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[cardfaction] 并排图 `{outPath}` 出错：{e.GetType().Name}: {e.Message}");
            return false;
        }
        finally
        {
            if (pnpTex != null) UnityEngine.Object.DestroyImmediate(pnpTex);
        }
    }

    /// <summary>把 `<paramref name="view"/>` 的 `frame` 一层单独留着、**透明底**渲一张，
    /// 用**与 PnP 侧同一个** `BodyRect` 判据量「卡体矩形」（像素坐标，**Unity 的 bottom-up**）。
    /// 其余层临时关掉、渲完**原样恢复**（含原本就关着的层）。
    ///
    /// <para>🔴 **为什么不用「alpha 外接矩形」**：卡框上可能有**凸出主体之外**的装饰
    /// （哥特拱顶 / 角饰），外接矩形会把它算进去 ⇒ 我们这侧裁得比 PnP 那侧高 ⇒ 并排图里
    /// **我们的卡显得更小**。`body_rect`（过中线的连续段 + 段长阈值）量的是**主体矩形**，
    /// 两边用同一个判据才谈得上「同一尺度并排」。</para></summary>
    static bool OurFrameRect(CardView view, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = y0 = 0; x1 = W - 1; y1 = H - 1;
        var face = view != null ? view.FaceLayerRoot : null;
        if (face == null) return false;

        int n = face.childCount;
        var wasActive = new bool[n];
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            var ch = face.GetChild(i);
            wasActive[i] = ch.gameObject.activeSelf;
            if (ch.name == "frame") found = true;
            else ch.gameObject.SetActive(false);
        }
        if (!found)
        {
            for (int i = 0; i < n; i++) face.GetChild(i).gameObject.SetActive(wasActive[i]);
            return false;
        }

        Color32[] px;
        try { px = RenderPixels(opaque: false); }          // 透明底 ⇒ alpha 就是卡框自己的 alpha
        finally { for (int i = 0; i < n; i++) face.GetChild(i).gameObject.SetActive(wasActive[i]); }

        return BodyRect(px, W, H, PnpAlphaMin, out x0, out y0, out x1, out y1);
    }

    /// <summary>`FrameQuadSize` → 屏幕像素矩形（`OurFrameRect` 的兜底）。
    /// 相机中心 `CamY`、正交半高 `OrthoHalfH`，卡在原点 —— 与 `RenderPixels` 同一套参数。</summary>
    static void FrameQuadCrop(out int x0, out int y0, out int x1, out int y1)
    {
        float ppu = H / (2f * OrthoHalfH);
        var q = CardView.FrameQuadSize;                    // (宽, 高, 中心 y)，世界单位
        float cx = W * 0.5f;
        float cy = H * 0.5f + (q.z - CamY) * ppu;          // bottom-up 像素
        float hw = q.x * ppu * 0.5f, hh = q.y * ppu * 0.5f;
        x0 = Mathf.Clamp(Mathf.FloorToInt(cx - hw), 0, W - 1);
        x1 = Mathf.Clamp(Mathf.CeilToInt(cx + hw), 1, W - 1);
        y0 = Mathf.Clamp(Mathf.FloorToInt(cy - hh), 0, H - 1);
        y1 = Mathf.Clamp(Mathf.CeilToInt(cy + hh), 1, H - 1);
    }

    /// <summary>**卡体矩形**（PnP 与我们两侧共用这一份判据）—— **逐行移植** `Unity/工具/make_cardface_montage.py` 的
    /// `body_rect` / `_close_1d` / `_run_through`（`gap = PnpGap` · `keep = PnpKeep`）。
    ///
    /// <para>⚠️ 那份 python 是判据正本（2026-09-15 实测踩过：数值圆/盾/生命框/宝石**伸出卡框外**，
    /// 拿 alpha bbox 当卡体会多出几十 px；`max` 段长会被越框立绘拉长 ⇒ 先丢短段再取中位）。
    /// 本文件只是 Unity 批处理里没有 python 时的等价实现 —— **改一处必须改另一处**。</para>
    ///
    /// <para>返回像素坐标（**bottom-up**；算法对上下/左右对称，方向不影响结果）。</para></summary>
    static bool BodyRect(Color32[] px, int w, int h, byte alphaMin,
                         out int x0, out int y0, out int x1, out int y1)
    {
        x0 = 0; y0 = 0; x1 = w - 1; y1 = h - 1;
        if (px == null || w < 32 || h < 32) return false;

        var mask = new byte[w * h];
        // 严格大于 `alphaMin`（python `fg_pnp` = `a > 250`）
        for (int i = 0; i < mask.Length; i++) mask[i] = (byte)(px[i].a > alphaMin ? 1 : 0);

        // 行：过「水平中线」的连续段长 → 选行
        var closedRows = Close1D(mask, w, h, PnpGap);
        var runH = RunThrough(closedRows, w, h, w / 2);
        var ys = PickRuns(runH, PnpKeep);

        // 列：转置之后同样跑一遍
        var mt = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++) mt[x * h + y] = mask[row + x];
        }
        var closedCols = Close1D(mt, h, w, PnpGap);
        var runV = RunThrough(closedCols, h, w, h / 2);
        var xs = PickRuns(runV, PnpKeep);

        if (xs.Length == 0 || ys.Length == 0) return false;
        x0 = xs[0]; x1 = xs[xs.Length - 1];
        y0 = ys[0]; y1 = ys[ys.Length - 1];
        return x1 > x0 && y1 > y0;
    }

    /// <summary>一维闭运算（膨胀 + 腐蚀），逐行独立。移植自 python `_close_1d`。
    /// `m` 是 `rows × L` 的 0/1 数组；窗口 = `[max(x-gap,0), min(x+gap+1, L))`。</summary>
    static byte[] Close1D(byte[] m, int L, int rows, int gap)
    {
        var cs = new int[rows * (L + 1)];                  // 前缀和：cs[r,x] = sum_{k<x} m[r,k]
        for (int r = 0; r < rows; r++)
        {
            int b = r * (L + 1), s = r * L, acc = 0;
            for (int x = 0; x < L; x++) { cs[b + x] = acc; acc += m[s + x]; }
            cs[b + L] = acc;
        }
        var d = new byte[rows * L];                        // 膨胀：窗口内有 1 ⇒ 1
        for (int r = 0; r < rows; r++)
        {
            int b = r * (L + 1), o = r * L;
            for (int x = 0; x < L; x++)
            {
                int lo = Math.Max(x - gap, 0), hi = Math.Min(x + gap + 1, L);
                d[o + x] = (byte)(cs[b + hi] - cs[b + lo] > 0 ? 1 : 0);
            }
        }
        var ds = new int[rows * (L + 1)];
        for (int r = 0; r < rows; r++)
        {
            int b = r * (L + 1), s = r * L, acc = 0;
            for (int x = 0; x < L; x++) { ds[b + x] = acc; acc += d[s + x]; }
            ds[b + L] = acc;
        }
        var closed = new byte[rows * L];                   // 腐蚀：窗口内全是 1 ⇒ 1
        for (int r = 0; r < rows; r++)
        {
            int b = r * (L + 1), o = r * L;
            for (int x = 0; x < L; x++)
            {
                int lo = Math.Max(x - gap, 0), hi = Math.Min(x + gap + 1, L);
                closed[o + x] = (byte)(ds[b + hi] - ds[b + lo] == hi - lo ? 1 : 0);
            }
        }
        return closed;
    }

    /// <summary>每行「过第 <paramref name="c"/> 列」的连续 1 段长度。移植自 python `_run_through`
    /// （左侧 = `x ≤ c` 里最右的 0，没有则 −1；右侧 = `x ≥ c` 里最左的 0，没有则 `L`）。</summary>
    static int[] RunThrough(byte[] closed, int L, int rows, int c)
    {
        var run = new int[rows];
        for (int r = 0; r < rows; r++)
        {
            int o = r * L;
            int left = -1;
            for (int x = c; x >= 0; x--) if (closed[o + x] == 0) { left = x; break; }
            int right = L;
            for (int x = c; x < L; x++) if (closed[o + x] == 0) { right = x; break; }
            run[r] = right - left - 1;
        }
        return run;
    }

    /// <summary>python `ref()` + `ys = where(run &gt;= keep*ref)`：
    /// 参考段长 = 「短于最大值一半的段先丢掉，再取中位」；没有长段就用最大值。</summary>
    static int[] PickRuns(int[] run, float keep)
    {
        if (run == null || run.Length == 0) return new int[0];
        int max = int.MinValue;
        for (int i = 0; i < run.Length; i++) if (run[i] > max) max = run[i];
        var big = new List<int>();
        for (int i = 0; i < run.Length; i++) if (run[i] > 0.5f * max) big.Add(run[i]);
        float refv;
        if (big.Count > 0)
        {
            big.Sort();
            refv = (big.Count % 2 == 1)
                 ? big[big.Count / 2]
                 : 0.5f * (big[big.Count / 2 - 1] + big[big.Count / 2]);
        }
        else refv = max;

        var keepers = new List<int>();
        for (int i = 0; i < run.Length; i++) if (run[i] >= keep * refv) keepers.Add(i);
        return keepers.ToArray();
    }

    /// <summary>把源图的一块矩形裁剪 + 双线性缩放到画布上（**源是 bottom-up**，画布也是）。
    /// 透明像素按 `bg` 合成（PnP 的透明边贴到深色页面上才不会出现半透明方块）。</summary>
    static void BlitCrop(Color32[] src, int sw, int sh, int sx0, int sy0, int cw0, int ch0,
                         int dw, int dh, Color32[] dst, int dstW, int dstH, int dx0, int dy0, Color32 bg)
    {
        for (int dy = 0; dy < dh; dy++)
        {
            float syf = (dy + 0.5f) * ch0 / dh - 0.5f + sy0;
            int y0 = Mathf.Clamp(Mathf.FloorToInt(syf), 0, sh - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, sh - 1);
            float fy = Mathf.Clamp01(syf - y0);
            int drow = (dy0 + dy) * dstW + dx0;

            for (int dx = 0; dx < dw; dx++)
            {
                float sxf = (dx + 0.5f) * cw0 / dw - 0.5f + sx0;
                int x0 = Mathf.Clamp(Mathf.FloorToInt(sxf), 0, sw - 1);
                int x1 = Mathf.Clamp(x0 + 1, 0, sw - 1);
                float fx = Mathf.Clamp01(sxf - x0);

                Color32 c00 = src[y0 * sw + x0], c10 = src[y0 * sw + x1];
                Color32 c01 = src[y1 * sw + x0], c11 = src[y1 * sw + x1];

                float r = Bilerp(c00.r, c10.r, c01.r, c11.r, fx, fy);
                float g = Bilerp(c00.g, c10.g, c01.g, c11.g, fx, fy);
                float b = Bilerp(c00.b, c10.b, c01.b, c11.b, fx, fy);
                float a = Bilerp(c00.a, c10.a, c01.a, c11.a, fx, fy) / 255f;

                dst[drow + dx] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(r * a + bg.r * (1f - a)), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(g * a + bg.g * (1f - a)), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(b * a + bg.b * (1f - a)), 0, 255),
                    255);
            }
        }
    }

    static float Bilerp(float c00, float c10, float c01, float c11, float fx, float fy)
    {
        return (c00 * (1f - fx) + c10 * fx) * (1f - fy) + (c01 * (1f - fx) + c11 * fx) * fy;
    }

    // ==================================================================
    //  渲图（与 `CardFaceProbe.RenderPixels` / `Shot` 同一套）
    // ==================================================================
    /// <summary>把场景里的卡**渲成像素**。`opaque = true` ⇒ 深底 RGB（出 PNG 用）；
    /// `opaque = false` ⇒ **透明底**（alpha = 卡自己的 alpha，`OurFrameRect` 用量卡体）。
    /// 相机：正交、`OrthoHalfH`、中心 y = `CamY`、正对 −z —— 与 `CardFaceProbe` 逐值一致。</summary>
    static Color32[] RenderPixels(bool opaque)
    {
        var camGo = new GameObject("probeFactionCam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = OrthoHalfH;
        cam.aspect = (float)W / H;
        cam.transform.position = new Vector3(0f, CamY, -20f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = opaque ? new Color(0.07f, 0.08f, 0.10f, 1f)   // 深底（同 CardFaceProbe）
                                    : new Color(0f, 0f, 0f, 0f);

        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0f, 0f, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        var px = tex.GetPixels32();
        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.DestroyImmediate(camGo);
        return px;
    }

    /// <summary>把这张卡单独渲进一张 PNG（深底）。与 `CardFaceProbe.Shot` 的输出逐参数同源。</summary>
    static void Shot(CardView view, string path)
    {
        var px = RenderPixels(opaque: true);
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static Texture2D LoadPng(string path)
    {
        byte[] bytes;
        try { bytes = File.ReadAllBytes(path); }
        catch (Exception e)
        {
            Debug.LogError($"[cardfaction] 读 `{path}` 失败：{e.GetType().Name}: {e.Message}");
            return null;
        }
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (!tex.LoadImage(bytes))
        {
            UnityEngine.Object.DestroyImmediate(tex);
            return null;
        }
        return tex;
    }
}
