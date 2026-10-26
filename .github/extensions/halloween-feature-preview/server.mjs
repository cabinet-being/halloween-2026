import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

const pagePath = fileURLToPath(new URL("./page.html", import.meta.url));
const page = await readFile(pagePath, "utf8");

const send = (response, status, contentType, body) => {
  response.writeHead(status, {
    "content-type": contentType,
    "cache-control": "no-store",
    "x-content-type-options": "nosniff",
  });
  response.end(body);
};

const readEmptyBody = async (request) => {
  let size = 0;
  for await (const chunk of request) {
    size += chunk.length;
    if (size > 1024) {
      return false;
    }
  }

  return size === 0;
};

export const createPreviewInstance = async () => {
  let count = 0;
  const server = createServer(async (request, response) => {
    const url = new URL(request.url ?? "/", "http://127.0.0.1");

    if (request.method === "GET" && url.pathname === "/") {
      send(response, 200, "text/html; charset=utf-8", page);
      return;
    }

    if (request.method === "GET" && url.pathname === "/api/state") {
      send(response, 200, "application/json; charset=utf-8", JSON.stringify({ count }));
      return;
    }

    if (
      request.method === "POST" &&
      (url.pathname === "/api/increment" || url.pathname === "/api/reset")
    ) {
      if (!(await readEmptyBody(request))) {
        send(response, 400, "application/json; charset=utf-8", '{"error":"Request body must be empty."}');
        return;
      }

      count = url.pathname === "/api/increment" ? count + 1 : 0;
      send(response, 200, "application/json; charset=utf-8", JSON.stringify({ count }));
      return;
    }

    send(response, 404, "text/plain; charset=utf-8", "Not found");
  });

  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });

  const address = server.address();
  if (!address || typeof address === "string") {
    await new Promise((resolve) => server.close(resolve));
    throw new Error("The preview server did not provide a TCP address.");
  }

  return {
    url: `http://127.0.0.1:${address.port}/`,
    isRunning: () => server.listening,
    increment: () => ({ count: ++count }),
    reset: () => {
      count = 0;
      return { count };
    },
    close: () =>
      new Promise((resolve, reject) => {
        server.close((error) => (error ? reject(error) : resolve()));
      }),
  };
};
