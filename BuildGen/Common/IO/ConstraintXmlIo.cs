using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BuildGen.Data;
using BuildGen.Constraints;

namespace BuildGen.IO
{
    public class ConstraintXmlIo
    {
        static readonly private string SchemaName = "ConstraintSchema";
        static readonly private string SchemaPath = "Schemas/ConstraintSchema.xsd";
        
        private string ErrMessage = "";

        public bool PerformValidation { get; set; }
        public string ErrorMessage { get { return ErrMessage; } }

        public ConstraintXmlIo()
        {
            ErrMessage = "";
            PerformValidation = true;
        }

        public Dictionary<string, ConstraintSet> Read(string filepath)
        {
            try
            {
                // Initialize the reader with validation settings if requested
                bool validXml = true;
                string validationMessage = "";
                XmlReaderSettings settings = new XmlReaderSettings();

                if (!PerformValidation)
                {
                    settings.ValidationType = ValidationType.None;
                }
                else
                {
                    settings.Schemas.Add(SchemaName, SchemaPath);
                    settings.ValidationType = ValidationType.Schema;
                    settings.ValidationFlags = XmlSchemaValidationFlags.ProcessSchemaLocation | XmlSchemaValidationFlags.ReportValidationWarnings;
                    settings.ValidationEventHandler += new ValidationEventHandler(delegate(object sender, ValidationEventArgs eargs)
                    {
                        validXml = false;
                        validationMessage += eargs.Message + " Severity: " + eargs.Severity.ToString() + "\n";
                    });
                }

                var reader = XmlTextReader.Create(filepath, settings);

                while (reader.Read()) ;

                if (!validXml)
                {
                    ErrMessage = validationMessage;
                    return null;
                }
                else
                {
                    XDocument doc = XDocument.Load(filepath);

                    if (doc.Root.Name.LocalName == "constraints")
                    {
                        ErrMessage = "";
                        return ParseConstraintSets(doc.Root);
                    }
                    else
                    {
                        ErrMessage = "Document root is invalid (expected <constraints> got <" + doc.Root.Name.LocalName + ">)";
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private Dictionary<string, ConstraintSet> ParseConstraintSets(XElement element)
        {
            Dictionary<string, ConstraintSet> ret = new Dictionary<string, ConstraintSet>();

            foreach (var childElement in element.Elements())
            {
                System.Console.WriteLine(childElement.Name.LocalName);
                if (childElement.Name.LocalName == "set")
                {
                    ConstraintSet set = ParseConstraintSet(childElement);

                    XAttribute nameAttribute = childElement.Attribute("name");
                    string setName = "";

                    if (nameAttribute == null)
                        setName = "main";
                    else
                        setName = nameAttribute.Value;

                    if (set != null)
                    {
                        ret[setName] = set;
                    }
                }
            }

            return ret;
        }

        private ConstraintSet ParseConstraintSet(XElement element)
        {
            ConstraintSet ret = new ConstraintSet();

            foreach (var childElement in element.Elements())
            {
                if (childElement.Name.LocalName == "floorconstraint")
                {
                    foreach (var floorConstraintElement in childElement.Elements())
                    {
                        if (floorConstraintElement.Name.LocalName == "zone")
                        {
                            if (!ParseZoneDefinition(floorConstraintElement, ref ret))
                                return null;
                        }
                    }
                }
            }

            return ret;
        }

        private bool ParseZoneDefinition(XElement element, ref ConstraintSet set)
        {
            XAttribute idAttribute = element.Attribute("id");
            XAttribute subdivSetAttribute = element.Attribute("subdivset");
            XAttribute typeAttribute = element.Attribute("type");
            XAttribute excludedFloorsAttribute = element.Attribute("excludedfloors");
            XElement widthElement = element.Element(element.Name.Namespace + "width");
            XElement heightElement = element.Element(element.Name.Namespace + "height");
            XElement amountElement = element.Element(element.Name.Namespace + "amount");
 
            if ((idAttribute == null) || (typeAttribute == null) || 
                (widthElement == null) || (heightElement == null) || 
                (amountElement == null))
            {
                ErrMessage = "Incomplete zone definition.";
                return false;
            }

            Tuple<double, double> widthValue = null;
            Tuple<double, double> heightValue = null;
            Tuple<int, int> amountValue = null;

            if (!TryParseRange(widthElement, out widthValue) || 
                !TryParseRange(heightElement, out heightValue) || 
                !TryParseRange(amountElement, out amountValue))
                return false;

            if (subdivSetAttribute == null)
            {
                set.RegisterZoneDefinition(idAttribute.Value, (ZoneType)Enum.Parse(typeof(ZoneType), typeAttribute.Value), null, widthValue.Item1,
                    widthValue.Item2, heightValue.Item1, heightValue.Item2, amountValue.Item1, amountValue.Item2);
            }
            else
            {
                set.RegisterZoneDefinition(idAttribute.Value, (ZoneType)Enum.Parse(typeof(ZoneType), typeAttribute.Value), subdivSetAttribute.Value, 
                    widthValue.Item1, widthValue.Item2, heightValue.Item1, heightValue.Item2, amountValue.Item1, amountValue.Item2);
            }

            return true;
        }

        private bool TryParseRange(XElement element, out Tuple<double, double> retValue)
        {
            XElement valueElement = element.Element(element.Name.Namespace + "value");
            XElement rangeElement = element.Element(element.Name.Namespace + "range");

            if ((valueElement == null) && (rangeElement == null))
            {
                ErrMessage = "Missing <value> and <range> definition for range.";
                retValue = null;
                return false;
            }

            if (valueElement != null)
            {
                double val = double.Parse(element.Value, CultureInfo.InvariantCulture);
                retValue = new Tuple<double, double>(val, val);
                return true;
            }
            else
            {
                retValue = new Tuple<double, double>(double.Parse(element.Attribute("min").Value, CultureInfo.InvariantCulture),
                    double.Parse(element.Attribute("max").Value, CultureInfo.InvariantCulture));
                return true;
            }
        }

        private bool TryParseRange(XElement element, out Tuple<int, int> retValue)
        {
            XElement valueElement = element.Element(element.Name.Namespace + "value");
            XElement rangeElement = element.Element(element.Name.Namespace + "range");

            if ((valueElement == null) && (rangeElement == null))
            {
                ErrMessage = "Missing <value> and <range> definition for range.";
                retValue = null;
                return false;
            }

            if (valueElement != null)
            {
                int val = int.Parse(element.Value, CultureInfo.InvariantCulture);
                retValue = new Tuple<int, int>(val, val);
                return true;
            }
            else
            {
                retValue = new Tuple<int, int>(int.Parse(element.Attribute("min").Value, CultureInfo.InvariantCulture),
                    int.Parse(element.Attribute("max").Value, CultureInfo.InvariantCulture));
                return true;
            }
        }
    }
}
