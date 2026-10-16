#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_probe_card_sfx.py — 探针：把卡 prefab 上 `CardScript.battleCardSounds`（9 条）+ 3 条 `changeTo*CUE`
的 **PPtr（m_FileID / m_PathID）** 解析成 **cue 资产名**。

为什么不能用 `import_battle_sfx.py` 那条链（GUID → catalog）：
  那一族（`SoundAssetCollection` 的 28 个字段）存的是 Addressables **AssetReference**（带 `m_AssetGUID`）；
  卡 prefab 这一族存的是**普通 PPtr**（只有 `m_FileID` + `m_PathID`）⇒ 只能按 **束的 externals** 解。

用法：PYTHONIOENCODING=utf-8 python -I -X utf8 工具/_probe_card_sfx.py
"""
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")
import UnityPy                                                     # noqa: E402

AA = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
PREFAB_BUNDLE = os.path.join(AA, "battleprefabs_vfxandmisc_assets_all.bundle")
CUE_BUNDLE = os.path.join(AA, "soundcollection_assets_all.bundle")

CUE_FIELDS = ["invokeCUE", "invokeCUELegendary", "minionDeathCUE",
              "meleeAttackHitSmall", "meleeAttackHitMedium", "meleeAttackHitBig",
              "backToHand", "landOnGround", "createCardInHand"]
TOP_FIELDS = ["changeToMeleeAttackCUE", "changeToRangeAttackCUE", "changeToActiveAbilityCUE"]


def main():
    env = UnityPy.load(PREFAB_BUNDLE)
    hit = None
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            d = obj.read_typetree()
        except Exception:                                          # noqa: BLE001
            continue
        if "battleCardSounds" in d:
            hit = (obj, d)
            break
    if hit is None:
        raise SystemExit("❌ 这一束里没有带 battleCardSounds 的 MonoBehaviour")
    obj, d = hit
    sf = obj.assets_file
    print(f"束 = {os.path.basename(PREFAB_BUNDLE)}")
    print(f"SerializedFile.m_Name = {sf.name}")
    print(f"命中对象：type={obj.type.name} path_id={obj.path_id} m_Name={d.get('m_Name')!r}")
    def _ext_name(e):
        """`FileIdentifier` 是对象（不是 dict）—— 兼容两种形状，⛔ 别写死。"""
        if isinstance(e, dict):
            return e.get("name") or "", str(e.get("path") or "")
        return (getattr(e, "name", "") or "",
                str(getattr(e, "path", getattr(e, "fileName", "")) or ""))

    print(f"\nexternals（{len(sf.externals)} 条）：")
    for i, e in enumerate(sf.externals):
        nm, p = _ext_name(e)
        print(f"  m_FileID={i + 1:<3} name={nm!r} path={p[:90]!r}")

    pptr = []
    for k in CUE_FIELDS:
        v = d["battleCardSounds"].get(k)
        pptr.append((k, v.get("m_FileID"), v.get("m_PathID")))
    for k in TOP_FIELDS:
        v = d.get(k)
        pptr.append((k, v.get("m_FileID"), v.get("m_PathID")))

    # 声音那一族的 cue 包装都在 soundcollection 束里 —— 建 path_id → 名字
    print(f"\n读 {os.path.basename(CUE_BUNDLE)} 建 path_id → m_Name …")
    cenv = UnityPy.load(CUE_BUNDLE)
    name_of = {}
    for o in cenv.objects:
        try:
            nm = o.read_typetree().get("m_Name")
        except Exception:                                          # noqa: BLE001
            nm = None
        name_of.setdefault(o.path_id, (o.type.name, nm))
    print(f"  该束对象数 = {len(name_of)}")

    print("\n=== 12 条 PPtr 解析 ===")
    print(f"{'字段':<26}{'fileID':<8}{'pathID':<22}{'在 soundcollection 里 → ':<28}")
    for k, fid, pid in pptr:
        ext = ""
        if fid and 1 <= fid <= len(sf.externals):
            ext = _ext_name(sf.externals[fid - 1])[0]
        got = name_of.get(pid)
        shown = f"{got[0]}/{got[1]!r}" if got else "（不在这一束）"
        print(f"{k:<26}{fid:<8}{pid:<22}{shown:<28}  ext={ext}")


if __name__ == "__main__":
    main()
