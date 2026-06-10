/**
 * Text-mode formatters for Dread debug server responses.
 *
 * Field-name note: Unity's JsonUtility serializes the C# DTO field names
 * verbatim, so log entries arrive PascalCase (Level, Message, Timestamp) and
 * patches arrive with flat numeric counts (prefixes, postfixes, transpilers,
 * finalizers) plus an owners array. See Systems/Debug/DebugServerSystem.cs.
 */

export function formatLogsText(entries: Array<Record<string, unknown>>): string {
  if (entries.length === 0) {
    return "No log entries found.";
  }

  const lines = [`# Recent Dread Mod Logs (${entries.length} entries)`, ""];
  for (const entry of entries) {
    const ts = entry.Timestamp ?? entry.timestamp ?? "";
    const lvl = entry.Level ?? entry.level ?? "Info";
    const msg = entry.Message ?? entry.message ?? "";
    lines.push(`[${ts}] [${lvl}] ${msg}`);
  }
  return lines.join("\n");
}

export function formatPatchesText(patches: Array<Record<string, unknown>>): string {
  if (patches.length === 0) {
    return "No Harmony patches found.";
  }

  const count = (v: unknown) => (typeof v === "number" ? v : 0);
  const lines = [`# Dread Mod Harmony Patches (${patches.length} total)`, ""];
  for (const patch of patches) {
    lines.push(`## ${patch.method ?? "unknown"}`);
    lines.push(`- **Types**: Prefix(${count(patch.prefixes)}), Postfix(${count(patch.postfixes)}), Transpiler(${count(patch.transpilers)}), Finalizer(${count(patch.finalizers)})`);
    const owners = patch.owners as string[] ?? [];
    if (owners.length > 0) {
      lines.push(`- **Owners**: ${owners.join(", ")}`);
    }
    lines.push("");
  }
  return lines.join("\n");
}
