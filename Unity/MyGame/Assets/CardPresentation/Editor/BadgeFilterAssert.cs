// BadgeFilterAssert.cs — 执行代理 `W10`（2026-10-24）· 「场上徽标筛选」+「主动能力图标来源」的断言
//
// 宿主：`BattleVfxAssertE3.Run()` 末尾顺带调一次（那份已经被 `BattleScene.Run` 调用 ⇒ 不用再改
// `Editor/BattleScene.cs`，那个文件本批**不归我**）。本文件**只断、不改**：不建场景、不开关窗、
// 不动驱动状态 ⇒ 放哪一步都安全。
//
// 断的是什么（判据全在反编译里，⛔ 不是「我们自己的常量」）：
//  · **T2 · 7 格场上徽标的筛选** = `CardTraitCollection__GetTraitInPlayListForIcons.c`（202 行）
//    要 `hasIcon && isInPlayIcon && traitIcon != null`；逐 trait 取值来自
//    `Unity/数据/游戏数据/trait_collection.json`（77 条全带，2026-10-23 解出）。
//    落地 = `Core/Badges.cs` 的 `InPlayIconKeys`（47 条白名单）→ `Badges.PlaysInPlayIcon`。
//  · **T2 · 敌方伏击整排不画** = `BattleCardUI__UpdateTraitIcons.c:40-46`
//    （先全 `Toggle(false)`，再 `HasCurrentTrait(ambush) && !isPlayer ⇒ return`）。
//    落地 = `Badges.For(..., isPlayer)`。
//  · **T1 · 技能钮图标按军队查** = `ArmyUtilities.GetActiveAbilityIconForArmy.c` →
//    `armyIconsSO.activeAbilityIcons`（`sharedassets0.assets` pathID 416，**5 条**，2026-10-24 逐字节解出）。
//    落地 = `Core/ArmyIcons.cs` 的 `ActiveAbilityIconName`。
//
// 🔑 **每条都能回答「改回旧写法会不会红」**：旧写法 =「凡查得到图标都画」+「不看阵营」——
//    把这层筛选删掉、或把 47 条白名单清空/放宽，下面都有对应的红（见每条消息末尾的 🧨）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;   // CardDatabase / CardDef（判据 = 卡池现读，⛔ 不写死张数）
using UnityEngine;

/// <summary>`W10` 的断言集合。`Run()` 幂等（除了 `Badges` 那两处「只告警一次」的静态诊断计数）。</summary>
public static class BadgeFilterAssert
{
    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    /// <summary>原版 `hasIcon == false` 的那 5 条（**有图**，但永远进不了 7 格）。</summary>
    static readonly string[] HasIconFalse = { "ambush", "berzerk", "flying", "huntMark", "markerlight" };

    /// <summary>原版 `hasIcon == true` 但 `isInPlayIcon == false` 的那 12 条（**有图**，但不算场上图标）。</summary>
    static readonly string[] NotInPlayIcon = {
        "armour", "companion", "ephemeral", "fast", "flank", "pack", "rally", "talent",
        "teleport", "tide", "uprising", "vanguard",
    };

    public static void Run()
    {
        Passed = 0; Failed = 0;
        Debug.Log("BT W10 --- 执行代理 W10（2026-10-24）：场上徽标筛选（T2）+ 主动能力图标来源（T1·⑦）---");

        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 1000,
              "(前提) 卡池载得到（`CardDatabase.Load()`，现读 " + (pool == null ? 0 : pool.Count) + " 张）"
            + " —— 载不到的话下面每一条都**没验到**，别读成通过");
        if (pool == null || pool.Count == 0) { Summary(); return; }

        // ── 1. 白名单本身：47 条，且每条的图都在（少一张就说明抄错了表）──────────────
        {
            var keys = Badges.InPlayIconKeysForTest;
            Check(keys.Length == 47,
                  "★ T2·白名单条数 = **47**（`trait_collection.json` 里 `hasIcon && isInPlayIcon && traitIcon != null`"
                + " 的条数；现读 " + keys.Length + "）—— 🧨 清空/放宽这张表（改回「有图就画」）⇒ 条数变 ⇒ 红");
            var noImg = new List<string>();
            foreach (var k in keys) if (Badges.SpriteOf(k) == null) noImg.Add(k);
            Check(noImg.Count == 0,
                  "★ T2·白名单里每一条都**真的有图**（`Art/traits/` 取得到）"
                + (noImg.Count == 0 ? "" : "｜✗ 没图的：" + string.Join(" / ", noImg.ToArray()))
                + " —— 🧨 往白名单里塞一个没有图的键 ⇒ 本条红");
        }

        // ── 2. 负例：17 条「有图、但原版不上场上徽标」一条都不许画 ────────────────
        //    判据：`trait_collection.json` 的 hasIcon / isInPlayIcon 两列（逐条见 `Badges.InPlayIconKeys` 的注释）。
        //    🧨 把 `For` 里那句 `if (!PlaysInPlayIcon(spr)) continue;` 删掉 ⇒ 这 17 条**全部**变 1 枚 ⇒ 两条都红。
        CheckAllHidden(HasIconFalse, "`hasIcon == false`（5 条）");
        CheckAllHidden(NotInPlayIcon, "`hasIcon == true` 但 `isInPlayIcon == false`（12 条）");

        // ── 3. 正例：47 条每一条都**真画得出来**（挡「一刀切全不画」那种假绿）──────────
        {
            var bad = new List<string>();
            foreach (var k in Badges.InPlayIconKeysForTest)
            {
                var b = Badges.For(One(k), null);
                if (b.Count != 1) bad.Add(k + "(" + b.Count + ")");
            }
            Check(bad.Count == 0,
                  "★ T2·正例：白名单里 **47 条每条都画 1 枚**"
                + (bad.Count == 0 ? "" : "｜✗ 不对的：" + string.Join(" / ", bad.ToArray()))
                + " —— 🧨 把筛选写成「一律不画」/ 白名单清空 ⇒ 47 条全变 0 ⇒ 本条红（第 2 条那种「全挡掉」也过不了这关）");
        }

        // ── 4. 全池：1127 张逐卡扫一遍，**17 条一次都不许出现在徽标里** ────────────────
        //    这一条**不复用 `PlaysInPlayIcon`**（那是被测实现），而是拿上面两张名单**独立**比 —— 灭自证。
        {
            // 🔴 **2026-10-24（`W26` · `D2` §11 / `W24` §11）：全池扫**之前**先清空累积** ——
            //    `Badges.UnknownIconSpritesForTest` 记的是**全进程累积**（上面第 2、3 段也调过 `Badges.For`），
            //    不清就等于「不限于这一趟」。清空之后 `PlaysInPlayIcon` 里那批图名会**再告警一次**
            //    （它同时是「每个新键只告警一次」的去重集 —— 已写在 `Badges.ResetUnknownForTest` 的 doc 里，有意为之）。
            Badges.ResetUnknownForTest();

            var banned = new HashSet<string>();
            foreach (var k in HasIconFalse) banned.Add(Norm(k));
            foreach (var k in NotInPlayIcon) banned.Add(Norm(k));

            int cards = 0, withBadges = 0, badCards = 0;
            var offenders = new SortedSet<string>();
            foreach (var c in pool)
            {
                cards++;
                var b = Badges.For(c.Keywords, c.Desc);
                if (b.Count > 0) withBadges++;
                foreach (var x in b)
                {
                    string spr = Badges.SpriteOf(x.key);
                    if (spr != null && banned.Contains(Norm(spr)))
                    {
                        badCards++;
                        offenders.Add(c.Id + ":" + x.key);
                    }
                }
            }
            Debug.Log("BT W10   全池 " + cards + " 张：能画出徽标的 " + withBadges + " 张（改前口径是 707 张）");
            Check(cards > 1000, "全池扫到了（现读 " + cards + " 张）");
            Check(badCards == 0,
                  "★ T2·全池（" + cards + " 张）：**没有一张卡**画出那 17 条里的任何一枚"
                + (badCards == 0 ? "" : "｜✗ " + badCards + " 处，例：" + First(offenders, 8))
                + " —— 🧨 把 `For` 的筛选删掉（改回「有图就画」）⇒ 带 `Rally`/`Vanguard`/`Flying`… 的卡立刻出徽标 ⇒ 红");
            Check(withBadges > 400,
                  "★ T2·全池：**过半数的卡至少有一枚徽标**（现读 " + withBadges
                + " 张；本表口径下 2026-10-24 实测 **407**）—— 🧨 筛得太狠（把正例也挡掉）⇒ 掉到 400 以下 ⇒ 红");
            // 🔴 **2026-10-24（`W26` · `D2` §11 / `W24` §11）：这条原来是 `unknown.Length == 0` —— 语义写错了。**
            //    `UnknownIconSpritesForTest` 记的是「**不在那 47 条白名单里**的图名」，而**类顶部**那两张名单里的
            //    **17 条**（`HasIconFalse` 5 + `NotInPlayIcon` 12）**必然**落在里面（原版三列 `hasIcon` /
            //    `isInPlayIcon` / `traitIcon` 就是要把它们挡掉）⇒ 正确语义 =「报出来的**每一条都解释得通**」，
            //    ⛔ 不是「一条都没有」。真正该红的是「**17 条之外的**图名」（图集里多一张、对不上数据表、又被当关键词用）。
            //    ⚠️ 归一化只认 `Badges.Norm` 那一份（图名 `huntMark` vs 键 `huntmark` 这种大小写差靠它抹平）。
            var unknown = Badges.UnknownIconSpritesForTest;

            var known = new HashSet<string>();
            foreach (var k in HasIconFalse) known.Add(Badges.Norm(k));
            foreach (var k in NotInPlayIcon) known.Add(Badges.Norm(k));

            var unexplained = new List<string>();
            var explained = new List<string>();
            foreach (var u in unknown)
            {
                if (known.Contains(Badges.Norm(u))) explained.Add(u);
                else unexplained.Add(u);
            }
            var missed = new List<string>();
            foreach (var k in known)
            {
                bool hit = false;
                foreach (var u in unknown) if (Badges.Norm(u) == k) { hit = true; break; }
                if (!hit) missed.Add(k);
            }
            Debug.Log("BT W10   诊断计数器（这一趟）：" + unknown.Length + " 条 = " + string.Join(" / ", unknown)
                    + "｜已知解释 " + explained.Count + " 条 · 未解释 " + unexplained.Count + " 条"
                    + (missed.Count == 0 ? "" : "｜已知但这一趟没出现：" + string.Join(" / ", missed.ToArray())));

            // 防假绿①：已知集合本身得真是那 17 条 —— 两张名单被清空/改小之后「什么都能解释」，下一条会假绿。
            Check(known.Count == 17,
                  "★ T2·诊断：已知可解释的图名 = **17 条**（`hasIcon == false` 5 + `isInPlayIcon == false` 12；"
                + "归一化后现读 " + known.Count + "）"
                + " —— 🧨 把两张名单清空/改小 ⇒ 下一条变成「什么都能解释」⇒ 本条先红（挡假绿）");
            // 防假绿②：计数器**真的记了账** —— 先把记账那句删掉，`unknown` 就是空的，
            // 只判 `unexplained == 0` 会**全绿**（「一条都没报」不等于「都对得上」）。
            Check(explained.Count > 0,
                  "★ T2·诊断：计数器**真的记了账**（这一趟报出 " + unknown.Length + " 条，其中已知 " + explained.Count + " 条）"
                + " —— 🧨 把 `PlaysInPlayIcon` 里那句 `_notInDataWarned.Add(sprite)` 删掉 ⇒ 一条都不记 ⇒ 本条红");
            Check(unexplained.Count == 0,
                  "★ T2·诊断：计数器里**没有没解释过**的图名（被原版三列挡掉的 17 条属于预期、不算；"
                + "这一趟报出 " + unknown.Length + " 条 / 已知 " + explained.Count + " 条）"
                + (unexplained.Count == 0 ? "" : "｜✗ " + string.Join(" / ", unexplained.ToArray()))
                + " —— 🧨 图集里多一张对不上 `trait_collection.json`、又被当关键词用 ⇒ 红（不静默）");
        }

        // ── 5. 敌方伏击整排不画（原版 `BattleCardUI__UpdateTraitIcons.c:40-46`）──────────────
        {
            var amb = new Dictionary<string, int> { { "Ambush", 1 }, { "Stealth", 1 } };
            int foe = Badges.For(amb, null, null, null, false).Count;
            int mine = Badges.For(amb, null, null, null, true).Count;
            int foeNoAmb = Badges.For(new Dictionary<string, int> { { "Stealth", 1 } }, null, null, null, false).Count;
            Check(foe == 0,
                  "★ T2·**敌方**（`isPlayer = false`）+ `Ambush` ⇒ 整排 **0 枚**（现读 " + foe + "）"
                + " —— 🧨 把 `For` 里那句 `if (!isPlayer && HasKeyword(ordered, \"ambush\")) return list;` 删掉"
                + " ⇒ 这里变 1（Stealth 那枚）⇒ 红");
            Check(mine == 1,
                  "★ T2·**我方**同样的词条 ⇒ 只画 `Stealth` 那 **1 枚**（现读 " + mine
                + "；`Ambush` 自己 `hasIcon=false` 本来就不画）"
                + " —— 🧨 把这条闸写成「见 ambush 就全藏」（不分敌我）⇒ 这里变 0 ⇒ 红");
            Check(foeNoAmb == 1,
                  "★ T2·**敌方但没有 ambush** ⇒ 照常画（现读 " + foeNoAmb + "）"
                + " —— 🧨 把 `isPlayer == false` 一律清空商品 ⇒ 这里变 0 ⇒ 红（别把闸开成「敌方不画徽标」）");
        }

        // ── 6. 技能钮图标：原版**按军队**查 `activeAbilityIcons`（5 条）────────────────────
        {
            var tbl = ArmyIcons.AllActiveAbility;
            Check(tbl.Length == 5,
                  "★ T1·⑦ `activeAbilityIcons` = **5 条**（`sharedassets0.assets` pathID 416 逐字节解出；现读 "
                + tbl.Length + "）—— 🧨 改成 13 条（照 `armyIcons` 那一族抄）⇒ 红");

            var pairs = new[]
            {
                new[] { "80",  "Attack_type_button_Pray"     },   // Sororitas
                new[] { "100", "Attack_type_button_Duty"     },   // AstraMilitarum
                new[] { "110", "Attack_type_button_Agenda"   },   // DarkAngels
                new[] { "130", "Attack_type_button_Ferocity" },   // SpaceWolves
                new[] { "10",  "Attack_type_button_Oath"     },   // Ultramarines
            };
            foreach (var p in pairs)
            {
                int army = int.Parse(p[0]);
                string got = ArmyIcons.ActiveAbilityIconName(army);
                Check(got == p[1],
                      "★ T1·⑦ army `" + army + "`（" + ArmyIcons.Faction(army) + "）→ 技能钮图 `" + p[1] + "`（现读 "
                    + (got ?? "null") + "）—— 🧨 改成按关键词挑图（把所有卡都画成 `Attack_type_Generic_Foreground`）、"
                    + "或把某个阵营对错了图 ⇒ 红");
            }
            Check(ArmyIcons.ActiveAbilityIconName(20) == null,
                  "★ T1·⑦ `Goff`（army 20）**不在那 5 条里** ⇒ 取不到图（原版此时整颗技能钮不显示；现读 "
                + (ArmyIcons.ActiveAbilityIconName(20) ?? "null") + "）"
                + " —— 🧨 给它兜底一张通用图（自造）⇒ 红");
            var noTex = new List<string>();
            foreach (var e in tbl)
                if (ArmyIcons.ActiveAbilityTexture(e.Army) == null) noTex.Add(ArmyIcons.ActiveAbilityIconName(e.Army));
            Check(noTex.Count == 0,
                  "★ T1·⑦ 那 5 张按钮图**都在工程里**（`Resources/Art/ui/`，`CardArt.Ui` 取得到）"
                + (noTex.Count == 0 ? "" : "｜✗ 没取到：" + string.Join(" / ", noTex.ToArray()))
                + " —— 🧨 把图改名/挪走 ⇒ 红（这条挡「表里有行、图没了」那种假绿）");
        }

        Summary();
    }

    // ------------------------------------------------------------------ 小工具

    /// <summary>一条关键词的「最小输入」（值给 1 = 引擎的兜底值，不画角标）。</summary>
    static Dictionary<string, int> One(string key) { return new Dictionary<string, int> { { key, 1 } }; }

    /// <summary>这一组词条**一条都不许画**（逐条单独喂，避免互相占 7 格）。</summary>
    static void CheckAllHidden(string[] keys, string label)
    {
        var bad = new List<string>();
        foreach (var k in keys)
        {
            int n = Badges.For(One(k), null).Count;
            if (n != 0) bad.Add(k + "(" + n + ")");
        }
        Check(bad.Count == 0,
              "★ T2·" + label + "：逐条都画 **0 枚**（" + string.Join(" / ", keys) + "）"
            + (bad.Count == 0 ? "" : "｜✗ 竟然画了：" + string.Join(" / ", bad.ToArray()))
            + " —— 🧨 把 `For` 里 `if (!PlaysInPlayIcon(spr)) continue;` 删掉（= 改回「凡查得到图标都画」）⇒ 每条变 1 ⇒ 红");
    }

    /// <summary>归一化（只留小写字母数字）。
    /// 🔴 **2026-10-24（`W26`）改成直接转发 `Badges.Norm`** —— 本文件原来另抄了一份一模一样的实现，
    /// 而 `Core/Badges.cs` 的 `Norm` doc 明确写着「⛔ 别在调用方另写一份归一化 —— 两份迟早不一致」；
    /// `PlaysInPlayIcon` 记账时用的是 `Badges.Key`，本文件**必须**和它同一份，否则大小写/别名差会静默对不上。
    /// （行为逐字节等价：原实现就是 `char.IsLetterOrDigit` + `ToLowerInvariant`。）</summary>
    static string Norm(string s) { return Badges.Norm(s); }

    static string First(IEnumerable<string> set, int n)
    {
        var list = new List<string>();
        foreach (var s in set) { list.Add(s); if (list.Count >= n) break; }
        return string.Join(" / ", list.ToArray());
    }

    static void Check(bool ok, string msg)
    {
        if (ok) { Passed++; Debug.Log("BT W10 ✓ " + msg); }
        else { Failed++; Debug.LogError("BT W10 ✗ " + msg); }
    }

    static void Summary()
    {
        string line = "BT W10 === 断言合计：" + Passed + " 通过 / " + Failed + " 失败 ===";
        if (Failed == 0) Debug.Log(line);
        else Debug.LogError(line + "（失败条目见上面每一条 `BT W10 ✗`）");
    }
}
