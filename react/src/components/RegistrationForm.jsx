import React, { useState, useEffect } from 'react';
import registrationService from '../api/registrationService';
import '../styles/RegistrationForm.css';

const RegistrationForm = () => {
  const [formData, setFormData] = useState({
    name: '',
    age: '',
    gender: '',
    email: '',
    phone: '',
    city: '',
    country: 'India',
    classMode: 'online',
    whatsappOptIn: true,
    message: '',
  });

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState({ type: '', text: '' });
  const [formErrors, setFormErrors] = useState({});

  useEffect(() => {
    // Track page visitor on component mount
    registrationService.trackVisitor();
  }, []);

  const validateForm = () => {
    const errors = {};
    const phoneRegex = /^[6-9]\d{9}$/;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!formData.name.trim()) errors.name = 'Name is required';
    if (!formData.age || formData.age < 5 || formData.age > 100) errors.age = 'Valid age required';
    if (!formData.gender) errors.gender = 'Gender is required';
    if (!emailRegex.test(formData.email)) errors.email = 'Valid email required';
    if (!phoneRegex.test(formData.phone.replace(/\D/g, ''))) errors.phone = 'Valid 10-digit phone required';
    if (!formData.city.trim()) errors.city = 'City is required';
    if (!formData.classMode) errors.classMode = 'Class mode is required';

    return errors;
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
    // Clear error for this field
    if (formErrors[name]) {
      setFormErrors(prev => ({ ...prev, [name]: '' }));
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errors = validateForm();

    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      setMessage({ type: 'error', text: 'Please fix the errors above' });
      return;
    }

    setLoading(true);
    setMessage({ type: '', text: '' });

    try {
      const response = await registrationService.registerForClass(formData);
      if (response.success) {
        setMessage({
          type: 'success',
          text: `✅ ${response.message}\n\nRegistration ID: ${response.data.registrationId}\n\n📱 Thank you SMS sent to ${formData.phone}\n📞 Admin contact: +91 9499035574`,
        });
        setFormData({
          name: '',
          age: '',
          gender: '',
          email: '',
          phone: '',
          city: '',
          country: 'India',
          classMode: 'online',
          whatsappOptIn: true,
          message: '',
        });
        setTimeout(() => {
          setMessage({ type: '', text: '' });
        }, 8000);
      }
    } catch (error) {
      setMessage({
        type: 'error',
        text: `❌ Error: ${error.error || 'Registration failed'}`,
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="registration-container">
      <div className="form-wrapper">
        <header className="form-header">
          <h1>🎻 Violin Classes</h1>
          <p className="subtitle">Professional Training</p>
          <div className="location-badge">📍 Perambur, Chennai | 🌐 Online Classes</div>
        </header>

        {message.text && (
          <div className={`alert alert-${message.type}`}>
            {message.text}
          </div>
        )}

        <form onSubmit={handleSubmit} className="registration-form">
          {/* Personal Information */}
          <section className="form-section">
            <h2>👤 Personal Information</h2>
            <div className="form-group">
              <label>Full Name *</label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleChange}
                placeholder="Enter your full name"
                className={formErrors.name ? 'error' : ''}
              />
              {formErrors.name && <span className="error-text">{formErrors.name}</span>}
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Age *</label>
                <input
                  type="number"
                  name="age"
                  value={formData.age}
                  onChange={handleChange}
                  min="5"
                  max="100"
                  className={formErrors.age ? 'error' : ''}
                />
                {formErrors.age && <span className="error-text">{formErrors.age}</span>}
              </div>
              <div className="form-group">
                <label>Gender *</label>
                <select
                  name="gender"
                  value={formData.gender}
                  onChange={handleChange}
                  className={formErrors.gender ? 'error' : ''}
                >
                  <option value="">Select Gender</option>
                  <option value="male">Male</option>
                  <option value="female">Female</option>
                  <option value="other">Other</option>
                </select>
                {formErrors.gender && <span className="error-text">{formErrors.gender}</span>}
              </div>
            </div>
          </section>

          {/* Contact Information */}
          <section className="form-section">
            <h2>📞 Contact Information</h2>
            <div className="form-group">
              <label>Phone Number *</label>
              <input
                type="tel"
                name="phone"
                value={formData.phone}
                onChange={handleChange}
                placeholder="10-digit mobile number"
                className={formErrors.phone ? 'error' : ''}
              />
              {formErrors.phone && <span className="error-text">{formErrors.phone}</span>}
            </div>

            <div className="form-group">
              <label>Email Address *</label>
              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                placeholder="your.email@example.com"
                className={formErrors.email ? 'error' : ''}
              />
              {formErrors.email && <span className="error-text">{formErrors.email}</span>}
            </div>

            <div className="form-group checkbox">
              <label>
                <input
                  type="checkbox"
                  name="whatsappOptIn"
                  checked={formData.whatsappOptIn}
                  onChange={handleChange}
                />
                <span>Receive updates on WhatsApp</span>
              </label>
            </div>
          </section>

          {/* Location */}
          <section className="form-section">
            <h2>🌍 Location</h2>
            <div className="form-row">
              <div className="form-group">
                <label>City *</label>
                <input
                  type="text"
                  name="city"
                  value={formData.city}
                  onChange={handleChange}
                  placeholder="e.g., Chennai"
                  className={formErrors.city ? 'error' : ''}
                />
                {formErrors.city && <span className="error-text">{formErrors.city}</span>}
              </div>
              <div className="form-group">
                <label>Country *</label>
                <input
                  type="text"
                  name="country"
                  value={formData.country}
                  onChange={handleChange}
                  className={formErrors.country ? 'error' : ''}
                />
              </div>
            </div>
          </section>

          {/* Class Mode */}
          <section className="form-section">
            <h2>🎓 Class Preference</h2>
            <label>Preferred Class Mode *</label>
            <div className="radio-group">
              <label className="radio-option">
                <input
                  type="radio"
                  name="classMode"
                  value="online"
                  checked={formData.classMode === 'online'}
                  onChange={handleChange}
                />
                <span>🌐 Online (WhatsApp)</span>
              </label>
              <label className="radio-option">
                <input
                  type="radio"
                  name="classMode"
                  value="offline"
                  checked={formData.classMode === 'offline'}
                  onChange={handleChange}
                />
                <span>🏫 Offline (Perambur, Chennai)</span>
              </label>
            </div>

            <div className="form-group">
              <label>Additional Notes</label>
              <textarea
                name="message"
                value={formData.message}
                onChange={handleChange}
                rows="3"
                placeholder="Any specific requirements..."
              />
            </div>
          </section>

          {/* Submit */}
          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? '⏳ Processing...' : '✓ Register Now'}
            </button>
            <button type="reset" className="btn btn-secondary" onClick={() => setFormErrors({})}>
              Clear Form
            </button>
          </div>
        </form>

        {/* Contact Info */}
        <div className="contact-section">
          <h3>📋 Administrator Details</h3>
          <div className="contact-item">
            <span className="icon">👤</span>
            <strong>Name:</strong>
            <span>L. Joseph Edison Rathinaraj</span>
          </div>
          <div className="contact-item">
            <span className="icon">📧</span>
            <strong>Email:</strong>
            <span>josephedison18@gmail.com</span>
          </div>
          <div className="contact-item">
            <span className="icon">📱</span>
            <strong>Phone:</strong>
            <span>+91 9499035574</span>
          </div>
          <div className="contact-item">
            <span className="icon">💬</span>
            <strong>WhatsApp:</strong>
            <span>+91 9499035574</span>
          </div>
          <div className="contact-item">
            <span className="icon">📍</span>
            <strong>Location:</strong>
            <span>Perambur, Chennai</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default RegistrationForm;
