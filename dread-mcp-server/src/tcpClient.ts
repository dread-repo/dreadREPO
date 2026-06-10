import * as net from "node:net";

export interface DreadResponse {
  id: number;
  ok: boolean;
  data?: unknown;
  error?: string;
  code?: number;
}

export interface TcpClientOptions {
  host: string;
  port: number;
  timeoutMs: number;
}

/**
 * Send one newline-framed JSON command to the Dread debug server and read the
 * first response line (the server answers one line per queued command).
 */
export async function sendCommand(
  cmd: string,
  data: unknown,
  options: TcpClientOptions,
): Promise<DreadResponse> {
  return new Promise((resolve, reject) => {
    const socket = new net.Socket();
    let buffer = "";
    let resolved = false;

    const timer = setTimeout(() => {
      socket.destroy();
      reject(new Error(`Command timed out after ${options.timeoutMs}ms`));
    }, options.timeoutMs);

    socket.connect(options.port, options.host, () => {
      const payload = JSON.stringify({
        id: 1,
        cmd,
        data: data ?? {},
      }) + "\n";
      socket.write(payload);
    });

    socket.on("data", (chunk) => {
      if (resolved) return;
      buffer += chunk.toString("utf-8");

      const idx = buffer.indexOf("\n");
      if (idx !== -1) {
        resolved = true;
        const line = buffer.slice(0, idx);
        clearTimeout(timer);
        socket.destroy();

        try {
          resolve(JSON.parse(line) as DreadResponse);
        } catch (e) {
          reject(new Error(`Failed to parse response: ${line}`));
        }
      }
    });

    socket.on("error", (err) => {
      if (resolved) return;
      resolved = true;
      clearTimeout(timer);
      reject(new Error(`Connection failed: ${err.message}. Is the Dread debug server running on ${options.host}:${options.port}?`));
    });

    socket.on("close", () => {
      if (!resolved) {
        resolved = true;
        clearTimeout(timer);
        reject(new Error("Connection closed without receiving a response"));
      }
    });
  });
}
