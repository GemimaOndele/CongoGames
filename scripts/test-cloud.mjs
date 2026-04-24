const base = process.env.CLOUD_API_URL || "https://congogames.vercel.app";

async function ping(path, init) {
  const res = await fetch(`${base}${path}`, init);
  const data = await res.json();
  return { status: res.status, data };
}

const health = await ping("/health");
const question = await ping("/question/generate", {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({ language: "fr" })
});

console.log(JSON.stringify({ base, health, question }, null, 2));
