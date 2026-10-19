# W57 · 工具线 · `_loc_official/*.py` 语法收尾

日期：2026-10-19 · 代理：TOOL-10 · 范围：`d:/4/Unity/工具/_loc_official/*.py`（只改语法/注释位置）

## 1. 逐文件编译结果（`python -I -m py_compile`）

| 文件 | 改前 | 改后 |
|---|---|---|
| `gen_official_names.py` | OK | OK |
| `loc_diff.py` | **FAIL — line 3, `SyntaxError: invalid character '：' (U+FF1A)`** | OK |
| `loc_revert_clash.py` | **FAIL — line 12, 同上** | OK |
| `loc_use_official.py` | **FAIL — line 14, 同上** | OK |
| `scan_stale_cardname.py` | OK | OK |
| `scan_stale_loc.py` | OK | OK |
| `w4_fix_comments.py` | OK | OK |
| `w4_fix_en.py` | OK | OK |
| `w4_fix_values.py` | OK | OK |
| `w4_gen_battle_block.py` | OK | OK |
| `w4_lib.py` | OK | OK |
| `w4_probe.py` | OK | OK |
| `w4_verify.py` | OK | OK |

改后 13/13 通过（循环取真实退出码，失败数 = 0）。

## 2. 改动清单（三处，均为同一句「落盘位置」被复制到 docstring 之外）

- `d:/4/Unity/工具/_loc_official/loc_revert_clash.py:12` —— 把 `**落盘位置**：…（判据脚本）。` 一行移进模块 docstring（`"""` 闭合引号之前），前面空一行。
- `d:/4/Unity/工具/_loc_official/loc_diff.py:3` —— 同上；原单行 docstring 改为多行，该句作为 docstring 第二段。
- `d:/4/Unity/工具/_loc_official/loc_use_official.py:14` —— 同上，移进 docstring 末尾（`--apply` 用法说明之后）。

未改动任何代码、常量、judgment/输出；`git diff --numstat`：3 / 1 · 2 / 1 · 2 / 1（无整篇重写）。三个文件改后仍为纯 LF（CRLF=0，二进制核对）。

## 3. 运行确认（C）

- `python -I -X utf8 d:/4/Unity/工具/_loc_official/loc_revert_clash.py --help` → **rc=0**，进入其默认 check（只读）分支，输出「要暂缓换回的键：5 个…（--check，没写文件）」，**未写任何文件**。
- 顺带只读跑了两支同样修过的脚本：
  - `loc_diff.py` → rc=0，正常列出中文差异。
  - `loc_use_official.py --check` → rc=0，输出「要改 44 条 · 已相同 395 · 官方表没值 241 · 复杂形态跳过 3 · 要改调用点的 1」。

## 4. 没查清的部分

无（三个报错文件全部定位并修好，全目录语法清零）。

## 5. 顺手发现

1. `loc_use_official.py` 的 docstring「用法」两行（现在位于 `:11-12`）仍写旧路径 `d:/4/_tmp_a1070/loc_use_official.py`，而文件已搬到 `Unity/工具/_loc_official/`（同一次搬家留下的）。属文档过期，不影响运行；本次未改（只做语法收尾）。
2. 三个文件那句「落盘位置」是**逐字复制**的（连标点都一样），这次也是三处一起坏 —— 以后搬家类注释若再复制，建议只留一处指针。
3. `py_compile` 会写 `__pycache__/`（该目录本就存在、未被 git 跟踪）；`git status` 里被跟踪的文件只有上述 3 个 `.py` 有改动。
