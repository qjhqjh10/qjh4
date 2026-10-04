#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_verify_prefab_bundle.py — 自检 `wf_prefabs_extra.bundle` 的重打包产物（**只读**）

背景：这两件 prefab（`Card 3D Death Explosion` / `Vanguard Frame Animated VAT`）
**不是 addressable** ⇒ Unity 的 `GetAllAssetNames()` / `LoadAllAssets<GameObject>()`
两条枚举路都不含它们（判据 → `资料/已知的坑.md` 同名那条）。
做法 = `extract_missing_shaders.py --prefabs` 把它们 + 整棵依赖树重打成一个小包。

**这个脚本查四件事**（每件都要有明确结论，别只看「跑通了」）：
 1. 产物能被打开、内层文件是 2 个（主 CAB + `.resS`）
 2. `AssetBundle.m_Container` 的 **16** 条名字对不对（**7 件根 × 裸名 / 小写路径** + **2 条 assetGUID 别名**）
 3. 6 张纹理的 `m_StreamData.path` 已指向**新 CAB 名**（改名了），且**取出来的字节与源包逐字节相同**
    —— 🔴 判据是「**字节**一样」，**不是「offset 一样」**：资源流做了瘦身（只留用到的区间）⇒ offset 本来就该变
 4. 流瘦身的长度落在预期区间（`Σsize ≤ 产物 ≤ Σsize + 每段 16 字节对齐的填充`）

🆕 **2026-10-01 加第 3 件根**：`Vanguard_Frame VAT Dissolve`（一个**材质**）。
为什么它也要当根收进来：**没有任何 Unity 对象引用它**（引用它的是 AnimFX 的**模块字段**，
活在 JSON 里）⇒ 依赖树走不到、容器里也没有 ⇒ Unity 侧三条取法**一条都拿不到**。
判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①（含探针 `EffectExporter.ProbeModuleMaterials`）。
⇒ 本脚本的期望值随之从 **2 件 / 4 条 / 2 条预加载** 改成 **3 件 / 6 条 / 3 条预加载**。

🆕 **2026-10-01 晚加第 4 件根**：`Card Explosion`（一个 **`AnimationClip`**）。
为什么：片段是**控制器的依赖**、不是资产根 ⇒ `LoadAllAssets<AnimationClip>()` 看不到它
（那个 API 只按容器/预加载表走）⇒ 症状是「控制器建出来了、**动作却是空的**」。
⇒ 期望值再改成 **4 件 / 8 条 / 4 条预加载**。

🆕 **同一晚再加第 5 件根**：`Card 3D WH40K Explosion`（那份 **`AnimatorController`**）。
为什么它也要当根：工程 `.controller` 的**曲线是空的**（muscle 格式落不了盘）⇒ 真正播的动画
由运行时 `WarpforgeAnimatorBridge` 从**这个包**里按名字取原件换上（与原版 shader 同一条路子），
而 `LoadAsset<T>(名字)` 只认容器项 ⇒ 不登记就取不到。
⇒ 期望值 **5 件 / 10 条 / 5 条预加载**。
（`Card 3D WH40K Explosion` 那份控制器在包里、也在依赖树里 —— 它**不需要**当根：
导出侧是按 `animator_controllers.json` **照建**的，不靠 Unity 取那份运行时格式的对象。）

🆕 **2026-10-07（A192）再加第 6、7 件根**：`LightAnimationOrbit` 与
`Dark Angels Void Combat animations`（两条 **`AnimationClip`**）。
为什么它们也要当根：它们**没有任何 Unity 对象引用** —— 引用它们的是环境 SO 的
`animationsToChange[].clip`（`AssetReferenceTyped<AnimationClip>`，**按 assetGUID**）
⇒ 依赖树走不到；而运行时那一跳 `AnimationClipByGuid(guid)` → `LoadAsset<AnimationClip>(guid)`
**只认容器键** ⇒ 还要**多登记一条 GUID 形态的别名**（原版源包的容器键本身就是 GUID，
所以这条别名就是原版那条取法）。
⇒ 期望值 **7 件 / 16 条 / 7 条预加载**（16 = 7 根 × 2 条 + 2 条 GUID 别名）。
判据与逐条原始读法 → `资料/普查产出_1007/波9_A192_两个clip进包.md` §2.2。

🔴 **2026-10-07 顺手修（A199）：本脚本原来没有 UTF-8 stdout 兜底** —— 这台机器默认 stdout
是 **GBK（cp936）**，`print("✅ …")` 会抛 `UnicodeEncodeError`（U+2705 编码不了）**整个脚本崩**，
中文还会全变乱码（终端、`> log.txt` 都实测过）⇒ 见下面那段 `reconfigure`。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/_verify_prefab_bundle.py
⚠️ 期望值随包内容变 —— 改了 `extract_missing_shaders.py --prefabs` 的收根规则，
   这里的 `WANT` / `GUID_ALIASES` 要**一起改**（否则本脚本会红，那正是它的用处）。
"""
import hashlib
import io
import os
import sys

import UnityPy

# 🔴 **UTF-8 stdout 兜底**（2026-10-07 · A199）：默认编码是 GBK 时，打印 `✅`（U+2705）
#    会 `UnicodeEncodeError` **整个脚本崩掉**，中文也全乱码 —— 重定向到文件时同样。
#    （同族先例：`extract_missing_shaders.py` 顶上那段注释。）
try:
    sys.stdout.reconfigure(encoding="utf-8")
    # ⚠️ **2026-10-08 顺手补**（同一处缺陷的另一半；⛔ 不是修「崩」）：`stderr` 也要配 ——
    #    实测 `stderr` **不会** `UnicodeEncodeError`（CPython 给 stderr 的默认 error handler 是
    #    `backslashreplace`），但会**打成乱码**（实测 `✅ 未修：这行会崩` → `\u2705 δ�ޣ…`）⇒
    #    真出错时那条回溯/报错读不了（本机路径里就带中文：`d:/4/Unity/工具/…`）。
    #    同族先例：`extract_missing_shaders.py:116-120`（它两条都配了）。
    sys.stderr.reconfigure(encoding="utf-8")
except Exception:                                    # 3.7 以下 / stdout 被接走时就算了
    pass

SRC = r"D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle"
DST = r"d:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle"
OLD_CAB = "CAB-d47690319398b604c3bb5a35a8ed2499"
NEW_CAB = "CAB-wfprefabsextra"
# 7 件根（2026-10-07 起）：两件 prefab + 一件**只被 JSON 数据引用的材质** + 两份**动画片段**
#   + 一份 **`AnimatorController`** + 两条**只被环境 SO 按 assetGUID 引用的 `AnimationClip`**
WANT = ["Card 3D Death Explosion", "Vanguard Frame Animated VAT", "Vanguard_Frame VAT Dissolve",
        "Card Explosion", "Card 3D WH40K Explosion",
        "LightAnimationOrbit", "Dark Angels Void Combat animations"]
# 🆕 后两条 clip 除了「裸名 / 小写路径」还各多一条 **assetGUID 形态**的容器别名
#    （运行时 `AnimationClipByGuid(guid)` 走的就是它）。{根名: GUID}
GUID_ALIASES = {"LightAnimationOrbit": "58db0a1f684b5ee4ba197e8d344012d2",
                "Dark Angels Void Combat animations": "aac3fe87a4618f5478ceaad364650105"}
# 那两条 GUID 别名**该指向谁**（出处 → `资料/普查产出_1007/波9_A192_两个clip进包.md` §2.1）：
#   {GUID: (clip 名, PathID)} —— 只登记对键还不够，键还得**指向那条 clip 本身**
CLIP_BY_GUID = {"58db0a1f684b5ee4ba197e8d344012d2": ("LightAnimationOrbit", 1230949865609814630),
                "aac3fe87a4618f5478ceaad364650105": ("Dark Angels Void Combat animations",
                                                      2397232529203406555)}
WANT_CONTAINER = 2 * len(WANT) + len(GUID_ALIASES)   # 16 = 7 根 × 裸名/小写路径 + 2 条 GUID
WANT_PRELOAD = len(WANT)                             # 7  = 每件根一条预加载


def cont_pid(cont, key):
    """容器条目 → 它指向的 PathID（UnityPy 各版本字段名不一样，取不到就 None）。"""
    for k, info in cont:
        if k != key:
            continue
        a = getattr(info, "asset", info)
        return getattr(a, "m_PathID", getattr(a, "path_id", None))
    return None


def load(path):
    env = UnityPy.load(path)
    bf = list(env.files.values())[0]
    sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")
    return bf, sf


def streams(sf):
    """{对象名: (path, offset, size)} —— 只看走资源流的 Texture2D。"""
    out = {}
    for o in sf.objects.values():
        if o.type.name != "Texture2D":
            continue
        try:
            d = o.read_typetree()
        except Exception:
            continue
        sd = d.get("m_StreamData") or {}
        if sd.get("path"):
            out[d.get("m_Name")] = (sd["path"], sd.get("offset"), sd.get("size"))
    return out


def main():
    bad = 0
    if not os.path.isfile(DST):
        print(f"!! 产物不存在：{DST}（先跑 `extract_missing_shaders.py --prefabs`）")
        return 1

    bf, sf = load(DST)
    print(f"[1] 产物内层文件 {len(bf.files)} 个：" + ", ".join(sorted(bf.files.keys())))
    if len(bf.files) != 2:
        print("   🔴 期望 2 个（主 CAB + .resS）")
        bad += 1

    ab = None
    for o in sf.objects.values():
        if o.type.name == "AssetBundle":
            ab = o.read()
    cont = list(ab.m_Container) if ab is not None else []
    names = [c[0] for c in cont]
    print(f"[2] 容器 {len(names)} 条（期望 {WANT_CONTAINER} = {len(WANT)} 件根 × 裸名/小写路径 "
          f"+ {len(GUID_ALIASES)} 条 GUID 别名）：{names}")
    if len(names) != WANT_CONTAINER:
        print(f"   🔴 容器条数 {len(names)} ≠ 期望 {WANT_CONTAINER}")
        bad += 1
    for w in WANT:
        bare_ok = w in names
        low_key = "assets/" + w.lower() + "."
        low_hits = [n for n in names if n.startswith(low_key)]
        low_ok = bool(low_hits)
        if not bare_ok:
            print(f"   🔴 没有 裸名 `{w}` —— `LoadAsset(name)` 会取不到")
            bad += 1
        if not low_ok:
            print(f"   🔴 没有 小写路径 `{low_key}<ext>`")
            bad += 1
        if bare_ok and low_ok:
            print(f"   ✅ 有   {w:36s} 裸名 ✅ · 小写路径 {low_hits[0]!r} ✅")
    print(f"    预加载表 {len(ab.m_PreloadTable)} 条（应为 {WANT_PRELOAD} = 根个数）")
    if len(ab.m_PreloadTable) != WANT_PRELOAD:
        bad += 1

    # [2·b] 两条 clip：**3 种别名**逐个查「有 / 没有」，并且**键要指向那条 clip 本身**
    clips_in_bundle = {o.path_id: o.read().m_Name for o in sf.objects.values()
                       if o.type.name == "AnimationClip"}
    print("[2·b] 只被环境 SO 按 assetGUID 引用的两条 clip —— 3 种别名逐个查"
          "（判据 → 资料/普查产出_1007/波9_A192_两个clip进包.md §2.1/§2.2）")
    for gname, guid in GUID_ALIASES.items():
        want_name, want_pid = CLIP_BY_GUID[guid]
        print(f"   {gname}")
        for label, key in (("裸名      ", gname),
                           ("小写 .anim", "assets/" + gname.lower() + ".anim"),
                           ("assetGUID ", guid)):
            if key not in names:
                print(f"     🔴 没有  {label} `{key}`")
                bad += 1
                continue
            pid = cont_pid(cont, key)
            ok = (pid == want_pid)
            if not ok:
                bad += 1
            print(f"     {'✅ 有' if ok else '🔴 有（但指错了）'}  {label} `{key}` → PathID {pid}"
                  + ("" if ok else f"（应为 {want_pid}）"))
        nm = clips_in_bundle.get(want_pid)
        obj_ok = (nm == want_name)
        if not obj_ok:
            bad += 1
        print(f"     {'✅' if obj_ok else '🔴'} 包里 PathID {want_pid} 的对象 m_Name = {nm!r}"
              f"（应为 {want_name!r}）")

    src_bf, src_sf = load(SRC)
    dst_s, src_s = streams(sf), streams(src_sf)
    src_raw = src_bf.files[f"{OLD_CAB}.resS"].bytes
    dst_raw = bf.files[f"{NEW_CAB}.resS"].bytes
    print(f"[3] 走资源流的纹理：源包 {len(src_s)} 张 · 产物 {len(dst_s)} 张；"
          f"流大小 源包 {len(src_raw)/1024/1024:.1f} MB → 产物 {len(dst_raw)/1024:.0f} KB")
    for name, (p, off, size) in sorted(dst_s.items()):
        ok_p = NEW_CAB in p and OLD_CAB not in p
        o0 = src_s.get(name)
        # 🔴 **判据是「取出来的字节一样」，不是「offset 一样」** —— 瘦身之后 offset 本来就该变
        ok_d = False
        if o0 is not None and o0[2] == size:
            try:
                ok_d = bytes(src_raw[o0[1]:o0[1] + size]) == bytes(dst_raw[off:off + size])
            except Exception:
                ok_d = False
        flag = "✅" if (ok_p and ok_d) else "🔴"
        if not (ok_p and ok_d):
            bad += 1
        print(f"   {flag} {name:42s} 产物 offset={off} size={size}"
              + (f"（源包 offset={o0[1]} —— offset 变了没关系，**字节必须一样**）" if o0 else "（源包没有这张？）"))

    # [4] 瘦身后的流**只装用到的区间**：总长应当在「Σsize」与「Σsize + 每段 16 字节对齐的填充」之间
    need = sum(s[2] for s in dst_s.values())
    cap = need + 16 * len(dst_s)
    print(f"[4] 流瘦身：产物 {len(dst_raw)/1024:.0f} KB（用到的区间合计 {need/1024:.0f} KB，"
          f"上限 {cap/1024:.0f} KB）· 源包 {len(src_raw)/1024/1024:.1f} MB")
    if not (need <= len(dst_raw) <= cap):
        print("   🔴 长度不在预期区间 —— 要么多装了、要么少装了（贴图会花/黑）")
        bad += 1

    print(f"\n结论：{'✅ 四项全过' if bad == 0 else f'🔴 {bad} 处不对'}")
    return 0 if bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
