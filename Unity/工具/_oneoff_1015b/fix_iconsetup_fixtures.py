# -*- coding: utf-8 -*-
"""按 `D3_IconSetup五条红.md` 施工单改 `IconSetup.cs` 的 ②e / ②f2 / ②g / ②h 四处夹具。
判据：那些手写串配的卡今天 `descZh` 带官方记号 ⇒ `gen_icon_plan.py` 整字段跳过 ⇒ 计划表里没那个字段
⇒ `CardIcons.Rewrite` 原样返回 ⇒ 断言恒假。换成**走计划表那一代**的卡 + 现池真文本。"""
import io

P = r"d:/4/Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs"
s = io.open(P, encoding="utf-8", newline="").read()
n = 0


def rep(old, new, tag):
    global s, n
    c = s.count(old)
    assert c == 1, "%s：命中 %d" % (tag, c)
    s = s.replace(old, new, 1)
    n += 1
    print("  改好", tag)


# ── ②e（裸关键词）────────────────────────────────────────────
rep(
'        string ly = CardPresentation.CardIcons.Rewrite("SAU23", "descZh", "残骸。装甲 2。");\n',
'        // 🔴 **2026-10-15（第三十二会话）就地改夹具（铁律 5）**：原夹具用 `SAU23|descZh` + 手写串\n'
'        //   `残骸。装甲 2。` —— **过时**：`SAU23` 现读 `descZh = \'[[remnant]]。[[armour]] 2。[[vanguard]]\'`\n'
'        //   （**带官方记号**），而 `gen_icon_plan.py` 对带记号的字段**整字段跳过** —— 那批文本运行时走\n'
'        //   `RewriteMarks`、**根本不查本表**（见本函数开头 `HasOfficialMarks` 那一支）⇒ 计划表里\n'
'        //   `SAU23` 只剩 `desc` ⇒ `Rewrite` 原样返回 ⇒ 四个子句全不成立。\n'
'        //   换成**走计划表那一代**的卡：`SAU41|descZh = \'复生一个友方残骸。\'`\n'
'        //   （计划表现读 `descZh:[(\'残骸\',\'remnant\')]`，token 是**裸词**、不带方括号）。\n'
'        //   ⚠️ 判据里的 `残骸。` 跟着放宽成 `残骸`（token 本身不带句号）。\n'
'        string ly = CardPresentation.CardIcons.Rewrite("SAU41", "descZh", "复生一个友方残骸。");\n',
'②e 夹具')

rep(
'                 < ly.IndexOf("残骸。", System.StringComparison.Ordinal)\n'
'              && CardPresentation.CardIcons.StripTags(ly).Contains("残骸。"),\n',
'                 < ly.IndexOf("残骸", System.StringComparison.Ordinal)\n'
'              && CardPresentation.CardIcons.StripTags(ly).Contains("残骸"),\n',
'②e 判据')

# ── ②f2（圈码）──────────────────────────────────────────────
rep(
'        string circ = CardPresentation.CardIcons.Rewrite("ASH80", "descZh", "①：给一个友方部队");\n',
'        // 🔴 **2026-10-15（第三十二会话）就地改夹具（铁律 5）**：原夹具用 `ASH80|descZh` —— 同 ②e，\n'
'        //   它现读带官方记号（`[[waystone]]。…((Spirit_x,1)) …`）⇒ 计划表里没有 `descZh` 字段。\n'
'        //   换成 `ASH83|descZh = \'①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命\'`\n'
'        //   （计划表现读 `descZh:[(\'[攻击]\',\'Melee\'),(\'[远程]\',\'Ranged\'),(\'①\',\'SpiritStone_1\')]`，\n'
'        //    该字段**不带记号**、走计划表那一路）。\n'
'        string circ = CardPresentation.CardIcons.Rewrite("ASH83", "descZh",\n'
'                     "①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命");\n',
'②f2 夹具')

# ── ②g（幂等）───────────────────────────────────────────────
rep(
'        string g1 = CardPresentation.CardIcons.Rewrite("GOF101", "descZh",\n'
'                     "给予一个友方单位[践踏]。如果其已经拥有[践踏]，改为本回合给予其 +2[攻击]。");\n'
'        string g2 = CardPresentation.CardIcons.Rewrite("GOF101", "descZh", g1);\n'
'        // ⚠️ 这张卡的原文用的是**方括号**写法（`[践踏]`）⇒ 卡面上本来就**没有**「践踏」这个词，\n'
'        //    换完只剩图标才是对的（别指望这里能查到「践踏」）。\n',
'        // 🔴 **2026-10-15（第三十二会话）就地改夹具（铁律 5）**：原夹具喂的是手写中文串\n'
'        //   `给予一个友方单位[践踏]。…`，而 `GOF101` 现读 `descZh` 带官方记号\n'
'        //   （`给一个友方单位[[stomp]]。…((Melee_attack_icon_x,+2))`）⇒ 计划表里没有那个字段。\n'
'        //   换成**该卡 `desc` 那一半**（现读 = 下面这一串；计划表现读\n'
'        //   `desc:[(\'[Stomp]\',\'stomp\'),(\'[Stomp]\',\'stomp\'),(\'[Attack]\',\'Melee\')]`\n'
'        //   ⇒ 与下面「stomp ×2 + Melee ×1」的判据逐字对得上）。\n'
'        string g1 = CardPresentation.CardIcons.Rewrite("GOF101", "desc",\n'
'                     "Give [Stomp] Stomp to a friendly unit. If it already has [Stomp] Stomp, "\n'
'                     + "give it +2 [Attack] this turn instead");\n'
'        string g2 = CardPresentation.CardIcons.Rewrite("GOF101", "desc", g1);\n'
'        // ⚠️ 这张卡的原文用的是**方括号**写法（`[Stomp]`）⇒ 卡面上本来就**没有** `Stomp` 这个词，\n'
'        //    换完只剩图标才是对的（别指望这里能查到 `Stomp`）。\n',
'②g 夹具')

# ── ②h（跟随 ②e 换卡）───────────────────────────────────────
rep(
'        Check(ly == CardPresentation.CardIcons.Rewrite("SAU23", "descZh", ly),\n'
'              "②h 裸关键词那条也幂等", ref bad);\n',
'        // ⚠️ 跟着 ②e 换了卡 —— 原来 ②e 的 `ly` **根本没被换过**（那条恒真 = 空转），本条也就没在测。\n'
'        //    换过之后 `ly` 里含 `<link=remnant><nobr><sprite …>残骸</nobr></link>`，\n'
'        //    **这一条才真的在测幂等**（判据 = `CardIcons.cs` 那句 `if (s.IndexOf(bare) >= 0) continue;`）。\n'
'        Check(ly == CardPresentation.CardIcons.Rewrite("SAU41", "descZh", ly),\n'
'              "②h 裸关键词那条也幂等", ref bad);\n',
'②h 夹具')

io.open(P, "wb").write(s.encode("utf-8"))
b = io.open(P, "rb").read()
print("改了 %d 处 · CRLF %d LF %d" % (n, b.count(b"\r\n"), b.count(b"\n")))
