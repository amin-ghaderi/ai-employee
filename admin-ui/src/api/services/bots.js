import { api } from '../client.js';

export const BotsApi = {
  async list() {
    const { data } = await api.get('/bots');
    return data;
  },

  async enable(id) {
    const { data } = await api.post(`/bots/${id}/enable`);
    return data;
  },

  async disable(id) {
    const { data } = await api.post(`/bots/${id}/disable`);
    return data;
  },

  async create(data) {
    const { data: body } = await api.post('/bots', data);
    return body;
  },

  async assign(id, data) {
    const { data: body } = await api.put(`/bots/${id}/assignments`, data);
    return body;
  },
};
