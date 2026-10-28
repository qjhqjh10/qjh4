using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using RuleEngine;

/// <summary>
/// 引擎离线对拍探针 —— **快速内环 / 旁证**。
/// ⚠️ **正式验收仍然是 `RuleEngineTest.Run`（断言的真宿主）**，本工具只是让「改完立刻能对一眼」变便宜。
/// 原理：`RuleEngine/Core/` 按工程成文规矩**只许碰 `UnityEngine.Debug` / `UnityEngine.Random`**
/// （见 Core 各文件头注 + `TutorialScript.cs:344-352`）⇒ 三十行桩就能把整个 Core 编成控制台程序，
/// **不跑 Unity、不占 Unity 实例、不用等 1–9 分钟编译**（实测重编 ~2 秒）。
/// 收编自 `D:/tmp/wf_b14_probe/`（2026-10-18；那份在临时目录里、迟早被清）。
/// </summary>
class Program
{
    static List<CardDef> Pool;
    public static string OutDir;

    /// <summary>
    /// 🆕 **`keywords` 的【原文条目】、与 <see cref="Pool"/> 同序**（下标一一对应）。
    ///
    /// 🔴 **为什么要留一份**：`CardDef.Keywords` 存的是 `KeywordTable.Parse` **规范化之后的键**
    ///   （`survivor` / `armour` 这种），**看不到原文** —— 而「带正文的触发关键词」在卡表里
    ///   是**整条字符串**（`"Sacrifice: Gain +1 Attack"`，见 `A1386` 那个实测用例）。
    ///   `willrun` 要判「卡面上写着哪几段正文」，就必须看得见这些原文；
    ///   ⛔ 这不是重新解析一遍，只是**把入参留个底**（`CardDef` 没有给它开只读窗口）。
    /// </summary>
    public static List<List<string>> PoolRawKeywords = new List<List<string>>();

    /// <summary>🆕 **卡池路径覆盖**（环境变量 `WFPROBE_POOL`，2026-10-12 第二十一会话 · 写手 `E5`）。
    ///
    /// **为什么要它**：验证一条**尺子**最直接的办法是**给它一个已知的输入** —— 而本仓的卡池
    /// （`MyGame/Assets/RuleEngine/Resources/cards_engine.json`）**归别的工具/写手**
    /// （作者 `工具/gen_cards_engine.py`），探针⛔ **不许改它**（`A1382` 那条哨兵正是靠「探针对卡池只读」立住的）。
    /// ⇒ 允许**指向一份副本**做**故障注入**：
    ///   `WFPROBE_POOL=D:/4/_tmp_e5/pool_mutated.json bash d:/4/Unity/工具/ruleprobe.sh play check`
    ///   ⇒ 当次现算的读数**真的变了** ⇒ 对拍应当**指名到卡、到列**（尺子的灵敏度实测）。
    ///
    /// 🔴 **它一律出声**（<see cref="LoadPool"/> 打横幅）—— ⛔ 不许静默换池：那会让「读数对不上工程现状」
    ///   变成一桩**谜案**（本仓反复吃过的亏）。
    /// ⚠️ 覆盖生效时：`check` 的哨兵 ⑤ 走「此时不判」（池 md5 与钉住的不同），
    ///   而 ⑦（解析输出指纹）**会红** —— **那是对的**（你看的那份池本来就不是被确认的那一份）；
    ///   `play check` 的哨兵 ② 只看**基线文件本身**，⛔ 不受覆盖影响。
    /// ⛔ **不许**拿覆盖之后的读数去重钉任何常量（那是「把演示当现状」）。</summary>
    public static string PoolOverride
    {
        get { try { var v = Environment.GetEnvironmentVariable("WFPROBE_POOL"); return string.IsNullOrEmpty(v) ? null : v; } catch { return null; } }
    }

    /// <summary>卡池路径 —— 由仓库根推出来，⛔ 不写死盘符绝对路径。
    /// 🆕 环境变量 `WFPROBE_POOL` 可覆盖（见 <see cref="PoolOverride"/> —— 只为**故障注入演示**，
    /// 它每处都会出声）。</summary>
    public static string PoolPath
    {
        get
        {
            var o = PoolOverride;
            return string.IsNullOrEmpty(o)
                ? Path.Combine(OutDir, "RuleEngine", "Resources", "cards_engine.json")
                : Path.GetFullPath(o);
        }
    }

    /// <summary>已装载的卡池（<see cref="LoadPool"/> 之后非 null）—— 新子命令取池的**唯一**出口。</summary>
    public static List<CardDef> PoolList { get { LoadPool(); return Pool; } }

    // ==================================================================================
    //  🔴 `A1382`：**基线指纹** —— 「任何真回归 ＋ 重生成基线 ⇒ 静默变绿」的机器级堵法
    // ==================================================================================
    //
    // **病灶**（台账 `A1382` · 出处 `资料/普查产出_第十三会话/W5d_A1339探针口径.md:173`）：
    //   `check` 的判据是「拿当前输出与**基线**比」，而**基线同一个工具就能重写**
    //   （`scan 基线/out_baseline.txt` 一条命令就覆盖它）⇒ 谁红了顺手重跑一次 `scan`，
    //   真回归被**静默抹平**、界面还显示全绿。
    //   `A1348` 那组哨兵只封了「**已知的**几类回归」（索引 / 货币名 / 目标列清单），
    //   **一般情形没封死** —— 这一条要治的正是「一般情形」。
    //
    // **为什么必须有【本工具写不动】的参照物**：哨兵写在同一份源码里、基线写在同一个目录里，
    //   两者都在本工具的写权限之内 ⇒ 光靠「两者互相印证」是**自证**（本仓红线）。
    //   ⇒ 参照物取**卡池文件**（`cards_engine.json`）—— 它的作者是**另一个工具**
    //     （`工具/gen_cards_engine.py`），本探针**只读**。
    //
    // **判据（两条 md5 配对，每次 `check` 现算）**：
    //   · `现算卡池 md5 == PinnedPoolMd5`（= **解析器的输入没变**）
    //     **且** `现算基线 md5 != PinnedBaselineMd5`（= 基线被改写了）
    //     ⇒ 🔴 **出声并计入 rc**：输入没变而基线变了，只有两种可能 ——
    //        ① **有人用 `scan` 把一次真回归抹平了**（要修解析器）；
    //        ② 有意重生成了一次（那就要**同时重钉下面两个常量** —— 改源码是**显式**动作、
    //           会出现在 `git diff` 里，这正是本哨兵要的「让静默动作变响」）。
    //   · 卡池 md5 对不上 ⇒ ⚠️ **只打印、不算失败**（卡池本身变了 ⇒ 两个指纹**都该重钉**）。
    //     ⛔ 不在这里判「卡池变没变坏」—— 那是**别的工具**的账。
    //
    // 🔴 **重钉的办法**（⛔ 没有「一条命令自动重钉」—— 那等于把这条洞又开回来）：
    //   跑 `check`，把它打印出来的两行现值抄进下面两个常量（**手改源码**）。
    //
    // ⚠️ **已知的假阴**：真回归 ＋ 重生成基线 ＋ **卡池也在同一批里变了** ⇒ 本哨兵**看不见**
    //   （卡池 md5 对不上那一支只打印）。如实标着，⛔ 不假装它封死了所有情形。
    // ==================================================================================

    /// <summary>钉住的卡池内容指纹（`MyGame/Assets/RuleEngine/Resources/cards_engine.json` 的 md5，**按字节**）。</summary>
    // 🔴 2026-10-11（第二十会话 · 写手 `E2`）**有意重钉**：旧值 `cfc13db1…`（= `HEAD` 里那一版卡池）。
    //    重钉理由：**卡池文件本身被改了**（本轮现算 `b2ae84b2…`，同一个会话里 `C1` 在改 `数据/`）
    //    ⇒ 上面那段判据里「**解析器的输入没变**」这一半不成立，`check` 一路走的是
    //    「⚠️ 卡池 md5 与钉住的不同 ⇒ 本哨兵此时不判」那一支（那是**出声**、不是绿）。
    //    ⇒ 按本文件上面写的流程把**两个**指纹一起重钉到现值（卡池 ＋ 基线，见下）。
    //    ⚠️ 卡池还在别人手里改（同会话并行写手）⇒ 它一变，这一条**又会**回到「此时不判」那一支；
    //      那是本哨兵**已知的**限制（判据原文见上面那段长注释），⛔ 不是失败。
    // 🔴 **2026-10-14（第二十七会话）三个指纹【有意重钉】—— 依据是下面四条，⛔ 不是为了把红抹平。**
    //   ① 变化的性质 = **纯改标签、解析逐字节没动**。实测：现算 dump 与旧基线**逐行比**，
    //      1120 行里 949 行不同，而**去掉第 1 列（id）之后 0 行不同** ⇒ 只有 id 变了
    //      （`diff <(cut -d'|' -f2- 新) <(cut -d'|' -f2- 旧)` = 0 行）。
    //   ② 成因是**别人改的**：第二十六会话把全池卡牌 id 换成原版真值（`A1758`，落地记录
    //      `资料/卡牌id_换真值_落地记录.md`）⇒ `cards_engine.json` 的 id 列整体置换，本探针只读它。
    //      那次换 id **已被 `RuleEngineTest.Run` 实跑确认**（5294 / 5294 全过，见
    //      `资料/交接_第二十七会话.md` §一）—— 这就是本哨兵要的「重钉前先确认」。
    //   ③ 两条**哨兵卡自己也过期了**（它们报的红是假红），同日就地换卡：
    //      `UM_Angels_of_Death` → **`UM64`**（`Angels of Death`）· `SOR72` → **`SOR71`**
    //      （`SOR72` 现在是 `Castigator`）。判据 = 现读卡表 + 卡图（见那两处的行内注释）。
    //   ④ ⚠️ 上一次重钉（`A1619`）漏了第三个指纹 ⇒ 这次三个一起钉（否则哨兵 ⑦ 永远红）。
    // 🔴 **2026-10-14（第二十九会话 · 账 `A1908`）有意重钉** —— 三个指纹一起钉，依据如下：
    //   ① 基线 vs 现算共 **218 行**差异，逐行分类（脚本 `_tmp_view/arena2/classify2.py`）：
    //      · **A 类 114 行** = 只多了新的可选块（`/{ATTR:}` `/{TAIL:}` 等），**把块删掉后逐字节相同**；
    //      · **B 类 104 行** = 真变化，且**只有两族、零条删除既有 op**：
    //          - **86 行**：`Talent: X` 从 `unparsed` 列搬成一条 `create/pool///1/X/…` op
    //            （`AM2 Ursula Creed` 等；`GSC37` 是复合句 `Talent: X,Costs 1 less…` 被正确切开）；
    //          - **18 行**：新增 `triggerability/pool///0/pray/…` op（战祷那一族）。
    //      ⚠️ 唯一一处「op 少了」是**假警报**：`SOR5 Battle Sister` 的 `gain` op 多了 `/{TAIL:heal 1}/`
    //         块 ⇒ 字符串不等被我那个朴素 `;` 切分读成「删了一条」；**op 本身没少**。
    //   ② **确认依据 = `RuleEngineTest.Run` 5386 / 5386 全过**（第二十八会话收口，
    //      见 `资料/交接_第二十九会话.md` §一）—— 这就是本哨兵要的「重钉前先确认」。
    //   ③ 卡池 md5 变了是**别人改的**（`cards_engine.json` 由 `工具/gen_cards_engine.py` 生成，本探针只读）。
    //   ④ 承上一次教训（`A1619` 漏第三个）：三个一起钉，否则哨兵 ⑦ 永远红。
        // 🔴 **2026-10-14（第三十会话）有意重钉 —— 两个常量一起钉**：
    //   ① `PinnedPoolMd5`：第三十会话按 `A1987`/`A1993` 改了 **6 张卡**的卡面文字
    //      （`SOR9`/`TAU2`/`TL_Psychophage` 补 `Talent:` 行 · `GOF18`/`BL34` 补 `Talent:` 行与中文名 · `SOR29` 的 `Deal 3` → `Deal 2-3`）
    //      ⇒ 卡池 md5 必变（`9ab79186…` → `37f349cb…`）；
    //   ② `PinnedParseMd5`：`check` 现跑的「解析差异」= **6 行**，**前 6 张正是上面那 6 张、一张不多不少**
    //      ⇒ 差异**可逐条归因到那次改动**，不是解析器被改坏 ⇒ 有意重钉（`e3a4b082…` → `79c93e35…`）；
    //   ③ `PinnedBaselineMd5` **本趟不动** —— 基线文件 `基线/out_baseline.txt` 现读 md5 = `6c0e41d71599bd7a42f078a50e90f614`，与它**逐字符相同**。
    //   ⛔ **这不是「把红抹平」**：与 `A1943` 那次重钉吞掉 1118 行漂移不同 —— 本趟的差异**只有 6 行、且能点名列出来**。
    // 🔴 **2026-10-18（第五十会话 · `K06` 池 id 大迁移）有意重钉 —— 三个常量一起钉**：
    //   ① 触发事件：`cards_engine.json` 里 **114 张卡的 id** 换成原版真 id（与 `card_ids.json.poolTrueIds` 逐条一致）。
    //   ② 判据（**比「跑一遍看看」更硬**）：用 `WFPROBE_POOL=<HEAD 卡池>` 与当前卡池各扫一遍全池，
    //      **按卡名对齐**后逐字节比较 —— **1,118 个卡名 · 0 处内容差异**（`/d/4/_tmp_view/k06/scan_{now,head}.txt`）。
    //      ⇒ 变化的**只有 id 那一列**；解析器/卡面文字一个字没动。
    //   ③ 基线按 `scan` 重生成后，`check` 现读 **解析差异 0 行 · 改名 0 · 新增 0 · 消失 0**。
    //   ⛔ **不是把红抹平**：抹平的定义是「真差异被基线吞掉」；这里三条指纹+上面那份按名对齐的证明互相独立。
    //   ⚠️ 本趟**没跑** `RuleEngineTest.Run`（现行「只做不验」口径）—— 确认依据是 ②，不是旧惯例里的那一句。
    // 🔴 **2026-10-19（第五十六会话 · `A1973` 收口重钉）三个指纹一起钉 —— 依据见下，⛔ 不是为了把红抹平。**
    //   ① 解析侧 **18 行差异 + 3 张「改名且解析同时变」**，逐条归因（**全部**来自第五十五会话已核销的活）：
    //      · `Talent:` 桥 5 张（`AM15` `DA1` `GOF87` `GOF77` `SW1`）= `A2142`（E12）；
    //      · `choicesDefinition → choosecard/specific` 6 张（`BL4` `BL13` `BL37` `BL38` `141c5ed6` `BL57`）
    //        = `A1959`/`A1960`（E9/E12）；
    //      · `DeadScope` 标记 5 行（`EC39` `GSC44` `SAU39` `SOR42` `UM61`）= `A1568②`（E8，
    //        账上原话就是「实测 5 行不是 3 行」）；
    //      · `equal to the number of cards in the enemy hand` 抬成 `AmountRef` 2 张
    //        （`GSC35` `GSC59`）= `A2001`（E9）；
    //      · `TL16` = 本会话 `A2000`（`this card` 认成施放者自己）；`GOF12` = E10 按 SO 的 5 项落地；
    //        `SW38` = 卡面补了 `Your Warlord has Armour 2`（数据侧）。
    //      另有 **63 行「改名」**（卡名规范化产物，`DefOf` 逐字节相同）—— 其中 3 张同时带解析变动
    //      （`BL37`/`GOF12`/`SW38`，归因同上）。
    //   ② 结算侧 **主档 15 张** = `AM58`（`A1571` draw+autoPlay）· `BL38`（choose 注入）·
    //      `BL55`/`SW57`（`A1571` autoPlay）· `GOF77`/`GOF87`/`SW1`（`A2142`）· `GOF93`（`A2143`，E12）·
    //      `GSC35`/`GSC59`/`SOR14`（`A2001`/E9）· `SW38`（卡面）· `TL37`/`TL49`/`TL8`（E8/E9 同批引擎改动，
    //      E8b `§5·2` 已把后 13 张逐张列过）。
    //   ③ **电池 586 行全变** = `A1965` 事件电池新增 6 步（E8）—— 输出格式的有意扩展，不是行为漂移。
    //   ④ 本趟**没有**新增未归因差异：重生成后 `check` 现读 **解析差异 0 行 · 改名 0 · 新增 0 · 消失 0**。
    // 🔴 **2026-10-19（第五十七会话 · 卡牌线 `CA-2` · `A1995②`/`A2002`）有意重钉 `PinnedPoolMd5`**：
    //   本批只改了 **18 个卡面字段**（`AM18`/`AM52`/`EC37` 的 `desc` 删句末句点 · 15 张 `descZh` 的
    //   天赋名换官方 `Card_Name` 中文名）⇒ `cards_engine.json` 内容必变，`41fc8751…` → `9527c129…`。
    //   ⛔ 不是把红抹平：**另外两个指纹原样不动** —— `PinnedParseMd5` 现跑仍一致（解析差异 0 行），
    //   `PinnedBaselineMd5` 现读与基线文件逐字符相同（没有重生成过基线）。判据 → `资料/普查产出_1019/W57_卡牌线_T08数据落地.md`。
    // 🔴 **2026-10-19（第五十七会话 · 卡牌线 `CA-3` · `A2047`）**：本笔先按 PnP 卡面把 5 张 `descZh`
    //   （`GOF104`/`SW26`/`SW6`/`SW51`/`UM77`）的 `troop` 从官方旧译「单位」改成「部队」、并把卡池钉到 `ecff0724…`；
    //   随后**调度台裁定「显示层以官方中文表为准」**（`CLAUDE.md` 的官方优先口径 + 用户 `A1945` 拍板）⇒ **5 条已全部回滚**，
    //   卡池 md5 回到 `CA-2` 钉的 `9527c129…`（本轮两次重生成逐字节一致），**本常量随之改回**。
    //   ⛔ 另外两个指纹原样不动：`check` 现跑 **解析差异 0 行 · 哨兵失败 0 · rc=0**。判据 → `资料/普查产出_1019/W57_卡牌线_T03中文与官方表.md` §续。
    // 🔴 **2026-10-19（第五十七会话 · 阵营机制写手 `FACTION-DATA` · 账 `W57 X4`）**：`SOR31` / `SOR44` /
    //   `Righteous Repugnance` 三张的 `desc` 把 Faith 误抄成 Energy，已改 `cardface_fixes.json` 并重生成
    //   （两次 `--write` 逐字节一致）⇒ 卡池 `9527c129…` → `e5894c40…`。`descZh` 两张均**保留官方措辞**
    //   （`SOR31` 的 `((Faith_cost_x,1))` 与 RR 的 `4 能量：` 都由显示层 `RewriteMarks` / `RewriteFaithCost`
    //   渲染成「数字 + 信仰图标」——后者是调度台 2026-10-19 的裁定）。
    //   ⛔ 本轮**只**钉这一格（白名单）：
    //   `check` 现读**解析差异 = 本笔 3 张**（`SOR31` `gainenergy`→`gainfaith` · `SOR44`/RR `cost 5/4` 的 `ck` 空→`faith`）
    //   ＋并行写手的 `ASH34`（`Talent:` 修复）⇒ `PinnedParseMd5` **应重钉为 `23da247f…`**（由主对话裁定，本轮未动）；
    //   `PinnedBaselineMd5` 原样不动（基线文件没重生成）。判据 → `资料/普查产出_1019/W57_阵营机制_修女三卡数据.md`。
    //   🔴 **2026-10-21（第五十九会话 · 引擎线 `T02`）重钉** —— 卡池 md5 现值变了（`a56aa0ba…`）：
    //   本批**没有改任何卡数据**，是**卡池内容随前几轮落盘**后这条钉子一直没跟上（`check` 自报「三个指纹都该重钉」）。
    //   同日改动只有解析层一处（`SOR31` 的 `or is a friendly unit` 新条件种类），逐行核过 ⇒ 按工具首条判据一并重钉。
    //   🔴 **2026-10-22（第六十三会话 · `W63` · `abilityContext` v14）有意重钉** —— 卡表新增 `abilityContext` 列 ⇒
    //   卡池 md5 变（`99d4f87e…`）。**解析输出逐字节没动**（`check` 解析差异 0 行 · ⑦ 输出指纹 `c9f6c642…` 仍一致）
    //   ⇒ **只重钉 PinnedPoolMd5**，`PinnedBaselineMd5` / `PinnedParseMd5` 原样不动。
    // 🔴 **2026-10-22（第六十四会话 · B-16 条件结构化）有意重钉**：`cards_engine.json` v14→v15，
    //   新列 `unitConditions`（池内 167 张 / 218 条）⇒ 卡池 md5 变（`99d4f87e…` → `0dd76354…`）。
    //   解析输出逐字节没动（`check` 解析差异 0 行 · ⑦ 输出指纹 `c9f6c642…` 仍一致）⇒ 只重钉这一颗。
    // 🔴 **2026-10-22（第六十四会话 · B-16 count 被数集合）有意重钉**：`cards_engine.json` v15→v16，
    //    `role=="count"` 行新增 `countSet`（池内 4 张 / 6 条；37 格）⇒ 卡池 md5 变
    //    （`0dd76354…` → `b7b19efd…`）。解析输出逐字节没动（`check` 解析差异 0 行 · 输出指纹 `c9f6c642…`）。
    // 🔴 **2026-10-23（引擎线 · `B-16` 残余 241/242 ＋ `B-17`）有意重钉**：`cards_engine.json` v16→v17，
    //    新列 `abilityTriggers`（1127 张 / 1008 张非空 / 1193 个 trigger 值）＋ `abilityTargets`
    //    （1127 张 / 1270 条 ability / 1616 个 logic）⇒ 卡池 md5 变（`b7b19efd…` → `3f4002d0…`、
    //    1,164,810 → 1,218,806 B）。解析输出逐字节没动（`check` 解析差异 0 行 · 输出指纹 `c9f6c642…`）
    //    ⇒ **只重钉这一颗**，`PinnedBaselineMd5` / `PinnedParseMd5` 原样不动。
    //    判据 → `资料/普查产出_1023b/引擎线_尾巴_执行报告.md` §5·1 / §6·1。
    // 🔴 2026-10-23（引擎深审 C/E-A 之后 · G1 三条 P1 落地）有意重钉：`cards_engine.json` → **version 18**
    //   新增 `abilityTriggerPriority` 列（7 条 / 7 张，键 = 原版 `AbilityTrigger` 枚举名）+ `goesSecond` 补齐 6 位督军
    //   ⇒ 卡池 md5 变（`b7b19efd…`/`3f4002d0…` → `a9bba94e…`）。**解析输出逐字节没动**（`check` 解析差异 0 行 ·
    //   ⑦ 输出指纹 `c9f6c642…` 仍一致）⇒ 只重钉这一颗，`PinnedBaselineMd5` / `PinnedParseMd5` 原样不动。
    // 🔴 **2026-10-23（第六十九会话 · `新账-95` / `新账-97 ⑤`）：有意重钉 `PinnedPoolMd5`。**
    //   本轮 `gen_cards_engine.py` 重生成卡表：version 18→19 + 新增 `spellType` 列（`cards_raw.json` 真值直搬），
    //   逐张语义 diff 只有这两处、其余 0 漂移（判据 → `普查产出_1023d/引擎批量_A1690_97_95_执行报告.md` §4）。
    //   基线文件 / 输出指纹**没动** ⇒ 只重钉这一条。
    // 🔴 **2026-10-24（执行代理 `E9` / 第七十三会话同步点）：有意重钉 `PinnedPoolMd5`（`860044b7…`）。**
    //   卡表 v24→v25，新增 logic/op 级 `targetCriteria` 两列（`activeAbilityTargets` 193 行 /
    //   `abilityTargetCriteria` 1616 行）。⚠️ **卡的内容一个没变**：同批 `check` 解析差异 0 行 ·
    //   ⑦ 输出指纹仍 `af836aa0…` · 基线仍 `71cedcd7…` ⇒ 只重钉这一颗。
    //   归因 → `资料/普查产出_1024c/E9_卡表v25_logic级判据列.md`
    // 🔴 **2026-10-25（执行代理 `E2` · 第七十五会话同步点 · 主对话裁定后重钉）：有意重钉 `PinnedPoolMd5`。**
    //   本轮 `gen_cards_engine.py` 重生成卡表：version 25→**26** + 新增 **`abilityLevelCriteria`** 列
    //   （原版 `CardAbility.targetCriteria` `+0x30` 的其余 36 格；**85 行 / 75 张卡**，每张卡都落、没行 = `[]`）。
    //   ⚠️ **卡的内容一个没变**（既有列一个字段都没改）：同批
    //     · `gen_cards_engine.py --check` 幂等一致（两次 `--write` 逐字节相同）；
    //     · `ruleprobe check` 解析差异 **0 行** · ⑦ 输出指纹仍 `af836aa0…`；
    //     · `ruleprobe play check` 主档 1127 张 + 电池 586 张 **逐字节零差异** · 结算指纹仍 `87209677…`。
    //   ⇒ **只重钉这一颗**，`PinnedBaselineMd5` / `PinnedParseMd5` 原样不动。
    //   卡池 md5：`860044b7…` → `af030379…`（2,929,811 → 3,024,683 B，+94,872 B 全部来自新列）。
    //   归因 → `资料/普查产出_1025/E2_ability_criteria管线.md` §二·3 / §四。
    // 🔴 **2026-10-25（执行代理 `E3` · 第七十五会话同步点 · 主对话裁定后重钉）：有意重钉 `PinnedPoolMd5`。**
    //   本轮 `gen_cards_engine.py` 重生成卡表：version 26→**27** + 新增 **`abilityActingCriteria`** 列
    //   （原版 `CardAbility.actingCardCriteria` `+0x28` 的其余 36 格；**83 行 / 80 张卡**，每张卡都落、没行 = `[]`）。
    //   ⚠️ **卡的内容一个没变**（既有列一个字段都没改）：同批
    //     · `gen_cards_engine.py --check` 幂等一致（两次 `--write` 逐字节相同）；
    //     · `ruleprobe check` 解析差异 **0 行** · ⑦ 输出指纹仍 `af836aa0…`；
    //     · `ruleprobe play check` 主档 1127 张 + 电池 586 张 **逐字节零差异** · 结算指纹仍 `87209677…`。
    //   ⇒ **只重钉这一颗**，`PinnedBaselineMd5` / `PinnedParseMd5` 原样不动。
    //   卡池 md5：`af030379…` → `5a17f6af…`（3,024,683 → 3,119,176 B，+94,493 B 全部来自新列）。
    //   归因 → `资料/普查产出_1025/E3_actingCardCriteria.md` §二·3 / §五。
    // 🔴 **2026-10-25（执行代理 `E4` · 第七十五会话同步点 · 主对话裁定后重钉）：有意重钉 `PinnedPoolMd5`。**
    //   本轮 `gen_cards_engine.py` 重生成卡表：version 27→**28** + 新增 **`activeAbilityPickCriteria`** 列
    //   （原版 `ActiveAbility.targetCriteria` `+0x38` 的其余 36 格；**107 行 / 107 张卡**，每张卡都落、没行 = `[]`）。
    //   ⚠️ **卡的内容一个没变**（既有列一个字段都没改）：同批
    //     · `gen_cards_engine.py --check` 幂等一致（两次 `--write` 逐字节相同）；
    //     · `ruleprobe check` 解析差异 **0 行** · ⑦ 输出指纹仍 `af836aa0…`；
    //     · `ruleprobe play check` 主档 1127 张 + 电池 586 张 **逐字节零差异** · 结算指纹仍 `87209677…`。
    //   ⇒ **只重钉这一颗**，`PinnedBaselineMd5` / `PinnedParseMd5` 原样不动。
    //   卡池 md5：`5a17f6af…` → `0397005d…`（3,119,176 → 3,235,327 B，+116,151 B 全部来自新列）。
    //   归因 → `资料/普查产出_1025/E4_trigger目标与activeAbility目标池.md` §二·2 / §四。
    public const string PinnedPoolMd5 = "81d580f3d510678148b3422d21e10ebc";  // 🔴 2026-10-28 重钉（第九十一会话 · 同步点 · 调度台串行执行）：卡表 v37→**v39** —— v38 是 B90 波 2 的 `enchantment` 一列（那一轮漏钉）、v39 是本会话 `A4` 的 `abilitySummon` 一列（`SummonCriteria` 七格，缺键落 `-1`）。`count` 仍 1,129；**解析侧零影响**（`A4` 做了 A/B 隔离：关掉该列重生成 ⇒ 1,129 张只多这一个键、其它差异 0 条）；`PinnedParseMd5` / `PinnedBaselineMd5` 本轮原样不动。上一版 = 519c2629…

    /// <summary>钉住的基线内容指纹（`工具/ruleprobe/基线/out_baseline.txt` 的 md5，**按字节**）。
    /// 与上面那个配对使用 —— 判据见上面那段长注释。</summary>
    // 🔴 2026-10-11（第二十会话 · 写手 `E2`）**有意重钉（第二次）**：旧值 `5b0f961d…`。
    //    重钉理由：`A1462` 给 dump **补了第二目标块** `/{T2:…}`（见 `TargetBlock` / `NestedText` 的头注），
    //    那是**探针输出格式的有意改动**、不是引擎解析变化 —— 全池 **8 行**变化，逐行核过：
    //      · 8 行**全部**是 `forceattack` 一族（`Target2 != null` 的那几张）；
    //      · 判据（自证「只追加」）= **把新行里的 `/{T2:…}` 块删掉之后与旧行【逐字节】相同** ——
    //        实测 8/8 成立（脚本 `re.sub(r'/\{T2:[^;]*', '', 新行) == 旧行`）；
    //      · 其余 **1112 行逐字节没变**（`新增键 0 · 消失键 0`）。
    //    ⚠️ 本轮**同时**把 `PinnedPoolMd5` 也重钉了（同会话里卡池文件被并行写手 `C1` 改过）——
    //      两条一起钉，上面那段判据的「输入没变 ＋ 基线变了」这一对才配得上。
    //    ⛔ **不是**为了抹平红（本轮没有解析差异；红的只有哨兵 ⑥ `A1462`，它**不读基线**）。
    // 🔴 **2026-10-14（第二十七会话）有意重钉** —— 与 `PinnedPoolMd5` 同一批、同一条理由
    //    （卡池 id 整列置换 ⇒ 基线里的第 1 列跟着换，**解析列逐字节没动**）。
    //    重生成只动 id 列：1120 行去第 1 列后 **0 行差异**（判据见上一行那段 ①②③④）。
    // 🔴 **2026-10-14（第二十九会话 · `A1908`）有意重钉** —— 与上面 `PinnedPoolMd5` 同一批、
    //    同一份分类判据（候选值由 `check` 现打：`6c0e41d7…`）。重生成后「解析差异 0 行」。
    // 🔴 **2026-10-18（`K06`）有意重钉** —— 与 `PinnedPoolMd5` 同一批、同一份判据；
    //    `scan` 重生成后基线读数为 **1,122 行**（旧 1,120 —— 那个 +1 是**迁移前就存在**的漂移：
    //    用 HEAD 卡池跑 `check` 同样是「新增 213 / 消失 212」，不是本次引入的）。
    // 🔴 **2026-10-19（`A1973` 收口重钉）** —— 与 `PinnedPoolMd5` 同一批、同一份归因（见那上面那段）；
    //    基线由 `scan` 重生成后现读 md5，逐行照抄。
    // 🔴 **2026-10-20（第五十七会话 · 工具线 `PLAY-1`）有意重钉** —— 与下面 `PinnedParseMd5` 同一批：
    //   那 4 行已逐行核过（`scan` 连跑两次逐字节一致 = 496,525 B / md5 `9d830493…`；
    //   `git diff` 该基线文件 = **4/4 行**，新增 0 / 消失 0）⇒ 按 `check` 的首条判据
    //   「先修好解析器，再跑 `scan` 把它定为新基线」重生成，⛔ 不是把红抹平。
    //   🔴 **2026-10-21（第五十九会话 · 引擎线 `T02`）有意重钉** —— 基线重生成（`scan` 后 1,122 行 / 496,535 B /
    //   md5 `691dd3eb…`），**唯一差异 = `SOR31|Dogmata`**（`EffectCondition.Normalize` 新增
    //   `targetdiesorfriendly` 分支：卡面「Deal 1 damage. If the target dies **or is a friendly unit**, gain 1 ☀.」
    //   的**后半句**原来被静默吃掉）。判据 → `资料/任务线/引擎线/任务总表.md` `T02` 行 ＋ 卡面中文
    //   「如果目标死亡或为友方单位」＋ `cards_1195_2022时代.json` 的 `targetCriteria(friendlyUnits=1/enemyUnits=1)`。
    //   ⇒ 按「先修好解析器，再跑 `scan` 定为新基线」重生成，⛔ 不是把红抹平。
    public const string PinnedBaselineMd5 = "ed2edeabc340c5899940bcda9eda7e66";  // 🔴 2026-10-28 重钉（第八十八会话 · F5 重同步点）：**有意重生成基线** —— 本波三项有意改动（`EC42` 超额伤害接上 · `EC47`/`UM118` 的 `equal to …` 正则），由调度台逐项归因；旧基线（_bak_1028/out_baseline.txt）↔ 新基线 `diff` = **恰好 3 张**（`EC42`/`EC47`/`UM118`，ID 列完全一致、行数 1121 不变）。上一版 = ce3faf91… ｜ 🔴 2026-10-26 重钉（第八十四会话同步点）：**有意重生成基线** —— 解析器真改对了（`EffectText` 新增「名词在前」加法型反向正则，`BL47` 的目标短语不再吞掉 `, and an additional armour 1`）。旧（_bak_1026/out_baseline.txt）↔ 新基线 `diff` = **恰好 1 张卡 `BL47`**（逐字节核对过）。上一版 = 5fb45aec… ｜ 🔴 2026-10-26 重钉（第八十二会话同步点）：基线重生成（Ethereal Supreme 的 `warlord`→`unit`） ｜ 2026-10-24 重钉（Z71 · G10）：GSC75 一行解析变化后重生成基线

    /// <summary>🆕 **解析输出指纹**（`A1382` **一般情形**的堵法，2026-10-12 第二十一会话 · 写手 `E5`）
    /// —— 「`check` 的判据是『与**基线文件**比』，而**基线文件同一个工具就能重写**」这条洞的机器级补丁。
    ///
    /// **判据（每次 `check` 现算，⛔ 一次都不读基线文件）**：把**当次现算的 dump** 规范化
    /// （见 <see cref="CanonParseMd5"/>：**吃掉卡名**、按 `id` 排序）算 md5，与本常量比。
    /// 不等 ⇒ 🔴 出声 ＋ **计入 rc**。参照物是**源码里的常量** ⇒ `scan` 重生成基线**动不了它**。
    ///
    /// **它与哨兵 ⑤ 的分工（⛔ 不是重复 —— ⑤ 那条【已知的假阴】正是它补的）**：
    ///   · ⑤ 比的是「**基线文件** vs 钉住值」，前提是「**卡池 md5** 与钉住的一致」。
    ///     而卡池本仓天天在变（作者是 `工具/gen_cards_engine.py` 那条链）
    ///     ⇒ ⑤ 走「**此时不判**」那一支（**出声但不算失败**）⇒ **洞大开**（这正是 `A1619` 现读的状态）。
    ///   · 本条**既不依赖卡池、也不依赖基线文件** ⇒ **永远在岗**。
    ///     🔴 **实测（2026-10-12，本会话）**：卡池已被别的写手改过（现读 `3ffab02f…` ≠ 钉住值）
    ///       ＋ 拿**当次 live 输出**当基线再 `check`（= 模拟「真回归 ＋ 重生成基线」）
    ///       ⇒ 旧版 **`解析差异 0 行` · 哨兵 0 条失败 · `rc=0` = 全绿**（= `A1382` 当场可复现）；
    ///       加本条之后**同一条命令** ⇒ 🔴 红。判据与逐条读数
    ///       ⇒ `资料/普查产出_第二十一会话/E5_尺子两笔.md`。
    ///   · **另一个区别**：本条把**卡名吃掉**（只留 `id`）⇒ 单纯改名／只重生成一次基线
    ///     ⛔ **不会**让它红（那是 ⑤ 盯的），两条各盯一种，⛔ 别拿一条去替另一条。
    ///
    /// **重钉（⛔ 没有「一条命令自动重钉」—— 那等于把这条洞又开回来）**：
    ///   `check` 会把该抄的**整行常量**打出来，**手改这一个常量** —— 那就是本哨兵要求的
    ///   「**显式确认**」（会出现在 `git diff` 里，⛔ 不是静默动作）。
    ///   🔴 **重钉前必须先确认这批变化是对的**（`RuleEngineTest.Run` 或真机验过）；
    ///   为了「把红抹平」而重钉 = **正是它在抓的那件事**。
    ///
    /// ⚠️ **初始值 = `HEAD` 那一版基线文件的规范化指纹**（`f350e247…` = **上一次被确认的状态**），
    ///   ⛔ **不是**「当次现读的 live」（`38f38061…`）—— 拿 live 当初始值 = **把未经确认的漂移盖成绿**，
    ///   那正是本仓红线「不许静默失败」要治的东西。
    ///   ⚠️ 本会话（第二十一会话）**另有 3 个写手在飞**（改 `RuleEngine/Core/*.cs`）
    ///   ⇒ 本条现在**就是红的**（他们那 8 行漂移还没被确认），**那是设计**，⛔ 不是本工具坏了。
    /// </summary>
    // 🔴 **2026-10-12 第二十三会话：三个指纹【有意重钉】—— 依据是下面四条，⛔ 不是为了把红抹平。
    //   ① 本会话的解析改动（`W1` 的 `A1686` 前缀形 + `F1` 的两条回归修）**已被 `RuleEngineTest.Run` 实跑确认**：
    //      **5246 / 5246 全过**（日志 `_tmp_view/s23_ruleengine2.log`；上一版基线是 5098）。
    //   ② 基线重生成**只动了 1 行**（`SW16|Long Fang`，`partial` 列由空 → 整句；op 块逐字节相同）——
    //      与 `F1` 报告的「全池 scan 逐行 diff = 1 行」**逐字吻合**，⛔ 没有夹带别人的漂移。
    //   ③ 卡池 md5 变了是**别人改的**（`cards_engine.json` 由 `工具/gen_cards_engine.py` 生成，本探针只读）。
    //   ④ ⚠️ **上一次重钉（`A1619`）漏了第三个指纹** —— 这次三个一起钉（否则哨兵 ⑦ 永远红）。
    //   判据与纪律 → `README.md` 的「改探针输出格式三步」那一节；⛔ **重钉只能手改源码**（没有自动口子）。
    // 🔴 **2026-10-14（第二十七会话）有意重钉** —— 与上面 `PinnedPoolMd5` 同一批、同一条理由：
    //   本条把**卡名吃掉、按 id 排序** ⇒ 卡池 id 整列置换之后它必然变号，
    //   而**解析结果本身逐字节没动**（去 id 列 diff = 0 行，判据见 `PinnedPoolMd5` 上面那段 ①②③④）。
    //   确认依据 = 第二十六会话换 id 之后 `RuleEngineTest.Run` **5294 / 5294 全过**。
    // 🔴 **2026-10-14（第二十九会话 · `A1908`）有意重钉** —— 与上面两个同一批、同一份判据；
    //    ⛔ **不是为了把红抹平**：本条现读的那 104 行漂移已逐行分类过（见 `PinnedPoolMd5` 上面那段 ①），
    //    且 `RuleEngineTest.Run` **5386 / 5386 全过**（②）。值由 `check` 的 ⑦ 现打、整行照抄。
    // 🔴 **2026-10-18（`K06`）有意重钉** —— 与上面两个同一批；判据 = `PinnedPoolMd5` 上面那段 ②
    //    （按卡名对齐后 1,118 个名字 0 处差异 ⇒ 只有 id 列变了）。值由 `check` 的 ⑦ 现打、整行照抄。
    // 🔴 **2026-10-19（`A1973` 收口重钉）** —— 与 `PinnedPoolMd5` 同一批、同一份逐条归因
    //    （18 行解析差异：Talent 桥 5 · choicesDefinition 6 · DeadScope 5 · AmountRef 2 · TL16 1，
    //    另 3 张「改名且解析变」已并进上面那几族）。值由 `check` 的 ⑦ 现打、整行照抄。
    // 🔴 **2026-10-20（第五十七会话 · 工具线 `PLAY-1`）有意重钉** —— 解析差异 4 行、逐行核过
    //    （现算 dump vs 旧基线逐行 diff：**只有这 4 行变、新增 0 / 消失 0**）：
    //      · `ASH34` Avatar of Khaine = 本会话 `W57` 的 `Talent:` 修复 —— `create` op 的
    //        `cost 2→0` · `CostShared 1→0` · `ck spirit→空`（`2 [Spirit Stone]` 的 carry 不再挂到
    //        天赋上；天赋由 `RuleCore.SpawnTalents` 回合开始造）；
    //      · `SOR31` / `SOR44` / `Righteous Repugnance` = `FACTION-DATA` 把 Faith 误抄成 Energy 的
    //        数据修正（`gainenergy→gainfaith` · `cost 5/4` 的 `ck energy→faith`）。
    //    判据 → `资料/普查产出_1019/W57_引擎线_T07工具收口.md` §四 ＋ `W57_阵营机制_修女三卡数据.md` §六。
    //   🔴 **2026-10-21（第五十九会话 · 引擎线 `T02`）重钉** —— 唯一变化 = `SOR31|Dogmata` 的条件种类
    //   （`targetdies` → `targetdiesorfriendly`）。行为侧另有三条新断言（友方活着 +1 / 敌方活着 +0 /
    //   敌方被打死 +1）钉住它，见 `Assets/RuleEngine/Editor/RuleEngineTest_T02.cs`。
    public const string PinnedParseMd5 = "136cbb6731e1d6a6ed171462fdb3de00";  // 🔴 2026-10-28 重钉（第八十八会话 · F5 重同步点）：解析输出随基线同步 —— 唯一差异 = `EC42`/`EC47`/`UM118` 三张（调度台已逐项归因：超额伤害接上 · 两条 `equal to …` 正则）。上一版 = c9a64798… ｜ 🔴 2026-10-26 重钉（第八十四会话同步点）：唯一解析差异 = `BL47 Disgustingly Resilient`（`EffectText` 新增「名词在前」加法型反向正则 ⇒ 不再被误判成「重复型」）⇒ 上一版 = 18439d96… ｜ 🔴 2026-10-26 重钉（第八十二会话同步点）：解析输出随基线同步 ｜ 2026-10-24 重钉（Z71 · G10）：`in your hand` 计数从句（全池只 GSC75 一张）；归因 → 资料/普查产出_1024/G10_GSC75解析缺陷.md

    /// <summary>一个文件的 md5（按字节；读不到返回 `null` —— ⛔ 不静默当成空串）。</summary>
    public static string Md5File(string path)
    {
        try { return Md5Bytes(File.ReadAllBytes(path)); }
        catch { return null; }
    }

    static string Md5Bytes(byte[] b)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var h = md5.ComputeHash(b);
            var sb = new System.Text.StringBuilder(h.Length * 2);
            foreach (var x in h) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>🆕 `A1382`：把一份 dump 规范化成「**只含解析结果**」的文本，再算 md5（见
    /// <see cref="PinnedParseMd5"/> 的判据）。规范化三步：
    ///   ① 每行只取 **`id`**（**吃掉卡名** —— 卡名是会变的产物，本仓已改过 6 张）
    ///      ＋ **第 3 列起**（`unparsed|partial|ops`），即 `CheckMain.DefOf` 那一段；
    ///   ② 按 `id` **排序**（`Dictionary` 的枚举序**不可靠**，⛔ 别拿它当判据的一部分）；
    ///   ③ 每行 `id \t 定义 \n` 连接（**含末尾换行**）。
    /// ⚠️ 切法与 `CheckMain.Run` 逐字一致：`Split('|', 5)` 只切 4 刀 —— `ops` 列里**还可能**有 `|`
    ///   （`countRef` 的 `board` 族 / `chooseone` 的 `chooseWhat`）。
    /// ⚠️ 行数不等（卡被加/删）也会改变指纹 ⇒ 红，**那是想要的**（⛔ 不是噪声）。
    /// ⚠️ 编码 = **UTF-8 字节**（照 `Md5Bytes`），⛔ 别用 `Encoding.Default`（本机是 GBK）。</summary>
    public static string CanonParseMd5(string[] lines)
    {
        var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var l in lines)
        {
            if (l.Length == 0) continue;
            var p = l.Split(new[] { '|' }, 5);
            if (p.Length < 5) continue;
            map[p[0]] = p[2] + "|" + p[3] + "|" + p[4];
        }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in map) sb.Append(kv.Key).Append('\t').Append(kv.Value).Append('\n');
        return Md5Bytes(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
    }

    static void Main(string[] args)
    {
        // 🔴 本机默认是 GBK（实测中文输出 `B8 B4 C5 DC`）⇒ 不设这个，所有中文都乱码，
        //    「摘要行一眼判读」这个用途就废了。
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
        OutDir = ResolveOutDir();
        if (args.Length == 0)
        {
            Console.WriteLine("usage: seg <text...> | card <name> | scan <out> | check [baseline] | filter <kw> | "
                              + "layers <out.jsonl> | "
                              + "willrun [dump <out>] | faith [dump <out>] | play <id|卡名> | play all [dump <out>] [pf <out>] | "
                              + "play check [<pf.txt>] | nonkw | jackal | emergency | suppressor | played | b19test | "
                              + "dbparse | ai <卡id|卡名> | traits [<id|卡名>|q=<trait,…>|cond=<n,…>] | "
                              + "tutorial <list|run [S]|g3 [行号]|pred <行号>>");
            Environment.Exit(1);
        }
        if (args[0] == "scan")
        {
            Scan.ScanMain(new[] { args.Length > 1 ? args[1] : "scan.txt" });
            return;
        }
        // 🆕 **`layers <out.jsonl>`**（`A1877①`，2026-10-14 · 写手 `W16`）：把 `scan` 那**两条看不见的层**
        //   （`CardDef.AuraSpecs` 光环 · `keywords`）连同**执行层 ops**（`EffectText.WillRunOps`）
        //   摊成**一行一卡的 JSON** ⇒ `A1759` 那类对账口才判得了 `WhileInPlay` / `WhileInPlayVariable`
        //   与 `bt=3 addTrait` 那几群。形状取舍（为什么不做成 `scan` 多几列）⇒ `Layers.cs` 类注。
        //   ⚠️ 它**不改** `scan`/`check`/基线/三个指纹 ⇒ 别的写手的仪器不受影响。
        //   ⚠️ 列名清单腐烂有机器级守卫（`Layers.AuraColsDrift`）；失败条数计入退出码。
        if (args[0] == "layers")
        {
            Environment.Exit(Layers.Run(args.Length > 1 ? args[1] : "layers.jsonl"));
        }
        // 🆕 **`dbparse`**（`E12`，2026-10-19 · 第五十七会话）：`CardDatabase.Parse` 的**形状 + 耗时**。
        //   为什么需要它：卡表 v12 新增 `animTimes` / `talentNameIds`（+30.8% 字节），
        //   而本探针的 `LoadPool` 走 `System.Text.Json` 手读、**不经过** `Data/CardDatabase.cs`
        //   ⇒ 「JsonUtility 形状吃不吃得下 / 装载慢了没有」没有任何读数。
        //   ⚠️ 它用的是 `Stub.cs` 那份 **Unity 形状的** `JsonUtility`（System.Text.Json + IncludeFields），
        //   **不是** Unity 原生解析器 ⇒ 原生那一半仍需首跑 Unity（判据 → `DbParse.cs` 头注）。
        //   ⛔ 只读卡池；不写基线、不动三个指纹。
        if (args[0] == "dbparse")
        {
            Environment.Exit(DbParse.Run());
        }
        // 🆕 **`ai <卡id|卡名>`**（`R11`，2026-10-25 · 第八十会话）：把 **AI 打分**逐条拆开摆出来
        //   （每一项 criteria 的贡献 · 攻击分逐段分解 · 挨打分/督军分曲线）。
        //   🔴 为什么需要它：`play all dump` 是**结算层**读数，**结构上看不见 AI** ——
        //   实测「只改 AI 打分」的那一版前后 dump **逐字节相同**（`R11` 报告 §四）
        //   ⇒ 在那之前每一笔 AI 改动都只能给「解析式读数」（手算）。
        //   ⛔ 只读：不碰基线、不碰三个指纹、不改 `check`/`play` 的任何输出。
        if (args[0] == "ai")
        {
            // 🆕 `B8_AV`（2026-10-26）：`ai actions <卡id>` —— **动作表**读数口
            //   （`SimpleAI.EnumerateActions` = 原版 `BattleManager.GetAvailableActions` 的等价物）。
            //   🔴 为什么需要它（`B8_E` §五）：`ai` 原来只打 `ScoreAction` 的分解、**看不到候选动作集**；
            //   而 `play all dump` 结构上看不见 AI ⇒ 那 3 条账行没有读数口。
            //   ⚠️ 第一参仍可以是卡 id/卡名（老用法一字不动）—— 只有字面量 `actions` 才切到动作表那一档。
            Environment.Exit(AiProbe.Run(args.Length > 1 ? args[1] : "",
                                         args.Length > 2 ? args[2] : ""));
        }
        // 🆕 **`tutorial <list|run|g3|pred>`**（`G5`，2026-10-26 · 第 5 轮）：**教程局读数口**。
        //   🔴 为什么需要它：`F4_第4轮.md` §1·3 —— 15 条 B 里 **12 条 `AiScripted` 全卡在 G3（可观察量）**，
        //   原话「落点全在 `CardPresentation/**` 或私有谓词上，`ruleprobe` 结构上读不到」。
        //   本模式用**真实 6 关教程数据（462 条动作）**造一局真教程局，把「原版判据转写」与
        //   「我们产品代码的实际返回值」**并排打出来**。
        //   ⛔ 只读：不碰基线、不碰三个指纹、不改 `check`/`play`/`ai` 的任何输出。
        //   ⚠️ 它**没有门禁**（`rc` 恒 0）：读数摆出来，判决归调度台（与 `play`/`ai` 同口径）。
        if (args[0] == "tutorial")
        {
            Environment.Exit(TutorialProbe.Run(args.Length > 1 ? args[1] : "list",
                                               args.Length > 2 ? args[2] : ""));
        }
        // 🆕 **`traits [<id|卡名> | q=<trait,…> | cond=<n,…>]`**（`B8_AH`，2026-10-26）：
        //   **trait 家族（原版 `RawCardScript.CardTraits +0x230`）＋ `keyword(+0x98)`** 的只读读数口。
        //   🔴 为什么需要它：`layers` 只给 `kw`/`kwKeys`（`_keywords` 那一份），**看不到**
        //   `CardDef.CardTraits`（`CountTraits` / `HasDefaultTrait` / `GetDefaultTraitValue` /
        //   `FindDefaultTrait` 的唯一数据源）与 `CardDef.KeywordId`（条件 275/276/277 的判据）；
        //   而 `play` 的夹具单位是现造的 `new CardDef(...)`（不带 keyword）⇒ 结构上测不到真值透传。
        //   `cond=` 那一档直接调**产品代码** `ConditionEval.MeetsCard` ⇒ 候选集可观察。
        //   ⛔ 只读：不碰基线、不碰三个指纹、不改 `check`/`play`/`ai` 的任何输出；`rc` 恒 0（与 `play`/`ai` 同口径）。
        if (args[0] == "traits")
        {
            Environment.Exit(TraitsProbe.Run(args.Length > 1 ? args[1] : ""));
        }
        if (args[0] == "check" && !File.Exists(PoolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + PoolPath); Environment.Exit(2); }
        int i = 0;
        while (i < args.Length)
        {
            switch (args[i])
            {
                case "seg": Dump(args[i + 1]); i += 2; break;
                case "card": Card(args[i + 1]); i += 2; break;
                case "filter": LoadPool(); FilterAdd.Run(Pool, args[i + 1]); i += 2; break;
                case "jackal": LoadPool(); BattleProbe.Jackal(Pool); i += 1; break;
                case "emergency": LoadPool(); BattleProbe.Emergency(Pool); i += 1; break;
                case "suppressor": LoadPool(); BattleProbe.Suppressor(Pool); i += 1; break;
                // 🆕 **`defence`**（2026-10-14 · `A1070` 第 ③ 档）：督军后手那张防御卡 ——
                //    原版三条来路（卡组自带 / 阵营池随机 / 督军 `goSecondCardInHand`）逐条离线真跑。
                //    有不对的 ⇒ rc=1。
                case "defence": LoadPool(); if (BattleProbe.Defence(Pool) > 0) Environment.Exit(1); i += 1; break;
                case "played": LoadPool(); BattleProbe19.Run(Pool); i += 1; break;
                case "b19test": LoadPool(); if (B19Test.Run(Pool) > 0) Environment.Exit(1); i += 1; break;
                // 🆕 **`willrun`**（`A1432`）：量「卡面正文收到没有」，把「空表」拆成
                //    ① 本来没正文 ② 收漏了 ③ 未归因（⛔ 不再让空表混进「全通」）。
                //    `willrun dump <out>` 附带一份逐卡 TSV。⚠️ 退出码可信：有「收漏了」⇒ rc=1。
                case "willrun":
                    {
                        string dumpPath = null;
                        if (args.Length > i + 2 && args[i + 1] == "dump") dumpPath = args[i + 2];
                        Environment.Exit(WillRun.Run(PoolList, PoolRawKeywords, dumpPath));
                    }
                    break;
                case "check": Environment.Exit(CheckMain.Run(args.Length > 1 ? args[1] : null)); break;
                // 🆕 **`nonkw`**（`A1462`，2026-10-11）：**`non <关键词>` 的目标筛选**的专用探针。
                //    这条账的原文是「疑似整个丢了 · ⛔ 未坐实」，而**判不了**的原因有两条，
                //    都在本探针这边（⛔ 不是引擎）：① dump 不打 `Target2`；② `atturn` 的目标在
                //    **嵌套 op** 里。两处都已补上（`TargetBlock` / `NestedText`），
                //    本子命令再把「谁写了 · 什么写法 · 承载在哪个字段 · 真打会怎样」逐条摆出来。
                case "nonkw": Environment.Exit(NonKwProbe.Run(PoolList, PoolRawKeywords)); break;
                // 🆕 **`faith`**（`A1452` / `A1453` / `A1565`）：**信仰付费段的【两把尺子】对账**。
                //    同一个池上「信仰付费段有几条」有两个数（解析层 19 条/17 张 vs 探针④ 11 条/9 张），
                //    而且两边**都是同一份源码跑出来的** ⇒ 单开一条把它们摆到一起、**指名差因**，
                //    并给出**收集层（`CardDef.FaithOps`）的真实覆盖**（那才是「射程」）。
                //    ⚠️ 退出码可信：自检不一致 / 差因解释不了 / 收集层漏 ⇒ rc=1。
                case "faith":
                    {
                        string fDump = null;
                        if (args.Length > i + 2 && args[i + 1] == "dump") fDump = args[i + 2];
                        Environment.Exit(FaithProbe.Run(PoolList, PoolRawKeywords, fDump));
                    }
                    break;
                // 🆕 **`a24`**（引擎线第四批 · 2026-10-24）：`T21④` 回合末发射序 +
                //    `新账-104` `EC58` 双子部署/`them` 的**离线真跑**（见 `A24Probe.cs` 头注）。
                //    ⚠️ 它只验「夹具与期望值」，正式验收仍是 `RuleEngineTest.Run`（Unity）。
                case "a24": LoadPool(); Environment.Exit(A24Probe.Run(Pool)); break;
                // 🆕 **`g2`**（引擎线剩余三件 · 2026-10-24）：`D8` `ThisUnitSummoned` ·
                //    `D9` `WhileInPlay` · `questTimes` 消费点的**离线真跑**（见 `G2Probe.cs` 头注）。
                //    ⚠️ 同 `a24`：它验行为/夹具，正式验收仍是 `RuleEngineTest.Run` / `BattleScene.Run`。
                case "g2": LoadPool(); Environment.Exit(G2Probe.Run(Pool)); break;
                // 🆕 **`n2`**（引擎线 `新账-95` #2 的红灯诊断 · 2026-10-24）：`role=mult` 的**结构化乘数**
                //    在 `RuleEngineTest_UnitRoles.N2RoleMult()` 里两条断言红（实得 3 / 1，期望 2 / 0）。
                //    本子命令把中间每一跳（归位下标 → `TryGetCondition` → `CountTargets` → `repeats`）摆出来。
                //    ⚠️ 同 `a24`/`g2`：读数而已，正式验收仍是 `RuleEngineTest.Run`。
                case "n2": LoadPool(); Environment.Exit(N2Probe.Run(Pool)); break;
                // 🆕 **`play`**（`A1431`，2026-10-11）：**逐卡真打** —— 「这张卡打出去，数字对不对」。
                //    在本子命令之前，本仓 **9 类**检查手段**全部只回答「这句英文今天解析成什么样」**，
                //    没有一条回答「这张卡打出去数字对不对」⇒「**解析过、结算错**」这一整类
                //    （`A1388`/`A1407`/`A1426`/`A1428` 那一族）**任何全池尺子都照不到**。
                //    · `play <id|卡名>` 单卡读数（卡面 + 打前/打后局面指纹 + 变化逐条）；
                //    · `play all [dump <out.tsv>] [pf <out.txt>]` 全池 sweep（每张真打一发，出候选清单
                //      ＋ 🆕 一份**结算指纹**）；`pf` 那份就是 `play check` 要对拍的「被确认的状态」。
                //    · 🆕 **`play check [<pf.txt>]`**（`A1431` 第二笔，2026-10-12 写手 `E5`）：**结算层对拍**
                //      —— 逐卡比 + 「基线文件内容 vs 源码常量 `PinnedPlayMd5`」哨兵（重生成基线抹不掉）。
                //      见 `PlayProbe` 的 `A1431 第二笔` 那一段长注释。
                //    ⚠️ 退出码：单卡 = 崩了 / `ErrUnimplemented` ⇒ rc=1；全池 = 抛异常 > 0 ⇒ rc=1；
                //      🆕 `play check` = **逐卡差异或哨兵红 ⇒ rc=1**。
                //    ⛔ 它**不判对错** —— 判据在卡面上（`desc`/`descZh`/PnP 卡图），它只把实得数字摆出来。
                case "play":
                    {
                        if (args.Length <= i + 1)
                        {
                            Console.Error.WriteLine("!! 用法：play <id|卡名> [preset <spec>] | play all [dump <out.tsv>] [pf <out.txt>] [preset <spec>（另一次扫描，必须给独立输出路径）] | play check [<pf.txt>]");
                            Environment.Exit(2);
                        }
                        string pKey = args[i + 1], pDump = null, pPf = null, pBase = null, pPreset = null;
                        // 🆕 `A1431` 第二笔：`pf <out>`（写结算指纹）· `check [<pf.txt>]`（结算层对拍）。
                        // ⚠️ 原来这一格只认紧跟在 `all` 后面的 `dump`（`args[i+2]`），
                        //    再加一个 option 就会**静默**把它当普通 token 丢掉 ⇒ 改成显式循环、
                        //    认过的 option **必须**有值（⛔ 不许把 `pf` 之后那个路径漏成 null 还接着说「跑完了」）。
                        int j = i + 2;
                        while (j < args.Length)
                        {
                            if (args[j] == "dump" && j + 1 < args.Length) { pDump = args[j + 1]; j += 2; continue; }
                            if (args[j] == "pf" && j + 1 < args.Length) { pPf = args[j + 1]; j += 2; continue; }
                            // 🆕 `A2009`：夹具预置局面（`play <id> preset dead=1:Infantry`）。
                            // 🆕 `P3`（2026-10-26）：`play all … preset <spec>` 也吃这一格 = **另一次全池扫描**
                            //   （B9 全池对拍要按族换夹具），但**必须**给独立输出路径、⛔ 不碰基准那一份。
                            if (args[j] == "preset" && j + 1 < args.Length) { pPreset = args[j + 1]; j += 2; continue; }
                            if ((args[j] == "baseline" || args[j] == "base") && j + 1 < args.Length)
                            { pBase = args[j + 1]; j += 2; continue; }
                            if (pBase == null && j == i + 2 && string.Equals(pKey, "check", StringComparison.OrdinalIgnoreCase))
                            { pBase = args[j]; j += 1; continue; }   // `play check <pf.txt>` 那种写法
                            if (args[j] == "dump" || args[j] == "pf" || args[j] == "preset")
                            {
                                Console.Error.WriteLine("!! `" + args[j] + "` 后面缺路径/写法（用法：play <id> preset dead[=N][:Subtype][,enemyhand=N][,chipenemy=1] · play all [dump <out.tsv>] [pf <out.txt>]）");
                                Environment.Exit(2);
                            }
                            break;   // 不认识的 token：不吞，留给后面的模式
                        }
                        if (!string.IsNullOrEmpty(pPreset)
                            && string.Equals(pKey, "check", StringComparison.OrdinalIgnoreCase))
                        {
                            // ⛔ 静默丢掉最坏（你会以为全池读数是在预置局面上跑的）⇒ 出声并停。
                            Console.Error.WriteLine("!! `preset` **不用于** `play check`（基线是基准夹具跑出来的，"
                                                    + "拿预置局去比一定红）。");
                            Environment.Exit(2);
                        }
                        // 🆕 `P3`（2026-10-26）：`play all … preset <spec>` = **另一次全池扫描**（B9 的全池
                        //   对拍要按族换夹具：`reanimate` 要坟场、`ta=30` 的单位要一个点定的目标）。
                        //   本档**不**写基准文件、也不进 `play check` 的基线 —— 调用方必须给**另一个**输出路径；
                        //   探针自己在摘要那一行会**出声**说明「这是带 preset 的另一次扫描」（见 `Sweep` 的头两行）。
                        if (!string.IsNullOrEmpty(pPreset)
                            && string.Equals(pKey, "all", StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrEmpty(pDump) && string.IsNullOrEmpty(pPf))
                        {
                            Console.Error.WriteLine("!! `play all preset <spec>` 必须同时给**独立的**输出路径"
                                                    + "（`dump <out.tsv>` / `pf <out.txt>`）—— ⛔ 别覆盖基准那一份。");
                            Environment.Exit(2);
                        }
                        Environment.Exit(PlayProbe.Run(PoolList, pKey, pDump, pPf, pBase, pPreset));
                    }
                    break;
                default: Dump(args[i]); i += 1; break;
            }
        }
    }

    /// <summary>从**程序集所在目录**往上找 `MyGame/Assets`（三条腿通用：bin/Debug/net8.0 → ruleprobe → 工具 → Unity）。</summary>
    static string ResolveOutDir()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        while (d != null)
        {
            var p = Path.Combine(d.FullName, "MyGame", "Assets");
            if (Directory.Exists(p)) return p;
            d = d.Parent;
        }
        return @"D:/4/Unity/MyGame/Assets";   // 兜底：找不到就按标准盘符（找不到时 caller 会出声，⛔ 不静默）
    }

    /// <summary>装载卡池并**建卡名索引** —— 探针的**唯一**卡池入口（`Scan` / `Dump` 也都要经过它）。
    /// 🔴 2026-10-10 `A1339`：它原来**不建索引** ⇒ 见下面 `CreatePool.BuildNameIndex` 那段注释。</summary>
    public static void LoadPool()
    {
        if (Pool != null) return;
        if (!File.Exists(PoolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + PoolPath); Environment.Exit(2); }
        // 🔴 `WFPROBE_POOL` 覆盖时**一律出声**（见 `PoolOverride`）—— ⛔ 不许静默换池。
        if (!string.IsNullOrEmpty(PoolOverride))
        {
            Console.WriteLine("⚠️⚠️ 卡池被环境变量 `WFPROBE_POOL` **覆盖** ⇒ " + PoolPath);
            Console.WriteLine("     ⛔ 这**不是**工程里那一份（`MyGame/Assets/RuleEngine/Resources/cards_engine.json`）"
                              + " ⇒ 下面的读数**只能**当「拿这份输入量出来的数」，⛔ 不能当「工程现状」，"
                              + "⛔ 更不能拿它去重钉任何常量。");
        }
        var doc = JsonDocument.Parse(File.ReadAllText(PoolPath));
        var list = new List<CardDef>();
        var rawKw = new List<List<string>>();      // 🆕 与 list **同序**的 `keywords` 原文（见 PoolRawKeywords）
        // 🔴 🆕 **2026-10-14（`A1911`）：卡表后加的那三列也要传进 `CardDef`** ——
        //   `CardDef` 有 `inventoryOptions` / `showInCollectionOptions` / `initiative` 三个**可选参数**
        //   （`Core/CardDef.cs:215-216`，默认全是 `-1` = 「真值里查不到这张卡」），
        //   而这里原来只传到 `goesSecond` 为止 ⇒ **探针侧恒 `-1`**：谁拿探针量
        //   「这张卡能不能进卡组（`DeckRules.CannotAddToDeck`）· 谁先手（`FirstSeatRules.ByInitiative`）」
        //   都会拿到「没数据」——**静默错**（同 `A1339` 那一族：**探针与引擎口径不同**）。
        //   引擎那边的对应调用点 = `RuleEngine/Data/CardDatabase.cs:159-161`（**同一个 JSON、同一批列名**）。
        //   ⚠️ **缺列【不许】当 `0`**：这三列的 `0` 都是**合法取值**
        //     （`CardInventoryOptions.InInventory` / `ShowInCollectionOptions.Hide` / `Initiative.Undefined`）
        //     ⇒ 上面那个 `I()`（缺列返回 `0`）对它们**不能用**：缺列一律 `-1`（= 缺值）**并出声**。
        //     这与 `CardDef` 那三个属性的 doc 口径一致（「`-1` = 真值里查不到」）。
        int poolVersion = doc.RootElement.TryGetProperty("version", out var vEl) && vEl.ValueKind == JsonValueKind.Number
                          ? vEl.GetInt32() : -1;
        int nMissInv = 0, nMissShow = 0, nMissInit = 0;
        // 🆕 **2026-10-26（执行代理 `B8_AV`）：`targeted` 这一列也要传**（`A1911` 同族的第 5 例）。
        //   `CardDef.Targeted` 默认 `-1`，而它唯一的消费者
        //   `EffectText.AssignCardTargetFallback`（`Core/EffectText.cs:1942`）的闸正是 **`Targeted != 1` ⇒ 不接**
        //   ⇒ 探针不传 = **卡级 `cardtarget` 条件在 `play` 这条路上永远挂不上**：
        //   引擎（`Data/CardDatabase.cs:1499`）传了、探针没传 ⇒ **探针与引擎口径不同**，
        //   任何拿 `play` 量「这张卡的卡级目标条件筛没筛掉人」的读数都是**静默错**。
        //   ⚠️ **`-1` 契约（⛔ 不许借 `I()`）**：`targeted = 0`（「不需要选目标」）是**合法真值**
        //      （现读池内 882 张 `0` / 246 张 `1`）⇒ 缺列一律 `-1`（= 缺值）并出声。
        //   🔴 **它会让 `check` / `play all` 的读数变**（`targeted=1` 且卡级条件真的筛人的那几张）
        //      ⇒ 改完必须记新指纹，⛔ **不许自己改 `Pinned*Md5`**（那是调度台重钉窗口的事）。
        int nMissTargeted = 0;
        // 🔴 🆕 **2026-10-19（收口硬前置 · `A1959`/`A1960`）**：`choicesDefinition` 那三列也要传
        //   （接口来源 = `资料/普查产出_1018/W_引擎线_波2b.md` §六·1）。
        //   不传的后果与 `A1911` **一模一样**（同一条链：**探针与引擎口径不同**）：
        //     `CardDef.HasSpecificCardChoice` 恒假 ⇒ `EffectText.Parse(…, card:)` **永远不注入**
        //     `choosecard` ⇒ `scan`/`play` 对 `A1960` 的注入面**恒为「没发生」**
        //     ⇒ 收口重钉基线会把「**引擎有、探针看不见**」钉死。
        //   ⚠️ **缺列一律 `-1`（= 缺值）并出声**，⛔ 不许借 `I()`（它对缺列返回 `0`，
        //     而 `0` 在 `choicesTargetsAffected` 上是**合法真值** = `TargetsAffected.any`）。
        int nMissChoicesTa = 0, nMissChoicesMt = 0, nMissChoicesIds = 0;
        // 🆕 2026-10-21（`T03·c` 批 B 接线）：`triggerPriority` 的读数列（v13）
        int nTpCards = 0, nBadTp = 0;
        // 🆕 2026-10-22（引擎线 `W63` · `T03·c/d`）：`abilityContext` 的读数列（v14）。
        //   `nAcRows` / `nAcCards` 只数**收下来且 `context != 0`** 的行（生成器也只落非 0 行）——
        //   期望对账值 = **157 张 / 170 条**（池内原始非 0 173/157，合并 3 条同 (trigger,context) 重复行后 170；判据 → `资料/普查产出_1022/W63_abilityContext_施工简报.md` §2，2026-10-22 已订正）。
        //   `nBadAc` = 被四道闸丢弃的条数；`nAcOnOldTable` = 「表 version<14 却带着这一列」的卡数
        //   （正常应为 0 —— 非 0 就是在说「探针与引擎会分叉」，见下面读数那一段）。
        int nAcRows = 0, nAcCards = 0, nBadAc = 0, nAcOnOldTable = 0;
        // 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：`unitConditions` 的读数列（v15）。
        //   `nUcRows` / `nUcCards` 只数**收下来**的行；`nBadUc` = 被四道闸丢弃的条数；
        //   `nUcOnOldTable` = 「表 version<15 却带着这一列」的卡数（正常应为 0 —— 非 0 就是在说
        //   「探针与引擎会分叉」，见下面读数那一段）。
        int nUcRows = 0, nUcCards = 0, nBadUc = 0, nUcOnOldTable = 0;
        // 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16` 的明确缺口 · v16）：`count` 行的**被数集合**。
        //   `nUcSetRows` = 收下来的 `countSet` 条数；`nBadUcSet` = 缺 `countSet` / 单格坏值（缺键、
        //   非 0/1 的布尔、`playerTurn` 不在 0/10/20、引用列表不是数组或含空串）。
        //   🔴 口径与 `CardDatabase.MapCountSet` **一致**：坏值**不丢整行**，只把那一格落 `-1`。
        int nUcSetRows = 0, nBadUcSet = 0;
        // 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`mult`/`multi`/`avail`/`choice` 四格
        //   **落了整条 criteria** 的行数（对账期望 = 10：4 + 1 + 3 + 2）。错位也计进 `nBadUcSet`。
        int nUcRoleSetRows = 0;
        // 🆕 v17（2026-10-23 · 引擎线 · `B-16` 残余 241/242 ＋ `B-17`）：`abilityTriggers` /
        //   `abilityTargets` 两列的读数。
        //   · `nMissTrig` / `nMissTgt` = **缺这一列**的卡数（探针按 `null` = **未知**收下，⛔ 不是 `[]`）；
        //   · `nBadTrig` = `abilityTriggers` 里非整数的格子数（⛔ 不猜成 0 —— `0` 是 `ThisCardPlayed`）；
        //   · `nBadTaCell` = `abilityTargets` 的坏格数（`ta` 非数组 / 单格 `< -1`）——
        //     口径同 `CardDatabase.MapAbilityTargets`：**那一格落 -1，⛔ 不丢整条 ability**
        //     （下标即语义 ⇒ 丢一条会让后面每一条 ability 整体错位）。
        int nMissTrig = 0, nMissTgt = 0, nBadTrig = 0, nBadTaCell = 0;
        // 🆕 v20（2026-10-24 · 引擎线 `A12`）：`abilityMultipliers` 的读数列。
        //   `nMissMul` = 缺这一列的卡数（按 `null` = **未知**收下，⛔ 不是 `[]`）；
        //   `nBadFmCell` = `fm` 非数组 / 单格 `< -1`；`nFmGt1` = 取值 > 1 的格子数
        //   （**这些才是真的会被乘的** —— 全 0/1 就等于这条消费端没在干活）。
        int nMissMul = 0, nBadFmCell = 0, nMulCards = 0, nMulLogics = 0, nFmGt1 = 0;
        int nTrigCards = 0, nTrigValues = 0, nTgtCards = 0, nTgtAbilities = 0, nTgtLogics = 0;
        // 🆕 v25（2026-10-24 · 执行代理 `W25`）：`abilityTargetCriteria`（普通 ability，`ab >= 0`）与
        //   `activeAbilityTargets`（主动/替代，`ab = -1 / -2`）两列的读数。
        //   `nTcCards` = 至少有一行的卡数（两列合计）· `nTcRows` = 收下来的行数 · `nBadTc` = 被行级闸丢弃的条数。
        //   对账期望（2026-10-24 现读卡池 v25）= **1008 张卡带 `abilityTargetCriteria`**（`activeAbilityTargets`
        //   只有主动能力卡有）· 行数见装载时打的那一行 —— 判据 → 资料/普查产出_1024c/E9_卡表v25_logic级判据列.md。
        int nTcCards = 0, nTcRows = 0, nBadTc = 0;
        // 🆕 2026-10-27（`B9_E5ENG` · 第五批）：灌进 `ForcedAttackRefund` 的条数（读数口，见下面那段注释）。
        int nRefundRows = 0, nRefundUnknown = 0;
        foreach (var c in doc.RootElement.GetProperty("cards").EnumerateArray())
        {
            string S(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            int I(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            var kws = new List<string>();
            if (c.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Array)
                foreach (var k in kw.EnumerateArray()) if (k.ValueKind == JsonValueKind.String) kws.Add(k.GetString());
            // 三列逐个判「有没有这一格」（⛔ 不借 `I()` —— 它对缺列返回 `0`，而 `0` 在这三列上是**真值**）
            bool hasInv  = c.TryGetProperty("inventoryOptions", out var invEl) && invEl.ValueKind == JsonValueKind.Number;
            bool hasShow = c.TryGetProperty("showInCollectionOptions", out var showEl) && showEl.ValueKind == JsonValueKind.Number;
            bool hasInit = c.TryGetProperty("initiative", out var initEl) && initEl.ValueKind == JsonValueKind.Number;
            if (!hasInv) nMissInv++;
            if (!hasShow) nMissShow++;
            if (!hasInit) nMissInit++;
            // 🆕 `B8_AV`（2026-10-26）：`targeted` —— 同样**逐格判「有没有这一格」**（理由见上面那段）。
            bool hasTgt = c.TryGetProperty("targeted", out var tgtEl) && tgtEl.ValueKind == JsonValueKind.Number;
            if (!hasTgt) nMissTargeted++;
            // 🆕 **2026-10-19**：`choicesDefinition` 三列 —— 同样**逐格判「有没有这一格」**。
            bool hasCTa = c.TryGetProperty("choicesTargetsAffected", out var cTaEl) && cTaEl.ValueKind == JsonValueKind.Number;
            bool hasCMt = c.TryGetProperty("choicesMaxTargets", out var cMtEl) && cMtEl.ValueKind == JsonValueKind.Number;
            bool hasCIds = c.TryGetProperty("choicesSpecificCardIds", out var cIdsEl) && cIdsEl.ValueKind == JsonValueKind.Array;
            var chIds = new List<string>();
            if (hasCIds)
                foreach (var e in cIdsEl.EnumerateArray())
                    if (e.ValueKind == JsonValueKind.String) chIds.Add(e.GetString());
            if (!hasCTa) nMissChoicesTa++;
            if (!hasCMt) nMissChoicesMt++;
            if (!hasCIds) nMissChoicesIds++;
            // 🆕 2026-10-21（引擎线 `T03·c` 批 B 接线）：**v13 的 `triggerPriority` 也要传**。
            //   不传的后果与 `A1911` / `A1959` 逐字同族：**探针与引擎口径不同** ——
            //   引擎侧 `Data/CardDatabase.cs` 从 v13 起把这一列传进 `CardDef`（`MapTriggerPriority`），
            //   探针不传 ⇒ `CardDef.TriggerPriority()` **恒 1** ⇒ 四道队列在探针里**永远走 Instant**，
            //   任何靠探针量「priority 压过 PlayOrder」的读数都会被静默钉成错基线。
            //   ⚠️ 缺列 / 形状不对**一律出声**（⛔ 不静默当成默认值）—— 本列的口径是「缺键 = 1」，
            //     所以「整列没有」与「全池都是默认值」在数据上分不开，必须靠 `version` 那道闸分辨。
            bool hasTp = c.TryGetProperty("triggerPriority", out var tpEl) && tpEl.ValueKind == JsonValueKind.Array;
            var trigPrio = new List<KeyValuePair<string, int>>();
            if (hasTp)
                foreach (var e in tpEl.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object) { nBadTp++; continue; }
                    string tpk = e.TryGetProperty("keyword", out var ke) && ke.ValueKind == JsonValueKind.String
                                 ? ke.GetString() : null;
                    bool hasPv = e.TryGetProperty("priority", out var pvEl) && pvEl.ValueKind == JsonValueKind.Number;
                    if (string.IsNullOrEmpty(tpk) || !hasPv) { nBadTp++; continue; }
                    // 🔴🆕 2026-10-21（审查 `W61b` 第 7 条）：**越界值也算「形状坏」并丢弃** ——
                    //   引擎侧 `CardDef` 构造函数对不在 0..3 的值同样 LogError + 丢弃；
                    //   探针这里再收进来的话，`TriggerLaneOfCard` 会拿到一个非法道号、
                    //   由 `ActionQueue.Enqueue` 出声丢弃 —— 两处报的是同一件事，但**计数**口径不同。
                    //   ⇒ 统一在「读数」这一层就按坏行处理（探针不再往下传）。
                    int pv = pvEl.GetInt32();
                    if (pv < 0 || pv > 3) { nBadTp++; continue; }
                    trigPrio.Add(new KeyValuePair<string, int>(tpk, pv));
                }
            if (trigPrio.Count > 0) nTpCards++;
            // 🆕 2026-10-22（引擎线 `W63` · `T03·c/d`）：**v14 的 `abilityContext` 也要传** ——
            //   不传的后果与 `A1911` / `A1959` / v13 的 `triggerPriority` **逐字同族**：
            //   **探针与引擎口径不同**。引擎侧 `Data/CardDatabase.cs:597` 经 `MapAbilityContext`
            //   把这一列传进 `CardDef`；探针不传 ⇒ `CardDef.AbilityContextOfTrigger/OfKeyword`
            //   在探针里**恒 -1**，任何靠探针量「这条触发要不要判载体区域」的读数都被静默钉成旧口径。
            //
            //   🔴 **`-1` 契约（⛔ 绝不退回 `0`）**：`context = 0` 是**合法值** = `Play`
            //     （载体必须**活着在场**）。缺键若读成 `0`，**死亡族 / 墓地族**触发会被静默掐死
            //     ⇒ 缺键一律 `-1`（= 未知），消费端按「未知」**回退旧口径**
            //     （`QueuedAction.RequireActorInPlay` 那一套），⛔ 不许在这里替它挑一档。
            //   🔴 **skip 表**（只是读数的背景，本函数**不做**消费侧判断）：原版
            //     `CardAbility__CanTriggerAbility.c:69-95` 对 trigger ∈ {0 ThisCardPlayed ·
            //     80 ThisUnitDeath · 145 Codex · 220 Sacrifice · 500 TriggerEnchantment} 整段
            //     **跳过** context 判定；600 UseSpiritStone / 605 UseFaith 在余额判定前显式检查；
            //     其余走 `:126` 兜底。⇒ 数据**全量照收**（含这五族），skip 由消费端按
            //     `CardDef.AbilityContextOfTrigger/OfKeyword` 自己判（⛔ 别在这里替它丢行）。
            //   判据 → `资料/普查产出_1022/W63_abilityContext_施工简报.md` §1/§3。
            bool hasAc = c.TryGetProperty("abilityContext", out var acEl) && acEl.ValueKind == JsonValueKind.Array;
            var ablCtx = new List<CardDef.AbilityContextSpec>();
            if (hasAc && poolVersion < 14)
            {
                // 🔴 `version < 14` 的表**整列不采**（= 全 `-1` 未知，见下面那声 `⚠️`）。
                //   但这一格必须**单独计数**：真出现「旧表却带着新列」时 ⛔ 不许静默丢掉 ——
                //   引擎侧 `CardDatabase.Parse` 只按 `version` 出声、列**照样映射** ⇒ 两边会分叉，得看得见。
                nAcOnOldTable++;
            }
            else if (hasAc)
            {
                HashSet<string> acNames = null, acKws = null;
                foreach (var e in acEl.EnumerateArray())
                {
                    // ↓ 四道闸与 `CardDatabase.MapAbilityContext`（`Data/CardDatabase.cs:739-814`）
                    //   **同口径**：全部「计数 + 丢弃该条」，⛔ 不静默、⛔ 不默认。
                    if (e.ValueKind != JsonValueKind.Object) { nBadAc++; continue; }
                    // ① 缺 `name`（`AbilityTrigger` 枚举名是唯一按键；关键词只覆盖 53/173 行）。
                    string acName = e.TryGetProperty("name", out var anEl) && anEl.ValueKind == JsonValueKind.String
                                    ? anEl.GetString() : null;
                    if (string.IsNullOrEmpty(acName)) { nBadAc++; continue; }
                    // ② 缺 `trigger` 键（⛔ 不许落成 `0` = `ThisCardPlayed` —— `0` 是**合法 trigger**）。
                    bool hasTrig = e.TryGetProperty("trigger", out var atEl) && atEl.ValueKind == JsonValueKind.Number;
                    if (!hasTrig) { nBadAc++; continue; }
                    // ③ `context` 缺键（`-1` 哨兵）或不在 {0,10,20,30,40,50}（⛔ `0` 是合法的 `Play`，不拒）。
                    bool hasCtx = e.TryGetProperty("context", out var acvEl) && acvEl.ValueKind == JsonValueKind.Number;
                    if (!hasCtx) { nBadAc++; continue; }
                    int acv = acvEl.GetInt32();
                    if (!IsLegalAbilityContextProbe(acv)) { nBadAc++; continue; }
                    // ④ 同 `name` / 同 `keyword` 重复 ⇒ 丢弃**后出现**的那条（先到的保留）。
                    //    ⚠️ 顺序照引擎：`name` **先**入表再看 `keyword` —— 所以「keyword 重复被丢」的那条，
                    //    它的 `name` 已经占位（后面的同名行照样算重复）。别改这个顺序（一改就与引擎分叉）。
                    if (acNames == null) acNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!acNames.Add(acName)) { nBadAc++; continue; }
                    string acKw = e.TryGetProperty("keyword", out var akEl) && akEl.ValueKind == JsonValueKind.String
                                  ? akEl.GetString() : null;
                    if (!string.IsNullOrEmpty(acKw))
                    {
                        if (acKws == null) acKws = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (!acKws.Add(acKw)) { nBadAc++; continue; }
                    }
                    ablCtx.Add(new CardDef.AbilityContextSpec
                    {
                        Trigger = atEl.GetInt32(),
                        Name = acName,
                        // 引擎侧同样把空串归一成 `null`（`MapAbilityContext:809`）
                        Keyword = string.IsNullOrEmpty(acKw) ? null : acKw,
                        Context = acv,
                    });
                    if (acv != 0) nAcRows++;      // 只数**非 0**（生成器也只落非 0 行）
                }
                if (ablCtx.Any(x => x.Context != 0)) nAcCards++;
            }
            // 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：**v15 的 `unitConditions` 也要传**。
            //   不传的后果与 `A1911` / `A1959` / v13 `triggerPriority` / v14 `abilityContext` **逐字同族**：
            //   **探针与引擎口径不同**。引擎侧 `Data/CardDatabase.cs` 经 `MapUnitConditions` 把这一列
            //   传进 `CardDef`；探针不传 ⇒ `CardDef.ConditionOf` 在探针里**恒 -1（未知）**，
            //   任何靠探针量「这一格有没有条件 / 条件是什么」的读数都被静默钉成旧口径。
            //
            //   🔴 **`-1` 契约（⛔ 绝不退回 `0`）**：`cond = 0` 是 `UnitConditions.none` = **没有条件**；
            //     `0` 也是 `ab` / `lg` 的**合法位置** ⇒ 缺键一律 `-99`（`MissingIndex`）并出声丢弃。
            //   🔴 **四道闸与 `CardDatabase.MapUnitConditions` 同口径**（role / ab·lg 缺键 /
            //     cond 缺键或非法 / 同 key 重复），全部「计数 + 丢弃该条」，⛔ 不静默、⛔ 不默认。
            //     🆕 2026-10-24（引擎线 `G6`）：`mult`/`multi`/`avail`/`choice` 四格的 `cond = 0` **不算坏数据**
            //     （见下面 ③ 的注释），其余 role 的 `cond = 0` 照旧丢弃。
            bool hasUc = c.TryGetProperty("unitConditions", out var ucEl) && ucEl.ValueKind == JsonValueKind.Array;
            var unitConds = new List<CardDef.ConditionSpec>();
            if (hasUc && poolVersion < 15)
            {
                // 🔴 `version < 15` 的表**整列不采**（= 全 `-1` 未知，见下面那声 `⚠️`）。
                //   但「旧表却带着新列」必须**单独计数**：引擎侧 `CardDatabase.Parse` 只按 `version`
                //   出声、列**照样映射** ⇒ 两边会分叉，得看得见。
                nUcOnOldTable++;
            }
            else if (hasUc)
            {
                HashSet<string> ucKeys = null;
                foreach (var e in ucEl.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object) { nBadUc++; continue; }
                    // ① `role` 必须是那十个之一。
                    string ucRole = e.TryGetProperty("role", out var urEl) && urEl.ValueKind == JsonValueKind.String
                                    ? urEl.GetString() : null;
                    if (!CardDef.IsLegalConditionRole(ucRole)) { nBadUc++; continue; }
                    // ② `ab` / `lg` 缺键（`-99` 哨兵；`0` 是合法位置，⛔ 不能拿缺省 0 顶）。
                    bool hasAb = e.TryGetProperty("ab", out var ucAbEl) && ucAbEl.ValueKind == JsonValueKind.Number;
                    bool hasLg = e.TryGetProperty("lg", out var ucLgEl) && ucLgEl.ValueKind == JsonValueKind.Number;
                    if (!hasAb || !hasLg) { nBadUc++; continue; }
                    int ucAb = ucAbEl.GetInt32(), ucLg = ucLgEl.GetInt32();
                    if (ucAb == CardDef.ConditionSpec.MissingIndex
                        || ucLg == CardDef.ConditionSpec.MissingIndex) { nBadUc++; continue; }
                    // ③ `cond` 缺键 / none(0) / 不在枚举里。
                    //    🔴 **2026-10-24（引擎线 `G6` · `C6` §1·3 A）：`mult`/`multi`/`avail`/`choice` 四格放行 `cond = 0`。**
                    //    口径同引擎 `Data/CardDatabase.cs` 的 `IsUnconditionalCriteriaRole`：那四格是
                    //    「**整条 criteria**」行（`multiplierSet` / `multiSet` / `availSet` / `choiceSet`），
                    //    `conditionsFilter == 0` 只是其中一个字段；其余 role 的「缺行 = 没有条件」被
                    //    `CardDef.ConditionOf` 依赖 ⇒ 照旧丢弃。
                    //    ⛔ 改前这里对**所有** role 一律丢 ⇒ 探针看不见 `G6` 新增的 131 行（`GOF4` 的结构化
                    //    `mult` 那条腿因此验不到）。⚠️ 这一行与引擎那份是**同一条规则的第二处** ——
                    //    引擎那格的 role 集合变了，这里要跟着改。
                    //    🔴 **2026-10-26（批次 5 · `G3`）加第五个 `count`**：第三道闸
                    //    `CardAbility.boardCriteria`（`+0x38`）的数据就挂在这行上（`countSet` / `opt` /
                    //    `op` / `refValue`），判据 = `BoardAnalysis__MeetsCriteria.c:150-152`；引擎那份已同步
                    //    （`CardDatabase.IsUnconditionalCriteriaRole`）。⛔ 不加这一档 ⇒ 池内 93 条会在这里
                    //    被当坏数据丢掉、每次跑都印一条**假的**「93 条被丢弃」。
                    bool hasCond = e.TryGetProperty("cond", out var ucCvEl) && ucCvEl.ValueKind == JsonValueKind.Number;
                    if (!hasCond) { nBadUc++; continue; }
                    int ucCond = ucCvEl.GetInt32();
                    if (!CardDef.IsLegalUnitCondition(ucCond)) { nBadUc++; continue; }
                    if (ucCond == CardDef.ConditionNone
                        && ucRole != "mult" && ucRole != "multi" && ucRole != "avail" && ucRole != "choice"
                        && ucRole != "count")
                    { nBadUc++; continue; }
                    // ④ 同 `(ab, lg, role)` 重复 ⇒ 丢弃后出现的那条（先到的保留）。
                    string ucKey = ucAb + ":" + ucLg + ":" + ucRole;
                    if (ucKeys == null) ucKeys = new HashSet<string>(StringComparer.Ordinal);
                    if (!ucKeys.Add(ucKey)) { nBadUc++; continue; }
                    int ucEx = e.TryGetProperty("ex", out var ucExEl) && ucExEl.ValueKind == JsonValueKind.Number
                               ? ucExEl.GetInt32() : 0;
                    // 🆕 v16（`B-16` 缺口）：`count` 行的**被数集合**。
                    //   🔴 口径同 `CardDatabase.MapCountSet`：坏值**不丢整行**（丢行 = 「没有条件」=
                    //   静默放行），只把那一格落 `-1`；`countSet` 整个缺 ⇒ `CountSet = null`（未知）。
                    CardDef.CountSetSpec ucSet = null;
                    if (ucRole == "count")
                    {
                        if (e.TryGetProperty("countSet", out var ucSetEl))
                        {
                            ucSet = ParseCountSet(ucSetEl, out int ucSetBad);
                            nBadUcSet += ucSetBad;
                            if (ucSet != null) nUcSetRows++;
                        }
                        else nBadUcSet++;          // `count` 行缺 `countSet`（v16+ 表上是坏数据）
                    }
                    else if (e.TryGetProperty("countSet", out _)) nBadUcSet++;   // 非 count 行带它 = 坏数据
                    // 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`mult` / `multi` / `avail` / `choice`
                    //   四格的**整条 criteria**。口径同 `CardDatabase.RoleSet`：
                    //   · 缺这一格 ⇒ 对应属性 `null`（**未知**，⛔ 不是「默认 criteria」）**且不出声**；
                    //   · **错位**（别的 role 带着它）⇒ 计一条 bad 并忽略（⛔ 不静默挂到别的 role 上）。
                    //   ⚠️ 探针不传 = 「引擎有、探针看不见」⇒ 结构化乘数在 `play` 里恒不生效，
                    //      而 `play` 正是验收工具 ⇒ 这一格是**必须**传的（同 `A1911`/`A1959` 那一族）。
                    CardDef.CountSetSpec ucMultSet  = ParseRoleSet(e, ucRole, "mult",   "multiplierSet", ref nBadUcSet);
                    CardDef.CountSetSpec ucMultiSet = ParseRoleSet(e, ucRole, "multi",  "multiSet",      ref nBadUcSet);
                    CardDef.CountSetSpec ucAvailSet = ParseRoleSet(e, ucRole, "avail",  "availSet",      ref nBadUcSet);
                    CardDef.CountSetSpec ucChoSet   = ParseRoleSet(e, ucRole, "choice", "choiceSet",     ref nBadUcSet);
                    if (ucMultSet != null) nUcRoleSetRows++;
                    if (ucMultiSet != null) nUcRoleSetRows++;
                    if (ucAvailSet != null) nUcRoleSetRows++;
                    if (ucChoSet != null) nUcRoleSetRows++;
                    unitConds.Add(new CardDef.ConditionSpec
                    {
                        Ability = ucAb,
                        Logic = ucLg,
                        Role = ucRole,
                        Condition = ucCond,
                        Name = e.TryGetProperty("name", out var ucNmEl) && ucNmEl.ValueKind == JsonValueKind.String
                               ? ucNmEl.GetString() : null,
                        Exclude = ucEx != 0,
                        CountOption = e.TryGetProperty("opt", out var ucOptEl) && ucOptEl.ValueKind == JsonValueKind.Number
                                      ? ucOptEl.GetInt32() : -1,
                        StatOption = e.TryGetProperty("op", out var ucOpEl) && ucOpEl.ValueKind == JsonValueKind.Number
                                     ? ucOpEl.GetInt32() : -1,
                        ReferenceValue = e.TryGetProperty("refValue", out var ucRefEl) && ucRefEl.ValueKind == JsonValueKind.Number
                                         ? ucRefEl.GetInt32() : -1,
                        CountSet = ucSet,
                        // 🆕 v22（2026-10-24 · `新账-95` #2）：`mult`/`multi`/`avail`/`choice` 四格。
                        MultiplierSet = ucMultSet,
                        MultiSet = ucMultiSet,
                        AvailSet = ucAvailSet,
                        ChoiceSet = ucChoSet,
                    });
                    nUcRows++;
                }
                if (unitConds.Count > 0) nUcCards++;
            }
            // 🆕 v17（2026-10-23 · 引擎线 · `B-16` 残余 241/242）：**`abilityTriggers` 也要传** ——
            //   不传的后果与 `A1911` / `A1959` / v13 `triggerPriority` / v14 `abilityContext` **逐字同族**：
            //   **探针与引擎口径不同**。引擎侧 `Data/CardDatabase.cs`（`abilityTriggers: c.abilityTriggers`）
            //   从 v17 起把这一列传进 `CardDef`；探针不传 ⇒ `CardDef.AbilityTriggerPresence` 恒 `-1`
            //   ⇒ `ConditionEval` 的 `241 hasUseSpiritStone` / `242 hasUseFaith` **在探针里永远判不了**。
            //
            //   🔴 **缺列 ≠ 空数组**（`-1` 契约）：缺列 = 真值快照里没有这张卡 = **未知**（访问器 -1）；
            //     空数组 = **明确没有任何 ability**（`HasAbilityType` ⇒ 假）。
            //     ⛔ 缺列**不许**落成 `[]`（那会把「不知道」静默变成「没有」）。
            //     判据 = `RawCardScript__HasAbilityType.c:16-38`（遍历 `cardAbilities`、只看 `trigger`）。
            //   ⚠️ 局部名加 `a17` 前缀：`hasTrig` / `e` 这些名字在**同一个卡循环的子作用域里**
            //      （`abilityContext` 那一段）已经用过 ⇒ 直接叫 `hasTrig` 会撞 `CS0136`。
            bool a17HasTrig = c.TryGetProperty("abilityTriggers", out var a17TrigEl) && a17TrigEl.ValueKind == JsonValueKind.Array;
            var a17TrigVals = new List<int>();
            if (a17HasTrig)
                foreach (var a17Te in a17TrigEl.EnumerateArray())
                {
                    // 形状坏（非整数）⇒ 跳过并计数（⛔ 不猜成 0 —— `0` = `ThisCardPlayed` 是一个**具体**触发，
                    //   同生成器 `ability_triggers_of` 的 `report["bad"]` 口径）。
                    if (a17Te.ValueKind != JsonValueKind.Number) { nBadTrig++; continue; }
                    a17TrigVals.Add(a17Te.GetInt32());
                }
            else nMissTrig++;
            if (a17HasTrig) { nTrigCards++; nTrigValues += a17TrigVals.Count; }
            // 🆕 v17（2026-10-23 · 引擎线 · 战斗审计 `B-17`）：**`abilityTargets` 也要传** ——
            //   不传 ⇒ `CardDef.AbilityTargets` 恒空 ⇒ `EffectText.AssignAbilityTargets` 归不了位
            //   ⇒ `EffectOp.SubjectTargetsAffected` 恒 `-1` ⇒ `EffectResolver` 的「无施放者、也没点目标」
            //   那一支**永远走旧口径（己方全体）** ⇒ 拿 `play` 量 `B-17` 的读数全是**假阴性**。
            //
            //   🔴 **下标即语义**（`[i]` = `cardAbilities[i]`）⇒ 坏行/坏格**不丢、不压缩**：
            //     非对象行 → `null` 占位（`CardDef.AbilityTargets` 的 doc 明写允许 null）；
            //     `ta` 不是数组 → 空表；单格 `< -1` → 落 `-1` 并计数。
            //     这是 `CardDatabase.MapAbilityTargets` 的**逐条口径**（那里也是出声 + 落 -1、⛔ 不丢整条）。
            //   ⚠️ `0` 是合法的 `any`，⛔ 别拿它当缺值。
            bool a17HasTgt = c.TryGetProperty("abilityTargets", out var a17TgtEl) && a17TgtEl.ValueKind == JsonValueKind.Array;
            var a17TgtRows = new List<CardDef.AbilityTargetSpec>();
            if (a17HasTgt)
                foreach (var a17Row in a17TgtEl.EnumerateArray())
                {
                    if (a17Row.ValueKind != JsonValueKind.Object) { a17TgtRows.Add(null); continue; }   // 下标即语义 ⇒ 占位、不错位
                    int a17Trigger = a17Row.TryGetProperty("trigger", out var a17TgEl) && a17TgEl.ValueKind == JsonValueKind.Number
                                     ? a17TgEl.GetInt32() : -1;   // trigger 只作可读性；坏值 -1（同 `AbilityTargetDto.trigger` 的默认）
                    var a17Ta = new List<int>();
                    if (a17Row.TryGetProperty("ta", out var a17TaEl) && a17TaEl.ValueKind == JsonValueKind.Array)
                        foreach (var a17Cell in a17TaEl.EnumerateArray())
                        {
                            if (a17Cell.ValueKind != JsonValueKind.Number) { nBadTaCell++; a17Ta.Add(-1); continue; }
                            int a17Tv = a17Cell.GetInt32();
                            if (a17Tv < -1) { nBadTaCell++; a17Tv = -1; }   // 只允许 `>= 0` 或 `-1`（同 `MapAbilityTargets`）
                            a17Ta.Add(a17Tv);
                        }
                    else nBadTaCell++;   // `ta` 非数组（`MapAbilityTargets` 按空表处理）
                    a17TgtRows.Add(new CardDef.AbilityTargetSpec { Trigger = a17Trigger, TargetsAffected = a17Ta.ToArray() });
                    nTgtAbilities++;
                    nTgtLogics += a17Ta.Count;
                }
            else nMissTgt++;
            if (a17HasTgt) nTgtCards++;
            // 🆕 v20（2026-10-24 · 引擎线 `A12`）：**`abilityMultipliers` 也要传** ——
            //   不传 ⇒ `CardDef.AbilityTargetSpec.FurtherMultiplier` 恒 `null` ⇒
            //   `EffectText.AssignAbilityTargets` 挂不上 `EffectOp.AbilityFurtherMultiplier`
            //   ⇒ `EffectResolver` 那一步**永远不乘** ⇒ 拿 `play` 量这一列全是**假阴性**
            //   （与 `A1911` / `A1959` / `A17` 那条「探针与引擎口径不同」是同一族坑）。
            //   🔴 **下标即语义**（与 `abilityTargets` 逐条同行）：非对象行 → `null` 占位；
            //      `fm` 不是数组 → `null`（= 这一条 ability 读不到，⛔ 不是空表）；
            //      单格 `< -1` → 落 `-1` 并计数（口径同 `CardDatabase.MapAbilityMultipliers`）。
            //   ⚠️ `0` 是合法取值（原版「不乘」），⛔ 别拿它当缺值。
            bool a20HasFm = c.TryGetProperty("abilityMultipliers", out var a20FmEl) && a20FmEl.ValueKind == JsonValueKind.Array;
            var a20FmRows = new List<int[]>();
            if (a20HasFm)
                foreach (var a20Row in a20FmEl.EnumerateArray())
                {
                    if (a20Row.ValueKind != JsonValueKind.Object) { a20FmRows.Add(null); continue; }
                    if (!a20Row.TryGetProperty("fm", out var a20Cell) || a20Cell.ValueKind != JsonValueKind.Array)
                    { a20FmRows.Add(null); nBadFmCell++; continue; }
                    var a20Fm = new List<int>();
                    foreach (var a20v in a20Cell.EnumerateArray())
                    {
                        if (a20v.ValueKind != JsonValueKind.Number) { nBadFmCell++; a20Fm.Add(-1); continue; }
                        int a20n = a20v.GetInt32();
                        if (a20n < -1) { nBadFmCell++; a20n = -1; }   // 只允许 `>= 0` 或 `-1`
                        if (a20n > 1) nFmGt1++;
                        a20Fm.Add(a20n);
                    }
                    a20FmRows.Add(a20Fm.ToArray());
                    nMulLogics += a20Fm.Count;
                }
            else nMissMul++;
            if (a20HasFm) nMulCards++;
            // 🆕 **2026-10-24（引擎线 `G2` · `C4` D8/D9）：`animTimes`（v11）也要传进 `CardDef`。**
            //   为何现在补：本探针的 `LoadPool` 是**手读 JSON**、不走 `Data/CardDatabase.cs`
            //   ⇒ 在这一行之前 `CardDef.AnimTimes` **恒空**，而引擎那边（`CardDatabase.Parse` →
            //   `MapAnimTimes`，`Data/CardDatabase.cs:1047`）**是传的**。
            //   后果（与 `A1911` / `A1959` / `A17`「探针与引擎口径不同」是同一族坑）：
            //   `CardDef.HasAnimTrigger(...)` 在探针里**恒假** ⇒ `D9` 的 `WhileInPlay` 发射口
            //   （按 `HasAnimTrigger` 收窄）在探针里**永远看不见**，读数会是**假阴性**。
            //   ⚠️ 只搬值、不排序不做取舍（口径与 `CardDatabase.MapAnimTimes` 逐字相同）；
            //      缺列 ⇒ 空表（= 这张卡没有 `cardAnimations`，⛔ 不是「解析失败」）。
            bool hasAnim = c.TryGetProperty("animTimes", out var animEl) && animEl.ValueKind == JsonValueKind.Array;
            var animRows = new List<CardDef.CardAnimTime>();
            if (hasAnim)
                foreach (var ar in animEl.EnumerateArray())
                {
                    if (ar.ValueKind != JsonValueKind.Object) continue;
                    string AS(string k) => ar.TryGetProperty(k, out var av) && av.ValueKind == JsonValueKind.String ? av.GetString() : null;
                    int AI(string k) => ar.TryGetProperty(k, out var av) && av.ValueKind == JsonValueKind.Number ? av.GetInt32() : 0;
                    float AF(string k) => ar.TryGetProperty(k, out var av) && av.ValueKind == JsonValueKind.Number ? (float)av.GetDouble() : 0f;
                    animRows.Add(new CardDef.CardAnimTime
                    {
                        Anim = AS("a"), Prefab = AS("n"),
                        Trigger = AI("tg"), TriggerName = AS("tn"),
                        RandomChoice = AI("rc") != 0, Order = AS("o"),
                        Delay = AF("d"), StartDelay = AF("sd"),
                        TimeAtStartPos = AF("s"), TimeMoving = AF("m"),
                        TimeAtEndPos = AF("e"), Duration = AF("u"), Offset = AF("t"),
                    });
                }
            // 🆕 v25（2026-10-24 · 执行代理 `W25`）：**`abilityTargetCriteria` / `activeAbilityTargets`
            //   两列也要传** —— 不传的后果与 `A1911` / `A1959` / v13 `triggerPriority` /
            //   v14 `abilityContext` **逐字同族**：**探针与引擎口径不同**。
            //   引擎侧 `Data/CardDatabase.cs:1343-1346` 经 `MapTargetCriteriaRows` 把这两列传进 `CardDef`；
            //   探针不传 ⇒ `CardDef.AbilityTargetCriteriaVersioned == false` ⇒
            //   `EffectText.AttachTargetCriteria` **直接 return** ⇒ 探针跑 `play` / `check` 时
            //   **这 36 格筛根本不生效**（实测：`play "Exemplary Warrior"` 治疗 1 个目标，
            //   而 Unity 里是 **0**）⇒ 任何拿探针量这条功能的读数都是**假阴性**。
            //   口径与引擎**逐字相同**（行级四道闸 / `ab` 的 `-1`·`-2` 合法 / 单格坏值落 `-1` 不丢行）。
            var atcRows = ParseTargetCriteriaRows(c, "abilityTargetCriteria", active: false, ref nBadTc);
            var aatRows = ParseTargetCriteriaRows(c, "activeAbilityTargets", active: true, ref nBadTc);
            if (atcRows != null && atcRows.Count > 0) nTcCards++;
            if (aatRows != null && aatRows.Count > 0) nTcCards++;
            nTcRows += (atcRows == null ? 0 : atcRows.Count) + (aatRows == null ? 0 : aatRows.Count);
            // 🆕 **2026-10-27（`B9_E5ENG` · 第五批）：把 `chosenAbility` 灌进 `ForcedAttackRefund`。**
            //   🔴 为什么要这一笔：`Data/ForcedAttackRefundMap.cs`（那半个 partial，走 `Resources.Load`）
            //   **不在本 csproj 的编译集** ⇒ `LoadFromUnity` 是空声明、调用点被抹掉 ⇒ 表恒 `null`
            //   ⇒ `ForcedAttackRefund.Of()` 恒 `Unknown` ⇒ 调用点按设计**一律退还**
            //   ⇒ 235/236 那两族（原版**不退还**）在探针里被错退、终局 `AttacksThisTurn` 必然回 0。
            //   实测证据（补这一笔之前）：`play GOF93`（`chosenAbility=235`）日志出现
            //   「Hunta Rig 重置行动：本回合攻击次数 1 → 0」—— 真值里 235 不退。
            //   ⇒ 这与 `A1911` / `A1959` / v13 `triggerPriority` / v14 `abilityContext` **同一族**
            //     （**探针与引擎口径不同** ⇒ 靠探针量出来的读数被静默钉成错口径）。
            //   🔴 判定规则**不在这里抄第二份**：只调 `Core/ForcedAttackRefund.Install`
            //     （`{231,237,238}` 退 / `{230,233,235,236}` 不退 / 都没有 ⇒ 不落键保持 `Unknown`）。
            //   🔴 数据来源 = 本函数**已经在读**的同一个 `cards_engine.json` ⇒ 不多读一次文件。
            // ==========================================================================================
            //  🆕 2026-10-27（执行代理 `B87_A3`）：v21 `hasActiveAbility` ＋ v23 `activeAbilityLogic`
            // ==========================================================================================
            //  🔴 **它治的是哪一条**：这两列**探针原来一个都没往 `CardDef` 传**（现读：池里每张卡的
            //     `CardDef.HasActiveAbility` 恒 `-1 = 未知`、`ActiveAbilityLogic` 恒 `null`）⇒
            //     `RuleCore.CanStartActiveAbility` 在探针里**恒 `ErrNoAbility(14)`**
            //     ⇒ 2026-10-27 新接的「原版通用那一格主动能力」（`RuleCore.UseActiveAbility`）
            //     **结构上量不到**（`play SOR66 preset aa=1` 读到 `rc=14`、三条断言全 ❌）。
            //     这与 `A1911` / `A1959` / v13 / v14 / v17 那几笔**逐字同族**（探针与引擎口径不同）。
            //  ✅ **不传的后果不是「中性」**：`hasActiveAbility=-1` 会让 `ConditionEval` 的 190/280
            //     退回文本近似 / 返回「判不了」，而引擎侧 `CardDatabase.Parse` 从 v21 起就读真值。
            //  ⚠️ 形状口径与 `CardDatabase.MapActiveAbilityLogic` 一致：坏 `ab`/`gate` 行丢弃、
            //     `ta`/`fm` 长度不一致 ⇒ 两格都按未知（`null`）、坏格落 `-1`；⛔ 不压缩、不猜。
            //  ⚠️ 局部名一律 `a3` 前缀（同族坑：同一个卡循环的子作用域里 `hasTrig` / `e` 已经用过）。
            int a3HasAA = -1;
            if (poolVersion >= 21 && c.TryGetProperty("hasActiveAbility", out var a3HaaEl)
                && a3HaaEl.ValueKind == JsonValueKind.Number)
                a3HasAA = a3HaaEl.GetInt32();
            else if (poolVersion >= 21)
                // 🔴 **缺键 ⇒ `0`，⛔ 不是 `-1`** —— 这里**故意**与「缺列 = 未知」那条总规矩不同，
                //   理由是**要跟引擎的实际读数一致**：`CardDatabase.CardDto.hasActiveAbility` 是
                //   裸 `public int`（默认 `0`），JsonUtility 缺键就给 `0` ⇒ 引擎侧这张卡
                //   `HasActiveAbility == 0` = **明确没有主动能力**。生成器今天只在**非 0** 时才写这个键
                //   （现读 v35：1129 张里只有 `SOR66` 带这一格），所以探针若按 `-1` 读，
                //   两边对「同一份表」的结论会**正好相反**（探针「未知」/ 引擎「没有」）。
                //   ⚠️ 表 < 21 时仍是 `-1`（那一版**整列都不存在**，引擎侧 `dto.version < 21` 也落 `-1`）。
                a3HasAA = 0;
            List<CardDef.ActiveAbilityLogicSpec> a3AaRows = null;   // `null` = 这一列没落（未知，⛔ 不是空表）
            if (poolVersion >= 23 && c.TryGetProperty("activeAbilityLogic", out var a3AalEl)
                && a3AalEl.ValueKind == JsonValueKind.Array)
            {
                a3AaRows = new List<CardDef.ActiveAbilityLogicSpec>();
                foreach (var a3Row in a3AalEl.EnumerateArray())
                {
                    if (a3Row.ValueKind != JsonValueKind.Object) continue;
                    if (!a3Row.TryGetProperty("ab", out var a3AbEl) || a3AbEl.ValueKind != JsonValueKind.Number) continue;
                    int a3Ab = a3AbEl.GetInt32();
                    if (a3Ab != CardDef.ActiveAbilityObject && a3Ab != CardDef.AlternativeAbilityObject) continue;
                    int a3Gate = -1, a3Trig = -1, a3Nl = -1;
                    if (a3Row.TryGetProperty("gate", out var a3GEl) && a3GEl.ValueKind == JsonValueKind.Number) a3Gate = a3GEl.GetInt32();
                    if (a3Gate != 1 && a3Gate != 0 && a3Gate != -1) continue;      // 坏 `gate` ⇒ 丢这一行（同引擎）
                    if (a3Row.TryGetProperty("trigger", out var a3TgEl) && a3TgEl.ValueKind == JsonValueKind.Number) a3Trig = a3TgEl.GetInt32();
                    if (a3Row.TryGetProperty("needsLoading", out var a3NlEl) && a3NlEl.ValueKind == JsonValueKind.Number) a3Nl = a3NlEl.GetInt32();

                    int[] a3Ta = A3IntArray(a3Row, "ta"), a3Fm = A3IntArray(a3Row, "fm");
                    if (a3Ta != null && a3Fm != null && a3Ta.Length != a3Fm.Length) { a3Ta = null; a3Fm = null; }
                    CardDef.ActiveAbilityDamageSpec[] a3Dmg = null;
                    if (a3Row.TryGetProperty("dmg", out var a3DEl) && a3DEl.ValueKind == JsonValueKind.Array)
                    {
                        a3Dmg = new CardDef.ActiveAbilityDamageSpec[a3DEl.GetArrayLength()];
                        int a3Di = 0;
                        foreach (var a3De in a3DEl.EnumerateArray())
                        {
                            var a3Sp = new CardDef.ActiveAbilityDamageSpec();
                            if (a3De.ValueKind == JsonValueKind.Object)
                            {
                                if (a3De.TryGetProperty("min",  out var a3V) && a3V.ValueKind == JsonValueKind.Number) a3Sp.Min = a3V.GetInt32();
                                if (a3De.TryGetProperty("max",  out var a3V2) && a3V2.ValueKind == JsonValueKind.Number) a3Sp.Max = a3V2.GetInt32();
                                if (a3De.TryGetProperty("opt",  out var a3V3) && a3V3.ValueKind == JsonValueKind.Number) a3Sp.ValueOptions = a3V3.GetInt32();
                                if (a3De.TryGetProperty("type", out var a3V4) && a3V4.ValueKind == JsonValueKind.Number) a3Sp.Type = a3V4.GetInt32();
                            }
                            a3Dmg[a3Di++] = a3Sp;
                        }
                    }
                    a3AaRows.Add(new CardDef.ActiveAbilityLogicSpec
                    {
                        Ability = a3Ab,
                        Gate = a3Gate,
                        Trigger = a3Trig,
                        NeedsLoading = a3Nl,
                        TargetsAffected = a3Ta,
                        FurtherMultiplier = a3Fm,
                        ChosenAbility = A3IntArray(a3Row, "ca"),
                        PlayersAffected = A3IntArray(a3Row, "pa"),
                        Damage = a3Dmg,
                    });
                }
            }

            if (c.TryGetProperty("chosenAbility", out var caEl) && caEl.ValueKind == JsonValueKind.Array)
            {
                var caList = new List<int>();
                foreach (var e in caEl.EnumerateArray())
                    if (e.ValueKind == JsonValueKind.Number) caList.Add(e.GetInt32());
                if (caList.Count > 0) { ForcedAttackRefund.Install(S("id"), caList.ToArray()); nRefundRows++; }
            }
            else nRefundUnknown++;
            // 🆕 2026-10-28（B88 · `A1d`）：**`abilityDamage`（原版 `damageCriteria` 四格）也要传进 `CardDef`。**
            //   为什么必须补：本探针 `LoadPool` 是**手读 JSON**、不走 `Data/CardDatabase.cs` ⇒
            //   漏这一列 = `CardDef.AbilityDamageAt(...)` 恒 `null` ⇒ 90 档「逐目标掷」
            //   在探针里**永远不触发** ⇒ `play all dump` / `play check` 会给出**盲绿**
            //   （与 `A1911` / `animTimes` / `A1959` 是同一族坑：探针 DTO 与生产 DTO 不同步）。
            //   口径与引擎 `CardDatabase.MapAbilityDamage` **逐字相同**（缺列 ⇒ `null` = 未知；
            //   缺 `ab/lg` / 撞键 ⇒ 出声丢弃）。
            CardDef.AbilityDamageSpec[] __adRows = null;
            if (c.TryGetProperty("abilityDamage", out var __adEl) && __adEl.ValueKind == JsonValueKind.Array)
            {
                var __adTmp = new List<CardDef.AbilityDamageSpec>();
                var __adSeen = new HashSet<long>();
                foreach (var __r in __adEl.EnumerateArray())
                {
                    if (__r.ValueKind != JsonValueKind.Object) continue;
                    int __ab = __r.TryGetProperty("ab", out var __abe) && __abe.ValueKind == JsonValueKind.Number ? __abe.GetInt32() : -1;
                    int __lg = __r.TryGetProperty("lg", out var __lge) && __lge.ValueKind == JsonValueKind.Number ? __lge.GetInt32() : -1;
                    if (__ab < 0 || __lg < 0)
                    {
                        Console.WriteLine("⚠️ `abilityDamage` 有一行缺 ab/lg（" + S("id") + "）—— 丢弃");
                        continue;
                    }
                    long __k = ((long)__ab << 32) ^ (uint)__lg;
                    if (!__adSeen.Add(__k))
                    {
                        Console.WriteLine("⚠️ `abilityDamage` 撞键（" + S("id") + " ab=" + __ab + " lg=" + __lg + "）—— 丢弃后一条");
                        continue;
                    }
                    __adTmp.Add(new CardDef.AbilityDamageSpec
                    {
                        Ab = __ab, Lg = __lg,
                        Min = __r.TryGetProperty("min", out var __mn) && __mn.ValueKind == JsonValueKind.Number ? __mn.GetInt32() : -1,
                        Max = __r.TryGetProperty("max", out var __mx) && __mx.ValueKind == JsonValueKind.Number ? __mx.GetInt32() : -1,
                        ValueOptions = __r.TryGetProperty("opt", out var __op) && __op.ValueKind == JsonValueKind.Number ? __op.GetInt32() : -1,
                        Type = __r.TryGetProperty("type", out var __ty) && __ty.ValueKind == JsonValueKind.Number ? __ty.GetInt32() : -1,
                    });
                }
                __adRows = __adTmp.ToArray();
            }
            var __def = new CardDef(S("id"), S("name"), S("type"), S("desc"), S("rarity"), S("faction"),
                                 I("cost"), I("attack"), I("health"), I("ranged"), kws,
                                 S("nameZh"), S("descZh"), true, S("subtype"), S("goesSecond"),
                                 inventoryOptions: hasInv ? invEl.GetInt32() : -1,
                                 showInCollectionOptions: hasShow ? showEl.GetInt32() : -1,
                                 initiative: hasInit ? initEl.GetInt32() : -1,
                                 // 🆕 `B8_AV`（2026-10-26）：原版 `RawCardScript.targeted` 透传（`-1` = 未知）。
                                 //   缺列 `-1`（⛔ 不是 `0` —— `0` 是「不需要选目标」的合法真值）。
                                 targeted: hasTgt ? tgtEl.GetInt32() : -1,
                                 // 🆕 2026-10-19（`A1959`，v10）：缺列 `-1` / 空表 ⇒ 如实收下，
                                 //   ⛔ 不在这里替消费侧决定「该不该开面板」（那在 `EffectText` 那四道闸）。
                                 choicesTargetsAffected: hasCTa ? cTaEl.GetInt32() : -1,
                                 choicesMaxTargets: hasCMt ? cMtEl.GetInt32() : -1,
                                 choicesSpecificCardIds: hasCIds ? chIds : null,
                                 // 🆕 2026-10-21（`T03·c` 批 B 接线）：v13 的逐卡触发优先级 ——
                                 //   `CardDef` 那个形参是**可选**（默认 null ⇒ 全池 1），这里只在有值时才传。
                                 triggerPriority: trigPrio.Count > 0 ? trigPrio : null,
                                 // 🆕 2026-10-22（引擎线 `W63` · `T03·c/d`）：v14 的逐条 `abilityContext`。
                                 //   四个形参**全是可选**（`CardDef.cs:598/602`）⇒ 这一段只补两个命名实参，
                                 //   前面那些老调用点的位置传参**不受影响**。
                                 //   `-1` 契约 / 四道闸 / skip 表 → 上面读数那一段注释（⛔ 别在这里抄第二份）。
                                 //   表 `< 14` 时 `ablCtx` 恒空 ⇒ 整列不传（= 全 -1 未知），
                                 //   与引擎 `Parse` 的 `< 14` 守卫同口径。
                                 abilityContext: ablCtx.Count > 0 ? ablCtx : null,
                                 abilityContextsVersioned: poolVersion >= 14,
                                 // 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：v15 的逐卡
                                 //   结构化条件（原版 `TargetCriteria.conditionsFilter`）。
                                 //   四道闸 / `-1` 契约 → 上面读数那一段注释（⛔ 别在这里抄第二份）。
                                 //   表 `< 15` 时 `unitConds` 恒空 ⇒ 整列不传（= 全 -1 未知），
                                 //   与引擎 `Parse` 的 `< 15` 守卫同口径。
                                 abilityConditions: unitConds.Count > 0 ? unitConds : null,
                                 conditionsVersioned: poolVersion >= 15,
                                 // 🆕 v16（`B-16` 缺口）：`count` 行的被数集合来自 v16 卡表的正面信号
                                 //   （口径同引擎 `CardDatabase.Parse` 的 `conditionsCountSetVersioned`）。
                                 conditionsCountSetVersioned: poolVersion >= 16,
                                 // 🆕 v17（2026-10-23 · 引擎线 · `B-16` 残余 / `B-17`）：两列 ——
                                 //   缺列口径 / `-1` 契约 / 下标即语义 → 上面读数那一段注释（⛔ 别在这里抄第二份）。
                                 //   ⚠️ 版本位与引擎 `CardDatabase.Parse` 同一口径：`dto.version >= 17`
                                 //   （它同时置 `AbilityTriggersVersioned` 与 `AbilityTargetsVersioned` ——
                                 //    v17 一次加的两列，`CardDef` 构造函数末尾就是这么绑的）。
                                 abilityTriggers: a17HasTrig ? a17TrigVals : null,
                                 abilityTriggersVersioned: poolVersion >= 17,
                                 abilityTargets: a17HasTgt ? a17TgtRows : null,
                                 // 🆕 v20（2026-10-24 · 引擎线 `A12`）：逐 `abilityLogic` 的
                                 //   `furtherMultiplier` —— 与 `abilityTargets` **逐条同下标**；
                                 //   缺列 / 版本位口径同引擎 `CardDatabase.Parse`（`dto.version >= 20`）。
                                 abilityMultipliers: a20HasFm ? a20FmRows : null,
                                 abilityMultipliersVersioned: poolVersion >= 20,
                                 // 🆕 2026-10-24（`G2`）：v11 的逐候选动画时间（见上面那一段注释）。
                                 animTimes: animRows.Count > 0 ? animRows : null,
                                 // 🆕 v25（2026-10-24 · `W25`）：36 格 criteria 两条支。
                                 //   缺列（< v25 的表 / 旧调用方）⇒ `null` = **未知**（⛔ 不是空表），
                                 //   版本位与引擎 `CardDatabase.Parse` 同一口径（`dto.version >= 25`）。
                                 abilityTargetCriteria: atcRows,
                                 abilityTargetCriteriaVersioned: poolVersion >= 25,
                                 activeAbilityTargetCriteria: aatRows,
                                 activeAbilityTargetCriteriaVersioned: poolVersion >= 25,
                                 // 🆕 2026-10-27（`B87_A3`）：v21 主动能力闸 ＋ v23 `activeAbilityLogic`
                                 //   （见上面那一段长注释；⛔ 缺列一律 `-1` / `null` = 未知，不是「没有」）。
                                 hasActiveAbility: a3HasAA,
                                 activeAbilityLogic: a3AaRows,
                                 activeAbilityLogicVersioned: poolVersion >= 23);
            // 🆕 2026-10-28（B88 · `A1d`）：构造后挂载（与引擎 `CardDatabase.Parse` 同一口径）。
            __def.AttachAbilityDamage(__adRows);
            list.Add(__def);
        rawKw.Add(kws);
        }
        // 🔴 **缺列一律出声**（铁律「不许静默失败」）：缺了就是缺了，⛔ 不让它静默变成「某张卡的值是 X」。
        if (poolVersion < 8)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 8 —— 这一版没有 `initiative` 一列"
                              + "（引擎侧同样会为此出声：`CardDatabase.cs:108-111`）⇒ 探针把它读成 **-1（缺值）**，"
                              + "⛔ 不是 `0`（`Initiative.Undefined`）。本次任何「先后手」读数都缺真值。"
                              + "跑 `工具/gen_cards_engine.py` 重生成。");
        if (nMissInv > 0 || nMissShow > 0 || nMissInit > 0)
            Console.WriteLine("⚠️ 卡池有缺列（探针按 **-1 = 缺值** 收下，⛔ 没当成 `0`）：`inventoryOptions` 缺 "
                              + nMissInv + " 行 · `showInCollectionOptions` 缺 " + nMissShow + " 行 · `initiative` 缺 "
                              + nMissInit + " 行 —— 先看生成器是不是漏了这一列（`gen_cards_engine.py`）。");
        // 🆕 `B8_AV`（2026-10-26）：`targeted` 缺列同样出声（⛔ 不静默变成「这张卡不需要选目标」）。
        if (nMissTargeted > 0)
            Console.WriteLine("⚠️ 卡池有缺列（探针按 **-1 = 缺值** 收下，⛔ 没当成 `0`）：`targeted` 缺 "
                              + nMissTargeted + " 行 —— ⇒ `EffectText.AssignCardTargetFallback` 的闸（`Targeted != 1`）"
                              + "对这些行恒不成立，**卡级 `cardtarget` 条件挂不上**（`play` 的读数会与引擎不同口径）；"
                              + "先看生成器是不是漏了这一列（`gen_cards_engine.py`）。");
        // 🆕 2026-10-19（`A1959`）：`choicesDefinition` 三列同样出声（判据/后果见上面那一段）。
        if (nMissChoicesTa > 0 || nMissChoicesMt > 0 || nMissChoicesIds > 0)
            Console.WriteLine("⚠️ 卡池有缺列（探针按 **-1 / 空表 = 缺值** 收下）：`choicesTargetsAffected` 缺 "
                              + nMissChoicesTa + " 行 · `choicesMaxTargets` 缺 " + nMissChoicesMt
                              + " 行 · `choicesSpecificCardIds` 缺 " + nMissChoicesIds + " 行 —— "
                              + "⇒ `HasSpecificCardChoice` 对这些行恒假、`A1960` 的选牌注入**看不见**；"
                              + "先确认卡池是 v10（`gen_cards_engine.py`）。");
        // 🆕 2026-10-21（`T03·c` 批 B 接线）：`triggerPriority` 的读数（缺列 / 形状不对**都出声**）。
        if (poolVersion >= 0 && poolVersion < 13)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 13 —— 这一版没有 `triggerPriority` 一列"
                              + "（引擎侧 `CardDatabase.cs` 同样会为此出声）⇒ 探针把它读成 **1（原版默认 Instant）**，"
                              + "`ActionQueue` 的四道在探针里就**永远只有 Instant 那条有东西**。"
                              + "跑 `工具/gen_cards_engine.py` 重生成。");
        if (nBadTp > 0)
            Console.WriteLine("⚠️ 卡池 `triggerPriority` 有 " + nBadTp + " 条形不全（缺 `keyword` / 缺 `priority` / "
                              + "`priority` 不在 0..3）"
                              + "—— 已跳过（⛔ 没当成默认值 1、也没当成 0 悄悄收下）。"
                              + "引擎侧 `CardDatabase.MapTriggerPriority` / `CardDef` 构造函数是同口径（出声 + 丢弃）。");
        // 🆕 2026-10-27（`B9_E5ENG` · 第五批）：`ForcedAttackRefund` 表的装载读数。
        //   🔴 这三格**必须打出来**：装载一旦没做（或缺 `chosenAbility` 一列），`play` 里 235/236 会
        //   **静默**退回「一律退还」（`Of` ⇒ `Unknown` ⇒ 下游按设计退还），读数看着正常、口径却错了。
        //   期望（卡表 v32 现读）：**退还 9 / 不退还 3 / Unknown 1116**。
        int nRefund = 0, nNoRefund = 0, nRefundUnk = 0;
        foreach (var c0 in list)
        {
            var vd = ForcedAttackRefund.Of(c0.Id);
            if (vd == ForcedAttackRefund.Verdict.Refund) nRefund++;
            else if (vd == ForcedAttackRefund.Verdict.NoRefund) nNoRefund++;
            else nRefundUnk++;
        }
        Console.WriteLine("  ForcedAttackRefund: 灌入 " + nRefundRows + " 条（缺 `chosenAbility` 列 = " + nRefundUnknown
                          + " 张）⇒ 实得 退还 " + nRefund + " / 不退还 " + nNoRefund + " / Unknown " + nRefundUnk
                          + "（判据 `{231,237,238}` 退 · `{230,233,235,236}` 不退 · 都没有 ⇒ Unknown ⇒ 下游退回「退还」）");
        Console.WriteLine("  triggerPriority: 带非默认值的卡 = " + nTpCards
                          + "（缺键 = 1 = Instant；判据 `CardAbility__.ctor.c:5`）");
        // 🆕 2026-10-22（引擎线 `W63` · `T03·c/d`）：`abilityContext` 的读数（旧表 / 形状不对**都出声**）。
        //   ⚠️ 「缺列」与「这一列的每一格都是 `Play(0)`」在数据上**分不开**（生成器只落非 0 行）
        //     ⇒ 只能靠 `version` 那道闸区分 —— 这正是 `< 14` **必须出声**的原因
        //     （引擎侧同一条判据 → `CardDatabase.cs:498-504`）。
        if (poolVersion < 14)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 14 —— 这一版没有 `abilityContext` 一列"
                              + "（原版 `CardAbility.abilityContext`：0 Play / 10 Hand / 20 Deck / 30 Cemetery"
                              + " / 40 InPlayOrDying（**含**待死） / 50 InPlayOrCementery（**不含**待死））；"
                              + "引擎侧 `CardDatabase.cs:498-504` 同样为此出声 ⇒ 探针一律读成 **-1（未知）**，"
                              + "⛔ 不是 `Play(0)`（那会把死亡族 / 墓地族触发**静默掐死**）。"
                              + "跑 `工具/gen_cards_engine.py` 重生成。");
        if (nAcOnOldTable > 0)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 14 **却带着 `abilityContext` 列**："
                              + nAcOnOldTable + " 张卡的这一列被**整列忽略**（= 全 -1 未知）。"
                              + "⚠️ 引擎侧 `CardDatabase.Parse` 只按 `version` 出声、列**照样映射** ⇒ "
                              + "这种表上探针与引擎**会分叉**，⛔ 别拿这份读数当引擎现状。");
        if (nBadAc > 0)
            Console.WriteLine("⚠️ 卡池 `abilityContext` 有 " + nBadAc + " 条被丢弃（缺 `name` / 缺 `trigger` 键 / "
                              + "`context` 缺键或不在 {0,10,20,30,40,50} / 同 `name` 或同 `keyword` 重复）"
                              + " —— ⛔ 没当成 0（Play）、也没静默取最后一条。"
                              + "引擎侧 `CardDatabase.MapAbilityContext` 是同口径（出声 + 丢弃）；"
                              + "先跑 `工具/gen_cards_engine.py`。");
        Console.WriteLine("  abilityContext: 带非 0 context 的卡 = " + nAcCards + " · 条 = " + nAcRows
                          + "（收下来的行；对账期望 = **170 条 / 157 张**"
                          + " —— 判据 → 资料/普查产出_1022/W63_abilityContext_施工简报.md §2）");
        // 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：`unitConditions` 的读数（旧表 / 形状不对**都出声**）。
        //   判据 → 资料/普查产出_1022/W64_兜底审计_战斗.md 的 B-16（原版结构化条件 = `conditionsFilter`）。
        if (poolVersion < 15)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 15 —— 这一版没有 `unitConditions` 一列"
                              + "（原版 `TargetCriteria.conditionsFilter` + `excludeCondition`）⇒ "
                              + "探针把 `CardDef.ConditionOf` 一律读成 **-1（未知）**，"
                              + "⛔ 不是 0（`none` = 「没有条件」）—— 两个哨兵必须分开（引擎侧 `CardDatabase.cs` 的 `< 15` 守卫同口径）。");
        if (nUcOnOldTable > 0)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 15 **却带着 `unitConditions` 列**："
                              + nUcOnOldTable + " 张卡 —— 探针按 version 整列不采，"
                              + "⚠️ 引擎侧 `CardDatabase.Parse` 只按 `version` 出声、列**照样映射** ⇒ 两边会分叉。");
        if (nBadUc > 0)
            Console.WriteLine("⚠️ 卡池 `unitConditions` 有 " + nBadUc + " 条被丢弃（`role` 不认识 / 缺 `ab`·`lg` 键 / "
                              + "`cond` 缺键·为 none·不在枚举里 / 同 (ab,lg,role) 重复）—— 口径同 `CardDatabase.MapUnitConditions`。");
        Console.WriteLine("  unitConditions: 带行的卡 = " + nUcCards + " · 条 = " + nUcRows
                          + "（原版 `TargetCriteria.conditionsFilter`；`mult`/`multi`/`avail`/`choice` 四格"
                          + " ＋ 🆕 `count` 第五格（批次 5 `G3`，第三道闸的数据）的 `cond = 0` 行**照收**"
                          + "（与引擎同口径），其余 role 的 none(0) 省略"
                          + " —— 判据 → 资料/普查产出_1022/W64_兜底审计_战斗.md B-16）");
        // 🆕 v16（`B-16` 的明确缺口）：`count` 行的**被数集合**（原版 `BoardCriteria.countCriteria` ＋ `playerTurn`）。
        if (poolVersion < 16)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 16 —— `count` 行没有 `countSet`（被数集合）"
                              + " ⇒ 探针把 `CardDef.ConditionSpec.CountSet` 一律读成 **null = 未知**"
                              + "（⛔ 不是「默认集合」：全 0 是一个具体的集合选择）。");
        if (nBadUcSet > 0)
            Console.WriteLine("⚠️ 卡池 `unitConditions[count]` 有 " + nBadUcSet + " 处 `countSet` 缺键/坏值"
                              + "（缺格 / 布尔非 0-1 / `playerTurn` 不在 0·10·20 / 引用列表不是数组或含空串）"
                              + " —— 口径同 `CardDatabase.MapCountSet`：**那一格落 -1**，⛔ 不丢整行、不 clamp。");
        Console.WriteLine("  unitConditions[count]: 带被数集合的行 = " + nUcSetRows + " · 坏值格数 = " + nBadUcSet
                          + "（原版 `BoardCriteria.countCriteria` ＋ `playerTurn`；判据 → 同上 B-16）");
        // 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`mult` / `multi` / `avail` / `choice` 四格的
        //   **整条 criteria**（原版 `abilityLogic.multiplier` / `CardAbility.multiCountCriteria` /
        //   `RawCardScript.targetsAvailableCounter` / `.choicesDefinition`）。
        //   `nMultSetRows > 0` 是 `EffectResolver` 的结构化乘数**真的在干活**的前提。
        if (poolVersion < 22)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 22 —— 这一版 `unitConditions` 的 "
                              + "`mult` / `multi` / `avail` / `choice` 四行**没有**整条 criteria"
                              + "（`multiplierSet` / `multiSet` / `availSet` / `choiceSet`）⇒ 探针把"
                              + " `CardDef.ConditionSpec` 那四格一律读成 **null = 未知**"
                              + "（⛔ 不是「默认 criteria」）。引擎侧 `CardDatabase.cs` 的 `< 22` 守卫同口径。"
                              + "跑 `工具/gen_cards_engine.py` 重生成。");
        if (nUcRoleSetRows > 0 || nBadUcSet > 0)
            Console.WriteLine("  unitConditions[roleSet]: `mult`/`multi`/`avail`/`choice` 四格落整条 criteria 的行 = "
                              + nUcRoleSetRows + "（对账期望：mult 31 · multi 9 · avail 17 · choice 84 = **141**）");
        // 🆕 v17（2026-10-23 · 引擎线 · `B-16` 残余 241/242 ＋ `B-17`）：两列的读数（旧表 / 缺列 / 形状坏**都出声**）。
        //   判据 → `资料/普查产出_1023b/引擎线_尾巴_执行报告.md` §2·3（abilityTriggers）与 §3·2（abilityTargets）。
        if (poolVersion < 17)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 17 —— 这一版没有 `abilityTriggers` / "
                              + "`abilityTargets` 两列 ⇒ 探针把 `CardDef.AbilityTriggerPresence` 一律读成 **-1（未知）**、"
                              + "`CardDef.AbilityTargets` 一律空表，⛔ 不是「没有 ability」（`[]` 才是明确没有）。"
                              + "引擎侧 `CardDatabase.cs` 的 `< 17` 守卫同口径。跑 `工具/gen_cards_engine.py` 重生成。");
        if (nMissTrig > 0 || nMissTgt > 0)
            Console.WriteLine("⚠️ 卡池有缺列（探针按 **null = 未知** 收下，⛔ 没当成 `[]` = 没有 ability）："
                              + "`abilityTriggers` 缺 " + nMissTrig + " 行 · `abilityTargets` 缺 " + nMissTgt + " 行"
                              + " —— 先确认卡池是 v17（`gen_cards_engine.py`）。");
        if (nBadTrig > 0 || nBadTaCell > 0)
            Console.WriteLine("⚠️ 卡池 v17 两列形状坏：`abilityTriggers` 非整数 " + nBadTrig + " 个 · "
                              + "`abilityTargets` 坏格 " + nBadTaCell + " 个（`ta` 非数组 / 单格 < -1）"
                              + " —— 口径同 `CardDatabase.MapAbilityTargets`：**那一格落 -1**，"
                              + "⛔ 不丢整条 ability（下标即语义）。先跑 `工具/gen_cards_engine.py`。");
        Console.WriteLine("  abilityTriggers: 带这一列的卡 = " + nTrigCards + " · trigger 值 = " + nTrigValues
                          + " · abilityTargets: 带这一列的卡 = " + nTgtCards
                          + " · ability = " + nTgtAbilities + " · logic = " + nTgtLogics
                          + "（对账期望：1127 张 / 1193 值 / 1127 张 / 1270 条 / 1616 个"
                          + " —— 判据 → 资料/普查产出_1023b/引擎线_尾巴_执行报告.md §2·3/§3·2）");
        // 🆕 v20（2026-10-24 · 引擎线 `A12`）：`abilityMultipliers`（原版 `AbilityLogic.furtherMultiplier`）读数。
        //   `nFmGt1 > 0` 是这个消费端**真的在干活**的前提（全 0/1 ⇒ 乘不乘都一样）。
        if (poolVersion < 20)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 20 —— 这一版没有 `abilityMultipliers` 一列"
                              + " ⇒ `CardDef.AbilityTargetSpec.FurtherMultiplier` 一律 **null（未知）**、"
                              + "`EffectResolver` 那一步不乘（= 改前行为，⛔ 不是「数据说 fM 全是 0」）。"
                              + "引擎侧 `CardDatabase.cs` 的 `< 20` 守卫同口径。跑 `工具/gen_cards_engine.py` 重生成。");
        if (nMissMul > 0 || nBadFmCell > 0)
            Console.WriteLine("⚠️ 卡池 v20 那列：缺 " + nMissMul + " 行 · 坏格 " + nBadFmCell + " 个"
                              + "（`fm` 非数组 / 单格 < -1）—— 口径同 `CardDatabase.MapAbilityMultipliers`："
                              + "**那一格落 -1**，⛔ 不丢整条 ability。先跑 `工具/gen_cards_engine.py`。");
        Console.WriteLine("  abilityMultipliers: 带这一列的卡 = " + nMulCards + " · logic = " + nMulLogics
                          + " · **取值 > 1 的格子 = " + nFmGt1 + "**"
                          + "（原版 `AbilityLogic.furtherMultiplier`；0 = 不乘、-1 = 缺键）");
        // 🆕 v25（2026-10-24 · 执行代理 `W25`）：两条 36 格 criteria 列的读数。
        if (poolVersion < 25)
            Console.WriteLine("⚠️ 卡池 `version`=" + poolVersion + " < 25 —— 这一版没有 `abilityTargetCriteria` /"
                              + " `activeAbilityTargets` 两列 ⇒ 探针把 `CardDef.AbilityTargetCriteriaVersioned` 读成"
                              + " **false** ⇒ `AttachTargetCriteria` 直接 return ⇒ `play` / `check` 里**这条筛不生效**"
                              + "（与引擎 `CardDatabase.Parse` 的 `< 25` 守卫同口径）。跑 `工具/gen_cards_engine.py` 重生成。");
        if (nBadTc > 0 || nTcRows == 0)
            Console.WriteLine("⚠️ 卡池 `abilityTargetCriteria` / `activeAbilityTargets` 读数异常：丢弃/坏格 "
                              + nBadTc + " 处 · 收下来的行 " + nTcRows + " 行 —— 口径同 "
                              + "`CardDatabase.MapTargetCriteriaRows`（行级四道闸丢弃 / 单格坏值落 -1 不丢行）。"
                              + "⚠️ 若为 0 行请先确认卡池是 v25。");
        Console.WriteLine("  abilityTargetCriteria + activeAbilityTargets: 带行的卡 = " + nTcCards
                          + " · 行 = " + nTcRows + " · 坏行/坏格 = " + nBadTc
                          + "（原版 `AbilityLogic.targetCriteria` 的其余 36 格；判据 → "
                          + "资料/普查产出_1024c/E9_卡表v25_logic级判据列.md）");

        Pool = list;
        // 🆕 **同序留一份 `keywords` 原文**（见 `PoolRawKeywords` 的说明）——
        //    `CardDef.Keywords` 只有规范化之后的键，`willrun` 要判「卡面上写着哪几段正文」得看原文。
        PoolRawKeywords = rawKw;
        // 🔴 **2026-10-10 `A1339`：探针必须与引擎【同口径】—— 就是缺了这一句。**
        //   引擎侧：`Data/CardDatabase.cs:113` 在读完卡表之后调 `CreatePool.BuildNameIndex(list)`；
        //   探针原来两边都不建 ⇒ `CreatePool.MatchCardName` 恒返回 null（`CreatePool.cs:613` 明写
        //   「没建索引时一律返回 null ⇒ 解析行为与从前完全一致」）⇒ 凡「目标靠**卡名**指」的句子，
        //   探针一律报 `target=[]` ＋ 落 `partial` 列。**它已经产出过两次假证据**：
        //   ① `A1334` 的前提（`UM_Angels_of_Death` 的 `Target == null`）
        //      ⚠️ 2026-10-14：那张卡的 id 现已是 `UM64`（换真值那批改的）—— 本行是**当时**的记录，别照它去认卡。
        //   ② `A1311` 的「它是 3 参重载唯一的真卡用户」（引擎里它 `Side=own`、走 4 参）。
        //   ⇒ 修法见 `资料/普查产出_第十三会话/D_A1334诊断.md` 顺手发现 2。
        // ⚠️ 建在**全池**上（含 `desc` 为空那几张卡）—— 与引擎一致：那几张照样能被别的卡的文本指到。
        // ⚠️ 它同时建**两张**表（英文 `_nameIndex` ＋ 中文 `_nameIndexCjk`），别只当英文那张看。
        CreatePool.BuildNameIndex(list);
        // 🆕 **2026-10-14（`A1070` 第 ③ 档）**：与引擎同口径的第二处 —— 引擎在 `CardDatabase.Parse`
        //   末尾调 `ResolveGoesSecond`（督军 `goesSecond` 的 id → 对象引用）。探针**不走 `Parse`**
        //   （它自己造 `CardDef`，为的是同时留一份 `keywords` 原文）⇒ 这里必须跟着调一次，
        //   否则探针会报「`GoesSecondCard` 有值 0 张」这种**看起来像工程缺陷**的假读数
        //   （与上面 `A1339` 那条同族：**探针与引擎不同口径**）。
        CreatePool.ResolveGoesSecond(list);
    }

    /// <summary>🆕 2026-10-22（引擎线 `W63` · `T03·c/d`）：`AbilityContext` 的**六档合法值** ——
    /// `0 Play` / `10 Hand` / `20 Deck` / `30 Cemetery` / `40 InPlayOrDying`（**含**待死） /
    /// `50 InPlayOrCementery`（**不含**待死）。
    /// ⚠️ 引擎侧同一件事写在 `CardDatabase.IsLegalAbilityContext`（`private`，探针够不着）——
    /// **常量本身只有一份** = `CardDef.AbilityContextPlay` 那一族（`Core/CardDef.cs:1252-1270`），
    /// 这里只做「是不是这六个数之一」，⛔ 别另抄一组数字（两处迟早不一致）。
    /// 🔴 `-1`（<see cref="CardDef.AbilityContextUnknown"/>）**不合法**：它是「缺键 / 未知」的哨兵，
    /// ⛔ 别放进来当第七档。</summary>
    static bool IsLegalAbilityContextProbe(int ctx)
    {
        return ctx == CardDef.AbilityContextPlay
            || ctx == CardDef.AbilityContextHand
            || ctx == CardDef.AbilityContextDeck
            || ctx == CardDef.AbilityContextCemetery
            || ctx == CardDef.AbilityContextInPlayOrDying
            || ctx == CardDef.AbilityContextInPlayOrCemetery;
    }

    /// <summary>🆕 v16（2026-10-22 · 引擎线 `W64` · `B-16` 缺口）：`unitConditions[].countSet` →
    /// <see cref="CardDef.CountSetSpec"/>。**口径与 `CardDatabase.MapCountSet` 一致**：
    /// 缺键 / 类型不对 / 布尔非 0-1 / `playerTurn` 不在 0-10-20 ⇒ **那一格落 `-1` 并计一条 bad**，
    /// ⛔ 不丢整行、不 clamp、不默认。`countSet` 不是对象 ⇒ 返回 null（未知）并计一条 bad。
    /// ⚠️ 语义校验（各枚举的成员表）**不在这里做** —— 与引擎侧同口径，只查类型/缺键/域。
    /// 返回的 `badCount` = 坏值/缺键的**格数**（不是行数）。
    /// </summary>
    /// <summary>🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`unitConditions` 里**某一 role 的整条 criteria**
    /// （`multiplierSet` / `multiSet` / `availSet` / `choiceSet`）→ <see cref="CardDef.CountSetSpec"/>。
    /// **口径与 `CardDatabase.RoleSet` 一致**：
    /// · 缺这一格 ⇒ `null`（**未知**，⛔ 不是「默认 criteria」）**且不计 bad**；
    /// · **错位**（`ucRole != role` 却带着这一格）⇒ 计一条 bad 并忽略（⛔ 不静默挂到别的 role 上）；
    /// · 有这一格就交给 <see cref="ParseCountSet"/>（坏格落 `-1` 并计 bad，**不丢整行**）。
    /// </summary>
    static CardDef.CountSetSpec ParseRoleSet(JsonElement e, string ucRole, string role,
                                             string field, ref int badCount)
    {
        if (!e.TryGetProperty(field, out var el)) return null;      // 缺 = 未知，⛔ 不出声
        if (ucRole != role) { badCount++; return null; }            // 错位 = 坏数据
        var r = ParseCountSet(el, out int bad);
        badCount += bad;
        return r;
    }

    static CardDef.CountSetSpec ParseCountSet(JsonElement el, out int badCount)
    {
        int bad = 0;
        if (el.ValueKind != JsonValueKind.Object) { badCount = 1; return null; }
        var s = new CardDef.CountSetSpec();

        int Int(string name)
        {
            if (!el.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Number)
            { bad++; return CardDef.CountSetSpec.Unknown; }
            return v.GetInt32();
        }
        int Flag(string name)
        {
            int v = Int(name);
            if (v == CardDef.CountSetSpec.Unknown) return v;
            if (v != 0 && v != 1) { bad++; return CardDef.CountSetSpec.Unknown; }
            return v;
        }
        string[] Ids(string name)
        {
            if (!el.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array)
            { bad++; return null; }
            var l = new List<string>();
            foreach (var it in v.EnumerateArray())
            {
                if (it.ValueKind != JsonValueKind.String) { bad++; continue; }
                string x = it.GetString();
                if (string.IsNullOrEmpty(x)) { bad++; continue; }
                l.Add(x);
            }
            return l.ToArray();
        }

        int pv = Int("playerTurn");
        if (pv != CardDef.CountSetSpec.Unknown && pv != 0 && pv != 10 && pv != 20)
        { bad++; pv = CardDef.CountSetSpec.Unknown; }
        s.PlayerTurn = pv;

        s.TargetsAffected = Int("targetsAffected");
        s.ChosenArmy = Int("chosenArmy");
        s.ArmyFilter = Int("armyFilter");
        s.KeywordFilter = Int("keywordFilter");
        s.TraitsFilter = Int("traitsFilter");
        s.MaxTargets = Int("maxTargets");
        s.TargetSelectMethod = Int("targetSelectMethod");
        s.AttackFilter = Int("attackFilter");
        s.AttackFilterOption = Int("attackFilterOption");
        s.CurrentHealthFilter = Int("currentHealthFilter");
        s.CurrentHealthFilterOption = Int("currentHealthFilterOption");
        s.ManaCostFilter = Int("manaCostFilter");
        s.ManaCostFilterOption = Int("manaCostFilterOption");
        s.RarityFilter = Int("rarityFilter");
        s.RarityFilterOption = Int("rarityFilterOption");
        s.SpellType = Int("spellType");

        s.FriendlyUnits = Flag("friendlyUnits");
        s.EnemyUnits = Flag("enemyUnits");
        s.IncludeHero = Flag("includeHero");
        s.IncludeMinions = Flag("includeMinions");
        s.IncludeSpells = Flag("includeSpells");
        s.IncludeWhispers = Flag("includeWhispers");
        s.IncludeWeapons = Flag("includeWeapons");
        s.IncludeRemnants = Flag("includeRemnants");
        s.IncludeHeroParts = Flag("includeHeroParts");
        s.IncludeSelf = Flag("includeSelf");
        s.ExcludeNotSelf = Flag("excludeNotSelf");
        s.ExcludeSimilarCards = Flag("excludeSimilarCards");
        s.ExcludeTargetted = Flag("excludeTargetted");
        s.ExcludeFriendlyHero = Flag("excludeFriendlyHero");
        s.ExcludeEnemyHero = Flag("excludeEnemyHero");
        s.ExcludeRace = Flag("excludeRace");
        s.ExcludeTrait = Flag("excludeTrait");
        s.SelectFromChooseResolution = Flag("selectFromChooseResolution");

        s.FilterSpecificCardIds = Ids("filterSpecificCards");
        s.ExcludeEnchantmentIds = Ids("excludeEnchantments");

        badCount = bad;
        return s;
    }

    /// <summary>🆕 v25（2026-10-24 · 执行代理 `W25`）：`abilityTargetCriteria` / `activeAbilityTargets`
    /// 一列 → <see cref="CardDef.TargetCriteriaRowSpec"/>。**口径与 `CardDatabase.MapTargetCriteriaRows`
    /// （`Data/CardDatabase.cs:2352`）逐字相同**：
    /// · **缺这一列** ⇒ `null`（**未知**，⛔ 不是空表 —— 空表 = 「原版明确没有这一行」）；
    /// · **行级四道闸**（缺 `ab` / `ab` 不属这一列 / `lg &lt; 0` / `criteria` 不是对象）
    ///   ⇒ **丢弃那一行并计 bad**（⛔ 缺键落 0 = 挂到第 0 条 ability / 第 0 条 logic 上，那是最坏的一种静默错）；
    /// · 36 格本体交给 <see cref="ParseCountSet"/>（**同一份字段表**；单格坏值落 `-1` 并计 bad，**不丢整行**）。
    /// 🔴 这一列不传的后果 = 「探针与引擎口径不同」（`A1911` 那一族）：`CardDef.AbilityTargetCriteriaVersioned`
    /// 恒假 ⇒ `AttachTargetCriteria` 直接 return ⇒ `play` / `check` 里这条筛**根本不生效**（假阴性）。
    /// </summary>
    static List<CardDef.TargetCriteriaRowSpec> ParseTargetCriteriaRows(
        JsonElement c, string col, bool active, ref int badCount)
    {
        if (!c.TryGetProperty(col, out var el) || el.ValueKind != JsonValueKind.Array) return null;
        var list = new List<CardDef.TargetCriteriaRowSpec>();
        foreach (var r in el.EnumerateArray())
        {
            if (r.ValueKind != JsonValueKind.Object) { badCount++; continue; }
            if (!r.TryGetProperty("ab", out var abEl) || abEl.ValueKind != JsonValueKind.Number) { badCount++; continue; }
            int ab = abEl.GetInt32();
            int lg = r.TryGetProperty("lg", out var lgEl) && lgEl.ValueKind == JsonValueKind.Number ? lgEl.GetInt32() : -1;
            int trigger = r.TryGetProperty("trigger", out var tgEl) && tgEl.ValueKind == JsonValueKind.Number ? tgEl.GetInt32() : -1;
            if (active)
            {
                if (ab != CardDef.ActiveAbilityObject && ab != CardDef.AlternativeAbilityObject) { badCount++; continue; }
            }
            else if (ab < 0) { badCount++; continue; }
            if (lg < 0) { badCount++; continue; }
            if (!r.TryGetProperty("criteria", out var cr) || cr.ValueKind != JsonValueKind.Object) { badCount++; continue; }
            var cs = ParseCountSet(cr, out int bad);
            badCount += bad;
            if (cs == null) { badCount++; continue; }
            list.Add(new CardDef.TargetCriteriaRowSpec { Ability = ab, Trigger = trigger, Logic = lg, Criteria = cs });
        }
        return list;
    }

    static void Dump(string line, string tag = "SEG", CardDef card = null)
    {
        // 🔴 **2026-10-10 `A1339`**：`seg` 这条入口原来**也不建卡名索引**（它只调 `EffectText.Parse`，
        //   不经过任何装池动作）⇒ 手搓一句含**卡名**的卡面原文时会静默报 `target=[]`，
        //   和 `card`/`scan`/`check` 一样是**与引擎不同口径**。⇒ 统一走这一道闸（`LoadPool` 幂等）。
        LoadPool();
        // 🔴 🆕 **2026-10-19（`A1960` 收口硬前置）**：`card` 非空时**必须传进 `Parse`** ——
        //   `A1960` 的 `choosecard` 注入（`EffectText.InjectChoicesDefinitionOp`）只在
        //   `Parse(…, card:)` 这条路上发生；不传 ⇒ `card <名>` 的 `parse` 行会把
        //   「引擎会开面板」的卡显示成「没有那条 op」（同 `A1339` 那族：探针与引擎口径不同）。
        //   `seg`（手搓一句、没有对应卡）传 null —— 它本来就不知道是哪张卡。
        var ops = EffectText.Parse(line, out var unparsed, out var partial, false, card: card);
        Console.WriteLine(tag + " = [" + line + "]");
        Console.WriteLine("  unparsed=" + (unparsed == null ? "-" : string.Join(" | ", unparsed))
                          + " partial=" + (partial == null ? "-" : string.Join(" | ", partial)));
        if (ops == null) { Console.WriteLine("  (null)"); return; }
        foreach (var op in ops)
        {
            Console.WriteLine("  op: verb=" + op.Verb
                + " amount=" + op.Amount
                + " payload=[" + op.Payload + "]"
                + " target=[" + (op.Target == null ? "" : op.Target.Raw) + "]"
                + " tail=[" + (op.Tail ?? "") + "]"
                + " cost=" + op.Cost + " ck=" + op.CostKind
                // 🔴 **2026-10-10 `A1368`**：这三样原来也不打 ⇒ 「付费怎么分账 / 条件认成什么 /
                //   `for each` 数谁」在**手搓一句**时同样看不见。与 `scan` 那四列**同一批**，
                //   两个入口打印的是**同一个值**（`CountRefText` 是唯一渲染口）。
                + " shared=" + (op.CostShared ? "1" : "0")
                + " condKind=[" + (op.ConditionKind ?? "") + "]"
                + " countRef=[" + CountRefText(op) + "]"
                // 🔴 **2026-10-10 `A1374`**：`scan` 又补了三列（`CostKind` / `CountScope` / `Condition`）
                //   ⇒ 这里也把**同一个值**打全，免得两个入口再出现「一个打、一个不打」的口径差
                //   （`A1374` 的病灶就是 `ck=` 只在这边打）。`null` → 空串，与上面同一规则。
                + " countScope=[" + (op.CountScope ?? "") + "]"
                + " cond=[" + (op.Condition ?? "") + "]"
                // 🔴 **2026-10-11 `A1477`**：目标规格打全（`scan` 那边打的是**同一个** `TargetText`）。
                //   `target=[…]` 那一格**保持原样不动**（外面有很多报告按它 grep），
                //   这里追加的是**整块**（含 `Raw`/`Count`/`Side`/`Kind`/相邻/滤镜/代词… 见 `TargetCols`）。
                + " T=" + TargetText(op.Target)
                + (op.DeathWatchTarget == null ? "" : " DW=" + TargetText(op.DeathWatchTarget, "DW"))
                // 🔴 **2026-10-11（`A1462`）**：第二目标（`Target2`）与**嵌套 op** 也补上 ——
                //   上面 `T=` 打的是 `op.Target`，而 `forceattack` 那一族**被打的那个**在 `Target2`；
                //   `atturn`（`At the start of each turn …`）那一族则把正文解析在 `AtTurnOps` 里，
                //   两个入口原来**一个字都不打** ⇒ 「`non <关键词>` 的目标筛有没有被承载」看不出来。
                //   ⚠️ 与 `scan` 那边**共用** `TargetBlock` / `TargetText`（本仓红线：同一条规则只写一处）。
                + (op.Target2 == null ? "" : " T2=" + TargetText(op.Target2, "T2"))
                + NestedText(op)
                + " chooseSrc=" + op.ChooseSrc + " what=[" + op.ChooseWhat + "] act=[" + op.ChooseAct + "]"
                + " deadScope=" + op.ChooseDeadScope
                // 🔴 **`A1568①`（`A1590②`）**：`Source` 原来这一条入口**一个字都不打**
                //   （`scan` 那条路也一样）⇒ 改 `Source` 的活两个入口都看不见它。
                //   `scan` 那边打的是**同一个值**的块形（`Program.SourceText`，`/{SRC:…}`）；
                //   这里按本入口的标签风格打成 `Source=[…]`（`null` → 空串，与相邻 `cond=[…]` 同一规则）。
                //   ⚠️ **标签必须是 `Source=` 而不是 `src=`** —— 本入口上面那条 `ASK …` 行的
                //     `src=` 打的是 **`ChooseSrc`（另一个字段）**，两个同名会静默看错。
                + " Source=[" + (op.Source ?? "") + "]"
                // 🔴 **`A1561①`（2026-10-19 波 4）：其余公开字段一块打全**（判据/取舍见 `ExtraFields`）。
                //   ⛔ 不动上面任何既有列 —— 本块只追加在行尾（同 `A1368`/`A1374`/`A1477` 的「只追加」形状）。
                + ExtraFields(op));
        }
    }

    /// <summary>🔴 `A1368`：`EffectOp.CountRef` 在 dump 里的**唯一**渲染口（`Scan` 的 ops 列与
    /// `Dump` 共用 —— 本仓红线「两处写同一条规则 = 迟早不一致」）。
    /// **原样返回**（只把 `null` 归一成空串）：`board` 族的取值自带两个 `|`（`enemy|troop|all`），
    /// ⛔ **不许转义/替换** —— 那会让 dump 的值与引擎字段不同，下一个人照 dump 抄进断言就抄错了。
    /// `scan` 那边靠「只认前两个 `|`」的 `CheckMain` 兜住（见 `Scan.cs` 文件头）。</summary>
    public static string CountRefText(EffectOp op) { return op.CountRef ?? ""; }

    /// <summary>🔴 **`A1561①`（2026-10-19 波 4）：`EffectOp` 其余公开字段的唯一渲染口**（只给 `Dump` 用）。
    ///
    /// **判据（本笔定的「该打哪些」—— 原账说这条判据本身没有出处，这里把它写死）**：
    ///   ① **凡 `EffectOp` 的公开字段，只要由解析器写入、且结算层会读，就必须在 `Dump` 里可见** ——
    ///      ⛔ 不再按「老 15 列」那张名单挑；新加字段时**照这条判据补**，而不是等下一次被账点到。
    ///   ② **已被共享渲染口覆盖的不重复打**：`T=` / `T2=` / `DW=`（`EffectTargetSpec` 的 28 列，
    ///      见 <see cref="TargetCols"/>）、`/{AM:…}` / `/{TAIL:…}` / `/{SRC:…}`（`TargetBlock`，
    ///      `scan` 用的是同一份）、`atTurn(...)` / `repeat(...)` / `base(...)`（`NestedText` 打嵌套 op 本体）。
    ///   ③ **只追加**：本块排在既有 `op:` 行的**末尾**，既有列一字不动（同 `A1368`/`A1374`/`A1477`）。
    ///   ⚠️ `scan` **不**打本块（它只打 `TargetBlock`）⇒ **改这里不会动 `check` 的基线**；
    ///      反过来说，`scan` 仍看不见 `Instead` / `AltAmount` 这类「顶层 op 自己的布尔/替换值」——
    ///      其中 `Tail` 已由 `A1877②` 补进 `scan`，`ChooseDeadScope` 也已于 **2026-10-19（`A1568②`）**
    ///      补进 `TargetBlock`（`/{CDS:…}`）⇒ 这一族**两个入口都看得见了**（只剩本段列出的那几项）。
    /// ⚠️ `null` → 空串、布尔 → `0/1`（与 `shared=` 同一条口径）；**原样打、不转义**（同全仓既有取舍）。
    /// </summary>
    public static string ExtraFields(EffectOp op)
    {
        if (op == null) return " ext={}";
        var parts = new List<string>();
        void S(string k, string v) { parts.Add(k + "=" + (v ?? "")); }
        void B(string k, bool v) { parts.Add(k + "=" + (v ? "1" : "0")); }
        void N(string k, int v) { parts.Add(k + "=" + v.ToString(System.Globalization.CultureInfo.InvariantCulture)); }

        string Crit(CardCriteria c)
        {
            if (c == null) return "null";
            var xs = new List<string>();
            if (!string.IsNullOrEmpty(c.KindWord)) xs.Add("kind=" + c.KindWord);
            if (c.KindAnyOf != null && c.KindAnyOf.Count > 0) xs.Add("any=" + string.Join(",", c.KindAnyOf));
            if (!string.IsNullOrEmpty(c.Keyword)) xs.Add("trait=" + c.Keyword);
            if (!string.IsNullOrEmpty(c.Name)) xs.Add("name=" + c.Name);
            return xs.Count == 0 ? "{}" : "{" + string.Join(";", xs) + "}";
        }

        string When(WhenEvent w)
        {
            if (w == null) return "null";
            var xs = new List<string>();
            xs.Add("kind=" + (w.Kind ?? ""));
            xs.Add("ownerIs=" + w.OwnerIs.ToString(System.Globalization.CultureInfo.InvariantCulture));
            xs.Add("selfOnly=" + (w.SelfOnly ? "1" : "0"));
            xs.Add("notEphemeral=" + (w.NotEphemeral ? "1" : "0"));
            xs.Add("actorSelf=" + (w.ActorSelf ? "1" : "0"));
            xs.Add("targetOwnerIs=" + w.TargetOwnerIs.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (w.TargetCriteria != null) xs.Add("targetCriteria=" + Crit(w.TargetCriteria));
            if (w.Criteria != null) xs.Add("criteria=" + Crit(w.Criteria));
            xs.Add("raw=[" + (w.Raw ?? "") + "]");
            return "{" + string.Join("|", xs) + "}";
        }

        // —— 数值 / 区间 / 替换值 ——
        N("am", op.AmountMax);
        N("costSetTo", op.CostSetTo);
        S("amountRef", op.AmountRef);
        B("instead", op.Instead);
        N("altAmount", op.AltAmount);
        S("altCond", op.AltCondition);
        S("altCondKind", op.AltConditionKind);
        S("altPayload", op.AltPayload);
        // —— 部署那一族 ——
        S("dest", op.Dest);
        S("deployFrom", op.DeployFrom);
        B("eachPlayer", op.EachPlayer);
        N("costMin", op.CostMin);
        N("costMax", op.CostMax);
        B("random", op.Random);
        B("upTo", op.UpTo);
        // —— 降费那一族 ——
        B("pickOne", op.PickOne);
        B("nextOnly", op.NextOnly);
        // —— 选牌动作的参数 ——
        N("chooseCostDelta", op.ChooseCostDelta);
        N("chooseCopies", op.ChooseCopies);
        B("randomPick", op.RandomPick);
        // —— 时长 / 常驻 / 筛选 ——
        S("dur", op.Duration);
        S("atTurnPhase", op.AtTurnPhase);
        S("filter", Crit(op.Filter));
        S("subject", Crit(op.Subject));
        B("otherThanSelf", op.OtherThanSelf);
        S("when", When(op.When));
        // —— for each / repeat ——
        N("perCount", op.PerCount);
        N("repeatTimes", op.RepeatTimes);
        // 🆕 2026-10-24（引擎线 `A12`）：原版 `AbilityLogic.furtherMultiplier`（`PlayAbility.c:235-236`）。
        //   判据同本方法 doc ① ：「由解析器写入、且结算层会读」⇒ 必须在 `Dump` 里可见。
        //   `-1` = **未知**（没挂上/旧表），`0` = 原版明确「不乘」。
        N("fm", op.AbilityFurtherMultiplier);
        // 🆕 2026-10-24（引擎线 `A12` 顺手发现）：`SubjectTargetsAffected`（`B-17` 的 `targetsAffected`）
        //   到本轮为止 **`Dump` / `scan` 两个入口都不打** —— 而它是「由解析器写入、结算层会读」的字段
        //   （判据同本方法 doc ①）⇒ 补上，否则「归位有没有生效」在探针里**没有任何仪器可看**。
        N("subjTa", op.SubjectTargetsAffected);

        return " ext={" + string.Join("|", parts) + "}";
    }

    // ==================================================================================
    //  `A1477`：**目标（`EffectTargetSpec`）在 dump 里的唯一渲染口**
    // ==================================================================================
    //
    // 🔴 **病灶**（台账 `A1477`）：`scan` 的 dump **一个字都不打 `Target`**
    //   —— 只有 `verb/chooseSrc/chooseWhat/chooseAct/amount/payload` ＋ 尾部七列（`A1368`/`A1374`）。
    //   而 **`A1467` 改的正是「目标怎么解」**（`T=[enemies]…n=1` → `T=[all enemies]…n=0`，
    //   `All enemies lose …` 句首那个 `All ` 被正则吃掉）⇒ **`check` 一行都没多**、
    //   「解析差异 0 行」照旧 ⇒ **那类改动会静默过关**。`A1467` 写手只能**自建**一份带 `Target.*`
    //   的签名 dump 才拿到读数（`D:/tmp/wf_a1467/sig_desc_{before,after}.txt`）。
    //   ⚠️ 与 `A1459`（`willrun` 补「无头」档）、`A1466`（`ruleprobe.sh` 拿旧 dll 跑）**同一条链** ——
    //     「**探针看不见哪一类改动**」这件事本身还有缝。
    //
    // **做法**：往每条 op 的**尾部再追加**一个目标块（仍然是「只追加、⛔ 不插在中间」的形状，
    //   见 `Scan.cs` 文件头）：`/{T:Raw=all enemies|Side=enemy|…|PickMost=}`
    //   · `Target == null` ⇒ `/{T:null}`（⛔ 别打成空 —— 「没有目标短语」本身就是要看的读数）
    //   · `EffectOp.DeathWatchTarget != null` 时再跟一个 `/{DW:…}`（同一族的**第二个**目标规格；
    //     原来同样一个字都不打，一并补上 ⇒ 不留第二道缝）
    //
    // 🔴 **为什么用「清单 ＋ 机器级守卫」而不是纯反射自动列**：
    //   · 纯反射（`GetFields()` 自动列）**列名会随声明顺序漂** ⇒ 基线里那一大块会无意义地全变，
    //     把「红了不用管」训练成习惯（`CheckMain` 头注那条教训）。
    //   · 手写清单**会腐烂** —— `EffectTargetSpec` 是**别人在改**的类（`A1311` 的
    //     `PrevSide`/`ResolvedSide` 就是后来加的），新字段**不会自己长进清单里**，那就是新的盲区。
    //   ⇒ 两者合起来：**列名与顺序写死**（基线稳定）＋ `TargetColsDrift()` 每次 `check` 现算一遍
    //     「类里有没有清单外的字段」（`Sentinel` 第 ④ 条，形状照 `A1348` 那三条）。
    //
    // ⚠️ **取尾部列必须先摘掉这个块** —— `Sentinel.Legacy()` 按 `/{T:` 切。块里**可以有任意字符**
    //   （`Raw` 是卡面原文），所以**不许**从后往前数字段（块内部自带 `/` 时会**静默拿错列**）。
    // ⚠️ **不做转义**（同 `countRef`/`condition` 那条取舍）：照 dump 抄进断言的值必须与引擎字段**逐字相同**。

    /// <summary>目标块里**该有哪些字段**（顺序 = dump 里的顺序）。
    /// 🔴 **加字段的地方在 `EffectTargetSpec`**（`RuleEngine/Core/EffectText.cs`）—— 那边加了字段、
    ///   这里没跟上 ⇒ `check` 的哨兵 ④ 会报 `✗`（`TargetColsDrift()`）。⛔ 别只改一边。</summary>
    public static readonly string[] TargetCols = {
        "Raw", "Side", "Kind", "Count",                       // ← `A1467` 改的两样（Raw / Count）在这
        "Random", "Auto", "Subjectless",
        "Adjacent", "Anchor", "AnchorInSet", "AdjacentAll", "AdjacentFailed",
        "Each", "KindUnfilterable", "SubtypeFilter",
        "Deployed", "AttackedBySelf", "KeywordFilter", "NotKeyword",
        "AlsoHand", "HandOnly", "PrevAntecedent", "PrevSide", "ResolvedSide",
        "NameFilter", "DamagedOnly", "PrayedOnly", "PickMost",
    };

    /// <summary>一个目标规格 ⇒ `{T:Raw=…|Side=…|…}`（`tag` 用来区分 `Target` 与 `DeathWatchTarget`）。
    /// ⚠️ 两个入口（`Scan` 的 ops 列、`Dump` 的 `seg`/`card`）打的是**同一个值** ——
    ///   本仓红线「两处写同一条规则 = 迟早不一致」（`A1374` 的病灶就是 `ck=` 只在一个入口打）。</summary>
    public static string TargetText(EffectTargetSpec t, string tag = "T")
    {
        if (t == null) return "{" + tag + ":null}";
        var sb = new System.Text.StringBuilder();
        foreach (var name in TargetCols)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(name).Append('=').Append(TargetValue(t, name));
        }
        return "{" + tag + ":" + sb.ToString() + "}";
    }

    /// <summary>要追加到 op 尾部的**整块**：`/` ＋ `{T:…}`（＋ `DeathWatchTarget` 非空时的 `/{DW:…}`
    /// ＋ `Target2` 非空时的 `/{T2:…}` ＋ `AmountMax > 0` 时的 `/{AM:…}`
    /// ＋ `Tail` 非空时的 `/{TAIL:…}` ＋ `ChooseDeadScope` 非空时的 `/{CDS:…}`
    /// ＋ **恒有的 `/{SRC:…}`**，见 <see cref="SourceText"/>）。
    /// `Sentinel.Legacy` 按其中的 `/{T:` 把 legacy 那一段切出来（⇒ 后面那几个块一起被摘掉）。</summary>
    public static string TargetBlock(EffectOp op)
    {
        var s = "/" + TargetText(op.Target);
        if (op.DeathWatchTarget != null) s += "/" + TargetText(op.DeathWatchTarget, "DW");
        // 🔴 **2026-10-11（第二十会话 · `A1462`）：第二目标（`EffectOp.Target2`）也补上。**
        //   病灶：「`non <关键词>` 的目标筛选**疑似整个丢了**」这条账**判不了**，
        //   原因就是**这一栏一个字都不打** —— `A1477`（同一天）补的是 `op.Target`，
        //   而 `forceattack` 那一族把**被打的那个**放在 `Target2` 里（`Target` 是攻击者，可为空）
        //   ⇒ `SW5 Hunting Wolf` 的 `Ferocity: Attack a random non Flying enemy troop`
        //   在 dump 里**看起来**是 `T={T:null}`（探针把它整个藏起来了）。
        //   ⚠️ **块名必须是 `/{T2:`**：`Sentinel.Legacy` 靠 `LastIndexOf("/{T:")` 找起点，
        //      `/{T2:` 里**不含** `/{T:`（`T` 后面那一颗是 `2` 不是 `:`）⇒ 切点不会挪。
        //   ⚠️ **只在非空时打**（与 `DW` 同一条取舍）—— 这样「只追加」这条自证形状还在。
        //      🔴 **自证判据要写准**（⛔ 别写成「旧行是新行的前缀」—— 块是插在 op 分隔符 `;` **之前**的，
        //      所以 `;` 的位置会往后挪）：**把新行里的 `/{T2:…}` 块删掉之后，必须与旧行【逐字节】相同**。
        //      本轮实测：扫描前后 8 个变化行**全部**满足（脚本：`re.sub(r'/\{T2:[^;]*', '', 新行) == 旧行`）。
        if (op.Target2 != null) s += "/" + TargetText(op.Target2, "T2");
        // 🔴 **2026-10-14（第二十八会话 · `W16` · 台账 `A1877②`）：补 `AmountMax` 与 `Tail` 两块。**
        //   病灶（`资料/普查产出_第二十八会话/A1759_逐条对账.md` §查不清最后一行）：
        //     `scan` 的 op **只打 `Amount`** ⇒ 原版那批**区间值**（`deals 1-2 damage`）在对账里
        //     **只比得上下界**、上界整个看不见（`EffectOp.AmountMax` 是**另一个字段**，
        //     `EffectText.cs:76`；消费点 `EffectResolver.cs:1817/2238/2267` 的 `Rng.Next(Amount, AmountMax+1)`）；
        //     而 `Tail`（`EffectOp.Tail`，`EffectText.cs:119` —— `… and stun it` 那半句 **就存在这里**）
        //     **三个 dump 入口一个都不打** ⇒ 「区间伤害」与「…and stun it」两族**没有仪器可证**。
        //   ⚠️ 形状**照 `/{DW:…}` / `/{T2:…}` 那一条**（本仓既有做法）：
        //     · **只在非默认值时打**（`AmountMax > 0` 才有区间；`Tail` 空 = 没有那半句）——
        //       这样「只追加」这条自证形状还在：**把这两块删掉之后必须与旧行逐字节相同**；
        //     · 排位**在 `/{T:…}` 之后、`/{SRC:…}` 之前** —— `Sentinel.Legacy()` 按 `/{T:` 切，
        //       切点前面的一切都被摘掉 ⇒ **尾部七列的位置一个都不动**（`Tail()` 不受影响）；
        //       ⚠️ 而 `/{SRC:…}` 必须**仍然排在最末尾**：哨兵 ⑧ 取 `o.Substring(k+6)` 再
        //       `EndsWith("}")` —— 排在它后面会让那一格把本块也吞进去。
        //     · **原样打、不转义**（同 `countRef`/`condition`/`Source`）：照 dump 抄进断言的值
        //       必须与引擎字段**逐字相同**。⚠️ `Tail` 是**卡面原文**，自带 `;` 时那一行会被
        //       `Ops()` 多切一刀（与 `Source` 同一条已知形状，哨兵 ⑧ 那一栏会如实报出来）。
        if (op.AmountMax > 0) s += "/{AM:" + op.AmountMax + "}";
        if (!string.IsNullOrEmpty(op.Tail)) s += "/{TAIL:" + op.Tail + "}";
        // 🔴 🆕 **2026-10-19（`A1568②` 剩下那半条 · 收口批）**：`ChooseDeadScope` 补进 dump。
        //   病灶：`A1877②` 把 `Tail` 补进 `scan` 之后，**同族只剩它一个看不见** ——
        //     `Dump`（`seg`/`card`）一直打 `deadScope=`，而 `scan` 一个字都不打 ⇒
        //     改「死域范围」的活 `check` 一行都不多。
        //   🔴 **2026-10-19 第五十五会话现测订正**：现池用到它的**不是 3 张而是 5 张** ——
        //     `GSC44` / `SAU39` / `UM61`（值 `all`）＋ `EC39` / `SOR42`（值 `since_last_turn`，
        //     `EffectText.cs:5032-5036` 那一族之外的第二个取值）。补本块实测**正好这 5 行变长**。
        //   ⚠️ **排位**：在 `/{TAIL:…}` 之后、`/{SRC:…}` **之前** —— `Sentinel.Legacy` 按 `/{T:` 切，
        //     尾部七列位置一个都不动；`/{SRC:…}` 必须仍然**排在最末尾**（哨兵 ⑧ 靠它收尾）。
        //   ⚠️ **只在非空时打**（同 `/{DW:}` / `/{T2:}` / `/{AM:}` / `/{TAIL:}` 那条取舍）：
        //     把本块删掉之后，新行必须与旧行**逐字节**相同。
        //   ⚠️ **原样打、不转义**（同 `countRef` / `condition` / `Source` / `Tail`）。
        if (!string.IsNullOrEmpty(op.ChooseDeadScope)) s += "/{CDS:" + op.ChooseDeadScope + "}";
        s += SourceText(op);
        return s;
    }

    /// <summary>🔴 **`A1568①`（`A1590②`）：`EffectOp.Source` 在 dump 里的【唯一】渲染口** ——
    /// `/{SRC:…}`（`Source == null` ⇒ `/{SRC:}`，⛔ 不写成「不打」）。
    ///
    /// **病灶**（台账 `A1568①`）：`ruleprobe` 的三个 dump 入口（`scan` / `seg` / `card`）
    ///   **一个都不打 `Source`**、基线里也没有 ⇒ **凡改 `Source` 的活 `check` 一行都不多**
    ///   （`A1439` 那条「两条路 `Source` 不是同一个东西」**今天没仪器可证**，只能按代码读）。
    ///   ⚠️ 与本文件里 `Target`（`A1477`）、`Target2`（`A1462`）是**同一条链**：
    ///     「**探针看不见哪一类改动**」这件事本身还有缝。
    /// · **原样打、不转义**（照 dump 抄进断言的值必须与引擎字段逐字相同 —— 同 `countRef` / `condition` / `Raw`）。
    /// · ⛔ **永远打**（不像 `/{DW:}` / `/{T2:}` 那样只在非空时打）：`Source` 从「有」变「没有」
    ///   本身就是要看的读数；而且哨兵 ⑧ 就是靠 `/{SRC:` 这个记号判「**这一列还在不在**」。
    /// · ⚠️ `Sentinel.Legacy()` 会**连同本块一起摘掉**（它按 `/{T:` 切）⇒ `Tail()` 的行列位置不受影响。
    /// </summary>
    public static string SourceText(EffectOp op)
    {
        return "/{SRC:" + (op.Source ?? "") + "}";
    }

    /// <summary>🔴 **2026-10-11（第二十会话 · `A1462`）：嵌套 op 的唯一渲染口** ——
    /// `atturn` 的 `AtTurnOps` · `repeat` 的 `RepeatOps` · `paidmod` 的 `BaseOps`。
    ///
    /// **为什么必须有**：`At the start of each turn attack a random non Flying enemy`（`GOF86`）
    /// 这句话的**目标规格不在顶层 op 上** —— 顶层只有一条 `atturn`（`Target = null`），
    /// 真正的正文解析在 `AtTurnOps[0].Target2` 里 ⇒ 顶层那几栏**看不出目标被解析成什么**
    /// （`A1462` 那条账「判不了」的第二个原因）。
    ///
    /// ⚠️ 只打**一层**（嵌套的嵌套不再展开）—— 本池实测没有第二层；
    ///   要真出现，下面会如实打出 `(还有嵌套)` 而不是静默截断。
    /// 🆕 **`A1568①`（`A1590②`）**：这一层里的 op 也补上了 `SRC=[…]` —— 原来「顶层打了、
    ///   嵌在里面的没打」= 同一条缝只堵一半。⚠️ 只出现在 `seg`/`card` 两个入口（`scan` 不打嵌套）。
    /// ⚠️ 与 `TargetBlock` 一样：**只追加、不打转义**（照 dump 抄进断言的值必须与引擎字段逐字相同）。</summary>
    static string NestedText(EffectOp op)
    {
        var sb = new System.Text.StringBuilder();
        Append("atTurn(" + op.AtTurnPhase + ")", op.AtTurnOps, sb);
        Append("repeat", op.RepeatOps, sb);
        Append("base", op.BaseOps, sb);
        return sb.ToString();

        void Append(string label, List<EffectOp> list, System.Text.StringBuilder outSb)
        {
            if (list == null || list.Count == 0) return;
            outSb.Append(" ").Append(label).Append("=[");
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (i > 0) outSb.Append(" ｜ ");
                outSb.Append(o.Verb)
                     .Append(o.Amount != 0 ? " n=" + o.Amount : "")
                     .Append(string.IsNullOrEmpty(o.Payload) ? "" : " [" + o.Payload + "]")
                     .Append(" T=").Append(TargetText(o.Target));
                if (o.Target2 != null) outSb.Append(" T2=").Append(TargetText(o.Target2, "T2"));
                // 🔴 **`A1568①`（`A1590②`）**：**嵌在里面的 op** 也有 `Source`，原来同样一个字都不打
                //   ⇒ 上一层打了、这一层没打 = 同一条缝只堵了一半（`A1568` 的病灶）。
                outSb.Append(" Source=[").Append(o.Source ?? "").Append("]");
                if (o.AtTurnOps != null && o.AtTurnOps.Count > 0) outSb.Append("(还有嵌套)");
            }
            outSb.Append("]");
        }
    }

    /// <summary>取一个字段的值。`null` → `-`（与「空串」区分开：`EffectTargetSpec` 里
    /// `null` 与 `""` **语义不同** —— 见 `PrevSide` 的「判不出就留 null、⛔ 不许拿「大概」顶」）。
    /// `bool` → `1`/`0`（与 `shared` 那一列同规则）；枚举 → 名字（`Anchor=Unset` 这种）；
    /// 其它对象 → 它自己的 `ToString()`（`CardCriteria` 有 `override`）。</summary>
    static string TargetValue(EffectTargetSpec t, string name)
    {
        var ty = typeof(EffectTargetSpec);
        var mi = (MemberInfo)ty.GetField(name) ?? ty.GetProperty(name);
        if (mi == null) return "?";                       // 清单过期（哨兵 ④ 会报出来）
        object v = mi is FieldInfo f ? f.GetValue(t) : ((PropertyInfo)mi).GetValue(t);
        if (v == null) return "-";
        if (v is bool b) return b ? "1" : "0";
        if (v is string s) return s;
        var to = v.ToString();
        return string.IsNullOrEmpty(to) ? "-" : to;
    }

    /// <summary>🔴 `A1477` 的**机器级守卫**（形状照 `Sentinel`）：返回**两边对不上**的名字，空 = 一致。
    ///   ① **类里有、`TargetCols` 里没有** ⇒ **新字段没跟进 = 新的盲区**（这正是 `A1477` 要治的缝）；
    ///   ② **清单里有、类里没有** ⇒ 清单过期 ⇒ 那一列恒打 `?`、看着像「没值」。
    /// ⛔ 只查公开实例成员（`EffectTargetSpec` 没有基类字段）。</summary>
    public static List<string> TargetColsDrift()
    {
        var drift = new List<string>();
        var ty = typeof(EffectTargetSpec);
        var inType = new List<string>();
        foreach (var f in ty.GetFields(BindingFlags.Public | BindingFlags.Instance)) inType.Add(f.Name);
        foreach (var p in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanRead && p.GetIndexParameters().Length == 0) inType.Add(p.Name);
        foreach (var n in inType) if (Array.IndexOf(TargetCols, n) < 0) drift.Add("类里有、清单没有: " + n);
        foreach (var n in TargetCols) if (!inType.Contains(n)) drift.Add("清单里有、类里没有: " + n);
        return drift;
    }

    /// <summary>🔴 **2026-10-10 `A1349`：`ParseSegment` 的【唯一入口闸】** —— 先过 `LoadPool`
    /// （＝建卡名索引）再解析。
    ///
    /// **病灶**：`BattleProbe.cs` 里原来有**三处**直调 `EffectText.ParseSegment`（硬编码字面量）
    /// （`BattleProbe19.Run` 两处 · `B19Test.Run` 一处）—— 它们**不过 `LoadPool`**，
    /// 是 `A1339` 那族的**第四种**解析入口（前三种：`card`/`seg` 走 `Dump`、`scan`/`check` 自己读 JSON）。
    /// ⚠️ 今天那三句**句子里没有卡名** ⇒ **今天无影响**；但**哪天有人塞一句带卡名的进去**，
    ///   就会重演 `A1339` 那条假象（「探针报 `target=[]`、引擎有目标」）——
    ///   而那正是**最贵**的一种错：它看起来像「引擎在这句话上不认卡名」。
    ///
    /// ⇒ **收口方式** = 让它们**走这条闸**，⛔ 不许再直调 `EffectText.ParseSegment`。
    ///   **判据**：`grep -rn "EffectText\.ParseSegment" 工具/ruleprobe/` 只该命中**本文件这一行**。
    ///   （`LoadPool` 幂等 ⇒ 重复过闸零成本；`BattleProbe` 的调用点本来就由 `Main` 先装过池。）</summary>
    public static EffectText.SegResult ParseSeg(string seg)
    {
        LoadPool();
        return EffectText.ParseSegment(seg);
    }

    static void Card(string name)
    {
        LoadPool();
        var hits = Pool.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (hits.Count == 0) { Console.WriteLine("!! no card named " + name); return; }
        foreach (var c in hits)
        {
            Console.WriteLine("CARD " + c.Name + " id=" + c.Id + " type=" + c.Type + " subtype=" + c.Subtype
                              + " rarity=" + c.Rarity + " faction=" + c.Faction + " cost=" + c.Cost
                              + " kw=[" + string.Join(",", c.Keywords.Keys) + "]");
            Console.WriteLine("  desc = " + c.Desc);
            var ops = PlayerChooseOpsOf(c);
            foreach (var op in ops)
                Console.WriteLine("  ASK verb=" + op.Verb + " src=" + op.ChooseSrc + " what=[" + op.ChooseWhat
                                  + "] act=[" + op.ChooseAct + "] payload=[" + op.Payload + "]"
                                  // 🔴 **`A1568①`（`A1590②`）**：这一行原来是**唯一**能看见
                                  //   「**`CardDef` 收集层**那一份 op」的地方（`PlayerChooseOps` 走的是收集层，
                                  //   ⛔ 不是 `EffectText.Parse`）—— 而它**不打 `Source`** ⇒ `A1439` 那条
                                  //   「两条路 `Source` 不是同一个东西」**没有仪器可看**。现在补上（原样打）。
                                  //   ⚠️ 标签是 `Source=`，⛔ 别与本行前面那个 `src=`（= `ChooseSrc`）混。
                                  + " Source=[" + (op.Source ?? "") + "]");
            // 🆕 2026-10-19：**把卡本身传进去** —— 否则 `A1960` 那条注入的 `choosecard`
            //   在这一行（`parse`）里看不见，而上面 `ASK` 行（走 `RuleCore.PlayerChooseOps(c)`）看得见
            //   ⇒ 同一次 `card` 输出的上下两段**自己打架**（下段「没有 choose」、上段「有 ASK」）。
            Dump(c.Desc, "  parse", c);
        }
    }

    static List<EffectOp> PlayerChooseOpsOf(CardDef c)
    {
        return RuleCore.PlayerChooseOps(c);
    }

    /// <summary>🆕 2026-10-27（`B87_A3`）：`activeAbilityLogic` 的逐行整型数组（`ta` / `fm` / `ca` / `pa`）。
    /// <para>口径同 `CardDatabase.MapActiveAbilityLogic`：缺键 / 不是数组 ⇒ **`null`（未知）**；
    /// 单格不是整数 / `&lt; -1` ⇒ 该格落 **`-1`**（`0` 是合法值，⛔ 别拿它当缺值）。</para>
    /// <para>⚠️ 放在类尾（本文件没有按行号被账引用，但同一条纪律：新成员追加在末尾）。</para></summary>
    static int[] A3IntArray(JsonElement row, string key)
    {
        if (!row.TryGetProperty(key, out var el) || el.ValueKind != JsonValueKind.Array) return null;
        var outp = new int[el.GetArrayLength()];
        int i = 0;
        foreach (var cell in el.EnumerateArray())
        {
            if (cell.ValueKind != JsonValueKind.Number) { outp[i++] = -1; continue; }
            int v = cell.GetInt32();
            outp[i++] = v < -1 ? -1 : v;
        }
        return outp;
    }
}

/// <summary>
/// `check` 的**哨兵** —— **与基线无关**的机器级自证（🆕 2026-10-10 `A1348`）。
///
/// 🔴 **为什么需要**：`check` 的判据是「拿当前输出与基线比」，而**基线同一个工具就能重写**
///   （`scan 基线/out_baseline.txt` 一条命令就覆盖它）⇒ 谁红了顺手重跑一次 `scan`，
///   真回归就被**静默**抹平、界面还显示全绿（台账 `A1348` · 出处 `W5d` 顺手发现 2）。
///   `README.md` 那条 `:1024` 哨兵是**文档级**防呆 —— 文档拦不住一个正在乱敲命令的人。
///   ⇒ 把几条判据**写死在源码里**（`scan` 重写得了基线文件、重写不了 `.cs`），每次 `check` 都现算一遍。
///
/// ⚠️ **判据里的期望值必须有【原版出处】**（卡图 / 反编译 / 引擎字段语义）——
///   ⛔ **不许拿我们自己的 dump 反填**，那会变成「拿我们的实现证明我们的实现」（本仓红线）。
/// ⚠️ 每条都读**当次现算的 dump**（`newLines`），⛔ 不读基线 ⇒ 重生成基线抹不掉它们。
/// ⚠️ 哨兵卡**按 `id` 找、⛔ 不按卡名** —— 卡名是**会变的产物**（本仓已改过 6 张），
///   `scan` 的第 1 列就是 `id|name`，拿名字当锚点第一天就会漂。
/// ⚠️ 哨兵出问题**一律出声**（`✗` ＋ 现值 vs 期望），⛔ 不许静默放过 —— 本仓红线「不许静默失败」。
/// ⚠️ **哨兵集是【有意的极小集】**：宁少而准 —— 一条会经常「合理地」变红的哨兵，
///   会把「红了不用管」训练成习惯（`CheckMain` 头注那条教训）。要加之前先问：它的期望值有原版出处吗？
/// </summary>
class Sentinel
{
    /// <summary>一条 op = `verb/chooseSrc/chooseWhat/chooseAct/amount/payload` ＋ **尾部七列**：
    /// `cost/shared/condKind/countRef/costKind/countScope/condition`（见 `Scan.cs` 文件头）。
    /// 🔴 取尾部列**必须从后往前数** —— `payload` 里**可以**含 `/`（实测 `ASH56` 的
    /// `<i>shuriken</i> 2` 就是，那条 op 有 14 个字段）⇒ 从前往后数下标会**静默拿错列**。
    /// 🔴 **2026-10-11 `A1477`：数之前必须先摘掉尾部的【目标块】** —— 块里是**卡面原文**
    ///   （`Raw`），它自己**可以**含 `/` ⇒ 不摘的话「从后往前数」也会**静默拿错列**
    ///   （尾部又多出 28 个字段，位置全漂）。见 <see cref="Legacy"/>。</summary>
    static string Tail(string op, int fromEnd) { var f = Legacy(op).Split('/'); return f[f.Length - fromEnd]; }

    /// <summary>`A1477`：摘掉 op 尾部那个目标块（`/{T:…}` 及它后面的一切，含可能的 `/{DW:…}`）。
    /// 块**内容不做解析**（只当字符串比），所以这里只需要一个**起点标记** ——
    /// `/{T:` 在 legacy 段里不会出现（那一段的字段里 `Target` 只在块内被打印）。</summary>
    static string Legacy(string op)
    {
        int i = op.LastIndexOf("/{T:", StringComparison.Ordinal);
        return i < 0 ? op : op.Substring(0, i);
    }

    /// <summary>取某张卡那一行。返回 `[id, name, unparsed, partial, ops]`；`null` = 不在 dump 里。
    /// ⚠️ **只切 4 刀** —— ops 列里可能还有 `|`（`countRef` 的 `board` 族 / `chooseone` 的 `chooseWhat`）。</summary>
    static string[] Row(string[] lines, string id)
    {
        foreach (var l in lines)
        {
            if (l.Length == 0) continue;
            var p = l.Split(new[] { '|' }, 5);
            if (p.Length == 5 && p[0] == id) return p;
        }
        return null;
    }

    static List<string> Ops(string opsCol)
    {
        var r = new List<string>();
        foreach (var o in opsCol.Split(';')) if (o.Length != 0) r.Add(o);
        return r;
    }

    /// <summary>返回**失败条数**（`0` = 全过）。
    /// `baselinePath` 只有哨兵 ⑤（`A1382` 基线指纹）要用 —— 其余哨兵读的是**当次现算的 dump**。</summary>
    public static int Run(string[] newLines, string baselinePath = null)
    {
        int fail = 0;
        Console.WriteLine("=== 哨兵（与基线无关 · 重生成基线抹不掉）===");

        // ---------------------------------------------------------------- ① 卡名索引自证
        // 直接问引擎：索引建了没有。判据 = `CreatePool.cs:613`：「没建索引时 `MatchCardName`
        // **一律返回 null**」。它掉了的代价是**静默**的 —— 凡「目标靠**卡名**指」的句子一律退化成
        // `target=[]` ＋ 落 `partial` 列（`A1339`，已经产出过**两次假证据**）。
        Program.LoadPool();                      // 幂等 —— `scan` 那条路已经调过一次
        const string ProbeName = "Primaris Intercessor";   // 池里真实存在的卡名（`UM_Angels_of_Death` 的 desc 就指着它）
        var idxHit = CreatePool.MatchCardName(ProbeName);
        if (idxHit == null)
        {
            fail++;
            Console.WriteLine("  ✗ [索引] `CreatePool.MatchCardName(\"" + ProbeName + "\")` == null ⇒ 卡名索引**没建**。"
                              + "先看 `Program.LoadPool` 尾部那句 `CreatePool.BuildNameIndex(list)` 还在不在。");
        }
        else
            Console.WriteLine("  ✓ [索引] 卡名索引在（`" + ProbeName + "` → `" + idxHit + "`）");

        // ---------------------------------------------------------------- ② `scan` 那条路上索引真的生效了（数据级）
        // ⚠️ ① 只证明「库函数调用得通」，证明不了「**`scan` 自己读 JSON 那条路**也过了 `LoadPool`」
        //    （`Scan.ScanMain` 是**第二条**装池路径 —— `A1339` 的病灶正是「只补一处 = 静默偏一半」）。
        //    ⇒ 这条读**当次现算的 dump**：索引不在时，`UM64` 的第 3 句
        //    （`Codex: Give +1 [Ranged] to your Primaris Intercessor`）会**落进 `partial` 列**。
        //    这一段就是 `README.md` 那条 `:1024` 哨兵的**机器级**版本。
        // 🔴 **2026-10-14 就地换卡**：原来这里写的是 `UM_Angels_of_Death`（**自造 id**）——
        //    第二十六会话把全池 id 换成原版真值之后，这张卡的 id 变成 **`UM64`**
        //    （名字 `Angels of Death`，`desc` 逐字没变、仍是那句 `Codex: Give +1 [Ranged] …`）
        //    ⇒ 本哨兵当时**静默变成「哨兵卡不在 dump 里」那条红**（这正是它自己要抓的「哨兵要跟着换」）。
        //    判据 = 现读 `cards_engine.json`：`UM64` 的 desc 与旧哨兵要的那句一字不差。
        const string SentinelCard1 = "UM64";
        var r1 = Row(newLines, SentinelCard1);
        if (r1 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [索引·scan 路] 哨兵卡 `" + SentinelCard1 + "` **不在 dump 里** —— "
                              + "它被改名/删了、或 `desc` 变空了？哨兵要**跟着换**（⛔ 别静默放过，也别拿基线重生成糊过去）。");
        }
        else if (r1[3].Length != 0)
        {
            fail++;
            Console.WriteLine("  ✗ [索引·scan 路] `" + SentinelCard1 + "` 的 `partial` 列**非空** = `" + r1[3]
                              + "` ⇒ 卡名索引在 `scan` 那条路上**没生效**。两个可能，按序看："
                              + "① `Scan.ScanMain` 头那句 `Program.LoadPool()` 在不在；"
                              + "② `Program.LoadPool` 尾那句 `CreatePool.BuildNameIndex(list)` 在不在。");
        }
        else
            Console.WriteLine("  ✓ [索引·scan 路] `" + SentinelCard1 + "` 的 `partial` 列为空（卡名目标认出来了）");

        // ---------------------------------------------------------------- ③ 货币名（`A1357` 那一类）
        // 「**只改货币名**」（`energy` ↔ `faith`）在 `A1374` 补列**之前**是 `check` 的盲区
        // （⚠️ 而 `A1357` 正是这种改动，今天之前它被看见纯属侥幸 —— 动词也跟着变了）。
        // `SOR71` 的期望值出处 = **卡图**：`Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png`
        // 亲读是**金太阳**；`☀` = **信仰**、⛔ 不是能量（`EffectText.cs:7802-7812` 的 `MentionsFaith`，
        // 那里写着「`Contains("energy")` 失配 ⇒ 判不了 ⇒ 那半句永远不发生」）。
        // 我们 `desc` 原来错抄成 `Energy`（`cardface_fixes.json` 已按卡图改正）。
        // 🔴 **2026-10-14 就地换卡**：原来这里是 `SOR72`（换 id 之前那张 `Adelaide the Serene`）——
        //    换真值之后 `SOR72` 变成 **`Castigator`**（`Armour 1. When a friendly unit Prays…`，**没有付费 op**），
        //    而 `Adelaide the Serene` 现在是 **`SOR71`**（`6 ☀ Gain Flank and Shield. Rally: …`）
        //    ⇒ 旧写法让本哨兵报「`SOR72` 那行**没有付费 op 了**」那条**假红**。
        //    判据 = 现读 `cards_engine.json` + 卡图（卡名与那句 `6 ☀` 都对上 `SOR71`）。
        const string SentinelCard2 = "SOR71";
        var r2 = Row(newLines, SentinelCard2);
        if (r2 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [货币名] 哨兵卡 `" + SentinelCard2 + "` **不在 dump 里**（同 ②：改名/删卡？哨兵要跟着换）。");
        }
        else
        {
            var ops2 = Ops(r2[4]);
            // 取「付费前缀那条 op」= `cost` 列非 0 的那条（⛔ 不用下标 0：下标会被将来插进来的 op 挪走）
            string pay = null;
            foreach (var o in ops2) if (Tail(o, 7) != "0") { pay = o; break; }
            if (pay == null)
            {
                fail++;
                Console.WriteLine("  ✗ [货币名] `" + SentinelCard2 + "` 那行**没有付费 op 了**（`cost` 列全 0）"
                                  + " —— 卡面 `6 ☀ Gain Flank and Shield.` 的付费前缀掉了？");
            }
            else
            {
                string cost = Tail(pay, 7), ck = Tail(pay, 3);
                if (cost != "6" || ck != "faith")
                {
                    fail++;
                    Console.WriteLine("  ✗ [货币名] `" + SentinelCard2 + "` 付费那条 op 现在是 `cost=" + cost
                                      + "` `ck=" + ck + "`，期望 `cost=6` `ck=faith`"
                                      + "（判据 = 卡图 `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png`：`6 ☀`，`☀`=信仰）。"
                                      + "\n             现值 op = " + pay);
                }
                else
                    Console.WriteLine("  ✓ [货币名] `" + SentinelCard2 + "` 付费那条 op = `cost=6` `ck=faith`（照卡图）");
            }
        }

        // ---------------------------------------------------------------- ④ 目标列清单与类一致（`A1477`）
        // 🔴 这一条**不是**「目标解析对不对」（那要靠基线 diff），而是**「目标那几十个字段
        //   是不是全都进了 dump」** —— `A1477` 的病灶是「探针看不见 Target」，而它**会复发**：
        //    `EffectTargetSpec` 是**别人在改**的类（`A1311` 刚加过 `PrevSide`/`ResolvedSide`），
        //    新字段不会自己长进 `Program.TargetCols` 那份手写清单里。
        //    判据写死在源码里（重生成基线抹不掉），两边对不上就**出声**。
        var drift = Program.TargetColsDrift();
        if (drift.Count > 0)
        {
            fail++;
            Console.WriteLine("  ✗ [目标列] `EffectTargetSpec` 与 dump 的目标列清单**对不上**（" + drift.Count + " 处）："
                              + string.Join(" · ", drift)
                              + "\n             ⇒ 对不上的字段**在 `check` 里看不见**（= `A1477` 那条缝又开了）。"
                              + "修法：把 `Program.TargetCols` 按 `RuleEngine/Core/EffectText.cs` 的 `EffectTargetSpec` 补齐/删旧。");
        }
        else
            Console.WriteLine("  ✓ [目标列] 目标块与 `EffectTargetSpec` 一致（" + Program.TargetCols.Length + " 个字段全在 dump 里）");

        // ---------------------------------------------------------------- ⑥ `non <关键词>` 的排除（`A1462`）
        // 🔴 **2026-10-11（第二十会话）：这一条【现在就是红的】** —— 它是一条**已知缺陷**的钉子，
        //    ⛔ **不是本工具坏了**（判据与逐条读数 ⇒ 跑 `nonkw`；原版权威 ⇒ `FilterMethods__MeetsTraitCriteria.c`）。
        //    · 判据（**外部**，⛔ 不拿我们自己的实现反填）：卡面（PnP 成品卡图逐张亲读）
        //      `… attack a random non 〔Flying〕 Flying enemy` ⇒ 那个目标规格**必须排除飞行单位**；
        //      我方承载「排除」的**唯一**字段 = `EffectTargetSpec.NotKeyword`
        //      （消费点 `RuleCore.ApplyTargetFilters`）。
        //    · **它判的不是「与基线比」** —— 重生成基线抹不掉它（判据写在这里、读的是现解析结果）。
        //    · **修好之后它会自己变绿** ⇒ 那时把这条哨兵的说明当成「已修」记一笔即可（⛔ 别删它：
        //      删了就没人盯着这一类再犯）。⚠️ 新增同类卡面（又出现一处 `non X`）也会让它重新出声。
        //    · 为什么**必须计入 rc**：这一类缺口是**静默**的（句子报 OK、卡面不打 `*`），
        //      `check` 是唯一会天天跑到的地方；不计入 rc 就等于「记在报告里、没人看」。
        var nonkw = NonKwProbe.Measure(Program.PoolList, Program.PoolRawKeywords);
        if (nonkw.Lost > 0)
        {
            fail++;
            Console.WriteLine("  ✗ [non 排除] 卡面写 `non <关键词>` 而目标规格**没承载**的：**"
                              + nonkw.Lost + " 处**（共扫到 " + nonkw.Hits.Count + " 处）——");
            foreach (var h in nonkw.Hits.Where(x => !x.Carried))
                Console.WriteLine("              · `" + h.Card.Id + "` " + h.Card.Name + " · " + h.Path
                                  + "（" + h.Verb + "）卡面 `non " + h.Word + "` ⇒ `NotKeyword` 为空");
            Console.WriteLine("             🔴 **这是已知缺陷 `A1462`（不是本工具坏了）** —— 丢在【解析层】："
                              + "`RuleEngine/Core/EffectText.cs` 的 `ParseTarget` 里**没有** `non <词>` 这一支"
                              + "（它只认 `with <词>` ⇒ `ReTargetWith` → `KeywordFilter`）"
                              + "⇒ 那两个词整段丢掉，而句子照旧报 OK（静默）。");
            Console.WriteLine("             · 后果已量过（`nonkw` 的 §③）：随机挑中飞行单位 ⇒ 那一发**空过**"
                              + "（实得 0，而场上明明有可打的非飞行敌人）。");
            Console.WriteLine("             · 结算层**是好的**：把 `NotKeyword` 补上，候选里就没有飞行单位（`nonkw` §③(a)）。");
            Console.WriteLine("             ⇒ 修法 = 解析层加那一支；**修好这条哨兵会自动变绿**。");
        }
        else
            Console.WriteLine("  ✓ [non 排除] 卡面写了 `non <关键词>` 的 " + nonkw.Hits.Count
                              + " 处目标规格**都承载**了排除（`NotKeyword` 非空）");

        // ---------------------------------------------------------------- ⑤ 基线指纹（`A1382`）
        // 🔴 这一条治的是「**任何**真回归 ＋ 重生成基线 ⇒ 静默变绿」那条**一般情形**的洞 ——
        //    前四条只封了已知的几类。参照物 = **卡池文件**（另一个工具的作者，本探针只读）。
        //    判据与「为什么不许自动重钉」见 `Program.PinnedPoolMd5` 上面那段长注释。
        string poolMd5 = Program.Md5File(Program.PoolPath);
        string baseMd5 = string.IsNullOrEmpty(baselinePath) ? null : Program.Md5File(baselinePath);
        if (poolMd5 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [基线指纹] 卡池读不到（`" + Program.PoolPath + "`）⇒ **这一条判不了**"
                              + "（⛔ 不静默放过：判不了也要出声）。");
        }
        else if (baseMd5 == null)
        {
            fail++;
            Console.WriteLine("  ✗ [基线指纹] 基线读不到（`" + baselinePath + "`）⇒ **这一条判不了**（同上一行）。");
        }
        else if (poolMd5 != Program.PinnedPoolMd5)
        {
            // ⚠️ **不算失败**：卡池本身变了 ⇒ 两个指纹都该重钉（那是**别的工具**的账）。
            Console.WriteLine("  ⚠️ [基线指纹] 卡池的 md5 与钉住的**不同** ⇒ 本哨兵**此时不判**"
                              + "（⛔ 不是「过了」）。");
            Console.WriteLine("             现值 卡池 md5=" + poolMd5 + " · 基线 md5=" + baseMd5);
            Console.WriteLine("             ⇒ **三个指纹都该重钉**（🔴 `A1619` 那次重钉就漏了第三个）："
                              + "`PinnedPoolMd5` = 上面那个卡池现值 · `PinnedBaselineMd5` = 上面那个基线现值 ·"
                              + " `PinnedParseMd5` = 下面 ⑦ 打出来的那一行（**整行照抄**）。");
            // 🔴 2026-10-12（`A1382`）：这一支**曾经就是那条洞本身**（卡池天天在变 ⇒ ⑤ 天天「不判」）。
            //    现在**不是**了 —— ⑦ 那一半（**不依赖卡池、也不依赖基线文件**）照样在岗。
            Console.WriteLine("             🔴 这一支**不再等于「没人看」**：⑦ [输出指纹] 与本条**独立**"
                              + "（它既不读卡池、也不读基线文件）⇒ 「卡池变了 ＋ 重生成基线」这条假阴由 ⑦ 兜住。");
        }
        else if (baseMd5 != Program.PinnedBaselineMd5)
        {
            fail++;
            Console.WriteLine("  ✗ [基线指纹] 🔴 **卡池没动、而基线文件被改写了** —— 这正是 `A1382` 那条洞："
                              + "「任何真回归 ＋ 重生成基线 ⇒ 静默变绿」。");
            Console.WriteLine("             卡池 md5=" + poolMd5 + "（与钉住的一致 ⇒ **解析器的输入没变**）"
                              + " · 基线 md5=" + baseMd5 + "（钉住的是 " + Program.PinnedBaselineMd5 + "）");
            Console.WriteLine("             ⇒ 先看 `git diff 工具/ruleprobe/基线/out_baseline.txt`：");
            Console.WriteLine("                · **有意**重生成（解析器真改对、已用 `RuleEngineTest.Run` 或真机验过）"
                              + "⇒ 把 `Program.PinnedBaselineMd5` 改成上面的现值；");
            Console.WriteLine("                · **为了把红抹平** ⇒ 那是真回归，去修解析器（⛔ 别改这个常量）。");
            Console.WriteLine("             ⚠️ 这一条**没有**「一条命令重钉」的口子 —— 重钉只能**手改源码**"
                              + "（那才是显式动作、才会出现在 `git diff` 里）。");
        }
        else
            Console.WriteLine("  ✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）");

        // ---------------------------------------------------------------- ⑦ 解析输出指纹（`A1382` 一般情形）
        // 🔴 **⑤ 补不了的那一半**：⑤ 的判据要「卡池 md5 与钉住的一致」才成立，而卡池天天在变
        //    ⇒ ⑤ 走「此时不判」那一支时，**这条洞是敞开的**（`A1619` 现读就是这样）。
        //    本条读的是**当次现算的 dump** 与**源码里的常量** ⇒ ⛔ 既不依赖卡池、也不依赖基线文件。
        //    判据/分工/重钉纪律 ⇒ `Program.PinnedParseMd5` 的注释。
        string liveMd5 = Program.CanonParseMd5(newLines);
        if (liveMd5 != Program.PinnedParseMd5)
        {
            fail++;
            Console.WriteLine("  ✗ [输出指纹] 🔴 **当次现算的解析输出 ≠ 被确认过的那一版**（`A1382`）。");
            Console.WriteLine("             现算=" + liveMd5 + " · 钉住=" + Program.PinnedParseMd5);
            Console.WriteLine("             ⚠️ 这一条**一次都没读基线文件**（⛔ 所以「重生成基线」抹不掉它）——"
                              + "它盯的是「**源码跑出来的解析结果**有没有离开被确认的那一版」。");
            Console.WriteLine("             ⇒ 先跑 `git diff 工具/ruleprobe/基线/out_baseline.txt`"
                              + "＋看上面那句「解析差异 N 行 / 前 N 张」：");
            Console.WriteLine("                · **真回归**（解析器被改坏了）⇒ 去修解析器，⛔ 别改下面的常量；");
            Console.WriteLine("                · **有意且已验过**（`RuleEngineTest.Run` 或真机；常见成因 ="
                              + "别的写手改了卡池/解析器、或本会话的改动**还没被确认**）"
                              + "⇒ **手改**下面这一行（那就是本哨兵要的显式确认）：");
            Console.WriteLine("                    public const string PinnedParseMd5 = \"" + liveMd5 + "\";");
            Console.WriteLine("             ⚠️ 「解析差异 0 行」而这一条红 = **基线已被重生成过**（正是 `A1382` 那条洞）；"
                              + "「解析差异 > 0」而这一条也红 = 两把尺子指着同一件事（正常）。");
        }
        else
            Console.WriteLine("  ✓ [输出指纹] 当次现算的解析输出与被确认的那一版一致（" + liveMd5.Substring(0, 8)
                              + "… · 与卡池/基线文件都无关 —— 重生成基线抹不掉）");

        // ---------------------------------------------------------------- ⑧ `Source` 列（`A1568①` / `A1590②`）
        // 🔴 **判据 = 「`Source` 这一列还在不在 dump 里、且真打了值」**。
        //    `A1568①` 的病灶：`EffectOp.Source` **三个 dump 入口一个都不打**、基线里也没有
        //    ⇒ 凡改 `Source` 的活 `check` 一行都不多（`A1439` 那条「两条路 `Source` 不是同一个东西」
        //    因此**今天没仪器可证**，只能按代码读）。这一条把它钉成
        //    「**把那段渲染摘掉 ⇒ 立刻红**」（红法实测见普查产出 `W3_探针A1478A1590b.md` §四）。
        // ⚠️ 它读**当次现算的 dump**（⛔ 一次都不读基线）⇒ `scan` 重生成基线抹不掉它。
        // ⚠️ 它**不判** `Source` 的内容对不对（那要靠基线 diff ＋ 卡面 / 反编译）——
        //    只判「这一列看得见」，这正是本条要治的那条缝。
        // ⚠️ 与 ④（目标列清单）分工不同：④ 比的是**类里的字段**与清单，本条比的是
        //    「**渲染还打不打**」—— 谁把 `Program.TargetBlock` 尾上那行 `s += SourceText(op);`
        //    删掉（或把 `SourceText` 改成返回空串），本条立刻出声。
        // 🔴 计数口径：`Ops()` 按 `;` 切，而**每条真 op 必带 `/{T:`**（`TargetBlock` 恒打 `/{T:…}`）
        //    ⇒ 「不带 `/{T:` 的片段」= 被 `;` 切出来的**假 op**（`Source` 是卡面原文、本仓取
        //    「原样打、不转义」⇒ 它自带 `;` 时那一行会被多切一刀）。真 op 数按 `opsWithT` 算。
        int pieces = 0, opsWithT = 0, opsWithSrc = 0, srcNonEmpty = 0;
        int srcTruncated = 0, srcMovesLegacyCut = 0;
        foreach (var l in newLines)
        {
            if (l.Length == 0) continue;
            var p = l.Split(new[] { '|' }, 5);
            if (p.Length < 5) continue;
            foreach (var o in Ops(p[4]))
            {
                pieces++;
                if (o.IndexOf("/{T:", StringComparison.Ordinal) >= 0) opsWithT++;
                int k = o.IndexOf("/{SRC:", StringComparison.Ordinal);
                if (k < 0) continue;
                opsWithSrc++;
                var rest = o.Substring(k + 6);                       // 6 = "/{SRC:".Length
                string src = rest.EndsWith("}") ? rest.Substring(0, rest.Length - 1) : rest;
                if (!rest.EndsWith("}")) srcTruncated++;             // `Source` 原文自带 `;` ⇒ 这条块被切断了
                if (src.Length != 0) srcNonEmpty++;
                if (src.IndexOf("/{T:", StringComparison.Ordinal) >= 0) srcMovesLegacyCut++;
            }
        }
        int phantom = pieces - opsWithT;
        if (opsWithT == 0 || opsWithSrc == 0 || srcNonEmpty == 0)
        {
            fail++;
            Console.WriteLine("  ✗ [Source 列] 🔴 `dump` 里**看不到 `EffectOp.Source`**（"
                              + (opsWithSrc == 0 ? "一条 op 都没带 `/{SRC:…}` 块" : "带块但**全是空值**")
                              + "）—— `A1568①` 那条缝又开了：改 `Source` 的活 `check` 一行都不会多"
                              + "（`A1439` 那类「两条路 `Source` 不是同一个东西」的问题**没仪器可证**）。");
            Console.WriteLine("             ⇒ 看 `Program.TargetBlock` 末尾那句 `s += SourceText(op);`"
                              + " 与 `Program.SourceText` 还在不在（`scan` 那条路唯一的口）。");
        }
        else
            Console.WriteLine("  ✓ [Source 列] " + opsWithT + " 条 op 带 `/{SRC:…}` 块（其中非空 "
                              + srcNonEmpty + " 条）—— 改 `Source` 的活 `check` 现在看得见");
        if (phantom > 0 || srcTruncated > 0 || srcMovesLegacyCut > 0)
        {
            // ⚠️ **不算失败**：`Source` 是**卡面原文**，本仓取的是「原样打、不转义」（同 `countRef`/`Raw`），
            //    所以它自带 `;` / `/{T:` 时 dump 的形状会**局部失真** —— 出声说清是哪一种、有几条，
            //    ⛔ 别让它变成一条「经常合理地红」的哨兵（那会把「红了不用管」训练成习惯）。
            //    出处：`A1374`/`E1` 顺手发现 3（`Target.Raw` 那处同族，当时实测 0 例）。
            Console.WriteLine("  ⚠️ [Source 列] 形状失真（`Source` 原文自带分隔符）："
                              + "被 `;` 多切出一刀的假 op **" + phantom + "** 条 · 被截断的块 **" + srcTruncated
                              + "** 条 · `Source` 含 `/{T:`（会挪 `Sentinel.Legacy` 的切点）**"
                              + srcMovesLegacyCut + "** 条（真 op 数按 `/{T:` 数 = " + opsWithT + "，"
                              + "`Ops()` 切出来 " + pieces + " 条）");
        }

        Console.WriteLine("=== 哨兵合计：失败 " + fail + " 条 ===");
        return fail;
    }
}

/// 🔴 两个数必须分开（照 `D:/tmp/wf_b14_probe` 那次的教训）：
///   · **解析差异** = 第 2 列往下变了 ⇒ **红**（引擎解析器真动了，要逐行看）
///   · **改名** = 只有第 1 列（`id|name`）变了 ⇒ **黄**（本工程每批都在改卡名/重跑产物，通常无害）
/// ⛔ 别拿「逐字节相同」当判据 —— 卡名是**会变的产物**（B16/B20 两批共改 6 张），
///    拿它当基线判据 ⇒ 第一天就红、而且红的是**噪声**，会把「红了不用管」训练成习惯。
/// 退出码：0 = 解析差异 0 行 **且** 哨兵全过 · 1 = 有解析差异**或**哨兵失败 · 2 = 基线缺失/读不到。
/// 🔴 **2026-10-10 `A1348`**：除「与基线比」之外，还跑一组**与基线无关的哨兵**（见 `Sentinel`）——
///   因为**基线同一个工具就能重写**（`scan` 一条命令），「红了顺手重生成基线」会把**真回归静默抹平**。
///   哨兵的判据写在**源码**里，重生成基线抹不掉。
/// </summary>
class CheckMain
{
    /// <summary>缺省基线 —— 🔴 **2026-10-11 修（台账 `A1380`）**。
    ///
    /// **原来写的是**：`程序集目录` 往上**两级** ＋ `基线/out_baseline.txt`。而 `Assembly.Location`
    /// 是 `工具/ruleprobe/bin/Debug/net8.0` ⇒ 两级只上到 **`bin`** ⇒ 算出来是
    /// **`工具/ruleprobe/bin/基线/out_baseline.txt`**（**不存在**）⇒ 谁直接敲 dll、**不带参数**，
    /// 一律 `!! 没有基线 …` · **`rc=2`**。日常躲过它只有一个原因：`ruleprobe.sh` **总是显式传**
    /// `"${2:-基线/out_baseline.txt}"`，而 README 的「怎么跑」那节**没写「必须带参数」**。
    /// ⚠️ **它是「响的」不是「静的」**（不会给出假绿），所以当时判「超范围」；但**仍然要修**（铁律 11）。
    /// ⚠️ 台账原文说「少两级」—— 实测**差一级**（`net8.0 → Debug → bin → ruleprobe` 是**三级**，
    ///   原文那个路径正好落在 `bin/基线`）。这处口径差已记进报告（铁律 5：正本不归本工具改）。
    ///
    /// ⇒ **修法 = 按名字往上找**（形状照 `ResolveOutDir` 找 `MyGame/Assets`）：
    ///   ① 先找 `基线/out_baseline.txt` **文件本身**（最稳：换 build 配置、换 TFM 都不受影响）；
    ///   ② 再找 `基线/` **目录**（文件还没生成时，报错信息也能指到**该在的位置**）；
    ///   ③ 都没有 ⇒ 回落到「按规则算的那一条」（三级 `..`），让 caller 的报错信息指对地方（⛔ 不静默）。
    /// </summary>
    public static string DefaultBaseline()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        DirectoryInfo dirHit = null;
        while (d != null)
        {
            var f = Path.Combine(d.FullName, "基线", "out_baseline.txt");
            if (File.Exists(f)) return f;
            if (dirHit == null && Directory.Exists(Path.Combine(d.FullName, "基线"))) dirHit = d;
            d = d.Parent;
        }
        if (dirHit != null) return Path.Combine(dirHit.FullName, "基线", "out_baseline.txt");
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                             "..", "..", "..", "基线", "out_baseline.txt"));
    }

    public static int Run(string baselinePath)
    {
        bool wasDefault = string.IsNullOrEmpty(baselinePath);
        if (wasDefault) baselinePath = DefaultBaseline();
        baselinePath = Path.GetFullPath(baselinePath);
        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine("!! 没有基线：" + baselinePath
                                    + (wasDefault ? "（**没传参数** ⇒ 缺省值 = 程序集目录往上找到的第一条 `基线/out_baseline.txt`）" : ""));
            Console.Error.WriteLine("   生成办法：先修好解析器，再跑  scan 工具/ruleprobe/基线/out_baseline.txt  把它定为新基线。");
            return 2;
        }
        var tmp = Path.Combine(Path.GetTempPath(), "wfprobe_check.txt");
        Scan.ScanMain(new[] { tmp }, true);
        var oldLines = File.ReadAllLines(baselinePath);
        var newLines = File.ReadAllLines(tmp);

        // 🔴 **2026-10-10 `A1348`：先跑哨兵** —— 它读的是**当次现算的 dump**、
        //   ⛔ 不看基线 ⇒ **重生成基线抹不掉它**。这就是「基线可被重写」那条洞的机器级堵法。
        //   ⚠️ 它必须算进退出码 —— 否则又是「静默」。
        int sentFail = Sentinel.Run(newLines, baselinePath);

        string KeyOf(string l) { int p = l.IndexOf('|', l.IndexOf('|') + 1); return p < 0 ? l : l.Substring(0, p); }
        string DefOf(string l) { int p = l.IndexOf('|', l.IndexOf('|') + 1); return p < 0 ? "" : l.Substring(p + 1); }

        var oldMap = new Dictionary<string, string>();
        foreach (var l in oldLines) { if (l.Length == 0) continue; oldMap[KeyOf(l)] = DefOf(l); }
        var newMap = new Dictionary<string, string>();
        foreach (var l in newLines) { if (l.Length == 0) continue; newMap[KeyOf(l)] = DefOf(l); }

        int parseDiff = 0, renamed = 0, added = 0, removed = 0;
        var parseNames = new List<string>();
        foreach (var kv in newMap)
        {
            if (!oldMap.TryGetValue(kv.Key, out var od)) { added++; continue; }
            if (!string.Equals(od, kv.Value, StringComparison.Ordinal)) { parseDiff++; if (parseNames.Count < 20) parseNames.Add(kv.Key); }
        }
        foreach (var kv in oldMap) if (!newMap.ContainsKey(kv.Key)) removed++;
        // 改名：同一张卡的 id 在两侧都有、但 name 段不同（键是 `id|name` ⇒ 会被当 added+removed 各一条）
        var oldById = new Dictionary<string, string>();
        foreach (var l in oldLines) { var k = KeyOf(l); int p = k.IndexOf('|'); if (p > 0) oldById[k.Substring(0, p)] = k.Substring(p + 1); }
        for (int n = 0; n < newLines.Length; n++)
        {
            var k = KeyOf(newLines[n]); int p = k.IndexOf('|'); if (p <= 0) continue;
            var id = k.Substring(0, p); var nm = k.Substring(p + 1);
            if (oldById.TryGetValue(id, out var on) && on != nm) renamed++;
        }
        added -= renamed; removed -= renamed;
        if (added < 0) added = 0; if (removed < 0) removed = 0;

        Console.WriteLine("=== 全池 " + newMap.Count + " 张 · 解析差异 " + parseDiff + " 行 · 改名 " + renamed
                          + " 行 · 新增 " + added + " · 消失 " + removed + " · 基线 " + Path.GetFileName(baselinePath) + " ===");

        // ---------------------------------------------------------------- 🔴 绝对读数（`A1433` ②）
        // 病灶：上面那一行**只有「与基线比」出来的 diff** ⇒ 「解析差异 0 行」被读成「没问题」，
        //   而它只说明「**与基线一致**」。实测（第十四会话 `逐卡效果检查_方法与边界` §2·1）：
        //   `scan` 的 `unparsed` 列有 **176/1120 行非空** 的同一时刻，这里照样报「解析差异 0 行」。
        // ⇒ 把**绝对数**打在同一段里 —— 读的人不会再拿「没变」当「没有」。
        int absRows = 0, absUn = 0, absPa = 0;
        foreach (var l in newLines)
        {
            if (l.Length == 0) continue;
            var p = l.Split(new[] { '|' }, 5);      // ⚠️ 只切 4 刀（ops 列里可能还有 `|`，同 `Sentinel.Row`）
            if (p.Length < 5) continue;
            absRows++;
            if (p[2].Length != 0) absUn++;
            if (p[3].Length != 0) absPa++;
        }
        Console.WriteLine("--- 绝对读数（**与基线无关**：上面那几个数说的是「有没有变」，这几个说的是「有多少」）---");
        Console.WriteLine("  全池 " + absRows + " 行 · `unparsed` 非空 " + absUn + " 行 · `partial` 非空 " + absPa + " 行");
        Console.WriteLine("  ⚠️ **别把「`unparsed` 非空」当缺陷计数**（`A1433` ①）—— `EffectText.Coverage` 把"
                          + "「已由别的层接手」");
        Console.WriteLine("     的句子（`When <事件>,` / `Talent:` / `Companion N:` / 光环 / 开局上手）**先筛掉**之后，"
                          + "四栏**全 0**。");
        Console.WriteLine("     两个口径的完整换算 ⇒ 跑 `willrun` 的 ⑤ 段。");
        Console.WriteLine("  ⚠️ 而「解析差异 0 行」= **解析器没被改坏**，⛔ **不等于**「解析全通」——"
                          + "一条**早就在**的缺口这里永远绿。");
        Console.WriteLine("     要问「**卡面正文到底收到没有**」⇒ 跑 `willrun`（`A1432` 修掉的就是那一栏）。");
        if (parseDiff > 0)
        {
            Console.WriteLine("⚠️ 解析差异（前 " + parseNames.Count + " 张）：" + string.Join(" · ", parseNames));
            Console.WriteLine("⚠️ 这**不一定**是缺陷 —— 逐行看过再决定是「修基线」还是「修解析器」。");
        }
        // ⚠️ 哨兵红了**不许**靠重生成基线消掉 —— 它读的不是基线（见 `Sentinel` 头注）。
        //   退出码把两者合并，免得「基线绿、哨兵红」被当成绿。
        return (parseDiff > 0 || sentFail > 0) ? 1 : 0;
    }

}
