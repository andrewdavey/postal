---
layout: doc
title: Unit Testing
---
 
When unit testing code that uses Postal, you may want to verify
that an email would be sent, without actual sending it.

Postal provides an interface `IEmailService` and an 
implementation, `EmailService`, which actually sends
email.

Assuming you use some kind of IoC container, configure it to inject
an `IEmailService` into your controller.

Then use the service to send email objects (instead of calling `Email.Send()`).

{% highlight csharp %}
public class ExampleController : Controller 
{
    public ExampleController(IEmailService emailService)
    {
        this.emailService = emailService;
    }

    readonly IEmailService emailService;

    public ActionResult Index()
    {
        dynamic email = new Email("Example");
        // ...
        emailService.Send(email);
        return View();
    }
}
{% endhighlight %}

Test this controller by creating a mock of the `IEmailService` interface.

Here's an example using <a href="https://github.com/FakeItEasy/FakeItEasy">FakeItEasy</a>.

{% highlight csharp %}
[Test]
public void ItSendsEmail()
{
    var emailService = A.Fake<IEmailService>();
    var controller = new ExampleController(emailService);
    controller.Index();
    A.CallTo(() => emailService.Send(A<Email>._))
     .MustHaveHappened();
}
{% endhighlight %}

