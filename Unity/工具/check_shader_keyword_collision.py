#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""check_shader_keyword_collision.py — 「shader 关键字 vs 同名变量」重定义静态自检（**只读**）。

## 为什么要有这个工具
Unity 的 `[Toggle(NAME)]` / `[ToggleOff(NAME)]` / `[KeywordEnum(...)]` 与
`#pragma multi_compile[_local]` / `#pragma shader_feature[_local]` 都是**声明关键字**：
**关键字一开，Unity 就给整份 shader 加一条 `#define NAME 1`**。
若同一份 shader 里又声明了同名变量（`float NAME;` / `half NAME;` / `float4 NAME;`，含
`CBUFFER_START/END` 内与 struct 成员），HLSL 立刻报 **`redefinition of 'NAME'`**，
**整份 shader 编不过**（= 品红 error shader / `Shader.Find` 拿不到 / 材质全黑）。

🔴 这个东西 **`工具/typecheck.sh` 完全看不出来**（它只编 C#）。本项目 2026-10-24 就这么踩过：
`WFEclipseTau.shader` 的 `redefinition of '_APPLYAMBIENTCOLOR'`（`_tmp_view/20261024_playerbuild.log:5661`）
与 `WFUIBorderHighlight.shader` 的 `redefinition of '_AUTOROTATION' at …(202)` 都是同一形状。

## 判据（什么算「真冲突」）
对**同一个 `.shader` 文件**求两个集合的交集：

1. **关键字集** ——
   - `[Toggle(NAME)]` / `[ToggleOff(NAME)]`（ShaderLab 属性特性；两者都生成同一个关键字名）
   - `[KeywordEnum(A,B,C)] _PROP`（Unity 按 **`_PROP的UPPER_A`** 展开，例 `[KeywordEnum(R,G,B,A)] _NOISE_CHANNEL`
     ⇒ `_NOISE_CHANNEL_R` …）
   - `#pragma multi_compile[_local][_fragment|_vertex] …`
   - `#pragma shader_feature[_local][_fragment|_vertex] …`
     其中 `_` / `__` 是**空档占位**，不是关键字名，跳过；
     `multi_compile_instancing` / `multi_compile_fog` / `multi_compile_shadowcaster` 这类
     **不逐条点名关键字**的 pragma 不算（它们生成的关键字不由本文件掌握）。
2. **变量声明集** —— HLSL/CG 里的**真变量声明**：
   `float|half|fixed|real|double|int|uint|bool|dword`（含 `float2/3/4`、`float4x4` 等向量矩阵形式）
   与 `sampler2D` 一族的 sampler、`Texture2D`/`texture2D` 一族的纹理。
   覆盖 `CBUFFER_START/END` 内、struct 成员、多声明符（`float a, b, c;`）。

**交集非空 = 真冲突**（rc=1）。

## 两类「看着像、其实不用改」的，单独报 WARN（rc 不受影响）
- **`Properties` 里的属性名与关键字同名**（`[Toggle(_X)] _X ("…", Float) = 0`）——
  这是**合法**的：ShaderLab 属性名**不会**在 HLSL 里被 `#define`，属性与关键字是两个命名空间。
  真正的坑是「属性名同名 **＋** 又写了一条 `float _X;`」。本项目 3 个文件就是这么修的
  （`WFEclipseTau.shader:96-101` · `WFParticlePremultiplyGreyscale.shader:109-111` ·
  `WFUIBorderHighlight.shader:214-219`：**只删 HLSL 声明、`Properties` 里那条 `[Toggle]` 原样保留**）。
  ⇒ 本工具把它报成 `WARN`，**不判失败**。
- **`[Toggle(...)]` 那一行本身** —— 它是声明关键字、不是声明变量；
  因为变量集只认「HLSL 类型 + 名字」，而 ShaderLab 里的类型写的是 `Float`（大写，不在列表里），
  所以天然不会自撞（本工具**不**另做特判）。

## 用法
    python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py [<路径> ...] [选项]

    <路径> 可以是 `.shader` 文件或目录（目录**递归**找 `*.shader`）。
    **一个都不给**时默认扫 `d:/4/Unity/MyGame/Assets`（本工程 65 份 shader）。

选项：
    --root DIR     追加一个扫描根（可重复；等价于把 DIR 当 <路径> 传）
    --json         输出机器可读 JSON（stdout 只有 JSON）
    --quiet        只打印冲突/WARN 明细（结尾仍打合计）
    --no-warn      不打印 WARN（只看真冲突）
    --self-test    自测：造临时正/负例 shader，用子进程验证 rc 与命中条数
    -h/--help      本帮助

退出码：
    0 = 没有真冲突（WARN 不影响）
    1 = 至少一处真冲突
    2 = 用法/读文件错误（或 `--self-test` 失败）

## 已知局限（刻意收窄，宁可漏报也不误报）
- **不是 HLSL 解析器**：正则够用即可，做不到语义级。已覆盖的写法见上；下列**不覆盖**：
  `UNITY_DEFINE_INSTANCED_PROP(TYPE, NAME)` / `TEXTURE2D(NAME)` 这类**宏**声明（名字在括号里）。
  真撞上了它会漏报 ⇒ 这份工具是**兜底闸**，不是「编过了」的证明。
- **大小写敏感**：HLSL 类型名只认小写（`float`）；ShaderLab 的 `Float`（属性类型）**不算声明**
  —— 这是「`[Toggle(X)] X ("…", Float)` 不误报」的关键。
- **函数形参不算**（`float3 foo (float3 c)` 里的 `c` 不采集）：形参与关键字同名的概率极低，
  而「把返回类型 + 函数名误当声明」的假阳性代价更高。
- 行为**只读**：不写盘、不改文件。要修哪一处由人按报告决定。
- **行号是现读的**：别的写手改动文件后行号会变，本工具每次都重新读文件、不缓存。

## 推荐调用（改过任何 `.shader` 之后跑一次；也是 `B-298` 的复跑入口）
    python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py
"""

import io
import json
import os
import re
import sys
import tempfile

PROG = "check_shader_keyword_collision.py"
DEFAULT_ROOT = os.path.abspath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MyGame", "Assets"))

USAGE = ("用法：python -I -X utf8 {prog} [<路径> ...] [--root DIR] [--json] "
         "[--quiet] [--no-warn] [--self-test]").format(prog=PROG)

# ---------------------------------------------------------------------------
# 关键字提取
# ---------------------------------------------------------------------------

# [Toggle(NAME)] / [ToggleOff(NAME)] —— 两个特性都生成同一个关键字名
RE_TOGGLE = re.compile(r"\[\s*Toggle(?:Off)?\s*\(\s*([A-Za-z_]\w*)\s*\)\s*\]")

# [KeywordEnum(A, B, C)] _PROP ("…", Float) = 0
RE_KEYWORD_ENUM = re.compile(r"\[\s*KeywordEnum\s*\(([^)]*)\)\s*\]")
RE_PROP_NAME_AFTER = re.compile(r"\b([A-Za-z_]\w*)\s*\(")

# #pragma multi_compile / multi_compile_local / shader_feature / shader_feature_local
# 允许可选的 _fragment / _vertex 后缀（Unity 支持的逐阶段写法）
RE_PRAGMA = re.compile(
    r"^[ \t]*#[ \t]*pragma[ \t]+"
    r"(multi_compile|multi_compile_local|shader_feature|shader_feature_local)"
    r"(?:_(?:fragment|vertex))?[ \t]+(.*)$")

# ---------------------------------------------------------------------------
# 变量声明提取（HLSL/CG）
# ---------------------------------------------------------------------------

# HLSL 标量/向量/矩阵类型 + sampler/纹理类型。**只认小写**（ShaderLab 的 `Float` 是大写）。
_SCALAR = r"(?:float|half|fixed|real|double|int|uint|bool|dword)"
_SIZES = r"(?:[1-4](?:x[1-4])?)?"
_SAMPLER = r"(?:sampler(?:_\w+)?|sampler2D\w*|sampler3D\w*|samplerCUBE\w*|sampler2DArray\w*|" \
           r"Texture2D\w*|Texture3D\w*|TextureCube\w*|Texture2DArray\w*|texture2D\w*|texture3D\w*|textureCUBE\w*)"
RE_TYPE = re.compile(r"\b(?:" + _SCALAR + _SIZES + r"|" + _SAMPLER + r")\b")
RE_IDENT = re.compile(r"[A-Za-z_]\w*")
RE_ARRAY = re.compile(r"\s*\[[^\]]*\]")

# 声明后面允许出现的字符（决定「这是个声明」而不是函数名/表达式）。
# ⚠️ **刻意不含 `)`**：函数形参（`float3 f (float3 c)` 的 `c`）与函数名（`float4 frag (`）都以此排除。
_AFTER_OK = set("=;,:[")


def strip_comments(text):
    """把 `//` 与 `/* */` 注释替换成空格（**保留换行与所有偏移**），字符串内容同样置空。

    返回 `(clean, in_block_flags)`：clean 与 text **等长**，行号/列号照旧可用。
    """
    out = list(text)
    n = len(text)
    i = 0
    NORMAL, LINE, BLOCK, STR, CHARS = 0, 1, 2, 3, 4
    state = NORMAL
    while i < n:
        c = text[i]
        two = text[i:i + 2]
        if state == NORMAL:
            if two == "//":
                state = LINE
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if two == "/*":
                state = BLOCK
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c == '"':
                state = STR
                i += 1
                continue
            if c == "'":
                state = CHARS
                i += 1
                continue
            i += 1
            continue
        if state == LINE:
            if c == "\n":
                state = NORMAL
                i += 1
            else:
                out[i] = " "
                i += 1
            continue
        if state == BLOCK:
            if two == "*/":
                out[i] = out[i + 1] = " "
                state = NORMAL
                i += 2
            else:
                if c != "\n":
                    out[i] = " "
                i += 1
            continue
        if state == STR:
            if c == "\\":
                # 转义：吞掉两个字符（跨行时留着换行）
                out[i] = " "
                if i + 1 < n:
                    if text[i + 1] != "\n":
                        out[i + 1] = " "
                    i += 2
                else:
                    i += 1
                continue
            if c == '"':
                state = NORMAL
                i += 1
                continue
            if c != "\n":
                out[i] = " "
            i += 1
            continue
        # CHARS
        if c == "\\":
            out[i] = " "
            if i + 1 < n and text[i + 1] != "\n":
                out[i + 1] = " "
            i += 2
            continue
        if c == "'":
            state = NORMAL
            i += 1
            continue
        if c != "\n":
            out[i] = " "
        i += 1
    return "".join(out)


def _line_of(clean, offset):
    """偏移 → 1 起的行号。"""
    return clean.count("\n", 0, offset) + 1


def _split_pragma_tokens(rest):
    """把 pragma 后面的 token 串切成关键字名列表（跳 `_` / `__` 空档）。"""
    out = []
    for tok in rest.split():
        if tok in ("_", "__"):
            continue
        if not re.match(r"^[A-Za-z_]\w*$", tok):
            # 非标识符（比如误入的 `//` 残片）——跳过并让调用方不采信
            continue
        out.append(tok)
    return out


def extract_keywords(clean):
    """返回 [(name, line, source_text)]；同名多次声明各自留一条（便于报落点）。"""
    kws = []

    for m in RE_TOGGLE.finditer(clean):
        line = _line_of(clean, m.start())
        raw = clean.split("\n")[line - 1].strip()
        kws.append({"name": m.group(1), "line": line, "kind": "Toggle", "raw": raw,
                    "offset": m.start()})

    for m in RE_KEYWORD_ENUM.finditer(clean):
        line = _line_of(clean, m.start())
        # 属性名在特性之后（可能跨行）；只在后面 240 字符里找
        tail = clean[m.end(): m.end() + 240]
        pm = RE_PROP_NAME_AFTER.search(tail)
        if not pm:
            continue
        prop = pm.group(1)
        prefix = prop.upper()
        ents = [e.strip() for e in m.group(1).split(",") if e.strip()]
        raw = clean.split("\n")[line - 1].strip()
        for ent in ents:
            if not re.match(r"^[A-Za-z_]\w*$", ent):
                continue
            kws.append({"name": prefix + "_" + ent.upper(), "line": line,
                        "kind": "KeywordEnum(%s)" % prop, "raw": raw,
                        "offset": m.start()})

    for idx, ln in enumerate(clean.split("\n")):
        m = RE_PRAGMA.match(ln)
        if not m:
            continue
        kind = m.group(1)
        for name in _split_pragma_tokens(m.group(2)):
            kws.append({"name": name, "line": idx + 1, "kind": "#pragma " + kind,
                        "raw": ln.strip(), "offset": 0})
    return kws


def extract_variables(clean):
    """返回 [(name, line, decl_text)]：HLSL 里**真变量声明**的名字。

    做法：逐行找「类型关键字」，从它后面按 `ident [数组] , ident …` 逐个收，
    并检查最后一个字符是 `= ; , : [` 之一（排除 `type funcName (` 这种函数声明）。
    以 `#` 开头的行（预处理指令）整行跳过。
    """
    vars_ = []
    for idx, ln in enumerate(clean.split("\n")):
        stripped = ln.lstrip()
        if stripped.startswith("#"):
            continue
        for m in RE_TYPE.finditer(ln):
            pos = m.end()
            # 类型与名字之间至少一个空格
            if pos >= len(ln) or not ln[pos].isspace():
                continue
            while True:
                while pos < len(ln) and ln[pos].isspace():
                    pos += 1
                im = RE_IDENT.match(ln, pos)
                if not im:
                    break
                name = im.group(0)
                pos = im.end()
                am = RE_ARRAY.match(ln, pos)
                if am:
                    pos = am.end()
                # 名字后面允许什么？
                save = pos
                while pos < len(ln) and ln[pos].isspace():
                    pos += 1
                nxt = ln[pos] if pos < len(ln) else ""
                if nxt in _AFTER_OK:
                    vars_.append({"name": name, "line": idx + 1,
                                  "decl": " ".join(ln.strip().split()), "offset": 0})
                else:
                    pos = save
                    break
                if nxt == ",":
                    pos += 1
                    continue
                break
    return vars_


def extract_properties(clean):
    """返回 Properties 块里的属性名集合（用于 WARN 一类：属性名与关键字同名）。"""
    m = re.search(r"\bProperties\s*\{", clean)
    if not m:
        return set()
    depth = 1
    i = m.end()
    while i < len(clean) and depth > 0:
        if clean[i] == "{":
            depth += 1
        elif clean[i] == "}":
            depth -= 1
        i += 1
    block = clean[m.end(): i - 1]
    names = set()
    for pm in re.finditer(r"([A-Za-z_]\w*)\s*\(\s*\"", block):
        names.add(pm.group(1))
    return names


def check_file(path):
    with io.open(path, "rb") as f:
        raw = f.read()
    text = raw.decode("utf-8-sig")
    clean = strip_comments(text)

    kws = extract_keywords(clean)
    vars_ = extract_variables(clean)
    props = extract_properties(clean)

    kw_by_name = {}
    for k in kws:
        kw_by_name.setdefault(k["name"], []).append(k)
    var_by_name = {}
    for v in vars_:
        var_by_name.setdefault(v["name"], []).append(v)

    conflicts = []   # 真冲突：关键字 ∩ HLSL 变量声明（每个「变量声明处」一条，列出**所有**关键字声明处）
    warns = []       # WARN：关键字 ∩ Properties 属性名（合法）
    for name, decls in sorted(var_by_name.items()):
        if name not in kw_by_name:
            continue
        kws_here = [{"line": k["line"], "kind": k["kind"], "raw": k["raw"]}
                    for k in kw_by_name[name]]
        for d in decls:
            conflicts.append({
                "name": name,
                "keywords": kws_here,
                "variable": {"line": d["line"], "decl": d["decl"]},
            })
    for name in sorted(kw_by_name):
        if name in props and name not in var_by_name:
            k = kw_by_name[name][0]
            warns.append({"name": name, "keyword": {"line": k["line"], "kind": k["kind"],
                                                    "raw": k["raw"]}})

    return {
        "file": path,
        "keyword_count": len(kws),
        "variable_count": len(vars_),
        "conflicts": conflicts,
        "warns": warns,
        "ok": not conflicts,
    }


def collect_files(paths):
    files = []
    for p in paths:
        if os.path.isdir(p):
            for root, dirs, names in os.walk(p):
                dirs.sort()
                for n in sorted(names):
                    if n.lower().endswith(".shader"):
                        files.append(os.path.join(root, n))
        elif os.path.isfile(p):
            files.append(p)
        else:
            raise IOError("路径不存在：%s" % p)
    # 去重 + 保持稳定顺序
    seen, out = set(), []
    for f in files:
        key = os.path.normcase(os.path.abspath(f))
        if key not in seen:
            seen.add(key)
            out.append(f)
    return out


def _fmt_text(results, quiet, show_warn):
    out = []
    n_conf = n_warn = 0
    for r in results:
        for c in r["conflicts"]:
            n_conf += 1
            kdesc = " · ".join(
                "{kind} @ :{line}".format(kind=k["kind"], line=k["line"])
                for k in c["keywords"])
            out.append("✗ FAIL {f}:{vl}  关键字 `{n}`（{kd}）"
                       "  vs  变量声明 `{decl}`".format(
                           f=r["file"], vl=c["variable"]["line"], n=c["name"],
                           kd=kdesc, decl=c["variable"]["decl"]))
            for k in c["keywords"]:
                out.append("        关键字原文（:{l}）：{raw}".format(l=k["line"], raw=k["raw"]))
        if show_warn:
            for w in r["warns"]:
                n_warn += 1
                out.append("⚠ WARN {f}:{kl}  `{n}` 同时是关键字（{kind}）与 Properties 属性名"
                           " —— **合法**（属性名不进 HLSL 的 #define），"
                           "只有再写同名 HLSL 变量才编不过".format(
                               f=r["file"], kl=w["keyword"]["line"], n=w["name"],
                               kind=w["keyword"]["kind"]))
        if not quiet and not r["conflicts"] and not (show_warn and r["warns"]):
            out.append("PASS {f}：关键字 {k} · 变量 {v} · 冲突 0".format(
                f=r["file"], k=r["keyword_count"], v=r["variable_count"]))
    out.append("合计：文件 {nf} · 关键字 {nk} · 变量声明 {nv} · 真冲突 {nc} · WARN {nw}"
               "  ⇒ {verdict}".format(
                   nf=len(results),
                   nk=sum(r["keyword_count"] for r in results),
                   nv=sum(r["variable_count"] for r in results),
                   nc=n_conf, nw=n_warn,
                   verdict="FAIL" if n_conf else "PASS"))
    return "\n".join(out), (1 if n_conf else 0)


def _run_self_test():
    """自测：造 4 个临时样本，用子进程跑自己，验证 rc 与命中条数。"""
    tmp = tempfile.mkdtemp(prefix="wf_shkwchk_")
    samples = {
        # 负例 1：`[Toggle]` 关键字名 + 同名 float 声明 ⇒ 真冲突（本项目踩过的形状）
        "neg_toggle_float.shader": (
            'Shader "T/Neg1" {\n'
            '    Properties {\n'
            '        [Toggle(_AUTOROTATION)] _AUTOROTATION ("Auto", Float) = 0\n'
            '    }\n'
            '    SubShader { Pass { CGPROGRAM\n'
            '        #pragma shader_feature_local _AUTOROTATION\n'
            '        float _AUTOROTATION;\n'
            '    ENDCG } }\n'
            '}\n'),
        # 负例 2：pragma 关键字 + CBUFFER 内的 half 声明
        "neg_pragma_cbuffer.shader": (
            'Shader "T/Neg2" {\n'
            '    Properties { }\n'
            '    SubShader { Pass { CGPROGRAM\n'
            '        #pragma multi_compile_local _ _APPLYAMBIENTCOLOR\n'
            '        CBUFFER_START(UnityPerMaterial)\n'
            '            half _APPLYAMBIENTCOLOR;\n'
            '        CBUFFER_END\n'
            '    ENDCG } }\n'
            '}\n'),
        # 负例 3：KeywordEnum 展开名 + 多声明符里的第二个变量
        "neg_keywordenum.shader": (
            'Shader "T/Neg3" {\n'
            '    Properties {\n'
            '        [KeywordEnum(R,G,B,A)] _NOISE_CHANNEL ("Chan", Float) = 0\n'
            '    }\n'
            '    SubShader { Pass { CGPROGRAM\n'
            '        float4 _Color, _NOISE_CHANNEL_B;\n'
            '    ENDCG } }\n'
            '}\n'),
        # 正例 1：`[Toggle(X)] X` 但**没有**同名 HLSL 变量 ⇒ 合法（只 WARN，不失败）
        "pos_toggle_property.shader": (
            'Shader "T/Pos1" {\n'
            '    Properties {\n'
            '        [Toggle(_X)] _X ("X", Float) = 0\n'
            '    }\n'
            '    SubShader { Pass { CGPROGRAM\n'
            '        float4 _Color;\n'
            '        #ifdef _X\n'
            '        #endif\n'
            '    ENDCG } }\n'
            '}\n'),
        # 正例 2：注释里的 `[Toggle]` / `float _Y;` 不算（剥注释）
        "pos_commented.shader": (
            'Shader "T/Pos2" {\n'
            '    // [Toggle(_Y)] float _Y;\n'
            '    /* float _Z; #pragma shader_feature_local _Z */\n'
            '    SubShader { Pass { CGPROGRAM\n'
            '        float4 _Color;\n'
            '    ENDCG } }\n'
            '}\n'),
        # 正例 3：函数声明不该被当成变量
        "pos_function.shader": (
            'Shader "T/Pos3" {\n'
            '    SubShader { Pass { CGPROGRAM\n'
            '        #pragma shader_feature_local _FOO\n'
            '        float4 frag (float4 x) : SV_Target { return x; }\n'
            '    ENDCG } }\n'
            '}\n'),
    }
    # (期望 rc, 期望真冲突条数)
    expect = {
        "neg_toggle_float.shader": (1, 1),
        "neg_pragma_cbuffer.shader": (1, 1),
        "neg_keywordenum.shader": (1, 1),
        "pos_toggle_property.shader": (0, 0),
        "pos_commented.shader": (0, 0),
        "pos_function.shader": (0, 0),
    }
    ok = True
    print("== {prog} --self-test ==".format(prog=PROG))
    for name, want in expect.items():
        path = os.path.join(tmp, name)
        with io.open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(samples[name])
        rc, n = _child_rc_count(path)
        good = (rc == want[0] and n == want[1])
        ok = ok and good
        print("{mark} {name}: rc={rc} 冲突={n}（期望 rc={wr} 冲突={wn}）".format(
            mark="✓" if good else "✗", name=name, rc=rc, n=n,
            wr=want[0], wn=want[1]))
    print("自测结论：{v}".format(v="PASS" if ok else "FAIL"))
    return 0 if ok else 1


def _child_rc_count(path):
    import subprocess
    cmd = [sys.executable, "-I", "-X", "utf8", os.path.abspath(__file__), path, "--json"]
    p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    try:
        payload = json.loads(p.stdout.decode("utf-8"))
        n = payload["totals"]["conflicts"]
    except Exception:
        n = -1
    return p.returncode, n


def main(argv):
    if argv and argv[0] in ("-h", "--help"):
        print(USAGE)
        print()
        print(__doc__.split("## 用法", 1)[1].split("## 推荐调用", 1)[0].rstrip())
        return 0

    if "--self-test" in argv:
        return _run_self_test()

    paths = []
    as_json = False
    quiet = False
    show_warn = True
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--json":
            as_json = True
        elif a == "--quiet":
            quiet = True
        elif a == "--no-warn":
            show_warn = False
        elif a == "--root":
            i += 1
            if i >= len(argv):
                print("✗ --root 缺少数值", file=sys.stderr)
                return 2
            paths.append(argv[i])
        elif a.startswith("--root="):
            paths.append(a.split("=", 1)[1])
        elif a.startswith("-"):
            print("✗ 未知选项：{a}".format(a=a), file=sys.stderr)
            print(USAGE)
            return 2
        else:
            paths.append(a)
        i += 1

    if not paths:
        paths = [DEFAULT_ROOT]

    try:
        files = collect_files(paths)
    except IOError as e:
        print("✗ {e}".format(e=e), file=sys.stderr)
        return 2
    if not files:
        print("✗ 没找到任何 .shader（扫过：{p}）".format(p=", ".join(paths)), file=sys.stderr)
        return 2

    results = []
    for f in files:
        try:
            results.append(check_file(f))
        except (IOError, OSError) as e:
            print("✗ 读不到 {f}：{e}".format(f=f, e=e), file=sys.stderr)
            return 2

    if as_json:
        payload = {
            "ok": all(r["ok"] for r in results),
            "results": results,
            "totals": {
                "files": len(results),
                "keywords": sum(r["keyword_count"] for r in results),
                "variables": sum(r["variable_count"] for r in results),
                "conflicts": sum(len(r["conflicts"]) for r in results),
                "warns": sum(len(r["warns"]) for r in results),
            },
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0 if payload["ok"] else 1

    text, rc = _fmt_text(results, quiet, show_warn)
    print(text)
    return rc


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
