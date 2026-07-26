// Thin fetch wrapper around the ASP.NET Core API. Every call sends the auth
// cookie (credentials: "include"); the API's CORS "Spa" policy allows it.

export const API_URL: string =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5202";

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
}

export interface Room {
  id: string;
  name: string;
}

// Matches the API's ChatMessageDto (camelCase JSON).
export interface ChatMessage {
  id: string;
  roomId: string;
  authorDisplayName: string;
  content: string;
  isFromBot: boolean;
  sentAtUtc: string;
}

export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response: Response = await fetch(`${API_URL}${path}`, {
    ...init,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readError(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function readError(response: Response): Promise<string> {
  try {
    const body: unknown = await response.json();
    if (body && typeof body === "object" && "detail" in body) {
      return String((body as { detail: unknown }).detail);
    }
    if (body && typeof body === "object" && "title" in body) {
      return String((body as { title: unknown }).title);
    }
    return response.statusText;
  } catch {
    return response.statusText;
  }
}

export const api = {
  register: (email: string, displayName: string, password: string): Promise<AuthUser> =>
    request<AuthUser>("/api/auth/register", {
      method: "POST",
      body: JSON.stringify({ email, displayName, password }),
    }),

  login: (email: string, password: string, rememberMe: boolean): Promise<AuthUser> =>
    request<AuthUser>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password, rememberMe }),
    }),

  logout: (): Promise<void> =>
    request<void>("/api/auth/logout", { method: "POST" }),

  me: (): Promise<AuthUser> => request<AuthUser>("/api/auth/me"),

  rooms: (): Promise<Room[]> => request<Room[]>("/api/rooms"),

  createRoom: (name: string): Promise<Room> =>
    request<Room>("/api/rooms", {
      method: "POST",
      body: JSON.stringify({ name }),
    }),
};
