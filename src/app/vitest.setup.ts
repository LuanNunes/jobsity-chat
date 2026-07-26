import "@testing-library/jest-dom/vitest";
import { afterEach, vi } from "vitest";
import { cleanup } from "@testing-library/react";

// Enable React's act() environment so state updates from simulated hub events flush.
(globalThis as unknown as { IS_REACT_ACT_ENVIRONMENT: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

// jsdom does not implement scrollIntoView; the chat view calls it on new messages.
Element.prototype.scrollIntoView = vi.fn();

afterEach(() => {
  cleanup();
});
