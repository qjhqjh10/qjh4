# -*- coding: utf-8 -*-
"""把原版 **1248 个 cosmetic ScriptableObject** 里属于卡背的那批，压成**运行期那张表**。

为什么要这一步：
  卡背页左侧那个筛选抽屉（原版 `Cosmetic FIlter` → `Army Filter` + `Owned Toggle`）要按**阵营**筛卡背，
  而「哪张卡背属于哪个阵营」原版是**跑在 SO 上**的（`CosmeticItemCardback.cardArmy`）。
  我们的 `Assets/` 里没有那份 SO（它只在 `d:/2` 的导出里）⇒ 把值抽成一张 `Resources/` 下的 JSON，
  运行时按**卡背名**查 —— 与 `UnitTweens.json` / `OffensiveCards.json` /
  `EnvironmentConditions.json` / `VfxMap` 那几件**同一个模式**。

数据源：`d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/*.json`
        （1261 个文件；其中 `m_Name` 以 `Cardback` 开头的是卡背，其余是头像/头像框/徽章等 —— 不收）

🔴 **怎么把 SO 对到我们那张图**（这是本脚本唯一一处非平凡的地方）：
   **不能拿 SO 的 `m_Name` 去对**！实测：SO 名与图名**对不上 23 个**
   （例：SO `Cardback_AM_Forge` ↔ 图 `Cardback_AM_Forge_Grit and Determination`；
    SO `Cardback_ASH_Warp Spiders` ↔ 图 `Cardback_ASH_Warp Spider`）。
   对得上的是 **`imageReference.m_SubObjectName` 去掉结尾 `_Main`**——
   实测 **243/243 全部命中**我们那 233 张图（有 10 个 SO 共用同一张图）。
   出处：SO 里 `imageReference.m_SubObjectName` = 图集里那张 sprite 的名字，
   而我们的 PNG 就是按 sprite 名导出的（`工具/import_*.py` 那一族）。

阵营：`cardArmy` 是 **`CardArmy` 枚举**（`d:/2/tools/il2cpp_out/dump.cs:45637-45650`）：
   `0 Neutral · 10 Ultramarines · 20 Goff · 30 SaimHann · 40 Sautekh · 50 BlackLegion ·
    60 Leviathan · 70 TauEmpire · 80 Sororitas · 90 Genestealers · 100 AstraMilitarum ·
    110 DarkAngels · 120 EmperorsChildren · 130 SpaceWolves`
   —— 与我们的 `CardDef.Faction` **逐字同名**（`cards_engine.json` 里就是这 13 个），
   所以表里直接写**枚举名**，C# 侧不用再转一次。

输出：`MyGame/Assets/CardPresentation/Resources/Cardbacks.json`
用法：`python d:/4/Unity/工具/gen_cardbacks.py`
"""
import io
import json
import os
import sys
import glob

SO_DIR = "d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour"
TEX_DIR = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cardbacks"
DST = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Cardbacks.json"
# 🆕 2026-10-19（B2）：**原版那张 sprite** —— 卡背「两段式」画法要的那两个矩形就在这儿
SPRITE_DIR = "d:/2/新解包资源/assets_full/bundle_cosmeticscardbacksimages_assets_all/Sprite"


def sprite_metrics(sprite_json_name):
    """读 `Sprite/<名>.json` 的 `m_Rect` 与 `m_RD.textureRect` ⇒ `(rectW, rectH, padL, padR, padB, padT)`。

    这是**原版 uGUI 画一张 sprite 用的全部数据**（判据全文与算式 → `Core/CardbackFace.cs`）：
      · `m_Rect`（= C# 侧 `Sprite.rect`）= **707×1020**（本批 233 张**恒等**，逐张现读核过）——
        uGUI 拿它**定框**（`Image.PreserveSpriteAspectRatio` 比的就是 `sprite.rect` 的比例）。
      · `m_RD.textureRect` = **画心**（逐张不同）—— uGUI 拿它算 `padding`，再把贴图**贴到框里内缩后的那一块**。
      · 四个 `padX` 按 `rect − textureRect` 算（Unity 的 `textureRect`/`m_Rect` 同在源图坐标系；
        `.y` **从底边量**，与 PIL 相反 —— 与 `import_original_art.write_cardback` 那条一致）。
    ⚠️ **逐张不同**（`padL`/`padR` 0~49.08、`padB` 0~49.08、`padT` 0~52.03，且**可不对称**）
       ⇒ 别按某一张的值常量算（铁律 5·c）。
    查不到那份 json ⇒ 返回 **None**（**不猜** —— 调用方报出来并拒绝写表）。
    """
    p = os.path.join(SPRITE_DIR, sprite_json_name + ".json")
    if not os.path.exists(p):
        return None
    d = json.load(io.open(p, encoding="utf-8"))
    r = d["m_Rect"]
    t = d["m_RD"]["textureRect"]
    rw, rh = r["width"], r["height"]
    pl = t["x"] - r["x"]
    pr = (r["x"] + r["width"]) - (t["x"] + t["width"])
    pb = t["y"] - r["y"]
    pt = (r["y"] + r["height"]) - (t["y"] + t["height"])
    return (rw, rh, pl, pr, pb, pt)


def metrics_row(m):
    """六个列（`round` 到 1e-4 像素 —— 卡背最大 707px，这个精度比 1/10000 px 还细）。

    ⚠️ `+ 0.0` 是为了把 `-0.0` 洗成 `0.0`：图集取整会让 `padR`/`padT` 落到 `-1.5e-05` 这种量级，
       `round(·, 4)` 出来是 **`-0.0`**。那是合法 JSON，但没必要让解析方去处理这个负零（值本来就一样）。
    """
    return {"rectW": round(m[0], 4) + 0.0, "rectH": round(m[1], 4) + 0.0,
            "padL": round(m[2], 4) + 0.0, "padR": round(m[3], 4) + 0.0,
            "padB": round(m[4], 4) + 0.0, "padT": round(m[5], 4) + 0.0}

# `CardArmy` 枚举（dump.cs:45637-45650）—— 逐条抄，不推
ARMY = {
    0: "Neutral", 10: "Ultramarines", 20: "Goff", 30: "SaimHann", 40: "Sautekh",
    50: "BlackLegion", 60: "Leviathan", 70: "TauEmpire", 80: "Sororitas",
    90: "Genestealers", 100: "AstraMilitarum", 110: "DarkAngels",
    120: "EmperorsChildren", 130: "SpaceWolves",
}


def main():
    # 我们本地真正有图的那些卡背（`_sdf` 是同一张的掩码，不算）
    local = sorted(f[:-4] for f in os.listdir(TEX_DIR)
                   if f.endswith(".png") and not f.endswith("_sdf.png"))
    local_set = set(local)

    rows = []
    unmatched_so = []
    no_metrics = []
    for p in sorted(glob.glob(os.path.join(SO_DIR, "*.json"))):
        d = json.load(io.open(p, encoding="utf-8"))
        if isinstance(d, list):
            d = d[0]
        so_name = d.get("m_Name") or ""
        if not so_name.startswith("Cardback"):
            continue                                   # 头像/头像框/徽章…不收
        sub = (d.get("imageReference") or {}).get("m_SubObjectName") or ""
        if sub.endswith("_Main"):
            sub = sub[:-5]
        if sub not in local_set:
            unmatched_so.append((so_name, sub))
            continue
        army_id = d.get("cardArmy", 0)
        met = sprite_metrics(sub + "_Main")
        if met is None:
            no_metrics.append(sub)
        rows.append({
            "name": sub,                               # **我们的图名**（= 对账的键）
            "so": so_name,                             # 原版 SO 名（留个可复查的坐标）
            "armyId": army_id,
            "army": ARMY.get(army_id, ""),             # 查不到 ⇒ 空串（生成时下面会报出来）
            "rarity": d.get("cardRarity", 0),
            "uniqueId": d.get("uniqueId") or "",
        })
        if met is not None:
            rows[-1].update(metrics_row(met))

    # 同一张图被多个 SO 引用时**留 army 最小的那个**（保守：Neutral 优先），并报出来
    by_name = {}
    dup = []
    for r in sorted(rows, key=lambda r: (r["armyId"], r["so"])):
        if r["name"] in by_name:
            dup.append((r["name"], by_name[r["name"]]["so"], r["so"]))
            continue
        by_name[r["name"]] = r
    items = [by_name[n] for n in sorted(by_name)]

    missing = sorted(local_set - set(by_name))
    unknown_army = sorted(r["name"] for r in items if not r["army"])

    # ---- 🆕 2026-10-03（A20）：**每阵营的默认卡背** —— 原版那 SO 里那份表 ----
    #   判据与读法 → `工具/read_default_cardbacks.py`（从 `sharedassets0.assets` 的原始字节读，
    #   那份 SO **不在任何 bundle、也不在任何有 type tree 的导出里**）。
    #   `name` 用的是 `imageReference.m_SubObjectName` 去 `_Main` ⇒ **与 `items` 同一把对账键**
    #   （所以能直接拿去 `CardArt.Cosmetic(...)` 取图）。
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from read_default_cardbacks import read_defaults
    defaults = []                                  # ⚠️ **出数组不出字典**：C# 侧 `JsonUtility` 不吃 Dictionary
    default_rows = read_defaults()
    bad_default = []
    for r in default_rows:
        if not r["army"] or r["name"] not in local_set:
            bad_default.append(r)
            continue
        defaults.append({"army": r["army"], "name": r["name"],
                         "cardback": r["cardback"], "uniqueId": r["uniqueId"]})
    print("默认卡背（DefaultCarbackByArmySO）:", len(defaults), "个阵营 →",
          {r["army"]: r["name"] for r in defaults})
    if bad_default:
        print("🔴 默认卡背对不上本地图的:", bad_default)

    out = {
        "note": "原版 cosmetic SO 的卡背子集（`cardArmy` = CardArmy 枚举名，与 CardDef.Faction 同名）。"
                "生成：工具/gen_cardbacks.py；判据：dump.cs:45637-45650 + SO 的 imageReference。"
                "`defaults` = **每个阵营的默认卡背**（原版 `DefaultCarbackByArmySO`，"
                "读法见 工具/read_default_cardbacks.py；数组不是字典 —— C# 的 JsonUtility 不吃 Dictionary）。"
                "🆕 `items` 每条的 `rectW/rectH/padL/padR/padB/padT` = **原版那张 sprite 的 `m_Rect` 与 `padding`**"
                "（读 `bundle_cosmeticscardbacksimages_assets_all/Sprite/<名>_Main.json`；"
                "`m_Rect` 233/233 恒 707×1020、padding 逐张不同）—— 卡背「两段式」画法要用，"
                "算式与判据全文 → `Core/CardbackFace.cs`。⚠️ `_SDF` 那张**不**进表（它的 padding 恒 0、"
                "两边差 <0.4%，见 `CardbackFace` 文件头）。",
        "items": items,
        "defaults": defaults,
    }
    # ⚠️ 先算好再写（别在 write 的实参里做会抛异常的事 —— 那会把文件截成 0 字节，CLAUDE.md 记过）
    text = json.dumps(out, ensure_ascii=False, indent=1)

    print("SO 卡背（m_Name 以 Cardback 开头）:", len(rows) + len(unmatched_so))
    print("  对不上本地图的 SO:", len(unmatched_so), unmatched_so[:5])
    print("本地 PNG:", len(local), "· 表里:", len(items), "· 本地缺表项:", len(missing), missing[:5])
    print("共用同一张图的 SO:", len(dup), dup[:5])
    print("army 查不出的:", len(unknown_army), unknown_army[:5])
    print("[B2] 读不到原版 sprite `m_Rect`/`textureRect` 的:", len(no_metrics), no_metrics[:5])
    import collections
    print("阵营分布:", dict(collections.Counter(r["army"] or "(空)" for r in items)))
    if missing or unmatched_so or unknown_army or bad_default or no_metrics or len(defaults) != 13:
        print("🔴 有对不上的 —— **先把上面几行查清再决定要不要写**")
        return
    io.open(DST, "w", encoding="utf-8", newline="\n").write(text)
    print("已写:", DST)


main()
