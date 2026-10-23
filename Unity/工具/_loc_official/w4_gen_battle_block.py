# -*- coding: utf-8 -*-
"""W4：生成 `Core/Loc.cs` 要插入的那一块（**只生成文本，不改任何文件**）。
用法: python -I -X utf8 工具/_loc_official/w4_gen_battle_block.py <输出文件>
"""
import io
import os
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法: python -I -X utf8 工具/_loc_official/w4_gen_battle_block.py <输出文件>\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

OUT = sys.argv[1]


def cstr(s):
    return '"' + (s or "").replace("\\", "\\\\").replace('"', '\\"').replace("\r", "").replace("\n", "\\n") + '"'


def main():
    off = L.official()
    tab = L.loc_table()
    ours = set(k for k in tab if k.startswith("Battle/"))
    miss = sorted(k for k in off if k.startswith("Battle/") and k not in ours)

    lines = []
    lines.append("            // ============================================================ "
                 "🆕 **2026-10-14（第二十八会话 · `A1803`）：原版 `Battle/` 那一族【剩下的 105 条】**")
    lines.append("            //")
    lines.append("            // 🔴 **这一块为什么今天才填**：这 105 条键原来只在**代码字面量**那一支上"
                 "（载波②，见 `Battle/` 那一族开头那三句），")
    lines.append("            //   而**值在远端 I2 表** ⇒ 本地一条 value 都没有（几处注释写着「远端 CCD、拿不到」）。")
    lines.append("            //   2026-10-14 官方 zh-CN 表解开（`数据/游戏数据/官方本地化_en+zh-CN.json`）")
    lines.append("            //   ⇒ **两列都照官方表逐字录入**（⛔ 不是我们译的、⛔ 不是兜底句）。")
    lines.append("            //")
    lines.append("            // ⚠️ **值里带记号的那几条**（`I2` 占位符，`gen_official_loc.py` 的文件头写着）：")
    lines.append("            //   `[[vanguard]]` / `[[regeneration]]` / `[[camouflage]]` /"
                 " `<sprite name=Atlas_trait_icon_armour>`")
    lines.append("            //   —— 那是**原版运行时才替换**的记号。我们这边：`<sprite name=Atlas_…>` **能直接解析**")
    lines.append("            //   （TMP 精灵图集 `Resources/Fonts/Warpforge Trait TextSprites.asset` 里 70 个 "
                 "`Atlas_trait_icon_*` 全在），")
    lines.append("            //   而 `[[..]]` 还**没有**渲染层（那是 `A1763` 的活）⇒ 这几条照写官方值，"
                 "**接线之前先补那一层**。")
    lines.append("            //")
    lines.append("            // ⚠️ **消费侧现状（如实记，⛔ 别以为填了就会上屏）**：")
    lines.append("            //   · `Battle/Tips/*`：`BattleDriver.HintForCodeWithKey` 走 "
                 "`RuleCodes.TermKey(rc)` → `Loc.HasEntry(键)` 两道闸；")
    lines.append("            //     `TermKey` 今天**只映射 4 条**（`ErrCost`/`ErrNotTurn`/`ErrNoTargetAvailable`/"
                 "`ErrNotEnoughRoom`）")
    lines.append("            //     —— 那 4 条**已在表内**；本块新加的 Tips 键**要 `TermKey` 也跟着映射才会上屏**"
                 "（`RuleEngine/Core/RuleCodes.cs`，⛔ 不在本笔白名单）。")
    lines.append("            //   · `Battle/Effect/*`：`Battle/CardDisplayWindow.cs` 的 `AttrTerm` 只取 "
                 "`…OneTurn` 那 4 条（本块的 `ChangeArmour` 等**无后缀**那批= 永久改属性档，今天无消费点）。")
    lines.append("            //   · `Battle/Cemetery/*`（20 条）：消费侧要**先加 3 种引擎事件 + 给 `Play`/`Ability` "
                 "补目标与伏击标记**")
    lines.append("            //     （判据全文在 `Battle/BattleDriver.cs` 的 `RefreshBattleLog` 那段注释）⇒ "
                 "**本块只是把值备好**，⛔ 不是「已接」。")
    lines.append("            //   · `Battle/HUD/{LostConnectionMsg,MatchAlreadyFinished,PleaseWaitConnection,"
                 "WaitOpponentConnectionMsg}`：")
    lines.append("            //     联机那几处今天走的是 `NetRuntime.Notice` 自己的串 —— 换成词条要动 `Battle/` 与 `Net/`。")
    lines.append("            //   · 其余（`BattleEnd/` 8 · `MatchType/` 5 · `Prebattle/` 4 · `ChooseCard/` 3 · "
                 "`Settings/` 2 + 4 条裸 `Battle/`）：**今天无消费点**，值备好。")
    lines.append("            // 判据逐条 → `资料/普查产出_第二十八会话/W4_官方文案族.md`。")
    lines.append("")
    group = None
    for k in miss:
        g = k.rsplit("/", 1)[0] if "/" in k[len("Battle/"):] else "Battle/（裸键）"
        if g != group:
            group = g
            lines.append("            // ---- %s（%d 条）----"
                         % (g, sum(1 for x in miss if (x.rsplit("/", 1)[0] if "/" in x[len("Battle/"):] else "Battle/（裸键）") == g)))
        v = off[k]
        lines.append("            { %-56s new Entry(%s, %s) },"
                     % (cstr(k) + ",", cstr(v[1]), cstr(v[0])))
    lines.append("")

    io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    print("写出 %s（%d 条键 + %d 行注释）" % (OUT, len(miss), len(lines) - len(miss)))


if __name__ == "__main__":
    main()
