import EventEmitter from "node:events";

export class TikTokLiveBridge extends EventEmitter {
  constructor(usernames) {
    super();
    this.usernames = this.normalizeUsernames(usernames);
    this.connection = null;
    this.connected = false;
    this.activeUsername = "";
    this.lastError = "";
  }

  normalizeUsernames(value) {
    if (!value) return [];
    if (Array.isArray(value)) {
      return value.map((u) => String(u).trim().replace(/^@/, "")).filter(Boolean);
    }
    return String(value)
      .split(",")
      .map((u) => u.trim().replace(/^@/, ""))
      .filter(Boolean);
  }

  async connect() {
    if (this.usernames.length === 0) {
      console.warn("TikTok username missing, bridge disabled.");
      return false;
    }

    const module = await import("tiktok-live-connector");
    const WebcastPushConnection = module.WebcastPushConnection || module.default?.WebcastPushConnection;
    if (!WebcastPushConnection) {
      throw new Error("tiktok-live-connector export not found");
    }

    for (const username of this.usernames) {
      try {
        this.connection = new WebcastPushConnection(username);
        await this.connection.connect();
        this.connected = true;
        this.activeUsername = username;
        this.lastError = "";

        this.connection.on("chat", (data) => {
          this.emit("chat", {
            user: data.nickname,
            message: data.comment
          });
        });

        this.connection.on("gift", (data) => {
          this.emit("gift", {
            user: data.nickname,
            giftName: data.giftName
          });
        });

        return true;
      } catch (error) {
        this.connected = false;
        this.lastError = error?.message || "unknown_error";
      }
    }

    const msg = this.lastError || "unknown_error";
    if (msg.includes("19881007") || msg.includes("user_not_found")) {
      console.warn(
        `TikTok bridge: aucun compte detecte en live (${this.usernames.join(", ")}). Nouvelle tentative automatique.`
      );
    } else {
      console.warn(`TikTok bridge connection failed: ${msg}`);
    }
    return false;
  }
}
