const DEFAULT_PORTS = [3000, 3001, 3002, 3003, 3004];

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function getJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`GET ${url} failed (${response.status})`);
  return response.json();
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(payload)
  });
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
    const candidate = `http://localhost:${port}`;
    try {
      await getJson(`${candidate}/health`);
      return candidate;
    } catch {
      // continue
    }
  }
  throw new Error("Aucun backend local detecte (ports 3000-3004). Lance `npm run start-all`.");
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
  console.error(error.message || String(error));
  process.exit(1);
});
