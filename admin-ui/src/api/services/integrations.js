import { api } from '../client.js';

export const IntegrationsApi = {
  async list(botId) {
    const config = {};
    if (botId !== undefined && botId !== null && botId !== '') {
      config.params = { botId };
    }
    const { data } = await api.get('/integrations', config);
    return data;
  },

  async create(body) {
    const { data } = await api.post('/integrations', body);
    return data;
  },

  async update(id, body) {
    const { data } = await api.patch(`/integrations/${id}`, body);
    return data;
  },

  async enable(id) {
    const { data } = await api.post(`/integrations/${id}/enable`);
    return data;
  },

  async disable(id) {
    const { data } = await api.post(`/integrations/${id}/disable`);
    return data;
  },
};
