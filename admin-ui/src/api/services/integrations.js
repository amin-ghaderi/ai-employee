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

  /** @returns {Promise<{ webhookUrl: string | null, status: string, lastError: string | null, lastSyncedAt: string | null }>} */
  async syncWebhook(id) {
    const { data } = await api.post(`/integrations/${id}/sync-webhook`);
    return data;
  },

  /** @returns {Promise<{ webhookUrl: string | null, status: string, lastError: string | null, lastSyncedAt: string | null }>} */
  async getWebhookStatus(id) {
    const { data } = await api.get(`/integrations/${id}/webhook-status`);
    return data;
  },

  /**
   * @param {string} id
   * @param {{ dropPendingUpdates?: boolean }} [options]
   * @returns {Promise<{ webhookUrl: string | null, status: string, lastError: string | null, lastSyncedAt: string | null }>}
   */
  async deleteWebhook(id, options = {}) {
    const config = {};
    if (options.dropPendingUpdates === true) {
      config.params = { dropPendingUpdates: true };
    }
    const { data } = await api.delete(`/integrations/${id}/webhook`, config);
    return data;
  },
};
