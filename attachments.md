---
layout: doc
title: Attachments
---

To add attachments to an email, call the `Attach` method.

{% highlight csharp %}
dynamic email = new Email("Example");
email.Attach(new Attachment("c:\\attachment.txt"));
email.Send();
{% endhighlight %}

