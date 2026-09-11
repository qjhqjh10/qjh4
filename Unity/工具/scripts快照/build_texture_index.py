# -*- coding: utf-8 -*-
"""build_texture_index.py — 建全局贴图索引 + 按场景补齐 `07_场景/<arena>/Texture2D/`。

背景（审计报告 A3）:
    15 个场景共 234 条贴图引用，只有 37 条（16%）的文件在场景自己的 Texture2D/ 里；
    其余 197 条散落在 08_预制体特效/共享资源、03_界面UI/去重资源、11_着色器/配套材质、
    12_主程序资源/内置资源，甚至别的场景目录（ring_warp）。本脚本：
      1) 全树扫描 png 建索引 `Warforge_tools/data/texture_index.json`（名字 -> [相对路径]）；
      2) 按审计产物里的逐场景引用清单，把缺失的贴图**复制**进各场景的 Texture2D/。

    **只复制，不移动、不删除**；目标已存在同名文件则不覆盖（内容不同则加来源后缀并告警）。

用法:
    py312/python.exe build_texture_index.py                  # 只建索引（默认）
    py312/python.exe build_texture_index.py --copy           # dry-run: 列出将复制哪些
    py312/python.exe build_texture_index.py --copy --apply   # 真正复制
    py312/python.exe build_texture_index.py --verify         # 校验每个场景是否已齐
选项:
    --refs <path>   逐场景引用清单 (默认 data/scene_tex_refs.json)
    --root <path>   解包根目录 (默认 d:/2/解包整理)
"""
import os
import sys
import json
import shutil
import hashlib
import collections

sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"d:/2/解包整理"
SCENES = os.path.join(ROOT, "07_场景")
DATA = r"d:/2/Warpforge_tools/data"
INDEX = os.path.join(DATA, "texture_index.json")
REFS = os.path.join(DATA, "scene_tex_refs.json")
REPORT = os.path.join(DATA, "texture_copy_report.json")

# 多候选且内容不一致时的裁决表（其余多候选若内容一致则按优先级取第一个）。
# ring2: 已用 UnityPy 核对 scenes_scenes_mainmenuwarpforge.bundle 的依赖
#        （AssetBundle.m_Dependencies 含 cab-6e07b2d1…→ duplicateassetisolation_assets_all.bundle，
#          不含 battleprefabs_vfxandmisc），故正确来源是 03_界面UI/去重资源 的 256x256 版本。
OVERRIDE = {
    "ring2": "03_界面UI/去重资源/Texture2D/ring2.png",
}

# 候选优先级（数字越小越优先）；未列出的目录排最后
PRIORITY = [
    "08_预制体特效/共享资源/",
    "03_界面UI/去重资源/",
    "08_预制体特效/战斗预制体/",
    "11_着色器/配套材质/",
    "12_主程序资源/内置资源/",
    "09_游戏数据/",
    "07_场景/",
]


def md5(path, bs=1 << 20):
    h = hashlib.md5()
    with open(path, "rb") as f:
        while True:
            b = f.read(bs)
            if not b:
                break
            h.update(b)
    return h.hexdigest()


def rank(rel):
    for i, p in enumerate(PRIORITY):
        if rel.startswith(p):
            return i
    return len(PRIORITY)


def build_index():
    """全树 png 索引: 名字(不含扩展名) -> [相对路径]。只读。"""
    idx = collections.defaultdict(list)
    n = 0
    for dp, dn, fn in os.walk(ROOT):
        for f in fn:
            if f.lower().endswith(".png"):
                rel = os.path.relpath(os.path.join(dp, f), ROOT).replace("\\", "/")
                idx[os.path.splitext(f)[0]].append(rel)
                n += 1
    for k in idx:
        idx[k].sort(key=lambda r: (rank(r), r))
    os.makedirs(DATA, exist_ok=True)
    with open(INDEX, "w", encoding="utf-8") as f:
        json.dump(dict(idx), f, ensure_ascii=False, indent=1)
    print("贴图索引: %d 个名字 / %d 个 png -> %s" % (len(idx), n, INDEX))
    return idx


def load_refs(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def choose(name, idx, ambiguous):
    """给一个贴图名挑源文件。返回 (relpath or None, 备注)。"""
    cands = idx.get(name) or []
    if not cands:
        # 大小写不敏感兜底
        low = name.lower()
        for k, v in idx.items():
            if k.lower() == low:
                cands = v
                break
    if not cands:
        return None, "全树找不到"
    if name in OVERRIDE:
        ov = OVERRIDE[name]
        return (ov, "裁决表指定") if ov in cands else (cands[0], "裁决表路径不在候选中,退回 %s" % cands[0])
    if len(cands) > 1:
        hashes = {}
        for c in cands:
            p = os.path.join(ROOT, c.replace("/", os.sep))
            try:
                hashes.setdefault(md5(p), []).append(c)
            except OSError:
                pass
        if len(hashes) > 1:
            ambiguous.append((name, {h: v for h, v in hashes.items()}))
            return cands[0], "⚠ 多候选且内容不一致(%d 种),取优先级最高的" % len(hashes)
        return cands[0], "多候选但内容一致(%d 份)" % len(cands)
    return cands[0], "唯一候选"


def do_verify(idx, refs):
    bad = 0
    total_need = total_local = 0
    for sc in sorted(refs):
        d = refs[sc]
        local = os.path.join(SCENES, sc, "Texture2D")
        have = set()
        if os.path.isdir(local):
            have = {n.lower() for n in os.listdir(local)}
        miss = [n for n in d["missing"] if (n + ".png").lower() not in have]
        total_need += d["need"]
        total_local += d["need"] - len(miss)
        flag = "OK " if not miss else "缺 %d" % len(miss)
        print("  [%s] %-30s 引用 %3d  本地有 %3d  %s" % (flag, sc, d["need"], d["need"] - len(miss), "" if not miss else miss[:6]))
        bad += len(miss)
    print("汇总: 引用 %d 条, 本地已覆盖 %d 条, 仍缺 %d 条" % (total_need, total_local, bad))
    return bad


def main():
    global ROOT, SCENES
    if "--root" in sys.argv:
        ROOT = sys.argv[sys.argv.index("--root") + 1]
        SCENES = os.path.join(ROOT, "07_场景")
    refs_path = sys.argv[sys.argv.index("--refs") + 1] if "--refs" in sys.argv else REFS
    if not os.path.exists(refs_path):
        alt = r"d:/2/Warpforge_tools/tmp/audit_0910/scene_tex.json"
        print("!! 找不到 %s，退回审计产物 %s" % (refs_path, alt))
        refs_path = alt
    apply_ = "--apply" in sys.argv
    do_copy = "--copy" in sys.argv
    verify_ = "--verify" in sys.argv

    print("=" * 78)
    print("build_texture_index.py  根=%s  引用清单=%s  %s"
          % (ROOT, refs_path, "[APPLY]" if apply_ else "[dry-run]"))
    print("=" * 78)

    idx = build_index()
    refs = load_refs(refs_path)

    # 把引用清单固化到 data/（不再依赖 tmp 审计目录）
    if os.path.abspath(refs_path) != os.path.abspath(REFS):
        os.makedirs(DATA, exist_ok=True)
        with open(REFS, "w", encoding="utf-8") as f:
            json.dump(refs, f, ensure_ascii=False, indent=1)
        print("引用清单已固化 -> %s" % REFS)

    if verify_:
        print("\n=== 校验 ===")
        return 0 if do_verify(idx, refs) == 0 else 1

    if not do_copy:
        return 0

    print("\n=== 补齐场景贴图 (只复制) ===")
    ambiguous = []
    report = {}
    n_copy = n_skip = n_fail = 0
    for sc in sorted(refs):
        d = refs[sc]
        local = os.path.join(SCENES, sc, "Texture2D")
        have = {n.lower() for n in os.listdir(local)} if os.path.isdir(local) else set()
        rows = []
        for name in d["missing"]:
            if (name + ".png").lower() in have:
                n_skip += 1
                continue
            src_rel, note = choose(name, idx, ambiguous)
            if src_rel is None:
                print("  [找不到] %-30s %s" % (sc, name))
                n_fail += 1
                continue
            src = os.path.join(ROOT, src_rel.replace("/", os.sep))
            dst = os.path.join(local, name + ".png")
            if os.path.exists(dst):
                # 目标同名但内容不同 -> 加来源后缀，绝不覆盖
                if md5(dst) != md5(src):
                    dst = os.path.join(local, "%s__%s.png" % (name, os.path.basename(os.path.dirname(os.path.dirname(src_rel)))))
                    note += " 目标同名不同内容,改写 %s" % os.path.basename(dst)
                else:
                    n_skip += 1
                    continue
            rows.append({"name": name, "src": src_rel, "dst": os.path.relpath(dst, ROOT).replace("\\", "/"), "note": note})
            if apply_:
                os.makedirs(local, exist_ok=True)   # 只在真要写时才建目录
                shutil.copy2(src, dst)
            n_copy += 1
            print("  %s %-62s <- %s   (%s)" % ("复制" if apply_ else "将复制",
                                               os.path.relpath(dst, ROOT).replace("\\", "/"), src_rel, note))
        if rows:
            report[sc] = rows
        print("  [%s] %d 张" % (sc, len(rows)))

    print("-" * 78)
    print("合计: %s %d 张, 跳过 %d, 失败 %d" % ("已复制" if apply_ else "将复制", n_copy, n_skip, n_fail))
    if ambiguous:
        print("!! 多候选内容不一致的贴图名 %d 个（已按优先级取用，需人工复核）:" % len(ambiguous))
        for name, hashes in ambiguous:
            print("   %s" % name)
            for h, ps in hashes.items():
                print("      %s  %s" % (h[:8], ps))
    if apply_:
        os.makedirs(DATA, exist_ok=True)
        with open(REPORT, "w", encoding="utf-8") as f:
            json.dump({"total_copied": n_copy, "skipped": n_skip, "failed": n_fail,
                       "ambiguous": [a[0] for a in ambiguous], "per_scene": report},
                      f, ensure_ascii=False, indent=1)
        print("复制报告 -> %s" % REPORT)
        print("复核: py312/python.exe build_texture_index.py --verify")
    else:
        print("dry-run 结束。加 --apply 真正复制。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
