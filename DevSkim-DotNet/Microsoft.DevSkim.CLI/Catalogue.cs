// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim.CLI
{
    internal class Catalogue
    {
        public Catalogue(RuleSet rules)
        {
            _rules = rules;
        }

        public void ToCsv(string fileName, string[] columnList)
        {
            if (columnList.Length == 0)
                columnList = new string[] { "id" };

            using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                List<string> lineItems = new List<string>();
                foreach (string col in columnList)
                {
                    lineItems.Add(string.Format("\"{0}\"", col));
                }
                sw.WriteLine(string.Join(",", lineItems));

                foreach (ConvertedOatRule rule in _rules.AsEnumerable())
                {
                    lineItems.Clear();
                    foreach (string col in columnList)
                    {
                        lineItems.Add(string.Format("\"{0}\"", GetProperty(rule.DevSkimRule, col)));
                    }

                    sw.WriteLine(string.Join(",", lineItems));
                }
                sw.Close();
                fs.Close();
            }
        }

        private RuleSet _rules;

        private string GetProperty(Rule rule, string propName)
        {
            string result = "#PROPERTY NOT FOUND";

            Type t = typeof(Rule);
            foreach (PropertyInfo property in t.GetProperties())
            {
                foreach (Attribute attr in property.GetCustomAttributes(true))
                {
                    if (attr is JsonPropertyNameAttribute jsonAttr && jsonAttr.Name == propName)
                    {
                        return GetPropertyValue(property, rule);
                    }
                }

                if (property.Name == propName)
                {
                    return GetPropertyValue(property, rule);
                }
            }

            return result;
        }

        private string GetPropertyValue(PropertyInfo property, Rule rule)
        {
            string result = string.Empty;
            switch (property.GetValue(rule))
            {
                case string s:
                    result = s;
                    break;

                case string[] list:
                    result = string.Join(",", list);
                    break;

                case SearchPattern[] _:
                case SearchCondition[] _:
                case CodeFix[] _:
                    result = "#UNSUPPORTED PROPERTY";
                    break;

                default:
                    result = property.GetValue(rule)?.ToString() ?? string.Empty;
                    break;
            }

            return result;
        }
    }
}