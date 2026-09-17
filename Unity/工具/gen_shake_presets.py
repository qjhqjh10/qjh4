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

产出：`MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json`
（放 Resources 是因为运行时 `WFModuleScreenShake` 要 `Resources.Load<TextAsset>` ——
 与效果库同一个目录，`WarpforgeEffectLibrary.ResourcesPath` 就在那儿）

交叉验证：`Shake Hit Small` 应当与 `CardPresentation/Core/CardFeel.cs` 里已核过的常量一致
（sustainTime 0.1 / attackTime 0 / decayTime 0.3 / amplitude 2.0 / frequency 0.05）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_shake_presets.py"
"""
import datetime
import glob
import io
import json
import os
import sys

SRC_DIR = r"d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour"
OUT = r"d:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json"

FIELDS = ("delay", "sustainTime", "attackTime", "decayTime", "amplitude", "frequency")


def main():
    out = {}
    bad = []
    for p in sorted(glob.glob(os.path.join(SRC_DIR, "Shake *.json"))):
        try:
            d = json.load(io.open(p, encoding="utf-8"))
        except Exception as e:
            bad.append((os.path.basename(p), str(e)))
            continue
        name = d.get("m_Name")
        preset = d.get("preset") or {}
        if not name or not preset:
            bad.append((os.path.basename(p), "没有 m_Name 或 preset"))
            continue
        rec = {k: float(preset.get(k) or 0.0) for k in FIELDS}
        dr = preset.get("direction") or {}
        rec["dirX"] = float(dr.get("x") or 0.0)
        rec["dirY"] = float(dr.get("y") or 0.0)
        rec["dirZ"] = float(dr.get("z") or 0.0)
        # rawSignal（Beautify 的波形资产）我们不复刻 —— 记下有没有，别假装还原了
        rs = preset.get("rawSignal") or {}
        rec["hasRawSignal"] = 1 if (rs.get("m_PathID") or rs.get("m_FileID")) else 0
        rec["name"] = name
        out[name] = rec

    doc = {
        "generated": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "source": "d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json",
        "note": "原版 CameraShakePresetSO 的数值；⚠️ `rawSignal`（Beautify 波形）**没复刻**，"
                "hasRawSignal=1 的说明原版还叠了一层自定义波形，我们用的是正弦/噪声兜底。"
                "⚠️ `presets` 是**数组不是字典** —— UnityEngine.JsonUtility 解析不了字典（踩过）。"
                "⚠️ 另有 4 个 `Shake Target*.json` 是**另一个类**（没有 m_Name/preset），没收进来。",
        "count": len(out),
        "presets": [out[n] for n in sorted(out)],
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(json.dumps(doc, ensure_ascii=False, indent=1))

    print("抽出 %d 个 preset → %s" % (len(out), OUT))
    for n in sorted(out):
        r = out[n]
        print("   %-28s amp=%-6.3f freq=%-6.3f sus=%-5.3f atk=%-5.3f dec=%-5.3f dir=(%.2f,%.2f,%.2f)%s"
              % (n, r["amplitude"], r["frequency"], r["sustainTime"], r["attackTime"], r["decayTime"],
                 r["dirX"], r["dirY"], r["dirZ"], "  +rawSignal" if r["hasRawSignal"] else ""))
    if bad:
        print("读不出的 %d 个：" % len(bad))
        for b in bad:
            print("   ", b)
    # 交叉验证
    ref = out.get("Shake Hit Small")
    if ref:
        ok = (abs(ref["sustainTime"] - 0.1) < 1e-4 and abs(ref["decayTime"] - 0.3) < 1e-4
              and abs(ref["amplitude"] - 2.0) < 1e-4 and abs(ref["frequency"] - 0.05) < 1e-4)
        print("交叉验证 Shake Hit Small 与 CardFeel 常量：%s" % ("一致 OK" if ok else "**不一致，要查**"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
