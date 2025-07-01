# Security Configuration

This document explains how to handle sensitive configuration in the Mind-Mend application.

## Sensitive Information

The following sensitive information has been moved from `appsettings.json` to `appsettings.secrets.json`:

- **Email Settings**: SMTP credentials for sending emails
- **JWT Configuration**: Secret key for JWT token generation
- **Google OAuth**: Client ID and Client Secret for Google authentication
- **Paymob Settings**: API key for payment processing
- **WhatsApp Settings**: Access token for WhatsApp Business API
- **OpenAI Settings**: API key for OpenAI services

## Setup Instructions

1. **Copy the secrets file**: The `appsettings.secrets.json` file contains the actual sensitive values.

2. **Environment-specific configuration**: For different environments (development, staging, production), create environment-specific secrets files:
   - `appsettings.Development.secrets.json`
   - `appsettings.Staging.secrets.json`
   - `appsettings.Production.secrets.json`

3. **Environment Variables**: For production deployments, consider using environment variables instead of files:
   ```bash
   # Example environment variables
   EmailSettings__SmtpUsername=your-email@gmail.com
   EmailSettings__SmtpPassword=your-app-password
   JwtConfig__Secret=your-jwt-secret
   GoogleAuth__ClientSecret=your-google-client-secret
   PaymobSettings__ApiKey=your-paymob-api-key
   WhatsAppSettings__AccessToken=your-whatsapp-token
   OpenAI__ApiKey=your-openai-api-key
   ```

## Security Best Practices

1. **Never commit secrets**: The `appsettings.secrets.json` file is already added to `.gitignore` to prevent accidental commits.

2. **Use strong secrets**: Generate strong, unique secrets for each environment.

3. **Rotate secrets regularly**: Regularly update API keys and secrets.

4. **Limit access**: Only authorized personnel should have access to production secrets.

5. **Use secure storage**: In production, use secure secret management services like:
   - Azure Key Vault
   - AWS Secrets Manager
   - HashiCorp Vault
   - Environment variables in containerized deployments

## File Structure

```
├── appsettings.json              # Public configuration (safe to commit)
├── appsettings.secrets.json      # Sensitive configuration (gitignored)
├── appsettings.Development.json  # Development-specific settings
└── .gitignore                    # Excludes sensitive files
```

## Configuration Hierarchy

The application uses the following configuration hierarchy (highest to lowest priority):
1. Environment variables
2. Command line arguments
3. `appsettings.{Environment}.secrets.json`
4. `appsettings.secrets.json`
5. `appsettings.{Environment}.json`
6. `appsettings.json`

This ensures that sensitive information can be overridden for different environments while maintaining security. 