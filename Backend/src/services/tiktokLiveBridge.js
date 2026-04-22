import EventEmitter from "node:events";

export class TikTokLiveBridge extends EventEmitter {
  constructor(username) {
    super();
    this.username = username;
    this.connection = null;
    this.connected = false;
  }

  async connect() {
    if (!this.username) {
      console.warn("TikTok username missing, bridge disabled.");
      return false;
    }

    try {
      const module = await import("tiktok-live-connector");
      const WebcastPushConnection = module.WebcastPushConnection || module.default?.WebcastPushConnection;
      if (!WebcastPushConnection) {
        throw new Error("tiktok-live-connector export not found");
      }

      this.connection = new WebcastPushConnection(this.username);
      await this.connection.connect();
      this.connected = true;

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
      const message = error?.message || "unknown_error";
      if (message.includes("19881007") || message.includes("user_not_found")) {
        console.warn(
          `TikTok bridge: compte introuvable ou pas en live pour "${this.username}". Nouvelle tentative automatique.`
        );
      } else {
        console.warn(`TikTok bridge connection failed: ${message}`);
      }
      return false;
    }
  }
}
