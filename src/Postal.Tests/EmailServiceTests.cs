using System.Net.Mail;
using Moq;
using Shouldly;
using Xunit;
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

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
            var service = new EmailService(renderer.Object, parser.Object, () => null);
            var expectedMailMessage = new MailMessage();
            parser.Setup(p => p.ParseAsync(It.IsAny<string>(), email)).Returns(Task.FromResult(expectedMailMessage));

            var actualMailMessage = await service.CreateMailMessageAsync(email);

            actualMailMessage.ShouldBeOfType<MailMessage>();

            parser.Verify();
            renderer.Verify();
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
                    var parser = new Mock<IEmailParser>();
                    var service = new EmailService(renderer.Object, parser.Object, () => smtp);
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
    }
}
