import React from "react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

const push = vi.fn();
const replace = vi.fn();
const router = { push, replace };

vi.mock("next/navigation", () => ({
  useRouter: () => router,
}));

vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) =>
    React.createElement("a", { href }, children),
}));

vi.mock("@/lib/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api")>();
  return {
    ...actual,
    api: { login: vi.fn(), register: vi.fn() },
  };
});

import LoginPage from "./page";
import { api, ApiError } from "@/lib/api";

describe("LoginPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("logs in and redirects to /chat on success", async () => {
    vi.mocked(api.login).mockResolvedValue({ id: "1", email: "a@b.c", displayName: "Ana" });
    render(<LoginPage />);

    await userEvent.type(screen.getByLabelText("Email"), "a@b.c");
    await userEvent.type(screen.getByLabelText("Password"), "pw");
    await userEvent.click(screen.getByRole("button", { name: "Log in" }));

    expect(api.login).toHaveBeenCalledWith("a@b.c", "pw", true);
    expect(push).toHaveBeenCalledWith("/chat");
  });

  it("shows an invalid-credentials error on 401 and does not redirect", async () => {
    vi.mocked(api.login).mockRejectedValue(new ApiError(401, "nope"));
    render(<LoginPage />);

    await userEvent.type(screen.getByLabelText("Email"), "a@b.c");
    await userEvent.type(screen.getByLabelText("Password"), "wrong");
    await userEvent.click(screen.getByRole("button", { name: "Log in" }));

    expect(await screen.findByText("Invalid email or password.")).toBeInTheDocument();
    expect(push).not.toHaveBeenCalled();
  });
});
