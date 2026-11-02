#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_audio_mixer_fixers.py — 从原版解包资源**现读**出 80 个 `AudioSourceMixerGroupFixer` 实例，
生成 `Assets/CardPresentation/Resources/audio_mixer_fixers.json`（`B120-W11` 的数据表）。

判据（原版侧，只读）：
  · 类 = `AudioSourceMixerGroupFixer`（`d:/2/tools/il2cpp_out/dump.cs:17479-17497`），
    pathID **7753607692688777346**（`_tmp_view/apk135/monoscripts.json` 现读）。
  · 13 个 `bundle_scenes_scenes_battlearena*` 各 **6** 个实例（3 聊天源 + 3 条 BGM 轨）；
  · `bundle_staticgeneralassets_assets_all` 的 `MusicManager` 上 **2** 个实例（都是 `mixerType = 1 = Music`）。
  ⇒ 78 + 2 = **80**。
  · 实例的 `audioSource` / `m_GameObject` / `mixerType` 都在 MonoBehaviour JSON 里；
    对象路径由 `Transform`/`RectTransform` 的 `m_Father` 链现场拼出。

⚠️ 写盘类脚本：**不带显式动作参数时只打印用法并退出（rc=2）**。
   `--write` 重生成 · `--check` 只比对（不写盘）· `--dry` 只打印预览。

用法：
  python -I -X utf8 d:/4/Unity/工具/gen_audio_mixer_fixers.py --write
"""
import io
import json
import os
import re
import sys

ASSETS = r"d:/4/Unity/MyGame/Assets"
OUT = os.path.join(ASSETS, "CardPresentation", "Resources", "audio_mixer_fixers.json")
ROOT = r"d:/2/新解包资源/assets_full"

FIXER_PID = 7753607692688777346
MIXER_NAMES = {0: "FX", 1: "Music", 2: "Voices", 3: "Jingles",
               4: "BattleGroupLow", 5: "BattleGroupMedium", 6: "BattleGroupHigh"}


def load(p):
    with io.open(p, 'r', encoding='utf-8') as f:
        return json.load(f)


def pid_of(fn):
    m = re.search(r'_(-?\d+)\.json$', fn)
    return int(m.group(1)) if m else None


def index_dir(d):
    out = {}
    if not os.path.isdir(d):
        return out
    for fn in os.listdir(d):
        if fn.endswith(".json"):
            pid = pid_of(fn)
            if pid is not None:
                out.setdefault(pid, os.path.join(d, fn))
    return out


def xform_path(bundle, go_pid):
    trs = index_dir(os.path.join(bundle, "Transform"))
    rts = index_dir(os.path.join(bundle, "RectTransform"))
    found = None
    for pid, p in list(trs.items()) + list(rts.items()):
        try:
            d = load(p)
        except Exception:
            continue
        if (d.get("m_GameObject") or {}).get("m_PathID") == go_pid:
            found = (pid, d)
            break
    if not found:
        return None
    trpid, trd = found
    gdir = os.path.join(bundle, "GameObject")
    names = {}

    def go_name(comp_pid):
        if comp_pid in names:
            return names[comp_pid]
        for fn in os.listdir(gdir):
            if not fn.endswith(".json"):
                continue
            try:
                g = load(os.path.join(gdir, fn))
            except Exception:
                continue
            for c in g.get("m_Component") or []:
                if (c.get("component") or {}).get("m_PathID") == comp_pid:
                    names[comp_pid] = g.get("m_Name")
                    return names[comp_pid]
        names[comp_pid] = "?"
        return "?"

    parts = [go_name(trpid)]
    cur = trd
    guard = 0
    while guard < 40:
        guard += 1
        father = (cur.get("m_Father") or {}).get("m_PathID")
        if not father:
            break
        fp = trs.get(father) or rts.get(father)
        if not fp:
            break
        cur = load(fp)
        parts.append(go_name(father))
    return "/".join(reversed(parts))


def arena_rows():
    out = []
    bundles = sorted(d for d in os.listdir(ROOT)
                     if d.startswith("bundle_scenes_scenes_battlearena"))
    for key in bundles:
        b = os.path.join(ROOT, key)
        arena = key.replace("bundle_scenes_scenes_", "")
        mbdir = os.path.join(b, "MonoBehaviour")
        rows = []
        for fn in os.listdir(mbdir):
            if not fn.endswith(".json"):
                continue
            try:
                d = load(os.path.join(mbdir, fn))
            except Exception:
                continue
            if (d.get("m_Script") or {}).get("m_PathID") != FIXER_PID:
                continue
            go = (d.get("m_GameObject") or {}).get("m_PathID")
            path = xform_path(b, go)
            if not path:
                raise SystemExit("路径解不出：%s / %s" % (arena, fn))
            rows.append({"path": path, "mixerType": int(d.get("mixerType"))})
        rows.sort(key=lambda r: (r["mixerType"], r["path"]))
        if len(rows) != 6:
            raise SystemExit("%s 期望 6 个实例，实得 %d" % (arena, len(rows)))
        out.append({"arena": arena, "rows": rows})
    return out


def music_rows():
    b = os.path.join(ROOT, "bundle_staticgeneralassets_assets_all")
    mbdir = os.path.join(b, "MonoBehaviour")
    rows = []
    for fn in os.listdir(mbdir):
        if not fn.endswith(".json"):
            continue
        try:
            d = load(os.path.join(mbdir, fn))
        except Exception:
            continue
        if (d.get("m_Script") or {}).get("m_PathID") != FIXER_PID:
            continue
        go = (d.get("m_GameObject") or {}).get("m_PathID")
        rows.append({
            "path": xform_path(b, go),
            "audioSource": (d.get("audioSource") or {}).get("m_PathID"),
            "mixerType": int(d.get("mixerType")),
        })
    rows.sort(key=lambda r: str(r["audioSource"]))
    if len(rows) != 2:
        raise SystemExit("MusicManager 期望 2 个实例，实得 %d" % len(rows))
    return rows


def build():
    arenas = arena_rows()
    music = music_rows()
    return {
        "_note": "AudioSourceMixerGroupFixer instances read from the shipped bundles (B120-W11). "
                 "Generated by gen_audio_mixer_fixers.py --write; do not hand-edit.",
        "_source": "dump.cs:17479-17497 (class) + bundle_scenes_scenes_battlearena* + bundle_staticgeneralassets_assets_all",
        "_mixerTypes": MIXER_NAMES,
        "arenas": arenas,
        "musicManager": music,
    }


def main(argv):
    if len(argv) != 2 or argv[1] not in ("--write", "--check", "--dry"):
        print(__doc__)
        print("用法：python -I -X utf8 gen_audio_mixer_fixers.py --write | --check | --dry")
        return 2
    data = build()
    total = sum(len(a["rows"]) for a in data["arenas"]) + len(data["musicManager"])
    print("arenas=%d rows=%d musicManager=%d total=%d"
          % (len(data["arenas"]), sum(len(a["rows"]) for a in data["arenas"]), len(data["musicManager"]), total))
    text = json.dumps(data, ensure_ascii=False, indent=1) + "\n"

    if argv[1] == "--dry":
        print(text[:1200])
        return 0
    if argv[1] == "--check":
        if not os.path.exists(OUT):
            print("MISSING " + OUT)
            return 1
        with io.open(OUT, 'r', encoding='utf-8') as f:
            cur = f.read()
        ok = cur == text
        print("IDENTICAL" if ok else "DIFFERS")
        return 0 if ok else 1
    # --write
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(text)
    print("wrote " + OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
