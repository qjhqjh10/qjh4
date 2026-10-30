#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_shake_presets.py — 把原版 27 个屏震 preset 的数值抽成工程里的一个 json

为什么需要：`AnimFXModuleScreenShake` 的每条 `cameraShakes[i]` 都指向一个
`CameraShakePresetSO`（名字如 `Shake Hit Small`），而 `OverwriteCameraShakePreset` 的
6 个 `overwrite*` 开关是**逐项覆盖到那个 preset 的字段上**的 —— 不知道 preset 的原始数值，
覆盖就没意义（`overwriteX=0` 的项要用 preset 的值）。

数据源：`d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json`
（27 个，其中 14 个被效果引用）。字段形状：
    {"m_Name": "Shake Hit Small", "preset": {"delay","sustainTime","attackTime","decayTime",
                                              "amplitude","frequency","direction{x,y,z}","rawSignal"}}

🆕 **2026-10-23（R9 落地）**：`rawSignal`（原版 `Cinemachine.NoiseSettings` 自定义波形）
**不再只是「有没有」** —— 本脚本现在把被引用的信号 SO 的 **`PositionNoise` / `OrientationNoise`
各 3 octave × X/Y/Z × {Frequency, Amplitude, Constant}` 全表**一起抽出来，写进产出 json 的
`signals` 段，preset 上记信号名（空 = 走默认信号）。
- 判据（机器码级算式）→ `d:/4/Unity/资料/普查产出_1023b/R9_rawSignal_算式.md`
- 逐轴全表（人工核对版）→ `d:/4/Unity/资料/普查产出_1023b/特效线_执行报告.md` §二
- 🔴 **`Constant` 的语义**：`1` ⇒ `A×0.5×sin(2πfu)` 走**正弦**；`0` ⇒ `A×(Perlin(u,0)−0.5)` 走 **Perlin**。
  三条带**相加**。⛔ 别按「Constant = 不随机化 ⇒ 常数」去理解（那会得到一条平线）。

**pathID → 信号名**：`assets_full` 那批 JSON **不带 pathID**，所以名字从**旧解包**
`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/<名字>_<pathID>.json` 的文件名反查
（两边同名的件内容**逐位相同**，2026-10-23 亲比 `Warpforge 6D Shake One Axis`）。
⚠️ 那个索引目录缺失时**直接报错退出**，⛔ 不要猜名字（猜错 = 静默采错波形）。

**默认信号** = `Warpforge 6D Shake`（场景 `CinemachineImpulseSource.m_RawSignal`，
pathID `1276427422462347950`；出处见算式文档 §2·6）—— 没有 `rawSignal` 的 preset 落到它。

产出：`MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json`
（放 Resources 是因为运行时 `WFModuleScreenShake` 要 `Resources.Load<TextAsset>` ——
 与效果库同一个目录，`WarpforgeEffectLibrary.ResourcesPath` 就在那儿）
⚠️ **是生成产物，别手改**：改这里再重跑。

交叉验证：`Shake Hit Small` 应当与 `CardPresentation/Core/CardFeel.cs` 里已核过的常量一致
（sustainTime 0.1 / attackTime 0 / decayTime 0.3 / amplitude 2.0 / frequency 0.05）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_shake_presets.py"
  加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。
  加 `--check` = 只比对「现有产物 vs 生成器算出来的」逐字段一致（忽略 `generated` 时间戳），零写盘；不一致退出码 1。
"""
import datetime
import glob
import io
import json
import os
import re
import sys

# 🔴 `--dry` = 只跑计算、打印读数、**零写盘**。默认（不带它）行为与以前完全一样。
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_shake_presets.py"\n  加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。\n  加 `--check` = 只比对「现有产物 vs 生成器算出来的」逐字段一致（忽略 `generated` 时间戳），零写盘；不一致退出码 1。\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

DRY = "--dry" in sys.argv[1:]
# 🆕 2026-10-31（B104）：`--check` = 幂等闸。生成产物里带 `generated` 时间戳 ⇒ 「两次逐字节一致」永远不成立；
#   闸的正确口径是「除时间戳外逐字段一致」。默认行为（不带参）= 真写盘，保持不变。
CHECK = "--check" in sys.argv[1:]

SRC_DIR = r"d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour"
# 旧解包：文件名带 pathID ⇒ 唯一能把 PPtr 的 pathID 翻成信号 SO 名字的地方
IDX_DIR = r"d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour"
OUT = r"d:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json"

FIELDS = ("delay", "sustainTime", "attackTime", "decayTime", "amplitude", "frequency")

# 场景 `CinemachineImpulseSource`（battlearena1 `MonoBehaviour_4914.json`）的 `m_RawSignal`
DEFAULT_SIGNAL_PID = 1276427422462347950

PID_RE = re.compile(r"_(-?\d+)\.json$")


def load_name_by_pid():
    """旧解包的 `<名字>_<pathID>.json` 文件名 → {pathID: 名字}。"""
    m = {}
    for p in glob.glob(os.path.join(IDX_DIR, "*.json")):
        mm = PID_RE.search(os.path.basename(p))
        if mm:
            m[int(mm.group(1))] = os.path.basename(p)[:mm.start()]
    return m


def octave(o):
    """一条 octave（含 X/Y/Z 三轴）→ 拍平的一行（JsonUtility 只吃扁平字段/数组）。"""
    row = {}
    for ax, key in (("X", "x"), ("Y", "y"), ("Z", "z")):
        a = (o or {}).get(ax) or {}
        row["f" + key] = float(a.get("Frequency") or 0.0)
        row["a" + key] = float(a.get("Amplitude") or 0.0)
        row["c" + key] = int(a.get("Constant") or 0)
    return row


def read_signal(name):
    """读一份 `Cinemachine.NoiseSettings` SO → 本脚本的信号记录（全 3 octave × X/Y/Z）。"""
    p = os.path.join(SRC_DIR, name + ".json")
    d = json.load(io.open(p, encoding="utf-8"))
    pos = d.get("PositionNoise") or []
    ori = d.get("OrientationNoise") or []
    return {
        "name": name,
        "position": [octave(o) for o in pos],
        "orientation": [octave(o) for o in ori],
    }


def main():
    name_by_pid = load_name_by_pid()
    if not name_by_pid:
        print("**索引目录缺失或没有任何 `<名字>_<pathID>.json`**：%s" % IDX_DIR)
        print("   ⇒ `rawSignal` 的 pathID 翻不出名字，宁可不写也不猜。")
        return 2

    out = {}
    bad = []
    skipped = []
    used_pids = set()
    for p in sorted(glob.glob(os.path.join(SRC_DIR, "Shake *.json"))):
        try:
            d = json.load(io.open(p, encoding="utf-8"))
        except Exception as e:
            bad.append((os.path.basename(p), str(e)))
            continue
        name = d.get("m_Name")
        preset = d.get("preset") or {}
        if not name or not preset:
            # `Shake Target*.json` 是另一个类（4 个），**不是缺陷** —— 单独计数，⛔ 别混进 bad
            skipped.append((os.path.basename(p), "没有 m_Name 或 preset"))
            continue
        rec = {k: float(preset.get(k) or 0.0) for k in FIELDS}
        dr = preset.get("direction") or {}
        rec["dirX"] = float(dr.get("x") or 0.0)
        rec["dirY"] = float(dr.get("y") or 0.0)
        rec["dirZ"] = float(dr.get("z") or 0.0)
        # `rawSignal`：PPtr（`m_FileID:0` = 同包）⇒ pathID 翻名字。空 = 走默认信号。
        rs = preset.get("rawSignal") or {}
        pid = int(rs.get("m_PathID") or 0)
        rsname = ""
        if pid:
            used_pids.add(pid)
            rsname = name_by_pid.get(pid, "")
            if not rsname:
                bad.append((name, "rawSignal pathID %d 在索引目录里翻不出名字" % pid))
        rec["rawSignal"] = rsname
        rec["hasRawSignal"] = 1 if rsname else 0
        rec["name"] = name
        out[name] = rec

    if DEFAULT_SIGNAL_PID in name_by_pid:
        used_pids.add(DEFAULT_SIGNAL_PID)
    else:
        bad.append(("(默认信号)", "pathID %d 翻不出名字" % DEFAULT_SIGNAL_PID))
    default_signal = name_by_pid.get(DEFAULT_SIGNAL_PID, "")

    # 4 份 `Warpforge 6D Shake*` 全收（2 份没被 preset 引用，但同一族，收着不花钱）
    signals = []
    for p in sorted(glob.glob(os.path.join(SRC_DIR, "Warpforge 6D Shake*.json"))):
        signals.append(read_signal(os.path.splitext(os.path.basename(p))[0]))

    doc = {
        "generated": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "source": "d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json",
        "note": "原版 CameraShakePresetSO 的数值 + 它引用的 rawSignal 信号表。"
                "🔴 `rawSignal` 的采样算式已从 GameAssembly.dll 反汇编坐实（2026-10-23）："
                "每轴每带 u = Frequency×T；Constant=1 ⇒ A×0.5×sin(2πu)、Constant=0 ⇒ A×(PerlinNoise(u,0)−0.5)；"
                "三条带【相加】；T = t×preset.frequency + 起播偏移。"
                "判据 → 资料/普查产出_1023b/R9_rawSignal_算式.md。"
                "⚠️ 我们 2D 版只采 PositionNoise 的 Y 轴三条带（X/Z 与 Orientation 收着但不用）。"
                "⚠️ `presets`/`signals` 是**数组不是字典** —— UnityEngine.JsonUtility 解析不了字典（踩过）。"
                "⚠️ 另有 4 个 `Shake Target*.json` 是**另一个类**（没有 m_Name/preset），没收进来。",
        "count": len(out),
        "defaultSignal": default_signal,
        "signals": signals,
        "presets": [out[n] for n in sorted(out)],
    }
    if CHECK:
        if not os.path.exists(OUT):
            print("**`--check` 失败**：产物不存在 %s" % OUT)
            return 1
        try:
            prev = json.load(io.open(OUT, encoding="utf-8"))
        except Exception as e:
            print("**`--check` 失败**：现有产物读不出来 %s（%s）" % (OUT, e))
            return 1
        a = dict(doc); b = dict(prev)
        # 生成产物带 `generated` 时间戳 ⇒ 逐字节一致永远不成立；闸的正确口径 = 除时间戳外逐字段一致。
        a.pop("generated", None); b.pop("generated", None)
        if a != b:
            print("**`--check` 不一致**：%s 与生成器算出来的不是同一份" % OUT)
            return 1
        print("`--check` OK：%s 与生成器逐字段一致（忽略 `generated` 时间戳）；%d 个 preset / %d 份信号"
              % (OUT, len(out), len(signals)))
        return 0
    if DRY:
        print("干跑（未写盘）：%s（%d 个 preset / %d 份信号）；要落盘请去掉 --dry"
              % (OUT, len(out), len(signals)))
    else:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        io.open(OUT, "w", encoding="utf-8", newline="\n").write(json.dumps(doc, ensure_ascii=False, indent=1))
        print("抽出 %d 个 preset / %d 份信号 → %s" % (len(out), len(signals), OUT))
    for n in sorted(out):
        r = out[n]
        print("   %-28s amp=%-6.3f freq=%-6.3f sus=%-5.3f atk=%-5.3f dec=%-5.3f dir=(%.2f,%.2f,%.2f)%s"
              % (n, r["amplitude"], r["frequency"], r["sustainTime"], r["attackTime"], r["decayTime"],
                 r["dirX"], r["dirY"], r["dirZ"],
                 ("  rawSignal=" + r["rawSignal"]) if r["hasRawSignal"] else "  rawSignal=<默认>"))
    print("   默认信号 = %s（pathID %d）" % (default_signal or "**翻不出**", DEFAULT_SIGNAL_PID))
    for s in signals:
        y = " / ".join("f%.2f a%.3f c%d" % (o["fy"], o["ay"], o["cy"]) for o in s["position"])
        print("   信号 %-32s Position.Y: %s  (octave %d, Orientation %d)"
              % (s["name"], y, len(s["position"]), len(s["orientation"])))
    if skipped:
        print("按设计跳过 %d 个（另一个类，没有 m_Name/preset）：" % len(skipped))
        for b in skipped:
            print("   ", b[0])
    if bad:
        print("**读不出/解不开的 %d 个**：" % len(bad))
        for b in bad:
            print("   ", b)
        return 1
    # 交叉验证
    ref = out.get("Shake Hit Small")
    if ref:
        ok = (abs(ref["sustainTime"] - 0.1) < 1e-4 and abs(ref["decayTime"] - 0.3) < 1e-4
              and abs(ref["amplitude"] - 2.0) < 1e-4 and abs(ref["frequency"] - 0.05) < 1e-4)
        print("交叉验证 Shake Hit Small 与 CardFeel 常量：%s" % ("一致 OK" if ok else "**不一致，要查**"))
        if not ok:
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
