#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""prefab_layer_diff.py — 「原版 prefab/场景 YAML ↔ 我们的树」逐层差异工具

为什么要有它
------------
用户 2026-11-03 指出：界面层大量还是旧方法一行行堆的，从来没回炉对过原版 prefab。
这份工具把「层级 / 几何 / 文字 / 图」逐条比成一张可复查的 TSV，而不是靠人一条条翻。

两侧输入（都可换）
------------------
  · yaml  = Unity 导出的 prefab / 场景 YAML
            例：d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity
  · dump  = SceneJumpShot（原版 mod）写的运行时 UI dump TSV
            例：d:/4/Unity/资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv

  ⚠️ **dump 那张表的表头列名是错位的**（本工程踩过多次）。表头只写了 11 列，
     真实是 15 列，顺序见 SceneJumpShot.cs 的 DumpGo：
        0 path · 1 name · 2 activeSelf · 3 activeInHierarchy · 4 rect(RT/TF) ·
        5 anchoredPosition · 6 sizeDelta · 7 anchorMin · 8 anchorMax · 9 pivot ·
        10 "TXT" 标记 · 11 text · 12 fontSize · 13 "IMG" 标记 · 14 sprite
     本工具**按真实列序读**，不看表头。表头错位这件事也写进了 `--list-src` 的输出。

对齐规则
--------
  · **按节点路径对齐**，不按顺序；对不上就是「仅原版有 / 仅我们有」，⛔ 不硬配。
  · 兄弟里重名的节点：第 2 个起加 `#2` / `#3` 后缀（两侧同一规则）。
  · 兄弟序按**父的 `m_Children` 序**（⛔ Unity 2023+/6 已删 `m_RootOrder`，那字段现在恒 0）。
  · 根 = `m_Father: {fileID: 0}` 的 Transform/RectTransform。

安全
----
  · **裸跑只打印用法并退出（rc=2），绝不写盘**。本工程踩过「写盘脚本裸跑就真写盘，冲掉 232 行手工修订」。
  · 真动作必须显式：`--orig ... --ours ... --out ...`；加 `--dry` 只预览不写。
  · 只读输入，只写 `--out` / `--summary` 指定的那两个文件。

用法
----
  # 0) 看有哪些 prefab / 场景可查
  python -I -X utf8 d:/4/Unity/工具/prefab_layer_diff.py --list [--grep HUD]

  # 1) 看一份源里有哪些根 / 某棵子树里有哪些节点
  python -I -X utf8 d:/4/Unity/工具/prefab_layer_diff.py --list-src <源文件> [--root BattleHud]

  # 2) 出差异表（--dry 只打印摘要）
  python -I -X utf8 d:/4/Unity/工具/prefab_layer_diff.py \
      --orig "<原版 .unity|.prefab>" [--root-orig BattleHud] \
      --ours "<我们侧 .unity|.py-dump .tsv>" [--root-ours BattleHud] \
      --out <tsv> [--summary <md>] [--only-diff] [--props all] [--tol 0.06] [--dry]

输出 TSV 列
-----------
  层路径 | 属性 | 原版值 | 我们值 | 判定(一致/不一致/仅原版有/仅我们有/?) | 原版出处 | 我们出处

「?」= 一侧或两侧**查不到这个属性**（不是「不一致」）。⛔ 不拿猜测填空。

已知限制（⛔ 都别当成「一致」）
------------------------------
  · 某属性在某一侧**整列都查不到** ⇒ 默认不展开（摘要里点名）；`--props all` 可强制展开，但那一侧全是 `?`。
  · dump 这一版的 **TXT 组整组坏**（`text` 全空、`fontSize` 全 `0.0`）⇒ 默认进 `--ours-dead-cols`，记 `?`。
  · 同名兄弟：两侧个数相同 → 按「字段差数最小」配对；个数不同 → 不配，摘要里出声。
  · 静态 YAML 与运行时 dump 本来就是两种状态（activeSelf / CanvasScaler 撑的 rect / 脚本定位），
    差异表 **不是缺陷表**。
  · 嵌套 prefab（`!u!1001`）不展开；本导出 0/1389 例，所以不影响。
"""

import glob
import io
import os
import re
import sys

# ---------------------------------------------------------------- 常量

ASSETS_ROOT = "d:/2/tools/assetripper_out/ExportedProject/Assets"
PKG_CACHE = "d:/4/_tmp_view/orig_ui_proj/Library/PackageCache"

# Unity classID
CID_GO = "1"
CID_TRANSFORM = "4"
CID_MONO = "114"
CID_CANVAS = "223"
CID_RECT = "224"
CID_CANVASGROUP = "225"

WANTED_CLASSES = {CID_GO, CID_TRANSFORM, CID_MONO, CID_CANVAS, CID_RECT, CID_CANVASGROUP}

# 全部可用属性（按输出顺序）
ALL_PROPS = [
    "parent",
    "activeSelf",
    "anchorMin",
    "anchorMax",
    "pivot",
    "sizeDelta",
    "anchoredPosition",
    "localPosition",
    "localScale",
    "localEuler",
    "text",
    "fontSize",
    "fontColor",
    "sprite",
    "imageColor",
    "imageType",
    "preserveAspect",
    "raycastTarget",
    "renderQueue",
    "sortingOrder",
    "canvasGroupAlpha",
]

# 默认属性集 = 两侧 dump 都可能有值的那些 + 规格要求的几项。
# ⚠️ renderQueue / imageColor / imageType 这类 dump 拿不到，默认**不展开**
#    （展开就是 1000+ 行「我们=?」的噪声）；要它们加 `--props all`。
DEFAULT_PROPS = [
    "parent",
    "activeSelf",
    "anchorMin",
    "anchorMax",
    "pivot",
    "sizeDelta",
    "anchoredPosition",
    "localPosition",
    "localScale",
    "text",
    "fontSize",
    "sprite",
]

UNKNOWN = "?"


def usage():
    return __doc__


def _exit_usage(rc=2):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    sys.stdout.write(usage())
    sys.stdout.write("\n（裸跑不做事。真动作请显式给 --orig / --ours / --out。）\n")
    sys.exit(rc)


# ---------------------------------------------------------------- 通用读文件

def read_lines(path):
    """二进制读 → 按 \\n 切 → 去尾部 \\r → 去 BOM。⛔ 不用文本模式（会把 \\r\\n 折成 \\n，
    也会把裸 CR 行数算错 —— 本工程踩过，见 CLAUDE.md 环境节）。"""
    b = io.open(path, "rb").read()
    if b.startswith(b"\xef\xbb\xbf"):
        b = b[3:]
    txt = b.decode("utf-8", "replace")
    return txt.split("\n")


def strip_cr(line):
    return line[:-1] if line.endswith("\r") else line


# ---------------------------------------------------------------- 迷你 YAML（Unity 子集）

_INLINE_MAP = re.compile(r"^\{(.*)\}$")
_NUM = re.compile(r"^-?\d+(\.\d+)?([eE][-+]?\d+)?$")


def _unquote(s):
    s = s.strip()
    if len(s) >= 2 and s[0] == "'" and s[-1] == "'":
        return s[1:-1].replace("''", "'")
    if len(s) >= 2 and s[0] == '"' and s[-1] == '"':
        body = s[1:-1]
        out = []
        i = 0
        while i < len(body):
            c = body[i]
            if c == "\\" and i + 1 < len(body):
                n = body[i + 1]
                m = {"n": "\n", "t": "\t", "r": "\r", '"': '"', "\\": "\\", "0": "\0"}
                if n in m:
                    out.append(m[n])
                    i += 2
                    continue
                if n == "u" and i + 5 < len(body) + 1:
                    try:
                        out.append(chr(int(body[i + 2:i + 6], 16)))
                        i += 6
                        continue
                    except Exception:
                        pass
                out.append(n)
                i += 2
                continue
            out.append(c)
            i += 1
        return "".join(out)
    return s


def _scalar(s):
    s = s.strip()
    if s == "":
        return ""
    if s == "[]":
        return []
    if s == "{}":
        return {}
    mm = _INLINE_MAP.match(s)
    if mm:
        return _parse_inline_map(mm.group(1))
    if s[0] in "'\"":
        return _unquote(s)
    return s


def _parse_inline_map(body):
    d = {}
    depth = 0
    cur = []
    parts = []
    for ch in body:
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
        if ch == "," and depth == 0:
            parts.append("".join(cur))
            cur = []
        else:
            cur.append(ch)
    if cur:
        parts.append("".join(cur))
    for p in parts:
        p = p.strip()
        if not p:
            continue
        idx = p.find(":")
        if idx < 0:
            continue
        k = p[:idx].strip()
        v = p[idx + 1:].strip()
        d[k] = _scalar(v)
    return d


def _split_kv(text):
    """'key: value' → (key, value)。key 里不会出现 ': '（Unity 的键都是标识符）。"""
    idx = text.find(": ")
    if idx >= 0:
        return text[:idx].strip(), text[idx + 2:]
    if text.endswith(":"):
        return text[:-1].strip(), ""
    idx = text.find(":")
    if idx >= 0:
        return text[:idx].strip(), text[idx + 1:]
    return text.strip(), ""


def parse_yaml_block(items, i, indent):
    """items = [(indent, text, lineno)]；返回 (value, next_i)。"""
    if i >= len(items) or items[i][0] < indent:
        return {}, i
    if items[i][1].startswith("- "):
        return _parse_seq(items, i, indent)
    return _parse_map(items, i, indent)


def _parse_seq(items, i, indent):
    out = []
    while i < len(items) and items[i][0] == indent and items[i][1].startswith("- "):
        rest = items[i][1][2:]
        if rest.strip() == "":
            v, i = parse_yaml_block(items, i + 1, items[i + 1][0] if i + 1 < len(items) else indent + 2)
            out.append(v)
            continue
        if rest.strip().startswith("- "):
            # 列表套列表（Unity 里少见）
            sub = [(indent + 2, rest.strip(), items[i][2])]
            j = i + 1
            while j < len(items) and items[j][0] == indent + 2 and items[j][1].startswith("- "):
                sub.append(items[j])
                j += 1
            v, _ = _parse_seq(sub, 0, indent + 2)
            out.append(v)
            i = j
            continue
        if ":" in rest and not rest.strip().startswith("{"):
            k, v = _split_kv(rest.strip())
            d = {}
            lin = items[i][2]
            i += 1
            if v.strip():
                d[k] = _scalar(v)
            else:
                if i < len(items) and items[i][0] > indent:
                    sub_indent = items[i][0]
                    vv, i = parse_yaml_block(items, i, sub_indent)
                    d[k] = vv
                else:
                    d[k] = ""
            # 同一 dict 的后续键（缩进 = indent + 2）
            while i < len(items) and items[i][0] == indent + 2 and not items[i][1].startswith("- "):
                k2, v2 = _split_kv(items[i][1])
                i += 1
                if v2.strip():
                    d[k2] = _scalar(v2)
                else:
                    if i < len(items) and items[i][0] > indent + 2:
                        vv, i = parse_yaml_block(items, i, items[i][0])
                        d[k2] = vv
                    else:
                        d[k2] = ""
            d["__line__"] = lin
            out.append(d)
            continue
        out.append(_scalar(rest))
        i += 1
    return out, i


def _parse_map(items, i, indent):
    d = {}
    while i < len(items) and items[i][0] == indent and not items[i][1].startswith("- "):
        text = items[i][1]
        k, v = _split_kv(text)
        lin = items[i][2]
        i += 1
        if v.strip():
            d[k] = _scalar(v)
        else:
            if i < len(items) and (items[i][0] > indent or
                                   (items[i][0] == indent and items[i][1].startswith("- "))):
                vv, i = parse_yaml_block(items, i, items[i][0])
                d[k] = vv
            else:
                d[k] = ""
        d.setdefault("__line__", lin)
    return d, i


_DOC_RE = re.compile(r"^--- !u!(\d+) &(-?\d+)")


def load_yaml_docs(path, wanted=None):
    """返回 {fileID: {'class': cid, 'line': n, 'body': dict}}（只解析 wanted 里的 classID）。"""
    lines = read_lines(path)
    marks = []
    for n, raw in enumerate(lines):
        s = strip_cr(raw)
        m = _DOC_RE.match(s)
        if m:
            marks.append((n, m.group(1), m.group(2)))
    docs = {}
    for idx, (n, cid, fid) in enumerate(marks):
        if wanted is not None and cid not in wanted:
            continue
        end = marks[idx + 1][0] if idx + 1 < len(marks) else len(lines)
        # 第一行是 'ClassName:'（已跳过），字段从下一行起
        items = []
        j = n + 1
        if j < end and strip_cr(lines[j]).strip().endswith(":"):
            j += 1
        base = None
        while j < end:
            s = strip_cr(lines[j])
            if s.strip() == "":
                j += 1
                continue
            ind = len(s) - len(s.lstrip(" "))
            if base is None:
                base = ind
            if ind < base:
                break
            items.append((ind, s.strip(), j + 1))
            j += 1
        try:
            body, _ = parse_yaml_block(items, 0, base if base is not None else 0)
        except Exception:
            body = {}
        if not isinstance(body, dict):
            body = {}
        body["__line__"] = n + 1
        docs[fid] = {"class": cid, "line": n + 1, "body": body, "doc_line": n + 1}
    return docs


# ---------------------------------------------------------------- guid → 名字 / 材质

class GuidMaps(object):
    def __init__(self, assets_root=ASSETS_ROOT, pkg_cache=PKG_CACHE):
        self.assets_root = assets_root
        self.pkg_cache = pkg_cache
        self._script = None
        self._sprite = None
        self._mat = None

    @staticmethod
    def _guid_of(meta_path):
        try:
            b = io.open(meta_path, "rb").read(1024)
        except Exception:
            return None
        m = re.search(rb"^guid:\s*([0-9a-fA-F]{32})", b, re.M)
        return m.group(1).decode("ascii") if m else None

    def script_map(self):
        if self._script is not None:
            return self._script
        m = {}
        for root in (self.assets_root, self.pkg_cache):
            if not root or not os.path.isdir(root):
                continue
            for p in glob.iglob(root + "/**/*.cs.meta", recursive=True):
                g = self._guid_of(p)
                if g:
                    m.setdefault(g, os.path.basename(p)[:-8])
        self._script = m
        return m

    def sprite_map(self):
        if self._sprite is not None:
            return self._sprite
        m = {}
        if self.assets_root and os.path.isdir(self.assets_root):
            for p in glob.iglob(self.assets_root + "/**/*.png.meta", recursive=True):
                g = self._guid_of(p)
                if g:
                    m.setdefault(g, os.path.basename(p)[:-9])
            for p in glob.iglob(self.assets_root + "/**/*.asset.meta", recursive=True):
                g = self._guid_of(p)
                if g:
                    m.setdefault(g, os.path.basename(p)[:-11])
        self._sprite = m
        return m

    def mat_queue(self, guid):
        if self._mat is None:
            m = {}
            if self.assets_root and os.path.isdir(self.assets_root):
                for p in glob.iglob(self.assets_root + "/**/*.mat.meta", recursive=True):
                    g = self._guid_of(p)
                    if not g:
                        continue
                    mat = p[:-5]
                    try:
                        txt = io.open(mat, "rb").read(8192).decode("utf-8", "replace")
                    except Exception:
                        continue
                    mm = re.search(r"m_CustomRenderQueue:\s*(-?\d+)", txt)
                    if mm:
                        m[g] = mm.group(1)
            self._mat = m
        return self._mat.get(guid)

    def script_name(self, guid):
        return self.script_map().get(guid)

    def sprite_name(self, guid):
        return self.sprite_map().get(guid)

    def material_queue(self, guid):
        return self.mat_queue(guid)


# ---------------------------------------------------------------- 数值格式化 / 比较

def fnum(s):
    """把 'a,b' / '{x: 1}' / '1.0' 变成 float 元组；拿不到返回 None。"""
    if s is None:
        return None
    if isinstance(s, (list, tuple)):
        out = []
        for x in s:
            try:
                out.append(float(x))
            except Exception:
                return None
        return tuple(out)
    t = str(s).strip()
    if t == "" or t == UNKNOWN:
        return None
    parts = [p.strip() for p in t.split(",")]
    out = []
    for p in parts:
        try:
            out.append(float(p))
        except Exception:
            return None
    return tuple(out)


def fmts(v):
    """数值 → 稳定字符串（去掉浮点噪声）。v 可以是 float 或元组。"""
    if v is None:
        return UNKNOWN
    if isinstance(v, (list, tuple)):
        return ",".join(fmts(x) for x in v)
    try:
        f = float(v)
    except Exception:
        return str(v)
    if f == int(f) and abs(f) < 1e15:
        return str(int(f))
    s = ("%.6f" % f).rstrip("0").rstrip(".")
    return s if s not in ("-0", "") else "0"


def same_num(a, b, tol):
    if a is None or b is None:
        return None
    if len(a) != len(b):
        return False
    for x, y in zip(a, b):
        if abs(x - y) > tol:
            return False
    return True


# ---------------------------------------------------------------- 节点模型

class Node(object):
    __slots__ = ("path", "name", "props", "src", "comp", "children", "rest")

    def __init__(self, path, name, src, comp=""):
        self.path = path
        self.name = name
        self.props = {}
        self.src = src
        self.comp = comp
        self.children = []
        self.rest = []


# ---------------------------------------------------------------- 源 1：Unity YAML

def _v2(d, key):
    v = d.get(key)
    if isinstance(v, dict):
        try:
            return (float(v.get("x", "0")), float(v.get("y", "0")))
        except Exception:
            return None
    if isinstance(v, list) and len(v) >= 2:
        try:
            return (float(v[0]), float(v[1]))
        except Exception:
            return None
    return None


def _v4(d, key):
    v = d.get(key)
    if isinstance(v, dict):
        try:
            return (float(v.get("r", "0")), float(v.get("g", "0")), float(v.get("b", "0")), float(v.get("a", "1")))
        except Exception:
            return None
    return None


def _fid(ref):
    if isinstance(ref, dict):
        return str(ref.get("fileID", "0"))
    return "0"


def _guid(ref):
    if isinstance(ref, dict):
        return ref.get("guid")
    return None


IMG_TYPE = {"0": "Simple", "1": "Sliced", "2": "Tiled", "3": "Filled"}


def load_yaml_tree(path, maps=None, want_component_label=True):
    """返回 (roots, all_nodes, meta)。roots 是 Node 列表（一条或多条根）。"""
    docs = load_yaml_docs(path, wanted=WANTED_CLASSES)

    gos, trans = {}, {}
    for fid, d in docs.items():
        if d["class"] == CID_GO:
            gos[fid] = d
        elif d["class"] in (CID_TRANSFORM, CID_RECT):
            trans[fid] = d

    # GO fid → 组件 docs
    comps_of = {}
    for fid, d in docs.items():
        if d["class"] in (CID_GO, CID_TRANSFORM, CID_RECT):
            continue
        go = _fid(d["body"].get("m_GameObject"))
        if go != "0":
            comps_of.setdefault(go, []).append(d)

    # 建 Node
    nodes = {}
    for fid, d in trans.items():
        go_fid = _fid(d["body"].get("m_GameObject"))
        go = gos.get(go_fid)
        name = (go["body"].get("m_Name") if go else "") or ""
        name = name if isinstance(name, str) else str(name)
        n = Node("", name, "%s:%d" % (os.path.basename(path), d["line"]))
        b = d["body"]
        is_rect = d["class"] == CID_RECT
        # activeSelf
        if go is not None:
            act = go["body"].get("m_IsActive")
            n.props["activeSelf"] = "True" if str(act).strip() in ("1", "True") else "False"
            n.src = "%s:%d" % (os.path.basename(path), go["line"])
        else:
            n.props["activeSelf"] = UNKNOWN
        lp = _v2(b, "m_LocalPosition")
        if lp is not None:
            n.props["localPosition"] = fmts((lp[0], lp[1], 0.0))
        else:
            n.props["localPosition"] = UNKNOWN
        sc = b.get("m_LocalScale")
        if isinstance(sc, dict):
            try:
                n.props["localScale"] = fmts((float(sc.get("x", "1")), float(sc.get("y", "1")), float(sc.get("z", "1"))))
            except Exception:
                n.props["localScale"] = UNKNOWN
        else:
            n.props["localScale"] = UNKNOWN
        # 旋转：UI 只看 z，用 m_LocalEulerAnglesHint
        lh = b.get("m_LocalEulerAnglesHint")
        if isinstance(lh, dict):
            try:
                n.props["localEuler"] = fmts((float(lh.get("x", "0")), float(lh.get("y", "0")), float(lh.get("z", "0"))))
            except Exception:
                n.props["localEuler"] = UNKNOWN
        else:
            n.props["localEuler"] = UNKNOWN

        if is_rect:
            n.props["anchorMin"] = fmts(_v2(b, "m_AnchorMin"))
            n.props["anchorMax"] = fmts(_v2(b, "m_AnchorMax"))
            n.props["pivot"] = fmts(_v2(b, "m_Pivot"))
            n.props["sizeDelta"] = fmts(_v2(b, "m_SizeDelta"))
            n.props["anchoredPosition"] = fmts(_v2(b, "m_AnchoredPosition"))
        else:
            for k in ("anchorMin", "anchorMax", "pivot", "sizeDelta", "anchoredPosition"):
                n.props[k] = UNKNOWN

        # 组件：text / image / canvas / canvasgroup
        labels = []
        best_text = None
        best_img = None
        for cd in comps_of.get(go_fid, []):
            cb = cd["body"]
            cname = maps.script_name(_guid(cb.get("m_Script"))) if maps else None
            if cname:
                labels.append(cname)
            if "m_text" in cb and best_text is None:
                best_text = (cd, cname or "TMP?")
            elif "m_Sprite" in cb and "m_Color" in cb and best_img is None:
                best_img = (cd, cname or "Image?")
            if cd["class"] == CID_CANVAS:
                try:
                    n.props["sortingOrder"] = fmts(float(cb.get("m_SortingOrder", "0")))
                except Exception:
                    n.props["sortingOrder"] = UNKNOWN
            if cd["class"] == CID_CANVASGROUP:
                try:
                    n.props["canvasGroupAlpha"] = fmts(float(cb.get("m_Alpha", "1")))
                except Exception:
                    n.props["canvasGroupAlpha"] = UNKNOWN

        n.props.setdefault("text", UNKNOWN)
        n.props.setdefault("fontSize", UNKNOWN)
        n.props.setdefault("fontColor", UNKNOWN)
        n.props.setdefault("sprite", UNKNOWN)
        n.props.setdefault("imageColor", UNKNOWN)
        n.props.setdefault("imageType", UNKNOWN)
        n.props.setdefault("preserveAspect", UNKNOWN)
        n.props.setdefault("raycastTarget", UNKNOWN)
        n.props.setdefault("renderQueue", UNKNOWN)
        n.props.setdefault("sortingOrder", UNKNOWN)
        n.props.setdefault("canvasGroupAlpha", UNKNOWN)

        if best_text is not None:
            cd, cname = best_text
            cb = cd["body"]
            txt = cb.get("m_text")
            if isinstance(txt, str):
                n.props["text"] = txt
            try:
                n.props["fontSize"] = fmts(float(cb.get("m_fontSize")))
            except Exception:
                pass
            fc = _v4(cb, "m_fontColor")
            if fc:
                n.props["fontColor"] = fmts(fc)
            if "m_RaycastTarget" in cb:
                n.props["raycastTarget"] = "True" if str(cb.get("m_RaycastTarget")).strip() in ("1", "True") else "False"
            n.comp = cname
        if best_img is not None:
            cd, cname = best_img
            cb = cd["body"]
            sp = _guid(cb.get("m_Sprite"))
            if sp:
                nm = maps.sprite_name(sp) if maps else None
                if nm:
                    n.props["sprite"] = nm
                elif sp.startswith("0000000000000000"):
                    # Unity 内置 sprite（unity_builtin_extra）—— 名字表不在本地，⛔ 不猜
                    n.props["sprite"] = "(内置:" + str(_fid(cb.get("m_Sprite"))) + ")"
                else:
                    n.props["sprite"] = "guid:" + sp[:8]
            else:
                n.props["sprite"] = "(none)"
            ic = _v4(cb, "m_Color")
            if ic:
                n.props["imageColor"] = fmts(ic)
            n.props["imageType"] = IMG_TYPE.get(str(cb.get("m_Type", "")).strip(), UNKNOWN)
            pa = cb.get("m_PreserveAspect")
            if pa is not None and str(pa) != "":
                n.props["preserveAspect"] = "True" if str(pa).strip() in ("1", "True") else "False"
            rt = cb.get("m_RaycastTarget")
            if rt is not None and str(rt) != "":
                n.props["raycastTarget"] = "True" if str(rt).strip() in ("1", "True") else "False"
            mg = _guid(cb.get("m_Material"))
            if mg and maps:
                q = maps.material_queue(mg)
                if q is not None:
                    n.props["renderQueue"] = q
            if not n.comp:
                n.comp = cname

        n.rest = labels
        nodes[fid] = n

    # 连父子
    roots = []
    for fid, d in trans.items():
        n = nodes[fid]
        father = _fid(d["body"].get("m_Father"))
        if father == "0" or father not in nodes:
            roots.append((fid, n))
        else:
            nodes[father].children.append(n)
    # 兄弟重名 → #k
    def assign(nodes_list, prefix):
        seen = {}
        for n in nodes_list:
            k = seen.get(n.name, 0) + 1
            seen[n.name] = k
            seg = n.name if k == 1 else "%s#%d" % (n.name, k)
            n.path = (prefix + "/" if prefix else "") + seg
            if n.path not in ("",):
                n.props["parent"] = prefix if prefix else "(root)"
            assign(n.children, n.path)
    rl = [n for _, n in roots]
    assign(rl, "")
    allnodes = list(nodes.values())
    meta = {"file": path, "docs": len(docs), "gos": len(gos), "transforms": len(trans), "roots": len(roots)}
    return rl, allnodes, meta


# ---------------------------------------------------------------- 源 2：SceneJumpShot dump TSV

DUMP_COLS = [
    "path", "name", "activeSelf", "activeInHierarchy", "rect",
    "anchoredPosition", "sizeDelta", "anchorMin", "anchorMax", "pivot",
    "_txt_mark", "text", "fontSize", "_img_mark", "sprite",
]
DUMP_HEADER_WRITTEN = ["path", "name", "activeSelf", "activeInHierarchy", "rect", "pos", "pivot",
                       "text", "fontSize", "color", "image"]


def load_dump_tree(path):
    raw = read_lines(path)
    rows = []
    for n, line in enumerate(raw):
        s = strip_cr(line)
        if not s.strip():
            continue
        rows.append((n + 1, s.split("\t")))
    if not rows:
        return [], [], {"file": path, "rows": 0}
    first = rows[0][1]
    start = 0
    header_note = "表头 ✅ 与真实列序一致"
    if first and first[0] == "path" and "anchoredPosition" not in first:
        start = 1
        if first == DUMP_HEADER_WRITTEN:
            header_note = "表头 ⚠️ 错位（写 11 列，真实 15 列；本工具按真实列序读）"
    nodes = []
    bypath = {}
    for lineno, r in rows[start:]:
        if len(r) < 15:
            r = r + [""] * (15 - len(r))
        d = dict(zip(DUMP_COLS, r[:15]))
        pcol = d["path"].strip()
        nm = d["name"]
        if not pcol and not nm:
            continue
        # 🔴 dump 的 `path` 列装的是【父节点的全路径】，不是自己的：
        #    SceneJumpShot.DumpGo 写的是传入的 path，而递归给子节点传的是 path + "/" + go.name。
        #    ⇒ 自己的全路径 = 父全路径 + "/" + 自己的 name；
        #      根行没有父（path 恰好等于自己的 name）⇒ 全路径 = name。
        #    「这一行的 path 是不是某个已见节点的全路径」可以判定：是 ⇒ 有父；不是 ⇒ 根。
        if pcol in bypath:
            full = pcol + "/" + nm
        else:
            full = pcol if pcol else nm
        n = Node(full, nm, "%s:%d" % (os.path.basename(path), lineno))
        n.props["parent"] = full.rsplit("/", 1)[0] if "/" in full else "(root)"
        n.props["activeSelf"] = d["activeSelf"] or UNKNOWN
        if d["rect"] in ("RT",):
            for k in ("anchoredPosition", "sizeDelta", "anchorMin", "anchorMax", "pivot"):
                n.props[k] = d[k] if d[k].strip() else UNKNOWN
            n.props["localPosition"] = UNKNOWN
        else:
            for k in ("anchoredPosition", "sizeDelta", "anchorMin", "anchorMax", "pivot"):
                n.props[k] = UNKNOWN
            n.props["localPosition"] = UNKNOWN
        n.props["localScale"] = UNKNOWN
        n.props["localEuler"] = UNKNOWN
        n.props["text"] = d["text"] if d["text"].strip() else UNKNOWN
        n.props["fontSize"] = d["fontSize"] if d["fontSize"].strip() else UNKNOWN
        n.props["sprite"] = d["sprite"] if d["sprite"].strip() else UNKNOWN
        for k in ("fontColor", "imageColor", "imageType", "preserveAspect", "raycastTarget",
                  "renderQueue", "sortingOrder", "canvasGroupAlpha"):
            n.props[k] = UNKNOWN
        n.comp = "TMP" if d["_txt_mark"] == "TXT" else ("Image" if d["_img_mark"] == "IMG" else "")
        nodes.append(n)
        bypath[full] = n
    for n in nodes:
        if "/" in n.path:
            par = bypath.get(n.path.rsplit("/", 1)[0])
            if par is not None:
                par.children.append(n)
    roots = [n for n in nodes if "/" not in n.path]
    # 🔴 兄弟重名 ⇒ 路径会撞（本场景 `Glow Orange` 就有 4 个）。与 YAML 侧同一规则加 `#k`，
    #    否则 `select_subtree` 的 dict 会把它们叠成一个。⛔ 不要按次序跨源配对 —— 见 repair_duplicate_paths。
    def _dedup(ns, prefix):
        seen = {}
        for n in ns:
            k = seen.get(n.name, 0) + 1
            seen[n.name] = k
            seg = n.name if k == 1 else "%s#%d" % (n.name, k)
            n.path = (prefix + "/" if prefix else "") + seg
            _dedup(n.children, n.path)
    _dedup(roots, "")
    return roots, nodes, {"file": path, "rows": len(nodes), "header": header_note}


# ---------------------------------------------------------------- 取子树 + 重定基

def select_subtree(nodes, root_hint, root_label=None):
    """按名字/路径后缀挑一棵子树，返回 (root_node_or_None, [nodes...])。
    重定基：root 自身 = `root_label`（默认用自己的名字），子孙 = `root_label/...`。
    ⇒ 两侧即使挂在不同的祖先下（`BattlePrefab/BattleHud` vs `BattlePrefab/BattlePrefab/BattleHud`）
      也能对齐；把两侧的 root_label 传成同一个值即可。"""
    if root_hint:
        cands = []
        for n in nodes:
            if n.path == root_hint or n.path.endswith("/" + root_hint) or n.name == root_hint:
                cands.append(n)
        if not cands:
            return None, []
        # 取路径最短的那个（最靠近根）
        cands.sort(key=lambda x: (len(x.path), x.path))
        root = cands[0]
        prefix = root.path
    else:
        roots = [n for n in nodes if "/" not in n.path]
        if len(roots) != 1:
            return None, []
        root = roots[0]
        prefix = root.path
    label = root_label if root_label is not None else root.name
    out = []
    for n in nodes:
        if n.path == prefix:
            out.append((n, label))
        elif n.path.startswith(prefix + "/"):
            out.append((n, label + "/" + n.path[len(prefix) + 1:]))
    # 重定基：parent 也要跟着换成相对路径，否则两侧前缀不同会整列刷成「不一致」
    for n, rel in out:
        n.props["parent"] = rel.rsplit("/", 1)[0] if "/" in rel else "(root)"
    out.sort(key=lambda x: x[1])
    return root, out


# ---------------------------------------------------------------- 同名兄弟的内容配对
#
# 为什么需要：兄弟里可以有多个同名节点（本场景 `Glow Orange` 就有 4 个）。
# 静态场景与运行时 dump 的**子节点次序不保证一致**（AssetRipper 也会重排子节点），
# 按次序编 `#k` 会把 A 配到 B 上，凭空刷出「不一致」+「仅原版有」。
# ⇒ 同名组**两侧个数相同**时，按「字段差数最小」贪心配对，再重新编号。
#   个数不同就不配（保持原样，并在摘要里出声）—— ⛔ 不硬配。

_PAIR_PROPS = ("anchorMin", "anchorMax", "pivot", "sizeDelta", "anchoredPosition",
               "activeSelf", "sprite", "text", "fontSize")


def _pair_cost(a, b, tol):
    c = 0.0
    n = 0
    for p in _PAIR_PROPS:
        va = a.props.get(p, UNKNOWN)
        vb = b.props.get(p, UNKNOWN)
        if va == UNKNOWN or vb == UNKNOWN:
            continue
        n += 1
        if p in ("anchorMin", "anchorMax", "pivot", "sizeDelta", "anchoredPosition", "fontSize"):
            eq = same_num(fnum(va), fnum(vb), tol)
            if eq is False:
                c += 1
        elif p == "sprite":
            if norm_sprite(va) != norm_sprite(vb):
                c += 1
        elif str(va).strip() != str(vb).strip():
            c += 1
    return c / n if n else 0.5


def repair_duplicate_paths(oitems, uitems, tol):
    """把两侧同名兄弟按内容配对，重写 `#k` 后缀。返回 (新 oitems, 新 uitems, 报告行)。"""
    om = dict((rel, n) for n, rel in oitems)
    um = dict((rel, n) for n, rel in uitems)
    if not om or not um:
        return oitems, uitems, []
    root_rel = sorted(om, key=lambda x: x.count("/"))[0]
    root_rel_u = sorted(um, key=lambda x: x.count("/"))[0]

    def children(m, rel):
        out = []
        if rel is None:
            return out
        pre = rel + "/"
        for r in m:
            if r.startswith(pre) and "/" not in r[len(pre):]:
                out.append(r)
        return out

    notes = []
    new_o = {}
    new_u = {}

    def walk(ro_old, ro_new, ru_old, ru_new):
        co = children(om, ro_old)
        cu = children(um, ru_old)
        go, gu = {}, {}
        for r in co:
            go.setdefault(om[r].name, []).append(r)
        for r in cu:
            gu.setdefault(um[r].name, []).append(r)
        for name in sorted(set(go) | set(gu)):
            lo = go.get(name, [])
            lu = gu.get(name, [])
            if len(lo) == 1 and len(lu) == 1:
                pairs = [(name, lo[0], lu[0])]
            elif lo and len(lo) == len(lu):
                # 贪心最小代价配对（个数相同才配）
                cand = []
                for i, a in enumerate(lo):
                    for j, b in enumerate(lu):
                        cand.append((_pair_cost(om[a], um[b], tol), i, j))
                cand.sort()
                usedi, usedj = set(), set()
                pairs = []
                for c, i, j in cand:
                    if i in usedi or j in usedj:
                        continue
                    usedi.add(i)
                    usedj.add(j)
                    pairs.append(("%s#%d" % (name, len(pairs) + 1), lo[i], lu[j]))
                notes.append("`%s` 同名 %d 个 → 按内容配对（不按次序）" % (name, len(lo)))
            else:
                # 个数对不上（或只有一侧有）：各走各的，⛔ 不硬配
                if lo and lu:
                    notes.append("⚠️ `%s` 同名个数不一致（原版 %d / 我们 %d）⇒ 未配对" % (name, len(lo), len(lu)))
                for i, a in enumerate(lo):
                    seg = name if len(lo) == 1 else "%s#%d" % (name, i + 1)
                    new_o[ro_new + "/" + seg] = om[a]
                    walk(a, ro_new + "/" + seg, None, None)
                for j, b in enumerate(lu):
                    seg = name if len(lu) == 1 else "%s#%d" % (name, j + 1)
                    new_u[ru_new + "/" + seg] = um[b]
                continue
            for seg, a, b in pairs:
                na = ro_new + "/" + seg
                nb = ru_new + "/" + seg
                new_o[na] = om[a]
                new_u[nb] = um[b]
                walk(a, na, b, nb)

    new_o[root_rel] = om[root_rel]
    new_u[root_rel_u] = um[root_rel_u]
    walk(root_rel, root_rel, root_rel_u, root_rel_u)
    o2 = [(n, rel) for rel, n in new_o.items()]
    u2 = [(n, rel) for rel, n in new_u.items()]
    for n, rel in o2 + u2:
        n.props["parent"] = rel.rsplit("/", 1)[0] if "/" in rel else "(root)"
    return o2, u2, notes


# ---------------------------------------------------------------- 差异

PROP_ORDER = {p: i for i, p in enumerate(ALL_PROPS)}


_SUBSPRITE = re.compile(r"_\d+$")


def norm_sprite(v):
    """子图命名归一：AssetRipper 导出的 atlas 子图叫 `X_0`，运行时 `sprite.name` 是 `X`。
    两边只差一个 `_<数字>` 后缀 = 同一张图（⛔ 不是差异）。"""
    if v is None:
        return v
    return _SUBSPRITE.sub("", str(v).strip())


def diff_trees(orig_items, ours_items, props, tol, only_diff, ours_dead_cols=()):
    omap = dict((p, n) for n, p in orig_items)
    nmap = dict((p, n) for n, p in ours_items)
    paths = sorted(set(omap) | set(nmap), key=lambda p: (p.count("/"), p))
    rows = []
    stats = {"一致": 0, "不一致": 0, "仅原版有": 0, "仅我们有": 0, "?": 0}
    node_stats = {"matched": 0, "orig_only": 0, "ours_only": 0}
    dead = set(ours_dead_cols or ())
    for p in paths:
        a = omap.get(p)
        b = nmap.get(p)
        if a is None:
            node_stats["ours_only"] += 1
            for prop in props:
                v = b.props.get(prop, UNKNOWN)
                if v == UNKNOWN:
                    continue
                rows.append([p, prop, UNKNOWN, v, "仅我们有", "-", b.src])
                stats["仅我们有"] += 1
            continue
        if b is None:
            node_stats["orig_only"] += 1
            for prop in props:
                v = a.props.get(prop, UNKNOWN)
                if v == UNKNOWN:
                    continue
                rows.append([p, prop, v, UNKNOWN, "仅原版有", a.src, "-"])
                stats["仅原版有"] += 1
            continue
        node_stats["matched"] += 1
        for prop in props:
            va = a.props.get(prop, UNKNOWN)
            vb = b.props.get(prop, UNKNOWN)
            if prop in dead:
                # 该列在「我们侧」这个源里已知不可用（如 dump 的 fontSize 恒 0.0）⇒ 记 ?，不记不一致
                verdict = "?"
            elif prop == "sprite":
                na, nb = norm_sprite(va), norm_sprite(vb)
                if va == UNKNOWN or vb == UNKNOWN or str(va).startswith("(内置") or str(vb).startswith("(内置"):
                    verdict = "?"
                elif str(va).strip() == str(vb).strip() or na == str(vb).strip() or str(va).strip() == nb:
                    # 只允许**一侧**去掉 `_<数字>` 后缀 —— 两侧都归一化会把真差异抹掉
                    # （实测 `40K_shop_offer_bg_Sororitas_0_2` vs `…_0`）
                    verdict = "一致"
                else:
                    verdict = "不一致"
            elif prop in ("anchorMin", "anchorMax", "pivot", "sizeDelta", "anchoredPosition",
                          "localPosition", "localScale", "localEuler", "sortingOrder",
                          "canvasGroupAlpha", "fontSize"):
                eq = same_num(fnum(va), fnum(vb), tol)
                if eq is None:
                    verdict = "?"
                elif eq:
                    verdict = "一致"
                else:
                    verdict = "不一致"
            else:
                if va == UNKNOWN or vb == UNKNOWN:
                    verdict = "?"
                elif str(va).strip() == str(vb).strip():
                    verdict = "一致"
                else:
                    verdict = "不一致"
            if only_diff and verdict in ("一致", "?"):
                continue
            if verdict == "?" and va == UNKNOWN and vb == UNKNOWN:
                continue  # 两侧都查不到 ⇒ 不占一行（`?` 只保留「一侧有、另一侧没有」）
            rows.append([p, prop, va, vb, verdict, a.src, b.src])
            stats[verdict] += 1
    return rows, stats, node_stats


# ---------------------------------------------------------------- 主流程

def _src_kind(path):
    ext = os.path.splitext(path)[1].lower()
    if ext in (".unity", ".prefab", ".asset"):
        return "yaml"
    if ext in (".tsv", ".txt", ".dump"):
        return "dump"
    return None


def _load_any(path, maps, want_label=True):
    k = _src_kind(path)
    if k == "yaml":
        return load_yaml_tree(path, maps)
    if k == "dump":
        return load_dump_tree(path)
    raise SystemExit("⛔ 认不出源类型：%s（只认 .unity/.prefab/.asset 或 .tsv）" % path)


def cmd_list(args):
    pat = None
    if "--grep" in args:
        pat = args[args.index("--grep") + 1]
    files = []
    for root in (ASSETS_ROOT,):
        files += glob.glob(root + "/**/*.prefab", recursive=True)
        files += glob.glob(root + "/**/*.unity", recursive=True)
    out = []
    for f in files:
        rel = os.path.relpath(f, ASSETS_ROOT).replace("\\", "/")
        if pat and pat.lower() not in rel.lower():
            continue
        out.append(rel)
    out.sort()
    sys.stdout.write("共 %d 个 prefab/场景（根 = %s）\n" % (len(out), ASSETS_ROOT))
    for r in out:
        sys.stdout.write(r + "\n")
    return 0


def cmd_list_src(args):
    path = args[args.index("--list-src") + 1]
    root = args[args.index("--root") + 1] if "--root" in args else None
    maps = GuidMaps()
    k = _src_kind(path)
    roots, nodes, meta = _load_any(path, maps)
    sys.stdout.write("源 = %s  (%s)\n" % (path, k))
    if k == "dump":
        sys.stdout.write("  " + meta.get("header", "") + "\n")
    sys.stdout.write("  节点 %d · 根 %d\n" % (len(nodes), len(roots)))
    for r in roots:
        sys.stdout.write("  根: %s\n" % r.path)
    if root:
        rnode, items = select_subtree(nodes, root)
        sys.stdout.write("\n子树 %s → %d 个节点（路径已相对 root 重定基）\n" % (root, len(items)))
        for n, rel in items:
            sys.stdout.write("  %s\t[%s]\n" % (rel, n.comp or ""))
    return 0


def main(argv):
    args = argv[1:]
    if not args:
        _exit_usage(2)
    if args[0] in ("-h", "--help"):
        _exit_usage(0)
    if "--list-src" in args:
        return cmd_list_src(args)
    if "--list" in args:
        return cmd_list(args)
    if "--orig" not in args or "--ours" not in args or "--out" not in args:
        sys.stderr.write("⛔ 真动作必须同时给 --orig / --ours / --out。\n")
        _exit_usage(2)
    orig = args[args.index("--orig") + 1]
    ours = args[args.index("--ours") + 1]
    out = args[args.index("--out") + 1]
    root_orig = args[args.index("--root-orig") + 1] if "--root-orig" in args else None
    root_ours = args[args.index("--root-ours") + 1] if "--root-ours" in args else None
    if root_ours is None:
        root_ours = root_orig
    summary = args[args.index("--summary") + 1] if "--summary" in args else None
    dry = "--dry" in args
    only_diff = "--only-diff" in args
    props_arg = args[args.index("--props") + 1] if "--props" in args else "default"
    tol = float(args[args.index("--tol") + 1]) if "--tol" in args else 0.06
    dead_arg = args[args.index("--ours-dead-cols") + 1] if "--ours-dead-cols" in args else "fontSize,text"
    ours_dead = [p.strip() for p in dead_arg.split(",") if p.strip()] if dead_arg else []
    force_all = "--props-all" in args or props_arg == "all"

    if props_arg == "all":
        props = list(ALL_PROPS)
    elif props_arg == "default":
        props = list(DEFAULT_PROPS)
    else:
        props = [p.strip() for p in props_arg.split(",") if p.strip()]
        bad = [p for p in props if p not in PROP_ORDER]
        if bad:
            raise SystemExit("⛔ 不认识的属性：%s（可用：%s）" % (",".join(bad), ",".join(ALL_PROPS)))

    maps = GuidMaps()
    oroots, onodes, ometa = _load_any(orig, maps)
    uroots, unodes, umeta = _load_any(ours, maps)

    oroot, oitems = select_subtree(onodes, root_orig)
    uroot, uitems = select_subtree(unodes, root_ours,
                                   root_label=(oroot.name if oroot is not None else None))
    if oroot is None:
        raise SystemExit("⛔ 原版侧挑不到子树（--root-orig %r）。先跑 --list-src 看有哪些根。" % (root_orig,))
    if uroot is None:
        raise SystemExit("⛔ 我们侧挑不到子树（--root-ours %r）。" % (root_ours,))

    pair_notes = []
    if "--no-pair-dups" not in args:
        oitems, uitems, pair_notes = repair_duplicate_paths(oitems, uitems, tol)

    # 某属性在某一侧**整列都查不到** ⇒ 两个源不具备可比性，默认不展开（记进摘要，⛔ 不静默丢）
    dropped = []
    if not force_all:
        keep = []
        for p in props:
            a_any = any(n.props.get(p, UNKNOWN) != UNKNOWN for n, _ in oitems)
            b_any = any(n.props.get(p, UNKNOWN) != UNKNOWN for n, _ in uitems)
            if a_any and b_any:
                keep.append(p)
            else:
                dropped.append(p)
        props = keep

    rows, stats, nstats = diff_trees(oitems, uitems, props, tol, only_diff, ours_dead)

    # 聚类
    by_prop = {}
    for p, prop, va, vb, verdict, sa, sb in rows:
        if verdict == "不一致":
            by_prop[prop] = by_prop.get(prop, 0) + 1
    total_cmp = stats["一致"] + stats["不一致"] + stats["?"]
    lines = []
    lines.append("原版侧 = %s（子树 %s · %d 节点）" % (orig, oroot.name, len(oitems)))
    lines.append("我们侧 = %s（子树 %s · %d 节点）" % (ours, uroot.name, len(uitems)))
    if _src_kind(ours) == "dump":
        lines.append("  " + umeta.get("header", ""))
    lines.append("节点对齐：对上 %d · 仅原版有 %d · 仅我们有 %d" % (
        nstats["matched"], nstats["orig_only"], nstats["ours_only"]))
    lines.append("属性比对 %d 条：一致 %d · 不一致 %d · ? %d" % (
        total_cmp, stats["一致"], stats["不一致"], stats["?"]))
    lines.append("仅原版有节点行 %d · 仅我们有节点行 %d" % (stats["仅原版有"], stats["仅我们有"]))
    lines.append("按属性聚类的「不一致」：" + ("、".join(
        "%s %d" % (k, v) for k, v in sorted(by_prop.items(), key=lambda x: -x[1])) or "（无）"))
    if dropped:
        lines.append("⚠️ 不可比而未展开的属性（某一侧整列都没有）：" + "、".join(dropped))
    if ours_dead:
        lines.append("⚠️ 我们侧已知不可用列（记 ? 不记不一致）：" + "、".join(ours_dead))
    for nt in pair_notes:
        lines.append("配对：" + nt)
    head = "\n".join(lines)

    sys.stdout.write(head + "\n\n")
    if dry:
        sys.stdout.write("--dry：未写盘。将要写 %s（%d 行）%s\n" % (
            out, len(rows), ("，摘要 " + summary) if summary else ""))
        return 0

    parent = os.path.dirname(out)
    if parent and not os.path.isdir(parent):
        os.makedirs(parent)
    with io.open(out, "w", encoding="utf-8", newline="\n") as w:
        w.write("层路径\t属性\t原版值\t我们值\t判定\t原版出处\t我们出处\n")
        for r in rows:
            w.write("\t".join("" if c is None else str(c).replace("\t", " ").replace("\n", "\\n") for c in r) + "\n")
    sys.stdout.write("已写 %s（%d 行）\n" % (out, len(rows)))

    if summary:
        with io.open(summary, "w", encoding="utf-8", newline="\n") as w:
            w.write(head + "\n")
        sys.stdout.write("已写 %s\n" % summary)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
