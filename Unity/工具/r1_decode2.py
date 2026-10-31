#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""r1_decode2.py — R1 第二批解码（905 对象里可解的那些）· **B106_W53 修了 m_Script 早读 3 字节**

来历
    真身原在 `d:/4/_tmp_view/wfres/r1_decode2.py`（2026-10-25 生成 `r1_decoded2/` 那 885 份 JSON）。
    2026-10-31（B106_W53）搬进 `Unity/工具/` 并按 `B106_W36` §④-1 修好 `m_Script` 那个坑后重跑。

🔴 修的那个坑
    `TypeTreeGeneratorAPI` 从 IL2CPP 元数据生成的 MonoBehaviour 类型树里，**`m_Enabled` 节点漏了
    `kAlignBytes(0x4000)`**。Unity 在 `m_Enabled`(bool, 1 B) 之后真有 3 字节对齐填充，于是紧跟其后的
    `m_Script` PPtr **早读 3 字节**：
        实测 `#1430` 读出 `{m_FileID 16777216, m_PathID 76420218880}`（真值 `{1, 4555}`）
        实测 `#1425` 读出 `{m_FileID 16777216, m_PathID 34309406720}`
    （`16777216 = 1<<24`、`33554432 = 2<<24` —— **garbage 的指纹**：真 `m_FileID` 被当成高字节。
     垃圾 `m_PathID` 同理是 8 字节右移 24 位。）
    因为下一个字段 `m_Name` 是 string、自带对齐 ⇒ **读口自愈**，其余字段不受影响，`consumed` 仍 == `byte_size`，
    所以「校验通过」**发现不了**这个坑。
    修法 = 给 `m_Enabled` 节点 `m_MetaFlag |= 0x4000`（见 `_align_fix()`；与
    `工具/r1_residual_decode.py:_align_fix()` 同一处判据）。

产物
    <out>/r1_decoded2/<src>/<Type>_<path_id>.json   （解出的字段 JSON；>4 MB 的 `#1427` 故意不落盘）
    <out>/r1_decode2_status.json                    （每条：decoded / consumed / byte_size / dumped）

用法（⛔ 不带参数 = 只打印用法、退出码 0、零读零写）
    python -I -X utf8 d:/4/Unity/工具/r1_decode2.py                     # 用法
    python -I -X utf8 d:/4/Unity/工具/r1_decode2.py --probe             # 只读：打印 externals / MonoScript 盘存
    python -I -X utf8 d:/4/Unity/工具/r1_decode2.py --scan              # 只读：数现有 r1_decoded2 里坏掉的 m_Script
    python -I -X utf8 d:/4/Unity/工具/r1_decode2.py --verify --n 30     # 只读：随机抽 N 份，解析 m_Script -> 类名
    python -I -X utf8 d:/4/Unity/工具/r1_decode2.py --decode --write    # 真重跑（唯一的写盘动作）
    python -I -X utf8 d:/4/Unity/工具/r1_decode2.py --all --write       # decode 完再 verify

依赖：UnityPy 1.24.2 + TypeTreeGeneratorAPI（`D:/2/Warpforge_tools/py312/python.exe`）· 本仓 `工具/ttread.py`。
⛔ `d:/2` 全程只读（本工具只 `UnityPy.load` 原版数据文件，不写一个字节）。
"""
import argparse
import hashlib
import json
import os
import random
import sys
import time

GAME = r'd:/2/Warhammer 40k Warpforge'
DATA = os.path.join(GAME, 'Warpforge_Data')
TOOLS = os.path.dirname(os.path.abspath(__file__))

OUT = r'd:/4/_tmp_view/wfres'
FILES = ["globalgamemanagers", "globalgamemanagers.assets", "level0", "level1",
         "resources.assets", "sharedassets0.assets"]
# 原生类/内置 MonoScript 所在的额外文件（相对 DATA）
EXTRA_FILES = ["Resources/unity default resources", "Resources/unity_builtin_extra"]

TSV_905 = r'd:/4/Unity/资料/完全复刻/资源/R1_905对象归属.tsv'

ALIGN_BYTES = 0x4000
DUMP_LIMIT = 4_000_000       # 与原脚本一致：>4 MB 不落盘（`resources.assets #1427 I2Languages` 10.7 MB）

sys.path.insert(0, TOOLS)


# ------------------------------------------------------------------ 基础设施
_gen = None


def gen_once():
    global _gen
    if _gen is not None:
        return _gen
    import UnityPy
    from UnityPy.helpers import TypeTreeGenerator as TTG

    def get_nodes_up_nodll(self, assembly, fullname):
        """UnityPy 的 helper 会自己加 `.dll` 后缀 ⇒ 每次 lookup 都失败；这里按原脚本的写法盖掉。"""
        key = (assembly, fullname)
        if key in self.cache:
            return self.cache[key]
        base = self.get_nodes(assembly, fullname)
        node = TTG.TypeTreeNode.from_list([
            TTG.TypeTreeNode(b.m_Level, b.m_Type, b.m_Name, 0, 0, m_MetaFlag=b.m_MetaFlag)
            for b in base])
        self.cache[key] = node
        return node

    TTG.TypeTreeGenerator.get_nodes_up = get_nodes_up_nodll
    g = TTG.TypeTreeGenerator('6000.2.6f2')
    g.load_local_game(GAME)
    _gen = g
    return g


def flat(node):
    out = []

    def walk(n):
        out.append({'m_Level': n.m_Level, 'm_Type': n.m_Type, 'm_Name': n.m_Name,
                    'm_MetaFlag': int(n.m_MetaFlag or 0)})
        for c in (n.m_Children or []):
            walk(c)
    walk(node)
    return out


def _align_fix(nodes):
    """🔴 本工具存在的理由：给 MonoBehaviour 根下的 `m_Enabled` 补 kAlignBytes(0x4000)。

    只改 **m_Level == 1**（根的直接子节点）的那一个 —— 深层同名节点（若有）不是那个 3 字节填充点，
    给它加对齐会把后面的字段整体推错。
    返回改了没（非 MonoBehaviour 对象没有 m_Enabled，返回 False 属正常）。
    """
    fixed = 0
    for n in nodes:
        if n['m_Name'] == 'm_Enabled' and n['m_Level'] == 1:
            n['m_MetaFlag'] |= ALIGN_BYTES
            fixed += 1
    return fixed


def load_env(name):
    import UnityPy
    return UnityPy.load(os.path.join(DATA, name))


def file_basename(p):
    return p.replace('\\', '/').split('/')[-1]


def ext_path(e):
    """externals 条目 -> 原始路径字符串。UnityPy 给的是 `FileIdentifier`，真路径在 `.path`。"""
    p = getattr(e, 'path', None)
    if p is None:
        s = str(e)
        if s.startswith('<FileIdentifier(') and s.endswith(')>'):
            s = s[len('<FileIdentifier('):-2]
        p = s
    return str(p)


def data_path(name):
    """把 externals 里的逻辑路径落到 `Warpforge_Data` 下的真实文件。

    ⚠️ 原版 log 里的 `Library/unity default resources` 在**构建产物里没有 Library 目录**，
    真文件在 `Warpforge_Data/Resources/unity default resources` ⇒ 按名字回退一层。
    """
    p = os.path.join(DATA, name.replace('\\', '/'))
    if os.path.isfile(p):
        return p
    b = file_basename(name)
    for rel in ('Resources/' + b, b):
        q = os.path.join(DATA, rel.replace('/', os.sep))
        if os.path.isfile(q):
            return q
    return p


def externals_of(env):
    """返回 [第 1 个外部文件的路径, ...]（m_FileID = 1 对应 [0]）。"""
    files = list(env.files.values()) if hasattr(env, 'files') else []
    if not files:
        return []
    exts = getattr(files[0], 'externals', None) or []
    return [ext_path(e) for e in exts]


def sha256_file(p):
    h = hashlib.sha256()
    with open(p, 'rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def write_text(path, text, crlf=True):
    """落盘（默认 CRLF）。

    ⚠️ 原脚本是 `open(..., 'w')` 文本模式，Windows 上写出的是 **CRLF**。这里**明确写 CRLF**
    而不是靠平台默认 —— 这样「修前 vs 修后」逐字节比时**只有 `m_Script` 那几行不同**，
    不会有 885 个文件「整体翻行尾」的噪声（本项目行尾纪律的同一考虑）。
    """
    if crlf:
        text = text.replace('\r\n', '\n').replace('\n', '\r\n')
    with open(path, 'wb') as f:
        f.write(text.encode('utf-8'))


# ------------------------------------------------------------------ --probe
def cmd_probe():
    print('[probe] 数据目录: %s' % DATA)
    for short in FILES:
        p = os.path.join(DATA, short)
        if not os.path.isfile(p):
            print('  %-28s <缺失>' % short)
            continue
        env = load_env(short)
        files = list(env.files.values()) if hasattr(env, 'files') else []
        exts = getattr(files[0], 'externals', None) or [] if files else []
        n_obj = len(env.objects)
        print('  %-28s objects=%-5d externals=%d' % (short, n_obj, len(exts)))
        for i, e in enumerate(exts):
            print('       [%d] %s' % (i + 1, ext_path(e)))
    print('  --- MonoScript 盘存（数每个文件里有多少 MonoScript 对象）---')
    for short in FILES + EXTRA_FILES:
        p = os.path.join(DATA, short)
        if not os.path.isfile(p):
            continue
        env = load_env(short)
        n = 0
        sample = None
        for obj in env.objects:
            if obj.type.name != 'MonoScript':
                continue
            n += 1
            if sample is None:
                try:
                    d = obj.read_typetree()
                    sample = '%s.%s @%d' % (d.get('m_Namespace') or '', d.get('m_ClassName'),
                                            obj.path_id)
                except Exception as e:                       # 读不出不算失败
                    sample = '<typetree 读不出: %s>' % type(e).__name__
        print('  %-34s MonoScript=%-5d  样例 %s' % (short, n, sample))
    return 0


# ------------------------------------------------------------------ --scan
def bad_script(v):
    """判一份解码 JSON 里的 m_Script 是不是垃圾值。返回 (坏不坏, 指纹)。

    🔴 两条指纹（都来自「PPtr 早读 3 字节」）：
      ① `m_FileID` 变成 `1<<24` 的倍数（`16777216` / `33554432` …）—— 真值恒在 0..externals 条数；
      ② `m_PathID` 变成真值 **左移 24 位**（实测 `#1430` 真值 4555 → 76420218880 = 4555<<24、
         `#544` 真值 12001 → 201343369216）⇒ **非 0 且能被 `1<<24` 整除** 就是它。
    ⚠️ 只用 ① 会**少数 27 份**：`globalgamemanagers.assets` 那批的 `m_FileID` 真值本来就是 0，
    错位后仍是 0（只是 `m_PathID` 错）⇒ **权威判据仍是 `--verify` 的「能不能解析出类名」**。
    """
    if not isinstance(v, dict):
        return True, 'not-a-dict'
    fid = v.get('m_FileID')
    pid = v.get('m_PathID')
    if not isinstance(fid, int) or not isinstance(pid, int):
        return True, 'not-int'
    if fid < 0 or fid > 64:
        return True, 'm_FileID'
    if pid != 0 and pid % (1 << 24) == 0:
        return True, 'm_PathID<<24'
    return False, ''


def scan_dir(dec_dir, verbose=False):
    """遍历 r1_decoded2 下所有 JSON，数 m_Script 坏掉的有几份。"""
    total = bad = bad_fid = 0
    errs = {}
    bad_names = []
    for root, _dirs, names in os.walk(dec_dir):
        for fn in sorted(names):
            if not fn.endswith('.json'):
                continue
            total += 1
            p = os.path.join(root, fn)
            try:
                d = json.load(open(p, encoding='utf-8'))
            except Exception as e:
                bad += 1
                errs[type(e).__name__] = errs.get(type(e).__name__, 0) + 1
                continue
            if 'm_Script' not in d:
                continue                      # 非 MonoBehaviour，没有这格
            isbad, why = bad_script(d['m_Script'])
            if isbad:
                bad += 1
                errs[why] = errs.get(why, 0) + 1
                if why in ('m_FileID', 'not-int', 'not-a-dict'):
                    bad_fid += 1
                if verbose:
                    bad_names.append('%s/%s  %s' % (os.path.basename(root), fn, why))
    return total, bad, bad_fid, errs, bad_names


def dec_dir_of(args):
    """解码产物目录：--dec-dir 显式指定，否则 <out>/r1_decoded2。"""
    return args.dec_dir or os.path.join(args.out, 'r1_decoded2')


def cmd_scan(args):
    dec = dec_dir_of(args)
    if not os.path.isdir(dec):
        print('找不到 %s' % dec)
        return 2
    total, bad, bad_fid, errs, names = scan_dir(dec, verbose=args.show_bad)
    print('[scan] %s' % dec)
    print('  解码后的 JSON 共 %d 份 · m_Script 垃圾 %d 份 · 正常 %d 份' % (total, bad, total - bad))
    print('  其中按 m_FileID 判坏的 %d 份（m_FileID 真值为 0 的那批只能靠 m_PathID 判，见 --verify）'
          % bad_fid)
    if errs:
        print('  垃圾指纹分布: %s' % (dict(sorted(errs.items(), key=lambda kv: -kv[1])),))
    for n in names[:args.n]:
        print('    ' + n)
    if len(names) > args.n:
        print('    …（共 %d 条，--n 只印前 %d）' % (len(names), args.n))
    return 0


# ------------------------------------------------------------------ --decode
def cmd_decode(args):
    raw_path = os.path.join(args.out, 'r1_raw.json')
    if not os.path.isfile(raw_path):
        print('找不到输入 %s' % raw_path)
        return 2
    raw = json.load(open(raw_path, encoding='utf-8'))
    wanted_by_src = {}
    for r in raw['rows']:
        wanted_by_src.setdefault(r['src'], {})[r['path_id']] = r

    t0 = time.time()
    gen = gen_once()
    print('generator ready %.1fs' % (time.time() - t0), flush=True)

    dec = os.path.join(args.out, 'r1_decoded2')
    status = []
    fixed_objs = 0
    for short in FILES:
        env = load_env(short)
        env.typetree_generator = gen
        wanted = wanted_by_src.get(short, {})
        d = os.path.join(dec, short)
        if args.write:
            os.makedirs(d, exist_ok=True)
        nok = nbad = nskip = 0
        for obj in env.objects:
            if obj.path_id not in wanted:
                continue
            rec = {"src": short, "path_id": obj.path_id, "type": obj.type.name}
            try:
                node = obj._get_typetree_node()
                nodes = flat(node)
                if _align_fix(nodes):
                    fixed_objs += 1
                data = obj.get_raw_data()
                rd = ttread.Reader(data, '>' if obj.reader.endian == '>' else '<')
                val = rd.read_root(nodes)
                if rd.p != len(data):
                    raise ValueError("consumed %d of %d bytes" % (rd.p, len(data)))
                fn = "%s_%d.json" % (obj.type.name, obj.path_id)
                dumped = len(data) <= DUMP_LIMIT
                if dumped and args.write:
                    write_text(os.path.join(d, fn),
                               json.dumps(val, ensure_ascii=False, indent=1, default=str))
                elif not dumped:
                    nskip += 1
                    print('  [skip dump] %s #%d %d B (> %d) — 解出来了，只是不落盘'
                          % (short, obj.path_id, len(data), DUMP_LIMIT), flush=True)
                rec.update(decoded=True, consumed=rd.p, byte_size=len(data), dumped=dumped,
                           key_count=len(val) if isinstance(val, dict) else 0)
                nok += 1
            except Exception as e:
                rec.update(decoded=False, err=("%s: %s" % (type(e).__name__, e))[:200])
                nbad += 1
            status.append(rec)
        print('%s: want=%d ok=%d fail=%d skip_dump=%d' % (short, len(wanted), nok, nbad, nskip),
              flush=True)

    print('补了 m_Enabled 对齐标记的对象: %d' % fixed_objs)

    # 与 pass 1 比对（若 pass1 产物还在）
    old_raw = os.path.join(args.out, 'r1_decode_status.json')
    if os.path.isfile(old_raw):
        old = {}
        for s in json.load(open(old_raw, encoding='utf-8')):
            old[(s['src'], s['path_id'])] = s.get('decoded')
        compared = same = 0
        diffs = []
        olddec = os.path.join(args.out, 'r1_decoded')
        for s in status:
            if not s.get('decoded'):
                continue
            k = (s['src'], s['path_id'])
            if not old.get(k):
                continue
            pa = os.path.join(olddec, s['src'], "%s_%d.json" % (s['type'], s['path_id']))
            pb = os.path.join(dec, s['src'], "%s_%d.json" % (s['type'], s['path_id']))
            if not (os.path.exists(pa) and os.path.exists(pb)):
                continue
            a = json.load(open(pa, encoding='utf-8'))
            b = json.load(open(pb, encoding='utf-8'))
            compared += 1
            if json.dumps(a, sort_keys=True, default=str) == json.dumps(b, sort_keys=True, default=str):
                same += 1
            else:
                diffs.append('%s/%s' % (s['src'], s['path_id']))
        print('AGREEMENT with pass1: %d/%d identical%s'
              % (same, compared, (' ; diffs: %s' % diffs[:10]) if diffs else ''), flush=True)

    if args.write:
        sp = os.path.join(args.out, 'r1_decode2_status.json')
        tmp = sp + '.tmp'
        write_text(tmp, json.dumps(status, ensure_ascii=False, indent=1))
        os.replace(tmp, sp)
        print('已写: %s' % sp)

    ok = sum(1 for s in status if s.get('decoded'))
    print('TOTAL %d: decoded=%d failed=%d' % (len(status), ok, len(status) - ok))
    return 0


# ------------------------------------------------------------------ --verify
def monoscript_index(short, cache={}):
    """文件（逻辑名或路径） -> {pathID: 'Namespace.ClassName'}"""
    if short in cache:
        return cache[short]
    p = data_path(short)
    idx = {}
    if os.path.isfile(p):
        import UnityPy
        env = UnityPy.load(p)
        for obj in env.objects:
            if obj.type.name != 'MonoScript':
                continue
            try:
                d = obj.read_typetree()
            except Exception:
                continue
            ns = d.get('m_Namespace') or ''
            cls = d.get('m_ClassName') or ''
            idx[obj.path_id] = ('%s.%s' % (ns, cls)) if ns else cls
    cache[short] = idx
    return idx


def resolve_script(src, script, ext_cache={}):
    """把 m_Script 的 {m_FileID, m_PathID} 解成 (目标文件, 类名 或 None)。"""
    fid = script.get('m_FileID')
    pid = script.get('m_PathID')
    if not isinstance(fid, int) or fid < 0 or fid > 64:
        return None, None, 'm_FileID 不合理 (%r)' % (fid,)
    if fid == 0:
        target = src
    else:
        if src not in ext_cache:
            ext_cache[src] = externals_of(load_env(src))
        exts = ext_cache[src]
        if fid - 1 >= len(exts):
            return None, None, 'm_FileID=%d 超出 externals(%d)' % (fid, len(exts))
        target = exts[fid - 1]
    name = monoscript_index(target).get(pid)
    if name is None:
        return target, None, 'pathID 在 %s 的 MonoScript 里查不到' % target
    return target, name, ''


def tsv_class_map():
    """(src, pathID) -> TSV 的 `类名` 列（旁证）。"""
    out = {}
    if not os.path.isfile(TSV_905):
        return out
    import io
    with io.open(TSV_905, encoding='utf-8-sig') as f:
        rows = [ln.rstrip('\r\n').split('\t') for ln in f]
    if not rows:
        return out
    for r in rows[1:]:
        if len(r) > 6:
            try:
                out[(r[0], int(r[1]))] = r[6]
            except ValueError:
                pass
    return out


def cmd_verify(args):
    dec = dec_dir_of(args)
    items = []
    for short in FILES:
        d = os.path.join(dec, short)
        if not os.path.isdir(d):
            continue
        for fn in sorted(os.listdir(d)):
            if fn.endswith('.json') and '_' in fn:
                pid = fn.rsplit('_', 1)[1][:-5]
                if pid.isdigit():
                    items.append((short, int(pid), os.path.join(d, fn)))
    if not items:
        print('没有可验证的 JSON（先跑 --decode --write）')
        return 2
    random.seed(args.seed)
    pick = items if args.n <= 0 or args.n >= len(items) else random.sample(items, args.n)
    pick = sorted(pick, key=lambda x: (x[0], x[1]))
    want = tsv_class_map()

    n_ok = n_unresolved = n_mismatch = 0
    rows = []
    for short, pid, p in pick:
        d = json.load(open(p, encoding='utf-8'))
        script = d.get('m_Script')
        if script is None:
            rows.append((short, pid, '-', '(无 m_Script)', '', ''))
            continue
        target, name, why = resolve_script(short, script)
        exp = want.get((short, pid), '')
        if name is None:
            n_unresolved += 1
            status = '未解析: ' + why
        elif exp and exp != name.split('.')[-1]:
            n_mismatch += 1
            status = '与 TSV 类名不符 (TSV=%s)' % exp
        else:
            n_ok += 1
            status = 'OK' + ('' if exp else ' (无 TSV 行可对照)')
        rows.append((short, pid, str(script), name or '-', target or '-', status))

    print('[verify] 抽样 %d / 共 %d 份（seed=%d）' % (len(pick), len(items), args.seed))
    print('  解析出合理类名 %d · 未解析 %d · 与 TSV 不符 %d' % (n_ok, n_unresolved, n_mismatch))
    print('  %-26s %-8s %-38s %-30s %s' % ('src', 'PathID', 'm_Script', '解析出的类名', '结论'))
    for r in rows:
        print('  %-26s %-8s %-38s %-30s %s' % (r[0], r[1], r[2], r[3], r[5]))
    return 0 if n_unresolved == 0 and n_mismatch == 0 else 1


# ------------------------------------------------------------------ main
def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument('--probe', action='store_true')
    ap.add_argument('--scan', action='store_true')
    ap.add_argument('--verify', action='store_true')
    ap.add_argument('--decode', action='store_true')
    ap.add_argument('--all', action='store_true')
    ap.add_argument('--write', action='store_true',
                    help='真的重跑解码并落盘（缺省一律只读）')
    ap.add_argument('--out', default=OUT, help='输出/输入目录（默认 %s）' % OUT)
    ap.add_argument('--dec-dir', default=None,
                    help='覆盖解码产物目录（--scan/--verify 用；默认 <out>/r1_decoded2）')
    ap.add_argument('--n', type=int, default=30, help='--verify 抽样份数 / --scan 印几条（0=全部）')
    ap.add_argument('--seed', type=int, default=10653)
    ap.add_argument('--show-bad', action='store_true', help='--scan 时把坏掉的逐条印出来')
    ap.add_argument('-h', '--help', action='store_true')
    args = ap.parse_args()

    if args.help or not (args.probe or args.scan or args.verify or args.decode or args.all):
        print(__doc__)
        return 0

    if not os.path.isdir(DATA):
        print('找不到原版数据目录: %s' % DATA, file=sys.stderr)
        return 2

    global ttread
    import ttread                                        # noqa: F401  （Reader 在 cmd_decode 里用）

    if args.probe:
        return cmd_probe()
    if args.scan:
        return cmd_scan(args)
    if args.decode or args.all:
        if not args.write:
            print('⛔ --decode 会覆盖 %s/r1_decoded2/ —— 必须显式加 --write' % args.out)
            print('   （只读查看请用 --scan / --verify / --probe）')
            return 3
        rc = cmd_decode(args)
        if rc:
            return rc
    if args.verify or args.all:
        return cmd_verify(args)
    return 0


if __name__ == '__main__':
    sys.exit(main())
