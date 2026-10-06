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

🆕 **2026-10-13 修（A231①）：不再「取第一份 `SerializedFile`」** —— `load()` 原来只拿包里
**第一份** CAB（`list(bf.files.values())[0]` + `next(…"SerializedFile")`）⇒ 多 CAB 包里**其余份
静默不查**（今天触发不到：本脚本的产物是我们自己打的单 CAB 包，它的源包也是单 CAB；
但 A173 之后**产物可能是多 CAB** —— 某件根的依赖树真跨了 CAB 时）。改法照
`工具/extract_missing_shaders.py` 的 **`repack_tree()`**（A173 已修的那一份，`:718-745`）的**同一条口径**：
  · **容器 / 预加载**只看「**装着 `AssetBundle` 对象的那一份**」（容器项写的是 `PPtr(m_FileID=0, …)`，
    `m_FileID == 0` = 引用方自己所在的那一份 ⇒ 只有这一份里查才有意义）—— 见 `ab_owner_sf()`；
  · **对象 / 流普查扫全部份** —— 见 `load()` / `streams()`；
  · 挑不出 AB 那一份、同名流对象撞车、pid 只落在别的 CAB 里 —— **一律出声**（⛔ 不静默跳过）。
判据 → `资料/普查产出_1008/波B1_工具三件.md` §四·2 · `资料/普查产出_1013/A表现核_块5.md` §A231。

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
    """打开包 → `(BundleFile, [包里**全部** SerializedFile])`。

    🔴 **2026-10-13（A231①）：这里原来是「取第一份 SerializedFile」** ——
    `list(bf.files.values())[0]` + `next(…"SerializedFile")`，多 CAB 包里**其余份静默不查**。
    `bf.files` 的序实测是**流文件与 `.sharedAssets` 在前、主 CAB 在后**（arena1：第一份 63 个对象，
    另一份 5304 个）⇒ 「第一份」纯属**撞巧**，不是判据。
    改法照 `extract_missing_shaders.py` 的 `repack_tree()`（A173 已修那一份）：
    **容器/预加载只查「装着 `AssetBundle` 对象的那一份」**（见 `ab_owner_sf`），
    **对象/流普查扫全部份** —— 本函数把全部份带出去，由调用方按用途取。
    """
    env = UnityPy.load(path)
    bf = list(env.files.values())[0]
    sfs = [v for v in bf.files.values() if type(v).__name__ == "SerializedFile"]
    if len(sfs) > 1:
        print(f"   ⓘ 多 CAB 包：内层 {len(sfs)} 份 CAB（{[v.name for v in sfs]}）—— "
              f"对象/流按**全部份**查；容器与预加载只查「装着 AssetBundle 对象的那一份」")
    return bf, sfs


def ab_owner_sf(sfs, tag):
    """挑出**装着 `AssetBundle` 对象的那一份 CAB**；挑不出就出声并返回 `None`。

    🔴 口径照 `extract_missing_shaders.py` 的 `repack_tree()`（A173 已修那一份）：
    **不是「第一份」**。判据是代码级的 —— 容器项/预加载项写的是 `PPtr(m_FileID=0, PathID)`，
    `m_FileID == 0` 在 Unity 语义里 = **引用方自己所在的那一份** ⇒ 只有把根收进 AB 那一份，
    `m_Container` 才指得对。0 个 / ≥2 个 AB 对象**一律出声**（⛔ 不猜、不退而求其次）。
    """
    owners = [(v, pid) for v in sfs for pid, o in v.objects.items()
              if o.type.name == "AssetBundle"]
    if len(owners) == 1:
        return owners[0][0]
    print(f"   🔴 {tag}里 AssetBundle 对象 {len(owners)} 个（要求恰好 1 个）⇒ 挑不出「该查的那一份」"
          + ("：" + " · ".join(f"pid {pid} @`{v.name}`" for v, pid in owners) if owners else ""))
    return None


def streams(sfs):
    """`{对象名: (CAB 名, 流路径, offset, size)}` —— 只看走资源流的 Texture2D，**扫全部 CAB**。

    🔴 2026-10-13（A231①）：原来是 `streams(sf)`（只扫一份）⇒ 多 CAB 包里别的份**静默不查**。
    同一张图在两份 CAB 里都有时，「按名字配对源包↔产物」会分不清是哪一张 ⇒ 记进 `dups`
    （第二个返回值），由调用方**出声**。
    """
    out, dups = {}, []
    for v in sfs:
        for o in v.objects.values():
            if o.type.name != "Texture2D":
                continue
            try:
                d = o.read_typetree()
            except Exception:
                continue
            sd = d.get("m_StreamData") or {}
            if sd.get("path"):
                n = d.get("m_Name")
                if n in out and out[n][0] != v.name:
                    dups.append(n)
                out[n] = (v.name, sd["path"], sd.get("offset"), sd.get("size"))
    return out, dups


def buf_of(bf, stream_path):
    """`m_StreamData.path`（`archive:/<目录>/<内层名>`）→ 那份内层文件的字节（取不到 → `None`）。

    🔴 2026-10-13（A231①）：[3] 段原来只认**一条写死的**流文件（`<NEW_CAB>.resS`）——
    多 CAB 产物里别的组有自己的流（`repack_tree()` 已给它们改名）⇒ 拿主组的缓冲去比**必错**。
    按**路径末段**取，单 CAB / 多 CAB 都成立。
    """
    name = stream_path.replace("\\", "/").rsplit("/", 1)[-1]
    return getattr(bf.files.get(name), "bytes", None)


def main():
    bad = 0
    if not os.path.isfile(DST):
        print(f"!! 产物不存在：{DST}（先跑 `extract_missing_shaders.py --prefabs`）")
        return 1

    bf, sfs = load(DST)
    print(f"[1] 产物内层文件 {len(bf.files)} 个：" + ", ".join(sorted(bf.files.keys())))
    if len(bf.files) != 2:
        print("   🔴 期望 2 个（主 CAB + .resS）—— 产物真变成多 CAB 的话，这条期望值要一起改"
              "（见文件头「期望值随包内容变」那段）")
        bad += 1

    # 🔴 A231①：容器 / 预加载的宿主 = **装着 AssetBundle 对象的那一份**（不是「第一份」）；
    #    挑不出来 ⇒ 后面两项查不了，**出声中止**（⛔ 不是安静地跳过）。
    sf = ab_owner_sf(sfs, "产物")
    if sf is None:
        print("   ⛔ 容器与预加载全靠这一份 ⇒ 中止（不是没查，是查不了）")
        return 1
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
    # 🔴 A231①：**扫全部份** —— pid 在**不同 CAB 里是两套命名空间**（实测 arena1 两份 CAB 有 63 个
    #    pid 撞号）⇒ 按裸 pid 建字典会**静默丢对象**。容器项写的是 `PPtr(m_FileID=0, …)` ⇒
    #    **权威的那一条永远在「装着 AB 对象的那一份」里**。{PathID: [(CAB 名, m_Name), …]}
    clips_in_bundle = {}
    for v in sfs:
        for o in v.objects.values():
            if o.type.name == "AnimationClip":
                clips_in_bundle.setdefault(o.path_id, []).append((v.name, o.read().m_Name))
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
        hits = clips_in_bundle.get(want_pid) or []
        own = [n for cab, n in hits if cab == sf.name]      # 容器项 m_FileID=0 ⇒ 只认这一份
        nm = own[0] if len(own) == 1 else None
        obj_ok = (nm == want_name)
        why = ""
        if not obj_ok:
            bad += 1
            if hits:
                why = (" —— PathID 在 " + " · ".join(f"`{c}`" for c, _ in hits)
                       + f" 里都有（两套命名空间），但 AB 那一份 `{sf.name}` 里没有 ⇒ 容器项指不到它")
            else:
                why = f" —— `{sf.name}` 这份里没这个 pid"
        print(f"     {'✅' if obj_ok else '🔴'} 包里 PathID {want_pid} 的对象 m_Name = {nm!r}"
              f"（应为 {want_name!r}）{why}")

    src_bf, src_sfs = load(SRC)
    dst_s, dst_dups = streams(sfs)
    src_s, src_dups = streams(src_sfs)
    # 🔴 A231①：同名流对象撞车 ⇒ 下面「按名字配对源包↔产物」只查得到其中一张，**出声**
    for tag, dups in (("产物", dst_dups), ("源包", src_dups)):
        for n in sorted(set(dups)):
            print(f"   🔴 {tag}里同名流对象 `{n}` 出现在多份 CAB ⇒ 下面按名字配对查不全它")
            bad += 1
    src_raw = src_bf.files[f"{OLD_CAB}.resS"].bytes
    dst_raw = bf.files[f"{NEW_CAB}.resS"].bytes
    print(f"[3] 走资源流的纹理：源包 {len(src_s)} 张 · 产物 {len(dst_s)} 张；"
          f"流大小 源包 {len(src_raw)/1024/1024:.1f} MB → 产物 {len(dst_raw)/1024:.0f} KB")
    for name, (cab, p, off, size) in sorted(dst_s.items()):
        ok_p = NEW_CAB in p and OLD_CAB not in p
        o0 = src_s.get(name)
        # 🔴 **判据是「取出来的字节一样」，不是「offset 一样」** —— 瘦身之后 offset 本来就该变
        # 🆕 A231①：缓冲**各按自己的流路径**取（多 CAB 产物里别的组有自己的流文件），不写死 `<NEW_CAB>.resS`
        ok_d = False
        if o0 is not None and o0[3] == size:
            try:
                sb, db = buf_of(src_bf, o0[1]), buf_of(bf, p)
                ok_d = bytes(sb[o0[2]:o0[2] + size]) == bytes(db[off:off + size])
            except Exception:
                ok_d = False
        flag = "✅" if (ok_p and ok_d) else "🔴"
        if not (ok_p and ok_d):
            bad += 1
        print(f"   {flag} {name:42s} 产物 offset={off} size={size}"
              + (f"（源包 offset={o0[2]} —— offset 变了没关系，**字节必须一样**）" if o0 else "（源包没有这张？）"))

    # [4] 瘦身后的流**只装用到的区间**：总长应当在「Σsize」与「Σsize + 每段 16 字节对齐的填充」之间
    # 🆕 A231①：只算**写那一组的流**（多 CAB 产物里别的 CAB 整份保留、**不瘦身** ⇒ 它们不在这个式子里）；
    #    单 CAB 时 `thin` = 全部，输出与改前逐字节一致。
    main_stream = f"{NEW_CAB}.resS"
    thin = [s for s in dst_s.values()
            if s[1].replace("\\", "/").rsplit("/", 1)[-1] == main_stream]
    need = sum(s[3] for s in thin)
    cap = need + 16 * len(thin)
    extra = "" if len(thin) == len(dst_s) else (
        f"（另有 {len(dst_s) - len(thin)} 张走别的 CAB 的流 —— 整份保留、不瘦身，不计入）")
    print(f"[4] 流瘦身：产物 {len(dst_raw)/1024:.0f} KB（用到的区间合计 {need/1024:.0f} KB，"
          f"上限 {cap/1024:.0f} KB）· 源包 {len(src_raw)/1024/1024:.1f} MB{extra}")
    if not (need <= len(dst_raw) <= cap):
        print("   🔴 长度不在预期区间 —— 要么多装了、要么少装了（贴图会花/黑）")
        bad += 1

    print(f"\n结论：{'✅ 四项全过' if bad == 0 else f'🔴 {bad} 处不对'}")
    return 0 if bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
