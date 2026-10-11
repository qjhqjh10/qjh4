using System;
using System.Collections.Generic;
using System.Linq;
using RuleEngine;

static class FilterAdd
{
    /// <summary>
    /// `filter <卡名>` —— 看「这张卡的 `choosecard` 会滤出哪些候选」。
    ///
    /// 🔴 **2026-10-11 修（台账 `A1383`）**：原来**没有 `choosecard` op 就一行都不打** ——
    ///   实测 `filter "Primaris Intercessor"` **零输出**（现场看着像「命令根本没跑」），
    ///   而同族的反面 `filter zzzznotacard` **反倒会打** `!! no card …`。本仓红线：**不许静默失败**。
    ///
    ///   台账原文要的是「**查清是【本来就没 op】还是【过滤条件写窄了】**」⇒ 两个成因现在**分开出声**：
    ///     · ① **本来就没 op** —— `RuleCore.PlayerChooseOps` 返回**空表**（`desc` 为空 ⇒ 引擎自己
    ///       就短路返回空；或卡面真的不问玩家）。
    ///     · ② **过滤条件写窄了** —— 有 ask op，但**动词不是 `choosecard`**。
    ///       🔴 `PlayerChooseOps` 一共会返回**三种**动词（`choosecard` / `chooseone` / `chooseeffect`），
    ///       而本子命令原来只处理第一种 ⇒ 后两种**一条都不打**（`chooseone` 的池子藏在 `ChooseWhat` 里、
    ///       `chooseeffect` 藏在 `Payload` 里，都不走 `CreatePool.FilterChoose`）。
    ///   两种现在都打一行**带 `!!` / `--` 的说明**（⛔ 不再让「空输出」冒充「没有这东西」）。
    ///
    /// ⚠️ **卡名不是唯一键** —— 全池有 3 个重名（`Terminator` / `Terminator Champion` / `Maulerfiend`），
    ///   原来 `FirstOrDefault` 只打**第一张**、其余**静默丢掉**；现在**同名全部打**（与 `card` 模式同口径）。
    /// </summary>
    public static void Run(List<CardDef> pool, string name)
    {
        var hits = pool.Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (hits.Count == 0) { Console.WriteLine("!! no card " + name); return; }
        if (hits.Count > 1)
            Console.WriteLine("ℹ️ 同名 " + hits.Count + " 张，全部列出（`filter` 原来只看 `FirstOrDefault` 那一张）："
                              + string.Join(" / ", hits.Select(x => x.Id)));
        foreach (var c in hits) One(pool, c);
    }

    static void One(List<CardDef> pool, CardDef c)
    {
        string tag = c.Name + "(" + c.Id + ")";
        var ops = RuleCore.PlayerChooseOps(c);
        if (ops.Count == 0)
        {
            // ① 本来就没 op —— **出声**（原来这里是「什么都不打」，`A1383` 的病灶本体）。
            Console.WriteLine("!! `" + tag + "` **没有任何会问玩家的 op** —— "
                + (string.IsNullOrWhiteSpace(c.Desc)
                    ? "它的 `desc` 是**空的**（`PlayerChooseOps` 对空 `desc` 直接返回空表）⇒ 【本来就没 op】，⛔ 不是过滤条件写窄了。"
                    : "`desc` 非空（`" + c.Desc + "`），但解出来的 op 里**没有 ask 动词** ⇒ 【本来就没 op】。逐条看 ⇒ `card \"" + c.Name + "\"`。"));
            return;
        }
        int printed = 0;
        foreach (var op in ops)
        {
            if (op.Verb != "choosecard")
            {
                // ② 有 ask op、但不是 `choosecard` ⇒ **没有候选池可滤**（原来这一支同样静默）
                Console.WriteLine("-- `" + tag + "` 有 ask op `" + op.Verb + "` —— 它**不是** `choosecard` ⇒ "
                    + "本子命令**没有候选池可滤**（池子在别处：`chooseone` 在 `ChooseWhat`、`chooseeffect` 在 `Payload`）"
                    + " src=" + op.ChooseSrc + " what=[" + (op.ChooseWhat ?? "") + "]");
                continue;
            }
            string detail, why;
            var cands = CreatePool.FilterChoose(pool, pool, op.ChooseWhat, out detail, out why);
            Console.WriteLine("FILTER " + tag + " src=" + op.ChooseSrc + " what=[" + op.ChooseWhat + "]"
                + " → " + cands.Count + " 张  why=" + (why ?? "-") + " detail=" + (detail ?? "-"));
            Console.WriteLine("    前 5 张: " + string.Join(" / ", cands.Take(5).Select(x => x.Name + "(" + x.Type + "," + x.Subtype + "," + x.Rarity + ")")));
            printed++;
        }
        if (printed == 0)
            Console.WriteLine("-- `" + tag + "` 共 " + ops.Count + " 条 ask op，但**没有一条是 `choosecard`** ⇒ "
                + "本子命令无可打的候选（⚠️ 这不是「没 op」，见上面那几行 `--`）。");
    }
}
