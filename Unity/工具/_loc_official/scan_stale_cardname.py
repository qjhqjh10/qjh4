# -*- coding: utf-8 -*-
"""扫 `Assets/**/*.cs`：还有哪些地方**写死了我们那批被换掉的旧中文卡名**（可能是断言期望值）。只读。

为什么要另写一版（`A1801`，2026-10-14）：同目录的 `scan_stale_loc.py` 比的是**两份 `Loc.cs` 的 Entry 值**，
与卡名那条链**不同源**（卡名走 `zh_cards.json` → `gen_cards_engine.py` → `cards_engine.json`）。
⛔ 直接复用它会把「Loc 表里换了」当成「卡表里换了」，**一条真命中都扫不出来**。

口径（比 `scan_stale_loc.py` 收严三条，否则会误报一片）：
  ① **只匹配引号包裹的字面量**（`"旧名"`）—— 不限引号的话中文注释会全进来；
  ② **名长 ≥ 2**（单字名几乎必然误报）；
  ③ 命中后**回看那一行**，判它是「期望值」还是「注释 / 兜底串」，**分别列出来**（铁律 13·5：
     审查给的是逐条 `结论 / 证据 / 该照哪个判据`，⛔ 不许只报个数）。

OLD / NEW 两份值集：
  · NEW = 工作区当前 `cards_engine.json` 的 `nameZh` 列（**换官方之后**的）
  · OLD = 默认取 `git HEAD:` 那一版（**换官方之前**的）—— 所以本脚本要在**提交之前**跑；
          也可以 `--old <路径>` 指一份快照。
若 HEAD 那一版与工作区**没有差异**（已经提交过了）⇒ 命令会把两条链都取到同一份，
判据失据 —— 脚本**当场报出来**（⛔ 不许静默给 0 条）。

用法：
    python -I -X utf8 d:/4/Unity/工具/_loc_official/scan_stale_cardname.py
    ... --old d:/4/_tmp_view/cards_engine_换官方前.json
"""
import io
import os
import re
import subprocess
import sys

ROOT = r"d:/4/Unity/MyGame/Assets"
NEWSRC = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
REPO = r"d:/4"
GITREL = "Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

# 「这一行像是**期望值**」的判据：出现断言口 / 等值比较 / 期望值形参名
EXPECT = re.compile(r"\b(Check|Assert|Expect|ExpectEq|IsTrue|AreEqual)\b|==|!=")


def load_cards(path):
    import json
    d = json.load(io.open(path, encoding="utf-8"))
    return {c["id"]: (c["name"], c.get("nameZh") or "") for c in d["cards"]}


def load_head():
    out = subprocess.run(["git", "show", "HEAD:" + GITREL], cwd=REPO,
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if out.returncode != 0:
        sys.exit("❌ 取不到 `git HEAD:%s`（%s）—— 用 `--old <路径>` 手动指一份换官方前的卡表"
                 % (GITREL, out.stderr.decode("utf-8", "replace").strip()[:200]))
    import json
    d = json.loads(out.stdout.decode("utf-8"))
    return {c["id"]: (c["name"], c.get("nameZh") or "") for c in d["cards"]}


def main():
    argv = sys.argv[1:]
    old_path = None
    if "--old" in argv:
        old_path = argv[argv.index("--old") + 1]
    old = load_cards(old_path) if old_path else load_head()
    new = load_cards(NEWSRC)

    # 被换掉的 = 旧值有、且与新值不同（**只按卡名取值**，两张卡同名同值时会合并，去重后仍是同一串）
    stale = {}
    for cid, (nm, oz) in old.items():
        nz = new.get(cid, (nm, ""))[1]
        if not oz or oz == nz:
            continue
        if len(oz) < 2 or "{" in oz or '"' in oz:
            continue
        stale.setdefault(oz, (nz, []))[1].append(cid)
    print("被换掉且**值不重复**的旧卡名：%d 个（覆盖 %d 张卡）"
          % (len(stale), sum(len(v[1]) for v in stale.values())))
    if not stale:
        sys.exit("⚠️ 没有「被换掉」的名字 ⇒ 说明 OLD 与 NEW 是同一份（多半是已经提交过了）。"
                 "判据失据，**本脚本这次什么都没证明**。")

    files = []
    for dirpath, _dn, filenames in os.walk(ROOT):
        for fn in filenames:
            if fn.endswith(".cs"):
                files.append(os.path.join(dirpath, fn))
    print("扫了 %d 个 `.cs`（%s 下全部）" % (len(files), ROOT))

    hits, n_quoted = {}, 0
    for f in files:
        try:
            lines = io.open(f, encoding="utf-8", errors="replace").read().splitlines()
        except Exception:
            continue
        for i, ln in enumerate(lines, 1):
            for oz, (nz, ids) in stale.items():
                lit = '"' + oz + '"'
                if lit not in ln:
                    continue
                n_quoted += 1
                pos = ln.index(lit)
                cpos = ln.find("//")
                if cpos >= 0 and cpos < pos and ln[:cpos].count('"') % 2 == 0:
                    kind = "注释"
                elif EXPECT.search(ln):
                    kind = "期望值?"
                else:
                    kind = "其他"
                hits.setdefault(f, []).append((i, kind, oz, nz, ids[:3], ln.strip()[:150]))
    for f in sorted(hits):
        print("\n==", f.replace(ROOT, "").lstrip("/\\"))
        for i, kind, oz, nz, ids, ln in hits[f]:
            print("   %-5d [%-6s] 「%s」→「%s」  (%s)" % (i, kind, oz, nz, ",".join(ids)))
            print("         %s" % ln)
    print("\n共 %d 个文件命中 · 引号字面量 %d 处" % (len(hits), n_quoted))
    print("口径：只匹配**引号包裹**的字面量 · 名长 ≥2 · 逐行判「注释 / 期望值? / 其他」"
          "（⚠️ 「期望值?」只是**行内有断言口或等值比较**，要人开那一行看它比的是不是这个字面量）")


if __name__ == "__main__":
    main()
