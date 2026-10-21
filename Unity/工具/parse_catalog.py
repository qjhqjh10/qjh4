# -*- coding: utf-8 -*-
"""parse_catalog.py —— Addressables 二进制 catalog（`catalog.bin` / `catalog_main.bin`）离线解析器（只读）。

## 这是什么（判据 / 出处）

Unity Addressables 2.x 的 `ContentCatalogData` 二进制格式（magic `0x0DE38942`、version `2`）。
本脚本**不依赖 Unity / Addressables 运行时**，纯 Python 读文件。

字段布局的判据（全部在本机可复查）：
  · `d:/2/Warpforge_code/Scripts/Unity.Addressables/UnityEngine/AddressableAssets/ResourceLocators/ContentCatalogData.cs`
    —— `ResourceLocator.Header` / `KeyData` / `ResourceLocation.Serializer.Data` /
       `AssetBundleRequestOptionsSerializationAdapter.SerializedData{,.Common}` 的字段顺序。
  · `d:/2/Warpforge_code/Scripts/Unity.ResourceManager/UnityEngine/ResourceManagement/Util/BinaryStorageBuffer.cs`
    —— `Reader.ReadString/ReadValue/ReadValueArray`、`kUnicodeStringFlag=0x80000000`、
       `kDynamicStringFlag=0x40000000`、`kClearFlagsMask=0x3FFFFFFF`、`DynamicString{stringId,nextId}`、
       `ObjectTypeData{typeId,objectId}`、`BuiltinTypesSerializer.ObjectToStringRemap{stringId,separator}`。
  · `d:/2/tools/il2cpp_out/dump.cs`（TypeDefIndex 20109/20110/20114/20119/20120）—— 同上字段偏移。

## 已经实测出来的编码约定（2026-10-19，Windows + Android 两份 catalog 都对上）

  · 普通字符串：`[u32 byteLen][UTF-8 bytes]`，**id 指向 bytes**（长度在 `id-4`）。
  · 动态字符串：id 带 `0x40000000`；指向一条 `DynamicString{stringId, nextId}` 链；
    **各段按链的逆序**拼接，段间插入调用点给的 `sep`（不是存在文件里的）。
  · Unicode 字符串：id 带 `0x80000000`（本机两份 catalog 均未出现）。
  · 数组：`[u32 byteSize][elements...]`，**id 指向 elements**（长度在 `id-4`）。
  · 对象：`ObjectTypeData{typeId, objectId}`；`typeId` → `TypeSerializer.Data{assemblyId, classId}`
    （两者都是字符串 id）；`objectId` → 该类型自己的 payload。
  · `0xFFFFFFFF` = null。

## 用法

    python -I -X utf8 d:/4/Unity/工具/parse_catalog.py <catalog.bin> \
        [--out DIR] [--bundles DIR]... [--cache DIR]... [--tail N]

默认输出目录 = `d:/4/Unity/资料/普查产出_1019/catalog_dump/<输入文件名去后缀>/`。
产物（全部确定性：不含时间戳、按 id 排序）：
    catalog_summary.txt    头部 / 计数 / provider 统计 / 几个头部字符串
    catalog_keys.tsv       每个 key：序号、key、类型、指向的 location 数 / id
    catalog_locations.tsv  每个 location：主键 / internalId / provider / 类型 / 依赖 / bundle 元数据
    catalog_bundles.tsv    87 个 AssetBundleProvider 条目 + 本地对账（--bundles/--cache）
    catalog_tail.txt       文件尾部 N 字节的结构解码（字符串 / 动态串 / 对象头）+ 谁引用它
    catalog_errors.txt     解析过程中"认不出/越界"的地方（有才写）

⛔ 只读输入；⛔ 不联网；⛔ 不猜：认不出的字段一律写 None / '?' 并在 errors 里记一行。

自检（🆕 2026-10-21，`A1787`）：加 `--check` ⇒ **一个字节都不写**，只比「盘上 `--out` 目录里的产物」
与「现在这份脚本 + 同样的参数会写出来的内容」；退出码 0 = 逐字节一致，1 = 有差异 / 缺产物。
（判据是**内容**，⛔ 不是 mtime —— 那条账最初就是被「脚本比产物新」这种 mtime 比较卡住的。）
"""
import argparse
import io
import os
import re
import struct
import sys

MAGIC = 0x0DE38942
K_UNICODE = 0x80000000
K_DYNAMIC = 0x40000000
K_MASK = 0x3FFFFFFF
NONE = 0xFFFFFFFF

DEFAULT_OUT_ROOT = r'd:/4/Unity/资料/普查产出_1019/catalog_dump'


class Catalog(object):
    def __init__(self, data, path='<mem>'):
        self.b = data
        self.N = len(data)
        self.path = path
        self.errors = []
        self.header = None

    # ---------------------------------------------------------------- 基础读
    def err(self, msg):
        if len(self.errors) < 5000:
            self.errors.append(msg)

    def u32(self, o):
        if o is None or o < 0 or o + 4 > self.N:
            return None
        return struct.unpack_from('<I', self.b, o)[0]

    def i32(self, o):
        if o is None or o < 0 or o + 4 > self.N:
            return None
        return struct.unpack_from('<i', self.b, o)[0]

    def raw(self, o, n):
        if o is None or o < 0 or o + n > self.N:
            return None
        return self.b[o:o + n]

    def plain(self, o):
        """`[u32 len][utf-8]`，o 指向字符串字节。"""
        if o is None or o < 4 or o >= self.N:
            return None
        ln = self.u32(o - 4)
        if ln is None or not (0 <= ln <= 65535) or o + ln > self.N:
            return None
        try:
            s = self.b[o:o + ln].decode('utf-8')
        except UnicodeDecodeError:
            return None
        # catalog 里出现的都是可打印 ASCII；控制字符按"不是字符串"处理（避免把二进制当字符串）
        if all(32 <= ord(c) < 127 for c in s):
            return s
        return None

    def readstr(self, v, sep='\0'):
        """Addressables BinaryStorageBuffer.Reader.ReadString 的等价实现。"""
        if v is None or v == NONE:
            return None
        if v & K_UNICODE:
            o = v & K_MASK
            ln = self.u32(o - 4)
            if ln is None or o + ln * 2 > self.N:
                return None
            try:
                return self.b[o:o + ln * 2].decode('utf-16-le')
            except UnicodeDecodeError:
                return None
        if v & K_DYNAMIC:
            return self.read_dynamic(v & K_MASK, sep)
        return self.plain(v & K_MASK)

    def read_dynamic(self, off, sep='\0'):
        """DynamicString 链：{stringId, nextId}；各段按链逆序拼接，段间插 sep。"""
        if off is None or not (4 <= off < self.N - 8):
            return None
        parts = []
        seen = set()
        t = off
        while t is not None and 4 <= t < self.N - 8 and t not in seen and len(parts) < 200:
            seen.add(t)
            sid = self.u32(t)
            nxt = self.u32(t + 4)
            parts.append(sid)
            if nxt in (None, NONE) or not (4 <= nxt < self.N - 8):
                break
            t = nxt
        if not parts:
            return None
        vals = []
        for sid in parts:
            s = self.readstr(sid) if sid is not None else None
            if s is None:
                return None
            vals.append(s)
        vals.reverse()
        # sep='\0' 是 ReadString 的默认值 —— 表示"调用点没给分隔符"，按不插分隔符处理。
        return ('' if sep in ('\0', None) else sep).join(vals)

    def array(self, v):
        """`[u32 byteSize][u32...]`，v 指向元素区。返回 list 或 None。"""
        if v is None or v == NONE:
            return []
        o = v & K_MASK
        sz = self.u32(o - 4)
        if sz is None or sz > self.N or sz % 4:
            return None
        if o + sz > self.N:
            return None
        return [self.u32(o + 4 * i) for i in range(sz // 4)]

    # ---------------------------------------------------------------- 类型
    def type_name(self, tid):
        """TypeSerializer.Data{assemblyId, classId} -> (assembly, class)。"""
        if tid is None or tid == NONE:
            return (None, None)
        asm = self.readstr(self.u32(tid), '.')
        cls = self.readstr(self.u32(tid + 4), '.')
        return (asm, cls)

    # ---------------------------------------------------------------- 头部
    def parse_header(self):
        h = struct.unpack_from('<8I', self.b, 0) if self.N >= 32 else None
        if h is None:
            raise RuntimeError('文件不足 32 字节，不是 catalog')
        names = ['magic', 'version', 'keysOffset', 'idOffset',
                 'instanceProvider', 'sceneProvider', 'initObjectsArray', 'buildResultHash']
        self.header = dict(zip(names, h))
        if h[0] != MAGIC:
            raise RuntimeError('magic 不符：%08x（期望 %08x）' % (h[0], MAGIC))
        if h[1] != 2:
            self.err('version=%d（本脚本按 version 2 写；字段可能不同）' % h[1])
        return self.header

    # ---------------------------------------------------------------- keys
    def parse_keys(self):
        ko = self.header['keysOffset']
        sz = self.u32(ko - 4)
        if sz is None or sz % 8:
            raise RuntimeError('keysOffset-4 不是合法的 KeyData 数组长度：%s' % sz)
        n = sz // 8
        keys = []
        for i in range(n):
            kobj = self.u32(ko + 8 * i)
            locset = self.u32(ko + 8 * i + 4)
            keys.append(dict(index=i, key_offset=kobj, locset_offset=locset,
                             key=self.parse_key_object(kobj)))
        return keys

    def parse_key_object(self, kobj):
        """KeyData.keyNameOffset -> ObjectTypeData{typeId, objectId}。"""
        if kobj is None or kobj == NONE:
            return dict(value=None, type_asm=None, type_cls=None, raw=None)
        tid = self.u32(kobj)
        oid = self.u32(kobj + 4)
        asm, cls = self.type_name(tid)
        short = cls.rsplit('.', 1)[-1] if cls else None
        value = None
        raw = None
        if short == 'String':
            # ObjectToStringRemap{uint stringId; char separator}
            sid = self.u32(oid) if oid is not None else None
            sepv = self.u32(oid + 4) if oid is not None else None
            sep = chr(sepv & 0xFFFF) if sepv is not None else '\0'
            value = self.readstr(sid, sep)
            raw = dict(stringId=sid, separator=sep)
        elif short in ('Int32', 'UInt32', 'Int64', 'UInt64',
                       'Boolean', 'Byte', 'Single'):
            # 值类型：objectId 指向原始值
            value = self.u32(oid)
            raw = dict(value_offset=oid)
        else:
            self.err('key #? 的类型没实现：%s / %s（offset=%#x）' % (asm, cls, kobj))
        return dict(value=value, type_asm=asm, type_cls=cls, raw=raw)

    # ---------------------------------------------------------------- locations
    def parse_location(self, lid):
        d = struct.unpack_from('<7I', self.b, lid) if lid is not None and lid + 28 <= self.N else None
        if d is None:
            self.err('location id 越界：%#x' % (lid or 0))
            return None
        asm, cls = self.type_name(d[6])
        return dict(
            id=lid,
            primary_key=self.readstr(d[0], '/'),
            internal_id=self.readstr(d[1], '/'),
            provider=self.readstr(d[2], '.'),
            dependency_set=self.array(d[3]),
            dependency_hash=self.i32(lid + 16),
            extra_offset=d[5],
            extra=self.parse_extra(d[5]),
            type_asm=asm,
            type_cls=cls,
        )

    def parse_extra(self, e):
        if e is None or e in (0, NONE):
            return None
        tid = self.u32(e)
        oid = self.u32(e + 4)
        asm, cls = self.type_name(tid)
        short = cls.rsplit('.', 1)[-1] if cls else None
        out = dict(offset=e, type_asm=asm, type_cls=cls, object_offset=oid)
        if short == 'AssetBundleRequestOptions':
            # SerializedData{uint hashId; uint bundleNameId; uint crc; uint bundleSize; uint commonId}
            vals = struct.unpack_from('<5I', self.b, oid) if oid is not None and oid + 20 <= self.N else None
            if vals is None:
                self.err('AssetBundleRequestOptions 的 SerializedData 越界：%#x' % (oid or 0))
                return out
            hash_off, bname_id, crc, size, common_off = vals
            h = self.raw(hash_off, 16)
            out.update(
                hash_offset=hash_off,
                hash128=h.hex() if h is not None else None,
                bundle_name_id=bname_id,
                bundle_name=self.readstr(bname_id),
                crc=crc,
                bundle_size=size,
                common_offset=common_off,
            )
            common = self.raw(common_off, 8)
            if common is not None:
                timeout, redirect, retry, flags = struct.unpack('<hBBI', common)
                out.update(timeout=timeout, redirect_limit=redirect,
                           retry_count=retry, flags=flags)
        else:
            # 其他 extraData 类型（本机两份 catalog 里只有 AssetBundleRequestOptions）
            out['unhandled'] = True
        return out

    # ---------------------------------------------------------------- 全图
    def parse_all(self):
        self.parse_header()
        keys = self.parse_keys()
        loc_ids = set()
        for k in keys:
            for lid in (self.array(k['locset_offset']) or []):
                loc_ids.add(lid)
        locs = {}
        for lid in sorted(loc_ids):
            loc = self.parse_location(lid)
            if loc is not None:
                locs[lid] = loc
        return keys, locs


# -------------------------------------------------------------------- 输出工具
# 🆕 2026-10-21（`A1787`）：`--check` 由 `main()` 置位 ⇒ `_w` 改成「只比不写」，
#   差异收进 `_CHECK_BAD`，由 `main()` 汇总打印并以 1 退出。⛔ 只读输入、⛔ 不建目录。
CHECK = False
_CHECK_BAD = []


def _w(path, text):
    """确定性写盘：UTF-8 无 BOM、LF。`--check` 下只比对、不写。"""
    data = text.encode('utf-8').replace(b'\r\n', b'\n')
    if CHECK:
        got = io.open(path, 'rb').read() if os.path.exists(path) else None
        if got != data:
            _CHECK_BAD.append((path, -1 if got is None else len(got), len(data)))
        return
    with io.open(path, 'wb') as f:
        f.write(data)


def _tsv(rows, header):
    out = ['\t'.join(header)]
    for r in rows:
        out.append('\t'.join('' if v is None else str(v) for v in r))
    return '\n'.join(out) + '\n'


def local_bundle_index(dirs):
    """列目录里的 *.bundle（非递归）→ {文件名: 完整路径}。"""
    idx = {}
    for d in dirs:
        if not os.path.isdir(d):
            continue
        for name in sorted(os.listdir(d)):
            p = os.path.join(d, name)
            if os.path.isfile(p) and name.lower().endswith('.bundle'):
                idx[name] = p
    return idx


def cache_index(dirs):
    """CCD/Android 缓存布局：<bundleNameHash>/<contentHash>/（目录，内含 __data）。
    → {(bundleNameHash, contentHash): (path, size)}。"""
    idx = {}
    for d in dirs:
        if not os.path.isdir(d):
            continue
        for name in sorted(os.listdir(d)):
            p1 = os.path.join(d, name)
            if not os.path.isdir(p1):
                continue
            for h in sorted(os.listdir(p1)):
                p2 = os.path.join(p1, h)
                data = os.path.join(p2, '__data')
                if os.path.isdir(p2):
                    idx[(name, h)] = (data if os.path.isfile(data) else p2,
                                      os.path.getsize(data) if os.path.isfile(data) else None)
    return idx


def base_name_of(internal_id):
    if not internal_id:
        return None
    s = internal_id.replace('\\', '/')
    return s.rsplit('/', 1)[-1]


def internal_kind(s):
    if not s:
        return 'none'
    if s.startswith('{UnityEngine.AddressableAssets.Addressables.RuntimePath}'):
        return 'local-runtimepath'
    if s.startswith('http://') or s.startswith('https://'):
        return 'remote-ccd'
    if len(s) <= 4 and all(c.isalnum() or c in '_-' for c in s):
        return 'short'
    if s.startswith('Assets/'):
        return 'assetpath'
    return 'other'


# -------------------------------------------------------------------- 尾部解码
def build_refs(cat, keys, locs):
    """offset -> [谁引用了它]。用于回答"尾部那些串到底属于哪一条 key/location"。"""
    refs = {}

    def add(off, desc):
        if isinstance(off, int) and 4 <= off < cat.N:
            refs.setdefault(off, []).append(desc)

    h = cat.header
    add(h['keysOffset'], 'header.keysOffset(KeyData[])')
    add(h['idOffset'], 'header.idOffset(locatorId)')
    add(h['buildResultHash'], 'header.buildResultHash')
    add(h['instanceProvider'], 'header.instanceProvider')
    add(h['sceneProvider'], 'header.sceneProvider')
    add(h['initObjectsArray'], 'header.initObjectsArray')
    for k in keys:
        idx = k['index']
        ko = k['key_offset']
        add(ko, 'key#%d KeyData.keyNameOffset' % idx)
        if ko is not None and ko + 8 <= cat.N:
            add(cat.u32(ko), 'key#%d typeId' % idx)
            add(cat.u32(ko + 4), 'key#%d ObjectToStringRemap' % idx)
            oid = cat.u32(ko + 4)
            if oid is not None and oid + 8 <= cat.N:
                add(cat.u32(oid), 'key#%d stringId' % idx)
                # 动态串链节点
                t = cat.u32(oid)
                if t is not None and (t & K_DYNAMIC):
                    t &= K_MASK
                    seen = set()
                    while 4 <= t < cat.N - 8 and t not in seen:
                        seen.add(t)
                        add(t, 'key#%d dynstr-node' % idx)
                        nxt = cat.u32(t + 4)
                        if nxt in (None, NONE):
                            break
                        t = nxt
                # 普通字符串的"长度前缀"位置也标出来（便于看尾部 [len][bytes]）
                if t is not None and not (t & (K_DYNAMIC | K_UNICODE)):
                    add((t & K_MASK) - 4, 'key#%d string-len-prefix' % idx)
        add(k['locset_offset'], 'key#%d locationSet-array' % idx)
        ids = cat.array(k['locset_offset'])
        for lid in (ids or []):
            add(lid, 'key#%d -> location' % idx)
    for lid in sorted(locs):
        l = locs[lid]
        add(lid, 'loc#%d Data' % lid)
        if lid + 28 > cat.N:
            continue
        d = struct.unpack_from('<7I', cat.b, lid)
        for name, v in zip(('primaryKey', 'internalId', 'provider', 'dependencySet',
                            'dependencyHash', 'extraData', 'typeId'), d):
            add(v, 'loc#%d %s' % (lid, name))
        for dep in (l['dependency_set'] or []):
            add(dep, 'loc#%d dependency' % lid)
        e = l['extra'] or {}
        if e:
            add(e.get('offset'), 'loc#%d extraData(ObjectTypeData)' % lid)
            add(e.get('object_offset'), 'loc#%d extraData.objectId' % lid)
            add(e.get('hash_offset'), 'loc#%d extra.hash128(16 bytes)' % lid)
            add(e.get('bundle_name_id'), 'loc#%d extra.bundleNameId' % lid)
            add(e.get('common_offset'), 'loc#%d extra.commonId' % lid)
        tid = d[6]
        if tid not in (None, NONE):
            add(cat.u32(tid), 'loc#%d type.assemblyId' % lid)
            add(cat.u32(tid + 4), 'loc#%d type.classId' % lid)
    return refs


def decode_tail(cat, nbytes, refs=None):
    """把文件最后 nbytes 字节按"结构 + 行内字符串"解码。
    这不是一张独立的字符串表 —— 它就是序列化对象图最后写入的那一段（见报告）。
    ⚠️ 这是**尽力而为**的注解：认不出的字节按原样打一行 16 进制。"""
    refs = refs or {}
    start = max(0, cat.N - nbytes)
    lines = []
    i = start
    while i < cat.N:
        ref = ''
        if i in refs:
            seen = refs[i]
            ref = '  <- ' + '; '.join(seen[:3]) + ((' (+%d more)' % (len(seen) - 3)) if len(seen) > 3 else '')

        def emit(kind, text):
            lines.append('%08x  %-9s %s%s' % (i, kind, text, ref))

        # 1) 普通字符串：[u32 len][bytes]
        ln = cat.u32(i)
        if ln is not None and 0 < ln <= 200 and i + 4 + ln <= cat.N:
            try:
                s = cat.b[i + 4:i + 4 + ln].decode('utf-8')
            except UnicodeDecodeError:
                s = None
            if s is not None and all(32 <= ord(c) < 127 for c in s):
                emit('STRING', 'len=%-3d %r' % (ln, s))
                i += 4 + ln
                continue
        sid = cat.u32(i)
        nxt = cat.u32(i + 4)
        # 1.5) 被引用为数组的元素区（[u32 byteSize] 在 i-4）—— 打印成 UINT-ARRAY
        if i in refs and any(('locationSet-array' in r) or ('dependency' in r) for r in refs[i]):
            sz = cat.u32(i - 4)
            if sz is not None and 0 <= sz <= cat.N and sz % 4 == 0 and i + sz <= cat.N:
                vals = [cat.u32(i + 4 * k) for k in range(sz // 4)]
                emit('UINT-ARRAY', 'byteSize=%d %s' % (sz, vals))
                i += sz if sz > 0 else 4
                continue
        # 2) DynamicString 节点：{stringId, nextId}
        if sid is not None and nxt is not None and (nxt == NONE or 4 <= nxt < cat.N - 8):
            part = cat.readstr(sid)
            if part:
                nxt_ok = (nxt == NONE) or (cat.readstr(cat.u32(nxt)) is not None)
                if nxt_ok:
                    emit('DYNSTR', 'stringId=%#x %r next=%#x%s'
                         % (sid, part, nxt, '' if nxt == NONE else ' (链)'))
                    i += 8
                    continue
        # 3) ObjectToStringRemap{stringId, char sep}（char 占低 16 位，高 16 位必须为 0）
        if sid is not None and nxt is not None and sid != NONE and (sid & (K_DYNAMIC | K_UNICODE)) \
                and (nxt & 0xFFFF0000) == 0 and (nxt & 0xFFFF) in (0,) + tuple(range(32, 127)):
            s = cat.readstr(sid)
            if s is not None:
                sep_c = chr(nxt & 0xFFFF)
                # 用 remap 里存的分隔符重拼一次（默认 '\0' 按"不插分隔符"）
                s2 = cat.readstr(sid, sep_c)
                emit('STR-REMAP', 'stringId=%#x sep=%r %r' % (sid, sep_c, s2 if s2 is not None else s))
                i += 8
                continue
        # 4) ObjectTypeData{typeId, objectId}（typeId 必须解出"像类型名"的字符串）
        if sid is not None and nxt is not None:
            asm, cls = cat.type_name(sid)
            if cls and len(cls) <= 200 and re.match(r'^[A-Za-z_][A-Za-z0-9_.+`<>\[\], ]*$', cls) \
                    and '.' in cls and ' ' not in cls:
                emit('OBJ-TYPE', 'typeId=%#x (%s) objectId=%#x' % (sid, cls, nxt))
                i += 8
                continue
        emit('BYTE', '%02x' % cat.b[i])
        i += 1
    return lines


# -------------------------------------------------------------------- main
def main(argv=None):
    global CHECK
    ap = argparse.ArgumentParser(description='Addressables binary catalog parser (read-only)')
    ap.add_argument('catalog', help='catalog.bin / catalog_main.bin')
    ap.add_argument('--out', default=None, help='输出目录（默认 %s/<stem>/）' % DEFAULT_OUT_ROOT)
    ap.add_argument('--bundles', action='append', default=[],
                    help='本地 bundle 目录（可多次；比对 internalId 的基名）')
    ap.add_argument('--cache', action='append', default=[],
                    help='CCD/Android 缓存目录（<bundleNameHash>/<contentHash>/__data 布局）')
    ap.add_argument('--tail', type=int, default=512, help='尾部解码字节数（默认 512）')
    ap.add_argument('--check', action='store_true',
                    help='🆕 A1787：只核对 --out 目录里的产物与现在会写出来的内容（⛔ 不写盘）')
    args = ap.parse_args(argv)
    CHECK = bool(args.check)
    del _CHECK_BAD[:]          # 同进程里被调用多次时别带上一轮的差异

    with io.open(args.catalog, 'rb') as f:
        data = f.read()
    cat = Catalog(data, args.catalog)
    try:
        keys, locs = cat.parse_all()
    except Exception as e:
        sys.stderr.write('解析失败：%s\n' % e)
        return 2

    outdir = args.out or os.path.join(DEFAULT_OUT_ROOT,
                                      os.path.splitext(os.path.basename(args.catalog))[0])
    if not CHECK and not os.path.isdir(outdir):
        os.makedirs(outdir)

    h = cat.header
    locator_id = cat.readstr(h['idOffset'])
    build_hash = cat.readstr(h['buildResultHash'])
    providers = {}
    for l in locs.values():
        providers[l['provider'] or '?'] = providers.get(l['provider'] or '?', 0) + 1
    bundles = [l for l in locs.values() if l['provider'] and 'AssetBundleProvider' in l['provider']]
    assets = [l for l in locs.values() if l['provider'] and 'BundledAssetProvider' in l['provider']]
    bundle_ids = set(l['id'] for l in bundles)
    asset_ids = set(l['id'] for l in assets)
    otherlocs = [l for l in locs.values()
                 if l['id'] not in bundle_ids and l['id'] not in asset_ids]

    # ---- summary
    s = []
    s.append('catalog: %s' % args.catalog)
    s.append('file size: %d' % cat.N)
    s.append('magic: %#010x   version: %d' % (h['magic'], h['version']))
    s.append('keysOffset: %#x   idOffset: %#x' % (h['keysOffset'], h['idOffset']))
    s.append('instanceProvider: %#x   sceneProvider: %#x' % (h['instanceProvider'], h['sceneProvider']))
    s.append('initObjectsArray: %#x   buildResultHash: %#x' % (h['initObjectsArray'], h['buildResultHash']))
    s.append('locatorId: %s' % locator_id)
    s.append('buildResultHash: %s' % build_hash)
    s.append('keys (KeyData[]): %d' % len(keys))
    s.append('unique locations: %d' % len(locs))
    for k in sorted(providers):
        s.append('  provider: %-80s %d' % (k, providers[k]))
    s.append('bundle locations (AssetBundleProvider): %d' % len(bundles))
    s.append('asset locations  (BundledAssetProvider): %d' % len(assets))
    if otherlocs:
        s.append('other locations: %d' % len(otherlocs))
    kk = {}
    for k in keys:
        t = k['key']['type_cls'] or '?'
        kk[t] = kk.get(t, 0) + 1
    for t in sorted(kk):
        s.append('  key type: %-40s %d' % (t, kk[t]))
    bundle_keys = [k['key']['value'] for k in keys if k['key']['value'] and k['key']['value'].endswith('.bundle')]
    hex_keys = [k['key']['value'] for k in keys if k['key']['value'] and
                len(k['key']['value']) == 32 and all(c in '0123456789abcdef' for c in k['key']['value'])]
    s.append('keys ending .bundle: %d' % len(bundle_keys))
    s.append('keys 32-hex (GUID): %d' % len(hex_keys))
    s.append('other keys: %d' % (len(keys) - len(bundle_keys) - len(hex_keys)))
    _w(os.path.join(outdir, 'catalog_summary.txt'), '\n'.join(s) + '\n')

    # ---- keys.tsv
    rows = []
    for k in keys:
        ids = cat.array(k['locset_offset'])
        rows.append((k['index'], k['key']['value'], k['key']['type_asm'], k['key']['type_cls'],
                     len(ids) if ids is not None else '?',
                     ','.join(str(x) for x in (ids or []))))
    _w(os.path.join(outdir, 'catalog_keys.tsv'),
       _tsv(rows, ['index', 'key', 'type_assembly', 'type_class', 'location_count', 'location_ids']))

    # ---- locations.tsv
    rows = []
    for lid in sorted(locs):
        l = locs[lid]
        e = l['extra'] or {}
        deps = l['dependency_set']
        rows.append((
            lid, l['primary_key'], l['internal_id'], l['provider'],
            l['type_asm'], l['type_cls'],
            len(deps) if deps is not None else '?',
            ','.join(str(x) for x in (deps or [])),
            l['dependency_hash'],
            e.get('type_cls'), e.get('hash128'), e.get('bundle_name'),
            e.get('crc'), e.get('bundle_size'),
            e.get('timeout'), e.get('redirect_limit'), e.get('retry_count'), e.get('flags'),
        ))
    _w(os.path.join(outdir, 'catalog_locations.tsv'),
       _tsv(rows, ['location_id', 'primary_key', 'internal_id', 'provider',
                   'resource_type_assembly', 'resource_type_class',
                   'dependency_count', 'dependency_ids', 'dependency_hash',
                   'extra_type', 'bundle_hash128', 'bundle_name', 'crc', 'bundle_size',
                   'timeout', 'redirect_limit', 'retry_count', 'flags']))

    # ---- bundles.tsv（+ 本地对账）
    lb = local_bundle_index(args.bundles)
    cb = cache_index(args.cache)
    rows = []
    for l in sorted(bundles, key=lambda x: x['id']):
        e = l['extra'] or {}
        base = base_name_of(l['internal_id'])
        local_path = lb.get(base) if base else None
        local_size = os.path.getsize(local_path) if local_path else None
        cache_path = cache_size = None
        if e.get('bundle_name') and e.get('hash128'):
            hit = cb.get((e['bundle_name'], e['hash128']))
            if hit:
                cache_path, cache_size = hit
        want = e.get('bundle_size')
        if local_size is not None:
            size_check = 'local=OK' if want == local_size else 'local=DIFF'
        elif cache_size is not None:
            size_check = 'cache=OK' if want == cache_size else 'cache=DIFF'
        else:
            size_check = ''
        rows.append((
            l['primary_key'], base, l['internal_id'], internal_kind(l['internal_id']),
            l['provider'], e.get('hash128'), e.get('bundle_name'), e.get('crc'), e.get('bundle_size'),
            len(l['dependency_set']) if l['dependency_set'] is not None else '?',
            local_path or '', local_size if local_size is not None else '',
            cache_path or '', cache_size if cache_size is not None else '',
            size_check,
        ))
    _w(os.path.join(outdir, 'catalog_bundles.tsv'),
       _tsv(rows, ['bundle_key', 'base_name', 'internal_id', 'internal_id_kind',
                   'provider', 'bundle_hash128', 'bundle_name', 'crc', 'bundle_size',
                   'dependency_count', 'local_path', 'local_size',
                   'cache_path', 'cache_size', 'size_check']))

    # ---- tail
    if args.tail and args.tail > 0:
        refs = build_refs(cat, keys, locs)
        lines = decode_tail(cat, args.tail, refs)
        head = ['# 尾部解码（最后 %d 字节）。' % args.tail,
                '# 说明：这里不是"独立的字符串表"——它是对象图最后写入的一段；',
                '# 字符串是行内的 [u32 len][bytes]，动态串是 [stringId,nextId] 链。',
                '# 每个地址的含义见 W_工具线_A1787_1019b.md §二。',
                '']
        _w(os.path.join(outdir, 'catalog_tail.txt'), '\n'.join(head + lines) + '\n')

    # ---- errors
    if cat.errors:
        _w(os.path.join(outdir, 'catalog_errors.txt'),
           '\n'.join(cat.errors) + '\n')
    elif CHECK:
        # 盘上留着一份「上一版有错」的 catalog_errors.txt，但现在一处错都没有 ⇒ 也算产物过期。
        _stale_err = os.path.join(outdir, 'catalog_errors.txt')
        if os.path.exists(_stale_err):
            _CHECK_BAD.append((_stale_err, os.path.getsize(_stale_err), 0))

    # ---- stdout
    sys.stdout.write('\n'.join(s) + '\n')
    sys.stdout.write('output: %s\n' % outdir)
    if cat.errors:
        sys.stdout.write('errors: %d（见 catalog_errors.txt）\n' % len(cat.errors))
    if CHECK:
        if _CHECK_BAD:
            sys.stdout.write('CHECK: 🔴 %d 个产物与现在会写出来的内容不一致（⛔ 一个字节都没写）\n'
                             % len(_CHECK_BAD))
            for p, got, want in _CHECK_BAD:
                sys.stdout.write('  DIFF %s（盘上 %s B / 现在 %d B）\n'
                                 % (p, '缺产物' if got < 0 else '%d' % got, want))
            return 1
        sys.stdout.write('CHECK: ✅ 产物与现在会写出来的内容逐字节一致（%s）\n' % outdir)
        return 0
    return 0


if __name__ == '__main__':
    sys.exit(main())
