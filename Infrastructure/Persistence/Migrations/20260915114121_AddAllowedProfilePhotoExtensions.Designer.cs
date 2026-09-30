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
    [Migration("20260915114121_AddAllowedProfilePhotoExtensions")]
    partial class AddAllowedProfilePhotoExtensions
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

                    b.Property<DateTime>("UploadDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

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

                    b.Property<int>("SortOrder")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("TransferAmount")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.HasKey("Id");

                    b.HasIndex("Iban")
                        .IsUnique();

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

                    b.Property<string>("VatNo")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("CommercialNo");

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

                    b.HasIndex("RequestId")
                        .IsUnique();

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

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

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

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

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

                    b.Property<int>("SortOrder")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.HasKey("Id");

                    b.HasIndex("Label")
                        .IsUnique();

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

                    b.HasIndex("CompanyId");

                    b.HasIndex("ProjectId");

                    b.HasIndex("RequestDate");

                    b.HasIndex("RequestNumber")
                        .IsUnique();

                    b.HasIndex("ReviewedById");

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

                    b.HasKey("Id");

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

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ReasonId");

                    b.HasIndex("RequestId");

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

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int>("LicenseValidityMonths")
                        .HasColumnType("int");

                    b.Property<int>("LoginLockoutMinutes")
                        .HasColumnType("int");

                    b.Property<int>("MaxLoginAttempts")
                        .HasColumnType("int");

                    b.Property<int>("MaxUploadSizeMb")
                        .HasColumnType("int");

                    b.Property<int>("ResponseSlaBusinessDays")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("SystemSettings");

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000001"),
                            AllowedAttachmentExtensions = "PDF,JPG,PNG",
                            AllowedProfilePhotoExtensions = "JPG,PNG",
                            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            LicenseValidityMonths = 12,
                            LoginLockoutMinutes = 2,
                            MaxLoginAttempts = 10,
                            MaxUploadSizeMb = 20,
                            ResponseSlaBusinessDays = 3
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

                    b.Property<string>("SystemName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

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

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("DeletedAccount")
                        .HasColumnType("bit");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Mobile")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("NationalId")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("PhotoFileRef")
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("NationalId")
                        .IsUnique()
                        .HasFilter("[NationalId] IS NOT NULL");

                    b.HasIndex("Role");

                    b.ToTable("Users");
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
                });
#pragma warning restore 612, 618
        }
    }
}
