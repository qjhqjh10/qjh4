using System;
using System.Collections.Generic;
using System.Linq;
using RuleEngine;

static class FilterAdd
{
    public static void Run(List<CardDef> pool, string name)
    {
        var c = pool.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (c == null) { Console.WriteLine("!! no card " + name); return; }
        var ops = RuleCore.PlayerChooseOps(c);
        foreach (var op in ops)
        {
            if (op.Verb != "choosecard") continue;
            string detail, why;
            var cands = CreatePool.FilterChoose(pool, pool, op.ChooseWhat, out detail, out why);
            Console.WriteLine("FILTER " + name + " src=" + op.ChooseSrc + " what=[" + op.ChooseWhat + "]"
                + " → " + cands.Count + " 张  why=" + (why ?? "-") + " detail=" + (detail ?? "-"));
            Console.WriteLine("    前 5 张: " + string.Join(" / ", cands.Take(5).Select(x => x.Name + "(" + x.Type + "," + x.Subtype + "," + x.Rarity + ")")));
        }
    }
}
