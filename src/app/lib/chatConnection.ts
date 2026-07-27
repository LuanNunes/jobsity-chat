import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { API_URL, type ChatMessage, type Room } from "./api";

export interface ChatClientHandlers {
  onReceiveMessage: (message: ChatMessage) => void;
  onLoadHistory: (messages: ChatMessage[]) => void;
  onRoomCreated: (room: Room) => void;
  onCommandAccepted: (stockCode: string) => void;
  onCommandRejected: (reason: string) => void;
  onErrorOccurred: (reason: string) => void;
}

export function buildChatConnection(handlers: ChatClientHandlers): HubConnection {
  const connection: HubConnection = new HubConnectionBuilder()

    .withUrl(`${API_URL}/hubs/chat`, { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on("ReceiveMessage", handlers.onReceiveMessage);
  connection.on("LoadHistory", handlers.onLoadHistory);
  connection.on("RoomCreated", handlers.onRoomCreated);
  connection.on("CommandAccepted", handlers.onCommandAccepted);
  connection.on("CommandRejected", handlers.onCommandRejected);
  connection.on("ErrorOccurred", handlers.onErrorOccurred);

  return connection;
}
