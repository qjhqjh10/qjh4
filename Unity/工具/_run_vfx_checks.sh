#!/usr/bin/env bash
# _run_vfx_checks.sh —— 特效线两条「跑得起来」的闸（纯 Python，秒级~分钟级，⛔ 不碰 Unity）
#
#   ① V-1 工具/vfx_param_audit.py --check        参数层（prefab 逐字段）+ 材质队列（运行时生效队列）
#   ② V-3 工具/check_vfx_trigger_chain.py --check 触发链表里的「已接」逐条核落点
#
# ⚠ 两个都是**只读**（--check 不写任何文件）；要重生成 TSV 用 `--write`，别在这儿做。
# ⚠ V-3 在「表上写着已接、代码里找不到落点」时按设计返回 1（那是它该报的），
#    所以本脚本**原样透传**两边的退出码：0 = 两条都干净；1 = 有一条报了东西。
# ⚠ ⛔ 不要把 工具/vfx_param_audit.py --self-test 加进来：它**故意**以 1 退出（证明闸有牙）。
set -u
cd "$(dirname "$0")/.." || exit 2        # → d:/4/Unity
PY="${PYTHON:-python}"
rc1=0
rc3=0

echo "== ① V-1 特效参数对账（--check，只算不写） =="
"$PY" -I -X utf8 工具/vfx_param_audit.py --check || rc1=$?
echo
echo "== ② V-3 特效触发链（--check，只读） =="
"$PY" -I -X utf8 工具/check_vfx_trigger_chain.py --check || rc3=$?
echo
echo "== 汇总：V-1 rc=$rc1 · V-3 rc=$rc3 =="
if [ "$rc1" -ne 0 ] || [ "$rc3" -ne 0 ]; then
  exit 1
fi
exit 0
