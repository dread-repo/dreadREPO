import { describe, expect, it } from "vitest";
import { formatLogsText, formatPatchesText } from "../src/format.js";

describe("formatLogsText", () => {
  it("reads the PascalCase field names Unity JsonUtility emits", () => {
    const text = formatLogsText([
      { Level: "Info", Message: "Dread v1.6.1 loaded.", Timestamp: "2026-06-10T12:00:00.0000000Z" },
      { Level: "Warning", Message: "stub probe failed", Timestamp: "2026-06-10T12:00:01.0000000Z" },
    ]);

    expect(text).toContain("# Recent Dread Mod Logs (2 entries)");
    expect(text).toContain("[2026-06-10T12:00:00.0000000Z] [Info] Dread v1.6.1 loaded.");
    expect(text).toContain("[2026-06-10T12:00:01.0000000Z] [Warning] stub probe failed");
  });

  it("falls back to camelCase keys", () => {
    const text = formatLogsText([
      { level: "Error", message: "legacy shape", timestamp: "t0" },
    ]);

    expect(text).toContain("[t0] [Error] legacy shape");
  });

  it("defaults missing fields instead of printing undefined", () => {
    const text = formatLogsText([{}]);

    expect(text).toContain("[] [Info] ");
    expect(text).not.toContain("undefined");
  });

  it("reports when there are no entries", () => {
    expect(formatLogsText([])).toBe("No log entries found.");
  });
});

describe("formatPatchesText", () => {
  it("formats the flat numeric counts the C# PatchEntry exposes", () => {
    const text = formatPatchesText([
      {
        method: "EnemyNavMeshAgent.Awake()",
        prefixes: 0,
        postfixes: 1,
        transpilers: 0,
        finalizers: 0,
        owners: ["com.elytraking.dread"],
      },
    ]);

    expect(text).toContain("# Dread Mod Harmony Patches (1 total)");
    expect(text).toContain("## EnemyNavMeshAgent.Awake()");
    expect(text).toContain("- **Types**: Prefix(0), Postfix(1), Transpiler(0), Finalizer(0)");
    expect(text).toContain("- **Owners**: com.elytraking.dread");
  });

  it("always emits the Types line, even when counts are missing", () => {
    const text = formatPatchesText([{ method: "Some.Method()" }]);

    expect(text).toContain("- **Types**: Prefix(0), Postfix(0), Transpiler(0), Finalizer(0)");
  });

  it("omits the Owners line when no owners are reported", () => {
    const text = formatPatchesText([{ method: "Some.Method()", owners: [] }]);

    expect(text).not.toContain("**Owners**");
  });

  it("reports when there are no patches", () => {
    expect(formatPatchesText([])).toBe("No Harmony patches found.");
  });
});
