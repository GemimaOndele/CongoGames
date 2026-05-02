import EventEmitter from "node:events";

function parsePositiveInt(raw, fallback) {
  const n = Number.parseInt(String(raw ?? ""), 10);
  return Number.isFinite(n) && n >= 0 ? n : fallback;
}

export class TikTokLiveBridge extends EventEmitter {
  constructor(usernames) {
    super();
    this.usernames = this.normalizeUsernames(usernames);
    this.connection = null;
    this.connected = false;
    this.activeUsername = "";
    this.lastError = "";
    /** @type {number} */
    this._metricTsLike = 0;
    /** @type {number} */
    this._metricTsMember = 0;
    /** @type {number} */
    this._metricTsRoomUser = 0;
    /** @type {number} */
    this._metricTsSocial = 0;
    /** @type {number} */
    this._metricTsSubscribe = 0;
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
        this._metricTsLike = 0;
        this._metricTsMember = 0;
        this._metricTsRoomUser = 0;
        this._metricTsSocial = 0;
        this._metricTsSubscribe = 0;

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

        const likeMs = parsePositiveInt(process.env.TIKTOK_METRIC_LIKE_MS, 420);
        const memberMs = parsePositiveInt(process.env.TIKTOK_METRIC_MEMBER_MS, 380);
        const roomMs = parsePositiveInt(process.env.TIKTOK_METRIC_ROOM_MS, 14000);
        const socialMs = parsePositiveInt(process.env.TIKTOK_METRIC_SOCIAL_MS, 720);
        const subscribeMs = parsePositiveInt(process.env.TIKTOK_METRIC_SUBSCRIBE_MS, 2200);
        const likeBurstMin = parsePositiveInt(process.env.TIKTOK_METRIC_LIKE_BURST_MIN, 7);

        this.connection.on("like", (data) => {
          const now = Date.now();
          if (now - this._metricTsLike < likeMs) return;
          this._metricTsLike = now;
          const n = Number(data.likeCount) || 0;
          this.emit("metric", {
            action: n >= likeBurstMin ? "like_burst" : "engagement",
            value: n,
            user: data.nickname || data.uniqueId || "",
            source: "tiktok-like"
          });
        });

        this.connection.on("member", (data) => {
          const now = Date.now();
          if (now - this._metricTsMember < memberMs) return;
          this._metricTsMember = now;
          this.emit("metric", {
            action: "viewer_milestone",
            value: 1,
            user: data.nickname || data.uniqueId || "",
            source: "tiktok-member"
          });
        });

        this.connection.on("roomUser", (data) => {
          const now = Date.now();
          if (now - this._metricTsRoomUser < roomMs) return;
          this._metricTsRoomUser = now;
          const vc = Number(data.viewerCount) || 0;
          this.emit("metric", {
            action: "pulse",
            value: vc,
            source: "tiktok-roomUser"
          });
        });

        this.connection.on("social", (data) => {
          const now = Date.now();
          if (now - this._metricTsSocial < socialMs) return;
          this._metricTsSocial = now;
          const dt = String(data.displayType || "").toLowerCase();
          const isShare = dt.includes("share");
          this.emit("metric", {
            action: "engagement",
            value: isShare ? 2 : 1,
            user: data.nickname || data.uniqueId || "",
            source: isShare ? "tiktok-share" : "tiktok-follow"
          });
        });

        this.connection.on("subscribe", (data) => {
          const now = Date.now();
          if (now - this._metricTsSubscribe < subscribeMs) return;
          this._metricTsSubscribe = now;
          this.emit("metric", {
            action: "engagement",
            value: 3,
            user: data?.nickname || data?.uniqueId || "",
            source: "tiktok-subscribe"
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
