using System;
/// <summary>
/// Gets thrown when the template service was unable to render the template
/// </summary>
public class TemplateServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TemplateServiceException"/>
    /// </summary>
    /// <param name="message"></param>
    public TemplateServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateServiceException"/>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public TemplateServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}