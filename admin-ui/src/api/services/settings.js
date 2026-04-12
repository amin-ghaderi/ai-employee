import { api } from '../client.js';

const settingsService = {
  /** @returns {Promise<{ publicBaseUrl: string | null }>} */
  async getPublicBaseUrl() {
    const { data } = await api.get('/settings/public-base-url');
    return data;
  },

  /** @returns {Promise<{ publicBaseUrl: string | null }>} */
  async updatePublicBaseUrl(url) {
    const { data } = await api.put('/settings/public-base-url', {
      publicBaseUrl: url,
    });
    return data;
  },

  /** @returns {Promise<{ publicBaseUrl: string | null }>} */
  async deletePublicBaseUrl() {
    const { data } = await api.delete('/settings/public-base-url');
    return data;
  },
};

export default settingsService;
