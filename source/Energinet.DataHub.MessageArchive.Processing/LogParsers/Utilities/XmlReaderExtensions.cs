using System.Xml;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers.Utilities
{
    internal static class XmlReaderExtensions
    {
        public static bool IsNodeType(this XmlReader xmlReader, XmlNodeType nodeType)
        {
            return xmlReader.NodeType == nodeType;
        }
    }
}
