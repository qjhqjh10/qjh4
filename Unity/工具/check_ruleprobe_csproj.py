#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_ruleprobe_csproj.py — 「`A/RuleEngine/Data/*.cs` 与 `工具/ruleprobe/ruleprobe.csproj` 的【文件差集】」同步点闸。

账 `B-34`（2026-11-01 · 第 111 会话 · 执行代理 `B111_E3`）。

## 它治的是什么

`工具/ruleprobe/ruleprobe.csproj` 关掉了 `EnableDefaultCompileItems`，`Data/` 下是**逐个列举**的
（只有 `Core/` 才是 `*.cs` 通配，见 csproj:91 那一行）。⇒ **谁在 `Data/` 新建一个 `.cs` 而不加一行
`<Compile Include=…>`，探针里那个文件就是「不存在」**，而症状是：

  · 最好的情况 = 构建期 `CS0103` 名称 "X"（当场红，好查）；
  · **最坏的情况 = 什么都没发生** —— 那个文件是某个类的 **partial 半边**（加字段 / 加重载），
    而探针**今天还没人用那些成员** ⇒ 探针编得过、跑得动、读数看着全绿，
    只是**它看见的类比产品代码少了几格**。等到某天有人写了 `probe` 代码读那一格，
    才会在**毫不相干**的一次构建里炸出来（同族先例：`A1478` 的 `Stub.cs` 被拿掉 ⇒ 18 个 `CS0103`）。

`B-34` 实测就抓到第一例：`Data/AiActionPartial_B110R3.cs`（`AiAction` 的 partial 半边，
`N07` 加的 `OathChoiceRecorded` / `OathChoice` 两格）**没在 csproj 里**，而构建一直是通过的。
⇒ 已补齐（csproj 里那段注释），本闸把这一类**长期钉住**。

## 怎么用

    python -I -X utf8 d:/4/Unity/工具/check_ruleprobe_csproj.py
    # 只检查。差集非空 ⇒ rc=1 并逐条列出；空 ⇒ rc=0。

    python -I -X utf8 d:/4/Unity/工具/check_ruleprobe_csproj.py --verbose

    # 隔离测试用（⛔ 别拿真 csproj 试）：
    python -I -X utf8 …/check_ruleprobe_csproj.py --csproj <另一份> --data-dir <另一个目录>

## 🔴 三条硬规矩

1. **本脚本【只读】，没有任何写盘的代码路径。** 本项目踩过一次「写盘脚本默认动作」的事故
   （`merge_ledger_parts.py` 不带参就是真合并，冲掉 232 行手工修订）⇒ 这里干脆**不提供 `--fix`**。
   要补 csproj 就人手动加一行（而且**应该**手动加：那是「这个文件能不能进探针」的判断，不是机械动作）。
2. **`--help` 只印用法**，不做任何检查之外的副作用（本来也没有副作用）。
3. **豁免表 = 冻结快照，不是「理由库」。** 表里每一条都有 `reason`；`-` 表示「当时没核」。
   判断口径（写下来免得后人重查）：**进探针的只有「纯解析 / DTO / 纯数据」那一类**；
   `Resources.Load` 的**装载半边**（`*Map.cs` / `TutorialDatabase.cs`）、
   存档与卡组管理（`DeckStore` / `DeckLibrary` / `DeckBuilder`）**一律不进**
   —— 探针的 `Resources` 是桩、恒 `null`，装进来只会让「装载口没有实现」被静默抹平
   （csproj 末尾那段长注释说的就是这件事）。

## 判据（四张表）

| 集合 | 含义 | 非空时的处置 |
|---|---|---|
| `MISSING` = B − A − C | `Data/*.cs` 里有、csproj 没列、也不在豁免表 | 🔴 **失败**（新文件漏列；照 `AiActionPartial_B110R3` 那条判「该不该补」） |
| `GHOST`   = A − B | csproj 列了、盘上没有 | 🔴 **失败**（文件被删/改名 ⇒ 下次构建就是 `CS2001`） |
| `STALE`   = C − B | 豁免表里有、盘上没有 | ⚠️ 告警（文件被删了，表要清；不清会掩盖将来**同名重建**的那一张） |
| `PROMOTED`= C ∩ A | 豁免表说「不进」，csproj 却列着 | ⚠️ 告警（表和事实打架 —— 两份说法打架比没有更糟，铁律 6） |

退出码：有 🔴 ⇒ 1；只有 ⚠️ 或全空 ⇒ 0；脚本自身参数错 ⇒ 2。
"""
import io
import os
import re
import sys

EXIT_OK = 0
EXIT_FAIL = 1
EXIT_USAGE = 2

# ---------------------------------------------------------------------------
#  豁免表 = 冻结快照（2026-11-01 现读的差集）
#     ⚠️ 加/删这一表里的行 = **一次判断**，请在 PR 说明里写清「为什么这个文件不能进探针」。
#     ⚠️ 这张表**不检查**「文件内容有没有变」—— 它只回答「有没有新文件溜过去」。
# ---------------------------------------------------------------------------
ALLOW = {
    # ── 存档 / 卡组管理：要 IO 或玩家存档，探针里没有 ──────────────────────────
    "DeckStore.cs":        "Application.persistentDataPath + JsonUtility（玩家真存档）",
    "DeckLibrary.cs":      "卡组库（多套/改名/复制/删除/选中 + 落盘），要 IO",
    "DeckBuilder.cs":      "玩法线 v1 的牌组构造器；探针自带夹具，不用它",
    "StarterCards.cs":     "自己设计的 26 张测试卡；探针读真卡池",
    "LegacyCardIds.cs":    "存档/回放的旧卡 id 迁移闸，要读玩家数据",
    # ── 「装载半边」：partial 的另一半走 Resources.Load，探针里那是桩 ─────────────
    #     形状一致（见各自文件头）：声明在 Core/，实现半边在 Data/；探针只编 Core/*
    #     ⇒ 「没有实现」被编译器抹掉、探针自动落回兜底（这是**有意**的设计，见 csproj 末尾注释）。
    "ForcedAttackRefundMap.cs": "partial 装载半边（Resources.Load）；探针的 LoadPool 自己灌",
    "OffensiveCardMap.cs":      "partial 装载半边（Resources.Load）",
    "RelatedCardMap.cs":        "partial 装载半边（Resources.Load）",
    "TalentCardMap.cs":         "partial 装载半边（Resources.Load）",
    "TutorialDatabase.cs":      "partial 装载半边（Resources.Load）",
}

CS_PROJ_REL = os.path.join("ruleprobe", "ruleprobe.csproj")
DATA_REL = os.path.join("..", "MyGame", "Assets", "RuleEngine", "Data")

# `<Compile Include="../../MyGame/Assets/RuleEngine/Data/CardDatabase.cs" />`
DATA_INC_RE = re.compile(r'<Compile\s+Include\s*=\s*"([^"]*?/RuleEngine/Data/[^"/]+\.cs)"\s*/?>',
                         re.IGNORECASE)


def script_dir():
    return os.path.dirname(os.path.abspath(__file__))


def parse_args(argv):
    opts = {"csproj": os.path.normpath(os.path.join(script_dir(), CS_PROJ_REL)),
            "data_dir": os.path.normpath(os.path.join(script_dir(), DATA_REL)),
            "verbose": False}
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ("-h", "--help"):
            print_usage()
            sys.exit(EXIT_OK)
        elif a == "--verbose" or a == "-v":
            opts["verbose"] = True
            i += 1
        elif a.startswith("--csproj="):
            opts["csproj"] = a.split("=", 1)[1]
            i += 1
        elif a == "--csproj":
            if i + 1 >= len(argv):
                print("!! `--csproj` 后面缺路径", file=sys.stderr)
                return None
            opts["csproj"] = argv[i + 1]
            i += 2
        elif a.startswith("--data-dir="):
            opts["data_dir"] = a.split("=", 1)[1]
            i += 1
        elif a == "--data-dir":
            if i + 1 >= len(argv):
                print("!! `--data-dir` 后面缺路径", file=sys.stderr)
                return None
            opts["data_dir"] = argv[i + 1]
            i += 2
        else:
            print("!! 不认识的参数：" + a, file=sys.stderr)
            print_usage()
            return None
    return opts


def print_usage():
    print("用法：python -I -X utf8 check_ruleprobe_csproj.py [--verbose] [--csproj <路径>] [--data-dir <路径>]")
    print("")
    print("检查 `A/RuleEngine/Data/*.cs` 与 `工具/ruleprobe/ruleprobe.csproj` 的文件差集。")
    print("  差集非空 ⇒ 逐条列出并 **rc=1**；空 ⇒ rc=0。")
    print("  ⛔ 本脚本**只读**：没有任何写盘路径（不提供 --fix —— 补 csproj 是一次判断，不是机械动作）。")
    print("  `--csproj` / `--data-dir` 只服务隔离测试（⛔ 别拿真 csproj 造差集）。")


def read_text(path):
    """一律按字节读、自己按 \\r\\n / \\n 切 —— 本仓有裸 CR 踩过的先例（`DeckRules.cs` 7 个）。"""
    with io.open(path, "rb") as f:
        b = f.read()
    b = b.replace(b"\r\r\n", b"\r\n").replace(b"\r\n", b"\n")
    return b.decode("utf-8", "replace")


def main(argv):
    opts = parse_args(argv)
    if opts is None:
        return EXIT_USAGE

    csproj = opts["csproj"]
    data_dir = opts["data_dir"]

    if not os.path.isfile(csproj):
        print("!! 找不到 csproj：" + csproj, file=sys.stderr)
        return EXIT_USAGE
    if not os.path.isdir(data_dir):
        print("!! 找不到 Data 目录：" + data_dir, file=sys.stderr)
        return EXIT_USAGE

    # ---- A：csproj 里列出来的 Data 文件（基名） ----
    listed = set()
    text = read_text(csproj)
    for m in DATA_INC_RE.finditer(text):
        listed.add(m.group(1).replace("\\", "/").rsplit("/", 1)[-1])

    # ---- B：盘上真实的 Data/*.cs ----
    actual = set()
    for name in os.listdir(data_dir):
        if name.endswith(".cs") and os.path.isfile(os.path.join(data_dir, name)):
            actual.add(name)

    allowed = set(ALLOW.keys())

    missing = sorted(actual - listed - allowed)     # 🔴 新文件漏列
    ghost = sorted(listed - actual)                 # 🔴 csproj 列了但盘上没有
    stale = sorted(allowed - actual)                # ⚠️ 豁免表过期
    promoted = sorted(allowed & listed)             # ⚠️ 表和事实打架

    print("=== check_ruleprobe_csproj ===")
    print("csproj  : " + csproj)
    print("Data 目录: " + data_dir)
    print("Data/*.cs = %d 个 · csproj 列出 = %d 个 · 豁免表 = %d 条" % (len(actual), len(listed), len(allowed)))

    if opts["verbose"]:
        print("[csproj 列出的 Data 文件]")
        for n in sorted(listed):
            print("   ✓ " + n)
        print("[豁免表]")
        for n in sorted(allowed):
            print("   - " + n + "  —— " + ALLOW[n])
        print("[盘上没有、也不在 csproj、也不在豁免表]（= 空才正常）")
        print("   " + ("（无）" if not missing else ""))

    ok = True

    if missing:
        ok = False
        print("")
        print("🔴 **差集非空**：下面 %d 个 `Data/*.cs` **既没进 csproj、也不在豁免表**：" % len(missing))
        for n in missing:
            print("   ✗ " + n)
        print("   ⇒ 两种可能，必须逐条判（⛔ 别机械补 csproj）：")
        print("      ① **该进**（纯解析 / DTO / 纯数据，例如某个已编类的 partial 半边）"
              " ⇒ 在 `ruleprobe.csproj` 里加一行 `<Compile Include=\"../../MyGame/Assets/RuleEngine/Data/<名>\" />`；")
        print("      ② **不该进**（装载半边 / 要 IO / 探针用不到）"
              " ⇒ 加进本脚本的 `ALLOW` 表并写清 `reason`。")

    if ghost:
        ok = False
        print("")
        print("🔴 **csproj 列了 %d 个盘上不存在的文件**（下次构建就是 `CS2001`/`CS0246`）：" % len(ghost))
        for n in ghost:
            print("   ✗ " + n)

    if stale:
        print("")
        print("⚠️ 豁免表里有 %d 条**盘上已不存在**（文件被删/改名了，表要清；留着会掩盖将来同名重建）：" % len(stale))
        for n in stale:
            print("   ? " + n)

    if promoted:
        print("")
        print("⚠️ 豁免表说「不进探针」，但 csproj **列着** %d 个（两份说法打架，铁律 6）：" % len(promoted))
        for n in promoted:
            print("   ? " + n)

    if ok:
        print("")
        print("✅ 差集为空（豁免表 %d 条%s）。" % (len(allowed), ("，" + str(len(stale)) + " 条过期") if stale else ""))
    return EXIT_OK if ok else EXIT_FAIL


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
