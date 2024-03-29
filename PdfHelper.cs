// --------------------------------------------------------------------------------------------------------------------
// File Name: PdfHelper.cs
// Last Updated: 2022-03-09 @ 12:40 PM
// --------------------------------------------------------------------------------------------------------------------

namespace App.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text;
    using iText.Forms;
    using iText.IO.Image;
    using iText.Kernel.Colors;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas;
    using iText.Kernel.Pdf.Canvas.Parser;
    using iText.Kernel.Pdf.Canvas.Parser.Listener;
    using iText.Kernel.Pdf.Extgstate;
    using iText.Kernel.Utils;
    using iText.Layout;
    using iText.Layout.Element;
    using iText.Layout.Properties;
    using Image = iText.Layout.Element.Image;

    public class PdfHelper
    {
        #region Constants

        private const float DefaultMargin = 20;

        private const int MaxUpperCaseTextLength = 50;

        #endregion
        #region Public Methods and Operators

        public static int GetPageCount(byte[] sourceContent)
        {
            CheckByteContent(sourceContent, "sourceContent");

            var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
            var pdfDocument = new PdfDocument(pdfReader);
            return pdfDocument.GetNumberOfPages();
        }


        public static byte[] AddDiagonalTextStamp(byte[] sourceContent, string stampText, bool eachPage = false)
        {
            CheckByteContent(sourceContent, "sourceContent");

            if (string.IsNullOrWhiteSpace(stampText))
            {
                return sourceContent;
            }

            byte[] content;

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);
                var document = new Document(pdfDocument);

                for (var page = 1; page <= pdfDocument.GetNumberOfPages() && (page <= 1 || eachPage); page++)
                {
                    var pdfPage = pdfDocument.GetPage(page);
                    var pdfPageSize = pdfPage.GetPageSize();

                    // Calculate text rotation angle.
                    var angle = Math.Atan2(pdfPageSize.GetHeight(), pdfPageSize.GetWidth()) * (180 / Math.PI);
                    var radAngle = (float)Math.PI / 180 * (int)angle;
                    var pdfCanvas = new PdfCanvas(pdfPage);
                    var paragraph = new Paragraph(stampText).SetFontSize(60);
                    pdfCanvas.SaveState();
                    var pdfExtGState = new PdfExtGState().SetFillOpacity(0.2f);
                    pdfCanvas.SetExtGState(pdfExtGState);
                    document.ShowTextAligned(paragraph, pdfPageSize.GetWidth() / 2, pdfPageSize.GetHeight() / 2, page, TextAlignment.CENTER, VerticalAlignment.MIDDLE, radAngle);
                    pdfCanvas.RestoreState();
                }

                pdfDocument.Close();
                content = ms.ToArray();
            }

            return content;
        }

        public static byte[] AddImageStamp(byte[] sourceContent, byte[] stampContent, bool eachPage = false)
        {
            CheckByteContent(sourceContent, "sourceContent");
            CheckByteContent(stampContent, "stampContent");
            byte[] content;

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);
                var document = new Document(pdfDocument);

                for (var page = 1; page <= pdfDocument.GetNumberOfPages() && (page <= 1 || eachPage); page++)
                {
                    var pdfPage = pdfDocument.GetPage(page);
                    var pdfPageSize = pdfPage.GetPageSize();
                    var image = new Image(ImageDataFactory.Create(stampContent)).SetOpacity(0.4f);
                    image.SetFixedPosition(page, pdfPageSize.GetLeft(), pdfPageSize.GetHeight() - image.GetImageHeight() - DefaultMargin);
                    document.Add(image);
                }

                pdfDocument.Close();
                content = ms.ToArray();
            }

            return content;
        }

        public static byte[] AddTextStamp(byte[] sourceContent, string stampText, bool eachPage = false)
        {
            CheckByteContent(sourceContent, "sourceContent");

            if (string.IsNullOrWhiteSpace(stampText))
            {
                return sourceContent;
            }

            byte[] content;

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);
                var document = new Document(pdfDocument);

                for (var page = 1; page <= pdfDocument.GetNumberOfPages() && (page <= 1 || eachPage); page++)
                {
                    var pdfPage = pdfDocument.GetPage(page);
                    var pdfPageSize = pdfPage.GetPageSize();
                    var paragraph = new Paragraph(stampText.Substring(0, stampText.Length > MaxUpperCaseTextLength ? MaxUpperCaseTextLength : stampText.Length)).SetFontSize(12).SetFontColor(ColorConstants.RED).SetBold().SetTextAlignment(TextAlignment.LEFT).SetOpacity(0.4f);
                    paragraph.SetFixedPosition(page, pdfPageSize.GetLeft() + DefaultMargin, pdfPageSize.GetHeight() - 20, pdfPageSize.GetWidth());
                    document.Add(paragraph);
                }

                pdfDocument.Close();
                content = ms.ToArray();
            }

            return content;
        }

        public static byte[] AppendPdf(byte[] sourceContent, string appendFile)
        {
            CheckByteContent(sourceContent, "sourceContent");
            CheckFilePath(appendFile);

            // Read file into byte[]
            var appendContent = OpenPdf(appendFile);
            return AppendPdf(sourceContent, appendContent);
        }

        public static byte[] AppendPdf(byte[] sourceContent, byte[] appendContent)
        {
            CheckByteContent(sourceContent, "sourceContent");
            CheckByteContent(appendContent, "appendContent");
            byte[] content;

            using (var memoryStream = new MemoryStream())
            {
                // Read source content into PDF document.
                var sourcePdfDocument = new PdfDocument(new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true));
                // Read append content into PDF document.
                var appendPdfDocument = new PdfDocument(new PdfReader(new MemoryStream(appendContent)).SetUnethicalReading(true));
                // Create new PDF document.
                var pdfDocument = new PdfDocument(new PdfWriter(memoryStream, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7).UseSmartMode()));
                // Merge source and append PDF documents into this newly create PDF document.
                var pdfMerger = new PdfMerger(pdfDocument);
                // Add source PDF pages.
                pdfMerger.Merge(sourcePdfDocument, 1, sourcePdfDocument.GetNumberOfPages());
                // Add append PDF pages.
                pdfMerger.Merge(appendPdfDocument, 1, appendPdfDocument.GetNumberOfPages());
                // Close all PDF documents.
                sourcePdfDocument.Close();
                appendPdfDocument.Close();
                pdfDocument.Close();
                content = memoryStream.ToArray();
            }

            return content;
        }

        public static byte[] ExtractPages(byte[] sourceContent, int startPage, int endPage)
        {
            CheckByteContent(sourceContent, "sourceContent");

            var pageList = new List<int>();
            for (var i = endPage; i > startPage; i--)
            {
                pageList.Add(i);
            }
            var pages = pageList.ToArray();
            return ExtractPages(sourceContent, pages);
        }

        public static List<byte[]> ExtractPagesIntoPdf(byte[] sourceContent, int[] pages)
        {
            CheckByteContent(sourceContent, "sourceContent");

            var collection = new List<byte[]>();

            var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
            var pdfSourceDocument = new PdfDocument(pdfReader);

            for (var page = 1; page <= pdfSourceDocument.GetNumberOfPages(); page++)
            {
                if (!pages.Contains(page))
                {
                    continue;
                }

                using (var ms = new MemoryStream())
                {
                    var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                    var pdfTargetDocument = new PdfDocument(pdfWriter);

                    pdfSourceDocument.CopyPagesTo(page, page, pdfTargetDocument);

                    pdfTargetDocument.Close();
                    pdfWriter.Close();

                    collection.Add(ms.ToArray());
                }

            }

            return collection;
        }


        public static byte[] ExtractPages(byte[] sourceContent, int[] pages)
        {
            CheckByteContent(sourceContent, "sourceContent");
            byte[] content;

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfSourceDocument = new PdfDocument(pdfReader);
                var pdfTargetDocument = new PdfDocument(pdfWriter);

                for (var page = 1; page <= pdfSourceDocument.GetNumberOfPages(); page++)
                {
                    if (pages.Contains(page))
                    {
                        pdfSourceDocument.CopyPagesTo(page, page, pdfTargetDocument);
                    }
                }

                pdfSourceDocument.Close();
                pdfReader.Close();
                pdfTargetDocument.Close();
                pdfWriter.Close();

                content = ms.ToArray();
            }

            return content;
        }

        public static List<MergeTag> FindMergeTags(byte[] sourceContent, string startTag, string endTag)
        {
            CheckByteContent(sourceContent, "sourceContent");
            var collection = new Dictionary<int, string>();

            var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
            var pdfDocument = new PdfDocument(pdfReader);

            for (var page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
            {
                var tag = string.Empty;
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();

                var currentPageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);

                // Only parse the text when both start and end tag are found.
                if (currentPageText.Contains(startTag) && currentPageText.Contains(endTag))
                {
                    var startTagIndex = currentPageText.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
                    var endTagIndex = currentPageText.IndexOf(endTag, startTagIndex, StringComparison.OrdinalIgnoreCase);

                    // End tag must come after start tag
                    if (startTagIndex + startTag.Length < endTagIndex)
                    {
                        tag = currentPageText.Substring(startTagIndex + startTag.Length, endTagIndex - startTag.Length).Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        collection.Add(page, tag);
                    }
                }
            }

            pdfDocument.Close();
            pdfReader.Close();

            // Group Tags.
            var mergeTags = new List<MergeTag>();
            var distinctTags = collection.Select(x => x.Value).Distinct().OrderBy(x => x);
            foreach (var distinctTag in distinctTags)
            {
                var selectedTag = collection.Where(x => x.Value == distinctTag);

                foreach (var tag in selectedTag.OrderBy(x => x.Key))
                {
                    var mergeTag = mergeTags.FirstOrDefault(t => t.Tag == distinctTag);

                    if (mergeTag == null)
                    {
                        mergeTag = new MergeTag { Tag = distinctTag };
                        mergeTag.OnPages.Add(tag.Key);
                        mergeTags.Add(mergeTag);
                    }
                    else
                    {
                        mergeTag.OnPages.Add(tag.Key);
                    }
                }
            }

            return mergeTags;
        }

        public static byte[] FlattenFormPdf(byte[] sourceContent)
        {
            CheckByteContent(sourceContent, "sourceContent");
            byte[] content;

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);
                var pdfForm = PdfAcroForm.GetAcroForm(pdfDocument, true);
                pdfForm.FlattenFields();
                pdfDocument.Close();
                content = ms.ToArray();
            }

            return content;
        }

        public static byte[] LoadImage(string path)
        {
            CheckFilePath(path);

            if (!path.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase) || !path.EndsWith(".bmp", StringComparison.CurrentCultureIgnoreCase) || !path.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase) || !path.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)
                || !path.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException(nameof(path), $@"Invalid image file specified.");
            }

            try
            {
                byte[] content;

                using (var stampImage = new Bitmap(path))
                {
                    using (var ms = new MemoryStream())
                    {
                        if (path.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase)) { stampImage.Save(ms, ImageFormat.Gif); }
                        if (path.EndsWith(".bmp", StringComparison.CurrentCultureIgnoreCase)) { stampImage.Save(ms, ImageFormat.Bmp); }
                        if (path.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase)) { stampImage.Save(ms, ImageFormat.Bmp); }
                        if (path.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)) { stampImage.Save(ms, ImageFormat.Jpeg); }
                        if (path.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase)) { stampImage.Save(ms, ImageFormat.Jpeg); }
                        content = ms.ToArray();
                    }
                }

                return content;
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }
        }

        public static byte[] OpenPdf(string path)
        {
            CheckFilePath(path);

            try
            {
                byte[] content;

                using (var ms = new MemoryStream())
                {
                    var pdfReader = new PdfReader(path).SetUnethicalReading(true);
                    var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                    var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                    using (var document = new Document(pdfDocument)) { }

                    content = ms.ToArray();
                }

                return content;
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }
        }

        public static string PdfToText(byte[] sourceContent)
        {
            CheckByteContent(sourceContent, "sourceContent");
            var pdfText = new StringBuilder();

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                for (var page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                {
                    ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                    pageText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(pageText)));
                    pdfText.Append(pageText);
                }

                pdfDocument.Close();
                pdfReader.Close();
            }

            return pdfText.ToString();
        }

        public static byte[] SetFormField(byte[] sourceContent, string fieldName, string fieldValue)
        {
            CheckByteContent(sourceContent, "sourceContent");
            byte[] content;

            using (var ms = new MemoryStream())
            {
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);
                var pdfForm = PdfAcroForm.GetAcroForm(pdfDocument, true);
                var pdfFormFields = pdfForm.GetFormFields();
                pdfFormFields.TryGetValue(fieldName, out var pdfFormField);
                pdfFormField?.SetValue(fieldValue);
                pdfDocument.Close();
                content = ms.ToArray();
            }

            return content;
        }

        public static List<byte[]> SplitPdf(byte[] sourceContent, int pagesPerDocument = 1)
        {
            CheckByteContent(sourceContent, "sourceContent");

            var sections = new List<byte[]>();

            var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
            var pdfSourceDocument = new PdfDocument(pdfReader);
            var pageCount = pdfSourceDocument.GetNumberOfPages();

            var ms = new MemoryStream();
            var pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
            var pdfDocument = new PdfDocument(pdfWriter);


            var sectionPage = 0;
            for (var page = 1; page <= pageCount; page++)
            {
                ++sectionPage;

                switch (page % pagesPerDocument)
                {
                    case 0:
                        {
                            // First and Last page for this section.
                            if (pagesPerDocument == 1)
                            {
                                ms = new MemoryStream();
                                pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                                pdfDocument = new PdfDocument(pdfWriter);
                            }

                            // Last page for this section.
                            pdfSourceDocument.CopyPagesTo(page, page, pdfDocument);

                            pdfDocument.Close();
                            pdfWriter.Close();
                            sections.Add(ms.ToArray());
                            ms.Close();
                            sectionPage = 0;
                            break;
                        }

                    case 1:
                        {
                            // First page for this section.
                            ms = new MemoryStream();
                            pdfWriter = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                            pdfDocument = new PdfDocument(pdfWriter);

                            pdfSourceDocument.CopyPagesTo(page, page, pdfDocument);

                            break;
                        }
                    default:
                        {
                            // Pages to include in this section.
                            pdfSourceDocument.CopyPagesTo(page, page, pdfDocument);

                            break;
                        }
                }
            }

            return sections;
        }

        public static byte[] WritePdf(byte[] sourceContent)
        {
            CheckByteContent(sourceContent, "sourceContent");

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var pdfWriter = new PdfWriter(memoryStream, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                    var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                    var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                    using (var document = new Document(pdfDocument)) { }

                    pdfDocument.Close();

                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }
        }

        public static bool WritePdf(List<byte[]> sourceContents, string path)
        {
            // Check parameters.
            CheckFilePath(path);

            try
            {
                var appendPdf = WritePdf(sourceContents[0]);
                if (sourceContents.Count > 1)
                {
                    for (var pdf = 1; pdf < sourceContents.Count; pdf++)
                    {
                        appendPdf = AppendPdf(appendPdf, sourceContents[pdf]);
                    }
                }

                return WritePdf(appendPdf, path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }
        }

        public static bool WritePdf(byte[] sourceContent, string path)
        {
            CheckByteContent(sourceContent, "sourceContent");

            // Check parameters.
            CheckFilePath(path);

            try
            {
                var pdfWriter = new PdfWriter(path, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                using (var document = new Document(pdfDocument)) { }

                pdfDocument.Close();
                return File.Exists(path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }
        }

        public static byte[] ProtectPdf(byte[] sourceContent, string password = "")
        {
            CheckByteContent(sourceContent, "sourceContent");

            try
            {
                byte[] ownerPassword = null;
                if (string.IsNullOrEmpty(password))
                {
                    ownerPassword = Guid.NewGuid().ToByteArray();
                }
                else
                {
                    ownerPassword = Encoding.ASCII.GetBytes(password);
                }

                using (var memoryStream = new MemoryStream())
                {
                    var pdfWriter = new PdfWriter(memoryStream, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7).SetStandardEncryption(null, ownerPassword, EncryptionConstants.ALLOW_PRINTING, EncryptionConstants.ENCRYPTION_AES_256 | EncryptionConstants.DO_NOT_ENCRYPT_METADATA));
                    var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                    var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                    using (var document = new Document(pdfDocument)) { }

                    pdfDocument.Close();

                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }

        }

        public static bool WriteProtectPdf(byte[] sourceContent, string path)
        {
            CheckByteContent(sourceContent, "sourceContent");

            // Check parameters.
            CheckFilePath(path);

            try
            {
                var ownerPassword = Guid.NewGuid();
                var pdfWriter = new PdfWriter(path, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7).SetStandardEncryption(null, ownerPassword.ToByteArray(), EncryptionConstants.ALLOW_PRINTING, EncryptionConstants.ENCRYPTION_AES_256 | EncryptionConstants.DO_NOT_ENCRYPT_METADATA));
                var pdfReader = new PdfReader(new MemoryStream(sourceContent)).SetUnethicalReading(true);
                var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                using (var document = new Document(pdfDocument)) { }

                pdfDocument.Close();
                return File.Exists(path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred in {ex.TargetSite.Name}.", ex);
            }
        }

        #endregion
        #region Methods

        private static void CheckByteContent(byte[] content, string contentText = "content")
        {
            if (content == null || content.Length < 1)
            {
                throw new ArgumentNullException(nameof(content), $@"The {contentText} was not specified.");
            }
        }

        private static void CheckFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "The file path was not specified.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                throw new ArgumentException("The specified file folder does not exists.", filePath);
            }

            // Check whether the file has PDF extension.
            if (!Path.GetExtension(Path.GetFileName(filePath)).Equals(".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException("The specified file does not appear to be a PDF file.", filePath);
            }
        }

        #endregion

        private struct Position
        {
            #region Fields

            public float X;

            public float Y;

            #endregion
            #region Constructors and Destructors

            public Position(float x, float y)
            {
                X = x;
                Y = y;
            }

            #endregion
        }

        private struct Size
        {
            #region Fields

            public float Height;

            public float Width;

            #endregion
            #region Constructors and Destructors

            public Size(float width, float height)
            {
                Width = width;
                Height = height;
            }

            #endregion
        }

        public sealed class MergeTag
        {
            public string Tag { get; set; } = string.Empty;
            public List<int> OnPages { get; set; } = new List<int>();
        }
    }
}
