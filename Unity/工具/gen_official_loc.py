#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_official_loc.py —— 原版官方本地化表（I2 `I2Languages` 远端包）→ 工程用的精简件

**这是什么**：原版客户端的**官方多语言词条表**，来源是 Addressables 远端包
`localization_assets_all` 里那个 `I2Languages` 资产的 `temp` 字段（**加密**）。
解码算法 = **逐字节重复密钥 XOR**（不是压缩），解码器 = `工具/_i2_decode_probe.py`
（本脚本**默认**仍调它解 **2025-07-30 旧快照**；⛔ 不另抄一份解码逻辑）。

**两条输入路**（2026-10-18 · `K07` 起）：
  · `--input <JSON>` 读**已解码**的 I2 全表（`{mTerms, mLanguages}`，例如
    `Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json` = **当前 build**），
    或读**合并候选**（`{terms:{键: "同值" | [en,zh]}}`）—— **不需要再解码**。
  · 不传 `--input` ⇒ **保持旧行为**：解 `localization_temp_9412193字节_未解密.bin`（2025-07-30 快照）。
`--carry <旧输出 JSON>`：把**只在旧表里存在**的键补回来（不丢键），值取旧表原值。

**输出**（只留 **en（第 0 列）与 zh-CN（第 8 列）** —— 其余 13 种语言跑 13lang 那份）：
  `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
  ```json
  { "_note": …, "_source": …, "_langs": ["en","zh-CN"], "_count": N,
    "terms": { "<键>": "<en>"            // en 与 zh 相同时压成一个字符串
             , "<键>": ["<en>", "<zh>"]  // 不同时是两元组
             } }
  ```
  ⚠️ **zh-CN 列里带 `[[token]]` 与 `((X_y,...))` 记号** —— 那是 I2 的**占位符**，
  由客户端在运行时换成图标/数字（`[[waystone]]`、`((Spirit_x,1))`、`((Melee_attack_icon_x,+1))`、
  `<br>`）。⇒ **它不是「可以直接印的成品文字」**，用之前要经我们那层图标重写
  （`CardPresentation/Core/CardIcons.cs` 的 `Rewrite`）。

用法：
  # 默认：解 2025-07-30 旧快照（保持旧行为）
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc.py
  # 当前 build（2026）＋ 保留旧表独有的键
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc.py \
      --input d:/4/Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json \
      --carry "d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"
  # 只对账，不写文件（对账的是「用同一组参数生成的结果」vs 盘上那份）
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc.py --check
"""
import argparse
import io
import json
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
PROBE = os.path.join(HERE, "_i2_decode_probe.py")
OUT = r"d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"

IDX_EN = 0
IDX_ZH = 8          # `mLanguages` 的第 8 项 = `Simplified Chinese` / `zh-CN`（脚本会现核一遍）

NOTE = (
    "原版官方本地化表（I2 Localization 的 `I2Languages`，来自 Addressables 远端包 "
    "`localization_assets_all`）。**本文件只留 en 与 zh-CN 两列**；"
    "🆕 **2026-10-17 起 13 种语言全量另有生成器** → `工具/gen_official_loc_13lang.py`"
    "（产出 `Resources/official_loc_13lang.json` + `数据/游戏数据/官方本地化_13lang.json`，"
    "`Core/Loc.cs` 按 `Loc.Current` 取列）。"
    "⚠️ zh-CN 列是 **I2 占位符模板**（`[[token]]` / `((X_y,…))` / `<br>`），不是成品文字 —— "
    "印出来之前要过图标重写那一层。"
)
NOTE_NEW = (
    "原版官方本地化表（I2 Localization 的 `I2Languages`，来自 Addressables 远端包 "
    "`localization_assets_all`）。**本文件 = 当前 build（2026）的 en 与 zh-CN 两列**；"
    "13 种语言全量 → `工具/gen_official_loc_13lang.py`。"
    "⚠️ zh-CN 列是 **I2 占位符模板**（`[[token]]` / `((X_y,…))` / `<br>`），不是成品文字 —— "
    "印出来之前要过图标重写那一层。"
)
CARRY_NOTE = (
    "另保留/补回 {n} 条 **2025 旧表里有内容、当前 build 缺失或已清空**的键"
    "（`--carry`，不丢键；值取旧表原值）。"
)
SOURCE_OLD = (
    "原件 = `d:/2/远端缓存_旧profile_1013/导出/localization_temp_9412193字节_未解密.bin`"
    "（9,412,193 字节；抢救自旧 Windows 用户缓存的 Unity Addressables 缓存，"
    "按哈希命名 ⇒ 按包名搜不到，见 `d:/2/远端缓存_旧profile_1013/README.md`）。"
    "解码 = **逐字节重复密钥 XOR**：`temp[i] = ((i 偶 ? 0x17 : 0xCD) * i) ^ temp[i] ^ pw[i % pw.Length]`，"
    "密钥 = 硬编码字符串字面量（VA `0x18427C678`，138 字符），"
    "出处 = `d:/2/tools/decomp_full/I2.Loc.LanguageSourceAsset__DecryptData.c` + `__.cctor.c`；"
    "**不是压缩**（前任试过的 gzip/zlib/deflate/lzma/bz2 方向是错的）。"
    "⚠️ 年代限制：与 `allcards` 同一批 —— Unity **2022.3.38f1 / 2025-07-30** 的快照，"
    "**不含**快照之后新增的内容。"
)
SOURCE_NEW = (
    "原件 = **当前 build（2026）的官方本地化表**：bundle "
    "`D:/2/关服前存档/新建文件夹/Everguild_Warpforge/"
    "c9187397bc0aeae61b33d2c1c82c2a6f/dca1724a3a849f02cbbf815c0a94c405/__data` 里的 "
    "`I2Languages.temp`（10,764,841 字节）。解码 = **逐字节重复密钥 XOR**（同一密钥/算法，"
    "解码器 `工具/_i2_decode_probe.py`；**不是压缩**）。"
    "已解码快照 = `Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json`"
    "（10,857 条 × 15 种语言）；本文件是它的 en / zh-CN 两列（en/zh 都空的 8 条丢弃）。"
    "⛔ **不是** 2025-07-30 旧快照 `localization_temp_9412193字节_未解密.bin`（9,447 条）。"
)


def decode(out_path):
    """调解码探针把词条表落成 JSON（⛔ 解码逻辑只此一处）。"""
    cmd = [sys.executable, "-I", "-X", "utf8", PROBE, "--out", out_path]
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0 or not os.path.exists(out_path):
        sys.exit(f"❌ 解码探针失败（rc={r.returncode}）\n{r.stdout[-2000:]}\n{r.stderr[-2000:]}")
    return json.load(io.open(out_path, encoding="utf-8"))


def terms_from_i2(doc):
    """I2 全表（`{mTerms, mLanguages}`）→ `({键: (en, zh)}, 丢弃数)`；en/zh 都空的丢掉。"""
    langs = doc.get("mLanguages") or []
    if len(langs) <= IDX_ZH or (langs[IDX_ZH].get("Code") or "") != "zh-CN":
        sys.exit(f"❌ `mLanguages[{IDX_ZH}]` 不是 zh-CN（实得 {langs[IDX_ZH] if len(langs) > IDX_ZH else None}）"
                 f" —— 列号改过，别照旧号取中文")
    terms = {}
    empty = 0
    for x in doc.get("mTerms") or []:
        key = x.get("Term")
        if not key:
            continue
        L = x.get("Languages") or []
        en = L[IDX_EN] if len(L) > IDX_EN else ""
        zh = L[IDX_ZH] if len(L) > IDX_ZH else ""
        if not en and not zh:
            empty += 1
            continue
        terms[key] = (en, zh)
    return terms, empty


def terms_from_merge(doc):
    """合并候选（`{terms:{键: "同值" | [en,zh]}}`）→ `({键: (en, zh)}, 0)`。"""
    terms = {}
    for k, v in (doc.get("terms") or {}).items():
        if isinstance(v, list):
            en = v[0] if len(v) > 0 else ""
            zh = v[1] if len(v) > 1 else ""
        else:
            en = zh = v
        if not en and not zh:
            continue
        terms[k] = (en, zh)
    return terms, 0


def load_input(path):
    """认两种输入形状：I2 全表 / 合并候选。"""
    doc = json.load(io.open(path, encoding="utf-8"))
    if "mTerms" in doc:
        return terms_from_i2(doc)
    if "terms" in doc:
        return terms_from_merge(doc)
    sys.exit("❌ 认不出 `--input` 的格式（要有 `mTerms` 或 `terms`）：%s" % path)


def load_carry(path):
    """旧输出件（`{terms}`，值可能是压缩字符串或 `[en,zh]`）→ `{键: (en, zh)}`。"""
    doc = json.load(io.open(path, encoding="utf-8"))
    terms = doc.get("terms")
    if not isinstance(terms, dict):
        sys.exit("❌ `--carry` 的文件里没有 `terms` 字典：%s" % path)
    out = {}
    for k, v in terms.items():
        if isinstance(v, list):
            en = v[0] if len(v) > 0 else ""
            zh = v[1] if len(v) > 1 else ""
        else:
            en = zh = v
        out[k] = (en, zh)
    return out


def compact(en, zh):
    return en if en == zh else [en, zh]


def build(terms, source, carried):
    out = {k: compact(en, zh) for k, (en, zh) in terms.items()}
    note = NOTE_NEW if source is SOURCE_NEW else NOTE
    if carried:
        note += CARRY_NOTE.format(n=carried)
    return {"_note": note, "_source": source, "_langs": ["en", "zh-CN"],
            "_count": len(out), "terms": out}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--input", default=None,
                    help="已解码的 I2 全表 JSON（{mTerms,mLanguages}）或合并候选（{terms}）；"
                         "不传 = 解 2025-07-30 旧快照（旧行为）")
    ap.add_argument("--carry", default=None,
                    help="旧输出 JSON：把只在旧表里存在的键补回来（不丢键）")
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    carried = 0
    if args.input:
        terms, empty = load_input(args.input)
        source = SOURCE_NEW
        if args.carry:
            carry = load_carry(args.carry)
            for k, (en, zh) in carry.items():
                if k not in terms:
                    terms[k] = (en, zh)
                    carried += 1
        input_desc = args.input
    else:
        tmp = os.path.join(tempfile.mkdtemp(prefix="wf_official_loc_"), "i2.json")
        terms, empty = terms_from_i2(decode(tmp))
        source = SOURCE_OLD
        input_desc = "旧快照（解码探针）"

    doc = build(terms, source, carried)

    pref = {}
    for k in doc["terms"]:
        pref[k.split("/")[0]] = pref.get(k.split("/")[0], 0) + 1
    print("输入：%s" % input_desc)
    print("词条 %d 条（en/zh 都空的丢掉 %d 条）· 键族 %d 个 · carry 补回 %d 条"
          % (doc["_count"], empty, len(pref), carried))
    for k, v in sorted(pref.items(), key=lambda x: -x[1])[:15]:
        print(f"  {k:24s} {v}")
    for fam in ("Card_Name/", "Card_Description/", "Card_Abilities/",
                "Card_Trait/", "CardTraitDescription/", "Battle/"):
        n = sum(1 for k in doc["terms"] if k.startswith(fam))
        print(f"  [{fam}] {n}")

    if args.check:
        if not os.path.exists(OUT):
            print(f"\n--check：{OUT} 不存在")
            return 1
        old = json.load(io.open(OUT, encoding="utf-8"))
        same = (old.get("terms") == doc["terms"] and old.get("_count") == doc["_count"]
                and old.get("_source") == doc["_source"])
        print(f"\n--check：{'一致 ✅' if same else '**已过期** ❌（重跑不带 --check）'}")
        return 0 if same else 1

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    text = json.dumps(doc, ensure_ascii=False, separators=(",", ":"))
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(f"\n写出 {OUT}  ({os.path.getsize(OUT) / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
