import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

// API proxy operates only in Vite. No changes to the ASP.NET backend.
// Default comes from backend/LegacyVault/Properties/launchSettings.json (https profile).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "VITE_");
  return {
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: env.VITE_BACKEND_TARGET || "http://localhost:5161",
        changeOrigin: true,
        secure: false,
      },
    },
  },
  };
});
