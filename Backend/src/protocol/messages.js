export const MessageType = {
  CHAT: "chat",
  GIFT: "gift",
  QUESTION: "question",
  BATTLE: "battle",
  METRIC: "metric",
  SYSTEM: "system"
};

export function createMessage(type, payload) {
  const safePayload = payload && typeof payload === "object" ? payload : {};
  return JSON.stringify({ type, ts: Date.now(), ...safePayload });
}
