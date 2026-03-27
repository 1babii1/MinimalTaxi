import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    host: "localhost",
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": {
        target: "http://localhost:5160",
        changeOrigin: true,
        secure: false,
      },
      "/taxi": {
        target: "http://localhost:5290",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/taxi/, ""),
      },
    },
  },
});
