---
layout: doc
title: Creating email without sending
---

You don't have to rely on Postal to send your emails.
Instead, you can just create a `System.Net.Mail.MailMessage` object and process it some other way.
The `MailMessage` will contain all the email headers, content and attachments.

Postal's `EmailService` class provides a `CreateMailMessage` method.

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

        var message = emailService.CreateMailMessage(email);
        CustomProcessMailMessage(message);        

        return View();
    }
}
{% endhighlight %}

