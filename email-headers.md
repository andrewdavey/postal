---
layout: doc
title: Email headers
---

### Email addresses with names

To include the name in an email address use the following format:

<pre>
To: John Smith &lt;john@example.org&gt;
</pre>

### Multiple values

Some headers can have multiple values e.g. Bcc and CC.

You can specify these in an Email view in two ways:

Comma separate:

<pre>
Bcc: john@smith.com, harry@green.com
Subject: Example

etc
</pre>

Or, repeat the header:

<pre>
Bcc: john@smith.com
Bcc: harry@green.com
Subject: Example

etc
</pre>

