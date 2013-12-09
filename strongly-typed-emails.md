---
layout: doc
title: Strongly-typed emails
---

Not everyone likes using dynamic objects.
Postal lets you strongly type your email data if you need to.

Step 1 - Define a class that inherits from Email

{% highlight csharp %}
namespace App.Models
{
  public class ExampleEmail : Email
  {
    public string To { get; set; }
    public string Message { get; set; }
  }
}
{% endhighlight %}

Step 2 - Use that class!

{% highlight csharp %}
public void Send()
{
  var email = new ExampleEmail
  {
    To = "hello@world.com",
    Message = "Strong typed message"
  };
  email.Send();
}
{% endhighlight %}

Step 3 - Create a view that uses your model.
The name of the view is based on the class name.
So `ExampleEmail` requires a view called `Example.cshtml`.

<pre>
@model App.Models.ExampleEmail
To: @Model.To
From: postal@example.com
Subject: Example

Hello,
@Model.Message
Thanks!
</pre>


