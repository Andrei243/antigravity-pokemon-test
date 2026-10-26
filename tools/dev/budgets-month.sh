#!/usr/bin/env bash
# The nightly's 30 % rule (plan 16 · T10): tonight's budgets (a file of JSON lines written by BudgetTests with
# BUDGETS_JSON set, each {"what": ..., "ms": ...}) held to the month's. For each operation, the median of the nights
# kept in <nights dir> over the last 30 files (tonight's left out); tonight's figure more than 30 % over it fails.
# An operation with fewer than three nights behind it is only reported. Needs jq. Exits 1 on a failure.
#
#   tools/dev/budgets-month.sh <tonight.jsonl> <nights dir>
set -euo pipefail
tonight="$1"
nights="$2"
limit="${BUDGETS_RULE:-1.30}"
[ -f "$tonight" ] || { echo "No budgets were recorded tonight ($tonight)."; exit 1; }
mkdir -p "$nights"
mapfile -t past < <(ls -1 "$nights"/*.jsonl 2>/dev/null | grep -vxF "$tonight" | sort | tail -n 30 || true)
failed=0
while IFS=$'\t' read -r what ms; do
  if [ "${#past[@]}" -eq 0 ]; then
    printf '%-60s %10.2f   (no nights behind it yet)\n' "$what" "$ms"
    continue
  fi
  values=$(cat "${past[@]}" | jq -r --arg w "$what" 'select(.what == $w) | .ms' | sort -g)
  count=$(printf '%s\n' "$values" | grep -c . || true)
  if [ "$count" -lt 3 ]; then
    printf '%-60s %10.2f   (%s night(s) behind it)\n' "$what" "$ms" "$count"
    continue
  fi
  median=$(printf '%s\n' "$values" | awk '{ a[NR] = $1 } END { if (NR % 2) print a[(NR + 1) / 2]; else print (a[NR / 2] + a[NR / 2 + 1]) / 2 }')
  verdict=$(awk -v t="$ms" -v m="$median" -v l="$limit" 'BEGIN { if (m > 0 && t > m * l) print "OVER"; else print "ok" }')
  printf '%-60s %10.2f   month %10.2f   %s\n' "$what" "$ms" "$median" "$verdict"
  if [ "$verdict" = "OVER" ]; then failed=1; fi
done < <(jq -r '[.what, (.ms | tostring)] | @tsv' "$tonight")
if [ "$failed" -ne 0 ]; then
  echo "A budget is more than $(awk -v l="$limit" 'BEGIN { printf "%d", (l - 1) * 100 + 0.5 }') % over the month's median."
  exit 1
fi
echo "Every budget is within $(awk -v l="$limit" 'BEGIN { printf "%d", (l - 1) * 100 + 0.5 }') % of the month's median."
