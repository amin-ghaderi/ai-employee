import { api } from '../client.js';

export async function getConfig(botId) {
  const { data } = await api.get(`/config/${botId}`);
  return data;
}

export async function updateConfig(botId, data) {
  await api.put(`/config/${botId}`, data);
}
