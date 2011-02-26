using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.IO;
using System.Net.Mime;
using System.Web;
using System.Net;

namespace Postal
{
    public class ImageEmbedder
    {
        public ImageEmbedder()
        {
            this.createLinkedResource = CreateLinkedResource;
        }

        public ImageEmbedder(Func<string, LinkedResource> createLinkedResource)
        {
            this.createLinkedResource = createLinkedResource;
        }

        readonly Func<string, LinkedResource> createLinkedResource;
        readonly Dictionary<string, LinkedResource> images = new Dictionary<string, LinkedResource>();

        public static LinkedResource CreateLinkedResource(string pathOrUrl)
        {
            if (Uri.IsWellFormedUriString(pathOrUrl, UriKind.Absolute))
            {
                var client = new WebClient();
                var bytes = client.DownloadData(pathOrUrl);
                return new LinkedResource(new MemoryStream(bytes));
            }
            else
            {
                return new LinkedResource(File.OpenRead(pathOrUrl));
            }
        }

        public LinkedResource AddImage(string pathOrUrl, string contentType = null)
        {
            LinkedResource resource;
            if (images.TryGetValue(pathOrUrl, out resource))
            {
                return resource;
            }
            else
            {
                resource = createLinkedResource(pathOrUrl);

                if (contentType == null)
                {
                    contentType = DetermineContentType(pathOrUrl);
                    if (contentType != null)
                    {
                        resource.ContentType = new ContentType(contentType);
                    }
                }

                images[pathOrUrl] = resource;
                return resource;
            }
        }

        string DetermineContentType(string pathOrUrl)
        {
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

        public void PutImagesIntoView(AlternateView view)
        {
            foreach (var image in images)
            {
                view.LinkedResources.Add(image.Value);
            }
        }
    }
}
