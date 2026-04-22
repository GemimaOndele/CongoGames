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

  resolveGift(username, giftName) {
    const now = Date.now();
    const lastUse = this.userCooldown.get(username) || 0;
    const inCooldown = now - lastUse < this.config.cooldownMs;
    const overRoundLimit = this.roundTriggers >= this.config.maxTriggersPerRound;
    const mapped = this.config.gifts[giftName];

    if (!mapped || inCooldown || overRoundLimit) {
      return { accepted: false, reason: "ignored" };
    }

    this.userCooldown.set(username, now);
    this.roundTriggers += 1;

    return {
      accepted: true,
      action: mapped.type,
      value: mapped.value,
      durationSec: mapped.durationSec || 0,
      giftName
    };
  }
}
