# -*- coding: utf-8 -*-
"""_mine_remote_cache.py -- 只读深挖 d:/2/远端缓存_旧profile_1013/ 的残余内容。

只读 d:/2（一个字节都不写）。中间产物写系统临时目录。

用法:
  python -I -X utf8 d:/4/Unity/工具/_mine_remote_cache.py <子命令> [参数]

子命令:
  tree                 列目录树 + 大小
  nrbf <file>          解析 .NET BinaryFormatter 流，输出对象树摘要
  nrbf-raw <file>      解析并 dump 成 JSON（默认只 stdout 摘要）
  catalog <file>       解析 Addressables catalog_main.json
"""
import sys, os, io, json, struct, base64, re

# ---------- MS-NRBF (BinaryFormatter) reader ----------
# 判据（实测自 d:/2/远端缓存_旧profile_1013/Everguild/PlayerDecks39.dat 的 value__ 字段）：
# 8 → 4 字节（Int32）；9 → 8 字节（Int64）。即 BinaryFormatter 走 InternalPrimitiveTypeE：
# 1 Boolean · 2 Byte · 3 Char · 5 Decimal · 6 Double · 7 Int16 · 8 Int32 · 9 Int64
# 10 SByte · 11 Single · 12 TimeSpan · 13 DateTime · 14 UInt16 · 15 UInt32 · 16 UInt64
PRIM = {1: ('B', 1, 'Boolean'), 2: ('B', 1, 'Byte'), 3: (None, 2, 'Char'),
        5: (None, 16, 'Decimal'), 6: ('d', 8, 'Double'), 7: ('h', 2, 'Int16'),
        8: ('i', 4, 'Int32'), 9: ('q', 8, 'Int64'), 10: ('b', 1, 'SByte'),
        11: ('f', 4, 'Single'), 12: ('q', 8, 'TimeSpan'), 13: ('q', 8, 'DateTime'),
        14: ('H', 2, 'UInt16'), 15: ('I', 4, 'UInt32'), 16: ('Q', 8, 'UInt64')}


class R:
    def __init__(self, b):
        self.b = b
        self.p = 0
        self.objs = {}

    def u8(self):
        v = self.b[self.p]; self.p += 1; return v

    def i32(self):
        v = struct.unpack_from('<i', self.b, self.p)[0]; self.p += 4; return v

    def i64(self):
        v = struct.unpack_from('<q', self.b, self.p)[0]; self.p += 8; return v

    def lpint(self):  # 7-bit encoded int
        r = 0; s = 0
        while True:
            b = self.b[self.p]; self.p += 1
            r |= (b & 0x7F) << s
            if not (b & 0x80):
                return r
            s += 7

    def lstr(self):
        n = self.lpint()
        s = self.b[self.p:self.p + n].decode('utf-8', 'replace')
        self.p += n
        return s

    def prim(self, t):
        fmt, sz, name = PRIM[t]
        raw = self.b[self.p:self.p + sz]
        self.p += sz
        if t == 4:
            return raw.hex()
        if t == 3:
            return raw.decode('utf-16-le', 'replace')
        if fmt == 'B':
            return bool(raw[0])
        return struct.unpack('<' + fmt, raw)[0]

    def read(self, tolerant=False):
        recs = []
        while self.p < len(self.b):
            start = self.p
            try:
                t = self.u8()
                v = self.rec(t)
            except Exception as e:
                recs.append(('ERR', (start, repr(e))))
                if not tolerant:
                    raise
                break
            recs.append((t, v))
            if t == 11:
                break
        return recs

    def classinfo(self):
        oid = self.i32()
        name = self.lstr()
        n = self.i32()
        names = [self.lstr() for _ in range(n)]
        return {'oid': oid, 'name': name, 'names': names}

    def membertypeinfo(self, n):
        bts = [self.u8() for _ in range(n)]
        add = []
        for bt in bts:
            # ObjectArray(5)/StringArray(6) 没有 AdditionalInfo
            if bt == 0:
                add.append(self.u8())
            elif bt == 3:
                add.append(self.lstr())
            elif bt == 4:
                add.append((self.lstr(), self.i32()))
            elif bt == 7:
                add.append(self.u8())
            else:
                add.append(None)
        return bts, add

    def values(self, names, bts, add):
        out = {}
        for i, nm in enumerate(names):
            bt = bts[i]
            if bt == 0:
                out[nm] = self.prim(add[i])
            else:
                # 🔴 String(1) 成员也是【记录】（BinaryObjectString 0x06 / MemberReference 0x09 /
                # ObjectNull 0x0A），不是裸 LengthPrefixedString。
                # 判据：本目录 PlayerDecks39.dat 解析到底、且 10.4 MB 的 deviceId 正好等于
                # 文件的全部剩余字节（若按裸字符串读，会在 1690 处跑飞）。
                out[nm] = self.rec_typed(bt, add[i])
        return out

    def rec_typed(self, bt, ai):
        # object-typed member: read a record
        t = self.u8()
        return self.rec(t, bt)

    def arr(self, container=None):
        # ArraySinglePrimitive/Object/String  -> ArrayInfo = oid, length(int32)
        oid = self.i32()
        ln = self.i32()
        return oid, ln

    def rec(self, t, expect_bt=None):
        if t == 0:
            return {'rootId': self.i32(), 'headerId': self.i32(),
                    'major': self.i32(), 'minor': self.i32()}
        if t == 1:  # ClassWithId
            oid = self.i32(); mid = self.i32()
            meta = self.objs.get(mid)
            names = meta['names'] if meta else []
            bts = meta.get('bts') if meta else []
            add = meta.get('add') if meta else []
            v = self.values(names, bts, add) if names else {}
            self.objs[oid] = {'name': meta['name'] if meta else '?', 'names': names,
                              'bts': bts, 'add': add}
            return {'__class': meta['name'] if meta else '?', '__oid': oid, 'v': v}
        if t == 2:  # SystemClassWithMembers
            ci = self.classinfo()
            v = self.values(ci['names'], [2] * len(ci['names']), [None] * len(ci['names']))
            self.objs[ci['oid']] = {'name': ci['name'], 'names': ci['names'], 'bts': [2] * len(ci['names']), 'add': [None] * len(ci['names'])}
            return {'__class': ci['name'], '__oid': ci['oid'], 'v': v}
        if t == 3:  # ClassWithMembers
            ci = self.classinfo(); lib = self.i32()
            v = self.values(ci['names'], [2] * len(ci['names']), [None] * len(ci['names']))
            self.objs[ci['oid']] = {'name': ci['name'], 'names': ci['names'], 'bts': [2] * len(ci['names']), 'add': [None] * len(ci['names'])}
            return {'__class': ci['name'], '__oid': ci['oid'], 'v': v}
        if t == 4:  # SystemClassWithMembersAndTypes
            ci = self.classinfo()
            bts, add = self.membertypeinfo(len(ci['names']))
            v = self.values(ci['names'], bts, add)
            self.objs[ci['oid']] = {'name': ci['name'], 'names': ci['names'], 'bts': bts, 'add': add}
            return {'__class': ci['name'], '__oid': ci['oid'], 'v': v}
        if t == 5:  # ClassWithMembersAndTypes
            ci = self.classinfo()
            bts, add = self.membertypeinfo(len(ci['names']))
            lib = self.i32()
            v = self.values(ci['names'], bts, add)
            self.objs[ci['oid']] = {'name': ci['name'], 'names': ci['names'], 'bts': bts, 'add': add}
            return {'__class': ci['name'], '__oid': ci['oid'], 'v': v}
        if t == 6:
            oid = self.i32(); s = self.lstr()
            return {'__str': s, '__oid': oid}
        if t == 7:
            oid = self.i32(); at = self.u8(); rank = self.i32()
            lens = [self.i32() for _ in range(rank)]
            total = 1
            for L in lens:
                total *= L
            bt = None; ai = None
            if at in (3, 4, 5):
                bt = self.u8()
                if bt == 0:
                    ai = self.u8()
                elif bt == 3:
                    ai = self.lstr()
                elif bt == 4:
                    ai = (self.lstr(), self.i32())
                elif bt == 5:
                    ai = (self.u8(), self.u8())
            elif at == 2:
                bt = 1
            if bt is not None and bt == 0:  # primitive array
                vals = [self.prim(ai) for _ in range(total)]
                return {'__arr': lens, '__prim': ai, 'v': vals}
            vals = []
            for _ in range(total):
                tt = self.u8()
                if tt == 9:
                    vals.append({'__ref': self.i32()})
                else:
                    vals.append(self.rec(tt))
            return {'__arr': lens, 'v': vals}
        if t == 8:
            pt = self.u8(); return {'__prim': pt, 'v': self.prim(pt)}
        if t == 9:
            return {'__ref': self.i32()}
        if t == 10:
            return None
        if t == 11:
            return '<END>'
        if t == 12:
            lid = self.i32(); nm = self.lstr()
            return {'__lib': lid, 'name': nm}
        if t == 13:
            n = self.u8(); return [None] * n
        if t == 14:
            n = self.i32(); return [None] * n
        if t == 15:
            ai = self.arr(); pt = self.u8()
            fmt, sz, nm = PRIM[pt]
            if pt == 2:
                vals = list(self.b[self.p:self.p + ai[1]]); self.p += ai[1]
            else:
                vals = [self.prim(pt) for _ in range(ai[1])]
            return {'__arr': ai, '__prim': nm, 'v': vals}
        if t == 16:
            ai = self.arr(); vals = [self.rec(self.u8()) for _ in range(ai[1])]
            return {'__arr': ai, 'v': vals}
        if t == 17:
            ai = self.arr()
            vals = []
            for _ in range(ai[1]):
                tt = self.u8()
                vals.append(self.rec(tt) if tt != 9 else {'__ref': self.i32()})
            return {'__arr': ai, 'v': vals}
        raise ValueError('unknown record type %d at %d' % (t, self.p - 1))


def summarize(o, depth=0, maxdepth=6, out=None):
    if out is None:
        out = []
    pad = '  ' * depth
    if depth > maxdepth:
        out.append(pad + '...')
        return out
    if isinstance(o, dict):
        if '__class' in o:
            out.append('%s%s #%s' % (pad, o['__class'], o.get('__oid')))
            for k, v in o['v'].items():
                if isinstance(v, (dict, list)) or (isinstance(v, dict) and '__arr' in v):
                    out.append('%s  .%s:' % (pad, k))
                    summarize(v, depth + 2, maxdepth, out)
                else:
                    out.append('%s  .%s = %r' % (pad, k, v))
        elif '__arr' in o:
            lens = o['__arr']
            if isinstance(lens, tuple):
                lens = lens[1]
            out.append('%s[array len=%s%s]' % (pad, lens, ' prim=' + str(o['__prim']) if '__prim' in o else ''))
            v = o['v']
            if isinstance(v, list) and len(v) <= 8:
                for x in v:
                    summarize(x, depth + 1, maxdepth, out)
            elif isinstance(v, list):
                out.append('%s  (first 3:)' % pad)
                for x in v[:3]:
                    summarize(x, depth + 2, maxdepth, out)
        elif '__str' in o:
            out.append('%s%r' % (pad, o['__str']))
        elif '__ref' in o:
            out.append('%s-> #%s' % (pad, o['__ref']))
        elif '__lib' in o:
            out.append('%sLIB %r' % (pad, o['name']))
        else:
            out.append('%s%r' % (pad, o))
    elif isinstance(o, list):
        for x in o:
            summarize(x, depth + 1, maxdepth, out)
    else:
        out.append('%s%r' % (pad, o))
    return out


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return
    cmd = sys.argv[1]
    if cmd == 'nrbf':
        p = sys.argv[2]
        b = open(p, 'rb').read()
        r = R(b)
        recs = r.read()
        for t, v in recs:
            print('RECORD type=%d' % t)
            for line in summarize(v, 0, 8):
                print(line)
    elif cmd == 'nrbf-top':
        p = sys.argv[2]
        b = open(p, 'rb').read()
        r = R(b)
        recs = r.read(tolerant=True)
        for t, v in recs:
            if t == 'ERR':
                print('!! PARSE ERROR at offset %d: %s' % v)
                continue
            if t == 5 and isinstance(v, dict) and '__class' in v:
                print('ROOT CLASS %s (oid %s)' % (v['__class'], v['__oid']))
                for k, val in v['v'].items():
                    if isinstance(val, dict) and '__str' in val:
                        s = val['__str']
                        print('  .%-38s str len=%-9d %r' % (k, len(s), s[:70]))
                    elif isinstance(val, dict) and '__ref' in val:
                        print('  .%-38s ->#%s' % (k, val['__ref']))
                    elif isinstance(val, dict) and '__arr' in val:
                        lens = val['__arr']
                        print('  .%-38s ARRAY %s prim=%s n=%d' % (
                            k, lens, val.get('__prim'), len(val.get('v') or [])))
                    elif isinstance(val, dict) and '__class' in val:
                        print('  .%-38s CLASS %s fields=%s' % (
                            k, val['__class'], list(val['v'].keys())))
                    elif isinstance(val, list):
                        print('  .%-38s NULLS x%d' % (k, len(val)))
                    else:
                        print('  .%-38s %r' % (k, val))
        print('--- classes seen in stream ---')
        seen = {}
        for oid, m in r.objs.items():
            seen.setdefault(m['name'], 0)
            seen[m['name']] += 1
        for nm, n in sorted(seen.items(), key=lambda x: -x[1])[:40]:
            print('  %4d x %s' % (n, nm))
    elif cmd == 'nrbf-classes':
        p = sys.argv[2]
        b = open(p, 'rb').read()
        r = R(b)
        r.read()
        seen = {}
        for oid, m in r.objs.items():
            seen.setdefault(m['name'], []).append((oid, m['names']))
        for nm, lst in sorted(seen.items()):
            print('%4d x %s' % (len(lst), nm))
            print('      fields:', lst[0][1])
    else:
        print(__doc__)


if __name__ == '__main__':
    main()
