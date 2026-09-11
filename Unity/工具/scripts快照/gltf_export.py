#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gltf_export.py — Unity OBJ/形态键 → glTF 2.0 导出一族
Godot 导入器原生支持 morph targets + weights 动画 + 外部贴图/相机/灯 →
场景级完整导出标准路线 (2026-08-23 二十轮续5 方案A: 轴/朝向/手性在导出阶段处理)

- export_morph_gltf: 单网格 morph targets + weights 动画 (20轮续, chain 甩鞭)
- GltfDoc: 场景级缓冲/访问器构建器 (共享一个 .bin, 外部贴图引用)
- merge_obj_gltf: OBJ 顶点合并(独立 v/vt/vn 索引 → 单索引) + Z 反射手性折入
  (v→(x,y,-z) 法线同向, 三角形绕序翻转; UV 不动 — glTF v=0 在顶部,
   obj_parse 已做 1-v; Godot glTF 导入器不翻 UV)

buffer 用 data URI 内嵌 (免 .bin); UV v 翻转一次 (Godot OBJ 导入器自动 1-v, glTF 导入器不翻,
1-v 后与当前 OBJ 路径渲染一致); morph targets 只给 POSITION 增量 (glTF 合法).
"""
import base64
import json
import os
import struct


def obj_parse(path):
    """OBJ → {pos, uv, nrm, idx} (正索引)"""
    pos, uv, nrm, faces = [], [], [], []
    fi = []
    tfi = []
    for line in open(path, encoding='utf-8', errors='ignore'):
        p = line.split()
        if not p:
            continue
        if p[0] == 'v':
            pos.append((float(p[1]), float(p[2]), float(p[3])))
        elif p[0] == 'vt':
            uv.append((float(p[1]), 1.0 - float(p[2])))  # v 翻转 (Godot OBJ 导入器等效)
        elif p[0] == 'vn':
            nrm.append((float(p[1]), float(p[2]), float(p[3])))
        elif p[0] == 'f':
            for tok in p[1:]:
                parts = tok.split('/')
                fi.append(int(parts[0]) - 1)
                if len(parts) > 1 and parts[1]:
                    tfi.append(int(parts[1]) - 1)
    return pos, uv, nrm, fi, tfi


def _f32(fs):
    return struct.pack('<%df' % len(fs), *fs)


def _u32(ix):
    return struct.pack('<%dI' % len(ix), *ix)


def export_morph_gltf(out_path, obj_path, bs_json, tex_res_path_local='', alpha_blend=True, double_sided=False,
                      anim=None):
    """bs_json: {channels:[...], shapes:[[[idx,dx,dy,dz],...]]};
    anim: {'curves': [{'key': 1-based, 'keys': [[t,v],...]},...], 'length': 15.98, 'name': 'SceneAnim'}
    (clip17 权重曲线 → gltf animation weights 通道, Godot 导入自动 AnimationPlayer)"""
    pos, uv, nrm, fi, tfi = obj_parse(obj_path)
    nv = len(pos)
    chunks = []
    accs = []

    def add_chunk(data, comp_type, accessor_type, count, name, minmax=None):
        off = sum(len(c[0]) for c in chunks)
        chunks.append((data, off))
        a = {'bufferView': len(accs), 'componentType': comp_type,
             'count': count, 'type': accessor_type, 'name': name}
        if minmax:
            a['min'] = minmax[0]
            a['max'] = minmax[1]
        accs.append(a)
        return len(accs) - 1

    bviews = []
    cur = 0
    def add_view(data):
        nonlocal cur
        bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(data)})
        cur += len(data)
        return len(bviews) - 1

    # POSITION
    fdata = _f32([c for v in pos for c in v])
    mn = [min(v[i] for v in pos) for i in range(3)]
    mx = [max(v[i] for v in pos) for i in range(3)]
    bviews.append({'buffer': 0, 'byteOffset': 0, 'byteLength': len(fdata)})
    accs.append({'bufferView': 0, 'componentType': 5126, 'count': nv, 'type': 'VEC3',
                 'min': mn, 'max': mx, 'name': 'position'})
    chunks.append((fdata, 0))
    cur = len(fdata)
    if uv:
        fdata2 = _f32([c for v in uv for c in v])
        bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(fdata2)})
        accs.append({'bufferView': 1, 'componentType': 5126, 'count': len(uv), 'type': 'VEC2', 'name': 'uv'})
        chunks.append((fdata2, cur))
        cur += len(fdata2)
    if nrm:
        fdata3 = _f32([c for v in nrm for c in v])
        bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(fdata3)})
        accs.append({'bufferView': 2, 'componentType': 5126, 'count': len(nrm), 'type': 'VEC3', 'name': 'normal'})
        chunks.append((fdata3, cur))
        cur += len(fdata3)
    i32 = fi
    if max(i32) > 65535:
        idata = _u32(i32)
        cit = 5125
    else:
        idata = struct.pack('<%dH' % len(i32), *i32)
        cit = 5123
    bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(idata)})
    accs.append({'bufferView': 3, 'componentType': cit, 'count': len(i32), 'type': 'SCALAR', 'name': 'indices'})
    chunks.append((idata, cur))
    cur += len(idata)

    # morph targets (POSITION 增量逐顶点)
    targets = []
    weight_views = []
    for shape in bs_json['shapes']:
        deltas = [[0.0, 0.0, 0.0]] * nv
        deltas = [list(d) for d in deltas]
        for e in shape:
            if 0 <= e[0] < nv:
                deltas[e[0]] = [e[1], e[2], e[3]]
        fdd = _f32([c for v in deltas for c in v])
        bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(fdd)})
        accs.append({'bufferView': len(bviews) - 1, 'componentType': 5126, 'count': nv, 'type': 'VEC3'})
        chunks.append((fdd, cur))
        cur += len(fdd)
        targets.append({'POSITION': len(accs) - 1})
    total = cur
    binary = b''.join(c[0] for c in chunks)
    uri = 'data:application/octet-stream;base64,' + base64.b64encode(binary).decode()

    doc = {
        'asset': {'version': '2.0', 'generator': 'warpforge-gltf-export'},
        'buffers': [{'byteLength': total, 'uri': uri}],
        'bufferViews': bviews,
        'accessors': accs,
    }
    mesh = {
        'primitives': [{
            'attributes': {'POSITION': 0},
            'indices': 3,
            'targets': targets,
            'material': 0,
        }],
    }
    if uv:
        mesh['primitives'][0]['attributes']['TEXCOORD_0'] = 1
    if nrm:
        mesh['primitives'][0]['attributes']['NORMAL'] = 2
    doc['meshes'] = [{'primitives': mesh['primitives'], 'name': 'mesh0', 'weights': [0.0] * len(targets)}]
    mat = {
        'name': 'morphmat',
        'pbrMetallicRoughness': {'baseColorFactor': [1.0, 1.0, 1.0, 1.0],
                                 'metallicFactor': 0.0, 'roughnessFactor': 0.9},
    }
    if tex_res_path_local:
        mat['pbrMetallicRoughness']['baseColorTexture'] = {'index': 0}
        doc['textures'] = [{'source': 0}]
        doc['images'] = [{'uri': './' + tex_res_path_local}]
    if alpha_blend:
        mat['alphaMode'] = 'BLEND'
    if double_sided:
        mat['doubleSided'] = True
    doc['materials'] = [mat]
    doc['nodes'] = [{'mesh': 0, 'name': 'mesh_node'}]
    doc['scenes'] = [{'nodes': [0], 'name': 'Scene'}]
    doc['scene'] = 0
    # 动画 (clip17: morph weights)
    if anim and anim.get('curves'):
        n_shape = len(targets)
        length = float(anim.get('length', 15.98))
        n_samples = 61
        times = [length * i / (n_samples - 1) for i in range(n_samples)]

        def ev(keys, t):
            if not keys:
                return 0.0
            if t <= keys[0][0]:
                return keys[0][1]
            if t >= keys[-1][0]:
                return keys[-1][1]
            for i in range(len(keys) - 1):
                t0, v0 = keys[i]
                t1, v1 = keys[i + 1]
                if t0 <= t <= t1:
                    f = 0.0 if t1 == t0 else (t - t0) / (t1 - t0)
                    return v0 + (v1 - v0) * f
            return keys[-1][1]

        weights = []
        for t in times:
            w = [0.0] * n_shape
            for c in anim['curves']:
                k = c['key'] - 1
                if 0 <= k < n_shape:
                    w[k] = ev(c['keys'], t) / 100.0
            weights.append(w)
        tdata = _f32(times)
        bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(tdata)})
        accs.append({'bufferView': len(bviews) - 1, 'componentType': 5126, 'count': n_samples, 'type': 'SCALAR',
                     'min': [0.0], 'max': [length], 'name': 'time'})
        chunks.append((tdata, cur))
        cur += len(tdata)
        in_acc = len(accs) - 1
        wflat = [x for w in weights for x in w]
        wdata = _f32(wflat)
        bviews.append({'buffer': 0, 'byteOffset': cur, 'byteLength': len(wdata)})
        accs.append({'bufferView': len(bviews) - 1, 'componentType': 5126,
                     'count': len(wflat), 'type': 'SCALAR', 'name': 'weights'})
        chunks.append((wdata, cur))
        cur += len(wdata)
        out_acc = len(accs) - 1
        doc['animations'] = [{
            'channels': [{'sampler': 0, 'target': {'node': 0, 'path': 'weights'}}],
            'samplers': [{'input': in_acc, 'interpolation': 'LINEAR', 'output': out_acc}],
            'name': anim.get('name', 'SceneAnim'),
        }]
        doc['buffers'][0]['byteLength'] = cur
        binary = b''.join(c[0] for c in chunks)
        uri = 'data:application/octet-stream;base64,' + base64.b64encode(binary).decode()
        doc['buffers'][0]['uri'] = uri
    open(out_path, 'w', encoding='utf-8').write(json.dumps(doc))
    return out_path


# ================================================================ 场景级 (方案A)
def merge_obj_gltf(obj_path, mirror_z=True):
    """OBJ → glTF 单索引几何 (顶点合并独立 v/vt/vn 索引 + 面三角化);
    mirror_z=True: Z 反射折入数据 (v/n z 取反 + 绕序翻转) — 免运行时负 scale"""
    pos, uv, nrm, fi, tfi = obj_parse(obj_path)
    has_uv = len(uv) > 0
    merged = {}
    out_p, out_u, out_n, out_i = [], [], [], []
    for j, vi in enumerate(fi):
        ti = tfi[j] if len(tfi) > j else vi
        key = (vi, ti)
        new_i = merged.get(key)
        if new_i is None:
            new_i = len(out_p)
            merged[key] = new_i
            v = pos[vi]
            out_p.append((v[0], v[1], -v[2]) if mirror_z else v)
            if has_uv and ti < len(uv):
                out_u.append(uv[ti])
            else:
                out_u.append((0.0, 0.0))
            if nrm and vi < len(nrm):
                n = nrm[vi]
                out_n.append((n[0], n[1], -n[2]) if mirror_z else n)
        out_i.append(new_i)
    # 扇形三角化; z 反射后绕序须反转 → 每三角交换后两顶点
    tris = []
    for k in range(0, len(out_i), 3):
        tris.extend(out_i[k:k + 3])
        if mirror_z and k + 3 <= len(out_i):
            tris[-3], tris[-2] = tris[-2], tris[-3]
    return out_p, out_u, out_n, tris


class GltfDoc:
    """共享单 buffer 的 glTF 文档构建器: add_view (4 字节对齐) → add_accessor → 命名实体"""

    def __init__(self, generator='warpforge-gltf-export'):
        self.doc = {'asset': {'version': '2.0', 'generator': generator},
                    'bufferViews': [], 'accessors': [], 'chunks': []}

    def add_view(self, data, target=None):
        # 4 字节对齐 (glTF 访问器偏移要求)
        off = sum(len(c[0]) for c in self.doc['chunks'])
        if off % 4:
            pad = 4 - off % 4
            self.doc['chunks'].append((b'\x00' * pad, off))
            off += pad
        self.doc['chunks'].append((data, off))
        bv = {'buffer': 0, 'byteOffset': off, 'byteLength': len(data)}
        if target:
            bv['target'] = target
        self.doc['bufferViews'].append(bv)
        return len(self.doc['bufferViews']) - 1

    def add_accessor(self, view_i, comp_type, count, type_, name='', minmax=None):
        a = {'bufferView': view_i, 'componentType': comp_type, 'count': count, 'type': type_}
        if name:
            a['name'] = name
        if minmax:
            a['min'] = minmax[0]
            a['max'] = minmax[1]
        self.doc['accessors'].append(a)
        return len(self.doc['accessors']) - 1

    def add_mesh_primitive(self, geo, mat_index, name='', morph_deltas=None):
        """geo = (pos, uv, nrm, indices) 已合并/镜像; morph_deltas = [每 shape 每顶点增量]"""
        pos, uv, nrm, idx = geo
        nv = len(pos)
        attrs = {}
        mn = [min(v[i] for v in pos) for i in range(3)]
        mx = [max(v[i] for v in pos) for i in range(3)]
        attrs['POSITION'] = self.add_accessor(
            self.add_view(_f32([c for v in pos for c in v]), 34962), 5126, nv, 'VEC3',
            name + '_pos', (mn, mx))
        if uv and nv:
            attrs['TEXCOORD_0'] = self.add_accessor(
                self.add_view(_f32([c for v in uv for c in v]), 34962), 5126, nv, 'VEC2', name + '_uv')
        if nrm and nv:
            attrs['NORMAL'] = self.add_accessor(
                self.add_view(_f32([c for v in nrm for c in v]), 34962), 5126, nv, 'VEC3', name + '_nrm')
        if idx and max(idx) > 65535:
            idata, cit = _u32(idx), 5125
        else:
            idata, cit = struct.pack('<%dH' % len(idx), *idx), 5123
        ia = self.add_accessor(self.add_view(idata, 34963), cit, len(idx), 'SCALAR', name + '_idx')
        prim = {'attributes': attrs, 'indices': ia, 'material': mat_index}
        targets = []
        if morph_deltas:
            for ds in morph_deltas:
                dd = [[0.0, 0.0, 0.0]] * nv
                dd = [list(x) for x in dd]
                for e in ds:
                    if 0 <= int(e[0]) < nv:
                        dd[int(e[0])] = [e[1], e[2], e[3]]
                fdd = _f32([c for v in dd for c in v])
                targets.append({'POSITION': self.add_accessor(
                    self.add_view(fdd, 34962), 5126, nv, 'VEC3', name + '_morph')})
            prim['targets'] = targets
        return prim, targets

    def add_mesh(self, prims, name='', weights=None):
        m = {'primitives': prims, 'name': name}
        if weights is not None:
            m['weights'] = weights
        self.doc.setdefault('meshes', []).append(m)
        return len(self.doc['meshes']) - 1

    def add_node(self, name='', mesh=None, cam=None, light=None, children=None, trs=None, weights=None):
        n = {'name': name}
        if mesh is not None:
            n['mesh'] = mesh
        if cam is not None:
            n['camera'] = cam
        if light is not None:
            n['extensions'] = {'KHR_lights_punctual': {'light': light}}
        if children:
            n['children'] = children
        if trs:
            t, q, s = trs
            if t != (0.0, 0.0, 0.0):
                n['translation'] = list(t)
            if q != (0.0, 0.0, 0.0, 1.0):
                n['rotation'] = list(q)
            if s != (1.0, 1.0, 1.0):
                n['scale'] = list(s)
        if weights is not None:
            n['weights'] = weights
        self.doc.setdefault('nodes', []).append(n)
        return len(self.doc['nodes']) - 1

    def add_material(self, name, mat=None):
        mat = mat or {'pbrMetallicRoughness': {'metallicFactor': 0.0, 'roughnessFactor': 0.9}}
        mat.setdefault('name', name)
        self.doc.setdefault('materials', []).append(mat)
        return len(self.doc['materials']) - 1

    def add_animation(self, name, channels, samplers):
        self.doc.setdefault('animations', []).append(
            {'channels': channels, 'samplers': samplers, 'name': name})

    def write(self, path, bin_path=None):
        """写入 .gltf (+.bin 外置; 不传 bin_path = data URI 内嵌)"""
        binary = b''.join(c[0] for c in self.doc['chunks'])
        del self.doc['chunks']
        if bin_path:
            with open(bin_path, 'wb') as f:
                f.write(binary)
            self.doc['buffers'] = [{'byteLength': len(binary),
                                    'uri': './' + os.path.basename(bin_path)}]
        else:
            uri = 'data:application/octet-stream;base64,' + base64.b64encode(binary).decode()
            self.doc['buffers'] = [{'byteLength': len(binary), 'uri': uri}]
        nodes = self.doc.get('nodes', [])
        self.doc.setdefault('scene', 0)
        self.doc.setdefault('scenes', [{'nodes': self.doc.get('root_nodes',
                                                           [0] if nodes else []), 'name': 'Scene'}])
        self.doc.pop('root_nodes', None)
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(self.doc, f)
        return path
