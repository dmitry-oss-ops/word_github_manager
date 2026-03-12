using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace word_doc_mvp.Services
{
    public static class XmlCanonicalizerService
    {
        /// <summary>
        /// Applies W3C Canonical XML (C14N) to the input XML string.
        /// Forces deterministic attribute ordering, uniform whitespace,
        /// and standardized empty element representation.
        /// </summary>
        public static string Canonicalize(string xmlContent)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(xmlContent);

            var transform = new XmlDsigC14NTransform(false);
            transform.LoadInput(xmlDoc);

            using (var outputStream = (Stream)transform.GetOutput(typeof(Stream)))
            using (var reader = new StreamReader(outputStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
