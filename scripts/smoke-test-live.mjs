const API_URL = process.env.CLOUD_API_URL || "https://congogames.vercel.app";
const WS_URL = process.env.CLOUD_WS_URL || "wss://congogames-ws-production.up.railway.app";

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(payload)
  });
  const text = await response.text();
  let data = {};
  try {
    data = JSON.parse(text);
  } catch {
    data = { raw: text };
  }
  return { status: response.status, data };
}

async function getJson(url) {
  const response = await fetch(url);
  const text = await response.text();
  let data = {};
  try {
    data = JSON.parse(text);
  } catch {
    data = { raw: text };
  }
  return { status: response.status, data };
}

async function testWebSocket() {
  if (typeof WebSocket === "undefined") {
    throw new Error("WebSocket API unavailable on this Node runtime");
  }

  return new Promise((resolve, reject) => {
    const messages = [];
    const ws = new WebSocket(WS_URL);
    let done = false;

    ws.addEventListener("open", async () => {
      try {
        await postJson(`${WS_URL.replace("wss://", "https://")}/events/chat`, { user: "smoke", message: "A" });
        setTimeout(() => {
          done = true;
          ws.close();
          resolve({ ok: true, messages });
        }, 1500);
      } catch (error) {
        reject(error);
      }
    });

    ws.addEventListener("message", (m) => {
      messages.push(typeof m.data === "string" ? m.data : String(m.data));
    });

    ws.addEventListener("error", (error) => reject(error));
    setTimeout(() => {
      if (!done) reject(new Error("WS timeout"));
    }, 10000);
  });
}

async function main() {
  const healthApi = await getJson(`${API_URL}/health`);
  const healthWs = await getJson(`${WS_URL.replace("wss://", "https://")}/health`);
  const questionApi = await postJson(`${API_URL}/question/generate`, { language: "fr" });
  const wsResult = await testWebSocket();

  const result = {
    apiUrl: API_URL,
    wsUrl: WS_URL,
    checks: {
      apiHealth: healthApi.status === 200,
      wsHealth: healthWs.status === 200,
      apiQuestion: questionApi.status === 200 && Boolean(questionApi.data?.question),
      wsMessageFlow: wsResult.ok && wsResult.messages.length > 0
    },
    sample: {
      apiQuestion: questionApi.data?.question?.question || null,
      wsMessages: wsResult.messages.slice(0, 2)
    }
  };

  console.log(JSON.stringify(result, null, 2));

  const allOk = Object.values(result.checks).every(Boolean);
  if (!allOk) process.exit(1);
}

main().catch((error) => {
  console.error(error.message || String(error));
  process.exit(1);
});
