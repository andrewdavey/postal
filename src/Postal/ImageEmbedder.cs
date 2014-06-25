using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.IO;
using System.Net.Mime;
using System.Net;
using System.Text.RegularExpressions;

namespace Postal
{
    /// <summary>
    /// Used by the <see cref="HtmlExtensions.EmbedImage"/> helper method.
    /// It generates the <see cref="LinkedResource"/> objects need to embed images into an email.
    /// </summary>
    public class ImageEmbedder
    {
        /// <summary>
        /// Creats a new <see cref="ImageEmbedder"/>.
        /// </summary>
        public ImageEmbedder()
        {
            createLinkedResource = CreateLinkedResource;
        }

        /// <summary>
        /// Creates a new <see cref="ImageEmbedder"/>.
        /// </summary>
        /// <param name="createLinkedResource">A delegate that creates a <see cref="LinkedResource"/> from an image path or URL.</param>
        public ImageEmbedder(Func<string, LinkedResource> createLinkedResource)
        {
            this.createLinkedResource = createLinkedResource;
        }

        readonly Func<string, LinkedResource> createLinkedResource;
        readonly Dictionary<string, LinkedResource> images = new Dictionary<string, LinkedResource>();

        /// <summary>
        /// Creates a <see cref="LinkedResource"/> from an image path or URL.
        /// </summary>
        /// <param name="imagePathOrUrl">The image path or URL.</param>
        /// <returns>A new <see cref="LinkedResource"/></returns>
        public static LinkedResource CreateLinkedResource(string imagePathOrUrl)
        {
            if (Uri.IsWellFormedUriString(imagePathOrUrl, UriKind.Absolute))
            {
                var client = new WebClient();
                var bytes = client.DownloadData(imagePathOrUrl);
                return new LinkedResource(new MemoryStream(bytes));
            }
            else
            {
                return new LinkedResource(File.OpenRead(imagePathOrUrl));
            }
        }

        /// <summary>
        /// Records a reference to the given image.
        /// </summary>
        /// <param name="imagePathOrUrl">The image path or URL.</param>
        /// <param name="contentType">The content type of the image e.g. "image/png". If null, then content type is determined from the file name extension.</param>
        /// <returns>A <see cref="LinkedResource"/> representing the embedded image.</returns>
        public LinkedResource ReferenceImage(string imagePathOrUrl, string contentType = null)
        {
            LinkedResource resource;
            if (images.TryGetValue(imagePathOrUrl, out resource)) return resource;

            resource = createLinkedResource(imagePathOrUrl);

            contentType = contentType ?? DetermineContentType(imagePathOrUrl);
            if (contentType != null)
            {
                resource.ContentType = new ContentType(contentType);
            }

            images[imagePathOrUrl] = resource;
            return resource;
        }

        string DetermineContentType(string pathOrUrl)
        {
            if (pathOrUrl == null) throw new ArgumentNullException("pathOrUrl");

            var extension = Path.GetExtension(pathOrUrl).ToLowerInvariant();
            switch (extension)
            {
                case ".png":
                    return "image/png";
                case ".jpeg":
                case ".jpg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Adds recorded <see cref="LinkedResource"/> image references to the given <see cref="AlternateView"/>.
        /// </summary>
        public void AddImagesToView(AlternateView view)
        {
            foreach (var image in images)
            {
                view.LinkedResources.Add(image.Value);
            }
        }

        /// <summary>
        /// Replaces all occurrences of the linked resources <see cref="LinkedResource"/> in the content
        /// </summary>
        public string ReplaceImageData(AlternateView view, string content)
        {
            var resources = view.LinkedResources;

            if (!resources.Any())
                return content;

            foreach (var resource in resources)
            {
                var regex = new Regex("src=\"cid:" + resource.ContentId + "\"");

                string imageData = ComposeImageData(resource);

                content = regex.Replace(content, "src=\"" + imageData + "\"");
            }

            return content;
        }

        string ComposeImageData(LinkedResource resource)
        {
            string contentType = resource.ContentType.MediaType;
            byte[] bytes = ReadFully(resource.ContentStream);

            return string.Format("data:{0};base64,{1}",
                contentType,
                Convert.ToBase64String(bytes));
        }

        static byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
