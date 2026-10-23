#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_camera_intro_clips.py — 战斗线 `T12 演出镜头时序` 的**曲线表生成器**。

输入（只读）：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/`
  · `Animation/*.json`      —— 原版挂在 `Cinemachine Vcam` 上的 `Animation` 组件
  · `AnimationClip/*.json`  —— 13 场各一条 `<场名> Camera Intro` **legacy 曲线** clip
输出：
  · `d:/4/Unity/数据/游戏数据/camera_intro_clips.json`（全量件）
  · `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json`（运行期副本）

## 判据链（逐条，2026-10-20 现核）
- 原版入口：`BattleManager.OnInitializationComplete` → `StartBattleSequence` 第 0 步
  `LoadingMenu.LoadingFade(false,null)` → `cameraAnim.Play()`（**无参**，播 `Animation.clip`）。
  证据 → `资料/普查产出_1020/W58_战斗线_T12判据.md` §1·2。
- 选 clip 的规则（**不是按 `m_Name` 找**）：在某一场的 `Animation/` 里取
  **`m_PlayAutomatically == false` 且 `m_Animation` 指向 `m_Legacy == true` 的非空 clip** 的那一份。
  为什么不能按名字：spacewolves 与 emperorschildren 的 clip 名**都叫** `Battle Arena Emperors Children Camera Intro`
  （⚠️ 2026-10-20 现核：调查报告里「sororitas 也同名」**不成立** —— sororitas 的名字是好的），
  leviathan 叫 `Battle Arena Leviathan`（无 `Camera Intro` 后缀），genestealers 是 `Genestaler Cults`（少个 e）。
  darkangels 另有 `Animation_1661`（`m_PlayAutomatically=false` 但 `m_Animation = 0`）—— 按上面规则跳过。
- 13 条全是 legacy 曲线（`m_Legacy=true` · `m_MuscleClipSize=0` · 无 streamed）⇒ 曲线数据本地全有，
  运行期用 `AnimationClip.SetCurve` + `SampleAnimation` 重建（见 `Battle/CameraIntro.cs`）。

## 🔴 对调查报告的一处订正（铁律 5）
调查文件 §3·2 写着「原 JSON 的 `inSlope/outSlope` **全 0**、`inWeight/outWeight` **全是 1/3**，
与 `new Keyframe(t, v)` 的 Unity 默认值逐位一致 ⇒ 生成器只存 `(t, v)`」。
**现核不成立**：13 场里有 5 场的关键曲线**带着非零斜率**（2026-10-20 逐键读）：
  · `emperorschildren` / `sororitas` / `spacewolves` 的 position（2 键，`outSlope = 首键的割线斜率`）；
  · `genestealers` 的 position（同上，9.2048）；
  · `leviathan` 的 position（4 键，interior 斜率最大 12.50）；
  · `aeldari` 的 euler（3 键，中间键斜率 4.56 / −2.00）；
  · 4 条四元数旋转曲线（151 键）**全部**带平滑切线（最大 0.0333 ~ 0.0798）。
这几条恰恰是「2 键位置曲线」那一类：原版存的是**线性**（首键 outSlope = 末键 inSlope = 割线）——
若用 `new Keyframe(t,v)`（切线 0）重建，会变成 ease-in/out，**运镜手感不同**（而端点仍对得上 ⇒ 端点断言抓不到）。
⇒ 本生成器**把 `si`/`so`（inSlope/outSlope，逐分量）也存进 JSON**，`CameraIntro` 逐键写回
`new Keyframe(t, v, inTangent, outTangent)`。权重仍是 1/3 且 `weightedMode=0`（非加权）⇒ 不需要存权重。

## schema（`CameraIntro.cs` 里的 `[Serializable]` 类逐字对应）
```
{ "schema":1, "note":"…", "source":"…",
  "clips":[
    { "key":"battlearena1", "name":"Battle Arena 1 Camera Intro",
      "legacy":true, "sampleRate":60.0, "wrapMode":"Once", "length":3.5,
      "src":{ "pkg":"…", "animation":"Animation_1607.json", "clip":"AnimationClip_19.json", "pid":19 },
      "position":[ {"t":0.0,"v":[x,y,z],"si":[..],"so":[..]}, … ],
      "euler":[ … ],            // 与 `rotation` 二选一
      "rotation":[ {"t":…,"v":[x,y,z,w],"si":[..4],"so":[..4]}, … ],
      "flare":[ {"t":0.0,"v":[1.0],"si":[0.0],"so":[0.0]}, … ]   // 没有这条曲线就不写
    }, …
  ] }
```
`length` = **全部曲线最晚的键**（位置/旋转都在 2.5 s 到位，多出来的时间是光晕淡出）。

## 幂等
跑两遍逐字节相同（键按场名排序、无时间戳）。`--check` 只比对不写盘。
用法：
    python -I -X utf8 d:/4/Unity/工具/gen_camera_intro_clips.py
    python -I -X utf8 d:/4/Unity/工具/gen_camera_intro_clips.py --check
"""
import glob
import io
import json
import os
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    python -I -X utf8 d:/4/Unity/工具/gen_camera_intro_clips.py\n    python -I -X utf8 d:/4/Unity/工具/gen_camera_intro_clips.py --check\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

ASSETS = r"d:/2/新解包资源/assets_full"
DST_FULL = r"d:/4/Unity/数据/游戏数据/camera_intro_clips.json"
DST_RES = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json"

PKG_PREFIX = "bundle_scenes_scenes_battlearena"     # 13 个战场的包名前缀（⛔ 不是 `..._scenes_` 全扫）
SCENES_PREFIX = "bundle_scenes_scenes_"             # 场键 = 包名去掉这一段（`battlearena1` 这种）
ANIMATION_DIR = "Animation"
CLIP_DIR = "AnimationClip"


def _f(x):
    """JSON 里保持 float（Python 的 int/float 都会被 Unity 的 JsonUtility 读成 float）。"""
    return float(x)


def _vec(d, dim):
    # 位置/欧拉 = Vector3、四元数 = Quaternion 的分量表固定用 x/y/z(/w)；
    # 浮点曲线（flare）是**标量** ⇒ dim=1 直接取数。
    if dim == 1:
        return [_f(d)]
    return [_f(d[c]) for c in "xyzw"[:dim]]


def _keys(curve, dim):
    out = []
    for k in curve["m_Curve"]:
        out.append({
            "t": _f(k["time"]),
            "v": _vec(k["value"], dim),
            "si": _vec(k["inSlope"], dim),
            "so": _vec(k["outSlope"], dim),
        })
    return out


def _last_t(curves):
    t = 0.0
    for c in curves:
        for k in c:
            if k["t"] > t:
                t = k["t"]
    return t


def _read_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def pick_animation_component(pkg_dir):
    """按【规则】取运镜用的 `Animation` 组件，⛔ 不看 `m_Name`。

    判据：`m_PlayAutomatically == false` 且 `m_Animation` 指向一条 `m_Legacy == true`、
    `m_MuscleClipSize == 0`、至少有一条位置/欧拉/四元数曲线的 clip。
    （darkangels 的 `Animation_1661` 因为 `m_Animation = 0` 被跳过。）
    """
    cands = []
    for p in sorted(glob.glob(os.path.join(pkg_dir, ANIMATION_DIR, "*.json"))):
        d = _read_json(p)
        if d.get("m_PlayAutomatically"):
            continue
        pid = (d.get("m_Animation") or {}).get("m_PathID") or 0
        if not pid:
            continue
        cp = os.path.join(pkg_dir, CLIP_DIR, "AnimationClip_%d.json" % pid)
        if not os.path.exists(cp):
            print("[gen] ⚠ %s 指向 %d，但 %s 不存在" % (p, pid, cp))
            continue
        clip = _read_json(cp)
        if not clip.get("m_Legacy") or clip.get("m_MuscleClipSize", 0) != 0:
            continue
        if not (clip.get("m_PositionCurves") or clip.get("m_EulerCurves")
                or clip.get("m_RotationCurves")):
            continue
        cands.append((p, pid, cp, d, clip))
    if len(cands) != 1:
        names = [os.path.basename(c[0]) for c in cands]
        raise SystemExit("[gen] 🔴 %s 里「运镜 Animation」命中 %d 份（%s），预期恰好 1 份"
                         % (pkg_dir, len(cands), names))
    return cands[0]


def build_clip(arena_key, pkg_dir):
    ap, pid, cp, ad, clip = pick_animation_component(pkg_dir)

    out = {
        "key": arena_key,
        "name": clip.get("m_Name", ""),
        "legacy": bool(clip.get("m_Legacy")),
        "sampleRate": _f(clip.get("m_SampleRate", 60.0)),
        "wrapMode": "Once" if clip.get("m_WrapMode") == 1 else str(clip.get("m_WrapMode")),
        "length": 0.0,
        "src": {
            "pkg": os.path.basename(pkg_dir),
            "animation": os.path.basename(ap),
            "clip": os.path.basename(cp),
            "pid": int(pid),
        },
    }

    pos = clip.get("m_PositionCurves") or []
    eul = clip.get("m_EulerCurves") or []
    rot = clip.get("m_RotationCurves") or []
    flt = [c for c in (clip.get("m_FloatCurves") or [])
           if c.get("attribute") == "flareIntensityMultiplier"]
    if len(pos) != 1:
        raise SystemExit("[gen] 🔴 %s 的 position 曲线 %d 条（预期 1）" % (arena_key, len(pos)))
    if bool(eul) == bool(rot) or len(eul) + len(rot) != 1:
        raise SystemExit("[gen] 🔴 %s 的旋转曲线：euler=%d rotation=%d（预期恰好一种、各 1 条）"
                         % (arena_key, len(eul), len(rot)))
    if len(flt) > 1:
        raise SystemExit("[gen] 🔴 %s 的 flare 曲线 %d 条（预期 0 或 1）" % (arena_key, len(flt)))

    out["position"] = _keys(pos[0]["curve"], 3)
    if eul:
        out["euler"] = _keys(eul[0]["curve"], 3)
    else:
        out["rotation"] = _keys(rot[0]["curve"], 4)
    if flt:
        out["flare"] = _keys(flt[0]["curve"], 1)

    # `length` = 全部曲线最晚的键（位置/旋转都在 2.5 s 到位，多出来的时间是光晕淡出）
    curves = [out["position"]]
    if "euler" in out:
        curves.append(out["euler"])
    if "rotation" in out:
        curves.append(out["rotation"])
    if "flare" in out:
        curves.append(out["flare"])
    out["length"] = _last_t(curves)
    return out


def build_all():
    clips = []
    for pkg_dir in sorted(glob.glob(os.path.join(ASSETS, PKG_PREFIX + "*"))):
        if not os.path.isdir(pkg_dir):
            continue
        key = os.path.basename(pkg_dir)[len(SCENES_PREFIX):]
        clips.append(build_clip(key, pkg_dir))
    clips.sort(key=lambda c: c["key"])
    return {
        "schema": 1,
        "note": ("由 工具/gen_camera_intro_clips.py 生成。判据 = assets_full/bundle_scenes_scenes_battlearena*/"
                 "Animation + AnimationClip（legacy 曲线）。🔴 切线（si/so）必须逐键保留："
                 "2 键位置曲线与 4 条四元数曲线带非零 auto 切线，new Keyframe(t,v) 会把它变成 ease-in/out。"),
        "source": ASSETS,
        "clips": clips,
    }


def self_check(doc):
    """落盘前的独立核对（⛔ 不是拿实现自己证明自己：期望值从原 JSON 再读一遍）。"""
    clips = doc["clips"]
    assert len(clips) == 13, "clip 数 %d != 13" % len(clips)
    home = (0.0, 2.2220745, -13.5719795)
    for c in clips:
        assert c["legacy"] is True, c["key"]
        assert abs(c["sampleRate"] - 60.0) < 1e-6, c["key"]
        assert c["wrapMode"] == "Once", c["key"]
        first = c["position"][0]["v"]
        last = c["position"][-1]["v"]
        assert abs(last[0] - home[0]) < 1e-5 and abs(last[1] - home[1]) < 1e-5 \
            and abs(last[2] - home[2]) < 1e-5, "%s 终点不是本位: %s" % (c["key"], last)
        d0 = sum((first[i] - home[i]) ** 2 for i in range(3)) ** 0.5
        assert d0 > 1.0, "%s 起点离本位 %.3f（灭自证前提：必须 > 1）" % (c["key"], d0)
        if "euler" in c:
            assert c["euler"][-1]["v"] == [0.0, 0.0, 0.0], c["key"]
        else:
            assert c["rotation"][-1]["v"][3] == 1.0, c["key"]
        # 原版 euler 曲线的旋转序 = `m_RotationOrder: 4`（Unity `RotationOrder.OrderZXY`，
        # 与 `Quaternion.Euler` 同一序）⇒ `CameraIntro` 用 `Quaternion.Euler` 采样是正确的。
        # 曲线的 pre/post infinity = 2（Unity 序列化的 **Clamp**）⇒ 2.5 s 之后位置/旋转停在末键。
        pkg2 = os.path.join(ASSETS, SCENES_PREFIX + c["key"])
        _, _, _, _, raw2 = pick_animation_component(pkg2)
        for cv in (raw2.get("m_EulerCurves") or []):
            assert cv["curve"].get("m_RotationOrder") == 4, (c["key"], cv["curve"].get("m_RotationOrder"))
        for kind in ("m_PositionCurves", "m_EulerCurves", "m_RotationCurves", "m_FloatCurves"):
            for cv in (raw2.get(kind) or []):
                assert cv["curve"].get("m_PreInfinity") == 2 and cv["curve"].get("m_PostInfinity") == 2, \
                    (c["key"], kind, cv["curve"].get("m_PreInfinity"), cv["curve"].get("m_PostInfinity"))
        assert abs(c["length"] - max(k["t"] for cur in
                                     [c["position"]] + ([c["euler"]] if "euler" in c else [])
                                     + ([c["rotation"]] if "rotation" in c else [])
                                     + ([c["flare"]] if "flare" in c else [])
                                     for k in cur)) < 1e-6

    # ① leviathan：位置必须 4 键（含两段中继），长度 4.0（光晕末键）
    lv = next(c for c in clips if c["key"] == "battlearenaleviathan")
    assert len(lv["position"]) == 4, "leviathan 位置 %d 键" % len(lv["position"])
    assert abs(lv["length"] - 4.0) < 1e-6, "leviathan 长度 %s" % lv["length"]
    # ② emperorschildren：四元数 151 键、起点/终点精确
    ec = next(c for c in clips if c["key"] == "battlearenaemperorschildren")
    assert "rotation" in ec and len(ec["rotation"]) == 151, "EC 旋转键数"
    assert abs(ec["rotation"][0]["v"][0] - (-0.06259899586439133)) < 1e-9
    assert ec["rotation"][-1]["v"] == [0.0, 0.0, 0.0, 1.0]
    # ③ spacewolves 与 emperorschildren 的**原版资产名相同**（禁止按名字取）；
    #    ⚠️ 2026-10-20 现核订正：**sororitas 的名字是好的**（`Battle Arena Sororitas Camera Intro`），
    #    调查报告 §二 表里对、但它下面那句「sororitas / spacewolves 两份都叫 EC」**不成立**。
    so = next(c for c in clips if c["key"] == "battlearenasororitas")
    sw = next(c for c in clips if c["key"] == "battlearenaspacewolves")
    ec0 = next(c for c in clips if c["key"] == "battlearenaemperorschildren")
    assert so["name"] == "Battle Arena Sororitas Camera Intro", so["name"]
    assert sw["name"] == ec0["name"] == "Battle Arena Emperors Children Camera Intro", (sw["name"], ec0["name"])
    # ④ 有 flare 曲线的 7 场（含 darkangels —— 它 `lensFlare` PPtr=0，原版 Awake 直接 disable ⇒ 曲线**空转**），
    #    真正挂 flare 的只有 6 场（判据 → 本仓 ArenaPrefabs/*.prefab 的 `LensFlareComponentSRP` 分布）。
    with_flare = sorted(c["key"] for c in clips if "flare" in c)
    assert with_flare == ["battlearena1", "battlearena2", "battlearenaaeldari",
                          "battlearenaastramilitarum", "battlearenadarkangels",
                          "battlearenaleviathan", "battlearenatauviorla"], with_flare
    # ⑤ 切线订正：至少这几个对象上确实存在非零斜率（⛔ 别把报告里「全 0」照抄回来）
    def max_slope(c, kind):
        return max(abs(x) for k in c[kind] for key in ("si", "so") for x in k[key])
    assert max_slope(ec, "position") > 5.0
    assert max_slope(lv, "position") > 12.0
    assert max_slope(ec, "rotation") > 0.03
    ae = next(c for c in clips if c["key"] == "battlearenaaeldari")
    assert max_slope(ae, "euler") > 4.0

    # ⑥ 三场抽核（逐值对照原 JSON 再读一遍）
    print("[gen] 抽核 3 场（对照原始 bundle 现读）")
    for key in ("battlearenaleviathan", "battlearenaemperorschildren", "battlearenasororitas"):
        c = next(x for x in clips if x["key"] == key)
        pkg = os.path.join(ASSETS, SCENES_PREFIX + key)
        ap, pid, cp, ad, raw = pick_animation_component(pkg)
        rawpos = raw["m_PositionCurves"][0]["curve"]["m_Curve"]
        assert len(rawpos) == len(c["position"])
        for a, b in zip(rawpos, c["position"]):
            assert abs(a["time"] - b["t"]) < 1e-12
            for i in range(3):
                assert abs(a["value"][c_axis(i)] - b["v"][i]) < 1e-12
        if "rotation" in c:
            rawrot = raw["m_RotationCurves"][0]["curve"]["m_Curve"]
            assert len(rawrot) == len(c["rotation"]) == 151
            for i in range(4):
                assert abs(rawrot[0]["value"][c_axis(i)] - c["rotation"][0]["v"][i]) < 1e-12
        print("  · %-28s %-42s keys pos=%d %s=%d flare=%s len=%s"
              % (key, c["name"], len(c["position"]),
                 "rotation" if "rotation" in c else "euler",
                 len(c.get("rotation", c.get("euler"))),
                 len(c.get("flare", [])), c["length"]))
    print("[gen] 自检通过：13 场 · legacy/60fps/Once · 终点本位 · 起点距本位 >1 · 切线非零已保留")


def c_axis(i):
    return "xyzw"[i]


def render(doc):
    return json.dumps(doc, ensure_ascii=False, indent=1, sort_keys=False) + "\n"


def main(argv):
    check = "--check" in argv
    doc = build_all()
    self_check(doc)
    txt = render(doc)
    if check:
        for p in (DST_FULL, DST_RES):
            old = io.open(p, "r", encoding="utf-8").read() if os.path.exists(p) else None
            if old != txt:
                print("[gen] ✗ 与 %s 不一致" % p)
                return 1
        print("[gen] ✅ --check：两份产物与现生成一致")
        return 0
    for p in (DST_FULL, DST_RES):
        d = os.path.dirname(p)
        if not os.path.isdir(d):
            os.makedirs(d)
        # 新文件一律 LF（CLAUDE.md：别让脚本翻行尾；新文件随仓库习惯，仓库正文是 LF）
        with io.open(p, "w", encoding="utf-8", newline="\n") as f:
            f.write(txt)
        print("[gen] 写出 %s（%d 字节）" % (p, len(txt.encode("utf-8"))))
    print("[gen] 场数 = %d · 长度集合 = %s"
          % (len(doc["clips"]), sorted(set(round(c["length"], 4) for c in doc["clips"]))))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
