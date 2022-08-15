using System;
using Xunit;
using Shouldly;
using System.Net.Mail;
using System.Net.Mime;
using System.IO;
using System.Threading.Tasks;

namespace Postal
{
    public class ImageEmbedderTests
    {
        Task<LinkedResource> StubLinkedResource(string s) 
        { 
            return Task.FromResult(new LinkedResource(new MemoryStream())); 
        }

        [Fact]
        public void ReferenceImage_returns_LinkedResource()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var resource = embedder.ReferenceImageAsync("test.png");
            resource.ShouldNotBeNull();
        }

        [Fact]
        public async Task Repeated_images_use_the_same_LinkedResource()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var r1 = await embedder.ReferenceImageAsync("test-a.png");
            var r2 = await embedder.ReferenceImageAsync("test-a.png");
            Assert.Same(r1, r2);
        }

        [Fact]
        public async Task Determine_content_type_from_PNG_file_extension()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var resource = await embedder.ReferenceImageAsync("test.png");
            resource.ContentType.ShouldBe(new ContentType("image/png"));
        }

        [Fact]
        public async Task Determine_content_type_from_PNG_http_file_extension()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var resource = await embedder.ReferenceImageAsync("http://test.com/test.png");
            resource.ContentType.ShouldBe(new ContentType("image/png"));
        }

        [Fact]
        public async Task Determine_content_type_from_JPEG_file_extension()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var resource = await embedder.ReferenceImageAsync("test.jpeg");
            resource.ContentType.ShouldBe(new ContentType("image/jpeg"));
        }

        [Fact]
        public async Task Determine_content_type_from_JPG_file_extension()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var resource = await embedder.ReferenceImageAsync("test.jpg");
            resource.ContentType.ShouldBe(new ContentType("image/jpeg"));
        }

        [Fact]
        public async Task Determine_content_type_from_GIF_file_extension()
        {
            var embedder = new ImageEmbedder(StubLinkedResource);
            var resource = await embedder.ReferenceImageAsync("test.gif");
            resource.ContentType.ShouldBe(new ContentType("image/gif"));
        }

        [Fact]
        public async Task Can_read_image_from_file_system()
        {
            var embedder = new ImageEmbedder();
            var filename = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(filename, new byte[] { 42 });
                using (var resource = await embedder.ReferenceImageAsync(filename))
                {
                    resource.ContentStream.Length.ShouldBe(1);
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [Fact]
        public async Task Can_read_image_from_http_url()
        {
            var embedder = new ImageEmbedder();
            using (var resource = await embedder.ReferenceImageAsync("http://upload.wikimedia.org/wikipedia/commons/6/63/Wikipedia-logo.png"))
            {
                resource.ContentStream.Length.ShouldNotBe(0);
            }
        }

        [Fact]
        public async Task AddImagesToView_adds_linked_resources()
        {
            var embedder = new ImageEmbedder(s => Task.FromResult(new LinkedResource(new MemoryStream())));
            var cid = await embedder.ReferenceImageAsync("test.png");
            using (var view = AlternateView.CreateAlternateViewFromString("<img src=\"cid:" + cid.ContentId + "\" />", new ContentType("text/html")))
            {
                embedder.AddImagesToView(view);

                view.LinkedResources.Count.ShouldBe(1);
                view.LinkedResources[0].ShouldBeSameAs(cid);
            }
        }
    }
}
