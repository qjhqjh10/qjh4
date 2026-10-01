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
# 🆕 2026-10-01：`效果名 \t 材质名` —— 只被**模块字段**引用、挂在渲染器上根本看不到的材质。
#   `EffectExporter.Export()` 读它，否则那 3 张卡材质永远导不进来（见文件尾那段）。
OUT_MATS = os.path.join(ROOT, "数据/游戏数据/module_materials.tsv")
# 🆕 2026-10-01：CardAnim 资产引用 → 工程里的 prefab 名。
#   为什么要这一趟：`AnimFXInstanceParticleAdjacent.cardAnim` 在原版是
#   `AssetReferenceTyped<CardAnim>`，落到数据里只剩一个 **`m_AssetGUID` 字符串**；
#   而那个 GUID 指的是**一份纯数据的 CardAnim MonoBehaviour**（不是 prefab、没有任何贴图网格），
#   它自己的 `animInfo.animAdressable.m_AssetGUID` 才指向真正要播的 GameObject。
#   两跳都在 `anim_address_map.json` 里（`cardanim_guid_to_name` → `cardanim_to_asset`）。
#   判据全文 → `资料/普查产出_1001/资产导入路三件_侦察.md` §②。
ADDR_MAP = os.path.join(ROOT, "数据/索引/anim_address_map.json")

# 只有这些模块**要** cardAnim → prefab 名（其余模块的 cardAnim 字段不动，别越权解释）
CARDANIM_MODULES = ("AnimFXInstanceParticleAdjacent",)


def load_cardanim_index():
    """→ (guid→prefabName, guid→为什么解不出)。表不在就返回 (None, None) 并出声。"""
    if not os.path.exists(ADDR_MAP):
        print("⚠️ 没有 %s —— 先跑 工具/gen_anim_address_map.py"
              "（否则 cardAnim 解不出 prefab 名）" % ADDR_MAP)
        return None, None
    doc = json.load(io.open(ADDR_MAP, encoding="utf-8"))
    by_guid = doc.get("cardanim_guid_to_name") or {}
    by_name = doc.get("cardanim_to_asset") or {}
    ok, why = {}, {}
    for guid, cname in by_guid.items():
        rec = by_name.get(cname) or {}
        tgt = rec.get("targetName")
        if tgt:
            ok[str(guid).lower()] = tgt
        else:
            why[str(guid).lower()] = "CardAnim「%s」的 animAdressable 解不出目标（%s）" % (
                cname, rec.get("unresolved") or "原因未记")
    return ok, why


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
    ca_ok, ca_why = load_cardanim_index()
    ca_miss = []                     # 解不出的（**出声，不静默**）
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
            # 🆕 2026-10-01：cardAnim 的**资产 GUID** → 工程里的 **prefab 名**。
            #   存进数据、运行时就不用再认识 Addressables 那套（见文件头 ADDR_MAP 那段）。
            if cls in CARDANIM_MODULES:
                g = str(flat.get("cardAnim.m_AssetGUID") or "").strip().lower()
                if g:
                    if ca_ok and g in ca_ok:
                        flat["prefabName"] = ca_ok[g]
                        stats["cardAnimResolved"] += 1
                    else:
                        stats["cardAnimUnresolved"] += 1
                        ca_miss.append({
                            "effect": name, "kind": cls, "guid": g,
                            "why": (ca_why or {}).get(g)
                                   or ("anim_address_map.json 里没有这个 GUID"
                                       if ca_ok is not None
                                       else "没读到 anim_address_map.json（先跑生成器）")})
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
        # 🆕 2026-10-01：cardAnim 解不出 prefab 名的哪些（**出声，不静默**）
        "cardAnimUnresolved": ca_miss,
        "effects": outp,
    }
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(out_doc, ensure_ascii=False, indent=1))

    # 🆕 2026-10-01：**效果 → 模块字段引用的材质名**，写成 TSV 给导出器读。
    #
    # 为什么单开一张表：`EffectExporter` 导出材质时**只走渲染器**
    # （`GetComponentsInChildren<Renderer>()`）⇒ 只被**模块字段**引用的材质根本不在遍历里。
    # 那 3 张（`Vanguard_Frame VAT Dissolve` / `Card 3d Dissolve Blend Image Ambush` / `Card 3d Stealth`）
    # 就是这么漏掉的。导出器读这张表就能把它们一并收进那个效果的 binder。
    # 格式：一行一条 `效果名 \t 材质名`（**一行一材质** —— 材质名里可能有空格，但不会有制表符）。
    mat_rows = []
    for e in outp:
        for m in e["modules"]:
            for v in m["values"]:
                if isinstance(v, str) and v.startswith("@asset:Material:"):
                    mat_rows.append((e["name"], v[len("@asset:Material:"):]))
    mat_rows = sorted(set(mat_rows))
    with io.open(OUT_MATS, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("# 效果名\t材质名  —— 由 工具/gen_animfx_modules.py 生成，别手改\n")
        fh.write("# 用途：`EffectExporter.Export()` 按这张表把**只被模块字段引用**的材质也导一遍\n")
        fh.write("# （原来只导挂在渲染器上的 ⇒ 这 3 张卡材质全部漏掉，见 资料/普查产出_1001/）\n")
        for a, b in mat_rows:
            fh.write("%s\t%s\n" % (a, b))
    print("模块引用的材质：%d 条（%d 个效果）→ %s"
          % (len(mat_rows), len(set(a for a, _ in mat_rows)), OUT_MATS))

    print("效果 %d 个 / 模块 %d 个 / 引用 node %d · asset %d"
          % (out_doc["effect_count"], out_doc["module_count"], stats["node"], stats["asset"]))
    if stats["cardAnimResolved"] or stats["cardAnimUnresolved"]:
        print("cardAnim → prefabName：解出 %d / 解不出 %d"
              % (stats["cardAnimResolved"], stats["cardAnimUnresolved"]))
        for m in ca_miss:
            print("   ! %s（%s）guid=%s —— %s" % (m["effect"], m["kind"], m["guid"], m["why"]))
    print("模块分布：")
    for k, v in kinds.most_common():
        print("   %-34s %d" % (k, v))
    print("写出 %s (%.0f KB)" % (OUT, os.path.getsize(OUT) / 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
