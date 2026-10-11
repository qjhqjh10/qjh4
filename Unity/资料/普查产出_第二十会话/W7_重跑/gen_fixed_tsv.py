# -*- coding: utf-8 -*-
"""W7 / A1592 —— 造一份「真换行被正确转义」的卡片文本 TSV。

用途：`E5` 那笔的离线探针（`D:/tmp/wf_e5/probe/Esc5Driver.cs`）的输入
`D:/tmp/wf_e5/texts.tsv` 把多行 `desc` 按【真换行】写了进去，而驱动的 `Load()`
是 `File.ReadAllLines` + `Split('\t')` + `if (p.Length < 3) continue;`
（`Esc5Driver.cs:25-31`）⇒ 续行掉、首行的 desc 也被截到第一句、keywords 一起丢。

本脚本按**驱动的约定**（`Desc.Replace("\\n", "\n")`，见 `Esc5Driver.cs:30`）重造一份：
`desc` 里的真换行先转义成**两个字面字符 `\\` + `n`**，keywords 用 ` ||| ` 连。

⛔ 不改任何工程文件、不写 `d:/2`、不动 git。只写 argv[1]（默认落 `_tmp_view`）。

用法：
    python -I gen_fixed_tsv.py [out.tsv]
对照自检（打印 24 张被截断的卡、字段直方图）：
    python -I gen_fixed_tsv.py --check
"""
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

POOL = "D:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
BROKEN = "D:/tmp/wf_e5/texts.tsv"
DEFAULT_OUT = "D:/4/_tmp_view/W7_e5/texts_fixed.tsv"

TAB = chr(9)
LF = chr(10)
BS = chr(92)          # 反斜杠
ESC_N = BS + "n"      # 两个字面字符：\ n —— 驱动会把它们换回真换行


def load_pool():
    with io.open(POOL, "rb") as f:
        blob = f.read()
    return json.loads(blob.decode("utf-8"))


def build_rows(pool):
    rows = []
    for c in pool["cards"]:
        desc = (c.get("desc") or "").replace(LF, ESC_N)
        kw = " ||| ".join(c.get("keywords") or [])
        rows.append(TAB.join([c["id"], c["type"], desc, kw]))
    return rows


def audit(pool, rows):
    """把「错在哪、错成什么样」用原始读数坐实。"""
    n_multi = sum(1 for c in pool["cards"] if LF in (c.get("desc") or ""))
    with io.open(BROKEN, "rb") as f:
        raw = f.read().decode("utf-8")
    lines = raw.split(LF)
    if lines and lines[-1] == "":
        lines = lines[:-1]
    hist = {}
    for ln in lines:
        hist[len(ln.split(TAB))] = hist.get(len(ln.split(TAB)), 0) + 1
    print("池 = %d 张（cards_engine.json 的 count=%s）" % (len(pool["cards"]), pool.get("count")))
    print("坏 TSV：物理行 %d（字段数直方图 = %s）" % (len(lines), dict(sorted(hist.items()))))
    print("真 desc 带换行的卡 = %d 张；新 TSV 行数 = %d" % (n_multi, len(rows)))

    # 驱动的 Load()：>=3 段才收，Kw 只在 >3 段时给
    ids = []
    for ln in lines:
        p = ln.split(TAB)
        if len(p) < 3:
            continue
        ids.append(p[0])
    print("坏 TSV 驱动侧载入 %d 条（唯一 %d）" % (len(ids), len(set(ids))))
    print("被截断（首行只有 3 段）的卡：")
    for ln in lines:
        p = ln.split(TAB)
        if len(p) == 3:
            print("   %s | %s" % (p[0], p[2][:70]))


def main(argv):
    pool = load_pool()
    rows = build_rows(pool)
    if "--check" in argv or "-c" in argv:
        audit(pool, rows)
        return 0
    out = argv[0] if argv else DEFAULT_OUT
    d = os.path.dirname(out)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    with io.open(out, "wb") as f:
        f.write((LF.join(rows) + LF).encode("utf-8"))
    print("wrote %s  (%d 张 / %d 字节)" % (out, len(rows), os.path.getsize(out)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
