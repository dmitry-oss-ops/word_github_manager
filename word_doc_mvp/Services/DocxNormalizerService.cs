using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace word_doc_mvp.Services
{
    public static class DocxNormalizerService
    {
        private static readonly XNamespace W =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace W14 =
            "http://schemas.microsoft.com/office/word/2010/wordml";
        private static readonly XNamespace W15 =
            "http://schemas.microsoft.com/office/word/2012/wordml";
        private static readonly XNamespace WP14 =
            "http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing";
        private static readonly XNamespace R =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace O =
            "urn:schemas-microsoft-com:office:office";

        private static readonly HashSet<string> RsidAttributeNames = new HashSet<string>
        {
            "rsidR", "rsidRPr", "rsidRDefault", "rsidP", "rsidDel",
            "rsidTr", "rsidSect", "rsidRoot", "rsidRPr", "rsidR"
        };

        /// <summary>
        /// Full normalization pipeline: loads a DOCX, strips all non-deterministic
        /// metadata, merges fragmented runs, re-indexes relationships, converts to
        /// Flat OPC, and applies C14N canonicalization.
        /// </summary>
        public static string NormalizeDocx(string inputFilePath, Action<string> log = null)
        {
            log?.Invoke("Reading DOCX file into memory...");
            byte[] fileBytes = File.ReadAllBytes(inputFilePath);

            using (var stream = new MemoryStream())
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
                stream.Position = 0;

                using (var doc = WordprocessingDocument.Open(stream, true))
                {
                    NormalizeAllParts(doc, log);

                    log?.Invoke("Converting to Flat OPC...");
                    string flatOpc = doc.ToFlatOpcString();

                    log?.Invoke("Re-indexing relationship IDs...");
                    flatOpc = ReindexRelationshipsInFlatOpc(flatOpc);

                    log?.Invoke("Applying XML Canonicalization (C14N)...");
                    string canonical = XmlCanonicalizerService.Canonicalize(flatOpc);

                    log?.Invoke("Normalization complete.");
                    return canonical;
                }
            }
        }

        private static void NormalizeAllParts(WordprocessingDocument doc, Action<string> log)
        {
            var mainPart = doc.MainDocumentPart;
            if (mainPart == null)
                throw new InvalidOperationException("Document has no MainDocumentPart.");

            log?.Invoke("Wiping volatile package properties...");
            doc.PackageProperties.Revision = null;
            doc.PackageProperties.Modified = null;
            doc.PackageProperties.LastModifiedBy = null;
            doc.PackageProperties.Created = null;
            doc.PackageProperties.Creator = null;

            // --- Process main document body ---
            log?.Invoke("Normalizing main document body...");
            NormalizeXmlPart(mainPart);

            // --- Process headers ---
            foreach (var headerPart in mainPart.HeaderParts)
            {
                log?.Invoke("Normalizing header part...");
                NormalizeXmlPart(headerPart);
            }

            // --- Process footers ---
            foreach (var footerPart in mainPart.FooterParts)
            {
                log?.Invoke("Normalizing footer part...");
                NormalizeXmlPart(footerPart);
            }

            // --- Process endnotes ---
            if (mainPart.EndnotesPart != null)
            {
                log?.Invoke("Normalizing endnotes part...");
                NormalizeXmlPart(mainPart.EndnotesPart);
            }

            // --- Process footnotes ---
            if (mainPart.FootnotesPart != null)
            {
                log?.Invoke("Normalizing footnotes part...");
                NormalizeXmlPart(mainPart.FootnotesPart);
            }

            // --- Process style definitions part (also contains RSIDs) ---
            if (mainPart.StyleDefinitionsPart != null)
            {
                log?.Invoke("Normalizing styles part...");
                var stylesXDoc = LoadXDocument(mainPart.StyleDefinitionsPart);
                StripRsidAttributes(stylesXDoc);
                NullifyW14Identifiers(stylesXDoc);
                SaveXDocument(mainPart.StyleDefinitionsPart, stylesXDoc);
            }

            // --- Process numbering definitions part ---
            if (mainPart.NumberingDefinitionsPart != null)
            {
                log?.Invoke("Normalizing numbering part...");
                var numXDoc = LoadXDocument(mainPart.NumberingDefinitionsPart);
                StripRsidAttributes(numXDoc);
                NullifyW14Identifiers(numXDoc);
                SaveXDocument(mainPart.NumberingDefinitionsPart, numXDoc);
            }

            // --- Process settings part (contains rsids element) ---
            if (mainPart.DocumentSettingsPart != null)
            {
                log?.Invoke("Normalizing settings part...");
                var settingsXDoc = LoadXDocument(mainPart.DocumentSettingsPart);
                StripRsidAttributes(settingsXDoc);
                RemoveElements(settingsXDoc, W + "rsids");
                NullifyW14Identifiers(settingsXDoc);
                SaveXDocument(mainPart.DocumentSettingsPart, settingsXDoc);
            }

            // --- Remove comment part entirely ---
            if (mainPart.WordprocessingCommentsPart != null)
            {
                log?.Invoke("Removing comments part...");
                mainPart.DeletePart(mainPart.WordprocessingCommentsPart);
            }
        }

        #region RSID Removal

        private static void StripRsidAttributes(XDocument xdoc)
        {
            foreach (var element in xdoc.Descendants().ToList())
            {
                var attrsToRemove = element.Attributes()
                    .Where(a => RsidAttributeNames.Contains(a.Name.LocalName)
                             && (a.Name.Namespace == W
                                 || a.Name.Namespace == XNamespace.None))
                    .ToList();

                foreach (var attr in attrsToRemove)
                    attr.Remove();
            }
        }

        #endregion

        #region Element Removal and Unwrapping

        private static void RemoveElements(XDocument xdoc, XName elementName)
        {
            xdoc.Descendants(elementName).ToList().ForEach(e => e.Remove());
        }

        /// <summary>
        /// Replaces wrapper elements (like smartTag) with their children,
        /// effectively removing the wrapper while keeping inner content.
        /// </summary>
        private static void UnwrapElements(XDocument xdoc, XName elementName)
        {
            foreach (var el in xdoc.Descendants(elementName).ToList())
            {
                el.ReplaceWith(el.Nodes());
            }
        }

        #endregion

        #region Namespace Hoisting

        /// <summary>
        /// Moves all namespace declarations from descendant elements up to the
        /// document root. This prevents Word's habit of injecting xmlns:wp14 etc.
        /// on arbitrary child nodes from producing floating-namespace noise in diffs.
        /// </summary>
        private static void HoistNamespacesToRoot(XDocument xdoc)
        {
            var root = xdoc.Root;
            if (root == null) return;

            var allNamespaces = root.Descendants()
                .SelectMany(e => e.Attributes().Where(a => a.IsNamespaceDeclaration))
                .Select(a => new { a.Name, a.Value })
                .GroupBy(a => a.Name.ToString())
                .Select(g => g.First())
                .ToList();

            foreach (var el in root.Descendants().ToList())
            {
                el.Attributes()
                    .Where(a => a.IsNamespaceDeclaration)
                    .ToList()
                    .ForEach(a => a.Remove());
            }

            foreach (var ns in allNamespaces)
            {
                if (root.Attribute(ns.Name) == null)
                    root.SetAttributeValue(ns.Name, ns.Value);
            }
        }

        #endregion

        #region Revision Acceptance

        /// <summary>
        /// Accepts all tracked changes:
        /// - w:ins → unwrap (keep children)
        /// - w:del → remove entirely
        /// - w:rPrChange → remove (keep current formatting)
        /// - w:pPrChange → remove
        /// - w:sectPrChange → remove
        /// - w:tblPrChange → remove
        /// </summary>
        private static void AcceptRevisions(XDocument xdoc)
        {
            // Remove deletions entirely
            xdoc.Descendants(W + "del").ToList().ForEach(e => e.Remove());

            // Unwrap insertions (keep the inner content)
            foreach (var ins in xdoc.Descendants(W + "ins").ToList())
            {
                ins.ReplaceWith(ins.Nodes());
            }

            // Remove property change tracking elements
            var changeElements = new[]
            {
                W + "rPrChange",
                W + "pPrChange",
                W + "sectPrChange",
                W + "tblPrChange",
                W + "tblGridChange",
                W + "tcPrChange",
                W + "trPrChange"
            };

            foreach (var name in changeElements)
            {
                xdoc.Descendants(name).ToList().ForEach(e => e.Remove());
            }

            // Remove move-from markers (treated as deletions)
            xdoc.Descendants(W + "moveFrom").ToList().ForEach(e => e.Remove());
            xdoc.Descendants(W + "moveFromRangeStart").ToList().ForEach(e => e.Remove());
            xdoc.Descendants(W + "moveFromRangeEnd").ToList().ForEach(e => e.Remove());

            // Unwrap move-to markers (treated as insertions)
            foreach (var mt in xdoc.Descendants(W + "moveTo").ToList())
            {
                mt.ReplaceWith(mt.Nodes());
            }
            xdoc.Descendants(W + "moveToRangeStart").ToList().ForEach(e => e.Remove());
            xdoc.Descendants(W + "moveToRangeEnd").ToList().ForEach(e => e.Remove());
        }

        #endregion

        #region W14 Identifier Nullification

        private static void NullifyW14Identifiers(XDocument xdoc)
        {
            foreach (var element in xdoc.Descendants().ToList())
            {
                element.Attributes(W14 + "paraId").Remove();
                element.Attributes(W14 + "textId").Remove();
                element.Attributes(W15 + "paraId").Remove();
                element.Attributes(W15 + "textId").Remove();

                element.Attributes(WP14 + "anchorId").Remove();
                element.Attributes(WP14 + "editId").Remove();
                element.Attributes(W14 + "anchorId").Remove();
                element.Attributes(W14 + "editId").Remove();
            }
        }

        #endregion

        #region VML GfxData Stripping

        private static void StripGfxData(XDocument xdoc)
        {
            foreach (var element in xdoc.Descendants().ToList())
            {
                element.Attributes(O + "gfxdata").Remove();
            }
        }

        #endregion

        #region Part Normalization Helper

        /// <summary>
        /// Applies the full normalization pipeline to any XML-bearing document part
        /// (main body, headers, footers, endnotes, footnotes).
        /// </summary>
        private static void NormalizeXmlPart(OpenXmlPart part)
        {
            var xdoc = LoadXDocument(part);
            StripRsidAttributes(xdoc);
            RemoveElements(xdoc, W + "proofErr");
            RemoveElements(xdoc, W + "bookmarkStart");
            RemoveElements(xdoc, W + "bookmarkEnd");
            RemoveElements(xdoc, W + "commentRangeStart");
            RemoveElements(xdoc, W + "commentRangeEnd");
            RemoveElements(xdoc, W + "commentReference");
            UnwrapElements(xdoc, W + "smartTag");
            AcceptRevisions(xdoc);
            NullifyW14Identifiers(xdoc);
            StripGfxData(xdoc);
            MergeAdjacentRuns(xdoc);
            HoistNamespacesToRoot(xdoc);
            SaveXDocument(part, xdoc);
        }

        #endregion

        #region Run Consolidation

        /// <summary>
        /// Merges adjacent w:r elements that share identical formatting (w:rPr).
        /// Only merges runs containing purely text content (w:t nodes).
        /// </summary>
        private static void MergeAdjacentRuns(XDocument xdoc)
        {
            var paragraphs = xdoc.Descendants(W + "p").ToList();

            foreach (var para in paragraphs)
            {
                MergeRunsInContainer(para);
            }

            // Also handle runs inside table cells, text boxes, etc.
            var containers = xdoc.Descendants(W + "hyperlink").ToList();
            foreach (var container in containers)
            {
                MergeRunsInContainer(container);
            }
        }

        private static void MergeRunsInContainer(XElement container)
        {
            var childNodes = container.Nodes().ToList();

            for (int i = childNodes.Count - 1; i > 0; i--)
            {
                var curr = childNodes[i] as XElement;
                var prev = childNodes[i - 1] as XElement;

                if (curr == null || prev == null) continue;
                if (curr.Name != W + "r" || prev.Name != W + "r") continue;
                if (!ContainsOnlyTextContent(curr) || !ContainsOnlyTextContent(prev)) continue;

                var currProps = curr.Element(W + "rPr");
                var prevProps = prev.Element(W + "rPr");

                if (!AreRunPropertiesEqual(prevProps, currProps)) continue;

                string prevText = GetRunText(prev);
                string currText = GetRunText(curr);

                // Remove old text elements from the previous run
                prev.Elements(W + "t").Remove();

                // Create merged text element
                string merged = prevText + currText;
                var textEl = new XElement(W + "t", merged);
                if (merged.Length > 0 && (merged[0] == ' ' || merged[merged.Length - 1] == ' '))
                {
                    textEl.SetAttributeValue(XNamespace.Xml + "space", "preserve");
                }

                prev.Add(textEl);
                curr.Remove();
            }
        }

        private static bool ContainsOnlyTextContent(XElement run)
        {
            return run.Elements()
                .All(e => e.Name == W + "rPr" || e.Name == W + "t");
        }

        private static string GetRunText(XElement run)
        {
            return string.Concat(run.Elements(W + "t").Select(t => t.Value));
        }

        private static bool AreRunPropertiesEqual(XElement props1, XElement props2)
        {
            if (props1 == null && props2 == null) return true;
            if (props1 == null || props2 == null) return false;

            // Deep-compare normalized copies
            var norm1 = NormalizeForComparison(props1);
            var norm2 = NormalizeForComparison(props2);

            return XNode.DeepEquals(norm1, norm2);
        }

        private static XElement NormalizeForComparison(XElement props)
        {
            var copy = new XElement(props);

            // Strip any lingering RSID or w14 attributes from the comparison
            foreach (var el in copy.DescendantsAndSelf().ToList())
            {
                var toRemove = el.Attributes()
                    .Where(a => RsidAttributeNames.Contains(a.Name.LocalName)
                             || a.Name.Namespace == W14
                             || a.Name.Namespace == W15)
                    .ToList();
                foreach (var a in toRemove)
                    a.Remove();
            }

            return copy;
        }

        #endregion

        #region Relationship Re-indexing (Flat OPC level)

        private static readonly XNamespace Pkg =
            "http://schemas.microsoft.com/office/2006/xmlPackage";
        private static readonly XNamespace Rels =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        /// <summary>
        /// Re-indexes relationship IDs in the Flat OPC XML by operating purely
        /// on the XML tree. This avoids OPC packaging API conflicts that occur
        /// when new sequential IDs collide with existing relationship entries.
        ///
        /// Walks the main document body to determine order of first appearance,
        /// then remaps both the references and the .rels definitions.
        /// </summary>
        private static string ReindexRelationshipsInFlatOpc(string flatOpcXml)
        {
            var xdoc = XDocument.Parse(flatOpcXml);

            // Locate the main document content part
            var docPart = xdoc.Descendants(Pkg + "part")
                .FirstOrDefault(p =>
                {
                    var name = p.Attribute(Pkg + "name")?.Value ?? "";
                    return name.EndsWith("/document.xml", StringComparison.OrdinalIgnoreCase);
                });

            if (docPart == null)
                return xdoc.Declaration?.ToString() + xdoc.ToString();

            // Get the inner XML content (the w:document element tree)
            var xmlData = docPart.Element(Pkg + "xmlData");
            if (xmlData == null)
                return xdoc.Declaration?.ToString() + xdoc.ToString();

            // Collect all relationship-referencing attributes in document order
            var relAttrs = xmlData.Descendants()
                .SelectMany(e => e.Attributes())
                .Where(a => a.Name.Namespace == R)
                .ToList();

            var orderedIds = new List<string>();
            foreach (var attr in relAttrs)
            {
                if (!orderedIds.Contains(attr.Value))
                    orderedIds.Add(attr.Value);
            }

            if (orderedIds.Count == 0)
                return xdoc.Declaration?.ToString() + xdoc.ToString();

            // Locate the matching .rels part for the document
            var relsPart = xdoc.Descendants(Pkg + "part")
                .FirstOrDefault(p =>
                {
                    var name = p.Attribute(Pkg + "name")?.Value ?? "";
                    return name.EndsWith("/_rels/document.xml.rels", StringComparison.OrdinalIgnoreCase)
                        || name.EndsWith("/_rels/document.xml.rels".Replace("/", "\\"), StringComparison.OrdinalIgnoreCase);
                });

            // Gather any relationship IDs defined in .rels but not referenced in body
            // (implicit relationships like styles, numbering, fonts, etc.)
            List<XElement> relElements = null;
            if (relsPart != null)
            {
                var relsXmlData = relsPart.Element(Pkg + "xmlData");
                if (relsXmlData != null)
                {
                    relElements = relsXmlData.Descendants(Rels + "Relationship").ToList();
                    foreach (var rel in relElements)
                    {
                        string id = rel.Attribute("Id")?.Value;
                        if (id != null && !orderedIds.Contains(id))
                            orderedIds.Add(id);
                    }
                }
            }

            // Build deterministic mapping: old → rId1, rId2, ...
            var mapping = new Dictionary<string, string>();
            for (int i = 0; i < orderedIds.Count; i++)
            {
                mapping[orderedIds[i]] = "rId" + (i + 1);
            }

            // Check if mapping is already identity (skip work)
            if (mapping.All(kvp => kvp.Key == kvp.Value))
                return xdoc.Declaration?.ToString() + xdoc.ToString();

            // Apply mapping to all r:* attributes in the document body
            foreach (var attr in relAttrs)
            {
                if (mapping.TryGetValue(attr.Value, out string newId))
                    attr.Value = newId;
            }

            // Apply mapping to the Relationship/@Id in the .rels definitions
            if (relElements != null)
            {
                foreach (var rel in relElements)
                {
                    var idAttr = rel.Attribute("Id");
                    if (idAttr != null && mapping.TryGetValue(idAttr.Value, out string newId))
                        idAttr.Value = newId;
                }
            }

            // Sort ALL .rels parts' Relationship elements by Id for deterministic ordering
            foreach (var relPartEl in xdoc.Descendants(Pkg + "part")
                .Where(p => (p.Attribute(Pkg + "name")?.Value ?? "").EndsWith(".rels",
                    StringComparison.OrdinalIgnoreCase)))
            {
                var relsXml = relPartEl.Element(Pkg + "xmlData");
                if (relsXml == null) continue;

                var container = relsXml.Elements().FirstOrDefault();
                if (container == null) continue;

                var sorted = container.Elements()
                    .OrderBy(e => e.Attribute("Id")?.Value ?? "", StringComparer.Ordinal)
                    .ToList();

                container.RemoveAll();
                foreach (var el in sorted)
                    container.Add(el);
            }

            // Rebuild string preserving the XML declaration
            var decl = xdoc.Declaration;
            return (decl != null ? decl.ToString() + "\n" : "") + xdoc.ToString();
        }

        #endregion

        #region XDocument I/O Helpers

        private static XDocument LoadXDocument(OpenXmlPart part)
        {
            using (var stream = part.GetStream(FileMode.Open, FileAccess.Read))
            {
                return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            }
        }

        private static void SaveXDocument(OpenXmlPart part, XDocument xdoc)
        {
            using (var stream = part.GetStream(FileMode.Create, FileAccess.Write))
            {
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
                {
                    Encoding = System.Text.Encoding.UTF8,
                    CloseOutput = false
                }))
                {
                    xdoc.WriteTo(writer);
                }
            }
        }

        #endregion
    }
}
