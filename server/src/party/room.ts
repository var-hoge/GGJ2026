import { Server, type Connection } from "partyserver";
import type { Env } from "../env";

/**
 * One Room Durable Object instance per matched 1v1 pair, keyed by roomId
 * (this.name === roomId). Reached by the Unity client at:
 *   wss://<worker-host>/parties/room/{roomId}
 *
 * Pure relay: whatever one peer sends (an "offer" / "answer" / "ice-candidate"
 * JSON envelope), the other peer receives verbatim. No parsing/validation is
 * done here on purpose - SDP/ICE payload shape is owned by the Unity client.
 */
export class Room extends Server<Env> {
  private readonly readyConnectionIds = new Set<string>();

  onConnect(connection: Connection) {
    const count = [...this.getConnections()].length;
    console.log(`[room:${this.name}] connected ${connection.id} (peers=${count})`);

    if (count > 2) {
      console.warn(`[room:${this.name}] rejecting ${connection.id}, room already has 2 peers (1v1 only)`);
      connection.close(4000, "room full");
      return;
    }

    connection.send(JSON.stringify({ type: "peer-count", count }));
  }

  onMessage(connection: Connection, message: string) {
    // A WebSocket being connected does not guarantee the Unity client has
    // finished installing its receive handlers. Wait for an explicit client
    // acknowledgement from BOTH sides before any SDP/ICE is allowed to flow.
    try {
      const envelope = JSON.parse(message) as { type?: string };
      if (envelope.type === "client-ready") {
        this.readyConnectionIds.add(connection.id);
        if (this.readyConnectionIds.size === 2 && [...this.getConnections()].length === 2) {
          this.broadcast(JSON.stringify({ type: "peer-ready" }));
        }
        return;
      }
    } catch {
      // Signaling payloads are validated by the Unity peer; relay them below.
    }
    console.log(`[room:${this.name}] relay from ${connection.id}: ${message}`);
    for (const conn of this.getConnections()) {
      if (conn.id !== connection.id) {
        conn.send(message);
      }
    }
  }

  onClose(connection: Connection) {
    console.log(`[room:${this.name}] disconnected ${connection.id}`);
    this.readyConnectionIds.delete(connection.id);
    for (const conn of this.getConnections()) {
      conn.send(JSON.stringify({ type: "peer-left" }));
    }
  }
}
