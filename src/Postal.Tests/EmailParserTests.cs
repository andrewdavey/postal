using System;
using System.IO;
using System.Net.Mime;
using Moq;
using Should;
using Xunit;
using System.Net.Mail;

namespace Postal
{
    public class EmailParserTests
    {
        [Fact]
        public void Parse_creates_MailMessage_with_headers_and_body()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
CC: test3@test.com
Bcc: test4@test.com
Reply-To: test5@test.com
X-Test: test
Subject: Test Subject

Hello, World!";
            var renderer = new Mock<IEmailViewRenderer>();
            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, new Email("Test")))
            {
                message.To[0].Address.ShouldEqual("test1@test.com");
                message.From.Address.ShouldEqual("test2@test.com");
                message.CC[0].Address.ShouldEqual("test3@test.com");
                message.Bcc[0].Address.ShouldEqual("test4@test.com");
                message.ReplyToList[0].Address.ShouldEqual("test5@test.com");
                message.Subject.ShouldEqual("Test Subject");
                message.Headers["X-Test"].ShouldEqual("test");
                message.Body.ShouldEqual("Hello, World!");
                message.IsBodyHtml.ShouldBeFalse();
            }
        }

        [Fact]
        public void Repeating_CC_adds_each_email_address_to_list()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
CC: test3@test.com
CC: test4@test.com
CC: test5@test.com
Subject: Test Subject

Hello, World!";
            var renderer = new Mock<IEmailViewRenderer>();
            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, new Email("Test")))
            {
                message.CC[0].Address.ShouldEqual("test3@test.com");
                message.CC[1].Address.ShouldEqual("test4@test.com");
                message.CC[2].Address.ShouldEqual("test5@test.com");                
            }
        }

        [Fact]
        public void Can_parse_multiple_email_addresses_in_header()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
CC: test3@test.com, test4@test.com, test5@test.com
Subject: Test Subject

Hello, World!";
            var renderer = new Mock<IEmailViewRenderer>();
            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, new Email("Test")))
            {
                message.CC[0].Address.ShouldEqual("test3@test.com");
                message.CC[1].Address.ShouldEqual("test4@test.com");
                message.CC[2].Address.ShouldEqual("test5@test.com");
            }
        }

        [Fact]
        public void Can_detect_HTML_body()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
Subject: Test Subject

<p>Hello, World!</p>";
            var renderer = new Mock<IEmailViewRenderer>();
            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, new Email("Test")))
            {
                message.Body.ShouldEqual("<p>Hello, World!</p>");
                message.IsBodyHtml.ShouldBeTrue();
            }
        }

        [Fact]
        public void Alternative_views_are_added_to_MailMessage()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
Subject: Test Subject
Views: Text, Html";
            var text = @"Content-Type: text/plain

Hello, World!";
            var html = @"Content-Type: text/html

<p>Hello, World!</p>";

            var email = new Email("Test");
            var renderer = new Mock<IEmailViewRenderer>();
            renderer.Setup(r => r.Render(email, "Test.Text")).Returns(text);
            renderer.Setup(r => r.Render(email, "Test.Html")).Returns(html);

            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, email))
            {
                message.AlternateViews[0].ContentType.ShouldEqual(new ContentType("text/plain"));
                var textContent = new StreamReader(message.AlternateViews[0].ContentStream).ReadToEnd();
                textContent.ShouldEqual("Hello, World!");

                message.AlternateViews[1].ContentType.ShouldEqual(new ContentType("text/html"));
                var htmlContent = new StreamReader(message.AlternateViews[1].ContentStream).ReadToEnd();
                htmlContent.ShouldEqual("<p>Hello, World!</p>");
            }
        }

        [Fact]
        public void Attachments_are_added_to_MailMessage()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
Subject: Test Subject

Hello, World!";
            var email = new Email("Test");
            email.Attach(new Attachment(new MemoryStream(), "name"));
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            
            var message = parser.Parse(input, email);

            message.Attachments.Count.ShouldEqual(1);
        }

        [Fact]
        public void Exception_throw_when_alternative_view_is_missing_Content_Type_header()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
Subject: Test Subject
Views: Text, Html";
            var text = @"incorrect: header

Hello, World!";
            var html = @"incorrect: header

<p>Hello, World!</p>";

            var email = new Email("Test");
            var renderer = new Mock<IEmailViewRenderer>();
            renderer.Setup(r => r.Render(email, "Test.Text")).Returns(text);
            renderer.Setup(r => r.Render(email, "Test.Html")).Returns(html);

            var parser = new EmailParser(renderer.Object);
            Assert.Throws<Exception>(delegate
            {
                parser.Parse(input, email);
            });
        }

        [Fact]
        public void To_header_can_be_set_automatically()
        {
            dynamic email = new Email("Test");
            email.To = "test@test.com";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.To[0].Address.ShouldEqual("test@test.com");
            }
        }

        [Fact]
        public void Subject_header_can_be_set_automatically()
        {
            dynamic email = new Email("Test");
            email.Subject = "test";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.Subject.ShouldEqual("test");
            }
        }

        [Fact]
        public void From_header_can_be_set_automatically()
        {
            dynamic email = new Email("Test");
            email.From = "test@test.com";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.From.Address.ShouldEqual("test@test.com");
            }
        }

        [Fact]
        public void From_header_can_be_set_automatically_as_MailAddress()
        {
            dynamic email = new Email("Test");
            email.From = new MailAddress("test@test.com");
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.From.ShouldEqual(new MailAddress("test@test.com"));
            }
        }
    }
}
