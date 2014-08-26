---
layout: doc
title: Previewing generated emails
---

During development you want to iterate quickly and don't want to send 
out a new mail each time you've made some changes to a template. That's
where `EmailViewResult` comes in. It's an `ActionResult` class that can be
returned from MVC actions, and simply renders your template to the browser.

To get started, just create an action that composes your `Email` object like you
would do otherwise. But then, instead of sending it, you just pass it to the `EmailViewResult`
constructor and return that from the action.

{% highlight csharp %}
public class PreviewConroller : Controller 
{
    public ActionResult Example()
    {
        dynamic email = new Email("Example");
        // set up the email ...

        return new EmailViewResult(email);
    }
}
{% endhighlight %}

There are several possible scenarios of what the `EmailViewResult` will output:

* If the resulting email simply is a text email, then the template will simply be
  rendered as text in the browser
* If the email is an html email, then the html body will be rendered in the browser
  and the To, Cc, Bcc and Subject info will be rendered as an html comment for you to check
* If the email contains both [an html and a text version](multi-part.html), then you can add a query string parameter
  to the url to indicate which version you want to see: `?format=text` or `?format=html`

If the resulting email is an html email and contains [embedded images](embedding-images.html) then those images
will be inlined in the html as well.
