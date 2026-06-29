// Runtime configuration for the auth app, read from Vite env vars.
export const config = {
  // Base URL of the Forkly .NET API.
  apiBase: (import.meta.env.VITE_API_BASE || 'http://localhost:5080').trim(),
}
