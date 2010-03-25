#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace XSharper.Core
{
    ///<summary>XML Schema generator for the given types</summary>
    public static class XsXsdGenerator
    {

        ///<summary>Generate XML schema for the given types
        ///</summary>
        ///<param name="ns">Default namespace</param>
        ///<param name="types">Types to include into schema</param>
        ///<param name="root">Root element</param>
        ///<param name="interfaces">Interface types</param>
        ///<returns>Built schema</returns>
        public static XmlSchema BuildSchema(string ns, Type[] types, Type root, Type[] interfaces)
        {
            XmlSchema xmlSchema = new XmlSchema();
            xmlSchema.Namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
            xmlSchema.Namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlSchema.ElementFormDefault = XmlSchemaForm.Qualified;
            xmlSchema.AttributeFormDefault = XmlSchemaForm.Unqualified;
            xmlSchema.Namespaces.Add("ns", ns);
            xmlSchema.TargetNamespace = ns;

            // Comment
            XmlSchemaAnnotation annotation = new XmlSchemaAnnotation();
            XmlSchemaDocumentation documentation = new XmlSchemaDocumentation();
            XmlDocument helperDocument = new XmlDocument();
            string comment = String.Format("  XML schema for {0} , generated at {1}  ", ns, DateTime.Now.ToString());
            documentation.Markup = new XmlNode[1] { helperDocument.CreateComment(comment) };
            annotation.Items.Add(documentation);
            xmlSchema.Items.Add(annotation);

            // Create group "action" to refer to any action
            var ints = new Dictionary<Type, XmlSchemaGroup>();
            if (interfaces != null)
            {
                foreach (var intf in interfaces)
                {
                    var action = new XmlSchemaGroup();
                    action.Name = getXmlTypeName(intf);
                    action.Particle = new XmlSchemaChoice();
                    xmlSchema.Items.Add(action);
                    ints.Add(intf, action);
                }
            }

            Dictionary<Type, XmlSchemaType> xmlTypes = new Dictionary<Type, XmlSchemaType>();
            foreach (var type in types)
            {
                // If it does not have our XML header - skip it
                var na = (CustomAttributeHelper.First<XsTypeAttribute>(type));
                if (na == null)
                    continue;


                // Check if it is complex or simple
                XmlSchemaComplexType ct = new XmlSchemaComplexType();
                ct.Name = getXmlTypeName(type);


                XmlSchemaObjectCollection attr = createComplexType(type, ct, ns, ints);

                // Add the new element as an option to the "action" group
                foreach (var i in ints)
                {
                    bool isAction = (type.FindInterfaces((tp, nu) => tp == i.Key, null).Length != 0);
                    if (isAction)
                    {
                        foreach (var tp in CustomAttributeHelper.All<XsTypeAttribute>(type))
                        {
                            if (!string.IsNullOrEmpty(tp.Name))
                                i.Value.Particle.Items.Add(new XmlSchemaElement
                                {
                                    Name = tp.Name,
                                    MinOccurs = 0,
                                    SchemaTypeName = new XmlQualifiedName(ct.Name, ns)
                                });
                        }
                    }
                }

                // Work with attributes
                foreach (var o in generateAttributes(xmlSchema, type, xmlTypes, ns))
                    attr.Add(o);

                if (na.AnyAttribute)
                {
                    ct.AnyAttribute = new XmlSchemaAnyAttribute
                    {
                        ProcessContents = XmlSchemaContentProcessing.Skip
                    };
                }


                // Add type to the list
                xmlTypes.Add(type, ct);
                xmlSchema.Items.Add(ct);

                if (root.IsAssignableFrom(type))
                {
                    // Add all variations of Script names as element
                    foreach (var o in CustomAttributeHelper.All<XsTypeAttribute>(root))
                    {
                        xmlSchema.Items.Add(new XmlSchemaElement
                        {
                            Name = o.Name,
                            SchemaTypeName = new XmlQualifiedName(xmlTypes[typeof(Script)].Name, ns)
                        });
                    }
                }
            }
            return xmlSchema;
        }

        private static string getXmlTypeName(Type type)
        {
            return type.FullName.Replace('`', '_').Replace('+', '_');
        }

        private static bool allowsText(Type t)
        {
            foreach (var pi in t.GetProperties())
                foreach (var att in CustomAttributeHelper.All<XsAttributeAttribute>(pi))
                    if (string.IsNullOrEmpty(att.Name))
                        return true;
            return false;
        }


        private static XmlSchemaObjectCollection createComplexType(Type type, XmlSchemaComplexType ct, string ns, Dictionary<Type, XmlSchemaGroup> intfs)
        {
            ct.IsMixed = allowsText(type);

            XmlSchemaSequence sequence = new XmlSchemaSequence();

            foreach (var pi in XsElement.GetOrderedElementProperties(type))
            {
                var atn = (CustomAttributeHelper.First<XsElementAttribute>(pi));

                if (atn.CollectionItemType != null && intfs.ContainsKey(atn.CollectionItemType))
                {
                    sequence.Items.Add(new XmlSchemaGroupRef
                    {
                        MinOccurs = 0,
                        MaxOccursString = "unbounded",
                        RefName = new XmlQualifiedName(getXmlTypeName(atn.CollectionItemType), ns)
                    });
                }
                else if (atn.CollectionItemType == null)
                {
                    if (atn.Name == "")
                    {
                        sequence.Items.Add(new XmlSchemaAny
                        {
                            MinOccurs = 0,
                            MaxOccursString = "unbounded",
                            ProcessContents = XmlSchemaContentProcessing.Skip
                        });
                    }
                    else
                        sequence.Items.Add(new XmlSchemaElement
                        {
                            MinOccurs = 0,
                            MaxOccurs = 1,
                            Name = atn.Name,
                            SchemaTypeName = new XmlQualifiedName(getXmlTypeName(pi.PropertyType), ns)
                        });
                }
                else
                {
                    sequence.Items.Add(new XmlSchemaElement
                    {
                        MinOccurs = 0,
                        MaxOccursString = "unbounded",
                        Name = atn.CollectionItemElementName,
                        SchemaTypeName = new XmlQualifiedName(getXmlTypeName(atn.CollectionItemType), ns)
                    });
                }

            }

            if (sequence.Items.Count > 0)
                ct.Particle = sequence;
            else
                ct.Particle = null;

            return ct.Attributes;
        }

        private static List<XmlSchemaAttribute> generateAttributes(XmlSchema xmlSchema, Type type, Dictionary<Type, XmlSchemaType> xmlTypes, string ns)
        {
            var def = Utils.CreateInstance(type);
            var ret = new List<XmlSchemaAttribute>();
            foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty))
            {
                var attnames = XsAttributeAttribute.GetNames(pi,false);
                if (attnames==null)
                    continue;
                
                foreach (var nameAttr in attnames)
                {
                    if (string.IsNullOrEmpty(nameAttr))
                        continue;

                    XmlSchemaAttribute xsa = new XmlSchemaAttribute();
                    xsa.Name = nameAttr;

                    if (pi.PropertyType == typeof(bool))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean).QualifiedName;
                    else if (pi.PropertyType == typeof(int))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Integer).QualifiedName;
                    else if (pi.PropertyType == typeof(float))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Float).QualifiedName;
                    else if (pi.PropertyType == typeof(double))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Double).QualifiedName;
                    else if (pi.PropertyType == typeof(uint))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UnsignedInt).QualifiedName;
                    else if (pi.PropertyType == typeof(ulong))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UnsignedLong).QualifiedName;
                    else if (pi.PropertyType == typeof(long))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Long).QualifiedName;
                    else if (pi.PropertyType == typeof(decimal))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Decimal).QualifiedName;
                    else if (pi.PropertyType.IsEnum)
                    {
                        if (!xmlTypes.ContainsKey(pi.PropertyType))
                        {
                            createEnum(pi, xmlTypes, xmlSchema);
                        }
                        xsa.SchemaTypeName = new XmlQualifiedName(xmlTypes[pi.PropertyType].Name, ns);
                    }
                    else if (pi.PropertyType == typeof(string))
                        xsa.SchemaTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String).QualifiedName;


                    var req = CustomAttributeHelper.First<XsRequiredAttribute>(pi);
                    if (req!=null && (string.IsNullOrEmpty(req.Name) || req.Name==nameAttr))
                        xsa.Use = XmlSchemaUse.Required;
                    else
                    {
                        object v = pi.GetValue(def, null);
                        if (v != null)
                        {
                            string value = v.ToString();
                            if ((pi.PropertyType.IsPrimitive || pi.PropertyType.IsEnum))
                                value = (value.Substring(0, 1).ToLower() + value.Substring(1));
                            xsa.DefaultValue = value;
                        }
                    }

                    ret.Add(xsa);
                }
            }
            return ret;
        }

        private static void createEnum(PropertyInfo pi, Dictionary<Type, XmlSchemaType> xmlTypes, XmlSchema xmlSchema)
        {
            // Create enum
            var res = new XmlSchemaSimpleTypeRestriction();
            res.BaseTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.NmToken).QualifiedName;

            foreach (var o in Enum.GetNames(pi.PropertyType))
            {
                res.Facets.Add(new XmlSchemaEnumerationFacet
                    {
                        Value = (o.Substring(0, 1).ToLower() + o.Substring(1))
                });
            }

            XmlSchemaSimpleType st = new XmlSchemaSimpleType
            {
                Content = res
            };

            // For flags must create a union of the values & string
            if (CustomAttributeHelper.Has<System.FlagsAttribute>(pi.PropertyType))
            {
                XmlSchemaSimpleType st2 = new XmlSchemaSimpleType();
                st2.Name = getXmlTypeName(pi.PropertyType);


                var union = new XmlSchemaSimpleTypeUnion();

                XmlSchemaSimpleType st3 = new XmlSchemaSimpleType();
                var res3 = new XmlSchemaSimpleTypeRestriction();
                res3.BaseTypeName = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String).QualifiedName;
                st3.Content = res3;

                union.BaseTypes.Add(st);
                union.BaseTypes.Add(st3);

                st2.Content = union;
                xmlSchema.Items.Add(st2);
                xmlTypes[pi.PropertyType] = st2;
            }
            else
            {
                st.Name = getXmlTypeName(pi.PropertyType);
                xmlSchema.Items.Add(st);
                xmlTypes[pi.PropertyType] = st;
            }
        }
    }
}