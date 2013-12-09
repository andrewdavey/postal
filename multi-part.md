---
layout: doc
title: HTML and plain-text emails
---

You want to send an email that includes both rich HTML and plain text versions? Postal makes it easy.

### Step 1

Create the main view. This will have the headers and reference the views to use.

`~\Views\Emails\Example.cshtml`

<pre>
To: test@test.com
From: example@test.com
Subject: Fancy email
Views: Text, Html
</pre>

### Step 2

Create the text view. Note the naming convention: `Example.cshtml` &rarr; `Example.Text.cshtml`

`Views\Emails\Example.Text.cshtml`

<pre>
Content-Type: text/plain; charset=utf-8

Hello @ViewBag.PersonName,
This is a message
</pre>

You must specify a single Content-Type header.

### Step 3

Create the HTML view, also with a single Content-Type header.

`Views\Email\Example.Html.cshtml`

{% highlight xml %}
Content-Type: text/html; charset=utf-8

<html>
  <body>
    <p>Hello @ViewBag.PersonName,</p>
    <p>This is a message</p>
  </body>
</html>
{% endhighlight %}

