/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GRAPHQL_URL: string;
  readonly VITE_SIGNALR_HUB_URL: string;
  readonly VITE_SIGNALR_HUB_KEY: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
