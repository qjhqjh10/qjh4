# -*- coding: utf-8 -*-
"""A650 · 重解包 resources.assets 的「616 个失败对象」（无 type tree ⇒ 按 m_Script pathID 扫 + 原始字节手工解）。
只读：⛔ 不改 d:/2 里任何文件；产物写到 d:/4 的普查产出目录。
用法：python -I -X utf8 d:/4/Unity/工具/_a650_unpack_resources.py
"""
import io
import os
import sys
import json
import collections

SRC = r'd:\2\unity_run_ref\Warpforge_Data\resources.assets'
OUT = r'd:\4\Unity\资料\普查产出_第三十会话\A650_resources_probe.txt'


def log(fh, *a):
    s = ' '.join(str(x) for x in a)
    print(s)
    fh.write(s + '\n')


def main():
    import UnityPy
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, 'w', encoding='utf-8') as fh:
        log(fh, '== 文件 ==', SRC, os.path.getsize(SRC), 'B')
        env = UnityPy.load(SRC)
        objs = list(env.objects)
        log(fh, '对象总数 =', len(objs))
        by_type = collections.Counter(o.type.name for o in objs)
        log(fh, '按类型（前 20）:', by_type.most_common(20))

        mbs = [o for o in objs if o.type.name == 'MonoBehaviour']
        log(fh, 'MonoBehaviour 数 =', len(mbs))
        ok = fail = 0
        script_hits = collections.Counter()
        for o in mbs:
            try:
                tt = o.read_typetree()
                ok += 1
            except Exception:
                fail += 1
                tt = None
            # 不论成败都拿 m_Script（Read 一般能拿到头部字段）
            try:
                d = o.read()
                sp = d.get('m_Script') if isinstance(d, dict) else None
                pid = getattr(sp, 'm_PathID', None) if sp is not None else None
                script_hits[pid] += 1
            except Exception:
                script_hits[None] += 1
        log(fh, '')
        log(fh, 'read_typetree 成功 =', ok, ' 失败 =', fail)
        log(fh, 'm_Script pathID 分布（前 25）:')
        for pid, n in script_hits.most_common(25):
            log(fh, '   pathID=%-22s x%d' % (pid, n))

        # 把 pathID 解成 MonoScript 的类名
        ms = {}
        for o in objs:
            if o.type.name == 'MonoScript':
                try:
                    d = o.read()
                    ms[o.path_id] = (d.get('m_ClassName'), d.get('m_Namespace'), d.get('m_AssemblyName'))
                except Exception:
                    pass
        log(fh, '')
        log(fh, '包内 MonoScript 数 =', len(ms))
        names = collections.Counter()
        for pid, n in script_hits.items():
            t = ms.get(pid)
            names[(t[0] if t else str(pid))] += n
        log(fh, '按类名归并（前 40）:')
        for nm, n in names.most_common(40):
            log(fh, '   %-46s x%d' % (nm, n))
        log(fh, '')
        log(fh, '★ 名字里带 Enemy/Info/Touch 的：')
        for nm, n in names.items():
            if nm and any(k in nm for k in ('Enemy', 'Info', 'Touch')):
                log(fh, '   %-46s x%d' % (nm, n))
        log(fh, '')
        log(fh, '== 包内所有 MonoScript 类名（前 80）==')
        for pid, t in list(ms.items())[:80]:
            log(fh, '   %-44s %s' % (t[0], t[1]))


if __name__ == '__main__':
    main()
