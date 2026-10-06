#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_probe_deckinfo.py — 把一个 prefab 节点的 **MonoBehaviour 序列化字段** 逐条解出来。

为什么（2026-09-24）：`DeckGeneralInfoDemo` 的 `Toggle()` 是**两个抽屉互斥**，
但「哪个 GameObject 是 `generalInfoContainer`、哪个组件是 `cardsInDeckPanel`」
只有**读它自己那个 MonoBehaviour 的字段值**才能钉死 —— 反编译只给偏移（0x70/0x78）。
⚠️ 不要靠名字猜（`Show Deck Content Button` 这种名字会把人带偏）。

用法：
    python _probe_deckinfo.py bundle_menus_assets_all <GO名字或pid> [--class DeckGeneralInfoDemo]
    python _probe_deckinfo.py bundle_menus_assets_all "Cards" --class Image --pid 6258122472555867940

🔴 **2026-10-04（A51 F9）：同名多实例【不再静默取第一个】。**
   以前这条工具命中多个同名 GO 时只把根链印出来、然后 **dump `hits[0]`** —— 实跑
   `… "Cards" --class Image` 会印三条根链却 dump 了 `Booster Pack Open Window` 那棵（`Ban Icon` /
   `New Card Badge` / `Card Ready for level up`），**看起来像「Deck Editing Menu 的 Cards 长这样」**。
   这正是铁律 4「同名多实例先按父链判用途」要防的坑，也与本工具 `--class` 那条「表空就硬失败」同源。
   ⇒ 现在：多命中**硬失败**（把根链和 pid 打出来让你选），要指定实例请加 `--pid <GO pid>`。

🔴 **2026-10-05（A57②）：本包目录以外的引用【不再印成「这个包里没有」】。**
   这条工具只 `Bundle(<当前 bundle 目录>)` —— 它**只索引当前包的 json**。而字段里那些 pid 常常
   指向**别的包**（Image 的 `m_Sprite` 带 `m_FileID`：非 0 = 跨包引用，实测 `Cards` 那颗印的是
   →pid …（这个包里没有），而它是 `40k_main_bt_selected BW`（在别的包里，**存在**））
   ⇒ 旧措辞会被读成「资源不存在」（铁律 2 那条红线）。
   现在：① 措辞一律改成「**本包目录里没有**」；② 接上 `menu_dump.py` 的 **sprite 索引**
   （`sprite_pid_map()` = 真包 pid→名字的**全局**表，能解跨包 sprite）—— 对得上就把名字印出来；
   ③ 对不上时**如实说出索引的条数或取不到的原因**（不许留一句看起来像结论的话）；
   ④ 加 `--no-sprite` 关掉这条查询（快，但会退回到纯本包目录的读法，届时措辞里写明）。

🔴 **2026-10-06（A80②）：补上「**全类型** pid → 名字表」。**
   A57② 只接上了 sprite 那一张（那是 `menu_dump` 现成的唯一一张全局表）⇒ 跨包的
   `m_fontAsset` / `m_sharedMaterial` / 各种 SO·Mesh·AudioClip 仍然只能印「可能是别的类型 /
   或真不在本地」——**「它到底叫什么」查不出**。
   现在多一张 `pid_name_map()`（本文件自己建，`UnityPy` 扫**全部 84 个真包**、
   **全类型**、缓存 `_tmp_view/pid_names_ALL.json`），实测效果：
     · `m_fontAsset`     → `MonoBehaviour`「Pragati-Regular SDF」（TMP 的字体资产）
     · `m_sharedMaterial` → `Material`「Pragati-Regular Light Cream」
   两条正是当初解不出的那两类。⚠️ pid 是**分包局部**的 ⇒ **撞号 1291 条**（小 pid 尤其严重），
   撞了就**如实报「撞了哪几个」+「不判」**，⛔ 不许替读的人挑一个。

🔴 **2026-10-06（A127②）：补上 `m_FileID → externals → CAB` 那条【更精确】的路。**
   上面那张全类型表是「拿 pid 全库反查」——**pid 是分包局部的**，撞号只能「不判」。
   而 PPtr 里本来就带着**更硬的信息**：`m_FileID` 是**引用者所在那份 CAB 的 externals 下标**
   （Unity 的 `FileID`：0 = 本文件，N ≥ 1 = `externals[N-1]`），顺着它能**指名道姓**说出
   「这个 pid 在哪个包、哪一份 CAB 里」，撞号问题从根上没了。
   🔴 **但先测过才敢接**（本件第一件事就是测这条）——**「一个包内多份 CAB 的 externals 顺序一致」
   实测【不成立】**：
     · 84 个真包 = **69 个单 CAB + 15 个双 CAB**（多出来那份是 `<CAB>.sharedAssets`，全在 `scenes_*`）；
     · 这 15 个里 **14 个两份 CAB 的 externals 逐位不同** —— 13 个「条数相同、顺序不同」
       （`scenes_scenes_mainmenuwarpforge.bundle`：两份各 17 条，**16 个位置不同**），
       1 个连条数都不同（`scenes_scenes_battlearena3.bundle`：21 vs 22）；
       只有 `scenes_scenes_simpletransition.bundle`（2 vs 2）恰好相同。
     ⇒ **`externals[m_FileID - 1]` 必须先钉死「引用者自己在那份 CAB 里」**才成立；
       拿包级的一份 externals 去解所有对象 = **静默错**。
   🔴 另一条同趟实测（让这条路能用）：**CAB 名全库唯一** —— 99 个 CAB / **0 重名**
     ⇒ `CAB 名 → 包` 是 1:1（建表 5.6s，缓存 `_tmp_view/cab_to_bundle.json`）。
   怎么做：`对象 pid → 它所在 CAB`（`object_cab()`，要在**真包**上查，解包目录不带这个信息）
   → `externals[FileID-1].path` → 目标 CAB 名 → 它属于哪个 `.bundle`（`cab_index()`）
   → 在**那份 CAB** 里查 pid（`cab_object()`）。实测：
     `Cards/OneText` 的 TMP · `m_fontAsset = {m_FileID: 5, m_PathID: 3485036404935369831}`
     ⇒ 引用者所在 CAB 的 `externals[4]` = `CAB-daee69d99912a3dbccd9867d179c3609`
     ⇒ 属 `fonts_assets_all.bundle` ⇒ 那份 CAB 里它是 `MonoBehaviour`「Pragati-Regular SDF」。
   ⚠️ `m_FileID` 指向 `Library/unity default resources` 时 = **Unity 内置资源**
     （不是任何 .bundle 里的资产）—— 现在**看得出是这一档**，不再只能说「大概率是内置资源」。
   ⚠️ 与那两张全局表**同一个开关**（`--no-sprite`）关掉：这条路也要扫真包（首次约 6s）。
"""
import argparse
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

from menu_rect import Bundle, BUNDLES, chain_up   # noqa: E402

SKIP = ('m_ObjectHideFlags', 'm_CorrespondingSourceObject', 'm_PrefabInstance',
        'm_PrefabAsset', 'm_GameObject', 'm_Script', 'm_EditorHideFlags',
        'm_EditorClassIdentifier', 'm_Name')

# 真包目录（**与 `menu_dump.py` 同一个环境变量**；能 import 到就拿它那份，别写第二份常量）
REAL_BUNDLE_DIR = os.environ.get(
    'WF_BUNDLE_DIR', r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64')


def _bundle_dir():
    """真包目录 —— 优先用 `menu_dump.BUNDLE_DIR`（一处定义、两处用），取不到就退回本文件那份。"""
    try:
        import menu_dump
        return getattr(menu_dump, 'BUNDLE_DIR', REAL_BUNDLE_DIR)
    except Exception:                                       # noqa: BLE001
        return REAL_BUNDLE_DIR


def load_mb(bundle_dir, pid):
    p = os.path.join(bundle_dir, 'MonoBehaviour', 'MonoBehaviour_%s.json' % pid)
    if not os.path.exists(p):
        return None
    try:
        return json.load(io.open(p, encoding='utf-8'))
    except Exception:
        return None


def name_of_pid(b, pid):
    """pid → 「GameObject 名 / 组件类名」这种可读串。

    🔴 **A626③（2026-10-14）：本函数（以及本文件其它 `b.go.get(pid)` 的读法）是【按 pid 认 GO】**
       —— pid 是**分包 / 分内层 CAB 局部**的：一个导出目录里可以有两份同号 GO，
       而 `Bundle.go` 只留得下**第一份**（`Bundle.go_collisions()` 数得出来）。
       ⇒ **撞车包**上这里印的名字可能是**另一个 CAB 那一件**的。
       ⚠️ 本脚本**故意**保留 pid 入口（它的用法就是「给我一个 pid，我把它解开」）⇒ 不改成按 RT 认；
       改的是**看读数的人**：在那类包上先跑 `menu_rect.go_coll_warning(b)`（或 `b.go_collisions()`）
       看撞了哪些 pid；要认准某一颗 RT 的 GO 用 `Bundle.go_obj_of_rt` / `go_name_of_rt`。
       📌 实测（2026-10-14）：`bundle_scenes_scenes_mainmenuwarpforge` 撞 **49** 组（其中 **49** 颗 RT
       的名在这两条路下**不一样**）· `bundle_menus_assets_all` 撞 **0** 组（本脚本常跑的包 = 安全）。
       📌 **本文件里「按 pid 认 GO」的读点全表**（2026-10-14 现读 · ⛔ 改的时候一处都别漏）：
       `name_of_pid`（本函数）· 多命中那段的根链打印 · `b.go.get(str(gp))` 那两处（`gp` 取自
       某颗 RT 的 `m_GameObject.m_PathID` —— 手上其实**已经有那颗 RT 的 pid**，要撞车安全就换
       `Bundle.go_obj_of_rt`）· 组件归属那段。
    """
    g = b.go.get(str(pid))
    if g:
        return 'GO「%s」' % (g.get('m_Name') or g.get('_name'))
    r = b.rt.get(str(pid))
    if r:
        gopid = r.get('m_GameObject', {}).get('m_PathID')
        g2 = b.go.get(str(gopid))
        if g2:
            return 'RT of GO「%s」' % (g2.get('m_Name') or g2.get('_name'))
    return None


def selftest():
    """A127② 的自检：`python _probe_deckinfo.py --selftest`（**7 条**，全钉在本机这份数据上）。

    🔴 为什么要有它：这条路是「**先测得结论、再照结论写**」（见文件头 A127②）——
    最硬的两条判据（「一个包内多份 CAB 的 externals **不一致**」·「CAB 名全库**唯一**」）
    是**扫出来的**；谁把这条路改回「拿包级的一份 externals 硬解」，得有一条命令当场变红。
    ⚠️ 断言里的 pid / CAB 名是**坐标**（`bundle_menus_assets_all` 与
    `scenes_scenes_mainmenuwarpforge` 这两份真包），不是算出来的 —— 换包/重导要一起更新。
    """
    ok = True
    print('== A127② 自检（`m_FileID → externals → CAB` 精确路）==')

    def chk(no, good, msg):
        nonlocal ok
        ok = ok and good
        print('  %s %s %s' % ('✅' if good else '❌', no, msg))

    # ① 纯函数：解包目录名 → 真包名（本机 84/84 逐个对得上，见 `unpacked_to_bundle_fn`）
    f1 = unpacked_to_bundle_fn(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    chk('①', f1 == 'menus_assets_all.bundle',
        '解包目录名 → 真包名：得 `%s`（要 `menus_assets_all.bundle`）' % f1)
    # ② CAB 索引：能建起来、**0 撞名**（撞名了就「不判」，见 `cab_index`）
    b2b, amb, note, err = cab_index()
    chk('②', err is None and bool(b2b) and not amb,
        '`CAB → 包` 索引：%d 个 CAB / 撞名 %d 条 %s%s（要能建起来 + **0 撞名**）'
        % (len(b2b), len(amb if isinstance(amb, dict) else {}), note,
           '' if err is None else ' ❌ 原因 = %r' % err))
    MENUS = 'menus_assets_all.bundle'
    # ③ 对象 → 它在哪份 CAB（**单 CAB 的包**：这条只证明查询通路通）
    cab3, why3 = object_cab(MENUS, -6987702860354100523)          # `Cards/OneText` 那颗 TMP 的 MB
    chk('③', cab3 == 'CAB-f67aac7dfa53acf12e1bcb16a2f33904',
        '对象 pid → 所在 CAB：得 `%s`（要 `CAB-f67aac7dfa53acf12e1bcb16a2f33904`；'
        '`menus_assets_all.bundle` 只有 1 份 CAB）%s' % (cab3, '' if why3 is None else ' ❌ %s' % why3))
    # ④ 目标 CAB 里查 pid（跨包那半的答案：TMP 的字体资产）
    got4, why4 = cab_object('CAB-daee69d99912a3dbccd9867d179c3609', 3485036404935369831)
    chk('④', got4 == ('MonoBehaviour', 'Pragati-Regular SDF', 'fonts_assets_all.bundle'),
        '目标 CAB 里查 pid：得 `%r`（要 `(\'MonoBehaviour\', \'Pragati-Regular SDF\', '
        'fonts_assets_all.bundle)`）%s' % (got4, '' if why4 is None else ' ❌ %s' % why4))
    # ⑤ 整条精确路（跨包）：`m_fontAsset` 那条实测链路
    t5, n5, k5 = fileid_note(3485036404935369831, 5, -6987702860354100523, MENUS)
    chk('⑤', n5 == 'Pragati-Regular SDF' and 'fonts_assets_all.bundle' in t5 and k5 is None,
        '整条精确路（跨包）：名字 = `%r`（要 `Pragati-Regular SDF`）· 链路里说了哪个包 = %s'
        % (n5, 'fonts_assets_all.bundle' in t5))
    # ⑥ 整条精确路（**内置资源**那一档）：`Text.m_FontData.m_Font` = {m_FileID:17, m_PathID:10102}
    t6, n6, k6 = fileid_note(10102, 17, -2840118935819114287, MENUS)
    chk('⑥', n6 is None and k6 == 'builtin' and 'default resources' in t6,
        '整条精确路（内置资源）：档 = `%r`（要 `builtin`）· 文本里点名了 `Library/unity default '
        'resources` = %s' % (k6, 'default resources' in t6))
    # ⑦ 🔴 **最要紧的一条**：多 CAB 的包 —— `m_FileID` 的语义**随 CAB 变**。
    #    实测 `scenes_scenes_mainmenuwarpforge.bundle` 两份 CAB 的 **pid 空间是同一套**
    #    （`.sharedAssets` 那 593 个 pid **全都在**主 CAB 里，交集 = 593），而**同一个 pid 指的不是同一件**：
    #    pid=1 在 `.sharedAssets` 里是 `PreloadData`、在主 CAB 里是 `Material「Everguild/UI/Greyscale」`。
    #    ⇒ ① 两条不同答案证明「钉死 CAB」真的影响结论；② 这种 pid 必须**拒绝**（不许替读的人挑一份）。
    SC = 'scenes_scenes_mainmenuwarpforge.bundle'
    a7, _w7a = cab_object('CAB-32e46c0abb8a090277ce1cc411d6d532.sharedAssets', 1)
    b7, _w7b = cab_object('CAB-32e46c0abb8a090277ce1cc411d6d532', 1)
    c7, why7 = object_cab(SC, 1)
    chk('⑦', a7 is not None and b7 is not None and a7[0] != b7[0] and c7 is None and why7
        and '钉不死' in why7,
        '多 CAB 那档：同一个 pid=1 在两份 CAB 里是 **%s vs %s**（要**不同**）· '
        '`object_cab` 对这种 pid **必须拒绝**（得 `%s`，原因 = `%s`）'
        % (a7[0] if a7 else None, b7[0] if b7 else None, c7, (why7 or '')[:60]))
    print('== %s ==' % ('全过 ✅' if ok else '**有红** ❌'))
    return 0 if ok else 1


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bundle', nargs='?')
    ap.add_argument('root', nargs='?')
    ap.add_argument('--selftest', action='store_true',
                    help='跑 A127② 精确路的 7 条自检（不需要 bundle/root）')
    ap.add_argument('--class', dest='cls', default=None,
                    help='只解这个类名的 MonoBehaviour（默认全解）')
    ap.add_argument('--pid', dest='pid', default=None,
                    help='同名多实例时指定要看哪一个（GO 的 PathID；先不加它跑一次，'
                         '命中的根链与 pid 会打出来）')
    ap.add_argument('--no-sprite', action='store_true',
                    help='不查那**三条**全局查询（sprite 索引 + 全类型 pid→名字表 + '
                         '`m_FileID→externals→CAB` 精确路）（快）—— 代价：'
                         '指向别包的 pid 只能印「本包目录里没有」，**查不出它其实叫什么**'
                         '（见 docstring A57② / A80② / A127②）')
    args = ap.parse_args()
    if args.selftest:
        return selftest()
    if not args.bundle or not args.root:
        ap.error('要 `bundle` + `root`（或 `--selftest`）')
    global NO_SPRITE
    NO_SPRITE = bool(args.no_sprite)
    if NO_SPRITE:
        sys.stderr.write('⚠️ `--no-sprite`：指向**别的包**的 pid 只会印「本包目录里没有」——'
                         '**那不等于资源不存在**（本工具只索引当前 bundle 目录，'
                         '且三条全局查询 —— 含 A127② 的 `m_FileID→externals→CAB` 精确路 —— 都关着）\n')

    path = args.bundle if os.path.isdir(args.bundle) else os.path.join(BUNDLES, args.bundle)
    b = Bundle(path)

    # 🔴 **同名多实例**：先按父链判用途（`CLAUDE.md` 铁律 4 那条）—— 每个实例印出它的根祖先
    hits = b.find_go_by_name(args.root) if not str(args.root).lstrip('-').isdigit() else [str(args.root)]
    if not hits:
        sys.exit('找不到 GameObject「%s」' % args.root)
    if args.pid:
        # 显式指定实例：跳过「按名字猜」这一步（仍核一遍，写错 pid 要出声）
        gopid = str(args.pid)
        if gopid not in hits:
            sys.exit('❌ `--pid %s` 不在「%s」的命中表里（命中：%s）'
                     % (args.pid, args.root, ', '.join(hits)))
    elif len(hits) > 1:
        print('# 「%s」在本包里有 %d 个实例 —— 按根祖先区分（**先判用途再取值**）' % (args.root, len(hits)))
        for h in hits:
            rp = b.rt_of_go(h)
            chain = chain_up(b, rp) if rp else []
            tops = []
            for c in chain:
                gp = b.go_of_rt(c)
                tops.append(b.go_name(gp) or '?')
            print('#   GO %s  根链：%s' % (h, ' > '.join(tops)))
        print()
        # 🔴 **2026-10-04（A51 F9）**：多命中**不许静默取第一个** —— 原来这里直接 `hits[0]`，
        #    实跑 `"Cards" --class Image` 会 dump 到 `Booster Pack Open Window` 那棵，**看着像真结论**。
        #    与 `--class` 表空时硬失败同一条原则（铁律 4：同名多实例先按父链判用途）。
        sys.exit('❌ 「%s」在本包里有 %d 个同名实例 —— **多义不许静默取第一个**。'
                 '上面三条根链挑一条，用 `--pid <GO pid>` 指定。' % (args.root, len(hits)))
    else:
        gopid = hits[0]
    rtp = b.rt_of_go(gopid)
    if rtp is None:
        sys.exit('「%s」没有 RectTransform' % args.root)

    # 走一遍整棵子树，收集 (rtpid, gapid, 名字)
    out = []
    stack = [(rtp, 0)]
    seen = set()
    while stack:
        cur, d = stack.pop()
        if cur in seen:
            continue
        seen.add(cur)
        rt = b.rt.get(str(cur))
        if rt is None:
            continue
        gp = rt.get('m_GameObject', {}).get('m_PathID')
        g = b.go.get(str(gp)) or {}
        out.append((cur, gp, g.get('m_Name') or g.get('_name'), d))
        for c in b.children(cur):
            stack.append((c, d + 1))

    owners = comp_owners(b)
    # A127②：这一份 MB 自己所在的那个**真包**文件名 —— 精确路（`m_FileID → externals → CAB`）
    # 要在真包上先钉「引用者在哪份 CAB 里」，解包目录名 → 真包名的换算只此一处。
    bundle_fn = unpacked_to_bundle_fn(path)
    # 🔴 **2026-10-04（W4）加**：`--class` 过滤**依赖类名表**；表一空，下面那个 `continue` 会把
    #    每一条都当「不匹配」跳过 ⇒ 输出为空、**看着像「这个包里没有这个类」**（我们就是这么被骗了几轮）。
    #    ⇒ 表空时**直接失败**，不给出空结果。
    if args.cls and not script_names_cached():
        sys.exit('❌ `--class %s` 需要 MonoScript 类名表，但那张表是空的（%s）——'
                 '不给出空结果（空 = 会被读成「这个包里没有这个类」）。' % (args.cls, _SCRIPTS_ERR or '原因未知'))
    n_shown = 0
    for (rtpid, gp, name, d) in out:
        g = b.go.get(str(gp)) or {}
        for c in g.get('m_Component', []):
            cp = str(c['component']['m_PathID'])
            if not cp.lstrip('-').isdigit():
                continue
            mb = load_mb(path, cp)
            if mb is None:
                continue
            script = mb.get('m_Script', {}).get('m_PathID')
            # 类名：从 m_Script 的 MonoBehaviour 里读 `m_ClassName`；取不到就印 pid
            clsname = 'MB[%s]' % (load_script_name(path, script) or ('pid ' + str(script)))
            if args.cls and args.cls not in (load_script_name(path, script) or ''):
                continue
            n_shown += 1
            print('=' * 70)
            print('%s「%s」 (depth %d)  组件 %s' % ('  ' * d, name, d, clsname))
            for k, v in mb.items():
                if k in SKIP:
                    continue
                print('   %-34s = %s' % (k, fmt(b, v, 0, owners, cp, bundle_fn)))
    if args.cls and n_shown == 0:
        # 表在、但一条没匹配 ⇒ 这是**真结论**（不是静默失败）：说出来
        sys.stderr.write('（`--class %s`：这棵树里一个匹配的组件都没有 —— 类名表在，这是真结论）\n' % args.cls)
    return 0


def load_script_name(bundle_dir, script_pid):
    """类名走 `menu_dump.mono_index()` —— MonoScript 在**另一个包**（`*monoscript*`）里，
    **不在当前 bundle 目录下**（照搬自 `menu_dump.py` 的注释，别再自己猜）。

    🔴 **2026-10-04（W4）修**：这里原来写的是 `from menu_dump import script_names` ——
       `menu_dump` **根本没有这个名字**（真名 = `mono_index()`，见 `menu_dump.py:143`），
       抛出 `ImportError` 时又被 `except Exception` **吞掉** ⇒ 类名表恒空 ⇒
       `--class` 过滤**恒不匹配、静默打印 0 条**（不加 `--class` 时才看不出问题）。
       现在：① 用真名 `mono_index`；② 兜底再试一次 `menu_dump` 里**实际存在的**候选名；
       ③ 真取不到时**出声**（`--class` 那条路另外硬失败，见 `main`）。"""
    return script_names_cached().get(str(script_pid))


_SCRIPTS = None
_SCRIPTS_ERR = None


def script_names_cached():
    """PathID → 类名。取不到时**返回空表并记下原因**（由调用方决定是出声还是硬失败）。"""
    global _SCRIPTS, _SCRIPTS_ERR
    if _SCRIPTS is None:
        try:
            import menu_dump
            fn = getattr(menu_dump, 'mono_index', None)
            if fn is None:                     # 上游改名兜底：把这些名字都试一遍
                for cand in ('mono_index', 'script_names', 'mono_script_index'):
                    fn = getattr(menu_dump, cand, None)
                    if fn is not None:
                        break
            if fn is None:
                raise AttributeError('menu_dump 里没有 mono_index / script_names')
            try:
                _SCRIPTS = fn(verbose=False)   # ⚠️ 别让它往 stdout 打索引行数（会混进报告）
            except TypeError:
                _SCRIPTS = fn()
        except Exception as e:                                  # noqa: BLE001
            _SCRIPTS_ERR = repr(e)
            sys.stderr.write('⚠️ 取不到 MonoScript 类名表：%r\n' % (e,))
            _SCRIPTS = {}
    return _SCRIPTS


_SPRITES = None
_SPRITES_AMB = None
_SPRITES_ERR = None
NO_SPRITE = False                     # `--no-sprite`：关掉「全局 sprite 索引」这条查询（见 docstring A57②）

# 🔴 **2026-10-06（A80②）：全类型 pid → 名字表**（`UnityPy` 扫全部真包；**不只 Sprite**）。
#    为什么还要一张（sprite 那张不够）：`m_fontAsset` / `m_sharedMaterial` / 各种 SO·Mesh·AudioClip
#    的 pid **也不在本包目录里**，sprite 表解不了 ⇒ 旧输出只能印「可能是别的类型 / 或真不在本地」，
#    **「它到底叫什么」查不出**（正是 A57② 留的那半条）。
#    实测（84 个真包 · 239921 个对象 · 67075 个带名字）：
#      · `m_fontAsset` → `MonoBehaviour`「Pragati-Regular SDF」· `m_sharedMaterial` → `Material`
#        「Pragati-Regular Light Cream」—— 两条正好是当初解不出的那两类。
#      · **同名不同包撞号 1291 条**（小 pid 尤其严重：`1`/`2`/`3`…）⇒ 撞了就**如实报撞**、**不判**。
#      · `AssetBundle` 类型**不收**：它的 `peek_name()` 返回的是**包文件名**（不是资产名），
#        收进来只会制造一堆假撞名。
PIDMAP_CACHE = 'd:/4/_tmp_view/pid_names_ALL.json'
PIDMAP_VERSION = 1
_PIDMAP = None
_PIDMAP_AMB = None
_PIDMAP_TYP = None
_PIDMAP_ERR = None


def pid_name_map(quiet=True):
    """`PathID → 名字`，**全类型**（不只是 Sprite）。缓存 `_tmp_view/pid_names_ALL.json`。

    返回 `(uniq, ambiguous, types)`：
      · `uniq[pid]`      = 全库**唯一**的那个名字（撞号的不进来）
      · `ambiguous[pid]` = `[[名字, 包文件名, 类型], …]`（**撞了就全列出来**，让读的人自己判）
      · `types[pid]`     = 那一件的**资产类型名**（`GameObject` / `MonoBehaviour` / `Material`…）

    ⚠️ PathID 是**分包局部**的 ⇒ 这张表是「全局反查」，**撞号必须出声**（与 sprite 表同一个口径）。
    ⚠️ 要 `UnityPy`；没装 / 没有缓存且扫不动 ⇒ **空表 + 记原因**，调用方**出声**（不许静默当「没有」）。
    """
    if os.path.exists(PIDMAP_CACHE):
        try:
            d = json.load(io.open(PIDMAP_CACHE, encoding='utf-8'))
            if d.get('version') == PIDMAP_VERSION:
                return (dict(d.get('uniq') or {}), dict(d.get('amb') or {}),
                        dict(d.get('type') or {}))
        except Exception:
            pass
    try:
        import UnityPy
    except ImportError as e:
        if not quiet:
            sys.stderr.write('⚠️ 没有 UnityPy（%r）⇒ 全类型表建不出来\n' % (e,))
        return {}, {}, {}
    if not os.path.isdir(_bundle_dir()):
        if not quiet:
            sys.stderr.write('⚠️ 找不到真包目录 %s ⇒ 同上\n' % _bundle_dir())
        return {}, {}, {}
    seen = {}
    files = sorted(f for f in os.listdir(_bundle_dir()) if f.endswith('.bundle'))
    for fn in files:
        try:
            env = UnityPy.load(os.path.join(_bundle_dir(), fn))
        except Exception:
            continue
        for o in env.objects:
            tn = o.type.name
            if tn == 'AssetBundle':
                continue                       # 它的名字是**包文件名**，收进来只会制造假撞名
            try:
                nm = o.peek_name()
            except Exception:
                nm = None
            if not nm:
                continue
            seen.setdefault(str(o.path_id), set()).add((tn, nm, fn))
    uniq, amb, typ = {}, {}, {}
    for pid, s in seen.items():
        names = sorted({n for _t, n, _b in s})
        if len(names) == 1:
            uniq[pid] = names[0]
            typ[pid] = sorted({t for t, _n, _b in s})[0]
        else:
            # 撞号：**全列出来**（名字、哪个包、什么类型）—— 让读的人自己判，本工具不替他挑
            amb[pid] = [[n, b, t] for t, n, b in sorted(s)][:8]
    try:
        os.makedirs(os.path.dirname(PIDMAP_CACHE), exist_ok=True)
        io.open(PIDMAP_CACHE, 'w', encoding='utf-8').write(json.dumps(
            {'version': PIDMAP_VERSION, 'n_bundle': len(files), 'uniq': uniq,
             'amb': amb, 'type': typ}, ensure_ascii=False))
    except Exception:
        pass
    if not quiet:
        sys.stderr.write('（扫 %d 个真包：唯一名 %d 条 · 撞号 %d 条）\n' % (len(files), len(uniq), len(amb)))
    return uniq, amb, typ


def pidmap_cached():
    """懒加载 `pid_name_map()`（取不到时返回三张空表 + 记原因）。"""
    global _PIDMAP, _PIDMAP_AMB, _PIDMAP_TYP, _PIDMAP_ERR
    if _PIDMAP is None:
        try:
            _PIDMAP, _PIDMAP_AMB, _PIDMAP_TYP = pid_name_map(quiet=True)
        except Exception as e:                              # noqa: BLE001
            _PIDMAP_ERR = repr(e)
            _PIDMAP, _PIDMAP_AMB, _PIDMAP_TYP = {}, {}, {}
    return _PIDMAP, _PIDMAP_AMB, _PIDMAP_TYP


def sprite_names_cached():
    """PathID → sprite 名字。**全局表**（不分包）—— 走 `menu_dump.sprite_pid_map()`
    （UnityPy 扫真包 `o.path_id`，缓存 `d:/4/_tmp_view/sprite_pids_ALL.json`）。

    🔴 **为什么需要它**（2026-10-05 · A57②）：`fmt()` 原来对本包目录里找不到的 pid 一律印
    「→pid …（这个包里没有）」—— 而 Image 的 `m_Sprite` 带 `m_FileID`，**非 0 就是跨包引用**
    （实测 `Cards` 那颗印「这个包里没有」，其实是 `40k_main_bt_selected BW`，在别的包里）
    ⇒ 那句话会被读成「资源不存在」。现在先用这张表补名字，补不到才印「本包目录里没有」。
    ⚠️ 取不到（没 UnityPy / 没有缓存且扫不动）⇒ **空表 + 记下原因**，调用方**出声**（不许静默当「没有」）。
    """
    global _SPRITES, _SPRITES_AMB, _SPRITES_ERR
    if _SPRITES is not None:
        return _SPRITES
    _SPRITES, _SPRITES_AMB = {}, {}
    try:
        import menu_dump
        fn = getattr(menu_dump, 'sprite_pid_map', None) or getattr(menu_dump, 'sprite_index', None)
        if fn is None:
            raise AttributeError('menu_dump 里没有 sprite_pid_map / sprite_index')
        sys.stderr.write('（读 sprite 索引：pid → 名字的**全局**表；没有缓存时首次要扫一遍真包，会慢）\n')
        try:
            r = fn(quiet=True)             # `sprite_pid_map(quiet=)`
        except TypeError:
            r = fn(verbose=False)          # `sprite_index(verbose=)`
        if isinstance(r, dict) and 'by_pid' in r:       # `sprite_index()` 的形状
            _SPRITES = dict(r.get('by_pid') or {})
            _SPRITES_AMB = dict(r.get('ambiguous') or {})
        else:
            _SPRITES = dict(r or {})
    except Exception as e:                                  # noqa: BLE001
        _SPRITES_ERR = repr(e)
        sys.stderr.write('⚠️ 取不到 sprite 索引：%r ⇒ 下面「本包目录里没有」那几句**没查过别包**\n' % (e,))
    return _SPRITES


# ================================================================ A127②：`m_FileID → externals → CAB`
# 判据（**先测后写**，全过程见文件头 A127② 那一段）：
#   · 「一个包内多份 CAB 的 externals 顺序一致」**不成立**（84 包里 15 个双 CAB 包，
#     其中 **14 个两份 CAB 的 externals 逐位不同**）⇒ 解 `m_FileID` 之前**必须先钉死引用者所在 CAB**。
#   · **CAB 名 → 包 是 1:1**（99 个 CAB / 0 重名）⇒ 这张索引可以放心用。
_ENV_CACHE = {}                      # 真包文件名 → {CAB 名: SerializedFile}（同一趟只载一次）
CAB2B_CACHE = 'd:/4/_tmp_view/cab_to_bundle.json'
CAB2B_VERSION = 1
_CAB2B = None                        # CAB 名 → 真包文件名
_CAB2B_AMB = None                    # CAB 名 → [包, …]（**重名**；实测 0 条，留着出声用）
_CAB2B_ERR = None                    # 建表失败的原因（不许静默当「没有」）
_CAB2B_NOTE = ''                     # 建表备注（扫了几个包 / 几个 CAB）


def unpacked_to_bundle_fn(path):
    """解包目录 → 真包文件名：`…/assets_full/bundle_menus_assets_all` → `menus_assets_all.bundle`。

    ⚠️ 实测本机 **84 个解包目录 ↔ 84 个真包逐个对得上**（去掉 `bundle_` 前缀 + 加 `.bundle`，
    0 例外、0 多余）；仍由调用方核「文件在不在」（对不上要出声，见 `fmt`/`fileid_note`）。
    """
    base = os.path.basename(os.path.normpath(str(path)))
    if base.startswith('bundle_'):
        base = base[len('bundle_'):]
    return base + '.bundle'


def bundle_cabs(bundle_fn):
    """载入一个**真包**，返回 `(CAB 名 → SerializedFile, 原因)`（带缓存；同一个包只载一次）。

    🔴 为什么要真包：`m_FileID` 是**引用者所在那份 CAB 的 externals 下标**，而解包目录
    （`assets_full/bundle_*/`）**按对象类型分目录、不带「这份对象属于哪份 CAB」**这个信息
    ⇒ 只有真包答得了。⚠️ `UnityPy` 的 `env.files` 外层 key 是**包路径**、值是 `BundleFile`，
    CAB 在**它**的 `.files` 里（第一版就在外层找 `externals`，恒 `None`）。
    """
    if bundle_fn in _ENV_CACHE:
        return _ENV_CACHE[bundle_fn], None
    try:
        import UnityPy
    except ImportError as e:                                # noqa: BLE001
        return None, '没有 UnityPy（%r）' % (e,)
    p = os.path.join(_bundle_dir(), bundle_fn)
    if not os.path.exists(p):
        return None, '真包里没有 %s' % p
    try:
        env = UnityPy.load(p)
        bf = list(env.files.values())[0]
        cabs = {k: v for k, v in (getattr(bf, 'files', {}) or {}).items()
                if not (k.endswith('.resS') or k.endswith('.resource'))}
    except Exception as e:                                  # noqa: BLE001
        return None, '载入 %s 失败（%r）' % (bundle_fn, e)
    _ENV_CACHE[bundle_fn] = cabs
    return cabs, None


def cab_index(quiet=True):
    """`CAB 名 → 真包文件名`。扫 `_bundle_dir()` 下全部 `*.bundle`（首次约 6s，之后读缓存）。

    返回 `(表, 撞名表, 备注, 原因)`；**任何一项取不到都不许静默当「没有」**（调用方出声）。
    ⚠️ 实测 99 个 CAB 名**全库唯一**（0 重名）⇒ 1:1；仍按「撞了就报、不判」的规矩写。
    """
    global _CAB2B, _CAB2B_AMB, _CAB2B_ERR, _CAB2B_NOTE
    if _CAB2B is not None:
        return _CAB2B, _CAB2B_AMB, _CAB2B_NOTE, _CAB2B_ERR
    if os.path.exists(CAB2B_CACHE):
        try:
            d = json.load(io.open(CAB2B_CACHE, encoding='utf-8'))
            if d.get('version') == CAB2B_VERSION:
                _CAB2B = dict(d.get('cab2b') or {})
                _CAB2B_AMB = dict(d.get('amb') or {})
                _CAB2B_NOTE = '（缓存：%d 个包 / %d 个 CAB / 撞名 %d）' % (
                    d.get('n_bundle', 0), len(_CAB2B), len(_CAB2B_AMB))
                return _CAB2B, _CAB2B_AMB, _CAB2B_NOTE, None
        except Exception:                                   # noqa: BLE001
            pass
    try:
        import UnityPy
    except ImportError as e:                                # noqa: BLE001
        _CAB2B, _CAB2B_AMB, _CAB2B_ERR = {}, {}, '没有 UnityPy（%r）' % (e,)
        return _CAB2B, _CAB2B_AMB, _CAB2B_NOTE, _CAB2B_ERR
    if not os.path.isdir(_bundle_dir()):
        _CAB2B, _CAB2B_AMB, _CAB2B_ERR = {}, {}, '找不到真包目录 %s' % _bundle_dir()
        return _CAB2B, _CAB2B_AMB, _CAB2B_NOTE, _CAB2B_ERR
    files = sorted(f for f in os.listdir(_bundle_dir()) if f.endswith('.bundle'))
    cab2b, amb = {}, {}
    for fn in files:
        try:
            env = UnityPy.load(os.path.join(_bundle_dir(), fn))
            bf = list(env.files.values())[0]
            names = [k for k in (getattr(bf, 'files', {}) or {})
                     if not (k.endswith('.resS') or k.endswith('.resource'))]
        except Exception:                                   # noqa: BLE001
            continue
        for k in names:
            if k in cab2b and cab2b[k] != fn:
                amb.setdefault(k, [cab2b[k]]).append(fn)
            cab2b.setdefault(k, fn)
    _CAB2B, _CAB2B_AMB = cab2b, amb
    _CAB2B_NOTE = '（扫 %d 个包 / %d 个 CAB / 撞名 %d）' % (len(files), len(cab2b), len(amb))
    try:
        os.makedirs(os.path.dirname(CAB2B_CACHE), exist_ok=True)
        io.open(CAB2B_CACHE, 'w', encoding='utf-8').write(json.dumps(
            {'version': CAB2B_VERSION, 'n_bundle': len(files), 'cab2b': cab2b, 'amb': amb},
            ensure_ascii=False))
    except Exception:                                       # noqa: BLE001
        pass
    return _CAB2B, _CAB2B_AMB, _CAB2B_NOTE, None


def object_cab(bundle_fn, pid):
    """对象 pid → 它在那份真包里的**哪一份 CAB**（`m_FileID` 必须先钉这个）。返回 `(CAB, 原因)`。"""
    cabs, why = bundle_cabs(bundle_fn)
    if cabs is None:
        return None, why
    hit = [k for k, sf in cabs.items() if int(pid) in sf.objects]
    if not hit:
        return None, 'pid %s **不在** %s 的任何一份 CAB 里（%d 份）' % (pid, bundle_fn, len(cabs))
    if len(hit) > 1:
        return None, 'pid %s 在 %s 的 %d 份 CAB 里都出现（%s）⇒ 钉不死' % (
            pid, bundle_fn, len(hit), ' / '.join(hit[:3]))
    return hit[0], None


def cab_object(cab, pid):
    """在**指定 CAB**里查 pid → `(类型名, 名字 | None)`（名字取不到也算查到 —— 回 `None`）。"""
    b2b, amb, _note, _err = cab_index()
    if cab in (amb or {}):
        return None, 'CAB 名 %s **撞号**（%s）⇒ 不判' % (cab, ' / '.join(amb[cab]))
    fn = (b2b or {}).get(cab)
    if fn is None:
        return None, 'CAB %s 不在 `cab_index()` 里' % cab
    cabs, why = bundle_cabs(fn)
    if cabs is None:
        return None, why
    sf = cabs.get(cab)
    if sf is None:
        return None, '%s 里没有 CAB %s' % (fn, cab)
    o = sf.objects.get(int(pid))
    if o is None:
        return None, '那份 CAB 里没有 pid %s' % pid
    try:
        nm = o.peek_name()
    except Exception:                                       # noqa: BLE001
        nm = None
    return (o.type.name, nm, fn), None


def fileid_note(pid, file_id, owner_pid, bundle_fn):
    """**精确路**：`m_FileID → 引用者所在 CAB 的 externals[FileID-1] → 目标包/CAB → 那个 pid 的名字`。

    返回 `(文本, 名字 | None, 档)`：`文本` 一定要印（成功了说清链路，失败了说清**为什么没走成**，
    ⛔ 不许留一句看起来像结论的话）；`名字` 有值时才表示**这条路真查出来了**；
    `档` ∈ `{None, 'builtin'}`（`'builtin'` = **已经查实**那个 `m_FileID` 指向
    `Library/…` 这类**非 archive 的内置资源** ⇒ 调用方那句「大概率是内置资源」要换成肯定句）。
    `owner_pid` = **引用者自己**（那个 MonoBehaviour）的 pid —— 缺它就没法知道是哪份 CAB 的 externals。
    """
    pre = '；🔹**精确路**（A127' + '）'
    if not file_id:
        return pre + '：`m_FileID=0` ⇒ 指的是**本文件内**的 pid，不是跨包（这条**没走**）', None, None
    if not bundle_fn:
        return pre + '：没给真包文件名（解包目录名对不上）⇒ **没走**', None, None
    if not os.path.isdir(_bundle_dir()) or bundle_fn not in os.listdir(_bundle_dir()):
        return pre + '：真包里找不到 %r（在 %s）⇒ **没走**' % (bundle_fn, _bundle_dir()), None, None
    if owner_pid is None:
        return pre + '：拿不到**引用者自己**的 pid ⇒ 不知该用哪份 CAB 的 externals（**没走**）', None, None
    owner_cab, why = object_cab(bundle_fn, owner_pid)
    if owner_cab is None:
        return pre + '：钉不住引用者所在 CAB（%s）⇒ **没走**' % why, None, None
    cabs, why2 = bundle_cabs(bundle_fn)
    if cabs is None:
        return pre + '：%s ⇒ **没走**' % why2, None, None
    ext = [getattr(x, 'path', None) for x in (getattr(cabs[owner_cab], 'externals', None) or [])]
    i = int(file_id) - 1
    if i < 0 or i >= len(ext):
        return pre + '：`m_FileID=%d` **越界**（%s 的 externals 只有 %d 条，下标要 0…%d）⇒ **没走**' % (
            file_id, owner_cab, len(ext), len(ext) - 1), None, None
    tgt = ext[i]
    if not tgt:
        return pre + '：`externals[%d]` 是空的 ⇒ **没走**' % i, None, None
    if not str(tgt).startswith('archive:/'):
        # Unity 内置资源 / 其它非 archive 的 externals：**看得出来了**，不用再猜「大概率是内置」
        return pre + '：`m_FileID=%d` ⇒ 引用者所在 CAB `%s` 的 `externals[%d]` = `%s`' \
               '（**不是任何 .bundle 里的资产** —— `archive:/` 开头才是包内 CAB）⇒ 名字查不到' % (
                   file_id, owner_cab, i, tgt), None, 'builtin'
    tgt_cab = str(tgt).split('/')[-1]
    got, why3 = cab_object(tgt_cab, pid)
    if got is None:
        return pre + '：`m_FileID=%d` ⇒ `externals[%d]` = `%s`，但%s ⇒ **没走通**' % (
            file_id, i, tgt_cab, why3), None, None
    tn, nm, fn = got
    _b2b, _amb, note, _e = cab_index()
    return pre + '：`m_FileID=%d` ⇒ 引用者所在 CAB `%s` 的 `externals[%d]` = `%s`' \
           '（属 `%s`%s）⇒ **那份 CAB 里它是 %s%s**（pid 是**分包局部**的，这条按 `m_FileID` 指名道姓、' \
           '不受撞号影响）' % (file_id, owner_cab, i, tgt_cab, fn, note, tn,
                             ('「%s」' % nm) if nm else '（**名字取不到**）'), nm, None


def cross_bundle_note(pid, file_id=None, owner_pid=None, bundle_fn=None):
    """`fmt()` 那句「本包目录里没有」的尾巴：这个 pid **在全局表里**叫什么。

    次序（**先窄后宽**，三条都留着）：
      ① 🔴 **精确路**（`m_FileID → 引用者所在 CAB 的 externals → 目标包/CAB → 那个 pid`，A127②）——
         能指名道姓（哪个包、哪份 CAB），**pid 撞号也不怕**；
      ② **sprite 表**（`menu_dump.sprite_pid_map()`）—— 类型明确、建得早，先问它；
      ③ 🔴 **全类型表**（`pid_name_map()`，A80②）—— `m_fontAsset`/`m_sharedMaterial`/SO/Mesh
         这些 sprite 表根本不收的，靠它才能说出「它到底叫什么」。
    ①走通时把它的结论**放在最前**；②③都对不上时**如实说**：索引多少条 / 为什么没查成 / 撞了多少个
    （⛔ 不许留一句像「资源不存在」的话）。
    `--no-sprite` ⇒ 三条都不查，并明确写出「没查」。
    """
    if NO_SPRITE:
        return '；⚠️ `--no-sprite` ⇒ **没查**别包（精确路/两张全局表都关着 —— 别把这句读成「资源不存在」）'
    prec, prec_name, prec_kind = fileid_note(pid, file_id, owner_pid, bundle_fn)
    idx = sprite_names_cached()
    key = str(pid)
    if key in idx:
        tail = '；**sprite 索引里有它** = 「%s」⇒ 跨包引用（`m_FileID ≠ 0` 时正常）' % idx[key]
        return prec + ('' if prec_name is None else _agree(prec_name, idx[key])) + tail
    amb = (_SPRITES_AMB or {}).get(key)
    if amb:
        return prec + '；⚠️ sprite 索引里这一条**打架**（%s）—— 别照抄' % ' / '.join(amb)
    # ② 全类型表（A80②）
    uniq, amb2, typ = pidmap_cached()
    if key in uniq:
        t = typ.get(key, '?')
        return (prec + ('' if prec_name is None else _agree(prec_name, uniq[key]))
                + '；**全类型表里有它** = %s「%s」⇒ 跨包引用（`m_FileID ≠ 0` 时正常）' % (t, uniq[key]))
    if key in amb2:
        cand = ' / '.join('%s「%s」@%s' % (t, n, b) for n, b, t in amb2[key])
        return (prec + '；⚠️ **全类型表里这个 pid 撞名 %d 个**（%s …）⇒ **不判**它到底是哪一个'
                % (len(amb2[key]), cand))
    if _PIDMAP_ERR is not None:
        return prec + '；⚠️ 全类型表**没建起来**（%s）⇒ 这句没查过别包' % _PIDMAP_ERR
    if _SPRITES_ERR is not None or not idx:
        return prec + ('；⚠️ sprite 索引**没查成**（%s）⇒ 这句没查过别包'
                       % (_SPRITES_ERR or '空表（原因未知）'))
    if not uniq and not amb2:
        return prec + '；⚠️ 全类型表**是空的**（没 UnityPy / 真包目录不在 / 建表失败）⇒ 这句没查过别包'
    if prec_kind == 'builtin':
        # 精确路**已经查实**它指向非 archive 的内置资源 ⇒ 这里不许再写「大概率是内置资源」
        return (prec + '；sprite 索引（%d 条）与**全类型表**（%d 条 + 撞号 %d 条）里也都没有它 '
                '—— 与上面的精确路**一致**（不是「查不到」，是它本来就不在任何包里）'
                % (len(idx), len(uniq), len(amb2)))
    return (prec + '；sprite 索引（%d 条）与**全类型表**（%d 条 + 撞号 %d 条）里都没有它 '
            '⇒ 大概率是**内置资源**（`m_FileID` 指向 Unity 内置）或真不在本地 —— '
            '但**本工具只索引当前 bundle 目录 + 那两张全局表**' % (len(idx), len(uniq), len(amb2)))


def _agree(a, b):
    """两条路都查出了名字时的小尾巴 —— 一致就 ✓，不一致就**大声报**（两条都是判据，打架要看得见）。"""
    if a == b:
        return '（两条路一致 ✓）'
    return '（⚠️ **两条路不一致**：精确路给「%s」、全局表给「%s」—— 以**精确路**为准）' % (a, b)


def comp_owners(b):
    """组件 pid → 它的 GameObject 名。🔴 字段值多半是**组件**（Image/TMP/Button…）的 pid，
    不是 GameObject/RT 的 —— 只查 rt/go 两张表会全部落空（我第一版就栽在这）。"""
    m = {}
    for gopid, g in b.go.items():
        nm = g.get('m_Name') or g.get('_name')
        for c in g.get('m_Component', []):
            cp = str(c['component']['m_PathID'])
            m.setdefault(cp, (nm, gopid))
    return m


def comp_go_name(m, pid):
    hit = m.get(str(pid))
    return hit[0] if hit else None


def fmt(b, v, depth=0, owners=None, owner_pid=None, bundle_fn=None):
    """把一个字段值印成可读串。

    `owner_pid` / `bundle_fn` = **这一份 MonoBehaviour 自己**的 pid 与它所在的那个真包文件名 ——
    A127② 的精确路（`m_FileID → externals → CAB`）要用它们钉「引用者在那份 CAB 里」；
    递归时**原样往下传**（同一个 MB 的字段，不分嵌套多深，都属同一份 CAB）。
    """
    if isinstance(v, dict):
        if 'm_PathID' in v and set(v.keys()) <= {'m_PathID', 'm_FileID'}:   # 🔴 字段值是 {m_FileID, m_PathID} **两个键**，不是只剩 m_PathID（第一版就是按 len==1 判的，全落空）
            pid = v['m_PathID']
            if pid == 0:
                return 'null'
            r = b.rt.get(str(pid))
            if r is not None:
                gopid = r.get('m_GameObject', {}).get('m_PathID')
                g = b.go.get(str(gopid)) or {}
                return '→RT of「%s」(GO %s / RT %s)' % (g.get('m_Name') or g.get('_name'), gopid, pid)
            g = b.go.get(str(pid))
            if g is not None:
                return '→GO「%s」(pid %s)' % (g.get('m_Name') or g.get('_name'), pid)
            if owners:
                nm = comp_go_name(owners, pid)
                if nm:
                    return '→组件 on GO「%s」(组件 pid %s)' % (nm, pid)
            return '→pid %s（**本包目录里没有**%s）' % (
                pid, cross_bundle_note(pid, v.get('m_FileID'), owner_pid, bundle_fn))
        return '{' + ', '.join('%s:%s' % (k, fmt(b, x, depth + 1, owners, owner_pid, bundle_fn))
                               for k, x in v.items()) + '}'
    if isinstance(v, list):
        if depth > 1:
            return '[%d 项]' % len(v)
        return '[' + ', '.join(fmt(b, x, depth + 1, owners, owner_pid, bundle_fn)
                               for x in v) + ']'
    return repr(v)


if __name__ == '__main__':
    sys.exit(main())
