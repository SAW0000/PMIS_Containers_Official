# SQLserver
$env:DB_PASSWORD = "PUT YOUR PASSWORD HERE"
$env:SQLSERVER_PORT = "1433"
$env:ConnectionStrings__PmisDatabase = "Server=localhost,$($env:SQLSERVER_PORT);Database=PMIS;User Id=sa;Password=$env:DB_PASSWORD;TrustServerCertificate=True"


# MongoDB
$env:MONGO_ROOT_USERNAME = "PUT YOUR USERNAME HERE"
$env:MONGO_ROOT_PASSWORD = "PUT YOUR PASSWORD HERE"
$env:MONGODB_PORT = "27017"
$env:ConnectionStrings__MongoDb = "mongodb://$($env:MONGO_ROOT_USERNAME):$($env:MONGO_ROOT_PASSWORD)@localhost:$($env:MONGODB_PORT)/?authSource=admin"
$env:MongoDb__DatabaseName = "pmis"


# Minio
$env:MINIO_ROOT_USER = "PUT YOUR USERNAME HERE"
$env:MINIO_ROOT_PASSWORD = "PUT YOUR PASSWORD HERE"
$env:MINIO_API_PORT = "9000"
$env:MINIO_CONSOLE_PORT = "9002"
$env:MinIO__Endpoint = "localhost:$($env:MINIO_API_PORT)"
$env:MinIO__AccessKey = $env:MINIO_ROOT_USER
$env:MinIO__SecretKey = $env:MINIO_ROOT_PASSWORD
$env:MinIO__BucketName = "pmis-attachments"
$env:MinIO__UseSsl = "false"


# Redis
$env:REDIS_PASSWORD = "PUT YOUR PASSWORD HERE"
$env:REDIS_PORT = "6379"
$env:ConnectionStrings__Redis = "localhost:$($env:REDIS_PORT),password=$env:REDIS_PASSWORD"


# ClamAV
$env:CLAMAV_PORT = "3310"
$env:ClamAv__Host = "127.0.0.1"
$env:ClamAv__Port = $env:CLAMAV_PORT


# Caddy
$env:DEV_CERT_PASSWORD = "PUT YOUR CERT PASSWORD HERE"
$env:DEV_CERT_DIR = "$env:USERPROFILE\.aspnet\https"
$env:APP_HTTP_PORT = "5011"
$env:APP_HTTPS_PORT = "7261"


# Nafath
$env:Nafath__IamSsoUrl = "PUT YOUR IAM SSO URL HERE"
$env:Nafath__GetTokenUrl = "Account/GeneraterToken?appkey="
$env:Nafath__UserClaimsUrl = "Account/GetUserClaim?token="
$env:Nafath__IamLoginUrl = "PUT YOUR IAM LOGIN URL HERE"
$env:Nafath__IamLogoutUrl = "PUT YOUR IAM LOGOUT URL HERE"
$env:Nafath__AppKey = "PUT YOUR APP KEY HERE"


# Email/SMS
$env:EmailSms__LmsBaseUrl = "PUT YOUR LMS BASE URL HERE"
$env:EmailSms__SendMailPath = "smsJson/MailServices.svc/SendMail"
$env:EmailSms__MailWsKey = "PUT YOUR MAIL WS KEY HERE"
$env:EmailSms__MailUsername = "PUT YOUR USERNAME HERE"
$env:EmailSms__MailPassword = "PUT YOUR PASSWORD HERE"
$env:EmailSms__MailFrom = "PUT YOUR SENDER EMAIL HERE"
$env:EmailSms__SendSmsPath = "smsJson/SmsServices.svc/SendSMSRest"
$env:EmailSms__SmsWsKey = "PUT YOUR SMS WS KEY HERE"
$env:EmailSms__AppName = "PMIS"
