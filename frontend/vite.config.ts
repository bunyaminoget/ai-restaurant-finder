import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Vitest inline config keeps the setup minimal (jsdom + jest-dom matchers).
// See https://vitest.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test-setup.ts"],
  },
});
