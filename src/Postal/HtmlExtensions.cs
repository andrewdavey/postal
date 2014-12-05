using System.Web;
using System.Web.Mvc;
using System;

namespace Postal
{
    /// <summary>
    /// Helper methods that extend <see cref="HtmlHelper"/>.
    /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// Embeds the given image into the email and returns an HTML &lt;img&gt; tag referencing the image.
        /// </summary>
        /// <param name="html">The <see cref="HtmlHelper"/>.</param>
        /// <param name="imagePathOrUrl">An image file path or URL. A file path can be relative to the web application root directory.</param>
        /// <param name="alt">The content for the &lt;img alt&gt; attribute.</param>
        /// <returns>An HTML &lt;img&gt; tag.</returns>
        [Obsolete( "Use second method overload" )]
        public static IHtmlString EmbedImage( this HtmlHelper html, string imagePathOrUrl, string alt = "" )
        {
            return EmbedImage( html, imagePathOrUrl, new { alt = alt } );
        }

        /// <summary>
        /// Embeds the given image into the email and returns an HTML &lt;img&gt; tag referencing the image.
        /// </summary>
        /// <param name="html">The <see cref="HtmlHelper"/>.</param>
        /// <param name="imagePathOrUrl">An image file path or URL. A file path can be relative to the web application root directory.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <returns>An HTML &lt;img&gt; tag.</returns>
        public static IHtmlString EmbedImage( this HtmlHelper html, string imagePathOrUrl, object htmlAttributes = null )
        {
            if (string.IsNullOrWhiteSpace( imagePathOrUrl )) throw new ArgumentException( "Path or URL required", "imagePathOrUrl" );

            if (IsFileName( imagePathOrUrl ))
            {
                imagePathOrUrl = html.ViewContext.HttpContext.Server.MapPath( imagePathOrUrl );
            }
            var imageEmbedder = (ImageEmbedder)html.ViewData[ImageEmbedder.ViewDataKey];
            var resource = imageEmbedder.ReferenceImage( imagePathOrUrl );

            var img = new TagBuilder( "img" );

            if (htmlAttributes is string) // method overload back compatibility
                img.MergeAttribute( "alt", (string)htmlAttributes );
            else
                img.MergeAttributes( HtmlHelper.AnonymousObjectToHtmlAttributes( htmlAttributes ) );

            img.MergeAttribute( "src", String.Format( "cid:{0}", resource.ContentId ), true );
            return new MvcHtmlString( img.ToString( TagRenderMode.SelfClosing ) );
        }

        static bool IsFileName( string pathOrUrl )
        {
            return !(pathOrUrl.StartsWith( "http:", StringComparison.OrdinalIgnoreCase )
                     || pathOrUrl.StartsWith( "https:", StringComparison.OrdinalIgnoreCase ));
        }
    }
}
