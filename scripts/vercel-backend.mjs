import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const backend = path.join(root, "Backend");
const v = path.join(backend, ".vercel", "project.json");

if (!existsSync(v)) {
  console.error("Manque le lien Vercel : cd Backend && npx vercel link  (projet congogames-backend-cg).");
  process.exit(1);
}

const r = spawnSync("npx", ["--yes", "vercel@latest", "deploy", "--prod"], {
  cwd: backend,
  stdio: "inherit",
  shell: true
});

if (r.status === 0) {
  process.exit(0);
}

console.error("");
console.error("Si l erreur contient  Backend\\Backend  : Vercel -> congogames-backend-cg -> Settings ->");
console.error("  General -> Root Directory : VIDER le champ (laisser vide) puis reessayer ce script.");
console.error("  (Avec un sous-dossier  Backend  dans les reglages, ne pas lancer  vercel  depuis  Backend. )");
process.exit(r.status ?? 1);
