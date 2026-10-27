# -*- coding: utf-8 -*-
"""dxbc_rdef.py —— 从原版 shader 的 DXBC 里解析 **RDEF 的常量缓冲表**，给出 `cbN[寄存器] → 属性名` 映射。

**为什么需要它**（第十二批）：`disasm_dxbc.py` 反汇编出来的指令里只有 `cb0[131].xxyx` 这种**数字偏移**；
`SerializedPass.m_CommonParameters` 在本工程的 `assets_full` 导出里是**空的**（逐 pass 核过）⇒ 走不通。
**RDEF 里有名字**（`dump_shader_blob.py` 打的就是它），只是那个工具没解析 `Variables[].startOffset`。
本脚本补这一步，并可把 asm 里引用了某个属性所在寄存器槽的行挑出来。

用法（必须 UTF-8）：
    PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/dxbc_rdef.py "<shader 名片段>"
    ... --asm d:/4/Unity/_tmp_view/dxbc/ui_border_ps.asm     # 顺便把 asm 里引用这些寄存器的行统计出来
    ... --only _Reveal,_Smoothness,_Noise_R,_Scale_X_Speed_Y  # 只看这几个名字

⚠️ 已知边界：
  · 只解析 **RDEF**；`SHDR` 的指令语义仍要人读（本脚本只做「偏移 → 名字」这一步）。
  · RDEF 里的 **cb 序号 = 该表里的出现顺序**（`$Globals` 通常第一个 ⇒ `cb0`）；asm 里 `cbN[x]` 的 `x` 是 **float4 下标**，
    本脚本按 `startOffset / 16` 换算。
"""
import io
import os
import re
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


def parse_rdef(data):
    """在 decompressed 字节流里找所有 RDEF 段，返回 [(cb_index, cb_name, cb_size, [(var, byteOff, size)])]。"""
    out = []
    for m in re.finditer(b'RDEF', data):
        base = m.start()
        size = struct.unpack_from('<I', data, base + 4)[0]
        d = base + 8                       # RDEF 数据起点（名字偏移相对这里）
        cb_count = struct.unpack_from('<I', data, d)[0]
        if cb_count == 0 or cb_count > 64:
            continue
        cbs = []
        p = d + 4
        for _ in range(cb_count):
            name_off, var_count, _pad, var_off, cb_size, _flags, _type = \
                struct.unpack_from('<IHHIIII', data, p)
            p += 24
            cbs.append((name_off, var_count, var_off, cb_size))
        for idx, (name_off, var_count, var_off, cb_size) in enumerate(cbs):
            cb_name = _cstr(data, d + name_off)
            vars_ = []
            q = d + var_off
            for _ in range(var_count):
                v_name_off, v_start, v_size, _vf, _vt, _vd = struct.unpack_from('<IIIIII', data, q)
                q += 24
                vars_.append((_cstr(data, d + v_name_off), v_start, v_size))
            out.append((idx, cb_name, cb_size, vars_))
    return out


def _cstr(data, off):
    end = data.find(b'\x00', off)
    if end < 0: end = off + 64
    return data[off:end].decode('utf-8', 'replace')


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return
    frag = args[0]
    asm_path = None
    only = None
    for i, a in enumerate(args):
        if a == '--asm' and i + 1 < len(args): asm_path = args[i + 1]
        if a == '--only' and i + 1 < len(args): only = {x.strip() for x in args[i + 1].split(',') if x.strip()}

    import UnityPy
    import dump_shader_blob as D      # 复用它的解压（blob_of）与包清单

    bundles = list(getattr(D, 'BUNDLES', []) or []) + list(getattr(D, 'BUILTIN_FILES', []) or [])
    if not bundles:
        print('🔴 拿不到 dump_shader_blob 的包清单（BUNDLES/BUILTIN_FILES）')
        return
    hits = 0
    for path in bundles:
        if not os.path.exists(path):
            continue
        env = UnityPy.load(path)
        for obj in env.objects:
            if obj.type.name != 'Shader':
                continue
            try:
                sh = obj.read()
            except Exception:
                continue
            pf = getattr(sh, 'm_ParsedForm', None)
            name = ((getattr(pf, 'm_Name', '') if pf else '') or '') or (getattr(sh, 'm_Name', '') or '')
            if frag.lower() not in (name or '').lower():
                continue
            hits += 1
            data = D.blob_of(sh)
            print('=' * 20, name, '（%s）' % os.path.basename(path), '字节', len(data))
            n_dxbc = data.count(b'DXBC'); n_rdef = data.count(b'RDEF'); n_shdr = data.count(b'SHDR')
            print('  DXBC 容器 %d 个 · SHDR %d · RDEF %d' % (n_dxbc, n_shdr, n_rdef))
            if n_rdef == 0:
                print('  🔴 **RDEF 被剥**（本 shader 只有 SHDR/ISGN/OSGN）⇒ 字节码里拿不到 `cbN[reg] → 名字`；'
                      '这条必须换路（见报告第十二批：找同族**带 RDEF** 的兄弟 shader 借偏移表 / 或差分实验）。')
            for idx, cb_name, cb_size, vars_ in parse_rdef(data):
                if cb_name not in ('$Globals',) and idx != 0:
                    # 只粗看第一个/全局表；其余表也列出名字，便于核对
                    if only is None:
                        print('  cb%d %-14s size=%-4d vars=%d' % (idx, cb_name, cb_size, len(vars_)))
                    continue
                print('  cb%d %-14s size=%-4d vars=%d' % (idx, cb_name, cb_size, len(vars_)))
                vm = {}
                for vname, vstart, vsize in vars_:
                    vm[vstart // 16] = vname
                    if only is None or vname in only:
                        print('      reg[%3d] (byte %4d, size %3d) %s' % (vstart // 16, vstart, vsize, vname))
                if asm_path and os.path.exists(asm_path):
                    txt = io.open(asm_path, encoding='utf-8', errors='replace').read()
                    print('    --- asm 引用统计（%s）' % os.path.basename(asm_path))
                    for reg, vname in sorted(vm.items()):
                        if only is not None and vname not in only:
                            continue
                        n = len(re.findall(r'\bcb%d\[%d\]' % (idx, reg), txt))
                        if n:
                            print('      %-46s reg[%3d] 被引用 %d 次' % (vname, reg, n))
    if hits == 0:
        print('🔴 没找到匹配的 shader（片段：%s）' % frag)


if __name__ == '__main__':
    main()
