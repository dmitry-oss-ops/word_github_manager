using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;

namespace word_doc_mvp.Services
{
    public static class DocxReconstructorService
    {
        /// <summary>
        /// Converts a Flat OPC XML string back into a .docx file.
        /// Uses the Open XML SDK's FromFlatOpcString() to reconstruct
        /// the full OPC package, then clones it to the output path.
        /// </summary>
        public static void ReconstructDocx(string flatOpcXml, string outputPath, Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(flatOpcXml))
                throw new ArgumentException("Flat OPC XML content is empty.");

            log?.Invoke("Parsing Flat OPC XML into WordprocessingDocument...");

            using (var memDoc = WordprocessingDocument.FromFlatOpcString(flatOpcXml))
            {
                log?.Invoke("Cloning package to output file...");

                using (var fileStream = File.Create(outputPath))
                {
                    memDoc.Clone(fileStream);
                }
            }

            var fileInfo = new FileInfo(outputPath);
            log?.Invoke($"DOCX saved: {fileInfo.Length:N0} bytes -> {outputPath}");
        }
    }
}
