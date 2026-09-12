import React, { useState, useEffect } from 'react';
import * as XLSX from 'xlsx';
import registrationService from '../api/registrationService';
import '../styles/AdminDashboard.css';

const AdminDashboard = () => {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [stats, setStats] = useState(null);
  const [registrations, setRegistrations] = useState([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState({ type: '', text: '' });
  const [activeTab, setActiveTab] = useState('dashboard');
  const [searchTerm, setSearchTerm] = useState('');

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const response = await registrationService.adminLogin(username, password);
      if (response.success) {
        setIsLoggedIn(true);
        setMessage({ type: 'success', text: '✅ Login successful!' });
        loadDashboardData();
      }
    } catch (error) {
      setMessage({ type: 'error', text: `❌ ${error.error}` });
    } finally {
      setLoading(false);
    }
  };

  const loadDashboardData = async () => {
    try {
      const statsData = await registrationService.getDashboardStats();
      setStats(statsData.statistics);
      const regsData = await registrationService.getAllRegistrations();
      setRegistrations(regsData);
    } catch (error) {
      setMessage({ type: 'error', text: 'Failed to load data' });
    }
  };

  const exportToExcel = () => {
    const ws = XLSX.utils.json_to_sheet(registrations);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Registrations');
    XLSX.writeFile(wb, `Violin_Registrations_${new Date().toISOString().split('T')[0]}.xlsx`);
    setMessage({ type: 'success', text: '✅ Excel file downloaded!' });
  };

  if (!isLoggedIn) {
    return (
      <div className="login-container">
        <div className="login-box">
          <h1>🎻 Admin Login</h1>
          <p>Violin Class Registration</p>
          {message.text && <div className={`alert alert-${message.type}`}>{message.text}</div>}
          <form onSubmit={handleLogin}>
            <div className="form-group">
              <label>Username</label>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="joseph_edison"
                required
              />
            </div>
            <div className="form-group">
              <label>Password</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter password"
                required
              />
            </div>
            <button type="submit" disabled={loading}>
              {loading ? 'Logging in...' : 'Login'}
            </button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-dashboard">
      <div className="sidebar">
        <div className="sidebar-logo">
          <h2>🎻 Admin</h2>
          <p>Violin Classes</p>
        </div>
        <nav className="sidebar-menu">
          <button
            className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`}
            onClick={() => setActiveTab('dashboard')}
          >
            📊 Dashboard
          </button>
          <button
            className={`nav-item ${activeTab === 'registrations' ? 'active' : ''}`}
            onClick={() => setActiveTab('registrations')}
          >
            📋 Registrations
          </button>
          <button
            className={`nav-item ${activeTab === 'export' ? 'active' : ''}`}
            onClick={() => setActiveTab('export')}
          >
            📊 Export
          </button>
          <button className="nav-item logout" onClick={() => setIsLoggedIn(false)}>
            🚪 Logout
          </button>
        </nav>
      </div>

      <div className="main-content">
        {message.text && (
          <div className={`alert alert-${message.type}`}>
            {message.text}
          </div>
        )}

        {activeTab === 'dashboard' && stats && (
          <div className="dashboard-section">
            <h2>📊 Dashboard Overview</h2>
            <div className="stats-grid">
              <div className="stat-card">
                <h3>Total Registrations</h3>
                <div className="stat-value">{stats.totalRegistrations}</div>
              </div>
              <div className="stat-card">
                <h3>Online Classes</h3>
                <div className="stat-value">{stats.onlineClasses}</div>
              </div>
              <div className="stat-card">
                <h3>Offline Classes</h3>
                <div className="stat-value">{stats.offlineClasses}</div>
              </div>
              <div className="stat-card">
                <h3>Pending Payments</h3>
                <div className="stat-value">{stats.pendingPayments}</div>
              </div>
              <div className="stat-card">
                <h3>Page Visitors</h3>
                <div className="stat-value">{stats.totalPageVisits}</div>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'registrations' && (
          <div className="registrations-section">
            <h2>📋 All Registrations</h2>
            <input
              type="text"
              placeholder="Search by name, phone, or email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="search-input"
            />
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Phone</th>
                    <th>Email</th>
                    <th>City</th>
                    <th>Class Mode</th>
                    <th>Payment</th>
                    <th>Date</th>
                  </tr>
                </thead>
                <tbody>
                  {registrations
                    .filter(
                      (reg) =>
                        reg.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                        reg.phone.includes(searchTerm) ||
                        reg.email.toLowerCase().includes(searchTerm.toLowerCase())
                    )
                    .map((reg) => (
                      <tr key={reg.id}>
                        <td>{reg.name}</td>
                        <td>{reg.phone}</td>
                        <td>{reg.email}</td>
                        <td>{reg.city}</td>
                        <td>
                          <span className={`badge badge-${reg.classMode}`}>
                            {reg.classMode}
                          </span>
                        </td>
                        <td>
                          <span className={`badge badge-${reg.paymentStatus}`}>
                            {reg.paymentStatus}
                          </span>
                        </td>
                        <td>{new Date(reg.registrationDate).toLocaleDateString()}</td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'export' && (
          <div className="export-section">
            <h2>📊 Export Data</h2>
            <div className="export-card">
              <h3>Export Registrations to Excel</h3>
              <p>Total Records: {registrations.length}</p>
              <button onClick={exportToExcel} className="btn btn-primary">
                📥 Download Excel File
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminDashboard;
