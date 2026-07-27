"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { HubConnectionState, type HubConnection } from "@microsoft/signalr";
import { ApiError, api, type AuthUser, type ChatMessage, type Room } from "@/lib/api";
import { buildChatConnection } from "@/lib/chatConnection";

export default function ChatPage() {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [rooms, setRooms] = useState<Room[]>([]);
  const [activeRoomId, setActiveRoomId] = useState<string>("");
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [notice, setNotice] = useState<string>("");
  const [draft, setDraft] = useState<string>("");
  const [newRoom, setNewRoom] = useState<string>("");

  const connectionRef = useRef<HubConnection | null>(null);
  const activeRoomRef = useRef<string>("");
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    let disposed: boolean = false;

    async function start() {
      try {
        const me: AuthUser = await api.me();
        if (disposed) {
          return;
        }
        setUser(me);
      } catch {
        router.replace("/login");
        return;
      }

      const connection: HubConnection = buildChatConnection({
        onLoadHistory: (history) => setMessages(history),
        onReceiveMessage: (message) => {
          if (message.roomId === activeRoomRef.current) {
            setMessages((prev) => [...prev, message]);
          }
        },
        onRoomCreated: (room) =>
          setRooms((prev) =>
            prev.some((r) => r.id === room.id) ? prev : [...prev, room]
          ),
        onCommandAccepted: (stockCode) =>
          setNotice(`Requested a quote for ${stockCode.toUpperCase()}...`),
        onCommandRejected: (reason) => setNotice(reason),
        onErrorOccurred: (reason) => setNotice(reason),
      });
      connectionRef.current = connection;

      try {
        await connection.start();
      } catch {
        setNotice("Could not connect to the chat hub.");
        return;
      }

      try {
        const loaded: Room[] = await api.rooms();
        if (!disposed) {
          setRooms(loaded);
        }
      } catch {
        setNotice("Could not load rooms.");
      }
    }

    start();

    return () => {
      disposed = true;
      const connection: HubConnection | null = connectionRef.current;
      if (connection) {
        void connection.stop();
      }
    };
  }, [router]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function selectRoom(roomId: string) {
    const connection: HubConnection | null = connectionRef.current;
    if (!connection || connection.state !== HubConnectionState.Connected) {
      return;
    }
    if (activeRoomRef.current === roomId) {
      return;
    }
    if (activeRoomRef.current) {
      await connection.invoke("LeaveRoom", activeRoomRef.current);
    }
    activeRoomRef.current = roomId;
    setActiveRoomId(roomId);
    setMessages([]);
    setNotice("");
    await connection.invoke("JoinRoom", roomId);
  }

  async function sendMessage(event: FormEvent) {
    event.preventDefault();
    const connection: HubConnection | null = connectionRef.current;
    const content: string = draft.trim();
    if (!connection || !activeRoomId || !content) {
      return;
    }
    setDraft("");
    await connection.invoke("SendMessage", activeRoomId, content);
  }

  async function createRoom(event: FormEvent) {
    event.preventDefault();
    const name: string = newRoom.trim();
    if (!name) {
      return;
    }
    try {
      const created: Room = await api.createRoom(name);
      setNewRoom("");
      setRooms((prev) =>
        prev.some((r) => r.id === created.id) ? prev : [...prev, created]
      );
      await selectRoom(created.id);
    } catch (error) {
      setNotice(
        error instanceof ApiError
          ? error.message
          : "Could not create the room (name may be invalid)."
      );
    }
  }

  async function logout() {
    await api.logout();
    router.replace("/login");
  }

  if (!user) {
    return <div className="center">Loading...</div>;
  }

  return (
    <div className="chat">
      <aside className="sidebar">
        <strong>Rooms</strong>
        {rooms.map((room) => (
          <button
            key={room.id}
            className={`room${room.id === activeRoomId ? " active" : ""}`}
            onClick={() => selectRoom(room.id)}
          >
            {room.name}
          </button>
        ))}
        {rooms.length === 0 ? <span className="muted">No rooms yet.</span> : null}
        <form onSubmit={createRoom} style={{ marginTop: "auto" }}>
          <input
            placeholder="New room name"
            value={newRoom}
            onChange={(e) => setNewRoom(e.target.value)}
            maxLength={100}
          />
          <button type="submit" style={{ marginTop: "0.5rem", width: "100%" }}>
            Create room
          </button>
        </form>
      </aside>

      <section className="main">
        <div className="topbar">
          <span>
            Signed in as <strong>{user.displayName}</strong>
          </span>
          <button className="secondary" onClick={logout}>
            Log out
          </button>
        </div>

        {activeRoomId ? null : (
          <p className="notice">Select or create a room to start chatting.</p>
        )}

        <div className="messages">
          {messages.map((message) => (
            <div
              key={message.id}
              className={`message${message.isFromBot ? " bot" : ""}`}
            >
              <div className="author">
                {message.authorDisplayName} ·{" "}
                {new Date(message.sentAtUtc).toLocaleTimeString()}
              </div>
              <div>{message.content}</div>
            </div>
          ))}
          <div ref={messagesEndRef} />
        </div>

        {notice ? <p className="notice">{notice}</p> : null}

        <form className="composer" onSubmit={sendMessage}>
          <input
            placeholder={
              activeRoomId
                ? "Message, or /stock=aapl.us"
                : "Join a room first"
            }
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            disabled={!activeRoomId}
            maxLength={1000}
          />
          <button type="submit" disabled={!activeRoomId}>
            Send
          </button>
        </form>
      </section>
    </div>
  );
}
