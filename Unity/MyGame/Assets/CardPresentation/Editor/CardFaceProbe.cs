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
using System.Collections.Generic;   // 🆕 2026-10-12：`CheckLayers` 用 `List<CardView>`
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
    /// 战术卡没有数值格）。
    /// ⚠️ **「超出卡框」那句 2026-10-10 订正**：原来这里写「督军卡有『角色越出卡框』」——
    ///    **与事实不符**。**原版默认不越出**（立绘在框下、由框的 alpha 剪出来），
    ///    越出是**逐卡 opt-in**、值在服务器（`CardView.UseFrontLayer` 那段注释是判据全本）。
    ///    我们原来自己多画了一层 `_artFront`（整条角色轮廓盖住卡框、剑尖/肩甲出拱顶），
    ///    **2026-10-10 用户点名「最严重的还是插图超出卡框」⇒ 已改成默认关**。</summary>
    static readonly string[] Names =
    {
        "Heavy Intercessor",     // 单位卡（有兵种行、有数值）
        "Aggressor Sergeant",    // 手牌里看着碎的那张（对照它是卡本身的问题还是缩小导致的）
        "Roboute Guilliman",     // 督军 —— 拿它对照原版成品卡（⚠️ 原版**不**越出卡框，见上面那段订正）
        "Armoured Offensive",    // 战术卡（没有抠图 → 只有一层）
        "Pariah Vanguard",       // 用户点名要看的那张
        "Canoptek Scarab",       // Sautekh common 单位（小费用、有护甲？）
        "Lychguard",             // Sautekh epic 单位
        // 🔴 **2026-10-22（`W64`）订正一条过期项**：这里原来写 `"stormlord"` —— 它**匹配不到任何卡**
        //    （卡池里现名 = `Lord of the Storm`，**上面第 48 行已经列了那一张**；`FindByName` 是逐字比较）。
        //    留着它只会每次跑都报一条假 `LogError`，还会让下一个读的人以为渲过这张 ⇒ 撤掉这条旧名。
        //    （⛔ 没有删掉任何**渲得出来的**卡：那张卡的图一直走的是 `Lord of the Storm` 那一条。）
        "Awakened Dynasty",      // Sautekh common 战术卡
        "Lord of the Storm",     // Sautekh legendary 战术卡
        "Da Old Ways",           // Goff rare 战术卡
        "Beast Snagga Boy",      // Goff rare 单位
        // ── 2026-09-15 加：**卡面图标**的三种记号各来一张（判据见 `资料/卡面图标_现状与缺口.md` §二之五）
        "Armorium Cherub",       // 修女会（`SOR66`）—— 行首 `☀`（符号**吃掉**，画成信仰图标）
        "Farseer",               // 灵族（`ASH80`）—— `①`（灵魂石档位，数字烘在图里 ⇒ 连数字吃掉）
        // ── 2026-09-15 加：**这一轮补立绘的那批**（原来 21 张配不上图，工具改完只剩 `Moment of Grace`）
        //    判据：`资料/PnP卡图_逐张对账_0915.md` §五 / §六。名字都**唯一**，不会撞同名跨阵营的卡。
        "Dark Pact of Excess",   // `b8e81b2fe85cda747a68c4d6326b669c` —— 原版叫 `Mark of Chaos_Slaanesh`（改名，按 id 点名）
        "Chosen of the Four",    // `97e959444b53f424ea4285e2ca02c7e6`  —— 原版叫 `Warmaster`
        "Lord Kaphrael",         // `EC56`  —— 原版叫 `Lord Exultant`
        "Veldras the Sublime",   // `EC22` —— 原版叫 `Threnodic Choir Flawless`（**前缀陷阱**那张）
        "Undying Legions",       // `SAU53`—— 原版叫 `Annihilation Command`
        "Awakened Obelisk",      // `SAU59`—— 与重复行 `SAU_Awakening_Obelisk` **共用一张图**
        "Alien Idol",            // `GSC63`—— 防御卡，验「`defence_` 变体没被配成 `strat_`」
        "Moment of Grace",       // `SOR44`—— 原版旧名是 `Righteous Repugnance`（**错开一位**那张，用户点的）
        "Righteous Repugnance",  // `f104d4c21abe84546abc9365d9eda8dd` —— 它该拿的是 `Purgator Mirabilis` 那张图（金色赎罪引擎）
        // ── 2026-09-19 加：**用户点名要并排比的那张**（PnP 有 `Aeldari/3部队/Warpforge_06_Howling-Banshee-Exarch.png`）
        //    并排比出来的问题：我们把「文字底板」做成了下半截一块**不透明白/黑板**，而 PnP 是
        //    **立绘铺满整张卡、文字直接压在立绘上**。见 `资料/PnP卡图_逐张对账_0915.md` 与本次的更正。
        "Howling Banshee Exarch",// `ASH69`
        // ── 2026-09-21 加：**卡面「关键词段」带图标**的验收尺子（铁律 10 第 6 条）。
        //    这张卡的 `desc` 是 `Rally: Stun an enemy` —— **`Waystone` / `Flank` 两个字在正文里
        //    一个字都没有**，它们只能从「关键词段」那一条路印出来。PnP 成品卡面印的是
        //    `◈Waystone.  ⬇Flank.`（`Aeldari/3部队/Warpforge_20_Howling-Banshee.png`，已并排核过）
        //    ⇒ 这张是「关键词段有没有画出来」最灵敏的一张。**名字与 Exarch 是两张卡，别混**。
        "Howling Banshee",       // `ASH15`
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
        "Devastator Marine",     // `UM98` —— `W5e`① 卡面：`〔紫尖刺〕Blast 2. 〔蓝圆盘+白袍人形〕Oath 4: Deal 5 damage to an enemy troop`
        "Phobos Librarian",      // `UM99` —— `W5e`② 卡面：`〔兜帽〕Camouflage. 〔同一枚〕Oath 3: Deal 2-4 damage to an enemy. If target dies, gain 〔盾〕Shield`
        "Scout Sniper",          // `UM94` —— `W5e`③ 卡面：`〔兜帽〕Camouflage. 〔准星〕Sniper. 〔同一枚〕Oath 1: Deal 1 damage`
        "Firestrike Servo-Turret", // `UM92` —— `W5e`④ 卡面：`〔导弹〕Long Range. 〔哨戒〕Sentry 2. 〔同一枚〕Oath 3: Gain 〔哨戒〕Sentry 2`（2026-10-18 `W28` 改官方名）
        "Dominion Superior",     // `SOR22` —— `W7`① 卡面：`⚡Rally: Deal 1 damage.` ／ `2 ⟨金太阳⟩: Deal 1 additional damage`
        "Devout Warriors",       // `SOR58` —— `W7`② 卡面：`Draw a troop.` ／ `2 ⟨金太阳⟩: Draw an additional troop`
        "Beacon of Faith",       // `SOR56` —— `W7`③ 卡面：`4 ⟨金太阳⟩: …`（**图标与冒号之间没有空格**）／ `7 ⟨金太阳⟩ : …`（**有空格、句尾无句号**）
        "Trial of Suffering",    // `SOR55` —— `W7`④ 卡面：`2 ⟨金太阳⟩: Give it ⟨银盾⟩Armour 2 instead`
        "Imperial Creed",        // `SOR48` —— `W7`⑤ 卡面：`⟨螺旋⟩Stun three random enemies.` ／ `4 ⟨金太阳⟩: Refill 3 Energy`
        // ── 🆕 2026-10-12（第二十三会话 · `A1687`）加：**「正文为空、只有关键词」那一格** ──
        //    来由：`BattleDriver.FaceTextFull` 原来在【正文为空】时**提前 return**，把关键词段整段吞掉。
        //    原版卡面确实有这种卡 —— 现扫全池 1127 张只有 `GOF8 Hornhelmz' Boy` 是这样
        //    （`desc`/`descZh` 双空、`keywords:["Tide 2"]`）。PnP 成品卡 `Orks/3部队/Warpforge_12_Hornhelmz-Boy.png`
        //    的效果区**只有 `Tide 2` + 图标** ⇒ 这张就是「关键词段有没有画出来」的尺子（与 `Howling Banshee` 同一类）。
        "Hornhelmz' Boy",        // `GOF8`（2026-10-18 `W28` 改官方名；旧 id 注释写的 `GOF12` 是**代 1 之前的旧 id**）
        // ── 🆕 2026-10-15（第三十二会话 · 铁律 10 第 6 条）：**官方记号那一代进卡面后的第一批并排比** ──
        //    来由：`Z_卡牌P3` 把 656 张官方 `descZh` 真灌进了池、`AC` 拆了记号闸 ⇒
        //    `[[…]]` / `((…))` / `$$…$$` **第一次真进卡面**，而整链**至今没有端到端证据**
        //    （`RewriteMarks` 四支只在 python 里演算过，没在真数据上出过画面）。这几张就是尺子：
        //      · `TAU65` 用了 `((Heal_1))` —— **现池 0 处置信度**那个宏，最该看的一张
        //      · `SOR30` / `SOR4` / `GOF61` 各走一支（`Heal_x_to_a_friendly_unit` / `Faith_cost_x` / `$$GUID$$`）
        //      · `ASH34` 是**两处 `<br>`** 的那张（顺带判那条挂起的成见：`<br>` 会不会被原样印出来）
        //    ⚠️ PnP 成品卡图在 `d:/2/Warpforge部队卡片/<阵营>/…`（900×1200）—— 只当**内容**的尺子。
        "War Shaper",            // `TAU65` —— `[[strike]]：((Heal_1))。[[talent]]：$$b3735ef3…$$`
        "Celestian Sacresant",   // `SOR30` —— `[[vanguard]]。[[armour]] 1。[[pray]]：((Heal_x_to_a_friendly_unit,3))`
        "Sister Novitiate",      // `SOR4`  —— `((Faith_cost_x,3)) 获得 +1 生命值。[[pray]]：((Deploy_x,$$SOR4$$))`
        "Avatar of Khaine",      // `ASH34` —— `护甲 2。<br>②：…<br>天赋：…`（两处 `<br>`）
        "Rukkatrukk Squigbuggy", // `GOF61` —— `[[armour]] 1。[[unstable]]。[[strike]]：((Deploy_x,$$GOF3$$))` —— 修前卡面效果区**整片空白**；修后应画出 `Tide 2`
        // ── 🆕 2026-10-22（`W64` · 卡牌线离线预判）：**拱顶最差三档**的补拍样本 ──
        //    判据 → `资料/普查产出_1022/W64_卡牌线_剩余与预判.md` §4·1（P1–P4）。
        //    `Armoured Offensive`（UM75）与 `Alien Idol`（GSC63）**本来就列在上面那两段里** ⇒ 不重复加；
        //    这三张是「离线量网格 missPct 最差的三档」，而现有渲图只覆盖 ultramarines。
        //    ⚠️ 名字必须与 `cards_engine.json` 的 `name` **逐字一致**（`FindByName` 是 `==` 比较）。
        "A Trap Sprung",         // `GSC56` —— genestealers **tier4** 矩形方案漏 **16.65%**（全场最差）
        "Armoured Lightning",    // `AM76` —— astramilitarum **tier4** 漏 **14.84%**（第二差）
        "Daemonic Frenzy",       // `BL43` —— blacklegion **tier3**（哥特拱）漏 **8.27%**
    };

    // ==================================================================
    //  🆕 2026-10-12（`A1658` / `A1659` / `A1665`）：**卡面层级三项**的断言
    // ==================================================================
    //
    // **为什么落在这儿**：这三条全是「**层与层之间的关系**」，截图看不出来（用户 2026-10-12 是在真机上
    // 肉眼发现「黑色底板压在卡框上」「图标与文字不在一个层面上」「几张牌的文本互相透出」的）。
    // ⚠️ 本文件**原来全文件零断言**（`InkCenterY` 那段的注释自陈过）⇒ 「红了」这条路本来不存在，这里补上。
    //
    // ⚠️ **本入口不在 `工具/_run_8_checks.sh` 的 12 条里** ⇒ 调度台要么把它加进那个脚本、
    //    要么把这一段搬进 `Editor/CardBaseDemo.cs`（那个在）。✅ 但 `Run()` 开头会调它
    //    ⇒ **今天跑本探针（`CardFaceProbe.Run`）就等于跑它**。
    //
    // 判据全文 = `资料/普查产出_第二十二会话/DIAG_卡面层级三项.md`；
    // 修法报告 = 同目录 `W1_卡面层级修复.md`。
    static int _pass, _fail;
    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[cardface-chk]   ✓ " + msg); }
        else { _fail++; Debug.LogError("[cardface-chk]   ✗ " + msg); }
    }

    /// <summary>`W62/T2`：一条「卡面某档字号 = 原版 em」的断言。
    /// <para>`origFontSize` = 原版 prefab 上那个 TMP 的 `m_fontSize`，`m_LocalScale` **恒 0.01**（逐份实读）
    /// ⇒ 原版 em = `origFontSize × 0.01` 卡单位；`emPerFontSize` = TMP 的 `0.1 × face.scale`；
    /// `ourFontSize` = 我们这一档的**名义**字号（⛔ 不含 `FitToWidth`/`FitToBox` 的回缩 —— 那两处
    /// 对应原版的 `m_enableAutoSizing`）。</para></summary>
    static void CheckFaceSize(string what, string origNode, float origFontSize,
                              float emPerFontSize, float ourFontSize, float cardH)
    {
        float wantEm = origFontSize * 0.01f;
        float gotEm = ourFontSize * emPerFontSize;
        Check(Mathf.Abs(gotEm - wantEm) <= wantEm * 0.01f,
              $"W62/T2 {what}字号 = 原版 `{origNode}` 的 em：期望 **{wantEm:F4}** 卡单位"
            + $"（fs {origFontSize:F2} × scale 0.01），实测 **{gotEm:F4}**"
            + $"（我们 fontSize {ourFontSize:F3} × {emPerFontSize:F3} em/fs）"
            + $" ⇒ 卡高 {gotEm / cardH * 100f:F2}%（原版 {wantEm / cardH * 100f:F2}%）"
            + "｜🧨 改回**汉字墨高**标定（`FontSizeForGlyphHeight`）⇒ 实测 ×1.172 ⇒ 红");
    }

    static int MinOf(int[] a)
    {
        int m = int.MaxValue;
        for (int i = 0; i < a.Length; i++) if (a[i] < m) m = a[i];
        return m;
    }
    static int MaxOf(int[] a)
    {
        int m = int.MinValue;
        for (int i = 0; i < a.Length; i++) if (a[i] > m) m = a[i];
        return m;
    }

    /// <summary>卡面层级三项的断言（不渲图、很快）。逐条都写明了「怎么才能红」。
    /// ⚠️ 外面套一层 `try` 是**为了不把本文件的另一半职责（渲图）一起炸掉** ——
    /// 抛异常时**大声报**（`LogError` + 计一次失败），**不静默**。</summary>
    public static void CheckLayers()
    {
        try { CheckLayersBody(); }
        catch (System.Exception e)
        {
            _fail++;
            Debug.LogError($"[cardface-chk]   ✗ 这一段抛异常了（**没跑完，别读成通过**）："
                         + $"{e.GetType().Name}: {e.Message}");
            Debug.Log($"[cardface-chk] === 合计：{_pass} 通过 / {_fail} 失败 ===");
        }
    }

    static void CheckLayersBody()
    {
        _pass = 0; _fail = 0;
        Debug.Log("[cardface-chk] === 卡面层级三项 开始（A1658 / A1659 / A1665）===");

        var pool = CardDatabase.Load();
        var def = FindByName(pool, "Heavy Intercessor");
        if (def == null)
        {
            Debug.LogError("[cardface-chk] 卡池里没有 `Heavy Intercessor` ⇒ 这一段**什么都没验到**"
                         + "（别把它读成「通过」）");
            return;
        }
        var data = BattleDriver.ToCardData(def, def.Faction);

        // ---------------------------------------------------------------- A1658
        // 原版卡面 `Front` 的子序（底→顶）：`Card Highlight And Shadow → CardImage → Textbackgrounds
        // → CardFrame → Card Info`（`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md:273,308`）。
        // 本仓 z 约定「**越小越靠前**」⇒ 底板必须 **比卡框大**（画在它之后）、**比立绘小**（压住它）。
        {
            var root = new GameObject("chk_a1658");
            var v = CardView.Create(root.transform, data, "chk_a1658");
            Check(!float.IsNaN(v.TextBgLayerZ) && !float.IsNaN(v.FrameZ) && !float.IsNaN(v.ArtZ),
                  "A1658 前提：底板 / 卡框 / 立绘三层都建出来了（缺一层下面几条不算验过）");
            // 🧨 **改坏法**：把 `CardView.TextBgZ` 改回 `−0.012`（原来那个值）⇒ 本条**必红**。
            Check(v.TextBgLayerZ > v.FrameZ,
                  $"A1658 效果文字底板在卡框**之后**（原版子序 Textbackgrounds < CardFrame）"
                + $" textBg.z={v.TextBgLayerZ:F4} > frame.z={v.FrameZ:F4}");
            Check(v.TextBgLayerZ < v.ArtZ,
                  $"A1658 底板仍压在立绘**之上** textBg.z={v.TextBgLayerZ:F4} < art.z={v.ArtZ:F4}");
            Check(!float.IsNaN(v.InfoLayerZ) && v.InfoLayerZ < v.FrameZ,
                  $"A1658 数值层（原版 `Card Info`）在卡框**之前** info.z={v.InfoLayerZ:F4} < frame.z={v.FrameZ:F4}");
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- A1659
        // 卡面**所有** quad 层都挂 `2DCard`（`AddLayer` 那句 `SetParent(FaceRoot)`），而倾摆
        // `AutoCardRotation` 就挂在 `2DCard` 上 ⇒ 文字层必须也挂它，否则位移中与底板/卡框分成两个平面。
        {
            var root = new GameObject("chk_a1659");
            var v = CardView.Create(root.transform, data, "chk_a1659");
            Check(TmpFont.Available,
                  "A1659 前提：字体资产在（不在 ⇒ 下面几条**没验到**，别读成通过）");
            Check(v.FaceLayerRoot != v.transform,
                  "A1659 前提：`2DCard` 那一层真的建出来了（倾摆挂在它身上）");
            if (v.KeywordsText != null)
                Check(v.KeywordsText.transform.parent == v.FaceLayerRoot,
                      $"A1659 效果文字挂 `2DCard` 下（不是卡根）实际父件 = "
                    + (v.KeywordsText.transform.parent != null ? v.KeywordsText.transform.parent.name : "<null>"));
            else Check(!TmpFont.Available, "A1659 效果文字那层没建出来（只有「没字体资产」才允许）");
            if (v.TitleText != null)
                Check(v.TitleText.transform.parent == v.FaceLayerRoot, "A1659 卡名也挂 `2DCard` 下");

            // 🔴 **灭自证（结构上不可能同时满足）**：把 `2DCard` 转 10°（= `AutoCardRotation.maxYAngle`），
            //    断「文字层与底板层的**相对距离**不变」。两块都在 `2DCard` 下 ⇒ 一起刚性旋转 ⇒ 不变；
            //    文字若挂回卡根 ⇒ 它不转、底板转 ⇒ 相对距离变 ~0.1 世界单位 ⇒ **必红**。
            //    （只要「挂错父节点」成立，这条就红；只要这条绿，「挂错父节点」就不成立。）
            if (v.KeywordsText != null && v.TextBgLayer != null)
            {
                Vector3 d0 = v.KeywordsText.transform.position - v.TextBgLayer.transform.position;
                v.FaceLayerRoot.localRotation = Quaternion.Euler(0f, 10f, 0f);
                Vector3 d1 = v.KeywordsText.transform.position - v.TextBgLayer.transform.position;
                v.FaceLayerRoot.localRotation = Quaternion.identity;
                Check(Mathf.Abs(d1.magnitude - d0.magnitude) < 0.02f,
                      $"A1659 倾摆 10° 时文字与底板**仍同面**（相对距离 {d0.magnitude:F4} → {d1.magnitude:F4}）");
            }
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- A1665
        // 「一张卡 = 一段队列」+ 布局给每张不同的号。**卡内层序仍靠 z**，所以判据用
        // `SubtreeQueues()` 的 min/max（前卡整段 > 后卡整段），别写死「一个号」。
        {
            const int N = 5;
            var root = new GameObject("chk_a1665");
            var handGo = new GameObject("chk_a1665_hand");
            handGo.transform.SetParent(root.transform, false);
            var hand = handGo.AddComponent<HandLayout>();
            var cards = new List<CardView>(N);
            for (int i = 0; i < N; i++)
                cards.Add(CardView.Create(root.transform, data, "chk_card" + i, CardFace.Hand));
            hand.Refresh(cards, -1, -1);

            var qs = new int[N];
            for (int i = 0; i < N; i++) qs[i] = cards[i].Queue;

            bool distinct = true;
            for (int i = 0; i < N && distinct; i++)
                for (int j = i + 1; j < N; j++) if (qs[i] == qs[j]) { distinct = false; break; }
            // 🧨 **改坏法**：删掉 `HandLayout.Refresh` 里那句 `c.SetQueue(...)` ⇒ 全部退回 3000 ⇒ 必红。
            Check(distinct, $"A1665 手牌每张卡一个独立队列号 [{string.Join(",", qs)}]");

            // 🔴 **本条的牙齿**：前卡**任何**一层都压得住后卡**任何**一层。
            bool sep = true; int badA = -1, badB = -1;
            for (int i = 0; i + 1 < N && sep; i++)
            {
                int minA = MinOf(cards[i].SubtreeQueues()), maxB = MaxOf(cards[i + 1].SubtreeQueues());
                if (!(minA > maxB)) { sep = false; badA = minA; badB = maxB; }
            }
            Check(sep, "A1665 相邻两张卡：前卡**所有层**的队列都大于后卡**所有层**"
                     + (sep ? "" : $"（实际 {badA} ≤ {badB} ⇒ 后卡的层会穿到前卡上面）"));

            bool dirOk = true;
            for (int i = 0; i + 1 < N; i++)
                if (!(cards[i].transform.localPosition.z < cards[i + 1].transform.localPosition.z
                      && qs[i] > qs[i + 1])) dirOk = false;
            Check(dirOk, "A1665 「左边的压在上面」：z 小的那张队号大（与 z 同向，⛔ 别写反）");

            int maxQ = MaxOf(qs);
            // 全仓有几处按「卡 ≤ 3000」写死：`BoardLayout.MarkerQ = 3100`（必须大于卡的 3000）、
            // `CardWinBox.QEffect = 3020`、`CardDisplayWindow.QChrome = 3000`（遮罩靠 z 压住手牌）。
            Check(maxQ <= CardView.QCard,
                  $"A1665 手牌队列不越过卡的上限 {CardView.QCard}（实际最高 {maxQ}）—— "
                + "越了就踩上面那三处写死的假设");

            // 悬停那张恒是顶号（原版 `ShowCardInHand` 的「亮出来」态）
            hand.Refresh(cards, 2, -1);
            int topQ = cards[2].Queue;
            bool hoverTop = topQ <= CardView.QCard;
            for (int i = 0; i < N; i++) if (i != 2 && !(topQ > cards[i].Queue)) hoverTop = false;
            Check(hoverTop, $"A1665 悬停那张拿到顶号 {topQ}（其余都在它之下）");

            // 拖起来的那张：`CardInteraction` 把它从名单里摘掉、只抬 z（`−dragLiftZ`）
            // ⇒ 布局必须补一个顶号给它，否则它会被左边那几张压住（见 `HandLayout.Refresh` 里那段）。
            var four = new List<CardView> { cards[0], cards[1], cards[2], cards[3] };   // 少了 cards[4]
            hand.Refresh(four, -1, 0);        // **带洞** = 正在拖（`CardInteraction.Relayout` 只在拖拽时传洞）
            Check(cards[4].Queue == CardView.QCard,
                  $"A1665 被拖起来（离开名单）的那张拿顶号：实际 {cards[4].Queue}，应为 {CardView.QCard}");

            // 机制自检：`SetQueue` 必须扫到**子树里每一层**，且**不许写到共享材质上**
            // （2026-09-27 踩过：把字体资产的 `m_CustomRenderQueue` 写脏并落盘）。
            var vr = cards[0];
            var spriteMat = CardIcons.Available ? CardIcons.SpriteAsset.material : null;
            int sharedBefore = spriteMat != null ? spriteMat.renderQueue : -999;
            vr.SetQueue(4444);
            int miss = 0;
            foreach (var mr in vr.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial != null && mr.sharedMaterial.renderQueue != 4444) miss++;
            Check(miss == 0, $"A1665 `SetQueue` 覆盖到子树里每一层（未覆盖 {miss} 层）");
            int sharedAfter = spriteMat != null ? spriteMat.renderQueue : -999;
            Check(sharedBefore == sharedAfter,
                  $"A1665 `SetQueue` 没写到共享 sprite 材质上（{sharedBefore} → {sharedAfter}）");

            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- A1674
        // `SetQueue` 必须也盖 **`ParticleSystemRenderer`**（它不是 `MeshRenderer` ⇒ 2026-10-18 前的
        // 实现扫不到它）。判据（原版 prefab 逐字段实读）：`CanActParticles` / `RotatingRing` 的
        // `m_SortingOrder = 0` · `m_SortingLayerID = 0` · `m_SortingFudge = −3 / −2` ·
        // 材质 `m_CustomRenderQueue = 3000` ⇒ **它们与卡面同队列、只靠负 fudge 压在同队列透明物之后**
        // ⇒ 我们必须让它们跟着**这张卡的队列段**走，否则卡重叠时绿光会盖到邻居卡上。
        // 判据全文 → `资料/普查产出_1018/W_卡牌线_卡面侧2.md` §一·1；代码注释 → `CardView.SetQueue`。
        {
            var root = new GameObject("chk_a1674");
            var v = CardView.Create(root.transform, data, "chk_a1674_card", CardFace.Hand);
            v.SetQueue(2971);
            // 造一颗**假的**粒子层（不依赖随包 shader / 贴图）：材质初始 3000 = 原版 `m_CustomRenderQueue`
            var fx = new GameObject("chk_a1674_particles");
            fx.transform.SetParent(v.transform, false);
            fx.AddComponent<ParticleSystem>();
            var pr = fx.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { renderQueue = 3000 };
            v.SetQueue(2971);       // 新挂的层在第一次 `SetQueue` 之后才出现 —— 与 `SyncBoard` 同一顺序
            int gotQ = pr.sharedMaterial != null ? pr.sharedMaterial.renderQueue : -999;
            Check(gotQ == 2971,
                  $"A1674 `SetQueue` 也盖 `ParticleSystemRenderer`（实际 {gotQ}，应为 2971）"
                + "｜🧨 删掉 `SetQueue` 里那段粒子循环 ⇒ 退回 3000 ⇒ 本条红"
                + "（真机上表现为「卡重叠时绿光盖住邻居卡」）");
            int mn = MinOf(v.SubtreeQueues()), mx = MaxOf(v.SubtreeQueues());
            Check(mn == 2971 && mx == 2971,
                  $"A1674 粒子也进 `SubtreeQueues` 的同一段（实际 [{mn}..{mx}]）"
                + "｜🧨 少算粒子 ⇒ A1672 那条「整段相等」盯不住粒子层");
            Object.DestroyImmediate(root);
        }

        // 徽标底板 = 原版 `IconBackground`（一个 **Sliced** 的 `SpriteRenderer`）。三条判据**锚在三个
        // 不同来源**上，所以**没有哪一处「改回去」能让三条一起变绿**（灭自证）：
        //   ① 色  ← 原版 `m_Color` = (0.32298, 0.34305, 0.35849)（深灰）
        //   ② 锚点 ← 原版 `m_Pivot = (0, 0.5)`（左边缘）+ 几何常量 `Badges.SlotAt/PlateOutward`
        //   ③ 形状/宽 ← 贴图 `Base3d_Trait_Background.png`（105×214）**逐像素实测**的不透明区 x `0..62`
        // 判据全文 = `资料/普查产出_第二十二会话/DIAG_徽标底板图.md`。
        {
            var root = new GameObject("chk_a1661");
            var bd = CardData.Simple("BadgePlateProbe", 3, 3, 4);
            bd.badges = new List<Badge>          // 7 位**全给**（左 3 + 右 4）—— ⑥ 要逐格量外接矩形
            {
                new Badge { sprite = "armour",     counter = 2, active = true },   // 位 0（**左列**首行）
                new Badge { sprite = "blast",      counter = 0, active = true },
                new Badge { sprite = "flying",     counter = 0, active = true },
                new Badge { sprite = "huntMark",   counter = 1, active = true },   // 位 3（**右列**首行）
                new Badge { sprite = "camouflage", counter = 0, active = true },
                new Badge { sprite = "longrange",  counter = 0, active = true },
                new Badge { sprite = "ferocity",   counter = 0, active = true },
            };
            var bv = CardView.Create(root.transform, bd, "chk_a1661_card");
            Check(bv.BadgesShown == 7, $"A1661 前提：7 枚徽标都建出来了（实际 {bv.BadgesShown}）"
                                     + "—— 建不出来下面几条**没验到**，别读成通过"
                                     + "（夹具直给 `bd.badges`，**不过 `InPlayIconKeys`**；"
                                     + "真实出牌路 `BattleDriver` 才过筛）");

            // ① 「不是白方块」：底板色 = 原版 `IconBackground.m_Color` 那个深灰。
            //    🧨 改坏法：删掉 `CardView.ApplyTint` 末尾那句 `SetBadgePlateInk(...)`（或建层时不刷）
            //       ⇒ 底板回到 `_tint` = **白** ⇒ 本条必红。**这正是用户 2026-10-12 报的那个白方块。**
            var pc = bv.BadgePlateColor(0);
            Check(Mathf.Abs(pc.r - 0.32298f) < 0.02f && Mathf.Abs(pc.g - 0.34305f) < 0.02f
                  && Mathf.Abs(pc.b - 0.35849f) < 0.02f,
                  $"A1661 ① 底板 = 原版那个**深灰** `m_Color`（不是白）"
                + $"实际 ({pc.r:F4}, {pc.g:F4}, {pc.b:F4})");

            // ② 锚点：原版 sprite 的 `m_Pivot = (0, 0.5)` 在**左边缘**，两列都**从外缘往卡内铺**
            //    ⇒ 板的外缘 = `SlotAt(i).x ∓ PlateOutward`（几何常量与原版逐值吻合，本次没动）。
            //    🧨 改坏法：把 `BadgePlateQuad` 的锚点改回**居中**（`ax ∓ w*0.5f`）⇒ 外缘偏出 0.276
            //       卡单位（正好半个板宽，可见板左边界从 −0.7881 变 −0.9601、伸到卡外）⇒ 本条必红。
            var xsL = bv.BadgePlateVertexXs(0);
            var xsR = bv.BadgePlateVertexXs(3);
            float outerL = Badges.SlotAt(0).x - Badges.PlateOutward;    // −0.788
            float outerR = Badges.SlotAt(3).x + Badges.PlateOutward;    // +0.788
            float gotL = xsL != null && xsL.Length > 0 ? xsL[0] : float.NaN;
            float gotR = xsR != null && xsR.Length > 0 ? xsR[xsR.Length - 1] : float.NaN;
            Check(Mathf.Abs(gotL - outerL) < 0.01f,
                  $"A1661 ② 左列底板**外缘**落在位子往外 `PlateOutward` 处（{outerL:F4}），实际 {gotL:F4}");
            Check(Mathf.Abs(gotR - outerR) < 0.01f,
                  $"A1661 ② 右列**镜像**：外缘在最右（{outerR:F4}），实际 {gotR:F4}");

            // ③ 形状 = Sliced（原版 `m_DrawMode = 1`）：**3 条竖线**（左片外缘 / 那条竖切线 / 右片内缘）
            //    —— 整图一个 quad 只有 2 条。竖切线在 `PlateSliceFrac` = 9/105 处。
            //    🧨 改坏法：把 `BadgePlateQuad` 换回 `FitQuad(...)`（整图按自身比例内接）⇒ 只剩 2 条 ⇒ 必红；
            //       只把 `PlateSliceFrac` 改错 ⇒ 「3 条」那条仍绿、切线位置那条红（两条各管一件事）。
            bool three = xsL != null && xsL.Length == 3;
            Check(three, $"A1661 ③ 底板是 Sliced **两片**（3 条竖线），实际 {xsL?.Length ?? -1} 条");
            if (three)
            {
                float cutWant = outerL + Badges.PlateW * CardView.PlateSliceFrac;   // = −0.7407
                Check(Mathf.Abs(xsL[1] - cutWant) < 0.005f,
                      $"A1661 ③ 那条竖切线在 9/105 处（{cutWant:F4}），实际 {xsL[1]:F4}");
                // ④ 宽：贴图里**不透明**的只有 x 0..62（63/105）⇒ 可见板宽 = `PlateW × 0.6` = **0.3312**。
                //    另外两个刻度供对照：我们原来「整图内接」只有 **0.2064**（0.62 倍）；若还原成
                //    「按贴图自身比例的内接宽度」= 0.344（quad 宽），也不是这个数。
                //    🧨 改坏法：把 `PlateInkFrac` 改错、或把板宽改成别的一档 ⇒ 必红。
                float visW = (xsL[xsL.Length - 1] - xsL[0]) * CardView.PlateInkFrac;
                Check(Mathf.Abs(visW - 0.3312f) < 0.003f,
                      $"A1661 ④ 可见板宽 ≈ 0.3312 卡单位（`PlateW × 63/105`），实际 {visW:F4}");
            }

            // ⑤ `talent`（天赋）**不进场上徽标** —— 判据 = 原版 `isInPlayIcon == false`
            //    （`Unity/数据/游戏数据/trait_collection.json` 现读：`talent` 的 `hasIcon=True` /
            //     `isInPlayIcon=False`；用户 2026-10-12 真机口径在这一条上与原版一致）。
            //    落地 = `Badges.InPlayIconKeys` 那 47 条白名单（`PlaysInPlayIcon` 过筛）。
            //    🧨 改坏法：把 `talent` 补进 `Badges.InPlayIconKeys`、或删掉 `Badges.For` 里那句
            //       `if (!PlaysInPlayIcon(spr)) continue;` 筛选 ⇒ 必红。
            //    🔴 **灭自证**：同表里**别的关键词必须照画** —— 少了这一半，「整个表都不画」也算绿。
            var kwT = new Dictionary<string, int> { { "Talent", 1 }, { "Blast", 2 } };
            var bl = Badges.For(kwT);
            bool hasTalent = false, hasBlast = false;
            for (int i = 0; i < bl.Count; i++)
            {
                if (Badges.Norm(bl[i].key) == "talent") hasTalent = true;
                if (Badges.Norm(bl[i].key) == "blast") hasBlast = true;
            }
            Check(!hasTalent, "A1661 ⑤ `talent`（天赋）**不画**成场上徽标（判据 = 用户 2026-10-12 口径）");
            Check(hasBlast && bl.Count == 1,
                  $"A1661 ⑤ 同一张表里别的关键词**照画**（`blast` 在 `InPlayIconKeys` 里、整表 {bl.Count} 条 —— "
                + "不然「整个表都不画」也绿）");

            // ⑥ **7 个位（含图标本体，不只是底板）都不许越出【可见 3D 卡体】的左右边界** ——
            //    🔴 **2026-10-20（`W58`）就地订正**：这里原来的口径是「卡面矩形 ±1.04635」，
            //    还写着一句**前提错误**的注释（「按 3D 网格判 7 个都会超出去 0.0777 ⇒ 不是本笔改出来的」）——
            //    超出去的是**带 `IconOutward` 的旧实现**，原版容器位根本不超。
            //    判据（`资料/普查产出_1020/W58_战斗线_场上词条判据.md` §2·2）：
            //    `Card 3D` localScale **0.88586** × 网格半宽 **1.0482** ⇒ **可见卡体半宽 0.9286**；
            //    图标中心 = 容器 **±0.563**、半宽 `IconSize/2 = 0.228` ⇒ 外缘 **0.791 < 0.9286**（余 0.1376）。
            //    ⚠️ **y 仍用卡面半高 ±1.66565**：7 个容器活在**未缩放的 3DBody 空间**里（`TraitIcons`
            //    是 `3D Body` 的孩子、与 `Card 3D` 平级）⇒ 最上一行的板顶（1.0439）本来就在
            //    缩放后网格顶（0.967）之上；**那是原版几何，不是缺陷**，代码与判据都不该把它判红。
            //    🧨 改坏法：把 `IconOutward` / `IconSize` 调大一档 ⇒ 必红（下面那条「灭自证」也会点名）。
            {
                float k = CardView.CardUnitW / CardView.Width;            // 世界 → 卡单位（今天 = 1，别写死）
                const float visibleBodyHalfW = 0.9286f;                   // 0.88586 × 1.0482
                float hw = visibleBodyHalfW, hh = CardView.CardUnitH * 0.5f;
                float minClear = float.MaxValue, over = 0f;
                string tight = "", worst = "";

                void Fit(string what, int i, float x0, float y0, float x1, float y1)
                {
                    float o = Mathf.Max(0f, Mathf.Max(-x0 - hw, x1 - hw, -y0 - hh, y1 - hh));
                    if (o > 0f) { over = Mathf.Max(over, o); worst += $"〔位{i} {what} 越界 {o:F4}〕"; }
                    float c = Mathf.Min(hw - Mathf.Max(-x0, x1), hh - Mathf.Max(-y0, y1));
                    if (c < minClear) { minClear = c; tight = $"位{i} {what}"; }
                }

                for (int i = 0; i < Badges.MaxSlots; i++)
                {
                    var pt = bv.FaceLayerRoot.Find("badgePlate" + i);
                    var ic = bv.FaceLayerRoot.Find("badgeIcon" + i);
                    var xsv = bv.BadgePlateVertexXs(i);
                    if (pt == null || ic == null || xsv == null || xsv.Length < 2)
                    {
                        Check(false, $"A1661 ⑥ 第 {i} 位的层没建出来 ⇒ 这一格**没验到**（别读成通过）");
                        continue;
                    }
                    // 底板网格烘的是**绝对**坐标（`BadgePlateQuad`）；图标网格**以原点为中心**（`CenteredQuad`）
                    var pb = pt.GetComponent<MeshFilter>().sharedMesh.bounds;
                    var ib = ic.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Fit("底板", i, pb.min.x * k, pb.min.y * k, pb.max.x * k, pb.max.y * k);
                    Fit("图标", i, (ic.localPosition.x - ib.extents.x) * k, (ic.localPosition.y - ib.extents.y) * k,
                                 (ic.localPosition.x + ib.extents.x) * k, (ic.localPosition.y + ib.extents.y) * k);
                }
                Check(over <= 0f,
                      $"A1661 ⑥ 7 个位（**图标本体 + 底板**）都不越出**可见 3D 卡体**左右边界"
                    + $"（半宽 ±{hw:F4}；y 用卡面半高 ±{hh:F4}，理由见源码注释）"
                    + $"；最紧的一处 = {tight} 余 {minClear:F4}" + (over > 0f ? " ⚠️ " + worst : ""));

                // 再钉一条**原版几何**的：图标外缘 = 容器 **0.563** + 图标半宽 **0.228** = **0.791**，
                // 距可见卡体半宽 0.9286 **余 0.1376**（原版 `TraitIconContainer` 就在这儿）。
                // ⚠️ 期望值用**字面量**（0.563 / 0.228），⛔ 不再从 `Badges.IconOutward` 反推 ——
                //    以前那条 `pinned = hw − (|SlotAt| + IconOutward + IconSize/2)` 是自证：
                //    实现与期望同源，把 IconOutward 改回去两边一起变、断言还是绿的。
                // 🧨 改坏法：`IconOutward` / `IconSize` 改大一档 ⇒ 必红。
                float outer = Mathf.Abs(Badges.SlotAt(0).x) + Badges.IconSize * 0.5f;
                Check(Mathf.Abs(outer - 0.791f) < 0.005f && visibleBodyHalfW - outer > 0.13f,
                      $"A1661 ⑥ 图标外缘 = 0.563 + 0.228 = 0.791（实际 {outer:F4}），"
                    + $"距可见卡体半宽 0.9286 余 {visibleBodyHalfW - outer:F4}");

                // 🔴 **灭自证（`W58` 新增）**：直接读【视图里】7 个 `badgeIcon{i}` 的 `localPosition.x`，
                //    与 `Badges.SlotAt(i).x`（= 原版容器位 ±0.563）**逐位相等**。
                //    这一条**结构上不可能同时满足**「把 `IconOutward` 加回来、又同步改上面那条期望值」：
                //    它比的是**原版容器位本身**，不是我们算出来的外缘。
                //    🧨 改坏法：`BadgeIconAt01` 里再加任何外偏 ⇒ 7 位全部点名。
                int badX = 0;
                var badXMsg = new System.Text.StringBuilder();
                for (int i = 0; i < Badges.MaxSlots; i++)
                {
                    var ic = bv.FaceLayerRoot.Find("badgeIcon" + i);
                    if (ic == null) continue;
                    float want = Badges.SlotAt(i).x;
                    if (Mathf.Abs(ic.localPosition.x - want) > 1e-3f)
                    {
                        badX++;
                        badXMsg.Append($"〔位{i} x {ic.localPosition.x:F4} ≠ {want:F4}〕");
                    }
                }
                Check(badX == 0,
                      "A1661 ⑥★ 灭自证：7 个图标的 `localPosition.x` = 原版容器位 `SlotAt(i).x`"
                    + "（⛔ 不是「容器位 + 我们记的外偏」）" + (badX > 0 ? " ⚠️ " + badXMsg : ""));
            }
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- A1676
        //  🔴 2026-10-18：徽标「未激活」= **两条通道叠加**，⛔ 不是二选一。
        //  判据 = 反编译**同一个方法体**里两条一起做（`d:/2/tools/decomp_full/BoardTraitIcon__Initialize.c`）：
        //    `traitIcons`         → `SetMaterial(r, enabled ? originalMaterial : disabledMaterial)`（灰化）
        //    `traitNotActiveIcons`→ `set_enabled(r, !enabled)`（那枚 ✕）
        //  逐条改坏法：
        //    · 删掉 `CardView.SetBadgeCross` 的调用 ⇒ ② 的 ✕ 那条红（灰化仍绿）——两条各管一件事；
        //    · 把 ✕ 改成「激活时才画」或把它并进灰化那条 ⇒ ① 红（激活位不该有 ✕）。
        {
            var root = new GameObject("chk_a1676");
            var bd = CardData.Simple("TraitCrossProbe", 3, 3, 4);
            bd.badges = new List<Badge>
            {
                new Badge { sprite = "armour", counter = 0, active = true  },
                new Badge { sprite = "blast",  counter = 0, active = false },
            };
            var v = CardView.Create(root.transform, bd, "chk_a1676_card");
            Check(v.BadgesShown == 2,
                  $"A1676 前提：两位徽标都建出来了（实际 {v.BadgesShown}）—— 建不出来下面**没验到**");
            bool crossTex = CardArt.Ui("40k_Combat_Icon_Cross") != null;
            Check(crossTex, "A1676 前提：`Art/ui/40k_Combat_Icon_Cross` 在盘上（缺图 ⇒ ✕ 那条没验到）");
            Check(!v.BadgeGreyed(0) && !v.BadgeCrossShown(0),
                  "A1676 ① 激活位：**不灰、不画 ✕**（原版 true 档只给 originalMaterial、`enabled=false`）");
            Check(v.BadgeGreyed(1),
                  "A1676 ② 未激活位：**灰化**（原版 `disabledMaterial` = `Sprite Greyscale`）");
            if (crossTex)
                Check(v.BadgeCrossShown(1),
                      "A1676 ② 未激活位：**同时**画 ✕（原版 `traitNotActiveIcons[].enabled`）"
                    + "｜⚖️ 与上一条**并存**才是原版 —— 判据 `BoardTraitIcon__Initialize.c` 同一个 if 分支里两条都做");
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- 新账-66
        //  🔴 2026-10-21（卡牌线 `新账-66` 收口）：**战术/防御卡插图要盖满卡框的开口**。
        //  判据（原版口径）= `SoftMaskScript` + `FlipAlphaMask=1`：立绘**铺满卡体**、由卡框 alpha
        //  反相遮罩 ⇒ 卡框**围住的透明区**（含 tier3/4 的拱顶/异形开口）都该有立绘。
        //  我们没那层 mask，等价物 = 按卡框 alpha 的**逐行条带**（`CardView.ArtWindowMesh`）。
        //  ⚠️ 这不是「算法自证」：`FrameArtCoverage` 量的是 `ArtMeshInFrame` **真交给渲染器的那块网格**
        //     （把它的三角形逐行投影回来）⇒ 谁把那条路改回矩形，`stripMissPct` 会立刻跳起来。
        //  🧨 改坏法：`ArtMeshInFrame` 的 `clipWindow` 分支删掉条带、只留 `FrameArtWindow` 那个矩形
        //     ⇒ 下面 tier3/tier4 两条**必红**（矩形漏 5~7%）。
        //  🔴 **2026-10-22（`W62/T1`）基线口径改成【逐像素 `hole ∪ ring` ∩ 立绘板 ∩ 卡体】**
        //     （原来那份是「每行开口 min..max」，把同一行里被实心金属隔开的两块开口之间那段金属也当基线
        //      ⇒ 三张 tier4 凭空多出 5.10%～7.37% 的「漏」，逐像素复核那一段全是 alpha 254–255 的金属）。
        //     连带 `rectMiss` 的口径也换成逐像素 ⇒ 阈值从 1f **收紧**到 0.5（ultramarines tier4 只漏 0.99%）。
        {
            // 全 60 张 `_strat` tier 卡框按「矩形会漏多少」排出来的最差几张（离线量法见报告 §2）。
            string[] worstFrames =
            {
                "genestealers|4", "blacklegion|3", "astramilitarum|4", "ultramarines|4",  // 拱顶/异形开口
                "saimhann|1",                                                            // 圆角矩形窗（对照档）
            };
            var art = CardArt.Portrait("UM75");            // 1024² —— 战术卡立绘的**真实形状**（现读 446 张 tactic 里 483 张是 1024²）
            foreach (var spec in worstFrames)
            {
                var parts = spec.Split('|');
                string fac = parts[0];
                int tier = int.Parse(parts[1]);
                var frTex = CardArt.Frame(fac, "", true, tier);
                float rectMiss = 0f, stripMiss = 100f, holePct = 0f;
                int rowCount = 0, quadCount = 0;
                bool ok = frTex != null && CardView.FrameArtCoverage(
                    frTex, art, 1f, out rectMiss, out stripMiss, out holePct,
                    out rowCount, out quadCount);
                Check(ok, $"新账-66 前提：{fac} tier{tier} 的战术卡框读得到、开口量得出来"
                        + (ok ? "（可读）" : " ⚠️ 量不出来 ⇒ 下面几条**没验到**，别读成通过"));
                if (!ok) continue;
                Check(stripMiss <= 0.5f,
                      $"新账-66 {fac} tier{tier}：条带网格盖住开口 **{100f - stripMiss:F2}%**"
                    + $"（基线 = 逐像素 `开口 ∪ 斜面` ∩ 立绘板 ∩ 卡体，占卡面 {holePct:F1}% · {rowCount} 行 / 合并成 {quadCount} 块）"
                    + "｜判据 = 原版 `FlipAlphaMask=1`：不透明金属底下有没有图看不出来 ⇒ 基线不含金属");
                // 这一档的开口是**拱形/异形** ⇒ 矩形方案必须真的漏（否则上一条就没有意义）
                if (tier >= 3)
                    Check(rectMiss > 0.5f,
                          $"新账-66 {fac} tier{tier}：⋯⋯而改前的**矩形**方案会漏 **{rectMiss:F2}%**"
                        + "（`FrameArtWindow` 只是一个内接矩形：拱顶那块填不满，开口一圈的半透明斜面它更是一点没盖"
                        + " —— 这条是上一条的牙齿；逐像素口径下 ultramarines tier4 的**开口部分**只漏 0.99% ⇒ 阈值取 0.5）");
            }
        }

        // ---------------------------------------------------------------- 新账-66 残余（半透明内斜面）
        //  🔴 2026-10-21（`W61b`）：开口一圈还有 **中位 ~11 px（p90 ~20）宽、alpha 从 ~10 缓升到 ~207**
        //  的内斜面 —— 原来条带在 `alpha <= 8` 处**硬切** ⇒ 那圈没有立绘（实测漏 1.5～2.3 万 px/张
        //  ≈ 开口面积的 11%～23%）。原版 `SoftMaskScript` + `FlipAlphaMask=1` 会让立绘按 `1−α` 透出来；
        //  我们的等价物 = 让立绘铺到「与开口 4 连通、alpha 在 9..247 的像素」下面
        //  （`CardView.FrameMaskData` 的 `ring` → `BuildFrameRows` 的 `Runs` → `ArtWindowMesh`）。
        //  判据 / 量法 / 逐档表 → `资料/普查产出_1021/W61b_卡牌_斜面遮罩.md`。
        //  🧨 改坏法：① 把 `ArtWindowMesh` 改回只画开口（去掉 ring）⇒ `missPct` 从 0 跳到 ~90%，
        //              第二/三条红；② 去掉 `AddBevelRun` / `ClampBevelRuns` 里「框外面」的夹取 ⇒ 最后一条红。
        {
            var art = CardArt.Portrait("UM75");            // 1024²，与上一组同一个立绘
            string[] bevelFrames =
            {
                "ultramarines|4", "genestealers|4", "blacklegion|3", "astramilitarum|2", "saimhann|1",
            };
            foreach (var spec in bevelFrames)
            {
                var parts = spec.Split('|');
                string bFac = parts[0];
                int bTier = int.Parse(parts[1]);
                var bFr = CardArt.Frame(bFac, "", true, bTier);
                int ringPx = 0;
                float missPct = 100f, meanErr = 1f, maxErr = 1f, spillPct = 100f;
                bool ok = bFr != null && CardView.FrameArtBevelCoverage(
                    bFr, art, 1f, out ringPx, out missPct, out meanErr, out maxErr, out spillPct);
                Check(ok, $"新账-66(残余) 前提：{bFac} tier{bTier} 的斜面量得出来"
                        + (ok ? "（可读）" : " ⚠️ 量不出来 ⇒ 下面几条**没验到**，别读成通过"));
                if (!ok) continue;
                Check(ringPx > 5000,
                      $"新账-66(残余) {bFac} tier{bTier}：开口一圈量出 **{ringPx}** 个半透明斜面像素（立绘板∩卡体内）");
                Check(missPct <= 0.5f,
                      $"新账-66(残余) {bFac} tier{bTier}：立绘盖住斜面 **{100f - missPct:F2}%**"
                    + "（原版 `FlipAlphaMask=1`：卡框半透明处立绘按 `1−α` 透出来）");
                Check(meanErr <= 0.01f,
                      $"新账-66(残余) {bFac} tier{bTier}：贡献 α ≈ 1−卡框α —— 整圈平均误差 **{meanErr:F4}**"
                    + $"（最大单点 {maxErr:F3} 只作参考、不定阈值：边界上那一个像素的 α 就能把它顶到 ~0.96）");
                Check(spillPct <= 0.5f,
                      $"新账-66(残余) {bFac} tier{bTier}：立绘**没画到卡框外面** —— {spillPct:F2}%"
                    + "（框上的透明缺口一点没碰；判据 = `frame_ultramarines_strat_tier4` 底部 5025 px）");
            }
        }

        // ---------------------------------------------------------------- 新账-66 ②（外发光围的是**卡框**）
        //  用户 2026-10-21 原话 ②：「外面的光芒边缘应该是**围绕着卡框**的，不是在卡框上面围绕插图」。
        //  判据（原版一手）：那圈可打出/高亮的光 = SDF `_Outline`，画在
        //  `Card2DController.frameHighlightAndShadow`（节点 `Card Highlight And Shadow`，**4.4281²**
        //  —— 比卡体 2.0927×3.3313 大得多）；它的 sprite 就是 `CardFramesSO.GetClanFrame` 的**第二项**：
        //  `stratagemHighlightFrame`（战术/防御卡，`CardTypeOptions.Tactic=20`）/
        //  `troopHighlightFrame`（部队·督军，`Minion=0`/`Hero=10`）
        //  （`CardTierUIController__SetTier.c` → `Card2DController` +0x40；
        //    判据全文 = `资料/普查产出_0919/卡面SDF软光影_查证.md:16-25`）。
        //  ⇒ 那圈**只能**落在卡框外沿（`_Outline` 由 `Card2DController.ChangeHighlightColor` 上色）。
        //  量法 = **渲两张逐像素差**（描边关 / 描边开白 α=1）⇒ 差集包围盒 = 真上屏的发光区。
        //  🧨 改坏法 ①：`OutlineMat` 退回立绘层（`_art` 的网格被 `ArtWindowMesh` 裁在开口里）
        //     ⇒ 光带最外沿 = 插图画窗外沿 ⇒ **左右两条必红**；
        //  🧨 改坏法 ②：把 `ShadowSize` / `ShadowY` 改小或挪边（等值带就会落回插图画窗 —— 用户
        //     2026-10-21 报的那一档）⇒ 同样红。
        //     ⚠️ 2026-10-24（L1 · `C1-B03` 并层）：原来这里写的是「把 `OutlineGlowScale` 调小」——
        //     **那个常量已删**（描边与软光/影并成一层，几何只有一份 `ShadowMesh()`/`ShadowSize`）。
        {
            CheckOutlineGlow("Armoured Offensive");   // UM75 —— 战术卡
            CheckOutlineGlow("Alien Idol");           // GSC63 —— 防御卡
        }

        // ---------------------------------------------------------------- W62/T2 卡面拉丁字号 = 原版 em
        //  🔴 2026-10-22（`W62/T2`）：卡面这几档渲的是 **Asar / Pragati**，字号却一直拿**汉字墨高**
        //  标定（`TmpFont.FontSizeForGlyphHeight` → `WorldGlyphPerFontSize`，量的是「国」）。
        //  CJK 资产的 `m_Scale` 一改（2026-10-21 对齐原版：1 → 0.9），这些**拉丁**字就跟着变大 **11%**
        //  （`资料/普查产出_1022/W62_卡面红诊断.md` §2·2/§2·3）。
        //
        //  判据（原版，逐字段实读，不是我们挑的）：
        //    · 卡名 `NameTextUnit`：`m_fontSize = 19` × `m_LocalScale 0.01` ⇒ **em 0.19 卡单位**
        //      （`bundle_battleprefabs_vfxandmisc_assets_all/RectTransform/RectTransform_4173703600236829632.json`
        //       的 `m_LocalScale` = 0.01 ＋ 同包 `MonoBehaviour_-6860425428091954240.json` 的 `m_fontSize` = 19）
        //    · 效果文字 `DescTextUnit` 23.55 → **0.2355** · 阵营 `ArmyTextUnit` 15 → **0.15** · 兵种 `RaceText` 18 → **0.18**
        //      （同包三份 GameObject → MonoBehaviour：`m_fontSize` 依次 23.55 / 15 / 18，`m_LocalScale` 全 0.01）
        //    · 卡高 `2DCard.m_SizeDelta.y` = **3.3313** ⇒ 5.70% / 7.07% / 4.50% / 5.40%
        //    · 🔴 **原版卡面 TMP 是 `m_isOrthographic = 1`**（`MonoBehaviour_-6860425428091954240.json`），
        //      而我们的卡面是 3D `TextMeshPro`（出厂 **false**）⇒ 两边同一条 TMP 换算律
        //      `fontScale = fontSize / pointSize × face.scale × (isOrtho ? 1 : 0.1)`
        //      （`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Text.cs:7183`）里那一跳**不一样**：
        //      · 原版：em = `fontSize × face.scale × localScale` = 19 × 1 × 0.01 = **0.19**
        //      · 我们：em = `0.1 × fontSize × face.scale` ⇒ 要让 em = 0.19，**fontSize 必须是 1.9**
        //      （同族先例：`Battle/PopNumber.cs` 头部记的「少了 isOrthographic 那一跳 ⇒ 小 10 倍」）。
        //  量法：`fontSize × 0.1 × face.scale` 必须 == 原版 em，容差 1%。
        //  🧨 改坏法：把任一条改回 `FontSizeForGlyphHeight`（汉字墨高 0.0853）⇒ 读数 ×1.172 ⇒ **红**。
        //  ⚠️ 为什么单开这一条：在此之前**全仓 0 条断言量卡面字号**（本次日志里一条「字号」都没有），
        //     字号回归只被 `CardBaseDemo` ② 间接抓到 —— 而它量的是行盒心、容差 0.02，只抓到一半。
        {
            var faUi = TmpFont.FaceAsset(TmpFont.Face.LatinUi);
            var faBody = TmpFont.FaceAsset(TmpFont.Face.LatinCardBody);
            Check(faUi != null && faBody != null,
                  "W62/T2 前提：Asar / Pragati 两份字体资产加载得到（缺 ⇒ 下面四条**没验到**，别读成通过）");
            if (faUi != null && faBody != null)
            {
                // 拉丁面：1 em = 0.1 × fontSize × face.scale（TMP 的 3D 换算，见上面那段）
                float kUi = 0.1f * faUi.faceInfo.scale;
                float kBody = 0.1f * faBody.faceInfo.scale;
                float cardH = CardView.Height;                  // 3.3313 = 原版 `2DCard.m_SizeDelta.y`
                CheckFaceSize("卡名", "NameTextUnit", 19f, kUi, CardView.TitleFontSize, cardH);
                CheckFaceSize("效果文字", "DescTextUnit", 23.55f, kBody, CardView.KeywordFontSize, cardH);
                CheckFaceSize("阵营行", "ArmyTextUnit", 15f, kBody, CardView.ArmyFontSize, cardH);
                CheckFaceSize("兵种行", "RaceText", 18f, kBody, CardView.RaceFontSize, cardH);
            }
        }

        // ---------------------------------------------------------------- 新账-67
        //  🔴 2026-10-21：`SetFace(Board)` 必须**复位**整卡倾摆。
        //  `AutoCardRotation` 只在 `Update` 里写 `_tiltRoot.localRotation`，`enabled=false` 只表示
        //  「不再更新」⇒ 手牌**正倾着**（拖拽时 ≤±10°）被打出去，那份倾摆会永久留在节点上。
        //  画面上看不出（场上 2D 层全关），但 `HitFrame()` 的标架就是 `_tiltRoot` ⇒ 命中框跟着歪。
        //  🧨 改坏法：删掉 `SetFace` 里那句 `_tiltRoot.localRotation = Quaternion.identity` ⇒ 本条必红。
        {
            var root = new GameObject("chk_67_tilt");
            try
            {
                var v = CardView.Create(root.transform, data, "chk_67_card", CardFace.Hand);
                var tilt = v.Tilt;
                Check(tilt != null && v.FaceLayerRoot != null && v.FaceLayerRoot != v.transform,
                      "新账-67 前提：倾摆组件与 `2DCard` 那一级都在（缺 ⇒ 下面几条没验到）");
                if (tilt != null && v.FaceLayerRoot != null && v.FaceLayerRoot != v.transform)
                {
                    // 模拟「拖到一半」：`AutoCardRotation` 写的就是**这个节点自己的** `localRotation`
                    var fanRot = v.transform.localRotation;                 // 卡根的扇形角（布局写的）
                    v.FaceLayerRoot.localRotation = Quaternion.Euler(6f, 8f, 0f);
                    float before = Quaternion.Angle(v.FaceLayerRoot.localRotation, Quaternion.identity);
                    Check(before > 5f, $"新账-67 前提：先把倾摆**拧到 {before:F1}°**（不拧就验不到复位）");
                    v.SetFace(CardFace.Board);
                    float after = Quaternion.Angle(v.FaceLayerRoot.localRotation, Quaternion.identity);
                    Check(after < 0.01f,
                          $"新账-67 `SetFace(Board)` 把倾摆**复位成 identity**（{before:F1}° → {after:F3}°）");
                    Check(!tilt.enabled, "新账-67 ⋯⋯倾摆组件在场上也**关着**（原版场上整张 `2DCard` 关掉）");
                    Check(Quaternion.Angle(v.transform.localRotation, fanRot) < 0.01f,
                          "新账-67 ⋯⋯复位只动 `2DCard` 这一级，**卡根的扇形角没被动**");
                }
            }
            // ⚠️ `finally`：这张卡若半路抛异常，**必须**从场景里清掉 ——
            //    `Run()` 的 `CheckLayers()` 之后还要接着渲图，留在原点的卡会出现在每一张截图里。
            finally { Object.DestroyImmediate(root); }
        }

        // ---------------------------------------------------------------- A7 · 原版立绘硬矩形 `_Clamp_Range`
        //  判据（一手）→ `CardView.ClampRangeSmall` 的 doc（`CardImageController.cctor` 的两个 Vector4 +
        //  `Everguild/Card ImageUI` 片元把它当**贴图 UV 的硬矩形**：矩形外 α=0）。
        //  这条**只在工程里有真立绘时**验得动（`Resources/Art/` 是本地件）⇒ 拿不到图时按本工程的规矩
        //  **出声、不读成通过**。
        CheckArtClampRange(pool);

        // ---------------------------------------------------------------- D3 · 大图档 ×1.27 只给 Minion/Hero
        //  判据（一手）→ `CardView.BigArtScaleApplies` 的 doc
        //  （`CardImageController__SetImage.c:31,79,110-118` + `CardImage.json` 的 `bigSizeMultiplier` = 1.2699999809）。
        CheckBigArtScaleGate(pool);

        // ---------------------------------------------------------------- P1-3 · opt-in 小图档前景层裁到卡框开口
        //  判据（一手）→ `Core/CardView.Build` 建 `_artFront` 小图档那一支（`CardView.cs:3748-3752`）。
        //  片段来源 = `资料/普查产出_1024/F1_审查修复卡面与断言.md` §三（`F1` 没权限改本文件）。
        CheckFrontLayerSmallClipsToWindow(pool);

        // ---------------------------------------------------------------- F4 · 硬矩形也要夹「逐行开口条带」
        //  `F1` §六·1：条带那条早退路跳过了「与硬矩形取交集」⇒ 非单位卡上前景层的 `_Clamp_Range` 空转。
        CheckArtClampReachesStrip(pool);

        Debug.Log($"[cardface-chk] === 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    // ==================================================================
    //  A7（2026-10-24）：原版立绘硬矩形 `_Clamp_Range`
    // ==================================================================
    //  **它是什么**：原版 `CardImage` 的材质（`Card Image Overflow`）挂的 shader `Everguild/Card ImageUI`
    //  在片元里只对 α 做一件事 —— `α *= (z < uv.x ≤ x && w < uv.y ≤ y) ? 1 : 0`（uv = **贴图 UV**）。
    //  真值 = `CardImageController.cctor` 的两个 Vector4（见 `CardView.ClampRangeSmall` 的 doc）。
    //
    //  **两族数字不同**（这是最容易被读错的一处）：原版卡图贴图统一 1024²，但 sprite rect 有两族 ——
    //  单位/督军 670.5×1024 @x176.5（⇒ 贴图 UV (0.18,0.81] 只等于图宽两端各切 1.2%/2.6%）、
    //  战术/防御 整张 1024²（⇒ 就是 (0.18,0.81]）。⇒ 换算到我们那张 PNG（= sprite rect 的裁剪）后
    //  两族的**局部 UV 窗口不同**。期望值下面全是**常数**，⛔ 不是把 `ArtClampUv01` 再算一遍。
    static void CheckArtClampRange(List<CardDef> pool)
    {
        var defU = FindByName(pool, "Heavy Intercessor");      // UM78 —— 单位卡
        var defT = FindByName(pool, "Armoured Offensive");     // UM75 —— 战术卡
        if (defU == null || defT == null)
        {
            Check(false, "A7 前提：卡池里缺 `Heavy Intercessor` / `Armoured Offensive` ⇒ "
                       + "硬矩形这一段**什么都没验到**（别读成通过）");
            return;
        }
        var artU = CardArt.Portrait(BattleDriver.ArtKey(defU));
        var artT = CardArt.Portrait(BattleDriver.ArtKey(defT));
        if (artU == null || artT == null)
        {
            Check(false, "A7 前提：样本立绘加载不到（`Resources/Art/` 不在？）⇒ "
                       + "硬矩形这一段**什么都没验到**（别读成通过）");
            return;
        }
        Check(artU.height == 1024 && artU.width < 1024,
              $"A7 前提：单位卡立绘 = 原版 sprite rect 的裁剪（实测 {artU.width}×{artU.height}；原版 670.5×1024 @x176.5）");
        Check(artT.width == 1024 && artT.height == 1024,
              $"A7 前提：战术卡立绘 = 整张 1024²（实测 {artT.width}×{artT.height}）");

        // ---- ② 值：原版贴图 UV → 我们这张 PNG 的局部 UV ----
        //  单位卡：uMin = 176.5/1024 = 0.172363 · span = 670.5/1024 = 0.654785；v 满高 ⇒ 局部 == 贴图
        //    SMALL（**产品值 0.82**）(0.18,0.82] → (0.0117,0.9883] · BIG (0.25,0.75] → (0.1185,0.8820]
        //    ⚠️ cctor 的 SMALL 是 0.81（→ 0.9731），只在 Big→Small 之后出现；产品路径用材质默认 0.82，
        //       见 `CardView.ClampRangeSmall` 的 doc（两档差 1% 图宽、都在框的金属底下）。
        Vector4 w;
        bool ok = CardView.ArtClampWindowForTest(artU, false, out w);
        Check(ok && Mathf.Abs(w.x - 0.0117f) < 0.004f && Mathf.Abs(w.y - 0.9883f) < 0.004f,
              "A7 ② 单位卡·小图档 u 窗口 = 原版贴图 UV (0.18,0.82] 经 sprite rect 换算 ≈ **(0.0117,0.9883]**"
            + $"（实测 ({(ok ? w.x : float.NaN):F4},{(ok ? w.y : float.NaN):F4})）"
            + "｜🧨 把贴图 UV 直接当局部 UV（K1 §五·2 那条「中间 64%」的读法）⇒ 实测 (0.180,0.820) ⇒ 红");
        Check(ok && Mathf.Abs(w.z) < 0.002f && Mathf.Abs(w.w - 1f) < 0.002f,
              $"A7 ② 单位卡·小图档 v 窗口 = 原版 (0,1]（实测 ({(ok ? w.z : float.NaN):F4},{(ok ? w.w : float.NaN):F4})）");
        ok = CardView.ArtClampWindowForTest(artU, true, out w);
        Check(ok && Mathf.Abs(w.x - 0.1186f) < 0.004f && Mathf.Abs(w.y - 0.8822f) < 0.004f,
              "A7 ② 单位卡·大图档 u 窗口 = 原版 (0.25,0.75] 经 sprite rect 换算 ≈ **(0.1186,0.8822]**"
            + $"（实测 ({(ok ? w.x : float.NaN):F4},{(ok ? w.y : float.NaN):F4})）｜🧨 两档用同一个值 ⇒ 红");
        Check(ok && Mathf.Abs(w.z - 0.10f) < 0.002f && Mathf.Abs(w.w - 0.89f) < 0.002f,
              $"A7 ② 单位卡·大图档 v 窗口 = 原版 (0.10,0.89]（实测 ({(ok ? w.z : float.NaN):F4},{(ok ? w.w : float.NaN):F4})）");
        //  战术卡：sprite rect = 整张 ⇒ 局部 UV == 贴图 UV
        ok = CardView.ArtClampWindowForTest(artT, false, out w);
        Check(ok && Mathf.Abs(w.x - 0.18f) < 0.002f && Mathf.Abs(w.y - 0.82f) < 0.002f
                 && Mathf.Abs(w.z) < 0.002f && Mathf.Abs(w.w - 1f) < 0.002f,
              "A7 ② 战术卡·小图档窗口 = 原版 **(0.18,0.82]×(0,1]**（sprite rect = 整张 1024²）"
            + $"（实测 ({(ok ? w.x : float.NaN):F4},{(ok ? w.y : float.NaN):F4})×({(ok ? w.z : float.NaN):F4},{(ok ? w.w : float.NaN):F4})）");
        ok = CardView.ArtClampWindowForTest(artT, true, out w);
        Check(ok && Mathf.Abs(w.x - 0.25f) < 0.002f && Mathf.Abs(w.y - 0.75f) < 0.002f
                 && Mathf.Abs(w.z - 0.10f) < 0.002f && Mathf.Abs(w.w - 0.89f) < 0.002f,
              "A7 ② 战术卡·大图档窗口 = 原版 **(0.25,0.75]×(0.10,0.89]**"
            + $"（实测 ({(ok ? w.x : float.NaN):F4},{(ok ? w.y : float.NaN):F4})×({(ok ? w.z : float.NaN):F4},{(ok ? w.w : float.NaN):F4})）");

        // ---- ③ 网格：**真交给渲染器的那块**（与 `Build` 同一个 `ArtMeshInFrame` 缓存键）----
        var mBig = CardView.ArtMeshForTest(artU, CardView.BigArtScale, null, false, 2);
        var mOff = CardView.ArtMeshForTest(artU, CardView.BigArtScale, null, false, 0);
        bool meshOk = mBig != null && mOff != null && mBig.vertexCount == 4 && mOff.vertexCount == 4;
        Check(meshOk, "A7 ③ 前提：大图档立绘网格两档都建出来了（4 顶点 quad）");
        if (meshOk)
        {
            var bv = mBig.vertices; var bu = mBig.uv;
            var ov = mOff.vertices; var ou = mOff.uv;
            float bu0 = MinComp(bu, 0), bu1 = MaxComp(bu, 0), bvv0 = MinComp(bu, 1), bvv1 = MaxComp(bu, 1);
            float ou0 = MinComp(ou, 0), ou1 = MaxComp(ou, 0), ovv0 = MinComp(ou, 1), ovv1 = MaxComp(ou, 1);
            Check(Mathf.Abs(bu0 - 0.1186f) < 0.006f && Mathf.Abs(bu1 - 0.8822f) < 0.006f,
                  $"A7 ③ 大图档立绘网格 u ∈ [{bu0:F4},{bu1:F4}] = 原版硬矩形 (0.1186,0.8822]"
                + "｜🧨 把硬矩形只写在常量里、没接到网格上 ⇒ 这里会是关档那组值 ⇒ 红");
            Check(Mathf.Abs(bvv0 - 0.10f) < 0.006f && Mathf.Abs(bvv1 - 0.89f) < 0.006f,
                  $"A7 ③ 大图档立绘网格 v ∈ [{bvv0:F4},{bvv1:F4}] = 原版硬矩形 (0.10,0.89]");
            // 灭自证：关档必须**严格更大**（否则「把 clamp 删了」也能过上面两条）
            Check(ovv0 < bvv0 - 0.02f && ovv1 > bvv1 + 0.02f && ou0 < bu0 - 0.02f && ou1 > bu1 + 0.02f,
                  $"A7 ③ 灭自证：关档窗口 u∈[{ou0:F4},{ou1:F4}] v∈[{ovv0:F4},{ovv1:F4}] **严格大于**开档"
                + "（两档若相同 ⇒ clamp 被删掉也会全绿，这条就是挡它的）");
            // 「裁的是画面，不是拉伸」：用**关档**反解板位，再用它预测**开档**的顶点位置
            float wBoard = (MaxV3(ov, 0) - MinV3(ov, 0)) / Mathf.Max(1e-6f, ou1 - ou0);
            float xL0 = MinV3(ov, 0) - ou0 * wBoard;
            float hBoard = (MaxV3(ov, 1) - MinV3(ov, 1)) / Mathf.Max(1e-6f, ovv1 - ovv0);
            float yB0 = MinV3(ov, 1) - ovv0 * hBoard;
            float predL = xL0 + bu0 * wBoard, predR = xL0 + bu1 * wBoard;
            float predB = yB0 + bvv0 * hBoard, predT = yB0 + bvv1 * hBoard;
            Check(Mathf.Abs(predL - MinV3(bv, 0)) < 0.01f && Mathf.Abs(predR - MaxV3(bv, 0)) < 0.01f
                  && Mathf.Abs(predB - MinV3(bv, 1)) < 0.01f && Mathf.Abs(predT - MaxV3(bv, 1)) < 0.01f,
                  "A7 ③ 硬矩形**裁的是画面、不是拉伸**：关档反解板位预测开档顶点 x"
                + $" [{predL:F4},{predR:F4}] y [{predB:F4},{predT:F4}]，实测 x [{MinV3(bv, 0):F4},{MaxV3(bv, 0):F4}]"
                + $" y [{MinV3(bv, 1):F4},{MaxV3(bv, 1):F4}]（差 <0.01）"
                + "｜🧨 只改 UV 不裁几何（= 把立绘拉扁）⇒ 红");
        }

        // ---- ④ 小图态逐像素（④a 生产口径**不变** · ④b opt-in 前台层上**真的生效**且只切硬矩形之外）----
        try { CheckClampSmallPixelsUnchanged(defU, "单位卡 `Heavy Intercessor`", true, artClamped: false); }
        catch (System.Exception e)
        {
            _fail++;
            Debug.LogError("[cardface-chk]   ✗ A7 ④ 单位卡渲图对照抛异常（**没验到，别读成通过**）："
                         + $"{e.GetType().Name}: {e.Message}");
        }
        // 战术卡那一档单跑 ④a —— `H1`（2026-10-24）起生产 `_art` 也套小图档硬矩形 ⇒ 这一条断「**必须变**」。
        // 🔴 **2026-10-24（`H3`）就地订正**：本行原来写「它的**拱窗比两族硬矩形都宽**（开口 ±0.75 vs 硬矩形 ±0.714）
        // ⇒ 一旦有人把 clampKind 接到生产 `_art` 上，这张卡**当场就红**」——**两处都不成立**：
        //   ① 小图档硬矩形是 **±0.9147**（`0.32 × 2.7484 × 1.04`），不是 ±0.714（±0.714 是大图档的 x 半宽）；
        //   ② 这张卡**自己的**卡框开口（`frame_ultramarines_strat_tier1/2` —— 默认等级 1）只有
        //      x ∈ [−0.848, +0.838]，**整个都在硬矩形里** ⇒ 接上 `clampKind` 也**不会**红
        //      （离线复刻 `FrameMaskData`/`BuildFrameRows` 的 `hole ∪ ring` 实测：越界像素 **0**；
        //        tier3 也只有 **1** 个 hole 像素越界 ⇒ 仍属「夹了等于没夹」）。
        // ⇒ 现在由 `CheckClampSmallPixelsUnchanged` **自己把卡框档位钉到 tier4**
        //   （`frame_ultramarines_strat_tier4` 的 `hole ∪ ring` x ∈ [−0.8148, +1.0321]，右侧越界 ≈0.117 卡单位），
        //   并在里面按 `artClamped` 分两种判据：单位卡 = 差集 0 · 战术卡 = 差集 > 0 且全在硬矩形之外。
        // 战术卡 ④a：🔴 **2026-10-24（第七十三会话 `M2`）翻面** —— `_art` 的硬矩形档位**恒 0**
        //   （原版实况：战术/防御卡插图铺满拱窗、无黑边；判据 → `CardView.ArtClampKindFor` 的 doc）。
        //   ⇒ 战术卡与单位卡同判：生产小卡档「硬矩形开/关」**逐像素不变**（差集 0）。
        //   🧨 谁把 `clampKind` 接回 1/2 ⇒ 差集 > 0 ⇒ 立刻红（这就是本条的判别力）。
        try { CheckClampSmallPixelsUnchanged(defT, "战术卡 `Armoured Offensive`", false, artClamped: false); }
        catch (System.Exception e)
        {
            _fail++;
            Debug.LogError("[cardface-chk]   ✗ A7 ④ 战术卡渲图对照抛异常（**没验到，别读成通过**）："
                         + $"{e.GetType().Name}: {e.Message}");
        }

        // ---- ④c 防御卡（引擎 `type="defence"`；🔴 SO `cardType` 现读也是 **20**、不是 5）—— 🆕 2026-10-24（`H6`）补的样卡 ----
        //  🔴 覆盖面缺口：④a/④b 原来只跑「单位卡 + 战术卡」。防御卡与战术卡在 `CardView` 里走**同一条**
        //    `!d.isUnit` 分支（`ArtClampKindFor` ⇒ `clampKind: 1/2`），逻辑上是同一条路 —— 但**一直没有样卡**
        //    （`H1` §五·6 / `H3` §四·3 / `H4` §四·5 都记着这个缺口）⇒ 防御卡那条支路回归了没人报。
        //  原版判据（一手）同样只核过 tactical 那一支的参数：`CardImageController__SetImage.c:126-144`
        //    的 `param_3 == 0x14(Tactic)` 分支只关 `frameMask`，而 `Image.sprite = sprite` 在 `:174-177`
        //    **无条件执行**；`ChangeState`/`DoChange` 对卡类型没有任何判断 ⇒ 防御卡与战术卡同一套硬矩形。
        //  样本 = `Sacred Altar`（**SOR59** · Sororitas · 防御卡 · `art_sor59.png` 实测 **1024²**）；
        //    ⛔ **不是拿战术卡顶替**（`def.Type` 逐字断）。卡池现读 `type = "defence"` **39 张**。
        //  档位：沿用 `TacticClampProbeFrameTier = 4` ⇒ `frame_sororitas_strat_tier4`。
        //    **离线依据**（口径与 `H4` 那份逐字同一套：`FrameMaskData` / `BuildFrameRows` / `ArtWindowMesh`，
        //    `clampKind = 0`）现读 mesh x ∈ **[−0.846065, +0.849357]**（= 卡框画窗边；立绘板 ±0.871792 更宽）
        //    ⇒ **两侧都越出**硬矩形 ±0.557947（左 0.2881 / 右 0.2914）⇒ 差集必须 > 0，且改动全在矩形之外。
        //    🔴 **2026-10-24（`W4` · 本线 T02）就地订正（铁律 5）**：本行原写
        //      「mesh x ∈ [−1.0403, +1.0463] ⇒ 两侧都越出硬矩形 **±0.914668**（左 0.1256 / 右 0.1316）」，
        //      那是 **`E9` 之前**的口径 —— 那时战术/防御卡的立绘板没乘 `cardScales 0.61`、中心也没加
        //      `tacticsOffsetPosition +0.5`，硬矩形端点才是 ±0.914668、外壁才是卡体 ±1.04635。
        //      ⚠️ 中间还流传过一版 **0.313845**（= 板边 0.871792 − 硬矩形 0.557947）—— **那一版也不对**：
        //      关档后的 x 极值**不是板边、而是这张卡框的画窗边**（sororitas `strat_tier4` 画窗
        //      x ∈ [−0.846065, +0.849357]，比板 ±0.871792 **窄**）⇒ 越出量是 **0.2881 / 0.2914**。
        //      判据与同一条卡/同一档的运行时读数 → `CardBaseDemo.AssertArtClampForSample` 的 ③ / ③b
        //      （`CardBaseDemo.cs` 里那段 E12 订正：astra 0.2815 / sororitas 0.2914 与离线数逐位吻合）。
        //      🔴 另注：本段描述的 `artClamped = true` 分支自第七十三会话 `M2` 起**已无调用点**
        //      （`_art` 恒不套硬矩形），这段保留作「万一要把硬矩形接回 `_art`」时的现成说明。
        //    选 Sororitas 而不与战术卡同阵营：**独立卡框资产**，能把「这张框恰好够宽」排除掉
        //    （13 个阵营 `strat_tier4` 的现读表 → `CardBaseDemo.AssertArtClampForSample` 的 doc）。
        var defD = FindByName(pool, "Sacred Altar");
        Check(defD != null && defD.Type == "defence",
              "A7 ④c 前提：卡池里有防御卡样本 `Sacred Altar`（SOR59，`type == \"defence\"`）"
            + $"（实测 `{(defD == null ? "<缺卡>" : defD.Type)}`）"
            + " —— 缺 ⇒ 防御卡那一条**没验到**（不是通过）");
        if (defD != null && defD.Type == "defence")
        {
            // 🔴 **2026-10-24（第七十三会话 `M2`）翻面**：防御卡与战术卡同走「不套」⇒ 差集 0。
            try { CheckClampSmallPixelsUnchanged(defD, "防御卡 `Sacred Altar`", false, artClamped: false); }
            catch (System.Exception e)
            {
                _fail++;
                Debug.LogError("[cardface-chk]   ✗ A7 ④ 防御卡渲图对照抛异常（**没验到，别读成通过**）："
                             + $"{e.GetType().Name}: {e.Message}");
            }
        }
    }

    // ==================================================================
    //  D3（2026-10-24）：大图档 ×1.27 **只给 Minion(0) / Hero(10)**
    // ==================================================================
    //  判据（一手）→ `CardView.BigArtScaleApplies` 的 doc：`CardImageController__SetImage.c:31,79,110-118`
    //  —— `fVar12` 先设 **1.0f**（`read_literal` 现读 `0x1834b2bb8` = 4B float 1），只有
    //  `currentState == 1 && (param_3 == 10 || param_3 == 0)` 才取 `bigSizeMultiplier`
    //  （`CardImage.json` 现读 **1.2699999809**）⇒ 战术(20) / 防御 / 其余一律 ×1.0。
    //
    //  ⚠️ **量的是真建出来的大图档卡的 `_artFront` 网格**（`ArtFrontMeshForTest`），
    //  ⛔ 不是把 `BigArtScale` 那个常量读一遍（那是自证）。参考网格由公开的
    //  `ArtMeshForTest(art, S, null, false, 2)` 按 **S = 1.0 / 1.27** 各建一份，**逐顶点比** ⇒
    //  「这张卡用了哪个倍率」是**量出来的**。两条都是「等于 A 且不等于 B」的双向判据 ——
    //  只断一边的话「倍率被删掉 / 两档撞成同一份」也能过。
    static void CheckBigArtScaleGate(List<CardDef> pool)
    {
        var defU = FindByName(pool, "Heavy Intercessor");       // UM78 单位卡（Minion 0）
        var defT = FindByName(pool, "Armoured Offensive");      // UM75 战术卡（Tactic 20）
        var defD = FindByName(pool, "Aspect Shrine");           // ASH55 防御卡（`CardDef.IsUnit` = false）
        if (defU == null || defT == null || defD == null)
        {
            Check(false, "D3 前提：卡池里缺 `Heavy Intercessor` / `Armoured Offensive` / `Aspect Shrine` "
                       + "⇒ 大图档放大门控这一段**什么都没验到**（别读成通过）");
            return;
        }
        var artU = CardArt.Portrait(BattleDriver.ArtKey(defU));
        var artT = CardArt.Portrait(BattleDriver.ArtKey(defT));
        var artD = CardArt.Portrait(BattleDriver.ArtKey(defD));
        if (artU == null || artT == null || artD == null)
        {
            Check(false, "D3 前提：三张样本立绘加载不到（`Resources/Art/` 不在？）⇒ "
                       + "这一段**什么都没验到**（别读成通过）");
            return;
        }

        // 真卡（大图档 = `CardUse.ZoomWindow`，`IsBigCardView` 的第一支）
        var realU = BuildBigFrontMesh(defU, "unit");
        var realT = BuildBigFrontMesh(defT, "tactic");
        var realD = BuildBigFrontMesh(defD, "defence");
        if (realU == null || realT == null || realD == null)
        {
            Check(false, "D3 前提：大图档前景层 `_artFront` 没建出来（卡框 / 真立绘 / `BigCardOverdraw` 缺一个）"
                       + $"⇒ 这一段**什么都没验到**（别读成通过）｜unit={realU != null} tactic={realT != null} "
                       + $"defence={realD != null}");
            return;
        }

        var u127 = CardView.ArtMeshForTest(artU, CardView.BigArtScale, null, false, 2);
        var u100 = CardView.ArtMeshForTest(artU, 1f, null, false, 2);
        var t127 = CardView.ArtMeshForTest(artT, CardView.BigArtScale, null, false, 2,
                                           CardView.TacticArtScale, CardView.TacticArtDy);
        var t100 = CardView.ArtMeshForTest(artT, 1f, null, false, 2,
                                           CardView.TacticArtScale, CardView.TacticArtDy);
        var d127 = CardView.ArtMeshForTest(artD, CardView.BigArtScale, null, false, 2,
                                           CardView.TacticArtScale, CardView.TacticArtDy);
        var d100 = CardView.ArtMeshForTest(artD, 1f, null, false, 2,
                                           CardView.TacticArtScale, CardView.TacticArtDy);

        // ① Minion：仍然 ×1.27
        Check(MeshNear(realU, u127, 1e-3f) && !MeshNear(realU, u100, 1e-3f),
              "D3 ① 单位卡（Minion）大图档立绘**仍然是 ×1.27** —— 真卡 `_artFront` 网格逐顶点 = `1.27` 那份，"
            + "且 ≠ `1.0` 那份"
            + $"（实测 {MeshSizeText(realU)}；1.27 参考 {MeshSizeText(u127)}；1.0 参考 {MeshSizeText(u100)}）"
            + "｜🧨 顺手把单位卡也挡掉（倍率恒 1）⇒ 与 1.0 那份相同 ⇒ 红");

        // ② Tactic：不放大（×1.0），但硬矩形**仍然是大图档那一组**（参考网格就是 `clampKind: 2` + S=1.0）
        Check(MeshNear(realT, t100, 1e-3f) && !MeshNear(realT, t127, 1e-3f),
              "D3 ② 战术卡（Tactic 20）大图档立绘 = **×1.0（不放大）** —— 真卡 `_artFront` 网格 = `1.0` 那份"
            + "（`clampKind` 仍是大图档 2），且 ≠ `1.27` 那份"
            + $"（实测 {MeshSizeText(realT)}；1.0 参考 {MeshSizeText(t100)}；1.27 参考 {MeshSizeText(t127)}）"
            + "｜🧨 门控没接上（所有类型都乘）⇒ 与 1.27 那份相同 ⇒ 红；"
            + "把硬矩形档位也跟着降到小图档 ⇒ 与两份都对不上 ⇒ 红");

        // ③ Defence：同 ②
        Check(MeshNear(realD, d100, 1e-3f) && !MeshNear(realD, d127, 1e-3f),
              "D3 ③ 防御卡（`CardDef.IsUnit` = false）大图档立绘 = **×1.0（不放大）** —— 真卡 `_artFront` 网格 = `1.0` 那份"
            + $"（实测 {MeshSizeText(realD)}；1.0 参考 {MeshSizeText(d100)}；1.27 参考 {MeshSizeText(d127)}）"
            + "｜🧨 同 ②；另外把门控写成「只挡 tactic、漏了 defence」⇒ 这里红");

        // ④ 灭自证：**把门控去掉**（`DebugDisableBigArtScaleGate` = 一律乘）⇒ 战术/防御卡必须真的变成 ×1.27
        //    —— 这一条证明 ②③ 有牙：不拆门控时它们量到 1.0，拆了门控它们量到 1.27（②③ 会因此变红）。
        var gateT = BuildBigFrontMesh(defT, "tactic_nogate", gateOff: true);
        var gateD = BuildBigFrontMesh(defD, "defence_nogate", gateOff: true);
        Check(gateT != null && gateD != null
              && MeshNear(gateT, t127, 1e-3f) && !MeshNear(gateT, t100, 1e-3f)
              && MeshNear(gateD, d127, 1e-3f) && !MeshNear(gateD, d100, 1e-3f),
              "D3 ④ 灭自证：把类型门控去掉（`DebugDisableBigArtScaleGate` = true）⇒ 战术/防御卡的大图档立绘"
            + "**真的变成 ×1.27**（= 2026-10-24 之前的旧行为，②③ 会因此变红）"
            + $"（战术 {MeshSizeText(gateT)} vs 1.27 {MeshSizeText(t127)}；防御 {MeshSizeText(gateD)} vs 1.27 {MeshSizeText(d127)}）"
            + "｜🧨 门控根本没接上（拆不拆都一样）/ 只改了常量 ⇒ 这里量不到变化 ⇒ 红");
    }

    // ==================================================================
    //  P1-3（2026-10-24 · `F1` 片段由 `F4` 粘贴）：opt-in 小图档前景层要裁到卡框开口
    // ==================================================================

    /// <summary>🆕 2026-10-24（`F1` · `R1` P1-3）：**opt-in 小图档前景层要裁到卡框开口（战术/防御卡）**。
    ///
    /// <para>判据 = `Core/CardView.Build` 建 `_artFront` 时把 `frameTex` 与 `clipWindow: !d.isUnit` 一起传进
    /// `ArtMeshInFrame`（非单位卡 ⇒ `clipWindow = true`；同改前 `artMesh` 那一处的写法 `CardView.cs:3649-3650`）。
    /// 🧨 **改坏法（本批改前那版）**：把三元支的 `else` 改回
    /// `ArtMeshInFrame(artTex, FrameQuad(frameTex), 1f, null, false, 1)` ⇒ 下面第 ② 条**必红**。</para>
    ///
    /// <para>🔴 **灭自证（四条一起才成立，结构上不可能同时被「两边一起改回去」满足）**：
    /// ① 生产层 = `clipWindow:true` 那块（说「参数真接上了」）；
    /// ② 生产层 ≠ `clipWindow:false` 那块（说「开口裁切真的生效」）；
    /// ③ 两块参考**彼此不同**（否则这张卡框根本没有开口 ⇒ **前提**不成立，别读成通过）；
    /// ④ 生产层 uv 的**纵向范围确实比不裁那块窄**（这条只看 uv 自身，不经过任何「期望网格」）。
    /// ⛔ 期望值**不取我们的常量**：开口的存在性由 `FrameArtCoverage` 从**卡框贴图 alpha** 逐像素数出来
    /// （`quads ≥ 2` = 走的是逐行开口条带），不是把 `ArtMeshInFrame` 再算一遍。</para>
    ///
    /// <para>⚠️ 前提：这张卡必须**非单位**（`clipWindow` 才为 true）且**有抠图立绘**（前景层的门是 `cut && UseFrontLayer`）。
    /// 池子里这一类今天只有 7 张（`SW39`–`SW45`）；一张都建不出来 ⇒ 本条报「没验到」，⛔ 不算通过。</para></summary>
    static void CheckFrontLayerSmallClipsToWindow(List<CardDef> pool)
    {
        var prevUse = CardView.UseFrontLayer;
        var prevClamp = CardView.DebugDisableArtClamp;
        GameObject root = null;
        string used = null;      // ⚠️ 2026-10-24（`F4` 粘片段时就地修）：F1 片段把它声明在 `try` 里面，
                                 //   而 `finally` 要读它 ⇒ C# 作用域编不过（CS0103）。挪到 `try` 外，语义不变。
        try
        {
            CardView.UseFrontLayer = true;           // opt-in 小图档前景层（出厂 false ⇒ 生产路径不建这一层）
            CardView.DebugDisableArtClamp = false;   // 硬矩形开着（本条验的是「开口裁切」，两个开关互不替代）
            foreach (var def in pool)
            {
                if (def == null || def.IsUnit) continue;                  // 只要战术/防御卡
                var data = BattleDriver.ToCardData(def, def.Faction);
                if (data.artOverride != null) continue;                   // 异画走另一份清单/另一张贴图 ⇒ 本片段不覆盖（另记一条）
                var key = BattleDriver.ArtKey(def);
                if (!CardArt.HasCutout(key)) continue;                    // 前景层的门（`cut && UseFrontLayer`）
                var art = CardArt.Portrait(key);
                if (art == null) continue;

                if (root != null) Object.DestroyImmediate(root);
                root = new GameObject("chk_p13_" + key);
                var v = CardView.Create(root.transform, data, "chk_p13_" + key);
                var frameTex = v.FrameTexture;                            // 与 `Build` 用的是同一张（省得复算档位）
                var mFront = v.ArtFrontMeshForTest;                       // 生产真交给渲染器的那块
                if (frameTex == null || mFront == null || mFront.vertexCount < 4) continue;

                // 前提：这张卡框有逐行开口（`FrameArtCoverage` 走卡框贴图 alpha，不经我们的网格选择）
                bool hasWin = CardView.FrameArtCoverage(frameTex, art, 1f, out _, out _, out _, out int rows, out int quads);
                Check(hasWin && rows > 0 && quads >= 2,
                      $"P1-3 前提 [{key}] 这张战术卡框有逐行开口（rows={rows} quads={quads}）"
                    + "｜量不出来 ⇒ 下面几条**没验到**，别读成通过");

                var mClip = CardView.ArtMeshForTest(art, 1f, frameTex, true, 1,
                                                    CardView.TacticArtScale, CardView.TacticArtDy);    // 该用的形状
                var mFull = CardView.ArtMeshForTest(art, 1f, frameTex, false, 1,
                                                    CardView.TacticArtScale, CardView.TacticArtDy);   // 丢参时退化成的那块
                Check(!MeshNear(mClip, mFull, 1e-4f),
                      $"P1-3 前提 [{key}] 「裁到开口」与「只裁卡体 + 硬矩形」**确实是两块不同的网格**"
                    + "｜两块一样 ⇒ 这张卡框没有可裁的开口 ⇒ 本片段对它是空验，换一张");

                // ① 生产层 = 该用的那块
                Check(MeshNear(mFront, mClip, 1e-4f),
                      $"P1-3 [{key}] opt-in 前景层（`UseFrontLayer=true`）的网格 = `clipWindow:true` 那块"
                    + $"（顶点 {mFront.vertexCount}，{MeshSizeText(mFront)} vs {MeshSizeText(mClip)}）"
                    + "｜🧨 小图档支丢掉 `frameTex`/`clipWindow` ⇒ 拿到的是不裁那块 ⇒ 红");

                // ② 生产层 ≠ 丢参那块（与 ① 合起来 = 参数真的接上了，且真的生效）
                Check(!MeshNear(mFront, mFull, 1e-4f),
                      $"P1-3 [{key}] …而且**不等于**「`clipWindow:false`」那块（丢参时就是它，{MeshSizeText(mFull)}）");

                // ④ 只看 uv 自身：纵向范围必须真的被开口收窄
                float uLo = 1f, uHi = 0f, vLo = 1f, vHi = 0f;
                foreach (var uv in mFront.uv)
                {
                    if (uv.x < uLo) uLo = uv.x; if (uv.x > uHi) uHi = uv.x;
                    if (uv.y < vLo) vLo = uv.y; if (uv.y > vHi) vHi = uv.y;
                }
                float gLo = 1f, gHi = 0f;
                foreach (var uv in mFull.uv) { if (uv.y < gLo) gLo = uv.y; if (uv.y > gHi) gHi = uv.y; }
                Check(vLo >= -1e-4f && vHi <= 1f + 1e-4f,
                      $"P1-3 [{key}] 前景层 uv 没跑出 [0,1]（v = [{vLo:F4}, {vHi:F4}]）");
                Check((vHi - vLo) < (gHi - gLo) - 1e-3f,
                      $"P1-3 [{key}] 前景层 uv 的**纵向范围真的被开口收窄**"
                    + $"（裁后 {vHi - vLo:F4} < 不裁 {gHi - gLo:F4}）"
                    + "｜🧨 `clipWindow` 被丢掉/被忽略 ⇒ 两个数相等 ⇒ 红");

                used = $"{key}（{def.Name}）";
                return;   // 验到一张就够（7 张走的是同一条 `Build`/`ArtMeshInFrame` 路）
            }
            Check(false, "P1-3 前提：池子里找不到「非单位 + 有抠图立绘 + 建得出 `_artFront`」的卡 ⇒"
                       + " 这一条**没验到**（别读成通过）");
        }
        catch (System.Exception e)
        {
            Check(false, $"P1-3 抛异常 ⇒ **没跑完，别读成通过**：{e.GetType().Name}: {e.Message}");
        }
        finally
        {
            CardView.UseFrontLayer = prevUse;
            CardView.DebugDisableArtClamp = prevClamp;
            if (root != null) Object.DestroyImmediate(root);
            Debug.Log($"[cardface-chk] P1-3 小图档前景层裁开口：验的卡 = {used ?? "<没验到>"}");
        }
    }

    // ==================================================================
    //  F4（2026-10-24）：`_Clamp_Range` 硬矩形必须也夹「逐行开口条带」那条路
    // ==================================================================

    /// <summary>🆕 2026-10-24（`F4` · `F1` §六·1 交叉面）：**`_Clamp_Range` 在逐行开口条带那条路上真的生效**。
    ///
    /// <para>**缺陷（本批修前）**：`ArtMeshInFrame` 在 `FrameRowsOf` 拿得到开口时直接 `return strip`，
    /// 跳过了下面「与硬矩形取交集」那一段 ⇒ **非单位卡**（`clipWindow = true`）的 opt-in 小图档前景层
    /// 上硬矩形**空转**；`A7 ④b` 只跑单位卡（`clipWindow = false`）所以断不到。</para>
    ///
    /// <para>判据（原版一手）：战术/防御立绘与单位卡走**同一个** `cardImage`、同一份
    /// `Card Image Overflow` 材质 —— `CardImageController__SetImage.c:174-177` 在
    /// `param_3 == 0x14` 分支**之后**无条件 `Image.sprite = sprite`（tactic 那一支 `:126-137`
    /// 只关 `frameMask`、不动 sprite 也不换材质），而 `ChangeState` / `DoChange` 对卡类型**无判断**
    /// ⇒ 硬矩形同样作用于战术/防御卡。全文 → `资料/普查产出_1024/F4_卡面条带与断言.md`。</para>
    ///
    /// <para>🔴 **灭自证**：期望值是**常数**（战术立绘 1024² ⇒ 产品小图档局部 u ∈ (0.18, 0.82]；
    /// 板宽 = `ArtUnitSquare 2.7484 × ArtCoverMargin 1.04 = 2.858336` 卡单位
    /// ⇒ 硬矩形 x = ±0.32 × 2.858336 = **±0.914668**）—— ⛔ 不是把 `ArtClampUv01` 再算一遍。
    /// 同时要求「**不夹那块两端都越界**」与「**夹后两端都贴在同一个常数边界上**」，两条方向相反，改回去只能满足其中一条。</para>
    ///
    /// <para>🔴 **2026-10-24（`E10`）判别维重设计（第 ② 条）**：原来断「夹后 x ⊆ ±0.914668」——
    /// **区间包含**只证明「没超出去」，对「夹得比硬矩形还窄」「只夹一侧」「窗口方向反」都没有牙
    /// （误用 BIG 档 u (0.25,0.75] ⇒ 两端 ±0.7145，离边 0.2 卡单位，**照样 ⊆**）。
    /// 现在改断**到边界的距离**（两端都必须贴在同一根常数边上）—— 理由与反例写在 ② 的注释里。</para>
    ///
    /// <para>⚠️ 卡框**刻意指定** `astramilitarum_strat_tier2`（离线逐像素量得开口 x ∈ [−1.027, +1.073]，
    /// 见 `F4_卡面条带与断言.md` §三「量框」）：用样本卡自己的框不行 —— SW39–SW45 的 tier 不确定，
    /// 而 SW 的 strat tier1/2 开口只有 ±0.78 ⇒ 夹不夹一个样 ⇒ **空验**。
    /// 这条只借那张框的**开口几何**，与卡本身无关。</para></summary>
    static void CheckArtClampReachesStrip(List<CardDef> pool)
    {
        const float ClampHalfX = 0.914668f;   // = 0.32 × 2.7484 × 1.04（`ArtUnitSquare` × `ArtCoverMargin`，artScale = 1）

        var defT = FindByName(pool, "Armoured Offensive");                    // UM75：战术卡，立绘 1024²
        var art = defT != null ? CardArt.Portrait(BattleDriver.ArtKey(defT)) : null;
        var fr = CardArt.Frame("astramilitarum", "", true, 2);                // 开口 x ∈ [−1.027, +1.073]
        var prevClamp = CardView.DebugDisableArtClamp;
        CardView.DebugDisableArtClamp = false;                                // 硬矩形必须开着（上一段可能留下别的值）
        var mOn = art != null && fr != null ? CardView.ArtMeshForTest(art, 1f, fr, true, 1) : null;   // 硬矩形开（小图档）
        var mOff = art != null && fr != null ? CardView.ArtMeshForTest(art, 1f, fr, true, 0) : null;  // 不套
        // 🔴 **2026-10-24（`E3` · `DG3` 红 #4 · α）：⛔ 别拿网格 bbox 当 v 窗口** —— 条带是**逐行组**建的
        //   （`CardView.cs` 的 `ArtWindowMesh` / `BuildFrameRows`）：x 一夹，底部那些「整段落在 ±0.9147 之外」
        //   的窄行组会被**整组丢掉** ⇒ bbox 的 y 下沿跟着上移（实测 −0.5946 → −0.2487），
        //   看起来像「小图档也夹了 y」，其实 v 窗口恒为 (0,1]、代码是对的。
        //   ⇒ 换成与 **`clampKind = 2`（BIG，v = 0.10..0.89）** 那块比 y：BIG 的**夹窗**是
        //   `yB0 + 0.10·h` / `yB0 + 0.89·h`（本例 −1.1873 / 1.0715），网格 bbox 还会被「逐行组」再收一点，
        //   但**上沿一定 ≤ 1.0715**（小图档 1.2135 比它高 0.14）、**下沿一定离 −0.2487 很远** ⇒ 有牙。
        var mBig = art != null && fr != null ? CardView.ArtMeshForTest(art, 1f, fr, true, 2) : null;   // BIG 档（对照）
        CardView.DebugDisableArtClamp = prevClamp;
        Check(mOn != null && mOff != null && mBig != null
              && mOn.vertexCount >= 4 && mOff.vertexCount >= 4 && mBig.vertexCount >= 4,
              "F4 前提：战术立绘（1024²）+ `astramilitarum_strat_tier2` 卡框 + 条带网格（小图档 / 不套 / BIG 档）都建得出来"
            + "｜建不出来 ⇒ 下面几条**没验到**，别读成通过");
        if (mOn == null || mOff == null || mBig == null
            || mOn.vertexCount < 4 || mOff.vertexCount < 4 || mBig.vertexCount < 4) return;

        float onX0 = 1e9f, onX1 = -1e9f, onY0 = 1e9f, onY1 = -1e9f;
        foreach (var v in mOn.vertices)
        {
            if (v.x < onX0) onX0 = v.x; if (v.x > onX1) onX1 = v.x;
            if (v.y < onY0) onY0 = v.y; if (v.y > onY1) onY1 = v.y;
        }
        float offX0 = 1e9f, offX1 = -1e9f, offY0 = 1e9f, offY1 = -1e9f;
        foreach (var v in mOff.vertices)
        {
            if (v.x < offX0) offX0 = v.x; if (v.x > offX1) offX1 = v.x;
            if (v.y < offY0) offY0 = v.y; if (v.y > offY1) offY1 = v.y;
        }
        float bigY0 = 1e9f, bigY1 = -1e9f;
        foreach (var v in mBig.vertices)
        {
            if (v.y < bigY0) bigY0 = v.y; if (v.y > bigY1) bigY1 = v.y;
        }

        // ① 前提 + 生产底图没被误伤：`clampKind == 0`（**不套硬矩形**，走同一条条带路）**不受硬矩形**。
        // 🔴 **2026-10-24（`H3`）就地订正（铁律 5）**：本行原来写「`clampKind = 0`（**战术卡底层 `_art`**，
        //   走同一条条带路）**不受硬矩形**」——「战术卡底层 `_art` 不受硬矩形」这句**作为生产事实已过期**：
        //   `H1`（同日）起生产 `_art` 对**非单位卡**套 `clampKind: 1/2`（`CardView.cs` 的 `artClampKind`）。
        //   ✅ **断言本身仍成立、逻辑没变**：它量的是 `ArtMeshForTest(..., clampKind: 0)` 的**合成网格**，
        //   验的是「`clampKind == 0` 这条路上硬矩形确实没生效」，与「哪一层是生产底图」无关。
        // 🔴 **2026-10-24（`E10`）**：**两侧**都要越界（原来只查左边）。② 的新判据要断「两端都贴边」，
        //   前提是「不夹那块**两端都伸到硬矩形之外**」—— 否则那一侧的贴边是空验。
        //   离线现读（`astramilitarum_strat_tier2` + 1024² 立绘、`clampKind = 0`；口径同 H4 的复刻表）：
        //   x ∈ [−1.0403, +0.9942] ⇒ 左越 0.1256 / 右越 0.0795 卡单位（≈ 38 px / 24 px），两侧都够。
        Check(offX0 < -ClampHalfX - 1e-3f && offX1 > ClampHalfX + 1e-3f,
              $"F4 前提：`clampKind=0` 的条带**两侧都伸出硬矩形**（不夹那块 x ∈ [{offX0:F4}, {offX1:F4}]，"
            + $"硬矩形 ±{ClampHalfX:F4}）—— 既证明样本框宽过硬矩形（否则 ② 那一侧空验），"
            + "也证明 `clampKind=0` 这条路上没把硬矩形误加上去"
            + "｜🧨 只有一侧越界 ⇒ ② 只有一侧有判别力 ⇒ 先核卡框 / 离线表，⛔ 别把 ② 放宽");

        // ② 【🔴 2026-10-24（`E10`）**判别维重设计**】夹后的 x 极值必须**两端都压在原版那根硬矩形边上**。
        //   ⛔ 原来断的是「x ⊆ ±0.914668」（区间包含）—— 它只证明「没超出去」，**拦不住方向 / 幅度反**：
        //     · **夹得比硬矩形还窄**（例如误把 BIG 档的 u 窗口 (0.25,0.75] 用在条带路上）⇒ 两端 ±0.7145，
        //       离边 0.2 卡单位，**照样满足 ⊆** ⇒ 旧判据全绿；
        //     · **只夹一侧**（另一侧的 Max/Min 写漏）⇒ 旧判据只在这一侧越界时才红，两张对称的错夹法分不开；
        //     · **窗口方向反**（`u0/u1` 对调 / `clampCard.x` 与 `.y` 对调）⇒ 交集为空、整块丢、退回矩形路径
        //       ⇒ 旧判据在某些组合下仍「⊆」，而**画面上的立绘已经被镜像/整块换掉**。
        //   ⇒ 新维度 = **到边界的距离**：`|onX0 + H| ≤ tol` 且 `|onX1 − H| ≤ tol`。
        //     能贴得准的机理：`ArtWindowMesh` 逐行 `Mathf.Max(FramePxXEdge(col), bxL)` /
        //     `Mathf.Min(FramePxXEdge(col+1), bxR)` ⇒ 每一行都被**截在同一根边界**上；而边界由原版
        //     `_Clamp_Range` 常数算出（`ClampHalfX`），⛔ 不是把实现再算一遍。
        //     `tol = 0.005` 卡单位 ≈ 1.5 px（682 px 宽的卡框上 1 px ≈ 0.00329；实测两端与边界的差应 < 1e-5，
        //     容差只吸收浮点）。BIG 档那 0.2 的偏差是这个阈值的 **40 倍** ⇒ 不处在抖动带里。
        //   🧨 删掉条带早退路的交集（`CardView.ArtMeshInFrame` 里那两句 `Mathf.Max/Min`）⇒ 两端回到
        //      −1.0403 / +0.9942 ⇒ 不贴边 ⇒ **红**
        //   🧨 误用 BIG 档窗口 (0.25,0.75] ⇒ 两端 ±0.7145、离边 0.2 ⇒ **红**（旧的「⊆」判据在这一档**是绿的**）
        //   🧨 只夹一侧 ⇒ 没夹的那端不贴边 ⇒ 红
        //   🧨 窗口方向反（`clampCard.x` 与 `.y` 对调）⇒ 交集为空 ⇒ 退回矩形路径 ⇒ 两端都不贴边 ⇒ **红**
        const float edgeTol = 0.005f;
        // 🔴 2026-10-24 二次订正（只读诊断代理现核 + 离线复刻逐位吻合）：「两端贴硬矩形边」在本样本上做不到 ——
        //   越界的是**独立细条**，被 ArtWindowMesh 的 Mathf.Max/Min(..., bxL/bxR) 弄成 xR<=xL 后整段丢弃，
        //   全帧没有一段跨在 ±H 上 ⇒ 极值顶点落在**卡框画窗边**（贴图列 257/767 = WinHalfX）而不是硬矩形边。
        //   ⇒ 改**三明治**：下界 = 画窗边（0.83949，离线量得）、上界 = 硬矩形（ClampHalfX）。
        const float WinHalfX = 0.83949f;
        float winDL = Mathf.Abs(onX0 + WinHalfX), winDR = Mathf.Abs(onX1 - WinHalfX);
        Check(winDL <= edgeTol && winDR <= edgeTol
              && onX0 >= -ClampHalfX - edgeTol && onX1 <= ClampHalfX + edgeTol,
              $"F4 硬矩形在**条带路**上真的生效（三明治判据）："
            + $"夹后 x ∈ [{onX0:F4}, {onX1:F4}]；到**画窗边** ±{WinHalfX} 的距离 = ({winDL:F4}, {winDR:F4}) ≤ {edgeTol:F3}；"
            + $"且不越**硬矩形** ±{ClampHalfX:F4}"
            + "｜🧨 条带早退路没过交集 ⇒ 等于 clampKind=0 那块（x ∈ [-1.0403, +0.9942]）⇒ 下界差 0.2 ⇒ 红"
            + "｜🧨 误用 BIG 档窗口 (0.25,0.75] ⇒ ±0.7145 ⇒ 下界差 0.125 ⇒ 红（旧「⊆」判据在这一档是绿的）"
            + "｜🧨 只夹一侧 / 窗口方向反（u0、u1 对调 ⇒ 完全不夹）⇒ 红");

        // ③ 灭自证：两块确实不同，且只窄在 x 上
        Check((onX1 - onX0) < (offX1 - offX0) - 1e-3f,
              $"F4 灭自证：夹后 x 宽 {onX1 - onX0:F4} < 不夹 {offX1 - offX0:F4}"
            + "｜🧨 条带路没夹 / 夹了没接进网格 ⇒ 两个数相等 ⇒ 红");

        // ④ 小图档的 v 窗口是 (0,1] ⇒ 纵向**不该被 v 窗口收窄**（防「拿错档 = 用了 BIG 的 v 0.10..0.89」）
        //   🔴 **2026-10-24（`E3` · `DG3` 红 #4）：量具换掉** —— 原来断「ON 与 OFF 的 bbox y 相同」，
        //   但 bbox 不是 v 窗口：条带按**逐行组**建，x 一夹会把底部那些「整段在 ±0.9147 之外」的窄行组
        //   整组丢掉 ⇒ ON 的 y 下沿必然上移（−0.5946 → −0.2487）⇒ 那条**结构性恒红**。
        //   ⇒ 改与 **`clampKind = 2`（BIG，v = 0.10..0.89）** 那块比。
        //
        // 🔴 **2026-10-24（`E4`）：判别式必须只用【上沿】做牙 —— 下沿对这张框没有判别力。**
        //   实测（`_tmp_view/20261024_sync2/CardBaseDemo.log` 11:15）：
        //     小图档 y **[−0.2487, 1.2135]** vs BIG 档 y **[−0.2487, 1.0708]** —— **下沿完全相同**。
        //   原因：BIG 的 v 下沿 = `yB0 + 0.10·h = −1.4732 + 0.28583 = −1.1873`，**比卡框开口还低**
        //   ⇒ 下沿先被**卡框开口**截住（两档都停在 −0.2487），轮不到 v 窗口去夹。
        //   ⇒ `E3` 写的 `onY0 > bigY0 + 0.1` 是**结构性恒红**（不是缺陷、也不是误用 BIG）。
        //   真正的牙在**上沿**：BIG 的 `yB0 + 0.89·h = 1.0707` 把上沿压下去（实测 1.2135 → 1.0708，差 0.143）。
        //   两条一起断：
        //     ① **上沿不许被压**（主判据 · 有牙：误用 BIG ⇒ ON 与 BIG 是**同一份网格** ⇒ 两上沿相等 ⇒ 红）；
        //     ② **下沿不许被抬**（方向闸 · 弱：小图档的下沿不得**高于** BIG 的；本例两者相等 ⇒ 恒真，
        //        ⛔ 别把它当主判据 —— 它挡的是「SMALL 比 BIG 收得还狠」那一种反向改坏）。
        Check(onY1 > bigY1 + 0.05f && onY0 <= bigY0 + 1e-3f,
              $"F4 小图档的 v 窗口是 (0,1] ⇒ 纵向没被收窄（小图档 y [{onY0:F4},{onY1:F4}] vs BIG 档 "
            + $"[{bigY0:F4},{bigY1:F4}]；不夹那块 y [{offY0:F4},{offY1:F4}]）"
            + "｜🧨 误用 BIG 档（v 0.10..0.89）⇒ **上沿被压到 ≈1.0707**（本例 1.2135→1.0708，差 0.143）⇒ 红"
            + "｜⚠️ 下沿对这张框**没有判别力**（BIG 的 v 下沿 −1.1873 低于卡框开口、先被开口截住 ⇒ 两档都是 −0.2487）");
    }

    /// <summary>`D3`：按**详情窗大图档**真建一张卡，取它 `_artFront` 层真交给渲染器的那块网格。
    /// <para>`gateOff` = 临时取消「×1.27 只给 Minion/Hero」那道门（灭自证用）。
    /// ⚠️ 两个开关都必须在 `Create` **之前**设 —— 倍率/档位都是 `Build` 那一趟烘进网格顶点的。</para>
    /// <para>建完立刻 `DestroyImmediate`（别让残卡出现在别人的截图里 —— 与 `RenderCardPixels` 同一条规矩）。
    /// ⚠️ 返回的 `Mesh` 不是 GameObject 的一部分（`ArtMeshInFrame` 建好塞进静态缓存）⇒ 销毁根节点不影响它。</para></summary>
    static Mesh BuildBigFrontMesh(CardDef def, string tag, bool gateOff = false)
    {
        var prevGate = CardView.DebugDisableBigArtScaleGate;
        var prevBig = CardView.BigCardOverdraw;
        var root = new GameObject("chk_d3_" + tag);
        try
        {
            CardView.DebugDisableBigArtScaleGate = gateOff;
            CardView.BigCardOverdraw = true;      // 出厂就 true；这里显式写死，免得被别的探针改过而静默少建一层
            var v = CardView.Create(root.transform, BattleDriver.ToCardData(def, def.Faction),
                                    "chk_d3_" + tag, use: CardUse.ZoomWindow);
            return v.ArtFrontMeshForTest;
        }
        finally
        {
            CardView.DebugDisableBigArtScaleGate = prevGate;
            CardView.BigCardOverdraw = prevBig;
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>两块立绘网格是否**逐顶点逐 uv** 相同（容差 `tol`，卡单位）。
    /// ⚠️ 比数值、不比引用 —— 网格是按缓存键共享的，引用相等证明不了「用的是哪一档」以外的任何事，
    /// 而数值比在「两档撞车」时也会当场红。</summary>
    static bool MeshNear(Mesh a, Mesh b, float tol)
    {
        if (a == null || b == null) return false;
        var av = a.vertices; var bv = b.vertices; var au = a.uv; var bu = b.uv;
        if (av == null || bv == null || au == null || bu == null) return false;
        if (av.Length != bv.Length || au.Length != bu.Length) return false;
        for (int i = 0; i < av.Length; i++)
            if ((av[i] - bv[i]).magnitude > tol || (au[i] - bu[i]).magnitude > tol) return false;
        return true;
    }

    static string MeshSizeText(Mesh m)
    {
        if (m == null) return "<null>";
        var s = m.bounds.size;
        return $"{s.x:F4}×{s.y:F4}";
    }

    // 战术卡 ④a 用的卡框档位：`frame_ultramarines_strat_tier4`（离线实测 `hole ∪ ring`
    // x ∈ [−0.8148, +1.0321]；硬矩形 ±0.9147 ⇒ **右侧**越界 ≈0.117 卡单位 ≈ 35 px）。
    // ⚠️ **只覆盖右侧**（左侧 −0.8148 本来就在硬矩形内）⇒ 本条挡不住「只夹左边、漏了右边」那种反向错；
    // 两侧都覆盖的是 `CheckArtClampReachesStrip`（用 `astramilitarum_strat_tier2` 的**合成网格**验）。
    // 🆕 **2026-10-24（`H6`）**：同一个档位也用于**防御卡** ④c 那个样本（`Sacred Altar` / Sororitas）——
    //   它的 `frame_sororitas_strat_tier4` 是**两侧都越**（离线实测 mesh x ∈ [−1.0403, +1.0463]），
    //   正好补上「只覆盖右侧」这一半。
    const int TacticClampProbeFrameTier = 4;

    /// <summary>A7 ④a：生产小卡档的「硬矩形开 / 关」像素对照 —— 按**卡型**分两种判据。
    /// <para>· **单位卡**（`artClamped` = false）：生产小卡路径**不建前景层**（`UseFrontLayer` = false），
    /// 硬矩形只挂在 `_artFront` 上 ⇒ 小卡的渲染一个像素都不该变（差集 **= 0**）。</para>
    /// <para>🔴 **2026-10-24（`H3` → 第七十三会话 `M2` 再翻面）**：本段在 `H1` 那天改过一次
    /// （「非单位卡的 `_art` 也套 `clampKind 1/2` ⇒ 战术卡断『差集 > 0』」），**那一版已作废** ——
    /// `M2` 用原版实况证伪了 `H1`（战术卡插图铺满拱窗、无黑边；判据 → `CardView.ArtClampKindFor` 的 doc）
    /// ⇒ **两种卡型现在同判：差集 = 0**。</para>
    /// <para>· **战术 / 防御卡**（🔴 **2026-10-24 第七十三会话 `M2` 起 `artClamped` = false**，与单位卡同判）：
    /// 生产 `_art` **不套**硬矩形（`CardView.ArtClampKindFor` 恒 0）⇒ 差集必须 **= 0**。
    /// 🧨 谁把 `clampKind` 接回 1/2 ⇒ 差集 > 0 ⇒ 红。判据 → `CardView.ArtClampKindFor` 的 doc。
    /// 历史：`H1`~`M2` 之前这一档是 `artClamped = true`、断「差集 > 0 且全在硬矩形之外」——
    /// 那条建立在「战术卡也吃 `_Clamp_Range`」上，已被原版实况证伪。</para>
    /// <para>⚠️ **卡框档位**：`M2` 起所有调用点都传 `artClamped = false` ⇒ 本方法**不再钉**
    /// `TacticClampProbeFrameTier`（不套硬矩形时档位不影响结果）。`artClamped = true` 那条分支
    /// **保留但当前无人调用**（留作「万一要把硬矩形接回 `_art`」时的现成判据）。
    /// 生产档位来自 `CardProgress.Level`（读玩家存档，没存档时 = 1），而同一张卡各档的开口宽度差很多
    /// （tier1/2 **完全落在硬矩形内** · tier3 只差 1 px · tier4 右侧越界 ≈0.117 卡单位）
    /// ⇒ 不钉住就会「随存档变红变绿」。⛔ 这不是放宽判据 —— 是把「测的是不是那个情况」钉死。</para>
    /// <para>A7 ④b（`withFront` · 只给单位卡跑）：**证明这份代码真的接上了** —— 临时开 `UseFrontLayer`
    /// （opt-in 小卡档前景层，出厂 false ⇒ 不建；开了才走 `clampKind: 1`），硬矩形必须**切掉一块**，
    /// 且切掉的全在**硬矩形之外**（矩形里面一个像素都不许动）⇒ 「两档一样」和「乱切」都过不了。</para></summary>
    static void CheckClampSmallPixelsUnchanged(CardDef def, string what, bool withFront, bool artClamped)
    {
        // ---- ④a 生产小卡档（前景层不建）：硬矩形开 / 关两张 ----
        //   ⚠️ 战术卡那一路**必须**把卡框档位钉死（见 doc）；单位卡保持现档（`clampKind = 0`，档位不影响结果）。
        Color32[] a, b;
        var prevTier = CardView.DebugFrameTier;
        try
        {
            if (artClamped) CardView.DebugFrameTier = TacticClampProbeFrameTier;
            a = RenderCardPixels(def, "prod_on", false, false);
            b = RenderCardPixels(def, "prod_off", false, true);
        }
        finally { CardView.DebugFrameTier = prevTier; }

        int d1 = CountDiff(a, b, out int mx1);
        if (!artClamped)
        {
            Check(d1 == 0,
                  $"A7 ④a {what} 生产小卡档（`UseFrontLayer`=false ⇒ 前景层不建）硬矩形开/关**逐像素不变**"
                + $"（差集 {d1} 点 / {a.Length}，最大通道差 {mx1}）"
                + "｜🧨 把 `clampKind` 也接到单位卡的 `_art` 底层（生产路径走它）⇒ 这里必红");
        }
        else
        {
            // 硬矩形（卡单位）= 「不裁卡框、只套小图档硬矩形」那块立绘网格的 bbox（口径同 ④b）。
            //   ⚠️ 必须在 `DebugDisableArtClamp = false` 下建 —— 否则拿到的是**没夹**的那块
            //   ⇒「矩形内」会扩到整块板 ⇒ 判据被放宽到无判别力。
            var artT = CardArt.Portrait(BattleDriver.ArtKey(def));
            Mesh hard = null;
            var prevOff = CardView.DebugDisableArtClamp;
            try
            {
                CardView.DebugDisableArtClamp = false;
                if (artT != null) hard = CardView.ArtMeshForTest(artT, 1f, null, false, 1,
                                                                 CardView.TacticArtScale, CardView.TacticArtDy);
            }
            finally { CardView.DebugDisableArtClamp = prevOff; }

            int inHard = -1;
            string boxTxt = "<硬矩形网格没建出来>";
            if (hard != null && hard.vertexCount >= 4)
            {
                var hv = hard.vertices;
                float hx0 = MinV3(hv, 0), hx1 = MaxV3(hv, 0), hy0 = MinV3(hv, 1), hy1 = MaxV3(hv, 1);
                float hPpu = H / (2f * OrthoHalfH);
                float hCamY = -0.03f;
                // 口径同 ④b：**向内缩 3 px**（边界那圈抗锯齿排除掉），⛔ 不是往外扩。
                float hTol = 3f / hPpu;
                hx0 += hTol; hx1 -= hTol; hy0 += hTol; hy1 -= hTol;
                inHard = 0;
                for (int py = 0; py < H; py++)
                    for (int px = 0; px < W; px++)
                    {
                        int i = py * W + px;
                        int dd = Mathf.Max(Mathf.Abs(a[i].r - b[i].r),
                                  Mathf.Max(Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)));
                        if (dd <= 2) continue;
                        float x = (px + 0.5f - W * 0.5f) / hPpu;
                        float y = hCamY - (py + 0.5f - H * 0.5f) / hPpu;
                        if (x > hx0 && x < hx1 && y > hy0 && y < hy1) inHard++;
                    }
                boxTxt = $"x[{hx0:F3},{hx1:F3}] y[{hy0:F3},{hy1:F3}]";
            }

            Check(inHard == 0 && d1 > 0,
                  $"A7 ④a {what} 生产小卡档（`UseFrontLayer`=false）**硬矩形真的作用在 `_art` 底层上**："
                + $"开/关差集 {d1} 点 / {a.Length}（最大通道差 {mx1}）> 0，"
                + $"且**硬矩形（内缩 3 px）内改动 {inHard} 点**（内缩框 {boxTxt} 卡单位；卡框档位钉在 tier{TacticClampProbeFrameTier}）"
                + "｜🧨 把 `clampKind` 改回 0（战术/防御卡 `_art` 不套硬矩形）⇒ 差集 = 0 ⇒ 红"
                + "｜🧨 硬矩形算错 / 矩形里也被切了 ⇒ 矩内改动 > 0 ⇒ 红"
                + "｜🧨 硬矩形网格建不出来（框显示 `<硬矩形网格没建出来>`）⇒ 这一条**没验到**，别读成通过");
        }
        if (!withFront) return;

        // ---- ④b 证明这份代码**真的接上了**（灭自证）----
        //  开 `UseFrontLayer`（opt-in 小卡档前台层）后，硬矩形必须**切掉一块**，且切掉的全在
        //  **硬矩形之外**（矩形里面一个像素都不许动）⇒ 「两档一样」和「乱切」都过不了。
        var art = CardArt.Portrait(BattleDriver.ArtKey(def));
        var mesh = art != null ? CardView.ArtMeshForTest(art, 1f, null, false, 1) : null;
        if (mesh == null || mesh.vertexCount < 4)
        {
            Check(false, $"A7 ④b {what} 前提：小图档前景网格建不出来 ⇒ 这一条**没验到**（别读成通过）");
            return;
        }
        var c = RenderCardPixels(def, "front_on", true, false);
        var d = RenderCardPixels(def, "front_off", true, true);
        int d2 = CountDiff(c, d, out int mx2);
        Check(d2 > 20,
              $"A7 ④b {what} 硬矩形在 opt-in 前景层上**真的生效**（开/关差集 {d2} 点，最大通道差 {mx2}）"
            + "｜🧨 clamp 没接到网格上 ⇒ 差集 = 0 ⇒ 红");

        // 硬矩形（卡单位）→ 相机像素。相机口径与 `RenderPixels` 同一份：
        //   中心 (0, −0.03)、正交半高 `OrthoHalfH`、W×H ⇒ ppu = H / (2·OrthoHalfH)
        var mv = mesh.vertices;
        float wx0 = MinV3(mv, 0), wx1 = MaxV3(mv, 0), wy0 = MinV3(mv, 1), wy1 = MaxV3(mv, 1);
        float ppu = H / (2f * OrthoHalfH);
        float camY = -0.03f;
        float tol = 3f / ppu;                    // 3 px 容差（抗锯齿边）
        // 🔴 **2026-10-24（`E3` · `DG3` 红 #2）：容差必须【向内缩】，⛔ 不是往外扩。**
        //   被硬矩形夹掉的那条**竖条正好从矩形边开始**（立绘板边 0.9363 → 夹边 0.9148，宽 ≈6.5 px），
        //   原来 `wx0 -= tol` 把边界往**外**推 3 px ⇒ 这条竖条里约 3 px 落进「矩形内」，
        //   `inside` 结构性不可能为 0（实测 4630/7607 = 61%）。
        //   断言的原意（「夹只发生在矩形外」）是对的，判据反了：要断的是「**严格在矩形里面**的像素一个都没变」
        //   ⇒ 用内缩的矩形把边界那圈抗锯齿排除掉。
        wx0 += tol; wx1 -= tol; wy0 += tol; wy1 -= tol;
        int inside = 0;
        for (int py = 0; py < H; py++)
            for (int px = 0; px < W; px++)
            {
                int i = py * W + px;
                int dd = Mathf.Max(Mathf.Abs(c[i].r - d[i].r),
                          Mathf.Max(Mathf.Abs(c[i].g - d[i].g), Mathf.Abs(c[i].b - d[i].b)));
                if (dd <= 2) continue;
                float x = (px + 0.5f - W * 0.5f) / ppu;
                float y = camY - (py + 0.5f - H * 0.5f) / ppu;
                if (x > wx0 && x < wx1 && y > wy0 && y < wy1) inside++;
            }
        Check(inside == 0,
              $"A7 ④b {what} 变的像素**全在硬矩形之外**（硬矩形**内缩 3 px** 后的框里改动 {inside} 点 / 差集 {d2} 点；"
            + $"内缩框 x[{wx0:F3},{wx1:F3}] y[{wy0:F3},{wy1:F3}] 卡单位）"
            + "｜🧨 硬矩形算错（切到矩形里面）或整块被平移 ⇒ 红");
    }

    /// <summary>按指定开关建一张卡并渲成像素（用完立刻从场景清掉 —— 别让残卡出现在别人的截图里）。
    /// 🔴 **2026-10-24（`E3` · `DG3` 红 #1/#3）**：渲图走 <see cref="RenderPixelsIsolated"/> ——
    /// 只渲这张卡。`CheckLayers()` 是在 `CardBaseDemo` 的**活现场**里调起的，整场渲会把
    /// 手牌 / 场上卡 / 背景 / 粒子的像素一起带进 A7 的 A/B 差集里（那不是被测卡的变化）。</summary>
    static Color32[] RenderCardPixels(CardDef def, string tag, bool frontLayer, bool clampOff)
    {
        var prevUse = CardView.UseFrontLayer;
        var prevOff = CardView.DebugDisableArtClamp;
        var root = new GameObject("chk_a7_" + tag);
        try
        {
            CardView.UseFrontLayer = frontLayer;
            CardView.DebugDisableArtClamp = clampOff;
            CardView.Create(root.transform, BattleDriver.ToCardData(def, def.Faction), "chk_a7_" + tag);
            return RenderPixelsIsolated(root);
        }
        finally
        {
            CardView.UseFrontLayer = prevUse;
            CardView.DebugDisableArtClamp = prevOff;
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>两张渲染的差集点数（任一通道差 &gt; `tol` 才算），并给出最大通道差。</summary>
    static int CountDiff(Color32[] a, Color32[] b, out int maxd, int tol = 2)
    {
        int n = 0; maxd = 0;
        for (int i = 0; i < a.Length; i++)
        {
            int d = Mathf.Max(Mathf.Abs(a[i].r - b[i].r),
                      Mathf.Max(Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)));
            if (d > tol) n++;
            if (d > maxd) maxd = d;
        }
        return n;
    }

    static float MinComp(Vector2[] a, int c)
    { float m = float.MaxValue; for (int i = 0; i < a.Length; i++) { float v = c == 0 ? a[i].x : a[i].y; if (v < m) m = v; } return m; }
    static float MaxComp(Vector2[] a, int c)
    { float m = float.MinValue; for (int i = 0; i < a.Length; i++) { float v = c == 0 ? a[i].x : a[i].y; if (v > m) m = v; } return m; }
    static float MinV3(Vector3[] a, int c)
    { float m = float.MaxValue; for (int i = 0; i < a.Length; i++) { float v = c == 0 ? a[i].x : a[i].y; if (v < m) m = v; } return m; }
    static float MaxV3(Vector3[] a, int c)
    { float m = float.MinValue; for (int i = 0; i < a.Length; i++) { float v = c == 0 ? a[i].x : a[i].y; if (v > m) m = v; } return m; }

    /// <summary>把一张贴图编成 PNG。**原件不可读时**（立绘一律如此，见 <see cref="ArtBaker"/> 的 `isFrame` 那行）
    /// 走一次 `Graphics.Blit` 拷进可读副本，并 `LogWarning` 说明「这份是副本、色值仅供参考」。
    /// <para>为什么要它：原来直接 `EncodeToPNG()` ⇒ **抛异常打断整个探针**，症状是「只有前 8 张有图」。</para></summary>
    static byte[] EncodePng(Texture2D tex)
    {
        if (tex.isReadable) return tex.EncodeToPNG();
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        var prev = RenderTexture.active;
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
        copy.ReadPixels(new Rect(0f, 0f, tex.width, tex.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        byte[] bytes = copy.EncodeToPNG();
        Object.DestroyImmediate(copy);
        Debug.LogWarning($"[cardface] 贴图 `{tex.name}` **不可读**（立绘默认不开 Read/Write）"
                       + "⇒ `_tex.png` 是 `Graphics.Blit` 的**副本**，色值仅供参考，⛔ 别拿它逐像素断原件");
        return bytes;
    }

    public static void Run()
    {
        CheckLayers();      // 🆕 2026-10-12：跑探针顺带把层级三项的断言跑掉（本入口不在 12 条自检里）
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
                    // 🔴 **2026-10-15（第三十二会话）就地修**：原来直接 `tex.EncodeToPNG()`。
                    //   而立绘**一律不可读** —— `ArtBaker.ApplyImportSettings:155` 是 `ti.isReadable = isFrame`，
                    //   只给**卡框**开。于是这句对**任何一张立绘**都抛 `ArgumentException: Texture ... is not readable`，
                    //   **把整个探针打断在第 8 张**（实测：本会话那 5 张新加的卡一张都没渲出来）。
                    //   ⇒ 不可读时走一次 `Graphics.Blit` 拷进可读副本；⚠️ 那一步**过一遍渲染管线、色值可能与原件有差**，
                    //   所以**出声写清**（⛔ 不许静默把副本当原件用 —— 那是本工程的「不许静默失败」红线）。
                    File.WriteAllBytes(Path.Combine(OutDir, SafeName(name) + "_tex.png"), EncodePng(tex));
                    Debug.Log($"[cardface] 贴图 {name}: {tex.width}×{tex.height} format={tex.format} "
                            + $"mip={tex.mipmapCount} 可读={tex.isReadable}");
                }
            }
            var root = new GameObject("probe_" + name);
            var view = CardView.Create(root.transform, data, name);
            // 卡的 z 都是 0 附近，相机放远一点正对着拍
            Shot(view, Path.Combine(OutDir, SafeName(name) + ".png"));

            // 🔴 **2026-10-10 删掉了原来那张「只有底层」的 `_nofront`** —— 它靠 `CardView.DebugNoArtFront`
            //    关掉前景角色抠图层，而那一层现在**出厂就不建**（`CardView.UseFrontLayer` = false，
            //    原版默认也不越出，见它那段注释）⇒ 再渲一张只会得到与上面那张**逐像素相同**的图，
            //    而「一个永远看不出差别」的诊断图 = 误导（本工程的「不许静默失败」）。
            //    要看 opt-in（越出）那一档：临时把 `CardView.UseFrontLayer = true` 再跑一次。
            // 再渲一张**不可打出**态：手牌上费用不够的卡就是这个状态，
            // 用来确认「卡片四周那圈白边」到底是不是状态描边（`_rim`）
            view.SetHighlight(CardHighlightState.Unplayable);
            Shot(view, Path.Combine(OutDir, SafeName(name) + "_unplayable.png"));
            // 选中态：描边**该**出现（羽化后应该是软光，不是硬方框）
            view.SetHighlight(CardHighlightState.Selected);
            Shot(view, Path.Combine(OutDir, SafeName(name) + "_selected.png"));
            // 🔴 **2026-10-22（`W64`）：回常规态必须把手动补间推到头**。
            //   `_selected` 那圈状态环（`_rim`）是**瞬切**亮的（`CardHighlight.TweenTimeOf(Selected)` = 0），
            //   而回 `Normal` 是 **0.1 s 淡出**（`CardHighlight.AnimTime`）；批处理没有帧循环
            //   （CLAUDE.md §三）⇒ 不推的话环会**停在白色**、跟着拍进下面那张 `_playable`
            //   —— 那样就分不清图里那圈白光是 `_rim` 还是 `_Outline`（正是 `W64` §2·3(b) 要判的那件事）。
            //   ⚠️ `CardTween.Mode` 必须在**建补间之前**切 `Manual`（`CardTween.Use` 是在建的时候读这个值）。
            {
                var prevMode = CardTween.Mode;
                try
                {
                    CardTween.Mode = DG.Tweening.UpdateType.Manual;
                    view.SetHighlight(CardHighlightState.Normal);
                    CardTween.Advance(CardHighlight.AnimTime + 0.05f);   // 环淡到 alpha 0 ⇒ 那层自己关掉
                }
                finally { CardTween.Mode = prevMode; }   // 抛异常也别把全局补间模式留在 Manual
            }

            // 🆕 2026-10-22（`W64` §4·2(1)）：**可打出描边**（`outlineGlow`）那一张 ——
            //   本探针原来一次都没调过 `SetOutline`（grep 命中 0），那层从没被渲染过
            //   （现有 `_selected.png` 里那圈白光是 `_rim`，不是它）。判据 → `W64` §2·3(b) / §4·2(1)。
            ShotPlayableOutline(view, def, Path.Combine(OutDir, SafeName(name) + "_playable.png"));

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

                // 🆕 2026-10-12（`A1659`）：**倾摆态**那一张 —— 判据（DIAG 四·b）要求
                //    「倾摆非零时文字/图标与底板/卡框仍同面」时**并排渲一张**（铁律 10 第 6 条）。
                //    `AutoCardRotation` 静止时是 identity，所以这里直接把 `2DCard` 转它的上限角
                //    （`maxYAngle = 10`）再拍 —— 修好之后文字该跟着一起歪、和底板卡框连成一张卡。
                {
                    var r4 = new GameObject("probe_tilt");
                    var v4 = CardView.Create(r4.transform, data, name + "_tilt", CardFace.Hand);
                    v4.FaceLayerRoot.localRotation = Quaternion.Euler(0f, 10f, 0f);
                    Shot(v4, Path.Combine(OutDir, SafeName(name) + "_tilt.png"));
                    Object.DestroyImmediate(r4);
                }
            }

            // 🆕 2026-10-21（卡牌线 `新账-66`）：**拱顶那一档**必须渲一张 —— 上面那张走的是
            //   `CardProgress.Level(artId)` 的缺省档（=1：圆角矩形窗 + 徽记处的外凸缺口），
            //   而 **tier3/4 才是哥特拱窗**（`frame_<阵营>_strat_tier{3,4}.png`）。
            //   这是「插图画窗有没有盖满拱顶」唯一能肉眼看的口子（断言只量得出几何覆盖，量不出观感）。
            //   ⚠️ 走 `CardView.DebugFrameTier`（**临时覆盖，不碰玩家存档**）——
            //      `CardProgress.SetLevel` 会**落盘改存档**，探针里⛔不许用。
            if (name == "Armoured Offensive")
            {
                int prevTier = CardView.DebugFrameTier;
                for (int tier = 3; tier <= 4; tier++)
                {
                    CardView.DebugFrameTier = tier;
                    var rArch = new GameObject("probe_strat_tier" + tier);
                    var vArch = CardView.Create(rArch.transform, data, name + "_tier" + tier);
                    Shot(vArch, Path.Combine(OutDir, SafeName(name) + "_tier" + tier + ".png"));
                    // 🆕 2026-10-22（`W64` §4·2(2)）：tier3/4 **也拍一张可打出描边** ——
                    //   原来每个 tier 只 `Shot` 一张常规态 ⇒「高 tier 的光带是不是也贴外沿」没有图可看。
                    ShotPlayableOutline(vArch, def,
                        Path.Combine(OutDir, SafeName(name) + "_tier" + tier + "_playable.png"));
                    Object.DestroyImmediate(rArch);
                }
                CardView.DebugFrameTier = prevTier;
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
    /// ⚠️ **只渲主视图**：`_tex`/`_selected`/`_unplayable` 那三张是排查用的，全池跑太贵。
    /// （原来还有一张 `_nofront`，**2026-10-10 删** —— 理由见 `Run()` 里那段注释。）
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
    ///    （`Core/CardText.cs` / `Core/CardIcons.cs`；`Core/TmpFont.cs` 给**全工程每颗 TMP** 挂了
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
    ///    而 **`keywords`（效果文字那层）今天就带 `&lt;sprite&gt;`**（`Core/CardText.cs` 的 `sprite`；sprite asset 的材质是独立一份 ⇒ **2 个槽**）⇒ 旧写法**漏掉图标**、读数会动 ⇒ **本笔在这层是活的**。
    ///    要「有牙」得先实跑确认那颗标签的 `textInfo.meshInfo.Length ≥ 2`（**要跑 Unity**）—— 线索交调度台。</summary>
    /// <returns>字墨中点（TMP 本地单位的 y）；**一个可见字形都量不到时回 `0f`**（哨兵值不变 ——
    /// ⚠️ 它与「字墨中点恰好是 0」撞车，调用点只打日志、不拿它判存在性）。</returns>
    internal static float InkCenterY(TMP_Text t)
    {
        float lo, hi;
        return InkSpanY(t, out lo, out hi) ? (lo + hi) * 0.5f : 0f;
    }

    /// <summary>**字墨**的纵向跨度（TMP 本地坐标）：该 TMP 全部可见字形顶点的 `[min, max]`。
    /// <para>🔴 **2026-10-22（`W62/T3`）从 `InkCenterY` 拆出来**：`CardBaseDemo` ② 要量墨心，
    /// 但「有没有量到字」得看**跨度**（`InkCenterY` 的 0f 是哨兵值，与「墨心恰好在原点」撞车）。
    /// `CardBaseDemo` 用的是同一份实现（⛔ 别在那边再写一份 —— 两处写同一条规则迟早不一致）。</para>
    /// <para>⚠️ 调用前必须已经 `ForceMeshUpdate()`（对象激活之后再调，否则 `textInfo` 是垃圾）。</para></summary>
    /// <returns>true = 量到了至少一个可见字形；false = 一个都没有（`lo`/`hi` 回 0）。</returns>
    internal static bool InkSpanY(TMP_Text t, out float lo, out float hi)
    {
        lo = 0f; hi = 0f;
        var ti = t != null ? t.textInfo : null;
        if (ti == null || ti.meshInfo == null || ti.meshInfo.Length == 0 || ti.characterInfo == null) return false;
        // 🔴 **2026-10-18（`A965`）**：网格槽按**字形自己的** `materialReferenceIndex` 取（原来写死 `[0]`，
        //    一行里混了 `<sprite name=…>` 就会把这个字串到 0 号槽里**同下标那个字**的顶点上）。
        //    取法照 `Editor/ShellScene.cs` 的 `SpanOfTmp` / `Editor/RewardsScene.cs` 的 `TmpVertPx`（`A851` 那三处）。
        var meshes = ti.meshInfo;
        var chr = ti.characterInfo;
        float l = float.MaxValue, h = float.MinValue;
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
                if (y < l) l = y;
                if (y > h) h = y;
            }
        }
        if (l > h) return false;   // 一个可见字都没有（空串 / 全不可见）
        lo = l; hi = h;
        return true;
    }

    /// <summary>拍一张「**可打出描边**」（`outlineGlow` 那层）的图 —— 🆕 2026-10-22（`W64`）。
    ///
    /// **为什么要它**：`_selected.png` 里那圈白光**是状态环 `_rim`**（`SetHighlight` 只写 tint + `_rim`），
    /// 而原版「打得出去」这一档走的是 **SDF `_Outline`**（入口 = `CardView.SetOutline`，
    /// 颜色判据 = `CardHighlight.OutlineOf`）。本探针原来**一次都没调过 `SetOutline`**
    /// （`W64` §2·3(b) 实测 grep 命中 0）⇒ `outlineGlow` 那层从来没被 Editor 侧渲染过。
    /// 判据 → `资料/普查产出_1022/W64_卡牌线_剩余与预判.md` §4·2(1) / §4·2(2)。
    ///
    /// 🔴 **批处理没有帧循环**（CLAUDE.md §三）：`SetOutline` 带 **0.2 s** 补间
    /// （`CardHighlight.OutlineAnimTime`）⇒ 不推的话拍到的是**补间起点 = alpha 0**（图上一片空，
    /// 看起来像「描边没做」，实际是没推）。做法 = 临时把 `CardTween.Mode` 切 `Manual`
    /// （`CardTween.Use` 在**建补间时**读它）→ `CardTween.Advance(时长 + 余量)` 推到头 → 还原。
    /// ⚠️ 推完**出声核对**「描边真的亮在 `Card Frame SDF` 那份材质上、alpha &gt; 0」—— ⛔ 不许静默渲一张空图当证据。
    /// </summary>
    static void ShotPlayableOutline(CardView view, CardDef def, string path)
    {
        var prevMode = CardTween.Mode;
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;      // 必须在 SetOutline 建补间之前切
            var want = CardHighlight.OutlineOf(def, canBePlayed: true);
            view.SetOutline(want);
            CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);   // 0.2 s 补间推到终点

            var got = view.OutlineColor;
            var dbg = view.OutlineDebug;
            // 🔴 **2026-10-24（L1 · `C1-B03` 并层）**：这里原来断的是「描边建在**专用 `outlineGlow` 层**上」。
            //   那一层**已删**（原版 `2DCard.prefab` 全文件只有 1 颗 `Card Highlight And Shadow`，
            //   `_Outline*` 与 `_ShadowColor` 是同一份 `Card Frame SDF` 的两个属性）⇒ 改断
            //   「亮起来的那份材质**就是 `Card Frame SDF`**」：`_Inner_Fallof = 0.224`
            //   （战斗状态环那份 `Card board Frame SDF` 是 **1.0** ⇒ 落错层会被这条抓到）。
            bool onSdf = Mathf.Abs(view.OutlineFloatForTest("_Inner_Fallof") - 0.224f) < 0.004f;
            if (got.a > 0.01f && onSdf)
            {
                Debug.Log($"[cardface] 可打出描边 → {path}：目标 {want} · 实测 {got} · {dbg}");
            }
            else
            {
                // 「不许静默失败」：渲出来没有描边，必须大声说，别让这张图被当成「原版就是这样」
                Debug.LogError($"[cardface] 可打出描边**没建出来 / 没亮**（{path}）："
                             + $"实测 alpha={got.a:F3}、材质是 `Card Frame SDF`={(onSdf ? "是" : "**不是**")} · {dbg}");
            }
            Shot(view, path);

            // 关掉描边（同一套手动推进）—— 免得同一张卡后面还要拍的图带着这一圈
            view.SetOutline(new Color(0f, 0f, 0f, 0f));
            CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
        }
        finally { CardTween.Mode = prevMode; }   // 抛异常也别把全局补间模式留在 Manual
    }

    /// <summary>`Shot` 的正交半高 —— 相机把整张卡（含卡框外沿与那圈发光）框进来的唯一来源。
    /// 🔴 **像素断言也读它**（`CheckOutlineGlow` 要把世界尺寸换成像素）⇒ 改这里等于同时改「图」和「量尺」。</summary>
    const float OrthoHalfH = 3.5f * 0.5f * 1.06f;      // 视野比卡本体大一圈：卡框 2.2452×3.2572 且偏下 0.03

    /// <summary>把当前场景里那张卡**渲成像素**（不落盘）—— `Shot` 与像素断言共用这一份。
    /// <para>相机：正交、正好框住整张卡；中心 y = −0.03 = 卡框中心；背景深底。</para>
    /// ⚠️ **它渲的是整个场景**（没有 `cullingMask`）—— 要「只渲这张卡」请走
    /// <see cref="RenderPixelsIsolated"/>（像素 A/B 断言必须走它，见那个方法的注释）。</summary>
    static Color32[] RenderPixels()
    {
        var camGo = new GameObject("probeCam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = OrthoHalfH;
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
        var px = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(camGo);
        return px;
    }

    /// <summary>🆕 **2026-10-24（`E3` · `DG3` 红 #1/#3 · δ 量具）：渲染隔离 —— 这一帧**只让 `root` 子树进画面**。
    ///
    /// <para>**为什么必须有它**：`RenderPixels()` 建一台相机渲**整个场景**，而 `CheckLayers()` 是从
    /// `CardBaseDemo.Run` 的中途（`CardBaseDemo.cs` 里 `CardFaceProbe.CheckLayers()` 那句）调起的 ——
    /// 那时现场里还有 **12 张手牌 + 3 张场上卡 + 背景 + 板位标记 + 粒子**。
    /// ⇒ `A7 ④a` 断的「硬矩形开/关逐像素不变」量到的 3 万点差里，有**不属于被测卡**的像素
    /// （夹具前提不成立：两次渲染只差被测卡的开关，别的对象本该逐位相同，但**现现场**里它们参与渲染）。</para>
    ///
    /// <para>**做法**：临时把 `root` 子树**以外**的所有 `Renderer` 置 `forceRenderingOff`，渲完在
    /// `finally` 里逐件还原（只还原「我们关掉的那些」，本来就关着的不碰）。
    /// 选它而不选另外两条：比「专用 layer + `cam.cullingMask`」**少一处全局配置**（不用改 TagManager、
    /// 不用担心 layer 号撞车），比「临时空场景」**少一堆建/拆副作用**（不会触发 `OnEnable` 之类）。
    /// ⚠️ 只影响这一帧的**可见集合**，⛔ 不碰材质 / 网格 / 队列。</para>
    /// <para>⚠️ `root == null` ⇒ 不隔离（等价于旧行为）。</para>
    ///
    /// <para>🔴 **2026-10-24（`E24`）补第二类载体：UGUI 的 `CanvasRenderer`。** 原来只关 `Renderer`
    /// （`MeshRenderer` / `SpriteRenderer` / `ParticleSystemRenderer`…）—— 它们在继承树上**都**是 `Renderer`，
    /// 但 UGUI 那几个 Graphic（`Image` / `Text` / `TextMeshProUGUI` / `RawImage`）画出来的东西**不是**
    /// `Renderer`，而是 `CanvasRenderer`（`Component` 的子类，见 `UnityEngine.CanvasRenderer`）⇒
    /// **一条都没被关掉**。只要现场里有一块 `ScreenSpaceCamera` / `WorldSpace` 的 Canvas 落在相机前面，
    /// 像素 A/B 的差集里就混进「被测卡以外」的像素（正是这个方法要消灭的那一类）。
    /// 做法：`SetAlpha(0)` ＋ `cull = true` **两条一起上**（前者让它整块变全透明、后者把它从 Canvas 的
    /// 批次里摘掉 —— 只做其中一条时不同 Canvas 模式下行为不一致），`finally` 里原样还原
    /// （`cull = false` + 把 `GetAlpha()` 存下来的值写回去）。
    /// ⚠️ 本来就 `cull == true` 的不碰（还原时会误开）。`ScreenSpaceOverlay` 的 Canvas 本来就不进相机
    /// 的 RT（它由 Canvas 系统直接合到屏幕上），但**照样一起关** —— 不花代价，且不依赖「哪种模式会漏」。</para></summary>
    static Color32[] RenderPixelsIsolated(GameObject root)
    {
        LastIsolatedCulledRenderers = 0;
        LastIsolatedCulledCanvasRenderers = 0;
        if (root == null) return RenderPixels();
        // ---- ① 普通渲染器（`Renderer.forceRenderingOff`）----
        var all = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var touched = new List<Renderer>();
        // ---- ② UGUI（`CanvasRenderer`：**不是** `Renderer` 的子类）----
        var canvases = Object.FindObjectsByType<CanvasRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var culledCanvas = new List<CanvasRenderer>();
        var keptAlpha = new List<float>();
        try
        {
            for (int i = 0; i < all.Length; i++)
            {
                var r = all[i];
                if (r == null || r.forceRenderingOff) continue;          // 本来就关着 ⇒ 别记进来（还原时会误开）
                if (r.transform.IsChildOf(root.transform)) continue;     // 被测卡自己的每一层都留着
                r.forceRenderingOff = true;
                touched.Add(r);
            }
            for (int i = 0; i < canvases.Length; i++)
            {
                var cr = canvases[i];
                if (cr == null || cr.cull) continue;                      // 本来就 culled ⇒ 别记（还原时会误开）
                if (cr.transform.IsChildOf(root.transform)) continue;     // 被测卡自己（若它带了 UGUI）留着
                keptAlpha.Add(cr.GetAlpha());                             // 必须先存再改（`SetAlpha` 会覆盖）
                culledCanvas.Add(cr);
                cr.SetAlpha(0f);
                cr.cull = true;
            }
            LastIsolatedCulledRenderers = touched.Count;
            LastIsolatedCulledCanvasRenderers = culledCanvas.Count;
            return RenderPixels();
        }
        finally
        {
            for (int i = 0; i < touched.Count; i++) if (touched[i] != null) touched[i].forceRenderingOff = false;
            for (int i = 0; i < culledCanvas.Count; i++)
            {
                if (culledCanvas[i] == null) continue;
                culledCanvas[i].cull = false;
                culledCanvas[i].SetAlpha(keptAlpha[i]);
            }
        }
    }

    /// <summary>只给断言读（🆕 2026-10-24 `E24`）：上一次 `RenderPixelsIsolated` **关掉了几个普通渲染器**
    /// （`Renderer.forceRenderingOff` 那条路）。`0` 也合法（现场里没有别的东西）。</summary>
    public static int LastIsolatedCulledRenderers { get; private set; }

    /// <summary>只给断言读（🆕 2026-10-24 `E24`）：上一次 `RenderPixelsIsolated` **关掉了几个 `CanvasRenderer`**
    /// （UGUI 那条路）。⚠️ 改之前这条恒 0（那条路根本不存在）⇒ 断言「> 0」在旧版上是红的。</summary>
    public static int LastIsolatedCulledCanvasRenderers { get; private set; }

    /// <summary>只给断言读（🆕 2026-10-24 `E24`）：`RenderPixelsIsolated` 的**公开入口** ——
    /// 断言要用同一份量具（⛔ 别在断言里另写一台相机，那是第二把尺子）。</summary>
    public static Color32[] RenderPixelsIsolatedForTest(GameObject root) { return RenderPixelsIsolated(root); }

    /// <summary>只给断言读（🆕 2026-10-24 `E24`）：**不隔离**的整场渲染（= 旧行为）。
    /// 断言用它当**对照**：「隔离关掉了某个东西」必须先用它证明「那个东西本来是画得出来的」。</summary>
    public static Color32[] RenderPixelsForTest() { return RenderPixels(); }

    /// <summary>渲染分辨率（`W × H`）；断言换算世界→像素要用。</summary>
    public static int RenderWidthForTest { get { return W; } }
    public static int RenderHeightForTest { get { return H; } }

    /// <summary>把这张卡单独渲进一张图 —— 相机正交、正好框住整张卡（含卡框外沿）</summary>
    static void Shot(CardView view, string path, bool quiet = false)
    {
        var px = RenderPixels();
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        if (!quiet) { Debug.Log($"[cardface] 写出 {path}"); }
    }

    /// <summary>`新账-66 ②`：**外发光必须围卡框，不是围插图** —— 渲两张、逐像素取差。
    ///
    /// <para>**为什么非渲不可**：图层/材质/quad 的读数全对，也可能被上层不透明件遮死、
    /// 或等值带落在插图画窗里 —— 只有渲染结果能回答「光带最后落在哪一圈像素上」。
    /// 判据与阈值理由写在调用处那段注释里。</para>
    ///
    /// <para>⚠️ **批处理没有帧循环** ⇒ `SetOutline` 带 **0.2 s** 补间，必须
    /// `CardTween.Mode = Manual` + `Advance` 推到头；否则拍到的是补间起点（α=0，一张空图）。</para>
    ///
    /// <para>🔴 **2026-10-24（`H2` · `E3` 顺手发现 1）：两张图都走 `RenderPixelsIsolated(root)`。**
    /// 原来走整场 `RenderPixels()` —— 它今天绿，只是因为别的对象在两张里都出现、**天然抵消**；
    /// 一旦有东西挡在被测卡前面（`CardBaseDemo.Run` 的活现场里有 12 张手牌 + 3 张场上卡 + 粒子），
    /// 描边 bbox 会被裁掉而**仍然绿**。隔离后「被测卡以外的像素」一个都不参与差集。
    /// ⚠️ 预期读数：`n`（差集点数）与本条的两条外沿断言**不变**（或 `n` 略增 = 把原来被挡掉的
    /// 描边像素补回来）；若 `n` 掉到 200 以下，是「描边本来就靠别的对象衬托」⇒ 那是**真问题**，别调阈值。</para>
    ///
    /// <para>🔴 **灭自证**：差值取的是**渲染结果**，⛔ 不是材质读数、也不是把算法再算一遍。
    /// 把描边挂回立绘层（`_art` 的网格被 `ArtWindowMesh` 裁在开口里），或把 `ShadowSize`/`ShadowY`
    /// 改到「等值带落进插图画窗」那一档 ⇒ 下面左右两条立刻红。
    /// （⚠️ 2026-10-24 `C1-B03` 并层：原来这里写的是 `OutlineGlowScale` —— 常量已删，几何只剩一份
    /// `ShadowMesh()`，所以「改坏法」也跟着换成 `ShadowSize`/`ShadowY`。）</para>
    /// </summary>
    static void CheckOutlineGlow(string cardName)
    {
        var pool = CardDatabase.Load();
        var def = FindByName(pool, cardName);
        if (def == null)
        {
            Debug.LogError($"[cardface-chk] 卡池里没有 `{cardName}` ⇒ 新账-66 ② 这张**没验到**（别读成通过）");
            return;
        }
        var root = new GameObject("chk_glow_" + SafeName(cardName));
        var view = CardView.Create(root.transform, BattleDriver.ToCardData(def, def.Faction), cardName + "_glow");
        try
        {
            var prevMode = CardTween.Mode;
            Color32[] off, on;
            try
            {
                CardTween.Mode = DG.Tweening.UpdateType.Manual;   // 必须在 SetOutline 建补间之前切
                view.SetOutline(new Color(0f, 0f, 0f, 0f));
                CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
                // 🔴 **2026-10-24（`H2` · `E3` 顺手发现 1）**：渲**隔离**图（只让 `root` 子树进画面），
                //   不再走整场 `RenderPixels()`。理由与 A7 那条相同（见 `RenderCardPixels` 的 doc）：
                //   本方法从 `CardBaseDemo.Run` 的**活现场**里调起，整场渲会把手牌 / 场上卡 / 背景 /
                //   粒子的像素一起带进来 —— 「别的对象天然抵消」只是**当前现场恰好如此**，
                //   「卡被别的对象挡住 ⇒ 描边 bbox 被裁」那一类静默失真它挡不住。
                //   ⚠️ 两张图（开 / 关）都必须走同一条隔离路，否则差集会混进「隔离本身」的差异。
                off = RenderPixelsIsolated(root);                 // 描边关（只渲这张卡）

                view.SetOutline(Color.white);                     // 纯白 α=1 ⇒ 差集最干净
                CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
                // 🔴 **2026-10-24（L1 · `C1-B03` 并层）**：原来这里断「描边建在**专用 `outlineGlow` 层**上」。
                //   那一层已删，描边与软光/影是**同一层同一份材质**（原版只有 1 颗
                //   `Card Highlight And Shadow`）⇒ 前提改成「点亮的 `_Outline` 落在 `Card Frame SDF` 上」。
                //   分辨法 = `_Inner_Fallof = 0.224`（战斗状态环那份 `Card board Frame SDF` 是 1.0）。
                //   ⚠️ 仍然**不是**恒真：「描边写到别的材质/别的层的实例上」这一档照样红。
                Check(Mathf.Abs(view.OutlineFloatForTest("_Inner_Fallof") - 0.224f) < 0.004f,
                      $"新账-66 ② 前提：`{cardName}` 的点亮描边落在 **`Card Frame SDF`**（`_Inner_Fallof = 0.224`）上"
                    + $"（实测 `_Outline`={view.OutlineColor} · {view.OutlineDebug}）");
                on = RenderPixelsIsolated(root);                  // 描边开（同上，只渲这张卡）
            }
            finally { CardTween.Mode = prevMode; }

            // 差集：只认「**亮起来**」的像素（任一通道差 > 40/255）。
            // 阈值取得高是**故意的**：软光/影那层底色很暗（黑 α 0.6 压在深底上），
            // 噪声/时相在两张之间即使有抖动也远达不到 40 ⇒ 不会把整块 4.4281² 的 SDF quad 算成「发光」。
            int x0 = int.MaxValue, x1 = int.MinValue, y0 = int.MaxValue, y1 = int.MinValue, n = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    int d = Mathf.Max(Mathf.Abs(on[i].r - off[i].r),
                              Mathf.Max(Mathf.Abs(on[i].g - off[i].g), Mathf.Abs(on[i].b - off[i].b)));
                    if (d <= 40) continue;
                    n++;
                    if (x < x0) x0 = x; if (x > x1) x1 = x;
                    if (y < y0) y0 = y; if (y > y1) y1 = y;
                }

            // 卡框在**同一套相机像素坐标**里的包围盒：相机中心 y = −0.03 = 卡框中心（见 `Shot`/`RenderPixels`）
            float ppu = H / (2f * OrthoHalfH);
            float fw = view.FrameDrawSize.x * ppu, fh = view.FrameDrawSize.y * ppu;
            float fL = W * 0.5f - fw * 0.5f, fR = W * 0.5f + fw * 0.5f;
            float fB = H * 0.5f - fh * 0.5f, fT = H * 0.5f + fh * 0.5f;
            // 判据（离线量过 5 张 `_strat` 卡框的开口并集）：插图画窗的**半宽 ≈ 卡框半宽的 0.68**
            // ⇒ 取 0.10 卡框宽做容差 = 落在「围插图（≈0.68）」与「围卡框（外沿 ≥0.9）」两档之间。
            float margin = fw * 0.10f;

            Check(n > 200, $"新账-66 ② `{cardName}`：描边**真的画上屏了**（差集 {n} 点；0 ⇒ 那层被遮死 / 没建）");
            if (n == 0) return;
            Debug.Log($"[cardface-chk]   新账-66 ② `{cardName}` 实测：发光 bbox x[{x0},{x1}] y[{y0},{y1}]"
                    + $"（{x1 - x0 + 1}×{y1 - y0 + 1} px / {n} 点）· 卡框 bbox x[{fL:F0},{fR:F0}] y[{fB:F0},{fT:F0}]"
                    + $"（{fw:F0}×{fh:F0} px）· 立绘实绘 {view.ArtDrawSize.x * ppu:F0}×{view.ArtDrawSize.y * ppu:F0} px");
            Check(x0 <= fL + margin,
                  $"新账-66 ② `{cardName}`：发光**伸到卡框左外沿**（发光 x0={x0} ≤ 卡框左沿 {fL:F0}+{margin:F0}={fL + margin:F0}）"
                + "｜🧨 光带若退到插图画窗（用户报的那一档）x0 ≈ 开口左边、比这条线还靠里 ~40 px ⇒ 红");
            Check(x1 >= fR - margin,
                  $"新账-66 ② `{cardName}`：发光**伸到卡框右外沿**（发光 x1={x1} ≥ 卡框右沿 {fR:F0}−{margin:F0}={fR - margin:F0}）"
                + "｜🧨 描边挂回立绘层 ⇒ 光带被 `ArtWindowMesh` 裁在开口里 ⇒ 红");
        }
        finally { Object.DestroyImmediate(root); }
    }
}
