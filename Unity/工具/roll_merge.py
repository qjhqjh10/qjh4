# -*- coding: utf-8 -*-
"""滚动合并入口（调度台专用）：冲突预检 → 逐份合并 → 归档补丁 → 只读闸复核。

⛔ 不会做的事（故意的）：
  · 不跑 `check_ledger_points --fix`（那必须在「没有账写手 + 没有 .cs 写手」时手工做）
  · 不跑 Unity / 不碰 git / 不改主表之外的任何东西

用法（**不带参数 = 只打用法，什么都不做**）：
    python -I -X utf8 d:/4/Unity/工具/roll_merge.py --status
        只读：列出 `_parts/` 下未归档补丁按前缀分组 + 跑跨补丁冲突检查

    python -I -X utf8 d:/4/Unity/工具/roll_merge.py --apply B8_
        真合并：先冲突检查（有冲突就停，除非再加 --force），再 `merge_ledger_parts --only B8_`，
        然后把这些补丁移进 `_parts/_merged_B8_/`，最后跑 verify_annot / check_a_vs_diff 两个只读闸。

    python -I -X utf8 d:/4/Unity/工具/roll_merge.py --apply B8_ --force
        有跨补丁冲突时仍然合并（⚠️ 只在「你已经手工确认该谁赢」时用）

    --archive-only B8_     只归档（补丁已经合并过、只是没搬走）
"""
import io
import os
import shutil
import subprocess
import sys

TOOLS = r"d:/4/Unity/工具"
PARTS = r"d:/4/Unity/资料/完全复刻/账/_parts"
PY = sys.executable or "python"


def usage():
    print(__doc__)
    print("（本次什么都没做。）")


def run(*args, quiet=False):
    cmd = [PY, "-I", "-X", "utf8"] + list(args)
    p = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if not quiet:
        out = (p.stdout or "").strip()
        if out:
            print(out)
        err = (p.stderr or "").strip()
        if err:
            print(err)
    return p.returncode, (p.stdout or "") + (p.stderr or "")


def pending():
    if not os.path.isdir(PARTS):
        return {}
    groups = {}
    for f in sorted(os.listdir(PARTS)):
        if not f.endswith(".tsv"):
            continue
        pre = f.split("_")[0] + "_" if "_" in f else "(无前缀)"
        groups.setdefault(pre, []).append(f)
    return groups


def conflicts(prefix=None):
    rc, out = run(os.path.join(TOOLS, "check_patch_conflicts.py"), quiet=True)
    lines = [l for l in out.split("\n") if l.strip()]
    hits = []
    for l in lines:
        if prefix and prefix not in l:
            continue
        if "冲突" in l or ".tsv" in l:
            hits.append(l)
    return rc, out, hits


def archive(prefix):
    dst = os.path.join(PARTS, "_merged_%s" % prefix.rstrip("_") if prefix else "_merged_手工")
    os.makedirs(dst, exist_ok=True)
    moved = []
    for f in sorted(os.listdir(PARTS)):
        if f.endswith(".tsv") and (not prefix or f.startswith(prefix)):
            shutil.move(os.path.join(PARTS, f), os.path.join(dst, f))
            moved.append(f)
    return dst, moved


def gate():
    print("\n—— 只读闸复核 ——")
    for tool in ("verify_annot.py", "check_a_vs_diff.py", "check_loc_dupes.py"):
        rc, out = run(os.path.join(TOOLS, tool), quiet=True)
        tail = [l for l in out.strip().split("\n") if l.strip()]
        print("  %-22s rc=%d  %s" % (tool, rc, tail[0][:120] if tail else "(无输出)"))


def main():
    a = sys.argv[1:]
    if not a:
        usage()
        return 0

    if "--status" in a or "--list" in a:
        g = pending()
        if not g:
            print("`_parts/` 下没有未归档补丁。")
        for pre, fs in g.items():
            print("%-10s %d 份：%s" % (pre, len(fs), " ".join(fs)))
        print("\n—— 跨补丁冲突检查（只读）——")
        rc, out, hits = conflicts()
        body = [l for l in out.split("\n") if l.strip()]
        print("\n".join(body[-12:]) if body else "(无输出)")
        return 0

    if "--archive-only" in a:
        i = a.index("--archive-only")
        pre = a[i + 1] if i + 1 < len(a) else ""
        dst, moved = archive(pre)
        print("已归档 %d 份 → %s" % (len(moved), dst))
        return 0

    if "--apply" in a:
        i = a.index("--apply")
        if i + 1 >= len(a):
            print("!! --apply 后面要跟前缀（例：--apply B8_）")
            return 2
        pre = a[i + 1]
        force = "--force" in a

        rc, out, hits = conflicts(pre)
        print("—— 冲突预检 ——\n" + (out.strip()[-1500:] if out.strip() else "(无输出)"))
        if rc != 0 and not force:
            print("!! 冲突检查失败 / 有冲突 ⇒ 停手（确认后加 --force）")
            return 3

        print("\n—— 合并 %s ——" % pre)
        rc, out = run(os.path.join(TOOLS, "merge_ledger_parts.py"), "--only", pre)
        if rc != 0:
            print("!! 合并失败 rc=%d ⇒ 没归档" % rc)
            return rc

        dst, moved = archive(pre)
        print("\n已归档 %d 份 → %s" % (len(moved), dst))
        gate()
        print("\n⚠️ 行号漂移（若 .cs 有增删行）留到「无 .cs 写手」时手工："
              "python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix")
        return 0

    usage()
    return 0


if __name__ == "__main__":
    sys.exit(main())
