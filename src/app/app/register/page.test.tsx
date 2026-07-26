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

import RegisterPage from "./page";
import { api, ApiError } from "@/lib/api";

describe("RegisterPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("registers and redirects to /chat on success", async () => {
    vi.mocked(api.register).mockResolvedValue({ id: "1", email: "a@b.c", displayName: "Ana" });
    render(<RegisterPage />);

    await userEvent.type(screen.getByLabelText("Email"), "a@b.c");
    await userEvent.type(screen.getByLabelText("Display name"), "Ana");
    await userEvent.type(screen.getByLabelText("Password"), "Passw0rd!");
    await userEvent.click(screen.getByRole("button", { name: "Register" }));

    expect(api.register).toHaveBeenCalledWith("a@b.c", "Ana", "Passw0rd!");
    expect(push).toHaveBeenCalledWith("/chat");
  });

  it("surfaces the API error message on failure", async () => {
    vi.mocked(api.register).mockRejectedValue(new ApiError(400, "Passwords must be at least 6 characters."));
    render(<RegisterPage />);

    await userEvent.type(screen.getByLabelText("Email"), "a@b.c");
    await userEvent.type(screen.getByLabelText("Display name"), "Ana");
    await userEvent.type(screen.getByLabelText("Password"), "x");
    await userEvent.click(screen.getByRole("button", { name: "Register" }));

    expect(
      await screen.findByText("Passwords must be at least 6 characters."),
    ).toBeInTheDocument();
    expect(push).not.toHaveBeenCalled();
  });
});
