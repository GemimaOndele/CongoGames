import { readdir } from "node:fs/promises";
import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const targetDir = path.join(root, "UnityProject", "Assets", "StreamingAssets", "Theme", "playlist");

function runFfmpegDecodeCheck(filePath) {
  return new Promise((resolve) => {
    const args = [
      "-v", "error",
      "-xerror",
      "-i", filePath,
      "-t", "3",
      "-f", "null",
      "-"
    ];
    const p = spawn("ffmpeg", args, { windowsHide: true });
    let stderr = "";
    p.stderr.on("data", (d) => {
      stderr += d.toString();
    });
    p.on("error", (err) => {
      resolve({ ok: false, reason: "ffmpeg_error: " + err.message });
    });
    p.on("close", (code) => {
      if (code === 0) resolve({ ok: true, reason: "" });
      else resolve({ ok: false, reason: stderr.trim() || "decode_failed (exit " + code + ")" });
    });
  });
}

const files = (await readdir(targetDir).catch(() => []))
  .filter((f) => /\.mp3$/i.test(f) && !/\.reenc\.tmp\.mp3$/i.test(f))
  .sort((a, b) => a.localeCompare(b, "fr", { sensitivity: "base" }));

if (files.length === 0) {
  console.log("Aucun mp3 trouvé dans:", targetDir);
  process.exit(0);
}

const rejected = [];
for (let i = 0; i < files.length; i++) {
  const name = files[i];
  const full = path.join(targetDir, name);
  const check = await runFfmpegDecodeCheck(full);
  if (!check.ok) {
    rejected.push({ file: name, reason: check.reason });
  }
}

console.log("Vérification audio blind:", files.length, "fichiers scannés.");
if (rejected.length === 0) {
  console.log("OK: aucun fichier rejeté.");
} else {
  console.log("FICHIERS REJETÉS:", rejected.length);
  for (const r of rejected) {
    console.log("- " + r.file + " | " + r.reason);
  }
  process.exitCode = 2;
}
