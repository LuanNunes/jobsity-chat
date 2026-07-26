import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { api, ApiError, API_URL } from "./api";

function mockResponse(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: "status-text",
    json: async () => body,
  } as unknown as Response;
}

describe("api client", () => {
  beforeEach(() => {
    global.fetch = vi.fn();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("login posts credentials with cookies and returns the user", async () => {
    const fetchMock = vi.mocked(global.fetch);
    fetchMock.mockResolvedValue(
      mockResponse(200, { id: "1", email: "a@b.c", displayName: "Ana" }),
    );

    const user = await api.login("a@b.c", "pw", true);

    expect(user.displayName).toBe("Ana");
    expect(fetchMock).toHaveBeenCalledWith(
      `${API_URL}/api/auth/login`,
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        body: JSON.stringify({ email: "a@b.c", password: "pw", rememberMe: true }),
      }),
    );
  });

  it("register posts email, displayName and password", async () => {
    const fetchMock = vi.mocked(global.fetch);
    fetchMock.mockResolvedValue(
      mockResponse(200, { id: "1", email: "a@b.c", displayName: "Ana" }),
    );

    await api.register("a@b.c", "Ana", "pw");

    expect(fetchMock).toHaveBeenCalledWith(
      `${API_URL}/api/auth/register`,
      expect.objectContaining({
        body: JSON.stringify({ email: "a@b.c", displayName: "Ana", password: "pw" }),
      }),
    );
  });

  it("throws ApiError carrying the status on a non-ok response", async () => {
    vi.mocked(global.fetch).mockResolvedValue(
      mockResponse(401, { detail: "Invalid credentials." }),
    );

    const error: unknown = await api.login("a@b.c", "wrong", false).catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(401);
    expect((error as ApiError).message).toBe("Invalid credentials.");
  });

  it("returns undefined for a 204 response (logout)", async () => {
    vi.mocked(global.fetch).mockResolvedValue(mockResponse(204));
    await expect(api.logout()).resolves.toBeUndefined();
  });

  it("createRoom posts the name and returns the room", async () => {
    const fetchMock = vi.mocked(global.fetch);
    fetchMock.mockResolvedValue(mockResponse(201, { id: "r1", name: "General" }));

    const room = await api.createRoom("General");

    expect(room).toEqual({ id: "r1", name: "General" });
    expect(fetchMock).toHaveBeenCalledWith(
      `${API_URL}/api/rooms`,
      expect.objectContaining({ method: "POST", body: JSON.stringify({ name: "General" }) }),
    );
  });

  it("me sends credentials and returns the current user", async () => {
    const fetchMock = vi.mocked(global.fetch);
    fetchMock.mockResolvedValue(
      mockResponse(200, { id: "1", email: "a@b.c", displayName: "Ana" }),
    );

    await api.me();

    expect(fetchMock).toHaveBeenCalledWith(
      `${API_URL}/api/auth/me`,
      expect.objectContaining({ credentials: "include" }),
    );
  });
});
