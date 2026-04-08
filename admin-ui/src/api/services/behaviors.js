import { api } from '../client.js';

export const BehaviorsApi = {
  async list() {
    const { data } = await api.get('/behaviors');
    return data;
  },

  async getById(id) {
    const { data } = await api.get(`/behaviors/${id}`);
    return data;
  },

  async create(body) {
    const { data } = await api.post('/behaviors', body);
    return data;
  },

  async update(id, body) {
    const { data } = await api.put(`/behaviors/${id}`, body);
    return data;
  },
};
