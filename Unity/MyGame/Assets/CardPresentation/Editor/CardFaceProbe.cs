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
        "stormlord",             // Sautekh legendary 督军「风暴王」（用户点名问的）
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
                                     + "—— 建不出来下面几条**没验到**，别读成通过");

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

            // ⑤ `talent`（天赋）**不进场上徽标** —— 判据 = **用户 2026-10-12 真机口径**
            //    （⛔ 不是原版读数：`HasIcon && IsInPlayIcon` 那两个开关本地取不到，见 `Badges.NotInBoardBadges`）。
            //    🧨 改坏法：把 `Badges.NotInBoardBadges` 里那一条删掉 ⇒ 必红。
            //    🔴 **灭自证**：同表里**别的关键词必须照画** —— 少了这一半，「整个表都不画」也算绿。
            var kwT = new Dictionary<string, int> { { "Talent", 1 }, { "Armour", 2 } };
            var bl = Badges.For(kwT);
            bool hasTalent = false, hasArmour = false;
            for (int i = 0; i < bl.Count; i++)
            {
                if (Badges.Norm(bl[i].key) == "talent") hasTalent = true;
                if (Badges.Norm(bl[i].key) == "armour") hasArmour = true;
            }
            Check(!hasTalent, "A1661 ⑤ `talent`（天赋）**不画**成场上徽标（判据 = 用户 2026-10-12 口径）");
            Check(hasArmour && bl.Count == 1,
                  $"A1661 ⑤ 同一张表里别的关键词**照画**（`armour` 在、整表 {bl.Count} 条 —— "
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

        Debug.Log($"[cardface-chk] === 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

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
