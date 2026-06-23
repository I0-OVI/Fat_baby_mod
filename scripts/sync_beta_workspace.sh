#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STABLE_DIR="$ROOT_DIR/workspace/mod"
BETA_DIR="$ROOT_DIR/workspace/mod-beta"

if [[ ! -d "$STABLE_DIR/ModCode" ]]; then
  echo "Missing stable ModCode: $STABLE_DIR/ModCode" >&2
  exit 1
fi

mkdir -p "$BETA_DIR"

echo "Syncing ModCode from stable -> beta..."
rsync -a --delete --exclude="/Commands/" "$STABLE_DIR/ModCode/" "$BETA_DIR/ModCode/"

echo "Syncing shared assets from stable -> beta..."
"$ROOT_DIR/scripts/sync_beta_assets.sh"

for file in icon.svg icon.svg.import .editorconfig .gitattributes; do
  if [[ -f "$STABLE_DIR/$file" ]]; then
    cp "$STABLE_DIR/$file" "$BETA_DIR/$file"
  fi
done

mkdir -p "$BETA_DIR/ModCode/Commands"

if [[ ! -f "$BETA_DIR/ModCode/Commands/ModPowerCmd.cs" ]]; then
  cat > "$BETA_DIR/ModCode/Commands/ModPowerCmd.cs" <<'EOF'
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Mod.ModCode.Commands;

/// <summary>
/// Compatibility helpers for game builds where <see cref="PowerCmd.Apply"/> requires a <see cref="PlayerChoiceContext"/>.
/// </summary>
internal static class ModPowerCmd
{
    internal static Task<T?> Apply<T>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false
    ) where T : PowerModel =>
        PowerCmd.Apply<T>(CreateContext(applier ?? target), target, amount, applier, cardSource, silent);

    internal static Task<IReadOnlyList<T>> Apply<T>(
        IEnumerable<Creature> targets,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false
    ) where T : PowerModel
    {
        Creature contextCreature = applier ?? targets.First();
        return PowerCmd.Apply<T>(CreateContext(contextCreature), targets, amount, applier, cardSource, silent);
    }

    internal static Task<int> ModifyAmount(
        PowerModel power,
        decimal delta,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false
    )
    {
        Creature contextCreature = applier ?? power.Owner ?? throw new System.InvalidOperationException("Power has no owner.");
        return PowerCmd.ModifyAmount(CreateContext(contextCreature), power, delta, applier, cardSource, silent);
    }

    private static HookPlayerChoiceContext CreateContext(Creature creature) =>
        new(creature.Player ?? throw new System.InvalidOperationException("Creature has no player."), 0UL, GameActionType.Combat);
}
EOF
fi

python3 - <<'PY' "$BETA_DIR/ModCode"
from pathlib import Path
import re
import sys

mod_code_dir = Path(sys.argv[1])

main_file = mod_code_dir / "MainFile.cs"
main_text = main_file.read_text(encoding="utf-8")
main_patched = main_text.replace('public const string ModId = "fat_baby";', 'public const string ModId = "fat_baby_beta";')
if main_patched == main_text and 'public const string ModId = "fat_baby_beta";' not in main_text:
    raise SystemExit(f"Could not patch beta ModId in {main_file}")
main_file.write_text(main_patched, encoding="utf-8")
print(f"Patched ModId in {main_file}")

patched_power_files = []
for path in sorted(mod_code_dir.rglob("*.cs")):
    if "Commands" in path.relative_to(mod_code_dir).parts:
        continue

    text = path.read_text(encoding="utf-8")
    patched = text.replace("ModModPowerCmd.", "ModPowerCmd.")
    patched = re.sub(r"(?<!Mod)PowerCmd\.Apply<", "ModPowerCmd.Apply<", patched)
    patched = re.sub(r"(?<!Mod)PowerCmd\.ModifyAmount\(", "ModPowerCmd.ModifyAmount(", patched)
    patched = re.sub(r"(?<!I)CombatState\\? combatState = creature\\.CombatState;", "ICombatState? combatState = creature.CombatState;", patched)
    patched = re.sub(r"private static bool IsGroupElite\\((?<!I)CombatState\\? combatState\\)", "private static bool IsGroupElite(ICombatState? combatState)", patched)
    if patched == text:
        continue

    if "using Mod.ModCode.Commands;" not in patched:
        lines = patched.splitlines()
        insert_at = 0
        for index, line in enumerate(lines):
            if line.startswith("using "):
                insert_at = index + 1
        lines.insert(insert_at, "using Mod.ModCode.Commands;")
        patched = "\n".join(lines) + ("\n" if text.endswith("\n") else "")

    path.write_text(patched, encoding="utf-8")
    patched_power_files.append(path.relative_to(mod_code_dir).as_posix())

if patched_power_files:
    print(f"Patched beta PowerCmd compatibility in {len(patched_power_files)} file(s)")
PY

echo "Synced stable workspace -> beta workspace"
echo "Kept beta-only files: GameRefs.props, mod.csproj, mod.json, mod.sln, project.godot, CHANNEL, ModCode/Commands/ModPowerCmd.cs"
