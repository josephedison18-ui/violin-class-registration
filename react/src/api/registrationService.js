import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const registrationService = {
  // Register for class
  registerForClass: async (formData) => {
    try {
      const response = await api.post('/registrations', formData);
      return response.data;
    } catch (error) {
      throw error.response?.data || { success: false, error: 'Network error' };
    }
  },

  // Track page visitor
  trackVisitor: async () => {
    try {
      const response = await api.post('/registrations/track-visitor', {});
      return response.data;
    } catch (error) {
      console.log('Visitor tracking failed (non-critical)');
    }
  },

  // Admin login
  adminLogin: async (username, password) => {
    try {
      const response = await api.post('/admin/login', { username, password });
      return response.data;
    } catch (error) {
      throw error.response?.data || { success: false, error: 'Login failed' };
    }
  },

  // Get dashboard stats
  getDashboardStats: async () => {
    try {
      const response = await api.get('/admin/dashboard');
      return response.data;
    } catch (error) {
      throw error.response?.data || { success: false, error: 'Failed to load stats' };
    }
  },

  // Get all registrations
  getAllRegistrations: async () => {
    try {
      const response = await api.get('/registrations');
      return response.data;
    } catch (error) {
      throw error.response?.data || { success: false, error: 'Failed to load registrations' };
    }
  },

  // Search registrations by phone
  searchByPhone: async (phone) => {
    try {
      const response = await api.get(`/registrations/search/byphone?phone=${phone}`);
      return response.data;
    } catch (error) {
      throw error.response?.data || { success: false, error: 'Search failed' };
    }
  },
};

export default registrationService;
