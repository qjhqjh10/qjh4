#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_surrogate_hardlock.py —— **「WFMatDef.shader 被写成替身名」的现读计数**（D118-29 ①）。

🔴 它回答什么
------------
2026-10-26 `E8SH` 报告与特效线任务总表记着：**43 个基材质 / 417 个 `WFMatDef` 块 / 343 个 prefab**
的 `WFMatDef.shader` 被写成了**我们替身的名字**（`WarpforgeVFX/…`）⇒ `TryResolve(d.shader)` 永远
解析到替身、走不到 `UseOriginal` 原件；当时结论是「改 `TryResolve` 要先 A/B」。
**本脚本量的是【现在】**：扫 `MyGame/Assets/**/*.prefab` 里 `shader: …` 的 `WFMatDef` 块，
按「值是否以 `WarpforgeVFX/` 开头」分两类。

判据（⛔ 不改口径）
------------------
· 数据侧的正本是 **prefab 里的 `WFMatDef` 块**（`WarpforgeEffectBinder.WFMatDef` 是纯 C# 类，
  序列化在 prefab 的 MonoBehaviour 上，字段名就叫 `shader`）—— 不是 `导出报告.tsv`（那是导出期日志）。
· 替身名判据 = `WarpforgeShaderMap.IsSurrogateName`：`StartsWith("WarpforgeVFX/")`。

用法（写盘纪律：不带显式动作 ⇒ 只打印用法 + rc=2）
--------------------------------------------------
    python -I -X utf8 d:/4/Unity/工具/check_surrogate_hardlock.py --scan        # 只读
    python -I -X utf8 d:/4/Unity/工具/check_surrogate_hardlock.py --scan --list # 再逐条列出命中块
    python -I -X utf8 d:/4/Unity/工具/check_surrogate_hardlock.py --write       # 写判据 tsV

产物（仅 `--write`）：`Unity/资料/完全复刻/判据/B118_W12_287材质现状.tsv`
退出码：0 = 扫完（命中 >0 也是 0，本工具是读数器）· 2 = 用法错。
"""
import glob
import io
import os
import sys

ROOT = 'd:/4/Unity'
ASSETS = ROOT + '/MyGame/Assets'
OUT_TSV = ROOT + '/资料/完全复刻/判据/B118_W12_287材质现状.tsv'
USAGE = __doc__


def scan():
    """⇒ (prefab 数, 块总数, [(prefab, 行号, shader, 材质名)], 按 shader 计数, 按材质基名去重计数)。"""
    prefabs = sorted(glob.glob(os.path.join(ASSETS, '**', '*.prefab'), recursive=True))
    total_blocks = 0
    hits = []
    by_shader = {}
    base_mats = set()
    for p in prefabs:
        rel = os.path.relpath(p, ASSETS).replace('\\', '/')
        text = io.open(p, 'rb').read().decode('utf-8', 'replace')
        lines = text.split('\n')
        cur_name = None
        for i, l in enumerate(lines):
            s = l.strip()
            if s.startswith('- name:'):
                cur_name = s[len('- name:'):].strip()
            elif s.startswith('name:'):
                cur_name = s[len('name:'):].strip()
            elif s.startswith('shader:'):
                total_blocks += 1
                val = s[len('shader:'):].strip()
                by_shader[val] = by_shader.get(val, 0) + 1
                if val.startswith('WarpforgeVFX/'):
                    hits.append((rel, i + 1, val, cur_name or ''))
                    base_mats.add(base_name(cur_name or ''))
        # 行内 `shader:` 之外的形态（理论上没有）：不臆造
    return len(prefabs), total_blocks, hits, by_shader, base_mats


def base_name(n):
    """`LightningTrail__9322aa06` → `LightningTrail`（与 `WarpforgeShaderMap.MaterialBaseName` 同口径）。"""
    i = n.rfind('__')
    if i <= 0 or len(n) - i - 2 < 6:
        return n
    suf = n[i + 2:]
    if all(c in '0123456789abcdefABCDEF' for c in suf):
        return n[:i]
    return n


def ab_report(lines=None):
    """D118-29 ① 的**映射级 A/B**（不跑 Unity）：读 `WarpforgeShaderMap.cs` 的四张表，
    对每个「替身名 → 原名」给出现在 `TryResolve(d.shader)` 解析成什么（d.shader 已是原名时）。"""
    p = ASSETS + '/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs'
    s = io.open(p, 'rb').read().decode('utf-8', 'replace')
    use = set()
    reps = {}
    sur = {}
    import re
    # UseOriginal：`public static readonly HashSet<string> UseOriginal = new HashSet<string> { "A", ... }`
    m = re.search(r'UseOriginal\s*=\s*new HashSet<string>\s*\{(.*?)\n\s*\};', s, re.S)
    if m:
        for x in re.findall(r'"([^"]+)"', m.group(1)):
            use.add(x)
    # Replacements：`{ "Orig", "Surrogate" },`
    m = re.search(r'Replacements\s*=\s*new Dictionary<string, string>(?:\([^)]*\))?\s*\{(.*?)\n\s*\};', s, re.S)
    if m:
        for a, b in re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', m.group(1)):
            reps[a] = b
    # SurrogateToOriginal：`{ "Surrogate", "Orig" },`（同样形状，方向相反）
    m = re.search(r'SurrogateToOriginal\s*=\s*(?:new Dictionary<string, string>(?:\([^)]*\))?\s*)?\{(.*?)\n\s*\};', s, re.S)
    if m:
        for a, b in re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', m.group(1)):
            sur[a] = b
    print()
    print('---- 映射级 A/B（d.shader 现读已是原名 ⇒ 现路径 = 原名解析）----')
    print('替身名 | 原名 | 原名在 UseOriginal | 原名有 Replacements 兜底 | 正常态解析 | 兜底路解析')
    bad = 0
    for sname, oname in sorted(sur.items()):
        in_use = 'Y' if oname in use else 'N'
        has_rep = 'Y' if oname in reps else 'N'
        normal = '原版bundle（白名单）' if oname in use else ('工程自带/原件' if oname not in reps else '自建')
        fallback = '自建（' + reps.get(oname, '?') + '）' if oname in reps else '原版bundle'
        if oname not in use:
            bad += 1
        print('%s | %s | %s | %s | %s | %s' % (sname, oname, in_use, has_rep, normal, fallback))
        if lines is not None:
            lines.append('A/B	%s	原名=%s · UseOriginal=%s · Replacements兜底=%s · 正常态=%s' % (sname, oname, in_use, has_rep, normal))
    print('小结：%d 个替身族里 **%d** 个的原名在 `UseOriginal`（= 原名解析在正常态走原件）；'
          '剩下 %d 个只能走工程自带/兜底（如实记）' % (len(sur), len(sur) - bad, bad))
    print('⚠️ 这是**映射级** A/B（解析到哪个 shader 对象），⛔ 不是像素 A/B；'
          '像素那一刀要 Unity（`WFBIND_FORCE_BUILTIN` sweep），归主对话。')
    return sur


def main(argv):
    a = list(argv)
    if not a or '-h' in a or '--help' in a:
        print(USAGE)
        return 2 if not a else 0
    if not ('--scan' in a or '--write' in a or '--ab' in a):
        print('⛔ 没有给动作（要 `--scan` 或 `--write`）。\n')
        print(USAGE)
        return 2
    do_list = '--list' in a
    print('= WFMatDef 硬锁现读（判据 = prefab 里的 `shader:` 值以 `WarpforgeVFX/` 开头）=')
    if '--ab' in a:
        ab_report()
        if not ('--scan' in a or '--write' in a):
            return 0
    n_prefab, n_blocks, hits, by_shader, base_mats = scan()
    print('prefab %d 份 · `shader:` 块 %d · **替身名块 %d** · 替身基材质（去重）%d'
          % (n_prefab, n_blocks, len(hits), len(base_mats)))
    print('不同的 shader 值 %d 种；前 8：' % len(by_shader))
    for k, v in sorted(by_shader.items(), key=lambda kv: -kv[1])[:8]:
        print('  %6d  %s' % (v, k))
    if hits:
        print('命中块：')
        for rel, ln, sh, nm in hits[: 200 if do_list else 10]:
            print('  %s:%d  %-64s  %s' % (rel, ln, sh, nm))
    if '--write' in a:
        lines = ['指标\t值\t说明',
                 'prefab 份数\t%d\t扫描 `MyGame/Assets/**/*.prefab`' % n_prefab,
                 'WFMatDef 块总数\t%d\t`shader:` 行计数' % n_blocks,
                 '替身名块数\t%d\t`WarpforgeVFX/` 开头 = 走不到 UseOriginal 原件' % len(hits),
                 '替身基材质（去重）\t%d\t' % len(base_mats),
                 '# 历史读数（E8SH 2026-10-26）\t43 基材质 / 417 块 / 343 prefab\t见 `报告/B9_E8SH_shader硬锁修复.md`',
                 '# 数据侧修复\tEffectExporter.cs:3033 写 OriginalNameFor\tcommit 9f67487f（2026-10-31 第一百零四会话）',
                 '# 复跑\tpython -I -X utf8 工具/check_surrogate_hardlock.py --scan']
        for rel, ln, sh, nm in hits:
            lines.append('命中\t%s:%d\t%s | %s' % (rel, ln, sh, nm))
        ab_report(lines=lines)
        d = os.path.dirname(OUT_TSV)
        if not os.path.isdir(d):
            os.makedirs(d, exist_ok=True)
        io.open(OUT_TSV, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines) + '\n')
        print('已写：%s' % OUT_TSV)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
