#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_animfx_modules.py — 把 `animfx_components.json` 拍平成 Unity 读得进的形状

为什么要有这一趟
----------------
`animfx_components.json` 是**嵌套字典**（效果 → 组件 → 字段 → 可能是对象/列表），
`UnityEngine.JsonUtility` **解析不了字典、也解析不了多态**（它只认固定字段和数组）。
所以在这里把每个模块的字段**拍平成两条平行数组**：

    {"kind": "AnimFXModuleScreenShake",
     "keys":   ["actionStart", "cameraShakes[0].delay", "cameraShakes[0].amplitude", ...],
     "values": ["0",           "0.8",                    "2.0", ...]}

C# 侧 `WFModuleDef` 按**点号键**取值（`GetFloat("cameraShakes[0].delay")`）。
好处：**不需要在 python 与 C# 两边各写一份 schema** —— 每个模块类只要问自己要的字段名。

引用值的编码（`dump_animfx.py` 的 `ptr_ref` 产出 dict，这里压成字符串）
------------------------------------------------------------------
    node  → `@node:ParticleSystem:Root/Card#0/Glow`      （同 prefab 内部，按路径解析）
    asset → `@asset:UnitTweenSO:Impact Light Tween`      （外部资产，按类型+名字找）
    解析不出 → `""`（空串 = 没有值），另记进统计

产出：`数据/游戏数据/animfx_modules.json`

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_animfx_modules.py"
"""
import collections
import datetime
import io
import json
import os
import sys

ROOT = r"d:/4/Unity"
SRC = os.path.join(ROOT, "数据/游戏数据/animfx_components.json")
OUT = os.path.join(ROOT, "数据/游戏数据/animfx_modules.json")


def enc(v, out, prefix):
    """把值写进 keys/values 两条平行数组（点号键）"""
    if v is None:
        return
    if isinstance(v, bool):
        out[prefix] = "1" if v else "0"
    elif isinstance(v, (int,)):
        out[prefix] = str(v)
    elif isinstance(v, float):
        out[prefix] = repr(v)
    elif isinstance(v, str):
        if v.startswith("<深>"):
            return                      # 太深了，宁可不给，也别给错
        out[prefix] = v
    elif isinstance(v, dict):
        k = v.get("kind")
        if k == "node":
            out[prefix] = "@node:%s:%s" % (v.get("component", ""), v.get("path", ""))
        elif k == "asset":
            out[prefix] = "@asset:%s:%s" % (v.get("type", ""), v.get("name") or "")
        elif k == "unresolved":
            return                      # 空引用，不写
        else:
            for kk, vv in v.items():
                enc(vv, out, prefix + "." + kk if prefix else kk)
    elif isinstance(v, (list, tuple)):
        for i, vv in enumerate(v):
            enc(vv, out, "%s[%d]" % (prefix, i))
    # 其它类型（Vector3f 之类）走 vars()
    else:
        try:
            for kk, vv in vars(v).items():
                if kk.startswith("_"):
                    continue
                enc(vv, out, prefix + "." + kk if prefix else kk)
        except Exception:
            pass


def main():
    if not os.path.exists(SRC):
        print("没有 %s —— 先跑 工具/dump_animfx.py" % SRC)
        return 1
    doc = json.load(io.open(SRC, encoding="utf-8"))
    effects = doc.get("effects", {})

    outp = []
    stats = collections.Counter()
    kinds = collections.Counter()
    for name in sorted(effects):
        mods = []
        for m in effects[name]:
            cls = m.get("class")
            if not cls:
                continue
            flat = {}
            enc(m.get("fields", {}), flat, "")
            # 模块自己挂在哪个节点（`__node`）—— 运行时按它把组件挂回原位置，
            # 不是一律挂在根上（`TransformModifier` 这类要看自己所在的 Transform）。
            np = m.get("nodePath") or ""
            if np:
                flat["__node"] = np
            keys = sorted(flat)
            mods.append({"kind": cls, "keys": keys, "values": [flat[k] for k in keys]})
            kinds[cls] += 1
            for k in keys:
                if flat[k].startswith("@node:"):
                    stats["node"] += 1
                elif flat[k].startswith("@asset:"):
                    stats["asset"] += 1
        if mods:
            outp.append({"name": name, "modules": mods})

    out_doc = {
        "generated": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "source": "animfx_components.json（由 工具/dump_animfx.py 产出）",
        "note": "字段拍平成 keys/values 两条平行数组（点号键）；引用值编码见 gen_animfx_modules.py 文件头",
        "effect_count": len(outp),
        "module_count": sum(len(e["modules"]) for e in outp),
        "refs": dict(stats),
        "effects": outp,
    }
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(out_doc, ensure_ascii=False, indent=1))

    print("效果 %d 个 / 模块 %d 个 / 引用 node %d · asset %d"
          % (out_doc["effect_count"], out_doc["module_count"], stats["node"], stats["asset"]))
    print("模块分布：")
    for k, v in kinds.most_common():
        print("   %-34s %d" % (k, v))
    print("写出 %s (%.0f KB)" % (OUT, os.path.getsize(OUT) / 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
