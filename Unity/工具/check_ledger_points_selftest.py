#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_ledger_points_selftest.py — 「落点形态闸」的回归自测（**只读、不碰真账**）。

怎么跑
------
    python -I -X utf8 d:/4/Unity/工具/check_ledger_points_selftest.py

· 全部数据都在 `tempfile.mkdtemp()` 里现造（假账 + 假目标文件），跑完 `rmtree` 清掉。
· **一条真账都不读**（`check_file` 直接吃临时路径），所以随时能跑、不会钉进半成品行号。
· 输出 `PASS n / FAIL m`；**任何一条 FAIL 都说明加固把老功能弄坏了**，退出码 1。

覆盖：2026-10-25 一天里 5 个代理各自撞到的 5 个盲点，每条**至少 1 正 1 反**
（散文括号 3 个正例），外加「四类判法」「退出码语义」「`--fix` 不往 getter 上改」
「不做全仓唯一同名兜底」。
"""
import contextlib
import io
import os
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import check_ledger_points as C  # noqa: E402

PASS = 0
FAIL = 0
BAD = []
HDR = "状态\t我们的落点\t原版类\t方法\t备注\n"

T_CS = """namespace N
{
    class T
    {
        public int Survivor { get { return KwValue(3); } }

        int KwValue(int x)
        {
            return x;
        }
    }
}
"""
U_CS = """class U
{
    void Awake() { Build(); }

    void Build()
    {
    }
}
"""
V_CS = """class V
{
    void RefreshDeckList()
    {
    }

    public void UiScrollDeck(float dy) { _s += dy; RefreshDeckList(); }

    void SetHint(string s) { SetHint(s, false); }

    void SetHint(string s, bool b)
    {
    }

    void ClaimForTest(int i)
    {
    }

    void Claim(int i)
    {
    }
}
"""
X_CS = """class X
{
    void SelectArmy(int i)
    {
    }
}
"""
# 🔴 盲点 6（2026-10-25 R1）：目标行是「字段 + 初始化器」—— `find_decl_candidates` 会接受，
#    但 `dn_at`（verify_annot 同款）解析不出声明名 ⇒ `--fix` 必须跳过（NEED-HUMAN）。
W_CS = """class W
{
    static readonly int[] ClaimTable = new int[]{1, 2};

    void Claim(int i)
    {
    }
}
"""
D_JSON = '{"a": 1}\n'
# 🔴 盲点 7（2026-10-25 R1）：**裸 `\r`（`\r\r\n`）** —— 文本模式的 universal newlines 会把它也当换行
#    ⇒ 其后所有行号整体偏大（实测 `RuleEngine/Core/DeckRules.cs` 有 7 个裸 CR ⇒ 行号偏 +7，
#      `--fix` 把 15 行账改成了错行、而且闸变绿）。工具必须按二进制读、只把 `\r\n` 归成 `\n`。
R_CS = b"class R\r\n{\r\n        int x = 1;\r\r\n        void Foo()\r\n        {\r\n        }\r\n        void Bar()\r\n        {\r\n        }\r\n}\r\n"

TMP = None
APP = None
CASEFILE = None


def _write(path, text):
    io.open(path, "w", encoding="utf-8", newline="\n").write(text)


def ok(name, cond, detail=""):
    global PASS, FAIL
    if cond:
        PASS += 1
        print("PASS  " + name)
    else:
        FAIL += 1
        BAD.append(name)
        print("FAIL  " + name + (("  <- " + str(detail)) if detail else ""))


def run(landing, do_fix=False, status="A", header=HDR):
    """把一条假账行喂给 `check_file`，返回分档结果。"""
    _write(CASEFILE, header + "%s\t%s\tFakeClass\tFakeMethod\t\n" % (status, landing))
    total, fails, drifts, rewrites, getters, warns = C.check_file(CASEFILE, [APP], False, do_fix)
    cls = "F" if fails else ("D" if drifts else ("W" if warns else "P"))
    return {
        "cls": cls, "total": total, "fails": fails, "drifts": drifts,
        "rewrites": rewrites, "getters": getters, "warns": warns,
        "why": (fails or drifts or [(None, None, None, None, w[4]) for w in warns] or [(None, None, None, None, "")])[0],
        "kinds": set(w[3] for w in warns),
    }


def case(name, landing, want, kind=None, do_fix=False):
    r = run(landing, do_fix)
    good = r["cls"] == want
    if kind is not None:
        good = good and (kind in r["kinds"] if kind != "-" else not r["kinds"])
    det = "want=%s%s got=%s kinds=%s why=%s" % (
        want, ("+" + kind) if kind else "", r["cls"], sorted(r["kinds"]), str(r["why"])[:90])
    ok(name, good, det)
    return r


def main():
    global TMP, APP, CASEFILE
    TMP = tempfile.mkdtemp(prefix="wf_ledger_pts_selftest_")
    APP = os.path.join(TMP, "app")
    os.makedirs(APP)
    CASEFILE = os.path.join(TMP, "_case.tsv")
    _write(os.path.join(APP, "T.cs"), T_CS)
    _write(os.path.join(APP, "U.cs"), U_CS)
    _write(os.path.join(APP, "V.cs"), V_CS)
    _write(os.path.join(APP, "X.cs"), X_CS)
    _write(os.path.join(APP, "d.json"), D_JSON)
    try:
        # ------------------------------------------------------------------
        # 盲点 1：一行式属性 getter 被当成方法声明
        # ------------------------------------------------------------------
        case("1+ getter 当落点：通过 + 告警（⛔ 不许变失败）", "T.cs:5（`Survivor`）", "W", C.WARN_GETTER)
        case("1- 普通方法落点：不报 getter", "T.cs:7（`KwValue`）", "P", "-")
        #   历史事故：`--fix` 曾把 KwValue 的漂移「修正」到 Survivor 那个属性上
        r = case("1+ KwValue 的漂移指向 getter 行 ⇒ 漂移、⛔ 不许当通过", "T.cs:5（`KwValue`）", "D")
        ok("1+ …且实际行号 = 7", "实际在 7 行" in str(r["why"]), r["why"])
        r = case("1+ --fix 不往 getter 上改（Survivor 在 5，账写 7）", "T.cs:7（`Survivor`）", "D", do_fix=True)
        ok("1+ …rewrites 为空", r["rewrites"] == [], r["rewrites"])
        ok("1+ …漂移理由里写明「--fix 不自动改」", "不自动改" in str(r["why"]), r["why"])

        # ------------------------------------------------------------------
        # 盲点 2：「一行内含声明 + 调用」（`void Awake() { Build(); }`）
        # ------------------------------------------------------------------
        r = case("2+ Awake 那行里的 Build() 不算 Build 的声明", "U.cs:3（`Build`）", "D")
        ok("2+ …实际定位到 5 行", "实际在 5 行" in str(r["why"]), r["why"])
        case("2- Build 的真声明行：通过", "U.cs:5（`Build`）", "P")

        # ------------------------------------------------------------------
        # 盲点 3：调用行被当成声明（RefreshDeckList 两处 / SetHint 三处）
        # ------------------------------------------------------------------
        r = case("3+ UiScrollDeck 那行里的 RefreshDeckList() 不算声明", "V.cs:7（`RefreshDeckList`）", "D")
        ok("3+ …实际定位到 3 行", "实际在 3 行" in str(r["why"]), r["why"])
        case("3- 真实现行：通过", "V.cs:3（`RefreshDeckList`）", "P")
        case("3+ 既声明又调用同名 ⇒ 通过 + SELFREF 告警", "V.cs:9（`SetHint`）", "W", C.WARN_SELFREF)

        # ------------------------------------------------------------------
        # 盲点 4：散文括号不验同名（`…:764（Refresh 方法）`）
        # ------------------------------------------------------------------
        r = case("4+ 中文分隔的散文名对不上 ⇒ 失败", "X.cs:3（Refresh 方法）", "F")
        ok("4+ …失败理由点名两个名字", "Refresh" in str(r["why"]) and "SelectArmy" in str(r["why"]), r["why"])
        case("4+ 中文冒号分隔", "X.cs:3（Refresh：旧名）", "F")
        case("4+ 英文标点分隔", "X.cs:3（Refresh/旧名）", "F")
        case("4- 散文名对得上 ⇒ 通过", "X.cs:3（等价实现 = SelectArmy：换将入口）", "P")
        case("4- 纯散文提不出标识符 ⇒ 只告警、不失败", "X.cs:3（判据逐句 :1-2）", "W", C.WARN_PROSE)
        case("4- 散文里多个标识符 ⇒ 只告警、不失败",
             "X.cs:3（等价实现 = SelectArmy + Foo.Bar 两处）", "W", C.WARN_PROSE)

        #   名字写在括号**之前**（账里 36 行的形状）
        case("4+1 行号后的名字对得上 ⇒ 通过", "X.cs:3 SelectArmy(int i)（换将入口）", "P")
        case("4+1 有它时括号里的散文词⛔不拿来判", "X.cs:3 SelectArmy（关窗钮 + 全屏透明 _bgClose）", "P")
        case("4+1 行号后的名字对不上 ⇒ 失败", "X.cs:3 Show（关窗钮 + 全屏透明 _bgClose）", "F")

        # ------------------------------------------------------------------
        # 盲点 5：`*ForTest` 转发口
        # ------------------------------------------------------------------
        case("5+ 落点是 ForTest 口 ⇒ 通过 + 告警（⛔ 不许变失败）", "V.cs:15（`ClaimForTest`）", "W", C.WARN_FORTEST)
        case("5- 普通方法不报 ForTest", "V.cs:19（`Claim`）", "P", "-")

        # ------------------------------------------------------------------
        # 盲点 6：`--fix` 的目标行**必须能解析成声明**（`dn_at` 同款）；2026-10-25 R1 加
        #   起因：Q3 投诉「工具报的『实际在 N 行』有 18/79 落在 `///` / `{` / `}` / 空行 / 别的方法上
        #   ⇒ 直接 `--fix` 会改到错的位置、而且闸变绿」。
        # ------------------------------------------------------------------
        _write(os.path.join(APP, "W.cs"), W_CS)
        ok("6- dn_at_name：普通方法 ⇒ 名字", C.dn_at_name("        void Claim(int i)") == "Claim")
        ok("6- dn_at_name：属性（`{` 紧跟）⇒ 名字", C.dn_at_name("    public int Survivor { get { return KwValue(3); } }") == "Survivor")
        ok("6+ dn_at_name：`///` 注释 ⇒ None", C.dn_at_name("        /// <summary>") is None)
        ok("6+ dn_at_name：只有 `{` ⇒ None", C.dn_at_name("        {") is None)
        ok("6+ dn_at_name：只有 `}` ⇒ None", C.dn_at_name("        }") is None)
        ok("6+ dn_at_name：空行 ⇒ None", C.dn_at_name("") is None)
        ok("6+ dn_at_name：字段 + 初始化器 ⇒ None", C.dn_at_name("    static readonly int[] ClaimTable = new int[]{1, 2};") is None)
        # 负例（任务书点名）：目标行是 `///` 时 ⇒ 不许算「已修正」
        _wl = ["class A", "{", "        /// <summary>", "        void Claim(int i) { }", "}"]
        ok("6+ 负例：目标行是 `///` ⇒ fix_block_reason 报 NEED-HUMAN",
           "NEED-HUMAN" in C.fix_block_reason(_wl, 3), C.fix_block_reason(_wl, 3))
        ok("6+ 负例：目标行是 `}` / 空行 ⇒ 也报 NEED-HUMAN",
           "NEED-HUMAN" in C.fix_block_reason(_wl, 5) and "NEED-HUMAN" in C.fix_block_reason(_wl, 0 + 1))
        ok("6- 正常方法行 ⇒ fix_block_reason 放行（空串）", C.fix_block_reason(_wl, 4) == "", C.fix_block_reason(_wl, 4))
        # 端到端：字段行是唯一「候选」，`--fix` 也必须一个字都不改
        r = case("6+ 目标行是字段（候选但解析不出声明）⇒ 漂移、⛔ 不许 --fix",
                 "W.cs:5（`ClaimTable`）", "D", do_fix=True)
        ok("6+ …rewrites 为空", r["rewrites"] == [], r["rewrites"])
        ok("6+ …漂移理由里写明 NEED-HUMAN", "NEED-HUMAN" in str(r["why"]), r["why"])

        # ------------------------------------------------------------------
        # 盲点 7：裸 `\r`（`\r\r\n`）不许让行号漂 —— 工具必须【二进制读】
        #   负例判据：文本模式读时 `Foo` 会算成第 5 行；二进制读必须算第 4 行。
        # ------------------------------------------------------------------
        _raw = os.path.join(APP, "R.cs")
        io.open(_raw, "wb").write(R_CS)      # ⚠️ 二进制写，\r\r\n 原样落盘
        _rl = C.read_src_lines(_raw)
        ok("7+ read_src_lines：`\\r\\r\\n` 之后行号不漂（`void Foo` 在第 4 行）",
           _rl[3].strip().startswith("void Foo"), _rl[:6])
        ok("7+ read_src_lines：`void Bar` 在第 7 行（文本模式会算成第 8 行）",
           _rl[6].strip().startswith("void Bar"), [_x.strip() for _x in _rl[:9]])
        ok("7+ read_src_lines：裸 `\\r` 留在行内、不当换行",
           "\r" in _rl[2] and len(_rl) == 11, (len(_rl), repr(_rl[2])))
        ok("7+ find_decl_candidates：Foo 唯一命中 4", C.find_decl_candidates(_rl, "Foo") == [4],
           C.find_decl_candidates(_rl, "Foo"))
        case("7+ 账写 `R.cs:4（Foo）` ⇒ 通过（⛔ 不许因裸 CR 判成漂移到 5）", "R.cs:4（`Foo`）", "P")
        case("7+ 账写 `R.cs:7（Bar）` ⇒ 通过", "R.cs:7（`Bar`）", "P")
        case("7+ 反证：账写 `R.cs:5（Foo）` ⇒ 漂移（真值 4）", "R.cs:5（`Foo`）", "D")

        # ------------------------------------------------------------------
        # 基本失败 / 通过（老功能不许坏）
        # ------------------------------------------------------------------
        case("基础：文件不存在 ⇒ 失败", "Nope.cs:1（Foo）", "F")
        case("基础：行号越界 ⇒ 失败", "T.cs:999（Foo）", "F")
        case("基础：只有 `{` 的行 ⇒ 失败", "T.cs:2（Foo）", "F")
        case("基础：空行 ⇒ 失败", "T.cs:6（Foo）", "F")
        case("基础：落点里没有 `文件:行` ⇒ 失败", "只有一个散文落点", "F")
        case("基础：数据文件非空行 ⇒ 通过（不查名字）", "d.json:1（Foo）", "P")
        case("基础：数据文件空行 ⇒ 失败", "d.json:2（Foo）", "F")
        case("纪律：⛔ 不做「全仓唯一同名」兜底（Build 在 U.cs 有，X.cs 不算）",
             "X.cs:3（Build）", "F")

        # 表头缺列 / 空账：不许抛异常（老版本返回 4 元组会解包崩）
        _write(CASEFILE, "只有一列\n")
        res = C.check_file(CASEFILE, [APP], False, False)
        ok("基础：表头缺列 ⇒ 记 1 条失败、返回 6 元组", len(res) == 6 and len(res[1]) == 1, res)
        _write(CASEFILE, "")
        res = C.check_file(CASEFILE, [APP], False, False)
        ok("基础：空账 ⇒ 返回 6 元组、total=0", len(res) == 6 and res[0] == 0, res)

        # ------------------------------------------------------------------
        # 四类判法在同一批里能分开
        # ------------------------------------------------------------------
        _write(CASEFILE, HDR
               + "A\t%s\tC1\tM1\t\n" % "U.cs:3（`Build`）"                 # 漂移
               + "A\t%s\tC2\tM2\t\n" % "X.cs:3（Refresh 方法）"            # 失败
               + "A\t%s\tC3\tM3\t\n" % "T.cs:5（`Survivor`）"              # 告警
               + "A\t%s\tC4\tM4\t\n" % "T.cs:7（`KwValue`）"               # 通过
               + "B\t%s\tC5\tM5\t\n" % "Nope.cs:1（Foo）")                 # 非 A 行不参与
        total, fails, drifts, rewrites, getters, warns = C.check_file(CASEFILE, [APP], False, False)
        ok("四类：A=4 / 失败 1 / 漂移 1 / 告警 1（通过含告警 = 3）",
           (total, len(fails), len(drifts), len(warns)) == (4, 1, 1, 1),
           (total, len(fails), len(drifts), len(warns)))
        ok("四类：getter 旧接口仍返回那一条", len(getters) == 1, getters)

        # ------------------------------------------------------------------
        # 退出码语义：0 = 无失败（漂移/告警不算）· 1 = 有失败 · 2 = 参数错
        # ------------------------------------------------------------------
        driftf = os.path.join(TMP, "drift.tsv")
        _write(driftf, HDR + "A\t%s\tC\tM\t\n" % "U.cs:3（`Build`）")      # 只有漂移
        failf = os.path.join(TMP, "fail.tsv")
        _write(failf, HDR + "A\t%s\tC\tM\t\n" % "X.cs:3（Refresh 方法）")   # 有失败
        warnf = os.path.join(TMP, "warn.tsv")
        _write(warnf, HDR + "A\t%s\tC\tM\t\n" % "T.cs:5（`Survivor`）")      # 只有告警

        def exit_of(argv):
            old = sys.argv[:]
            sys.argv = ["check_ledger_points.py"] + argv
            buf = io.StringIO()
            try:
                with contextlib.redirect_stdout(buf):
                    return C.main()
            finally:
                sys.argv = old

        ok("退出码：只有漂移 ⇒ 0", exit_of([driftf, "--root", APP]) == 0)
        ok("退出码：只有告警（+--warn-getters）⇒ 0", exit_of([warnf, "--root", APP, "--warn-getters"]) == 0)
        ok("退出码：有失败 ⇒ 1", exit_of([failf, "--root", APP]) == 1)
        ok("退出码：账本给错 ⇒ 2", exit_of([os.path.join(TMP, "nope.tsv"), "--root", APP]) == 2)

        # ------------------------------------------------------------------
        # --fix：正常漂移该改；改完账里其它列不动
        # ------------------------------------------------------------------
        fixf = os.path.join(TMP, "fix.tsv")
        _write(fixf, HDR + "A\t%s\tC\tM\t\n" % "T.cs:9（`KwValue`）")
        before = io.open(fixf, encoding="utf-8", newline="").read()
        exit_of([fixf, "--root", APP, "--fix"])
        after = io.open(fixf, encoding="utf-8", newline="").read()
        ok("--fix：普通漂移就地改正 9 -> 7", "T.cs:7（`KwValue`）" in after, after.strip())
        ok("--fix：行尾仍是 LF、列数不变", after.count("\n") == before.count("\n") and "\r" not in after)
        fixf2 = os.path.join(TMP, "fix2.tsv")
        _write(fixf2, HDR + "A\t%s\tC\tM\t\n" % "T.cs:7（`Survivor`）")
        before2 = io.open(fixf2, encoding="utf-8", newline="").read()
        exit_of([fixf2, "--root", APP, "--fix"])
        after2 = io.open(fixf2, encoding="utf-8", newline="").read()
        ok("--fix：目标行是 getter ⇒ 账一个字都不改", before2 == after2, after2.strip())
        # 盲点 6 的端到端：目标行是字段（候选但 `dn_at` 解析不出声明）⇒ 账一个字都不改
        fixf3 = os.path.join(TMP, "fix3.tsv")
        _write(fixf3, HDR + "A\t%s\tC\tM\t\n" % "W.cs:5（`ClaimTable`）")
        before3 = io.open(fixf3, encoding="utf-8", newline="").read()
        exit_of([fixf3, "--root", APP, "--fix"])
        after3 = io.open(fixf3, encoding="utf-8", newline="").read()
        ok("--fix：目标行解析不出声明 ⇒ 账一个字都不改（NEED-HUMAN 跳过）", before3 == after3, after3.strip())
        # 🆕 2026-10-28（B93_E4）：`--fix` 的**重定位语义一个字没动** —— 它只改第一处（落点）的行号，
        #   ⛔ 绝不碰次要落点（证据指针）。这条样例的第一处会漂（9 → 7）、第二处故意写坏（S.cs:7 空行）。
        fixf4 = os.path.join(TMP, "fix4.tsv")
        _write(fixf4, HDR + "A\t%s\tC\tM\t\n" % "T.cs:9（`KwValue`） · S.cs:7")
        exit_of([fixf4, "--root", APP, "--fix"])
        after4 = io.open(fixf4, encoding="utf-8", newline="").read()
        ok("--fix：只改正第一处（9→7），次要落点 `S.cs:7` 逐字不动",
           "T.cs:7（`KwValue`） · S.cs:7" in after4, after4.strip())

        # ------------------------------------------------------------------
        # 辅助函数单测（形态判据的底）
        # ------------------------------------------------------------------
        ok("decl_name_at：一行式属性", C.decl_name_at("    public int Survivor { get { return KwValue(3); } }") == "Survivor")
        ok("decl_name_at：方法", C.decl_name_at("        void SetHint(string s, bool opaque)") == "SetHint")
        ok("decl_name_at：表达式体属性", C.decl_name_at("    public static Vector2 P => new Vector2(1, 2);") == "P")
        ok("decl_name_at：带初始化表达式的字段", C.decl_name_at("    static readonly Foo[] A = new Foo[3];") == "A")
        ok("decl_name_at：调用行 ⇒ None", C.decl_name_at("        RefreshDeckList();") is None)
        ok("decl_name_at：`{` 行 ⇒ None", C.decl_name_at("        {") is None)
        ok("paren_idents：反引号包裹", C.paren_idents("a.cs:1（`Dispatch`）") == (["Dispatch"], "exact"))
        ok("paren_idents：散文里恰好一个", C.paren_idents("a.cs:1（Refresh 方法）") == (["Refresh"], "prose"))
        ok("head_name：参数式", C.head_name("a.cs:642 ShowError(text, loc)", C.REF.search("a.cs:642 ShowError(text, loc)")) == "ShowError")
        ok("head_name：括号紧跟", C.head_name("a.cs:750 Show（关窗钮）", C.REF.search("a.cs:750 Show（关窗钮）")) == "Show")
        ok("head_name：落点直接进括号 ⇒ None", C.head_name("a.cs:1（Foo）", C.REF.search("a.cs:1（Foo）")) is None)

        # ------------------------------------------------------------------
        # 🆕 2026-10-28（B93_E4）三个新盲点的构造性自检
        #   判据 = `资料/完全复刻/报告/F2_落点失败收尾.md` §④ 的 1 / 2 / 3
        #   外形 = 「**修前必红、修后必绿**」：修前那三红的实证见 B93_E4 报告
        #   （同一批样例喂给 `.bak_20261028` 那个版本），本文件钉的是「修后」的行为。
        # ------------------------------------------------------------------
        S_CS = (
            "class S\n"
            "{\n"
            "    void Good()\n"
            "    {\n"
            "        Helper();\n"
            "    }\n"
            "\n"
            "    // Helper 在这里只是被提了一句（注释行）\n"
            "    void Helper()\n"
            "    {\n"
            "    }\n"
            "}\n"
        )
        B_CS = (
            "public sealed class Badge : InventoryBadge { }\n"
            "\n"
            "public sealed record Match : BadgeNotification { }\n"
        )
        P_CS = (
            "class P\n"
            "{\n"
            "    bool Flag\n"
            "    {\n"
            "        get { return Kw(1) != 0; }\n"
            "    }\n"
            "\n"
            "    int Count => Kw(3);\n"
            "\n"
            "    int Kw(int x)\n"
            "    {\n"
            "        return x;\n"
            "    }\n"
            "\n"
            "    void Body()\n"
            "    {\n"
            "        int local = 5;\n"
            "        Kw(local);\n"
            "    }\n"
            "\n"
            "    bool NotAProp\n"
            "        = false;\n"
            "}\n"
        )
        _write(os.path.join(APP, "S.cs"), S_CS)
        _write(os.path.join(APP, "B.cs"), B_CS)
        _write(os.path.join(APP, "P.cs"), P_CS)

        # ---- 盲点①：落点列里第二处及以后的 `文件:行` 也要机检 ----
        # 修前这些**全部通过**（`REF.search` 只取第一处 ⇒ 后面那截根本没看 = 空转绿灯）。
        r = case("B93-1+ 次要落点指向空行（`S.cs:7`）⇒ 失败", "S.cs:3（`Good`） · S.cs:7", "F")
        ok("B93-1+ …理由点名空行", "空行" in str(r["why"]), r["why"])
        r = case("B93-1+ 次要落点行号越界 ⇒ 失败", "S.cs:3（`Good`） · S.cs:9999", "F")
        ok("B93-1+ …理由点名越界", "越界" in str(r["why"]), r["why"])
        r = case("B93-1+ 次要落点文件不存在 ⇒ 告警（可能是原版源码引用，⛔ 不算失败）",
                 "S.cs:3（`Good`） · NoSuchFile.cs:3", "W", C.WARN_SECOND)
        ok("B93-1+ …理由点名「找不到 / 不存在」",
           ("不存在" in str(r["why"]) or "找不到" in str(r["why"])), r["why"])
        r = case("B93-1+ 次要落点声称的名字不在那一行 ⇒ 告警",
                 "S.cs:3（`Good`） · S.cs:10（`Helper`）", "W", C.WARN_SECOND)
        ok("B93-1+ …告警里报出它实际在哪（9）", "9" in str(r["why"]), r["why"])
        # 反例（⛔ 不许过火）
        case("B93-1- 次要落点是**调用行**（`Helper();` 在 5 行）⇒ 通过、不告警",
             "S.cs:3（`Good`） · S.cs:5（`Helper`）", "P", "-")
        case("B93-1- 次要落点是**注释行但提了那个名字**（8 行）⇒ 通过、不告警",
             "S.cs:3（`Good`） · S.cs:8（`Helper`）", "P", "-")
        case("B93-1- 次要落点与第一处重复（同一 `文件:行`）⇒ 不算两遍",
             "S.cs:3（`Good`） · S.cs:3（`Good`）", "P", "-")
        case("B93-1- 次要落点没声称名字、那行非空 ⇒ 通过（散文不硬套）",
             "S.cs:3（`Good`） · S.cs:5（实现细节两处 Foo Bar）", "P", "-")

        # ---- 盲点②：带基类列表的 `class X : Y` ----
        # 修前：候选正则只认 `( < { ; = $`，名字后面是 `:` ⇒ 判「找不到这份声明」= 假失败
        #   （F2 报告 §④2 点名的就是这条：`BoosterPackBadge` / `ChallengeMatch` 两条 record）。
        case("B93-2+ `class Foo : Bar { }` 能当落点", "B.cs:1（`Badge`）", "P")
        case("B93-2+ `record Foo : Bar { }` 能当落点", "B.cs:3（`Match`）", "P")
        case("B93-2- 反例：**基类名**不是声明（`InventoryBadge` 只是 `Badge` 的基类）",
             "B.cs:1（`InventoryBadge`）", "F")
        ok("B93-2+ find_decl_candidates：`Badge` 命中 1 行",
           C.find_decl_candidates(C.read_src_lines(os.path.join(APP, "B.cs")), "Badge") == [1],
           C.find_decl_candidates(C.read_src_lines(os.path.join(APP, "B.cs")), "Badge"))

        # ---- 盲点③：隐式 private / 一行式 getter ----
        # 修前：`bool Flag`（`{` 在下一行）判「既无 `(` 也无 `=>`/`{`」、
        #       `int Count => Kw(3);` 判「`)` 后接的是语句」⇒ 两条**假失败**。
        _pc = C.read_src_lines(os.path.join(APP, "P.cs"))
        case("B93-3+ 隐式 private 的名字行属性（`bool Flag` + 下一行 `{`）⇒ 告警、⛔ 不算失败",
             "P.cs:3（`Flag`）", "W", C.WARN_MEMBER)
        case("B93-3+ 一行式表达式体属性（`int Count => Kw(3);`）⇒ 告警、⛔ 不算失败",
             "P.cs:8（`Count`）", "W")
        # 反例（⛔ 不许放宽）
        case("B93-3- 方法体里的局部变量（`int local = 5;`）仍然失败",
             "P.cs:17（`local`）", "F")
        case("B93-3- 方法体里的调用语句（`Kw(local);`）仍然失败 —— 不声称名字，只验形态",
             "P.cs:18（两处实现细节）", "F")
        case("B93-3- 隐式 private 的**裸字段**（`bool NotAProp` 换行 `= false;`）仍然失败"
             "（与局部变量同形 ⇒ 故意不认）", "P.cs:21（`NotAProp`）", "F")
        ok("B93-3+ member_decl_kind：名字行属性 + 下一行 `{` ⇒ property",
           C.member_decl_kind("    bool Flag", _pc, 2) == "property",
           C.member_decl_kind("    bool Flag", _pc, 2))
        ok("B93-3+ member_decl_kind：名字行属性但**下一行不是 `{`** ⇒ None（⛔ 不猜）",
           C.member_decl_kind("    bool NotAProp", _pc, 20) is None,
           C.member_decl_kind("    bool NotAProp", _pc, 20))
        ok("B93-3+ member_decl_kind：表达式体属性 ⇒ property",
           C.member_decl_kind("    int Count => Kw(3);") == "property",
           C.member_decl_kind("    int Count => Kw(3);"))
        ok("B93-3- member_decl_kind：裸字段 ⇒ None（不认）",
           C.member_decl_kind("    int local = 5;") is None, C.member_decl_kind("    int local = 5;"))
        ok("B93-3- member_decl_kind：语句 ⇒ None", C.member_decl_kind("    Kw(local);") is None,
           C.member_decl_kind("    Kw(local);"))
    finally:
        shutil.rmtree(TMP, ignore_errors=True)

    print("\nPASS %d / FAIL %d" % (PASS, FAIL))
    if BAD:
        print("失败项：" + " / ".join(BAD))
    return 0 if FAIL == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
