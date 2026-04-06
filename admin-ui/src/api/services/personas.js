import { api } from '../client.js';

export const PersonasApi = {
  async list() {
    const { data } = await api.get('/personas');
    return data;
  },

  async create(data) {
    const { data: body } = await api.post('/personas', data);
    return body;
  },

  async update(id, data) {
    const { data: body } = await api.put(`/personas/${id}`, data);
    return body;
  },
};
