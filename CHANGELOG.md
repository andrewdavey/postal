# Changelog

## [v2.7.1](https://github.com/hermanho/postal.aspnetcore/tree/v2.7.1) (2020-12-12)

**Fixing:**
- Fix no header in email template will throw exception

## [v2.6.0](https://github.com/hermanho/postal.aspnetcore/tree/v2.6.0) (2020-01-16)

**Implemented enhancements:**
- Support Connection String when configure EmailServiceOptions ([@the-avid-engineer](https://github.com/the-avid-engineer) in [#11](https://github.com/hermanho/postal.aspnetcore/pull/11))

## [v2.5.0](https://github.com/hermanho/postal.aspnetcore/tree/v2.5.0) (2020-01-13)

**Support netcoreapp3.1**

## [v2.3.0](https://github.com/hermanho/postal.aspnetcore/tree/v2.3.0) (2019-03-18)

**Implemented enhancements:**
- Support all templates under the views folder

## [v2.2.1.1](https://github.com/hermanho/postal.aspnetcore/tree/v2.2.1.1) (2018-12-11)

**Implemented enhancements:**
- Decouple "Func\<SmtpClient\>" into "IOptions\<EmailServiceOptions\>" in EmailService constructor for dependency injection
- Allow SendAsync with MailMessage in EmailService

## [v2.1.7](https://github.com/hermanho/postal.aspnetcore/tree/v2.1.7) (2018-11-30)

**Implemented enhancements:**
- Support for style attribute in image attachment ([@amitmittal](https://github.com/amitmittal) in [#5](https://github.com/hermanho/postal.aspnetcore/pull/5))

**Fixing:**
- Fix bugs in image attachment. ([@amitmittal](https://github.com/amitmittal) in [#5](https://github.com/hermanho/postal.aspnetcore/pull/5))

## [v2.1.6](https://github.com/hermanho/postal.aspnetcore/tree/v2.1.6) (2018-11-20)

**Fixing:**
- Fix bugs in ImageEmbedder. ([@amitmittal](https://github.com/amitmittal) in [#2](https://github.com/hermanho/postal.aspnetcore/pull/2))

## [v2.1.5](https://github.com/hermanho/postal.aspnetcore/tree/v2.1.5) (2018-10-12)

**Implemented enhancements:**
- Allow pass RequestPath and RouteData in TemplateService and Razor

## [v2.1.3](https://github.com/hermanho/postal.aspnetcore/tree/f15bbc2993c1812e9cff3fca01fd717c44a675c8) (2018-09-14)

**Fixing:**
- Fix ViewDataDictionary / ViewBag do not pass to ViewContext

## [v2.1.2](https://github.com/hermanho/postal.aspnetcore/tree/063b4e21f002406f10f4a0a8a06155d333ecbb20) (2018-09-06)

**Implemented enhancements:**
- Add namespace in templateservice

## [v2.1.1](https://github.com/hermanho/postal.aspnetcore/tree/5b8324c8e6091e2c59541c43cd524cc4ad9454ca) (2018-09-06)

**Implemented enhancements:**
- Add ServiceCollectionExtensions for DI

## [v2.1.0](https://github.com/hermanho/postal.aspnetcore/tree/95a101e2f0b2496452abf9ede640e6d0fcd7522b) (2018-09-06)

**Implemented enhancements:**
- Say goodbye to RazorEngine 😀
