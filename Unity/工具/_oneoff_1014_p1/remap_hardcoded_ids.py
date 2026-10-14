# -*- coding: utf-8 -*-
"""P1 · 把**工具脚本里写死的旧卡 id**换算成新卡 id（`A1782` 落地链第 6 步）。

判据照 `Unity/资料/卡牌id_换真值_落地记录.md` §三·6：**按「旧 id → 同名同阵营的新 id」逐条换算，值一字不动**。
旧→新 的对照 = `_tmp_view/p1_1014/idmap.json`（`sim_apply.py` 用真值索引判据算出来的 171 条，已逐张核过）。

🔴 **按字节替换**（`b"'OLD'"` / `b'"OLD"'`）—— 只动字面量，**行尾/缩进/其它一个字节都不碰**
   （`import_original_art.py` 是混行尾：1615 CRLF + 3 个裸 LF，用文本模式写会把整篇翻掉）。

用法：python -I -X utf8 d:/4/Unity/工具/_oneoff_1014_p1/remap_hardcoded_ids.py [--dry]
"""
import argparse
import io
import json
import re
import sys

IDMAP = "d:/4/_tmp_view/p1_1014/idmap.json"
FILES = [
    "d:/4/Unity/工具/import_original_art.py",     # `ART_ID_ALIAS`（按 id 点名的贴图表）
    "d:/4/Unity/工具/gen_icon_plan.py",           # `TOKEN_BY_CARD` 的按卡表
    "d:/4/Unity/工具/gen_tutorial_stages.py",
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true")
    args = ap.parse_args()
    idmap = json.load(io.open(IDMAP, 'r', encoding='utf-8'))
    for path in FILES:
        b = io.open(path, 'rb').read()
        crlf0, lf0 = b.count(b'\r\n'), b.count(b'\n')
        out = b
        used = {}
        for old, new in sorted(idmap.items(), key=lambda kv: -len(kv[0])):
            n = 0
            for q in (b"'", b'"'):
                pat = q + old.encode('utf-8') + q
                c = out.count(pat)
                if c:
                    out = out.replace(pat, q + new.encode('utf-8') + q)
                    n += c
            if n:
                used[old] = (new, n)
        # 幂等守卫：替换后旧 id 不应再以字面量出现
        left = [o for o in idmap if (b"'" + o.encode() + b"'") in out or (b'"' + o.encode() + b'"') in out]
        crlf1, lf1 = out.count(b'\r\n'), out.count(b'\n')
        print("%s\n   换掉 %d 个键 / 共 %d 处 · 行尾 CRLF %d→%d（LF %d→%d）· 残留旧 id %d"
              % (path, len(used), sum(v[1] for v in used.values()), crlf0, crlf1, lf0, lf1, len(left)))
        if left:
            print("   ⚠️ 残留（可能是**别的同名不同卡**或注释里的历史记录，逐条看）：", left[:10])
        if crlf1 != crlf0 or lf1 != lf0:
            sys.exit("❌ 行尾被改了 —— 停下")
        if args.dry:
            continue
        if not used:
            print("   （无需改动）")
            continue
        io.open(path, 'wb').write(out)
        chk = io.open(path, 'rb').read()
        assert chk == out, path
        print("   ✅ 落盘并回读一致")


if __name__ == '__main__':
    main()
