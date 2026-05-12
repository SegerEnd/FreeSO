#!/usr/bin/env bash
# Build walkby_visitor.iff from walkby_visitor.yaml (skeleton) + walkby_visitor.s (BHAV bodies).
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
out="$here/../../TSOClient/FSO.Content.TSO/Content/Objects/walkby_visitor.iff"
yaml="$here/walkby_visitor.yaml"
asm="$here/walkby_visitor.s"

sims-iff from-yaml "$yaml" -o "$out"

ids=(4097 4098 4099 4100 4101 4102 4103 4104 4105 5000 5001)

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

for id in "${ids[@]}"; do
  awk -v id="$id" '
    /^!macro/,/^!endmacro/ {print; next}
    /^BHAV \[/ { keeping = ($0 ~ ("\\[" id "\\]")) }
    keeping { print }
  ' "$asm" > "$tmpdir/$id.s"
  sims-iff patch "$out" --asm "$tmpdir/$id.s" --id "$id" -o "$out"
done

echo "Built $out ($(stat -f%z "$out" 2>/dev/null || stat -c%s "$out") bytes)"
sims-iff disasm "$out"
