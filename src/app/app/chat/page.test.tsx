import { describe, it, expect, vi, beforeEach } from "vitest";
import { act } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ChatClientHandlers } from "@/lib/chatConnection";
import type { ChatMessage } from "@/lib/api";

const push = vi.fn();
const replace = vi.fn();
const router = { push, replace };

vi.mock("next/navigation", () => ({
  useRouter: () => router,
}));

// A controllable fake HubConnection; the test drives server->client events
// through the captured client handlers.
const hub = vi.hoisted(() => {
  return {
    connection: null as unknown as {
      state: string;
      invoke: ReturnType<typeof vi.fn>;
      start: ReturnType<typeof vi.fn>;
      stop: ReturnType<typeof vi.fn>;
    },
    handlers: null as unknown as ChatClientHandlers,
  };
});

vi.mock("@/lib/chatConnection", () => ({
  buildChatConnection: (handlers: ChatClientHandlers) => {
    hub.connection = {
      state: "Connected",
      invoke: vi.fn().mockResolvedValue(undefined),
      start: vi.fn().mockResolvedValue(undefined),
      stop: vi.fn().mockResolvedValue(undefined),
    };
    hub.handlers = handlers;
    return hub.connection;
  },
}));

vi.mock("@/lib/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api")>();
  return {
    ...actual,
    api: {
      me: vi.fn(),
      rooms: vi.fn(),
      createRoom: vi.fn(),
      logout: vi.fn(),
    },
  };
});

import ChatPage from "./page";
import { api, ApiError } from "@/lib/api";

const CID = { id: "u1", email: "cid@test.com", displayName: "Cid" };
const GENERAL = { id: "r1", name: "General" };

function message(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: "m1",
    roomId: "r1",
    authorDisplayName: "Cid",
    content: "Hello",
    isFromBot: false,
    sentAtUtc: "2026-07-26T01:00:00Z",
    ...overrides,
  };
}

describe("ChatPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.me).mockResolvedValue(CID);
    vi.mocked(api.rooms).mockResolvedValue([GENERAL]);
  });

  it("redirects to /login when not authenticated", async () => {
    vi.mocked(api.me).mockRejectedValue(new Error("401"));
    render(<ChatPage />);
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
  });

  it("shows the signed-in user and the room list", async () => {
    render(<ChatPage />);
    expect(await screen.findByText("Cid")).toBeInTheDocument();
    expect(await screen.findByRole("button", { name: "General" })).toBeInTheDocument();
  });

  it("joins a room and renders the loaded history", async () => {
    render(<ChatPage />);
    await userEvent.click(await screen.findByRole("button", { name: "General" }));

    await waitFor(() => expect(hub.connection.invoke).toHaveBeenCalledWith("JoinRoom", "r1"));

    await act(async () => {
      hub.handlers.onLoadHistory([message({ content: "welcome back" })]);
    });
    expect(await screen.findByText("welcome back")).toBeInTheDocument();
  });

  it("appends a broadcast message received for the active room", async () => {
    render(<ChatPage />);
    await userEvent.click(await screen.findByRole("button", { name: "General" }));
    await waitFor(() => expect(hub.connection.invoke).toHaveBeenCalledWith("JoinRoom", "r1"));

    await act(async () => {
      hub.handlers.onReceiveMessage(message({ id: "m2", content: "live message" }));
    });
    expect(await screen.findByText("live message")).toBeInTheDocument();
  });

  it("sends a typed message through the hub", async () => {
    render(<ChatPage />);
    await userEvent.click(await screen.findByRole("button", { name: "General" }));
    await waitFor(() => expect(hub.connection.invoke).toHaveBeenCalledWith("JoinRoom", "r1"));

    const composer = await screen.findByPlaceholderText("Message, or /stock=aapl.us");
    await userEvent.type(composer, "hi there");
    await userEvent.click(screen.getByRole("button", { name: "Send" }));

    expect(hub.connection.invoke).toHaveBeenCalledWith("SendMessage", "r1", "hi there");
  });

  it("shows an ephemeral notice for a stock-command ack", async () => {
    render(<ChatPage />);
    await screen.findByText("Cid");

    await act(async () => {
      hub.handlers.onCommandAccepted("aapl.us");
    });
    expect(await screen.findByText(/Requested a quote for AAPL.US/)).toBeInTheDocument();
  });

  it("appends a broadcast RoomCreated room to the sidebar", async () => {
    render(<ChatPage />);
    // Wait for the initial room list to render before driving the broadcast.
    await screen.findByRole("button", { name: "General" });

    await act(async () => {
      hub.handlers.onRoomCreated({ id: "r2", name: "Random" });
    });

    expect(await screen.findByRole("button", { name: "Random" })).toBeInTheDocument();
  });

  it("does not duplicate a RoomCreated room whose id already exists", async () => {
    render(<ChatPage />);
    await screen.findByRole("button", { name: "General" });

    // The creator already appended this room locally (from the POST response); the Clients.All
    // broadcast for the same id must be deduped, not re-added.
    await act(async () => {
      hub.handlers.onRoomCreated({ id: "r1", name: "General" });
    });

    expect(screen.getAllByRole("button", { name: "General" })).toHaveLength(1);
  });

  it("shows exactly one entry when RoomCreated arrives before the create POST resolves", async () => {
    vi.mocked(api.createRoom).mockResolvedValue({ id: "r2", name: "Random" });
    render(<ChatPage />);
    await screen.findByRole("button", { name: "General" });

    // The Clients.All broadcast reaches the creator first...
    await act(async () => {
      hub.handlers.onRoomCreated({ id: "r2", name: "Random" });
    });

    // ...then the creator's own POST resolves and appends the same room.
    await userEvent.type(
      screen.getByPlaceholderText("New room name"),
      "Random"
    );
    await userEvent.click(screen.getByRole("button", { name: "Create room" }));

    await waitFor(() =>
      expect(api.createRoom).toHaveBeenCalledWith("Random")
    );
    expect(screen.getAllByRole("button", { name: "Random" })).toHaveLength(1);
  });

  it("shows exactly one entry when the create POST resolves before RoomCreated arrives", async () => {
    vi.mocked(api.createRoom).mockResolvedValue({ id: "r2", name: "Random" });
    render(<ChatPage />);
    await screen.findByRole("button", { name: "General" });

    // The creator's own POST resolves first and appends the room...
    await userEvent.type(
      screen.getByPlaceholderText("New room name"),
      "Random"
    );
    await userEvent.click(screen.getByRole("button", { name: "Create room" }));
    await waitFor(() =>
      expect(api.createRoom).toHaveBeenCalledWith("Random")
    );

    // ...then the Clients.All broadcast for the same id arrives and must be deduped.
    await act(async () => {
      hub.handlers.onRoomCreated({ id: "r2", name: "Random" });
    });

    expect(screen.getAllByRole("button", { name: "Random" })).toHaveLength(1);
  });

  it("shows the API error message when createRoom rejects with a duplicate-name error", async () => {
    vi.mocked(api.createRoom).mockRejectedValue(
      new ApiError(400, "A room named 'General' already exists.")
    );
    render(<ChatPage />);
    await screen.findByRole("button", { name: "General" });

    await userEvent.type(
      screen.getByPlaceholderText("New room name"),
      "general"
    );
    await userEvent.click(screen.getByRole("button", { name: "Create room" }));

    await waitFor(() => expect(api.createRoom).toHaveBeenCalledWith("general"));

    expect(
      await screen.findByText("A room named 'General' already exists.")
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/Could not create the room/)
    ).not.toBeInTheDocument();
  });
});
