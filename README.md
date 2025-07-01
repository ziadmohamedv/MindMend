# 🧠 Mind-Mend - Mental Health Platform

A comprehensive mental health platform built with ASP.NET Core 8.0 that provides therapy services, mental health resources, and AI-powered diagnosis support.

## 🌟 Features

### 🔐 Authentication & Authorization
- **JWT-based authentication** with role-based authorization
- **Google OAuth integration** for seamless login
- **Phone verification** via WhatsApp
- **Multi-role system**: Admin, Therapist, Doctor, Patient

### 👥 User Management
- **Patient profiles** with health data tracking
- **Therapist profiles** with availability management
- **Admin dashboard** for user management
- **Role assignment** and user administration

### 📅 Appointment System
- **Real-time appointment booking** with therapists
- **Availability management** for healthcare providers
- **Automated notifications** via email and WhatsApp
- **Appointment status tracking**

### 💬 Communication
- **Real-time chat** using SignalR
- **Video calling** capabilities
- **Message history** and chat threads
- **File sharing** in conversations

### 📚 Mental Health Resources
- **Book library** with mental health literature
- **Podcast collection** for audio content
- **Resource categorization** by mental health topics
- **Multilingual support** (English & Arabic)

### 🤖 AI-Powered Features
- **Initial diagnosis** using OpenAI GPT-3.5
- **Mental health assessment** tools
- **Personalized recommendations**

### 💳 Payment Integration
- **Paymob payment gateway** integration
- **Secure payment processing**
- **Transaction management**

### 📱 Notifications
- **Email notifications** using SMTP
- **WhatsApp Business API** integration
- **Real-time notifications** via SignalR
- **Appointment reminders**

### 🏥 Health Data Integration
- **Google Health Connect** integration
- **Health data synchronization**
- **Patient health monitoring**

## 🏗️ Architecture

### Technology Stack
- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server (LocalDB for development)
- **Authentication**: ASP.NET Core Identity + JWT
- **Real-time Communication**: SignalR
- **API Documentation**: Swagger/OpenAPI
- **Payment**: Paymob Gateway
- **AI**: OpenAI GPT-3.5
- **Health Data**: Google Health Connect
- **Notifications**: WhatsApp Business API + SMTP

### Project Structure
```
Mind-Mend/
├── Controllers/           # API endpoints
├── Models/               # Data models and DTOs
├── Services/             # Business logic
├── Data/                 # Database context and migrations
├── Hubs/                 # SignalR hubs for real-time features
├── DTOs/                 # Data transfer objects
├── wwwroot/             # Static files and uploads
├── covers/              # Book cover images
└── Migrations/          # Entity Framework migrations
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (or LocalDB)
- Visual Studio 2022 or VS Code
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/mind-mend.git
   cd mind-mend
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure the database**
   ```bash
   dotnet ef database update
   ```

4. **Set up configuration**
   - Copy `appsettings.secrets.json` and update with your values
   - See [SECURITY.md](SECURITY.md) for detailed configuration instructions

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - API: `https://localhost:7000`
   - Swagger UI: `https://localhost:7000/swagger`

### Environment Variables

For production deployment, set these environment variables:

```bash
# Email Settings
EmailSettings__SmtpUsername=your-email@gmail.com
EmailSettings__SmtpPassword=your-app-password

# JWT Configuration
JwtConfig__Secret=your-jwt-secret-key

# Google OAuth
GoogleAuth__ClientId=your-google-client-id
GoogleAuth__ClientSecret=your-google-client-secret

# Paymob Settings
PaymobSettings__ApiKey=your-paymob-api-key

# WhatsApp Settings
WhatsAppSettings__AccessToken=your-whatsapp-access-token

# OpenAI Settings
OpenAI__ApiKey=your-openai-api-key
```

## 📖 API Documentation

### Authentication Endpoints
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/google` - Google OAuth login
- `POST /api/auth/verify-phone` - Phone verification

### User Management
- `GET /api/users/profile` - Get user profile
- `PUT /api/users/profile` - Update user profile
- `GET /api/users` - Get all users (Admin only)

### Appointments
- `POST /api/appointments` - Create appointment
- `GET /api/appointments` - Get appointments
- `PUT /api/appointments/{id}` - Update appointment
- `DELETE /api/appointments/{id}` - Cancel appointment

### Chat & Communication
- `POST /api/chat/send-message` - Send message
- `GET /api/chat/threads` - Get chat threads
- `GET /api/chat/messages/{threadId}` - Get messages

### Resources
- `GET /api/books` - Get books
- `GET /api/podcasts` - Get podcasts
- `GET /api/resources` - Get all resources

### Diagnosis
- `POST /api/diagnosis/initial` - Get initial diagnosis

## 🔧 Development

### Database Migrations
```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### Seeding Data
The application includes seed data for:
- Default users (Admin, Therapist, Patient)
- Books and podcasts
- User roles

Run seeding via the `/api/seed` endpoint or automatically on startup.

### Real-time Features
SignalR hubs are available at:
- `/chathub` - Chat functionality
- `/videocallhub` - Video calling

## 🐳 Docker Support

The project includes Docker configuration:

```bash
# Build the image
docker build -t mind-mend .

# Run the container
docker run -p 8080:80 mind-mend
```

## 🔒 Security

- **JWT authentication** with secure token validation
- **Role-based authorization** for all endpoints
- **Sensitive configuration** stored in separate files
- **CORS policy** configured for cross-origin requests
- **Input validation** and sanitization

See [SECURITY.md](SECURITY.md) for detailed security information.

## 📱 Integration Services

### External APIs
- **Google OAuth** - User authentication
- **OpenAI GPT-3.5** - AI diagnosis
- **Paymob** - Payment processing
- **WhatsApp Business API** - Notifications
- **Google Health Connect** - Health data

### File Storage
- **Local file storage** for uploads
- **Book cover images** management
- **User profile pictures**

## 🧪 Testing

The application includes comprehensive testing support:
- **Swagger UI** for API testing
- **HTTP files** for endpoint testing
- **Integration tests** for services

## 📊 Monitoring & Logging

- **Structured logging** with different levels
- **Performance monitoring** capabilities
- **Error tracking** and reporting

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:
- Create an issue in the GitHub repository
- Contact the development team
- Check the [SECURITY.md](SECURITY.md) for configuration help

## 🙏 Acknowledgments

- ASP.NET Core team for the excellent framework
- OpenAI for AI capabilities
- Google for OAuth and Health Connect
- Paymob for payment processing
- WhatsApp for messaging services

---

**Mind-Mend** - Empowering mental health through technology 💙
