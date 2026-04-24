import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const backend = path.join(root, "Backend");
const v = path.join(backend, ".vercel", "project.json");

if (!existsSync(v)) {
  console.error("Manque le lien Vercel : cd Backend && npx vercel link  (projet congogames-backend-cg).");
  console.error("Monorepo (1 Git, 2 projets Vercel) : docs/VERCEL_MONOREPO.md");
  process.exit(1);
}

let orgId;
let projectId;
try {
  const j = JSON.parse(readFileSync(v, "utf8"));
  orgId = j.orgId;
  projectId = j.projectId;
} catch {
  console.error("Fichier illisible : " + v);
  process.exit(1);
}
if (!orgId || !projectId) {
  console.error("Manque orgId ou projectId dans " + v);
  process.exit(1);
}

// Depuis la racine du dépôt + org/project explicites : évite l’erreur Backend\Backend
// (Root Directory = Backend sur Vercel + cwd = Backend double le chemin).
const r = spawnSync("npx", ["--yes", "vercel@latest", "deploy", "--prod"], {
  cwd: root,
  stdio: "inherit",
  shell: true,
  env: { ...process.env, VERCEL_ORG_ID: orgId, VERCEL_PROJECT_ID: projectId }
});

if (r.status === 0) {
  process.exit(0);
}

console.error("");
console.error("Si l erreur contient encore Backend\\Backend  : Vercel -> congogames-backend-cg ->");
console.error("  General -> verifie le Root Directory (souvent Backend) ; deploiement reussi avec ce script = pas besoin de vider le champ en general.");
console.error("Voir : docs/VERCEL_MONOREPO.md");
process.exit(r.status ?? 1);
