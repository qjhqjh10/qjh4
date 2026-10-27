#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_battle_bgm.py — 把**战斗 BGM**（每场战场自带的三条强度分轨）搬进 Unity 工程。

判据（**亲读**，不是按文件名猜）→ `资料/普查产出_1016战斗/T06d_战斗BGM判据.md`：
  · 曲子**烘在战场场景里**：`<arena>.unity` → GO `Battle Music Controller`（挂 `BattleMusicManager`）
    → 三个子 GO `Music Low/Medium/High Intensity`，各带一个 `AudioSource(playOnAwake, loop)`。
  · 🔴 **clip 存在 `AudioSource.m_Resource`（`{_FileID:3, _PathID:NN}`），`m_audioClip` 13 场恒 0**
    —— 只读 `m_audioClip` 会得出「BGM 没有 clip」的**假结论**（本仓已知的坑）。
  · `useMultipleTracks == 0` ⇒ 原版 `Initialize()` 直接 `enabled = false` ⇒ **不切档**（单曲从头放到尾）。
  · 阈值：`h = min(双方督军当前生命)`；`h > midHealthChange(18)` → Low · `10 < h <= 18` → Medium ·
    `h <= highHealthChange(10)` → High。⚠️ **13 场序列化一律 18 / 10**（C# 初值 20 不是落地值）。
  · ⚠️ **Astra 是异常档**（`useMultipleTracks = 1` 但只有一首曲子）⇒ ⛔ 别按「三首 ogg ⇔ 多轨」推。

产物两块：
  ① **音频**：`MyGame/Assets/CardPresentation/Resources/Art/audio/bgm/<clip 名>.ogg`
     （`Resources/Art/` 整个被 `.gitignore` 排除 —— 与美术/语音/音效同一条规矩）
  ② **表**：`MyGame/Assets/CardPresentation/Resources/battle_bgm.json`（进仓库）

用法：
    PYTHONIOENCODING=utf-8 python -I -X utf8 工具/import_battle_bgm.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python -I -X utf8 工具/import_battle_bgm.py           # 拷音频 + 写表
"""
import argparse
import io
import json
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")
import UnityPy                                                     # noqa: E402

AA = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
UNPACK = "d:/2/新解包资源/assets_full"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/bgm"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/battle_bgm.json"

# 我们工程里那 13 个战场文件夹名（= bundle 名的后缀），逐场一档
ARENAS = ["battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari",
          "battlearenaastramilitarum", "battlearenablacklegion", "battlearenadarkangels",
          "battlearenaemperorschildren", "battlearenagenestealers", "battlearenaleviathan",
          "battlearenasororitas", "battlearenaspacewolves", "battlearenatauviorla"]


def resolve_ptr(env, owner, ptr):
    """把 `owner` 上的一个 PPtr 解成对象。→ (对象, 解法说明)。

    🔴 **必须带 `m_FileID`**：同一个 bundle 里**同 pid 可以是不同对象**
    （arena1 实测：`pid 32` 既是 `AudioClip 'Warpforge Ultramarines Combat High'`、
    又是 `GameObject 'Cardback Shadow SDF'`）⇒ ⛔ **别只按 pid 找**（本仓反复踩过的那条）。
    先按「`m_FileID` → 该文件 externals 的 CAB 名 == 对象的 `assets_file.name`」精确解；
    解不出才退一档「同 pid 里类型对得上的那个」，并把退化事实写进报告的 `resolvedBy`。
    """
    fid = (ptr or {}).get("m_FileID", 0)
    pid = (ptr or {}).get("m_PathID", 0)
    if not pid:
        return None, "空 PPtr"
    sf = owner.assets_file
    cab = None
    exts = getattr(sf, "externals", []) or []
    if fid and 1 <= fid <= len(exts):
        e = exts[fid - 1]
        cab = e.get("name") if isinstance(e, dict) else getattr(e, "name", None)
    if cab:
        for o in env.objects:
            if o.path_id == pid and getattr(o.assets_file, "name", None) == cab:
                return o, f"按 CAB `{cab}` 精确解"
    for o in env.objects:
        if o.path_id == pid and o.type.name == "AudioClip":
            return o, f"⚠️ 退化：按「同 pid + 类型 AudioClip」解（CAB 名 `{cab}` 对不上任何对象）"
    return None, f"解不出（fileID={fid} cab={cab} pathID={pid}）"


def read_arena(arena):
    """→ dict（从 shipped bundle **实读**）：三条轨各自的 clip 名 + `useMultipleTracks` + 两个阈值。"""
    bundle = os.path.join(AA, f"scenes_scenes_{arena}.bundle")
    if not os.path.exists(bundle):
        return {"arena": arena, "error": f"没有这个 bundle：{os.path.basename(bundle)}"}
    env = UnityPy.load(bundle)
    by_id = {}
    for o in env.objects:
        by_id.setdefault(o.path_id, o)

    mgr, mname = None, None
    for o in env.objects:
        if o.type.name != "MonoBehaviour":
            continue
        try:
            d = o.read_typetree()
        except Exception:                                          # noqa: BLE001
            continue
        if "useMultipleTracks" in d:
            mgr, mname = d, o.path_id
            break
    if mgr is None:
        return {"arena": arena, "error": "这一束里没有带 `useMultipleTracks` 的 MonoBehaviour"}

    # 三条轨：扫所有 AudioSource，按 `m_Resource` 解 clip（⚠️ 不是 `m_audioClip`）
    tracks = {}
    notes = set()
    for o in env.objects:
        if o.type.name != "AudioSource":
            continue
        d = o.read_typetree()
        res = d.get("m_Resource") or {}
        src, how = resolve_ptr(env, o, res)
        if src is not None:
            notes.add(how)
        clip = None
        if src is not None:
            try:
                clip = src.read_typetree().get("m_Name")
            except Exception:                                      # noqa: BLE001
                clip = None
        gid = (d.get("m_GameObject") or {}).get("m_PathID")
        go, _how2 = resolve_ptr(env, o, d.get("m_GameObject") or {})
        gname = None
        if go is not None and go.type.name == "GameObject":
            try:
                gname = go.read_typetree().get("m_Name")
            except Exception:                                      # noqa: BLE001
                gname = None
        if gname is None and gid:                                  # 再退一档：同 pid 里找 GameObject
            for o2 in env.objects:
                if o2.path_id == gid and o2.type.name == "GameObject":
                    try:
                        gname = o2.read_typetree().get("m_Name")
                    except Exception:                              # noqa: BLE001
                        gname = None
                    break
        if (res or {}).get("m_PathID"):
            tracks[gname] = {"audioSourcePid": o.path_id, "resourcePathId": res.get("m_PathID"),
                             "clip": clip, "how": how}

    def pick(kind):
        for gname, t in tracks.items():
            if gname and kind.lower() in gname.lower():
                return t["clip"]
        return None

    return {
        "arena": arena,
        "musicManagerPid": mname,
        "useMultipleTracks": int(mgr.get("useMultipleTracks", 0)),
        "midHealthChange": mgr.get("midHealthChange", 20.0),
        "highHealthChange": mgr.get("highHealthChange", 10.0),
        "timeToChange": mgr.get("timeToChange", 5.0),
        "timeToCheck": mgr.get("timeToCheck", 5.0),
        "tracks": tracks,
        "resolvedBy": sorted(notes),
        "low": pick("low"), "medium": pick("medium"), "high": pick("high"),
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    # 解包目录里的 ogg（搬运用这个，名字 = clip 名）
    files = {}
    out, need = [], []
    for arena in ARENAS:
        r = read_arena(arena)
        if "error" in r:
            print(f"❌ {arena}: {r['error']}")
            out.append(r)
            continue
        clipdir = os.path.join(UNPACK, f"bundle_scenes_scenes_{arena}", "AudioClip")
        if os.path.isdir(clipdir):
            for f in os.listdir(clipdir):
                stem, ext = os.path.splitext(f)
                files.setdefault(stem.strip(), os.path.join(clipdir, f))
        # 三轨回填：解不出就用「解包目录里那几首」兜（并如实标 why）
        clips = [r.get("low"), r.get("medium"), r.get("high")]
        if not any(clips):
            here = sorted({os.path.splitext(f)[0].strip()
                           for f in (os.listdir(clipdir) if os.path.isdir(clipdir) else [])})
            if len(here) == 1:
                r["low"] = r["medium"] = r["high"] = here[0]
            r["tracksResolvedBy"] = "文件名兜底（`m_Resource` 没解出）"
        for k in ("low", "medium", "high"):
            if r.get(k):
                need.append(r[k])
        out.append(r)
        print(f"{arena:<28} 多轨={r['useMultipleTracks']}  阈值={r['midHealthChange']}/{r['highHealthChange']}"
              f"  L={r.get('low')!r}\n{'':<28} M={r.get('medium')!r}  H={r.get('high')!r}")

    uniq = sorted(set(need))
    have = set()
    if os.path.isdir(DST_AUDIO):
        have = {os.path.splitext(f)[0].strip() for f in os.listdir(DST_AUDIO)}
    print(f"\n要用到 {len(uniq)} 首曲子；本地目标目录已有 {sum(1 for c in uniq if c in have)} 首"
          f" ⇒ 要拷 {sum(1 for c in uniq if c not in have)} 首")
    missing = [c for c in uniq if c not in files]
    if missing:
        print("⚠️ 解包目录里找不到这几首（名字对不上？）：" + ", ".join(missing))

    copied = skipped = 0
    if not args.check:
        os.makedirs(DST_AUDIO, exist_ok=True)
        for c in uniq:
            src = files.get(c)
            if src is None:
                continue
            ext = os.path.splitext(src)[1].lower()
            dst = os.path.join(DST_AUDIO, c + ext)
            if os.path.exists(dst) and os.path.getsize(dst) == os.path.getsize(src):
                skipped += 1
                continue
            shutil.copy2(src, dst)
            copied += 1

    doc = {
        "version": 1,
        "note": "**战斗 BGM**：原版**不是**一张全局歌单 —— 曲子烘在**每个战场场景**里，"
                "三个子 GO（`Music Low/Medium/High Intensity`）各带一个 `AudioSource`；"
                "`useMultipleTracks == 0` ⇒ 那个战场**不切档**（单曲）。"
                "🔴 clip 名取自 `AudioSource.m_Resource`（原版 `m_audioClip` 恒 0）。"
                "三档阈值 = `min(双方督军当前生命)`：`> midHealthChange` → low · `<= midHealthChange` 且 "
                "`> highHealthChange` → medium · `<= highHealthChange` → high。"
                "文件名在 `Resources/Art/audio/bgm/<clip 名>.ogg`（`Resources/Art/` 是 .gitignore 的）。",
        "source": {
            "where": "每个战场的 `scenes_scenes_<arena>.bundle` 里（`Battle Music Controller` → 3 个 AudioSource）",
            "report": "资料/普查产出_1016战斗/T06d_战斗BGM判据.md",
            "script": "工具/import_battle_bgm.py",
        },
        "arenas": out,
    }
    if not args.check:
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(doc, ensure_ascii=False, indent=1))
        print(f"已拷 {copied} 首 · 已存在跳过 {skipped} 首 → {DST_AUDIO}")
        print(f"表已写：{DST_JSON}")
    else:
        # 🔴 2026-10-27（执行代理 B9_W35CHECK）：`--check` 必须**连产物一起查** ——
        #   原来这里只打「（--check：没有写盘）」；`have` 那行虽算了「要拷 N 首」却不点名、不报红。
        #   判据同 工具/import_original_art.py `--only-altart`（`:1714-1721`）。
        lack_json = None
        if not os.path.exists(DST_JSON):
            lack_json = f"{DST_JSON} 不存在"
        else:
            old = io.open(DST_JSON, encoding="utf-8", newline="").read()
            if old != json.dumps(doc, ensure_ascii=False, indent=1):
                lack_json = f"{DST_JSON} 已过期（与现生成不一致）"
        lack_files = []
        for c in uniq:
            src = files.get(c)
            if src is None:
                continue          # 源里就没有 —— 上面已单独报过
            ext = os.path.splitext(src)[1].lower()
            dst = os.path.join(DST_AUDIO, c + ext)
            if not os.path.exists(dst):
                lack_files.append(c + ext)
            elif os.path.getsize(dst) != os.path.getsize(src):
                lack_files.append(c + ext + "（尺寸不符）")
        if lack_json:
            print("🔴 产物缺失/过期：" + lack_json)
        if lack_files:
            print(f"🔴 音频产物缺失 {len(lack_files)}/{len(uniq)} 首（{DST_AUDIO}）—— "
                  f"重跑不带 --check 落盘：")
            for b in lack_files[:50]:
                print("   缺:", b)
            if len(lack_files) > 50:
                print(f"   …共 {len(lack_files)} 首")
        if not lack_json and not lack_files:
            print(f"✅ --check：产物齐全（表 {DST_JSON} · 音频 {len(uniq)} 首）")
        print("（--check：没有写盘）")
        return 0 if (not lack_json and not lack_files) else 1


if __name__ == "__main__":
    sys.exit(main())
