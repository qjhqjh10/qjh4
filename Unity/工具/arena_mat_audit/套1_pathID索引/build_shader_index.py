#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""重建 shader 名索引（名字在 `m_ParsedForm.m_Name`，顶层 `m_Name` 是空的）。

🔴 **2026-10-06 修（A160 / 普查 `资料/普查产出_1006/A152_pid陷阱普查.md` A6）**：
  原来是 `idx[str(o.path_id)] = nm` + `missing.discard` —— **全局裸 pid → shader 名**，
  先到先得、**零歧义检测**。实测 **Shader 同 pid 不同名 6 条，来源无一例外是
  `scenes_scenes_battlearena*`**（pid 28–33，例 `pid=33`：arena1 的 `Hidden/LUTBlender`
  vs tauviorla 的 `Everguild/Misc/URP Transparent Shadow Receiver`）——
  🔴 **这个坑已经咬过一次**：`disasm_dxbc.py` 的一条注释就是「`Hidden/LUTBlender` 又漏，
  它在 `scenes_scenes_battlearena*` 里」。
  修法（**保住消费端格式** —— `resolve.py` / `compare.py` / `diff.py` 都是 `shidx.get(str(pid))`，
  不能把顶层改成嵌套）：顶层仍是 `pid → 名字`，但
    ① 值改成**首见为准**（原来是 last-write-wins，取决于扫到哪张文件）；
    ② 另存一张 **`_amb`** 撞号表（`pid → [[名字, 包, CAB], …]`），撞号的**全列出来**；
    ③ **扫完所有文件**（原来 `if not missing: break` ⇒ 撞号表会是**残缺**的，会给出**假的全清**）。
  撞号表只在 `idx` 里多一个 `_amb` 键 —— pid 键全是数字串，消费端的 `.get(str(pid))` 不受影响。

🔴 **2026-10-07（A161 ⑥）——「两个写手写同一个文件」**：`build_mat_index.py` **也写这个
  `shader_index.json`**（它顺手建 shader 那一半），而两支**扫的输入不同**（本支 = 84 个 bundle
  **+ `assets_full`**；那一支只扫那 84 个 bundle）⇒ **谁后跑谁定内容，覆盖不是等价的**。
  做法（按调度台口径：**加「后写的出声」、不改归属**）：产物里多一个旁挂键 **`_writer`**
  （与 `_amb` 同级，消费端全是 `.get(str(pid))` ⇒ 不受影响）；写盘前若盘上那份是**别人**写的，
  就先打一行 🔴 再覆盖。⛔ 真要合并两支，得先有「谁才是 shader 索引的正主」的判据。
"""
import io, json, os
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
AF = 'd:/2/新解包资源/assets_full'
OUT = 'd:/4/Unity/_tmp_view/mataudit/shader_index.json'
ME = os.path.basename(__file__)          # 🔴 A161 ⑥：写进产物 `_writer`、用来判「上一份是谁写的」

targets = set()
d = json.load(io.open('d:/4/Unity/_tmp_view/mataudit/orig_resolved.json', encoding='utf-8'))
for a, v in d.items():
    for r in v['resolved']:
        p = r['mat'].get('shaderPid')
        if p is not None:
            targets.add(str(p))
print('需要 %d 个 shader' % len(targets))

idx = {}
seen = {}          # pid → [(名字, 包, CAB), …]（**全部**候选，判撞号用）
files = [os.path.join(AA, f) for f in sorted(os.listdir(AA)) if f.endswith('.bundle')]
for f in sorted(os.listdir(AF)):
    p = os.path.join(AF, f)
    if os.path.isfile(p):
        files.append(p)
missing = set(targets)
for i, p in enumerate(files):
    try:
        env = UnityPy.load(p)
        for o in env.objects:
            if o.type.name == 'Shader':
                try:
                    d2 = o.read_typetree()
                except Exception:
                    continue
                nm = (d2.get('m_ParsedForm') or {}).get('m_Name') or d2.get('m_Name') or ''
                if nm:
                    k = str(o.path_id)
                    idx.setdefault(k, nm)           # 🔴 首见为准（不再 last-write-wins）
                    cab = getattr(getattr(o, 'assets_file', None), 'name', None)
                    seen.setdefault(k, []).append((nm, os.path.basename(p), cab))
                    missing.discard(k)
        del env
    except Exception as e:
        print('  !! %s %s' % (os.path.basename(p), e))
    print('  [%d/%d] %s  shader=%d 还缺 %d' % (i + 1, len(files), os.path.basename(p), len(idx), len(missing)))

amb = {k: [[n, f, c] for n, f, c in v] for k, v in seen.items() if len({x[0] for x in v}) > 1}
out = dict(idx)
out['_amb'] = amb
# 🔴 2026-10-07（A161 ⑥）：`build_mat_index.py` **也写这个文件** ⇒ 覆盖**别人**写的之前先出声。
prev = None
try:
    prev = (json.load(io.open(OUT, encoding='utf-8')) or {}).get('_writer')
except Exception:
    pass                      # 盘上还没有 / 读不动 / 旧版没这个键 —— 都当「没线索」，不挡写盘
if prev and prev != ME:
    print('🔴 %s 原来是 `%s` 写的 ⇒ 本工具（`%s`）马上把它覆盖掉'
          '（两支写同一个文件、**输入不同** ⇒ 谁后跑谁定内容）' % (OUT, prev, ME))
out['_writer'] = ME
io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False))
print('OK shader=%d 还缺=%s' % (len(idx), sorted(missing)[:20]))
if amb:
    print('🔴 **%d 个 pid 有多个 shader 名**（`_amb` 里全列出来了；下游按 pid 反查会撞）：' % len(amb))
    for k, v in sorted(amb.items()):
        print('    pid=%-8s %s' % (k, ' ｜ '.join('%s(%s/%s)' % (n, f, c) for n, f, c in v[:4])))
else:
    print('✅ 没扫到同 pid 不同名的 shader')
