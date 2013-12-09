---
layout: doc
title: SMTP Configuration
---

Postal sends email using the .NET Framework's built-in `SmtpClient`.

You can configure this in your `Web.config` file.
See the <a href="http://msdn.microsoft.com/en-us/library/ms164240(v=vs.110).aspx">MSDN documentation</a> for more details.

{% highlight xml %}
<configuration>
  ...
  <system.net>
    <mailSettings>
      <smtp deliveryMethod="network">
        <network host="example.org" port="25" defaultCredentials="true"/>
      </smtp>
    </mailSettings>
  </system.net>
  ...
</configuration>
{% endhighlight %}
