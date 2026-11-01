#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""assert_selfproof_census.py —— **只读**普查：找「断言期望值 = 我们自己的读取函数」这类**灭不掉自证**的写法。

为什么（新账 `B-112`）
----------------------
`B111_F1B` 撞到一例：`CardGlitchProbe` 原来写
    `var noise = CardArt.CardbackSdfNoise();  Check(实际 == noise, ...)`
—— **期望值与实现是同一个函数** ⇒ 两边一起错也绿；绿只证明「我们错得一致」。
本项目已因此栽过不止一次（「断言自证」与「断言同义反复」是同一族）。

判据（两档）
------------
* **`高置信`**：同一个表达式在断言里**出现两次**（两种形状）
  ① 同一次 `Check(a, b, …)` 里两个**值实参**文本相同（忽略空白）**或代入赋值后相同**；
  ② `Check(lhs == rhs, …)` 的 `lhs` / `rhs` 在把「同文件内（**整文件**）的赋值」代进去之后**文本相同**，
     且该表达式里含一个**本工程类型**的方法调用。
* **`待人工判`**：断言里用到的标识符，在**同文件内（整文件）**由 `<本工程类型>.<方法>(…)` 赋值
  （类型不在 Unity / BCL / 第三方白名单里）—— 可能是自证，也可能那个函数读的是**外部真值**
  （例如它读的是解包 JSON / 原版资产），必须人工看一眼。

🔴 **2026-11-01（账 `B-122`）：补两个盲点**
* **盲点①**：赋值表原来只扫「断言前 20 行」、且 `ASSIGN_RE` 认不出**字段/属性初始化器**
  ⇒ 「赋值离断言 >20 行 / 在字段里」的自证形全漏。现在 = **整文件** + 字段/自动属性/表达式体属性。
* **盲点②**：`cond_idx` 原来硬编码 `1 if name == "MenuCheck.True" else 0`
  ⇒ 池内 **8 处**「**首参是消息**」的签名（7 × `Check(string what, bool ok)` +
  `Check(string what, bool ok, float measured)`）被读反：**消息当条件、真条件 `got == want` 整条不看**。
  现在按 `DECL_RE` 解析出的**签名**判「哪个位置是 cond / msg / 值」；
  `Check(string name, BattleDoors.Result r)`（`VideoProbe.cs:66`）这类首参非条件的签名按「无条件」处理，
  ⛔ 不再误判成「消息当条件」。

⛔ **本工具只读**：不写任何 `.cs`、不改任何东西。唯一可选的写盘是 `--out <文件>`（你自己指定的路径）。

用法（**不带参 = 只打印本段并退出 0**；`-h/--help` 同）
--------------------------------------------------------
  python -I -X utf8 d:/4/Unity/工具/assert_selfproof_census.py                 # 打印用法
  python -I -X utf8 d:/4/Unity/工具/assert_selfproof_census.py --scan          # 跑普查，打摘要
  ... --scan --top 40              每条明细最多印几条（默认 20）
  ... --scan --only 高置信          只印一档（`高置信` / `待人工判` / `all`）
  ... --scan --out d:/4/_tmp_view/b112/census.tsv     可选：把两档全量落 TSV（**显式才写**）
  ... --scan --var-window 20                         赋值表退回「断言前 20 行」（旧口径，默认 0 = 整文件）
  ... --scan --assets d:/4/Unity/MyGame/Assets        换扫描根

扫描范围：`<assets>/**/Editor/*_Assert.cs` 与 `<assets>/**/Editor/*.cs`。
⚠️ **一律二进制读 + 自己按 `\\r\\n` / `\\n` 切**（本工程踩过裸 CR 让行号整体偏移）。
"""
import argparse
import io
import os
import re
import sys

DEFAULT_ASSETS = r"d:/4/Unity/MyGame/Assets"

# 断言入口（`MenuCheck.True` 是本工程另一套同义口）
CALL_RE = re.compile(r"(?:^|[^\w.])(?P<name>MenuCheck\.True|CheckTrue|CheckEq|Check|Ok)\s*\(")
# 🆕 2026-11-01（账 `B-122` 盲点②）：容忍泛型 —— 池内有 `static void Check<T>(T got, T want, string msg)`
#   （`DeckScene.cs:82` 那一族）。不收它的话，这类文件的 `want` 位置只能靠旧启发式猜。
DECL_RE = re.compile(
    r"\bstatic\s+(?:void|bool)\s+(?:MenuCheck\.True|CheckTrue|CheckEq|Check|Ok)\s*(?:<[^<>]*>\s*)?\(")

# `<本工程类型>.<方法>(` —— 类型名后面紧跟 `.` 和「大写开头的方法名」再来一个 `(` 或 `<`
PROJ_CALL_RE = re.compile(r"\b(?P<t>[A-Za-z_]\w*)\s*\.\s*(?P<m>[A-Za-z_]\w*)\s*(?:<[^()]*>)?\s*\(")

# 不是「本工程类型」的外部来源（Unity / BCL / 第三方 / 纯语法糖）
EXTERNAL_TYPES = {
    # Unity
    "UnityEngine", "UnityEditor", "Mathf", "Vector2", "Vector3", "Vector4", "Color", "Color32",
    "Quaternion", "Rect", "Debug", "GameObject", "Transform", "RectTransform", "Camera",
    "Resources", "Application", "Time", "Random", "Object", "Material", "Shader", "Texture2D",
    "Sprite", "Font", "ScriptableObject", "JsonUtility", "Screen", "Input", "Physics",
    "AssetDatabase", "EditorUtility", "Selection", "EditorSceneManager", "AssetImporter",
    "PrefabUtility", "PlayerSettings", "EditorApplication", "EditorGUIUtility", "Handles",
    "TextAsset", "TextMeshPro", "TMP_Text", "TMP_FontAsset", "TMP_Settings", "LayerMask",
    "AnimationCurve", "AudioClip", "AudioSource", "ParticleSystem", "Renderer", "Mesh",
    "Graphics", "GL", "GUI", "GUILayout", "GUIUtility", "Event", "FontStyle", "TextAnchor",
    "SceneManager", "QualitySettings", "RenderSettings", "Gizmos", "Bounds", "Plane", "Ray",
    # BCL
    "System", "String", "string", "Math", "MathF", "Convert", "Enum", "Array", "List",
    "Dictionary", "HashSet", "StringComparer", "CultureInfo", "Encoding", "Path", "File",
    "Directory", "Guid", "DateTime", "TimeSpan", "Regex", "Environment", "Console",
    "StringBuilder", "Tuple", "Nullable", "Activator", "Type", "Object", "Int32", "Int64",
    "Single", "Double", "Boolean", "Byte", "Char", "Decimal", "Linq", "Enumerable",
    # NUnit
    "Assert", "CollectionAssert", "StringAssert",
    # 常见字段/局部名（避免把 `x.Foo()` 里的 x 当成类型）
    "this", "base",
}

STR_LIT_RE = re.compile(r'^"')

# 🆕 2026-11-01（账 `B-122` 盲点①）：赋值表改**整文件**扫描，并且要收**字段/属性初始化器**。
#   旧 `ASSIGN_RE` 只认「[var|一个类型] 名字 = 右值;」，于是下面这些一律匹配不上：
#     `static readonly float Foo = Bar();`     （多个修饰符）
#     `public int Foo { get; private set; } = 5;`（自动属性初始化器）
#     `public int Foo => Bar();`               （表达式体属性）
#   再叠上 `var_map(..., window=20)`（只看同文件前 20 行）⇒ 「赋值离断言 >20 行 / 在字段里」全看不见。
_MODS = (r"(?:(?:public|private|protected|internal|static|readonly|const|volatile|new|"
         r"override|virtual|abstract|sealed|unsafe|extern|partial|async)\s+)*")
_TYPE_TOKENS = r"(?:[A-Za-z_][\w<>\[\],.?]*\s+)*"
_LHS = r"^\s*" + _MODS + _TYPE_TOKENS + r"(?P<n>[A-Za-z_]\w*)\s*(?:\{[^{}]*\})?\s*"

# 赋值 / 字段初始化器。`(?<![=!<>+\-*/%&|^])=(?!=)` ⇒ 不把 `==` `!=` `<=` `+=` `=>` 当赋值。
ASSIGN_RE = re.compile(_LHS + r"(?<![=!<>+\-*/%&|^])=(?!=)\s*(?P<rhs>[^;]+);\s*(?://.*)?$")
# 表达式体属性 / 表达式体方法：`public int Foo => Bar();`（当作「Foo 的值 = Bar()」看）
PROP_EXPR_RE = re.compile(_LHS + r"=>\s*(?P<rhs>[^;]+);\s*(?://.*)?$")


# ---------------------------------------------------------------- 读写（二进制，自己切行）
def read_lines(path):
    """二进制读、自己切行：只按 `\\r\\n` 与 `\\n` 切（裸 CR 留在行内 ⇒ 行号与编辑器一致）。"""
    with io.open(path, "rb") as f:
        b = f.read()
    if b.startswith(b"\xef\xbb\xbf"):
        b = b[3:]
    out = []
    for chunk in b.split(b"\r\n"):
        out.extend(chunk.split(b"\n"))
    return [x.decode("utf-8", "replace") for x in out]


def norm(s):
    return re.sub(r"\s+", "", s or "")


# ---------------------------------------------------------------- 括号配平取实参
def split_args(s, start):
    """`s[start]` 是 '(' 的下标；返回 (实参列表, 右括号下标)。"""
    depth = 0
    args, cur = [], []
    i = start
    in_str = None
    while i < len(s):
        c = s[i]
        if in_str:
            cur.append(c)
            if c == "\\":
                if i + 1 < len(s):
                    cur.append(s[i + 1])
                    i += 1
            elif c == in_str:
                in_str = None
        elif c in "\"'":
            in_str = c
            cur.append(c)
        elif c in "([{":
            depth += 1
            if depth == 1 and c == "(":
                pass
            else:
                cur.append(c)
        elif c in ")]}":
            depth -= 1
            if depth == 0:
                a = "".join(cur).strip()
                if a:
                    args.append(a)
                return args, i
            cur.append(c)
        elif c == "," and depth == 1:
            args.append("".join(cur).strip())
            cur = []
        else:
            cur.append(c)
        i += 1
    return args, -1


def top_split(expr, ops):
    """在深度 0 处找第一个 `op`，返回 (lhs, op, rhs) 或 None。"""
    depth = 0
    in_str = None
    i = 0
    while i < len(expr):
        c = expr[i]
        if in_str:
            if c == "\\":
                i += 2
                continue
            if c == in_str:
                in_str = None
        elif c in "\"'":
            in_str = c
        elif c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
        elif depth == 0:
            for op in ops:
                if expr.startswith(op, i):
                    # 跳过 `==` 里的 `=`；`!=` / `==` 优先
                    return expr[:i], op, expr[i + len(op):]
        i += 1
    return None


# ---------------------------------------------------------------- 工程类型表
def collect_project_types(assets):
    names = set()
    rx = re.compile(r"\b(?:class|struct|enum|interface)\s+([A-Za-z_]\w*)")
    for root, _d, fs in os.walk(assets):
        for f in fs:
            if not f.endswith(".cs"):
                continue
            try:
                with io.open(os.path.join(root, f), "rb") as fh:
                    t = fh.read().decode("utf-8", "replace")
            except Exception:
                continue
            for m in rx.finditer(t):
                names.add(m.group(1))
    return names


# ---------------------------------------------------------------- 主扫描
def scan_file(path, project_types):
    lines = read_lines(path)
    hits = []
    for i, line in enumerate(lines):
        for m in CALL_RE.finditer(line):
            if DECL_RE.search(line):
                continue
            name = m.group("name")
            # 调用跨行的也要处理：把后 6 行拼进来
            buf = line
            j = i
            while buf.count("(") > buf.count(")") and j < i + 6 and j + 1 < len(lines):
                j += 1
                buf += "\n" + lines[j]
            open_i = buf.find("(", m.start())
            if open_i < 0:
                continue
            args, _close = split_args(buf, open_i)
            if not args:
                continue
            hits.append((i + 1, name, args, line.strip()))
    return hits, lines


# 已知的「**外部数据**读取口」——它们读的是本地化表 / 解包资产 / 存档，**不是被测实现**。
# ⚠️ 这是**提示，不是判决**：`Loc.T` 本身也可能是被测对象（翻译回退逻辑）⇒ 仍要人看一眼。
EXTERNAL_READERS = ("Loc.T", "Loc.LanguageName", "Loc.OfficialCurrent", "Loc.HasEntry",
                    "CardArt.", "CardText.", "ArmyIcons.", "CardIcons.", "DeckLibrary.",
                    "CampaignData.", "CardImage.", "UiSprite.", "Art.")


def is_external_reader(calltext):
    c = calltext or ""
    return any(c == p or c.startswith(p) for p in EXTERNAL_READERS)


def is_harness(calltext):
    """`Type.Method` 里的 Type 是不是**断言/夹具层自己**（不是被测实现）。
    夹具层之间的比对可以是真的自证，但**不是**「期望值 = 被测实现」那一族 ⇒ 单独一档，别混进来。"""
    t = (calltext or "").split(".")[0]
    return (t == "MenuCheck" or t.endswith(("Check", "Assert", "Fixture", "Harness"))
            or t.startswith("Assert"))


def in_project_call(expr, project_types):
    for m in PROJ_CALL_RE.finditer(expr or ""):
        t = m.group("t")
        if t in EXTERNAL_TYPES or t in ("if", "while", "for", "foreach", "switch", "return"):
            continue
        if t in project_types:
            return m.group(0).rstrip("(")
    return None


def build_var_map(lines):
    """**整文件**的赋值表：`名字 -> [(行号, 右值), …]`（升序）。
    收普通赋值、字段初始化器、自动属性初始化器、表达式体属性（见 `ASSIGN_RE` / `PROP_EXPR_RE`）。
    ⚠️ 旧版只看「断言前 20 行」⇒ 赋值在字段里 / 离得远就全看不见（账 `B-122` 盲点①）。"""
    out = {}
    for k, line in enumerate(lines):
        for rx in (ASSIGN_RE, PROP_EXPR_RE):
            m = rx.match(line)
            if m:
                out.setdefault(m.group("n"), []).append((k + 1, m.group("rhs").strip()))
                break
    return out


class VarProjection(object):
    """把整文件赋值表**投影到「第 `upto` 行之前」**：`名字 -> (右值, 行号)`（取最近一次）。
    懒查：`name` 首次被问到时才在它自己的赋值序列里二分/线性找，不预先展开整张字典（大文件很快）。"""

    __slots__ = ("allmap", "upto", "window", "_cache")

    def __init__(self, allmap, upto, window=0):
        self.allmap = allmap
        self.upto = upto
        self.window = window        # 0 = **整文件**（默认）；>0 = 只认「断言前 N 行内」的赋值（旧行为，`--var-window`）
        self._cache = {}

    def _lookup(self, name):
        lst = self.allmap.get(name)
        if not lst:
            return None
        lo = (self.upto - self.window) if self.window > 0 else 0
        best = None
        for ln, rhs in lst:            # 已按行号升序
            if ln >= self.upto:
                break
            if ln >= lo:
                best = (rhs, ln)
        return best

    def get(self, name, default=None):
        if name not in self._cache:
            self._cache[name] = self._lookup(name)
        v = self._cache[name]
        return default if v is None else v

    def __contains__(self, name):
        return self.get(name) is not None

    def __getitem__(self, name):
        v = self.get(name)
        if v is None:
            raise KeyError(name)
        return v


def expand(expr, vmap):
    e = (expr or "").strip()
    m = re.fullmatch(r"([A-Za-z_]\w*)", e)
    if m and m.group(1) in vmap:
        return vmap[m.group(1)][0], vmap[m.group(1)][1]
    return e, None


# ---------------------------------------------------------------- 断言签名表（账 `B-122` 盲点②）
# 旧版 `classify` 硬编码 `cond_idx = 1 if name == "MenuCheck.True" else 0` ⇒
# 池内 **8 处**「首参是消息」的签名被读反（7 × `Check(string what, bool ok)` +
# `Check(string what, bool ok, float measured)`）⇒ 把消息当条件、真条件 `got == want` 整条不看。
# 现在按**声明**解析出「哪个位置是 cond / msg / 值」。
MSG_PARAM_NAMES = ("msg", "message", "what", "label", "name", "why", "desc", "tag", "note", "title")


def parse_sig_params(args):
    """`['bool ok', 'string what']` → `[('bool','ok'), ('string','what')]`；切歪了返回 None。"""
    out = []
    for a in args:
        p = a.split("=", 1)[0].strip()                      # 去掉默认值
        p = re.sub(r"^(?:this|ref|out|in|params)\s+", "", p)
        toks = p.split()
        if not toks:
            continue
        nm, ty = toks[-1], " ".join(toks[:-1])
        if ty.count("<") != ty.count(">"):
            # `System.Action<bool, string>` 里的逗号会被 `split_args` 当分隔符切开 ⇒ 这条签名不可用
            return None
        out.append((ty, nm))
    return out


def build_sig_map(lines):
    """`名字 -> [ [ (类型, 形参名), … ], … ]`（同名重载全部收）。"""
    sigs = {}
    i, n = 0, len(lines)
    while i < n:
        line = lines[i]
        m = DECL_RE.search(line)
        if not m:
            i += 1
            continue
        buf, j = line, i
        while buf.count("(") > buf.count(")") and j < i + 8 and j + 1 < n:
            j += 1
            buf += "\n" + lines[j]
        open_i = buf.find("(", m.start())
        if open_i < 0:
            i = j + 1
            continue
        args, _ = split_args(buf, open_i)
        # 名字提取要跳过泛型参数：`static void Check<T>(` ⇒ `Check`
        mm = re.search(r"(?P<nm>MenuCheck\.True|CheckTrue|CheckEq|Check|Ok)\s*(?:<[^<>]*>\s*)?$",
                       buf[:open_i])
        if mm:
            ps = parse_sig_params(args)
            if ps is not None:
                sigs.setdefault(mm.group("nm"), []).append(ps)
        i = j + 1
    return sigs


def resolve_roles(args, sigs, name):
    """返回 `(cond_idx, msg_idx集合, 值位置列表)`。
    * 签名已知 ⇒ 按**声明**判（`bool` 那个位置 = 条件；`string` 且形参名像 msg = 消息；其余 = 值）。
    * 签名未知 ⇒ 退回旧启发式（`MenuCheck.True` 首参是 sink ⇒ 条件在 1；其余在 0），
      **保住召回**：老版本能抓到的，新版本一条不少。"""
    cands = [s for s in sigs.get(name, []) if len(s) == len(args)]
    if not cands:
        # 签名未知 ⇒ 退回旧启发式：`MenuCheck.True` 首参是 sink（条件在 1），其余条件在 0；
        #   消息 = 字符串字面量；剩下的位置一律当「值」（保住旧版 `Check(got, want, msg)` 的召回）。
        base = 1 if name == "MenuCheck.True" else 0
        msg = {i for i, a in enumerate(args) if is_msg_like(a)}
        val = [i for i in range(len(args)) if i not in msg and i != base]
        return base, msg, val
    # 同名同参数量重载时，优先带 `bool` 的那份（`Check` 家族里只有断言版带 bool）
    cands.sort(key=lambda s: 0 if any(t == "bool" for t, _ in s) else 1)
    sig = cands[0]
    last = len(sig) - 1
    has_nonstring = any(t not in ("string", "String", "System.String") for t, _ in sig)
    msg_idx, cond_idx = set(), None
    for i, (ty, nm) in enumerate(sig):
        is_str = ty in ("string", "String", "System.String")
        if is_str and (nm.lower() in MSG_PARAM_NAMES or (i == last and has_nonstring)):
            msg_idx.add(i)
        elif ty == "bool" and cond_idx is None:
            cond_idx = i
    val_idx = [i for i in range(len(sig)) if i not in msg_idx and i != cond_idx]
    return cond_idx, msg_idx, val_idx


def top_split_all(expr, seps):
    """按**深度 0** 处的分隔符切分（分隔符按长度优先）。返回子表达式列表。"""
    out, depth, in_str, cur, i = [], 0, None, [], 0
    seps = sorted(seps, key=len, reverse=True)
    while i < len(expr):
        c = expr[i]
        if in_str:
            cur.append(c)
            if c == "\\" and i + 1 < len(expr):
                cur.append(expr[i + 1])
                i += 2
                continue
            if c == in_str:
                in_str = None
        elif c in "\"'":
            in_str = c
            cur.append(c)
        elif c in "([{":
            depth += 1
            cur.append(c)
        elif c in ")]}":
            depth -= 1
            cur.append(c)
        elif depth == 0:
            hit = next((s for s in seps if expr.startswith(s, i)), None)
            if hit:
                out.append("".join(cur).strip())
                cur = []
                i += len(hit)
                continue
            cur.append(c)
        else:
            cur.append(c)
        i += 1
    out.append("".join(cur).strip())
    return [x for x in out if x]


LITERAL_RE = re.compile(r'^(?:[-+]?[\d.]+f?|"[^"]*"|\'[^\']*\'|true|false|null|default)$')


def is_msg_like(a):
    s = (a or "").strip()
    return s.startswith('"') or s.startswith('$"') or s.startswith('@"') or s.startswith('$@"')


def classify(args, allmap, lineno, project_types, name, sigs, var_window=0):
    """返回 `(档位, 说明, 赋值→断言距离, 是否外部数据读取口)`；无命中返回 `(None, None, 0, 0)`。
    「距离」只对**变量型**期望值有意义（0 = 期望值就是调用本身）。
    🔴 2026-11-01（账 `B-122`）：`cond_idx` **不再硬编码** —— 由 `resolve_roles` 按声明解析出
      「哪个位置是条件 / 消息 / 值」。硬编码那版把 `Check(string what, bool ok)` 的**消息当条件**。"""
    cond_idx, msg_idx, val_idx = resolve_roles(args, sigs, name)
    if cond_idx is None and len(val_idx) < 2:
        return None, None, 0, 0
    cond = args[cond_idx] if (cond_idx is not None and cond_idx < len(args)) else None

    vmap = VarProjection(allmap, lineno, var_window)

    # ——— 形状 A：「同一个表达式出现两次」——同一次断言里两个**值**实参相同
    #   2026-11-01 扩（账 `B-122`）：① 值位置按**签名**取（消息位置排除，不再靠 `is_msg_like` 猜）；
    #                              ② 补一档「两参值对值」——`Check(got, want, msg)` / `Check(got, want)`。
    #   ⚠️ **两档分开**：两个实参**原文**相同 ⇒ 高置信（`Check(X, X)` 是恒真式）；
    #      只有**代入赋值后**才相同 ⇒ **待人工判** —— 那多半是**前后不变量**（before/after：
    #      先 `w0 = F()` 再动作、再 `Check(F(), w0)`），不是自证，但值得人看一眼（旧版根本看不见）。
    soft = None
    vals = [a for i, a in enumerate(args) if i not in msg_idx and not is_msg_like(a)]
    if len(vals) >= 2:
        for a in range(len(vals)):
            for b in range(a + 1, len(vals)):
                na, nb = norm(vals[a]), norm(vals[b])
                if na and na == nb and ("(" in na or "." in na) and len(na) > 6:
                    pc = in_project_call(vals[a], project_types)
                    return "高置信", "同一次断言里两个值实参文本相同：`%s`%s" % (
                        vals[a], ("（本工程调用 `%s`%s）" % (
                            pc, "·夹具层" if is_harness(pc) else "")) if pc else ""), 0, 0
                ea, la = expand(vals[a], vmap)
                eb, _lb = expand(vals[b], vmap)
                ne = norm(ea)
                if soft is None and ne and ne == norm(eb) and ne != na and ne != nb \
                        and ("(" in ne or "." in ne) and len(ne) > 6:
                    pc = in_project_call(ea, project_types)
                    soft = ("两个值实参**代入赋值后**相同：`%s == %s` ⇒ 都是 `%s`%s"
                            "（要问：这是**前后不变量**，还是期望值直接来自被测实现？赋值得在 :%s）"
                            % (vals[a], vals[b], ea,
                               ("（本工程调用 `%s`）" % pc) if pc else "",
                               la or "?"),
                            (lineno - la) if la else 0,
                            (1 if pc and is_external_reader(pc) else 0))

    clauses = top_split_all(cond, ["&&", "||"]) if cond else []

    # ——— 形状 B：`lhs == rhs`，代入赋值后两边相同（赋值表 = **整文件**，不再是前 20 行）
    for cl in clauses:
        sp = top_split(cl, ["==", "!="])
        if not sp:
            continue
        lhs, op, rhs = sp
        el, ll = expand(lhs, vmap)
        er, _lr = expand(rhs, vmap)
        if norm(el) and norm(el) == norm(er) and "(" in norm(el):
            pc = in_project_call(el, project_types)
            if pc:
                return "高置信", "`%s %s %s` 代入后两边都是 `%s`（本工程调用 `%s`；赋值在 :%s）" % (
                    lhs.strip(), op, rhs.strip(), el, pc, ll or "前若干行"), 0, 0

    # ——— 形状 C：**期望值**（`actual == expected` 的右侧，或 `Check(got, want, msg)` 的 `want`）
    # ⚠️ 2026-11-01（`B112_T2C`）：**三元表达式不拆**。`Check(kept == null ? -1 : kept.Count, 1, …)` 这种
    #    写法在深度 0 上第一个 `==` 落在 `?` 前面，拆出来的「期望值」是 `null ? -1 : kept.Count` ⇒ 假阳性。
    #    `B112_T2C` 实测这类在本工程里有一批（`C8_Assert.cs:150` / `RuleEngineTest_N1.cs:515` 等）。
    exprs = []
    for cl in clauses:
        if "?" in cl:
            continue
        sp = top_split(cl, ["=="])
        if sp:
            exprs.append(sp[2].strip())
    if val_idx:                                      # `Check(got, want, msg)` / `Check(got, want)` 的 want
        w = args[val_idx[-1]].strip()
        if not LITERAL_RE.match(w) and "?" not in w:
            exprs.append(w)
    for expect in exprs:
        if not expect or LITERAL_RE.match(expect):
            continue
        pc = in_project_call(expect, project_types)
        if pc and not is_harness(pc):
            return "待人工判", ("期望值**直接**来自本工程调用 `%s`"
                            "（要问：这个函数读的是外部真值，还是被测实现自己？）" % pc), 0, \
                (1 if is_external_reader(pc) else 0)
        for tok in set(re.findall(r"[A-Za-z_]\w*", expect)):
            if tok in vmap:
                rhs2, at = vmap[tok]
                pc = in_project_call(rhs2, project_types)
                if pc and not is_harness(pc):
                    return "待人工判", "期望值变量 `%s` 在 :%d 由本工程调用 `%s` 赋值" % (
                        tok, at, pc), lineno - at, (1 if is_external_reader(pc) else 0)
    if soft is not None:
        return "待人工判", soft[0], soft[1], soft[2]
    return None, None, 0, 0


def iter_targets(assets):
    for root, _d, fs in os.walk(assets):
        if os.path.basename(root) != "Editor":
            continue
        for f in fs:
            if f.endswith("_Assert.cs") or f.endswith(".cs"):
                yield os.path.join(root, f)


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--scan", action="store_true")
    ap.add_argument("--top", type=int, default=20)
    ap.add_argument("--only", default="all", choices=["all", "高置信", "待人工判"])
    ap.add_argument("--out", default=None)
    ap.add_argument("--var-window", type=int, default=0,
                    help="赋值表只看「断言前 N 行」；0（默认）= **整文件**（账 B-122 盲点①的修法）")
    ap.add_argument("--assets", default=DEFAULT_ASSETS)
    ap.add_argument("-h", "--help", action="store_true")
    args = ap.parse_args()
    if args.help or not args.scan:
        print(__doc__)
        return 0

    assets = args.assets
    if not os.path.isdir(assets):
        print("⛔ 扫描根不存在：%s" % assets)
        return 1

    print("── 收集工程类型表 …")
    pt = collect_project_types(assets)
    print("   本工程类型 %d 个" % len(pt))

    files = list(iter_targets(assets))
    print("── 扫描 %d 个 Editor/*.cs …" % len(files))
    hi, mid = [], []
    n_calls = 0
    for p in files:
        try:
            hits, lines = scan_file(p, pt)
        except Exception as e:                                   # noqa: BLE001
            print("   ⚠️ 读不动 %s（%s）" % (p, e))
            continue
        n_calls += len(hits)
        allmap = build_var_map(lines)             # 🆕 `B-122` 盲点①：整文件赋值表（含字段/属性初始化器）
        sigs = build_sig_map(lines)               # 🆕 `B-122` 盲点②：按声明解析 cond/msg/值位置
        for lineno, name, cargs, raw in hits:
            try:
                kind, why, dist, ext = classify(cargs, allmap, lineno, pt, name, sigs,
                                                args.var_window)
            except Exception:                                    # noqa: BLE001
                kind, why, dist, ext = None, None, 0, 0
            if not kind:
                continue
            rec = (os.path.relpath(p, assets).replace(os.sep, "/"), lineno, name, raw[:180],
                   why, dist, ext)
            (hi if kind == "高置信" else mid).append(rec)

    print("── 断言调用 %d 次 · **高置信 %d 条** · **待人工判 %d 条**" % (n_calls, len(hi), len(mid)))

    # 「待人工判」排序：① **实现本体**优先（外部数据口排后）② 同组内「赋值→断言」距离升序。
    #   距离 1~3 = `x = F(); Check(y == x)` 那种**经典自证形**（`B111_F1B` 就是这一形）；
    #   距离 10+ 的多半是**前后不变量**（before/after），是本工具已知的假阳性大头。
    mid.sort(key=lambda r: (r[6], r[5]))
    near = sum(1 for r in mid if r[5] <= 3 and not r[6])
    nxt = sum(1 for r in mid if not r[6])

    for label, recs in (("高置信", hi), ("待人工判", mid)):
        if args.only not in ("all", label):
            continue
        print("\n===== %s（%d）=====" % (label, len(recs)))
        if label == "待人工判":
            print("  （排序 = **实现本体优先** · 再按「赋值→断言」距离升序；"
                  "其中**实现本体 %d 条**，距离 ≤3 的实现本体 **%d 条** = 经典自证形，先看它们）"
                  % (nxt, near))
        for rel, lineno, name, raw, why, dist, ext in recs[: args.top]:
            tag = "〔疑似外部数据口·多半假阳性〕" if ext else ""
            print("  %s:%d  [%s]%s" % (rel, lineno, name, tag))
            print("     写法: %s" % raw)
            print("     判据: %s" % why)
        if len(recs) > args.top:
            print("  …（其余 %d 条见 %s）" % (len(recs) - args.top, args.out or "（加 --out 落盘）"))

    if args.out:
        d = os.path.dirname(args.out)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        with io.open(args.out, "w", encoding="utf-8", newline="\n") as f:
            f.write("档位\t文件\t行\t调用\t写法\t判据\t赋值距离\t外部数据口\n")
            for label, recs in (("高置信", hi), ("待人工判", mid)):
                for rel, lineno, name, raw, why, dist, ext in recs:
                    f.write("\t".join([label, rel, str(lineno), name,
                                       raw.replace("\t", " "), why.replace("\t", " "),
                                       str(dist), str(ext)]) + "\n")
        print("\n已写 %s（%d 行）" % (args.out, len(hi) + len(mid) + 1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
