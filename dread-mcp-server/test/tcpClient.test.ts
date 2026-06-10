import * as net from "node:net";
import { AddressInfo } from "node:net";
import { afterEach, describe, expect, it } from "vitest";
import { sendCommand } from "../src/tcpClient.js";

type LineHandler = (line: string, socket: net.Socket) => void;

let server: net.Server | undefined;

/** Start a one-shot fixture server speaking the newline-JSON protocol. */
async function startFixtureServer(onLine: LineHandler): Promise<number> {
  server = net.createServer((socket) => {
    let buffer = "";
    socket.on("data", (chunk) => {
      buffer += chunk.toString("utf-8");
      const idx = buffer.indexOf("\n");
      if (idx !== -1) {
        const line = buffer.slice(0, idx);
        buffer = buffer.slice(idx + 1);
        onLine(line, socket);
      }
    });
  });

  await new Promise<void>((resolve) => server!.listen(0, "127.0.0.1", resolve));
  return (server!.address() as AddressInfo).port;
}

function options(port: number, timeoutMs = 1000) {
  return { host: "127.0.0.1", port, timeoutMs };
}

afterEach(async () => {
  if (server) {
    await new Promise<void>((resolve) => server!.close(() => resolve()));
    server = undefined;
  }
});

describe("sendCommand", () => {
  it("frames the request as one JSON line and returns the parsed response", async () => {
    let received = "";
    const port = await startFixtureServer((line, socket) => {
      received = line;
      socket.write(JSON.stringify({ id: 1, ok: true, data: { pong: true, version: "1.6.1" } }) + "\n");
    });

    const response = await sendCommand("ping", { extra: 1 }, options(port));

    expect(JSON.parse(received)).toEqual({ id: 1, cmd: "ping", data: { extra: 1 } });
    expect(response.ok).toBe(true);
    expect(response.data).toEqual({ pong: true, version: "1.6.1" });
  });

  it("defaults data to an empty object", async () => {
    let received = "";
    const port = await startFixtureServer((line, socket) => {
      received = line;
      socket.write(JSON.stringify({ id: 1, ok: true }) + "\n");
    });

    await sendCommand("ping", undefined, options(port));

    expect(JSON.parse(received).data).toEqual({});
  });

  it("passes through ok:false error responses without rejecting", async () => {
    const port = await startFixtureServer((_line, socket) => {
      socket.write(JSON.stringify({ id: 1, ok: false, error: "unknown command", code: -2 }) + "\n");
    });

    const response = await sendCommand("nope", {}, options(port));

    expect(response.ok).toBe(false);
    expect(response.error).toBe("unknown command");
    expect(response.code).toBe(-2);
  });

  it("reads only the first response line", async () => {
    const port = await startFixtureServer((_line, socket) => {
      socket.write(
        JSON.stringify({ id: 1, ok: true, data: { first: true } }) + "\n"
          + JSON.stringify({ id: 2, ok: true, data: { second: true } }) + "\n",
      );
    });

    const response = await sendCommand("ping", {}, options(port));

    expect(response.data).toEqual({ first: true });
  });

  it("handles a response split across TCP chunks", async () => {
    const port = await startFixtureServer((_line, socket) => {
      const payload = JSON.stringify({ id: 1, ok: true, data: { whole: true } }) + "\n";
      socket.write(payload.slice(0, 10));
      setTimeout(() => socket.write(payload.slice(10)), 20);
    });

    const response = await sendCommand("ping", {}, options(port));

    expect(response.data).toEqual({ whole: true });
  });

  it("rejects on malformed response JSON", async () => {
    const port = await startFixtureServer((_line, socket) => {
      socket.write("not json\n");
    });

    await expect(sendCommand("ping", {}, options(port))).rejects.toThrow(/Failed to parse response/);
  });

  it("rejects when the server never answers (timeout)", async () => {
    const port = await startFixtureServer(() => {
      // swallow the request, never reply
    });

    await expect(sendCommand("ping", {}, options(port, 150))).rejects.toThrow(/timed out after 150ms/);
  });

  it("rejects when the connection is refused", async () => {
    // Bind then immediately close to get a port that refuses connections.
    const probe = net.createServer();
    await new Promise<void>((resolve) => probe.listen(0, "127.0.0.1", resolve));
    const deadPort = (probe.address() as AddressInfo).port;
    await new Promise<void>((resolve) => probe.close(() => resolve()));

    await expect(sendCommand("ping", {}, options(deadPort))).rejects.toThrow(/Connection failed/);
  });

  it("rejects when the server closes without responding", async () => {
    const port = await startFixtureServer((_line, socket) => {
      socket.end();
    });

    await expect(sendCommand("ping", {}, options(port))).rejects.toThrow(
      /Connection closed without receiving a response/,
    );
  });
});
