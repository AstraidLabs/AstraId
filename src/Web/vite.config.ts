import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig(() => {
  const base = process.env.VITE_BASE ?? "/";
  const outDir = process.env.VITE_OUT_DIR ?? "dist";

  return {
    base,
    plugins: [react()],
    server: {
      port: 5173
    },
    build: {
      outDir,
      manifest: true,
      rollupOptions: {
        input: path.resolve(__dirname, "index.html")
      }
    }
  };
});
