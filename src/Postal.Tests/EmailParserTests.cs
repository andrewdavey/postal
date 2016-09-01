using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using Moq;
using Should;
using Xunit;

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
Sender: test6@test.com
Priority: high
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
                message.Sender.Address.ShouldEqual("test6@test.com");
                message.Priority.ShouldEqual(MailPriority.High);
                message.Headers["X-Test"].ShouldEqual("test");
                message.Body.ShouldEqual("Hello, World!");
                message.IsBodyHtml.ShouldBeFalse();
            }
        }

        [Fact]
        public void Parse_creates_email_addresses_with_display_name()
        {
            var input = @"
To: John H Smith test1@test.com
From: test2@test.com
Subject: Test Subject

Hello, World!";
            var renderer = new Mock<IEmailViewRenderer>();
            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, new Email("Test")))
            {
                message.To[0].Address.ShouldEqual("test1@test.com");
                message.To[0].DisplayName.ShouldEqual("John H Smith");
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
        public void Can_detect_HTML_body_with_leading_whitespace()
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
            renderer.Setup(r => r.Render(email, "Test.Text", null)).Returns(text);
            renderer.Setup(r => r.Render(email, "Test.Html", null)).Returns(html);

            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, email))
            {
                message.AlternateViews[0].ContentType.ShouldEqual(new ContentType("text/plain; charset=utf-16"));
                var textContent = new StreamReader(message.AlternateViews[0].ContentStream).ReadToEnd();
                textContent.ShouldEqual("Hello, World!");

                message.AlternateViews[1].ContentType.ShouldEqual(new ContentType("text/html; charset=utf-16"));
                var htmlContent = new StreamReader(message.AlternateViews[1].ContentStream).ReadToEnd();
                htmlContent.ShouldEqual("<p>Hello, World!</p>");
            }
        }

        [Fact]
        public void Given_base_view_is_full_path_Then_alternative_views_are_correctly_looked_up()
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

            var email = new Email("~/Views/Emails/Test.cshtml");
            var renderer = new Mock<IEmailViewRenderer>();
            renderer.Setup(r => r.Render(email, "~/Views/Emails/Test.Text.cshtml", null)).Returns(text);
            renderer.Setup(r => r.Render(email, "~/Views/Emails/Test.Html.cshtml", null)).Returns(html);

            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, email))
            {
                message.AlternateViews[0].ContentType.ShouldEqual(new ContentType("text/plain; charset=utf-16"));
                var textContent = new StreamReader(message.AlternateViews[0].ContentStream).ReadToEnd();
                textContent.ShouldEqual("Hello, World!");

                message.AlternateViews[1].ContentType.ShouldEqual(new ContentType("text/html; charset=utf-16"));
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
        public void ContentType_determined_by_view_name_when_alternative_view_is_missing_Content_Type_header()
        {
            var input = @"
To: test1@test.com
From: test2@test.com
Subject: Test Subject
Views: Text, Html";
            var text = @"
Hello, World!";
            var html = @"
<p>Hello, World!</p>";

            var email = new Email("Test");
            var renderer = new Mock<IEmailViewRenderer>();
            renderer.Setup(r => r.Render(email, "Test.Text", null)).Returns(text);
            renderer.Setup(r => r.Render(email, "Test.Html", null)).Returns(html);

            var parser = new EmailParser(renderer.Object);
            using (var message = parser.Parse(input, email))
            {
                message.AlternateViews[0].ContentType.MediaType.ShouldEqual("text/plain");
                message.AlternateViews[1].ContentType.MediaType.ShouldEqual("text/html");
            }
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

        [Fact]
        public void ReplyTo_header_can_be_set_automatically()
        {
            dynamic email = new Email("Test");
            email.ReplyTo = "test@test.com";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.ReplyToList[0].Address.ShouldEqual("test@test.com");
            }
        }

        [Fact]
        public void Priority_header_can_be_set_automatically()
        {
            dynamic email = new Email("Test");
            email.Priority = "high";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.Priority = MailPriority.High;
            }
        }

        [Fact]
        public void Priority_header_can_be_set_automatically_from_MailPriorityEnum()
        {
            dynamic email = new Email("Test");
            email.Priority = MailPriority.High;
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            using (var message = parser.Parse("body", (Email)email))
            {
                message.Priority = MailPriority.High;
            }
        }

        [Fact]
        public void Email_address_can_include_display_name()
        {
            var input = @"To: ""John Smith"" <test@test.com>
From: test2@test.com
Subject: test

message";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            var email = new Email("Test");
            using (var message = parser.Parse(input, email))
            {
                message.To[0].Address.ShouldEqual("test@test.com");
                message.To[0].DisplayName.ShouldEqual("John Smith");
            }
        }

        [Fact]
        public void Can_parse_reply_to()
        {
            var input = @"To: test@test.com
From: test2@test.com
Reply-To: other@test.com
Subject: test

message";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            var email = new Email("Test");
            using (var message = parser.Parse(input, email))
            {
                message.ReplyToList[0].Address.ShouldEqual("other@test.com");
                
                // Check for bug reported here: http://aboutcode.net/2010/11/17/going-postal-generating-email-with-aspnet-mvc-view-engines.html#comment-153486994
                // Should not add anything extra to the 'To' list.
                message.To.Count.ShouldEqual(1);
                message.To[0].Address.ShouldEqual("test@test.com");
            }
        }

        [Fact]
        public void Do_not_implicitly_add_To_from_model_when_set_in_view()
        {
            var input = @"To: test@test.com
From: test2@test.com
Subject: test

message";
            var parser = new EmailParser(Mock.Of<IEmailViewRenderer>());
            dynamic email = new Email("Test");
            email.To = "test@test.com";
            using (var message = parser.Parse(input, (Email)email))
            {
                // Check for bug reported here: http://aboutcode.net/2010/11/17/going-postal-generating-email-with-aspnet-mvc-view-engines.html#comment-153486994
                // Should not add anything extra to the 'To' list.
                message.To.Count.ShouldEqual(1);
                message.To[0].Address.ShouldEqual("test@test.com");
            }
        }
    }
}
