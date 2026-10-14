# -*- coding: utf-8 -*-
"""A650 · 第二跳：摸 resources.assets 的 SerializedFile 元数据（externals / type tree / 原始字节头）。
只读。用法：python -I -X utf8 d:/4/Unity/工具/_a650_probe2.py
"""
import io, os, struct, collections

SRC = r'd:\2\unity_run_ref\Warpforge_Data\resources.assets'
OUT = r'd:\4\Unity\资料\普查产出_第三十会话\A650_resources_probe2.txt'


def main():
    import UnityPy
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, 'w', encoding='utf-8') as fh:
        def log(*a):
            s = ' '.join(str(x) for x in a); print(s); fh.write(s + '\n')

        env = UnityPy.load(SRC)
        files = getattr(env, 'files', None)
        log('env.files 类型', type(files), ' 条数', len(files) if files else 0)
        sf = None
        if isinstance(files, dict):
            for k, v in files.items():
                log('  file:', k, type(v).__name__)
                if sf is None:
                    sf = v
        if sf is None:
            log('!! 拿不到 SerializedFile'); return
        log('enable_type_tree =', getattr(sf, 'enable_type_tree', 'N/A'))
        log('unity_version    =', getattr(sf, 'unity_version', 'N/A'))
        ex = getattr(sf, 'externals', None) or []
        log('externals 条数    =', len(ex))
        for i, e in enumerate(ex):
            log('   [%d] %s' % (i, getattr(e, 'path', e)))

        objs = [o for o in env.objects if o.type.name == 'MonoBehaviour']
        o0 = objs[0]
        log('')
        log('第一个 MB: path_id=%s byte_start=%s byte_size=%s' % (
            getattr(o0, 'path_id', '?'), getattr(o0, 'byte_start', '?'), getattr(o0, 'byte_size', '?')))
        for attr in ('get_raw_data', 'get_raw_data_io', 'read', 'read_typetree'):
            log('   有 %s ? %s' % (attr, hasattr(o0, attr)))
        raw = None
        try:
            raw = o0.get_raw_data()
        except Exception as e:
            log('   get_raw_data 失败:', type(e).__name__, str(e)[:120])
        if raw:
            log('   原始字节 %d 个，前 48 字节 hex:' % len(raw), raw[:48].hex())
            # 按标准 MB 头解：fileID(4) pathID(8) enabled(1+3pad) script.fileID(4) script.pathID(8) nameLen(4) name
            go_f, go_p = struct.unpack_from('<iq', raw, 0)
            en = raw[12]
            sc_f, sc_p = struct.unpack_from('<iq', raw, 16)
            nlen = struct.unpack_from('<i', raw, 28)[0]
            nm = raw[32:32 + max(0, min(nlen, 200))]
            log('   m_GameObject fileID=%d pathID=%d' % (go_f, go_p))
            log('   m_Enabled=%d' % en)
            log('   m_Script fileID=%d pathID=%d' % (sc_f, sc_p))
            log('   m_Name len=%d = %r' % (nlen, nm))
        # 统计全部 MB 的 m_Script pathID（原始字节）
        cnt = collections.Counter(); names = collections.Counter()
        for o in objs:
            try:
                r = o.get_raw_data()
                sc_f, sc_p = struct.unpack_from('<iq', r, 16)
                cnt[(sc_f, sc_p)] += 1
                nlen = struct.unpack_from('<i', r, 28)[0]
                if 0 < nlen < 200:
                    names[r[32:32 + nlen].decode('utf-8', 'replace')] += 1
            except Exception:
                cnt[('ERR', 0)] += 1
        log('')
        log('616 个 MB 的 m_Script 分布（前 30）:')
        for k, n in cnt.most_common(30):
            log('   fileID=%-4s pathID=%-22s x%d' % (k[0], k[1], n))
        log('')
        log('616 个 MB 的 m_Name（前 60，去重）:')
        for k, n in names.most_common(60):
            log('   %-52s x%d' % (k, n))


if __name__ == '__main__':
    main()
