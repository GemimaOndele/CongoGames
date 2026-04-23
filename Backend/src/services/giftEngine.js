import fs from "node:fs";

export class GiftEngine {
  constructor(configPath) {
    this.config = JSON.parse(fs.readFileSync(configPath, "utf-8"));
    this.userCooldown = new Map();
    this.roundTriggers = 0;
  }

  resetRound() {
    this.roundTriggers = 0;
  }

  resolveGiftKey(giftName) {
    const raw = String(giftName || "").trim();
    if (!raw) return raw;
    if (this.config.gifts[raw]) return raw;
    const lower = raw.toLowerCase();
    for (const key of Object.keys(this.config.gifts)) {
      if (key.toLowerCase() === lower) return key;
    }
    return raw;
  }

  resolveGift(username, giftName) {
    const now = Date.now();
    const lastUse = this.userCooldown.get(username) || 0;
    const inCooldown = now - lastUse < this.config.cooldownMs;
    const overRoundLimit = this.roundTriggers >= this.config.maxTriggersPerRound;
    const key = this.resolveGiftKey(giftName);
    const mapped = this.config.gifts[key];

    if (!mapped || typeof mapped !== "object" || inCooldown || overRoundLimit) {
      return { accepted: false, reason: "ignored" };
    }

    this.userCooldown.set(username, now);
    this.roundTriggers += 1;

    const gameMode =
      typeof mapped.gameMode === "string" && mapped.gameMode.trim()
        ? mapped.gameMode.trim().toLowerCase()
        : "";

    return {
      accepted: true,
      action: mapped.type,
      value: mapped.value,
      durationSec: mapped.durationSec || 0,
      gameMode,
      giftName
    };
  }
}
