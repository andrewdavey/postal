---
layout: doc
title: Embedding images into emails
---

Postal provides an HTML helper extension method to allow embedding images into an email, instead of linking to an external image URL.

First, make sure your `Web.config` razor configuration contains the Postal namespace.
This makes the helper available in your email views.

{% highlight xml %}
<configuration>
  <system.web.webPages.razor>
    <pages pageBaseType="System.Web.Mvc.WebViewPage">
      <namespaces>
        <add namespace="Postal" />
      </namespaces>
    </pages>
  </system.web.webPages.razor>
</configuration>
{% endhighlight %}

The `EmbedImage` method will embed the given image into the email and generate an `<img/>` tag to reference it.

<pre>
To: john@example.org
From: app@example.org
Subject: Image

@Html.EmbedImage("~/content/postal.jpg")
</pre>

Postal will try to resolve the image's filename, relative to the web application's root directory.


