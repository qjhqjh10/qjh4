#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Generic Unity type-tree reader (pure python).

UnityPy 1.24.2/1.25.3 fail on a few generated layouts, notably
`List<string>` (a `string` node whose Array child's `data` is itself a
`string`), which makes them read a single string and then run off the buffer
("read_str out of bounds"). This reader implements the layout rules directly:

  * a node with children is a struct unless its type is Array/TypedArray/vector
  * `Array` node = int32 size + N x <data child>
  * `string` node without children = aligned string (int32 len + bytes)
  * `string` node WITH an Array child = that array (char data => string,
    string data => list of strings)
  * after reading a node with m_MetaFlag & 0x4000 (kAlignBytes) -> align to 4

Usage (import):  from ttread import Reader
                 r = Reader(raw_bytes, endian='<')
                 val = r.read_root(nodes)     # nodes = flat [{m_Level,m_Type,m_Name,m_MetaFlag}]
"""
import struct

ALIGN_FLAG = 0x4000

INT_TYPES = {
    'bool': ('<?', 1), 'SInt8': ('<b', 1), 'UInt8': ('<B', 1), 'char': ('<B', 1),
    'SInt16': ('<h', 2), 'UInt16': ('<H', 2), 'short': ('<h', 2),
    'unsigned short': ('<H', 2),
    'SInt32': ('<i', 4), 'int': ('<i', 4), 'unsigned int': ('<I', 4),
    'UInt32': ('<I', 4), 'Type*': ('<i', 4),
    'SInt64': ('<q', 8), 'long long': ('<q', 8), 'UInt64': ('<Q', 8),
    'FileSize': ('<q', 8), 'unsigned long long': ('<Q', 8),
    'float': ('<f', 4), 'double': ('<d', 8),
}
ARRAY_TYPES = ('Array', 'TypedArray', 'staticvector', 'vector', 'map')


def build_tree(nodes):
    """flat level list -> nested dict tree (root first)."""
    def mk(i):
        nd = nodes[i]
        i += 1
        ch = []
        while i < len(nodes) and nodes[i]['m_Level'] > nd['m_Level']:
            c, i = mk(i)
            ch.append(c)
        return {'t': nd['m_Type'], 'n': nd['m_Name'],
                'f': int(nd.get('m_MetaFlag') or 0), 'c': ch}, i
    tree, _ = mk(0)
    return tree


class Reader:
    def __init__(self, data, endian='<'):
        self.d = data
        self.p = 0
        self.e = endian

    # -- primitives ---------------------------------------------------
    def _need(self, n):
        if self.p + n > len(self.d):
            raise EOFError(f"out of bounds at {self.p} (+{n} > {len(self.d)})")

    def align(self):
        if self.p % 4:
            self.p = (self.p + 3) & ~3

    def i32(self):
        self._need(4)
        v = struct.unpack_from(self.e + 'i', self.d, self.p)[0]
        self.p += 4
        return v

    def astring(self):
        n = self.i32()
        if n < 0 or n > (1 << 24):
            raise EOFError(f"bad string len {n} at {self.p - 4}")
        self._need(n)
        s = self.d[self.p:self.p + n].decode('utf-8', 'replace')
        self.p += n
        self.align()
        return s

    # -- tree walk ----------------------------------------------------
    def read_root(self, nodes):
        return self.read(build_tree(nodes))

    def read(self, node):
        t, ch = node['t'], node['c']
        # --- string -------------------------------------------------
        if t == 'string':
            if not ch:
                return self.astring()
            arr = ch[0]
            if arr['t'] != 'Array':
                raise ValueError(f"string node {node['n']} with non-Array child {arr['t']}")
            dchild = arr['c'][1] if len(arr['c']) > 1 else arr['c'][0]
            if dchild['t'] in ('char', 'UInt8'):
                return self.astring()
            # List<string> / List<T> written as a string node
            size = self.i32()
            if size < 0 or size > 200_000_000:
                raise EOFError(f"bad string-array size {size} at {self.p - 4}")
            vals = [self.read(dchild) for _ in range(size)]
            if (node['f'] | arr['f']) & ALIGN_FLAG:
                self.align()
            return vals
        # --- array (Unity's rule: first child type == "Array") -------
        if ch and ch[0]['t'] == 'Array':
            arr = ch[0]
            size = self.i32()
            if size < 0 or size > 200_000_000:
                raise EOFError(f"bad array size {size} at {self.p - 4}")
            dchild = arr['c'][1] if len(arr['c']) > 1 else arr['c'][0]
            vals = [self.read(dchild) for _ in range(size)]
            if (node['f'] | arr['f']) & ALIGN_FLAG:
                self.align()
            return vals
        if t == 'pair' and len(ch) == 2:
            return (self.read(ch[0]), self.read(ch[1]))
        # --- primitive ----------------------------------------------
        if t in INT_TYPES and not ch:
            fmt, n = INT_TYPES[t]
            self._need(n)
            v = struct.unpack_from(self.e + fmt[1], self.d, self.p)[0]
            self.p += n
            if node['f'] & ALIGN_FLAG:
                self.align()
            return v
        # --- class / struct / PPtr ----------------------------------
        out = {}
        for c in ch:
            out[c['n']] = self.read(c)
        if node['f'] & ALIGN_FLAG:
            self.align()
        return out


def node_without_f(node):
    return {'t': node['t'], 'n': node['n'], 'f': 0, 'c': node['c']}
