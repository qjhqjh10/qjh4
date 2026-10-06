#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R5 只读探针 v2：按 GO pid / RT pid / 名字，递归印子树（兄弟序 = m_Children 原序）。
用法: python r5_probe.py <bundle> go:<gopid>|rt:<rtpid>|name:<名字> [depth]
"""
import sys, os, io, json
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'd:/4/Unity/工具')
import menu_rect as MR

bundle, mode_arg = sys.argv[1], sys.argv[2]
depth = int(sys.argv[3]) if len(sys.argv) > 3 else 6
os.chdir('d:/2/新解包资源/assets_full')
b = MR.Bundle(os.path.join(MR.BUNDLES, bundle))

def kids_of_rt(rtpid):
    return b.children(rtpid) or []

def go_pid_of_rt(rtpid):
    return b.go_of_rt(rtpid)

def name_of_rt(rtpid):
    n = b.go_name_of_rt(rtpid)
    return n if n else '<?>'

def rec(rtpid, ind, d, path):
    print('%s%s  [rt=%s  go=%s]' % ('  ' * ind, name_of_rt(rtpid), rtpid, go_pid_of_rt(rtpid)))
    if d <= 0:
        return
    for k in kids_of_rt(rtpid):
        rec(k, ind + 1, d - 1, path)

if mode_arg.startswith('go:'):
    gp = mode_arg[3:]
    rt = b.rt_of_go(gp)
    print('go %s name=%r rt=%s' % (gp, b.go_name(gp), rt))
    if rt: rec(rt, 0, depth, [])
elif mode_arg.startswith('rt:'):
    rt = mode_arg[3:]
    rec(rt, 0, depth, [])
elif mode_arg.startswith('name:'):
    nm = mode_arg[5:]
    rt = b.find_rt(nm)
    print('name %r -> rt %s (%s)' % (nm, rt, name_of_rt(rt) if rt else None))
    if rt: rec(rt, 0, depth, [])
