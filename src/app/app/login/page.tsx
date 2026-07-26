"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { api, ApiError } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [busy, setBusy] = useState<boolean>(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError("");
    setBusy(true);
    try {
      await api.login(email, password, true);
      router.push("/chat");
    } catch (err) {
      const message: string =
        err instanceof ApiError && err.status === 401
          ? "Invalid email or password."
          : "Login failed. Is the API running?";
      setError(message);
      setBusy(false);
    }
  }

  return (
    <div className="center">
      <form className="card" onSubmit={onSubmit}>
        <h1>Log in</h1>
        <label htmlFor="email">Email</label>
        <input
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        {error ? <p className="error">{error}</p> : null}
        <p style={{ marginTop: "1rem" }}>
          <button type="submit" disabled={busy}>
            {busy ? "Logging in..." : "Log in"}
          </button>
        </p>
        <p className="muted">
          No account? <Link href="/register">Register</Link>
        </p>
      </form>
    </div>
  );
}
