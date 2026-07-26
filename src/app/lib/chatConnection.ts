import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { API_URL, type ChatMessage } from "./api";

// Server -> client events on IChatClient.
export interface ChatClientHandlers {
  onReceiveMessage: (message: ChatMessage) => void;
  onLoadHistory: (messages: ChatMessage[]) => void;
  onCommandAccepted: (stockCode: string) => void;
  onCommandRejected: (reason: string) => void;
  onErrorOccurred: (reason: string) => void;
}

export function buildChatConnection(handlers: ChatClientHandlers): HubConnection {
  const connection: HubConnection = new HubConnectionBuilder()
    // withCredentials sends the auth cookie on negotiate + the socket.
    .withUrl(`${API_URL}/hubs/chat`, { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on("ReceiveMessage", handlers.onReceiveMessage);
  connection.on("LoadHistory", handlers.onLoadHistory);
  connection.on("CommandAccepted", handlers.onCommandAccepted);
  connection.on("CommandRejected", handlers.onCommandRejected);
  connection.on("ErrorOccurred", handlers.onErrorOccurred);

  return connection;
}
