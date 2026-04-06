// NOTE: Restart dev server after changing .env files
import axios from 'axios';

const baseURL = `${import.meta.env.VITE_API_URL ?? 'http://localhost:5155'}/admin`;

const adminKey =
  import.meta.env.VITE_ADMIN_KEY !== undefined && import.meta.env.VITE_ADMIN_KEY !== ''
    ? import.meta.env.VITE_ADMIN_KEY
    : 'your-secret-key';

export const api = axios.create({
  baseURL,
  headers: {
    'X-Admin-Key': adminKey,
    'Content-Type': 'application/json',
  },
});
