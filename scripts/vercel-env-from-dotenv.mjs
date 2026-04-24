/**
 * Pousse des clés de Backend/.env vers Vercel (Production) sans afficher les valeurs.
 * Usage (racine du dépôt) : node scripts/vercel-env-from-dotenv.mjs
 */
import { readFileSync, existsSync } from "node:fs";
import { spawnSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(__dirname, "..");
const envPath = path.join(root, "Backend", ".env");
const vjson = path.join(root, "Backend", ".vercel", "project.json");

const KEYS = [
  "ELEVENLABS_API_KEY",
  "ELEVENLABS_VOICE_ID",
  "OPENAI_API_KEY",
];

function parseDotenv(content) {
  const out = {};
  for (const line of content.split(/\r?\n/)) {
    const t = line.trim();
    if (!t || t.startsWith("#")) continue;
    const i = t.indexOf("=");
    if (i <= 0) continue;
    const k = t.slice(0, i).trim();
    let v = t.slice(i + 1);
    if ((v.startsWith('"') && v.endsWith('"')) || (v.startsWith("'") && v.endsWith("'"))) {
      v = v.slice(1, -1);
    }
    out[k] = v;
  }
  return out;
}

if (!existsSync(envPath)) {
  console.error("Manque " + envPath);
  process.exit(1);
}
if (!existsSync(vjson)) {
  console.error("Manque " + vjson);
  process.exit(1);
}

const { orgId, projectId } = JSON.parse(readFileSync(vjson, "utf8"));
const local = parseDotenv(readFileSync(envPath, "utf8"));
const baseEnv = {
  ...process.env,
  VERCEL_ORG_ID: orgId,
  VERCEL_PROJECT_ID: projectId,
};

for (const key of KEYS) {
  const value = local[key];
  if (!value || !String(value).trim()) {
    console.warn("Ignoré (vide) : " + key);
    continue;
  }
  // stdin évite l’échappement Windows / shell sur des clés longues ; pas de --value
  const r = spawnSync(
    "npx",
    [
      "--yes",
      "vercel@latest",
      "env",
      "add",
      key,
      "production",
      "--yes",
      "--sensitive",
      "--force",
    ],
    {
      cwd: root,
      env: baseEnv,
      input: value,
      encoding: "utf8",
      stdio: ["pipe", "inherit", "inherit"],
      shell: true,
    }
  );
  if (r.status !== 0) {
    if (r.error) console.error(r.error);
    process.exit(r.status ?? 1);
  }
  console.log("OK : " + key + " (production)");
}
