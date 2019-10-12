using System.Net.Mail;
using Moq;
using Shouldly;
using Xunit;
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Postal.AspNetCore;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace Postal
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task CreateMessage_returns_MailMessage_created_by_parser()
        {
            var html = @"Content-Type: text/html
To: test1@test.com
From: test2@test.com
Subject: Test Subject

<p>Hello, World!</p>";
            var email = new Email("Test");
            var renderer = new Mock<IEmailViewRender>();
            renderer.Setup(r => r.RenderAsync(email)).Returns(Task.FromResult(html));
            var parser = new Mock<IEmailParser>();
            var emailOptions = new EmailServiceOptions();
            emailOptions.CreateSmtpClient = () => null;
            var options = new Mock<IOptions<EmailServiceOptions>>();
            options.SetupGet(o => o.Value).Returns(emailOptions);
            var logger = new Mock<ILogger<EmailService>>();
            var service = new EmailService(renderer.Object, parser.Object, options.Object, logger.Object);
            var expectedMailMessage = new MailMessage();
            parser.Setup(p => p.ParseAsync(It.IsAny<string>(), email)).Returns(Task.FromResult(expectedMailMessage));

            var actualMailMessage = await service.CreateMailMessageAsync(email);

            actualMailMessage.ShouldBeOfType<MailMessage>();

            parser.Verify();
            renderer.Verify();
            options.Verify();
        }

        [Fact]
        public void SendAync_returns_a_Task_and_sends_email()
        {
            var html = @"Content-Type: text/html
To: test1@test.com
From: test2@test.com
Subject: Test Subject

<p>Hello, World!</p>";
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try
            {
                using (var smtp = new SmtpClient())
                {
                    smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    smtp.PickupDirectoryLocation = dir;
                    smtp.Host = "localhost"; // HACK: required by SmtpClient, but not actually used!

                    var email = new Email("Test");
                    var renderer = new Mock<IEmailViewRender>();
                    renderer.Setup(r => r.RenderAsync(email)).Returns(Task.FromResult(html));
                    var emailOptions = new EmailServiceOptions();
                    emailOptions.CreateSmtpClient = () => smtp;
                    var options = new Mock<IOptions<EmailServiceOptions>>();
                    options.SetupGet(o => o.Value).Returns(emailOptions);
                    var parser = new Mock<IEmailParser>();
                    var logger = new Mock<ILogger<EmailService>>();
                    var service = new EmailService(renderer.Object, parser.Object, options.Object, logger.Object);
                    parser.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<Email>()))
                          .Returns(Task.FromResult(new MailMessage("test@test.com", "test@test.com")));

                    var sending = service.SendAsync(email);
                    sending.Wait();

                    Directory.GetFiles(dir).Length.ShouldBe(1);
                    parser.Verify();
                    renderer.Verify();
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Dependency_injection_default()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            var viewEngine = new Mock<IRazorViewEngine>();
            var tempDataProvider = new Mock<ITempDataProvider>();
#if NETCOREAPP3_0
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
#else
            var hostingEnvironment = new Mock<IHostingEnvironment>();
#endif
            var logger = new Mock<ILogger<EmailService>>();
            serviceCollection.AddSingleton(logger.Object);
            serviceCollection.AddSingleton(viewEngine.Object);
            serviceCollection.AddSingleton(tempDataProvider.Object);
            serviceCollection.AddSingleton(hostingEnvironment.Object);

            serviceCollection.AddPostal();

            var services = serviceCollection.BuildServiceProvider();
            var emailService = services.GetRequiredService<IEmailService>();

            var emailOption = services.GetRequiredService<IOptions<EmailServiceOptions>>();

            emailService.ShouldBeOfType<EmailService>();
            var smtpClient = ((EmailService)emailService).CreateSmtpClient;
            smtpClient.ShouldBe(emailOption.Value.CreateSmtpClient);
        }

        [Fact]
        public void Dependency_injection_smtpOtions1()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "Host", "abc" },
                { "Port","12345"},
                { "FromAddress","qwerty"},
                { "UserName","zxcvbn"},
                { "Password","asdfgh"}
            });
            var _configuration = configBuilder.Build();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            var viewEngine = new Mock<IRazorViewEngine>();
            var tempDataProvider = new Mock<ITempDataProvider>();
#if NETCOREAPP3_0
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
#else
            var hostingEnvironment = new Mock<IHostingEnvironment>();
#endif
            var logger = new Mock<ILogger<EmailService>>();
            serviceCollection.AddSingleton(logger.Object);
            serviceCollection.AddSingleton(viewEngine.Object);
            serviceCollection.AddSingleton(tempDataProvider.Object);
            serviceCollection.AddSingleton(hostingEnvironment.Object);

            serviceCollection.Configure<EmailServiceOptions>(_configuration);
            serviceCollection.AddPostal();

            var services = serviceCollection.BuildServiceProvider();
            var emailService = services.GetRequiredService<IEmailService>();

            var emailOption = services.GetRequiredService<IOptions<EmailServiceOptions>>().Value;

            EmailServiceOptions emailOptionField = GetInstanceField(typeof(EmailService), emailService, "options") as EmailServiceOptions;
            emailOption.Host.ShouldBe("abc");
            emailOption.Port.ShouldBe(12345);
            emailOption.FromAddress.ShouldBe("qwerty");
            emailOption.UserName.ShouldBe("zxcvbn");
            emailOption.Password.ShouldBe("asdfgh");

            emailOptionField.Host.ShouldBe("abc");
            emailOptionField.Port.ShouldBe(12345);
            emailOptionField.FromAddress.ShouldBe("qwerty");
            emailOptionField.UserName.ShouldBe("zxcvbn");
            emailOptionField.Password.ShouldBe("asdfgh");
        }

        [Fact]
        public void Dependency_injection_smtpOtions2()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            var viewEngine = new Mock<IRazorViewEngine>();
            var tempDataProvider = new Mock<ITempDataProvider>();
#if NETCOREAPP3_0
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
#else
            var hostingEnvironment = new Mock<IHostingEnvironment>();
#endif
            var logger = new Mock<ILogger<EmailService>>();
            serviceCollection.AddSingleton(logger.Object);
            serviceCollection.AddSingleton(viewEngine.Object);
            serviceCollection.AddSingleton(tempDataProvider.Object);
            serviceCollection.AddSingleton(hostingEnvironment.Object);

            serviceCollection.Configure<EmailServiceOptions>(o =>
            {
                o.Host = "abc";
                o.Port = 12345;
                o.FromAddress = "qwerty";
                o.UserName = "zxcvbn";
                o.Password = "asdfgh";
                o.CreateSmtpClient = () => new FactExcetpionForSmtpClient();
            });

            serviceCollection.AddPostal();

            var services = serviceCollection.BuildServiceProvider();
            var emailService = services.GetRequiredService<IEmailService>();

            EmailServiceOptions emailOptionField = GetInstanceField(typeof(EmailService), emailService, "options") as EmailServiceOptions;
            emailOptionField.Host.ShouldBe("abc");
            emailOptionField.Port.ShouldBe(12345);
            emailOptionField.FromAddress.ShouldBe("qwerty");
            emailOptionField.UserName.ShouldBe("zxcvbn");
            emailOptionField.Password.ShouldBe("asdfgh");
            
            emailOptionField.CreateSmtpClient().ShouldBeOfType<FactExcetpionForSmtpClient>();
        }

        [Fact]
        public void Dependency_injection_smtpOtions3()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            var viewEngine = new Mock<IRazorViewEngine>();
            var tempDataProvider = new Mock<ITempDataProvider>();
#if NETCOREAPP3_0
            var hostingEnvironment = new Mock<IWebHostEnvironment>();
#else
            var hostingEnvironment = new Mock<IHostingEnvironment>();
#endif
            var logger = new Mock<ILogger<EmailService>>();
            serviceCollection.AddSingleton(logger.Object);
            serviceCollection.AddSingleton(viewEngine.Object);
            serviceCollection.AddSingleton(tempDataProvider.Object);
            serviceCollection.AddSingleton(hostingEnvironment.Object);

            serviceCollection.Configure<EmailServiceOptions>(o =>
            {
                o.CreateSmtpClient = () => throw new FactExcetpionForSmtpCreation();
            });

            serviceCollection.AddPostal();

            var services = serviceCollection.BuildServiceProvider();
            var emailService = services.GetRequiredService<IEmailService>();

            Assert.ThrowsAsync<FactExcetpionForSmtpCreation>(() => emailService.SendAsync(new Email("testView")));
            EmailServiceOptions emailOptionField = GetInstanceField(typeof(EmailService), emailService, "options") as EmailServiceOptions;
            Assert.Throws<FactExcetpionForSmtpCreation>(() => emailOptionField.CreateSmtpClient());
        }

        private static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        class FactExcetpionForSmtpClient : SmtpClient
        {

        }

        class FactExcetpionForSmtpCreation : Exception
        {

        }
    }
}
