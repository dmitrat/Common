using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using OutWit.Common.Collections;

namespace OutWit.Common.Controls.HighlightTextBox.Highlighters
{
    public class HighlighterManager : IEnumerable<Highlighter>
    {
        #region Constants

        private const string NAME_ELEMENT = "name";

        private const string SCHEMA_RESOURCE_URI = "/OutWit.Common.Controls.HighlightTextBox;component/Properties/Syntax.xsd";

        #endregion

        #region Static Fields

        private static volatile HighlighterManager m_instance = null;
        private static readonly object m_syncRoot = new Object();

        #endregion

        #region Events

        public event HighlighterManagerEventHandler HighlighterAdded = delegate { };

        #endregion

        #region Constructors

        private HighlighterManager()
        {
            InitDefaults();
            InitSchema();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Highlighters = new Dictionary<string, Highlighter>();
        }

        private void InitSchema()
        {
            var resourceUri = new Uri(SCHEMA_RESOURCE_URI, UriKind.RelativeOrAbsolute);
            var streamInfo = Application.GetResourceStream(resourceUri);

            if (streamInfo == null)
                return;

            XmlSchema = XmlSchema.Read(streamInfo.Stream, null);
            if (XmlSchema == null)
                return;

            XmlSettings = new XmlReaderSettings();
            XmlSettings.Schemas.Add(XmlSchema);
            XmlSettings.ValidationType = ValidationType.Schema;
        }

        #endregion

        #region Functions

        public bool Add(string name, Highlighter highlighter)
        {
            if (Highlighters.ContainsKey(name))
                Highlighters[name] = highlighter;
            else
                Highlighters.Add(name, highlighter);
            
            HighlighterAdded(highlighter);

            return true;
        }

        public bool LoadSyntaxXml(string xmlContent)
        {
            using var stream = new StringReader(xmlContent);
            using var reader = new XmlTextReader(stream);

            return LoadSyntaxXml(reader);
        }

        public bool LoadSyntaxXml(Stream xmlContent)
        {
            using var reader = XmlReader.Create(xmlContent);
            
            return LoadSyntaxXml(reader);
        }

        public bool LoadSyntaxXml(XmlReader reader)
        {
            try
            {
                var document = XDocument.Load(reader);

                var root = document.Root;
                if (root == null)
                    return false;

                var name = root.Attribute(NAME_ELEMENT)?.Value.Trim();
                if (string.IsNullOrEmpty(name))
                    return false;

                var highlighter = new HighlighterXml(root);

                return Add(name, highlighter);

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public Highlighter Find(string key)
        {
            return Highlighters.TryGetValue(key);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<Highlighter> GetEnumerator()
        {
            return Highlighters.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 

        #endregion

        #region Properties

        private XmlSchema XmlSchema { get; set; }

        private XmlReaderSettings XmlSettings { get; set; }

        private Dictionary<string, Highlighter> Highlighters { get; set; }

        #endregion

        #region Static Properties

        public static HighlighterManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (m_syncRoot)
                    {
                        if (m_instance == null)
                            m_instance = new HighlighterManager();
                    }
                }

                return m_instance;
            }
        }

        #endregion


    }

    public delegate void HighlighterManagerEventHandler(Highlighter highlighter);
}
