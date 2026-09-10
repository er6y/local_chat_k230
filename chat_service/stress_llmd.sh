#!/bin/sh
# stress_llmd.sh — 滑窗压力测试：直接驱动 llmd，长对话逼出滑窗淘汰
# 用法: sh stress_llmd.sh <起始轮号>   （轮号用于生成唯一 out 文件名，避免 DONE 误匹配）
RUN=/tmp/chat
LOG=$RUN/llm_out.log
N=${1:-1}

ask() {  # ask <turn> <text>
  out=$RUN/s_$1.txt
  rm -f "$out"
  printf '%s\t128\t%s\n' "$out" "$2" > $RUN/llm_in
  i=0
  while [ $i -lt 150 ]; do
    if grep -q "^DONE $out " $LOG 2>/dev/null; then break; fi
    sleep 1; i=$((i+1))
  done
  echo "=== turn $1 (${i}s) ==="
  head -c 300 "$out" 2>/dev/null; echo; echo
}

ask $N "我叫王建国，今年四十五岁，住在杭州，家里养了一只金毛犬叫豆豆，请记住我的信息，然后简单介绍一下你自己"
ask $((N+1)) "我养的是什么狗？叫什么名字？我今年多大？住在哪里？"
ask $((N+2)) "请你详细解释一下鸡兔同笼问题的解法，包括假设法、方程法两种思路，并且各举一个完整的例子，步骤要写清楚"
ask $((N+3)) "把刚才鸡兔同笼的解释再扩展一下，加上抬脚法，三种方法对比一下优缺点"
ask $((N+4)) "豆豆今天不爱吃饭，金毛犬挑食一般有哪些原因？该怎么处理？"
ask $((N+5)) "回到豆豆的问题，你觉得我应该先检查哪一项？为什么？"
ask $((N+6)) "请把前面我们聊过的内容总结一下：我的名字、狗的名字、还有鸡兔同笼有哪几种解法"
ask $((N+7)) "再用一句话说一遍：鸡兔同笼抬脚法是怎么回事？"
echo "STRESS_ROUND_DONE"
