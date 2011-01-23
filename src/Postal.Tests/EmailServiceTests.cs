using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Moq;
using System.Net.Mail;
using Should;

namespace Postal
{
    public class EmailServiceTests
    {
        [Fact]
        public void CreateMessage_returns_MailMessage_created_by_parser()
        {
            var renderer = new Mock<IEmailViewRenderer>();
            var parser = new Mock<IEmailParser>();
            var service = new EmailService(renderer.Object, parser.Object);
            var email = new Email("Test");
            var expectedMailMessage = new MailMessage();
            parser.Setup(p => p.Parse(It.IsAny<string>(), email)).Returns(expectedMailMessage);
            
            var actualMailMessage = service.CreateMailMessage(email);

            actualMailMessage.ShouldBeSameAs(expectedMailMessage);
        }
    }
}
