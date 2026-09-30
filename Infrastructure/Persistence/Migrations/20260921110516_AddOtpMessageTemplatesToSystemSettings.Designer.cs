using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PMIS.Infrastructure.Persistence;

#nullable disable

namespace PMIS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PmisDbContext))]
    [Migration("20260921110516_AddOtpMessageTemplatesToSystemSettings")]
    partial class AddOtpMessageTemplatesToSystemSettings
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("PMIS.Domain.Entities.Attachment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("AttachmentType")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<long>("FileSize")
                        .HasColumnType("bigint");

                    b.Property<string>("FileType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<Guid>("RequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<DateTime>("UploadDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("RequestId", "AttachmentType");

                    b.ToTable("Attachments");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.BankAccount", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("AccountHolderName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("BankName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Iban")
                        .IsRequired()
                        .HasMaxLength(34)
                        .HasColumnType("nvarchar(34)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<int>("SortOrder")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("TransferAmount")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("Iban")
                        .IsUnique();

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("SortOrder", "CreatedDate");

                    b.ToTable("BankAccounts");

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000201"),
                            AccountHolderName = "أمانة منطقة المدينة المنورة",
                            BankName = "مصرف الراجحي",
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Iban = "SA03 8000 0000 6080 1016 7519",
                            IsActive = true,
                            SortOrder = 0,
                            TransferAmount = "5000"
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000202"),
                            AccountHolderName = "أمانة منطقة المدينة المنورة",
                            BankName = "البنك الأهلي السعودي",
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Iban = "SA44 2000 0001 2345 6789 1234",
                            IsActive = true,
                            SortOrder = 1,
                            TransferAmount = "7500"
                        });
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Company", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("CommercialNo")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("CompanyActivity")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("CompanyName")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Iban")
                        .HasMaxLength(34)
                        .HasColumnType("nvarchar(34)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LogoFileRef")
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<string>("NationalAddress")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<string>("VatNo")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("CommercialNo");

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.ToTable("Companies");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.License", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime?>("ActivationDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LicenseKey")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("NoOfLicenses")
                        .HasColumnType("int");

                    b.Property<Guid>("RequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RequestId")
                        .IsUnique();

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("Status", "EndDate");

                    b.ToTable("Licenses");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.NotificationPreference", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("PreferenceKey")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("UserId", "PreferenceKey")
                        .IsUnique();

                    b.ToTable("NotificationPreferences");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("ContractNumber")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("ProjectName")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ProjectNumber")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ProjectOwner")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.RejectionReasonOption", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("DefaultNote")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<string>("Hint")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<int>("SortOrder")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("Label")
                        .IsUnique();

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("SortOrder", "CreatedDate");

                    b.ToTable("RejectionReasonOptions");

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000101"),
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            DefaultNote = "تبيّن وجود نقص أو خطأ في المستندات المرفقة (سجل تجاري/ضريبة القيمة المضافة/عنوان وطني). يرجى مراجعة المرفقات وإعادة تقديم الطلب.",
                            Hint = "سجل تجاري، ضريبة القيمة المضافة، عنوان وطني...",
                            IsActive = true,
                            Label = "نقص أو خطأ في المستندات",
                            SortOrder = 0
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000102"),
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            DefaultNote = "رقم العقد المرجعي المدخل غير مطابق لسجلات الأمانة أو أن العقد منتهي الصلاحية. يرجى التحقق من بيانات العقد وإعادة المحاولة.",
                            Hint = "رقم العقد المرجعي غير مطابق أو منتهٍ",
                            IsActive = true,
                            Label = "خلل في بيانات العقد",
                            SortOrder = 0
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000103"),
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            DefaultNote = "الشركة/مقدّم الطلب لا يستوفي شروط الاستفادة من هذه الخدمة حاليًا.",
                            Hint = "لا يستوفي شروط الاستفادة من الخدمة",
                            IsActive = true,
                            Label = "مقدّم الطلب غير مؤهل",
                            SortOrder = 0
                        },
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000104"),
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            DefaultNote = "",
                            Hint = "يُذكر تفصيليًا في الملاحظة أدناه",
                            IsActive = true,
                            Label = "سبب آخر",
                            SortOrder = 100
                        });
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Request", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("ApplicantEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("ApplicantFirstName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ApplicantLastName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ApplicantMobile")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<Guid>("CompanyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Decision")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Notes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("PaymentConfirmedDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("RequestDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("RequestNumber")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime?>("ReviewDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("ReviewedById")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(35)
                        .HasColumnType("nvarchar(35)");

                    b.Property<Guid?>("SystemId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("UserTypeDisplay")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("CompanyId");

                    b.HasIndex("ProjectId");

                    b.HasIndex("RequestDate");

                    b.HasIndex("RequestNumber")
                        .IsUnique();

                    b.HasIndex("ReviewedById");

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("Status");

                    b.HasIndex("SystemId");

                    b.HasIndex("UserId");

                    b.ToTable("Requests");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.RequestHistory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("EventLabel")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<Guid>("RequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("RequestId", "EventDate");

                    b.ToTable("RequestHistories");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.RequestNote", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("NoteText")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<string>("NoteType")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<Guid>("ReasonId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("RequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("ReasonId");

                    b.HasIndex("RequestId");

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("UserId");

                    b.ToTable("RequestNotes");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.SystemSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("AllowedAttachmentExtensions")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("AllowedProfilePhotoExtensions")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("AuditLogMaxCount")
                        .HasColumnType("int");

                    b.Property<int>("AuditLogRetentionDays")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int>("LicenseValidityMonths")
                        .HasColumnType("int");

                    b.Property<int>("LoginLockoutMinutes")
                        .HasColumnType("int");

                    b.Property<int>("MaxActiveRequestsPerUser")
                        .HasColumnType("int");

                    b.Property<int>("MaxLoginAttempts")
                        .HasColumnType("int");

                    b.Property<int>("MaxUploadSizeMb")
                        .HasColumnType("int");

                    b.Property<int>("NotificationMaxCount")
                        .HasColumnType("int");

                    b.Property<int>("NotificationRetentionDays")
                        .HasColumnType("int");

                    b.Property<string>("OtpEmailMessageTemplate")
                        .IsRequired()
                        .HasMaxLength(500)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(500)");

                    b.Property<int>("OtpExpiryMinutes")
                        .HasColumnType("int");

                    b.Property<int>("OtpMaxResendCount")
                        .HasColumnType("int");

                    b.Property<int>("OtpResendCooldownSeconds")
                        .HasColumnType("int");

                    b.Property<string>("OtpSmsMessageTemplate")
                        .IsRequired()
                        .HasMaxLength(160)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(160)");

                    b.Property<int>("ResponseSlaBusinessDays")
                        .HasColumnType("int");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<string>("TermsAgreementLabel")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("TermsAndConditionsText")
                        .IsRequired()
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.ToTable("SystemSettings");

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000001"),
                            AllowedAttachmentExtensions = "PDF,JPG,PNG",
                            AllowedProfilePhotoExtensions = "JPG,PNG",
                            AuditLogMaxCount = 0,
                            AuditLogRetentionDays = 0,
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            LicenseValidityMonths = 12,
                            LoginLockoutMinutes = 2,
                            MaxActiveRequestsPerUser = 5,
                            MaxLoginAttempts = 10,
                            MaxUploadSizeMb = 20,
                            NotificationMaxCount = 0,
                            NotificationRetentionDays = 0,
                            OtpEmailMessageTemplate = "يرجى إدخال رمز التحقق: @MAIL",
                            OtpExpiryMinutes = 5,
                            OtpMaxResendCount = 5,
                            OtpResendCooldownSeconds = 45,
                            OtpSmsMessageTemplate = "يرجى إدخال رمز التحقق: @SMS",
                            ResponseSlaBusinessDays = 3,
                            TermsAgreementLabel = "أقر بأن جميع البيانات الواردة في هذا الطلب صحيحة، وأنني اطّلعت على الشروط والأحكام الخاصة بتفعيل رخصة الدخول على أنظمة إدارة المشاريع، وأوافق على الالتزام بها.",
                            TermsAndConditionsText = "يقر مقدم الطلب بصحة واكتمال جميع البيانات والمعلومات المقدمة، ويتحمل المسؤولية الكاملة عن أي بيانات غير صحيحة أو ناقصة قد يترتب عليها تأخير في التفعيل أو رفض الطلب.\nيشترط أن يكون مقدم الطلب ممثلًا لاستشاري أو مقاول مرتبط بعقد ساري ومعتمد لدى أمانة منطقة المدينة المنورة.\nيحق للأمانة طلب ما يثبت علاقة المستخدم بالمشروع، مثل العقد أو خطاب التفويض أو أي مستند رسمي آخر، قبل تفعيل صلاحية الاستخدام.\nتُمنح صلاحية استخدام النظام للمستخدم المحدد في الطلب فقط، ويُمنع مشاركة بيانات الدخول مع أي طرف آخر، ويتحمل المستخدم كامل المسؤولية عن أي إساءة استخدام.\nتكون مدة صلاحية رخصة استخدام النظام (12) شهرًا تبدأ من تاريخ تفعيل صلاحية الدخول.\nيجوز استبدال المستخدم المرتبط بالرخصة بمستخدم آخر من نفس الجهة المتعاقدة خلال فترة سريان الرخصة، بناءً على طلب رسمي معتمد وتحديث جميع بيانات المستخدم الجديد، دون رسوم إضافية.\nيُستخدم الحساب للأغراض المرتبطة بمشاريع أمانة منطقة المدينة المنورة فقط، ويحظر استخدامه لأي أغراض غير مصرح بها.\nيلتزم المستخدم بجميع السياسات والتعليمات الصادرة عن الأمانة المتعلقة بالأمن السيبراني وحماية البيانات وسرية المعلومات.\nيلتزم المستخدم بالمحافظة على سرية البيانات والمعلومات المتاحة عبر النظام وعدم الإفصاح عنها أو استخدامها خارج نطاق العمل المعتمد.\nيقر مقدم الطلب بأن رسوم الرخصة المدفوعة غير قابلة للاسترداد بعد إتمام عملية التفعيل، سواء تم استخدام الرخصة أم لا.\nتحتفظ الأمانة بحق تعديل أسعار الرخص مع إشعار الأطراف ذات العلاقة مسبقًا.\nتطبق ضريبة القيمة المضافة وجميع الرسوم النظامية الأخرى وفق الأنظمة والتعليمات المعمول بها في المملكة العربية السعودية.\nيتحمل مقدم الطلب كامل المسؤولية عن إجراءات شراء الرخصة، بما في ذلك التعاقد، والسداد، والتجديد، وأي التزامات مرتبطة بها.\nتُحتسب ضريبة القيمة المضافة على قيمة الرخصة وفق النسبة النظامية السارية وقت السداد.\nلا يترتب على إيقاف الصلاحية أو انتهاء المشروع أو انتهاء العقد أي التزام على الأمانة بإعادة الرسوم المدفوعة.\nيلتزم مقدم الطلب بتحويل قيمة الرخصة إلى الحساب البنكي المحدد، مع إرفاق إيصال التحويل ضمن المرفقات الإلزامية.\nيعتمد تاريخ وصول المبلغ إلى حساب الأمانة مرجعًا لبدء إجراءات مراجعة الطلب وتفعيل الرخصة.\nيتحمل مقدم الطلب مسؤولية أي تأخير ناتج عن تأخير التحويل أو إدخال بيانات حساب غير صحيحة.\nيتم تفعيل المستخدم خلال ثلاثة (3) أيام عمل من تاريخ تأكيد وصول مبلغ الرخصة، ما لم توجد أسباب تقنية أو تنظيمية تستدعي تمديد المدة مع إشعار المستخدم.\nيحق للأمانة تعليق أو إلغاء صلاحية الاستخدام في حالات إساءة استخدام النظام، أو مشاركة بيانات الدخول، أو انتهاء العقد أو المشروع، أو بناءً على توجيهات الجهات المختصة، دون الالتزام برد الرسوم أو تقديم تعويض.\nيعتمد التوقيع الإلكتروني أو التوقيع المرفق في الطلب وسيلة رسمية لاعتماد الإجراءات الخاصة بالخدمة.\nيلتزم مقدم الطلب بإرفاق شعار الجهة عند استخدامه في تقارير النظام، ويقر بامتلاكه الحقوق النظامية المتعلقة به، ولا تتحمل الأمانة أي مسؤولية عن النزاعات المتعلقة بحقوق الملكية.\nيلتزم مقدم الطلب بتحديث بياناته، مثل رقم الجوال والبريد الإلكتروني واسم الشركة وبيانات الحساب البنكي، عند حدوث أي تغيير، من خلال القنوات الرسمية المعتمدة.\nتعتمد آخر البيانات المسجلة في النظام بيانات رسمية للتواصل وإرسال الإشعارات والتنبيهات.\nتحتفظ الأمانة بحق تحديث أو تعديل هذه الشروط والأحكام عند الحاجة، مع إشعار الأطراف ذات العلاقة بذلك.\nيُعد استكمال نموذج الطلب وإرساله مع جميع المرفقات المطلوبة إقرارًا من مقدم الطلب بالاطلاع على هذه الشروط والأحكام وفهمها والموافقة عليها والالتزام بها طوال فترة استخدام النظام."
                        });
                });

            modelBuilder.Entity("PMIS.Domain.Entities.TargetSystem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("bit");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<string>("SystemName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("SystemName")
                        .IsUnique();

                    b.ToTable("TargetSystems");

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000001"),
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            IsDelete = false,
                            SystemName = "Oracle Primavera Unifier"
                        });
                });

            modelBuilder.Entity("PMIS.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<string>("ArabicFatherName")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ArabicFirstName")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ArabicGrandFatherName")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ArabicNationality")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("DeletedAccount")
                        .HasColumnType("bit");

                    b.Property<string>("DobGregorian")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Gender")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Mobile")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("NationalId")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("PasswordHash")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("PhotoFileRef")
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasFilter("[Email] IS NOT NULL");

                    b.HasIndex("NationalId")
                        .IsUnique()
                        .HasFilter("[NationalId] IS NOT NULL");

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.UserRoleAssignment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasDefaultValueSql("NEWID()");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("RowId"));

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RowId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RowId"));

                    b.HasIndex("UserId", "Role")
                        .IsUnique();

                    b.ToTable("UserRoleAssignments");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Attachment", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.Request", "Request")
                        .WithMany("Attachments")
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Request");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.License", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.Request", "Request")
                        .WithOne("License")
                        .HasForeignKey("PMIS.Domain.Entities.License", "RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Request");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.NotificationPreference", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Request", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.Company", "Company")
                        .WithMany("Requests")
                        .HasForeignKey("CompanyId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("PMIS.Domain.Entities.Project", "Project")
                        .WithMany("Requests")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("PMIS.Domain.Entities.User", "ReviewedBy")
                        .WithMany()
                        .HasForeignKey("ReviewedById")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("PMIS.Domain.Entities.TargetSystem", "System")
                        .WithMany("Requests")
                        .HasForeignKey("SystemId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("PMIS.Domain.Entities.User", "User")
                        .WithMany("Requests")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Company");

                    b.Navigation("Project");

                    b.Navigation("ReviewedBy");

                    b.Navigation("System");

                    b.Navigation("User");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.RequestHistory", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.Request", "Request")
                        .WithMany("RequestHistories")
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Request");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.RequestNote", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.RejectionReasonOption", "Reason")
                        .WithMany("RequestNotes")
                        .HasForeignKey("ReasonId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("PMIS.Domain.Entities.Request", "Request")
                        .WithMany("RequestNotes")
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PMIS.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Reason");

                    b.Navigation("Request");

                    b.Navigation("User");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.UserRoleAssignment", b =>
                {
                    b.HasOne("PMIS.Domain.Entities.User", "User")
                        .WithMany("RoleAssignments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Company", b =>
                {
                    b.Navigation("Requests");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Project", b =>
                {
                    b.Navigation("Requests");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.RejectionReasonOption", b =>
                {
                    b.Navigation("RequestNotes");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.Request", b =>
                {
                    b.Navigation("Attachments");

                    b.Navigation("License");

                    b.Navigation("RequestHistories");

                    b.Navigation("RequestNotes");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.TargetSystem", b =>
                {
                    b.Navigation("Requests");
                });

            modelBuilder.Entity("PMIS.Domain.Entities.User", b =>
                {
                    b.Navigation("Requests");

                    b.Navigation("RoleAssignments");
                });
#pragma warning restore 612, 618
        }
    }
}
