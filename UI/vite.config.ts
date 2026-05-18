import path from 'node:path';
import { fileURLToPath } from 'node:url';
import dotenv from 'dotenv';
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

const uiRoot = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
  dotenv.config({ path: path.resolve(uiRoot, '.env') });
  dotenv.config({ path: path.resolve(uiRoot, '..', '.env'), override: false });

  loadEnv(mode, uiRoot, '');

  return {
    plugins: [react()],
    envDir: uiRoot,
  };
});
