#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""全局索引：pathID → Material 的瘦身 typetree（84 个 aa bundle）。

⚠️ **2026-10-06 更正（A160 / A152 A7）**：文件头原来写的「为什么按 pathID：这批包 `m_FileID` 不可信」
   —— 这条**推不出**「pathID 可以当唯一键」，**两个问题互相独立**。实测 **Material 同 pid 不同名 18 条**
   （跨包同 pid 不同名的还有 Mesh 82 · Texture2D 11 · Shader 6），**来源无一例外是
   `scenes_scenes_battlearena*`**（每个场景包的主 CAB 都从 pid=1 重新编号）。
   ⇒ 「`m_FileID` 不可信」是真的，「pid 唯一」是**假的**。
   本条错推理在三个文件里都写着（本文件 · `dump_orig.py:4` · `gen_arena_texslots.py`），见
   `资料/已知的坑.md`。

🔴 **本次修法（为什么没把键改成 `(bundle, pid)`）**：`mat_index.json` 的消费端是 `resolve.py:29`
   （`idx.get(str(pid))`），而它**不在本次改动白名单里** ⇒ 换键格式会**静默**让它一条都查不到
   （全部落进「未解出」）。所以取 A152 给的第二个选项：**保留 pid 键 + 撞号全列出来**，
   并把**CAB** 一并记进条目（`_cab`）—— 原来只记包名 `_b`，而双 CAB 包的**包名字符串相同** ⇒
   光看 `_b` 分不出是哪一份。撞号表另存 `_amb`（消费端 `.get(str(pid))` 不受影响：pid 键都是数字串）。

🔴 **2026-10-07（A161 ⑥）——「两个写手写同一个文件」**：本文件**也写 `shader_index.json`**
   （`OUTSH`，顺手建 shader 那一半），而 `build_shader_index.py` **也写它**，两支**输入不同**
   （那一支还扫 `assets_full`，本文件只扫那 84 个 bundle）⇒ 谁后跑谁定内容、覆盖不等价。
   做法（调度台口径：**加「后写的出声」、不改归属**）：产物里多一个旁挂键 **`_writer`**，
   覆盖**别人**写的之前先打一行 🔴（见 `warn_other_writer()`）。
"""

import io, json, os, sys
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
OUT = 'd:/4/Unity/_tmp_view/mataudit/mat_index.json'
OUTSH = 'd:/4/Unity/_tmp_view/mataudit/shader_index.json'
ME = os.path.basename(__file__)          # 🔴 A161 ⑥：写进产物 `_writer`、用来判「上一份是谁写的」


def warn_other_writer(path):
    """🔴 **2026-10-07（A161 ⑥）**：`OUTSH`（`shader_index.json`）有**两个写手** —— 本文件顺手建
    shader 那一半，`build_shader_index.py` 也写它（**输入不同**：那一支还扫 `assets_full`）
    ⇒ 谁后跑谁定内容、覆盖**不等价**。按调度台口径「加『后写的出声』、不改归属」：
    判据 = 产物里的旁挂键 `_writer`（与 `_amb` 同级；消费端全是 `.get(str(pid))` ⇒ 不受影响）
    ⇒ 覆盖**别人**写的之前先出声。（`OUT` 只有本文件写，属于顺手把「这份是谁产的」也钉住。）
    """
    try:
        prev = (json.load(io.open(path, encoding='utf-8')) or {}).get('_writer')
    except Exception:
        return                            # 还没有 / 读不动 / 旧版没这个键 —— 都当「没线索」，不挡写盘
    if prev and prev != ME:
        print('🔴 %s 原来是 `%s` 写的 ⇒ 本工具（`%s`）马上把它覆盖掉'
              '（两支写同一个文件、**输入不同** ⇒ 谁后跑谁定内容）' % (path, prev, ME))


def slim(m):
    sp = m.get('m_SavedProperties') or {}
    return {
        'name': m.get('m_Name'),
        'shaderPid': (m.get('m_Shader') or {}).get('m_PathID'),
        'queue': m.get('m_CustomRenderQueue'),
        'validKw': m.get('m_ValidKeywords'),
        'invalidKw': m.get('m_InvalidKeywords'),
        'floats': {k: v for k, v in (sp.get('m_Floats') or [])},
        'colors': {k: v for k, v in (sp.get('m_Colors') or [])},
        'texs': {k: ((v or {}).get('m_Texture') or {}).get('m_PathID', 0)
                 for k, v in (sp.get('m_TexEnvs') or [])},
    }


def main():
    idx, shidx = {}, {}
    seen_mat, seen_sh = {}, {}      # pid → [(名字, 包, CAB), …] —— 判撞号
    files = sorted(f for f in os.listdir(AA) if f.endswith('.bundle'))
    for i, f in enumerate(files):
        try:
            env = UnityPy.load(os.path.join(AA, f))
            for o in env.objects:
                if o.type.name not in ('Material', 'Shader'):
                    continue
                cab = getattr(getattr(o, 'assets_file', None), 'name', None)
                k = str(o.path_id)
                d = o.read_typetree()
                if o.type.name == 'Material':
                    nm = d.get('m_Name')
                    if k not in idx:                 # 🔴 首见为准（原来也是 setdefault，保持）
                        rec = slim(d)
                        rec['_b'] = f
                        rec['_cab'] = cab            # A160：连 CAB 一起记（`_b` 光看包名分不出双 CAB）
                        idx[k] = rec
                    seen_mat.setdefault(k, []).append((nm, f, cab))
                else:
                    # 🔴 A160 顺手修：原来这里是 `o.read_typetree().get('m_Name','')` ——
                    #   **Shader 的顶层 `m_Name` 实测恒为空串**（2026-10-06 抽验 5 个：
                    #   top=''、`m_ParsedForm.m_Name` 才是真名）⇒ 本文件重跑一次就会把
                    #   `shader_index.json` 写成**148 条全空**（正在污染 `build_shader_index.py` 的产物）。
                    #   判据同 `arena_mat_audit/README.md`「原版 shader 名要读 `m_ParsedForm.m_Name`」。
                    nm = (d.get('m_ParsedForm') or {}).get('m_Name') or d.get('m_Name') or ''
                    shidx.setdefault(k, nm)          # 首见为准（原来这里也是 setdefault）
                    seen_sh.setdefault(k, []).append((nm, f, cab))
            del env
        except Exception as e:
            print('  !! %s %s' % (f, e))
        print('  [%d/%d] %s (mat=%d sh=%d)' % (i + 1, len(files), f, len(idx), len(shidx)))

    def amb_of(seen):
        return {k: [[n, f, c] for n, f, c in v] for k, v in seen.items() if len({x[0] for x in v}) > 1}

    out = dict(idx)
    out['_amb'] = amb_of(seen_mat)          # 🔴 撞号**全列出来**（不静默先到先得）
    out['_writer'] = ME                     # 🔴 A161 ⑥：这份是谁产的（与 `_amb` 同级，消费端不受影响）
    warn_other_writer(OUT)
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False))
    oush = dict(shidx)
    oush['_amb'] = amb_of(seen_sh)
    oush['_writer'] = ME                    # 🔴 A161 ⑥：**这个文件 `build_shader_index.py` 也写** ⇒ 先出声再覆盖
    warn_other_writer(OUTSH)
    io.open(OUTSH, 'w', encoding='utf-8', newline='\n').write(json.dumps(oush, ensure_ascii=False))
    print('OK mat=%d shader=%d' % (len(idx), len(shidx)))
    for what, amb in (('Material', out['_amb']), ('Shader', oush['_amb'])):
        if amb:
            print('🔴 **%d 个 pid 有多个 %s 名**（`_amb` 里全列出来；按 pid 反查会撞）：'
                  % (len(amb), what))
            for k, v in sorted(amb.items())[:20]:
                print('    pid=%-8s %s' % (k, ' ｜ '.join('%s(%s/%s)' % (n, f, c) for n, f, c in v[:4])))
        else:
            print('✅ 没扫到同 pid 不同名的 %s' % what)


main()
