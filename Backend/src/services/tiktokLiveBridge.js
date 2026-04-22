import EventEmitter from "node:events";

export class TikTokLiveBridge extends EventEmitter {
  constructor(username) {
    super();
    this.username = username;
    this.connection = null;
  }

  async connect() {
    if (!this.username) {
      console.warn("TikTok username missing, bridge disabled.");
      return false;
    }

    const module = await import("tiktok-live-connector");
    const WebcastPushConnection = module.WebcastPushConnection || module.default?.WebcastPushConnection;
    if (!WebcastPushConnection) {
      throw new Error("tiktok-live-connector export not found");
    }

    this.connection = new WebcastPushConnection(this.username);
    await this.connection.connect();

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
  }
}
