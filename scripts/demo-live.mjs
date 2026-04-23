const DEFAULT_PORTS = [3000, 3001, 3002, 3003, 3004, 3005, 3006, 3007, 3008, 3009, 3010];
/** 127.0.0.1 d'abord (évite l’IPv6 ::1 / localhost sur Windows quand le serveur n’écoute qu’en IPv4). */
const DEFAULT_HOSTS = ["127.0.0.1", "localhost"];

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function getJson(url) {
  let response;
  try {
    response = await fetch(url);
  } catch (e) {
    const m = e && e.message ? e.message : String(e);
    throw new Error(`GET ${url} — ${m} (backend lance ? memes hote et port qu’au log HTTP ?)`);
  }
  if (!response.ok) throw new Error(`GET ${url} failed (${response.status})`);
  return response.json();
}

async function postJson(url, payload) {
  let response;
  try {
    response = await fetch(url, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload)
    });
  } catch (e) {
    const m = e && e.message ? e.message : String(e);
    throw new Error(`POST ${url} — ${m}`);
  }
  const text = await response.text();
  if (!response.ok) throw new Error(`POST ${url} failed (${response.status}): ${text}`);
  try {
    return JSON.parse(text);
  } catch {
    return { raw: text };
  }
}

async function resolveBaseUrl() {
  if (process.env.DEMO_BASE_URL) return process.env.DEMO_BASE_URL;

  for (const port of DEFAULT_PORTS) {
    for (const host of DEFAULT_HOSTS) {
      const candidate = `http://${host}:${port}`;
      try {
        await getJson(`${candidate}/health`);
        return candidate;
      } catch {
        // autre hôte / port
      }
    }
  }
  throw new Error(
    "Aucun backend local detecte (ports 3000-3010). Lance `npm run start-all` et verifie le port affiche (ex. 3000)."
  );
}

async function sendStep(baseUrl, step) {
  if (step.type === "question") {
    await postJson(`${baseUrl}/question/generate`, { language: step.language || "fr" });
    return `Question (${step.language || "fr"})`;
  }
  if (step.type === "chat") {
    await postJson(`${baseUrl}/events/chat`, { user: step.user, message: step.message });
    return `Chat ${step.user}: ${step.message}`;
  }
  if (step.type === "gift") {
    await postJson(`${baseUrl}/events/gift`, { user: step.user, giftName: step.giftName });
    return `Gift ${step.user}: ${step.giftName}`;
  }
  throw new Error(`Type de step inconnu: ${step.type}`);
}

function buildDemoSteps() {
  return [
    { type: "question", language: "fr" },
    { type: "chat", user: "brazza_king", message: "A" },
    { type: "chat", user: "mama_kituba", message: "B" },
    { type: "chat", user: "lingala_star", message: "D" },
    { type: "gift", user: "supporter01", giftName: "Rose" },
    { type: "question", language: "fr" },
    { type: "chat", user: "brazza_king", message: "B" },
    { type: "chat", user: "mama_kituba", message: "B" },
    { type: "gift", user: "supporter01", giftName: "Finger Heart" },
    { type: "chat", user: "coach_quiz", message: "A" },
    { type: "question", language: "fr" },
    { type: "chat", user: "brazza_king", message: "C" },
    { type: "chat", user: "mama_kituba", message: "D" },
    { type: "gift", user: "top_donateur", giftName: "Lion" },
    { type: "chat", user: "lingala_star", message: "B" },
    { type: "question", language: "fr" },
    { type: "chat", user: "brazza_king", message: "B" },
    { type: "gift", user: "top_donateur", giftName: "Galaxy" },
    { type: "chat", user: "mama_kituba", message: "A" },
    { type: "chat", user: "lingala_star", message: "B" }
  ];
}

async function main() {
  const baseUrl = await resolveBaseUrl();
  const steps = buildDemoSteps();
  const delayMs = Number(process.env.DEMO_DELAY_MS || 700);

  console.log(`Demo live start -> ${baseUrl}`);
  console.log(`Evenements: ${steps.length}, delai: ${delayMs}ms`);

  for (let i = 0; i < steps.length; i++) {
    const label = await sendStep(baseUrl, steps[i]);
    console.log(`[${i + 1}/${steps.length}] ${label}`);
    await sleep(delayMs);
  }

  console.log("Demo live terminee.");
}

main().catch((error) => {
  const extra = error.cause ? ` | cause: ${error.cause}` : "";
  console.error((error.message || String(error)) + extra);
  process.exit(1);
});
