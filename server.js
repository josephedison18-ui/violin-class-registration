const express = require('express');
const path = require('path');
const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, 'public')));

// CORS middleware
app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept');
    next();
});

// In-memory storage (for demo - replace with database)
let registrations = [];

// Routes

// Home page
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

// API: Get all registrations
app.get('/api/registrations', (req, res) => {
    res.json({
        success: true,
        data: registrations,
        count: registrations.length
    });
});

// API: Create registration
app.post('/api/registrations', (req, res) => {
    try {
        const { name, age, level, email, phone, parentName, parentPhone, message } = req.body;

        // Validation
        if (!name || !age || !level || !email || !phone) {
            return res.status(400).json({
                success: false,
                error: 'Missing required fields'
            });
        }

        // Email validation
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(email)) {
            return res.status(400).json({
                success: false,
                error: 'Invalid email format'
            });
        }

        // Create registration object
        const registration = {
            id: Date.now().toString(),
            name,
            age: parseInt(age),
            level,
            email,
            phone,
            parentName: parentName || '',
            parentPhone: parentPhone || '',
            message: message || '',
            registrationDate: new Date().toISOString(),
            status: 'pending'
        };

        registrations.push(registration);

        // TODO: Send email confirmation
        // TODO: Save to database

        res.status(201).json({
            success: true,
            message: 'Registration successful!',
            data: registration
        });

    } catch (error) {
        console.error('Registration error:', error);
        res.status(500).json({
            success: false,
            error: 'Server error during registration'
        });
    }
});

// API: Get registration by ID
app.get('/api/registrations/:id', (req, res) => {
    const registration = registrations.find(r => r.id === req.params.id);
    if (!registration) {
        return res.status(404).json({
            success: false,
            error: 'Registration not found'
        });
    }
    res.json({
        success: true,
        data: registration
    });
});

// API: Update registration
app.put('/api/registrations/:id', (req, res) => {
    const index = registrations.findIndex(r => r.id === req.params.id);
    if (index === -1) {
        return res.status(404).json({
            success: false,
            error: 'Registration not found'
        });
    }

    registrations[index] = {
        ...registrations[index],
        ...req.body,
        id: registrations[index].id, // Don't allow ID change
        registrationDate: registrations[index].registrationDate // Don't allow date change
    };

    res.json({
        success: true,
        data: registrations[index]
    });
});

// API: Delete registration
app.delete('/api/registrations/:id', (req, res) => {
    const index = registrations.findIndex(r => r.id === req.params.id);
    if (index === -1) {
        return res.status(404).json({
            success: false,
            error: 'Registration not found'
        });
    }

    const deleted = registrations.splice(index, 1);
    res.json({
        success: true,
        message: 'Registration deleted',
        data: deleted[0]
    });
});

// API: Health check
app.get('/api/health', (req, res) => {
    res.json({
        success: true,
        status: 'Server is running',
        timestamp: new Date().toISOString()
    });
});

// 404 handler
app.use((req, res) => {
    res.status(404).json({
        success: false,
        error: 'Route not found'
    });
});

// Error handler
app.use((err, req, res, next) => {
    console.error('Server error:', err);
    res.status(500).json({
        success: false,
        error: 'Internal server error'
    });
});

// Start server
app.listen(PORT, () => {
    console.log(`
    ╔════════════════════════════════════════════╗
    ║  Violin Class Registration Server         ║
    ║  Running on: http://localhost:${PORT}              ║
    ║  Admin: L. Joseph Edison Rathinaraj       ║
    ║  Email: josephedison18@gmail.com          ║
    ║  Phone: +91 9499035574                    ║
    ╚════════════════════════════════════════════╝
    `);
});

module.exports = app;
