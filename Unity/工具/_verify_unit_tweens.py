# -*- coding: utf-8 -*-
"""`UnitTweens.json` 的覆盖自检：**工程里真正被 `tweenAnims` 引用的名字，表里都得有**。

为什么要这个：
  `gen_unit_tweens.py` 把 `d:/2` 那份导出里的 74 个 `UnitTweenSO` 压成一张运行期表，
  但**工程里真正引用到的可能不是那 74 个的全部、也可能有表外漏网**。
  表少一个名字的后果是**静默的**：那条补间查不到 ⇒ 不播（本项目红线）。
  ⇒ 判据必须是「**引用侧**算出来的集合 ⊆ 表」，不是「我导出时看着差不多」。

判据（形状）：`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 里
  `kind: AnimFXModuleTween` 的模块，`keys` 与 `values` **一一对应**，
  `keys[i] == 'tweenAnims[N]'` ⇒ `values[i]` 就是那个资产名（形如 `@asset:MonoBehaviour:<名字>`）。

用法：`python d:/4/Unity/工具/_verify_unit_tweens.py`（只读，不写任何东西）
"""
import io
import json
import re
import sys

LIB = "d:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset"
TAB = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/UnitTweens.json"
PREFIX = "@asset:MonoBehaviour:"


def referenced():
    """从效果库里抽出所有被 `tweenAnims` 引用的资产名。"""
    lines = io.open(LIB, encoding="utf-8").read().splitlines()
    out, in_tween = set(), False
    mode = None          # 'keys' | 'values'
    keys, values = [], []
    for ln in lines:
        s = ln.strip()
        if s.startswith("- kind:"):
            in_tween = (s[len("- kind:"):].strip() == "AnimFXModuleTween")
            keys, values, mode = [], [], None
            continue
        if not in_tween:
            continue
        if s == "keys:":
            mode = "keys"; continue
        if s == "values:":
            mode = "values"; continue
        if mode == "keys" and s.startswith("- "):
            keys.append(s[2:].strip().strip("'"))
            continue
        if mode == "values" and s.startswith("- "):
            values.append(s[2:].strip().strip("'"))
            # keys/values 到齐就配一次对
            if len(values) >= len(keys):
                for k, v in zip(keys, values):
                    if re.match(r"^tweenAnims\[\d+\]$", k) and v.startswith(PREFIX):
                        out.add(v[len(PREFIX):])
                # 下一段（这个模块可能还有别的 keys，简单起见清掉继续）
                keys, values, mode = [], [], None
            continue
    return out


def main():
    ref = referenced()
    tab = json.load(io.open(TAB, encoding="utf-8"))
    have = {it["name"] for it in tab["items"]}
    miss = sorted(ref - have)
    unused = sorted(have - ref)

    print("① 效果库里被 `tweenAnims` 引用：**%d** 个名字" % len(ref))
    print("② 运行期表里：**%d** 个" % len(have))
    print("③ 引用了但表里没有：**%d** 个 %s" % (len(miss), miss[:10]))
    print("④ 表里有但没人引用：**%d** 个（不影响，只是白带）" % len(unused))
    bad = 0
    for it in tab["items"]:
        for t in it["tweens"]:
            if t.get("kind") == "unknown":
                bad += 1
    print("⑤ 表里认不出的补间类：**%d** 条" % bad)
    ok = (len(miss) == 0 and bad == 0)
    print("\n" + ("✅ 覆盖完整（引用侧 ⊆ 表）" if ok else "🔴 有缺口 —— 见 ③/⑤"))
    return 0 if ok else 1


sys.exit(main())
