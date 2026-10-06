# -*- coding: utf-8 -*-
"""menu_dump 的「旧版行为复现器」—— 打印某扇窗 **W1(旧) → W3(现在)** 的逐节点位移。

## 为什么留着它
2026-10-03 把 `menu_dump._child_sizes` 补成**完整 uGUI**（W1 → W3）之后，**真值变了**，
而我们有几处 C# 常量是照 **W1（旧值）** 抄的 ⇒ 排查「这个常量是不是照旧值抄的」时，
**必须能把旧版行为原样复现出来**。这次（A88）就是靠它定位了两处「没查清」。

## 怎么跑
    python d:/4/Unity/工具/menu_dump_w1diff.py "MessagePopupWindowDuel" "Alliance Trophy Info Popup"
（可一次给多个窗名；窗名照 `menu_dump` 那套）

## 怎么做的（**不碰仓库**）
它在内存里 `exec` 一份 `menu_dump.py` 的**拷贝**，只把那一行
`if fexp:` 改成 `if fexp and ctrl:`（= W1 的语义）⇒ **仓库里的 `menu_dump.py` 一个字没动**。

## 出处
2026-10-03 由 A88 那批的执行代理写于系统临时目录 `/tmp/a88/w1diff.py`（**会丢**），
同日搬进 `工具/` 并补了这段头。
"""
import io, os, sys, types
sys.path.insert(0, r'd:/4/Unity/工具')
import menu_rect as MR
import menu_dump as MD3

SRC = io.open(r'd:/4/Unity/工具/menu_dump.py', encoding='utf-8').read()
OLD = "    if fexp:                          # `:237-238` —— 在 if/else【外面】，与 `ctrl` 无关\n        fx = max(fx, 1.0)"
if OLD not in SRC:
    # 尝试用更宽松的定位
    import re
    m = re.search(r"\n    if fexp:\s*\n        fx = max\(fx, 1\.0\)\n", SRC)
    assert m, 'patch anchor not found'
    SRC2 = SRC[:m.start()] + "\n    if fexp and ctrl:\n        fx = max(fx, 1.0)\n" + SRC[m.end():]
else:
    SRC2 = SRC.replace(OLD, "    if fexp and ctrl:                 # [W1 模拟] 只在 ctrl 时 Max(.,1)\n        fx = max(fx, 1.0)")

md = types.ModuleType('menu_dump_w1')
md.__dict__['__name__'] = 'menu_dump_w1'
md.__dict__['__file__'] = r'd:/4/Unity/工具/menu_dump.py'
exec(compile(SRC2, 'menu_dump_w1', 'exec'), md.__dict__)

BUNDLE = os.path.join(MR.BUNDLES, 'bundle_menus_assets_all')
b = MR.Bundle(BUNDLE)
mono = MD3.mono_index()

def rows(mod, root, depth=14):
    # 🔴 **A626③（2026-10-14）：下面这两句是【按 pid 认 GO】**（`find_go` 查的是 `Bundle.go`
    #    = 「pid 去重后的索引」，撞车时**另一份 GO 根本不在里面**）⇒ 在**撞车包**上：
    #    按名字找**整个找不到**（`assert gopid` 会炸）、或找到**另一个 CAB 的那一份**。
    #    本脚本只跑 `bundle_menus_assets_all`（**实测 0 组撞车**）⇒ 今天不受影响；
    #    要挪去别的包时改成 `b.find_rt(root)`（A499 那条撞车安全的路，`menu_rect.main` 用的就是它）。
    #    ⚠️ 判据与「哪些包撞了」见 `menu_rect.go_coll_warning` / `Bundle.go_collisions()`。
    gopid = b.find_go(root)
    assert gopid, root
    rtpid = b.rt_of_go(gopid)
    # 🔴 2026-10-05（块13 连带）：`menu_rect.parent_rect_of` 的返回从 **2 元组改成 3 元组**
    #   —— 块13 给 `rect_of` 补上了 `scale` 语义（`scale` = **lossyScale(父)**，爬链时要跟着累积）。
    #   ⇒ 这里必须同步两件事：
    #     ① 解包出第三个值 `psc`；
    #     ② `walk` 的 `scale` 实参从**写死的 `(1.0, 1.0)`** 换成 `psc`
    #        （写死 1.0 = 退回「不乘祖先缩放」的旧口径，那正是块13 修掉的那个错）。
    base, pname, psc = MR.parent_rect_of(b, rtpid, (0.0, 0.0, 1920.0, 1080.0))
    out = []
    mod.walk(b, mono, rtpid, base, psc, 0, depth, out, 0, True, {'by_pid': {}, 'ambiguous': {}}, {},
             force_root_rect=None, stats=mod.new_stats())
    res = []
    for e in out:
        res.append((e['name'], tuple(round(v, 4) for v in e['rect']), e['ind']))
    return res

def rect(t):
    return tuple(float(x) for x in t.replace('→', ',').split(','))

def diff(root, depth=14):
    w3 = rows(MD3, root, depth)
    w1 = rows(md, root, depth)
    if len(w3) != len(w1):
        print(f'!! {root}: 行数不同 {len(w1)} vs {len(w3)}')
    print(f'=== {root}: {len(w1)} 行 (W1) / {len(w3)} 行 (W3)')
    for (n1, r1, c1), (n3, r3, c3) in zip(w1, w3):
        if n1 != n3:
            print('   行错位:', n1, n3); continue
        a, bb = r1, r3
        d = max(abs(x - y) for x, y in zip(a, bb))
        if d > 0.005:
            print(f'   {n1:42s} {r1}  ->  {r3}   最大位移 {d:.2f}')

if __name__ == '__main__':
    for w in sys.argv[1:]:
        diff(w)
