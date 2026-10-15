# -*- coding: utf-8 -*-
"""④b 的控制集：从**手写 19 条**改成**从现计划表推导**（`D3_IconSetup五条红.md` §一·5 的施工单）。
理由：`descZh` 换代后，手写那 19 条现读只剩 5 条在表里 ⇒ 抽掉之外等于没抽 ⇒ 灭自证退化成空转。"""
import io

P = r"d:/4/Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs"
s = io.open(P, encoding="utf-8", newline="").read()

# ── 1) 替换 ④b 的执行块 ─────────────────────────────────────────
OLD_BLOCK = (
'                // ④b 【灭自证】把 `A1360` 5 张 + `A1400` 14 张**改回去** ⇒ ④a 必须认出来\n'
'                var broke = LoadPlanDoc();\n'
'                int removed = 0;\n'
'                if (broke != null && broke.cards != null)\n'
'                    foreach (var c in broke.cards)\n'
'                        foreach (var f in c.fields)\n'
'                        {\n'
'                            if (f.name != "descZh" || f.items == null) continue;\n'
'                            var keep = new List<PlanItem>();\n'
'                            foreach (var it in f.items)\n'
'                            {\n'
'                                if (System.Array.IndexOf(RevertControl, c.id + "|" + it.sprite) >= 0)\n'
'                                { removed++; continue; }\n'
'                                keep.Add(it);\n'
'                            }\n'
'                            f.items = keep.ToArray();\n'
'                        }\n'
'                var ctrl = broke == null ? new List<string>() : ZhGaps(broke);\n'
'                var missed = new List<string>();\n'
'                foreach (var pair in RevertControl) if (ctrl.IndexOf(pair) < 0) missed.Add(pair);\n'
'                Check(removed == RevertControl.Length && missed.Count == 0,\n'
'                      "★ ④b【灭自证】把 `A1360` 那 5 张 + `A1400` 那 14 张**改回去**（抽掉 " + removed +\n'
'                      " 条 `descZh` 条目）⇒ ④a 必须认出来：" +\n'
'                      (RevertControl.Length - missed.Count) + "/" + RevertControl.Length + " 条被抓到" +\n'
'                      (missed.Count == 0 ? "" : "；**漏掉** " + string.Join(" / ", missed.ToArray())) +\n'
'                      "。这条是「结构上不可能同时满足」的口径：谓词恒绿 ⇒ 这里必红。", ref bad);\n'
)
assert s.count(OLD_BLOCK) == 1, s.count(OLD_BLOCK)

NEW_BLOCK = (
'                // ④b 【灭自证】：把「**今天两边都画着**」的每一枚**从 `descZh` 侧抽掉** ⇒ ④a 必须**全部**认出来。\n'
'                //   🔴 **2026-10-15（第三十二会话）改：控制集从【手写 19 条】改成【从现计划表推导】** ——\n'
'                //   原来那 19 条（`A1360` 5 + `A1400` 14）的注释自陈「这 19 条今天**都在**真计划表的 `descZh` 里」，\n'
'                //   而 2026-10-15 起 `gen_icon_plan.py` 对**带官方记号**的字段**整字段跳过** ⇒ 现读只剩 **5 条**在表里\n'
'                //   ⇒ 抽掉那 5 条之外**等于没抽** ⇒ 这条灭自证**退化成空转**（改动前同池就已经是 5/19）。\n'
'                //   ✅ **判据形式没变**：仍是「抽掉 ⇒ ④a 每一条都得认出来」＋「抽掉的条数 == 控制集长度」（**前提检查**）。\n'
'                //   出处 = `资料/普查产出_第三十二会话/D3_IconSetup五条红.md` §一·5。\n'
'                var broke = LoadPlanDoc();\n'
'                var control = RevertControlFor(broke);\n'
'                var controlSet = new HashSet<string>(control);\n'
'                int removed = 0;\n'
'                if (broke != null && broke.cards != null)\n'
'                    foreach (var c in broke.cards)\n'
'                        foreach (var f in c.fields)\n'
'                        {\n'
'                            if (f.name != "descZh" || f.items == null) continue;\n'
'                            var keep = new List<PlanItem>();\n'
'                            foreach (var it in f.items)\n'
'                            {\n'
'                                if (it != null && controlSet.Contains(c.id + "|" + it.sprite))\n'
'                                { removed++; continue; }\n'
'                                keep.Add(it);\n'
'                            }\n'
'                            f.items = keep.ToArray();\n'
'                        }\n'
'                var ctrl = broke == null ? new List<string>() : ZhGaps(broke);\n'
'                var missed = new List<string>();\n'
'                foreach (var pair in control) if (ctrl.IndexOf(pair) < 0) missed.Add(pair);\n'
'                string missTxt = missed.Count == 0 ? "" : ("；**漏掉** " + string.Join(" / ", missed.GetRange(0, System.Math.Min(8, missed.Count)).ToArray()));\n'
'                Check(control.Count > 0 && removed == control.Count && missed.Count == 0,\n'
'                      "★ ④b【灭自证】把「今天两边都画着」的每一枚从 `descZh` 侧抽掉（控制集 " + control.Count +\n'
'                      " 条 · 实际抽掉 " + removed + " 条）⇒ ④a 必须认出来：" +\n'
'                      (control.Count - missed.Count) + "/" + control.Count + " 条被抓到" + missTxt +\n'
'                      "。这条是「结构上不可能同时满足」的口径：谓词恒绿 ⇒ 这里必红。" +\n'
'                      "⚠️ 控制集为空（或抽不动）**也算红** —— 那是「这一格今天没在测任何东西」的前提检查。", ref bad);\n'
)
s = s.replace(OLD_BLOCK, NEW_BLOCK, 1)

# ── 2) 把 `RevertControl` 常量换成推导函数 ───────────────────────
OLD_DEF_START = '    /// <summary>\n    /// ④b「**改回去**」合成表：`"卡id|sprite"` —— 把这两笔落地的中文徽记**从计划表里抽掉**，\n'
i = s.find(OLD_DEF_START)
assert i > 0, "找不到 RevertControl 的 doc 起点"
j = s.find('    };\n', i)
assert j > i and j - i < 2000, (i, j, j - i)
j += len('    };\n')

NEW_DEF = (
'    /// <summary>\n'
'    /// ④b 的**控制集（推导式）**：`"卡id|sprite"` —— 对每一张卡取「`desc` 的 sprite 集合 ∩ `descZh` 的 sprite 集合」\n'
'    /// 里的每一枚。那些正是「**今天英文与中文两边都画着**」的徽记；把它们从 `descZh` 侧抽掉，④a 必须逐条认出来。\n'
'    ///\n'
'    /// <para>🔴 **2026-10-15（第三十二会话）从【手写 19 条】改成【推导】**。原表出处是\n'
'    ///   `A1360`（`资料/普查产出_第十四会话/W_A1360中文段首词.md` §一，5 条段首关键词）＋\n'
'    ///   `A1400`（同目录 `W_图标族8词.md` §四 1，`Invulnerable` 那 14 张）—— 它当时自陈「这 19 条今天都在真计划表的 `descZh` 里」。\n'
'    ///   ⚠️ 2026-10-15 起 `gen_icon_plan.py` 对**带官方记号**（`[[…]]`/`((…))`/`$$…$$`）的字段**整字段跳过**\n'
'    ///   （那批文本运行时走 `CardIcons.RewriteMarks`、**不查本表**）⇒ `descZh` 换代之后一大批卡落进那一支\n'
'    ///   ⇒ 手写那 19 条现读**只剩 5 条**在表里 ⇒ 抽掉那 5 条之外**等于没抽** ⇒ 这条灭自证**退化成空转**。\n'
'    ///   （⚠️ 改动前同池就已经是 5/19，**不是**那次改动引入的。）</para>\n'
'    ///\n'
'    /// <para>✅ **判据形式没变**：仍是「抽掉 ⇒ ④a 必须每一条都认出来」＋「实际抽掉的条数 == 控制集长度」（前提检查）。\n'
'    ///   改成推导之后，控制集永远与「**今天真画着的那一批**」同步 —— 数据换代不会再让它空转。\n'
'    ///   出处 = `资料/普查产出_第三十二会话/D3_IconSetup五条红.md` §一·5。</para>\n'
'    /// </summary>\n'
'    static List<string> RevertControlFor(PlanDoc doc)\n'
'    {\n'
'        var list = new List<string>();\n'
'        if (doc == null || doc.cards == null) return list;\n'
'        foreach (var c in doc.cards)\n'
'        {\n'
'            if (c == null || c.fields == null) continue;\n'
'            var en = new HashSet<string>();\n'
'            var zh = new HashSet<string>();\n'
'            foreach (var f in c.fields)\n'
'            {\n'
'                if (f == null || f.items == null) continue;\n'
'                HashSet<string> set = f.name == "desc" ? en : (f.name == "descZh" ? zh : null);\n'
'                if (set == null) continue;\n'
'                foreach (var it in f.items)\n'
'                    if (it != null && !string.IsNullOrEmpty(it.sprite)) set.Add(it.sprite);\n'
'            }\n'
'            foreach (var sp in en) if (zh.Contains(sp)) list.Add(c.id + "|" + sp);\n'
'        }\n'
'        list.Sort(System.StringComparer.Ordinal);\n'
'        return list;\n'
'    }\n'
)
s = s[:i] + NEW_DEF + s[j:]

io.open(P, "wb").write(s.encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
