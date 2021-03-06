﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace splitter
{
    public class Fragment
    {
        public string Id { get; set; }

        public XElement Content { get; set; }

        public string Type { get; set; }
    }

    public static class StringExtensions
    {
        public static string SplitCamel(this string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1-").ToLower();
        }
    }

    class Program
    {
        //<VTM>
        // <VTMID>68088000</VTMID>
        // <NM>Acebutolol</NM>
        //</VTM>

        //<VTM>
        // <VTMID>13868411000001104</VTMID>
        // <NM>Promethazine hydrochloride</NM>
        // <VTMIDPREV>404843004</VTMIDPREV>
        // <VTMIDDT>2008-08-08</VTMIDDT>
        //</VTM>

        public class LookupInfo
        {
            public string BnfId { get; set; }
            public string Type { get; set; }
            public string Slug { get; set; }
            public string Href { get; set; }
        }

		static readonly Slugger Slugger = new Slugger();

        public static LookupInfo AnnotateElement(XElement e)
        {
            var title = e.Element("title");
			var bnfid = e.Attribute("id").Value.Replace(".xml", "").Replace("#", "");
			var type = GetTopicType(e);
			var slug = RootIds.Contains(bnfid) ? "index" : bnfid;

			if (slug != "index" && title != null && TypesToSlug.Contains(type))
                slug = Slugger.For(title.Value);

			var lookup = new LookupInfo {
				BnfId = bnfid,
				Type = type,
				Slug = slug,
				Href = type.SplitCamel () + "/" + slug + ".xml"
			};

			e.SetAttributeValue("id", lookup.Slug);
			e.SetAttributeValue("bnfid", lookup.BnfId);
			e.SetAttributeValue("type", lookup.Type);
			e.SetAttributeValue("Slug", lookup.Slug);
			e.SetAttributeValue("href", lookup.Href);

            return lookup;
		}

		static readonly List<string> RootIds = new List<string>
		{
			"PHP101868",		// dental prescribers formulary
			"PHP101869",		// nurse practitioners formulary
			"PHP101071",		// wound management
			"PHP103610",		// borderline stubstances
			"medicalDevices",	// medical devices
   "About",
   "Guidance"
		};

        static readonly List<string> TypesToSlug = new List<string>
        {
			"borderlineSubstance",
			"borderlineSubstanceTaxonomy",
			"woundManagement",
			"treatmentSummary",
			"drugClassifications",
			"drug",
			"drugClass",
			"medicalDevice",
			"medicalDeviceType",
			"clinicalMedicinalProductInformation",
			"interaction",
            "clinicalMedicalDeviceInformationGroup",
			"about",
			"guidance",
			"labels",
			"cautionaryAndAdvisoryLabels",
			"dental-practitioners-formulary",
			"nurse-prescribers-formulary"
        };

        static void Main(string[] args)
        {
            if (args.Length < 2)
                throw new Exception("must specify input file and output directory");

            var filename = args[0];
            var outputdir = args[1];
            if (!Directory.Exists(outputdir))
                Directory.CreateDirectory(outputdir);

            Func<Fragment, bool> process = f => true;
            if (args.Length > 2)
            {
                var types = args[2].Split(new[] {','});
                process = f => types.Contains(f.Type);
            }

   var doc = XDocument.Load(filename, LoadOptions.PreserveWhitespace);

			// change the outputclass so it doesn't clash
			foreach (var section in doc.XPathSelectElements("//section[@outputclass='electrolytes']"))
			{
				section.SetAttributeValue("outputclass", "fluidAndElectrolytes");
			}

			foreach (var section in doc.XPathSelectElements("//topic[@outputclass='labels']"))
			{
				section.SetAttributeValue("outputclass", "treatmentSummary about");
    doc.XPathSelectElement("//topic[@id='About']").Add(section);
    section.Remove();
			}

			foreach (var section in doc.XPathSelectElements("//topic[@outputclass='cautionaryAndAdvisoryLabels']"))
			{
				section.SetAttributeValue("outputclass", "treatmentSummary about");
    doc.XPathSelectElement("//topic[@id='About']").Add(section);
    section.Remove();
			}

            // <data name="inheritsFromClass">PHP34650</data>
            foreach (var data in doc.XPathSelectElements("//data[@name='inheritsFromClass' or @name='category']"))
            {
                data.Name = "xref";
                data.SetAttributeValue("href",data.Value);
			}

			// generate lookup info
            var lookup = doc.XPathSelectElements("//topic[@id] | //section[@id]")
				.Select(AnnotateElement)
                .ToDictionary(i => i.BnfId, i => i);

			// annotate links with lookup info
            foreach (var xref in doc.XPathSelectElements("//xref").ToList())
            {
                var href = xref.Attribute("href");
                if (href == null) continue;
                var id = href.Value.Replace(".xml", "").Replace("#","");

                var parts = id.Split(new[] {'/'});

                if (parts.Count() == 1 && lookup.ContainsKey(id))
                {
                    //alter the href to point at the new id, stash the old one
                    xref.SetAttributeValue("rel", lookup[id].Type.ToLower());
                    xref.SetAttributeValue("href", lookup[id].Href);
                    xref.SetAttributeValue("bnfid", lookup[id].BnfId);
                }

                // href="#PHP78102/PHP185"
                // href="treatment-summary/PHP78102#PHP185"
                if (parts.Count() == 2 && lookup.ContainsKey(parts[0]) && lookup.ContainsKey(parts[1]))
                {
                    xref.SetAttributeValue("rel", lookup[parts[1]].Type.ToLower());
                    xref.SetAttributeValue("href", lookup[parts[0]].Href + "#" + lookup[parts[1]].Slug);
                    xref.SetAttributeValue("bnfid", lookup[parts[1]].BnfId);
                }
            }
            
            var fragments = ProcessWithId(doc.Root);

            foreach (var fragment in fragments.Where(process))
            {
                var type = fragment.Type.Replace("#", "");
                
                type = type.SplitCamel();

                var typeDir = Path.Combine(outputdir, type);
                
                if (!Directory.Exists(typeDir))
                    Directory.CreateDirectory(typeDir);
                var path = Path.Combine(typeDir, fragment.Id + ".xml");
                using (var stream = new StreamWriter(path,false))
                    stream.Write(fragment.Content.ToString());
            }
        }

        static IEnumerable<Fragment> ProcessWithId(XElement element)
        {
            var childFragments = FindAllAddressableChildren(element)
                .SelectMany(ProcessWithId)
                .Concat(new List<Fragment>{new Fragment
            {
                Id = DeriveId(element),
                Content = CreateLinks(element),
                Type = GetTopicType(element)
            }});

            return childFragments;
		}

		public static string DeriveId(XElement element)
		{
			return element.Attribute("id") == null ? "index" : element.Attribute("id").Value;
		}

        static IEnumerable<XElement> FindAllAddressableChildren(XContainer element)
        {
            return element.Elements()
                    .SelectMany(e => e.Attribute("id") != null && IsUnitOfWork(e) ?
                        new List<XElement> { e } :
                        FindAllAddressableChildren(e));
        }

        static string CreateLink(XElement child)
        {
			return child.GetAttributeValue("href");
        }

        static XElement CreateLinks(XElement element)
        {
            var copy = new XElement(element);
            var addressableChildren = FindAllAddressableChildren(copy).ToList();
            foreach (var addressableChild in addressableChildren)
            {
                var title = addressableChild.Descendants("title").Select(e => e.Value).FirstOrDefault() ?? "";
                addressableChild.ReplaceWith(new XElement("xref", title,
                    new XAttribute("href", CreateLink(addressableChild)),
                    new XAttribute("rel",
                        addressableChild.Attribute("outputclass") != null
                            ? addressableChild.Attribute("outputclass").Value
                            : "")));
            }
            foreach (var xref in copy.Descendants("xref").Where(l => l.Attribute("href").Value.StartsWith("#")).ToList())
                xref.SetAttributeValue("href", xref.Attribute("href").Value.Replace("#", "") + ".xml");

            return copy;
        }

        static readonly List<string> TypesOfInterest = new List<string>
        {
            "publication",
			"evidenceCategories",

			"borderlineSubstance",
			"borderlineSubstanceAcbs",
			"borderlineSubstanceTaxonomy",
			"borderlineSubstancePrep",

			"woundManagement",
			"productGroups",

			"treatmentSummary",

			"drugClassifications",
			"drug",
			"drugClass",
			"medicalDevice",
			"medicalDeviceType",
			"medicinalForm",
			"clinicalMedicinalProductInformation",
			"#clinicalMedicinalProductInformation",

            "clinicalMedicalDeviceInformationGroup",

			"interaction",
	        "guidance",
	        "about",
	        "labels",
	        "cautionaryAndAdvisoryLabels",

            //no output class
			"#PHP101868",
			"#PHP101869",
			"dental-practitioners-formulary",
			"nurse-prescribers-formulary",

            //the lists
            "#drugs",
            "#treatmentSummaries",
            "#medicalDevices",
            "#interactions",
            "#borderlineSubstances",
            "#drugClasses",
            "#bnf-interactions-list",
            "#About",
            "#Guidance",

            //treatmentSummaries
            "malariaProphylaxisRegimens",
            //"intramuscularAdrenalineEmergency",
            "helicobacterPyloriRegimens",
            "antiTuberculosisTreatments",
            "bloodMonitoringStrips",
            "hrtRisks",
            "parenteralFeeding",
            "fluidAndElectrolytes"
        };

        static readonly Dictionary<string,string> TopicHack = new Dictionary<string, string>
        {
			{"PHP101868","dental-practitioners-formulary"},
			{"PHP101869","nurse-prescribers-formulary"},
        };

        static bool IsUnitOfWork(XElement xElement)
        {
            if (xElement.Name == "topic" || xElement.Name == "section")
            {
                var type = GetTopicType(xElement);
                return TypesOfInterest.Contains(type);
            }
            return false;
        }

        static string GetTopicType(XElement topic)
        {
            if (!topic.HasAttributes) return "";

            var bnfid = topic.GetAttributeValue("bnfid", topic.GetAttributeValue("id", "index")).Trim();
            var outputclass = topic.GetAttributeValue("outputclass").Trim();

			if (TopicHack.ContainsKey(bnfid))
				return TopicHack[bnfid];

            if ((bnfid == "" || bnfid.StartsWith("PHP") || bnfid.StartsWith("bnf_")) && outputclass != "")
            {
                var types = outputclass.Contains(" ") ? outputclass.Split(new[] { ' ' }) : new[] { outputclass };
                if (types.Length > 1 && new[] { "about", "guidance" }.Contains(types[1]))
                    types = types.Reverse().ToArray();

                return types[0];
            }

            if (bnfid.StartsWith("bnf_"))
                return "drugInterction";
			
            return "#" + bnfid;
        }
    }

    public static class XElementExtensions
    {
        public static string GetAttributeValue(this XElement element, string name, string defaultValue = "")
        {
            var attibute = element.Attribute(name);
            return attibute == null ? defaultValue : attibute.Value;
        }
    }
}
