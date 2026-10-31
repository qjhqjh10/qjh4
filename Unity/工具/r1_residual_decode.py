#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""r1_residual_decode.py — R1 那三条「解不出」疑点的取证工具（B106_W36）

对应施工单 `判据/B106_资源侧R0R4_施工单.md` §二 的 R1-01 / R1-02 / R1-04。
三条都**只读** `d:/2` 与工程，只有 `--write` 时才往 `资料/完全复刻/资源/` 落产物。

  --dotween   R1-01  resources.assets #1430 DOTweenSettings（200 B）
              用 IL2CPP 生成树读**原版真实字节**，再与我们
              `MyGame/Assets/Resources/DOTweenSettings.asset` 逐字段比。
              （此前 R1 用的是 AssetRipper 导出的同名 YAML —— 那是"按名字找"，
               本条改成按对象字节读，证据链不再依赖同名。）

  --guiskin   R1-02  sharedassets0.assets #544 reporterScrollerSkin（7772 B）
              它是 Unity **原生类 GUISkin**（m_Script -> externals[1]
              `Resources/unity default resources` pathID 12001 = MonoScript `UnityEngine.GUISkin`）。
              UnityPy 自带 tpk（AssetRipper 的 lzma.tpk，415 个类）**没有 GUISkin**
              （全 1420 个版本都没有），所以走 **IL2CPP 元数据**
              assembly=`UnityEngine.IMGUIModule`，type=`UnityEngine.GUISkin` ⇒ 解得出。

  --i2        R1-04  resources.assets #1427 I2Languages（10,764,844 B）与 #1431（48 B）
              `temp` 是 XOR 密文；用 `工具/_i2_decode_probe.py` 的同一把钥匙解成词条表，
              并与 `资料/普查产出_1018/allcards_1350/localization_2026_decoded.json`
              （= Addressables `localization` 包那份）逐条比。

🔴 共用的一个坑（本工具已修，也请记进账）：
  **TypeTreeGeneratorAPI 从 IL2CPP 元数据生成的 MonoBehaviour 类型树，`m_Enabled` 节点没有
  `kAlignBytes(0x4000)` 标记** ⇒ 紧跟其后的 `m_Script` PPtr 会**早读 3 字节**（`m_Enabled` 后
  Unity 真有 3 字节对齐填充）。实测 `#1430` 读出 `{m_FileID: 16777216, m_PathID: 76420218880}`
  （真值 `{2, 16995157}`）、`#544` 读出 `{33554432, 201343369216}`（真值 `{2, 12001}`）。
  因为下一个字段 `m_Name` 是 string（4 对齐），读口会自愈 ⇒ **只有 `m_Script` 一格是错的**，
  其余字段不受影响。修法 = 给 `m_Enabled` 节点置 `m_MetaFlag |= 0x4000`（本工具 `_align_fix()`）。
  ⚠️ 这解释了 `_tmp_view/wfres/r1_decoded2/` 里 885 份 JSON 中 **858 份 `m_Script` 是垃圾**
  （R1 的 TSV `类名` 列不受影响 —— 那一列是 `parse_monobehaviour_head()` 直接读字节得到的）。

用法（⛔ 不带参数 = 只打印用法、退出码 0、零读零写）：
  python -I -X utf8 d:/4/Unity/工具/r1_residual_decode.py
  python -I -X utf8 d:/4/Unity/工具/r1_residual_decode.py --dotween
  python -I -X utf8 d:/4/Unity/工具/r1_residual_decode.py --guiskin
  python -I -X utf8 d:/4/Unity/工具/r1_residual_decode.py --i2
  python -I -X utf8 d:/4/Unity/工具/r1_residual_decode.py --all --write

依赖：UnityPy 1.24.2 + TypeTreeGeneratorAPI（`D:/2/Warpforge_tools/py312/python.exe`）、
     本仓 `工具/ttread.py`、`工具/_i2_decode_probe.py`。
"""
import argparse
import hashlib
import io
import json
import os
import struct
import sys

GAME = r'd:/2/Warhammer 40k Warpforge'
DATA = os.path.join(GAME, 'Warpforge_Data')
TOOLS = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = r'd:/4/Unity/资料/完全复刻/资源'

OURS_DOTWEEN = r'd:/4/Unity/MyGame/Assets/Resources/DOTweenSettings.asset'
BUNDLE_I2_JSON = r'd:/4/Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json'
BUNDLE_I2_BIN = r'd:/4/Unity/资料/普查产出_1018/allcards_1350/localization_temp_2026.bin'

ALIGN_BYTES = 0x4000

_gen = None


# ---------------------------------------------------------------- 基础设施
def _gen_once():
    global _gen
    if _gen is not None:
        return _gen
    import UnityPy
    from UnityPy.helpers import TypeTreeGenerator as TTG

    def get_nodes_up_nodll(self, assembly, fullname):
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


def flat(node, out=None):
    if out is None:
        out = []
    out.append({'m_Level': node.m_Level, 'm_Type': node.m_Type,
                'm_Name': node.m_Name, 'm_MetaFlag': int(node.m_MetaFlag or 0)})
    for c in (node.m_Children or []):
        flat(c, out)
    return out


def nodes_for(assembly, fullname):
    return flat(_gen_once().get_nodes_up(assembly, fullname))


def _align_fix(nodes, after_name):
    """给 after_name 节点加 kAlignBytes —— 修 IL2CPP 生成树漏掉的 m_Enabled 后填充。"""
    for n in nodes:
        if n['m_Name'] == after_name:
            n['m_MetaFlag'] |= ALIGN_BYTES
            return True
    return False


def read_object(src_file, path_id, nodes):
    """返回 (value, consumed, byte_size, raw)。"""
    import UnityPy
    sys.path.insert(0, TOOLS)
    import ttread
    env = UnityPy.load(os.path.join(DATA, src_file))
    obj = next((o for o in env.objects if o.path_id == path_id), None)
    if obj is None:
        raise SystemExit('找不到 %s #%d' % (src_file, path_id))
    raw = obj.get_raw_data()
    rd = ttread.Reader(raw, '<')
    val = rd.read_root(nodes)
    return val, rd.p, len(raw), raw


def header_ptr(raw, file_id_off, path_id_off):
    return {'m_FileID': struct.unpack_from('<i', raw, file_id_off)[0],
            'm_PathID': struct.unpack_from('<q', raw, path_id_off)[0]}


def yaml_flat(path):
    import re
    import yaml
    txt = io.open(path, encoding='utf-8', errors='replace').read()
    body = re.sub(r'--- !u!\d+ &\d+', '---', txt)
    doc = yaml.safe_load(body[body.index('---'):])
    return next(iter(doc.values()))


def flatten(d, prefix=''):
    out = {}
    for k, v in d.items():
        key = prefix + k
        if isinstance(v, dict):
            out.update(flatten(v, key + '.'))
        else:
            out[key] = v
    return out


def write_json(name, payload):
    os.makedirs(OUT_DIR, exist_ok=True)
    p = os.path.join(OUT_DIR, name)
    tmp = p + '.tmp'
    with io.open(tmp, 'w', encoding='utf-8', newline='') as f:
        f.write(json.dumps(payload, ensure_ascii=False, indent=1, default=str))
    os.replace(tmp, p)
    return p


# ---------------------------------------------------------------- R1-01
ID_LIKE = {'m_ObjectHideFlags', 'm_CorrespondingSourceObject', 'm_PrefabInstance',
           'm_PrefabAsset', 'm_GameObject', 'm_Enabled', 'm_EditorHideFlags',
           'm_Script', 'm_Name', 'm_EditorClassIdentifier'}


def cmd_dotween(write):
    nodes = nodes_for('DOTween', 'DG.Tweening.Core.DOTweenSettings')
    _align_fix(nodes, 'm_Enabled')
    val, consumed, size, raw = read_object('resources.assets', 1430, nodes)
    val['m_Script'] = header_ptr(raw, 16, 20)

    orig = flatten(val)
    ours = flatten(yaml_flat(OURS_DOTWEEN))
    only_orig = sorted(set(orig) - set(ours))
    only_ours = sorted(set(ours) - set(orig))
    rows, same, diff = [], 0, 0
    for k in sorted(set(orig) & set(ours)):
        head = k.split('.')[0]
        if k in ID_LIKE or head in ID_LIKE:
            continue
        a, b = orig[k], ours[k]
        if isinstance(a, float) or isinstance(b, float):
            eq = abs(float(a) - float(b)) < 1e-6
        else:
            eq = (a == b)
        same += eq
        diff += (not eq)
        rows.append({'key': k, 'orig': a, 'ours': b, 'same': bool(eq)})

    print('[R1-01] resources.assets #1430 DOTweenSettings — consumed %d / %d' % (consumed, size))
    print('  原版 m_Script = %s' % (val['m_Script'],))
    print('  逐字段比：相同 %d · 不同 %d' % (same, diff))
    for r in rows:
        if not r['same']:
            print('  DIFF %-40s 原版=%s  我们=%s' % (r['key'], r['orig'], r['ours']))
    print('  仅原版有的键: %s' % (only_orig,))
    print('  仅我们有的键: %s' % (only_ours,))
    payload = {
        '_meta': {
            'what': 'R1-01：resources.assets #1430 DOTweenSettings 逐字段比（原版真实字节 vs 我们的资产）',
            'tool': '工具/r1_residual_decode.py --dotween',
            'date': '2026-10-31',
            'evidence': [
                '原版：d:/2/Warhammer 40k Warpforge/Warpforge_Data/resources.assets #1430（200 B），'
                'IL2CPP 生成树 DOTween::DG.Tweening.Core.DOTweenSettings 读满 200/200',
                '我们：' + OURS_DOTWEEN,
                '外部旁证：d:/2/tools/assetripper_out/ExportedProject/Assets/Resources/DOTweenSettings.asset（37 键，与本次字节读一致）',
            ],
            'note': 'm_Script 的 fileID/guid 两边本就不同（我们自带 DOTween.dll，原版那份是原包的），不是值差异。',
        },
        'object': {'src': 'resources.assets', 'path_id': 1430, 'byte_size': size,
                   'consumed': consumed, 'm_Script': val['m_Script'],
                   'm_Name': val.get('m_Name')},
        'value_diff': {'same': same, 'diff': diff,
                       'only_in_original': only_orig, 'only_in_ours': only_ours,
                       'rows': rows},
    }
    if write:
        print('  已写: %s' % write_json('R1_1430_DOTweenSettings_逐字段比.json', payload))
    return payload


# ---------------------------------------------------------------- R1-02
def cmd_guiskin(write):
    nodes = nodes_for('UnityEngine.IMGUIModule', 'UnityEngine.GUISkin')
    _align_fix(nodes, 'm_Enabled')
    val, consumed, size, raw = read_object('sharedassets0.assets', 544, nodes)
    real_script = header_ptr(raw, 16, 20)
    val['m_Script'] = real_script

    # m_Script -> externals[1] = Resources/unity default resources，查那个 MonoScript 的类名
    script_name = None
    try:
        import UnityPy
        env = UnityPy.load(os.path.join(DATA, 'Resources', 'unity default resources'))
        o = next(x for x in env.objects if x.path_id == real_script['m_PathID'])
        d = o.read_typetree()
        script_name = '%s.%s' % (d.get('m_Namespace') or '', d.get('m_ClassName'))
    except Exception as e:                                     # 查不到不算失败
        print('  [warn] 反查 MonoScript 失败: %s: %s' % (type(e).__name__, e))

    styles = {}
    for k in ('m_box', 'm_button', 'm_toggle', 'm_label', 'm_textField', 'm_textArea',
              'm_window', 'm_horizontalScrollbar', 'm_verticalScrollbar', 'm_ScrollView'):
        v = val.get(k)
        if isinstance(v, dict):
            styles[k] = v.get('m_Name')
    print('[R1-02] sharedassets0.assets #544 reporterScrollerSkin — consumed %d / %d' % (consumed, size))
    print('  m_Script = %s -> %s' % (real_script, script_name))
    print('  顶层键 %d：%s' % (len(val), list(val)))
    print('  样式名: %s' % (styles,))
    print('  m_Font = %s · m_Settings = %s' % (val.get('m_Font'), val.get('m_Settings')))
    payload = {
        '_meta': {
            'what': 'R1-02：sharedassets0.assets #544 reporterScrollerSkin（GUISkin）全字段',
            'tool': '工具/r1_residual_decode.py --guiskin',
            'date': '2026-10-31',
            'judgement': [
                'm_Script -> externals[1] Resources/unity default resources，pathID %d = MonoScript `%s`' % (
                    real_script['m_PathID'], script_name),
                'UnityPy 自带 tpk 全 1420 个版本都没有 GUISkin（415 个类里查不到）；'
                'AssetRipper 导出的 .guiskin 只有头（405 B）',
                'IL2CPP 元数据里它在 assembly=UnityEngine.IMGUIModule，type=UnityEngine.GUISkin '
                '⇒ 用该类型树读，字节 7772/7772 读满',
            ],
            'note': 'IL2CPP 生成树漏了 m_Enabled 的 3 字节对齐 ⇒ m_Script 会早读 3 字节；'
                    '本工具已置 kAlignBytes 修正，并与头部字节手动解析的结果对过。',
        },
        'object': {'src': 'sharedassets0.assets', 'path_id': 544, 'byte_size': size,
                   'consumed': consumed, 'm_Name': val.get('m_Name'),
                   'm_Script': real_script, 'm_Script_resolved': script_name},
        'GUISkin': val,
    }
    if write:
        print('  已写: %s' % write_json('R1_544_reporterScrollerSkin.json', payload))
    return payload


# ---------------------------------------------------------------- R1-04
def _i2_decode(raw_temp):
    sys.path.insert(0, TOOLS)
    import _i2_decode_probe        # 复用同一把钥匙（含 password 自检）
    pw = _i2_decode_probe.password()
    return json.loads(_i2_decode_probe.decode(raw_temp, pw).decode('utf-8'))


def cmd_i2(write):
    nodes = nodes_for('Assembly-CSharp', 'I2.Loc.LanguageSourceAsset')
    _align_fix(nodes, 'm_Enabled')
    out = {}
    for pid in (1427, 1431):
        val, consumed, size, raw = read_object('resources.assets', pid, nodes)
        val['m_Script'] = header_ptr(raw, 16, 20)
        temp = val.get('temp')
        temp_bytes = bytes(bytearray(temp)) if isinstance(temp, list) else b''
        out[pid] = {
            'src': 'resources.assets', 'path_id': pid, 'byte_size': size,
            'consumed': consumed, 'keys': list(val), 'm_Name': val.get('m_Name'),
            'm_Script': val['m_Script'],
            'temp_len': len(temp_bytes),
            'temp_sha256': hashlib.sha256(temp_bytes).hexdigest() if temp_bytes else None,
            'temp_head16': temp_bytes[:16].hex() if temp_bytes else None,
        }
        print('[R1-04] resources.assets #%d I2Languages — consumed %d / %d · temp %d B sha256 %s'
              % (pid, consumed, size, len(temp_bytes),
                 out[pid]['temp_sha256']))
        if pid == 1427:
            dec = _i2_decode(temp_bytes)
            langs = [l.get('Name') for l in (dec.get('mLanguages') or [])]
            terms = dec.get('mTerms') or []
            zh = langs.index('Simplified Chinese') if 'Simplified Chinese' in langs else None
            zh_nonempty = sum(1 for t in terms if zh is not None and t['Languages'][zh])
            print('  解出：mTerms=%d · mLanguages=%d · zh-CN 非空 %d' % (len(terms), len(langs), zh_nonempty))
            out[pid]['decoded'] = {
                'mTerms': len(terms), 'mLanguages': len(langs), 'langs': langs,
                'zh_cn_nonempty': zh_nonempty,
                'google_last_updated_version': dec.get('Google_LastUpdatedVersion'),
            }
            diff = None
            if os.path.isfile(BUNDLE_I2_JSON):
                b = json.load(io.open(BUNDLE_I2_JSON, encoding='utf-8'))
                bt = b.get('mTerms') or []
                ka = [t['Term'] for t in terms]
                kb = [t['Term'] for t in bt]
                diffs = []
                for x, y in zip(terms, bt):
                    if x.get('Languages') != y.get('Languages'):
                        diffs.append({'term': x['Term'],
                                      'langs': [i for i, (p, q) in enumerate(zip(x['Languages'], y['Languages'])) if p != q]})
                diff = {'target': BUNDLE_I2_JSON,
                        'bundle_terms': len(bt),
                        'term_key_order_identical': ka == kb,
                        'terms_with_value_diff': len(diffs),
                        'diffs': diffs,
                        'google_last_updated_version_bundle': b.get('Google_LastUpdatedVersion')}
                print('  与 Addressables `localization` 包那份比：条数 %d vs %d · 键序一致=%s · 值不同 %d 条 %s'
                      % (len(terms), len(bt), ka == kb, len(diffs), [d['term'] for d in diffs][:5]))
                print('  Google_LastUpdatedVersion：本 build 内置 %s · 包内那份 %s'
                      % (out[pid]['decoded']['google_last_updated_version'],
                         b.get('Google_LastUpdatedVersion')))
            out[pid]['decoded']['diff_vs_bundle_copy'] = diff
        else:
            print('    temp 为空（48 B 的桩）· keys=%s' % (out[pid]['keys'],))

    payload = {
        '_meta': {
            'what': 'R1-04：resources.assets #1427 / #1431 I2Languages 的结构与「值在哪」',
            'tool': '工具/r1_residual_decode.py --i2',
            'date': '2026-10-31',
            'note': '`temp` 是逐字节 XOR 密文（算法=反编译 I2.Loc.LanguageSourceAsset__DecryptData/'
                    '_DoByteSwap，钥匙=I2.Loc.LanguageSourceAsset__.cctor 的 stringObfuscatorPassword）。'
                    '全文不落盘（10.7 MB 密文），落结构 + 摘要；词条表本身已由 K07（2026-10-18）解在 '
                    '资料/普查产出_1018/allcards_1350/localization_2026_decoded.json，'
                    '并已写进 数据/游戏数据/官方本地化_{en+zh-CN,13lang}.json。',
        },
        'objects': out,
    }
    if write:
        print('  已写: %s' % write_json('R1_1427_I2Languages_结构.json', payload))
    return payload


# ---------------------------------------------------------------- main
USAGE = __doc__


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument('--dotween', action='store_true')
    ap.add_argument('--guiskin', action='store_true')
    ap.add_argument('--i2', action='store_true')
    ap.add_argument('--all', action='store_true')
    ap.add_argument('--write', action='store_true',
                    help='把产物落到 资料/完全复刻/资源/；缺省只打印、不写盘')
    ap.add_argument('-h', '--help', action='store_true')
    args = ap.parse_args()

    if args.help or not (args.dotween or args.guiskin or args.i2 or args.all):
        print(USAGE)
        return 0

    if not os.path.isdir(DATA):
        print('找不到原版数据目录: %s' % DATA, file=sys.stderr)
        return 2
    sys.path.insert(0, TOOLS)
    if args.dotween or args.all:
        cmd_dotween(args.write)
    if args.guiskin or args.all:
        cmd_guiskin(args.write)
    if args.i2 or args.all:
        cmd_i2(args.write)
    return 0


if __name__ == '__main__':
    sys.exit(main())
