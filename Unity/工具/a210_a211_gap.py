# -*- coding: utf-8 -*-
"""A210 / A211 的**缺口自检**（只读，不改任何东西）。

判据源 = 原版 bundle 本身（`battleprefabs_vfxandmisc_assets_all.bundle`），不是我们的旁挂：
  · **A210**：原版有 **30 个 `ParticleSystemPoolable`**（TypeDefIndex 1108），落点是**模板粒子自己那个
    GameObject**；我们那 961 件 prefab 里 **0 个**。缺了不会错（`ParticleSystemAreaSpawner.CreatePooledItem`
    会就地 `AddComponent` 补一个），但**每建一颗粒子刷一条 `LogError`（假警报）**，
    把真信号淹掉；而且「资产缺什么永远看不见」。
  · **A211**：`Environmental Condition Particles Orbital` 整件没导（它**一个 `ParticleSystemRenderer` 都没有**
    ⇒ 被 `EffectExporter` 的「效果根」过滤器挡掉了，**不是**文档里写的「有意没导」）。
    它里面有 4 条不被任何 blendable 引用的 spawner/controller（旁挂 `_standaloneMissing` 记着）。

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    PYTHONIOENCODING=utf-8 $PY d:/4/Unity/工具/a210_a211_gap.py            # 只打印
    PYTHONIOENCODING=utf-8 $PY d:/4/Unity/工具/a210_a211_gap.py --strict   # 有缺口就退出码 1（自检用）

⚠️ 它**不改任何文件** —— 只是把「原版有、我们没有」逐条列出来（含**该改哪一件、改在哪条路径**）。
"""
import argparse
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
VFX_BUNDLE = 'battleprefabs_vfxandmisc_assets_all.bundle'
PREFAB_DIR = 'd:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs'
POOLABLE_SCRIPT = 149267643601780667          # `bundle_Waprforge_monoscripts/MonoScript/MonoScript_149267643601780667.json`
A211_ROOT = 'Environmental Condition Particles Orbital'


def load_bundle():
    env = UnityPy.load(os.path.join(AA, VFX_BUNDLE))
    tt = {}
    for o in env.objects:
        try:
            tt[o.path_id] = (o.type.name, o.read_typetree())
        except Exception:
            tt[o.path_id] = (o.type.name, None)
    return tt


def hierarchy_maps(tt):
    tr = {p: d for p, (t, d) in tt.items() if t == 'Transform' and isinstance(d, dict)}
    go = {p: d for p, (t, d) in tt.items() if t == 'GameObject' and isinstance(d, dict)}
    go2tr = {}
    for p, d in tr.items():
        go2tr.setdefault(d['m_GameObject']['m_PathID'], []).append(p)

    def chain(go_pid):
        tids = go2tr.get(go_pid)
        if not tids:
            return None
        cur, out, guard = tids[0], [], 0
        while cur and guard < 64:
            d = tr[cur]
            g = go.get(d['m_GameObject']['m_PathID'])
            out.append(g.get('m_Name') if g else '?')
            cur = (d.get('m_Father') or {}).get('m_PathID')
            guard += 1
        out.reverse()
        return out
    return tr, go, chain


def norm(s):
    """两边都归一化再比：**原版有带尾随空格的名字**（`'Orbital 5 repeat '` · `'Railgun Turret 1 Target '`），
    Unity 导出 YAML 时会把它写成**单引号**（`m_Name: 'Orbital 5 repeat '`）⇒ 去引号 + Trim 才比得上。
    ⚠️ 这是**比法**上的归一化，**不是**「把原版名字改好」—— 旁挂里那份仍是原样留档的判据。"""
    if s is None:
        return ''
    s = s.strip()
    if len(s) >= 2 and s[0] == s[-1] and s[0] in ("'", '"'):
        s = s[1:-1]
    return s.strip()


def prefab_names_on_disk():
    """`WarpforgeVFX/Prefabs/*.prefab` 的文件名（去扩展名）。⚠️ 按 `norm` 归一化后做键，
    回一个 {归一化名: 真实文件名}（Windows 文件名存不下尾随空格）。"""
    out = {}
    if not os.path.isdir(PREFAB_DIR):
        return out
    for f in os.listdir(PREFAB_DIR):
        if f.endswith('.prefab'):
            out.setdefault(norm(f[:-7]), f[:-7])
    return out


def prefab_go_names(prefab_path):
    """某个 prefab 文本里出现过的 `m_Name:`（**归一化**后；仅供核对路径，判层级要读 Transform）。"""
    t = io.open(prefab_path, encoding='utf-8', errors='replace').read()
    out = set()
    for line in re.findall(r'^  m_Name:.*$', t, re.M):
        out.add(norm(line.split('m_Name:', 1)[1]))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--strict', action='store_true', help='有缺口就退出码 1')
    ap.add_argument('--cs', action='store_true',
                    help='只打 `EffectExporter` 要的那张表（C# 数组字面量）—— **别手抄**，直接贴进补丁')
    args = ap.parse_args()

    tt = load_bundle()
    _, go, chain = hierarchy_maps(tt)
    disk = prefab_names_on_disk()

    # ---------- A210 ----------
    rows = []
    for pid, (ty, d) in tt.items():
        if ty != 'MonoBehaviour' or not isinstance(d, dict):
            continue
        if (d.get('m_Script') or {}).get('m_PathID') != POOLABLE_SCRIPT:
            continue
        gp = (d.get('m_GameObject') or {}).get('m_PathID')
        ch = chain(gp) if gp else None
        mps = (d.get('myParticleSystem') or {}).get('m_PathID')
        same_go = mps is not None and mps != 0 and mps in [
            (c.get('component') or {}).get('m_PathID') for c in (go.get(gp, {}).get('m_Component') or [])]
        rows.append({'root': ch[0] if ch else '?', 'path': '/'.join(ch) if ch else '?' ,
                     'leaf': ch[-1] if ch else '?', 'same_go_ps': same_go})
    rows.sort(key=lambda r: (r['root'], r['path']))

    if args.cs:
        print('    // 🆕 A210：原版 30 个 `ParticleSystemPoolable` 的落点（root \t 相对 prefab 根的层级路径）')
        print('    // 由 `工具/a210_a211_gap.py --cs` 生成 —— **别手抄**')
        print('    static readonly string[][] PoolableSpots =')
        print('    {')
        for r in rows:
            print('        new[] { "%s", "%s" },' % (r['root'].replace('\\', '\\\\').replace('"', '\\"'),
                                                    r['path'].replace('\\', '\\\\').replace('"', '\\"')))
        print('    };')
        return 0

    print('=== A210 · 原版 `ParticleSystemPoolable` %d 个（落点 = 模板粒子自己那个 GameObject）===' % len(rows))
    print('  判据：%s 的 `m_Script` → `%d`（=`bundle_Waprforge_monoscripts/MonoScript/MonoScript_%d.json`，'
          '`m_ClassName = ParticleSystemPoolable`）' % (VFX_BUNDLE, POOLABLE_SCRIPT, POOLABLE_SCRIPT))
    miss_root, miss_path, ok = 0, 0, 0
    for r in rows:
        real = disk.get(norm(r['root']))
        if real is None:
            state = '❌ prefab 不在工程里'
            miss_root += 1
        else:
            names = prefab_go_names(os.path.join(PREFAB_DIR, real + '.prefab'))
            if norm(r['leaf']) in names:
                state = '✅ 路径在（组件仍未挂 —— 本脚本只查路径）'
                ok += 1
            else:
                state = '❌ 那件 prefab 里没有这个子件'
                miss_path += 1
        print('  %-52s %-62s %s' % (r['root'], r['path'], state))
    print('  —— 小计：路径在 %d · prefab 缺 %d · 子件缺 %d' % (ok, miss_root, miss_path))
    print('  ⚠️ 本脚本**只查「那件 prefab / 那条路径在不在」** —— 组件（`ParticleSystemPoolable`）'
          '挂没挂**要另查：grep 那件 .prefab 的【内容】里有没有 `ParticleSystemPoolable`'
          '（判据见 `资料/已知的坑.md`：读 `m_Name` 判对象在不在包里要 grep 文件【内容】，不是文件名）。')
    roots = sorted(set(x['root'] for x in rows))
    n_have = 0
    for r in roots:
        real = disk.get(norm(r))
        if real and 'ParticleSystemPoolable' in io.open(os.path.join(PREFAB_DIR, real + '.prefab'),
                                                       encoding='utf-8', errors='replace').read():
            n_have += 1
    print('  🔴 实测：这 %d 件 prefab 里，**带 `ParticleSystemPoolable` 的 = %d 件**' % (len(roots), n_have))

    # ---------- A211 ----------
    print()
    print('=== A211 · `%s` ===' % A211_ROOT)
    exists = norm(A211_ROOT) in disk
    print('  我们工程里有没有这件 prefab：%s' % ('✅ 有' if exists else '❌ 没有'))
    if not exists:
        # 原版那件里有什么（证明「一个 ParticleSystemRenderer 都没有」⇒ 被出口过滤器挡掉）
        root_pid = None
        for p, d in go.items():
            if d.get('m_Name') == A211_ROOT:
                root_pid = p
                break
        n_psr = 0
        if root_pid is not None:
            # 数它子树里的 ParticleSystemRenderer：按 Transform 父链回溯
            tr, _, _ = hierarchy_maps(tt)
            mytr = [p for p, d in tr.items() if d['m_GameObject']['m_PathID'] == root_pid]
            subtree = set(mytr)
            changed = True
            while changed:
                changed = False
                for p, d in tr.items():
                    if p in subtree:
                        continue
                    if (d.get('m_Father') or {}).get('m_PathID') in subtree:
                        subtree.add(p)
                        changed = True
            owners = set(tr[p]['m_GameObject']['m_PathID'] for p in subtree)
            n_psr = sum(1 for p, (t, _) in tt.items() if t == 'ParticleSystemRenderer'
                        and (tt[p][1] or {}).get('m_GameObject', {}).get('m_PathID') in owners)
        print('  原版那件里 `ParticleSystemRenderer` = **%d** 个 ⇒ `EffectExporter.Run` 的「效果根」过滤器'
              % n_psr)
        print('  （`g.GetComponentsInChildren<ParticleSystemRenderer>(true).Length > 0`）把它挡在门外 —— '
              '**这才是没导的真因**（文档里写的「有意没导」是误记）。')

    bad = (miss_root + miss_path) > 0 or (n_have < len(set(x['root'] for x in rows))) or not exists
    print()
    print('=== 结论：%s ===' % ('🔴 有缺口（见上）' if bad else '✅ 全齐'))
    return 1 if (args.strict and bad) else 0


if __name__ == '__main__':
    sys.exit(main())
