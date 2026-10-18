#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_official_loc_13lang.py —— 官方 I2 词条表 → **13 语言全列**（`A1812`）

**为什么要有这一份**（2026-10-17 · 界面线 T08 `A1812`）：
`工具/gen_official_loc.py` 只落 `en` / `zh-CN` 两列（`数据/游戏数据/官方本地化_en+zh-CN.json`），
而工程里的语言下拉是**原版的 12 项**（`Core/Loc.cs` 的 `AvailableLanguages`）⇒ 选到西/法/德/…时
`Loc.T` 只能回退英文。本脚本把**官方表全部 13 种真语言列**落下来，让 `Loc.T` / `Loc.BulkOr`
按 `Current` 各取各的列。

**数据源**（⛔ 不另抄解码逻辑）：
  · `--input <JSON>` 读**已解码**的 I2 全表（`{mTerms, mLanguages}`），例如
    `Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json` = **当前 build**；
  · 不传 `--input` ⇒ **保持旧行为**：调 `工具/_i2_decode_probe.py` 解
    `d:/2/远端缓存_旧profile_1013/导出/localization_temp_9412193字节_未解密.bin`（2025-07-30 快照）。
`--carry <旧 13lang 数据件>`：把**只在旧表里存在**的键补回来（13 列取旧表原值，不丢键）。
13 种真语言之外的 `Translation_eng_version` / `Order` 两列**不是语言**、不进本文件。

**两个产物**：
1. `Unity/数据/游戏数据/官方本地化_13lang.json` —— 供人读 / 供后续抽取（紧凑 JSON）：
   `{"_note":…, "_source":…, "_langs":[…13 码…], "_count":N, "terms":{键: [13 个值]}}`
2. `Unity/MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json` —— **运行时**读的那份：
   平行数组（`keys` + 每语言一列）—— `JsonUtility` 直接能反序列化，且比逐条对象小 ~10%。
   ⚠️ **两份内容同源**，改一个就得重跑本脚本（⛔ 别手工编辑）。

⚠️ 值的形态与 en/zh 那份一样：**I2 占位符模板**（`[[token]]` / `((X_y,…))` / `<br>`），
不是成品文字 —— 印出来之前要过 `Core/CardIcons.cs` 的记号重写那一层。

用法：
  # 默认：解 2025-07-30 旧快照（保持旧行为）
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc_13lang.py
  # 当前 build（2026）＋ 保留旧表独有的键
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc_13lang.py \
      --input d:/4/Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json \
      --carry "d:/4/Unity/数据/游戏数据/官方本地化_13lang.json"
  # 只对账，不写文件
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc_13lang.py --check
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

DATA_OUT = r"d:/4/Unity/数据/游戏数据/官方本地化_13lang.json"
RES_OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json"

# 语言列的顺序 = 官方 `mLanguages` 的前 13 项（脚本会按 `Name` 现核一遍）。
LANGS = [
    ("en", "English"),
    ("es", "Spanish"),
    ("ru", "Russian"),
    ("fr", "French"),
    ("de", "German"),
    ("pt", "Portuguese (Brazil)"),
    ("it", "Italian"),
    ("ko", "Korean"),
    ("zh", "Simplified Chinese"),
    ("ja", "Japanese"),
    ("pl", "Polish"),
    ("cs", "Czech"),
    ("tr", "Turkish"),
]
CODES = [c for c, _ in LANGS]

NOTE = (
    "原版官方本地化表（I2 Localization 的 `I2Languages`，来自 Addressables 远端包 "
    "`localization_assets_all`）。**13 种真语言全列**（`mLanguages` 另有 `Translation_eng_version` / "
    "`Order` 两列 —— 不是语言，未收录）。⚠️ 值是 **I2 占位符模板**（`[[token]]` / `((X_y,…))` / `<br>`），"
    "不是成品文字 —— 印出来之前要过 `Core/CardIcons.cs` 的记号重写那一层。"
)
NOTE_NEW = (
    "原版官方本地化表（I2 Localization 的 `I2Languages`，来自 Addressables 远端包 "
    "`localization_assets_all`）。**本文件 = 当前 build（2026）的 13 种真语言全列**"
    "（`mLanguages` 另有 `Translation_eng_version` / `Order` 两列 —— 不是语言，未收录）。"
    "⚠️ 值是 **I2 占位符模板**（`[[token]]` / `((X_y,…))` / `<br>`），"
    "不是成品文字 —— 印出来之前要过 `Core/CardIcons.cs` 的记号重写那一层。"
)
CARRY_NOTE = (
    "另保留/补回 {n} 条 **2025 旧表里有内容、当前 build 缺失或已清空**的键"
    "（`--carry`，不丢键；13 列取旧表原值）。"
)
SOURCE_OLD = (
    "原件 = `d:/2/远端缓存_旧profile_1013/导出/localization_temp_9412193字节_未解密.bin`"
    "（9,412,193 字节；抢救自旧 Windows 用户缓存的 Unity Addressables 缓存，"
    "按哈希命名 ⇒ 按包名搜不到，见 `d:/2/远端缓存_旧profile_1013/README.md`）。"
    "解码 = **逐字节重复密钥 XOR**：`temp[i] = ((i 偶 ? 0x17 : 0xCD) * i) ^ temp[i] ^ pw[i % pw.Length]`，"
    "密钥 = 硬编码字符串字面量（VA `0x18427C678`，138 字符），"
    "出处 = `d:/2/tools/decomp_full/I2.Loc.LanguageSourceAsset__DecryptData.c` + `__.cctor.c`；"
    "**不是压缩**（前任试过的 gzip/zlib/deflate/lzma/bz2 方向是错的）。"
    "⚠️ 年代限制：Unity **2022.3.38f1 / 2025-07-30** 的快照，不含快照之后新增的内容。"
)
SOURCE_NEW = (
    "原件 = **当前 build（2026）的官方本地化表**：bundle "
    "`D:/2/关服前存档/新建文件夹/Everguild_Warpforge/"
    "c9187397bc0aeae61b33d2c1c82c2a6f/dca1724a3a849f02cbbf815c0a94c405/__data` 里的 "
    "`I2Languages.temp`（10,764,841 字节）。解码 = **逐字节重复密钥 XOR**（同一密钥/算法，"
    "解码器 `工具/_i2_decode_probe.py`；**不是压缩**）。"
    "已解码快照 = `Unity/资料/普查产出_1018/allcards_1350/localization_2026_decoded.json`"
    "（10,857 条 × 15 种语言）。⛔ **不是** 2025-07-30 旧快照 "
    "`localization_temp_9412193字节_未解密.bin`（9,447 条）。"
)
RUNTIME_NOTE = (
    "运行时读的官方 13 语言词条表（`Loc.LoadLang13`）。生成器 = `工具/gen_official_loc_13lang.py`，"
    "**别手工编辑**。值与 `数据/游戏数据/官方本地化_13lang.json` 同源；仍是 I2 占位符模板。"
)


def decode(out_path):
    cmd = [sys.executable, "-I", "-X", "utf8", PROBE, "--out", out_path]
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0 or not os.path.exists(out_path):
        sys.exit("❌ 解码探针失败（rc=%s）\n%s\n%s" % (r.returncode, r.stdout[-2000:], r.stderr[-2000:]))
    return json.load(io.open(out_path, encoding="utf-8"))


def rows_from_i2(doc):
    """I2 全表 → `({键: [13 个值]}, [键序])`（空 Term 跳过；与旧版一样**不**过滤空值词条）。"""
    names = [l.get("Name") for l in (doc.get("mLanguages") or [])]
    for i, (_, want) in enumerate(LANGS):
        if i >= len(names) or names[i] != want:
            sys.exit("❌ `mLanguages[%d]` 不是 %r（实得 %r）—— 列号改过，别照旧号取"
                     % (i, want, names[i] if i < len(names) else None))
    rows, order = {}, []
    for t in doc.get("mTerms") or []:
        k = t.get("Term")
        if not k:
            continue
        L = t.get("Languages") or []
        order.append(k)
        rows[k] = [L[i] if i < len(L) else "" for i in range(len(LANGS))]
    return rows, order


def rows_from_carry(path):
    """旧 13lang 数据件（`{terms:{键: [13 个值]}}`）→ `{键: [13 个值]}`。"""
    doc = json.load(io.open(path, encoding="utf-8"))
    terms = doc.get("terms")
    if not isinstance(terms, dict):
        sys.exit("❌ `--carry` 的文件里没有 `terms` 字典：%s" % path)
    out = {}
    for k, v in terms.items():
        if not isinstance(v, list):
            sys.exit("❌ `--carry` 的 %r 不是 13 列数组（拿错文件了？）" % k)
        vals = [str(x) if x is not None else "" for x in v]
        vals += [""] * (len(LANGS) - len(vals))
        out[k] = vals[:len(LANGS)]
    return out


def build(rows, order, source, carried):
    note = NOTE_NEW if source is SOURCE_NEW else NOTE
    if carried:
        note += CARRY_NOTE.format(n=carried)
    data = {"_note": note, "_source": source, "_langs": list(CODES),
            "_count": len(order), "terms": {k: list(rows[k]) for k in order}}
    runtime = {"_note": RUNTIME_NOTE, "langs": list(CODES), "keys": list(order)}
    for i, c in enumerate(CODES):
        runtime[c] = [rows[k][i] for k in order]
    return data, runtime


def write(path, doc):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    text = json.dumps(doc, ensure_ascii=False, separators=(",", ":"))
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print("写出 %s  (%.1f MB)" % (path, os.path.getsize(path) / 1048576.0))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--input", default=None,
                    help="已解码的 I2 全表 JSON（{mTerms,mLanguages}）；不传 = 解 2025-07-30 旧快照")
    ap.add_argument("--carry", default=None,
                    help="旧 13lang 数据件：把只在旧表里存在的键补回来（不丢键）")
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    carried = 0
    if args.input:
        doc = json.load(io.open(args.input, encoding="utf-8"))
        if "mTerms" not in doc:
            sys.exit("❌ 13lang 需要 I2 全表（含 `mTerms`）作输入；合并候选只有 en/zh 两列：%s"
                     % args.input)
        rows, order = rows_from_i2(doc)
        source = SOURCE_NEW
        if args.carry:
            for k, vals in rows_from_carry(args.carry).items():
                if k not in rows:
                    rows[k] = vals
                    order.append(k)
                    carried += 1
                elif not any(rows[k]) and any(vals):
                    # 当前 build 把整行清空了、旧表还有内容 ⇒ 补回（与 en+zh 那份口径一致）
                    rows[k] = vals
                    carried += 1
        input_desc = args.input
    else:
        tmp = os.path.join(tempfile.mkdtemp(prefix="wf_loc13_"), "i2.json")
        rows, order = rows_from_i2(decode(tmp))
        source = SOURCE_OLD
        input_desc = "旧快照（解码探针）"

    data, runtime = build(rows, order, source, carried)

    print("输入：%s" % input_desc)
    print("词条 %d 条 · 语言 %d 列 · carry 补回 %d 条" % (data["_count"], len(data["_langs"]), carried))
    for c, name in LANGS:
        n = sum(1 for v in runtime[c] if v)
        print("  %-3s %-22s 非空 %d" % (c, name, n))

    if args.check:
        ok = True
        for path, doc in ((DATA_OUT, data), (RES_OUT, runtime)):
            if not os.path.exists(path):
                print("--check：%s 不存在" % path)
                ok = False
                continue
            old = json.load(io.open(path, encoding="utf-8"))
            # `_note` 里带 `--carry` 的条数说明，不参与对账（内容一致即可复现）
            same = all(old.get(k) == doc.get(k) for k in doc if k != "_note")
            print("--check：%s %s" % (path, "一致 ✅" if same else "**已过期** ❌（重跑不带 --check）"))
            ok = ok and same
        return 0 if ok else 1

    write(DATA_OUT, data)
    write(RES_OUT, runtime)
    return 0


if __name__ == "__main__":
    sys.exit(main())
